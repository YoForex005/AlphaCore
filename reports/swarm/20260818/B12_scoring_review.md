# B12 — BaselineScorer review (trade #3 SHADOW, leakage, formulas)

**Artifact:** `D:\Prop\reports\swarm\20260818\B12_scoring_review.md`  
**Date:** 2026-08-18  
**Agent:** B12  
**Product source modified:** **none** (read-only review).  
**Assigned question:** read `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`; check **trade #3 = SHADOW not LIVE**, **leakage**, and **formulas**.

**Source of law:** architecture v2 §§1.4, 15, 17–23; `A22_scoring_spec.md` (`baseline.v1`); `A69_trader_states.md`; `A45_mfe_mae_policy.md`.  
**SUT:** `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (the only scoring type in Domain). Callers inspected: `ReconstructionScoringService`, `EfTradingStore.UpsertScoreAsync`, `EfDashboardQueries.GetTradersAsync`, demo seed path.

---

## Verdict (honest)

| Question | Answer |
|---|---|
| Can trade #3 become `LIVE` or `LIVE_CANDIDATE` in this stub? | **No.** `FromBaseline` never returns those tokens. `CanPromoteToLive` is hard-`false`. `AfterHighEarlyScore()` pins `SHADOW`. |
| Is that the A22/A69 safety gate? | **No. Vacuous.** Live is unreachable at *every* `N`, not locked at `N == 3`. There is no R5/R6, no `N > 3` conjunct, no `ScoreConfig.MIN_LIVE_TRADES` loader. Adding a live path later without R5 would open the hole. |
| Trade #3 + high score → `SHADOW`? | **Sometimes.** Clean profitable book → `SHADOW`. Winning martingale with ratio in `[1.25, 1.50)` and `risk < 40` also → **`SHADOW`**. Spec: `FLAG_MARTINGALE` floors risk at 80 and lands `RISK_BLOCKED`. |
| A22 Case B (huge NET, 2×/2× martingale) → `RISK_BLOCKED`? | **FAIL.** Stub lands **`WATCH`** (`risk = 70`, NET > 0). Dollars buy a watch seat. |
| Formulas match `baseline.v1`? | **No.** Additive heuristic, not weighted Lerp + floors + `U(N)`. Different martingale/escalation ratios. Population CV, not sample. `U(3)=18` missing → quality can be **100** at `N=3` (spec cap **82**). |
| Leakage? | **No dest-P&L / MFE fabrication / clock inside the calculator.** **Yes** as-of/`FIRST3` isolation missing, NET sign inside quality (I9), provisional `N<3` scores published, SL always 0 on ingest, no dirty filter, unstable trade order. |
| Tests? | **None.** Zero `BaselineScorer` / `TraderStateMachine` / `ThreeTradeSafetyGate` tests under `D:\Prop\tests`. |
| Claim `baseline.v1` implemented? | **False.** This is an unversioned stub. Do not gold-file A22 Case A–F against it. |

**One-line:** trade #3 cannot be `LIVE` today only because **nothing can be `LIVE`**; the formulas are **not** A22; a winning martingale at `N=3` can still be **`SHADOW`**.

---

## 1. What is actually in the file

One file, three types:

| Type | Role |
|---|---|
| `FeatureSnapshot` | Window aggregates + unused MFE/MAE slots |
| `BaselineScore` | Three scores + `SuggestedState` + `EarlyScoreEligible` |
| `BaselineScorer` | `ComputeFeatures` + `Score` + population CV |
| `TraderStateMachine` | Stub resolver + two pins |

Constants / pins:

```40:40:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public const int EarlyScoreTradeCount = 3;
```

```209:211:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;
    public static bool CanPromoteToLive(TraderState current) => false;
}
```

No `ScoreConfig`. No window id (`EXPANDING` / `FIRST3` / `PROVISIONAL`). No `as_of`. No flags. No `ResolveState` R0–R9. No `DateTime.UtcNow` inside the calculator (I11 **pass** for the SUT itself).

`Score()` is the only public scoring entry. It always:

1. Re-filters `completed && IsXauUsd`, sorts by `ClosedAt` only.
2. Builds features on the **entire** list (caller-supplied window).
3. Sets `EarlyScoreEligible = N >= 3` (boolean, not the §15 one-shot event).
4. Adds/subtracts flat points for risk / behavior / quality.
5. Calls `TraderStateMachine.FromBaseline`.

`AfterHighEarlyScore` is **never called** by `Score()`. It is a dead pin until tests exist.

---

## 2. Trade #3 = SHADOW, not LIVE

### 2.1 Binding rule (not optional)

Architecture §1.4 / §23 and A22 I4–I5 / A69 S4–S5:

```text
WHEN N == 3:
    next_state ∈ { EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED }
    next_state ∉ { LIVE, LIVE_CANDIDATE }
    Trade #3 + high score → SHADOW only
    EVEN IF early_quality == 82 and NET is huge
