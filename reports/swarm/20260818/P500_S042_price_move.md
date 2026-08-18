# P500_S042 — `MaxPriceMove=3.0` vs `ExpectedPrice` mid is too loose for gold copy

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S042_price_move.md` |
| Agent | P500_S042 (price-move guard / gold units) |
| Date | 2026-08-18 |
| Assigned | Audit `MaxPriceMove=3.0` vs `ExpectedPrice` mid. On gold $3 is ~300 typical points. Guard may be too loose for copy. **Do not edit product.** |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` |
| Tests | `D:\Prop\tests\Unit\RiskEngineTests.cs` — **no** `PRICE_MOVED_TOO_FAR` fact |
| Adjacent | `CopyIntent.ExpectedPrice` written as `trade.EntryVwap` in `EfTradingStore.cs`; A72 §4.3 / A23 §6.3 / architecture §37 |
| Method | Full read of the evaluate branch, defaults, fixture book, store writer, A72 signed-taker law, sibling unit report P500_S029. No product edit. |

Classification: `STUB_WRONG` (unsigned mid, source VWAP as expected, $3 lab constant) · `TOO_LOOSE` for XAU copy · `ABSENT` test · `SAFE_BY_ABSENCE` (no live `35=D`; `Evaluate` has no product caller).

This file does **not** tighten the default. It records the measured predicate and why **$3 mid vs expected is not a copy-safe gold guard**.

---

## 0. Verdict

**`MaxPriceMove = 3.0` is three US dollars per ounce of unsigned mid, not 3 points.**

On typical 2-digit XAUUSD (`Point = 0.01`), **$3.00 = 300 points**. That is a **whole typical gold scalp target**, not a pre-trade noise band.

The engine rejects only when:

```text
|(Bid + Ask) / 2 − ExpectedPrice| > 3.0
AND action is OpenExposure or IncreaseExposure
```

That is **too loose for copy**:

| Allowed mid gap | Typical gold meaning | $ / 1.00 lot (100 oz) | $ / 0.10 lot (unit fixture) |
|---|---|---:|---:|
| **$3.00** (default cap, equality **passes**) | **300 points** | **$300** | **$30** |
| $2.99 (still **approves**) | 299 points | $299 | $29.90 |
| $0.30 (common 30-pt scalp) | 30 points | $30 | $3 |
| $0.50–$2.00 (3-minute target band, P500_CODE_5) | 50–200 points | $50–$200 | $5–$20 |

A dest book can walk **almost the entire scalp** against the copy and still print `APPROVED`. The guard only fires after the move **is** the edge.

Sibling fact (P500_S029): `MaxAllowedSpread = 2.0` is the same unit trap. Together they allow a **$2.00 book + $3.00 mid gap** before either reason fires.

**Do not claim** “3-point slippage protection,” “price-move is production-ready,” or “§37 is implemented.” Unsigned mid + $3 default + source VWAP as expected is **not** A72 §4.3.

---

## 1. What the code actually compares

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`

Default:

```csharp
public decimal MaxPriceMove { get; init; } = 3.0m;
```

Predicate (lines 108–110):

```csharp
var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
    return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
