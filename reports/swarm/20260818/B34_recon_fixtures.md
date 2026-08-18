# B34 — Eight concrete reconstruction deal fixtures (native scale 10 000)

| Field | Value |
|---|---|
| Agent | B34 (reconstruction unit-test fixtures) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B34_recon_fixtures.md` |
| Product source modified | **none** (report only) |
| SUT | `TradeReconstructor` + `NormalizedDeal` + `VolumeConverter.Manager` |
| Binding volume law | `lots = 0.10` ≡ `VolumeNative = 1000` at scale `10_000` |

These eight fixtures are the paste-ready unit-test pack for Architecture §60 reconstruction cases (full close, short close, scale-in, partial close, reversal, first-3 XAUUSD, averaging-down, filters/aliases). They speak **Manager classic integers** (`IMTDeal::Volume()`, `MTAPI_VOLUME_DIV = 10000`), which is what `Mt5Deal.VolumeNative` / `NormalizedDeal.VolumeNative` store today.

They are **not** A21 `volume_h` hundredths. Mixing the two is a 100× bug (`0.10` lot is `1000` native, **not** `10`).

---

## 0. Volume pin (assert this in every test class)

Measured from product + SDK (do not re-litigate here; see `B14_volume_review.md`, `A37_mt5_deal_enums.md`, `A38_mt5_volume_units.md`):

| Lots | Hundredths (`volume_h`, A21 only) | Classic `Volume()` / `VolumeNative` | Ext `VolumeExt()` |
|---:|---:|---:|---:|
| 0.01 | 1 | **100** | 1 000 000 |
| **0.10** | 10 | **1000** | 10 000 000 |
| 0.20 | 20 | **2000** | 20 000 000 |
| 1.00 | 100 | **10 000** | 100 000 000 |

```csharp
VolumeConverter.Manager.Scale.Should().Be(10_000m);
VolumeConverter.Manager.ToNative(0.10m).Should().Be(1000UL);
VolumeConverter.Manager.ToLots(1000).Should().Be(0.10m);
VolumeConverter.Manager.ToNative(0.20m).Should().Be(2000UL);
```

Brick used by every fixture:

```text
LOT_BRICK          = 0.10m
NATIVE_BRICK       = 1000UL          // 0.10 * 10_000
NATIVE_TWO_BRICKS  = 2000UL          // 0.20 lots, only when a single deal must carry two bricks
BROKER             = "ACHIEVER"
DEFAULT_SYMBOL     = "XAUUSDm"       // alias → canonical XAUUSD
TIME(ms)           = DateTimeOffset.FromUnixTimeMilliseconds(timeMsc)
```

`TradeReconstructor` converts with `VolumeConverter.Manager.ToLots(deal.VolumeNative)` (`native / 10_000m`). VWAP is `Σ(price * lots) / Σ(lots)` in `decimal`. For these bricks that is exact (0.10 and 0.20 are terminating decimals).

PnL is **broker-supplied** (`deal.Profit` / `Commission` / `Swap`). The engine must **not** recompute from price × contract. Informational `$10 / oz × 10 oz per 0.10 lot` notes are comments only.

---

## 1. Shared `NormalizedDeal` factory (unit project only)

Copy into `tests/Unit` (or `_Support/B34Deals.cs`) when a coder implements the tests. **Do not add this file from this agent.**

```csharp
internal static class B34
{
    public const string Broker = "ACHIEVER";
    public const decimal BrickLots = 0.10m;
    public const ulong BrickNative = 1000;
    public static readonly DateTimeOffset T0 = DateTimeOffset.UnixEpoch;

