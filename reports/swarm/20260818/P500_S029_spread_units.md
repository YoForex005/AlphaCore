# P500_S029 — `MaxAllowedSpread=2.0` units for XAUUSD

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S029_spread_units.md` |
| Agent | P500_S029 (spread unit audit) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Verdict | **`2.0` is dollars per ounce (absolute dest price), not MT5/cTrader “points”.** The lab default is **very loose** vs a 0.20–0.50 USD XAU book. |

---

## 1. Question

Is `RiskLimits.MaxAllowedSpread = 2.0` for XAUUSD **2.0 dollars** or **2.0 points**?

## 2. Answer (measured from code)

**Dollars (USD per troy ounce).** More precisely: **the same unit as `Quote.Ask` and `Quote.Bid`**, with **no** `point` / `digits` / `tick_size` conversion.

On this product those quotes are XAUUSD cash/CFD prices in **USD/oz** (fixtures `2399.90` / `2400.10`; dashboard spec `2401.12` / `2401.28`). Therefore `2.0` means **$2.00/oz of bid–ask width**.

It is **not**:

- 2 MT5 points (`Point = 0.01` on 2-digit gold ⇒ **$0.02/oz**)
- 2 gold “pips” (often **$0.10/oz** or **$0.01/oz**, broker-dependent)
- 2 ticks
- 2 dollars of **account P&L** (that would require multiplying by ounces)

The property name and default carry **no unit suffix**. Architecture law (A72) already called this out: *“no unit”* on the current check.

---

## 3. RiskEngine math (source of truth)

File: `D:\Prop\src\Domain\Risk\RiskEngine.cs`

Default:

```csharp
public decimal MaxAllowedSpread { get; init; } = 2.0m;
```

Check (lines 104–106):

```csharp
var spread = request.Quote.Ask - request.Quote.Bid;
if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
    return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");
```

Facts:

| Fact | Evidence |
|---|---|
| Spread is raw `Ask − Bid` | line 104; no `/ Point`, no `* 10^digits`, no tick count |
| Compared with `>` (equality passes) | line 105 |
| Only OPEN / INCREASE | `IsIncreasing` — REDUCE/CLOSE skip this guard |
| Crossed book (`Ask < Bid`) is **not** rejected as invalid | negative spread is `< 2.0` so it **approves** |
| `MaxSlippage` is unused | property `1.5m` never read |
| Same unit family as move guard | `|mid − ExpectedPrice| > MaxPriceMove` (`3.0m`) — also price, not points |

`DestinationQuote` (`Bid` / `Ask`) is a `decimal` price snapshot. There is no `Digits`, `Point`, or `TickSize` field on the quote or on `RiskLimits`.

Shadow path uses the same subtraction (`ShadowCopyEngine`: `Spread = quote.Ask - quote.Bid`) and does **not** reject on width.

---

## 4. What the quote numbers actually are

| Source | Bid / Ask | Implied spread |
|---|---|---|
| Unit fixture `RiskEngineTests.Base` | `2399.9` / `2400.1` | **0.20** |
| Risk fixture pack B36 | `2399.90` / `2400.10` | **0.20 ≪ 2.0** |
| A26 QUOTE card example | `2401.12` / `2401.28` | **0.16** |
| A23 spec | `spread = ask − bid` (same units as quote) | — |
| A72 §4.2 (binding design) | `spread = ask − bid  # dest price units (XAUUSD USD/oz on cTrader)` | default unit = **absolute dest price**, not points unless config explicitly says ticks |

A72 verbatim (already law for later work, **not** implemented as config):

> Units are **price**, not “points” and not MT5 points, unless `destination_symbols.tick_size` is measured and config explicitly says `ticks`. Default unit: **absolute dest price**.

`docs\risk.md` talks about “Slippage Tolerance 30 points” — that document is **stale / not the engine**. The live Domain type does **not** convert to points.

---

## 5. Why 2.0 is loose (loss translation)

XAUUSD cash is priced in **USD per ounce**. A **1.00 standard lot** is **100 oz** on typical MT5/cTrader gold CFDs (Pepperstone / most retail books). Round-trip (and the entry half-spread you pay as taker) scales as:

```text
spread_cost_usd ≈ (Ask − Bid) × ounces
ounces          ≈ lots × 100          # confirm per destination contract; do not hardcode at send time
```

| Book width (USD/oz) | Entry cost / 1.00 lot (100 oz) | Entry cost / 0.10 lot (fixture qty) | vs default cap `2.0` |
|---|---:|---:|---|
| 0.16 (A26 example) | $16 | $1.60 | passes by **12.5×** |
| 0.20 (lab fixture) | $20 | $2.00 | passes by **10×** |
| 0.50 (wide-but-normal) | $50 | $5.00 | passes by **4×** |
| **2.00 (the cap)** | **$200** | **$20** | **just rejects above this** |
| 2.50 (A72 news example) | $250 | $25 | rejects |

