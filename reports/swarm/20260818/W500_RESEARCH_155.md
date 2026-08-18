# W500_RESEARCH_155 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **155** |
| Date | 2026-08-18 |
| Role | Senior engineer — independent re-read of current disk (no product edit) |
| Topic | Check `DealIngestionService` `Take(200)` positions cap |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secrets written / printed | **No** (passwords, proxy auth, FIX password never copied) |
| Siblings on same angle | `W500_RESEARCH_15.md`, `W500_RESEARCH_35.md`, `W500_RESEARCH_55.md`, `W500_RESEARCH_75.md`, `W500_RESEARCH_95.md`, `W500_RESEARCH_115.md`. This slot **re-reads** the tree; it does **not** inherit 15’s “scores all `ListLoginsAsync`” claim or 115’s “DI/logon pin `RealCopyEnabled=false`” claim. |

**Honesty rule:** quote files as they sit now. A005 / A007 / W500_SLICE_7 / W500_SLICE_17 that still print `foreach (var account in accounts.Take(200))` are **stale vs `D:\Prop\src`**. W500_RESEARCH_15’s claim that `LiveIngestHostedService` scores **all** `ListLoginsAsync` is **stale**. W500_RESEARCH_115 §10 claim that DI L38–41 and `CTraderFixLogonHostedService` L68 force `RealCopyEnabled=false` is **stale vs current disk** — DI now binds env `REAL_COPY_EXECUTION_ENABLED`; lab `.env` L73 is `true`; logon host **does not** re-pin. Live Manager census is the existing probe artifact (`LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`). This slot did **not** re-attach to Manager and did **not** send any FIX order.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `DealIngestionService` still applies `accounts.Take(200)` (or any first-N) on position snapshot | **No — cap is gone** | `EXISTS_AND_GOOD` (unbounded ingest) |
| Catalog fetches **all** groups the two Manager logins can see | **Yes (code + prior live probe)** | `MEASURED` |
| Catalog fetches **all** manager traders those groups contain | **Yes (code + prior live probe)** | `MEASURED` |
| Open-position replace is limited to first 200 accounts | **No** — bulk `"*"` or `foreach` **all** accounts | `EXISTS_AND_GOOD` |
| Residual `Take(200)` anywhere in product C# | **Yes, one LINQ site** — `GET /api/trades` reconstructed rows (`Program.cs` **L110**). Sibling display: `ListIntentsAsync(200)` | `NARROW leftover` (not positions, not capital) |
| Hosted scoring walks every persisted login | **No** — `ListLoginsWithDealsAsync` only | `NARROW` (not a position cap) |
| Copy to cTrader can emit live `NewOrderSingle` (`35=D`) | **No** | `SAFE_BY_ABSENCE` |
| `REAL_COPY_EXECUTION_ENABLED` still hard-pinned false in process | **No** — DI binds env; lab `.env` is `true` | residual arm, **not** a sender |
| Capital at risk from this ingest / copy path | **None** | no-loss |

**One-line:** Ingest `Take(200)` is **absent**; live probe already returned **18 groups / 8460 traders / 1984 open positions** (counts a 200-account window cannot produce); cTrader TRADE may log on (`35=A`) but **cannot** send `35=D` because no builder exists (`NewOrderSingleImplemented=false`).

**Slot verdict:** `PASS_CAP_REMOVED`

**Risk to capital:** `NONE` (`SAFE_BY_ABSENCE`)

---

## 1. Method (measured this slot)

Re-read in full or in the cited ranges:

| Path | Lines / range checked |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **146/146** — `ITradingStore` + `SyncCatalogAsync` + `SyncBrokerAsync` + scoring sibling |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | **80/80** — bulk + per-login ports (no N) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | **458/458** — `GroupRequestArray("*")`, `UserRequestArray`, `PositionRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **151/151** interface impl; no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLogins*` L339–345; `ReplaceBrokerPositionsAsync` L475–501 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | **141/141** — scores `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **62/62** — `RealCopyEnabled` **from env**; native connectors only; fail-closed passwords |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | **94/94** — ACHIEVER + STARWAVEFX factory; no account window |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | traders = **all** `Mt5Accounts`; `.Take(20)` on risk rejects only |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **257/257** — `NewOrderSingleImplemented=false`; persist `AllowFixSend=false` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | **40/40** — SHADOW intents only |
| `D:\Prop\apps\api\Program.cs` | leftover `Take(200)` **L110**; `ListIntentsAsync(200)` L103; `/api/ops/resync` all `ListLoginsAsync` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | **scores 4 demo logins** if that host is used |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135/135** — only `(35, "A")` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `RealCopyExecutionEnabled = false` (unbound to env) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logs `RealCopyArmed={Armed}`; **does not** write `RealCopyEnabled` |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | in-memory `35=y` / `35=V` tag lists; **0** `WriteAsync` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | snapshot admits armed ≠ send |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` (L211) |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | same walk as ingest: `GetGroups` + `GetAccounts(null)` + `GetGroupPositionsAsync("*")` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetUserLogins` L315–328; `GetPositions` L396–426; `GetAllGroups` L962–982 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | live census 2026-08-18T08:42:16Z; group-name count **18**; header 8/6512/1506 + 10/1948/478 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | same census, no secrets |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | dashboard 18 / 8460; copy-off **doc is stale** on “forced false” |

Workspace greps (product C# / C++), re-run this slot:

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(200)` under `D:\Prop\apps` `*.cs` | **1** — `D:\Prop\apps\api\Program.cs:110` |
| `Take(` under product `*.cs` (`src` + `apps`) | `Program.cs:110` `.Take(200)`; `EfDashboardQueries.cs:204` `.Take(20)`; `FixMessageParser.cs:45` checksum `parts.Take`; `CopyTradingService.cs:67` `.Take(take)` |
| `Take(` / `accounts.Take` / `MaxAccounts` under `D:\Prop\src\Application` | **0** |
| `GetGroupPositionsAsync` under `D:\Prop\src` | ingest L84 + native L57–58 + contract L78 |
| `(35, "D")` / `35=D` builder in `Fix.CTrader` Sessions | **0** (only `(35, "A")` at `CTraderFixSession.cs:96`; one `WriteAsync` at L49) |
| Product `*.cs` / `*.json` / `*.csproj` `35=D` literal | **0** (name `NewOrderSingle` appears only in comments / log strings / `MayRetryNewOrderSingle`) |
| YoPips C++ `src` `35=D` / cTrader FIX sender | **0** (string `FIX` there is **bug-fix comments**, not protocol) |
| C++ first-200 window on `GetPositions` / `GetUserLogins` / `GetAllGroups` | **0** (`feature_flags.h` `max_accounts_per_user` default **5** is a **prop-firm user quota**, not a Manager census cap) |
| `ExecutionIntents.Add` / `new ExecutionIntent` under `D:\Prop\src` | **0** (DbSet + entity + count-only) |

This slot did not execute a live Manager connect and did not compile.

---

## 2. Historical cap (stale) vs current source

A005 (`D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md`) / A007 quoted this loop as the silent first-200 position snapshot:

```csharp
foreach (var account in accounts.Take(200))
{
    var positions = await connector.GetPositionsAsync(account.Login, ct);
    await _store.ReplacePositionsAsync(...);
}
```

That literal is **absent** from current `DealIngestionService.cs`. Grep of `D:\Prop\src` for `Take(200)|accounts.Take` = **0**. Grep of `D:\Prop\src\Application` for `Take(` = **0**.

What that old cap would have done at current scale: catalog could still upsert 8460 logins, but `mt5_positions` would refresh **only the first 200** of the connector’s account-list order. Accounts 201+ would keep stale or empty open risk. `demo\yo-2step` alone is **6295** logins — a 200-account window would miss almost the entire Achiever book. Once a send path existed, copy / flatten / recon would be **blind to the tail** — a capital-loss path. That is why this slot exists.

