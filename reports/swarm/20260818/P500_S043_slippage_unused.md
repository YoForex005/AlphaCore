# P500_S043 — `RiskLimits.MaxSlippage = 1.5` is unread in `Evaluate`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S043_slippage_unused.md` |
| Agent | P500_S043 (dead slippage limit) |
| Date | 2026-08-18 |
| Assigned | Confirm `MaxSlippage` 1.5 on `RiskLimits` is unused by `Evaluate()`. Dead limit = not reducing loss. **Do not edit product.** |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines) |
| Tests | `D:\Prop\tests\Unit\RiskEngineTests.cs` (87 lines, 5 facts; **zero** mention of `MaxSlippage`) |
| Product `Evaluate(` caller | `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L159 (`GenerateShadowIntentsAsync`) |
| Method | Full read of `RiskEngine.cs`. Grep `_limits.` (14 reads; **no** `MaxSlippage`). Grep product `*.cs`/`*.ts`/`*.tsx`/`*.json` for `MaxSlippage` (**one hit**: the property). Grep `MAX_SLIPPAGE_EXCEEDED` (specs/reports only). Contrast A23 §6.3, A72 §4.4, architecture §37/§39, `docs/risk.md` “30 points”. Nothing from memory. |

Classification: `MISSING` (`MAX_SLIPPAGE_EXCEEDED`) · `STUB_WRONG` (property exists, never consulted) · `SAFE_BY_ABSENCE` (live `35=D` still off; persist path forces `AllowFixSend = false`).

This file does **not** implement the guard. Product was **not** edited.

---

## 0. Verdict

**CONFIRMED. `MaxSlippage = 1.5m` is a dead field. `Evaluate` never reads it. A dead limit does not reduce loss.**

The engine can reject `PRICE_MOVED_TOO_FAR` against `MaxPriceMove = 3.0` (unsigned mid vs `ExpectedPrice`). That is **not** a slippage guard (A72: price-move ≠ slippage). It cannot emit `MAX_SLIPPAGE_EXCEEDED`. It cannot refuse a dest touch that is 1.51+ away from the source fill / modeled cost while still inside the 3.0 mid-move band.

`docs/risk.md` “Slippage Tolerance | 30 points” and post-fill suspend are a **different** story and are also **not** in this type.

A property that is never read cannot refuse a copy, cannot shrink quantity, cannot flatten, and cannot suspend. **Declared 1.5 ≠ enforced 1.5.** Until a predicate exists **and** sits on a send hop that actually writes `35=D`, this field reduces **zero** dollars.

Do **not** claim “slippage cap is 1.5,” “§37 max slippage is implemented,” or “copy is protected at 1.5.” The number is a compile-time default on an unread `init` property.

---

## 1. The field

```4:22:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed class RiskLimits
{
    public decimal MaxLossPerTrader { get; init; } = 500m;
    public decimal MaxDailyExecutionLoss { get; init; } = 2_000m;
    public decimal MaxPortfolioDrawdown { get; init; } = 3_000m;
    public decimal MaxXauGrossExposure { get; init; } = 20m;
    public decimal MaxXauNetExposure { get; init; } = 10m;
    public decimal MaxPositionQuantity { get; init; } = 5m;
    public int MaxOpenPositions { get; init; } = 20;
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
    public decimal MaxMarginUsage { get; init; } = 0.70m;
    public bool BlockMartingale { get; init; } = true;
    public bool BlockAbnormalSizing { get; init; } = true;
}
```

Facts:

| Fact | Evidence |
|---|---|
| Default | `1.5m` (line 18) |
| Type | `decimal` — **no unit suffix**, no `Point`, no `digits` |
| Config binding | **None.** Not in `appsettings`, env, Settings API, Redis keys, or web types |
| Product C# / TS / JSON readers besides this line | **0** |
| `Evaluate` access | **0** (`_limits.MaxSlippage` does not exist in the method) |

Unit is unspecified. Sibling `MaxAllowedSpread = 2.0` / `MaxPriceMove = 3.0` are compared as **raw dest price** (USD/oz on XAU fixtures). If 1.5 were ever compared the same way it would mean **$1.50/oz**, not 1.5 MT5 points ($0.015 if `Point=0.01`) and not `docs/risk.md`’s **30 points**. The field never reaches a comparison, so the unit is academic.

---

## 2. What `Evaluate` actually reads

`RiskEngine.Evaluate` (L76–172). Every `_limits.` access:

| Line | Field read | Reason if trip |
|---:|---|---|
| 101 | `MaxQuoteAge` | `QUOTE_STALE` |
| 105 | `MaxAllowedSpread` | `SPREAD_TOO_WIDE` |
| 109 | `MaxPriceMove` | `PRICE_MOVED_TOO_FAR` |
| 114 | `MaxSourceSignalAge` | `SIGNAL_STALE` |
| 117 | `MaxLossPerTrader` | `MAX_LOSS_PER_TRADER` |
| 120 | `MaxDailyExecutionLoss` | `MAX_DAILY_EXECUTION_LOSS` |
| 123 | `MaxPortfolioDrawdown` | `MAX_PORTFOLIO_DRAWDOWN` |
| 126 | `MaxOpenPositions` | `MAX_OPEN_POSITIONS` |
| 129 | `MaxPositionQuantity` | `MAX_POSITION_QUANTITY` |
| 132 | `MaxXauGrossExposure` | `MAX_XAU_GROSS` |
| 135 | `MaxXauNetExposure` | `MAX_XAU_NET` |
| 138 | `MaxMarginUsage` | `MAX_MARGIN_USAGE` |
| 141 | `BlockMartingale` | `MARTINGALE_BLOCK` |
| 144 | `BlockAbnormalSizing` | `ABNORMAL_SIZING_BLOCK` |

**14 of 16 `RiskLimits` members are consulted. `MaxSlippage` is the 15th member and is absent from this table.** (There is no 16th unused numeric besides this one; the two bools are used.)

Quote-quality block that **is** implemented (L98–110) — mid-move, not slippage:

```csharp
var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
    return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
