# E004 — Test projects: passing vs skipped (measured)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\E004_tests.md` |
| Agent | E004 (test-project pass/skip census) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:48:18+05:30 → 13:48:23+05:30 |
| Assigned | Read test projects and list passing vs skipped. Write this file. Do **not** modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| HEAD | `398a142` (`docs: add PNG fallback and update README; add conversion script`) |
| SDK | .NET 8.0.424 / xUnit 2.5.3.1 / VSTest adapter 2.5.3.1 |
| Host | `ADMIN@DESKTOP-FQPFPKE` |

**Companions (do not treat as this pass):** A09 / A10 (empty-scaffold, stale), A18 (C++ contract pin, not this run), A27 (required-class backlog), A89 / A90 (class backlog), B08 (29/28/1, stale), C17 (83/60/1/22, stale), C52 (29/29 prediction, not a run), D09 (method census without `DealReasonTests` / cancel fact), D33–D37 (class close-reads).

This file is a **measured pass/skip list**. A listed PASS is not Architecture §60 coverage. A `[Fact(Skip=…)]` is not a fail and is not coverage.

Scratch logs (not the report): `D:\Prop\reports\swarm\20260818\_tmp_e004\` (`unit.trx`, `integration.trx`, console captures).

---

## 0. Verdict

**Both solution test projects completed with exit 0. Combined: 89 discovered cases, 67 passed, 22 skipped, 0 failed.**

| Project | Path | Total | Passed | Skipped | Failed | Exit |
|---|---|---:|---:|---:|---:|---:|
| `TraderIntelligence.Tests.Unit` | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | **86** | **64** | **22** | **0** | **0** |
| `TraderIntelligence.Tests.Integration` | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | **3** | **3** | **0** | **0** | **0** |
| **Solution test surface** | `Mt5TraderIntelligence.sln` `tests` folder | **89** | **67** | **22** | **0** | **0** |

TRX:

| Lane | File | Start | Finish | Outcome |
|---|---|---|---|---|
| Unit | `_tmp_e004\unit.trx` | 2026-08-18T13:48:20.2308598+05:30 | 13:48:20.5760630+05:30 | Completed |
| Integration | `_tmp_e004\integration.trx` | 2026-08-18T13:48:22.4397606+05:30 | 13:48:23.4691560+05:30 | Completed |

Unit TRX counters: `total=86 executed=64 passed=64 failed=0`. The 22 skips are `NotExecuted` rows (xUnit `Skip=`), not TRX `notExecuted` counter (that field stays 0). Console summary is the one to quote:

```text
Test Run Successful.
Total tests: 86
     Passed: 64
    Skipped: 22
```

```text
Test Run Successful.
Total tests: 3
     Passed: 3
```

**All 22 skips live in two Unit files.** Integration has zero `Skip=`. There is no red fact on this worktree.

Do **not** treat 67/89 green as §60, §69, or §70. C17’s single red (`Allocation_scales_before_step` arithmetic) is **stale** — that fact now expects `0.10 × 0.10 = 0.01` and **passed**. B08’s averaging-down red is **stale** — `Scale_in_and_partial_close` **passed**.

---

## 1. Method

Read, then execute. Product source was not edited.

| Source | Path |
|---|---|
| Solution | `D:\Prop\Mt5TraderIntelligence.sln` (both test projects nested under `tests`) |
| Unit project | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| Integration project | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| Unit sources | 10 `*.cs` under `tests\Unit` excluding `bin/` `obj/` |
| Integration sources | 2 `*.cs` under `tests\Integration` excluding `bin/` `obj/` |
| C++ suite | `D:\Prop\mt5-sdk\tests\` + `CMakeLists.txt` (`MT5SDK_BUILD_TESTS` default **OFF**) |
| Web | `D:\Prop\apps\web\package.json` — no `test` script, no Jest/Vitest |

Commands this pass (read-only on product; rebuild of already-present test assemblies):

```text
dotnet test D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj --nologo --verbosity normal --logger "trx;LogFileName=unit.trx" --results-directory D:\Prop\reports\swarm\20260818\_tmp_e004

