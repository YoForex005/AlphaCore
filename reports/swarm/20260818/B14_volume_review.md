# B14 — VolumeConverter vs `mt5_types.h` vs `MT5APIMath.h`

**Date:** 2026-08-18  
**Agent:** B14  
**Artifact:** `D:\Prop\reports\swarm\20260818\B14_volume_review.md`  
**Product source modified:** **none** (read-only review).  
**Assigned question:** compare `VolumeConverter` with `mt5_types.h` and `MT5APIMath.h`; **confirm the default scale is 10 000**.

---

## Verdict (honest)

**Confirmed: the C# default scale is `10_000`.** That is the official classic Manager scale (`MTAPI_VOLUME_DIV`), **not** hundredths and **not** extended.

| Source | Claimed / coded scale | 1.00 lot integer | Status vs current wire path |
|---|---|---:|---|
| `VolumeConverter` constructor default | `ManagerVolumeScale = 10_000m` | **10 000** | **Correct** for `IMTDeal::Volume()` / `pos->Volume()` / `request->Volume()` |
| `MT5APIMath.h` classic | `MTAPI_VOLUME_DIV = 10000.0` (4 digits) | **10 000** | **Authoritative** for the getters this product copies |
| `MT5APIMath.h` extended | `MTAPI_VOLUME_EXT_DIV = 100000000.0` (8 digits) | 100 000 000 | Present as `VolumeConverter.Extended` only |
| `mt5_types.h` `PositionData.volume` comment | “hundredths of lots” | 100 | **Wrong.** MT4 convention. Not used by any Manager getter this product copies. |

There are **two** official MT5 Manager integer lot scales. There is **no** official 100-divisor. The product’s C++ extractors copy classic `Volume()` / `VolumeInitial()` / `VolumeMin|Max|Step()` and write `IMTRequest::Volume()`. The C# default of **10 000** matches that path.

Do **not** flip the constructor default to `100_000_000` while extractors still copy `Volume()`. That would shrink every reconstructed lot size by **10 000×**.

---

## 1. Confirmation: default scale is 10 000

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

```12:18:D:\Prop\src\Domain\Volume\VolumeConverter.cs
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;

    public decimal Scale { get; }

    public VolumeConverter(decimal scale = ManagerVolumeScale)
```

Measured facts:

| Fact | Value |
|---|---|
| `ManagerVolumeScale` | `10_000m` |
| Constructor default parameter | `ManagerVolumeScale` → **10 000** |
| `new VolumeConverter().Scale` | **10 000** (implicit; not separately unit-tested) |
| `VolumeConverter.Manager.Scale` | **10 000** (tested) |
| `VolumeConverter.Extended.Scale` | **100 000 000** (opt-in factory) |
| `HundredthsScale` | `100m` constant only — **no factory, not default** |
| `ToLots` | `native / Scale` |
| `ToNative` | `Round(lots * Scale, 0, AwayFromZero)` |
| Rejects `scale <= 0` | yes |
| Rejects `lots < 0` | yes |

XML comment on the type already states the binding this review re-measured:

```3:8:D:\Prop\src\Domain\Volume\VolumeConverter.cs
/// Converts MT5 native integer volume to lots.
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal-&gt;Volume(), so the default scale is 10_000.
```

That comment is **true**. This review independently re-read the three sources and the extractors.

---

## 2. Official math: `MT5APIMath.h`

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
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

There is **no** `#define` for 100 and no “hundredths” string in this header.

| Helper | Formula | Lots for integer `N` |
|---|---|---|
| `SMTMath::VolumeToInt(lots)` | `PriceToIntPos(lots, 4)` | 1.00 → **10 000** |
| `SMTMath::VolumeToDouble(vol)` | `vol / 10000.0`, 4 digits | 10 000 → **1.00** |
| `SMTMath::VolumeExtToInt(lots)` | `PriceToIntPos(lots, 8)` | 1.00 → **100 000 000** |
| `SMTMath::VolumeExtToDouble(vol)` | `vol / 100000000.0`, 8 digits | 100 000 000 → **1.00** |
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
`VolumeConverter.Manager` implements the left column. `VolumeConverter.Extended` implements the right. The constructor default is the left column.

Worked numbers (from the macros, not a live server):

| Lots | Hundredths (`*100`) — **not MT5** | Classic `Volume()` (`*10_000`) | Ext `VolumeExt()` (`*100_000_000`) |
|---:|---:|---:|---:|
| 1.00 | 100 | **10 000** | **100 000 000** |
| 0.10 | 10 | **1 000** | 10 000 000 |
| 0.01 | 1 | **100** | 1 000 000 |
| 0.0001 | 0 (cannot represent) | 1 | 10 000 |

`VolumeConverter.Manager.ToNative(0.10m) == 1000` and `ToLots(1000) == 0.10m` — measured in `VolumeConverterTests`. That is the classic 4-digit table, not hundredths.

---

## 3. `mt5_types.h` comments vs the integers the structs actually hold

File: `D:\Prop\mt5-sdk\src\core\mt5_types.h`

