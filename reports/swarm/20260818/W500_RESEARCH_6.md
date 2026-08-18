# W500_RESEARCH_6 — `IMTDeal.Volume()` scale is **10 000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_6.md` |
| Slot | **6** |
| Agent | W500_RESEARCH_6 (volume-unit pin) |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **No.** No manager/FIX passwords, proxy credentials, or `.env` values. |
| Assigned | Confirm `IMTDeal.Volume()` scale is **10000**, not hundredths (`100`), not `VolumeExt` (`1e8`). Goal context: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Method | Independent re-read of official Manager math + `IMTDeal` headers (Prop vendor SDK **and** YoPips vendor SDK), product extractors on both trees, C# `VolumeConverter` + reconstructor + native connector, unit tests, compiled Domain eval stdout already on disk, FIX send path. No live DealerSend / no live deal dump this slot. |

**One-line:** `IMTDeal::Volume()` is classic 4-digit lots (`native / 10_000`). Hundredths (`/100`) is an **MT4 comment bug**. `VolumeExt()` is a **different getter** at `/ 100_000_000`. Product copies `Volume()`, never `VolumeExt()`. Live `NewOrderSingle` is **off** (`SAFE_BY_ABSENCE` + `RealCopyEnabled=false`).

---

## 0. Verdict (binding)

| Claim | Verdict | Class |
|---|---|---|
| `IMTDeal::Volume()` integer scale is **10 000** (1.00 lot = `10000`) | **CONFIRMED** | Official `MTAPI_VOLUME_DIV` + `SMTMath::VolumeToDouble` |
| Scale is **hundredths** (1.00 lot = `100`) | **FALSE** | Only a **wrong comment** on `PositionData.volume` in `mt5_types.h` |
| Scale is **`VolumeExt` 1e8** (1.00 lot = `100_000_000`) | **FALSE for `Volume()`** | True **only** for `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` |
| Product ingest uses `Volume()`, not `VolumeExt()` | **CONFIRMED** | YoPips + Prop C++ `extractDeal`; C# `ReadDeals` → `d.Volume()` |
| `VolumeConverter` default matches that wire | **CONFIRMED** | ctor default / `Manager` = `10_000m` |
| Copy-to-cTrader can send a live order **today** | **NO** | No `35=D` builder; `RealCopyEnabled` forced `false` |
| Risk to capital **this slot / current process** | **NONE** | Fetch/reconstruct only. Wrong scale would become lethal **if** live send were armed |

Do **not** flip the C# default to `100_000_000` while extractors still copy `Volume()`. That shrinks every reconstructed lot by **10 000×** (`10000 / 1e8 = 0.0001` lots). Do **not** believe `mt5_types.h` “hundredths”: that **inflates** lots by **100×** (`10000 / 100 = 100` lots).

This slot did **not** attach Achiever or Starwave and print a live `deal->Volume()` integer. The unit is proven from the official math header + the getters this product actually calls. That is stronger than a single ticket and weaker than a live round-trip measurement. Do not greenwash “live-proven on ticket N.”

---

## 1. Two official Manager scales (none is 100)

Both vendor copies define the **same** constants. Copyright banner: MetaQuotes Ltd., 2000–2026.

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
`D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Classes\MT5APIMath.h`

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

YoPips header (re-read this pass, lines 12–19) is byte-identical on the two divisors:

```
MTAPI_VOLUME_DIV        (10000.0)
MTAPI_VOLUME_EXT_DIV    (100000000.0)
```

Conversion that **names** the classic integer as lots:

```262:265:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline double SMTMath::VolumeToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV),MTAPI_VOLUME_DIGITS));
  }
```

```297:300:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
```

Bridge between the two official integers (ratio **exactly 10 000**, not 100):

```283:286:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeFromVolumeExt(const uint64_t volume_ext)
  {
   return(volume_ext/10000);
  }
```