dotnet test D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj --nologo --verbosity normal --logger "trx;LogFileName=integration.trx" --results-directory D:\Prop\reports\swarm\20260818\_tmp_e004
```

Names below are TRX `testName` values (fully qualified, theory args expanded). Skip reasons are the `Skip = "…"` strings on the attributes.

---

## 2. What is a test project (and what is not)

### 2.1 In the .sln (runnable xUnit)

| Project | TFM | Packages | Project refs | `[Skip]` |
|---|---|---|---|---|
| `TraderIntelligence.Tests.Unit` | net8.0 | xUnit 2.5.3, FluentAssertions 6.12.0, Moq 4.20.70, coverlet 6.0.0, Microsoft.NET.Test.Sdk 17.8.0 | Domain, Application, Fix.CTrader | 22 cases |
| `TraderIntelligence.Tests.Integration` | net8.0 | xUnit 2.5.3, FluentAssertions 6.12.0, EF InMemory 8.0.4, coverlet 6.0.0, Microsoft.NET.Test.Sdk 17.8.0 | Domain, Application, Infrastructure, Fix.CTrader, Mt5 | 0 |

No `[Trait]`. No `IClassFixture`. No Testcontainers. No Replay/Fix projects.

### 2.2 On disk, not in the .sln, not executed this pass

| Tree | Role | This pass |
|---|---|---|
| `D:\Prop\mt5-sdk\tests\*.cpp` | 4 hermetic C++ binaries + 2 live probes | **Not built.** `MT5SDK_BUILD_TESTS=OFF` (default). No `mt5-sdk\build`. CMake 4.4.0 is installed. |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\` | Vendored MetaQuotes samples | **Not** project tests. |

### 2.3 Missing / empty

| Path | Status |
|---|---|
| `D:\Prop\tests\Replay` | **Absent** (A27 proposed) |
| `D:\Prop\tests\Fix` | **Absent** (A27 / §61 proposed) |
| `D:\Prop\tests\Risk` | **Absent** (architecture §66 folder; risk lives in Unit) |
| `D:\Prop\apps\web` | Vite/React app; **no** test runner |

---

## 3. On-disk test sources (SHA-256 this pass)

| Bytes | SHA-256 | Path |
|---:|---|---|
| 1113 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | `tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| 224 | `6B1A127F1810FF0A0E1C07F0913A415CBE61D31FE56DF3BD46378C97EB77E6A5` | `tests\Unit\UnitTest1.cs` (`SmokeTests`) |
| 2414 | `61E34A07D76B104CF5D8B818242104522A8B59D12422C5EF4555C2447308D408` | `tests\Unit\BaselineScorerTests.cs` |
| 1333 | `2B660B79B2D9BF812F637AF5200894FBA74536E7AB28439F8187CA956BAEB0E9` | `tests\Unit\DealReasonTests.cs` |
| 2144 | `CA24E357C5FCFDAAA436F2628E9B47042355DDF19D4D915DC9284FEC0E6B9046` | `tests\Unit\ExecutionAndSizingTests.cs` |
| 2909 | `7B95236489E2FE169BFC8E9F57A9E2C89F6F5F047116D9DC82CFA8820FB2DF51` | `tests\Unit\RiskEngineTests.cs` |
| 896 | `EB26D062B1574F218D60D16578B8243411C5996FA43EE7CD616485932CCEFF33` | `tests\Unit\SymbolNormalizerTests.cs` |
| 4895 | `CB223DDE3D8FC90BB39C15C8369640B6164A09B7FB30523BF40D8A0BA8E78B9D` | `tests\Unit\TradeReconstructionTests.cs` |
| 791 | `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5` | `tests\Unit\VolumeConverterTests.cs` |
| 7344 | `AA1FA307A0D81A8A7978106397BA7994BE9D73981CC82C3967969C0AB1C08A9B` | `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` |
| 5174 | `63D2691DDD89CFB09DCAF2868F1F1FABA78459F2C644A0F2EA89CC7527F8FA05` | `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` |
| 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | `tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| 162 | `49671A3C7C367ED87C7711E2204865AA2ABB8A7A5783AD785CD66A1F6DA7F4D6` | `tests\Integration\UnitTest1.cs` (`PlaceholderRemoved`) |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `tests\Integration\SeedingAndStoreTests.cs` |

Git: Unit/Integration sources are **untracked** except `tests\Integration\TraderIntelligence.Tests.Integration.csproj` (`M`).

D09 hashed `TradeReconstructionTests` as 3939 / `5D99BA22…`. Current file is **4895** / `CB223DDE…` (adds `Canceled_deal_on_a_position_excludes_it_from_first_three`). D09 omitted `DealReasonTests.cs` entirely.

---

## 4. Class roll-up (measured)

Namespace roots: `TraderIntelligence.Tests.Unit` and `TraderIntelligence.Tests.Integration`.

| # | Class | File | Cases | Passed | Skipped | Failed |
|---:|---|---|---:|---:|---:|---:|
| 1 | `SmokeTests` | `UnitTest1.cs` | 1 | 1 | 0 | 0 |
| 2 | `BaselineScorerTests` | `BaselineScorerTests.cs` | 3 | 3 | 0 | 0 |
| 3 | `DealReasonTests` | `DealReasonTests.cs` | 2 | 2 | 0 | 0 |
| 4 | `ExecutionAndSizingTests` | `ExecutionAndSizingTests.cs` | 6 | 6 | 0 | 0 |
| 5 | `RiskEngineTests` | `RiskEngineTests.cs` | 5 | 5 | 0 | 0 |
| 6 | `SymbolNormalizerTests` | `SymbolNormalizerTests.cs` | 6 | 6 | 0 | 0 |
| 7 | `TradeReconstructionTests` | `TradeReconstructionTests.cs` | 6 | 6 | 0 | 0 |
| 8 | `VolumeConverterTests` | `VolumeConverterTests.cs` | 3 | 3 | 0 | 0 |
| 9 | `Normalization.SourceDestinationQuantityConversionTests` | `Normalization\SourceDestinationQuantityConversionTests.cs` | 25 | 4 | **21** | 0 |
| 10 | `Sizing.QuantityNormalizerStepMinMaxTests` | `Sizing\QuantityNormalizerStepMinMaxTests.cs` | 29 | 28 | **1** | 0 |
| | **Unit subtotal** | | **86** | **64** | **22** | **0** |
| 11 | `SeedingAndStoreTests` | `SeedingAndStoreTests.cs` | 2 | 2 | 0 | 0 |
| 12 | `PlaceholderRemoved` | `Integration\UnitTest1.cs` | 1 | 1 | 0 | 0 |
| | **Integration subtotal** | | **3** | **3** | **0** | **0** |
| | **Combined** | | **89** | **67** | **22** | **0** |

Attribute mix (distinct methods, not expanded theories):

| Attribute | Unit | Integration |
|---|---:|---:|
| `[Fact]` (no Skip) | 46 | 3 |
| `[Fact(Skip=…)]` | 20 | 0 |
| `[Theory]` (no Skip) | 5 | 0 |
| `[Theory(Skip=…)]` | 1 | 0 |
| **Distinct methods** | **72** | **3** |

xUnit **does not** expand the skipped `Known_lot_to_OrderQty_examples` InlineData (9 rows stay **1** discovered skip). Active Theories **are** expanded (`Maps_known_aliases_to_XAUUSD` ×5, `Floors_to_step` ×7, throw theories ×3/3/2).

---

## 5. SKIPPED (22) — complete list

All 22 are explicit `Skip=` because `IQuantityConverter` / dest re-floor is not implemented. Bodies that `Assert.Fail` never run.

### 5.1 `SourceDestinationQuantityConversionTests` — 21 skipped

| Method | Kind | Skip reason (attribute text) |
|---|---|---|
| `Never_passthrough_MT5_lots` | Fact | A43 G7 / E01: IQuantityConverter missing. 0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10. |
| `Known_lot_to_OrderQty_examples` | Theory (9 InlineData, **1** skip row) | A43 §10.1 E01–E09: IQuantityConverter missing. |
| `Mini_and_nano_contracts_differ` | Fact | A43 E06 vs E01: mini contract_size=10 must yield 1.00 oz, not the same qty as contract_size=100. |
| `Lots_convention_only_when_mapped` | Fact | A43 E08: Lots convention is the only mapping where 0.10 lots may equal OrderQty 0.10. |
| `Mt5_ticks_scale_is_10000` | Fact | A38: source ticks / 10_000 = lots. Converter not implemented. |
| `Decimal_not_double_for_0_0001_lot` | Fact | A43 E05: 1 tick × 100 oz = 0.01 dest; must stay decimal 0.01m. |
| `Confidence_cannot_exceed_one` | Fact | A43 E21: confidence_scale > 1 is illegal. QuantityNormalizer has no confidence input. |
| `Close_uses_mapped_destination_qty` | Fact | A43 §4.7 E32/E38: REDUCE/CLOSE uses mapped dest qty, not source lots × allocation. |
| `Dust_leftover_promotes_to_close` | Fact | A43 E37: leftover < dest min promotes REDUCE to CLOSE. |
| `Unverified_dest_spec_rejects` | Fact | A43 E39: Unverified dest spec must reject. InstrumentQuantitySpec has no spec_status. |
| `Allocation_and_confidence_scale` | Fact | A43 E16–E21: allocation × confidence before dest step. QuantityNormalizer has no confidence input. |
| `Dest_max_and_risk_caps_reduce` | Fact | A43 E22–E26: dest max and risk caps reduce then re-quantize. |
| `Below_min_after_cap_rejects` | Fact | A43 E14/E20/E25/E29: below min after cap is REJECT, not a sendable 0. |
| `Margin_room_reduces_qty` | Fact | A43 E27–E29: margin room reduces qty. No quote/leverage on QuantityNormalizer. |
| `Increase_uses_incremental_volume` | Fact | A43 E30–E31: INCREASE uses incremental source ticks, not position max_volume. |
| `Partial_reduce_is_fraction_of_dest` | Fact | A43 E33–E37: REDUCE is a fraction of mapped dest qty. |
| `Invalid_contract_size_rejects` | Fact | A43 E40–E41: contract_size <= 0 / NaN rejects. |
| `Missing_mapping_rejects` | Fact | A43 E42–E43: missing dest spec or REDUCE link rejects. |
| `Invalid_step_or_min_rejects` | Fact | A43 E44–E46: step 0 / step 0.001 / min not multiple of step reject the spec. |
| `Shadow_and_live_share_converter` | Fact | A43 §6: shadow and live must call the same converter. |
| `Fix_worker_does_not_rescale` | Fact | A43 §6: FIX worker must not rescale requested_quantity. |

Skipped Theory rows (source only; **not** expanded by the runner):

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

### 5.2 `QuantityNormalizerStepMinMaxTests` — 1 skipped

| Method | Kind | Skip reason |
|---|---|---|
| `Above_max_re_floors_to_step` | Fact | A43 E23: after dest max, q must be FloorToStep(max, step). Today Normalize returns raw MaxQuantity. |

Sibling fact `Unaligned_max_is_returned_raw_not_re_floored` **passes** and locks the current (wrong-vs-A43) behavior. The skip is the desired contract; the passer documents the defect.

### 5.3 TRX fully-qualified skip names (22)

```text
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
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Margin_room_reduces_qty
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_and_nano_contracts_differ
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Missing_mapping_rejects
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mt5_ticks_scale_is_10000
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Never_passthrough_MT5_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Partial_reduce_is_fraction_of_dest
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Shadow_and_live_share_converter
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Unverified_dest_spec_rejects
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Above_max_re_floors_to_step
```

---

## 6. PASSED (67) — complete list

### 6.1 Unit — 64

#### `SmokeTests` (1)

```text
TraderIntelligence.Tests.Unit.SmokeTests.Domain_assembly_loads
```

Assembly-load smoke. Not §60.

#### `BaselineScorerTests` (3)

```text
TraderIntelligence.Tests.Unit.BaselineScorerTests.Two_trades_remain_insufficient
TraderIntelligence.Tests.Unit.BaselineScorerTests.Three_disciplined_winners_go_to_shadow_not_live
TraderIntelligence.Tests.Unit.BaselineScorerTests.Martingale_after_losses_is_risk_blocked
```

#### `DealReasonTests` (2) — new vs D09 / C17

```text
TraderIntelligence.Tests.Unit.DealReasonTests.Rollover_is_not_a_trader_lifecycle_deal
TraderIntelligence.Tests.Unit.DealReasonTests.Client_buy_still_counts
```

#### `ExecutionAndSizingTests` (6)

```text
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Unknown_ack_cannot_retry_new_order
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Disconnect_after_send_is_unknown_state
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Filled_report_is_terminal
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Quantity_normalizer_steps_and_min
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.ClOrdId_is_deterministic_and_unique_per_sequence
TraderIntelligence.Tests.Unit.ExecutionAndSizingTests.Copy_intent_expires
```

#### `RiskEngineTests` (5)

```text
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_quote_rejects_open
TraderIntelligence.Tests.Unit.RiskEngineTests.Real_flag_false_never_allows_fix_send
TraderIntelligence.Tests.Unit.RiskEngineTests.Stop_new_execution_blocks_opens_not_closes
TraderIntelligence.Tests.Unit.RiskEngineTests.Unreconciled_venue_blocks_new_exposure
TraderIntelligence.Tests.Unit.RiskEngineTests.Stale_signal_rejected
```

#### `SymbolNormalizerTests` (6)

```text
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD.")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSDm")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "XAUUSD.a")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Maps_known_aliases_to_XAUUSD(source: "GOLD")
TraderIntelligence.Tests.Unit.SymbolNormalizerTests.Does_not_guess_venue_instrument_ids
```

#### `TradeReconstructionTests` (6)

```text
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reconstructs_simple_round_trip
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Scale_in_and_partial_close
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Reverse_inout_closes_then_opens_opposite
TraderIntelligence.Tests.Unit.TradeReconstructionTests.First_three_completed_xau_unlocks_early_score
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Canceled_deal_on_a_position_excludes_it_from_first_three
TraderIntelligence.Tests.Unit.TradeReconstructionTests.Ignores_balance_deals
```

`Canceled_deal_on_a_position_excludes_it_from_first_three` is new vs D09 (5 facts / 3939 B).

#### `VolumeConverterTests` (3)

```text
TraderIntelligence.Tests.Unit.VolumeConverterTests.Manager_scale_maps_0_10_lots_to_1000_native
TraderIntelligence.Tests.Unit.VolumeConverterTests.Extended_scale_maps_one_lot_to_100_million
TraderIntelligence.Tests.Unit.VolumeConverterTests.Hundredths_comment_is_not_the_default
```

#### `SourceDestinationQuantityConversionTests` — **4 live** (passthrough locks, not A43 converter)

```text
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Mini_contract_same_lots_same_normalizer_output
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Lots_convention_row_also_returns_source_lots
TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests.Respects_min_qty_and_step_as_last_stage
```

These four prove `QuantityNormalizer` **does** passthrough 0.10 lots. They do **not** prove lots→ounces→OrderQty. That is why the other 21 methods are skipped.

#### `QuantityNormalizerStepMinMaxTests` — 28 live

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
TraderIntelligence.Tests.Unit.Sizing.QuantityNormalizerStepMinMaxTests.Negative_precision_throws
```

