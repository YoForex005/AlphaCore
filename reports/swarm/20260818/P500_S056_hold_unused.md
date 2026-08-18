# P500_S056 — `AverageHoldSeconds` computed, unused in `Score()` (profit bug)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S056_hold_unused.md` |
| Agent | P500_S056 (hold-time unused in quality / copy eligibility) |
| Date | 2026-08-18 |
| Slot | **S056** |
| Angle | Reconstructed hold is a real deal-supported feature. Quality treats a 90 s gold scalp as identical to a 2 h swing. That is a **destination-PnL** bug, not a style preference. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| SUT read | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (full, 212 lines) |
| Persist read | `D:\Prop\src\Domain\Entities\TraderScore.cs`, `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L119–145, `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` `UpsertScoreAsync` |
| Copy / risk read | `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L94–143, `D:\Prop\src\Domain\Risk\RiskEngine.cs` L16 / L113–115, `D:\Prop\src\Domain\Execution\CopyIntentExpiry.cs` |
| Tests read | `D:\Prop\tests\Unit\BaselineScorerTests.cs` (hold frozen at **30 minutes**) |
| Spec siblings | A22 §3.2 / §4.2 / §6 (`hold_sec_i`, `hold_stability_score`); A73 §36 (`MaxSourceSignalAge` 15 s); P500_PROFIT_SYNTHESIS §2.6; P500_CODE_15 (180 s filter — **this slot is tighter**) |
| Law | Architecture v2 **§3** (target = future dest-net PnL), **§18** (deal-supported features include hold), **§36** (stale copy destroys edge). A22 I12: numeric floors are provisional, **structure** is locked. |

**One-line:** `ComputeFeatures` writes `AverageHoldSeconds`. `Score()` never reads it. `FromBaseline` never filters it. `TraderScore` does not persist it. Three winning 90 s scalps can land `SHADOW` with the **same** `EarlyQualityScore` as three 30-minute winners. **Recommend: refuse copy eligibility unless mean hold ≥ 15 minutes (900 s).**

This file does **not** change `BaselineScorer`. It does **not** add a quality term. It does **not** flip `CanPromoteToLive`.

---

## 0. Verdict

| Claim | Allowed / true today? | Evidence |
|---|---|---|
| Hold is reconstructed from open/close | **YES — measured** | `ComputeFeatures` L96–99, L120: `(ClosedAt - OpenedAt).TotalSeconds`, then `holds.Average()` |
| Hold enters `risk` / `behavior` / `quality` | **NO** | `Score()` L134–160 never mentions `AverageHoldSeconds` |
| Hold enters `FromBaseline` state | **NO** | `FromBaseline` L189–206 reads `CompletedXauTrades`, `Martingale`, `MaxDrawdown`, `NetPnl`, `quality`, `risk` only |
| Hold is implicit via `TradeFrequencyPerDay` | **NO** | Frequency is also computed (L101–106, L123) and **also unused** in `Score()` / `FromBaseline` |
| Hold is implicit because `FeatureSnapshot` is passed to `FromBaseline` | **Cosmetic only** | Parameter is the whole record. The field is never dereferenced. Same as shipping a unused column. |
| Hold is persisted on `TraderScore` | **NO** | Entity has risk / behavior / quality / flags / trade count. No hold column. `UpsertScoreAsync` cannot store what does not exist. |
| Hold is shown on the trader row | **NO** | `TraderRowDto` has no hold field. Web `apps/web/src` has **zero** `hold` hits. |
| Unit tests distinguish scalp vs swing | **NO** | Helper `Closed()` always `AddMinutes(30)` → every fixture is **1800 s** |
| A22 `hold_stability_score` (10 % of behavior) is implemented | **NO** | Spec exists. Code does not compute `hold_cv`. Mean hold is not even a proxy. |
| Scalper and swing with same PnL/PF/SL/lots get the same quality | **YES — measured** | Quality inputs are `NetPnl`, `ProfitFactor`, `behavior`, `risk`, `N`. Duration is not among them. |
| Copy hop can keep a 90 s gold scalp | **NO — dest expectancy** | `MaxSourceSignalAge = 15 s`; intent `ExpiresAt = now + 15 s`. A 90 s source life leaves ~0–1 hop of edge after dest spread. |
| Recommended copy floor | **15 minutes (900 s)** | Hard eligibility gate, not a quality nudge. See §5. |

