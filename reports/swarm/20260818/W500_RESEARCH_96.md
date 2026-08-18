# W500_RESEARCH_96 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_96.md` |
| Slot | **96** |
| Date | 2026-08-18 |
| Agent | W500 research 96 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only required product-adjacent write (plus `SWARM_LOG.md` / `INDEX.md` catalog lines). |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values, proxy auth, FIX password, and individual manager logins from the census JSON were not copied. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (**217** physical lines). Cross-read ingest, store login lists, native Manager connector, live ingest host, API maps, FIX logon session, DI, runtime flag, Traders/Groups/Overview/LiveCopy pages, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `foreach (var account`, `35=D`, `(35, "D")`, `GuardedNewOrderSingle`, `DealerSend`, `Take(`, `ListLogins*`, `Evaluate(`. Compared against stale `A005_dashboard_traders.md` and siblings 16/36/56. No live HTTP / no re-probe this slot. No product edit. |

**Honesty:** A005 (`foreach (var s in scores)`; unscored logins invisible) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-ingest **scores** only logins that already have deals; the **list** still emits every persisted `Mt5Account`. That is a scoring-coverage split, not a hide of the trader book. Slot 16’s “hosted service scores every stored login” is also **stale** vs current `ListLoginsWithDealsAsync`.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No (stale).** Driver is `foreach (var account in accounts)` | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores + PnL | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GroupRequestArray("*")` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Hosted scoring covers every cataloged login | **No.** `ListLoginsWithDealsAsync` only | `EXISTS_NEEDS_REFACTOR` (score freshness, not list hide) |
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Logon `35=A` only; `RealCopyEnabled` forced `false` | `SAFE_BY_ABSENCE` |
| Risk to live capital from this path | **None** from this process | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login (18 / 8460 last measure).
Hosted score = logins-with-deals only; list still shows the rest as INSUFFICIENT_DATA.
FIX copy = logon/recon only; no 35=D; RealCopyEnabled=false.
```

**Slot-96 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

---

## 1. Assigned question — scores-only vs all `Mt5Accounts`

