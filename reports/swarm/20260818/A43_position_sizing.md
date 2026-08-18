# A43 — Source-to-destination quantity conversion (Architecture §38)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A43_position_sizing.md` |
| Agent | A43 (position sizing / quantity conversion) |
| Date | 2026-08-18 |
| Status | **BINDING design** — specification only; product source not modified |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §38 (lines 1457–1492), executive change #10, §16, §30, §33, §35, §39, §60, §64, §68, §72.13–14, §72.18 |
| Supporting swarm | `A01` (Notional / sizing intermediates), `A09` (`SourceDestinationQuantityConversionTests`), `A13` (MT5 volume law: 1 lot = 10 000), `A23` (risk engine consumes this converter), `A32` (FIX tag 38 `OrderQty`, Security List fields) |
| Scope | XAUUSD only. Source = MT5 Manager-API volume. Destination = Pepperstone / cServer FIX 4.4 `NewOrderSingle` tag 38. |
| Product source edited | **No** |

Go-live checkbox this document owns (`§68`):

```text
[ ] position sizing conversion is verified
```

That box is **unchecked** until the unit fixtures in §12 pass **and** the Pepperstone destination min/step/max/convention row in `destination_symbols` is **measured on the live account** (not copied from this lab table).

---

## 0. One-line law

```text
source 0.10 MT5 lots  ≠  destination OrderQty 0.10
```

Never emit FIX `38` from a source lot number. Never emit FIX `38` from a scoring “suggested size”. Convert through **canonical XAU ounces**, apply allocation and caps, then **quantize down** to the destination instrument’s min / step / max.

This is executive change #10 and Architecture §38. A passthrough implementation is a hard FAIL of `SourceDestinationQuantityConversionTests.Never_passthrough_MT5_lots` (`A09`).

---

## 1. Why lots ≠ OrderQty (XAUUSD)

Three independent unit systems sit on this path. They share a name (“volume”) and almost never share a scale.

### 1.1 Source — MT5 integer volume (A13 BINDING)

Manager API `IMT*::Volume()` is stored as `uint64_t` / C# `ulong`. Official scale (`MT5APIMath.h`):

```text
MTAPI_VOLUME_DIV    = 10000.0
MTAPI_VOLUME_DIGITS = 4
lots                = Volume / 10000
Volume              = lots * 10000
```

| Lots | Integer `volume` |
|---:|---:|
| 1.00 | 10 000 |
| 0.10 | 1 000 |
| 0.01 | 100 |
| 0.001 | 10 |
| 0.0001 | 1 |

The in-file comment `// in hundredths of lots` on `PositionData::volume` is **wrong**. Do not divide by 100. `VolumeExt` (1 lot = 100 000 000) is **not used**.

`SymbolData.contract_size` is a `double` copied from `IMTConSymbol`. For XAUUSD it is **usually** `100` (troy ounces per 1.00 lot). It is **not guaranteed**. `XAUUSDm`, `GOLD`, cent groups, and some suffixes use `10`, `1`, or a broker-specific size. The converter reads the persisted per-`(broker, source_symbol)` spec. It never assumes `100`.

Source symbol limits (`SymbolData.volume_min` / `volume_step` / `volume_max`) use the **same** 1 lot = 10 000 ticks. They describe what the **source broker** will accept. They are **not** destination OrderQty limits.

### 1.2 Canonical — troy ounces of XAU

The only quantity that is comparable across venues:

```text
canonical_oz = source_lots × source_contract_size
```

Example: `0.10` lots × `100` oz/lot = **10 oz**.  
Same `0.10` lots × `10` oz/lot (mini) = **1 oz**.

All risk caps (max XAU gross/net, max position quantity after conversion, margin notional) are evaluated in canonical ounces **or** in destination units derived from those ounces. Never mix “lots” from broker A with “lots” from broker B.

### 1.3 Destination — cTrader FIX `OrderQty` (tag 38)

Official Spotware FIX 4.4 (`A32`, help.ctrader.com/fix/specification):

- Tag 38 is **required** on `NewOrderSingle`.
- Type `Qty`. Quoted: *“The number of shares ordered. … A maximum precision is 0.01.”*
- Official worked example (instrument `55=1` = EURUSD):

```text
38=10000
```

That is **10 000 units of base**, i.e. **0.10 FX lot**, not `38=0.10`. FIX OrderQty on cTrader is **units of the instrument’s base asset**, not MT5 lots.

cTrader UI can display “lots” or “units”. That is a client preference. The FIX wire does not follow the UI toggle.

For XAUUSD on Spotware-style venues the usual (must still be **measured**) convention is:

```text
1 OrderQty unit  =  1 troy ounce
```

so the same economic size as `0.10` MT5 lots × `100` oz/lot is:

```text
OrderQty = 10.00
```

Blindly writing `38=0.10` would be **100× too small** under that convention, or rejected as below min.

Security List (`35=y`) returns only `55` instrument id, `1007` symbol name, `1008` digits (`A32`). It does **not** publish min, step, max, lot size, or unit convention. Those fields are **operator-measured / otherwise discovered** and persisted on `destination_symbols`. Architecture §30 already forbids hard-coding the Pepperstone instrument id; §38 equally forbids hard-coding dest min/step.

### 1.4 The only case where the numbers match

`0.10` lots **may** equal `OrderQty 0.10` **if and only if** the destination convention is explicitly `LOTS` with `lot_size_oz = source_contract_size` (typically 100). That is a **persisted mapping fact**, not a default. Tests must include both:

