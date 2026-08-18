# A41 — PostgreSQL transactional outbox design

| Field | Value |
|---|---|
| Agent | A41 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A41_outbox_design.md` |
| Product source edited | **No** (design only) |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§4, 12, 13, 22–24, 32–36, 39–45, 57–63, 66–72 |
| Adjacent specs | A02 (Application ports), A03 (Infrastructure / Redis authority), A06 (health + SignalR), A07 (mt5-worker), A23 (risk engine), A27 (tests), A28 (phases) |
| Domain enum already present | `D:\Prop\src\Domain\Enums\OutboxEventType.cs` |
| Broker | **None.** No Kafka, no NATS, no Rabbit, no Azure Service Bus. |

**Status:** specification. Do not treat this file as implemented schema. `outbox_events` is still **MISSING** in product source (A01, A03). Kafka is correctly absent (§13, §71).

---

## 1. Purpose

Replace a message broker with a **PostgreSQL transactional outbox** so MT5 ingest, reconstruction, scoring, shadow copy, risk, and dashboard notifications stay **crash-safe and decoupled**.

Architecture §12 live path (binding):

```text
MT5 event
   ↓
validate
   ↓
deduplicate
   ↓
persist raw record
   ↓
write transactional outbox event
   ↓
commit
```

Then **background workers** drain the outbox. This is the only legal way to leave the MT5 callback.

Architecture §13 event kinds (binding, five only for v1):

```text
trade-completed events
score-update requests
shadow-copy intents
risk-check requests
notification events
```

Existing C# enum (do not invent a second vocabulary):

```1:10:D:\Prop\src\Domain\Enums\OutboxEventType.cs
namespace TraderIntelligence.Domain.Enums;

public enum OutboxEventType
{
    TradeCompleted = 0,
    ScoreUpdate = 1,
    ShadowCopyIntent = 2,
    RiskCheckRequest = 3,
    NotificationEvent = 4
}
```

Wire / SQL names stay kebab-case as written in §13. Map 1:1:

| `OutboxEventType` | SQL / JSON `event_type` |
|---|---|
| `TradeCompleted` | `trade-completed` |
| `ScoreUpdate` | `score-update` |
| `ShadowCopyIntent` | `shadow-copy-intent` |
| `RiskCheckRequest` | `risk-check-request` |
| `NotificationEvent` | `notification-event` |

---

## 2. Non-goals (binding)

| Do not | Why |
|---|---|
| Introduce Kafka (or any broker) | §13, §71, §72.20. Migrate later **behind** `IEventBus` only if measured throughput requires it. |
| Use Redis as outbox authority | A03: Redis-first trading outbox is **UNSAFE**. Redis may cache scores / relay SignalR, never own orders, positions, balances, copy/risk/execution state. |
| Reuse `mt5-sdk` “Redis fast outbox” | `D:\Prop\mt5-sdk\src\services\metrics_service.h` (`terminal_fast_outbox_*`) is a **different tree**. Do not port it into `src/Infrastructure` as the §13 bus. |
| Call scoring, ML, or FIX from the MT5 callback | §12, §32, §72.6. Callback writes raw + outbox and returns. |
| Drive `NewOrderSingle` from outbox retry | At-least-once redelivery + a send is how you double-fire live gold. FIX send is a **separate** `execution_intents` poller with persist-before-send (§33, §72.8–9). |
| Emit shadow/risk outbox rows for historical backfill | §63 no blind catch-up. Backfill persists raw / reconstructs; it does not enqueue stale copy or risk work. |
| Overload `outbox_events` as `ingestion_events`, `audit_logs`, or `system_events` | A03: keep those tables distinct. Outbox is a **delivery queue**, not an audit log. |
| Hand-write MQ5 / mutate product source from this agent | This file is the only output. |

---

## 3. Measured current state (honest)

| Capability | Class | Evidence |
|---|---|---|
| `outbox_events` table / migration | **MISSING** | A03: 0 migrations, 0 `DbSet` |
| `IOutboxWriter` / `ITransactionalOutbox` | **MISSING** | A02 O6 |
| `IOutboxProcessor` | **MISSING** | A02 O8; `apps/mt5-worker/Worker.cs` and `apps/fix-worker/Worker.cs` are template `Task.Delay(1000)` loops |
| Five payload contracts | **MISSING** | A02 O7; only the enum exists |
| `IEventBus` seam | **MISSING** | A02 O9 |
| Poison / retry columns | **MISSING** | A03 §6 |
| Idempotency unique index | **MISSING** | A03 |
| `mt5_outbox_backlog` | **MISSING** | §58 |
| Integration `OutboxProcessingTests` | **MISSING** | A27 inventory only |
| Kafka | correctly absent | **EXISTS_AND_GOOD** |

Infrastructure already references `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 — the right stack for this design. There is no DbContext yet.

---

## 4. Guarantees

| Guarantee | How |
|---|---|
| Atomic produce | Producer inserts domain row(s) **and** `outbox_events` in the **same** PostgreSQL transaction. Crash before commit → neither exists. Crash after commit → row is durable and will be claimed. |
| At-least-once delivery | Poller claims with `FOR UPDATE SKIP LOCKED`, increments `attempt_count`, processes, then marks `processed` in a **second** transaction. Crash after side-effect and before ack → redelivery. |
| Producer idempotency | Unique `(event_type, idempotency_key)`. Duplicate enqueue is a no-op (`ON CONFLICT DO NOTHING`). |
| Consumer idempotency | Handler uses domain unique keys **and** `outbox_handler_receipts`. Re-delivery must not create a second score history row, second `CopyIntent`, second `RiskDecision`, or second `execution_intent`. |
| Time-sensitive expiry | `shadow-copy-intent` and `risk-check-request` carry `expires_at`. Claim of an expired row marks `expired` and **does not** invoke shadow or risk. This is policy, not poison. |
| Poison isolation | After `max_attempts` transient failures, row becomes `poisoned` and is copied to `outbox_poison_events`. It leaves the poll index. Manual replay only. |
| No blind FIX retry | Outbox never sends FIX. `APPROVE` / `REDUCE_SIZE` persist `execution_intents.status = not_sent`. Only the FIX worker, after reconcile + feature flag, may send. |

Delivery is **not** exactly-once. Exactly-once is approximated by **unique idempotency keys on both sides**.

---

## 5. End-to-end event graph

