# W500_RESEARCH_38 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_38.md` |
| Slot | **38** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent re-read of product + tests + FIX + catalog; no live Manager/TLS re-attach) |
| Agent | W500 research subagent, slot 38 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture §38 + executive #10 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492); `A43_position_sizing.md`; `A38` (1 lot = 10_000); A89 #45/#47; A100 **G7/G10** |
| Sibling re-measure | `W500_RESEARCH_18.md` (same topic, earlier slot). This file is an independent slot-38 re-read, not a copy. |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** |
| Live `35=D` this pass | **No** (builder absent; flag forced false) |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full SUT (31/31). Conversion tests. `VolumeConverter`, `TradeReconstructor`, `ShadowCopyEngine`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `CTraderFixSession`, `CTraderFixLogonHostedService`, DI, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`. No password values. |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. G7 / G10 remain FAIL. Live FIX `OrderQty` is `SAFE_BY_ABSENCE`, not a proven converter.**

Do not collapse these two facts:

| Question | Measured this slot |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With `allocationFactor = 1` it **is** a lots passthrough (`0.10 → 0.10`, **not** `10.00`). |
| Does any product path write that number as FIX tag 38? | **No.** Zero product callers. Zero `35=D` / `OrderQty` / `38=` builders in `Fix.CTrader`. `LiveRuntimeStatus.RealCopyEnabled` forced **false** in DI and again after logon. `FEATURE_COPY_TRADING_ENABLED` hardcoded **false**. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” If anyone later wires `Normalize(sourceLots, 1, dest)` into a NewOrderSingle, **0.10 MT5 lots becomes 0.10 dest qty**. A43 E01 / G7 requires **10.00** on a BaseUnits XAU book (`0.10 lots × 100 oz/lot ÷ 1 oz/unit`). `0.10` would be **100× too small** (or rejected as below min).

Passing unit Facts **lock the passthrough**. The binding “never passthrough” Fact is **`[Fact(Skip = …)]`**.

**Risk to capital from this type: none.** No socket write of tag 38 exists. Manager fetch of Achiever + Starwave is independent of this class.

---

## 1. Architecture law (binding)

Executive change #10 (architecture L100) and §38 (L1457–1464):

```text
Never blindly:
source 0.10 MT5 lots
=
destination OrderQty 0.10
```

Legal pipeline (`A43` §2 / architecture §38 L1468–1475):

```text
source volume (ulong ticks)
    ↓  ÷ 10_000                 VolumeConverter.Manager
source lots
    ↓  × source_contract_size
canonical ounces
    ↓  × allocation × confidence   (both in (0, 1])
allocated ounces
    ↓  ÷ dest unit (BaseUnits vs Lots)
pre-round dest qty
    ↓  floor to dest step / min / max     ← QuantityNormalizer is ONLY this last box
requested_quantity  →  FIX tag 38
```

`QuantityNormalizer` implements **only** the last floor (`sourceLots * allocation`, then dest grid). It has no ticks, no contract size, no convention, no confidence, no margin, no mapped close qty.

Known fixture the SUT fails as a converter (A43 E01):

| Input | Required dest `OrderQty` (BaseUnits, 1 unit = 1 oz) | `Normalize(0.10, 1, DestBaseUnits1Oz)` |
|---|---:|---:|
| 1_000 ticks = 0.10 lots × 100 oz | **10.00** | **0.10** |

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is **not** on `InstrumentQuantitySpec`.

Go-live checkbox this owns (`§68` / A100 G10):

```text
[ ] position sizing conversion is verified
```

Still **unchecked**.

---

## 2. Measured SUT

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot) |
| Types | `InstrumentQuantitySpec` (record: `MinQuantity`, `MaxQuantity`, `StepSize`, `Precision`); `QuantityNormalizer` (one method) |
| Content vs D18 / B17 / W500_18 quoted source | **line-identical** (same 31 lines, same arithmetic) |
| Prior measured SHA-256 (D18, not re-hashed this worker) | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` (1041 B) |

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
steps = Truncate(raw / dest.StepSize)     // toward zero — never ceil
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
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** — 0 hits under `D:\Prop\src\**\*.cs` |
| `IQuantityConverter` type | **No** — name exists only in **skipped** test strings |
| `confidence_scale` | **No** |
| `spec_status` | **No** |
| margin / leverage / quote | **No** |
| mapped dest remaining (CLOSE/REDUCE) | **No** |
| `allocationFactor > 1` reject | **No** — test `Allocation_greater_than_one_is_currently_accepted` locks `1 × 1.5 = 1.50` |

