# E039 — 22 skipped conversion tests (A43 / G7 / G10)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E039_skipped.md` |
| Agent | E039 (skipped source→dest quantity conversion tests) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:51:43+05:30 |
| Assigned | 22 skipped conversion tests. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only product-tree write besides `reports/SWARM_LOG.md`. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Binding law | Architecture §38 / executive #10 / §60 item 8 / §68 G7+G10; A43 (E01–E50); A38 (1 lot = 10_000); A89 #45 / #47; A09 |
| Siblings (do not treat as this file) | E004 (solution 64 pass / 22 skip); D18 / B17 (same SHAs); C14 G10; C17 (stale helper claim); A43 §10–§12; D09 §4.8 |
| Method | Full read of both test classes + `QuantityNormalizer`. SHA-256 via `Get-FileHash`. `dotnet test` filter on the three conversion classes (verbosity normal + `--list-tests`). Grep `IQuantityConverter` / `QuantityConvention` / `SIZE_BELOW_MIN` under `src/**/*.cs` (zero hits). Grep `QuantityNormalizer` callers (tests only). **No product edit. No un-skip.** |

Classification: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE` / `STALE_REPORT` / `PARTIAL`.

---

## 0. Verdict (binding)

**CONFIRMED. The entire unit project’s skip set is these 22 conversion tests. They are intentional A43 backlog, not flaky ignores.**

| Assigned claim | Measured result | Class |
|---|---|---|
| There are 22 skipped conversion tests | **Yes.** Filter run **33 passed / 0 failed / 22 skipped / 55 total**, exit 0 | measured |
| They prove source lots → dest `OrderQty` | **No.** Skipped methods never call product code. Passing facts lock **passthrough** `0.10 → 0.10` | G7 **FAIL** |
| `IQuantityConverter` exists | **No.** Zero types in `src/` | `MISSING` |
| Un-skipping today would go green | **No.** Bodies are `Assert.Fail` / `true.Should().BeFalse(...)` / `Should().Be(5.00m)` vs live `5.09m` | fail-closed |
| Green filter run = G7 / G10 | **No.** A green skip is **not** a verified conversion | G10 **FAIL** |

One-line:

```text
22 SKIPS = 21 A43 IQuantityConverter facts/theory + 1 E23 FloorToStep(max)
IQuantityConverter MISSING
PASSING FACTS LOCK 0.10 LOTS → 0.10 QTY (want 10.00 BaseUnits)
G7 / G10 STILL FAIL
```

Do **not** count 22 skips as coverage. Do **not** un-skip onto a test-local ounces helper. Do **not** treat `QuantityNormalizer` as the §38 converter.

---

## 1. Files hashed (inputs; no product edits)

| Path | Bytes | Lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | 7344 | 184 | `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | A09 / A43 §12 / A89 #45 — **21** skips |
| `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | 5174 | 162 | `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | A89 #47 last-stage — **1** skip (E23) |
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | 1041 | 31 | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` | last-stage floor only |
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | — | — | `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2` | source ticks↔lots; **not** dest conversion |
| `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` | — | 62 | (not re-hashed; 1 smoke fact in the filter) | `Quantity_normalizer_steps_and_min` passes |

Hashes match B17 / D18. Bytes/lines of the two conversion test files are unchanged.

`IQuantityConverter`, `QuantityConverter`, `QuantityConvention`, `spec_status`, `SIZE_BELOW_MIN`, `FloorToStep` — **0 hits** under `D:\Prop\src\**\*.cs`.

`QuantityNormalizer` / `InstrumentQuantitySpec` product callers: **definition file only**. `new QuantityNormalizer` exists only under `tests/`.

---

## 2. Measured command

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal
  --filter FullyQualifiedName~QuantityNormalizerStepMinMaxTests|FullyQualifiedName~SourceDestinationQuantityConversionTests|FullyQualifiedName~ExecutionAndSizingTests.Quantity_normalizer
```

Measured 2026-08-18T13:51:43+05:30, **exit 0**:

| | Count |
|---|---:|
| Passed | **33** |
| Failed | **0** |
| Skipped | **22** |
| Total | **55** |
| Duration | 0.3651 s (VSTest) / 1.60 s elapsed |

