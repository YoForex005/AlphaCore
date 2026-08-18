# P500_BOOK_7 — XAUUSD copy cost: spread + slippage + 15s `MaxSourceSignalAge` reject. Scalps die.

| Field | Value |
|---|---|
| Slot | **7** |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_BOOK_7.md` |
| Date | 2026-08-18 |
| Agent | P500_BOOK_7 (senior quant / book; dest XAU copy cost) |
| Topic | XAUUSD copy cost = **dest spread + dest slippage + 15 s `MaxSourceSignalAge` reject**. Sub-15-minute gold **scalps die**. |
| Product source modified | **No.** This report is the only write. |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Live API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **blocked** (tool SSRF on `127.0.0.1`). Book numbers are the same-wave **measured** pin in `P500_PROFIT_SYNTHESIS.md` / `P500_S007`, not a re-probe. |
| Method | Full `read_file` of `RiskEngine.cs`, `ShadowCopyEngine.cs`, `CopyIntentExpiry.cs`, `CopyTradingService.cs`, `XauUsdOneToOneCopyPolicy.cs`, `EfTradingStore.PersistDemoShadowAsync`, `EfDashboardQueries.GetOverviewAsync`, `BaselineScorer` / `TraderStateMachine`, `CTraderFixOptions.MaxQuoteAgeMs`, `DemoSeeder` dest quote, `RiskEngineTests.Stale_signal_rejected`, architecture §36/§37/§63, `docs/trade-reconstruction.md`, `docs/risk.md`, `P500_S005`, `P500_S007`, `CREDENTIALS_AND_COPY_STATUS.md`. Grep `MaxSlippage` / `AverageHoldSeconds` / `SIGNAL_STALE` / `8463`. |

**Honesty rule:** wanting higher profit and lower loss does **not** create an edge. A TLS Logon (`35=A`) is not a fill. Copying all **8463** catalog logins would copy the `RISK_BLOCKED` losses (**−$241,580** source) onto one Pepperstone gold book. That is ruin, not a strategy.

---

## 0. Verdict

**SCALPS_DIE_AFTER_COSTS. DO_NOT_COPY_HOLDS_UNDER_15_MIN. COPY_ALL_8463_COPIES_RISK_BLOCKED. LIVE_DEST_PNL_0 (`SAFE_BY_ABSENCE`).**

XAUUSD copy expectancy is **not** source expectancy. Destination is a **taker** book. Three measured cost layers sit between an MT5 gold fill and a cTrader fill:

| Layer | What the tree actually does | Effect on a 90–180 s gold scalp |
|---|---|---|
| **Dest spread** | Long enters **ask**, exits **bid** (`ShadowCopyEngine`). Seed book **2399.45 / 2399.85** = **0.40**. Lab still **allows** `MaxAllowedSpread = 2.0`. | Round-trip = full dest spread. On 0.05 lot that is **$2.00** at the seed 0.40, **$10.00** at the allowed 2.0. Typical assigned 0.05-lot winners are **$0.35–$0.85**. Dest RT already **exceeds** the source capture. |
| **Dest slippage** | `MaxSlippage = 1.5` is declared and **never read** in `Evaluate`. Shadow overlay **+0.05** only if `modeledDelay > 250 ms`. Product shadow calls pass **80 ms**, so overlay **does not fire**. | Slippage is **un-guarded**. Demo SHADOW **understates** dest cost. A 1.5-point slip is **$7.50** on 0.05 lot / **$150** on 1.00 lot — same order as a 1–3 min gold target. |
| **15 s `SIGNAL_STALE`** | `RiskLimits.MaxSourceSignalAge = 15s`. OPEN/INCREASE with `DecisionTime − SourceEventTime > 15s` → `Reject` / `ApprovedQuantity = 0`. Settings API advertises `maxSignalAgeSeconds = 15`. Copy intents stamp `ExpiresAt = now + 15s` (service) or `OpenedAt + 15s` (demo persist). | **9.2%** of login **322947**’s **~163 s** average hold. After 15.001 s the open is **dropped**. After the source closes, policy also rejects with `NO_LOOKAHEAD_CLOSED_WINNER`. The remaining “fast” tail still pays dest spread. |

**Higher profit** is **not** “copy more logins” or “copy faster.” It is **not copying** a book whose entire life is inside dest RT + lag.

**Lower loss** is **not** tightening `MaxSourceSignalAge` to 2 s (that only rejects more opens; remaining fills still pay dest RT > source capture). It is **eligibility**: skip holds **&lt; 15 minutes**, skip `RISK_BLOCKED`, skip copy-all **8463**.

**Today dest capital is unharmed** (`SAFE_BY_ABSENCE`): `NewOrderSingleImplemented = false`, persist `AllowFixSend = false`, `VenueReconciled = false`, `CanPromoteToLive => false`, `OverviewDto.DestinationRealPnl` constructor literal **0**. The 15 s reject is **not** currently the first blocker on the product path (`VENUE_NOT_RECONCILED` fires first). That does **not** make scalps copyable later.

---

## 1. Measured book (same-wave live API, mid-scoring — not re-probed here)

Source pin: `P500_PROFIT_SYNTHESIS.md` §1 (`GET /api/health`, `/api/overview`, `/api/ingest/status`, `/api/fix/sessions`, `/api/traders`). Cross-check: `P500_S007_blocked_left_tail.md`, `CREDENTIALS_AND_COPY_STATUS.md`. This slot’s localhost fetch was **blocked**.

| Metric | Value | Note |
|---|---:|---|
| Catalog accounts (P500 overview) | **8463** | Achiever 6512 + Starwave ~1951 |
| Manager census (CREDENTIALS / INDEX) | **8460** | 8/6512 + 10/1948. **+3 unreconciled** — do not greenwash |
| XAU traders with a score | **197** (rising) | Achiever only |
| Starwave scored | **0** | phase `deals-done`; not a hold-time book |
| `SHADOW` | 70 | **100% demo**; source **+$78,276** |
| `WATCH` | 79 | source **+$8,178** |
| `RISK_BLOCKED` | **29** | **all** `martingale=true`; source **−$241,580** |
| `LIVE` / `LIVE_CANDIDATE` | 0 / 0 | `CanPromoteToLive => false` |
| `INSUFFICIENT_DATA` remainder | **~8284** | 8463 − 197 |
| All scored XAU source PnL | **−$154,425** | copy-all EV **at source** |
| Destination real PnL | **$0** | hardcoded, not a venue rollup |
| Shadow PnL (that pin) | **$0** | no live quote tape |
| `realCopyEnabled` | **false** | do not flip |
| FIX | LoggedOn | session paint, **not** a fill |

Copy-all EV is the scored tape: **negative six figures before dest spread**. Destination after costs is **worse**. The `RISK_BLOCKED` tail (−$241,580) is larger than the SHADOW+WATCH head (+$86,454).

Named scalp class (synthesis §2.4 + `P500_S005`; cards in `LIVE_GROUPS_AND_TRADERS.json`):

| Login | Group | XAU | Hold | Why dest dies |
|---|---|---:|---|---|
| **322947** | `demo\yo-payp` | 194 | **~163 s** | 15 s cap is 9.2% of the hold; dest RT ≥ 0.30 = **30 cents** of gold just to pay spread |
| **303274** | `demo\yo-2step` | 102 | **1–3 min**, same-second 0.05 grid | Assigned winners **$0.35–$0.85** on 0.05 lot = **7–17 cents** of gold — dest RT eats them |

Both are `demo\…`. Current `XauUsdOneToOneCopyPolicy` would refuse them as `DEMO_OR_CONTEST_GROUP`. That is **correct**. It is **not** a hold-time gate. A non-demo 163 s book with 20+ XAU and `XauNetPnl > 0` would still be **1:1 eligible**.

---

## 2. The three cost layers (quoted from this tree)

### 2.1 Spread — taker dest, not source mid

```33:59:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public const decimal DefaultLatencySlippagePoints = 0.05m;
    // long → dest Ask; short → dest Bid
    // if modeledDelay > 250 ms: raw ± 0.05
    // SourceVsShadowSlippage = dest_touch − source (signed adverse)
