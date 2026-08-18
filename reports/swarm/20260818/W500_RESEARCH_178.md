# W500_RESEARCH_178 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_178.md` |
| Slot | **178** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent product + test + FIX + catalog + YoPips re-read; **no** Manager/TLS re-attach; **no** xUnit re-run; **no** `Get-FileHash`) |
| Agent | W500 research subagent, slot 178 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture executive change #10 + §38 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492); `A43_position_sizing.md` L28; A38 (1 lot = 10_000); A89 **G7**; A100 **G10** (`[ ] FAIL` L33) |
| Same-topic siblings (re-read SUT; **not** copied as proof) | `W500_RESEARCH_18.md`, `38`, `58`, `78`, `98`, `118`, `138`, `158`, `D18_qty.md`, `B17_qty_review.md` |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no Manager / proxy / FIX / DB password values; flag **booleans** only) |
| Live `35=D` this pass | **No** (this slot did not open TLS). Copy hop has **no** NOS assembler. Residual: standalone demo tool **already** sent `35=D` on demo (see §5.2) — **not** via this type. |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full 31-line SUT. Conversion + last-stage + `ExecutionAndSizingTests`. Product hop `CopyTradingService` + `CopyTradingHostedService`. Adjacent: `VolumeConverter`, `TradeReconstructor` L89, `ShadowCopyEngine`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `LiveMt5Registration`, `CTraderFixSession` (135 lines), **`CTraderFixDemoTestTrade` (347 lines — new residual vs 138/158)**, `CTraderFixLogonHostedService`, `CTraderQuoteService`, `FixSimulationHarness` `(35,…)` set, DI, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`, `.env` flag names + booleans only, live census JSON header + both brokers’ `groupNames`. YoPips `volume/10000` + `DealerSend` (MT5 dealer, not FIX). |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest — do not collapse two facts)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. A89 G7 / A100 G10 remain FAIL. Copy-path FIX `OrderQty` is `SAFE_BY_ABSENCE`. A sibling demo helper can send `35=D` and is not this type.**

| Stale claim (older slots / docs) | Measured this slot |
|---|---|
| 78 / 98 / D18 “**zero product callers**” | **STALE.** `CopyTradingService` L24 / L120 still calls `Normalize`. |
| 108 / `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY` forced false” | **STALE.** DI binds env; lab `.env` L73 is `true`. |
| 138 / 158 “product `*.cs` has **0** `35=D` / `(35, "D")`” | **STALE.** `CTraderFixDemoTestTrade.Build("D", …)` exists; `DEMO_FIX_TEST_TRADE.json` records `OrderSent=true` on **demo**. |
| A100 G10 “No `SourceDestinationQuantityConversionTests`” | **STALE as inventory.** File exists; the **converter** Facts are still `[Fact(Skip=…)]`. Gate remains **FAIL**. |

| Question | Measured this slot (2026-08-18 disk) |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With `allocationFactor = 1` it **is** a lots passthrough (`0.10 → 0.10`, **not** `10.00`). Product calls it with hardcoded **`0.05`**, so `0.10 → 0` (below min) and `1.00 → 0.05` (still **100×** too small vs 5.00 oz). |
| Does any product path write that number as FIX tag 38? | **No.** Copy / Risk / Shadow / workers never assemble tag 38. The only tag-38 writer is the **demo tool**, hardcoded `"1000"`, not `Normalize` output. |
| Is the class unused? | **No.** `CopyTradingService` L24 / L120: `_qty.Normalize(trade.MaxVolumeLots, AllocationFactor, GoldSpec)`. Hosted every 20 s. |
| Does Manager catalog of ALL groups/traders go through this class? | **No.** `GroupRequestArray("*")` / `UserRequestArray` / `UserLogins` only. |
| Can **copy-to-cTrader of the 8460-login book** take a live loss today via this type? | **No.** `NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, no NOS in the copy hop. The “would send” branch only stamps `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” It **does** blindly scale MT5 lots. If anyone later wires `qty` into a NewOrderSingle, **1.00 MT5 lot becomes 0.05 dest qty** (allocation 0.05, no ounces). A43 E01 / G7 requires **100.00** on a BaseUnits XAU book before allocation, or **5.00** after 5% allocation (`1.00 × 100 oz × 0.05`). `0.05` would be **100× too small**.

Passing unit Facts still **lock `allocation=1` passthrough**. The binding “never passthrough” Fact is **`[Fact(Skip = …)]`**.

**Risk to capital from this type: NONE.** Fetching the prior-measured 8460 manager logins cannot open a Pepperstone **live** (`1369850`) position through `QuantityNormalizer`. Demo-tool `35=D` is a **separate** residual (hardcoded `38=1000`, demo host/account only).

**One-liner:** `QuantityNormalizer` is on the SHADOW copy hop and still blindly multiplies MT5 lots (`1.00×0.05=0.05 ≠ 5.00 oz`); copy-path `OrderQty` cannot be sent (`SAFE_BY_ABSENCE`). A demo helper sent `35=D` with hardcoded `38=1000` — not this class.

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

Known fixture that the SUT fails as a converter (A43 E01):

| Input | Required dest `OrderQty` (BaseUnits, 1 unit = 1 oz) | `Normalize(0.10, 1, DestBaseUnits1Oz)` | Product hop `Normalize(1.00, 0.05, GoldSpec)` |
|---|---:|---:|---:|
| 1_000 ticks = 0.10 lots × 100 oz | **10.00** | **0.10** | n/a (0.10 × 0.05 → **0**, below min 0.01) |
| 10_000 ticks = 1.00 lots × 100 oz × 0.05 alloc | **5.00** | 1.00 | **0.05** (100× too small) |

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is **not** on `InstrumentQuantitySpec`.

A43 one-line law (`A43_position_sizing.md` L28):

```text
source 0.10 MT5 lots  ≠  destination OrderQty 0.10
```

A100 **G10** (`position sizing conversion is verified`) is still **`[ ] FAIL`** (A100 L33, re-read this slot). Checkbox stays unchecked until `Never_passthrough_MT5_lots` is a live Fact against `IQuantityConverter.Convert`.

Do not confuse A89 **G7** (converter never-passthrough) with A100 **G07** (cTrader reconciliation after restart) or A89 **G10** (`REAL_COPY` must not allow FIX send). This slot owns the **converter** question. A89 G7 and A100 G10 are both FAIL.

---

## 2. Measured SUT (re-read this slot, 31/31 lines)

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot; identical to the W500_18 / D18 / W500_138 / W500_158 dump) |
| Prior bytes / SHA-256 (D18/B17; content unchanged vs those quotes) | 1041 / `B6CC53E8F6CAB7599B2673408616ADF8B3C8E3804663C3605CE2F1137807C149` |
| Types | `InstrumentQuantitySpec` (record: Min, Max, Step, Precision); `QuantityNormalizer` (one method) |
| This-slot hash | **Not recomputed** (no shell). Identity is the 31-line re-read vs the quoted dump. |

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

There is **no** `Precision < 0` guard; `decimal.Round` throws (`QuantityNormalizerStepMinMaxTests.Negative_precision_throws`).

Absent from the type (required by A43):

| Input | On `InstrumentQuantitySpec` / `Normalize`? |
|---|---|
| MT5 ticks / `VolumeConverter` | **No** |
| `contract_size` | **No** |
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** — `grep` of `QuantityConvention` / `interface IQuantityConverter` / `contract_size` under `D:\Prop\**\*.cs` = **0 type definitions** |
| `IQuantityConverter` | **No** (name exists only in skipped tests + reports) |
| `confidence_scale` | **No** |
| `spec_status` | **No** |
| margin / leverage / quote | **No** |
| mapped dest remaining (CLOSE/REDUCE) | **No** |
| `allocationFactor > 1` reject | **No** — `Normalize(1, 1.5, …)` returns `1.50` (live Fact documents this) |

`VolumeConverter` is a **source** scale only. It is **not** composed into `QuantityNormalizer`.

```1:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
// IMTDeal::Volume() scale = 10_000. ToLots(1000) = 0.10 lots.
// Does not know ounces, dest convention, or FIX tag 38.
```

`TradeReconstructor` (`L89`) calls `_volume.ToLots(deal.VolumeNative)` and stores `MaxVolumeLots`. That is source lots for scoring / reconstruction. It is **not** dest `OrderQty`.

---

## 3. Product callers: **one** live hop (remeasured; 78/98/D18 stale)

`grep` `QuantityNormalizer` / `new QuantityNormalizer` / `_qty.Normalize` over product `*.cs`:

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition (`public sealed class QuantityNormalizer`) |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | **L24** `new QuantityNormalizer()`; **L120** `_qty.Normalize(trade.MaxVolumeLots, AllocationFactor, GoldSpec)` |
| `D:\Prop\src\Application` | **0** (models only: `CopyGateStatus.NewOrderSingleImplemented`) |
| `D:\Prop\src\Fix.CTrader` | **0** (`CTraderFixDemoTestTrade` hardcodes `(38, "1000")`; does not import this type) |
| `D:\Prop\src\Mt5` | **0** |
| `D:\Prop\apps` | **0** (API maps `GET /api/copy/status` + `/api/copy/intents` onto `CopyTradingService`; does not call `Normalize`) |
| DI `AddTraderIntelligence` | registers `CopyTradingService` scoped (L44), `RiskEngine` singleton (L45), `CopyTradingHostedService` (L59). **Does not** register `QuantityNormalizer` (ad-hoc `new` inside the service) |
| `new QuantityNormalizer` in tests | `ExecutionAndSizingTests` L37, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests` |

