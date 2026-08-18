# A38 — MT5 Manager API volume units (SDK vs `mt5_types.h`)

**Date:** 2026-08-18  
**Agent:** A38  
**Scope:** Exact integer volume units in the vendored Manager API headers vs comments in `mt5_types.h`.  
**Product source:** not modified.  
**Method:** read-only comparison of SDK headers, official math helpers, SDK examples, and `D:\Prop\mt5-sdk\src\core\mt5_types.h`. No live broker/DealerSend measurement.

---

## Verdict (honest)

There is **no type, header, or identifier named `MT5APIVolume`** in the vendored SDK (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\`). Closest names:

| What people might mean | Actual SDK name | Path |
|---|---|---|
| Volume math / units | `MTAPI_VOLUME_*` / `MTAPI_VOLUME_EXT_*` + `SMTMath` | `Include\Classes\MT5APIMath.h` |
| Trade object volume | `IMTPosition::Volume` / `VolumeExt` (same pattern on deal/order/request) | `Include\Bases\MT5APIPosition.h`, `MT5APIDeal.h`, `MT5APIOrder.h`, `MT5APIRequest.h` |
| Dataset column types | `TYPE_VOLUME` (200) vs `TYPE_VOLUME_EXT` (202) | `Include\Bases\MT5APIDataset.h` |
| Dataset fields | `FIELD_DEAL_VOLUME_EXT`, `FIELD_POSITION_VOLUME` vs `FIELD_POSITION_VOLUME_EXT` | same |

The Manager API has **two** integer lot scales, not one:

1. **Classic / “old 4-digit” `Volume()`** — `lots * 10_000`. **Not hundredths.**
2. **Extended / “new 8-digit” `VolumeExt()`** — `lots * 100_000_000`.

`mt5_types.h` **conflicts with the SDK**. The only explicit unit comment on a trade `volume` field says **“hundredths of lots”**. That is the **MT4** scale (`lots * 100`). It is **wrong for every MT5 Manager volume getter/setter this product actually copies**.

The product’s pump/session extractors copy **`Volume()` / `VolumeInitial()`**, and `SendTrade` writes **`IMTRequest::Volume()`**, not `VolumeExt()`. So the integers that flow through `PositionData` / `DealData` / `OrderData` / `MT5TradeRequest` are **4-digit classic units** (`1.00 lot == 10000`), **unless some other layer converts them** (none found in `mt5-sdk\src`).

`volume * 100000000` is **only** correct for **`VolumeExt` / `TYPE_VOLUME_EXT` / `FIELD_*_VOLUME_EXT`**. Using that multiplier on `Volume()` overstates lots by **10_000×**.

---

## 1. Authoritative SDK constants

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
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

Conversion implementations (same file):

| Function | Formula | Meaning |
|---|---|---|
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, 4)` | lots → classic integer |
| `SMTMath::VolumeToDouble(vol)` | `vol / 10000.0`, 4 digits | classic integer → lots |
| `SMTMath::VolumeExtToInt(lots)` | `PriceToIntPos(lots, 8)` | lots → ext integer |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / 100000000.0`, 8 digits | ext integer → lots |
| `SMTMath::VolumeFromVolumeExt(ext)` | `ext / 10000` | 8-digit → 4-digit |
| `SMTMath::VolumeExtFromVolume(vol)` | `vol * 10000` | 4-digit → 8-digit |

Comments on the size helpers say **“lots”** explicitly:

```267:275:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
//| Volume conversion from lots to amount                            |
inline double SMTMath::VolumeToSize(const uint64_t volume,double contract_size)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV)*contract_size,MTAPI_VOLUME_DIGITS));
  }
