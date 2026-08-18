using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Application.Runtime;

namespace TraderIntelligence.Infrastructure.Hosting;

public sealed class LiveIngestHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly LiveRuntimeStatus _runtime;
    private readonly ILogger<LiveIngestHostedService> _log;

    public LiveIngestHostedService(
        IServiceScopeFactory scopes,
        LiveRuntimeStatus runtime,
        ILogger<LiveIngestHostedService> log)
    {
        _scopes = scopes;
        _runtime = runtime;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        try
        {
            using var scope = _scopes.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IBrokerRegistry>();
            var ingest = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
            var scoring = scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>();
            var store = scope.ServiceProvider.GetRequiredService<ITradingStore>();

            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
            var connectors = registry.All().ToList();

            foreach (var connector in connectors)
            {
                var st = _runtime.Broker(connector.BrokerCode);
                st.Phase = "connecting";
                st.UpdatedAt = DateTimeOffset.UtcNow;
                _log.LogInformation("Live catalog starting for {Broker}", connector.BrokerCode);
                try
                {
                    await connector.ConnectAsync(stoppingToken);
                    st.Connected = await connector.IsConnectedAsync(stoppingToken);
                    st.LastError = connector is TraderIntelligence.Mt5.Connectors.NativeMt5BrokerConnector native
                        ? native.LastError
                        : null;
                    st.Phase = "catalog";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var catalog = await ingest.SyncCatalogAsync(connector.BrokerCode, stoppingToken);
                    st.Groups = catalog.Groups;
                    st.Accounts = catalog.Accounts;
                    st.Phase = "catalog-done";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogInformation("{Broker} catalog groups={Groups} accounts={Accounts}",
                        connector.BrokerCode, catalog.Groups, catalog.Accounts);
                }
                catch (Exception ex)
                {
                    st.Connected = false;
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} catalog failed. No dummy data will be substituted.", connector.BrokerCode);
                }
            }

            foreach (var connector in connectors)
            {
                var st = _runtime.Broker(connector.BrokerCode);
                if (!st.Connected)
                    continue;
                try
                {
                    st.Phase = "deals";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
                    st.DealsInserted = deals;
                    st.Phase = "deals-done";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogInformation("{Broker} deals inserted={Deals}", connector.BrokerCode, deals);
                }
                catch (Exception ex)
                {
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} deal ingest failed. Catalog data is kept.", connector.BrokerCode);
                }
            }

            foreach (var connector in connectors)
            {
                var st = _runtime.Broker(connector.BrokerCode);
                if (!st.Connected)
                    continue;
                try
                {
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                        if (scored % 25 == 0)
                        {
                            st.Scored = scored;
                            st.UpdatedAt = DateTimeOffset.UtcNow;
                        }
                    }

                    st.Scored = scored;
                    st.Phase = "done";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogInformation("{Broker} scored {Scored} logins that have deals", connector.BrokerCode, scored);
                }
                catch (Exception ex)
                {
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} scoring failed. Catalog and deals are kept.", connector.BrokerCode);
                }
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Live ingest host failed");
        }
    }
}