### 3.1 The product hop (re-read 257 lines)

`CopyTradingService` constants (L16–19):

```text
VenueReconciled            = false          (const)
NewOrderSingleImplemented  = false          (const)
AllocationFactor           = 0.05m          (const)
GoldSpec                   = (0.01, 5, 0.01, 2)   // lot-shaped dest grid, not ounces
```

Sizing at L117–128:

```117:128:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
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
```

Worked product numbers (same arithmetic as the 31-line SUT; no test re-run required):

| `MaxVolumeLots` | `× 0.05` | After GoldSpec floor/min | A43 BaseUnits after 5% (`lots × 100 oz × 0.05`) |
|---:|---:|---:|---:|
| 0.10 | 0.005 | **0** (skip intent) | **0.50** |
| 0.20 | 0.010 | **0.01** | **1.00** |
| 1.00 | 0.050 | **0.05** | **5.00** |

That is **lots × 0.05**, not ounces. `GoldSpec` has no `contract_size` and no convention. The 5% factor is a **scalar on lots**, not allocated ounces.

`CopyTradingHostedService` (`D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`, 40 lines) ticks every 20 s after an 8 s startup delay and only calls `GenerateShadowIntentsAsync`. Log line L30: *“Live NewOrderSingle still blocked.”*

API `GET /api/copy/status` and `/api/copy/intents` (`apps/api/Program.cs` L102–103) are **read** surfaces. They do not send FIX.