`VolumeConverter` is a **source** scale only (`native / 10_000 → lots`). It is **not** composed into `QuantityNormalizer`.

```1:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
// IMTDeal::Volume() scale = 10_000. ToLots(1000) = 0.10 lots.
// Does not know ounces, dest convention, or FIX tag 38.
```

`InstrumentQuantitySpec` product definition hits: **this file only**. All other C# hits are tests.

---

## 3. Product callers: zero

`grep` this slot of `QuantityNormalizer` / `new QuantityNormalizer` / `InstrumentQuantitySpec` over product `*.cs` (`D:\Prop\src`, `D:\Prop\apps`; not `bin`/`obj`/`tests`):

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition only |
| `D:\Prop\src\Infrastructure` | **0** |
| `D:\Prop\src\Application` | **0** |
| `D:\Prop\src\Fix.CTrader` | **0** |
| `D:\Prop\src\Mt5` | **0** |
| `D:\Prop\apps` | **0** |
| DI `AddTraderIntelligence` | registers reconstructor + scorer; **not** `QuantityNormalizer` / `RiskEngine` / `ShadowCopyEngine` |
| `new QuantityNormalizer` | **tests only** (`ExecutionAndSizingTests`, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests`) |

Adjacent engines do **not** call it:

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `TradeReconstructor` | `lots = VolumeConverter.Manager.ToLots(VolumeNative)` → `MaxVolumeLots` | **No** (source lots only) |
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity` (approve/reduce); reject → `0` | **No** |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` | **No** |
| `EfTradingStore.PersistDemoShadowAsync` | `RequestedQuantity = trade.MaxVolumeLots` (1:1 lots) | **No** |
| `BaselineScorer` | `MaxVolumeLots` for martingale / lot-escalation **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` (lots, not dest units) | unused writer in product |
| `RiskDecision` entity | `AdjustedVolumeLots` | unused by `RiskEngine` record path |
| Outbox | `OutboxEventType.ScoreUpdate` JSON `{"state","completed"}` — **no qty, no FIX** | **No** |

---

## 4. Measured lots pipeline (this is the real “conversion” today)

```text
IMTDeal.Volume()                 ulong ticks
        ↓  TradeReconstructor L89
VolumeConverter.ToLots           ÷ 10_000
        ↓
MaxVolumeLots                    source lots
        ↓  PersistDemoShadowAsync L302  (only if SuggestedState == SHADOW and a DestinationQuote row exists)
CopyIntent.RequestedQuantity     = MaxVolumeLots          ← 1:1 lots, Status = "SHADOW_ONLY"
        ↓  SimulateEntry(..., trade.MaxVolumeLots, ...)
ShadowOrder.Quantity             = same lots
        ↓  RiskEngine (if anyone constructed a request from that intent)
ApprovedQuantity                 = RequestedQuantity      ← still lots
        ↓
FIX tag 38 OrderQty              DOES NOT EXIST
```

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

`PersistDemoShadowAsync` is reached from `ReconstructionScoringService.RebuildTraderAsync` (L144). It **returns before any intent** unless `state == TraderState.SHADOW` **and** a `DestinationQuotes` row exists. That is a demo ledger, not a venue send.

`RiskLimits` are **lots-shaped**, not ounce-shaped:

| Limit | Default | If 0.10 lots were first converted to 10 oz |
|---|---:|---|
| `MaxPositionQuantity` | **5** | 10 oz would `MAX_POSITION_QUANTITY` reject |
| `MaxXauGrossExposure` | **20** | 10 oz would still pass as “10” of something undefined |
| `MaxXauNetExposure` | **10** | same unit confusion |

`RiskEngine` compares `RequestedQuantity` directly to those caps (L129–136). There is no ounces step. Unit collision is latent: today the shadow path feeds **lots**; a future ounces converter must **not** keep these numeric defaults without remapping.

`ExecutionIntent.VolumeLots` (entity L12) names the destination-side field as **lots**. That is a vocabulary leak toward the forbidden shortcut.

---

## 5. Tests: passthrough is green; never-passthrough is skipped

This slot re-read the three unit files. Tests were **not** re-executed (read-only worker). On-disk attributes are the measurement.

### 5.1 Binding conversion suite — `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs`

184 lines. Header: *“Full SUT is the missing `IQuantityConverter`.”*

Four **live** Facts **prove** `0.10 → 0.10` (the forbidden shortcut):

| Fact | Asserted output |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | `0.10` (A43 E06 requires mini `contract_size=10` → `1.00`) |
| `Lots_convention_row_also_returns_source_lots` | `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | last-stage grid only (`12.30` / `12.00` / `0`) |

**21** `[Fact]`/`[Theory]` methods are `Skip = "A43 … IQuantityConverter missing"` including:

- `Never_passthrough_MT5_lots` — *“0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.”*
- `Known_lot_to_OrderQty_examples` — 9-row table (ticks × contract × convention)
- `Mini_and_nano_contracts_differ`
- `Lots_convention_only_when_mapped`
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”*
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”*

Those skips are the **honest** G7 lock. Do not un-skip until `IQuantityConverter.Convert` exists.

### 5.2 Last-stage floor suite — `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs`

162 lines. Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m` (passthrough on a 0.01 step). One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`).

### 5.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out**.

### 5.4 Volume scale (source only)

`D:\Prop\tests\Unit\VolumeConverterTests.cs`: `ToLots(1000) = 0.10`, scale `10_000`, not hundredths. Correct for Manager `Volume()` (A38). Irrelevant to dest `OrderQty`.

---

## 6. Live FIX `OrderQty`: impossible in current code

`grep` this slot of `OrderQty` / `38=` / `35=D` / `(35, "D")` under `D:\Prop\src\Fix.CTrader` `*.cs`:

| Pattern | Hits |
|---|---|
| `OrderQty` / `38=` | **0** |
| `35=D` / `(35, "D")` | **0** |
| `(35, …)` present | Logon `"A"`; harness `"A"` / `"3"` / `"0"` / `"y"` / `"X"` / `"8"`; quote service `"y"` / `"V"` |

`CTraderFixSession.TryLogonAsync` writes **only** `35=A` (Logon) then **disposes** the TLS socket. No NewOrderSingle method. No heartbeat loop. No order send.

`CTraderFixLogonHostedService` after optional TLS logon:

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
```

DI pins the same bit **before** any logon:

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

`CTraderFixOptions.RealCopyExecutionEnabled` default **`false`**. Options class is **not** `Configure<>`’d; live logon reads env + hardcoded ports.

`apps/api/Program.cs`:

- `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` = `runtime.RealCopyEnabled` (forced false)
- `FEATURE_COPY_TRADING_ENABLED` = **literal `false`**
- recon note: *“NewOrderSingle still off”*

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** — there is no builder to call.

`LiveRuntimeStatus.copyNote` when flag false: *“NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”*

A100 **G10** (`position sizing conversion is verified`) is **FAIL**. Checkbox stays `[ ]`. §70 live FIX send stays **0**.

---

## 7. Goal context: ALL groups / ALL traders; no live copy loss

Catalog code (independent of `QuantityNormalizer`):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group returned by `GetGroupsCore`:

1. `GroupRequestArray("*")` first
2. fallback `GroupTotal` + `GroupNext`
3. per group: `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`

No plan-name filter. No `Take(N)` on accounts.

Prior **measured** census (this slot did **not** re-attach; cite `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md`, 2026-08-18T08:45Z):

| Broker | Groups | Traders | Positions | Connect |
|---|---:|---:|---:|---|
| Achiever | 8 | 6512 | 1506 | HTTP proxy |
| StarwaveFX | 10 | 1948 | 478 | direct |
| **Total** | **18** | **8460** | **1984** | |

`QuantityNormalizer` is **not** on the Manager fetch path. Completeness of `GroupRequestArray` / `UserRequestArray` is a different slot. This class cannot drop or invent traders.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog (ALL groups, ALL logins)
    → reconstruct / score
    → SHADOW_ONLY CopyIntent (lots 1:1)   [only if SHADOW + dest quote]
    ✗  QuantityNormalizer unused
    ✗  IQuantityConverter missing
    ✗  no 35=D / tag 38
    ✗  RealCopyEnabled = false
    ✗  FEATURE_COPY_TRADING_ENABLED = false
```

Fetching 8460 logins does **not** arm send. Shadow `RequestedQuantity = MaxVolumeLots` is a **demo/ledger** 1:1 lots copy. It is still not FIX. It **would** be a G7 defect the day a sender is added without `IQuantityConverter`.

---

## 8. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty` / `35=D` / `cTrader`.

Display/JSON uses classic Manager scale (grep this slot):

```text
deal.volume / 10000.0     export_controller, journal_controller, trade_service, worker_service
pos.volume  / 10000.0     symbol_controller, trade_service, worker_service
units / 10000.0L          trade_execution_service (lots display)
```

`mt5_manager.cpp` `DealerSend` writes `request->Volume(volume)` in **native ticks**. That is **MT5→MT5 dealer volume**, **not** cTrader tag 38. Do not copy YoPips `volume/10000` into a FIX builder.

---

## 9. Spec vs code (do not rubber-stamp)

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
| Catalog ALL | Achiever + Starwave groups + traders | implemented; last census 18/8460 | **INDEPENDENT** of this class |

---

## 10. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** tick G10 or authorize `REAL_COPY_EXECUTION_ENABLED=true`.
- Does **not** print passwords, FIX password, proxy auth, or account secrets.
- Does **not** add a `35=D` sender.
- Does **not** re-hash the SUT via `Get-FileHash` (content re-read matches D18 quote).
- Does **not** re-run xUnit this pass.
- Does **not** re-attach Manager or FIX; census numbers are the 2026-08-18 measured dump.

---

## 11. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire it from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = `requested_quantity` only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`. Remap `RiskLimits` if those numbers stay lot-shaped.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 12. Slot-38 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines, re-read) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`) |
| Blind lots → FIX `OrderQty` on the wire | **NO** (unused + no `35=D`) |
| `IQuantityConverter` | **MISSING** |
| Product callers | **0** |
| Shadow `RequestedQuantity` | `= MaxVolumeLots` (1:1 lots, `SHADOW_ONLY`) |
| G7 / G10 | **FAIL** |
| Live send | **OFF** (`SAFE_BY_ABSENCE`) |
| Extra flags | `RealCopyEnabled=false`; `FEATURE_COPY_TRADING_ENABLED=false` |
| Capital at risk from this class | **none** |
| Manager census (context, not re-run) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |

**One-line:** `QuantityNormalizer` would blindly passthrough MT5 lots as dest qty (`0.10 ≠ 10.00` ounces), but it never reaches FIX; no live `OrderQty` can be sent.

```text
[DO NOT] Treat unused last-stage floor as a verified §38 converter.
[DO NOT] Un-skip G7 before IQuantityConverter.
[DO NOT] Write 38= from MaxVolumeLots or Normalize(lots, 1, dest).
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
[DO NOT] Copy YoPips volume/10000 into a FIX builder.
```
