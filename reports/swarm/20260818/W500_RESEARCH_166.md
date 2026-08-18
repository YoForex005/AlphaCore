# W500_RESEARCH_166 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_166.md` |
| Slot | **166** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 166 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000**, not hundredths (`100`), and not `VolumeExt` (`1e8`) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Flag names and booleans only. No manager / proxy / FIX passwords. |
| Method | Independent re-read of official Manager SDK math + `IMTDeal`/`IMTPosition`, Prop + YoPips extractors, C# native reader + `VolumeConverter` + reconstructor + fixtures, FIX send path, copy gates. Cross-checked D92 Domain eval (`_tmp_d92_vote\stdout.txt`), E004 unit census (2026-08-18T13:48:20+05:30), and `LIVE_GROUPS_AND_TRADERS.json` (08:42:16Z). This slot did **not** live-attach a manager and did **not** send FIX. |
| Related (not rubber-stamped) | Slots 6/26/46/66/86/106/126/146, A38, A81, B14, D14, D92, A006. Volume-scale conclusion independently re-confirmed from the same headers and the live extractors. |

---

## 0. Verdict (binding)

**CONFIRMED.** The integers this product copies from `IMTDeal::Volume()` (C++ `deal->Volume()`, C# `d.Volume()`) are classic Manager volume:

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
| Scale is `VolumeExt` (`/ 1e8`) | **NO** — that is `IMTDeal::VolumeExt()` / `MTAPI_VOLUME_EXT_DIV` / `FIELD_DEAL_VOLUME_EXT` only |
| Product extractors call `VolumeExt()` | **NO** — 0 calls under `D:\Prop\mt5-sdk\src`, 0 under `D:\Prop\src` (except one XML comment), 0 under `D:\Prop\apps`, 0 under YoPips `src` |
| C# default converter matches the wire | **YES** — `VolumeConverter.ManagerVolumeScale = 10_000m`; reconstructor binds `VolumeConverter.Manager` |
| Unit tests pin 10000 | **YES** — `VolumeConverterTests` 3 facts + reconstruction fixtures treat `VolumeNative=1000` as **0.10** lots. E004 unit run: 64 passed / 22 skipped / 0 failed |
| Live cTrader copy uses this scale today | **N/A** — copy hop has **no** `35=D` / `NewOrderSingle` sender. Capital at risk from copy = **NONE** (`SAFE_BY_ABSENCE`) |

Do **not** flip the default to `100` or `100_000_000` while extractors still copy `deal->Volume()` / `d.Volume()`. That is a silent **100×** or **10_000×** sizing bug the moment live copy is armed.

A81 documented both official scales, then **wrongly** recommended ctor default `1e8`. B14 / D14 / D92 / slots 66/106/126/146 keep **10 000**. This slot independently re-read the same sources and **agrees with B14/D92**.

**Flag drift vs older status docs (honesty, not a volume-scale change):** `DependencyInjection.cs:41` **binds** `REAL_COPY_EXECUTION_ENABLED` from configuration. Lab `.env` L73 is `true`. `CTraderFixLogonHostedService` **does not** overwrite the flag to false. `CREDENTIALS_AND_COPY_STATUS.md` “REAL_COPY forced false” is **stale**. This still does **not** send copy orders: `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`, persisted `AllowFixSend = false`, and product hosted FIX still has **0** `35=D` builders.

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

There is **no** `#define` for `100` and no “hundredths” string in this header. The official classic divisor is **10000.0**. The official extended divisor is **100000000.0**.

`VolumeToInt` is `PriceToIntPos(volume, MTAPI_VOLUME_DIGITS)` with `digits=4`. `s_decimal[4] = 10000.0` (`MT5APIMath.h:66–72`). Therefore:

```text
SMTMath::VolumeToInt(1.0)  = 1.0 * 10000.0  = 10000
SMTMath::VolumeToDouble(n) = n / 10000.0
```

Official SDK example `SimpleManager.cpp:190` writes one lot as classic integer:

```190:190:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         request->Volume(SMTMath::VolumeToInt(1.0));
```

That is `IMTRequest::Volume(10000)`, not `100` and not `100000000`.

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

Same split on `IMTPosition` (`MT5APIPosition.h:132` `Volume()` vs `:205` `VolumeExt()`). Dataset report columns use **ext only** for deal volume:

```461:461:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h
      FIELD_DEAL_VOLUME_EXT                  =2014,         // uint64_t    , deal volume with extended accuracy
```

There is **no** `FIELD_DEAL_VOLUME` (classic) in the dataset enum. Official Capital reports that divide by `1e8` do so because they bind `FIELD_DEAL_VOLUME_EXT` / `deal->VolumeExt()`, **not** because `IMTDeal::Volume()` is 8-digit.

`TYPE_VOLUME = 200` vs `TYPE_VOLUME_EXT = 202` (`MT5APIDataset.h:39–41`) is the same two-scale split.

### 1.3 Worked numbers (from macros, not from a live server)

| Lots (double) | Hundredths (`*100`) — **not MT5** | Classic `Volume()` (`*10_000`) | Ext `VolumeExt()` (`*100_000_000`) |
|---|---:|---:|---:|
| 1.00 | 100 | **10 000** | **100 000 000** |
| 0.10 | 10 | **1 000** | **10 000 000** |
| 0.01 | 1 | **100** | **1 000 000** |
| 0.0001 | — | **1** | **10 000** |

If a live deal returns classic `Volume() = 1000` (0.10 lots):

| Wrong divisor | Result | Blast |
|---|---:|---|
| `/ 10000` (correct) | **0.10** lots | none |
| `/ 100` (hundredths) | **10.00** lots | **100× too large** — account-killing if sent |
| `/ 1e8` (`VolumeExt`) | **0.00001** lots | **10 000× too small** — silent under-size |

---

## 2. What this product actually copies (the wire)

### 2.1 C++ extractors copy `Volume()`, never `VolumeExt()`

Prop: `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`

```1489:1518:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
PositionData MT5Manager::extractPosition(const IMTPosition* pos) {
    // ...
    d.volume = pos->Volume();
    // ...
}

DealData MT5Manager::extractDeal(const IMTDeal* deal) {
    // ...
    d.volume = deal->Volume();
```

Same file also writes `request->Volume(volume)` (classic setter). Pool twin: `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp:833` / `:855`.

YoPips tree is the same extractors:

```1495:1517:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    d.volume = pos->Volume();
    // ...
    d.volume = deal->Volume();
```

`VolumeExt` grep under `D:\Prop\mt5-sdk\src` = **0**. Under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` = **0**.

### 2.2 The “hundredths” comment is a lie next to those extractors

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

Identical comment: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h:75`.

`DealData::volume` has **no** unit comment. The only explicit unit comment on a trade `volume` field is **wrong**. Following it makes every size **100×** too large. Product C# already records this:

```3:8:D:\Prop\src\Domain\Volume\VolumeConverter.cs
/// Converts MT5 native integer volume to lots.
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal-&gt;Volume(), so the default scale is 10_000.
```

`hundredths` string in product C# / C++ headers: **only** that XML comment + `mt5_types.h:75`.

### 2.3 C# native reader copies `CIMTDeal.Volume()`, not `VolumeExt()`

`NativeMt5BrokerConnector` references `MetaQuotes.MT5ManagerAPI64` / `MetaQuotes.MT5CommonAPI64` (`TraderIntelligence.Mt5.csproj`). Deal and position rows take the classic getter:

```396:424:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                p.Volume(),
                // ...
                d.Volume(),
```

Those values land in `Mt5DealDto.VolumeNative` / `Mt5PositionDto.VolumeNative` (`Mt5Contracts.cs:32,45`) and are persisted unchanged (`EfTradingStore`). Reconstruction divides by `VolumeConverter.Manager` (10 000):

```18:20:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
```

```89:89:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
            var lots = _volume.ToLots(deal.VolumeNative);
```

`VolumeExt` grep under `D:\Prop\src` `*.cs` = **1** (the XML comment in `VolumeConverter.cs`). Zero call sites.

---

## 3. C# converter + measured tests

### 3.1 Converter

```12:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;
    // ...
    public VolumeConverter(decimal scale = ManagerVolumeScale)
    // ...
    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
```

`HundredthsScale` exists only as a named constant so tests can assert it is **not** the default. There is no `VolumeConverter.Hundredths` factory.

### 3.2 Unit pins (source, this slot)

`D:\Prop\tests\Unit\VolumeConverterTests.cs`:

| Fact | Assert |
|---|---|
| `Manager_scale_maps_0_10_lots_to_1000_native` | `Scale == 10_000`; `ToNative(0.10m) == 1000`; `ToLots(1000) == 0.10m` |
| `Extended_scale_maps_one_lot_to_100_million` | `ToNative(1m) == 100_000_000` (opt-in only) |
| `Hundredths_comment_is_not_the_default` | `Manager.Scale != HundredthsScale` |

Reconstruction fixtures treat `VolumeNative=1000` as **0.10** lots (`TradeReconstructionTests.Reconstructs_simple_round_trip`: `InitialVolumeLots.Should().Be(0.10m)`). Partial close uses two 1000-tick legs → `MaxVolumeLots == 0.20m`. Reverse `InOut` 2000 ticks → remaining **0.10** lots.

Demo fake broker uses the same scale: `DemoBrokerFactory.VolumeScale = 10_000m`; `Lots(0.10m)` writes native **1000** (`FakeMt5BrokerConnector.cs:72–75,145`).

If hundredths were the law, `VolumeNative=1000` would be **10.00** lots and every reconstruction fixture would fail. If `VolumeExt` were the law, `1000 / 1e8 = 0.00001` lots and `InitialVolumeLots.Should().Be(0.10m)` would fail.

### 3.3 Prior measured eval (re-read this slot, not re-compiled)

`D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt` (D92 Domain eval, 2026-08-18T13:46:01+05:30):

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
Extended.ToLots(100000000)=1
default.ToNative(1)=10000
Extended.ToNative(1)=100000000
ratio_ext_div_mgr=10000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

Feeding classic `10000` into A81’s recommended `1e8` default yields **0.0001 lots**. Reconstruction `FlatEpsilon` is `0.0000001m`, so that would be accepted as a real trade **10 000× too small**.

E004 (`E004_tests.md`): unit TRX 2026-08-18T13:48:20+05:30, **64 passed / 22 skipped / 0 failed**. The 22 skips include `SourceDestinationQuantityConversionTests.Mt5_ticks_scale_is_10000` (Skip: “A38: source ticks / 10_000 = lots. Converter not implemented.”) — that skip is about the **missing destination `IQuantityConverter`**, not about the Manager scale. The Manager scale facts in `VolumeConverterTests` are **not** skipped.

This slot did **not** re-run `dotnet test`. Source + prior TRX agree.

---

## 4. ALL Achiever + Starwave groups and traders

### 4.1 Code path is catalog-wide (flag-blind)

`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync`:

- `GetGroupsAsync` → `GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback (`NativeMt5BrokerConnector.GetGroupsCore` L155–182).
- `GetAccountsAsync(null)` → every group name, then per-group `UserRequestArray` / `UserGetByGroup` / `UserLogins` (`GetAccountsCore` L191–212, `ReadAccountsForGroup` L223–232).
- Deals: `DealRequestByGroup` for each group, else per-login `DealRequest`.
- Positions: `GetGroupPositionsAsync("*")` or per-login.

No `Take(`/`Skip` on the catalog walk. `REAL_COPY_EXECUTION_ENABLED` is not consulted by the connector.

### 4.2 Prior live census (re-summed this slot; not re-attached)

File: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`utc`: **2026-08-18T08:42:16.8519545+00:00**. Note: “Passwords never written.”

| Broker | Connected | Groups | Accounts (header) | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | true (7212.6 ms) | **8** | **6512** | 1506 |
| STARWAVEFX | true (6413.5 ms) | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | **1984** |

Independent re-sum of `groupNames[].accounts` this slot:

**Achiever (8):** `contest\yo-1step` 2 + `contest\yo-2step` 179 + `contest\yo-instant` 4 + `contest\yo-payp` 5 + `demo\yo-1step` 4 + `demo\yo-2step` 6295 + `demo\yo-instant` 0 + `demo\yo-payp` 23 = **6512**.

**Starwave (10):** `Starwave\cent\FX1\grp1` 11 + `grp2` 4 + `demo\FX2\grp1` 170 + `grp2` 1735 + `real\FX3\grp1` 22 + `grp2` 0 + `grp3` 0 + `grp4` 4 + `grp5` 0 + `real\FX3\LP` 2 = **1948**.

Headers match the per-group sums. This slot did **not** live-attach; do not treat 08:42Z as a fresh probe.

---

## 5. Copy to cTrader must not send live orders (no loss)

### 5.1 Copy hop cannot emit `35=D`

| Gate | Measured |
|---|---|
| `CopyTradingService.NewOrderSingleImplemented` | **`const false`** (`CopyTradingService.cs:16`) |
| `CopyTradingService.VenueReconciled` | **`const false`** (`:15`) |
| Persist `RiskDecisionRecord.AllowFixSend` | **hardcoded `false`** (`:192`) even if `RiskEngine` would compute `allowSend` |
| Live-send branch | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (`:198`) — unreachable |
| Else branch | `Status = "SHADOW_ONLY"` + in-memory `ShadowCopyEngine.SimulateEntry` |
| Hosted copy tick | `CopyTradingHostedService` calls **only** `GenerateShadowIntentsAsync`; log: “Live NewOrderSingle still blocked.” |
| Hosted FIX session | `CTraderFixSession.BuildLogon` emits **`(35, "A")` only**. One `WriteAsync`. Socket disposed after logon reply. |
| Product `35=D` in `src` / `apps` / `*.json` / `*.csproj` | **0** (except demo-only tool; see residual) |
| YoPips C++ `src` cTrader FIX sender | **0** |

`RiskEngine.Evaluate` *can* set `AllowFixSend = true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. The copy service then **overwrites** the persisted flag to `false` and never builds a FIX order. `VenueReconciled` const false also keeps `allowSend` false even if `.env` arms `RealCopyEnabled`.

### 5.2 Residuals (honest, not a live-copy send)

1. **DI binds env.** `DependencyInjection.cs:41`: `RealCopyEnabled = configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` (case-insensitive). Lab `.env` L73 is `true`. Logon host no longer re-pins false. Architecture / README / `CREDENTIALS_AND_COPY_STATUS.md` “forced false” is **stale**. Flag armed ≠ sender present.

2. **Demo-only `35=D` exists outside the copy hop.** `CTraderFixDemoTestTrade.SendAsync` builds `35=D` with hardcoded `38=1000` (cTrader ounces, not MT5 ticks). Caller is `D:\Prop\tools\DemoFixTestTrade\Program.cs` only. Gate refuses unless host starts with `demo-`, sender starts with `demo.`, and account is not the live id. **Not** wired into `CopyTradingService` / API / workers.

3. **FIX worker** (`apps\fix-worker\Worker.cs`) never sends; it writes “NewOrderSingle remains off” onto session rows. If its own config flag is true it **warns** and still refuses.

4. **Quantity hop is still lots×0.05** (`AllocationFactor = 0.05m` + `QuantityNormalizer`). Destination ounces converter is missing (`IQuantityConverter` skipped tests). Irrelevant to live send while NOS is absent; relevant the day a sender is added.

### 5.3 Risk to capital

**NONE** from this process’s copy path (`SAFE_BY_ABSENCE`). No product `NewOrderSingle` implementation. Hosted FIX is logon-only. Persist path cannot mark `AllowFixSend`. Wrong volume scale would become capital risk **only after** a sender is added; until then it is a reconstruction/scoring correctness issue, not a live-loss issue.

---

## 6. What would be false, and how we would know

| Hypothesis | Falsifier already in tree |
|---|---|
| Scale is hundredths | `VolumeConverterTests.Hundredths_comment_is_not_the_default`; `Reconstructs_simple_round_trip` expects 0.10 lots from 1000 native |
| Scale is `VolumeExt` 1e8 | Same fixture; `Manager.ToLots(1000)` would be 0.00001; D92 eval `Extended.ToLots(10000)=0.0001` |
| Extractors already use `VolumeExt()` | 0 product call sites (this slot’s grep) |
| Live copy already sends | 0 product `35=D` builders; NOS const false |

This slot did not attach a manager, so it cannot quote a live `IMTDeal::Volume()` integer from Achiever/Starwave. The official math + extractors + fixtures are sufficient to bind the **scale**. A future live probe should log one gold deal as `{ Volume, VolumeExt, VolumeExt/Volume }` and expect ratio **10000**.

---

## 7. Binding recommendation

Keep `VolumeConverter` default / reconstructor / fake-broker scale at **10 000**. Treat `mt5_types.h` “hundredths” as a **wrong comment**. Do not switch to `VolumeExt` / `1e8` unless **all** extractors (`extractDeal`, `extractPosition`, `ReadDeals`, `ReadPositions`, `request->Volume`) are switched in the same change and fixtures retargeted (`1000` → `10_000_000` for 0.10 lots). Keep copy `NewOrderSingle` unimplemented until risk + recon + destination-qty converter are real.

---

## 8. Files read (absolute)

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIPosition.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDataset.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\tests\Unit\VolumeConverterTests.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt`
- `D:\Prop\reports\swarm\20260818\D92_volume_vote.md`
- `D:\Prop\reports\swarm\20260818\E004_tests.md`
- `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md`
- `D:\Prop\reports\swarm\20260818\B14_volume_review.md`
