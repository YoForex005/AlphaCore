# A60 — Copy Correlation / Concentration (Architecture §65) — Phase 2 Hooks Only

**Artifact:** `D:\Prop\reports\swarm\20260818\A60_correlation_phase2.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §65  
**Supporting law:** §14, §17–§18, §22–§24, §32–§41, §44–§45, §53, §57–§60, §62–§64, §67 Phase 8, §68, §70–§72  
**Sibling specs (do not contradict):**  
- `A20_table_catalog.md` — 47-table v1 catalog; this document only *names* future tables  
- `A22_scoring_spec.md` — feature fields reused as correlation inputs  
- `A23_risk_engine_spec.md` §5 step 13 and §10 — reserved concentration slot  
- `A24_shadow_copy_spec.md` — observe-only clustering on shadow book  
- `A26_dashboard_api_spec.md` — v1 Risk page; cluster tiles are additive  
- `A27_test_inventory.md` — v1 risk tests; cluster tests listed here as **future**  
- `A28_phases_gates.md` — Engineering Phase 0–8; this is **not** Engineering Phase 2  
**Date:** 2026-08-18  
**Agent:** A60  
**Status:** specification of **future hooks only**. **Do not implement now.**  
**Constraint:** no product source was modified. No migrations. No new C# / React / SQL files.

---

## 0. Binding standing of this document

| Rule | Binding meaning |
|---|---|
| **Do not implement now** | No `StrategyCluster` types, no correlator job, no `CONCENTRATION_CAPS_ENABLED` wiring, no extra tables, no dashboard cluster tile in current work. |
| **Do not block v1** | First live path must not refuse copy because a correlation graph is missing. |
| **Do not invent a second risk engine** | When this phase is later switched on, concentration lives **inside** the existing risk engine (same persist-before-send, same `risk_decisions`). |
| **Do not put clustering on the FIX / MT5 hot path** | Pairwise / graph work is offline or batch. Risk only *reads* a versioned membership + exposure snapshot. |
| **Do not confuse two “Phase 2” labels** | See §1. |
| **Do not confuse two “correlation” words** | See §1.3. |
| **Do not hardcode production caps** | `max_allocation_per_cluster` is configuration, measured later, audited like every other §39 limit. |

This file exists so later implementation has named insertion points. Until Engineering Phase 8 is stable and the §2 exit criteria are true, every hook below is **reserved, unused, default-off**.

---

## 1. Name collision — read this first

### 1.1 Architecture §65 “Phase 2” ≠ Engineering Phase 2 (§67)

| Label | Authority | Meaning | When |
|---|---|---|---|
| **Engineering Phase 2** | Architecture §67, `A28` | XAUUSD reconstruction (mappings, reconstructed trades, first-3 counter, unit tests) | After Engineering Phase 1 ingestion |
| **§65 “Phase 2” (this document)** | Architecture §65 last sentence | Concentration / correlation **after basic copy execution is stable** | After Engineering **Phase 8** works, kill switches work, sizing/stale guards proven, §68 + §70 checked |

Architecture §65, quoted in full (normative):

```text
Do not copy 50 "different traders" if they are effectively the same XAUUSD strategy.

Track correlation by:

    direction
    entry time
    holding time
    return series
    session
    lot behavior

Add concentration caps.

Example:

    maximum allocation per correlated strategy cluster

