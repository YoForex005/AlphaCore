# D92 — Vote: A81 default 1e8 vs B14 10 000

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D92_volume_vote.md` |
| Agent | D92 (volume default vote) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:46:01+05:30 |
| Assigned | A81 default 1e8 vs B14 10k. Who is right? Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| Method | Re-read A81 / B14 / D14, re-read current converter + extractors + send path + tests, compile an isolated Domain eval. Independent of B14; same conclusion. |

Eval (reports only, not product): `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\` → `stdout.txt`.

---

## 0. Verdict (honest)

**B14 is right.** The `VolumeConverter` constructor default must stay **`10_000`** (`MTAPI_VOLUME_DIV` / `IMTDeal::Volume()`).

A81 is **right** that official **extended** volume is `1e8` (`MTAPI_VOLUME_EXT_DIV` / `VolumeExt()` / `FIELD_*_VOLUME_EXT`). A81 is **wrong** to recommend that 1e8 become the **constructor default** while this product still copies classic `Volume()`.

The question is not “does 1e8 exist?” It does. The question is “what default should `new VolumeConverter()` use for *this* tree, *today*?” That default must match the integers that actually flow. Those integers are **4-digit classic**, not 8-digit ext.

| Claim | Agent | Status |
|---|---|---|
| Official ext scale is `100_000_000` | A81 (and B14, D14) | **True.** Not in dispute. |
| Official classic scale is `10_000` | A81 (and B14, D14) | **True.** Not in dispute. |
| `mt5_types.h` “hundredths” comment is wrong | A81 (and B14, D14) | **True.** Not in dispute. |
| **Ctor default should be `1e8`** | **A81 §7.1** | **False for this product today.** |
| **Ctor default should be `10_000`** | **B14 §5 / D14** | **True.** Measured and required. |
| Flip default to 1e8 while extractors copy `Volume()` | A81 recommended; B14 rejected | **10 000× sizing bug.** B14 wins. |

Independent compile of current Domain (`2026-08-18T13:46:01+05:30`):

```text
ctor_default_Scale=10000
Manager.Scale=10000
Extended.Scale=100000000
default_eq_Manager=True
default_eq_Extended=False
Manager.ToLots(10000)=1
default.ToLots(10000)=1
Extended.ToLots(10000)=0.0001
default.ToNative(1)=10000
Extended.ToNative(1)=100000000
ratio_ext_div_mgr=10000
blast_if_A81_default_on_classic_10000=0.0001
blast_if_B14_default_on_classic_10000=1
```

`1.00` lot on the current wire is integer **`10 000`**. Feeding that integer to A81’s recommended default yields **`0.0001` lots**. That is not a loud fail. Reconstruction `FlatEpsilon` is `0.0000001m`, so `0.0001` is accepted as a real trade **10 000× too small**.

---

## 1. What each agent actually said (no strawman)

### 1.1 A81 — `A81_volume_unit_conflict.md`

SHA-256 `D7BE6227DC4530D50C75235A32089AC9D9B7BCC4CFCF5C1DA3A2189A314BA74D`.

A81’s **facts** are good:

- Two official Manager integer lot scales in `MT5APIMath.h`. No official `100` divisor.
- Product extractors copy `pos->Volume()` / `deal->Volume()` / `order->VolumeInitial()` and send `request->Volume(...)`.
- Official Capital reports divide by `100000000.0` **because they bind `FIELD_DEAL_VOLUME_EXT`**, not because classic `Volume()` is 8-digit.
- Changing the default without switching extractors to `VolumeExt()` is a **10 000×** sizing bug.

A81’s **recommendation** is the conflict:

> a C# `VolumeConverter` with a configurable scale whose **constructor default is `100_000_000`**

Reasons A81 gave (verbatim sense of §7.1):

1. Official **new** accuracy is 8 digits.
2. Official **dataset / report** path is `FIELD_*_VOLUME_EXT`.
3. New domain code should speak ext unless the integer’s source is proven to be `Volume()`.
4. Default `1e8` makes feeding classic `10 000` look like `0.0001` lots “instead of silently looking almost right.”

That last reason is inverted. `0.0001` looks like a legal micro-lot. It does **not** fail tests unless a test explicitly pins `new VolumeConverter().Scale == 100_000_000m` (none does) or pins reconstructed `0.10` lots from native `1000` (those tests bind `VolumeConverter.Manager`, so they would stay green while any default-ctor caller silently undersizes).

A81 also wrote the binding table that **contradicts** its own default: current extractors **must** use `VolumeConverter.Manager` until switched to `VolumeExt()`. So A81 already knew the live integers are 4-digit. The 1e8 default is a future-API preference, not a description of this tree.

### 1.2 B14 — `B14_volume_review.md`

SHA-256 `90288E905D3C046F506DD2977EB63438CA7B2307DD3911A84B8FF27B36883202`.

B14 agrees with A81 on the two official scales and the wrong hundredths comment. B14 **rejects the flip**:

> For the code that exists today, B14 **rejects flipping the default**.
> Do **not** flip the constructor default to `100_000_000` while extractors still copy `Volume()`. That would shrink every reconstructed lot size by **10 000×**.

B14’s blast table (re-measured this vote — same numbers):

| If default became 1e8 and `VolumeNative` stayed `deal->Volume()` | Result for 1.00 lot integer `10 000` |
|---|---|
| `new VolumeConverter().ToLots(10_000)` | **0.0001 lots** |
| Reconstruction `InitialVolumeLots` via default ctor | 10 000× too small |
| Send path `ToNative(1m)` into `IMTRequest::Volume()` | `100_000_000` classic units = **10 000 lots** |

### 1.3 D14 — already reconfirmed 10 000

`D14_volume.md` (SHA-256 `D48373094448052EB325535FAF9777C359516BF6156CE59E48D96B60CA385AD3`) independently re-read the same converter and call sites. Same pin. This D92 vote is not a rubber-stamp of D14: extractors, send path, compiled default, and A81’s own blast-radius paragraph were re-read from disk.

---

## 2. Independent re-measure (2026-08-18)

### 2.1 C# converter — default is 10 000

File: `D:\Prop\src\Domain\Volume\VolumeConverter.cs`  
1318 bytes, SHA-256 `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2`

```12:18:D:\Prop\src\Domain\Volume\VolumeConverter.cs
    public const decimal ManagerVolumeScale = 10_000m;
    public const decimal ExtendedVolumeScale = 100_000_000m;
    public const decimal HundredthsScale = 100m;

    public decimal Scale { get; }

    public VolumeConverter(decimal scale = ManagerVolumeScale)
