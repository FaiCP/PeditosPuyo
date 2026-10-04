using NetTopologySuite.Geometries;

namespace PuyoDelivery.Core.Entities;

public enum DeliveryRequestStatus
{
    Pending,
    Assigned,
    Accepted,
    InTransit,
    Delivered,
    Cancelled
}

public class DeliveryRequest : BaseEntity
{
    public Guid RestaurantId { get; set; }
    public Guid RequestedBy { get; set; }
    public DeliveryRequestStatus Status { get; set; } = DeliveryRequestStatus.Pending;
    public string DeliveryAddress { get; set; } = string.Empty;
    public Point DeliveryLocation { get; set; } = null!;
    public string? Notes { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
    public ICollection<OrderAssignment> Assignments { get; set; } = new List<OrderAssignment>();
}