This can be Phase 2 after basic copy execution is stable.
```

`A23` §10.1 already records the same disambiguation. This document expands it into hooks. It does **not** reopen Engineering Phase 2.

Suggested later schedule name (when work is actually queued):

```text
Engineering Phase 9 — Cluster concentration   (optional; not in §67 today)
```

Do not invent Phase 9 in `A28` until this work is scheduled. Until then call it **“§65 post-copy concentration”**.

### 1.2 What “basic copy execution is stable” means

Not “we can compile a CopyIntent.” All of the following must already be true (`A23` §10.3, `A28` §68 / §70):

```text
[ ] copy intents are idempotent
[ ] stale quote rejection is tested
[ ] stale signal rejection is tested
[ ] sizing conversion is verified (source lots ≠ destination qty)
[ ] kill switches are tested (STOP_NEW_EXECUTION ≠ EMERGENCY_FLATTEN)
[ ] shadow sample exists and destination costs/slippage are measured
[ ] risk-engine rejection happens before FIX send
[ ] unknown-state recovery works (no blind retry)
[ ] reconciliation blocks execution while inconsistent
[ ] REAL_COPY_EXECUTION_ENABLED remains an explicit production flag
```

Concentration clustering is **not** a v1 go-live checkbox (`A23` §11.3, `A28` go-live list).

### 1.3 Two different “correlation” words

| Word | Meaning | v1 status |
|---|---|---|
| `correlation_id` | Request / pipeline log identifier (§57, `A26` `X-Correlation-Id`) | **v1 — implement with logging** |
| Strategy correlation | “These N source logins are the same XAUUSD strategy” (§65) | **Phase 2 — this document** |

Never overload `correlation_id` as a cluster id. Cluster identity is `strategy_cluster_id` (UUID) plus a `cluster_run_id` / `membership_version`.

---

## 2. Problem this phase exists to solve

v1 risk (`A23` §6.4) can still approve 50 separately-selected “different” traders who:

- open XAUUSD in the same direction,
- within the same few seconds,
- hold for similar durations,
- print nearly identical return paths,
- trade the same session,
- size lots the same way.

Book-level `max XAUUSD gross/net` and per-trader `max loss` **bound the account** if they all fire together. They do **not** distinguish “50 independent edges” from “one signal with 50 logins.” That is the §65 failure mode.

v1 substitute (keep; do not remove when Phase 2 lands):

| v1 control | What it does | What it does not do |
|---|---|---|
| `max XAUUSD gross / net` | Caps destination book | Treats 50 clones as 50 traders |
| `max loss per selected trader` | Caps one `(broker_id, login)` | Misses the cluster |
| `max number of open positions` | Caps count, not thesis | A clone army still shares one thesis |
| `martingale` / `abnormal sizing` flags | Per-trader behavior | Does not group lookalikes |
| `suggested_allocation` from scoring | Advisory only | Scoring must not become the cap |

Phase 2 adds **one** extra hard limit:

```text
maximum allocation per correlated strategy cluster
```

---

## 3. Non-goals (now and when implemented)

### 3.1 Do not implement now

```text
[DO NOT] Create strategy_clusters / membership tables
[DO NOT] Write a correlator job or pairwise similarity service
[DO NOT] Call clustering from RiskEngine
[DO NOT] Reject live or shadow copy for missing cluster_id
[DO NOT] Add CONCENTRATION_CAP to any v1 test as an expected production reason
[DO NOT] Add a dashboard “Clusters” nav item that implies the feature exists
[DO NOT] Train XGBoost / LLM / embeddings to invent clusters
[DO NOT] Put Kafka / a mesh / ClickHouse under this feature (§71)
[DO NOT] Modify product source to “leave a stub that compiles against nothing”
```

A reserved reason-code **name** in `A23` §4.3 is documentation, not a license to ship the check.

### 3.2 Do not do later either (unless a later spec revises this)

| Forbidden | Why |
|---|---|
| ML / scoring bypasses concentration | §39, §72.15 — risk is the authority |
| Clustering inside an MT5 callback or FIX send path | §32, §72.6 — persist/queue; heavy work is offline |
| A second filter *after* `ApprovedExecutionIntent` that can veto a persisted send | Persist-before-send is already the gate; do not add a bypassable sidecar |
| Blocking `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` on a cluster cap | §64 — reducing risk is a different policy |
| Using destination cTrader quotes as source return series | §17 — do not mix price sources |
| Treating `login` as globally unique when grouping | §10 — identity is `(broker_id, login)` |
| Using log `correlation_id` as cluster identity | §1.3 |
| Auto-promoting a cluster to live because it “looks diversified” | Promotion remains a trader-state + shadow-sample gate (`A22`, §23) |
| Hardcoded production ounces / dollars in source | §23, `A23` §6 |

---

## 4. Phase 2 intent (design only)

When scheduled, add a concentration **layer inside** the risk engine:

```text
CopyIntent
    ↓
RiskEngine steps 1–12          ← unchanged v1
    ↓
step 13 Concentration          ← NEW, flag-gated, default OFF
    ↓
persist risk_decision
    ↓
