# A89 — Complete xUnit class list

| Field | Value |
|---|---|
| Agent | A89 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A89_unit_class_list.md` |
| Scope | Reconstruction, scoring, risk sizing, FIX, FSM, symbol mapping |
| Lane | **Unit only** (`TraderIntelligence.Tests.Unit`) |
| Product source edited | **No** |
| Test source edited | **No** |

**Sources of law (read, not rewritten):**

- Architecture: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§14–18, 22–23, 25–42, 60, 69–70
- Specs: `A21` reconstruction, `A22` scoring, `A23` risk, `A25` FIX session, `A37` deal enums, `A38` volume units, `A42` ClOrdID, `A43` position sizing, `A44` symbol mapping, `A45` MFE/MAE, `A46` session ownership, `A48` kill switch
- Prior inventories: `A09` (17 §60 names, now stale vs code), `A27` (full four-lane inventory)
- Measured SUTs under `D:\Prop\src\` as of 2026-08-18

This file is the **authoritative unit-class backlog** for the six named domains. It supersedes `A09`/`A27` class names **where they collide**, because those reports were written against `Class1` stubs. Current SUTs exist. Implement the classes below; do not invent a second naming scheme.

Integration / Replay / FIX-harness **projects** stay in `A27`. Classes here are xUnit types that belong in `tests/Unit` (in-process, no live venue, no production TRADE port). A few FIX codec/session classes are the **unit slice** of §60–61.

---

## 0. Current measured test surface

| Path | Status |
|---|---|
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | xUnit 2.5.3 + FluentAssertions + Moq + coverlet; TFM `net8.0` |
| Project refs | Domain, Application, Fix.CTrader. **Not** Infrastructure, Mt5 |
| `*.cs` test classes in `tests/Unit` | **None** (`UnitTest1.cs` is gone) |
| `tests/Replay`, `tests/Fix` | Missing (out of this list except as notes) |

**Runnable `[Fact]`/`[Theory]` covering these six domains: 0.**

---

## 1. Conventions (binding)

```text
TraderIntelligence.Tests.Unit.<Area>.<SutOrCapability>Tests
```

| Rule | Value |
|---|---|
| Project | `tests/Unit` → `TraderIntelligence.Tests.Unit` |
| File | `tests/Unit/<Area>/<ClassName>.cs` (folder = last namespace segment) |
| Suffix | `Tests` only. Harnesses/builders are **not** `*Tests` |
| Fact names | `Method_Scenario_Expected` |
| One public class | per capability cluster (do not dump six domains into one file) |
| No live I/O | no `p.c-trader.com`, no production TRADE, no broker passwords |
| Arithmetic | `decimal` for lots / ounces / OrderQty / VWAP / scores |
| Framework | xUnit + FluentAssertions already referenced |
| Delete | do not recreate `UnitTest1` |

**SUT status column**

| Tag | Meaning |
|---|---|
| `EXISTS` | Type is in `src/` today; write the test now |
| `PARTIAL` | Type exists but is missing fields/behavior the spec requires; test the existing surface **and** assert the missing contract so the next implementation is forced |
| `MISSING` | Spec type not in `src/` yet; still name the class; skip/fail-closed with an explicit `Assert.Fail` **only** if the team chooses TDD-first. Prefer implementing the SUT in a later coding task, then the test |

**Priority**

| Tag | Meaning |
|---|---|
| P0 | Unblocks §69 first useful version / §60 required bullets. Write first |
| P1 | Locks hard safety (risk, FSM, FIX identity). Write before any live NOS |
| P2 | Completeness / dirty paths / discovery |

---

## 2. Shared fixtures (not `*Tests`)

Place under `tests/Unit/_Support/`. These are builders, not inventory counts.

| Type | Role |
|---|---|
| `NormalizedDealBuilder` | Fluent `NormalizedDeal` with `VolumeNative = lots * 10_000` via `VolumeConverter.Manager` |
| `ReconstructedTradeBuilder` | Completed / open XAU or non-XAU `ReconstructedTradeResult` |
| `RiskRequestBuilder` | Valid `RiskEvaluationRequest` defaults (healthy venue, fresh quote, flag off/on) |
| `QuoteBuilder` | `DestinationQuote` with explicit `ReceivedAt` |
| `FixPipeMessageBuilder` | Pipe-delimited FIX using `FixMessageParser.BuildFixMessage` |
| `DeterministicClock` | Frozen `DateTimeOffset` for expiry / quote age / ClOrdID timestamp |
| `XauThreeTradeFixture` | Exactly three completed XAUUSD lifecycles (A21 first-3) |
| `MartingaleThreeTradeFixture` | Loss then 2× size-up (A22 `MARTINGALE_VOLUME_RATIO`) |
| `DemoDealTape` | Wraps `DemoBrokerFactory` deals **after** `TraderIntelligence.Mt5` is referenced, or copies the same tape as literals so Unit does not need Mt5 |

`tests/Unit` currently cannot reference `FakeMt5BrokerConnector` without adding a project reference to `src/Mt5`. Keep reconstruction fixtures **in the unit project** so P0 tests compile today.

---

## 3. Existing SUTs these classes bind to

| Area | Type | Path |
|---|---|---|
| Reconstruction | `TradeReconstructor` | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` |
| Reconstruction | `NormalizedDeal` | `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` |
| Reconstruction | `ReconstructedTradeResult` | `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` |
| Reconstruction persist | `ReconstructedTrade` | `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` |
| Volume | `VolumeConverter` | `D:\Prop\src\Domain\Volume\VolumeConverter.cs` |
| Scoring | `BaselineScorer`, `FeatureSnapshot`, `BaselineScore` | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| FSM (trader) | `TraderStateMachine` | same file |
| FSM (trader enum) | `TraderState` | `D:\Prop\src\Domain\Enums\TraderState.cs` |
| FSM (exec) | `ExecutionOrderStateMachine`, `ExecutionReportInput` | `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs` |
| FSM (exec enum) | `ExecutionOrderStatus` | `D:\Prop\src\Domain\Enums\ExecutionOrderStatus.cs` |
| FSM (FIX session) | `FixSessionStatus`, `FixSessionQualifier` | `D:\Prop\src\Domain\Enums\` |
| Risk | `RiskEngine`, `RiskLimits`, `RiskEvaluationRequest`, `RiskDecision` | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| Risk enum | `RiskDecisionOutcome`, `CopyIntentAction`, `KillSwitchMode` | `D:\Prop\src\Domain\Enums\` |
| Sizing | `QuantityNormalizer`, `InstrumentQuantitySpec` | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Mapping | `SymbolNormalizer`, `CanonicalInstrumentRef` | `D:\Prop\src\Domain\Instruments\SymbolNormalizer.cs` |
| Mapping entity | `SourceSymbolMapping`, `CanonicalInstrument` | `D:\Prop\src\Domain\Entities\` |
| FIX codec | `FixMessageParser` | `D:\Prop\src\Fix.CTrader\Parsing\FixMessageParser.cs` |
| FIX sim | `FixSimulationHarness` | `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` |
| FIX lease | `FixSessionOwnership` | `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs` |
| FIX config | `CTraderFixOptions` | `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` |
| ClOrdID | `ClOrdIdFactory` | `D:\Prop\src\Domain\Execution\ClOrdIdFactory.cs` |
| Expiry | `CopyIntentExpiry` | `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` |
| Orchestration | `ReconstructionScoringService`, `DealIngestionService` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| Deal enums | `DealAction`, `DealEntry` | `D:\Prop\src\Domain\Enums\` |

Missing spec types that classes still name: `SourceDestinationQuantityConverter` / `PositionSizingCalculator` (A43), `MfeMaeCalculator` (A45), `TraderScoreStateMachine.ResolveState` full R0–R9 (A22 §9), `CTraderQuoteSession` / `CTraderTradeSession`, `FixSessionStatusMachine`, `DestinationSymbol` / `CanonicalInstrumentMapper`, `CopyIntentIdempotencyGuard`.

---

## 4. Spec vs current code (tests must lock the **spec**)

Do not rubber-stamp the first implementation. Several P0 facts will **fail today**. That is intended.

| ID | Spec (source of truth) | Current code | Test stance |
|---|---|---|---|
| G1 | A21 `was_averaged_down`: LONG add **below** prior VWAP; SHORT add **above** | `OpenTrade.ScaleIn` treats LONG `price > EntryVwap` as worse (inverted) | Assert A21 polarity |
| G2 | A22 `MARTINGALE_VOLUME_RATIO = 1.80` after a loss | `BaselineScorer` uses `> 1.25×` | Assert 1.80; 1.26× after loss is **not** martingale |
| G3 | A22 `ESCALATION_VOLUME_RATIO = 2.00` | code uses `> 1.5×` | Assert 2.00 |
| G4 | A22 risk/behavior/quality are weighted lerp + floors/caps + `U(N)` | additive point table, `quality` min 40 when `N<3` | Assert A22 formulas; current numbers are a stub |
| G5 | A22 R5: `N==3` cannot be `LIVE` / `LIVE_CANDIDATE`; max auto = `SHADOW` | `FromBaseline` never emits LIVE (good) but also never WATCH/SHADOW with A22 thresholds | Assert graph + `CanPromoteToLive == false` at N=3 |
| G6 | A38 / `VolumeConverter`: `Volume() / 10_000` | **correct** (`ManagerVolumeScale = 10_000`) | Protect against hundredths (`/100`) regression |
| G7 | A43: lots → ounces → dest convention → floor step; never passthrough | `QuantityNormalizer` is `lots * allocation` only | Assert ounces path; passthrough FAIL |
| G8 | A44 lookup key: trim + upper; **do not** strip `.` or suffix | `SymbolNormalizer` strips `.` / space then `StartsWith("XAUUSD")` | Assert `XAUUSD.` is its own key; unknown suffix is **not** silent XAU |
| G9 | A23 `REDUCE_SIZE` returns a **smaller non-zero** qty when possible | `MAX_XAU_NET` returns `ReduceSize` with `ApprovedQuantity = 0` | Assert reduced qty or `REJECT` + `SIZE_BELOW_MIN` |
| G10 | A23 `REAL_COPY_EXECUTION_ENABLED=false` must not allow FIX send | `AllowFixSend` is false (good) but open still `Approve` | Assert `AllowFixSend==false`; live worker still cannot emit NOS |
| G11 | A21 first-3 = completed XAU lifecycles only | `CountCompletedXauUsdTrades` / `IsEarlyScoreEligible` exist | Lock; ignore partial / open / non-XAU / balance |
| G12 | A42: unknown after send → no NOS retry | `MayRetryNewOrderSingle` only `NotSent`/`Rejected` (good) | Lock disconnect → `ExecutionStateUnknown` |

---

## 5. Master list (92 unit classes)

Namespace prefix: `TraderIntelligence.Tests.Unit`.

### 5.1 Reconstruction — 22 classes

Project folder: `tests/Unit/Reconstruction/`

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 1 | `TradeReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | §14, §60, A21 | `Order ≠ Deal ≠ Position ≠ Logical Trade`; one completed lifecycle per position book; multiple deals → one trade when same `position_id` same side |
| 2 | `PartialCloseReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | §14, §35, §60, A21 | `ENTRY_OUT` with remaining > 0 → `WasPartialClose`; **not** completed; **does not** increment first-3 |
| 3 | `ScaleInReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | §14, §35, §60, A21 | second `ENTRY_IN` same side → one trade; `WasScaledIn`; `MaxVolumeLots` rises; entry VWAP updates |
| 4 | `FullCloseReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | §14, §60, A21 | remaining → 0 → `Completed`; `ClosedAt` set; `ExitVwap`; `NetRealizedPnl = Gross + Commission + Swap + Fees` |
| 5 | `PositionReversalReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | §35, A21 §7.6 | `ENTRY_INOUT` closes old lifecycle and opens leftover opposite; two results; directions opposite |
| 6 | `OutByReconstructionTests` | `TradeReconstructor` | P1 | EXISTS | A21 `ENTRY_OUT_BY` | `DealEntry.OutBy` treated as close; remaining/complete flags same rules as `Out` |
| 7 | `HedgeVsNettingReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | A21 §1.6 | distinct `position_id`s = distinct trades (hedge); same `position_id` extra IN = scale-in (netting) |
| 8 | `FirstThreeCompletedXauTradesTests` | `TradeReconstructor` | P0 | EXISTS | §15, §69.6, A21 §4.2 | `CountCompletedXauUsdTrades` counts only completed XAU; order/place/partial/SL/open/non-XAU/balance excluded |
| 9 | `EarlyScoreEligibleLatchTests` | `TradeReconstructor` | P0 | EXISTS | §15, A21, A22 I2 | `IsEarlyScoreEligible` true iff count ≥ 3; trade #4 does not emit `PROVEN_PROFITABLE`; no such token exists |
| 10 | `NonTradeDealExclusionTests` | `NormalizedDeal`, `TradeReconstructor` | P0 | EXISTS | A21 §6, A37 | `IsTradingDeal` only Buy/Sell; Balance/Credit/Commission/Bonus/Tax/Dividend/SO-comp skipped; no book change |
| 11 | `CanceledDealHandlingTests` | `TradeReconstructor` | P1 | PARTIAL | A21 §6 | `BuyCanceled`/`SellCanceled` must not invent an inverse fill; spec: dirty + exclude from first-3. Today they are non-trading (`IsTradingDeal==false`) and silently skipped — assert skip and document dirty-lifecycle gap |
| 12 | `ReconstructionSortDeterminismTests` | `TradeReconstructor` | P0 | EXISTS | A21 §7.1 | sort `Time` then `DealTicket`; shuffled input → identical results; same input twice → same `Id` / VWAP / flags |
| 13 | `ReconstructionBrokerIsolationTests` | `TradeReconstructor` | P0 | EXISTS | §10 | same ticket on ACHIEVER vs STARWAVEFX is not one trade; filter `brokerId`+`login` |
| 14 | `ReconstructionLifecycleReuseTests` | `TradeReconstructor` | P1 | EXISTS | A21 key | flat then new IN on same `position_id` is a **new** lifecycle (`Id` differs by open time); do not merge |
| 15 | `ReconstructionVwapAndPnlTests` | `TradeReconstructor` | P0 | EXISTS | A21 §4.1 | entry/exit VWAP = Σ(px×lots)/Σ(lots) decimal; commission+swap signed as stored; `Fees` currently 0 |
| 16 | `ReconstructionSlTpPropagationTests` | `TradeReconstructor` | P2 | EXISTS | A21 §4.1 | `InitialSl/Tp` from first open; `FinalSl/Tp` last deal that carries them |
| 17 | `AveragingDownFlagReconstructionTests` | `TradeReconstructor` | P0 | EXISTS | A21 §4.1, **G1** | LONG scale-in **below** prior VWAP sets `WasAveragedDown`; above does **not**; SHORT inverted; add-in-profit is not averaging-down |
| 18 | `OpenLifecycleNotCompletedTests` | `TradeReconstructor` | P0 | EXISTS | §15 | leftover open book → `Completed=false`, `ClosedAt=null`; omitted from `CompletedXauUsdTrades` |
| 19 | `ReconstructionZeroAndBadVolumeTests` | `TradeReconstructor` | P1 | PARTIAL | A21 failures | `lots<=0` skipped; spec dirty codes `RECON_ZERO_VOLUME` / `RECON_BAD_PRICE` not implemented — assert skip today + no crash on `price<=0` |
| 20 | `NormalizedDealContractTests` | `NormalizedDeal` | P1 | EXISTS | A37 | required fields; `IsTradingDeal` matrix for every `DealAction` |
| 21 | `ReconstructedTradeResultXauFlagTests` | `ReconstructedTradeResult` | P0 | EXISTS | §16 | `IsXauUsd` iff `CanonicalSymbol == "XAUUSD"` (ordinal ignore-case); `GOLD` leftover in canonical is **not** XAU |
| 22 | `ReconstructionScoringServiceRebuildTests` | `ReconstructionScoringService` | P1 | EXISTS | A02 | loads deals → reconstruct → persist → score completed XAU only; mock `ITradingStore`; does not call FIX |

§60 bullets covered here: trade reconstruction, partial close, scale-in, full close, position reversal. Dedup is class **23** (ingest, still reconstruction-adjacent).

### 5.2 Ingest feeding reconstruction — 3 classes

Folder: `tests/Unit/Ingestion/`

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 23 | `Mt5DealDeduplicationTests` | `ITradingStore.UpsertDealAsync` contract / in-memory fake | P0 | PARTIAL | §12, §60 | same `(broker_id, deal_ticket)` second upsert is no-op; different brokers same ticket persist both. Unit-test via a fake store (do not require EF). `EfTradingStore` is integration |
| 24 | `DealIngestionServiceSyncTests` | `DealIngestionService` | P1 | EXISTS | §12 | connector deals flow to store; counts inserted; **no** reconstruct/score/FIX on the ingest method |
| 25 | `DemoTapeReconstructionAcceptanceTests` | `TradeReconstructor` + `XauThreeTradeFixture` | P0 | EXISTS | §69.4–6 | login 10001-style 3 closed 0.10 XAU → count 3, eligible; login 10002-style 0.10/0.20/0.40 after losses → 3 completed **and** martingale flag at scoring layer |

### 5.3 Scoring — 16 classes

Folder: `tests/Unit/Scoring/` and `tests/Unit/Features/`

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 26 | `BaselineScorerFeatureSnapshotTests` | `BaselineScorer.ComputeFeatures` | P0 | EXISTS | A22 §3 | empty → zeros / flags false; only `Completed && IsXauUsd`; ordered by `ClosedAt` |
| 27 | `DrawdownCalculatorTests` | `BaselineScorer.ComputeFeatures` (`MaxDrawdown`) | P0 | EXISTS | §18, §60, A22 §3.4 | peak-to-trough on completed-trade equity; empty = 0; no tick MTM |
| 28 | `MartingaleDetectorTests` | `BaselineScorer` | P0 | EXISTS | §18, §60, A22 §3.5, **G2** | size-up after **loss** at ratio ≥ 1.80 → `Martingale`; flat size after loss → false; size-up after **win** is escalation, not martingale |
| 29 | `AveragingDownDetectorTests` | `FeatureSnapshot.AveragingDown` | P0 | EXISTS | §18, §60, A21 | `true` if any trade `WasAveragedDown`; does not re-derive from VWAP here |
| 30 | `LotEscalationDetectorTests` | `BaselineScorer` | P1 | EXISTS | A22 §3.5, **G3** | adjacent `MaxVolumeLots` ratio ≥ 2.00 → `LotEscalation` |
| 31 | `ProfitFactorAndNetPnlTests` | `BaselineScorer` | P0 | EXISTS | A22 §3.3 | GP/GL; `ProfitFactor` cap path when GL=0 and GP>0; NET = Σ net pnl |
| 32 | `LotCvAndLossSizeCvTests` | `BaselineScorer` | P1 | PARTIAL | A22 §2.6 | CV defined; spec uses **sample** stdev (`n-1`); code uses population `.Average()` of squares — assert spec formula |
| 33 | `SlUseAndHoldTimeTests` | `BaselineScorer` | P1 | EXISTS | A22 | `SlUseRate` = fraction with `InitialSl > 0`; `AverageHoldSeconds` from open/close |
| 34 | `TraderScoreCalculatorTests` | `BaselineScorer.Score` | P0 | PARTIAL | §69.7, A22 §§5–7, **G4** | emits `RiskScore`, `BehaviorScore`, `EarlyQualityScore` in `[0,100]`; **not** ranked by raw NET (A22 I9) |
| 35 | `EarlyQualityUncertaintyPenaltyTests` | future `ScoreConfig` / scorer | P1 | MISSING | A22 §7.2 | `U(3)=18` so perfect book ≤ 82 at N=3; stub scorer has no `U(N)` — class exists to force it |
| 36 | `MfeMaeFeatureQualityTests` | `FeatureSnapshot` | P0 | PARTIAL | §17, §60, A45 | deals-only path: `MaeMfeQuality=Unavailable`, averages null; **never** fabricate from entry/exit VWAP |
| 37 | `MfeMaeExactRequiresSourceTicksTests` | future `MfeMaeCalculator` | P1 | MISSING | A45 | EXACT only from one source-broker tick tape; no mix with cTrader quotes; no `MIXED` |
| 38 | `ScoringAsOfNoFutureLeakageTests` | `BaselineScorer` | P0 | EXISTS | A22 I6 | caller passing only trades 1..n; adding trade n+1 changes score — document that scorer itself does not filter as-of (orchestration must) |
| 39 | `ScoreHistoryAppendContractTests` | `TraderScore` / `TraderScoreHistory` | P2 | EXISTS | §22 | history row is a new record; current score fields overwritten on upsert **must not** imply history delete (unit: mapping only) |
| 40 | `ScoringCannotBypassRiskContractTests` | `BaselineScorer` + `RiskEngine` | P0 | EXISTS | §39, A22 I10 | high `EarlyQualityScore` still rejected by `RiskEngine` when a hard limit trips |
| 41 | `ReconstructionScoringServiceScoreFieldsTests` | `ReconstructionScoringService` | P1 | EXISTS | app layer | writes `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, flags, `CurrentState`, `CompletedXauTrades` from `BaselineScore` |

