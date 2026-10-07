using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Repositories;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentTenantAccessor, CurrentTenantService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<JwtTokenGenerator>(sp =>
        {
            var secretKey = configuration["Jwt:SecretKey"] ?? "DefaultSecretKeyForDev2026!DefaultSecretKeyForDev2026!";
            var issuer = configuration["Jwt:Issuer"] ?? "PuyoDelivery";
            var audience = configuration["Jwt:Audience"] ?? "PuyoDeliveryApp";
            return new JwtTokenGenerator(secretKey, issuer, audience);
        });

        services.AddScoped<GeocodingService>();
        services.AddHttpClient<GeocodingService>();

        services.AddScoped<OrderService>();

        services.AddSingleton<FcmV1Service>(_ => new FcmV1Service(
            configuration["Fcm:ServiceAccountPath"],
            configuration["Fcm:ServiceAccountJson"]));

        return services;
    }
}
