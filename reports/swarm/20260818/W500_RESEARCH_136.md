# W500_RESEARCH_136 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_136.md` |
| Slot | **136** |
| Date | 2026-08-18 |
| Agent | W500 research 136 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report + `SWARM_LOG.md` / `INDEX.md` catalog lines only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No password, proxy auth, FIX password, or manager login list copied. `.env` residual is the **boolean** `REAL_COPY_EXECUTION_ENABLED=true` only. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (**217** physical lines). Cross-read ingest, store login lists, native Manager connector, live ingest host, API maps, FIX logon session, DI, copy pipeline, runtime flag, Traders/Groups/Overview/LiveCopy pages, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `foreach (var account`, `35=D`, `(35, "D")`, `NewOrderSingle`, `DealerSend`, `Take(`, `ListLogins*`, `Evaluate(`, `/api/copy`. Compared against stale `A005_dashboard_traders.md` and siblings 16/36/56/76/96/116. Re-summed `LIVE_GROUPS_AND_TRADERS.json` `groupNames[].accounts` this slot. **No live HTTP / no re-attach.** No product edit. |

**Honesty:** A005 (`foreach (var s in scores)`; unscored logins invisible) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-ingest **scores** only logins that already have deals; the **list** still emits every persisted `Mt5Account`. Slot 16’s “hosted service scores every stored login” is **stale** vs current `ListLoginsWithDealsAsync`. Slots 56/76/96 “DI/FIX force `RealCopyEnabled=false`” are **stale**. Slot 116 “`FEATURE_COPY` settings literal false / no `/api/copy*` / `GetRiskAsync` literal false” is **stale** vs this disk.

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
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Logon `35=A` only; sender absent | `SAFE_BY_ABSENCE` |
| `RealCopyEnabled` still hard-forced false | **No.** DI binds `.env`; lab key is `true` | `POLICY_RESIDUAL` (does not create a sender) |
| Risk to live capital from this path | **None** from this process | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login (18 / 8460 last measure).
Hosted score = logins-with-deals only; list still shows the rest as INSUFFICIENT_DATA.
FIX copy = logon/recon + SHADOW intents only; no 35=D; NewOrderSingleImplemented=false.
```

**Slot-136 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

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

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. A005’s “detail of unscored login returns null” is **false** on current disk. Cost residual: one keyed lookup still rematerializes the whole leaderboard (`UNSAFE` as 8k lookup; not a hide).

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L96–97). `TradersPage` title is **“All manager traders ({data.length})”** and calls `useTraders({})` with **no** default `state` filter, so `INSUFFICIENT_DATA` rows stay visible. `ScoringPage` uses the same unfiltered hook.

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

### 1.2 Scoring universe ≠ list universe (slot-136 reconfirm)

| Path | Login set | Effect on `/api/traders` |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L38–51 | `GetGroupsAsync` + `GetAccountsAsync(null)` — **all** | Writes `Mt5Accounts` → listed |
| `LiveIngestHostedService` scoring L106–125 | `ListLoginsWithDealsAsync` (`Mt5Deals` distinct logins) | Only those get a `TraderScore`; others stay `INSUFFICIENT_DATA` **but still listed** |
| `POST /api/ops/resync` L114–146 | `ListLoginsAsync` — **all** `Mt5Accounts` | Scores every stored login |
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

| File | Stale claim | Current disk (slot 136) |
|---|---|---|
| `A005_dashboard_traders.md` §0 / §2.2 | Driver `foreach (var s in scores)`; unscored invisible | `foreach (var account in accounts)` + `scoreMap.TryGetValue` |
| A005 `Take(200)` on positions | `accounts.Take(200)` in ingest | **0** `Take(` in `DealIngestionService` (146 lines). Positions via `GetGroupPositionsAsync("*")` |
| A005 health FakeMt5 | hardcoded FakeMt5 string | `/api/health` reports live Manager `groups=` / `accounts=` / `phase=` (`Program.cs` L32–56) |
| A005 / W500_16 axios 15s | `timeout: 15000` | `timeout: 60000` |
| W500_16 “live ingest scores every stored login” | `ListLoginsAsync` | Hosted path is `ListLoginsWithDealsAsync` |
| `C36` / `D21` query body | 168 lines / scores-as-driver era | **217** lines; account driver |
| C42 “live MT5 not proven” | Fake only | Native ×2 on API graph; census 18/8460 (08:42Z) |
| W500_56/76/96 | DI + FIX host pin `RealCopyEnabled=false` | DI binds env; FIX host **does not** assign the flag |
| W500_116 §3.2 / §3.3 | Settings `FEATURE_COPY=false`; `GetRiskAsync` literal `false`; “no `/api/copy*`” | Settings `FEATURE_COPY=true`; risk DTO uses `_runtime.RealCopyEnabled`; `GET /api/copy/status` + `/api/copy/intents` exist |

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first (request API, not pump cache) — L155.
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981: cache/pump `GroupTotal` + `GroupNext`; **no name filter**).

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup` (L216–271): `UserRequestArray` first → fallback `UserGetByGroup` only on hard fail (`!= OK && != OK_NONE && != NOTFOUND`) → if `users.Total()==0` then `UserLogins` + `UserRequestByLogins`.
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

Honesty residual on the request walk: if `UserRequestArray` returns OK/OK_NONE/NOTFOUND with a **partial** `Total() > 0`, `UserGetByGroup` and `UserLogins` are skipped. Completeness then depends on that RPC. Same-day probe still measured empty groups plus 8460 logins, so this is a **theoretical** hole, not a measured hide.

`GetGroupPositionsAsync` uses mask `"*"` when blank (L57–58). Ingest uses that bulk path (`DealIngestionService` L82–86).

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter.

`UpsertGroupsBatchAsync` / `UpsertAccountsBatchAsync` persist **every** incoming row (accounts flush every 500 for memory, not a cap — store L423–424). New groups get `EnabledForAnalysis = true` (store L376). Dashboard `GetGroupsAsync` (L70–82) is `Mt5Groups.ToListAsync` — displays those flags, does **not** filter on them.

`LiveMt5Registration.CreateConnectors` registers **exactly two** native connectors: `BrokerCodes.Achiever` (`ACHIEVER`) and `BrokerCodes.StarwaveFx` (`STARWAVEFX`). Starwave `ProxyEnabled = false` hard pin (L45). DI **throws** if either real Manager password is absent (`DependencyInjection.cs` L36–37). `FakeMt5BrokerConnector` exists as a type but is **not** registered on the API graph (`AddSingleton<IMt5BrokerConnector>` only wraps `CreateConnectors`). Dummy seed is **not** on API startup (`Program.cs` only `EnsureCreated` + `BrokerCatalogSeed`).

`GroupsPage` copy: “Every group visible to the Achiever and Starwave managers.”

`src` `Take(` grep this slot: **3** hits — `EfDashboardQueries` L204 (`Take(20)` reject reasons), `CopyTradingService` L67 (`Take(take)` on **intent** list; API passes 200), `FixMessageParser` checksum slice. **Zero** account/position caps in product ingest. Residual `Take(200)` on `GET /api/trades` is the reconstructed tape (`Program.cs` L110), not the trader census.

### 2.2 Measured live census (prior probe; passwords and logins not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`, probe `LiveBrokerProbe`, note “Passwords never written”). Header fields: Achiever `groups=8 accounts=6512 openPositions=1506`; Starwave `groups=10 accounts=1948 openPositions=478`. `CREDENTIALS_AND_COPY_STATUS.md` independently recorded live dashboard `/api/traders` = **8460** and `/api/groups` = **18**.

This slot independently re-summed `groupNames[].accounts` (no login dump):

| Broker | Connect | Groups | Traders (re-sum) | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 2+179+4+5+4+6295+0+23 = **6512** | 1506 |
| STARWAVEFX | direct | 10 | 11+4+170+1735+22+0+0+4+0+2 = **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (account counts only): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (account counts only): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good). This slot did **not** re-run the live probe. Later briefs citing **8463** are **unreconciled**; this slot pins the JSON: **18 / 8460 / 1984**.