```text
MT5 callback / live ingest (same TX)
  persist mt5_deals / orders / positions
  persist ingestion_events
  commit
        │
        │  (reconstruction worker — not the callback)
        ▼
reconstructed_trades (same TX as outbox)
  + outbox trade-completed          ──► mt5-worker
        │
        ├─ if completed XAUUSD ──► outbox score-update          ──► mt5-worker
        │                              │
        │                              ├─ trader_scores + trader_score_history
        │                              └─ outbox notification-event (state/score)
        │
        ├─ if trader in SHADOW / LIVE_CANDIDATE / LIVE
        │     persist copy_intents (expires_at, max_signal_age)
        │     + outbox shadow-copy-intent                      ──► mt5-worker
        │            │
        │            ├─ SHADOW: persist shadow_orders / fills / positions / performance
        │            └─ LIVE / gated LIVE_CANDIDATE:
        │                  + outbox risk-check-request         ──► fix-worker
        │                         │
        │                         ├─ persist risk_decisions
        │                         ├─ APPROVE / REDUCE_SIZE:
        │                         │     persist execution_intents (not_sent)
        │                         │     (FIX poller — NOT outbox)
        │                         └─ PAUSE_* / GLOBAL_STOP: mutate risk state + audit
        │
        └─ outbox notification-event                           ──► mt5-worker
               persist system_events
               optional Redis pub/sub → API OpsHub
```

Hard splits:

1. **Ingest TX** writes raw MT5 only (plus optional `ingestion_events`). It does **not** enqueue `score-update`, `shadow-copy-intent`, or `risk-check-request`. Reconstruction decides those after a logical trade is known.
2. **Reconstruction TX** may enqueue `trade-completed` (and, if the trade is already known complete in that same commit, `score-update` + `notification-event`). Prefer **one** `trade-completed` and let its handler enqueue the rest so the callback/reconstruction path stays small.
3. **Shadow handler** persists `copy_intents` if the reconstruction path did not. Live path then enqueues `risk-check-request` in the **same TX** as that persist.
4. **Risk handler** never talks to the FIX socket (A23 §4.2).
5. **FIX Execution Worker** polls `execution_intents` (`status = not_sent`), not `outbox_events`.

---

## 6. Schema

All objects live in schema `public` unless a later migration ADR pins `trading`. Use versioned EF migrations only (§72.3). Names below are the pin for that first outbox migration.

### 6.1 Status vocabulary

```text
pending      claimable
processing   claimed under lease
processed    handler succeeded (terminal)
expired      past expires_at or max_signal_age; not a failure
poisoned     exceeded max_attempts or classified permanent (terminal until replay)
cancelled    producer/operator voided before process (rare; kill-switch drain)
```

Do not use `failed` as a stored status. Failures stay `pending` with `next_attempt_at` in the future, or become `poisoned`.

### 6.2 `outbox_events`

```sql
CREATE TABLE outbox_events (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),

    event_type              text NOT NULL,
    payload_schema_version  integer NOT NULL DEFAULT 1,
    idempotency_key         text NOT NULL,

    aggregate_type          text NOT NULL,
    aggregate_id            uuid NOT NULL,

    broker_id               uuid NULL,
    source_login            bigint NULL,
    source_trade_id         uuid NULL,
    source_event_id         uuid NULL,
    copy_intent_id          uuid NULL,
    risk_decision_id        uuid NULL,
    execution_intent_id     uuid NULL,

    correlation_id          uuid NOT NULL,
    causation_id            uuid NULL,          -- parent outbox id when chained

    payload                 jsonb NOT NULL,

    status                  text NOT NULL DEFAULT 'pending',
    attempt_count           integer NOT NULL DEFAULT 0,
    max_attempts            integer NOT NULL DEFAULT 8,
    next_attempt_at         timestamptz NOT NULL DEFAULT now(),
    available_at            timestamptz NOT NULL DEFAULT now(),
    expires_at              timestamptz NULL,

    locked_until            timestamptz NULL,
    locked_by               text NULL,          -- "{hostname}:{pid}:{instance_guid}"

    last_error              text NULL,
    last_error_class        text NULL,          -- transient | permanent | expired | handler
    last_attempt_at         timestamptz NULL,
    processed_at            timestamptz NULL,

    occurred_at             timestamptz NOT NULL,
    created_at              timestamptz NOT NULL DEFAULT now(),
    updated_at              timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT ck_outbox_events_type CHECK (event_type IN (
        'trade-completed',
        'score-update',
        'shadow-copy-intent',
        'risk-check-request',
        'notification-event'
    )),
    CONSTRAINT ck_outbox_events_status CHECK (status IN (
        'pending', 'processing', 'processed', 'expired', 'poisoned', 'cancelled'
    )),
    CONSTRAINT ck_outbox_events_attempts CHECK (
        attempt_count >= 0 AND max_attempts >= 1 AND attempt_count <= max_attempts + 1
    ),
    CONSTRAINT ck_outbox_events_payload_object CHECK (jsonb_typeof(payload) = 'object'),
    CONSTRAINT uq_outbox_events_idempotency UNIQUE (event_type, idempotency_key)
);

CREATE INDEX ix_outbox_events_claim
    ON outbox_events (event_type, next_attempt_at, created_at)
    WHERE status = 'pending';

CREATE INDEX ix_outbox_events_lease
    ON outbox_events (locked_until)
    WHERE status = 'processing';

CREATE INDEX ix_outbox_events_backlog
    ON outbox_events (status, event_type, created_at);

CREATE INDEX ix_outbox_events_correlation
    ON outbox_events (correlation_id);

CREATE INDEX ix_outbox_events_source
    ON outbox_events (broker_id, source_login, occurred_at DESC)
    WHERE broker_id IS NOT NULL;

CREATE INDEX ix_outbox_events_copy_intent
    ON outbox_events (copy_intent_id)
    WHERE copy_intent_id IS NOT NULL;

CREATE INDEX ix_outbox_events_poisoned
    ON outbox_events (updated_at DESC)
    WHERE status = 'poisoned';
```

`updated_at` is maintained by a `BEFORE UPDATE` trigger (`NEW.updated_at = now()`). Do not rely on application clocks for lease expiry.

### 6.3 `outbox_handler_receipts` (consumer inbox)

One row per successful `(handler_name, idempotency_key)`. Required because at-least-once will re-enter a handler after a crash between side-effect and `processed`.

```sql
CREATE TABLE outbox_handler_receipts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    outbox_event_id     uuid NOT NULL REFERENCES outbox_events (id),
    handler_name        text NOT NULL,
    event_type          text NOT NULL,
    idempotency_key     text NOT NULL,
    correlation_id      uuid NOT NULL,
    processed_at        timestamptz NOT NULL DEFAULT now(),
    result_json         jsonb NOT NULL DEFAULT '{}'::jsonb,

    CONSTRAINT uq_outbox_handler_receipts UNIQUE (handler_name, idempotency_key)
);

CREATE INDEX ix_outbox_handler_receipts_event
    ON outbox_handler_receipts (outbox_event_id);
```

v1: exactly one handler per `event_type`. Receipts still exist so a future second consumer (metrics projector, warehouse) can share the table without a broker.

### 6.4 `outbox_poison_events` (append-only snapshot)

