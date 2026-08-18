# A22 — Deterministic Baseline Scoring Specification

**Document:** `D:\Prop\reports\swarm\20260818\A22_scoring_spec.md`  
**Source of law:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§15, 18–23, plus binding context from §§1, 3, 14, 17, 39, 45, 50, 60, 69, 72.  
**Score family:** `baseline.v1`  
**Date:** 2026-08-18  
**Status:** Implementation spec. Not product source. Not a trained model.

This document locks the **deterministic statistical baseline** that Phase 3 must implement and that later ML must beat out-of-sample. It specifies exact formulas for `risk_score`, `behavior_score`, `early_quality_score`, and the trader-state machine.

No machine learning is used. Weights are named constants in `ScoreConfig`, not learned parameters. Trade #3 is the first official score and is **never** a live-capital promotion.

---

## 0. Binding invariants

These are not tunable. Tests must fail the build if any is violated.

| ID | Invariant |
|---|---|
| I1 | Count only **completed reconstructed XAUUSD position lifecycles**. Orders, deal fills, partial closes, SL/TP modifications are not trades. (§15) |
| I2 | Trade #3 closure emits `EARLY_SCORE_ELIGIBLE`. It does **not** emit `PROVEN_PROFITABLE`. (§15) |
| I3 | Trade #3 is the **first official score**, then rescore after 4, 5, 6, … (§22) |
| I4 | At `completed_xau_n == 3`, legal states are only: `INSUFFICIENT_DATA` (must not remain), `EARLY_SCORE`, `WATCH`, `SHADOW`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED`. **Forbidden:** `LIVE`, `LIVE_CANDIDATE`. (§22–23) |
| I5 | Trade #3 + high score → **SHADOW only**. Never automatic real capital. (§1.4, §23) |
| I6 | Scoring as-of timestamp `T` may use only information available at `T`. No trade #n+1, no future balance, no challenge pass/fail, no future drawdown, no future ticks. (§20 applied to the baseline) |
| I7 | Do not fabricate MFE/MAE from closed deals. Use them in scores only when `feature_quality == EXACT`. (§17) |
| I8 | The three scores are **source-behavior** scores. Shadow/live destination P&L is used only in the **state gate**, never inside the three formulas. This keeps all ~5,000 accounts comparable. |
| I9 | `early_quality_score` must **not** rank traders by raw net P&L. The product target is future copyable profitability inside risk limits, not “who made the most in the first 3 trades.” (§3) |
| I10 | Scoring/ML produce candidate/confidence only. The risk engine is the final authority on approve/reduce/reject/pause. (§39) |
| I11 | Same `(trades[1..n], ScoreConfig, as_of)` → bit-identical scores. Pure function. No `DateTime.UtcNow` inside the calculator. |
| I12 | Numeric thresholds below are **v1 provisional defaults**. Formula *structure* is locked for `baseline.v1`. Constants live in versioned config so backtests can retune without rewriting code. Do not claim they are optimal. (§23) |

---

## 1. Purpose and non-goals

### 1.1 Purpose

Before XGBoost, produce a rules/statistics baseline that:

1. Computes features from reconstructed XAUUSD trades only.
2. Emits `risk_score`, `behavior_score`, `early_quality_score` on every eligible rescore.
3. Assigns a unique trader state from the §22 vocabulary.
4. Persists a frozen Trade-#3 snapshot for later ML comparison (same formulas, window `FIRST3`).
5. Becomes the benchmark that any future model must beat on chronological out-of-sample top-N future copy P&L / drawdown / CVaR. (§21)

### 1.2 Non-goals

- No XGBoost, logistic regression, neural net, clustering, or “auto-tuned” weights.
- No ranking by source dollar P&L.
- No live FIX sizing, copy intent, or execution in this spec.
- No silent substitution of destination cTrader quotes for source MT5 ticks.
- Do not modify product source in the same change-set as this document.

---

## 2. Universe, identity, as-of

### 2.1 Eligible trade

A row in `reconstructed_trades` is eligible iff **all** of:

```text
completed        == true
canonical_symbol == XAUUSD
closed_at        IS NOT NULL
closed_volume    >  0
```

Ignore: open positions, non-XAU symbols, incomplete reconstructions.

### 2.2 Stable order (trade number)

```text
ORDER BY closed_at ASC, opened_at ASC, id ASC
```

`n` is the 1-based index in that list for one `(broker_id, login)`.

**Trade #3** = the third eligible row. Its `closed_at` is the first official score timestamp.

### 2.3 Scoring windows

| Window id | Trades used | When written | Use |
|---|---|---|---|
| `EXPANDING` | `[1..n]` | After every completed eligible trade with `n >= 3` | Operational scores and state |
| `FIRST3` | `[1..3]` | Once, when trade #3 closes | Frozen research snapshot; later ML feature analog |
| `PROVISIONAL` | `[1..n]` | Optional, `n < 3` | Diagnostics only; **not** ranked; state stays `INSUFFICIENT_DATA` |

Both `EXPANDING` and `FIRST3` use the **same** formulas. Only `n` differs.

### 2.4 As-of payload

```text
ScoreAsOf
  broker_id
  login
  n                         // completed eligible trades at T
  as_of                     // closed_at of trade n
  score_version             // "baseline.v1"
  window                    // EXPANDING | FIRST3 | PROVISIONAL
  price_source              // from feature metadata
  feature_quality           // EXACT | APPROXIMATE | UNAVAILABLE
  mfe_mae_used              // true only if quality == EXACT and values present