---

## 3. Current `DealIngestionService` — no first-N

File: `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (146 lines).

Catalog (all groups + all accounts, `group=null` means every name from `GetGroupsAsync`):

```38:51:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

Deals + positions. Native connector implements `IMt5BulkDealReader` + `IMt5BulkPositionReader`, so the live path is **group-wide**, not first-200 logins:

```54:98:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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

Measured facts on this file:

- **Zero** `Take(` / `Skip(` / `First(` / `MaxAccounts` / literal `200`.
- Group discovery is **not** filtered by plan mapping (no `MT5_GROUP_*` argument).
- Fake connector (tests / leftover `DemoSeeder` only) does **not** implement the bulk ifaces (`FakeMt5BrokerConnector` is `IMt5BrokerConnector` only; `GetPositionsAsync` filters by login with no N). Fake takes `foreach (var account in accounts)` — still **no** `Take(200)`.
- Completeness of the open book now depends on `PositionRequestByGroup("*")` succeeding (or the per-login fallback walking **all** accounts). Re-introducing `accounts.Take(200)` would be a **caller defect**.

Ports (`Mt5Contracts.cs` 53–79) declare no page size. `IMt5BulkPositionReader.GetGroupPositionsAsync(string? groupMask)` is the unbounded bulk.

---

## 4. Native connector — Manager request APIs, no 200 window

`NativeMt5BrokerConnector` is `IMt5BrokerConnector, IMt5BulkDealReader, IMt5BulkPositionReader` (L24).

| Method | What it walks | Cap? |
|---|---|---|
| `GetGroupsCore` L144–187 | `GroupRequestArray("*")`; fallback `GroupTotal` + `GroupNext` | **No** |
| `GetAccountsCore(null)` L189–214 | every name from `GetGroupsCore`, then `ReadAccountsForGroup` | **No** |
| `ReadAccountsForGroup` L216–271 | `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins` | **No** |
| `GetGroupDealsCore` L296–317 | `DealRequestByGroup` over 14-day windows | **No** (time slice, not account N) |
| `GetGroupPositionsCore` L336–353 | `PositionRequestByGroup(mask)` then `PositionGetByGroup` | **No** |
| `ReadPositions` L383+ | `for (uint i = 0; i < arr.Total(); i++)` | **No** |

Account walk (null group = all Manager-visible groups):

```189:214:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }

            return byLogin.Values.ToList();
        }
    }
```

Bulk positions used by ingest (`"*"`):

```336:353:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
            }
            finally { arr.Release(); }
        }
    }
```

Honesty on the empty-array path: if **both** `PositionRequestByGroup` and `PositionGetByGroup` fail with a non-OK/non-empty code, this returns `Array.Empty`. `ReplaceBrokerPositionsAsync` then **deletes every** `Mt5Positions` row for that broker and inserts nothing. That is a **wipe-on-error** risk for the dashboard book, not a 200-account cap, and it still cannot send a live cTrader order.

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors (`ACHIEVER`, `STARWAVEFX`). Starwave `ProxyEnabled = false` (hard pin, L45). No Fake is registered when real passwords are present (`HasRealPasswords` fail-closed at `DependencyInjection.cs` L36–37). No account-count knob.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L25–29) uses the **same** unbounded APIs the ingest path uses: `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. The 08:42Z JSON is therefore a measure of this walk, not a different enumerator.

---

## 5. Store + dashboard — persist all, display all traders

`ReplaceBrokerPositionsAsync` replaces the **entire broker book** with whatever list the connector returned (no `Take`):

```475:501:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public async Task ReplaceBrokerPositionsAsync(Guid brokerId, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct)
    {
        var existing = _db.Mt5Positions.Where(p => p.BrokerId == brokerId);
        _db.Mt5Positions.RemoveRange(existing);
        foreach (var p in positions)
        {
            _db.Mt5Positions.Add(new Mt5Position { /* maps every DTO field; no N */ });
        }

        await _db.SaveChangesAsync(ct);
    }
```

