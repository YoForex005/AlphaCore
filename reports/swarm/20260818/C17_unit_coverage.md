# C17 — `tests/Unit` vs Architecture §60 required unit tests

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C17_unit_coverage.md` |
| Agent | C17 (unit coverage) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:24:09+05:30 (`dotnet test`) |
| Product source edited | **No** |
| Test source edited | **No** |
| Scope | `D:\Prop\tests\Unit\*.cs` (and subfolders) vs Architecture **§60 Unit tests** only |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §60 lines 2228–2256 |
| Class backlog | A09 (17 names), A27 §4, A89 (92-class expansion). This file measures **what exists and what it actually proves**. |
| Stale predecessors | A09 described empty `UnitTest1` + `Class1` SUTs. B08 measured 29/28/1 before the two new sizing files landed. **Use this file for current Unit vs §60.** |

Classification vs a §60 bullet: `COVERED` / `PARTIAL` / `MISSING` / `FAIL`.

- **COVERED** — dedicated passing (non-skipped) facts lock the required contract; no known critical hole for that bullet.
- **PARTIAL** — at least one executed fact touches the area; the contract is incomplete.
- **MISSING** — zero executed facts in `tests/Unit`.
- **FAIL** — an executed fact that is supposed to lock the area is red.

A green `dotnet test` is not coverage. A `[Fact(Skip=…)]` is not coverage.

---

## 0. Verdict

**FAIL / 0 of 17 §60 unit areas COVERED.**

Product engines exist (`TradeReconstructor`, `BaselineScorer`, `RiskEngine`, `QuantityNormalizer`, `ClOrdIdFactory`, `SymbolNormalizer`, `VolumeConverter`, `ExecutionOrderStateMachine`). A thin xUnit surface sits on top. It does **not** satisfy Architecture §60. It does **not** satisfy A27 / A89 class inventories.

| Gate | Required | Measured now |
|---|---:|---|
| §60 unit areas | 17 | **0 COVERED / 13 PARTIAL / 4 MISSING / 0 FAIL** |
| A89 named unit classes (six domains) | 92 | **3 name-matches** (`TradeReconstructionTests`, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests`) |
| A09/A27 dedicated classes for the 17 bullets | 17 | **2** (`SourceDestinationQuantityConversionTests`, plus the collapsed `TradeReconstructionTests`) |

Measured `dotnet test` (this pass):

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal
```

| Project | Total | Passed | Failed | Skipped | Exit |
|---|---:|---:|---:|---:|---|
| `TraderIntelligence.Tests.Unit` | **83** | **60** | **1** | **22** | **1** |

The single red fact is **not** a §60-area FAIL. It is a **test arithmetic defect** in `QuantityNormalizerStepMinMaxTests.Allocation_scales_before_step` (expects `0.10 × 0.10 = 0.10`; SUT correctly returns `0.01`). See §5.

B08’s averaging-down **FAIL** is **stale**. `TradeReconstructor.OpenTrade.ScaleIn` now uses long-add-below-VWAP polarity (`deal.Price < EntryVwap`). `Scale_in_and_partial_close` **passes**. The contract is still PARTIAL (no add-in-profit negative, no short polarity).

Do **not** treat 60/83 green as a Phase-3 or §69 exit signal.

---

## 1. Method

| Source | Path |
|---|---|
| Architecture §60 Unit | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2230–2256 |
| Prior inventories | `A09_unit_tests_audit.md`, `A27_test_inventory.md` §4, `A89_unit_class_list.md` §7 |
| Prior measured gap | `B08_tests_gap.md` (29 tests; 1 red averaging-down) |
| Reconstruction review | `C01_recon_tests_review.md` (5/5 green ≠ coverage) |
| Product SUTs | `D:\Prop\src\Domain\**\*.cs` (read, not edited) |
| Test tree | `Get-ChildItem D:\Prop\tests\Unit -Recurse -Filter *.cs` excluding `bin`/`obj` |

Commands:

```text
Get-ChildItem D:\Prop\tests\Unit -Recurse -Filter *.cs |
  Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal
```

§60 Integration (8) and Replay are **out of this map**. They belong in `tests/Integration` and a future `tests/Replay`. Mentioned only when a Unit file is being used as a stand-in.

---

## 2. §60 required unit list (quoted)

From the architecture `Required:` block (lines 2234–2256):

```text
MT5 deal deduplication
trade reconstruction
partial close
scale-in
full close
position reversal

