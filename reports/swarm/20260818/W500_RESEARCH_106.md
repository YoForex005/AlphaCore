# W500_RESEARCH_106 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_106.md` |
| Slot | **106** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (independent re-read of SDK + extractors + Domain + FIX; **no** live Manager/TLS re-attach) |
| Agent | W500 research subagent, slot 106 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** |
| Live attach this pass | **No** |
| Live `35=D` this pass | **No** (builder absent; flags refuse send) |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read official `MT5APIMath.h` / `MT5APIDeal.h` (both trees), `extractDeal` in YoPips + `D:\Prop\mt5-sdk\src`, C# `ReadDeals` / `VolumeConverter` / tests, ingest persist, Fake `Lots()`, FIX session (`35=A` only), DI + copy service. No password values. |
| Sibling re-measures (do not treat as this file) | W500_RESEARCH_6 / 26 / 46 / 66 / 86, A38, A81, B14, D14, D92, A006 |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**CONFIRMED.** `IMTDeal::Volume()` is the official Manager **classic 4-digit** integer. **1.00 lot = 10 000.** That is `MTAPI_VOLUME_DIV`, **not** hundredths (`100`) and **not** `VolumeExt` (`100 000 000`).

| Claim | Measured this slot |
|---|---|
| Official divisor for `IMTDeal::Volume()` | **`MTAPI_VOLUME_DIV = 10000.0`** (`MTAPI_VOLUME_DIGITS = 4`) |
| Official divisor for `IMTDeal::VolumeExt()` | **`MTAPI_VOLUME_EXT_DIV = 100000000.0`** (`MTAPI_VOLUME_EXT_DIGITS = 8`) |
| Official hundredths (`/100`) divisor | **Does not exist** in Manager SDK math |
| Product C++ deal extractor (YoPips + Prop `mt5-sdk\src`) | `d.volume = deal->Volume()` — **never** `VolumeExt()` |
| Product C# live reader | `CIMTDeal.Volume()` → `Mt5DealDto.VolumeNative` — **0** `VolumeExt()` calls under `D:\Prop\src` |
| Domain default | `VolumeConverter` ctor / `Manager` factory = **`10_000m`** |
| `HundredthsScale = 100` | Constant only. **Not default.** No factory. |
| `ExtendedVolumeScale = 100_000_000` | Opt-in factory only. **Unused** on the ingest wire. |
| `mt5_types.h` `// in hundredths of lots` | **Wrong comment.** Field is still filled from `Volume()`. |
| YoPips execution / display | `MT5_VOLUME_PER_LOT = 10000.0`; `lot_size = volume / 10000.0` |
| Copy to cTrader can place a live order today | **No.** `CTraderFixSession` writes only `(35, "A")`. Product `35=D` / `NewOrderSingle` builder = **0**. `RealCopyEnabled` pinned **false**. `CopyTradingService.NewOrderSingleImplemented = false`. Intents land **`SHADOW_ONLY`**. **`SAFE_BY_ABSENCE`.** |

**One-liner:** Treat every `IMTDeal::Volume()` / `CIMTDeal.Volume()` / `DealData.volume` / `Mt5DealDto.VolumeNative` integer as **lots × 10 000**. Dividing by 100 inflates lots **100×**. Dividing by 1e8 shrinks lots **10 000×**. Live cTrader send is still absent, so a scale bug cannot spend capital today.

Worked identity on the current wire:

```
lots   = VolumeNative / 10_000
native = Round(lots * 10_000, AwayFromZero)
```

| Lots | Classic `Volume()` | If treated as hundredths (÷100) | If treated as `VolumeExt` (÷1e8) |
|---:|---:|---:|---:|
| 1.00 | **10 000** | 100.00 lots (**100× too big**) | 0.0001 lots (**10 000× too small**) |
| 0.10 | **1 000** | 10.00 lots | 0.00001 lots |
| 0.01 | **100** | 1.00 lot | 0.000001 lots |
| 0.05 | **500** | 5.00 lots | 0.000005 lots |

Ratio of the two official scales: `MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10 000`. That is why `SMTMath::VolumeFromVolumeExt` divides by 10000 and `VolumeExtFromVolume` multiplies by 10000.

