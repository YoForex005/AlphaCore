# W500_RESEARCH_56 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_56.md` |
| Slot | **56** |
| Date | 2026-08-18 |
| Agent | W500 research 56 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only required write. |
| Test source modified | **No.** |
| Secrets printed | **None.** Passwords, proxy auth, FIX password not copied. Manager logins in the prior probe JSON are not recopied. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (217 lines). Cross-read DTOs, ingest, native Manager connector, live ingest host, API maps, FIX logon, DI, runtime flag, Traders/Groups pages, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `35=D`, `NewOrderSingle`, `DealerSend`, `Take(`, `ListLogins*`. Compared against stale `A005_dashboard_traders.md` and prior census `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json`. No live HTTP / no re-probe this slot. |

**Honesty:** A005 (“driver is `TraderScores`; unscored logins invisible”) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. Auto-ingest **scores** only logins that already have deals; the **list** still emits every persisted `Mt5Account`. That is a scoring-coverage split, not a hide of the trader book.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No (stale).** Driver is `Mt5Accounts` | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GroupRequestArray("*")` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Hosted scoring covers every cataloged login | **No.** `ListLoginsWithDealsAsync` only | `EXISTS_NEEDS_REFACTOR` (score freshness, not list hide) |
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Logon `35=A` only; `RealCopyEnabled` forced `false` | `SAFE_BY_ABSENCE` |
| Risk to live capital from this path | **None** from this process | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login.
Hosted score = logins-with-deals only; list still shows the rest as INSUFFICIENT_DATA.
FIX copy = logon/recon only; no 35=D; RealCopyEnabled=false.
```

**Slot-56 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

---

## 1. Assigned question — scores-only vs all `Mt5Accounts`

