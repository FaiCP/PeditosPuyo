using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeliveryRequestsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;
    private readonly IHubContext<RestaurantHub> _restaurantHub;
    private readonly IHubContext<CompanyHub> _companyHub;

    public DeliveryRequestsController(
        ApplicationDbContext context,
        ICurrentTenantService tenant,
        IHubContext<RestaurantHub> restaurantHub,
        IHubContext<CompanyHub> companyHub)
    {
        _context = context;
        _tenant = tenant;
        _restaurantHub = restaurantHub;
        _companyHub = companyHub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeliveryRequestDto>>> GetAll()
    {
        var query = _context.DeliveryRequests
            .IgnoreQueryFilters()
            .Include(r => r.Restaurant)
            .AsQueryable();

        // CompanyAdmin ve todas las requests; otros solo las de su tenant
        if (_tenant.Role != "CompanyAdmin")
        {
            query = query.Where(r => _tenant.TenantId == null || r.TenantId == _tenant.TenantId.Value);
        }

        var requests = await query
            .Select(r => new DeliveryRequestDto(
                r.Id,
                r.RestaurantId,
                r.Restaurant.Name,
                r.Status.ToString(),
                r.DeliveryAddress,
                r.DeliveryLocation.Y,
                r.DeliveryLocation.X,
                r.Notes,
                r.CreatedAt,
                r.AssignedAt,
                r.AcceptedAt,
                r.DeliveredAt))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "CompanyAdmin")]
    public async Task<ActionResult<IEnumerable<DeliveryRequestDto>>> GetPending()
    {
        var requests = await _context.DeliveryRequests
            .IgnoreQueryFilters()
            .Include(r => r.Restaurant)
            .Where(r => r.Status == DeliveryRequestStatus.Pending || r.Status == DeliveryRequestStatus.Assigned)
            .Select(r => new DeliveryRequestDto(
                r.Id,
                r.RestaurantId,
                r.Restaurant.Name,
                r.Status.ToString(),
                r.DeliveryAddress,
                r.DeliveryLocation.Y,
                r.DeliveryLocation.X,
                r.Notes,
                r.CreatedAt,
                r.AssignedAt,
                r.AcceptedAt,
                r.DeliveredAt))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DeliveryRequestDto>> GetById(Guid id)
    {
        var request = await _context.DeliveryRequests
            .IgnoreQueryFilters()
            .Include(r => r.Restaurant)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (request == null) return NotFound();

        return Ok(new DeliveryRequestDto(
            request.Id,
            request.RestaurantId,
            request.Restaurant.Name,
            request.Status.ToString(),
            request.DeliveryAddress,
            request.DeliveryLocation.Y,
            request.DeliveryLocation.X,
            request.Notes,
            request.CreatedAt,
            request.AssignedAt,
            request.AcceptedAt,
            request.DeliveredAt));
    }

    [HttpPost]
    [Authorize(Roles = "RestaurantAdmin")]
    public async Task<ActionResult<DeliveryRequestDto>> Create([FromBody] CreateDeliveryRequestRequest req)
    {
        var restaurant = await _context.Restaurants.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == req.RestaurantId);
        if (restaurant == null) return BadRequest(new { isSuccess = false, error = "Restaurant not found" });

        var deliveryRequest = new DeliveryRequest
        {
            TenantId = restaurant.TenantId,
            RestaurantId = req.RestaurantId,
            RequestedBy = _tenant.RestaurantId ?? Guid.Empty,
            Status = DeliveryRequestStatus.Pending,
            DeliveryAddress = req.DeliveryAddress,
            DeliveryLocation = new NetTopologySuite.Geometries.Point(req.Lng, req.Lat) { SRID = 4326 },
            Notes = req.Notes
        };

        _context.DeliveryRequests.Add(deliveryRequest);
        await _context.SaveChangesAsync();

        // Notificar por SignalR al grupo de la empresa
        await _companyHub.Clients.Group($"company-{restaurant.TenantId}").SendAsync("newDeliveryRequest", new
        {
            requestId = deliveryRequest.Id,
            restaurantName = restaurant.Name,
            address = req.DeliveryAddress,
            createdAt = deliveryRequest.CreatedAt
        });

        // Notificar al restaurante
        await _restaurantHub.Clients.Group($"restaurant-{req.RestaurantId}").SendAsync("requestCreated", new
        {
            requestId = deliveryRequest.Id,
            status = deliveryRequest.Status.ToString()
        });

        return Ok(new DeliveryRequestDto(
            deliveryRequest.Id, restaurant.Id, restaurant.Name, deliveryRequest.Status.ToString(),
            deliveryRequest.DeliveryAddress, req.Lat, req.Lng, deliveryRequest.Notes,
            deliveryRequest.CreatedAt, null, null, null));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "CompanyAdmin,RestaurantAdmin")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        var deliveryRequest = await _context.DeliveryRequests.FindAsync(id);
        if (deliveryRequest == null) return NotFound();

        var newStatus = req.Status.ToUpper() switch
        {
            "ASSIGNED" => DeliveryRequestStatus.Assigned,
            "ACCEPTED" => DeliveryRequestStatus.Accepted,
            "IN_TRANSIT" => DeliveryRequestStatus.InTransit,
            "DELIVERED" => DeliveryRequestStatus.Delivered,
            "CANCELLED" => DeliveryRequestStatus.Cancelled,
            _ => deliveryRequest.Status
        };

        deliveryRequest.Status = newStatus;
        if (newStatus == DeliveryRequestStatus.Assigned) deliveryRequest.AssignedAt = DateTime.UtcNow;
        if (newStatus == DeliveryRequestStatus.Accepted) deliveryRequest.AcceptedAt = DateTime.UtcNow;
        if (newStatus == DeliveryRequestStatus.Delivered) deliveryRequest.DeliveredAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Notificar cambio de estado
        await _restaurantHub.Clients.Group($"restaurant-{deliveryRequest.RestaurantId}").SendAsync("requestStatusChanged", new
        {
            requestId = id,
            status = newStatus.ToString()
        });

        return NoContent();
    }
}