**Do not flip the C# constructor default to `100_000_000` (A81 §7.1) while extractors still copy `Volume()`.** Feeding classic `10 000` into an 1e8 default yields **0.0001 lots**. Reconstruction `FlatEpsilon` is `0.0000001m`, so that undersize is accepted as a real trade **10 000× too small**.

---

## 1. Official SDK pin — two getters, two scales

### 1.1 `IMTDeal` has two volume accessors (not interchangeable)

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`

```140:142:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume
   virtual uint64_t  Volume(void) const=0;
   virtual MTAPIRES  Volume(const uint64_t volume)=0;
```

```229:231:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume with extended accuracy
   virtual uint64_t  VolumeExt(void) const=0;
   virtual MTAPIRES  VolumeExt(const uint64_t volume)=0;
```

Header comments: `Volume` = “deal volume”; `VolumeExt` = “deal volume with extended accuracy”. Same pair exists for `VolumeClosed` / `VolumeClosedExt`. Positions use the same classic getter (`IMTPosition::Volume()` comment: “position volume”, `MT5APIPosition.h` L131–133).

### 1.2 Authoritative math macros (identical in both trees)

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
Copyright: MetaQuotes Ltd., 2000–2026.

Same first 20 lines at  
`D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Classes\MT5APIMath.h`.

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

Conversion implementations (same file, independently re-read):

| Function | Formula | Meaning |
|---|---|---|
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, MTAPI_VOLUME_DIGITS)` = 4 digits | lots → classic integer |
| `SMTMath::VolumeToDouble(vol)` | `vol / MTAPI_VOLUME_DIV` then 4-digit normalize | classic integer → lots |
| `SMTMath::VolumeExtToInt(lots)` | `PriceToIntPos(lots, 8)` | lots → ext integer |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / MTAPI_VOLUME_EXT_DIV` | ext integer → lots |
| `SMTMath::VolumeFromVolumeExt(ext)` | `ext / 10000` | 8-digit → 4-digit |
| `SMTMath::VolumeExtFromVolume(vol)` | `vol * 10000` | 4-digit → 8-digit |

```255:265:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeToInt(const double volume)
  {
   return(PriceToIntPos(volume,MTAPI_VOLUME_DIGITS));
  }
inline double SMTMath::VolumeToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_DIV),MTAPI_VOLUME_DIGITS));
  }
```

There is **no** `100` divisor and **no** “hundredths” string in this header.

### 1.3 Official examples bind `Volume()` to `/10000`, not `/100` or `/1e8`

Manager sample places **1.00 lot** via the 4-digit helper:

```186:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
```

`VolumeToInt(1.0)` = **10000**.

Official trades report divides **`deal->Volume()` by `10000.0`**:

```628:628:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

WebAPI helpers document the same split as **old 4-digit** vs **new 8-digit** (ratio 10 000), not hundredths:

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

PHP twin (`mt5_utils.php` L114–128): `ToOldVolume = /10000`, `ToNewVolume = *10000`.

**This product does not use the WebAPI.** It uses Manager `CIMTDeal.Volume()` / `IMTDeal::Volume()`, which already return the **old 4-digit** integer. Applying `ConvetToOldVolume` a second time would undersize **10 000×**. Lots from classic is `/10000` **once**; lots from ext is `/1e8`.

Official **report plugins** that call `deal->VolumeExt()` and `SMTMath::VolumeExtToDouble` are on the **8-digit** path (`TYPE_VOLUME_EXT` / `FIELD_*_VOLUME_EXT`). That does **not** change the meaning of `Volume()`.

---

## 2. What this product actually copies

### 2.1 C++ extractors copy `Volume()`, never `VolumeExt()`

YoPips Manager wrapper:

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

Same assignment in:

| File | Line | Statement |
|---|---:|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | 1517 | `d.volume = deal->Volume();` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | 787 | `d.volume = deal->Volume();` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | 1517 | `d.volume = deal->Volume();` |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | 855 | `d.volume = deal->Volume();` |

Positions: `d.volume = pos->Volume()` (YoPips `mt5_manager.cpp` L1495). Orders: `d.volume = order->VolumeInitial()` (L1534). Sends: `request->Volume(volume)` / `request->Volume(req.volume)` — classic setter.

