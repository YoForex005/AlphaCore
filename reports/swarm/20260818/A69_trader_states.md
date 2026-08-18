# A69 — Trader State Transitions

**Artifact:** `D:\Prop\reports\swarm\20260818\A69_trader_states.md`  
**Date:** 2026-08-18  
**Agent:** A69 (state machine only)  
**Status:** Binding implementation spec. **No product source was modified.**  
**Source of law:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§1.4, 15, 22, 23, 39–41, 45, 59, 62, 68–72  
**Sibling specs (do not contradict unless this file is more specific on *state*):**  
A22 scoring, A23 risk engine, A24 shadow copy, A26 dashboard API, A27 test inventory, A20 table catalog, A28 phases/gates, A48 kill switch  
**Score family this machine consumes:** `baseline.v1` (A22)  
**Identity grain:** one current state per `(broker_id, login)`

This file is the implementer-binding trader-state machine. Scoring computes numbers and flags. This machine assigns **exactly one** of the nine §22 states. Risk is the final authority on any real `NewOrderSingle`. Trade #3 + high score is **SHADOW only**.

---

## 0. Verdict (read this first)

Architecture §23 recommended default, now locked:

```text
Trade #3 + high score
        ↓
SHADOW only
```

Do not automatically send real capital after three completed XAUUSD lifecycles.  
Do not emit `LIVE` or `LIVE_CANDIDATE` at `N == 3`.  
Do not emit `PROVEN_PROFITABLE` ever (that token is not a state).  
Do not skip shadow-sample evidence into live capital.

| Binding ID | Invariant |
|---|---|
| S0 | Vocabulary is exactly the nine §22 tokens listed in §2. No extras. No aliases. |
| S1 | `N` counts only **completed reconstructed XAUUSD position lifecycles** (§15). Orders, fills, partial closes, SL/TP edits are not trades. |
| S2 | `N < 3` official landing is `INSUFFICIENT_DATA` unless a higher-priority override (R0–R3) already fired. |
| S3 | First crossing of `N == 3` emits event `EARLY_SCORE_ELIGIBLE` once. It does **not** emit `PROVEN_PROFITABLE`. |
| S4 | At `N == 3`, legal states ⊆ `{EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED}`. **Forbidden:** `LIVE`, `LIVE_CANDIDATE`. |
| S5 | **Trade #3 + high score → `SHADOW` only.** Even if `early_quality_score` is at the A22 U(3) cap (82) and source NET is large. |
| S6 | `MIN_LIVE_TRADES` **must be `> 3`**. Config loader rejects `<= 3`. R5 is evaluated before R6. R6 also requires `N > 3`. |
| S7 | Scorer / ML may **nominate** `LIVE_CANDIDATE`. They never assign `LIVE` by themselves. `LIVE` requires R6 structure **and** audited `manual_live_approve` **and** `risk_engine_live_ok`. |
| S8 | `REAL_COPY_EXECUTION_ENABLED` is an execution flag. It does not raise trader state. A trader may sit in `LIVE` while the venue still refuses `NewOrderSingle`. |
| S9 | Shadow book is generated only from `{SHADOW, LIVE_CANDIDATE, LIVE}` (LIVE only if `SHADOW_PARALLEL_TO_LIVE=true`; A24 default **false** after promotion). Therefore R6 cannot be satisfied without prior shadow-eligible time. |
| S10 | Demotion is automatic on rescore. A high previous state is not a grandfather clause. |
| S11 | Leaving `PAUSED` **re-resolves** from current evidence. It does **not** restore a stale `LIVE`. (A26 “prior non-blocked state” is UI shorthand; this file wins on resume.) |
| S12 | `DISQUALIFIED` is sticky until an audited reclaim. Reclaim then re-resolves; it does not jump to `LIVE`. |
| S13 | Same `(N, scores, flags, shadow sample, manuals, prev_state, config)` → same state. Pure function. No clock, no RNG, no ML call inside `ResolveState`. |
| S14 | Every state change writes `trader_states` **and** an append-only history/audit row. Current-row overwrite without audit is a defect. |

---

## 1. Architecture quotes (binding)

### 1.1 §1.4 — do not send real money after trade #3

> Do not send a trader to real money immediately after trade #3.  
> The default action after a strong early score should be SHADOW.  
> Live execution should require additional evidence.

### 1.2 §15 — first 3 trades

Count only `3 completed reconstructed XAUUSD position lifecycles`.  
Trade #3 closure triggers `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE`.

### 1.3 §22 — continuous rescoring + vocabulary

Trade #3 is the first official score. Then rescore after 4, 5, 6, … and keep history.

Suggested (now closed) states:

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

### 1.4 §23 — three-trade safety gate

```text
Trade #3 + high score
        ↓
SHADOW only
```

A later configurable live gate may require: minimum completed trades, minimum shadow trades, minimum shadow net P&L, maximum shadow DD, minimum current score, no severe risk flags. Numbers are chosen from data (A22 provisional defaults). Do not treat them as proven optima.

### 1.5 Adjacent law

| Section | Relevance |
|---|---|
| §39 | Scoring emits candidate/confidence/allocation only. Risk approves/reduces/rejects/pauses. |
| §41 | `REAL_COPY_EXECUTION_ENABLED` default false. Flag is not a state transition. |
| §59 / §72.19 | Manual pause / DQ / live-approve / reclaim are RBAC + audit. |
| §62 | ML unavailable: do not *promote* to live. Ingestion continues. |
| §68 / §70 | Live `NewOrderSingle` stays off until shadow sample, stale guards, sizing, kill switches, reconcile-before-ready are proven. |
| §72.16 | Trade #3 means early evidence, not proven skill. |

---

