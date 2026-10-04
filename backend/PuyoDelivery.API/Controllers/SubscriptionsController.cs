using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SubscriptionsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAll()
    {
        var subscriptions = await _context.Subscriptions
            .Select(s => new SubscriptionDto(
                s.Id, s.CompanyId, s.PlanName, s.PricePerMonth, s.RiderLimit,
                s.Status.ToString(), s.ExpiresAt))
            .ToListAsync();

        return Ok(subscriptions);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionDto>> GetById(Guid id)
    {
        var sub = await _context.Subscriptions.FindAsync(id);
        if (sub == null) return NotFound();

        return Ok(new SubscriptionDto(sub.Id, sub.CompanyId, sub.PlanName, sub.PricePerMonth, sub.RiderLimit, sub.Status.ToString(), sub.ExpiresAt));
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> Create([FromBody] CreateSubscriptionRequest request)
    {
        var company = await _context.DeliveryCompanies.FindAsync(request.CompanyId);
        if (company == null) return BadRequest(new { isSuccess = false, error = "Company not found" });

        var subscription = new Subscription
        {
            TenantId = company.TenantId,
            CompanyId = request.CompanyId,
            PlanName = request.PlanName,
            PricePerMonth = request.PricePerMonth,
            RiderLimit = request.RiderLimit,
            Status = SubscriptionStatus.Active,
            ExpiresAt = DateTime.UtcNow.AddMonths(request.Months)
        };

        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = subscription.Id },
            new SubscriptionDto(subscription.Id, subscription.CompanyId, subscription.PlanName,
                subscription.PricePerMonth, subscription.RiderLimit, subscription.Status.ToString(), subscription.ExpiresAt));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<PaymentDto>> CreatePayment(Guid id, [FromBody] CreatePaymentRequest request)
    {
        var subscription = await _context.Subscriptions.FindAsync(id);
        if (subscription == null) return NotFound();

        var method = request.Method.ToUpper() switch
        {
            "TRANSFER" => PaymentMethod.Transfer,
            "TICKET" => PaymentMethod.Ticket,
            _ => PaymentMethod.Transfer
        };

        var payment = new CompanyPayment
        {
            TenantId = subscription.TenantId,
            SubscriptionId = id,
            CompanyId = subscription.CompanyId,
            Amount = request.Amount,
            Method = method,
            Reference = request.Reference,
            Status = PaymentStatus.Confirmed,
            PaidAt = DateTime.UtcNow
        };

        _context.CompanyPayments.Add(payment);
        await _context.SaveChangesAsync();

        return Ok(new PaymentDto(payment.Id, payment.Amount, payment.Method.ToString(), payment.Reference, payment.Status.ToString(), payment.PaidAt));
    }

    [HttpGet("payments")]
    public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAllPayments()
    {
        var payments = await _context.CompanyPayments
            .Select(p => new PaymentDto(p.Id, p.Amount, p.Method.ToString(), p.Reference, p.Status.ToString(), p.PaidAt))
            .ToListAsync();

        return Ok(payments);
    }
}
