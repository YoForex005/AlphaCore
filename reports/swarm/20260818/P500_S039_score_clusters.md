# P500_S039 — SHADOW scores cluster 85–96; coarse buckets cannot rank 70 names

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | P500_S039 |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S039_score_clusters.md` |
| Agent | P500_S039 (read-only score-lattice / ranking) |
| Product source modified | **No** |
| Live `35=D` sent | **No** |

**Honesty:** this is algebra of the **running stub** (`BaselineScorer.Score`) plus the mid-scoring census in `P500_PROFIT_SYNTHESIS.md` (`SHADOW = 70`). It is **not** a live histogram of 70 distinct quality values. Destination P&L is **$0**. `CanPromoteToLive` stays **false**.

---

## 0. Verdict (one paragraph)

`EarlyQualityScore` is **not** a continuous skill rank. It is a **small integer lattice**:

```text
quality = 50
        + 15·I(XAU NetPnl > 0)
        + 10·I(PF ≥ 1.2)
        +  5·I(PF ≥ 1.8)
        + 0.20 · behavior
        − 0.25 · risk
SHADOW  ⇐  N ≥ 3  ∧  quality ≥ 70  ∧  risk < 40
          ∧  ¬(risk ≥ 80 ∨ (Martingale ∧ NetPnl < 0))
```

`behavior` and `risk` are themselves **boolean stacks** (steps of 10 / 15 / 20 / 30 / 35), not lerp scores. On the production ingest path `InitialSl` is always missing, so **every** book takes `risk += 10` and `behavior -= 10` (P500_S030). That pins the clean profitable cell at **exactly 95.50**, not 100, and makes `NET ≤ 0` books **WATCH** (65.50) instead of SHADOW (70). The SHADOW band that remains is almost entirely **NET>0 + PF bonuses ± a few flags**, which lands in **85–96**. Sorting 70 SHADOW names by `earlyScore` therefore sorts **which discrete flags fired**, not copy expectancy. Coarse buckets (`SHADOW`, `≥90`, `95.50`) collapse those 70 names into a handful of cells. Ranking for capital needs **expectancy after destination costs**, **hold-time**, **size stability**, and **chronological OOS** — none of which enter quality today.

---

## 1. What actually runs (stub, not A22)

Source: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` lines 129–160, 189–206.

### 1.1 Risk (higher = worse; additive; cap 100)

| Flag | Δ risk | Continuous? |
|---|---:|---|
| `Martingale` | +35 | no |
| `AveragingDown` | +20 | no |
| `LotEscalation` | +15 | no |
| `LotCv > 0.5` | +10 | threshold only |
| `SlUseRate < 0.3` | +10 | threshold only |
| `MaxDrawdown > GrossProfit > 0` | +10 | threshold only |

### 1.2 Behavior (higher = better; start 100; clamp [0,100])

| Flag | Δ behavior | Continuous? |
|---|---:|---|
| `Martingale` | −30 | no |
| `AveragingDown` | −15 | no |
| `LotCv > 0.4` | −10 | threshold only (note: 0.4, not 0.5) |
| `SlUseRate < 0.5` | −10 | threshold only |
| `LossSizeCv > 0.8` | −10 | threshold only |

### 1.3 Quality (the leaderboard key)

```text
quality0 = 50 + 15·I(NET>0) + 10·I(PF≥1.2) + 5·I(PF≥1.8)
         + 0.20·behavior − 0.25·risk
if N < 3: quality0 = min(quality0, 40)
EarlyQualityScore = Clamp(Round2(quality0), 0, 100)
```

`decimal.Round(..., 2)` is **ToEven**. There is **no** A22 `U(N)` sample-uncertainty penalty. At `N=3` spec would cap a perfect book at `100 − 18 = 82`. The stub prints **95.50**.

### 1.4 Features computed and then ignored by `Score()`

