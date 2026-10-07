using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PuyoDelivery.API.Controllers;
using PuyoDelivery.Core.Entities;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Development;

/// <summary>
/// Solo Development: garantiza un tenant demo con su CompanyAdmin y puebla
/// los restaurantes desde restaurantes_puyo.json si la tabla está vacía.
/// Idempotente: nunca duplica si ya existe.
/// </summary>
public static class DevelopmentDataSeeder
{
    private const string DemoCompanyName = "Puyo Demo Company";
    private const string DemoAdminEmail = "demo@puyodelivery.local";
    private const string DemoAdminPassword = "Puyo2026!";
    private const string SeedFileName = "restaurantes_puyo.json";

    public static async Task SeedAsync(IServiceProvider services, string contentRootPath, ILogger logger)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var tenantId = await EnsureDemoCompanyAsync(db, userManager, logger);

        // Idempotente: dedupe por slug; si ya están importados no agrega nada
        await ImportRestaurantsAsync(db, contentRootPath, tenantId, logger);
    }

    private static async Task<Guid> EnsureDemoCompanyAsync(
        ApplicationDbContext db, UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var company = await db.DeliveryCompanies.FirstOrDefaultAsync();
        if (company != null) return company.TenantId;

        company = new DeliveryCompany
        {
            TenantId = Guid.NewGuid(),
            Name = DemoCompanyName,
            Slug = "puyo-demo"
        };
        db.DeliveryCompanies.Add(company);
        await db.SaveChangesAsync();

        var admin = new ApplicationUser
        {
            UserName = DemoAdminEmail,
            Email = DemoAdminEmail,
            FullName = "Demo Admin",
            Phone = "0990000000",
            Role = "CompanyAdmin",
            TenantId = company.TenantId
        };
        var result = await userManager.CreateAsync(admin, DemoAdminPassword);
        if (!result.Succeeded)
            logger.LogWarning("Seeder: no se pudo crear el admin demo: {errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        else
            logger.LogInformation("Seeder: admin demo creado → {email} / {password}", DemoAdminEmail, DemoAdminPassword);

        if (result.Succeeded)
        {
            db.CompanyAdmins.Add(new CompanyAdmin
            {
                TenantId = company.TenantId,
                UserId = Guid.Parse(admin.Id),
                CompanyId = company.Id,
                FullName = admin.FullName,
                Email = admin.Email,
                Phone = admin.Phone
            });
            await db.SaveChangesAsync();
        }

        return company.TenantId;
    }

    private static async Task ImportRestaurantsAsync(
        ApplicationDbContext db, string contentRootPath, Guid tenantId, ILogger logger)
    {
        var filePath = FindSeedFile(contentRootPath);
        if (filePath == null)
        {
            logger.LogWarning("Seeder: no se encontró {file}; restaurantes no poblados.", SeedFileName);
            return;
        }

        var json = await File.ReadAllTextAsync(filePath);
        var restaurants = JsonSerializer.Deserialize<List<ImportRestaurantJson>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ImportRestaurantJson>();

        var seenSlugs = new HashSet<string>();
        var added = 0;

        foreach (var r in restaurants)
        {
            var name = RestaurantsController.CleanScrapedName(r.Name);
            if (string.IsNullOrWhiteSpace(name)) continue;

            var slug = RestaurantsController.Truncate(RestaurantsController.Slugify(name), 100);
            if (!seenSlugs.Add(slug)) continue;
            if (await db.Restaurants.IgnoreQueryFilters().AnyAsync(x => x.Slug == slug)) continue;

            db.Restaurants.Add(new Restaurant
            {
                TenantId = tenantId,
                Name = RestaurantsController.Truncate(name, 100),
                Slug = slug,
                Address = r.Address ?? "",
                Phone = r.Phone ?? "",
                Location = new NetTopologySuite.Geometries.Point(
                    r.Lng ?? RestaurantsController.DefaultLng,
                    r.Lat ?? RestaurantsController.DefaultLat) { SRID = 4326 },
                MenuSummary = r.MenuSummary,
                ExternalId = r.Id?.ToString(),
                Source = RestaurantSource.Scraper
            });
            added++;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Seeder: {count} restaurantes poblados desde {file}", added, Path.GetFileName(filePath));    }

    /// <summary>Busca el JSON desde la carpeta del proyecto hacia arriba (raíz del repo).</summary>
    private static string? FindSeedFile(string startDir)
    {
        var dir = new DirectoryInfo(startDir);
        for (var i = 0; i < 6 && dir != null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, SeedFileName);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
