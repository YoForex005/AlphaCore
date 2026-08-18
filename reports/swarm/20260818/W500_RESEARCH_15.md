# W500_RESEARCH_15 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **15** |
| Date | 2026-08-18 |
| Role | Senior engineer — measured source + live-probe re-read (no product edit) |
| Topic | Check `DealIngestionService` `Take(200)` positions cap |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secrets written | **No** (passwords / proxy auth never copied) |

**Honesty rule:** quote the files as they sit on disk. Older swarm notes (A005 / A007 / W500_SLICE_7 / W500_SLICE_17) that still cite `foreach (var account in accounts.Take(200))` are **stale** vs current `D:\Prop\src`. Live Manager census is taken from the existing probe artifact, not invented.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `DealIngestionService` still applies `accounts.Take(200)` (or any first-N) on position snapshot | **No — cap is gone** | `EXISTS_AND_GOOD` (unbounded ingest) |
| Catalog fetches **all** groups the two Manager logins can see | **Yes (code + live probe)** | `MEASURED` |
| Catalog fetches **all** manager traders those groups contain | **Yes (code + live probe)** | `MEASURED` |
| Open-position replace is limited to first 200 accounts | **No** — bulk `"*"` or `foreach` **all** accounts | `EXISTS_AND_GOOD` |
| Residual `Take(200)` anywhere in C# | **Yes, one site** — `GET /api/trades` reconstructed rows | `NARROW leftover` (not positions, not capital) |
| Copy to cTrader can emit live `NewOrderSingle` (`35=D`) | **No** | `SAFE_BY_ABSENCE` |
| Capital at risk from this ingest / copy path | **None** | no-loss |

**One-line:** `Take(200)` is **not** on ingest/positions anymore; live Manager probe already returned **18 groups / 8460 traders / 1984 open positions**; cTrader TRADE may log on (`35=A`) but **cannot** send `35=D`.

---

## 1. Method (measured)

Re-read in full or in the cited ranges:

| Path | What was checked |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **145/145** lines — catalog + deals + positions |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | bulk + per-login ports (no N) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `GroupRequestArray("*")`, `UserRequestArray`, `PositionRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ReplacePositionsAsync` / `ReplaceBrokerPositionsAsync` / `ListLoginsAsync` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | scores **all** `ListLoginsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false`; native connectors only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | ACHIEVER + STARWAVEFX factory; no account window |
| `D:\Prop\apps\api\Program.cs` | `/api/trades` leftover `Take(200)`; `/api/ops/resync` all logins |
| `D:\Prop\apps\mt5-worker\Worker.cs` | still scores 4 demo logins if that host is used |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | only `35=A` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | forces `_runtime.RealCopyEnabled = false` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | traders = **all** `Mt5Accounts`; `Take(20)` on risk rejects only |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetPositions` / `GetUserLogins` — no 200 cap |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | live census 2026-08-18T08:42:16Z |

Workspace greps (C# product tree):

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(200)` under `D:\Prop` `*.cs` | **1** — `D:\Prop\apps\api\Program.cs:107` |
| `Take(` under `D:\Prop\src` `*.cs` | `EfDashboardQueries.cs:204` `.Take(20)` (risk rejects); `FixMessageParser.cs:45` checksum `parts.Take` |
| `35=D` / `(35, "D")` builder in `Fix.CTrader` Sessions | **0** |
| `GetGroupPositionsAsync` | ingest L83 + native L57–58 + `LiveBrokerProbe` L27–28 |

C++ YoPips backend: no `Take(200)` / first-200 account window on `GetPositions` or `GetUserLogins`. Chart `limit` and HTTP `200` status codes are unrelated.

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

That literal is **absent** from current `DealIngestionService.cs` (grep of `D:\Prop\src\Application\Ingestion` for `Take(200)|accounts.Take` = **0**).

What it would have done at today's census: snapshot positions for **200 of 8460** logins; leave `mt5_positions` empty/stale for **8260** accounts (including most of Achiever `demo\yo-2step` = 6295). Open-risk / no-loss copy reading that table would be **blind** — a capital-loss path **if** live send were armed. The loop is gone; that path is closed at this layer.

---

## 3. Current `DealIngestionService` — unbounded catalog + book

File is **145 lines**. Two public syncs; **zero** `Take(` / `Skip(` / `MaxAccounts`.

### 3.1 Catalog = every group + every login the connector returns

```37:50:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

```53:97:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        // ...
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        // bulk: foreach group → GetGroupDealsAsync
        // else: foreach account in accounts (NO Take)
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
        return insertedDeals;
    }
```

Live DI registers `NativeMt5BrokerConnector`, which **is** `IMt5BulkDealReader` + `IMt5BulkPositionReader`. Production ingest therefore:

1. Upserts **all** groups from `GroupRequestArray("*")`.
2. Upserts **all** users from `UserRequestArray` (fallbacks: `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`).
3. Pulls deals **per group** (not first 200 logins).
4. Replaces the **entire** broker position book via `PositionRequestByGroup("*")` → `ReplaceBrokerPositionsAsync`.

Fake connector (tests / leftover `DemoSeeder` only) does **not** implement the bulk ifaces, so it takes the `foreach (var account in accounts)` branch — still **no** `Take(200)`.

Store replace is also unbounded: `ReplaceBrokerPositionsAsync` deletes all `Mt5Positions` for the broker and inserts **every** DTO (`EfTradingStore.cs` L471–497). `ListLoginsAsync` is `Mt5Accounts.Where(broker).Select(Login)` with no `Take`.

---

## 4. Native Manager connector — no 200 window

`NativeMt5BrokerConnector` (459 lines) implements the bulk ports. Group / user / position enumerations walk `arr.Total()` / `GroupTotal()` with **no** `i < 200` cut.

| Call | Mask / filter | Cap in this file |
|---|---|---|
| `GroupRequestArray("*", arr)` then `GroupNext` fallback | all groups manager can see | none |
| `GetAccountsAsync(null)` | every group name from `GetGroupsCore` | none |
| `UserRequestArray(gname)` / `UserLogins` | per group | none |
| `GetGroupPositionsAsync(mask)` | ingest passes `"*"` | none |
| `ReadPositions` | `for (uint i = 0; i < arr.Total(); i++)` | none |

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

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors (`ACHIEVER`, `STARWAVEFX`). No `MaxAccounts`. Dummy/fake path is refused when passwords are missing (`DependencyInjection.cs` L35–36).

---

## 5. Measured live census (same APIs ingest uses)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
UTC: `2026-08-18T08:42:16.8519545+00:00`  
Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`

