# D42 — Architecture §68 go-live gates scored vs current tests

| Field | Value |
|---|---|
| Agent | D42 (go-live re-score vs current tests only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:39:47+05:30 (hashes); tests re-run through 2026-08-18T08:11Z |
| Artifact | `D:\Prop\reports\swarm\20260818\D42_gates_now.md` |
| Assigned | Score §68 gates honestly vs current tests. Write this file. Do not modify product source. |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§68** (lines 2605–2628) |
| Supporting | §60 test strategy (lines 2228–2273), §69, §70, §72; A27 inventory; A100 / C14 prior live scores |
| Product source modified | **No.** This report is the only write. |

**Law (verbatim §68):** Do not enable real copying until **all** of these are true.

**Scoreboard (re-measured 2026-08-18):** **0 PASS / 19 FAIL** for the live-copy license. Same integer as A100 and C14.

A green `dotnet test` is **not** 19/19. One FAIL blocks `REAL_COPY_EXECUTION_ENABLED`. This file does not flip any checkbox.

---

## 0. Verdict

**All 19 Architecture §68 gates remain FAIL for live.**

What changed since C14 is **lab surface**, not venue proof:

| Bar | A100 | C14 | D42 (this file) |
|---|---|---|---|
| §68 live-copy license | **0 / 19 PASS** | **0 / 19 PASS** | **0 / 19 PASS** |
| Checkboxes that may be `[x]` for live | none | none | **none** |
| Unit executed | 0 `[Fact]` (stale) | 61 pass / 22 skip / 83 | **64 pass / 22 skip / 86** |
| Integration executed | placeholder | 3 pass (incl. `Assert.True(true)`) | **3 pass** when Infrastructure compiles |
| `REAL_COPY_EXECUTION_ENABLED` | default false | default false | still false; still no send function |
| Live `35=D` possible if process starts | No (absence) | No (absence) | **No (absence)** |

**Do not greenwash these later facts into PASS:**

1. Unit **64 passed / 22 skipped / 0 failed** is Domain + FakeMt5 math. It is not Achiever / StarwaveFX / Pepperstone proof.
2. Integration **3 passed** is EF InMemory + canned FakeMt5 deals + `PlaceholderRemoved.Integration_project_loads` → `Assert.True(true)`.
3. Twenty-two skips are **intentional A43 holes**. A skip is not a converter.
4. Four passing conversion facts **lock passthrough** (`0.10` lots → `0.10` qty). That is anti-evidence for G10.
5. Six reconstruction facts (including a new canceled-deal first-3 case) are synthetic `NormalizedDeal` tapes. They are not A21/A27 venue fixtures.
6. Five `RiskEngineTests` facts call a method **no Application / worker / API path** invokes.
7. Mid-swarm, Infrastructure **failed to compile** (entity/API drift: `FixSessionState` ctor, missing `CopyIntent.IdempotencyKey`, leftover `ReconstructedTrades` type). A later retry compiled and the 3 integration facts passed. Flux is **not** a gate.

Vacuous / demo law (binding): Fake connector, in-memory DB, unused method, skipped test, seeded rows, or a recovered compile **cannot** become PASS.

```text
REAL_COPY_EXECUTION_ENABLED = false     -- stays false
CTrader:RealCopyExecutionEnabled        -- worker default false
Live 35=D to *.c-trader.com             -- FORBIDDEN until 19/19 §68 + 14/14 §70 + explicit prod review
```

---

## 1. Method

Read, hashed, grepped, and tested. Did not edit product source.

| Source | Path / action |
|---|---|
| Architecture §68 / §60 / §70 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Prior live scorecards | `A100_golive_gates.md`, `C14_golive_still_fail.md` |
| Required class names | `A27_test_inventory.md` §4–§7 |
| Unit coverage predecessor | `C17_unit_coverage.md` (60/1/22/83 — **stale counts**) |
| Product | `src/`, `apps/`, `tests/` (still **no** `tests/Fix`, **no** `tests/Replay`) |
| Measured tests | `dotnet test` Unit + Integration, `--nologo --verbosity minimal` |

Commands:

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity minimal
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity minimal
```

### 1.1 Measured `dotnet test` (authoritative for this file)

| Project | When | Total | Passed | Failed | Skipped | Exit |
|---|---|---:|---:|---:|---:|---:|
| `TraderIntelligence.Tests.Unit` | first this pass | 85 | 63 | 0 | 22 | 0 |
| `TraderIntelligence.Tests.Unit` | after `Canceled_deal_on_a_position_excludes_it_from_first_three` landed | **86** | **64** | **0** | **22** | **0** |
| `TraderIntelligence.Tests.Integration` | mid-edit compile | — | — | compile | — | **1** |
| `TraderIntelligence.Tests.Integration` | after entities restabilized | **3** | **3** | **0** | **0** | **0** |

**Use the last green rows:** Unit **64 / 22 / 86**, Integration **3 / 0 / 3**.

First Integration compile failures observed this pass (do not treat as a permanent product claim; they prove the tree was in flux):

- `CS8858` `with` on `ReconstructedTradeResult` while it was briefly a `class` (later became `record`; `EligibleForFirstThree` exists).
- `CS0246` leftover `IEntityTypeConfiguration<ReconstructedTrades>` (file later gone).
- Transient `FixSessionState` positional-ctor vs object-initializer mismatch; `CopyIntent` missing `IdempotencyKey` / `RequestedQuantity`; `OutboxEvent` missing `Type`.

A later Integration run built Infrastructure and passed 3/3. That recovery is **not** G01–G19.

### 1.2 What the 86 Unit cases actually are

| Class | Executed pass | Skip | What it locks |
|---|---:|---:|---|
| `SmokeTests` (`UnitTest1.cs`) | 1 | 0 | Domain assembly loads |
| `TradeReconstructionTests` | **6** | 0 | Synthetic XAU tapes: round-trip, scale-in+partial+avg-down, `InOut` reverse, first-3 count, **canceled deal excludes first-3**, ignore balance |
| `DealReasonTests` | 2 | 0 | `Rollover` is not a trading deal; `Client` counts, `Migration` does not |
| `BaselineScorerTests` | 3 | 0 | 2 trades insufficient; 3 winners → SHADOW not LIVE; martingale → RISK_BLOCKED |
| `RiskEngineTests` | 5 | 0 | `QUOTE_STALE`, flag-false `AllowFixSend`, stop-new open vs close, unreconciled, `SIGNAL_STALE` — all on a **dead** `Evaluate` |
| `ExecutionAndSizingTests` | 6 | 0 | SM unknown-ack / disconnect / FILL; last-stage step/min; ClOrdId prefix; expiry helper |
| `SymbolNormalizerTests` | 6 | 0 | 5 hardcoded aliases + refuse unregistered venue id |
| `VolumeConverterTests` | 3 | 0 | Manager 10_000 scale; Extended; hundredths is not default |
| `Normalization.SourceDestinationQuantityConversionTests` | **4** | **21** | Passing facts **assert passthrough** `0.10`; A43 E01–E46 / G7 converter cases are `[Skip]` |
| `Sizing.QuantityNormalizerStepMinMaxTests` | 28 | 1 | Last-stage floor/min/max on already-lots input; skip is unaligned-max re-floor |
| **Unit total** | **64** | **22** | |

Theory expansion used: `Maps_known_aliases` = 5; `Floors_to_step` = 7; three throw theories = 3+3+2. Skipped `Known_lot_to_OrderQty_examples` counts as **1** skip (theory not expanded).

### 1.3 What the 3 Integration cases actually are

| Class | Result | What it locks |
|---|---|---|
| `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` | pass | FakeMt5 + InMemory: 2 brokers, groups, canned deals, login `10001` has 3 completed XAU, `10001` not LIVE, `10002` RISK_BLOCKED, 2 FIX rows `TargetCompId == cServer` |
| `SeedingAndStoreTests.Deal_upsert_is_idempotent` | pass | Select-then-insert on **EF InMemory** (unique index **not** enforced) |
| `PlaceholderRemoved.Integration_project_loads` | pass | `Assert.True(true)` |

Absent projects (A27 / §60 / §61): `tests/Replay`, `tests/Fix`. Zero QuickFIX package on `TraderIntelligence.Fix.CTrader.csproj`.

---

## 2. Evidence files (SHA-256 at 2026-08-18T13:39:47+05:30)

| SHA-256 | Path |
|---|---|
| `0B3C0EDC09081C25D097FF0E6AADC7A638562EBB8DB345DC325DC54EC904D37E` | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | `D:\Prop\apps\fix-worker\Worker.cs` |
| `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `D:\Prop\apps\api\Program.cs` |
| `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` |
| `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` |
| `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| `EF41E7743A411EFE74A25611CE2C161940C31BD5CB8811715F0E81F8EFB687BA` | `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` |
| `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` |
| `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` |
| `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` |
| `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `D:\Prop\tests\Unit\SymbolNormalizerTests.cs` |
| `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` |
| `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` |
| `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` | `D:\Prop\tests\Unit\DealReasonTests.cs` |
| `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |

---

## 3. Working checklist (copy of §68) — live status

```text
[ ] FAIL  G01  MT5 historical/live ingestion is stable
[ ] FAIL  G02  duplicate event handling is proven
[ ] FAIL  G03  trade reconstruction tests pass
[ ] FAIL  G04  XAU symbol mappings are verified
[ ] FAIL  G05  quote session stable
[ ] FAIL  G06  trade session stable
[ ] FAIL  G07  cTrader reconciliation works after restart
[ ] FAIL  G08  copy intents are idempotent
[ ] FAIL  G09  unknown execution state recovery works
[ ] FAIL  G10  position sizing conversion is verified
[ ] FAIL  G11  risk engine unit/integration tests pass
[ ] FAIL  G12  stale quote rejection works
[ ] FAIL  G13  stale signal rejection works
[ ] FAIL  G14  shadow copy has sufficient sample
[ ] FAIL  G15  destination costs / slippage measured
[ ] FAIL  G16  kill switch tested
[ ] FAIL  G17  secrets removed from repo / logs
[ ] FAIL  G18  dashboard exposes venue health / risk
[ ] FAIL  G19  manual review completed
```

**Enable live copy only when:** `19/19 PASS` **and** `14/14` §70 **and** explicit production flag reviewed. Default remains OFF.

How this file scores:

| Column | Meaning |
|---|---|
| **LIVE** | §68 license. Vacuous / demo / unused / skip **cannot** be PASS. |
| **Tests vs gate** | What on-disk xUnit actually proves for that sentence. `NONE` / `PARTIAL` / `ANTI` / `GREEN_INSUFFICIENT`. Never upgrades LIVE. |

---

## 4. Required test surface vs what exists (A27 / §60)

§68 is not “any green test.” Several gates *name* tests. Those names map to A27 / §60 classes that are still missing.

| Required cluster (§60 / A27) | On disk now | Executed? |
|---|---|---|
| `Reconstruction.Mt5DealDeduplicationTests` | **Missing** (only InMemory upsert fact) | n/a |
| `TradeReconstructionTests` + 6 sibling recon classes | **One** class, 6 facts (A27 wanted 7 dedicated classes) | 6 pass |
| `Mapping.XauCanonicalMappingTests` | `SymbolNormalizerTests` only | 6 pass |
| `Sizing.SourceDestinationQuantityConversionTests` | Present; **21 Skip** + 4 passthrough facts | 4 pass / 21 skip |
| `Risk.RiskEngineHardLimitTests` + 9 siblings | Collapsed into 5 `RiskEngineTests` facts | 5 pass |
| `Execution.CopyIntentIdempotencyTests` | **Missing** | n/a |
| `Execution.UnknownExecutionStateTests` | 2 SM facts in `ExecutionAndSizingTests` | 2 pass |
| `Fix.*` unit + integration | **Missing** (`tests/Fix` absent; no QuickFIX NuGet) | n/a |
| `Reconcile.PositionReconciliationTests` / `UnknownExecutionRecoveryTests` | **Missing** | n/a |
| `Mt5.Mt5BackfillRestartTests` / `Mt5LiveIngestIdempotencyTests` | **Missing** | n/a |
| `Persistence.PostgreSqlMigrationTests` | **Missing** (still `EnsureCreated`) | n/a |
| Entire `tests/Replay` project | **Missing** | n/a |

C17’s “0 of 17 §60 areas COVERED” still holds. D42 does not reopen that census except where a §68 sentence names tests.

---

## 5. Per-gate live score vs current tests

### G01 — MT5 historical/live ingestion is stable — **FAIL**

| | |
|---|---|
| §68 text | `MT5 historical/live ingestion is stable` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** (no backfill/restart/live-ingest class) |
| Earliest phase | 1 |

**Tests that exist:** none of `Mt5BackfillRestartTests`, `Mt5LiveIngestIdempotencyTests`, `DualBrokerIsolationTests`. Integration seed pulls **FakeMt5** canned deals for four logins.

**Product (not a test):** `AddTraderIntelligence` always registers `DemoBrokerFactory.CreateDefault()`. `apps/mt5-worker/Worker.cs` syncs those fakes every 30 s. Missing connection string → InMemory. Hosts call `EnsureCreated`. No C# Manager P/Invoke.

**PASS when:** Achiever **and** StarwaveFX stay connected across reconnect; groups/accounts discovered; history backfill restart-safe; live deals persist before async work; measured ~5k path; **not** the fake factory. CI class names in A27 §5.

---

### G02 — Duplicate event handling is proven — **FAIL**

| | |
|---|---|
| §68 text | `duplicate event handling is proven` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — `Deal_upsert_is_idempotent` only |
| Earliest phase | 1 |

**Tests:** `SeedingAndStoreTests.Deal_upsert_is_idempotent` inserts the same `Mt5DealDto` twice on EF InMemory; second `UpsertDealAsync` returns `false`; row count = 1.

**Why not proven:** InMemory does **not** enforce the fluent unique index `(BrokerId, DealTicket)`. No concurrent insert. No PostgreSQL. No FIX ER replay. No restart-vs-broker-history. No `Mt5DealDeduplicationTests`.

**PASS when:** Replaying the same deal/event stream (and FIX ER later) does not create duplicate rows; tested in CI against PostgreSQL; source ledger matches broker history after restart.

---

### G03 — Trade reconstruction tests pass — **FAIL** (easiest false-green)

| | |
|---|---|
| §68 text | `trade reconstruction tests pass` |
| LIVE | **FAIL** |
| Tests vs gate | **GREEN_INSUFFICIENT** — 6/6 recon facts + 2 deal-reason facts are green |
| Earliest phase | 2 |

**What is green (do not ignore):**

| Fact | Locks |
|---|---|
| `Reconstructs_simple_round_trip` | one completed long XAU, 0.10 lots, PnL 100 |
| `Scale_in_and_partial_close` | scaled, partial, **avg-down polarity** (2290 < 2300 long) |
| `Reverse_inout_closes_then_opens_opposite` | `InOut` → completed long + open short |
| `First_three_completed_xau_unlocks_early_score` | count = 3 → early-score true |
| `Canceled_deal_on_a_position_excludes_it_from_first_three` | **new vs C14**: 3 completed books, first-3 count = 2 |
| `Ignores_balance_deals` | balance → empty |
| `Rollover_is_not_a_trader_lifecycle_deal` | `DealReason.Rollover` not `IsTradingDeal` |
| `Client_buy_still_counts` | enum helper only |

**Why LIVE is still FAIL:**

- Inputs are hand-built `NormalizedDeal` (`SourceSymbol = "XAUUSDm"`), not Manager history.
- A27 still missing dedicated partial/full/reversal/first-3/XAU fixture **classes** and recorded venue files.
- C31 (adversarial, not in `tests/`): zero-volume still skipped with no dirty flag; that edge has **no** unit fact.
- Integration success is FakeMt5 seed, not Achiever/Starwave tickets.
- Mid-pass `TradeReconstructor` briefly used `with` on a non-record type (`CS8858`). Current result type is a `record` with `EligibleForFirstThree`. Flux ≠ coverage.

**PASS when:** A27 reconstruction fixtures pass in CI on recorded/live-shaped deals (completed XAU, first-3, reversals, volume units, cancel/zero-volume dirty). Demo seeder success is not a test.

---

### G04 — XAU symbol mappings are verified — **FAIL**

| | |
|---|---|
| §68 text | `XAU symbol mappings are verified` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — `SymbolNormalizerTests` only |
| Earliest phase | 2 / 4 |

**Tests:** five aliases (`XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`) map to `XAUUSD`; `TryMapVenueInstrumentId("123456")` is false until `RegisterVenueInstrument`.

**Missing:** `SourceSymbolMappingTests`, `DestinationInstrumentMappingTests`, live Achiever/Starwave symbol confirmation, cTrader Security List, persisted numeric instrument id. Seeded dest quote still has `VenueInstrumentId = null`. Guessing FIX `55=XAUUSD` remains forbidden (§72.13) and **untested** against a session.

**PASS when:** Source aliases confirmed on both MT5 brokers; Pepperstone instrument ID **discovered** and stored; tests reject guessed `55=XAUUSD`.

---

### G05 — Quote session stable — **FAIL**

| | |
|---|---|
| §68 text | `quote session stable` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 4 |

**Tests:** zero. `tests/Fix` absent. No QuickFIX package. No `CTraderQuoteSession` tests.

**Product honesty (still not stability):** `apps/fix-worker/Worker.cs` (SHA `92A8F492…`, D32) now writes QUOTE `Status = Disconnected` and `LastError = "No live QUOTE socket. Simulator/demo only."` every 15 s. Seeder plants the same. `/api/health` FIX QUOTE `healthy = false`. That removes the C14 `LoggedOn` lie. A disconnected stamp is **not** a stable SSL session.

**PASS when:** Independent SSL QUOTE session stays logged on across reconnect; heartbeats real; dashboard age from last **venue** quote; A27 QuickFIX session tests pass.

---

### G06 — Trade session stable — **FAIL**

| | |
|---|---|
| §68 text | `trade session stable` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 7 |

**Tests:** zero TRADE initiator / seq-file / reconnect facts.

**Product:** same worker stamps TRADE `Disconnected` / `"No live TRADE socket. NewOrderSingle remains off."` Flag `CTrader:RealCopyExecutionEnabled` is log-only. No `CTraderTradeSession`. Absence of `35=D` is a control, not session stability.

**PASS when:** Independent SSL TRADE logon is stable; seq files persist; disconnect/reconnect proven. Send remains flagged off until G01–G19 + §70.

---

### G07 — cTrader reconciliation works after restart — **FAIL**

| | |
|---|---|
| §68 text | `cTrader reconciliation works after restart` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 7 |

**Tests:** no `PositionReconciliationTests`, `StartupReconciliationGateTests`. Grep of product still has no `OrderMassStatus` / `RequestForPositions` client in the paths scored here.

**Anti-test:** `GET /api/reconciliation/status` returns **now + zeros** (`unknownPositions = 0`, `mismatches = 0`) with no store read.

**PASS when:** After process restart, mass-status + positions reconcile to `destination_positions`; inconsistent book **blocks** `READY_FOR_EXECUTION`; A27 integration class exists and is green on a recorded book.

---

### G08 — Copy intents are idempotent — **FAIL**

| | |
|---|---|
| §68 text | `copy intents are idempotent` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** (no `CopyIntentIdempotencyTests`) |
| Earliest phase | 5 / 8 |

**Tests:** zero facts that insert the same source event twice and assert one intent / one order.

**Product (C59 is stale):** `EfTradingStore` now constructs `CopyIntent` with `IdempotencyKey = $"shadow:{brokerId}:{login}:{trade.PositionId}"` and skips if the key exists, then writes a `ShadowOrder`. That path runs only when scorer state is `SHADOW`, uses `RequestedQuantity = trade.MaxVolumeLots` (lots passthrough), and is **not** covered by any test. Unique index may exist on the fluent model; Integration never asserts it. Mid-pass the entity briefly lost `IdempotencyKey` and Infrastructure failed to compile.

**PASS when:** Same source event cannot insert a second intent or fire a second order; persist-before-send; unique key proven under retry/crash (A42).

---

### G09 — Unknown execution state recovery works — **FAIL**

| | |
|---|---|
| §68 text | `unknown execution state recovery works` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — state-machine helpers only |
| Earliest phase | 7 / 8 |

**Tests:**

- `Unknown_ack_cannot_retry_new_order` — after send, `MayRetryNewOrderSingle == false`, `RequiresReconciliation == true`
- `Disconnect_after_send_is_unknown_state` — `AfterDisconnectWithUnknownAck() == ExecutionStateUnknown`
- `Filled_report_is_terminal` — one FILL → `Filled`

No recovery service, no OrderStatus / MassStatus path, no test that drives send+disconnect+recover. A27 `UnknownExecutionRecoveryTests` **missing**.

**PASS when:** After send+disconnect the intent is `EXECUTION_STATE_UNKNOWN`; **no** blind `NewOrderSingle` retry; recover via status/ER/positions; tested.

---

### G10 — Position sizing conversion is verified — **FAIL** (tests are anti-evidence)

| | |
|---|---|
| §68 text | `position sizing conversion is verified` |
| LIVE | **FAIL** |
| Tests vs gate | **ANTI** |
| Earliest phase | 5 / 8 |

**Passing tests lock the wrong contract:**

```csharp
_n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().Be(0.10m);
_n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().NotBe(10.00m);
```

A43 E01 requires **10.00** OrderQty (0.10 lots × 100 oz). `Never_passthrough_MT5_lots` is `[Fact(Skip = …)]`. Twenty more converter fixtures skip. One step test skips unaligned-max re-floor.

`QuantityNormalizer` is last-stage `sourceLots * allocation` floor. No contract size, no ticks, no convention. Product callers outside tests: **none** on the send/shadow path except the store writing `MaxVolumeLots` straight into `RequestedQuantity`.

**PASS when:** Known source-lot → dest-qty fixtures **execute and pass** (not skip); `Never_passthrough_MT5_lots` fails any `requested_quantity = source_lots` shortcut; dest spec comes from the venue.

---

### G11 — Risk engine unit / integration tests pass — **FAIL**

| | |
|---|---|
| §68 text | `risk engine unit/integration tests pass` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — 5 unit facts; 0 integration |
| Earliest phase | 8 |

**Green facts:** stale quote, `RealExecutionEnabled=false` never `AllowFixSend`, stop-new blocks open / approves close, unreconciled, stale signal.

**Why the sentence is still false:**

- `grep Evaluate(` under product `*.cs` hits **only** the definition. `AddTraderIntelligence` does **not** register `RiskEngine`.
- A27 hard-limit / flatten-auth / feature-flag / quote-guard **classes** are missing.
- C33: `EmergencyFlatten` has **zero** facts; flatten does not close dest ids; loss/DD checks freeze exits.
- No spy that outbound `35=D` count = 0, because there is no send function.
- No integration suite.

Five greens on a dead method are not “unit/integration tests pass” for live.

**PASS when:** A23/A27 unit + integration suite passes: each hard limit, quote/signal stale, reduce vs open, kill switch, recon block, **zero** FIX outbound on reject, hook proven on the real worker pipeline (InProcess venue).

---

### G12 — Stale quote rejection works — **FAIL**

| | |
|---|---|
| §68 text | `stale quote rejection works` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — `Stale_quote_rejects_open` only |
| Earliest phase | 4 / 8 |

**Test:** constructs a 30 s-old `DestinationQuote` DTO; `Evaluate` returns `QUOTE_STALE`. Thresholds are compile defaults (`RiskLimits.MaxQuoteAge = 3s` vs `CTraderFixOptions.MaxQuoteAgeMs = 5000`). Nothing on Application / workers calls `Evaluate`. No destination quote feed. FIX worker no longer forges `LastInboundAt` (good) but also has **no** quote age.

**PASS when:** `quote_age > configured_max` rejects OPEN/INCREASE on **live and shadow**; config not compile constants; `QuoteFreshnessGuardTests` pass; logged-on ≠ fresh.

---

### G13 — Stale signal rejection works — **FAIL**

| | |
|---|---|
| §68 text | `stale signal rejection works` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** |
| Earliest phase | 5 / 8 |

**Tests:** `Stale_signal_rejected` (`SourceEventTime` −5 min → `SIGNAL_STALE`); `Copy_intent_expires` (16 s > 15 s window). Neither is on a send/shadow path. Store-written shadow intents set `ExpiresAt = trade.OpenedAt.AddSeconds(15)` from **historical** open time — they are already expired at seed. No pipeline test. Reduce/close vs open-more (§72.17–18) is not tested on a writer.

**PASS when:** Expired `CopyIntent` cannot open more; tested on the pipeline; reduce/close not treated as open-more.

---

### G14 — Shadow copy has sufficient sample — **FAIL**

| | |
|---|---|
| §68 text | `shadow copy has sufficient sample` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 5 |

**Tests:** zero `ShadowCopyEngine` / replay-shadow facts. `tests/Replay` absent.

**Product:** store now calls `SimulateEntry` for SHADOW traders against the latest `DestinationQuotes` row (seeded snapshot bid/ask, `VenueInstrumentId = null`). Four demo logins are not a go-live sample (A24). No agreed window/size stored. Engine still fills a crossed / stale book (B18/D16).

**PASS when:** Selected traders shadow-copied on **destination** quotes; sample size and window agreed and stored; source-vs-shadow analysis exists before any live copy.

---

### G15 — Destination costs / slippage measured — **FAIL**

| | |
|---|---|
| §68 text | `destination costs / slippage measured` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 5 |

**Tests:** none. Overview “shadow P&L” is `Sum(ShadowOrders.SourceVsShadowSlippage)` if any rows exist. Seeded book is a demo snapshot, not a Pepperstone tape. No commission/spread model tests.

**PASS when:** Slippage/spread/commission measured from destination quotes (and later dest fills); numbers drive `shadow_performance`, not source-broker P&L.

---

### G16 — Kill switch tested — **FAIL**

| | |
|---|---|
| §68 text | `kill switch tested` |
| LIVE | **FAIL** |
| Tests vs gate | **PARTIAL** — one fact |
| Earliest phase | 8 |

**Test:** `Stop_new_execution_blocks_opens_not_closes` — open → `GlobalStop`; close → `Approve` with `AllowFixSend = false` because the fixture flag is false.

**Missing:** `KillSwitchStopNewExecutionTests` as a command/API/integration; `KillSwitchEmergencyFlattenAuthorizationTests` (C33: flatten never closes); no `stop-new` mutation under `apps/` used by tests. Seeder inserts `Mode = None`. Risk DTO daily P&L / DD / XAU exposure are **zeros** (`GetRiskAsync`). Exclusive `KillSwitchMode` still cannot represent stop-new **and** flatten.

**PASS when:** `STOP_NEW_EXECUTION` proven (does not flatten); flatten is a distinct authorized path; audited; integration test; §70.13 also true.

---

### G17 — Secrets removed from repo / logs — **FAIL**

| | |
|---|---|
| §68 text | `secrets removed from repo / logs` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 0 (re-check always) |

**Tests:** no secrets-scan test in Unit/Integration.

**Scan (A19/B25/D30, still holds):** no live passwords in tree. Empty `Password` / `EmergencyFlattenApiKey`. Not sufficient. Committed targeting remains: seeder hosts `live-us-eqx-01.p.c-trader.com` + `SenderCompId = live.pepperstone.1369850`; `CTraderFixOptions` same host + account id default; `apps/api/appsettings.json` `CTraderFix:TargetCompId = CSERVER` (case risk, D26) and empty Postgres password. No `FixWireRedactor` / Serilog denylist (A76). No test that 553/554 log as `***`.

**PASS when:** Re-scan finds no committed secrets; FIX tags 553/554 and `Password=` redacted to `***`; dashboard never receives credentials (§72.4–5). Identifier policy signed.

---

### G18 — Dashboard exposes venue health / risk — **FAIL**

| | |
|---|---|
| §68 text | `dashboard exposes venue health / risk` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** |
| Earliest phase | 3 / 4 |

**Tests:** no API/UI health tests.

**Product (more honest than A100, still not venue health):**

- `/api/health` MT5 `healthy = true` with details `"demo FakeMt5BrokerConnector — not live Manager"`; FIX QUOTE `healthy = false` / `"no live TLS socket"`.
- `GetOverviewAsync`: `Mt5Healthy = brokers > 0`; Quote/Trade healthy from **session enums** (`LoggedOn` / `ReadyForMarketData` / …). Worker now keeps those enums at `Disconnected`, so overview bits should read false if the worker ran — still enum-as-health, not a socket probe.
- `GetBrokersAsync` hard-codes `Connected = true` (line 53) and `LastEventAt = UtcNow`. Never `IsConnectedAsync`.
- `GetRiskAsync` returns **zeros** for P&L / DD / exposure; kill-switch string from DB; `ExecutionEnabled` false literal.
- `/api/reconciliation/status` zeros.

**PASS when:** React shows real MT5/QUOTE/TRADE health, quote age, risk rejects, kill-switch mode, shadow book — from venue facts, not seed/heartbeat. No secrets in the browser.

---

### G19 — Manual review completed — **FAIL**

| | |
|---|---|
| §68 text | `manual review completed` |
| LIVE | **FAIL** |
| Tests vs gate | **NONE** (not a test) |
| Earliest phase | 8 sign-off |

No signed Phase 8 review exists. Swarm audits (A/B/C/D including this D42) are **not** go-live sign-off. D42 is a **FAIL confirmation**.

**PASS when:** Named reviewer records G01–G18 PASS + §70 14/14 + production flag decision (default OFF if any box unchecked).

---

## 6. Gate-to-test scoreboard

| ID | Gate | LIVE | Tests vs gate | Current on-disk tests |
|----|------|------|---------------|------------------------|
| G01 | MT5 historical/live ingestion stable | **FAIL** | NONE | — |
| G02 | Duplicate event handling proven | **FAIL** | PARTIAL | `Deal_upsert_is_idempotent` (InMemory) |
| G03 | Trade reconstruction tests pass | **FAIL** | GREEN_INSUFFICIENT | 6 recon + 2 deal-reason facts |
| G04 | XAU symbol mappings verified | **FAIL** | PARTIAL | `SymbolNormalizerTests` 6 |
| G05 | Quote session stable | **FAIL** | NONE | — |
| G06 | Trade session stable | **FAIL** | NONE | — |
| G07 | cTrader reconciliation after restart | **FAIL** | NONE | stub `/api/reconciliation/status` |
| G08 | Copy intents idempotent | **FAIL** | NONE | writer exists; **untested** |
| G09 | Unknown execution state recovery | **FAIL** | PARTIAL | 2 SM facts |
| G10 | Position sizing conversion verified | **FAIL** | **ANTI** | 4 passthrough facts; 22 skips |
| G11 | Risk engine unit/integration tests | **FAIL** | PARTIAL | 5 unused-method facts |
| G12 | Stale quote rejection | **FAIL** | PARTIAL | 1 `Evaluate` fact |
| G13 | Stale signal rejection | **FAIL** | PARTIAL | 1 `Evaluate` + 1 expiry helper |
| G14 | Shadow copy sufficient sample | **FAIL** | NONE | — |
| G15 | Destination costs/slippage measured | **FAIL** | NONE | — |
| G16 | Kill switch tested | **FAIL** | PARTIAL | 1 stop-new fact; 0 flatten |
| G17 | Secrets removed from repo/logs | **FAIL** | NONE | — |
| G18 | Dashboard venue health/risk | **FAIL** | NONE | — |
| G19 | Manual review completed | **FAIL** | NONE | — |

**Count:** 19 gates. **0 PASS for live.** Zero skips of the **gate** (every box is FAIL). A single unchecked item blocks real copy.

Closest lab greens (still FAIL): **G03** (6/6 recon facts), **G11/G12/G13/G16** (pure `Evaluate`). Most dangerous false-green: **G03** and **G10**.

---

## 7. What changed since C14 (none of it is live PASS)

| Surface | C14 | D42 now | Live? |
|---|---|---|---|
| Unit totals | 61 pass / 22 skip / 83 | **64 pass / 22 skip / 86** | **No** |
| New facts | — | `DealReasonTests` ×2; `Canceled_deal_on_a_position_excludes_it_from_first_three` | **No** |
| Integration | 3 pass | 3 pass **when it compiles**; mid-pass compile FAIL observed | **No** |
| FIX worker | stamped `LoggedOn` / `ReadyForMarketData` | stamps **`Disconnected`** (D32; SHA `92A8F492…`) | **No** — honest absence |
| `/api/health` | demo `healthy: true` | MT5 details admit FakeMt5; FIX `healthy: false` | **No** — honesty ≠ health |
| CopyIntent writer | none (C59) | `EfTradingStore` shadow-only writer + skip-by-key | **No** — untested, lots passthrough |
| `EligibleForFirstThree` | absent | record property + cancel exclusion | **No** — lab only |
| QuickFIX | none | still **none** (`Fix.CTrader.csproj` SHA `0AD91D39…`) | **No** |
| `tests/Fix`, `tests/Replay` | absent | still absent | **No** |
| Migrations | `EnsureCreated` | still `EnsureCreated` | **No** |

C14’s **0 / 19 live** integer is **not** stale.

---

## 8. Related bars (not substitutes)

| Bar | Score | Note |
|-----|-------|------|
| §60 unit areas (C17) | **0 COVERED / 17** | Still the right coverage integer |
| §69 first useful version (A57/C13) | **0 / 12** (unless a later FUV file updates) | Required before judging ML; not a live-copy license |
| §70 live FIX acceptance (A101) | **0 / 14** | Required **in addition** to this list before Phase 8 send |
| §71 do-not-build | Kafka / K8s / ClickHouse / LLM / mesh | Correctly absent |

`CTrader:RealCopyExecutionEnabled` default **false** and “worker refuses NewOrderSingle” are **controls by absence**, not G01–G19 PASS.

Conjunction still required (A25 / A101; do not satisfy on live):

```text
CTRADER_FIX_ENABLED
CTRADER_FIX_TRADE_SESSION_ENABLED
REAL_COPY_EXECUTION_ENABLED          -- default false
TRADE == READY_FOR_EXECUTION
lease owned + fencing token current
risk AllowFixSend
STOP_NEW_EXECUTION == false
QUOTE usable if the order needs a price
intent + cl_ord_id persisted, status NotSent, not expired
venue.Kind == InProcess
  OR (explicit prod review + §68 19/19)
```

Today almost every conjunct is unimplemented. The process cannot send `35=D`. That is **not** 19/19.

---

## 9. Anti-greenwash (would let an operator think §68 is done)

| Location | Defect | Why it stays FAIL |
|---|---|---|
| Unit **64 passed** | Domain lab | §68 is a live-copy block list |
| Integration **3 passed** | FakeMt5 + `Assert.True(true)` | Not PostgreSQL / venue |
| 22 `[Skip]` conversion facts | Documented holes | Skip ≠ verified |
| `QuantityNormalizer_passthroughs_0_10_lots…` | Locks `0.10` not `10.00` | G10 anti-evidence |
| `TradeReconstructionTests` 6 greens | Synthetic deals | Not live history |
| `Canceled_deal_…first_three` | Lab cancel tape | Not Manager 13/14 + dirty fixture set |
| `RiskEngineTests` 5 greens | Dead path | Not on send |
| `Deal_upsert_is_idempotent` | InMemory | Not Postgres / live replay |
| FIX worker `Disconnected` stamp | Honest | Session is **not** stable; it is **absent** |
| `/api/health` FIX `healthy: false` | Honest | Does not pass G18 |
| `/api/health` MT5 `healthy: true` | Demo connector | Operator can still misread the bool |
| `GetBrokersAsync` `Connected = true` | Hard-coded | Never `IsConnectedAsync` |
| `/api/reconciliation/status` zeros | Hard-coded | No recon run |
| Shadow `CopyIntent` writer | Untested; expired `ExpiresAt`; lots qty | Not G08 / G14 |
| `LiveCopyPage` amber (if still present) | Honest stub | Does not pass G18 |

A §68 item is **not** green if the only evidence is a DB enum, a skipped test, a method nobody calls, or a recovered compile.

---

## 10. Sign-off

```text
[ ] 19/19 §68 gates PASS
[ ] 14/14 §70 live FIX acceptance PASS
[ ] First useful version (§69) signed (A57)
[ ] Explicit production flag reviewed
[ ] Manual review name / date / evidence links recorded
[ ] Default remains OFF if any box is unchecked
```

**Current:** all boxes unchecked. **Real copy: DISABLED.**

When a later coding wave ticks a box, update **A100 or a dated successor** with the test class name, command, timestamp, and SHA-256 of the test assembly. Do not tick from chat. Do not tick from this D42 file — it is a **still-FAIL** pin.

---

## 11. Sources

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §60, §67–§70, §72
- `D:\Prop\reports\swarm\20260818\A100_golive_gates.md`
- `D:\Prop\reports\swarm\20260818\C14_golive_still_fail.md`
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md`
- `D:\Prop\reports\swarm\20260818\A101_live_fix_acceptance.md`
- `D:\Prop\reports\swarm\20260818\C17_unit_coverage.md`
- `D:\Prop\reports\swarm\20260818\C31_recon_adversarial.md`
- `D:\Prop\reports\swarm\20260818\C33_risk_adversarial.md`
- `D:\Prop\reports\swarm\20260818\C59_copyintent_gap.md` (writer claim **stale**; untested-writer claim **not**)
- `D:\Prop\reports\swarm\20260818\D18_qty.md`
- `D:\Prop\reports\swarm\20260818\D32_fixw.md`
- `D:\Prop\apps\fix-worker\Worker.cs`, `apps\mt5-worker\Worker.cs`, `apps\api\Program.cs`
- `D:\Prop\src\Infrastructure\**`, `D:\Prop\src\Domain\**`, `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\tests\Unit\*`, `D:\Prop\tests\Integration\*`

---

*End of D42. Product source was not modified. Architecture §68 remains **0 PASS / 19 FAIL** against current tests. Real orders remain disabled.*
