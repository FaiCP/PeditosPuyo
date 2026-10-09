using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.Infrastructure.Services;

/// <summary>
/// Lógica de dominio de pedidos Fase 2: creación, totales y mapeo a DTO de tracking.
/// La máquina de estados y las transiciones viven aquí (F2.3/F2.4 las extienden).
/// </summary>
public class OrderService
{
    private readonly ApplicationDbContext _context;
    public const decimal DefaultDeliveryFee = 2.00m;

    public OrderService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(Order? Order, string? Error)> CreateOrderAsync(CustomerToken token, CreateOrderRequest req)
    {
        var type = (req.Type ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(req.DestinationAddress))
            return (null, "La dirección de entrega es obligatoria");
        if (req.DestinationLat is < -90 or > 90 || req.DestinationLng is < -180 or > 180)
            return (null, "Coordenadas de destino inválidas");
        if (string.IsNullOrWhiteSpace(req.CustomerName) || string.IsNullOrWhiteSpace(req.CustomerPhone))
            return (null, "Nombre y teléfono del cliente son obligatorios");

        var order = new Order
        {
            TenantId = token.TenantId,
            CustomerTokenId = token.Id,
            CustomerName = req.CustomerName.Trim(),
            CustomerPhone = req.CustomerPhone.Trim(),
            DestinationAddress = req.DestinationAddress.Trim(),
            DestinationLocation = new Point(req.DestinationLng, req.DestinationLat) { SRID = 4326 },
            DestinationLinkRaw = req.DestinationLinkRaw,
            Status = OrderStatus.WaitingRider,
            SubmittedAt = DateTime.UtcNow
        };

        if (type == "restaurant")
        {
            if (!req.RestaurantId.HasValue) return (null, "Debes elegir un restaurante");

            var restaurant = await _context.Restaurants
                .IgnoreQueryFilters()
                .Include(r => r.MenuItems)
                .FirstOrDefaultAsync(r => r.Id == req.RestaurantId && r.TenantId == token.TenantId && r.IsActive);
            if (restaurant == null) return (null, "Restaurante no encontrado en este catálogo");

            order.Type = OrderType.Restaurant;
            order.RestaurantId = restaurant.Id;
            order.OriginName = restaurant.Name;
            order.OriginAddress = restaurant.Address;
            order.OriginLocation = restaurant.Location;

            var items = req.Items ?? new List<OrderItemLine>();
            if (items.Count == 0) return (null, "El pedido no tiene productos");

            foreach (var line in items)
            {
                var menuItem = restaurant.MenuItems.FirstOrDefault(m => m.Id == line.MenuItemId);
                if (menuItem == null) return (null, $"Producto no válido en el menú: {line.Name}");
                if (line.Quantity <= 0) return (null, "Cantidad inválida");

                order.Items.Add(new OrderItem
                {
                    TenantId = token.TenantId,
                    MenuItemId = menuItem.Id,
                    Name = menuItem.Name,
                    Quantity = line.Quantity,
                    UnitPrice = menuItem.Price,
                    Notes = line.Notes
                });
            }
            order.ProductsAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

            if (req.PaymentMethod.Trim().ToLowerInvariant() == "restaurantqr")
            {
                if (string.IsNullOrWhiteSpace(restaurant.PaymentQrUrl))
                    return (null, "Este restaurante no tiene código QR de pago");
                order.PaymentMethod = OrderPaymentMethod.RestaurantQr;
            }
            else
            {
                order.PaymentMethod = OrderPaymentMethod.Cash;
            }
        }
        else if (type == "compra" || type == "encargo")
        {
            if (req.OriginLat is null || req.OriginLng is null ||
                req.OriginLat < -90 || req.OriginLat > 90 || req.OriginLng < -180 || req.OriginLng > 180)
                return (null, "Indica la ubicación del origen (link de Google Maps)");

            order.Type = type == "compra" ? OrderType.Compra : OrderType.Encargo;
            order.OriginLocation = new Point(req.OriginLng!.Value, req.OriginLat!.Value) { SRID = 4326 };
            order.OriginName = req.OriginName?.Trim() ?? "Punto de recogida";
            order.OriginAddress = req.OriginAddress?.Trim() ?? order.OriginName;
            order.Description = req.Description;
            order.PaymentMethod = OrderPaymentMethod.Cash;

            if (type == "compra")
            {
                var items = req.Items ?? new List<OrderItemLine>();
                if (items.Count == 0) return (null, "Indica los artículos a comprar");
                foreach (var line in items)
                {
                    if (string.IsNullOrWhiteSpace(line.Name)) return (null, "Artículo sin nombre");
                    order.Items.Add(new OrderItem
                    {
                        TenantId = token.TenantId,
                        Name = line.Name.Trim(),
                        Quantity = Math.Max(1, line.Quantity),
                        UnitPrice = line.UnitPrice,
                        Notes = line.Notes
                    });
                }
                order.ProductsAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
            }
        }
        else
        {
            return (null, "Tipo de pedido no válido");
        }

        order.DeliveryFeeAmount = DefaultDeliveryFee;

        _context.Orders.Add(order);
        // El evento se registra por DbSet con FK explícita (no vía navegación de colección):
        // es el patrón robusto para padres ya persistidos y lo reutiliza el motor de asignación.
        _context.OrderEvents.Add(NewEvent(order.Id, token.TenantId, OrderActorType.Customer, "Pedido enviado por el cliente"));
        await _context.SaveChangesAsync();

        return (order, null);
    }

    /// <summary>Registra un evento de trazabilidad sobre un pedido ya existente (FK explícita, sin navegación).</summary>
    public void LogEvent(Guid orderId, Guid tenantId, OrderActorType actorType, string description,
        Guid? actorId = null, string? metadataJson = null)
    {
        _context.OrderEvents.Add(NewEvent(orderId, tenantId, actorType, description, actorId, metadataJson));
    }

    private static OrderEvent NewEvent(Guid orderId, Guid tenantId, OrderActorType actorType, string description,
        Guid? actorId = null, string? metadataJson = null) => new()
    {
        OrderId = orderId,
        TenantId = tenantId,
        ActorType = actorType,
        ActorId = actorId,
        Description = description,
        MetadataJson = metadataJson
    };

    public static string GeneratePickupCode()
    {
        // 6 dígitos criptográficos
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    public static string GenerateDeliveryCode()
    {
        // 4 dígitos criptográficos
        return RandomNumberGenerator.GetInt32(0, 10_000).ToString("D4");
    }

    public static OrderTrackingDto ToTracking(Order order, Restaurant? restaurant, Rider? rider)
    {
        return new OrderTrackingDto(
            order.Id,
            order.Type.ToString().ToLowerInvariant(),
            order.Status.ToString(),
            order.OriginName,
            order.OriginAddress,
            order.DestinationAddress,
            order.Description,
            order.ProductsAmount,
            order.DeliveryFeeAmount,
            order.ProductsAmount + order.DeliveryFeeAmount,
            order.PaymentMethod.ToString().ToLowerInvariant(),
            restaurant?.PaymentQrUrl != null,
            restaurant?.PaymentQrUrl,
            order.PickupCode,
            order.DeliveryCode,
            rider?.FullName,
            rider?.CurrentLocation?.Y,
            rider?.CurrentLocation?.X,
            order.CancelDetail,
            order.CreatedAt,
            order.SubmittedAt,
            order.RiderAcceptedAt,
            order.RestaurantConfirmedAt,
            order.DeliveredAt,
            order.Items.Select(i => new OrderTrackingItemDto(i.Name, i.Quantity, i.UnitPrice)).ToList());
    }
}
