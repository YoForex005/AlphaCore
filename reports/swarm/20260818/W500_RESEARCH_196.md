# W500_RESEARCH_196 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_196.md` |
| Slot | **196** |
| Date | 2026-08-18 |
| Agent | W500 research 196 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report + `SWARM_LOG.md` / `INDEX.md` catalog lines only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No password, proxy auth, FIX password, or manager/trader login list copied. `.env` residual named is the **boolean** `REAL_COPY_EXECUTION_ENABLED=true` only. Live account id `1369850` appears only as the **demo-helper refuse list**. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (**217** physical lines; ends at L217 `MaskLogin`). Cross-read ingest, store login lists, native Manager connector, live ingest host, API maps, FIX logon session, **current** `CTraderFixDemoTestTrade.cs` (**391** lines), DI, copy pipeline, runtime flag, Traders/Groups/Overview/LiveCopy/Scoring pages, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `foreach (var account`, `35=D`, `(35, "D")`, `Build("D"`, `GuardedNewOrderSingle`, `DealerSend`, `Take(`, `ListLogins*`, `ExecutionIntents.Add`, `Evaluate(`, `/api/copy`. Compared against stale `A005_dashboard_traders.md` and siblings 36/56/76/96/116/136/156/176. Independently re-summed `LIVE_GROUPS_AND_TRADERS.json` `groupNames[].accounts` (logins not recopied). **No live HTTP / no re-attach / no TLS logon this slot.** No product edit. SHA-256 not rehashed (no shell). |

**Honesty:** A005 (`foreach (var s in scores)`; unscored logins invisible) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-ingest **scores** only logins that already have deals; the **list** still emits every persisted `Mt5Account`. Slots 36/56/76/96 still hold on the **list** question. Slot 116 is **stale** on settings / `/api/copy*` / risk DTO. Slots 136/156 “product C# has **0** `35=D`” is **stale**: a **demo-gated** encoder exists in `CTraderFixDemoTestTrade.cs`. Slot 176’s helper line-map is **stale** (349 lines / `Build("D")` at L126/L157): current file is **391** lines with `Build("D")` at **L139 / L163 / L197**. Slot 176 / this slot’s first pass `AllocationFactor=0.05m` is **stale mid-slot**: HEAD `CopyTradingService` now aliases `XauUsdOneToOneCopyPolicy.AllocationFactor = 1m` and only shadows **open** XAU of policy-eligible traders. That is dest-ruin **if** a sender existed. The **copy / API / hosted FIX** path still has **no** NewOrderSingle. Capital is safe by **sender absence on the copy graph**, plus a demo-only CLI gate that refuses the live account. Do **not** treat 1:1 shadow qty as a live ticket.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No (stale).** Driver is `foreach (var account in accounts)` L99 | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores + PnL | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GroupRequestArray("*")` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Live HTTP census of that catalog | **18 groups / 8460 traders** (08:42Z JSON + `CREDENTIALS_AND_COPY_STATUS`). P500 later cited **8463** (`+3` unreconciled) | `MEASURED` (not re-probed this slot) |
| Hosted scoring covers every cataloged login | **No.** `ListLoginsWithDealsAsync` only | `EXISTS_NEEDS_REFACTOR` (score freshness, not list hide) |
| Copy-to-cTrader pipeline emits `NewOrderSingle` / `35=D` | **No.** Hosted FIX = logon `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; **0** `ExecutionIntent` writers. Shadow qty is now **1:1 lots** (`XauUsdOneToOneCopyPolicy`) — dest-ruin **if** sent | `SAFE_BY_ABSENCE` |
| Any `35=D` builder on disk | **Yes — demo CLI only.** `CTraderFixDemoTestTrade.Build("D")` ×3 (L139/L163/L197); caller `tools/DemoFixTestTrade`; demo host/sender + refuse live account `1369850` | `DEMO_ONLY_NOT_COPY` |
| `RealCopyEnabled` still hard-forced false | **No.** DI binds `.env`; lab key is `true` | `POLICY_RESIDUAL` (does not create a copy sender) |
| Risk to live capital from this path | **None** from API/copy/ingest | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login (18 / 8460 last measure).
Hosted score = logins-with-deals only; list still shows the rest as INSUFFICIENT_DATA.
Copy FIX = logon 35=A + SHADOW intents; NewOrderSingle unimplemented.
Residual 35=D encoder is tools/DemoFixTestTrade (demo-gated; not on the copy graph).
```

