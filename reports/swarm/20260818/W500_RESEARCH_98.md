# W500_RESEARCH_98 — `QuantityNormalizer` never blindly converts MT5 lots → FIX `OrderQty`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_98.md` |
| Slot | **98** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent product + test + FIX + catalog + YoPips re-read; **no** Manager/TLS re-attach; **no** xUnit re-run; **no** `Get-FileHash` this slot) |
| Agent | W500 research subagent, slot 98 |
| Topic | Check `QuantityNormalizer` never blindly converts MT5 lots to FIX `OrderQty` |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| SUT | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Spec | Architecture executive change #10 + §38 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L100, L1457–1492); A43; A38 (1 lot = 10_000); A89 #45/#47; A100 **G10** (`[ ] FAIL`); A89 **G7** (lots → ounces → dest; never passthrough) |
| Same-topic siblings (re-read SUT; not copied as proof) | `W500_RESEARCH_18.md`, `W500_RESEARCH_38.md`, `W500_RESEARCH_58.md`, `W500_RESEARCH_78.md`, `D18_qty.md`, `B17_qty_review.md` |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** (no Manager / proxy / FIX / DB password values) |
| Live `35=D` this pass | **No** (builder absent; flags forced false) |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full 31-line SUT. Conversion + last-stage + `ExecutionAndSizingTests`. `VolumeConverter`, `TradeReconstructor`, `ShadowCopyEngine`, `RiskEngine`, `EfTradingStore.PersistDemoShadowAsync`, `DealIngestionService.SyncCatalogAsync`, `NativeMt5BrokerConnector` group/user walk, `LiveMt5Registration`, `CTraderFixSession` (135 lines), `CTraderFixLogonHostedService`, `CTraderQuoteService`, `FixSimulationHarness` `(35,…)` set, DI, `apps/api/Program.cs`, `apps/fix-worker/Worker.cs`, `.env` flag names only, live census JSON header + Starwave `groupNames`. YoPips `volume/10000` + `DealerSend` (MT5 dealer, not FIX). |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest — do not collapse two facts)

**`EXISTS_NEEDS_REFACTOR` as a last-stage dest min/step/max floor. `MISSING` as the §38 / A43 `IQuantityConverter`. A89 G7 / A100 G10 remain FAIL. Live FIX `OrderQty` is `SAFE_BY_ABSENCE`, not a proven converter.**

| Question | Measured this slot |
|---|---|
| Does `QuantityNormalizer` convert lots → ounces → dest `OrderQty`? | **No.** `raw = sourceLots * allocationFactor`. With `allocationFactor = 1` it **is** a lots passthrough (`0.10 → 0.10`, **not** `10.00`). |
| Does any product path write that number as FIX tag 38? | **No.** Zero product callers. Zero `35=D` / `OrderQty` / `38=` builders in product `*.cs`. `LiveRuntimeStatus.RealCopyEnabled` forced **false** in DI (`DependencyInjection.cs` L41) and again after logon (`CTraderFixLogonHostedService.cs` L68). API `FEATURE_COPY_TRADING_ENABLED` is a **literal `false`**. |
| Does Manager catalog of ALL groups/traders go through this class? | **No.** `GroupRequestArray("*")` / `UserRequestArray` / `UserLogins` only. |
| Can copy-to-cTrader take a live loss today via this type? | **No.** No NewOrderSingle method. The only CopyIntent writer is `SHADOW_ONLY`. |

So the class **does not emit live FIX `OrderQty` today**. That is **not** the same as “never blindly converts.” If anyone later wires `Normalize(sourceLots, 1, dest)` into a NewOrderSingle, **0.10 MT5 lots becomes 0.10 dest qty**. A43 E01 / G7 requires **10.00** on a BaseUnits XAU book (`0.10 lots × 100 oz/lot ÷ 1 oz/unit`). `0.10` would be **100× too small** (or rejected as below min).

Passing unit Facts **lock the passthrough**. The binding “never passthrough” Fact is **`[Fact(Skip = …)]`**.

**Risk to capital from this type: NONE.** No socket write of tag 38 exists. Fetching the prior-measured 8460 manager logins cannot open a Pepperstone/cTrader position through this class.

