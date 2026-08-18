# W500_RESEARCH_198 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_198.md` |
| Slot | **198** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent product + test + FIX + catalog + YoPips re-read; **no** Manager/TLS re-attach; **no** xUnit re-run; **no** `Get-FileHash`) |
| Agent | W500 research subagent, slot 198 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture executive change #10 + §38 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492, §68 L2619); `A43_position_sizing.md` L28; A38 (1 lot = 10_000); A89 **G7**; A100 **G10** (`[ ] FAIL` L33) |
| Same-topic siblings (re-read SUT; **not** copied as proof) | `W500_RESEARCH_18.md`, `38`, `58`, `78`, `98`, `118`, `138`, `158`, `178`, `D18_qty.md`, `B17_qty_review.md` |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no Manager / proxy / FIX / DB password values; flag **booleans** only) |
| Live `35=D` this pass | **No** (this slot did not open TLS). Copy hop has **no** NOS assembler. Residual: standalone demo helper **already** sent `35=D` on demo (see §6.2) — **not** via this type. |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full 31-line SUT. New hop: `XauUsdOneToOneCopyPolicy` (173) + `CopyTradingService` (276) + `CopyTradingHostedService`. Conversion + last-stage + `ExecutionAndSizingTests` + `XauUsdOneToOneCopyPolicyTests`. Adjacent: `VolumeConverter`, `TradeReconstructor` L89, `ShadowCopyEngine`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `LiveMt5Registration`, `CTraderFixSession` (135), `CTraderFixDemoTestTrade` (391), `CTraderFixLogonHostedService`, `CTraderQuoteService`, DI, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`. `.env` flag **names + booleans only**. Census JSON header + both brokers’ `groupNames` **re-summed**. YoPips `volume/10000` + `DealerSend` (MT5 dealer, not FIX). |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest — do not collapse two facts)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. A89 G7 / A100 G10 remain FAIL. Copy-path FIX `OrderQty` is `SAFE_BY_ABSENCE`. A sibling demo helper can send `35=D` and is not this type.**

| Stale claim (older slots / docs) | Measured this slot |
|---|---|
| 78 / 98 / D18 “**zero product callers**” | **STALE.** `XauUsdOneToOneCopyPolicy` L71 / L139 calls `Normalize`. |
| 118 / 138 / 158 / 178 “product `lots × 0.05`” | **STALE.** Allocation is now **`1m`** (1:1). `CopyTradingService` no longer owns `GoldSpec` or `_qty`. |
| 138 / 158 “product `*.cs` has **0** `35=D` / `(35, "D")`” | **STALE.** `CTraderFixDemoTestTrade.Build("D", …)` exists. |
| 178 “demo helper hardcoded `38=1000`” / “file 347 lines” | **STALE.** Current file is **391** lines; open qty is **`(38, "1")`**. Demo JSON `LastQty=1`. |
| `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY` forced false” | **STALE.** DI binds env; lab `.env` L73 is `true`. |
| A100 G10 “No `SourceDestinationQuantityConversionTests`” | **STALE as inventory.** File exists; converter Facts are still `[Fact(Skip=…)]`. Gate remains **FAIL**. |

| Question | Measured this slot (2026-08-18 disk) |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With the new policy `allocationFactor = 1` it **is** a lots passthrough (`0.10 → 0.10`, **not** `10.00`). |
| Does the **policy** convert to ounces? | **Partially, unused.** `FixOrderQtyUnits = lots * 100` (hardcoded `GoldOuncesPerLot`). `CopyTradingService` persists **`instruction.Lots`**, not `FixOrderQtyUnits`. |
| Does any copy path write that number as FIX tag 38? | **No.** Copy / Risk / Shadow / workers never assemble tag 38. The only tag-38 writer is the **demo tool**, hardcoded `"1"`, not `Normalize` output. |
| Is the class unused? | **No.** `XauUsdOneToOneCopyPolicy` L139: `_qty.Normalize(signal.SourceLots, AllocationFactor=1, GoldLots)`. Hosted every 20 s. |
| Does Manager catalog of ALL groups/traders go through this class? | **No.** `GroupRequestArray("*")` / `UserRequestArray` / `UserLogins` only. |
| Can **copy-to-cTrader of the 8460-login book** take a live loss today via this type? | **No.** `NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, no NOS in the copy hop. The “would send” branch only stamps `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” It **does** blindly scale MT5 lots. The new 1:1 policy **worsens** the G7 shape: `Normalize(0.10, 1, GoldLots) = 0.10`. A43 E01 / G7 requires **10.00** on a BaseUnits XAU book (`0.10 × 100 oz`). If anyone later wires `CopyIntent.RequestedQuantity` (`instruction.Lots`) into a NewOrderSingle, **0.10 lots becomes 0.10 dest qty** (100× too small vs 10 oz), or **`FixOrderQtyUnits=10` is the unused sibling field**.

Passing unit Facts still **lock `allocation=1` passthrough**. The binding “never passthrough” Fact is **`[Fact(Skip = …)]`**. The new policy test **locks** `Lots=0.05` and `FixOrderQtyUnits=5` — ounces exist on the instruction, **not** on the persisted hop.

**Risk to capital from this type: NONE.** Fetching the re-summed 8460 manager logins cannot open a Pepperstone **live** (`1369850`) position through `QuantityNormalizer`. Demo-tool `35=D` is a **separate** residual (hardcoded `38=1`, demo host/account only).

**One-liner:** `QuantityNormalizer` is still dest-grid only; the new 1:1 policy calls it with `allocation=1` (`0.10→0.10`) and computes unused `FixOrderQtyUnits=lots×100`; copy persists **Lots**, not ounces; copy-path `OrderQty` cannot be sent (`SAFE_BY_ABSENCE`).

---

## 1. Architecture law (binding)

Executive change #10 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100):

```text
Never blindly convert MT5 lots directly into cTrader OrderQty.
```

§38 (`L1457–1464`):

```text
Never blindly:
source 0.10 MT5 lots
=
destination OrderQty 0.10
```

Legal pipeline (A43 §2 / architecture §38):

```text
source volume (ulong ticks)
    ↓  ÷ 10_000                 VolumeConverter.Manager
