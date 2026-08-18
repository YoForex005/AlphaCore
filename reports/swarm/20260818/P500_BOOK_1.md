# P500_BOOK_1 — RiskEngine reject reasons that cut dest loss if live send existed

| Field | Value |
|---|---|
| Slot | **1** |
| Agent | P500_BOOK_1 (senior quant / trading-systems) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Topic | Read `RiskEngine.cs`. List every reject reason that reduces loss **if live send existed**. |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (189 lines: `RiskLimits`, `DestinationQuote`, `RiskEvaluationRequest`, `RiskDecision`, `RiskEngine`) |
| SHA-256 (D13/E005 pin; file still 189 lines, same 19 `Reject()` strings) | `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D` |
| Adjacent | `BaselineScorer.FromBaseline`, `CopyTradingService.GenerateShadowIntentsAsync`, `RiskEngineTests` (5 facts), `EfDashboardQueries.GetOverviewAsync`, `P500_PROFIT_SYNTHESIS.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live `35=D` sent | **No** |
| `REAL_COPY` flipped | **No** |
| Secrets printed | **None** |
| Local API this slot | `GET http://127.0.0.1:5000/api/overview` and `/api/traders` **not re-probed** (agent HTTP client SSRF-blocks loopback). Book integers are the same-day live pin in `P500_PROFIT_SYNTHESIS.md`. |

**Honesty rule:** wanting higher profit and lower loss does **not** create an edge. A reject string is not a fill. Copying all **8463** logins onto one Pepperstone cTrader book would copy the `RISK_BLOCKED` left tail (**−$241,580** source XAU). `RiskEngine` never reads `TraderState`. Dest is unharmed today only because there is **no** NewOrderSingle (`SAFE_BY_ABSENCE`), not because these reasons are a working live gate.

Classification: **16 / 19** `Reject()` reasons would cut **new-exposure** dest loss if they actually sat in front of a sender with honest snapshots. **3 / 19** also freeze exits and would **increase** trapped dest loss. **0 / 19** is `TRADER_RISK_BLOCKED`. Copy-all remains **−EV**. Not a go-live PASS.

---

## 0. Direct answer

If live send existed, dest loss is reduced by **refusing new gold** when the venue, quote, signal, size, or trader-flag check fails — **not** by spraying more of the 8463-login catalog.

| User ask | Measured answer |
|---|---|
| Higher profit | **Not from these rejects.** Rejects do not create expectancy. Profit is a **tiny filtered** XAU subset that is still +EV **after** Pepperstone spread/slip, proven in shadow on a standing quote tape. Copy-all scored XAU is already **−$154,425** at source. |
| Lower loss (today) | **Do not send.** `NewOrderSingleImplemented=false`. Persist `AllowFixSend=false`. `CanPromoteToLive => false`. `destinationRealPnl` is a constructor **0**. |
| Lower loss (if live send existed) | Fire the **16 new-exposure rejects** below. Do **not** copy `RISK_BLOCKED` / demo-challenge / first-3 luck / same-second grids. Never honor the three book-loss rejects as close blockers. |
| Copy all 8463 | **Copies `RISK_BLOCKED` losses.** Engine cannot stop that by state. `MARTINGALE_BLOCK` only helps if the caller sets `MartingaleFlag`. |

```text
Wanting profit ≠ edge.
8463 logins ≠ 8463 copy candidates.
RISK_BLOCKED is a scorer state, not a RiskEngine reason.
Dest PnL today = $0 because 35=D is absent, not because Evaluate is a gate.
```

---

## 1. What `Evaluate` actually emits

`Reject()` is the only reject factory. It always sets `ApprovedQuantity = 0` and `AllowFixSend = false`.

```180:188:D:\Prop\src\Domain\Risk\RiskEngine.cs
    private static RiskDecision Reject(RiskEvaluationRequest request, RiskDecisionOutcome outcome, string reason) =>
        new()
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = outcome,
            ApprovedQuantity = 0,
            Reason = reason,
            AllowFixSend = false
        };
```

There are **19** `return Reject(...)` sites and **2** approve fall-throughs (`RISK_REDUCTION`, `APPROVED`). `MaxSlippage` is declared and **never read**. The empty `if` on lines 90–93 (`RealExecutionEnabled == false`) is **not** a reject.

