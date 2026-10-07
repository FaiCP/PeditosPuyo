namespace PuyoDelivery.Core.Entities;

public enum RiderOfferStatus
{
    Pending,
    Accepted,
    Rejected,
    Expired
}

/// <summary>
/// Oferta de un pedido a un rider concreto. El motor de asignación crea hasta
/// N ofertas (una por rider, ordenadas por cercanía). Cada oferta expira si no se acepta.
/// </summary>
public class RiderOffer : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid RiderId { get; set; }
    public RiderOfferStatus Status { get; set; } = RiderOfferStatus.Pending;

    public int AttemptNumber { get; set; }          // 1..N
    public int NotifyCount { get; set; }            // notificaciones insistidas enviadas
    public DateTime? LastNotifiedAt { get; set; }   // para cadencia (40s rider)
    public DateTime ExpiresAt { get; set; }         // deadline de aceptación (2 min)
    public DateTime? RespondedAt { get; set; }
    public string? RejectionReason { get; set; }

    public Order Order { get; set; } = null!;
    public Rider Rider { get; set; } = null!;
}
