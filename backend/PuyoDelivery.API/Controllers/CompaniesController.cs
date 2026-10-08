using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class CompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompaniesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CompanyDto>>> GetAll()
    {
        var companies = await _context.DeliveryCompanies
            .Select(c => new CompanyDto(c.Id, c.Name, c.Slug, c.MonthlyRatePerDriver, c.RiderLimit, c.IsActive))
            .ToListAsync();

        return Ok(companies);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CompanyDto>> GetById(Guid id)
    {
        var company = await _context.DeliveryCompanies.FindAsync(id);
        if (company == null) return NotFound();

        return Ok(new CompanyDto(company.Id, company.Name, company.Slug, company.MonthlyRatePerDriver, company.RiderLimit, company.IsActive));
    }

    [HttpPost]
    public async Task<ActionResult<CompanyDto>> Create([FromBody] CreateCompanyRequest request)
    {
        var exists = await _context.DeliveryCompanies.AnyAsync(c => c.Slug == request.Slug);
        if (exists) return BadRequest(new { isSuccess = false, error = "Slug already exists" });

        var company = new Core.Entities.DeliveryCompany
        {
            Name = request.Name,
            Slug = request.Slug,
            MonthlyRatePerDriver = request.MonthlyRatePerDriver,
            RiderLimit = request.RiderLimit,
            TenantId = Guid.NewGuid()
        };

        _context.DeliveryCompanies.Add(company);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = company.Id },
            new CompanyDto(company.Id, company.Name, company.Slug, company.MonthlyRatePerDriver, company.RiderLimit, company.IsActive));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _context.DeliveryCompanies.FindAsync(id);
        if (company == null) return NotFound();

        company.Name = request.Name;
        company.Slug = request.Slug;
        company.MonthlyRatePerDriver = request.MonthlyRatePerDriver;
        company.RiderLimit = request.RiderLimit;
        company.IsActive = request.IsActive;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var company = await _context.DeliveryCompanies.FindAsync(id);
        if (company == null) return NotFound();

        _context.DeliveryCompanies.Remove(company);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:guid}/admins")]
    public async Task<ActionResult<CompanyAdminDto>> CreateAdmin(Guid id, [FromBody] CreateCompanyAdminRequest request)
    {
        var company = await _context.DeliveryCompanies.FindAsync(id);
        if (company == null) return NotFound(new { isSuccess = false, error = "Empresa no encontrada" });

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { isSuccess = false, error = "El email ya está registrado" });

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            Phone = request.Phone,
            Role = "CompanyAdmin",
            TenantId = company.TenantId
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { isSuccess = false, error = string.Join(", ", result.Errors.Select(e => e.Description)) });

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
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAdminById), new { id = admin.Id },
            new CompanyAdminDto(admin.Id, user.Id, admin.CompanyId, admin.FullName, admin.Email, admin.Phone));
    }

    [HttpGet("admins/{id:guid}", Name = nameof(GetAdminById))]
    public async Task<ActionResult<CompanyAdminDto>> GetAdminById(Guid id)
    {
        var admin = await _context.CompanyAdmins.FindAsync(id);
        if (admin == null) return NotFound();
        return Ok(new CompanyAdminDto(admin.Id, admin.UserId.ToString(), admin.CompanyId, admin.FullName, admin.Email, admin.Phone));
    }
}
