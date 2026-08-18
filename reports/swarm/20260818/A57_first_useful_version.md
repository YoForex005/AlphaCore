# A57 — Architecture §69 first useful version (12 items)

| Field | Value |
|---|---|
| Agent | A57 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A57_first_useful_version.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §69 (lines 2633–2654) |
| Product source modified | **No** |
| Method | Disk inventory of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk` plus architecture §§6–18, 22–32, 45–54, 66–73. Cross-checked against swarm A02–A07, A24–A28. |

**Bar (verbatim §69):** the first genuinely useful system does **not** need ML. It is accepted only when **all 12 items are true**. Phase 6/7/8 (ML, TRADE send, live copy) are **out of this bar**. Live `NewOrderSingle` stays off (`REAL_COPY_EXECUTION_ENABLED=false`).

---

## Executive verdict

**Accepted: 0 / 12.**  
**Honest measured state: Domain algorithms are starting to appear; the running system does not exist.**

Do not treat entity records, an in-memory reconstructor/scorer, or a Vite route table as a first useful version. §69 is an **end-to-end operating bar**: both brokers connected, groups and ~5k accounts synchronized, XAUUSD deals captured, logical trades reconstructed, first-3 detected, deterministic scores + ranking persisted, QUOTE FIX logged on, Pepperstone XAU instrument **discovered** (not guessed), selected traders shadow-copied on destination quotes, and all of that shown in React.

| # | §69 item | Phase | Classification | Accepted? |
|---|---|---|---|---|
| 1 | Connect to both MT5 brokers | 1 | C++ **EXISTS_NEEDS_REFACTOR** (one broker); C# connector **MISSING** impl | **No** |
| 2 | Discover all groups | 1 | C++ group APIs exist; collector persist **MISSING** | **No** |
| 3 | Synchronize ~5,000 accounts | 1 | C++ user/account reads exist; checkpointed sync **MISSING** | **No** |
| 4 | Capture XAUUSD trades correctly | 1–2 | C++ `GetDeals` + ledger helper; C# ingest/XAU filter **MISSING** | **No** |
| 5 | Reconstruct logical trades | 2 | In-memory `TradeReconstructor` **EXISTS_NEEDS_REFACTOR**; persist/worker **MISSING** | **No** |
| 6 | Detect first 3 completed XAUUSD trades | 2 | Counter methods exist on reconstructor; durable first-3 **MISSING** | **No** |
| 7 | Produce a deterministic trader/risk score | 3 | `BaselineScorer` **EXISTS_NEEDS_REFACTOR**; no persist/job/tests | **No** |
| 8 | Rank traders | 3 | **MISSING** ranking query/API | **No** |
| 9 | Connect to cTrader QUOTE FIX securely | 4 | Options + parser/harness only; no QuickFIX/n session | **No** |
| 10 | Discover Pepperstone XAUUSD instrument ID | 4 | **MISSING** (harness hardcodes `123456` — forbidden) | **No** |
| 11 | Shadow-copy selected traders using destination quotes | 5 | In-memory `ShadowCopyEngine` stub; no dest quotes, no persist | **No** |
| 12 | Show all of this in React | 3–5 | Vite shell + routes to **missing pages**; API is weatherforecast | **No** |

Phase 0–5 sign-off boxes from A28 remain unchecked. ML is correctly **not** required.

---

## Cross-cutting repo facts (apply to every item)

These are not §69 items, but they block every item from becoming true.

### What exists (measured)

| Surface | Path | What is actually there |
|---|---|---|
| Domain entities + enums | `D:\Prop\src\Domain\Entities`, `Enums` | Records/classes for brokers, groups, accounts, deals, positions, reconstructed trades, scores, copy/shadow/FIX/risk/outbox/checkpoint. Enums match architecture states (`TraderState`, `OutboxEventType`, `DealAction`/`DealEntry` aligned to MT5 SDK). |
| Domain algorithms | `Reconstruction\`, `Scoring\`, `Risk\`, `Shadow\`, `Execution\`, `Instruments\`, `Volume\` | In-memory `TradeReconstructor`, `BaselineScorer` + `TraderStateMachine`, `RiskEngine`, `ShadowCopyEngine`, `SymbolNormalizer`, `VolumeConverter`, `QuantityNormalizer`, `ClOrdIdFactory`, `ExecutionOrderStateMachine`. |
| Application | `D:\Prop\src\Application\Class1.cs` | Empty `dotnet new` leftover. FluentValidation referenced, unused. **No ports, no use-cases.** |
| Infrastructure | `TraderDbContext` + 4 configurations | **Does not match Domain types.** DbSets and configs name plural types (`Brokers`, `Mt5Groups`, …) that **do not exist**. Entity records are singular (`Broker`, `Mt5Group`, …). Remaining 16 `*Configuration` types referenced in `OnModelCreating` are **absent**. **This project will not compile.** No EF migrations. |
| C# MT5 | `D:\Prop\src\Mt5` | `IBrokerConnector` + `Mt5BrokerOptions` + `DeterministicGuid`. **No implementation.** No HTTP client, no P/Invoke, no broker registry. |
| C# FIX | `D:\Prop\src\Fix.CTrader` | `CTraderFixOptions`, pipe-delimited `FixMessageParser`, `FixSimulationHarness`. Package `QuickFix.Net 1.11.2` (not the architecture pin `QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1). **No initiator, no SSL session, no dictionary.** |
| API | `D:\Prop\apps\api\Program.cs` | Stock `GET /weatherforecast`. No auth, no `/api/v1`, no DbContext, no SignalR hub. |
| MT5 worker | `D:\Prop\apps\mt5-worker\Worker.cs` | 1 Hz `LogInformation`. Empty `appsettings.json`. |
| FIX worker | `D:\Prop\apps\fix-worker\Worker.cs` | Same 1 Hz template. |
| React | `D:\Prop\apps\web` | Vite + React 18 + Router + TanStack Query + axios + SignalR client + Tailwind. `App.tsx` routes 14 pages. **`src/pages` has 0 files.** Layout/hooks/types exist. |
| Tests | `D:\Prop\tests\Unit`, `Integration` | Projects only. **No test classes.** A27 inventory (77 classes) is a backlog, not code. |
| C++ SDK | `D:\Prop\mt5-sdk` | Real Manager API wrapper (`MT5Manager` / `MT5Pool` / `IMT5Client`) plus optional HTTP client. **Single-broker `AppConfig` (`MT5_*` only). No `broker_id`. No `MT5_STARWAVEFX_*`.** Ledger helper is not the §11 table set. |

