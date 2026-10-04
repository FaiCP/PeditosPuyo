using Microsoft.Extensions.DependencyInjection;
using PuyoDelivery.Core.Interfaces;
using PuyoDelivery.Infrastructure.Data;
using PuyoDelivery.Infrastructure.Repositories;
using PuyoDelivery.Infrastructure.Services;

namespace PuyoDelivery.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentTenantAccessor, CurrentTenantService>();
        services.AddScoped<ICurrentTenantService, CurrentTenantService>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<JwtTokenGenerator>();
        services.AddScoped<GeocodingService>();
        services.AddHttpClient<GeocodingService>();

        return services;
    }
}
