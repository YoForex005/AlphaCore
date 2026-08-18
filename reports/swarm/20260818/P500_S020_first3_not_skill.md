# P500_S020 — First-3 completed XAU is early score only, not a profit license

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S020_first3_not_skill.md` |
| Slot | **P500_S020** |
| Date | 2026-08-18 |
| Assigned | First 3 completed XAU trades are **early score only**. Live **303274** first three: **-0.35, -55.30, +25.90** (net negative) yet later **SHADOW 93.50**. Prove first-3 is **not** a profit license. Do **not** edit product. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** |
| Method | Full read of `TradeReconstructor.cs`, `docs/trade-reconstruction.md`, `docs/scoring.md`, `BaselineScorer.cs`, `ReconstructionScoringService`, architecture §15, A22 I2/I9, A69 S3/S5. Catalog pin of login 303274 in `LIVE_GROUPS_AND_TRADERS.json`. No Manager re-attach. No deal-tape replay in this slot. No product edit. |
| Binding law | Architecture v2 §15 / §1.4 / §22–23; `docs/scoring.md`; A22 I2 / I5 / I9; A69 S3 / S5; `TradeReconstructor.IsEarlyScoreEligible` |

**Honesty:** The P&L triple and later `SHADOW` / `93.50` are **assigned live facts** for this slot (parent swarm). This workspace has **no** persisted reconstructed-trade dump for login `303274`. Catalog confirms the login exists. The **code path** is measured here; the **tape** is not re-pulled.

**One-line:** Trade #3 is a **count gate** (`EARLY_SCORE_ELIGIBLE`). It is **not** `PROVEN_PROFITABLE`, not a ranking by first-3 dollars, and not a live-capital license. Live 303274 first-3 net **-29.75** can still later land **SHADOW / 93.50** on the expanding book.

## Profit implication

Treating `earlyScore=95.5` / first-3 winners as skill is how you copy luck onto Pepperstone. Treating first-3 losers as “do not score” would also discard names that later flip net (303274). First-3 is a **sample-size latch**, not +EV. Higher profit = require completed XAU **≥ 20** and dest shadow after costs. `CanPromoteToLive => false` is the capital brake (`BaselineScorer.cs` L211).

**Remeasured 2026-08-18:** `IsEarlyScoreEligible` = count ≥ 3 (`TradeReconstructor.cs` L75–76). Production score window is all `Completed && IsXauUsd` (`DealIngestionService` persist path). Quality is not first-3 dollars (`BaselineScorer.cs` L152–159). Catalog pin: login **303274** `demo\yo-2step` balance 16228.24 (`LIVE_GROUPS_AND_TRADERS.json` L2564–2568). First-3 tape is assigned (synthesis), not re-pulled.

---

## 0. Verdict

**CONFIRMED. First-3 completed XAUUSD lifecycles are early-score eligibility only. They are not a profit license.**

| Claim | Result |
|---|---|
| What first-3 *is* | Count of **completed reconstructed XAU** books (helper also requires `EligibleForFirstThree`). Crossing **N ≥ 3** sets `EarlyScoreEligible`. |
| What first-3 *is not* | Not `PROVEN_PROFITABLE`. Not “who made the most in the first 3.” Not LIVE. Not a freeze of later quality at the first-3 P&L. |
| Product helper | `IsEarlyScoreEligible = CountCompletedXauUsdTrades >= 3` |
| Production score window | **Expanding:** all `Completed && IsXauUsd` (does **not** slice to first 3; does **not** apply `EligibleForFirstThree`) |
| Best state from score | `SHADOW` when `quality >= 70 && risk < 40`. `CanPromoteToLive => false`. |
| Live 303274 first-3 P&L (given) | **-0.35, -55.30, +25.90** → **net −29.75** |
| Live 303274 later (given) | **SHADOW**, **EarlyQualityScore 93.50** |
| Can first-3 P&L alone produce 93.50? | **No.** FIRST3 window on this triple caps well below 93.50 (see §5). 93.50 requires the **later expanding** book (net>0 and high PF). |
| Therefore | A red first-3 book **does not** forbid later SHADOW. A green first-3 book **does not** license profit or LIVE. First-3 is a **sample-size latch**, not skill / profit proof. |

Do **not** market “first 3 winners ⇒ copy.” Do **not** market “first 3 losers ⇒ discard.” Do **not** claim first-3 P&L is the score.

---

## 1. Law

Architecture §15 (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

```text
3 completed reconstructed XAUUSD position lifecycles
```

Trade #3 closure triggers `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`. Orders, fills, partials, SL/TP edits are not trades.

Architecture §1.4 / §23: do **not** send real money after trade #3. Default after a strong early score is **SHADOW**.

`D:\Prop\docs\scoring.md`:

```text
Trade #3 completed XAUUSD ⇒ EARLY_SCORE_ELIGIBLE.
High quality + low risk ⇒ SHADOW, never LIVE.
```

A22 I2 / I9 (binding spec, not product):

- I2: Trade #3 emits `EARLY_SCORE_ELIGIBLE`. It does **not** emit `PROVEN_PROFITABLE`.
- I9: `early_quality_score` must **not** rank traders by raw net P&L. Target is future copyable profitability inside risk limits, **not** “who made the most in the first 3 trades.”

A69 S3 / S5: first crossing of N==3 emits the eligibility event once. Trade #3 + high score → **SHADOW only**, even if source NET is large.

`docs/trade-reconstruction.md` “First-3-Trade Semantics” is weaker: it says the first 3 completed trades are used for opening-rule / initial-risk / pass-fail **pattern** checks. It does **not** say they license profit. It also omits the XAU-only constraint (architecture / A21 win on that conflict; D72 already recorded this).

---

## 2. Product reconstruction (measured)

`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`

A completed reconstructed trade is **one position lifecycle that returned to flat** (or a reverse that closed the prior side). Partial closes stay on the same book. Only flatten increments the completed counter.

```60:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(
        string brokerId,
        long login,
        IReadOnlyList<NormalizedDeal> deals)
    {
        return Reconstruct(brokerId, login, deals)
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
            .OrderBy(t => t.ClosedAt)
            .ThenBy(t => t.OpenedAt)
            .ToList();
    }

    public int CountCompletedXauUsdTrades(string brokerId, long login, IReadOnlyList<NormalizedDeal> deals) =>
        CompletedXauUsdTrades(brokerId, login, deals).Count;

    public bool IsEarlyScoreEligible(string brokerId, long login, IReadOnlyList<NormalizedDeal> deals) =>
        CountCompletedXauUsdTrades(brokerId, login, deals) >= 3;
