# P500_S005 — XAUUSD gold scalp is uncopyable

| Field | Value |
|---|---|
| Agent | P500_S005 (senior copy-risk; gold scalp expectancy) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S005_gold_scalp_uncopyable.md` |
| Product source edited | **No.** Report only. Do not change engines, limits, or FIX. |
| SUT | `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`, `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`RiskLimits.MaxSourceSignalAge` / `MaxQuoteAge`) |
| Adjacent | `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs`, `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`, `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`PersistDemoShadowAsync`), `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs`, `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` |
| Law | Architecture v2 **§36** (“For XAUUSD, stale trade copying can destroy expected edge”), §31 / §37 / §39 / §63–64; A24, A72, A73 |
| Live cards | `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` logins **322947**, **303274** |
| Verdict | **DO NOT COPY holds under 5–15 minutes.** Venue taker spread + FIX/ingest latency + 15 s `SIGNAL_STALE` reject **destroys** source scalp expectancy. Lower loss = skip this book, not “copy faster.” |

---

## 0. One-line law

```text
source XAU scalp expectancy  ≠  destination copy expectancy
hold < 5–15 min  →  do not OPEN/INCREASE on dest (shadow or live)
```

A 1–3 minute gold winner of **$0.35–$0.85** on **0.05** lot is a **7–17 cent** source capture. Pepperstone cTrader is a **taker** book. One dest round-trip already exceeds that capture. The 15 s stale gate then either **drops** the open or **fills it after the edge is gone**. Both outcomes are a loss relative to “do nothing.”

This is not a code-speed problem. It is a **venue + clock** problem. Product was not edited.

---

## 1. What was measured vs what was assigned

### 1.1 Repo-measured (this pass)

| Item | Value | Path |
|---|---|---|
| `RiskLimits.MaxQuoteAge` | **3 s** (compile default) | `RiskEngine.cs` L15 |
| `RiskLimits.MaxSourceSignalAge` | **15 s** (compile default) | `RiskEngine.cs` L16 |
| `RiskLimits.MaxAllowedSpread` | **2.0** dest price units | `RiskEngine.cs` L14 |
| `RiskLimits.MaxPriceMove` | **3.0** | `RiskEngine.cs` L17 |
| `RiskLimits.MaxSlippage` | **1.5** — **never read** by `Evaluate` | `RiskEngine.cs` L18 |
| QUOTE accept age | **5000 ms** (unbound second clock) | `CTraderFixOptions.MaxQuoteAgeMs` |
| Shadow fill | dest **ask** (long) / **bid** (short); RT = full dest spread | `ShadowCopyEngine.SimulateEntry` / `SimulateExit` |
| Shadow latency overlay | **+0.05** dest points if `modeledDelay > 250 ms` | `DefaultLatencySlippagePoints` |
| Demo shadow delay | **80 ms** → overlay **does not fire** | `EfTradingStore.PersistDemoShadowAsync` L319 |
| Demo `ExpiresAt` | `trade.OpenedAt.AddSeconds(15)` | same file L307 |
| Scorer `AverageHoldSeconds` | computed, **unused** for state | `BaselineScorer.cs` L120 vs L134–161 |
| `CanPromoteToLive` | **always false** | `TraderStateMachine` L211 |
| Contract used for $ math | **1 lot = 100 oz**; $1/oz × 1 lot = **$100** | `docs/trade-reconstruction.md` |

Account cards in `LIVE_GROUPS_AND_TRADERS.json` (balances only; **no deal tape in this tree**):

| Login | Group | Leverage | Balance / equity |
|---|---|---|---|
| **303274** | `demo\yo-2step` | 100 | 16,228.24 / 16,228.24 |
| **322947** | `demo\yo-payp` | 100 | 104,949.80 / 104,949.80 |

Deal-level stats below are **assigned live observations** from the tasking brief (not reconstructed from a repo dump). If a later ledger export disagrees, replace the numbers; the **inequality** (dest RT cost ≫ source scalp capture) does not move.

### 1.2 Assigned live books (tasking)

| Login | Observed book | Hold | Size | Copy state |
|---|---|---|---|---|
| **322947** | **194** completed XAU trades | **~163 s** average (~2.7 min) | **0.10–0.99** lot | **SHADOW** |
| **303274** | many **0.05** lot gold entries, **overlapping, same second** | **1–3 min** | **0.05** | winners **$0.35–$0.85** |

Both books are the same class: **sub-5-minute gold scalp**. 322947 is just a larger ticket on the same clock.

---

## 2. Clocks that sit between source fill and dest fill

Architecture §36 is explicit: stale XAU copy **destroys expected edge**. The tree implements two reject clocks and one unused expiry helper. None of them make a 163 s scalp copyable.

### 2.1 `RiskEngine` — OPEN/INCREASE only

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

Defaults:

```14:16:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
```

`IsIncreasing` = `OpenExposure` | `IncreaseExposure`. **REDUCE/CLOSE skip every age/spread/move check.** That is the toxic asymmetry for a missed scalp open.

### 2.2 FIX QUOTE accept — a second, wider clock

```94:100:D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs
        var ageMs = (DateTimeOffset.UtcNow - ts.ToUniversalTime()).TotalMilliseconds;
        if (ageMs < 0) ageMs = 0;
        if (ageMs > _options.MaxQuoteAgeMs)
        {
            rejectReason = $"Quote is stale. AgeMs={ageMs:0}. ThresholdMs={_options.MaxQuoteAgeMs}.";
            return false;
        }
