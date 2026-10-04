using Microsoft.EntityFrameworkCore;
using PuyoDelivery.Infrastructure.Data;

namespace PuyoDelivery.API.Background;

public class RiderCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RiderCleanupService> _logger;

    public RiderCleanupService(IServiceProvider serviceProvider, ILogger<RiderCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var cutoffTime = DateTime.UtcNow.AddMinutes(-5);

                var offlineRiders = await context.Riders
                    .IgnoreQueryFilters()
                    .Where(r => r.IsOnline && r.LastLocationUpdate < cutoffTime)
                    .ToListAsync(stoppingToken);

                foreach (var rider in offlineRiders)
                {
                    rider.IsOnline = false;
                    rider.CurrentLocation = null;
                }

                if (offlineRiders.Count > 0)
                {
                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Marked {Count} riders as offline (no location update in 5 minutes)", offlineRiders.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RiderCleanupService");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