**Slot-196 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

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

This-slot grep of `foreach (var` in this file = **three** hits: brokers L59, groups L75, **`foreach (var account in accounts)` L99**. There is **no** `foreach (var s in scores)`.

The only `Take` in this file is `Take(20)` on risk-reject **reasons** (L204), not traders.

`EnabledForAnalysis` / `PlanMapping` appear only as **display** fields on `GetGroupsAsync` L79 — they are **not** a `Where` on the group or trader list.

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

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. `GetTraderDetailAsync` (L137–172) uses that header; A005’s “detail of unscored login returns null” is **false** on current disk. Cost residual: one keyed lookup still rematerializes the whole leaderboard (`UNSAFE` as an 8k lookup; not a hide).

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L96–97). `TradersPage` title is **“All manager traders ({data.length})”** and calls `useTraders({})` with **no** default `state` filter (`D:\Prop\apps\web\src\pages\TradersPage.tsx` L4–9). `ScoringPage` uses the same unfiltered hook (`ScoringPage.tsx` L4). `INSUFFICIENT_DATA` rows stay visible.

Axios timeout is **60 000 ms** (`D:\Prop\apps\web\src\api\client.ts` L5). A005 “15s” is **stale**.

Product tests: `grep` of `D:\Prop\tests` for `EfDashboardQueries` / `GetTradersAsync` = **0**. Completeness is source-proven + prior live HTTP, not unit-proven.

`TraderScore.BehaviorScore` is loaded by `GetTradersAsync` and **discarded**. `ScoringPage` still reads `t.behaviorScore ?? 0` — that column is **0** on the wire, not a hide of the login.

### 1.1 What is still scores-only (not the trader list)

`GetOverviewAsync` L20–53:

| Card | Source |
|---|---|
| `TotalAccounts` | `Mt5Accounts.CountAsync` — **all accounts** |
| `XauTraders` / `TradersWithThreeTrades` | `TraderScores` counts (`CompletedXauTrades > 0` / `>= 3`) |
| `Watch` / `Shadow` / `LiveCandidates` / `Live` / `RiskBlocked` | `TraderScores.CurrentState` only |
| Destination real P&L / XAU gross / XAU net | literals **`0`** |
| `RealCopyEnabled` | `_runtime.RealCopyEnabled` (env-bound; see §3.2) |

Until scoring upserts a row, state tiles **under-count**. The `/traders` table still lists the cataloged login. That is the residual A005 intuition that remains true **only** for overview buckets.

Orphan `TraderScores` (score without an `Mt5Account`) are **invisible** on the leaderboard. That is correct for “all manager traders”: the census is the account book.

Accounts whose `BrokerId` is not in `Brokers` are skipped (`continue`). Catalog seed writes `ACHIEVER` + `STARWAVEFX` (`BrokerCatalogSeed.EnsureAsync`); live ingest resolves `BrokerCodes.Achiever` / `BrokerCodes.StarwaveFx`. Not a hide of Manager users.

### 1.2 Scoring universe ≠ list universe