## 2. Closed vocabulary

### 2.1 States (exactly these nine)

Persisted as text matching the enum in `D:\Prop\src\Domain\Enums\TraderState.cs`:

| Token | Enum value | Kind |
|---|---|---|
| `INSUFFICIENT_DATA` | 0 | pre-score |
| `EARLY_SCORE` | 1 | scored, not selected |
| `WATCH` | 2 | scored, ops interest |
| `SHADOW` | 3 | scored, destination-quote simulation |
| `LIVE_CANDIDATE` | 4 | structure of live gates passed; awaiting risk + RBAC |
| `LIVE` | 5 | risk-approved real-copy *selection* |
| `PAUSED` | 6 | operational / manual hold |
| `RISK_BLOCKED` | 7 | severe flags or risk-engine block |
| `DISQUALIFIED` | 8 | terminal until audited reclaim |

No tenth state. No `PROVEN_PROFITABLE`. No `CANDIDATE` alias. No `BLOCKED` alias. Dashboard roll-ups (Watch / Shadow / Live candidates / Live copied / Risk blocked) are **views**, not extra states.

### 2.2 Events (not states)

| Event | When | Persist |
|---|---|---|
| `EARLY_SCORE_ELIGIBLE` | First time `N` reaches 3 | once; idempotent on reconstruction replay |
| `RESCORED` | Every official compute with `N >= 3` | `trader_score_history` |
| `STATE_CHANGED` | `ResolveState` output ≠ `prev_state` | `trader_states` + audit |
| `MANUAL_PAUSE` | RBAC pause | audit |
| `MANUAL_RESUME` | RBAC clear of `manual_pause` | audit; then re-resolve |
| `MANUAL_BLOCK` | RBAC force `RISK_BLOCKED` | audit |
| `MANUAL_DISQUALIFY` | RBAC DQ | audit |
| `MANUAL_RECLAIM` | RBAC clear of DQ | audit; then re-resolve |
| `MANUAL_LIVE_APPROVE` | RBAC live seat | audit; still needs R6 + `risk_engine_live_ok` |
| `MANUAL_LIVE_REVOKE` | RBAC clear of live approve | audit; re-resolve (will leave `LIVE`) |
| `RISK_PAUSE_TRADER` | Risk engine `PAUSE_TRADER` | `risk_decisions` + state |
| `SHADOW_STARTED` / `SHADOW_STOPPED` | Enter / leave shadow-eligible set | optional system event |

Never persist `PROVEN_PROFITABLE`.

### 2.3 What is not a state

- Score bands (`early_quality_score` is a number).
- Risk-engine outcomes (`APPROVE`, `REDUCE_SIZE`, `REJECT`, `PAUSE_TRADER`, `PAUSE_VENUE`, `GLOBAL_STOP`).
- Kill-switch modes (`STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN`) — A48. They freeze *execution*, they do not by themselves rewrite trader state.
- Feature flags.
- Challenge pass/fail / prop-firm phase.
- Source-broker group / plan name.

---

## 3. Inputs to `ResolveState`

Pure function. First matching rule in §6 wins.

```text
StateInput
  broker_id, login
  N                               // completed eligible XAU lifecycles as-of T
  as_of                           // closed_at of trade N (or last known)
  early_quality_score             // A22; unused when N < 3 except diagnostics
  risk_score, behavior_score      // audit / reason only
  flags[]                         // A22 §8
  severe_risk                     // any currently severe flag
  shadow_completed_trades         // destination-quote shadow lifecycles (A24)
  shadow_net_pnl                  // shadow_performance, dest costs
  shadow_max_dd_frac              // conservative dest MTM (A24 §12)
  manual_pause                    // sticky until MANUAL_RESUME
  manual_block                    // sticky until audited clear
  manual_dq                       // sticky until MANUAL_RECLAIM
  manual_live_approve             // sticky until MANUAL_LIVE_REVOKE
  prev_state
  reclaim_now                     // true only on the audited reclaim transaction
  risk_engine_live_ok             // external snapshot; scorer does not compute it
  score_version                   // e.g. baseline.v1
  config                          // ScoreConfig.v1 gate constants
```

**Not inputs (must not raise state):**

- Source NET / balance / challenge result.
- `REAL_COPY_EXECUTION_ENABLED`.
- Future trade `N+1`.
- Destination *live* P&L (shadow P&L is a **gate** input only, never a score input — A22 I8).
- ML probability unless a later `baseline.v2` / model spec redefines the ranking key. v1 ranking key is `early_quality_score`.
- Kill-switch flags (they block sends; they do not promote or DQ).

`N` and the trade window follow A22 §2 (eligible = completed + canonical XAUUSD + `closed_at` + `closed_volume > 0`; order `closed_at, opened_at, id`).

---

## 4. Per-state contract

Copy columns:

- **Shadow:** may the shadow engine create `OPEN`/`INCREASE` intents? (REDUCE/CLOSE of an *existing* shadow position still follow A24 close policy even after demotion — see §10.3.)
- **Live NOS:** may the live path persist an `ApprovedExecutionIntent` that the FIX worker is allowed to send? Still subject to A23, A48, and `REAL_COPY_EXECUTION_ENABLED`.

### 4.1 `INSUFFICIENT_DATA`

| | |
|---|---|
| Meaning | Fewer than 3 completed XAU lifecycles. No official leaderboard score. |
| Entry | Default for a newly seen `(broker_id, login)`. Also the R4 landing when `N < 3` and R0–R3 did not fire. |
| Official scores | Not published. Optional `PROVISIONAL` snapshot only (A22 window). |
| Shadow | no |
| Live NOS | no |
| Dashboard | hidden from ranked lists; may appear in raw account inventory |
| Exit | When `N` reaches 3 → R5 (or R0–R3). Never → `LIVE` / `LIVE_CANDIDATE`. |

