namespace PuyoDelivery.Core.Entities;

public enum PaymentMethod
{
    Transfer,
    Ticket
}

public enum PaymentStatus
{
    Pending,
    Confirmed,
    Rejected
}

public class CompanyPayment : BaseEntity
{
    public Guid SubscriptionId { get; set; }
    public Guid CompanyId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; }
    public string Reference { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public DeliveryCompany Company { get; set; } = null!;
}
