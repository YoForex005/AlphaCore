# W500_SLICE_1

- **slot:** 1
- **file:** `D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (65 lines) via `read_file`; grep for `GetGroupsAsync|manager group|MT5_GROUP_|while|PeriodicTimer` on this file and `src/Infrastructure/Hosting`; followed call into `DealIngestionService.SyncBrokerAsync` and `NativeMt5BrokerConnector.GetGroupsCore`
- **verdict:** FAIL

## Binding law (this angle)

Architecture v2 §7: `demo\Maxmaster` is not the only group; startup/resync must **dynamically enumerate all groups accessible to the Manager login** (Connect → enumerate groups → upsert → accounts → history).

Architecture v2 §9: `MT5_GROUP_*` plan mappings must **not** determine which groups are fetched. Discovery is Manager API `GroupTotal`/`GroupNext` (A39: set A), not the plan subset.

Phase 1 gate / A07: after connect **and on periodic resync / reconnect**, call `GetGroupsAsync`. Empty or partial census is not “all groups discovered.”

§62 / A53: MT5 unavailable → do not invent data; **continue retrying**; expose stale-source status. Swallow-and-exit is not retry.

## Evidence quotes

`LiveIngestHostedService` is the live ingest entry point (`AddHostedService<LiveIngestHostedService>`). It does **not** call `GetGroupsAsync` itself. It runs **one** delayed pass over `registry.All()`, then returns. There is no `while`, no `PeriodicTimer`, no reconnect loop. The only delay in this file is a 2s startup sleep:

```21:33:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
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
```

Per broker it `ConnectAsync`s, then `SyncBrokerAsync`, then scores **already-persisted** logins. Group discovery is entirely delegated. Success log records deals / accounts / scored — **not** group count or `GroupTotal` vs upserted:

```35:57:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            foreach (var connector in registry.All())
            {
                _log.LogInformation("Live ingest starting for {Broker}", connector.BrokerCode);
                try
                {
                    await connector.ConnectAsync(stoppingToken);
                    var n = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                    }

                    _log.LogInformation("{Broker} ingest done. dealsInserted={Deals} accounts={Accounts} scored={Scored}",
                        connector.BrokerCode, n, logins.Count, scored);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "{Broker} live ingest failed. No dummy data will be substituted.", connector.BrokerCode);
                }
            }
```

This file does **not** contain:

- `GetGroupsAsync` / `GroupTotal` / `GroupNext` / `GetAllGroups`
- any assert that `groups.Count > 0` or `groups.Count == GroupTotal`
- retry / resync after failed or empty discovery
- `MT5_GROUP_*` / `PlanMapping` as a fetch list (plan-map filter is absent here — that part of §9 is OK)

What `SyncBrokerAsync` actually does (called from this host, not owned by it): it **does** ask the connector for groups and upserts every returned name, with `GetAccountsAsync(null)`:

```40:47:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
```

That callee shape is the required §7/§9 *list* (no plan-map intersection). It is **not** a complete ALL-groups guarantee:

1. **One-shot host.** `ExecuteAsync` ends after the first `foreach`. Groups created after that pass, or groups that were not yet in the Manager pump cache at `Connect`+2s, are never fetched. Architecture required startup **and resync**.

2. **Failed fetch is swallowed.** Per-broker `catch` logs and continues. No retry. If `ConnectAsync` or `GetGroupsAsync` (inside `SyncBrokerAsync`) throws, that broker’s group census is empty/stale for the life of the process. Outer catch (`Live ingest host failed`) also does not retry. Violates §62 “Continue retrying.”

3. **Empty list is success.** If the connector returns `[]` (pump not ready; `GroupNext` all fail and the native walker `continue`s), `SyncBrokerAsync` upserts nothing, `n==0`, `logins` may be empty or leftover, and the host logs `{Broker} ingest done`. There is no fail-closed on zero groups.

4. **Partial native walk is invisible to this host.** `GetGroupsCore` skips failed `GroupNext` and returns a shorter list as a normal result. The host never compares against `GroupTotal()` and never logs names:

```128:145:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            var total = _manager!.GroupTotal();
            var list = new List<Mt5GroupDto>((int)total);
            var grp = _manager.GroupCreate();
            try
            {
                for (uint i = 0; i < total; i++)
                {
                    if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                        continue;
                    list.Add(new Mt5GroupDto(
```

5. **Post-ingest scoring is not a group census.** `store.ListLoginsAsync` only sees logins already in the store. It cannot detect groups the Manager can see that were never upserted.

6. **No pump-ready wait.** Host `ConnectAsync` then immediately `SyncBrokerAsync` (which `ConnectAsync`s again). Native connect uses `PUMP_MODE_GROUPS` but this file does not wait for group sink / non-zero `GroupTotal` before declaring ingest done.

Not a plan-map filter bug in this file. It **is** a host-level failure to **obtain, verify, retry, and resync** the full Manager-visible group set.

## No-loss implication

Missing or empty group discovery means those groups’ accounts, 90-day deals, and positions never enter the store. Reconstruction/scoring/`ListLoginsAsync` then run on a **subset** of the Manager book.

Capital-relevant effects:

- Source traders in unmapped / later / unpumped groups (anything beyond whatever `GroupNext` returned on the first pass, including `demo\Maxmaster` and non-`MT5_GROUP_*` books) are invisible to risk and copy.
- Empty or failed fetch logged as `{Broker} ingest done` (or swallowed as `{Broker} live ingest failed`) looks operationally complete while the live source universe is partial.
- Copy / kill-switch / scoring decisions then use an understated book: missed martingale or averaging-down books, missed source signals, false “synced.”

This slice does **not** invent dummy groups (the catch message is correct on that one point). The loss path is **omission**, not fabrication: ALL-groups failure is treated as a finished live ingest.

Residual: plan-map non-filter and `GetAccountsAsync(null)` in `DealIngestionService` are the right shape **if** the connector actually returns the full Manager set. This host does not ensure that condition.
