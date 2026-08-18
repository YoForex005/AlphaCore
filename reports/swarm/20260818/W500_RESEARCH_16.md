# W500_RESEARCH_16 — `GetTradersAsync`: scores-only vs all `Mt5Accounts`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_16.md` |
| Slot | **16** |
| Date | 2026-08-18 |
| Agent | W500 research 16 (senior engineer) |
| Topic | Check `EfDashboardQueries.GetTradersAsync` — only `TraderScores` vs **all** `Mt5Accounts`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **None.** Passwords, proxy auth, FIX password not copied. |
| Method | Full `read_file` of current `EfDashboardQueries.cs` (217 lines). Cross-read ingest, native Manager connector, API maps, FIX logon session, DI, `LiveRuntimeStatus`, `TradersPage`, YoPips `MT5Manager::GetAllGroups`. Grep `GetTradersAsync`, `35=D`, `NewOrderSingle`, `DealerSend`. Compared against stale `A005_dashboard_traders.md` and measured census `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json`. No live HTTP this slot. No product edit. |

**Honesty:** A005 (scores-as-driver) is **stale**. On-disk `GetTradersAsync` is **account-driven**. Overview **state tiles** are still score-counted. That is a metric split, not a hide of the trader list.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| `GetTradersAsync` lists **only** `TraderScores` | **No (stale).** Driver is `Mt5Accounts` | `FIXED_ON_DISK` vs A005 |
| `GetTradersAsync` lists **all** persisted `Mt5Accounts` | **Yes**, left-join scores | `EXISTS_AND_GOOD` for completeness |
| Unscored login hidden on `/api/traders` | **No.** Row emitted with zeros + `INSUFFICIENT_DATA` | `EXISTS_AND_GOOD` |
| Fetch path walks **all** manager groups + users | **Yes** (`GetGroupsAsync` `*` / `GetAccountsAsync(null)`) | `EXISTS_AND_GOOD` |
| Dashboard `/api/groups` is all `Mt5Groups` | **Yes.** No plan / `EnabledForAnalysis` WHERE | `EXISTS_AND_GOOD` |
| Copy-to-cTrader emits `NewOrderSingle` / `35=D` | **No.** Logon `35=A` only; `RealCopyEnabled` forced `false` | `SAFE_BY_ABSENCE` |
| Risk to live capital from this path | **None** from this process | `NO_LOSS` |

One-line:

```text
GetTradersAsync = ALL Mt5Accounts LEFT JOIN TraderScores (not scores-only).
Catalog = every Manager-visible Achiever+Starwave group/login.
FIX copy = logon/recon only; no 35=D; RealCopyEnabled=false.
```

**Slot-16 verdict:** `PASS_ALL_ACCOUNTS_NO_LIVE_SEND`

---

## 1. Assigned question — scores-only vs all `Mt5Accounts`

