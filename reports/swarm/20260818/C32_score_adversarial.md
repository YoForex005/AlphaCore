# C32 — Adversarial: can `EarlyQualityScore >= 70` with martingale?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\C32_score_adversarial.md` |
| Agent | C32 (score adversarial) |
| Date | 2026-08-18 |
| Assigned question | Read `BaselineScorer`. **Can `quality >= 70` with martingale?** |
| Product source edited | **No** |
| SUT | `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` (`BaselineScorer` + `TraderStateMachine`) |
| SUT SHA-256 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` (8143 bytes, 212 lines) |
| Measurement harness | `D:\Prop\reports\swarm\20260818\_tmp_c32_score\` (Domain-only `dotnet run`, not product) |
| Measured dump | `D:\Prop\reports\swarm\20260818\_tmp_c32_score\C32_measured.tsv` |
| Adjacent (read, not SUT) | `ReconstructedTradeResult`; `DealIngestionService` persist; `RiskEngine` `MARTINGALE_BLOCK`; A22 `baseline.v1`; A24 shadow eligibility; B12 / B35 / C02 |
| Method | Read `Score` / `ComputeFeatures` / `FromBaseline` in full. Algebra on the additive surface. Execute the **live** `BaselineScorer` on 43 reconstructed books. Nothing answered from memory. |

**One-line:** **Yes.** On the implemented stub, a profitable martingale book at `N >= 3` scores **`70.25`–`85.25`**. A mild `1.26×`–`1.50×` size-up after a loss with SL lands **`85.25` and `SHADOW`**. A22 `baseline.v1` cannot reach 70 with `FLAG_MARTINGALE` (cap **`24.75`** at `N=3`).

---

## 0. Verdict

| Question | Answer | Evidence |
|---|---|---|
| Can `EarlyQualityScore >= 70` while `Features.Martingale == true`? | **YES** | Measured max **`85.25`**. Tight profitable / `1 < PF < 1.2` book still **`70.25`**. A22 Case B (2×/2×, last trade +800) is **`77.00`** with SL, **`72.50`** without. |
| Is that a rare corner? | **No.** Any `N>=3` book with `Martingale`, `NetPnl > 0`, and no extra risk/behavior hits is `>= 70.25`. | Surface in §3. |
| Can that book also be `SHADOW`? | **YES** if `risk < 40` (martingale-only `risk = 35`) | `MILD_1.26_SL` → `SHADOW`. |
| Does a winning 2× martingale get blocked? | **No.** It is `WATCH` (`risk = 60` or `70`, quality still `>= 70`). | `CASEB_SL` / `CASEB_NOSL`. |
| Can quality stay `>= 70` if the martingale **lost**? | **No.** `NetPnl <= 0` caps the martingale-only book at **`55.25`**. Losing + DD>GP is **`52.75`**. Unit FX-03 is **`47.00`**. | `NET_ZERO_MILD`, `NET_NEG1_MILD`, `FX03_LOSING`. |
| Can `N < 3` martingale hit 70? | **No.** Hard cap `min(quality, 40)`. | `N2_MILD_WIN` → `40`. |
| Does A22 `baseline.v1` allow `quality >= 70` with martingale? | **No.** Flag floors risk at 80, caps behavior at 35, subtracts `U(3)=18`. Max **`24.75`** at `N=3`, **`42.75`** even as `N→∞`. | §7. |
| Do unit tests lock this hole? | **No.** The only martingale fact is the **losing** 2×/2× book (`RISK_BLOCKED` via `NET < 0`, quality `47`). | `BaselineScorerTests.Martingale_after_losses_is_risk_blocked`. |

**Do not claim** “martingale cannot score high.” **Do not claim** “quality >= 70 implies a clean book.” **Do not claim** A22 Case B is blocked — it is `WATCH` with quality **77**.

---

## 1. What the stub actually computes

### 1.1 Martingale detector (boolean, adjacent pair)

```86:94:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var martingale = false;
        var lotEscalation = false;
        for (var i = 1; i < trades.Count; i++)
        {
            if (trades[i - 1].NetRealizedPnl < 0 && trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.25m)
                martingale = true;
            if (trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.5m)
                lotEscalation = true;
        }