```

`MaxQuoteAgeMs = 5000`. A quote **4.0 s** old is **accepted** by FIX QUOTE and **rejected** by `RiskEngine` (`QUOTE_STALE`). A quote **2.5 s** old is accepted by both — and on XAU that print can already be **10–40 cents** away from the source deal.

### 2.3 Intent expiry vs risk (third clock, not wired the same way)

`CopyIntentExpiry.IsExpired` is `now - sourceEventTime > maxSignalAge`. Demo persist stamps `ExpiresAt = OpenedAt + 15s`. `RiskEngine.Evaluate` **does not read `ExpiresAt`**. A73 already classified this as `EXISTS_NEEDS_REFACTOR`. For this note: **15 s is the only number the live reject path will honor**, and it is a **lab constant**, not a measured dest-edge horizon.

### 2.4 Latency stack the 15 s cap is supposed to contain

```text
source deal_time
  → MT5 Manager poll / callback          (often 0.5–5 s; history poll worse)
  → persist + reconstruct                (worker tick)
  → CopyIntent + RiskEngine.Evaluate
  → (live) FIX 35=D + cServer RTT + fill
  = total source-to-fill
```

§36 requires those hops to be **measured**. They are **not** instrumented (A73). Honest bound for a healthy poll: **1–8 s** typical, **>15 s** whenever the worker is behind, the QUOTE session hiccups, or reconstruct waits a deal. For a **60–180 s** hold:

| source-to-decision | Fraction of 163 s hold already gone | `SIGNAL_STALE`? |
|---|---|---|
| 1 s (optimistic FIX) | 0.6% | no |
| 3 s (`MaxQuoteAge`) | 1.8% | no |
| 8 s (slow poll) | 4.9% | no |
| **15 s + 1 ms** | 9.2% | **yes — OPEN dropped** |
| 30 s (backfill / stall) | 18% | **yes** |

The 15 s reject does **not** protect expectancy. It **censors the late tail of a book whose entire life is 1–3 minutes**. The early tail that sneaks through still pays dest spread (next section). The late tail that is rejected still produces **CLOSE** intents (unguarded). Net: **missed winners + late / one-sided losers**.

---

## 3. Destination economics vs source scalp P&L

### 3.1 Shadow is a taker, not a mid

```35:50:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public ShadowFill SimulateEntry(...)
    {
        var useAsk = direction == TradeDirection.Long;
        var raw = useAsk ? quote.Ask : quote.Bid;
        var adverse = direction == TradeDirection.Long ? DefaultLatencySlippagePoints : -DefaultLatencySlippagePoints;
        if (modeledDelay > TimeSpan.FromMilliseconds(250))
            raw += adverse;
        ...
    }
