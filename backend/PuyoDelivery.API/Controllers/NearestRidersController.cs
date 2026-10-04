using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/riders")]
[Authorize(Roles = "CompanyAdmin")]
public class NearestRidersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;

    public NearestRidersController(ApplicationDbContext context, ICurrentTenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    [HttpGet("nearest")]
    public async Task<ActionResult<IEnumerable<RiderNearbyDto>>> GetNearest(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] double radiusKm = 10.0)
    {
        if (!_tenant.TenantId.HasValue)
            return Unauthorized();

        var location = new Point(lng, lat) { SRID = 4326 };

        var riders = await _context.Riders
            .FromSqlRaw(@"
                SELECT id, tenant_id, company_id, user_id, full_name, phone, vehicle_plate,
                       is_online, is_busy, fcm_token,
                       current_location, last_location_update,
                       ST_Y(current_location::geometry) as lat,
                       ST_X(current_location::geometry) as lng
                FROM riders
                WHERE tenant_id = {0}
                  AND is_online = true
                  AND is_busy = false
                  AND current_location IS NOT NULL
                  AND ST_DWithin(
                    current_location::geography,
                    ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography,
                    {3} * 1000
                  )
                ORDER BY current_location <-> ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography
                LIMIT 3",
                _tenant.TenantId.Value, lng, lat, radiusKm)
            .ToListAsync();

        var result = riders.Select(r => new RiderNearbyDto(
            r.Id,
            r.FullName,
            r.VehiclePlate,
            r.CurrentLocation != null ? r.CurrentLocation.Distance(location) / 1000.0 : 0,
            r.CurrentLocation?.Y ?? 0,
            r.CurrentLocation?.X ?? 0
        )).ToList();

        return Ok(result);
    }
}