- `BASE_UNITS` (1 unit = 1 oz): `0.10` lots → `10.00` OrderQty
- `LOTS` (1 lot = 100 oz): `0.10` lots → `0.10` OrderQty

A single-branch “copy the number” implementation fails one of those fixtures. That is intentional.

---

## 2. Pipeline (the only legal path)

Architecture §38 + `A23` §7:

```text
source volume (ulong ticks)
    ↓  ÷ 10 000
source lots
    ↓  × source_contract_size
canonical ounces
    ↓  × min(risk_allocation, remaining_caps) × confidence_scale
allocated ounces
    ↓  ÷ dest_unit_size_oz     (convention)
pre-round destination qty
    ↓  floor to destination step
    ↓  enforce destination min / max
    ↓  re-check book / margin caps (may reduce again, then re-quantize)
requested_quantity  →  ExecutionIntent.requested_quantity  →  FIX 38
```

Scoring / ML may emit `suggested_allocation` and `confidence` only. They never emit OrderQty (`A23` §1, §3.8).

`OPEN` / `INCREASE` run this pipeline on the **incremental** source volume.

`REDUCE` / `CLOSE` **do not** re-run source lots through allocation (`A23` §7, Architecture §35 / §64 / §72.18). Quantity comes from the **mapped destination position**.

---

## 3. Types (Domain — not implemented)

No product types exist yet (`A01`: `Notional` / sizing intermediates **MISSING**; `CanonicalInstrument` is a 3-field stub). Future names below are binding for the first implementation. Do not invent a second converter in `Fix.CTrader`.

### 3.1 Value objects

| Type | Meaning | Invariants |
|---|---|---|
| `Mt5VolumeTicks` | `ulong` Manager-API volume | ≥ 0; 0 is legal only for “no incremental size” |
| `SourceLots` | `decimal` = ticks / 10 000 | 4 decimal places max |
| `CanonicalOunces` | `decimal` troy oz of XAU | ≥ 0; scale enough for 0.0001 lot × smallest contract |
| `DestinationQty` | `decimal` OrderQty in dest units | ≥ 0; scale ≤ dest step and ≤ 0.01 FIX precision |
| `AllocationFactor` | `decimal` in `(0, 1]` | never > 1; never ≤ 0 |
| `QuantityStep` | `decimal` > 0 | dest step ≥ FIX precision 0.01 **or** config is rejected |

All arithmetic is `System.Decimal`. **Never** `double` for lots, ounces, or OrderQty. `SymbolData.contract_size` is ingested as `double` once, checked finite and `> 0`, then stored as `decimal`.

### 3.2 Enums

```text
QuantityConvention
    BaseUnits      // Spotware FIX default: OrderQty in base-asset units
    Lots           // OrderQty in venue lots; requires lot_size_oz
    Unverified     // mapping present but convention not measured → fail closed

SizingPath
    Approve
    ReduceSize
    Reject

ExposureClass                 // already specified in A23
    OpenExposure
    IncreaseExposure
    ReduceExposure
    CloseExposure
```

`Unverified` is the default for a newly discovered Security List row. Startup may persist id/name/digits (`§30`) without enabling trading. Live copy requires `BaseUnits` or `Lots` plus measured min/step/max.

### 3.3 Source instrument spec (per broker + source symbol)

Persisted from MT5 `SymbolData` (`A13` §8). Refresh on symbol-config change; do not cache across process life without a version.

| Field | Source | Notes |
|---|---|---|
| `broker_id` | domain | |
| `source_symbol` | `SymbolData.symbol` | raw, pre-canonical (`XAUUSDm`, `GOLD`, …) |
| `canonical_symbol` | mapper §16 | must be `XAUUSD` or converter refuses |
| `contract_size` | `SymbolData.contract_size` | ounces per 1.00 lot; **must be > 0** |
| `volume_min_ticks` | `volume_min` | 1 lot = 10 000 |
| `volume_step_ticks` | `volume_step` | same units |
| `volume_max_ticks` | `volume_max` | same units |
| `digits` | `digits` | price digits; not qty |
| `spec_version` / `fetched_at` | ingest | |

Source min/step/max are **not** applied to destination OrderQty. They are inputs to reconstruction display, abnormal-sizing features, and sanity checks (“source deal volume not a multiple of source step” → flag, do not invent a dest size).

### 3.4 Destination instrument spec (per execution venue + cTrader instrument id)

| Field | Required for live copy | Notes |
|---|---|---|
| `venue_id` / `destination_account` | yes | Pepperstone / cServer account |
| `instrument_id` | yes | FIX tag 55; **discovered**, never guessed (`§30`, `§72.13`) |
| `symbol_name` | yes | FIX tag 1007 (`XAUUSD` / broker alias) |
| `canonical_symbol` | yes | `XAUUSD` |
| `digits` | yes | FIX tag 1008 (price) |
| `quantity_convention` | yes | `BaseUnits` or `Lots` |
| `unit_size_oz` | yes if `BaseUnits` | ounces represented by `OrderQty = 1`; typical lab value `1` |
| `lot_size_oz` | yes if `Lots` | ounces represented by `OrderQty = 1.00` lot; typical `100` |
| `min_qty` | yes | destination OrderQty floor |
| `step_qty` | yes | destination increment; must divide all legal sizes |
| `max_qty` | yes | destination instrument cap (venue), may be tightened by risk |
| `qty_precision` | yes | min(`step` decimals, **0.01** FIX max) |
| `spec_status` | yes | `Unverified` / `Measured` / `Revoked` |
| `measured_at` / `measured_by` / `evidence_ref` | yes if Measured | audit |