**Honesty class:** computed feature, **dead for ranking and for copy**. This is a **profit** defect (wrong people become `SHADOW` / enter the copyable set), not a missing dashboard decoration.

---

## 1. Reconstruction (what *is* computed)

```96:120:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var holds = trades
            .Where(t => t.ClosedAt.HasValue)
            .Select(t => (decimal)(t.ClosedAt!.Value - t.OpenedAt).TotalSeconds)
            .ToList();
        // …
            AverageHoldSeconds = holds.Count == 0 ? 0 : holds.Average(),
```

Properties of this reconstruction (honest, not oversold):

| Property | Fact |
|---|---|
| Input | Completed XAUUSD trades only (`t.Completed && t.IsXauUsd`), ordered by `ClosedAt` |
| Clock | `ReconstructedTradeResult.OpenedAt` (required) and `ClosedAt` (nullable) |
| Empty book | Forced `0` (L59) |
| Missing close | Trade dropped from the hold list; mean is over closed rows only |
| Unit | Seconds, `decimal` average — **not** p50, **not** min, **not** CV |
| Sign | No `Max(0, …)`. A reversed clock would go negative. A22 §3.2 specified `hold_sec_i = Max(0, …)`. Current code does not clamp. |
| Partial closes | One reconstructed lifecycle, `OpenedAt` → last `ClosedAt`. Not per-deal hold. |
| MAE/MFE | Unrelated. Hold does **not** need ticks. It is a legal §18 deal-supported feature. |

A22 already defined the same per-trade quantity (`hold_sec_i`) and a **stability** term (`hold_cv` → `hold_stability_score`, 10 % of `behavior_body` when MFE is unused). Current `BaselineScorer` implements **neither** the CV term **nor** a mean-hold gate. The mean is written and abandoned.

`SlUseAndHoldTimeTests` is named in A89 / C02 / D34 / E019 as a **missing** class. No test asserts hold math.

---

## 2. `Score()` — hold is not in the formula (not even implicitly)

### 2.1 What `Score()` actually uses

```129:170:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public BaselineScore Score(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var features = ComputeFeatures(completedXau);
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;

        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        if (features.LotCv > 0.5m) risk += 10;
        if (features.SlUseRate < 0.3m) risk += 10;
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
        risk = Math.Min(100m, risk);

        var behavior = 100m;
        if (features.Martingale) behavior -= 30;
        if (features.AveragingDown) behavior -= 15;
        if (features.LotCv > 0.4m) behavior -= 10;
        if (features.SlUseRate < 0.5m) behavior -= 10;
        if (features.LossSizeCv > 0.8m) behavior -= 10;
        behavior = Math.Clamp(behavior, 0m, 100m);

        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
        quality = Math.Clamp(decimal.Round(quality, 2), 0m, 100m);

        var state = TraderStateMachine.FromBaseline(eligible, quality, risk, features);
```

Field use in the three headlines + state:

| `FeatureSnapshot` field | `risk` | `behavior` | `quality` | `FromBaseline` |
|---|---|---|---|---|
| `CompletedXauTrades` | no | no | cap if `N < 3` | `N==0`, `!earlyEligible` |
| `NetPnl` | no | no | `> 0` → +15 | martingale + DD + `NetPnl < 0` → `RISK_BLOCKED` |
| `GrossProfit` | vs DD | no | via PF only | no |
| `GrossLoss` | no | no | via PF only | no |
| `ProfitFactor` | no | no | 1.2 / 1.8 steps | no |
| `LotCv` | `> 0.5` | `> 0.4` | via behavior/risk | no |
| `LossSizeCv` | no | `> 0.8` | via behavior | no |
| `Martingale` | +35 | −30 | via behavior/risk | + DD + loss → block |
| `AveragingDown` | +20 | −15 | via behavior/risk | no |
| `LotEscalation` | +15 | no | via risk only | no |
| **`AverageHoldSeconds`** | **no** | **no** | **no** | **no** |
| `SlUseRate` | `< 0.3` | `< 0.5` | via behavior/risk | no |
| `MaxDrawdown` | vs GP | no | via risk | with martingale |
| `TradeFrequencyPerDay` | **no** | **no** | **no** | **no** |
| `MaeMfeQuality` / `AverageMfe` / `AverageMae` | no (correct omit) | no | no | no |