```318:321:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

There is **no** `MTAPI_VOLUME_*` divisor of `100`. Hundredths is not an MT5 Manager unit.

Worked identity (from the macros, not from a live server):

| Lots | Hundredths (`*100`) — **not MT5** | Classic `Volume()` (`*10_000`) | Ext `VolumeExt()` (`*100_000_000`) |
|---:|---:|---:|---:|
| 1.00 | 100 | **10 000** | **100 000 000** |
| 0.10 | 10 | 1 000 | 10 000 000 |
| 0.05 | 5 | 500 | 5 000 000 |
| 0.01 | 1 | 100 | 1 000 000 |
| 0.0001 | 0 (cannot represent) | 1 | 10 000 |

`VolumeExt / Volume == 10_000` for the same lot size. That is why treating a `Volume()` integer as `VolumeExt` is a **10 000×** error, not a 100× error.

---

## 2. `IMTDeal` has two getters. The assigned name is the classic one.

YoPips header (Prop vendor copy is the same interface):  
`D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`

Classic (assigned topic):

```140:142:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume
   virtual uint64_t  Volume(void) const=0;
   virtual MTAPIRES  Volume(const uint64_t volume)=0;
```

Extended (different vtable slot, different scale):

```229:231:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume with extended accuracy
   virtual uint64_t  VolumeExt(void) const=0;
   virtual MTAPIRES  VolumeExt(const uint64_t volume)=0;
```

SDK comments never say “hundredths.” They say “deal volume” vs “deal volume with extended accuracy.” Units come from `SMTMath`, not from the `IMTDeal` comment.

Official Manager sample places **1.00 lot** with the **4-digit** helper, not `*100` and not `*1e8`:

```186:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
```

`VolumeToInt(1.0)` = `PriceToIntPos(1.0, MTAPI_VOLUME_DIGITS=4)` = **10000**.

Official report code that **reads `Volume()`** (not `VolumeExt()`) divides by **10000.0** in place:

```619:628:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      if(deal->Volume()==0 ||
        fabs(deal->ContractSize())<DBL_EPSILON ||
        fabs(deal->RateProfit())<DBL_EPSILON ||
        _isnan(deal->RateProfit()))
         continue;
      //--- compute count
      m_deals_count[groupNum][reason][index]++;
      deals_processed++;
      //--- compute volume
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

Web API sample names the two formats explicitly (“old 4-digits” vs “new 8-digits”) and uses the same `*10000` bridge — **not** `*100`:

```107:129:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\PHP\mt5_api\mt5_utils.php
  /**
   * Convert new 8-digits volume to old 4-digits format
   */
  public static function ToOldVolume($new_volume)
    {
     return (int)$new_volume / 10000;
    }
  /**
   * Convert old 4-digits volume to new 8-digits format
   */
  public static function ToNewVolume($old_volume)
    {
     return (int)$old_volume * 10000;
    }
```

Dataset path is **ext-first** for deals (`FIELD_DEAL_VOLUME_EXT = 2014`, comment “deal volume with extended accuracy”). That is why official Capital / transaction reports call `VolumeExt()` / `VolumeExtToDouble`. It does **not** change `IMTDeal::Volume()`.

---

## 3. Product extractors copy `Volume()`, never `VolumeExt()`

### 3.1 YoPips (the live Manager backend this lab copies)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`

```1508:1518:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
DealData MT5Manager::extractDeal(const IMTDeal* deal) {
    DealData d;
    d.ticket = deal->Deal();
    d.login = deal->Login();
    d.order = deal->Order();
    d.position = deal->PositionID();
    d.symbol = StringUtils::toUtf8(deal->Symbol());
    d.action = deal->Action();
    d.entry = deal->Entry();
    d.volume = deal->Volume();
```

Same file, positions: `d.volume = pos->Volume();` (L1495).  
`mt5_pool.cpp` mirrors it: `deal->Volume()` at L787, `pos->Volume()` at L765. Send path writes `request->Volume(...)` (classic setter), not `VolumeExt`.

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `VolumeExt`: **0 hits**.

YoPips itself converts **legacy native volume as `/ 10000.0L`**, not `/100` and not `/1e8`:

```1418:1426:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.cpp
TradeExecutionService::LegacyVolumeResult
TradeExecutionService::checkLegacyNativeVolume(uint64_t units) {
    // ...
    const long double exactLots = static_cast<long double>(units) / 10000.0L;
    const double lots = static_cast<double>(exactLots);
```

That is an independent product-side confirmation that integers coming off `IMTDeal::Volume()` / `IMTRequest::Volume()` are 4-digit classic.

### 3.2 Prop C++ SDK copy