```

Facts measured from this block (not from `docs/risk.md`):

| Fact | Evidence |
|---|---|
| Unit is **dest price** (USD/oz on XAU fixtures) | `Bid`/`Ask`/`ExpectedPrice` are `decimal` prices; **no** `/ Point`, **no** `digits`, **no** tick table |
| Reference is **mid**, not taker touch | `(Bid + Ask) / 2` |
| Distance is **unsigned** | `Math.Abs` — favorable $3.01 **rejects** the same as adverse $3.01 |
| Comparator is **strict `>`** | `|mid − expected| == 3.0` **passes** |
| Only OPEN / INCREASE | `IsIncreasing` = `OpenExposure` \| `IncreaseExposure` |
| REDUCE / CLOSE skip the guard | they fall through to `RISK_REDUCTION` after earlier checks |
| Quote must be non-null to reach this line | null quote already returned `QUOTE_MISSING` on increasing |
| `MaxSlippage = 1.5m` is **never read** | no `MAX_SLIPPAGE_EXCEEDED` branch |
| Hardcoded lab constant | not in `appsettings*.json`; no symbol-specific table |

`DestinationQuote` has `Bid`, `Ask`, `ReceivedAt`, `VenueTimestamp`. It has **no** `Digits`, `Point`, or `TickSize`. `RiskLimits` has **no** unit suffix on `MaxPriceMove`.

---

## 2. Why $3 on gold is 300 points, not a tight band

Retail XAUUSD cash/CFD (2-digit books, the lab fixture style `2399.9` / `2400.1`):

```text
Point      = 0.01 USD/oz
1 point    = $0.01/oz
MaxPriceMove 3.0 USD/oz = 3.0 / 0.01 = 300 points
```

If a desk is 3-digit (`Point = 0.001`), the **same $3** is **3000 points**. The code does not know which book it is looking at.

`docs/risk.md` “Slippage Tolerance **30 points**” is a **different, unimplemented** story (`slippage_points = abs(mt5 − ctrader) / point_size`). The Domain engine does **not** divide by `point_size`. Misreading `3.0` as “3 points” understates the allowed gap by **100×**.

Contract translation (confirm ounces-per-lot on the dest symbol before quoting $/lot on a dashboard; typical retail gold = 100 oz / 1.00 lot):

```text
gap_usd ≈ |mid − expected| × ounces
ounces  ≈ lots × 100
```

| Mid gap vs `ExpectedPrice` | Points (2-digit) | Cost / 1.00 lot | Cost / 0.10 lot | Engine at default |
|---:|---:|---:|---:|---|
| 0.01 | 1 | $1 | $0.10 | approve |
| 0.20 (fixture half-spread×2) | 20 | $20 | $2 | approve |
| 0.50 | 50 | $50 | $5 | approve |
| 1.50 (`MaxSlippage` stub, unused) | 150 | $150 | $15 | approve |
| **2.99** | **299** | **$299** | **$29.90** | **approve** |
| **3.00** | **300** | **$300** | **$30** | **approve** (`>` not `>=`) |
| 3.01 | 301 | $301 | $30.10 | `PRICE_MOVED_TOO_FAR` |

A copy that is late by **one typical scalp** is still legal.

---

## 3. `ExpectedPrice` is source VWAP, compared to dest mid

Request field:

```csharp
public required decimal ExpectedPrice { get; init; }   // RiskEvaluationRequest
```

Persist:

```csharp
public decimal ExpectedPrice { get; set; }             // CopyIntent
```

The only product writer found (`D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` ~303):

```csharp
ExpectedPrice = trade.EntryVwap,
```

`EntryVwap` is the **source reconstructed entry**, not dest ask/bid at intent create.

A72 §4.3 (binding design, **not** implemented):

> `expected_destination_price` is typically the taker touch **at intent creation**. It is **not** the source MT5 price unless mapping explicitly says so.

What this means if a live path ever called `Evaluate` with today’s writer:

1. Source fills at `2400.00`.
2. Dest mid is already `2401.20` (broker offset + half-spread + 80 ms model delay).
3. Remaining **unsigned** budget before reject: `3.00 − 1.20 = 1.80` of **further** dest mid walk — still 180 points.
4. Or dest mid is `2399.50` (favorable offset). Unsigned math **consumes** $0.50 of the $3.00 budget on a **better** book, then still allows $2.50 adverse from there.

Unit tests never exercise a gap. `RiskEngineTests.Base` sets:

| Field | Value | Mid vs expected |
|---|---|---|
| `ExpectedPrice` | `2400m` | — |
| `Bid` / `Ask` | `2399.9` / `2400.1` | mid = **2400.0** exactly |
| Spread | `0.20` | well under `2.0` |

Interior point. The move branch is **dead in every current fact**.

C03 / D35 / E005 / E017 already catalogued rule 8 as **Missing**. Still true.

---

## 4. Unsigned mid vs signed taker (why the predicate is wrong even at $0.30)

Architecture §37 wants three prices: expected dest, current dest quote, source. A23 §6.3 says `current_quote` is **bid for sell, ask for buy**. A72 §4.3 **forbids** `|mid − expected|` as the production predicate.

Required (not coded):

```text
# open/increase long  — we pay the ask
adverse = dest_ask_now − expected_destination_price