`Allocation_scales_before_step` **passed** (expects `0.01m` for `0.10 × 0.10`). C17 recorded this as the only Unit red; that measurement is stale.

### 6.2 Integration — 3

```text
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores
TraderIntelligence.Tests.Integration.SeedingAndStoreTests.Deal_upsert_is_idempotent
TraderIntelligence.Tests.Integration.PlaceholderRemoved.Integration_project_loads
```

Seam: EF **InMemory** + `DemoSeeder` / `EfTradingStore`. Not PostgreSQL, not outbox, not MT5 backfill. A90 / D37: InMemory does **not** count as §60 integration.

`PlaceholderRemoved.Integration_project_loads` is `Assert.True(true)`. **FALSE_GREEN.** Do not count toward §60.

---

## 7. Failed

**None.** `failed=0` on both TRX files. Exit 0 on both projects.

Prior reds that must not be recycled:

| Old report | Case | Then | Now |
|---|---|---|---|
| B08 (29-case Unit) | `TradeReconstructionTests.Scale_in_and_partial_close` | Failed (`WasAveragedDown` false) | **Passed** |
| C17 (83-case Unit) | `QuantityNormalizerStepMinMaxTests.Allocation_scales_before_step` | Failed (test expected `0.10`) | **Passed** (expects `0.01`) |

