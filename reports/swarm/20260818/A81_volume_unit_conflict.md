# A81 — Volume unit conflict (`mt5_types.h` “hundredths” vs SDK `MTAPI_VOLUME_EXT_DIV`)

**Date:** 2026-08-18  
**Agent:** A81  
**Artifact:** `D:\Prop\reports\swarm\20260818\A81_volume_unit_conflict.md`  
**Product source modified:** **none** (read-only). Chat is not storage.  
**Method:** read the two named sources, then the official math helpers, official report examples, C++ extractors, and the existing C# `VolumeConverter`. No live broker / DealerSend measurement.

---

## Verdict (honest)

The conflict is real. It is a **comment / convention** bug, not a third MetaTrader 5 integer scale.

There are **two official MT5 Manager integer lot scales**, both defined in `MT5APIMath.h`. Neither is “hundredths of lots.”

| Claim | Implied 1.00 lot | Source | Status |
|---|---:|---|---|
| “hundredths of lots” | **100** | `PositionData.volume` comment in `mt5_types.h:75` | **Wrong for every MT5 Manager volume this product copies.** This is the **MT4** `MODE_LOTSTEP` / hundredths convention. |
| Classic `Volume()` | **10 000** | `MTAPI_VOLUME_DIV = 10000.0` (4 digits) | **What this product actually transports today.** |
| Extended `VolumeExt()` | **100 000 000** | `MTAPI_VOLUME_EXT_DIV = 100000000.0` (8 digits) | **What official Capital reports divide by.** Correct for `VolumeExt` / `FIELD_*_VOLUME_EXT` / `TYPE_VOLUME_EXT`. |

`volume / 100000000` is **exact** for extended volume. It is **10 000× too small** if applied to the current C++ `pos->Volume()` / `deal->Volume()` integers.  
`volume / 100` (believing the comment) is **100× too large** on those same integers.

**Recommendation (this file, not implemented here):** a C# `VolumeConverter` with a **configurable** scale whose **constructor default is `100_000_000`**, plus unit tests for **both official scales**. Existing Domain code already has a converter, but it **defaults to `10_000`**, has **no tests**, and `TradeReconstructor` hard-binds `VolumeConverter.Manager`. Changing the default without switching extractors to `VolumeExt()` is a **10 000×** sizing bug.

---

## 1. What `mt5_types.h` says

File: `D:\Prop\mt5-sdk\src\core\mt5_types.h`

### 1.1 The only explicit lot-unit comment — **wrong**

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

“Hundredths of lots” means `lots * 100`. That is **not** defined anywhere in the vendored Manager SDK math header.

### 1.2 Same file, other volume fields — **silent or dangling**

```95:95:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;
```

`DealData::volume` and `OrderData::volume` have **no** unit comment. They are filled from the same classic `Volume()` / `VolumeInitial()` family as `PositionData.volume` (see §4).

```143:151:D:\Prop\mt5-sdk\src\core\mt5_types.h
// One self-contained trade instruction passed to IMT5Client::SendTrade().
// Volume is in MT5 native integer units (see MT5_VOLUME_PER_LOT in
// trade_execution_service.cpp). No credentials/secrets are ever carried here.
struct MT5TradeRequest {
    ...
    uint64_t    volume = 0;          // MT5 native volume units
```

This is not a second official scale. It is an **unresolved pointer**:

1. “MT5 native integer units” is ambiguous — the SDK has **two** native integers.
2. `MT5_VOLUME_PER_LOT` is **not defined** anywhere under `D:\Prop` (grep hits only this comment).
3. `trade_execution_service.cpp` **does not exist** in this tree.

`SymbolData::{volume_min,volume_max,volume_step}` also have no unit comment.  
`ChartBarData.volume` is documented as **“real (exchange) volume”** — not lots. Do not apply any lot divisor to it.

The header **disagrees with itself**. That is a documentation bug, not an extra API mode.

---