```sql
CREATE TABLE outbox_poison_events (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    outbox_event_id     uuid NOT NULL REFERENCES outbox_events (id),
    event_type          text NOT NULL,
    idempotency_key     text NOT NULL,
    payload             jsonb NOT NULL,
    attempt_count       integer NOT NULL,
    last_error          text NOT NULL,
    last_error_class    text NOT NULL,
    correlation_id      uuid NOT NULL,
    broker_id           uuid NULL,
    source_login        bigint NULL,
    copy_intent_id      uuid NULL,
    poisoned_at         timestamptz NOT NULL DEFAULT now(),
    replayed_at         timestamptz NULL,
    replayed_by         text NULL
);

CREATE INDEX ix_outbox_poison_open
    ON outbox_poison_events (poisoned_at DESC)
    WHERE replayed_at IS NULL;
```

Poison snapshot is **not** deleted on replay. `replayed_at` is set; a new attempt happens on the live `outbox_events` row.

### 6.5 What this is not

| Table | Role vs outbox |
|---|---|
| `ingestion_events` | Raw collector audit of MT5 pump messages. Survives even when no outbox row is written (backfill). |
| `audit_logs` | Human/RBAC actions (§59). Never written by the poller. |
| `system_events` | Operational facts the dashboard lists (`GET /api/v1/system/events`). Notification handler **inserts** here; it does not replace outbox. |
| `execution_intents` | Persist-before-send ledger for FIX. Not an outbox partition. |
| `copy_intents` | Domain intent with `expires_at` / `max_signal_age`. Outbox only **points** at it. |

---

## 7. Idempotency keys

Keys are **deterministic**, ASCII, lowercase, colon-separated. They must be computable from durable identity **without** a new Guid. Producer and consumer use the same string.

Never put wall-clock timestamps or `correlation_id` into the key (that defeats dedupe).

### 7.1 Producer keys (`outbox_events.idempotency_key`)

| `event_type` | Key | Dedupes |
|---|---|---|
| `trade-completed` | `trade-completed:{broker_id:N}:{reconstructed_trade_id:N}` | Second completion write for the same logical trade |
| `score-update` | `score-update:{broker_id:N}:{login}:{trigger}:{trigger_id}` | Same trigger (trade close, backfill watermark, manual) |
| `shadow-copy-intent` | `shadow-copy-intent:{broker_id:N}:{login}:{source_event_id:N}:{exposure_class}` | Same source event + exposure class (§35 reversals are two events / two keys) |
| `risk-check-request` | `risk-check-request:{copy_intent_id:N}` | One risk evaluation per `CopyIntent`. A re-check is a **new** intent or `…:rev{n}` written explicitly |
| `notification-event` | `notification-event:{kind}:{subject_type}:{subject_id}:{dedupe_bucket}` | Dashboard spam (see §7.3) |

`{broker_id:N}` is the Guid in **lowercase hex with hyphens** (EF default). `{login}` is the decimal MT5 login. `{exposure_class}` is one of `open-exposure`, `increase-exposure`, `reduce-exposure`, `close-exposure` (maps `CopyIntentAction`).

`score-update` `{trigger}` values (closed set):

```text
after-trade          trigger_id = reconstructed_trade_id
backfill-watermark   trigger_id = sync_checkpoint_id
manual               trigger_id = audit/command id
model-refresh        trigger_id = model_version_id   (Phase 6 only)
```

### 7.2 Domain unique indexes the handlers rely on

Outbox uniqueness is not enough. Handlers must hit **domain** uniqueness so a crash-after-write is a no-op:

| Table | Unique | Notes |
|---|---|---|
| `mt5_deals` | `(broker_id, deal_ticket)` | §10 compound identity |
| `reconstructed_trades` | `id` (and business: `(broker_id, position_ticket, opened_at)` once reconstruction pins it) | Completions flip `is_completed`; they do not insert a second trade |
| `copy_intents` | `(source_broker_id, source_event_id, exposure_class)` | A27 `CopyIntentIdempotencyTests` |
| `risk_decisions` | `(copy_intent_id)` | One decision per intent (A23). Revisions are a new intent |
| `execution_intents` | `cl_ord_id` **and** `(copy_intent_id)` | Persist-before-send §33 |
| `trader_scores` | `(broker_id, login)` | One current score row |
| `trader_score_history` | `(trader_score_id, trigger, trigger_id)` | History is append-only |
| `system_events` | optional `(kind, subject_id, dedupe_bucket)` | Stops SignalR floods on redelivery |

### 7.3 Notification dedupe buckets

Notifications are lossy **to humans**, not to the ledger. Use a coarse bucket:

| `kind` | `subject_type` | `dedupe_bucket` |
|---|---|---|
| `trade.completed` | `reconstructed_trade` | `once` (subject_id is the trade) |
| `score.changed` | `trader` | `{score_history_id}` |
| `trader.state.changed` | `trader` | `{from_state}:{to_state}:{score_history_id}` |
| `risk.rejected` | `copy_intent` | `once` |
| `risk.approved` | `copy_intent` | `once` |
| `outbox.poisoned` | `outbox_event` | `once` |
| `ingest.stale` | `broker` | `{utc_date}/{utc_hour}` |
| `fix.session` | `fix_session` | `{session_event_id}` |

Do not emit a notification per MT5 tick or per deal.

### 7.4 Enqueue SQL (producer, same TX as domain write)

```sql
INSERT INTO outbox_events (
    event_type, payload_schema_version, idempotency_key,
    aggregate_type, aggregate_id,
    broker_id, source_login, source_trade_id, source_event_id, copy_intent_id,
    correlation_id, causation_id,
    payload, expires_at, occurred_at
) VALUES (
    @event_type, @schema_version, @idempotency_key,
    @aggregate_type, @aggregate_id,
    @broker_id, @source_login, @source_trade_id, @source_event_id, @copy_intent_id,
    @correlation_id, @causation_id,
    @payload::jsonb, @expires_at, @occurred_at
)
ON CONFLICT (event_type, idempotency_key) DO NOTHING;
```

`ON CONFLICT DO NOTHING` is the producer idempotency path. Do **not** update payload on conflict — the first durable payload wins. If the payload was wrong, write a **new** key (new intent / revision) or fix via an audited operator action.

After commit, optionally:

```sql
SELECT pg_notify('outbox_events', @event_type);
```

NOTIFY is a **wake-up hint**. Polling remains the correctness path (a missed NOTIFY cannot lose work).

---

## 8. Polling and claim

### 8.1 Why polling (not Kafka, not NOTIFY-only)

- ~5,000 accounts (§13) do not justify a broker.
- `LISTEN/NOTIFY` is dropped if no listener is connected; it cannot be the log.
- `FOR UPDATE SKIP LOCKED` lets several worker instances share one table without a coordinator.
- Lease reclaim recovers a worker that died mid-handler.

### 8.2 Claim batch (one transaction)