`RiskEvaluationRequest` has **no** `TraderState`, **no** `RISK_BLOCKED`, **no** `CompletedXauTrades`. `BrokerId` / `SourceLogin` are unused. Spec token `TRADER_RISK_BLOCKED` (A23 §4.3) is **never emitted**.

```194:195:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
```

That is the class a copy-all of 8463 would drag onto one dest gold book.

---

## 2. Complete reject catalog (measured, first-match order)

Family: `IsIncreasing` = `OpenExposure` \| `IncreaseExposure`. `IsReducing` = `ReduceExposure` \| `CloseExposure`.

| # | Reason | Outcome | Fires on | Default trip | Cuts **new** dest loss if live send? | Traps **close**? |
|---:|---|---|---|---|---|---|
| 1 | `STOP_NEW_EXECUTION` | `GlobalStop` | increasing | kill = `StopNewExecution` | **YES** — stop-new is the correct dest brake | no |
| 2 | `EMERGENCY_FLATTEN_BLOCKS_NEW` | `GlobalStop` | increasing | kill = `EmergencyFlatten` | **YES** — blocks add-during-panic | no (flatten itself is **MISSING**) |
| 3 | `VENUE_NOT_RECONCILED` | `Reject` | increasing | `Reconciled=false` | **YES** — unknown dest book must not add | no |
| 4 | `VENUE_UNHEALTHY` | `PauseVenue` | increasing | `VenueHealthy=false` | **YES** — no add when QUOTE/TRADE down | no |
| 5 | `QUOTE_MISSING` | `Reject` | increasing | `Quote is null` | **YES** — no blind market | no |
| 6 | `QUOTE_STALE` | `Reject` | increasing | age > **3 s** (`ReceivedAt` only) | **YES** — gold moves in seconds | no |
| 7 | `SPREAD_TOO_WIDE` | `Reject` | increasing | ask−bid > **2.0** | **YES, weak** — 2.0 on gold is ~$200/lot if dollars | no |
| 8 | `PRICE_MOVED_TOO_FAR` | `Reject` | increasing | \|mid−expected\| > **3.0** | **YES, weak** — 3.0 gold is a chase, not a tick | no |
| 9 | `SIGNAL_STALE` | `Reject` | increasing | decision−source > **15 s** | **YES** — most scalps / replay / ingest lag die here | no |
| 10 | `MAX_LOSS_PER_TRADER` | `PauseTrader` | **all actions** | trader realized ≤ **−500** | **MIXED** — good as stop-new; **UNSAFE** on close | **YES** |
| 11 | `MAX_DAILY_EXECUTION_LOSS` | `GlobalStop` | **all actions** | dest day PnL ≤ **−2_000** | **MIXED** — latch is late vs 5-lot gold | **YES** |
| 12 | `MAX_PORTFOLIO_DRAWDOWN` | `GlobalStop` | **all actions** | DD ≥ **3_000** | **MIXED** — same trap | **YES** |
| 13 | `MAX_OPEN_POSITIONS` | `Reject` | increasing | open ≥ **20** | **YES, weak** — 20 dest slots is saturation, not a working cap | no |
| 14 | `MAX_POSITION_QUANTITY` | `Reject` | increasing | qty > **5** | **YES, weak** — 5.00 XAU is a blow-up ticket (`P500_S055`) | no |
| 15 | `MAX_XAU_GROSS` | `Reject` | increasing | gross+qty > **20** | **YES, weak** — 20 lots same metal | no |
| 16 | `MAX_XAU_NET` | `ReduceSize` via `Reject()` | increasing | \|net\|+qty > **10** | **YES as hard-zero add** — **not** a clip; leftover dest stays | no |
| 17 | `MAX_MARGIN_USAGE` | `Reject` | increasing | usage > **0.70** | **YES, weak** — 70% is liquidation territory | no |
| 18 | `MARTINGALE_BLOCK` | `PauseTrader` | increasing | `BlockMartingale && MartingaleFlag` | **YES — strongest quality reject in this file** | no |
| 19 | `ABNORMAL_SIZING_BLOCK` | `Reject` | increasing | `BlockAbnormalSizing && AbnormalSizing` | **YES** — lot explosions (303310 class) | no |

Not rejects (do **not** list as loss reducers):

