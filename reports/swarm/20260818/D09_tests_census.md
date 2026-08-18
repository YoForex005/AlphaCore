# D09 — `tests/Unit` + `tests/Integration` census (test method names)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D09_tests_census.md` |
| Agent | D09 (test-method census) |
| Date | 2026-08-18 |
| Assigned | Inventory `tests/Unit` and `tests/Integration`. Write this file with test method names. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| Scope | `D:\Prop\tests\Unit` and `D:\Prop\tests\Integration` only (exclude `bin/` / `obj/`) |
| Out of scope | `mt5-sdk/tests/*.cpp`, proposed `tests/Replay`, proposed `tests/Fix` |

**Companions (do not treat this file as coverage):** A09 / A10 (empty-scaffold audits, stale), A27 (required-class inventory), A89 (92 unit-class backlog), A90 (integration-class backlog), B08 (29-case gap), C16 (seed review), C17 (Unit vs §60), C52 (29/29 prediction).

This file is a **name census**. A listed method is not Architecture §60 coverage.

---

## 0. Verdict (counts only)

| Lane | Source `.cs` files | Public test classes | Distinct methods | Discovered xUnit cases |
|---|---:|---:|---:|---:|
| `tests/Unit` | 9 | 9 | **68** | **83** |
| `tests/Integration` | 2 | 2 | **3** | **3** |
| **Total** | **11** | **11** | **71** | **86** |

Discovered via `dotnet test <dll> --list-tests` on the already-built Debug net8.0 assemblies (Unit csproj rebuild was locked by another process writing `TraderIntelligence.Domain.dll`; Integration listed cleanly). Source was also read so skipped Theories and helpers are not missed.

| Attribute mix (distinct methods) | Unit | Integration |
|---|---:|---:|
| `[Fact]` (no Skip) | 43 | 3 |
| `[Fact(Skip=…)]` | 20 | 0 |
| `[Theory]` (no Skip) | 5 | 0 |
| `[Theory(Skip=…)]` | 1 | 0 |

xUnit does **not** expand the skipped `Known_lot_to_OrderQty_examples` InlineData rows (9 rows stay 1 discovered name). Active Theories **are** expanded (`Maps_known_aliases_to_XAUUSD` ×5, `Floors_to_step` ×7, throw theories ×3/3/2).

File-name ≠ class-name leftovers:

| Path | Class |
|---|---|
| `D:\Prop\tests\Unit\UnitTest1.cs` | `SmokeTests` |
| `D:\Prop\tests\Integration\UnitTest1.cs` | `PlaceholderRemoved` |

---

## 1. Method

| Source | Path |
|---|---|
| Unit project | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| Integration project | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| Tree | `Get-ChildItem D:\Prop\tests\{Unit,Integration} -Recurse -Filter *.cs` excluding `bin`/`obj` |
| Discovery | `dotnet test …\TraderIntelligence.Tests.Unit.dll --list-tests` |
| Discovery | `dotnet test …\TraderIntelligence.Tests.Integration.dll --list-tests` |

Projects (read only):

| Project | TFM | Packages | Project refs |
|---|---|---|---|
| Unit | net8.0 | xUnit 2.5.3, FluentAssertions 6.12.0, Moq 4.20.70, coverlet 6.0.0, Microsoft.NET.Test.Sdk 17.8.0 | Domain, Application, Fix.CTrader |
| Integration | net8.0 | xUnit 2.5.3, FluentAssertions 6.12.0, EF InMemory 8.0.4, coverlet 6.0.0, Microsoft.NET.Test.Sdk 17.8.0 | Domain, Application, Infrastructure, Fix.CTrader, Mt5 |

No `[Trait]`. No `IClassFixture`. No Testcontainers. No `tests/Replay`. No `tests/Fix`.

---

## 2. On-disk files (SHA-256 this pass)

