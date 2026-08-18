# B17 — QuantityNormalizer review

| Field | Value |
|---|---|
| Agent | B17 (quantity / last-stage step-min-max) |
| Date | 2026-08-18 |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture §38, executive change #10, `A43_position_sizing.md`, `A38` (1 lot = 10 000), `A89` #45/#47, `A100` G10 |
| Product source edited | **No** |
| Tests added | `tests/Unit/Sizing/QuantityNormalizerStepMinMaxTests.cs`, `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs` |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**`EXISTS_NEEDS_REFACTOR` as a last-stage floor. `MISSING` as the §38 / A43 converter. G7 / G10 remain FAIL.**

`QuantityNormalizer` is 31 lines: `sourceLots * allocationFactor`, floor to `StepSize`, truncate to `Precision`, then min→`0` / max→raw cap. All arithmetic is `decimal`. It never sees MT5 ticks, contract size, dest convention, confidence, margin, or a mapped position.

Measured passthrough (A43 E01 / G7):

```text
Normalize(0.10, allocation=1, Dest_BaseUnits min=0.01 step=0.01 max=5000)
    = 0.10
```

A43 requires **10.00** OrderQty (0.10 lots × 100 oz/lot ÷ 1 oz/unit). `0.10` would be **100× too small** on a Spotware-style BaseUnits XAU book, or rejected as below min on a lots-convention book that expected the same number for a different reason.

`QuantityNormalizer` is **not referenced** by `RiskEngine`, `ShadowCopyEngine`, Application ingestion, or either worker. `grep Normalize(` / `new QuantityNormalizer` over product `*.cs` = definition only. The class cannot satisfy §68 “position sizing conversion is verified.”

---

## 1. Measured source

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Bytes | 1041 |
| Lines | 31 |
| SHA-256 | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` |

Types in the same file:

| Type | Kind | Role |
|---|---|---|
| `InstrumentQuantitySpec` | `record` (Min, Max, Step, Precision) | dest limits only; no convention, no ounces, no `spec_status` |
| `QuantityNormalizer` | `sealed class` | one method `Normalize(sourceLots, allocationFactor, dest)` |

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

Guards that do **not** exist: `allocationFactor > 1`, `MinQuantity` multiple of step, `MaxQuantity >= MinQuantity`, `StepSize` multiple of 0.01, `Precision >= 0`, finite contract size.

`Precision < 0` is not checked; `decimal.Round(..., -1, ToZero)` throws `ArgumentOutOfRangeException` from the BCL (locked by test).

---

## 2. Spec vs code (do not rubber-stamp)

| ID | Spec (A43 / §38) | Current code | Stance |
|---|---|---|---|
| G7 / E01 | 0.10 lots × 100 oz → BaseUnits `OrderQty=10.00` | `0.10 * 1 = 0.10` | **FAIL** passthrough |
| E06 | same 0.10 lots × 10 oz (mini) → `1.00` | still `0.10` | **FAIL** ignores contract |
| E08 | Lots convention is the **only** case `0.10→0.10` | always passthrough | **FAIL** |
| E11 / §4.5 | floor, never ceil | `Truncate` then `ToZero` | **PASS** for positive qty |
| E14 | below min after floor → do not send | returns `0m` | **PARTIAL** (0 is not `SIZE_BELOW_MIN`) |
| E23 | `max=5.09` step `0.10` → **5.00** | returns **5.09** | **FAIL** raw max |
| E21 | `confidence_scale > 1` reject | no confidence input; `allocation>1` accepted | **FAIL** |
| E32 / E38 | CLOSE = mapped dest qty | no close path | **MISSING** |
| E39 | Unverified dest spec rejects | no `spec_status` | **MISSING** |
| E44–E46 | invalid min/step rejects spec | step≤0 throws; 0.001 step and non-multiple min accepted | **PARTIAL** |
| A38 | ticks / 10 000 | no tick input | **MISSING** (scale lives on `VolumeConverter` only) |
| A43 §6 | Risk + Shadow + FIX share one converter; FIX does not rescale | **zero callers** | **MISSING** |
| A43 rule 2 | `decimal` only | `decimal` | **PASS** |

`InstrumentQuantitySpec` is a dest-only tuple. It cannot carry `QuantityConvention`, `unit_size_oz` / `lot_size_oz`, `spec_status`, or source `contract_size`. Extending this record until it is the A43 dest spec is acceptable; treating `Normalize(sourceLots, 1, spec)` as OrderQty is not.

---

## 3. Defects on the class that *does* exist

These are independent of the missing ounces pipeline. A last-stage quantizer must still get them right (A43 §4.5).

### F1 — Cap does not re-floor to step (E23)

```text
Normalize(10, 1, min=0.01 max=5.09 step=0.10 prec=2)  →  5.09
```

5.09 is not a multiple of 0.10. Sending tag 38 = 5.09 is `SIZE_NOT_MULTIPLE_OF_STEP`. Spec: `q = FloorToStep(max, step)` → **5.00**, path `ReduceSize`.

Characterization test: `Unaligned_max_is_returned_raw_not_re_floored` (locks today’s 5.09).  
Spec test: `Above_max_re_floors_to_step` **Skipped** until the cap is `FloorToStep`.

### F2 — Precision after step can break alignment

```text
Normalize(0.08, 1, min=0.01 max=5 step=0.025 prec=2)
    Truncate(0.08/0.025)=3 → 0.075 → Round(ToZero, 2) → 0.07
