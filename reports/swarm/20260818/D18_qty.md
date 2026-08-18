# D18 — `QuantityNormalizer` re-measure (last-stage floor vs §38 converter)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D18_qty.md` |
| Agent | D18 (quantity / last-stage step-min-max) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:34:20+05:30 |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture §38, executive change #10, `A43_position_sizing.md`, `A23` §7, `A38` (1 lot = 10 000), `A89` #45/#47, `A100` G10 |
| Prior reviews | `B17_qty_review.md` (same SHA), `C17_unit_coverage.md` (stale red-fact claim — see §6.3) |
| Product source edited | **No** |
| Test source edited | **No** |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**`EXISTS_NEEDS_REFACTOR` as a last-stage floor. `MISSING` as the §38 / A43 converter. G7 / G10 remain FAIL.**

`QuantityNormalizer` is still 31 lines. It multiplies `sourceLots * allocationFactor`, floors to `StepSize` via `decimal.Truncate`, truncates to `Precision` with `MidpointRounding.ToZero`, then maps below-min → `0` and above-max → **raw** `MaxQuantity`. All arithmetic is `decimal`. It never sees MT5 ticks, contract size, dest convention, confidence, margin, or a mapped destination position.

Measured passthrough (A43 E01 / G7), re-run this wave:

```text
Normalize(0.10, allocation=1, Dest_BaseUnits min=0.01 step=0.01 max=5000)
    = 0.10
```

A43 requires **10.00** OrderQty (0.10 lots × 100 oz/lot ÷ 1 oz/unit). `0.10` would be **100× too small** on a Spotware-style BaseUnits XAU book.

Product callers: **zero**. `grep` of `QuantityNormalizer` / `InstrumentQuantitySpec` / `.Normalize(` over `D:\Prop\src\**\*.cs` (excluding `bin`/`obj`) hits only the definition file. `new QuantityNormalizer` exists only under `tests/`. `RiskEngine.Evaluate` consumes a pre-baked `RequestedQuantity` and never quantizes. `ShadowCopyEngine` copies the quantity it is given. The class cannot satisfy §68 “position sizing conversion is verified.”

SUT bytes/SHA are **identical** to B17. This report is a D-wave re-measure, not a rewrite of the type.

---

## 1. Measured source

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Bytes | 1041 |
| Lines | 31 |
| SHA-256 | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` |
| LastWriteUtc | 2026-08-18T07:38:10.3123032Z |
| Unchanged vs B17 | **yes** (same hash, same 31 lines) |

Types in the same file:

| Type | Kind | Role |
|---|---|---|
| `InstrumentQuantitySpec` | `sealed record` (`MinQuantity`, `MaxQuantity`, `StepSize`, `Precision`) | dest limits only; no convention, no ounces, no `spec_status` |
| `QuantityNormalizer` | `sealed class` | one method `Normalize(sourceLots, allocationFactor, dest)` |

Full source (product not edited):

```1:31:D:\Prop\src\Domain\Execution\QuantityNormalizer.cs
namespace TraderIntelligence.Domain.Execution;

public sealed record InstrumentQuantitySpec(
    decimal MinQuantity,
    decimal MaxQuantity,
    decimal StepSize,
    int Precision);

public sealed class QuantityNormalizer
{
    public decimal Normalize(decimal sourceLots, decimal allocationFactor, InstrumentQuantitySpec dest)
    {
        if (sourceLots <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceLots));
        if (allocationFactor <= 0)
            throw new ArgumentOutOfRangeException(nameof(allocationFactor));
        if (dest.StepSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(dest.StepSize));

        var raw = sourceLots * allocationFactor;
        var steps = decimal.Truncate(raw / dest.StepSize);
        var qty = steps * dest.StepSize;
        qty = decimal.Round(qty, dest.Precision, MidpointRounding.ToZero);

        if (qty < dest.MinQuantity)
            return 0m;
        if (qty > dest.MaxQuantity)
            return dest.MaxQuantity;
        return qty;
    }
}
```

### 1.1 Algorithm (what the bytes actually do)

```text
if sourceLots <= 0           → ArgumentOutOfRangeException(sourceLots)
if allocationFactor <= 0     → ArgumentOutOfRangeException(allocationFactor)
if dest.StepSize <= 0        → ArgumentOutOfRangeException(StepSize)

