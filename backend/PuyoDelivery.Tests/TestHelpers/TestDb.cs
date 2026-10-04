using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.Tests.TestHelpers;

public class NullTenantAccessor : ICurrentTenantAccessor
{
    public Guid? TenantId { get; set; }
}

public static class TestDb
{
    public static ApplicationDbContext CreateContext(string? dbName = null, Guid? tenantId = null, NullTenantAccessor? accessor = null)
    {
        accessor ??= new NullTenantAccessor { TenantId = tenantId };
        if (tenantId.HasValue)
            accessor.TenantId = tenantId;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, accessor);
    }

    public static UserManager<ApplicationUser> CreateUserManager(ApplicationDbContext context)
    {
        var store = new UserStore<ApplicationUser>(context);
        var identityOptions = Options.Create(new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequiredLength = 6,
                RequireNonAlphanumeric = false,
                RequireDigit = false,
                RequireLowercase = false,
                RequireUppercase = false,
                RequiredUniqueChars = 0
            }
        });

        return new UserManager<ApplicationUser>(
            store,
            identityOptions,
            new PasswordHasher<ApplicationUser>(),
            new[] { new UserValidator<ApplicationUser>() },
            new[] { new PasswordValidator<ApplicationUser>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