Current method (`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L85–129):

1. Load **all** `TraderScores` (join payload).
2. Load **all** `Brokers` (dictionary by `Id`).
3. Load **all** `Mt5Accounts` — **this is the foreach driver**.
4. Load completed `ReconstructedTrades` PnL grouped by `(BrokerId, Login)`.
5. For **each account**: skip only if `BrokerId` is missing from `Brokers`; `scoreMap.TryGetValue` / `pnlMap.TryGetValue` (left join); emit a `TraderRowDto`.
6. Optional in-memory `broker` / `state` filters.
7. `OrderByDescending(EarlyScore)`. **No `Skip`/`Take`.** `src/` has **0** `Take(200)` hits.

Missing score does **not** drop the row:

| Field | If `TraderScore` missing |
|---|---|
| `CompletedXauTrades` | `0` |
| `NetSourcePnl` | reconstructed PnL or `0` |
| `EarlyScore` / `RiskScore` | `0` |
| `MlProbability` | literal `null` (ML not built) |
| Flags | `false` |
| `State` | `TraderState.INSUFFICIENT_DATA` |
| `ShadowPnl` | literal `0` (even when a score exists) |
| `LastScored` | `account.LastSyncedAt` |

`GetTraderAsync` (L131–135) reloads `GetTradersAsync(broker, null)` then `FirstOrDefault` by login. An ingested unscored login is **findable**. (A005’s “detail of unscored login returns null” is **false** on current disk.)

`GET /api/traders` is a passthrough (`Program.cs` L95–96). `TradersPage` title is **“All manager traders”** and calls `useTraders({})` with **no** default `state` filter, so `INSUFFICIENT_DATA` rows stay visible.

### 1.1 What is still scores-only (not the trader list)

`GetOverviewAsync` L22–42:

| Card | Source |
|---|---|
| `TotalAccounts` | `Mt5Accounts.CountAsync` — **all accounts** |
| `XauTraders` / `TradersWithThreeTrades` | `TraderScores` counts |
| `Watch` / `Shadow` / `LiveCandidates` / `Live` / `RiskBlocked` | `TraderScores.CurrentState` only |

Until scoring upserts a row, state tiles **under-count**. The `/traders` table still lists the cataloged login. That is the residual A005 intuition that remains true **only** for overview buckets.

Orphan `TraderScores` (score without an `Mt5Account`) are **invisible** on the leaderboard. That is correct for “all manager traders”: the census is the account book.

Accounts whose `BrokerId` is not in `Brokers` are skipped (`continue`). Catalog seed writes `ACHIEVER` + `STARWAVEFX` (`BrokerCatalogSeed.EnsureAsync`); live ingest resolves those same codes. Not a hide of Manager users.

### 1.2 Stale report to ignore

`A005_dashboard_traders.md` §0 / §2.2 said the driver was `foreach (var s in scores)` and unscored logins were invisible. **Current body is `foreach (var account in accounts)` + `scoreMap.TryGetValue`.** W500_SLICE_126 already recorded this flip. Slot 16 re-read the live file and confirms A005 is **STALE**.

---

## 2. Goal — fetch ALL Achiever + Starwave groups and ALL manager traders

### 2.1 Connector (every group the manager can see)

`NativeMt5BrokerConnector.GetGroupsCore` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L144–186):

- `GroupRequestArray("*", arr)` first.
- If empty: fallback `GroupTotal()` + `GroupNext(i)` — same walk as YoPips `MT5Manager::GetAllGroups` (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L962–981).

`GetAccountsCore(null)` (L189–213):

- `group` null/blank → iterate **every** name from `GetGroupsCore()`.
- Per group `ReadAccountsForGroup`: `UserRequestArray` → fallback `UserGetByGroup` → if still empty `UserLogins` + `UserRequestByLogins`.
- Dedup by login into `Dictionary<ulong, Mt5AccountDto>`. **No `Take`.**

`DealIngestionService.SyncCatalogAsync` (L37–50): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. `SyncBrokerAsync` repeats the same unfiltered catalog before deals/positions. Bulk deals are `foreach (var group in groups)` with **no** `EnabledForAnalysis` / `PlanMapping` filter.

`EfTradingStore.ListLoginsAsync` = **all** `Mt5Accounts.Login` for the broker.

`LiveIngestHostedService` + `POST /api/ops/resync` score **every** stored login for `ACHIEVER` and `STARWAVEFX`. Dummy seed is **not** on API startup (`Program.cs` only `EnsureCreated` + `BrokerCatalogSeed`). DI **throws** if both real Manager passwords are absent (`DependencyInjection.cs` L35–36). Fake 4-login tape is not the live graph.

`GetGroupsAsync` dashboard (L70–82): `Mt5Groups.ToListAsync` — **all** persisted groups. Displays `EnabledForAnalysis` / `PlanMapping` but does **not** filter on them.

### 2.2 Measured live census (prior probe; passwords not reprinted)

`LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16Z`, probe `LiveBrokerProbe`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2.

Honesty: these are **all groups this Manager login is allowed to see**, not a claim that the server has no hidden ACL groups. Zero-account groups are still cataloged (good).

This slot did **not** re-run the live probe. Completeness of the **code path** is proven on disk; completeness of the **last census** is the 08:42Z JSON.

### 2.3 Scale residual (does not flip completeness)

`GetTradersAsync` materializes four full sets in process. At 8460 logins the **set is correct** and **unpaged**. UI is a single `<table>` + axios 15s timeout. That is `UNSAFE` as a 5k/8k UX (C36/D95), **not** a scores-only hide.

During `Phase=scoring`, cataloged logins already appear as `INSUFFICIENT_DATA`. Fail-fast: one `RebuildTraderAsync` throw stops remaining scores; leftover accounts stay visible.

`/api/trades` still `Take(200)` reconstructed trades — trade tape, not the trader census.

---

## 3. Goal — copy to cTrader must not send live orders (no loss)

### 3.1 No `35=D` builder

Product FIX send sites (`src/Fix.CTrader`):

| Site | MsgType | Socket? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | **`35=A` Logon** | Yes — then **read one reply and dispose** TCP/SSL |
| `FixSimulationHarness` | `A` / `8` / `y` / `X` / `0` / `3` | In-memory only |

Grep `(35, "D")` / `35=D` under `src/Fix.CTrader` = **0**.  
`NewOrderSingle` hits are comments / logs / `MayRetryNewOrderSingle` (pure status helper).  
YoPips `MT5Manager::DealerSendOrder` exists in the C++ sibling; **Prop C# does not call it.**

`CTraderFixLogonHostedService` logons QUOTE:5211 and TRADE:5212 with tag **553 = integer account id**, then **forces** `_runtime.RealCopyEnabled = false` (L68). Session objects are disposed after the logon read — no keep-alive TRADE initiator, no order send.

### 3.2 Flag cannot arm a missing sender

| Surface | Value |
|---|---|
| `AddTraderIntelligence` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” |
| FIX hosted service | sets `false` again after logon |
| `GET /api/settings` | `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` |
| `GET /api/health` | `realCopyEnabled = runtime.RealCopyEnabled` |
| `GetOverviewAsync` last arg | `_runtime.RealCopyEnabled` |
| `GetFixSessionsAsync` last arg | literal `false` → `ExecutionEnabled` |
| `GetRiskAsync` 7th arg | literal `false` → `RealCopyEnabled` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` |
| `FEATURE_COPY_TRADING_ENABLED` | hardcoded `false` in settings |

`ShadowCopyEngine` is in-process fill math only. No venue write.

Flipping a JSON/env flag **cannot** place a live order: there is no `GuardedNewOrderSingle`, no QuickFIX initiator, no `35=D` encoder. Safety is **SAFE_BY_ABSENCE**, not a unit-tested choke. Still **no capital at risk** from this copy path.

---

## 4. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Assigned SUT (217 lines) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | DTO / `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Unfiltered catalog + score upsert |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Live all-login score loop |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ListLoginsAsync` = all accounts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real passwords required; `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native Achiever + Starwave connectors |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `*` groups + all users |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Demo tape (not registered when live) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon then force copy off |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow math, no send |
| `D:\Prop\apps\api\Program.cs` | Routes, resync all logins, health |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered all-traders table |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders({})` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Sibling `GetAllGroups` / `DealerSendOrder` (unused here) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | 8+10 groups / 6512+1948 traders |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Probe header (logins not recopied) |
| `D:\Prop\reports\swarm\20260818\A005_dashboard_traders.md` | Stale scores-only claim |

---

## 5. No-loss implication

Listing every Manager login on `/api/traders` does **not** promote anyone to LIVE send. Rows can be `INSUFFICIENT_DATA` / `WATCH` / `SHADOW` / `RISK_BLOCKED`; `GetFixSessionsAsync.ExecutionEnabled` stays `false`; overview `RealCopyEnabled` is the runtime flag that DI and the FIX host pin **false**. Fetch + score + dashboard paint are read/upsert paths. **No loss of trading capital from slot-16 behavior.**

---

## 6. Do not claim

- EX5 decompiled / ≥95% copy-trading live.
- Overview state tiles equal `Mt5Accounts` count (they do not).
- 8460-row unpaged table is a production UX (it is complete, not scaled).
- Manager ACL-hidden server groups are included (they cannot be).
- A coded `MaySendNewOrderSingle` gate exists (it does not; absence is the safety).