XAU canonical mapping
source/destination quantity conversion

drawdown
MFE/MAE where data exists
martingale detection
averaging-down detection

score-state transitions

risk limits

copy-intent idempotency
ClOrdID generation
ExecutionReport state transitions
```

That is **17 areas**, not 60 tests. A09 named 17 future classes. A89 absorbed those names into 92 classes. C17 scores the **17 architecture bullets**. A89 is a completeness backlog, not a replacement of §60.

---

## 3. Current `tests/Unit` inventory

Project: `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`  
SHA-256: `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50`  
TFM `net8.0`. Packages: xUnit 2.5.3, FluentAssertions 6.12.0, Moq 4.20.70, coverlet 6.0.0.  
**Project refs:** Domain, Application, Fix.CTrader.  
**Not referenced:** Infrastructure, Mt5. That blocks an honest unit `Mt5DealDeduplicationTests` against `EfTradingStore` / `FakeMt5BrokerConnector` unless a fake store lives in the unit project.

No `[Trait]`. No `IClassFixture`. Moq is unused. Coverlet is unused. No recorded fixtures.

### 3.1 Source files (excluding `bin`/`obj`)

| Bytes | SHA-256 | Path | Role |
|---:|---|---|---|
| 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | `BaselineScorerTests.cs` | 3 facts (state + martingale flag) |
| 2144 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `ExecutionAndSizingTests.cs` | 6 facts (FSM + qty + ClOrdID + expiry) |
| 2909 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `RiskEngineTests.cs` | 5 facts |
| 896 | `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `SymbolNormalizerTests.cs` | 1 theory (5 cases) + 1 fact |
| 3939 | `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` | `TradeReconstructionTests.cs` | 5 facts (collapsed recon cluster) |
| 224 | `6B1A127F1810FF0A0E1C07F0913A415CBE61D31FE56DF3BD46378C97EB77E6A5` | `UnitTest1.cs` (`SmokeTests`) | assembly-load smoke |
| 791 | `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5` | `VolumeConverterTests.cs` | 3 facts (source scale, not dest conversion) |
| 7344 | `A1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | `Normalization/SourceDestinationQuantityConversionTests.cs` | 4 pass + 21 skip (A43 converter missing) |
| 5174 | `C23CD36C0D4562000FF880AAA07EF157B4A21771AF301AA05BDEBDE515982886` | `Sizing/QuantityNormalizerStepMinMaxTests.cs` | last-stage min/step; **1 FAIL**, 1 skip |

**9 source files. 8 real suites + 1 smoke.**

### 3.2 Executed case census (83)

| Class | Pass | Fail | Skip | Notes |
|---|---:|---:|---:|---|
| `SmokeTests` | 1 | 0 | 0 | Not a §60 area |
| `TradeReconstructionTests` | 5 | 0 | 0 | Four §60 recon bullets collapsed into 2 facts |
| `BaselineScorerTests` | 3 | 0 | 0 | State + martingale; no drawdown / MFE / averaging feature |
| `RiskEngineTests` | 5 | 0 | 0 | 5 of ~19 `Evaluate` reason codes |
| `ExecutionAndSizingTests` | 6 | 0 | 0 | Mixes ER FSM, qty, ClOrdID, expiry |
| `SymbolNormalizerTests` | 6 | 0 | 0 | 5 aliases + venue-id register |
| `VolumeConverterTests` | 3 | 0 | 0 | Manager 10_000 / Extended / not-hundredths |
| `SourceDestinationQuantityConversionTests` | 4 | 0 | 21 | Passing facts **document passthrough**; spec cases skipped |
| `QuantityNormalizerStepMinMaxTests` | 27 | 1 | 1 | Last-stage floor; 1 test-bug red; 1 A43 E23 skip |
| **Total** | **60** | **1** | **22** | |

---

## 4. §60 Unit map (17 required areas)

Status is vs **behavior locked**, not vs “a method name exists.”

| # | §60 required | Existing fact(s) | SUT on disk | Status | Gap |
|---:|---|---|---|---|---|
| 1 | MT5 deal deduplication | **none in Unit** | `EfTradingStore.UpsertDealAsync` (Infrastructure); unique `(BrokerId, DealTicket)` | **MISSING** | Integration `Deal_upsert_is_idempotent` is InMemory `AnyAsync`, not a Unit test and not a PostgreSQL UK. No fake-store unit. Dual-broker same ticket untested. `tests/Unit` does not reference Infrastructure/Mt5. |
| 2 | trade reconstruction | `Reconstructs_simple_round_trip`, `Ignores_balance_deals`, `First_three_completed_xau_unlocks_early_score` | `TradeReconstructor` | **PARTIAL** | One IN/OUT + Balance skip + N=3 latch. No multi-position, no broker/login filter, no sort determinism, no commission/swap/fees, no open leftover, no `Order ≠ Deal ≠ Position` identity lock. C01: 0/21 A21 fixtures. |
| 3 | partial close | bundled in `Scale_in_and_partial_close` | `WasPartialClose` | **PARTIAL** | Flag + completed. No remaining-volume after first OUT, no “partial does not increment first-3”, no dedicated class. |
| 4 | scale-in | same fact | `WasScaledIn`, `MaxVolumeLots` | **PARTIAL** | No entry-VWAP `(2300+2290)/2`. No `WasScaledIn` on IN-only (without the two OUTs). Fused with partial + averaging. |
| 5 | full close | `Reconstructs_simple_round_trip` | `Completed`, `ExitVwap`, `NetRealizedPnl` | **PARTIAL** | Fees hardcoded 0. Commission/swap never asserted. No dedicated `FullCloseReconstructionTests`. |
| 6 | position reversal | `Reverse_inout_closes_then_opens_opposite` | `DealEntry.InOut` | **PARTIAL** | Count/direction/remaining only. No money split, no `OutBy`, no exact-flat leftover, no ticket-reuse-across-lifecycle, no opposite `ENTRY_IN` fallback. |
| 7 | XAU canonical mapping | 5 aliases + venue-id register | `SymbolNormalizer` | **PARTIAL** | Locks `XAUUSD` / `XAUUSD.` / `XAUUSDm` / `XAUUSD.a` / `GOLD` and “numeric 55 needs register”. **No** `EURUSD`/empty must-not-become-XAU. Prefix heuristic `compact.StartsWith("XAUUSD")` untested (`XAUUSD.PRO` is compiled-in; `XAUUSDFOO` would silently map). No persist-key vs compact-dot (A44 G8). |
| 8 | source/destination quantity conversion | 4 passing “passthrough” facts + 21 skipped A43 facts + `VolumeConverter` 3 + last-stage min/step | `QuantityNormalizer` (lots × allocation); **no** `IQuantityConverter` | **PARTIAL** | Spec: never passthrough MT5 lots → dest `OrderQty` (`0.10 lot × 100 oz → 10`). Passing facts **prove the opposite** (`Normalize(0.10, 1) == 0.10`). `Never_passthrough_MT5_lots` is skipped. VolumeConverter is source scale 10_000, not dest conversion. |
| 9 | drawdown | **none** | `BaselineScorer.ComputeFeatures.MaxDrawdown` (peak-to-trough on closed-trade equity) | **MISSING** | Equity path is implemented. Never asserted. Empty-series = 0 untested. |
| 10 | MFE/MAE where data exists | **none** | `FeatureSnapshot.MaeMfeQuality` always `Unavailable`; `AverageMfe`/`AverageMae` null | **MISSING** | No `MfeMaeCalculator`. No “refuse to fabricate from entry/exit VWAP”. Policy A45 is implicit by omission — that is **not** a test. |
| 11 | martingale detection | `Martingale_after_losses_is_risk_blocked` | `BaselineScorer` `> 1.25×` after loss | **PARTIAL** | Positive only (0.10 → 0.20 → 0.40). No flat-sizing negative. No 1.80 spec threshold (A22 G2: 1.26× after loss is **not** martingale). No size-up-after-win (escalation, not martingale). |
| 12 | averaging-down detection | `WasAveragedDown.Should().BeTrue()` inside scale+partial | `OpenTrade.ScaleIn` (polarity **fixed**: long add **below** VWAP) | **PARTIAL** | Long add-in-loss is green. No add-in-profit negative. No short add-above-VWAP. `FeatureSnapshot.AveragingDown` never asserted. B08 FAIL is stale. |
| 13 | score-state transitions | 3 facts | `TraderStateMachine.FromBaseline` | **PARTIAL** | Locks `INSUFFICIENT_DATA` (N=2), `SHADOW` (3 winners), `RISK_BLOCKED` (martingale+loss), `CanPromoteToLive == false`. Missing `EARLY_SCORE`, `WATCH`, `PAUSED`, `DISQUALIFIED`, illegal `INSUFFICIENT_DATA → LIVE`, rescoring 3 vs 4 vs 5, “high quality cannot skip RISK_BLOCKED”. |
| 14 | risk limits | 5 facts | `RiskEngine.Evaluate` | **PARTIAL** | Covered: `QUOTE_STALE`, flag-off `AllowFixSend=false`, `STOP_NEW_EXECUTION` open vs close, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`. **~14 reason codes have zero facts** (see §6). `MAX_XAU_NET` is `ReduceSize` with `ApprovedQuantity = 0` (G9) — untested. |
| 15 | copy-intent idempotency | **none** | `CopyIntent.IdempotencyKey` field + unique index in EF; no factory / guard | **MISSING** | `Copy_intent_expires` is **expiry**, not idempotency. Same source event → one intent is untested. |
| 16 | ClOrdID generation | `ClOrdId_is_deterministic_and_unique_per_sequence` | `ClOrdIdFactory.Next` | **PARTIAL** | Prefix `TI20260818120000` + seq uniqueness. Same `(intent, now, sequence)` stability not asserted (only seq 0 vs 1 differ). Empty intent / `sequence < 0` throws untested. Persist-before-send reuse of the **same** id after send: **none**. |
| 17 | ExecutionReport state transitions | 3 facts | `ExecutionOrderStateMachine` | **PARTIAL** | Sent-unknown + no retry + reconcile required; disconnect → `ExecutionStateUnknown`; FILL terminal. Missing: PARTIAL (`1`), REJECT (`8`), CANCEL (`4`), PENDING_NEW (`A`), unknown OrdStatus → unknown, Rejected/Cancelled sticky, `MayRetry` true only from `NotSent`/`Rejected` (positive `NotSent` untested). No `fix_execution_reports` persistence. |

