using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize(Roles = "CompanyAdmin")]
public class CompanyOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentTenantService _tenant;

    public CompanyOrdersController(ApplicationDbContext db, ICurrentTenantService tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderTrackingDto>>> List([FromQuery] string? status)
    {
        var query = _db.Orders
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .Include(o => o.Restaurant)
            .Include(o => o.AssignedRider)
            .Where(o => o.TenantId == _tenant.TenantId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<OrderStatus>(status, true, out var st))
            query = query.Where(o => o.Status == st);

        var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        return Ok(orders.Select(o => OrderService.ToTracking(o, o.Restaurant, o.AssignedRider)).ToList());
    }

    [HttpPost("{orderId:guid}/retry-assign")]
    public async Task<IActionResult> RetryAssign(Guid orderId)
    {
        var order = await _db.Orders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == _tenant.TenantId);
        if (order == null) return NotFound();

        if (order.Status != OrderStatus.OnHold)
            return BadRequest(new { isSuccess = false, error = "Solo se reintentan pedidos en espera" });

        order.Status = OrderStatus.WaitingRider; // el motor volverá a ofertar en próximos ticks
        _db.OrderEvents.Add(new OrderEvent
        {
            OrderId = order.Id, TenantId = order.TenantId,
            ActorType = OrderActorType.CompanyAdmin, ActorId = _tenant.UserId,
            Description = "Company reactivó la búsqueda de rider"
        });
        await _db.SaveChangesAsync();
        return Ok(new { status = order.Status.ToString() });
    }

    [HttpGet("{orderId:guid}/events")]
    public async Task<ActionResult<IEnumerable<OrderEventDto>>> Events(Guid orderId)
    {
        var order = await _db.Orders.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == _tenant.TenantId);
        if (order == null) return NotFound();

        var events = await _db.OrderEvents.IgnoreQueryFilters()
            .Where(e => e.OrderId == orderId)
            .OrderBy(e => e.CreatedAt)
            .Select(e => new OrderEventDto(e.CreatedAt, e.ActorType.ToString(), e.Description))
            .ToListAsync();
        return Ok(events);
    }
}