Manual pause / block / DQ **can** apply before trade #3 (R0–R3 beat R4). That is how ops stop a two-trade martingale book from ever being scored into `SHADOW`.

### 4.2 `EARLY_SCORE`

| | |
|---|---|
| Meaning | Officially scored (`N >= 3`) and not selected for watch/shadow; not blocked. Default *weak* landing. |
| Entry | R5/R9 when `early_quality < WATCH_MIN` and no override. |
| Shadow | no |
| Live NOS | no |
| Event | First arrival at `N == 3` still emits `EARLY_SCORE_ELIGIBLE` even if the state is `EARLY_SCORE` rather than `SHADOW`. |
| Exit | Rescore to `WATCH`/`SHADOW` (or R6 after `N > 3` **and** shadow evidence). Overrides to pause / block / DQ. |

`EARLY_SCORE` is **not** “failed.” It is “scored, not yet interesting.”

### 4.3 `WATCH`

| | |
|---|---|
| Meaning | Mid-band quality. Human/ops interest. Not shadowed. |
| Entry | R5/R8 when `WATCH_MIN <= early_quality < SHADOW_MIN`. |
| Shadow | no |
| Live NOS | no |
| Exit | Up to `SHADOW` on better scores; down to `EARLY_SCORE`; R6 only with shadow evidence (which this state does not accumulate — so a `WATCH` trader cannot satisfy R6 until they have been `SHADOW`). |

### 4.4 `SHADOW`

| | |
|---|---|
| Meaning | High early quality. Destination-quote **simulation only** (A24). |
| Entry | **Default automatic ceiling** after Trade #3 + high score (S5, R5). Also R7 when `N > 3` and live gates fail or are not requested. |
| Shadow | **yes** (`OPEN`/`INCREASE` subject to A24 open policy) |
| Live NOS | **no** — `TRADER_NOT_LIVE` |
| Exit | Demote on score drop or flags. Promote only via R6 to `LIVE_CANDIDATE` (or `LIVE` if already approved). Pause / block / DQ. |

This is the load-bearing safety state. A24 §15: “Shadow is the default after trade #3 + high score.”

**High score (v1 operational definition):**

```text
high_score  ≡  early_quality_score >= SHADOW_MIN
               AND NOT severe_risk
               AND NOT manual_pause / manual_block / manual_dq
```

`SHADOW_MIN = 62` after U(N) is an A22 **provisional** default, not a proven optimum. Structure is locked; the number is a config knob. At `N == 3`, U(3) = 18 so a theoretically perfect book scores at most 82 — still `SHADOW`, never live.

### 4.5 `LIVE_CANDIDATE`

| | |
|---|---|
| Meaning | Passed the **structure** of the live gate (sample + shadow evidence + score + no severe flags + `N > 3`). Awaiting risk engine + RBAC. |
| Entry | R6 when live structure holds and (`manual_live_approve` is false **or** `risk_engine_live_ok` is false). |
| Impossible when | `N == 3` (S4, R5 before R6, `N > 3` conjunct). |
| Shadow | **yes** (still simulation) |
| Live NOS | **no** — still `TRADER_NOT_LIVE` |
| Exit | `LIVE` only when R6 + both approve bits. Else fall back to `SHADOW`/`WATCH`/`EARLY_SCORE` or overrides. |

Nomination is not permission. Dashboard may highlight candidates; the FIX worker must treat them as shadow.

### 4.6 `LIVE`

| | |
|---|---|
| Meaning | Risk-approved *selection* for real copy. Not a send license by itself. |
| Entry | **Only** R6 with `manual_live_approve && risk_engine_live_ok && N > 3` and all live-gate numbers. |
| Impossible when | `N == 3`. Also impossible if `MIN_LIVE_TRADES <= 3` (loader must reject that config). |
| Shadow | only if `SHADOW_PARALLEL_TO_LIVE=true` (A24 default **false**) |
| Live NOS | **eligible**, iff `REAL_COPY_EXECUTION_ENABLED` and A23 path is healthy |
| Exit | Automatic demotion on rescore if R6 fails; pause / block / DQ; `MANUAL_LIVE_REVOKE`. |

A trader in `LIVE` with the feature flag off is a selected trader whose orders are still not sent. That is correct. Do not “helpfully” drop them to `SHADOW` solely because the flag is off (S8).

### 4.7 `PAUSED`

| | |
|---|---|
| Meaning | Manual or operational hold. Previous state is stored for audit, **not** as a restore target. |
| Entry | R2 `manual_pause`, or risk-engine `PAUSE_TRADER` that sets `manual_pause` (or a dedicated `risk_pause` bit that R2 also honors). A26 `POST .../copy-control` `{action: PAUSE}`. |
| Shadow new opens | **no** |
| Live NOS | **no** — `TRADER_PAUSED` |
| Existing dest / shadow | REDUCE/CLOSE of already-open mapped positions remain allowed (A23 §64, A24 §8, A48 stop-new analog). |
| Exit | `MANUAL_RESUME` / clear pause → **re-resolve R0–R9 on current evidence**. |

If the trader was `LIVE` and then paused, resume must not put them back in `LIVE` unless R6 still holds **and** `manual_live_approve` is still set **and** `risk_engine_live_ok` is still true.

### 4.8 `RISK_BLOCKED`