```

The calculator receives the trade list **already filtered to as-of `T`**. Callers must not pass later trades.

### 2.5 Money and rounding

- P&L, prices, volumes: `decimal`.
- Intermediate ratios: `decimal` (or IEEE-754 `double` only inside `StdevSample` / `Log` helpers, then immediately quantized).
- Stored scores: `Round2(x) = decimal.Round(x, 2, MidpointRounding.AwayFromZero)`.
- Clamp: `Clamp(x, lo, hi) = Min(hi, Max(lo, x))`.
- Division by zero: every formula below names the defined fallback. No NaN/Inf may leave the calculator.

### 2.6 Sample standard deviation

For `m >= 2` values `x_i`:

```text
mean = (1/m) * Σ x_i
var  = (1/(m-1)) * Σ (x_i - mean)^2
sd   = Sqrt(var)
```

For `m < 2`: `sd = 0`, coefficient of variation treated as `0` (defined, not missing).

---

## 3. Feature catalog (deterministic)

All features are computed on the window trades `t_1 … t_n` in stable order.

### 3.1 Per-trade fields consumed

From `ReconstructedTrade` (§14) plus feature metadata (§17):

```text
id, broker_id, login, position_id
direction                          // Buy | Sell
opened_at, closed_at
entry_vwap, exit_vwap
initial_volume, max_volume, closed_volume
gross_realized_pnl, commission, swap, fees, net_realized_pnl
initial_sl, initial_tp, final_sl, final_tp
was_scaled_in, was_partial_close, was_averaged_down
completed
```

Optional, **only if** `feature_quality == EXACT` and both values present:

```text
mfe            // maximum favorable excursion in price units, source ticks
mae            // maximum adverse excursion in price units, source ticks
price_source
feature_quality
```

If `feature_quality != EXACT` or either value is null: set `mfe_mae_used = false` and **drop** every MFE/MAE term from all three scores. Do not approximate from `entry_vwap`/`exit_vwap`.

### 3.2 Per-trade derived

```text
pnl_i        = net_realized_pnl_i
vol_i        = max_volume_i                          // captures scale-in
hold_sec_i   = Max(0, (closed_at_i - opened_at_i).TotalSeconds)
is_win_i     = pnl_i >  0
is_loss_i    = pnl_i <  0
is_be_i      = pnl_i == 0
had_init_sl  = initial_sl_i IS NOT NULL
```

`R`-multiple, only when `initial_sl` and `entry_vwap` exist and contract size `C > 0`:

```text
risk_unit_i  = |entry_vwap_i - initial_sl_i|
if risk_unit_i > 0:
    r_i = pnl_i / (risk_unit_i * closed_volume_i * C)
else:
    r_i = undefined
```

If `C` is unknown, all `r_i` are undefined. Do not invent contract size.

### 3.3 Window aggregates

```text
N            = n
GP           = Σ Max(pnl_i, 0)
GL           = Σ Max(-pnl_i, 0)
NET          = Σ pnl_i
wins         = count(is_win_i)
losses       = count(is_loss_i)
be           = count(is_be_i)
win_rate     = wins / N
loss_rate    = losses / N

if GL > 0:           pf = Min(GP / GL, PF_CAP)
elif GP > 0:         pf = PF_CAP
else:                pf = 1

mean_vol     = Mean(vol_i)
lot_cv       = (mean_vol > 0) ? StdevSample(vol_i) / mean_vol : 0

loss_abs     = { |pnl_i| : is_loss_i }
if losses >= 2:  loss_cv = StdevSample(loss_abs) / Mean(loss_abs)
elif losses == 1: loss_cv = 0
else:            loss_cv = 0          // no losses → this component is not a defect

avg_win      = (wins   > 0) ? Mean(positive pnl_i) : 0
avg_loss     = (losses > 0) ? Mean(|negative pnl_i|) : 0
if avg_loss > 0:
    payoff          = avg_win / avg_loss
    expectancy_Rpx  = (win_rate * avg_win - loss_rate * avg_loss) / avg_loss
else:
    payoff          = (avg_win > 0) ? PAYOFF_CAP : 0
    expectancy_Rpx  = (NET > 0) ? 1 : 0

sl_rate      = count(had_init_sl) / N
avg_down_n   = count(was_averaged_down_i)
scale_in_n   = count(was_scaled_in_i)

hold_cv      = (Mean(hold_sec_i) > 0) ? StdevSample(hold_sec_i) / Mean(hold_sec_i) : 0
```

`R`-window (if at least `R_MIN_DEFINED` trades have defined `r_i`, default 2):

```text
expectancy_R = Mean(defined r_i)
```

Otherwise `expectancy_R` is missing and `expectancy_Rpx` is used.

### 3.4 Equity and drawdown

```text
eq[0]    = 0
eq[k]    = eq[k-1] + pnl_k                 // k = 1..N
peak[k]  = Max(eq[0], …, eq[k])
dd[k]    = peak[k] - eq[k]                 // ≥ 0
max_dd   = Max(dd[k])                      // absolute currency

scale    = Max(Σ |pnl_i|, Max(peak[k]), DD_SCALE_EPS)
max_dd_frac = max_dd / scale               // in [0, 1] typically
```

`DD_SCALE_EPS` (default `0.01` account currency) prevents division by zero on a flat book.

This is **completed-trade equity**, not mark-to-market. That is intentional at v1: it is available for every account without ticks.

### 3.5 Martingale, escalation, revenge

For each adjacent pair `(i, i+1)`, `i = 1..N-1`, if `vol_i > 0`:

```text
ratio_i = vol_{i+1} / vol_i