ApprovedExecutionIntent        ← only if still APPROVE / REDUCE_SIZE
```

`A23` §5 already reserved slot 13:

> 13. **Concentration** — **not in v1**. Reserved Phase 2 (§10).

This document is the contract for that slot.

### 4.1 Who computes clusters vs who enforces caps

| Role | Process | Latency budget | May call ML? |
|---|---|---|---|
| **Strategy correlator** (batch / offline) | Build feature vectors, pairwise scores, connected components, write membership | minutes–hours | No requirement. Prefer deterministic features. If a model is later used to *propose* merges, a human or a rules gate still writes the membership row. |
| **Concentration policy** (in-process, risk) | Read `strategy_cluster_id` + current cluster gross/net + `max_allocation_per_cluster` | same as other book caps | **Never** |

The correlator is **not** on the send path. The risk engine does not recompute Pearson / DTW / graphs per intent.

### 4.2 Exposure class (reuse §64)

| Class | Concentration applies? |
|---|---|
| `OPEN_EXPOSURE` | **Yes** |
| `INCREASE_EXPOSURE` | **Yes** |
| `REDUCE_EXPOSURE` | **No** (still needs identity + venue health) |
| `CLOSE_EXPOSURE` | **No** |

Reversals stay two events (`CLOSE` then `OPEN`). Only the `OPEN` half can hit `CONCENTRATION_CAP`.

### 4.3 Decisions the layer may emit

Reuse `A23` §4.1. No new decision enum.

| Situation | Decision | `primary_reason` |
|---|---|---|
| Remaining cluster budget ≥ proposed destination qty | `APPROVE` (or keep prior `REDUCE_SIZE` from an earlier cap) | unchanged |
| `0 < remaining < proposed` and remainder ≥ destination min | `REDUCE_SIZE` | `CONCENTRATION_CAP` (or `SIZE_REDUCED_TO_LIMIT` + `CONCENTRATION_CAP` as secondary) |
| Remainder < destination min | `REJECT` | `CONCENTRATION_CAP` |
| Flag ON and cluster graph stale / missing when policy is fail-closed | `REJECT` | `CLUSTER_GRAPH_STALE` *(Phase 2 code; not in v1)* |
| Flag **OFF** | skip the step | never `CONCENTRATION_CAP` |

`PAUSE_TRADER` is **not** the default response to a cluster cap. A cap is book-structure, not “this login is toxic.” An operator may still pause a login separately.

### 4.4 Reason codes (reserved)

Already reserved in `A23` §4.3:

| Code | v1 | Phase 2 |
|---|---|---|
| `CONCENTRATION_CAP` | listed, **unused** | blocking / reduce reason |
| `CLUSTER_GRAPH_STALE` | **do not add to v1 enums** | only if flag ON and fail-closed-on-stale is chosen |
| `CLUSTER_MEMBERSHIP_MISSING` | do not add in v1 | optional; prefer treat-as-singleton (see §8.3) |

Until Phase 2 ships, tests must **not** expect `CONCENTRATION_CAP` on any live fixture.

---

## 5. Correlation dimensions (normative feature contract)

Track correlation by the six §65 axes. Map each axis onto fields that **already** exist (or are already specified) so Phase 2 does not invent a parallel trade store.

Identity of a source trader remains `(broker_id, login)` (`A20` §2). A cluster may span **both** source brokers. That is the point of “50 different traders.”

Universe: completed reconstructed XAUUSD lifecycles only (`A22` I1). Open positions may contribute to *live* cluster exposure, not to the historical similarity graph.

### 5.1 `direction`

| Source | Field |
|---|---|
| `reconstructed_trades.direction` | Buy / Sell (`A22` §3.1, architecture §14) |
| Live intent | `CopyIntent.side` / `exposure_class` |

Suggested cluster features (computed offline):

```text
long_frac          = count(direction == Buy)  / N
short_frac         = count(direction == Sell) / N
dominant_side      = argmax(long_frac, short_frac)
side_flip_rate     = adjacent side changes / (N-1)
```

Two traders who are 95%+ the same dominant side, flipping on the same clocks, are more similar than a long-only vs a short-only book.

**Hook:** reuse `ReconstructedTrade.direction`. Do not add a second direction enum.

### 5.2 `entry time`

| Source | Field |
|---|---|
| `reconstructed_trades.opened_at` | UTC timestamptz |
| Live intent | `source_event_time` / `collector_receive_time` |

Suggested cluster features:

```text
entry_tod_bucket     = UTC hour of opened_at   // same buckets as A22 §3.6
entry_sync_score(i,j)  = fraction of i's entries that have a j entry
                         within ENTRY_SYNC_WINDOW
```

`ENTRY_SYNC_WINDOW` is configuration (provisional discussion default: 5–30 seconds for “same signal,” 1–5 minutes for “same headline”). **Do not freeze a production number in code now.**

This axis is how “50 logins fire together” is detected. It is **not** the same as `signal_age` / `expires_at` (§63). Stale-entry rejection stays a v1 check and stays independent.

**Hook:** pairwise timestamps from `reconstructed_trades`; live sync can later use a short rolling window of recent `copy_intents.source_event_time` **read-only**.

### 5.3 `holding time`

| Source | Field |
|---|---|
| `A22` derived `hold_sec_i` | `Max(0, closed_at - opened_at)` |
| Snapshot | `trader_feature_snapshots.hold_cv` |

Suggested cluster features:

```text
median_hold_sec
hold_p10, hold_p90
hold_cv                 // already in A22 snapshot
```

Similarity: same order of magnitude (scalp vs swing) plus similar CV. A 30-second scalper and an 8-hour hold are different clusters even if they share direction.

**Hook:** `DeterministicFeatureEngine` already emits hold stats. Correlator **reads** snapshots; it does not fork a second hold calculator.

### 5.4 `return series`

This is the strongest “same strategy” evidence and the easiest to get wrong.

| Allowed source | Forbidden source |
|---|---|
| Per-trade `net_realized_pnl` series on reconstructed XAUUSD, in stable `A22` order | Destination shadow P&L as the *similarity* series |
| Optional **source** tick path only when `feature_quality == EXACT` (MFE/MAE shape) | Silent mix of Achiever ticks + Starwave ticks + cTrader quotes as one series (`A22` I7, §17) |
| Sign sequence of trade PnL (`+ + − + …`) | Raw dollar PnL without a scale (0.01 lot vs 1.00 lot) |

Suggested cluster features:

```text
signed_r_series[k]     = r_i or (pnl_i / vol_i) for last K completed trades
sign_series[k]         = sign(pnl_i)
equity_curve_norm      = cumsum(signed_r) / max(1, range)
```

Pairwise (offline only):

```text
pearson(signed_r_i, signed_r_j)     // same length, aligned by trade index or by time bins
sign_agreement
time_binned_return_corr             // e.g. 15-minute bins on days both traded
```

Do **not** require DTW / HMM / embeddings in the first correlator. Prefer Pearson + sign agreement + entry-sync. Complexity waits on measurement (`A28` / §71 / §72.20).

**Hook:** series built from `reconstructed_trades` via the same stable order as `A22` §2.2 (`closed_at, opened_at, id`). Persist the vector on `cluster_feature_snapshots` (future table), not inside `risk_decisions`.

### 5.5 `session`

| Source | Field |
|---|---|
| `A22` §3.6 UTC buckets | Asia / London / NY_overlap / Late |
| Snapshot | `session_max_frac` |

`A22` already says high session concentration is **not** a scoring defect. For §65 it is a **grouping** signal: two traders who both live in London-open XAU are more similar than London vs Asia.

Suggested cluster features:

```text
session_hist[4]        // fractions, sum = 1
session_max_frac       // already snapshotted
session_js_divergence(i,j)
```

**Hook:** same buckets as `A22`. Do not invent a second session clock. If session definitions are later retuned, bump `feature_schema_version` **and** `cluster_feature_schema_version` together.

### 5.6 `lot behavior`

| Source | Field |
|---|---|
| `vol_i = max_volume_i` | `A22` §3.2 |
| `lot_cv`, `vol_span`, martingale / revenge / escalation counts | `A22` §3.5–3.6 |
| Flags | `trader_risk_flags` (`MARTINGALE`, averaging-down, abnormal sizing) |

Suggested cluster features:

```text
lot_cv, vol_span
median_vol_canonical     // after source contract-size normalize (§38), so brokers compare
martingale_rate
scale_in_rate
avg_down_rate
```

Two martingale clones with the same step-up after a loss are one cluster even if they sit on different brokers.

**Hook:** reuse `A22` lot features + `SourceDestinationQuantityConverter` **only** to canonicalize volume for comparison. Do not use the converter’s destination `OrderQty` as a similarity feature (that is our sizing, not their strategy).

### 5.7 Feature vector (offline contract)

A future `ClusterFeatureVector` (name reserved) is the correlator input. Logical fields only:

```text
broker_id
login
as_of
n                              // completed XAU trades in window
window                         // EXPANDING | LAST_K | FIRST3   (FIRST3 is research-only)
feature_schema_version
cluster_feature_schema_version

