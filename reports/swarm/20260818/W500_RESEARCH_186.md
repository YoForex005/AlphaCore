# W500_RESEARCH_186 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_186.md` |
| Slot | **186** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 186 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Flag names and booleans only. No manager / proxy / FIX passwords. |
| Method | Independent re-read of official Manager SDK math + `IMTDeal`, Prop + YoPips extractors, C# native reader + `VolumeConverter` + reconstructor + fixtures, FIX send path, copy gates. Cross-checked E004 VolumeConverter TRX (`2026-08-18T13:48:20+05:30`), D92 compiled Domain eval (`stdout.txt` `ctor_default_Scale=10000`), and `LIVE_GROUPS_AND_TRADERS.json` (`2026-08-18T08:42:16.8519545Z`). This slot did **not** live-attach a manager and did **not** send FIX. |
| Related (not rubber-stamped) | Slots 6/26/46/66/86/106/126/146, A38, A81, B14, D14, D92, A006. Volume-scale conclusion independently re-confirmed on current trees. Slot 66/108 “DI pins `RealCopyEnabled=false`” and `CREDENTIALS_AND_COPY_STATUS.md` “forced false” are **stale**. |

---

## 0. Verdict (binding)

**CONFIRMED.** The integers this product copies from `IMTDeal::Volume()` are classic Manager volume:

```text
lots = IMTDeal::Volume() / 10000
1.00 lot  ==  10000   (MTAPI_VOLUME_DIV, 4 digits)
0.10 lot  ==   1000
0.01 lot  ==    100
```

| Claim | Result |
|---|---|
| `IMTDeal::Volume()` scale is **10000** | **YES** — official `MTAPI_VOLUME_DIV` / `SMTMath::VolumeToDouble` / `VolumeToInt(1.0)` |
| Scale is hundredths (`/ 100`) | **NO** — MT4 convention. Wrong comment in `mt5_types.h:75` (both trees). Not an official MT5 Manager scale. |
| Scale is `VolumeExt` (`/ 1e8`) | **NO** — that is `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` / 8-digit WebAPI only |
| Product extractors call `VolumeExt()` | **NO** — 0 calls under `D:\Prop\mt5-sdk\src`, 0 under `D:\Prop\src` (except one XML comment), 0 under `D:\Prop\apps`, 0 under YoPips `src` |
| C# default converter matches the wire | **YES** — `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor binds `VolumeConverter.Manager` |
| Unit tests pin 10000 | **YES** — E004 `VolumeConverterTests` 3/3 **Passed**; reconstruction fixtures treat `VolumeNative=1000` as **0.10** lots |
| Live copy send uses this scale today | **N/A** — copy hop has **no** `NewOrderSingle` sender. Capital at risk from copy = **NONE** (`SAFE_BY_ABSENCE`) |

Do **not** flip the default to `100` or `100_000_000` while extractors still copy `deal->Volume()` / `d.Volume()`. That is a silent **100×** or **10_000×** sizing bug the moment live copy is armed.

A81 documented both official scales, then **wrongly** recommended ctor default `1e8`. B14 / D14 / D92 / slots 66–146 keep **10 000**. This slot independently re-read the same sources and **agrees with B14/D92**.

**Flag drift vs older slots (honesty, not a volume-scale change):** `DependencyInjection.cs:41` **binds** `REAL_COPY_EXECUTION_ENABLED` from configuration. Lab `.env` L73 is `true`. `CTraderFixLogonHostedService` **no longer** overwrites the flag to false. This still does **not** send copy orders: `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`. Hosted FIX session emits **only** `35=A` Logon.

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
Same split in YoPips `MetaTrader5SDK\Include\Bases\MT5APIDeal.h`.

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

`Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp:628` (both trees):

```628:628:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

Same classic path: `AgentsDetailed.cpp:460` (`SMTMath::VolumeToDouble(m_deal->Volume())`). Official **Capital / Transaction** reports that use `VolumeExt()` pair it with `SMTMath::VolumeExtToDouble` / `FormatVolumeExt`. That is the ext dataset column, **not** `IMTDeal::Volume()`. Using `/1e8` on `Volume()` is the A81 blast (`1.00 lot` integer `10000` → `0.0001` lots).

Official WebAPI utils name the conversion explicitly — “new 8 digits volume” ↔ “old 4 digits volume” via `×/÷ 10000`:

```114:129:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\PHP\mt5_api\mt5_utils.php
  public static function ToOldVolume($new_volume)
    {
     return (int)$new_volume / 10000;
    }
  public static function ToNewVolume($old_volume)
    {
     return (int)$old_volume * 10000;
    }
