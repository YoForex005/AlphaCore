# A48 — Kill switch design: `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN`

**Artifact:** `D:\Prop\reports\swarm\20260818\A48_kill_switch.md`  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Primary sections:** §40 Kill Switch, §59 Authentication and RBAC  
**Supporting sections:** §33–§35, §39, §41–§45, §53, §57, §62–§64, §67 Phase 8, §68, §70.13, §72.18–19  
**Sibling specs (do not contradict):** A23 risk engine, A25 FIX session / flags, A06 API + v1 RBAC, A01 domain inventory, A27 test inventory, A28 Phase 8 gates  
**Date:** 2026-08-18  
**Status:** specification only — **no product source modified**  
**Scope:** destination cTrader/cServer copy book only. Never flatten source MT5. Never treat shadow as live.

---

## 0. Verdict

Architecture §40 names **two controls**. They share a dashboard card and a “kill” metaphor. They do **not** share a flag, a permission, a confirmation, or an effect on open positions.

| Control | What it does | What it must not do |
|---|---|---|
| `STOP_NEW_EXECUTION` | Blocks new copy exposure (`OPEN_EXPOSURE` / `INCREASE_EXPOSURE`). Leaves the destination book **untouched**. | Close, reduce, cancel-all, or send any flatten `NewOrderSingle`. |
| `EMERGENCY_FLATTEN` | Attempts to **close destination positions** (`CLOSE_EXPOSURE`) under stronger authorization and a typed confirmation. | Be an alias of stop-new. Auto-fire from `GLOBAL_STOP`. Run without SuperAdmin + step-up. Blind-retry unknowns. |

§59 lists both as separately authorized mutations. §72.19 requires every manual override to be audited. A single `bool killSwitch` is a **§40 violation**. A mutually exclusive `KillSwitchMode` used as *the* persisted state is the same violation wearing an enum.

Current measured tree (2026-08-18):

| Item | Path / evidence | Class |
|---|---|---|
| Label enum only | `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` (`None`, `StopNewExecution`, `EmergencyFlatten`) | **EXISTS — unsafe if treated as exclusive state** |
| `KillSwitch` aggregate / `IKillSwitch` | not present | **MISSING** |
| `audit_logs` | §45 table; A03 | **MISSING** |
| Auth / roles / policies | `apps/api` (A06) | **MISSING** |
| Risk hot-path consult | `IRiskEngine` (A02/A23) | **MISSING** |
| Flatten mutation | A06: not in first useful version | **NOT IN V1** |

This file is the binding design for later implementation. It does not implement.

---

## 1. Architecture quotes (binding)

### 1.1 §40 Kill Switch (verbatim contract)

```text
STOP_NEW_EXECUTION
```

and a separately permissioned:

```text
EMERGENCY_FLATTEN
```

- Do not conflate them.
- `STOP_NEW_EXECUTION` prevents new copy orders but leaves existing positions untouched.
- `EMERGENCY_FLATTEN` attempts to close destination positions and therefore requires stronger authorization/confirmation.

### 1.2 §59 Authentication and RBAC (verbatim contract)

Dashboard roles:

```text
SuperAdmin
RiskManager
Analyst
ReadOnly
```

Only authorized roles may:

```text
enable real execution
change risk limits
pause/resume trader copying
change symbol mapping
activate stop-new-orders
request emergency flatten
promote a model
change broker/FIX configuration
```

All actions must be audited.

### 1.3 Adjacent law this design must honor

| Clause | Obligation for kill switches |
|---|---|
| §39 | Risk engine is final authority. `GLOBAL_STOP` is a decision that **engages stop-new**, not flatten. |
| §41 | `REAL_COPY_EXECUTION_ENABLED` defaults false. Distinct from both kill controls (A25 §6.5). |
| §33–§34 | Flatten closes persist-before-send with unique `cl_ord_id`. Disconnect → `EXECUTION_STATE_UNKNOWN`. No blind resend. |
| §35 | Flatten only known `destination_position_id`s via `source_destination_links`. |
| §42–§43 | Unresolved dest book / unknown orders are not “fixed” by a second close. |
| §53 | Risk dashboard shows **both** `STOP_NEW_EXECUTION` state **and** `EMERGENCY_FLATTEN` availability. |
| §57 | Structured ids: `correlation_id` plus order/position ids. Never log auth secrets. |
| §62 | TRADE down: do not enqueue an unbounded flatten blast. Alert and stop. |
| §64 | Flatten orders are `CLOSE_EXPOSURE`. Open/increase stay stricter. |
| §68 / §70.13 | Go-live: “kill switch tested” **and** “global stop-new-orders works.” |
| §72.18 | Reduce/close ≠ open-more. |
| §72.19 | Every manual override audited. |

---

## 2. Why two controls exist (do not “simplify”)

