# W500_RESEARCH_146 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_146.md` |
| Slot | **146** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 146 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Flag names and booleans only. |
| Method | Independent re-read of official Manager SDK math + `IMTDeal`, Prop + YoPips extractors, C# native reader + `VolumeConverter` + reconstructor + fixtures, FIX send path, copy gates. Cross-checked E004 VolumeConverter TRX (2026-08-18T13:48:20+05:30), D92 compiled Domain eval (13:46:01+05:30), and `LIVE_GROUPS_AND_TRADERS.json` (08:42:16Z). This slot did **not** live-attach a manager and did **not** send FIX. |
| Related (not rubber-stamped) | Slots 6/26/46/66/86/106/126, A38, A81, B14, D14, D92, A006. Volume-scale conclusion independently re-confirmed. Slot 66/108 “DI pins `RealCopyEnabled=false`” is **stale**. |

---

## 0. Verdict (binding)

**CONFIRMED.** The integers this product copies from `IMTDeal::Volume()` are classic Manager volume:

```text
lots = IMTDeal::Volume() / 10000
1.00 lot  ==  10000   (MTAPI_VOLUME_DIV, 4 digits)
0.10 lot  ==   1000
```

| Claim | Result |
|---|---|
| `IMTDeal::Volume()` scale is **10000** | **YES** — official `MTAPI_VOLUME_DIV` / `SMTMath::VolumeToDouble` / `VolumeToInt(1.0)` |
| Scale is hundredths (`/ 100`) | **NO** — MT4 convention. Wrong comment in `mt5_types.h:75` (both trees). Not an official MT5 Manager scale. |
| Scale is `VolumeExt` (`/ 1e8`) | **NO** — that is `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` / `FIELD_DEAL_VOLUME_EXT` only |
| Product extractors call `VolumeExt()` | **NO** — 0 calls under `D:\Prop\mt5-sdk\src`, 0 under `D:\Prop\src` (except one XML comment), 0 under `D:\Prop\apps`, 0 under YoPips `src` |
| C# default converter matches the wire | **YES** — `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor binds `VolumeConverter.Manager` |
| Unit tests pin 10000 | **YES** — E004 `VolumeConverterTests` 3/3 **Passed**; reconstruction fixtures treat `VolumeNative=1000` as **0.10** lots |
| Live cTrader send uses this scale today | **N/A** — there is **no** `35=D` / `NewOrderSingle` sender. Capital at risk from copy = **NONE** (`SAFE_BY_ABSENCE`) |

Do **not** flip the default to `100` or `100_000_000` while extractors still copy `deal->Volume()` / `d.Volume()`. That is a silent **100×** or **10_000×** sizing bug the moment live copy is armed.

A81 documented both official scales, then **wrongly** recommended ctor default `1e8`. B14 / D14 / D92 / slots 66/106/126 keep **10 000**. This slot independently re-read the same sources and **agrees with B14/D92**.

**Flag drift vs older slots (honesty, not a volume-scale change):** `DependencyInjection.cs:41` **binds** `REAL_COPY_EXECUTION_ENABLED` from configuration. Lab `.env` may be `true`. `CTraderFixLogonHostedService` **no longer** overwrites the flag to false. This still does **not** send orders: `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, and product C# still has **0** `35=D` builders.

---

## 1. Official Manager SDK (authoritative)

### 1.1 Two scales, not three

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
Same text: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
Copyright: MetaQuotes Ltd., 2000–2026.

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

There is **no** `#define` for `100` and no “hundredths” string in this header.

`VolumeToInt` is `PriceToIntPos(volume, MTAPI_VOLUME_DIGITS)` with `digits=4`. `s_decimal[4] = 10000.0` (`MT5APIMath.h:66–72`). Therefore:

```text
SMTMath::VolumeToInt(1.0)  = 1.0 * 10000.0  = 10000
SMTMath::VolumeToDouble(n) = n / 10000.0
```

| Helper | Formula | 1.00 lot integer |
|---|---|---:|
| `SMTMath::VolumeToDouble(vol)` | `vol / MTAPI_VOLUME_DIV` (4 digits) | **10 000** |
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, 4)` | **10 000** |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / MTAPI_VOLUME_EXT_DIV` (8 digits) | 100 000 000 |
| `SMTMath::VolumeFromVolumeExt(ext)` | `ext / 10000` | 8-digit → 4-digit |
| `SMTMath::VolumeExtFromVolume(vol)` | `vol * 10000` | 4-digit → 8-digit |

Ratio `MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10_000`. Mixing the two getters is an exact **10 000×** error.

