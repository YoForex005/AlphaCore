# W500_SLICE_78

- **slot:** 78
- **file:** `D:/Prop/src/Infrastructure/DependencyInjection.cs`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full assigned file (58/58 lines) via `read_file` on 2026-08-18; grep `DealRequestByGroup|FromDays\(90\)|AddDays\(-90\)|Windows\(|chunk|timeout|IMt5BulkDealReader` under `D:/Prop/src`; followed composition to `LiveMt5Registration.CreateConnectors`, `LiveIngestHostedService`, `DealIngestionService`, `NativeMt5BrokerConnector.GetGroupDealsCore` / `Windows`, SDK `MT5APIManager.h`. No secrets printed.
- **verdict:** PASS

## File (assigned)

`D:/Prop/src/Infrastructure/DependencyInjection.cs` is the composition root (`AddTraderIntelligence`). It contains **zero** `DealRequestByGroup`, `AddDays(-90)`, `Windows`, or timeout registrations. Grep of this file for those tokens is empty. That is **not** an empty-PASS skip: this type is the only production binder of the 90-day ingest host to the sole `IMt5BulkDealReader` / `DealRequestByGroup` implementation.

```19:57:D:/Prop/src/Infrastructure/DependencyInjection.cs
public static class DependencyInjection
{
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];
        // ... DbContext (in-memory if connection missing / placeholder; else Npgsql) ...

        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);

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

`LiveMt5Registration.CreateConnectors` (DI lines 44–45) constructs two `NativeMt5BrokerConnector` instances (`IMt5BulkDealReader`). Connectors are `AddSingleton`. There is no chunking decorator, no Polly/`HttpClient` timeout, and no `IOptions` window splitter in this file. Chunking lives **inside** the registered connector (see §2).

Older slot 28 scored this same file FAIL against a one-shot `DealRequestByGroup(group, from, to, arr)` body. That body is **gone**. Current `GetGroupDealsCore` walks `Windows(from, to)` at 14 days.

## Evidence quotes

### 1. Host still asks for 90 days; ingest forwards the whole `[from, to]`

DI `AddHostedService<LiveIngestHostedService>()` (line 54) + `AddScoped<DealIngestionService>()` (line 52) are the only ingest wiring. The host hard-codes a 90-day UTC window and passes it whole to `SyncBrokerAsync`:

```37:64:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);

            foreach (var connector in registry.All())
            {
                // ...
                    await connector.ConnectAsync(stoppingToken);
                    // ...
                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

```64:70:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
```

`IMt5BulkDealReader` is a single `(group, from, to)` contract (`Mt5Contracts.cs` L71–74). DI does not wrap it. The question for this angle is whether that 90-day span is sent as **one** `DealRequestByGroup`.

### 2. Registered connector chunks every group request into 14-day `Windows`

```296:366:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequestByGroup {group}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }

            return all;
        }
    }

    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Windows(DateTimeOffset from, DateTimeOffset to)
    {
        var cursor = from;
        while (cursor < to)
        {
            var end = cursor.AddDays(14);
            if (end > to)
                end = to;
            yield return (cursor, end);
            cursor = end;
        }
    }
```

A host 90-day `GetGroupDealsAsync` is therefore **~7** native `DealRequestByGroup` RPCs per group, not one 90-day dump. The same splitter is used by per-login `DealRequest` (`GetDealsCore` L279). Adjacent windows share the `end` instant (`cursor = end`); that is a closed/open-boundary overlap of a single unix second, not an unchunked 90-day call.

### 3. Fail-closed on Manager timeout / network; Connect timeout is separate

```443:455:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            5 => "disk/no-connect in some builds — server unreachable",
            10 => "no connection",
            9 => "timeout",
            _ => code.ToString()
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }
```

Codes 7 and 9 on a chunk throw (`GetGroupDealsCore` L308–309). Host catch is fail-closed (no dummy book):

```93:99:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                catch (Exception ex)
                {
                    st.Connected = false;
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} live ingest failed. No dummy data will be substituted.", connector.BrokerCode);
                }
```

`Connect` uses `30000` ms (`ConnectCore` L92 / L101). There is still **no** per-`DealRequestByGroup` timeout argument (SDK signature is `group, from, to, array` only). Public surface is `Task.Run(() => GetGroupDealsCore(...), ct)` (connector L51–52); `ct` is not observed inside the lock. DI registers no request-timeout decorator.

### 4. Residuals that do **not** restore “90-day without chunking”

- `DealRequestPage` is unused (`grep` of `D:/Prop/src` is zero). SDK pages **per login**, not per group.
- A59 still prefers 1–7 day chunks until paging exists. Current slice is **14 days**, wider than that guidance.
- `lock (_gate)` is held across **all** 14-day windows of the 90-day span on a singleton connector, so one hung 14-day RPC still starves other Manager calls on that broker.
- These residuals are **not** the assigned defect. The assigned angle is a **single unchunked 90-day `DealRequestByGroup`**. That call site no longer exists.

### 5. Empty-PASS is not applicable

Assigned file was fully read (58/58 lines). The angle is **present via wiring** and was scored on the live `GetGroupDealsCore` / `Windows` path this composition root enables, not skipped because DI has no deal-request tokens.

## No-loss implication

`DependencyInjection.cs` composes ingest, scoring, catalog persistence, and a FIX **logon** host. `REAL_COPY_EXECUTION_ENABLED` is stored on `LiveRuntimeStatus` only (DI L38–42); this file does not send FIX `NewOrderSingle`, Manager `DealerSend`, or any close/modify. Direct equity reduction from this file is **none**.

History-completeness risk that would have come from **one 90-day `DealRequestByGroup`** (timeout hang under `lock (_gate)`, silent cap of a huge group dump, reconstruction missing losers, copy/risk promoting a bad leader) is **mitigated** by 14-day windowing plus throw-on-timeout. On throw, ingest logs and does **not** substitute dummy deals, so score/copy cannot treat a timed-out window as a clean book.

Residual: a busy group’s 14-day dump can still stall the singleton mutex; `stoppingToken` cannot abort the native call; there is no `DealRequestPage` completeness proof. Those residuals do not restore the assigned “90-day unchunked” defect.

**Risk to capital:** none from order send. Operational completeness only; 90-day unchunked `DealRequestByGroup` timeout path is closed on the types this composition root registers.

## Verdict rationale

PASS: the assigned composition root still chooses a 90-day `[UtcNow-90d, UtcNow+1m]` ingest window, but the `NativeMt5BrokerConnector` it registers splits that window in `GetGroupDealsCore` via `Windows(..., 14d)`, so each `DealRequestByGroup` is a 14-day RPC (~7 per group), fail-closed on Manager codes 7/9, and is not a single unchunked 90-day Manager call.
