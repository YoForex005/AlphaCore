using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TraderIntelligence.Application.Runtime;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Fix.CTrader.Sessions;

namespace TraderIntelligence.Fix.CTrader.Hosting;

public sealed class CTraderFixLogonHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly LiveRuntimeStatus _runtime;
    private readonly ILogger<CTraderFixLogonHostedService> _log;

    public CTraderFixLogonHostedService(
        IServiceScopeFactory scopes,
        IConfiguration config,
        LiveRuntimeStatus runtime,
        ILogger<CTraderFixLogonHostedService> log)
    {
        _scopes = scopes;
        _config = config;
        _runtime = runtime;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }

        var host = _config["CTRADER_FIX_HOST"] ?? "demo-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "5328266";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "demo.pepperstone.5328266";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";

        // cTrader FIX tag 553 must be the integer account id, not SenderCompID.
        var username = account;

        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);

        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);

        try
        {
            using var scope = _scopes.CreateScope();
            var dbType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name == "TraderDbContext");
            if (dbType is null)
                return;
            var db = scope.ServiceProvider.GetService(dbType);
            if (db is null)
                return;
            await PersistAsync(db, quote, trade, host, stoppingToken);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Could not persist FIX session rows");
        }
    }

    private static async Task PersistAsync(object db, CTraderFixSessionResult quote, CTraderFixSessionResult trade, string host, CancellationToken ct)
    {
        if (db is not DbContext ctx)
            return;
        var set = ctx.Set<TraderIntelligence.Domain.Entities.FixSessionState>();
        foreach (var result in new[] { quote, trade })
        {
            var row = await set.FirstOrDefaultAsync(s => s.Qualifier == result.Qualifier, ct);
            if (row is null)
                continue;
            row.Host = host;
            row.Port = result.Qualifier == FixSessionQualifier.Quote ? 5211 : 5212;
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
            row.LastError = result.LastError;
            row.LastInboundAt = DateTimeOffset.UtcNow;
            row.LastOutboundAt = DateTimeOffset.UtcNow;
            row.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await ctx.SaveChangesAsync(ct);
    }
}