raw   = sourceLots * allocationFactor
steps = Truncate(raw / dest.StepSize)          // toward zero; = Floor for qty > 0
qty   = steps * dest.StepSize
qty   = Round(qty, dest.Precision, ToZero)

if qty < dest.MinQuantity    → 0
if qty > dest.MaxQuantity    → dest.MaxQuantity   // NOT FloorToStep(max)
else                         → qty
```

Guards that do **not** exist: `allocationFactor > 1`, `MinQuantity` multiple of step, `MaxQuantity >= MinQuantity`, `StepSize` multiple of 0.01, `Precision >= 0`, finite contract size, dest convention, `spec_status`.

`Precision < 0` is not checked; `decimal.Round(..., -1, ToZero)` throws `ArgumentOutOfRangeException` from the BCL (locked by `Negative_precision_throws`).

`sourceLots` / `allocationFactor` are required `> 0`, so `raw` is always positive. `Truncate` therefore equals `Floor` on the only legal path. Negative qty cannot appear.

### 1.2 Adjacent product types (not composed)

| Type | Path | SHA-256 | Relation |
|---|---|---|---|
| `VolumeConverter` | `Domain\Volume\VolumeConverter.cs` | `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2` | ticks → lots at scale **10 000**. Normalizer never calls it. |
| `RiskEngine` | `Domain\Risk\RiskEngine.cs` | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` | Takes `RequestedQuantity` as an input. No `Normalize` call. Caps **reject**, they do not resize. |
| `ShadowCopyEngine` | `Domain\Shadow\ShadowCopyEngine.cs` | `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` | Copies `quantity` through. No dest spec. |
| `ExecutionIntent` | `Domain\Entities\ExecutionIntent.cs` | — | `RequestedQuantity` is a bare `decimal`. No unit, no remainder. |
| `CopyIntent` | `Domain\Entities\CopyIntent.cs` | — | Same: `RequestedQuantity` with no conversion audit. |
| `CanonicalInstrument` | `Domain\Entities\CanonicalInstrument.cs` | — | `Id` / `Code` / `Description` only. No contract size. |
| `SourceSymbolMapping` | `Domain\Entities\SourceSymbolMapping.cs` | — | broker + source symbol + canonical id. No contract size. |
| `IQuantityConverter` / `QuantityConverter` | — | — | **MISSING** (no type in `src/`) |
| `DestinationSymbol` / `destination_symbols` | — | — | **MISSING** |
| `QuantityConvention` enum | — | — | **MISSING** |

`VolumeConverter.ManagerVolumeScale = 10_000m` is the official Manager scale (B14). That fact does not flow into `QuantityNormalizer`. A caller that already converted ticks→lots and then called `Normalize(lots, 1, dest)` would still emit lots as OrderQty.

---

## 2. Spec vs code (do not rubber-stamp)