```

Event on first crossing: `EARLY_SCORE_ELIGIBLE`. Never `PROVEN_PROFITABLE`.

### 2.2 Stub resolver (measured)

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

Reachable tokens: `INSUFFICIENT_DATA`, `RISK_BLOCKED`, `SHADOW`, `WATCH`, `EARLY_SCORE`.  
Unreachable: `LIVE`, `LIVE_CANDIDATE`, `PAUSED`, `DISQUALIFIED`.

### 2.3 Checklist vs A22 I4 / I5 / T4 / T5 / T6

| ID | Required | Stub | Result |
|---|---|---|---|
| I2 / T3 | `N==3` emits `EARLY_SCORE_ELIGIBLE` once | `EarlyScoreEligible` is a sticky `N>=3` bool; no event, no idempotent first-crossing | **FAIL** |
| I4 | `N==3` cannot be `LIVE` / `LIVE_CANDIDATE` | Those tokens are unrepresentable in this function | **PASS (vacuous)** |
| I4 | `N==3` may be `PAUSED` / `DISQUALIFIED` | Unreachable | **MISSING** |
| I5 / T5 | High score @3 → `SHADOW` | Yes **iff** `quality >= 70` **and** `risk < 40` | **PARTIAL** (thresholds 70/40 ≠ `SHADOW_MIN=62`) |
| A69 S5 | Even quality=82 cannot be live | Live unreachable | **PASS (vacuous)** |
| T4 property | `∀ N==3: state ∉ {LIVE, LIVE_CANDIDATE}` | True today | **PASS (vacuous)** — **no unit test** |
| T6 | `AfterHighEarlyScore() == SHADOW` | Hardcoded | **PASS** — **no unit test** |
| A69 TS22 | `CanPromoteToLive` false when `N<=3` | False for **all** states / all `N` | **PASS-safer** — later must become R6+approve, still false at `N==3` |
| Case B / T6 | Winning martingale @3 → `RISK_BLOCKED` | `WATCH` (see §4.3) | **FAIL** |
| Case D / T7 | `N<3` → `INSUFFICIENT_DATA`, no official score | State OK for `N==0` and `1..2`; **quality still computed and persisted** | **PARTIAL** |
| Case E | `N=20` + shadow sample → `LIVE_CANDIDATE` | Impossible | **MISSING** (safe, incomplete) |
| Case F | Illegal `MIN_LIVE_TRADES=3` still cannot live @3 | No config / no loader | **MISSING** (live still unreachable) |

There is **no** `if (N == 3) forbid LIVE` line. Safety is “we forgot live,” not “we locked the gate.” A69 §13 already named this stub `EXISTS_NEEDS_REFACTOR`. That classification still holds.

### 2.4 Worked state landings (hand-evaluated on the stub)

Constants = stub additives. Volumes in lots. P&L already net. No MFE.

#### Case A — A22 §12.1 clean book (must be SHADOW, never LIVE)

```text
t1 0.10  +80  sl=yes
t2 0.10  -40  sl=yes
t3 0.10  +60  sl=yes
```

| Quantity | Stub | A22 v1 |
|---|---|---|
| `N` / NET / PF | 3 / +100 / 3.50 | same |
| `lot_cv` / martingale / sl_rate | 0 / false / 1 | 0 / 0 events / 1 |
| `risk_score` | **0** | low, no floors |
| `behavior_score` | **100** | high |
| `early_quality_score` | **100.00** (`50+15+10+5+20-0`) | **≤ 82** after `U(3)=18` |
| `state` | **`SHADOW`** | **`SHADOW`** |
| `LIVE` / `LIVE_CANDIDATE` | no | no |

**State: PASS. Numbers: FAIL** (no uncertainty penalty; over-confident leaderboard).

#### Case B — A22 §12.2 huge profit + martingale (must be RISK_BLOCKED)

```text
t1 0.10  -50   sl=no
t2 0.20  -100  sl=no   // 2.0× after loss
t3 0.40  +800  sl=no   // 2.0× after loss
NET=+650
```

Stub:

```text
martingale=true, lotEscalation=true, lotCv≈0.5345, sl_rate=0
max_dd=150, GP=800 → max_dd>GP? false
risk = 35+15+10+10 = 70
behavior = 100-30-10-10 = 50
quality = 50+15+10+5 + 10 - 17.5 = 72.50
RISK_BLOCKED? risk>=80? no.  (martingale ∧ maxDd>0 ∧ NET<0)? NET>0 → no
quality>=70 ∧ risk<40? 70 is not <40 → no
quality>=55 → WATCH
```

**State: `WATCH`. Spec: `RISK_BLOCKED`.** This is the load-bearing anti-pattern and the stub **fails** it. Dollars do not buy a live seat (vacuous) but they **do** buy a mid-band operational state.

#### Synthetic — mild winning martingale (the SHADOW hole)

```text
t1 0.10  -50   sl=yes
t2 0.13  -50   sl=yes   // 1.30× after loss ≥ 1.25, < 1.50
t3 0.13  +400  sl=yes
```

```text
risk = 35          // martingale only; lotCv≈0.12; sl=1; maxDd=100 < GP=400
behavior = 70
quality = 85.25
85.25>=70 ∧ 35<40 → SHADOW
```

**A martingale book at trade #3 is `SHADOW`.** A22: `FLAG_MARTINGALE` is severe, risk floor 80, behavior cap 35, state `RISK_BLOCKED`. This is the most important SHADOW-vs-LIVE-adjacent defect: the stub will **start destination-quote simulation** (A24 eligibility is `{SHADOW, LIVE_CANDIDATE, LIVE}`) on a size-up-after-loss book.

#### Demo seed (measured arithmetic; no SL on the deal DTO)

| Login | Book | Stub risk / behavior / quality | Stub state | Spec qualitative |
|---|---|---|---|---|
| 10001 | 3 XAU, NET≈+223.6, flat 0.10, no SL | 10 / 90 / **95.50** | **SHADOW** | SHADOW *if* quality still ≥62 after U(3); SL-rate 0 is watch-flag only |
| 10002 | 0.10→0.20→0.40, all losses, NET≈−2107 | 80 / 50 / 40.00 | **RISK_BLOCKED** | RISK_BLOCKED (agrees because NET<0 **and** risk hits 80) |
| 10003 | 0 trades | 0 / 100 / **40.00** persisted | INSUFFICIENT_DATA | no official score |
| 99001 | 3 small wins, no losses, PF stub=**99**, no SL | 10 / 90 / **95.50** | **SHADOW** | SHADOW after U(3); PF must be capped at 5 |

10002 is blocked only because it **lost**. Flip the last trade to a large win (Case B) and the same pattern becomes `WATCH`.

### 2.5 Caller does not add a live path

`ReconstructionScoringService.RebuildTraderAsync` writes `CurrentState = score.SuggestedState` and nothing else. No RBAC live-approve, no shadow-sample gate, no `risk_engine_live_ok`. Dashboard counts `LIVE` / `LIVE_CANDIDATE` but the scorer cannot produce them.

**Conclusion on the assigned SHADOW question:**  
**Trade #3 is not LIVE. Trade #3 is not automatically the correct SHADOW/WATCH/BLOCKED landing.** Treat `AfterHighEarlyScore` / `CanPromoteToLive` as pins, not as the machine.

---

## 3. Leakage

Architecture §20 + A22 I6–I9, T8, T16. Scoring as-of `T` may use only information available at `T`. The three scores are **source-behavior** scores. Shadow/live destination P&L is a **state gate** input only. Do not rank by raw NET. Do not fabricate MFE/MAE.

### 3.1 Leakage matrix

| ID | Channel | In `BaselineScorer`? | In caller / persist / UI? | Verdict |
|---|---|---|---|---|
| L1 | Trade `#n+1` inside a Trade-#3 / `FIRST3` score (I6, T8) | Calculator scores **whatever list it is given**. No `window`, no reject-if-`Count!=3` when `FIRST3`. | `RebuildTraderAsync` always passes **all** completed XAU. No FIRST3 snapshot. History rows have no `n` / `window` / `as_of=closed_at(n)`. | **OPEN.** Operational expanding is OK. **Research / ML leakage is unprevented:** later training on `trader_scores.EarlyQualityScore` as if it were FIRST3 will include trades 4+. T8 cannot even be written against this API. |
| L2 | Future balance / challenge pass-fail / group plan (I6, §20) | Not read | Not mapped into `Score()` | **CLEAN** |
| L3 | Destination / shadow P&L inside the three formulas (I8) | Not read | Overview sums `ShadowOrders.SourceVsShadowSlippage` for a **dashboard** number only; not fed back | **CLEAN** (state gate that *should* read shadow sample is also missing — that is incompleteness, not leakage) |
| L4 | Raw NET ranking (I9, T16) | `if (NetPnl > 0) quality += 15` | Leaderboard `OrderByDescending(EarlyScore)` | **LEAK / POLICY FAIL.** Sign-of-NET is a quality term. Two identical-process books, one −1 and one +1, differ by **15** quality points plus any PF gap. Magnitude of NET is not used (T16 magnitude pair would be close if both profitable with same PF). |
| L5 | Fabricated MFE/MAE from deals (I7, A45) | Forced `MaeMfeQuality = Unavailable`; `AverageMfe` / `AverageMae` never set | No tick tape | **CLEAN** (omission, correct). `PriceSource.Unknown` is stamped but carries **no number** — A45 forbids UNKNOWN *with* a number; omit-with-Unknown is a naming smell, not a silent mix. |
| L6 | cTrader quotes as source ticks (A45 mix) | Unused | `DestinationQuotes` exist for FIX UI only | **CLEAN** |
| L7 | `DateTime.UtcNow` inside the pure function (I11) | Absent | `LastScoredAt = DateTimeOffset.UtcNow`; history `RecordedAt` is wall clock, not `closed_at` | Calculator **CLEAN**. Persist **dirty clock**: replay of the same close is not bit-identical as a history row (new `Id`, new `RecordedAt`). |
| L8 | Open / non-XAU / partials counted as trades (I1) | Re-filters `Completed && IsXauUsd` | Reconstructor already defines completed = flat lifecycle | **MOSTLY CLEAN.** Missing `closed_at IS NOT NULL`, `closed_volume > 0`, `!Dirty` (A83). |
| L9 | Unstable trade order → lookahead on adjacent pairs | `OrderBy(ClosedAt)` only | Spec: `closed_at, opened_at, id` | **NONDETERMINISM.** Equal `ClosedAt` can flip martingale/escalation pair direction. A22 T2 FAIL. |
| L10 | Provisional `N<3` scores on the official leaderboard | Quality still computed; capped at 40 when `N<3`; `N==0` empty snapshot then quality walks 50→40 | `UpsertScoreAsync` always writes; `GetTradersAsync` does not hide `INSUFFICIENT_DATA` | **PUBLISH LEAK.** Demo 10003 would sit on the board with `EarlyQualityScore=40`. |
| L11 | Canceled / dirty reconstructions (A83) | No dirty filter | No `Dirty` field | **OPEN** when F17 lands |
| L12 | SL never ingested → every book looks like no-SL | `InitialSl.GetValueOrDefault() > 0` | `Mt5DealDto` has **no** SL/TP; `LoadDealsAsync` never sets `NormalizedDeal.StopLoss` | **INPUT HOLE.** Demo 10001/99001 take +10 risk / −10 behavior for “no SL” regardless of the real book. Not future leakage; it **poisons** risk/behavior for every live ingest. |
| L13 | ML target leakage | No ML | `A52` / no labels | **N/A** — but L1+L4 are exactly the features a later FIRST3 model would leak if trained on this table |