That 8460 HTTP row count is the operational proof that `GetTradersAsync` is **not** a scores-only leaderboard: a scores-only list would be `<=` the subset of logins that have `TraderScore` rows (auto-score = deals-only).

### 2.3 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios **60 s** timeout + 5 s refetch (`hooks.ts` L20–25). That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

During `Phase=scoring`, cataloged logins already appear as `INSUFFICIENT_DATA`. Fail-fast: one `RebuildTraderAsync` throw stops remaining scores; leftover accounts stay visible.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 No `35=D` builder

Product FIX send sites (`D:\Prop\src\Fix.CTrader`):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL (`using var tcp` / `await using var ssl`) |
| `CTraderFixLogonHostedService` | calls `TryLogonAsync` twice (QUOTE:5211, TRADE:5212) | Session objects disposed; no keep-alive initiator |
| `FixSimulationHarness` | `(35, "y")` SecurityList only | In-memory only |

This-slot grep under `D:\Prop\src`:

| Needle | Hits |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` / `GuardedNewOrderSingle` / `SubmitNewOrderSingle` | **0** |
| `DealerSend` in C# under `src\` | **0** |
| `NewOrderSingle` in product `.cs` | comments / logs / `MayRetryNewOrderSingle` (pure status helper) / `CopyTradingService.NewOrderSingleImplemented` const **false** |

YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling (`mt5_manager.cpp` L1119+); **Prop C# does not call it.** YoPips `src` has **0** cTrader FIX `35=D` senders.

`CTraderFixSession` is **135 / 135** lines. The only outbound `WriteAsync` is the assembled Logon. After one `ReadAsync`, sockets dispose. There is no session loop, no Heartbeat writer, no NewOrderSingle method.

API `MapPost` grep: **one** hit — `POST /api/ops/resync` (catalog + score). **No** order POST.

### 3.2 Flag is env-armed (sibling “forced false” is stale)

| Surface | Measured this slot |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` (`DependencyInjection.cs` L41). **Not** a hard `false`. |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; other env values not copied) |
| FIX hosted service | **Does not** assign `_runtime.RealCopyEnabled`. Logs `RealCopyArmed={Armed}` (`CTraderFixLogonHostedService.cs` L68–70). Prior sibling “L68 forces false” is **stale**. |
| Product `RealCopyEnabled =` assignments | **1** hit: DI L41 only |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` hardcoded **`true`** (`Program.cs` L75–77) — matches `CopyTradingService.GetStatusAsync` `FeatureCopyEnabled: true` (W500_116 “settings FEATURE false” is stale) |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` (L52) |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` (L195) |
| `GetRiskAsync` 7th arg | `_runtime.RealCopyEnabled` (L208) — W500_116 “literal false” is **stale** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (POCO; not bound to the env key on the API host) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled` is true, **refuses** send; stamps TRADE `Disconnected` + “NewOrderSingle remains off” (L41, L46) |

