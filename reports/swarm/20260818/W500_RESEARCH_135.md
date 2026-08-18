# W500_RESEARCH_135 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **135** |
| Date | 2026-08-18 |
| Role | Senior engineer — independent re-read of current disk (no product edit) |
| Topic | Check `DealIngestionService` `Take(200)` positions cap |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secrets written / printed | **No** (passwords, proxy auth, FIX password never copied) |
| Siblings on same angle | `W500_RESEARCH_15.md`, `W500_RESEARCH_35.md`, `W500_RESEARCH_55.md`, `W500_RESEARCH_75.md`, `W500_RESEARCH_95.md`, `W500_RESEARCH_115.md`. This slot **re-reads** the tree. |

**Honesty rule:** quote files as they sit **now**. A005 / A007 / W500_SLICE_7 / W500_SLICE_17 that still print `foreach (var account in accounts.Take(200))` are **stale vs `D:\Prop\src`**. W500_RESEARCH_15’s claim that `LiveIngestHostedService` scores **all** `ListLoginsAsync` is **stale** — current host scores `ListLoginsWithDealsAsync`. W500_RESEARCH_108 / CREDENTIALS “flag forced false” is **stale vs DI + `.env` L73** (see §8). Live Manager census is the existing probe artifact (`LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`). This slot did **not** re-attach to Manager and did **not** send any FIX order.

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
| `REAL_COPY_EXECUTION_ENABLED` still hard-pinned false in process | **No** — DI binds env; lab `.env` L73 is `true`; FIX host does **not** overwrite | `RESIDUAL` (still no sender) |
| Capital at risk from this ingest / copy path | **None** | no-loss |

**One-line:** Ingest `Take(200)` is **absent**; live probe already returned **18 groups / 8460 traders / 1984 open positions** (a 200-account window cannot produce those numbers); cTrader TRADE may log on (`35=A`) but **cannot** send `35=D`.

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
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **151/151** — `IMt5BrokerConnector` only; no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLogins*` L339–345; `ReplaceBrokerPositionsAsync` L475–501 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | **141/141** — scores `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **62/62** — `RealCopyEnabled` from env string `"true"`; native connectors only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | **94/94** — ACHIEVER + STARWAVEFX factory; no account window |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | traders = **all** `Mt5Accounts`; `.Take(20)` on risk rejects only |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **257/257** — `NewOrderSingleImplemented=false`; `AllowFixSend=false` hard write |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | **41/41** — shadow intents only |
| `D:\Prop\apps\api\Program.cs` | leftover `Take(200)` L110; `/api/ops/resync` all `ListLoginsAsync`; settings flag = runtime |
| `D:\Prop\apps\mt5-worker\Worker.cs` | ingest both brokers unbounded; **scores 4 demo logins** if that host is used |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135/135** — only `(35, "A")` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `RealCopyExecutionEnabled = false` (unbound from env name) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logs `_runtime.RealCopyEnabled`; **does not pin false** |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | 0 `WriteAsync` / 0 outbound `35=` / 0 `NewOrder` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | copy note still says no ticket will be sent even if armed |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetUserLogins` L315–328; `GetPositions` L396–426; `GetAllGroups` L962–982 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\feature_flags.h` | `max_accounts_per_user` default **5** = prop-firm user quota, not Manager census |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | live census 2026-08-18T08:42:16Z — header + 18 group names re-summed |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | same census, no secrets |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | dashboard 18 / 8460; copy-status prose is **stale** on flag pin |