### 3.2 As-of contract is missing (the real §20 hole)

A22 §2.4: calculator receives a list **already filtered to `T`**, plus `ScoreAsOf { n, as_of, window, score_version }`. T8: a `FIRST3` call that receives 4 trades **rejects**.

Actual API:

```129:132:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    public BaselineScore Score(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var features = ComputeFeatures(completedXau);
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;
```

```85:86:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
```

No slice to `[1..3]` for the frozen snapshot. `TraderScore` has no `n` / `as_of` / `score_version` / `window`. `TraderScoreHistory` is an append of the **current expanding** triple + wall-clock. Continuous rescoring §22 is “overwrite current + append an unlabeled row,” not “FIRST3 once + EXPANDING after 4,5,6… with a unique key.”

If Phase 6 trains on these rows as “features observable through completed trade #3” (arch §19), **that is leakage**. The stub does not protect you.

### 3.3 What is *not* leaking (do not over-report)

- Shadow fill prices are not inputs to `risk_score` / `behavior_score` / `early_quality_score`.
- No challenge-phase feature.
- Drawdown is completed-trade equity on the **same** window (A22 §3.4 intentional; not MTM lookahead).
- MFE/MAE are not invented from entry/exit VWAP.
- The Domain project has no ML package reference (A22 T17 holds for this assembly).

