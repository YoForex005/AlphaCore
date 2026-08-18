# W500_RESEARCH_158 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_158.md` |
| Slot | **158** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent product + test + FIX + catalog + YoPips re-read; **no** Manager/TLS re-attach; **no** xUnit re-run; **no** `Get-FileHash`) |
| Agent | W500 research subagent, slot 158 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture executive change #10 + §38 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492, §68 L2619); `A43_position_sizing.md`; A38 (1 lot = 10_000); A89 #45/#47; A100 **G7 / G10** |
| Same-topic siblings (re-read SUT; **not** copied as proof) | `W500_RESEARCH_18.md`, `W500_RESEARCH_38.md`, `W500_RESEARCH_58.md`, `W500_RESEARCH_78.md`, `W500_RESEARCH_98.md`, `W500_RESEARCH_118.md`, `D18_qty.md`, `B17_qty_review.md` |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no Manager / proxy / FIX / DB password values; flag booleans only) |
| Live `35=D` this pass | **No** (builder absent) |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full 31-line SUT. Conversion + last-stage + `ExecutionAndSizingTests`. Product hop: `CopyTradingService` + `CopyTradingHostedService` + DI env bind. Adjacent: `VolumeConverter`, `TradeReconstructor`, `ShadowCopyEngine`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `LiveMt5Registration`, `CTraderFixSession` (135 lines), `CTraderFixLogonHostedService`, `CTraderQuoteService`, `FixSimulationHarness` `(35,…)` set, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`. YoPips `volume/10000` + `DealerSend` native ticks. `.env` flag names + boolean for `REAL_COPY_*` / `FEATURE_COPY_*` only. |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest — do not collapse two facts)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. G7 / G10 remain FAIL. Live FIX `OrderQty` is still `SAFE_BY_ABSENCE`.**

Siblings **78 / 98 / D18 “zero product callers”** are **STALE**. Sibling **118** still matches the SUT + one product caller. Siblings **108 / CREDENTIALS “REAL_COPY forced false”** and **127 “logon re-pins false”** are **STALE** (DI binds env; logon does not write `RealCopyEnabled`).

| Question | Measured this slot (2026-08-18 disk) |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With `allocationFactor = 1` it **is** a lots passthrough (`0.10 → 0.10`, **not** `10.00`). Product now calls it with **hardcoded `0.05`**, so `0.10 → 0` (below min) and `1.00 → 0.05` (still **100×** too small vs 5.00 oz). |
| Does any product path write that number as FIX tag 38? | **No.** Zero `35=D` / `OrderQty` / `38=` builders in product `*.cs`. |
| Is the class unused? | **No.** `CopyTradingService` L24 / L120: `_qty.Normalize(trade.MaxVolumeLots, AllocationFactor, GoldSpec)`. Hosted every 20 s. |
| Does Manager catalog of ALL groups/traders go through this class? | **No.** `GroupRequestArray("*")` / `UserRequestArray` / `UserLogins` only. |
| Can copy-to-cTrader take a live loss today via this type? | **No.** `NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, no NOS assembler. The “would send” branch only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” It **does** blindly scale MT5 lots. If anyone later wires `qty` into a NewOrderSingle, **1.00 MT5 lot becomes 0.05 dest qty** (allocation 0.05, no ounces). A43 E01 / G7 requires **100.00** on a BaseUnits XAU book before allocation, or **5.00** after 5% allocation (`1.00 × 100 oz × 0.05`). `0.05` would be **100× too small**.

Passing unit Facts still **lock `allocation=1` passthrough**. The binding “never passthrough” Fact is **`[Fact(Skip = …)]`**.

**Risk to capital from this type: none.** No socket write of tag 38 exists. Fetching the prior-measured 8460 manager logins cannot open a Pepperstone/cTrader position through this class.

**One-line:** `QuantityNormalizer` is on the SHADOW copy hop and still blindly multiplies MT5 lots (`1.00×0.05=0.05 ≠ 5.00 oz`); live `OrderQty` cannot be sent (`SAFE_BY_ABSENCE`).

---

## 1. Architecture law (binding)

Executive change #10 (`L100`):

```text
Never blindly convert MT5 lots directly into cTrader OrderQty.
```

§38 (`L1458–1464`):

```text
Never blindly:
source 0.10 MT5 lots
=
destination OrderQty 0.10
```

Legal pipeline (`A43` §2 / architecture §38 `L1468–1475`):

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

Known fixture the SUT fails as a converter (A43 E01), plus the **product call**:

| Input | Required dest `OrderQty` (BaseUnits, 1 unit = 1 oz) | Measured |
|---|---:|---|
| `Normalize(0.10, 1, DestBaseUnits1Oz)` | **10.00** | **0.10** (unit lock) |
| Product `Normalize(1.00, 0.05, GoldSpec)` | **5.00** (`1.00 × 100 oz × 0.05`) | **0.05** |
| Product `Normalize(0.10, 0.05, GoldSpec)` | **0.50** | **0** (0.005 &lt; min 0.01) |

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is **not** on `InstrumentQuantitySpec`. Product `*.cs` has **0** hits for `QuantityConvention` and **0** hits for `destination_symbols` and **0** hits for `IQuantityConverter`. `GoldSpec` is a hardcoded `(0.01, 5, 0.01, 2)` with **no** convention label.

Go-live checkbox this owns (`§68` L2619 / A100 G10):

```text
[ ] position sizing conversion is verified
```

Still **unchecked**.

A43 §1.3 reminder (not re-derived this slot): cTrader FIX worked example `38=10000` is **units of base**, not `38=0.10`. For XAU BaseUnits (1 unit = 1 oz), `0.10` MT5 lots × 100 oz = **10.00** OrderQty. Blind `38=0.10` is 100× too small or below min.

---

## 2. Measured SUT

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot) |
| Types | `InstrumentQuantitySpec` (record: `MinQuantity`, `MaxQuantity`, `StepSize`, `Precision`); `QuantityNormalizer` (one method) |
| Content vs D18 / B17 / W500_38 / W500_58 / W500_78 / W500_98 / W500_118 quoted source | **line-identical** (same 31 lines, same arithmetic) |
| Prior measured SHA-256 (D18; **not** re-hashed this worker) | `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` (1041 B) |

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

Hand-checked this slot (no xUnit):

```text
Normalize(0.10, 1, min=0.01 step=0.01) = Truncate(0.10/0.01)*0.01 = 0.10
Normalize(1.00, 0.05, GoldSpec)        = Truncate(0.05/0.01)*0.01 = 0.05
Normalize(0.10, 0.05, GoldSpec)        = 0.005 < 0.01 → 0
Normalize(1.00, 1.50, DefaultSpec)     = 1.50   (allocation > 1 is accepted)
```

Absent from the type (required by A43):

| Input | On `InstrumentQuantitySpec` / `Normalize`? |
|---|---|
| MT5 ticks / `VolumeConverter` | **No** |
| `contract_size` | **No** |
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** — 0 hits under `D:\Prop\src\**\*.cs` |
| `IQuantityConverter` type | **No** — name exists only in **skipped** test strings |
| `confidence_scale` | **No** |
| `spec_status` / `destination_symbols` | **No** — 0 product hits |
| margin / leverage / quote | **No** |
| mapped dest remaining (CLOSE/REDUCE) | **No** |
| `allocationFactor > 1` reject | **No** — test `Allocation_greater_than_one_is_currently_accepted` locks `1 × 1.5 = 1.50` |

`VolumeConverter` is a **source** scale only (`native / 10_000 → lots`). It is **not** composed into `QuantityNormalizer`. Product composition is `TradeReconstructor` L14–20 / L89 only.

```1:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
// IMTDeal::Volume() scale = 10_000. ToLots(1000) = 0.10 lots.
// Does not know ounces, dest convention, or FIX tag 38.
```

Header comment on disk (L4–8) states the official `MTAPI_VOLUME_DIV = 10_000` law and that the `mt5_types.h` “hundredths” comment is wrong. That is **source** law (A38 / A43 §1.1). It is **not** a dest `OrderQty` converter.

---

## 3. Product callers: **one** (D18 / 78 / 98 “zero” STALE)

`grep` this slot of `QuantityNormalizer` / `new QuantityNormalizer` / `_qty.Normalize` over product `*.cs`:

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **`new()` L24 + `Normalize` L120** |
| `D:\Prop\src\Application` | **0** |
| `D:\Prop\src\Fix.CTrader` | **0** |
| `D:\Prop\src\Mt5` | **0** |
| `D:\Prop\apps` | **0** (API only *calls* `CopyTradingService`, not the normalizer) |
| `new QuantityNormalizer` in tests | `ExecutionAndSizingTests`, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests` |

Not registered in DI. `CopyTradingService` constructs it privately.

### 3.1 The product hop — `CopyTradingService` (257 lines)

```14:25:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
public sealed class CopyTradingService
{
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;