```

Facts:

1. **Eligibility is a count**, not a P&L sign. `>= 3` completed clean XAU → early-score eligible. Net can be −29.75 or +10_000. Same latch.
2. Helper order is `ClosedAt`, then `OpenedAt`. That is the first-3 **sequence**. It is not a profit sort.
3. `CountCompletedXauUsdTrades` is **not capped at 3**. After trade #4+ the count keeps growing. The latch is “at least three,” not “exactly the first three forever.”
4. Canceled-deal positions flip `EligibleForFirstThree = false` on that `position_id` (helper only). Score path does not use this flag (D72 / E024).
5. Unit pin (`TradeReconstructionTests.First_three_completed_xau_unlocks_early_score`): three completed XAU round-trips → eligible **true**. No assertion on P&L sign.

`docs/trade-reconstruction.md` matches the flatten rule: “A completed trade means all IN volume for a position ticket has been fully matched by OUT volume. Partial closes count toward the same trade — only fully closed positions increment the trade counter.”

---

## 3. Product scoring (measured) — expanding book, not first-3 dollars

`BaselineScorer.EarlyScoreTradeCount = 3`.

`Score()` sets `EarlyScoreEligible = features.CompletedXauTrades >= 3`, then computes risk / behavior / quality on **whatever completed XAU list it is given**. Features sum **all** passed trades:

```66:66:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var net = trades.Sum(t => t.NetRealizedPnl);
```

Quality is **not** “sum of first 3 P&L”:

```152:160:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
```

State machine (`TraderStateMachine.FromBaseline`):

| Condition | State |
|---|---|
| N == 0 | `INSUFFICIENT_DATA` |
| risk ≥ 80 **or** (martingale ∧ DD > 0 ∧ net < 0) | `RISK_BLOCKED` |
| N < 3 | `INSUFFICIENT_DATA` |
| quality ≥ 70 **and** risk < 40 | `SHADOW` |
| quality ≥ 55 | `WATCH` |
| else (eligible) | `EARLY_SCORE` |

`AfterHighEarlyScore() => SHADOW`. `CanPromoteToLive(_) => false`. `LIVE` / `LIVE_CANDIDATE` exist on the enum and are **unreachable** from this unit.

Production persist (`ReconstructionScoringService.RebuildTraderAsync`) scores **all** reconstructed `Completed && IsXauUsd` — **not** `Take(3)`, **not** `EligibleForFirstThree`:

```126:144:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            ...
            EarlyQualityScore = score.EarlyQualityScore,
            CompletedXauTrades = score.Features.CompletedXauTrades,
            CurrentState = score.SuggestedState,
            ...
        }, ct);