---

## 4. Formulas — stub vs `baseline.v1`

A22 locks **structure**. Numbers in `ScoreConfig.v1` are provisional. The stub hardcodes a different structure **and** different numbers, unversioned.

### 4.1 Feature formulas

| Feature | Stub | A22 `baseline.v1` | Match? |
|---|---|---|---|
| Universe | `Completed && IsXauUsd` | completed + canonical XAU + `closed_at` + `closed_volume > 0` (+ `!Dirty` when A83 lands) | **partial** |
| Order | `ClosedAt` only | `closed_at, opened_at, id` | **no** |
| `N` | count | count | yes |
| `NET` / `GP` / `GL` | sums of net; wins `>0`, losses `<0` (BE dropped from both) | same shape | yes |
| `pf` | `GL<=0 ? (GP>0 ? 99 : 0) : round(GP/GL, 4)` | `GL>0 → min(GP/GL, 5)`; `GP>0` → **5**; else **1** | **no** (99 vs 5; all-BE is 0 vs 1) |
| `lot_cv` | population σ / mean; `<2` → 0 | **sample** σ (`1/(m-1)`); `<2` → 0 | **no** |
| `loss_cv` | same pop-CV on `\|loss\|` | sample CV; 0 or 1 loss → 0 | **no** (estimator) |
| Martingale | **boolean**; any adjacent `pnl<0` and `vol_{i+1} > vol_i * 1.25` | **event count**; ratio `>= 1.80` | **no** (1.25 vs 1.80; bool vs count) |
| Lot escalation | boolean; any adjacent `vol_{i+1} > vol_i * 1.50` | `vol_span` + events at ratio `>= 2.00` | **no** |
| Revenge | absent | events at ratio `>= 1.40` after a loss | **missing** |
| Averaging | `Any(WasAveragedDown)` | `avg_down_n / N` + flag severity at `N<=3` | **partial** |
| Hold | mean seconds; negative possible | `Max(0, Δ)` + `hold_cv` + sub-5s cap | **partial** |
| SL rate | `InitialSl.GetValueOrDefault() > 0` | `initial_sl IS NOT NULL` | **no** (and ingest never supplies SL) |
| Drawdown | completed-trade peak-to-trough, **absolute $** | same path + `max_dd_frac = max_dd / scale` | **partial** (no fraction, no `DD_SCALE_EPS`) |
| Frequency | `N / max(1, closed_N − closed_1 in days)` | `N / max(1s, closed_N − opened_1)` + burst `<300s` | **no** |
| Session | absent | UTC hour buckets + `session_max_frac` | **missing** |
| R-multiple / expectancy | absent | defined when SL+entry+contract size exist | **missing** |
| MFE/MAE | always `Unavailable` / unused | only if `feature_quality==EXACT` | **correct omit** |
| Flags | three booleans on `TraderScore` | A22 §8 catalog + severity | **missing** |