| | |
|---|---|
| Meaning | Severe behavioral flags or an explicit manual/risk block. Not a timeout. |
| Entry | R3: `manual_block` **or** `severe_risk`. Typical A22 severe flags: `FLAG_MARTINGALE`, severe averaging-down, abnormal sizing, severe drawdown, MAE-over-SL, burst+martingale, etc. |
| Shadow new opens | **no** |
| Live NOS | **no** — `TRADER_RISK_BLOCKED` / `SEVERE_RISK_FLAG` |
| Exit | Only when `severe_risk` is cleared **and** `manual_block` is cleared; then re-resolve. May go to `DISQUALIFIED` (R0) or `PAUSED` (R2) without clearing. |

Dollars do not clear this state. A22 Case B (huge NET, martingale) is `RISK_BLOCKED`, not `SHADOW`.

### 4.9 `DISQUALIFIED`

| | |
|---|---|
| Meaning | Terminal / near-terminal: confirmed abuse, repeated martingale after prior block, or audited manual DQ. |
| Entry | R0 `manual_dq`, or R1 sticky `prev_state == DISQUALIFIED` without `reclaim_now`. |
| Shadow / live | **none** |
| Exit | **Only** `MANUAL_RECLAIM` (`reclaim_now=true` on that transaction), then re-resolve. Never auto-reclaim on a lucky later score. |

Repeated `FLAG_MARTINGALE` after a previous `RISK_BLOCKED` **may** be escalated to DQ by a versioned policy (`DQ_AFTER_REPEAT_MARTINGALE=true`). That policy is an input that sets `manual_dq` (system actor) — it is not a tenth state.

---

## 5. Trade #3 safety gate (must be unit-tested)

Normative. Tests fail the build if any clause is violated.

```text
WHEN N == 3:
    emit EARLY_SCORE_ELIGIBLE once (idempotent on reconstruction replay)

    next_state ∈ {
        EARLY_SCORE, WATCH, SHADOW,
        PAUSED, RISK_BLOCKED, DISQUALIFIED
    }

    next_state ∉ { LIVE, LIVE_CANDIDATE }

    EVEN IF early_quality_score == 82 (v1 U(3) cap)
         AND source NET is arbitrarily large
         AND MIN_LIVE_TRADES was (illegally) set to 3
         AND shadow_* fields are stuffed with fake evidence:
            maximum automatic promotion = SHADOW
```

Pseudo-asserts:

```text
Assert(N != 3 || (state != LIVE && state != LIVE_CANDIDATE))
Assert(N < 3  || first_crossing_event == EARLY_SCORE_ELIGIBLE)
Assert(AfterHighEarlyScore() == SHADOW)
Assert(CanPromoteToLive(any_state_at_N_eq_3) == false)
```

Current stub already encodes the last two in `TraderStateMachine` (`AfterHighEarlyScore` → `SHADOW`, `CanPromoteToLive` → `false`). That stub is **not** the full machine; see §13.

High-score landing table at `N == 3` (R0–R3 not firing):

| `early_quality_score` | State |
|---|---|
| `>= SHADOW_MIN` (62 v1) | **`SHADOW`** |
| `>= WATCH_MIN` (48 v1) and `< SHADOW_MIN` | `WATCH` |
| `< WATCH_MIN` | `EARLY_SCORE` |

No row produces `LIVE` or `LIVE_CANDIDATE`.

---

## 6. Resolver — first match wins

`ResolveState(StateInput) -> TraderState`

```text
R0  if manual_dq:                                           DISQUALIFIED

R1  if prev_state == DISQUALIFIED and not reclaim_now:      DISQUALIFIED

R2  if manual_pause:                                        PAUSED

R3  if manual_block or severe_risk:                         RISK_BLOCKED

R4  if N < 3:                                               INSUFFICIENT_DATA

R5  if N == 3:
       if early_quality >= SHADOW_MIN:                      SHADOW
       elif early_quality >= WATCH_MIN:                     WATCH
       else:                                                EARLY_SCORE
       // LIVE / LIVE_CANDIDATE unreachable here

R6  if N >= MIN_LIVE_TRADES
       and N > 3
       and shadow_completed_trades >= MIN_SHADOW_TRADES
       and shadow_net_pnl >= MIN_SHADOW_PNL
       and shadow_max_dd_frac <= MAX_SHADOW_DD_FRAC
       and early_quality >= MIN_LIVE_SCORE
       and not severe_risk:
         if manual_live_approve and risk_engine_live_ok:    LIVE
         else:                                              LIVE_CANDIDATE

R7  if early_quality >= SHADOW_MIN:                         SHADOW

R8  if early_quality >= WATCH_MIN:                          WATCH

R9  else:                                                   EARLY_SCORE
```

### 6.1 Belt-and-braces on live

1. R5 runs **before** R6, so `N == 3` never reaches live gates.
2. R6 repeats `N > 3`.
3. Config loader rejects `MIN_LIVE_TRADES <= 3`.
4. Shadow sample is destination-quote evidence (A24). Source NET cannot substitute.
5. `LIVE` additionally requires two independent bits: human RBAC and current risk-engine health. One is not enough.
6. ML unavailable (§62): force `risk_engine_live_ok = false` for *new* promotions. Existing `LIVE` may remain `LIVE` as a selection state; sends still fail closed if risk/venue is unhealthy.

### 6.2 Provisional v1 gate constants (structure locked; values are knobs)

From A22 `ScoreConfig.v1`. Changing a number increments `baseline.v1.N`. Changing a formula or this graph requires `baseline.v2` + a new spec.

```text
SHADOW_MIN            = 62     // after U(N)
WATCH_MIN             = 48
MIN_LIVE_TRADES       = 20     // MUST be > 3
MIN_SHADOW_TRADES     = 10
MIN_SHADOW_PNL        = 0      // destination-quote shadow net
MAX_SHADOW_DD_FRAC    = 0.15
MIN_LIVE_SCORE        = 70
```

