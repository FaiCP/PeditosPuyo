namespace PuyoDelivery.Core.Interfaces;

public interface ICurrentTenantService
{
    Guid? TenantId { get; }
    Guid? UserId { get; }
    string? Role { get; }
    Guid? CompanyId { get; }
    Guid? RestaurantId { get; }
    Guid? RiderId { get; }
}
