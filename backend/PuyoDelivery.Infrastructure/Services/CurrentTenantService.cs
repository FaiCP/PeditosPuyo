using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService, ICurrentTenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? TenantId => GetClaimGuid("tenant_id");
    public Guid? UserId => GetClaimGuid("sub");
    public string? Role => User?.FindFirst("role")?.Value;
    public Guid? CompanyId => GetClaimGuid("company_id");
    public Guid? RestaurantId => GetClaimGuid("restaurant_id");
    public Guid? RiderId => GetClaimGuid("rider_id");

    private Guid? GetClaimGuid(string claimType)
    {
        var value = User?.FindFirst(claimType)?.Value;
        return Guid.TryParse(value, out var guid) ? guid : null;
    }
}