### 3.2 Adjacent engines still do **not** call `Normalize`

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity` (approve/reduce); reject → `0`. `AllowFixSend` needs `RealExecutionEnabled && kill=None && Reconciled && VenueHealthy` | **No** (consumes pre-baked qty) |
| Product `RiskEngine.Evaluate` | **1** caller: `CopyTradingService` L159. Persist then **overwrites** `AllowFixSend = false` (L192) | **No** (does not quantize) |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` unchanged (`Quantity = quantity`) | **No** |
| `EfTradingStore.PersistDemoShadowAsync` | gated on `state == TraderState.SHADOW`; `RequestedQuantity = trade.MaxVolumeLots` (**1:1 lots**), `Status = "SHADOW_ONLY"` | **No** (second writer, still lots) |
| `BaselineScorer` | uses `MaxVolumeLots` for martingale/size-up **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` (lots, not dest units) | **no product writer** (`grep new ExecutionIntent` under `D:\Prop\**\*.cs` = **0**; only `CountAsync` of `SentAt` in copy status) |
| `DealIngestionService.SyncCatalogAsync` | groups + accounts only (`GetGroupsAsync` + `GetAccountsAsync(null)`) | **No** |

Demo-store 1:1 lots echo (still not FIX):

```295:308:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
            var intent = new CopyIntent
            {
                // ...
                RequestedQuantity = trade.MaxVolumeLots,
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
```

Two CopyIntent writers now exist:

| Writer | Qty source | Status | Uses `QuantityNormalizer`? |
|---|---|---|---|
| `CopyTradingService.GenerateShadowIntentsAsync` | `Normalize(MaxVolumeLots, 0.05, GoldSpec)` | `SHADOW_ONLY` (or `LIVE_SEND_BLOCKED_UNIMPLEMENTED` if four AND bits ever true) | **Yes** |
| `EfTradingStore.PersistDemoShadowAsync` | `MaxVolumeLots` 1:1 | `SHADOW_ONLY` | **No** |

Both are **G7 defects waiting for a sender**. Neither writes tag 38.

Live-send AND in the copy service (L198–201) can never fire a ticket:

```198:201:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

- `NewOrderSingleImplemented` is **const false**.
- `VenueReconciled` is **const false**.
- Persist forces `AllowFixSend = false` even if `Evaluate` would have allowed.
- `Evaluate` itself rejects increasing actions when `Reconciled=false` (`VENUE_NOT_RECONCILED`, RiskEngine L84–85). `OpenExposure` is increasing.
- Even if all four bits flipped, the branch **only changes a status string**. There is no NOS call.

`GetStatusAsync` still reports `FeatureCopyEnabled: true` (literal) and a blocker list that includes `SAFE_BY_ABSENCE` while NOS is unimplemented.

---

## 4. Tests: passthrough is green; never-passthrough is skipped

### 4.1 Binding conversion suite — `SourceDestinationQuantityConversionTests.cs`

184 lines. Header: *“Full SUT is the missing `IQuantityConverter`.”*

Four **live** Facts **prove** `0.10 → 0.10` (the forbidden shortcut):

| Fact | Asserted output |
|---|---|
| `QuantityNormalizer_passthroughs_0_10_lots_when_allocation_is_one` | `0.10` and **not** `10.00` |
| `Mini_contract_same_lots_same_normalizer_output` | `0.10` (mini must differ under A43 E06) |
| `Lots_convention_row_also_returns_source_lots` | `0.10` |
| `Respects_min_qty_and_step_as_last_stage` | last-stage grid only (`12.30` stays `12.30` on 0.01 step) |

**21** `[Fact]`/`[Theory]` methods are `Skip = "A43 …"` (counted this slot via `Skip =` grep on that file):

- `Never_passthrough_MT5_lots` — *“0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.”*
- `Known_lot_to_OrderQty_examples` — 9-row table (ticks × contract × convention)
- `Mini_and_nano_contracts_differ`
- `Lots_convention_only_when_mapped`
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”* (still true of those two types; **stale** if read as “unused by all product”)
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”* (**still true** of the copy/FIX-worker path)

Those skips are the **honest** G7 lock. Do not un-skip until `IQuantityConverter.Convert` exists. Bodies `Assert.Fail` / `true.Should().BeFalse(...)` so they cannot be un-skipped onto a test-local ounces helper.

### 4.2 Last-stage floor suite — `QuantityNormalizerStepMinMaxTests.cs`

162 lines. Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m` (passthrough on a 0.01 step). One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`). Live Fact `Unaligned_max_is_returned_raw_not_re_floored` **locks** that defect.