`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` `extractDeal` is the same binding: `d.volume = deal->Volume();` (L1517).  
`mt5_pool.cpp`: `deal->Volume()` (L855).  
Grep of `D:\Prop\mt5-sdk\src` for `VolumeExt`: **0 hits**.

### 3.3 Prop C# native collector (live ALL-groups path)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadDeals` materializes `CIMTDeal` (managed wrapper of `IMTDeal`) into `Mt5DealDto.VolumeNative` via **`d.Volume()`**, not `VolumeExt()`:

```416:424:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            rows.Add(new Mt5DealDto(
                (long)d.Deal(),
                (long)d.Login(),
                (long)d.Order(),
                (long)d.PositionID(),
                d.Symbol(),
                (DealAction)d.Action(),
                (DealEntry)d.Entry(),
                d.Volume(),
```

Positions: `p.Volume()` (L396).  
Grep of `D:\Prop\src` for `VolumeExt(`: **only the XML comment** on `VolumeConverter`. No call site.

`Mt5DealDto.VolumeNative` is `ulong` (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs` L32). It is **not** lots. Lots happen once, later, in reconstruction.

### 3.4 The hundredths comment is a documentation bug on the **same** integer

YoPips and Prop share:

```70:75:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

`DealData.volume` (L95) has **no** unit comment and is filled from `deal->Volume()`. The comment is the **MT4** `lots * 100` convention. It is **wrong** for every Manager getter this product copies. Do not implement that comment.

---

## 4. C# default is 10 000 and is locked to `Volume()`

`D:\Prop\src\Domain\Volume\VolumeConverter.cs`

```3:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
/// Converts MT5 native integer volume to lots.
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal-&gt;Volume(), so the default scale is 10_000.
public sealed class VolumeConverter
{
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;
    // ctor default = ManagerVolumeScale
    public decimal ToLots(ulong native) => native / Scale;
    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
}
```

`TradeReconstructor` fallback is `volume ?? VolumeConverter.Manager` and converts **once**:

```89:89:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            var lots = _volume.ToLots(deal.VolumeNative);
```

Architecture pin (implementation map, not a second scale):

```23:23:D:\Prop\docs\architecture.md
- Volume default scale = 10_000 (`IMTDeal.Volume()`)
```

Demo tape uses the same scale. `DemoBrokerFactory.VolumeScale = 10_000m`; Achiever 0.10 / 0.20 / 0.40 lots become native `1000` / `2000` / `4000`; Starwave 0.05 lots becomes native `500`.

Unit tests (`D:\Prop\tests\Unit\VolumeConverterTests.cs`):

| Fact | Assertion |
|---|---|
| Manager scale | `Scale == 10_000m` |
| 0.10 lot | `ToNative(0.10m) == 1000`, `ToLots(1000) == 0.10m` |
| Extended (opt-in) | `1` lot ↔ `100_000_000` |
| Hundredths is **not** default | `Manager.Scale != HundredthsScale` |

Independent compiled Domain eval already on disk (`D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt`, measured 2026-08-18T13:46:01+05:30):

```text
ctor_default_Scale=10000
Manager.Scale=10000
Extended.Scale=100000000
default_eq_Manager=True
default_eq_Extended=False
Manager.ToLots(10000)=1
Manager.ToLots(1000)=0.1
Manager.ToLots(100)=0.01
Extended.ToLots(10000)=0.0001
Extended.ToLots(100000000)=1
default.ToNative(1)=10000
Extended.ToNative(1)=100000000
ratio_ext_div_mgr=10000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

`new VolumeConverter().ToLots(10000) == 1`. Feeding the same classic integer to the ext scale yields **0.0001** lots. Reconstruction `FlatEpsilon` is far below that, so a 1e8 default would accept a **silent 10 000× undersize** as a real trade.

---

## 5. Blast radius if the scale is wrong (why this slot exists)

Assume a live Achiever/Starwave deal whose `IMTDeal::Volume()` is **10000** (1.00 lot XAUUSD). That is the integer `ReadDeals` will store as `VolumeNative`.

| Mis-scale | Lots computed | Error vs truth | If that lot size were later sent live |
|---|---:|---|---|
| Correct `/ 10_000` | **1.00** | 1× | intended size |
| Hundredths `/ 100` (believe `mt5_types.h`) | **100.00** | **100× too large** | account-destroying oversize |
| Ext `/ 1e8` (treat `Volume()` as `VolumeExt`) | **0.0001** | **10 000× too small** | looks like a fill, is not the trader |
| Ext integer written into `request->Volume()` | `100_000_000` classic | **10 000 lots** | catastrophic oversize the other way |

A81 was **right** that `1e8` is the official **ext** scale and **wrong** to recommend it as the constructor default while extractors copy `Volume()`. B14 / D14 / D92 / A006 / A38 already voted **10 000**. This slot re-measured the same binding on **both** trees and does not change the vote.

Sizing law still applies even after the unit is correct: architecture §1.10 — never blindly convert MT5 lots into cTrader `OrderQty`. `QuantityNormalizer` is dest min/step/max only. There is no `IQuantityConverter`. That is a **later** gate, not an excuse to pick the wrong MT5 divisor.

---

## 6. Fetch-all + no-live-send (goal, this slot)

Goal text: *fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss).*

| Goal piece | This-slot measurement | Honest status |
|---|---|---|
| Volume unit for those deals | Classic `Volume()` / **10 000** | **Pinned.** Required before any sizing |
| ALL groups / ALL logins | Not this slot’s attach. Recipe is YoPips `GetAllGroups` + `GetUserLogins` (A004). Plan-group labels are **not** fetch filters (architecture.md L24) | **Do not** filter ingest by mapped plans |
| Live copy / no loss | `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35). DI **forces** `RealCopyEnabled = false` (`DependencyInjection.cs` L40–41: “Live NewOrderSingle is not implemented”). FIX worker `GetValue(..., false)` and still **refuses** send. Grep of `D:\Prop\src` `*.cs` for a `35=D` / `NewOrderSingle` **builder**: comments and FSM helpers only — **no socket write** | **SAFE_BY_ABSENCE + forced false.** Current process cannot lose money on cTrader |

