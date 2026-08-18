# A27 — Test Inventory (class names)

**Agent:** A27  
**Date:** 2026-08-18  
**Source:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Sections used:** §60 Testing Strategy, §61 FIX Simulation / Test Harness, §69 First Useful Version, §70 Live FIX Execution  
**Supporting context only (not extra scope):** §11–18, §22–24, §27–43, §66 repo layout, §68 go-live gates  
**Constraint:** inventory only. Product source was not modified.

---

## 1. Purpose

Enumerate the **required test classes** for:

| Lane | Architecture | Existing / proposed project |
|---|---|---|
| Unit | §60 Unit tests | `tests/Unit` → `TraderIntelligence.Tests.Unit` |
| Integration | §60 Integration tests | `tests/Integration` → `TraderIntelligence.Tests.Integration` |
| Replay | §60 Replay tests | `tests/Replay` → `TraderIntelligence.Tests.Replay` *(proposed)* |
| FIX harness | §61 FIX Simulation / Test Harness | `tests/Fix` → `TraderIntelligence.Tests.Fix` *(proposed)* |

Placeholders today (`UnitTest1`) are **not** inventory. Class names below are the target xUnit surface. Each test class maps to a proposed SUT (system-under-test) type named from the architecture, not from the current `Class1.cs` stubs.

Do **not** use the real Pepperstone/cTrader account as the first integration test (§61). First useful version (§69) does **not** require ML. Live NewOrderSingle requires §70 + `REAL_COPY_EXECUTION_ENABLED=false` by default.

---

## 2. Naming and layout conventions

```text
TraderIntelligence.Tests.<Lane>.<Area>.<SutOrCapability>Tests
```

- Framework: xUnit + FluentAssertions (already referenced).
- One public test class per capability cluster.
- Fact names: `Method_Scenario_Expected` (or equivalent Given/When/Then).
- Fixtures / harness types are **not** `*Tests`; they live next to the suite they support.
- No live venue I/O in Unit or Replay. FIX harness is in-process / recorded-message only.
- Integration may use local PostgreSQL + in-memory or recorded FIX, never production TRADE.

Proposed tree (matches §66; does not create files):

```text
/tests
  /Unit          TraderIntelligence.Tests.Unit
  /Integration   TraderIntelligence.Tests.Integration
  /Replay        TraderIntelligence.Tests.Replay
  /Fix           TraderIntelligence.Tests.Fix
```

`tests/Risk` from §66 is **not** a separate project in this inventory. Risk classes sit under Unit / Integration / FIX as required by §60–61.

---

## 3. Proposed SUT types (production names the tests bind to)

These are the architecture-facing class names the inventory assumes. They are **targets**, not a claim that they exist in `src/` today.

| Area | SUT / type | Architecture origin |
|---|---|---|
| Identity | `BrokerId`, `Mt5LoginKey`, `Mt5DealKey`, `Mt5OrderKey`, `Mt5PositionKey` | §10 compound identities |
| Connector | `IMt5BrokerConnector`, `Mt5Group`, `Mt5Account`, `Mt5Deal`, `Mt5Order`, `Mt5Position`, `Mt5Event` | §6 |
| Ingest | `Mt5DealDeduplicator`, `Mt5HistoryBackfillService`, `Mt5LiveIngestPipeline`, `SyncCheckpointStore` | §11–12, §60 |
| Outbox | `TransactionalOutboxWriter`, `OutboxProcessor` | §12–13, §60 |
| Reconstruct | `TradeReconstructor`, `ReconstructedTrade`, `FirstThreeTradeCounter` | §14–15, §60 |
| XAU map | `CanonicalInstrument`, `CanonicalInstrumentMapper`, `SourceSymbolMapping`, `DestinationSymbolMapping` | §16, §60 |
| Sizing | `SourceDestinationQuantityConverter`, `PositionSizingCalculator` | §38, §60 |
| Features | `DrawdownCalculator`, `MfeMaeCalculator`, `MartingaleDetector`, `AveragingDownDetector`, `DeterministicFeatureEngine` | §17–18, §60 |
| Score | `TraderScoreCalculator`, `TraderScoreStateMachine`, `ScoreState` | §18, §22, §60 |
| Shadow | `ShadowCopyEngine`, `ShadowOrder`, `ShadowFill`, `ShadowPosition` | §24, §60 replay |
| Intent | `CopyIntent`, `CopyIntentFactory`, `CopyIntentIdempotencyGuard`, `CopyIntentExpiryPolicy` | §32, §36, §63, §60 |
| Risk | `RiskEngine`, `RiskDecision`, `KillSwitch`, `QuoteFreshnessGuard`, `PriceMoveGuard` | §37–41, §60, §70.11–13 |
| Execution | `ApprovedExecutionIntent`, `ClOrdIdGenerator`, `ExecutionIntentStore`, `ExecutionState`, `UnknownExecutionRecoveryService` | §33–34, §60, §70 |
| FIX session | `CTraderQuoteSession`, `CTraderTradeSession`, `CTraderFixSessionConfiguration` | §25–28, §60 |
| FIX codec | `FixMessageParser`, `FixMessageBuilder`, `ExecutionReportHandler`, `PositionReportHandler`, `SecurityListHandler` | §29–30, §60–61 |
| Reconcile | `DestinationPositionMapper`, `CTraderStartupReconciler`, `PeriodicReconciler`, `ReconciliationGate` | §35, §42–43, §60, §70 |
| Feature flag | `RealExecutionFeatureFlags` | §41, §70.12 |