**§60 unit score: 0 COVERED, 13 PARTIAL, 4 MISSING, 0 FAIL.**

Missing four: **deal deduplication, drawdown, MFE/MAE, copy-intent idempotency.**

---

## 5. The one red fact (not a §60 FAIL)

```83:88:D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs
    public void Allocation_scales_before_step()
    {
        _n.Normalize(1.00m, 0.25m, DefaultSpec).Should().Be(0.25m);
        _n.Normalize(0.10m, 0.10m, DefaultSpec).Should().Be(0.10m);
        _n.Normalize(1.00m, 0.001m, DefaultSpec).Should().Be(0m);
    }
```

SUT:

```20:20:D:\Prop\src\Domain\Execution\QuantityNormalizer.cs
        var raw = sourceLots * allocationFactor;
```

`0.10 × 0.10 = 0.01`, which is also `DefaultSpec.MinQuantity`, so Normalize returns `0.01`. The test expected `0.10` (allocation ignored). First line (`1.00 × 0.25 = 0.25`) is consistent with multiply; second line is not.

**Classification:** test defect, not a product bug. It does **not** turn §60 item 8 into FAIL. Item 8 stays PARTIAL because the real conversion SUT is missing.

Skipped sibling (honest):

```148:153:D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs
    [Fact(Skip = "A43 E23: after dest max, q must be FloorToStep(max, step). Today Normalize returns raw MaxQuantity.")]
    public void Above_max_re_floors_to_step()
```