Operators will ask for one red button. Refuse.

| Failure if conflated | Why it is unacceptable |
|---|---|
| Panic-stop closes the book | Stop-new is the **safe** first action during a quote gap, unknown state, or “we need to think.” Flattening into a stale/unknown book can open a second disaster. |
| Flatten blocked because stop-new is on | Flatten is how you **exit** after stop-new. A single exclusive enum cannot represent “stop-new ON + flatten ACTIVE.” |
| RiskManager can flatten | §40 “stronger authorization/confirmation.” RiskManager may halt new risk. Closing the live book is SuperAdmin + step-up. |
| Engine daily-loss auto-flatten | `GLOBAL_STOP` (A23 §4.1, §8.3) engages **stop-new only**. Auto-flatten from a threshold is a trading-policy decision the architecture did not authorize. |
| Feature-flag used as kill | `REAL_COPY_EXECUTION_ENABLED` is a deploy/config floor (A25). Kill switches are **runtime** operational levers that must work without a process restart. |
| Shadow / source flattened | Kill switches apply to the **destination execution account**. MT5 source positions are not ours to close. Shadow has no live orders. |

**Invariant:** at any instant the system may be in any combination of `{stop-new off|on}` × `{flatten idle|confirm-pending|active|partial-failed}`. Those four flatten phases never replace the stop-new bit.

---

## 3. Control definitions

### 3.1 `STOP_NEW_EXECUTION`

**Kind:** durable runtime latch.  
**Default:** **on** (fail closed) until an authorized operator explicitly clears it after the venue is `READY_FOR_EXECUTION` **or** off-at-boot with documented seed — see §3.3.  
**Scope:** global destination copy path (the §70.13 “global stop-new-orders”). Not a trader pause. Not a venue-health bit.

**Blocks**

- New `CopyIntent` promotion to live `execution_intent` for `OPEN_EXPOSURE` / `INCREASE_EXPOSURE`.
- Already-persisted live intents in `not_sent` — FIX worker must not send them (A23 §8.1).
- Catch-up of a backlog when the latch is later cleared (§63): clearing stop-new does **not** release expired intents.

**Allows**

- `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` of **already mapped** destination positions, unless an operator policy `allow_risk_reduction_while_stop_new` is false (default **true**).
- Shadow evaluation (no live NOS).
- Ingestion, scoring, dashboard.
- `EMERGENCY_FLATTEN` itself (flatten is not “new copy”).

**Does not**

- Cancel in-flight dest orders.
- Touch `destination_positions`.
- Flip `REAL_COPY_EXECUTION_ENABLED`.
- Change trader lifecycle state (that is `PAUSE_TRADER` / `RISK_BLOCKED`).

**Risk codes (A23 §4.3):** `STOP_NEW_EXECUTION` on blocked open/increase. Decision is `REJECT` or, when the engine itself raises the latch, `GLOBAL_STOP`.

### 3.2 `EMERGENCY_FLATTEN`

**Kind:** separately permissioned **run**, not a boolean.  
**Default:** idle. **Availability** on the dashboard is a computed flag, not a stored “ready to fire” secret.  
**Scope (v1 live path):** the single destination execution account / XAUUSD book this product copies onto. No multi-account fan-out in v1.

**Does**

1. Force `STOP_NEW_EXECUTION = on` if it is off (recorded side-effect `STOP_NEW.ENGAGED_BY_FLATTEN`). One-way safety coupling — **not** identity of the two controls.
2. Snapshot eligible `destination_positions` into `flatten_targets`.
3. Persist one `CLOSE_EXPOSURE` `execution_intent` per target (unique `cl_ord_id`) **before** any socket write.
4. Send only those flatten closes, subject to TRADE logon + lease + known dest position id (A25 §6.5).
5. Record fills / rejects / unknowns. Alert on any target that does not fully close.
6. Remain **active** until every target is `closed` / `failed` / `blocked_unknown` / `skipped`, then enter `completed` or `partial_failed`. **Never auto-clear stop-new.**

**Does not**

- Open or increase any position.
- Close source MT5 or shadow positions.
- Guess quantity from source lots (qty comes from the mapped dest position, §64 / A23 §7).
- Retry a sent-but-unknown close with the same or a new `cl_ord_id` until §34 recovery says another order is required.
- Run when confirm token is missing, expired, reused, or phrase-mismatched.

**Risk codes:** while a run is `active` or `confirm_pending` after confirm, open/increase → `EMERGENCY_FLATTEN_ACTIVE`. Flatten closes themselves are not evaluated as new entries (skip stale-entry / price-move **entry** guards; A23 §8.2). They still require TRADE session + known dest ids.

### 3.3 Boot / seed policy