```sql
WITH claimed AS (
    SELECT id
    FROM outbox_events
    WHERE status = 'pending'
      AND event_type = ANY (@types)          -- worker allow-list
      AND next_attempt_at <= now()
      AND available_at    <= now()
    ORDER BY
      CASE event_type
        WHEN 'risk-check-request'  THEN 0
        WHEN 'shadow-copy-intent'  THEN 1
        WHEN 'trade-completed'     THEN 2
        WHEN 'score-update'        THEN 3
        WHEN 'notification-event'  THEN 4
      END,
      created_at
    LIMIT @batch_size
    FOR UPDATE SKIP LOCKED
)
UPDATE outbox_events AS o
SET status          = 'processing',
    attempt_count   = o.attempt_count + 1,
    last_attempt_at = now(),
    locked_until    = now() + (@lease_seconds * interval '1 second'),
    locked_by       = @worker_id,
    updated_at      = now()
FROM claimed
WHERE o.id = claimed.id
RETURNING o.*;
```

Notes:

- **Do not filter `expires_at` in the claim predicate.** Expired rows must still be claimed so they can be marked `expired` and leave the backlog (otherwise `mt5_outbox_backlog` lies and §63 catch-up remains sitting in `pending`).
- Priority is **in-SQL** so a mixed-type worker still drains risk first. Dedicated workers (recommended) pass a one-element `@types` array; the CASE still documents the global order.
- `ORDER BY created_at` is FIFO within a type. Do not order by `attempt_count` (that starves healthy rows).
- Default `@batch_size = 32`, `@lease_seconds = 30`.

### 8.3 Lease reclaim (same poller, before claim)

```sql
UPDATE outbox_events
SET status       = 'pending',
    locked_until = NULL,
    locked_by    = NULL,
    last_error_class = COALESCE(last_error_class, 'transient'),
    last_error   = CONCAT('lease expired; last owner=', COALESCE(locked_by, '?')),
    next_attempt_at = now(),
    updated_at   = now()
WHERE status = 'processing'
  AND locked_until < now();
```

Reclaim is **not** an extra attempt. The attempt was already counted when the dead worker claimed the row.

If `attempt_count >= max_attempts` after reclaim, the next loop must poison rather than claim again (see §9).

Heartbeat (optional, same worker, every N seconds while handling a long score job):

```sql
UPDATE outbox_events
SET locked_until = now() + (@lease_seconds * interval '1 second')
WHERE id = @id
  AND status = 'processing'
  AND locked_by = @worker_id;
```

Handlers that may exceed 15s (batch score, ML later) **must** heartbeat or the lease will double-process.

### 8.4 Poll loop

```text
loop until cancelled:
  reclaim expired leases
  expire rows whose expires_at <= now() and status in (pending, processing[own lease])
  claimed = claim batch
  if claimed is empty:
      wait WaitHandle(NOTIFY or PollIntervalMs)   # default 250ms
      continue
  foreach row in claimed:          # parallelism: 1 for risk/shadow; up to 4 for notify/score
      process one row (own connection / transaction)
  if claimed.count == batch_size:
      continue immediately         # back-pressure drain
```

`apps/mt5-worker` and `apps/fix-worker` each host `OutboxProcessorHostedService` with a **type allow-list** (see §12). Do not run the processor in `apps/api`.

### 8.5 LISTEN/NOTIFY (optional accelerator)

| Channel | Payload | Listener |
|---|---|---|
| `outbox_events` | `event_type` text | Each processor; ignore types not in its allow-list |

Rules:

- `pg_notify` only **after** the produce transaction commits (same session, after `SaveChanges` / `COMMIT`).
- A worker that is already busy ignores notify.
- If LISTEN setup fails, log and fall back to interval polling. Never crash the host.

### 8.6 Concurrency model

| Event type | Max in-flight per process | Cross-process |
|---|---|---|
| `risk-check-request` | 1 | SKIP LOCKED; risk engine is deterministic but book snapshots must not race two approvals for the same account |
| `shadow-copy-intent` | 2 | SKIP LOCKED; serialize per `(broker_id, login)` in the handler with `pg_advisory_xact_lock(hashtextextended(key, 0))` |
| `trade-completed` | 4 | SKIP LOCKED |
| `score-update` | 2 | advisory lock per `(broker_id, login)` so two trades for the same trader do not clobber `trader_scores` |
| `notification-event` | 8 | SKIP LOCKED; no advisory lock |

Advisory lock key: `hashtextextended(format('%s:%s', broker_id, login), 0)` taken inside the handler transaction **before** reading trader/score/intent rows.

---

## 9. Poison messages, retry, expiry

### 9.1 Error classes

| Class | Examples | Action |
|---|---|---|
| `transient` | Postgres serialization failure, timeout, Redis notify fail (after ledger write), temporary quote-cache miss that the handler treats as retryable | Stay `pending`; exponential backoff |
| `permanent` | JSON schema mismatch, unknown `event_type` (should be impossible via CHECK), missing required payload field, handler bug that throws `OutboxPermanentException` | `poisoned` immediately (do not burn remaining attempts) |
| `expired` | `expires_at <= now()`, or `signal_age > max_signal_age` at handle time | `expired`; no retry; write `system_events` / notification `copy.expired` |
| `handler` | Domain reject that is **not** an outbox failure (e.g. risk `REJECT`) | Mark outbox `processed`. The domain decision is the result. |

A risk engine `REJECT` is **success** for the outbox. Poison is for “we could not run the handler,” not “risk said no.”

### 9.2 Backoff

```text
delay = min(BackoffCapSeconds, BackoffBaseSeconds * 2^(attempt_count - 1))
next_attempt_at = now() + delay + jitter(0 .. 0.2 * delay)
```

Defaults: `BackoffBaseSeconds = 1`, `BackoffCapSeconds = 300`, `MaxAttempts = 8`.

Attempt timeline (no jitter): 0s → 1s → 2s → 4s → 8s → 16s → 32s → 64s → poison.

`notification-event` may use `MaxAttempts = 5` and `BackoffCapSeconds = 60`. Ledger-backed notify (row already in `system_events`) can drop after that; the dashboard still has REST.

### 9.3 Time-sensitive expiry (not poison)

Applies to `shadow-copy-intent` and `risk-check-request` only.

```text
if row.expires_at is not null AND row.expires_at <= now():
    status = expired
    last_error_class = expired
    last_error = 'OUTBOX_EXPIRED'
    clear lease
    insert handler receipt (handler, key) with result { "outcome": "expired" }
    enqueue notification-event kind=copy.expired (best-effort, own idempotency key)
    return
```

Additionally, the shadow/risk handler must recompute signal age from payload `source_event_time` vs `decision_time` (A23 §6.2, §63). If the intent is still `pending` in `copy_intents` but stale, mark the **intent** expired and the outbox row `processed` with result `SIGNAL_STALE` / `INTENT_EXPIRED`. Do **not** leave it to retry until `expires_at` — a 3-minute FIX outage must not fire 20 stale entries on resume (A27 `NoBlindCatchUpReplayTests`, §63).

`trade-completed` and `score-update` have **no** `expires_at` by default. They must drain even after a long outage (reconstruction/scoring is not a live order).

### 9.4 Poison path

Trigger when:

1. `last_error_class = permanent`, or
2. after a failed attempt, `attempt_count >= max_attempts`.

Steps (one TX):

