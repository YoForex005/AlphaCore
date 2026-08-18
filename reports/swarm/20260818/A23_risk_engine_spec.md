# A23 — Risk Engine Specification

**Artifact:** `D:\Prop\reports\swarm\20260818\A23_risk_engine_spec.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary sections:** §32–§40, §62–§65  
**Supporting sections:** §4, §23, §31, §41–§45, §53, §57–§60, §68, §70, §72  
**Date:** 2026-08-18  
**Status:** specification only — no product source modified  
**Scope:** XAUUSD copy path, MT5 sources → CopyIntent → RiskEngine → ExecutionIntent → cTrader/cServer FIX 4.4  

---

## 1. Role and authority

The risk engine is the **final authority** between scoring and execution (§39, §72.15).

Scoring / ML may emit only:

```text
candidate
confidence
suggested allocation
```

They never send FIX, never size destination quantity, and never override a reject / pause / global stop.

Correct production flow (§32, §4, §75):

```text
Source MT5 event
      ↓
Copy candidate?
      ↓
Create CopyIntent          ← persist first
      ↓
RiskEngine evaluates
      ↓
ApprovedExecutionIntent    ← persist before any FIX send
      ↓
FIX Execution Worker
      ↓
NewOrderSingle
      ↓
ExecutionReport(s)
      ↓
Persist fills / order state
      ↓
Update destination position
      ↓
