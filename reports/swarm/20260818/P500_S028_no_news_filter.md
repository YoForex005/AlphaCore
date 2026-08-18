# P500_S028 — No news / session filter in RiskEngine

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S028_no_news_filter.md` |
| Agent | P500_S028 (session / news calendar gate) |
| Date | 2026-08-18 |
| Assigned | Read RiskEngine for session/news filters (likely none). Gold during NFP/FOMC is a loss spike. Confirm no news calendar gate. Lower-loss note: block copy in first minutes of high-impact USD events once live exists. **Do not edit product.** |
| Product source edited | **No** |
| Test source edited | **No** |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines) |
| SUT SHA-256 (prior measured, D102/E005) | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| Tests | `D:\Prop\tests\Unit\RiskEngineTests.cs` (87 lines, 5 facts) |
| Adjacent (not wired) | `D:\Prop\mt5-sdk` news calendar probe/types only |
| Method | Full read of `RiskEngine.cs`, `RiskLimits`, `RiskEvaluationRequest`, `RiskDecision`/`RiskDecisionOutcome`, `RiskEngineTests`, Settings risk keys. Grep `src\` for `news`/`NFP`/`FOMC`/`calendar`/`session filter` (zero hits). Grep product `Evaluate(` callers (zero outside the type). Contrast architecture §37 / A23 §6.1. Nothing from memory. |

Classification: `MISSING` (calendar / session blackout) · `PARTIAL` (spread / quote-age / mid-move as **reactive** proxies) · `SAFE_BY_ABSENCE` (live `35=D` still off; `Evaluate` has **zero** product callers).

This file does **not** implement a gate. It records the measured absence and the loss-reduction recommendation for **after** a live copy path exists.

---

## 0. Verdict

**CONFIRMED. `RiskEngine` has no session filter and no news-calendar gate.**

There is no request field, limit, reason code, clock window, currency filter, or impact grade that would reject `OpenExposure` / `IncreaseExposure` because NFP, FOMC, CPI, or any other high-impact USD print is inside a blackout.

Gold (XAUUSD) around those prints is a known loss spike. Architecture §37 names “XAUUSD around news” as the **motivation** for quote-age / spread / price-move guards. A23 §6.1 states v1 **does not** special-case a news calendar — those microstructure checks are the intended stand-in.

Those stand-ins are **not** a calendar:

| Control | What it does | What it does **not** do |
|---|---|---|
| `QUOTE_STALE` (`MaxQuoteAge` = 3 s) | Rejects increasing if last quote is old | Does not know event time. A 200 ms quote during NFP still passes. |
| `SPREAD_TOO_WIDE` (`MaxAllowedSpread` = 2.0) | Rejects if `Ask-Bid` already > 2.0 | Fires only **after** the book is already wide. Tight-but-violent reprints pass. |
| `PRICE_MOVED_TOO_FAR` (`MaxPriceMove` = 3.0) | Rejects if mid vs `ExpectedPrice` > 3.0 | If the source fill **is** the new price, mid ≈ expected. Spike copies through. |
| `SIGNAL_STALE` (15 s) | Rejects late source events | A source that trades **into** the print is fresh, not stale. |

**Lower loss (recommendation only — not implemented, not scheduled here):** once a live copy path exists, **block copy of new / increased XAU exposure for the first minutes of scheduled high-impact USD events** (NFP, FOMC, CPI, and the same-grade USD set). Do **not** invent this gate in product now. Live send is still `SAFE_BY_ABSENCE`. Adding a calendar before a live orchestrator would be theater.

**Do not claim** “news filter exists,” “session filter exists,” or “§37 news protection is implemented.” Spread/age/move are not a calendar.

---

## 1. What `Evaluate` actually gates

Read in full: `D:\Prop\src\Domain\Risk\RiskEngine.cs`.

### 1.1 `RiskLimits` — no calendar / session knobs

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

Absent from this type (and from `RiskEvaluationRequest` below): event id, event time, impact, currency, country, `InNewsWindow`, `SessionName`, London/NY/Asia window, `MinutesToEvent`, `MinutesAfterEvent`, `BlockUsdHighImpact`.

`MaxSlippage` is declared and **never read** by `Evaluate`. It is not a news filter either.

### 1.2 `RiskEvaluationRequest` — no news / session input

```32:56:D:\Prop\src\Domain\Risk\RiskEngine.cs
public sealed record RiskEvaluationRequest
{
    public required string CopyIntentId { get; init; }
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required CopyIntentAction Action { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal ExpectedPrice { get; init; }
    public required DateTimeOffset SourceEventTime { get; init; }
    public required DateTimeOffset DecisionTime { get; init; }
    public required DestinationQuote? Quote { get; init; }
    public required bool VenueHealthy { get; init; }
    public required bool RealExecutionEnabled { get; init; }
    public required bool Reconciled { get; init; }
    public required KillSwitchMode KillSwitch { get; init; }
    public required decimal TraderRealizedLoss { get; init; }
    public required decimal DailyExecutionPnl { get; init; }
    public required decimal PortfolioDrawdown { get; init; }
    public required decimal CurrentGrossXau { get; init; }
    public required decimal CurrentNetXau { get; init; }
    public required int OpenPositions { get; init; }
    public required decimal MarginUsage { get; init; }
    public required bool MartingaleFlag { get; init; }
    public required bool AbnormalSizing { get; init; }
}
```

`SourceEventTime` / `DecisionTime` exist only to compute **signal age** (stale copy), not “minutes from NFP.” A caller **cannot** pass a calendar fact into this engine.

### 1.3 Reason codes actually emitted

First-match order in `Evaluate` (L76–171):

| Reason | Outcome | Family |
|---|---|---|
| `STOP_NEW_EXECUTION` | `GlobalStop` | kill |
| `EMERGENCY_FLATTEN_BLOCKS_NEW` | `GlobalStop` | kill |
| `VENUE_NOT_RECONCILED` | `Reject` | venue |
| `VENUE_UNHEALTHY` | `PauseVenue` | venue |
| `QUOTE_MISSING` | `Reject` | quote |
| `QUOTE_STALE` | `Reject` | quote age |
| `SPREAD_TOO_WIDE` | `Reject` | quote |
| `PRICE_MOVED_TOO_FAR` | `Reject` | quote vs expected |
| `SIGNAL_STALE` | `Reject` | source age |
| `MAX_LOSS_PER_TRADER` | `PauseTrader` | loss |
| `MAX_DAILY_EXECUTION_LOSS` | `GlobalStop` | loss |
| `MAX_PORTFOLIO_DRAWDOWN` | `GlobalStop` | loss |
| `MAX_OPEN_POSITIONS` | `Reject` | book |
| `MAX_POSITION_QUANTITY` | `Reject` | size |
| `MAX_XAU_GROSS` | `Reject` | book |
| `MAX_XAU_NET` | `ReduceSize` | book |
| `MAX_MARGIN_USAGE` | `Reject` | margin |
| `MARTINGALE_BLOCK` | `PauseTrader` | behavior |
| `ABNORMAL_SIZING_BLOCK` | `Reject` | behavior |
| `RISK_REDUCTION` | `Approve` | reducing passthrough |
| `APPROVED` | `Approve` | default |

**Zero** of: `NEWS_BLACKOUT`, `HIGH_IMPACT_USD`, `NFP`, `FOMC`, `SESSION_CLOSED`, `SESSION_FILTER`, `EVENT_WINDOW`, `CALENDAR_BLOCK`.

`RiskDecisionOutcome` (`D:\Prop\src\Domain\Enums\RiskDecisionOutcome.cs`) is `{Approve, ReduceSize, Reject, PauseTrader, PauseVenue, GlobalStop}`. No news/session outcome.

`KillSwitchMode` is `{None, StopNewExecution, EmergencyFlatten}`. Operator/engine stop is **not** an event calendar.

### 1.4 Microstructure block that is the only news-adjacent code

```98:115:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.Quote is not null)
        {
            var age = request.DecisionTime - request.Quote.ReceivedAt;
            if (age > _limits.MaxQuoteAge && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_STALE");

            var spread = request.Quote.Ask - request.Quote.Bid;
            if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");

            var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
            if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
        }

        var signalAge = request.DecisionTime - request.SourceEventTime;
        if (signalAge > _limits.MaxSourceSignalAge && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "SIGNAL_STALE");
```

All four predicates are `IsIncreasing` only. `CloseExposure` / `ReduceExposure` skip them and fall through to `RISK_REDUCTION` (L152–161). That matches A23 close-family direction (do not trap exits with entry news guards). It also means **during NFP a source that opens will be evaluated with no calendar, and a source that closes will be approved as reduction** — the dangerous side is the open.

Approve path has no extra clock:

```164:171:D:\Prop\src\Domain\Risk\RiskEngine.cs
        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
```

`AllowFixSend` is `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). No calendar conjunct.

---

## 2. Grep evidence (product)

| Query | Scope | Hits |
|---|---|---|
| `news` / `NFP` / `FOMC` / `calendar` / `high.?impact` / `NewsFilter` / `SessionFilter` | `D:\Prop\src\` `*.{cs,json,md}` | **0** |
| same tokens | `D:\Prop\src\Domain\Risk\` | **0** |
| `Evaluate(` product callers | `D:\Prop\src\` | **only** `RiskEngine.Evaluate` definition |
| Settings risk keys | `apps\api\Controllers\SettingsController.cs` | `MaxDailyDrawdownPct`, `MaxPositionSize`, `MaxOpenPositions`, `KillSwitchEnabled` — **no** news/session |

`docs\risk.md` lists daily/total loss, position size, open positions, daily trades, slippage, copy delay 100–2000 ms. **No** calendar. That 100–2000 ms window is a copy-latency rule, not a session filter, and it is **not** implemented inside `Evaluate` (`MaxSourceSignalAge` is 15 s, not 2 s).

---

## 3. What exists *near* news — not a RiskEngine gate

| Artifact | Role | Wired to `Evaluate`? |
|---|---|---|
| Architecture §37 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1452) | “especially important for XAUUSD around news” as **why** `PRICE_MOVED_TOO_FAR` / `QUOTE_STALE` / `SPREAD_TOO_WIDE` exist | Motive text only |
| A23 §6.1 | Explicit: v1 does **not** special-case a news calendar; spread + quote age are the controls | Spec of absence |
| `mt5-sdk` `GetNewsCalendarItems` / `mt5_news_calendar_probe.cpp` / `mt5_news_calendar_test.cpp` | Manager API `NewsTotal` / `NewsNext` probe. Vendor `FLAG_CALENDAR`. Pump `PUMP_MODE_NEWS` | **No.** C++ probe / HTTP client. Zero C# `RiskEngine` call |
| `mt5_time_window` | Deal lookback `from`/`to` for ledger ingest | Query window, **not** a trading session filter |
| `ShadowCopyEngine` | Latency slippage 0.05 after 250 ms modeled delay | No calendar |
| Settings / Redis | Risk % / size / flags | No event list |

The SDK news calendar is **observability of MT5 NEWS topics**, not an economic-event SoT (no impact grade, no USD filter, no “first 5 minutes after NFP”). Even if a future worker ingested it, `RiskEvaluationRequest` has nowhere to put the fact.

---

## 4. Tests do not cover news / session

`D:\Prop\tests\Unit\RiskEngineTests.cs` — five facts:

1. `Stale_quote_rejects_open` → `QUOTE_STALE`
2. `Real_flag_false_never_allows_fix_send`
3. `Stop_new_execution_blocks_opens_not_closes`
4. `Unreconciled_venue_blocks_new_exposure` → `VENUE_NOT_RECONCILED`
5. `Stale_signal_rejected` → `SIGNAL_STALE`

Fixture (`Base`) uses `2026-08-18T12:00:00Z` and `XAUUSD` 2399.9 / 2400.1. No event clock. No NFP date. No session name. **Zero** facts named news / FOMC / calendar / session.

E005 matrix: 3 of 21 reasons have any unit assert. News reasons are not in the 21.

---

## 5. Why gold + NFP/FOMC is a loss spike this engine will not stop

Typical failure mode this SUT would **approve**:

1. Source trader is in the market (or enters) in the first 1–3 minutes after NFP / FOMC.
2. Destination quote `ReceivedAt` is milliseconds old → `QUOTE_STALE` false.
3. Spread may still be inside 2.0 (or already the new tight reprint) → `SPREAD_TOO_WIDE` false.
4. `ExpectedPrice` is the source fill at the new print → mid delta < 3.0 → `PRICE_MOVED_TOO_FAR` false.
5. Signal age < 15 s → `SIGNAL_STALE` false.
6. Loss / DD / martingale / size caps may still be green on the **first** spike trade.
7. Outcome `APPROVED`. If a future live path sets `RealExecutionEnabled=true`, `AllowFixSend` can become true **during the spike**.

Reactive caps (`MaxLossPerTrader` 500, `MaxDailyExecutionLoss` 2000, `MaxPortfolioDrawdown` 3000) fire **after** the money is gone. They are not a pre-event blackout.

`MaxAllowedSpread=2.0` / `MaxPriceMove=3.0` units are not documented as dollars vs points. On XAU they may be too loose or too tight; they are still not “NFP + 5 minutes.”

---

## 6. Lower-loss recommendation (after live exists — do not code now)

**Intent:** cut the cluster of gold copy losses that sit in the first minutes of scheduled high-impact USD events.

**When to implement:** only after a real copy orchestrator exists (`Evaluate` is called, persist-before-send, `REAL_COPY` still default false, kill switch tested). Implementing a calendar into an uncalled stub does not lower live loss.

**Recommended gate (design note, not a patch):**

| Item | Suggestion |
|---|---|
| Scope | `OpenExposure` / `IncreaseExposure` on XAU (canonical gold) only |
| Family | Do **not** block `CloseExposure` / `ReduceExposure` (same as other entry guards) |
| Events | High-impact **USD**: NFP (Nonfarm), FOMC statement / rate, CPI, maybe GDP / Core PCE — operator-configurable list |
| Window | `[T-N, T+M]` minutes; start with something like `N=1`, `M=5` (first minutes after the print). Exact N/M is a measured live parameter, not a guess to ship today |
| Reason | `NEWS_BLACKOUT` / `HIGH_IMPACT_USD` — new reason string, first-match **before** `APPROVED` |
| Clock | Venue or UTC event time from a **trusted calendar feed**, not MT5 NEWS subject strings |
| Fail closed | Missing calendar, stale calendar, or clock skew → treat as in-window for increasing XAU (or pause venue). Do not fail open |
| SoT | External economic calendar (or broker calendar with impact+currency). Do **not** treat `mt5-sdk` `NewsTotal` topics as NFP |
| Live conjunction | Same as other increasing rejects: `AllowFixSend=false` |
| Tests required | In-window open reject; out-of-window approve; close still `RISK_REDUCTION`; missing feed fail-closed; non-USD event does not block; non-XAU (if any) policy explicit |

**Not recommended:** using only wider spread / tighter `MaxPriceMove` as a substitute. Those still copy the first tight reprint into the spike.

**Also not recommended:** a generic “London/NY session filter” as the NFP fix. NFP is a **clocked event**, not a session. Session hours can be a later, separate control (Asia thin book, weekend gap). That control is also **absent** today.

---

## 7. Honesty / go-live boxes

| Claim | Status |
|---|---|
| Session filter in `RiskEngine` | **MISSING** |
| News calendar gate in `RiskEngine` | **MISSING** |
| §37 / A23 v1: spread + quote age as news stand-in | **PARTIAL** (code exists; not a calendar; `Evaluate` uncalled) |
| MT5 news calendar as risk SoT | **DEAD / adjacent probe only** |
| Live capital at risk from this gap **today** | **NONE** — `SAFE_BY_ABSENCE` (no product `Evaluate` caller, no NOS from this unit) |
| Live capital at risk **if** copy is enabled without this gate | **YES** — first minutes of USD high-impact on gold |
| Product edited this slot | **No** |

Go-live remains unchecked for reasons owned by E005/A23. This slot adds one more **unchecked** future box, not a present control:

```text
[ ] high-impact USD news blackout rejects XAU open/increase (calendar + fail-closed)
```

---

## 8. One-line answer

**No news calendar. No session filter. Gold NFP/FOMC spike would copy if live were on. Lower loss later = blackout the first minutes of high-impact USD events; do not ship that into product in this slot.**
