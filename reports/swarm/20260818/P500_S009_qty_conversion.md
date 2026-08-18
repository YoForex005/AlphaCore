# P500_S009 — Quantity conversion (lots ↛ FIX tag 38)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S009_qty_conversion.md` |
| Agent | P500_S009 (qty conversion / last-stage floor vs §38 converter) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Binding law | Architecture executive #10, §38; `A43_position_sizing.md`; `A38` (1 lot = 10 000); `A32` tag 38 |
| SUTs | `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs`; `D:\Prop\src\Domain\Volume\VolumeConverter.cs` |
| Volume docs | `D:\Prop\docs\xauusd-normalization.md`; Architecture §38; `A38_mt5_volume_units.md`; `A81_volume_unit_conflict.md`; `B14_volume_review.md`; `D14_volume.md` |
| Prior reviews | `A43_position_sizing.md` (binding design); `B17_qty_review.md`; `D18_qty.md`; `A006_volume_scoring_risk.md`; `W500_RESEARCH_38` / `_58` |
| Remeasured | 2026-08-18 this pass (`read_file` + `grep`) — copy pipeline now calls the normalizer |

---

## Verdict (this pass)

**Lots still do not become FIX tag 38.** `VolumeConverter` is source ticks → lots (`÷ 10_000`). `QuantityNormalizer` is a last-stage dest grid. `IQuantityConverter` is still **MISSING** (`grep IQuantityConverter` under `src/` = 0). Product `*.cs` still has **0** `OrderQty` / `(35, "D")` builders.

**Drift vs first draft:** `QuantityNormalizer` is no longer unused. `CopyTradingService` (`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`) now does `Normalize(trade.MaxVolumeLots, AllocationFactor=0.05, GoldSpec(0.01, 5, 0.01, 2))`. That is **source lots × 0.05**, floored to 0.01, capped at **5** lots — still **not** ounces → dest convention. `PersistDemoShadowAsync` still copies `trade.MaxVolumeLots` 1:1.

## Profit implication

Copying the source lot number (or 5% of it) onto Pepperstone is a **loss vector** if dest `38` is ounces, and a **reject vector** if dest min is missed after the 0.05 haircut. Synthesis dest working cap is **0.05 lot**, not `GoldSpec.MaxQuantity = 5`. Wrong qty is how a “filtered” book still blows the account. Do not send until A43 ounces math exists.

---

## 0. One-line law

```text
source 0.10 MT5 lots  ≠  destination FIX OrderQty (tag 38) 0.10
```

Never emit tag `38` from a source lot number. Never emit tag `38` from a scoring “suggested size.” Convert through **canonical XAU ounces**, apply a **tiny** `allocationFactor` plus caps, then **quantize down** to a **measured** destination instrument spec.

Wrong quantity is two independent capital vectors:

| Vector | Mechanism | Outcome |
|---|---|---|
| **Loss (oversize)** | Units or scale wrong in the *large* direction (e.g. treat `Volume()` ticks as hundredths; apply `× 100` oz when dest convention is already `Lots`; `allocationFactor = 1` on a funded book) | Position too large vs source / vs risk book |
| **Reject (below min)** | Units or scale wrong in the *small* direction, or floor + tiny allocation lands under dest `min_qty` | Venue reject, or silent `0` that must never become a send |

Today both vectors are **SAFE_BY_ABSENCE** on the wire: no product sender consumes `QuantityNormalizer`, and `Fix.CTrader` has **zero** `OrderQty` / tag `38` construction.

---

## 1. Verdict (honest)