Reconcile
```

Hard rules:

- Never send a FIX order from an MT5 event callback (§32).
- Never send `NewOrderSingle` unless `REAL_COPY_EXECUTION_ENABLED=true` **and** the runtime risk-engine state is healthy (§41).
- Risk-engine rejection happens **before** FIX send (§70.11).
- Fail closed when required state is missing or inconsistent (§62, §42, §70.14).
- Every risk decision is persisted (`risk_decisions` / `risk_events`) and correlated (`risk_decision_id`) (§44, §45, §57).

---

## 2. Exposure class (evaluate first)

Every `CopyIntent` is classified before limits are applied (§35, §64, §72.18). Closing/reducing is a risk-reduction action and **must not** share the same policy as opening/increasing.

| Class | Typical source mapping | Default stance |
|---|---|---|
| `OPEN_EXPOSURE` | new source trade / new destination position | strictest |
| `INCREASE_EXPOSURE` | source scale-in / add to same side | same family as open |
| `REDUCE_EXPOSURE` | source partial close | looser; still needs venue health + identity |
| `CLOSE_EXPOSURE` | source full close | looser; still needs venue health + identity |

Reversals are **not** one event: they are `CLOSE_EXPOSURE` (or `REDUCE_EXPOSURE`) of the existing side, then `OPEN_EXPOSURE` of the opposite side. Do not assume one source event equals one destination order forever (§35).

Stale-entry, price-move, spread, and catch-up expiry apply to **open/increase**. Reduce/close may proceed under a separate policy when the destination position mapping is known (§63 last line, §64).

---

## 3. Inputs

The engine is deterministic. It does not call ML at decision time. It consumes a snapshot assembled by the application layer from durable stores and the live quote cache.

### 3.1 CopyIntent (required)

From §32, §33, §36, §63:

| Field | Purpose |
|---|---|
| `copy_intent_id` | idempotency / correlation |
| `source_broker_id` | identity |
| `source_login` | trader identity |
| `source_trade_id` | reconstructed trade |
| `source_event_id` | exact source event |
| `canonical_symbol` | must be `XAUUSD` (mapped; never raw `55`) |
| `side` | intended destination side |
| `exposure_class` | `OPEN` / `INCREASE` / `REDUCE` / `CLOSE` |
| `source_volume` | raw source volume (not destination qty) |
| `source_price` | source fill / signal price |
| `source_event_time` | venue/source event time |
| `collector_receive_time` | collector clock |
| `decision_time` | clock at risk evaluation |
| `expires_at` | hard expiry (§63) |
| `max_signal_age` | per-intent stale-signal bound (§63) |
| `suggested_allocation` | from scoring; advisory only |
| `confidence` | from scoring; advisory only |
| `trader_state` | `SHADOW` / `LIVE_CANDIDATE` / `LIVE` / `PAUSED` / `RISK_BLOCKED` / … |
| `destination_account` | Pepperstone / cServer account |
| `linked_destination_position_id` | required for reduce/close |

Intents missing required identity fields fail closed (`REJECT`, reason `INTENT_INCOMPLETE`).

### 3.2 Destination quote snapshot (required for open/increase)

From §31, §37:

| Field | Purpose |
|---|---|
| `symbol_id` | cTrader instrument ID |
| `bid` / `ask` | best available destination quote |
| `quote_received_timestamp` | local receive time (mandatory) |
| `venue_timestamp` | if the session provides it |
| `spread` | `ask - bid` (same units as quote) |
| `quote_session_healthy` | QUOTE FIX logon / heartbeat |

Absence of a fresh quote is equivalent to `QUOTE_STALE` / `QUOTE_UNAVAILABLE`.

### 3.3 Destination instrument / account (required for sizing)

From §38, §30:

| Field | Purpose |
|---|---|
| `source_symbol_contract_size` | MT5 contract normalization |
| `destination_quantity_convention` | cTrader units, not “lots = lots” |
| `destination_min_quantity` | floor |
| `destination_step_size` | increment |
| `destination_max_quantity` | instrument cap (if published) |
| `account_leverage` | destination |
| `available_margin` / `equity` / `balance` / `margin_level` | destination account |
| `current_margin_usage` | vs `max execution account margin usage` |

### 3.4 Portfolio / exposure snapshot

From §38, §39, §53:

| Field | Purpose |
|---|---|
| `current_xau_long_qty` | destination |
| `current_xau_short_qty` | destination |
| `current_xau_gross_exposure` | `|long| + |short|` |
| `current_xau_net_exposure` | `long - short` |
| `open_position_count` | destination |
| `open_qty_this_intent_symbol` | per-instrument |
| `trader_open_exposure` | per copied trader |
| `trader_realized_pnl` / `trader_unrealized_pnl` | per selected trader |
| `daily_execution_account_pnl` | destination day P&L |
| `portfolio_drawdown` | vs configured high-water |
| `copy_allocations` | current booked allocations |

### 3.5 Trader / source health

From §22, §23, §18, §39:

| Field | Purpose |
|---|---|
| `trader_state` | live copy only if `LIVE` (or explicit live-candidate gate) |
| `trader_risk_flags` | martingale, averaging-down, abnormal sizing, severe flags |
| `completed_xau_trade_count` | reconstructed lifecycles only |
| `shadow_sample` / `shadow_net_pnl` / `shadow_dd` | live-promotion gate, not per-tick |
| `source_broker_healthy` | MT5 collector not stale |
| `source_data_stale` | §62 MT5 unavailable |

### 3.6 Venue / system health

From §41, §42, §43, §62, §70:

| Field | Purpose |
|---|---|
| `REAL_COPY_EXECUTION_ENABLED` | feature flag; default false |
| `quote_session_state` | connected / logged on |
| `trade_session_state` | connected / logged on |
| `reconciliation_state` | `READY_FOR_EXECUTION` only after successful reconcile |
| `unresolved_execution_states` | any `EXECUTION_STATE_UNKNOWN` |
| `database_available` | fail closed if not |
| `kill_switch_stop_new` | `STOP_NEW_EXECUTION` |
| `kill_switch_flatten` | `EMERGENCY_FLATTEN` in progress (block new opens) |

### 3.7 Configured hard limits

All numeric thresholds are **configuration**, not code constants (§31, §23). They must be measured and changeable by `RiskManager` / `SuperAdmin` with audit (§59). The engine receives the active limit set as an input document.

See §6.

### 3.8 What the engine must **not** take as authoritative

- Suggested destination `OrderQty` from ML or from raw MT5 lots (§38, executive change #10).
- In-memory-only position books when the database is down (§62).
- A “retry the same NewOrderSingle” request after TCP break (§33, §34).
- Blind catch-up batches of expired intents after FIX reconnect (§63).

---

## 4. Outputs

Every evaluation writes one durable `risk_decision` (§44, §45) and returns an application result. Scoring suggested a size; **risk decides the size that may be sent**.

### 4.1 Decision enum (§39)

| Decision | Meaning | Downstream |
|---|---|---|
| `APPROVE` | intent may become `ApprovedExecutionIntent` at computed quantity | persist execution intent, then FIX worker |
| `REDUCE_SIZE` | approved only at a smaller destination quantity | persist execution intent with reduced qty |
| `REJECT` | do not send; intent terminal or expired | no FIX; record reason |
| `PAUSE_TRADER` | reject this intent and stop new copy for this `source_login` | trader state → `PAUSED` or `RISK_BLOCKED` |
| `PAUSE_VENUE` | reject and stop new copy for the destination venue | equivalent to venue-scoped stop-new |
| `GLOBAL_STOP` | reject and engage `STOP_NEW_EXECUTION` | existing positions untouched |

`PAUSE_*` / `GLOBAL_STOP` are **decisions that mutate runtime risk state**. They are not implicit side effects of a reject. State changes are audited.

### 4.2 ApprovedExecutionIntent (only on `APPROVE` / `REDUCE_SIZE`)

Persist **before** send (§33):

```text
execution_intent_id
risk_decision_id
copy_intent_id
cl_ord_id                  ← unique; generated here or by execution service before send
source_broker_id
source_login
source_trade_id
source_event_id
destination_account
canonical_symbol
destination_symbol_id
side
requested_quantity         ← destination units after normalize + step
expected_destination_price
max_slippage
exposure_class
created_at
status                     ← not_sent
```

The FIX worker is the only component that transitions `not_sent` → `sent but acknowledgement unknown`. Risk does not talk to the socket.

### 4.3 Structured reason codes

Stable codes for dashboard, metrics (`risk_rejections_total`), and tests. One decision may carry **multiple** codes; the first blocking code is `primary_reason`.

**Quote / signal / price**

| Code | Typical class |
|---|---|
| `QUOTE_STALE` | open/increase |
| `QUOTE_UNAVAILABLE` | open/increase |
| `SPREAD_TOO_WIDE` | open/increase |
| `PRICE_MOVED_TOO_FAR` | open/increase |
| `SIGNAL_STALE` | open/increase |
| `INTENT_EXPIRED` | open/increase (`expires_at`) |
| `MAX_SLIPPAGE_EXCEEDED` | open/increase |

**Sizing / book**

| Code | Typical class |
|---|---|
| `SIZE_BELOW_MIN` | after normalize/step |
| `SIZE_NOT_MULTIPLE_OF_STEP` | should be fixed by engine, not rejected, unless remainder is 0 |
| `SIZE_REDUCED_TO_LIMIT` | `REDUCE_SIZE` (informational) |
| `MAX_POSITION_QTY` | hard cap |
| `MAX_OPEN_POSITIONS` | hard cap |
| `MAX_XAU_GROSS` | hard cap |
| `MAX_XAU_NET` | hard cap |
| `MAX_MARGIN_USAGE` | hard cap |
| `INSUFFICIENT_MARGIN` | account |
| `ABNORMAL_SIZING_BLOCK` | trader flag / step-up vs last fills |
| `MARTINGALE_BLOCK` | trader flag / sequence |

**Trader / allocation**

| Code | Typical class |
|---|---|
| `TRADER_NOT_LIVE` | state ≠ `LIVE` (or gated candidate) |
| `TRADER_PAUSED` | |
| `TRADER_RISK_BLOCKED` | |
| `MAX_LOSS_PER_TRADER` | |
| `SEVERE_RISK_FLAG` | §23 live gate |
| `CONCENTRATION_CAP` | **Phase 2 only** — see §10 |

**Venue / system / kill**

| Code | Typical class |
|---|---|
| `STOP_NEW_EXECUTION` | all new open/increase |
| `EMERGENCY_FLATTEN_ACTIVE` | block new open/increase |
| `REAL_EXECUTION_DISABLED` | flag off |
| `QUOTE_FIX_UNAVAILABLE` | §62 |
| `TRADE_FIX_UNAVAILABLE` | §62 |
| `SOURCE_STALE` | §62 MT5 |
| `ML_UNAVAILABLE` | does **not** block existing hard limits; blocks *promotion* only |
| `RECONCILIATION_BLOCK` | not `READY_FOR_EXECUTION` |
| `EXECUTION_STATE_UNKNOWN` | unresolved unknown order |
| `DATABASE_UNAVAILABLE` | fail closed |
| `MAPPING_MISSING` | reduce/close without dest position link |
| `INTENT_INCOMPLETE` | |

### 4.4 Telemetry on every decision

Emit for §36 latency chain and §58:

```text
risk_latency
quote_age
signal_age
price_deviation
spread
requested_quantity_in
approved_quantity_out
primary_reason
decision
```

Log identifiers (§57): `correlation_id`, `broker_id`, `source_login`, `source_trade_id`, `copy_intent_id`, `risk_decision_id`, `execution_intent_id`.

---

## 5. Evaluation order (deterministic)

Fail closed on the first **blocking** check. Order is fixed so tests are stable.

1. **Infrastructure:** database available; otherwise no new order (§62).
2. **Feature flag:** `REAL_COPY_EXECUTION_ENABLED` for live path. Shadow path may evaluate the same rules without emitting execution intents.
3. **Kill switch:** `STOP_NEW_EXECUTION` blocks `OPEN`/`INCREASE`. `EMERGENCY_FLATTEN` in progress also blocks `OPEN`/`INCREASE`. Reduce/close remain allowed unless flatten already owns the position (§40, §64).
4. **Reconciliation / unknown state:** not `READY_FOR_EXECUTION` or any unresolved `EXECUTION_STATE_UNKNOWN` → `RECONCILIATION_BLOCK` / `EXECUTION_STATE_UNKNOWN` (§34, §42, §70.14). Do not send another order to “fix” unknown state.
5. **Venue health:** QUOTE down → no new live copy that needs fresh pricing (§62). TRADE down → do **not** queue an unlimited stale-entry backlog; mark intents `TRADE_FIX_UNAVAILABLE` / let them expire (§62, §63).
6. **Source health:** stale MT5 / collector → no new copied positions from stale source data (§62). Do not invent source trades.
7. **Trader eligibility:** state, pause, severe flags, per-trader max loss.
8. **Intent expiry / signal age** (§6.2, §63, §36).
9. **Quote age / spread / price-move / slippage** (§6.1, §37).
10. **Sizing normalize** (§7) → proposed destination quantity.
11. **Book / account hard limits** (§6.3). May `REDUCE_SIZE` down to the binding cap, or `REJECT` if the remainder is below minimum quantity.
12. **Martingale / abnormal sizing** (§6.4).
13. **Concentration** — **not in v1**. Reserved Phase 2 (§10).
14. Persist `risk_decision`. If approved, persist `execution_intent` with `status=not_sent`.

ML unavailable: continue ingestion/reconstruction; **do not promote** new traders to live; existing hard limits stay active (§62).

---

## 6. Hard limits and freshness policies

Limits listed in §39. All are configurable and measured. **Do not hardcode production numbers in this spec** (§23, §31).

### 6.1 Stale quote

Authority: §31, §37, §62 QUOTE, §68 “stale quote rejection works”.

Quote age:

```text
quote_age = decision_time - quote_received_timestamp
```

If a venue timestamp is present, also compute `venue_quote_age` and reject if **either** exceeds `max_quote_age`.

Policy:

```text
if quote missing OR quote session unhealthy:
    reject OPEN/INCREASE  (QUOTE_UNAVAILABLE)