## 2. What the official SDK math header says

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
(Not `D:\Prop\mt5-sdk\Include\Classes\MT5APIMath.h` — that path does not exist. The vendored SDK lives under `vendor\MetaTrader5SDK\`.)

Copyright banner: MetaQuotes Ltd., 2000–2026.

```12:20:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
#define MTAPI_VOLUME_DIV        (10000.0)
#define MTAPI_VOLUME_DIGITS     (4)
#define MTAPI_VOLUME_MAX        ((uint64_t)10000000000)
//+------------------------------------------------------------------+
//| Volume with extended accuracy constants                          |
//+------------------------------------------------------------------+
#define MTAPI_VOLUME_EXT_DIV    (100000000.0)
#define MTAPI_VOLUME_EXT_DIGITS (8)
#define MTAPI_VOLUME_EXT_MAX    ((uint64_t)10000000000000000000u)
```

There is **no** `100` divisor and no “hundredths” string in this header.

Conversion implementations (same file):

| Function | Formula | Meaning |
|---|---|---|
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, 4)` | lots → classic integer |
| `SMTMath::VolumeToDouble(vol)` | `vol / 10000.0`, 4 digits | classic integer → lots |
| `SMTMath::VolumeExtToInt(lots)` | `PriceToIntPos(lots, 8)` | lots → ext integer |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / 100000000.0`, 8 digits | ext integer → lots |
| `SMTMath::VolumeFromVolumeExt(ext)` | `ext / 10000` | 8-digit → 4-digit |
| `SMTMath::VolumeExtFromVolume(vol)` | `vol * 10000` | 4-digit → 8-digit |

```262:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline double SMTMath::VolumeToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV),MTAPI_VOLUME_DIGITS));
  }
...
inline uint64_t SMTMath::VolumeFromVolumeExt(const uint64_t volume_ext)
  {
   return(volume_ext/10000);
  }
...
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
...
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

Ratio: `MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10 000`.  
`1.00 lot` is **never** the integer `100` on either official path.

The size helpers’ comments say **“lots”** explicitly (`VolumeToSize` / `VolumeFromSize`).