E004 (whole unit project, 13:48:18+05:30): **64 passed / 22 skipped / 0 failed / 86 total**. The 22 skips in the solution **are exactly this set**. `VolumeConverterTests` has **0** `[Skip]`. Integration: 0 skips.

`--list-tests` on the same filter lists **55** names. The skipped Theory is **one** list row (`Known_lot_to_OrderQty_examples`), not nine. xUnit does not expand `[Theory(Skip=…)]` InlineData.

A green run here is **not** G7 / G10.

---

## 3. The 22 skips (catalog)

### 3.1 Split

| Class | `[Skip]` attributes | vstest skipped | Executed |
|---|---:|---:|---:|
| `SourceDestinationQuantityConversionTests` | 20 Fact + 1 Theory | **21** | 4 Facts (passthrough lock) |
| `QuantityNormalizerStepMinMaxTests` | 1 Fact | **1** | 28 (last-stage floor) |
| `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min` | 0 | 0 | 1 smoke |
| **Total** | **22** | **22** | **33** |

### 3.2 `SourceDestinationQuantityConversionTests` — 21 skips

SUT named in the class header: **missing** `IQuantityConverter`. Live SUT of the 4 passing Facts: `QuantityNormalizer`.

| # | Method | Kind | Skip message (verbatim) | A43 / law | Body if un-skipped today |
|---:|---|---|---|---|---|
| 1 | `Never_passthrough_MT5_lots` | Fact | `A43 G7 / E01: IQuantityConverter missing. 0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.` | E01, E47, G7 | `Assert.Fail("Call IQuantityConverter.Convert; do not implement ounces math in the test.")` |
| 2 | `Known_lot_to_OrderQty_examples` | Theory (9 InlineData, **1 skip**) | `A43 §10.1 E01–E09: IQuantityConverter missing.` | E01–E09 | `Assert.Fail("Call IQuantityConverter.Convert for E01–E09.")` |
| 3 | `Mini_and_nano_contracts_differ` | Fact | `A43 E06 vs E01: mini contract_size=10 must yield 1.00 oz, not the same qty as contract_size=100.` | E06, E07 | `Assert.Fail` — `contract_size` is not on `InstrumentQuantitySpec` |
| 4 | `Lots_convention_only_when_mapped` | Fact | `A43 E08: Lots convention is the only mapping where 0.10 lots may equal OrderQty 0.10.` | E08 vs E01 | `Assert.Fail` — no `QuantityConvention` |
| 5 | `Mt5_ticks_scale_is_10000` | Fact | `A38: source ticks / 10_000 = lots. Converter not implemented.` | A38, A13 | `Assert.Fail("Converter must use VolumeConverter.Manager (1 lot = 10_000), never /100.")` |
| 6 | `Decimal_not_double_for_0_0001_lot` | Fact | `A43 E05: 1 tick × 100 oz = 0.01 dest; must stay decimal 0.01m.` | E05 | `Assert.Fail` — converter must return exact `0.01m` |
| 7 | `Confidence_cannot_exceed_one` | Fact | `A43 E21: confidence_scale > 1 is illegal. QuantityNormalizer has no confidence input.` | E21 | `true.Should().BeFalse(...)` |
| 8 | `Close_uses_mapped_destination_qty` | Fact | `A43 §4.7 E32/E38: REDUCE/CLOSE uses mapped dest qty, not source lots × allocation.` | E32, E38 | `true.Should().BeFalse` — CLOSE is not `Normalize` |
| 9 | `Dust_leftover_promotes_to_close` | Fact | `A43 E37: leftover < dest min promotes REDUCE to CLOSE.` | E37 | `true.Should().BeFalse` — dust policy missing |
| 10 | `Unverified_dest_spec_rejects` | Fact | `A43 E39: Unverified dest spec must reject. InstrumentQuantitySpec has no spec_status.` | E39 | `true.Should().BeFalse` — no `spec_status` |
| 11 | `Allocation_and_confidence_scale` | Fact | `A43 E16–E21: allocation × confidence before dest step. QuantityNormalizer has no confidence input.` | E16–E21 | `true.Should().BeFalse` |
| 12 | `Dest_max_and_risk_caps_reduce` | Fact | `A43 E22–E26: dest max and risk caps reduce then re-quantize.` | E22–E26 | `true.Should().BeFalse` |
| 13 | `Below_min_after_cap_rejects` | Fact | `A43 E14/E20/E25/E29: below min after cap is REJECT, not a sendable 0.` | E14, E20, E25, E29 | `true.Should().BeFalse` — `Normalize` returns `0m`, not `SIZE_BELOW_MIN` |
| 14 | `Margin_room_reduces_qty` | Fact | `A43 E27–E29: margin room reduces qty. No quote/leverage on QuantityNormalizer.` | E27–E29 | `true.Should().BeFalse` |
| 15 | `Increase_uses_incremental_volume` | Fact | `A43 E30–E31: INCREASE uses incremental source ticks, not position max_volume.` | E30–E31 | `true.Should().BeFalse` |
| 16 | `Partial_reduce_is_fraction_of_dest` | Fact | `A43 E33–E37: REDUCE is a fraction of mapped dest qty.` | E33–E37 | `true.Should().BeFalse` |
| 17 | `Invalid_contract_size_rejects` | Fact | `A43 E40–E41: contract_size <= 0 / NaN rejects.` | E40–E41 | `true.Should().BeFalse` — no `contract_size` |
| 18 | `Missing_mapping_rejects` | Fact | `A43 E42–E43: missing dest spec or REDUCE link rejects.` | E42–E43 | `true.Should().BeFalse` |
| 19 | `Invalid_step_or_min_rejects` | Fact | `A43 E44–E46: step 0 / step 0.001 / min not multiple of step reject the spec.` | E44–E46 | `true.Should().BeFalse` — spec is an unvalidated record |
| 20 | `Shadow_and_live_share_converter` | Fact | `A43 §6: shadow and live must call the same converter.` | A43 §9.12 | `true.Should().BeFalse` — `QuantityNormalizer` unused by `ShadowCopyEngine` and `RiskEngine` |
| 21 | `Fix_worker_does_not_rescale` | Fact | `A43 §6: FIX worker must not rescale requested_quantity.` | A43 §9.11 | `true.Should().BeFalse` — no FIX NOS builder consumes normalizer output |