martingale_hit_i =
    (pnl_i < 0) AND (ratio_i >= MARTINGALE_VOLUME_RATIO)

revenge_hit_i =
    (pnl_i < 0) AND (ratio_i >= REVENGE_VOLUME_RATIO)

escalation_hit_i =
    ratio_i >= ESCALATION_VOLUME_RATIO
```

```text
martingale_events = count(martingale_hit_i)
revenge_events    = count(revenge_hit_i)
escalation_events = count(escalation_hit_i)
vol_span          = (Min(vol_i) > 0) ? Max(vol_i) / Min(vol_i) : 1
```

Defaults:

```text
MARTINGALE_VOLUME_RATIO = 1.80
REVENGE_VOLUME_RATIO    = 1.40
ESCALATION_VOLUME_RATIO = 2.00
```

### 3.6 Frequency and session

```text
span_sec = Max(1, (closed_at_N - opened_at_1).TotalSeconds)
trades_per_day = N / (span_sec / 86400)

burst_flag =
    (N >= 3) AND
    (closed_at_N - opened_at_1).TotalSeconds < BURST_WINDOW_SEC
```

Default `BURST_WINDOW_SEC = 300` (three completed XAU lifecycles inside five minutes is treated as a risk burst, not as skill).

Session of `opened_at` in **UTC**:

```text
Asia          hour in [00, 07)
London        hour in [07, 12)
NY_overlap    hour in [12, 17)
Late          hour in [17, 24)
```

```text
session_max_frac = max share of the four buckets
```

High concentration is **not** a defect. High entropy at `N = 3` is also not a defect. Session is a **stability** term only (one dominant session scores higher than a 1/1/1 split), and is the first term dropped when `mfe_mae_used`.

### 3.7 Optional EXACT MFE/MAE features

Used only when `mfe_mae_used == true`.

Direction-aware price units: already stored as positive magnitudes.

```text
if risk_unit_i > 0:
    mae_R_i = mae_i / risk_unit_i
    mfe_R_i = mfe_i / risk_unit_i
else:
    undefined

mae_overrun_i  = (had_init_sl AND mae_i > risk_unit_i * MAE_OVER_SL)   // default 1.10
mfe_capture_i  = realized_favorable_i / mfe_i     // 0 if mfe_i == 0

realized_favorable_i =
    Buy  ? Max(0, exit_vwap_i - entry_vwap_i)
    Sell ? Max(0, entry_vwap_i - exit_vwap_i)
```

```text
mae_overrun_rate = count(mae_overrun_i) / N
mean_mfe_capture = Mean(defined mfe_capture_i)     // Clamp to [0, 1]
mean_mae_R       = Mean(defined mae_R_i)
```

---

## 4. Shared maps

Piecewise-linear score maps. Outside the first/last knot, clamp.

```text
LerpScore(x, knots[]) :
    knots are strictly increasing in x
    if x <= x0: return s0
    if x >= xk: return sk
    find i such that x_i <= x <= x_{i+1}
    t = (x - x_i) / (x_{i+1} - x_i)
    return s_i + t * (s_{i+1} - s_i)
```

All component scores are in `[0, 100]`.

### 4.1 Risk components (higher = more risk)

```text
martingale_risk =
    0 events → 0
    1 event  → 70
    2 events → 90
    ≥3       → 100
    plus: if vol_span >= 4 and martingale_events >= 1 → max(…, 95)

averaging_down_risk =
    LerpScore(avg_down_n / N,  (0,0), (0.34,55), (0.67,85), (1.00,100))

lot_escalation_risk =
    max(
      LerpScore(vol_span,           (1.00,0), (2.00,40), (3.00,75), (5.00,100)),
      LerpScore(escalation_events,  (0,0),    (1,50),    (2,80),    (3,100))
    )

loss_size_risk =
    LerpScore(loss_cv, (0.00,0), (0.40,30), (0.80,65), (1.50,100))
    if losses == 0: 0

drawdown_risk =
    LerpScore(max_dd_frac, (0.00,0), (0.15,35), (0.30,65), (0.50,90), (0.80,100))

no_sl_risk =
    LerpScore(1 - sl_rate, (0.00,0), (0.50,40), (1.00,85))
    if N >= 3 and sl_rate == 0: max(…, 70)

revenge_risk =
    LerpScore(revenge_events, (0,0), (1,55), (2,85), (3,100))

frequency_risk =
    max(
      burst_flag ? 80 : 0,
      LerpScore(trades_per_day, (0,0), (6,10), (20,50), (50,85), (100,100))
    )

mae_overrun_risk =                          // only if mfe_mae_used
    LerpScore(mae_overrun_rate, (0.00,0), (0.34,60), (0.67,85), (1.00,100))
```

### 4.2 Behavior components (higher = better process)

```text
lot_consistency_score =
    LerpScore(lot_cv, (0.00,100), (0.15,80), (0.50,40), (1.00,10), (1.50,0))

loss_size_consistency_score =
    if losses == 0: 80          // not 100: untested loss process
    else LerpScore(loss_cv, (0.00,100), (0.30,80), (0.70,40), (1.20,10), (1.80,0))

sl_discipline_score =
    LerpScore(sl_rate, (0.00,0), (0.50,45), (1.00,100))

no_martingale_score =
    0 events → 100
    1 event  → 25
    ≥2       → 0

