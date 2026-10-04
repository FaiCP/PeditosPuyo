namespace PuyoDelivery.Core.Entities;

public class CompanyAdmin : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DeliveryCompany Company { get; set; } = null!;
}
