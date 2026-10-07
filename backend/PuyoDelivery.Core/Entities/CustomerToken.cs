namespace PuyoDelivery.Core.Entities;

/// <summary>
/// Acceso sin contraseña del cliente final. La company genera un link con este token.
/// Un mismo token puede crear varios pedidos.
/// </summary>
public class CustomerToken : BaseEntity
{
    public string Token { get; set; } = string.Empty; // url-safe, único
    public string Phone { get; set; } = string.Empty;
    public string? Name { get; set; }
    public Guid CreatedById { get; set; } // company admin que lo generó
    public DateTime ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