| ID | Spec (A43 / §38 / A23 §7) | Current code | Stance |
|---|---|---|---|
| G7 / E01 | 0.10 lots × 100 oz → BaseUnits `OrderQty=10.00` | `0.10 * 1 = 0.10` | **FAIL** passthrough |
| E06 | same 0.10 lots × 10 oz (mini) → `1.00` | still `0.10` | **FAIL** ignores contract |
| E08 | Lots convention is the **only** case `0.10→0.10` | always passthrough | **FAIL** |
| E11 / §4.5 | floor, never ceil | `Truncate` then `ToZero` | **PASS** for positive qty |
| E14 | below min after floor → do not send | returns `0m` | **PARTIAL** (0 is not `SIZE_BELOW_MIN`) |
| E23 | `max=5.09` step `0.10` → **5.00** | returns **5.09** | **FAIL** raw max |
| E21 | `confidence_scale > 1` reject | no confidence input; `allocation>1` accepted | **FAIL** |
| E16–E20 | allocation × confidence ≤ 1 before dest step | allocation only; no confidence | **PARTIAL** |
| E22–E26 | dest max and risk caps reduce then re-quantize | raw max only; no book/margin | **FAIL** / **MISSING** |
| E27–E29 | margin room reduces qty | no quote / leverage | **MISSING** |
| E30–E31 | INCREASE = incremental source ticks | no ticks, no exposure class | **MISSING** |
| E32 / E38 | CLOSE = mapped dest qty | no close path | **MISSING** |
| E33–E37 | REDUCE = fraction of dest; dust → close | no reduce path | **MISSING** |
| E39 | Unverified dest spec rejects | no `spec_status` | **MISSING** |
| E40–E41 | invalid contract size rejects | no contract size | **MISSING** |
| E42–E43 | missing dest spec / REDUCE link rejects | dest is a required record, not a mapping | **MISSING** |
| E44–E46 | invalid min/step rejects spec | step≤0 throws; 0.001 step and non-multiple min accepted | **PARTIAL** |
| A38 | ticks / 10 000 | no tick input | **MISSING** (scale lives on `VolumeConverter` only) |
| A23 step 10 | Risk calls converter, then persist `requested_quantity` | Risk never calls this type | **MISSING** |
| A43 §6 | Risk + Shadow + FIX share one converter; FIX does not rescale | **zero callers** | **MISSING** |
| A43 rule 2 | `decimal` only | `decimal` | **PASS** |
| A43 rule 7 | Floor, never ceil / never banker’s round-up | `Truncate` + `ToZero` | **PASS** (positive path) |
| A43 rule 13 | One converter in Domain/Application, not inside FIX | this type is Domain; FIX has no copy | **PASS** as absence |

`InstrumentQuantitySpec` is a dest-only tuple. It cannot carry `QuantityConvention`, `unit_size_oz` / `lot_size_oz`, `spec_status`, or source `contract_size`. Extending this record until it is the A43 dest spec is acceptable; treating `Normalize(sourceLots, 1, spec)` as FIX tag 38 is not.

---

## 3. Defects on the class that *does* exist

These are independent of the missing ounces pipeline. A last-stage quantizer must still get them right (A43 §4.5).

### F1 — Cap does not re-floor to step (E23)

```text
Normalize(10, 1, min=0.01 max=5.09 step=0.10 prec=2)  →  5.09
```

5.09 is not a multiple of 0.10. Sending tag 38 = 5.09 is `SIZE_NOT_MULTIPLE_OF_STEP`. Spec: `q = FloorToStep(max, step)` → **5.00**, path `ReduceSize`.

Characterization test `Unaligned_max_is_returned_raw_not_re_floored` locks today’s 5.09. Spec test `Above_max_re_floors_to_step` is **Skipped**.

### F2 — Precision after step can break alignment

```text
Normalize(0.08, 1, min=0.01 max=5 step=0.025 prec=2)
    Truncate(0.08/0.025)=3 → 0.075 → Round(ToZero, 2) → 0.07
```

0.07 is not a multiple of 0.025. A43 also forbids `step=0.025` on FIX (step must be a multiple of 0.01). Spec validation is missing on the record, so this input is legal today. Locked by `Coarser_precision_than_step_can_break_step_alignment`.

### F3 — `allocationFactor > 1` is accepted

A43 `AllocationFactor` ∈ `(0, 1]`. `Normalize(1, 1.5, DefaultSpec)` returns `1.50`. Confidence/allocation must not enlarge size. Locked by `Allocation_greater_than_one_is_currently_accepted`.

### F4 — Below min is `0m`, not a reject reason

Callers cannot tell “do not send” from a tradable zero. A43 wants `REJECT` / `SIZE_BELOW_MIN` and **zero FIX**. Returning `0` is only safe if every caller treats 0 as reject. There are no callers.

### F5 — Spec record is unvalidated

`MinQuantity=0.03` + `StepSize=0.02` (E46), `MaxQuantity < MinQuantity`, `StepSize=0.001` (E45), `Precision<0` all construct. Only `StepSize<=0` is rejected, and only at `Normalize` time.