| Reason | Outcome | AllowFixSend | Note |
|---|---|---|---|
| `RISK_REDUCTION` | `Approve` | flag ∧ kill=`None` ∧ reconciled ∧ healthy | Passthrough requested qty. No dest id. Close of unmapped dest still approves. |
| `APPROVED` | `Approve` | same conjunction | Echoes requested qty. **No trader-state conjunct.** This is the copy-all hole. |

Dead / unused that *would* reduce loss if implemented:

| Missing / dead | Spec | Why it would cut dest loss |
|---|---|---|
| `TRADER_RISK_BLOCKED` | A23 §4.3, A71 G20 | Stops the **−$241,580** martingale-loser cluster |
| `TRADER_NOT_LIVE` | A23 §4.3 | Stops SHADOW / WATCH / first-3 from going live |
| `REAL_EXECUTION_DISABLED` | A23 §4.3 | Empty `if` today; should be a reason, not a comment |
| `MAX_SLIPPAGE_EXCEEDED` | A23 §4.3; `MaxSlippage=1.5` unread | Gold fill vs expected |
| `INTENT_EXPIRED` / `expires_at` | §63 | Catch-up after reconnect |
| `MAPPING_MISSING` | A23 | Close without dest id is not risk reduction |
| `CONCENTRATION_CAP` | Phase 2 (correctly unused) | 70 SHADOW names = one gold bet |
| Demo / contest group block | synthesis §2.5 | Adverse selection (all current SHADOW is `demo\yo-*`) |

Unit lock is thin: only `QUOTE_STALE`, `VENUE_NOT_RECONCILED`, `SIGNAL_STALE` (plus kill-switch outcome, not reason) have facts. **0** facts for `MARTINGALE_BLOCK` / `ABNORMAL_SIZING_BLOCK` / the three book-loss traps / `MAX_XAU_*`.

---

## 3. The 16 reasons that reduce **new** dest loss if live send existed

These are the answers to the assigned question. Each one, **if it fired on the send hop**, would refuse a `35=D` that would otherwise add dest XAU.

### 3.1 Venue / recon / kill (must-stay-off until true)

| Reason | Why dest loss falls |
|---|---|
| `VENUE_NOT_RECONCILED` | Product `CopyTradingService.VenueReconciled` is **const false**. If Evaluate were the send gate, **every** open/increase would die here. That is the correct fail-closed for an unknown dest book. |
| `VENUE_UNHEALTHY` | Logon-and-dispose is not a living TRADE session. Adding into a dead socket is unreconcilable dest loss. |
| `STOP_NEW_EXECUTION` | Dest-only brake. Does not touch the 8463 MT5 sources (must stay that way). |
| `EMERGENCY_FLATTEN_BLOCKS_NEW` | Stops doubling down while a dest flatten *should* be running. It does **not** flatten. Do not treat this string as dest close. |

### 3.2 Quote / signal (would reject most of a copy-all spray)

| Reason | Why dest loss falls |
|---|---|
| `QUOTE_MISSING` | Live cards have had `bid=null` / `ask=null`. Shadow PnL is **$0** because there is no quote tape. A missing quote live send is a blind gold market. |
| `QUOTE_STALE` | Default 3 s. News / gap / probe-dispose all trip this. Unit fact locks `AllowFixSend=false`. |
| `SPREAD_TOO_WIDE` | Stops paying a blown gold book. Cap **2.0** is too loose to be a working policy — it still beats “no spread check.” |
| `PRICE_MOVED_TOO_FAR` | Stops chasing a 3+ move between source print and dest mid. |
| `SIGNAL_STALE` | 15 s. Demo SHADOW names include ~163 s holds **and** same-second grids. Ingest/score delay makes historical replay uncopyable. That is loss avoidance, not missed profit. |

### 3.3 Size / book (ceilings, not a working policy)

| Reason | Why dest loss falls | Honesty |
|---|---|---|
| `MAX_OPEN_POSITIONS` | Caps dest ticket count at 20 | 70 SHADOW gold names same-minute still saturate one login before 20 if they net into fewer tickets; 20 is still too many |
| `MAX_POSITION_QUANTITY` | Blocks a **>5** lot add | 5.00 XAU is ruin (`$4/oz = $2,000` = entire daily latch). Working cap in synthesis is **0.05** |
| `MAX_XAU_GROSS` | Blocks add past 20 lots gross | 20 lots same metal is a gap-wipe |
| `MAX_XAU_NET` | Blocks add past 10 lots net (qty **0**, not clip) | `ReduceSize` label is a lie. Still better than unbounded net |
| `MAX_MARGIN_USAGE` | Blocks add above 70% | 0.70 is stop-out territory; it only stops the *next* add |