**“Except implicitly” audit.** The only honest implicit paths are:

1. **Object plumbing.** `features` is passed into `FromBaseline`. That is not a formula term.
2. **Test freeze.** Every unit-score fixture holds 30 minutes (`AddMinutes(30)`). Quality numbers in tests are therefore **conditioned on 1800 s**, but the scorer would emit the **same** numbers for 9 s holds.
3. **Correlation folklore.** A human might believe long holds correlate with SL use or lower lot CV. The code does not encode that.

There is **no** implicit `quality -= f(hold)` and **no** implicit `risk += f(short hold)`. A22’s 10 % `hold_stability_score` is **not** the current behavior body.

### 2.2 State machine also ignores hold

```189:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
        if (quality >= 55)
            return TraderState.WATCH;
        return TraderState.EARLY_SCORE;
    }
```

Copyable set in `CopyTradingService.GenerateShadowIntentsAsync`:

```csharp
var copyable = new[] { TraderState.SHADOW, TraderState.LIVE_CANDIDATE, TraderState.LIVE };
```

`CanPromoteToLive` is hard-`false` (S011). The live-adjacent tokens are still in the copyable array. The **only** measured on-ramp today is `SHADOW` (`quality >= 70 && risk < 40`). Hold cannot keep a scalper off that ramp.

### 2.3 Persistence drops the number even if a later gate wanted it

`TraderScore` (`D:\Prop\src\Domain\Entities\TraderScore.cs`): `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CompletedXauTrades`, `Martingale`, `AveragingDown`, `LotEscalation`, `CurrentState`, `LastScoredAt`.

`ReconstructionScoringService.RebuildTraderAsync` copies those fields only. After the process exits `Score()`, `AverageHoldSeconds` is garbage-collected. A copy worker that queried scores **cannot** apply a hold floor without re-scoring reconstructed trades.

Dashboard `TraderRowDto` likewise has no hold. Operators cannot sort SHADOW by hold even by eye.

---

## 3. Identical quality: scalp vs swing (worked example)

Unit helper (the only scored shape under test):

```43:73:D:\Prop\tests\Unit\BaselineScorerTests.cs
    private static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots = 0.10m) =>
        new()
        {
            // …
            OpenedAt = DateTimeOffset.UnixEpoch.AddHours(n),
            ClosedAt = DateTimeOffset.UnixEpoch.AddHours(n).AddMinutes(30),
            // … InitialSl = 2290, lots constant, no average-down …
            NetRealizedPnl = pnl,
            Completed = true
        };
```

`Three_disciplined_winners_go_to_shadow_not_live` scores `{+80, +70, +90}` at **1800 s** each.

Algebra for that book (and for the **same** book with 90 s holds):

| Term | Value |
|---|---|
| `N` | 3 → `earlyEligible` |
| `NetPnl` | +240 → quality +15 |
| `ProfitFactor` | 99 (no losses) → +10 and +5 |
| `Martingale` / `AveragingDown` / `LotEscalation` | false |
| `LotCv` | 0 (constant 0.10) |
| `SlUseRate` | 1.0 |
| `MaxDrawdown` | 0 |
| `LossSizeCv` | 0 |
| **risk** | **0** |
| **behavior** | **100** |
| **quality** | `50 + 15 + 10 + 5 + 20 − 0 = 100` |
| **state** | `100 >= 70 && 0 < 40` → **`SHADOW`** |

Change only `ClosedAt` to `OpenedAt + 90 seconds`. Every row in that table is unchanged. `AverageHoldSeconds` becomes `90` and is ignored. **Scalper quality = swing quality = 100. Both SHADOW.**

That is the profit bug.

A22 fixture (`t1` 45 m / `t2` 30 m / `t3` 50 m) assumed multi-tens-of-minutes holds. The product scorer cannot fail a book that violates that assumption.

---

## 4. Why this is dest-PnL, not aesthetics

Architecture §36: stale XAUUSD copy **destroys expected edge**. Measured hop law in this tree:

| Clock | Value | Path |
|---|---|---|
| `RiskLimits.MaxSourceSignalAge` | **15 s** | `RiskEngine.cs` L16; OPEN/INCREASE → `SIGNAL_STALE` (L113–115) |
| `CopyIntent.ExpiresAt` | **now + 15 s** | `CopyTradingService.cs` L143 |
| Demo persist expiry | `OpenedAt + 15 s` | `EfTradingStore.cs` L306 |
| Shadow modeled delay | 80 ms in the live generator | `SimulateEntry(..., TimeSpan.FromMilliseconds(80))` — **not** a measured hop |
| `CanPromoteToLive` | false | no live send today (S011 / S002) — the ranking still fills the copyable set |

P500_PROFIT_SYNTHESIS §2.4 already named a real Achiever SHADOW that this gap would promote:

| Login | XAU trades | Source PnL | Hold | Why dest dies |
|---|---:|---:|---:|---|
| 322947 | 194 | +4,950 | **~163 s** | Gold scalps die in dest spread + 15 s `MaxSourceSignalAge` |

A 163 s mean hold is **11×** the 15 s stale window and still **too short** once dest bid/ask, 0.05 allocation, and missed first impulse are paid. P500_CODE_15 proposed rejecting OPEN when `AverageHoldSeconds < 180`. That floor is only **12×** the stale window and still inside typical gold scalp duration. **This slot rejects 180 s as too loose.**

Copy economics (order-of-magnitude, not a fabricated tick path):

| Source mean hold | After a 15 s dest hop | Fraction of life remaining at dest entry | Typical dest outcome |
|---|---|---|---|
| 30–90 s (scalp) | 15–75 s, often already closed | 0–50 %, often **0** (source flat before NOS) | Spread + slip ≥ remaining move. Negative expectancy. |
| ~163 s (322947) | ~148 s if still open | ~90 % of a **tiny** move | First impulse already gone. Synthesis already calls this uncopyable. |
| 180 s (CODE_15 floor) | 165 s | Still a scalp vs gold spread | Unproven. Do not treat as copyable. |
| **900 s (15 min)** | 885 s | **~98 %** of a swing-ish life | Hop is a small fraction of hold. Edge, if any, can survive dest costs. |
| 1800 s (test fixture) | 1785 s | ~99 % | Same class as the 15 min floor. |

`SIGNAL_STALE` already refuses **late** copy of a **single** signal. It does **not** refuse a **trader** whose entire book is shorter than dest survival. That is the missing gate.

`TradeFrequencyPerDay` being unused is a sibling smell (burst scalpers also look “high quality”) but is **not** a substitute for a min-hold floor. Frequency can be high with 20-minute holds, or low with 30-second trades on a thin sample.

---

## 5. Recommendation (do not implement in this slot)

### 5.1 Binding proposal

**Copy eligibility floor:** `AverageHoldSeconds >= 900` (15 minutes).

Apply as a **hard gate**, not a quality nudge:

| Place | Why |
|---|---|
| `TraderStateMachine.FromBaseline` | If `N >= 3` and mean hold `< 900`, do **not** emit `SHADOW` (keep `WATCH` or a dedicated non-copyable token). Quality may still be computed for research. |
| `CopyTradingService` copyable set | Defense in depth: even a stale score row must not generate OPEN intents when hold `< 900`. Requires **persisting** `AverageHoldSeconds` on `TraderScore` (it is dropped today). |
| Per-intent (optional later) | Individual tickets with hold `< 900` (or still-open age `< 900` at decision) stay off OPEN. Mean-only can hide a mixed book. |

Do **not** “fix” this by `quality -= 5` when hold is short. Three +80 / +70 / +90 scalps still land 95+ and pass `quality >= 70`. A bonus cannot outvote a perfect PF book.

Do **not** invent MFE/MAE to justify the floor. Hold is deal-supported. Ticks stay `Unavailable` (S015).

Do **not** flip `CanPromoteToLive`. The floor is about who is allowed into **SHADOW / copyable**, not about going live.

### 5.2 Why 15 minutes, not 180 seconds

