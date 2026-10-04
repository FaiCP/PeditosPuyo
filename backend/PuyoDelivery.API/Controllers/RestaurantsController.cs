using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;

    public RestaurantsController(ApplicationDbContext context, ICurrentTenantService tenant)
    {
        _context = context;
        _tenant = tenant;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<RestaurantDto>>> GetAll()
    {
        var restaurants = await _context.Restaurants
            .IgnoreQueryFilters()
            .Where(r => r.IsActive)
            .Select(r => new RestaurantDto(
                r.Id, r.Name, r.Slug, r.Address, r.Phone,
                r.Location.Y, r.Location.X, r.MenuSummary, r.IsActive,
                r.Source.ToString()))
            .ToListAsync();

        return Ok(restaurants);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantDto>> GetById(Guid id)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        return Ok(new RestaurantDto(
            restaurant.Id, restaurant.Name, restaurant.Slug, restaurant.Address, restaurant.Phone,
            restaurant.Location.Y, restaurant.Location.X, restaurant.MenuSummary, restaurant.IsActive,
            restaurant.Source.ToString()));
    }

    [HttpGet("{id:guid}/menu")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<MenuItemDto>>> GetMenu(Guid id)
    {
        var menuItems = await _context.MenuItems
            .Where(m => m.RestaurantId == id && m.IsActive)
            .Select(m => new MenuItemDto(m.Id, m.Name, m.Description, m.Price, m.IsActive))
            .ToListAsync();

        return Ok(menuItems);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<ActionResult<RestaurantDto>> Create([FromBody] CreateRestaurantRequest request)
    {
        var tenantId = _tenant.TenantId ?? Guid.NewGuid();

        var restaurant = new Restaurant
        {
            TenantId = tenantId,
            Name = request.Name,
            Slug = request.Slug,
            Address = request.Address,
            Phone = request.Phone,
            Location = new NetTopologySuite.Geometries.Point(request.Lng, request.Lat) { SRID = 4326 },
            MenuSummary = request.MenuSummary,
            Source = RestaurantSource.Manual
        };

        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();

        return Ok(new RestaurantDto(
            restaurant.Id, restaurant.Name, restaurant.Slug, restaurant.Address, restaurant.Phone,
            request.Lat, request.Lng, restaurant.MenuSummary, restaurant.IsActive, restaurant.Source.ToString()));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRestaurantRequest request)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        restaurant.Name = request.Name;
        restaurant.Slug = request.Slug;
        restaurant.Address = request.Address;
        restaurant.Phone = request.Phone;
        restaurant.Location = new NetTopologySuite.Geometries.Point(request.Lng, request.Lat) { SRID = 4326 };
        restaurant.MenuSummary = request.MenuSummary;
        restaurant.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin")]
    public async Task<ActionResult<ImportResult>> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { isSuccess = false, error = "No file uploaded" });

        var tenantId = _tenant.TenantId ?? Guid.NewGuid();
        var errors = new List<string>();
        var imported = 0;

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            List<Dictionary<string, string>> rows;

            if (file.ContentType == "application/json" || file.FileName.EndsWith(".json"))
            {
                var restaurants = JsonSerializer.Deserialize<List<ImportRestaurantJson>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                rows = restaurants?.Select(r => new Dictionary<string, string>
                {
                    ["name"] = r.Name ?? "",
                    ["address"] = r.Address ?? "",
                    ["phone"] = r.Phone ?? "",
                    ["lat"] = r.Lat?.ToString() ?? "0",
                    ["lng"] = r.Lng?.ToString() ?? "0",
                    ["menuSummary"] = r.MenuSummary ?? ""
                }).ToList() ?? new List<Dictionary<string, string>>();
            }
            else
            {
                rows = ParseCsv(content);
            }

            foreach (var row in rows)
            {
                try
                {
                    if (!row.ContainsKey("name") || string.IsNullOrWhiteSpace(row["name"]))
                    {
                        errors.Add("Row missing name");
                        continue;
                    }

                    if (!double.TryParse(row.GetValueOrDefault("lat"), out var lat) ||
                        !double.TryParse(row.GetValueOrDefault("lng"), out var lng))
                    {
                        errors.Add($"Invalid lat/lng for {row["name"]}");
                        continue;
                    }

                    var slug = row["name"].ToLower().Replace(" ", "-");
                    var exists = await _context.Restaurants.AnyAsync(r => r.Slug == slug);
                    if (exists) continue;

                    var restaurant = new Restaurant
                    {
                        TenantId = tenantId,
                        Name = row["name"],
                        Slug = slug,
                        Address = row.GetValueOrDefault("address") ?? "",
                        Phone = row.GetValueOrDefault("phone") ?? "",
                        Location = new NetTopologySuite.Geometries.Point(lng, lat) { SRID = 4326 },
                        MenuSummary = row.GetValueOrDefault("menuSummary"),
                        Source = RestaurantSource.Scraper
                    };

                    _context.Restaurants.Add(restaurant);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"Error processing row: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            return BadRequest(new { isSuccess = false, error = $"Import failed: {ex.Message}" });
        }

        return Ok(new ImportResult(imported, errors));
    }

    [HttpPost("{id:guid}/menu-items")]
    [Authorize(Roles = "SuperAdmin,CompanyAdmin,RestaurantAdmin")]
    public async Task<ActionResult<MenuItemDto>> AddMenuItem(Guid id, [FromBody] CreateMenuItemRequest request)
    {
        var restaurant = await _context.Restaurants.FindAsync(id);
        if (restaurant == null) return NotFound();

        var menuItem = new MenuItem
        {
            TenantId = restaurant.TenantId,
            RestaurantId = id,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        _context.MenuItems.Add(menuItem);
        await _context.SaveChangesAsync();

        return Ok(new MenuItemDto(menuItem.Id, menuItem.Name, menuItem.Description, menuItem.Price, menuItem.IsActive));
    }

    private List<Dictionary<string, string>> ParseCsv(string content)
    {
        var rows = new List<Dictionary<string, string>>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length < 2) return rows;

        var headers = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();

        for (int i = 1; i < lines.Length; i++)
        {
            var values = lines[i].Split(',');
            var row = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length && j < values.Length; j++)
            {
                row[headers[j]] = values[j].Trim().Trim('"');
            }
            rows.Add(row);
        }

        return rows;
    }
}

public class ImportRestaurantJson
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public string? MenuSummary { get; set; }
}