| Question | Answer |
|---|---|
| Does `QuantityNormalizer` convert MT5 lots → FIX `38`? | **No.** Last-stage dest grid only (`sourceLots × allocationFactor`, floor step, min→`0`, max→raw cap). |
| Does `VolumeConverter` convert to OrderQty? | **No.** Source ticks ↔ lots only (`÷ 10_000` Manager default). |
| Is `IQuantityConverter` implemented? | **MISSING.** A43 design exists; Domain types for ounces / convention / dest spec are absent. |
| Wired to a FIX / copy sender? | **No.** Product callers of `QuantityNormalizer` = **zero**. |
| Live tag 38 possible from this path? | **No.** `OrderQty` / `38=` = **0 hits** in product `*.cs`. `RealCopyExecutionEnabled` defaults **false**. |
| Capital risk from this class today | **NONE** (`SAFE_BY_ABSENCE`). |
| G7 / G10 / §68 sizing checkbox | **FAIL / unchecked.** Passing Facts lock `0.10 → 0.10` (want `10.00` on BaseUnits). |
| Classification (§73.B) | **EXISTS_NEEDS_REFACTOR** as dest-grid floor; **MISSING** as the §38 / A43 converter. |

Profit path (when a sender is eventually wired) is **not** “copy the lot number.” It is:

```text
SecurityList (discover dest instrument id)
    + operator-measured dest spec (min / step / max / convention)   // NOT on SecurityList
    + source contract_size (never assume 100)
    + VolumeConverter.Manager (ticks → lots)
    + ounces math
    + tiny allocationFactor (and confidence ≤ 1)
    + QuantityNormalizer last-stage floor
    → ExecutionIntent.requested_quantity
    → FIX 38   (worker must not rescale)
```

---

## 2. What exists

### 2.1 `VolumeConverter` — source ticks only

Path: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

| Constant | Value | Meaning |
|---|---:|---|
| `ManagerVolumeScale` | `10_000` | `IMTDeal::Volume()` / `SMTMath::VolumeToDouble`. **Product default.** |
| `ExtendedVolumeScale` | `100_000_000` | `VolumeExt()` only. Not used by extractors. |
| `HundredthsScale` | `100` | MT4 / wrong `mt5_types.h` comment. **Must not be default.** |

```text
ToLots(native)   = native / Scale
ToNative(lots)   = Round(lots × Scale, 0, AwayFromZero)
default ctor     = Manager (10_000)
```

`TradeReconstructor` injects `volume ?? VolumeConverter.Manager`. Reconstruction lots are therefore classic Manager lots. This is the **first** arrow in the A43 pipeline, not the last.

Locked by `D:\Prop\tests\Unit\VolumeConverterTests.cs`:

- `1000` native → `0.10` lots
- Extended `1.00` lot → `100_000_000`
- Manager scale ≠ hundredths

Wrong scale blast radius (if someone later wires send from reconstructed lots):

| Mistake | 1 000 classic ticks become | vs truth `0.10` lots |
|---|---:|---|
| `÷ 100` (hundredths) | `10.00` lots | **100× oversize** (loss) |
| `÷ 100_000_000` (ext on classic) | `0.00001` lots | **10 000× undersize** → dest min reject |
| `÷ 10_000` on a true `VolumeExt` integer | `10_000` lots for 1.00 lot | **10 000× oversize** (loss) |

A13 / A38 / B14 / D14: extractors copy `Volume()`, not `VolumeExt()`. Do not flip the default.

### 2.2 `QuantityNormalizer` — last-stage dest grid only

Path: `D:\Prop\src\Domain\Execution\QuantityNormalizer.cs` (31 lines)

```text
Normalize(sourceLots, allocationFactor, dest):
    reject sourceLots ≤ 0, allocationFactor ≤ 0, dest.StepSize ≤ 0
    raw   = sourceLots * allocationFactor
    qty   = Truncate(raw / dest.StepSize) * dest.StepSize
    qty   = Round(qty, dest.Precision, ToZero)
    if qty < dest.MinQuantity → 0
    if qty > dest.MaxQuantity → dest.MaxQuantity     // NOT FloorToStep(max)
    else                      → qty
```

`InstrumentQuantitySpec` is `(MinQuantity, MaxQuantity, StepSize, Precision)` only. No `contract_size`, no `QuantityConvention`, no `unit_size_oz` / `lot_size_oz`, no `spec_status`, no confidence, no margin, no mapped dest position.

What it **does** (A43 §4.5 tail, when the *input is already dest units*):