| Feature | Computed? | Enters quality / state? |
|---|---|---|
| `AverageHoldSeconds` | yes (mean hold of completed XAU) | **no** |
| `TradeFrequencyPerDay` | yes | **no** |
| `LotCv` magnitude | yes (population CV) | only as `>0.4` / `>0.5` bits |
| `LossSizeCv` magnitude | yes | only as `>0.8` bit |
| `AverageMfe` / `AverageMae` | always `Unavailable` / null | **no** |
| expectancy after costs | **not computed** | — |
| hold CV / median hold | **not computed** | — |
| OOS / walk-forward split | **not computed** | — |

Dashboard sort is `OrderByDescending(EarlyScore)` (`EfDashboardQueries.GetTradersAsync`). That is the only rank.

---

## 2. Why the mass sits in 85–96

### 2.1 Production SL hole fixes two addends for everyone

P500_S030: `Mt5DealDto` has no SL; `ReadDeals` never calls `CIMTDeal.PriceSL()`; `LoadDealsAsync` never sets `NormalizedDeal.StopLoss`. Therefore:

```text
SlUseRate = 0    for every store-scored book
risk     += 10   (always)
behavior −= 10   (always)
```

Clean profitable tape:

| | Tests (`InitialSl=2290`) | Prod / demo / live ingest |
|---|---:|---:|
| risk | 0 | **10** |
| behavior | 100 | **90** |
| quality | 50+15+10+5+20−0 = **100** | 50+15+10+5+18−2.5 = **95.50** |

**95.50 is the production ceiling** for a no-flag, NET>0, PF≥1.8 book. Nothing in the SHADOW set can print 96–100 unless SL ingest is wired. Unit tests hide this by hardcoding SL.

### 2.2 SHADOW gate + SL hole almost require NET>0

Without the +15 (XAU net ≤ 0), PF bonuses cannot fire (`PF ≤ 1`). Production clean loser:

```text
quality = 50 + 0.20·90 − 0.25·10 = 65.50  →  WATCH  (need ≥70)
```

Tests with SL present: `50 + 20 = 70.00` → still SHADOW. That path **does not exist on the ingest book**. So the 70 SHADOW names are, by arithmetic, almost all **XAU NET>0**. That is a **sign bit**, not a dollar rank: +$0.01 and +$41,634 get the same +15 (see 303310 in synthesis).

### 2.3 Martingale cannot sit in the 85–96 SHADOW cluster on production

Martingale + missing SL:

```text
risk = 35 + 10 = 45  ≥  40  →  not SHADOW
```

C32’s **85.25 SHADOW** cell is martingale-only **with SL on** (`risk=35`, `behavior=70`, `50+15+10+5+14−8.75=85.25`). Live ingest never has SL, so those books fall to **WATCH** (or `RISK_BLOCKED` if also NET<0). The 70-name SHADOW set is therefore the **non-martingale** (or at least `risk<40`) profitable-XAU slice — which is exactly the lattice in §3.

### 2.4 PF is two bits, not a curve

| PF | Addend | Typical quality (SL missing, no other flags, NET>0) |
|---|---:|---:|
| `< 1.2` | 0 | **80.50** (SHADOW, below the 85–96 pile) |
| `[1.2, 1.8)` | +10 | **90.50** |
| `≥ 1.8` or no losses (`PF` stub **99**) | +15 | **95.50** |

A 1.21 PF and a 50 PF are identical. A 1.79 PF and a 1.19 PF differ by a full **10** points. That is why the histogram piles on **90.50** and **95.50**, not a smear from 85 to 96.

---

## 3. Discrete SHADOW lattice (production: SL always missing)

Let `R0 = 10`, `B0 = 90` (the SL pair). Extra flags stack. `SHADOW` requires `risk < 40`.

### 3.1 NET>0 and PF≥1.8 (the 85–96 pile)