```

Exit is the other touch (`SimulateExit`: long exits **bid**). Round-trip dest cost, before commission:

```text
RT_spread_$ = (ask - bid) × 100 oz/lot × lots
```

Plus, if modeled delay **> 250 ms** (any real FIX path will be):

```text
overlay_$ = 0.05 × 100 × lots     # $5.00 per lot, $0.25 on 0.05 lot
```

`PersistDemoShadowAsync` passes **80 ms**, so **demo SHADOW understates dest cost** by hiding the overlay. That is another reason not to promote 322947’s SHADOW book as evidence.

Shadow **records** `Spread` and `QuoteAge`. It **never rejects** `QUOTE_STALE` / `SPREAD_TOO_WIDE` (B18). Risk is the only rejector, and nobody on the live worker path calls `Evaluate` today. Even if wired tomorrow, the **numbers** below still kill the book.

### 3.2 303274 — 0.05 lot, $0.35–$0.85 winners, 1–3 min

Contract law (`docs/trade-reconstruction.md`): 1 lot = 100 oz, P&L = `$/oz × 100 × lots`.

```text
$ per $1 gold move on 0.05 lot = 100 × 0.05 = $5.00
source winner $0.35  →  captured move = 0.35 / 5 = $0.07   (7 cents)
source winner $0.85  →  captured move = 0.85 / 5 = $0.17   (17 cents)
```

Dest **round-trip** at representative cTrader XAU spreads (not yet p50-measured on this venue — A72 forbids treating `MaxAllowedSpread = 2.0` as measured; these are **illustrative tight-to-normal retail gold** books):

| Dest spread (ask−bid) | RT $ on 0.05 lot | vs $0.35 win | vs $0.85 win |
|---|---|---|---|
| 0.12 (fantasy tight) | $0.60 | **−$0.25** | **−$0.25** wait: 0.85−0.60 = +0.25 before latency |
| 0.18 | $0.90 | **−$0.55** | **−$0.05** |
| **0.25** | **$1.25** | **−$0.90** | **−$0.40** |
| **0.30** | **$1.50** | **−$1.15** | **−$0.65** |
| 0.45 | $2.25 | **−$1.90** | **−$1.40** |
| + 0.05 overlay (delay > 250 ms) | +$0.25 | worse | worse |

At any dest spread **≥ ~0.17**, the **median assigned winner is already negative** after one dest RT, **before** late-entry adverse, **before** exit late, **before** commission. A 0.12 book is not a planning assumption for Pepperstone XAU.

`MaxAllowedSpread = 2.0` will **not** reject a 0.30 book. The engine **approves** the spread that **erases the scalp**. `MaxPriceMove = 3.0` ($3 / 300 cents) will **not** reject a 20-cent gap that is already larger than the 7–17 cent edge.

### 3.3 322947 — 0.10–0.99 lot, ~163 s hold, 194 trades, SHADOW

Same clock, bigger ticket. Dest RT at 0.30 spread:

| Lots | RT $ @ 0.30 | Gold move needed just to pay dest RT |
|---|---|---|
| 0.10 | $3.00 | 30 cents |
| 0.50 | $15.00 | 30 cents |
| 0.99 | $29.70 | 30 cents |

A **163 s** average hold does **not** give 30 cents of **copyable** edge. The source trader is already **inside** the move; the copy is **outside** it (ask in, bid out, 1–15 s late). Need dest capture **after** RT ≥ 30 cents **plus** the adverse move during ingest/FIX. On a 2.7-minute gold scalp that remainder is typically **≤ 0** and often **negative**.

194 such tickets on a **$105k** `demo\yo-payp` book look “active” on a leaderboard. They are **not** a live-copy candidate. SHADOW is the **ceiling** (`CanPromoteToLive => false`), and even SHADOW P&L on this book will **lie high** if it uses 80 ms / no overlay / latest quote instead of the contemporaneous dest book.

### 3.4 Same-second overlapping 0.05 (303274)

Several 0.05 entries in the **same second** is either:

1. **Many position tickets** → N dest `35=D` after one delay, all lifting the same ask, or
2. **One reconstructed position** with scale-in → `IncreaseExposure` still pays dest ask N times, or
3. **Martingale / averaging** → `BlockMartingale = true` / `BlockAbnormalSizing = true` **PauseTrader** / reject.

None of those recover a 7–17 cent source capture. Clustered same-second size also blows **gross XAU** (`MaxXauGrossExposure = 20`) if the copier naively 1:1’s a spray. That is a **risk** problem on top of a **dead edge**.

---

## 4. Why the 15 s stale reject *destroys* (not “saves”) this expectancy

Two failure modes, both worse than “do not copy”:

### Mode A — signal arrives **> 15 s** late (`SIGNAL_STALE`)

- OPEN/INCREASE: **rejected**, `ApprovedQuantity = 0`, `AllowFixSend = false`.
- CLOSE/REDUCE: **approved** (`RISK_REDUCTION`), age ignored.
- If a prior open somehow exists: dest is flattened **without** a matched dest entry, or flattened after a different fill.
- If no dest open exists: CLOSE is a no-op or an unknown flatten. Either way the **source winner is not on dest**.
- Scalps that **lose** and run longer are **more** likely to still be open when a late poll arrives — selection bias toward **copying the slow losers**.

### Mode B — signal arrives **≤ 15 s** late (approved)

- Fill = dest **ask** (long) on a book that has already moved with the source.
- Source already extracted 7–17 cents in the first 30–90 s of a 1–3 min hold.
- Copy enters **after** that extraction, pays **half-spread**, exits **bid**, pays the other half.
- `PRICE_MOVED_TOO_FAR` only trips at **$3**. A $0.40 adverse gap (common in 3–15 s of XAU) is **legal**.
- `QUOTE_STALE` at 3 s can reject the open while a 4–5 s quote is still in `CTraderQuoteService` memory — **false freshness**.

Net expectancy of {A ∪ B} on this book:

```text
E[dest] ≈ p(fast) × (source_capture − dest_RT − adverse_during_lag)
         + p(stale) × (0  or  close-without-open residual)
         ≪  0