Login lists (no `Take`):

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

`EfDashboardQueries.GetGroupsAsync` iterates **every** `Mt5Groups` row. `GetTradersAsync` is account-driven (A005 “scores-only” is stale):

```85:128:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(...)
    {
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        foreach (var account in accounts)
        {
            // left-join scores; missing score → INSUFFICIENT_DATA
        }
        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
    }
```

**No** `Take(200)` on the trader census. Optional filters are broker/state only. The only dashboard `Take` is risk rejects `.Take(20)` (`EfDashboardQueries.cs:204`).

---

## 6. Residual `Take(200)` — reconstructed-trade explorer only

The only product C# `Take(200)` LINQ site (line **110**, not the L107 cited by slot 115):

```104:112:D:\Prop\apps\api\Program.cs
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

This is a **read page** of reconstructed trade **rows**, newest 200 by `OpenedAt`. It does not enumerate Manager logins, does not snapshot positions, and cannot send FIX. `broker` query-string is unused. Do not claim “zero `Take(200)` in the tree.”

Sibling display window (not LINQ `Take(200)`, but a 200-row page): `GET /api/copy/intents` → `ListIntentsAsync(200)` (`Program.cs` L103). Same class: explorer page, not Manager ACL.

`POST /api/ops/resync` walks **both** broker codes (`ACHIEVER`, `STARWAVEFX`), `SyncCatalogAsync` + `SyncBrokerAsync`, then scores **every** `ListLoginsAsync` login (L134–140). That is heavier than the hosted deals-only score path; still not a position cap.

---

## 7. Hosted ingest scoring vs catalog (not a position cap)

`LiveIngestHostedService` (API live path):

1. `SyncCatalogAsync` for every registered connector (all groups + all accounts).
2. `SyncBrokerAsync` (deals + positions as in §3).
3. Score **`ListLoginsWithDealsAsync`**, not every catalog login:

```105:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    // ...
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                    }
                    _log.LogInformation("{Broker} scored {Scored} logins that have deals", connector.BrokerCode, scored);