```

Universe first: `Completed && IsXauUsd`, `OrderBy(ClosedAt)` only.

| Rule | Implemented | A22 v1 |
|---|---|---|
| Predicate | any adjacent pair | event count |
| After-loss ratio | **strict `>` `1.25`** | **`>= 1.80`** |
| Escalation | any adjacent **`>` `1.50`** | `>= 2.00` + vol-span |
| Size field | `MaxVolumeLots` | `max_volume` (same idea) |
| Exact `1.25×` after a loss | **not** martingale (`0.125 > 0.125` is false) | not martingale either |
| Exact `1.50×` after a loss | martingale **yes**, escalation **no** | neither (below 1.80 / 2.00) |

### 1.2 Score additives (the only quality law in product)

```134:160:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        if (features.LotCv > 0.5m) risk += 10;
        if (features.SlUseRate < 0.3m) risk += 10;
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
        risk = Math.Min(100m, risk);
        // ...
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
```

`decimal.Round(x, 2)` is **ToEven**. A22 wants `AwayFromZero`. No `U(N)`. Sign of `NET` is a quality term (A22 I9 forbid).

### 1.3 State (why quality 70 is not just a number)

```194:206:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;
        // ...
        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;
        if (quality >= 55)
            return TraderState.WATCH;
        return TraderState.EARLY_SCORE;
```

Martingale always implies a prior `pnl < 0`, and peak starts at 0, so **`MaxDrawdown > 0` whenever `Martingale`**. The block clause therefore collapses to:

```text
RISK_BLOCKED  ⇐  risk >= 80   OR   (Martingale AND NetPnl < 0)
```

Winning martingales are **never** blocked by the second clause. They only block if the additive risk stack hits 80 (martingale 35 + averaging 20 + escalation 15 + SL-or-CV-or-DD 10 = 80).

A24: `{SHADOW, LIVE_CANDIDATE, LIVE}` generate destination-quote simulation. A winning mild martingale that scores `SHADOW` is **shadow-eligible**.

---

## 2. Algebra — quality surface with `Martingale = true`

Let extra flags be off unless named. Then:

```text
risk     = 35 + 20·avg + 15·esc + 10·I(lotCv>0.5) + 10·I(sl<0.3) + 10·I(dd>gp ∧ gp>0)
behavior = 100 − 30 − 15·avg − 10·I(lotCv>0.4) − 10·I(sl<0.5) − 10·I(lossCv>0.8)
quality  = 50 + 15·I(NET>0) + 10·I(PF≥1.2) + 5·I(PF≥1.8) + 0.20·behavior − 0.25·risk
           then min(., 40) if N<3
