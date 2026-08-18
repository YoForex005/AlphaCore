# W500_RESEARCH_86 — Confirm `IMTDeal.Volume` scale is **10000**, not hundredths, not `VolumeExt` 1e8

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_86.md` |
| Slot | **86** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 86 |
| Topic | Confirm `IMTDeal.Volume` scale is **10000** (not hundredths / not `VolumeExt` 1e8) |
| Goal context | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source edited | **No** |
| Test source edited | **No** |
| Secrets printed | **No** |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read official `MT5APIMath.h`, `MT5APIDeal.h`, product extractors (`mt5_manager.cpp` / `mt5_pool.cpp` in both trees), C# `VolumeConverter` + tests + D92 compiled eval, C# `ReadDeals` (`d.Volume()`), cTrader FIX session (no `35=D`). No live attach. No live send. |

Classification vocabulary is architecture §73.B.

---

## 0. Verdict (honest)

**CONFIRMED.** `IMTDeal::Volume()` is classic Manager 4-digit volume. **1.00 lot = 10 000.** That is `MTAPI_VOLUME_DIV`, not hundredths (`100`) and not `VolumeExt` (`100 000 000`).

| Claim | Measured |
|---|---|
| Official classic divisor for `Volume()` | **`MTAPI_VOLUME_DIV = 10000.0`** (`MTAPI_VOLUME_DIGITS = 4`) |
| Official ext divisor for `VolumeExt()` | **`MTAPI_VOLUME_EXT_DIV = 100000000.0`** (`MTAPI_VOLUME_EXT_DIGITS = 8`) |
| Official hundredths divisor | **Does not exist** in Manager SDK math |
| Product C++ deal extractor | `d.volume = deal->Volume()` — **never** `VolumeExt()` |
| Product C# deal reader | `d.Volume()` into `Mt5DealDto.VolumeNative` — **never** `VolumeExt()` |
| Domain default | `VolumeConverter` ctor default / `Manager` factory = **10 000** |
| `HundredthsScale = 100` | Constant only. **Not default.** No factory. |
| `ExtendedVolumeScale = 100_000_000` | Opt-in factory only. **Unused** on the ingest wire. |
| `mt5_types.h` comment “in hundredths of lots” | **Wrong.** MT4 leftover. Integers on that field still come from `Volume()`. |
| YoPips trade path | `MT5_VOLUME_PER_LOT = 10000.0` in both `TradeExecutionService` and `TerminalStateService` |
| Copy to cTrader can place a live order today | **No.** Zero `35=D` / `NewOrderSingle` builders. `RealCopyEnabled` pinned **false**. CopyIntents are `SHADOW_ONLY`. **`SAFE_BY_ABSENCE`.** |

**One-liner:** Treat every `IMTDeal::Volume()` / `CIMTDeal.Volume()` integer as **lots × 10 000**. Dividing by 100 inflates lots **100×**. Dividing by 1e8 shrinks lots **10 000×**. Live cTrader send is still absent, so the scale bug cannot spend capital today.

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

Ratio of the two official scales: `MTAPI_VOLUME_EXT_DIV / MTAPI_VOLUME_DIV = 10 000`. That is why `VolumeFromVolumeExt` divides by 10000 and `VolumeExtFromVolume` multiplies by 10000.

---

## 1. Official SDK pin — `IMTDeal::Volume()` is 4-digit

### 1.1 `IMTDeal` has two volume getters

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

Same pair exists for `VolumeClosed` / `VolumeClosedExt`. Comments on the header: `Volume` = “deal volume”; `VolumeExt` = “deal volume with extended accuracy”. They are **not interchangeable**.

### 1.2 Math constants (authoritative)

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
Copyright: MetaQuotes Ltd., 2000–2026. Same file at  
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

Classic conversion used by `SMTMath::VolumeToInt` / `VolumeToDouble` (the pair that matches `IMTDeal::Volume()`):

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

`MTAPI_VOLUME_DIGITS = 4` → `PriceToIntPos(1.0, 4)` = **10 000**.  
`VolumeToDouble(10000)` = `10000 / 10000.0` = **1.00 lot**.

Extended conversion (only for `VolumeExt()` / `FIELD_*_VOLUME_EXT`):

```290:300:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeExtToInt(const double volume)
  {
   return(PriceToIntPos(volume,MTAPI_VOLUME_EXT_DIGITS));
  }
