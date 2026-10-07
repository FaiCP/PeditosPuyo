using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/customer-tokens")]
[Authorize(Roles = "CompanyAdmin")]
public class CustomerTokensController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentTenantService _tenant;
    private readonly IConfiguration _config;

    public CustomerTokensController(ApplicationDbContext context, ICurrentTenantService tenant, IConfiguration config)
    {
        _context = context;
        _tenant = tenant;
        _config = config;
    }

    [HttpPost]
    public async Task<ActionResult<CustomerTokenDto>> Create([FromBody] CreateCustomerTokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { isSuccess = false, error = "Phone es obligatorio" });

        if (!_tenant.CompanyId.HasValue)
            return BadRequest(new { isSuccess = false, error = "Company no encontrada en el token" });

        var token = GenerateUrlSafeToken();
        var entity = new CustomerToken
        {
            TenantId = _tenant.CompanyId.Value,
            Token = token,
            Phone = req.Phone.Trim(),
            Name = req.Name?.Trim(),
            CreatedById = _tenant.UserId ?? Guid.Empty,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsActive = true
        };

        _context.CustomerTokens.Add(entity);
        await _context.SaveChangesAsync();

        var baseUrl = _config["PublicAppBaseUrl"] ?? "https://peditos-puyo.vercel.app";
        return Ok(new CustomerTokenDto(entity.Id, entity.Token, entity.Phone, entity.Name, entity.ExpiresAt, $"{baseUrl}/p/{entity.Token}"));
    }

    private static string GenerateUrlSafeToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
