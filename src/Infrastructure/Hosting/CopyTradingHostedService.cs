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
                var roster = await copy.TickRosterAsync(stoppingToken);
                var n = await copy.GenerateShadowIntentsAsync(stoppingToken);
                var sent = await copy.ExecuteDemoCopyAsync(stoppingToken);
                if (roster > 0 || n > 0 || sent > 0)
                    _log.LogInformation(
                        "Copy roster={Roster} intents={Intents} demoSends={Sends}.",
                        roster, n, sent);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Copy pipeline tick failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
}
