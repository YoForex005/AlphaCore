# W500_RESEARCH_35 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **35** |
| Date | 2026-08-18 |
| Role | Senior engineer — re-read current disk (no product edit) |
| Topic | Check `DealIngestionService` `Take(200)` positions cap |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secrets written / printed | **No** (passwords, proxy auth, FIX password never copied) |
| Sibling on same angle | `W500_RESEARCH_15.md` (same question, earlier pass). This slot **re-reads** the current tree; it does not inherit 15’s scoring-loop claim. |

**Honesty rule:** quote files as they sit now. A005 / A007 / W500_SLICE_7 / W500_SLICE_17 that still print `foreach (var account in accounts.Take(200))` are **stale vs `D:\Prop\src`**. W500_RESEARCH_15’s claim that `LiveIngestHostedService` scores **all** `ListLoginsAsync` is also **stale** — current host scores `ListLoginsWithDealsAsync`. Live Manager census is the existing probe artifact; this slot did **not** re-attach to Manager (no shell).

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `DealIngestionService` still applies `accounts.Take(200)` (or any first-N) on position snapshot | **No — cap is gone** | `EXISTS_AND_GOOD` (unbounded ingest) |
| Catalog fetches **all** groups the two Manager logins can see | **Yes (code + prior live probe)** | `MEASURED` |
| Catalog fetches **all** manager traders those groups contain | **Yes (code + prior live probe)** | `MEASURED` |
| Open-position replace is limited to first 200 accounts | **No** — bulk `"*"` or `foreach` **all** accounts | `EXISTS_AND_GOOD` |
| Residual `Take(200)` anywhere in product C# | **Yes, one site** — `GET /api/trades` reconstructed rows | `NARROW leftover` (not positions, not capital) |
| Hosted scoring walks every persisted login | **No** — `ListLoginsWithDealsAsync` only | `NARROW` (not a position cap) |
| Copy to cTrader can emit live `NewOrderSingle` (`35=D`) | **No** | `SAFE_BY_ABSENCE` |
| Capital at risk from this ingest / copy path | **None** | no-loss |

**One-line:** Ingest `Take(200)` is **absent**; live probe already returned **18 groups / 8460 traders / 1984 open positions** (counts that a 200-account window cannot produce); cTrader TRADE may log on (`35=A`) but **cannot** send `35=D`.

**Slot verdict:** `PASS_CAP_REMOVED`

**Risk to capital:** `NONE` (`SAFE_BY_ABSENCE`)

---

## 1. Method (measured this slot)

Re-read in full or in the cited ranges:

| Path | Lines / range checked |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **145/145** — catalog + deals + positions |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | bulk + per-login ports (no N) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | **458/458** — `GroupRequestArray("*")`, `UserRequestArray`, `PositionRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ReplacePositionsAsync` L116+; `ListLogins*` L339–345; `ReplaceBrokerPositionsAsync` L475–501 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | **141/141** — scores `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false`; native connectors only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | **94/94** — ACHIEVER + STARWAVEFX; no account window |
| `D:\Prop\apps\api\Program.cs` | leftover `Take(200)` L107; `/api/ops/resync` all logins |
| `D:\Prop\apps\mt5-worker\Worker.cs` | `SyncBrokerAsync` both brokers; scores 4 demo logins |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `(35, "A")` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | forces `_runtime.RealCopyEnabled = false` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | traders = **all** `Mt5Accounts`; `.Take(20)` on risk rejects only |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetUserLogins` L315–327; `GetPositions` L396–426 — no 200 cap |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | live census `2026-08-18T08:42:16.8519545+00:00` |

Workspace greps (this slot):

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(200)` / `accounts.Take` / `i < 200` / `first 200` / `MaxAccounts` under `D:\Prop\src\Application` | **0** |
| `Take(200)` under `D:\Prop\apps` `*.cs` | **1** — `D:\Prop\apps\api\Program.cs:107` |
| `Take(` under `D:\Prop\src` `*.cs` | `EfDashboardQueries.cs:204` `.Take(20)` (risk rejects); `FixMessageParser.cs:45` checksum `parts.Take` |
| `(35, "D")` / `35=D` / `MsgType="D"` builder in `Fix.CTrader` | **0** |
| `NewOrderSingle` in `Fix.CTrader` | **2** — XML comment (`CTraderFixOptions` L33); log format (`CTraderFixLogonHostedService` L70) |
| `GetGroupPositionsAsync` implementors | `NativeMt5BrokerConnector` only (`IMt5BulkPositionReader`) |
| C++ `Take(200)` / first-200 account window on `GetPositions` / `GetUserLogins` | **0** (`max_accounts_per_user` default **5** is a **prop-firm user quota**, not a Manager census cap) |

