# W500_RESEARCH_76 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_76.md` |
| Slot | **76** |
| Date | 2026-08-18 |
| Agent | W500 research 76 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only required write (plus catalog lines in `SWARM_LOG.md` / `INDEX.md`). |
| Test source modified | **No.** |
| Secrets printed | **None.** Password values, proxy auth, FIX password, and individual manager logins from the census JSON were not recopied. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (217 physical lines). Cross-read ingest, native Manager connector, live ingest host, store login lists, API maps, FIX logon session, DI, `LiveRuntimeStatus`, Traders/Groups/Overview/LiveCopy/Scoring pages, hooks/client, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `foreach (var account`, `35=D`, `(35, "D")`, `GuardedNewOrderSingle`, `DealerSend`, `Take(200)`, `ListLoginsWithDealsAsync`. Compared against stale `A005_dashboard_traders.md` and siblings `W500_RESEARCH_16.md` / `36.md` / `56.md`. Re-summed `LIVE_GROUPS_AND_TRADERS.json` group counts. Localhost HTTP blocked (SSRF). No live Manager re-attach. No product edit. |

**Honesty:** A005 (`foreach (var s in scores)` / unscored logins invisible) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-scoring is **deals-only** (`ListLoginsWithDealsAsync`); that does **not** hide cataloged logins on `/api/traders`. Siblings 16 / 36 / 56 still hold on current disk. Slot 16’s claim that `LiveIngestHostedService` scores every stored login is **stale vs current L106**. This slot did **not** re-probe Manager and did **not** SHA-256 the SUT (no shell in this worker). Completeness of the last census is the 08:42Z JSON + `CREDENTIALS_AND_COPY_STATUS` HTTP counts.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No.** Driver is `foreach (var account in accounts)` L99 | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores + PnL | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GroupRequestArray("*")` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Live HTTP census of that catalog | **18 groups / 8460 traders** (prior probe + `CREDENTIALS_AND_COPY_STATUS`) | `MEASURED` (not re-probed this slot) |
| Auto-score covers every cataloged login | **No.** Hosted service uses `ListLoginsWithDealsAsync` | `RESIDUAL` (list still complete) |
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Logon `35=A` only; `RealCopyEnabled` forced `false` | `SAFE_BY_ABSENCE` |
| Risk to live capital from this path | **None** from this process | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login (18 / 8460 last measure).
Auto-score = logins that have deals; unscored rows still list as INSUFFICIENT_DATA.
FIX copy = logon/recon only; no 35=D; RealCopyEnabled=false.
```

**Slot-76 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

---

## 1. Assigned question — scores-only vs all `Mt5Accounts`