`spec_status != Measured` → converter returns `REJECT` / `DEST_QTY_SPEC_UNVERIFIED`. Guessing Pepperstone min from another cTrader broker is the same class of bug as hard-coding instrument id.

### 3.5 Converter input / output

Input (`QuantityConversionRequest`):

```text
exposure_class
source_volume_ticks          // incremental for OPEN/INCREASE
source_spec                  // §3.3
dest_spec                    // §3.4
risk_allocation              // (0, 1], from copy_allocations / scoring suggestion after policy min()
confidence_scale             // (0, 1], advisory; cannot increase size (A23)
account_leverage
available_margin
max_margin_usage             // 0–1 of equity or configured base
quote_mid_or_side_price      // dest; required for margin notional
current_xau_long_oz
current_xau_short_oz
max_xau_gross_oz
max_xau_net_oz               // signed policy; see A23
max_position_qty_dest        // dest units or oz — store both, compare in one unit
linked_destination_position  // required for REDUCE/CLOSE
source_closed_ticks          // REDUCE only
source_position_ticks_before // REDUCE only
```

Output (`QuantityConversionResult`):

```text
sizing_path                  // Approve | ReduceSize | Reject
reason_codes[]               // SIZE_BELOW_MIN, SIZE_REDUCED_TO_LIMIT, …
binding_cap                  // which limit bound the size (or none)
source_lots
canonical_oz                 // before allocation
allocated_oz
pre_round_dest_qty
requested_quantity           // dest units, step-aligned; 0 on Reject
remainder_discarded          // dest units lost to floor
canonical_oz_approved        // ounces that requested_quantity represents
audit                        // full input snapshot + arithmetic trace
```

`requested_quantity` is the only value allowed into `ExecutionIntent.requested_quantity` and FIX tag 38 (`A23` §4.2).

---

## 4. Formulas

Use `decimal`. Intermediate scale: 8 decimal places is enough for 0.0001 lot × 1 oz/lot. Quantize **only** at the destination step (and again after any later cap).

### 4.1 Source ticks → lots

```text
source_lots = source_volume_ticks / 10000
```

Reject if `source_volume_ticks` would overflow a reasonable XAU size (config `max_source_lots_sane`, lab default `50`) — that is an `ABNORMAL_SIZING` input, not a silent wrap.

### 4.2 Lots → canonical ounces

```text
if contract_size <= 0 or not finite:
    REJECT SOURCE_CONTRACT_SIZE_INVALID

canonical_oz = source_lots × contract_size
```

| source ticks | lots | contract_size | canonical_oz |
|---:|---:|---:|---:|
| 1 000 | 0.10 | 100 | 10 |
| 100 | 0.01 | 100 | 1 |
| 10 000 | 1.00 | 100 | 100 |
| 1 000 | 0.10 | 10 | 1 |
| 1 000 | 0.10 | 1 | 0.10 |
| 1 | 0.0001 | 100 | 0.01 |

### 4.3 Allocation and confidence

```text
scale           = risk_allocation × confidence_scale     // both in (0, 1]
allocated_oz    = canonical_oz × scale
```

`confidence_scale` is **advisory and ≤ 1**. A model must not enlarge a position. If scoring omitted confidence, use `1`. If scoring omitted allocation, use the configured default allocation for that trader (still ≤ 1), never “copy 1:1 lots”.

`risk_allocation` is already `min(suggested, remaining trader/cluster caps)` **before** this function (`A23`). The converter still re-applies book/margin caps in ounces after conversion.

### 4.4 Canonical ounces → destination qty (pre-round)

```text
BaseUnits:
    if unit_size_oz <= 0: REJECT DEST_UNIT_SIZE_INVALID
    pre_round = allocated_oz / unit_size_oz

Lots:
    if lot_size_oz <= 0: REJECT DEST_LOT_SIZE_INVALID
    pre_round = allocated_oz / lot_size_oz

Unverified:
    REJECT DEST_QTY_SPEC_UNVERIFIED
```

Lab default for Pepperstone **until measured**: treat as `BaseUnits` / `unit_size_oz = 1` **only inside unit tests**. Production mapping stays `Unverified` until an operator marks `Measured`.

### 4.5 Floor to step (never round up)

Rounding up increases risk. Always floor.

```text
FloorToStep(q, step):
    if step <= 0: REJECT DEST_STEP_INVALID
    n = Floor(q / step)          // decimal integer
    return n × step
```

Then:

```text
q = FloorToStep(pre_round, step_qty)

if q > max_qty:
    q = FloorToStep(max_qty, step_qty)
    path = ReduceSize
    binding_cap = DEST_INSTRUMENT_MAX  (or MAX_POSITION_QTY if that was tighter)

if q < min_qty:
    REJECT SIZE_BELOW_MIN     // do not send a non-tradable order (A23 §7)
```

`remainder_discarded = pre_round - q` (after all floors). Persist it. Dust is expected and must not be “made up” on the next scale-in by rounding the other way.

FIX serialization: format tag 38 with a number of decimals equal to `qty_precision` (≤ 2). `10` and `10.00` are the same Qty; tests compare `decimal`, not strings.

### 4.6 Book / margin caps (after first quantize, then re-quantize)

Work in **canonical ounces**, convert the binding remainder back through §4.4–4.5.