Enums / states the tests must lock:

```text
ScoreState:
  INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW,
  LIVE_CANDIDATE, LIVE, PAUSED, RISK_BLOCKED, DISQUALIFIED

ExecutionState:
  not_sent, sent_ack_unknown, accepted, partially_filled,
  filled, rejected, cancelled, EXECUTION_STATE_UNKNOWN

ExposurePolicy:
  OPEN_EXPOSURE, INCREASE_EXPOSURE, REDUCE_EXPOSURE, CLOSE_EXPOSURE

KillSwitch:
  STOP_NEW_EXECUTION, EMERGENCY_FLATTEN

VenueHealth:
  READY_FOR_EXECUTION | blocked (reconciliation / session / quote stale)
```

---

## 4. Unit inventory — §60 required list

Project: `TraderIntelligence.Tests.Unit`  
Namespace prefix: `TraderIntelligence.Tests.Unit`

### 4.1 Reconstruction cluster

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Reconstruction.Mt5DealDeduplicationTests` | `Mt5DealDeduplicator` | MT5 deal deduplication | Same `(broker_id, deal_ticket)` is a no-op; different brokers with the same ticket are distinct; replay of live+backfill does not double-count |
| `Reconstruction.TradeReconstructionTests` | `TradeReconstructor` | trade reconstruction | `Order != Deal != Position != Logical Trade`; one `ReconstructedTrade` per position lifecycle |
| `Reconstruction.PartialCloseReconstructionTests` | `TradeReconstructor` | partial close | Partial close does **not** increment first-3-trade counter; `was_partial_close`; VWAP/volume fields |
| `Reconstruction.ScaleInReconstructionTests` | `TradeReconstructor` | scale-in | Scale-in stays one logical trade; `was_scaled_in`; `max_volume` |
| `Reconstruction.FullCloseReconstructionTests` | `TradeReconstructor` | full close | `completed=true`; `closed_at`; net PnL = gross − commission − swap − fees |
| `Reconstruction.PositionReversalReconstructionTests` | `TradeReconstructor` | position reversal | Close of old lifecycle + new opposite `ReconstructedTrade`; no ticket reuse across lifecycles |
| `Reconstruction.FirstThreeCompletedXauTradesTests` | `FirstThreeTradeCounter` | (supports §69.6) | Counts only **completed reconstructed XAUUSD** lifecycles; ignores order place, deal fill, SL/TP, partial close as a “trade” |

### 4.2 Canonical mapping / quantity

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Mapping.XauCanonicalMappingTests` | `CanonicalInstrumentMapper` | XAU canonical mapping | `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD` → `CanonicalInstrument.XAUUSD`; never assume FIX tag 55 is the string `"XAUUSD"` |
| `Mapping.SourceSymbolMappingTests` | `SourceSymbolMapping` | XAU canonical mapping | Per-broker source symbol → canonical; missing mapping is explicit fail, not silent pass-through |
| `Mapping.DestinationInstrumentMappingTests` | `DestinationSymbolMapping` | XAU canonical mapping | cTrader numeric instrument ID → canonical XAUUSD; no hardcoded foreign-account ID |
| `Sizing.SourceDestinationQuantityConversionTests` | `SourceDestinationQuantityConverter` | source/destination quantity conversion | MT5 lots ≠ destination `OrderQty` unless mapping says so; min/step/precision rounding; known-example table |
| `Sizing.PositionSizingCalculatorTests` | `PositionSizingCalculator` | quantity conversion + §38 | source volume → canonical notional → allocation → destination qty; margin/exposure caps |