`LiveRuntimeStatus.Snapshot()` copy note when the flag is false:

```42:43:D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs
        copyNote = RealCopyEnabled
            ? "LIVE SEND ARMED — unexpected"
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
```

API `/api/settings` exposes `featureFlags.REAL_COPY_EXECUTION_ENABLED` from that runtime object. DI never arms it.

**Honesty:** vacuous “cannot send because nothing can send” is **not** Architecture §70 PASS. It **is** the no-loss condition required for this fetch wave. Do not enable `REAL_COPY_EXECUTION_ENABLED=true` from this report.

---

## 7. What this slot did **not** measure

- No live `DealRequest` / `DealRequestByGroup` against Achiever or Starwave this pass. No ticket integer printed.
- No SHA-256 of the headers (no shell in this agent). Constants were read from the files, not hashed.
- No unit-test run this pass. Tests exist and assert the 10 000 identity; D92 eval stdout is the last compiled measurement on disk.
- Did not edit `mt5_types.h` to fix the hundredths comment (out of scope).

None of those holes change the unit. They bound the claim: **SDK + extractor binding CONFIRMED; live ticket integer NOT re-sampled here.**

---

## 8. Sources re-read this pass (absolute paths)

| Path | Why it counts |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h` | Official `MTAPI_VOLUME_DIV=10000.0` / `EXT=1e8` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Classes\MT5APIMath.h` | Same macros on the production C++ tree |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` | `Volume()` vs `VolumeExt()` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `extractDeal` → `deal->Volume()` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` | Wrong “hundredths” comment |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.cpp` | Legacy native `/ 10000.0L` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same `extractDeal` binding |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Live C# path copies `d.Volume()` |
| `D:\Prop\src\Domain\Volume\VolumeConverter.cs` | Default scale **10 000** |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | Lots = `ToLots(VolumeNative)` via Manager |
| `D:\Prop\tests\Unit\VolumeConverterTests.cs` | 0.10 lot ↔ 1000 native |
| `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt` | Compiled `ToLots(10000)=1` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Real copy default false |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real copy forced false |
| `D:\Prop\apps\fix-worker\Worker.cs` | No send; stamps NewOrderSingle off |

**Slot 6 verdict: CONFIRMED.** Use **10 000** for every `IMTDeal.Volume()` / `CIMTDeal.Volume()` / `DealData.volume` / `Mt5DealDto.VolumeNative` integer on the Achiever+Starwave fetch. Do not use 100. Do not use 1e8 unless the getter in hand is literally `VolumeExt()`. Do not send live cTrader orders.