direction:   long_frac, short_frac, side_flip_rate
entry:       session_hist, entry_tod_hist, recent_entry_times[]   // last K, for sync
hold:        median_hold_sec, hold_p10, hold_p90, hold_cv
returns:     signed_r_series[], sign_series[], equity_curve_norm[]
session:     session_hist[4], session_max_frac
lots:        lot_cv, vol_span, median_vol_canonical,
             martingale_rate, scale_in_rate, avg_down_rate

price_source
feature_quality                // do not mix EXACT with APPROXIMATE in one pairwise cell
```

`FIRST3` snapshots stay research windows (`A22`). Do **not** cluster live capital on three trades. Minimum `n` for a *merge* proposal is configuration (discussion floor: well above first-3; the live graph can still place a young trader in a **singleton**).

---

## 6. Clustering method (future; not implemented)

### 6.1 Preferred v1-of-Phase-2 algorithm

Deterministic, replayable, no learned weights required:

```text
1. Select eligible traders: reconstructed XAU n >= min_n, not DISQUALIFIED.
2. Build ClusterFeatureVector as-of T from durable stores only (A22 I6).
3. For each pair (i, j):
      skip if feature_quality mix is illegal
      score_dir, score_entry, score_hold, score_ret, score_sess, score_lot
      S(i,j) = weighted sum of the six  (weights in ClusterConfig, versioned)
4. Undirected edge if S(i,j) >= merge_threshold
   AND entry_sync_score >= sync_threshold   // prevents “same vibe, different clock”
5. Connected components → clusters
6. Persist:
      cluster_runs            (one row per as-of)
      strategy_clusters       (stable id when possible; see §6.3)
      trader_cluster_memberships
      cluster_feature_snapshots
7. Never write membership from the risk process.
```

Weights and thresholds live in `ClusterConfig` the same way `ScoreConfig` works (`A22` I12): versioned, not “optimal,” not hardcoded in conditionals.

### 6.2 What we are not picking now

```text
[NOT NOW] k-means / GMM with a guessed k
[NOT NOW] spectral clustering
[NOT NOW] deep embeddings / LLM “this EA looks like that EA”
[NOT NOW] online incremental clustering on each deal
[NOT NOW] treating source-broker as a cluster (that is riskBySourceBroker, already v1)
```

If measurements later show connected components over-merge, the revision is a new `cluster_feature_schema_version` + a new `cluster_runs` row — not a hot-path patch.

### 6.3 Cluster identity stability

Operators will stare at cluster UUIDs on the Risk page. Membership will churn. Rules:

| Rule | Why |
|---|---|
| Surrogate `strategy_cluster_id uuid` | Same as `A20` convention |
| A new run **reuses** an id when Jaccard(membership_old, membership_new) ≥ `retain_threshold` | Dashboard continuity |
| Otherwise mint a new id and retire the old (`retired_at`) | Do not silently rename a living cluster |
| Singleton traders still have a cluster id | Risk step 13 always has a key |
| Manual pin (`membership_source = MANUAL`) survives auto-rebuild unless explicitly unpinned | Audit + RBAC (`A26` / §59) |

### 6.4 Cross-broker clones

A strategy that is sold / copied onto Achiever **and** StarwaveFX is **one** cluster. Pairwise scoring is on features, not on `broker_id`. The compound identity stays on the membership row so a recycled login on another broker cannot inherit a pin.

### 6.5 Young traders / first-3

`A22` I2–I5: trade #3 is early evidence, never live capital. Clustering:

- May attach a young trader to a **singleton**.
- Must **not** auto-merge into a large live cluster on `n < min_n`.
- Must **not** use cluster membership as a promotion signal to `LIVE`.

---

## 7. Concentration cap (the only new hard limit)

### 7.1 Quantity the cap binds

Same units as other book caps: **destination XAU quantity** after `A23` §7 normalize/step, on the **execution account**.

```text
cluster_gross = Σ |destination qty| of open dest positions
                whose source (broker_id, login) ∈ cluster C