```text
gross_after = current_gross_oz + allocated_oz          // OPEN/INCREASE of this side
net_after   = current_net_oz   ± allocated_oz          // +long / −short

room_gross  = max_xau_gross_oz - current_gross_oz
room_net    = room toward max_xau_net_oz on this side
room_pos    = max_position_qty converted to oz − this_position_oz

required_margin_per_oz = dest_side_price / account_leverage
usable_margin          = available_margin × max_margin_usage_factor
                         // or available_margin if policy is “free margin only”
room_margin_oz         = usable_margin / required_margin_per_oz

binding_oz = min(allocated_oz, room_gross, room_net, room_pos, room_margin_oz)
```

If `binding_oz < allocated_oz` → `ReduceSize` and record `binding_cap` (`MAX_XAU_GROSS` / `MAX_XAU_NET` / `MAX_POSITION_QTY` / `MAX_MARGIN_USAGE` / `INSUFFICIENT_MARGIN`).

Then re-run §4.4–4.5 on `binding_oz`. If the second quantize falls below `min_qty` → `REJECT` / `SIZE_BELOW_MIN` (do not send).

Leverage ≤ 0, price ≤ 0, or missing quote → do not invent margin; the risk engine already rejected `QUOTE_UNAVAILABLE`. Converter may assume a quote is present when invoked from step 10 of `A23` §5.

### 4.7 REDUCE / CLOSE (mapped destination qty)

```text
CLOSE:
    requested_quantity = linked_destination_position.qty     // entire mapped size
    // do not FloorToStep if the live position is already venue-legal
    // do not re-apply allocation or confidence

REDUCE:
    fraction = source_closed_ticks / source_position_ticks_before
    if ticks_before <= 0 or fraction <= 0: REJECT REDUCE_FRACTION_INVALID
    if fraction >= 1: treat as CLOSE
    raw = dest_position.qty × fraction
    q   = FloorToStep(raw, step_qty)
    if dest_position.qty − q < min_qty:
        // leftover would be untradable dust
        policy default: CLOSE the whole mapped position
        reason: REDUCE_PROMOTED_TO_CLOSE_DUST
    else if q < min_qty:
        REJECT SIZE_BELOW_MIN          // cannot send a sub-min reduce
    else:
        requested_quantity = q
```

Do **not** convert the source closed lots through contract size and hope it matches. Prior OPEN may have discarded remainder; dest size is the book of record (`§35`).

Reversals are two conversions: `CLOSE` of the mapped dest position, then `OPEN` of the opposite side through §4.1–4.6 (`A23` §2).

---

## 5. Min / step / max — two layers, do not mix

| Layer | Fields | Unit | Applied to |
|---|---|---|---|
| Source MT5 symbol | `volume_min`, `volume_step`, `volume_max` | ticks (1 lot = 10 000) | interpreting source deals; **not** FIX 38 |
| Destination instrument | `min_qty`, `step_qty`, `max_qty` | OrderQty dest units | every live/shadow dest order |
| Risk book | `max position quantity`, XAU gross/net, margin | config; compare in oz | may **tighten** dest max |

Rules:

1. Dest `step_qty` must be `> 0` and `min_qty` must be a multiple of `step_qty`. `max_qty` must be ≥ `min_qty`. Invalid spec → `DEST_QTY_SPEC_INVALID`, fail closed.
2. Dest `step_qty` must be a multiple of `0.01` (FIX max precision). A measured step of `0.001` is **not** sendable on this venue; reject the spec rather than silently coarsening.
3. Never apply source `volume_step` to OrderQty (source step `0.01` lots is **1 oz** when contract = 100, dest step may be `0.01` **ounces** — 100× different).
4. Never apply dest step to reconstructed source volume.
5. Shadow copy uses the **same** dest spec so source-vs-shadow drift is economic, not a unit bug (`A28` Phase 5, `§72.14`).

### 5.1 Lab fixture dest spec (tests only — not Pepperstone production)

Until the live account is measured, unit tests use named fixtures:

**`Dest_BaseUnits_1oz`** (Spotware-style; official EURUSD `38=10000` analogue)

| Field | Value |
|---|---|
| `quantity_convention` | `BaseUnits` |
| `unit_size_oz` | 1 |
| `min_qty` | 0.01 |
| `step_qty` | 0.01 |
| `max_qty` | 5 000 |
| `qty_precision` | 0.01 |

**`Dest_BaseUnits_1oz_whole`** (whole-ounce venue)

| Field | Value |
|---|---|
| `min_qty` / `step_qty` | 1.00 |
| other | same as above |

**`Dest_Lots_100oz`** (only when mapping says lots)

| Field | Value |
|---|---|
| `quantity_convention` | `Lots` |
| `lot_size_oz` | 100 |
| `min_qty` | 0.01 |
| `step_qty` | 0.01 |
| `max_qty` | 50 |
| `qty_precision` | 0.01 |

**`Src_Std_100`** — `contract_size = 100`, `volume_min = 100` (0.01 lot), `volume_step = 100`, `volume_max = 500_000` (50 lots).  
**`Src_Mini_10`** — `contract_size = 10`, same ticks.  
**`Src_Nano_1`** — `contract_size = 1`, `volume_min = 10` (0.001 lot), `volume_step = 10`.

### 5.2 How to measure Pepperstone (go-live, not guessed here)

Instrument **id is unknown** in-repo (`§68` item 10, `A28`). Procedure:

1. Security List on the TRADE session → persist `55` / `1007` / `1008` for the row whose `1007` maps to canonical `XAUUSD` (`§16`, `§30`).
2. Read the account’s XAUUSD specification from the Pepperstone cTrader UI (or a later Open API `ProtoOASymbol` if that integration is added). Record `minVolume` / `stepVolume` / `maxVolume` / `lotSize` **and** convert from Open API “cents of a unit” (`volume / 100 = units`) into FIX OrderQty units.
3. Confirm convention with a **demo** market order of a known size (e.g. `38=1` vs `38=0.01`) and the resulting position qty / P&amp;L per $1 move. 1 oz of XAU moves ~$1 P&amp;L per $1 price change.
4. Mark `destination_symbols.spec_status = Measured` with `evidence_ref` (screenshot hash, demo `ClOrdID`, or Open API dump hash).
5. Re-run `SourceDestinationQuantityConversionTests` against a **copy** of the measured row (new fixture name `Dest_Pepperstone_Measured`). Do not overwrite lab fixtures.

If Open API is never added, step 2+3 are still mandatory. FIX Security List alone is insufficient.

---

## 6. Placement in the risk / execution flow

`A23` evaluation order step 10 is **Sizing normalize**. This converter **is** that step.

```text
RiskEngine
    … freshness / eligibility …
    → IQuantityConverter.Convert(request)     // this spec
    → book caps (may call converter.ReduceTo(binding_oz))
    → martingale / abnormal flags (do not resize; REJECT)
    → persist risk_decision
    → persist execution_intent.requested_quantity
FIX worker
    → NewOrderSingle 38 = requested_quantity only
```

The FIX worker must not rescale. If it “helpfully” multiplies by 100 or by contract size, tests that inspect the outbound FIX dictionary fail.

Shadow path calls the same converter so shadow P&amp;L is in dest units (`A28` Phase 5).

---

## 7. Persistence / audit

Every conversion that reaches a `risk_decision` stores:

```text
source_volume_ticks
source_lots
source_contract_size
canonical_oz
risk_allocation
confidence_scale
allocated_oz
dest_instrument_id
dest_convention
dest_unit_or_lot_size_oz
dest_min / dest_step / dest_max
pre_round_dest_qty
requested_quantity
remainder_discarded
canonical_oz_approved
sizing_path
binding_cap
reason_codes
```

`execution_intents.requested_quantity` is dest units (`§33`, `A23` §4.2).

`destination_positions` store **both** `qty_dest` (venue) and `qty_oz` (canonical) so reconciliation quantity-mismatch (`§43`) is unit-safe.

---

## 8. Reason codes (sizing only)

Extends `A23` §4.3. First blocking code is `primary_reason`.

| Code | Path | When |
|---|---|---|
| `SIZE_BELOW_MIN` | Reject | after floor, `q < min_qty` |
| `SIZE_NOT_MULTIPLE_OF_STEP` | should not escape | converter always floors; if it appears, bug |
| `SIZE_REDUCED_TO_LIMIT` | ReduceSize | informational, with `binding_cap` |
| `DEST_INSTRUMENT_MAX` | ReduceSize / Reject | venue `max_qty` |
| `MAX_POSITION_QTY` | ReduceSize / Reject | risk cap |
| `MAX_XAU_GROSS` | ReduceSize / Reject | |
| `MAX_XAU_NET` | ReduceSize / Reject | |
| `MAX_MARGIN_USAGE` | ReduceSize / Reject | |
| `INSUFFICIENT_MARGIN` | ReduceSize / Reject | usable margin < min-size margin |
| `SOURCE_CONTRACT_SIZE_INVALID` | Reject | missing / ≤ 0 / non-finite |
| `DEST_QTY_SPEC_UNVERIFIED` | Reject | Security List only; min/step not measured |
| `DEST_QTY_SPEC_INVALID` | Reject | step ≤ 0, min not multiple of step, … |
| `DEST_UNIT_SIZE_INVALID` | Reject | |
| `DEST_LOT_SIZE_INVALID` | Reject | |
| `DEST_STEP_INVALID` | Reject | |
| `MAPPING_MISSING` | Reject | no dest spec or (reduce/close) no position link |
| `REDUCE_FRACTION_INVALID` | Reject | |
| `REDUCE_PROMOTED_TO_CLOSE_DUST` | Approve (close) | leftover &lt; min |
| `PASSTHROUGH_FORBIDDEN` | test-only | never a production code |

---

## 9. Binding rules (implementation checklist)

1. **No passthrough.** `source_lots` is never assigned to tag 38.
2. **No double.** All qty math is `decimal`.
3. **No `Volume / 100`.** A13: 1 lot = 10 000 ticks.
4. **No assumed contract size.** Read `source_spec.contract_size`.
5. **No assumed dest instrument id.** `§30`.
6. **No assumed dest min/step/max.** `Unverified` refuses live copy.
7. **Floor, never ceil / never banker’s round-up.**
8. **Below min → reject**, never send.
9. **Confidence cannot increase size.**
10. **REDUCE/CLOSE from mapped dest qty**, not from a second source conversion.
11. **FIX worker does not convert.**
12. **Shadow uses the same function.**
13. **One converter** (`IQuantityConverter` in Domain / Application). Not a copy inside the FIX assembly.
14. **Canonical symbol must already be `XAUUSD`.** Unknown source symbols do not silently become gold (`A09` XAU mapping tests).
15. **Fail closed** on missing spec, missing link, non-finite numbers.

---

## 10. Worked examples (known fixtures)

Unless noted: allocation = 1, confidence = 1, no book/margin bind, dest = `Dest_BaseUnits_1oz`, source = `Src_Std_100`.

