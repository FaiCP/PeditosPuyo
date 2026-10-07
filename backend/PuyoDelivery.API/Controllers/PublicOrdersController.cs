using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/p/{token}")]
[AllowAnonymous]
public class PublicOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly OrderService _orders;

    public PublicOrdersController(ApplicationDbContext context, OrderService orders)
    {
        _context = context;
        _orders = orders;
    }

    private async Task<CustomerToken?> ResolveActiveToken(string token)
    {
        return await _context.CustomerTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Token == token && t.IsActive && t.ExpiresAt > DateTime.UtcNow);
    }

    // ---- Catálogo ----
    [HttpGet("catalog")]
    public async Task<ActionResult<IEnumerable<PublicRestaurantDto>>> Catalog(string token)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var restaurants = await _context.Restaurants
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == ct.TenantId && r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new PublicRestaurantDto(
                r.Id, r.Name, r.Address, r.Phone, r.MenuSummary,
                r.MenuItems.Count(m => m.IsActive),
                r.PaymentQrUrl != null && r.PaymentQrUrl != ""))
            .ToListAsync();

        return Ok(restaurants);
    }

    [HttpGet("restaurants/{restaurantId:guid}")]
    public async Task<ActionResult<PublicRestaurantDetailDto>> RestaurantMenu(string token, Guid restaurantId)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var restaurant = await _context.Restaurants
            .IgnoreQueryFilters()
            .Include(r => r.MenuItems.Where(m => m.IsActive))
            .FirstOrDefaultAsync(r => r.Id == restaurantId && r.TenantId == ct.TenantId && r.IsActive);
        if (restaurant == null) return NotFound(new { isSuccess = false, error = "Restaurante no encontrado" });

        return Ok(new PublicRestaurantDetailDto(
            restaurant.Id, restaurant.Name, restaurant.Address, restaurant.Phone, restaurant.PaymentQrUrl,
            restaurant.MenuItems
                .Select(m => new PublicMenuItemDto(m.Id, m.Name, m.Description, m.Price))
                .ToList()));
    }

    // ---- Crear pedido ----
    [HttpPost("orders")]
    public async Task<ActionResult<OrderTrackingDto>> CreateOrder(string token, [FromBody] CreateOrderRequest req)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var (order, error) = await _orders.CreateOrderAsync(ct, req);
        if (order == null) return BadRequest(new { isSuccess = false, error });

        // F2.3: el motor de asignación tomará esta orden en WaitingRider automáticamente.

        var restaurant = order.RestaurantId.HasValue
            ? await _context.Restaurants.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == order.RestaurantId)
            : null;

        return Ok(OrderService.ToTracking(order, restaurant, null));
    }

    // ---- Mis pedidos + tracking ----
    [HttpGet("orders")]
    public async Task<ActionResult<IEnumerable<OrderTrackingDto>>> MyOrders(string token)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var orders = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.AssignedRider)
            .Where(o => o.CustomerTokenId == ct.Id)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return Ok(orders.Select(o => OrderService.ToTracking(o, o.Restaurant, o.AssignedRider)).ToList());
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<ActionResult<OrderTrackingDto>> Track(string token, Guid orderId)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var order = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.AssignedRider)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerTokenId == ct.Id);
        if (order == null) return NotFound(new { isSuccess = false, error = "Pedido no encontrado" });

        return Ok(OrderService.ToTracking(order, order.Restaurant, order.AssignedRider));
    }

    // ---- Cancelar (solo antes de que un rider recoja) ----
    [HttpPost("orders/{orderId:guid}/cancel")]
    public async Task<IActionResult> Cancel(string token, Guid orderId)
    {
        var ct = await ResolveActiveToken(token);
        if (ct == null) return NotFound(new { isSuccess = false, error = "Link inválido o expirado" });

        var order = await _context.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.AssignedRider)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.CustomerTokenId == ct.Id);
        if (order == null) return NotFound();

        if (order.Status is OrderStatus.PickedUp or OrderStatus.InTransit or OrderStatus.Delivered)
            return BadRequest(new { isSuccess = false, error = "El pedido ya está en camino y no puede cancelarse" });

        order.Status = OrderStatus.Cancelled;
        order.CancelReason = OrderCancelReason.CustomerCancelled;
        order.CancelledAt = DateTime.UtcNow;
        _orders.LogEvent(order.Id, order.TenantId, OrderActorType.Customer, "Cliente canceló el pedido");

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
