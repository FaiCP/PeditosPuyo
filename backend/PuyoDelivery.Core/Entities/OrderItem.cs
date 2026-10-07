namespace PuyoDelivery.Core.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }

    /// <summary>MenuItemId si es un plato del catálogo; null si es artículo de compra libre.</summary>
    public Guid? MenuItemId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public MenuItem? MenuItem { get; set; }
}