```text
UPDATE outbox_events SET
    status = 'poisoned',
    locked_until = NULL,
    locked_by = NULL,
    last_error = <truncated 4k>,
    last_error_class = ...,
    processed_at = NULL
WHERE id = @id;

INSERT INTO outbox_poison_events (...snapshot...);

-- best-effort chained notify; if this insert conflicts, ignore
INSERT INTO outbox_events (event_type=notification-event,
    idempotency_key='notification-event:outbox.poisoned:outbox_event:{id}:once',
    ...);
```

Poisoned rows **leave** `ix_outbox_events_claim`. They remain queryable for `GET /api/v1/system/health` and a future ops page.

### 9.5 Manual replay

Operator action (RiskManager+ / SuperAdmin), audited in `audit_logs`:

```text
1. Verify the handler bug is fixed (or payload is still valid).
2. If expires_at is in the past for shadow/risk: do **not** replay; leave expired/poisoned.
3. UPDATE outbox_events SET
       status = 'pending',
       attempt_count = 0,
       next_attempt_at = now(),
       last_error = NULL,
       locked_until = NULL,
       locked_by = NULL
   WHERE id = @id AND status = 'poisoned';
4. UPDATE outbox_poison_events SET replayed_at = now(), replayed_by = @actor
   WHERE outbox_event_id = @id AND replayed_at IS NULL;
```

Do **not** delete the receipt unless the operator explicitly requests “force re-handle.” Default replay is blocked by `uq_outbox_handler_receipts` if the handler already committed domain writes — that is correct (replay becomes a no-op success). Force re-handle is only for “handler wrote nothing, receipt missing, row poisoned.”

### 9.6 Payload truncation / PII

`last_error` is `varchar`-equivalent `text` truncated to 4000 chars. Do not put FIX passwords, MT5 passwords, or connection strings into payload or error (A06, §57). Payloads below are allow-listed fields only.

---

## 10. Handler contracts

All payloads are JSON objects, `payload_schema_version = 1`. Extra fields are ignored; missing required fields are `permanent` poison.

Common envelope (columns + payload):

```json
{
  "schema": 1,
  "correlation_id": "uuid",
  "causation_id": "uuid|null",
  "occurred_at": "2026-08-18T12:00:00.000Z"
}
```

Identifiers in every structured log line (§57): `correlation_id`, `broker_id`, `source_login`, `source_trade_id`, `copy_intent_id`, `risk_decision_id`, `execution_intent_id` when present.

### 10.1 `trade-completed`

**Producer:** trade reconstruction, only when `ReconstructedTrade.IsCompleted` becomes true in that commit.

**Not produced by:** live deal ingest, historical backfill (see §11).

**Payload:**

```json
{
  "schema": 1,
  "reconstructed_trade_id": "uuid",
  "broker_id": "uuid",
  "login": 0,
  "position_ticket": 0,
  "canonical_symbol": "XAUUSD",
  "source_symbol": "XAUUSD.a",
  "direction": "Long",
  "opened_at": "…Z",
  "closed_at": "…Z",
  "net_realized_pnl": "0.00",
  "closed_volume": 0,
  "deal_count": 0,
  "was_scaled_in": false,
  "was_partial_close": false,
  "was_averaged_down": false,
  "completed_xau_trade_count_after": 3,
  "source_event_id": "uuid",
  "source_event_time": "…Z"
}
```

**Handler (`TradeCompletedHandler`, `apps/mt5-worker`):**

1. Load `ReconstructedTrade`. If missing → `transient` (replication lag should not exist; still retry a few times) then `permanent`.
2. If `canonical_symbol != XAUUSD` → `processed` no-op (do not score/copy).
3. Enqueue `score-update` trigger `after-trade` (same TX as receipt).
4. Load `TraderScore.CurrentState` (or `INSUFFICIENT_DATA` if none).
5. If state ∈ {`SHADOW`, `LIVE_CANDIDATE`, `LIVE`} **and** `source_event_time` is within `max_signal_age`: persist `copy_intents` (if not exists) and enqueue `shadow-copy-intent`. If already stale: do **not** enqueue shadow/risk.
6. Enqueue `notification-event` `trade.completed`.
7. Write receipt + mark outbox `processed`.

Never call `IScoringService` or `IRiskEngine` inline if those implementations can exceed a few milliseconds or call ML. Enqueue instead. A purely in-process deterministic score **may** run here later, but the first implementation should stay chained so tests can inject failures between steps.

### 10.2 `score-update`

**Producer:** `TradeCompletedHandler`, one-shot backfill watermark job, or audited manual rescore.

**Payload:**

```json
{
  "schema": 1,
  "broker_id": "uuid",
  "login": 0,
  "trigger": "after-trade",
  "trigger_id": "uuid",
  "reconstructed_trade_id": "uuid|null",
  "reason": "completed-xau-trade"
}
```

**Handler (`ScoreUpdateHandler`, `apps/mt5-worker`):**

1. Advisory lock `(broker_id, login)`.
2. `IScoringService` → only the §39 triple plus the existing Domain fields on `TraderScore` (`RiskScore`, `BehaviorScore`, `EarlyQualityScore`, `CompletedXauTrades`, `CurrentState`).
3. Upsert `trader_scores`; append `trader_score_history` (unique on trigger).
4. If `CurrentState` changed → `notification-event` `trader.state.changed`.
5. Always `notification-event` `score.changed` on a new history row.
6. **Never** enqueue `risk-check-request`. Scoring cannot approve orders (A23, §39, §72.15).
7. **Never** promote `EARLY_SCORE` → `LIVE`. Trade #3 is evidence, not skill (§23, §72.16). Default gate is SHADOW only.

### 10.3 `shadow-copy-intent`

**Producer:** `TradeCompletedHandler` (or `CopyIntentFactory` in the same reconstruction TX).

**Payload:** aligns with A23 §3.1 (subset stored on the intent row; payload is a pointer + clock fields):

```json
{
  "schema": 1,
  "copy_intent_id": "uuid",
  "source_broker_id": "uuid",
  "source_login": 0,
  "source_trade_id": "uuid",
  "source_event_id": "uuid",
  "canonical_symbol": "XAUUSD",
  "side": "Long",
  "exposure_class": "open-exposure",
  "source_volume": 0,
  "source_price": "0.00",
  "source_event_time": "…Z",
  "collector_receive_time": "…Z",
  "expires_at": "…Z",
  "max_signal_age_ms": 0,
  "trader_state": "SHADOW",
  "suggested_allocation": "0.00",
  "confidence": "0.00"
}
```

Copy `expires_at` onto the outbox row column.

**Handler (`ShadowCopyIntentHandler`, `apps/mt5-worker`):**

1. Expiry / signal-age check (§9.3). Fail closed to `expired`.
2. Ensure `copy_intents` row exists (unique source event + class). Fill `expires_at`, `max_signal_age`.
3. If `trader_state == SHADOW` (or `WATCH` must **not** copy — only SHADOW+): run shadow engine (§24) against **destination** quotes, persist `shadow_orders` / `shadow_fills` / `shadow_positions` / `shadow_performance`. Idempotent on `(copy_intent_id, shadow_order` identity).
4. If `trader_state ∈ {LIVE, LIVE_CANDIDATE}`:
   - Enqueue `risk-check-request` with the same `copy_intent_id` (same TX).
   - Still write a shadow observation if the shadow book is on (recommended: always shadow, even when live, so source-vs-dest slippage stays measurable).
