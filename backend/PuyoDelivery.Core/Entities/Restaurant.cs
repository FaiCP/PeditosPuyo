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

    public ICollection<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    public ICollection<RestaurantAdmin> Admins { get; set; } = new List<RestaurantAdmin>();
    public ICollection<DeliveryRequest> DeliveryRequests { get; set; } = new List<DeliveryRequest>();
}
