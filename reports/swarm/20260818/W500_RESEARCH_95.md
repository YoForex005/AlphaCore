# W500_RESEARCH_95 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **95** |
| Date | 2026-08-18 |
| Role | Senior engineer — independent re-read of current disk (no product edit) |
| Topic | Check `DealIngestionService` `Take(200)` positions cap |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secrets written / printed | **No** (passwords, proxy auth, FIX password never copied) |
| Siblings on same angle | `W500_RESEARCH_15.md`, `W500_RESEARCH_35.md`, `W500_RESEARCH_55.md`. This slot **re-reads** the tree; it does not inherit 15’s “scores all `ListLoginsAsync`” claim. |

**Honesty rule:** quote files as they sit now. A005 / A007 / W500_SLICE_7 / W500_SLICE_17 that still print `foreach (var account in accounts.Take(200))` are **stale vs `D:\Prop\src`**. W500_RESEARCH_15’s claim that `LiveIngestHostedService` scores **all** `ListLoginsAsync` is also **stale** — current host scores `ListLoginsWithDealsAsync`. Live Manager census is the existing probe artifact (`LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`). This slot did **not** re-attach to Manager and did **not** send any FIX order.

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

**One-line:** Ingest `Take(200)` is **absent**; live probe already returned **18 groups / 8460 traders / 1984 open positions** (counts a 200-account window cannot produce); cTrader TRADE may log on (`35=A`) but **cannot** send `35=D`.

**Slot verdict:** `PASS_CAP_REMOVED`

**Risk to capital:** `NONE` (`SAFE_BY_ABSENCE`)

---

## 1. Method (measured this slot)

Re-read in full or in the cited ranges:

| Path | Lines / range checked |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **146/146** — `ITradingStore` + `SyncCatalogAsync` + `SyncBrokerAsync` + scoring sibling |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | **79/79** — bulk + per-login ports (no N) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | **458/458** — `GroupRequestArray("*")`, `UserRequestArray`, `PositionRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **68/68** interface impl; no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLogins*` L339–345; `ReplaceBrokerPositionsAsync` L475–501 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | **141/141** — scores `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **59/59** — `RealCopyEnabled = false`; native connectors only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | **94/94** — ACHIEVER + STARWAVEFX factory; no account window |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | traders = **all** `Mt5Accounts`; `.Take(20)` on risk rejects only |
| `D:\Prop\apps\api\Program.cs` | leftover `Take(200)` L107; `/api/ops/resync` all `ListLoginsAsync` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | **scores 4 demo logins** if that host is used |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135/135** — only `(35, "A")` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | forces `_runtime.RealCopyEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | 0 `WriteAsync` / 0 `35=` / 0 `NewOrder` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetUserLogins` L315–328; `GetPositions` L396–426; `GetAllGroups` L962–982 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | live census 2026-08-18T08:42:16Z |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | same census, no secrets |

Workspace greps (product C# / C++):

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(200)` under `D:\Prop\apps` `*.cs` | **1** — `D:\Prop\apps\api\Program.cs:107` |
| `Take(` under `D:\Prop\src` `*.cs` | `EfDashboardQueries.cs:204` `.Take(20)` (risk rejects); `FixMessageParser.cs:45` checksum `parts.Take` |
| `Take(` / `accounts.Take` / `MaxAccounts` / `i < 200` under `D:\Prop\src\Application` | **0** |
| `GetGroupPositionsAsync` under `D:\Prop\src` | ingest L84 + native L57–58 + contract L78 |
| `(35, "D")` / `35=D` builder in `Fix.CTrader` Sessions | **0** (only `(35, "A")` at `CTraderFixSession.cs:96`; one `WriteAsync` at L49) |
| Product `*.cs` `35=D` literal | **0** (name `NewOrderSingle` appears only in comments / log strings / `MayRetryNewOrderSingle`) |
| C++ `Take(200)` / first-200 window on `GetPositions` / `GetUserLogins` / `GetAllGroups` | **0** (`feature_flags.h` `max_accounts_per_user` default **5** is a **prop-firm user quota**, not a Manager census cap) |

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

