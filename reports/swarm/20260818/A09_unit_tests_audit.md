# A09 — Unit tests audit (`tests/Unit` vs Architecture §60)

**Date:** 2026-08-18  
**Auditor:** senior engineer (swarm A09)  
**Scope:** `D:\Prop\tests\Unit` vs Architecture §60 required **unit** tests only  
**Sources:**
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §60 “Testing Strategy” (lines 2228–2256)
- `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`
- `D:\Prop\tests\Unit\UnitTest1.cs`
- `D:\Prop\Mt5TraderIntelligence.sln`

**Product source:** not modified.

---

## Verdict

**FAIL / 0 of 17 required unit-test areas exist.**

`tests/Unit` is a scaffold: one empty xUnit placeholder (`UnitTest1.Test1`). No `[Fact]` asserts anything. No class name matches any §60 required item.

Architecture §60 lists **17 required unit-test areas** (not 60 tests). Each area below is mapped 1:1 to a **future xUnit test class**. Classes are named for the future SUT, not for the current `Class1` stubs.

---

## Current `tests/Unit` inventory

| Path | Role | Status |
|---|---|---|
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | xUnit + FluentAssertions + Moq + coverlet; TFM `net8.0` | Present |
| `D:\Prop\tests\Unit\UnitTest1.cs` | `TraderIntelligence.Tests.Unit.UnitTest1.Test1()` empty body | Placeholder only |
| `D:\Prop\tests\Unit\obj\*` | NuGet restore artifacts | Ignore |

**Project references today:** Domain, Application, Fix.CTrader.  
**Not referenced:** Infrastructure, Mt5. That is enough for domain/application unit tests; deal-dedup / FIX parser tests will need additional refs when those types exist.

**Runnable facts found:** 1 (`Test1`) — **no assertions**. Coverage of §60: **0/17**.

Product assemblies under `src/*` are still `Class1` stubs. Future class names below assume the types named in architecture §§11–18, 22, 32–39 (not present yet).

---

## Required list (Architecture §60, Unit tests)

Quoted from the architecture `Required:` block:

1. MT5 deal deduplication  
2. trade reconstruction  
3. partial close  
4. scale-in  
5. full close  
6. position reversal  
7. XAU canonical mapping  
8. source/destination quantity conversion  
9. drawdown  
10. MFE/MAE where data exists  
11. martingale detection  
12. averaging-down detection  
13. score-state transitions  
14. risk limits  
15. copy-intent idempotency  
16. ClOrdID generation  
17. ExecutionReport state transitions  

§60 Integration / Replay lists are **out of this mapping**. They belong in `tests/Integration` and a future `tests/Replay`, not `tests/Unit`.

---

## Mapping: required test → future test class

Namespace root: `TraderIntelligence.Tests.Unit`.  
Folder = last namespace segment.  
File = `{ClassName}.cs`.  
xUnit class suffix: `Tests`.