- Floor, never ceil (PASS for `qty > 0`)
- Below min → `0` (PARTIAL: not `SIZE_BELOW_MIN`; a sender must treat `0` as do-not-send)
- Allocation scales **before** step (so a tiny factor can legally collapse to `0`)

What it **does not** do (G7 / A43 converter):

- ticks → lots (that is `VolumeConverter`)
- lots × `contract_size` → ounces
- ounces ÷ dest unit → OrderQty
- `BaseUnits` vs `Lots` vs `Unverified`
- `confidence_scale ≤ 1`
- book / margin re-quantize
- REDUCE/CLOSE from mapped dest qty
- write FIX tag 38

Measured passthrough (A43 E01 / G7), locked by passing Facts:

```text
Normalize(0.10, allocation=1, Dest_BaseUnits min=0.01 step=0.01 max=5000)
    = 0.10
    ≠ 10.00
```

On a Spotware-style BaseUnits XAU book (`1` OrderQty = `1` oz, `0.10` MT5 lots × `100` oz/lot = `10` oz), `38=0.10` is **100× too small** (reject / dust), not a 1:1 copy.

On a `Lots` book with `lot_size_oz = 100`, `0.10` **would** be correct — **only** if that convention is a persisted measured mapping (A43 E08). The current helper cannot tell the two books apart.

Known last-stage defects vs A43 (do not “fix” by un-skipping tests first):

| Id | Spec | Code |
|---|---|---|
| E23 | After dest max, `q = FloorToStep(max, step)` | Returns **raw** `MaxQuantity` (`5.09` kept when step is `0.10`) — skipped test |
| E21 | `allocationFactor` / confidence must be in `(0, 1]` | `1.5` is accepted and **enlarges** size |
| E14 / E20 | Below min → `REJECT` / no send | Returns `0m` (safe only if every caller checks) |

### 2.3 Call graph — not wired to a sender

`grep` of `QuantityNormalizer` / `new QuantityNormalizer` (this pass):

| Location | Role |
|---|---|
| `src\Domain\Execution\QuantityNormalizer.cs` | definition |
| `src\Infrastructure\Copy\CopyTradingService.cs` | **product caller** — `AllocationFactor = 0.05m`, `GoldSpec` min 0.01 / max **5** / step 0.01 |
| `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | last-stage floor Facts |
| `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | G7 passthrough Facts + skipped A43 converter (skip text still says unused — **stale**) |
| `tests\Unit\ExecutionAndSizingTests.cs` | `0.10 → 0.10` / `0.10×0.05 → 0` / floor `0.333 → 0.33` |

`RiskEngine.Evaluate` still consumes a pre-baked `RequestedQuantity` and does not quantize. `ShadowCopyEngine` copies the quantity it is given. Copy path now feeds the normalizer **lots**, not ounces.

Shadow demo path (`EfTradingStore.PersistDemoShadowAsync`) is the closest “size” wire — and it is **1:1 lots**, not dest units:

```text
CopyIntent.RequestedQuantity = trade.MaxVolumeLots
ShadowCopyEngine.SimulateEntry(..., trade.MaxVolumeLots, ...)
Status = "SHADOW_ONLY"
```

That is a **unit bug waiting for a sender**, not a live FIX order. Status is shadow-only.

FIX TRADE:

- `OrderQty` / `38=` in product C#: **0**
- `35=D` / NewOrderSingle builder: **MISSING** (`A003_fix_noloss.md`)
- `CTraderFixOptions.RealCopyExecutionEnabled` default **false**
- `LiveRuntimeStatus.RealCopyEnabled` set **false** in DI and on FIX logon host

**SAFE_BY_ABSENCE** = no class can put a wrong (or right) qty on the wire today.

---

## 3. Binding design (A43) — not implemented

Source of truth: `D:\Prop\reports\swarm\20260818\A43_position_sizing.md`  
Architecture: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` executive #10 + §38.

Three unit systems share the word “volume” and almost never share a scale:

| Layer | Unit | Typical `0.10` MT5 lots (contract 100) |
|---|---|---|
| Source MT5 `Volume()` | ulong ticks, `÷ 10_000` | `1 000` ticks = `0.10` lots |
| Canonical | troy ounces | `0.10 × contract_size` → **`10` oz** (or `1` oz if mini/`10`) |
| Dest FIX `38` | dest units | BaseUnits 1 oz → **`10.00`**; Lots 100 oz → **`0.10`** |

Legal pipeline (A43 §2):

```text
source volume (ulong ticks)
    ↓  ÷ 10 000                         VolumeConverter.Manager