`SMTFormat::FormatVolume` uses `VolumeToDouble` (÷ 10 000).  
`SMTFormat::FormatVolumeExt` uses `VolumeExtToDouble` (÷ 100 000 000).  
File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h`.

---

## 3. Report examples that divide by `100000000`

These examples do **not** contradict §2. They store **`FIELD_DEAL_VOLUME_EXT`** and then divide by the ext constant.

### 3.1 Capital `DealCache.cpp`

Field list binds **extended** volume into `DealRecord.volume`:

```39:44:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp
  {{ IMTDatasetField::FIELD_DEAL_LOGIN           , true   , offsetof(DealRecord,login)           },
   { IMTDatasetField::FIELD_DEAL_TIME            , true   , offsetof(DealRecord,time)            },
   { IMTDatasetField::FIELD_DEAL_ACTION          },
   { IMTDatasetField::FIELD_DEAL_ENTRY           },
   { IMTDatasetField::FIELD_DEAL_VOLUME_EXT      , true   , offsetof(DealRecord,volume)          },
```

Then:

```333:333:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp
   const double volume=fabs((deal.volume/100000000.0)*deal.contract_size*deal.rate_profit);
```

### 3.2 Capital `DealWeekCache.cpp`

Same binding (`FIELD_DEAL_VOLUME_EXT` at line 18) and the same divisor:

```337:337:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealWeekCache.cpp
   const double volume=fabs((deal.volume/100000000.0)*deal.contract_size*deal.rate_profit);
```

Dataset field comment (official):

```461:461:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h
      FIELD_DEAL_VOLUME_EXT                  =2014,         // uint64_t    , deal volume with extended accuracy
```

There is **no** `FIELD_DEAL_VOLUME` (non-ext) in that deal-field block. New report/dataset code is **ext-first**. That is why this report recommends a **default scale of `100_000_000`**.

### 3.3 Trades.Standard `ExecutionType.cpp` — the *other* official example

When the integer comes from **`deal->Volume()`** (classic getter), the official sample divides by **`10000.0`**, not `100` and not `1e8`:

```628:628:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

Many other official reports declare columns as `TYPE_VOLUME_EXT` and request `FIELD_DEAL_VOLUME_EXT` (`DealsHistory.cpp`, `PositionsHistory.cpp`, `FastProfitDeals.cpp`, `DailyTradeReport.cpp`, `DailyPositionReport.cpp`).

### 3.4 Manager sample — 1.00 lot is `VolumeToInt(1.0)` = **10 000**

```186:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
```

### 3.5 WebAPI .NET sample — names the split “old 4 digits” vs “new 8 digits”

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs`:

- `ConvetToOldVolume(new_volume)` = `new_volume / 10000`
- `ConvertToNewVolume(old_volume)` = `old_volume * 10000`

Book struct is the only Manager header that states the digit count in-line:

```24:25:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIBook.h
   int64_t           volume;                                // deal volume - only integer values
   int64_t           volume_ext;                            // deal volume with extended accuracy - 8 digits
```

`IMTDeal::Volume()` comment: `//--- deal volume`.  
`IMTDeal::VolumeExt()` comment: `//--- deal volume with extended accuracy`.  
Neither says “hundredths.”

---

## 4. What this product actually copies (code, not comments)

Read-only. No `VolumeExt()` / `VolumeMinExt()` call was found under `D:\Prop\mt5-sdk\src`.

| Product field | Copied from / written to | Official scale |
|---|---|---|
| `PositionData.volume` | `IMTPosition::Volume()` | classic `/ 10000` |
| `DealData.volume` | `IMTDeal::Volume()` | classic `/ 10000` |
| `OrderData.volume` | `IMTOrder::VolumeInitial()` | classic `/ 10000` (**initial**, not remaining) |
| `SymbolData.volume_*` | `IMTConSymbol::VolumeMin/Max/Step()` | classic `/ 10000` |
| `MT5TradeRequest.volume` | `IMTRequest::Volume()` | classic `/ 10000` |
| `TickData.volume` | `MTTickShort::volume` | last-trade size; companion `volume_ext` exists on the tick struct |
| `ChartBarData.volume` | chart real volume | **not** lot-integer |

Extractors (verbatim, no rescale):

```1495:1495:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = pos->Volume();
```

```1517:1517:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = deal->Volume();
```

```1534:1534:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = order->VolumeInitial();
```

Same three assignments in `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (`extractPosition` 833, `extractDeal` 855, `extractOrder` 872).

Send path writes **classic** `Volume()`, not `VolumeExt()`:

```1130:1130:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    request->Volume(volume);
```

Same in `SendTrade` branches (`mt5_manager.cpp` 1191 / 1201 / 1243; `mt5_pool.cpp` 404 / 414 / 456 / 801).

C++ unit fixture uses `request.volume = 10000` (`mt5_http_client_pool_timeout_test.cpp:94`). That is **1.00 lot on the 4-digit scale**, matching `SMTMath::VolumeToInt(1.0)`. It is **not** hundredths (`100`) and **not** ext (`100000000`).

C# DTOs already keep the integer (`ulong VolumeNative` on `Mt5Deal`, `Mt5Position`, `Mt5DealDto`, `Mt5PositionDto`, `NormalizedDeal`). That part is correct. The missing piece is a **tested** converter whose default matches the **ext / report** law, with an explicit Manager factory for the current C++ integers.

---

## 5. Worked numbers (from the macros, not a live server)

| Lots | Hundredths (`*100`) — **not MT5** | Classic `Volume()` (`*10_000`) | Ext `VolumeExt()` (`*100_000_000`) |
|---:|---:|---:|---:|
| 1.00 | 100 | **10 000** | **100 000 000** |
| 0.10 | 10 | 1 000 | 10 000 000 |
| 0.01 | 1 | 100 | 1 000 000 |
| 0.0001 | 0 (cannot represent) | 1 | 10 000 |
| 0.00000001 | 0 | 0 | 1 |

Mis-scale blast radius for a real **1.00 lot** integer of `10 000` coming from `extractPosition` / `extractDeal`:

| Caller belief | Divisor | Computed lots | Error |
|---|---:|---:|---|
| Hundredths comment | 100 | **100.00** | **100× too large** |
| Classic `Volume()` (actual C++ path) | 10 000 | **1.00** | correct for that integer |
| Report / `VolumeExt` | 100 000 000 | **0.0001** | **10 000× too small** |

Mis-scale blast radius if a caller **sends** 1.00 lot using the wrong integer into `IMTRequest::Volume()`:

| Integer sent | Server sees (classic `Volume()`) |
|---:|---|
| 100 (comment) | **0.01 lot** |
| 10 000 (correct for this send path) | **1.00 lot** |
| 100 000 000 (ext integer on classic setter) | **10 000 lots** |

`MTAPI_VOLUME_MAX = 10_000_000_000` classic units = **1 000 000 lots**.  
`MTAPI_VOLUME_EXT_MAX` is the matching 8-digit ceiling.

---

## 6. Existing C# converter (measured, not redesigned here)

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

Already present. Product source was **not** changed by this agent.

| Fact | Measured value |
|---|---|
| Configurable scale | **yes** (`ctor(decimal scale = …)`) |
| `ManagerVolumeScale` | `10_000m` |
| `ExtendedVolumeScale` | `100_000_000m` |
| `HundredthsScale` | `100m` (constant only; no factory) |
| **Constructor default** | **`ManagerVolumeScale` (`10_000`)** — **not** `100_000_000` |
| Named factories | `Manager`, `Extended` |
| `ToLots` / `ToNative` | `native / Scale`; `Round(lots * Scale, AwayFromZero)` |
| Rejects `scale <= 0` | yes |
| Rejects negative lots | yes |
| Used by | `TradeReconstructor` → `volume ?? VolumeConverter.Manager` |
| Unit tests | **none** (no `*Tests*.cs` under `D:\Prop\tests\Unit`) |

The XML comment already states the `mt5_types.h` hundredths comment is incorrect. That comment is right. The **default scale is not** the ext/report default this task asks for.

A21 (`A21_reconstruction_spec.md`) adds a **third** in-repo convention: reconstruction arithmetic in **integer hundredths** (`volume_h`, 1.00 lot = 100) after an explicit adapter. That is a **domain remaining-volume** unit, not an SDK wire unit. Mixing A21 `volume_h` with raw `VolumeNative` without the adapter is the same 100× bug as believing `mt5_types.h:75`.

---

## 7. Recommendation (do not implement in this task)

Keep every wire / persist field as `ulong` native volume. Convert to lots in **one** type.

### 7.1 `VolumeConverter` — configurable, default `100_000_000`

Recommended shape (documentation only):

```csharp
namespace TraderIntelligence.Domain.Volume;

/// Converts MT5 integer volume ↔ lots.
/// Default scale is MTAPI_VOLUME_EXT_DIV (VolumeExt / FIELD_*_VOLUME_EXT / official reports).
/// Manager Volume() integers are 10_000 — pass ManagerVolumeScale or use VolumeConverter.Manager.
/// mt5_types.h "hundredths of lots" is not an MT5 Manager unit.
public sealed class VolumeConverter
{
    public const decimal ManagerVolumeScale   = 10_000m;        // Volume() / VolumeInitial() / VolumeMin
    public const decimal ExtendedVolumeScale  = 100_000_000m;   // VolumeExt() / FIELD_*_VOLUME_EXT
    public const decimal HundredthsScale      = 100m;           // MT4 / wrong comment; tests only

    public decimal Scale { get; }

    public VolumeConverter(decimal scale = 100_000_000m)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Volume scale must be positive.");
        Scale = scale;
    }

    public decimal ToLots(ulong native) => native / Scale;

    public ulong ToNative(decimal lots)
    {
        if (lots < 0)
            throw new ArgumentOutOfRangeException(nameof(lots));
        return (ulong)decimal.Round(lots * Scale, 0, MidpointRounding.AwayFromZero);
    }

    public static VolumeConverter Extended  => new(ExtendedVolumeScale);
    public static VolumeConverter Manager   => new(ManagerVolumeScale);
    public static VolumeConverter Hundredths => new(HundredthsScale); // tests / A21 adapter only
}
```

Why default **`100_000_000`** (this assignment) and not `10_000`:

1. Official **new** accuracy is 8 digits (`MTAPI_VOLUME_EXT_DIGITS`).
2. Official **dataset / report** path is `FIELD_*_VOLUME_EXT` + `÷ 100000000.0`.
3. `VolumeExtFromVolume` / WebAPI `ConvertToNewVolume` exist so old 4-digit integers can be lifted to ext. New domain code should speak ext unless the integer’s source is proven to be `Volume()`.
4. A constructor default of `100_000_000` makes the **unsafe** case (feeding classic `10 000` into the default converter) fail tests as `0.0001` lots instead of silently looking “almost right.”

Why the scale **must stay configurable**:

- Current C++ extractors and `IMTRequest::Volume()` are **4-digit**. Those callers **must** use `VolumeConverter.Manager` (or `new(10_000m)`) until they are switched to `VolumeExt()`.
- A21 reconstruction hundredths are a **downstream** integer, produced only after an adapter. Do not point `TradeReconstructor` at `Hundredths` and then pass raw `deal->Volume()`.

**Binding rule for callers:**

| Integer source | Factory |
|---|---|
| `VolumeExt()` / `VolumeClosedExt()` / `VolumeMinExt()` / `FIELD_*_VOLUME_EXT` / `TYPE_VOLUME_EXT` | `VolumeConverter.Extended` (**default ctor**) |
| `Volume()` / `VolumeInitial()` / `VolumeMin/Max/Step()` / current `mt5_types.h` extractors / `request.volume = 10000` fixture | `VolumeConverter.Manager` |
| A21 `volume_h` after a successful adapter | `VolumeConverter.Hundredths` (never on raw SDK integers) |
| Chart / tick / exchange `volume` | **do not convert** with this type |

If the existing default is flipped from `10_000` to `100_000_000`, `TradeReconstructor` **must keep** `VolumeConverter.Manager` for as long as `NormalizedDeal.VolumeNative` is filled from `deal->Volume()`. That flip is a later, reviewed change — not part of this report.

Do **not** treat `VolumeConverter` as a cTrader `OrderQty` converter. Architecture rule: source lots ≠ destination tag 38.

### 7.2 Tests for **both** official scales

Proposed file (not written): `D:\Prop\tests\Unit\Volume\VolumeConverterTests.cs`  
Project already references Domain: `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`.

Use `decimal` equality, not `double`. Pin **both** official scales plus a **negative** hundredths table.

#### A. Extended / default (`100_000_000`) — official report scale

| Lots | Native | Notes |
|---:|---:|---|
| 1.00 | 100 000 000 | `VolumeExtToInt(1.0)` |
| 0.10 | 10 000 000 | |
| 0.01 | 1 000 000 | |
| 0.0001 | 10 000 | smallest classic step, still representable in ext |
| 0.00000001 | 1 | one ext quantum |

Required assertions:

- `new VolumeConverter().Scale == 100_000_000m`
- `VolumeConverter.Extended.ToLots(100_000_000) == 1.00m`
- `VolumeConverter.Extended.ToNative(1.00m) == 100_000_000ul`
- `new VolumeConverter().ToLots(100_000_000) == 1.00m` (default == ext)
- round-trip: `ToLots(ToNative(lots)) == lots` for the table above
- `ToNative(0.01m) == 1_000_000ul`

#### B. Manager / classic (`10_000`) — current C++ extractor scale

| Lots | Native | Notes |
|---:|---:|---|
| 1.00 | 10 000 | `VolumeToInt(1.0)`; HTTP pool fixture |
| 0.10 | 1 000 | |
| 0.01 | 100 | **this 100 is 0.01 lot, not 1.00 lot** |
| 0.0001 | 1 | smallest classic quantum |

Required assertions:

- `VolumeConverter.Manager.Scale == 10_000m`
- `VolumeConverter.Manager.ToLots(10_000) == 1.00m`
- `VolumeConverter.Manager.ToNative(1.00m) == 10_000ul`
- `VolumeConverter.Manager.ToLots(100) == 0.01m` (kills the hundredths comment)
- `new VolumeConverter(10_000m)` matches `Manager`

#### C. Cross-scale and trap tests

- `Extended.ToLots(10_000) == 0.0001m` — feeding a classic integer into the default/ext converter.
- `Manager.ToLots(100_000_000) == 10_000m` — feeding an ext integer into the Manager converter (10 000 lots).
- `Hundredths.ToLots(100) == 1.00m` **and** `Manager.ToLots(100) != 1.00m` **and** `Extended.ToLots(100) != 1.00m`.
- `Extended.ToNative(1m) / Manager.ToNative(1m) == 10_000` (matches `VolumeExtFromVolume`).
- `Manager.ToNative(1m) == Extended.ToNative(1m) / 10_000` (matches `VolumeFromVolumeExt`).
- `new VolumeConverter(0)` and `new VolumeConverter(-1)` throw.
- `ToNative(-0.01m)` throws.
- `ToLots(0) == 0` on both official factories.

#### D. Do not test as PASS

- Any test that treats `PositionData.volume == 100` as 1.00 lot.
- Any test that applies a lot scale to `ChartBarData.volume` / `tick_volume`.
- Live server volume. These tests pin **SDK constants**, not a broker.

---

## 8. Residual uncertainty (stated, not papered over)

- **Not live-verified** against a running MT5 server. Numbers are from MetaQuotes headers + official examples in the vendored tree.
- **WebAPI JSON** in this SDK’s .NET sample stores **8-digit** on the wire and exposes 4-digit via a property converter. `MT5HttpClient` JSON-forwards `req.volume` as an integer with no rescale (`mt5_http_client.cpp`). Whether a remote HTTP service expects 4-digit or 8-digit is **not proven** from these headers. If that service is ext-first, the C++ client must convert before send.
- **Ticks / books:** `volume` vs `volume_ext` follow the same 4/8-digit split for last/book size. That is last-trade/book size, not always “lots.”
- One official NFA-style sample (cited in A38) calls `VolumeExtToSize(deal->Volume(), …)` — mixing the ext helper with the classic getter. That is an **SDK sample inconsistency**, not a third unit. Do not copy it.
- A21 hundredths are a **reconstruction remaining** unit. They are valid only **after** an adapter. They do not rehabilitate `mt5_types.h:75`.

---

## 9. Related on-disk reports (not re-opened here)

These already recorded pieces of the same conflict. This file is the dedicated A81 write-up and the converter/test recommendation.

- `D:\Prop\reports\swarm\20260818\A13_mt5_types_map.md` — binding: do not follow the hundredths comment; current DTO scale 10 000.
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` — domain `volume_h` hundredths **after** an adapter.
- `D:\Prop\reports\swarm\20260818\A37_mt5_deal_enums.md` — `Volume` vs `VolumeExt` field table.
- `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md` — full SDK unit law.
- `D:\Prop\reports\swarm\20260818\A43_position_sizing.md` / `A56_risk_list.md` — 100× P0 if the comment is believed.

---

## Sources (absolute)

- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIPosition.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIBook.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealWeekCache.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Entities\Mt5Deal.cs`
- `D:\Prop\src\Domain\Entities\Mt5Position.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`
