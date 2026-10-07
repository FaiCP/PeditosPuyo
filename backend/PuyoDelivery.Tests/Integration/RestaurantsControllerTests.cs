using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class RestaurantsControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly RestaurantsController _controller;

    private readonly Restaurant _restaurant;

    public RestaurantsControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _tenantMock = new Mock<ICurrentTenantService>();

        _restaurant = TestData.CreateRestaurant(TestData.TenantA, "Pizza Place");
        _context.Restaurants.Add(_restaurant);
        _context.SaveChanges();

        _tenantMock.SetupGet(t => t.TenantId).Returns(TestData.TenantA);

        _controller = new RestaurantsController(_context, _tenantMock.Object);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetAll_Public_ReturnsActiveOnly()
    {
        var inactive = TestData.CreateRestaurant(TestData.TenantA, "Closed Place", isActive: false);
        _context.Restaurants.Add(inactive);
        _context.SaveChanges();

        var result = await _controller.GetAll();

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<RestaurantDto>>().Subject.ToList();
        list.Should().ContainSingle();
        list[0].Name.Should().Be("Pizza Place");
        list[0].Lat.Should().Be(-1.0465);
        list[0].Lng.Should().Be(-78.4684);
    }

    [Fact]
    public async Task GetById_Found_ReturnsDto()
    {
        var result = await _controller.GetById(_restaurant.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<RestaurantDto>().Subject;
        dto.Name.Should().Be("Pizza Place");
        dto.Source.Should().Be("Manual");
    }

    [Fact]
    public async Task GetById_Missing_Returns404()
    {
        var result = await _controller.GetById(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ReturnsRestaurant()
    {
        var request = new CreateRestaurantRequest("Sushi King", "sushi-king", "Av. Amazonas", "0991234567", -1.05, -78.47, "Sushi");

        var result = await _controller.Create(request);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<RestaurantDto>().Subject;
        dto.Name.Should().Be("Sushi King");
        dto.Lat.Should().Be(-1.05);
        dto.Lng.Should().Be(-78.47);

        var saved = _context.Restaurants.IgnoreQueryFilters().Single(r => r.Id == dto.Id);
        saved.TenantId.Should().Be(TestData.TenantA);
    }

    [Fact]
    public async Task Update_ModifiesRestaurant()
    {
        var request = new UpdateRestaurantRequest("Pizza Place 2", "pizza-place-2", "Nueva dirección", "0999999999", -1.06, -78.48, "Italiana", true);

        var result = await _controller.Update(_restaurant.Id, request);

        result.Should().BeOfType<NoContentResult>();
        var saved = _context.Restaurants.IgnoreQueryFilters().Single(r => r.Id == _restaurant.Id);
        saved.Name.Should().Be("Pizza Place 2");
        saved.Address.Should().Be("Nueva dirección");
        saved.MenuSummary.Should().Be("Italiana");
    }

    [Fact]
    public async Task Update_Missing_Returns404()
    {
        var result = await _controller.Update(Guid.NewGuid(), new UpdateRestaurantRequest("X", "x", "x", "x", 0, 0, null, true));

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task AddMenuItem_And_GetMenu()
    {
        var createResult = await _controller.AddMenuItem(_restaurant.Id, new CreateMenuItemRequest("Hamburguesa", "Con queso", 4.50m));
        var ok = createResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var item = ok.Value.Should().BeOfType<MenuItemDto>().Subject;
        item.Name.Should().Be("Hamburguesa");
        item.Price.Should().Be(4.50m);

        var inactive = new MenuItem
        {
            TenantId = _restaurant.TenantId,
            RestaurantId = _restaurant.Id,
            Name = "Oculto",
            Price = 1m,
            IsActive = false
        };
        _context.MenuItems.Add(inactive);
        _context.SaveChanges();

        var menuResult = await _controller.GetMenu(_restaurant.Id);
        var menuOk = menuResult.Result.Should().BeOfType<OkObjectResult>().Subject;
        var menu = menuOk.Value.Should().BeAssignableTo<IEnumerable<MenuItemDto>>().Subject.ToList();
        menu.Should().ContainSingle();
        menu[0].Name.Should().Be("Hamburguesa");
    }

    [Fact]
    public async Task AddMenuItem_MissingRestaurant_Returns404()
    {
        var result = await _controller.AddMenuItem(Guid.NewGuid(), new CreateMenuItemRequest("X", null, 1m));

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task ImportCsv_ParsesRows_AndSkipsDuplicates()
    {
        var csv = "name,address,phone,lat,lng\nRest Uno,Calle 1,0991,-1.0,-78.5\nRest Dos,Calle 2,0992,-1.1,-78.6\n";
        var file = CreateFormFile(csv, "text/csv", "restaurants.csv");

        var result = await _controller.Import(file);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var importResult = ok.Value.Should().BeOfType<ImportResult>().Subject;
        importResult.Imported.Should().Be(2);
        importResult.Errors.Should().BeEmpty();

        _context.Restaurants.IgnoreQueryFilters().Where(r => r.Source == RestaurantSource.Scraper).Should().HaveCount(2);
    }

    [Fact]
    public async Task ImportCsv_ReportsMissingName()
    {
        var csv = "name,address,phone,lat,lng\n,Calle 1,0991,-1.0,-78.5\n";
        var file = CreateFormFile(csv, "text/csv", "bad.csv");

        var result = await _controller.Import(file);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var importResult = ok.Value.Should().BeOfType<ImportResult>().Subject;
        importResult.Imported.Should().Be(0);
        importResult.Errors.Should().ContainSingle().Which.Should().Contain("missing name");
    }

    [Fact]
    public async Task ImportJson_ParsesRestaurants()
    {
        var json = """[{"name":"JSON Rest","address":"Av. 1","phone":"0991","lat":-1.0,"lng":-78.5,"menuSummary":"Variado"}]""";
        var file = CreateFormFile(json, "application/json", "restaurants.json");

        var result = await _controller.Import(file);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var importResult = ok.Value.Should().BeOfType<ImportResult>().Subject;
        importResult.Imported.Should().Be(1);

        var saved = _context.Restaurants.IgnoreQueryFilters().Single(r => r.Name == "JSON Rest");
        saved.MenuSummary.Should().Be("Variado");
    }

    [Fact]
    public async Task ImportJson_WithoutCoords_UsesPuyoCenterAndCleansSeoName()
    {
        var json = """[{"id":42,"name":"LA HACIENDA RESTAURANTE - Restaurantes, Parrilladas en Puyo","address":"Via Puyo-Shell KM, Puyo","phone":"099 981 0999"}]""";
        var file = CreateFormFile(json, "application/json", "restaurantes_puyo.json");

        var result = await _controller.Import(file);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ImportResult>().Subject.Imported.Should().Be(1);

        var saved = _context.Restaurants.IgnoreQueryFilters().Single(r => r.ExternalId == "42");
        saved.Name.Should().Be("LA HACIENDA RESTAURANTE");
        saved.Location.Y.Should().Be(RestaurantsController.DefaultLat);
        saved.Location.X.Should().Be(RestaurantsController.DefaultLng);
        saved.Source.Should().Be(RestaurantSource.Scraper);
    }

    [Fact]
    public async Task Import_MissingFile_Returns400()
    {
        var result = await _controller.Import(null!);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static IFormFile CreateFormFile(string content, string contentType, string fileName)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }
}