//| Volume conversion from amount to lots                            |
```

There is **no** `100` divisor anywhere in the official volume math. Hundredths (`lots * 100`) is not an MT5 Manager unit.

Worked numbers (from the macros, not from a live server):

| Lots (double) | Hundredths (`*100`) — **not MT5** | Classic `Volume()` (`*10_000`) | Ext `VolumeExt()` (`*100_000_000`) |
|---|---:|---:|---:|
| 1.00 | 100 | **10 000** | **100 000 000** |
| 0.10 | 10 | 1 000 | 10 000 000 |
| 0.01 | 1 | 100 | 1 000 000 |
| 0.0001 | 0 (cannot represent) | 1 | 10 000 |
| 0.00000001 | 0 | 0 | 1 |

Ratio: `VolumeExt / Volume == 10_000` when both are populated for the same lot size.

`MTAPI_VOLUME_MAX = 10_000_000_000` classic units = **1_000_000 lots**.  
`MTAPI_VOLUME_EXT_MAX` is the corresponding 8-digit ceiling.

---

## 2. Which API field uses which scale

SDK comments are thin (“deal volume” vs “deal volume with extended accuracy”). They never say “hundredths”. The **8-digit** wording appears on the book struct and in the WebAPI sample.

### 2.1 Classic 4-digit (`/ 10000`)

Used by the non-`Ext` accessors. Examples:

- `IMTPosition::Volume()` — comment: `//--- position volume` (`MT5APIPosition.h` 131–133)
- `IMTDeal::Volume()` / `VolumeClosed()` — `//--- deal volume` / `//--- closed volume` (`MT5APIDeal.h` 140–142, 185–187)
- `IMTOrder::VolumeInitial()` / `VolumeCurrent()` (`MT5APIOrder.h` 203–208)
- `IMTRequest::Volume()` / `ResultVolume()` / `VolumeCurrent()` (`MT5APIRequest.h` 112–114, 152–153, 213–215)
- `IMTConfirm::Volume()`, `IMTExecution::OrderVolume()` / `DealVolume()`
- Symbol/group config: `VolumeMin` / `VolumeMax` / `VolumeStep` / `VolumeLimit` / `IEVolumeMax` (`MT5APIConfigSymbol.h` 701–712; group header has the same pair)
- Dataset: `TYPE_VOLUME = 200`

Official Manager example places **1.00 lot** with the 4-digit helper, not `*100` and not `*1e8`:

```186:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
```

`VolumeToInt(1.0)` = `10000`.

Another official report divides **`deal->Volume()` by `10000.0`**:

```628:628:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

WebAPI sample documents the same split as **“Old 4 digits volume”** vs **“New 8 digits volume”**:

```347:363:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs
    /// From new volume to old volume
    /// <param name="new_volume">New 8 digits volume</param>
    /// <returns>Old 4 digits volume</returns>
    public static ulong ConvetToOldVolume(ulong new_volume)
      {
       return(new_volume/10000);
      }
    /// From old volume to new volume
    /// <param name="old_volume">Old 4 digits volume</param>
    /// <returns>New 8 digits volume</returns>
    public static ulong ConvertToNewVolume(ulong new_volume)
      {
       return(new_volume*10000);
      }
```

Default symbol sample (`MTConSymbol.cs`): `VolumeStep = 10000`, `VolumeMax = 100000` → **0.01 / 10.00 lots** on the 4-digit scale.

### 2.2 Extended 8-digit (`/ 100000000`)

Used by every `*Ext` accessor. Comments say **“with extended accuracy”**, never “hundredths”.

- `IMTPosition::VolumeExt()` (`MT5APIPosition.h` 204–206)
- `IMTDeal::VolumeExt()` / `VolumeClosedExt()` / `VolumeGatewayExt()` (`MT5APIDeal.h` 229–234, 249–251)
- `IMTOrder::VolumeInitialExt()` / `VolumeCurrentExt()` (`MT5APIOrder.h` 264–269)
- `IMTRequest::VolumeExt()` / `ResultVolumeExt()` / `VolumeCurrentExt()`
- Config: `VolumeMinExt` / `VolumeMaxExt` / `VolumeStepExt` / `VolumeLimitExt` / `IEVolumeMaxExt` (`MT5APIConfigSymbol.h` 859–873)
- Dataset: `TYPE_VOLUME_EXT = 202`, `FIELD_DEAL_VOLUME_EXT = 2014`

Book struct is the only Manager header that names the digit count:

```24:25:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIBook.h
   int64_t           volume;                                // deal volume - only integer values
   int64_t           volume_ext;                            // deal volume with extended accuracy - 8 digits
