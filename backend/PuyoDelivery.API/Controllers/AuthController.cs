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

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = request.Role
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { isSuccess = false, error = string.Join(", ", result.Errors.Select(e => e.Description)) });

        Guid? companyId = null;
        Guid? restaurantId = null;
        Guid? riderId = null;

        // Crear perfil según rol
        if (request.Role == "CompanyAdmin")
        {
            var company = new DeliveryCompany
            {
                TenantId = Guid.NewGuid(),
                Name = $"{request.FullName}'s Company",
                Slug = request.Email.Split('@')[0].ToLower()
            };
            _context.DeliveryCompanies.Add(company);
            await _context.SaveChangesAsync();

            var admin = new CompanyAdmin
            {
                TenantId = company.TenantId,
                UserId = Guid.Parse(user.Id),
                CompanyId = company.Id,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone
            };
            _context.CompanyAdmins.Add(admin);
            user.TenantId = company.TenantId;
            companyId = company.Id;

            await _userManager.UpdateAsync(user);
        }
        else if (request.Role == "RestaurantAdmin")
        {
            var restaurant = new Restaurant
            {
                TenantId = Guid.NewGuid(),
                Name = $"{request.FullName}'s Restaurant",
                Slug = request.Email.Split('@')[0].ToLower(),
                Address = "",
                Phone = request.Phone,
                Location = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 }
            };
            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            var admin = new RestaurantAdmin
            {
                TenantId = restaurant.TenantId,
                UserId = Guid.Parse(user.Id),
                RestaurantId = restaurant.Id,
                FullName = request.FullName,
                Email = request.Email,
                Phone = request.Phone
            };
            _context.RestaurantAdmins.Add(admin);
            user.TenantId = restaurant.TenantId;
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
}
