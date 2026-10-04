namespace PuyoDelivery.Core.Entities;

public enum SubscriptionStatus
{
    Active,
    Expired,
    Suspended,
    Cancelled
}

public class Subscription : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public int RiderLimit { get; set; }
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime ExpiresAt { get; set; }

    public DeliveryCompany Company { get; set; } = null!;
    public ICollection<CompanyPayment> Payments { get; set; } = new List<CompanyPayment>();
}