If `Max < Min` (e.g. min=5, max=1) a legal 1.00 qty is classified as below min and returns `0`. The inverted-range case is untested.

### F6 — Dead path

No product call site. `RiskEngine` compares `RequestedQuantity` against `MaxPositionQuantity` / XAU caps and **rejects** on overshoot (`MAX_POSITION_QUANTITY`, `MAX_XAU_GROSS`) instead of `ReduceSize` + re-quantize. `MAX_XAU_NET` returns `ReduceSize` but still sets `ApprovedQuantity = 0`. Shipping this class does not move G10.

### F7 — Units are unnamed

Parameter `sourceLots` is the unit. The return value is implied dest OrderQty. Nothing in the type system stops a caller from passing dest ounces, dest lots, or raw ticks. A43 wants `Mt5VolumeTicks` → `SourceLots` → `CanonicalOunces` → `DestinationQty`.

---

## 4. What this class gets right

- `decimal` end-to-end (no `double` lots).
- Floor via `Truncate` on a positive raw qty — 0.333 / 0.335 / 0.339 / 1.999 all step to the lower increment; 12.30 with step 1.00 → 12.00 (E11); 12.35 with step 0.10 → 12.30 (E12); 10.01 with step 0.10 → 10.00 (E13).
- `MidpointRounding.ToZero` after step — no banker’s round-up.
- `sourceLots<=0`, `allocationFactor<=0`, `StepSize<=0` throw with a param name.
- Exact min (`0.01`) kept; just-below-min after floor → `0`.
- Aligned max (`5.00`) kept; aligned overshoot (`5.01`) caps to `5.00`.
- Allocation is applied **before** step (`1.00 × 0.25 = 0.25`; `0.10 × 0.10 = 0.01`; `1.00 × 0.001` floors below min → `0`).
- Lives in Domain, not inside `Fix.CTrader` (A43 rule 13: do not grow a second converter in the FIX assembly).

That is enough for a **last-stage** `FloorToStep + min/max` helper **after** ounces conversion. It is not enough to emit FIX 38.

---

## 5. Worked last-stage numbers (this SUT, not the converter)

Unless noted: `InstrumentQuantitySpec(0.01, 5, 0.01, 2)`.

| Case | Inputs | Result | Note |
|---|---|---:|---|
| Smoke / G7 | lots=0.10, alloc=1, BaseUnits fixture max=5000 | **0.10** | A43 wants **10.00** |
| Mini same lots | lots=0.10, alloc=1 | **0.10** | contract_size is invisible |
| Lots-convention fixture | lots=0.10, alloc=1, max=50 | **0.10** | numbers match **only** by coincidence |
| E11 last-stage | 12.30, step=1.00 | **12.00** | floor, not 13 |
| E12 last-stage | 12.35, step=0.10 | **12.30** | |
| E13 last-stage | 10.01, step=0.10 | **10.00** | |
| E14 last-stage | 0.50, min=1, step=1 | **0** | not `SIZE_BELOW_MIN` |
| E15 last-stage | 0.99, min=1, step=0.01 | **0** | |
| Alloc 25% | 1.00 × 0.25 | **0.25** | |
| Alloc 10% of 0.10 | 0.10 × 0.10 | **0.01** | exact dest min |
| Alloc dust | 1.00 × 0.001 | **0** | 0.001 < 0.01 |
| Alloc 1/3 | 1 × (1/3) | **0.33** | Truncate, not 0.34 |
| Overshoot aligned | 5.01 / 100 | **5.00** | |
| Overshoot unaligned | 10, max=5.09, step=0.10 | **5.09** | F1 |
| Step/prec clash | 0.08, step=0.025, prec=2 | **0.07** | F2 |
| Alloc > 1 | 1 × 1.5 | **1.50** | F3 |
| Non-positive lots / alloc / step | 0 or negative | throw | param name set |

---