### 10.1 OPEN — identity through ounces

| Id | Source ticks | Lots | Contract | Oz | Dest spec | `requested_quantity` | Path | Notes |
|---|---:|---:|---:|---:|---|---:|---|---|
| E01 | 1 000 | 0.10 | 100 | 10 | BaseUnits 1 oz | **10.00** | Approve | **not 0.10** |
| E02 | 100 | 0.01 | 100 | 1 | BaseUnits 1 oz | **1.00** | Approve | 1 micro-lot = 1 oz |
| E03 | 10 000 | 1.00 | 100 | 100 | BaseUnits 1 oz | **100.00** | Approve | 1 standard lot |
| E04 | 10 | 0.001 | 100 | 0.10 | BaseUnits 1 oz | **0.10** | Approve | |
| E05 | 1 | 0.0001 | 100 | 0.01 | BaseUnits 1 oz | **0.01** | Approve | exact min |
| E06 | 1 000 | 0.10 | 10 | 1 | BaseUnits 1 oz | **1.00** | Approve | mini contract; **not 10** |
| E07 | 1 000 | 0.10 | 1 | 0.10 | BaseUnits 1 oz | **0.10** | Approve | nano contract |
| E08 | 1 000 | 0.10 | 100 | 10 | Lots 100 oz | **0.10** | Approve | numbers match **only** here |
| E09 | 100 | 0.01 | 100 | 1 | Lots 100 oz | **0.01** | Approve | dest min |

### 10.2 OPEN — step floor

| Id | Source ticks | Oz | Dest step | Pre-round | `requested_quantity` | Remainder | Path |
|---|---:|---:|---:|---:|---:|---:|---|
| E10 | 1 230 | 12.30 | 0.01 | 12.30 | 12.30 | 0 | Approve |
| E11 | 1 230 | 12.30 | 1.00 | 12.30 | **12.00** | 0.30 | Approve |
| E12 | 1 235 | 12.35 | 0.10 | 12.35 | **12.30** | 0.05 | Approve |
| E13 | 1 001 | 10.01 | 0.10 | 10.01 | **10.00** | 0.01 | Approve |
| E14 | 50 | 0.50 | 1.00 (min 1.00) | 0.50 | 0 | — | **Reject** `SIZE_BELOW_MIN` |
| E15 | 99 | 0.99 | 0.01 (min 1.00) | 0.99 | 0 | — | **Reject** `SIZE_BELOW_MIN` |

Never round E11 to `13.00`.

### 10.3 OPEN — allocation and confidence

| Id | Ticks | Oz | Alloc | Conf | Allocated oz | Dest qty | Path |
|---|---:|---:|---:|---:|---:|---:|---|
| E16 | 1 000 | 10 | 0.25 | 1.00 | 2.50 | 2.50 | Approve |
| E17 | 1 000 | 10 | 1.00 | 0.50 | 5.00 | 5.00 | Approve |
| E18 | 1 000 | 10 | 0.25 | 0.50 | 1.25 | 1.25 | Approve |
| E19 | 1 000 | 10 | 0.001 | 1.00 | 0.01 | 0.01 | Approve (exact min) |
| E20 | 1 000 | 10 | 0.0005 | 1.00 | 0.005 | 0 | **Reject** `SIZE_BELOW_MIN` |
| E21 | 100 | 1 | 1.00 | 1.10 | — | — | **Reject** input (`confidence_scale > 1` illegal) |

### 10.4 OPEN — dest max and risk caps

| Id | Oz in | Cap | Binding | Dest qty | Path |
|---|---:|---|---|---:|---|
| E22 | 10 | dest `max_qty = 5` | `DEST_INSTRUMENT_MAX` | 5.00 | ReduceSize |
| E23 | 10 | dest `max_qty = 5.09`, step 0.10 | dest max floored | **5.00** | ReduceSize |
| E24 | 10 | remaining gross room 3.33 oz, step 0.01 | `MAX_XAU_GROSS` | 3.33 | ReduceSize |
| E25 | 10 | remaining gross room 0.004 oz, min 0.01 | `MAX_XAU_GROSS` | 0 | **Reject** `SIZE_BELOW_MIN` |
| E26 | 10 | `max_position_qty_dest = 2.00` (2 oz) | `MAX_POSITION_QTY` | 2.00 | ReduceSize |

### 10.5 OPEN — margin (lab numbers)

Assumptions: dest price = `2400` USD/oz, leverage = `100`, `required_margin_per_oz = 24`, `available_margin = 100`, policy uses 100% of free margin (`max_margin_usage` factor = 1).

```text
room_margin_oz = 100 / 24 = 4.1666… → floor via dest step 0.01 → 4.16
```

| Id | Requested oz | Binding oz | Dest qty | Path |
|---|---:|---:|---:|---|
| E27 | 10 | 4.16 | 4.16 | ReduceSize `MAX_MARGIN_USAGE` / `INSUFFICIENT_MARGIN` |
| E28 | 1 | 1 | 1.00 | Approve (margin 24 &lt; 100) |
| E29 | 10 | usable margin 0.20 → 0.008 oz | 0 | **Reject** `SIZE_BELOW_MIN` |

Do **not** hard-code `24` or `2400` in production. Quote and leverage are inputs.

### 10.6 INCREASE (scale-in)

Source already long 0.10 lots (10 oz) mapped to dest `10.00`. Source adds 0.05 lots (500 ticks).