    private static readonly InstrumentQuantitySpec GoldSpec = new(0.01m, 5m, 0.01m, 2);
    // ...
    private readonly QuantityNormalizer _qty = new();
```

```117:145:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                decimal qty;
                try
                {
                    qty = _qty.Normalize(trade.MaxVolumeLots, AllocationFactor, GoldSpec);
                }
                catch
                {
                    qty = 0m;
                }

                if (qty <= 0)
                    continue;
                // ...
                RequestedQuantity = qty,
                Status = "PENDING_RISK",
```

Then `RiskEngine.Evaluate` is called with that `qty`. Persist **overrides** `AllowFixSend = false` (L192). Live-send conjunction:

```198:204:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even if `AllowFixSend` were true, **no FIX is written**. Status string only.

`CopyTradingHostedService` (40 lines) waits 8 s, then every 20 s calls `GenerateShadowIntentsAsync` and logs *“Live NewOrderSingle still blocked.”*

DI (`DependencyInjection.cs` L39–59):

```text
RealCopyEnabled = env REAL_COPY_EXECUTION_ENABLED == "true"   // not hard-false
AddScoped<CopyTradingService>()
AddHostedService<CopyTradingHostedService>()
AddSingleton<RiskEngine>()   // unused by CopyTradingService — that type does `new RiskEngine()`
```

`GetStatusAsync` reports `FeatureCopyEnabled: true` (hardcoded record field) while `NewOrderSingleImplemented: false`. Summary string when blockers exist: *“Copy pipeline ON. Shadow intents only. Pepperstone will not receive NewOrderSingle.”*

Copyable states include `SHADOW`, `LIVE_CANDIDATE`, **and `LIVE`**. Promotion cannot auto-happen: `TraderStateMachine.CanPromoteToLive => false` (`BaselineScorer.cs` L211). `FromBaseline` reachable set remains `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}`.

### 3.2 Adjacent engines (still do not call the SUT themselves)

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `TradeReconstructor` | `lots = VolumeConverter.Manager.ToLots(VolumeNative)` → `MaxVolumeLots` | **No** (source lots only) |
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity`; reject → `0` | **No** (consumes pre-baked qty from CopyTradingService) |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` | **No** |
| `EfTradingStore.PersistDemoShadowAsync` | `RequestedQuantity = trade.MaxVolumeLots` (1:1 lots), `Status = "SHADOW_ONLY"` | **No** — **second writer**, still blind lots |
| `BaselineScorer` | `MaxVolumeLots` for martingale / lot-escalation **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` | **0 writers** (`new ExecutionIntent` / `ExecutionIntents.Add` = 0). Only `DbSet`, a count in `GetStatusAsync`, a scratch `_tmp_*` dump, and `FixSessionOwnership.ExecutionIntentsAllowed`. |

Demo shadow persist (still 1:1 lots — **not** through `QuantityNormalizer`):

```295:308:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
            var intent = new CopyIntent
            {
                // ...
                RequestedQuantity = trade.MaxVolumeLots,
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
```

Two SHADOW writers now exist:

| Writer | Qty | Status |
|---|---|---|
| `PersistDemoShadowAsync` (via `ReconstructionScoringService` L144) | `MaxVolumeLots` **1:1** | `SHADOW_ONLY` |
| `CopyTradingService.GenerateShadowIntentsAsync` | `Normalize(MaxVolumeLots, 0.05, GoldSpec)` | `SHADOW_ONLY` (or unimplemented live label) |

Neither is FIX tag 38. Both are still lots-shaped (or lots×0.05), not ounces.

`RiskLimits` stay **lots-shaped** (`MaxPositionQuantity = 5`, `MaxXauGrossExposure = 20`, `MaxXauNetExposure = 10`). After a real ounces converter, 5 oz would pass a cap that was meant as 5 lots. CopyTradingService also feeds `CurrentGrossXau = 0` / `CurrentNetXau = 0` every tick, so those caps never fire on the hosted path.

`ExecutionIntent.VolumeLots` is itself a naming trap: if a writer appears and copies `RequestedQuantity` into `VolumeLots` then onto tag 38, G7 fails by construction.

---

## 4. Measured lots pipeline (current)

```text
IMTDeal.Volume()                 ulong ticks
        ↓  TradeReconstructor L89
VolumeConverter.ToLots           ÷ 10_000
        ↓
MaxVolumeLots                    source lots
        ├─ PersistDemoShadowAsync
        │     RequestedQuantity = MaxVolumeLots     ← 1:1 lots, SHADOW_ONLY
        │
        └─ CopyTradingService (hosted, 20 s)
              qty = Normalize(MaxVolumeLots, 0.05, GoldSpec)
                    = lots * 0.05, dest grid          ← STILL lots, not ounces
              CopyIntent.RequestedQuantity = qty
              RiskEngine.Evaluate(RequestedQuantity=qty)
              RiskDecisionRecord.AllowFixSend = false   ← hardcoded persist
              Status = SHADOW_ONLY
              ShadowOrder.Quantity = same qty
        ↓
FIX tag 38 OrderQty              DOES NOT EXIST
```

`AllowFixSend` computed inside `Evaluate` requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine` L147–150). Copy path **forces** `Reconciled = VenueReconciled = false`, so `Evaluate` itself yields `AllowFixSend=false` even if `.env` armed the runtime flag. Persist then **overwrites** `AllowFixSend=false` again (L192).

`VenueHealthy` is `_runtime.Trade.LoggedOn && _runtime.Quote.LoggedOn`. Logon can be true (TLS `35=A` only). That does **not** open a send path.

---

## 5. Tests: passthrough is green; never-passthrough is skipped

This slot re-read the three unit files. Tests were **not** re-executed. On-disk attributes are the measurement.

### 5.1 Binding conversion suite — `D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs`

184 lines. Header: *“Full SUT is the missing `IQuantityConverter`.”*

Four **live** Facts **prove** `0.10 → 0.10` (the forbidden shortcut at allocation=1):

| Fact | Asserted output |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | `0.10` (A43 E06 requires mini `contract_size=10` → `1.00`) |
| `Lots_convention_row_also_returns_source_lots` | `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | last-stage grid only (`12.30` / `12.00` / `0`) |

**21** `[Fact]`/`[Theory]` methods are `Skip = "A43 … IQuantityConverter missing"` including:

- `Never_passthrough_MT5_lots` — *“0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.”*
- `Known_lot_to_OrderQty_examples` — 9-row table (ticks × contract × convention)
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”* — **partially stale**: Shadow/Risk still do not call it; **CopyTradingService now does**.
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”* — **still true**.

Do not un-skip until `IQuantityConverter.Convert` exists.

### 5.2 Last-stage floor suite — `D:\Prop\tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs`

162 lines. Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m`. One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`). `Allocation_scales_before_step` expects `0.10 × 0.10 = 0.01`. Live fact `Unaligned_max_is_returned_raw_not_re_floored` **locks** the E23 defect (`10 → 5.09`).

### 5.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out**. The `0.10 × 0.05 → 0` case matches the product GoldSpec min.

### 5.4 Volume scale (source only)

`D:\Prop\tests\Unit\VolumeConverterTests.cs` (cited by E004 / W500_126): `ToLots(1000) = 0.10`, scale `10_000`, not hundredths. Correct for Manager `Volume()` (A38). Irrelevant to dest `OrderQty`.

---

## 6. Live FIX `OrderQty`: still impossible

`grep` this slot of `OrderQty` / `38=` / `35=D` / `(35, "D")` under product `*.cs`:

| Pattern | Tree | Hits |
|---|---|---|
| `35=D` / `"35=D"` / `(35, "D")` | all `D:\Prop\**\*.cs` | **0** |
| `OrderQty` / `38=` | `Fix.CTrader` | **0** |
| `(35, …)` present | `Fix.CTrader` | Logon `"A"`; harness `"A"` / `"3"` / `"0"` / `"y"` / `"X"` / `"8"`; quote service `"y"` / `"V"` |

`CTraderFixSession` is **135** lines. `TryLogonAsync` writes **only** `35=A` (Logon) then **disposes** the TLS socket (`using` TcpClient + SslStream). No NewOrderSingle method. No heartbeat loop. No order send. No tag 38.

`CTraderQuoteService` builds SecurityList `35=y` and MarketDataRequest `35=V` as **in-memory lists**. `grep CTraderQuoteService` under `D:\Prop\src` = definition only. **Not** registered in DI. **Not** wired to `CTraderFixSession.Write` (no `Write` method exists).

`FixSimulationHarness` can assemble `35=8` ExecutionReport for tests. It is not a live sender.

### 6.1 Flag bind (78 / 98 / 108 / CREDENTIALS “forced false” STALE)

`DependencyInjection.cs` L39–42 **does not** hard-pin false:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` **does not** assign `_runtime.RealCopyEnabled = false`. It only logs `RealCopyArmed={Armed}` (L68–70). **W500_RESEARCH_127** “logon re-pins false” is **STALE** vs this re-read.

`.env` (boolean only, not a secret):

| Key | Value on disk |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **`true`** (`.env` L73) |
| `FEATURE_COPY_TRADING_ENABLED` | **`true`** (`.env` L106) |

So **process `LiveRuntimeStatus.RealCopyEnabled` is armed if that env is loaded**. That is an operator wish + a DI bind. It is **not** a send license.

`CREDENTIALS_AND_COPY_STATUS.md` still says `REAL_COPY_EXECUTION_ENABLED` = **false (forced)**. That document is **STALE** relative to `.env` + DI. The **send** fact in that file remains true: `35=D` method does not exist.

`/api/settings` exposes:

```text
REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled   // follows env
FEATURE_COPY_TRADING_ENABLED = true                     // literal
```

`CTraderFixOptions.RealCopyExecutionEnabled` default remains **`false`**. Options class is **not** `Configure<>`’d.

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** — there is no builder to call.

`LiveRuntimeStatus.copyNote` when flag true: *“REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”* When false: *“NewOrderSingle disabled…”*. Snapshot honesty now depends on env. **Wire send is still absent.**

A100 **G10** is **FAIL**. §70 live FIX send stays **0**.

---

## 7. Goal context: ALL groups / ALL traders; no live copy loss

Catalog code (independent of `QuantityNormalizer`):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` (`NativeMt5BrokerConnector` L189–214):

1. `GroupRequestArray("*")` first (L155)
2. fallback `GroupTotal` + `GroupNext`
3. per group: `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`

No plan-name filter. `DealIngestionService` has **0** `Take(` hits. Positions use `GetGroupPositionsAsync("*")` or all accounts. Residual `Take(200)` is `GET /api/trades` reconstructed rows only (not this SUT).

`LiveMt5Registration.CreateConnectors` builds **both** Achiever (proxy optional from env) and Starwave (`ProxyEnabled = false` hard pin, L45). DI throws unless both Manager passwords pass `IsSecret` (dummy/fake path disabled).

Prior **measured** census (this slot did **not** re-attach; cite `CREDENTIALS_AND_COPY_STATUS.md` + `LIVE_MANAGER_FETCH_MEASURED.md` / INDEX pin):

| Broker | Groups | Traders | Positions | Connect |
|---|---:|---:|---:|---|
| Achiever | 8 | 6512 | 1506 | HTTP proxy |
| StarwaveFX | 10 | 1948 | 478 | direct |
| **Total** | **18** | **8460** | **1984** | |

`QuantityNormalizer` is **not** on the Manager fetch path. Completeness of `GroupRequestArray` / `UserRequestArray` is a different slot.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog (ALL groups, ALL logins)
    → reconstruct / score
    → PersistDemoShadowAsync: SHADOW_ONLY, lots 1:1
    → CopyTradingHostedService: Normalize(lots, 0.05) → SHADOW_ONLY
    ✗  IQuantityConverter missing (ounces path)
    ✗  no 35=D / tag 38
    ✗  NewOrderSingleImplemented = false
    ✗  VenueReconciled = false
    ✗  persisted AllowFixSend = false
    ✗  Evaluate.Reconciled forced false
    ✗  CanPromoteToLive = false
    ✗  0 ExecutionIntent writers
    ~  RealCopyEnabled follows env (may be true) — not a sender
```

Fetching 8460 logins does **not** arm send. Shadow qty is a **demo/ledger** number. It **would** be a G7 defect the day a sender is added without `IQuantityConverter`.

---

## 8. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty` / `35=D` / `cTrader`.

Display/JSON uses classic Manager scale (grep this slot under `src\`):

```text
deal.volume / 10000.0     export_controller, journal_controller, trade_service, worker_service
pos.volume  / 10000.0     symbol_controller, trade_service, worker_service
```

`trade_execution_service.cpp` L1118: `ed.volume = c.mt5.volume; // MT5 native volume units (lot_size = volume/10000)`.

`mt5_pool.cpp` `DealerSend` / `DealerSendOrder` writes `request->Volume(volume)` in **native ticks**. That is **MT5→MT5 dealer volume**, **not** cTrader tag 38. Do not copy YoPips `volume/10000` into a FIX builder.

---

## 9. Spec vs code (do not rubber-stamp)

| ID | Spec | Measured | Stance |
|---|---|---|---|
| G7 / E01 | 0.10 lots × 100 oz → BaseUnits `10.00` | `Normalize` → `0.10` | **FAIL** passthrough |
| Product call | 1.00 lot × 100 oz × 0.05 → `5.00` | `0.05` | **FAIL** 100× small |
| E06 | same lots × mini 10 oz → `1.00` | still `0.10` | **FAIL** |
| E08 | Lots convention is the **only** `0.10→0.10` | always passthrough at alloc=1 | **FAIL** |
| §4.5 | floor, never ceil | `Truncate` + `ToZero` | **PASS** last-stage (positive qty) |
| E14 | below min → do not send | returns `0m` (CopyTradingService skips) | **PARTIAL** (not `SIZE_BELOW_MIN`) |
| E23 | cap then re-floor to step | returns raw `MaxQuantity` | **FAIL** (skipped test; live fact locks `5.09`) |
| A43 §6 | Risk + Shadow + FIX share one converter | CopyTradingService uses last-stage only; no NOS | **MISSING** |
| §68 G10 | conversion verified | skipped fixtures | **FAIL** |
| Live tag 38 | only after converter + gates | no builder | **SAFE_BY_ABSENCE** |
| Catalog ALL | Achiever + Starwave groups + traders | implemented; last census 18/8460 | **INDEPENDENT** of this class |
| D18 / 78 / 98 “0 callers” | unused helper | **CopyTradingService** now calls it | **STALE siblings** |
| 108 / CREDENTIALS “forced false” | process cannot arm | DI binds env; `.env` L73 `true` | **STALE** |
| 127 “logon re-pins false” | hosted service clears flag | logon **does not** write `RealCopyEnabled` | **STALE** |

---

## 10. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** tick G10 or authorize treating env `REAL_COPY_EXECUTION_ENABLED=true` as a go-live.
- Does **not** print passwords, FIX password, proxy auth, or account secrets.
- Does **not** add a `35=D` sender.
- Does **not** re-hash the SUT via `Get-FileHash` (content re-read matches D18 quote).
- Does **not** re-run xUnit this pass.
- Does **not** re-attach Manager or FIX; census numbers are the 2026-08-18 measured dump.

---

## 11. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire it from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = `requested_quantity` only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`. Remap `RiskLimits` if those numbers stay lot-shaped. Rename `ExecutionIntent.VolumeLots` when a writer appears.

Do **not** treat `CopyTradingService` + `AllocationFactor=0.05` as the converter. Do **not** add a `35=D` builder that reads `CopyIntent.RequestedQuantity` until G10 is measured PASS.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 12. Slot-158 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines, re-read) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`) |
| Product call `lots × 0.05` | **YES** (`1.00 → 0.05 ≠ 5.00 oz`) |
| Blind lots → FIX `OrderQty` on the wire | **NO** (no `35=D`) |
| `IQuantityConverter` | **MISSING** (0 product hits) |
| Product callers | **1** (`CopyTradingService`) |
| Hosted copy tick | **YES** (`CopyTradingHostedService` 20 s) |
| Demo shadow `RequestedQuantity` | still `= MaxVolumeLots` (1:1 lots) |
| `ExecutionIntent` writers | **0** |
| G7 / G10 | **FAIL** |
| Live send | **OFF** (`SAFE_BY_ABSENCE`) |
| Env `REAL_COPY_EXECUTION_ENABLED` | **true** (boolean; process flag can arm) |
| Extra hard blocks | `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; Evaluate `Reconciled=false`; `CanPromoteToLive=false` |
| Capital at risk from this class | **none** |
| Manager census (context, not re-run) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |

```text
[DO NOT] Treat last-stage floor × 0.05 as a verified §38 converter.
[DO NOT] Un-skip G7 before IQuantityConverter.
[DO NOT] Write 38= from MaxVolumeLots or Normalize(lots, 0.05, GoldSpec).
[DO NOT] Treat env REAL_COPY_EXECUTION_ENABLED=true as a send license.
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
[DO NOT] Copy YoPips volume/10000 into a FIX builder.
[DO NOT] Trust W500_RESEARCH_78/98/D18 “zero callers” without re-read.
[DO NOT] Trust W500_RESEARCH_108 / CREDENTIALS “REAL_COPY forced false” without re-read.
[DO NOT] Trust W500_RESEARCH_127 “logon re-pins false” — logon does not write the flag.
```