Do not hardcode these in multiple places. The machine reads config.

### 6.3 Why `WATCH` cannot jump to `LIVE`

A24: only `{SHADOW, LIVE_CANDIDATE, LIVE}` generate new shadow opens.  
R6 requires `shadow_completed_trades >= MIN_SHADOW_TRADES`.  
Therefore a book that never entered `SHADOW` cannot accumulate the sample R6 needs.  
Any test that injects fake `shadow_*` on a never-shadowed trader is testing the resolver in isolation; the application layer must pass **actual** `shadow_performance` rows, not source P&L.

---

## 7. Allowed transition matrix

Rows = `from`. Columns = `to`.  
`Y` = allowed. `3` = allowed **only** as the Trade #3 first official landing (`INSUFFICIENT_DATA` → … when `N` reaches 3).  
`L` = allowed only via R6 (`N > 3` + shadow evidence + live structure).  
`A` = allowed only via audited manual / risk action (R0–R2, reclaim, live approve).  
`R` = allowed on re-resolve after the sticky bit that held the state is cleared.  
blank = **illegal**.

| from \ to | INSUF | EARLY | WATCH | SHADOW | LIVE_CAND | LIVE | PAUSED | RISK_BLK | DQ |
|---|---|---|---|---|---|---|---|---|---|
| `INSUFFICIENT_DATA` | · | 3 | 3 | 3 |  |  | A | 3/A | A |
| `EARLY_SCORE` |  | · | Y | Y | L | L* | A | Y | A |
| `WATCH` |  | Y | · | Y | L | L* | A | Y | A |
| `SHADOW` |  | Y | Y | · | L | L* | A | Y | A |
| `LIVE_CANDIDATE` |  | Y | Y | Y | · | L | A | Y | A |
| `LIVE` |  | Y | Y | Y | Y | · | A | Y | A |
| `PAUSED` | R | R | R | R | R/L | R/L | · | R/Y | A |
| `RISK_BLOCKED` |  | R | R | R |  |  | A | · | A |
| `DISQUALIFIED` |  |  |  |  |  |  |  |  | · |

`L*` = `LIVE` only through the R6 live path (structure + `manual_live_approve` + `risk_engine_live_ok`). The application should normally pass through `LIVE_CANDIDATE` for at least one persisted rescore so ops can see the nomination. Same-tick `SHADOW → LIVE` is legal in the pure function if both approve bits are already set; it is **not** the default product path.

`DISQUALIFIED` has no outbound edge except reclaim (not shown as a state). After `reclaim_now`, `prev_state` is treated as non-DQ for R1 and the function re-enters at R0 (now false) … R9. The first persisted post-reclaim state is whatever R2–R9 produce.

`INSUFFICIENT_DATA` after `N >= 3` is illegal (must not remain). The only way back to `INSUFFICIENT_DATA` is if reconstruction *retracts* completed trades so `N < 3` and R0–R3 are clear — a data-correction path, not a demotion.

Self-transitions (`·`) are no-ops: update scores/history, do not emit `STATE_CHANGED`.

---

## 8. Forbidden transitions (always)

```text
*                  → LIVE              when N == 3
*                  → LIVE_CANDIDATE    when N == 3
INSUFFICIENT_DATA  → LIVE
INSUFFICIENT_DATA  → LIVE_CANDIDATE
INSUFFICIENT_DATA  → EARLY_SCORE|WATCH|SHADOW   when N < 3
any auto path that skips shadow-sample evidence into LIVE
EARLY_SCORE|WATCH  → LIVE_CANDIDATE|LIVE        without shadow_completed_trades >= MIN_SHADOW_TRADES
RISK_BLOCKED       → LIVE or LIVE_CANDIDATE     in the same resolve (severe_risk still true would have hit R3;
                                                even after flags clear, R6 must be re-satisfied; do not jump)
DISQUALIFIED       → *                          without MANUAL_RECLAIM
PAUSED             → LIVE                       as a blind restore of prev_state
any state          → PROVEN_PROFITABLE          (token does not exist)
scorer/ML          → LIVE                       without both approve bits
REAL_COPY_EXECUTION_ENABLED flip                → any state change
source NET alone                                → SHADOW or LIVE
```

Property tests (A22 T4, A27 `ThreeTradeSafetyGateTests`):

```text
∀ fixtures with N == 3:  state ∉ { LIVE, LIVE_CANDIDATE }
∀ fixtures with N <  3 and ¬(R0∨R1∨R2∨R3): state == INSUFFICIENT_DATA
∀ fixtures with high_score and N == 3 and ¬(R0∨R1∨R2∨R3): state == SHADOW
```

---

## 9. Copy / risk coupling

### 9.1 Eligibility by state

| State | New shadow OPEN/INCR | New live OPEN/INCR | REDUCE/CLOSE existing dest | REDUCE/CLOSE existing shadow |
|---|---|---|---|---|
| `INSUFFICIENT_DATA` | no | no | n/a | n/a |
| `EARLY_SCORE` | no | no | n/a | if any leftover (rare) |
| `WATCH` | no | no | n/a | if leftover |
| `SHADOW` | yes | no | n/a | yes |
| `LIVE_CANDIDATE` | yes | no | n/a | yes |
| `LIVE` | only if parallel shadow | yes if flag+A23 | yes (A23 close policy) | if parallel |
| `PAUSED` | no | no | yes (risk-reduction) | yes (A24 close) |
| `RISK_BLOCKED` | no | no | yes | yes |
| `DISQUALIFIED` | no | no | yes (flatten residual) | yes |

“Leftover” shadow after demotion: do **not** open new shadow size; **do** close/reduce what already exists so P&L is not stranded (A24 §8.4). Same for live residuals on demotion from `LIVE`: A23 close/reduce policy, not a new open.

