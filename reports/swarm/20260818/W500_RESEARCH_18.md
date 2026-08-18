# W500_RESEARCH_18 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_18.md` |
| Slot | **18** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 18 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture §38 + executive change #10 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492); `A43_position_sizing.md`; `A38` (1 lot = 10_000); A89 #45/#47; A100 **G10** |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read full SUT (31/31 lines), both unit suites, `RiskEngine`, `ShadowCopyEngine`, `EfTradingStore` persist path, FIX session/logon/options/worker, DI, `VolumeConverter`. Cross-check YoPips volume math. No live `35=D`. |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. G7 / G10 remain FAIL. Live `OrderQty` is `SAFE_BY_ABSENCE`, not a proven converter.**

Two facts must not be collapsed:

| Question | Measured answer |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With `allocationFactor = 1` it **is** a lots passthrough. |
| Does any product path write that number as FIX tag 38? | **No.** Zero product callers. Zero `35=D` / `OrderQty` / `38=` builders. `RealCopyEnabled` forced **false**. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” If anyone later wires `Normalize(sourceLots, 1, dest)` into a NewOrderSingle, **0.10 MT5 lots becomes 0.10 dest qty**. A43 E01 / G7 requires **10.00** on a BaseUnits XAU book (`0.10 × 100 oz/lot ÷ 1 oz/unit`). `0.10` would be **100× too small** (or rejected as below min).

Passing unit Facts **lock the passthrough**. The binding “never passthrough” Fact is **skipped**.

**Risk to capital from this type: none.** No socket write of tag 38 exists. Manager fetch of Achiever + Starwave is independent of this class.

---

## 1. Architecture law (binding)

Executive change #10 and §38:

```text
Never blindly:
source 0.10 MT5 lots
=
destination OrderQty 0.10
```

Legal pipeline (`A43` §2 / architecture §38):

```text
source volume (ulong ticks)
    ↓  ÷ 10_000          VolumeConverter.Manager
source lots
    ↓  × contract_size
canonical ounces
    ↓  × allocation × confidence   (both in (0, 1])
allocated ounces
    ↓  ÷ dest unit (BaseUnits vs Lots)
pre-round dest qty
    ↓  floor to dest step / min / max
requested_quantity  →  FIX tag 38
```

`QuantityNormalizer` implements **only** the last floor (`sourceLots * allocation`, then dest grid). It has no ticks, no contract size, no convention, no confidence, no margin, no mapped close qty.

Known fixture that the SUT fails as a converter (A43 E01):

| Input | Required dest `OrderQty` (BaseUnits, 1 unit = 1 oz) | `Normalize(0.10, 1, DestBaseUnits1Oz)` |
|---|---:|---:|
| 1_000 ticks = 0.10 lots × 100 oz | **10.00** | **0.10** |

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is not on `InstrumentQuantitySpec`.

---

## 2. Measured SUT

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot) |
| Bytes / SHA-256 (prior D18/B17, content unchanged vs those quotes) | 1041 / `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` |
| Types | `InstrumentQuantitySpec` (record: Min, Max, Step, Precision); `QuantityNormalizer` (one method) |

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

Arithmetic (all `decimal`):

```text
raw   = sourceLots * allocationFactor
steps = Truncate(raw / dest.StepSize)     // toward zero
qty   = steps * dest.StepSize
qty   = Round(qty, Precision, ToZero)
qty < Min  → 0
qty > Max  → dest.MaxQuantity             // NOT FloorToStep(max)  [A43 E23 FAIL]
else       → qty
```

Absent from the type (required by A43):

| Input | On `InstrumentQuantitySpec` / `Normalize`? |
|---|---|
| MT5 ticks / `VolumeConverter` | **No** |
| `contract_size` | **No** |
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** (zero hits under `D:\Prop\src\**\*.cs`) |
| `IQuantityConverter` | **No** (tests + reports only) |
| `confidence_scale` | **No** |
| `spec_status` | **No** |
| margin / leverage / quote | **No** |
| mapped dest remaining (CLOSE/REDUCE) | **No** |
| `allocationFactor > 1` reject | **No** — `Normalize(1, 1.5, …)` returns `1.50` |