## 6. Tests (product source untouched; re-measured this wave)

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | 5174 | 162 | `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | A89 #47 last-stage contract |
| `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | 7344 | 184 | `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | A09 / A43 §12 / A89 #45 |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | 2144 | 62 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | 1 smoke fact |

Hashes match B17’s recorded test files. The Allocation row inside `QuantityNormalizerStepMinMaxTests` now expects `0.01` (see §6.3).

### 6.1 Command

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity minimal
  --filter FullyQualifiedName~QuantityNormalizerStepMinMaxTests|FullyQualifiedName~SourceDestinationQuantityConversionTests|FullyQualifiedName~ExecutionAndSizingTests.Quantity_normalizer
```

Measured 2026-08-18T13:34+05:30, exit 0:

| | Count |
|---|---:|
| Passed | **33** |
| Failed | **0** |
| Skipped | **22** |
| Total | 55 |

Skipped rows are **intentional**. They name the A43 methods that cannot run until `IQuantityConverter` exists. Bodies `Assert.Fail` / `true.Should().BeFalse(...)` so they cannot be un-skipped onto a test-local ounces helper (that would greenwash G7).

A green filter run is **not** G7 / G10.

### 6.2 What the 33 passing facts actually prove

**`QuantityNormalizerStepMinMaxTests` (28 executed + 1 skip):**

| Method | Rows | Proves |
|---|---:|---|
| `Floors_to_step` | 7 | 0.333/0.335/0.339/1.999/0.019/0.10/1÷3 all floor |
| `Floors_not_rounds_up_on_whole_step` | 1 | E11: 12.30 / 12.99 → 12.00 |
| `Floors_partial_step_of_tenth` | 1 | E12/E13 |
| `Below_min_returns_zero` | 1 | 0.10×0.05 and 0.009 → 0 |
| `Below_min_after_floor_returns_zero` | 1 | 0.019 floored to 0.01 < min 0.02 |
| `Exact_min_is_kept` | 1 | 0.01 |
| `Above_max_caps` / `Exact_max_is_kept` | 2 | aligned 5.00 |
| `Allocation_scales_before_step` | 1 | 1×0.25=0.25; **0.10×0.10=0.01**; 1×0.001=0 |
| `Precision_truncates_toward_zero_after_step` | 1 | prec=1 → 0.3 |
| `Coarser_precision_than_step_can_break_step_alignment` | 1 | F2 characterization |
| `Unaligned_max_is_returned_raw_not_re_floored` | 1 | F1 characterization (5.09) |
| `Allocation_greater_than_one_is_currently_accepted` | 1 | F3 characterization |
| `Non_positive_source_lots_throws` | 3 | 0 / −0.01 / −1 |
| `Non_positive_allocation_throws` | 3 | same |
| `Non_positive_step_throws` | 2 | 0 / −0.01 |
| `Negative_precision_throws` | 1 | BCL `ArgumentOutOfRangeException` |
| `Above_max_re_floors_to_step` | skip | E23 / F1 not implemented |

**`SourceDestinationQuantityConversionTests` (4 executed + 21 skip):**

| Method | Measured |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | mini vs standard is invisible |
| `Lots_convention_row_also_returns_source_lots` | Lots spec still `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | E10/E11/E14/E15 last-stage only |

**`ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`:** 0.10 stays 0.10; 0.10×0.05 → 0; 0.333 → 0.33.

**Still skipped (un-skip only against `IQuantityConverter`):**

`Never_passthrough_MT5_lots`, `Known_lot_to_OrderQty_examples` (E01–E09), `Mini_and_nano_contracts_differ`, `Lots_convention_only_when_mapped`, `Mt5_ticks_scale_is_10000`, `Decimal_not_double_for_0_0001_lot`, `Confidence_cannot_exceed_one`, `Allocation_and_confidence_scale`, `Dest_max_and_risk_caps_reduce`, `Below_min_after_cap_rejects`, `Margin_room_reduces_qty`, `Increase_uses_incremental_volume`, `Close_uses_mapped_destination_qty`, `Partial_reduce_is_fraction_of_dest`, `Dust_leftover_promotes_to_close`, `Unverified_dest_spec_rejects`, `Invalid_contract_size_rejects`, `Missing_mapping_rejects`, `Invalid_step_or_min_rejects`, `Shadow_and_live_share_converter`, `Fix_worker_does_not_rescale`.

### 6.3 Stale claims (do not reuse)

| Prior file | Claim | Measured now |
|---|---|---|
| `C17_unit_coverage.md` §0 / §5 | `Allocation_scales_before_step` is red (expects `0.10 × 0.10 = 0.10`, SUT returns `0.01`) | **Stale.** Current test expects `0.01m` (line 87) and **passes**. Whole-unit filter is 0 failed. |
| `A100_golive_gates.md` G10 | “No `SourceDestinationQuantityConversionTests` (A43)” | **Stale.** Class exists; 4 facts lock passthrough; 21 A43 methods are skipped. G10 is still **FAIL** for the right reason (converter absent), not because the file is missing. |
| `A09_unit_tests_audit.md` | conversion class MISSING | Superseded by the file on disk + this re-measure. |

B17’s 33/0/22 count is still the measured truth.

---

## 7. Call graph (product)

```text
CopyIntent.RequestedQuantity          ← written by nobody that calls this type
        │
        ▼