inline double SMTMath::VolumeExtToDouble(const uint64_t volume)
  {
   return(PriceNormalize(volume/double(MTAPI_VOLUME_EXT_DIV),MTAPI_VOLUME_EXT_DIGITS));
  }
```

Cross-scale helpers prove the two integers are 10 000× apart, not 100×:

```283:320:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
inline uint64_t SMTMath::VolumeFromVolumeExt(const uint64_t volume_ext)
  {
   return(volume_ext/10000);
  }
// ...
inline uint64_t SMTMath::VolumeExtFromVolume(const uint64_t volume)
  {
   return(volume*10000);
  }
```

| Function | Direction | Formula | 1.00 lot |
|---|---|---|---:|
| `VolumeToInt(1.0)` | lots → classic | × 10 000 (4 digits) | **10 000** |
| `VolumeToDouble(10000)` | classic → lots | ÷ 10 000 | **1.00** |
| `VolumeExtToInt(1.0)` | lots → ext | × 100 000 000 (8 digits) | 100 000 000 |
| `VolumeExtToDouble(100000000)` | ext → lots | ÷ 100 000 000 | 1.00 |
| `VolumeFromVolumeExt(100000000)` | ext → classic | ÷ 10 000 | 10 000 |
| `VolumeExtFromVolume(10000)` | classic → ext | × 10 000 | 100 000 000 |

There is **no** `MTAPI_VOLUME_HUNDREDTHS` and no `÷ 100` helper in this header.

### 1.3 Official sample writes 1.00 lot as `VolumeToInt(1.0)`

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp` (same under YoPips SDK):

```185:191:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\SimpleManager\SimpleManager.cpp
         //--- buy 1.00 EURUSD
         request->Clear();
         request->Login(user->Login());
         request->Action(IMTRequest::TA_DEALER_POS_EXECUTE);
         request->Type(IMTOrder::OP_BUY);
         request->Volume(SMTMath::VolumeToInt(1.0));
         request->Symbol(L"EURUSD");
```

That is `request->Volume(10000)`, **not** `100` and **not** `100000000`.

### 1.4 WebAPI / PHP confirm old = 4 digits, new = 8 digits

Official WebAPI store is **ext**. The `Volume` property is a 4-digit view of the same 8-digit field (`÷ / × 10000`), not a hundredths view.

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs`:

```185:197:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\MTDeal.cs
    public ulong Volume 
      { 
       get { return MTUtils.ConvetToOldVolume(m_Volume);  }
       set { m_Volume = MTUtils.ConvertToNewVolume(value); }
      }
    /// <summary>
    /// deal volume with exta 8-digits accuracy
    /// </summary>
    public ulong VolumeExt 
     {
      get { return m_Volume;  }
      set { m_Volume = value; }
     }
```

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs`:

```352:364:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Utils\MTUtils.cs
    public static ulong ConvetToOldVolume(ulong new_volume)
      {
       return(new_volume/10000);
      }
    public static ulong ConvertToNewVolume(ulong new_volume)
      {
       return(new_volume*10000);
      }
```

PHP (`mt5_utils.php`) is identical: `ToOldVolume` = `/ 10000`, `ToNewVolume` = `* 10000`. Comments: “new 8-digits volume” ↔ “old 4-digits format”.

This product does **not** ingest WebAPI JSON. It copies the Manager C++ `Volume()` integer. So the wire is **old 4-digit**, not the WebAPI 8-digit storage field.

---

## 2. Product extractors copy `Volume()`, never `VolumeExt()`

### 2.1 Prop C++ (`D:\Prop\mt5-sdk\src`)

`VolumeExt()` hits under `D:\Prop\mt5-sdk\src`: **0**.

`MT5Manager::extractDeal` (`mt5_manager.cpp`):

```1508:1524:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
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
    d.price = deal->Price();
    d.profit = deal->Profit();
    d.commission = deal->Commission();
    d.storage = deal->Storage();
    d.time = deal->Time();
    d.comment = StringUtils::toUtf8(deal->Comment());
    return d;
}
```