What that old cap would have done at current scale: catalog could still upsert 8460 logins, but `mt5_positions_current` would refresh **only the first 200** of the connector’s account-list order. Accounts 201+ would keep stale or empty open risk. Once a send path existed, copy / flatten / recon would be **blind to the tail** — a capital-loss path. That is why this slot exists.

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

`NativeMt5BrokerConnector` is `IMt5BrokerConnector, IMt5BulkDealReader, IMt5BulkPositionReader`.

| Method | What it walks | Cap? |
|---|---|---|
| `GetGroupsCore` L144–187 | `GroupRequestArray("*")`; fallback `GroupTotal` + `GroupNext` | **No** |
| `GetAccountsCore(null)` L189–214 | every name from `GetGroupsCore`, then `ReadAccountsForGroup` | **No** |
| `ReadAccountsForGroup` L216–271 | `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins` | **No** |
| `GetGroupPositionsCore` L336–353 | `PositionRequestByGroup(mask)` then `PositionGetByGroup` | **No** |
| `ReadPositions` L383–406 | `for (uint i = 0; i < arr.Total(); i++)` | **No** |
| `GetGroupDealsCore` L296–317 | `DealRequestByGroup` over 14-day windows | **No** (time slice, not account N) |

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

`LiveMt5Registration.CreateConnectors` builds **exactly two** native connectors (`ACHIEVER`, `STARWAVEFX`). Starwave `ProxyEnabled = false` (hard pin). No Fake is registered when real passwords are present (`HasRealPasswords` fail-closed at `DependencyInjection.cs` L35–36). No account-count knob.

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
            _db.Mt5Positions.Add(new Mt5Position
            {
                // ... maps every DTO field; no N ...
            });
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

`EfDashboardQueries.GetGroupsAsync` iterates **every** `Mt5Groups` row. `GetTradersAsync` is account-driven:

```85:128:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(...)
    {
        // ...
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        foreach (var account in accounts)
        {
            // left-join scores; missing score → INSUFFICIENT_DATA
        }
        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
    }
```

**No** `Take(200)` on the trader census. Optional filters are broker/state only.

The only dashboard `Take` is risk rejects `.Take(20)` (`EfDashboardQueries.cs:204`).

---

## 6. Residual `Take(200)` — reconstructed-trade explorer only

The only product C# `Take(200)`:

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

This is a **read page** of reconstructed trade **rows**, newest 200 by `OpenedAt`. It does not enumerate Manager logins, does not snapshot positions, and cannot send FIX. `broker` query-string is unused. Do not claim “zero `Take(200)` in the tree.”

`POST /api/ops/resync` walks **both** broker codes, `SyncCatalogAsync` + `SyncBrokerAsync`, then scores **every** `ListLoginsAsync` login (not a 200 window). That is heavier than the hosted deals-only score path; still not a position cap.

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
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                        // ...
                    }
                    _log.LogInformation("{Broker} scored {Scored} logins that have deals", connector.BrokerCode, scored);
```

W500_RESEARCH_15 said this host scores all `ListLoginsAsync`. **That is stale.** Catalog still persists **all** manager traders; scoring is the deals-only subset. That is **not** a `Take(200)` positions cap and does **not** drop groups/accounts from the census. Unscored catalog rows still appear on `/api/traders` as `INSUFFICIENT_DATA`.

`apps/mt5-worker/Worker.cs` (separate host, if started) still scores only `{10001,10002,10003,99001}` after calling `SyncBrokerAsync` for both codes. That is a **demo-scoring leftover**, not a Manager fetch cap. Live API DI does not use Fake; worker scoring of those four logins is a no-op / miss on a live book.

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

`GetPositions` walks `positions->Total()` with no slice (L418–422). `GetAllGroups` walks `GroupTotal()` + `GroupNext` (L962–982). No `resize(200)`, no first-N. Feature-flag `getMaxAccountsPerUser()` default **5** (`feature_flags.h` L230) is a **per-user product** limit in YoPips, not a Manager census cap and not used by Prop ingest.

Prop ingest does **not** call this C++ process. It is a sibling Manager client. Absence of a 200 window there is corroboration, not the live path.

---

## 9. Prior live census (not re-run this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc `2026-08-18T08:42:16.8519545+00:00`  
Note on disk: `"Passwords never written. Groups and manager logins only."`

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK (HTTP proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | OK (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever group breakdown (sums to 6512): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave group breakdown (sums to 1948): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `Starwave\demo\FX2\grp1` 170, `grp2` **1735**, `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `Starwave\real\FX3\LP` 2.