```

| Pin | Measured |
|---|---|
| `ManagerVolumeScale` | `10_000m` |
| Ctor default parameter | `ManagerVolumeScale` → **10 000** |
| Compiled `new VolumeConverter().Scale` | **10000** |
| `VolumeConverter.Extended.Scale` | **100000000** (opt-in only) |
| `HundredthsScale` | `100m` — constant only, no factory, not default |
| Product `VolumeExt()` call sites under `D:\Prop\src` | **1 comment** in this file; **zero** reads/writes |
| Product `VolumeExt()` call sites under `D:\Prop\mt5-sdk\src` | **zero** |

`VolumeConverter.Extended` exists and is correct **for `VolumeExt()` integers**. That does not make it the default.

### 2.2 What this product actually copies and sends

No `VolumeExt` token exists under `D:\Prop\mt5-sdk\src`. Extractors copy classic getters unchanged:

```1495:1495:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = pos->Volume();
```

```1517:1517:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = deal->Volume();
```

```1534:1534:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    d.volume = order->VolumeInitial();
```

Same three assignments in `mt5_pool.cpp` (`extractPosition` 833, `extractDeal` 855, `extractOrder` 872).

Send path writes **classic** `Volume()`, not `VolumeExt()`:

```1130:1130:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    request->Volume(volume);
```

Also `mt5_manager.cpp` 1191 / 1201 / 1243 and `mt5_pool.cpp` 404 / 414 / 456 / 801.

C++ fixture `mt5_http_client_pool_timeout_test.cpp:94` sets `request.volume = 10000`. That is `SMTMath::VolumeToInt(1.0)` — **1.00 lot on the 4-digit scale**. It is not hundredths (`100`) and not ext (`100000000`).

`PositionData.volume` still comments “hundredths of lots” (`mt5_types.h:75`). The **comment** is A81-correct-as-bug. The **integer stored there** is still `pos->Volume()` = classic 10 000. Converter law follows the assignment, not the comment.

### 2.3 Official math — both scales exist; default follows the getter in use

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`  
SHA-256 `645DF20050F90399B2FD530119880FE1B92B0C6DBE553D055B5CFCE7A6CB3285`

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

`SMTMath::VolumeToDouble` divides by `MTAPI_VOLUME_DIV` (10 000) — helper for `Volume()`.  
`SMTMath::VolumeExtToDouble` divides by `MTAPI_VOLUME_EXT_DIV` (1e8) — helper for `VolumeExt()`.  
`VolumeExtFromVolume` multiplies by **10 000**. Ratio of the two official scales is exactly 10 000.