cluster_net   = Σ signed destination qty of those positions
                (sign = +long / −short)

proposed      = this intent's destination qty (already step-rounded)

remaining_gross = max(0, max_allocation_per_cluster_gross - cluster_gross)
remaining_net   = slack vs max_allocation_per_cluster_net
                  in the direction of this intent

binding = min(remaining_gross, remaining_net_slack, other_caps_already_applied)
```

Net vs gross: both should exist, mirroring v1 `max XAUUSD gross/net`. A cluster that is flat net but 10 + 10 long/short is still concentrated.

`max_allocation_per_cluster` in §65 is the **example** name. Concrete config keys (future):

```text
max_allocation_per_cluster_gross
max_allocation_per_cluster_net
```

Optional later refinement (not required to schedule the phase):

```text
max_traders_per_cluster_copied
max_notional_per_cluster
```

Do not ship those extra knobs until the two quantity caps are measured.

### 7.2 Interaction with earlier caps (step 11 then 13)

Order stays `A23` §5:

```text
11 book / account hard limits     → may REDUCE_SIZE
12 martingale / abnormal sizing   → reject / pause
13 concentration                  → may REDUCE_SIZE again or REJECT
```

Never *increase* a quantity that step 11 already reduced. Concentration only binds further.

`copy_allocations` remains the persist of the **final** destination qty (`A20` §5.6). Add a future column `binding_cap` value `CONCENTRATION` when step 13 bound the size (see §9.2).

### 7.3 Shadow vs live

| Path | Flag | Behavior when Phase 2 exists |
|---|---|---|
| v1 now | n/a | no cluster check |
| Shadow after Phase 2 lands | `CONCENTRATION_CAPS_ENABLED` may be **on for shadow only** | observe / simulate rejects; do not block Engineering Phase 5 |
| Live | default **off** until measured on shadow | same engine, same codes |

Shadow is the right place to learn whether the threshold is sane. It is **not** a reason to implement clustering during Phase 5.

---

## 8. Future hooks (named insertion points)

These are the only “implementation” this document allows: **names, seats, and contracts**. Nothing below is created in this change-set.

### 8.1 Feature flag and config (future env / options)

Add later next to `A23` / architecture §41 / §56 — **default off**:

```env
# future — do not add to production env files today
CONCENTRATION_CAPS_ENABLED=false
CONCENTRATION_CAPS_SHADOW_ENABLED=false
CONCENTRATION_FAIL_CLOSED_ON_STALE_GRAPH=true
MAX_CLUSTER_GRAPH_AGE=24h
```

Numeric limits live in the same audited limits document as §39 (dashboard `PATCH /api/v1/risk/limits` today — `A26` §6.12). Future keys on that document:

```text
controls.limits.maxAllocationPerClusterGross
controls.limits.maxAllocationPerClusterNet
controls.limits.maxClusterGraphAgeMs
```

v1 `GET /api/v1/risk/dashboard` must **not** grow these fields until the phase is scheduled. Adding unused keys now would imply the control exists.

`ClusterConfig` (correlator, not risk limits):

```text
cluster_feature_schema_version
min_n
merge_threshold
sync_threshold
retain_threshold
ENTRY_SYNC_WINDOW
weights.{direction,entry,hold,returns,session,lots}
```

Separate object from `ScoreConfig`. Bumping score weights must not silently retune clusters.

### 8.2 Domain types (reserved names)

When Engineering Phase 9 / §65 work is scheduled, proposed seats (match `A27` SUT style; **do not create files now**):

| Type | Layer | Role |
|---|---|---|
| `StrategyClusterId` | Domain | UUID wrapper |
| `ClusterMembership` | Domain | `(broker_id, login) → cluster_id + version + source` |
| `ClusterFeatureVector` | Domain / Scoring | §5.7 |
| `ClusterConfig` | Domain | versioned correlator config |
| `IStrategyCorrelator` | Application | batch rebuild |
| `IClusterMembershipStore` | Application | read current membership |
| `IClusterExposureReader` | Application | destination book rolled up by cluster |
| `IConcentrationPolicy` | Application / Risk | step 13 |
| `ConcentrationDecision` | Domain | qty out + reason; maps to existing `RiskDecisionOutcome` |

Do **not** add a new `RiskDecisionOutcome` value. `ReduceSize` / `Reject` already cover it (`src/Domain/Enums/RiskDecisionOutcome.cs` today).

Reserved reason-code names (documentation / future enum members, **not added now**):

```text
CONCENTRATION_CAP
CLUSTER_GRAPH_STALE
```

### 8.3 Risk engine hook (the load-bearing one)

File that will later change (not now): the future `RiskEngine` implementation specified by `A23`.

```text
// step 13 — PSEUDOCODE, not to be pasted into product source today
if (!opts.ConcentrationCapsEnabled)
    return prior;                    // v1 path; CONCENTRATION_CAP never emitted