source lots
    ↓  × source_contract_size   (measured; never assume 100)
canonical ounces
    ↓  × allocation × confidence   (both in (0, 1])
allocated ounces
    ↓  ÷ dest unit (BaseUnits vs Lots)
pre-round dest qty
    ↓  floor to dest step / min / max     ← QuantityNormalizer is ONLY this last box
requested_quantity  →  FIX tag 38
```

`QuantityNormalizer` implements **only** the last floor (`sourceLots * allocation`, then dest grid). It has no ticks, no contract size, no convention, no confidence, no margin, no mapped close qty.

A43 one-line law (`A43_position_sizing.md` L28):

```text
source 0.10 MT5 lots  ≠  destination OrderQty 0.10
```

Known fixture the SUT fails as a converter (A43 E01), plus the **current product hop**:

| Input | Required dest `OrderQty` (BaseUnits, 1 unit = 1 oz) | Measured |
|---|---:|---|
| `Normalize(0.10, 1, DestBaseUnits1Oz)` | **10.00** | **0.10** (unit lock) |
| Policy `Normalize(0.10, 1, GoldLots)` then persist `Lots` | **10.00** | **0.10** written to `RequestedQuantity` |
| Policy `FixOrderQtyUnits = 0.10 * 100` | **10.00** | **10.00 computed, never persisted, never sent** |
| Older hop `Normalize(1.00, 0.05, GoldSpec)` | **5.00** | **gone** — `AllocationFactor` is now `1m` |

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is **not** on `InstrumentQuantitySpec`. Product `*.cs` has **0** hits for `QuantityConvention`, `destination_symbols`, and `IQuantityConverter`. `GoldLots` is a hardcoded `(0.01, 5, 0.01, 2)` with **no** convention label. `GoldOuncesPerLot = 100` **assumes** contract size 100 (A43 §1.1 / L61: *never assume 100*).

Go-live checkbox this owns (`§68` L2619 / A100 G10):

```text
[ ] FAIL  G10  position sizing conversion is verified
```

Still **unchecked**.

---

## 2. Measured SUT

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot) |
| Types | `InstrumentQuantitySpec` (record: `MinQuantity`, `MaxQuantity`, `StepSize`, `Precision`); `QuantityNormalizer` (one method) |
| Content vs D18 / B17 / W500_38…178 quoted source | **line-identical** (same 31 lines, same arithmetic) |
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
Normalize(1.00, 1, GoldLots)           = 1.00   (new policy allocation)
Normalize(0.10, 0.05, GoldLots)        = 0.005 < 0.01 → 0   (old hop, unused)
Normalize(1.00, 1.50, DefaultSpec)     = 1.50   (allocation > 1 is accepted)
```

