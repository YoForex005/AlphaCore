# C13 — Architecture §69 first useful version scorecard

| Field | Value |
|---|---|
| Agent | C13 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C13_fuv_scorecard.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §69 (lines 2633–2654) |
| Product source modified | **No** |
| Method | Read-only inventory of current `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`, `D:\Prop\mt5-sdk`, `D:\Prop\docs`, plus §69 text and later measured audits (A57, B04, B05, B07, B10, B18, PHASE0). A57’s **0/12 accepted** is re-checked against the tree as it exists now, not restated from memory. |

**Bar (verbatim §69):** the first genuinely useful system does **not** need ML. It is accepted only when **all 12 items are true** as an operating system — both brokers connected, groups and ~5k accounts synchronized, XAUUSD deals captured, logical trades reconstructed, first-3 detected, deterministic scores + ranking persisted, QUOTE FIX logged on, Pepperstone XAU instrument **discovered** (not guessed), selected traders shadow-copied on destination quotes, and all of that shown in React.

Live `NewOrderSingle` and Phase 6 ML are **out of this bar**. `REAL_COPY_EXECUTION_ENABLED=false` is the correct default.

---

## 0. Scoring rubric (do not greenwash)

| Token | Meaning | Counts as §69 accepted? |
|---|---|---|
| **PASS** | Running system does the item against the intended venues, or against persisted data that originated from those venues / a recorded replay of those venues. | **Yes** |
| **DEMO** | Fake connector + `EnsureCreated` / EF InMemory + seeder implements the *shape*. Integration tests may pass on canned rows. | **No** |
| **PARTIAL** | Algorithm or UI exists, but is not a complete pipeline, or the UI is a stub / JSON dump. | **No** |
| **FAIL** | Missing, unused, or a **health lie** (row / API claims connected / logged-on / discovered without a socket). | **No** |

Demo fixtures, hardcoded `Connected = true`, and a FIX worker that stamps `LastInboundAt` are **not** first useful version.

---

## 1. Executive verdict

**Accepted: 0 / 12.**  
**Demo-wired (not accepted): 6 / 12** — items 2, 4, 5, 6, 7, 8.  
**UI shell (not accepted): item 12 PARTIAL.**  
**Fail / lie: items 1, 3, 9, 10, 11.**

A57’s **accepted 0/12** is still the correct gate. What changed since A57 is **not** a first useful version. It is a **demo ingest → reconstruct → score → rank → React table** path on `FakeMt5BrokerConnector` and in-memory (or `EnsureCreated`) storage. C# still cannot talk to Achiever, StarwaveFX, or Pepperstone QUOTE.

Do not treat:

- `FakeMt5BrokerConnector.ConnectAsync` setting `_connected = true`
- `EfDashboardQueries` hard-coding `Connected = true`
- `/api/health` hard-coding `healthy = true`
- `apps/fix-worker` writing `ReadyForMarketData` / `LoggedOn` every 15 s
- 4 canned accounts / 18 canned XAUUSD deals
- `VenueInstrumentId = null` plus harness tag 55 `123456`
- an unused `ShadowCopyEngine`

as §69 acceptance.

ML is correctly **not** required. Live copy is **safe by absence** of a `NewOrderSingle` send path (`Fix.CTrader.csproj` has **zero** QuickFIX packages; `grep` of `D:\Prop\src` finds no `HttpClient`, no `QuickFix`, no EF `Migration`).