5. `LIVE_CANDIDATE` + `REAL_COPY_EXECUTION_ENABLED=false` → shadow only; **still** enqueue risk-check only if a later flag says “evaluate but do not persist execution_intent.” Default: **do not** enqueue risk-check unless state is `LIVE` **and** the feature flag is true. Safer default: enqueue risk-check for `LIVE` always; the risk engine itself returns `REAL_EXECUTION_DISABLED` and writes a decision without an execution intent (A23 §4.3). Prefer that — it proves the risk path without sending.
6. Notification `shadow.filled` / `copy.intent.created` as needed.

### 10.4 `risk-check-request`

**Producer:** `ShadowCopyIntentHandler` (or CopyIntent factory for live).

**Payload:**

```json
{
  "schema": 1,
  "copy_intent_id": "uuid",
  "source_broker_id": "uuid",
  "source_login": 0,
  "source_trade_id": "uuid",
  "source_event_id": "uuid",
  "exposure_class": "open-exposure",
  "canonical_symbol": "XAUUSD",
  "trader_state": "LIVE",
  "expires_at": "…Z"
}
```

**Handler (`RiskCheckRequestHandler`, `apps/fix-worker`):**

1. Expiry / stale / FIX-down policy (A23 evaluation order 1–9). TRADE down → do **not** leave an unbounded pending backlog: mark intent `TRADE_FIX_UNAVAILABLE` and outbox `processed` or `expired` per A23 §5.5. Do not retry forever waiting for FIX — that is how stale catch-up is born.
2. `IRiskEngine.Evaluate` (A23). Persist `risk_decisions` (unique `copy_intent_id`).
3. `APPROVE` / `REDUCE_SIZE` → persist `execution_intents` with `status = not_sent`, unique `cl_ord_id`, all §33 fields. **Stop.**
4. `REJECT` → processed + `notification-event` `risk.rejected`.
5. `PAUSE_TRADER` / `PAUSE_VENUE` / `GLOBAL_STOP` → mutate trader/venue/kill-switch state in the **same** TX as the decision; audit; notify.
6. **Forbidden:** `NewOrderSingle`, QuickFIX send, any socket.

FIX worker’s **other** hosted service (`FixExecutionWorker`) later claims `execution_intents` where `status = not_sent` AND `REAL_COPY_EXECUTION_ENABLED` AND `READY_FOR_EXECUTION`. That claim is out of scope for this document except the boundary: **outbox retry must never become a second execution_intent** (unique on `copy_intent_id`).

### 10.5 `notification-event`

**Producer:** any handler, kill-switch, ingest health, poison path.

**Payload:**

```json
{
  "schema": 1,
  "kind": "trade.completed",
  "severity": "info",
  "subject_type": "reconstructed_trade",
  "subject_id": "uuid",
  "dedupe_bucket": "once",
  "title": "XAUUSD trade completed",
  "body": "login 12345 closed XAUUSD",
  "data": { "broker_id": "uuid", "login": 12345 },
  "broker_id": "uuid",
  "source_login": 12345
}
```

`severity`: `info` | `warning` | `critical`.

**Handler (`NotificationEventHandler`, `apps/mt5-worker` by default):**

1. Insert `system_events` (idempotent unique if present).
2. Publish Redis channel `ops:events` with the **same allow-listed DTO** as A06 REST (no secrets). If Redis is down: log `transient` **only if** the ledger insert has not committed; if the insert committed, mark `processed` anyway (REST polling covers the dashboard).
3. API `OpsHub` (`/hubs/ops`) subscribes to Redis and pushes to ReadOnly+ clients (A06 §4.14). Workers do not host SignalR.

---

## 11. Backfill vs live (do not mix)

| Path | Raw persist | Reconstruction | `trade-completed` | `score-update` | `shadow-copy-intent` | `risk-check-request` |
|---|---|---|---|---|---|---|
| Live deal / position event | yes, same TX as ingest audit | async after raw | yes, when a logical trade completes | chained | only if state already SHADOW/LIVE **and** signal fresh | only via shadow handler for LIVE |
| Historical backfill | yes, checkpointed | batch job | **no** | one per trader at watermark: `score-update:{broker}:{login}:backfill-watermark:{checkpoint}` | **no** | **no** |
| Replay tests | fixture | harness | yes, with fake clock | yes | yes, dest quote tape | optional; must honour expiry |

Backfill of ~5,000 accounts would otherwise enqueue years of stale shadow/risk work and violate §63.

Live ingest **may** write a future optional sixth type only via a new migration + enum value. Until then, do not sneak `deal-ingested` into `notification-event`.

---

## 12. Process placement

| Host | Claims | Also hosts |
|---|---|---|
| `apps/mt5-worker` | `trade-completed`, `score-update`, `shadow-copy-intent`, `notification-event` | MT5 connectors, backfill, live ingest, reconstruction, periodic source reconcile |
| `apps/fix-worker` | `risk-check-request` | QUOTE/TRADE sessions, destination quotes, `execution_intents` sender, cTrader reconcile |
| `apps/api` | none | REST + SignalR consume `system_events` / Redis |

Rationale: risk needs destination quote + venue health that already live on the FIX worker (A23 §3.2, §3.6). Putting risk on mt5-worker would either duplicate quote cache or couple MT5 to FIX.

Multiple instances of the same host are allowed (SKIP LOCKED). Advisory locks serialize per-trader score/shadow.

### 12.1 Suggested configuration

```json
{
  "Outbox": {
    "PollIntervalMs": 250,
    "BatchSize": 32,
    "LeaseSeconds": 30,
    "MaxAttempts": 8,
    "BackoffBaseSeconds": 1,
    "BackoffCapSeconds": 300,
    "EnableListenNotify": true,
    "ClaimTypes": [
      "trade-completed",
      "score-update",
      "shadow-copy-intent",
      "notification-event"
    ]
  }
}
```

`apps/fix-worker` overrides `ClaimTypes` to `[ "risk-check-request" ]` and may set `PollIntervalMs: 100` (time-sensitive).

---

## 13. Application / Infrastructure ports (design, not implemented)

A02 names these as **MISSING**. When implemented, keep them here — do not invent a second stack.