Workspace greps (product C# / C++), re-run this slot:

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(` / `Skip(` under `D:\Prop\src\Application` | **0** |
| `Take(200)` under `D:\Prop` `*.cs` (product, not reports) | **1** — `D:\Prop\apps\api\Program.cs:110` |
| `Take(` under `D:\Prop\src` `*.cs` | `EfDashboardQueries.cs:204` `.Take(20)` (risk rejects); `FixMessageParser.cs:45` checksum `parts.Take`; `CopyTradingService.cs:67` `ListIntentsAsync` `.Take(take)` with caller `200` |
| `GetGroupPositionsAsync` under `D:\Prop\src` | ingest L84 + native L57–58 + contract L78 |
| `(35, "D")` / `35=D` builder in `Fix.CTrader` Sessions | **0** (only `(35, "A")` at `CTraderFixSession.cs:96`; one `WriteAsync` at L49) |
| Product `src`/`apps` `*.cs` literal `35=D` | **0** |
| C++ `Take(200)` on `GetPositions` / `GetUserLogins` / `GetAllGroups` | **0** |

This slot did **not** execute a live Manager connect and did **not** compile.

---

## 2. `DealIngestionService` — current ingest (146 lines, 0 `Take`)

`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` is **146** lines. Grep of that file for `Take` / `Skip` / `200` = **0**.

Catalog (all groups + all accounts the connector returns):

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

`GetGroupsAsync` has **no** name / plan / allow-list argument. `GetAccountsAsync(null)` is the “all groups” contract (native walks every discovered name).

Positions (the historical `Take(200)` site):

```82:94:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
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
```

Deals are likewise unbounded: `foreach` **all** `groups` via `GetGroupDealsAsync`, else `foreach` **all** `accounts` via `GetDealsAsync`. No first-N.

Stale literal that **must not** be treated as current product:

```csharp
foreach (var account in accounts.Take(200))
```

That string is **absent** from current `DealIngestionService.cs`.

---

## 3. Native Manager walk — no N-window

`NativeMt5BrokerConnector` implements `IMt5BrokerConnector`, `IMt5BulkDealReader`, **and** `IMt5BulkPositionReader`. Live ingest therefore takes the `"*"` bulk position path.

| Step | Code | Cap? |
|---|---|---|
| Groups | `GroupRequestArray("*")` then fallback `GroupTotal`/`GroupNext` (`GetGroupsCore` L144–185) | **No** — `*` + pump-cache fallback |
| Accounts | `GetAccountsAsync(null)` unions every `GetGroupsCore()` name; per group `UserRequestArray` → `UserGetByGroup` → `UserLogins` (`GetAccountsCore` L189–214, `ReadAccountsForGroup` L216–271) | **No** |
| Positions | `GetGroupPositionsAsync` → `PositionRequestByGroup(mask)` then `PositionGetByGroup` (`GetGroupPositionsCore` L336–353); ingest passes `"*"` | **No** |
| Per-login fallback | `PositionRequest(login)` (`GetPositionsCore` L319–334) | **No** — used only if bulk iface missing |

```57:58:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    public Task<IReadOnlyList<Mt5PositionDto>> GetGroupPositionsAsync(string? groupMask, CancellationToken ct) =>
        Task.Run(() => GetGroupPositionsCore(string.IsNullOrWhiteSpace(groupMask) ? "*" : groupMask), ct);
```

```336:348:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

`FakeMt5BrokerConnector` is **not** a bulk reader. Ingest then uses `foreach (var account in accounts)` with no `Take`. Fake is **not** registered on the live API path: `AddTraderIntelligence` throws unless both Manager passwords pass `IsSecret`, then `LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances only.

`IMt5BulkPositionReader.GetGroupPositionsAsync` has **no** `max` / `limit` parameter (`Mt5Contracts.cs` L76–79).

---

## 4. Hosted scoring vs catalog (not a positions cap)

`LiveIngestHostedService`:

1. `SyncCatalogAsync` per connected broker (all groups + all accounts).
2. `SyncBrokerAsync` (deals + positions, unbounded as §2).
3. Score **`ListLoginsWithDealsAsync`** only.

```105:125:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsWithDealsAsync(brokerId, stoppingToken);
                    st.Phase = "scoring";
                    ...
                    foreach (var login in logins)
                    {
                        ...
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
```

Store:

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Catalog still persists **all** manager traders. Scoring is the deals-only subset. Unscored catalog rows still appear on `/api/traders` as `INSUFFICIENT_DATA` (`EfDashboardQueries.GetTradersAsync` L99–120 `foreach (var account in accounts)` — **no** `Take`). That is **not** a `Take(200)` positions cap.

`/api/ops/resync` scores **`ListLoginsAsync`** (all persisted logins). `apps\mt5-worker\Worker.cs` still scores only `{10001,10002,10003,99001}` after an unbounded `SyncBrokerAsync` — leftover demo scorer, not a Manager census cap.

---

## 5. Residual `Take(200)` — reconstructed-trade explorer only

The only product C# `Take(200)`:

```104:111:D:\Prop\apps\api\Program.cs
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

This is a **read page** of reconstructed trade **rows**, newest 200 by `OpenedAt`. It does not enumerate Manager logins, does not snapshot positions, and cannot send FIX. Query-string `broker` is unused.

Related display caps (not ingest):

| Site | Cap | Effect |
|---|---|---|
| `GET /api/trades` | `Take(200)` | explorer tape |
| `GET /api/copy/intents` | `ListIntentsAsync(200, ct)` | copy-intent page |
| `EfDashboardQueries.GetRiskAsync` | `.Take(20)` | reject-reason chips |
| `GET /api/groups` / `/api/traders` | **none** | full persisted catalog |

Do **not** claim “zero `Take(200)` in the tree.”

---

## 6. Live census (re-summed this slot; not re-probed)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`. Note on disk: “Passwords never written.”

Header fields (this slot re-read):

| Broker | `connected` | `groups` | `accounts` | `openPositions` |
|---|---|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 |
| STARWAVEFX | true | 10 | 1948 | 478 |
| **Sum** | | **18** | **8460** | **1984** |

Independent group-name count this slot: **18** `"name"` rows under `groupNames` (8 Achiever + 10 Starwave). Independent sum of per-group `accounts` fields:

| ACHIEVER | n | STARWAVEFX | n |
|---|---:|---|---:|
| contest\yo-1step | 2 | Starwave\cent\FX1\grp1 | 11 |
| contest\yo-2step | 179 | Starwave\cent\FX1\grp2 | 4 |
| contest\yo-instant | 4 | Starwave\demo\FX2\grp1 | 170 |
| contest\yo-payp | 5 | Starwave\demo\FX2\grp2 | 1735 |
| demo\yo-1step | 4 | Starwave\real\FX3\grp1 | 22 |
| demo\yo-2step | 6295 | Starwave\real\FX3\grp2 | 0 |
| demo\yo-instant | 0 | Starwave\real\FX3\grp3 | 0 |
| demo\yo-payp | 23 | Starwave\real\FX3\grp4 | 4 |
| | | Starwave\real\FX3\grp5 | 0 |
| | | Starwave\real\FX3\LP | 2 |
| **sum** | **6512** | **sum** | **1948** |

`2+179+4+5+4+6295+0+23 = 6512`. `11+4+170+1735+22+0+0+4+0+2 = 1948`. Header ↔ group sum **match**.

These counts are **> 200** on both brokers. A still-live `accounts.Take(200)` on position refresh **could not** have produced 1506 / 478 open-position rows from the same connector in one pass. `demo\yo-2step` alone is **6295** logins.

Dashboard `/api/traders` = 8460 and `/api/groups` = 18 were recorded in `CREDENTIALS_AND_COPY_STATUS.md` / `LIVE_MANAGER_FETCH_MEASURED.md`. This slot did not re-hit HTTP.

These are **all groups each manager login can see**. Groups outside that ACL are not a `Take(200)` defect.

---

## 7. Copy to cTrader — no live orders (no loss)

Safety is **by absence of a sender**, not by the ingest cap.

| Gate | Measured now |
|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | **`false` const** L16 |
| `CopyTradingService.VenueReconciled` | **`false` const** L15 |
| `RiskDecisionRecord.AllowFixSend` | **hardcoded `false`** L192 (ignores `decision.AllowFixSend`) |
| Live branch | even if all four AND bits were true, status is `LIVE_SEND_BLOCKED_UNIMPLEMENTED` L198–201 |
| Default / else | `SHADOW_ONLY` + optional `ShadowCopyEngine.SimulateEntry` |
| Hosted copy loop | `GenerateShadowIntentsAsync` only; log “Live NewOrderSingle still blocked.” |
| FIX session | `CTraderFixSession` builds **only** `(35, "A")` Logon; single `WriteAsync`; `using` sockets disposed |
| Quote service | **0** socket writes |
| Product `35=D` / `(35, "D")` | **0** in `src` + `apps` `*.cs` |
| `apps\fix-worker\Worker.cs` | stamps TRADE `Disconnected`; “NewOrderSingle remains off”; even if nested config true, **refuses** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default **false**; env name is **not** bound onto this POCO |

```198:205:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
            (554, password)
        };
        return Assemble(fields);
```

YoPips C++ `src` has **no** cTrader FIX `35=D` sender. `DealerSend` there is **prop-firm Manager dealer** (unrelated to this copy path). `GetAllGroups` / `GetUserLogins` / `GetPositions` have **no** first-200 window.

---

## 8. Residual that older same-angle slots under-stated

`DependencyInjection.AddTraderIntelligence` **now binds** the env flag:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `D:\Prop\.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret printed).

`CTraderFixLogonHostedService` **does not** assign `_runtime.RealCopyEnabled = false`. It only logs the current value (L68–70).

`GET /api/settings` exposes `runtime.RealCopyEnabled` (Program.cs L76), so a running API that loaded this `.env` will **report** the flag true. That is **not** a send license:

- no `35=D` builder exists
- `NewOrderSingleImplemented` is a compile-time false
- `AllowFixSend` is written false
- `VenueReconciled` is false
- `LiveRuntimeStatus.Snapshot` still says even if armed: “NewOrderSingle still unimplemented; … No ticket will be sent.”

W500_RESEARCH_108 / A014 / CREDENTIALS “forced false” are **stale** on the pin. W500_RESEARCH_115’s table row “hosted forces false” is **stale**. Slot 135 agrees with slot 116/123: flag is **bound**, sender is still **absent**.

---

## 9. What would be unsafe later

If a `35=D` sender is added while `accounts.Take(200)` is re-introduced:

- 8460 − 200 = **8260** logins would keep stale / empty `mt5_positions`
- open-risk / copy sizing would be silently wrong
- that is a **capital-loss hole** once send exists

Do **not** re-introduce `accounts.Take(200)` on positions.

---

## 10. Debt / residuals (honest, not blockers for this slot)

1. `GET /api/trades` `Take(200)` hides older reconstructed rows from the explorer.
2. Hosted scoring is deals-only (`ListLoginsWithDealsAsync`); catalog rows without deals stay `INSUFFICIENT_DATA`.
3. `mt5-worker` still scores four demo logins after a full ingest.
4. `/api/ops/resync` scores **all** `ListLoginsAsync` (heavier than the hosted path).
5. `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now visible on runtime/settings; still cannot emit `35=D`.
6. Older swarm notes (A005, A007, W500_SLICE_7/17) still cite ingest `Take(200)` — treat as historical.

---

## 11. Do / do-not

- Do treat ingest `Take(200)` as **removed**.
- Do treat live catalog completeness as **18 / 8460 / 1984** from the 08:42Z probe (not re-attached this slot).
- Do **not** claim zero `Take(200)` in the whole repo (HTTP trades page remains).
- Do **not** claim hosted scoring walks every login (deals-only).
- Do **not** re-introduce `accounts.Take(200)` on positions.
- Do **not** treat env `REAL_COPY=true` or FIX `35=A` logon as a send license.
- Do **not** send live `35=D` until a real sender + recon + risk hop + explicit go-live exist.

---

## 12. Sources (absolute)

- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\feature_flags.h`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`

---

**End of W500_RESEARCH_135.** Verdict `PASS_CAP_REMOVED`. Risk to capital `NONE`.
