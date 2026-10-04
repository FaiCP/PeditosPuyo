using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class RidersControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly NullTenantAccessor _accessor;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly RidersController _ridersController;
    private readonly DriverController _driverController;

    private readonly DeliveryCompany _companyA;
    private readonly DeliveryCompany _companyB;
    private readonly Rider _riderA;
    private readonly Rider _riderB;

    public RidersControllerTests()
    {
        _accessor = new NullTenantAccessor();
        _context = TestDb.CreateContext(_dbName, accessor: _accessor);
        _tenantMock = new Mock<ICurrentTenantService>();

        _companyA = TestData.CreateCompany(TestData.TenantA, "Company A");
        _companyB = TestData.CreateCompany(TestData.TenantB, "Company B");
        _riderA = TestData.CreateRider(_companyA, "Rider A", isOnline: true);
        _riderB = TestData.CreateRider(_companyB, "Rider B", isOnline: false);
        _context.DeliveryCompanies.AddRange(_companyA, _companyB);
        _context.Riders.AddRange(_riderA, _riderB);
        _context.SaveChanges();

        _ridersController = new RidersController(_context, _tenantMock.Object);
        _driverController = new DriverController(_context, _tenantMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetCompanyTenant(Guid tenantId, Guid? companyId = null)
    {
        _accessor.TenantId = tenantId;
        _tenantMock.SetupGet(t => t.TenantId).Returns(tenantId);
        _tenantMock.SetupGet(t => t.CompanyId).Returns(companyId);
        _tenantMock.SetupGet(t => t.Role).Returns("CompanyAdmin");
    }

    [Fact]
    public async Task GetAll_FiltersByTenant()
    {
        SetCompanyTenant(TestData.TenantA, _companyA.Id);

        var result = await _ridersController.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].FullName.Should().Be("Rider A");
    }

    [Fact]
    public async Task GetOnline_ReturnsOnlyOnlineActive()
    {
        var offlineOnline = TestData.CreateRider(_companyA, "Offline Active", isOnline: false);
        var inactive = TestData.CreateRider(_companyA, "Inactive", isOnline: true);
        inactive.IsActive = false;
        _context.Riders.AddRange(offlineOnline, inactive);
        _context.SaveChanges();

        SetCompanyTenant(TestData.TenantA, _companyA.Id);

        var result = await _ridersController.GetOnline();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RiderDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].FullName.Should().Be("Rider A");
    }

    [Fact]
    public async Task Create_AddsRiderToCompany()
    {
        SetCompanyTenant(TestData.TenantA, _companyA.Id);
        var request = new CreateRiderRequest("Nuevo Rider", "0990000000", "XYZ-9999", "nuevo@test.com", "Test123!");

        var result = await _ridersController.Create(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<RiderDto>().Subject;
        dto.FullName.Should().Be("Nuevo Rider");
        dto.VehiclePlate.Should().Be("XYZ-9999");

        var saved = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == dto.Id);
        saved.TenantId.Should().Be(TestData.TenantA);
        saved.CompanyId.Should().Be(_companyA.Id);
    }

    [Fact]
    public async Task Update_ModifiesRiderFields()
    {
        SetCompanyTenant(TestData.TenantA, _companyA.Id);
        var request = new UpdateRiderRequest("Rider A Updated", "0991111111", "NEW-0001", false);

        var result = await _ridersController.Update(_riderA.Id, request);

        result.Should().BeOfType<NoContentResult>();
        var saved = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == _riderA.Id);
        saved.FullName.Should().Be("Rider A Updated");
        saved.Phone.Should().Be("0991111111");
        saved.VehiclePlate.Should().Be("NEW-0001");
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_WrongTenant_Returns403()
    {
        SetCompanyTenant(TestData.TenantB, _companyB.Id);
        var request = new UpdateRiderRequest("Hacked", "099", "HACK-1", true);

        var result = await _ridersController.Update(_riderA.Id, request);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        SetCompanyTenant(TestData.TenantA, _companyA.Id);

        var result = await _ridersController.Update(Guid.NewGuid(), new UpdateRiderRequest("X", "X", "X", true));

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_RemovesRider()
    {
        SetCompanyTenant(TestData.TenantA, _companyA.Id);

        var result = await _ridersController.Delete(_riderA.Id);

        result.Should().BeOfType<NoContentResult>();
        _context.Riders.IgnoreQueryFilters().Should().NotContain(r => r.Id == _riderA.Id);
    }

    [Fact]
    public async Task Delete_WrongTenant_Returns403()
    {
        SetCompanyTenant(TestData.TenantB, _companyB.Id);

        var result = await _ridersController.Delete(_riderA.Id);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task UpdateLocation_SetsPointAndTimestamp()
    {
        _tenantMock.SetupGet(t => t.RiderId).Returns(_riderA.Id);

        var result = await _driverController.UpdateLocation(new UpdateLocationRequest(-1.0500, -78.4700));

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();

        var saved = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == _riderA.Id);
        saved.CurrentLocation.Should().NotBeNull();
        saved.CurrentLocation!.X.Should().Be(-78.4700);
        saved.CurrentLocation.Y.Should().Be(-1.0500);
        saved.LastLocationUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateLocation_UnknownRider_Returns404()
    {
        _tenantMock.SetupGet(t => t.RiderId).Returns(Guid.NewGuid());

        var result = await _driverController.UpdateLocation(new UpdateLocationRequest(-1.05, -78.47));

        result.Should().BeOfType<NotFoundResult>();
    }
}