**One-liner:** `QuantityNormalizer` would blindly passthrough MT5 lots as dest qty when `allocation=1`, but it never reaches FIX; no live `OrderQty` can be sent.

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

Lots convention is the **only** legal case where `0.10 → 0.10` (A43 E08). That mapping is **not** on `InstrumentQuantitySpec`.

A100 **G10** (`position sizing conversion is verified`) is still **`[ ] FAIL`**. Checkbox stays unchecked until `Never_passthrough_MT5_lots` is a live Fact against `IQuantityConverter.Convert`.

A100 working checklist L33 (remeasured this slot):

```text
[ ] FAIL  G10  position sizing conversion is verified
```

Do not confuse A89 **G7** (converter never-passthrough) with A100 **G07** (cTrader reconciliation after restart). Both are FAIL. This slot owns the **converter** question.

---

## 2. Measured SUT (re-read this slot, 31/31 lines)

| | |
|---|---|
| Path | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` |
| Lines | **31** (full file re-read this slot; identical to the W500_18 / D18 / W500_58 / W500_78 dump) |
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

Absent from the type (required by A43):

| Input | On `InstrumentQuantitySpec` / `Normalize`? |
|---|---|
| MT5 ticks / `VolumeConverter` | **No** |
| `contract_size` | **No** |
| `QuantityConvention` (BaseUnits / Lots / Unverified) | **No** — `grep` of `QuantityConvention` / `IQuantityConverter` / `contract_size` under `D:\Prop\src\**\*.cs` = **0** |
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

## 3. Product callers: zero (remeasured)

`grep` `QuantityNormalizer` / `new QuantityNormalizer` over product `*.cs`:

| Tree | Hits |
|---|---|
| `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` | definition only (`public sealed class QuantityNormalizer`) |
| `D:\Prop\src\Application` | **0** |
| `D:\Prop\src\Infrastructure` | **0** |
| `D:\Prop\src\Fix.CTrader` | **0** |
| `D:\Prop\src\Mt5` | **0** |
| `D:\Prop\apps` | **0** |
| DI `AddTraderIntelligence` | registers reconstructor + scorer + ingest + FIX logon host; **not** `QuantityNormalizer` / `RiskEngine` / `ShadowCopyEngine` |
| `new QuantityNormalizer` | **tests only** (`ExecutionAndSizingTests` L37, `SourceDestinationQuantityConversionTests`, `QuantityNormalizerStepMinMaxTests`) |

Adjacent engines do **not** call it:

| Type | What it does with quantity | Calls `QuantityNormalizer`? |
|---|---|---|
| `RiskEngine.Evaluate` | `ApprovedQuantity = request.RequestedQuantity` (approve/reduce); reject → `0`. `AllowFixSend` needs `RealExecutionEnabled && kill=None && Reconciled && VenueHealthy` | **No** |
| Product `new RiskEngine` / `RiskEngine.Evaluate` | **0** hits under `D:\Prop\src` + `apps` (only `tests/Unit/RiskEngineTests.cs` constructs it). `apps/api/Controllers/SettingsController.cs` uses a **settings DTO** named `RiskEngine`, not the domain class | **No** |
| `ShadowCopyEngine.SimulateEntry/Exit` | copies the `quantity` argument onto `ShadowFill` unchanged (`Quantity = quantity`) | **No** |
| `EfTradingStore.PersistDemoShadowAsync` | gated on `state == TraderState.SHADOW`; `RequestedQuantity = trade.MaxVolumeLots` (1:1 lots), `Status = "SHADOW_ONLY"` | **No** |
| `BaselineScorer` | uses `MaxVolumeLots` for martingale/size-up **features** only | **No** |
| `ExecutionIntent` | field named `VolumeLots` (lots, not dest units) | **no product writer** (`new ExecutionIntent` = 0 in `src`) |
| `DealIngestionService.SyncCatalogAsync` | groups + accounts only (`GetGroupsAsync` + `GetAccountsAsync(null)`) | **No** |

Shadow persist (lots copied into a **SHADOW_ONLY** intent — not FIX):

```267:319:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
        if (state != TraderState.SHADOW)
        {
            await _db.SaveChangesAsync(ct);
            return;
        }
        // ...
            var intent = new CopyIntent
            {
                // ...
                RequestedQuantity = trade.MaxVolumeLots,
                Status = "SHADOW_ONLY",
                IdempotencyKey = key
            };
            // SimulateEntry(..., trade.MaxVolumeLots, ...)