### 3.3 Latent Theory rows (still **one** vstest skip)

`Known_lot_to_OrderQty_examples` InlineData — A43 §10.1 identity table. Not executed. Parameters are discarded (`_ = (ticks, contractSize, convention, expected)`).

| ticks | lots (= ticks/10_000) | contract | oz | convention | expected `OrderQty` | A43 |
|---:|---:|---:|---:|---|---:|---|
| 1_000 | 0.10 | 100 | 10 | BaseUnits | **10.00** | E01 |
| 100 | 0.01 | 100 | 1 | BaseUnits | **1.00** | E02 |
| 10_000 | 1.00 | 100 | 100 | BaseUnits | **100.00** | E03 |
| 10 | 0.001 | 100 | 0.10 | BaseUnits | **0.10** | E04 |
| 1 | 0.0001 | 100 | 0.01 | BaseUnits | **0.01** | E05 |
| 1_000 | 0.10 | 10 | 1 | BaseUnits | **1.00** | E06 |
| 1_000 | 0.10 | 1 | 0.10 | BaseUnits | **0.10** | E07 |
| 1_000 | 0.10 | 100 | 10 | Lots | **0.10** | E08 |
| 100 | 0.01 | 100 | 1 | Lots | **0.01** | E09 |

If this Theory is un-skipped **without** a converter, vstest would report **9 failures**, not 1. Do not count 9 extra “tests” today.

### 3.4 `QuantityNormalizerStepMinMaxTests` — 1 skip

| # | Method | Skip message (verbatim) | A43 | Live vs required |
|---:|---|---|---|---|
| 22 | `Above_max_re_floors_to_step` | `A43 E23: after dest max, q must be FloorToStep(max, step). Today Normalize returns raw MaxQuantity.` | E23 | Input `10` lots, spec `max=5.09 step=0.10` → live **`5.09m`** (passing sibling `Unaligned_max_is_returned_raw_not_re_floored`). Required **`5.00m`**. |