Same assignment in the pool session (`mt5_pool.cpp` L846–862): `d.volume = deal->Volume();`.

Sibling extractors (same scale family):

| Extractor | Getter | Scale that matches |
|---|---|---|
| `extractDeal` | `deal->Volume()` | classic 10 000 |
| `extractPosition` | `pos->Volume()` | classic 10 000 |
| `extractOrder` | `order->VolumeInitial()` | classic 10 000 |
| symbol min/max/step | `VolumeMin()` / `VolumeMax()` / `VolumeStep()` | classic 10 000 |

Send path (YoPips / Prop C++ `SendTrade`) writes `request->Volume(req.volume)` / `request->Volume(volume)`, **not** `VolumeExt()`. Integers that leave the process on a dealer request are therefore also 4-digit.

### 2.2 YoPips C++ (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`)

`VolumeExt()` hits under YoPips `src\`: **0**.

`extractDeal` is line-for-line the same: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L1517 `d.volume = deal->Volume();` and `mt5_pool.cpp` L787 same.

YoPips pins the same number twice:

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.h` L240–244:

```240:244:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\trade_execution_service.h
    // MT5 legacy "Volume" field scale: 1.00 lot == 10000 units. Mirrors the unit
    // convention used by PositionData.volume / SymbolData.volume_* coming back
    // from the SDK. MUST be confirmed against the live symbol metadata in a
    // sandbox before any real (dry_run=false) execution.
    static constexpr double MT5_VOLUME_PER_LOT = 10000.0;
```

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\terminal_state_service.h` L112–115:

```112:115:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\services\terminal_state_service.h
    // MT5 legacy "Volume" scale: 1.00 lot == 10000 units. Mirrors
    // TradeExecutionService::MT5_VOLUME_PER_LOT so the lots we display match the
    // units accepted on the trade path.
    static constexpr double MT5_VOLUME_PER_LOT = 10000.0;
```

Lots → native on the YoPips trade gate (`trade_execution_service.cpp` L1261–1263):

```
units = llround(c.lots * MT5_VOLUME_PER_LOT)   // 1.00 lot → 10000
```

Legacy-close tests use `volume = 10000` as **one lot** (`legacy_close_compatibility_test.cpp` L119, L184, L211, L216, L225). HTTP client test: `request.volume = 10000`.

### 2.3 The “hundredths” comment is a lie about the same integer

Both trees carry:

```75:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;  // in hundredths of lots
```

Same line in YoPips `mt5_types.h` L75. That comment is **MT4** (`lots * 100`). The field is filled from `pos->Volume()` / `deal->Volume()` / `order->VolumeInitial()`, which are **MT5 classic 10 000**. `VolumeConverter` XML already records this:

> The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.  
> Existing mt5_manager.cpp copies deal->Volume(), so the default scale is 10_000.

`DealData.volume` itself has **no** hundredths comment — only `PositionData.volume` does. Both fields receive the same getter family.

### 2.4 C# live ingest copies the same getter

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `ReadDeals`:

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

`ReadPositions` L396: `p.Volume()`. DTO field name is `VolumeNative` (`Mt5Contracts.cs` L32, L45) — persist raw ulong, convert later.

`TradeReconstructor` default: `_volume = volume ?? VolumeConverter.Manager` (`TradeReconstructor.cs` L18–20). Every reconstructed lot is `native / 10_000` (`L89`).

---

## 3. C# `VolumeConverter` — measured default is 10 000

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

```12:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;

    public decimal Scale { get; }

    public VolumeConverter(decimal scale = ManagerVolumeScale)
    {
        if (scale <= 0)
            throw new ArgumentOutOfRangeException(nameof(scale), "Volume scale must be positive.");
        Scale = scale;
    }

    public decimal ToLots(ulong native) => native / Scale;
    // ...
    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
