using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class RestaurantClaimsControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RestaurantClaimsController _controller;
    private readonly DeliveryCompany _company;
    private readonly Restaurant _targetRestaurant;

    public RestaurantClaimsControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _userManager = TestDb.CreateUserManager(_context);
        _controller = new RestaurantClaimsController(_context, _userManager);

        _company = TestData.CreateCompany(name: "Claims Company");
        _context.DeliveryCompanies.Add(_company);
        _context.SaveChanges();

        _targetRestaurant = TestData.CreateRestaurant(_company.TenantId, "Claimable Restaurant");
        _context.Restaurants.Add(_targetRestaurant);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Claimable_ReturnsRestaurantWithoutAdmin()
    {
        var result = await _controller.Claimable();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<ClaimableRestaurantDto>>().Subject.ToList();
        list.Should().ContainSingle(r => r.Id == _targetRestaurant.Id);
    }

    [Fact]
    public async Task Submit_CreatesUserAndPendingClaim()
    {
        var request = new SubmitRestaurantClaimRequest(
            _targetRestaurant.Id, "Owner", "owner@test.com", "0992222222", "Test123!");

        var result = await _controller.Submit(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<RestaurantClaimDto>().Subject;
        dto.Status.Should().Be("Pending");
        dto.RestaurantId.Should().Be(_targetRestaurant.Id);

        _context.RestaurantClaims.IgnoreQueryFilters().Should().ContainSingle();
        _context.RestaurantAdmins.IgnoreQueryFilters().Should().ContainSingle();
        var createdUser = await _userManager.FindByEmailAsync("owner@test.com");
        createdUser.Should().NotBeNull();
    }

    [Fact]
    public async Task Submit_WithExistingAdmin_ReturnsBadRequest()
    {
        var user = new ApplicationUser
        {
            UserName = "existing@test.com",
            Email = "existing@test.com",
            FullName = "Existing",
            Phone = "0993333333",
            Role = "RestaurantAdmin",
            TenantId = _company.TenantId
        };
        await _userManager.CreateAsync(user, "Test123!");
        _context.RestaurantAdmins.Add(new RestaurantAdmin
        {
            TenantId = _company.TenantId,
            UserId = Guid.Parse(user.Id),
            RestaurantId = _targetRestaurant.Id,
            FullName = "Existing",
            Email = "existing@test.com",
            Phone = "0993333333"
        });
        await _context.SaveChangesAsync();

        var request = new SubmitRestaurantClaimRequest(
            _targetRestaurant.Id, "Owner", "owner2@test.com", "0994444444", "Test123!");

        var result = await _controller.Submit(request);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Approve_ReassignsAdminToTargetRestaurant()
    {
        var submitResult = await _controller.Submit(new SubmitRestaurantClaimRequest(
            _targetRestaurant.Id, "Owner", "owner3@test.com", "0995555555", "Test123!"));
        var claimId = ((RestaurantClaimDto)((OkObjectResult)submitResult.Result!)!.Value!).Id;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
                }))
            }
        };

        var result = await _controller.Approve(claimId, null);

        result.Should().BeOfType<NoContentResult>();

        var admin = _context.RestaurantAdmins.IgnoreQueryFilters().Single();
        admin.RestaurantId.Should().Be(_targetRestaurant.Id);
        admin.TenantId.Should().Be(_targetRestaurant.TenantId);

        var claim = _context.RestaurantClaims.IgnoreQueryFilters().Single(c => c.Id == claimId);
        claim.Status.Should().Be(RestaurantClaimStatus.Approved);
    }
}
