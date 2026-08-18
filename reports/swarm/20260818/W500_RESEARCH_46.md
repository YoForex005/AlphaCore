# W500_RESEARCH_46 — Confirm `IMTDeal.Volume()` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_46.md` |
| Slot | **46** |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No manager / proxy / FIX passwords. |
| Assigned | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`). Goal: fetch **ALL** Achiever+Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Method | Independent re-read this pass of official Manager SDK math + `IMTDeal` getters, Prop C++/C# extractors, C# `VolumeConverter` + reconstructor + unit TRX, YoPips PropFirm extractors + `/10000.0` lot converters, cTrader FIX send path. No live `35=D`. |

Same topic as W500_26; this slot re-opens the **primary sources** and does not treat that report as authority.

---

## 0. Verdict (binding)

**CONFIRMED.** Integers copied from `IMTDeal::Volume()` are classic Manager volume:

```text
lots = IMTDeal::Volume() / 10000
1.00 lot  ==  10000   (MTAPI_VOLUME_DIV, 4 digits)
0.10 lot  ==   1000
0.01 lot  ==    100   ← this integer is 0.01 lot, not 1.00 lot
```

| Claim | Result |
|---|---|
| `IMTDeal::Volume()` scale is **10000** | **YES** — `MTAPI_VOLUME_DIV` / `SMTMath::VolumeToDouble` |
| Scale is hundredths (`/ 100`) | **NO** — MT4 convention. Wrong comment on `PositionData.volume`. Not an official MT5 Manager scale. |
| Scale is `VolumeExt` (`/ 1e8`) | **NO** — that is `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` only |
| Product extractors call `VolumeExt()` | **NO** — 0 runtime hits under `D:\Prop\mt5-sdk\src`, `D:\Prop\src`, and `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` |
| C# default converter matches the wire | **YES** — `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor binds `VolumeConverter.Manager` |
| Live cTrader send uses this scale today | **N/A** — there is **no** `35=D` / `NewOrderSingle` sender. Capital at risk from copy = **none** |

Do **not** flip the default to `100` or `100_000_000` while extractors still copy `deal->Volume()` / `d.Volume()`. That is a silent **100×** or **10 000×** sizing bug the moment live copy is armed.

---

## 1. Official Manager SDK (authoritative)

### 1.1 Two official scales. Not three.

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
Copyright: MetaQuotes Ltd., 2000–2026.

Same constants exist at  
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

There is **no** `#define` for `100` and no “hundredths” string in this header. Grep this pass: `hundredths` is absent from `MT5APIMath.h`.

| Helper | Formula | 1.00 lot integer |
|---|---|---:|
| `SMTMath::VolumeToDouble(vol)` | `vol / MTAPI_VOLUME_DIV` (4 digits) | **10 000** |
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, 4)` | **10 000** |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / MTAPI_VOLUME_EXT_DIV` (8 digits) | 100 000 000 |
| `SMTMath::VolumeFromVolumeExt(ext)` | `ext / 10000` | 8-digit → 4-digit |
| `SMTMath::VolumeExtFromVolume(vol)` | `vol * 10000` | 4-digit → 8-digit |

```255:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeToInt(const double volume)
  {
   return(PriceToIntPos(volume,MTAPI_VOLUME_DIGITS));
  }
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

The **10000** inside the ext converters is the **ratio** `MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10000`, not a third scale. Classic `Volume()` already *is* 4-digit. Extended is classic × 10000.

Formatters match the pair: `SMTFormat::FormatVolume` → `VolumeToDouble`; `FormatVolumeExt` → `VolumeExtToDouble` (`MT5APIFormat.h` 417–427).

### 1.2 `IMTDeal` has two getters. Comments name them.

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`

```140:142:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume
   virtual uint64_t  Volume(void) const=0;
   virtual MTAPIRES  Volume(const uint64_t volume)=0;
```

```229:234:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h
   //--- deal volume with extended accuracy
   virtual uint64_t  VolumeExt(void) const=0;
   virtual MTAPIRES  VolumeExt(const uint64_t volume)=0;
   //--- closed volume with extended accuracy
   virtual uint64_t  VolumeClosedExt(void) const=0;
```