§60 bullets: drawdown, MFE/MAE, martingale, averaging-down, score-state (FSM section), plus scoring support for §69.7.

### 5.4 Risk sizing — 18 classes

Folder: `tests/Unit/Sizing/` and `tests/Unit/Risk/`

**Volume + destination quantity (A38 / A43 / §38 / §60 “source/destination quantity conversion”)**

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 42 | `VolumeConverterManagerScaleTests` | `VolumeConverter` | P0 | EXISTS | A38 | `1.00 lot ↔ 10_000`; `0.10 ↔ 1_000`; `0.01 ↔ 100`; **not** `*100` |
| 43 | `VolumeConverterExtendedScaleTests` | `VolumeConverter.Extended` | P2 | EXISTS | A38 | `1.00 lot ↔ 100_000_000`; product default path must **not** use Extended |
| 44 | `VolumeConverterRejectsNonPositiveScaleTests` | `VolumeConverter` | P2 | EXISTS | code | `scale<=0` throws; `ToNative(lots<0)` throws |
| 45 | `SourceDestinationQuantityConversionTests` | `QuantityNormalizer` + future converter | P0 | PARTIAL | §38, §60, A43, **G7** | **never** passthrough MT5 lots to OrderQty; known table: `0.10 lot × 100 oz → 10 oz` BaseUnits `OrderQty=10`; Lots convention `OrderQty=0.10` |
| 46 | `PositionSizingCanonicalOuncesTests` | future `PositionSizingCalculator` | P0 | MISSING | A43 §4 | ticks/10000 × contract_size; contract 10 vs 100 changes ounces; `contract_size<=0` reject |
| 47 | `QuantityNormalizerStepMinMaxTests` | `QuantityNormalizer` | P0 | EXISTS | A43 §4.5 | floor to step (never round up); below min → 0; above max → max; `allocationFactor<=0` / `step<=0` throw |
| 48 | `AllocationAndConfidenceCannotIncreaseSizeTests` | future converter | P1 | MISSING | A43 §4.3 | `confidence_scale` in (0,1]; cannot enlarge; default 1 |
| 49 | `ReduceCloseSizingUsesMappedDestinationTests` | future converter | P1 | MISSING | A23 §7, A43 | REDUCE/CLOSE qty from mapped dest position, **not** source lots × allocation |

