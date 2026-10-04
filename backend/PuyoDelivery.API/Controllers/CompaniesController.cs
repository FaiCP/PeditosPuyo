using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class CompaniesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public CompaniesController(ApplicationDbContext context)
    {
        _context = context;
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
}