no_averaging_score =
    LerpScore(avg_down_n / N, (0.00,100), (0.34,40), (0.67,15), (1.00,0))

hold_stability_score =
    LerpScore(hold_cv, (0.00,100), (0.50,80), (1.00,50), (2.00,20), (3.00,0))
    if Max(hold_sec_i) < 5: min(…, 30)     // sub-5s lifecycles: noise / fat-finger

expectancy_process_score =
    x = expectancy_R if defined else expectancy_Rpx
    LerpScore(x, (-1.00,0), (0.00,40), (0.30,70), (0.80,90), (1.50,100))

session_stability_score =
    LerpScore(session_max_frac, (0.25,40), (0.50,70), (0.75,90), (1.00,100))

mfe_capture_score =                         // only if mfe_mae_used
    LerpScore(mean_mfe_capture, (0.00,10), (0.30,40), (0.60,75), (0.85,100))
```

### 4.3 Quality side-components (not raw $)

```text
profit_factor_score =
    LerpScore(pf, (0.00,0), (0.80,30), (1.00,50), (1.50,75), (2.50,95), (PF_CAP,100))

expectancy_score =
    x = expectancy_R if defined else expectancy_Rpx
    LerpScore(x, (-1.00,0), (0.00,45), (0.25,65), (0.60,85), (1.20,100))
```

`PF_CAP = 5`, `PAYOFF_CAP = 5`.

**Forbidden inputs** to any of the three headline scores: `NET` in account currency, account balance, challenge phase, group plan name, destination P&L, future trades.

---

## 5. `risk_score`

**Range:** `[0, 100]`. **Direction:** higher = more dangerous.

### 5.1 Weighted body

When `mfe_mae_used == false` (the common case):

```text
risk_body =
    0.22 * martingale_risk
  + 0.16 * averaging_down_risk
  + 0.14 * lot_escalation_risk
  + 0.12 * loss_size_risk
  + 0.12 * drawdown_risk
  + 0.10 * no_sl_risk
  + 0.08 * revenge_risk
  + 0.06 * frequency_risk
```

When `mfe_mae_used == true`, replace the last 0.06 bucket:

```text
risk_body =
    0.20 * martingale_risk
  + 0.14 * averaging_down_risk
  + 0.13 * lot_escalation_risk
  + 0.11 * loss_size_risk
  + 0.11 * drawdown_risk
  + 0.09 * no_sl_risk
  + 0.08 * revenge_risk
  + 0.06 * frequency_risk
  + 0.08 * mae_overrun_risk
```

Weights always sum to `1.00`.

### 5.2 Hard floors (flags raise the floor; they do not add)

```text
risk_score = risk_body

if FLAG_MARTINGALE:        risk_score = Max(risk_score, 80)
if FLAG_AVERAGING_DOWN:    risk_score = Max(risk_score, 75)
if FLAG_ABNORMAL_SIZING:   risk_score = Max(risk_score, 70)
if FLAG_SEVERE_DRAWDOWN:   risk_score = Max(risk_score, 75)
if FLAG_BURST_FREQUENCY:   risk_score = Max(risk_score, 60)
if FLAG_NO_STOP_LOSS:      risk_score = Max(risk_score, 55)
if FLAG_MAE_EXCEEDS_SL:    risk_score = Max(risk_score, 70)   // EXACT only

risk_score = Round2(Clamp(risk_score, 0, 100))
```

Floors exist so a single catastrophic pattern cannot be averaged away by three tidy-looking secondary terms.

---

## 6. `behavior_score`

**Range:** `[0, 100]`. **Direction:** higher = healthier process. Independent of dollar P&L.

When `mfe_mae_used == false`:

```text
behavior_body =
    0.20 * lot_consistency_score
  + 0.15 * loss_size_consistency_score
  + 0.15 * sl_discipline_score
  + 0.15 * no_martingale_score
  + 0.10 * hold_stability_score
  + 0.10 * expectancy_process_score
  + 0.08 * no_averaging_score
  + 0.07 * session_stability_score
```

When `mfe_mae_used == true`, swap the 0.07 session term for MFE capture:

```text
  … + 0.07 * mfe_capture_score
```

```text
if FLAG_MARTINGALE:      behavior_body = Min(behavior_body, 35)
if FLAG_AVERAGING_DOWN:  behavior_body = Min(behavior_body, 45)
if FLAG_ABNORMAL_SIZING: behavior_body = Min(behavior_body, 50)

behavior_score = Round2(Clamp(behavior_body, 0, 100))
```

Caps prevent a “pretty” SL-rate from laundering a martingale book.

---

## 7. `early_quality_score`

**Range:** `[0, 100]`. **Direction:** higher = better early evidence of *copyable* quality.

This is the operational ranking key (leaderboard “Early score”). It is **recomputed** on every `EXPANDING` window. The name does not change after trade #3.

### 7.1 Blend

```text
raw =
    0.45 * behavior_score
  + 0.35 * (100 - risk_score)
  + 0.12 * profit_factor_score
  + 0.08 * expectancy_score
```

P&L-shaped terms are capped at **20%** combined. A lucky three-trade spike cannot dominate a reckless process.

### 7.2 Sample-uncertainty penalty

Three trades are evidence, not a track record. Subtract a deterministic penalty that shrinks with `N`:

```text
U(N) =
    N <=  2 : 100     // not used for official scores
    N ==  3 :  18
    N ==  4 :  15
    N ==  5 :  12
    N <=  7 :  10
    N <= 10 :   7
    N <= 15 :   5
    N <= 20 :   3
    N <= 40 :   1
    N >  40 :   0
