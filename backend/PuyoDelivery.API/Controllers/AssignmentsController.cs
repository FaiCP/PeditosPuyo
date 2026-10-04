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
public class AssignmentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;
    private readonly IHubContext<RiderHub> _riderHub;
    private readonly IHubContext<RestaurantHub> _restaurantHub;

    public AssignmentsController(
        ApplicationDbContext context,
        ICurrentTenantService tenant,
        IHubContext<RiderHub> riderHub,
        IHubContext<RestaurantHub> restaurantHub)
    {
        _context = context;
        _tenant = tenant;
        _riderHub = riderHub;
        _restaurantHub = restaurantHub;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetAll()
    {
        var assignments = await _context.OrderAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Rider)
            .Include(a => a.Request).ThenInclude(r => r.Restaurant)
            .Select(a => new AssignmentDto(
                a.Id, a.RequestId, a.RiderId, a.Rider.FullName,
                a.Request.Restaurant.Name, a.Status.ToString(), a.RejectionReason,
                a.CreatedAt, a.AcceptedAt, a.RejectedAt, a.DeliveredAt))
            .ToListAsync();
        return Ok(assignments);
    }

    [HttpGet("rider/pending")]
    [Authorize(Roles = "Rider")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetRiderPending()
    {
        var assignments = await _context.OrderAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Rider)
            .Include(a => a.Request).ThenInclude(r => r.Restaurant)
            .Where(a => a.RiderId == _tenant.RiderId && a.Status == AssignmentStatus.Pending)
            .Select(a => new AssignmentDto(
                a.Id, a.RequestId, a.RiderId, a.Rider.FullName,
                a.Request.Restaurant.Name, a.Status.ToString(), a.RejectionReason,
                a.CreatedAt, a.AcceptedAt, a.RejectedAt, a.DeliveredAt))
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpGet("rider/active")]
    [Authorize(Roles = "Rider")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetRiderActive()
    {
        var assignments = await _context.OrderAssignments
            .IgnoreQueryFilters()
            .Include(a => a.Rider)
            .Include(a => a.Request).ThenInclude(r => r.Restaurant)
            .Where(a => a.RiderId == _tenant.RiderId &&
                       (a.Status == AssignmentStatus.Accepted || a.Status == AssignmentStatus.InTransit))
            .Select(a => new AssignmentDto(
                a.Id, a.RequestId, a.RiderId, a.Rider.FullName,
                a.Request.Restaurant.Name, a.Status.ToString(), a.RejectionReason,
                a.CreatedAt, a.AcceptedAt, a.RejectedAt, a.DeliveredAt))
            .ToListAsync();

        return Ok(assignments);
    }

    [HttpPost]
    [Authorize(Roles = "CompanyAdmin")]
    public async Task<ActionResult<AssignmentDto>> Create([FromBody] CreateAssignmentRequest req)
    {
        var request = await _context.DeliveryRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == req.RequestId);
        if (request == null) return NotFound(new { isSuccess = false, error = "Delivery request not found" });

        var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == req.RiderId);
        if (rider == null) return NotFound(new { isSuccess = false, error = "Rider not found" });

        if (rider.IsBusy)
            return BadRequest(new { isSuccess = false, error = "Rider is busy", code = "RIDER_BUSY" });

        var existingAssignment = await _context.OrderAssignments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.RequestId == req.RequestId && a.Status != AssignmentStatus.Rejected);

        if (existingAssignment)
            return BadRequest(new { isSuccess = false, error = "Request already assigned", code = "ALREADY_ASSIGNED" });

        var assignment = new OrderAssignment
        {
            TenantId = request.TenantId,
            RequestId = req.RequestId,
            RiderId = req.RiderId,
            AssignedBy = _tenant.UserId ?? Guid.Empty,
            Status = AssignmentStatus.Pending
        };

        // Actualizar estado de la request
        request.Status = DeliveryRequestStatus.Assigned;
        request.AssignedAt = DateTime.UtcNow;

        // Marcar rider como busy
        rider.IsBusy = true;

        _context.OrderAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        // Obtener datos para la notificación
        var restaurant = await _context.Restaurants.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == request.RestaurantId);

        // Notificar al rider por SignalR
        await _riderHub.Clients.Group($"rider-{req.RiderId}").SendAsync("newAssignment", new
        {
            assignmentId = assignment.Id,
            requestId = request.Id,
            restaurantName = restaurant?.Name ?? "",
            pickupAddress = restaurant?.Address ?? "",
            deliveryAddress = request.DeliveryAddress,
            distanceKm = 0 // Calcular con PostGIS en el futuro
        });

        // Notificar al restaurante
        await _restaurantHub.Clients.Group($"restaurant-{request.RestaurantId}").SendAsync("riderAssigned", new
        {
            requestId = request.Id,
            riderName = rider.FullName,
            vehiclePlate = rider.VehiclePlate,
            etaMinutes = 0
        });

        return Ok(new AssignmentDto(
            assignment.Id, request.Id, rider.Id, rider.FullName,
            restaurant?.Name ?? "", assignment.Status.ToString(), null,
            assignment.CreatedAt, null, null, null));
    }

    [HttpPut("{id:guid}/accept")]
    [Authorize(Roles = "Rider")]
    public async Task<ActionResult<AssignmentDto>> Accept(Guid id)
    {
        var assignment = await _context.OrderAssignments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (assignment.RiderId != _tenant.RiderId) return Forbid();

        assignment.Status = AssignmentStatus.Accepted;
        assignment.AcceptedAt = DateTime.UtcNow;

        var request = await _context.DeliveryRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == assignment.RequestId);
        if (request != null)
        {
            request.Status = DeliveryRequestStatus.Accepted;
            request.AcceptedAt = DateTime.UtcNow;

            await _restaurantHub.Clients.Group($"restaurant-{request.RestaurantId}").SendAsync("requestStatusChanged", new
            {
                requestId = request.Id,
                status = "ACCEPTED"
            });
        }

        await _context.SaveChangesAsync();

        return Ok(new AssignmentDto(
            assignment.Id, assignment.RequestId, assignment.RiderId, "",
            "", assignment.Status.ToString(), null,
            assignment.CreatedAt, assignment.AcceptedAt, null, null));
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Rider")]
    public async Task<ActionResult<AssignmentDto>> Reject(Guid id, [FromBody] RejectAssignmentRequest req)
    {
        var assignment = await _context.OrderAssignments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (assignment.RiderId != _tenant.RiderId) return Forbid();

        assignment.Status = AssignmentStatus.Rejected;
        assignment.RejectedAt = DateTime.UtcNow;
        assignment.RejectionReason = req.Reason;

        var request = await _context.DeliveryRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == assignment.RequestId);
        if (request != null)
        {
            request.Status = DeliveryRequestStatus.Pending;
            request.AssignedAt = null;
        }

        // Liberar rider
        var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == assignment.RiderId);
        if (rider != null) rider.IsBusy = false;

        await _context.SaveChangesAsync();

        return Ok(new AssignmentDto(
            assignment.Id, assignment.RequestId, assignment.RiderId, "",
            "", assignment.Status.ToString(), assignment.RejectionReason,
            assignment.CreatedAt, null, assignment.RejectedAt, null));
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Rider")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest req)
    {
        var assignment = await _context.OrderAssignments.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();
        if (assignment.RiderId != _tenant.RiderId) return Forbid();

        var newStatus = req.Status.ToUpper() switch
        {
            "IN_TRANSIT" => AssignmentStatus.InTransit,
            "DELIVERED" => AssignmentStatus.Delivered,
            _ => assignment.Status
        };

        assignment.Status = newStatus;
        if (newStatus == AssignmentStatus.Delivered)
        {
            assignment.DeliveredAt = DateTime.UtcNow;

            // Liberar rider
            var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == assignment.RiderId);
            if (rider != null) rider.IsBusy = false;

            // Actualizar request
            var request = await _context.DeliveryRequests.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == assignment.RequestId);
            if (request != null)
            {
                request.Status = DeliveryRequestStatus.Delivered;
                request.DeliveredAt = DateTime.UtcNow;

                await _restaurantHub.Clients.Group($"restaurant-{request.RestaurantId}").SendAsync("requestDelivered", new
                {
                    requestId = request.Id
                });
            }
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
}