| Id | Incremental ticks | Incremental oz | Dest incremental | Path |
|---|---:|---:|---:|---|
| E30 | 500 | 5 | 5.00 | Approve |
| E31 | 500, remaining position cap 3 oz | 5 → 3 | 3.00 | ReduceSize `MAX_POSITION_QTY` |

Converter sees **500 ticks**, not 1 500. Reconstruction `max_volume` is irrelevant here.

### 10.7 REDUCE / CLOSE

Mapped dest position `qty = 20.00` (from a prior converted OPEN). Source position before close = 2 000 ticks (0.20 lots).

| Id | Class | Closed ticks | Fraction | Raw dest | Dest qty | Path |
|---|---|---:|---:|---:|---:|---|
| E32 | CLOSE | 2 000 | 1 | 20.00 | **20.00** | Approve (full mapped) |
| E33 | REDUCE | 700 | 0.35 | 7.00 | **7.00** | Approve |
| E34 | REDUCE | 1 | 0.0005 | 0.01 | **0.01** | Approve (equals min) |
| E35 | REDUCE | 1, dest step 1.00 | 0.0005 | 0.01 | 0 | **Reject** `SIZE_BELOW_MIN` |
| E36 | REDUCE | 1 950, dest min 1, step 1 | 0.975 | 19.50 → 19.00 leftover 1.00 | 19.00 | Approve |
| E37 | REDUCE | 1 950, dest min 2, step 1 | leftover 1.00 &lt; min 2 | — | **20.00** | Approve `REDUCE_PROMOTED_TO_CLOSE_DUST` |
| E38 | CLOSE | n/a | dest live qty 19.97 (venue remainder) | 19.97 | **19.97** | Approve — do not reconvert source 0.20 lots to 20.00 |

E38 is the reason REDUCE/CLOSE must not go through §4.1.

### 10.8 Fail-closed / forbidden

| Id | Setup | Result |
|---|---|---|
| E39 | `quantity_convention = Unverified` | Reject `DEST_QTY_SPEC_UNVERIFIED` |
| E40 | `contract_size = 0` | Reject `SOURCE_CONTRACT_SIZE_INVALID` |
| E41 | `contract_size = NaN` (raw double) | Reject `SOURCE_CONTRACT_SIZE_INVALID` |
| E42 | missing dest spec | Reject `MAPPING_MISSING` |
| E43 | REDUCE without `linked_destination_position` | Reject `MAPPING_MISSING` |
| E44 | `step_qty = 0` | Reject `DEST_STEP_INVALID` / `DEST_QTY_SPEC_INVALID` |
| E45 | `step_qty = 0.001` (finer than FIX 0.01) | Reject `DEST_QTY_SPEC_INVALID` |
| E46 | `min_qty = 0.03`, `step_qty = 0.02` (min not a multiple) | Reject `DEST_QTY_SPEC_INVALID` |
| E47 | Implementation assigns `requested_quantity = source_lots` (0.10) under `Dest_BaseUnits_1oz` | **Test FAIL** `Never_passthrough_MT5_lots` |
| E48 | Canonical mapper did not resolve symbol (still `GOLD` as dest) | Reject — converter not invoked; mapping tests own this |
| E49 | `source_volume_ticks = 0` on OPEN | Reject `SIZE_BELOW_MIN` |
| E50 | Book cap would flip side (net cap) | Reduce to room; never emit opposite-side qty |

---

## 11. Inverse conversion (reconciliation / exposure book)

To compare FIX fills with the internal book (`§43` quantity mismatch):

```text
BaseUnits:  fill_oz = LastQty × unit_size_oz
Lots:       fill_oz = LastQty × lot_size_oz
```

`LastQty` / `CumQty` / `LeavesQty` / position qty from cServer are dest units. Summing them with MT5 lots is a defect. Dashboard “XAU long/short” (`A06`, `A23` §11.2) displays dest qty **and** ounces.

---

## 12. Tests (Architecture §60 item 8)

**Class:** `TraderIntelligence.Tests.Unit.Normalization.SourceDestinationQuantityConversionTests`  
**Path:** `tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs`  
**SUT:** `QuantityConverter` / `IQuantityConverter`  
**Status today:** **MISSING** (`A09`). `UnitTest1` does not count.

Required methods (`A09` plus the fixtures above):

| Method | Covers |
|---|---|
| `Known_lot_to_OrderQty_examples` | E01–E09 Theory |
| `Never_passthrough_MT5_lots` | E01 vs 0.10; E47 |
| `Respects_min_qty_and_step` | E10–E15 |
| `Floors_not_rounds_up` | E11, E13, E23 |
| `Allocation_and_confidence_scale` | E16–E21 |
| `Confidence_cannot_exceed_one` | E21 |
| `Dest_max_and_risk_caps_reduce` | E22–E26 |
| `Below_min_after_cap_rejects` | E14, E20, E25, E29 |
| `Margin_room_reduces_qty` | E27–E29 |
| `Increase_uses_incremental_volume` | E30–E31 |
| `Close_uses_mapped_destination_qty` | E32, E38 |
| `Partial_reduce_is_fraction_of_dest` | E33–E37 |
| `Dust_leftover_promotes_to_close` | E37 |
| `Unverified_dest_spec_rejects` | E39 |
| `Invalid_contract_size_rejects` | E40–E41 |
| `Missing_mapping_rejects` | E42–E43 |
| `Invalid_step_or_min_rejects` | E44–E46 |
| `Mini_and_nano_contracts_differ` | E06, E07 |
| `Lots_convention_only_when_mapped` | E08, E09 vs E01 |
| `Mt5_ticks_scale_is_10000` | 1 000 ticks = 0.10 lots, not 10 lots |
| `Decimal_not_double_for_0_0001_lot` | E05 exact `0.01m` |
| `Shadow_and_live_share_converter` | same function, two callers |
| `Fix_worker_does_not_rescale` | integration / dictionary assert when FIX exists |

