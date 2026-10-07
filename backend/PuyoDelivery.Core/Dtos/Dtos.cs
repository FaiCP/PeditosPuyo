namespace PuyoDelivery.Core.Dtos;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string FullName, string Email, string Phone, string Password, string Role);

public record LoginResponse(string Token, string UserId, string Role, Guid? TenantId, string FullName, string Email, Guid? CompanyId = null, Guid? RestaurantId = null, Guid? RiderId = null);

public record CompanyDto(Guid Id, string Name, string Slug, decimal MonthlyRatePerDriver, int RiderLimit, bool IsActive);
public record CreateCompanyRequest(string Name, string Slug, decimal MonthlyRatePerDriver, int RiderLimit);
public record UpdateCompanyRequest(string Name, string Slug, decimal MonthlyRatePerDriver, int RiderLimit, bool IsActive);

public record SubscriptionDto(Guid Id, Guid CompanyId, string PlanName, decimal PricePerMonth, int RiderLimit, string Status, DateTime ExpiresAt);
public record CreateSubscriptionRequest(Guid CompanyId, string PlanName, decimal PricePerMonth, int RiderLimit, int Months);
public record CreatePaymentRequest(decimal Amount, string Method, string Reference);
public record PaymentDto(Guid Id, decimal Amount, string Method, string Reference, string Status, DateTime? PaidAt);

public record RiderDto(Guid Id, string FullName, string Phone, string VehiclePlate, bool IsOnline, bool IsBusy, bool IsActive, double? Lat, double? Lng, DateTime? LastLocationUpdate);
public record CreateRiderRequest(string FullName, string Phone, string VehiclePlate, string Email, string Password);
public record UpdateRiderRequest(string FullName, string Phone, string VehiclePlate, bool IsActive);
public record RiderStatusRequest(bool IsOnline);
public record UpdateLocationRequest(double Lat, double Lng);
public record RiderNearbyDto(Guid Id, string FullName, string VehiclePlate, double DistanceKm, double Lat, double Lng);

public record RestaurantDto(Guid Id, string Name, string Slug, string Address, string Phone, double Lat, double Lng, string? MenuSummary, bool IsActive, string Source);
public record CreateRestaurantRequest(string Name, string Slug, string Address, string Phone, double Lat, double Lng, string? MenuSummary);
public record UpdateRestaurantRequest(string Name, string Slug, string Address, string Phone, double Lat, double Lng, string? MenuSummary, bool IsActive);
public record MenuItemDto(Guid Id, string Name, string? Description, decimal Price, bool IsActive);
public record CreateMenuItemRequest(string Name, string? Description, decimal Price);
public record ImportResult(int Imported, List<string> Errors);