| Bytes | SHA-256 | Path |
|---:|---|---|
| 1113 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| 224 | `6B1A127F1810FF0A0E1C07F0913A415CBE61D31FE56DF3BD46378C97EB77E6A5` | `D:\Prop\tests\Unit\UnitTest1.cs` |
| 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | `D:\Prop\tests\Unit\BaselineScorerTests.cs` |
| 2144 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `D:\Prop\tests\Unit\ExecutionAndSizingTests.cs` |
| 2909 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `D:\Prop\tests\Unit\RiskEngineTests.cs` |
| 896 | `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `D:\Prop\tests\Unit\SymbolNormalizerTests.cs` |
| 3939 | `5D99BA22B0FEFC248568E6CB0B462A31126DF825F57D34F9DD8C1586B661FBF2` | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` |
| 791 | `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5` | `D:\Prop\tests\Unit\VolumeConverterTests.cs` |
| 7344 | `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` |
| 5174 | `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` |
| 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| 162 | `49671A3C7C367ED87C7711E2204865AA2ABB8A7A5783AD785CD66A1F6DA7F4D6` | `D:\Prop\tests\Integration\UnitTest1.cs` |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |

---

## 3. `tests/Unit` — class roll-up

Namespace root: `TraderIntelligence.Tests.Unit`.

| # | Class | File | Methods | Cases | Skip methods |
|---:|---|---|---:|---:|---:|
| 1 | `SmokeTests` | `UnitTest1.cs` | 1 | 1 | 0 |
| 2 | `BaselineScorerTests` | `BaselineScorerTests.cs` | 3 | 3 | 0 |
| 3 | `ExecutionAndSizingTests` | `ExecutionAndSizingTests.cs` | 6 | 6 | 0 |
| 4 | `RiskEngineTests` | `RiskEngineTests.cs` | 5 | 5 | 0 |
| 5 | `SymbolNormalizerTests` | `SymbolNormalizerTests.cs` | 2 | 6 | 0 |
| 6 | `TradeReconstructionTests` | `TradeReconstructionTests.cs` | 5 | 5 | 0 |
| 7 | `VolumeConverterTests` | `VolumeConverterTests.cs` | 3 | 3 | 0 |
| 8 | `Normalization.SourceDestinationQuantityConversionTests` | `Normalization/SourceDestinationQuantityConversionTests.cs` | 25 | 25 | 21 |
| 9 | `Sizing.QuantityNormalizerStepMinMaxTests` | `Sizing/QuantityNormalizerStepMinMaxTests.cs` | 18 | 29 | 1 |
| | **Unit total** | | **68** | **83** | **22** |

---

## 4. `tests/Unit` — every test method

Fully-qualified name = `{namespace}.{class}.{method}`. Kind is the attribute on the method. Line numbers are 1-based in the current file.

### 4.1 `TraderIntelligence.Tests.Unit.SmokeTests` (`UnitTest1.cs`)

| Line | Kind | Method |
|---:|---|---|
| 6 | Fact | `Domain_assembly_loads` |

Discovered:

```text
TraderIntelligence.Tests.Unit.SmokeTests.Domain_assembly_loads
```

### 4.2 `TraderIntelligence.Tests.Unit.BaselineScorerTests`

| Line | Kind | Method |
|---:|---|---|
| 13 | Fact | `Two_trades_remain_insufficient` |
| 21 | Fact | `Three_disciplined_winners_go_to_shadow_not_live` |
| 30 | Fact | `Martingale_after_losses_is_risk_blocked` |

Helper (not a test): `Closed(int n, decimal pnl, decimal lots = 0.10m)`.

Discovered:

```text
TraderIntelligence.Tests.Unit.BaselineScorerTests.Two_trades_remain_insufficient
TraderIntelligence.Tests.Unit.BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live
TraderIntelligence.Tests.Unit.BaselineScorerTests.Martingale_after_losses_is_risk_blocked
```

### 4.3 `TraderIntelligence.Tests.Unit.ExecutionAndSizingTests`

| Line | Kind | Method |
|---:|---|---|
| 10 | Fact | `Unknown_ack_cannot_retry_new_order` |
| 19 | Fact | `Disconnect_after_send_is_unknown_state` |
| 26 | Fact | `Filled_report_is_terminal` |
| 35 | Fact | `Quantity_normalizer_steps_and_min` |
| 45 | Fact | `ClOrdId_is_deterministic_and_unique_per_sequence` |
| 56 | Fact | `Copy_intent_expires` |

Discovered:

```text
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Unknown_ack_cannot_retry_new_order
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Disconnect_after_send_is_unknown_state
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Filled_report_is_terminal
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Quantity_normalizer_steps_and_min
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.ClOrdId_is_deterministic_and_unique_per_sequence
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Copy_intent_expires
```

### 4.4 `TraderIntelligence.Tests.Unit.RiskEngineTests`

