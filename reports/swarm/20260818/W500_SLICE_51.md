# W500_SLICE_51

- **slot:** 51
- **file:** `D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (65 lines) via `read_file`; grep of this file for `group` (zero hits); followed `ingest.SyncBrokerAsync` → `DealIngestionService` and `NativeMt5BrokerConnector.GetGroupsCore`
- **verdict:** FAIL

## Binding law (this angle)

Architecture v2 §7: startup/resync must **dynamically enumerate all groups accessible to the Manager login** (Connect → enumerate groups → upsert → accounts → history). `demo\Maxmaster` is not the only group.

Architecture v2 §9: `MT5_GROUP_*` plan mappings must **not** determine which groups are fetched. Discovery is Manager API `GroupRequestArray("*")` / `GroupTotal`/`GroupNext` (A39 set A), not the plan subset.

Phase 1 / A07 / A39: after connect **and on periodic resync**, obtain the full Manager-visible census. Empty or partial list is not “all groups discovered.” `GroupTotal()==0` on a no-pump session is not “broker has no groups” without `GroupRequestArray("*")`.

§62 / A53: MT5 unavailable → do not invent data; **continue retrying**; expose stale-source status. Swallow-and-exit is not retry.

## Evidence quotes

`LiveIngestHostedService` is the live ingest entry point. It does **not** mention groups. Grep of this file for `group` / `GetGroups` / `GroupTotal` / `GroupRequest` is empty. `ExecuteAsync` sleeps 2s, opens one DI scope, walks `registry.All()` **once**, then returns. There is no `while`, no `PeriodicTimer`, no reconnect, no second census.

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

Per broker it `ConnectAsync`s, then `SyncBrokerAsync` (return value is **deal insert count only**), then scores `store.ListLoginsAsync`. The success log records deals / accounts / scored — **not** group count, not `GroupTotal`, not names:

```35:63:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
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
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Live ingest host failed");
        }
```

This file does **not** contain:

- `GetGroupsAsync` / `GroupRequestArray` / `GroupTotal` / `GroupNext` / `GetAllGroups`
- any assert that `groups.Count > 0` or `groups.Count == GroupTotal`
- retry / resync after failed, empty, or partial discovery
- `MT5_GROUP_*` / plan-map as a fetch list (plan-map filter is absent here — that part of §9 is OK)

### What the callee actually does (current tree, not W500_SLICE_1’s stale quotes)

`SyncBrokerAsync` **does** request groups (twice: via `SyncCatalogAsync` then again) and upserts the returned list. `GetAccountsAsync(null)` is the full-catalog shape. **Bulk deal history is per returned group name**, so a short group list drops those groups’ 90-day deals:

```53:70:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var catalog = await SyncCatalogAsync(brokerCode, ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;

        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
```

`catalog` (the only object that still holds `groups.Count`) is discarded (`_ = catalog`). The host therefore never sees how many groups were found.

Native discovery now prefers `GroupRequestArray("*")` (A39 no-pump complete enumerator). Fallback to `GroupTotal`/`GroupNext` runs **only when `list.Count == 0`**. A non-empty **partial** request result is accepted; failed `GroupRequestArray` (not OK / OK_NONE) is swallowed; failed `GroupNext` is `continue`d:

```144:185:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        lock (_gate)
        {
            Ensure();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Mt5GroupDto>();

            var arr = _manager!.GroupCreateArray();
            try
            {
                var res = _manager.GroupRequestArray("*", arr);
                if (res == MTRetCode.MT_RET_OK || res == MTRetCode.MT_RET_OK_NONE)
                {
                    for (uint i = 0; i < arr.Total(); i++)
                    {
                        var g = arr.Next(i);
                        if (g is null)
                            continue;
                        AddGroup(list, seen, g);
                    }
                }
            }
            finally { arr.Release(); }

            if (list.Count == 0)
            {
                var grp = _manager.GroupCreate();
                try
                {
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
                        AddGroup(list, seen, grp);
                    }
                }
                finally { grp.Release(); }
            }

            return list;
        }
    }
```

Connect first tries `PUMP_MODE_GROUPS|USERS|POSITIONS`, then silently falls back to `PUMP_MODE_NONE` (cache may be empty; request API is then the only complete path). This host does not wait for pump-ready or non-zero `GroupTotal` before `SyncBrokerAsync`.

Positions use mask `"*"` (`GetGroupPositionsAsync("*")`) while deals walk the enumerated group list. Completeness of positions is therefore **not** evidence that ALL groups’ deal books were fetched.

### Why this host still FAILs ALL-groups

1. **One-shot host.** `ExecuteAsync` ends after the first `foreach`. Groups created after that pass, groups not yet visible 2s after process start, or groups missed by a partial `GroupRequestArray` are never fetched. Architecture required startup **and resync**.

2. **Failed fetch is swallowed.** Per-broker `catch` logs and continues. No retry. If `ConnectAsync` or `GetGroupsAsync` (inside `SyncBrokerAsync`) throws, that broker’s census stays empty/stale for the life of the process. Outer catch (`Live ingest host failed`) also does not retry. Violates §62 “Continue retrying.”

3. **Empty or partial list is success.** If the connector returns `[]` (`GroupRequestArray` OK_NONE + cold `GroupTotal==0`, or pump not ready), `SyncBrokerAsync` upserts nothing, bulk `foreach (var group in groups)` inserts zero deals, `n==0`, and the host logs `{Broker} ingest done`. There is no fail-closed on zero groups and no compare to `GroupTotal()`.

4. **Host cannot see the census.** `SyncBrokerAsync` returns `int` deals only. `BrokerSyncResult.Groups` is thrown away. Success telemetry cannot distinguish “all manager groups” from “three of forty.”

5. **Post-ingest scoring is not a group census.** `store.ListLoginsAsync` only sees logins already in the store. It cannot detect Manager-visible groups that were never upserted.

6. **Not a plan-map filter bug in this file.** Absence of `MT5_GROUP_*` is correct. The defect is host-level failure to **obtain, verify, retry, and resync** the full Manager-visible set.

Compared with `W500_SLICE_1.md`: the connector **now** calls `GroupRequestArray("*")` (that earlier quote is stale). That does **not** make this host PASS. The host still never checks, never retries, and never resyncs.

## No-loss implication

Missing or empty group discovery means those groups’ accounts and 90-day deals never enter the store (bulk deals are `foreach` on the returned names). Reconstruction / scoring / `ListLoginsAsync` then run on a **subset** of the Manager book.

Capital-relevant effects:

- Source traders in unmapped / later / unpumped / ACL-visible-but-unenumerated groups are invisible to risk and copy.
- Empty or failed fetch logged as `{Broker} ingest done` (or swallowed as `{Broker} live ingest failed`) looks operationally complete while the live source universe is partial.
- Copy / kill-switch / scoring then use an understated book: missed martingale or averaging-down books, missed source signals, false “synced.”
- Positions via `"*"` can look live while the deal history used for no-loss / risk scoring omitted whole groups.

This slice does **not** invent dummy groups (the catch message is correct on that one point). The loss path is **omission**, not fabrication: ALL-groups failure is treated as a finished live ingest.

Residual: plan-map non-filter, `GetAccountsAsync(null)`, and `GroupRequestArray("*")` in the connector are the right *shape* **if** the result is complete and the host re-verifies/resyncs. This host does not ensure that condition.
