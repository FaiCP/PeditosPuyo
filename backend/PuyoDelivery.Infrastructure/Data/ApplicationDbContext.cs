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
    public DbSet<RestaurantClaim> RestaurantClaims => Set<RestaurantClaim>();

    // ---- Fase 2: plataforma de pedidos B2C ----
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<CustomerToken> CustomerTokens => Set<CustomerToken>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>();
    public DbSet<RiderOffer> RiderOffers => Set<RiderOffer>();

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
            e.Property(x => x.CurrentLocation).HasColumnType("geometry(point, 4326)");
            e.HasIndex(x => x.CurrentLocation).HasMethod("GIST");
            e.HasIndex(x => new { x.TenantId, x.IsOnline, x.IsBusy });
        });

        builder.Entity<RestaurantAdmin>(e =>
        {
            e.HasOne(x => x.Restaurant).WithMany(x => x.Admins).HasForeignKey(x => x.RestaurantId);
        });

        builder.Entity<RestaurantClaim>(e =>
        {
            e.HasIndex(x => x.RestaurantId);
            e.HasIndex(x => x.Status);
            e.HasOne(x => x.Restaurant).WithMany(x => x.Claims).HasForeignKey(x => x.RestaurantId);
            e.Property(x => x.Email).HasMaxLength(256);
            e.Property(x => x.Phone).HasMaxLength(30);
        });

        builder.Entity<Restaurant>(e =>
        {
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(100);
            e.Property(x => x.Location).HasColumnType("geometry(point, 4326)");
            e.HasIndex(x => x.Location).HasMethod("GIST");
        });

        builder.Entity<MenuItem>(e =>
        {
            e.HasOne(x => x.Restaurant).WithMany(x => x.MenuItems).HasForeignKey(x => x.RestaurantId);
        });

        // ---- Fase 2 ----
        builder.Entity<CustomerToken>(e =>
        {
            e.HasIndex(x => x.Token).IsUnique();
            e.Property(x => x.Token).HasMaxLength(64);
            e.Property(x => x.Phone).HasMaxLength(30);
        });

        builder.Entity<Order>(e =>
        {
            e.Property(x => x.OriginLocation).HasColumnType("geometry(point, 4326)");
            e.Property(x => x.DestinationLocation).HasColumnType("geometry(point, 4326)");
            e.HasIndex(x => x.OriginLocation).HasMethod("GIST");
            e.HasIndex(x => x.DestinationLocation).HasMethod("GIST");
            e.HasIndex(x => new { x.TenantId, x.Status });
            e.HasIndex(x => x.CustomerTokenId);

            e.Property(x => x.ProductsAmount).HasPrecision(10, 2);
            e.Property(x => x.DeliveryFeeAmount).HasPrecision(10, 2);
            e.Property(x => x.FeeCollectedAmount).HasPrecision(10, 2);

            e.HasOne(x => x.CustomerToken).WithMany(t => t.Orders).HasForeignKey(x => x.CustomerTokenId);
            e.HasOne(x => x.Restaurant).WithMany().HasForeignKey(x => x.RestaurantId);
            e.HasOne(x => x.AssignedRider).WithMany().HasForeignKey(x => x.AssignedRiderId);
            e.Property(x => x.PickupCode).HasMaxLength(10);
            e.Property(x => x.DeliveryCode).HasMaxLength(10);
        });

        builder.Entity<OrderItem>(e =>
        {
            e.Property(x => x.UnitPrice).HasPrecision(10, 2);
            e.HasOne(x => x.Order).WithMany(o => o.Items).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.MenuItem).WithMany().HasForeignKey(x => x.MenuItemId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<OrderEvent>(e =>
        {
            e.HasIndex(x => x.OrderId);
            e.HasOne(x => x.Order).WithMany(o => o.Events).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RiderOffer>(e =>
        {
            e.HasIndex(x => x.OrderId);
            e.HasIndex(x => x.RiderId);
            e.HasOne(x => x.Order).WithMany(o => o.Offers).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Rider).WithMany().HasForeignKey(x => x.RiderId);
        });

        builder.Entity<Rider>().Property(x => x.DeliveryFee).HasPrecision(10, 2);
        builder.Entity<Restaurant>().Property(x => x.PaymentQrUrl).HasMaxLength(2000);
        builder.Entity<Restaurant>().Property(x => x.LogoUrl).HasMaxLength(2000);
        builder.Entity<MenuItem>().Property(x => x.ImageUrl).HasMaxLength(2000);

        // Global Query Filter para multi-tenancy (null = mostrar todos, para endpoints públicos)
        builder.Entity<DeliveryCompany>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<Subscription>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<CompanyAdmin>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<Rider>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<Restaurant>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<MenuItem>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<Order>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<OrderItem>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<OrderEvent>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<RiderOffer>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
        builder.Entity<RestaurantClaim>().HasQueryFilter(x => _tenantAccessor.TenantId == null || x.TenantId == _tenantAccessor.TenantId);
    }
}

public interface ICurrentTenantAccessor
{
    Guid? TenantId { get; }
}
