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
        var ticks = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var copy = scope.ServiceProvider.GetRequiredService<CopyTradingService>();
                var closed = await copy.ReconcileDestClosesAsync(stoppingToken);
                ticks++;
                var roster = 0;
                var n = 0;
                if (ticks % 40 == 0)
                {
                    roster = await copy.TickRosterAsync(stoppingToken);
                    n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                }

                if (roster > 0 || n > 0 || closed > 0)
                    _log.LogInformation(
                        "Copy roster={Roster} intents={Intents} destCloses={Closes}.",
                        roster, n, closed);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Copy pipeline tick failed");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
        }
    }
}