**Risk engine limits (§39, §60 “risk limits”, A23)**

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 50 | `RiskEngineHardLimitTests` | `RiskEngine` | P0 | EXISTS | §39, §60, A23 §6 | one fact per reason: `QUOTE_MISSING`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, `PRICE_MOVED_TOO_FAR`, `SIGNAL_STALE`, `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN`, `MAX_OPEN_POSITIONS`, `MAX_POSITION_QUANTITY`, `MAX_XAU_GROSS`, `MAX_XAU_NET`, `MAX_MARGIN_USAGE`, `MARTINGALE_BLOCK`, `ABNORMAL_SIZING_BLOCK`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`, `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN_BLOCKS_NEW` |
| 51 | `RiskEngineApproveReduceRejectTests` | `RiskEngine` | P0 | EXISTS | §39, A23 §4.1 | outcomes only `Approve` / `ReduceSize` / `Reject` / `PauseTrader` / `PauseVenue` / `GlobalStop`; happy path `APPROVED` + `AllowFixSend` only when flag+reconciled+healthy+kill none |
| 52 | `OpenVsCloseExposurePolicyTests` | `RiskEngine` | P0 | EXISTS | §64, A23 §2 | `OpenExposure`/`IncreaseExposure` hit quote/signal/kill/reconcile; `ReduceExposure`/`CloseExposure` still approve reduction (`RISK_REDUCTION`) |
| 53 | `QuoteFreshnessGuardTests` | `RiskEngine` + `RiskLimits.MaxQuoteAge` | P0 | EXISTS | §31, §37 | age `>` 3s default rejects open; age == max is allowed (strict `>`); missing quote ≡ reject |
| 54 | `PriceMoveAndSpreadGuardTests` | `RiskEngine` | P1 | EXISTS | §37 | mid vs expected; spread = ask−bid; codes `PRICE_MOVED_TOO_FAR` / `SPREAD_TOO_WIDE` |
| 55 | `StaleCopyIntentExpiryTests` | `CopyIntentExpiry` + risk `SIGNAL_STALE` | P0 | EXISTS | §36, §63 | `IsExpired` when `now - source > max`; 3-minute outage must not copy a backlog of expired intents |
| 56 | `KillSwitchStopNewExecutionTests` | `RiskEngine` | P0 | EXISTS | §40, §70.13, A48 | `StopNewExecution` blocks open/increase (`GlobalStop`); reduce/close still allowed |
| 57 | `KillSwitchEmergencyFlattenTests` | `RiskEngine` | P1 | EXISTS | §40 | `EmergencyFlatten` blocks new; separate mode from stop-new; not implied by stop-new |
| 58 | `RealExecutionFeatureFlagTests` | `RiskEngine` + `CTraderFixOptions` | P0 | EXISTS | §41, §70.12 | default `RealCopyExecutionEnabled=false`; `AllowFixSend=false` when flag false even if `Approve` |
| 59 | `RiskEngineNetExposureReduceSizeTests` | `RiskEngine` | P1 | EXISTS | A23, **G9** | net-cap path is `ReduceSize`; approved qty must be the residual cap, not silently 0 unless residual < min |

### 5.5 FIX — 15 classes

Folder: `tests/Unit/Fix/` and `tests/Unit/Execution/`

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 60 | `FixMessageParseBuildTests` | `FixMessageParser` | P0 | EXISTS | §60, §61 | round-trip Logon / ER / SecurityList / MD snapshot (pipe form); tag dictionary; missing 8 throws |
| 61 | `FixChecksumValidationTests` | `FixMessageParser` | P0 | EXISTS | FIX 4.4 | tag 10 last; numeric; mismatch throws; `BuildFixMessage` writes 9+10 consistent with parse |
| 62 | `CTraderHeaderMappingTests` | `CTraderFixOptions` + harness | P0 | EXISTS | §26, A25 §3 | QUOTE 5211 / TRADE 5212; `TargetCompId` default `cServer` **case preserved**; `TargetSubId` QUOTE vs TRADE; `SenderSubId` configurable; do not infer tags from form labels |
| 63 | `QuoteAndTradeSessionIsolationTests` | `CTraderFixOptions.Quote` / `.Trade` | P1 | PARTIAL | §27, A25 §2 | independent ports, Sender/Target/Sub; no shared seq fields on options. Full session objects `MISSING` — assert options isolation now |
| 64 | `FixSimulationHarnessExecutionReportTests` | `FixSimulationHarness` | P0 | EXISTS | §61 | New / Partial / Fill / Cancel / Reject / Expired / Unknown-status messages parse; tags 11, 37, 39, 150, 32, 31 |
| 65 | `FixSimulationHarnessLogonAndRejectTests` | `FixSimulationHarness` | P1 | EXISTS | A25 | Logon success `35=A`; fail `35=3`; SecurityList `55` numeric + `1007=XAUUSD`; MD bid/ask present |
| 66 | `DuplicateExecutionReportParseTests` | `FixSimulationHarness.SimulateDuplicateExecutionReport` | P1 | EXISTS | §61, §70.5 | identical ER string twice is byte-identical; handler idempotency is a later class — here prove harness + parse do not mutate |
| 67 | `ClOrdIdGenerationTests` | `ClOrdIdFactory` | P0 | EXISTS | §33, §70.4, A42 | unique per `(intent, now, sequence)`; stable for same args; empty intent throws; sequence `<0` throws; length/prefix `TI` |
| 68 | `CopyIntentIdempotencyKeyTests` | `CopyIntent.IdempotencyKey` contract | P0 | PARTIAL | §32, §60 | same source event must yield one key; entity has the field — unit-test a **pure** key function when added; until then assert unique index intent via documented key format |
| 69 | `UnknownExecutionNoBlindRetryTests` | `ExecutionOrderStateMachine` | P0 | EXISTS | §34, A42, **G12** | after send → `SentAcknowledgementUnknown`; disconnect → `ExecutionStateUnknown`; `MayRetryNewOrderSingle` false for those + Filled/Cancelled/Partial |
| 70 | `FixSessionOwnershipLeaseTests` | `FixSessionOwnership` + in-memory lock | P0 | EXISTS | §28, A46 | second owner cannot acquire; release then acquire; `ExecutionIntentsAllowed` false until `MarkReconciled` |
| 71 | `FixSessionOwnershipFencingTokenTests` | `InMemoryDistributedLockWithFencing` | P1 | EXISTS | A46 | token monotonic; stale token cannot release another owner; cancel token throws |
| 72 | `CTraderFixOptionsSafetyDefaultsTests` | `CTraderFixOptions` | P0 | EXISTS | §41, A25 | `UseSsl=true`; `RealCopyExecutionEnabled=false`; heartbeat 30; quote age ms set; TRADE NOS gated by flag |
| 73 | `SecurityListDoesNotHardcodePepperstoneIdTests` | `FixSimulationHarness` + `SymbolNormalizer` | P1 | EXISTS | §30, A44 | sample `55=123456` is a **test** id only; `TryMapVenueInstrumentId` fails until `RegisterVenueInstrument`; production source must not contain a Pepperstone long |
| 74 | `FixParserRejectsGarbageTests` | `FixMessageParser` | P2 | EXISTS | codec | empty, no fields, missing `10=`, bad tag, missing `=` |

### 5.6 FSM — 10 classes

Folder: `tests/Unit/Fsm/`

Three machines: **trader score state**, **execution-order / ER**, **FIX session status**.

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 75 | `TraderStateMachineFromBaselineTests` | `TraderStateMachine` | P0 | EXISTS | §22, §60, A22 §9 | `N=0` → `INSUFFICIENT_DATA`; `N<3` → `INSUFFICIENT_DATA`; high quality+low risk → `SHADOW`; mid → `WATCH`; else `EARLY_SCORE`; martingale+DD+neg NET → `RISK_BLOCKED` |
| 76 | `ThreeTradeSafetyGateTests` | `TraderStateMachine` | P0 | EXISTS | §23, A22 §9.2 | at N=3, `SuggestedState ∉ {LIVE, LIVE_CANDIDATE}`; `AfterHighEarlyScore() == SHADOW`; `CanPromoteToLive` always false in current code (lock until a later audited promotion type exists) |
| 77 | `ScoreStateTransitionGraphTests` | `TraderStateMachine` + future `ResolveState` | P0 | PARTIAL | A22 §9.5 | legal graph; illegal `INSUFFICIENT_DATA → LIVE*`; `DISQUALIFIED` sticky without reclaim. Implement as theory over `(prev, N, flags, quality)` |
| 78 | `RiskBlockedAndDisqualifiedTransitionsTests` | `TraderStateMachine` | P0 | EXISTS | §60, A22 R0–R3 | severe risk cannot be skipped by a high quality tick; manual DQ not in stub — assert RISK_BLOCKED path |
| 79 | `ScoreRescoringAfterTradeNTests` | `BaselineScorer` | P1 | EXISTS | §22 | scores 3 vs 4 vs 5 differ when the 4th/5th trade is added; state can demote SHADOW → RISK_BLOCKED when martingale appears |
| 80 | `ExecutionReportStateTransitionTests` | `ExecutionOrderStateMachine.Apply` | P0 | EXISTS | §33, §60, A42 | map `0/NEW→Accepted`, `1/PARTIAL→PartiallyFilled`, `2/FILL→Filled`, `4/CANCELED→Cancelled`, `8/REJECTED→Rejected`, `A/PENDING_NEW→Accepted`, unknown → `ExecutionStateUnknown` |
| 81 | `ExecutionOrderTerminalStickyTests` | `ExecutionOrderStateMachine` | P0 | EXISTS | §33 | `Filled` ignores later non-fill; `Rejected`/`Cancelled` sticky; `AfterSendAttempt` = `SentAcknowledgementUnknown` |
| 82 | `ExecutionOrderRetryPolicyTests` | `MayRetryNewOrderSingle` / `RequiresReconciliation` | P0 | EXISTS | §34, A42 | retry only `NotSent`/`Rejected`; reconcile required for sent-unknown and execution-unknown |
| 83 | `FixSessionStatusVocabularyTests` | `FixSessionStatus`, `FixSessionQualifier` | P1 | EXISTS | A25 §2.3 | enum contains Disconnected/Connecting/LogonSent/LoggedOn/Reconciling/ReadyForMarketData/ReadyForExecution/LogoutSent/Error; qualifier Quote≠Trade |
| 84 | `FixSessionReadyForExecutionGateTests` | `FixSessionOwnership.ExecutionIntentsAllowed` + risk `Reconciled` | P0 | EXISTS | §42, §70.14 | Logon ≠ ready; ownership without reconcile ≠ send; unreconciled open → `VENUE_NOT_RECONCILED` |

### 5.7 Symbol mapping — 4 classes in §5.7 plus 4 more

Folder: `tests/Unit/Mapping/`

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 85 | `XauCanonicalMappingTests` | `SymbolNormalizer` | P0 | EXISTS | §16, §60, A44 | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` (+ listed aliases) → `XAUUSD`; case-insensitive |
| 86 | `SymbolNormalizerUnknownSymbolTests` | `SymbolNormalizer` | P0 | EXISTS | A44 | `EURUSD`, empty, whitespace → false / empty canonical; **never** silent XAU |
| 87 | `SymbolNormalizerLookupKeyPolicyTests` | `SymbolNormalizer` | P0 | PARTIAL | A44 §4, **G8** | trim+upper; `XAUUSD.` distinct from `XAUUSD` at persist key; compact-dot behavior is a **documented deviation** — test both current compact match **and** a pending A44-strict theory marked until mapper is persisted |
| 88 | `VenueInstrumentIdMappingTests` | `SymbolNormalizer` | P0 | EXISTS | §16, §30 | `TryMapVenueInstrumentId` false until `RegisterVenueInstrument`; empty id throws; never treat tag 55 string `"XAUUSD"` as a venue id |