These counts are **> 200** on both brokers. A still-live `accounts.Take(200)` on position refresh **could not** have produced 1506 / 478 open-position rows from the same connector in one pass. Dashboard `/api/traders` = 8460 and `/api/groups` = 18 were recorded in `CREDENTIALS_AND_COPY_STATUS.md` / `LIVE_MANAGER_FETCH_MEASURED.md`.

These are **all groups / logins those two manager records can see**. Groups outside the manager ACL are invisible by design, not by a 200 cap.

---

## 10. Copy to cTrader — no live orders (no loss)

| Gate | Measured state |
|---|---|
| `DependencyInjection` L38–41 | `RealCopyEnabled = false` with comment: live NewOrderSingle is **not implemented** |
| `CTraderFixLogonHostedService` L68 | `_runtime.RealCopyEnabled = false` **after** QUOTE/TRADE logon |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** |
| `CTraderFixSession.BuildLogon` L96 | only `(35, "A")` |
| `CTraderFixSession` L49 | only `WriteAsync` is the Logon; sockets disposed after read |
| `CTraderFixSession` | no `(35, "D")`, no NewOrderSingle builder, no ClOrdID send |
| `CTraderQuoteService` | SecurityList + MD snapshot only; 0 `WriteAsync` |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled=true`, logs a warning and **still refuses** send; stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` |
| `/api/settings` | `REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced false); `FEATURE_COPY_TRADING_ENABLED` hardcoded **false** |
| `/api/health` | exposes `realCopyEnabled` from the same runtime flag |
| `LiveRuntimeStatus.Snapshot` | when false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |

`CTraderFixSession` can open TLS to 5211/5212 and exchange Logon (`35=A`). LoggedOn ≠ armed copy. There is **no** method that assembles tag `35=D`. That is `SAFE_BY_ABSENCE`, not a proven go-live flag.

---

## 11. Goal check (this slot)

| Goal item | Status |
|---|---|
| Fetch **ALL** Achiever + Starwave groups | **Code path = all visible.** `GetGroupsAsync` → `GroupRequestArray("*")` / `GroupTotal`. Prior measure: **8 + 10 = 18**. |
| Fetch **ALL** manager traders | **Code path = all visible.** `GetAccountsAsync(null)` walks every group. Prior measure: **6512 + 1948 = 8460**. |
| Positions not silently first-200 | **PASS.** Bulk `"*"` or `foreach` all accounts. |
| Copy to cTrader must not send live orders | **PASS.** `35=D` absent; `RealCopyEnabled=false`. |
| No secrets printed | **PASS.** |

---

## 12. Residual debt (honest, not a fail of this angle)

1. `GET /api/trades` `Take(200)` hides older reconstructed rows from the explorer.
2. Hosted scoring is deals-only (`ListLoginsWithDealsAsync`). Catalog still has all logins.
3. `mt5-worker` still scores four demo logins if that process is started.
4. `GetGroupPositionsCore` empty-on-error + `ReplaceBrokerPositionsAsync` can wipe the broker book.
5. This slot did not re-run `LiveBrokerProbe`; census is the 08:42Z artifact.
6. Older swarm notes (A005, A007, W500_SLICE_7/17) still cite ingest `Take(200)` — treat as historical.

---

## 13. Do not claim

- Do not claim “EX5 decompiled” or “≥95% copy live.”
- Do not claim zero `Take(200)` in the whole repo (HTTP trades page remains).
- Do not claim this process can lose money on cTrader today — there is no send path.
- Do not re-introduce `accounts.Take(200)` on positions. At 8460 logins that is a silent open-risk hole.

---

## 14. Files read (absolute)

- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