| # | §69 item (verbatim) | Maturity | Accepted? |
|---|---|---|---|
| 1 | Connect to both MT5 brokers | **FAIL** (fake connect + health lie) | **No** |
| 2 | Discover all groups | **DEMO** | **No** |
| 3 | Synchronize ~5,000 accounts | **FAIL** (4 logins, no checkpoints) | **No** |
| 4 | Capture XAUUSD trades correctly | **DEMO** (18 canned deals) | **No** |
| 5 | Reconstruct logical trades | **DEMO** (algorithm + persist + tests) | **No** |
| 6 | Detect the first 3 completed XAUUSD trades | **DEMO** (counter + persist + tests) | **No** |
| 7 | Produce a deterministic trader/risk score | **DEMO** (`baseline.v1`, no ML) | **No** |
| 8 | Rank traders | **DEMO** (sort by early score) | **No** |
| 9 | Connect to cTrader QUOTE FIX securely | **FAIL** (no socket; worker stamps healthy) | **No** |
| 10 | Discover the Pepperstone XAUUSD instrument ID | **FAIL** (null + forbidden `123456` fixture) | **No** |
| 11 | Shadow-copy selected traders using destination quotes | **FAIL** (engine unused; UI stub) | **No** |
| 12 | Show all of this in React | **PARTIAL** (15 pages; cannot show 1/3/9–11 truthfully) | **No** |

---

## 2. Cross-cutting facts (apply to every item)

