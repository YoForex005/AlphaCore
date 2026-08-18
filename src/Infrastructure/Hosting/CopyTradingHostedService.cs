using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraderIntelligence.Infrastructure.Copy;

namespace TraderIntelligence.Infrastructure.Hosting;

public sealed class CopyTradingHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CopyTradingHostedService> _log;

    public CopyTradingHostedService(IServiceScopeFactory scopes, ILogger<CopyTradingHostedService> log)
    {
        _scopes = scopes;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                if (n > 0)
                    _log.LogInformation("Copy pipeline created {Count} SHADOW intents. Live NewOrderSingle still blocked.", n);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Copy pipeline tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
}