| Method | Line | Unit |
|---|---|---|
| `Volume()` | 141 | classic 4-digit (`/ 10000` lots) |
| `VolumeClosed()` | 186 | same classic family |
| `VolumeExt()` | 230 | extended 8-digit (`/ 1e8` lots) |
| `VolumeClosedExt()` | 233 | same ext family |

Same dual API exists on `IMTPosition`, `IMTOrder`, `IMTRequest`. The comment on `VolumeExt` is “with extended accuracy”, never “hundredths”.

### 1.3 Official MetaQuotes samples bind `Volume()` to `/10000` or `VolumeToDouble`

| Sample | Expression | Meaning |
|---|---|---|
| `Examples\Manager\SimpleManager\SimpleManager.cpp:190` | `request->Volume(SMTMath::VolumeToInt(1.0))` | 1.00 lot → **10000** on the classic setter |
| `Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp:628` | `deal->Volume()/10000.0` | lots from classic getter |
| `Examples\Report\Trades.Standard.Reports\Reports\AgentsDetailed.cpp:460` | `SMTMath::VolumeToDouble(m_deal->Volume())` | same |
| `Examples\Server\FeedCommission\PluginInstance.cpp:458` | `SMTMath::VolumeToDouble(deal->Volume())` | same |
| `Examples\Report\Gateways.Standard.Reports\Tools\GatewayUtils.h:92` | `VolumeToDouble(deal->Volume())` | same |

WebAPI sample names the two scales “old 4 digits” vs “new 8 digits”:

```348:364:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs
    /// From new volume to old volume
    /// <param name="new_volume">New 8 digits volume</param>
    /// <returns>Old 4 digits volume</returns>
    public static ulong ConvetToOldVolume(ulong new_volume)
      {
       return(new_volume/10000);
      }
    /// From old volume to new volume
    public static ulong ConvertToNewVolume(ulong new_volume)
      {
       return(new_volume*10000);
      }
```

`MTDeal.Volume` in that WebAPI sample **stores ext** and exposes classic via `/10000`:

```185:197:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs
    public ulong Volume
      {
       get { return MTUtils.ConvetToOldVolume(m_Volume);  }
       set { m_Volume = MTUtils.ConvertToNewVolume(value); }
      }
    /// deal volume with exta 8-digits accuracy
    public ulong VolumeExt
     {
      get { return m_Volume;  }
      set { m_Volume = value; }
     }
```

**This product does not use the WebAPI.** It uses Manager `CIMTDeal.Volume()` / `IMTDeal::Volume()`, which already return the **old 4-digit** integer. Applying `ConvetToOldVolume` a second time would be a 10 000× undersize. Lots from classic is still `/10000` once; lots from ext is `/1e8`.

---

## 2. What this product actually copies

### 2.1 C++ Manager extractors (Prop `mt5-sdk`)

Both session and manager copy **`deal->Volume()` unchanged**. They never call `VolumeExt()`.

`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (`MT5Manager::extractDeal`):

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

`D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (`MT5Session::extractDeal`):

```846:855:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
DealData MT5Session::extractDeal(const IMTDeal* deal) {
    ...
    d.volume = deal->Volume();
```

Positions: `d.volume = pos->Volume()` (manager 1495, pool 833). Orders: `o->VolumeInitial()`, not `VolumeInitialExt()`.

Grep `VolumeExt(` under `D:\Prop\mt5-sdk\src` this pass: **0 hits**.

C++ HTTP-pool fixture locks the same unit: `request.volume = 10000` at `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp:94` — **1.00 lot** on the 4-digit scale, matching `VolumeToInt(1.0)`. That is not hundredths (`100`) and not ext (`100000000`).

### 2.2 C# native connector (live ingest path)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadPositions` / `ReadDeals`:

```396:424:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                p.Volume(),
                ...
                d.Volume(),
```

DTO field is `VolumeNative` (`D:\Prop\src\Application\Contracts\Mt5Contracts.cs:32,45`). Persistence stores the same `ulong` (`Mt5Deal.VolumeNative`, `D:\Prop\src\Domain\Entities\Mt5Deal.cs:16`). No conversion at ingest. Conversion to lots happens later in `TradeReconstructor` via `VolumeConverter`.

