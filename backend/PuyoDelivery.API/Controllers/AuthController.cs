using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly ApplicationDbContext _context;

    public AuthController(UserManager<ApplicationUser> userManager, JwtTokenGenerator jwtTokenGenerator, ApplicationDbContext context)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
        _context = context;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { isSuccess = false, error = "Invalid credentials" });

        // Buscar perfil adicional según rol (ignorar query filters durante login)
        Guid? companyId = null;
        Guid? restaurantId = null;
        Guid? riderId = null;

        if (user.Role == "CompanyAdmin")
        {
            var admin = await _context.CompanyAdmins.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.UserId.ToString() == user.Id);
            companyId = admin?.CompanyId;
        }
        else if (user.Role == "RestaurantAdmin")
        {
            var admin = await _context.RestaurantAdmins.IgnoreQueryFilters().FirstOrDefaultAsync(a => a.UserId.ToString() == user.Id);
            restaurantId = admin?.RestaurantId;
        }
        else if (user.Role == "Rider")
        {
            var rider = await _context.Riders.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.UserId.ToString() == user.Id);
            riderId = rider?.Id;
        }

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id, user.Email!, user.Role, user.TenantId, companyId, restaurantId, riderId);

        return Ok(new LoginResponse(token, user.Id, user.Role, user.TenantId, user.FullName, user.Email!, companyId, restaurantId, riderId));
    }

    private static readonly string[] AllowedSelfRegisterRoles = { "RestaurantAdmin", "Rider" };

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        var role = request.Role?.Trim() ?? string.Empty;
        if (!AllowedSelfRegisterRoles.Contains(role))
            return BadRequest(new { isSuccess = false, error = "Rol no válido para registro público" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = role
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { isSuccess = false, error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        Guid? companyId = null;
        Guid? restaurantId = null;
        Guid? riderId = null;

        // Crear perfil según rol
        if (request.Role == "RestaurantAdmin")
        {
            var company = await _context.DeliveryCompanies.IgnoreQueryFilters().FirstOrDefaultAsync();
            if (company == null)
                return BadRequest(new { isSuccess = false, error = "No existe una empresa configurada en la plataforma" });

            var restaurantName = !string.IsNullOrWhiteSpace(request.RestaurantName)
                ? request.RestaurantName.Trim()
                : $"{request.FullName}'s Restaurant";
            var restaurantAddress = !string.IsNullOrWhiteSpace(request.RestaurantAddress)
                ? request.RestaurantAddress.Trim()
                : "";
            var restaurantPhone = !string.IsNullOrWhiteSpace(request.RestaurantPhone)
                ? request.RestaurantPhone.Trim()
                : request.Phone;

            if (string.IsNullOrWhiteSpace(restaurantName) || string.IsNullOrWhiteSpace(restaurantAddress) || string.IsNullOrWhiteSpace(restaurantPhone))
                return BadRequest(new { isSuccess = false, error = "Nombre, dirección y teléfono del restaurante son obligatorios" });

            var baseSlug = Slugify(restaurantName);
            var slug = baseSlug;
            var suffix = 2;
            while (await _context.Restaurants.IgnoreQueryFilters().AnyAsync(r => r.Slug == slug))
            {
                slug = $"{baseSlug}-{suffix++}";
            }

            var restaurant = new Restaurant
            {
                TenantId = company.TenantId,
                Name = restaurantName,
                Slug = slug,
                Address = restaurantAddress,
                Phone = restaurantPhone,
                LogoUrl = request.LogoUrl,
                Location = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 },
                Source = RestaurantSource.Manual
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            if (request.InitialMenuItems?.Count > 0)
            {
                foreach (var item in request.InitialMenuItems)
                {
                    if (string.IsNullOrWhiteSpace(item.Name)) continue;
                    _context.MenuItems.Add(new MenuItem
                    {
                        TenantId = company.TenantId,
                        RestaurantId = restaurant.Id,
                        Name = item.Name.Trim(),
                        Description = item.Description,
                        Price = item.Price,
                        ImageUrl = item.ImageUrl
                    });
                }
            }

            var admin = new RestaurantAdmin
            {
                TenantId = company.TenantId,
                UserId = Guid.Parse(user.Id),
                RestaurantId = restaurant.Id,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone
            };
            _context.RestaurantAdmins.Add(admin);
            user.TenantId = company.TenantId;
            restaurantId = restaurant.Id;

            await _userManager.UpdateAsync(user);
        }
        else if (request.Role == "Rider")
        {
            // Buscar o crear una empresa por defecto para riders (ignorar query filters)
            var company = await _context.DeliveryCompanies.IgnoreQueryFilters().FirstOrDefaultAsync();
            if (company == null)
            {
                company = new DeliveryCompany
                {
                    TenantId = Guid.NewGuid(),
                    Name = "Puyo Delivery",
                    Slug = "puyo-delivery"
                };
                _context.DeliveryCompanies.Add(company);
                await _context.SaveChangesAsync();
            }

            var rider = new Rider
            {
                TenantId = company.TenantId,
                CompanyId = company.Id,
                UserId = Guid.Parse(user.Id),
                FullName = request.FullName,
                Phone = request.Phone,
                VehiclePlate = ""
            };
            _context.Riders.Add(rider);
            user.TenantId = company.TenantId;
            riderId = rider.Id;

            await _userManager.UpdateAsync(user);
        }

        await _context.SaveChangesAsync();

        var token = _jwtTokenGenerator.GenerateToken(
            user.Id, user.Email!, user.Role, user.TenantId, companyId, restaurantId, riderId);

        return Ok(new LoginResponse(token, user.Id, user.Role, user.TenantId, user.FullName, user.Email!, companyId, restaurantId, riderId));
    }

    private static string Slugify(string value) =>
        value.Trim().ToLowerInvariant().Replace(" ", "-");
}