```csharp
// Application — produce
public interface IOutboxWriter
{
    // Must enlist in the ambient EF/Npgsql transaction. Never open its own connection.
    Task WriteAsync(OutboxWriteRequest request, CancellationToken cancellationToken);
}

public sealed record OutboxWriteRequest(
    OutboxEventType EventType,
    string IdempotencyKey,
    string AggregateType,
    Guid AggregateId,
    Guid CorrelationId,
    Guid? CausationId,
    object Payload,
    int PayloadSchemaVersion,
    Guid? BrokerId,
    ulong? SourceLogin,
    Guid? SourceTradeId,
    Guid? SourceEventId,
    Guid? CopyIntentId,
    DateTimeOffset OccurredAt,
    DateTimeOffset? AvailableAt,
    DateTimeOffset? ExpiresAt);

// Application — future broker seam. v1 implementation = IOutboxWriter.
public interface IEventBus
{
    Task PublishAsync(OutboxWriteRequest request, CancellationToken cancellationToken);
}

// Application — drain
public interface IOutboxProcessor
{
    Task<int> ProcessBatchAsync(CancellationToken cancellationToken);
}

public interface IOutboxHandler
{
    OutboxEventType EventType { get; }
    string HandlerName { get; }
    Task<OutboxHandleResult> HandleAsync(OutboxEnvelope envelope, CancellationToken cancellationToken);
}

public sealed record OutboxHandleResult(
    OutboxHandleOutcome Outcome,
    string? ErrorClass,
    string? Error);

public enum OutboxHandleOutcome
{
    Processed = 0,
    TransientFailure = 1,
    PermanentFailure = 2,
    Expired = 3
}
```

`IEventBus` v1 = `TransactionalOutboxEventBus` that calls `IOutboxWriter`. No Kafka client package. A later Kafka implementation must **still** write the outbox (or an equivalent local table) in the domain transaction and publish **from the poller**, never from the MT5 callback.

Infrastructure:

- `TransactionalOutboxWriter` — `DbSet<OutboxEvent>` insert + `ON CONFLICT DO NOTHING`.
- `PostgresOutboxProcessor` — reclaim, expire, claim, dispatch, ack.
- `OutboxProcessorHostedService` — loop in the worker hosts.
- EF configuration for the three tables in §6.
- Metric exporter for §15.

Domain entity (when Wave 3 lands; A01 path `Domain/Platform/OutboxEvent.cs`): persistence-oriented record is allowed to live in Infrastructure if Domain stays pure. Do **not** put `jsonb` or EF attributes in Domain if the rest of Domain stays persistence-ignorant. Prefer Infrastructure `OutboxEventRecord` mapping to the table; Application sees `OutboxEnvelope`.

---

## 14. Produce-side transaction rules

### 14.1 Live ingest (callback / pump thread)

```text
BEGIN
  validate + dedupe
  INSERT mt5_deals / mt5_orders / mt5_positions_current   -- ON CONFLICT DO NOTHING
  INSERT ingestion_events
COMMIT
```

No outbox on this path unless reconstruction is proven **pure SQL in the same TX** (it will not be in Phase 1). A07 requires the callback to stay light (§72.6).

### 14.2 Reconstruction commit

```text
BEGIN
  UPSERT reconstructed_trades
  INSERT outbox_events trade-completed   -- ON CONFLICT DO NOTHING
COMMIT
```

### 14.3 Handler commit (success)

```text
BEGIN
  advisory lock if required
  IF receipt exists THEN
      -- already done
  ELSE
      domain writes
      INSERT outbox_handler_receipts
      INSERT chained outbox_events (optional)
  END IF
  UPDATE outbox_events SET status='processed', processed_at=now(), lock cleared
COMMIT
```

Receipt **before** chained notify side effects that cannot roll back (Redis). Domain writes + receipt + ack are one TX.

### 14.4 Handler commit (transient failure)

```text
BEGIN
  UPDATE outbox_events SET
      status = CASE WHEN attempt_count >= max_attempts THEN 'poisoned' ELSE 'pending' END,
      next_attempt_at = ...,
      last_error = ...,
      last_error_class = 'transient',
      lock cleared
  -- if poisoned: INSERT outbox_poison_events + notification
COMMIT
```

Do not write a receipt on failure.

### 14.5 Isolation

Use `READ COMMITTED` (Postgres default). Claim uses row locks, not `SERIALIZABLE` (avoids retry storms on the queue). Domain upserts that need “insert-or-load” use unique violations as control flow, not serializable transactions.

---

## 15. Metrics, health, logs

### 15.1 Metrics (§58 plus outbox-specific)

| Metric | Type | Labels | Meaning |
|---|---|---|---|
| `mt5_outbox_backlog` | gauge | `event_type`, `status` | Row count. §58 name kept even for non-MT5 types. |
| `outbox_claim_total` | counter | `event_type`, `worker` | Claims |
| `outbox_processed_total` | counter | `event_type`, `handler` | Success |
| `outbox_retry_total` | counter | `event_type`, `error_class` | Transient |
| `outbox_poisoned_total` | counter | `event_type` | Poison |
| `outbox_expired_total` | counter | `event_type` | Expiry |
| `outbox_lease_reclaimed_total` | counter | `event_type` | Dead worker |
| `outbox_handler_duration_seconds` | histogram | `event_type`, `handler` | Handle time |
| `outbox_oldest_pending_age_seconds` | gauge | `event_type` | Freshness SLO |
| `outbox_duplicate_enqueue_total` | counter | `event_type` | `ON CONFLICT DO NOTHING` hits |

`score_requests_total` / `score_failures_total` (§58) increment inside `ScoreUpdateHandler`, not the poller.

### 15.2 Health (A06 `GET /api/v1/system/health`)

Include:

```text
outbox.backlog_by_type.{pending,processing,poisoned,expired}
outbox.oldest_pending_age_seconds
outbox.poison_open_count
outbox.workers[].{name, last_claim_at, claim_types}
```

Stale-source flags (§62) stay separate (`mt5` collector heartbeat). Outbox health is **not** green if `poison_open_count > 0` for `risk-check-request` or `shadow-copy-intent`.

Alert hints (ops, not code):

| Condition | Severity |
|---|---|
| `risk-check-request` oldest pending > 5s while rows exist | critical |
| `shadow-copy-intent` oldest pending > 15s | warning |
| `trade-completed` oldest pending > 30s | warning |
| any `poison_open_count > 0` | warning (critical if risk/shadow) |
| lease reclaim rate > 1/min sustained | warning (handler too slow or crash loop) |

### 15.3 Logging

Structured fields only (§57). Never log `payload` wholesale if it might later grow to include destination account secrets. Log `outbox_event_id`, `event_type`, `idempotency_key`, `attempt_count`, `status`, `handler`, `outcome`.

---

## 16. Failure modes