### 4.3 Features / risk flags

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Features.DrawdownCalculatorTests` | `DrawdownCalculator` | drawdown | Peak-to-trough on reconstructed equity path; isolated vs portfolio |
| `Features.MfeMaeCalculatorTests` | `MfeMaeCalculator` | MFE/MAE where data exists | Computes only when `price_source` + in-trade ticks exist; otherwise `feature_quality=APPROXIMATE` or omitted — **never fabricated from close deals alone** |
| `Features.MfeMaeMissingTickDataTests` | `MfeMaeCalculator` | MFE/MAE where data exists | Missing ticks → no silent mix of Achiever ticks with cTrader quotes |
| `Features.MartingaleDetectorTests` | `MartingaleDetector` | martingale detection | Size-up-after-loss patterns; flag on / off thresholds |
| `Features.AveragingDownDetectorTests` | `AveragingDownDetector` | averaging-down detection | Adds to a losing position → `was_averaged_down` + risk flag |
| `Features.DeterministicFeatureEngineTests` | `DeterministicFeatureEngine` | (feeds scores / §69.7) | net pnl, P/L ratio, lot consistency, holding time, SL use, session — deterministic given same trades |

### 4.4 Score-state transitions

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Scoring.ScoreStateTransitionTests` | `TraderScoreStateMachine` | score-state transitions | Legal graph: `INSUFFICIENT_DATA` → `EARLY_SCORE` (on trade #3 close) → `WATCH` / `SHADOW`; never `INSUFFICIENT_DATA` → `LIVE` |
| `Scoring.EarlyScoreEligibleTests` | `TraderScoreStateMachine` | score-state transitions + §15 | Trade #3 close → `EARLY_SCORE_ELIGIBLE` / `EARLY_SCORE`, **not** `PROVEN_PROFITABLE` |
| `Scoring.TraderScoreCalculatorTests` | `TraderScoreCalculator` | (supports §69.7–8) | Deterministic `risk_score`, `behavior_score`, `early_quality_score` |
| `Scoring.ThreeTradeSafetyGateTests` | `TraderScoreStateMachine` | §23 / §69 | Trade #3 + high score defaults to **SHADOW only** |
| `Scoring.ScoreRescoringAfterTradeNTests` | `TraderScoreStateMachine` | §22 | Rescore after trades 4, 5, 6…; history appended, not overwritten blindly |
| `Scoring.RiskBlockedAndDisqualifiedTransitionsTests` | `TraderScoreStateMachine` | score-state transitions | `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` cannot be skipped by a high ML/score tick |

### 4.5 Risk limits

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Risk.RiskEngineHardLimitTests` | `RiskEngine` | risk limits | max loss/trader, daily loss, portfolio DD, gross/net XAU, max qty, max open positions, spread, quote age, signal age, price move, slippage, margin, martingale block, abnormal size, venue health |
| `Risk.RiskEngineApproveReduceRejectTests` | `RiskEngine` | risk limits | Outputs only `approve` / `reduce size` / `reject` / `pause trader` / `pause venue` / `global stop` |
| `Risk.OpenVsCloseExposurePolicyTests` | `RiskEngine` | §64 | Stricter on `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` than `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` |
| `Risk.QuoteFreshnessGuardTests` | `QuoteFreshnessGuard` | risk limits + §31 | `quote_age > max` → reject new copy; threshold configurable |
| `Risk.PriceMoveGuardTests` | `PriceMoveGuard` | §37 | `PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE` |
| `Risk.StaleCopyIntentExpiryTests` | `CopyIntentExpiryPolicy` | §36, §63 | `expires_at` / `max_signal_age`; no catch-up of 20 stale entries after 3-minute FIX outage |
| `Risk.KillSwitchStopNewExecutionTests` | `KillSwitch` | §40, §70.13 | `STOP_NEW_EXECUTION` blocks new copy orders; leaves existing positions |
| `Risk.KillSwitchEmergencyFlattenAuthorizationTests` | `KillSwitch` | §40 | `EMERGENCY_FLATTEN` is separate and more privileged |
| `Risk.ScoringCannotBypassRiskTests` | `RiskEngine` | §39, rule 15 | High score / ML candidate still rejected by hard limits |
| `Risk.RealExecutionFeatureFlagTests` | `RealExecutionFeatureFlags` | §41, §70.12 | `REAL_COPY_EXECUTION_ENABLED=false` cannot emit NewOrderSingle |

### 4.6 Copy intent / FIX order identity / ER states

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Execution.CopyIntentIdempotencyTests` | `CopyIntentIdempotencyGuard` | copy-intent idempotency | Same source event / trade / login cannot create a second live intent |
| `Execution.CopyIntentFactoryTests` | `CopyIntentFactory` | copy-intent idempotency | Persist-before-send fields: source ids, symbol, side, qty, `expires_at` |
| `Execution.ClOrdIdGenerationTests` | `ClOrdIdGenerator` | ClOrdID generation | Unique per destination order; stable if regenerated from same `execution_intent_id`; never reused after send |
| `Execution.ExecutionReportStateTransitionTests` | `ExecutionReportHandler` | ExecutionReport state transitions | Legal `ExecutionState` graph including partial fill, reject, cancel |
| `Execution.UnknownExecutionStateTests` | `UnknownExecutionRecoveryService` | ER transitions + §34 | Disconnect after send → `EXECUTION_STATE_UNKNOWN`; **no** blind NewOrderSingle retry |
| `Execution.DestinationPositionMappingTests` | `DestinationPositionMapper` | §35 | Source reconstructed trade ↔ dest orders ↔ dest position IDs; scale-in / partial / close / reversal |

### 4.7 FIX codec (no socket)

| Test class | SUT | §60 / §61 | Must prove |
|---|---|---|---|
| `Fix.FixMessageParseBuildTests` | `FixMessageParser`, `FixMessageBuilder` | FIX message parse/build (unit slice of §60 integration) | Round-trip Logon, ER, MDIncRefresh, SecurityList, PositionReport with cTrader header fields |
| `Fix.CTraderHeaderMappingTests` | `CTraderFixSessionConfiguration` | §26 | `SenderSubID` / `TargetSubID` configurable; `cServer` case preserved; QUOTE vs TRADE independent |
| `Fix.QuoteAndTradeSessionIsolationTests` | `CTraderQuoteSession`, `CTraderTradeSession` | §27 | Independent sequence / heartbeat / reconnect state; no shared seq |

---

## 5. Integration inventory — §60 required list

Project: `TraderIntelligence.Tests.Integration`  
Namespace prefix: `TraderIntelligence.Tests.Integration`

These classes may use local PostgreSQL (Testcontainers or the existing lab Postgres), EF migrations, and recorded/stub FIX. They must **not** open the production TRADE port as the first suite.

| Test class | SUT | §60 requirement | Must prove |
|---|---|---|---|
| `Persistence.PostgreSqlMigrationTests` | Infrastructure DbContext / migrations | PostgreSQL migrations | Fresh apply of core + execution tables (§44–45) is idempotent; down/up or repeat apply does not corrupt |
| `Persistence.CoreSchemaContractTests` | migrations | PostgreSQL migrations | Tables listed in §45 exist with `broker_id` on source tables |
| `Mt5.Mt5BackfillRestartTests` | `Mt5HistoryBackfillService`, `SyncCheckpointStore` | MT5 backfill/restart | Checkpoint resume; no duplicate raw deals after kill/restart |
| `Mt5.Mt5LiveIngestIdempotencyTests` | `Mt5LiveIngestPipeline` | MT5 backfill/restart | Live deal + later backfill of same ticket → one row |
| `Mt5.DualBrokerIsolationTests` | `IMt5BrokerConnector` + stores | (supports §69.1–3) | Achiever vs StarwaveFX logins/tickets never collide |
| `Outbox.OutboxProcessingTests` | `TransactionalOutboxWriter`, `OutboxProcessor` | outbox processing | Persist raw + outbox in one commit; crash before process → at-least-once; handler is idempotent |
| `Outbox.OutboxDoesNotCallFixFromCallbackTests` | `Mt5LiveIngestPipeline` | outbox processing + §32 | MT5 callback path writes outbox only; no `NewOrderSingle` on the ingest thread |
| `Fix.QuickFixnSessionConfigurationTests` | `CTraderFixSessionConfiguration` | QuickFIX/n session configuration | QUOTE 5211 / TRADE 5212 SSL defaults; independent session qualifier files; dictionary is cTrader RoE, not generic FIX 4.4 only |
| `Fix.FixMessageParseBuildIntegrationTests` | `FixMessageParser`, `FixMessageBuilder` | FIX message parse/build | QuickFIX/n + cTrader data dictionary parse/build recorded messages |
| `Fix.ExecutionReportHandlingTests` | `ExecutionReportHandler` | ExecutionReport handling | Persist `fix_execution_reports`; advance `fix_orders` + destination position |
| `Reconcile.PositionReconciliationTests` | `CTraderStartupReconciler`, `PeriodicReconciler` | position reconciliation | Internal vs venue orders/positions; issues for unknown/missing/qty/side/orphan/unexpected fill |
| `Reconcile.StartupReconciliationGateTests` | `ReconciliationGate` | position reconciliation + §42 | After logon, new execution blocked until `READY_FOR_EXECUTION` |
| `Reconcile.UnknownExecutionRecoveryTests` | `UnknownExecutionRecoveryService` | unknown-execution recovery | Uses OrderStatusRequest / OrderMassStatusRequest / ERs / positions; may send a **new** order only after reconcile |
| `Flags.RealExecutionDisabledIntegrationTests` | `RealExecutionFeatureFlags`, FIX worker | §41 / §70.12 | TRADE session may logon + request status; NewOrderSingle path is closed |

---

## 6. Replay inventory — §60 pipeline

Project: `TraderIntelligence.Tests.Replay`  
Namespace prefix: `TraderIntelligence.Tests.Replay`

Architecture pipeline:

```text
historical MT5 events → replay → reconstruction → features → scores → shadow copy
```

Goal: **deterministic debugging**. Same fixture + same clock → same artifacts (ids except where time-uuid is injected via a fake clock).

### 6.1 Harness / fixture classes (not `*Tests`)

| Class | Role |
|---|---|
| `Mt5HistoricalEventFixture` | Load recorded Achiever/StarwaveFX deals/orders/positions |
| `Mt5EventReplayer` | Drive events in timestamp order into ingest/outbox |
| `DeterministicClock` | Freeze `source_event_time` / `decision_time` |
| `ReplayHarness` | Compose reconstruct → features → scores → shadow |
| `ReplaySnapshotAsserter` | Compare reconstructed trades / scores / shadow PnL to golden files |
| `XauUsdFirstThreeTradeFixture` | Minimal fixture that yields exactly 3 completed XAUUSD trades |

### 6.2 Replay test classes

| Test class | Pipeline stage | Must prove |
|---|---|---|
| `Replay.HistoricalMt5EventReplayTests` | historical events → replay | Ordered ingest; broker_id preserved; duplicates in the tape are dropped |
| `Replay.ReconstructionFromReplayTests` | replay → reconstruction | Golden `ReconstructedTrade` set (partial, scale-in, full close, reversal) |
| `Replay.FeatureComputationFromReplayTests` | reconstruction → features | Deterministic feature snapshot; MFE/MAE only when fixture includes ticks |
| `Replay.ScoreComputationFromReplayTests` | features → scores | Deterministic rank + `EARLY_SCORE` after third completed XAU trade |
| `Replay.ShadowCopyFromReplayTests` | scores → shadow copy | Shadow entries/exits priced from **destination** quote tape, not source last-deal |
| `Replay.EndToEndReplayDeterminismTests` | full pipeline | Two runs, bit-identical scores/shadow PnL given fake clock + quotes |
| `Replay.FirstUsefulVersionReplayAcceptanceTests` | §69.4–8, §69.11 | Captures XAUUSD, reconstructs, detects first 3 completed trades, scores, ranks, shadow-copies selected traders |
| `Replay.NoBlindCatchUpReplayTests` | §63 | FIX-down gap in the tape does not fire expired `CopyIntent`s on resume |
| `Replay.DataLeakageGuardReplayTests` | §20 | Score at trade #3 close cannot see trade #4+ or future equity |

---

## 7. FIX simulation / test harness — §61

Project: `TraderIntelligence.Tests.Fix`  
Namespace prefix: `TraderIntelligence.Tests.Fix`

**Rule:** adapter test mode **before** any real `NewOrderSingle`. Do not use the real account as the first integration test.

### 7.1 Harness classes (the simulator)

| Class | §61 capability | Responsibility |
|---|---|---|
| `FixAdapterTestMode` | test mode entry | In-process FIX adapter; no TCP to `p.c-trader.com` |
| `RecordedFixMessageStore` | shared | Load captured FIX 4.4 / cTrader messages (ER, MD, Reject, Logout) |
| `RecordedExecutionReportParser` | parse recorded ExecutionReports | Feed stored ERs through `ExecutionReportHandler` |
| `MarketDataIncrementalRefreshReplayer` | replay MarketDataIncrementalRefresh | Drive QUOTE book / last bid-ask for shadow + risk |
| `FixDisconnectSimulator` | simulate disconnects | Drop session mid-heartbeat, mid-NOS, mid-ER stream |
| `DuplicateExecutionReportSimulator` | simulate duplicate ExecutionReports | Replay same `ExecID` / same fill twice |
| `PartialFillSimulator` | simulate partial fill | ER `OrdStatus=1` then `2`; qty accumulation |
| `OrderRejectSimulator` | simulate rejection | Session Reject + BusinessMessageReject + ER `OrdStatus=8` |
| `UnknownStateDisconnectSimulator` | simulate unknown-state disconnect | NOS sent, no ER, socket dead → `EXECUTION_STATE_UNKNOWN` |
| `SecurityListReplayStub` | §30 / §69.10 | Return Pepperstone XAUUSD instrument ID from recorded SecurityList |
| `PositionReportReplayStub` | §42 / §70.3 | RequestForPositions / PositionReport tape for restart reconcile |
| `OrderMassStatusReplayStub` | §42 / §70.6 | OrderMassStatusRequest responses |
| `FixSequenceGapSimulator` | §29 | ResendRequest / sequence reset paths |
| `CTraderHeaderCaseStub` | §26 | Prove `cServer` / SenderSubID / TargetSubID wiring in test mode |

### 7.2 FIX harness test classes

| Test class | Harness used | Must prove |
|---|---|---|
| `Harness.FixAdapterTestModeDoesNotHitVenueTests` | `FixAdapterTestMode` | No live host/port; test mode cannot be “accidentally production” |
| `Harness.RecordedExecutionReportParseTests` | `RecordedExecutionReportParser` | Parse recorded ERs; persist; map to `ExecutionState` |
| `Harness.MarketDataIncrementalRefreshReplayTests` | `MarketDataIncrementalRefreshReplayer` | Quote cache updates; stale-quote clock advances |
| `Harness.DisconnectDuringHeartbeatTests` | `FixDisconnectSimulator` | QUOTE vs TRADE independent; reconnect does not share seq |
| `Harness.DisconnectAfterNewOrderSingleTests` | `FixDisconnectSimulator` + `UnknownStateDisconnectSimulator` | State = unknown; no automatic resend of same ClOrdID as a **new** NOS |
| `Harness.DuplicateExecutionReportTests` | `DuplicateExecutionReportSimulator` | Second identical ER is idempotent; fills not double-booked |
| `Harness.PartialFillLifecycleTests` | `PartialFillSimulator` | Partial then fill; dest position qty; copy mapping stays one position |
| `Harness.OrderRejectLifecycleTests` | `OrderRejectSimulator` | Reject is terminal; intent not retried as a duplicate ClOrdID; risk/audit recorded |
| `Harness.UnknownStateRecoveryTests` | `UnknownStateDisconnectSimulator` + mass-status stub | Recovery via status/position reports; only then optional replacement order with **new** ClOrdID |
| `Harness.CancelReplaceInTestModeTests` | `FixAdapterTestMode` | OrderCancelRequest / CancelReplace / CancelReject where required (§70.9) |
| `Harness.SecurityListXauDiscoveryTests` | `SecurityListReplayStub` | Discover and persist Pepperstone XAUUSD instrument ID (§69.10) |
| `Harness.StartupReconciliationAfterSimulatedRestartTests` | position + mass-status stubs | Restart → block → reconcile → `READY_FOR_EXECUTION` or stay blocked (§70.3) |
| `Harness.ReconciliationBlocksExecutionWhileInconsistentTests` | `ReconciliationGate` | Inconsistent book → no NOS (§70.14) |
| `Harness.RiskRejectionBeforeFixSendTests` | `FixAdapterTestMode` + `RiskEngine` | Failed risk never reaches builder/send (§70.11) |
| `Harness.GlobalStopNewOrdersTests` | `KillSwitch` | `STOP_NEW_EXECUTION` honored in adapter test mode (§70.13) |
| `Harness.UniqueClOrdIdUnderRetryTests` | `ClOrdIdGenerator` | Unique ClOrdID rules under reconnect/retry (§70.4) |
| `Harness.QuoteUnavailableBlocksNewCopyTests` | MD replayer + risk | QUOTE down / stale → no new live copy requiring fresh pricing (§62) |
| `Harness.TradeUnavailableDoesNotQueueUnlimitedBacklogTests` | disconnect sim | TRADE down → intents expire / mark stale; no unbounded NOS queue (§62) |

---

## 8. Acceptance mapping

### 8.1 §69 — First useful version (no ML)

| # | Criterion | Primary test class(es) | Lane |
|---|---|---|---|
| 1 | Connect to both MT5 brokers | `Mt5.DualBrokerIsolationTests` (+ connector health in integration; live soak is ops, not first unit) | Integration |
| 2 | Discover all groups | `Mt5.Mt5BackfillRestartTests` (group sync slice) | Integration |
| 3 | Synchronize ~5,000 accounts | `Mt5.Mt5BackfillRestartTests` (checkpointed account sync; volume as fixture/scale later) | Integration |
| 4 | Capture XAUUSD trades correctly | `Mapping.XauCanonicalMappingTests`, `Replay.HistoricalMt5EventReplayTests` | Unit + Replay |
| 5 | Reconstruct logical trades | `Reconstruction.TradeReconstructionTests`, `Replay.ReconstructionFromReplayTests` | Unit + Replay |
| 6 | Detect first 3 completed XAUUSD trades | `Reconstruction.FirstThreeCompletedXauTradesTests`, `Replay.FirstUsefulVersionReplayAcceptanceTests` | Unit + Replay |
| 7 | Deterministic trader/risk score | `Scoring.TraderScoreCalculatorTests`, `Replay.ScoreComputationFromReplayTests` | Unit + Replay |
| 8 | Rank traders | `Replay.ScoreComputationFromReplayTests` | Replay |
| 9 | Connect to cTrader QUOTE FIX securely | `Fix.QuickFixnSessionConfigurationTests` (config/TLS defaults); live logon is staging, not first harness | Integration + ops |
| 10 | Discover Pepperstone XAUUSD instrument ID | `Harness.SecurityListXauDiscoveryTests`, `Mapping.DestinationInstrumentMappingTests` | FIX + Unit |
| 11 | Shadow-copy selected traders using destination quotes | `Replay.ShadowCopyFromReplayTests`, `Harness.MarketDataIncrementalRefreshReplayTests` | Replay + FIX |
| 12 | Show all of this in React | **out of backend test-class scope** — API contract tests may be added later; not in §60–61 class list |

### 8.2 §70 — Live FIX execution (before production)

| # | Criterion | Primary test class(es) | Lane |
|---|---|---|---|
| 1 | TRADE FIX Logon is stable | `Fix.QuickFixnSessionConfigurationTests`, `Harness.DisconnectDuringHeartbeatTests` | Integration + FIX |
| 2 | ExecutionReports persisted correctly | `Fix.ExecutionReportHandlingTests`, `Harness.RecordedExecutionReportParseTests` | Integration + FIX |
| 3 | Position reports reconcile after restart | `Reconcile.PositionReconciliationTests`, `Harness.StartupReconciliationAfterSimulatedRestartTests` | Integration + FIX |
| 4 | Unique ClOrdID rules proven | `Execution.ClOrdIdGenerationTests`, `Harness.UniqueClOrdIdUnderRetryTests` | Unit + FIX |
| 5 | Duplicate report handling proven | `Harness.DuplicateExecutionReportTests` | FIX |
| 6 | Unknown-state recovery proven | `Execution.UnknownExecutionStateTests`, `Reconcile.UnknownExecutionRecoveryTests`, `Harness.UnknownStateRecoveryTests` | Unit + Integration + FIX |
| 7 | Partial fills supported | `Reconstruction.PartialCloseReconstructionTests` (source), `Harness.PartialFillLifecycleTests` (dest) | Unit + FIX |
| 8 | Order rejects supported | `Harness.OrderRejectLifecycleTests` | FIX |
| 9 | Cancel/replace supported where required | `Harness.CancelReplaceInTestModeTests` | FIX |
| 10 | Destination position mapping correct | `Execution.DestinationPositionMappingTests` | Unit |
| 11 | Risk-engine rejection before FIX send | `Risk.RiskEngineHardLimitTests`, `Harness.RiskRejectionBeforeFixSendTests` | Unit + FIX |
| 12 | Real execution feature-flagged | `Risk.RealExecutionFeatureFlagTests`, `Flags.RealExecutionDisabledIntegrationTests` | Unit + Integration |
| 13 | Global stop-new-orders works | `Risk.KillSwitchStopNewExecutionTests`, `Harness.GlobalStopNewOrdersTests` | Unit + FIX |
| 14 | Reconciliation blocks execution while inconsistent | `Reconcile.StartupReconciliationGateTests`, `Harness.ReconciliationBlocksExecutionWhileInconsistentTests` | Integration + FIX |

---

## 9. Count summary

| Lane | Test classes | Support / harness types | Source |
|---|---:|---:|---|
| Unit | 36 | — | §60 required bullets + necessary locks for §69–70 |
| Integration | 14 | — | §60 integration list + gates for §70.12 |
| Replay | 9 | 6 fixtures | §60 replay pipeline + §69.4–8,11 |
| FIX harness | 18 | 14 simulators | §61 capabilities + §70.1–14 |
| **Total** | **77** | **20** | |

§60 unit bullets covered 1:1:

| §60 unit bullet | Class |
|---|---|
| MT5 deal deduplication | `Mt5DealDeduplicationTests` |
| trade reconstruction | `TradeReconstructionTests` |
| partial close | `PartialCloseReconstructionTests` |
| scale-in | `ScaleInReconstructionTests` |
| full close | `FullCloseReconstructionTests` |
| position reversal | `PositionReversalReconstructionTests` |
| XAU canonical mapping | `XauCanonicalMappingTests` (+ source/dest mapping) |
| source/destination quantity conversion | `SourceDestinationQuantityConversionTests` |
| drawdown | `DrawdownCalculatorTests` |
| MFE/MAE where data exists | `MfeMaeCalculatorTests`, `MfeMaeMissingTickDataTests` |
| martingale detection | `MartingaleDetectorTests` |
| averaging-down detection | `AveragingDownDetectorTests` |
| score-state transitions | `ScoreStateTransitionTests` (+ early/safety/rescore) |
| risk limits | `RiskEngineHardLimitTests` (+ related guards) |
| copy-intent idempotency | `CopyIntentIdempotencyTests` |
| ClOrdID generation | `ClOrdIdGenerationTests` |
| ExecutionReport state transitions | `ExecutionReportStateTransitionTests` |

§60 integration bullets covered 1:1:

| §60 integration bullet | Class |
|---|---|
| PostgreSQL migrations | `PostgreSqlMigrationTests` |
| MT5 backfill/restart | `Mt5BackfillRestartTests` |
| outbox processing | `OutboxProcessingTests` |
| QuickFIX/n session configuration | `QuickFixnSessionConfigurationTests` |
| FIX message parse/build | `FixMessageParseBuildIntegrationTests` |
| ExecutionReport handling | `ExecutionReportHandlingTests` |
| position reconciliation | `PositionReconciliationTests` |
| unknown-execution recovery | `UnknownExecutionRecoveryTests` |

§61 harness capabilities covered 1:1:

| §61 capability | Class / harness |
|---|---|
| FIX adapter test mode | `FixAdapterTestMode` |
| parse recorded ExecutionReports | `RecordedExecutionReportParser` + `RecordedExecutionReportParseTests` |
| replay MarketDataIncrementalRefresh | `MarketDataIncrementalRefreshReplayer` + `MarketDataIncrementalRefreshReplayTests` |
| simulate disconnects | `FixDisconnectSimulator` + disconnect tests |
| simulate duplicate ExecutionReports | `DuplicateExecutionReportSimulator` + `DuplicateExecutionReportTests` |
| simulate partial fill | `PartialFillSimulator` + `PartialFillLifecycleTests` |
| simulate rejection | `OrderRejectSimulator` + `OrderRejectLifecycleTests` |
| simulate unknown-state disconnect | `UnknownStateDisconnectSimulator` + `UnknownStateRecoveryTests` |

---

## 10. Explicit non-goals for this inventory

- No ML / XGBoost / leakage-training suites as a §69 gate (replay has a **guard** only).
- No Kafka, ClickHouse, K8s, LLM tests (§71).
- No React component class names (§69.12 is UI; backend inventory stops at API/data).
- No product source, test project, or placeholder `UnitTest1` edits in this agent pass.
- First FIX suite stays in `FixAdapterTestMode`. Real-account logon is a later staging checklist, not a unit/integration default.

---

## 11. Implementation order (test-first, matches §67)

1. **Unit reconstruction + XAU mapping + first-3 counter** — unblock §69.4–6.  
2. **Unit features + deterministic scores + state machine** — unblock §69.7–8.  
3. **Integration migrations + backfill/restart + outbox** — unblock §69.1–3.  
4. **Replay golden tape** — deterministic debug of 1–3.  
5. **FIX harness test mode** (ER / MD / disconnect / dup / partial / reject / unknown) — before any live NOS.  
6. **Shadow replay + dest quotes** — §69.11.  
7. **Risk + flags + reconcile + ClOrdID** — §70 gate; only then consider `REAL_COPY_EXECUTION_ENABLED`.

---

## 12. Current repo vs inventory

| Path | Status vs this inventory |
|---|---|
| `D:\Prop\tests\Unit\UnitTest1.cs` | Placeholder only |
| `D:\Prop\tests\Integration\UnitTest1.cs` | Placeholder only |
| `D:\Prop\tests\Replay` | Missing (proposed) |
| `D:\Prop\tests\Fix` | Missing (proposed) |
| `src\*\Class1.cs` | SUT types above are **not** implemented yet |

This report is the class-level backlog. It does not create those projects or types.
