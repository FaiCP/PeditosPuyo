using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/restaurant/profile")]
[Authorize(Roles = "RestaurantAdmin")]
public class RestaurantProfileController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;

    public RestaurantProfileController(ApplicationDbContext context, ICurrentTenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    private async Task<RestaurantAdmin?> GetAdminAsync()
    {
        if (!_tenant.UserId.HasValue) return null;
        return await _context.RestaurantAdmins
            .IgnoreQueryFilters()
            .Include(a => a.Restaurant)
            .FirstOrDefaultAsync(a => a.UserId == _tenant.UserId.Value);
    }

    [HttpGet]
    public async Task<ActionResult<RestaurantDto>> Get()
    {
        var admin = await GetAdminAsync();
        if (admin?.Restaurant == null) return NotFound();

        var r = admin.Restaurant;
        return Ok(new RestaurantDto(
            r.Id, r.Name, r.Slug, r.Address, r.Phone,
            r.Location.Y, r.Location.X, r.MenuSummary, r.IsActive,
            r.Source.ToString(), r.LogoUrl));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateRestaurantProfileRequest request)
    {
        var admin = await GetAdminAsync();
        if (admin?.Restaurant == null) return NotFound();

        var r = admin.Restaurant;
        if (!string.IsNullOrWhiteSpace(request.Name)) r.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Address)) r.Address = request.Address.Trim();
        if (!string.IsNullOrWhiteSpace(request.Phone)) r.Phone = request.Phone.Trim();
        if (!string.IsNullOrWhiteSpace(request.MenuSummary)) r.MenuSummary = request.MenuSummary.Trim();
        if (request.LogoUrl != null) r.LogoUrl = request.LogoUrl.Trim();
        if (request.PaymentQrUrl != null) r.PaymentQrUrl = request.PaymentQrUrl.Trim();

        await _context.SaveChangesAsync();
        return NoContent();
    }
}

public record UpdateRestaurantProfileRequest(string? Name, string? Address, string? Phone, string? MenuSummary, string? LogoUrl, string? PaymentQrUrl);
