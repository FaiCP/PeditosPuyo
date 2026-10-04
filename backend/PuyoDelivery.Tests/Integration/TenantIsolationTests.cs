using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class TenantIsolationTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly NullTenantAccessor _accessor;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly RidersController _riders;
    private readonly DeliveryRequestsController _deliveryRequests;

    private readonly DeliveryCompany _companyA;
    private readonly DeliveryCompany _companyB;
    private readonly Restaurant _restaurantA;
    private readonly Restaurant _restaurantB;

    public TenantIsolationTests()
    {
        _accessor = new NullTenantAccessor();
        _context = TestDb.CreateContext(_dbName, accessor: _accessor);
        _tenantMock = new Mock<ICurrentTenantService>();

        _companyA = TestData.CreateCompany(TestData.TenantA, "Co A");
        _companyB = TestData.CreateCompany(TestData.TenantB, "Co B");
        _restaurantA = TestData.CreateRestaurant(TestData.TenantA, "Rest A");
        _restaurantB = TestData.CreateRestaurant(TestData.TenantB, "Rest B");

        _context.DeliveryCompanies.AddRange(_companyA, _companyB);
        _context.Restaurants.AddRange(_restaurantA, _restaurantB);
        _context.Riders.AddRange(
            TestData.CreateRider(_companyA, "Rider A1"),
            TestData.CreateRider(_companyA, "Rider A2"),
            TestData.CreateRider(_companyB, "Rider B1"));
        _context.DeliveryRequests.AddRange(
            TestData.CreateDeliveryRequest(_restaurantA),
            TestData.CreateDeliveryRequest(_restaurantB));
        _context.SaveChanges();

        _riders = new RidersController(_context, _tenantMock.Object);
        _deliveryRequests = new DeliveryRequestsController(
            _context,
            _tenantMock.Object,
            MockHub.Create<RestaurantHub>().Object,
            MockHub.Create<CompanyHub>().Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void QueryFilter_HidesOtherTenants_WhenTenantSet()
    {
        _accessor.TenantId = TestData.TenantA;

        var riders = _context.Riders.ToList();
        riders.Should().HaveCount(2);
        riders.Should().OnlyContain(r => r.TenantId == TestData.TenantA);

        var restaurants = _context.Restaurants.ToList();
        restaurants.Should().ContainSingle();
        restaurants[0].TenantId.Should().Be(TestData.TenantA);
    }

    [Fact]
    public void QueryFilter_ShowsAll_WhenTenantNull()
    {
        _accessor.TenantId = null;

        _context.Riders.ToList().Should().HaveCount(3);
        _context.Restaurants.ToList().Should().HaveCount(2);
        _context.DeliveryRequests.ToList().Should().HaveCount(2);
    }

    [Fact]
    public void QueryFilter_IgnoreQueryFilters_BypassesTenant()
    {
        _accessor.TenantId = TestData.TenantA;

        _context.Riders.IgnoreQueryFilters().Should().HaveCount(3);
        _context.Restaurants.IgnoreQueryFilters().Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_ReturnsOnlyCurrentTenant()
    {
        _accessor.TenantId = TestData.TenantA;
        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantA);

        var result = await _riders.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderDto>>().Subject.ToList();
        list.Should().HaveCount(2);
        list.Select(r => r.FullName).Should().BeEquivalentTo(new[] { "Rider A1", "Rider A2" });
    }

    [Fact]
    public async Task GetAll_AfterTenantChange_ReturnsOtherTenantData()
    {
        _accessor.TenantId = TestData.TenantA;
        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantA);
        await _riders.GetAll();

        _accessor.TenantId = TestData.TenantB;
        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantB);

        var result = await _riders.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].FullName.Should().Be("Rider B1");
    }

    [Fact]
    public async Task DeliveryRequests_Create_UsesRestaurantTenant_NotCallerTenant()
    {
        // Restaurant belongs to TenantA; "caller" claims TenantB but request is for restaurantA
        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantB);
        _tenantMock.SetupGet(t => t.Role).Returns("RestaurantAdmin");
        _tenantMock.SetupGet(t => t.RestaurantId).Returns(_restaurantB.Id);
        _accessor.TenantId = TestData.TenantB;

        var result = await _deliveryRequests.Create(new CreateDeliveryRequestRequest(
            _restaurantA.Id, "Dir", -1.0, -78.5, null));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DeliveryRequestDto>().Subject;

        // Created request must belong to the restaurant's tenant (TenantA), not the caller's (TenantB)
        var saved = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == dto.Id);
        saved.TenantId.Should().Be(TestData.TenantA);
    }

    [Fact]
    public async Task RestaurantAdmin_GetAll_DoesNotSeeOtherTenantRequests()
    {
        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantA);
        _tenantMock.SetupGet(t => t.Role).Returns("RestaurantAdmin");
        _accessor.TenantId = TestData.TenantA;

        var result = await _deliveryRequests.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<DeliveryRequestDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].RestaurantName.Should().Be("Rest A");
    }
}
