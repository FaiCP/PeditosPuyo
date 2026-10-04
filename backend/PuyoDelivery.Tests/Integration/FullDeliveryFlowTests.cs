using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class FullDeliveryFlowTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly NullTenantAccessor _accessor;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _jwt;

    private readonly AuthController _auth;
    private readonly DeliveryRequestsController _deliveryRequests;
    private readonly AssignmentsController _assignments;

    public FullDeliveryFlowTests()
    {
        _accessor = new NullTenantAccessor();
        _context = TestDb.CreateContext(_dbName, accessor: _accessor);
        _userManager = TestDb.CreateUserManager(_context);
        _jwt = new JwtTokenGenerator("unit-test-secret-key-32-bytes-minimum!!", "PuyoDelivery", "PuyoDeliveryApp");
        _tenantMock = new Mock<ICurrentTenantService>();

        _auth = new AuthController(_userManager, _jwt, _context);
        _deliveryRequests = new DeliveryRequestsController(
            _context,
            _tenantMock.Object,
            MockHub.Create<RestaurantHub>().Object,
            MockHub.Create<CompanyHub>().Object);
        _assignments = new AssignmentsController(
            _context,
            _tenantMock.Object,
            MockHub.Create<RiderHub>().Object,
            MockHub.Create<RestaurantHub>().Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetTenant(Guid? tenantId, string role, Guid? restaurantId = null, Guid? riderId = null)
    {
        _accessor.TenantId = tenantId;
        _tenantMock.SetupGet(t => t.TenantId).Returns(tenantId);
        _tenantMock.SetupGet(t => t.Role).Returns(role);
        _tenantMock.SetupGet(t => t.RestaurantId).Returns(restaurantId);
        _tenantMock.SetupGet(t => t.RiderId).Returns(riderId);
        _tenantMock.SetupGet(t => t.UserId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task HappyPath_CreateAssignAcceptTransitDeliver()
    {
        // 1. Register all three roles
        var restaurantReg = await _auth.Register(new RegisterRequest("Rest Owner", "flow-rest@test.com", "0991111111", "Test123!", "RestaurantAdmin"));
        var companyReg = await _auth.Register(new RegisterRequest("Comp Owner", "flow-comp@test.com", "0992222222", "Test123!", "CompanyAdmin"));
        var riderReg = await _auth.Register(new RegisterRequest("Flow Rider", "flow-rider@test.com", "0993333333", "Test123!", "Rider"));

        var restaurantLogin = (LoginResponse)((OkObjectResult)restaurantReg.Result!).Value!;
        var companyLogin = (LoginResponse)((OkObjectResult)companyReg.Result!).Value!;
        var riderLogin = (LoginResponse)((OkObjectResult)riderReg.Result!).Value!;

        restaurantLogin.Role.Should().Be("RestaurantAdmin");
        companyLogin.Role.Should().Be("CompanyAdmin");
        riderLogin.Role.Should().Be("Rider");
        restaurantLogin.RestaurantId.Should().NotBeNull();
        companyLogin.CompanyId.Should().NotBeNull();
        riderLogin.RiderId.Should().NotBeNull();

        // 2. Restaurant admin creates delivery request
        SetTenant(restaurantLogin.TenantId, "RestaurantAdmin", restaurantLogin.RestaurantId);
        var createResult = await _deliveryRequests.Create(new CreateDeliveryRequestRequest(
            restaurantLogin.RestaurantId!.Value,
            "Av. Principal 123 Puyo",
            -1.0465,
            -78.4684,
            "Entrega rápida"));

        var deliveryDto = (DeliveryRequestDto)((OkObjectResult)createResult.Result!).Value!;
        deliveryDto.Status.Should().Be("Pending");
        deliveryDto.RestaurantName.Should().Be("Rest Owner's Restaurant");

        // 3. Company admin sees pending
        SetTenant(companyLogin.TenantId, "CompanyAdmin");
        var pendingResult = await _deliveryRequests.GetPending();
        var pending = ((OkObjectResult)pendingResult.Result!).Value.Should().BeAssignableTo<IEnumerable<DeliveryRequestDto>>().Subject.ToList();
        pending.Should().Contain(p => p.Id == deliveryDto.Id);

        // 4. Company admin assigns rider
        var assignResult = await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, riderLogin.RiderId!.Value));
        var assignmentDto = (AssignmentDto)((OkObjectResult)assignResult.Result!).Value!;
        assignmentDto.Status.Should().Be("Pending");
        assignmentDto.RiderName.Should().Be("Flow Rider");

        var assignedRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == deliveryDto.Id);
        assignedRequest.Status.Should().Be(DeliveryRequestStatus.Assigned);
        assignedRequest.AssignedAt.Should().NotBeNull();

        var busyRider = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == riderLogin.RiderId);
        busyRider.IsBusy.Should().BeTrue();

        // 5. Rider accepts
        SetTenant(riderLogin.TenantId, "Rider", riderId: riderLogin.RiderId);
        var acceptResult = await _assignments.Accept(assignmentDto.Id);
        var accepted = (AssignmentDto)((OkObjectResult)acceptResult.Result!).Value!;
        accepted.Status.Should().Be("Accepted");
        accepted.AcceptedAt.Should().NotBeNull();

        _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == deliveryDto.Id)
            .Status.Should().Be(DeliveryRequestStatus.Accepted);

        // 6. Rider marks InTransit
        var transitResult = await _assignments.UpdateStatus(assignmentDto.Id, new UpdateStatusRequest("IN_TRANSIT"));
        transitResult.Should().BeOfType<NoContentResult>();
        _context.OrderAssignments.IgnoreQueryFilters().Single(a => a.Id == assignmentDto.Id)
            .Status.Should().Be(AssignmentStatus.InTransit);

        // 7. Rider delivers
        var deliverResult = await _assignments.UpdateStatus(assignmentDto.Id, new UpdateStatusRequest("DELIVERED"));
        deliverResult.Should().BeOfType<NoContentResult>();

        var finalRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == deliveryDto.Id);
        finalRequest.Status.Should().Be(DeliveryRequestStatus.Delivered);
        finalRequest.DeliveredAt.Should().NotBeNull();

        var finalAssignment = _context.OrderAssignments.IgnoreQueryFilters().Single(a => a.Id == assignmentDto.Id);
        finalAssignment.Status.Should().Be(AssignmentStatus.Delivered);
        finalAssignment.DeliveredAt.Should().NotBeNull();

        var freedRider = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == riderLogin.RiderId);
        freedRider.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task RejectFlow_RiderRejects_RequestReturnsToPending()
    {
        var restaurantReg = await _auth.Register(new RegisterRequest("R2", "rej-rest@test.com", "0991", "Test123!", "RestaurantAdmin"));
        var companyReg = await _auth.Register(new RegisterRequest("C2", "rej-comp@test.com", "0992", "Test123!", "CompanyAdmin"));
        var riderReg = await _auth.Register(new RegisterRequest("Rider2", "rej-rider@test.com", "0993", "Test123!", "Rider"));

        var restaurantLogin = (LoginResponse)((OkObjectResult)restaurantReg.Result!).Value!;
        var companyLogin = (LoginResponse)((OkObjectResult)companyReg.Result!).Value!;
        var riderLogin = (LoginResponse)((OkObjectResult)riderReg.Result!).Value!;

        SetTenant(restaurantLogin.TenantId, "RestaurantAdmin", restaurantLogin.RestaurantId);
        var createResult = await _deliveryRequests.Create(new CreateDeliveryRequestRequest(
            restaurantLogin.RestaurantId!.Value, "Calle 9", -1.0, -78.5, null));
        var deliveryDto = (DeliveryRequestDto)((OkObjectResult)createResult.Result!).Value!;

        SetTenant(companyLogin.TenantId, "CompanyAdmin");
        var assignResult = await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, riderLogin.RiderId!.Value));
        var assignmentDto = (AssignmentDto)((OkObjectResult)assignResult.Result!).Value!;

        SetTenant(riderLogin.TenantId, "Rider", riderId: riderLogin.RiderId);
        var rejectResult = await _assignments.Reject(assignmentDto.Id, new RejectAssignmentRequest("Muy lejos"));
        var rejected = (AssignmentDto)((OkObjectResult)rejectResult.Result!).Value!;
        rejected.Status.Should().Be("Rejected");
        rejected.RejectionReason.Should().Be("Muy lejos");

        _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == deliveryDto.Id)
            .Status.Should().Be(DeliveryRequestStatus.Pending);

        _context.Riders.IgnoreQueryFilters().Single(r => r.Id == riderLogin.RiderId)
            .IsBusy.Should().BeFalse();

        // Reassign to another rider should work
        var rider2Reg = await _auth.Register(new RegisterRequest("Rider3", "rej-rider3@test.com", "0994", "Test123!", "Rider"));
        var rider3Login = (LoginResponse)((OkObjectResult)rider2Reg.Result!).Value!;

        SetTenant(companyLogin.TenantId, "CompanyAdmin");
        var reassignResult = await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, rider3Login.RiderId!.Value));
        reassignResult.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CannotAssign_DeliveryTwice()
    {
        var restaurantReg = await _auth.Register(new RegisterRequest("R3", "d-rest@test.com", "0991", "Test123!", "RestaurantAdmin"));
        var companyReg = await _auth.Register(new RegisterRequest("C3", "d-comp@test.com", "0992", "Test123!", "CompanyAdmin"));
        var riderReg = await _auth.Register(new RegisterRequest("DRider", "d-rider@test.com", "0993", "Test123!", "Rider"));

        var restaurantLogin = (LoginResponse)((OkObjectResult)restaurantReg.Result!).Value!;
        var companyLogin = (LoginResponse)((OkObjectResult)companyReg.Result!).Value!;
        var riderLogin = (LoginResponse)((OkObjectResult)riderReg.Result!).Value!;

        SetTenant(restaurantLogin.TenantId, "RestaurantAdmin", restaurantLogin.RestaurantId);
        var createResult = await _deliveryRequests.Create(new CreateDeliveryRequestRequest(
            restaurantLogin.RestaurantId!.Value, "Calle 5", -1.0, -78.5, null));
        var deliveryDto = (DeliveryRequestDto)((OkObjectResult)createResult.Result!).Value!;

        SetTenant(companyLogin.TenantId, "CompanyAdmin");
        await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, riderLogin.RiderId!.Value));

        // Free rider so second attempt hits ALREADY_ASSIGNED, not RIDER_BUSY
        var riderEntity = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == riderLogin.RiderId);
        riderEntity.IsBusy = false;
        _context.SaveChanges();

        var secondAssign = await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, riderLogin.RiderId!.Value));
        var bad = secondAssign.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var code = bad.Value!.GetType().GetProperty("code")!.GetValue(bad.Value);
        code.Should().Be("ALREADY_ASSIGNED");
    }

    [Fact]
    public async Task CannotAssign_BusyRider()
    {
        var restaurantReg = await _auth.Register(new RegisterRequest("R4", "b-rest@test.com", "0991", "Test123!", "RestaurantAdmin"));
        var companyReg = await _auth.Register(new RegisterRequest("C4", "b-comp@test.com", "0992", "Test123!", "CompanyAdmin"));
        var riderReg = await _auth.Register(new RegisterRequest("BusyRider", "b-rider@test.com", "0993", "Test123!", "Rider"));

        var restaurantLogin = (LoginResponse)((OkObjectResult)restaurantReg.Result!).Value!;
        var companyLogin = (LoginResponse)((OkObjectResult)companyReg.Result!).Value!;
        var riderLogin = (LoginResponse)((OkObjectResult)riderReg.Result!).Value!;

        // Make rider busy
        var riderEntity = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == riderLogin.RiderId);
        riderEntity.IsBusy = true;
        _context.SaveChanges();

        SetTenant(restaurantLogin.TenantId, "RestaurantAdmin", restaurantLogin.RestaurantId);
        var createResult = await _deliveryRequests.Create(new CreateDeliveryRequestRequest(
            restaurantLogin.RestaurantId!.Value, "Calle 7", -1.0, -78.5, null));
        var deliveryDto = (DeliveryRequestDto)((OkObjectResult)createResult.Result!).Value!;

        SetTenant(companyLogin.TenantId, "CompanyAdmin");
        var assignResult = await _assignments.Create(new CreateAssignmentRequest(deliveryDto.Id, riderLogin.RiderId!.Value));
        var bad = assignResult.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var code = bad.Value!.GetType().GetProperty("code")!.GetValue(bad.Value);
        code.Should().Be("RIDER_BUSY");
    }
}
