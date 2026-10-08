using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/restaurant/menu")]
[Authorize(Roles = "RestaurantAdmin")]
public class RestaurantMenuController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;

    public RestaurantMenuController(ApplicationDbContext context, ICurrentTenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    private async Task<RestaurantAdmin?> GetAdminAsync()
    {
        if (!_tenant.UserId.HasValue) return null;
        return await _context.RestaurantAdmins
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.UserId == _tenant.UserId.Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenu()
    {
        var admin = await GetAdminAsync();
        if (admin == null) return Unauthorized();

        var items = await _context.MenuItems
            .IgnoreQueryFilters()
            .Where(m => m.RestaurantId == admin.RestaurantId)
            .OrderBy(m => m.Name)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.IsActive, m.ImageUrl))
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost("items")]
    public async Task<ActionResult<MenuItemDto>> Create([FromBody] CreateMenuItemRequest request)
    {
        var admin = await GetAdminAsync();
        if (admin == null) return Unauthorized();

        var item = new MenuItem
        {
            TenantId = admin.TenantId,
            RestaurantId = admin.RestaurantId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            ImageUrl = request.ImageUrl
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        return Ok(new MenuItemDto(item.Id, item.Name, item.Description, item.Price, item.IsActive, item.ImageUrl));
    }

    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<MenuItemDto>> Update(Guid id, [FromBody] UpdateMenuItemRequest request)
    {
        var admin = await GetAdminAsync();
        if (admin == null) return Unauthorized();

        var item = await _context.MenuItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == admin.RestaurantId);
        if (item == null) return NotFound();

        item.Name = request.Name;
        item.Description = request.Description;
        item.Price = request.Price;
        item.IsActive = request.IsActive;
        item.ImageUrl = request.ImageUrl;

        await _context.SaveChangesAsync();
        return Ok(new MenuItemDto(item.Id, item.Name, item.Description, item.Price, item.IsActive, item.ImageUrl));
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var admin = await GetAdminAsync();
        if (admin == null) return Unauthorized();

        var item = await _context.MenuItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id && m.RestaurantId == admin.RestaurantId);
        if (item == null) return NotFound();

        _context.MenuItems.Remove(item);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
