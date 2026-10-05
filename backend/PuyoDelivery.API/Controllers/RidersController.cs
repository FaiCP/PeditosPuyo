using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RidersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;
    private readonly UserManager<ApplicationUser> _userManager;

    public RidersController(ApplicationDbContext context, ICurrentTenantService tenant, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _tenant = tenant;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RiderDto>>> GetAll()
    {
        var riders = await _context.Riders
            .Where(r => r.TenantId == _tenant.TenantId)
            .Select(r => new RiderDto(
                r.Id, r.FullName, r.Phone, r.VehiclePlate, r.IsOnline, r.IsBusy, r.IsActive,
                r.CurrentLocation != null ? r.CurrentLocation.Y : (double?)null,
                r.CurrentLocation != null ? r.CurrentLocation.X : (double?)null,
                r.LastLocationUpdate))
            .ToListAsync();

        return Ok(riders);
    }

    [HttpGet("online")]
    public async Task<ActionResult<IEnumerable<RiderDto>>> GetOnline()
    {
        var riders = await _context.Riders
            .Where(r => r.TenantId == _tenant.TenantId && r.IsOnline && r.IsActive)
            .Select(r => new RiderDto(
                r.Id, r.FullName, r.Phone, r.VehiclePlate, r.IsOnline, r.IsBusy, r.IsActive,
                r.CurrentLocation != null ? r.CurrentLocation.Y : (double?)null,
                r.CurrentLocation != null ? r.CurrentLocation.X : (double?)null,
                r.LastLocationUpdate))
            .ToListAsync();

        return Ok(riders);
    }

    [HttpPost]
    [Authorize(Roles = "CompanyAdmin")]
    public async Task<ActionResult<RiderDto>> Create([FromBody] CreateRiderRequest request)
    {
        if (!_tenant.TenantId.HasValue || !_tenant.CompanyId.HasValue)
            return BadRequest(new { isSuccess = false, error = "No company tenant" });

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { isSuccess = false, error = "Email already registered" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = "Rider",
            TenantId = _tenant.TenantId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { isSuccess = false, error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        var rider = new Core.Entities.Rider
        {
            TenantId = _tenant.TenantId!.Value,
            CompanyId = _tenant.CompanyId!.Value,
            UserId = Guid.Parse(user.Id),
            FullName = request.FullName,
            Phone = request.Phone,
            VehiclePlate = request.VehiclePlate
        };

        _context.Riders.Add(rider);
        await _context.SaveChangesAsync();

        return Ok(new RiderDto(rider.Id, rider.FullName, rider.Phone, rider.VehiclePlate, rider.IsOnline, rider.IsBusy, rider.IsActive, null, null, null));
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] RiderStatusRequest request)
    {
        var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (rider == null) return NotFound();

        if (_tenant.Role == "Rider" && _tenant.RiderId != id) return Forbid();
        if (_tenant.Role == "CompanyAdmin" && rider.TenantId != _tenant.TenantId) return Forbid();

        rider.IsOnline = request.IsOnline;
        if (request.IsOnline)
            rider.LastLocationUpdate = DateTime.UtcNow;
        else
            rider.CurrentLocation = null;

        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{id:guid}/location")]
    public async Task<IActionResult> UpdateLocation(Guid id, [FromBody] UpdateLocationRequest request)
    {
        var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == id);
        if (rider == null) return NotFound();

        if (_tenant.Role == "Rider" && _tenant.RiderId != id) return Forbid();
        if (_tenant.Role == "CompanyAdmin" && rider.TenantId != _tenant.TenantId) return Forbid();

        rider.CurrentLocation = new NetTopologySuite.Geometries.Point(request.Lng, request.Lat) { SRID = 4326 };
        rider.LastLocationUpdate = DateTime.UtcNow;
        rider.IsOnline = true;

        await _context.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRiderRequest request)
    {
        var rider = await _context.Riders.FindAsync(id);
        if (rider == null) return NotFound();
        if (rider.TenantId != _tenant.TenantId) return Forbid();

        rider.FullName = request.FullName;
        rider.Phone = request.Phone;
        rider.VehiclePlate = request.VehiclePlate;
        rider.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var rider = await _context.Riders.FindAsync(id);
        if (rider == null) return NotFound();
        if (rider.TenantId != _tenant.TenantId) return Forbid();

        _context.Riders.Remove(rider);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}

[ApiController]
[Route("api/driver")]
[Authorize(Roles = "Rider")]
public class DriverController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;

    public DriverController(ApplicationDbContext context, ICurrentTenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationRequest request)
    {
        var rider = await _context.Riders.FindAsync(_tenant.RiderId);
        if (rider == null) return NotFound();

        rider.CurrentLocation = new NetTopologySuite.Geometries.Point(request.Lng, request.Lat) { SRID = 4326 };
        rider.LastLocationUpdate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
