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

var riders = await _context.Riders
            .FromSqlRaw(@"
                SELECT ""Id"", ""TenantId"", ""CompanyId"", ""UserId"", ""FullName"", ""Phone"", ""VehiclePlate"",
                       ""IsOnline"", ""IsBusy"", ""FcmToken"",
                       ""CurrentLocation"", ""LastLocationUpdate"", ""IsActive"", ""CreatedAt"", ""UpdatedAt""
                FROM ""Riders""
                WHERE ""TenantId"" = {0}
                  AND ""IsOnline"" = true
                  AND ""IsBusy"" = false
                  AND ""CurrentLocation"" IS NOT NULL
                  AND ST_DWithin(
                    ""CurrentLocation""::geography,
                    ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography,
                    {3} * 1000
                  )
                ORDER BY ""CurrentLocation"" <-> ST_SetSRID(ST_MakePoint({1}, {2}), 4326)::geography
                LIMIT 3",
                _tenant.TenantId.Value, lng, lat, radiusKm)
            .ToListAsync();

        var result = riders.Select(r => new RiderNearbyDto(
            r.Id,
            r.FullName,
            r.VehiclePlate,
            r.CurrentLocation != null ? HaversineKm(r.CurrentLocation.Y, r.CurrentLocation.X, lat, lng) : 0,
            r.CurrentLocation?.Y ?? 0,
            r.CurrentLocation?.X ?? 0
        )).ToList();

        return Ok(result);
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