| Line | Kind | Method |
|---:|---|---|
| 12 | Fact | `Stale_quote_rejects_open` |
| 21 | Fact | `Real_flag_false_never_allows_fix_send` |
| 29 | Fact | `Stop_new_execution_blocks_opens_not_closes` |
| 44 | Fact | `Unreconciled_venue_blocks_new_exposure` |
| 51 | Fact | `Stale_signal_rejected` |

Helper (not a test): `Base(Func<RiskEvaluationRequest, RiskEvaluationRequest>? tweak = null)`.

Discovered:

```text
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open
TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send
TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes
TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected
```

### 4.5 `TraderIntelligence.Tests.Unit.SymbolNormalizerTests`

| Line | Kind | Method | Cases |
|---:|---|---|---:|
| 16 | Theory | `Maps_known_aliases_to_XAUUSD(string source)` | 5 |
| 24 | Fact | `Does_not_guess_venue_instrument_ids` | 1 |

`[InlineData]` for `Maps_known_aliases_to_XAUUSD`: `XAUUSD`, `XAUUSD.`, `XAUUSDm`, `XAUUSD.a`, `GOLD`.

Discovered:

```text
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD.")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSDm")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD.a")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "GOLD")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Does_not_guess_venue_instrument_ids
```

### 4.6 `TraderIntelligence.Tests.Unit.TradeReconstructionTests`

| Line | Kind | Method |
|---:|---|---|
| 13 | Fact | `Reconstructs_simple_round_trip` |
| 33 | Fact | `Scale_in_and_partial_close` |
| 52 | Fact | `Reverse_inout_closes_then_opens_opposite` |
| 70 | Fact | `First_three_completed_xau_unlocks_early_score` |
| 84 | Fact | `Ignores_balance_deals` |

Helper (not a test): `Deal(long ticket, long position, DealAction action, DealEntry entry, ulong volume, decimal price, decimal profit, int t)`.

Discovered:

```text
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reconstructs_simple_round_trip
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Scale_in_and_partial_close
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reverse_inout_closes_then_opens_opposite
TraderIntelligence.Tests.Unit.TradeReconstructionTests.First_three_completed_xau_unlocks_early_score
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Ignores_balance_deals
```

### 4.7 `TraderIntelligence.Tests.Unit.VolumeConverterTests`

| Line | Kind | Method |
|---:|---|---|
| 9 | Fact | `Manager_scale_maps_0_10_lots_to_1000_native` |
| 18 | Fact | `Extended_scale_maps_one_lot_to_100_million` |
| 25 | Fact | `Hundredths_comment_is_not_the_default` |

Discovered:

```text
TraderIntelligence.Tests.Unit.VolumeConverterTests.Manager_scale_maps_0_10_lots_to_1000_native
TraderIntelligence.Tests.Unit.VolumeConverterTests.Extended_scale_maps_one_lot_to_100_million
TraderIntelligence.Tests.Unit.VolumeConverterTests.Hundredths_comment_is_not_the_default
```

### 4.8 `TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests`