```

There is **no** sibling of the form:

```text
expected_slippage > _limits.MaxSlippage  →  MAX_SLIPPAGE_EXCEEDED
```

`Reject(...)` reason catalog (19 strings) does **not** include `MAX_SLIPPAGE_EXCEEDED`. First-match cannot emit a reason that has no branch.

Unread on the request/quote side (adjacent dead inputs, not this slot’s primary): `DestinationQuote.VenueTimestamp`, `BrokerId`, `SourceLogin`. Empty `if (RealExecutionEnabled == false && Action != CloseExposure)` (L90–93) is a comment-only stub.

---

## 3. Spec vs code

### 3.1 Architecture / A23 / A72 (intended)

Architecture §39 lists `max slippage` as a hard limit next to `max tolerated price move`.

A23 §6.3 (independent predicates):

```text
|current_quote - expected| > max_tolerated_price_move   → PRICE_MOVED_TOO_FAR
expected slippage > max_slippage                         → MAX_SLIPPAGE_EXCEEDED
```

A72 §4.4: price-move answers “did dest move vs what we expected?” Slippage answers “is the dest touch too far from the **source** fill / modeled cost?” Both can fire on the same tick.

A72 intended OPEN/INCREASE reject:

```text
if expected_slippage > max_slippage:
    OPEN/INCREASE → REJECT  MAX_SLIPPAGE_EXCEEDED
