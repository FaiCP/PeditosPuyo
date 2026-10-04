using FluentAssertions;
using NetTopologySuite.Geometries;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Unit;

public class EntityTests
{
    [Fact]
    public void BaseEntity_Defaults()
    {
        var entity = TestData.CreateRestaurant();

        entity.Id.Should().NotBe(Guid.Empty);
        entity.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        entity.UpdatedAt.Should().BeNull();
        entity.TenantId.Should().Be(TestData.TenantA);
    }

    [Fact]
    public void DeliveryRequest_DefaultStatusIsPending()
    {
        var restaurant = TestData.CreateRestaurant();
        var request = TestData.CreateDeliveryRequest(restaurant);

        request.Status.Should().Be(DeliveryRequestStatus.Pending);
        request.AssignedAt.Should().BeNull();
        request.AcceptedAt.Should().BeNull();
        request.DeliveredAt.Should().BeNull();
        request.DeliveryLocation.Should().NotBeNull();
        request.DeliveryLocation.SRID.Should().Be(4326);
    }

    [Fact]
    public void DeliveryRequest_LocationStoresLatLngCorrectly()
    {
        var restaurant = TestData.CreateRestaurant();
        var request = new DeliveryRequest
        {
            TenantId = restaurant.TenantId,
            RestaurantId = restaurant.Id,
            RequestedBy = Guid.NewGuid(),
            DeliveryAddress = "Test",
            DeliveryLocation = new Point(-78.4684, -1.0465) { SRID = 4326 }
        };

        request.DeliveryLocation.X.Should().Be(-78.4684);
        request.DeliveryLocation.Y.Should().Be(-1.0465);
    }

    [Fact]
    public void OrderAssignment_DefaultStatusIsPending()
    {
        var restaurant = TestData.CreateRestaurant();
        var company = TestData.CreateCompany();
        var rider = TestData.CreateRider(company);
        var request = TestData.CreateDeliveryRequest(restaurant);
        var assignment = TestData.CreateAssignment(request, rider, Guid.NewGuid());

        assignment.Status.Should().Be(AssignmentStatus.Pending);
        assignment.AcceptedAt.Should().BeNull();
        assignment.RejectedAt.Should().BeNull();
        assignment.DeliveredAt.Should().BeNull();
    }

    [Fact]
    public void Rider_Defaults()
    {
        var company = TestData.CreateCompany();
        var rider = TestData.CreateRider(company);

        rider.IsOnline.Should().BeTrue();
        rider.IsBusy.Should().BeFalse();
        rider.IsActive.Should().BeTrue();
        rider.CurrentLocation.Should().NotBeNull();
        rider.LastLocationUpdate.Should().NotBeNull();
    }

    [Fact]
    public void Restaurant_Defaults()
    {
        var restaurant = TestData.CreateRestaurant();

        restaurant.IsActive.Should().BeTrue();
        restaurant.Source.Should().Be(RestaurantSource.Manual);
        restaurant.Location.SRID.Should().Be(4326);
    }

    [Fact]
    public void Subscription_Defaults()
    {
        var company = TestData.CreateCompany();
        var sub = TestData.CreateSubscription(company, months: 1);

        sub.Status.Should().Be(SubscriptionStatus.Active);
        sub.PricePerMonth.Should().Be(150m);
        sub.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(29));
        sub.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(32));
    }

    [Fact]
    public void CompanyPayment_Defaults()
    {
        var company = TestData.CreateCompany();
        var sub = TestData.CreateSubscription(company);
        var payment = new CompanyPayment
        {
            TenantId = company.TenantId,
            SubscriptionId = sub.Id,
            CompanyId = company.Id,
            Amount = 150m,
            Method = PaymentMethod.Transfer,
            Reference = "TRF-001",
            Status = PaymentStatus.Confirmed,
            PaidAt = DateTime.UtcNow
        };

        payment.Status.Should().Be(PaymentStatus.Confirmed);
        payment.Method.Should().Be(PaymentMethod.Transfer);
        payment.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public void DeliveryRequestStatus_EnumValues()
    {
        new[] { "Pending", "Assigned", "Accepted", "InTransit", "Delivered", "Cancelled" }
            .Should().BeEquivalentTo(Enum.GetNames<DeliveryRequestStatus>());
    }

    [Fact]
    public void AssignmentStatus_EnumValues()
    {
        new[] { "Pending", "Accepted", "Rejected", "InTransit", "Delivered" }
            .Should().BeEquivalentTo(Enum.GetNames<AssignmentStatus>());
    }
}