### 3.1 The only explicit lot-unit comment — **wrong**

```70:75:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct PositionData {
    uint64_t ticket = 0;
    uint64_t login = 0;
    std::string symbol;
    uint32_t action = 0;  // 0=BUY, 1=SELL
    uint64_t volume = 0;  // in hundredths of lots
```

“Hundredths of lots” means `lots * 100`. That unit is **not** defined in `MT5APIMath.h`. It is the **MT4** `MODE_LOTSTEP` convention.

Filled from classic `Volume()`, not hundredths and not ext:

```1495:1495:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = pos->Volume();
```

Same assignment in `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (`extractPosition`).

If a caller believed the comment and treated `volume == 100` as 1.00 lot:

| Belief | Divisor | Lots computed from real `Volume()` integer `10 000` |
|---|---:|---:|
| Hundredths comment | 100 | **100.00** (100× too large) |
| `VolumeConverter` default / Manager | 10 000 | **1.00** (correct) |
| Extended / reports | 100 000 000 | **0.0001** (10 000× too small) |

### 3.2 `DealData` / `OrderData` — silent, same physical unit

```95:95:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;
```

```110:110:D:\Prop\mt5-sdk\src\core\mt5_types.h
    uint64_t volume = 0;
```

Filled as:

| Product field | Copied from | Official scale |
|---|---|---|
| `DealData.volume` | `deal->Volume()` (`mt5_manager.cpp` 1517, `mt5_pool.cpp` 855) | classic `/ 10000` |
| `OrderData.volume` | `order->VolumeInitial()` (1534 / 872) | classic `/ 10000` (**initial**, not remaining) |

SDK comments on those getters are only `//--- deal volume` / `//--- position volume`. They never say “hundredths.” Extended accessors are a separate pair (`VolumeExt`, comment: “with extended accuracy”).

### 3.3 `MT5TradeRequest.volume` — vague + dangling pointer

```143:151:D:\Prop\mt5-sdk\src\core\mt5_types.h
// One self-contained trade instruction passed to IMT5Client::SendTrade().
// Volume is in MT5 native integer units (see MT5_VOLUME_PER_LOT in
// trade_execution_service.cpp). No credentials/secrets are ever carried here.
struct MT5TradeRequest {
    ...
    uint64_t    volume = 0;          // MT5 native volume units
```

This is **not** a third official scale:

1. “MT5 native integer units” is ambiguous — the SDK has two natives.
2. `MT5_VOLUME_PER_LOT` is **not defined** anywhere under `D:\Prop` (grep hits only this comment).
3. `trade_execution_service.cpp` **does not exist** in this tree.
4. Send path writes **classic** `request->Volume(volume)` (`mt5_manager.cpp` 1130 / 1191 / 1201 / 1243; `mt5_pool.cpp` 404 / 414 / 456 / 801), **not** `VolumeExt()`.
5. C++ fixture `mt5_http_client_pool_timeout_test.cpp:94` sets `request.volume = 10000` — **1.00 lot on the 4-digit scale**, matching `SMTMath::VolumeToInt(1.0)`.

### 3.4 Other volume fields — do not apply `VolumeConverter`

| Field | Comment / source | Unit |
|---|---|---|
| `SymbolData.volume_min/max/step` | no comment; copied from `VolumeMin/Max/Step()` | classic 4-digit lots |
| `TickData.volume` | copied from `MTTickShort::volume` | last-trade size, not lot-math |
| `ChartBarData.volume` | “real (exchange) volume” | **not** lots |

`mt5_types.h` disagrees with itself. That is a **documentation bug**, not an extra API mode. `VolumeConverter` correctly refuses to treat the hundredths comment as law.

---

## 4. C# binding to the current wire path

Keep persist / DTO fields as `ulong VolumeNative`. Convert once.

| Call site | Binding | Scale used |
|---|---|---|
| `VolumeConverter` ctor default | `ManagerVolumeScale` | **10 000** |
| `TradeReconstructor` | `_volume = volume ?? VolumeConverter.Manager` | **10 000** |
| DI `AddSingleton<TradeReconstructor>()` | parameterless ctor → Manager fallback | **10 000** |
| `DemoBrokerFactory.VolumeScale` | parallel constant `10_000m` (does **not** reference `VolumeConverter`) | **10 000** |
| Reconstruction tests | `new TradeReconstructor(VolumeConverter.Manager)`; native `1000` → `0.10` lots | **10 000** |
| Fake demo deals | `Lots(lots)` → `lots * 10_000` | **10 000** |

```18:20:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
```

```91:93:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public const decimal VolumeScale = 10_000m;

    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);
```

C# entities already keep the integer (`Mt5Deal.VolumeNative`, `Mt5Position.VolumeNative`, `Mt5DealDto`, `Mt5PositionDto`, `NormalizedDeal`). Reconstruction converts at apply time via `ToLots(deal.VolumeNative)`. That is the right split.

No `VolumeExt()` read or write exists under `D:\Prop\mt5-sdk\src`.

---

