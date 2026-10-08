namespace PuyoDelivery.Core.Entities;

public class MenuItem : BaseEntity
{
    public Guid RestaurantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Opcional. URL de imagen del ítem.</summary>
    public string? ImageUrl { get; set; }

    public Restaurant Restaurant { get; set; } = null!;
}