### 9.2 Risk-engine reason codes (A23)

| State at evaluate | Typical primary reason on live OPEN/INCR |
|---|---|
| not `LIVE` | `TRADER_NOT_LIVE` |
| `PAUSED` | `TRADER_PAUSED` |
| `RISK_BLOCKED` | `TRADER_RISK_BLOCKED` |
| `DISQUALIFIED` | `TRADER_RISK_BLOCKED` or a dedicated `TRADER_DISQUALIFIED` (prefer dedicated) |
| `LIVE` but severe flag raced in | `SEVERE_RISK_FLAG` (and resolver should already have moved state) |
| shadow OPEN when not shadow-eligible | `TRADER_NOT_SHADOW_ELIGIBLE` (A24) |

`PAUSE_TRADER` (A23 decision) sets `manual_pause` (or `risk_pause` aliased into R2) and persists `risk_decision` **before** the next resolve. It does not send FIX.

Kill switches (A48) do **not** rewrite trader state. A `LIVE` trader under `STOP_NEW_EXECUTION` stays `LIVE`; new opens reject with `STOP_NEW_EXECUTION`.

### 9.3 Scoring cannot bypass risk (§39, §72.15)

`LIVE` + high score + ML confidence = 1.0 still walks A23. The state machine never calls FIX.

---

## 10. When the machine runs

### 10.1 Continuous rescoring (§22)

On each newly completed eligible trade `k`:

```text
1. Rebuild window [1..k] as-of closed_at(k).          // A22 I6
2. Compute features + flags + three scores.
3. If k == 3: persist FIRST3 snapshot; emit EARLY_SCORE_ELIGIBLE.
4. Load shadow_performance + manual bits + prev_state.
5. next = ResolveState(...)
6. INSERT trader_score_history (never update-in-place).
7. UPSERT trader_scores (current).
8. If next != prev: UPSERT trader_states + audit STATE_CHANGED
       (old, new, rule id, flags, n, as_of, actor).
9. UPSERT trader_risk_flags (close cleared flags with ended_at).
```

Replay of the same close is idempotent: same `as_of + n + score_version + window` does not create a second history row and does not flap state.

### 10.2 Out-of-band triggers (same resolver)

| Trigger | What changes in StateInput | Then |
|---|---|---|
| `MANUAL_PAUSE` / `RISK_PAUSE_TRADER` | `manual_pause=true` | resolve → `PAUSED` |
| `MANUAL_RESUME` | `manual_pause=false` | resolve (no restore) |
| `MANUAL_BLOCK` | `manual_block=true` | resolve → `RISK_BLOCKED` |
| clear block | `manual_block=false` | resolve |
| `MANUAL_DISQUALIFY` | `manual_dq=true` | resolve → `DISQUALIFIED` |
| `MANUAL_RECLAIM` | `manual_dq=false`, `reclaim_now=true` | resolve |
| `MANUAL_LIVE_APPROVE` | `manual_live_approve=true` | resolve (still needs R6) |
| `MANUAL_LIVE_REVOKE` | `manual_live_approve=false` | resolve (leaves `LIVE`) |
| flag raise / clear on rescore | `severe_risk` | resolve |
| shadow sample update | shadow_* | resolve (may enter R6) |
| `risk_engine_live_ok` flip | that bit | resolve (candidate ↔ live only) |

All manual rows require actor id + reason + RBAC role (A26 / §59). SuperAdmin or RiskManager for pause/resume/block. SuperAdmin (or a dedicated `LiveApprover` if later split) for live-approve and reclaim. ReadOnly never mutates state.

### 10.3 Demotion examples (normative)

| Situation | Result |
|---|---|
| `SHADOW`, trade #6 raises `FLAG_MARTINGALE` | `RISK_BLOCKED` via R3 |
| `LIVE`, shadow DD exceeds `MAX_SHADOW_DD_FRAC` (or live book analog fed into the same fields) | leave `LIVE`; R7/R8/R9 or R3 |
| `LIVE`, score drops below `MIN_LIVE_SCORE` but `>= SHADOW_MIN` | `SHADOW` (or `LIVE_CANDIDATE` if R6 structure otherwise holds — it does **not**, because `early_quality < MIN_LIVE_SCORE`) → `SHADOW` |
| `LIVE_CANDIDATE`, ops never approves | remains `LIVE_CANDIDATE` while R6 holds; if sample decays, R7 → `SHADOW` |
| `PAUSED` (was `LIVE`), resume, R6 now false | `SHADOW` / `WATCH` / `EARLY_SCORE` / `RISK_BLOCKED` — **not** `LIVE` |
| Two-trade book, one averaging-down lifecycle, `severe_risk` | `RISK_BLOCKED` even at `N < 3` (R3 before R4) |

---

## 11. Persistence contract

Logical only. No migration in this change-set. Aligns with A20 / A22 §10.

### 11.1 `trader_states` (current)

```text
id
broker_id, login                 -- UNIQUE (broker_id, login)
state                            -- CHECK: the nine tokens
prev_state                       -- nullable only for the first insert
reason                           -- R0..R9 + flag list + event
n, as_of
changed_at
actor                            -- system:baseline.v1 | system:risk_engine | user:<id>
score_version
```

CHECK:

```text
state IN (
  'INSUFFICIENT_DATA','EARLY_SCORE','WATCH','SHADOW',
  'LIVE_CANDIDATE','LIVE','PAUSED','RISK_BLOCKED','DISQUALIFIED'
)
```

### 11.2 History / audit

Do not overwrite `trader_states` without an audit row (`audit_logs` and/or `trader_state_history`). Minimum audit payload: `from`, `to`, `rule`, `n`, `as_of`, `actor`, `reason`, `correlation_id`.