```

```text
early_quality_score = Round2(Clamp(raw - U(N), 0, 100))
```

At `N = 3`, a theoretically perfect book scores at most `100 - 18 = 82`. That is deliberate: Trade #3 cannot look like a proven live trader on the leaderboard.

### 7.3 What this score is not

| Reject | Why |
|---|---|
| `rank by NET` | §3 forbids “who made the most in 3 trades” |
| `rank by win_rate` | High WR + rare huge loss is the prop-blowup pattern |
| `rank by max_volume` | Size is a risk input, not a quality input |
| Include shadow/live P&L | Would make scored and unshadowed books incomparable; leakage vs §8 |

---

## 8. Risk flags

Flags are first-class (`trader_risk_flags`). They feed floors/caps **and** the state machine.

| Flag | Predicate (v1 defaults) | Severity |
|---|---|---|
| `FLAG_MARTINGALE` | `martingale_events >= 1` | severe |
| `FLAG_AVERAGING_DOWN` | `avg_down_n >= 2` OR (`avg_down_n >= 1` AND `N <= 3`) | severe if `avg_down_n >= 2` or (`N<=3` and `avg_down_n>=1`); else watch |
| `FLAG_LOT_ESCALATION` | `vol_span >= 3.0` OR `escalation_events >= 2` | severe if `vol_span >= 4` or `escalation_events >= 2`; else watch |
| `FLAG_ABNORMAL_SIZING` | `vol_span >= 5.0` OR (`N >= 3` AND `lot_cv >= 1.50`) | severe |
| `FLAG_NO_STOP_LOSS` | `sl_rate == 0` AND `N >= 3` | watch (floor only; not enough alone to block) |
| `FLAG_BURST_FREQUENCY` | `burst_flag` | watch; severe if also `FLAG_MARTINGALE` or `FLAG_NO_STOP_LOSS` |
| `FLAG_SEVERE_DRAWDOWN` | `max_dd_frac >= 0.50` | severe |
| `FLAG_MAE_EXCEEDS_SL` | `mfe_mae_used` AND `mae_overrun_rate >= 0.50` | severe |
| `FLAG_REVENGE` | `revenge_events >= 2` | watch |

```text
severe_risk = any flag currently marked severe
```

`FLAG_AVERAGING_DOWN` at `N = 3` with a single averaged trade is **severe**. One averaging-down lifecycle in a three-trade book is already the pattern we are screening, not a rounding error.

Manual flags (`MANUAL_BLOCK`, `MANUAL_DISQUALIFY`, `MANUAL_PAUSE`) are RBAC-audited and take priority (see §9.3).

---

## 9. Trader states

Vocabulary is exactly §22:

```text
INSUFFICIENT_DATA
EARLY_SCORE
WATCH
SHADOW
LIVE_CANDIDATE
LIVE
PAUSED
RISK_BLOCKED
DISQUALIFIED
```

Plus the **event** (not a state) from §15:

```text
EARLY_SCORE_ELIGIBLE      // emitted once when N first reaches 3
```

Never emit `PROVEN_PROFITABLE`. That token is not in the state vocabulary.

### 9.1 Meaning

| State | Meaning | Copy? |
|---|---|---|
| `INSUFFICIENT_DATA` | `N < 3`. No official score. | none |
| `EARLY_SCORE` | Officially scored; not selected for watch/shadow; not blocked. Default landing when `N >= 3` and quality is weak. | none |
| `WATCH` | Mid-band quality. Human/ops interest. Not shadowed. | none |
| `SHADOW` | High early quality. Destination-quote **simulation only**. Default action after Trade #3 + high score. | shadow only |
| `LIVE_CANDIDATE` | Passed **structure** of live gates (sample + shadow evidence). Awaiting risk engine + RBAC. **Impossible at `N == 3`.** | still shadow |
| `LIVE` | Risk-approved real copy. Feature flag + risk engine. **Impossible at `N == 3`.** | real, if `REAL_COPY_EXECUTION_ENABLED` |
| `PAUSED` | Manual or operational pause. Previous state stored. | none (flatten/reduce is risk-engine, not scorer) |
| `RISK_BLOCKED` | Severe flags or risk-engine block. | none |
| `DISQUALIFIED` | Terminal / near-terminal (repeated martingale, confirmed abuse, manual DQ). | none |

### 9.2 Trade #3 hard rule (must be unit-tested)

```text
WHEN N == 3:
    emit EARLY_SCORE_ELIGIBLE once (idempotent on reconstruction replay)

    next_state ∈ {
        EARLY_SCORE, WATCH, SHADOW,
        PAUSED, RISK_BLOCKED, DISQUALIFIED
    }

    next_state ∉ { LIVE, LIVE_CANDIDATE }

    EVEN IF early_quality_score == 82 (v1 cap) AND NET is large:
        maximum automatic promotion = SHADOW
```

Pseudo-assert:

```text
Assert(N != 3 || (state != LIVE && state != LIVE_CANDIDATE))
Assert(N < 3  || event_on_first_crossing_3 == EARLY_SCORE_ELIGIBLE)
```

### 9.3 Resolver priority

A single pure function `ResolveState(input) -> state`. First matching rule wins.

Inputs:

```text
N, scores, flags, severe_risk
shadow_completed_trades, shadow_net_pnl, shadow_max_dd_frac
manual_pause, manual_dq, manual_block, manual_live_approve
prev_state
REAL_COPY_EXECUTION_ENABLED          // never inspected to *raise* to LIVE by scorer
risk_engine_live_ok                  // external; scorer may only *nominate*
```

Rules, in order:

```text
R0  if manual_dq:                              DISQUALIFIED