| Path | Login set | Effect on `/api/traders` |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` | `GetGroupsAsync` + `GetAccountsAsync(null)` — **all** | Writes `Mt5Accounts` → listed |
| `LiveIngestHostedService` scoring | `ListLoginsWithDealsAsync` (`Mt5Deals` distinct logins) L106 | Only those get a `TraderScore`; others stay `INSUFFICIENT_DATA` **but still listed** |
| `POST /api/ops/resync` | `ListLoginsAsync` — **all** `Mt5Accounts` (`Program.cs` L124–146) | Scores every stored login for `ACHIEVER` then `STARWAVEFX` |
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

This-slot `Take(` grep in `DealIngestionService.cs` = **0**. Residual `Take(200)` is `GET /api/trades` (`Program.cs` L110), not the trader census.

### 1.3 Stale reports to ignore for this question

| Report | Stale claim | Current disk |
|---|---|---|
| `A005_dashboard_traders.md` §0 / §2.2 | Driver `foreach (var s in scores)`; unscored invisible | `foreach (var account in accounts)` + left join |
| `A005` positions | `accounts.Take(200)` position snapshot | `GetGroupPositionsAsync("*")` or all accounts; **0** `Take(` in ingest |
| `A005` health | `/api/health` says FakeMt5 | Live `runtime.Brokers` groups/accounts/phase (`Program.cs` L33–58) |
| `A005` / `A013` axios | 15 s timeout | `client.ts` `timeout: 60000` |
| `A005` ingest host | scores all `ListLoginsAsync` | scores `ListLoginsWithDealsAsync` |
| `C36` / `D21` query body | 168 lines / scores-as-driver era | **217** lines; account driver |
| W500 56 / 76 / 96 copy pin | DI + FIX host force `RealCopyEnabled=false`; 0 product `Evaluate` callers | Env-bound flag; `.env` `true`; `CopyTradingService.Evaluate` exists; still no copy `35=D` |
| W500 116 §3.2 `GetRiskAsync` | 7th arg literal `false` | `_runtime.RealCopyEnabled` (L208) |
| W500 116 §3.2 `/api/settings` FEATURE | hardcoded `false` | `FEATURE_COPY_TRADING_ENABLED = true` (`Program.cs` L77) |
| W500 116 §3.3 copy API | “No `/api/copy*` map” | `GET /api/copy/status` L102 + `GET /api/copy/intents` L103 |
| W500 127 logon | hosted FIX re-pins `RealCopyEnabled=false` | Host only logs `RealCopyArmed={Armed}` (`CTraderFixLogonHostedService.cs` L68–70) |
| W500 136 / 156 §3.1 | Product C# `35=D` hits = **0** | `CTraderFixDemoTestTrade.Build("D")` exists; **not** wired to copy/API |
| W500 176 helper map | 349 lines; `Build("D")` L126/L157 | **391** lines; `Build("D")` L139/L163/L197; `flattenOnly` 8th arg default |
| `CREDENTIALS_AND_COPY_STATUS.md` | `REAL_COPY` **false (forced)**; “method does not exist” | DI binds env; lab `.env` L73 is `true`; demo helper **does** assemble `D` off-hop |
| `LIVE_MANAGER_FETCH_MEASURED.md` §Copy | `REAL_COPY` forced **false** | Same policy residual as CREDENTIALS |

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

`GetGroupPositionsAsync` uses mask `"*"` when blank (L57–58). Ingest uses that bulk path (`DealIngestionService` L84). `GetGroupPositionsCore` is `PositionRequestByGroup` then cache `PositionGetByGroup` (L336–352). **No `Take`.**

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` (L54–98) repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter.

`UpsertGroupsBatchAsync` persists **every** incoming group. New groups get `EnabledForAnalysis = true` (store L376). Dashboard `GetGroupsAsync` (query L70–82) is `Mt5Groups.ToListAsync` — displays those flags, does **not** filter on them.

`LiveMt5Registration.CreateConnectors` registers **exactly two** native connectors: `BrokerCodes.Achiever` (`ACHIEVER`) and `BrokerCodes.StarwaveFx` (`STARWAVEFX`). Starwave `ProxyEnabled = false` (hard pin; env unread). DI **throws** if either real Manager password is absent (`DependencyInjection.cs` L36–37). `FakeMt5BrokerConnector` is **not** registered on the API graph. Dummy seed is **not** on API startup (`Program.cs` L152–156 only `EnsureCreated` + `BrokerCatalogSeed`).

`GroupsPage` copy: “Every group visible to the Achiever and Starwave managers.” (`GroupsPage.tsx` L10).

This-slot `Take(200)` under product hosts = **one** site: `GET /api/trades` reconstructed explorer (`Program.cs` L110). Not the trader census.

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

P500 later live overview cited **8463** accounts (`+3` vs 08:42Z JSON). The delta is **unreconciled**. Do not treat either number as dest fills. A scores-only list would still be `<=` the subset of logins that have `TraderScore` rows (auto-score = deals-only). Either 8460 or 8463 HTTP row count is operational proof that `GetTradersAsync` is **not** a scores-only leaderboard.

### 2.3 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios **60 s** timeout. That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

During `Phase=scoring`, cataloged logins already appear as `INSUFFICIENT_DATA`. Fail-fast: one `RebuildTraderAsync` throw stops remaining scores; leftover accounts stay visible.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 Copy hop has no `35=D` builder

Product FIX **hosted** send site (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`, **135 / 135** lines):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL (`using var tcp` / `await using var ssl`) |
| `CTraderFixLogonHostedService` | calls `TryLogonAsync` twice (QUOTE:5211, TRADE:5212) | Session objects disposed; no keep-alive initiator |
| `CTraderQuoteService` | `(35, "y")` SecurityList / `(35, "V")` MarketDataRequest | Builders only; not a NewOrderSingle |
| `FixSimulationHarness` | `(35, "A")` / `"3"` / `"0"` / `"y"` / `"X"` / `"8"` | In-memory only; **no** `"D"` |

This-slot grep:

| Needle | Hits |
|---|---|
| Literal `35=D` under `D:\Prop\src` `*.cs` | **0** (encoder uses `Build("D")`, not the substring) |
| `(35, "D")` / `MsgType="D"` / `GuardedNewOrderSingle` / `SubmitNewOrderSingle` | **0** |
| `DealerSend` in C# under `src\` | **0** |
| `Build("D"` | **3** — only `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 |
| `NewOrderSingle` in product `.cs` | comments / logs / `MayRetryNewOrderSingle` (pure status helper) / `CopyTradingService.NewOrderSingleImplemented` const **false** |
| `ExecutionIntents.Add` / `_db.ExecutionIntents` write | **0** writers. Only read is `CountAsync(e => e.SentAt != null)` in `GetStatusAsync` L38 |

YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling (`mt5_manager.cpp` L1119+); **Prop C# does not call it.** YoPips `src` has **0** cTrader FIX `35=D` / `NewOrderSingle` senders.

`CTraderFixSession` is **135 / 135** lines. The only outbound `WriteAsync` is the assembled Logon. After one `ReadAsync`, sockets dispose. There is no session loop, no Heartbeat writer, no NewOrderSingle method.

### 3.2 Residual demo CLI encoder (not the copy graph)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` — **391 / 391** lines this slot (W176 “349” is stale).

`SendAsync` signature is **8 parameters** with `flattenOnly = false` default (L33–41). `tools/DemoFixTestTrade/Program.cs` L33 passes that 8th arg. W175 “8-arg call vs 7-arg method compile drift” is **stale**.

`Build("D", …)` write sites:

| Line | Purpose | Qty |
|---|---|---|
| L139 | flatten existing gold position | tag 38 = pos 704/705 or `"1"` |
| L163 | open market buy | tag 38 hardcoded **`"1"`** |
| L197 | close after fill | tag 38 = ER 32/14 or `"1"` |

Fail-closed gate **before any TCP** (L43–47): host must start `demo-`; SenderCompID must start `demo.`; host/sender must not contain `live-` / `live.`; account **`1369850` refused**.

Callers of `CTraderFixDemoTestTrade` / `SendAsync` in product `*.cs`:

| Path | Role |
|---|---|
| `Sessions\CTraderFixDemoTestTrade.cs` | definition |
| `tools\DemoFixTestTrade\Program.cs` L33 | **only** caller |

**0** hits in `Infrastructure`, `apps\api`, `apps\mt5-worker`, `apps\fix-worker`, `CopyTradingService`, `CTraderFixLogonHostedService`. Not registered in `AddTraderIntelligence`.

This slot did **not** run the tool. Residual capital path is **operator running the standalone CLI against a demo gateway**, not Achiever/Starwave copy placing a live Pepperstone ticket.

### 3.3 Flag is now env-armed (sibling “forced false” is stale)

| Surface | Measured this slot |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", OrdinalIgnoreCase)` (`DependencyInjection.cs` L41). **Not** a hard `false`. |
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; other env values not copied) |
| FIX hosted service | **Does not** assign `_runtime.RealCopyEnabled`. Logs `RealCopyArmed={Armed}` (`CTraderFixLogonHostedService.cs` L68–70). W127 “logon re-pins false” is **stale**. |
| Product `RealCopyEnabled =` assignments | **1** hit: DI L41 only |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` now **`true`** (`Program.cs` L74–77) |
| `GET /api/health` | `realCopyEnabled = runtime.RealCopyEnabled` (L55) |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` (L52) |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` (L195) — **still a hard-false display**, not a send gate |
| `GetRiskAsync` 7th arg | `_runtime.RealCopyEnabled` (L208) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (POCO; not bound to the env key on the API host) |
| `apps/fix-worker/Worker.cs` | even if `CTrader:RealCopyExecutionEnabled` is true, **refuses** send; stamps TRADE `Disconnected` + “NewOrderSingle remains off” (L41, L46) |

Policy residual: architecture §41 / README / `CREDENTIALS_AND_COPY_STATUS` still say the flag is forced **false**. On disk the live API host will start **armed** if `.env` is loaded. That does **not** create a copy sender.

### 3.4 Copy pipeline exists — still cannot place

Product types, registered this slot (re-read after concurrent edit):

- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (**320** lines; first pass this slot was 257)
- `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` (**173** lines) — new mid-wave
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` (**40** lines)
- DI: `AddScoped<CopyTradingService>()` L44; `AddHostedService<CopyTradingHostedService>()` L59

Hosted loop: wait 8 s, then every 20 s call `GenerateShadowIntentsAsync` only. Log: `"SHADOW intents. Live NewOrderSingle still blocked."`

Hard constants on the service / policy:

| Const | Value | Effect |
|---|---|---|
| `NewOrderSingleImplemented` | **`false`** | `BuildBlockers` always adds “No NewOrderSingle sender — SAFE_BY_ABSENCE”; live-send `if` is dead |
| `VenueReconciled` | **`false`** | `RiskEngine.allowSend` requires `Reconciled` → send bit stays false |
| `AllocationFactor` | **`1m`** (via `XauUsdOneToOneCopyPolicy`) | Shadow `RequestedQuantity` is 1:1 source lots (`0.10→0.10`). **Dest-ruin if a sender existed.** W176 `0.05m` is **stale**. |
| Persist `AllowFixSend` | literal **`false`** (L211) | Recorded decision cannot authorize FIX even if `decision.AllowFixSend` were true |
| Live-send branch | `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L217–219) | **Status string only** — no socket, no `35=D` |
| Else | `SHADOW_ONLY` + optional `ShadowCopyEngine.SimulateEntry` | In-process fill math |

Policy eligibility (`IsTraderEligible`): not `RISK_BLOCKED`/`DISQUALIFIED`/`PAUSED`; not `INSUFFICIENT_DATA`/`EARLY_SCORE`/`WATCH`; no MG/AVG/ESC flags; `CompletedXauTrades >= 20`; XAU net PnL `> 0`; group must **not** start `demo\` or `contest\`. Intents now walk **open** XAU (`!t.Completed`) plus close-follow of prior open keys. That is a **shadow-universe** shrink, **not** a `/api/traders` hide.

`RiskEngine.Evaluate` **is** called from `GenerateShadowIntentsAsync` L178. `allowSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–150). With `VenueReconciled=false` const, `AllowFixSend` from the engine is **false**. The service then **overwrites** persist to `false` anyway.

`PersistDemoShadowAsync` independently writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307) and **bypasses** `Evaluate`.

`BaselineScorer.CanPromoteToLive => false` (L211). `FromBaseline` reachable set is `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}` — never `LIVE` / `LIVE_CANDIDATE`.

`LiveCopyPage` is **live-wired** (`useCopyStatus` / `useCopyIntents` → `/api/copy/status` + `/api/copy/intents`). It is still read-only: no order POST. Summary string when blockers exist: “Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle.” Overview copy: “Live FIX NewOrderSingle is off — no capital at risk from this dashboard.” `/api/reconciliation/status` note: “NewOrderSingle still off”.

Flipping `.env` / JSON **cannot** place a live copy order: there is no `GuardedNewOrderSingle` on the copy hop, no QuickFIX initiator, no copy-path `35=D` encoder, no `ExecutionIntent` writer. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke on an armed flag. Still **no capital at risk** from this copy path.

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
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Shadow pipeline; `NewOrderSingleImplemented=false` (**320** lines after mid-slot 1:1 policy) |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | `AllocationFactor=1m`; eligible-trader filter; unused `FixOrderQtyUnits=lots×100` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 20 s SHADOW-intent tick |
| `D:\Prop\src\Application\Copy\CopyTradingModels.cs` | Gate DTO |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | `INSUFFICIENT_DATA` default |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction; no encoder |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135 lines) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Demo-gated `Build("D")` ×3 (391 lines) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon; does not clear RealCopy |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health, copy APIs (160 lines) |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses send even if flag true |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Demo 4-login scorer (not API live) |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | Only caller of demo `35=D` helper |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | All manager groups |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Honest “NewOrderSingle is off” copy |
| `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` | Live copy status/intents (read-only) |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | Same unfiltered `useTraders({})` |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders` / `useCopyStatus` |
| `D:\Prop\apps\web\src\api\client.ts` | axios 60 s, no paging |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` / unused-by-Prop `DealerSendOrder` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders (flag row stale) |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header + group names (logins not recopied) |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Live `/api/traders` = 8460 (flag / “no method” rows stale) |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_176.md` | Same list question; helper line-map now stale |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`. `CanPromoteToLive` is hardcoded false. Copy intents that exist are `SHADOW_ONLY` (or a dead-branch status string that still writes nothing to FIX). The only **hosted** cTrader write is a disposed Logon (`35=A`). **No loss of trading capital from slot-196 copy/dashboard behavior.**