if (intent.ExposureClass is Reduce or Close)
    return prior;

var graph = snapshot.ClusterGraph;   // assembled by application layer, like other inputs
if (graph is null || graph.AsOfAge > opts.MaxClusterGraphAge)
{
    if (opts.FailClosedOnStaleGraph)
        return Reject(CLUSTER_GRAPH_STALE);
    // else: treat trader as singleton (see policy note)
}

var clusterId = graph.Membership[intent.SourceBrokerId, intent.SourceLogin]
                ?? StrategyClusterId.SingletonOf(intent);

var exposure = snapshot.ClusterExposure[clusterId];
var remaining = Remaining(exposure, opts.MaxAllocationPerClusterGross/Net, intent.Side);

if (remaining <= 0)                  return Reject(CONCENTRATION_CAP);
if (remaining < proposed && remaining >= min)
                                     return ReduceSize(remaining, CONCENTRATION_CAP);
if (remaining < min)                 return Reject(CONCENTRATION_CAP);
return prior;
```

**Missing membership policy (lock this when implementing, not now):**

| Option | Behavior | Recommendation |
|---|---|---|
| A. Treat as singleton | Cap = the cluster cap applied to this one trader | Safe default if graph is fresh but a brand-new login appeared |
| B. Fail closed | `CLUSTER_MEMBERSHIP_MISSING` | Use only when flag ON **and** the run is supposed to cover 100% of `LIVE` traders |
| C. Skip cap | Equivalent to flag off for that intent | **Forbidden** — silent hole |

Recommended combination when the phase ships: graph stale → fail closed on OPEN/INCREASE; graph fresh + unknown login → singleton (option A).

### 8.4 Risk snapshot input (extend `A23` §3.4 later)

Add to the **assembled snapshot** the engine already consumes (not to MT5 callbacks):

```text
cluster_graph_as_of
cluster_run_id
cluster_id_for_this_trader
cluster_member_count
cluster_gross_qty
cluster_net_qty
max_allocation_per_cluster_gross
max_allocation_per_cluster_net
concentration_caps_enabled
```

v1 snapshot builders must **not** query future tables. When tables do not exist, the builder omits the block and step 13 is skipped because the flag is off.

### 8.5 Persistence hooks (`A20` additive catalog — future tables 48+)

Do **not** migrate these now. Do **not** bump the “47 tables” claim. When the phase is scheduled, add via versioned migrations (`§72.3`) **after** a short RFC against `A20`.

| Future # | Table | Side | Purpose | Suggested identity |
|---|---|---|---|---|
| 48 | `cluster_runs` | G | One rebuild | `id uuid` (`cluster_run_id`); `as_of`, `cluster_feature_schema_version`, `status` |
| 49 | `strategy_clusters` | G | Durable cluster | `id uuid`; `display_code` unique; `retired_at` nullable |
| 50 | `trader_cluster_memberships` | S | Current + history | current UNIQUE `(broker_id, login) WHERE unpinned_at IS NULL`; history append |
| 51 | `cluster_feature_snapshots` | S | Vector at run | UNIQUE `(cluster_run_id, broker_id, login)` |
| 52 | `cluster_pairwise_scores` | G | Optional debug / audit of edges | UNIQUE `(cluster_run_id, login_lo, login_hi)` — store `(broker_id, login)` pairs, not bare login |

`broker_id` law (`A20` §2) applies to every membership / vector row.

**Existing tables — future nullable columns only, never required in v1:**

| Table | Future column | Why |
|---|---|---|
| `risk_decisions` | `strategy_cluster_id`, `cluster_run_id`, `cluster_gross_before`, `cluster_qty_remaining` | explain `CONCENTRATION_CAP` |
| `copy_allocations` | `binding_cap` includes `CONCENTRATION` | sizing audit |
| `copy_intents` | none required | intent stays source-event identity |
| `execution_intents` | carry `strategy_cluster_id` like other §57 ids | logs / dashboard |
| `trader_feature_snapshots` | none required | correlator reads existing columns |
| `audit_logs` | `entity_type = strategy_cluster` | manual pin / unpin / limit change |

Do **not** unique-constrain `strategy_cluster_id` onto `copy_intents`. Many intents share one cluster.

### 8.6 Application / worker hook

Batch rebuild belongs next to scoring refresh, **not** inside `apps/fix-worker` or `apps/mt5-worker`.

Reserved later seat:

```text
/src/Application/Clustering/          # or /src/Scoring/Clustering
    StrategyCorrelator
    ClusterMembershipProjector
    ClusterExposureProjector

