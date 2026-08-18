# D14 — VolumeConverter default scale is 10 000

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D14_volume.md` |
| Agent | D14 (volume default reconfirm) |
| Date | 2026-08-18 |
| Assigned | Read `VolumeConverter.cs`. Confirm default **10000**. Write this file. |
| Product source modified | **No** |
| Test source modified | **No** |
| Method | Re-read `D:\Prop\src\Domain\Volume\VolumeConverter.cs` and current call sites. Independent of B14; same conclusion. |

---

## Verdict (honest)

**Confirmed: the default volume scale is `10_000` (ten thousand), not `100` and not `100_000_000`.**

| Pin | Measured value |
|---|---|
| `ManagerVolumeScale` | `10_000m` |
| Constructor default | `decimal scale = ManagerVolumeScale` → **10 000** |
| `VolumeConverter.Manager.Scale` | **10 000** (unit-tested) |
| `VolumeConverter.Extended.Scale` | **100 000 000** (opt-in only) |
| `HundredthsScale` | `100m` — constant only; **not default** |
| `TradeReconstructor` fallback | `volume ?? VolumeConverter.Manager` → **10 000** |
| Demo fake lots | `DemoBrokerFactory.VolumeScale = 10_000m` |
| Official Manager classic | `MTAPI_VOLUME_DIV = 10000.0` |

Worked identity used everywhere on the current wire path:

```
lots   = VolumeNative / 10_000
native = Round(lots * 10_000, AwayFromZero)
```

1.00 lot = **10 000**. 0.10 lot = **1 000**. 0.01 lot = **100**.

Do **not** flip the constructor default to `100_000_000` while extractors still copy `IMTDeal::Volume()`. That would shrink reconstructed lots by **10 000×**.

---

## 1. Source (re-read 2026-08-18)

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`

```3:35:D:\Prop\src\Domain\Volume\VolumeConverter.cs
/// Converts MT5 native integer volume to lots.
/// IMTDeal::Volume() / SMTMath::VolumeToDouble uses MTAPI_VOLUME_DIV = 10_000
/// (4 decimal places). IMTDeal::VolumeExt() uses 100_000_000.
/// The comment in mt5-sdk mt5_types.h ("hundredths of lots") is incorrect.
/// Existing mt5_manager.cpp copies deal-&gt;Volume(), so the default scale is 10_000.
/// </summary>
public sealed class VolumeConverter
{
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

    public ulong ToNative(decimal lots)
    {
        if (lots < 0)
            throw new ArgumentOutOfRangeException(nameof(lots));
        return (ulong)decimal.Round(lots * Scale, 0, MidpointRounding.AwayFromZero);
    }

    public static VolumeConverter Manager => new(ManagerVolumeScale);
    public static VolumeConverter Extended => new(ExtendedVolumeScale);
}
```

Facts from that file only:

| Item | Code | Meaning |
|---|---|---|
| Classic Manager scale | `public const decimal ManagerVolumeScale = 10_000m` | **10 000** |
| Ctor default parameter | `VolumeConverter(decimal scale = ManagerVolumeScale)` | `new VolumeConverter().Scale == 10_000m` |
| Manager factory | `new(ManagerVolumeScale)` | same 10 000 |
| Extended factory | `new(ExtendedVolumeScale)` | 100 000 000 — **must be requested** |
| Hundredths | `HundredthsScale = 100m` | **no factory**; never assigned as default |
| `ToLots` | `native / Scale` | 10 000 native → 1.00 lot at default |
| `ToNative` | `Round(lots * Scale, 0, AwayFromZero)` | 1.00 lot → 10 000 native at default |

There is no other constructor. There is no config override inside this type.

---

## 2. Tests pin the same number

File: `D:\Prop\tests\Unit\VolumeConverterTests.cs`