    public static NormalizedDeal Deal(
        long ticket,
        long position,
        DealAction action,
        DealEntry entry,
        ulong native,
        decimal price,
        decimal profit,
        long timeMsc,
        long login = 1,
        long? order = null,
        decimal commission = 0,
        decimal swap = 0,
        string symbol = "XAUUSDm",
        decimal? sl = null,
        decimal? tp = null,
        string? comment = null) => new()
    {
        BrokerId = Broker,
        Login = login,
        DealTicket = ticket,
        OrderTicket = order ?? ticket + 10_000,
        PositionId = position,
        SourceSymbol = symbol,
        Action = action,
        Entry = entry,
        VolumeNative = native,
        Price = price,
        Profit = profit,
        Commission = commission,
        Swap = swap,
        Time = DateTimeOffset.FromUnixTimeMilliseconds(timeMsc),
        StopLoss = sl,
        TakeProfit = tp,
        Comment = comment
    };
}
```

Sort law (already in `TradeReconstructor.Reconstruct`): `Time` then `DealTicket`. Fixtures are listed in apply order; F08 includes a later-listed earlier `timeMsc` to prove sort.

Result id law (current implementation):

```text
Id = $"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}"
```

`lifecycle_seq` is **not** on `ReconstructedTradeResult` today. After a flatten + reopen on the same `position_id`, the new trade is distinguished by `OpenedAt` in `Id`.

---

## 2. Coverage map (8 fixtures ↔ §60 ↔ current tests)

| Id | Name | §60 case | Login | Position | Native bricks | Current `TradeReconstructionTests` |
|---|---|---|---:|---:|---|---|
| **F01** | Long full close | full close / reconstruct | 1 | 9101 | 1000 / 1000 | Partial: `Reconstructs_simple_round_trip` (no comm/swap/SL) |
| **F02** | Short full close | full close (short) | 1 | 9102 | 1000 / 1000 | **Missing** |
| **F03** | Scale-in, not averaged | scale-in + VWAP | 1 | 9103 | 1000 + 1000 + 2000 | Mixed into `Scale_in_and_partial_close` |
| **F04** | Partial then flatten | partial close | 1 | 9104 | 2000 / 1000 / 1000 | Mixed into same test |
| **F05** | `ENTRY_INOUT` reversal | position reversal | 1 | 9105 | 1000 / 2000 | `Reverse_inout_closes_then_opens_opposite` (no money assert) |
| **F06** | First 3 completed XAU | first-3 / early score | 2006 | 9110–9112 | 6 × 1000 | `First_three_completed_xau_unlocks_early_score` |
| **F07** | Average-down long | averaging-down | 1 | 9107 | 1000 + 1000 + 2000 | Mixed into scale/partial test |
| **F08** | Filters + aliases + open | XAU mapping / skip noise | 2008 | mixed | 1000 bricks + zeros | Only `Ignores_balance_deals` |

Tickets 10101–10811 do not collide with the existing helper (`ticket` 1–11). All eight may live in one class.

Concatenation: F01+F02+F03+F04+F05+F07 on `login=1` is a legal multi-book tape (distinct `position_id`s). F06 and F08 use other logins so `Reconstruct("ACHIEVER", 1, …)` never sees them.

---

## 3. F01 — Long full close (one completed XAUUSD)

**Purpose:** baseline lifecycle. `0.10` in, `0.10` out, one trade, not early-score. Pins SL/TP carry, comm+swap net, alias `XAUUSDm` → `XAUUSD`.

### Deals

| ticket | time_msc | order | pos | symbol | action | entry | native | lots | price | profit | comm | swap | sl | tp | comment |
|---:|---:|---:|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 10101 | 1000 | 20101 | 9101 | XAUUSDm | Buy=0 | In=0 | **1000** | 0.10 | 2320.00 | 0 | -0.60 | 0 | 2310 | 2340 | B34-F01-IN |
| 10102 | 2000 | 20102 | 9101 | XAUUSDm | Sell=1 | Out=1 | **1000** | 0.10 | 2330.00 | 100.00 | -0.60 | -0.20 | — | — | B34-F01-OUT |

Informational (engine does not compute this): `$10/oz × 10 oz × $10 = $100`.

### C#

```csharp
var deals = new[]
{
    B34.Deal(10101, 9101, DealAction.Buy,  DealEntry.In,  1000, 2320m,   0m, 1000, sl: 2310m, tp: 2340m, commission: -0.60m, comment: "B34-F01-IN"),
    B34.Deal(10102, 9101, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100m, 2000, commission: -0.60m, swap: -0.20m, comment: "B34-F01-OUT"),
};
```

### Expected `Reconstruct("ACHIEVER", 1, deals)` — one row

| Field | Value |
|---|---|
| `Id` | `ACHIEVER:1:9101:1000` |
| `Direction` | `Long` |
| `CanonicalSymbol` / `SourceSymbol` | `XAUUSD` / `XAUUSDm` |
| `IsXauUsd` | `true` |
| `OpenedAt` / `ClosedAt` | `ms=1000` / `ms=2000` |
| `EntryVwap` / `ExitVwap` | `2320.00` / `2330.00` |
| `InitialVolumeLots` | **0.10** |
| `MaxVolumeLots` | **0.10** |
| `ClosedVolumeLots` | **0.10** |
| `RemainingVolumeLots` | **0** |
| `GrossRealizedPnl` | `100.00` |
| `Commission` | `-1.20` |
| `Swap` | `-0.20` |
| `Fees` | `0` |
| `NetRealizedPnl` | **`98.80`** |
| `DealCount` / `OrderCount` | 2 / 2 |
| `DealTickets` | `[10101, 10102]` |
| `InitialSl` / `InitialTp` | `2310` / `2340` |
| `FinalSl` / `FinalTp` | `2310` / `2340` (OUT has no SL/TP; impl keeps last present) |
| `WasScaledIn` / `WasPartialClose` / `WasAveragedDown` | F / F / F |
| `Completed` | **T** |

```text
Apply: 10101 IN +0.10 rem=0.10 open
       10102 OUT -0.10 rem=0    complete
CountCompletedXauUsdTrades = 1
IsEarlyScoreEligible       = false
```

Suggested fact: `F01_Long_full_close_0_10_lots_native_1000`.

---

## 4. F02 — Short full close (one completed XAUUSD)

**Purpose:** opening `DealAction.Sell` + `ENTRY_IN` is **Short**. Close is `Buy` + `ENTRY_OUT`. Same `0.10` brick. No current dedicated test.

### Deals

| ticket | time_msc | order | pos | symbol | action | entry | native | lots | price | profit | comm | swap | sl | tp | comment |
|---:|---:|---:|---:|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 10201 | 1000 | 20201 | 9102 | XAUUSDm | Sell=1 | In=0 | **1000** | 0.10 | 2330.00 | 0 | -0.60 | 0 | 2340 | 2310 | B34-F02-IN |
| 10202 | 2000 | 20202 | 9102 | XAUUSDm | Buy=0 | Out=1 | **1000** | 0.10 | 2320.00 | 100.00 | -0.60 | -0.20 | — | — | B34-F02-OUT |

### C#

```csharp
var deals = new[]
{
    B34.Deal(10201, 9102, DealAction.Sell, DealEntry.In,  1000, 2330m,   0m, 1000, sl: 2340m, tp: 2310m, commission: -0.60m, comment: "B34-F02-IN"),
    B34.Deal(10202, 9102, DealAction.Buy,  DealEntry.Out, 1000, 2320m, 100m, 2000, commission: -0.60m, swap: -0.20m, comment: "B34-F02-OUT"),
};
```

### Expected — one row

| Field | Value |
|---|---|
| `Id` | `ACHIEVER:1:9102:1000` |
| `Direction` | **`Short`** |
| `EntryVwap` / `ExitVwap` | `2330.00` / `2320.00` |
| Lots init/max/closed/rem | **0.10 / 0.10 / 0.10 / 0** |
| `NetRealizedPnl` | **`98.80`** (`100 - 1.20 - 0.20`) |
| Flags | scaled F, partial F, avg F, `Completed` T |
| `InitialSl` / `InitialTp` | `2340` / `2310` |

```text
Apply: 10201 IN short +0.10 rem=0.10
       10202 OUT      -0.10 rem=0    complete
