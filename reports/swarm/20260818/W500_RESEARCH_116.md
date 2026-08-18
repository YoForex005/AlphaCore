# W500_RESEARCH_116 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_116.md` |
| Slot | **116** |
| Date | 2026-08-18 |
| Agent | W500 research 116 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only required write (plus catalog lines in `SWARM_LOG.md` / `INDEX.md`). |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values, proxy auth, FIX password, manager logins, and individual trader logins from the census JSON were not copied. Only the boolean of `REAL_COPY_EXECUTION_ENABLED` is named. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (**217** physical lines). Cross-read ingest, store login lists, native Manager connector, live ingest host, API maps, FIX logon session, **current** DI + `CopyTradingService` + `CopyTradingHostedService`, runtime flag, Traders/Groups/Overview/LiveCopy/Scoring pages, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `foreach (var account`, `35=D`, `(35, "D")`, `GuardedNewOrderSingle`, `DealerSend`, `Take(`, `ListLogins*`, `Evaluate(`, `RealCopyEnabled`. Compared against stale `A005_dashboard_traders.md` and siblings 16 / 36 / 56 / 76 / 96. Re-summed `LIVE_GROUPS_AND_TRADERS.json` group `accounts` (logins not recopied). No live Manager re-attach. No TLS / no Logon this slot. No product edit. |

**Honesty:** A005 (`foreach (var s in scores)` / unscored logins invisible) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-ingest **scores** only logins that already have deals; the **list** still emits every persisted `Mt5Account`. Siblings 56 / 76 / 96 still hold on the **list** question. They are **stale** on two copy-path facts measured this slot: (1) `CopyTradingService` + hosted shadow-intent loop now exist and **do** call `RiskEngine.Evaluate`; (2) DI no longer hard-pins `RealCopyEnabled=false` — `.env` line 73 is `REAL_COPY_EXECUTION_ENABLED=true` and FIX logon **no longer overwrites** the flag to false. Live `35=D` is still **absent**. Capital is safe by **sender absence**, not by a forced-false flag.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No.** Driver is `foreach (var account in accounts)` L99 | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores + PnL | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GroupRequestArray("*")` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Live HTTP census of that catalog | **18 groups / 8460 traders** (08:42Z JSON + `CREDENTIALS_AND_COPY_STATUS`) | `MEASURED` (not re-probed this slot) |
| Auto-score covers every cataloged login | **No.** Hosted service uses `ListLoginsWithDealsAsync` | `RESIDUAL` (list still complete) |
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Only wire write is Logon `35=A`. `NewOrderSingleImplemented=false` const. Persist `AllowFixSend=false`. | `SAFE_BY_ABSENCE` |
| `RealCopyEnabled` still hard-pinned `false` | **No (stale sibling claim).** DI binds env; `.env` is `true`; FIX host no longer clears it | `POLICY_RESIDUAL` |
| Risk to live capital from this path | **None** from this process (no encoder) | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login (18 / 8460 last measure).
Auto-score = logins that have deals; unscored rows still list as INSUFFICIENT_DATA.
FIX copy = logon 35=A only; NewOrderSingle unimplemented; env REAL_COPY=true is armed but cannot place.
```

**Slot-116 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

---

## 1. Assigned question — scores-only vs all `Mt5Accounts`

