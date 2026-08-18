# W500_SLICE_3

- **slot:** 3
- **file:** `D:/Prop/src/Infrastructure/DependencyInjection.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (51 lines) via `read_file`; grepped `Infrastructure` and `src` for `NewOrderSingle|cTrader|35=D|RealCopy|PlaceOrder`; followed the only FIX hosted service named in this file (`CTraderFixLogonHostedService`) plus `CTraderFixSession.TryLogonAsync` / `BuildLogon`
- **verdict:** PASS

## Evidence quotes

Assigned file is the composition root `AddTraderIntelligence`. It binds persistence, live MT5 *ingest* connectors, dashboard/scoring, and two hosted services. It does **not** bind `CTraderFixOptions`, does **not** register `CTraderQuoteService`, and does **not** register any order-send / execution type.

```17:48:D:/Prop/src/Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];
        // ... InMemory vs Npgsql ...
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        services.AddScoped<ITradingStore, EfTradingStore>();
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        return services;
    }
}
```

This file does not contain `NewOrderSingle`, `35=D`, `RealCopyExecutionEnabled`, `CTraderFixOptions`, or any FIX body builder.

The only cTrader-adjacent registration is `AddHostedService<CTraderFixLogonHostedService>()`. That type lives in `Fix.CTrader` (`TraderIntelligence.Fix.CTrader.Hosting`). `TraderIntelligence.Infrastructure.csproj` has **no** `ProjectReference` to `Fix.CTrader` (only Domain, Application, Mt5). There is also **no** `using TraderIntelligence.Fix.CTrader.Hosting`. The named hosted service is therefore not a compiled, injectable send path from this assembly as it sits today.

Even if that type were later referenced, its live work is Logon-only (`35=A`) on QUOTE 5211 and TRADE 5212, then persist session rows. The log line itself asserts NOS remains off:

```41:54:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            sender, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            sender, password, stoppingToken);

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

`CTraderFixSession.BuildLogon` emits only msg type `A`. Socket is `using`/`await using` and disposed after one read — no keep-alive TRADE initiator, no `35=D`:

```89:108:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
    }
```

Grep of `D:\Prop\src\Fix.CTrader` for `NewOrderSingle` hits only the log string above and the options comment (`RealCopyExecutionEnabled` default OFF). No `35=D` builder exists in that project.

Sibling hosted service registered on the same method, `LiveIngestHostedService`, only `ConnectAsync` + `SyncBrokerAsync` + `RebuildTraderAsync` on `IMt5BrokerConnector` — deal ingest/scoring, not venue order send.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults false and is **not** `Configure`’d here. Runtime status copy-note (not registered by this file) is `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

## No-loss implication

`AddTraderIntelligence` cannot emit cTrader `NewOrderSingle` (FIX `35=D`) and cannot reduce Pepperstone/cTrader equity from this graph. The MT5 branch is password-gated ingest (`IMt5BrokerConnector` + `LiveIngestHostedService`), not a FIX order sender. The named `CTraderFixLogonHostedService` is (a) not project-referenced from Infrastructure, and (b) even in `Fix.CTrader` is a one-shot `35=A` Logon that disposes the socket and never builds an order.

Residual (not a capital-loss path): if a later change adds the missing `Fix.CTrader` reference and a real password is present, a diagnostic TRADE logon to the live host/port could occur — still Logon-only, still no NOS.

Slot 3 therefore has **no live cTrader NewOrderSingle / capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (51 lines). The angle is absent as a send path (no `35=D`, no order service registration), not because review was skipped.
