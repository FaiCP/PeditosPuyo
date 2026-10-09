namespace PuyoDelivery.Core.Dtos;

// ---- Company: generar link del cliente ----
public record CreateCustomerTokenRequest(string Phone, string? Name);
public record CustomerTokenDto(Guid Id, string Token, string Phone, string? Name, DateTime ExpiresAt, string PublicUrl);

// ---- Catálogo público (cliente) ----
public record PublicRestaurantDto(Guid Id, string Name, string Address, string Phone, string? MenuSummary, int ItemCount, bool HasPaymentQr, string? LogoUrl);
public record PublicMenuItemDto(Guid Id, string Name, string? Description, decimal Price, string? ImageUrl);
public record PublicRestaurantDetailDto(Guid Id, string Name, string Address, string Phone, string? PaymentQrUrl, string? LogoUrl, List<PublicMenuItemDto> Menu);

// ---- Crear orden ----
public record OrderItemLine(Guid? MenuItemId, string Name, int Quantity, decimal UnitPrice, string? Notes);
public record CreateOrderRequest(
    string Type,                 // "restaurant" | "compra" | "encargo"
    Guid? RestaurantId,          // requerido si Type=restaurant
    double? OriginLat,           // requerido si compra/encargo (link del lugar)
    double? OriginLng,
    string? OriginName,
    string? OriginAddress,
    string DestinationAddress,   // siempre: domicilio del cliente
    double DestinationLat,
    double DestinationLng,
    string? DestinationLinkRaw,
    string? Description,         // encargo/compra: qué es o qué comprar
    List<OrderItemLine>? Items,  // restaurant/compra
    string CustomerName,
    string CustomerPhone,
    string PaymentMethod);       // "cash" | "restaurantQr"

// ---- Estado / tracking ----
public record OrderTrackingItemDto(string Name, int Quantity, decimal UnitPrice);
public record OrderTrackingDto(
    Guid Id,
    string Type,
    string Status,
    string OriginName,
    string OriginAddress,
    string DestinationAddress,
    string? Description,
    decimal ProductsAmount,
    decimal DeliveryFeeAmount,
    decimal TotalAmount,
    string PaymentMethod,
    bool HasPaymentQr,
    string? PaymentQrUrl,
    string? PickupCode,
    string? DeliveryCode,
    string? RiderName,
    double? RiderLat,
    double? RiderLng,
    string? CancelDetail,
    DateTime CreatedAt,
    DateTime? SubmittedAt,
    DateTime? RiderAcceptedAt,
    DateTime? RestaurantConfirmedAt,
    DateTime? DeliveredAt,
    List<OrderTrackingItemDto> Items);

// ---- Rider ----
public record RiderOfferDto(
    Guid OfferId, Guid OrderId, string Type, string OriginName, string OriginAddress,
    string DestinationAddress, string? Description, decimal DeliveryFeeAmount,
    List<OrderTrackingItemDto> Items, DateTime ExpiresAt);
public record RejectOfferRequest(string? Reason);
public record VerifyCodeRequest(string Code);
public record ConfirmReadyRequest(bool Ready = true);
public record RegisterFcmTokenRequest(string Token);

// ---- Restaurante ----
public record RestaurantOrderDto(
    Guid Id, string Type, string Status, string DestinationAddress, string CustomerName, string CustomerPhone,
    decimal ProductsAmount, decimal DeliveryFeeAmount, string PaymentMethod,
    DateTime CreatedAt, DateTime? RiderAcceptedAt, List<OrderTrackingItemDto> Items);

public record OrderEventDto(DateTime At, string ActorType, string Description);

// ---- Rider app F2.6: tracking + lo que el rider necesita en pantalla (código de recolección y coordenadas) ----
public record RiderActiveOrderDto(
    OrderTrackingDto Order,
    double OriginLat, double OriginLng,
    double DestinationLat, double DestinationLng,
    string? PickupCode);