| Condition | Stop-new | Flatten |
|---|---|---|
| Fresh install, no row | Treat as **ON** (fail closed). First SuperAdmin/RiskManager clear is audited. | Idle |
| Process restart | Reload from PostgreSQL. Redis is a cache only. | Resume in-progress run; do not start a new one |
| `REAL_COPY_EXECUTION_ENABLED=false` | Still meaningful (A06: safe even with execution off). | Mutation **not in first useful version**. If called: 409. When Phase 8 ships: flatten may send reducing NOS **even if** the copy flag is false (A25 §6.5), because it is exit, not new copy. |

Do not store the latch only in memory or only in Redis. PostgreSQL is SoT (§5 stack).

---

## 4. State machines

### 4.1 Stop-new (two-state latch)

```text
OFF  --activate (human RiskManager+|engine GLOBAL_STOP|flatten side-effect)-->  ON
ON   --deactivate (human RiskManager+ only; forbidden if flatten not idle)-->  OFF
```

Illegal:

- Engine auto-clear when daily loss recovers.
- Deactivate while flatten phase ∈ {`confirm_pending`, `active`}.
- Deactivate by Analyst / ReadOnly.
- “Toggle” RPC with no explicit target state (use `activate` / `deactivate`).

### 4.2 Flatten run

```text
idle
  --request (SuperAdmin + step-up session)--> confirm_pending
confirm_pending
  --confirm (valid token + typed phrase)--> active
  --expire / cancel / deny--> idle
active
  --all targets terminal + none failed/blocked--> completed
  --all targets terminal + any failed/blocked--> partial_failed
  --abort (SuperAdmin; unsents cancelled only)--> aborted
completed | partial_failed | aborted
  --acknowledge (SuperAdmin or RiskManager)--> idle
```

`confirm_pending` older than `confirm_ttl` (default **90s**) returns to `idle`. Token is single-use.

While phase ∈ {`confirm_pending` after a successful confirm is N/A, `active`}: treat flatten as **in progress** for open/increase blocking. `confirm_pending` **before** confirm does **not** send orders and does **not** by itself flatten; it **does** still force stop-new on (so a confirmation race cannot open).

### 4.3 Flatten target

```text
eligible → queued → intent_persisted → sent
sent → filled | rejected | unknown
unknown → (reconcile) filled | rejected | needs_replacement | blocked_unknown
queued | intent_persisted → cancelled   (abort before send)
eligible → skipped_zero | blocked_unknown | blocked_unmapped
```

`needs_replacement` may create **one** new `execution_intent` with a **new** `cl_ord_id` only after §34 recovery says the first never landed. Cap: one replacement per target per run unless SuperAdmin re-requests.

---

## 5. Domain model (recommended; do not implement in this task)

Existing `KillSwitchMode` is a **control label**, not runtime state. `None = 0` invites “one mode.” Later coding must not persist `KillSwitchMode` as the single current mode.

### 5.1 Types

```text
Domain/Enums/KillSwitchControl.cs     -- StopNewExecution | EmergencyFlatten
Domain/Enums/FlattenPhase.cs          -- Idle, ConfirmPending, Active, Completed, PartialFailed, Aborted
Domain/Enums/FlattenTargetStatus.cs   -- (see §4.3)
Domain/Enums/KillSwitchActorKind.cs   -- Human, System

Domain/Risk/KillSwitchState.cs        -- stop_new + flatten phase + versions
Domain/Risk/FlattenRun.cs
Domain/Risk/FlattenTarget.cs
Domain/Risk/KillSwitchCommand.cs      -- activate/deactivate/request/confirm/abort/ack

Domain/Platform/AuditLog.cs           -- already named in A01; used here
```

`KillSwitchState` fields (conceptual):

```text
stop_new_execution          bool
stop_new_version            int          -- optimistic concurrency
stop_new_changed_at         timestamptz
stop_new_changed_by         actor_id
stop_new_reason             text
stop_new_source             operator | engine_global_stop | flatten_side_effect

flatten_phase               FlattenPhase
flatten_run_id              uuid?        -- current or last
flatten_version             int
```

Two versions, two columns. Never one `mode`.

### 5.2 Application ports

```text
IKillSwitchQuery
  GetSnapshot(ct) -> KillSwitchSnapshot

IKillSwitchCommands
  ActivateStopNew(cmd)      -- RiskManager+ or System
  DeactivateStopNew(cmd)    -- RiskManager+; 409 if flatten not idle
  RequestFlatten(cmd)       -- SuperAdmin + step-up
  ConfirmFlatten(cmd)       -- SuperAdmin + token + phrase
  AbortFlatten(cmd)         -- SuperAdmin
  AcknowledgeFlatten(cmd)   -- SuperAdmin or RiskManager

IFlattenExecutor            -- owned by execution/FIX worker, not the API
  ProcessActiveRun(runId)

IAuditLog
  Append(entry)             -- same transaction as the state write
```