```

0.07 is not a multiple of 0.025. A43 also forbids `step=0.025` on FIX (step must be a multiple of 0.01). Spec validation is missing on the record, so this input is legal today.

### F3 — `allocationFactor > 1` is accepted

A43 `AllocationFactor` ∈ `(0, 1]`. `Normalize(1, 1.5, DefaultSpec)` returns `1.50`. Confidence/allocation must not enlarge size.

### F4 — Below min is `0m`, not a reject reason

Callers cannot tell “do not send” from a tradable zero. A43 wants `REJECT` / `SIZE_BELOW_MIN` and **zero FIX**. Returning `0` is only safe if every caller treats 0 as reject. There are no callers.

### F5 — Spec record is unvalidated

`MinQuantity=0.03` + `StepSize=0.02` (E46), `MaxQuantity < MinQuantity`, `StepSize=0.001` (E45), `Precision<0` all construct. Only `StepSize<=0` is rejected, and only at `Normalize` time.

### F6 — Dead path

No product call site. `RiskEngine.Evaluate` takes a pre-baked `RequestedQuantity` and never quantizes. `ShadowCopyEngine` copies the quantity it is given. FIX worker has no NOS builder hooked to this type. Shipping this class does not move G10.

---

## 4. What this class gets right

- `decimal` end-to-end (no `double` lots).
- Floor via `Truncate` on a positive raw qty — 0.333 / 0.335 / 0.339 / 1.999 all step to the lower increment; 12.30 with step 1.00 → 12.00 (E11).
- `MidpointRounding.ToZero` after step — no banker’s round-up.
- `sourceLots<=0`, `allocationFactor<=0`, `StepSize<=0` throw with a param name.
- Exact min (`0.01`) kept; just-below-min after floor → `0`.
- Aligned max (`5.00`) kept; aligned overshoot (`5.01`) caps to `5.00`.

That is enough for a **last-stage** `FloorToStep + min/max` helper **after** ounces conversion. It is not enough to emit FIX 38.

---

## 5. Tests written this wave (product source untouched)

| Path | SHA-256 | Role |
|---|---|---|
| `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | A89 #47 last-stage contract |
| `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | A09 / A43 §12 / A89 #45 |

Pre-existing smoke (left in place): `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min` (3 asserts).

### 5.1 Command

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj
  --filter FullyQualifiedName~QuantityNormalizerStepMinMaxTests|FullyQualifiedName~SourceDestinationQuantityConversionTests|FullyQualifiedName~ExecutionAndSizingTests.Quantity_normalizer
```

| | Count |
|---|---:|
| Passed | **33** |
| Failed | **0** |
| Skipped | **22** |
| Total | 55 |

