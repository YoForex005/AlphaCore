using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.FixWorker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopes, IConfiguration config)
    {
        _logger = logger;
        _scopes = scopes;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            if (quote is not null)
            {
                quote.UpdatedAt = DateTimeOffset.UtcNow;
                quote.Status = FixSessionStatus.Disconnected;
                quote.LastError = "No live QUOTE socket. Simulator/demo only.";
            }

            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            if (trade is not null)
            {
                trade.UpdatedAt = DateTimeOffset.UtcNow;
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
            }

            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
}