| Extra flags | risk | behavior | quality | Notes |
|---|---:|---:|---:|---|
| none | 10 | 90 | **95.50** | demo 10001 / 99001; live 302252 / 303174 shape |
| `LossSizeCv>0.8` only | 10 | 80 | **93.50** | |
| `0.4 < LotCv ≤ 0.5` | 10 | 80 | **93.50** | risk bit does **not** fire |
| `DD > GP` | 20 | 90 | **93.00** | DD is risk-only |
| `LotEscalation` | 25 | 90 | **91.75** | C32 `SIZEUP_AFTER_WIN`; 303310 hides here |
| `LotCv>0.5` | 20 | 80 | **91.00** | |
| `LossCv` + `DD` | 20 | 80 | **91.00** | |
| `Escal` + `LossCv` | 25 | 80 | **89.75** | |
| `Escal` + `DD` | 35 | 90 | **89.25** | |
| `LotCv>0.5` + `DD` | 30 | 80 | **88.50** | |
| `AveragingDown` | 30 | 75 | **87.50** | still SHADOW (`30 < 40`) |
| `Escal` + `LotCv>0.5` | 35 | 80 | **87.25** | |
| `Avg` + `LossCv` | 30 | 65 | **85.50** | floor of this PF band |
| `Avg` + `DD` | 40 | 75 | 85.00 | **not SHADOW** (`risk < 40` fails) |
| `Avg` + `LotCv>0.5` | 40 | 65 | 83.00 | **not SHADOW** |
| `Martingale` | 45 | 60 | 79.25 | **not SHADOW** (prod SL) |

### 3.2 NET>0 and 1.2 ≤ PF < 1.8 (subtract 5 from the table)

| Extra flags | quality |
|---|---:|
| none | **90.50** |
| LossCv or mid LotCv | **88.50** |
| DD | **88.00** |
| Escalation | **86.75** |
| LotCv>0.5 | **86.00** |
| AveragingDown | **82.50** |

### 3.3 How many distinct ranks exist?

Even granting every legal combo in §3.1–3.2, SHADOW quality takes on the order of **~20 discrete values**, most of them **2.00 or 2.50 apart**. Seventy names **must** collide. The modal cells expected on a demo-challenge XAU book (flat-ish lots, PF often stubbed at 99 because few/no XAU losses, SL missing) are:

```text
95.50   clean NET>0 PF≥1.8
93.50   messy loss sizes or mild lot CV
93.00   DD > GP
91.75   lot escalation after wins (not martingale)
91.00   lot CV > 0.5
90.50   NET>0 but PF in [1.2, 1.8)
```

That is the 85–96 cluster. It is **not** 70 shades of skill.

---

## 4. Why coarse buckets cannot rank 70 SHADOW names

Census at synthesis time (`P500_PROFIT_SYNTHESIS.md`): **70 SHADOW**, 79 WATCH, 29 RISK_BLOCKED, 0 LIVE. Groups: **100% demo** (`demo\yo-2step` + `demo\yo-payp`). Starwave scored **0**.

| Coarse key | What it actually partitions | What it cannot tell apart |
|---|---|---|
| `state == SHADOW` | `N≥3`, `q≥70`, `r<40`, not blocked | 70 names, including +$41k / 2.0 lot (303310) and −$68 all-symbol (302252) |
| `earlyScore ≥ 90` | “few extra flags” + usually PF≥1.2 | 163s gold scalps (322947) vs 30-minute swings |
| `earlyScore == 95.50` | SL missing + NET>0 + PF≥1.8 + no other bits | N=3 luck vs N=194; $1 vs $10k; one gold bet copied 40 times |
| `earlyScore` sort desc | flag count / which bit | expectancy, hold, size path, dest cost, OOS |
| `netSourcePnl` (dashboard) | Σ **all symbols** | XAU-only book the scorer used; 302252 is SHADOW 95.50 at **−68.46** dashboard |
| `behavior` 90 / `risk` 10 | the SL-ignorance pair | every clean book looks the same |

Concrete collisions already named in synthesis:

| Login | Group | XAU N | Dashboard PnL | earlyScore | Why 95.50 / 91.75 is the wrong rank |
|---|---|---:|---:|---:|---|
| 303310 | demo\yo-2step | 22 | +41,634 | ~91.75 (escalation) | max **2.0** lots; mixed FX/BTC/XAU; one ticket +13,692. Source $ is challenge-pass, not dest EV |
| 322947 | demo\yo-payp | 194 | +4,950 | high SHADOW | avg hold **~163 s** — dies in spread + 15s `MaxSourceSignalAge` |
| 303274 | demo\yo-2step | 102 | +1,228 | SHADOW | same-second 0.05 grid; first 3 XAU **−0.35, −55.30, +25.90**; scorer missed averaging |
| 302252 | demo\yo-2step | 11 | **−68.46** | **95.50** | XAU subset won; dashboard is all-symbol |
| 303174 | demo\yo-2step | — | **−29.38** | **95.50** | same shape |

Seventy names that share five quality cells are **one correlated gold bet** if copied together (synthesis §3.D.7). A coarse `SHADOW` bucket will fire them as a cluster.

A22 already said this: *“rank by NET”* and *“rank by win_rate”* are rejected; quality was supposed to be a **lerp blend** (`0.45·behavior + 0.35·(100−risk) + 0.12·pf + 0.08·expectancy` minus `U(N)`). The stub replaced that with **three sign/threshold bits + 0.2/0.25 of other bits**. Spec would still cluster at N=3 (cap 82), but it would **spread** as N grows. The stub does not: a 194-trade scalp and a 3-trade winner can share **95.50**.

---

## 5. What would actually rank (not implemented)

Do not promote on `earlyScore`. If a later ranker exists, it needs four axes the stub does not compute.

### 5.1 Expectancy **after destination costs**

Source `NetRealizedPnl` is MT5 `Profit+Commission+Swap` on the **challenge** book. Pepperstone gold spread + commission + copy delay is **worse**, especially sub-3-minute holds.

Need, per login, on **XAU only**, after a modeled dest haircut (spread + slip + commission, not source swap):

```text
E[R] = mean( win_rate · avg_win − loss_rate · avg_loss )   after cost
```

A22 defined `expectancy_R` / `expectancy_Rpx` and put 8–10% of the blend on it. Grep of `src/Domain/Scoring` for `expectancy` / `commission` is **empty**. PF≥1.8 on a 3-trade tape with one tiny loss is **not** after-cost EV.

Gate (research, not product change): drop the name if dest-haircut expectancy ≤ 0, or if XAU NET ≤ 0 after the haircut.

### 5.2 Hold-time

`AverageHoldSeconds` is already on `FeatureSnapshot` and **never read** by `Score()` or `FromBaseline`. Synthesis: 322947 ~163s, 303274 same-second grid. Copy hop + 15s stale-signal guard + dest spread eats that edge.

Need **median** (not mean) hold, hold CV, and a hard floor (synthesis used **≥ 15 minutes** for shadow eligibility). Mean of {2s, 2s, 3600s} is a lie.

### 5.3 Size stability

Today: three booleans (`Martingale`, `LotEscalation`, `AveragingDown`) plus `LotCv` cut at 0.4/0.5.

Holes already measured:

- Martingale = *next ticket after a loss is >1.25×*. Parallel same-second tickets do **not** count (303274).
- Escalation = *any* next ticket >1.5×, including after **wins** → 91.75 SHADOW (C32 `SIZEUP_AFTER_WIN`, 303310).
- Averaging = `WasAveragedDown` on **same position** scale-in only.
- `LotCv` is population stdev; A22 wanted sample stdev + a lerp, not a cliff.

Need: max lots, last/first lot ratio, intra-bar multi-ticket count, dest qty after `allocationFactor` (cap **0.05** XAU in synthesis), and reject if dest min lot is unreachable or source max is a blow-up.

### 5.4 Out-of-sample (chronological)

Trade #3 is eligibility, not a track record. A22 `U(3)=18` was the honesty penalty; the stub omitted it.