```

So:

- Trade #3 **unlocks** official scoring.
- Trade #4, #5, … **re-score the whole XAU book**.
- Later SHADOW / 93.50 is an **expanding-window** number. It is allowed to disagree with first-3 net.

A22 named this on purpose: window `FIRST3` is a frozen research snapshot; operational state uses `EXPANDING`. Product persists one row (expanding). There is **no** separate frozen FIRST3 column in `TraderScore`.

---

## 4. Live login 303274 (catalog + assigned tape)

Catalog (`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`):

| Field | Value |
|---|---|
| login | **303274** |
| group | `demo\yo-2step` |
| leverage | 100 |
| balance | 16228.24 |
| equity | 16228.24 |

Assigned live first-3 **completed XAU** net P&L (this slot; not re-fetched):

| # | NetRealizedPnl |
|---|---:|
| 1 | −0.35 |
| 2 | −55.30 |
| 3 | +25.90 |
| **Σ first-3** | **−29.75** |

Gross profit first-3 = **25.90**. Gross loss first-3 = **55.65**. Profit factor first-3 = **25.90 / 55.65 ≈ 0.4654**.

Assigned later operational score: **SHADOW**, **EarlyQualityScore = 93.50**.

Balance 16228.24 vs a −29.75 first-3 is consistent with **later** profitable XAU (or deposits). This slot does **not** claim a deposit split. It claims only: first-3 net is **red**, later score is **SHADOW 93.50**.

---

## 5. Arithmetic: first-3 book cannot be the 93.50

Apply `BaselineScorer` quality to the FIRST3 window only (N=3, net −29.75, PF 0.4654):

```text
quality = 50
        + 0          // NetPnl > 0 ? no
        + 0          // PF >= 1.2 ? no
        + 0          // PF >= 1.8 ? no
        + 0.20 * behavior
        - 0.25 * risk
```

Bounds (behavior ∈ [0,100], risk ∈ [0,100]):

| Case | quality |
|---|---:|
| Best (behavior=100, risk=0) | **70.00** |
| Typical if SL-use < 0.5 only (behavior=90, risk=0) | 68.00 |
| SL-use < 0.3 (behavior=90, risk=10) | 65.50 |
| Worst | 0 after clamp |

**93.50 is impossible on this first-3 P&L.** Even the theoretical ceiling is 70.00, and that still needs a clean behavior book.

93.50 **is** reachable on an **expanding** book with `NetPnl > 0` and `PF >= 1.8`:

```text
quality = 50 + 15 + 10 + 5 + 0.20*behavior - 0.25*risk
        = 80 + 0.20*behavior - 0.25*risk
```

One exact hit used by the live number:

```text
behavior = 80, risk = 10
quality  = 80 + 16 - 2.50 = 93.50
state    = SHADOW          (93.50 >= 70 and 10 < 40)
```

That is **later trades flipping net and PF**, not first-3 skill. First-3 net stayed −29.75.

If the production score had been frozen to first-3 P&L, live 303274 would sit at quality ≤ 70 and most likely `EARLY_SCORE` or `WATCH` (or `RISK_BLOCKED` if the −55.30 was a sized-up loser). It would **not** print 93.50.

---

## 6. What this proves (and what it does not)

**Proves**

1. First-3 is a **sample-size / eligibility** event. Three completed XAU flats unlock `EarlyScoreEligible`. Sign of P&L is not in the predicate.
2. First-3 is **not** a profit license. A −29.75 open is still a legal N=3 book.
3. First-3 is **not** a skill certificate. Later expanding quality can be 93.50 SHADOW while the first three are net negative. Operators who copy “because first 3 looked good” or skip “because first 3 looked bad” are ranking the wrong window.
4. High early quality still maps to **SHADOW**, never LIVE (`CanPromoteToLive => false`). Even 93.50 is a **shadow** nomination, not capital.

**Does not prove**

- That 303274 is copyable. SHADOW is the **highest** baseline state. It is not destination P&L and not a FIX send.
- That first-3 reconstruction on this login is A21-clean (no dirty / cancel / OUT_BY audit in this slot; no tape).
- That 93.50 was computed from official Manager deals in CI. Number is assigned, not replayed here.
- That `docs/trade-reconstruction.md` is as tight as §15 (it is not; XAU-only missing).

---

## 7. Files read (no hashes this slot — no shell)

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | First-3 count + eligibility |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | `Completed`, `IsXauUsd`, `EligibleForFirstThree`, `NetRealizedPnl` |
| `D:\Prop\docs\trade-reconstruction.md` | Flatten / first-3 semantics (weaker than §15) |
| `D:\Prop\docs\scoring.md` | Trade #3 ⇒ EARLY_SCORE_ELIGIBLE; SHADOW never LIVE |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | Features + quality + FromBaseline |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Expanding `Completed && IsXauUsd` score persist |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | LIVE exists; scorer cannot emit it |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | N=3 unlocks eligibility; cancel drops helper count |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | N=2 insufficient; 3 winners → SHADOW not LIVE |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §15 | Binding first-3 definition |
| `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | I2 / I9 / FIRST3 vs EXPANDING |
| `D:\Prop\reports\swarm\20260818\A69_trader_states.md` | S3 / S5 |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | login 303274 catalog row |

---

## 8. Operator takeaway

```text
first-3 completed XAU  =  EARLY_SCORE_ELIGIBLE latch
                       ≠  profit
                       ≠  skill
                       ≠  LIVE
                       ≠  the 93.50 on the score row

303274 first-3         =  -0.35 + -55.30 + 25.90  =  -29.75
303274 later (given)   =  SHADOW / 93.50   ← expanding book, not first-3
```

If a dashboard highlights the first three XAU trades, label them **early-score sample**, not **edge**. A later SHADOW score on a red first-3 open is expected product behavior, not a bug and not a license to send `35=D`.