Four live Facts lock `QuantityNormalizer` passthrough. Twenty-one methods are skipped because `IQuantityConverter` is missing (A43 / A89 #45).

| Line | Kind | Method | Skip |
|---:|---|---|---|
| 20 | Fact | `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | no |
| 27 | Fact | `Mini_contract_same_lots_same_normalizer_output` | no |
| 33 | Fact | `Lots_convention_row_also_returns_source_lots` | no |
| 39 | Fact | `Respects_min_qty_and_step_as_last_stage` | no |
| 50 | Fact | `Never_passthrough_MT5_lots` | yes — A43 G7 / E01 |
| 65 | Theory | `Known_lot_to_OrderQty_examples(ulong ticks, int contractSize, string convention, double expected)` | yes — A43 §10.1 E01–E09 |
| 72 | Fact | `Mini_and_nano_contracts_differ` | yes — A43 E06 vs E01 |
| 78 | Fact | `Lots_convention_only_when_mapped` | yes — A43 E08 |
| 84 | Fact | `Mt5_ticks_scale_is_10000` | yes — A38 |
| 90 | Fact | `Decimal_not_double_for_0_0001_lot` | yes — A43 E05 |
| 96 | Fact | `Confidence_cannot_exceed_one` | yes — A43 E21 |
| 102 | Fact | `Close_uses_mapped_destination_qty` | yes — A43 §4.7 E32/E38 |
| 108 | Fact | `Dust_leftover_promotes_to_close` | yes — A43 E37 |
| 114 | Fact | `Unverified_dest_spec_rejects` | yes — A43 E39 |
| 120 | Fact | `Allocation_and_confidence_scale` | yes — A43 E16–E21 |
| 126 | Fact | `Dest_max_and_risk_caps_reduce` | yes — A43 E22–E26 |
| 132 | Fact | `Below_min_after_cap_rejects` | yes — A43 E14/E20/E25/E29 |
| 138 | Fact | `Margin_room_reduces_qty` | yes — A43 E27–E29 |
| 144 | Fact | `Increase_uses_incremental_volume` | yes — A43 E30–E31 |
| 150 | Fact | `Partial_reduce_is_fraction_of_dest` | yes — A43 E33–E37 |
| 156 | Fact | `Invalid_contract_size_rejects` | yes — A43 E40–E41 |
| 162 | Fact | `Missing_mapping_rejects` | yes — A43 E42–E43 |
| 168 | Fact | `Invalid_step_or_min_rejects` | yes — A43 E44–E46 |
| 174 | Fact | `Shadow_and_live_share_converter` | yes — A43 §6 |
| 180 | Fact | `Fix_worker_does_not_rescale` | yes — A43 §6 |

Skipped Theory rows (source only; **not** expanded by `--list-tests`):

| ticks | contractSize | convention | expected |
|---:|---:|---|---:|
| 1000 | 100 | BaseUnits | 10.00 |
| 100 | 100 | BaseUnits | 1.00 |
| 10000 | 100 | BaseUnits | 100.00 |
| 10 | 100 | BaseUnits | 0.10 |
| 1 | 100 | BaseUnits | 0.01 |
| 1000 | 10 | BaseUnits | 1.00 |
| 1000 | 1 | BaseUnits | 0.10 |
| 1000 | 100 | Lots | 0.10 |
| 100 | 100 | Lots | 0.01 |

Helpers (not tests): `DestBaseUnits1Oz`, `DestLots100Oz`.

Discovered (25 names; skipped Theory is one row):

```text
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_contract_same_lots_same_normalizer_output
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Lots_convention_row_also_returns_source_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Respects_min_qty_and_step_as_last_stage
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Never_passthrough_MT5_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Known_lot_to_OrderQty_examples
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_and_nano_contracts_differ
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Lots_convention_only_when_mapped
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mt5_ticks_scale_is_10000
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Decimal_not_double_for_0_0001_lot
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Confidence_cannot_exceed_one
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Close_uses_mapped_destination_qty
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Dust_leftover_promotes_to_close
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Unverified_dest_spec_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Allocation_and_confidence_scale
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Dest_max_and_risk_caps_reduce
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Below_min_after_cap_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Margin_room_reduces_qty
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Increase_uses_incremental_volume
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Partial_reduce_is_fraction_of_dest
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Invalid_contract_size_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Missing_mapping_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Invalid_step_or_min_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Shadow_and_live_share_converter
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Fix_worker_does_not_rescale
```

### 4.9 `TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests`

| Line | Kind | Method | Cases | Skip |
|---:|---|---|---:|---|
| 18 | Theory | `Floors_to_step(decimal sourceLots, decimal allocation, decimal expected)` | 7 (`FloorCases`) | no |
| 35 | Fact | `Floors_not_rounds_up_on_whole_step` | 1 | no |
| 43 | Fact | `Floors_partial_step_of_tenth` | 1 | no |
| 51 | Fact | `Below_min_returns_zero` | 1 | no |
| 58 | Fact | `Below_min_after_floor_returns_zero` | 1 | no |
| 65 | Fact | `Exact_min_is_kept` | 1 | no |
| 71 | Fact | `Above_max_caps` | 1 | no |
| 78 | Fact | `Exact_max_is_kept` | 1 | no |
| 84 | Fact | `Allocation_scales_before_step` | 1 | no |
| 92 | Fact | `Precision_truncates_toward_zero_after_step` | 1 | no |
| 99 | Fact | `Coarser_precision_than_step_can_break_step_alignment` | 1 | no |
| 106 | Fact | `Unaligned_max_is_returned_raw_not_re_floored` | 1 | no |
| 113 | Fact | `Allocation_greater_than_one_is_currently_accepted` | 1 | no |
| 122 | Theory | `Non_positive_source_lots_throws(decimal sourceLots)` | 3 | no |
| 132 | Theory | `Non_positive_allocation_throws(decimal allocation)` | 3 | no |
| 141 | Theory | `Non_positive_step_throws(decimal step)` | 2 | no |
| 149 | Fact | `Above_max_re_floors_to_step` | 1 | yes — A43 E23 |
| 156 | Fact | `Negative_precision_throws` | 1 | no |

`FloorCases` rows: `(0.333, 1, 0.33)`, `(0.339, 1, 0.33)`, `(0.335, 1, 0.33)`, `(1.999, 1, 1.99)`, `(0.019, 1, 0.01)`, `(0.10, 1, 0.10)`, `(1, 1/3, 0.33)`.

Throw InlineData: sourceLots `0`, `-0.01`, `-1`; allocation `0`, `-0.01`, `-1`; step `0`, `-0.01`.

Helpers (not tests): `DefaultSpec`, `FloorCases()`.

Discovered:

```text
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 0.333, allocation: 1, expected: 0.33)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 0.339, allocation: 1, expected: 0.33)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 0.335, allocation: 1, expected: 0.33)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 1.999, allocation: 1, expected: 1.99)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 0.019, allocation: 1, expected: 0.01)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 0.10, allocation: 1, expected: 0.10)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step(sourceLots: 1, allocation: 0.3333333333333333333333333333, expected: 0.33)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_not_rounds_up_on_whole_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_partial_step_of_tenth
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Below_min_returns_zero
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Below_min_after_floor_returns_zero
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Exact_min_is_kept
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Above_max_caps
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Exact_max_is_kept
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Allocation_scales_before_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Precision_truncates_toward_zero_after_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Coarser_precision_than_step_can_break_step_alignment
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Unaligned_max_is_returned_raw_not_re_floored
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Allocation_greater_than_one_is_currently_accepted
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_source_lots_throws(sourceLots: 0)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_source_lots_throws(sourceLots: -0.01)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_source_lots_throws(sourceLots: -1)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_allocation_throws(allocation: 0)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_allocation_throws(allocation: -0.01)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_allocation_throws(allocation: -1)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_step_throws(step: 0)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_step_throws(step: -0.01)
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Above_max_re_floors_to_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Negative_precision_throws
```

C17 recorded `Allocation_scales_before_step` as a **test-arithmetic red** (expects `0.10 × 0.10 = 0.10`; SUT returns `0.01`). This census does not re-run outcomes.

---

## 5. `tests/Integration` — every test method

Namespace: `TraderIntelligence.Tests.Integration`.

| # | Class | File | Methods | Cases | Skip |
|---:|---|---|---:|---:|---:|
| 1 | `SeedingAndStoreTests` | `SeedingAndStoreTests.cs` | 2 | 2 | 0 |
| 2 | `PlaceholderRemoved` | `UnitTest1.cs` | 1 | 1 | 0 |
| | **Integration total** | | **3** | **3** | **0** |

### 5.1 `TraderIntelligence.Tests.Integration.SeedingAndStoreTests`

| Line | Kind | Method | Seam |
|---:|---|---|---|
| 16 | Fact | `Demo_seed_discovers_groups_reconstructs_and_scores` | EF InMemory + `DemoSeeder` |
| 39 | Fact | `Deal_upsert_is_idempotent` | EF InMemory `AnyAsync` upsert |

Discovered:

```text
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Deal_upsert_is_idempotent
```

A90: InMemory does **not** count as §60 PostgreSQL / outbox / backfill proof. C16: both facts green as orchestration smoke.

### 5.2 `TraderIntelligence.Tests.Integration.PlaceholderRemoved`

| Line | Kind | Method |
|---:|---|---|
| 6 | Fact | `Integration_project_loads` |

Body: `Assert.True(true)`. **FALSE_GREEN.** Do not count toward §60.

Discovered:

```text
TraderIntelligence.Tests.Integration.PlaceholderRemoved.Integration_project_loads
```

---

## 6. Flat method-name index (71)

Alphabetical by FQN without theory arguments.

```text
TraderIntelligence.Tests.Integration.PlaceholderRemoved.Integration_project_loads
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Deal_upsert_is_idempotent
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores
TraderIntelligence.Tests.Unit.BaselineScorerTests.Martingale_after_losses_is_risk_blocked
TraderIntelligence.Tests.Unit.BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live
TraderIntelligence.Tests.Unit.BaselineScorerTests.Two_trades_remain_insufficient
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.ClOrdId_is_deterministic_and_unique_per_sequence
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Copy_intent_expires
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Disconnect_after_send_is_unknown_state
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Filled_report_is_terminal
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Quantity_normalizer_steps_and_min
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Unknown_ack_cannot_retry_new_order
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Allocation_and_confidence_scale
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Below_min_after_cap_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Close_uses_mapped_destination_qty
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Confidence_cannot_exceed_one
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Decimal_not_double_for_0_0001_lot
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Dest_max_and_risk_caps_reduce
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Dust_leftover_promotes_to_close
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Fix_worker_does_not_rescale
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Increase_uses_incremental_volume
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Invalid_contract_size_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Invalid_step_or_min_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Known_lot_to_OrderQty_examples
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Lots_convention_only_when_mapped
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Lots_convention_row_also_returns_source_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Margin_room_reduces_qty
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_and_nano_contracts_differ
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_contract_same_lots_same_normalizer_output
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Missing_mapping_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mt5_ticks_scale_is_10000
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Never_passthrough_MT5_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Partial_reduce_is_fraction_of_dest
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Respects_min_qty_and_step_as_last_stage
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Shadow_and_live_share_converter
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Unverified_dest_spec_rejects
TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected
TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes
TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Above_max_caps
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Above_max_re_floors_to_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Allocation_greater_than_one_is_currently_accepted
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Allocation_scales_before_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Below_min_after_floor_returns_zero
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Below_min_returns_zero
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Coarser_precision_than_step_can_break_step_alignment
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Exact_max_is_kept
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Exact_min_is_kept
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_not_rounds_up_on_whole_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_partial_step_of_tenth
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Floors_to_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Negative_precision_throws
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_allocation_throws
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_source_lots_throws
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Non_positive_step_throws
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Precision_truncates_toward_zero_after_step
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Unaligned_max_is_returned_raw_not_re_floored
TraderIntelligence.Tests.Unit.SmokeTests.Domain_assembly_loads
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Does_not_guess_venue_instrument_ids
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD
TraderIntelligence.Tests.Unit.TradeReconstructionTests.First_three_completed_xau_unlocks_early_score
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Ignores_balance_deals
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reconstructs_simple_round_trip
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reverse_inout_closes_then_opens_opposite
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Scale_in_and_partial_close
TraderIntelligence.Tests.Unit.VolumeConverterTests.Extended_scale_maps_one_lot_to_100_million
TraderIntelligence.Tests.Unit.VolumeConverterTests.Hundredths_comment_is_not_the_default
TraderIntelligence.Tests.Unit.VolumeConverterTests.Manager_scale_maps_0_10_lots_to_1000_native
```

---

## 7. What is **not** in these two trees

No other `*.cs` under `tests/Unit` or `tests/Integration` besides the 11 files above.

Named A89 / A27 / A90 classes **absent** from disk (not an exhaustive reprint — those reports remain the backlog):

- Unit: no `FixMessageParseBuildTests`, no `Mt5DealDeduplicationTests`, no `DrawdownCalculatorTests`, no `MfeMaeCalculatorTests`, no dedicated partial/scale-in/full-close/reversal classes (collapsed into `TradeReconstructionTests`).
- Integration: no `PostgreSqlMigrationTests`, no `Mt5BackfillRestartTests`, no `OutboxProcessingTests`, no QuickFIX / ER / reconcile classes.
- Projects: `tests/Replay` and `tests/Fix` do not exist.

`mt5-sdk/tests` C++ files are a different process and are **not** counted here.

---

## 8. Disposition

| Metric | Value |
|---|---|
| Distinct test methods | **71** (68 Unit + 3 Integration) |
| Discovered xUnit cases | **86** (83 + 3) |
| Skipped methods | **22** Unit (21 conversion + 1 step re-floor) |
| Placeholder / false-green | `PlaceholderRemoved.Integration_project_loads` |
| Smoke (not §60) | `SmokeTests.Domain_assembly_loads` |
| Product source changed | **No** |
| Test source changed | **No** |

Use C17 for Unit-vs-§60 scoring. Use C16 / A90 for Integration-seam scoring. This file is the method-name list those reviews bind to.