Current method (`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L85–129), re-read in full this slot (217 physical lines):

1. Load **all** `TraderScores` (join payload, `AsNoTracking`).
2. Load **all** `Brokers` (dictionary by `Id`).
3. Load **all** `Mt5Accounts` — **this is the foreach driver**.
4. Load completed `ReconstructedTrades` PnL grouped by `(BrokerId, Login)`.
5. For **each account**: skip only if `BrokerId` is missing from `Brokers`; `scoreMap.TryGetValue` / `pnlMap.TryGetValue` (left join); emit a `TraderRowDto`.
6. Optional in-memory `broker` / `state` filters (`OrdinalIgnoreCase` / `Enum.TryParse`).
7. `OrderByDescending(EarlyScore)`. **No `Skip`/`Take` on the trader set.**

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

`GET /api/traders` is a passthrough (`D:\Prop\apps\api\Program.cs` L95–96). `TradersPage` title is **“All manager traders”** and calls `useTraders({})` with **no** default `state` filter, so `INSUFFICIENT_DATA` rows stay visible.

Axios timeout is **60 000 ms** (`D:\Prop\apps\web\src\api\client.ts` L5). A005/W500_16 “15s” is **stale**.

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

### 1.2 Stale reports to ignore on this question

| File | Stale claim | Current disk |
|---|---|---|
| `A005_dashboard_traders.md` §0 | Driver is `foreach` scores; unscored invisible | `foreach (var account in accounts)` + `scoreMap.TryGetValue` |
| A005 `Take(200)` on positions | `accounts.Take(200)` in ingest | **0** `Take(` in `D:\Prop\src`; positions via `GetGroupPositionsAsync("*")` |
| A005 health FakeMt5 | hardcoded FakeMt5 string | `/api/health` reports live Manager `groups=` / `accounts=` / `phase=` |
| A005 / W500_16 axios 15s | `timeout: 15000` | `timeout: 60000` |
| W500_16 “live ingest scores every stored login” | `ListLoginsAsync` | Hosted path is now `ListLoginsWithDealsAsync` |

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first.
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981: cache/pump `GroupTotal` + `GroupNext`; **no name filter**).

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup`: `UserRequestArray` → fallback `UserGetByGroup` → if still empty `UserLogins` + `UserRequestByLogins`.
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

`DealIngestionService.SyncCatalogAsync` (L37–50): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter. Positions: `GetGroupPositionsAsync("*")` when the connector implements `IMt5BulkPositionReader`.

`EfTradingStore.ListLoginsAsync` = **all** `Mt5Accounts.Login` for the broker (L339–341).

`EfTradingStore.ListLoginsWithDealsAsync` = **distinct** `Mt5Deals.Login` for the broker (L343–345).

`LiveIngestHostedService` catalog: both registered connectors (`ACHIEVER` + `STARWAVEFX` via `LiveMt5Registration.CreateConnectors`). Dummy seed is **not** on API startup (`Program.cs` only `EnsureCreated` + `BrokerCatalogSeed`). DI **throws** if both real Manager passwords are absent (`DependencyInjection.cs` L35–36). Fake 4-login tape is not the live graph.

`GetGroupsAsync` dashboard (L70–82): `Mt5Groups.ToListAsync` — **all** persisted groups. Displays `EnabledForAnalysis` / `PlanMapping` but does **not** filter on them. `GroupsPage` subtitle: “Every group visible to the Achiever and Starwave managers.”

`EnabledForAnalysis` is written **true** on upsert (`EfTradingStore` L39 / L376). It is a display flag, not a fetch gate.

### 2.2 Scoring coverage (does not hide the list)

| Path | Login set scored |
|---|---|
| `LiveIngestHostedService` L106–113 | `ListLoginsWithDealsAsync` — **deals-only** |
| `POST /api/ops/resync` L131–136 | `ListLoginsAsync` — **all persisted accounts** |
| `apps/mt5-worker/Worker.cs` L31–35 | leftover `{10001, 10002, 10003, 99001}` only — **not** the API live path |

During `Phase=scoring`, cataloged logins **already appear** as `INSUFFICIENT_DATA` because `GetTradersAsync` walks `Mt5Accounts`. Fail-fast: one `RebuildTraderAsync` throw stops remaining scores; leftover accounts stay visible.

This is the measured split vs “fetch ALL manager traders”:

- **Catalog + dashboard list:** all Manager-visible groups/users that persist as `Mt5Accounts`.
- **Automatic quality scores:** only logins that have at least one ingested deal.

That does **not** flip the assigned question. The assigned question is the **traders query**, not the scorer loop.

### 2.3 Measured live census (prior probe; passwords not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`, probe `LiveBrokerProbe`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. Sum **6512**.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2. Sum **1948**.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good).

This slot did **not** re-run the live probe. Completeness of the **code path** is proven on disk; completeness of the **last census** is the 08:42Z JSON.

### 2.4 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios 60s timeout. That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

`/api/trades` still `Take(200)` reconstructed trades (`Program.cs` L107) — trade tape, not the trader census. `grep Take(200)` under `D:\Prop\src` = **0**.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 No `35=D` builder

Product FIX send sites (`src/Fix.CTrader`):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL |
| `FixSimulationHarness` | comments / in-memory only | No live socket from this research path |

Grep `35=D` / `(35, "D")` under `D:\Prop\src` = **0**.  
`NewOrderSingle` hits are comments / logs / `MayRetryNewOrderSingle` (pure status helper).  
Grep `DealerSend` under `D:\Prop\src` = **0**. YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling; **Prop C# does not call it.**

`CTraderFixLogonHostedService` logons QUOTE:5211 and TRADE:5212 with tag **553 = integer account id**, then **forces** `_runtime.RealCopyEnabled = false` (L68). Session objects are disposed after the logon read — no keep-alive TRADE initiator, no order send.

`apps/fix-worker/Worker.cs` stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` and, even if `CTrader:RealCopyExecutionEnabled` is true, **still refuses** to send (L45–46 warning only).

`RiskEngine` can set `AllowFixSend` (L147–170) but there is **no** `GuardedNewOrderSingle` / FIX encoder consumer. A theoretical `Approve` cannot leave the process as a venue order.

### 3.2 Flag cannot arm a missing sender

| Surface | Value |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” |
| FIX hosted service | sets `false` again after logon |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded `false` in settings |
| `GET /api/health` | `realCopyEnabled = runtime.RealCopyEnabled` |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` |
| `GetRiskAsync` 7th arg | literal `false` → `RealCopyEnabled` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` |
| `LiveRuntimeStatus.Snapshot().copyNote` when false | “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |

`ShadowCopyEngine` is in-process fill math only. No venue write.

Flipping a JSON/env flag **cannot** place a live order: there is no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Live catalog + deals-only score loop |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` vs `ListLoginsWithDealsAsync` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real passwords required; `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | ACHIEVER + STARWAVEFX catalog rows |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon then force copy off |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow math, no send |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` with no encoder |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | `GetAccountsAsync(string? group)` |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health |
| `D:\Prop\apps\fix-worker\Worker.cs` | Refuses NewOrderSingle |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer (not API) |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | All groups table |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | Honest “NewOrderSingle is off” copy |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders({})` |
| `D:\Prop\apps\web\src\api\client.ts` | 60s timeout |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` (unused sender here) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header + group counts (logins not recopied) |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |
| `D:\Prop\reports\swarm\20260818\W500_RESEARCH_16.md` | Prior same-question slot; scoring-loop claim now stale |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`; `GetFixSessionsAsync.ExecutionEnabled` stays `false`; overview `RealCopyEnabled` is the runtime flag that DI and the FIX host pin **false**. Fetch + score + dashboard paint are read/upsert paths. **No loss of trading capital from slot-56 behavior.**

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- Hosted scoring already covers all 8460 logins (it covers logins-with-deals; resync covers all).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- This slot re-measured the live Manager attach (it did not; census is 08:42Z).
- A coded `MaySendNewOrderSingle` gate exists (it does not; absence is the safety).
