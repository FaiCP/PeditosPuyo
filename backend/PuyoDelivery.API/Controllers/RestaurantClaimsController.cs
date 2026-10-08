using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/restaurant-claims")]
public class RestaurantClaimsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public RestaurantClaimsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // ---- Público: restaurantes que se pueden reclamar (sin admin y sin reclamo pendiente) ----
    [HttpGet("claimable")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ClaimableRestaurantDto>>> Claimable()
    {
        var adminRestaurantIds = await _context.RestaurantAdmins
            .IgnoreQueryFilters()
            .Select(a => a.RestaurantId)
            .Distinct()
            .ToListAsync();

        var pendingRestaurantIds = await _context.RestaurantClaims
            .IgnoreQueryFilters()
            .Where(c => c.Status == RestaurantClaimStatus.Pending)
            .Select(c => c.RestaurantId)
            .Distinct()
            .ToListAsync();

        var restaurants = await _context.Restaurants
            .IgnoreQueryFilters()
            .Where(r => r.IsActive
                        && !adminRestaurantIds.Contains(r.Id)
                        && !pendingRestaurantIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => new ClaimableRestaurantDto(r.Id, r.Name, r.Address, r.Phone, r.LogoUrl))
            .ToListAsync();

        return Ok(restaurants);
    }

    // ---- Público: enviar solicitud de reclamo (crea cuenta placeholder) ----
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<RestaurantClaimDto>> Submit([FromBody] SubmitRestaurantClaimRequest request)
    {
        var restaurant = await _context.Restaurants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Id == request.RestaurantId && r.IsActive);
        if (restaurant == null) return NotFound(new { isSuccess = false, error = "Restaurante no encontrado" });

        var hasAdmin = await _context.RestaurantAdmins
            .IgnoreQueryFilters()
            .AnyAsync(a => a.RestaurantId == request.RestaurantId);
        if (hasAdmin)
            return BadRequest(new { isSuccess = false, error = "Este restaurante ya tiene un administrador" });

        var hasPendingClaim = await _context.RestaurantClaims
            .IgnoreQueryFilters()
            .AnyAsync(c => c.RestaurantId == request.RestaurantId && c.Status == RestaurantClaimStatus.Pending);
        if (hasPendingClaim)
            return BadRequest(new { isSuccess = false, error = "Este restaurante ya tiene una solicitud de reclamo pendiente" });

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { isSuccess = false, error = "El email ya está registrado" });

        // Crear usuario + restaurante placeholder bajo el tenant de la empresa para que pueda iniciar sesión.
        var company = await _context.DeliveryCompanies.IgnoreQueryFilters().FirstOrDefaultAsync();
        if (company == null)
            return BadRequest(new { isSuccess = false, error = "No existe una empresa configurada" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = "RestaurantAdmin",
            TenantId = company.TenantId
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return BadRequest(new { isSuccess = false, error = string.Join(", ", createResult.Errors.Select(e => e.Description)) });

        var placeholder = new Restaurant
        {
            TenantId = company.TenantId,
            Name = $"Pendiente - {request.FullName}",
            Slug = $"pendiente-{Guid.NewGuid():N}",
            Address = "",
            Phone = request.Phone,
            Location = new NetTopologySuite.Geometries.Point(0, 0) { SRID = 4326 },
            Source = RestaurantSource.Manual,
            IsActive = false
        };
        _context.Restaurants.Add(placeholder);
        await _context.SaveChangesAsync();

        _context.RestaurantAdmins.Add(new RestaurantAdmin
        {
            TenantId = company.TenantId,
            UserId = Guid.Parse(user.Id),
            RestaurantId = placeholder.Id,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone
        });

        var claim = new RestaurantClaim
        {
            TenantId = company.TenantId,
            RestaurantId = request.RestaurantId,
            ClaimantUserId = Guid.Parse(user.Id),
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Status = RestaurantClaimStatus.Pending
        };
        _context.RestaurantClaims.Add(claim);
        await _context.SaveChangesAsync();

        return Ok(new RestaurantClaimDto(
            claim.Id, claim.RestaurantId, restaurant.Name, claim.FullName, claim.Email,
            claim.Phone, claim.Status.ToString(), claim.SubmittedAt, null, null));
    }

    // ---- Admin empresa/super: listar reclamos pendientes del tenant ----
    [HttpGet("pending")]
    [Authorize(Roles = "CompanyAdmin,SuperAdmin")]
    public async Task<ActionResult<IEnumerable<RestaurantClaimDto>>> Pending()
    {
        var claims = await _context.RestaurantClaims
            .IgnoreQueryFilters()
            .Include(c => c.Restaurant)
            .Where(c => c.Status == RestaurantClaimStatus.Pending)
            .OrderBy(c => c.SubmittedAt)
            .Select(c => new RestaurantClaimDto(
                c.Id, c.RestaurantId, c.Restaurant.Name, c.FullName, c.Email,
                c.Phone, c.Status.ToString(), c.SubmittedAt, null, null))
            .ToListAsync();

        return Ok(claims);
    }

    // ---- Admin empresa/super: aprobar reclamo ----
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "CompanyAdmin,SuperAdmin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ResolveClaimRequest? request)
    {
        var claim = await _context.RestaurantClaims
            .IgnoreQueryFilters()
            .Include(c => c.Restaurant)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null) return NotFound();
        if (claim.Status != RestaurantClaimStatus.Pending)
            return BadRequest(new { isSuccess = false, error = "La solicitud ya fue resuelta" });

        var admin = await _context.RestaurantAdmins
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.UserId == claim.ClaimantUserId);
        if (admin == null)
            return BadRequest(new { isSuccess = false, error = "No se encontró el perfil del solicitante" });

        // Desactivar restaurante placeholder.
        var placeholder = await _context.Restaurants.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Id == admin.RestaurantId);
        if (placeholder != null) placeholder.IsActive = false;

        // Reasignar admin al restaurante reclamado.
        admin.RestaurantId = claim.RestaurantId;
        admin.TenantId = claim.Restaurant.TenantId;

        var user = await _userManager.FindByIdAsync(claim.ClaimantUserId.ToString()!);
        if (user != null) user.TenantId = claim.Restaurant.TenantId;

        claim.Status = RestaurantClaimStatus.Approved;
        claim.ResolvedAt = DateTime.UtcNow;
        claim.ResolvedByUserId = GetCurrentUserId();
        claim.AdminNotes = request?.Notes;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ---- Admin empresa/super: rechazar reclamo ----
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "CompanyAdmin,SuperAdmin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ResolveClaimRequest? request)
    {
        var claim = await _context.RestaurantClaims
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id);
        if (claim == null) return NotFound();
        if (claim.Status != RestaurantClaimStatus.Pending)
            return BadRequest(new { isSuccess = false, error = "La solicitud ya fue resuelta" });

        claim.Status = RestaurantClaimStatus.Rejected;
        claim.ResolvedAt = DateTime.UtcNow;
        claim.ResolvedByUserId = GetCurrentUserId();
        claim.AdminNotes = request?.Notes;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}

public record ClaimableRestaurantDto(Guid Id, string Name, string Address, string Phone, string? LogoUrl);
public record SubmitRestaurantClaimRequest(Guid RestaurantId, string FullName, string Email, string Phone, string Password);
public record RestaurantClaimDto(Guid Id, Guid RestaurantId, string RestaurantName, string FullName, string Email, string Phone, string Status, DateTime SubmittedAt, DateTime? ResolvedAt, string? AdminNotes);
public record ResolveClaimRequest(string? Notes);