Risk engine (A23) **reads** `IKillSwitchQuery` at evaluation step 3. It may **request** `ActivateStopNew` on `GLOBAL_STOP` via a domain event → command, not by flipping a static.

FIX worker **re-reads** the snapshot immediately before every NOS write (A25 §6.3). Stop-new or flatten-active blocks open/increase even if risk approved milliseconds earlier.

### 5.3 Who may raise `GLOBAL_STOP`

| Actor | Stop-new activate | Flatten request |
|---|---|---|
| Human `RiskManager` | yes | no |
| Human `SuperAdmin` | yes | yes + step-up |
| System `risk-engine` (daily loss / portfolio DD per A23 §6.4) | yes (source=`engine_global_stop`) | **never** |
| System `fix-worker` / `mt5-worker` | **never** (they honor; they do not own) | **never** |

System actor is a seeded principal `actor_kind=System`, `role=System`. It has no dashboard login. Its actions still land in `audit_logs`.

---

## 6. Persistence

PostgreSQL is authoritative. Redis may cache the snapshot for the hot path and SignalR. Cache invalidation is **after** commit. Cache miss → DB. DB unavailable → fail closed (`DATABASE_UNAVAILABLE` / treat as stop-new ON). Do not execute from a stale Redis “off.”

### 6.1 `kill_switch_state` (singleton row id = 1)

```text
id                              smallint pk check (id = 1)
stop_new_execution              boolean not null
stop_new_version                integer not null
stop_new_changed_at             timestamptz not null
stop_new_changed_by             uuid not null
stop_new_reason                 text not null
stop_new_source                 text not null
flatten_phase                   text not null
flatten_run_id                  uuid null
flatten_version                 integer not null
updated_at                      timestamptz not null
```

### 6.2 `flatten_runs`

```text
flatten_run_id                  uuid pk
requested_at                    timestamptz not null
requested_by                    uuid not null
confirm_token_hash              bytea null          -- store hash only
confirm_expires_at              timestamptz null
confirmed_at                    timestamptz null
confirmed_by                    uuid null
typed_phrase                    text null           -- the expected phrase echo, not a secret
destination_account             text not null
canonical_symbol                text not null       -- v1: XAUUSD
reason                          text not null
phase                           text not null
correlation_id                  uuid not null
started_at / finished_at        timestamptz
target_count / closed_count / failed_count / blocked_count
ack_at, ack_by
```

### 6.3 `flatten_targets`

```text
flatten_target_id               uuid pk
flatten_run_id                  uuid fk
destination_position_id         text not null
destination_account             text not null
side                            text not null
quantity                        numeric not null    -- dest units to close (full)
execution_intent_id             uuid null
cl_ord_id                       text null
replacement_of                  uuid null
status                          text not null
last_error                      text null
updated_at                      timestamptz not null
unique (flatten_run_id, destination_position_id)
```

### 6.4 `audit_logs` (shared §45 table; kill-switch rows use the catalog in §9)

```text
audit_id                        uuid pk
occurred_at                     timestamptz not null
actor_id                        uuid not null
actor_name                      text not null
actor_kind                      text not null       -- Human | System
actor_role                      text not null       -- SuperAdmin | RiskManager | Analyst | ReadOnly | System
action                          text not null       -- dotted catalog
entity_type                     text not null       -- KillSwitch | FlattenRun | FlattenTarget | ExecutionIntent
entity_id                       text not null
before_json                     jsonb null
after_json                      jsonb null
reason                          text null
outcome                         text not null       -- allowed | denied
correlation_id                  uuid not null
request_id                      uuid null
ip                              inet null
user_agent                      text null
step_up_method                  text null           -- password | totp | none
confirm_token_id                uuid null           -- id of token, never raw token
```

**Immutability:** append-only. No `UPDATE` / `DELETE` of audit rows. Corrections are new rows (`action` suffix `.CORRECTION` is not used for kill-switch; denied attempts are first-class rows).

Kill-switch state mutation and its audit row **commit in one transaction**. If audit insert fails, the latch must not move.

### 6.5 Outbox (optional but preferred)

After commit, emit `outbox_events` kinds:

```text
kill_switch_changed
flatten_run_changed
```

Consumers: Redis snapshot publisher, SignalR Risk hub, flatten executor. Do not have the API HTTP thread send FIX.

---

## 7. Hot path (copy → risk → FIX)

Normative with A23 §5 step 3 and A25 §6.3–6.5.

```text
CopyIntent (OPEN/INCREASE)
      ↓
RiskEngine
  1. database available
  2. REAL_COPY_EXECUTION_ENABLED (live path)
  3. KillSwitch snapshot
        stop_new ON            → REJECT / GLOBAL_STOP, code STOP_NEW_EXECUTION
        flatten in progress    → REJECT, code EMERGENCY_FLATTEN_ACTIVE
  4. … remaining A23 checks
      ↓
execution_intent not_sent
      ↓
FIX worker pre-send re-check of the same snapshot
      ↓
NewOrderSingle  or  drop send (status stays not_sent / marked blocked)
```

