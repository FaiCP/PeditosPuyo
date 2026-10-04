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

public class AssignmentsControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly AssignmentsController _controller;

    private readonly Restaurant _restaurant;
    private readonly DeliveryCompany _company;
    private readonly Rider _rider;
    private readonly Guid _adminUserId;

    public AssignmentsControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _tenantMock = new Mock<ICurrentTenantService>();
        _adminUserId = Guid.NewGuid();

        _restaurant = TestData.CreateRestaurant();
        _company = TestData.CreateCompany();
        _rider = TestData.CreateRider(_company);
        _context.Restaurants.Add(_restaurant);
        _context.DeliveryCompanies.Add(_company);
        _context.Riders.Add(_rider);
        _context.SaveChanges();

        _tenantMock.SetupGet(t => t.TenantId).Returns(_company.TenantId);
        _tenantMock.SetupGet(t => t.UserId).Returns(_adminUserId);
        _tenantMock.SetupGet(t => t.Role).Returns("CompanyAdmin");

        _controller = new AssignmentsController(
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

    private DeliveryRequest SeedRequest(DeliveryRequestStatus status = DeliveryRequestStatus.Pending)
    {
        var request = TestData.CreateDeliveryRequest(_restaurant, status: status);
        _context.DeliveryRequests.Add(request);
        _context.SaveChanges();
        return request;
    }

    private void SetRider(Guid riderId)
    {
        _tenantMock.SetupGet(t => t.RiderId).Returns(riderId);
        _tenantMock.SetupGet(t => t.Role).Returns("Rider");
    }

    [Fact]
    public async Task Create_AssignsRequest_AndMarksRiderBusy()
    {
        var request = SeedRequest();

        var result = await _controller.Create(new CreateAssignmentRequest(request.Id, _rider.Id));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<AssignmentDto>().Subject;
        dto.Status.Should().Be("Pending");
        dto.RiderName.Should().Be(_rider.FullName);

        var savedRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == request.Id);
        savedRequest.Status.Should().Be(DeliveryRequestStatus.Assigned);
        savedRequest.AssignedAt.Should().NotBeNull();

        var savedRider = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == _rider.Id);
        savedRider.IsBusy.Should().BeTrue();
    }

    [Fact]
    public async Task Create_BusyRider_Returns400()
    {
        var request = SeedRequest();
        _rider.IsBusy = true;
        _context.SaveChanges();

        var result = await _controller.Create(new CreateAssignmentRequest(request.Id, _rider.Id));

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var payload = bad.Value!.GetType().GetProperty("code")!.GetValue(bad.Value);
        payload.Should().Be("RIDER_BUSY");
    }

    [Fact]
    public async Task Create_DuplicateAssignment_Returns400()
    {
        var request = SeedRequest();
        var first = TestData.CreateAssignment(request, _rider, _adminUserId);
        _context.OrderAssignments.Add(first);
        request.Status = DeliveryRequestStatus.Assigned;
        _context.SaveChanges();

        // Free the rider so we hit ALREADY_ASSIGNED instead of RIDER_BUSY
        _rider.IsBusy = false;
        _context.SaveChanges();

        var result = await _controller.Create(new CreateAssignmentRequest(request.Id, _rider.Id));

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var payload = bad.Value!.GetType().GetProperty("code")!.GetValue(bad.Value);
        payload.Should().Be("ALREADY_ASSIGNED");
    }

    [Fact]
    public async Task Create_UnknownRequest_Returns404()
    {
        var result = await _controller.Create(new CreateAssignmentRequest(Guid.NewGuid(), _rider.Id));

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Create_UnknownRider_Returns404()
    {
        var request = SeedRequest();

        var result = await _controller.Create(new CreateAssignmentRequest(request.Id, Guid.NewGuid()));

        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Accept_SetsAccepted_AndRequestAccepted()
    {
        var request = SeedRequest();
        var assignment = TestData.CreateAssignment(request, _rider, _adminUserId);
        _context.OrderAssignments.Add(assignment);
        request.Status = DeliveryRequestStatus.Assigned;
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.Accept(assignment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<AssignmentDto>().Subject;
        dto.Status.Should().Be("Accepted");
        dto.AcceptedAt.Should().NotBeNull();

        var savedRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == request.Id);
        savedRequest.Status.Should().Be(DeliveryRequestStatus.Accepted);
        savedRequest.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Accept_WrongRider_Returns403()
    {
        var request = SeedRequest();
        var assignment = TestData.CreateAssignment(request, _rider, _adminUserId);
        _context.OrderAssignments.Add(assignment);
        _context.SaveChanges();
        SetRider(Guid.NewGuid());

        var result = await _controller.Accept(assignment.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Reject_SetsRejected_RequestBackToPending_RiderFreed()
    {
        var request = SeedRequest(DeliveryRequestStatus.Assigned);
        var assignment = TestData.CreateAssignment(request, _rider, _adminUserId);
        _context.OrderAssignments.Add(assignment);
        _rider.IsBusy = true;
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.Reject(assignment.Id, new RejectAssignmentRequest("Muy lejos"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<AssignmentDto>().Subject;
        dto.Status.Should().Be("Rejected");
        dto.RejectionReason.Should().Be("Muy lejos");

        var savedRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == request.Id);
        savedRequest.Status.Should().Be(DeliveryRequestStatus.Pending);
        savedRequest.AssignedAt.Should().BeNull();

        var savedRider = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == _rider.Id);
        savedRider.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateStatus_InTransit()
    {
        var request = SeedRequest(DeliveryRequestStatus.Accepted);
        var assignment = TestData.CreateAssignment(request, _rider, _adminUserId, AssignmentStatus.Accepted);
        _context.OrderAssignments.Add(assignment);
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.UpdateStatus(assignment.Id, new UpdateStatusRequest("IN_TRANSIT"));

        result.Should().BeOfType<NoContentResult>();
        var saved = _context.OrderAssignments.IgnoreQueryFilters().Single(a => a.Id == assignment.Id);
        saved.Status.Should().Be(AssignmentStatus.InTransit);
    }

    [Fact]
    public async Task UpdateStatus_Delivered_FreesRider_AndDeliversRequest()
    {
        var request = SeedRequest(DeliveryRequestStatus.InTransit);
        var assignment = TestData.CreateAssignment(request, _rider, _adminUserId, AssignmentStatus.InTransit);
        _context.OrderAssignments.Add(assignment);
        _rider.IsBusy = true;
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.UpdateStatus(assignment.Id, new UpdateStatusRequest("DELIVERED"));

        result.Should().BeOfType<NoContentResult>();

        var savedAssignment = _context.OrderAssignments.IgnoreQueryFilters().Single(a => a.Id == assignment.Id);
        savedAssignment.Status.Should().Be(AssignmentStatus.Delivered);
        savedAssignment.DeliveredAt.Should().NotBeNull();

        var savedRequest = _context.DeliveryRequests.IgnoreQueryFilters().Single(r => r.Id == request.Id);
        savedRequest.Status.Should().Be(DeliveryRequestStatus.Delivered);
        savedRequest.DeliveredAt.Should().NotBeNull();

        var savedRider = _context.Riders.IgnoreQueryFilters().Single(r => r.Id == _rider.Id);
        savedRider.IsBusy.Should().BeFalse();
    }

    [Fact]
    public async Task GetRiderPending_ReturnsPendingAssignments()
    {
        var request = SeedRequest();
        var pending = TestData.CreateAssignment(request, _rider, _adminUserId, AssignmentStatus.Pending);
        _context.OrderAssignments.Add(pending);
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.GetRiderPending();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<AssignmentDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Id.Should().Be(pending.Id);
    }

    [Fact]
    public async Task GetRiderActive_ReturnsAcceptedOrInTransit()
    {
        var request1 = SeedRequest();
        var request2 = SeedRequest();
        _context.OrderAssignments.AddRange(
            TestData.CreateAssignment(request1, _rider, _adminUserId, AssignmentStatus.Accepted),
            TestData.CreateAssignment(request2, _rider, _adminUserId, AssignmentStatus.Pending));
        _context.SaveChanges();
        SetRider(_rider.Id);

        var result = await _controller.GetRiderActive();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<AssignmentDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Status.Should().Be("Accepted");
    }
}