```

That 1:1 lots echo is a **G7 defect waiting for a sender**. It is still not tag 38.

`RiskEngine` is not in DI. Even if a caller appeared, it would pass `RequestedQuantity` through unchanged (`RiskEngine.cs` L158, L168). `AllowFixSend` is a boolean on a record that **no product path reads**.

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

**21** `[Fact]`/`[Theory]` methods are `Skip = "A43 … IQuantityConverter missing"` (counted this slot via `Skip =` grep):

- `Never_passthrough_MT5_lots` — *“0.10 MT5 lots × 100 oz → BaseUnits OrderQty 10.00, not 0.10.”*
- `Known_lot_to_OrderQty_examples` — 9-row table (ticks × contract × convention)
- `Mini_and_nano_contracts_differ`
- `Lots_convention_only_when_mapped`
- `Shadow_and_live_share_converter` — *“QuantityNormalizer is unused by ShadowCopyEngine and RiskEngine”*
- `Fix_worker_does_not_rescale` — *“No FIX NOS builder consumes QuantityNormalizer output”*

Those skips are the **honest** G7 lock. Do not un-skip until `IQuantityConverter.Convert` exists. Bodies `Assert.Fail` / `true.Should().BeFalse(...)` so they cannot be un-skipped onto a test-local ounces helper.

### 4.2 Last-stage floor suite — `QuantityNormalizerStepMinMaxTests.cs`

162 lines. Header states it **does not** cover lots→ounces→OrderQty. Passing cases include `0.10m, 1m, 0.10m` (passthrough on a 0.01 step). One skip remains: A43 E23 (`Above_max_re_floors_to_step` expects `5.00`, SUT returns raw `MaxQuantity` `5.09`).

`Allocation_greater_than_one_is_currently_accepted` documents `1 × 1.5 = 1.50` — A43 requires `allocationFactor ∈ (0, 1]`.

`Allocation_scales_before_step` expects `0.10 × 0.10 = 0.01` (correct last-stage math; not dest conversion).

### 4.3 `ExecutionAndSizingTests.Quantity_normalizer_steps_and_min`

```35:41:D:\Prop\tests\Unit\ExecutionAndSizingTests.cs
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
```

Again: **0.10 lots in → 0.10 out**.

This slot did **not** re-run `dotnet test`. Arithmetic is visible in the SUT (`0.10 * 1` on a 0.01 step is 0.10). The green Facts lock that identity.

---

## 5. Live FIX `OrderQty`: impossible in current code

`grep` `OrderQty` / `38=` / `35=D` / `NewOrderSingle` under `D:\Prop\src\**\*.cs`:

| Pattern | Hits |
|---|---|
| `OrderQty` / `38=` | **0** in product |
| `35=D` / `(35, "D")` | **0** in product and in `Fix.CTrader` (including `Testing/`) |
| `(35, …)` present | Logon `"A"`; quote `"y"` / `"V"`; harness `"A"` / `"3"` / `"0"` / `"y"` / `"X"` / `"8"` only |

`CTraderFixSession.TryLogonAsync` writes **only** `35=A` (Logon) via `BuildLogon` L96. After TLS handshake it reads **one** reply and returns; `using` disposes the socket. There is no NewOrderSingle method, no keep-alive TRADE writer, no tag 38.

`CTraderQuoteService` outgoing types: `35=y` (SecurityListRequest L113), `35=V` (MarketDataRequest L127). Not `D`. `BuildMarketDataRequestTags` sets 55/263/264 only.

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

`CTraderFixOptions.RealCopyExecutionEnabled` default **`false`** (L35).

`apps/fix-worker/Worker.cs`: stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."` Even if config `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still refuses** (L45–46) — there is no builder to call.

`GET /api/settings` (`apps/api/Program.cs` L73–76):

```text
REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled   // pinned false
FEATURE_COPY_TRADING_ENABLED = false                    // literal
```

`GET /api/reconciliation/status` note: *“NewOrderSingle still off”*.

`.env` names only (values are flags, not secrets): `REAL_COPY_EXECUTION_ENABLED=false` (L73), `FEATURE_COPY_TRADING_ENABLED=false` (L106). The env token `REAL_COPY_EXECUTION_ENABLED` is **not** bound onto `CTraderFixOptions` (wrong key vs `CTrader:RealCopyExecutionEnabled`). Safety is **pin-false + SAFE_BY_ABSENCE**, not a single named choke that a unit test proves refuses `35=D` on a logged-on TRADE socket.

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

`LiveMt5Registration.CreateConnectors` (`L23–47`): Achiever may set `ProxyEnabled` from `ACHIEVER_PROXY_*`; Starwave **`ProxyEnabled = false`** hard pin. This slot does **not** print proxy host/user/password.

Measured census (`LIVE_GROUPS_AND_TRADERS.json` probe `2026-08-18T08:42:16Z`; `LIVE_MANAGER_FETCH_MEASURED.md`; `CREDENTIALS_AND_COPY_STATUS.md`). Values are counts, not secrets. **This slot did not re-attach live Manager.**

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| Achiever | OK via whitelist HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| StarwaveFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (all this manager can see; JSON `groupNames`): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (JSON `STARWAVEFX.groupNames`, re-read this slot at offset 45644): `Starwave\cent\FX1\grp1` 11, `grp2` 4; `demo\FX2\grp1` 170, `grp2` 1735; `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