```8:28:D:\Prop\tests\Unit\VolumeConverterTests.cs
    [Fact]
    public void Manager_scale_maps_0_10_lots_to_1000_native()
    {
        var c = VolumeConverter.Manager;
        c.Scale.Should().Be(10_000m);
        c.ToNative(0.10m).Should().Be(1000);
        c.ToLots(1000).Should().Be(0.10m);
    }
    ...
    [Fact]
    public void Hundredths_comment_is_not_the_default()
    {
        VolumeConverter.Manager.Scale.Should().NotBe(VolumeConverter.HundredthsScale);
    }
```

Reconstruction independently uses the same 4-digit scale. `TradeReconstructionTests` constructs `new(VolumeConverter.Manager)` and asserts native `1000` → `0.10` lots:

```10:26:D:\Prop\tests\Unit\TradeReconstructionTests.cs
    private readonly TradeReconstructor _r = new(VolumeConverter.Manager);
    ...
            Deal(1, 10, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, t: 1),
            Deal(2, 10, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100, t: 2)
    ...
        trades[0].InitialVolumeLots.Should().Be(0.10m);
```

Gap (not a counter-example): no test asserts `new VolumeConverter().Scale == 10_000m` on the parameterless constructor. The default parameter is still `ManagerVolumeScale` in source.

---

## 3. Call sites that inherit the default

```18:21:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
        _symbols = symbols ?? new SymbolNormalizer();
```

```37:38:D:\Prop\src\Infrastructure\DependencyInjection.cs
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
```

`AddSingleton<TradeReconstructor>()` uses the parameterless constructor → Manager fallback → **10 000**.

```91:93:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public const decimal VolumeScale = 10_000m;

    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);
```

Duplicate constant (does not reference `VolumeConverter.ManagerVolumeScale`) but same number.

Docs already state the same pin:

- `D:\Prop\docs\architecture.md` — “Volume default scale = 10_000 (`IMTDeal.Volume()`)”
- `D:\Prop\docs\trade-reconstruction.md` — “Volume: `native / 10_000` unless configured otherwise (`IMTDeal.Volume()` / `MTAPI_VOLUME_DIV`).”

---

## 4. Official SDK (why 10 000 is the right default)

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`

```12:19:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h
#define MTAPI_VOLUME_DIV        (10000.0)
#define MTAPI_VOLUME_DIGITS     (4)
#define MTAPI_VOLUME_MAX        ((uint64_t)10000000000)
//+------------------------------------------------------------------+
//| Volume with extended accuracy constants                          |
//+------------------------------------------------------------------+
#define MTAPI_VOLUME_EXT_DIV    (100000000.0)
#define MTAPI_VOLUME_EXT_DIGITS (8)
```

`SMTMath::VolumeToDouble` divides by `MTAPI_VOLUME_DIV` (10 000). That is the helper for `IMTDeal::Volume()`, which `mt5_manager.cpp` copies.

| Scale | 1.00 lot integer | When to use |
|---|---:|---|
| Hundredths (`*100`) | 100 | **Never** on Manager integers. MT4 comment in `mt5_types.h` is wrong. |
| Classic `Volume()` (`*10_000`) | **10 000** | **Default.** Current extractors + `VolumeConverter` ctor. |
| Ext `VolumeExt()` (`*100_000_000`) | 100 000 000 | `VolumeConverter.Extended` only. |

C++ fixture `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp:94` sets `request.volume = 10000` — 1.00 lot on the 4-digit scale.

---

## 5. Direct answers

| Question | Answer |
|---|---|
| Is the default 10000? | **Yes.** `ManagerVolumeScale = 10_000m`; ctor default is that constant. |
| Is hundredths (`100`) the default? | **No.** Tested: `Manager.Scale != HundredthsScale`. |
| Is extended (`100_000_000`) the default? | **No.** Opt-in via `VolumeConverter.Extended`. |
| Should the default change? | **Not while extractors copy `Volume()`.** Keep 10 000. |

---

## Sources (absolute)

- `D:\Prop\src\Domain\Volume\VolumeConverter.cs`
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\tests\Unit\VolumeConverterTests.cs`
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\docs\trade-reconstruction.md`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp`
- Prior independent reviews (same pin, not re-used as evidence): `A38_mt5_volume_units.md`, `A81_volume_unit_conflict.md`, `B14_volume_review.md`