Grep `VolumeExt` under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` = **0**.  
Grep `VolumeExt` under `D:\Prop\mt5-sdk\src` = **0**.  
Grep `VolumeExt` under `D:\Prop\src` = **1** (XML comment in `VolumeConverter.cs` only).

### 2.2 C# live connector copies `d.Volume()` into `VolumeNative`

`NativeMt5BrokerConnector.ReadDeals` is the only live deal materializer:

```408:424:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private static List<Mt5DealDto> ReadDeals(CIMTDealArray arr)
    {
        var rows = new List<Mt5DealDto>((int)arr.Total());
        for (uint i = 0; i < arr.Total(); i++)
        {
            var d = arr.Next(i);
            if (d is null)
                continue;
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

`CIMTDeal.Volume()` is the C# wrapper of `IMTDeal::Volume()`. The 8th `Mt5DealDto` argument is `ulong VolumeNative` (`Mt5Contracts.cs` L24–38). Positions use `p.Volume()` into `Mt5PositionDto.VolumeNative` (L396).

Callers of `ReadDeals`:

- `GetDealsCore` → `DealRequest(login, from, to, arr)` (per-login)
- `GetGroupDealsCore` → `DealRequestByGroup(group, from, to, arr)` (ALL deals in that group)

Ingest persist is a **passthrough** of that integer (`EfTradingStore` L103 and L457: `VolumeNative = deal.VolumeNative`). Reconstruction then does `lots = _volume.ToLots(deal.VolumeNative)` with default `VolumeConverter.Manager` (`TradeReconstructor.cs` L20, L89).

### 2.3 Domain converter default is 10 000 (tested)

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
    ...
    public VolumeConverter(decimal scale = ManagerVolumeScale)
    ...
    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
}
```

Unit tests (`D:\Prop\tests\Unit\VolumeConverterTests.cs`, 3 facts):

| Fact | Assertion |
|---|---|
| `Manager_scale_maps_0_10_lots_to_1000_native` | `Scale == 10_000`; `ToNative(0.10m) == 1000`; `ToLots(1000) == 0.10m` |
| `Extended_scale_maps_one_lot_to_100_million` | `ToNative(1m) == 100_000_000`; `ToLots(100_000_000) == 1m` |
| `Hundredths_comment_is_not_the_default` | `Manager.Scale != HundredthsScale` |

`TradeReconstructionTests` and `DealReasonTests` construct `new TradeReconstructor(VolumeConverter.Manager)`.

Skipped (not implemented) A38 converter fact still **states the same law**:

```83:87:D:\Prop\tests\Unit\Normalization\SourceDestinationQuantityConversionTests.cs
    [Fact(Skip = "A38: source ticks / 10_000 = lots. Converter not implemented.")]
    public void Mt5_ticks_scale_is_10000()
    {
        Assert.Fail("Converter must use VolumeConverter.Manager (1 lot = 10_000), never /100.");
    }
```

### 2.4 Fake / demo tape uses the same 10 000 scale

```72:74:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public const decimal VolumeScale = 10_000m;

    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);
```

Worked Fake fixtures (`ClosedRoundTrip` → `Lots(lots)`):

| Tape lots | Native `VolumeNative` |
|---:|---:|
| 0.10 (Achiever 10001 / 10002 first) | **1 000** |
| 0.20 | **2 000** |
| 0.40 | **4 000** |
| 0.05 (Starwave) | **500** |

If those integers were `/100`, 0.10 lots would reconstruct as **10 lots**. If `/1e8`, as **0.00001 lots**.

### 2.5 YoPips already divides by 10000 everywhere it displays or sends

| Site | Evidence |
|---|---|
| `TradeExecutionService::MT5_VOLUME_PER_LOT` | `= 10000.0` (`trade_execution_service.h` L240–244) |
| Comment on that constant | “1.00 lot == 10000 units” |
| Provisional deal cache | `ed.volume = c.mt5.volume; // MT5 native volume units (lot_size = volume/10000)` (cpp L1118) |
| Lots → units | `units = llround(c.lots * MT5_VOLUME_PER_LOT)` (cpp L1262) |
| Legacy native check | `exactLots = units / 10000.0L` (cpp L1425) |
| `TerminalStateService::MT5_VOLUME_PER_LOT` | `10000.0` (header L115) |
| `hft_detection_service.cpp` | `kVolumePerLot = 10000.0` |
| `worker_service.cpp` | `kMt5VolumePerLot = 10000.0`; JSON `"volume", deal.volume / 10000.0` |
| `trade_service.cpp` | `{"lot_size", deal.volume / 10000.0}` and same for positions |
| `export_controller.cpp` | `deal.volume / 10000.0` |
| `journal_controller.cpp` | `deal.volume / 10000.0` |
| `symbol_controller.cpp` | `volume_min/max/step / 10000.0`; position `lot_size = pos.volume / 10000.0` |