Current method (`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L85–129), re-read in full this slot (file ends at L217):

1. Load **all** `TraderScores` (join payload, `AsNoTracking`) — L87.
2. Load **all** `Brokers` (dictionary by `Id`) — L88.
3. Load **all** `Mt5Accounts` — **this is the `foreach` driver** — L89, L99.
4. Load completed `ReconstructedTrades` PnL grouped by `(BrokerId, Login)` — L90–95.
5. Build `scoreMap` / `pnlMap` dictionaries — L96–97.
6. For **each account**: skip only if `BrokerId` is missing from `Brokers` (`continue`); `scoreMap.TryGetValue` / `pnlMap.TryGetValue` (left join); **always** `mapped.Add(...)` — L99–120.
7. Optional in-memory `broker` / `state` filters (`OrdinalIgnoreCase` / `Enum.TryParse(..., true)`) — L122–126.
8. `OrderByDescending(t => t.EarlyScore)`. **No `Skip`/`Take` on the trader set** — L128.

Workspace grep of `foreach (var (s|score|account)` in this file = **one** hit: `foreach (var account in accounts)` at L99. There is **no** `foreach (var s in scores)`.

The only `Take` in this file is `Take(20)` on risk-reject **reasons** (L204), not traders.

Missing score does **not** drop the row:

| Field | If `TraderScore` missing |
|---|---|
| `CompletedXauTrades` | `0` |
| `NetSourcePnl` | reconstructed PnL or `0` |
| `EarlyScore` / `RiskScore` | `0` |
| `MlProbability` | literal `null` (ML not built) |
| Flags (`Martingale` / `AveragingDown` / `LotEscalation`) | `false` |
| `State` | `TraderState.INSUFFICIENT_DATA` |
| `ShadowPnl` | literal `0` (even when a score exists) |
| `LastScored` | `account.LastSyncedAt` |

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. A005’s “detail of unscored login returns null” is **false** on current disk.

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L95–96). `TradersPage` title is **“All manager traders ({data.length})”** and calls `useTraders({})` with **no** default `state` filter, so `INSUFFICIENT_DATA` rows stay visible.

Axios timeout is **60 000 ms** (`D:\Prop\apps\web\src\api\client.ts` L5). A005 “15s” is **stale**.

Product tests: `grep` of `D:\Prop\tests` for `EfDashboardQueries` / `GetTradersAsync` = **0**. Completeness is source-proven, not unit-proven.

### 1.1 What is still scores-only (not the trader list)

`GetOverviewAsync` L22–42:

| Card | Source |
|---|---|
| `TotalAccounts` | `Mt5Accounts.CountAsync` — **all accounts** |
| `XauTraders` / `TradersWithThreeTrades` | `TraderScores` counts |
| `Watch` / `Shadow` / `LiveCandidates` / `Live` / `RiskBlocked` | `TraderScores.CurrentState` only |

Until scoring upserts a row, state tiles **under-count**. The `/traders` table still lists the cataloged login. That is the residual A005 intuition that remains true **only** for overview buckets. Overview page copy states live FIX NewOrderSingle is off (`OverviewPage.tsx` L15).

Orphan `TraderScores` (score without an `Mt5Account`) are **invisible** on the leaderboard. That is correct for “all manager traders”: the census is the account book.

Accounts whose `BrokerId` is not in `Brokers` are skipped (`continue`). `BrokerCatalogSeed.EnsureAsync` writes `ACHIEVER` + `STARWAVEFX` (`D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`). Live ingest resolves those same codes (`BrokerCodes.Achiever` / `BrokerCodes.StarwaveFx`). Not a hide of Manager users.

### 1.2 Scoring universe ≠ list universe (slot-96 reconfirm)

| Path | Login set | Effect on `/api/traders` |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L38–51 | `GetGroupsAsync` + `GetAccountsAsync(null)` — **all** | Writes `Mt5Accounts` → listed |
| `LiveIngestHostedService` scoring L106–125 | `ListLoginsWithDealsAsync` (`Mt5Deals` distinct logins) | Only those get a `TraderScore`; others stay `INSUFFICIENT_DATA` **but still listed** |
| `POST /api/ops/resync` L121–143 | `ListLoginsAsync` — **all** `Mt5Accounts` | Scores every stored login |
| `apps/mt5-worker/Worker.cs` L31–35 | leftover `{10001,10002,10003,99001}` | Demo host only; **not** the API live graph |

`EfTradingStore` (L339–345):

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

W500_RESEARCH_16 / A005 said the hosted service scores **every** `ListLoginsAsync` login. Current file is `ListLoginsWithDealsAsync` (L106). That is a **score-freshness** split, not a dashboard hide.

### 1.3 Stale reports to ignore on this question

| File | Stale claim | Current disk (slot 96) |
|---|---|---|
| `A005_dashboard_traders.md` §0 / §2.2 | Driver `foreach (var s in scores)`; unscored invisible | `foreach (var account in accounts)` + `scoreMap.TryGetValue` |
| A005 `Take(200)` on positions | `accounts.Take(200)` in ingest | **0** `Take(` in `DealIngestionService` (146 lines). Positions via `GetGroupPositionsAsync("*")` |
| A005 health FakeMt5 | hardcoded FakeMt5 string | `/api/health` reports live Manager `groups=` / `accounts=` / `phase=` (`Program.cs` L32–56) |
| A005 / W500_16 axios 15s | `timeout: 15000` | `timeout: 60000` |
| W500_16 “live ingest scores every stored login” | `ListLoginsAsync` | Hosted path is `ListLoginsWithDealsAsync` |
| `C36` / `D21` query body | 168 lines / scores-as-driver era | **217** lines; account driver |
| C42 “live MT5 not proven” | Fake only | Native ×2 on API graph; census 18/8460 (08:42Z) |

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first (request API, not pump cache) — L155.
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981: cache/pump `GroupTotal` + `GroupNext`; **no name filter**).

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup` (L216–262): `UserRequestArray` first → fallback `UserGetByGroup` only on hard fail → if still empty `UserLogins` + `UserRequestByLogins`.
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

`GetGroupPositionsAsync` uses mask `"*"` when blank (L57–58). Ingest uses that bulk path (`DealIngestionService` L82–86).

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter.

`UpsertGroupsBatchAsync` / `UpsertAccountsBatchAsync` persist **every** incoming row (accounts flush every 500 for memory, not a cap). New groups get `EnabledForAnalysis = true` (store L376). Dashboard `GetGroupsAsync` (L70–82) is `Mt5Groups.ToListAsync` — displays those flags, does **not** filter on them.

`LiveMt5Registration.CreateConnectors` registers **exactly two** native connectors: `BrokerCodes.Achiever` (`ACHIEVER`) and `BrokerCodes.StarwaveFx` (`STARWAVEFX`). Starwave `ProxyEnabled = false` hard pin (L45). DI **throws** if either real Manager password is absent (`DependencyInjection.cs` L35–36). `FakeMt5BrokerConnector` exists as a type but is **not** registered on the API graph (`AddSingleton<IMt5BrokerConnector>` only wraps `CreateConnectors`). Dummy seed is **not** on API startup (`Program.cs` only `EnsureCreated` + `BrokerCatalogSeed`).

`GroupsPage` copy: “Every group visible to the Achiever and Starwave managers.”

`src` `Take(` grep this slot: **2** hits — `EfDashboardQueries` L204 (`Take(20)` reject reasons) and `FixMessageParser` checksum slice. **Zero** account/position caps in product ingest.

### 2.2 Measured live census (prior probe; passwords and logins not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`, probe `LiveBrokerProbe`). `CREDENTIALS_AND_COPY_STATUS.md` independently recorded live dashboard `/api/traders` = **8460** and `/api/groups` = **18**.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (account counts only): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. Sum **6512**.

Starwave groups (account counts only): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2. Sum **1948**.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good). This slot did **not** re-run the live probe.

That 8460 HTTP row count is the operational proof that `GetTradersAsync` is **not** a scores-only leaderboard: a scores-only list would be `<=` the subset of logins that have `TraderScore` rows (auto-score = deals-only).

### 2.3 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios **60 s** timeout. That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

`/api/trades` still `Take(200)` reconstructed trades (`Program.cs` L107) — trade tape, not the trader census.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 No `35=D` builder

Product FIX send sites (`src/Fix.CTrader`):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL (`using` tcp/ssl) |
| `CTraderFixLogonHostedService` | calls `TryLogonAsync` twice (QUOTE:5211, TRADE:5212) | Session objects disposed; no keep-alive initiator |

Grep under `D:\Prop\src` this slot:

| Needle | Hits |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` | **0** |
| `GuardedNewOrderSingle` / `SubmitNewOrderSingle` | **0** |
| `DealerSend` / `SendTrade` in C# | **0** |
| `NewOrderSingle` in product `.cs` | comments / logs / `MayRetryNewOrderSingle` (pure status helper, L35) |
| `RiskEngine.Evaluate(` callers in product | **definition only** (L76). No ingest/copy/FIX caller |

YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling (`mt5_manager.cpp` L1119+); **Prop C# does not call it.**

`CTraderFixLogonHostedService` logons with tag **553 = integer account id**, then **forces** `_runtime.RealCopyEnabled = false` (L68).

### 3.2 Flag cannot arm a missing sender

| Surface | Value |
|---|---|
| `AddTraderIntelligence` L38–41 | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” |
| FIX hosted service L68 | sets `false` again after logon |
| `GET /api/settings` L73–77 | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` hardcoded `false` |
| `GET /api/health` L54 | `realCopyEnabled = runtime.RealCopyEnabled` |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` (L195) |
| `GetRiskAsync` 7th arg | literal `false` → `RealCopyEnabled` (L208) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) |
| `apps/fix-worker/Worker.cs` | even if config true, **refuses** send; stamps TRADE `Disconnected` + “NewOrderSingle remains off” (L41, L45–46) |
| `ShadowCopyEngine` | in-process fill math only. No venue write |
| `PersistDemoShadowAsync` | `CopyIntent.Status = "SHADOW_ONLY"` (store L307). No `ExecutionIntent` writer |
| `RiskEngine.allowSend` | requires `RealExecutionEnabled && ...`; there is still **no** encoder behind it |
| `LiveCopyPage` | static SHADOW warning; no order POST |
| `OverviewPage` L15 / L32 | “Live FIX NewOrderSingle is off”; renders `realCopyEnabled` OFF |

Flipping a JSON/env flag **cannot** place a live order: there is no `GuardedNewOrderSingle`, no QuickFIX initiator, no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines; L85–129) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert (146 lines, 0 `Take`) |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog all; score deals-only (L106) |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` vs `ListLoginsWithDealsAsync`; `SHADOW_ONLY` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real passwords required; `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | ACHIEVER + STARWAVEFX catalog; FIX seed “NewOrderSingle off” |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135 lines) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon then force copy off |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow math, no send |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `MayRetryNewOrderSingle` helper only |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction; 0 product callers of `Evaluate` |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send even if flag true |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Demo 4-login scorer (not API live) |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | All manager groups |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Honest “NewOrderSingle is off” copy |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | Static SHADOW warning |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders({})` |
| `D:\Prop\apps\web\src\api\client.ts` | axios 60 s, no paging |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` / unused `DealerSendOrder` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header + group names (logins not recopied) |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Live `/api/traders` = 8460 |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_16.md` | Same question; hosted-service score set now stale |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_36.md` | Same question; still matches current disk |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_56.md` | Same question; still matches current disk |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`; `GetFixSessionsAsync.ExecutionEnabled` stays `false`; overview `RealCopyEnabled` is the runtime flag that DI and the FIX host pin **false**. Fetch + score + dashboard paint are read/upsert paths. Copy intents, when written, are `SHADOW_ONLY` math. **No loss of trading capital from slot-96 behavior.**

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Auto-score covers all 8460 logins (it covers logins **with deals**; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- A coded `MaySendNewOrderSingle` gate exists (it does not; absence is the safety).
- This slot re-attached to Manager or recomputed SHA-256 (it did not; completeness of last census is the 08:42Z JSON + `CREDENTIALS_AND_COPY_STATUS` HTTP counts).
- `apps/mt5-worker` scores the live book (it still hardcodes four demo logins).