Copy-to-cTrader **no-loss** for this slot:

```text
Manager catalog (18 / 8460)  →  reconstruct / score  →  SHADOW_ONLY CopyIntent (lots 1:1)
                                                 ✗  QuantityNormalizer unused
                                                 ✗  IQuantityConverter missing
                                                 ✗  no 35=D / tag 38
                                                 ✗  RealCopyEnabled = false (DI + post-logon pin)
```

Fetching 8460 logins does **not** arm send. Shadow `RequestedQuantity = MaxVolumeLots` is a **demo/ledger** 1:1 lots copy. It is still not FIX. It **would** be a G7 defect the day a sender is added without `IQuantityConverter`.

---

## 7. YoPips C++ backend (contrast, not a converter)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm` `src` has **no** `QuantityNormalizer`, **no** `IQuantityConverter`, **no** FIX `OrderQty` / tag 38 / `35=D` / `cTrader`.

Display/JSON uses classic Manager scale:

| File | Expression |
|---|---|
| `src\http\controllers\export_controller.cpp` | `deal.volume / 10000.0` |
| `src\http\controllers\journal_controller.cpp` | `deal.volume / 10000.0` |
| `src\http\controllers\symbol_controller.cpp` | `pos.volume / 10000.0` |
| `src\services\trade_service.cpp` L92, L117 | `deal.volume / 10000.0`, `pos.volume / 10000.0` |
| `src\services\worker_service.cpp` | `pos.volume / 10000.0`, `deal.volume / 10000.0` |

That is **MT5 ticks → lots for JSON**, not cTrader tag 38. Official SDK `SMTMath::VolumeToDouble` is the same `/10000` (`MT5APIMath.h`). Do **not** copy YoPips `volume/10000` into a FIX builder.

YoPips **does** send live MT5 dealer orders (`DealerSend` / `DealerSendOrder` in `mt5_manager.cpp`, `mt5_pool.cpp`). That is **in-broker Manager dealer**, not Pepperstone FIX. It is **out of scope** for this cTrader copy path and is **not** invoked by `D:\Prop` product code. Do not treat YoPips dealer volume as a dest `OrderQty` recipe.

---

## 8. Spec vs code (do not rubber-stamp)