`REDUCE` / `CLOSE` (non-flatten, source-driven): allowed while stop-new is on (default policy). **Blocked** while a flatten run `active` owns that `destination_position_id` (flatten is the exclusive closer). Other positions: still blocked for open/increase; source-driven close of a position flatten already queued is coalesced (do not double-close).

Flatten closes:

```text
exposure_class = CLOSE_EXPOSURE
flatten_run_id set
skip: quote-age entry, price-move entry, REAL_COPY_EXECUTION_ENABLED
require: TRADE logged on, lease owned, dest position id known, no unresolved unknown on that position
persist execution_intent before send
```

---

## 8. RBAC

Roles are exactly §59. No extra dashboard roles. No “Ops” alias.

### 8.1 Matrix (full design; v1 subset in §11)

| Action | ReadOnly | Analyst | RiskManager | SuperAdmin | System `risk-engine` |
|---|---|---|---|---|---|
| `GET` kill-switch snapshot | yes | yes | yes | yes | n/a |
| See flatten **availability** boolean | yes | yes | yes | yes | n/a |
| See flatten run / target detail | no | no | yes | yes | n/a |
| Activate stop-new | no | no | **yes** | **yes** | **yes** (`GLOBAL_STOP` only) |
| Deactivate stop-new | no | no | **yes** | **yes** | no |
| Request / confirm / abort flatten | no | no | no | **yes + step-up** | no |
| Acknowledge flatten terminal phase | no | no | **yes** | **yes** | no |
| `GET` `audit_logs` (kill-switch + others) | no | no | **yes** | **yes** | n/a |
| Enable real execution (§59, §41) | no | no | no | yes (config + audit; **not** this control) | no |
| Change risk limits | no | no | yes | yes | no |
| Pause / resume trader | no | no | yes | yes | via `PAUSE_TRADER` decision only |

Unauthenticated → 401. Wrong role → **403** and an `*.DENIED` audit row. Flatten while execution path not built / Phase 8 not accepted → **409**. Deactivate stop-new during flatten → **409**. Stale `stop_new_version` / `flatten_version` → **409** conflict.

### 8.2 Policies (ASP.NET names)

```text
Risk.KillSwitch.Read              -- ReadOnly+
Risk.StopNew.Write                -- RiskManager, SuperAdmin
Risk.Flatten.Write                -- SuperAdmin only
Risk.Flatten.Acknowledge          -- RiskManager, SuperAdmin
Audit.Read                        -- RiskManager, SuperAdmin
```

Do not implement flatten write as `RiskManager+`. A06 already records SuperAdmin + step-up.

### 8.3 Step-up confirmation (flatten only)

Stop-new does **not** require step-up. Flatten does. This is the §40 “stronger authorization/confirmation.”

**Protocol**

1. Caller must already be `SuperAdmin` (ordinary session).
2. Caller must present a **fresh step-up**: re-password or TOTP, `max_age = 60s`, bound to `action=EMERGENCY_FLATTEN`.
3. `POST .../emergency-flatten/request` with `reason` (min 10 chars), `destination_account`, optional `canonical_symbol` (default XAUUSD).
4. Server computes expected phrase:

```text
FLATTEN {destination_account} {yyyy-MM-dd} UTC
```

   Date is **server UTC date**, not client-supplied.

5. Server stores **only** `SHA-256(token + server_pepper)` and returns `{ confirm_token, expires_at, expected_phrase_hint }` where the hint is the phrase itself (the operator must type it; the UI may display it). Token TTL **90s**, single use.
6. `POST .../emergency-flatten/confirm` with `{ confirm_token, typed_phrase }`. Constant-time compare of phrase and of token hash.
7. Success → phase `active`, outbox `flatten_run_changed`. Failure (mismatch, expiry, reuse, wrong role) → `denied` audit, phase back to `idle` if token invalid; do not leak which check failed beyond `confirm_invalid`.

**Four-eyes:** not required by §40. Optional later config `flatten_require_second_superadmin` default **false**. Do not block Phase 8 on it.

**UI:** two widgets. Flatten is a destructive modal, never a shared “Kill” toggle. Availability is false when phase ≠ idle, TRADE down, no dest positions, or role ≠ SuperAdmin (availability for ReadOnly is “whether the control exists / is idle,” not a permission grant).

---

## 9. Audit catalog

Every row below is written on both **allowed** and **denied** outcomes (`outcome` column). Denied still records `actor_role` and attempted `action`.