source lots
    ↓  × source_contract_size           never assume 100
canonical ounces
    ↓  × min(risk_allocation, remaining_caps) × confidence_scale
allocated ounces                        allocationFactor MUST be tiny and ≤ 1
    ↓  ÷ dest_unit_size_oz              convention from measured dest spec
pre-round destination qty
    ↓  floor to destination step        QuantityNormalizer (once input is dest units)
    ↓  enforce destination min / max
    ↓  re-check book / margin (may reduce, then re-quantize)
requested_quantity  →  ExecutionIntent  →  FIX 38
```

`OPEN` / `INCREASE` run this on **incremental** source volume.

`REDUCE` / `CLOSE` **do not** re-run source lots through allocation. Qty comes from the **mapped destination position** (A43 §4.7). Prior OPEN may have discarded remainder; dest size is the book of record.

Scoring / ML may emit `suggested_allocation` and `confidence` only. They never emit OrderQty.

### 3.1 Why SecurityList is necessary — and not sufficient

`CTraderQuoteService` discovers XAUUSD via SecurityList (`35=y` response):

| Tag | Field | Used today |
|---|---|---|
| `55` | numeric instrument id | stored; required on later MD / NOS |
| `1007` | symbol name | match `XAUUSD` |
| `1008` | price digits | specified in A32 / A43; not a qty field |

Security List **does not** publish min, step, max, lot size, or unit convention (A32 / A43 §1.3). Those fields are **operator-measured** and persisted on `destination_symbols` with `spec_status = Measured`. A newly discovered row stays `Unverified` → converter **REJECT** / `DEST_QTY_SPEC_UNVERIFIED`. Guessing Pepperstone min from another cTrader broker is the same class of bug as hard-coding instrument id (`docs/xauusd-normalization.md`: cTrader IDs are discovered, never hardcoded).

Profit-path dest spec (must exist before any `38=`):

| Field | Why |
|---|---|
| `instrument_id` (55) | from SecurityList |
| `quantity_convention` | `BaseUnits` or `Lots` — never assumed |
| `unit_size_oz` or `lot_size_oz` | converts ounces → OrderQty |
| `min_qty` / `step_qty` / `max_qty` | last-stage grid; step must be ≥ FIX 0.01 |
| `spec_status = Measured` | evidence_ref (demo `38=1` vs `38=0.01` P&L check) |

### 3.2 Tiny `allocationFactor`

`QuantityNormalizer.allocationFactor` is the **last** scale knob the helper understands. A43 requires:

```text
scale        = risk_allocation × confidence_scale     // both in (0, 1]
allocated_oz = canonical_oz × scale
```

Rules:

- Default must **not** be “copy 1:1 lots.”
- Confidence cannot increase size (`> 1` is illegal; today’s helper accepts `1.5`).
- Profit / prop path wants a **small** factor so dest size is a slice of source ounces, then floor. Example: source `0.10` lots × `100` oz = `10` oz; `allocationFactor = 0.05` → `0.50` oz → BaseUnits `0.50` (or reject if dest min is `1.00`).
- After floor, `qty < min` → **do not send** (reject vector). Do not round up to min (that would be an unapproved increase).

A 1:1 passthrough (`allocationFactor = 1` on lots, no ounces) is both a **convention bug** and a **risk-book bug**.

---

## 4. Loss vector vs reject vector (numeric)

Assume source `0.10` lots, `contract_size = 100`, dest BaseUnits `unit_size_oz = 1`, min `0.01`, step `0.01`. Truth: **`38=10.00`**.

| Implementation | Tag 38 | Vector |
|---|---|---|
| Blind lots (`Normalize(0.10, 1, dest)`) | `0.10` | **100× undersize** — may fill as dust or sit at min; economic mismatch |
| Blind `× 100` on a **Lots** dest that wanted `0.10` | `10.00` | **100× oversize** — **loss** |
| `Volume / 100` then passthrough | `10.00` lots-as-qty | oversize **and** unit-confused |
| Tiny alloc `0.0005` on `10` oz | `0.005` → floor `0` | **reject / no-send** (correct if policy is fail-closed) |
| Round **up** `0.005` to dest min `0.01` | `0.01` | unapproved increase (forbidden) |
| Unaligned max `5.09` kept | `5.09` | venue **reject** (not multiple of step) |
| Mini contract `10` oz/lot treated as `100` | `10.00` instead of `1.00` | **10× oversize** — **loss** |

Do not “fix” G7 by multiplying by 100 in `Fix.CTrader`. Convention is a **measured mapping fact**. The FIX worker must not rescale (`A43` rule 11). One converter in Domain / Application (`IQuantityConverter`).

---

## 5. Tests (honest lock)

| File | What it proves |
|---|---|
| `tests\Unit\VolumeConverterTests.cs` | Manager `1000 → 0.10` lots; hundredths is not default |
| `tests\Unit\Sizing\QuantityNormalizerStepMinMaxTests.cs` | Floor / min→0 / max cap / alloc-before-step. Skip: E23 re-floor |
| `tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs` | **Passing Facts lock passthrough** `0.10→0.10`. 21 skips = missing `IQuantityConverter` |
| `tests\Unit\ExecutionAndSizingTests.cs` | Same last-stage helper; `0.10×0.05→0` |

Do **not** un-skip G7 first. Un-skip after `IQuantityConverter.Convert` exists and E01–E50 (or equivalent) pass.

`docs/xauusd-normalization.md` only covers **symbol** aliases (`XAUUSD`, `XAUUSDm`, `GOLD`, …) and discovered cTrader IDs. It does **not** specify qty units. Qty law lives in Architecture §38 + A43.

---

## 6. Profit-path checklist (implementation later — not this task)

```text
[ ] destination_symbols row for Pepperstone XAUUSD
      instrument_id from SecurityList (55 / 1007 / 1008)
      convention + min/step/max Measured with evidence_ref
      Unverified cannot go live