Skipped rows are **intentional**. They name the A43 methods that cannot run until `IQuantityConverter` exists. Bodies `Assert.Fail` so they cannot be un-skipped onto a test-local ounces helper (that would greenwash G7).

### 5.2 `QuantityNormalizerStepMinMaxTests` (A89 first methods + edges)

Running: `Floors_to_step` (7 rows), `Floors_not_rounds_up_on_whole_step` (E11), `Floors_partial_step_of_tenth` (E12/E13), `Below_min_returns_zero`, `Below_min_after_floor_returns_zero`, `Exact_min_is_kept`, `Above_max_caps`, `Exact_max_is_kept`, `Allocation_scales_before_step`, precision / alignment / raw-max characterization, `allocation>1` characterization, non-positive source / allocation / step, negative precision.

Skipped: `Above_max_re_floors_to_step` (E23 / F1).

### 5.3 `SourceDestinationQuantityConversionTests`

**Running (measure G7, do not hide it):**

| Method | Measured |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | mini vs standard is invisible |
| `Lots_convention_row_also_returns_source_lots` | Lots spec still `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | E10/E11/E14/E15 last-stage only |

**Skipped (A43 §12 names, un-skip only against `IQuantityConverter`):**

`Never_passthrough_MT5_lots`, `Known_lot_to_OrderQty_examples` (E01–E09), `Mini_and_nano_contracts_differ`, `Lots_convention_only_when_mapped`, `Mt5_ticks_scale_is_10000`, `Decimal_not_double_for_0_0001_lot`, `Confidence_cannot_exceed_one`, `Allocation_and_confidence_scale`, `Dest_max_and_risk_caps_reduce`, `Below_min_after_cap_rejects`, `Margin_room_reduces_qty`, `Increase_uses_incremental_volume`, `Close_uses_mapped_destination_qty`, `Partial_reduce_is_fraction_of_dest`, `Dust_leftover_promotes_to_close`, `Unverified_dest_spec_rejects`, `Invalid_contract_size_rejects`, `Missing_mapping_rejects`, `Invalid_step_or_min_rejects`, `Shadow_and_live_share_converter`, `Fix_worker_does_not_rescale`.

---

## 6. What must exist next (not built here)

A43 §3 / §14. Do not grow `QuantityNormalizer` into a second converter inside `Fix.CTrader`.

```text
ticks / 10_000 → lots → × contract_size → oz
    × min(allocation, confidence≤1) → dest convention
    → FloorToStep → min (else REJECT) → FloorToStep(max)
    → book/margin re-quantize
OPEN/INCREASE: incremental source ticks
REDUCE/CLOSE: mapped dest qty (never reconvert source lots)
```

Keep `QuantityNormalizer` (or a renamed `QuantityStep.Floor`) as the last two lines of that pipeline **after** F1 is fixed. Wire it from `RiskEngine` step 10 and `ShadowCopyEngine`. FIX tag 38 = `requested_quantity` only.

Un-skip §5.3 only when those types exist. Do not copy A43 ounces math into the test project.

---

## 7. Go-live

| Gate | Status |
|---|---|
| A100 G10 `position sizing conversion is verified` | **FAIL** |
| A89 G7 ounces path | **FAIL** (measured `0.10` ≠ `10.00`) |
| A43 E01–E50 | **not runnable** (converter absent) |
| Pepperstone dest min/step/max/convention | **UNMEASURED** (lab fixtures only) |
| `QuantityNormalizer` last-stage floor (aligned max) | **PASS** (33 tests) |
| `QuantityNormalizer` last-stage floor (unaligned max) | **FAIL** (F1, skipped E23) |

Checkbox stays `[ ]` until E01–E50 (or equivalent) pass on a **Measured** dest row and `Never_passthrough_MT5_lots` fails a deliberate `requested_quantity = source_lots` implementation.

---

## 8. Disposition

| Metric | Value |
|---|---|
| Product source changed | **No** |
| Classification | last-stage helper: `EXISTS_NEEDS_REFACTOR`; §38 converter: `MISSING` |
| Callers in product | **0** |
| New test classes | 2 |
| Qty-related tests this run | 33 pass / 22 skip / 0 fail |
| G10 | **FAIL** |