```256:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeToInt(const double volume)
  {
   return(PriceToIntPos(volume,MTAPI_VOLUME_DIGITS));
  }
inline double SMTMath::VolumeToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV),MTAPI_VOLUME_DIGITS));
  }
// ...
inline uint64_t SMTMath::VolumeFromVolumeExt(const uint64_t volume_ext)
  {
   return(volume_ext/10000);
  }
// ...
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
// ...
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

`SMTFormat::FormatVolume` calls `VolumeToDouble` (classic). `FormatVolumeExt` calls `VolumeExtToDouble` (ext). Two formatters, two scales.

### 1.2 `IMTDeal` exposes **both** getters

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`

Classic (comment: `//--- deal volume`):

```140:142:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume
   virtual uint64_t  Volume(void) const=0;
   virtual MTAPIRES  Volume(const uint64_t volume)=0;
```

Extended (comment: `//--- deal volume with extended accuracy`):

```229:231:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume with extended accuracy
   virtual uint64_t  VolumeExt(void) const=0;
   virtual MTAPIRES  VolumeExt(const uint64_t volume)=0;
```

The classic getter never says “hundredths.” Extended is a **separate** pair (`VolumeExt` / `VolumeClosedExt` / `VolumeGatewayExt`).

### 1.3 Official samples that consume `deal->Volume()` divide by **10000.0**

`Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp:628`:

```text
DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

Same classic path: `AgentsDetailed.cpp:460` (`SMTMath::VolumeToDouble(m_deal->Volume())`), `FeedCommission\PluginInstance.cpp:458`, `GatewayUtils.h:87–96`.

Official **Capital** reports that divide by `100000000.0` (`DealCache.cpp:333`) do so **after binding `FIELD_DEAL_VOLUME_EXT`** (`DealCache.cpp:43`). That is the ext dataset column, **not** `IMTDeal::Volume()`. Using that divisor on `Volume()` is the A81 blast (`1.00 lot` integer `10000` → `0.0001` lots).

WebAPI utils (`MTUtils.cs:352–363`) name the conversion explicitly: “new 8 digits volume” ↔ “old 4 digits volume” via `×/÷ 10000`. Same two official scales.

---

## 2. What this product actually copies

### 2.1 C++ extractors copy `deal->Volume()` unchanged

| File | Line | Assignment |
|---|---:|---|
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | 1517 | `d.volume = deal->Volume();` |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | 855 | `d.volume = deal->Volume();` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | 1517 | `d.volume = deal->Volume();` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | 787 | `d.volume = deal->Volume();` |

```1508:1518:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
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

Positions copy `pos->Volume()`. Orders copy `order->VolumeInitial()`. Send path writes `request->Volume(...)` (classic setter), **not** `VolumeExt()`.

`grep VolumeExt` under `D:\Prop\mt5-sdk\src` and YoPips `src`: **0 hits**.

### 2.2 YoPips production already converts those integers by **10000**

This is independent corroboration that the same extractors are classic 4-digit units:

| File | Evidence |
|---|---|
| `trade_execution_service.h:244` | `static constexpr double MT5_VOLUME_PER_LOT = 10000.0;` plus comment `1.00 lot == 10000 units` |
| `trade_execution_service.cpp:753 / 1425` | `volume / MT5_VOLUME_PER_LOT` and `units / 10000.0L` |
| `terminal_state_service.h:115` | same `MT5_VOLUME_PER_LOT = 10000.0` |
| `worker_service.cpp:547 / 650` | `pos.volume / 10000.0`, `deal.volume / 10000.0` |
| `trade_service.cpp:92 / 117` | `lot_size = *.volume / 10000.0` |
| `symbol_controller.cpp` | `volumeMin/Max/Step / 10000.0` |
| `export_controller.cpp:146` | `deal.volume / 10000.0` |

YoPips never divides those extracted integers by `100` or `1e8`.

### 2.3 C# live connector copies `d.Volume()` into `VolumeNative`

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadDeals` (`CIMTDeal` managed wrapper of `IMTDeal`):

```416:425:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`Mt5DealDto.VolumeNative` is that raw `ulong`. `EfTradingStore` persists `VolumeNative` with **no** rescale. `TradeReconstructor` converts later via `VolumeConverter.Manager`.

`grep VolumeExt` under `D:\Prop\src` and `D:\Prop\apps`: **one XML comment** in `VolumeConverter.cs:6`. Zero product calls.

### 2.4 Domain default is 10 000 and reconstructor binds it

```3:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal->Volume(), so the default scale is 10_000.
public sealed class VolumeConverter
{
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;
    public VolumeConverter(decimal scale = ManagerVolumeScale)
    // ...
    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
}
```

```18:21:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
```

