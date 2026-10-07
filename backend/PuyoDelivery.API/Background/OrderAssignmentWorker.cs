namespace PuyoDelivery.API.Background;

/// <summary>Hilo que dispara el motor de asignación periódicamente (cada 10s).</summary>
public class OrderAssignmentWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(10);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderAssignmentWorker> _logger;

    public OrderAssignmentWorker(IServiceScopeFactory scopeFactory, ILogger<OrderAssignmentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var engine = scope.ServiceProvider.GetRequiredService<AssignmentEngine>();
                var changed = await engine.RunTickAsync();
                if (changed > 0)
                    _logger.LogInformation("OrderAssignmentWorker: {Changed} cambios en el tick", changed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en OrderAssignmentWorker");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