```

W500_RESEARCH_15 said this host scores all `ListLoginsAsync`. **That is stale.** Catalog still persists **all** manager traders; scoring is the deals-only subset. That is **not** a `Take(200)` positions cap and does **not** drop groups/accounts from the census. Unscored catalog rows still appear on `/api/traders` as `INSUFFICIENT_DATA`.

`apps/mt5-worker/Worker.cs` (separate host, if started) still scores only `{10001,10002,10003,99001}` after calling `SyncBrokerAsync` for both codes (L29–35). That is a **demo-scoring leftover**, not a Manager fetch cap. Live API DI does not use Fake; worker scoring of those four logins is a no-op / miss on a live book.

---

## 8. YoPips C++ backend — same “no 200 window” on Manager reads

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

`GetUserLogins` assigns the **full** `UserLogins` buffer:

```315:328:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint64_t* raw_logins = nullptr;
    uint32_t total = 0;

    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;

    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetPositions` walks `positions->Total()` with no slice (L418–422). `GetAllGroups` walks `GroupTotal()` + `GroupNext` (L962–982). No `resize(200)`, no first-N. Feature-flag `getMaxAccountsPerUser()` default **5** (`feature_flags.h` L230) is a **per-user product** limit in YoPips, not a Manager census cap and not used by Prop ingest. HTTP admin `limit` max **200** on compliance/dashboard/deals pages is a **JSON page**, not `GetUserLogins`.

Prop ingest does **not** call this C++ process. It is a sibling Manager client. Absence of a 200 window there is corroboration, not the live path. YoPips `src` has **no** cTrader FIX `35=D` sender (string `FIX` in that tree is bug-fix comments).

---

## 9. Prior live census (not re-run this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc `2026-08-18T08:42:16.8519545+00:00`  
Note on disk: `"Passwords never written. Groups and manager logins only."`

This slot **re-summed** the JSON headers + `groupNames[].accounts` (did not dump logins):

| Broker | Connect | Groups | Traders (header) | Open positions | `groupNames` count |
|---|---|---:|---:|---:|---:|
| ACHIEVER | OK (HTTP proxy; `elapsedMs` 7212.5885) | 8 | 6512 | 1506 | 8 |
| STARWAVEFX | OK (direct; `elapsedMs` 6413.478) | 10 | 1948 | 478 | 10 |
| **Total** | | **18** | **8460** | **1984** | **18** |

Achiever group `accounts` sum (2+179+4+5+4+6295+0+23) = **6512**.  
Starwave group `accounts` sum (11+4+170+1735+22+0+0+4+0+2) = **1948**.  
JSON `"name"` keys under `groupNames` = **18** (8 Achiever + 10 Starwave). Achiever trader array last login line 45635 (`333106` / `demo\yo-payp`); first login line 58 → `(45635-58)/7+1` = **6512**.

Achiever breakdown: `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave breakdown: `Starwave\cent\FX1\grp1` 11, `grp2` 4, `Starwave\demo\FX2\grp1` 170, `grp2` **1735**, `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `Starwave\real\FX3\LP` 2.

Empty groups (`demo\yo-instant`, three Starwave real grps) prove the enumerator is group-config / `GroupRequestArray`, not “groups that happened to have pumped users.”

These counts are **> 200** on both brokers. A still-live `accounts.Take(200)` on position refresh **could not** have produced 1506 / 478 open-position rows from the same connector in one pass. Dashboard `/api/traders` = 8460 and `/api/groups` = 18 were recorded in `CREDENTIALS_AND_COPY_STATUS.md` / `LIVE_MANAGER_FETCH_MEASURED.md`.

These are **all groups / logins those two manager records can see**. Groups outside the manager ACL are invisible by design, not by a 200 cap.

A later swarm note (`P500_CODE_15.md`) cites a live caller of **8463** vs this file **8460** (delta 3). This slot does **not** re-probe and does **not** greenwash the delta. Either way the count is an order of magnitude above 200.

---

## 10. Copy to cTrader — no live orders (no loss)

Safety today is **absence of a sender**, not a process pin of `RealCopyEnabled=false`. Slot 115 §10 is **wrong on current disk**.

| Gate | Measured state **now** |
|---|---|
| `DependencyInjection` L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` — **env-bound** |
| Lab `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (value only; no other secrets quoted) |
| `CTraderFixLogonHostedService` L68–70 | logs `RealCopyArmed={Armed}`; **does not assign** `_runtime.RealCopyEnabled` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default **false** (L35); **not** bound to the env key |
| `CTraderFixSession.BuildLogon` L96 | only `(35, "A")` |
| `CTraderFixSession` L49 | only `WriteAsync` is the Logon; sockets disposed after read |
| `CTraderFixSession` | no `(35, "D")`, no NewOrderSingle builder, no ClOrdID send |
| `CTraderQuoteService` | in-memory `35=y` / `35=V` tag lists; **0** `WriteAsync` (not on a socket) |
| `CopyTradingService` L15–16 | `VenueReconciled = false`; `NewOrderSingleImplemented = false` |
| `CopyTradingService` L192 | persist `AllowFixSend = false` **unconditionally** |
| `CopyTradingService` L198–224 | even the dead branch is `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; else `SHADOW_ONLY` |
| `CopyTradingHostedService` | only calls `GenerateShadowIntentsAsync` |
| `ExecutionIntent` writers in product | **0** (`CountAsync` only) |
| `BaselineScorer.CanPromoteToLive` | **false** (hard) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled=true`, logs a warning and **still refuses** send; stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` |
| `/api/settings` L76–77 | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (**follows env**); `FEATURE_COPY_TRADING_ENABLED` hardcoded **true** |
| `/api/reconciliation/status` L69 | note: `"NewOrderSingle still off"` |
| `LiveRuntimeStatus.Snapshot` L42–44 | if armed: `"REAL_COPY armed. NewOrderSingle still unimplemented; … No ticket will be sent."` |