### Identity and compile hazards (fix before wiring)

1. **Guid vs string `broker_id`.** Entities use `Guid BrokerId`. `TradeReconstructor` / `BaselineScorer` / `ShadowCopyEngine` use `string BrokerId`. Pick one (Guid + stable `code` like `achiever` / `starwavefx`) and map at the Application boundary.
2. **Record vs class.** Older entities are positional `record`s (`Broker`, `Mt5Deal`, `ReconstructedTrade`). Newer ones are mutable classes (`CopyIntent`, `OutboxEvent`). EF configurations must bind to the **actual** types, not invented plurals.
3. **Volume scale.** `VolumeConverter` documents Manager `Volume()/10_000` vs `VolumeExt()/1e8`. Persist native `ulong` + scale used. Do not store lots in a `bigint` column named `volume` (current `Mt5DealsConfiguration` does that).
4. **Secrets.** `Mt5BrokerOptions.Password` and `CTraderFixOptions.Password` exist as properties. Never put values in repo, logs, or React. `CTraderFixOptions.TargetCompId` defaults to `"CSERVER"` — §26 forbids silent case change from issued `cServer`.
5. **TRADE session default.** `TradeSessionEnabled = true` on options. First useful version needs **QUOTE only**. TRADE logon may exist later; `RealCopyExecutionEnabled` is correctly `false`. Keep TRADE dark.

### What not to build yet (§71 / first-useful)

Kafka, Kubernetes, ClickHouse, LLM, deep learning, microservice mesh, live `NewOrderSingle`, ML promote APIs, emergency flatten from React, guessed cTrader tag 55, schema edits without migrations.

---

## Item 1 — Connect to both MT5 brokers

**Phase:** 1. **Architecture:** §§6–8, 10. **Suggested proof:** both stay connected across reconnect; independent `broker_id`.

### Current status — **FAIL / not connected**

