using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/restaurant/orders")]
[Authorize(Roles = "RestaurantAdmin")]
public class RestaurantOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly IHubContext<RiderHub> _riderHub;
    private readonly IHubContext<CompanyHub> _companyHub;
    private readonly IHubContext<RestaurantHub> _restaurantHub;
    private readonly FcmV1Service _fcm;

    public RestaurantOrdersController(
        ApplicationDbContext db,
        ICurrentTenantService tenant,
        IHubContext<RiderHub> riderHub,
        IHubContext<CompanyHub> companyHub,
        IHubContext<RestaurantHub> restaurantHub,
        FcmV1Service fcm)
    {
        _db = db;
        _tenant = tenant;
        _riderHub = riderHub;
        _companyHub = companyHub;
        _restaurantHub = restaurantHub;
        _fcm = fcm;
    }

    private IQueryable<Order> Mine() =>
        _db.Orders.IgnoreQueryFilters()
            .Where(o => o.RestaurantId == _tenant.RestaurantId);

    // ---- Pedidos entrantes (aceptados por rider, esperando confirmación) ----
    [HttpGet("incoming")]
    public async Task<ActionResult<IEnumerable<RestaurantOrderDto>>> Incoming()
    {
        var orders = await Mine()
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.RiderAccepted && o.RestaurantConfirmedAt == null)
            .OrderBy(o => o.RiderAcceptedAt)
            .ToListAsync();
        return Ok(orders.Select(ToDto).ToList());
    }

    // ---- Pedidos activos del restaurante (en curso ya confirmados) ----
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<RestaurantOrderDto>>> Active()
    {
        var orders = await Mine()
            .Include(o => o.Items)
            .Where(o => o.Status == OrderStatus.ReadyForPickup || o.Status == OrderStatus.PickedUp || o.Status == OrderStatus.InTransit)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders.Select(ToDto).ToList());
    }

    [HttpPost("{orderId:guid}/confirm-ready")]
    public async Task<IActionResult> ConfirmReady(Guid orderId, [FromBody] ConfirmReadyRequest req)
    {
        var order = await Mine().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.RiderAccepted)
            return BadRequest(new { isSuccess = false, error = "El pedido no está esperando confirmación" });

        if (!req.Ready)
            return await RejectInternal(order, "Restaurante marcó que no puede atender");

        var now = DateTime.UtcNow;
        order.RestaurantConfirmedAt = now;
        order.Status = OrderStatus.ReadyForPickup;
        order.PickupCode = OrderService_GeneratePickup();
        order.DeliveryCode = OrderService_GenerateDelivery();
        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Restaurant, Description = "Restaurante confirmó pedido listo" });

        await _db.SaveChangesAsync();

        if (order.AssignedRiderId.HasValue)
        {
            await _riderHub.Clients.Group($"rider-{order.AssignedRiderId}").SendAsync("orderGreenLight", new
            {
                orderId = order.Id,
                status = order.Status.ToString(),
                pickupCode = order.PickupCode,
                deliveryCode = order.DeliveryCode
            });

            var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == order.AssignedRiderId);
            if (rider != null)
            {
                await _fcm.SendToTokenAsync(rider.FcmToken,
                    "🟢 Pedido listo en el restaurante",
                    $"Código de recolección: {order.PickupCode}. Ve al origen.",
                    new Dictionary<string, string>
                    {
                        ["event"] = "orderGreenLight",
                        ["orderId"] = order.Id.ToString(),
                        ["pickupCode"] = order.PickupCode ?? ""
                    });
            }
        }
        return Ok(new { status = order.Status.ToString() });
    }

    [HttpPost("{orderId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid orderId, [FromBody] RejectOfferRequest req)
    {
        var order = await Mine().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return NotFound();
        if (order.Status != OrderStatus.RiderAccepted)
            return BadRequest(new { isSuccess = false, error = "El pedido no está en fase de confirmación" });

        return await RejectInternal(order, string.IsNullOrWhiteSpace(req.Reason) ? "Restaurante rechazó el pedido" : req.Reason);
    }

    private async Task<IActionResult> RejectInternal(Order order, string reason)
    {
        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Cancelled;
        order.CancelReason = OrderCancelReason.RestaurantRejected;
        order.CancelDetail = reason; // se muestra al cliente en su tracking
        order.CancelledAt = now;

        if (order.AssignedRiderId.HasValue)
        {
            var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == order.AssignedRiderId);
            if (rider != null && !await _db.Orders.AnyAsync(o => o.AssignedRiderId == rider.Id
                && o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Delivered))
                rider.IsBusy = false;
        }

        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Restaurant, Description = $"Restaurante rechazó: {reason}" });
        await _db.SaveChangesAsync();

        await _companyHub.Clients.Group($"company-{order.TenantId}").SendAsync("orderCancelled", new { orderId = order.Id, reason });
        return Ok(new { status = order.Status.ToString(), reason });
    }

    private static RestaurantOrderDto ToDto(Order o) => new(
        o.Id, o.Type.ToString().ToLowerInvariant(), o.Status.ToString(), o.DestinationAddress,
        o.CustomerName, o.CustomerPhone, o.ProductsAmount, o.DeliveryFeeAmount,
        o.PaymentMethod.ToString().ToLowerInvariant(), o.CreatedAt, o.RiderAcceptedAt,
        o.Items.Select(i => new OrderTrackingItemDto(i.Name, i.Quantity, i.UnitPrice)).ToList());

    private static string OrderService_GeneratePickup() => Infrastructure.Services.OrderService.GeneratePickupCode();
    private static string OrderService_GenerateDelivery() => Infrastructure.Services.OrderService.GenerateDeliveryCode();
}