| `action` | When |
|---|---|
| `KILL_SWITCH.STOP_NEW.ACTIVATE` | Human on |
| `KILL_SWITCH.STOP_NEW.DEACTIVATE` | Human off |
| `KILL_SWITCH.STOP_NEW.ENGAGED_BY_ENGINE` | `GLOBAL_STOP` latch |
| `KILL_SWITCH.STOP_NEW.ENGAGED_BY_FLATTEN` | Flatten request side-effect |
| `KILL_SWITCH.STOP_NEW.DENIED` | 403/409 on activate/deactivate |
| `KILL_SWITCH.FLATTEN.REQUEST` | Confirm window opened |
| `KILL_SWITCH.FLATTEN.CONFIRM` | Token + phrase accepted |
| `KILL_SWITCH.FLATTEN.CONFIRM_DENIED` | Bad token/phrase/role/ttl |
| `KILL_SWITCH.FLATTEN.START` | Run entered `active`; targets snapshotted |
| `KILL_SWITCH.FLATTEN.TARGET_ENQUEUED` | Per target (may batch in `after_json`) |
| `KILL_SWITCH.FLATTEN.INTENT_PERSISTED` | `execution_intent` + `cl_ord_id` |
| `KILL_SWITCH.FLATTEN.SENT` | Worker marked sent |
| `KILL_SWITCH.FLATTEN.FILLED` | Dest close filled |
| `KILL_SWITCH.FLATTEN.FAILED` | Reject / send failure |
| `KILL_SWITCH.FLATTEN.BLOCKED_UNKNOWN` | §34; no second NOS yet |
| `KILL_SWITCH.FLATTEN.ABORT` | Unsent cancelled |
| `KILL_SWITCH.FLATTEN.COMPLETE` | All closed |
| `KILL_SWITCH.FLATTEN.PARTIAL` | Terminal with leftovers |
| `KILL_SWITCH.FLATTEN.ACK` | Human returns phase to idle |
| `KILL_SWITCH.FLATTEN.DENIED` | Any other flatten 403/409 |

`before_json` / `after_json` must include `{ stop_new_execution, flatten_phase, flatten_run_id, versions }`. Never include FIX passwords, tokens in raw form, or MT5 manager secrets (§55 / A06).

Structured log fields (§57) on the same events: `correlation_id`, `flatten_run_id`, `execution_intent_id`, `cl_ord_id`, `destination_position_id`, `actor_id`. Redact centrally.

---

## 10. HTTP / dashboard contracts

Aligned with A06 §4.10–4.12; this section is the **full** contract. First useful version ships only the marked subset.

Prefix `/api/v1`. Auth on all `/api/**`. Mutations write `audit_logs`.

| Method | Path | Policy | First useful? | Behavior |
|---|---|---|---|---|
| `GET` | `/api/v1/risk/kill-switch` | `Risk.KillSwitch.Read` | **yes** | Snapshot DTO below |
| `POST` | `/api/v1/risk/stop-new-execution/activate` | `Risk.StopNew.Write` | **yes** | Body `{ reason, expectedVersion }`. Does not flatten. |
| `POST` | `/api/v1/risk/stop-new-execution/deactivate` | `Risk.StopNew.Write` | **yes** | Body `{ reason, expectedVersion }`. 409 if flatten not idle. |
| `POST` | `/api/v1/risk/emergency-flatten/request` | `Risk.Flatten.Write` + step-up | **no** (409/404 until Phase 8) | Opens confirm window |
| `POST` | `/api/v1/risk/emergency-flatten/confirm` | `Risk.Flatten.Write` + token | **no** | Starts run |
| `POST` | `/api/v1/risk/emergency-flatten/abort` | `Risk.Flatten.Write` | **no** | Cancel unsents |
| `POST` | `/api/v1/risk/emergency-flatten/ack` | `Risk.Flatten.Acknowledge` | **no** | Idle after terminal |
| `GET` | `/api/v1/risk/emergency-flatten/{runId}` | `Risk.StopNew.Write` or SuperAdmin | **no** | Run + targets |
| `GET` | `/api/v1/audit/logs` | `Audit.Read` | **yes** (filterable) | ReadOnly denied |

A06’s combined `POST /stop-new-execution` set/clear is **split** here so activate vs deactivate are unambiguous audit actions. Implementers may keep one POST with `desiredState` **only if** `action` in the audit log is still the specific activate/deactivate verb.

### 10.1 Snapshot DTO

```json
{
  "stopNewExecution": true,
  "stopNewVersion": 4,
  "stopNewChangedAt": "2026-08-18T12:00:00Z",
  "stopNewSource": "operator",
  "stopNewReason": "QUOTE gap — halt new copy",
  "emergencyFlattenAvailable": false,
  "flattenPhase": "idle",
  "flattenRunId": null,
  "flattenVersion": 1,
  "realCopyExecutionEnabled": false
}
```

