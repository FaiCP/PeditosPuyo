using NetTopologySuite.Geometries;
using PuyoDelivery.Core.Entities;

namespace PuyoDelivery.Tests.TestHelpers;

public static class TestData
{
    public static readonly Guid TenantA = Guid.NewGuid();
    public static readonly Guid TenantB = Guid.NewGuid();

    public static Restaurant CreateRestaurant(Guid? tenantId = null, string name = "Test Restaurant", bool isActive = true)
    {
        return new Restaurant
        {
            TenantId = tenantId ?? TenantA,
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            Address = "Av. Principal 123",
            Phone = "0991234567",
            Location = new Point(-78.4684, -1.0465) { SRID = 4326 },
            MenuSummary = "Comida rápida",
            IsActive = isActive,
            Source = RestaurantSource.Manual
        };
    }

    public static DeliveryCompany CreateCompany(Guid? tenantId = null, string name = "Test Company")
    {
        return new DeliveryCompany
        {
            TenantId = tenantId ?? TenantB,
            Name = name,
            Slug = name.ToLower().Replace(" ", "-"),
            MonthlyRatePerDriver = 150m,
            RiderLimit = 10,
            IsActive = true
        };
    }

    public static Rider CreateRider(DeliveryCompany company, string fullName = "Carlos Rider", bool isOnline = true, bool isBusy = false)
    {
        return new Rider
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Phone = "0998765432",
            VehiclePlate = "PBA-1234",
            IsOnline = isOnline,
            IsBusy = isBusy,
            IsActive = true,
            CurrentLocation = new Point(-78.4700, -1.0500) { SRID = 4326 },
            LastLocationUpdate = DateTime.UtcNow
        };
    }

    public static DeliveryRequest CreateDeliveryRequest(Restaurant restaurant, string address = "Calle Falsa 123", DeliveryRequestStatus status = DeliveryRequestStatus.Pending)
    {
        return new DeliveryRequest
        {
            TenantId = restaurant.TenantId,
            RestaurantId = restaurant.Id,
            RequestedBy = Guid.NewGuid(),
            Status = status,
            DeliveryAddress = address,
            DeliveryLocation = new Point(-78.4690, -1.0470) { SRID = 4326 },
            Notes = "Entrega rápida"
        };
    }

    public static OrderAssignment CreateAssignment(DeliveryRequest request, Rider rider, Guid assignedBy, AssignmentStatus status = AssignmentStatus.Pending)
    {
        return new OrderAssignment
        {
            TenantId = rider.TenantId,
            RequestId = request.Id,
            RiderId = rider.Id,
            AssignedBy = assignedBy,
            Status = status,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Subscription CreateSubscription(DeliveryCompany company, int months = 1)
    {
        return new Subscription
        {
            TenantId = company.TenantId,
            CompanyId = company.Id,
            PlanName = "Básico",
            PricePerMonth = 150m,
            RiderLimit = 10,
            Status = SubscriptionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMonths(months)
        };
    }
}