# optional scheduled host (not a new mesh)
/apps/api  hosted service   OR   existing worker with a distinct execute-on-timer
```

Trigger:

```text
on reconstructed trade close (enqueue, do not compute inline)
on operator "Rebuild clusters"
on schedule (e.g. after scoring refresh)
```

Idempotent on `(as_of, cluster_feature_schema_version)`. Crash after persist of `cluster_runs` must be resumable (`§72.7` spirit).

**Never** from:

- MT5 deal callback
- FIX `ExecutionReport` handler
- RiskEngine constructor / first request

### 8.7 Dashboard / API hooks (`A26` additive — do not add routes now)

v1 Risk page already shows `riskByCopiedTrader` and `riskBySourceBroker`. Phase 2 adds a third rollup.

Reserved later:

| Surface | Contract |
|---|---|
| `GET /api/v1/risk/dashboard` | additive `riskByCluster[]` `{ clusterId, displayCode, memberCount, longQuantity, shortQuantity, netQuantity, grossQuantity, capGross, capNet }` |
| `GET /api/v1/clusters` | list + filters |
| `GET /api/v1/clusters/{clusterId}` | members `(brokerId, login)`, last run, pairwise excerpt |
| `POST /api/v1/clusters/rebuild` | SuperAdmin / RiskManager; audited |
| `POST /api/v1/clusters/{id}/pin` | pin `(brokerId, login)` ; audited |
| `PATCH /api/v1/risk/limits` | the two new limit keys |
| SignalR | `risk.cluster` tile — same denylist as all other hubs (no secrets) |
| Rejected intents | already generic; will start showing `CONCENTRATION_CAP` when the check exists |

Nav: **do not** add a top-level §46 item until the phase is real. Until then a section **on** `/risk` is enough.

RBAC: rebuild / pin / limit change = `RiskManager` or `SuperAdmin`, same as other limit writes (`A26` §10). Viewer roles see cluster exposure read-only.

### 8.8 Logging and metrics hooks

Extend §57 identifier set **when the phase ships**:

```text
correlation_id                 // request — unchanged
strategy_cluster_id            // NEW
cluster_run_id                 // NEW
broker_id
source_login
...
```

Reserved metrics (`A07` / architecture §58 style):

```text
concentration_evaluations_total{decision}
concentration_rejects_total
concentration_reduce_size_total
cluster_graph_age_seconds
cluster_run_duration_seconds
cluster_member_count
```

Never log FIX/MT5 passwords. Cluster features are not secrets, but do not dump full pairwise matrices into info logs.

### 8.9 Shadow hook (`A24`)

When (and only when) the correlator exists:

- Shadow evaluation may run step 13 with `CONCENTRATION_CAPS_SHADOW_ENABLED`.
- Persist the would-be reason on the shadow decision / `shadow_performance` attribution.
- Do **not** require TRADE FIX.
- Do **not** use shadow P&L as the return series for clustering (§5.4).

### 8.10 Scoring / ML hook (`A22`, §19, §72.15)

| Allowed | Forbidden |
|---|---|
| Correlator **reads** `trader_feature_snapshots` | Scoring writes `strategy_cluster_id` as a score component |
| A later model may *propose* a merge (offline) | Model output sends or sizes an order |
| Cluster id as an **input feature** to a future model | “This cluster is good → skip risk” |

`A22` non-goal “no clustering” stays true for **baseline.v1**. Phase 2 clustering is a **risk** feature, not a scoring rewrite.

### 8.11 Test hooks (`A27` — future classes only)

Do **not** add these classes now. Inventory for the later change-set:

| Future test class | SUT | Must prove |
|---|---|---|
| `Risk.ConcentrationPolicyDisabledByDefaultTests` | `IConcentrationPolicy` / flags | Flag off → no `CONCENTRATION_CAP` even if 50 clones fire |
| `Risk.ConcentrationCapReduceAndRejectTests` | `IConcentrationPolicy` | remaining=0 reject; partial `REDUCE_SIZE`; below min reject |
| `Risk.ConcentrationIgnoresReduceCloseTests` | `RiskEngine` | `REDUCE`/`CLOSE` not blocked by cluster cap |
| `Risk.ConcentrationStaleGraphFailClosedTests` | `RiskEngine` | flag ON + stale graph → `CLUSTER_GRAPH_STALE` on OPEN |
| `Clustering.FeatureVectorFromReconstructedTradesTests` | `ClusterFeatureVector` | six axes; no dest quotes mixed in |
| `Clustering.ConnectedComponentMergeTests` | `IStrategyCorrelator` | A↔B, B↔C ⇒ one cluster; isolated D singleton |
| `Clustering.CrossBrokerCloneTests` | `IStrategyCorrelator` | same features on Achiever + StarwaveFX merge |
| `Clustering.FirstThreeNotAutoMergedTests` | `IStrategyCorrelator` | `n < min_n` stays singleton |
| `Clustering.MembershipIdempotentRebuildTests` | membership store | same as-of + schema → same components |
| `Clustering.ManualPinSurvivesRebuildTests` | membership store | `MANUAL` pin not overwritten |
| `Api.RiskDashboardClusterRollupTests` | API | `riskByCluster` present only when feature shipped |
| `Replay.FiftyCloneTradersConcentrationTests` | replay | 50 lookalikes cannot exceed cluster cap |

v1 `Risk.RiskEngineHardLimitTests` must **continue to pass without** any cluster fixture.

### 8.12 Repository-structure hook (§66)

If/when types are created, prefer extending existing projects:

```text
/src/Domain          # ids, membership value objects
/src/Application     # correlator + policy
/src/Infrastructure  # table configs / migrations
/tests/Unit          # classes in §8.11
/tests/Replay        # fifty-clone replay
```

Do **not** create `/services/cluster-service` or a new worker host just for this. §71 / §72.20: prefer simple systems.

---

## 9. v1 implementation constraints (what *current* work must do)

These are the only obligations this document places on work happening **now**. They are all negative / documentary.

| ID | Constraint | Owner today |
|---|---|---|
| V1-1 | Do not refuse copy for lack of a cluster graph | Risk / copy path (`A23` §10.1) |
| V1-2 | Keep reason name `CONCENTRATION_CAP` reserved in specs; do not emit it | `A23` §4.3 |
| V1-3 | Keep evaluation-order slot 13 empty | `A23` §5 |
| V1-4 | Do not add cluster tables to the 47-table catalog as required | `A20` |
| V1-5 | Do not add cluster fields to `GET /risk/dashboard` | `A26` |
| V1-6 | Do not add clustering to `baseline.v1` | `A22` |
| V1-7 | Do not add §8.11 test classes to the v1 inventory as missing-fail | `A27` |
| V1-8 | Do not list concentration as a §68 go-live checkbox | `A28` |
| V1-9 | Persist reconstructed trades and feature snapshots faithfully — they are the future correlator’s only clean input | Phases 2–3 engineering |
| V1-10 | Keep `(broker_id, login)` compound identity everywhere | `A20` §2 |
| V1-11 | Keep `correlation_id` as a **log** id only | §57 |

V1-9 is the only constructive preparation: good reconstruction + versioned feature snapshots. That work is already required by Engineering Phases 2–3. It is **not** a license to start pairwise jobs.

---

## 10. Scheduling checklist (when to open a real implementation ticket)

Copy this into the later ticket. Do not start the ticket until every box is evidenced on disk.

```text
Prerequisites (Engineering Phase 8 + gates)
[ ] A28 Phase 8 exit criteria true
[ ] Entire §68 list checked
[ ] Entire §70 list checked
[ ] Shadow sample sufficient; destination costs/slippage measured
[ ] Manual review of live flag still treats REAL_COPY_EXECUTION_ENABLED as explicit

