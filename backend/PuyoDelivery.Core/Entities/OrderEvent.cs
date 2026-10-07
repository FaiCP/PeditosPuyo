namespace PuyoDelivery.Core.Entities;

/// <summary>Trazabilidad de cada cambio de estado; también dispara notificaciones.</summary>
public class OrderEvent : BaseEntity
{
    public Guid OrderId { get; set; }
    public OrderActorType ActorType { get; set; } = OrderActorType.System;
    public Guid? ActorId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }

    public Order Order { get; set; } = null!;
}