R1  if prev_state == DISQUALIFIED
       and not explicit audited reclaim:       DISQUALIFIED

R2  if manual_pause:                           PAUSED

R3  if manual_block or severe_risk:            RISK_BLOCKED

R4  if N < 3:                                  INSUFFICIENT_DATA

R5  if N == 3:
       if early_quality >= SHADOW_MIN:         SHADOW
       elif early_quality >= WATCH_MIN:        WATCH
       else:                                   EARLY_SCORE
       // LIVE / LIVE_CANDIDATE unreachable

R6  if N >= MIN_LIVE_TRADES
       and shadow_completed_trades >= MIN_SHADOW_TRADES
       and shadow_net_pnl >= MIN_SHADOW_PNL
       and shadow_max_dd_frac <= MAX_SHADOW_DD_FRAC
       and early_quality >= MIN_LIVE_SCORE
       and not severe_risk
       and N > 3:                              // belt and braces
         if manual_live_approve
            and risk_engine_live_ok:           LIVE
         else:                                 LIVE_CANDIDATE

R7  if early_quality >= SHADOW_MIN:            SHADOW

R8  if early_quality >= WATCH_MIN:             WATCH

R9  else:                                      EARLY_SCORE
```

Notes:

- `R5` is evaluated **before** `R6`. At `N == 3`, live gates are not reachable even if someone sets `MIN_LIVE_TRADES = 3` in config. The `N > 3` conjunct in `R6` is a second lock.
- `MIN_LIVE_TRADES` **must be `> 3`**. Config loader rejects `<= 3`.
- Scorer never reads `REAL_COPY_EXECUTION_ENABLED` to promote. That flag is an execution concern (§41). A trader can sit in `LIVE` as a *selection* state while the venue still refuses `NewOrderSingle`.
- Demotion is automatic on rescore: a `SHADOW` trader who picks up `FLAG_MARTINGALE` on trade #6 falls to `RISK_BLOCKED` via `R3`.
- Leaving `PAUSED` requires clearing `manual_pause`; the next rescore then runs R0–R9 on current evidence (not a blind restore of a stale `LIVE`).

### 9.4 Provisional v1 gate constants

**Not proven.** Structure is locked; values are backtest knobs. Config schema:

```text
ScoreConfig.v1
  version                         = "baseline.v1"

  SHADOW_MIN                      = 62     // after U(N); at N=3 this is a strong book
  WATCH_MIN                       = 48

  MIN_LIVE_TRADES                 = 20     // MUST be > 3; loader-enforced
  MIN_SHADOW_TRADES               = 10
  MIN_SHADOW_PNL                  = 0      // destination-quote shadow net
  MAX_SHADOW_DD_FRAC              = 0.15
  MIN_LIVE_SCORE                  = 70

  MARTINGALE_VOLUME_RATIO         = 1.80
  REVENGE_VOLUME_RATIO            = 1.40
  ESCALATION_VOLUME_RATIO         = 2.00
  BURST_WINDOW_SEC                = 300
  PF_CAP                          = 5
  PAYOFF_CAP                      = 5
  R_MIN_DEFINED                   = 2
  MAE_OVER_SL                     = 1.10
  DD_SCALE_EPS                    = 0.01
```

§23: do not treat these numbers as production-optimal. Changing a number increments a config revision (`baseline.v1.N`) and is stored on every snapshot. Changing a *formula* requires `baseline.v2` and a new spec.

### 9.5 Allowed transitions (reference)

```text
INSUFFICIENT_DATA → EARLY_SCORE | WATCH | SHADOW | RISK_BLOCKED | DISQUALIFIED | PAUSED
                    (only when N reaches 3; never → LIVE / LIVE_CANDIDATE)

EARLY_SCORE       → WATCH | SHADOW | RISK_BLOCKED | DISQUALIFIED | PAUSED
                    | LIVE_CANDIDATE   (only N > 3 and R6)
                    | LIVE             (only via LIVE_CANDIDATE path, N > 3)

WATCH             → SHADOW | EARLY_SCORE | RISK_BLOCKED | DISQUALIFIED | PAUSED
                    | LIVE_CANDIDATE | LIVE     (N > 3, R6)

SHADOW            → WATCH | EARLY_SCORE | RISK_BLOCKED | DISQUALIFIED | PAUSED
                    | LIVE_CANDIDATE | LIVE     (N > 3, R6)

LIVE_CANDIDATE    → LIVE | SHADOW | WATCH | RISK_BLOCKED | DISQUALIFIED | PAUSED

LIVE              → PAUSED | RISK_BLOCKED | DISQUALIFIED | SHADOW | WATCH
                    (demote if gates fail on rescore)

PAUSED            → (re-resolve from current evidence; not a restore)

RISK_BLOCKED      → EARLY_SCORE | WATCH | SHADOW | DISQUALIFIED | PAUSED
                    (only if severe_risk cleared)