`emergencyFlattenAvailable` is computed:

```text
role == SuperAdmin
AND flattenPhase == idle
AND TRADE session logged on
AND at least one known dest XAU position
AND Phase 8 flatten mutation is shipped
```

For ReadOnly/Analyst/RiskManager the boolean is still returned (so §53 “availability” is visible) but is **false** unless they *could* act — actually §53 is “availability” of the control on the desk, not “this user can press it.” **Binding choice:** expose two fields:

```text
emergencyFlattenAvailable     -- control exists, idle, venue could flatten (desk-level)
emergencyFlattenAllowedForMe  -- AND caller is SuperAdmin with step-up capability
```

v1 (A06): `emergencyFlattenAvailable: false` always; omit or false `emergencyFlattenAllowedForMe`.

### 10.2 Risk dashboard (§53)

Two independent indicators. Never a single traffic light.

```text
STOP_NEW_EXECUTION     ON | OFF     + actor + time + reason
EMERGENCY_FLATTEN      Idle | Confirm pending | Active (n/m closed) | Partial failed | …
```

Partial-failed is an unresolved operational issue (§54 spirit): it must not be silently cleared.

---

## 11. Phasing

| Phase | Stop-new | Flatten |
|---|---|---|
| First useful / §69 (execution off) | Read + activate/deactivate. Durable. Honored by any future live path. | **Not shipped.** GET reports available=false. POST → 409 `execution not enabled` / `flatten not implemented`. |
| Phase 7 (TRADE read/reconcile) | Same. | Still no mutation. |
| Phase 8 (risk-controlled execution) | Required go-live: §68 kill switch tested + §70.13 global stop-new-orders. | Required deliverable “kill switch” (§67) includes flatten **authorization + dry-run/harness**, then live flatten only with production flag + SuperAdmin confirm. |
| After Phase 8 stable | Unchanged | Optional four-eyes, per-symbol scope, cancel-all of *open dest orders* as a distinct command (not this design). |

Enabling flatten does **not** require `REAL_COPY_EXECUTION_ENABLED=true` (A25 §6.5). It **does** require TRADE logon, lease, persist-before-send, and this RBAC.

---

## 12. Flatten executor rules (Phase 8)

1. One active run globally (v1 single dest account).
2. Snapshot positions **once** at `START`. Positions opened after the snapshot cannot exist if stop-new + flatten-active are honored; if they appear (external), they become a reconciliation issue, not silent extra targets.
3. Eligibility:
   - known `destination_position_id`
   - quantity > 0
   - mapped (`source_destination_links` optional for flatten — dest id is enough)
   - no `EXECUTION_STATE_UNKNOWN` on a live dest order for that position → `blocked_unknown`
4. Concurrency: small (default 1–2 in-flight NOS). No blast.
5. TRADE down mid-run: pause sending, keep phase `active`, alert. Do not mark remaining targets failed until abort or timeout policy (`flatten_send_timeout`, measured, not hardcoded here).
6. Rejected close: target `failed`, continue others, terminal `partial_failed`.
7. Unknown after send: §34 recovery only. Alert. No retry storm (A23 §8.2).
8. Abort: cancel `queued` / `intent_persisted` (intent → cancelled, never sent). Leave `sent`/`unknown` to recovery.
9. Metrics: `flatten_runs_total`, `flatten_targets_closed`, `flatten_targets_failed`, `flatten_targets_blocked_unknown` (family of §58 execution metrics).

---

## 13. Tests (must exist before live; names lock to A27)

| Class (A27) | Must prove |
|---|---|
| `Risk.KillSwitchStopNewExecutionTests` | Stop-new blocks `OPEN`/`INCREASE`; dest positions unchanged; `not_sent` not sent; reduce/close still allowed by default. |
| `Risk.KillSwitchEmergencyFlattenAuthorizationTests` | Flatten is a distinct control; RiskManager cannot start it; SuperAdmin without confirm cannot start it; confirm token single-use / TTL. |
| `Harness.GlobalStopNewOrdersTests` | Adapter test mode honors stop-new; zero NOS for new copy (§70.13). |
| `Domain.KillSwitchSeparationTests` (A01) | State is two fields; activating flatten does not clear the need for a separate stop-new record; `KillSwitchMode` is not persisted as exclusive mode. |

**Additional classes this design owns** (add when coding):