| Failure | Result | Recovery |
|---|---|---|
| Crash after raw deal commit, before reconstruction | Deal durable; no outbox | Reconstruction scanner / next reconcile picks unfinished deals |
| Crash after reconstruction + outbox commit, before handle | At-least-once | Poller claims |
| Crash after handler domain write, before ack | Redelivery | Receipt or domain unique index → no-op, then ack |
| Crash after `execution_intents` insert, before ack of risk outbox | Redelivery of risk-check | Unique `copy_intent_id` on `risk_decisions` / `execution_intents` |
| Crash after FIX send | **Out of band** | `execution_intents` → `sent but acknowledgement unknown` / `EXECUTION_STATE_UNKNOWN`; reconcile; **never** outbox retry (§33–34) |
| Worker deadlock / GC pause > lease | Another worker claims | Handler must be idempotent |
| Handler bug (null ref) | transient → poison | Fix + audited replay if not expired |
| Payload schema bump without migrator | `permanent` poison | Deploy handler first, then producers |
| Postgres down | Producers fail closed (do not emit in-memory events) | §62 |
| Redis down | Notifications still in `system_events` | REST poll; outbox notify still `processed` |
| FIX TRADE down | Risk handler marks unavailable / expires; does not queue infinite retries | §63, A23 |
| Duplicate MT5 deal | Raw unique index; no second reconstruction; no second outbox | Phase 1 idempotency |
| Two workers score same login | Advisory lock | Last writer is the same deterministic result |

---

## 17. Tests (bind to A27)

Implement these when product code exists. This document does not add tests.

### 17.1 Unit (`TraderIntelligence.Tests.Unit`)

| Class | Must prove |
|---|---|
| `Outbox.IdempotencyKeyTests` | Keys in §7.1 are stable; reversals produce two shadow keys; notification buckets as specified |
| `Outbox.BackoffTests` | Delay formula + cap + poison after `MaxAttempts` |
| `Outbox.ExpiryPolicyTests` | Shadow/risk past `expires_at` → `Expired`, no handler domain writes |
| `Outbox.ErrorClassTests` | Risk `REJECT` → `Processed`; missing payload field → `PermanentFailure`; timeout → `TransientFailure` |
| `Outbox.EventBusIsOutboxTests` | `IEventBus` v1 does not reference a broker package |

### 17.2 Integration (`TraderIntelligence.Tests.Integration`) — A27

| Class | Must prove |
|---|---|
| `Outbox.OutboxProcessingTests` | Persist raw/reconstructed + outbox in **one** commit (second connection cannot see the outbox row before commit). Kill processor after commit and before handle → row still `pending` → handle runs. Second handle is a no-op (receipt / unique). |
| `Outbox.OutboxDoesNotCallFixFromCallbackTests` | Live ingest path has **zero** `NewOrderSingle` / FIX session invocations. Only `IOutboxWriter`. |
| `Outbox.SkipLockedConcurrencyTests` | Two processors, one row → one claim. |
| `Outbox.PoisonAndReplayTests` | Handler throws `MaxAttempts` times → `poisoned` + poison snapshot; replay without force does not duplicate domain rows. |
| `Outbox.LeaseReclaimTests` | Stuck `processing` with `locked_until` in the past is reclaimed and handled once. |
| `Outbox.BackfillDoesNotEnqueueShadowOrRiskTests` | Backfill fixture of old completed trades → score watermark only; zero `shadow-copy-intent` / `risk-check-request`. |
| `Outbox.NoBlindCatchUpTests` | Enqueue shadow/risk, advance clock past `expires_at`, start processor → `expired`, no `execution_intents`. |
| `Outbox.RiskApproveDoesNotSendFixTests` | Approved risk persists `execution_intents.not_sent` only. |

Use **real PostgreSQL** (Testcontainers or lab). EF InMemory cannot prove `SKIP LOCKED`, unique conflicts, or `jsonb` (A03).

### 17.3 Replay

`Mt5EventReplayer` drives ingest → reconstruction → outbox (A27 §6.1). Fake clock must be the source of `occurred_at` / `source_event_time` / `expires_at`.

---

## 18. Implementation sequence (when a coding agent is assigned)

Do not start this until Phase 0 artifacts exist (A28). Suggested incremental slices:

1. Migration: three tables + indexes + CHECKs + `updated_at` trigger.
2. Infrastructure writer + EF mapping + `ON CONFLICT DO NOTHING`.
3. Processor: reclaim, expire, claim, ack; hosted service; metrics.
4. `TradeCompletedHandler` + `NotificationEventHandler` (unblocks dashboard later).
5. `ScoreUpdateHandler` behind `IScoringService`.
6. `ShadowCopyIntentHandler` + `copy_intents` unique index.
7. `RiskCheckRequestHandler` on fix-worker + `execution_intents` persist-only.
8. Integration tests in §17.2 (gate for Phase 1–2 outbox item in A28 / §60).

No Kafka client. No Redis streams. No extra microservice.

---

## 19. Pinned decisions (do not reopen without measurement)

1. **PostgreSQL is the bus.** `IEventBus` is a one-method facade over the outbox.
2. **Five event types only** until a versioned enum + CHECK change.
3. **Polling + SKIP LOCKED** is the delivery mechanism. NOTIFY is optional.
4. **Poison is a status + snapshot table**, not a side table that the poller reads.
5. **Idempotency is `(event_type, idempotency_key)`** plus domain unique indexes plus handler receipts.
6. **FIX send is not an outbox event.**
7. **Redis is not an outbox.**
8. **Backfill does not enqueue shadow or risk.**
9. **Expired time-sensitive rows are not retried** and are not a catch-up queue.
10. **Risk `REJECT` is outbox success.**
11. **mt5-worker** drains reconstruction/score/shadow/notify; **fix-worker** drains risk.
12. **Same-transaction produce** is mandatory; in-memory channels after commit are forbidden for these five kinds.

---

## 20. Mapping to architecture rules

| Rule | How this design satisfies it |
|---|---|
| §12 lightweight callback | Callback persists raw only |
| §13 no Kafka | Confirmed |
| §32 never FIX from MT5 callback | Callback cannot see FIX; risk handler cannot send |
| §33 persist-before-send | `execution_intents` written by risk handler; send is a different poller |
| §34 no blind resend | Outbox retry cannot insert a second intent; FIX unknown state is separate |
| §36 latency clocks | Payload carries `source_event_time`, `collector_receive_time`; handler stamps `decision_time` |
| §39 / A23 risk final authority | Score handler cannot enqueue execution; only risk writes `execution_intents` |
| §41 flag default false | Risk returns `REAL_EXECUTION_DISABLED`; no send |
| §57 correlation ids | Columns + log fields |
| §58 `mt5_outbox_backlog` | Gauge on `status,event_type` |
| §60 outbox processing tests | §17.2 |
| §63 no stale catch-up | `expires_at` + expire-before-handle + backfill isolation |
| §71 / §72.20 simplicity | One table, one poller, no mesh |

---

## 21. Explicit gaps this design does **not** fill

These remain **MISSING** in product code and are out of this file’s write scope:

- EF DbContext, migrations, repositories
- `ILiveIngestionService`, reconstruction service, `IScoringService`, `IRiskEngine`, `CopyIntent` entity
- Worker host composition (`OutboxProcessorHostedService` is specified, not written)
- SignalR `OpsHub` and Redis relay
- Ops UI for poison replay (API route not in A06 yet; add later as `POST /api/v1/system/outbox/{id}/replay` RiskManager+)

Until those exist, this document is the contract those types must implement.

---

*End of A41. Product source was not modified.*
