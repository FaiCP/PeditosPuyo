using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PuyoDelivery.Infrastructure;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.API.Hubs;
using PuyoDelivery.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("VercelPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition");
    });
});

// JWT config values
var jwtKey = builder.Configuration["Jwt:SecretKey"] ?? "PuyoDeliverySecretKeyForDev2026!PuyoDeliverySecretKeyForDev2026!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PuyoDelivery";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PuyoDeliveryApp";

builder.Services.AddAuthorization();

// EF Core + PostgreSQL
var rawConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? "Host=localhost;Port=5433;Database=puyodelivery;Username=postgres;Password=postgres";
var connectionString = NormalizeConnectionString(rawConnectionString);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql =>
    {
        npgsql.UseNetTopologySuite();
    }));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT (DESPUÉS de Identity para sobreescribir el scheme por defecto)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        NameClaimType = "name",
        RoleClaimType = "role"
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// SignalR
builder.Services.AddSignalR();

// Background Services
builder.Services.AddHostedService<PuyoDelivery.API.Background.RiderCleanupService>();

// Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("VercelPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CompanyHub>("/hubs/company");
app.MapHub<RiderHub>("/hubs/rider");
app.MapHub<RestaurantHub>("/hubs/restaurant");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

// Auto-migrate en desarrollo
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.Run();

// Convierte URLs postgres:// / postgresql:// (formato Render/Heroku) a connection string Npgsql
static string NormalizeConnectionString(string cs)
{
    if (!cs.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
        !cs.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return cs;

    var uri = new Uri(cs);
    var database = uri.AbsolutePath.Trim('/');
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;

    var result = $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password}";

    // SSL obligatorio en remoto (Render); local no lo usa
    if (!uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
        !uri.Host.Equals("127.0.0.1") &&
        !uri.Host.Equals("host.docker.internal", StringComparison.OrdinalIgnoreCase))
    {
        result += ";SSL Mode=Require;Trust Server Certificate=true";
    }

    return result;
}