```89:89:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            var lots = _volume.ToLots(deal.VolumeNative);
```

Architecture pin (`D:\Prop\docs\architecture.md:23`): `Volume default scale = 10_000 (`IMTDeal.Volume()`)`.

---

## 3. The hundredths comment is a bug, not a unit

Both trees, same wrong comment:

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

`DealData.volume` in the same file has **no** unit comment but is filled from the same `deal->Volume()`. `MT5TradeRequest.volume` says “MT5 native integer units” and points at `MT5_VOLUME_PER_LOT` in YoPips `trade_execution_service.cpp` — that constant is **10000.0**, not 100.

Hundredths (`lots * 100`) is the **MT4** `OrderLots` integer convention. It is **not** an official MT5 Manager scale. Implementing that comment would turn `1.00 lot` (`10000`) into **100.00 lots** — a **100×** oversize.

`VolumeConverter.HundredthsScale = 100m` exists only so tests can assert it is **not** the default.

---

## 4. Measured tests and compiled eval

### 4.1 Unit fixtures treat `1000` as `0.10` lots

`VolumeConverterTests` (3 facts):

| Fact | Pin |
|---|---|
| `Manager_scale_maps_0_10_lots_to_1000_native` | `Scale == 10_000`; `ToNative(0.10) == 1000`; `ToLots(1000) == 0.10` |
| `Extended_scale_maps_one_lot_to_100_million` | `ToNative(1) == 100_000_000` (ext only) |
| `Hundredths_comment_is_not_the_default` | `Manager.Scale != 100` |

`TradeReconstructionTests` constructs `VolumeNative = 1000` and asserts `InitialVolumeLots == 0.10m` (and `MaxVolumeLots == 0.20m` after two 1000-unit INs). The reconstructor is constructed as `new(VolumeConverter.Manager)`. If the scale were 100, those fixtures would read **10.00 lots**. If 1e8, **0.00001 lots**.

### 4.2 E004 measured run (prior this calendar day; not re-executed this slot)

`D:\Prop\reports\swarm\20260818\E004_tests.md`, TRX start `2026-08-18T13:48:20+05:30`:

| Class | Total | Passed | Failed |
|---|---:|---:|---:|
| `VolumeConverterTests` | 3 | **3** | 0 |
| `TradeReconstructionTests` | 6 | **6** | 0 |

Solution unit lane: 64 passed / 22 skipped / 0 failed.

### 4.3 D92 isolated Domain eval (compiled, 13:46:01+05:30)

