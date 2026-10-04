using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using PuyoDelivery.Core.Entities;

namespace PuyoDelivery.Infrastructure.Data;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentTenantAccessor _tenantAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentTenantAccessor tenantAccessor)
        : base(options)
    {
        _tenantAccessor = tenantAccessor;
    }

    public DbSet<DeliveryCompany> DeliveryCompanies => Set<DeliveryCompany>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<CompanyPayment> CompanyPayments => Set<CompanyPayment>();
    public DbSet<CompanyAdmin> CompanyAdmins => Set<CompanyAdmin>();
    public DbSet<Rider> Riders => Set<Rider>();
    public DbSet<RestaurantAdmin> RestaurantAdmins => Set<RestaurantAdmin>();
    public DbSet<Restaurant> Restaurants => Set<Restaurant>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<DeliveryRequest> DeliveryRequests => Set<DeliveryRequest>();
    public DbSet<OrderAssignment> OrderAssignments => Set<OrderAssignment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var nt = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        builder.Entity<DeliveryCompany>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(50);
        });

        builder.Entity<Subscription>(e =>
        {
            e.HasOne(x => x.Company).WithMany(x => x.Subscriptions).HasForeignKey(x => x.CompanyId);
        });

        builder.Entity<CompanyPayment>(e =>
        {
            e.HasOne(x => x.Subscription).WithMany(x => x.Payments).HasForeignKey(x => x.SubscriptionId);
            e.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId);
        });

        builder.Entity<CompanyAdmin>(e =>
        {
            e.HasOne(x => x.Company).WithMany(x => x.Admins).HasForeignKey(x => x.CompanyId);
        });

        builder.Entity<Rider>(e =>
        {
            e.HasOne(x => x.Company).WithMany(x => x.Riders).HasForeignKey(x => x.CompanyId);
            e.Property(x => x.CurrentLocation).HasColumnType("geography(point, 4326)");
            e.HasIndex(x => x.CurrentLocation).HasMethod("GIST");
            e.HasIndex(x => new { x.TenantId, x.IsOnline, x.IsBusy });
        });

        builder.Entity<RestaurantAdmin>(e =>
        {
            e.HasOne(x => x.Restaurant).WithMany(x => x.Admins).HasForeignKey(x => x.RestaurantId);
        });

        builder.Entity<Restaurant>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(100);
            e.Property(x => x.Location).HasColumnType("geography(point, 4326)");
            e.HasIndex(x => x.Location).HasMethod("GIST");
        });

        builder.Entity<MenuItem>(e =>
        {
            e.HasOne(x => x.Restaurant).WithMany(x => x.MenuItems).HasForeignKey(x => x.RestaurantId);
        });

        builder.Entity<DeliveryRequest>(e =>
        {
            e.HasOne(x => x.Restaurant).WithMany(x => x.DeliveryRequests).HasForeignKey(x => x.RestaurantId);
            e.Property(x => x.DeliveryLocation).HasColumnType("geography(point, 4326)");
            e.HasIndex(x => x.DeliveryLocation).HasMethod("GIST");
        });

        builder.Entity<OrderAssignment>(e =>
        {
            e.HasOne(x => x.Request).WithMany(x => x.Assignments).HasForeignKey(x => x.RequestId);
            e.HasOne(x => x.Rider).WithMany(x => x.Assignments).HasForeignKey(x => x.RiderId);
        });

        // Global Query Filter para multi-tenancy
        builder.Entity<DeliveryCompany>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<Subscription>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<CompanyAdmin>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<Rider>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<Restaurant>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<MenuItem>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<DeliveryRequest>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
        builder.Entity<OrderAssignment>().HasQueryFilter(x => x.TenantId == _tenantAccessor.TenantId!.Value);
    }
}

public interface ICurrentTenantAccessor
{
    Guid? TenantId { get; }
}