`VolumeConverter` is a **source** scale only (`native / 10_000 → lots`). It is not composed into `QuantityNormalizer`.

```1:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
// IMTDeal::Volume() scale = 10_000. ToLots(1000) = 0.10 lots.
// Does not know ounces, dest convention, or FIX tag 38.
```

---

## 3. Product callers: zero

`grep` of `QuantityNormalizer` / `InstrumentQuantitySpec` / `new QuantityNormalizer` / `.Normalize(` over product `*.cs` (`D:\Prop\src`, `D:\Prop\apps`; not `bin`/`obj`/`tests`):

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition only |
| `D:\Prop\src\Infrastructure` | **0** |
| `D:\Prop\apps` | **0** |
| DI `AddTraderIntelligence` | registers reconstructor + scorer; **not** `QuantityNormalizer` / `RiskEngine` / `ShadowCopyEngine` |
| `new QuantityNormalizer` | **tests only** |

Adjacent engines do **not** call it:

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity` (approve/reduce); reject → `0` | **No** |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` | **No** |
| `EfTradingStore` demo shadow persist | `RequestedQuantity = trade.MaxVolumeLots` (1:1 lots) | **No** |
| `BaselineScorer` | uses `MaxVolumeLots` for martingale/size-up **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` (lots, not dest units) | unused writer in product |
| `RiskDecision` entity | `AdjustedVolumeLots` | unused by `RiskEngine` record path |

Shadow persist (lots copied into a **SHADOW_ONLY** intent — not FIX):

```295:316:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
            var intent = new CopyIntent
            {
                // ...
                RequestedQuantity = trade.MaxVolumeLots,
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
            // SimulateEntry(..., trade.MaxVolumeLots, ...)
```

`RiskEngine` unit fixture also uses `RequestedQuantity = 0.10m` with `RealExecutionEnabled = false`. That is a **lots-shaped** number on a risk DTO, not tag 38.

---

## 4. Tests: passthrough is green; never-passthrough is skipped

### 4.1 Binding conversion suite — `SourceDestinationQuantityConversionTests.cs`

184 lines. Four **live** Facts **prove** `0.10 → 0.10` (the forbidden shortcut):

| Fact | Asserted output |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | `0.10` (mini must differ under A43 E06) |
| `Lots_convention_row_also_returns_source_lots` | `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | last-stage grid only |

**21** `[Fact]`/`[Theory]` methods are `Skip = "A43 … IQuantityConverter missing"` including:

- `Never_passthrough_MT5_lots` — *“0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.”*
- `Known_lot_to_OrderQty_examples` — 9-row table (ticks × contract × convention)
- `Mini_and_nano_contracts_differ`
- `Lots_convention_only_when_mapped`
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”*
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”*

Those skips are the **honest** G7 lock. Do not un-skip until `IQuantityConverter.Convert` exists.

### 4.2 Last-stage floor suite — `QuantityNormalizerStepMinMaxTests.cs`

162 lines. Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m` (passthrough on a 0.01 step). One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`).

`Allocation_greater_than_one_is_currently_accepted` documents `1 × 1.5 = 1.50` — A43 requires `allocationFactor ∈ (0, 1]`.

### 4.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out**.

### 4.4 Volume scale (source only)

`VolumeConverterTests`: `ToLots(1000) = 0.10`, scale `10_000`, not hundredths. Correct for Manager `Volume()`. Irrelevant to dest `OrderQty`.

---

## 5. Live FIX `OrderQty`: impossible in current code

`grep` `OrderQty` / `38=` / `35=D` / `(35, "D")` under `D:\Prop\src\Fix.CTrader` `*.cs`:

| Pattern | Hits |
|---|---|
| `OrderQty` / `38=` | **0** |
| `35=D` / `(35, "D")` | **0** |
| `(35, …)` present | Logon `"A"`, harness `"A"/"3"/"0"/"y"/"X"/"8"`, quote `"y"` / `"V"` only |

`CTraderFixSession.TryLogonAsync` writes **only** `35=A` (Logon). No NewOrderSingle method.