These five reduce **unbounded** dest ruin. They do **not** make copy-all +EV. `P500_S055`: honoring these defaults as first-money policy still ruins one retail Pepperstone login.

### 3.4 Trader flags (the only quality rejects in this file)

| Reason | Why dest loss falls |
|---|---|
| `MARTINGALE_BLOCK` | Live pin: **29 / 29** `RISK_BLOCKED` rows are `martingale=true`, source XAU **−$241,580**. If the caller sets `MartingaleFlag`, those new copies die here. That is the single largest loss cut **inside** this file. |
| `ABNORMAL_SIZING_BLOCK` | `LotEscalation` (303310, max 2.0 source lots, +$41k challenge PnL that is **not** dest-copyable). Stops dest 5-lot echoes of a demo pass-target. |

**Caveat (measured):** flags are **caller-supplied**. `CopyTradingService` does pass `score.Martingale` / `score.LotEscalation`. A copy-all hop that forgets the flags, or a `risk>=80` name that is **not** martingale-flagged, **APPROVE**s.

---

## 4. The 3 reasons that do **not** qualify as “reduces loss”

These fire on **every** action, including `CloseExposure` / `ReduceExposure`. Architecture / A71: daily-loss and DD engage **stop-new**; they must not freeze dest exits.

```117:124:D:\Prop\src\Domain\Risk\RiskEngine.cs
        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");
```

| Reason | On a new open | On a dest close |
|---|---|---|
| `MAX_LOSS_PER_TRADER` (−500) | cuts further add — good | **refuses the exit** — dest stays in the hole |
| `MAX_DAILY_EXECUTION_LOSS` (−2_000) | late latch vs 5-lot gold | **same trap** |
| `MAX_PORTFOLIO_DRAWDOWN` (3_000) | late latch | **same trap** |

If live send existed and a copy-all dest started losing (the expected path: source book **−$154,425**), these three would **increase** dest loss by blocking `RISK_REDUCTION`. They are **not** on the “reduces loss” list.

---

## 5. Measured book — why copy-all 8463 is the loss you are trying to reject

Pin: `P500_PROFIT_SYNTHESIS.md` (same date; API not re-probed this slot). Manager census on disk is **18 groups / 8460** (Achiever 8/6512 + Starwave 10/1948). Synthesis mid-scoring sum is **8463** (Achiever 6512 + Starwave ~1951). Do not pretend those are the same integer; do not pretend either number is 8463 **copyable** XAU names.

| Metric | Value |
|---|---|
| Catalog logins | **8463** (synthesis) / **8460** (INDEX census) |
| XAU traders with a score | **197** and rising (Achiever only) |
| ≥3 completed XAU | **178** |
| `SHADOW` | **70** — source XAU **+$78,276** — **100% demo** (`demo\yo-2step` / `demo\yo-payp`) |
| `WATCH` | **79** — **+$8,178** |
| `RISK_BLOCKED` | **29** (all `martingale=true`) — **−$241,580** |
| `LIVE` / `LIVE_CANDIDATE` | **0 / 0** |
| `INSUFFICIENT_DATA` | **~8284** of the catalog |
| All scored XAU source PnL | **−$154,425** |
| Destination real PnL | **$0** (literal `0` in `EfDashboardQueries` L44, not a venue rollup) |
| Shadow PnL | **$0** (no quote tape; shadow PnL is Σ slip of empty fills) |
| `REAL_COPY` on the synthesis pin | **false** (later addendum: env may bind **true**; sender still absent) |

Copy-all EV **is** the scored tape: **negative six figures before dest costs**. The blocked tail is larger than the SHADOW head. Starwave was **scored 0** — those ~2k logins add **no** XAU edge.

`RiskEngine` does not see any of those states. Copy-every-login therefore:

1. Treats `RISK_BLOCKED` as if it were `SHADOW`.
2. Aligns one retail dest to **one** metal with no concentration cap.
3. Hits 10 net / 20 gross / 5-lot tickets of the **losing** cluster.
4. Then trips #10–12 and **cannot close**.

That is dest ruin (`P500_S055`), not a profit path.

---

## 6. What already sits next to Evaluate (do not credit the engine)

```15:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = 0.05m;
```

