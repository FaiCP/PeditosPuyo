using NetTopologySuite.Geometries;

namespace PuyoDelivery.Core.Entities;

public enum RestaurantSource
{
    Osm,
    Manual,
    Scraper
}

public class Restaurant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public string? MenuSummary { get; set; }
    public bool IsActive { get; set; } = true;
    public RestaurantSource Source { get; set; } = RestaurantSource.Manual;
    public string? ExternalId { get; set; }

    /// <summary>Opcional. URL/imagen del código QR de pago. Si existe, el cliente puede pagar por transferencia; si no, solo efectivo.</summary>
    public string? PaymentQrUrl { get; set; }

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<RestaurantAdmin> Admins { get; set; } = new List<RestaurantAdmin>();
}
