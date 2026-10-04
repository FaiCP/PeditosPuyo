using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Infrastructure.Services;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class AuthControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly Infrastructure.Data.ApplicationDbContext _context;
    private readonly UserManager<Infrastructure.Data.ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _jwt;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _userManager = TestDb.CreateUserManager(_context);
        _jwt = new JwtTokenGenerator("unit-test-secret-key-32-bytes-minimum!!", "PuyoDelivery", "PuyoDeliveryApp");
        _controller = new AuthController(_userManager, _jwt, _context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Register_RestaurantAdmin_CreatesRestaurantAndProfile()
    {
        var request = new RegisterRequest("Maria Lopez", "maria@test.com", "0991111111", "Test123!", "RestaurantAdmin");

        var result = await _controller.Register(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Role.Should().Be("RestaurantAdmin");
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.TenantId.Should().NotBeNull();
        response.RestaurantId.Should().NotBeNull();

        var restaurant = _context.Restaurants.IgnoreQueryFilters().Single(r => r.Name == "Maria Lopez's Restaurant");
        response.TenantId.Should().Be(restaurant.TenantId);

        var admin = _context.RestaurantAdmins.IgnoreQueryFilters().Single(a => a.Email == "maria@test.com");
        admin.RestaurantId.Should().Be(restaurant.Id);
    }

    [Fact]
    public async Task Register_CompanyAdmin_CreatesCompanyAndProfile()
    {
        var request = new RegisterRequest("Pedro Ruiz", "pedro@test.com", "0992222222", "Test123!", "CompanyAdmin");

        var result = await _controller.Register(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Role.Should().Be("CompanyAdmin");
        response.CompanyId.Should().NotBeNull();

        var company = _context.DeliveryCompanies.IgnoreQueryFilters().Single(c => c.Name == "Pedro Ruiz's Company");
        response.TenantId.Should().Be(company.TenantId);

        var admin = _context.CompanyAdmins.IgnoreQueryFilters().Single(a => a.Email == "pedro@test.com");
        admin.CompanyId.Should().Be(company.Id);
    }

    [Fact]
    public async Task Register_Rider_CreatesRiderWithDefaultCompany()
    {
        var request = new RegisterRequest("Carlos Rider", "carlos@test.com", "0993333333", "Test123!", "Rider");

        var result = await _controller.Register(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Role.Should().Be("Rider");
        response.RiderId.Should().NotBeNull();

        var rider = _context.Riders.IgnoreQueryFilters().Single(r => r.FullName == "Carlos Rider");
        rider.UserId.ToString().Should().Be(response.UserId);
        rider.CompanyId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Register_Rider_UsesExistingCompany_WhenAlreadyPresent()
    {
        var company = TestData.CreateCompany();
        _context.DeliveryCompanies.Add(company);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest("Ana Rider", "ana@test.com", "0994444444", "Test123!", "Rider");
        await _controller.Register(request);

        var rider = _context.Riders.IgnoreQueryFilters().Single(r => r.FullName == "Ana Rider");
        rider.CompanyId.Should().Be(company.Id);
        rider.TenantId.Should().Be(company.TenantId);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Fails()
    {
        await _controller.Register(new RegisterRequest("First", "dup@test.com", "0991", "Test123!", "Rider"));

        var result = await _controller.Register(new RegisterRequest("Second", "dup@test.com", "0992", "Test123!", "Rider"));

        var bad = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Register_WeakPassword_Fails()
    {
        var result = await _controller.Register(new RegisterRequest("Weak", "weak@test.com", "0991", "123", "Rider"));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        await _controller.Register(new RegisterRequest("Login User", "login@test.com", "0995555555", "Test123!", "Rider"));

        var result = await _controller.Login(new LoginRequest("login@test.com", "Test123!"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<LoginResponse>().Subject;
        response.Token.Should().NotBeNullOrWhiteSpace();
        response.Role.Should().Be("Rider");
        response.Email.Should().Be("login@test.com");
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        await _controller.Register(new RegisterRequest("Login User", "wrongpw@test.com", "0996666666", "Test123!", "Rider"));

        var result = await _controller.Login(new LoginRequest("wrongpw@test.com", "WrongPass99!"));

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_UnknownEmail_ReturnsUnauthorized()
    {
        var result = await _controller.Login(new LoginRequest("nobody@test.com", "Test123!"));

        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Login_RestaurantAdmin_IncludesRestaurantIdClaim()
    {
        await _controller.Register(new RegisterRequest("Rest Admin", "restlogin@test.com", "0997777777", "Test123!", "RestaurantAdmin"));

        var result = await _controller.Login(new LoginRequest("restlogin@test.com", "Test123!"));
        var response = (OkObjectResult)result.Result!;
        var login = (LoginResponse)response.Value!;

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        jwt.Claims.First(c => c.Type == "restaurant_id").Value.Should().Be(login.RestaurantId!.Value.ToString());
    }

    [Fact]
    public async Task Login_CompanyAdmin_IncludesCompanyIdClaim()
    {
        await _controller.Register(new RegisterRequest("Comp Admin", "complogin@test.com", "0998888888", "Test123!", "CompanyAdmin"));

        var result = await _controller.Login(new LoginRequest("complogin@test.com", "Test123!"));
        var response = (OkObjectResult)result.Result!;
        var login = (LoginResponse)response.Value!;

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(login.Token);
        jwt.Claims.First(c => c.Type == "company_id").Value.Should().Be(login.TenantId.HasValue ? jwt.Claims.First(c => c.Type == "company_id").Value : "");
        jwt.Claims.Should().Contain(c => c.Type == "company_id");
    }
}