xUnit shape (guidance, not product source):

```csharp
[Theory]
[InlineData(1000UL, 100.0, "BaseUnits", 10.00)]
[InlineData(1000UL, 100.0, "Lots",      0.10)]
[InlineData(1000UL,  10.0, "BaseUnits",  1.00)]
public void Known_lot_to_OrderQty_examples(...)
```

Do not live-call Pepperstone from unit tests. Measured dest spec is a **checked-in fixture snapshot** with `measured_at` in the fixture name.

Integration (later, Phase 8): one demo `NewOrderSingle` whose `38` equals the converter output for a canned source deal; Execution Report `38` / `14` / `151` echo that qty (`A32` examples use `38=10000`).

---

## 13. Current repo state (honest)

| Piece | State |
|---|---|
| Architecture §38 | Specified (pipeline + inputs; no numeric fixtures) |
| `A23` §7 | Specified; defers examples to this document |
| `IQuantityConverter` / `QuantityConverter` | **MISSING** — do not add in this task |
| Domain `Notional` / dest spec entity | **MISSING** (`A01`) |
| `CanonicalInstrument` | stub: `Id`, `Symbol`, `Description` — no qty fields |
| `SourceSymbolMapping` | stub: no contract size |
| `Mt5Position.Volume` / `Mt5Deal.Volume` | `ulong` ticks — correct width, no conversion |
| `destination_symbols` table | **MISSING** |
| `SourceDestinationQuantityConversionTests` | **MISSING** (`A09`) |
| Pepperstone XAUUSD instrument id | **UNKNOWN** — must be discovered |
| Pepperstone min/step/max/convention | **UNMEASURED** — lab fixtures only |

This report does **not** implement the converter. It is the design `QuantityConverter` and the risk engine must follow.

---

## 14. Suggested type / table sketch (for a later implementation wave)

Not created now. When Domain is filled (`A01` Wave 1+):

```text
Domain/ValueObjects/Mt5VolumeTicks.cs
Domain/ValueObjects/CanonicalOunces.cs
Domain/ValueObjects/DestinationQty.cs
Domain/ValueObjects/QuantityStep.cs
Domain/Enums/QuantityConvention.cs
Domain/Enums/SizingPath.cs
Domain/Entities/SourceSymbolSpec.cs
Domain/Entities/DestinationSymbol.cs      // extends A01 DestinationSymbol
Domain/Services/IQuantityConverter.cs

Application/Sizing/QuantityConversionRequest.cs
Application/Sizing/QuantityConversionResult.cs
Application/Sizing/QuantityConverter.cs   // or Domain service if kept pure

tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs
tests/Unit/Normalization/Fixtures/Dest_BaseUnits_1oz.json
tests/Unit/Normalization/Fixtures/Dest_Lots_100oz.json
```

`destination_symbols` columns: §3.4. `risk_decisions` sizing columns: §7.

Generator / hand-written MQ5 rules do not apply (no `.mq5` here).

---

## 15. What this converter does **not** do

- Detect martingale / averaging-down / abnormal sizing (`A23` §6.5). It may expose `source_lots` / last dest qty to those detectors.
- Choose side, symbol, or ClOrdID.
- Talk to FIX or MT5.
- Convert prices, SL/TP distances, or pip values (tick-value is a later helper; not required to emit OrderQty once dest spec is measured).
- Cluster correlated traders (`A23` Phase 2).
- Invent MFE/MAE or destination costs.
- Support non-XAUUSD. Other symbols need their own canonical unit (not ounces).

---

## 16. Cross-references

| Doc | What to reuse |
|---|---|
| Architecture §38 | Pipeline + input list |
| Architecture executive #10 | Lots ↛ OrderQty |
| Architecture §16 / §30 | Canonical XAUUSD; discover dest id |
| Architecture §33 | `requested_quantity` on execution intent |
| Architecture §35 / §64 / §72.18 | REDUCE/CLOSE ≠ OPEN |
| Architecture §39 | Caps that bind after conversion |
| Architecture §60 / §68 | Tests + go-live gate |
| `A13` | 1 lot = 10 000; `contract_size`; source min/step/max ticks |
| `A23` §3.3, §4.2–4.3, §5 step 10, §7 | Engine contract |
| `A32` | Tag 38 precision 0.01; example `38=10000`; Security List has no min/step |
| `A09` | Test class name and first methods |

---

## 17. Acceptance (when implementation happens)

DONE for this slice means all of the following, measured:

```text
[ ] IQuantityConverter is the only path from source ticks to FIX 38
[ ] E01–E50 (or equivalent Theory rows) pass
[ ] Never_passthrough_MT5_lots fails a deliberate passthrough implementation
[ ] Floor-only step behavior proven (E11, E13)
[ ] SIZE_BELOW_MIN sends zero FIX
[ ] REDUCE/CLOSE fixtures use mapped dest qty (E32, E38)
[ ] Unverified Pepperstone row cannot go live
[ ] destination_symbols has a Measured Pepperstone XAUUSD row with evidence_ref
[ ] §68 “position sizing conversion is verified” checked only after the above
```

Until then the honest state is: **design complete, converter absent, Pepperstone dest qty spec unverified.**