Current method (`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L85–129), re-read in full this slot (file ends at L217 `MaskLogin`):

1. Load **all** `TraderScores` (`AsNoTracking`) — **join payload, not the driver** (L87).
2. Load **all** `Brokers` into `Id` dictionary (L88).
3. Load **all** `Mt5Accounts` — **this is the `foreach` driver** (L89, L99).
4. Load completed `ReconstructedTrades` PnL grouped by `(BrokerId, Login)` (L90–95).
5. Build `scoreMap` / `pnlMap` (L96–97).
6. For **each account**: skip only if `BrokerId` is missing from `Brokers` (`continue`); `scoreMap.TryGetValue` / `pnlMap.TryGetValue` (left join); **always** `mapped.Add(...)` (L99–120).
7. Optional in-memory `broker` / `state` filters (`OrdinalIgnoreCase` / `Enum.TryParse(..., true)`) (L122–126).
8. `OrderByDescending(t => t.EarlyScore)`. **No `Skip`/`Take` on the trader set** (L128).

This-slot grep of `foreach (var account` in this file = **one** hit: L99. There is **no** `foreach (var s in scores)`.

The only `Take` in this file is `Take(20)` on risk-reject **reasons** (L204), not traders.

Missing score does **not** drop the row:

| Field | If `TraderScore` missing |
|---|---|
| `CompletedXauTrades` | `0` |
| `NetSourcePnl` | reconstructed PnL or `0` |
| `EarlyScore` / `RiskScore` | `0` |
| `MlProbability` | literal `null` (7th ctor arg; ML not built) |
| Flags (`Martingale` / `AveragingDown` / `LotEscalation`) | `false` |
| `State` | `TraderState.INSUFFICIENT_DATA` (`D:\Prop\src\Domain\Enums\TraderState.cs` L5) |
| `ShadowPnl` | literal `0` (even when a score exists) |
| `LastScored` | `account.LastSyncedAt` |

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. `GetTraderDetailAsync` (L137–172) uses that header; A005’s “detail of unscored login returns null” is **false** on current disk.

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L95–96). `TradersPage` title is **“All manager traders ({data.length})”** and calls `useTraders({})` with **no** default `state` filter (`D:\Prop\apps\web\src\pages\TradersPage.tsx` L4–9). `ScoringPage` uses the same unfiltered hook (`ScoringPage.tsx` L4). `INSUFFICIENT_DATA` rows stay visible.

Axios timeout is **60 000 ms** (`D:\Prop\apps\web\src\api\client.ts` L5). A005 “15s” is **stale**.

Product tests: `grep` of `D:\Prop\tests` for `EfDashboardQueries` / `GetTradersAsync` = **0**. Completeness is source-proven + prior live HTTP, not unit-proven.

### 1.1 What is still scores-only (not the trader list)

`GetOverviewAsync` L20–53:

| Card | Source |
|---|---|
| `TotalAccounts` | `Mt5Accounts.CountAsync` — **all accounts** |
| `XauTraders` / `TradersWithThreeTrades` | `TraderScores` counts (`CompletedXauTrades > 0` / `>= 3`) |
| `Watch` / `Shadow` / `LiveCandidates` / `Live` / `RiskBlocked` | `TraderScores.CurrentState` only |
| Destination real P&L / XAU gross / XAU net | literals **`0`** |
| `RealCopyEnabled` | `_runtime.RealCopyEnabled` (now env-bound; see §3.2) |

Until scoring upserts a row, state tiles **under-count**. The `/traders` table still lists the cataloged login. That is the residual A005 intuition that remains true **only** for overview buckets.

Orphan `TraderScores` (score without an `Mt5Account`) are **invisible** on the leaderboard. That is correct for “all manager traders”: the census is the account book.

Accounts whose `BrokerId` is not in `Brokers` are skipped (`continue`). Catalog seed writes `ACHIEVER` + `STARWAVEFX` (`BrokerCatalogSeed.EnsureAsync`); live ingest resolves `BrokerCodes.Achiever` / `BrokerCodes.StarwaveFx`. Not a hide of Manager users.

### 1.2 Scoring universe ≠ list universe

| Path | Login set | Effect on `/api/traders` |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` | `GetGroupsAsync` + `GetAccountsAsync(null)` — **all** | Writes `Mt5Accounts` → listed |
| `LiveIngestHostedService` scoring | `ListLoginsWithDealsAsync` (`Mt5Deals` distinct logins) L106 | Only those get a `TraderScore`; others stay `INSUFFICIENT_DATA` **but still listed** |
| `POST /api/ops/resync` | `ListLoginsAsync` — **all** `Mt5Accounts` (`Program.cs` L121–136) | Scores every stored login for `ACHIEVER` then `STARWAVEFX` |
| `apps/mt5-worker/Worker.cs` | hardcoded `{10001,10002,10003,99001}` L31 | Demo host only; **not** the API live graph |

`EfTradingStore` (`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`):

```339:345:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);

    public Task<IReadOnlyList<long>> ListLoginsWithDealsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Deals.Where(d => d.BrokerId == brokerId).Select(d => d.Login).Distinct().ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Hosted-service log line L125: `"scored {Scored} logins that have deals"`. That is a **score-freshness** split, not a dashboard hide.

`ReconstructionScoringService.RebuildTraderAsync` always `UpsertScoreAsync` for the logins it is given (L128–142). It is not called for deal-less logins on the auto path.

### 1.3 Stale reports to ignore for this question

| Report | Stale claim | Current disk |
|---|---|---|
| `A005_dashboard_traders.md` §0 / §2.2 | Driver `foreach (var s in scores)`; unscored invisible | `foreach (var account in accounts)` + left join |
| `A005` positions | `accounts.Take(200)` position snapshot | `GetGroupPositionsAsync("*")` or all accounts; **0** `Take(` in ingest |
| `A005` health | `/api/health` says FakeMt5 | Live `runtime.Brokers` groups/accounts/phase (`Program.cs` L32–56) |
| `A005` / `A013` axios | 15 s timeout | `client.ts` `timeout: 60000` |
| `A005` / W500_RESEARCH_16 ingest host | scores all `ListLoginsAsync` | scores `ListLoginsWithDealsAsync` |
| `C36` / `D21` query body | 168 lines / scores-as-driver era | **217** lines; account driver |
| W500 56 / 76 / 96 copy pin | DI + FIX host force `RealCopyEnabled=false`; 0 product `Evaluate` callers | Env-bound flag; `.env` `true`; `CopyTradingService.Evaluate` exists; still no `35=D` |

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first (request API, not pump cache) L155.
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981). YoPips is **cache-only** (`GroupTotal`/`GroupNext`). Prop C# prefers the request array, then the same pump walk.

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup`: `UserRequestArray` (L223) → fallback `UserGetByGroup` (L225) → if still empty `UserLogins` + `UserRequestByLogins` (L230–232).
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

`GetGroupPositionsAsync` uses mask `"*"` when blank (L57–58). Ingest uses that bulk path.

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` (L54–98) repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter. This-slot `Take(` grep in that file = **0**.

`UpsertGroupsBatchAsync` persists **every** incoming group. New groups get `EnabledForAnalysis = true` (store L376). Dashboard `GetGroupsAsync` (query L70–82) is `Mt5Groups.ToListAsync` — displays those flags, does **not** filter on them.

`LiveMt5Registration.CreateConnectors` registers **exactly two** native connectors: `BrokerCodes.Achiever` (`ACHIEVER`) and `BrokerCodes.StarwaveFx` (`STARWAVEFX`). Starwave `ProxyEnabled = false` (hard pin; env unread). DI **throws** if either real Manager password is absent (`DependencyInjection.cs` L36–37). `FakeMt5BrokerConnector` is **not** registered on the API graph. Dummy seed is **not** on API startup (`Program.cs` L149–154 only `EnsureCreated` + `BrokerCatalogSeed`).

`GroupsPage` copy: “Every group visible to the Achiever and Starwave managers.” (`GroupsPage.tsx` L10).

This-slot `Take(200)` under product hosts = **one** site: `GET /api/trades` reconstructed explorer (`Program.cs` L107). Not the trader census.

### 2.2 Measured live census (prior probe; passwords and logins not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`, probe `LiveBrokerProbe`, `envLoaded=true`). `CREDENTIALS_AND_COPY_STATUS.md` independently recorded live dashboard `/api/traders` = **8460** and `/api/groups` = **18**.

This slot **re-summed** the JSON `groupNames[].accounts` (logins not recopied):

| Broker | Connect | Groups | Traders (JSON `accounts`) | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever (re-sum `2+179+4+5+4+6295+0+23 = 6512`): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave (re-sum `11+4+170+1735+22+0+0+4+0+2 = 1948`): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good). This slot did **not** re-run the live probe.

That 8460 HTTP row count is the operational proof that `GetTradersAsync` is **not** a scores-only leaderboard: a scores-only list would be `<=` the subset of logins that have `TraderScore` rows (auto-score = deals-only).

### 2.3 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios **60 s** timeout. That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

During `Phase=scoring`, cataloged logins already appear as `INSUFFICIENT_DATA`. Fail-fast: one `RebuildTraderAsync` throw stops remaining scores; leftover accounts stay visible.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 No `35=D` builder

Product FIX send sites (`D:\Prop\src\Fix.CTrader`):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL (`using var tcp` / `await using var ssl`) |
| `CTraderFixLogonHostedService` | calls `TryLogonAsync` twice (QUOTE:5211, TRADE:5212) | Session objects disposed; no keep-alive initiator |
| `FixSimulationHarness` | `(35, "A")` | In-memory only |

This-slot grep under `D:\Prop\src`:

| Needle | Hits |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` / `GuardedNewOrderSingle` / `SubmitNewOrderSingle` | **0** |
| `DealerSend` in C# under `src\` | **0** |
| `NewOrderSingle` in product `.cs` | comments / logs / `MayRetryNewOrderSingle` (pure status helper) / `CopyTradingService.NewOrderSingleImplemented` const **false** |

YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling (`mt5_manager.cpp` L1119+); **Prop C# does not call it.** YoPips `src` has **0** cTrader FIX `35=D` senders.

`CTraderFixSession` is **135 / 135** lines. The only outbound `WriteAsync` is the assembled Logon. After one `ReadAsync`, sockets dispose. There is no session loop, no Heartbeat writer, no NewOrderSingle method.

### 3.2 Flag is now env-armed (sibling “forced false” is stale)

| Surface | Measured this slot |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` (`DependencyInjection.cs` L41). **Not** a hard `false`. |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; other env values not copied) |
| FIX hosted service | **Does not** assign `_runtime.RealCopyEnabled`. Logs `RealCopyArmed={Armed}` (`CTraderFixLogonHostedService.cs` L68–70). Prior sibling “L68 forces false” is **stale**. |
| Product `RealCopyEnabled =` assignments | **1** hit: DI L41 only |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` still hardcoded **`false`** (`Program.cs` L73–76) — **inconsistent** with `CopyTradingService.GetStatusAsync` which reports `FeatureCopyEnabled: true` |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` (L52) |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` (L195) |
| `GetRiskAsync` 7th arg | literal `false` → `RealCopyEnabled` (L208) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (POCO; not bound to the env key on the API host) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled` is true, **refuses** send; stamps TRADE `Disconnected` + “NewOrderSingle remains off” (L41, L46) |

Policy residual: architecture §41 / README / `CREDENTIALS_AND_COPY_STATUS` still say the flag is forced **false**. On disk the live API host will start **armed** if `.env` is loaded. That does **not** create a sender.

### 3.3 Copy pipeline now exists — still cannot place

New (vs W500 56/76/96) product types, registered this slot:

- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (257 lines)
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` (40 lines)
- DI: `AddScoped<CopyTradingService>()` L44; `AddHostedService<CopyTradingHostedService>()` L59

Hosted loop: wait 8 s, then every 20 s call `GenerateShadowIntentsAsync` only. Log: `"SHADOW intents. Live NewOrderSingle still blocked."`

Hard constants on the service:

| Const | Value | Effect |
|---|---|---|
| `NewOrderSingleImplemented` | **`false`** | `BuildBlockers` always adds “No NewOrderSingle sender — SAFE_BY_ABSENCE”; live-send `if` is dead |
| `VenueReconciled` | **`false`** | `RiskEngine.allowSend` requires `Reconciled` → send bit stays false |
| Persist `AllowFixSend` | literal **`false`** (L192) | Recorded decision cannot authorize FIX even if `decision.AllowFixSend` were true |
| Live-send branch | `LIVE_SEND_BLOCKED_UNIMPLEMENTED` | **Status string only** — no socket, no `35=D` |
| Else | `SHADOW_ONLY` + optional `ShadowCopyEngine.SimulateEntry` | In-process fill math |

`RiskEngine.Evaluate` **is** called from `GenerateShadowIntentsAsync` L159 (siblings that said “0 product Evaluate callers” are stale). `allowSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–150). With `VenueReconciled=false` const, `AllowFixSend` from the engine is **false**. The service then **overwrites** persist to `false` anyway.

