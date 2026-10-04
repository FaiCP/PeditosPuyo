namespace PuyoDelivery.Core.Entities;

public enum AssignmentStatus
{
    Pending,
    Accepted,
    Rejected,
    InTransit,
    Delivered
}

public class OrderAssignment : BaseEntity
{
    public Guid RequestId { get; set; }
    public Guid RiderId { get; set; }
    public Guid AssignedBy { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Pending;
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public DeliveryRequest Request { get; set; } = null!;
    public Rider Rider { get; set; } = null!;
}