`CTraderFixLogonHostedService` after optional TLS logon:

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
```

DI pins the same bit:

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **`false`**.

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** — there is no builder to call.

`LiveRuntimeStatus.copyNote` when flag false: *“NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”*

A100 **G10** (`position sizing conversion is verified`) is **FAIL**. Checkbox stays `[ ]`.

---

## 6. Goal context: all groups / all traders; no live copy loss

Measured census (`LIVE_MANAGER_FETCH_MEASURED.md`, `CREDENTIALS_AND_COPY_STATUS.md`; values not secrets):

| Broker | Groups | Traders | Positions | Connect |
|---|---:|---:|---:|---|
| Achiever | 8 | 6512 | 1506 | HTTP proxy |
| StarwaveFX | 10 | 1948 | 478 | direct |
| **Total** | **18** | **8460** | **1984** | |

`QuantityNormalizer` is **not** on the Manager fetch path. Completeness of `GroupRequestArray` / `UserRequestArray` is a different slot. This class cannot drop or invent traders.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog  →  reconstruct / score  →  SHADOW_ONLY CopyIntent (lots 1:1)
                                         ✗  QuantityNormalizer unused
                                         ✗  IQuantityConverter missing
                                         ✗  no 35=D / tag 38
                                         ✗  RealCopyEnabled = false
```

Fetching 8460 logins does **not** arm send. Shadow `RequestedQuantity = MaxVolumeLots` is a **demo/ledger** 1:1 lots copy. It is still not FIX. It **would** be a G7 defect the day a sender is added without `IQuantityConverter`.

---

## 7. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty`.

Display/JSON uses classic Manager scale:

```text
deal.volume / 10000.0     export_controller, journal_controller, trade_service, worker_service
pos.volume  / 10000.0     symbol_controller, trade_service
```

`mt5_manager.cpp` copies `deal->Volume()` / `pos->Volume()` and `DealerSend` writes `request->Volume(volume)` in **native ticks**. That is MT5→MT5 dealer volume, **not** cTrader tag 38. Do not copy YoPips `volume/10000` into a FIX builder.

---

## 8. Spec vs code (do not rubber-stamp)

| ID | Spec | Measured | Stance |
|---|---|---|---|
| G7 / E01 | 0.10 lots × 100 oz → BaseUnits `10.00` | `Normalize` → `0.10` | **FAIL** passthrough |
| E06 | same lots × mini 10 oz → `1.00` | still `0.10` | **FAIL** |
| E08 | Lots convention is the **only** `0.10→0.10` | always passthrough | **FAIL** |
| §4.5 | floor, never ceil | `Truncate` + `ToZero` | **PASS** last-stage (positive qty) |
| E14 | below min → do not send | returns `0m` (not `SIZE_BELOW_MIN`) | **PARTIAL** |
| E23 | cap then re-floor to step | returns raw `MaxQuantity` | **FAIL** (skipped test) |
| E21 | `confidence_scale ≤ 1` | no input | **MISSING** |
| CLOSE/REDUCE | dest remaining, not source × allocation | N/A | **MISSING** |
| A43 §6 | Risk + Shadow + FIX share one converter | unused + no NOS | **MISSING** |
| §68 G10 | conversion verified | skipped fixtures | **FAIL** |
| Live tag 38 | only after converter + gates | no builder | **SAFE_BY_ABSENCE** |

---

## 9. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** tick G10 or authorize `REAL_COPY_EXECUTION_ENABLED=true`.
- Does **not** print passwords, FIX password, or account secrets.
- Does **not** add a `35=D` sender.

---

## 10. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire it from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = `requested_quantity` only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 11. Slot-18 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`) |
| Blind lots → FIX `OrderQty` on the wire | **NO** (unused + no `35=D`) |
| `IQuantityConverter` | **MISSING** |
| Product callers | **0** |
| G7 / G10 | **FAIL** |
| Live send | **OFF** (`SAFE_BY_ABSENCE`) |
| Capital at risk from this class | **none** |
| Manager census (context) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |

**One-line:** `QuantityNormalizer` would blindly passthrough MT5 lots as dest qty, but it never reaches FIX; no live `OrderQty` can be sent.

[DO NOT] Treat unused last-stage floor as a verified §38 converter.
[DO NOT] Un-skip G7 before `IQuantityConverter`.
[DO NOT] Write `38=` from `MaxVolumeLots` or `Normalize(lots, 1, dest)`.
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