`trader_scores.current_state` is a denormalized copy of `trader_states.state`. `trader_score_history.state` is the state **as resolved at that score**. Neither is authoritative over `trader_states`.

### 11.3 Manual bits

Persist separately so resume/reclaim is not “guess the previous enum”:

```text
trader_state_controls
  broker_id, login
  manual_pause, paused_at, paused_by, pause_reason
  manual_block, blocked_at, blocked_by, block_reason
  manual_dq, dq_at, dq_by, dq_reason
  manual_live_approve, approved_at, approved_by, approve_reason
  prev_state_at_pause            -- audit only, never a restore target
```

---

## 12. Worked examples

Constants = A22 v1 defaults. Qualitative outcomes are hard.

### 12.1 Case A — clean three-trade book → SHADOW, never LIVE

```text
N=3, early_quality high (≤ 82), no severe flags, no manuals
```

```text
event = EARLY_SCORE_ELIGIBLE
state = SHADOW
NOT   = LIVE, LIVE_CANDIDATE, PROVEN_PROFITABLE
```

This is the title rule of this document.

### 12.2 Case B — huge profit, martingale → RISK_BLOCKED

```text
N=3, NET large, FLAG_MARTINGALE severe
```

```text
state = RISK_BLOCKED
NOT   = SHADOW, LIVE, LIVE_CANDIDATE
```

Dollars do not buy a live seat or a shadow seat.

### 12.3 Case C — mid / weak three-trade book

```text
N=3, no severe flags
early_quality in [WATCH_MIN, SHADOW_MIN) → WATCH
early_quality < WATCH_MIN                 → EARLY_SCORE
```

### 12.4 Case D — two trades

```text
N=2, any P&L
```

```text
state = INSUFFICIENT_DATA   (unless R0–R3)
no EARLY_SCORE_ELIGIBLE
```

### 12.5 Case E — trade #20, good shadow, no human approve → LIVE_CANDIDATE

```text
N=20, early_quality >= 70
shadow_completed_trades=12, shadow_net_pnl>0, shadow_max_dd_frac=0.08
severe_risk=false, manual_live_approve=false
```

```text
state = LIVE_CANDIDATE
NOT LIVE until risk_engine_live_ok AND audited manual_live_approve
```

### 12.6 Case F — config attack

```text
MIN_LIVE_TRADES set to 3, N=3, perfect scores, stuffed shadow fields
```

```text
config loader rejects MIN_LIVE_TRADES <= 3
AND R5 still forces {EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED}
state ≠ LIVE and ≠ LIVE_CANDIDATE
```

### 12.7 Case G — pause then resume must not resurrect LIVE

```text
prev LIVE, manual_pause set → PAUSED
later: score now 50, shadow DD 0.30, resume
```

```text
state ∈ {WATCH, EARLY_SCORE, SHADOW, RISK_BLOCKED} according to current evidence
state ≠ LIVE
```

### 12.8 Case H — DQ is sticky

```text
DISQUALIFIED, then a later rescore with early_quality=80 and clean flags
```

```text
state = DISQUALIFIED          // R1
until MANUAL_RECLAIM
```

---

## 13. Measured implementation (2026-08-18) — honesty

This spec is **not** implemented as the full machine. Product source was not changed by A69.