That skip matches the passing `Unaligned_max_is_returned_raw_not_re_floored` (documents the deviation). Do not count the skip as coverage of E23.

---

## 6. `RiskEngine` branches vs facts

Covered by `RiskEngineTests`:

| Reason / path | Fact |
|---|---|
| `QUOTE_STALE` | `Stale_quote_rejects_open` |
| `AllowFixSend=false` when `RealExecutionEnabled=false` | `Real_flag_false_never_allows_fix_send` (outcome still `Approve`) |
| `STOP_NEW_EXECUTION` blocks increasing | `Stop_new_execution_blocks_opens_not_closes` |
| close still `Approve` under stop-new | same |
| `VENUE_NOT_RECONCILED` | `Unreconciled_venue_blocks_new_exposure` |
| `SIGNAL_STALE` | `Stale_signal_rejected` |

Zero facts (all live code in `RiskEngine.Evaluate`):

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
| `MAX_XAU_NET` | `ReduceSize` **via `Reject()` → `ApprovedQuantity = 0`** (A89 G9; likely product bug; untested) |
| `MAX_MARGIN_USAGE` | `Reject` |
| `MARTINGALE_BLOCK` | `PauseTrader` |
| `ABNORMAL_SIZING_BLOCK` | `Reject` |
| `RISK_REDUCTION` reason on reduce/close | `Approve` + `AllowFixSend` only if flag+reconcile+venue+kill none |
| `RealExecutionEnabled=true` happy path | `AllowFixSend=true` |