DISQUALIFIED      → (reclaim only via audited manual action, then re-resolve)
```

Illegal at any time:

```text
* → LIVE             when N == 3
* → LIVE_CANDIDATE   when N == 3
INSUFFICIENT_DATA → LIVE*
any auto path that skips SHADOW evidence into LIVE
```

### 9.6 Continuous rescoring (§22)

On each newly completed eligible trade `k >= 3`:

```text
1. Rebuild window [1..k] as-of closed_at(k).
2. Compute features + flags + three scores (EXPANDING).
3. If k == 3: also persist FIRST3 snapshot; emit EARLY_SCORE_ELIGIBLE.
4. Resolve state.
5. INSERT trader_score_history row (never update-in-place).
6. UPSERT trader_scores current row.
7. UPSERT trader_states if state changed (old → new, reason, as_of).
8. UPSERT trader_risk_flags (close cleared flags with ended_at).
9. Persist trader_feature_snapshots (full component breakdown).
```

Replay of the same close must be idempotent: same `as_of` + `n` + `score_version` + `window` replaces nothing that differs; a unique key prevents duplicates.

---

## 10. Persistence contract

Logical columns only. No schema migration in this change-set.

### 10.1 `trader_feature_snapshots`

```text
id
broker_id, login
n, as_of, window, score_version
price_source, feature_quality, mfe_mae_used
N, GP, GL, NET, pf, win_rate, lot_cv, loss_cv
sl_rate, avg_down_n, scale_in_n, hold_cv
martingale_events, revenge_events, escalation_events, vol_span
max_dd, max_dd_frac, trades_per_day, burst_flag, session_max_frac
expectancy_R, expectancy_Rpx, expectancy_used   // which one
mae_overrun_rate, mean_mfe_capture              // null if unused
component_json                                  // every * _risk / *_score term
created_at
```

### 10.2 `trader_scores` (current)

```text
broker_id, login
n, as_of, score_version, window = EXPANDING
risk_score
behavior_score
early_quality_score
state
severe_risk
last_event                    // EARLY_SCORE_ELIGIBLE | RESCORED | STATE_CHANGED
```

### 10.3 `trader_score_history`

Append-only copy of every official compute, including `FIRST3` and every later `EXPANDING`.

### 10.4 `trader_states`

```text
broker_id, login
state
prev_state
reason                        // rule id R0..R9 + flag list
n, as_of
changed_at
actor                         // system:baseline.v1 | user:<id>
```

### 10.5 `trader_risk_flags`

```text
broker_id, login, flag
severity                      // watch | severe
opened_at, opened_n
ended_at, ended_n             // null if active
evidence_json
```

---

## 11. Calculator API (normative shape)

Pure, side-effect free:

```text
ScoreResult Compute(
    IReadOnlyList<ReconstructedTrade> window,   // already as-of filtered
    ScoreConfig cfg,
    ScoreAsOf meta)

ScoreResult {
    risk_score            decimal(5,2)
    behavior_score        decimal(5,2)
    early_quality_score   decimal(5,2)
    flags[]
    severe_risk           bool
    components            // named decimals, for snapshot
    mfe_mae_used          bool
}

TraderState ResolveState(StateInput input)      // also pure
```

No database, no clock, no RNG, no ML library reference.

---

## 12. Worked examples (normative)

Constants = v1 defaults. Volumes in lots. P&L already net. No EXACT MFE/MAE (`mfe_mae_used = false`). Numbers below are the **acceptance fixtures**; implementers must match `Round2` results within `0.05` (fixture table in unit tests should pin the exact computed values from the reference implementation once written). The qualitative outcomes are **hard**: they cannot flip.

### 12.1 Case A — clean three-trade book → SHADOW, never LIVE

```text
t1  vol=0.10  pnl=+80   sl=yes  hold=45m   no scale-in
t2  vol=0.10  pnl=-40   sl=yes  hold=30m
t3  vol=0.10  pnl=+60   sl=yes  hold=50m
N=3  NET=+100  lot_cv=0  martingale_events=0  sl_rate=1  avg_down=0
     pf=140/40=3.50 → capped map high
     U(3)=18