Additional mapping classes (continue numbering):

| # | Class | SUT | Pri | Status | § / spec | Must prove |
|---:|---|---|---|---|---|---|
| 89 | `SourceSymbolMappingEntityTests` | `SourceSymbolMapping` | P2 | EXISTS | A44 | `(BrokerId, SourceSymbol)` is the persist key shape used by `TraderDbContext` unique index |
| 90 | `CanonicalInstrumentSeedTests` | `CanonicalInstrument`, `CanonicalInstrumentRef` | P1 | EXISTS | A44 §3.1 | `CanonicalInstrumentRef.XauUsd.Code == "XAUUSD"`; entity `Code` is the only canonical string |
| 91 | `ReconstructionUsesMappedCanonicalTests` | `TradeReconstructor` + `SymbolNormalizer` | P0 | EXISTS | §16, A21 §5 | reconstruct with `GOLD` / `XAUUSDm` sets `CanonicalSymbol=XAUUSD` and `IsXauUsd`; extra mapping override can force non-XAU (broker override) |
| 92 | `NeverAssumeFixTag55IsXauUsdTests` | `SymbolNormalizer` + harness SecurityList | P0 | EXISTS | §16, A32, A44 | `55` numeric ≠ canonical; human name is `1007`; mapping required before risk/copy |