```

E005 R043: `MISSING` — `MaxSlippage=1.5` **never read**. Price-move ≠ slippage.

### 3.2 Marketing docs (also not this code)

| Doc claim | Product |
|---|---|
| `docs/risk.md`: Slippage Tolerance **30 points**; `abs(mt5_price - ctrader_price) / point_size`; flag + **automatic copy-trade suspension** | **No** point conversion, **no** post-fill path, **no** suspend latch, **no** 30 |
| `docs/ctrader-fix.md`: “Flag slippage exceeding threshold (default: 30 points)” | Recon copy. **No** such flag in `RiskEngine` |

Two numbers in the tree (1.5 vs 30 points) already disagree. **Neither is evaluated.**

### 3.3 Shadow records slippage; it does not gate

`ShadowCopyEngine` writes `SourceVsShadowSlippage` after a **simulated** fill (`DefaultLatencySlippagePoints = 0.05m` adverse). It does **not** compare that value to `RiskLimits.MaxSlippage`. `CopyTradingService` only calls `SimulateEntry` when `decision.Outcome == Approve`. A missing slippage reject cannot stop a shadow fill that already passed mid-move.

---

## 4. Counterfactual: why unread 1.5 does not cut loss

Assume a live hop existed and called this `Evaluate` **before** `35=D` (it does not send today).

Worked XAU example (same units as fixtures: dest USD/oz). Buy / long, source fill = expected = `2400.00`.

| Dest bid / ask | Mid | `\|mid − 2400\|` vs `MaxPriceMove=3.0` | Taker ask vs source | Would 1.5 slippage reject **if wired**? | What `Evaluate` does **today** |
|---|---:|---|---:|---|---|
| 2399.90 / 2400.10 | 2400.00 | 0.00 pass | +0.10 | pass | `APPROVED` (if other caps pass) |
| 2400.80 / 2401.20 | 2410.00 wait: mid 2401.00 | 1.00 pass | +1.20 | pass (1.20 ≤ 1.5) | `APPROVED` |
| 2401.10 / 2401.60 | 2401.35 | 1.35 pass | **+1.60** | **REJECT `MAX_SLIPPAGE_EXCEEDED`** | **`APPROVED`** — 1.60 < 3.0 mid-move |
| 2402.40 / 2402.80 | 2402.60 | 2.60 pass | **+2.80** | **REJECT** | **`APPROVED`** — still under 3.0 |
| 2403.40 / 2403.80 | 2403.60 | 3.60 fail | +3.80 | REJECT (either) | `PRICE_MOVED_TOO_FAR` only |

The band **1.5 < adverse taker < 3.0 mid-gap** is exactly where a real slippage cap would refuse new/increased gold and the current engine **approves**. That is worse entry / worse dest fill vs source — **loss not reduced**.

Further holes even if someone later reads the field naively:

- No source-fill field on `RiskEvaluationRequest` distinct from `ExpectedPrice` (A72: expected dest ≠ source unless mapping says so).
- No side/taker-touch (unsigned mid is used for move). Favorable vs adverse not distinguished.
- REDUCE/CLOSE would need record-only (A72 table); there is no slippage branch to be family-aware.
- Units unbound; shipping `1.5` as “points” or “dollars” without measurement is a second defect (A72: do not ship these lab defaults).

**Dead limit = not reducing loss.** A number on a POCO is not a control.

---

## 5. Call path today (does not rescue the field)

Older reports (A72/B13/D13/E005) said product `Evaluate(` callers = 0. **That is stale.** Measured 2026-08-18 this slot:

| Site | Role |
|---|---|
| `RiskEngine.Evaluate` L76 | definition |
| `tests/Unit/RiskEngineTests.cs` | 5 facts; no slippage case |
| `CopyTradingService.GenerateShadowIntentsAsync` L159–183 | **does** call `Evaluate` |

That call does **not** make `MaxSlippage` live:

1. `CopyTradingService` holds `private readonly RiskEngine _risk = new();` — default limits, including unread 1.5. DI also `AddSingleton<RiskEngine>()` (`DependencyInjection.cs` L45); the hosted copy service **does not inject it**.
2. Request always `Reconciled = VenueReconciled` and `VenueReconciled = false` (const). Increasing actions therefore hit `VENUE_NOT_RECONCILED` **before** any quote-quality check. Slippage would still be unreachable even if wired, on this caller.
3. Persist: `AllowFixSend = false` **hardcoded** on `RiskDecisionRecord` (L192), ignoring `decision.AllowFixSend`.
4. `NewOrderSingleImplemented = false`. The only “live” branch sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. No `35=D`.

So: engine is now on a **shadow intent** hop, still not on a send hop, and still does not read `MaxSlippage`. Live dest P&L cannot be improved by a field the predicate never sees.

Settings API (`SettingsController`) exposes `RiskEngine:MaxDailyDrawdownPct` / `MaxPositionSize` / `MaxOpenPositions` / `KillSwitchEnabled` into Redis keys **nothing** binds into `RiskLimits`. **No MaxSlippage key.** Web has **0** `MaxSlippage` / `maxSlippage` hits.

---

## 6. Tests

`RiskEngineTests` facts: `QUOTE_STALE`, `Real_flag_false_never_allows_fix_send`, stop-new open vs close, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE`.

No fact:

- documents that `MaxSlippage` is ignored (`CURRENT_STUB`)
- asserts `MAX_SLIPPAGE_EXCEEDED` (would fail today)
- constructs `new RiskLimits { MaxSlippage = … }`

C03 already required those two facts. Still absent.

---

## 7. Loss-reduction recommendation (not implemented here)

When (and only when) a persist-before-send hop exists:

1. Bind **one** measured `max_slippage` from config. Do not ship `1.5` or docs’ `30 points` as production without units + measurement.
2. Compute **signed adverse** dest taker vs source fill (and/or modeled cost). Independent of quote age and of `MaxPriceMove` (A23 §6.3 / A72 §4.4).
3. OPEN/INCREASE → `REJECT` `MAX_SLIPPAGE_EXCEEDED`. REDUCE/CLOSE → record only.
4. Optional post-fill: persistent breach → stop-new (not flatten). That is a different latch; do not pretend `Evaluate` does it.

Until then, **delete-or-read**: an unread 1.5 is theater. This slot does **not** add the branch.

---

## 8. Honesty close

| Claim | Status |
|---|---|
| `RiskLimits.MaxSlippage` default 1.5 | **True** (L18) |
| `Evaluate` uses it | **False** |
| Engine can emit `MAX_SLIPPAGE_EXCEEDED` | **False** |
| Mid-move `MaxPriceMove=3.0` substitutes | **False** (different predicate, looser band) |
| Docs 30-point post-fill suspend | **Not this code** |
| Dead limit reduces dest loss | **False** |
| Live `35=D` | **Absent** (`SAFE_BY_ABSENCE`) |
| Product edited this slot | **No** |

**DONE (report only).** Reviewer of this file: confirm L18 exists, confirm `_limits.MaxSlippage` has zero reads, confirm no `MAX_SLIPPAGE_EXCEEDED` in `RiskEngine.cs`.