---

## 8. Drift vs earlier censuses

| Report | Unit total / P / F / S | Why stale |
|---|---|---|
| B08 | 29 / 28 / 1 / 0 | Pre-`Normalization/` / `Sizing/` / `DealReason` / cancel fact |
| C17 | 83 / 60 / 1 / 22 | Missing `DealReasonTests` (2) + cancel fact (1); `Allocation_scales_before_step` red |
| D09 | 83 discovered / methods 68 Unit | Same gap: no `DealReasonTests`; recon file 3939 B |
| D33 | recon class 5 facts | File now 6 facts / 4895 B |
| **E004 (this)** | **86 / 64 / 0 / 22** | Measured 13:48 +05:30 |

Arithmetic check: C17 83 + 2 DealReason + 1 cancel = **86**. The old fail flipped to pass → 60 + 1 + 2 + 1 = **64** passed. Skip count unchanged at **22**.

---

## 9. C++ `mt5-sdk/tests` (not in this `dotnet test`)

CMake (`D:\Prop\mt5-sdk\CMakeLists.txt`): default `MT5SDK_BUILD_TESTS=OFF`, `MT5SDK_BUILD_PROBES=OFF`. No `D:\Prop\mt5-sdk\build`. **0 C++ tests executed this pass.** They are not xUnit skips; they are an unbuilt opt-in suite.