Mapping cluster is classes **85–92**. Grand total = **92**.

---

## 6. Required first methods (do not collapse)

Minimum `[Fact]` / `[Theory]` names. Implementers must not replace a class with a single smoke test.

### Reconstruction

| Class | First methods |
|---|---|
| `TradeReconstructionTests` | `Multiple_deals_one_position_yield_one_trade`; `Unrelated_position_ids_are_separate_trades` |
| `PartialCloseReconstructionTests` | `Partial_out_sets_WasPartialClose_and_keeps_open`; `Partial_does_not_count_as_completed_xau` |
| `ScaleInReconstructionTests` | `Second_in_sets_WasScaledIn_and_updates_vwap`; `MaxVolumeLots_tracks_peak_remaining` |
| `FullCloseReconstructionTests` | `Full_out_marks_completed_and_exit_vwap`; `Net_pnl_is_gross_plus_commission_plus_swap_plus_fees` |
| `PositionReversalReconstructionTests` | `InOut_closes_prior_and_opens_leftover_opposite`; `InOut_exact_flat_does_not_open_next` |
| `FirstThreeCompletedXauTradesTests` | `Three_completed_xau_returns_three`; `Partial_and_open_and_eurusd_do_not_count` |
| `EarlyScoreEligibleLatchTests` | `Two_completed_is_not_eligible`; `Three_completed_is_eligible` |
| `NonTradeDealExclusionTests` | `Balance_credit_commission_are_ignored`; `Buy_sell_only_open_book` |
| `AveragingDownFlagReconstructionTests` | `Long_add_below_vwap_is_averaged_down`; `Long_add_above_vwap_is_not`; `Short_add_above_vwap_is_averaged_down` |
| `ReconstructionBrokerIsolationTests` | `Same_ticket_different_broker_is_not_merged`; `Wrong_login_is_ignored` |
| `ReconstructionSortDeterminismTests` | `Shuffled_deals_match_sorted_deals`; `Replay_is_bit_identical` |