`CTraderFixSession` can open TLS to 5211/5212 and exchange Logon (`35=A`). LoggedOn ≠ armed copy ≠ send. There is **no** method that assembles tag `35=D`. That is `SAFE_BY_ABSENCE`.

`RiskEngine.Evaluate` *can* set `AllowFixSend=true` when `RealExecutionEnabled && Reconciled && VenueHealthy && KillSwitch==None`. The product caller passes `Reconciled = VenueReconciled` (**const false**), then **overwrites** persist to `AllowFixSend=false`. Do not treat the engine as a live hop.

YoPips C++ `src` has `DealerSendOrder` for **MT5** dealer ops. That is not a cTrader FIX sender and is not on the Prop copy path.

`CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false** (forced)” is **stale** vs `DependencyInjection.cs` L41 + `.env` L73. The no-loss claim still holds because the sender is unimplemented.

---

## 11. Goal check (this slot)

| Goal item | Status |
|---|---|
| Fetch **ALL** Achiever + Starwave groups | **Code path = all visible.** `GetGroupsAsync` → `GroupRequestArray("*")` / `GroupTotal`. Prior measure: **8 + 10 = 18**. |
| Fetch **ALL** manager traders | **Code path = all visible.** `GetAccountsAsync(null)` walks every group. Prior measure: **6512 + 1948 = 8460**. |
| Positions not silently first-200 | **PASS.** Bulk `"*"` or `foreach` all accounts. |
| Copy to cTrader must not send live orders | **PASS.** `35=D` absent; `NewOrderSingleImplemented=false`. Env arm may be true; that does **not** emit an order. |
| No secrets printed | **PASS.** |

---

## 12. Residual debt (honest, not a fail of this angle)

1. `GET /api/trades` `Take(200)` hides older reconstructed rows from the explorer (`Program.cs` L110).
2. `GET /api/copy/intents` pages 200 intents (`ListIntentsAsync(200)`).
3. Hosted scoring is deals-only (`ListLoginsWithDealsAsync`). Catalog still has all logins.
4. `mt5-worker` still scores four demo logins if that process is started.
5. `GetGroupPositionsCore` empty-on-error + `ReplaceBrokerPositionsAsync` can wipe the broker book.
6. This slot did not re-run `LiveBrokerProbe`; census is the 08:42Z artifact.
7. Older swarm notes (A005, A007, W500_SLICE_7/17) still cite ingest `Take(200)` — treat as historical.
8. Unreconciled 8460 (probe file) vs 8463 (later live caller). Not a 200-cap issue.
9. Lab `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now **bound** by DI. Keep `NewOrderSingleImplemented=false` until §68/§70. Do not treat CREDENTIALS “forced false” as current process law.

---

## 13. Do not claim

- Do not claim “EX5 decompiled” or “≥95% copy live.”
- Do not claim zero `Take(200)` in the whole repo (HTTP trades page remains).
- Do not claim `RealCopyEnabled` is process-pinned false (that was true earlier; it is **not** true now).
- Do not claim this process can lose money on cTrader today — there is no `35=D` send path.
- Do not re-introduce `accounts.Take(200)` on positions. At 8460 logins that is a silent open-risk hole.
- Do not treat hosted deals-only scoring as “we only fetched 200 traders.” Catalog + positions are unbounded.

---

## 14. Files read (absolute)

- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (L211 only)
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (L147–187)
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (GetUserLogins / GetPositions / GetAllGroups)
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (headers + groupNames; no secret keys)
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\.env` L73 key presence only (`REAL_COPY_EXECUTION_ENABLED=true`)