```

Official Capital reports store **`FIELD_DEAL_VOLUME_EXT`** then divide by **`100000000.0`**:

```43:43:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp
   { IMTDatasetField::FIELD_DEAL_VOLUME_EXT      , true   , offsetof(DealRecord,volume)          },
```

```333:333:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp
   const double volume=fabs((deal.volume/100000000.0)*deal.contract_size*deal.rate_profit);
```

Same `/100000000.0` in `DealWeekCache.cpp`. That is **ext** volume, not classic `Volume()`.

WebAPI: `VolumeExt` comment = “deal volume with exta 8-digits accuracy” (`MTDeal.cs`). The wire field is stored as ext; the `Volume` property converts to/from old 4-digit via `/10000`.

### 2.3 Not lot-integer units (do not mix in)

These `volume` fields are **not** documented as `lots * 10000` / `lots * 1e8`:

| Field | Header / product comment | Unit |
|---|---|---|
| `MTChartBar::tick_volume` | “tick volume” | tick count |
| `MTChartBar::volume` | “volume” / product: “real (exchange) volume” | exchange volume, not lot-integer |
| `ChartBarData::tick_volume` | `mt5_types.h` 218: “number of ticks within the bar” | ticks |
| `ChartBarData::volume` | `mt5_types.h` 220: “real (exchange) volume” | exchange volume |
| `MTTick` / `MTTickShort::volume` | “last trade volume” | last trade size; companion `volume_ext` is “extended accuracy” |
| `TickData::volume` | no unit comment | copied from `tick.volume` in `mt5_tick_bridge.cpp` |

Do **not** apply `* 100`, `* 10000`, or `* 1e8` to chart/tick/exchange volume without a separate spec.

---

## 3. `mt5_types.h` comments vs SDK

File: `D:\Prop\mt5-sdk\src\core\mt5_types.h`

### 3.1 `PositionData::volume` — **WRONG comment**

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

Conflict:

| Claim | Implied 1.00 lot | SDK `IMTPosition::Volume()` | SDK `IMTPosition::VolumeExt()` |
|---|---:|---:|---:|
| “hundredths of lots” | 100 | 10 000 | 100 000 000 |

The product fills this field from **classic** `Volume()`, not hundredths and not ext:

```1495:1495:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = pos->Volume();
```

Same in `mt5_pool.cpp` (`MT5Session::extractPosition`).

If a caller believed the comment and sent `volume = 100` for 1.00 lot, `IMTRequest::Volume(100)` is **0.01 lot**. If they used `volume = 100000000` on `Volume()`, the server sees **10_000 lots**.

### 3.2 `DealData::volume` / `OrderData::volume` — **no unit comment**

```95:95:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;
```

```110:110:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;
```

Filled as classic 4-digit:

- deals: `deal->Volume()` (`mt5_manager.cpp` 1517, `mt5_pool.cpp` 855)
- orders: `order->VolumeInitial()` (`mt5_manager.cpp` 1534, `mt5_pool.cpp` 872)

Same physical unit as `PositionData.volume`. Only the position field is (mis)documented.

### 3.3 `MT5TradeRequest::volume` — **vague + dangling reference**

```143:151:D:\Prop\mt5-sdk\src\core\mt5_types.h
// One self-contained trade instruction passed to IMT5Client::SendTrade().
// Volume is in MT5 native integer units (see MT5_VOLUME_PER_LOT in
// trade_execution_service.cpp). No credentials/secrets are ever carried here.
struct MT5TradeRequest {
    ...
    uint64_t    volume = 0;          // MT5 native volume units
```

Conflicts / gaps:

1. **“MT5 native integer units” is ambiguous** — the SDK has two native integers.
2. **`MT5_VOLUME_PER_LOT` does not exist** anywhere under `D:\Prop` (grep: only this comment).
3. **`trade_execution_service.cpp` does not exist** in this tree.
4. Transport uses **classic** `request->Volume(req.volume)` (`mt5_manager.cpp` 1130, 1191, 1201, 1243; `mt5_pool.cpp` 404/414/456/801), **not** `VolumeExt()`.
5. Unit test `mt5_http_client_pool_timeout_test.cpp` sets `request.volume = 10000`, which is **1.00 lot on the 4-digit scale** (matches `VolumeToInt(1.0)` / `VolumeStep` default). That is evidence of intended classic units, not hundredths (`100`) and not ext (`100000000`).

So the trade-request comment is **not a second official scale**. It is an unresolved pointer. The **code path** is classic `Volume()`.

### 3.4 `SymbolData::volume_min/max/step` — **no unit comment**

Populated from `sym->VolumeMin()` / `VolumeMax()` / `VolumeStep()` (`mt5_manager.cpp` 722–724, 746–748; `mt5_pool.cpp` 601–603, 625–627) — **4-digit**, not `Volume*Ext()`.

### 3.5 In-file inconsistency

`mt5_types.h` itself disagrees with itself:

| Location | Comment | Implied scale |
|---|---|---|
| `PositionData.volume` L75 | “hundredths of lots” | `* 100` |
| `MT5TradeRequest` L144–151 | “MT5 native integer units” + missing `MT5_VOLUME_PER_LOT` | unspecified |
| `DealData` / `OrderData` / `SymbolData` | none | unspecified |
| `ChartBarData.volume` L220 | “real (exchange) volume” | **not** lots |

That is a documentation bug, not an extra API mode.

---

## 4. What this product actually transports

Read-only, no source change:

| Product field | Copied from / written to | SDK scale |
|---|---|---|
| `PositionData.volume` | `IMTPosition::Volume()` | 4-digit (`/10000`) |
| `DealData.volume` | `IMTDeal::Volume()` | 4-digit |
| `OrderData.volume` | `IMTOrder::VolumeInitial()` | 4-digit |
| `SymbolData.volume_*` | `IMTConSymbol::VolumeMin/Max/Step()` | 4-digit |
| `MT5TradeRequest.volume` | `IMTRequest::Volume()` | 4-digit |
| `TickData.volume` | `MTTickShort::volume` | last-trade volume (not lot-math) |
| `ChartBarData.volume` | chart real volume | exchange volume |

No `VolumeExt()` read/write was found under `D:\Prop\mt5-sdk\src`.

---

## 5. Direct answers to the assigned question

**`MT5APIVolume`:** not a symbol in this SDK. Units live in `MT5APIMath.h` (`MTAPI_VOLUME_DIV` / `MTAPI_VOLUME_EXT_DIV`) and in the `Volume` vs `VolumeExt` method pairs.

**`volume * 100000000`:** exact **only** for **extended** volume (`VolumeExt`, `TYPE_VOLUME_EXT`, `FIELD_*_VOLUME_EXT`). 1.00 lot = `100000000`. 8 decimal digits.

**Hundredths (`volume * 100`):** **not** an MT5 Manager API unit. It is what `mt5_types.h` claims on `PositionData.volume`. That comment is **false** relative to the SDK and relative to the extractors that fill the struct.

**Correct scale for this product’s current Manager mapping:** classic 4-digit:

```
lots = Volume / 10000.0
Volume = lround(lots * 10000)   // SMTMath::VolumeToInt
```

1.00 lot = **10000**. 0.01 lot = **100**. Smallest classic step = **0.0001 lot**.

---

## 6. Residual uncertainty (stated, not papered over)

- **Not live-verified** against a running MT5 server. Conclusion is from MetaQuotes headers + official examples in the vendored tree.
- **WebAPI JSON** in this SDK’s .NET sample stores **8-digit** on the wire and exposes 4-digit via a property converter. If any HTTP microservice used that convention, `MT5HttpClient` would have to convert. Current C++ HTTP client just JSON-forwards `req.volume` as an integer (`mt5_http_client.cpp`). Whether a remote service expects 4-digit or 8-digit is **not proven from these headers**.
- **Ticks/books:** `volume` vs `volume_ext` follow the same 4/8-digit split for **last/book size**, but that is last-trade/book size, not always “lots”.
- One official NFA example calls `VolumeExtToSize(deal->Volume(), …)` — mixing ext helper with classic getter. That is an **SDK sample inconsistency**, not a third unit. Do not copy it.

---

## Sources (absolute)

- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIPosition.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIOrder.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIRequest.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIBook.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APITick.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigSymbol.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTConSymbol.cs`
