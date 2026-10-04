namespace PuyoDelivery.Core.Entities;

public class DeliveryCompany : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public decimal MonthlyRatePerDriver { get; set; } = 10.00m;
    public int RiderLimit { get; set; } = 10;
    public bool IsActive { get; set; } = true;

    public ICollection<CompanyAdmin> Admins { get; set; } = new List<CompanyAdmin>();
    public ICollection<Rider> Riders { get; set; } = new List<Rider>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
