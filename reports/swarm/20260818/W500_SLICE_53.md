# W500_SLICE_53

- **slot:** 53
- **file:** `D:/Prop/src/Infrastructure/DependencyInjection.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full assigned file (58 lines) via `read_file`; grep on this file for `AddHostedService|CTraderFix|NewOrderSingle|RealCopy|AddSingleton|AddScoped|IOptions`; followed composition to `CTraderFixLogonHostedService`, `CTraderFixSession`, `CTraderFixOptions`, `LiveRuntimeStatus`, `LiveIngestHostedService`, `LiveMt5Registration`, `NativeMt5BrokerConnector`; repo grep for `35=D`, `OrderSend`/`DealerSend`, `RealCopyEnabled` consumers
- **verdict:** PASS

## File (assigned)

`D:/Prop/src/Infrastructure/DependencyInjection.cs` is the shared composition root (`AddTraderIntelligence`). It registers EF, live MT5 **read** connectors, ingest/scoring, a runtime status singleton, `LiveIngestHostedService`, and `CTraderFixLogonHostedService`. It does **not** register a cTrader TRADE sender, `CTraderFixOptions`, `CTraderQuoteService`, `GuardedNewOrderSingle`, QuickFIX initiator, or any `35=D` builder.

```19:56:D:/Prop/src/Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];
        // ... InMemory vs Npgsql; HasRealPasswords fail-closed ...
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        // store, dashboard, reconstructor, scorer, ingest, scoring ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        return services;
    }
}
```

Grep of this file for `NewOrderSingle`, `35=D`, `OrderSend`, `DealerSend`, `CTraderFixOptions`, `Configure<`, `IOptions` returned **no** matches except the hosted-service type name `CTraderFixLogonHostedService` and the `RealCopyEnabled` assignment above.

## Evidence quotes

### 1. Composition root does not register a send path

Registrations actually present:

| # | Registration | Role vs angle |
|---|---|---|
| 1 | `TraderDbContext` | persistence (InMemory if empty/`<SECRET>`, else Npgsql) |
| 2 | `LiveRuntimeStatus` singleton | **status bit only** (`RealCopyEnabled`) |
| 3–4 | `IMt5BrokerConnector` ×2 | `NativeMt5BrokerConnector` from `LiveMt5Registration.CreateConnectors` — Manager **read** |
| 5 | `IBrokerRegistry` | lookup of those connectors |
| 6–7 | `ITradingStore` / `IDashboardQueries` | EF read/write of ingested rows |
| 8–9 | `TradeReconstructor` / `BaselineScorer` | offline math |
| 10–11 | `DealIngestionService` / `ReconstructionScoringService` | catalog/deals/scores |
| 12 | `LiveIngestHostedService` | Connect + SyncCatalog + SyncBroker + RebuildTrader |
| 13 | `CTraderFixLogonHostedService` | one-shot FIX **Logon** (`35=A`) on QUOTE+TRADE, then persist session rows |

**Not registered** (so this graph cannot send):

- `CTraderFixOptions` / `IOptions<CTraderFixOptions>` / `Configure<CTraderFixOptions>`
- `CTraderQuoteService` (exists on disk; ctor takes options; **never** added here)
- any `IHostedService` that builds `35=D`
- `RiskEngine`, `ShadowCopyEngine`, `ClOrdIdFactory`, outbox dispatcher
- QuickFIX `SocketInitiator` / `IInitiator` (project has no QuickFIX package)

Infrastructure **does** reference `Fix.CTrader` (`TraderIntelligence.Infrastructure.csproj` project ref). That only makes `CTraderFixLogonHostedService` compilable. A project reference is not a NewOrderSingle.

### 2. `REAL_COPY_EXECUTION_ENABLED` is copied onto a snapshot, not a sender

```38:42:D:/Prop/src/Infrastructure/DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`LiveRuntimeStatus.Snapshot` treats the bit as display copy. Default path (flag not the exact string `"true"`) is the no-send note:

```39:44:D:/Prop/src/Application/Runtime/LiveRuntimeStatus.cs
    public object Snapshot() => new
    {
        startedAt = StartedAt,
        realCopyEnabled = RealCopyEnabled,
        copyNote = RealCopyEnabled
            ? "LIVE SEND ARMED — unexpected"
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

Repo consumers of `RealCopyEnabled` (product C#): this assignment, `LiveRuntimeStatus` itself, and `apps/api/Program.cs` health/settings **display** (`realCopyEnabled = runtime.RealCopyEnabled`, featureFlags dictionary). **Zero** callers use the bit to emit FIX or Manager trade. Flipping the env name to `true` changes a JSON field and a `copyNote` string. It does not register a sender that this file omitted.

Owning POCO default remains off and is **not bound** by this method:

```32:35:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

### 3. The only cTrader host is logon-only; it still says NewOrderSingle is disabled

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

After logon it reflects session rows (`PersistAsync` updates `FixSessionState` host/port/status). It does not keep the socket, does not heartbeat, and does not call any order API. Missing / placeholder FIX password → **return** (logon skipped), not a send.

### 4. The FIX session type can only build Logon (`35=A`) and then disposes the TCP/TLS client

```35:50:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            // ...
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
```

`BuildLogon` field 35 is **`"A"`** only:

```94:108:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

`using var tcp` means the TRADE probe **closes** when `TryLogonAsync` returns. There is no long-lived TRADE session for a later `35=D` even if one existed. Grep of `D:/Prop/src/Fix.CTrader` for `35=D` / `MsgType.?D`: **0 hits**.

### 5. Live MT5 wiring in this file is ingest, not dealer send

`HasRealPasswords` **throws** if MT5 secrets are dummy (`<SECRET>` / empty / `(a/c`). That is a **demo-block**, not an order arm.

`CreateConnectors` builds two `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `D:/Prop/src/Mt5` for `OrderSend` / `DealerSend` / `TradeTrans` / `35=D`: **0 hits**. Connector surface is Connect / groups / accounts / deals / positions (read/pump).

`LiveIngestHostedService` (DI line 54) uses those connectors for `ConnectAsync` → `SyncCatalogAsync` → `SyncBrokerAsync` → `RebuildTraderAsync`. No FIX, no NewOrderSingle, no qty send.

### 6. Empty-PASS is not this slice

Assigned file was fully read (58/58 lines). The angle is **adjacent** (FIX TRADE logon host + `REAL_COPY_EXECUTION_ENABLED` bit) and was followed into the hosted service and session builder. Absence of NewOrderSingle is **measured**, not skipped.

## No-loss implication

This composition root **cannot** place a cTrader `NewOrderSingle` and **cannot** reduce destination equity.

- No `35=D` builder is registered or even present under `Fix.CTrader`.
- TRADE logon is a 20s one-shot `35=A` that disposes the socket; the host then writes DB status and logs “NewOrderSingle still disabled.”
- `RealCopyEnabled=true` is a dashboard/health string only. `CTraderFixOptions.RealCopyExecutionEnabled` is never bound here.
- Native MT5 objects registered here have no `OrderSend` / `DealerSend`.
- Ingest/scoring can be wrong about a book (out of this angle; see slot 28 on 90-day `DealRequestByGroup`) but they do not submit orders.

**Risk to capital:** none from a live send path in this file. Classification: **`SAFE_BY_ABSENCE`** of a NewOrderSingle sender at the composition root. Do **not** treat this PASS as “flag-gated execution” or as a go-live tick: there is still no coded refuse-path when TRADE is LoggedOn, because nothing can send.

## Verdict rationale

PASS: `AddTraderIntelligence` wires live MT5 **read** + a FIX **logon probe** + an observational copy flag. It does not wire, bind, or construct a cTrader NewOrderSingle / capital-mutation path. Slot 53 angle is therefore **not present as a send path**.