CV implementation (population, not sample):

```174:184:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
    private static decimal CoefficientOfVariation(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2)
            return 0;
        var mean = values.Average();
        if (mean == 0)
            return 0;
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        var std = (decimal)Math.Sqrt((double)variance);
        return decimal.Round(Math.Abs(std / mean), 4);
    }
```

`.Average()` of squared deviations is `1/m`, not `1/(m-1)`. A22 §2.6 is explicit.

### 4.2 Score formulas

**Stub risk** (higher = worse). Additive flags, cap 100:

```text
+35 martingale
+20 averaging
+15 lot escalation
+10 lot_cv > 0.5
+10 sl_rate < 0.3
+10 max_dd > 0 and GP > 0 and max_dd > GP
```

**A22 risk:** weighted Lerp body (weights sum to 1.00) then **floors** (`Max`, not add): martingale floor **80**, averaging 75, abnormal sizing 70, severe DD 75, burst 60, no-SL 55, MAE-over-SL 70.

Consequence: one martingale event is **35** stub points (still eligible for SHADOW if nothing else fires) vs **floor 80** in v1 (cannot be SHADOW; R3 → `RISK_BLOCKED`).

**Stub behavior** (higher = better):

```text
100
-30 martingale
-15 averaging
-10 lot_cv > 0.4
-10 sl_rate < 0.5
-10 loss_cv > 0.8
clamp [0,100]
```

