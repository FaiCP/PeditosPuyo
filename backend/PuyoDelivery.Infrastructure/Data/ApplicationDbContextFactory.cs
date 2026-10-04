using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PuyoDelivery.Infrastructure.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=puyodelivery;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.UseNetTopologySuite();
        });

        var nullTenantAccessor = new NullCurrentTenantAccessor();
        return new ApplicationDbContext(optionsBuilder.Options, nullTenantAccessor);
    }
}

public class NullCurrentTenantAccessor : ICurrentTenantAccessor
{
    public Guid? TenantId => null;
}