```95:96:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
        var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
        var scores = await _db.TraderScores.Where(s => copyable.Contains(s.CurrentState)).ToListAsync(ct);
```

```192:192:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

| Adjacent fact | Loss implication |
|---|---|
| `copyable` excludes `RISK_BLOCKED` | **This** is what currently avoids the −$241k tail — **not** a RiskEngine reason |
| Book snapshots hardcoded **0** (`DailyExecutionPnl`, `PortfolioDrawdown`, `CurrentGrossXau`, `CurrentNetXau`, `OpenPositions`, `MarginUsage`) | Caps #11–17 **never fire** on the shadow hop |
| `TraderRealizedLoss = Min(0, this trade NetRealizedPnl)` | Not trader lifetime. −500 latch is almost dead |
| `KillSwitch = None` always | #1–2 never fire from this caller |
| `Reconciled = VenueReconciled = false` | If Evaluate were the send gate, **#3 kills every new open** |
| Persist `AllowFixSend=false` | Even `APPROVED` cannot send |
| `CTraderFixSession` outbound is **only** `35=A`, then dispose | No product `35=D` |
| `CanPromoteToLive => false` | `LIVE` cannot appear from scoring |

**If someone “fixed” recon to true and expanded `copyable` to all 8463 so they could send,** the 16 new-exposure rejects become the last brake — and they still **miss** `TRADER_RISK_BLOCKED`. That is the honesty line for this slot.

---

## 7. Higher profit is not “more rejects” or “more logins”

| Temptation | Measured |
|---|---|
| Copy more of 8463 | Copy-all EV **−$154,425** source; dest worse after spread |
| Copy the 70 SHADOW | All demo challenge. 303310 lot explosion. 322947 ~163 s scalps die in 15 s `SIGNAL_STALE` + dest spread. 302252 is SHADOW **95.50** at **−$68.46** all-symbol PnL |
| Raise caps | 5 / 10 / 20 / 0.70 / $2,000 is already ruin-sized |
| Treat `earlyScore=95.5` as skill | First-3 is luck; `CanPromoteToLive` is hard-false on purpose |
| Treat FIX LoggedOn as ready | Session probe ≠ fill |

Profit path (synthesis §3, still no send): keep `REAL_COPY` false → standing quote tape → shadow **after costs** on a **non-demo**, **non-`RISK_BLOCKED`**, hold≥15 min, dest qty ≤ **0.05**, net 0.15–0.30, daily dest loss **$200–500** then `STOP_NEW_EXECUTION` only → only then a guarded NOS.

The 16 reasons in §3 are **filters on that path**, not a substitute for it.

---

## 8. Binding list (slot 1 deliverable)

**Reject reasons that reduce dest loss if live send existed** (new exposure only):

1. `STOP_NEW_EXECUTION`
2. `EMERGENCY_FLATTEN_BLOCKS_NEW`
3. `VENUE_NOT_RECONCILED`
4. `VENUE_UNHEALTHY`
5. `QUOTE_MISSING`
6. `QUOTE_STALE`
7. `SPREAD_TOO_WIDE`
8. `PRICE_MOVED_TOO_FAR`
9. `SIGNAL_STALE`
10. `MAX_OPEN_POSITIONS`
11. `MAX_POSITION_QUANTITY`
12. `MAX_XAU_GROSS`
13. `MAX_XAU_NET` (hard-zero add, mislabeled `ReduceSize`)
14. `MAX_MARGIN_USAGE`
15. `MARTINGALE_BLOCK` (largest quality cut; needs honest flag)
16. `ABNORMAL_SIZING_BLOCK`

**Do not count as loss-reducing:** `MAX_LOSS_PER_TRADER`, `MAX_DAILY_EXECUTION_LOSS`, `MAX_PORTFOLIO_DRAWDOWN` (freeze exits).

**Missing reason that would cut the copy-all 8463 loss:** `TRADER_RISK_BLOCKED`.

**Today’s dest loss:** **$0** by absence of send. Do not enable live copy of the scored book to “use” this list.

---

## Binding one-liner

`RiskEngine` has **16** new-exposure reject reasons that would cut dest loss if they sat in front of a real `35=D`, and **zero** `TRADER_RISK_BLOCKED` conjunct — so copying all **8463** logins would still copy the **−$241k** `RISK_BLOCKED` tail. Wanting profit does not create an edge. Keep send off.
