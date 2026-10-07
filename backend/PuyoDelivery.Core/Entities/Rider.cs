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

    /// <summary>Tarifa de carrera que cobra este rider en efectivo al cliente (Puyo: 1.50 - 2.50 USD).</summary>
    public decimal DeliveryFee { get; set; } = 2.00m;

    public DeliveryCompany Company { get; set; } = null!;
}