This is the only skip whose body already calls product code. It is skipped so the last-stage helper can stay green while F1 (raw max) is documented. Un-skip → **red** until `Normalize` (or the future converter’s last stage) does `FloorToStep(max, step)`.

---

## 4. The 4 passing conversion Facts (not skips — they lock the defect)

These run against `QuantityNormalizer`. They are **characterization of G7**, not A43 coverage.

| Method | Measured | Spec |
|---|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `Normalize(0.10, 1, DestBaseUnits1Oz) == 0.10` **and** `!= 10.00` | E01 wants **10.00** |
| `Mini_contract_same_lots_same_normalizer_output` | same `0.10` | E06 wants **1.00** (contract_size=10) |
| `Lots_convention_row_also_returns_source_lots` | `DestLots100Oz` still `0.10` | E08 is the **only** legal `0.10==0.10` row; the test cannot tell Lots from BaseUnits |
| `Respects_min_qty_and_step_as_last_stage` | 12.30 stays 12.30; whole-step → 12.00; 0.50 / 0.99 → **0m** | E10/E11 last-stage only. E14/E15 spec is **Reject `SIZE_BELOW_MIN`**, not a sendable 0 |

`ExecutionAndSizingTests.Quantity_normalizer_steps_and_min` repeats the passthrough (`0.10 → 0.10`).

---

## 5. A43 fixture coverage vs these 22

| A43 ids | Owned by skip # | Status |
|---|---|---|
| E01–E09 identity | #1, #2, #3, #4, #6 | skipped |
| E10–E13 step floor | passing last-stage Facts (not a skip) | **PARTIAL** — no ounces in front |
| E14 / E20 / E25 / E29 reject-not-zero | #13 | skipped; live helper returns `0m` |
| E16–E21 alloc × confidence | #7, #11 | skipped — no confidence arg |
| E22–E26 dest/risk caps | #12 | skipped |
| E23 FloorToStep(max) | **#22** | skipped; live returns raw max |
| E27–E29 margin | #14 | skipped — no quote/leverage |
| E30–E31 INCREASE incremental ticks | #15 | skipped |
| E32 / E38 CLOSE mapped dest | #8 | skipped |
| E33–E37 REDUCE fraction | #16, #9 | skipped |
| E39 unverified spec | #10 | skipped |
| E40–E41 bad contract | #17 | skipped |
| E42–E43 missing map / REDUCE link | #18 | skipped |
| E44–E46 invalid step/min | #19 | skipped (`StepSize<=0` already throws from the helper; 0.001 / min-not-multiple do not) |
| E47 passthrough FAIL | #1 | skipped (the **passing** sibling locks the illegal assignment) |
| E48–E50 (GOLD dest / 0 ticks / flip-side) | — | **no test method** |
| §6 one converter, two callers | #20 | skipped — zero product callers |
| §6 FIX does not rescale | #21 | skipped — no `35=D` builder (SAFE_BY_ABSENCE) |
| A38 ticks/10_000 | #5 | skipped; `VolumeConverterTests` (3 Facts, 0 skip) only covers source scale |

---

## 6. Why they are skipped (product holes)

A43 §9 binding rules vs live Domain:

| A43 rule | Live |
|---|---|
| 1. No passthrough `source_lots` → tag 38 | `Normalize(0.10, 1) == 0.10`. Demo store writes `CopyIntent.RequestedQuantity = trade.MaxVolumeLots` (`EfTradingStore` ~L302) and `SimulateEntry(..., trade.MaxVolumeLots, ...)` |
| 2. All qty math `decimal` | Helper is `decimal`. Converter that would keep E05 exact `0.01m` is missing |
| 3. No `Volume/100` | `VolumeConverter.Manager` = 10_000. Converter does not call it |
| 4. Read `source_spec.contract_size` | `InstrumentQuantitySpec` = `(Min, Max, Step, Precision)` only. `SourceSymbolMapping` has no contract size. `CanonicalInstrument` is `Id/Code/Description` |
| 5–6. Dest id + verified min/step/max | No `destination_symbols`. No `spec_status` |
| 7. Floor, never ceil | Helper floors. Does **not** re-floor max (E23 / skip #22) |
| 8. Below min → reject | Helper returns **`0m`** (sendable zero, not `SIZE_BELOW_MIN`) |
| 9. Confidence cannot increase size | No confidence parameter; `allocationFactor > 1` is accepted (passing characterization) |
| 10. REDUCE/CLOSE from mapped dest qty | No mapped dest position input |
| 11. FIX worker does not convert | No NOS builder at all (E002 / E016) |
| 12–13. One converter; shadow = live | Type **MISSING**. `ShadowCopyEngine` copies the quantity it is given. `RiskEngine.Evaluate` consumes pre-baked `RequestedQuantity` and never calls `Normalize` |

`QuantityNormalizer` algorithm (31 lines, unchanged vs D18):

```text
raw   = sourceLots * allocationFactor
qty   = Truncate(raw / step) * step
qty   = Round(qty, precision, ToZero)
qty < min → 0
qty > max → raw MaxQuantity     // not FloorToStep(max)
```

That is a last-stage floor. It is **not** `ticks → lots → oz → convention → caps → tag 38`.

---

## 7. What un-skipping would do (do not do this)

| Action | Result |
|---|---|
| Remove `[Skip]` on #1–#21 | **21 red** (`Assert.Fail` / `BeFalse`). No converter to call |
| Implement ounces math **inside the test** | Greenwash G7. Forbidden by the Fail message on #1 |
| Remove `[Skip]` on #22 only | **1 red** (`5.09` ≠ `5.00`) until last-stage F1 is fixed |
| Un-skip Theory #2 | **9 red** (InlineData expand) |
| Report 22 skips as “22 tests” in a coverage score | Lie. They did not execute |

Correct order (A43 §14 / C17 item 10 / D18 §6): implement `IQuantityConverter` in Domain/Application → wire Risk + Shadow → FIX tag 38 = `requested_quantity` only → then un-skip against **product** `Convert`. Keep `QuantityNormalizer` as the last two lines after F1 is fixed.

---

## 8. Stale vs this file

| Earlier claim | This measure |
|---|---|
| A43 §13 `SourceDestinationQuantityConversionTests` **MISSING** | File **exists** (184 lines). Status = **PARTIAL** (4 pass + 21 skip) |
| C17 “bodies call a local `ConvertTicks` helper” | **STALE.** Current bodies are `Assert.Fail` / `BeFalse` only |
| C17 “83 tests / 1 skip family” | E004: unit **86 / 64 / 22 / 0**. Skip family still 22 |
| B17 / D18 “33 pass / 22 skip” on this filter | **Still true** (same SHAs, re-run 13:51:43) |
| A09 class missing | **STALE.** Class is on disk under `Normalization/`, not `Sizing/` |

---

## 9. Go-live boxes this catalog owns

| Gate | Status | Why these 22 do not flip it |
|---|---|---|
| §68 **G7** never passthrough MT5 lots | **FAIL** | Skip #1 is the owning test and is skipped. Passing Facts assert `0.10` |
| §68 **G10** position sizing conversion is verified | **FAIL** | E01–E09 Theory skipped. Dest spec not measured on Pepperstone |
| A43 checkbox `IQuantityConverter` only path to FIX 38 | **unchecked** | Type missing; no FIX 38 path |
| A43 checkbox E01–E50 pass | **unchecked** | 22 skips + E48–E50 unwritten |

Live copy remains **OFF** / **SAFE_BY_ABSENCE** (E016). That does **not** make G7 a pass.

---

## 10. Honesty

- 22 is the **vstest skip count**, not 22 + 9 Theory rows.
- Skips are the **correct** fail-closed posture while the SUT is missing.
- Passing conversion Facts are **evidence of the bug**, not evidence the converter works.
- `VolumeConverter` (3/3 green, 0 skip) is source scale only.
- Demo shadow qty is **source lots** (`MaxVolumeLots`). That is the same illegal passthrough the skipped tests exist to catch.
- This file does not implement `IQuantityConverter`. Product source was not modified.