Current method (`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L85–129), re-read in full this slot (217 physical lines; last method is `MaskLogin` L211–216):

1. Load **all** `TraderScores` (join payload, `AsNoTracking`) — **not** the driver.
2. Load **all** `Brokers` (dictionary by `Id`).
3. Load **all** `Mt5Accounts` — **this is the `foreach` driver** (L99).
4. Load completed `ReconstructedTrades` PnL grouped by `(BrokerId, Login)`.
5. For **each account**: skip only if `BrokerId` is missing from `Brokers`; `scoreMap.TryGetValue` / `pnlMap.TryGetValue` (left join); **always** `mapped.Add(...)`.
6. Optional in-memory `broker` / `state` filters (`OrdinalIgnoreCase` / `Enum.TryParse(..., true)`).
7. `OrderByDescending(EarlyScore)`. **No `Skip`/`Take` on the trader set.**

This-slot grep of `foreach (var account` in this file = **one** hit: L99. There is no `foreach (var s in scores)`.

The only `Take` in this file is `Take(20)` on risk-reject **reasons** (L204), not traders.

Missing score does **not** drop the row:

| Field | If `TraderScore` missing |
|---|---|
| `CompletedXauTrades` | `0` |
| `NetSourcePnl` | reconstructed PnL or `0` |
| `EarlyScore` / `RiskScore` | `0` |
| `MlProbability` | literal `null` (ML not built; 7th ctor arg) |
| Flags (`Martingale` / `AveragingDown` / `LotEscalation`) | `false` |
| `State` | `TraderState.INSUFFICIENT_DATA` (`D:\Prop\src\Domain\Enums\TraderState.cs` L5) |
| `ShadowPnl` | literal `0` (even when a score exists) |
| `LastScored` | `account.LastSyncedAt` |

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. `GetTraderDetailAsync` (L137–172) uses that header; A005’s “detail of unscored login returns null” is **false** on current disk.

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L95–96). `TradersPage` title is **“All manager traders”** and calls `useTraders({})` with **no** default `state` filter (`D:\Prop\apps\web\src\pages\TradersPage.tsx` L5–9). `ScoringPage` uses the same unfiltered hook (`ScoringPage.tsx` L4). `INSUFFICIENT_DATA` rows stay visible.

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
| `RealCopyEnabled` | `_runtime.RealCopyEnabled` (pinned false) |

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
| `D78` query size | 8708 B / 205 lines / SHA `328D0924…` | File is **217** lines now; that SHA is **stale** unless re-hashed |

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first (request API, not pump cache).
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981). YoPips is **cache-only** (`GroupTotal`/`GroupNext`). Prop C# prefers the request array, then the same pump walk.

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup`: `UserRequestArray` → fallback `UserGetByGroup` → if still empty `UserLogins` + `UserRequestByLogins`.
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

`GetGroupPositionsAsync` uses mask `"*"` when blank (L57–58). Ingest uses that bulk path.

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` (L54–98) repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter.

`UpsertGroupsBatchAsync` persists **every** incoming group. New groups get `EnabledForAnalysis = true` (L376). Account upserts flush in batches of 500 for memory, not a census cap. Dashboard `GetGroupsAsync` (L70–82) is `Mt5Groups.ToListAsync` — displays those flags, does **not** filter on them.

`LiveMt5Registration.CreateConnectors` registers **exactly two** native connectors: `BrokerCodes.Achiever` (`ACHIEVER`) and `BrokerCodes.StarwaveFx` (`STARWAVEFX`). Starwave `ProxyEnabled = false` (hard pin; env unread). DI **throws** if either real Manager password is absent (`DependencyInjection.cs` L35–36). `FakeMt5BrokerConnector` is **not** registered on the API graph. Dummy seed is **not** on API startup (`Program.cs` L149–154 only `EnsureCreated` + `BrokerCatalogSeed`).

`GroupsPage` copy: “Every group visible to the Achiever and Starwave managers.”

This-slot `Take(200)` grep under `D:\Prop` `*.cs` = **one** site: `GET /api/trades` reconstructed explorer (`Program.cs` L107). Not the trader census. `Take(` under `D:\Prop\src` `*.cs` = `EfDashboardQueries` risk reasons **20** + `FixMessageParser` checksum slice.

### 2.2 Measured live census (prior probe; passwords and logins not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`, probe `LiveBrokerProbe`, `envLoaded=true`). `CREDENTIALS_AND_COPY_STATUS.md` independently recorded live dashboard `/api/traders` = **8460** and `/api/groups` = **18**.

This slot **re-summed** the JSON `groupNames[].accounts` (logins not recopied):

| Broker | Connect | Groups | Traders (JSON `accounts`) | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy (`elapsedMs` 7212.6) | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever (re-sum `2+179+4+5+4+6295+0+23 = 6512`): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave (re-sum `11+4+170+1735+22+0+0+4+0+2 = 1948`): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good). This slot did **not** re-run the live probe. Localhost `/ready` / `/api/health` fetch was **blocked** (SSRF policy).

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
| `NewOrderSingle` in product `.cs` | comments / logs / `MayRetryNewOrderSingle` (pure status helper, `ExecutionOrderStateMachine.cs` L35) |

YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling (`mt5_manager.cpp` L1119+); **Prop C# does not call it.**

`CTraderFixLogonHostedService` logons with tag **553 = integer account id** (not SenderCompID), then **forces** `_runtime.RealCopyEnabled = false` (L68). Log: `"NewOrderSingle still disabled"`.

### 3.2 Flag cannot arm a missing sender

| Surface | Value |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” (`DependencyInjection.cs` L38–41) |
| FIX hosted service | sets `false` again after logon (L68) |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED` hardcoded `false` (`Program.cs` L73–76) |
| `GET /api/health` | `realCopyEnabled = runtime.RealCopyEnabled` (L54) |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` (L52) |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` (L195) |
| `GetRiskAsync` 7th arg | literal `false` → `RealCopyEnabled` (L208) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` |
| `apps/fix-worker/Worker.cs` | even if config true, **refuses** send; stamps TRADE `Disconnected` + “NewOrderSingle remains off” (L41, L46) |
| `ShadowCopyEngine` | in-process fill math only. No venue write |
| `PersistDemoShadowAsync` | writes `CopyIntent.Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307) |
| `RiskEngine.allowSend` | requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`; there is still **no** encoder behind it |
| `LiveCopyPage` | static SHADOW warning; no order POST |
| `OverviewPage` | “Live FIX NewOrderSingle is off — no capital at risk from this dashboard.” |
| `LiveRuntimeStatus.Snapshot` | copyNote when false: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |
| `/api/reconciliation/status` | note: “NewOrderSingle still off” |

Flipping a JSON/env flag **cannot** place a live order: there is no `GuardedNewOrderSingle`, no QuickFIX initiator, no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines; L85–129) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog all; score deals-only |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` vs `ListLoginsWithDealsAsync`; SHADOW_ONLY |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real passwords required; `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | `INSUFFICIENT_DATA` default |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135 lines) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon then force copy off |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow math, no send |
| `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` | `MayRetryNewOrderSingle` helper only |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction; no encoder |
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

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`; `GetFixSessionsAsync.ExecutionEnabled` stays `false`; overview `RealCopyEnabled` is the runtime flag that DI and the FIX host pin **false**. Fetch + score + dashboard paint are read/upsert paths. Copy intents that exist are `SHADOW_ONLY`. **No loss of trading capital from slot-76 behavior.**

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Auto-score covers all 8460 logins (it covers logins **with deals**; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- A coded `MaySendNewOrderSingle` gate exists (it does not; absence is the safety).
- This slot re-attached to Manager, hit localhost HTTP, or recomputed SHA-256 (it did not; completeness of last census is the 08:42Z JSON + `CREDENTIALS_AND_COPY_STATUS` HTTP counts).