| File | Bytes | SHA-256 | CMake | CTest? | Network |
|---|---:|---|---|---|---|
| `mt5_time_window_test.cpp` | 3839 | `F0EC2A4E48D9426C90CA62F6B5D5DA3131A22D612089E153232F0FD4619BD900` | `MT5SDK_BUILD_TESTS=ON` | yes | none |
| `mt5_http_client_pool_timeout_test.cpp` | 17629 | `E600319D752B939DFFDFB42F6840CDFA2DC128CA135BFB352F0E99ED46BD3D14` | `MT5SDK_BUILD_TESTS=ON` | yes | none (fake curl) |
| `mt5_news_calendar_test.cpp` | 3398 | `414282DDE22EA23B423FC5338730DF798E64D3685A1166D336EC7CBB82D831E9` | `MT5SDK_BUILD_TESTS=ON` | yes | none |
| `mt5_ledger_store_test.cpp` | 1094 | `061D87EE4639C6A531EFFDFEBB5D206F09638A025E6652EEE11DBE0375CADD00` | TESTS **and** `MT5SDK_WITH_POSTGRES=ON` | yes if built | none |
| `mt5_group_probe.cpp` | 5688 | `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` | `MT5SDK_BUILD_PROBES=ON` + WIN32 | **no** | live Manager API |
| `mt5_news_calendar_probe.cpp` | 7733 | `006BB24D4F16AAE6D7326461D87241326715F034C681EB3745660CFAF14C3874` | `MT5SDK_BUILD_PROBES=ON` + WIN32 | **no** | live Manager / HTTP |