Need a **time split** on the same login (first 70% / last 30% by `ClosedAt`), plus a **destination** shadow window (30+ days on real bid/ask — tape is null today). Promote only if **OOS dest** expectancy after costs stays > 0. Demo/contest groups stay research-only until funded-and-still-green (P500_S004).

ML is Phase 6 and must beat this baseline OOS. There is no baseline OOS number yet. Do not invent one.

---

## 6. Spec vs stub (why the cluster is worse than A22)

| A22 `baseline.v1` | Running stub | Effect on 70-name rank |
|---|---|---|
| lerp PF, expectancy, hold CV, lot CV | 3 bits + 5 flag bits | ~20 atoms instead of a curve |
| `U(N=3)=18` → cap 82 | no U(N); 95.50 legal | first-3 looks “proven” |
| NET sign **forbidden** in quality | `+15` if NET>0 | SHADOW ≈ “XAU won once” |
| risk floors (martingale ≥80) | additive; winning MG can be 35 | C32 85.25 SHADOW if SL present |
| MFE/MAE optional | always Unavailable | no capture / overrun |
| official rank after expanding window | same formula at N=3 and N=194 | 3-trade luck ties 194-trade scalp |

`docs/scoring.md` is three lines: trade #3 → early eligible; high quality + low risk → **SHADOW never LIVE**; martingale → RISK_BLOCKED. That state law is intact (`CanPromoteToLive => false`). The **rank inside SHADOW** is the hole.

---

## 7. What not to do with the cluster

- Do **not** treat `95.50` as skill or as dest PnL (P500_S001 / CODE_32).
- Do **not** copy all 70 SHADOW names — they are one gold direction on demo challenge accounts.
- Do **not** break ties with dashboard `netSourcePnl` (wrong universe).
- Do **not** flip `REAL_COPY` because 70 names are “high quality.”
- Do **not** hand-edit `BaselineScorer` in this slot. Product was not modified.

If a later increment adds a ranker, put it **beside** the stub (new versioned score / shadow-eligibility view), keep SHADOW as the safety state, and require the four axes in §5. Until then the honest leaderboard caption is: **“discrete flag hash, 85–96, not a ranking.”**

---

## 8. File map

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | discrete +15+10+5+0.2B−0.25R; hold unused |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | `OrderByDescending(EarlyScore)`; all-symbol PnL |
| `D:\Prop\docs\scoring.md` | SHADOW never LIVE; no lattice |
| `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md` | lerp / expectancy / U(N) **not implemented** |
| `D:\Prop\reports\swarm\20260818\P500_S030_sl_rate.md` | why 95.50 is the SL-ignorance ceiling |
| `D:\Prop\reports\swarm\20260818\P500_S001_scorer_vs_negative_pnl.md` | 95.50 + red dashboard PnL |
| `D:\Prop\reports\swarm\20260818\P500_S004_demo_adverse_selection.md` | 70 names are demo challenge |
| `D:\Prop\reports\swarm\20260818\C32_score_adversarial.md` | 85.25 MG / 91.75 size-up cells |
| `D:\Prop\reports\swarm\20260818\P500_PROFIT_SYNTHESIS.md` | census 70 SHADOW; hold unused |

---

## 9. Honest metrics

| Claim | Status |
|---|---|
| Quality is `50+15+10+5+0.2·B−0.25·R` | **true** (stub) |
| Production SHADOW mass in 85–96 | **true by algebra** + named live cells 95.50 / 91.75; full 70-way histogram **not** re-pulled this slot |
| 95.50 = SL missing + NET>0 + PF≥1.8 + no other flags | **true** |
| 70 distinct earlyScores | **false** — lattice has ~20 SHADOW atoms |
| `earlyScore` ranks copy EV | **false** |
| Hold-time / dest-cost expectancy / OOS used in score | **false** |
| A22 lerp implemented | **false** |
| SHADOW ⇒ safe to send live | **false** (`CanPromoteToLive` hard-false; dest PnL $0) |

No product files were modified.