| Surface | Path | Measured now |
|---|---|---|
| Domain algorithms | `D:\Prop\src\Domain\` | Reconstruction, `SymbolNormalizer`, `BaselineScorer`, `RiskEngine`, `ShadowCopyEngine`, FIX FSM helpers. Real code. |
| Application | `D:\Prop\src\Application\` | Ports (`IMt5BrokerConnector`, `ITradingStore`, `IDashboardQueries`), `DealIngestionService`, `ReconstructionScoringService`. |
| Persistence | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Fluent map of first-useful tables + compound uniques. **0 migrations.** `Configurations/` is empty. Hosts call `EnsureCreatedAsync`. Empty connection string → `UseInMemoryDatabase("trader-intelligence")`. |
| C# MT5 | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | **Only** `IMt5BrokerConnector` implementation. DI always registers `DemoBrokerFactory.CreateDefault()`. Dead unused `IBrokerConnector`. Unused `Mt5BrokerOptions`. **No HTTP adapter. No P/Invoke.** |
| C++ SDK | `D:\Prop\mt5-sdk\config\app_config.h` | Real `MT5Manager` / `MT5Pool` / `IMT5Client`. **Single-broker** `mt5_server` / `mt5_login`. **Not wired** into the C# product hosts. |
| FIX | `D:\Prop\src\Fix.CTrader\` | Options + pipe parser + ownership lock + string harness. **No** `CTraderQuoteSession`. **No** QuickFIXn. |
| API | `D:\Prop\apps\api\Program.cs` | Unversioned `/api/*` (not `/api/v1`). Seeds demo on startup. Health is hardcoded. |
| MT5 worker | `D:\Prop\apps\mt5-worker\Worker.cs` | 30 s god-loop: sync both fake brokers, rebuild 4 hard-coded logins. |
| FIX worker | `D:\Prop\apps\fix-worker\Worker.cs` | 15 s stamp of session rows. **UNSAFE health lie.** |
| React | `D:\Prop\apps\web\src\pages\` | **15** page modules + nav. Shadow / Live / Audit are copy stubs. SignalR client hits `/hubs/dashboard`; API has **no** hub. |
| Tests | `D:\Prop\tests\Unit`, `Integration` | Reconstruction, scorer, symbol, volume, risk, sizing, plus `SeedingAndStoreTests` (demo seed + deal upsert). **No** live-broker / FIX-session / 5k / shadow-pipeline tests. |
| Compose | `D:\Prop\docker-compose.yml` | Postgres + Redis + `dotnet run` API. Native MT5 worker correctly kept off Linux. |

Identity leftover: Domain reconstructor keys `BrokerId` as **string** (`"ACHIEVER"`); EF entities use **Guid**. Application maps at `LoadDealsAsync` / `ResolveBrokerIdAsync`. That is acceptable at the boundary; it is not a dual live session.

---

## 3. Item 1 — Connect to both MT5 brokers

**Accepted: No. Score: FAIL.**

### Law

§69.1 + §§6–8, 10. Two independent Manager sessions (Achiever + StarwaveFX), reconnect-safe, `broker_id` isolation.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Only C# implementation | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` L6–42 | `ConnectAsync` sets `_connected = true`. No socket. |
| Dual “brokers” | same file `DemoBrokerFactory.CreateDefault` L95–127 | Two in-memory fakes labeled `"ACHIEVER"` / `"STARWAVEFX"`. |
| Production DI | `D:\Prop\src\Infrastructure\DependencyInjection.cs` L31–34 | **Always** `DemoBrokerFactory.CreateDefault()`. No slot binder, no env switch. |
| Worker “connect” | `D:\Prop\apps\mt5-worker\Worker.cs` L29–30 | `SyncBrokerAsync(Achiever)` then `SyncBrokerAsync(StarwaveFx)` against those fakes. |
| Worker config | `D:\Prop\apps\mt5-worker\appsettings.json` | Empty logging only. No `MT5_*` hosts. |
| Dashboard lie | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L53 | `Connected` hard-coded `true`; `LastEventAt = DateTimeOffset.UtcNow`. Never calls `IsConnectedAsync`. |
| API health lie | `D:\Prop\apps\api\Program.cs` L26–33 | `mt5Connections: [{ name: "ACHIEVER", healthy: true, details: "demo connector" }]`. |
| Seeded broker rows | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L29–59 | Writes Achiever `57.128.141.65:443` login 2027 and StarwaveFX `84.201.6.142:443` login 9904 as **metadata**, not connections. |
| C++ still one broker | `D:\Prop\mt5-sdk\config\app_config.h` L19–25 | Single `mt5_server` / `mt5_login` / `mt5_password`. No `broker_id`. |
| Unused options | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | Documents remote/local; **not bound**. |
| Dead interface | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Zero implementers. |
| No HTTP / native C# | `grep` `HttpClient` under `D:\Prop\src` | **Zero hits.** |

C++ `IMT5Client::Connect` exists adjacent to the product (`mt5-sdk\src\core`). It is **not** called by `apps/mt5-worker`.

### Done when

Process logs a real Manager Connect for **both** broker codes after restart; health is derived from `IsConnectedAsync` (or the C++ watchdog), not a constant; killing one socket does not drop the other.

---

## 4. Item 2 — Discover all groups

**Accepted: No. Score: DEMO.**

### Law

§69.2 + §§7, 9, 11, A39/A40. Enumerate **all** Manager-visible groups. `MT5_GROUP_*` plan env is a **label**, never the fetch filter.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Fake group list | `FakeMt5BrokerConnector.cs` L101–120 | Hard-codes 3 Achiever groups (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) + 1 Starwave (`real\standard`). |
| Ingest upsert | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–40 | `GetGroupsAsync` → `UpsertGroupAsync` for whatever the connector returns. |
| Persist unique | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` L22–51 | Upsert on `(BrokerId, Name)`. |
| Schema | `TraderDbContext.cs` L44–49 | Unique `(BrokerId, Name)` on `mt5_groups`. |
| API + UI | `apps/api/Program.cs` L56; `apps/web/src/pages/GroupsPage.tsx` | Lists persisted groups. Copy claims “Discovered dynamically.” |
| Integration | `tests/Integration/SeedingAndStoreTests.cs` L27–28 | After seed: 2 brokers, `Mt5Groups.Count() > 2`. |
| Plan filter | Fake `GetGroupsAsync` | Returns the canned list; **does not** filter by plan env. That part of the law is accidentally satisfied **because there is no live fetch**. |

This is **not** “discover all groups.” It is “persist four fixture group names.” C++ `GroupTotal` / `GetAllGroups` (`mt5-sdk`) is unused by C#.

### Done when

After one resync, `mt5_groups` contains every Manager-visible group on **both** live brokers, including groups **not** in `MT5_GROUP_*`. `demo\Maxmaster` is not exclusive.

---

## 5. Item 3 — Synchronize ~5,000 accounts

**Accepted: No. Score: FAIL.**

### Law

§69.3 + §§7, 10–12. ~5k-scale, checkpointed, unique `(broker_id, login)`, restart-safe.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Fixture accounts | `FakeMt5BrokerConnector.cs` L107–124 | **Four** logins: 10001, 10002, 10003, 99001. |
| Worker score list | `apps/mt5-worker/Worker.cs` L31–35 | Same four logins hard-coded. |
| API resync | `apps/api/Program.cs` L79–80 | Same four logins. |
| Checkpoints | `TraderDbContext.cs` L23, L115–120; `Domain\Entities\SyncCheckpoint.cs` | Entity + unique `(BrokerId, Login, Stream)` exist. **`grep SyncCheckpoint` in `*.cs` writers: unused.** |
| Scale | no batch upsert, no paging, no measured 5k timing | Single-account `SaveChanges` in `UpsertAccountAsync`. |
| Overview count | `EfDashboardQueries.cs` L16 | `Mt5Accounts.CountAsync` — will show **4** after seed. |

Schema uniqueness `(BrokerId, Login)` is the right shape (`TraderDbContext.cs` L51–56). There is no 5k path.

### Done when

Both brokers’ accessible accounts are in Postgres at ~5k order of magnitude, unique on `(broker_id, login)`, checkpointed so a killed mid-sync restart does not duplicate, and Overview counts that population.

---

## 6. Item 4 — Capture XAUUSD trades correctly

**Accepted: No. Score: DEMO.**

### Law

§69.4 + §§11–12, 16. Raw deals immutable; XAU aliases map to canonical `XAUUSD`; native volume recoverable; no fabricated ticks.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Fixture deals | `FakeMt5BrokerConnector.cs` L130–169 | 9 closed round-trips × 2 deals = **18** deals. Symbol is the literal string `"XAUUSD"` on every row. |
| Volume scale | same file L91–93; `VolumeConverter` | `1 lot = 10_000` (correct official classic `Volume()`). |
| Persist + dedupe | `EfTradingStore.cs` L85–114 | Insert-if-absent on `(BrokerId, DealTicket)`. Integration test proves second upsert returns false (`SeedingAndStoreTests.cs` L38–62). |
| Schema | `TraderDbContext.cs` L58–64 | Unique `(BrokerId, DealTicket)`; index `(BrokerId, Login, DealTime)`. |
| Normalizer | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` | Maps `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `GOLD`, … Used by **reconstructor**, not by ingest. Source column stored as fixture `"XAUUSD"`. |
| Observed mappings | `SourceSymbolMappings` DbSet | Table mapped; **not seeded** from Manager `GetSymbol*`. |
| Ticks | none in C# ingest | Correctly not fabricated. MFE/MAE stays `Unavailable` in scorer. |
| Live capture | no HTTP / native GetDeals | Worker pulls the same 18 canned rows every 30 s. |

Correct pieces: compound identity, native `ulong` volume, buy/sell + in/out entries, idempotent upsert. Missing: real broker symbols, history backfill checkpoints, `ingestion_events` / outbox in the same transaction, live deal stream.

### Done when

A known XAUUSD **or alias** deal on **each live broker** survives restart exactly once, mapped to canonical `XAUUSD`, native volume recoverable via the documented scale.

---

## 7. Item 5 — Reconstruct logical trades

**Accepted: No. Score: DEMO** (strongest algorithm item).

### Law

§69.5 + §14. Order ≠ Deal ≠ Position ≠ Logical Trade. Count completed **position lifecycles**, including scale-in, partial close, SL/TP, reversal.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Algorithm | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | Groups by `PositionId`; IN / OUT / OUT_BY / INOUT; scale-in; partial close; reverse leftover; open leftover as `Completed=false`. |
| XAU flag | `ReconstructedTradeResult.cs` L40–41 | `IsXauUsd` iff canonical `XAUUSD`. |
| Ignore non-trading | `NormalizedDeal.cs` L25; unit test `Ignores_balance_deals` | Balance deals dropped. |
| Persist | `ReconstructionScoringService` L78–83 + `EfTradingStore.ReplaceReconstructedAsync` L172–213 | Rebuild from stored deals; replace per `(broker, login)`. |
| Unit tests | `tests/Unit/TradeReconstructionTests.cs` | Round-trip, scale-in + partial + average-down, reverse INOUT, first-3, ignore balance. |
| Integration | `SeedingAndStoreTests.cs` L30 | Seed produces completed XAU reconstructed trades. |
| API / UI | `apps/api/Program.cs` L63–71; `TradeExplorerPage.tsx` | Last 200 reconstructed rows. |

This is a **real reconstructor** on **canned** deals. It is not proven on live Achiever/Starwave books, mixed non-XAU books, canceled deals (13/14), or Manager volume-ext.

### Done when

Completed XAU position lifecycles from **live or recorded** Manager deals persist 1:1 with A21 fixtures, including scale-in / partial / reverse, and survive worker restart without inventing trades.

---

## 8. Item 6 — Detect the first 3 completed XAUUSD trades

**Accepted: No. Score: DEMO.**

### Law

§69.6 + §22 / A21 / A69. Count **completed reconstructed XAUUSD** lifecycles only. Trade #3 unlocks early score / SHADOW — **never LIVE**.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Counter | `TradeReconstructor.cs` L47–63 | `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` (`>= 3`). |
| Persist count | `ReconstructionScoringService.cs` L85–95 | `CompletedXauTrades = score.Features.CompletedXauTrades`. |
| Fixture 10001 | `DemoBrokerFactory.BuildAchieverDeals` L133–135 | Three closed XAU round-trips. |
| Integration | `SeedingAndStoreTests.cs` L31–33 | Login 10001 `CompletedXauTrades == 3`; state is **not** `LIVE`. Login 10002 (martingale fixture) is `RISK_BLOCKED`. |
| Unit | `TradeReconstructionTests.First_three_completed_xau_unlocks_early_score` | 3 completed → eligible. |
| UI | `TraderDetailPage.tsx` L14, L22 | Shows completed XAU; copy says first 3 unlock EARLY_SCORE / SHADOW only. |
| State machine | `BaselineScorer.cs` `TraderStateMachine.CanPromoteToLive` L210–211 | **Always false.** Correct for this bar. |

Not accepted: first-3 is proven only on the four demo logins, not on a durable counter over live books.

### Done when

A trader with 0 / 2 / 3 completed XAU lifecycles is classified correctly after restart from persisted deals; trade #3 never writes `LIVE`.

---

## 9. Item 7 — Produce a deterministic trader/risk score

**Accepted: No. Score: DEMO.**

### Law

§69.7 + §18 / A22. Deterministic `baseline.v1`. `mlProbability` may be null. No XGBoost.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Scorer | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | Features (net, PF, lot CV, martingale, averaging, lot escalation, hold, SL rate, DD) → risk / behavior / early quality → `TraderStateMachine`. |
| Early gate | L40, L132 | `EarlyScoreTradeCount = 3`; `< 3` caps quality and stays `INSUFFICIENT_DATA`. |
| Persist | `EfTradingStore.UpsertScoreAsync` L215–248 | Current row + `trader_score_history`. |
| Unit | `tests/Unit/BaselineScorerTests.cs` | 2 trades insufficient; 3 winners → **SHADOW not LIVE**; martingale → `RISK_BLOCKED`. |
| ML | `EfDashboardQueries.cs` L100; `TraderRowDto` | `mlProbability` is **null**. `D:\Prop\services` empty (correct). |
| UI | `ScoringPage.tsx` | “XGBoost is not active.” Table binds early / risk / state. **`behaviorScore` is not on `TraderRowDto`** — column will render `0`. |

Deterministic path exists and is the production ranker in the demo. It is not accepted until it scores **captured** (item 4) reconstructed (item 5) books, not only `Closed(1,80)` fixtures.

### Done when

Persisted scores are a pure function of persisted completed XAU trades; same deals → same numbers; ML remains null; trade #3 + high score is SHADOW only.

---

## 10. Item 8 — Rank traders

**Accepted: No. Score: DEMO.**

### Law

§69.8 + §50 / A92. Leaderboard of deterministic scores.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Query | `EfDashboardQueries.GetTradersAsync` L74–117 | Loads all scores, maps broker code, optional broker/state filter, **`OrderByDescending(t => t.EarlyScore)`**. |
| API | `apps/api/Program.cs` L57–60 | `GET /api/traders`, `GET /api/traders/{broker}/{login}`. Not `/api/v1`, no A92 envelope. |
| UI | `apps/web/src/pages/TradersPage.tsx` | Table: broker, login, group, XAU trades, net, early, risk, flags, state. Links to detail. |
| Population | demo seed | At most **4** scored logins (10003 has 0 deals → `INSUFFICIENT_DATA`). |

Ranking **works on the demo set**. It is not a useful ranker of the ~5k book.

### Done when

`GET /api/v1/traders` (A92) returns the scored population ordered by the documented sort, including filters, without fabricating ML.

---

## 11. Item 9 — Connect to cTrader QUOTE FIX securely

**Accepted: No. Score: FAIL.**

### Law

§69.9 + §§25–27, 31. Independent QUOTE session, TLS, issued Comp/Sub IDs, `TargetCompID = cServer` (case preserved). TRADE send is **not** required.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| No session type | `D:\Prop\src\Fix.CTrader\` | Four sources: options, parser, in-memory ownership, string harness. **No** `CTraderQuoteSession`. |
| No engine | `TraderIntelligence.Fix.CTrader.csproj` | **No** `QuickFIXn.Core` / `QuickFIXn.FIX44`. No `using QuickFix` anywhere. |
| Options only | `CTraderFixOptions.cs` L10–57 | Host `live-us-eqx-01.p.c-trader.com`, QUOTE port 5211, `TargetCompId = "cServer"`, `UseSsl = true`. Password empty. `TradeSessionEnabled = true` (should stay dark for FUV). |
| Seeded lie | `DemoSeeder.cs` L68–85 | Inserts QUOTE row `Status = ReadyForMarketData`, TRADE `LoggedOn`, seq 1, timestamps = now. |
| Worker lie | `apps/fix-worker/Worker.cs` L28–40 | Every 15 s: `LastInboundAt = UtcNow`; QUOTE → `ReadyForMarketData`; TRADE → `LoggedOn` **regardless of `real`**. No socket. |
| Dashboard | `EfDashboardQueries.GetOverviewAsync` L40–41; `GetFixSessionsAsync` L125–147 | Treats those statuses as healthy / logged on. |
| API config | `apps/api/appsettings.json` L12–20 | Host + account `1369850` + empty password + `RealCopyExecutionEnabled: false`. |

`cServer` case is preserved (good). A TLS Logon never happens.

### Done when

QUOTE initiator completes Logon on TLS 5211 with issued IDs; `fix_sessions` status is driven by the session, not a timer; password never logged or returned to React; TRADE may remain disconnected / “not started.”

---

## 12. Item 10 — Discover the Pepperstone XAUUSD instrument ID

**Accepted: No. Score: FAIL.**

### Law

§69.10 + §30 / A86. Discover via `35=x` / `35=y`. **Never hardcode tag 55.**

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Seeded quote | `DemoSeeder.cs` L103–111 | `VenueInstrumentId = null`. Bid/ask invented `2399.45` / `2399.85`. |
| Dashboard | `EfDashboardQueries.cs` L142 | Surfaces `quote?.VenueInstrumentId` (null). |
| UI | `FixSessionsPage.tsx` L19 | Renders `not discovered yet` when null. Honest for this field. |
| Forbidden fixture | `FixSimulationHarness.SimulateSecurityList` L129–143 | Hard-codes `(55, "123456")` and `(1007, "XAUUSD")`. A86 forbids treating this as the venue ID. |
| Normalizer | `SymbolNormalizerTests.cs` L26–28 | `TryMapVenueInstrumentId("123456")` is false until `RegisterVenueInstrument` — unit-only. |
| No request | no `SecurityListRequest` builder, no QUOTE session | Discovery cannot run. |

### Done when

A SecurityList (live or recorded) yields a **persisted** numeric instrument id for Pepperstone XAUUSD; tag 55 in later MD/ER is that discovered id; `123456` is not written to `destination_quotes`.

---

## 13. Item 11 — Shadow-copy selected traders using destination quotes

**Accepted: No. Score: FAIL.**

### Law

§69.11 + §24 / A24 / A72. Selected SHADOW traders; fills from **destination QUOTE** only; OPEN/INCREASE vs REDUCE/CLOSE policies; fail closed on stale/wide quotes.

### Evidence

| Claim | File | What the file actually does |
|---|---|---|
| Engine | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | In-memory `SimulateEntry` / `SimulateExit` / `MarkToMarket`. Takes bid/ask from a `DestinationQuote` DTO. Mutates price by `0.05` if delay > 250 ms. **No quote guards.** |
| Callers | `grep ShadowCopyEngine` in `*.cs` | **Definition only.** Not registered in DI. Not called from worker, seeder, or API. |
| Table | `TraderDbContext` `ShadowOrders`; `EfDashboardQueries` L21 | Sums `SourceVsShadowSlippage` (empty → 0). |
| Seeded dest quote | `DemoSeeder.cs` L103–111 | One snapshot, **null** instrument id, invented bid/ask. |
| UI | `ShadowPortfolioPage.tsx` | Static paragraph. No table. Says shadow orders appear only after approved CopyIntent — **no CopyIntent pipeline writes them.** |
| Risk engine | `Domain\Risk\RiskEngine.cs` | Separate `DestinationQuote` record. Unused by shadow pipeline. |
| B18 | `B18_shadow_review.md` | Engine is a taker-touch calculator, not a dest-quote authority. |

### Done when

Traders in `SHADOW` emit persisted shadow fills priced from a **usable** dest QUOTE snapshot (age/spread/move guards), marked to that tape, visible on the Shadow page. Source ticks are never the fill price.

---

## 14. Item 12 — Show all of this in React

**Accepted: No. Score: PARTIAL.**

### Law

§69.12 + §§46–54. The dashboard must **show items 1–11**. A Vite route table over missing or lying APIs is not enough. SignalR is recommended, not a gate (A06).

### Evidence — what exists

| Page | Path | Bound to |
|---|---|---|
| Overview | `apps/web/src/pages/OverviewPage.tsx` | `GET /api/overview` — demo counts + liar MT5/FIX health |
| Brokers | `BrokersPage.tsx` | `GET /api/brokers` — always “connected” |
| Groups | `GroupsPage.tsx` | `GET /api/groups` — 4 fixture groups |
| Traders | `TradersPage.tsx` | `GET /api/traders` — ranked demo scores |
| Trader detail | `TraderDetailPage.tsx` | `GET /api/traders/{broker}/{login}` — same DTO; **no first-3 trade block** (A93) |
| Trades | `TradeExplorerPage.tsx` | `GET /api/trades` — reconstructed demo trades |
| Scoring | `ScoringPage.tsx` | Reuses traders; `behaviorScore` missing on wire |
| FIX | `FixSessionsPage.tsx` | `GET /api/fix/sessions` — seeded/stamped sessions; instrument “not discovered yet” |
| Risk | `RiskPage.tsx` | `GET /api/risk` — kill switch + zeros |
| Health | `SystemHealthPage.tsx` | `GET /api/health` JSON dump (hardcoded) |
| Recon | `ReconciliationPage.tsx` | `GET /api/reconciliation/status` — **static zeros**, not DB |
| Settings | `SettingsPage.tsx` | `GET /api/settings` — flags + broker ids |
| Shadow | `ShadowPortfolioPage.tsx` | **No API** |
| Live | `LiveCopyPage.tsx` | Static “flag false” (correct for live, not a §69 show) |
| Audit | `AuditPage.tsx` | **No API** |

Router: `apps/web/src/App.tsx` (15 routes). Nav: `DashboardLayout.tsx` (14 labels). Hooks: `apps/web/src/api/hooks.ts` → unversioned `/api/*`.

### Evidence — why it is not item 12

1. Items 1, 3, 9, 10, 11 are not true, so they cannot be shown.
2. Where they are “shown,” the values are **wrong** (connected, QUOTE healthy, 4 accounts as if that were the book).
3. No `/api/v1`, no RBAC, no login page, no SignalR hub (`signalr.ts` → `/hubs/dashboard`; `Program.cs` maps none).
4. B10 called this a thin demo BFF binding. Pages grew (Live + Audit stubs; 15 vs B10’s 13) but the contract did not become A26.

### Done when

An operator can open React and see **true** dual-broker health, discovered groups, ~5k account counts, captured/reconstructed XAU, first-3, baseline ranks, QUOTE logon, discovered instrument id, and shadow fills — without a green badge that the process invented.

---

## 15. What is correctly **not** required

| Topic | Status | Pin |
|---|---|---|
| ML / XGBoost / `services/ml-service` | Empty `D:\Prop\services` | §69 first sentence; A52 / A104 |
| Live `NewOrderSingle` | No send path | §70 / §69 (out of bar); flag false in API settings |
| Kafka, K8s, ClickHouse, LLM, DNN, RL | Absent | §71 / A80 |
| Emergency flatten from React | Live page is a stub | A06 / A48 |

Absence of ML is **PASS for this bar**, not a gap.

---

## 16. Delta vs A57 (same day, later tree)

A57 measured empty Application `Class1`, broken EF plural types, weatherforecast API, 1 Hz workers, 0 React page files, 0 tests.

| Area | A57 | C13 (now) |
|---|---|---|
| Accepted §69 | 0/12 | **still 0/12** |
| Fake dual-broker ingest | missing | **present** (18 deals / 4 accounts) |
| Group/account/deal persist | EF did not compile | **compiles**; InMemory / EnsureCreated |
| Reconstructor / scorer / rank | in-memory only | **persisted + unit/integration** |
| React pages | 0 files | **15 modules**, demo-bound |
| Live MT5 / QUOTE / instrument / shadow pipeline | missing | **still missing** |
| Health honesty | N/A (nothing to lie about) | **worse** — dashboard claims connected / logged-on |

Progress is real. Acceptance is not.

---

## 17. Flip list (minimum to move each cell)

| # | Flip to PASS |
|---|---|
| 1 | Two real `IMt5BrokerConnector` instances (HTTP bridge or native host) with measured Connect; dashboard reads `IsConnectedAsync`. |
| 2 | Startup `GetGroups` from Manager on both brokers → upsert all names. |
| 3 | Checkpointed account backfill; measured ~5k; unique `(broker_id, login)`. |
| 4 | Checkpointed `GetDeals`; persist native tickets; map observed aliases; no invented ticks. |
| 5 | Same reconstructor on those deals; A21 replay green. |
| 6 | Durable completed-XAU count; #3 ≠ LIVE. |
| 7 | `BaselineScorer` job on persisted trades; tests locked. |
| 8 | A92 leaderboard over that population. |
| 9 | QuickFIXn 1.14.1 QUOTE TLS Logon; status from session. |
| 10 | Persist SecurityList XAU id; never `123456`. |
| 11 | Shadow pipeline writes fills from dest quotes with A24/A72 guards. |
| 12 | React binds the true read models for 1–11; delete health lies. |

Do not enable TRADE `NewOrderSingle` as part of this list.

---

## 18. Honesty line

**First useful version accepted: 0 / 12.**  
**Demo pipeline (fake MT5 → reconstruct → baseline score → rank → some React tables): items 2, 4, 5, 6, 7, 8.**  
**Live Achiever, live StarwaveFX, live Pepperstone QUOTE, discovered tag 55, and destination-quote shadow copy: not present.**  
**ML: correctly not built.**  
**Live copy: off by absence.**

C13 does not authorize product edits. Re-score after a live (or recorded-replay) transport exists; do not increment the accepted count for more demo rows.