```

`p(stale)` is **not small** for 60–180 s holds on a Manager-API poll. `source_capture − dest_RT` is **already negative** at normal spreads. There is **no** p(fast) that saves it.

Architecture already said this in one sentence:

> For XAUUSD, stale trade copying can destroy expected edge. (§36)

This report only **prices** that sentence against two live gold-scalp logins.

---

## 5. Hold-time floor: 5–15 minutes

### 5.1 Why a floor, not a tighter age cap

Tightening `MaxSourceSignalAge` from 15 s → 2 s would **reject more** 303274/322947 opens. It would **not** create dest edge. The remaining “fast” fills still pay dest RT > source capture.

Tightening `MaxAllowedSpread` to 0.10 would **reject almost every** dest XAU print and starve the book. Same outcome as “do not copy,” with more reason-code noise.

The **lossy** action is the **copy itself**. The cheap control is **eligibility**:

```text
if reconstructed.hold_seconds < MIN_COPY_HOLD_SECONDS:
    do not emit OPEN/INCREASE
    (optional: still score the trader; still SHADOW-mark; never dest size)
```

Recommended lab band (policy, **not** a new code default in this change):

| Floor | Meaning |
|---|---|
| **5 minutes (300 s)** | Hard skip. ~2× 322947’s 163 s average. Still thin vs dest RT; use only as a **research** cut. |
| **15 minutes (900 s)** | **Default do-not-copy** for live/shadow OPEN. Gives dest RT (≈ 20–40 cents) room to be a **cost**, not the **entire** trade. |
| Hold unknown / trade still open < floor | Treat as **uncopyable** until close **or** until elapsed ≥ floor **and** remaining MFE is measured. Do not “copy now and hope.” |

**Lower loss = do not copy holds < 5–15 minutes.** Not “copy them smaller.” Half of a negative-edge ticket is still negative.

### 5.2 What the scorer does **not** do today

`FeatureSnapshot.AverageHoldSeconds` is populated and then **ignored**. A 163 s, 194-trade SHADOW book can still look “high quality” on PF / net PnL. That is **source** economics. It is **not** dest economics.

Do **not** treat `SuggestedState = SHADOW` on 322947 as a copy-green light. SHADOW is the post–trade-#3 ceiling, not a dest-PnL proof (A22 I5, `CanPromoteToLive => false`).

### 5.3 What would make a gold book copyable (none of this is true here)

A dest-copyable XAU book needs **all** of:

1. Median hold **≫ dest source-to-fill p95** (minutes, not seconds).
2. Median source capture **≫ dest RT spread + measured adverse during lag**.
3. One position ticket (or a reconstruction that does not spray same-second 0.05s).
4. Measured dest quote age / spread (A72) — not `2.0` / `3 s` / `5 s` lab triples.
5. Shadow P&L on **contemporaneous** dest bid/ask, delay **> 250 ms** overlay on, not 80 ms demo.

322947 and 303274 fail (1), (2), and (3). (4) and (5) are **unmeasured / optimistic** in this tree.

---

## 6. Worked dest P&L (303274-shaped ticket)

Assumptions (stated, not venue-measured): dest spread **0.30**, lots **0.05**, long, source in **2340.00**, source out **2340.17** (17 cents, $0.85), hold **120 s**, copy lag **4 s**, dest already **+0.10** by decision time, exit dest **−0.05** vs source out (lag + bid).

| Leg | Source | Dest shadow / FIX taker |
|---|---|---|
| Entry | 2340.00 | 2340.00 + 0.10 + 0.15 (half-spread) = **2340.25** |
| Exit | 2340.17 | 2340.17 − 0.05 − 0.15 = **2339.97** |
| Capture | +0.17 oz | **−0.28 oz** |
| $ on 0.05 | **+$0.85** | **−$1.40** |

If the same ticket is **15.001 s** late: dest **does not enter**; source still prints +$0.85. Copier PnL **$0** on that winner. If a **loser** of −$2.00 runs 8 minutes and the poll catches the open at t+6 s, dest **does** enter and can realize the dest-spread-worse loss. That is how a “profitable scalp source” becomes a **dest drain**.

322947 at 0.50 lot, same 0.30 / 4 s story: dest RT **$15** plus adverse. A 163 s source trade cannot be assumed to leave $15+ on the table for a late taker.

---

## 7. Honesty / non-claims

- This file does **not** claim the two logins’ deal tapes were replayed from PostgreSQL. The tree has **account cards only**. Replace assigned $ / hold / lot numbers when a deal export lands; **do not** wait for that to skip the book.
- `MaxQuoteAge = 3s` and `MaxQuoteAgeMs = 5000` are **unbound lab numbers** (A72). They are cited as **what the code will do**, not as measured production policy.
- Dest spread table is **illustrative**. A72 requires a measured p50/p95 before any production `MAX_SPREAD_OPEN`. Even a **tight** 0.18 already kills 303274 winners.
- Shadow demo path **understates** cost (80 ms < 250 ms overlay). Do not advertise 322947 SHADOW PnL as dest truth.
- `REAL_COPY_EXECUTION_ENABLED` remains **false**. This note is about **not turning it on** for this class, and about **not treating SHADOW as a green copy**.
- No product file was modified.

---

## 8. Operator rule (binding for copy selection; not a code patch)

```text
XAUUSD OPEN/INCREASE copy eligibility
  REQUIRE hold_seconds >= 300   # 5 min research floor
  DEFAULT hold_seconds >= 900   # 15 min do-not-copy floor
  REJECT overlapping same-second spray as a single “scalp edge”
  NEVER size dest from source $0.35–$0.85 / 0.05-lot winners
  NEVER promote 322947-class 163 s books off SHADOW
