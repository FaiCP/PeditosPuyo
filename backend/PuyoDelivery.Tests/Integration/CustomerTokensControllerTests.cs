using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Dtos;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Tests.TestHelpers;

namespace PuyoDelivery.Tests.Integration;

public class CustomerTokensControllerTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentTenantService> _tenantMock;
    private readonly CustomerTokensController _controller;

    public CustomerTokensControllerTests()
    {
        _context = TestDb.CreateContext(_dbName);
        _tenantMock = new Mock<ICurrentTenantService>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["PublicAppBaseUrl"] = "https://app.test" })
            .Build();
        _controller = new CustomerTokensController(_context, _tenantMock.Object, config);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private void SetCompany(Guid tenantId)
    {
        _tenantMock.SetupGet(t => t.Role).Returns("CompanyAdmin");
        _tenantMock.SetupGet(t => t.TenantId).Returns(tenantId);
        _tenantMock.SetupGet(t => t.UserId).Returns(Guid.NewGuid());
    }

    [Fact]
    public async Task Create_ReturnsUrlSafeTokenAndPublicUrl()
    {
        SetCompany(TestData.TenantA);

        var result = await _controller.Create(new CreateCustomerTokenRequest("0991234567", "Juan"));

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<CustomerTokenDto>().Subject;
        dto.PublicUrl.Should().Be($"https://app.test/p/{dto.Token}");
        dto.Token.Should().NotBeNullOrWhiteSpace();
        dto.Token.Should().NotContain("=");

        var saved = _context.CustomerTokens.IgnoreQueryFilters().Single();
        saved.TenantId.Should().Be(TestData.TenantA);
        saved.Phone.Should().Be("0991234567");
    }

    [Fact]
    public async Task Create_MissingPhone_Returns400()
    {
        SetCompany(TestData.TenantA);

        var result = await _controller.Create(new CreateCustomerTokenRequest("  ", null));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_NoCompany_Returns400()
    {
        _tenantMock.SetupGet(t => t.Role).Returns("CompanyAdmin");
        _tenantMock.SetupGet(t => t.TenantId).Returns((Guid?)null);

        var result = await _controller.Create(new CreateCustomerTokenRequest("099", null));

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }
}