### Scoring

| Class | First methods |
|---|---|
| `DrawdownCalculatorTests` | `Peak_to_trough_on_closed_series`; `Empty_series_is_zero` |
| `MartingaleDetectorTests` | `Size_up_1_8_after_loss_is_martingale`; `Size_up_1_26_after_loss_is_not`; `Size_up_after_win_is_not_martingale` |
| `MfeMaeFeatureQualityTests` | `Deals_only_quality_is_Unavailable`; `Does_not_fill_average_mfe_from_vwap` |
| `TraderScoreCalculatorTests` | `Scores_are_in_0_100`; `Does_not_rank_by_raw_net_pnl` |
| `ThreeTradeSafetyGateTests` | `N3_high_score_is_not_LIVE`; `AfterHighEarlyScore_is_SHADOW` |
| `ScoringCannotBypassRiskContractTests` | `High_score_still_rejected_on_stale_quote` |

### Risk sizing

| Class | First methods |
|---|---|
| `VolumeConverterManagerScaleTests` | `One_lot_is_10000`; `Tenth_lot_is_1000`; `Hundredth_lot_is_100` |
| `SourceDestinationQuantityConversionTests` | `Never_passthrough_MT5_lots`; `BaseUnits_0_10_lot_100oz_is_OrderQty_10`; `LotsConvention_0_10_lot_is_OrderQty_0_10` |
| `QuantityNormalizerStepMinMaxTests` | `Floors_to_step`; `Below_min_returns_zero`; `Above_max_caps` |
| `RiskEngineHardLimitTests` | one theory member per reason code in §5.4 |
| `OpenVsCloseExposurePolicyTests` | `Stale_quote_rejects_open_not_close`; `Kill_switch_allows_reduce` |
| `RealExecutionFeatureFlagTests` | `Default_flag_is_false`; `Approve_with_flag_off_disallows_fix_send` |