Official Manager sample (`SimpleManager.cpp`) places 1.00 lot with `SMTMath::VolumeToInt(1.0)` → **10 000**.  
Official `ExecutionType.cpp` divides `deal->Volume()/10000.0`.  
Official Capital `DealCache.cpp` divides by `100000000.0` **after** binding `FIELD_DEAL_VOLUME_EXT`. That is A81’s report evidence. It is **not** evidence that `deal->Volume()` is 8-digit.

### 2.4 Every live C# caller already binds Manager / 10 000

| Call site | Binding | Scale |
|---|---|---|
| `VolumeConverter` ctor default | `scale = ManagerVolumeScale` | **10 000** (compiled) |
| `TradeReconstructor` | `_volume = volume ?? VolumeConverter.Manager` | **10 000** |
| DI `AddSingleton<TradeReconstructor>()` | parameterless ctor → Manager fallback | **10 000** |
| `DemoBrokerFactory.VolumeScale` | parallel `10_000m` | **10 000** |
| Reconstruction tests | `new TradeReconstructor(VolumeConverter.Manager)`; native `1000` → `0.10` lots | **10 000** |
| `VolumeConverterTests` | `Manager.Scale == 10_000m`; `ToNative(0.10m) == 1000` | **10 000** |
| Docs `architecture.md` | “Volume default scale = 10_000 (`IMTDeal.Volume()`)” | **10 000** |
| Docs `risk.md` / env pin | `MT5_VOLUME_SCALE=10000` | **10 000** |

There is **no** `new VolumeConverter()` in product C# (grep). Callers use the `Manager` factory or the recon fallback. That does **not** make a 1e8 default safe: the next `new VolumeConverter()` on `VolumeNative` from `extractDeal` would silently emit `0.0001` lots per real 1.00 lot.

Reconstruction `ToLots` is applied at `TradeReconstructor.cs:89`. SHA-256 of that file: `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B` (12 768 bytes). Fake SHA-256: `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4`.

### 2.5 Worked numbers for the vote

| Lots | Hundredths (`*100`) — **not MT5** | Classic `Volume()` / **correct default** | Ext `VolumeExt()` / A81 default |
|---:|---:|---:|---:|
| 1.00 | 100 | **10 000** | 100 000 000 |
| 0.10 | 10 | **1 000** | 10 000 000 |
| 0.01 | 1 | **100** | 1 000 000 |

Mis-scale of a real extractor integer `10 000` (1.00 lot):

| Belief | Divisor | Computed lots | Error |
|---|---:|---:|---|
| Hundredths comment | 100 | 100.00 | 100× too large |
| **B14 / current default** | **10 000** | **1.00** | **correct** |
| **A81 recommended default** | **100 000 000** | **0.0001** | **10 000× too small** |

Mis-scale if a sender uses `ToNative(1.00)` into `IMTRequest::Volume()` (classic setter):

| Integer sent | Server sees |
|---:|---|
| 100 (comment) | 0.01 lot |
| **10 000 (B14 / current)** | **1.00 lot** |
| **100 000 000 (A81 default)** | **10 000 lots** |

---

## 3. Why A81’s “new code should speak ext” does not win