The residual is **policy + a demo CLI + 1:1 shadow sizing**, not the copy wire: `.env` arms `REAL_COPY_EXECUTION_ENABLED=true` and DI honors it; settings advertise `FEATURE_COPY_TRADING_ENABLED=true`; `CTraderFixDemoTestTrade` can emit `35=D` **only** to a `demo-` host with a `demo.` sender and not the live account; shadow qty is now **1:1 lots** (`AllocationFactor=1m`). Do **not** treat any of those as a live copy send. Do **not** add a copy-path `35=D` builder while §68 / §70 are unpassed. Do **not** start `tools/DemoFixTestTrade` from this process. If a sender is ever added, 1:1 copy of the 8460-login book (or even the SHADOW subset) is dest-ruin — P500 already measured a large `RISK_BLOCKED` tail.

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Auto-score covers all 8460 logins (it covers logins **with deals**; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- `RealCopyEnabled` is still hard-forced false (it is not).
- A coded `MaySendNewOrderSingle` gate exists on an armed TRADE session (it does not; absence is the safety).
- Product tree has zero NewOrderSingle constructors (the demo helper exists; copy hop does not call it).
- This slot re-probed Manager or re-logged FIX (it did not).
- The P500 `8463` vs 08:42Z `8460` delta is explained (it is not).
- W176’s 349-line / L126 helper map is current (it is not).
- `AllocationFactor` is still `0.05m` (HEAD is `1m` via `XauUsdOneToOneCopyPolicy`; dest-ruin **if** sent).
- 1:1 shadow `RequestedQuantity` is a live Pepperstone ticket (it is not).