Evidence that the problem is real (measurement, not vibe)
[ ] Count of simultaneously selected LIVE / SHADOW traders whose entry-sync
    in a replay window exceeds a stated threshold
[ ] Estimate of destination XAU that would have stacked on one thesis
[ ] Confirmation that v1 gross/net caps would have been the only binder

Design freeze for the change-set
[ ] This A60 document reviewed; any threshold numbers moved into ClusterConfig
[ ] A20 RFC for tables 48–52
[ ] A23 step 13 filled in (still flag default OFF)
[ ] A26 dashboard additive fields specified
[ ] A27 classes in §8.11 added as real tests, not placeholders

Ship sequence
[ ] Migrations for tables 48–52 (empty membership = every trader singleton)
[ ] Correlator batch + idempotent rebuild
[ ] IConcentrationPolicy behind CONCENTRATION_CAPS_ENABLED=false
[ ] Shadow-only enablement + replay of fifty-clone fixture
[ ] Dashboard rollup
[ ] Live enablement only after shadow measurement and audited limit values
```

Default remains **off** after merge. Turning it on is an audited limits / flag change, same spirit as `REAL_COPY_EXECUTION_ENABLED`.

---

## 11. Worked example (illustrative — not production thresholds)

Numbers below are **story quantities** so reviewers share a picture. They are not caps to paste into config.

```text
Cluster C = { Achiever/1001, Achiever/1002, StarwaveFX/5555 }
            // same direction, entries within 8s, hold ~90s, lot_cv < 0.2

max_allocation_per_cluster_gross = 3.0 dest XAU     // STORY ONLY
current cluster_gross            = 2.4
new OPEN from Achiever/1004, proposed dest qty 1.0
                                  // 1004 just merged into C on last run

remaining = 0.6
→ REDUCE_SIZE 0.6 if 0.6 ≥ destination min
→ else REJECT CONCENTRATION_CAP

A REDUCE from Achiever/1001 closing 0.5
→ step 13 skipped; close proceeds under §64
```

v1 without this phase: the same OPEN is limited only by account gross/net and per-trader loss. If account gross headroom is 8.0, all four clones can still stack.

---

## 12. Traceability

| Topic | Authority |
|---|---|
| Problem + six axes + cap example + “Phase 2 after basic copy” | **§65** |
| Engineering Phase 2 is reconstruction, not this | §67, `A28` |
| Risk is final authority; scoring only suggests | §39, §72.15 |
| Persist before FIX; no work in MT5 callback | §32, §72.6–7 |
| OPEN/INCREASE vs REDUCE/CLOSE | §64, §72.18 |
| No blind catch-up (independent of clustering) | §63 |
| Compound identity | §10, `A20` |
| Feature fields / session / lots / hold | §14, §18, `A22` |
| Do not mix price sources | §17 |
| Tables / UUID + natural keys | §44–§45, `A20` |
| Log identifiers | §57 |
| Prefer simple systems | §71, §72.20 |
| Reserved engine slot + unused reason | `A23` §5 step 13, §4.3, §10 |
| Not a v1 go-live gate | `A23` §11.3, `A28` §68 |
| Dashboard v1 surface this must not break | `A26` §6.12 |

---

## 13. Explicit non-delivery of this change-set

```text
[ ] No product source modified
[ ] No migration
[ ] No new Domain / Application types created
[ ] No env flag added to apps/api or workers
[ ] No dashboard field added
[ ] No test class added under tests/
[ ] CONCENTRATION_CAP is still unused at runtime
[ ] Engineering Phase 2 (reconstruction) is unaffected
```

The deliverable is **this file only**.