`Allocation_greater_than_one_is_currently_accepted` documents `1 × 1.5 = 1.50` — A43 requires `allocationFactor ∈ (0, 1]`.

`Allocation_scales_before_step` expects `0.10 × 0.10 = 0.01` and `Below_min_returns_zero` expects `0.10 × 0.05 = 0` (correct last-stage math; not dest conversion). The **same** `0.05` factor is now the product constant.

### 4.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out** when allocation is 1. Product allocation `0.05` on a 0.10 lot source is **0** (below min) — the same number the hosted pipeline will skip.

This slot did **not** re-run `dotnet test`. Arithmetic is visible in the SUT (`0.10 * 1` on a 0.01 step is 0.10; `0.10 * 0.05` is 0.005 → 0). The green Facts lock that identity.

---

## 5. Live / demo FIX `OrderQty` (copy hop vs demo helper)

### 5.1 Copy / hosted / session path — still no tag 38

`CTraderFixSession.TryLogonAsync` (135/135 lines re-read) writes **only** `35=A` (Logon) via `BuildLogon` L96. After TLS handshake it reads **one** reply and returns; `using` disposes the socket. There is no NewOrderSingle method, no keep-alive TRADE writer, no tag 38.

`CTraderFixLogonHostedService` (`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`) after optional TLS logon (L68–70) **logs** `RealCopyArmed={Armed}` and *“NewOrderSingle still unimplemented.”* It does **not** force `RealCopyEnabled = false` (that pin from slots 58/78/98 is **STALE**). It never calls `CTraderFixDemoTestTrade`.

`CTraderQuoteService` outgoing types: `35=y` (SecurityListRequest L113), `35=V` (MarketDataRequest L127). Not `D`. `BuildMarketDataRequestTags` sets 55/263/264 only. No socket writer in this type.