| Class | Must prove |
|---|---|
| `Risk.KillSwitchEngineGlobalStopDoesNotFlattenTests` | `GLOBAL_STOP` → stop-new ON, flatten stays idle, no dest close intents. |
| `Risk.KillSwitchDeactivateBlockedDuringFlattenTests` | 409 / domain reject. |
| `Risk.KillSwitchFailClosedWithoutDatabaseTests` | No snapshot → no new send. |
| `Risk.FlattenSkipsUnknownPositionsTests` | Unknown → `blocked_unknown`, no second NOS. |
| `Risk.FlattenPersistsBeforeSendTests` | Intent + audit before builder/send. |
| `Risk.FlattenDoesNotRequireRealCopyFlagTests` | A25 §6.5. |
| `Api.KillSwitchRbacTests` | Matrix in §8.1; denied writes audit. |
| `Api.FlattenStepUpTests` | Phrase / token / role. |
| `Audit.KillSwitchAuditImmutabilityTests` | Append-only; missing audit aborts the latch write. |

Go-live checkboxes this file owns:

```text
[ ] STOP_NEW_EXECUTION does not flatten
[ ] EMERGENCY_FLATTEN permission is distinct (SuperAdmin + confirm)
[ ] GLOBAL_STOP engages stop-new only
[ ] All kill-switch mutations audited (allowed and denied)
[ ] Global stop-new-orders works in FIX harness (§70.13)
[ ] Kill switch tested (§68)
[ ] Flatten unknown-state does not blindly resend (§34)
```

---

## 14. Anti-patterns (reject in review)

```text
[DO NOT] One bool, one enum-as-state, or one “halt trading” RPC that sometimes flattens
[DO NOT] RiskManager flatten, or flatten without typed confirm
[DO NOT] Auto-flatten from daily loss / drawdown / ML
[DO NOT] Auto-clear stop-new when flatten completes or when loss recovers
[DO NOT] Store latch only in Redis or in a worker static
[DO NOT] Flip the latch in the risk hot path without a persisted + audited transition
[DO NOT] Flatten source MT5 or shadow
[DO NOT] Use source lots to size the flatten close
[DO NOT] Blind NewOrderSingle retry on flatten disconnect
[DO NOT] Cancel-all + flatten as an implicit combo without its own command
[DO NOT] Ship flatten mutation before Phase 8 + TRADE reconcile
[DO NOT] Show FIX/MT5 secrets on the confirm modal or in audit JSON
[DO NOT] Treat KillSwitchMode.None as “safe default off”
```

---

## 15. Recommended later file map (not created by this task)

```text
D:\Prop\src\Domain\Enums\KillSwitchControl.cs
D:\Prop\src\Domain\Enums\FlattenPhase.cs
D:\Prop\src\Domain\Risk\KillSwitchState.cs
D:\Prop\src\Domain\Risk\FlattenRun.cs
D:\Prop\src\Domain\Platform\AuditLog.cs
D:\Prop\src\Application\Risk\IKillSwitchQuery.cs
D:\Prop\src\Application\Risk\IKillSwitchCommands.cs
D:\Prop\src\Application\Risk\IFlattenExecutor.cs
D:\Prop\src\Application\Platform\IAuditLog.cs
D:\Prop\apps\api\Endpoints\KillSwitchEndpoints.cs
D:\Prop\tests\Unit\Domain\KillSwitchSeparationTests.cs
D:\Prop\tests\Unit\Risk\KillSwitchStopNewExecutionTests.cs
D:\Prop\tests\Unit\Risk\KillSwitchEmergencyFlattenAuthorizationTests.cs
```

If `KillSwitchMode` remains, document it as **control id only** and stop using `None` as persisted state. Prefer replacing it with `KillSwitchControl` in the same change set that introduces `KillSwitchState`.

---

## 16. Traceability

| Requirement | Design section |
|---|---|
| §40 two controls, do not conflate | §0–§3 |
| §40 stop-new leaves positions | §3.1, §7, §13 |
| §40 flatten closes dest + stronger auth | §3.2, §8.3, §12 |
| §59 roles + authorized list | §8 |
| §59 all actions audited | §6.4, §9 |
| §53 both indicators | §10.2 |
| §39 / A23 `GLOBAL_STOP` | §3.1, §5.3, §13 |
| §41 / A25 feature flags | §3.3, §7, §11 |
| §33–§34 persist / unknown | §4.3, §12 |
| §64 `CLOSE_EXPOSURE` | §3.2, §7 |
| §68 / §70.13 / Phase 8 | §11, §13 |
| A06 v1 API | §10, §11 |
| A27 test names | §13 |

---

## 17. What this artifact did not do

- Did not modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Did not create migrations, endpoints, or tests.
- Did not invent numeric risk limits or production confirm TTLs beyond the design defaults (90s token, 60s step-up) — those remain configuration.
- Did not authorize auto-flatten, multi-account flatten, or cancel-all-open-orders.

**Bottom line:** implement two durable, independently permissioned, fully audited levers. `STOP_NEW_EXECUTION` is the default safe halt. `EMERGENCY_FLATTEN` is a SuperAdmin, step-up, persist-before-send close of known destination positions. Never one flag.