CountCompletedXauUsdTrades = 1
IsEarlyScoreEligible       = false
```

If this fixture comes out `Long`, the reconstructor is using close action instead of the opening deal.

Suggested fact: `F02_Short_full_close_0_10_lots_native_1000`.

---

## 5. F03 — Scale-in at a better price (not averaged down)

**Purpose:** two `ENTRY_IN` on the same `position_id` (netting). Second buy is **above** current VWAP → `WasScaledIn=T`, `WasAveragedDown=F`. One close of **0.20** (`native=2000` = two bricks). Partial-close is **false**.

### Deals

| ticket | time_msc | order | pos | action | entry | native | lots | price | profit | comm | swap | comment |
|---:|---:|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---|
| 10301 | 1000 | 20301 | 9103 | Buy | In | **1000** | 0.10 | 2300.00 | 0 | -0.60 | 0 | B34-F03-IN1 |
| 10302 | 1500 | 20302 | 9103 | Buy | In | **1000** | 0.10 | 2310.00 | 0 | -0.60 | 0 | B34-F03-IN2 |
| 10303 | 2000 | 20303 | 9103 | Sell | Out | **2000** | 0.20 | 2320.00 | 300.00 | -1.20 | -0.40 | B34-F03-OUT |

```text
entry_vwap = (2300 * 0.10 + 2310 * 0.10) / 0.20 = 461.0 / 0.20 = 2305.00
exit_vwap  = 2320.00
before 10302: vwap=2300; 2310 > 2300 on a Long → NOT averaged down
after 10302: rem=0.20, max=0.20, completed=false, first-3 still 0
```

Informational PnL: `(2320-2300)*10 + (2320-2310)*10 = 200+100 = 300`.

### C#

```csharp
var deals = new[]
{
    B34.Deal(10301, 9103, DealAction.Buy,  DealEntry.In,  1000, 2300m,   0m, 1000, commission: -0.60m, comment: "B34-F03-IN1"),
    B34.Deal(10302, 9103, DealAction.Buy,  DealEntry.In,  1000, 2310m,   0m, 1500, commission: -0.60m, comment: "B34-F03-IN2"),
    B34.Deal(10303, 9103, DealAction.Sell, DealEntry.Out, 2000, 2320m, 300m, 2000, commission: -1.20m, swap: -0.40m, comment: "B34-F03-OUT"),
};
```

### Expected — one row

| Field | Value |
|---|---|
| `Id` | `ACHIEVER:1:9103:1000` |
| `Direction` | `Long` |
| `EntryVwap` | **`2305.00`** |
| `ExitVwap` | `2320.00` |
| Lots init/max/closed/rem | **0.10 / 0.20 / 0.20 / 0** |
| `Gross` / `Comm` / `Swap` / `Net` | `300.00` / `-2.40` / `-0.40` / **`297.20`** |
| `DealCount` | 3 |
| `WasScaledIn` | **T** |
| `WasPartialClose` | **F** |
| `WasAveragedDown` | **F** |
| `Completed` | T |

Fail if someone treats two INs as two reconstructed trades.

Suggested fact: `F03_Scale_in_better_price_vwap_2305_not_averaged`.

---

## 6. F04 — Partial close, then remainder (still one trade)

**Purpose:** a `0.10` OUT against a `0.20` book is **not** trade #2. `WasPartialClose` latches on the first OUT. First-3 counter stays 0 until the second OUT flattens.

Open uses two bricks in **one** deal (`native=2000`) so the only `1000` natives in this fixture are the two closing legs.

### Deals

| ticket | time_msc | order | pos | action | entry | native | lots | price | profit | comm | swap | sl | tp | comment |
|---:|---:|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|
| 10401 | 1000 | 20401 | 9104 | Buy | In | **2000** | 0.20 | 2300.00 | 0 | -1.20 | 0 | 2280 | 2350 | B34-F04-IN |
| 10402 | 1400 | 20402 | 9104 | Sell | Out | **1000** | 0.10 | 2310.00 | 100.00 | -0.60 | 0 | — | — | B34-F04-OUT1 |
| 10403 | 2000 | 20403 | 9104 | Sell | Out | **1000** | 0.10 | 2320.00 | 200.00 | -0.60 | -0.40 | — | — | B34-F04-OUT2 |

```text
after 10402: rem=0.10  closed=0.10  completed=false  was_partial=true  completed_xau=0
after 10403: rem=0     closed=0.20  completed=true   completed_xau=1
exit_vwap = (2310 * 0.10 + 2320 * 0.10) / 0.20 = 2315.00
```

### Mid-tape assert (run reconstructor on `[10401, 10402]` only)

| Field | Value |
|---|---|
| `Completed` | **F** |
| `ClosedAt` | `null` |
| `RemainingVolumeLots` | **0.10** |
| `ClosedVolumeLots` | **0.10** |
| `WasPartialClose` | **T** |
| `ExitVwap` | `2310.00` (only the first close leg) |
| `CountCompletedXauUsdTrades` | **0** |

### Full-tape expected — one row (not two)

| Field | Value |
|---|---|
| `Id` | `ACHIEVER:1:9104:1000` |
| `EntryVwap` / `ExitVwap` | `2300.00` / **`2315.00`** |
| Lots init/max/closed/rem | **0.20 / 0.20 / 0.20 / 0** |
| `Gross` / `Comm` / `Swap` / `Net` | `300.00` / `-2.40` / `-0.40` / **`297.20`** |
| `DealCount` | 3 |
| `WasScaledIn` / `WasPartialClose` / `WasAveragedDown` | F / **T** / F |
| `InitialSl` / `InitialTp` | `2280` / `2350` |
| `Completed` | T |
| `CountCompletedXauUsdTrades` | **1** |

Suggested facts: `F04_Partial_out_is_not_a_second_trade`; `F04_Remainder_flatten_completes_same_lifecycle`.

`ENTRY_OUT_BY` (entry=3) is applied by the same `ApplyOut` path. Optional twin: copy F04, set 10402/10403 `Entry = DealEntry.OutBy`. Same numbers. Not a ninth fixture — same expected row.

---

## 7. F05 — `ENTRY_INOUT` reversal (complete long + open short)

**Purpose:** one deal closes the `0.10` long and opens a `0.10` short on the **same** `position_id`. INOUT native is **2000** (closed brick + new brick). Ticket **10502 is listed on both trades**.

### Deals

| ticket | time_msc | order | pos | action | entry | native | lots | price | profit | comm | swap | comment |
|---:|---:|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---|
| 10501 | 1000 | 20501 | 9105 | Buy | In | **1000** | 0.10 | 2300.00 | 0 | -0.60 | 0 | B34-F05-IN |
| 10502 | 2000 | 20502 | 9105 | Sell | **InOut=2** | **2000** | 0.20 | 2290.00 | -100.00 | -1.20 | -0.20 | B34-F05-INOUT |

```text
impl leftover = ToLots(2000) - remaining 0.10 = 0.20 - 0.10 = 0.10 short
A21 closed_h = 10, new_h = 10  (same split after native/100)
```

### Expected — two rows, apply order

`NormalizedDeal` has **no** `VolumeClosed`. The current engine always closes `open.RemainingLots` and opens `dealLots - remaining`.

#### Trade A — completed long (money from both deals)

| Field | Spec (A21) | **Current `TradeReconstructor` (assert this)** |
|---|---|---|
| `Id` | — | `ACHIEVER:1:9105:1000` |
| `Direction` | Long | Long |
| `Completed` / `ClosedAt` | T / 2000 | T / `ms=2000` |
| Lots init/max/closed/rem | 0.10 / 0.10 / 0.10 / 0 | same |
| `EntryVwap` / `ExitVwap` | 2300 / 2290 | 2300 / 2290 |
| `Gross` / `Comm` / `Swap` / `Net` | -100 / -1.80 / -0.20 / **-102.00** | **same** |
| `DealTickets` | 10501, **10502** | 10501, **10502** |
| `DealCount` | 2 | 2 |

#### Trade B — open short (INOUT leftover)

| Field | Spec (A21) — money **not** reapplied | **Current impl — `OpenTrade.Start` reapplies 10502 money** |
|---|---|---|
| `Id` | — | `ACHIEVER:1:9105:2000` |
| `Direction` | Short | Short |
| `Completed` / `ClosedAt` | F / null | F / null |
| Lots init/max/closed/rem | 0.10 / 0.10 / 0 / **0.10** | same |
| `EntryVwap` / `ExitVwap` | 2290 / null | 2290 / null |
| `Gross` | **0** | **-100.00** |
| `Commission` | **0** | **-1.20** |
| `Swap` | **0** | **-0.20** |
| `NetRealizedPnl` | **0** | **-101.40** |
| `DealTickets` | **[10502]** | **[10502]** |
| `DealCount` | 1 | 1 |

```text
CountCompletedXauUsdTrades = 1     (only A)
IsEarlyScoreEligible       = false
RemainingVolumeLots on B   = 0.10
```

**Honesty:** unit tests written **today** must expect the impl column for Trade B. A21 says INOUT money stays on the closed lifecycle only (`Start` must skip `apply_money` for the leftover). That is a known recon gap, not a fixture typo. Do not “fix” the fixture to hide the double-count.

Suggested facts: `F05_InOut_completes_long_and_opens_short_0_10`; `F05_InOut_ticket_listed_on_both_trades`.

Optional closer (not part of the 8-deal core; add only if a test needs two completed):

```csharp
B34.Deal(10503, 9105, DealAction.Buy, DealEntry.Out, 1000, 2280m, 100m, 3000, commission: -0.60m, comment: "B34-F05-SHORT-OUT")
```

Then B completes: `ExitVwap=2280`, `ClosedVolumeLots=0.10`, `Completed=T`. Impl net on B becomes `-101.40 + 100 - 0.60 = -2.00`. Spec net on B would be `100 - 0.60 = 99.40`.

---

## 8. F06 — First three completed XAUUSD (early-score latch)

**Purpose:** `IsEarlyScoreEligible` is true **iff** `CountCompletedXauUsdTrades >= 3`. Each trade is a clean `0.10` round-trip on its **own** `position_id` (hedging-style). Login **2006** so this tape never pollutes login `1`.

### Deals

| ticket | time_msc | login | pos | action | entry | native | price | profit | comm | comment |
|---:|---:|---:|---:|---|---|---:|---:|---:|---:|---|
| 10601 | 1000 | 2006 | 9110 | Buy | In | **1000** | 2320.00 | 0 | -0.60 | B34-F06-T1-IN |
| 10602 | 2000 | 2006 | 9110 | Sell | Out | **1000** | 2330.00 | 100.00 | -0.60 | B34-F06-T1-OUT |
| 10603 | 3000 | 2006 | 9111 | Buy | In | **1000** | 2320.00 | 0 | -0.60 | B34-F06-T2-IN |
| 10604 | 4000 | 2006 | 9111 | Sell | Out | **1000** | 2330.00 | 100.00 | -0.60 | B34-F06-T2-OUT |
| 10605 | 5000 | 2006 | 9112 | Buy | In | **1000** | 2320.00 | 0 | -0.60 | B34-F06-T3-IN |
| 10606 | 6000 | 2006 | 9112 | Sell | Out | **1000** | 2330.00 | 100.00 | -0.60 | B34-F06-T3-OUT |

### C#

```csharp
const long L = 2006;
var deals = new[]
{
    B34.Deal(10601, 9110, DealAction.Buy,  DealEntry.In,  1000, 2320m,   0m, 1000, login: L, commission: -0.60m, comment: "B34-F06-T1-IN"),
    B34.Deal(10602, 9110, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100m, 2000, login: L, commission: -0.60m, comment: "B34-F06-T1-OUT"),
    B34.Deal(10603, 9111, DealAction.Buy,  DealEntry.In,  1000, 2320m,   0m, 3000, login: L, commission: -0.60m, comment: "B34-F06-T2-IN"),
    B34.Deal(10604, 9111, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100m, 4000, login: L, commission: -0.60m, comment: "B34-F06-T2-OUT"),
    B34.Deal(10605, 9112, DealAction.Buy,  DealEntry.In,  1000, 2320m,   0m, 5000, login: L, commission: -0.60m, comment: "B34-F06-T3-IN"),
    B34.Deal(10606, 9112, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100m, 6000, login: L, commission: -0.60m, comment: "B34-F06-T3-OUT"),
};
```

### Prefix / full asserts

| Input slice | completed XAU | `IsEarlyScoreEligible` | notes |
|---|---:|---|---|
| `[10601]` | 0 | F | open only |
| `[10601..10602]` | 1 | F | trade #1 `Id=ACHIEVER:2006:9110:1000`, net `98.80` |
| `[10601..10604]` | 2 | F | trade #2 `Id=ACHIEVER:2006:9111:3000` |
| `[10601..10606]` | **3** | **T** | trade #3 `Id=ACHIEVER:2006:9112:5000`; latch here |
| + optional 4th (below) | 4 | **T** (stays) | must **not** emit `PROVEN_PROFITABLE` |

Each completed row (identical shape, different keys):

| Field | Value |
|---|---|
| `Direction` | Long |
| Lots | **0.10 / 0.10 / 0.10 / 0** |
| `EntryVwap` / `ExitVwap` | 2320 / 2330 |
| `NetRealizedPnl` | **98.80** |
| Flags | all F except `Completed` T |
| `IsXauUsd` | T |

Optional latch deal pair (same fixture file, extra test method):

```csharp
B34.Deal(10607, 9113, DealAction.Buy,  DealEntry.In,  1000, 2320m,   0m, 7000, login: 2006, commission: -0.60m),
B34.Deal(10608, 9113, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100m, 8000, login: 2006, commission: -0.60m),
```

Suggested facts: `F06_Two_completed_xau_is_not_early_score`; `F06_Third_0_10_close_latches_early_score`; `F06_Fourth_does_not_clear_latch`.

---

## 9. F07 — Averaging down on a long (scale-in at a worse price)

**Purpose:** second `ENTRY_IN` at `2290 <` VWAP `2300` on a Long → `WasAveragedDown=T` **and** `WasScaledIn=T`. Dedicated fixture; do not fold into F03 or F04.

### Deals

| ticket | time_msc | order | pos | action | entry | native | lots | price | profit | comm | swap | comment |
|---:|---:|---:|---:|---|---|---:|---:|---:|---:|---:|---:|---|
| 10701 | 1000 | 20701 | 9107 | Buy | In | **1000** | 0.10 | 2300.00 | 0 | -0.60 | 0 | B34-F07-IN1 |
| 10702 | 1500 | 20702 | 9107 | Buy | In | **1000** | 0.10 | 2290.00 | 0 | -0.60 | 0 | B34-F07-IN2 |
| 10703 | 2000 | 20703 | 9107 | Sell | Out | **2000** | 0.20 | 2310.00 | 300.00 | -1.20 | -0.40 | B34-F07-OUT |

```text
before 10702: vwap=2300; 2290 < 2300 on Long → WasAveragedDown=true
entry_vwap = (2300 * 0.10 + 2290 * 0.10) / 0.20 = 459.0 / 0.20 = 2295.00
exit_vwap  = 2310.00
```

Informational PnL: `(2310-2300)*10 + (2310-2290)*10 = 100+200 = 300`.

### C#

```csharp
var deals = new[]
{
    B34.Deal(10701, 9107, DealAction.Buy,  DealEntry.In,  1000, 2300m,   0m, 1000, commission: -0.60m, comment: "B34-F07-IN1"),
    B34.Deal(10702, 9107, DealAction.Buy,  DealEntry.In,  1000, 2290m,   0m, 1500, commission: -0.60m, comment: "B34-F07-IN2"),
    B34.Deal(10703, 9107, DealAction.Sell, DealEntry.Out, 2000, 2310m, 300m, 2000, commission: -1.20m, swap: -0.40m, comment: "B34-F07-OUT"),
};
```

### Expected — one row

| Field | Value |
|---|---|
| `Id` | `ACHIEVER:1:9107:1000` |
| `EntryVwap` / `ExitVwap` | **`2295.00`** / `2310.00` |
| Lots init/max/closed/rem | **0.10 / 0.20 / 0.20 / 0** |
| `Gross` / `Comm` / `Swap` / `Net` | `300.00` / `-2.40` / `-0.40` / **`297.20`** |
| `WasScaledIn` | **T** |
| `WasAveragedDown` | **T** |
| `WasPartialClose` | F |
| `Completed` | T |

Contrast with F03: only the scale-in **price vs VWAP-before** differs. If F03 and F07 both set `WasAveragedDown`, the detector is ignoring direction/VWAP.

Short twin (not a 9th fixture — optional theory row): sell IN 1000 @ 2300, sell IN 1000 @ 2310 (`2310 > 2300` on a Short → avg down). Same flags.

Suggested fact: `F07_Long_add_below_vwap_is_averaged_down`.

---

## 10. F08 — Noise, non-XAU, aliases, still-open (not early-score)

**Purpose:** reconstruction / first-3 must ignore balance + commission, must **not** count EURUSD toward first-3, must map `GOLD` and `XAUUSD.a` to canonical `XAUUSD`, and must **not** count a still-open `0.10` XAU book.

Login **2008**. Presented **out of time order** (commission and balance listed last) so tests prove sort-by-`timeMsc`, not array order.

### Deals

| ticket | time_msc | login | pos | symbol | action | entry | native | price | profit | comm | comment |
|---:|---:|---:|---:|---|---|---|---:|---:|---:|---:|---|
| 10802 | 1000 | 2008 | 9201 | EURUSD | Buy=0 | In | **1000** | 1.10000 | 0 | 0 | B34-F08-EUR-IN |
| 10803 | 2000 | 2008 | 9201 | EURUSD | Sell=1 | Out | **1000** | 1.10100 | 10.00 | 0 | B34-F08-EUR-OUT |
| 10804 | 3000 | 2008 | 9202 | GOLD | Buy | In | **1000** | 2320.00 | 0 | -0.60 | B34-F08-GOLD-IN |
| 10805 | 4000 | 2008 | 9202 | GOLD | Sell | Out | **1000** | 2330.00 | 100.00 | -0.60 | B34-F08-GOLD-OUT |
| 10806 | 5000 | 2008 | 9203 | XAUUSDm | Buy | In | **1000** | 2340.00 | 0 | -0.60 | B34-F08-OPEN |
| 10807 | 6000 | 2008 | 9205 | XAUUSD.a | Buy | In | **1000** | 2320.00 | 0 | -0.60 | B34-F08-ALIAS-IN |
| 10808 | 7000 | 2008 | 9205 | XAUUSD.a | Sell | Out | **1000** | 2330.00 | 100.00 | -0.60 | B34-F08-ALIAS-OUT |
| 10801 | 500 | 2008 | 0 | *(empty)* | **Balance=2** | In | **0** | 0 | 10000.00 | 0 | B34-F08-BAL |
| 10809 | 3500 | 2008 | 0 | *(empty)* | **Commission=7** | In | **0** | 0 | -2.00 | 0 | B34-F08-COMM |

### C# (deliberately unsorted)

```csharp
const long L = 2008;
var deals = new[]
{
    B34.Deal(10802, 9201, DealAction.Buy,        DealEntry.In,  1000, 1.10000m,     0m, 1000, login: L, symbol: "EURUSD",   comment: "B34-F08-EUR-IN"),
    B34.Deal(10803, 9201, DealAction.Sell,       DealEntry.Out, 1000, 1.10100m,    10m, 2000, login: L, symbol: "EURUSD",   comment: "B34-F08-EUR-OUT"),
    B34.Deal(10804, 9202, DealAction.Buy,        DealEntry.In,  1000, 2320m,        0m, 3000, login: L, symbol: "GOLD",     commission: -0.60m, comment: "B34-F08-GOLD-IN"),
    B34.Deal(10805, 9202, DealAction.Sell,       DealEntry.Out, 1000, 2330m,      100m, 4000, login: L, symbol: "GOLD",     commission: -0.60m, comment: "B34-F08-GOLD-OUT"),
    B34.Deal(10806, 9203, DealAction.Buy,        DealEntry.In,  1000, 2340m,        0m, 5000, login: L, symbol: "XAUUSDm",  commission: -0.60m, comment: "B34-F08-OPEN"),
    B34.Deal(10807, 9205, DealAction.Buy,        DealEntry.In,  1000, 2320m,        0m, 6000, login: L, symbol: "XAUUSD.a", commission: -0.60m, comment: "B34-F08-ALIAS-IN"),
    B34.Deal(10808, 9205, DealAction.Sell,       DealEntry.Out, 1000, 2330m,      100m, 7000, login: L, symbol: "XAUUSD.a", commission: -0.60m, comment: "B34-F08-ALIAS-OUT"),
    B34.Deal(10801, 0,    DealAction.Balance,    DealEntry.In,     0,    0m,    10000m,  500, login: L, symbol: "",         comment: "B34-F08-BAL"),
    B34.Deal(10809, 0,    DealAction.Commission, DealEntry.In,     0,    0m,       -2m, 3500, login: L, symbol: "",         comment: "B34-F08-COMM"),
};
```

Apply order after sort: `10801 (500) → 10802 → 10803 → 10804 → 10809 (3500) → 10805 → 10806 → 10807 → 10808`.

### Expected

`IsTradingDeal` is only `Buy`/`Sell`. Balance + commission never open a book. Commission **-2** is **not** folded into GOLD.

#### A21 / first-3 view (what scoring must use)

| # | key | source | dir | completed | counts for first-3 |
|---:|---|---|---|---|---|
| — | EURUSD 9201 | EURUSD | Long | T | **No** (`!IsXauUsd`) |
| 1 | `ACHIEVER:2008:9202:3000` | GOLD | Long | T | **Yes** |
| — | `ACHIEVER:2008:9203:5000` | XAUUSDm | Long | **F** (open 0.10) | **No** |
| 2 | `ACHIEVER:2008:9205:6000` | XAUUSD.a | Long | T | **Yes** |

```text
CountCompletedXauUsdTrades("ACHIEVER", 2008, deals) = 2
IsEarlyScoreEligible                               = false
```

GOLD and `XAUUSD.a` rows: lots **0.10/0.10/0.10/0**, entry 2320, exit 2330, net **98.80**, `CanonicalSymbol=XAUUSD`, `IsXauUsd=true`.

Open XAU row: `Completed=false`, `ClosedAt=null`, `RemainingVolumeLots=0.10`, `ExitVwap=null`, `NetRealizedPnl=-0.60` (open IN commission only).

#### Current `Reconstruct()` (no pre-filter)

`TradeReconstructor.Reconstruct` does **not** drop non-XAU. It returns **4** results: EURUSD completed + GOLD completed + XAUUSDm open + `XAUUSD.a` completed.

| Assert against | Value |
|---|---|
| `Reconstruct(...).Count` | **4** (impl) |
| `CompletedXauUsdTrades(...).Count` | **2** |
| EURUSD `CanonicalSymbol` | `"EURUSD"` (unmapped → source string) |
| EURUSD `IsXauUsd` | `false` |
| EURUSD lots | **0.10 / 0.10 / 0.10 / 0** (same native brick) |

A21 would set `skipped_non_xau=2` (EUR IN/OUT) and `skipped_non_trade=2` and would **not** emit an EURUSD `ReconstructedTrade`. First-3 counts still match. Tests that call `Reconstruct` directly must expect 4 rows **until** a pre-filter is added; tests that call `CompletedXauUsdTrades` / `CountCompletedXauUsdTrades` already match A21 §15.

Suggested facts: `F08_Balance_and_commission_are_not_trades`; `F08_EurUsd_is_not_first3`; `F08_Gold_and_XauUsdA_map_to_canonical`; `F08_Open_0_10_xau_does_not_count`.

---

## 11. Worked remainder traces (copy into comments)

```text
F01 rem: +0.10 → 0
F02 rem: -0.10 → 0          (sign is conceptual; impl stores RemainingLots >= 0 and Direction)
F03 rem: +0.10 → +0.20 → 0
F04 rem: +0.20 → +0.10 → 0
F05 rem: +0.10 → 0 (A), then +0.10 short (B)
F06 rem: three independent +0.10 → 0 books
F07 rem: +0.10 → +0.20 → 0
F08 rem: EUR +0.10→0 | GOLD +0.10→0 | XAU open +0.10 | alias +0.10→0
```

Impl stores `RemainingLots` as a positive decimal and `Direction` separately. Do not assert a signed remaining.

Invariant after every successful apply (lots form):

```text
ClosedVolumeLots + RemainingVolumeLots == (WasScaledIn ? MaxVolumeLots : InitialVolumeLots)
MaxVolumeLots >= InitialVolumeLots
MaxVolumeLots >= RemainingVolumeLots
completed ⇒ RemainingVolumeLots == 0 && ClosedVolumeLots > 0
```

For F03/F07: `0.20 + 0 == 0.20 == Max`. For F04: `0.20 + 0 == 0.20 == Initial == Max`.

---

## 12. Suggested xUnit methods (do not implement in this report)

| Method | Fixture | Hard asserts |
|---|---|---|
| `Manager_scale_0_10_lots_is_1000_native` | pin | `ToNative(0.10m)==1000` |
| `F01_Long_full_close_0_10_lots_native_1000` | F01 | 1 completed Long, net `98.80`, SL/TP, `Id` |
| `F02_Short_full_close_0_10_lots_native_1000` | F02 | `Direction==Short`, net `98.80` |
| `F03_Scale_in_better_price_vwap_2305_not_averaged` | F03 | max `0.20`, VWAP `2305`, scaled T, avg F, partial F |
| `F04_Partial_out_is_not_a_second_trade` | F04 mid | 1 open trade, rem `0.10`, completed XAU `0` |
| `F04_Remainder_flatten_completes_same_lifecycle` | F04 full | 1 completed, exit VWAP `2315`, partial T |
| `F05_InOut_completes_long_and_opens_short_0_10` | F05 | 2 trades, rem B=`0.10`, ticket 10502 on both |
| `F05_InOut_ticket_listed_on_both_trades` | F05 | `DealTickets` |
| `F06_Two_completed_xau_is_not_early_score` | F06 prefix | count=2, eligible F |
| `F06_Third_0_10_close_latches_early_score` | F06 | count=3, eligible T |
| `F07_Long_add_below_vwap_is_averaged_down` | F07 | avg T, scaled T, VWAP `2295` |
| `F08_Balance_and_commission_are_not_trades` | F08 | no pos-0 trade |
| `F08_EurUsd_is_not_first3` | F08 | completed XAU=2 |
| `F08_Gold_and_XauUsdA_map_to_canonical` | F08 | both `IsXauUsd` |
| `F08_Open_0_10_xau_does_not_count` | F08 | rem `0.10`, completed F |

Replay: run F01–F07 twice on the same array; second pass identical `Id`, flags, decimals.

Do **not** assert MFE/MAE. `ReconstructedTradeResult` has no excursion fields (`A21` §1.4 / `A45`).

---

## 13. What these fixtures deliberately omit

| Omitted | Why | Where it lives |
|---|---|---|
| A21 `volume_h` / hundredths arithmetic | Product wire is `VolumeNative` scale 10 000 | Adapter note in A21 §2.1; do not put `10` in `VolumeNative` |
| `VolumeExt` (`0.10` lot = 10 000 000) | Extractors copy `Volume()`, not `VolumeExt()` | `VolumeConverter.Extended` only |
| Dirty / failure codes (`RECON_OUT_OVERCLOSE`, cancel, opposite IN) | `TradeReconstructor` has no dirty flag today | A21 F17–F22 |
| Hedge close-by pair (two `position_id`s + `OUT_BY`) | Same `ApplyOut` as F04; optional twin noted there | A21 F09 |
| Opposite `ENTRY_IN` (impl **discards** the closed trade) | Would be a 9th adversarial fixture | `ApplyIn` lines that ignore `closed` |
| Duplicate-ticket idempotency | Engine does not dedupe | A21 F16 |
| Fee / reason / `position_by_id` / `TimeMsc` extra fields | Not on `NormalizedDeal` | ingest gap |
| Destination qty / FIX / scoring numbers | Out of reconstruction unit scope | A22 / A43 |

---

## 14. Implementation deltas testers must not greenwash

Read from `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` on 2026-08-18:

1. **INOUT money on the new book.** `ApplyReverse` → `OpenTrade.Start` adds `Profit`/`Commission`/`Swap` of the INOUT deal again. F05 Trade B impl net is `-101.40`, not `0`. Pin impl until `Start` grows a `applyMoney: false` path.
2. **No XAU pre-filter on `Reconstruct`.** F08 returns EURUSD as a real trade. First-3 helpers filter after. Prefer `CompletedXauUsdTrades` for §15 asserts.
3. **Decimal lots, not `remaining_h`.** Safe here because `1000/10000m = 0.10m` exactly. Do not add a `0.03+0.03+0.04` split in this pack (that belongs to an integer-hundredths engine).
4. **No `lifecycle_seq`.** Reuse of `position_id` after flatten (A21 F11) would be a 9th fixture; `Id` uses `OpenedAt` ms instead.
5. **Opposite IN is not a hard fail.** Current code synthesizes a reverse and **drops** the closed result. None of these eight feed that branch.
6. Existing mashed test `Scale_in_and_partial_close` covers F03+F04+F07 in one tape (avg-down + two OUTs). These eight **split** that so a single flag cannot hide another.

---

## 15. Native integer checklist (every deal in F01–F08)

| Deal ticket | `VolumeNative` | Meaning |
|---:|---:|---|
| 10101, 10102 | 1000 | F01 0.10 / 0.10 |
| 10201, 10202 | 1000 | F02 0.10 / 0.10 |
| 10301, 10302 | 1000 | F03 scale-in bricks |
| 10303 | 2000 | F03 close 0.20 |
| 10401 | 2000 | F04 open 0.20 |
| 10402, 10403 | 1000 | F04 partial bricks |
| 10501 | 1000 | F05 open 0.10 |
| 10502 | 2000 | F05 INOUT 0.20 = close 0.10 + open 0.10 |
| 10601–10606 | 1000 | F06 six 0.10 legs |
| 10701, 10702 | 1000 | F07 scale-in bricks |
| 10703 | 2000 | F07 close 0.20 |
| 10802–10808 | 1000 | F08 trading bricks |
| 10801, 10809 | **0** | non-trade |

If a future test puts `VolumeNative = 10` for `0.10` lot, `ToLots` yields `0.001` and every expected lot column in this file will fail. That failure is correct.

---

## 16. Acceptance for a later unit-test PR

A reconstruction unit class is not done until:

- [ ] Scale pin `0.10 ↔ 1000` lives in the same test assembly.
- [ ] All eight fixtures exist as `[Fact]`s (F04 and F06 may use two facts each).
- [ ] F01 net is `98.80`, not `100` (comm+swap must move the needle).
- [ ] F03 VWAP is `2305` and `WasAveragedDown` is false.
- [ ] F04 mid-tape completed-XAU count is `0`.
- [ ] F05 yields two trades, leftover rem `0.10`, ticket `10502` on both; money on B matches **impl** (or spec, if `Start` is fixed first — update this file if that happens).
- [ ] F06 eligible flips from false → true exactly on `10606`.
- [ ] F07 `WasAveragedDown` is true.
- [ ] F08 completed XAU = 2; open rem = 0.10; balance/commission produce no row.
- [ ] Product source for this fixture design remains unmodified (this report is the spec).

**Product source was not modified.** This file is the only write from B34.