```

| Pin | Value | Role |
|---|---|---|
| `ManagerVolumeScale` | `10_000m` | **Default.** Matches `IMTDeal::Volume()` |
| Ctor default | `scale = ManagerVolumeScale` | `new VolumeConverter().Scale == 10000` |
| `ExtendedVolumeScale` | `100_000_000m` | Opt-in only. Matches `VolumeExt()` |
| `HundredthsScale` | `100m` | Named so tests can reject it. **No factory.** |

Demo fake uses the same pin: `DemoBrokerFactory.VolumeScale = 10_000m`; `Lots(0.10m) → 1000` (`FakeMt5BrokerConnector.cs` L72–74, L145). Achiever demo tape: 0.10 / 0.20 / 0.40 lots → native 1000 / 2000 / 4000.

Architecture map already states the binding (`D:\Prop\docs\architecture.md` L23):

> Volume default scale = 10_000 (`IMTDeal.Volume()`)

### 3.1 Unit tests lock 10 000, reject 100

`D:\Prop\tests\Unit\VolumeConverterTests.cs`:

- `Manager.Scale == 10_000`
- `ToNative(0.10m) == 1000`, `ToLots(1000) == 0.10m`
- `Extended.ToNative(1m) == 100_000_000`
- `Manager.Scale` **is not** `HundredthsScale`

`TradeReconstructionTests` constructs `new TradeReconstructor(VolumeConverter.Manager)` and asserts native **1000** → **0.10 lots** (`Reconstructs_simple_round_trip`). If the default were hundredths, that fixture would report **10.00 lots**. If it were 1e8, it would report **0.00001 lots**.

### 3.2 Compiled Domain eval (prior D92, still on disk)

Eval binary: `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\bin\Release\net8.0\D92VoteEval.exe`  
Source: `_tmp_d92_vote\Program.cs` (calls current `VolumeConverter`, no product edits).  
Stdout (`_tmp_d92_vote\stdout.txt`):

```
ctor_default_Scale=10000
Manager.Scale=10000
Extended.Scale=100000000
ManagerVolumeScale=10000
ExtendedVolumeScale=100000000
HundredthsScale=100
default_eq_Manager=True
default_eq_Extended=False
Manager.ToLots(10000)=1
Manager.ToLots(1000)=0.1
Manager.ToLots(100)=0.01
Extended.ToLots(10000)=0.0001
Extended.ToLots(100000000)=1
default.ToLots(10000)=1
default.ToNative(1)=10000
Manager.ToNative(1)=10000
Extended.ToNative(1)=100000000
ratio_ext_div_mgr=10000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

Measured: `new VolumeConverter().Scale == 10000`. Feeding classic `Volume()=10000` into the ext scale yields **0.0001 lots**. Reconstruction `FlatEpsilon` is `0.0000001m`, so `0.0001` would be accepted as a real trade **10 000× too small** — silent, not a loud fail.

A81 recommended flipping the ctor default to 1e8. That recommendation is **wrong for this tree today** while extractors still copy `Volume()`. B14 / D14 / D92 / this slot: keep **10 000**.

---

## 4. Blast radius if the scale is wrong

Assume a real Achiever/Starwave deal of **0.10 lot** (`Volume() = 1000`) later becomes dest qty.

| Divisor used | Lots computed | vs truth 0.10 | If that number were ever sent |
|---|---:|---|---|
| **10 000** (correct) | **0.10** | identity | intended size |
| 100 (hundredths / MT4) | **10.00** | **100× too large** | 100× capital / margin |
| 100 000 000 (`VolumeExt`) | **0.00001** | **10 000× too small** | ghost size or reject-below-min |

`QuantityNormalizer` is **not** a lots→ounces converter (W500_18). It does `sourceLots * allocation`. Wrong `ToLots` therefore poisons scoring (`MaxVolumeLots`), shadow `RequestedQuantity`, and any future FIX tag 38.

Do **not**:

1. Divide `IMTDeal::Volume()` by 100.
2. Divide `IMTDeal::Volume()` by 1e8.
3. Flip `VolumeConverter` default to `100_000_000` without switching extractors to `VolumeExt()`.
4. Treat `mt5_types.h` “hundredths” as law.

Do:

1. Persist `VolumeNative` as the raw `Volume()` ulong.
2. Convert with `VolumeConverter.Manager` (÷ 10 000).
3. If a future path reads `VolumeExt()` / `FIELD_DEAL_VOLUME_EXT`, use `VolumeConverter.Extended` and label the column.

---

## 5. Goal context — ALL groups / ALL traders; no live cTrader send

Volume scale is independent of the census walk. Confirming the fetch path cannot spend capital:

### 5.1 Fetch is request-wide, not a plan-group filter

`NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*", arr)` then falls back to `GroupTotal`/`GroupNext` (`NativeMt5BrokerConnector.cs` L155–183).  
`GetAccountsCore(null)` walks **every** returned group and `UserRequestArray` / `UserLogins` + `UserRequestByLogins` (L189–233).

Prior live census (cited, this slot did **not** re-attach): Achiever **8 groups / 6512 traders**, Starwave **10 / 1948**, total **18 / 8460**. Architecture: plan-group mappings are labels, **not** fetch filters (`docs/architecture.md` L24).

### 5.2 Copy to cTrader cannot send live orders

| Gate | Measured |
|---|---|
| `CTraderFixSession.BuildLogon` | Only outbound MsgType is `(35, "A")`. One `WriteAsync`. Sockets disposed after logon reply. File 135 lines. |
| Product `*.cs` / `*.json` / `*.csproj` | **0** hits for `35=D` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** |
| `AddTraderIntelligence` | `RealCopyEnabled = false` hard pin (`DependencyInjection.cs` L38–41) |
| `LiveRuntimeStatus.copyNote` | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` unless the pin is flipped |
| `EfTradingStore` demo/shadow persist | `Status = "SHADOW_ONLY"`; `ShadowCopyEngine.SimulateEntry` only |
| `apps/fix-worker` | Even if config is true, worker **logs a warning and still does not send** (`Worker.cs` L45–46) |
| YoPips `src` | 0 cTrader FIX senders |

**`SAFE_BY_ABSENCE`:** there is no NewOrderSingle builder to attach a wrong lot size to. Fetching all Achiever+Starwave traders cannot open a Pepperstone/cTrader position from this process.

### 5.3 Residual (honesty, not a FAIL on this slot)

- `mt5_types.h` L75 still says “hundredths”. Comment-only. Do not “fix” it in this research slot.
- A81’s “default to 1e8” note is still in `A81_volume_unit_conflict.md`. It is a **recommendation**, not product code. Product default remains 10 000.
- This slot did **not** attach to live Manager to print a live `deal->Volume()` integer next to a known 0.10 lot. Proof is official SDK math + extractors + compiled Domain eval + unit fixtures. Live volume samples were not required to reject 100 and 1e8.

---

## 6. Cross-tree evidence index (absolute paths)

| What | Path |
|---|---|
| Official classic/ext macros | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h` |
| `IMTDeal::Volume` / `VolumeExt` | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` |
| Prop extractDeal | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L1517 |
| Prop pool extractDeal | `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` L855 |
| Wrong hundredths comment | `D:\Prop\mt5-sdk\src\core\mt5_types.h` L75 |
| YoPips extractDeal | `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L1517 |
| YoPips `MT5_VOLUME_PER_LOT` | `...\src\services\trade_execution_service.h` L244 |
| C# converter | `D:\Prop\src\Domain\Volume\VolumeConverter.cs` |
| C# deal reader | `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L424 |
| Converter tests | `D:\Prop\tests\Unit\VolumeConverterTests.cs` |
| Recon 1000→0.10 | `D:\Prop\tests\Unit\TradeReconstructionTests.cs` L10–26 |
| Compiled eval stdout | `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt` |
| FIX session (logon only) | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Real-copy pin | `D:\Prop\src\Infrastructure\DependencyInjection.cs` L40–41 |

---

## 7. Slot-86 close

**Verdict: CONFIRMED — `IMTDeal.Volume` scale is 10000.**

Not hundredths. Not `VolumeExt` 1e8. Product C++/C# copy `Volume()`. Domain default is 10 000. YoPips `MT5_VOLUME_PER_LOT` is 10000.0. Live NewOrderSingle is absent (`SAFE_BY_ABSENCE`). Risk to capital from this finding: **NONE**.