| ID | Spec | Measured | Stance |
|---|---|---|---|
| A89 G7 / A43 E01 | 0.10 lots × 100 oz → BaseUnits `10.00` | `Normalize` → `0.10` | **FAIL** passthrough |
| E06 | same lots × mini 10 oz → `1.00` | still `0.10` | **FAIL** |
| E08 | Lots convention is the **only** `0.10→0.10` | always passthrough | **FAIL** |
| §4.5 | floor, never ceil | `Truncate` + `ToZero` | **PASS** last-stage (positive qty) |
| E14 | below min → do not send | returns `0m` (not `SIZE_BELOW_MIN`) | **PARTIAL** |
| E23 | cap then re-floor to step | returns raw `MaxQuantity` | **FAIL** (skipped test) |
| E21 | `confidence_scale ≤ 1` | no input | **MISSING** |
| CLOSE/REDUCE | dest remaining, not source × allocation | N/A | **MISSING** |
| A43 §6 | Risk + Shadow + FIX share one converter | unused + no NOS | **MISSING** |
| A100 G10 / §68 | conversion verified | skipped fixtures; `[ ] FAIL` | **FAIL** |
| Live tag 38 | only after converter + gates | no builder | **SAFE_BY_ABSENCE** |

---

## 9. What this slot does **not** claim

- Does **not** claim EX5 / YoPips is a cTrader sizer.
- Does **not** claim `Never_passthrough_MT5_lots` is green.
- Does **not** tick G10 or authorize `REAL_COPY_EXECUTION_ENABLED=true`.
- Does **not** print passwords, FIX password, or account secrets.
- Does **not** add a `35=D` sender.
- Does **not** re-run the live Manager probe; census is the 2026-08-18 measured dump (`utc` `2026-08-18T08:42:16Z`).
- Does **not** claim `QuantityNormalizer` is safe to wire into NOS as-is.
- Does **not** re-hash files or re-run xUnit.

---

## 10. Binding next implementation (not done here)

A43 §3 / §14: implement **one** `IQuantityConverter` in Domain. Keep `QuantityNormalizer` (or rename `QuantityStep.Floor`) as the **last two lines** after ounces math. Wire it from Risk step 10 and Shadow **before** any NOS. FIX tag 38 = `requested_quantity` only. Un-skip `Never_passthrough_MT5_lots` first; do not grow ounces math inside `Fix.CTrader`. Remap `ExecutionIntent.VolumeLots` if that field stays lot-shaped.

Until that converter exists and G10 is measured PASS, live copy stays off. That is how this process avoids a sizing-induced live loss.

---

## 11. Slot-98 scorecard

| Check | Result |
|---|---|
| `QuantityNormalizer` is dest-grid only | **YES** (31 lines, unchanged vs W500_18/58/78 quote) |
| Blind lots → dest qty when allocation=1 | **YES** (`0.10 → 0.10`; live Facts lock this) |
| Blind lots → FIX `OrderQty` on the wire | **NO** (unused + no `35=D`) |
| `IQuantityConverter` | **MISSING** (21 skipped conversion Facts + 0 type in `src/`) |
| Product callers | **0** |
| DI registration | **none** (`QuantityNormalizer` / `RiskEngine` / `ShadowCopyEngine` not registered) |
| A89 G7 / A100 G10 | **FAIL** |
| Live send | **OFF** (`SAFE_BY_ABSENCE` + `RealCopyEnabled=false`) |
| Capital at risk from this class | **NONE** |
| Manager census (context, prior measure) | Achiever 8/6512 + Starwave 10/1948 = **18 / 8460** |
| Catalog uses this class | **No** |

**One-line:** `QuantityNormalizer` would blindly passthrough MT5 lots as dest qty, but it never reaches FIX; no live `OrderQty` can be sent. Copy of ALL Achiever+Starwave traders stays SHADOW-only.

[DO NOT] Treat unused last-stage floor as a verified §38 converter.
[DO NOT] Un-skip G7 before `IQuantityConverter`.
[DO NOT] Write `38=` from `MaxVolumeLots` or `Normalize(lots, 1, dest)`.
[DO NOT] Enable live copy until G10 + §70 conjunction PASS.
[DO NOT] Copy YoPips `volume/10000` into a FIX NOS builder.

---

*End of W500_RESEARCH_98. Product source was not modified.*