Policy residual: architecture §41 / README / `CREDENTIALS_AND_COPY_STATUS` still say the flag is forced **false**. On disk the live API host will start **armed** if `.env` is loaded. That does **not** create a sender.

### 3.3 Copy pipeline exists — still cannot place

Registered on the API graph:

- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (257 lines)
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` (40 lines)
- DI: `AddScoped<CopyTradingService>()` L44; `AddHostedService<CopyTradingHostedService>()` L59
- HTTP: `GET /api/copy/status` L102; `GET /api/copy/intents` L103 (W500_116 “no `/api/copy*`” is **stale**)

Hosted loop: wait 8 s, then every 20 s call `GenerateShadowIntentsAsync` only. Log: `"SHADOW intents. Live NewOrderSingle still blocked."`

Hard constants on the service:

| Const / persist | Value | Effect |
|---|---|---|
| `NewOrderSingleImplemented` | **`false`** | `BuildBlockers` always adds “No NewOrderSingle sender — SAFE_BY_ABSENCE”; live-send `if` is dead |
| `VenueReconciled` | **`false`** | `RiskEngine.allowSend` requires `Reconciled` → send bit stays false |
| Persist `AllowFixSend` | literal **`false`** (L192) | Recorded decision cannot authorize FIX even if `decision.AllowFixSend` were true |
| Live-send branch | `LIVE_SEND_BLOCKED_UNIMPLEMENTED` | **Status string only** — no socket, no `35=D` |
| Else | `SHADOW_ONLY` + optional `ShadowCopyEngine.SimulateEntry` | In-process fill math |

`RiskEngine.Evaluate` **is** called from `GenerateShadowIntentsAsync` L159 (older siblings that said “0 product Evaluate callers” are stale). `allowSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–150). With `VenueReconciled=false` const, `AllowFixSend` from the engine is **false**. The service then **overwrites** persist to `false` anyway.

