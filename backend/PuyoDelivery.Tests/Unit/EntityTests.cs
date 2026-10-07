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
    public void OrderStatus_EnumValues()
    {
        new[] { "Draft", "WaitingRider", "RiderAccepted", "ReadyForPickup", "PickedUp", "InTransit", "Delivered", "OnHold", "Cancelled" }
            .Should().BeEquivalentTo(Enum.GetNames<OrderStatus>());
    }
}