```

### 2.1 Maximum (achievable)

Martingale only, `N>=3`, `NET>0`, `PF>=1.8`, SL on, lot CV ≤ 0.4, one or zero losses (loss CV = 0), no averaging, ratio in `(1.25, 1.50]`:

```text
risk = 35    behavior = 70
quality = 50 + 15 + 10 + 5 + 14 − 8.75 = 85.25
```

**Measured:** `MILD_1.26_SL`, `MILD_1.30_SL_B12`, `MILD_1.50_EXACT`, `PF_1.80_MILD`, `N4_EXPAND_MILD` all **`85.25`**.

85.25 is the **stub ceiling** for any martingale book. You cannot cancel the −30 behavior / +35 risk.

### 2.2 Necessary conditions for `quality >= 70`

| Condition | Why |
|---|---|
| `N >= 3` | Otherwise `min(quality, 40)`. Measured `N2_MILD_WIN = 40`. |
| `NetPnl > 0` | Without the +15, martingale-only max is `50 + 14 − 8.75 = 55.25`. Measured `NET_ZERO_MILD = 55.25`. `NET <= 0` also trips `RISK_BLOCKED`. |
| Extra penalties cannot eat the +15 | See §2.3. |

`NetPnl > 0` already implies `PF > 1` (unless a book of only zeros, which is not `> 0`). So a profitable martingale is always at least:

```text
65 + 0.20·B − 0.25·R
```

Martingale-only (`B=70`, `R=35`): **`70.25`**. That is why **barely-green** books still clear 70.

### 2.3 Extra-flag map (profitable, `N>=3`)

| Book extras | `1 < PF < 1.2` | `1.2 ≤ PF < 1.8` | `PF ≥ 1.8` |
|---|---:|---:|---:|
| martingale only | **70.25** | **80.25** | **85.25** |
| + escalation (`R=50`) | 66.50 | 76.50 | **81.50** |
| + no SL (`R=45`, `B=60`) | 65.75 | 75.75 | **80.75** |
| + `sl ∈ [0.3, 0.5)` (`R=35`, `B=60`) | 68.25 | 78.25 | **83.25** |
| + `lotCv > 0.5` (`R=45`, `B=60`) | 65.75 | 75.75 | **80.75** |
| + `lossCv > 0.8` (`R=35`, `B=60`) | 68.25 | 78.25 | **83.25** |
| + averaging (`R=55`, `B=55`) | 62.25 | 72.25 | **77.25** |
| + esc + lotCv (`R=60`, `B=60`) | 61.50 | 71.50 | **77.00** |
| Case B no SL (`R=70`, `B=50`) | 57.50 | 67.50 | **72.50** |
| Case B with SL (`R=60`, `B=60`) | 61.50 | 71.50 | **77.00** |
| avg+esc+no SL (`R=80`, `B=45`) | 50.00 | 60.00 | 69.00 → **`< 70`** and **`RISK_BLOCKED`** |

**Read:** with `PF ≥ 1.8` you have to stack **averaging + escalation + no-SL** (risk floor 80) before quality falls under 70. A “clean” martingale — or even a sloppy 2× recovery — **clears 70**.

### 2.4 `MaxDrawdown > GrossProfit` cannot save the gate on a winner

Completed-trade DD is a peak-to-trough drop. That drop is assembled from losses, so `MaxDrawdown ≤ GrossLoss`. `NetPnl > 0` ⇒ `GrossProfit > GrossLoss` ⇒ `MaxDrawdown ≤ GrossLoss < GrossProfit`. The addend

```text
MaxDrawdown > 0 && GrossProfit > 0 && MaxDrawdown > GrossProfit
```

is **impossible on a profitable book**. Measured `MILD_DD_GT_GP` / `MILD_DD_GT_GP_TRUE` keep `risk = 35` and quality `70.25`. The addend **does** fire on mixed losing books (`NET_NEG1_MILD` risk 45) — those are already `RISK_BLOCKED` via `NET < 0`.

---

## 3. Measured books (`BaselineScorer.Score`, Release / net8.0 / invariant)

Harness: `dotnet run -c Release --project D:\Prop\reports\swarm\20260818\_tmp_c32_score\C32ScoreEval.csproj`. Same `Closed(...)` shape as `BaselineScorerTests` (XAU, completed, 30-minute holds).

### 3.1 Load-bearing: quality ≥ 70 **and** martingale = 1

| id | lots after loss | NET | PF | risk | beh | **qual** | state |
|---|---|---:|---:|---:|---:|---:|---|
| `MILD_1.26_SL` | 0.10 → **0.126** → 0.10 | +200 | 5.00 | 35 | 70 | **85.25** | **SHADOW** |
| `MILD_1.30_SL_B12` | 0.10 → **0.13** / 0.13 | +300 | 4.00 | 35 | 70 | **85.25** | **SHADOW** |
| `MILD_1.50_EXACT` | 0.10 → **0.15** | +200 | 5.00 | 35 | 70 | **85.25** | **SHADOW** |
| `JUST_OVER_1.25` | 0.10 → **0.1250001** | +200 | 5.00 | 35 | 70 | **85.25** | **SHADOW** |
| `PF_1.05_MILD` | 0.10 → 0.13 | +5 | 1.05 | 35 | 70 | **70.25** | **SHADOW** |
| `PF_1.19_MILD` | 0.10 → 0.13 | +19 | 1.19 | 35 | 70 | **70.25** | **SHADOW** |
| `PF_1.20_MILD` | 0.10 → 0.13 | +20 | 1.20 | 35 | 70 | **80.25** | **SHADOW** |
| `PF_1.79_MILD` | 0.10 → 0.13 | +79 | 1.79 | 35 | 70 | **80.25** | **SHADOW** |
| `PF_1.80_MILD` | 0.10 → 0.13 | +80 | 1.80 | 35 | 70 | **85.25** | **SHADOW** |
| `MILD_SL_1of3` | sl rate 1/3 | +200 | 5.00 | 35 | 60 | **83.25** | **SHADOW** |
| `HIGH_LOSS_CV` | losses 10 vs 200 | +190 | 1.90 | 35 | 60 | **83.25** | **SHADOW** |
| `N4_EXPAND_MILD` | same mild + 4th win | +250 | 6.00 | 35 | 70 | **85.25** | **SHADOW** |
| `N10_SL3_MILD` | sl = 0.30 | +310 | 7.20 | 35 | 60 | **83.25** | **SHADOW** |
| `MILD_NOSL` | sl = 0 | +200 | 5.00 | 45 | 60 | **80.75** | WATCH |
| `MILD_AVG` | `WasAveragedDown` | +200 | 5.00 | 55 | 55 | **77.25** | WATCH |
| `MILD_ESC_1.51` | 0.10 → 0.151 | +200 | 5.00 | 50 | 70 | **81.50** | WATCH |
| `CASEB_SL` | 0.10 → 0.20 → 0.40 | +650 | 5.33 | 60 | 60 | **77.00** | WATCH |
| `CASEB_NOSL` | same, no SL | +650 | 5.33 | 70 | 50 | **72.50** | WATCH |
| `HIGH_LOT_CV` | 0.01 → 0.20 → 0.01 | +200 | 5.00 | 60 | 60 | **77.00** | WATCH |
| `N10_SL2_MILD` | sl = 0.20 | +310 | 7.20 | 45 | 60 | **80.75** | WATCH |
| `EURUSD_SPACER` | non-XAU stripped | +200 | 5.00 | 50 | 70 | **81.50** | WATCH |
| `INCOMPLETE_SPACER` | incomplete stripped | +200 | 5.00 | 50 | 70 | **81.50** | WATCH |
| `SAME_CLOSE_LOSS_THEN_BIG` | equal `ClosedAt`, loss first | +200 | 5.00 | 50 | 70 | **81.50** | WATCH |

`CASEB_SL` matches B35 §7.7 gold (`77.00` / `WATCH`). `FX03_LOSING` matches B35 §7.5 (`47.00` / `RISK_BLOCKED`).

### 3.2 Martingale = 1 but quality **< 70**

| id | why | qual | state |
|---|---|---:|---|
| `NET_ZERO_MILD` | `NET > 0` false | **55.25** | WATCH |
| `NET_NEG1_MILD` | `NET = −1`, DD>GP | **52.75** | RISK_BLOCKED |
| `FX03_LOSING` | all losses, 2×/2× | **47.00** | RISK_BLOCKED |
| `DD_GT_GP_UNPROFITABLE` | `NET = −30` | **52.75** | RISK_BLOCKED |
| `N2_MILD_WIN` | N<3 cap | **40.00** | INSUFFICIENT_DATA |
| `N2_MILD_LOSE` | N<3 cap + `NET<0` | **40.00** | RISK_BLOCKED |
| `STACK_ALL_STILL_Q70?` | avg+esc+no SL → risk 80 | **69.00** | RISK_BLOCKED |
| `STACK_ALL_FLAGS_WIN` | + lotCv | **64.50** | RISK_BLOCKED |

These are the **only** measured ways a martingale book stays under 70: not enough trades, not profitable, or three-plus stacked flags that also trip `risk >= 80`.

### 3.3 Control: martingale-shaped but **flag false** (still quality ≥ 70)

These are not “with martingale,” but they show the detector is bypassable while the leaderboard stays green.

| id | trick | mart | qual | state |
|---|---|---:|---:|---|
| `BOUND_1.25_EXACT_NOT_MART` | `0.10 → 0.125` (`>` fails) | 0 | 100.00 | SHADOW |
| `GEO_1.24x3` | 1.24× after each loss | 0 | 100.00 | SHADOW |
| `GEO_1.24x4` | four-step 1.24× | 0 | 95.00 | SHADOW |
| `SPACER_BE` | loss, **BE 0.10**, then 0.20 | 0 | 96.25 | SHADOW |
| `SPACER_TINY_WIN` | loss, **+0.01**, then 0.20 | 0 | 96.25 | SHADOW |
| `SIZEUP_AFTER_WIN` | 2×/2× after **wins** | 0 | 91.75 | SHADOW |
| `SAME_CLOSE_BIG_THEN_LOSS` | equal `ClosedAt`, **input order** flips the pair | 0 | 100.00 | SHADOW |

`EURUSD_SPACER` / `INCOMPLETE_SPACER` do **not** evade: the scorer drops non-XAU and incomplete rows, so the XAU loss and the XAU size-up become adjacent. Spacer must itself be a **completed XAU** trade with `pnl >= 0`.

---

## 4. Thresholds an adversary actually uses

### 4.1 To **score ≥ 70 with the flag on** (the assigned question)

Minimum recipe:

```text
N >= 3 completed XAU
one loss, then MaxVolumeLots > prev * 1.25          // flag on
ratio <= 1.50                                       // avoid +15 escalation
lot CV <= 0.4                                       // reset or stay near the new size
InitialSl > 0 on at least 50% of rows               // avoid both SL hits
WasAveragedDown = false
NET > 0                                             // even +1 is enough if PF stays > 1
```

That book is **`quality ∈ {70.25, 80.25, 85.25}`** and **`SHADOW`**.

Optional cosmetics that still stay ≥ 70:

- no SL at all (`80.75`, `WATCH` — risk 45)
- 2×/2× recovery (`77.00` / `72.50`, `WATCH`)
- averaging-down on a winner (`77.25`, `WATCH`)
- `sl_rate = 0.20` (`80.75`, `WATCH`)

### 4.2 To **score ≥ 70 and look like SHADOW without the flag** (evasion)

```text
size-up by exactly 1.25× after a loss
OR  size-up by 1.24× chained
OR  insert a completed XAU breakeven / +0.01 between the loss and the double
OR  double only after a win
OR  feed equal ClosedAt rows in the order that puts the large lot first
```

`OrderBy(ClosedAt)` is the only sort. LINQ `OrderBy` is stable, so **caller order** of equal timestamps is load-bearing. Persist / reconstruct order is therefore a silent martingale toggle.

### 4.3 What does **not** work

- Two-trade wonder: quality capped at 40.
- Finish red: `Martingale ∧ NET < 0` → `RISK_BLOCKED` (unit test only covers this).
- Hide a size-up behind EURUSD or an incomplete row: those are stripped; XAU pairs re-join.
- Count on `DD > GP` to punish a winner: impossible when `NET > 0`.

---

## 5. Persist / risk-engine / shadow — why the number matters

`DealIngestionService` writes `EarlyQualityScore` and `Martingale` and **`CurrentState = score.SuggestedState`** with no extra gate:

```85:100:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            // ...
            EarlyQualityScore = score.EarlyQualityScore,
            Martingale = score.Features.Martingale,
            CurrentState = score.SuggestedState,
