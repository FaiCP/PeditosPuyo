using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class RiderOrdersControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly RiderOrdersController _controller;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _riderId;

    public RiderOrdersControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _riderId = SeedRider();

        var tenantMock = new Mock<ICurrentTenantService>();
        tenantMock.SetupGet(t => t.TenantId).Returns(_tenantId);
        tenantMock.SetupGet(t => t.RiderId).Returns(_riderId);

        _controller = new RiderOrdersController(
            _context,
            tenantMock.Object,
            MockHub.Create<CompanyHub>().Object,
            MockHub.Create<RestaurantHub>().Object,
            MockHub.Create<RiderHub>().Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private Guid SeedRider()
    {
        var rider = new Rider
        {
            TenantId = _tenantId,
            CompanyId = _tenantId,
            UserId = Guid.NewGuid(),
            FullName = "Rider Test",
            Phone = "0990000000",
            VehiclePlate = "PBA-1234",
            IsOnline = true,
            IsBusy = false,
            IsActive = true,
            CurrentLocation = new Point(-78.47, -1.05) { SRID = 4326 }
        };
        _context.Riders.Add(rider);
        _context.SaveChanges();
        return rider.Id;
    }

    private RiderOffer SeedOffer(Order order)
    {
        var offer = new RiderOffer
        {
            TenantId = _tenantId,
            OrderId = order.Id,
            RiderId = _riderId,
            Status = RiderOfferStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2)
        };
        _context.RiderOffers.Add(offer);
        var rider = _context.Riders.Find(_riderId)!;
        rider.IsBusy = true;
        _context.SaveChanges();
        return offer;
    }

    private Order SeedOrder(OrderType type, OrderStatus status = OrderStatus.WaitingRider)
    {
        var order = new Order
        {
            TenantId = _tenantId,
            Type = type,
            Status = status,
            CustomerName = "Cliente",
            CustomerPhone = "0991111111",
            OriginName = "Origen",
            OriginAddress = "Calle Origen",
            OriginLocation = new Point(-78.47, -1.05) { SRID = 4326 },
            DestinationAddress = "Calle Destino",
            DestinationLocation = new Point(-78.46, -1.04) { SRID = 4326 },
            DeliveryFeeAmount = 2.00m,
            SubmittedAt = DateTime.UtcNow
        };
        if (type == OrderType.Restaurant)
        {
            var restaurant = new Restaurant
            {
                TenantId = _tenantId,
                Name = "Rest Test",
                Slug = "rest-test",
                Address = "Av. 1",
                Location = new Point(-78.47, -1.05) { SRID = 4326 }
            };
            _context.Restaurants.Add(restaurant);
            order.RestaurantId = restaurant.Id;
            order.OriginName = restaurant.Name;
            order.OriginAddress = restaurant.Address;
            order.OriginLocation = restaurant.Location;
            order.Items.Add(new OrderItem
            {
                TenantId = _tenantId,
                Name = "Hamburguesa",
                Quantity = 1,
                UnitPrice = 5.00m
            });
        }
        _context.Orders.Add(order);
        _context.SaveChanges();
        return order;
    }

    [Fact]
    public async Task Offers_ReturnsPendingOffersForRider()
    {
        var order = SeedOrder(OrderType.Encargo);
        SeedOffer(order);

        var result = await _controller.Offers();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderOfferDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].OrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task Accept_Encargo_GeneratesCodesAndSetsReadyForPickup()
    {
        var order = SeedOrder(OrderType.Encargo);
        var offer = SeedOffer(order);

        var result = await _controller.Accept(offer.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tracking = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        tracking.Status.Should().Be("ReadyForPickup");

        var updated = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updated.AssignedRiderId.Should().Be(_riderId);
        updated.PickupCode.Should().NotBeNullOrEmpty();
        updated.DeliveryCode.Should().NotBeNullOrEmpty();

        var rider = await _context.Riders.FindAsync(_riderId);
        rider!.IsBusy.Should().BeTrue();
    }

    [Fact]
    public async Task Accept_Restaurant_SetsRiderAcceptedWithoutCodes()
    {
        var order = SeedOrder(OrderType.Restaurant);
        var offer = SeedOffer(order);

        var result = await _controller.Accept(offer.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var tracking = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        tracking.Status.Should().Be("RiderAccepted");

        var updated = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updated.PickupCode.Should().BeNull();
        updated.DeliveryCode.Should().BeNull();
    }

    [Fact]
    public async Task Reject_SetsOfferRejectedAndOrderWaitingRider()
    {
        var order = SeedOrder(OrderType.Encargo);
        var offer = SeedOffer(order);

        var result = await _controller.Reject(offer.Id, new RejectOfferRequest("No puedo"));

        result.Should().BeOfType<NoContentResult>();
        var updatedOrder = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updatedOrder.Status.Should().Be(OrderStatus.WaitingRider);
        var updatedOffer = await _context.RiderOffers.IgnoreQueryFilters().FirstAsync(o => o.Id == offer.Id);
        updatedOffer.Status.Should().Be(RiderOfferStatus.Rejected);
        var rider = await _context.Riders.FindAsync(_riderId);
        rider!.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task Pickup_WrongCode_ReturnsBadRequest()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.ReadyForPickup;
        order.PickupCode = "123456";
        await _context.SaveChangesAsync();

        var result = await _controller.Pickup(order.Id, new VerifyCodeRequest("000000"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Pickup_CorrectCode_SetsPickedUp()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.ReadyForPickup;
        order.PickupCode = "123456";
        await _context.SaveChangesAsync();

        var result = await _controller.Pickup(order.Id, new VerifyCodeRequest("123456"));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(new { status = "PickedUp" });
        var updated = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updated.Status.Should().Be(OrderStatus.PickedUp);
    }

    [Fact]
    public async Task InTransit_SetsInTransit()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.PickedUp;
        await _context.SaveChangesAsync();

        var result = await _controller.InTransit(order.Id);

        result.Should().BeOfType<OkObjectResult>();
        var updated = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updated.Status.Should().Be(OrderStatus.InTransit);
    }

    [Fact]
    public async Task Deliver_CorrectCode_SetsDeliveredAndFreesRider()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.InTransit;
        order.DeliveryCode = "9876";
        await _context.SaveChangesAsync();

        var result = await _controller.Deliver(order.Id, new VerifyCodeRequest("9876"));

        result.Should().BeOfType<OkObjectResult>();
        var updated = await _context.Orders.IgnoreQueryFilters().FirstAsync(o => o.Id == order.Id);
        updated.Status.Should().Be(OrderStatus.Delivered);
        var rider = await _context.Riders.FindAsync(_riderId);
        rider!.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task Active_ReturnsActiveOrders()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.ReadyForPickup;
        order.PickupCode = "111222";
        await _context.SaveChangesAsync();

        var result = await _controller.Active();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderActiveOrderDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].PickupCode.Should().Be("111222");
        list[0].Order.Status.Should().Be("ReadyForPickup");
    }

    [Fact]
    public async Task History_ReturnsDeliveredOrders()
    {
        var order = SeedOrder(OrderType.Encargo);
        order.AssignedRiderId = _riderId;
        order.Status = OrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var result = await _controller.History();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<OrderTrackingDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Status.Should().Be("Delivered");
    }
}