| Broker | Connect | Groups | Traders | Open positions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | OK (proxy) | 8 | **6512** | **1506** | 7212.6 |
| STARWAVEFX | OK (direct) | 10 | **1948** | **478** | 6413.5 |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `demo\FX2\grp2` **1735**, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

These counts are **> 200** on both brokers. A still-live `Take(200)` could not have produced 6512 / 1948 account rows or 1506 / 478 open positions from the same connector.

Honest bound: this is **every group / login those two Manager ACLs can see**. Server-side groups outside the manager permission set are not claimed.

---

## 6. Who consumes the full census (and who does not)

| Consumer | Accounts / positions | Score loop |
|---|---|---|
| `LiveIngestHostedService` (API DI hosted) | `SyncCatalogAsync` + `SyncBrokerAsync` (all / `"*"`) | **all** `store.ListLoginsAsync` |
| `POST /api/ops/resync` | same, both `ACHIEVER` and `STARWAVEFX` | **all** `ListLoginsAsync` |
| `GET /api/groups` / `GET /api/traders` | all persisted `Mt5Groups` / `Mt5Accounts` — **no** `Take(200)` | n/a |
| `apps/mt5-worker/Worker.cs` | still calls `SyncBrokerAsync` for both brokers (full catalog if native) | **hardcoded** `{10001,10002,10003,99001}` — demo score set; **not** a position cap |
| `GET /api/trades` | reconstructed trade **rows** `OrderByDescending(OpenedAt).Take(200)` | explorer page only |

Dashboard traders (`EfDashboardQueries.GetTradersAsync` L85–128) iterate **every** `Mt5Accounts` row, left-join scores. No first-200 login window.

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

This cannot hide live MT5 exposure. It can hide older reconstructed rows from the explorer.

`EfDashboardQueries.GetRiskAsync` `.Take(20)` is reject-reason display only.

---

## 7. C++ YoPips backend (adjacent, not Prop ingest)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- `GetUserLogins` assigns `raw_logins` … `raw_logins + total` (L315–327) — **no 200**.
- `GetPositions` walks `positions->Total()` after `PositionGet` / `PositionRequest` (L396–425) — **no 200**.

That process is the **prop-firm** Manager client (includes `DealerSendOrder` for challenge ops). It is **not** the Prop cTrader copy sender. Slot 15’s no-loss claim for **copy to cTrader** is owned by `D:\Prop` FIX code, not by YoPips dealer send.

---

## 8. Copy to cTrader — live orders still impossible (no loss)

Goal constraint: fetch-all must **not** imply live destination orders.

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| `DependencyInjection` `LiveRuntimeStatus.RealCopyEnabled` | **hardcoded `false`** with comment “Do not arm a flag that cannot be honored safely.” |
| `CTraderFixLogonHostedService` | may TCP/TLS Logon (`35=A`) on QUOTE 5211 + TRADE 5212; then `_runtime.RealCopyEnabled = false`; log: `"NewOrderSingle still disabled"` |
| `CTraderFixSession.BuildLogon` | tags include `(35, "A")` only — **no** `D` |
| `CTraderQuoteService` outgoing | `35=y` SecurityList, `35=V` MarketDataRequest — **not** orders |
| `apps/fix-worker` | stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."`; if config true, **still refuses** until risk/recon gates (which are not wired to a sender) |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED` = runtime (false); `FEATURE_COPY_TRADING_ENABLED` = **false** |
| `RiskEngine` `AllowFixSend` | requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`; rejects force `false`. **No socket write** even on approve |
| Product `35=D` / NewOrderSingle builder | **MISSING** (`SAFE_BY_ABSENCE`) |

Flipping a config bool does **not** place size: there is no function that emits FIX `MsgType=D` to a TRADE socket.

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
2. `apps/mt5-worker` still **scores** four demo logins; do not use that host as the live scorer. API `LiveIngestHostedService` / `/api/ops/resync` score all persisted logins.
3. Native group-position failure returns empty → store wipe of that broker’s positions (error path, not a 200-slice).
4. Manager ACL / pump-mode scope is the outer bound of “ALL”.
5. Deal window is caller-supplied (hosted: −90d; mt5-worker: −30d), 14-day Manager chunks — history completeness, not position-account cap.
6. In-memory DB on missing `DATABASE_URL` — census is not durable across API restarts (probe JSON is the permanent login list).

---

## 11. Verdict recap

**PASS** on slot 15’s assigned question.

- `DealIngestionService` **does not** cap position sync at 200 accounts.
- Achiever + Starwave catalog + traders + open book are fetched **without** a first-N login window.
- Live probe already measured **18 / 8460 / 1984** — proof the connector APIs are not secretly 200-capped in this worktree.
- Copy-to-cTrader remains **no-loss**: logon/recon only; **no** live `NewOrderSingle`.