Kill-switch close path asserts `AllowFixSend=false` because the flag is off — it does **not** prove reduce/close would send when the flag is on.

---

## 7. A89 / A27 class names vs files on disk

A89 §7 maps each §60 bullet to class numbers. Present **name matches**:

| A89 # | Class | On disk |
|---:|---|---|
| 1 | `TradeReconstructionTests` | `tests/Unit/TradeReconstructionTests.cs` (flat namespace; not `Reconstruction/`) |
| 45 | `SourceDestinationQuantityConversionTests` | `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` |
| 47 | `QuantityNormalizerStepMinMaxTests` | `tests/Unit/Sizing/QuantityNormalizerStepMinMaxTests.cs` |

**Absent** (selected P0 names that *are* the §60 bullets):

`Mt5DealDeduplicationTests`, `PartialCloseReconstructionTests`, `ScaleInReconstructionTests`, `FullCloseReconstructionTests`, `PositionReversalReconstructionTests`, `XauCanonicalMappingTests`, `DrawdownCalculatorTests`, `MfeMaeFeatureQualityTests` / `MfeMaeCalculatorTests`, `MartingaleDetectorTests`, `AveragingDownDetectorTests`, `ScoreStateTransitionTests` / `TraderStateMachineFromBaselineTests`, `RiskEngineHardLimitTests`, `CopyIntentIdempotencyKeyTests`, `ClOrdIdGenerationTests`, `ExecutionReportStateTransitionTests`.

Informal stand-ins that must **not** be renamed to “cover” a missing class:

| Informal class | Absorbs (incompletely) |
|---|---|
| `BaselineScorerTests` | score-state + martingale |
| `RiskEngineTests` | risk limits (5/19) |
| `ExecutionAndSizingTests` | ClOrdID + ER FSM + last-stage qty + **expiry (not idempotency)** |
| `SymbolNormalizerTests` | XAU mapping |
| `VolumeConverterTests` | source ticks scale (feeds item 8, is not item 8) |
| `SmokeTests` (`UnitTest1.cs`) | nothing |

---

## 8. Placeholders and false signals that must not count

```1:10:D:\Prop\tests\Unit\UnitTest1.cs
namespace TraderIntelligence.Tests.Unit;

public class SmokeTests
{
    [Fact]
    public void Domain_assembly_loads()
    {
        Assert.NotNull(typeof(TraderIntelligence.Domain.Volume.VolumeConverter).Assembly);
    }
}
```

Smoke is harmless. It is not a §60 area. File name is still the VS template.

**Skipped A43 facts are a backlog, not coverage.** `SourceDestinationQuantityConversionTests` has 21 skips whose bodies either call a local `ConvertTicks` helper (not product code) or `true.Should().BeFalse("…")`. Un-skipping them today would go red because `IQuantityConverter` does not exist. That is the correct fail-closed posture — as long as nobody reports 21 extra “tests.”

**Passing passthrough facts are documentation of G7, not a pass of item 8.**

```19:24:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact]
    public void QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one()
    {
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().Be(0.10m);
        _n.Normalize(0.10m, 1m, DestBaseUnits1Oz).Should().NotBe(10.00m);
    }
```

That locks current (wrong-for-spec) behavior. The skipped `Never_passthrough_MT5_lots` is the §60/A43 contract.

---

## 9. What changed since B08 (honest delta)