`PersistDemoShadowAsync` independently writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307).

`BaselineScorer.CanPromoteToLive => false` (L211). `FromBaseline` reachable set never includes `LIVE`.

`LiveCopyPage` is a static SHADOW warning; no order POST. `OverviewPage`: “Live FIX NewOrderSingle is off — no capital at risk from this dashboard.” `/api/reconciliation/status` note: “NewOrderSingle still off”. No `/api/copy*` map on `Program.cs`.

Flipping `.env` / JSON **cannot** place a live order: there is no `GuardedNewOrderSingle`, no QuickFIX initiator, no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke on an armed flag. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines; L85–129) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog all; score deals-only |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` vs `ListLoginsWithDealsAsync`; SHADOW_ONLY |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Env-bound `RealCopyEnabled`; CopyTrading registration |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Shadow pipeline; `NewOrderSingleImplemented=false` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20 s SHADOW-intent tick |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | Gate DTO |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | `INSUFFICIENT_DATA` default |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction; no encoder |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135 lines) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon; no longer clears RealCopy |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send even if flag true |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Demo 4-login scorer (not API live) |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | All manager groups |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Honest “NewOrderSingle is off” copy |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | Static SHADOW warning |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | Same unfiltered `useTraders({})` |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders` |
| `D:\Prop\apps\web\src\api\client.ts` | axios 60 s, no paging |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` / unused-by-Prop `DealerSendOrder` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header + group names (logins not recopied) |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Live `/api/traders` = 8460 |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_56.md` | Same list question; copy pin now stale |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_76.md` | Same list question; copy pin now stale |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_96.md` | Same list question; copy pin now stale |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`. `CanPromoteToLive` is hardcoded false. Copy intents that exist are `SHADOW_ONLY` (or a dead-branch status string that still writes nothing to FIX). The only cTrader write is a disposed Logon (`35=A`). **No loss of trading capital from slot-116 behavior.**

The new residual is **policy**, not wire: `.env` arms `REAL_COPY_EXECUTION_ENABLED=true` and DI honors it. Do **not** treat that as a live send. Do **not** add a `35=D` builder while §68 / §70 are unpassed.

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Auto-score covers all 8460 logins (it covers logins **with deals**; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- `RealCopyEnabled` is still hard-forced false (it is not).
- A coded `MaySendNewOrderSingle` gate exists on an armed TRADE session (it does not; absence is the safety).
- This slot re-probed Manager or re-logged FIX (it did not).