Grep `VolumeExt` under `D:\Prop\src` this pass: **1 hit**, the XML comment in `VolumeConverter.cs`. **Zero** runtime calls.

### 2.3 C# default scale = 10000

`D:\Prop\src\Domain\Volume\VolumeConverter.cs`:

```4:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
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

- `HundredthsScale` has **no factory** and is **not** the constructor default.
- `TradeReconstructor` hard-binds `_volume = volume ?? VolumeConverter.Manager` (`TradeReconstructor.cs:20`).
- Lots: `var lots = _volume.ToLots(deal.VolumeNative)` (line 89) → `native / 10000`.
- `QuantityNormalizer.Normalize` takes **already-converted** `sourceLots` (`QuantityNormalizer.cs:11`). A wrong ingest scale would be multiplied, not rescued, if live copy were armed.

`DemoBrokerFactory.VolumeScale = 10_000m`; `Lots(0.10m)` → native **1000** (`FakeMt5BrokerConnector.cs:72-74`).

Architecture pin: `D:\Prop\docs\architecture.md:23` — “Volume default scale = 10_000 (`IMTDeal.Volume()`)”.

### 2.4 Measured unit tests (TRX on disk)

Source assertions (`D:\Prop\tests\Unit\VolumeConverterTests.cs`):

| Test | Assertion |
|---|---|
| Manager scale | `Scale == 10_000`; `ToNative(0.10m) == 1000`; `ToLots(1000) == 0.10m` |
| Extended scale | `ToNative(1m) == 100_000_000` (opt-in only) |
| Hundredths is not default | `Manager.Scale != HundredthsScale` |

`TradeReconstructionTests.Reconstructs_simple_round_trip` feeds `VolumeNative = 1000` and asserts `InitialVolumeLots == 0.10m`. That only holds if the divisor is **10000**, not 100 (would be 10.00 lots) and not 1e8 (would be 0.00001 lots).

On-disk TRX from this calendar day, not re-executed in this slot (no shell in this agent):

`D:\Prop\reports\swarm\20260818\_tmp_e004\unit.trx` startTime `2026-08-18T13:48:20+05:30`:

| Test | Outcome |
|---|---|
| `VolumeConverterTests.Manager_scale_maps_0_10_lots_to_1000_native` | **Passed** |
| `VolumeConverterTests.Extended_scale_maps_one_lot_to_100_million` | **Passed** |
| `VolumeConverterTests.Hundredths_comment_is_not_the_default` | **Passed** |
| `TradeReconstructionTests.Reconstructs_simple_round_trip` | **Passed** |

---

## 3. The “hundredths” claim is a comment bug (MT4), not a third API

### 3.1 Wrong comment on the C++ DTO

`D:\Prop\mt5-sdk\src\core\mt5_types.h:75` (identical at YoPips `src\core\mt5_types.h:75`):

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    ...
    uint64_t volume = 0;  // in hundredths of lots
```

“Hundredths of lots” means `lots * 100`. That is the **MT4** `MODE_LOTSTEP` convention. It is **not** defined in `MT5APIMath.h`. The field is filled from `pos->Volume()` (classic 4-digit). The comment is **false**.

`DealData.volume` in the same file has **no** unit comment but is filled from the same `deal->Volume()`. `MT5TradeRequest.volume` says “MT5 native integer units” and points at `MT5_VOLUME_PER_LOT` in `trade_execution_service.cpp` — that constant is **not** in `mt5_types.h`; YoPips production uses `/ 10000.0L` (see §4.2).

### 3.2 Docs mix contract size / hundredths wording with the number 10000

`D:\Prop\docs\trade-reconstruction.md:31-37` says gold volume is “in hundredths of lots where 1 lot = 100 oz” **and** then `display_lots = native_volume / MT5_VOLUME_SCALE` with default **10000**. The 100 oz figure is **contract size**, not the Manager integer. The worked example (`50000` native = 5.0 lots = 500 oz) **only** works with divisor **10000**.

`D:\Prop\docs\risk.md:65-68` contradicts itself in four lines:

- “MT5: volume in hundredths of lots (**50000 = 5.0 lots**)”
- “`MT5_VOLUME_SCALE=10000` converts MT5 native → lots”

`50000 / 100 = 500` lots. `50000 / 10000 = 5.0` lots. The **numeric example is the 10000 scale**. The word “hundredths” is leftover MT4 language. `architecture.md:23` is the correct one-liner.

### 3.3 Blast radius if someone believes the wrong divisor

Assume a real deal `Volume() = 10000` (1.00 lot):

| Divisor used | Lots computed | Error vs truth |
|---|---:|---|
| **10000** (correct `Volume()`) | **1.00** | 1× |
| **100** (hundredths / MT4) | **100.00** | **100× oversize** |
| **100_000_000** (`VolumeExt`) | **0.0001** | **10 000× undersize** |

Reconstruction `FlatEpsilon` is `0.0000001m` (`TradeReconstructor.cs:16`), so `0.0001` lots is accepted as a real trade — a silent undersize, not a crash. Hundredths would score and (if live copy were armed) send **100×** size.

A81 correctly documented both official scales and the comment bug, then **wrongly** recommended ctor default `1e8`. B14 / D14 / D92 voted **keep 10 000** while extractors copy `Volume()`. This slot independently re-reads the SDK and agrees with **B14/D92**.

---

## 4. YoPips PropFirm backend (same Manager integers)

YoPips is the production Manager consumer this lab was extracted from. Same extractors, same SDK.

### 4.1 Extractors copy `Volume()`, never `VolumeExt()`

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:1517`  
`d.volume = deal->Volume();`

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp:787`  
`d.volume = deal->Volume();`

Sends write `request->Volume(...)` (classic setter), not `VolumeExt`.

Grep `VolumeExt` under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` this pass: **0 hits**.

### 4.2 Production lot conversion is `/ 10000.0`

Measured live-data converters (not percent-of-balance `/100.0`):

| File | Expression | Meaning |
|---|---|---|
| `src\services\trade_service.cpp:92` | `deal.volume / 10000.0` | deal `lot_size` JSON |
| `src\services\trade_service.cpp:117` | `pos.volume / 10000.0` | position `lot_size` JSON |
| `src\http\controllers\journal_controller.cpp:192` | `deal.volume / 10000.0` | journal lots |
| `src\http\controllers\export_controller.cpp:146` | `deal.volume / 10000.0` | CSV lots |
| `src\http\controllers\symbol_controller.cpp:21-23, 288` | `volume_min/max/step / 10000.0`, `pos.volume / 10000.0` | symbol + position lots |
| `src\services\worker_service.cpp:547, 650` | `pos.volume / 10000.0`, `deal.volume / 10000.0` | worker JSON |
| `src\services\trade_execution_service.cpp:1425-1442` | `units / 10000.0L` then `* 10000` must equal `units` | **legacy native volume → lots contract** |

`TradeExecutionService::checkLegacyNativeVolume` is an explicit measured contract: native units are **exactly** representable as `lots * 10000` with no remainder. That function would reject a `VolumeExt` integer (`100000000` for 1.00 lot) as not a clean 4-digit round-trip only if someone treated it as classic — but more importantly, production **never reads** `VolumeExt()`.

### 4.3 One display-only `/100.0` leftover (do not copy)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\http\controllers\admin\admin_dashboard_controller.cpp:996`:

```996:996:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\http\controllers\admin\admin_dashboard_controller.cpp
                double lots = static_cast<double>(pos.volume) / 100.0;
```

This is **admin exposure display**, not the execution path. It treats classic `Volume()` as hundredths and would overstate lots **100×** on that dashboard. It is **evidence of the comment bug**, not evidence that Manager volume is hundredths. Prop C# must not inherit this divisor.

---

## 5. Goal context — fetch ALL groups / ALL traders; do not send live

### 5.1 Both brokers, all groups, all manager users