# open/increase short — we hit the bid
adverse = expected_destination_price − dest_bid_now

if adverse > max_tolerated_price_move_open:
    REJECT PRICE_MOVED_TOO_FAR

# favorable (adverse <= 0) must NOT reject
```

What today’s mid does instead (fixture `2399.9 / 2400.1`, expected `2400`):

| Event | Ask | Bid | Mid | `|mid−exp|` | Taker adverse (long) | Today | A72 |
|---|---:|---:|---:|---:|---:|---|---|
| Quiet | 2400.10 | 2399.90 | 2400.00 | 0.00 | +0.10 | approve | approve (if cap ≥ 0.10) |
| Ask jumps +2.00, bid unchanged | 2402.10 | 2399.90 | 2401.00 | **1.00** | **+2.10** | approve | reject if cap 0.30–1.00 |
| Both sides +2.99 | 2403.09 | 2402.89 | 2402.99 | 2.99 | +3.09 | **approve** | reject at any sane cap |
| Favorable −3.10 (better for long) | 2396.90 | 2396.90 | 2396.90 | 3.10 | **−3.10** | **reject** | **approve** |
| Mid moves, ask does not | 2400.10 | 2394.00 | 2397.05 | 2.95 | +0.10 | approve | approve |

The mid **under-states** the tax the copy actually pays (half-spread is invisible in `|mid − expected|` when expected was already a mid). A $2.00 ask jump with a sticky bid is a **$2** taker hit and only a **$1** mid move — half the budget, twice the pain.

Shadow already uses **taker** for fills (`ShadowCopyEngine.SimulateEntry`: long = `Ask`, short = `Bid`) and then adds a toy `0.05` named “Points.” Risk does **not** reuse that touch. Two models, neither is the A72 guard.

---

## 5. Combined with the other quote stubs (how loose “copy” would be)

All three quote checks share the same `if (Quote is not null)` block and the same `IsIncreasing` gate.

| Knob | Default | Unit (measured) | Typical gold | Loose factor vs 30-pt scalp |
|---|---:|---|---|---|
| `MaxAllowedSpread` | 2.0 | USD/oz width | 0.16–0.50 cash | **4–12×** a live book (P500_S029) |
| `MaxPriceMove` | 3.0 | USD/oz \|mid−expected\| | 0.10–0.50 intent-to-send | **6–30×** a 30–50 pt target |
| `MaxQuoteAge` | 3 s | receive clock | 50–200 ms healthy QUOTE | age only; does not cap $ gap |
| `MaxSlippage` | 1.5 | unused | — | **dead** |
| `MaxSourceSignalAge` | 15 s | source event age | 3-minute hold → 8% of hold | late copy still has a $3 mid budget |

A news reprint that is **fresh**, **tight** (`spread ≤ 2.0`), and whose **source fill already is the new print** (`mid ≈ ExpectedPrice`) **passes** `PRICE_MOVED_TOO_FAR` (P500_S028). The $3 cap does not see a spike that both sides already printed.

There is **no** news calendar. This guard is not a substitute (same report).

---

## 6. Wiring status (do not inflate)

| Claim | Measured |
|---|---|
| Engine **can** emit `PRICE_MOVED_TOO_FAR` | Yes, if a caller builds a request with `|mid−expected| > 3` and increasing action |
| Any product caller of `Evaluate` | **None** (only `tests/Unit/RiskEngineTests.cs` constructs `RiskEngine`) |
| `AllowFixSend` read before a socket | **No** (P500_S003) |
| Live `35=D` | **Absent** |
| Unit test for this branch | **Absent** |
| Config / env key | **Absent** — `3.0m` in source |
| Dashboard / settings PATCH of this limit | Spec only (A95). Not a measured dest-tape histogram |

Capital at risk from **this process**: none. The finding is **pre-wiring**. Shipping `REAL_COPY` with this default would be a capital-loss path (P500_CODE_15).

---

## 7. Lower-loss recommendation (design only — product not edited)

Keep the unit as **USD per ounce** (absolute dest price). Do **not** switch the engine to “points” unless `tick_size` is measured per dest instrument and the config key says `ticks`.

When (and only when) a live copy orchestrator exists:

1. Replace `|mid − ExpectedPrice|` with **signed taker adverse** (A72 §4.3). Favorable must pass.
2. Persist `expected_destination_price` as dest **ask/bid at intent create**, not source `EntryVwap`. Keep source price on a separate field for the unused `MaxSlippage` sibling.
3. Name the config key with the unit: `MaxAdverseMoveOpenXauUsdPerOz`.
4. Cut the **gap tail** from a dest tape (A72 §7.2 item 4), not the mean, not `3.0`.
5. Suggested **starting** research band after a histogram — **not** a number to paste into `RiskLimits` today:

| Regime | Signed adverse cap (USD/oz) | 2-digit points | Max tax / 1.00 lot |
|---|---:|---:|---:|
| Tight London / NY overlap scalp | **0.20–0.40** | 20–40 | $20–$40 |
| Normal + buffer | **0.50–0.80** | 50–80 | $50–$80 |
| Current lab default | **3.00 unsigned mid** | **300** | **$300** — reject as production |

6. Add the missing facts: long ask +ε over cap rejects; short bid −ε over cap rejects; favorable > $3 approves; mid-only move does **not** reject; `|gap| == cap` documented (`>` vs `>=`); REDUCE/CLOSE still skip.
7. Do **not** treat a tighter `MaxPriceMove` as a news filter (P500_S028).

**Not recommended:** advertising “300-point protection” or “3-point protection.” The first is what the number **is** and is too wide; the second is a unit lie.

---

## 8. Honest status

- Default **`3.0` = $3.00/oz unsigned dest mid vs `ExpectedPrice`**.
- On typical gold that is **300 points** and **~$300 per standard lot**.
- **Too loose for copy** of XAU scalps; also **wrong-signed** (rejects improvement, under-counts ask/bid).
- `ExpectedPrice` in the store is **source VWAP**, not dest touch.
- No test locks the branch.
- No product file was changed.

---

## 9. Paths cited

- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (default `MaxPriceMove = 3.0m`, lines 108–110)
- `D:\Prop\src\Domain\Entities\CopyIntent.cs` (`ExpectedPrice`)
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`ExpectedPrice = trade.EntryVwap`)
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` (taker fill; unused by this guard)
- `D:\Prop\tests\Unit\RiskEngineTests.cs` (interior `2400` mid; no move fact)
- `D:\Prop\docs\risk.md` (stale “30 points”; not the engine)
- `D:\Prop\reports\swarm\20260818\A72_quote_guards.md` §4.3 / §7.2 / §10.1
- `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` §6.3
- `D:\Prop\reports\swarm\20260818\P500_S029_spread_units.md` (same unit family)
- `D:\Prop\reports\swarm\20260818\P500_S028_no_news_filter.md` (spike copies when source **is** the new print)
- `D:\Prop\reports\swarm\20260818\P500_CODE_5.md` / `P500_CODE_15.md` (scalp target vs $3)
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §37
