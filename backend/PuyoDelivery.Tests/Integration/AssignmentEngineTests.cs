using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PuyoDelivery.API.Background;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class AssignmentEngineTests
{
    private static readonly Guid Tenant = Guid.NewGuid();
    private static readonly DateTime T0 = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static (ApplicationDbContext db, AssignmentEngine engine, FakeTimeProvider clock) Build()
    {
        var db = TestDb.CreateContext(Guid.NewGuid().ToString());
        var clock = new FakeTimeProvider(new DateTimeOffset(T0, TimeSpan.Zero));
        var engine = new AssignmentEngine(
            db,
            MockHub.Create<RiderHub>().Object,
            MockHub.Create<RestaurantHub>().Object,
            MockHub.Create<CompanyHub>().Object,
            new PuyoDelivery.Infrastructure.Services.FcmV1Service(null, null),
            clock);
        return (db, engine, clock);
    }

    private static Order NewWaitingOrder(Point? origin = null) => new()
    {
        TenantId = Tenant,
        Type = OrderType.Encargo,
        Status = OrderStatus.WaitingRider,
        CustomerName = "C",
        CustomerPhone = "099",
        OriginName = "Oficina",
        OriginAddress = "Calle 1",
        OriginLocation = origin ?? new Point(-78.4699, -1.0512) { SRID = 4326 },
        DestinationAddress = "Casa",
        DestinationLocation = new Point(-78.4700, -1.0500) { SRID = 4326 },
        SubmittedAt = T0
    };

    private static Rider NewRider(string name, double lng, double lat) => new()
    {
        TenantId = Tenant,
        CompanyId = Tenant,
        UserId = Guid.NewGuid(),
        FullName = name,
        Phone = "099",
        VehiclePlate = "PBA-1",
        IsOnline = true,
        IsBusy = false,
        IsActive = true,
        CurrentLocation = new Point(lng, lat) { SRID = 4326 },
        LastLocationUpdate = T0
    };

    [Fact]
    public async Task Tick_CreatesOfferForNearestAvailableRider()
    {
        var (db, engine, clock) = Build();
        var near = NewRider("Cerca", -78.4699, -1.0513);   // ~11m
        var far = NewRider("Lejos", -78.4600, -1.0400);    // ~1.5km
        db.Riders.AddRange(near, far);
        var order = NewWaitingOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync();

        var offer = db.RiderOffers.IgnoreQueryFilters().Single(o => o.OrderId == order.Id);
        offer.RiderId.Should().Be(near.Id);
        offer.Status.Should().Be(RiderOfferStatus.Pending);
        offer.AttemptNumber.Should().Be(1);
        offer.ExpiresAt.Should().Be(T0.AddMinutes(2));
        near.IsBusy.Should().BeTrue();
        order.OfferCount.Should().Be(1);
        clock.GetUtcNow().UtcDateTime.Should().Be(T0);
    }

    [Fact]
    public async Task Tick_ReNotifiesRiderAfter40Seconds()
    {
        var (db, engine, clock) = Build();
        db.Riders.Add(NewRider("R", -78.4699, -1.0512));
        var order = NewWaitingOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync(); // crea oferta, NotifyCount=0, LastNotifiedAt=T0

        clock.AdvTime(TimeSpan.FromSeconds(39));
        await engine.RunTickAsync();
        var o1 = db.RiderOffers.IgnoreQueryFilters().Single(o => o.OrderId == order.Id);
        o1.NotifyCount.Should().Be(0); // aún no llega a 40s

        clock.AdvTime(TimeSpan.FromSeconds(2)); // total 41s
        await engine.RunTickAsync();
        o1.NotifyCount.Should().Be(1);
    }

    [Fact]
    public async Task Tick_ExpiresOfferAndReoffersSameRiderIfNotRejected()
    {
        var (db, engine, clock) = Build();
        var rider = NewRider("R", -78.4699, -1.0512);
        db.Riders.Add(rider);
        var order = NewWaitingOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync(); // attempt 1 (crear)
        clock.AdvTime(TimeSpan.FromSeconds(121));
        await engine.RunTickAsync(); // expirar attempt 1
        clock.AdvTime(TimeSpan.FromSeconds(121));
        await engine.RunTickAsync(); // crear attempt 2

        var offers = db.RiderOffers.IgnoreQueryFilters().Where(o => o.OrderId == order.Id).ToList();
        offers.Should().HaveCount(2);
        offers.Single(o => o.AttemptNumber == 1).Status.Should().Be(RiderOfferStatus.Expired);
        offers.Single(o => o.AttemptNumber == 2).Status.Should().Be(RiderOfferStatus.Pending);
        order.OfferCount.Should().Be(2);
    }

    [Fact]
    public async Task Tick_CancelsWithNoRidersAfterFourAttempts()
    {
        var (db, engine, clock) = Build();
        db.Riders.Add(NewRider("R", -78.4699, -1.0512));
        var order = NewWaitingOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // Cada intento = crear + expirar (2 ticks). Cancelar en el tick con 4 ofertas agotadas.
        for (int i = 0; i < 12 && order.Status == OrderStatus.WaitingRider; i++)
        {
            await engine.RunTickAsync();
            clock.AdvTime(TimeSpan.FromSeconds(121));
            db.Entry(order).Reload();
        }

        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelReason.Should().Be(OrderCancelReason.NoRidersAvailable);
        order.CancelDetail.Should().Contain("riders");
        db.RiderOffers.IgnoreQueryFilters().Count(o => o.OrderId == order.Id).Should().Be(4);
    }

    [Fact]
    public async Task Tick_OnHoldWhenOnlyRiderRejectedAndNoCandidates()
    {
        var (db, engine, clock) = Build();
        var rider = NewRider("R", -78.4699, -1.0512);
        db.Riders.Add(rider);
        var order = NewWaitingOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync(); // crea oferta
        // Simular rechazo (como haría el controller):
        var offer = db.RiderOffers.IgnoreQueryFilters().Single(o => o.OrderId == order.Id);
        offer.Status = RiderOfferStatus.Rejected;
        offer.RejectionReason = "no puedo";
        rider.IsBusy = false;
        order.Status = OrderStatus.WaitingRider;
        await db.SaveChangesAsync();

        await engine.RunTickAsync(); // candidato: único rider rechazado -> excluido -> OnHold

        order.Status.Should().Be(OrderStatus.OnHold);
    }

    [Fact]
    public async Task RestaurantOrder_NudgesEvery30s_AndCancelsAfter3Min()
    {
        var (db, engine, clock) = Build();
        var rider = NewRider("R", -78.4699, -1.0512);
        var restaurant = TestData.CreateRestaurant(Tenant);
        db.Riders.Add(rider);
        db.Restaurants.Add(restaurant);
        var order = NewWaitingOrder();
        order.Type = OrderType.Restaurant;
        order.RestaurantId = restaurant.Id;
        order.Status = OrderStatus.RiderAccepted;
        order.AssignedRiderId = rider.Id;
        order.RiderAcceptedAt = T0;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync();
        order.RestaurantNotifyCount.Should().Be(1);

        clock.AdvTime(TimeSpan.FromSeconds(31));
        await engine.RunTickAsync();
        order.RestaurantNotifyCount.Should().Be(2);

        clock.AdvTime(TimeSpan.FromMinutes(3)); // ahora > RiderAcceptedAt + 3min
        await engine.RunTickAsync();
        order.Status.Should().Be(OrderStatus.Cancelled);
        order.CancelReason.Should().Be(OrderCancelReason.RestaurantNoResponse);
    }

    [Fact]
    public async Task RestaurantOrder_ConfirmedStopsNudging()
    {
        var (db, engine, clock) = Build();
        var rider = NewRider("R", -78.4699, -1.0512);
        var restaurant = TestData.CreateRestaurant(Tenant);
        db.Riders.Add(rider);
        db.Restaurants.Add(restaurant);
        var order = NewWaitingOrder();
        order.Type = OrderType.Restaurant;
        order.RestaurantId = restaurant.Id;
        order.Status = OrderStatus.RiderAccepted;
        order.AssignedRiderId = rider.Id;
        order.RiderAcceptedAt = T0;
        order.RestaurantConfirmedAt = T0; // ya confirmado
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await engine.RunTickAsync();

        order.RestaurantNotifyCount.Should().Be(0);
        order.Status.Should().Be(OrderStatus.RiderAccepted);
    }
}