**A22 behavior:** weighted process scores + **caps** (`Min`): martingale cap **35**, averaging 45, abnormal sizing 50. Independent of dollar P&L (stub also avoids NET here — good).

**Stub quality:**

```text
50
+15 if NET > 0          // I9 violation
+10 if pf >= 1.2
+5  if pf >= 1.8
+ 0.20 * behavior
- 0.25 * risk
if N < 3: min(quality, 40)
Round2, clamp [0,100]
```

**A22 quality:**

```text
raw = 0.45*behavior + 0.35*(100-risk) + 0.12*pf_score + 0.08*expectancy_score
early_quality = Clamp(raw - U(N), 0, 100)
U(3)=18, U(40+)=0
P&L-shaped terms ≤ 20% combined
NET currency is a forbidden input
```

| | Stub max at `N=3` | A22 max at `N=3` |
|---|---:|---:|
| Perfect clean book | **100** | **82** |
| NET used? | yes (+15) | **forbidden** |
| Rounding | `decimal.Round(x, 2)` = **ToEven** | `AwayFromZero` |

`U(N)` is the deliberate “trade #3 is not a proven live trader on the leaderboard” term (A22 §7.2). The stub deletes it. Combined with dashboard sort-by-`EarlyScore`, a three-trade winner looks **fully proven**.

### 4.3 Thresholds vs A22 / A69

| Knob | Stub | A22 v1 (provisional) |
|---|---|---|
| Early eligible `N` | 3 | 3 |
| SHADOW | `quality>=70` **and** `risk<40` | `early_quality >= 62` after U(N), via R5; severe flags already took R3 |
| WATCH | `quality>=55` | `>= 48` |
| RISK_BLOCKED | `risk>=80` **or** (martingale **and** DD>0 **and** **NET<0**) | any **severe** flag (martingale is always severe) |
| LIVE | unreachable | R6 + `N>3` + `MIN_LIVE_TRADES=20` + shadow sample + two approve bits |
| Martingale ratio | **1.25** | **1.80** |
| Escalation ratio | **1.50** | **2.00** |
| Revenge ratio | — | 1.40 |
| PF cap | 99 | 5 |
| Score version | none | `baseline.v1` |

The NET<0 conjunct on `RISK_BLOCKED` is a second I9 failure at the **state** layer: **losing** martingales block, **winning** martingales do not. That is the Case B miss.

### 4.4 Rounding / purity / versioning

- Intermediate `ProfitFactor` uses `decimal.Round(..., 4)` ToEven; stored scores ToEven 2 dp. A22: `AwayFromZero`.
- `Math.Sqrt((double)variance)` can lose precision on huge P&L books; A22 allows double only inside Stdev then quantize.
- No `score_version` column. Changing a weight silently rewrites current scores. A22: formula change = `baseline.v2`; number change = `baseline.v1.N` stored on every snapshot.
- Calculator is pure given the list. Caller is not (clock, full history, no window).

---

## 5. Persistence / API coupling (not the SUT, but it is how scores escape)

| Surface | What happens | Defect vs A22 §10 / §9.6 |
|---|---|---|
| `TraderScore` | current triple + 3 bools + `CurrentState` + `LastScoredAt` | no `n`, `as_of`, `window`, `score_version`, `severe_risk`, `last_event` |
| `TraderScoreHistory` | same triple + `RecordedAt` | not append-only unique on `(as_of,n,version,window)`; no FIRST3 |
| `trader_feature_snapshots` | **missing** | no component breakdown |
| `trader_risk_flags` | **missing** | flags are denormalized bools on the score row |
| `trader_states` | **missing** | `CurrentState` on the score row is the only copy |
| Dashboard traders | `ORDER BY EarlyQualityScore DESC` | publishes `N<3` rows; ranks a NET-tinted score |