YoPips `mt5_types.h` L75 comment `// in hundredths of lots` is the **same wrong MT4 leftover** as Prop’s copy. The **code** that consumes `DealData.volume` always uses `/10000`.

---

## 3. Wrong comments (do not implement them)

| Source | Claim | Status |
|---|---|---|
| `D:\Prop\mt5-sdk\src\core\mt5_types.h` L75 | `uint64_t volume = 0;  // in hundredths of lots` | **Wrong.** MT4 `lots*100`. Field is filled from `pos->Volume()`. |
| `D:\Projects\YoPips\...\src\core\mt5_types.h` L75 | same | **Wrong.** Same extractor. |
| `D:\Prop\docs\trade-reconstruction.md` L31 | “Gold (XAUUSD) volume on MT5 is in hundredths of lots where 1 lot = 100 oz” | **Prose mix-up.** Contract size 100 oz/lot is real; **volume integer scale is not hundredths**. L31–35 then correctly say default `MT5_VOLUME_SCALE` **10000** and `50000 = 5.0 lots`. |
| `D:\Prop\docs\architecture.md` L23 | `Volume default scale = 10_000 (IMTDeal.Volume())` | **Correct.** Matches SDK + extractors. |
| A81 recommendation: ctor default `1e8` | “new accuracy is 8 digits” | **Wrong for this wire today.** Ext scale exists; extractors do not use it. |

`DealData.volume` / `OrderData.volume` have **no** unit comment. They ride the same classic integers.

---

## 4. Blast if the wrong scale is used on live Achiever/Starwave deals

Ingest stores **native** `ulong`. Scoring / first-3 / shadow qty all go through `ToLots`.

Assume a typical 0.10-lot XAU deal (`Volume() = 1000`):

| Divisor used | Lots seen by reconstructor | Effect |
|---|---:|---|
| **10 000** (correct) | **0.10** | Matches Manager terminal |
| 100 (hundredths comment) | **10.00** | **100× oversize** — if a send path ever existed, this is a ruinous ticket |
| 100 000 000 (`VolumeExt`) | **0.00001** | **10 000× undersize** — silent; still `> FlatEpsilon` |

A81’s recommended default on a 1.00-lot classic integer (`10000`): `10000 / 1e8 = 0.0001` lots. Not a loud fail.

**Today this cannot spend dest capital** because there is no `35=D` writer. It **can** corrupt reconstructed lots, scores, and SHADOW qty if someone later wires those lots into a send path without changing extractors.

Contract size (XAU 100 oz/lot) is **not** a third volume scale. It is the lots → ounces step (`A43` / `QuantityNormalizer` last-stage only; G7 still FAIL; live tag 38 absent).

---

## 5. ALL Achiever + Starwave groups / traders (fetch path; scale applies to every deal)

This slot did **not** re-attach to Manager. Prior measured census (`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`, 2026-08-18):

| Broker | Connect | Groups | Traders | Path |
|---|---|---:|---:|---|
| ACHIEVER | HTTP proxy | **8** | **6512** | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | direct | **10** | **1948** | same |
| **Total** | | **18** | **8460** | |

Current product walk (re-read this slot):