### FIX / FSM / mapping

| Class | First methods |
|---|---|
| `FixChecksumValidationTests` | `Valid_checksum_parses`; `Bad_checksum_throws`; `Build_then_parse_round_trips` |
| `ClOrdIdGenerationTests` | `Same_inputs_are_stable`; `Sequence_changes_id`; `Blank_intent_throws` |
| `UnknownExecutionNoBlindRetryTests` | `Disconnect_after_send_is_unknown`; `Unknown_cannot_retry_NOS` |
| `FixSessionOwnershipLeaseTests` | `Second_owner_fails`; `Intents_require_reconcile` |
| `ExecutionReportStateTransitionTests` | `Partial_then_fill`; `Reject_from_new`; `Unknown_ordstatus_is_unknown` |
| `XauCanonicalMappingTests` | `Maps_architecture_aliases`; `Unknown_is_not_xau` |
| `NeverAssumeFixTag55IsXauUsdTests` | `Numeric_55_is_not_canonical`; `Unregistered_venue_id_fails` |
| `TraderStateMachineFromBaselineTests` | `Zero_trades_insufficient`; `Martingale_drawdown_loss_is_risk_blocked` |

---

## 7. §60 unit bullets → this list (1:1)

| §60 required unit item | Class # |
|---|---|
| MT5 deal deduplication | 23 |
| trade reconstruction | 1, 12, 13, 25 |
| partial close | 2 |
| scale-in | 3 |
| full close | 4 |
| position reversal | 5 |
| XAU canonical mapping | 85–88, 91–92 |
| source/destination quantity conversion | 42, 45–49 |
| drawdown | 27 |
| MFE/MAE where data exists | 36, 37 |
| martingale detection | 28 |
| averaging-down detection | 17, 29 |
| score-state transitions | 75–79 |
| risk limits | 50–59 |
| copy-intent idempotency | 68, 55 |
| ClOrdID generation | 67 |
| ExecutionReport state transitions | 80–82, 64, 69 |