```

**Lower loss = do not copy holds under 5–15 minutes.** Venue spread + FIX/ingest latency + 15 s stale reject **destroys** gold-scalp expectancy. Skip 303274 and 322947 for dest size. Score them if useful. Do not send, and do not pretend SHADOW mid-fills are a live analog.

---

## 9. Paths cited

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Taker fill + 0.05 overlay + no stale reject |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | 3 s quote / 15 s signal / 2.0 spread / CLOSE exemption |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `MaxQuoteAgeMs = 5000` |
| `D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs` | QUOTE accept vs venue tag 60 |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 80 ms shadow, `ExpiresAt = OpenedAt+15s`, `SHADOW_ONLY` |
| `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` | unused-by-Evaluate 15 s predicate |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | hold computed, unused; SHADOW ceiling |
| `D:\Prop\docs\trade-reconstruction.md` | 100 oz / $1,000 per 10-point / lot |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §36 | stale XAU copy destroys edge |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 303274 / 322947 cards |
| `D:\Prop\reports\swarm\20260818\A72_quote_guards.md` | two MaxQuoteAge clocks |
| `D:\Prop\reports\swarm\20260818\A73_copy_latency.md` | §36 hops missing |
| `D:\Prop\reports\swarm\20260818\B18_shadow_review.md` | shadow never fail-closes on age/spread |