RiskEngine.Evaluate(RequestedQuantity)
        │  reject if > MaxPositionQuantity / XAU caps
        │  never Normalize
        ▼
RiskDecision.ApprovedQuantity         ← copy or 0
        │
        ▼
ExecutionIntent.RequestedQuantity     ← dest units by convention only
        │
        ▼
FIX worker / NOS tag 38               ← no builder consumes QuantityNormalizer

ShadowCopyEngine.SimulateEntry(quantity)  ← passthrough
```

`IQuantityConverter` is the missing node A43 §6 places at Risk step 10. `QuantityNormalizer` could become the last two lines of that node after F1 is fixed. It is not that node today.

---

## 8. What must exist next (not built here)

A43 §3 / §14. Do not grow `QuantityNormalizer` into a second converter inside `Fix.CTrader`. Do not copy ounces math into the test project.

```text
ticks / 10_000 → lots → × contract_size → oz
    × min(allocation, confidence≤1) → dest convention
    → FloorToStep → min (else REJECT SIZE_BELOW_MIN) → FloorToStep(max)
    → book/margin re-quantize
OPEN/INCREASE: incremental source ticks
REDUCE/CLOSE: mapped dest qty (never reconvert source lots)
```

Keep `QuantityNormalizer` (or a renamed `QuantityStep.Floor`) as the last two lines of that pipeline **after** F1 (re-floor max) and F3 (`allocationFactor > 1` reject). Wire it from `RiskEngine` step 10 and `ShadowCopyEngine`. FIX tag 38 = `requested_quantity` only.

Un-skip §6.2 converter facts only when those types exist.

---

## 9. Go-live

| Gate | Status |
|---|---|
| A100 G10 `position sizing conversion is verified` | **FAIL** |
| A89 G7 ounces path | **FAIL** (measured `0.10` ≠ `10.00`) |
| A43 E01–E50 | **not runnable** (converter absent; 21 skipped facts) |
| Pepperstone dest min/step/max/convention | **UNMEASURED** (lab fixtures only) |
| `QuantityNormalizer` last-stage floor (aligned max) | **PASS** (33 tests this run) |
| `QuantityNormalizer` last-stage floor (unaligned max) | **FAIL** (F1, skipped E23) |
| Product callers | **0** |

Checkbox stays `[ ]` until E01–E50 (or equivalent) pass on a **Measured** dest row and `Never_passthrough_MT5_lots` fails a deliberate `requested_quantity = source_lots` implementation.

---

## 10. Disposition

| Metric | Value |
|---|---|
| Product source changed | **No** |
| Test source changed | **No** |
| Classification | last-stage helper: `EXISTS_NEEDS_REFACTOR`; §38 converter: `MISSING` |
| Callers in product | **0** |
| SUT SHA vs B17 | **identical** |
| Qty-related tests this run | 33 pass / 22 skip / 0 fail |
| G10 | **FAIL** |
| INDEX / SWARM_LOG rewritten | **No** (this file is the assigned artifact) |