| Floor | Multiple of 15 s stale | Verdict |
|---|---:|---|
| 180 s (P500_CODE_15) | 12× | Still gold-scalp territory. 322947 (~163 s) would sit **just under** it; a 190 s cousin would pass and still die in spread. |
| **900 s (this slot)** | **60×** | Hop + dest spread are a small fraction of life. Matches A22’s example holds (30–50 min) in spirit. Provisional (A22 I12) — retune only with measured dest hop p95 + dest spread vs source MFE when EXACT exists. |
| 1800 s (test freeze) | 120× | Safer, but excludes legitimate 20-minute XAU swings. 15 min is the eligibility **minimum**, not the target style. |

Retune rule (when someone later implements): if measured `source_to_fill` p95 is `H` seconds and dest half-spread is `S` (price), require mean hold `>> H` **and** typical source favorable excursion `>> S`. Until that tape exists, **900 s** is the conservative default. Tightening later is allowed. Loosening toward 180 s is not, unless dest hop p95 is proven small **and** dest spread is a small fraction of median source MFE (`feature_quality == EXACT` only).

### 5.3 A22 hold_stability is complementary, not a substitute

| Control | What it catches | What it misses |
|---|---|---|
| `hold_stability_score` (CV) | Mixed 5 s + 5 h books, fat-finger noise (`Max(hold) < 5` cap) | A **consistent** 60 s scalper has **low** CV and would **score well** |
| **Min mean hold 900 s** | Entire short-hold books | Mixed book whose mean is 20 min but half the tickets are 30 s |

Need **both** if A22 is ever implemented. This slot’s profit bug is the **mean-hold** hole. Implementing only CV would greenwash disciplined scalpers.

### 5.4 Tests that must exist before any product edit (future increment)

None of these exist today. When a later wave implements the gate, require:

1. `ComputeFeatures` mean: three trades 60 / 120 / 180 s → `AverageHoldSeconds == 120`.
2. Same PnL/SL/lots, 90 s vs 1800 s → **identical** quality **until** the gate is added; after the gate, 90 s must **not** be `SHADOW`.
3. Mean 899 s → not copyable; mean 900 s → existing quality/risk rules apply.
4. `N < 3` still `INSUFFICIENT_DATA` regardless of hold.
5. Persist + copyable query: a `SHADOW` row with stored hold 120 s generates **0** new OPEN intents.

### 5.5 Explicit non-goals

- Do not hand-write MQ5 / do not touch EX5 lab from this slot.
- Do not persist hold by editing product in this wave.
- Do not treat dashboard absence as the bug. The bug is **ranking + eligibility**.
- Do not claim live dest PnL was measured for 15 min vs 3 min. The 900 s floor is a **loss-avoidance prior**, not an OOS proof of max profit.

---

## 6. Cross-links (not copied as verdict)

| Doc | Relation |
|---|---|
| `P500_PROFIT_SYNTHESIS.md` §2.6 | Same finding in one bullet. This slot is the measured expansion + 15 min floor. |
| `P500_CODE_15.md` | Proposed `< 180 s` reject. **Superseded here as too loose** for copy eligibility. |
| `P500_CODE_35.md` | Hold is a source feature, not a dest survival test — agreed; the floor is the dest-survival **policy** on that source feature. |
| `P500_S015_no_mae.md` | MFE/MAE stay omitted. Hold does not require ticks. |
| `A22_scoring_spec.md` | `hold_sec_i`, `hold_cv`, 10 % behavior weight — **unimplemented**. |
| `A73_copy_latency.md` | 15 s `SIGNAL_STALE` is per-signal, not per-trader. |
| `D34_score_tests.md` / `E019_score_cov.md` | `AverageHoldSeconds` unasserted; fixtures always 1800. |
| `docs/scoring.md` | Silent on hold. High quality + low risk → SHADOW. |

---

## 7. Close

`AverageHoldSeconds` is reconstructed, then ignored by `Score()`, `FromBaseline`, persistence, dashboard, and the copyable query. Quality therefore grades **source dollar process** as if dest hop were free. It is not: 15 s stale + dest gold spread kill scalps that look perfect at trade #3.

**Copy eligibility should require min average hold 15 minutes (900 s).** Implement later as a hard gate, persist the field, and add the missing hold tests. Do not bury it as a small quality weight.

*End of P500_S056. Product source was not modified.*