```

```105:111:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        db.DestinationQuotes.Add(new DestinationQuoteSnapshot
        {
            CanonicalSymbol = "XAUUSD",
            VenueInstrumentId = null,
            Bid = 2399.45m,
            Ask = 2399.85m,
```

Seed spread **0.40**. `VenueInstrumentId = null` — this is **not** a Pepperstone SecurityList id. It is the only dest book `DemoSeeder` writes.

`RiskEngine` spread guard (OPEN/INCREASE only):

```104:106:D:\Prop\src\Domain\Risk\RiskEngine.cs
            var spread = request.Quote.Ask - request.Quote.Bid;
            if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");
```

`MaxAllowedSpread = 2.0` (`RiskLimits` L14). A 0.30–0.40 dest gold book **passes**. The engine **approves the spread that erases the scalp**.

Contract (`docs/trade-reconstruction.md` L31–39): **1 lot = 100 oz**; $1/oz × 1 lot = **$100**. Round-trip dest spread dollars:

```text
RT_$ = (ask − bid) × 100 × lots
```

| Dest spread | 0.05 lot RT | 0.10 lot RT | 1.00 lot RT | vs 303274 $0.35 / $0.85 win |
|---|---:|---:|---:|---|
| 0.18 (tight retail, **not** measured here) | $0.90 | $1.80 | $18 | already **negative** vs $0.35 |
| **0.40 seed** | **$2.00** | **$4.00** | **$40** | **−$1.65 / −$1.15** |
| **2.00 allowed** | **$10.00** | **$20.00** | **$200** | dest may still **APPROVE** |

Worked 303274-shaped ticket (`P500_S005` §6, restated): source long 2340.00 → 2340.17 (17 cents, **+$0.85** on 0.05), hold 120 s, copy lag 4 s, dest already +0.10, dest exit −0.05 vs source out, half-spread 0.15:

```text
dest entry 2340.25    dest exit 2339.97    dest capture −0.28 oz    dest $ = −$1.40
source capture +0.17 oz                                           source $ = +$0.85
```

Wanting the source +$0.85 does **not** put +$0.85 on Pepperstone.

### 2.2 Slippage — declared, unread, or understated

```14:18:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
```

Grep of `src/` for `MaxSlippage` = **this property only**. `Evaluate` never compares dest touch vs source / expected. There is **no** `MAX_SLIPPAGE_EXCEEDED`. `PRICE_MOVED_TOO_FAR` is unsigned `|mid − expected| > 3.0` — a **$0.40** adverse gap (common in 3–15 s of gold) is **legal**.

Product shadow delay is **80 ms** (`CopyTradingService` L233, `EfTradingStore` L319). Overlay threshold is **`>` 250 ms**, so demo SHADOW records dest-touch only and **hides** the 0.05 overlay (`$5/lot`). A real FIX hop (TLS + risk + cServer RTT) is **not** 80 ms.

`docs/risk.md` still claims a **100–2000 ms** copy window and a **30-point** slip tolerance. Those numbers are **not** the `RiskLimits` defaults and are **not** wired. Do not plan dest edge off the markdown.

### 2.3 15 s `MaxSourceSignalAge` — the reject that kills the remaining life

```113:115:D:\Prop\src\Domain\Risk\RiskEngine.cs
        var signalAge = request.DecisionTime - request.SourceEventTime;
        if (signalAge > _limits.MaxSourceSignalAge && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "SIGNAL_STALE");
```

Unit fact (measured, green by construction):

```51:55:D:\Prop\tests\Unit\RiskEngineTests.cs
    public void Stale_signal_rejected()
    {
        var d = _e.Evaluate(Base(q => q with { SourceEventTime = q.DecisionTime.AddMinutes(-5) }));
        d.Reason.Should().Be("SIGNAL_STALE");
    }
```

`apps/api/Program.cs` L73 paints `maxSignalAgeSeconds = 15` on `/api/settings`. Architecture §36: *“For XAUUSD, stale trade copying can destroy expected edge.”* §63: TRADE down 3 minutes + 20 source opens → **0** `NewOrderSingle` on reconnect.

Product copy stamps (`CopyTradingService` L157–159, L287–289):

```text
SourceEventTime = trade.OpenedAt     (OPEN)  /  trade.ClosedAt  (CLOSE)
ExpiresAt       = now.AddSeconds(15)
DecisionTime    = now
```

`CopyIntentExpiry.IsExpired` is `now − sourceEventTime > maxSignalAge`. `Evaluate` **does not read `ExpiresAt`**. Demo persist is stricter on paper (`ExpiresAt = OpenedAt + 15s`) and then **bypasses risk** and still `SimulateEntry`s.

`IsIncreasing` = `OpenExposure | IncreaseExposure`. **CLOSE skips age, spread, and move.** Asymmetry:

| Arrival | OPEN | CLOSE |
|---|---|---|
| ≤ 15 s | may APPROVE (if other gates pass) → dest pays ask, remaining hold already shorter | n/a |
| &gt; 15 s | **`SIGNAL_STALE`**, qty 0 | **APPROVE** (`RISK_REDUCTION`) if a dest open exists |
| Source already flat | policy `NO_LOOKAHEAD_CLOSED_WINNER` | close-without-open residual |

Selection bias: **fast winners** finish inside 90–163 s and are often **already closed** (or &gt;15 s old) when the Manager poll lands. **Slow losers** stay open and are **more** likely to still be `!Completed` inside the 15 s window. Copying that arrival process is how a “profitable scalp source” becomes a **dest drain**.

Against 322947’s **163 s** average:

| source-to-decision | Fraction of hold gone | `SIGNAL_STALE`? |
|---|---:|---|
| 1 s (fantasy FIX) | 0.6% | no |
| 3 s (`MaxQuoteAge`) | 1.8% | no |
| 8 s (slow poll / Achiever HTTP proxy) | 4.9% | no |
| **15 s + 1 ms** | **9.2%** | **yes — OPEN dropped** |
| 30 s (stall / backfill) | 18% | **yes** |

`CTraderFixOptions.MaxQuoteAgeMs = 5000` is a **second, wider** quote clock. A 4.0 s quote is accepted by FIX QUOTE and rejected by `RiskEngine` (`QUOTE_STALE` at 3 s). Neither clock is a measured dest-edge horizon (A72 / A73).

---

## 3. Why the current copy path still cannot save a scalp

`GenerateShadowIntentsAsync` now only emits OPEN for **`!Completed`** trades (still open). That is the right anti-lookahead shape (`NO_LOOKAHEAD_CLOSED_WINNER` on closed winners). Combined with 15 s age it means:

```text
copyable OPEN window = first 15 s of a still-open XAU ticket
                      ∩  ingest + reconstruct + score already done
                      ∩  quote fresh ≤ 3 s
                      ∩  VenueReconciled (today: const false)
```

Today `VenueReconciled = false` → every increasing action is `VENUE_NOT_RECONCILED` **before** `SIGNAL_STALE`. Shadow fills require `quote != null && Outcome == Approve`. Historical / late / unreconciled opens **do not get a dest-touch mark**. Dashboard `destinationRealPnl` stays the constructor **0**.

`XauUsdOneToOneCopyPolicy` (read this slot):

| Gate | Present? | Helps scalps? |
|---|---|---|
| Block `RISK_BLOCKED` / DISQUALIFIED / PAUSED | **yes** | stops the −$241k tail **if this policy is the only sender** |
| Block WATCH / EARLY / INSUFFICIENT | **yes** | not a hold filter |
| Block martingale / averaging / lot-escalation | **yes** | 303274 same-second grid is **not** always flagged (`WasAveragedDown` is same-position scale-in only) |
| `CompletedXauTrades >= 20` | **yes** | 322947 (194) **passes** |
| `XauNetPnl > 0` | **yes** | source $, not dest $ after spread |
| Block `demo\` / `contest\` | **yes** | current SHADOW book **blocked** (good) |
| Hold-time floor | **no** | 163 s non-demo book is still 1:1 |
| `AllocationFactor` | **1.0** | dest lots = source lots (capped 5). **Worse** than the old 0.05 haircut |

`AverageHoldSeconds` is **computed** (`BaselineScorer` L96–120) and **never used** in `FromBaseline` (L189–206). A 163 s, 194-trade book can still paint `SHADOW` if quality ≥ 70 and risk &lt; 40.

`EfDashboardQueries.GetOverviewAsync` L29–44: Overview “Shadow P&L” = `Sum(ShadowOrders.SourceVsShadowSlippage)` — **slippage Σ, not PnL**. Dest real PnL is literal **0**.

---

## 4. Copy-all 8463 is the `RISK_BLOCKED` path

`FromBaseline`:

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

`RiskEngine.Evaluate` has **0** `TraderState` / `RISK_BLOCKED` tokens. Identity is on the request (`BrokerId`, `SourceLogin`) and **never read**. If an operator sprays the 8463 catalog past the policy, the engine will **APPROVE** any open that is inside caps and not flagged `MartingaleFlag`.

| If you copy… | What you inherit |
|---|---|
| All **8463** logins | ~8284 `INSUFFICIENT_DATA` + 29 `RISK_BLOCKED` (−$241,580) + 70 demo SHADOW + 79 WATCH + Starwave **unscored** |
| All **scored** XAU (197) | source **−$154,425** |
| All **SHADOW** (70) | +$78,276 **demo challenge** (pass-target, then many martingale the rest) + named scalps 322947 / 303274 |
| `RISK_BLOCKED` only | −$241,580 **plus** dest spread / lag |

Wanting the +$78k SHADOW headline does **not** cancel the −$241k tail. Net scored book is already **negative**. Dest costs make dest **more** negative.

---

## 5. What actually raises profit and cuts loss (measured, still no send)

Do these. Skipping them to “just send” is how the Pepperstone login dies.

1. **Do not copy holds under 15 minutes** (900 s). Research floor 5 minutes. `AverageHoldSeconds` must become a **copy gate**, not a painted feature. 322947 / 303274 class stays score-only.
2. **Do not copy `RISK_BLOCKED`.** Policy already refuses them. Do not add a “copy all scored” bypass. Do not copy the 8463 catalog.
3. **Keep `REAL_COPY_EXECUTION_ENABLED=false`** and do not add `35=D` until dest quote tape + shadow-after-costs is green. `SAFE_BY_ABSENCE` is the only dest-PnL protection today.
4. **Price dest as a taker.** Keep long=ask / short=bid. Stop passing **80 ms** into shadow when claiming dest expectancy. Use delay **&gt; 250 ms** (overlay on) against a **live** bid/ask, not `2399.45/2399.85`.
5. **Tighten gold spread / move / slip** *or* leave them unused and **refuse the book**. `MaxAllowedSpread = 2.0` / `MaxPriceMove = 3.0` / unread `MaxSlippage = 1.5` **admit** dest books larger than a 1–3 min gold target. Reading `MaxSlippage` without a hold floor still does not create edge.
6. **Do not 1:1** (`AllocationFactor = 1`) any gold ticket that has not proven dest EV after RT. Synthesis Stage D cap **0.05** lot dest is the conservative number; current policy **1.0** is a blow-up size if send is ever armed.
7. **Finish Starwave scoring** before treating 8463 as a universe. Unscored ≠ copyable.

**What higher profit is not:** more logins, higher lot caps, `earlyScore = 95.5`, FIX `LoggedOn`, or wanting the source +$0.85.

---

## 6. Honesty / non-claims

- This slot did **not** re-query `:5000`. Book $ / N are the same-wave measured pin. Census **8463 vs 8460** is unreconciled.
- Dest spread table uses the **seed 0.40** and the **allowed 2.0**. Live Pepperstone p50/p95 bid/ask were **null** on the synthesis pass. Even a **tight 0.18** already kills 303274 winners.
- 322947 / 303274 hold / ticket $ come from synthesis + `P500_S005` assigned live observations, not a deal-tape replay this slot. The **inequality** (dest RT ≫ source scalp capture) does not depend on replaying every ticket.
- `VENUE_NOT_RECONCILED` currently shadows `SIGNAL_STALE` on the product hop. Citing 15 s as “already rejecting live scalps” would be a **lie**. Citing 15 s as **what OPEN will do once reconciled** is the measured law.
- No product file was modified. No `35=D`. No `REAL_COPY` enable. No secrets.

---

## 7. One-line operating law

```text
source XAU scalp $  ≠  dest copy $
dest cost = full spread + unread slip + 15s SIGNAL_STALE
hold < 15 min → do not OPEN
copy all 8463 → copy RISK_BLOCKED (−$241,580) → dest ruin
wanting profit is not an edge
35=D stays OFF
```

**Slot 7 verdict: scalps die after dest spread + slippage + the 15 s reject. Lower loss = skip that book and never spray 8463. Higher profit = residual after costs on long-hold, non-blocked, non-demo XAU — unproven today. Dest PnL is $0.**

---

## 8. Paths cited

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 15 s `MaxSourceSignalAge`, 2.0 spread, unread 1.5 slip |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | dest ask/bid + 0.05 overlay &gt; 250 ms |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | unused-by-Evaluate 15 s predicate |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` | no hold gate; 1:1 lots; demo / `RISK_BLOCKED` refuse |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `ExpiresAt = now+15s`; 80 ms shadow; `VenueReconciled=false` |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `ExpiresAt = OpenedAt+15s`; shadow without `Evaluate` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dest PnL literal 0; shadow tile = Σ slippage |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | invented 2399.45 / 2399.85 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | hold computed, unused |
| `D:\Prop\apps\api\Program.cs` | `maxSignalAgeSeconds = 15` |
| `D:\Prop\tests\Unit\RiskEngineTests.cs` | `SIGNAL_STALE` at −5 min |
| `D:\Prop\docs\trade-reconstruction.md` | 100 oz / $100 per $1 / lot |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §36 / §37 / §63 | stale XAU destroys edge; no catch-up |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | 8463 / −$154,425 / −$241,580 |
| `D:\Prop\reports\swarm\20260818\P500_S005_gold_scalp_uncopyable.md` | 322947 / 303274 dest RT math |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | 18 / 8460; `35=D` OFF |