A09’s 17 names are **absorbed**. Do not create a second parallel set (`TradeReconstructionTests` vs `Reconstruction.TradeReconstructionTests` — use the namespace in §1, class short name in the table).

---

## 8. Count summary

| Domain | Classes | P0 | EXISTS / PARTIAL / MISSING |
|---|---:|---:|---|
| Reconstruction | 22 | 15 | 20 / 2 / 0 |
| Ingest (feeds recon) | 3 | 2 | 2 / 1 / 0 |
| Scoring / features | 16 | 9 | 11 / 3 / 2 |
| Risk sizing | 18 | 11 | 14 / 1 / 3 |
| FIX | 15 | 9 | 13 / 2 / 0 |
| FSM | 10 | 8 | 9 / 1 / 0 |
| Symbol mapping | 8 | 6 | 7 / 1 / 0 |
| **Total** | **92** | **60** | **76 / 11 / 5** |

Support types (not counted): 8 builders/fixtures in §2.

`A27` unit lane was 36 classes across **all** product areas. This list is **only** the six named domains and is complete for them. Do not drop P0 classes to match the old 17/36 counts.

---

## 9. Implementation order (unit-first)

1. **42, 85, 1–5, 8–9, 17** — volume law, XAU map, reconstruction + first-3 + averaging polarity (G1).
2. **26–28, 34, 75–76** — features, scores, Trade-#3 SHADOW gate.
3. **47, 50–53, 56, 58** — dest step + risk hard limits + kill + flag.
4. **60–62, 67, 69, 80–82** — FIX codec, ClOrdID, ER FSM, no blind retry.
5. **70–73, 84, 92** — ownership, defaults, venue id, ready gate.
6. **45–46, 35, 37, 48–49** — A43 converter and A22/A45 missing SUTs (TDD allowed).

---

## 10. Project-reference notes (do not edit product)

| Need | Action when implementing tests |
|---|---|
| Domain / Application / Fix.CTrader | already referenced |
| `EfTradingStore` deal unique index | **Integration**, not this list (class 23 uses a fake) |
| `FakeMt5BrokerConnector` / `DemoBrokerFactory` | optional later Unit ref to `src/Mt5`, or copy tape literals (preferred for P0) |
| PostgreSQL / QuickFIX socket | out of Unit |

---

## 11. Explicit non-goals

- No ML / XGBoost / leakage-training suites (`A52`, `A80`).
- No Kafka, ClickHouse, K8s, LLM.
- No React / API controller class names (`A26` / `A63`).
- No integration classes from `A27` §5 (migrations, backfill, outbox, live QuickFIX).
- No Replay project classes (`A27` §6) except that unit fixtures must be **reusable** by replay later.
- No product source, csproj, or placeholder test edits in this pass.

---

## 12. Disposition

| Metric | Value |
|---|---|
| Domains covered | reconstruction, scoring, risk sizing, FIX, FSM, symbol mapping |
| Unit classes named | **92** |
| P0 classes | **60** |
| Classes present in `tests/Unit` today | **0** |
| Product source changed | **No** |
| Gaps tests must fail until fixed | G1–G5, G7–G9 (see §4) |

Implement against this file. When a class lands, do not rename it to “cover” a different §60 bullet. One class, one capability cluster.
