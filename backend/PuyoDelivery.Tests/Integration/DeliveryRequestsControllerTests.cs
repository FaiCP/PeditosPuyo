using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
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

public class DeliveryRequestsControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly NullTenantAccessor _accessor;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly DeliveryRequestsController _controller;

    public DeliveryRequestsControllerTests()
    {
        _accessor = new NullTenantAccessor();
        _context = TestDb.CreateContext(_dbName, accessor: _accessor);
        _tenantMock = new Mock<ICurrentTenantService>();

        _controller = new DeliveryRequestsController(
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

    private void SetTenant(Guid tenantId, string role = "RestaurantAdmin", Guid? restaurantId = null)
    {
        _accessor.TenantId = tenantId;
        _tenantMock.SetupGet(t => t.TenantId).Returns(tenantId);
        _tenantMock.SetupGet(t => t.Role).Returns(role);
        _tenantMock.SetupGet(t => t.RestaurantId).Returns(restaurantId);
        _tenantMock.SetupGet(t => t.UserId).Returns(Guid.NewGuid());
    }

    private async Task<Restaurant> SeedRestaurantAsync(Guid? tenantId = null, string name = "Test Restaurant")
    {
        var restaurant = TestData.CreateRestaurant(tenantId, name);
        _context.Restaurants.Add(restaurant);
        await _context.SaveChangesAsync();
        return restaurant;
    }

    [Fact]
    public async Task Create_ValidRestaurant_ReturnsPendingRequest()
    {
        var restaurant = await SeedRestaurantAsync();
        SetTenant(restaurant.TenantId, "RestaurantAdmin", restaurant.Id);
        var req = new CreateDeliveryRequestRequest(restaurant.Id, "Calle 1", -1.04, -78.46, "nota");

        var result = await _controller.Create(req);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DeliveryRequestDto>().Subject;
        dto.Status.Should().Be("Pending");
        dto.RestaurantName.Should().Be(restaurant.Name);
        dto.DeliveryAddress.Should().Be("Calle 1");
        dto.Lat.Should().Be(-1.04);
        dto.Lng.Should().Be(-78.46);

        var saved = _context.DeliveryRequests.IgnoreQueryFilters().Single(d => d.Id == dto.Id);
        saved.TenantId.Should().Be(restaurant.TenantId);
        saved.Status.Should().Be(DeliveryRequestStatus.Pending);
    }

    [Fact]
    public async Task Create_UnknownRestaurant_Returns400()
    {
        SetTenant(Guid.NewGuid(), "RestaurantAdmin", Guid.NewGuid());
        var req = new CreateDeliveryRequestRequest(Guid.NewGuid(), "Calle 1", -1.04, -78.46, null);

        var result = await _controller.Create(req);

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAll_CompanyAdmin_SeesRequestsFromAllTenants()
    {
        var r1 = await SeedRestaurantAsync(TestData.TenantA, "Rest A");
        var r2 = await SeedRestaurantAsync(TestData.TenantB, "Rest B");
        _context.DeliveryRequests.AddRange(
            TestData.CreateDeliveryRequest(r1),
            TestData.CreateDeliveryRequest(r2));
        await _context.SaveChangesAsync();

        SetTenant(TestData.TenantB, "CompanyAdmin");

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<DeliveryRequestDto>>().Subject.ToList();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_RestaurantAdmin_SeesOnlyOwnTenant()
    {
        var r1 = await SeedRestaurantAsync(TestData.TenantA, "Rest A");
        var r2 = await SeedRestaurantAsync(TestData.TenantB, "Rest B");
        _context.DeliveryRequests.AddRange(
            TestData.CreateDeliveryRequest(r1),
            TestData.CreateDeliveryRequest(r2));
        await _context.SaveChangesAsync();

        SetTenant(TestData.TenantA, "RestaurantAdmin");

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<DeliveryRequestDto>>().Subject.ToList();
        list.Should().HaveCount(1);
        list[0].RestaurantName.Should().Be("Rest A");
    }

    [Fact]
    public async Task GetPending_ReturnsPendingAndAssigned_Only()
    {
        var restaurant = await SeedRestaurantAsync();
        _context.DeliveryRequests.AddRange(
            TestData.CreateDeliveryRequest(restaurant, "P", DeliveryRequestStatus.Pending),
            TestData.CreateDeliveryRequest(restaurant, "A", DeliveryRequestStatus.Assigned),
            TestData.CreateDeliveryRequest(restaurant, "D", DeliveryRequestStatus.Delivered));
        await _context.SaveChangesAsync();

        SetTenant(restaurant.TenantId, "CompanyAdmin");

        var result = await _controller.GetPending();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<DeliveryRequestDto>>().Subject.ToList();
        list.Should().HaveCount(2);
        list.Select(r => r.Status).Should().BeEquivalentTo(new[] { "Pending", "Assigned" });
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var restaurant = await SeedRestaurantAsync();
        var request = TestData.CreateDeliveryRequest(restaurant);
        _context.DeliveryRequests.Add(request);
        await _context.SaveChangesAsync();
        SetTenant(restaurant.TenantId, "RestaurantAdmin");

        var result = await _controller.GetById(request.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<DeliveryRequestDto>().Subject;
        dto.Id.Should().Be(request.Id);
        dto.RestaurantName.Should().Be(restaurant.Name);
        dto.Notes.Should().Be("Entrega rápida");
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        SetTenant(Guid.NewGuid(), "RestaurantAdmin");

        var result = await _controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task UpdateStatus_Delivered_SetsDeliveredAt()
    {
        var restaurant = await SeedRestaurantAsync();
        var request = TestData.CreateDeliveryRequest(restaurant);
        _context.DeliveryRequests.Add(request);
        await _context.SaveChangesAsync();
        SetTenant(restaurant.TenantId, "CompanyAdmin");

        var result = await _controller.UpdateStatus(request.Id, new UpdateStatusRequest("DELIVERED"));

        result.Should().BeOfType<NoContentResult>();
        var saved = _context.DeliveryRequests.IgnoreQueryFilters().Single(d => d.Id == request.Id);
        saved.Status.Should().Be(DeliveryRequestStatus.Delivered);
        saved.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_Assigned_SetsAssignedAt()
    {
        var restaurant = await SeedRestaurantAsync();
        var request = TestData.CreateDeliveryRequest(restaurant);
        _context.DeliveryRequests.Add(request);
        await _context.SaveChangesAsync();
        SetTenant(restaurant.TenantId, "CompanyAdmin");

        await _controller.UpdateStatus(request.Id, new UpdateStatusRequest("ASSIGNED"));

        var saved = _context.DeliveryRequests.IgnoreQueryFilters().Single(d => d.Id == request.Id);
        saved.Status.Should().Be(DeliveryRequestStatus.Assigned);
        saved.AssignedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateStatus_UnknownStatus_KeepsCurrent()
    {
        var restaurant = await SeedRestaurantAsync();
        var request = TestData.CreateDeliveryRequest(restaurant);
        _context.DeliveryRequests.Add(request);
        await _context.SaveChangesAsync();
        SetTenant(restaurant.TenantId, "CompanyAdmin");

        await _controller.UpdateStatus(request.Id, new UpdateStatusRequest("WHATEVER"));

        var saved = _context.DeliveryRequests.IgnoreQueryFilters().Single(d => d.Id == request.Id);
        saved.Status.Should().Be(DeliveryRequestStatus.Pending);
    }
}