Absent from the type (required by A43):

| Input | On `InstrumentQuantitySpec` / `Normalize`? |
|---|---|
| MT5 ticks / `VolumeConverter` | **No** |
| `contract_size` | **No** (policy hardcodes `100` beside the SUT) |
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** — 0 hits under product `*.cs` |
| `IQuantityConverter` type | **No** — name exists only in **skipped** test strings |
| `confidence_scale` | **No** |
| `spec_status` / `destination_symbols` | **No** |
| margin / leverage / quote | **No** |
| mapped dest remaining (CLOSE/REDUCE) | **No** |
| `allocationFactor > 1` reject | **No** — test `Allocation_greater_than_one_is_currently_accepted` locks `1 × 1.5 = 1.50` |

`VolumeConverter` is a **source** scale only (`native / 10_000 → lots`). It is **not** composed into `QuantityNormalizer`. Product composition is `TradeReconstructor` L14–20 / L89 only.

---

## 3. Product callers: **one type**, not `CopyTradingService` (118–178 hop STALE)

`grep` this slot of `QuantityNormalizer` / `new QuantityNormalizer` / `_qty.Normalize` over product `*.cs`:

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | **`new()` L71 + `Normalize` L139** |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **0** `Normalize` — only `AllocationFactor` const alias |
| `D:\Prop\src\Application` | **0** |
| `D:\Prop\src\Fix.CTrader` | **0** |
| `D:\Prop\src\Mt5` | **0** |
| `D:\Prop\apps` | **0** |
| Tests | `ExecutionAndSizingTests`, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests` |

Not registered in DI. The policy constructs it privately.

### 3.1 New hop — `XauUsdOneToOneCopyPolicy` (173 lines)

```63:71:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
public sealed class XauUsdOneToOneCopyPolicy
{
    public const int MinCompletedXauTrades = 20;
    public const decimal AllocationFactor = 1m;
    public const decimal GoldOuncesPerLot = 100m;

    public static readonly InstrumentQuantitySpec GoldLots = new(0.01m, 5m, 0.01m, 2);

    private readonly QuantityNormalizer _qty = new();
```

```136:167:D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs
        decimal lots;
        try
        {
            lots = _qty.Normalize(signal.SourceLots, AllocationFactor, GoldLots);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Reject("INVALID_SOURCE_LOTS");
        }