| Item | B08 (earlier 2026-08-18) | C17 now |
|---|---|---|
| Executed | 29 | **83** |
| Passed / failed / skipped | 28 / 1 / 0 | **60 / 1 / 22** |
| Red fact | `Scale_in_and_partial_close` (`WasAveragedDown` expected true) | `Allocation_scales_before_step` (test math) |
| Averaging polarity in SUT | inverted (`>` for long) | **fixed** (`<` for long) — `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` lines 238–240 |
| New files | — | `Normalization/SourceDestinationQuantityConversionTests.cs`, `Sizing/QuantityNormalizerStepMinMaxTests.cs` |
| §60 COVERED | 0 | **0** |
| §60 PARTIAL / MISSING / FAIL | 13 / 3 / 1 | **13 / 4 / 0** |

MISSING went 3 → 4 because B08 counted averaging-down as FAIL (an executed red fact). Averaging-down is now a **passing incomplete** fact → PARTIAL. Drawdown / MFE/MAE / copy-intent stay MISSING. Deal-dedup stays MISSING in Unit (B08 called it PARTIAL by counting the Integration upsert). **C17 does not credit Integration toward §60 Unit.**

---

## 10. Out of scope (not scored as Unit coverage)

| Item | Why excluded |
|---|---|
| §60 Integration (8) | PostgreSQL migrations, MT5 backfill/restart, outbox, QuickFIX session, FIX parse/build, ER handling, position reconciliation, unknown-execution recovery — `tests/Integration`, not this file |
| §60 Replay | `tests/Replay` does not exist |
| §61 FIX harness (7) | `FixSimulationHarness` exists in product; **zero** Unit facts call it |
| `tests/Integration/SeedingAndStoreTests.cs` | InMemory seed + upsert; not Unit |
| `mt5-sdk/tests/*.cpp` | Different process (A18) |
| `apps/web` tests | none found; not §60 |

---

## 11. Recommended next Unit facts (do not add empty classes)

Align with A89 implementation order. Prefer facts against **existing** SUTs. Do not rename `SmokeTests` to cover a bullet.

| Order | Add | Why |
|---:|---|---|
| 1 | Split reconstruction: dedicated partial / scale-in / full-close / reversal / first-3 negatives (open, partial, non-XAU, N=2). Assert VWAP, remaining, commission+swap. Add-in-profit + short averaging polarity. | Unblocks §69.5–6; C01 already listed the holes |
| 2 | `DrawdownCalculatorTests` on `ComputeFeatures.MaxDrawdown` (peak-to-trough + empty=0) | §60 item 9; SUT already computes it |
| 3 | `MfeMaeFeatureQualityTests`: deals-only → `Unavailable`, averages null; never fill from VWAP | §60 item 10 / A45; cheap, fail-closed |
| 4 | Martingale negatives (flat after loss; 1.26× not martingale if locking A22 1.80; size-up after win) | §60 item 11 |
| 5 | Remaining `RiskEngine` reason codes (§6), especially `MAX_XAU_NET` qty and `EMERGENCY_FLATTEN` | §60 item 14 |
| 6 | `CopyIntentIdempotencyKeyTests` (pure key function) + `ClOrdIdFactory` same-args stability / throws | §60 items 15–16 |
| 7 | ER matrix: PARTIAL / REJECT / CANCEL / unknown / sticky terminal / `MayRetry` positives | §60 item 17 |
| 8 | `EURUSD` / empty must not map to XAU; document prefix heuristic | §60 item 7 |
| 9 | Fake-store `Mt5DealDeduplicationTests` (do not require EF) | §60 item 1 |
| 10 | Implement `IQuantityConverter` then un-skip A43 E01–E09 | §60 item 8 — do **not** un-skip first |

Fix `Allocation_scales_before_step` expected value to `0.01m` when someone next touches the test file. C17 did not edit it.

---

## 12. Disposition

| Metric | Value |
|---|---|
| §60 unit areas required | **17** |
| COVERED | **0** |
| PARTIAL | **13** (items 2–8, 11–14, 16–17) |
| MISSING | **4** (deal dedup, drawdown, MFE/MAE, copy-intent idempotency) |
| FAIL (area red) | **0** |
| Files under `tests/Unit` (source) | **9** |
| `[Fact]`/`[Theory]` executed | **83** |
| Passed / failed / skipped | **60 / 1 / 22** |
| A89 classes present by name | **3 / 92** |
| Red fact | `QuantityNormalizerStepMinMaxTests.Allocation_scales_before_step` (test expects 0.10, SUT 0.01) |
| Product source changed by C17 | **No** |

**Do not claim “unit tests cover Architecture §60.”** Claim: 13 of 17 areas have a smoke; 4 have nothing; 0 are locked.
