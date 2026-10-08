using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class RestaurantManagementControllersTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly RestaurantProfileController _profileController;
    private readonly RestaurantMenuController _menuController;
    private readonly DeliveryCompany _company;
    private readonly Restaurant _restaurant;
    private readonly ApplicationUser _user;

    public RestaurantManagementControllersTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _userManager = TestDb.CreateUserManager(_context);
        _tenantMock = new Mock<ICurrentTenantService>();

        _company = TestData.CreateCompany(name: "Platform Company");
        _context.DeliveryCompanies.Add(_company);
        _context.SaveChanges();

        _restaurant = TestData.CreateRestaurant(_company.TenantId, "Managed Restaurant");
        _context.Restaurants.Add(_restaurant);
        _context.SaveChanges();

        _user = new ApplicationUser
        {
            UserName = "restadmin@test.com",
            Email = "restadmin@test.com",
            FullName = "Rest Admin",
            Phone = "0991111111",
            Role = "RestaurantAdmin",
            TenantId = _company.TenantId
        };
        _userManager.CreateAsync(_user, "Test123!").GetAwaiter().GetResult();

        _context.RestaurantAdmins.Add(new RestaurantAdmin
        {
            TenantId = _company.TenantId,
            UserId = Guid.Parse(_user.Id),
            RestaurantId = _restaurant.Id,
            FullName = _user.FullName,
            Email = _user.Email,
            Phone = _user.Phone
        });
        _context.SaveChanges();

        _tenantMock.SetupGet(t => t.UserId).Returns(Guid.Parse(_user.Id));
        _tenantMock.SetupGet(t => t.TenantId).Returns(_company.TenantId);

        _profileController = new RestaurantProfileController(_context, _tenantMock.Object);
        _menuController = new RestaurantMenuController(_context, _tenantMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Profile_Get_ReturnsRestaurant()
    {
        var result = await _profileController.Get();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<RestaurantDto>().Subject;
        dto.Id.Should().Be(_restaurant.Id);
        dto.Name.Should().Be(_restaurant.Name);
    }

    [Fact]
    public async Task Profile_Update_ModifiesFields()
    {
        var request = new UpdateRestaurantProfileRequest(
            "Updated Name", "New Address", "0999999999", "Pizza", "https://logo.png", "https://qr.png");

        var result = await _profileController.Update(request);

        result.Should().BeOfType<NoContentResult>();
        var saved = _context.Restaurants.IgnoreQueryFilters().Single(r => r.Id == _restaurant.Id);
        saved.Name.Should().Be("Updated Name");
        saved.Address.Should().Be("New Address");
        saved.LogoUrl.Should().Be("https://logo.png");
        saved.PaymentQrUrl.Should().Be("https://qr.png");
    }

    [Fact]
    public async Task Menu_Get_ReturnsItems()
    {
        _context.MenuItems.Add(new MenuItem
        {
            TenantId = _company.TenantId,
            RestaurantId = _restaurant.Id,
            Name = "Burger",
            Price = 4.50m
        });
        await _context.SaveChangesAsync();

        var result = await _menuController.GetMenu();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<MenuItemDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Name.Should().Be("Burger");
    }

    [Fact]
    public async Task Menu_Create_AddsItem()
    {
        var result = await _menuController.Create(new CreateMenuItemRequest("Taco", "Con salsa", 2.50m, "https://taco.png"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<MenuItemDto>().Subject;
        dto.Name.Should().Be("Taco");
        dto.ImageUrl.Should().Be("https://taco.png");

        _context.MenuItems.IgnoreQueryFilters().Count(m => m.RestaurantId == _restaurant.Id).Should().Be(1);
    }

    [Fact]
    public async Task Menu_Update_ChangesItem()
    {
        var item = new MenuItem
        {
            TenantId = _company.TenantId,
            RestaurantId = _restaurant.Id,
            Name = "Old",
            Price = 1m,
            IsActive = true
        };
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        var result = await _menuController.Update(item.Id, new UpdateMenuItemRequest("New", "Desc", 2m, false, null));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<MenuItemDto>().Subject;
        dto.Name.Should().Be("New");
        dto.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Menu_Delete_RemovesItem()
    {
        var item = new MenuItem
        {
            TenantId = _company.TenantId,
            RestaurantId = _restaurant.Id,
            Name = "ToDelete",
            Price = 1m
        };
        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        var result = await _menuController.Delete(item.Id);

        result.Should().BeOfType<NoContentResult>();
        _context.MenuItems.IgnoreQueryFilters().Any(m => m.Id == item.Id).Should().BeFalse();
    }
}