        if (lots <= 0)
            return Reject("QTY_BELOW_MIN_OR_STEP");
        // ...
        return new CopyInstruction
        {
            Accept = true,
            Reason = "ONE_TO_ONE_XAUUSD",
            Lots = lots,
            FixOrderQtyUnits = decimal.Round(lots * GoldOuncesPerLot, 2, MidpointRounding.ToZero),
            // ...
        };
```

Measured meaning:

| Field | Value for `SourceLots=0.10` | Legal BaseUnits OrderQty |
|---|---:|---:|
| `Lots` (after `Normalize(..., 1, GoldLots)`) | **0.10** | n/a — this is still **lots** |
| `FixOrderQtyUnits` | **10.00** | **10.00** — hardcoded `×100`, not dest convention / Security List |

Eligibility (independent of ounces math): state not blocked/early; no martingale/averaging/lot-escalation; `CompletedXauTrades ≥ 20`; `XauNetPnl > 0`; group name must **not** start with `demo\` or `contest\`. Open/Increase requires `SourceStillOpen` (no lookahead on closed winners).

### 3.2 `CopyTradingService` (276 lines) — persists **Lots**, not ounces

```14:24:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
public sealed class CopyTradingService
{
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
    // ...
    private readonly XauUsdOneToOneCopyPolicy _policy = new();
```

Generate path (open book only — `!t.Completed`):

```129:165:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var instruction = _policy.Evaluate(snapshot, new CopySignal { /* SourceLots = trade.MaxVolumeLots */ });
                if (!instruction.Accept)
                    continue;

