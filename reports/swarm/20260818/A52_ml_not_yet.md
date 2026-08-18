# A52 — Why ML Is Not Built Now (Architecture §§19–21, Phase 6 Only)

| Field | Value |
|---|---|
| Agent | A52 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A52_ml_not_yet.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Primary sections | **§19 ML Objective**, **§20 Data Leakage Protection**, **§21 Model Evaluation** |
| Supporting sections | §1 (do not use ML first), §3 (business target), §12 (no callback→ML), §15 (first-3 meaning), §17 (do not fabricate MFE/MAE), §18 (deterministic baseline first), §22 (rescoring), §23 (trade #3 → SHADOW only), §39 / §72.15 (ML never bypasses risk), §45 / A20 (tables), §50 / A26 (`mlProbability` nullable), §60 / A27 (no ML as a §69 gate), §62 (ML unavailable), §66 (`/services/ml-service` later), **§67 Phase 6**, §69, §71, §75 |
| Phase | **6 only**. Phases 0–5, 7, 8 are out of this document’s build scope. |
| Product source edited | **No** |

---

## 1. Verdict

**Do not build ML now.** Phase 6 is not open.

Architecture §§19–21 define a **later** research job: train XGBoost on features that were observable at the exact close of reconstructed XAUUSD trade #3, label the **next twenty completed copy-horizon trades** by **execution-venue-net** P&L under a drawdown cap, split **chronologically**, and accept the model **only** if it beats the Phase 3 deterministic baseline on **top-N economics**, not accuracy.

That job has no legal inputs today:

- Phase 3 baseline does not exist, so there is nothing for ML to beat (§18, §21, §67 Phase 3).
- Phase 5 shadow copy does not exist, so the official label `future_net_copy_pnl` cannot be computed (§19, §24, §67 Phase 5).
- There is no as-of feature snapshot keyed by `(broker_id, login, completed_trade_count)` (§20, A20 `trader_feature_snapshots`).
- There is no durable reconstructed-trade ledger feeding a first-3 / 4–23 counter into a dataset.
- There is no source tick tape; fabricating MFE/MAE from closed deals is forbidden (§1.5, §17, A17).
- `D:\Prop\services` is empty. There is no Python/XGBoost service, no MLflow, no training script, no `model_*` tables.

This is the **correct** interim state. Starting XGBoost, scikit-learn, a FastAPI scorer, or a fake `mlProbability` now would be a policy FAIL even if the code compiled.

**First useful version (§69) does not include Phase 6.** If ML does not later beat the baseline out of sample, the baseline **remains** the production scorer (§21, A28 Phase 6 exit).

---

## 2. How to use this document

- Treat this file as the Phase 6 **hold** and the §§19–21 **contract**. Do not implement `/services/ml-service` from it.
- Phases are sequential (A28). Phase 6 depends on Phase 5 exit **and** proven data quality (§67 Phase 6).
- Items marked `[ ]` stay unchecked until evidence exists (dataset rows, split hashes, evaluation tables). Do not mark them done from intention.
- UI may show `mlProbability: null` (A26). **Do not stub a number.**
- Scoring/ML, when it exists, may emit only `candidate / confidence / suggested allocation` (§39). Risk remains the send authority (§72.15).

---

## 3. Binding architecture (quoted)

### 3.1 §19 — ML Objective

Once **sufficient clean historical data exists**, train:

```text
Input:
behavior/features observable through completed trade #3

Target:
future execution-venue-net profitability
over trades #4 through #23
subject to drawdown constraint
```

Initial label (architecture text):

```text
label = 1
if:
    future_net_copy_pnl > 0
    AND future_max_drawdown <= configured_limit
else:
    label = 0
```

Use **XGBoost** initially. Do **not** use a deep neural network first.

### 3.2 §20 — Data Leakage Protection

Never expose future information to the model.

A trade-#3 sample may only use information available **up to the exact timestamp that trade #3 completed**.

Do not include:

- final challenge result
- future balance
- future drawdown
- trade #4 onward
- eventual pass/fail
- future market information

Split training **chronologically**. Architecture example:

```text
oldest 70% → training
next 15%   → validation
newest 15% → untouched final test
```

### 3.3 §21 — Model Evaluation

Do **not** optimize around raw accuracy.

Evaluate Top 1% / 5% / 10% / 20% selected traders. For each, calculate future:

```text
net copied P&L
max drawdown
profit factor
return volatility
CVaR
trade count
execution cost
slippage sensitivity
```

Compare against:

```text
all traders
random traders
simple rules baseline
highest historical P&L baseline
highest win-rate baseline
```

**ML is justified only if it beats simple baselines out-of-sample.**

### 3.4 Adjacent law that keeps Phase 6 closed

| Location | Binding statement |
|---|---|
| §1 change #3 | Do not use ML first. Build a deterministic statistical baseline first. ML must beat that baseline out-of-sample. |
| §3 | Target is **future** copyable profitability inside risk limits, **not** “who made the most money in their first 3 trades.” |
| §12 | Outbox after persist. Do not couple MT5 callbacks to ML. |
| §15 | Trade #3 close → `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`. Count only completed reconstructed XAUUSD position lifecycles. |
| §17 / A17 | Exact MFE/MAE needs a **source** tick tape while the position is open. Do not fabricate from closed deals. Label `price_source` + `feature_quality`. Never silently mix source ticks, bars, and cTrader quotes. |
| §18 | Before XGBoost, build `risk_score`, `behavior_score`, `early_quality_score`. That baseline is the benchmark ML must beat. |
| §22 | First score at trade #3; then rescore after 4, 5, 6, … History is append-only. |
| §23 | Trade #3 + high score → **SHADOW only**. Do not send real capital. Do not hardcode gates before backtesting. |
| §39 / §72.15 | ML never bypasses risk. Output is candidate / confidence / suggested allocation only. |
| §62 | ML unavailable: continue ingestion/reconstruction; do **not** promote new traders to live; hard limits stay on. |
| §67 Phase 6 | Deliver **only after data quality is proven**: training dataset, chronological split, XGBoost, probability calibration, top-N evaluation, comparison against deterministic baseline. |
| §69 | First useful system **does not need ML**. Judge ML only after items 1–12 work. |
| §71 | Do not add: LLM/AI API, deep learning, reinforcement learning, automated model self-promotion. |
| §72.16 | Trade #3 is early evidence, not proven skill. |
| §72.20 / §71 | Prefer simple systems until measurements justify complexity. |
| §75 | Pipeline is rules/statistical baseline → **ML ranking when justified** → trader state → shadow → CopyIntent → risk. |

---

## 4. Why ML is not built now

Six independent blockers. Any one is sufficient to keep Phase 6 closed.

### 4.1 Phase order (process)

```text
Phase 0 audit
  → 1 ingestion
  → 2 XAU reconstruction + first-3 counter
  → 3 deterministic feature engine + baseline scores + dashboard
  → 4 cTrader QUOTE
  → 5 shadow copy (destination-venue P&L)
  → 6 ML   ← we are not here
```

A28: do not start a later phase until the prior phase’s exit criteria are true. Phase 6 also requires **data quality proven**, not merely “Phase 5 folder exists.”

Phase 5 exit still includes “**ML is still not required**.” Phase 6 is optional relative to the first useful bar.

### 4.2 The official label does not exist yet

§19’s `y` is **not** source-broker P&L of trades 4–23.

| Symbol | Meaning | Available now? |
|---|---|---|
| `future_net_copy_pnl` | Net P&L of a **copy** of reconstructed XAUUSD trades #4–#23 on the **execution venue**, after destination fill, costs, and slippage | **No** — needs Phase 5 shadow (or later live fills). Source `NetRealizedPnl` is the wrong series. |
| `future_max_drawdown` | Max drawdown of that **same copy book** over the same horizon | **No** |
| `configured_limit` | Drawdown cap used in the label | **No** — §23 forbids hardcoding this before backtesting |

Without those three, every row’s `label ∈ {0,1}` is either missing or a **different problem** than §19. Training a different problem and calling it Phase 6 is a false PASS.

### 4.3 Features-as-of-t3 do not exist yet

§19 input is “behavior/features **observable through completed trade #3**.”

That requires an **as-of snapshot**, not the latest `TraderScore` row.

Intended durable key (A20):

```text
trader_feature_snapshots
  UNIQUE (broker_id, login, completed_trade_count, feature_schema_version)
  as_of = trade-#N close time
  carry price_source, feature_quality
```

Repo today:

- No `TraderFeatureSnapshot` type.
- `TraderScore` / `TraderScoreHistory` are **latest / history of blended scores**, not as-of feature vectors. They have no `completed_trade_count` on the history unique key, no `price_source`, no `feature_quality`.
- `FeatureQuality` and `PriceSource` enums exist (`D:\Prop\src\Domain\Enums\`) and are **unused** on any snapshot.
- Phase 3 “deterministic feature engine” is not implemented. Application is still `Class1`. There is no `/src/Scoring`.

Computing features from “whatever the account looks like today” **is leakage** (§20). Building a model on that matrix is forbidden, not “good enough for a prototype.”

### 4.4 There is no chronological population to split

§20’s 70 / 15 / 15 split is a split of **decision-time samples**, not of logins, not of deals.

A row exists only when:

1. The trader has **three completed reconstructed XAUUSD lifecycles** (§15).
2. Features were frozen at `closed_at` of trade #3 (or, for later rescoring samples, at close of trade *N*).
3. The label horizon is **complete or explicitly censored** (see §6.4). For the official 4–23 label, that means twenty subsequent completed XAUUSD lifecycles **and** a copy-book P&L for them.

None of those row conditions are produced by a dataset builder today. Applying sklearn `train_test_split` to raw deals or to current accounts is not §20.

### 4.5 There is no baseline to beat

§18 / §21 / §67 Phase 3: ML is a challenger, not the first scorer.

Missing Phase 3 delivers:

```text
[ ] deterministic feature engine
[ ] risk flags
[ ] early scoring baseline
[ ] React trader dashboard ranking
```

Without `risk_score`, `behavior_score`, `early_quality_score`, and the §21 simple-rules / highest-historical-P&L / highest-win-rate comparators, any AUC or log-loss number is unanchored. A model that “looks good” against a dummy classifier is **not** justified.

### 4.6 Data quality is not proven (ingestion / reconstruction / ticks)

Even a correct label/feature spec is worthless on dirty rows.

Measured gaps (do not greenwash):

| Prerequisite | Honest state |
|---|---|
| Dual-broker MT5 ingestion + idempotent deals | C++ SDK ledger exists behind `MT5SDK_WITH_POSTGRES`; C# workers are not a proven Phase 1 collector. A03/A07: live Application ingestion path missing. |
| Reconstructed trades persisted | `TradeReconstructor` can rebuild lifecycles **in memory** and expose `IsEarlyScoreEligible` (≥3 completed XAUUSD). There is no proven persist/replay dataset of `reconstructed_trades` for ~5,000 accounts. |
| First-3 definition | Coded as completed XAUUSD position-flat (or reversal close), matching §15. **Not** wired to scores, snapshots, or labels. No unit tests under `D:\Prop\tests` reference `IsEarlyScoreEligible`. |
| Source tick tape for exact MFE/MAE | **FAIL** (A17). `MT5TickBridge` is in-memory fan-out. No `mt5_xau_ticks` writer. Closed deals are not a path. |
| Destination quotes + shadow book | A24 is a **spec**. No production shadow P&L series. |
| Migrations for scoring/ML tables | A20 catalogs `trader_feature_snapshots`, `model_versions`, `model_predictions`, `model_evaluations`. They are **not** in product SQL. `TraderDbContext` has no `DbSet` for any `model_*` or feature-snapshot type. |
| `/services/ml-service` | **Missing.** `D:\Prop\services` is an empty directory (A11). No `.py`, no XGBoost, no MLflow. |

Training on this stack would either invent features, invent labels, or both.

---

## 5. Current repo (measured, 2026-08-18)

Read-only survey. Product source was not modified to produce this report.

### 5.1 Present (not Phase 6)

| Path | What it is | What it is not |
|---|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | In-memory lifecycle rebuild; `CountCompletedXauUsdTrades`, `IsEarlyScoreEligible` | Dataset builder, labeler, scorer |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | Entity shape aligned with §14 (no MFE/MAE fields — correct) | Persisted population |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | Latest `RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CompletedXauTrades`, `TraderState` | As-of feature vector; no `MlProbability` |
| `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | Score history snapshot | Append-only as-of features; no `completed_trade_count` in the type |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | §22 state vocabulary | A quality label. `LIVE` / `DISQUALIFIED` after t3 must **not** be an ML feature |
| `D:\Prop\src\Domain\Enums\FeatureQuality.cs` | `Exact / Approximate / Unavailable` | Unused |
| `D:\Prop\src\Domain\Enums\PriceSource.cs` | Achiever ticks / Starwave ticks / bar approx / cTrader quote | Unused; mixing these silently is a leak |
| `D:\Prop\src\Domain\Enums\OutboxEventType.cs` | Includes `ScoreUpdate` | No handler (A02 S2 missing) |
| `D:\Prop\src\Application\Class1.cs` | Empty template | No `IScoringService` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Draft DbSets for brokers/deals/scores/shadow/FIX | **No** `model_versions`, `model_predictions`, `model_evaluations`, `trader_feature_snapshots` |

### 5.2 Absent (Phase 6 surface)

```text
D:\Prop\services\ml-service          (directory does not exist; parent is empty)
D:\Prop\src\Scoring                  (not created — do not stub now)
D:\Prop\docs\ml.md                   (suggested by §66; do not write product docs in this pass)
Python / XGBoost / scikit-learn / Polars training code
MLflow
model_versions / model_predictions / model_evaluations migrations
IScoringService / IScoreUpdateRequestHandler (A02 S1–S3)
Fake mlProbability on the API (A06 / A26: leave null)
```

Grep of `*.cs` / `*.py` / `*.sql` / `*.csproj` under the product tree for `XGBoost`, `sklearn`, `mlflow`, `IScoring`, `FeatureSnapshot`, `ModelVersion` returned **no product hits**. Those names live in architecture + swarm reports only.

### 5.3 Domain types that would leak if used naively as “features”

These types are legitimate **now** for reconstruction and ops. They are **illegal as raw ML columns** without an as-of cut:

| Type / field | Why it leaks if taken “current” |
|---|---|
| `TraderScore.CompletedXauTrades` | After trade 23 this is 23+, not 3. |
| `TraderScore.CurrentState` | `SHADOW` / `LIVE` / `DISQUALIFIED` encodes **later** decisions and outcomes. |
| `TraderScore.LastScoredAt` / latest scores | Scores computed after more trades. |
| `Mt5Account` group / later snapshots | Challenge → funded group change is “eventual pass/fail” (§20). |
| `ReconstructedTrade` rows with `OpenedAt` after t3 | Trade #4 onward. |
| Lifetime `WasAveragedDown` / `WasScaledIn` / `MaxVolume` | Must be computed on trades 1–3 only (or 1–N for a later sample). |
| Destination `Shadow*` P&L “to date” | Contains the label horizon if the account was shadowed. |
| Live allocation | Selection / sizing after the decision point. |

---

## 6. The label (Phase 6 contract — do not implement yet)

### 6.1 One sample, one decision time

**Primary sample (architecture §19):**

```text
entity key:        (broker_id, login)          -- never login alone (§10)
sample id:         (broker_id, login, n=3)
decision time t*:  closed_at of completed XAUUSD lifecycle #3
X:                 features with as_of <= t*
y:                 label from copy-book of lifecycles #4..#23
```

A “trade” is **one completed reconstructed XAUUSD position lifecycle** (§15). Do not count order placement, deal fill, partial close, SL change, or TP change as a trade.

`TradeReconstructor.CompletedXauUsdTrades` already orders by `ClosedAt` then `OpenedAt`. That order is the only legal numbering for #1, #2, #3, … when Phase 6 starts. Do not renumber by profit, lot, or ticket.

### 6.2 Official `y` (binary, initial)

```text
label = 1  iff  future_net_copy_pnl > 0
               AND future_max_drawdown <= configured_limit

label = 0  otherwise, and only when the horizon is complete
           (see censoring below)
```

Definitions required before any `label` column is written:

| Term | Required definition | Forbidden substitute |
|---|---|---|
| Copy book | Shadow fills (Phase 5) priced on **cTrader QUOTE**, sized by the same converter live will use (§24, §38). Later: live fills, still destination-net. | Source `NetRealizedPnl`, entry/exit VWAP * volume, “points won.” |
| `future_net_copy_pnl` | Sum of copy-book net (after destination costs / modeled slippage) over reconstructed XAUUSD lifecycles **#4 through #23 inclusive**. | Lifetime source P&L; P&L of trades 1–3; P&L through “today.” |
| `future_max_drawdown` | Maximum peak-to-trough of the **copy-book equity curve** over that same 20-trade window, same clock as the fills. | Source account drawdown; challenge max DD; dashboard “current DD.” |
| `configured_limit` | A **versioned** constant chosen from Phase 5 measurement / backtest, stored on `model_versions` (or an explicit `label_spec_version`). | A magic number in a notebook. §23: do not hardcode before backtesting. |

`label_spec_version` is part of the dataset identity. Changing the DD cap, cost model, or horizon creates a **new** dataset, not an in-place overwrite.

### 6.3 What the label is not

| Wrong `y` | Why it is wrong |
|---|---|
| `sum(source net PnL of first 3 trades) > 0` | That is the quantity §3 says we are **not** selecting for. Also uses only the feature window, so it is not a future target. |
| `sum(source net PnL of trades 4–23) > 0` | **Research proxy only**, and only if named `proxy_source_future_pnl` and never promoted as production `y`. Source ≠ destination (costs, slippage, sizing, rejects). |
| Challenge pass / funded group / “still active today” | Explicitly banned as information (§20: final challenge result, eventual pass/fail). Survival-to-today also leaks as-of dataset-build time. |
| `TraderState == LIVE` or “was copied live” | Outcome of a later human/risk decision. Target leakage and selection leakage. |
| High Phase 3 score | The baseline is a **competitor**, not the label. Training to imitate it cannot beat it. |
| Accuracy-friendly rebalance of `y` that drops all censored rows without a protocol | See §6.4. |
| In-trade MFE/MAE sign | Path feature, not copy-horizon P&L. |

A **named proxy** (`proxy_source_future_pnl`, `proxy_source_future_dd`) may be used **after** reconstruction exists, for pipeline dry-runs only. It must:

1. Still obey the as-of-t3 feature cut and chronological split.
2. Stay out of the API (`mlProbability` stays null).
3. Never promote a model trained only on the proxy (§71: no automated self-promotion; promotion is a SuperAdmin act after OOS proof on the **official** label).

Until reconstruction + shadow exist, even the proxy is unavailable.

### 6.4 Censoring (must not become silent `label=0`)

Many accounts will not have 23 completed XAUUSD lifecycles. Treating “horizon incomplete” as `label=0` trains the model to predict **“this account died or is young”**, which correlates with calendar time, group, and broker — leakage and the wrong objective.

Required label states when Phase 6 builds the dataset:

| State | Condition | Use in training? |
|---|---|---|
| `COMPLETE_POS` | 20 subsequent completed XAUUSD lifecycles exist; copy-book PnL > 0 and DD ≤ limit | `y=1` |
| `COMPLETE_NEG` | Horizon complete; not `COMPLETE_POS` | `y=0` |
| `CENSORED_SHORT_HORIZON` | Fewer than 20 subsequent completed XAUUSD lifecycles, account still open / not blown on copy book | **Exclude** from binary train, or use a documented later survival/horizon model. Do not code as 0. |
| `CENSORED_NO_COPY_BOOK` | Reconstruction exists but shadow/live copy P&L missing for #4–#23 | **Exclude**. Do not fall back to source PnL silently. |
| `INVALID_RECONSTRUCTION` | First-3 numbering or symbol map failed | **Exclude.** |

Counts of each state are part of the dataset report. A 95% `CENSORED_*` rate means Phase 6 is still not justified.

### 6.5 Rescoring samples (later than #3)

§22 allows samples at n = 3, 4, 5, … Features at sample *n* may use completed lifecycles **1..n** only. The matching official horizon is the **next 20** completed XAUUSD lifecycles after *n* (n+1 .. n+20), not a second look at 4–23 once n > 3.

If both n=3 and n=8 samples for the same `(broker_id, login)` are in the dataset:

- Features of n=8 include trades that are **inside the label window** of n=3.
- A random split that puts n=3 in test and n=8 in train **leaks the test label**.

Rule: overlapping-horizon samples from the same trader must not cross the train / validation / test cut. Prefer: **one primary sample per trader** (n=3) for the official §19 model; treat later-n rows as a separate experiment with an embargo (see §7.4).

---

## 7. Chronological split (Phase 6 contract)

### 7.1 What is ordered

Order samples by **decision time** `t*` = `closed_at` of the completed XAUUSD lifecycle that defines the sample (trade #3 for the official model).

```text
sort samples by t* ascending
oldest 70%  → train
next 15%    → validation  (early stopping, calibration, hyperparameters)
newest 15%  → final test  (untouched until one locked evaluation)
```

Ties: `(t*, broker_id, login)`. Persist the exact membership lists (ids + `t*`) with the dataset hash. Re-running the splitter must be bitwise identical.

### 7.2 What is not a chronological split

| Procedure | Why it is illegal |
|---|---|
| `sklearn.model_selection.train_test_split(..., shuffle=True)` | Mixes future decision times into train. |
| Stratified-by-label random split | Same, plus it uses `y` to balance folds (not itself a leak into X, but destroys time order). |
| Split by `login` hash / broker | Contemporaneous test traders share the **future market** that train labels already saw; also leaks any global target encoding. |
| Split by account **registration** time | A 2023 account whose trade #3 closed in 2026 is a **2026** decision. |
| Split by last-trade time or “data dump time” | Uses the future. |
| K-fold CV over all rows | Every fold trains on the future of another fold. |
| Expanding-window CV that still reports the **final test** during search | Final test is no longer untouched. |
| Putting a trader’s **later** sample in train and an **earlier** sample in test | Direct label leakage (§6.5). |
| Fitting scalers, imputers, target encoders, or rare-group maps on train+val+test | Leakage of test marginals into X. Fit on **train only**; freeze; apply forward. |
| Early-stopping or model selection on the 15% final test | The test is then validation. |

### 7.3 Why time order matters here

XAUUSD regimes, spread, and prop-firm populations move. A random split lets the model “see” 2026 gold behavior while scoring a 2024 trade-#3. Top-N P&L on a leaked test is not copyable live.

The newest 15% is the only split that resembles production: **train on the past, score the next traders who just printed trade #3**.

### 7.4 Embargo / purge (required when any label window can overlap a later `t*`)

Architecture example does not name an embargo. §20’s “never expose future information” still requires one if label windows are long (20 trades can span weeks to months).

When Phase 6 builds the official n=3-only dataset:

- If trader A’s label window (trades 4–23) ends at `t_end_A` and trader B’s `t*_B` is before `t_end_A`, B’s **features** are still legal (cut at `t*_B`).
- The **market path** that generated A’s label is not an input column, so this is not a direct X-leak.
- It **is** a shared-regime coupling. Accept it for the simple 70/15/15; do **not** “fix” it by feeding A’s copy P&L or A’s later trades into anyone’s features.

When later-n samples exist:

```text
embargo: drop from train any sample whose label window
         overlaps a validation/test sample from the same (broker_id, login)
```

Safer default: **n=3 only** for the model that can be promoted.

### 7.5 Split unit is the sample, not the deal

Do not split deals, ticks, or daily bars. Those are feature **sources** inside `as_of <= t*`. The ML row is one trader-decision.

Broker is a **feature or a stratum for reporting**, not a split key. Evaluate §21 metrics **overall and per broker** (Achiever vs StarwaveFX) on the same time cut so a model cannot win by fitting one server’s group scheme.

### 7.6 Calibration split

§67 Phase 6 requires **probability calibration**. Fit isotonic / Platt **on validation only**, after the booster is frozen. Never calibrate on final test. Never calibrate on train if the reported probability is the production `confidence` — that over-confidences in-sample.

Until Phase 6 is open: no calibrator object in the repo.

---

## 8. What would leak

This is the Phase 6 hazard list. Anything in the left column entering `X` (or the split, or the promotion decision) is a defect.

### 8.1 Explicit §20 bans

| Forbidden input | Typical accidental source in *this* repo |
|---|---|
| Final challenge result | `mt5_groups` / plan map after they moved to a funded group; comments; dashboard “passed.” |
| Future balance / equity | `mt5_account_snapshots` rows with `captured_at > t*`. |
| Future drawdown | Lifetime DD computed through today; challenge max-DD fields updated later. |
| Trade #4 onward | Aggregating **all** `ReconstructedTrade` rows for the login; “lifetime” win rate, PF, hold time, lot stats. |
| Eventual pass/fail | Same as challenge result; also `DISQUALIFIED` / blown-account state. |
| Future market information | Ticks, bars, or cTrader quotes with `ts > t*` used to build t3 MFE/MAE, “volatility,” or indicators. Peeking at the path of trades 4–23. |

### 8.2 Target / selection leakage

| Leak | How it would appear |
|---|---|
| `future_net_copy_pnl` (or source proxy) as a feature | Column join “for sanity.” |
| Current `shadowPnl` / `liveAllocation` | Leaderboard fields (A26) reused as X. |
| `TraderState` in {`SHADOW`,`LIVE_CANDIDATE`,`LIVE`,`DISQUALIFIED`} | Encodes later gates and outcomes. At t3 the legal state is `EARLY_SCORE` (or still `INSUFFICIENT_DATA` if miscounted). |
| “This login was selected for shadow/live” | Treatment leakage; the model learns the old policy. |
| Training to predict Phase 3 score | Circular; cannot beat the baseline. |
| Using test-set top-N to pick the model | Test is no longer untouched. |

### 8.3 As-of / aggregation leakage

| Leak | Correct as-of rule |
|---|---|
| Latest `TraderScore` as X | Use snapshot at `completed_trade_count = 3` (or *n*). |
| Lifetime martingale / averaging-down / lot-escalation flags | Recompute flags on lifecycles 1–3 only. |
| `MaxVolume`, `DealCount`, `WasScaledIn` over the whole account | Same cut. |
| `CompletedXauTrades` as of dataset build | Constant `3` for the official sample (or *n* for a rescoring sample). |
| SL/TP “final” values on later trades | Only finals of trades 1–3. |
| Open position at dataset build | Ignore anything still open after t3; do not wait for it to close to “complete” a t3 feature. Trade #3 is already closed by definition. |
| Deal revisions applied after t3 that change trades 1–3 | Reconstruction must be **point-in-time**. If the raw ledger is immutable (§11), replay deals with `DealTime <= t*` only. |

### 8.4 Price / MFE / MAE leakage (and fabrication)

A17 is binding. If Phase 6 includes excursion features:

| Action | Result |
|---|---|
| MFE/MAE from `{entry, exit}` of a closed deal | Fabrication. Forbidden. |
| MFE/MAE from cTrader QUOTE while labeling the **source** trade | Wrong book. Only legal as `price_source=CTraderQuoteSession`, `feature_quality=Approximate`, never `Exact`. |
| MFE/MAE from `GetChart` bars | `BarApproximation` / `Approximate` only. |
| Using ticks after `closed_at` of that lifecycle | Future market. |
| Mixing Achiever ticks into a Starwave trade (or the reverse) | Silent mix. Forbidden. |
| Using session `bid_high` / `bid_low` as the trade window | Wrong interval; fabrication. |
| Leaving MFE/MAE null when the tape is missing | **Correct.** `feature_quality=Unavailable`. |

Until a labeled source tape exists, Phase 6 must **omit** MFE/MAE rather than invent them. Baseline §18 may list MAE/MFE as *optional* inputs subject to §17 — optional means droppable, not faked.

### 8.5 Split / preprocessing / evaluation leakage

Already listed in §7.2. Additional Phase 6-specific:

| Leak | Rule |
|---|---|
| Hyperparameter search that peeks at final test | Search on train; lock on validation; **one** test report. |
| Reporting accuracy / F1 as the go-live metric | §21: top-N copy economics. Accuracy is allowed as a footnote only. |
| Dropping losers from the test set | Selection bias. |
| Training only on traders who later reached 23 trades, then scoring everyone at trade #3 | Train population ≠ production population (survivorship). Document the eligible set; consider a two-stage “will they reach horizon / if so, label” **later** — not now. |
| Target-encoding `group` or `login` on full data | Future mean of `y` enters X. |
| Using `login`, deal-ticket, or increasing account age-at-dump | IDs encode recency and broker scheme. Age **at t*** is legal; age-at-dump is not. |

### 8.6 Pipeline / serving leakage

| Leak | Rule |
|---|---|
| Scoring inside the MT5 callback | §12. Persist raw → outbox → later `ScoreUpdate`. |
| Model writes FIX / sizes `OrderQty` | §39. Output triple only. |
| Auto-promote best validation AUC | §71. Human + audit. If ML loses to baseline, **do not promote**. |
| Dashboard shows a made-up probability | A26: `null` until a promoted model exists. |
| Training on live production Postgres without a frozen extract | Rows change; as-of broken. Freeze a dated parquet/table extract. |
| Using other traders’ **later** outcomes as a same-day “flow” feature | Future market + others’ labels. |

### 8.7 What would leak **if we trained this week** (concrete)

If someone started XGBoost on the tree as it exists on 2026-08-18, the only available numeric columns are roughly: in-memory reconstructed deal aggregates (if they hand-fed a dump), current account fields, and latest score stubs. That matrix would leak **all** of:

1. No as-of cut (everything is “now”).
2. No official `y` (no shadow book) — they would use source PnL or challenge outcome.
3. No chronological split (too few real rows; temptation to shuffle).
4. Fabricated or omitted-but-imputed MFE/MAE.
5. Survivorship (whoever is still on the manager today).
6. Group/plan labels that already encode pass/fail.
7. No baseline comparison.

That experiment would not be Phase 6. It would be a contaminated notebook. Do not check it in as a model.

---

## 9. Phase 6 deliverables (later — freeze the checklist, do not build)

Copied from §67 Phase 6 and A28. All stay `[ ]`.

```text
[ ] training dataset
[ ] chronological split
[ ] XGBoost
[ ] probability calibration
[ ] top-N evaluation
[ ] comparison against deterministic baseline
```

### 9.1 Suggested proof (A28)

```text
[ ] Dataset built from reconstructed, reconciled, de-duplicated trades
[ ] Split is chronological (no leakage from future into past)
[ ] Model compared to Phase 3 deterministic baseline — must beat it on agreed metric
[ ] Calibration reported (not only AUC)
[ ] Top-N vs baseline ranking documented
[ ] Model output cannot send an order; risk engine remains the gate (§72.15)
[ ] No automated model self-promotion (§71)
[ ] No LLM, deep learning, or RL substituted for this baseline (§71)
```

### 9.2 Exit criteria (A28)

```text
[ ] All six §67 Phase 6 delivers are evidenced
[ ] Quality vs deterministic baseline is measured, not assumed
[ ] If ML does not beat baseline, baseline remains the production scorer
```

### 9.3 Dataset contract (when — not now — the builder is written)

Each frozen extract must record:

```text
dataset_id
label_spec_version
feature_schema_version
as_of_rule            = "closed_at of completed XAUUSD lifecycle n"
n                     = 3 for the official model
horizon               = n+1 .. n+20
y_definition          = official copy-book | named proxy (proxy cannot promote)
configured_limit      + how it was chosen
row counts by label state (COMPLETE_POS / COMPLETE_NEG / CENSORED_*)
split membership hashes (train / validation / test)
price_source mix + feature_quality mix  (must not be silently mixed)
reconstruction git SHA + replay command
shadow cost model version (if official y)
baseline score version used as competitor
```

A20 tables to persist **results**, not to train inside the trading DB:

| Table | Role |
|---|---|
| `trader_feature_snapshots` | As-of X (Phase 3 writes these; Phase 6 only reads) |
| `model_versions` | Artifact + metrics + `is_production`; promotion audited |
| `model_predictions` | One row per `(model_version_id, broker_id, login, completed_trade_count)` |
| `model_evaluations` | OOS top-N vs baselines; `evaluation_split ∈ {validation, final_test, live_shadow}` |

Do **not** create empty `model_*` tables now just to look ready. §66: adapt to the existing repo; do not create duplicates unnecessarily. Phase 3 snapshots are the first scoring table that is actually needed.

### 9.4 Serving contract (when a model exists)

```text
outbox ScoreUpdate
    → IScoringService
    → candidate, confidence, suggested allocation
    → persist trader_scores + history + model_predictions
    → risk engine (unchanged)
```

- `confidence` is the **calibrated** P(label=1), or null if the model is not promoted.
- API `mlProbability` stays null until promotion (A26).
- §62 `ML_UNAVAILABLE`: do not promote new traders to live; ingestion continues; hard limits stay on (A23).
- High ML score cannot skip `RISK_BLOCKED` / `DISQUALIFIED` (A27 `Scoring.RiskBlockedAndDisqualifiedTransitionsTests` / `Risk.ScoringCannotBypassRiskTests`).

Stack **when** justified (§5): Python, FastAPI, XGBoost, scikit-learn, Polars, NumPy. Optional later: MLflow. **No** LLM API.

### 9.5 Evaluation contract (the only justification)

On **final test** (and separately on a later live-shadow window), compute §21 tables for:

```text
ML top 1 / 5 / 10 / 20%
Phase 3 early_quality_score top-N
simple rules baseline
highest historical source P&L baseline     -- expected to lose; still required
highest win-rate baseline
random (mean ± band over documented seeds)
all traders (universe mean)
```

Metrics: net **copied** P&L, max DD, profit factor, return volatility, CVaR, trade count, execution cost, slippage sensitivity.

**Promote only if** ML top-N copy economics beat the Phase 3 baseline **and** the simple-rules baseline on the locked test, with calibration reported. AUC alone is not a promote reason. Accuracy is not a promote reason.

If the test is small, **do not promote**. Lack of power is not a win.

---

## 10. What not to build even after Phase 6 opens

| Forbidden | Authority |
|---|---|
| Deep neural net as the first model | §19, §71 |
| Reinforcement learning, LLM ranker, “agent trader” | §71 |
| Automated model self-promotion | §71 |
| ML path that sends `NewOrderSingle` or bypasses risk | §39, §72.15, A28 [DO NOT] |
| Training / heavy predict in the Manager callback | §12, §72.6 |
| Fake `mlProbability` so the dashboard looks complete | A06, A26, §69 |
| Kafka / ClickHouse feature store “for ML scale” | §71 |
| Using destination quotes as if they were source ticks | §17, A17, A24 |
| Hardcoded live-capital gate at trade #3 | §23, §72.16 |
| Treating three winning source trades as `PROVEN_PROFITABLE` | §15 |

Phase 6 does **not** unlock Phase 8. Live `NewOrderSingle` stays behind §68 / §70 / `REAL_COPY_EXECUTION_ENABLED`.

---

## 11. Honest status vs a false PASS

| Claim | True measured state |
|---|---|
| “EX5 / ML fully ready” | **False.** There is no model, no dataset, no split. |
| “We can train a quick XGBoost on MT5 deals” | **False as Phase 6.** Deals are not trades; source PnL is not copy PnL; no as-of cut. |
| “Enums for FeatureQuality mean features exist” | **False.** Enums are unused. |
| “TraderScore means a baseline exists” | **False.** Type only; no calculator, no tests, no persisted scores. |
| “IsEarlyScoreEligible means labels exist” | **False.** It is a count ≥ 3, which is **eligibility**, not `y`. |
| “Phase 6 can start in parallel with Phase 3” | **False.** No baseline to beat; no snapshots; violates A28 sequencing. |
| “Leaving ML unbuilt is a gap we should close now” | **False.** It is compliance with §1, §18, §67, §69. |
| First useful version | Phases 0–5 + React. **No ML** (§69). |

Prefer a false negative (no model) over a leaked PASS.

---

## 12. Work that *is* allowed before Phase 6 (and is not this agent)

These belong to **other phases**. They are listed so Phase 6 is not used as an excuse to skip them — and so nobody “starts ML” under their name.

| Phase | Work that unblocks a *future* honest model |
|---|---|
| 1 | Immutable deals/orders/positions; checkpoints; no callback scoring |
| 2 | Persist reconstructed XAUUSD lifecycles; first-3 counter; replay tests |
| 3 | Deterministic features **as-of n**; risk flags; baseline scores; dashboard with `mlProbability=null` |
| 4 | Destination quote tape (shadow pricing, **not** source MFE) |
| 5 | Shadow copy book = official label substrate; measure costs/slippage; enough completed 4–23 horizons to estimate censoring |

A52 does **not** implement any of the above.

---

## 13. Cross-references

| Artifact | Use |
|---|---|
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§19–21, 67 Phase 6 | Law |
| `D:\Prop\reports\swarm\20260818\A28_phases_gates.md` | Phase 6 checklist / §69 bar |
| `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` | `trader_feature_snapshots`, `model_*` keys |
| `D:\Prop\reports\swarm\20260818\A17_ticks_and_ledger.md` | Why MFE/MAE would be fabricated today |
| `D:\Prop\reports\swarm\20260818\A24_shadow_copy_spec.md` | Destination-net P&L definition |
| `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md` | `ML_UNAVAILABLE`; score cannot send |
| `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` | `mlProbability: null` |
| `D:\Prop\reports\swarm\20260818\A27_test_inventory.md` | No XGBoost/leakage suites as a §69 gate |
| `D:\Prop\reports\swarm\20260818\A02_application_audit.md` | `IScoringService` missing (baseline now, ML later) |
| `D:\Prop\reports\swarm\20260818\A11_solution_coverage.md` | `/services/ml-service` not created |

---

## 14. Close

**Phase 6 is closed.** Architecture 19–21 are a specification for a later challenger model, not a license to train.

- **Why not now:** no clean as-of features, no official copy-horizon labels, no chronological population, no baseline to beat, data quality unproven, first useful version does not need it.
- **Label:** `y=1` only if **execution-venue-net** P&L of reconstructed XAUUSD trades **#4–#23** is positive **and** copy-book max DD ≤ a versioned limit; incomplete horizons are censored, not zero; source PnL and challenge pass/fail are not `y`.
- **Split:** oldest 70% / next 15% / newest 15% by **trade-#3 close time**; train-only preprocessing; overlapping same-trader horizons must not cross cuts; final test is untouched.
- **What would leak:** anything after `t*`, lifetime aggregates, current state/allocation/group, fabricated MFE/MAE, destination quotes posing as source ticks, shuffled splits, target encoding, test-set model selection, and any notebook trained on this week’s tree.

Do not add `/services/ml-service`, XGBoost, or `model_*` migrations until Phase 5 has exited and a reviewer can point at a frozen, leakage-checked extract. Until then the production scorer — when Phase 3 exists — is the deterministic baseline.

**Product source was not modified.**
