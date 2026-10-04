using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class SubscriptionsControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly SubscriptionsController _controller;
    private readonly DeliveryCompany _company;

    public SubscriptionsControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _company = TestData.CreateCompany();
        _context.DeliveryCompanies.Add(_company);
        _context.SaveChanges();

        _controller = new SubscriptionsController(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Create_ValidCompany_ReturnsSubscription()
    {
        var request = new CreateSubscriptionRequest(_company.Id, "Pro", 200m, 20, 3);

        var result = await _controller.Create(request);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var dto = created.Value.Should().BeOfType<SubscriptionDto>().Subject;
        dto.PlanName.Should().Be("Pro");
        dto.PricePerMonth.Should().Be(200m);
        dto.RiderLimit.Should().Be(20);
        dto.Status.Should().Be("Active");
        dto.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddDays(89));
        dto.ExpiresAt.Should().BeBefore(DateTime.UtcNow.AddDays(92));
    }

    [Fact]
    public async Task Create_UnknownCompany_Returns400()
    {
        var request = new CreateSubscriptionRequest(Guid.NewGuid(), "Pro", 200m, 10, 1);

        var result = await _controller.Create(request);

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var sub = TestData.CreateSubscription(_company);
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();

        var result = await _controller.GetById(sub.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<SubscriptionDto>().Subject;
        dto.CompanyId.Should().Be(_company.Id);
        dto.PlanName.Should().Be("Básico");
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var result = await _controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAll_ReturnsAllSubscriptions()
    {
        _context.Subscriptions.AddRange(
            TestData.CreateSubscription(_company),
            TestData.CreateSubscription(_company));
        _context.SaveChanges();

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<SubscriptionDto>>().Subject.ToList();
        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreatePayment_Transfer_ConfirmsPayment()
    {
        var sub = TestData.CreateSubscription(_company);
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();

        var result = await _controller.CreatePayment(sub.Id, new CreatePaymentRequest(150m, "transfer", "TRF-001"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PaymentDto>().Subject;
        dto.Amount.Should().Be(150m);
        dto.Method.Should().Be("Transfer");
        dto.Reference.Should().Be("TRF-001");
        dto.Status.Should().Be("Confirmed");
        dto.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePayment_Ticket_MapsToTicket()
    {
        var sub = TestData.CreateSubscription(_company);
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();

        var result = await _controller.CreatePayment(sub.Id, new CreatePaymentRequest(150m, "ticket", "TCK-002"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PaymentDto>().Subject;
        dto.Method.Should().Be("Ticket");
    }

    [Fact]
    public async Task CreatePayment_UnknownMethod_DefaultsToTransfer()
    {
        var sub = TestData.CreateSubscription(_company);
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();

        var result = await _controller.CreatePayment(sub.Id, new CreatePaymentRequest(150m, "crypto", "XYZ"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PaymentDto>().Subject;
        dto.Method.Should().Be("Transfer");
    }

    [Fact]
    public async Task CreatePayment_MissingSubscription_Returns404()
    {
        var result = await _controller.CreatePayment(Guid.NewGuid(), new CreatePaymentRequest(150m, "transfer", "X"));

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetAllPayments_ReturnsPayments()
    {
        var sub = TestData.CreateSubscription(_company);
        _context.Subscriptions.Add(sub);
        _context.SaveChanges();
        _context.CompanyPayments.Add(new CompanyPayment
        {
            TenantId = _company.TenantId,
            SubscriptionId = sub.Id,
            CompanyId = _company.Id,
            Amount = 150m,
            Method = PaymentMethod.Transfer,
            Reference = "TRF-001",
            Status = PaymentStatus.Confirmed,
            PaidAt = DateTime.UtcNow
        });
        _context.SaveChanges();

        var result = await _controller.GetAllPayments();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<PaymentDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Reference.Should().Be("TRF-001");
    }
}
