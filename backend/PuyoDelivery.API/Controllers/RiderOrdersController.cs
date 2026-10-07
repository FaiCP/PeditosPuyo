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
[Route("api/rider/orders")]
[Authorize(Roles = "Rider")]
public class RiderOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;
    private readonly IHubContext<CompanyHub> _companyHub;
    private readonly IHubContext<RestaurantHub> _restaurantHub;
    private readonly IHubContext<RiderHub> _riderHub;

    public RiderOrdersController(
        ApplicationDbContext db,
        ICurrentTenantService tenant,
        IHubContext<CompanyHub> companyHub,
        IHubContext<RestaurantHub> restaurantHub,
        IHubContext<RiderHub> riderHub)
    {
        _db = db;
        _tenant = tenant;
        _companyHub = companyHub;
        _restaurantHub = restaurantHub;
        _riderHub = riderHub;
    }

    private async Task<List<Order>> LoadRiderOrders(params OrderStatus[] statuses)
    {
        return await _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.AssignedRider)
            .Where(o => o.AssignedRiderId == _tenant.RiderId && statuses.Contains(o.Status))
            .ToListAsync();
    }

    // ---- Ofertas pendientes para este rider ----
    [HttpGet("offers")]
    public async Task<ActionResult<IEnumerable<RiderOfferDto>>> Offers()
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();

        var offers = await _db.RiderOffers
            .IgnoreQueryFilters()
            .Include(o => o.Order).ThenInclude(ord => ord.Items)
            .Where(o => o.RiderId == _tenant.RiderId && o.Status == RiderOfferStatus.Pending)
            .OrderBy(o => o.ExpiresAt)
            .Select(o => new RiderOfferDto(
                o.Id, o.OrderId, o.Order.Type.ToString().ToLowerInvariant(), o.Order.OriginName,
                o.Order.OriginAddress, o.Order.DestinationAddress, o.Order.Description,
                o.Order.DeliveryFeeAmount, o.Order.Items.Select(i => new OrderTrackingItemDto(i.Name, i.Quantity, i.UnitPrice)).ToList(),
                o.ExpiresAt))
            .ToListAsync();

        return Ok(offers);
    }

    [HttpPost("offers/{offerId:guid}/accept")]
    public async Task<ActionResult<OrderTrackingDto>> Accept(Guid offerId)
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();

        var offer = await _db.RiderOffers
            .IgnoreQueryFilters()
            .Include(o => o.Order).ThenInclude(ord => ord.Items)
            .Include(o => o.Order).ThenInclude(ord => ord.Restaurant)
            .FirstOrDefaultAsync(o => o.Id == offerId && o.RiderId == _tenant.RiderId);
        if (offer == null) return NotFound();
        if (offer.Status != RiderOfferStatus.Pending)
            return BadRequest(new { isSuccess = false, error = "La oferta ya no está disponible" });

        var now = DateTime.UtcNow;
        var order = offer.Order;
        if (order.Status is not (OrderStatus.WaitingRider or OrderStatus.OnHold))
            return BadRequest(new { isSuccess = false, error = "El pedido ya no admite asignación" });

        offer.Status = RiderOfferStatus.Accepted;
        offer.RespondedAt = now;
        order.AssignedRiderId = offer.RiderId;
        order.RiderAcceptedAt = now;
        order.Status = OrderStatus.RiderAccepted;
        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Rider, ActorId = _tenant.RiderId, Description = "Rider aceptó el pedido" });

        // Compra/encargo: no hay restaurante que confirmar -> luz verde + códigos ya.
        if (order.Type != OrderType.Restaurant)
        {
            order.RestaurantConfirmedAt = now;
            order.Status = OrderStatus.ReadyForPickup;
            GenerateCodes(order);
            await _riderHub.Clients.Group($"rider-{offer.RiderId}").SendAsync("orderGreenLight", new
            {
                orderId = order.Id, status = order.Status.ToString(), pickupCode = order.PickupCode, deliveryCode = order.DeliveryCode
            });
        }

        await _db.SaveChangesAsync();
        return Ok(OrderService.ToTracking(order, order.Restaurant, order.AssignedRider));
    }

    [HttpPost("offers/{offerId:guid}/reject")]
    public async Task<IActionResult> Reject(Guid offerId, [FromBody] RejectOfferRequest req)
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();

        var offer = await _db.RiderOffers
            .IgnoreQueryFilters()
            .Include(o => o.Order)
            .FirstOrDefaultAsync(o => o.Id == offerId && o.RiderId == _tenant.RiderId);
        if (offer == null) return NotFound();
        if (offer.Status != RiderOfferStatus.Pending) return BadRequest(new { isSuccess = false, error = "Oferta no pendiente" });

        var now = DateTime.UtcNow;
        offer.Status = RiderOfferStatus.Rejected;
        offer.RespondedAt = now;
        offer.RejectionReason = req.Reason;

        var order = offer.Order;
        order.Status = OrderStatus.WaitingRider; // el motor ofrecerá al siguiente en próximos ticks
        var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == _tenant.RiderId);
        if (rider != null) rider.IsBusy = false;
        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Rider, ActorId = _tenant.RiderId, Description = $"Rider rechazó: {req.Reason}" });

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Confirmación de recojo en origen con PickupCode ----
    [HttpPost("{orderId:guid}/pickup")]
    public async Task<IActionResult> Pickup(Guid orderId, [FromBody] VerifyCodeRequest req)
    {
        var order = await LoadOrderForRider(orderId, OrderStatus.ReadyForPickup);
        if (order == null) return NotFound();

        if (order.PickupCode == null || req.Code != order.PickupCode)
            return BadRequest(new { isSuccess = false, error = "Código de recolección inválido" });

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.PickedUp;
        order.PickedUpAt = now;
        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Rider, ActorId = _tenant.RiderId, Description = "Pedido recogido en origen" });
        await _db.SaveChangesAsync();
        return Ok(new { status = order.Status.ToString() });
    }

    [HttpPost("{orderId:guid}/in-transit")]
    public async Task<IActionResult> InTransit(Guid orderId)
    {
        var order = await LoadOrderForRider(orderId, OrderStatus.PickedUp);
        if (order == null) return NotFound();
        order.Status = OrderStatus.InTransit;
        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Rider, ActorId = _tenant.RiderId, Description = "En camino al destino" });
        await _db.SaveChangesAsync();
        return Ok(new { status = order.Status.ToString() });
    }

    // ---- Entrega al cliente con DeliveryCode ----
    [HttpPost("{orderId:guid}/deliver")]
    public async Task<IActionResult> Deliver(Guid orderId, [FromBody] VerifyCodeRequest req)
    {
        var order = await LoadOrderForRider(orderId, OrderStatus.InTransit, OrderStatus.PickedUp);
        if (order == null) return NotFound();

        if (order.DeliveryCode == null || req.Code != order.DeliveryCode)
            return BadRequest(new { isSuccess = false, error = "Código de entrega inválido" });

        var now = DateTime.UtcNow;
        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = now;
        order.FeeCollected = true;
        order.FeeCollectedAmount = order.DeliveryFeeAmount;

        var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == _tenant.RiderId);
        if (rider != null) rider.IsBusy = false;

        _db.OrderEvents.Add(new OrderEvent { OrderId = order.Id, TenantId = order.TenantId, ActorType = OrderActorType.Rider, ActorId = _tenant.RiderId, Description = "Pedido entregado al cliente" });
        await _restaurantHub.Clients.Group($"restaurant-{order.RestaurantId}").SendAsync("orderDelivered", new { orderId = order.Id });
        await _companyHub.Clients.Group($"company-{order.TenantId}").SendAsync("orderDelivered", new { orderId = order.Id });
        await _db.SaveChangesAsync();
        return Ok(new { status = order.Status.ToString() });
    }

    // ---- Registrar token FCM del rider ----
    [HttpPost("fcm-token")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest req)
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();
        var rider = await _db.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == _tenant.RiderId);
        if (rider == null) return NotFound();
        rider.FcmToken = req.Token;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ---- Active orders del rider (para la app) ----
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<RiderActiveOrderDto>>> Active()
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();
        var orders = await LoadRiderOrders(OrderStatus.RiderAccepted, OrderStatus.ReadyForPickup, OrderStatus.PickedUp, OrderStatus.InTransit);
        return Ok(orders.Select(o => new RiderActiveOrderDto(
            OrderService.ToTracking(o, o.Restaurant, o.AssignedRider),
            o.OriginLocation.Y, o.OriginLocation.X,
            o.DestinationLocation.Y, o.DestinationLocation.X,
            o.Status >= OrderStatus.ReadyForPickup ? o.PickupCode : null)).ToList());
    }

    // ---- Historial de entregas del rider ----
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<OrderTrackingDto>>> History()
    {
        if (!_tenant.RiderId.HasValue) return Unauthorized();
        var orders = await LoadRiderOrders(OrderStatus.Delivered);
        return Ok(orders
            .OrderByDescending(o => o.DeliveredAt)
            .Take(50)
            .Select(o => OrderService.ToTracking(o, o.Restaurant, o.AssignedRider))
            .ToList());
    }

    private async Task<Order?> LoadOrderForRider(Guid orderId, params OrderStatus[] allowed)
    {
        var order = await _db.Orders.IgnoreQueryFilters().FirstOrDefaultAsync(o => o.Id == orderId && o.AssignedRiderId == _tenant.RiderId);
        if (order == null) return null;
        return allowed.Contains(order.Status) ? order : null;
    }

    private static void GenerateCodes(Order order)
    {
        order.PickupCode = OrderService.GeneratePickupCode();
        order.DeliveryCode = OrderService.GenerateDeliveryCode();
    }
}
