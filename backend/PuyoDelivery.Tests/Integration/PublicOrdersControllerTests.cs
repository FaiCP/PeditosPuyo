using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Services;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class PublicOrdersControllerTests : IDisposable
{
    // Cada operación usa su propio ApplicationDbContext sobre la misma base InMemory,
    // replicando el scoping por-request real (los controllers nunca comparten tracker).
    private readonly string _dbName = Guid.NewGuid().ToString();

    private ApplicationDbContext NewContext() => TestDb.CreateContext(_dbName);
    private PublicOrdersController NewController()
    {
        var ctx = NewContext();
        return new PublicOrdersController(ctx, new OrderService(ctx));
    }

    public void Dispose()
    {
        using var ctx = NewContext();
        ctx.Database.EnsureDeleted();
    }

    private async Task<(Restaurant restaurant, CustomerToken token)> SeedAsync(
        Guid? tenantId = null, string? qr = null)
    {
        using var ctx = NewContext();
        var restaurant = TestData.CreateRestaurant(tenantId ?? TestData.TenantA);
        restaurant.PaymentQrUrl = qr;
        restaurant.MenuItems.Add(TestData.CreateMenuItem(restaurant, "Hamburguesa", 5.00m));
        restaurant.MenuItems.Add(TestData.CreateMenuItem(restaurant, "Papas", 2.50m));
        ctx.Restaurants.Add(restaurant);

        var token = TestData.CreateCustomerToken(restaurant.TenantId);
        ctx.CustomerTokens.Add(token);
        await ctx.SaveChangesAsync();
        return (restaurant, token);
    }

    private static CreateOrderRequest RestaurantOrder(Restaurant r, CustomerToken t, string payment = "cash") => new(
        Type: "restaurant",
        RestaurantId: r.Id,
        OriginLat: null, OriginLng: null, OriginName: null, OriginAddress: null,
        DestinationAddress: "Mi casa 123",
        DestinationLat: -1.0470, DestinationLng: -78.4690,
        DestinationLinkRaw: null,
        Description: null,
        Items: new List<OrderItemLine> { new(r.MenuItems.First().Id, "", 2, 0, null) },
        CustomerName: "Ana", CustomerPhone: "0990001112",
        PaymentMethod: payment);

    [Fact]
    public async Task Catalog_ValidToken_ReturnsActiveRestaurantsOfTenant()
    {
        var (r, token) = await SeedAsync();
        using (var ctx = NewContext())
        {
            ctx.Restaurants.Add(TestData.CreateRestaurant(TestData.TenantB, "Otro"));
            await ctx.SaveChangesAsync();
        }

        var result = await NewController().Catalog(token.Token);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<PublicRestaurantDto>>().Subject.ToList();
        list.Should().HaveCount(1);
        list[0].Name.Should().Be(r.Name);
        list[0].ItemCount.Should().Be(2);
    }

    [Fact]
    public async Task Catalog_InvalidToken_Returns404()
    {
        var result = await NewController().Catalog("no-existe");
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task RestaurantMenu_ReturnsOnlyActiveItems()
    {
        var (r, token) = await SeedAsync();
        using (var ctx = NewContext())
        {
            var rest = ctx.Restaurants.IgnoreQueryFilters().Single(x => x.Id == r.Id);
            var inactive = TestData.CreateMenuItem(rest, "Plato viejo", 3m);
            inactive.IsActive = false;
            ctx.MenuItems.Add(inactive); // FK explícita, no navegación de colección
            await ctx.SaveChangesAsync();
        }

        var result = await NewController().RestaurantMenu(token.Token, r.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<PublicRestaurantDetailDto>().Subject;
        dto.Menu.Should().HaveCount(2);
        dto.PaymentQrUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateOrder_Restaurant_SetsWaitingRiderAndAmounts()
    {
        var (r, token) = await SeedAsync();

        var result = await NewController().CreateOrder(token.Token, RestaurantOrder(r, token));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        dto.Status.Should().Be("WaitingRider");
        dto.Type.Should().Be("restaurant");
        dto.ProductsAmount.Should().Be(10.00m); // 2 x 5.00
        dto.DeliveryFeeAmount.Should().Be(OrderService.DefaultDeliveryFee);
        dto.TotalAmount.Should().Be(10.00m + OrderService.DefaultDeliveryFee);

        using var ctx = NewContext();
        var saved = ctx.Orders.IgnoreQueryFilters().Include(o => o.Events).Single(o => o.Id == dto.Id);
        saved.TenantId.Should().Be(token.TenantId);
        saved.OriginName.Should().Be(r.Name);
        saved.Events.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateOrder_RestaurantQR_WithoutQr_Returns400()
    {
        var (r, token) = await SeedAsync(qr: null);
        var result = await NewController().CreateOrder(token.Token, RestaurantOrder(r, token, "restaurantqr"));
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_RestaurantQR_WithQr_Succeeds()
    {
        var (r, token) = await SeedAsync(qr: "https://qr.example.com/123");

        var result = await NewController().CreateOrder(token.Token, RestaurantOrder(r, token, "restaurantqr"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        dto.PaymentMethod.Should().Be("restaurantqr");
        dto.HasPaymentQr.Should().Be(true);
        dto.PaymentQrUrl.Should().Be("https://qr.example.com/123");
    }

    [Fact]
    public async Task CreateOrder_BogusMenuItem_Returns400()
    {
        var (r, token) = await SeedAsync();
        var req = RestaurantOrder(r, token) with
        {
            Items = new List<OrderItemLine> { new(Guid.NewGuid(), "Falso", 1, 0, null) }
        };

        var result = await NewController().CreateOrder(token.Token, req);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_Compra_WithItems_ComputesAmounts()
    {
        var (_, token) = await SeedAsync();
        var req = new CreateOrderRequest(
            "compra", null, -1.0450, -78.4680, "Farmacia", "Av. Main",
            "Mi casa", -1.0470, -78.4690, null, "Cerca para la gripe",
            new List<OrderItemLine> { new(null, "Paracetamol", 2, 1.25m, null) },
            "Luis", "0991", "cash");

        var result = await NewController().CreateOrder(token.Token, req);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        dto.Type.Should().Be("compra");
        dto.ProductsAmount.Should().Be(2.50m);
    }

    [Fact]
    public async Task CreateOrder_Encargo_MissingOrigin_Returns400()
    {
        var (_, token) = await SeedAsync();
        var req = new CreateOrderRequest(
            "encargo", null, null, null, null, null,
            "Mi casa", -1.0470, -78.4690, null, "Llevar documento",
            null, "Luis", "0991", "cash");

        var result = await NewController().CreateOrder(token.Token, req);
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateOrder_Encargo_WithDescription_SucceedsCash()
    {
        var (_, token) = await SeedAsync();
        var req = new CreateOrderRequest(
            "encargo", null, -1.0450, -78.4680, "Oficina", "Calle Trabajo",
            "Mi casa", -1.0470, -78.4690, null, "Traer el sobre azul",
            null, "Luis", "0991", "cash");

        var result = await NewController().CreateOrder(token.Token, req);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OrderTrackingDto>().Subject;
        dto.Type.Should().Be("encargo");
        dto.PaymentMethod.Should().Be("cash");
        dto.ProductsAmount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOrder_InvalidToken_Returns404()
    {
        var (r, token) = await SeedAsync();
        var result = await NewController().CreateOrder("mal", RestaurantOrder(r, token));
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task MyOrders_ReturnsOnlyThatTokensOrders()
    {
        var (r, token) = await SeedAsync();
        CustomerToken otherToken;
        using (var ctx = NewContext())
        {
            otherToken = TestData.CreateCustomerToken(token.TenantId);
            ctx.CustomerTokens.Add(otherToken);
            await ctx.SaveChangesAsync();
        }

        await NewController().CreateOrder(token.Token, RestaurantOrder(r, token));
        await NewController().CreateOrder(otherToken.Token, RestaurantOrder(r, otherToken));

        var result = await NewController().MyOrders(token.Token);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<IEnumerable<OrderTrackingDto>>().Subject.ToList();
        list.Should().HaveCount(1);
    }

    [Fact]
    public async Task Cancel_BeforePickup_Succeeds()
    {
        var (r, token) = await SeedAsync();
        var created = await NewController().CreateOrder(token.Token, RestaurantOrder(r, token));
        var id = ((OrderTrackingDto)((OkObjectResult)created.Result!).Value!).Id;

        var result = await NewController().Cancel(token.Token, id);

        result.Should().BeOfType<NoContentResult>();
        using var ctx = NewContext();
        var saved = ctx.Orders.IgnoreQueryFilters().Single(o => o.Id == id);
        saved.Status.Should().Be(OrderStatus.Cancelled);
        saved.CancelReason.Should().Be(OrderCancelReason.CustomerCancelled);
    }

    [Fact]
    public async Task Cancel_AfterPickup_Returns400()
    {
        var (r, token) = await SeedAsync();
        var created = await NewController().CreateOrder(token.Token, RestaurantOrder(r, token));
        var id = ((OrderTrackingDto)((OkObjectResult)created.Result!).Value!).Id;
        using (var ctx = NewContext())
        {
            var order = ctx.Orders.IgnoreQueryFilters().Single(o => o.Id == id);
            order.Status = OrderStatus.PickedUp;
            await ctx.SaveChangesAsync();
        }

        var result = await NewController().Cancel(token.Token, id);
        result.Should().BeOfType<BadRequestObjectResult>();
    }
}
