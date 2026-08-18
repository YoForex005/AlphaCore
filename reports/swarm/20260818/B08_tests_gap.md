# B08 — `tests/Unit` and `tests/Integration` gap

| Field | Value |
|---|---|
| Agent | B08 (senior engineer, test-gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:19:49+05:30 (Unit), 2026-08-18T13:19:56+05:30 (Integration) |
| Scope | `D:\Prop\tests\Unit` + `D:\Prop\tests\Integration` vs Architecture §60 / §61 and current product SUTs |
| Spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §60 (lines 2228–2293), §61 (lines 2297–2315) |
| Class backlog | `A27_test_inventory.md` (77 named classes). This file measures **what exists and what it actually proves**. |
| Stale predecessors | `A09_unit_tests_audit.md`, `A10_integration_tests_audit.md` — both describe empty `UnitTest1` + `Class1` SUTs. **Superseded for current tree contents.** |
| Product source modified | **No.** Report only. |

Classification: `COVERED` / `PARTIAL` / `MISSING` / `FAIL` / `FALSE_GREEN`. A green `dotnet test` is not coverage.

---

## 0. Verdict

**Unit: FAIL (1 red). Integration: false-green slice, 0/8 §60 areas.**

Product engines now exist (`TradeReconstructor`, `RiskEngine`, `BaselineScorer`, `QuantityNormalizer`, `FixMessageParser`, `EfTradingStore`, …). A thin xUnit surface was added on top. It does **not** satisfy Architecture §60. It does **not** satisfy A27. One reconstruction test already disagrees with the SUT. Integration never opens PostgreSQL.

| Gate | Required | Measured now |
|---|---:|---|
| §60 unit areas | 17 | **0 COVERED / 13 PARTIAL / 3 MISSING / 1 FAIL** |
| §60 integration areas | 8 | **0 / 8** |
| §61 FIX harness capabilities | 7 | **0 / 7** (`FixSimulationHarness` exists in product; no test drives it) |
| A27 named test classes | 36 Unit + 14 Integration | **6 real Unit + 1 real Integration** (+ 2 placeholders) |
| `tests/Replay`, `tests/Fix` | proposed | **absent** |
| CI (`.github`) | — | **absent** |

Measured `dotnet test` (this pass):

| Project | Total | Passed | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|---:|
| `TraderIntelligence.Tests.Unit` | **29** | **28** | **1** | 0 | **1** |
| `TraderIntelligence.Tests.Integration` | **3** | **3** | 0 | 0 | **0** |

The Integration green run includes `PlaceholderRemoved.Integration_project_loads` → `Assert.True(true)`. That is **not** a §60 test.

---

## 1. Method

| Source | Path |
|---|---|
| Architecture §60 / §61 | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Inventory | `A27_test_inventory.md` |
| Product SUTs | every `*.cs` under `D:\Prop\src` excluding `bin`/`obj` (64 product files) |
| Test tree | `Get-ChildItem D:\Prop\tests -Recurse -Filter *.cs` excluding `bin`/`obj` |
| Measured run | `dotnet test` on each test csproj, `--verbosity normal` |

Commands:

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal
dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity normal
```

An earlier run in this same audit (before `FixMessageParser.EndsWith` was edited by another agent) failed to compile:

```text
D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs(28,42): error CS1503:
  Argument 1: cannot convert from 'char' to 'string'
```

Both test projects reference `TraderIntelligence.Fix.CTrader` even though **no test class uses it**. Domain/Application unit tests are coupled to a FIX compile. That is a wiring defect, not coverage.

---

## 2. Current on-disk inventory

No fixtures (`.fix`, recorded JSON, `xunit.runner.json`, `appsettings*.json`). No `[Trait]`. No `IClassFixture`. No `WebApplicationFactory`. No Testcontainers. No Respawn. Moq is referenced on Unit and unused. Coverlet is referenced on both and unused.

### 2.1 `tests/Unit`

| Bytes | SHA-256 | Path | Role |
|---:|---|---|---|
| — | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | `TraderIntelligence.Tests.Unit.csproj` | xUnit 2.5.3 + FA 6.12 + Moq 4.20.70 + coverlet 6.0; TFM `net8.0` |
| 3939 | `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` | `TradeReconstructionTests.cs` | 5 facts; **1 FAIL** |
| 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | `BaselineScorerTests.cs` | 3 facts |
| 2909 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `RiskEngineTests.cs` | 5 facts |
| 2144 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `ExecutionAndSizingTests.cs` | 6 facts (FSM + qty + ClOrdID + expiry) |
| 896 | `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `SymbolNormalizerTests.cs` | 1 theory (5 cases) + 1 fact |
| 791 | `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5` | `VolumeConverterTests.cs` | 3 facts |
| 224 | `6B1A127F1810FF0A0E1C07F0913A415CBE61D31FE56DF3BD46378C97EB77E6A5` | `UnitTest1.cs` (`SmokeTests`) | assembly-load smoke |

**Project references:** Domain, Application, **Fix.CTrader**.  
**Not referenced:** Infrastructure, Mt5. Fine for domain units. Blocks any future `DealIngestionService` / store test in this project.

### 2.2 `tests/Integration`

| Bytes | SHA-256 | Path | Role |
|---:|---|---|---|
| — | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | `TraderIntelligence.Tests.Integration.csproj` | xUnit + FA + **EF InMemory 8.0.4**; refs Domain, Application, Infrastructure, Fix.CTrader, **Mt5** (A10 hash `93B83D02…` is the pre-Mt5 file) |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `SeedingAndStoreTests.cs` | 2 facts, InMemory EF |
| 162 | `49671A3C7C367ED87C7711E2204865AA2ABB8A7A5783AD785CD66A1F6DA7F4D6` | `UnitTest1.cs` (`PlaceholderRemoved`) | `Assert.True(true)` |

**Project references:** Domain, Application, Infrastructure, Fix.CTrader, Mt5.  
**Not referenced:** `apps/api`, `apps/mt5-worker`, `apps/fix-worker`.

**Packages that are wrong or missing for §60 Integration:**

| Package | Status |
|---|---|
| `Microsoft.EntityFrameworkCore.InMemory` 8.0.4 | Present. **Wrong seam** for “PostgreSQL migrations”. |
| `Testcontainers` / `Testcontainers.PostgreSQL` | Missing |
| `Respawn` | Missing |
| `Microsoft.AspNetCore.Mvc.Testing` | Missing (API not under this audit’s required list, still no HTTP tests) |
| QuickFIX/n 1.14.x + cTrader dictionary | Missing (product pins `QuickFix.Net 1.8.0`, NU1701 netfx) |

`D:\Prop\docker-compose.yml` now has `postgres:16`. Tests do not use it.

---

## 3. Measured test results (authoritative)

### 3.1 Unit — 29 executed, 1 failed

```text
Failed  TradeReconstructionTests.Scale_in_and_partial_close
  Expected trade.WasAveragedDown to be true, but found False.
  at TradeReconstructionTests.cs:46

Total tests: 29
     Passed: 28
     Failed: 1
 Total time: 0.3880 Seconds
```

Passing Unit cases (28):

| Class | Case |
|---|---|
| `SmokeTests` | `Domain_assembly_loads` |
| `TradeReconstructionTests` | `Reconstructs_simple_round_trip`; `Reverse_inout_closes_then_opens_opposite`; `First_three_completed_xau_unlocks_early_score`; `Ignores_balance_deals` |
| `BaselineScorerTests` | `Two_trades_remain_insufficient`; `Three_disciplined_winners_go_to_shadow_not_live`; `Martingale_after_losses_is_risk_blocked` |
| `RiskEngineTests` | `Stale_quote_rejects_open`; `Real_flag_false_never_allows_fix_send`; `Stop_new_execution_blocks_opens_not_closes`; `Unreconciled_venue_blocks_new_exposure`; `Stale_signal_rejected` |
| `ExecutionAndSizingTests` | `Unknown_ack_cannot_retry_new_order`; `Disconnect_after_send_is_unknown_state`; `Filled_report_is_terminal`; `Quantity_normalizer_steps_and_min`; `ClOrdId_is_deterministic_and_unique_per_sequence`; `Copy_intent_expires` |
| `SymbolNormalizerTests` | `Maps_known_aliases_to_XAUUSD` × 5 (`XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`); `Does_not_guess_venue_instrument_ids` |
| `VolumeConverterTests` | `Manager_scale_maps_0_10_lots_to_1000_native`; `Extended_scale_maps_one_lot_to_100_million`; `Hundredths_comment_is_not_the_default` |

### 3.2 Averaging-down FAIL (test is right, SUT is inverted)

Fixture (long scale-in at a **lower** price — classic average-down):

```csharp
Deal In  0.10 @ 2300
Deal In  0.10 @ 2290   // long add below entry VWAP
Deal Out 0.10 @ 2310
Deal Out 0.10 @ 2320
```

`TradeReconstructor.OpenTrade.ScaleIn` (product, not modified by this agent):

```237:241:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            var worse = Direction == TradeDirection.Long
                ? deal.Price > EntryVwap
                : deal.Price < EntryVwap;
            if (worse)
                WasAveragedDown = true;
```

For a long, `2290 > 2300` is false, so `WasAveragedDown` stays false. Averaging-down is add-in-loss: long add **below** VWAP, short add **above** VWAP. The comparison is backwards. Architecture §60 + A27 require `Detects_add_in_loss` / `Does_not_flag_add_in_profit`.

This is the only reconstruction assertion that currently fails. Scale-in / partial-close flags on the same fact (`WasScaledIn`, `WasPartialClose`, `Completed`, `MaxVolumeLots == 0.20`) are unobserved because the fact aborts on line 46.

### 3.3 Integration — 3 passed, 0 §60

```text
Passed  PlaceholderRemoved.Integration_project_loads          [1 ms]
Passed  SeedingAndStoreTests.Demo_seed_discovers_groups_...   [515 ms]
Passed  SeedingAndStoreTests.Deal_upsert_is_idempotent        [19 ms]
Total tests: 3  Passed: 3
```

`Demo_seed_discovers_groups_reconstructs_and_scores` (InMemory):

- 2 brokers, >2 groups, >0 deals
- at least one completed XAU reconstructed trade
- login 10001 → 3 completed XAU, state ≠ `LIVE`
- login 10002 → `RISK_BLOCKED` (demo martingale tape)
- 2 `fix_sessions` rows with `TargetCompId == "cServer"`

`Deal_upsert_is_idempotent`: second `UpsertDealAsync` of the same `(broker, ticket)` returns `false`; row count stays 1. This is an **application-level** `AnyAsync` check, not a PostgreSQL unique-index proof.

`PlaceholderRemoved` is a renamed Visual Studio template. Delete it. Do not count it.

---

## 4. §60 Unit map (17 required areas)

Namespace today is flat `TraderIntelligence.Tests.Unit` (A09/A27 wanted folders). Status is vs **behavior locked**, not vs “a method exists.”

| # | §60 required | Existing fact(s) | SUT on disk | Status | Gap |
|---:|---|---|---|---|---|
| 1 | MT5 deal deduplication | Integration `Deal_upsert_is_idempotent` only | `EfTradingStore.UpsertDealAsync` unique `(BrokerId, DealTicket)` | **PARTIAL** | No unit `Mt5DealDeduplicator`. No live+backfill replay. InMemory does not prove the UK. Different brokers / same ticket untested. |
| 2 | trade reconstruction | `Reconstructs_simple_round_trip`, `Ignores_balance_deals` | `TradeReconstructor` | **PARTIAL** | No multi-position, no broker/login filter, no commission/swap net PnL, no open leftover, no order≠deal≠position identity lock. |
| 3 | partial close | bundled in failing `Scale_in_and_partial_close` | `WasPartialClose` | **PARTIAL** | No remaining-volume / first-3-counter lock. Fact currently red. |
| 4 | scale-in | same failing fact | `WasScaledIn`, `MaxVolumeLots`, VWAP | **PARTIAL** | No entry-VWAP assert (`(2300+2290)/2`). Fact currently red. |
| 5 | full close | `Reconstructs_simple_round_trip` | `Completed`, exit VWAP, net PnL | **PARTIAL** | Fees hardcoded 0. No commission/swap. No dedicated full-close class. |
| 6 | position reversal | `Reverse_inout_closes_then_opens_opposite` | `DealEntry.InOut` | **PARTIAL** | No `OutBy`. No opposite `In` fallback. No ticket-reuse-across-lifecycle lock. |
| 7 | XAU canonical mapping | 5 aliases + venue-id register | `SymbolNormalizer` | **PARTIAL** | No unknown-symbol fail (`EURUSD` must not become XAUUSD). Prefix heuristic (`compact.StartsWith("XAUUSD")`) untested. No `GOLD.` / `XAUUSD.PRO`. No cTrader numeric id without explicit register. |
| 8 | source/destination quantity conversion | `Quantity_normalizer_steps_and_min` | `QuantityNormalizer` | **PARTIAL** | Allocation + min/step/precision only. No MT5-lots ↛ cTrader `OrderQty` known-example table. No contract size. `VolumeConverter` is source scale, not dest conversion. |
| 9 | drawdown | none | `BaselineScorer.ComputeFeatures.MaxDrawdown` | **MISSING** | Equity peak-to-trough never asserted. |
| 10 | MFE/MAE where data exists | none | scorer always `FeatureQuality.Unavailable` | **MISSING** | No `MfeMaeCalculator`. No “refuse to fabricate from closes” test. Policy is implicit by omission. |
| 11 | martingale detection | `Martingale_after_losses_is_risk_blocked` | `BaselineScorer` 1.25× after loss | **PARTIAL** | No flat-sizing negative. Threshold 1.25 / 1.5 lot-escalation split untested as its own class. |
| 12 | averaging-down detection | `WasAveragedDown.Should().BeTrue()` | inverted `ScaleIn` comparison | **FAIL** | Add-in-profit negative case does not exist. Current fact is red. |
| 13 | score-state transitions | 3 facts | `TraderStateMachine` | **PARTIAL** | Locks `INSUFFICIENT_DATA`, `SHADOW`, `RISK_BLOCKED`, `CanPromoteToLive == false`. Missing `EARLY_SCORE`, `WATCH`, `PAUSED`, `DISQUALIFIED`, rescoring history, “high score never skips RISK_BLOCKED”. |
| 14 | risk limits | 5 facts | `RiskEngine` | **PARTIAL** | See §6. ~11 hard-limit branches have zero tests. |
| 15 | copy-intent idempotency | none | `CopyIntent.IdempotencyKey` unique index only | **MISSING** | No factory, no guard, no “same source event → one intent”. Expiry helper is tested; that is not idempotency. |
| 16 | ClOrdID generation | `ClOrdId_is_deterministic_and_unique_per_sequence` | `ClOrdIdFactory.Next` | **PARTIAL** | Format + seq uniqueness. No persist-before-send reuse of the **same** id. No “never regenerate after send”. Clock is an input (good); production callers untested. |
| 17 | ExecutionReport state transitions | 3 facts | `ExecutionOrderStateMachine` | **PARTIAL** | Sent-unknown, disconnect→UNKNOWN, fill terminal. Missing: partial, reject, cancel, unknown exec type, filled stays filled on later noise, `MayRetry` only from `NotSent`/`Rejected`. No persistence of `fix_execution_reports`. |

**§60 unit score: 0 COVERED, 13 PARTIAL, 3 MISSING, 1 FAIL.**

---

## 5. §60 Integration + §61 map

### 5.1 §60 Integration (8)

| # | Required | Existing | Status | Why it does not count |
|---:|---|---|---|---|
| 1 | PostgreSQL migrations | none | **MISSING** | No `Migrations/` folder. Hosts use `EnsureCreated`. Tests use `UseInMemoryDatabase`. docker-compose Postgres is unused. |
| 2 | MT5 backfill/restart | none | **MISSING** | `SyncCheckpoint` entity exists; store never reads/writes it. Seeder does a full fake pull, not crash-resume. |
| 3 | outbox processing | none | **MISSING** | `OutboxEvent` / `OutboxEventType` exist. `EfTradingStore` never writes outbox in the same commit as a deal. No processor. |
| 4 | QuickFIX/n session configuration | none | **MISSING** | `CTraderFixOptions` defaults live host `live-us-eqx-01.p.c-trader.com` and `SenderCompId = live.pepperstone.1369850`. No test locks QUOTE 5211 / TRADE 5212 / `cServer` case / independent seq. Package is `QuickFix.Net 1.8.0` (netfx). |
| 5 | FIX message parse/build | none | **MISSING** | `FixMessageParser` exists (pipe + checksum). **Zero tests.** Repeating groups (MD) cannot be stored in `Dictionary<int,string>`. |
| 6 | ExecutionReport handling | none | **MISSING** | Harness can emit `35=8` strings. Nothing persists `fix_execution_reports` or advances an order row. |
| 7 | position reconciliation | none | **MISSING** | `ReconciliationIssueType` enum only. API `/api/reconciliation/status` returns hardcoded zeros. No test. |
| 8 | unknown-execution recovery | Unit FSM only | **MISSING** | `AfterDisconnectWithUnknownAck` is a unit fact. No OrderStatus / MassStatus / “no blind NOS retry” integration. |

**§60 integration score: 0/8.**

### 5.2 §61 FIX harness (7 + safety)

`D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` is a **string factory**, not a venue (see A68). Tests do not call it.

| # | Capability | Test | Status |
|---:|---|---|---|
| 1 | parse recorded ExecutionReports | none | **MISSING** |
| 2 | replay MarketDataIncrementalRefresh | none | **MISSING** |
| 3 | simulate disconnects | none | **MISSING** |
| 4 | simulate duplicate ExecutionReports | none | **MISSING** |
| 5 | simulate partial fill | none | **MISSING** |
| 6 | simulate rejection | none | **MISSING** |
| 7 | simulate unknown-state disconnect | none | **MISSING** |
| S | test mode never hits `*.c-trader.com` | none | **MISSING** |
| S | default `dotnet test` never sends NOS | implicit (no adapter) | **unenforceable** |

Recorded fixtures on disk: **0**.

---

## 6. Product SUTs vs tests (what can be locked today)

These types compile. Tests do not need to wait for missing architecture classes.

| SUT | Location | Test lock | Residual risk |
|---|---|---|---|
| `TradeReconstructor` | Domain | 4 green + 1 red | Averaging-down inverted; many lifecycle edges open |
| `VolumeConverter` | Domain | 3 green | Scale `100` vs `10_000` documented; no deal-pipeline test that the **store** uses Manager scale |
| `SymbolNormalizer` | Domain | 6 green | Unknown symbol + prefix over-match |
| `BaselineScorer` / `TraderStateMachine` | Domain | 3 green | Drawdown, PF, WATCH/EARLY_SCORE, averaging feature bit |
| `RiskEngine` | Domain | 5 green | See branches below |
| `QuantityNormalizer` | Domain | 1 green | Dest contract / cTrader 0.01 step table |
| `ClOrdIdFactory` | Domain | 1 green | Persist-before-send |
| `CopyIntentExpiry` | Domain | 1 green | Not wired to `CopyIntent.ExpiresAt` |
| `ExecutionOrderStateMachine` | Domain | 3 green | Partial/reject/cancel/unknown |
| `ShadowCopyEngine` | Domain | **none** | Entry/exit use dest bid/ask + 0.05 delay slip; easy unit |
| `DealIngestionService` | Application | only via seeder | Date window, connect, per-account loop |
| `ReconstructionScoringService` | Application | only via seeder | `Guid.NewGuid()` on score insert; no history assertion |
| `EfTradingStore` | Infrastructure | 1 idempotent upsert + seeder | No outbox, no checkpoint, no unique-index proof |
| `TraderDbContext` | Infrastructure | implicit InMemory | 0 migrations; 18 tables vs §45 43 (B03) |
| `EfDashboardQueries` | Infrastructure | **none** | Overview/traders/FIX/risk queries untested |
| `DemoSeeder` | Infrastructure | 1 InMemory seed | Embeds live FIX host + SenderCompId `live.pepperstone.1369850` |
| `FakeMt5BrokerConnector` / `BrokerRegistry` / `DemoBrokerFactory` | Mt5 | via seeder | Unknown broker throw; date filter; group filter |
| `DeterministicGuid` | Mt5 | **none** | Unused by store (store uses `Guid.NewGuid()`) |
| `IBrokerConnector` | Mt5 | **none** | Parallel/dead interface vs `IMt5BrokerConnector` |
| `FixMessageParser` | Fix.CTrader | **none** | Checksum, Build/Parse round-trip |
| `FixSimulationHarness` | Fix.CTrader | **none** | String factory; RoE disagreements (A68) |
| `FixSessionOwnership` + in-memory lock | Fix.CTrader | **none** | Fencing token, expiry, split-brain |
| `CTraderFixOptions` | Fix.CTrader | **none** | Live host default; `RealCopyExecutionEnabled=false` |
| API minimal APIs | `apps/api` | **none** | `/health` hardcoded healthy; CORS any-origin |
| `Mt5Worker` / `FixWorker` | apps | **none** | Template loops + demo sync |

### 6.1 `RiskEngine` branches with **zero** facts

Covered: `QUOTE_STALE`, `AllowFixSend` when flag false, `STOP_NEW_EXECUTION` open vs close, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`.

Untested (all live code in `RiskEngine.Evaluate`):

| Reason / path | Outcome in code |
|---|---|
| `EMERGENCY_FLATTEN_BLOCKS_NEW` | `GlobalStop` on increasing |
| `VENUE_UNHEALTHY` | `PauseVenue` |
| `QUOTE_MISSING` | `Reject` |
| `SPREAD_TOO_WIDE` | `Reject` |
| `PRICE_MOVED_TOO_FAR` | `Reject` |
| `MAX_LOSS_PER_TRADER` | `PauseTrader` |
| `MAX_DAILY_EXECUTION_LOSS` | `GlobalStop` |
| `MAX_PORTFOLIO_DRAWDOWN` | `GlobalStop` |
| `MAX_OPEN_POSITIONS` | `Reject` |
| `MAX_POSITION_QUANTITY` | `Reject` |
| `MAX_XAU_GROSS` | `Reject` |
| `MAX_XAU_NET` | `ReduceSize` **via `Reject()`** → `ApprovedQuantity = 0` (likely a product bug; untested) |
| `MAX_MARGIN_USAGE` | `Reject` |
| `MARTINGALE_BLOCK` | `PauseTrader` |
| `ABNORMAL_SIZING_BLOCK` | `Reject` |
| `RISK_REDUCTION` approve on close/reduce | `Approve` + `AllowFixSend` only if flag+reconcile+venue |
| `RealExecutionEnabled=true` happy path | `AllowFixSend=true` |

---

## 7. A27 class backlog vs now

A27 asked for **36** Unit + **14** Integration named classes (plus Replay 9 + FIX 18, out of these two projects).

| A27 class | Now |
|---|---|
| `Reconstruction.TradeReconstructionTests` | collapsed into 1 file; 1 red |
| `Reconstruction.PartialClose*` / `ScaleIn*` / `FullClose*` / `PositionReversal*` / `FirstThree*` / `Mt5DealDeduplication*` | missing as classes |
| `Mapping.XauCanonicalMappingTests` | `SymbolNormalizerTests` (partial) |
| `Sizing.SourceDestinationQuantityConversionTests` | 1 method on `ExecutionAndSizingTests` |
| `Features.*` (drawdown, MFE/MAE, martingale, averaging, feature engine) | 1 martingale fact on scorer |
| `Scoring.*` (6 classes) | 3 facts on `BaselineScorerTests` |
| `Risk.*` (10 classes) | 5 facts on `RiskEngineTests` |
| `Execution.*` (6 classes) | 4 facts mixed into `ExecutionAndSizingTests` |
| `Fix.*` (3 unit codec/session) | **0** |
| `Persistence.PostgreSqlMigrationTests` / `CoreSchemaContractTests` | **0** |
| `Mt5.Mt5BackfillRestartTests` / `Mt5LiveIngestIdempotencyTests` / `DualBrokerIsolationTests` | **0** (seeder hits two fake brokers, no isolation assert) |
| `Outbox.*` | **0** |
| `Fix.QuickFixn*` / `FixMessageParseBuildIntegration*` / `ExecutionReportHandling*` | **0** |
| `Reconcile.*` / `Flags.RealExecutionDisabledIntegrationTests` | **0** |

A27 total in these two lanes: **50** classes. Present as real suites: **7**. Placeholders: **2**.

---

## 8. Placeholders that must not count

```csharp
// tests/Unit/UnitTest1.cs — file name is still the template
public class SmokeTests
{
    [Fact]
    public void Domain_assembly_loads()
        => Assert.NotNull(typeof(VolumeConverter).Assembly);
}

// tests/Integration/UnitTest1.cs
public class PlaceholderRemoved
{
    [Fact]
    public void Integration_project_loads() => Assert.True(true);
}
```

Smoke is harmless but not a §60 area. `Assert.True(true)` is **FALSE_GREEN**. Delete both when the next real class lands. Do not rename them to “cover” migrations or FIX.

---

## 9. Wiring defects (concrete)

1. **Unit is red.** `dotnet test` on the solution will fail CI the moment CI exists. The red fact is a real averaging-down inversion, not a flaky fixture.
2. **False-green Integration.** 3/3 pass, including `Assert.True(true)`. Duration of the useful seed test (~500 ms) is InMemory, not Postgres.
3. **InMemory in the Integration project** whose first §60 item is PostgreSQL migrations. Unique indexes, `jsonb`, advisory locks, and restart durability are not exercised.
4. **Both test projects reference Fix.CTrader with zero FIX tests.** A FIX compile break (observed this audit) takes down Domain unit tests.
5. **No `[Trait("Category","Integration")]`, no live-skip, no host allow-list.** Nothing prevents a later engineer from pointing tests at `CTRADER_FIX_HOST`.
6. **`DemoSeeder` writes live-looking FIX session rows** (host `live-us-eqx-01.p.c-trader.com`, SenderCompId `live.pepperstone.1369850`). The seed test **asserts** `TargetCompId == "cServer"`, which cements those rows as expected demo data. That is not a live-socket test, but it is a live-identifier fixture.
7. **No EF migrations.** `EnsureCreated` + InMemory cannot satisfy §60 item 1.
8. **No outbox write path**, so outbox tests cannot be honest yet — do not add an empty `OutboxProcessingTests`.
9. **`IBrokerConnector` vs `IMt5BrokerConnector`.** Fake implements the latter. The former is untested dead surface.
10. **No Replay / Fix test projects.** Acceptable only if Integration covers §61. It does not.
11. **No CI.** `.github` is absent. A failing unit suite is currently a local-only signal.
12. **A09/A10 must not be used as the current name map without this file.** They assumed no SUTs. SUTs exist. Tests exist. Coverage is still far below §60.

---

## 10. Safety

Architecture §61: **Do not use the real account as the first integration test.**  
Architecture §41 / `CTraderFixOptions.RealCopyExecutionEnabled`: default **false**.  
`RiskEngineTests.Real_flag_false_never_allows_fix_send` is the only automated lock on that flag, and it only covers `Evaluate` — not the FIX worker send path (worker already refuses NOS in a log line, untested).

Until an in-process venue + `FixAdapterTestMode` safety facts exist, `tests/Integration` must not resolve `*.c-trader.com`. Current tests do not open sockets (InMemory + fakes). That is **absence of a send path**, not a proven gate.

---

## 11. What this audit is not

| Item | Why excluded |
|---|---|
| `mt5-sdk/tests/*.cpp` | Different process (A18). Do not count toward these csproj. |
| `apps/web` | No Vitest/Jest files found. Not §60. |
| Replay pipeline | `tests/Replay` does not exist (A67 format only). |
| Creating or fixing tests | Out of scope. Product source not edited. The averaging-down invert is reported, not patched. |

---

## 12. Recommended next tests (do not add empty classes)

Align with §67 / A27 implementation order. Prefer facts against **existing** SUTs.

| Order | Add | Why |
|---:|---|---|
| 1 | Split reconstruction: dedicated partial / scale-in / full-close / reversal / first-3; **fix or invert-assert** averaging-down; add add-in-profit negative | Unblocks §69.5–6; current red fact is the highest-value signal |
| 2 | Remaining `RiskEngine` reasons (table in §6.1), especially `ReduceSize` qty and EMERGENCY_FLATTEN | Hard limits are the live-copy gate |
| 3 | `ShadowCopyEngine` entry/exit/mark — dest quotes only | Cheap, §69.11 precursor |
| 4 | `FixMessageParser` round-trip + checksum + missing tag 10 (Unit; drop the unused Fix ref or keep it for this) | §60.5 unit slice; would have caught CS1503 |
| 5 | `FixSessionOwnership` in-memory fencing | Split-brain before TRADE |
| 6 | Replace InMemory seed with Testcontainers Postgres + `EnsureCreated` **only until** real migrations exist; then `PostgresMigrationTests` | §60.1 |
| 7 | Checkpoint resume + deal UK on **Postgres**; dual-broker same ticket | §60.1–2 |
| 8 | Outbox same-commit **after** the writer exists | §60.3 — do not fake it |
| 9 | In-process FIX harness (A68) **before** any live NOS | §61 |

Delete `UnitTest1.cs` in both projects as part of step 1.

---

## 13. Disposition

| Metric | Value |
|---|---|
| Unit `[Fact]`/`[Theory]` executed | 29 |
| Unit passed / failed | 28 / 1 |
| Integration executed / passed | 3 / 3 (1 FALSE_GREEN) |
| §60 unit areas COVERED | **0 / 17** |
| §60 unit areas PARTIAL | 13 |
| §60 unit areas MISSING | 3 (drawdown, MFE/MAE, copy-intent idempotency) |
| §60 unit areas FAIL | 1 (averaging-down) |
| §60 integration areas | **0 / 8** |
| §61 harness capabilities | **0 / 7** |
| A27 Unit+Integration classes present (real) | 7 / 50 |
| Recorded FIX fixtures | 0 |
| Product source changed by B08 | **No** |

**Do not treat the Integration green run as a Phase-1 or §68 exit signal.** Treat the Unit red as a real reconstruction defect: long scale-in at a worse (lower) price does not set `WasAveragedDown`.