`FixSimulationHarness` `(35, …)` set: `"A"` / `"3"` / `"0"` / `"y"` / `"X"` / `"8"` only.

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** (L45–46) — there is no builder to call.

DI now **binds the env flag** (L39–42):

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixOptions.RealCopyExecutionEnabled` default is still **`false`** (L35). That POCO is **not** what DI uses for `LiveRuntimeStatus`.

`GET /api/settings` (`apps/api/Program.cs` L71–78):

```text
REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled   // env-bound, not forced false
FEATURE_COPY_TRADING_ENABLED = true                     // literal (slot 98 “literal false” is STALE)
```

`GET /api/reconciliation/status` note: *“NewOrderSingle still off”*.

`.env` **names + booleans only** (values of passwords not printed): `REAL_COPY_EXECUTION_ENABLED=true` (L73), `FEATURE_COPY_TRADING_ENABLED=true` (L106). Lab disk therefore **arms** `LiveRuntimeStatus.RealCopyEnabled`. Copy-path safety is **SAFE_BY_ABSENCE of a NOS builder on that hop** + const `NewOrderSingleImplemented=false` + persist `AllowFixSend=false` + const `VenueReconciled=false`. It is **not** a single named choke that a unit test proves refuses `35=D` on a logged-on TRADE socket.

`CREDENTIALS_AND_COPY_STATUS.md` still says `REAL_COPY_EXECUTION_ENABLED` **false (forced)** and “Live `35=D` method does not exist.” Both sentences are **STALE** vs current DI + `.env` + `CTraderFixDemoTestTrade`.

`LiveRuntimeStatus.Snapshot` copyNote when armed: *“REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”* That note is **true of the copy hop** and **false as a whole-tree claim** once the demo helper is counted.

### 5.2 Residual (slot-178 delta vs 138/158): demo helper **can** send `35=D`

`grep` this slot of `Build("D"` / `(38,` under product `*.cs`:

| File | What it writes |
|---|---|
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L124–130 | `Build("D", …, (38, "1000"))` market buy |
| same file L155–156 | second `Build("D", …)` flatten; tag 38 = ER last-qty or `"1000"` |
| Callers in product hosts | **0** |
| Caller | `D:\Prop\tools\DemoFixTestTrade\Program.cs` L32 only |

Gate (L42–59): host must start with `demo-`, sender with `demo.`, refuse `live-` / `live.` and **refuse account `1369850`**.

That is **not** `QuantityNormalizer`. Qty is a **string literal `"1000"`** — another blind number, 20_000× larger than the copy hop’s `0.05` on a 1.00 lot source.

On-disk result (`D:\Prop\reports\swarm\20260818\DEMO_FIX_TEST_TRADE.json`, re-read this slot; no password fields):

| Field | Value |
|---|---|
| `Allowed` / `LoggedOn` / `OrderSent` | **true** |
| `Filled` / `Flattened` | **false** |
| `ExecType` / `OrdStatus` | `"0"` / `"0"` (New) |
| `LastQty` | `"0"` |
| `SymbolName` | `XAUUSD` (`SymbolId` `41`) |
| `Host` | `demo-us-eqx-01.p.c-trader.com` |
| `Account` | `5328266` (demo; **not** live `1369850`) |
| `ClOrdId` | `T20260818090836374` |

So: **a demo NewOrderSingle was already sent** by a standalone tool. It is **outside** the copy pipeline of the 8460 manager logins. It **does not** consume `Normalize`. It **does** prove that “product tree has zero `35=D` builders” is no longer true.

Copy-to-cTrader **no-loss** for this slot still holds for the **Manager-book copy hop**. Do not treat the demo helper as “copy is live.” Do not treat it as “no FIX sender exists anywhere.”

---

## 6. Goal context: ALL Achiever + Starwave groups/traders; no live copy loss

`QuantityNormalizer` is **not** on the Manager fetch path. Catalog ingest:

```38:51:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        // GetGroupsAsync → GroupRequestArray("*")
        // GetAccountsAsync(null) → every group → UserRequestArray / UserLogins
        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

`NativeMt5BrokerConnector.GetGroupsCore` (`L152–165`): `GroupRequestArray("*", arr)`. Fallback `GroupTotal`/`GroupNext` only if the request array is empty. `ReadAccountsForGroup` (`L223–232`): `UserRequestArray` then `UserLogins` + `UserRequestByLogins`. No quantity conversion on this path.

`LiveIngestHostedService` walks `registry.All()` (Achiever + Starwave from `CreateConnectors`) and calls `SyncCatalogAsync` per broker. No dummy substitution on failure (L70).

`LiveMt5Registration.CreateConnectors` (`L23–47`): Achiever may set `ProxyEnabled` from `ACHIEVER_PROXY_*`; Starwave **`ProxyEnabled = false`** hard pin. This slot does **not** print proxy host/user/password.

Measured census (`LIVE_GROUPS_AND_TRADERS.json` probe `2026-08-18T08:42:16Z`; `CREDENTIALS_AND_COPY_STATUS.md`). Values are counts, not secrets. **This slot did not re-attach live Manager.** Group `accounts` **re-summed this slot**:

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| Achiever | OK via whitelist HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| StarwaveFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (JSON `ACHIEVER.groupNames`): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23. Sum **6512**.

Starwave groups (JSON `STARWAVEFX.groupNames`, re-read this slot at offset 45644): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2. Sum **1948**.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog (18 / 8460)  →  reconstruct / score
    →  PersistDemoShadowAsync  (SHADOW_ONLY, lots 1:1)          ✗ no converter
    →  CopyTradingService      (Normalize lots×0.05, SHADOW)    ✗ still lots, not ounces
                                                 ✗  IQuantityConverter missing
                                                 ✗  no 35=D / tag 38 on this hop
                                                 ✗  NewOrderSingleImplemented = false
                                                 ✗  VenueReconciled = false
                                                 ✗  persist AllowFixSend = false
                                                 ~  RealCopyEnabled may be true (env-bound)
tools/DemoFixTestTrade        (38="1000" literal)               ✗ not QuantityNormalizer
                                                 ✗  demo host/account only
                                                 ✗  already OrderSent=true (Filled=false)
```

Fetching 8460 logins does **not** arm copy send. Shadow `RequestedQuantity` is either lots×0.05 or lots 1:1. It is still not FIX. It **would** be a G7 defect the day a sender is added without `IQuantityConverter`.

---

## 7. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` `src` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty` / tag 38 / `35=D` / `cTrader` / `NewOrderSingle` (grep of those tokens under `src` = **0**).

Display/JSON uses classic Manager scale:

| File | Expression |
|---|---|
| `src\http\controllers\export_controller.cpp` | `deal.volume / 10000.0` |
| `src\http\controllers\journal_controller.cpp` | `deal.volume / 10000.0` |
| `src\http\controllers\symbol_controller.cpp` | `pos.volume / 10000.0` |
| `src\services\trade_service.cpp` L92, L117 | `deal.volume / 10000.0`, `pos.volume / 10000.0` |
| `src\services\worker_service.cpp` L547, L650 | `pos.volume / 10000.0`, `deal.volume / 10000.0` |

That is **MT5 ticks → lots for JSON**, not cTrader tag 38. Official SDK `SMTMath::VolumeToDouble` is the same `/10000`. Do **not** copy YoPips `volume/10000` into a FIX builder.

YoPips **does** send live MT5 dealer orders (`DealerSend` / `DealerSendOrder` in `mt5_manager.cpp` L1119–1144, `mt5_pool.cpp`). That is **in-broker Manager dealer**, not Pepperstone FIX. It is **out of scope** for this cTrader copy path and is **not** invoked by `D:\Prop` product code. Do not treat YoPips dealer volume as a dest `OrderQty` recipe.

---

## 8. Spec vs code (do not rubber-stamp)

| ID | Spec | Measured | Stance |
|---|---|---|---|
| A89 G7 / A43 E01 | 0.10 lots × 100 oz → BaseUnits `10.00` | `Normalize` → `0.10`; product `0.10×0.05` → `0` | **FAIL** passthrough / wrong unit |
| Product 5% on 1.00 lot | 5.00 oz after alloc | `0.05` | **FAIL** 100× small |
| E06 | same lots × mini 10 oz → `1.00` | still `0.10` | **FAIL** |
| E08 | Lots convention is the **only** `0.10→0.10` | always passthrough | **FAIL** |
| §4.5 | floor, never ceil | `Truncate` + `ToZero` | **PASS** last-stage (positive qty) |
| E14 | below min → do not send | returns `0m` (not `SIZE_BELOW_MIN`); product `continue`s | **PARTIAL** |
| E23 | cap then re-floor to step | returns raw `MaxQuantity` | **FAIL** (skipped test; live Fact locks raw max) |
| E21 | `confidence_scale ≤ 1` | no input | **MISSING** |
| CLOSE/REDUCE | dest remaining, not source × allocation | N/A | **MISSING** |
| A43 §6 | Risk + Shadow + FIX share one converter | copy hop uses last-stage only; demo store 1:1; demo tool hardcodes `1000` | **MISSING** |
| A100 G10 / §68 | conversion verified | skipped fixtures; `[ ] FAIL` L33 | **FAIL** |
| Copy-path tag 38 | only after converter + gates | no builder on that hop | **SAFE_BY_ABSENCE** |
| Whole-tree `35=D` | none until go-live | demo helper exists; `OrderSent=true` on demo | **RESIDUAL** (not this type) |

---

## 9. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** tick G10 or authorize treating env `REAL_COPY_EXECUTION_ENABLED=true` as a live-send permit.
- Does **not** print passwords, FIX password, or account secrets (demo account id already on disk in `DEMO_FIX_TEST_TRADE.json` is cited as a public artifact).
- Does **not** add a `35=D` sender.
- Does **not** re-run the live Manager probe; census is the 2026-08-18 measured dump (`utc` `2026-08-18T08:42:16Z`), group rows re-summed.
- Does **not** claim `QuantityNormalizer` is safe to wire into NOS as-is.
- Does **not** re-hash files or re-run xUnit.
- Does **not** claim slots 78/98 “unused” is still true.
- Does **not** claim slots 138/158 “zero `35=D` in product” is still true.
- Does **not** claim the demo helper is the copy pipeline, or that live account `1369850` was touched.

---

## 10. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire it from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = `requested_quantity` only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`. Remap `ExecutionIntent.VolumeLots` if that field stays lot-shaped. Replace `CopyTradingService` `lots × 0.05` and `PersistDemoShadowAsync` 1:1 with the same converter.

The demo helper must **not** become the copy sender. If it stays, keep the demo-host / `1369850` refuse gate and **do not** feed it `Normalize` output (or a hardcoded `1000`) as if that were ounces.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 11. Slot-178 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines, unchanged vs W500_18/138/158 quote) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`; live Facts lock this) |
| Product hop `lots × 0.05` | **YES** (`1.00 → 0.05 ≠ 5.00 oz`) |
| Blind lots → FIX `OrderQty` on the **copy** wire | **NO** (copy hop has no `35=D`) |
| Whole-tree `35=D` builder | **YES, residual** (`CTraderFixDemoTestTrade`; hardcoded `38=1000`; demo-only; `OrderSent=true` / `Filled=false`) |
| `IQuantityConverter` | **MISSING** (21 skipped conversion Facts + 0 type in `src/`) |
| Product callers of this type | **1** (`CopyTradingService`); demo store is a second **lots 1:1** writer |
| DI registration of the type | **none** (ad-hoc `new` inside copy service) |
| A89 G7 / A100 G10 | **FAIL** |
| `RealCopyEnabled` | **env-bound** (lab `.env` boolean `true`; copy sender still absent) |
| Copy live send | **OFF** (`SAFE_BY_ABSENCE` + const NOS flag + persist `AllowFixSend=false`) |
| Capital at risk from this class | **NONE** |
| Manager census (context, prior measure, re-summed) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |
| Catalog uses this class | **No** |

**One-line:** `QuantityNormalizer` blindly scales MT5 lots (`1.00×0.05=0.05 ≠ 5.00 oz`) on the SHADOW hop; it never reaches FIX; copy of ALL Achiever+Starwave traders stays SHADOW-only. Demo-tool `35=D` with `38=1000` is a separate residual, not this type.

[DO NOT] Treat last-stage floor (now product-called) as a verified §38 converter.
[DO NOT] Un-skip G7 before `IQuantityConverter`.
[DO NOT] Write `38=` from `MaxVolumeLots` or `Normalize(lots, 0.05, GoldSpec)`.
[DO NOT] Treat env `REAL_COPY_EXECUTION_ENABLED=true` as a send permit.
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
[DO NOT] Copy YoPips `volume/10000` into a FIX NOS builder.
[DO NOT] Claim “zero `35=D` in product” — `CTraderFixDemoTestTrade` exists.
[DO NOT] Wire `CTraderFixDemoTestTrade` (or its `38=1000`) into the copy hop.

---

*End of W500_RESEARCH_178. Product source was not modified.*
