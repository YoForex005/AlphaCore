# W500_RESEARCH_126 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_126.md` |
| Slot | **126** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 126 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. |
| Test source modified | **No.** |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Feature-flag name/value only. |
| Method | Independent re-read of official Manager SDK math + `IMTDeal`, Prop + YoPips extractors, C# native reader + `VolumeConverter` + reconstructor + fixtures, FIX send path, copy gates. Cross-checked E004 VolumeConverter TRX (2026-08-18T13:48:20+05:30) and prior census. This slot did **not** live-attach a manager. |
| Related (not rubber-stamped) | Slot 66 (`W500_RESEARCH_66.md`), A38, A81, B14, D14, D92, A006. Slot 66 is **stale on the REAL_COPY DI pin**; volume-scale conclusion is independently re-confirmed. |

---

## 0. Verdict (binding)

**CONFIRMED.** The integers this product copies from `IMTDeal::Volume()` are classic Manager volume:

```text
lots = IMTDeal::Volume() / 10000
1.00 lot  ==  10000   (MTAPI_VOLUME_DIV, 4 digits)
```

| Claim | Result |
|---|---|
| `IMTDeal::Volume()` scale is **10000** | **YES** — official `MTAPI_VOLUME_DIV` / `SMTMath::VolumeToDouble` / `VolumeToInt(1.0)` |
| Scale is hundredths (`/ 100`) | **NO** — MT4 convention. Wrong comment in `mt5_types.h:75`. Not an official MT5 Manager scale. |
| Scale is `VolumeExt` (`/ 1e8`) | **NO** — that is `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` only |
| Product extractors call `VolumeExt()` | **NO** — 0 calls under `D:\Prop\mt5-sdk\src`, 0 under `D:\Prop\src\Mt5`, 0 under `D:\Prop\apps`, 0 under YoPips `src`. One C# **XML comment** only (`VolumeConverter.cs:6`). |
| C# default converter matches the wire | **YES** — `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor binds `VolumeConverter.Manager` |
| Unit tests pin 10000 | **YES** — E004 `VolumeConverterTests` 3/3 **Passed** (2026-08-18T13:48:20+05:30) |
| Live cTrader send uses this scale today | **N/A** — there is **no** `35=D` / `NewOrderSingle` sender. Capital at risk from copy = **NONE** (`SAFE_BY_ABSENCE`) |

Do **not** flip the default to `100` or `100_000_000` while extractors still copy `deal->Volume()` / `d.Volume()`. That is a silent **100×** or **10_000×** sizing bug the moment live copy is armed.

A81 documented both official scales, then **wrongly** recommended ctor default `1e8`. B14 / D14 / D92 / slot 66 keep **10 000**. This slot independently re-read the same sources and **agrees with B14/D92** on the integer scale.

**Flag drift vs slot 66 (honesty, not a volume-scale change):** slot 66 recorded DI + logon host forcing `RealCopyEnabled=false`. Current `DependencyInjection.cs:41` **binds** `REAL_COPY_EXECUTION_ENABLED` from configuration. Operator `.env` on this machine sets that flag **true**. `CTraderFixLogonHostedService` **no longer** overwrites the flag to false. This does **not** send orders: `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, and product C# still has **0** `35=D` builders.

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

```255:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
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
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

The **10000** that appears in the ext converters is the **ratio between the two official scales**, not a third scale. Classic `Volume()` already *is* 4-digit. Extended is classic × 10000.

Dataset types confirm the split (`MT5APIDataset.h`): `TYPE_VOLUME = 200` vs `TYPE_VOLUME_EXT = 202`. Official deal dataset field is `FIELD_DEAL_VOLUME_EXT = 2014` (8-digit). There is **no** `FIELD_DEAL_VOLUME` (classic) on that enum. Reports that divide by `1e8` are bound to **ext**, not to `IMTDeal::Volume()`.