**C++ (adjacent, not the C# collector):**

- `IMT5Client` + `MT5Manager::Connect(server, login, password, pumpMode)` and `MT5Pool` request-only sessions exist (`D:\Prop\mt5-sdk\src\core\imt5_client.h`, `mt5_manager.h`, `mt5_pool.h`).
- Watchdog/reconnect exists (`mt5_watchdog.h`).
- `AppConfig` (`mt5-sdk\config\app_config.h`) is **one** broker: `mt5_server`, `mt5_login`, `mt5_password`, `mt5_pool_size`, proxy. **No StarwaveFX fields.**
- C++ DTOs have **no `broker_id`**. Login/ticket uniqueness is assumed global — violates §10 if a second broker is added naively.

**C# product tree:**

- Architecture sketch `IMt5BrokerConnector` is **not** in Application.
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` is a close cousin (Connect/Disconnect/GetGroups/GetAccounts/GetDeals/GetPositions/SubscribeEvents). **Zero implementations.**
- `Mt5BrokerOptions` documents Achiever-shaped fields + `RemoteUrl` but nothing binds two named brokers.
- `apps/mt5-worker` does not call Connect. No `MT5_*` / `MT5_STARWAVEFX_*` in worker `appsettings.json`.

A successful `dotnet build` of `TraderIntelligence.Mt5Worker` only proves the template compiles.

### What to implement

1. **Application port** `IMt5BrokerConnector` (keep `IBrokerConnector` or merge — do not keep two unused interfaces). Members: `ConnectAsync`, `DisconnectAsync`, `IsConnected`, `GetGroupsAsync`, `GetAccountsAsync`, `GetDealsAsync`, `GetOrdersAsync`, `GetPositionsAsync`, `SubscribeAsync`, `GetServerTimeAsync`. Every DTO carries `BrokerId`.
2. **`IMt5BrokerRegistry`** keyed by `broker_id` / code. Register **Achiever** and **StarwaveFX** as two instances of the **same** implementation.
3. **One implementation** wrapping the existing C++ surface:
   - Preferred for Windows: reuse `MT5Manager`/`MT5Pool` via a small native host or the existing HTTP remote mode (`MT5HttpClient` / `mt5_mode=remote`) so C# does not fork a second SDK.
   - Do **not** copy dealer/create-user/SendTrade onto the source collector (A04). Read + subscribe only.
4. **Config (secrets in env/user-secrets, not git):**
   - Achiever: `57.128.141.65:443`, manager `2027`, pool 8, optional proxy, egress note `81.29.145.69`.
   - StarwaveFX: `84.201.6.142:443`, manager `9904`, pool 4, proxy-designable.
5. **Worker hosted service** `Mt5ConnectionHostedService`: connect both at startup, expose health (`mt5_connected{broker}`), reconnect with backoff, never log passwords/proxy secrets.
6. **Tests:** `Mt5.DualBrokerIsolationTests` (A27) — Achiever login `1001` and StarwaveFX login `1001` never collide.

**Done when:** process logs successful Connect for **both** broker_ids after restart; health shows two independent sessions; a killed socket on one broker does not drop the other.

---

## Item 2 — Discover all groups

**Phase:** 1. **Architecture:** §§7, 9, 11 (`mt5_groups`). **Rule:** Manager API enumerates **all** accessible groups. Plan env (`MT5_GROUP_2STEP_DEMO=demo\yo-2step`, …) is an **optional label**, never the fetch list.

### Current status — **FAIL / not discovered into the product DB**

- C++ can enumerate: `GroupTotal`, `GetAllGroups`, `GetGroupDetails`, `GetGroupLogins` (`imt5_client.h` 163–167; `MT5Manager` implements them).
- Domain `Mt5Group` exists (`Id`, `BrokerId`, `Name`, `Currency`, `IsEnabledForAnalysis`, `LastDiscoveredAt`, `LastSyncedAt`).
- Infrastructure `Mt5GroupsConfiguration` targets a **non-existent** type `Mt5Groups` and invents `group_id` `bigint` — MT5 groups are **string paths** (`demo\Maxmaster`). Unusable.
- Worker never calls GetGroups. `demo\Maxmaster` is not even configured as a default in the C# worker.

### What to implement

1. Startup/resync per broker (§7): Connect → enumerate **all** groups → upsert `mt5_groups` with compound key `(broker_id, group_name)` → attach optional plan label from `MT5_GROUP_*`.
2. EF config on `Mt5Group` (singular): unique `(BrokerId, Name)`, `Name` as `text` (preserve backslash paths).
3. Versioned migration. No hand-edited schema.
4. Periodic rediscovery (new groups appear). Toggle `IsEnabledForAnalysis` is a later API `PATCH` (A26), not a fetch filter.
5. Tests: group sync slice of `Mt5.Mt5BackfillRestartTests`; fixture with groups **outside** the plan map must still persist.

**Done when:** after one resync, `mt5_groups` contains every Manager-visible group on both brokers, including groups not in `MT5_GROUP_*`. `demo\Maxmaster` is not treated as exclusive.

---

## Item 3 — Synchronize ~5,000 accounts

**Phase:** 1. **Architecture:** §§7, 10–12. **Proof:** ~5k path designed and measured; checkpointed; compound identity `broker_id + login`.

### Current status — **FAIL / no account sync**

- C++ `GetUserLogins(group)`, `GetUser`, `GetAccount` exist.
- Domain `Mt5Account` exists (`BrokerId`, `Login`, `GroupId`, balances, `LastSyncedAt`).
- `SyncCheckpoint` entity exists (`BrokerId`, `Login`, `Stream`, `LastTimestamp`, `LastTicket`) but is unused.
- `Mt5AccountsConfiguration` maps a phantom `Mt5Accounts` type and extra columns (`name`, `is_active`) that are not on `Mt5Account`.
- No backfill loop, no batching, no measured 5k timing, no idempotent upsert.

### What to implement

1. **Historical account sync:** for each broker, for each discovered group, page logins → `GetAccount` → upsert `mt5_accounts` on `(broker_id, login)`. Associate `GroupId`. Persist `sync_checkpoints` per `(broker_id, stream='accounts')` so restart resumes.
2. **Live path:** user add/update/delete from the event queue → lightweight persist + outbox (`§12`). No scoring in the callback.
3. **Scale design (must be written before claiming 5k):**
   - Pool size already in options (Achiever 8, Starwave 4) — use request-only pool for history, pump manager for events.
   - Batch upserts; do not open one transaction per account across 5k.
   - Bound concurrency; measure wall time and Manager rate limits.
4. Optional `mt5_account_snapshots` (§11) if balance history is needed for later features — not a §69 gate if current row is correct.
5. Tests: `Mt5BackfillRestartTests` — kill mid-sync, restart, no duplicate rows, checkpoint advances.

**Done when:** both brokers’ accessible accounts are in Postgres (~5k order of magnitude), restart-safe, unique on `(broker_id, login)`, and Overview can count them.

---

## Item 4 — Capture XAUUSD trades correctly

**Phase:** 1–2. **Architecture:** §§11–12, 16. Raw deals are immutable; XAU aliases map to canonical `XAUUSD`; do not fabricate ticks.

### Current status — **FAIL / no product ingest of deals**

- C++ `GetDeals(login, from, to)` is a complete-history contract; `GetRecentDeals` ring exists because broker history lags.
- C++ `mt5_ledger::Store` (`mt5_ledger_store.h`) is an **immutable revision ledger** keyed by `serverKey` — useful pattern, **not** the C# `mt5_deals` table and **not** XAU-filtered.
- Domain `Mt5Deal` exists (compound identity via `BrokerId` + `DealTicket`, `Symbol`, `Action`, `Entry`, native `Volume`, `IngestionEventId`).
- `SymbolNormalizer` maps `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD`, etc. to canonical `XAUUSD`. **Not called from any worker.**
- `SourceSymbolMapping` / `CanonicalInstrument` entities exist; no seed, no persist of observed broker symbols.
- Deal EF config is wrong-shaped (`open_time`/`close_time` on a deal; `volume` as decimal mapped to `bigint`).
- No `ingestion_events`, no outbox writer, no live deal persist-before-async.

### What to implement

1. **Raw persist first (§11, §72.6–7):** validate → dedupe `(broker_id, deal_ticket)` → insert `mt5_deals` + `ingestion_events` + `outbox_events` in **one** transaction. Callbacks only enqueue.
2. **Backfill:** per account checkpoint `stream='deals'` using last deal time/ticket; page `GetDeals`; never skip pages (C++ contract: false = unavailable).
3. **XAU correctness (this item):**
   - Persist **all** trading deals (not only XAU) so reconstruction is complete for mixed positions; **flag/map** XAU via `SymbolNormalizer` + per-broker `source_symbol_mappings`.
   - Seed/observe real Achiever and StarwaveFX gold symbols from Manager `GetSymbol*` / first deals. Do not assume the string `"XAUUSD"`.
   - Keep source `Symbol` **and** canonical code on reconstructed trades.
4. **Orders + current positions** (`mt5_orders`, `mt5_positions_current`) as supporting raw tables — required for honest reconstruction of open state.
5. **Ticks:** only if Manager tick subscribe is proven. Otherwise mark MFE/MAE `FeatureQuality.Unavailable` (`BaselineScorer` already does this). Never use cTrader quotes as source ticks (§17).
6. Tests: `Mapping.XauCanonicalMappingTests`, `Mt5DealDeduplicationTests`, replay of recorded XAU deals (`Replay.HistoricalMt5EventReplayTests`).

**Done when:** a known XAUUSD (or alias) deal on each broker survives restart exactly once, mapped to canonical `XAUUSD`, with native volume recoverable to lots via the documented scale.

---

## Item 5 — Reconstruct logical trades

**Phase:** 2. **Architecture:** §14. Order ≠ Deal ≠ Position ≠ Logical Trade. Count completed **position lifecycles**, including scale-in, partial close, SL/TP, reversal.

### Current status — **PARTIAL algorithm / not in the pipeline**

`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` is real logic:

- Groups trading deals (`Buy`/`Sell`) by `PositionId`.
- Handles `In` / `Out` / `OutBy` / `InOut` via `DealEntry`.
- Tracks VWAP, scale-in, partial close, averaging-down, SL/TP, native→lots via `VolumeConverter`.
- Emits `ReconstructedTradeResult` (string `BrokerId`, `long Login`, optional `ClosedAt`).

Gaps:

- **Not invoked** from Application, worker, or outbox.
- Entity `ReconstructedTrade` is a different shape (`Guid BrokerId`, `ulong Login`, **required** `ClosedAt`) — cannot persist open trades without a model change.
- **No unit tests** (`TradeReconstructionTests`, partial/scale-in/full/reversal classes from A27 are missing).
- Fees field is hardcoded `0`.
- Opposite `In` on the same position is a heuristic; must be locked by tests against real MT5 tapes.

Classification: algorithm **EXISTS_NEEDS_REFACTOR**. Product capability **MISSING**.

### What to implement

1. Align persist model with `ReconstructedTradeResult` (nullable `ClosedAt`, `Completed`, deal ticket list, remaining lots).
2. Application service `ITradeReconstructionService`: on outbox `TradeCompleted` / deal-batch, load raw deals for `(broker_id, login, position_id)`, run reconstructor, upsert `reconstructed_trades` idempotently.
3. Do not reconstruct inside the MT5 callback.
4. **Tests first (A27 order):** full close, partial close, scale-in, reversal, mixed non-XAU ignored for XAU views, volume scale 10_000.
5. Optional extract to `/src/TradeReconstruction` later (§66). Fine to keep in Domain until tests pass.

**Done when:** reconstruction unit + replay tests pass; DB rows match golden tapes for the four lifecycle cases.

---

## Item 6 — Detect the first 3 completed XAUUSD trades

**Phase:** 2. **Architecture:** §15. Count **3 completed reconstructed XAUUSD position lifecycles**. Trade #3 → `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`. Do not count order place, fill, partial close, SL/TP modify as a trade.

### Current status — **PARTIAL helpers / not durable**

On `TradeReconstructor`:

- `CompletedXauUsdTrades` filters `Completed && IsXauUsd`, ordered by close time.
- `CountCompletedXauUsdTrades`
- `IsEarlyScoreEligible` ⇔ count ≥ 3

`BaselineScorer.EarlyScoreTradeCount = 3` and `TraderState.INSUFFICIENT_DATA` until eligible.

Missing: persisted `CompletedXauTrades` updates, first-three highlight on trades, state transition to `EARLY_SCORE` / `SHADOW` driven by a job, tests that reject partials as trades.

### What to implement

1. After each completed XAU reconstruction, recompute count for `(broker_id, login)`.
2. Persist on `TraderScore.CompletedXauTrades` (field already exists).
3. Flag the three earliest completed XAU trades (`isFirstThree`) for Trader Detail (§51).
4. On crossing 3: emit outbox `ScoreUpdate`; set eligibility; **default next state is SHADOW only if score is high** (§23) — never LIVE.
5. Tests: `FirstThreeCompletedXauTradesTests` — ignore SL modify, partial close, non-XAU, incomplete position.

**Done when:** a fixture with 2 completed XAU + 1 partial + 5 gold-alias fills still reports count 2; the third completed close flips eligibility exactly once.

---

## Item 7 — Produce a deterministic trader / risk score

**Phase:** 3. **Architecture:** §§18, 22–23. Baseline outputs `risk_score`, `behavior_score`, `early_quality_score`. Same trades → same scores. ML later must beat this. Trade #3 + high score → **SHADOW only**.

### Current status — **PARTIAL scorer / not operational**

`D:\Prop\src\Domain\Scoring\BaselineScorer.cs` computes:

- Features: net PnL, profit factor, lot CV, loss-size CV, martingale, averaging-down, lot escalation, hold time, SL use, drawdown, frequency.
- MFE/MAE left `Unavailable` (correct until source ticks exist).
- Scores 0–100 and `TraderStateMachine` (`INSUFFICIENT_DATA` / `EARLY_SCORE` / `WATCH` / `SHADOW` / `RISK_BLOCKED`).
- `CanPromoteToLive` is **hard-false** (correct for §69).

`TraderScore` + `TraderScoreHistory` entities exist. **No job writes them.** No feature snapshot table (`trader_feature_snapshots`, `trader_risk_flags` in §45). No tests. Thresholds (1.25× martingale, quality 70, risk 80) are **unbacktested defaults** — acceptable as v0 if versioned.

`RiskEngine` is a **copy-risk** gate (quotes, exposure, kill switch), not the §18 trader score. Keep them separate.

### What to implement

1. `ITraderScoreCalculator` Application service wrapping `BaselineScorer`.
2. Outbox consumer: load completed XAU reconstructed trades → `Score()` → upsert `trader_scores` + append `trader_score_history` + persist feature snapshot + risk flags (martingale / averaging / lot escalation).
3. Version the formula (`baseline_v0`). Changing weights requires a new version and history rows.
4. Continuous rescore after trade 4, 5, … (§22).
5. Tests: `TraderScoreCalculatorTests` (deterministic), `ThreeTradeSafetyGateTests` (high score → SHADOW, not LIVE), `ScoreStateTransitionTests`.
6. Do **not** add XGBoost / `mlProbability` except as nullable null.

**Done when:** two runs on the same reconstructed set write identical scores; a trader with 3 wins and no martingale is `SHADOW` or `WATCH`, never `LIVE`.

---

## Item 8 — Rank traders

**Phase:** 3. **Architecture:** §§21, 50. Leaderboard ordered by the **deterministic** early/quality score (ML column null). Filters: broker, group, state, score, risk, trade count, martingale.

### Current status — **MISSING**

No ranking service, no `ORDER BY`, no `/api/v1/traders` endpoint. `BaselineScorer` produces a scalar but nothing sorts traders against each other. React `useTraders` already expects `{ items, total }` from `/api/traders` (wrong prefix vs A26 `/api/v1/traders`) — both the API and the page are absent.

### What to implement

1. Query service: join `trader_scores` + `mt5_accounts` + `mt5_groups` + latest flags.
2. Default sort: `early_quality_score DESC`, then `risk_score ASC`, then `completed_xau_trades DESC`. Stable tie-break `(broker_id, login)`.
3. Persist nothing extra unless a “rank_as_of” snapshot is needed for audit; live rank from current scores is enough for §69.
4. API: `GET /api/v1/traders` per A26 §4.6 (RBAC ReadOnly+). `mlProbability` always null.
5. Tests: replay `ScoreComputationFromReplayTests` asserts order of a 3-trader fixture.

**Done when:** the same fixture always returns the same ordered list; traders with `< 3` completed XAU sort below eligible ones or are filterable via `minTrades`.

---

## Item 9 — Connect to cTrader QUOTE FIX securely

**Phase:** 4. **Architecture:** §§25–28, 31. Two **independent** sessions conceptually; first useful needs **QUOTE Logon over TLS**. Prefer QuickFIX/n + cTrader RoE dictionary. Do **not** write a TcpClient engine.

### Current status — **FAIL / no session**

| Piece | State |
|---|---|
| `CTraderFixOptions` | Host `live-us-eqx-01.p.c-trader.com`, QUOTE SSL 5211, TRADE SSL 5212, `UseSsl=true`, `RealCopyExecutionEnabled=false`. |
| TargetCompID default | `"CSERVER"` — **conflicts with issued `cServer`** unless staging proves otherwise (§26). |
| `TradeSessionEnabled` | `true` — too aggressive for §69. |
| Package | `QuickFix.Net 1.11.2` — **wrong package family** vs A05 pin `QuickFIXn.Core 1.14.1` + `QuickFIXn.FIX44 1.14.1`. Local nupkgs already sit in `reports\swarm\20260818\_tmp_qfn\`. |
| Parser / harness | Unit-test toys using `\|` instead of SOH. Not a session. |
| `FixSessionState` entity | Unused. |
| `apps/fix-worker` | 1 Hz heartbeat. No SSL, no Logon, no sequence files. |

No production TcpClient engine was found (good). Do not add one.

### What to implement

1. Replace `QuickFix.Net` with **QuickFIXn.Core + QuickFIXn.FIX44 1.14.1** and a **cTrader Rules-of-Engagement dictionary** (generic FIX44 is not sufficient — §5, A05).
2. `CTraderQuoteSession` as its own initiator: independent TCP/SSL, sequence store, heartbeat, reconnect, metrics. Make `SenderSubID` **and** `TargetSubID` configurable; do not hardcode case.
3. Persist `fix_session_states` (QUOTE row): connected, logged on, seq in/out, last in/out, reconnect count, last error. **Never persist password.**
4. `apps/fix-worker`: host QUOTE only for §69. TRADE object may be constructed later with `TradeSessionEnabled=false`.
5. Single-active ownership (Redis lease / DB advisory lock) so two workers cannot share a session (§28). Required even for QUOTE if two instances start.
6. TLS default 5211. Plain 5201 is not a production default.
7. Tests: `Fix.QuickFixnSessionConfigurationTests` (SSL ports, reset flag, independent stores). Live Logon is a **staging checklist**, not a unit default. Harness stays in test mode (§61).

**Done when:** staging Logon on QUOTE SSL is proven and the persisted session row shows `LoggedOn` / `ReadyForMarketData` after process restart + sequence recovery. Password never appears in logs or API.

---

## Item 10 — Discover the Pepperstone XAUUSD instrument ID

**Phase:** 4. **Architecture:** §§16, 30, 72.13. SecurityList → find XAU → persist instrument ID, name, digits. **Never guess tag 55. Never copy an ID from another account.**

### Current status — **FAIL / guessed in the harness**

- `SymbolNormalizer.RegisterVenueInstrument` / `TryMapVenueInstrumentId` exist but the venue map starts **empty**.
- `FixSimulationHarness.SimulateSecurityList` hardcodes tag `55=123456` and `1007=XAUUSD`. That is a test double, **not** a discovery implementation. Shipping `123456` as production would violate §72.13.
- No `SecurityListRequest` sender, no persist of `destination_symbols`, no QUOTE MarketDataRequest for the discovered ID.

### What to implement

1. After QUOTE Logon: send `SecurityListRequest`; parse repeating group; select the Pepperstone XAU contract by name/description rules (document the match: symbol name contains XAUUSD / GOLD as confirmed on **this** account).
2. Persist `destination_symbols` / mapping: `venue_instrument_id`, symbol name, digits/precision, min/max/step qty, `canonical_instrument_id = XAUUSD`.
3. Subscribe market data for **that** ID only. Persist `destination_quotes` (`DestinationQuoteSnapshot` already sketched).
4. Reject startup of shadow copy if mapping is missing.
5. Tests: `Harness.SecurityListXauDiscoveryTests` against a **recorded** SecurityList from the real account (redact secrets). `Mapping.DestinationInstrumentMappingTests`. Fixture ID must come from the recording, not `123456`.

**Done when:** DB contains the instrument ID that SecurityList actually returned for account `1369850`, and a fresh environment without that row cannot shadow-trade.

---

## Item 11 — Shadow-copy selected traders using destination quotes

**Phase:** 5. **Architecture:** §§23–24, 31, 36–38, 63–64. A24 is the executable policy. **Every shadow fill price comes from a persisted destination QUOTE.** No TRADE `NewOrderSingle`.

### Current status — **PARTIAL stub / not a shadow book**

`ShadowCopyEngine` can simulate entry/exit and conservative MTM (longs on dest bid, shorts on dest ask) and records `SourceVsShadowSlippage`. It does **not**:

- persist `shadow_copy_order` / fill / position / pnl
- reject stale quote / stale signal / wide spread / catch-up opens
- distinguish OPEN vs REDUCE/CLOSE (A24: different guards)
- normalize quantity (`QuantityNormalizer` exists unused)
- apply destination commission/swap model
- consume reconstructed events or trader state `SHADOW`
- read live `destination_quotes`

`ShadowOrder` entity is a single fill-shaped row, not the A24 table set. `CopyIntent` exists unused. `RiskEngine` already encodes QUOTE_STALE / SPREAD_TOO_WIDE / SIGNAL_STALE / STOP_NEW for increasing actions — not wired.

React hook `useEmergencyFlatten` posts a live-danger endpoint that **must not** exist in §69.

### What to implement

Follow A24 acceptance (13 bullets). Minimum product path:

1. **Select:** only `TraderState.SHADOW` (from item 7 gate). Manual Analyst promote Watch→Shadow via audited `PATCH` (A26).
2. **Intent:** source reconstructed event → `CopyIntent` with `expires_at` + `max_signal_age` + idempotency key. Persist **before** simulate.
3. **Price:** latest persisted QUOTE snapshot for the discovered instrument. If missing/stale/wide/QUOTE down → reject **opens**; closes use last quote + quality flag or `UNPRICED` hold — never invent a tick (A24.4–6).
4. **Size:** `QuantityNormalizer` with destination min/step/max. 0.10 MT5 lots is not blindly 0.10 cTrader qty.
5. **Book:** persist order, fill, position, pnl, source_vs_shadow_slippage. Conservative MTM. Null ≠ 0 when unpriced.
6. **Outage:** source open+close while QUOTE down and no prior shadow position → **zero** positions (A24.5). Restart must flatten or flag, not leave a silent open.
7. **Reversal:** CLOSE then OPEN; rejected OPEN leaves flat.
8. **Hard no:** no FIX TRADE send. `AllowFixSend` stays false.
9. Tests: A24.12 unit list + `Replay.ShadowCopyFromReplayTests`.

Optional module `/src/Shadow` when it outgrows Domain.

**Done when:** replay of recorded dest quotes + MT5 events produces golden fills/P&L, and a grep of the send path shows zero `NewOrderSingle` from this worker.

---

## Item 12 — Show all of this in React

**Phase:** 3–5 (UI can trail data, but §69 is false until the operator can **see** items 1–11). **Architecture:** §§5, 46–54, 55, 59. Contracts: A26 + A06.

### Current status — **FAIL / shell only, cannot compile, nothing to bind**

**Web (`D:\Prop\apps\web`):**

| Exists | Missing / wrong |
|---|---|
| Vite, React 18, Router, TanStack Query, axios, SignalR client, Tailwind, Recharts dep | **`src/pages/*` = 0 files** while `App.tsx` imports 14 pages → **`tsc` fails** |
| `DashboardLayout` nav (Overview…Settings) | No Models / Live Copy / Audit nav items (acceptable to omit Models/Live for §69; Audit is required for overrides) |
| Types + hooks for overview/brokers/groups/traders/FIX/risk/recon/health/settings | Hooks call `/api/...` not A26 `/api/v1/...` |
| SignalR `/hubs/dashboard` | A26 hub is `/hubs/ops` (optional for §69) |
| `useEmergencyFlatten` + `useUpdateSettings` PUT | **Unsafe for v1** — flatten is §70; settings must not write secrets |

**API (`D:\Prop\apps\api`):** still `GET /weatherforecast` only. A06: 0 dashboard endpoints, no RBAC, no redaction layer.

**Auth:** none. Entire host would be anonymous if endpoints were added.

### What to implement

Implement **pages that display items 1–11**, backed by a **read-mostly, secret-safe, RBAC-gated** API. Full route list is A26; first-useful subset:

| UI (must ship) | Proves item | API (A26) |
|---|---|---|
| Overview | 1–3, 6, 8, 9, 11 counts | `GET /api/v1/overview` |
| Brokers | 1 | `GET /api/v1/brokers` (masked login, no password) |
| Groups | 2 | `GET /api/v1/mt5/groups` |
| Traders leaderboard | 6–8 | `GET /api/v1/traders` |
| Trader detail + first 3 highlighted | 4–7, 11 | `GET /api/v1/traders/{brokerId}/{login}` + `/trades` + `/scores` + `/shadow` |
| Trade explorer | 4–5 | `GET /api/v1/trades` |
| Scoring summary | 7–8 | `GET /api/v1/scoring/summary` (baseline version; no Models) |
| Shadow portfolio | 11 | `GET /api/v1/shadow/portfolio` + positions |
| FIX page QUOTE card | 9–10 | `GET /api/v1/fix/sessions` + `/fix/quote` (instrument id, bid/ask, age). TRADE card may say “disabled / not started” |
| System health | 1, 9 | `GET /api/v1/system/health` |
| Reconciliation (MT5) | 1–4 | `GET /api/v1/reconciliation/runs` |
| Risk read + STOP_NEW | safety | `GET /api/v1/risk/snapshot` + `POST .../stop-new-execution` (RiskManager+) |
| Settings **read** | flags | `GET /api/v1/settings/public` — `realCopyExecutionEnabled: false` |

Also required so the UI is not a hole:

- Auth + roles (§59): at least ReadOnly / Analyst / RiskManager / SuperAdmin.
- Secret denylist (§55): never send MT5/FIX passwords, proxy creds, connection strings, raw env.
- Delete `GET /weatherforecast`.
- Create the 14 page components `App.tsx` already names (or change routes to match shipped pages — do one or the other).
- Align hook base paths to `/api/v1`.
- **Do not** ship `POST /emergency-flatten`, `POST /execution/enable`, credential PUTs, or Models promote.
- SignalR optional; polling Overview + `/fix/quote` is enough to unblock §69.12 (A06).

**Done when:** an authenticated ReadOnly user can open Overview and see both brokers connected, group/account counts, XAU reconstructed trades, first-3, deterministic ranks, QUOTE health + discovered instrument, and shadow PnL — all from real persisted data, no secrets in the JSON.

---

## Implementation sequence (do not skip)

Matches §67 and A27 §11. Later items stay red until earlier persist exists.

| Step | Unblocks | Work |
|---|---|---|
| 0 | honesty | Finish Phase 0 artifacts (this file + A01–A28). Fix Infrastructure so it **compiles** against real Domain types. Delete Application `Class1`. Add versioned migrations. |
| 1 | 1–3 | Dual-broker connector + worker connect + group discover + 5k account sync + checkpoints + dual-broker tests. |
| 2 | 4–6 | Deal/order/position ingest + outbox + `SymbolNormalizer` mappings + `TradeReconstructor` persist + first-3 counter + reconstruction unit tests. |
| 3 | 7–8 | `BaselineScorer` job + history + rank query + score tests. React can start on these reads. |
| 4 | 9–10 | QuickFIX/n QUOTE TLS + SecurityList persist + dest quotes. No TRADE send. |
| 5 | 11 | Shadow engine on dest quotes + A24 tests. |
| 6 | 12 | API `/api/v1` + RBAC + pages bound to 1–11. |
| — | not §69 | ML, TRADE Logon soak, NewOrderSingle, live copy — §70 / Phase 6–8. |

**Definition of first useful version:** items 1–12 **all true** with evidence (tests, staging logs, hashes). This report records **0/12**.

---

## Evidence index (paths cited)

| Area | Absolute paths |
|---|---|
| Architecture §69 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Domain logic | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`, `Scoring\BaselineScorer.cs`, `Shadow\ShadowCopyEngine.cs`, `Risk\RiskEngine.cs`, `Instruments\SymbolNormalizer.cs` |
| Broken persistence | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`, `Configurations\BrokersConfiguration.cs`, `Mt5GroupsConfiguration.cs`, `Mt5AccountsConfiguration.cs`, `Mt5DealsConfiguration.cs` |
| Empty application | `D:\Prop\src\Application\Class1.cs` |
| MT5 C# port only | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`, `Configuration\Mt5BrokerOptions.cs` |
| C++ SDK | `D:\Prop\mt5-sdk\src\core\imt5_client.h`, `mt5_manager.h`, `mt5_pool.h`, `config\app_config.h`, `services\mt5_ledger_store.h` |
| FIX stub | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`, `Parsing\FixMessageParser.cs`, `Testing\FixSimulationHarness.cs` |
| Hosts | `D:\Prop\apps\api\Program.cs`, `apps\mt5-worker\Worker.cs`, `apps\fix-worker\Worker.cs` |
| React shell | `D:\Prop\apps\web\src\App.tsx`, `layouts\DashboardLayout.tsx`, `api\hooks.ts` |
| Prior specs | `D:\Prop\reports\swarm\20260818\A06_api_audit.md`, `A24_shadow_copy_spec.md`, `A26_dashboard_api_spec.md`, `A27_test_inventory.md`, `A28_phases_gates.md` |

---

## Sign-off (first useful version)

```text
[ ]  1. Connect to both MT5 brokers.
[ ]  2. Discover all groups.
[ ]  3. Synchronize ~5,000 accounts.
[ ]  4. Capture XAUUSD trades correctly.
[ ]  5. Reconstruct logical trades.
[ ]  6. Detect the first 3 completed XAUUSD trades.
[ ]  7. Produce a deterministic trader / risk score.
[ ]  8. Rank traders.
[ ]  9. Connect to cTrader QUOTE FIX securely.
[ ] 10. Discover the Pepperstone XAUUSD instrument ID.
[ ] 11. Shadow-copy selected traders using destination quotes.
[ ] 12. Show all of this in React.

[ ] Phase 0–5 exits (A28)
[ ] Reviewer PASS + test PASS with on-disk evidence
[ ] ML not required
[ ] REAL_COPY_EXECUTION_ENABLED remains false
```

**A57 conclusion:** treat the tree as Phase 0 → early Phase 2 **sketches**. The first useful version has not started operating. Implement in the sequence above; do not mark an item true until the Done-when line is evidenced.
