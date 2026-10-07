using NetTopologySuite.Geometries;

namespace PuyoDelivery.Core.Entities;

public enum OrderType
{
    /// <summary>Pedido de un restaurante del catálogo (usa menú y precios).</summary>
    Restaurant,

    /// <summary>Compra personalizada: el rider compra artículos y el cliente reembolsa.</summary>
    Compra,

    /// <summary>Encargo: llevar/traer un paquete, sin compra de producto.</summary>
    Encargo
}

public enum OrderStatus
{
    Draft,
    WaitingRider,
    RiderAccepted,
    ReadyForPickup,
    PickedUp,
    InTransit,
    Delivered,
    OnHold,
    Cancelled
}

public enum OrderPaymentMethod
{
    Cash,
    RestaurantQr
}

public enum OrderActorType
{
    System,
    Customer,
    Rider,
    Restaurant,
    CompanyAdmin
}

public class Order : BaseEntity
{
    public OrderType Type { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Draft;

    // ---- Cliente (sin login, vía token de la company) ----
    public Guid? CustomerTokenId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // ---- Origen (restaurante o punto de recogida del encargo) ----
    public Guid? RestaurantId { get; set; }
    public string OriginName { get; set; } = string.Empty;
    public string OriginAddress { get; set; } = string.Empty;
    public Point OriginLocation { get; set; } = null!;

    // ---- Destino (domicilio del cliente) ----
    public string DestinationAddress { get; set; } = string.Empty;
    public Point DestinationLocation { get; set; } = null!;
    public string? DestinationLinkRaw { get; set; }

    // ---- Detalle ----
    public string? Description { get; set; } // encargo/compra: qué es o qué comprar

    // ---- Dinero (la plataforma NO procesa pagos, solo referencia) ----
    public OrderPaymentMethod PaymentMethod { get; set; } = OrderPaymentMethod.Cash;
    public decimal ProductsAmount { get; set; }   // valor estimado de productos
    public decimal DeliveryFeeAmount { get; set; } // tarifa del rider
    public bool FeeCollected { get; set; }         // rider confirma cobro en efectivo
    public decimal FeeCollectedAmount { get; set; }

    // ---- Códigos de verificación ----
    public string? PickupCode { get; set; }  // 6 dígitos: rider -> restaurante
    public string? DeliveryCode { get; set; } // 4 dígitos: cliente -> rider

    // ---- Asignación automática ----
    public Guid? AssignedRiderId { get; set; }
    public int OfferCount { get; set; } // intentos de oferta realizados

    // ---- Notificación insistente al restaurante (tras aceptar el rider) ----
    public int RestaurantNotifyCount { get; set; }
    public DateTime? LastRestaurantNotifyAt { get; set; }

    // ---- Cancelación ----
    public OrderCancelReason CancelReason { get; set; } = OrderCancelReason.None;
    public string? CancelDetail { get; set; } // ej. razón del restaurante reenviada al cliente

    // ---- Reembolso oculto (compra rechazada por cliente; interno, sin UI pública) ----
    public bool RequiresRefund { get; set; }
    public bool Refunded { get; set; }

    // ---- Timestamps ----
    public DateTime? SubmittedAt { get; set; }
    public DateTime? RiderAcceptedAt { get; set; }
    public DateTime? RestaurantConfirmedAt { get; set; }
    public DateTime? PickedUpAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    // ---- Navegación ----
    public CustomerToken? CustomerToken { get; set; }
    public Restaurant? Restaurant { get; set; }
    public Rider? AssignedRider { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<RiderOffer> Offers { get; set; } = new List<RiderOffer>();
    public ICollection<OrderEvent> Events { get; set; } = new List<OrderEvent>();
}

public enum OrderCancelReason
{
    None,
    NoRidersAvailable,
    RestaurantRejected,
    RestaurantNoResponse,
    CustomerCancelled,
    RiderRefusedPayment
}
