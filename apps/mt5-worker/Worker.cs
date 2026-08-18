using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;

namespace TraderIntelligence.Mt5Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopes;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopes)
    {
        _logger = logger;
        _scopes = scopes;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
                var scoring = scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>();
                var from = DateTimeOffset.UtcNow.AddDays(-30);
                var to = DateTimeOffset.UtcNow.AddMinutes(1);
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "MT5 sync cycle failed; will retry. No source trades invented.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
