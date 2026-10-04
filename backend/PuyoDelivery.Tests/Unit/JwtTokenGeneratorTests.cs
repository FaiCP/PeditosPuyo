using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.Tests.Unit;

public class JwtTokenGeneratorTests
{
    private const string Secret = "unit-test-secret-key-32-bytes-minimum!!";
    private const string Issuer = "PuyoDelivery";
    private const string Audience = "PuyoDeliveryApp";

    private static JwtTokenGenerator CreateGenerator() => new(Secret, Issuer, Audience);

    [Fact]
    public void GenerateToken_ReturnsNonEmptyToken()
    {
        var token = CreateGenerator().GenerateToken("user-1", "test@test.com", "Rider", null);

        token.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GenerateToken_ContainsBasicClaims()
    {
        var token = CreateGenerator().GenerateToken("user-1", "test@test.com", "Rider", null);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be("user-1");
        jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value.Should().Be("test@test.com");
        jwt.Claims.First(c => c.Type == "role").Value.Should().Be("Rider");
    }

    [Fact]
    public void GenerateToken_ContainsTenantId_WhenProvided()
    {
        var tenantId = Guid.NewGuid();
        var token = CreateGenerator().GenerateToken("user-1", "a@b.com", "CompanyAdmin", tenantId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == "tenant_id").Value.Should().Be(tenantId.ToString());
    }

    [Fact]
    public void GenerateToken_OmitsTenantId_WhenNull()
    {
        var token = CreateGenerator().GenerateToken("user-1", "a@b.com", "Rider", null);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == "tenant_id");
    }

    [Fact]
    public void GenerateToken_ContainsOptionalProfileClaims()
    {
        var companyId = Guid.NewGuid();
        var restaurantId = Guid.NewGuid();
        var riderId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var token = CreateGenerator().GenerateToken("u1", "a@b.com", "RestaurantAdmin", tenantId, companyId, restaurantId, riderId);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.First(c => c.Type == "company_id").Value.Should().Be(companyId.ToString());
        jwt.Claims.First(c => c.Type == "restaurant_id").Value.Should().Be(restaurantId.ToString());
        jwt.Claims.First(c => c.Type == "rider_id").Value.Should().Be(riderId.ToString());
    }

    [Fact]
    public void GenerateToken_SetsIssuerAndAudience()
    {
        var token = CreateGenerator().GenerateToken("u1", "a@b.com", "Rider", null);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Issuer.Should().Be(Issuer);
        jwt.Audiences.Should().Contain(Audience);
    }

    [Fact]
    public void GenerateToken_ExpiresIn24Hours()
    {
        var before = DateTime.UtcNow;
        var token = CreateGenerator().GenerateToken("u1", "a@b.com", "Rider", null);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.Should().BeAfter(before.AddHours(23));
        jwt.ValidTo.Should().BeBefore(before.AddHours(25));
    }

    [Fact]
    public void GenerateToken_ProducesDifferentTokens_EachCall()
    {
        var gen = CreateGenerator();
        var t1 = gen.GenerateToken("u1", "a@b.com", "Rider", null);
        var t2 = gen.GenerateToken("u1", "a@b.com", "Rider", null);

        t1.Should().NotBe(t2);
    }
}