### 1.2 `IMTDeal` has two getters

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`

| Method | Lines | Comment | Unit |
|---|---|---|---|
| `virtual uint64_t Volume(void) const=0` | 141–142 | `//--- deal volume` | classic 4-digit (`/ 10000` lots) |
| `virtual uint64_t VolumeClosed(void) const=0` | 186–187 | `//--- closed volume` | same classic family |
| `virtual uint64_t VolumeExt(void) const=0` | 230–231 | `//--- deal volume with extended accuracy` | 8-digit (`/ 1e8` lots) |
| `virtual uint64_t VolumeClosedExt(void) const=0` | 233–234 | `//--- closed volume with extended accuracy` | same ext family |

Same dual API exists on `IMTPosition`, `IMTOrder`, `IMTRequest`.

### 1.3 Official examples bind `Volume()` to `/10000` or `VolumeToDouble`

MetaQuotes Manager sample places **1.00 lot** with the 4-digit helper:

```186:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
```

`VolumeToInt(1.0)` = **10000**, not `100`, not `100000000`.

Official report plugin divides **`deal->Volume()` by `10000.0`**:

```628:628:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Report\Trades.Standard.Reports\Reports\ExecutionType.cpp
      DOUBLE volume=fabs((deal->Volume()/10000.0)*deal->ContractSize()*deal->RateProfit());
```

Other official samples call `SMTMath::VolumeToDouble(deal->Volume())` (`AgentsDetailed.cpp`, `FeedCommission\PluginInstance.cpp`, `GatewayUtils.h`). Capital / NFA / Daily reports that use `VolumeExt()` convert with `VolumeExtToDouble` / `VolumeExtToSize` — those are **ext** paths and are **not** what Prop extractors copy.

---

## 2. What this product actually copies

### 2.1 C++ Manager extractors (Prop `mt5-sdk`)

Both session and manager copy **`deal->Volume()` unchanged**. They never call `VolumeExt()`.

`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (`MT5Manager::extractDeal`):

```1508:1517:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
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

Same for positions: `d.volume = pos->Volume()` (manager 1495, pool 833). Send path writes `request->Volume(req.volume)` (classic setter), not `VolumeExt` (`mt5_manager.cpp:1130,1191,1201,1243`; `mt5_pool.cpp:404,414,456,801`).

Grep `VolumeExt` under `D:\Prop\mt5-sdk\src`: **0 hits**.

C++ unit fixture `mt5_http_client_pool_timeout_test.cpp:94` sets `request.volume = 10000`. That is `SMTMath::VolumeToInt(1.0)` — **1.00 lot on the 4-digit scale**. Not hundredths (`100`) and not ext (`100000000`).

### 2.2 YoPips PropFirm extractors (same Manager integers)

YoPips is the production Manager consumer this lab was extracted from.

| File | Line | Assignment |
|---|---:|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | 1517 | `d.volume = deal->Volume();` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | 787 | `d.volume = deal->Volume();` |
| `mt5_types.h` | 75 | **wrong comment** “hundredths of lots” (same as Prop) |

Grep `VolumeExt` under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`: **0 hits**.

Production lot conversion is `/ 10000.0` (not `/100.0`, not `/1e8`):

| File | Expression | Meaning |
|---|---|---|
| `src\services\trade_service.cpp:92` | `deal.volume / 10000.0` | deal `lot_size` JSON |
| `src\services\trade_service.cpp:117` | `pos.volume / 10000.0` | position `lot_size` JSON |
| `src\http\controllers\journal_controller.cpp:192` | `deal.volume / 10000.0` | journal lots |
| `src\http\controllers\export_controller.cpp:146` | `deal.volume / 10000.0` | CSV lots |
| `src\http\controllers\symbol_controller.cpp:288` | `pos.volume / 10000.0` | position lots |
| `src\http\controllers\symbol_controller.cpp:21–23,59–61,174–176` | `volume_min/max/step / 10000.0` | symbol grid |
| `src\services\worker_service.cpp:547, 650` | `pos/deal.volume / 10000.0` | worker JSON |
| `src\services\trade_execution_service.cpp:1425` | `units / 10000.0L` | `checkLegacyNativeVolume` — native must be exactly `lots * 10000` |

One display-only leftover (`admin_dashboard_controller.cpp:996`): `pos.volume / 100.0`. That is **admin exposure display**, not the execution path. It treats classic `Volume()` as hundredths and would overstate lots **100×** on that dashboard. Evidence of the **comment bug**, not of a third API. Prop C# must not inherit this divisor.

### 2.3 C# native connector (live ingest path)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadPositions` / `ReadDeals`:

```396:424:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                p.Volume(),
                ...
                d.Volume(),
```

DTO field is named `VolumeNative` (`Mt5Contracts.cs:32, 45`). Persistence stores that same `ulong` (`Mt5Deal.VolumeNative`, `Mt5Position.VolumeNative`). No conversion at ingest. Conversion to lots happens later in `TradeReconstructor` via `VolumeConverter`.

Grep `VolumeExt` under `D:\Prop\src\Mt5`: **0 hits**.  
Grep `VolumeExt` under `D:\Prop\src\Domain`: **1 hit**, the XML comment in `VolumeConverter.cs`. No runtime call.  
Grep `VolumeExt` under `D:\Prop\apps`: **0 hits**.  
Grep `VolumeExt` under `D:\Prop\tests` (product tests via Unit file): tests pin Manager **10_000**, not ext.

### 2.4 C# default scale = 10000

`D:\Prop\src\Domain\Volume\VolumeConverter.cs`:

```10:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
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
- DI: `services.AddSingleton<TradeReconstructor>()` (`DependencyInjection.cs:53`) uses that default ctor.
- Lots: `var lots = _volume.ToLots(deal.VolumeNative)` (line 89) → `native / 10000`.
- `DemoBrokerFactory.VolumeScale = 10_000m`; `Lots(0.10m)` → native **1000** (`FakeMt5BrokerConnector.cs:72–74`).

---

## 3. Measured compile / test pins (not just source comments)

### 3.1 Unit tests (E004 run, 2026-08-18T13:48:20+05:30)

From `D:\Prop\reports\swarm\20260818\_tmp_e004\unit.trx` / `unit.console.txt` (this slot re-read the TRX; did not re-attach Manager):

| Test | Outcome |
|---|---|
| `VolumeConverterTests.Manager_scale_maps_0_10_lots_to_1000_native` | **Passed** — `Scale == 10_000`; `ToNative(0.10m) == 1000`; `ToLots(1000) == 0.10m` |
| `VolumeConverterTests.Extended_scale_maps_one_lot_to_100_million` | **Passed** — opt-in only |
| `VolumeConverterTests.Hundredths_comment_is_not_the_default` | **Passed** — `Manager.Scale != HundredthsScale` |

Source (`D:\Prop\tests\Unit\VolumeConverterTests.cs`) is the same three Facts.

`TradeReconstructionTests.Reconstructs_simple_round_trip` feeds `VolumeNative = 1000` and asserts `InitialVolumeLots == 0.10m`. That identity holds **only** if the divisor is **10000**:

| Divisor | 1000 native → lots | Fixture expect 0.10? |
|---|---:|---|
| **10000** (correct) | **0.10** | yes |
| 100 (hundredths) | 10.00 | **no — 100×** |
| 1e8 (`VolumeExt`) | 0.00001 | **no — 10 000×** |

### 3.2 Prior compiled Domain eval (D92, cited not re-run)

`_tmp_d92_vote\stdout.txt` previously printed `ctor_default_Scale=10000`, `Manager.ToLots(10000)=1`, `Extended.ToLots(10000)=0.0001`. Feeding classic `10000` to A81’s 1e8 default yields **0.0001 lots**. Reconstruction `FlatEpsilon` is `0.0000001m`, so `0.0001` is accepted as a real trade **10 000× too small**.

---

## 4. The “hundredths” claim is a comment bug (MT4), not a third API

### 4.1 Wrong comment on the C++ DTO

`D:\Prop\mt5-sdk\src\core\mt5_types.h:75` and the YoPips twin:

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

“Hundredths of lots” means `lots * 100`. That is the **MT4** `MODE_LOTSTEP` convention. It is **not** defined in `MT5APIMath.h`. The field is filled from `pos->Volume()` (classic 4-digit). The comment is **false**.

`DealData.volume` in the same file has **no** unit comment but is filled from the same `deal->Volume()`.

### 4.2 Docs mix contract size with integer scale

`D:\Prop\docs\architecture.md:23` is correct: `Volume default scale = 10_000 (IMTDeal.Volume())`.

`D:\Prop\docs\trade-reconstruction.md:31–37` says gold volume is “in hundredths of lots where 1 lot = 100 oz” **and** then `display_lots = native_volume / MT5_VOLUME_SCALE` with default **10000**. The 100 oz figure is **contract size**, not the Manager integer. The worked example (`50000` native = 5.0 lots) only works with divisor **10000**.

`D:\Prop\docs\risk.md:65` repeats the same contradiction: “volume in hundredths of lots (50000 = 5.0 lots)” **and** `MT5_VOLUME_SCALE=10000`. `50000 / 100 = 500` lots. The **example number** is classic 4-digit; the **word** “hundredths” is wrong.

A21 defines a **downstream** remaining-volume unit `volume_h` (1.00 lot = 100) **after** an adapter. Implemented reconstruction uses **decimal lots** via `VolumeConverter.Manager`, not integer hundredths. Mixing A21 `volume_h` with raw `VolumeNative` is the same 100× bug as believing `mt5_types.h:75`.

### 4.3 Blast radius if someone believes the wrong divisor

Assume a real deal `Volume() = 10000` (1.00 lot):

| Divisor used | Lots computed | Error vs truth |
|---|---:|---|
| **10000** (correct `Volume()`) | **1.00** | 1× |
| **100** (hundredths / MT4) | **100.00** | **100× oversize** |
| **100_000_000** (`VolumeExt`) | **0.0001** | **10 000× undersize** |

If live copy were armed: hundredths would send **100×** size; ext-as-default would send **0.0001** lots (or, inverted on send, `ToNative(1m)` into `IMTRequest::Volume()` would write `100_000_000` classic units = **10 000 lots**).

---

## 5. Goal context — fetch ALL groups / ALL traders; do not send live

### 5.1 Both brokers, all groups, all manager users

`LiveMt5Registration.CreateConnectors` registers **both** `ACHIEVER` and `STARWAVEFX` native connectors (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs:23–49`). Dummy/fake is refused if real passwords are missing (`DependencyInjection.cs:36–37`). Starwave `ProxyEnabled = false` (hard pin, L45). This slot does **not** print those secrets.