A18 remains the contract pin for what those binaries lock. This file does **not** claim they pass.

---

## 10. Honesty / non-claims

- **67 passed is not §60.** A27 wants 77 named classes across Unit / Integration / Replay / FIX. On disk: 12 classes, two of them placeholders (`SmokeTests`, `PlaceholderRemoved`).
- **22 skipped is the A43 converter hole**, not flakiness. Unskip only after `IQuantityConverter` exists.
- **Integration 3/3 is InMemory orchestration smoke**, not PostgreSQL migrations, not outbox, not live MT5, not FIX.
- **`PlaceholderRemoved.Integration_project_loads`** is `Assert.True(true)`.
- **No Replay project. No FIX harness project. No web tests.**
- **C++ suite not run.** Default CMake leaves it off.
- Product source was **not** modified. Test source was **not** modified.

---

## 11. Disposition

| Metric | Value |
|---|---|
| Runnable .sln test projects | **2** |
| Discovered xUnit cases | **89** (86 + 3) |
| **Passed** | **67** (64 + 3) |
| **Skipped** | **22** (all Unit; 21 converter + 1 dest re-floor) |
| **Failed** | **0** |
| Distinct Unit methods | **72** |
| Distinct Integration methods | **3** |
| False-green placeholder | `PlaceholderRemoved.Integration_project_loads` |
| C++ hermetic binaries executed | **0** |
| Product source changed | **No** |
| Test source changed | **No** |

Use this file for **current pass vs skip names**. Use C17/D33–D37 for “does a green fact prove the architecture bullet.” Use A27/A89/A90 for the missing class backlog.