```

Expected:

```text
risk_score           low  (no floors)
behavior_score       high
early_quality_score  mid-high, but ≤ 82 because of U(3)
state                SHADOW
event                EARLY_SCORE_ELIGIBLE
NOT                  LIVE, LIVE_CANDIDATE, PROVEN_PROFITABLE
```

### 12.2 Case B — huge profit, martingale → RISK_BLOCKED, not LIVE

```text
t1  vol=0.10  pnl=-50    sl=no
t2  vol=0.20  pnl=-100   sl=no          // ratio 2.0 after loss
t3  vol=0.40  pnl=+800   sl=no          // ratio 2.0 after loss
N=3  NET=+650  pf high   martingale_events=2  vol_span=4  sl_rate=0
```

Expected:

```text
FLAG_MARTINGALE, FLAG_LOT_ESCALATION, FLAG_ABNORMAL_SIZING, FLAG_NO_STOP_LOSS
risk_score           >= 80            (martingale floor)
behavior_score       <= 35            (martingale cap)
early_quality_score  low despite NET
state                RISK_BLOCKED
NOT                  SHADOW, LIVE, LIVE_CANDIDATE
```

This is the load-bearing anti-pattern: **dollars do not buy a live seat.**

### 12.3 Case C — mixed, no SL, mild size-up → EARLY_SCORE

```text
t1  vol=0.10  pnl=+30    sl=no
t2  vol=0.10  pnl=+10    sl=no
t3  vol=0.14  pnl=-25    sl=no          // ratio 1.40 after win, not martingale
N=3  martingale_events=0  sl_rate=0  lot_cv modest
```

Expected:

```text
FLAG_NO_STOP_LOSS (watch, not severe alone)
state = EARLY_SCORE or WATCH
NOT LIVE / LIVE_CANDIDATE / SHADOW unless early_quality >= SHADOW_MIN
```

### 12.4 Case D — two trades → INSUFFICIENT_DATA

```text
N=2, any P&L, any flags
```

Expected:

```text
official scores     not published to leaderboard
optional PROVISIONAL snapshot allowed
state               INSUFFICIENT_DATA
no EARLY_SCORE_ELIGIBLE
```

### 12.5 Case E — Trade #20, good shadow → LIVE_CANDIDATE, not auto LIVE

```text
N=20
early_quality >= 70
shadow_completed_trades=12, shadow_net_pnl>0, shadow_max_dd_frac=0.08
severe_risk=false
manual_live_approve=false
```

Expected:

```text
state = LIVE_CANDIDATE
NOT LIVE until risk_engine_live_ok AND audited manual_live_approve
```

### 12.6 Case F — attempted config attack

```text
MIN_LIVE_TRADES set to 3, N=3, perfect scores, all shadow fields zero/null
```

Expected:

```text
config loader rejects MIN_LIVE_TRADES <= 3
AND R5 still forces {EARLY_SCORE, WATCH, SHADOW, …}
state ≠ LIVE and ≠ LIVE_CANDIDATE
```

---

## 13. Test matrix (required before claiming Phase 3 done)

Architecture §60 already requires `score-state transitions`, `martingale detection`, `averaging-down detection`, `drawdown`, `MFE/MAE where data exists`. This spec adds:

| Test | Assert |
|---|---|
| T1 Universe | Partial close / SL modify / extra deal does not increment `N` |
| T2 Order | Equal `closed_at` broken by `opened_at` then `id` |
| T3 First score | `N==3` emits `EARLY_SCORE_ELIGIBLE` once under replay |
| T4 No live@3 | Property: for any fixture with `N==3`, state ∉ {LIVE, LIVE_CANDIDATE} |
| T5 Case A | Lands `SHADOW` |
| T6 Case B | Lands `RISK_BLOCKED`; `early_quality` < Case A despite higher NET |
| T7 Case D | `INSUFFICIENT_DATA` |
| T8 Leakage | Injecting trade #4 into a FIRST3 call changes nothing if caller filters; calculator that receives 4 trades with `window=FIRST3` **rejects** |
| T9 MFE absent | `feature_quality=APPROXIMATE` → `mfe_mae_used=false`; scores ignore MAE |
| T10 MFE present | EXACT values move only the documented 0.07 / 0.08 terms |
| T11 Idempotent | Same window hashed twice → identical `Round2` scores |
| T12 U(N) | `N=3` score is `raw-18`; `N=40` uses `raw-0` |
| T13 Config | `MIN_LIVE_TRADES<=3` fails validation |
| T14 Demote | SHADOW + new martingale on trade #6 → RISK_BLOCKED |
| T15 Pause | `manual_pause` wins over SHADOW; clearing pause re-resolves |
| T16 No $ rank | Two books, same process, NET 10 vs NET 10_000 → `early_quality` within 2.00 |
| T17 Pure | Calculator assembly references no ML package |

---

## 14. Evaluation role (for later; not implemented here)

When a model exists, §21 comparison is:

```text
baseline rank key = early_quality_score on FIRST3 window
model    rank key = predicted P(future net copy P&L > 0 AND DD <= limit)

For top 1 / 5 / 10 / 20 % of each:
    future net copied P&L, max DD, profit factor,
    return volatility, CVaR, trade count,
    execution cost, slippage sensitivity

vs all / random / this baseline / highest historical P&L / highest win-rate
```

ML is justified only if it beats **this** baseline out-of-sample. Highest-historical-P&L and highest-win-rate are additional naive baselines, not replacements for `early_quality_score`.

---

## 15. Explicit exclusions

```text
No ML in baseline.v1
No deep neural net
No LLM scoring
No Trade #3 → LIVE
No Trade #3 → LIVE_CANDIDATE
No Trade #3 → PROVEN_PROFITABLE
No raw NET ranking
No fabricated MFE/MAE
No destination quote used as source tick
No product-source edits as part of this spec
```

---

## 16. Traceability to architecture §§18–23

| Section | How this spec implements it |
|---|---|
| §18 Deterministic baseline before ML | Three named scores; rules + statistics only; benchmark for ML |
| §18 inputs | Encoded as features in §3 and maps in §4 (pnl structure, PF, lot CV, loss-size CV, martingale, averaging, hold, SL, MFE/MAE if EXACT, escalation, DD, frequency, session) |
| §19 ML objective | Out of scope to train; `FIRST3` snapshot is the future feature window; target remains trades #4–#23 venue-net with DD constraint |
| §20 Leakage | As-of filter, FIRST3 isolation, no future fields, no destination P&L inside scores |
| §21 Evaluation | §14 names the rank key and the comparison set |
| §22 Continuous rescoring | §9.6 + history tables; Trade #3 is first score |
| §22 states | §9 full resolver |
| §23 Safety gate | Trade #3 + high score = SHADOW; live gates require later sample + shadow evidence; numbers are config, not claimed truth |

---

## 17. Implementation note

Phase 3 implementers should add a calculator in the Application/Domain layer as a pure function and gold-file the Case A–F fixtures. Do not put these weights in a React client. Do not silently change `U(3)` to `0` to make the leaderboard “look more confident.”

**Trade #3 = `EARLY_SCORE` evidence (event `EARLY_SCORE_ELIGIBLE`, maximum automatic state `SHADOW`). Trade #3 ≠ `LIVE`.**