`GetGroupsCore` requests `GroupRequestArray("*")`, then falls back to `GroupTotal`/`GroupNext` — **all** groups the manager can see, not a plan-name filter (`NativeMt5BrokerConnector.cs:154–183`).

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` and unions users by login (`189–213`). `ReadAccountsForGroup` uses `UserRequestArray` first, cache `UserGetByGroup` only on hard fail, then `UserLogins`+`UserRequestByLogins` (`223–232`).

`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` call exactly that pair (`GetGroupsAsync` + `GetAccountsAsync(null)`). Bulk deals go `DealRequestByGroup` per group (`DealIngestionService.cs:45–70`).

Architecture (`docs/architecture.md:24`): “Plan-group mappings are labels, not fetch filters.”

Prior live census (this slot did **not** re-attach; cited, not re-run): `LIVE_MANAGER_FETCH_MEASURED.md` + `CREDENTIALS_AND_COPY_STATUS.md` — Achiever **8 groups / 6512 traders** (HTTP proxy) + Starwave **10 groups / 1948 traders** (direct) = **18 / 8460**. Dashboard `/api/traders` = 8460, `/api/groups` = 18. Volume scale therefore applies to **every** ingested Achiever + Starwave deal, not a demo subset.

### 5.2 Copy to cTrader must not send live orders (measured this pass)

| Gate | Measured state |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default **`false`** (`CTraderFixOptions.cs:35`) |
| `DependencyInjection` | **Binds env** `REAL_COPY_EXECUTION_ENABLED=="true"` (`DependencyInjection.cs:41`). **Not** a hardcoded false. Operator `.env` on this machine is **true**. |
| `CTraderFixLogonHostedService` | Logs `RealCopyArmed={Armed}`; does **not** force false (L68–70). Only opens logon. |
| `CopyTradingService.NewOrderSingleImplemented` | **`false` const** (`CopyTradingService.cs:16`) |
| `CopyTradingService.VenueReconciled` | **`false` const** (L15) |
| Persisted `RiskDecision.AllowFixSend` | **hardcoded `false`** (L192) |
| Live-send `if` | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — unreachable (L198). Else `SHADOW_ONLY`. |
| `CTraderFixSession.BuildLogon` | only `(35, "A")` — no `(35, "D")`. Single `WriteAsync` of that logon. |
| Grep `35=D` under `D:\Prop\src` | **0 hits** |
| Grep `NewOrderSingle` under `D:\Prop\src` | comments / logs / option name / FSM helper / `NewOrderSingleImplemented` const — **no builder** |
| `/api/settings` | `FEATURE_COPY_TRADING_ENABLED = true`; `REAL_COPY_EXECUTION_ENABLED` bound to `runtime.RealCopyEnabled` (can be true if env loaded) |
| `LiveRuntimeStatus.Snapshot` | even when armed: “NewOrderSingle still unimplemented; … No ticket will be sent.” |
| `QuantityNormalizer` | dest min/step/max only; **zero** product callers write FIX tag 38 |

Shadow path (`ShadowCopyEngine`) is in-memory. It never opens a TRADE socket for `35=D`.

**Risk to capital from this process today: NONE.** Wrong volume scale would become a capital event **only after** a `35=D` sender exists. That is why the scale must stay 10000 *before* anyone implements send. An armed `REAL_COPY` flag without a builder is **not** a live order.

---

## 6. Worked integers (no live broker required)

| Native `Volume()` | `/100` (wrong) | `/10000` (correct) | `/1e8` (wrong, ext) |
|---:|---:|---:|---:|
| 1000 (fixture 0.10 lot) | 10.00 | **0.10** | 0.00001 |
| 10000 (1.00 lot) | 100.00 | **1.00** | 0.0001 |
| 50000 (docs example) | 500.00 | **5.00** | 0.0005 |
| 100000000 (1.00 lot **ext**) | 1e6 | 10000 | **1.00** |

This product never stores the last row: extractors do not call `VolumeExt()`.

---

## 7. What this slot does **not** claim

- Did **not** attach a live manager and print a real Achiever/Starwave `Volume()` vs terminal lots this pass. Confirmation is from official SDK math + extractors + YoPips production `/10000.0` + unit tests. A live 1.00-lot deal reading `10000` would be a useful extra measurement, not a missing proof of the API contract.
- Did **not** change product source, env, or flags.
- Did **not** print passwords, proxy auth, or FIX secrets.
- Did **not** emit or recommend a live `35=D`.
- Did **not** re-run `LiveBrokerProbe`; census **18 / 8460** is prior measure (`LIVE_MANAGER_FETCH_MEASURED.md`, 2026-08-18T08:42Z).
- Did **not** treat slot 66’s “DI pins false” sentence as current. That pin drifted.

---

## 8. One-line pin

```text
IMTDeal::Volume() / SMTMath::VolumeToDouble = /10000.
VolumeExt() = /1e8 (unused on this wire).
Hundredths /100 = MT4 comment bug (mt5_types.h + one YoPips admin display).
Fetch all Achiever+Starwave groups/traders; NewOrderSingle still unimplemented.
REAL_COPY env may be true; 35=D still absent.
Risk to capital: NONE (SAFE_BY_ABSENCE).
```