```

Two official scales. Hundredths is not among them.

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

YoPips pool extractor is identical except line number (`mt5_pool.cpp:787`). Positions copy `pos->Volume()`. Orders copy `order->VolumeInitial()`. Product C++ send path (YoPips) writes classic `request->Volume(...)`, **not** `VolumeExt()`.

`grep VolumeExt` under `D:\Prop\mt5-sdk\src` and YoPips `src`: **0 hits**.

### 2.2 YoPips production already converts those integers by **10000**

Independent corroboration that the same extractors are classic 4-digit units:

| File | Evidence |
|---|---|
| `trade_execution_service.h:244` | `static constexpr double MT5_VOLUME_PER_LOT = 10000.0;` plus comment `1.00 lot == 10000 units` |
| `trade_execution_service.cpp:753 / 1425` | `volume / MT5_VOLUME_PER_LOT` and `units / 10000.0L` |
| `terminal_state_service.h:115` | same `MT5_VOLUME_PER_LOT = 10000.0` |
| `worker_service.cpp:547 / 650` | `pos.volume / 10000.0`, `deal.volume / 10000.0` |
| `trade_service.cpp:92 / 117` | `lot_size = *.volume / 10000.0` |
| `symbol_controller.cpp` | `volumeMin/Max/Step / 10000.0` |
| `export_controller.cpp:146` | `deal.volume / 10000.0` |
| `journal_controller.cpp:192` | `deal.volume / 10000.0` |

YoPips never divides those extracted integers by `100` or `1e8`.

### 2.3 C# live connector copies `d.Volume()` into `VolumeNative`

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadDeals` (`CIMTDeal` managed wrapper of `IMTDeal`):

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

`Mt5DealDto.VolumeNative` is that raw `ulong` (`Mt5Contracts.cs:32`). Persist copies it unchanged (`EfTradingStore.cs:103`). Reconstruction converts **once**: `_volume.ToLots(deal.VolumeNative)` with default `VolumeConverter.Manager` (`TradeReconstructor.cs:20,89`).

`grep VolumeExt` under `D:\Prop\src`: **1 hit**, the XML comment in `VolumeConverter.cs:6`. Zero product calls.

### 2.4 C# converter + measured eval

```12:18:D:\Prop\src\Domain\Volume\VolumeConverter.cs
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;
    public decimal Scale { get; }
    public VolumeConverter(decimal scale = ManagerVolumeScale)
```

Compiled Domain eval (`D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt`):

```text
ctor_default_Scale=10000
Manager.Scale=10000
Extended.Scale=100000000
HundredthsScale=100
default_eq_Manager=True
default_eq_Extended=False
Manager.ToLots(10000)=1
Manager.ToLots(1000)=0.1
Manager.ToLots(100)=0.01
Extended.ToLots(10000)=0.0001
default.ToNative(1)=10000
ratio_ext_div_mgr=10000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

E004 unit TRX (`2026-08-18T13:48:20.545+05:30`), 3/3 **Passed**:

| Test | Pin |
|---|---|
| `Manager_scale_maps_0_10_lots_to_1000_native` | `Scale==10000`; `0.10 lot ↔ 1000` |
| `Extended_scale_maps_one_lot_to_100_million` | ext exists, **not** default |
| `Hundredths_comment_is_not_the_default` | `Manager.Scale != 100` |

Reconstruction fixtures (`TradeReconstructionTests.cs`) pass `VolumeNative=1000` and assert `InitialVolumeLots == 0.10m` / `MaxVolumeLots == 0.20m`. Fake demo factory uses the same scale (`FakeMt5BrokerConnector.cs:72` `VolumeScale = 10_000m`). Architecture pin: `docs/architecture.md:23` `Volume default scale = 10_000 (IMTDeal.Volume())`.

### 2.5 The “hundredths” comment is a naming bug, not a second scale

| Location | Text | Status |
|---|---|---|
| `D:\Prop\mt5-sdk\src\core\mt5_types.h:75` | `uint64_t volume = 0;  // in hundredths of lots` | **Wrong.** Extractor copies classic `Volume()`. |
| YoPips `src\core\mt5_types.h:75` | same comment | **Wrong.** Same extractor. |
| YoPips `mt5_types.h:144` | “Volume is in MT5 native integer units (see `MT5_VOLUME_PER_LOT`)” | **Correct pointer** to 10000. |
| `docs/risk.md:65–68` | “hundredths … (50000 = 5.0 lots)” **and** `MT5_VOLUME_SCALE=10000` | Name is wrong; the **example** is 10000-scale (`50000/10000=5`). |
| `docs/trade-reconstruction.md:31` | “hundredths … default 10000” | Mixed prose. Default divisor is 10000. |

If the comment were true, `50000` would be **500.00** lots. The written example (`50000 = 5.0`) already assumes **10000**.

---

## 3. ALL Achiever + Starwave groups / traders (fetch path + census)