---

## 2. Historical cap (stale) vs current source

A005 / A007 quoted this loop as the silent first-200 position snapshot:

```csharp
foreach (var account in accounts.Take(200))
{
    var positions = await connector.GetPositionsAsync(account.Login, ct);
    await _store.ReplacePositionsAsync(...);
}
```

That literal is **absent** from current `DealIngestionService.cs`. Grep of `D:\Prop\src\Application` for `Take(200)|accounts.Take|i < 200|first 200|MaxAccounts` = **0**.

What it would have done at today’s census: snapshot positions for **200 of 8460** logins; leave `mt5_positions` empty/stale for **8260** accounts (including most of Achiever `demo\yo-2step` = 6295). Open-risk / no-loss copy reading that table would be **blind** — a capital-loss path **if** live send were armed. The loop is gone; that path is closed at this layer.

---

## 3. Current `DealIngestionService` — unbounded catalog + book

File is **145 lines**. Two public syncs; **zero** `Take(` / `Skip(` / `MaxAccounts`.

### 3.1 Catalog = every group + every login the connector returns

```37:51:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);

        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

- `GetGroupsAsync` has **no** plan-mapping / allow-list argument (B32 still true).
- `GetAccountsAsync(null, …)` is the contract for **all groups** (`Mt5Contracts.cs` L60). Native walks every discovered group name.

### 3.2 Deals + positions — all groups / all accounts / `"*"` book

```54:97:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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
        else
        {
            foreach (var account in accounts)
            {
                var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }

        if (connector is IMt5BulkPositionReader posBulk)
        {
            var positions = await posBulk.GetGroupPositionsAsync("*", ct);
            await _store.ReplaceBrokerPositionsAsync(brokerId, positions, ct);
        }
        else
        {
            foreach (var account in accounts)
            {
                var positions = await connector.GetPositionsAsync(account.Login, ct);
                await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
            }
        }

        _ = catalog;
        return insertedDeals;
    }
```

Live DI registers `NativeMt5BrokerConnector`, which **is** `IMt5BulkDealReader` + `IMt5BulkPositionReader` (only implementor of either bulk iface). Production ingest therefore:

1. Upserts **all** groups from `GroupRequestArray("*")`.
2. Upserts **all** users from `UserRequestArray` (fallbacks: `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`).
3. Pulls deals **per group** (not first 200 logins).
4. Replaces the **entire** broker position book via `PositionRequestByGroup("*")` → `ReplaceBrokerPositionsAsync`.

Fake connector (leftover `DemoBrokerFactory` / tests only) does **not** implement the bulk ifaces, so it takes the `foreach (var account in accounts)` branch — still **no** `Take(200)`. Live DI **refuses** to start without real Manager passwords (`DependencyInjection.cs` L35–36), so Fake is not the API path.

Store replace is also unbounded:

```475:500:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task ReplaceBrokerPositionsAsync(Guid brokerId, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct)
    {
        var existing = _db.Mt5Positions.Where(p => p.BrokerId == brokerId);
        _db.Mt5Positions.RemoveRange(existing);
        foreach (var p in positions)
        {
            _db.Mt5Positions.Add(new Mt5Position
            {
                // ... every DTO field ...
            });
        }

        await _db.SaveChangesAsync(ct);
    }
```

`ListLoginsAsync` is `Mt5Accounts.Where(broker).Select(Login)` with **no** `Take`. `ReplacePositionsAsync` (per-login fallback) deletes that login’s rows and inserts **every** DTO.

---

## 4. Native Manager connector — no 200 window

`NativeMt5BrokerConnector` (**458 lines**) implements the bulk ports. Group / user / position enumerations walk `arr.Total()` / `GroupTotal()` with **no** `i < 200` cut.

| Call | Mask / filter | Cap in this file |
|---|---|---|
| `GroupRequestArray("*", arr)` then `GroupNext` fallback | all groups manager can see | none |
| `GetAccountsAsync(null)` | every group name from `GetGroupsCore` | none |
| `UserRequestArray(gname)` / `UserLogins` | per group | none |
| `GetGroupPositionsAsync(mask)` | ingest passes `"*"`; empty mask becomes `"*"` (L58) | none |
| `ReadPositions` | `for (uint i = 0; i < arr.Total(); i++)` | none |

```144:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        // GroupRequestArray("*") then for i in 0..arr.Total()
        // fallback: GroupTotal() + GroupNext(i)
    }

    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // null/blank → foreach group from GetGroupsCore
        // ReadAccountsForGroup: UserRequestArray / UserGetByGroup / UserLogins
        // byLogin dictionary — no first-N
    }