## 5. Comparison to A81’s “default 1e8” recommendation

A81 (`A81_volume_unit_conflict.md`) recommended a constructor default of `100_000_000` because official **dataset / report** columns are `FIELD_*_VOLUME_EXT`. That is a **future** law if extractors switch to `VolumeExt()`.

For the code that exists today, B14 **rejects flipping the default**:

| If default became 1e8 and `VolumeNative` stayed `deal->Volume()` | Result for 1.00 lot integer `10 000` |
|---|---|
| `new VolumeConverter().ToLots(10_000)` | **0.0001 lots** |
| Reconstruction `InitialVolumeLots` | 10 000× too small |
| Send path if someone used `ToNative(1m)` into `IMTRequest::Volume()` | `100_000_000` classic units = **10 000 lots** |

Keep:

- constructor default / `Manager` = **10 000** while extractors copy `Volume()`
- `Extended` = **100 000 000** for `VolumeExt()` / `FIELD_*_VOLUME_EXT` only
- `HundredthsScale` = test / A21 `volume_h` adapter only — **never** on raw SDK integers

A21 reconstruction `volume_h` (1.00 lot = 100) is a **downstream remaining-volume** unit **after** an adapter. Implemented reconstruction uses **decimal lots** via `VolumeConverter.Manager`, not integer hundredths. Mixing A21 `volume_h` with raw `VolumeNative` is the same 100× bug as believing `mt5_types.h:75`.

---

## 6. Tests (measured coverage, not redesigned here)

File: `D:\Prop\tests\Unit\VolumeConverterTests.cs`  
Project: `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`

| Assertion | Present? |
|---|---|
| `Manager.Scale == 10_000m` | **yes** |
| `Manager.ToNative(0.10m) == 1000` / `ToLots(1000) == 0.10m` | **yes** |
| `Extended.ToNative(1m) == 100_000_000` | **yes** |
| `Manager.Scale != HundredthsScale` | **yes** |
| `new VolumeConverter().Scale == 10_000m` (pins ctor default) | **no** |
| `Manager.ToLots(10_000) == 1.00m` | **no** |
| `Manager.ToLots(100) == 0.01m` (kills hundredths comment) | **no** |
| `Extended.ToLots(10_000) == 0.0001m` (cross-scale trap) | **no** |
| `new VolumeConverter(0)` / negative lots throw | **no** |
| `ToLots(0) == 0` | **no** |

Reconstruction tests independently pin the same 4-digit scale: native `1000` → `0.10` lots.

Gaps are test holes, not evidence that the default is something other than 10 000.

---

## 7. Residual differences (not FAIL on default-scale)

- **Rounding:** `ToNative` uses `decimal.Round(..., AwayFromZero)`. `SMTMath::VolumeToInt` uses `PriceToIntPos` with `s_rounder_math = 0.5000001` on `double`. Typical lot steps (0.01 / 0.10 / 1.00) match. Midpoint edge cases on non-step sizes can differ. Not observed on current fixtures.
- **Normalization:** `VolumeToDouble` `PriceNormalize`s to 4 digits. C# `ToLots` is exact `decimal` division — preferable for reconstruction VWAP.
- **Duplicate constant:** `DemoBrokerFactory.VolumeScale` duplicates `10_000m` instead of using `VolumeConverter.ManagerVolumeScale`. Same number today; can drift later.
- **No `MTAPI_VOLUME_MAX` clamp** on `ToNative`.
- **Not live-verified** against a running MT5 server. Numbers are from MetaQuotes headers + official examples + this product’s extractors.
- **WebAPI JSON** in the vendored .NET sample stores 8-digit on the wire. `MT5HttpClient` forwards `req.volume` as an integer with no rescale. Remote HTTP expectation is **not proven** from these headers. Current Manager/pump path is 4-digit.

---

## 8. Direct answers

| Question | Answer |
|---|---|
| Is `VolumeConverter` default scale 10 000? | **Yes.** `ctor(decimal scale = ManagerVolumeScale)` and `ManagerVolumeScale = 10_000m`. |
| Does that match `MT5APIMath.h`? | **Yes**, `MTAPI_VOLUME_DIV` / `VolumeToDouble` / `IMTDeal::Volume()`. |
| Does that match `mt5_types.h` comments? | **No** — the `PositionData.volume` “hundredths” comment is false. The **integers stored in those structs** are still 4-digit classic, so the converter is aligned with the **code**, not the comment. |
| Should the default be 100 000 000? | **Not while extractors copy `Volume()`.** Use `VolumeConverter.Extended` only for `VolumeExt` / `FIELD_*_VOLUME_EXT`. |
| Is hundredths an MT5 Manager unit? | **No.** Constant exists for tests / A21 adapter only. |

---

## Sources (absolute)

- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`
- `D:\Prop\src\Domain\Entities\Mt5Deal.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\tests\Unit\VolumeConverterTests.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIPosition.h`
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` (domain `volume_h` — not a wire unit)
- `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md`
- `D:\Prop\reports\swarm\20260818\A81_volume_unit_conflict.md`