### 3.1 Product fetch is flag-blind ALL

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector`s: Achiever (optional HTTP proxy) + Starwave (`ProxyEnabled=false`). DI refuses to start without both real passwords (`HasRealPasswords`).

| Step | API | Scope |
|---|---|---|
| Groups | `GroupRequestArray("*")` then fallback `GroupTotal`/`GroupNext` | **ALL** groups |
| Accounts | `UserRequestArray(gname)` then `UserGetByGroup` / `UserLogins`+`UserRequestByLogins` for every group | **ALL** users |
| Deals | `DealRequestByGroup` per group (bulk) or `DealRequest` per login | **ALL** traders in catalog |
| Positions | `GetGroupPositionsAsync("*")` when mask empty | **ALL** |

`DealIngestionService.SyncCatalogAsync` calls `GetGroupsAsync` + `GetAccountsAsync(null)` (null = every group). Copy / REAL_COPY flags do **not** filter this catalog.

### 3.2 Live census independently re-summed (not re-attached)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at `2026-08-18T08:42:16.8519545+00:00`. This slot **did not** re-connect.

**Achiever** (`connected=true`, 8 groups, header `accounts=6512`, `openPositions=1506`):

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
| **sum** | **6512** |

**StarwaveFX** (`connected=true`, 10 groups, header `accounts=1948`, `openPositions=478`):

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
| **sum** | **1948** |

**Total: 18 groups / 8460 traders / 1984 open positions.** Header counts match the per-group sums. Dummy logins `10001`/`10002` are not in this dump.

---

## 4. Copy to cTrader must not send live orders (no loss)

| Gate | Measured state |
|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | `const false` (`CopyTradingService.cs:16`) |
| `CopyTradingService.VenueReconciled` | `const false` (L15) |
| Persist `AllowFixSend` | **forced `false`** even after `RiskEngine.Evaluate` (L192) |
| Live-send branch | Requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — unreachable (L198) |
| Intents | Status `SHADOW_ONLY`; `CopyTradingHostedService` logs “Live NewOrderSingle still blocked” |
| `CTraderFixSession.BuildLogon` | only outbound MsgType `(35, "A")` (L96). One `WriteAsync`. Sockets disposed. |
| Copy hop `35=D` | **Absent** (`SAFE_BY_ABSENCE`) |
| `QuantityNormalizer` | Last-stage dest grid (`lots × 0.05`). Never writes FIX tag 38. |
| Trade #3 | EARLY_SCORE / SHADOW only — never auto LIVE (independent slots 129/149) |

**Residual (honesty):** `CTraderFixDemoTestTrade` can emit demo-gated `Build("D", …)` with hardcoded qty. Caller is `tools/DemoFixTestTrade` only. Gate refuses live host / live SenderCompID / live account `1369850`. **Not** the copy book. **Not** fed by `IMTDeal.Volume()`. This slot did **not** invoke it.

**Residual flag:** lab `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now DI-bound (`DependencyInjection.cs:41`). `CTraderFixLogonHostedService` no longer re-pins false. Architecture / `docs/architecture.md` still require the flag stay **false** until §68 19/19 + §70 14/14. `CREDENTIALS_AND_COPY_STATUS.md` “forced false” is **stale**. Capital is still safe because the sender is missing.

`docs/architecture.md:28`: “Live TRADE send and ML are explicitly not enabled.”

---

## 5. Blast radius if the scale is flipped later

Assume a live 1.00 lot deal (`Volume() == 10000`) and a future sender that uses `VolumeConverter` default:

| Wrong default | Lots computed | Error vs truth |
|---|---:|---|
| Hundredths `/100` | 100.00 | **100× oversize** |
| Ext `/1e8` (A81 ctor) | 0.0001 | **10 000× undersize** |
| Manager `/10000` (this product) | 1.00 | **correct** |

Do not change `ManagerVolumeScale` or the extractor getter without a paired convert. Fetch ALL 8460 traders with the wrong scale would poison reconstruction, scoring, and (if armed) destination size.

---

## 6. What this slot did **not** do

- Did not attach Achiever or Starwave managers.
- Did not send FIX `35=D` / NewOrderSingle / flatten.
- Did not modify product or test source.
- Did not print secrets.
- Did not re-run `dotnet test` (E004 TRX + D92 eval already on disk from this calendar day; line-level source re-read matches those artifacts).

---

## 7. Slot 186 conclusion

**`IMTDeal.Volume` scale is 10000.** Not hundredths. Not `VolumeExt` 1e8. Product copies classic `Volume()`. Converter default is 10 000. Tests and compiled eval agree. ALL-groups / ALL-traders fetch path is in place; last measured census is **18 / 8460**. Copy to cTrader cannot lose capital today: **no live NewOrderSingle on the copy hop**.

**Verdict: CONFIRMED. Risk to capital: NONE.**