1. **This product does not speak ext.** Zero `VolumeExt()` in `mt5-sdk\src`. Zero `VolumeExt` in `src\` except a comment. The C# persist field is `VolumeNative` filled from those classic integers (fake path: `Lots(lots) = lots * 10_000`).
2. **Dataset/report 1e8 is a different API.** `FIELD_DEAL_VOLUME_EXT` is not what `extractDeal` copies. Citing Capital reports to pick the Domain ctor default is mixing two MetaQuotes entry points.
3. **A fail-closed default of 1e8 is not fail-closed.** `0.0001` lots passes `lots > 0` and `FlatEpsilon`. Scores, first-3, and shadow size would all look internally consistent and all be 10 000× wrong.
4. **Send-path blast is worse than recon blast.** `ToNative(1m)` under 1e8 writes `100_000_000` into `request->Volume()`. Official max classic is `MTAPI_VOLUME_MAX = 10_000_000_000` units, so that integer is accepted as **10 000 lots**.
5. **A21 `volume_h` (1.00 lot = 100) is a third, downstream unit.** Implemented recon uses **decimal lots** via `VolumeConverter.Manager`, not integer hundredths. A21 does not rehabilitate either the `mt5_types.h` comment or an 1e8 default.
6. **Docs already pin 10 000.** `architecture.md` line 23. Env examples pin `MT5_VOLUME_SCALE=10000`. `docs/trade-reconstruction.md` still says “hundredths of lots where 1 lot = 100 oz” in the same paragraph as `MT5_VOLUME_SCALE` default 10000 — that sentence mixes **contract size** (100 oz/lot) with a wrong “hundredths” word. The **number** in that doc is still 10000. Another comment bug, not an 1e8 vote.

A81’s 1e8 default becomes right **only after** extractors, JSON HTTP volume, fake `Lots()`, reconstruction tests, and `IMTRequest::Volume()` are all switched to `VolumeExt()` / `VolumeExtFromVolume` together, as one reviewed change. That change has not happened. Until it does, `Extended` stays opt-in.

---

## 4. Binding rule (do not implement here)

| Integer source | Factory | Scale |
|---|---|---|
| `Volume()` / `VolumeInitial()` / `VolumeMin|Max|Step()` / current extractors / `request.volume = 10000` fixture / `VolumeNative` today | `VolumeConverter.Manager` (**ctor default**) | **10 000** |
| `VolumeExt()` / `VolumeClosedExt()` / `FIELD_*_VOLUME_EXT` / `TYPE_VOLUME_EXT` | `VolumeConverter.Extended` | 100 000 000 |
| A21 `volume_h` after a successful adapter | `HundredthsScale` only | 100 |
| Chart / tick / exchange `volume` | **do not convert** | n/a |

Do **not** flip `decimal scale = ManagerVolumeScale` to `100_000_000m`.  
Do **not** treat A81’s recommended shape as a pending code change.  
Do **not** treat hundredths as an MT5 Manager wire unit.

Test hole (not a counter-vote): no unit test asserts `new VolumeConverter().Scale == 10_000m` on the parameterless constructor. `Manager.Scale` is tested. The compiled default was measured this vote as **10000**.

---

## 5. Residual uncertainty (stated)

- **Not live-verified** against a running MT5 server. Numbers are from MetaQuotes headers, official examples, this product’s extractors, and a compiled Domain eval.
- **WebAPI JSON** in the vendored .NET sample stores 8-digit on the wire. `MT5HttpClient` forwards `req.volume` as an integer with no rescale. Remote HTTP expectation is **not proven** from these headers. Current Manager/pump path is 4-digit. If a future HTTP service is ext-first, convert **at the HTTP adapter**, do not flip the Domain default first.
- **Rounding:** C# `ToNative` uses `decimal.Round(..., AwayFromZero)`. `SMTMath::VolumeToInt` uses `PriceToIntPos` on `double`. Typical lot steps match. Not material to the 1e8 vs 10k vote.
- One official NFA sample (cited in A38) mixes `VolumeExtToSize(deal->Volume(), …)`. That is an SDK-sample inconsistency, not a third unit, and not a reason to default to 1e8.

---

## 6. Direct answers

| Question | Answer |
|---|---|
| A81 default 1e8 vs B14 10k — who is right? | **B14.** |
| Is 1e8 a real official scale? | **Yes** — `VolumeExt` / `MTAPI_VOLUME_EXT_DIV` only. |
| Is 1e8 the right **default** for this product today? | **No.** |
| What is the compiled ctor default right now? | **`10000`.** |
| Would flipping the default be a comment cleanup? | **No.** It is a **10 000×** recon undersize and a **10 000×** send oversize. |
| Should product source change in this task? | **No.** Default is already the winning number. |

---

## Sources (absolute)

- `D:\Prop\src\Domain\Volume\VolumeConverter.cs` (SHA-256 `C6C5E3FD26343532EF047F46D7728A5FED7027B82312A225B9CC3AA881EAC0A2`)
- `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (SHA-256 `AEA3930B98CCD8B37F59ED5E339FE839AA78718B89EB845C157635D6F167534B`)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (SHA-256 `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4`)
- `D:\Prop\tests\Unit\VolumeConverterTests.cs` (SHA-256 `DD04782A06319BB978C2E908C5C1FDEB6EBDB85E8525399FCBABBCE5CA94BFE5`)
- `D:\Prop\tests\Unit\TradeReconstructionTests.cs`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\docs\trade-reconstruction.md`
- `D:\Prop\docs\risk.md`
- `D:\Prop\mt5-sdk\src\core\mt5_types.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_http_client_pool_timeout_test.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIMath.h`
- `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md`
- `D:\Prop\reports\swarm\20260818\A38_mt5_volume_units.md`
- `D:\Prop\reports\swarm\20260818\A81_volume_unit_conflict.md`
- `D:\Prop\reports\swarm\20260818\B14_volume_review.md`
- `D:\Prop\reports\swarm\20260818\B34_recon_fixtures.md`
- `D:\Prop\reports\swarm\20260818\D14_volume.md`
- `D:\Prop\reports\swarm\20260818\_tmp_d92_vote\stdout.txt`