`PersistDemoShadowAsync` independently writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307).

`BaselineScorer.CanPromoteToLive => false` (L211). `FromBaseline` reachable set is `INSUFFICIENT_DATA` / `RISK_BLOCKED` / `SHADOW` / `WATCH` / `EARLY_SCORE` — **never** `LIVE` / `LIVE_CANDIDATE`.

`LiveCopyPage` is GET-only SHADOW status; no order POST. `OverviewPage`: “Live FIX NewOrderSingle is off — no capital at risk from this dashboard.” `/api/reconciliation/status` note: “NewOrderSingle still off”.

Flipping `.env` / JSON **cannot** place a live order: there is no `GuardedNewOrderSingle`, no QuickFIX initiator, no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke on an armed flag. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines; L85–129) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert (146 lines, 0 `Take`) |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog all; score deals-only (L106) |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` vs `ListLoginsWithDealsAsync`; `SHADOW_ONLY`; flush-500 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Env-bound `RealCopyEnabled`; CopyTrading registration; passwords required |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Shadow pipeline; `NewOrderSingleImplemented=false` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20 s SHADOW-intent tick |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | Gate DTO |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | ACHIEVER + STARWAVEFX catalog |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | `INSUFFICIENT_DATA` default |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false`; `FromBaseline` never LIVE |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction; no encoder |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow math, no send |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `MayRetryNewOrderSingle` helper only |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135 lines) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon; no longer clears RealCopy |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health, `/api/copy*` |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send even if flag true |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Demo 4-login scorer (not API live) |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | All manager groups |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Honest “NewOrderSingle is off” copy |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | GET-only SHADOW status |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | Same unfiltered `useTraders({})` |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders` / `useCopyStatus` |
| `D:\Prop\apps\web\src\api\client.ts` | axios 60 s, no paging |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` / unused-by-Prop `DealerSendOrder` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header + group names (logins not recopied; accounts re-summed) |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Live `/api/traders` = 8460 (copy “forced false” claim stale) |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_96.md` | Same list question; copy pin now stale |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_116.md` | Same list question; settings/`/api/copy`/risk DTO now stale |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`. `CanPromoteToLive` is hardcoded false. Copy intents that exist are `SHADOW_ONLY` (or a dead-branch status string that still writes nothing to FIX). The only cTrader write is a disposed Logon (`35=A`). **No loss of trading capital from slot-136 behavior.**

The residual is **policy**, not wire: `.env` arms `REAL_COPY_EXECUTION_ENABLED=true` and DI honors it; settings now advertise `FEATURE_COPY_TRADING_ENABLED=true`. Do **not** treat either as a live send. Do **not** add a `35=D` builder while §68 / §70 are unpassed.

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Auto-score covers all 8460 logins (it covers logins **with deals**; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- `UserRequestArray` is proven complete on a partial-array return (fallback is skipped when `Total()>0`).
- `RealCopyEnabled` is still hard-forced false (it is not).
- A coded `MaySendNewOrderSingle` gate exists on an armed TRADE session (it does not; absence is the safety).
- This slot re-probed Manager or re-logged FIX (it did not).
- `apps/mt5-worker` scores the live book (it still hardcodes four demo logins).
- Census is 8463 (unreconciled later cite; this slot pins 8460 from the 08:42Z JSON + independent group-sum).