So if a live XAU book is often **0.20–0.50 USD**, `MaxAllowedSpread = 2.0` only fires on a **disaster / news blowout**, not on a merely expensive book. The operator can still **open** into a **$200/lot** half-spread. That is a **lower-loss hole**, not a tight pre-trade filter.

If someone *thought* `2.0` meant “2 points” on 2-digit gold (`Point=0.01`):

| Interpretation | Width | Cost / 1.00 lot |
|---|---:|---:|
| 2.0 **points** (0.01) | $0.02/oz | **$2** |
| 2.0 **pips** (0.10, some desks) | $0.20/oz | **$20** |
| 2.0 **USD/oz** (what the code does) | $2.00/oz | **$200** |

Misreading the default as points understates the allowed tax by **10–100×**.

`MaxPriceMove = 3.0` is the same trap: **$3/oz** (~$300/lot) mid-gap, not 3 points.

---

## 6. Related unit hazards (do not conflate)

| Symbol | What it is | Unit |
|---|---|---|
| `RiskLimits.MaxAllowedSpread` | `Ask − Bid` cap | **dest price (USD/oz on XAU)** |
| `RiskLimits.MaxPriceMove` | `|mid − ExpectedPrice|` | dest price (USD/oz) |
| `RiskLimits.MaxSlippage` | unused | intended dest price; never evaluated |
| `ShadowCopyEngine.DefaultLatencySlippagePoints` | `0.05m` added to dest price | **named “Points” but added as 0.05 price** (another unit lie) |
| `docs\risk.md` “30 points” | not wired to `RiskEngine` | ignore for this question |
| `CTraderFixOptions.MaxQuoteAgeMs = 5000` | age only | milliseconds; **not** bound to `MaxQuoteAge=3s` |

There is **no** symbol-specific spread table in `appsettings*.json`. The `2.0m` is a **hardcoded lab constant**, not measured Pepperstone config (A72, B13, D13).

---

## 7. Lower-loss recommendation (design only — product not edited)

Keep the **unit as USD per ounce** (absolute dest price). Do **not** switch the engine to “points” unless `tick_size` is measured per destination instrument and the config key says `ticks`.

Recommended production shape (A72 already specified this; still unimplemented):

```text
max_spread_open_xauusd_usd_per_oz     = <tail cut from dest tape, not the mean>
max_spread_increase_xauusd_usd_per_oz = same or slightly wider
max_spread_reduce_xauusd_usd_per_oz   = wider / advisory (CLOSE must not die on a fat book)
```

Suggested **starting** open cap after a dest-tape histogram (not a guess to ship blindly):

| Regime | Cap (USD/oz) | Approx max entry tax / 1.00 lot |
|---|---:|---:|
| Tight London cash | 0.35–0.50 | $35–$50 |
| Normal + buffer | **0.60–0.80** | $60–$80 |
| Current lab default | **2.00** | **$200** — reject as production |

Process:

1. Plot destination `ask−bid` distribution on the Pepperstone QUOTE tape (same way A72 §8 says for quote age).
2. Set `max_spread_open` as a **tail cut** (e.g. 99th percentile + small buffer), not the mean and not `2.0`.
3. Name the config key with the unit: `MaxAllowedSpreadUsdPerOz` / `MAX_SPREAD_OPEN_XAUUSD_USD`.
4. Reject crossed / non-positive books **before** width (`QUOTE_INVALID`), so a negative spread cannot sneak under the cap.
5. Confirm ounces-per-lot from destination contract size before quoting “$/lot” on the dashboard.

Do **not** treat `2.0` as “2 points” in UI copy. That would hide a $200/lot hole.

---

## 8. Honest status

- Engine **does** compare `Ask − Bid` to `2.0` and can emit `SPREAD_TOO_WIDE`.
- `Evaluate` is still a **lab path** (historically unused by Application/FIX send). This audit is about **units**, not go-live readiness.
- No product file was changed.

---

## 9. Paths cited

- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (defaults + `Ask − Bid`)
- `D:\Prop\src\Domain\Entities\DestinationQuote.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\tests\Unit\RiskEngineTests.cs` (book `2399.9` / `2400.1`)
- `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md`
- `D:\Prop\reports\swarm\20260818\A72_quote_guards.md` §4.2
- `D:\Prop\reports\swarm\20260818\B36_risk_fixtures.md`
- `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` (`spread: 0.16`)