```

Dashboard ranks by `EarlyQualityScore`. A 1.30× recovery book at **85.25** sits next to a flat three-winner book at **100.00**. The boolean `Martingale` is stored but **does not** clip the rank key.

`RiskEngine` (`BlockMartingale = true`) rejects **new increasing** copy when `MartingaleFlag` is passed in. That is a **FIX / copy** brake. It does **not** rewrite the score. It does **not** run on leaderboard ingest. A24 still starts **shadow** on `SHADOW` state. So:

```text
winning mild martingale
  → quality 85.25
  → state SHADOW
  → destination-quote simulation eligible
  → live FIX still blocked later IF someone threads MartingaleFlag
  → leaderboard already sold the book as high-quality
```

Dollars buy a **watch seat** (Case B) or a **shadow seat** (mild ratio). They do not buy `LIVE` today only because `FromBaseline` never emits `LIVE` (vacuous; C02).

---

## 6. Unit tests vs this hole

`D:\Prop\tests\Unit\BaselineScorerTests.cs` — three facts:

| Fact | Martingale? | Quality asserted? | What it actually locks |
|---|---|---|---|
| `Two_trades_remain_insufficient` | no | no | N=2 → `INSUFFICIENT_DATA` |
| `Three_disciplined_winners_go_to_shadow_not_live` | no | no | clean winners → `SHADOW` |
| `Martingale_after_losses_is_risk_blocked` | **losing** 2×/2× | **no** | `NET<0` clause, quality would be **47** |

Missing facts that would fail **today** if written against A22, or would document the stub hole if written against v0:

1. `PF_1.05_MILD` → `Martingale==true` and `EarlyQualityScore >= 70` and `SHADOW`.
2. `CASEB_SL` → `Martingale==true` and `EarlyQualityScore == 77.00` and `WATCH` (not `RISK_BLOCKED`).
3. `BOUND_1.25_EXACT_NOT_MART` → flag **false**.
4. `SPACER_BE` → flag **false**.
5. Property: `Martingale && NetPnl>0 && N>=3 && no extra flags ⇒ quality >= 70.25`.

C02 already named (2) as a product fail. This file pins (1) as the **quality** fail.

---

## 7. Contrast with A22 `baseline.v1` (spec, not code)

```text
FLAG_MARTINGALE  →  risk_score = Max(body, 80)
                 →  behavior_score = Min(body, 35)
                 →  severe_risk → R3 RISK_BLOCKED