if quote_age > configured_max_quote_age:
    reject OPEN/INCREASE  (QUOTE_STALE)
```

Additionally:

```text
if spread > max_allowed_spread:
    reject OPEN/INCREASE  (SPREAD_TOO_WIDE)
```

`max_quote_age` and `max_allowed_spread` are configuration. XAUUSD around news is the motivating case (§37); the engine does not special-case a news calendar in v1 — spread and quote age are the controls.

Shadow copy uses the same freshness rules so shadow P&L is not marked on rotten quotes (§24).

### 6.2 Stale signal / no blind catch-up

Authority: §36, §63, §72.17, §68 “stale signal rejection works”.

Each source signal carries:

```text
source_event_time
collector_receive_time
decision_time
fix_send_time          ← filled later by execution
execution_time         ← filled later
```

Signal age at decision:

```text
signal_age = decision_time - source_event_time
```

Also enforce `expires_at` on the intent (absolute clock).

```text
if now >= expires_at:                 REJECT  INTENT_EXPIRED
if signal_age > max_signal_age:       REJECT  SIGNAL_STALE
if signal_age > max_source_signal_age:REJECT  SIGNAL_STALE   # global cap
```

`max_signal_age` may be tighter on the intent than the global `max_source_signal_age`; the **stricter** bound wins.

**Catch-up example from §63 (normative):** if TRADE FIX is down for 3 minutes and sources open 20 trades, reconnecting must **not** fire 20 `NewOrderSingle`s. Expired / over-age intents die. Only still-fresh `OPEN`/`INCREASE` intents may be re-evaluated against a live quote.

Measure and store the latency chain even on reject (§36):

```text
MT5 → collector
collector → scoring
risk latency
FIX outbound
cServer ack
fill
total source-to-fill
```

### 6.3 Slippage / price-move guard

Authority: §37.

Before approve, compute deviation among:

```text
expected destination price   ← typically source_price mapped, or last approved mid
current destination quote    ← bid for sell, ask for buy
source price
```

Reject `OPEN`/`INCREASE` when:

```text
|current_quote - expected| > max_tolerated_price_move   → PRICE_MOVED_TOO_FAR
expected slippage > max_slippage                         → MAX_SLIPPAGE_EXCEEDED
```

These are independent of quote age. A fresh but gapped XAU print still fails the move guard.

### 6.4 Book, account, and trader caps

From §39. Binding checks after proposed quantity is known.

| Limit | Action |
|---|---|
| `max loss per selected trader` | `REJECT` + optional `PAUSE_TRADER` |
| `max daily execution-account loss` | `REJECT` + optional `GLOBAL_STOP` |
| `max portfolio drawdown` | `REJECT` + optional `GLOBAL_STOP` |
| `max XAUUSD gross exposure` | `REDUCE_SIZE` or `REJECT` (`MAX_XAU_GROSS`) |
| `max XAUUSD net exposure` | `REDUCE_SIZE` or `REJECT` (`MAX_XAU_NET`) |
| `max position quantity` | `REDUCE_SIZE` or `REJECT` |
| `max number of open positions` | `REJECT` on new `OPEN` (increase of existing may still be allowed) |
| `max execution account margin usage` | `REDUCE_SIZE` or `REJECT` |
| `venue health requirement` | see §5 steps 4–5 |

`REDUCE_SIZE` is allowed only when the reduced quantity still satisfies min/step and still expresses the same `exposure_class` (do not flip a close into a flatten-all here).

### 6.5 Behavioral blocks

| Limit | Action |
|---|---|
| `martingale block` | `REJECT` (`MARTINGALE_BLOCK`); may `PAUSE_TRADER` |
| `abnormal sizing block` | `REJECT` (`ABNORMAL_SIZING_BLOCK`) |

Detection logic lives in the deterministic feature / risk-flag pipeline (§18, §60). The engine consumes flags plus a last-N size series; it does not re-implement full reconstruction.

---

## 7. Position sizing

Authority: §38, executive change #10, §60 unit tests, §68 “position sizing conversion is verified”.

**Never:**

```text
source 0.10 MT5 lots  =  destination OrderQty 0.10
```

Normalized pipeline (the only legal path):

```text
source volume
    ↓