                var qty = instruction.Lots;
                var intent = new CopyIntent
                {
                    // ...
                    RequestedQuantity = qty,
                    Status = "PENDING_RISK",
```

Then `RiskEngine.Evaluate` is called with that **lots** `qty`. Persist **overrides** `AllowFixSend = false` (L211). Live-send conjunction:

```217:223:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even if `AllowFixSend` were true, **no FIX is written**. Status string only. `instruction.FixOrderQtyUnits` is **never read** (`grep FixOrderQtyUnits` = policy field + two unit Facts only).

`CopyTradingHostedService` (40 lines) waits 8 s, then every 20 s calls `GenerateShadowIntentsAsync` and logs *“Live NewOrderSingle still blocked.”*

DI (`DependencyInjection.cs` L39–59):

```text
RealCopyEnabled = env REAL_COPY_EXECUTION_ENABLED == "true"   // not hard-false
AddScoped<CopyTradingService>()
AddHostedService<CopyTradingHostedService>()
AddSingleton<RiskEngine>()   // unused by CopyTradingService — that type does `new RiskEngine()`
```

Copyable states include `SHADOW`, `LIVE_CANDIDATE`, **and `LIVE`**. Promotion cannot auto-happen: `TraderStateMachine.CanPromoteToLive => false` (`BaselineScorer.cs` L211). `FromBaseline` reachable set remains `{INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, RISK_BLOCKED}`.

### 3.3 Adjacent engines (still do not call the SUT themselves)

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `TradeReconstructor` | `lots = VolumeConverter.Manager.ToLots(VolumeNative)` → `MaxVolumeLots` | **No** (source lots only) |
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity`; reject → `0` | **No** (consumes pre-baked **lots** from CopyTradingService) |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` | **No** |
| `EfTradingStore.PersistDemoShadowAsync` | `RequestedQuantity = trade.MaxVolumeLots` (1:1 lots), `Status = "SHADOW_ONLY"` | **No** — **second writer**, still blind lots |
| `BaselineScorer` | `MaxVolumeLots` for martingale / lot-escalation **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` | **0 writers** (`new ExecutionIntent` / `ExecutionIntents.Add` = 0) |

Two SHADOW writers now exist:

| Writer | Qty | Status |
|---|---|---|
| `PersistDemoShadowAsync` (via `ReconstructionScoringService` L144) | `MaxVolumeLots` **1:1** | `SHADOW_ONLY` |
| `CopyTradingService.GenerateShadowIntentsAsync` | `instruction.Lots` = `Normalize(MaxVolumeLots, 1, GoldLots)` | `SHADOW_ONLY` (or unimplemented live label) |

Neither is FIX tag 38. Both are **lots-shaped**. The ounces field is computed and dropped.

`RiskLimits` stay **lots-shaped** (`MaxPositionQuantity = 5`, `MaxXauGrossExposure = 20`, `MaxXauNetExposure = 10`). After a real ounces converter, 5 oz would pass a cap that was meant as 5 lots. CopyTradingService also feeds `CurrentGrossXau = 0` / `CurrentNetXau = 0` every tick, so those caps never fire on the hosted path.

`ExecutionIntent.VolumeLots` is itself a naming trap: if a writer appears and copies `RequestedQuantity` (lots) into `VolumeLots` then onto tag 38, G7 fails by construction.

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
              policy.Evaluate(SourceLots = MaxVolumeLots)
                    lots = Normalize(lots, 1, GoldLots)   ← STILL lots
                    FixOrderQtyUnits = lots × 100         ← computed, dropped
              CopyIntent.RequestedQuantity = lots         ← not ounces
              RiskEngine.Evaluate(RequestedQuantity=lots)
              RiskDecisionRecord.AllowFixSend = false     ← hardcoded persist
              Status = SHADOW_ONLY
              ShadowOrder.Quantity = same lots
        ↓
FIX tag 38 OrderQty              DOES NOT EXIST on this hop
```

`AllowFixSend` computed inside `Evaluate` requires `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine` L147–150). Copy path **forces** `Reconciled = VenueReconciled = false`, so `Evaluate` itself yields `AllowFixSend=false` even if `.env` armed the runtime flag. Persist then **overwrites** `AllowFixSend=false` again (L211).

`VenueHealthy` is `_runtime.Trade.LoggedOn && _runtime.Quote.LoggedOn`. Logon can be true (TLS `35=A` only). That does **not** open a send path.

---

## 5. Tests: passthrough is green; never-passthrough is skipped; new policy locks 1:1 lots

This slot re-read the four unit files. Tests were **not** re-executed. On-disk attributes are the measurement.

### 5.1 Binding conversion suite — `SourceDestinationQuantityConversionTests.cs` (184 lines)

Header: *“Full SUT is the missing `IQuantityConverter`.”*

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
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”* — **still true for those two**; the **policy** now calls it.
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”* — **still true**.

Do not un-skip until `IQuantityConverter.Convert` exists.

### 5.2 Last-stage floor suite — `QuantityNormalizerStepMinMaxTests.cs` (162 lines)

Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m`. One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`). Live fact `Unaligned_max_is_returned_raw_not_re_floored` **locks** the E23 defect.

### 5.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out**.

### 5.4 New `XauUsdOneToOneCopyPolicyTests` (136 lines)

| Fact | Locked number |
|---|---|
| `Eligible_open_xau_is_one_to_one_lots_and_sl_tp` | `Lots=0.05` **and** `FixOrderQtyUnits=5` |
| `Close_of_open_book_is_one_to_one` | `Lots=0.10` **and** `FixOrderQtyUnits=10` |
| `Lot_below_min_rejected` | `0.001` → `QTY_BELOW_MIN_OR_STEP` |

This is **not** G7. It locks the 1:1 lots identity plus a hardcoded `×100` ounces sidecar. It does **not** un-skip `Never_passthrough_MT5_lots`. It does **not** prove `RequestedQuantity` is ounces (the service writes `Lots`).

### 5.5 Volume scale (source only)

`VolumeConverter.ToLots(1000) = 0.10`, scale `10_000`, not hundredths. Correct for Manager `Volume()` (A38 / W500_186). Irrelevant to dest `OrderQty`.

---

## 6. Live FIX `OrderQty`: copy hop still impossible

`grep` this slot of `OrderQty` / `38=` / `35=D` / `(35, "D")` / `(38,` under product `*.cs`:

| Pattern | Tree | Hits |
|---|---|---|
| Literal `35=D` / `"35=D"` / `(35, "D")` | `D:\Prop\src` + `apps` | **0** |
| `Build("D"` | `Fix.CTrader` | **3** — `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 |
| `(38,` | `Fix.CTrader` | **3** — flatten qty from 704/705; open **`"1"`**; close LastQty/`"1"` |
| `OrderQty` | `Fix.CTrader` | **0** |
| `(35, …)` on hosted session | `CTraderFixSession` | **only** `(35, "A")` Logon |

`CTraderFixSession` is **135** lines. `TryLogonAsync` writes **only** `35=A` then **disposes** the TLS socket (`using` TcpClient + SslStream). No NewOrderSingle method. No heartbeat loop. No order send. No tag 38.

`CTraderQuoteService` builds SecurityList `35=y` and MarketDataRequest `35=V` as **in-memory lists**. `grep CTraderQuoteService` under `D:\Prop\src` = definition only. **Not** registered in DI.

### 6.1 Flag bind (108 / CREDENTIALS “forced false” STALE)

`DependencyInjection.cs` L39–42 **does not** hard-pin false:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` **does not** assign `_runtime.RealCopyEnabled = false`. It only logs `RealCopyArmed={Armed}` (L68–70).

`.env` (boolean only, not a secret):

| Key | Value on disk |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | **`true`** (`.env` L73) |
| `FEATURE_COPY_TRADING_ENABLED` | **`true`** (`.env` L106) |

So **process `LiveRuntimeStatus.RealCopyEnabled` is armed if that env is loaded**. That is an operator wish + a DI bind. It is **not** a send license.

`/api/settings` exposes `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled` and `FEATURE_COPY_TRADING_ENABLED = true` (literal).

`CTraderFixOptions.RealCopyExecutionEnabled` default remains **`false`**. Options class is **not** `Configure<>`’d.

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** — there is no builder to call.

`LiveRuntimeStatus.copyNote` when flag true: *“REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”*

A100 **G10** is **FAIL**. §70 live FIX send stays **0** on the copy hop.

### 6.2 Residual demo helper (not this SUT; 178 `38=1000` STALE)

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` — **391** lines (178 said 347; 170 said 371; file grew).

- Fail-closed gate L43–59: host must start `demo-`, SenderCompID must start `demo.`, refuse `live-` / `live.`, refuse account **`1369850`**.
- Open NOS: `Build("D", …, (38, "1"))` at L163–169 — **hardcoded 1 unit**, not `QuantityNormalizer`.
- Flatten/close use position qty or ER LastQty.
- Only caller: `D:\Prop\tools\DemoFixTestTrade\Program.cs` L33. **0** hits in `apps/`, `Infrastructure/`, `CopyTradingService`, logon host.
- Prior measured demo fill: `DEMO_FIX_TEST_TRADE.md` — demo host `demo-us-eqx-01.p.c-trader.com`, account **5328266**, `LastQty=1`, `OrderSent=true`. **Not** live `1369850`. **Not** Achiever/Starwave copy.

This slot did **not** invoke that tool.

---

## 7. Goal context: ALL groups / ALL traders; no live copy loss

Catalog code (independent of `QuantityNormalizer`):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` (`NativeMt5BrokerConnector` L145–214):

1. `GroupRequestArray("*")` first (L155)
2. fallback `GroupTotal` + `GroupNext`
3. per group: `UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`

No plan-name filter. `DealIngestionService` has **0** `Take(` hits. Positions use `GetGroupPositionsAsync("*")` or all accounts.

`LiveMt5Registration.CreateConnectors` builds **both** Achiever (proxy optional from env) and Starwave (`ProxyEnabled = false` hard pin, L45). DI throws unless both Manager passwords pass `IsSecret` (dummy/fake path disabled).

Census **re-summed this slot** from `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16.8519545+00:00`). This slot did **not** re-attach.

**Achiever** (8 groups, header `accounts=6512`, `openPositions=1506`):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |
| **Sum** | **6512** |

**StarwaveFX** (10 groups, header `accounts=1948`, `openPositions=478`):

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |
| **Sum** | **1948** |

**Total: 18 groups / 8460 traders / 1984 positions.** Header counts match group-row arithmetic.

`QuantityNormalizer` is **not** on the Manager fetch path. Completeness of `GroupRequestArray` / `UserRequestArray` is a different slot.

Copy **eligibility** now excludes `demo\` / `contest\` (policy L105–110). That filters almost all Achiever (6489 of 6512 sit under `demo\`/`contest\`). It does **not** change the fetch-all mandate. It does **not** send FIX.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog (ALL groups, ALL logins)     ← 18 / 8460, flag-blind
    → reconstruct / score
    → PersistDemoShadowAsync: SHADOW_ONLY, lots 1:1
    → CopyTradingHostedService:
          policy.Normalize(lots, 1) → RequestedQuantity = Lots
          FixOrderQtyUnits dropped
          SHADOW_ONLY
    ✗  IQuantityConverter missing (ounces path not the hop)
    ✗  no copy-path 35=D / tag 38
    ✗  NewOrderSingleImplemented = false
    ✗  VenueReconciled = false
    ✗  persisted AllowFixSend = false
    ✗  Evaluate.Reconciled forced false
    ✗  CanPromoteToLive = false
    ✗  0 ExecutionIntent writers
    ~  RealCopyEnabled follows env (may be true) — not a sender
    ~  demo CLI Build("D") + 38=1 — off hop, demo-gated
```

Fetching 8460 logins does **not** arm send. Shadow qty is a **demo/ledger** number. It **would** be a G7 defect the day a sender is added that reads `RequestedQuantity` (`Lots`) without `IQuantityConverter`.

---

## 8. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty` / `35=D` / `cTrader`.

Display/JSON uses classic Manager scale (grep this slot under `src\`):

```text
deal.volume / 10000.0     export_controller, journal_controller, trade_service, worker_service
pos.volume  / 10000.0     symbol_controller, trade_service, worker_service
```

`trade_execution_service.cpp` L1118: `ed.volume = c.mt5.volume; // MT5 native volume units (lot_size = volume/10000)`.

`mt5_pool.cpp` `DealerSend` / `DealerSendOrder` writes `request->Volume(volume)` in **native ticks**. That is **MT5→MT5 dealer volume**, **not** cTrader tag 38. Do not copy YoPips `volume/10000` into a FIX builder. Do not copy policy `lots×100` into a FIX builder without dest convention.

---

## 9. Spec vs code (do not rubber-stamp)

| ID | Spec | Measured | Stance |
|---|---|---|---|
| G7 / E01 | 0.10 lots × 100 oz → BaseUnits `10.00` | `Normalize` → `0.10`; hop persists `Lots=0.10` | **FAIL** passthrough |
| Policy sidecar | ounces field | `FixOrderQtyUnits=10` computed, **unused** | **PARTIAL / not the hop** |
| Assumed 100 oz/lot | A43: never assume 100 | `GoldOuncesPerLot = 100m` const | **FAIL** |
| E06 | same lots × mini 10 oz → `1.00` | still `0.10` | **FAIL** |
| E08 | Lots convention is the **only** `0.10→0.10` | always passthrough at alloc=1 | **FAIL** |
| §4.5 | floor, never ceil | `Truncate` + `ToZero` | **PASS** last-stage (positive qty) |
| E14 | below min → do not send | returns `0m` (policy rejects) | **PARTIAL** (not `SIZE_BELOW_MIN`) |
| E23 | cap then re-floor to step | returns raw `MaxQuantity` | **FAIL** (skipped test; live fact locks `5.09`) |
| A43 §6 | Risk + Shadow + FIX share one converter | policy uses last-stage; service writes Lots; no NOS | **MISSING** |
| §68 G10 | conversion verified | skipped fixtures | **FAIL** |
| Live tag 38 (copy) | only after converter + gates | no copy builder | **SAFE_BY_ABSENCE** |
| Catalog ALL | Achiever + Starwave groups + traders | implemented; re-sum **18/8460** | **INDEPENDENT** of this class |
| D18 / 78 / 98 “0 callers” | unused helper | **policy** now calls it | **STALE siblings** |
| 118–178 `×0.05` | product GoldSpec 5% | **`AllocationFactor=1m`** | **STALE siblings** |
| 178 `38=1000` / 347 lines | demo helper | **`(38,"1")` / 391 lines** | **STALE sibling** |
| CREDENTIALS “forced false” | process cannot arm | DI binds env; `.env` L73 `true` | **STALE** |

---

## 10. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** claim `FixOrderQtyUnits` is the live hop (it is unused).
- Does **not** tick G10 or authorize treating env `REAL_COPY_EXECUTION_ENABLED=true` as a go-live.
- Does **not** print passwords, FIX password, proxy auth, or account secrets.
- Does **not** add a copy-path `35=D` sender.
- Does **not** re-hash the SUT via `Get-FileHash` (content re-read matches D18 quote).
- Does **not** re-run xUnit this pass.
- Does **not** re-attach Manager or FIX; census numbers are the 08:42Z dump **re-summed**.
- Does **not** invoke `tools/DemoFixTestTrade`.

---

## 11. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire **`requested_quantity = converted dest qty`** (not `Lots`) from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = that field only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`. Remap `RiskLimits` if those numbers stay lot-shaped. Rename `ExecutionIntent.VolumeLots` when a writer appears. Stop assuming `GoldOuncesPerLot = 100`.

Do **not** treat `XauUsdOneToOneCopyPolicy` + `AllocationFactor=1` as the converter. Do **not** add a `35=D` builder that reads `CopyIntent.RequestedQuantity` (`Lots`) until G10 is measured PASS. Do **not** wire `FixOrderQtyUnits` to tag 38 without dest convention + measured Security List.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 12. Slot-198 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines, re-read) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`) |
| Product allocation | **`1.00`** (was 0.05 in 118–178) |
| Product persist | **`instruction.Lots`**, not `FixOrderQtyUnits` |
| Policy ounces sidecar | `lots × 100` computed, **dropped** |
| Blind lots → FIX `OrderQty` on the copy wire | **NO** (no copy `35=D`) |
| `IQuantityConverter` | **MISSING** (0 product hits) |
| Product callers of `Normalize` | **1 type** (`XauUsdOneToOneCopyPolicy`) |
| Hosted copy tick | **YES** (`CopyTradingHostedService` 20 s) |
| Demo shadow `RequestedQuantity` | still `= MaxVolumeLots` (1:1 lots) |
| `ExecutionIntent` writers | **0** |
| G7 / G10 | **FAIL** |
| Live send (copy hop) | **OFF** (`SAFE_BY_ABSENCE`) |
| Env `REAL_COPY_EXECUTION_ENABLED` | **true** (boolean; process flag can arm) |
| Extra hard blocks | `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; Evaluate `Reconciled=false`; `CanPromoteToLive=false` |
| Demo residual | `CTraderFixDemoTestTrade` `(38,"1")`, 391 lines, tools-only, live `1369850` refused |
| Capital at risk from this class | **none** |
| Manager census (re-summed, not re-run) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460 / 1984** |

```text
[DO NOT] Treat last-stage floor × 1.00 as a verified §38 converter.
[DO NOT] Treat unused FixOrderQtyUnits=lots×100 as G10 PASS.
[DO NOT] Assume contract_size=100.
[DO NOT] Un-skip G7 before IQuantityConverter.
[DO NOT] Write 38= from MaxVolumeLots or Normalize(lots, 1, GoldLots).
[DO NOT] Treat env REAL_COPY_EXECUTION_ENABLED=true as a send license.
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
[DO NOT] Copy YoPips volume/10000 into a FIX builder.
[DO NOT] Trust W500_RESEARCH_78/98/D18 “zero callers” without re-read.
[DO NOT] Trust W500_RESEARCH_118–178 “lots×0.05” — hop is now 1:1.
[DO NOT] Trust W500_RESEARCH_178 “38=1000” / 347 lines — now 38=1 / 391 lines.
[DO NOT] Trust CREDENTIALS / 108 “REAL_COPY forced false” without re-read.
[DO NOT] Invoke tools/DemoFixTestTrade from this finding.
```