| Item | Path | Class |
|---|---|---|
| Enum (nine tokens, correct order) | `D:\Prop\src\Domain\Enums\TraderState.cs` | **EXISTS_AND_GOOD** (vocabulary) |
| Stub resolver | `TraderStateMachine.FromBaseline` in `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | **EXISTS_NEEDS_REFACTOR** |
| `AfterHighEarlyScore() => SHADOW` | same file | **EXISTS_AND_GOOD** (S5 pin) |
| `CanPromoteToLive(_) => false` | same file | **EXISTS** — safer than a premature live path; must become the R6+approve function later, still `false` at `N==3` |
| `TraderScore.CurrentState` | `D:\Prop\src\Domain\Entities\TraderScore.cs` | **EXISTS** (denormalized field only) |
| `TraderScoreHistory.State` | `D:\Prop\src\Domain\Entities\TraderScoreHistory.cs` | **EXISTS** (no unique key / window / reason) |
| `trader_states` table / EF config | A20 named; not in Infrastructure | **MISSING** |
| `trader_state_controls` | — | **MISSING** |
| `ResolveState` R0–R9 | — | **MISSING** (stub has no PAUSED / LIVE_CANDIDATE / LIVE / DISQUALIFIED / manuals / shadow gates) |
| Unit tests (`ScoreStateTransitionTests`, `ThreeTradeSafetyGateTests`, …) | A09 / A27 | **MISSING** |
| Dashboard `copy-control` PAUSE/RESUME | A26 | **SPEC ONLY** |

Stub behavior today (for implementers replacing it):

```text
N == 0                         → INSUFFICIENT_DATA
risk >= 80 OR (martingale ∧ maxDD>0 ∧ NET<0) → RISK_BLOCKED
N < 3                          → INSUFFICIENT_DATA
quality >= 70 ∧ risk < 40      → SHADOW
quality >= 55                  → WATCH
else                           → EARLY_SCORE
```

Gaps vs this spec: no R0–R2, no `N == 3` hard exclusion of live (live is simply unreachable because the stub never returns it), thresholds 70/55/40 are not `SHADOW_MIN`/`WATCH_MIN`, no shadow-sample live gate, no sticky DQ/pause, no history/audit on change. **Do not treat the stub as the legal graph.**

---

## 14. Test matrix (required)

Aligns with A22 T3–T7, T13–T15 and A27 §4.4. New rows are A69-owned.

| ID | Assert |
|---|---|
| TS1 | Closed vocabulary: enum names == the nine tokens; no `PROVEN_PROFITABLE` |
| TS2 | `N < 3` ∧ ¬(R0–R3) → `INSUFFICIENT_DATA` |
| TS3 | First `N == 3` emits `EARLY_SCORE_ELIGIBLE` once under replay |
| TS4 | Property: `N == 3` ⇒ state ∉ `{LIVE, LIVE_CANDIDATE}` |
| TS5 | Case A: high score @3 → **`SHADOW` only** |
| TS6 | `AfterHighEarlyScore() == SHADOW` |
| TS7 | Case B: martingale @3 → `RISK_BLOCKED` |
| TS8 | Case C: mid → `WATCH`; weak → `EARLY_SCORE` |
| TS9 | `MIN_LIVE_TRADES <= 3` fails config validation |
| TS10 | Case F: stuffed shadow + illegal config still cannot produce live @3 |
| TS11 | Case E: R6 without approve → `LIVE_CANDIDATE`, not `LIVE` |
| TS12 | R6 + both approve bits + `N >= 20` → `LIVE` |
| TS13 | `WATCH` with `shadow_completed_trades = 0` cannot reach R6 |
| TS14 | Demote: `SHADOW` + new martingale @6 → `RISK_BLOCKED` |
| TS15 | Pause wins over SHADOW; resume re-resolves (Case G) |
| TS16 | DQ sticky (Case H); reclaim then re-resolve, not auto `LIVE` |
| TS17 | `INSUFFICIENT_DATA → LIVE` is unrepresentable |
| TS18 | Same input twice → same state; no extra `STATE_CHANGED` |
| TS19 | Kill-switch on does not change state |
| TS20 | Feature-flag off does not demote `LIVE` |
| TS21 | High ML/score tick cannot skip `RISK_BLOCKED` / `DISQUALIFIED` / `PAUSED` |
| TS22 | `CanPromoteToLive` is false whenever `N <= 3` |

Suggested test classes (A27 names):

```text
Scoring.ScoreStateTransitionTests
Scoring.EarlyScoreEligibleTests
Scoring.ThreeTradeSafetyGateTests
Scoring.ScoreRescoringAfterTradeNTests
Scoring.RiskBlockedAndDisqualifiedTransitionsTests
Scoring.PauseResumeReresolveTests          // A69 add
Scoring.LiveCandidateNotAutoLiveTests      // A69 add
```

---

## 15. Mermaid (reference)

```text
                    ┌─────────────────────┐
                    │ INSUFFICIENT_DATA   │
                    │      (N < 3)        │
                    └──────────┬──────────┘
                               │ N reaches 3
                               │ EARLY_SCORE_ELIGIBLE
              ┌────────────────┼────────────────┐
              ▼                ▼                ▼
        EARLY_SCORE          WATCH           SHADOW
        (weak)               (mid)           (high)  ← DEFAULT CEILING @ N==3
              │                │                │
              │         N>3 + shadow sample     │
              │         + live structure        │
              └────────────────┼────────────────┘
                               ▼
                        LIVE_CANDIDATE
                               │ manual_live_approve
                               │ AND risk_engine_live_ok
                               ▼
                             LIVE
                               │
                               │ rescore fail / revoke
                               ▼
                        SHADOW / WATCH / EARLY_SCORE

Any scored state --manual_pause / PAUSE_TRADER--> PAUSED
Any state       --severe_risk / manual_block----> RISK_BLOCKED
Any state       --manual_dq---------------------> DISQUALIFIED (sticky)
```

Overrides R0–R3 sit above this picture.

---

## 16. Explicit exclusions

```text
No product-source edits in this change-set
No Trade #3 → LIVE
No Trade #3 → LIVE_CANDIDATE
No Trade #3 → PROVEN_PROFITABLE
No automatic real capital after three trades
No restore-to-LIVE on resume
No self-promotion of models into LIVE (§71)
No kill-switch as a tenth trader state
No source-NET ranking as a state input
No Kafka / mesh / extra venue
```

---

## 17. Traceability

| Topic | Authority |
|---|---|
| Vocabulary | Arch §22; enum `TraderState` |
| First-3 definition + event | Arch §15 |
| SHADOW-only default | Arch §1.4, §23; A22 I5; A24 §15; this file S5 |
| Score formulas / flags / U(N) | A22 (not duplicated here) |
| Live gate numbers | A22 §9.4; Arch §23 “choose later from data” |
| Shadow eligibility / book | A24 |
| Live send path / reasons | A23 |
| Pause vs flatten | A48 |
| Dashboard pause/resume | A26 §6.5 — resume semantics overridden by S11 |
| Table grain | A20 `trader_states` |
| Tests | A27 §4.4; A22 §13; this file §14 |
| Phase | A28 Phase 3 (machine + scores); live assign only Phase 8 + §68/§70 |

---

## 18. Decision summary

| Question | Decision |
|---|---|
| How many states? | Nine. Closed set. |
| What happens at trade #3 + high score? | **`SHADOW` only.** |
| Can trade #3 be live or live-candidate? | **No.** |
| Who may assign `LIVE`? | R6 structure + audited human approve + current risk-engine OK. Never the scorer alone. |
| Does resume restore the old state? | **No.** Re-resolve. |
| Is DQ reversible? | Only by audited reclaim, then re-resolve. |
| Does the execution feature flag change state? | **No.** |
| Is this implemented today? | Enum + stub only. Full R0–R9 is **MISSING**. |

---

*End of A69. Architecture §§15, 22, 23 implemented as an executable state machine. Default after trade 3 + high score = SHADOW only.*