raw = 0.45*behavior + 0.35*(100-risk) + 0.12*pf_score + 0.08*expectancy
    ≤ 0.45*35 + 0.35*20 + 0.12*100 + 0.08*100
    = 15.75 + 7 + 12 + 8
    = 42.75

early_quality = raw − U(N)
U(3) = 18  →  quality ≤ 24.75
U(∞) =  0  →  quality ≤ 42.75
```

**A22 answer to the same question: no, not at any N.**  
**Implemented answer: yes, from trade #3, up to 85.25.**

The gap is structural, not a rounding miss:

| Knob | Stub | A22 |
|---|---|---|
| Martingale contribution to quality | −30×0.20 − 35×0.25 = **−14.75** plus leftover 70-point behavior | floors/caps **dominate** the blend |
| `NET > 0` bonus | **+15** (buys the 70 line by itself vs the −14.75) | **forbidden** (I9) |
| `U(3)` | **0** | **18** |
| State given the flag | SHADOW if `risk<40` (35 is `< 40`) | `RISK_BLOCKED` (R3 severe) |
| Ratio | `> 1.25` | `>= 1.80` |

The +15 NET term is the load-bearing leak: it almost exactly cancels the martingale drag (`14.75`), and the PF steps push the book through 70 / 80 / 85.

---

## 8. Worked traces (hand + measured, same numbers)

### 8.1 Mild winning martingale → 85.25 SHADOW

```text
t1  0.10  −50   sl=yes
t2  0.126 +200  sl=yes     // 1.26× > 1.25, ≤ 1.50
t3  0.10  +50   sl=yes
```

```text
Martingale=true  LotEscalation=false  LotCv=0.1128  Sl=1  LossCv=0
NET=+200  GP=250  GL=50  PF=5  DD=50
risk=35  behavior=70
quality=50+15+10+5+14−8.75=85.25
85.25>=70 ∧ 35<40 → SHADOW
```

### 8.2 Barely green → 70.25 SHADOW (the 70-line)

```text
t1  0.10  −100  sl=yes
t2  0.13  +80   sl=yes
t3  0.10  +25   sl=yes
NET=+5  PF=1.05
quality=50+15+14−8.75=70.25 → SHADOW
```

One extra flag (escalation, no SL, averaging, lot CV) drops this row **below** 70. The PF=1.05 book is the **thinnest** passing martingale.

### 8.3 A22 Case B → 77.00 WATCH (quality still ≥ 70)

```text
t1  0.10  −50   sl=yes
t2  0.20  −100  sl=yes
t3  0.40  +800  sl=yes
```

```text
Martingale=true  Escalation=true  LotCv=0.5345  LossCv=0.3333  Sl=1
NET=+650  PF=5.3333  DD=150 < GP=800
risk=35+15+10=60  behavior=100−30−10=60
quality=50+15+10+5+12−15=77.00
risk>=80? no.  NET<0? no.  77>=70 ∧ 60<40? no.  77>=55 → WATCH
```

Without SL: risk 70, behavior 50, quality **72.50**, still `WATCH`, still `>= 70`.

### 8.4 Losing 2× (the only tested path) → 47.00 BLOCKED

```text
t1/t2/t3  0.10/−100, 0.20/−200, 0.40/−400
risk=60  behavior=60  NET<0
quality=50+12−15=47.00
Martingale ∧ DD>0 ∧ NET<0 → RISK_BLOCKED
```

Green tests here do not constrain §8.1–§8.3.

---

## 9. Honesty / do-not-claim

| Claim | Allowed? |
|---|---|
| `quality >= 70` is possible with `Martingale == true` | **Yes. Measured. Max 85.25. Min passing 70.25.** |
| That path is `RISK_BLOCKED` | **No** unless `NET < 0` or `risk >= 80`. |
| That path cannot be `SHADOW` | **False.** Mild ratio + SL is `SHADOW`. |
| A22 / `docs/scoring.md` “martingale ⇒ RISK_BLOCKED” is implemented | **Only for losing books.** |
| `baseline.v1` quality can be ≥ 70 with the flag | **No** (≤ 24.75 at N=3). |
| Unit tests cover the assigned question | **No.** |
| Product source was changed to close the hole | **No. Frozen.** |
| LIVE promotion | **Still impossible** (vacuous; not this ticket). |

**C32 answer, without greenwash:**

```text
YES — BaselineScorer.EarlyQualityScore can be >= 70 with Features.Martingale == true.
Typical winning mild martingale: 85.25 SHADOW.
Typical winning 2× martingale:   77.00 WATCH.
Losing martingale:               <= 55.25 and RISK_BLOCKED.
N<3 martingale:                  40.00, state INSUFFICIENT_DATA or RISK_BLOCKED.
A22 would have said NO. The stub says YES because NET is a +15 quality term
and martingale is only +35 risk / −30 behavior, not a floor/cap.
```

*End of C32. Product source untouched.*