```text
ctor_default_Scale=10000
Manager.Scale=10000
Extended.Scale=100000000
default.ToLots(10000)=1
Extended.ToLots(10000)=0.0001
default.ToNative(1)=10000
Extended.ToNative(1)=100000000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

`1.00` lot on the current wire is integer **`10 000`**. Feeding that integer to A81’s recommended default yields **`0.0001` lots**. Reconstruction `FlatEpsilon` is `0.0000001m`, so `0.0001` is accepted as a real trade **10 000× too small**. That is why the ctor default must stay **10 000** while extractors copy `Volume()`.

---

## 5. Blast radius if the scale is wrong

| Wrong divisor applied to `Volume()=10000` (1.00 lot) | Computed lots | Effect if live copy were armed |
|---|---:|---|
| `/ 100` (hundredths comment) | **100.00** | **100× oversize** send |
| `/ 10000` (correct) | **1.00** | correct |
| `/ 1e8` (`VolumeExt`) | **0.0001** | **10 000× undersize** recon; if later used as lots→native-ext, inverse send oversize |

Copy pipeline today: `ToLots(VolumeNative)` → `trade.MaxVolumeLots` → `QuantityNormalizer.Normalize(lots, 0.05, GoldSpec)` (`1.00 → 0.05`). Wrong scale would poison `RequestedQuantity` **before** any future `35=D` is written. The absence of a sender is why that bug cannot lose money **today**.

---

## 6. ALL Achiever + Starwave groups / traders

Ingest path is **not** plan-filtered:

- Groups: `GroupRequestArray("*")` then fallback `GroupTotal`/`GroupNext` (`NativeMt5BrokerConnector.GetGroupsCore` L155–182).
- Traders: `GetAccountsAsync(null)` walks **every** group via `UserRequestArray` (L223), cache `UserGetByGroup` only on hard fail, then `UserLogins` + `UserRequestByLogins`.
- Deals: `DealRequestByGroup` per group (`GetGroupDealsCore` L307) → `ReadDeals` → `d.Volume()`.
- Positions: `GetGroupPositionsAsync("*")` or per-login.

`DealIngestionService.SyncCatalogAsync` uses `GetGroupsAsync` + `GetAccountsAsync(null)`. `SyncBrokerAsync` walks all groups for deals and `"*"` for positions. **No** `Take(` on that path.

Live census **re-summed this slot** from `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (probe `2026-08-18T08:42:16.8519545+00:00`). This slot did **not** re-attach. Passwords are not in this report.

| Broker | Connect (prior) | Groups | Group names (accounts) | Traders | Open pos |
|---|---|---:|---|---:|---:|
| Achiever | HTTP proxy | **8** | `contest\yo-1step`(2), `contest\yo-2step`(179), `contest\yo-instant`(4), `contest\yo-payp`(5), `demo\yo-1step`(4), `demo\yo-2step`(6295), `demo\yo-instant`(0), `demo\yo-payp`(23) | **6512** | 1506 |
| StarwaveFX | Direct | **10** | `Starwave\cent\FX1\grp1`(11), `grp2`(4), `demo\FX2\grp1`(170), `grp2`(1735), `real\FX3\grp1`(22), `grp2`(0), `grp3`(0), `grp4`(4), `grp5`(0), `LP`(2) | **1948** | 478 |
| **Total** | | **18** | | **8460** | **1984** |

Group-account checksums: Achiever `2+179+4+5+4+6295+0+23 = 6512`. Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948`. `8+10=18`, `6512+1948=8460`. Matches `CREDENTIALS_AND_COPY_STATUS.md`.

Every deal those walks return is `d.Volume()` / scale **10 000**. Volume scale is independent of the census count.

---

## 7. Copy to cTrader must not send live orders (no loss)

| Gate | Measured state |
|---|---|
| `CTraderFixSession` outbound `MsgType` | **Only** `(35, "A")` Logon. One `WriteAsync`. No `35=D`. File 135 lines. |
| Product `*.cs` `35=D` / `NewOrderSingle` builder | **0** senders. Name exists only as a `const bool = false` and comments. |
| `CopyTradingService.NewOrderSingleImplemented` | **`false`** (const) |
| `CopyTradingService.VenueReconciled` | **`false`** (const) |
| Persisted `AllowFixSend` | **hardcoded `false`** (`CopyTradingService.cs:192`) even after `RiskEngine.Evaluate` |
| LIVE promotion | Trade #3 cannot auto-LIVE (`CanPromoteToLive` remains false; `FromBaseline` has no LIVE) |
| Live send branch | Requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — currently **unreachable**. Else `SHADOW_ONLY`. |
| Blocker text | `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` |

`CTraderFixOptions.RealCopyExecutionEnabled` default is **false**. DI now **binds** env `REAL_COPY_EXECUTION_ENABLED` onto `LiveRuntimeStatus.RealCopyEnabled`. Lab `.env` may be `true`. That is an **arm**, not a sender. `/api/settings` exposes `runtime.RealCopyEnabled` (so it can read true) while the TRADE socket still cannot emit `NewOrderSingle`.

YoPips C++ `src` has **0** cTrader FIX senders. This slot did not send.

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`).

---

## 8. Honesty residuals

1. This slot did **not** attach to live Manager to print a live `deal->Volume()` integer next to a known 0.10 lot ticket. Proof is official SDK math + extractors + compiled Domain eval + unit fixtures. Stronger than a single ticket; weaker than a live round-trip. Do not greenwash “live-proven on ticket N.”
2. Census JSON is **prior** (08:42Z). Counts re-summed; not re-probed.
3. Slot 66/108 “process pins `RealCopyEnabled=false`” is **stale**. Volume scale is unaffected.
4. A81’s **facts** (two official scales; Capital `/1e8` is `FIELD_DEAL_VOLUME_EXT`) remain true. A81’s **ctor-default=1e8** recommendation remains **wrong for this tree today**.
5. `PositionData.volume` comment still says “hundredths.” Do not implement it.

---

## 9. Files read (absolute)

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIFormat.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Capital.Standard.Reports\Cache\DealCache.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\worker_service.cpp`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Entities\Mt5Deal.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\tests\Unit\VolumeConverterTests.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (headers + groupName totals only)
- `D:\Prop\reports\swarm\20260818\E004_tests.md`
- `D:\Prop\reports\swarm\20260818\D92_volume_vote.md`

---

## 10. Slot 146 close

**Verdict: CONFIRMED.** `IMTDeal::Volume()` / product `VolumeNative` = **÷ 10000**. Not hundredths. Not `VolumeExt` 1e8. ALL-groups/ALL-traders catalog remains `*` + every user (prior **18 / 8460**, re-summed). Copy cannot send live cTrader orders (`35=D` absent). **Risk to capital: NONE.**