```

```336:349:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5PositionDto> GetGroupPositionsCore(string mask)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.PositionCreateArray();
            try
            {
                var res = _manager.PositionRequestByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    res = _manager.PositionGetByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    return Array.Empty<Mt5PositionDto>();
                return ReadPositions(arr);
```

**Residual (not a 200-cap):** if both group-position APIs fail, this returns **empty** and `ReplaceBrokerPositionsAsync` would wipe the local book. That is an all-or-nothing error path, not a silent first-200 slice.

Pump on connect is `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`, with `PUMP_MODE_NONE` fallback (L89–111). Request-array paths still run after connect; pump-none does **not** introduce a 200 window.

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors (`BrokerCodes.Achiever` = `"ACHIEVER"`, `BrokerCodes.StarwaveFx` = `"STARWAVEFX"`). No `MaxAccounts`. Dummy/fake path is refused when passwords are missing.

---

## 5. Measured live census (same APIs ingest uses)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
UTC: `2026-08-18T08:42:16.8519545+00:00`  
Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`

This slot **re-read** the JSON headers (Achiever L7–53; Starwave L45644–45700). It did **not** re-run the probe.

| Broker | Connect | Groups | Traders | Open positions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | OK (proxy) | 8 | **6512** | **1506** | 7212.6 |
| STARWAVEFX | OK (direct) | 10 | **1948** | **478** | 6413.5 |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `demo\FX2\grp2` **1735**, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

These counts are **> 200** on both brokers. A still-live `Take(200)` could not have produced 6512 / 1948 account rows or 1506 / 478 open positions from the same connector.

Honest bound: this is **every group / login those two Manager ACLs can see**. Server-side groups outside the manager permission set are not claimed. Probe UTC is hours old relative to this write; if a group was added after `08:42Z`, this file does not contain that delta.

---

## 6. Who consumes the full census (and who does not)

| Consumer | Accounts / positions | Score loop |
|---|---|---|
| `LiveIngestHostedService` (API DI hosted) | `SyncCatalogAsync` + `SyncBrokerAsync` (all / `"*"`) | **`ListLoginsWithDealsAsync`** — logins that have **at least one persisted deal**, not every `Mt5Accounts` row |
| `POST /api/ops/resync` | same, both `ACHIEVER` and `STARWAVEFX` | **all** `ListLoginsAsync` |
| `GET /api/groups` / `GET /api/traders` | all persisted `Mt5Groups` / `Mt5Accounts` — **no** `Take(200)` | n/a |
| `apps/mt5-worker/Worker.cs` | still calls `SyncBrokerAsync` for both brokers (full catalog if native) | **hardcoded** `{10001,10002,10003,99001}` — demo score set; **not** a position cap |
| `GET /api/trades` | reconstructed trade **rows** `OrderByDescending(OpenedAt).Take(200)` | explorer page only |

Dashboard traders (`EfDashboardQueries.GetTradersAsync` L85–128) iterate **every** `Mt5Accounts` row, left-join scores, `OrderByDescending(EarlyScore)`. **No** `Skip`/`Take` on the trader census.

**Slot-35 delta vs W500_RESEARCH_15:** hosted scoring is **not** “all logins.” Current body:

```105:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // ...
                    foreach (var login in logins)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
```

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Goal text is **fetch** all groups/traders, not score every empty login. Catalog + positions still walk the full Manager set. Scoring a deals-only subset is **not** a `Take(200)` positions cap; it is residual scoring debt.

The leftover `Take(200)`:

```101:108:D:\Prop\apps\api\Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});
```

This cannot hide live MT5 exposure. It can hide older reconstructed rows from the explorer. Query-string `broker` is unused. `login` still applies `Take(200)` after `OpenedAt` desc.

`EfDashboardQueries.GetRiskAsync` `.Take(20)` is reject-reason display only.

---

## 7. C++ YoPips backend (adjacent, not Prop ingest)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    // ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;

    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

```396:425:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetPositions(uint64_t login, std::vector<PositionData>& out) {
    // PositionGet then PositionRequest fallback
    out.clear();
    for (uint32_t i = 0; i < positions->Total(); i++) {
        const IMTPosition* pos = positions->Next(i);
        if (pos) out.push_back(extractPosition(pos));
    }
    // ...
}
```

No 200-account window. `feature_flags.h` `getMaxAccountsPerUser()` default **5** is how many **challenge accounts one user** may hold — unrelated to Manager `GetPositions` / ingest.

That process is the **prop-firm** Manager client (includes `DealerSendOrder` for challenge ops). It is **not** the Prop cTrader copy sender. Slot 35’s no-loss claim for **copy to cTrader** is owned by `D:\Prop` FIX code, not by YoPips dealer send.

---

## 8. Copy to cTrader — live orders still impossible (no loss)

Goal constraint: fetch-all must **not** imply live destination orders.

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (L35) |
| `DependencyInjection` `LiveRuntimeStatus.RealCopyEnabled` | **hardcoded `false`** (L38–41) with comment “Do not arm a flag that cannot be honored safely.” |
| `CTraderFixLogonHostedService` | may TCP/TLS Logon (`35=A`) on QUOTE **5211** + TRADE **5212**; then `_runtime.RealCopyEnabled = false` (L68); log: `"NewOrderSingle still disabled"` (L70) |
| `CTraderFixSession.BuildLogon` | tags include `(35, "A")` only (L96) — **no** `D` |
| `CTraderQuoteService` outgoing | `35=y` SecurityList, `35=V` MarketDataRequest — **not** orders |
| `FixSimulationHarness` | `(35, "A")` / `"y"` / `"X"` — in-process, not venue send of `D` |
| `apps/fix-worker` | stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."`; if `CTrader:RealCopyExecutionEnabled=true`, **still refuses** (log only; no sender) |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED` = runtime (false); `FEATURE_COPY_TRADING_ENABLED` = **false** (hardcoded) |
| Product `35=D` / NewOrderSingle builder | **MISSING** (`SAFE_BY_ABSENCE`) |

Flipping a config bool does **not** place size: there is no function that emits FIX `MsgType=D` to a TRADE socket.

`DealIngestionService` itself never references FIX, `OrderQty`, or send. It persists + reconstruct/score is a sibling type in the same file. `PersistDemoShadowAsync` is a DB shadow write, not a venue order.

---

## 9. No-loss implication of removing the 200-cap

| Scenario | If `Take(200)` were still present | Current source |
|---|---|---|
| Achiever `demo\yo-2step` (6295 logins) | positions for accounts 201+ stale/empty | `"*"` book + 1506 measured opens |
| Copy later armed against `mt5_positions` | would size/flatten while blind to most inventory | book can be complete **if** Manager `"*"` succeeds |
| Today’s copy | N/A | **cannot send** — no `35=D` |

Completeness of the open book now depends on `PositionRequestByGroup("*")` succeeding (or the per-login fallback walking **all** accounts). Re-introducing `accounts.Take(200)` would be a **caller defect** and a capital-loss path once send exists.

---

## 10. Residual debt (honest, out of the removed cap)

1. `GET /api/trades` `Take(200)` — reconstructed explorer page.
2. `LiveIngestHostedService` scores **deals-only** logins (`ListLoginsWithDealsAsync`). Dashboard still lists all `Mt5Accounts`. Manual `/api/ops/resync` scores all logins.
3. `apps/mt5-worker` still **scores** four demo logins; do not use that host as the live scorer.
4. Native group-position failure returns empty → store wipe of that broker’s positions (error path, not a 200-slice).
5. Manager ACL / pump-mode scope is the outer bound of “ALL”.
6. Deal window is caller-supplied (hosted + resync: −90d; mt5-worker: −30d), 14-day Manager chunks — history completeness, not position-account cap.
7. In-memory DB on missing `DATABASE_URL` — census is not durable across API restarts (probe JSON is the permanent login list).

---

## 11. Verdict recap

**PASS** on slot 35’s assigned question.

- `DealIngestionService` **does not** cap position sync at 200 accounts.
- Achiever + Starwave catalog + traders + open book are fetched **without** a first-N login window.
- Prior live probe measured **18 / 8460 / 1984** — proof the connector APIs are not secretly 200-capped in this worktree.
- Copy-to-cTrader remains **no-loss**: logon/recon only; **no** live `NewOrderSingle`.
- Do **not** re-introduce `accounts.Take(200)`. Do **not** enable `RealCopyExecutionEnabled`. Do **not** add `35=D` from this slot.
