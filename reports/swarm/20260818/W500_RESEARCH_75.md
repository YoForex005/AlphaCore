# W500_RESEARCH_75 — `DealIngestionService` `Take(200)` positions cap

| Field | Value |
|---|---|
| Slot | **75** |
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

Re-read in full or in the cited ranges (slot 75 independent pass):

| Path | Lines / range checked |
|---|---|
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | **146/146** — `ITradingStore` + `SyncCatalogAsync` + `SyncBrokerAsync` + `ReconstructionScoringService` sibling |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | **79/79** — bulk + per-login ports (no N) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | **458/458** — `GroupRequestArray("*")`, `UserRequestArray`, `PositionRequestByGroup` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **151/151** — no bulk iface; per-login lists unbounded |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ReplacePositionsAsync` L116–142; `ListLogins*` L339–345; `ReplaceBrokerPositionsAsync` L475–501 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | **141/141** — scores `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | **59/59** — `RealCopyEnabled = false`; native connectors only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | **94/94** — ACHIEVER + STARWAVEFX factory; Starwave `ProxyEnabled = false` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | **217/217** — traders = **all** `Mt5Accounts`; `.Take(20)` on risk rejects only |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | **66/66** — copy note when flag false |
| `D:\Prop\apps\api\Program.cs` | **156/156** — leftover `Take(200)` L107; `/api/ops/resync` all `ListLoginsAsync` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | **45/45** — `SyncBrokerAsync` both brokers; scores **4 demo logins** if that host is used |
| `D:\Prop\apps\fix-worker\Worker.cs` | **51/51** — refuses `NewOrderSingle` even if config true |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135/135** — only `(35, "A")` Logon |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | **80/80** — `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | **113/113** — forces `_runtime.RealCopyEnabled = false` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `GetUserLogins` L315–328; `GetPositions` L396–426; `GetAllGroups` L962–982 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | probe `2026-08-18T08:42:16.8519545+00:00` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | same census, no secrets |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | names-only secrets; copy `35=D` OFF |

Workspace greps (this slot, product C#):

| Needle | Hits |
|---|---|
| `Take(200)` / `accounts.Take` under `D:\Prop\src` `*.cs` | **0** |
| `Take(` / `Skip(` / `MaxAccounts` / `first 200` / `i < 200` under `D:\Prop\src\Application` | **0** |
| `Take(` under `D:\Prop\src` `*.cs` | **2** — `EfDashboardQueries.cs:204` `.Take(20)` (risk rejects); `FixMessageParser.cs:45` checksum `parts.Take` |
| `Take(200)` under `D:\Prop\apps` `*.cs` | **1** — `D:\Prop\apps\api\Program.cs:107` |
| Other `Take(200)` under `D:\Prop` `*.cs` | vendor packet slice only: `mt5-sdk\vendor\...\MTAsyncConnect.cs:692` (`data.Skip(dataPos).Take(packetBodySize)`) — **not** an account window |
| `GetGroupPositionsAsync` under `D:\Prop\src` | ingest L84 + native L57–58 + contract L78 |
| `(35, "D")` / `35=D` / `MsgType="D"` in `D:\Prop\src` | **0** |
| Live session MsgType builders | `CTraderFixSession.cs:96` `(35, "A")` only. Quote service: `(35, "y")` / `(35, "V")`. Harness: A / 3 / 0 / y / X / 8. **No D.** |
| C++ `Take(200)` under YoPips `src` | **0** |
| C++ first-200 window on `GetPositions` / `GetUserLogins` / `GetAllGroups` | **0** (`feature_flags.h` `max_accounts_per_user` default **5** is a **prop-firm user quota**, not a Manager census cap) |

This slot did **not** execute a live Manager connect, did **not** compile, and did **not** open a FIX TRADE socket.

---

## 2. Historical cap (stale) vs current source

A005 / A007 quoted this loop as the silent first-200 position snapshot:

```csharp
foreach (var account in accounts.Take(200))
```

That literal is **absent** from current `DealIngestionService.cs`. Grep of `D:\Prop\src\Application` for `Take(200)|accounts.Take|Skip(|MaxAccounts|i < 200|first 200` = **0**.

Current position snapshot (`DealIngestionService.SyncBrokerAsync`, L82–94):

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

There is **no** `Take`, **no** `Skip`, **no** first-N. Native live connector implements `IMt5BulkPositionReader`, so production takes the bulk `"*"` branch. Fake (leftover `DemoBrokerFactory` / tests only) does **not** implement the bulk iface, so it takes `foreach (var account in accounts)` — still **no** `Take(200)`.

Live DI **refuses** to start without real Manager passwords (`DependencyInjection.cs` L35–36) and registers only `LiveMt5Registration.CreateConnectors` (Achiever + Starwave native). Fake is not the API path.

---

## 3. Catalog = ALL groups + ALL manager traders (code)

### 3.1 Ingest catalog

`SyncCatalogAsync` (L38–51):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`SyncBrokerAsync` calls `SyncCatalogAsync` first, then **re-fetches** `GetGroupsAsync` + `GetAccountsAsync(null)` for deals/positions. `GetAccountsAsync(null)` is the documented “all groups” argument (no plan-map filter, no `MT5_GROUP_*` allow-list).

`BrokerSyncResult` returns `groups.Count` / `accounts.Count` with no truncation.

### 3.2 Native Manager walk

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L144–186):

1. `GroupRequestArray("*", arr)` — every group this manager ACL can see.
2. Fallback if empty: `GroupTotal()` + `GroupNext(i)` for `i in [0, total)`.
3. Dedupes by name. **No `Take(200)`.**

`GetAccountsCore(null)` (L189–213):

1. Walks **every** name from `GetGroupsCore()`.
2. Per group: `UserRequestArray` → fallback `UserGetByGroup` → fallback `UserLogins` + `UserRequestByLogins`.
3. Dedupes by login. **No `Take(200)`.**

`GetGroupPositionsCore("*")` (L336–352):

1. `PositionRequestByGroup(mask)`.
2. Fallback `PositionGetByGroup(mask)`.
3. Else **empty list** (not a 200-window — a **full-book miss** if both APIs fail).
4. `ReadPositions` walks `arr.Total()` with no N.

`GetPositionsCore(login)` (L319–333): `PositionRequest` for that login; entire array. Per-login fallback is unbounded.

### 3.3 Dashboard list

`EfDashboardQueries.GetTradersAsync` (L85–128) is **account-driven**:

```99:128:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        foreach (var account in accounts)
        {
            // ... left-join score + pnl ...
        }
        // optional broker/state filter, then OrderByDescending(EarlyScore).ToList()
```

**No `Skip`/`Take` on traders.** Unscored logins still render as `INSUFFICIENT_DATA`. `/api/groups` walks every `Mt5Groups` row (L71–82). `/api/traders` is the same universe.

A005 “scores-only dashboard” is **stale**.

---

## 4. Live census (prior measure; not re-probed this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe` `utc=2026-08-18T08:42:16.8519545+00:00` `envLoaded=true`  
Note on file: “Passwords never written. Groups and manager logins only.”

| Broker | Connect | Groups | Accounts | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | `true` (7212.6 ms) | **8** | **6512** | **1506** | HTTP whitelist proxy |
| STARWAVEFX | `true` (6413.5 ms) | **10** | **1948** | **478** | **direct** (`ProxyEnabled=false`) |
| **Total** | | **18** | **8460** | **1984** | |

Achiever group account sum (re-added this slot): `2+179+4+5+4+6295+0+23 = 6512`.

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Starwave group account sum (re-added this slot): `11+4+170+1735+22+0+0+4+0+2 = 1948`.

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

These counts are **> 200** on **both** brokers (Achiever `demo\yo-2step` alone is **6295**; Starwave `demo\FX2\grp2` is **1735**; open book **1506 / 478**). A still-live `Take(200)` **could not** have produced this probe JSON.

`CREDENTIALS_AND_COPY_STATUS.md` independently recorded dashboard `/api/traders` = **8460** and `/api/groups` = **18**. This slot did not re-hit HTTP.

These are **all groups / logins this manager ACL can see**. Groups outside the two manager permissions are not claimed.

---

## 5. Scoring vs catalog (not a positions cap)

| Caller | Login set | Cap? |
|---|---|---|
| `LiveIngestHostedService` L106 | `ListLoginsWithDealsAsync` | deals-only subset |
| `POST /api/ops/resync` L131 | `ListLoginsAsync` | **all** persisted accounts |
| `apps/mt5-worker/Worker.cs` L31 | hardcoded `{10001,10002,10003,99001}` | **demo leftover** if that host runs |
| Catalog persist | `GetAccountsAsync(null)` | **all** |
| Positions persist | `GetGroupPositionsAsync("*")` or all accounts | **all** (or empty on bulk miss) |

Store implementations (`EfTradingStore.cs`):

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Goal text is **fetch** all groups/traders, not score every empty login. Catalog + positions still walk the full Manager set. Scoring a deals-only subset is **not** a `Take(200)` positions cap; it is residual scoring debt. W500_15 “hosted scores all logins” is **stale**.

The leftover `Take(200)`:

```101:108:D:\Prop\apps\api\Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
```

This cannot hide live MT5 exposure. It can hide older reconstructed rows from the explorer. Query-string `broker` is unused. `login` still applies `Take(200)` after `OpenedAt` desc.

`GetRiskAsync` `.Take(20)` is reject-reason preview only.

---

## 6. Copy to cTrader cannot send live orders (no loss)

| Check | Measured |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` default | **`false`** (L35) |
| DI `LiveRuntimeStatus.RealCopyEnabled` | **forced `false`** (`DependencyInjection.cs` L38–41) |
| `CTraderFixLogonHostedService` | **re-forces `_runtime.RealCopyEnabled = false`** (L68) after optional `35=A` logon |
| Live session outbound MsgType | **`(35, "A")` only** (`CTraderFixSession.BuildLogon` L96). One `WriteAsync`. `using` disposes TCP/SSL. |
| `35=D` / `(35, "D")` / `NewOrderSingle` builder in product `*.cs` | **0** (name appears in comments / log strings / FSM helper only) |
| `apps/fix-worker` | stamps TRADE `Disconnected`; even if `CTrader:RealCopyExecutionEnabled=true` it **logs a refuse** and still has **no sender** |
| `/api/settings` | reports `runtime.RealCopyEnabled` (false unless someone mutates the singleton) |
| `/api/reconciliation/status` | note: “NewOrderSingle still off” |
| Persist-before-send / `GuardedNewOrderSingle` | **MISSING** |
| Architecture §68 / §70 | **not PASS** (do not treat absence as a go-live tick) |

`CTraderFixSession.TryLogonAsync` may emit **Logon `35=A`** on QUOTE TLS 5211 and TRADE TLS 5212 when `CTRADER_FIX_PASSWORD` is present. That is **not** a NewOrderSingle. TargetCompID default is `cServer`. Password values are **not** printed here.

`ExecutionOrderStateMachine.MayRetryNewOrderSingle` is status math only — no socket.

**YoPips C++ is a different product.** `mt5_manager.cpp` `DealerBalance` / YoPips `SendTrade` can mutate **MT5 prop-firm accounts**. That path is **not** called by `DealIngestionService`, is **not** cTrader FIX, and is **out of this slot’s copy-to-cTrader wire**. Do not confuse C++ dealer send with Prop copy.

**Operating mode that satisfies “fetch all + no loss”:** Manager catalog/deals/positions + FIX logon/recon only. **Do not** enable `RealCopyExecutionEnabled`. **Do not** add `35=D` from this slot.

`SAFE_BY_ABSENCE` ≠ a tested refuse-on-LoggedOn-TRADE gate. It **does** mean this process cannot lose cTrader capital today.

---

## 7. C++ backend (YoPips) — no 200-account Manager window

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

| Method | Lines | Cap? |
|---|---|---|
| `GetUserLogins` | 315–328 | `logins.assign(raw_logins, raw_logins + total)` — **all** |
| `GetPositions` | 396–426 | `for (i = 0; i < positions->Total(); i++)` — **all** for that login |
| `GetAllGroups` | 962–982 | `for (i = 0; i < GroupTotal(); i++) GroupNext` — **all** (cache/pump) |

Grep `Take(200)` in that `src` tree: **0**.  
`getMaxAccountsPerUser()` default **5** (`feature_flags.h`) is a **per-user challenge quota**, not “first 200 Manager positions.”

C++ `GetAllGroups` is **cache-only** (`GroupTotal`/`GroupNext`). Prop C# live path prefers `GroupRequestArray("*")` then falls back to the same cache walk. Different code, same “no 200” fact.

---

## 8. Residual risks that are **not** `Take(200)` (do not greenwash)

1. **`GET /api/trades` `Take(200)`** — reconstructed explorer page.
2. **Hosted scoring = deals-only** — empty logins stay `INSUFFICIENT_DATA` until `/api/ops/resync` or a future scorer.
3. **`apps/mt5-worker` scores 4 demo logins** — leftover; live API uses `LiveIngestHostedService` instead.
4. **Bulk position miss** — if `PositionRequestByGroup("*")` **and** `PositionGetByGroup("*")` fail, native returns **empty** and `ReplaceBrokerPositionsAsync` **wipes** `mt5_positions` for that broker. Completeness of the open book depends on that API succeeding (probe already measured **1984**). This is **not** a 200-cap; it is a fail-empty replace.
5. **Manager ACL** — only groups the two manager logins can see (18 measured). Not “every group on the broker server.”
6. **In-memory DB** — `DATABASE_URL` placeholder → restart re-fetches. Not a census cap.
7. **Re-introducing `accounts.Take(200)`** after send exists would become a **capital-loss path** (copy would see a truncated book). Keep it gone.

| Scenario | If `Take(200)` were still present | Current source |
|---|---|---|
| Achiever 6512 accounts | 6312 never get position replace | bulk `"*"` or all accounts |
| Starwave 1948 accounts | 1748 never get position replace | same |
| Probe 1984 open positions | impossible from 200 accounts unless those 200 held every ticket | probe JSON exists with 1984 |

---

## 9. Do / do-not (slot 75)

**Do**

- Keep `GetAccountsAsync(null)` + `GroupRequestArray("*")` + `PositionRequestByGroup("*")`.
- Keep upserting **all** manager traders (no account `Take`).
- Keep `RealCopyEnabled = false` and no `35=D` builder.
- Treat A005/A007/`accounts.Take(200)` reports as **historical**.

**Do not**

- Re-introduce `accounts.Take(200)`.
- Enable `RealCopyExecutionEnabled` / `REAL_COPY_EXECUTION_ENABLED`.
- Add `35=D` / `F` / `G` from this slot.
- Claim hosted scoring already covers 8460 logins.
- Claim “zero `Take(200)` in the tree” — `/api/trades` L107 remains.
- Print or copy Manager / proxy / FIX passwords.

---

## 10. Answers to the assigned questions

1. **Is `DealIngestionService` still capped at `Take(200)` positions?**  
   **No.** File is 146 lines; class `SyncBrokerAsync` has **zero** `Take`/`Skip`. Live path is `GetGroupPositionsAsync("*")` + `ReplaceBrokerPositionsAsync`. Fallback is `foreach` **all** accounts.

2. **Can we fetch ALL Achiever + Starwave groups and ALL manager traders?**  
   **Yes, in code, and previously measured.** 8+10 groups, 6512+1948 traders, 1506+478 open positions (`2026-08-18T08:42:16Z`). This slot did not re-attach.

3. **Can copy to cTrader send live orders (loss)?**  
   **No.** `SAFE_BY_ABSENCE`: no `35=D` builder; flag forced false; worker refuse is a log line on a missing sender. Logon `35=A` may occur. No-loss live copy is also **not** built (gates not PASS) — absence is the safety.

---

## 11. Artifact

| Item | Path |
|---|---|
| This report | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_75.md` |
| Prior same-angle | `W500_RESEARCH_15.md`, `W500_RESEARCH_35.md`, `W500_RESEARCH_55.md` |
| Live census JSON | `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` |
| Live census write-up | `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` |

**Slot 75 verdict: `PASS_CAP_REMOVED`. Risk to capital: `NONE`.**
