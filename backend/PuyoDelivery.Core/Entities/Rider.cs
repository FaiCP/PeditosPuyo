using NetTopologySuite.Geometries;

namespace PuyoDelivery.Core.Entities;

public class Rider : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string VehiclePlate { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public bool IsBusy { get; set; }
    public string? FcmToken { get; set; }
    public Point? CurrentLocation { get; set; }
    public DateTime? LastLocationUpdate { get; set; }
    public bool IsActive { get; set; } = true;

    public DeliveryCompany Company { get; set; } = null!;
    public ICollection<OrderAssignment> Assignments { get; set; } = new List<OrderAssignment>();
}