canonical notional / risk
    ↓
portfolio allocation          ← min(suggested_allocation, remaining_caps)
    ↓
destination instrument quantity
    ↓
round down to destination step
    ↓
enforce destination min / max
    ↓
re-check book / margin caps   ← may reduce again
```

Inputs to the sizing function (§38):

```text
source symbol contract size
destination symbol quantity convention
destination minimum quantity
destination step size
account leverage
available margin
risk allocation
current XAU exposure
trader confidence             ← advisory scale, not a bypass
```

Output of sizing is a destination `requested_quantity` plus:

```text
sizing_path = APPROVE | REDUCE_SIZE | REJECT
remainder_discarded
binding_cap                   ← which limit bound the size
```

If rounding/caps produce `qty < min` → `REJECT` / `SIZE_BELOW_MIN` (do not send a non-tradable order).

Unit tests must use **real known source/destination examples** before any live execution (§38). That test fixture is a go-live gate (§68).

Sizing applies to `OPEN`/`INCREASE`. `REDUCE`/`CLOSE` quantity comes from the **mapped destination position** (partial or full), not from re-running source lots through allocation (§35, §64).

---

## 8. Kill switches

Authority: §40, §53, §59, §70.13, §68 “kill switch tested”.

**Do not conflate.** Two controls, two permissions, two effects.

| Control | Effect | Positions | Permission |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | no new copy orders (`OPEN`/`INCREASE`) | **untouched** | `activate stop-new-orders` — `RiskManager` or `SuperAdmin` |
| `EMERGENCY_FLATTEN` | attempt to close destination positions | **reduced/closed** | separately permissioned; stronger confirmation |

### 8.1 `STOP_NEW_EXECUTION`

- Runtime flag, durable, visible on the Risk dashboard (§53).
- Risk engine returns `REJECT` / `GLOBAL_STOP` with code `STOP_NEW_EXECUTION` for `OPEN`/`INCREASE`.
- Already-persisted `execution_intent` in `not_sent` must not be sent while the flag is on.
- Intents already `sent` are not cancelled by this flag (that is flatten or explicit cancel).
- Reduce/close of existing mapped positions **may** still be approved (risk-reduction, §64) unless an operator policy disables that.

### 8.2 `EMERGENCY_FLATTEN`

- Not a silent alias of stop-new.
- Requires stronger authorization and confirmation (§40, §59).
- All flatten actions are audited.
- While flatten is active: block `OPEN`/`INCREASE`; flatten orders themselves are `CLOSE_EXPOSURE` and skip stale-entry / price-move **entry** guards, but still require TRADE session + known destination position IDs.
- Failure to flatten a position is an alert, not an implicit retry storm (§34).

### 8.3 Engine-raised stops

The engine may *request* `GLOBAL_STOP` or `PAUSE_VENUE` when daily loss or portfolio drawdown breaches. Engagement of the durable kill-switch record is an audited state transition, not a boolean flipped inside the hot path without persistence.

### 8.4 Dashboard / RBAC

Risk dashboard shows both `STOP_NEW_EXECUTION` state and `EMERGENCY_FLATTEN` availability (§53). Only authorized roles may activate them; all actions audited (§59).

---

## 9. Failure rules (fail closed)

Normative summary of §62 as risk-engine obligations:

| Failure | New OPEN/INCREASE | Other |
|---|---|---|
| MT5 unavailable | reject (`SOURCE_STALE`); do not invent trades | expose stale-source status; keep retrying collector |
| ML unavailable | do not *promote* to live | ingestion continues; hard limits stay on |
| QUOTE FIX unavailable | reject (`QUOTE_FIX_UNAVAILABLE` / `QUOTE_UNAVAILABLE`) | no live copy that needs fresh pricing |
| TRADE FIX unavailable | do not enqueue unbounded stale entries; mark and expire | do not resend unknown orders |
| Database unavailable | fail closed; no new orders | no real execution from volatile memory only |
| Reconciliation inconsistent | reject (`RECONCILIATION_BLOCK`) | startup path: block until `READY_FOR_EXECUTION` (§42) |

---

## 10. Copy correlation / concentration — Phase 2 note

Authority: **§65** (explicit deferral).

> Do not copy 50 “different traders” if they are effectively the same XAUUSD strategy.  
> Add concentration caps.  
> **This can be Phase 2 after basic copy execution is stable.**

### 10.1 What this is *not*

This is **not** Engineering Phase 2 in §67 (that phase is XAUUSD reconstruction).  
§65 “Phase 2” means **after basic copy execution is stable** — i.e. after Phase 8 risk-controlled execution works, kill switches work, and sizing/stale guards are proven.

**v1 / first live path must not block on clustering.** Do not refuse copy solely for lack of a correlation graph. Reason code `CONCENTRATION_CAP` is reserved and unused until this phase is switched on.

### 10.2 Phase 2 intent (do not implement now)

When basic copy is stable, add a concentration layer **inside** the risk engine (same authority, same persist-before-send), not a second bypassable filter.

Track correlation by (§65):

```text
direction
entry time
holding time
return series
session
lot behavior
```

Cap:

```text
maximum allocation per correlated strategy cluster
```

Suggested (future) inputs:

- cluster id assigned by an offline/batch correlator (not ML-in-the-hot-path)
- current cluster gross / net XAU
- `max_allocation_per_cluster`

Suggested (future) outputs:

- `REDUCE_SIZE` to remaining cluster budget, or
- `REJECT` / `CONCENTRATION_CAP`

Per-trader and book-level XAU caps in §6.4 remain the v1 concentration substitute (they bound the account even if 50 lookalikes fire together, but they do **not** distinguish clusters).

### 10.3 Phase 2 exit criteria (when to schedule the work)

Only after:

- copy intents are idempotent
- stale quote/signal rejection is tested
- sizing conversion is verified
- kill switches are tested
- shadow sample exists  
(§68)

Then implement clustering behind a config flag default **off**.

---

## 11. Persistence, UI, tests, go-live

### 11.1 Tables (already named in architecture)

`copy_intents`, `copy_allocations`, `risk_decisions`, `risk_events`, `execution_intents`, `destination_quotes`, `destination_positions`, `source_destination_links`, `trader_risk_flags`, `audit_logs` (§44, §45).

### 11.2 Dashboard (§53)

Must show: equity/balance/free margin/margin level, daily P&L, drawdown, XAU long/short/net, risk by trader and by source broker, rejected intents + reasons, `STOP_NEW_EXECUTION` state, `EMERGENCY_FLATTEN` availability.

### 11.3 Tests required before live (§60, §68, §70)

Unit / integration:

- each hard limit in isolation
- stale quote reject (`quote_age > max_quote_age`)
- stale signal reject and `expires_at`
- no blind 20-intent catch-up after a 3-minute FIX gap
- `PRICE_MOVED_TOO_FAR` / `SPREAD_TOO_WIDE`
- source lots ≠ destination qty (known conversion fixtures)
- `REDUCE_SIZE` vs hard `REJECT` when below min
- `OPEN`/`INCREASE` stricter than `REDUCE`/`CLOSE`
- `STOP_NEW_EXECUTION` does not flatten
- `EMERGENCY_FLATTEN` permission is distinct
- reconciliation / unknown-state blocks send
- risk rejection occurs with **zero** FIX outbound

Go-live checkboxes that this spec owns (§68, §70):

```text
[ ] position sizing conversion is verified
[ ] risk engine unit/integration tests pass
[ ] stale quote rejection works
[ ] stale signal rejection works
[ ] kill switch tested
[ ] risk-engine rejection happens before FIX send
[ ] global stop-new-orders works
[ ] reconciliation blocks execution while inconsistent
```

Concentration clustering is **not** a v1 go-live checkbox.

---

## 12. Explicit non-goals (this document)

- No product source changes.
- No hardcoded production thresholds.
- No Kafka / mesh / extra venue (§71).
- No ML inside the engine; ML never bypasses risk (§72.15).
- No cluster concentration enforcement until Phase 2 as defined in §10.
- No writing MQ5 / EX5 / any path outside this artifact.

---

## 13. Traceability

| Topic | Architecture |
|---|---|
| Pipeline, persist-before-FIX | §32, §4, §75 |
| Execution intent identity / no blind retry | §33, §34 |
| Position mapping / scale-in / close / reverse | §35 |
| Signal timestamps / latency / stale entries | §36, §63, §72.17 |
| Quote freshness, spread, price-move | §31, §37 |
| Sizing layer | §38 |
| Decisions + hard limits | §39 |
| Kill switches | §40, §53, §59 |
| Feature flag + healthy risk state | §41 |
| Reconcile-before-ready | §42, §43, §70.14 |
| Failure / fail-closed | §62 |
| No blind catch-up | §63 |
| Exposure-class policy | §64, §72.18 |
| Concentration deferred | **§65 Phase 2 note** |
| Live execution delivery | §67 Phase 8 |
| Gates | §68, §70 |
)
