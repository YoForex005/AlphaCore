# W500_SLICE_28

- **slot:** 28
- **file:** `D:/Prop/src/Infrastructure/DependencyInjection.cs`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full assigned file (50 lines) via `read_file`; grep `DealRequestByGroup|90.?day|chunk|TimeSpan|Timeout` on `D:/Prop/src` and `DealRequestByGroup` repo-wide; followed composition to `LiveIngestHostedService`, `DealIngestionService`, `NativeMt5BrokerConnector`, `IMt5BulkDealReader`
- **verdict:** FAIL

## File (assigned)

`D:/Prop/src/Infrastructure/DependencyInjection.cs` is the composition root. It does not implement `DealRequestByGroup` itself. It **does** register the three types that together issue one unchunked 90-day group deal pull and never wrap it with a timeout or window splitter:

```36:47:D:/Prop/src/Infrastructure/DependencyInjection.cs
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
```

Grep of this file for `chunk`, `Timeout`, `CommandTimeout`, `HttpClient`, `DealRequest`, `FromDays` returned **no matches**. There is no decorator, options type, or Polly/timeout registration around `IMt5BulkDealReader`.

`LiveMt5Registration.CreateConnectors` (called at lines 36–37) constructs `NativeMt5BrokerConnector` instances. That type implements `IMt5BulkDealReader` and is the only production `DealRequestByGroup` caller.

## Evidence quotes

### 1. Composition root registers the 90-day ingest host with no chunk policy

`AddHostedService<LiveIngestHostedService>()` (DI line 46) and `AddScoped<DealIngestionService>()` (DI line 44) are the only ingest wiring. The host hard-codes a 90-day UTC window and passes it whole to `SyncBrokerAsync`:

```32:41:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);

            foreach (var connector in registry.All())
            {
                _log.LogInformation("Live ingest starting for {Broker}", connector.BrokerCode);
                try
                {
                    await connector.ConnectAsync(stoppingToken);
                    var n = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

No loop over 1–7 day slices. No `GetServerTime`. No checkpoint cursor.

### 2. Ingest forwards the full window to one `GetGroupDealsAsync` per group

```49:59:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                foreach (var deal in deals)
                {
                    if (await _store.UpsertDealAsync(brokerId, deal, now, ct))
                        insertedDeals++;
                }
            }
        }
```

`IMt5BulkDealReader` is a single-shot contract (`from`, `to` only). DI does not register a chunking adapter in front of it.

### 3. Native connector: one `DealRequestByGroup`, no page, no timeout

```240:254:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.DealCreateArray();
            try
            {
                var res = _manager.DealRequestByGroup(group, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    throw new InvalidOperationException($"{BrokerCode} DealRequestByGroup {group} failed: {res}");
                return ReadDeals(arr);
            }
            finally { arr.Release(); }
        }
    }
```

Public surface is `Task.Run(() => GetGroupDealsCore(...), ct)` (lines 49–50). `CancellationToken` is **not** observed inside `GetGroupDealsCore`. The only Manager timeout in this type is `Connect(..., 30000)` (line 99), which does not apply to deal history.

`lock (_gate)` holds the same mutex used by Connect / GetGroups / GetAccounts / GetDeals / GetPositions for the entire group-window RPC. A 90-day `DealRequestByGroup` that stalls blocks all other Manager calls on that singleton connector (DI registers connectors as `AddSingleton`).

### 4. Design law already forbids this window size

`D:/Prop/reports/swarm/20260818/A59_ingestion_checkpoints.md`:

> `to = min(from + chunk_sec, server_now)   -- chunk e.g. 7 days`

> Chunk | 1–7 days per commit. Smaller chunks → more frequent restart-safe advances.

> The C# collector must not treat a single `DealRequest` success as proof of completeness for wide windows. Until the native wrapper pages, keep chunks small enough that a silent cap is unlikely, and still run reconcile.

Live path violates all three: 90-day one-shot, no page (`DealRequestPage` unused), no checkpoint advance per chunk.

### 5. Empty-PASS is not applicable

Assigned file was fully read (50/50 lines). The angle is **present via wiring**, not absent. Empty PASS would be a skip.

## No-loss implication

This slice does **not** send FIX `NewOrderSingle`, Manager `DealerSend`, or any close/modify. `DependencyInjection.cs` only composes ingest, scoring, and a FIX **logon** host. Direct equity reduction from this file is **none**.

Indirect capital / book risk is real:

1. **Silent truncation.** A 90-day `DealRequestByGroup` with no paging can return a capped array. Reconstruction/scoring then see a partial book (missed losses, broken round-trips, wrong `SuggestedState`). Downstream copy/risk that trusts those scores can promote a bad leader.
2. **Hang / mutex starvation.** Unbounded `DealRequestByGroup` under `lock (_gate)` on a singleton connector can freeze live position/deal refresh for that broker until the Manager call returns. Kill-switch and open-risk views go stale while the 90-day pull runs.
3. **No fail-closed timeout.** DI registers no request timeout. A stuck history call is not cancelled by `stoppingToken`. Ingest error is logged and the host continues to the next broker; it does **not** freeze trading, but it also does **not** mark the book incomplete.

**Risk to capital:** operational / completeness, not an order-send path. Fail closed on history completeness is **not** implemented at the composition root.

## Verdict rationale

FAIL: the assigned composition root enables production `NativeMt5BrokerConnector.DealRequestByGroup` with a host-chosen 90-day `[UtcNow-90d, UtcNow+1m]` window, no 1–7 day chunking, no `DealRequestPage`, and no deal-request timeout. That is exactly the Slot 28 angle.