| # | §60 required test | Future test class | Future path under `tests/Unit/` | Future SUT (architecture) | Arch refs | Current coverage |
|---|-------------------|-------------------|---------------------------------|---------------------------|-----------|------------------|
| 1 | MT5 deal deduplication | `Mt5DealDeduplicationTests` | `Ingestion/Mt5DealDeduplicationTests.cs` | `DealDeduplicator` (live ingest: validate → deduplicate → persist raw) | §12, §60 | Missing |
| 2 | trade reconstruction | `TradeReconstructionTests` | `Reconstruction/TradeReconstructionTests.cs` | `TradeReconstructor` / `ReconstructedTrade` (Order ≠ Deal ≠ Position ≠ logical trade) | §14, §15, §60 | Missing |
| 3 | partial close | `PartialCloseReconstructionTests` | `Reconstruction/PartialCloseReconstructionTests.cs` | `TradeReconstructor` (`was_partial_close`, closed vs remaining volume) | §14, §35, §60 | Missing |
| 4 | scale-in | `ScaleInReconstructionTests` | `Reconstruction/ScaleInReconstructionTests.cs` | `TradeReconstructor` (`was_scaled_in`, max_volume, entry VWAP) | §14, §35, §60 | Missing |
| 5 | full close | `FullCloseReconstructionTests` | `Reconstruction/FullCloseReconstructionTests.cs` | `TradeReconstructor` (`completed`, exit VWAP, net realized P&L) | §14, §35, §60 | Missing |
| 6 | position reversal | `PositionReversalReconstructionTests` | `Reconstruction/PositionReversalReconstructionTests.cs` | `TradeReconstructor` (close + opposite open as distinct lifecycles) | §35, §60 | Missing |
| 7 | XAU canonical mapping | `XauCanonicalMappingTests` | `Normalization/XauCanonicalMappingTests.cs` | `CanonicalInstrumentMapper` (`XAUUSD` / `XAUUSDm` / `GOLD` / cTrader ID → `XAUUSD`) | §16, §30, §60 | Missing |
| 8 | source/destination quantity conversion | `SourceDestinationQuantityConversionTests` | `Normalization/SourceDestinationQuantityConversionTests.cs` | `QuantityConverter` (lots ↛ OrderQty; contract size, min/step) | §38 (line 1492), §60 | Missing |
| 9 | drawdown | `DrawdownCalculatorTests` | `Features/DrawdownCalculatorTests.cs` | `DrawdownCalculator` (equity / reconstructed-trade series) | §18, §39, §60 | Missing |
| 10 | MFE/MAE where data exists | `MfeMaeCalculatorTests` | `Features/MfeMaeCalculatorTests.cs` | `MfeMaeCalculator` (compute only with tick/price observations; never invent from closed deals) | §1.5, §17, §60 | Missing |
| 11 | martingale detection | `MartingaleDetectorTests` | `Features/MartingaleDetectorTests.cs` | `MartingaleDetector` | §18, §39, §60 | Missing |
| 12 | averaging-down detection | `AveragingDownDetectorTests` | `Features/AveragingDownDetectorTests.cs` | `AveragingDownDetector` (`was_averaged_down`) | §14, §18, §60 | Missing |
| 13 | score-state transitions | `ScoreStateTransitionTests` | `Scoring/ScoreStateTransitionTests.cs` | `ScoreStateMachine` (`INSUFFICIENT_DATA` → `EARLY_SCORE` → `WATCH`/`SHADOW`/`LIVE`/`PAUSED`/`RISK_BLOCKED`/`DISQUALIFIED`) | §15, §22, §60 | Missing |
| 14 | risk limits | `RiskLimitEngineTests` | `Risk/RiskLimitEngineTests.cs` | `RiskEngine` (hard limits: DD, exposure, quote/signal age, martingale block, kill switch) | §39, §40, §60 | Missing |
| 15 | copy-intent idempotency | `CopyIntentIdempotencyTests` | `Execution/CopyIntentIdempotencyTests.cs` | `CopyIntentFactory` (same source event → one intent) | §32, §33, §60 | Missing |
| 16 | ClOrdID generation | `ClOrdIdGeneratorTests` | `Execution/ClOrdIdGeneratorTests.cs` | `ClOrdIdGenerator` (unique, persist-before-send) | §33, §70.4, §60 | Missing |
| 17 | ExecutionReport state transitions | `ExecutionReportStateTransitionTests` | `Execution/ExecutionReportStateTransitionTests.cs` | `ExecutionReportStateMachine` (`not sent` / `sent unknown` / `accepted` / `partial` / `filled` / `rejected` / `cancelled` / `EXECUTION_STATE_UNKNOWN`) | §33, §34, §60 | Missing |

**Mapped classes: 17. Implemented classes: 0.**

---

## Placeholder that must not count

```csharp
namespace TraderIntelligence.Tests.Unit;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
    }
}
```

`UnitTest1` is the Visual Studio template. It is not a §60 test. Delete it when the first real class lands. Do not rename it to “cover” a required item.

---

## Suggested method names (not implemented)

Minimum `[Fact]`/`[Theory]` names per class so implementers do not collapse 17 areas into one file:

| Class | First methods to add |
|---|---|
| `Mt5DealDeduplicationTests` | `Same_broker_login_deal_ticket_is_ignored_on_replay`; `Different_ticket_is_persisted` |
| `TradeReconstructionTests` | `Multiple_deals_one_position_yield_one_reconstructed_trade`; `Partial_close_is_not_a_new_trade` |
| `PartialCloseReconstructionTests` | `Partial_close_sets_was_partial_close_and_remaining_volume`; `Does_not_increment_first_three_trade_counter` |
| `ScaleInReconstructionTests` | `Scale_in_updates_max_volume_and_entry_vwap`; `Sets_was_scaled_in` |
| `FullCloseReconstructionTests` | `Full_close_marks_completed_and_exit_vwap`; `Net_pnl_includes_commission_and_swap` |
| `PositionReversalReconstructionTests` | `Reversal_closes_old_lifecycle_and_opens_new`; `Does_not_merge_opposite_direction_into_same_trade` |
| `XauCanonicalMappingTests` | `Maps_XAUUSD_variants_and_GOLD_to_canonical`; `Maps_cTrader_instrument_id`; `Unknown_symbol_does_not_silently_become_XAUUSD` |
| `SourceDestinationQuantityConversionTests` | `Known_lot_to_OrderQty_examples`; `Respects_min_qty_and_step`; `Never_passthrough_MT5_lots` |
| `DrawdownCalculatorTests` | `Peak_to_trough_on_closed_trade_series`; `Empty_series_is_zero` |
| `MfeMaeCalculatorTests` | `Computes_when_tick_window_exists`; `Refuses_to_fabricate_from_closed_deals_only`; `Sets_feature_quality` |
| `MartingaleDetectorTests` | `Detects_size_increase_after_loss`; `Does_not_flag_flat_sizing` |
| `AveragingDownDetectorTests` | `Detects_add_in_loss`; `Does_not_flag_add_in_profit` |
| `ScoreStateTransitionTests` | `Trade_3_complete_moves_to_EARLY_SCORE`; `High_score_defaults_to_SHADOW_not_LIVE`; `Risk_flag_moves_to_RISK_BLOCKED` |
| `RiskLimitEngineTests` | `Rejects_over_max_daily_loss`; `Rejects_stale_quote`; `Rejects_stale_signal`; `Blocks_martingale`; `Approve_reduce_reject_paths` |
| `CopyIntentIdempotencyTests` | `Same_source_event_id_returns_existing_intent`; `Distinct_events_create_distinct_intents` |
| `ClOrdIdGeneratorTests` | `Ids_are_unique_across_retries`; `Persisted_id_is_reused_not_regenerated` |
| `ExecutionReportStateTransitionTests` | `Partial_then_fill`; `Reject_from_new`; `Disconnect_after_send_is_UNKNOWN_not_resend`; `Unknown_requires_reconcile_before_new_NOS` |

---

## Gaps / blockers (honest)

1. **No SUTs.** Domain/Application/Mt5/Fix.CTrader/Infrastructure are empty `Class1` types. Tests cannot be written against real behavior yet.
2. **0/17 §60 unit areas implemented.**
3. **Unit project refs omit `TraderIntelligence.Mt5`.** Needed once `DealDeduplicator` lives there.
4. **§60 Integration (8 items) and Replay pipeline** are not in this map. Current `tests/Integration/UnitTest1.cs` is the same empty template.
5. **Do not count `mt5-sdk/tests/*.cpp`** toward §60 C# unit tests. Different tree, different process.

---

## Out of scope (not mapped here)

§60 Integration required: PostgreSQL migrations, MT5 backfill/restart, outbox processing, QuickFIX/n session configuration, FIX message parse/build, ExecutionReport handling, position reconciliation, unknown-execution recovery.

§60 Replay: historical MT5 events → replay → reconstruction → features → scores → shadow copy.

---

## Disposition

| Metric | Value |
|---|---|
| §60 unit areas required | 17 |
| Future test classes named | 17 |
| Classes present in `tests/Unit` | 0 (only `UnitTest1`) |
| Product source changed | No |

Implement the 17 classes above when the corresponding SUTs exist. Until then this audit is the authoritative name map.