- Groups: `GroupRequestArray("*")` then fallback `GroupTotal`/`GroupNext` (`NativeMt5BrokerConnector.cs` L144–185). Mask is `*`, **not** a plan-group filter.
- Accounts: `GetAccountsAsync(null)` walks **every** group name (`L189–213`) via `UserRequestArray` then `UserGetByGroup` / `UserLogins` (`L216–232`).
- Deals: `DealIngestionService.SyncBrokerAsync` uses `IMt5BulkDealReader.GetGroupDealsAsync` **per catalog group** (L65–71) — ALL groups, not first-N, no `Take(200)` on ingest.
- Hosted ingest: `LiveIngestHostedService` loops `registry.All()` (Achiever + Starwave when both passwords pass `HasRealPasswords`).

Every deal those walks return is `d.Volume()` / scale **10 000**. Volume scale is independent of the census count.

---

## 6. Copy to cTrader must not send live orders (no loss)

Measured refuse surfaces (this slot):

| Surface | What it does |
|---|---|
| `CTraderFixSession.BuildLogon` | **Only** wire write is `(35, "A")` plus logon tags. File is 135 lines. **0** `35=D`, **0** `NewOrderSingle`, **0** `OrderQty` / `38=` |
| Product `src/` + `apps/` grep `35=D` / `(35, "D")` / `MsgType = "D"` | **0** builders (mentions are comments / log strings / UI copy) |
| `DependencyInjection` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” |
| `CTraderFixLogonHostedService` L68 | `_runtime.RealCopyEnabled = false` **after** quote/trade logon |
| `CTraderFixOptions.RealCopyExecutionEnabled` | compile-time default **`false`** |
| `apps/fix-worker/Worker.cs` | `GetValue("CTrader:RealCopyExecutionEnabled", false)`; even if true, **no send**, only a warning |
| `CopyTradingService` | `NewOrderSingleImplemented = false` (const); `VenueReconciled = false`; live branch writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; else **`SHADOW_ONLY`**; `AllowFixSend = false` on risk rows |
| `EfTradingStore.PersistDemoShadowAsync` | intents created with `Status = "SHADOW_ONLY"` |
| `LiveRuntimeStatus.Snapshot` | copy note: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |

Honesty: `CopyTradingService.GetStatusAsync` returns `FeatureCopyEnabled: true` as a **status DTO literal**. That is **not** a FIX send gate. `NewOrderSingleImplemented` remains **false**. Flipping `REAL_COPY_EXECUTION_ENABLED=true` still cannot emit `35=D` (`SAFE_BY_ABSENCE`).

`QuantityNormalizer` is unused on the wire (last-stage dest grid only; `0.10 → 0.10` passthrough). It does not emit tag 38.

---

## 7. What this slot did **not** measure

- No live `DealRequestByGroup` dump of Achiever/Starwave `Volume()` vs `VolumeExt()` pair on the same ticket.
- No re-run of `VolumeConverterTests` (source assertions read; compile/test not executed here).
- No SHA-256 re-hash (no shell this slot).
- Official report plugins that bind `VolumeExt()` were **not** treated as this product’s ingest path.

Those gaps do **not** weaken the SDK + extractor pin: the integers this tree copies are `Volume()`, and MetaQuotes defines that getter as `/10000`.

---

## 8. Binding recommendation (slot 106)

1. **Keep** `VolumeConverter` default / `Manager` = **10 000**.
2. **Keep** persisting `VolumeNative` as the classic integer. Convert at the algorithm boundary only.
3. **Never** divide `IMTDeal::Volume()` by 100 or by 1e8.
4. Use `VolumeConverter.Extended` **only** if a future reader switches to `VolumeExt()` / `FIELD_*_VOLUME_EXT` and records which scale was stored.
5. Treat `mt5_types.h` “hundredths” and `trade-reconstruction.md` L31 “hundredths of lots” as **documentation bugs**, not as a third API mode.
6. Do **not** enable live cTrader send. Fetch-all catalog may proceed; `35=D` must stay absent.

---

## 9. Slot-106 verdict line

```
CONFIRMED — IMTDeal.Volume scale = 10000
(not hundredths/100, not VolumeExt/1e8).
Extractors copy Volume(). Domain default 10_000.
ALL-groups ingest uses that integer.
Live cTrader NewOrderSingle absent (SAFE_BY_ABSENCE).
Risk to capital: NONE.
```

*End of W500_RESEARCH_106. Product source was not modified. No secrets printed. This slot did not live-attach and did not send orders.*