Live DI (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs:20-49`) registers **both** `ACHIEVER` and `STARWAVEFX` native connectors. Dummy/fake is refused if real passwords are missing (`DependencyInjection.cs:35-36`).

`GetGroupsCore` requests `GroupRequestArray("*")`, then falls back to `GroupTotal`/`GroupNext` — **all** groups the manager can see, not a plan-name filter (`NativeMt5BrokerConnector.cs:144-186`).

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` and unions users by login (`189-213`). `DealIngestionService.SyncCatalogAsync` calls exactly that pair (`GetGroupsAsync` + `GetAccountsAsync(null)`). `SyncBrokerAsync` then pulls deals per group via `DealRequestByGroup` (`NativeMt5BrokerConnector.cs:307`) and scores **every** login from `ListLoginsAsync`.

`LiveIngestHostedService` runs that for **each** registered connector (`foreach (var connector in connectors)`), then rebuilds stored logins. Volume scale therefore applies to **every** ingested Achiever + Starwave deal, not a demo subset.

Architecture (`D:\Prop\docs\architecture.md:24`): “Plan-group mappings are labels, not fetch filters.”

This slot did **not** re-run a live manager census. Prior same-day W500_0 census (do not treat as this slot’s measurement): Achiever 8 groups / 6512 logins, Starwave 10 / 1948, total 18 / 8460 via `GroupRequestArray` + `UserRequestArray`. The **code path** is “all manager-visible”, not “first N”.

### 5.2 Copy to cTrader must not send live orders (measured this pass)

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (`CTraderFixOptions.cs:35`) |
| `DependencyInjection` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” (`DependencyInjection.cs:40-41`) |
| `CTraderFixLogonHostedService` | logon 35=`A` only; then `_runtime.RealCopyEnabled = false` (`:68`) |
| `CTraderFixSession.BuildLogon` | only `(35, "A")` (`:96`) — no `(35, "D")` |
| Product `Fix.CTrader` 35= builders | `A` (logon), `y`/`V` (quotes), harness `A`/`3`/`0`/`y`/`X`/`8`. **No `D`.** |
| `apps/fix-worker/Worker.cs` | stamps TRADE `LastError = "No live TRADE socket. NewOrderSingle remains off."`; even if config flag true, it **warns and still does not send** (`:41-46`) |
| Dashboard copy | `LiveCopyPage.tsx` / `OverviewPage.tsx` state NewOrderSingle is disabled |

Shadow path (`ShadowCopyEngine`) is in-memory mark-to-market. It never opens a TRADE socket.

**Risk to capital from this process today: none.** Wrong volume scale would become a capital event **only after** a `35=D` sender exists. That is why the scale must stay 10000 *before* anyone arms copy.

---

## 6. Worked integers (no live broker required)

| Native `Volume()` | `/100` (wrong) | `/10000` (correct) | `/1e8` (wrong, ext) |
|---:|---:|---:|---:|
| 100 (0.01 lot classic) | 1.00 | **0.01** | 0.000001 |
| 1000 (fixture 0.10 lot) | 10.00 | **0.10** | 0.00001 |
| 10000 (1.00 lot) | 100.00 | **1.00** | 0.0001 |
| 50000 (docs example) | 500.00 | **5.00** | 0.0005 |
| 100000000 (1.00 lot **ext**) | 1e6 | 10000 | **1.00** |

This product never stores the last row: extractors do not call `VolumeExt()`.

---

## 7. What this slot does **not** claim

- Did **not** attach a live manager and print a real Achiever/Starwave `Volume()` vs terminal lots this pass. Confirmation is from official SDK math + extractors + YoPips production `/10000.0` + C# tests/TRX. A live 1.00-lot deal reading `10000` would be a useful extra measurement, not a missing proof of the API contract.
- Did **not** re-run `dotnet test` in this process. Cited TRX is on disk from 2026-08-18 13:48 +05:30, outcome **Passed**.
- Did **not** change product source, env, or flags.
- Did **not** print passwords, proxy auth, or FIX secrets.
- Did **not** emit or recommend a live `35=D`.

---

## 8. One-line pin

```text
IMTDeal::Volume() / SMTMath::VolumeToDouble = /10000.
VolumeExt() = /1e8 (unused on every product extractor).
Hundredths /100 = MT4 comment bug (mt5_types.h + docs/risk.md wording + one YoPips admin display).
Fetch all Achiever+Starwave groups/traders; keep NewOrderSingle off.
```