[ ] source_spec.contract_size per (broker, symbol) — never assume 100
[ ] IQuantityConverter is the only ticks → 38 path
[ ] VolumeConverter.Manager for classic Volume() ticks
[ ] allocationFactor tiny and ≤ 1; confidence cannot enlarge
[ ] QuantityNormalizer applied only to dest units (last stage)
      plus E23: FloorToStep after max
[ ] below min → SIZE_BELOW_MIN, zero FIX
[ ] REDUCE/CLOSE from mapped dest qty (A74 link required)
[ ] FIX worker copies requested_quantity into 38; no rescale
[ ] Shadow and live share the same converter
[ ] Never_passthrough_MT5_lots fails a deliberate 0.10→0.10 implementation
[ ] §68 “position sizing conversion is verified” still unchecked until the above
```

Until then: **design complete (A43), converter absent, dest spec unverified, wire empty.**

---

## 7. Cross-references

| Doc | Path |
|---|---|
| Architecture #10 / §38 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| A43 design | `D:\Prop\reports\swarm\20260818\A43_position_sizing.md` |
| A38 volume units | `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md` |
| A32 FIX tag 38 | `D:\Prop\reports\swarm\20260818\A32_ctrader_fix_specification.md` |
| A003 no live NOS | `D:\Prop\reports\swarm\20260818\A003_fix_noloss.md` |
| B17 / D18 re-measure | `B17_qty_review.md`, `D18_qty.md` |
| Symbol aliases only | `D:\Prop\docs\xauusd-normalization.md` |

---

## 8. This slice

Read-only. Product not modified. Report only.

Honest state: **`VolumeConverter` is the source-lot scale; `QuantityNormalizer` is an unused dest-grid floor; the §38 / A43 converter is missing; FIX tag 38 is unbuilt; capital is protected by absence, not by a correct conversion.**