None of this was changed by this review.

---

## 6. Tests (required, absent)

A22 §13 / A27 §4.4 / A69 §14. Grep of `D:\Prop\tests` for `BaselineScorer`, `TraderStateMachine`, `EarlyScoreTradeCount`, `CanPromoteToLive`: **no hits**.

Must-fail-the-build cases that would fail **today** if written against A22 gold, or would pass only vacuously:

| Test | Against stub now |
|---|---|
| T4 `N==3` ⇏ LIVE | would **pass** (vacuous) |
| T5 Case A → SHADOW | state **pass**; numeric gold **fail** (100 vs ≤82) |
| T6 Case B → RISK_BLOCKED | **fail** (WATCH) |
| T7 Case D no official score | **fail** if the test reads persisted quality |
| T8 FIRST3 rejects 4 trades | **cannot compile** (no window arg) |
| T12 `U(3)=18` | **fail** |
| T16 no $ rank | **fail** on opposite-sign NET pair |
| A69 TS5 high score @3 → SHADOW | **pass** only for low-risk books |
| Martingale 1.80 | **fail** (fires at 1.25) |

Do not mark Phase 3 scoring done until these exist against a real `baseline.v1` calculator, not this stub.

---

## 7. Classification

| Item | Path | Class |
|---|---|---|
| Three named scores 0–100 | `BaselineScorer.Score` | **EXISTS_NEEDS_REFACTOR** |
| Feature snapshot record | `FeatureSnapshot` in same file | **EXISTS** (not the A22 persist entity) |
| `TraderState` nine tokens | `Domain\Enums\TraderState.cs` | **EXISTS_AND_GOOD** (vocab) |
| `FromBaseline` | same file | **EXISTS_NEEDS_REFACTOR** — not R0–R9 |
| `AfterHighEarlyScore → SHADOW` | same file | **EXISTS_AND_GOOD** (pin) |
| `CanPromoteToLive → false` | same file | **EXISTS** — safer than a premature live path |
| `N==3` hard exclusion of LIVE | — | **MISSING** (vacuous via no live path) |
| `U(N)`, Lerp maps, flags, `ScoreConfig` | — | **MISSING** |
| FIRST3 / as-of API | — | **MISSING** |
| MFE/MAE omit | `FeatureQuality.Unavailable` | **EXISTS_AND_GOOD** until ticks exist |
| SL on ingest | `Mt5DealDto` / `LoadDealsAsync` | **MISSING** (every SL rate is 0) |
| Unit tests | `tests\Unit` | **MISSING** |

---

## 8. What implementers must not do

```text
Do not treat this stub as baseline.v1
Do not gold-file A22 Case A–F against these additives
Do not add LIVE to FromBaseline without R5-before-R6 and MIN_LIVE_TRADES>3
Do not train ML on trader_scores.EarlyQualityScore as a FIRST3 feature
Do not “fix” U(3) by leaving it at 0 so the board looks confident
Do not fill MFE/MAE from entry/exit VWAP or cTrader quotes
Do not rank the official board by NET or by this stub’s quality until I9 is gone
Do not modify product source in the same change-set as this review
```

**Implement `baseline.v1` as a new versioned calculator** (A22 §11 API). Keep this stub only as a historical reference until the replacement is wired and the Case A–F fixtures pass.

---

## 9. Decision summary

| Question | Decision |
|---|---|
| Is trade #3 LIVE? | **No.** |
| Is trade #3 correctly gated to SHADOW? | **No** — SHADOW is the high-score landing only when `risk<40`; winning martingale can still be SHADOW; Case B is WATCH. |
| Are formulas A22? | **No.** |
| Is leakage contained? | **Dest-P&L / MFE / clock-in-SUT: yes. As-of/FIRST3 / NET-in-quality / provisional publish / SL hole: no.** |
| Product source changed? | **No.** |

*End of B12. Trade #3 ≠ LIVE. Trade #3 ≠ proven A22 SHADOW gate. Stub ≠ baseline.v1.*
