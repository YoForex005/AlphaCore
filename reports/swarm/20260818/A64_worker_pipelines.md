# A64 — mt5-worker and fix-worker hosted service pipelines

**Status:** binding implementation spec (pre-code)  
**Date:** 2026-08-18  
**Scope:** hosted-service composition, transactional outbox consumers, reconnect/retry backoff, fail-closed gates  
**Product source modified:** none. Write this file only. Do not implement from this spec until a later coding task.  
**Primary architecture:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§4–13, 25–34, 40–45, 56–63, 66–72  
**Adjacent swarm law (do not fork):**

| Doc | What this spec consumes |
|---|---|
| `A07_mt5_worker_audit.md` | Seven required MT5 jobs; template loop is not a collector |
| `A08_fix_worker_audit.md` | FIX worker is scaffold; live send is fail-closed by absence today |
| `A15_mt5_pool_watchdog.md` | Native pool acquire + watchdog 5→60 s backoff |
| `A16_mt5_http_client.md` | Remote SSE reconnect 1 s → 30 s |
| `A20_table_catalog.md` | `outbox_events`, `sync_checkpoints`, lease/recon tables |
| `A23_risk_engine_spec.md` | Risk is the last gate before send |
| `A24_shadow_copy_spec.md` | Shadow consumes outbox, never an MT5 callback |
| `A25_fix_session_spec.md` | Two sessions, lease + fencing, unknown-state, flags |
| `A27_test_inventory.md` | Outbox / backfill-restart / no-FIX-from-callback tests |
| `A28_phases_gates.md` | Phase order; `NewOrderSingle` stays off until Phase 8 |

This document exists so the two worker hosts are not “one `while` + `Task.Delay(1000)` that does everything.”  
It specifies **process authority**, **hosted-service pipelines**, **outbox claim/dispatch**, **backoff that is never a send-retry**, and **fail-closed** as the default money-risk posture.

---

## 0. Verdict

`apps/mt5-worker` and `apps/fix-worker` are **two authority-separated .NET 8 Worker hosts**. They share PostgreSQL and a transactional outbox. They do **not** share sockets, Manager sessions, FIX sequence files, or a god loop.

```text
MT5 Manager (Achiever, StarwaveFX, …)
        │
        ▼
apps/mt5-worker          ← source authority
  connect / groups / accounts / backfill / live / reconcile
  persist raw + outbox (same commit)
  consume source-family outbox (reconstruct / score / notify)
        │
        │  outbox_events (PostgreSQL)
        ▼
apps/fix-worker          ← destination authority
  QUOTE + TRADE sessions, lease, Security List, quotes
  consume dest-family outbox (shadow / risk / execution)
  persist-before-send; never retry a possibly-sent order
        │
        ▼
Pepperstone / cServer FIX 4.4
```

**Current measured state is not this.** Both hosts are the stock `dotnet new worker` delay loop. There is no connector, no outbox processor, no lease, no flags in config, no health port. `REAL_COPY_EXECUTION_ENABLED=false` is architecture policy, not a runtime gate. Live copy is **safe by absence**, not by an implemented control.

**Do not** put reconstruction, scoring, shadow, risk, and `NewOrderSingle` on the Manager pump thread.  
**Do not** introduce Kafka, a third “copy-worker” process, or a custom FIX codec on day one.  
**Do not** treat a green `dotnet build` or `Worker running at:` as Phase 1/4 progress.

---

## 1. Non-goals

- Implementing product source from this file.
- Kafka / NATS / Rabbit / mesh / multi-region TRADE.
- Active-active TRADE sessions.
- Blind catch-up of stale OPEN intents after FIX reconnect (§63).
- Retrying `NewOrderSingle` because TCP broke (§33–§34).
- Filtering MT5 groups to `MT5_GROUP_*` plan maps (§9).
- Calling `CreateUser` / `DealerBalance` / `SendTrade` from `mt5-worker`.
- Sending FIX from an MT5 callback (§32).
- Hardcoding a Pepperstone instrument id into tag 55.
- Using Redis as authority for orders, positions, balances, or outbox cursors.
- One shared sequence counter for QUOTE and TRADE.

---

## 2. Measured current state (honest)

Snapshot 2026-08-18. Product source was read, not edited.

| Location | Measured |
|---|---|
| `D:\Prop\apps\mt5-worker\Worker.cs` | `BackgroundService` logs timestamp every 1000 ms |
| `D:\Prop\apps\mt5-worker\Program.cs` | `AddHostedService<Worker>()` only |
| `D:\Prop\apps\mt5-worker\appsettings*.json` | logging levels only |
| `D:\Prop\apps\fix-worker\*` | byte-identical template (namespace `Fix` vs `Mt5`) |
| `src/Mt5/Configuration/Mt5BrokerOptions.cs` | options sketch; unused by the worker |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | flags + header sketch; unused by the worker |
| `src/Fix.CTrader/Parsing/FixMessageParser.cs` | unit-test parser; not a session |
| `src/Infrastructure/Persistence/TraderDbContext.cs` | **does not compile as written** — `DbSet<OutboxEvents>` etc. have no matching entity types; `BrokersConfiguration` binds `Brokers` while Domain has `Broker` |
| `Domain.Enums.OutboxEventType` | `TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent` — no ingest types yet |
| C++ `MT5Watchdog` | 5→10→20→40→60 s reconnect; **not wired** into the C# worker |
| C++ `MT5HttpClient` SSE | 1000 ms → 30000 ms; **not wired** into the C# worker |
| Health / metrics / Serilog / OTel | absent on both workers |
| `REAL_COPY_EXECUTION_ENABLED` in product config | **absent** |

Classification:

| Component | Class |
|---|---|
| Template `Worker` in both hosts | `DEPRECATED` (delete once real services register) |
| Host `Program.cs` | `EXISTS_NEEDS_REFACTOR` |
| `Mt5BrokerOptions` / `CTraderFixOptions` | `EXISTS_NEEDS_REFACTOR` (shape useful; not bound; TRADE `TargetCompId` default `CSERVER` is a §26 trap) |
| Outbox processor | `MISSING` |
| Hosted pipelines below | `MISSING` |

---

## 3. Process topology and authority

### 3.1 Two processes, one database

| Process | OS | Authority | Must not |
|---|---|---|---|
| `TraderIntelligence.Mt5Worker` | Windows if `MT5_MODE=local`; Linux OK if **every** broker is `remote` and a Windows sidecar exists | Source brokers, raw ledger, checkpoints, source-family outbox | Open FIX; send `35=D`; provision users; invent deals |
| `TraderIntelligence.FixWorker` | Linux or Windows (no native Manager DLL) | Destination venue, quotes, TRADE lease, dest-family outbox | Persist raw MT5 tickets as source of truth; connect to Manager; consume source deals to send |

`apps/api` is **read + command** (kill switch, manual ack). It is not an outbox consumer for execution. Dashboard never receives secrets.

### 3.2 Replica counts

| Process | Production replicas | Why |
|---|---|---|
| `mt5-worker` | 1 per deployment **or** 1 per broker **if** each replica’s claim filter + checkpoint scope is disjoint | Two replicas claiming the same `(broker_id, login, stream)` will double-fetch. Prefer **one replica, N connectors**. |
| `fix-worker` | **Exactly one** TRADE owner per destination account | Official RoE: multiple connections duplicate ExecutionReports (A25 §4) |

If `mt5-worker` is ever scaled, partition by `broker_id` (Achiever replica vs StarwaveFX replica), never by login hash across a shared Manager pool.

### 3.3 In-process vs cross-process

Same-process producer + consumer is **allowed** and preferred at this scale (~5,000 accounts, PostgreSQL outbox, no Kafka). The invariant is a **transaction boundary**, not a process boundary:

```text
LEGAL:   ingest commit (raw + outbox row)  →  later hosted loop claims row
ILLEGAL: Manager callback → reconstruct → score → NewOrderSingle
ILLEGAL: GetDeals → send FIX before commit
ILLEGAL: mark outbox processed in the same stack as an uncommitted raw write
```

### 3.4 Library vs host

```text
apps/mt5-worker          host, DI, flags, health port
apps/fix-worker          host, DI, flags, health port, lease loop

src/Application          pipelines, handler interfaces, backoff policy types
src/Infrastructure       EF, outbox claim SQL, checkpoint store, leases
src/Mt5                  IMt5BrokerConnector + transports
src/Fix.CTrader          sessions, headers, dictionary, parsers
src/TradeReconstruction  Phase 2+ (not this host’s Phase 1 job, but an outbox handler)
src/Scoring              Phase 3+ handler
src/Shadow               Phase 5 handler (runs in fix-worker)
src/Risk                 Phase 8 handler (runs in fix-worker)
src/Execution            persist-before-send + unknown-state (runs in fix-worker)
```

Hosts stay thin. Domain rules do not live in `Worker.cs`.

---

## 4. Shared primitives

### 4.1 Clock

| Clock | Used for |
|---|---|
| MT5 `GetServerTime` | history windows, checkpoint `cursor_to`, deal `DealTime` compare |
| Host UTC (`ISystemClock`) | `created_at`, `available_at`, quote_age, signal age, lease TTL, backoff |
| FIX `SendingTime` (52) | wire; never a substitute for `quote_received_at` |

Do not mix host-local civil time into `GetDeals(from,to)`. Incomplete page + host-clock window = silent gaps.

### 4.2 Correlation

Every persist, outbox row, log, and metric tag carries whatever of these is known (§57):

```text
correlation_id
broker_id / venue_id
source_login
source_trade_id
source_event_id
copy_intent_id
risk_decision_id
execution_intent_id
cl_ord_id
fix_session
fencing_token
outbox_id
attempt_count
```

Never log MT5 password, proxy password, FIX tag 554, or API keys. Redact in the sink, not ad hoc.

### 4.3 Idempotency keys

| Write | Natural key |
|---|---|
| `mt5_deals` | `(broker_id, deal_ticket)` |
| `mt5_orders` | `(broker_id, order_ticket)` |
| `mt5_positions_current` | `(broker_id, position_id)` |
| `mt5_accounts` | `(broker_id, login)` |
| `mt5_groups` | `(broker_id, group_name)` |
| `outbox_events` | `(aggregate_type, aggregate_id, event_type, dedupe_key)` |
| `sync_checkpoints` | `(scope_type, scope_id, stream_name)` plus login when scoped |
| `copy_intents` | `(source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| `execution_intents` / `fix_orders` | `cl_ord_id` |
| `fix_execution_reports` | `(venue_id, exec_id)` |

`ON CONFLICT … DO NOTHING` (or hash-compare) on raw tickets. Corrections are new revisions / `ingestion_events`, not silent history rewrite (§11).

### 4.4 Bounded in-memory queues

Callbacks and FIX engines **only enqueue**. Persistence is on a worker task.

| Queue | Bound | Overflow (fail-closed) |
|---|---|---|
| MT5 pump → persist | 10_000 events | increment `mt5_queue_dropped_total`; set source **stale**; **do not** invent; **do not** block the pump for DB I/O |
| FIX inbound app messages | 5_000 | increment `fix_queue_dropped_total`; TRADE drops `READY_FOR_EXECUTION`; do not send |
| Outbox dispatch in-flight | `Outbox:MaxInFlight` (default 8) | stop claiming until a slot frees |

Memory is not a ledger. On process death the durable truth is PostgreSQL (raw + unprocessed outbox + checkpoints).

---

## 5. Outbox — table, writer, claim, poison

### 5.1 Why an outbox (architecture §12–§13)

```text
validate → dedup → persist raw → write outbox row → SINGLE COMMIT
        ↘ background consumer(s) later
```

This is the only legal coupling from source ingest to reconstruction, scoring, shadow, risk, and FIX.

Day-one bus = **this table**. Event-bus abstraction may wrap the processor later. Do not add Kafka first.

### 5.2 Table contract (`outbox_events`)

Align with A20 §5.8. Columns the processors require (canonical names):

```text
id                  uuid PK
aggregate_type      text        -- mt5_deal | mt5_order | mt5_position | mt5_account
                                -- reconstructed_trade | trader_score
                                -- copy_intent | risk_decision | execution_intent
                                -- destination_quote | notification
aggregate_id        uuid
event_type          text        -- see §6; persist the string, not only the C# enum
schema_version      int         -- start at 1
dedupe_key          text        -- producer-assigned
payload             jsonb
broker_id           uuid null
venue_id            uuid null
source_login        bigint null
correlation_id      uuid
created_at          timestamptz
available_at        timestamptz  -- now at insert; pushed forward on retry
processed_at        timestamptz null
attempt_count       int         -- 0 at insert
last_error          text null
locked_until        timestamptz null
locked_by           text null   -- instance_id
```

Constraints / indexes:

```text
UNIQUE (aggregate_type, aggregate_id, event_type, dedupe_key)
INDEX  (processed_at, available_at, event_type) WHERE processed_at IS NULL
INDEX  (created_at) WHERE processed_at IS NULL          -- backlog age
```

`TraderDbContext` already *names* `OutboxEvents` but the entity, configuration, and migration **do not exist**. When implementation is authorized, add a real type (prefer `OutboxEvent`, not the plural stub name) and a versioned migration. Do not hand-write SQL against production.

### 5.3 Event-type catalog

Extend `OutboxEventType` when coding. The current enum is **downstream-only** and is not enough for Phase 1 ingest.

| Wire `event_type` | Enum (proposed) | Producer | Consumer process | First phase |
|---|---|---|---|---|
| `mt5.deal_persisted` | `Mt5DealPersisted` | mt5 ingest | mt5 reconstruction | 1 / used in 2 |
| `mt5.order_persisted` | `Mt5OrderPersisted` | mt5 ingest | mt5 reconstruction | 1 / used in 2 |
| `mt5.position_upserted` | `Mt5PositionUpserted` | mt5 ingest | mt5 reconstruction | 1 / used in 2 |
| `mt5.account_upserted` | `Mt5AccountUpserted` | mt5 accounts | notify / snapshots | 1 |
| `mt5.group_upserted` | `Mt5GroupUpserted` | mt5 groups | notify | 1 |
| `mt5.history_window_synced` | `Mt5HistoryWindowSynced` | mt5 backfill | reconstruction catch-up | 1 / 2 |
| `mt5.source_stale` / `mt5.source_healthy` | `SourceHealthChanged` | mt5 connect | fix-worker risk + dashboard | 1 |
| `trade.completed` | `TradeCompleted` | reconstruction | scoring + shadow candidate | 2 |
| `score.updated` | `ScoreUpdate` | scoring | notify / state | 3 |
| `copy.shadow_intent` | `ShadowCopyIntent` | copy policy | **fix-worker** shadow | 5 |
| `risk.check_requested` | `RiskCheckRequest` | copy / live promote | **fix-worker** risk | 8 |
| `execution.approved` | `ExecutionApproved` | risk | **fix-worker** send path | 8 |
| `notification.event` | `NotificationEvent` | any | API/SignalR publisher | 3 |

Phase 1 required emissions: the `mt5.*` rows. Downstream handlers may be **registered as no-ops that mark processed only after a structured log**, or left unregistered (row stays, backlog metric rises, **no drop**).

Unregistered type in a process = **do not claim**. The other process (or a later phase) owns it.

### 5.4 Writer API (same commit as the aggregate)

```text
IOutboxWriter.Enqueue(OutboxEnvelope env, CancellationToken ct)
```

Rules:

1. Called on the **same** `DbContext` / `NpgsqlConnection` / ambient transaction as the raw upsert.
2. Insert `ON CONFLICT (aggregate_type, aggregate_id, event_type, dedupe_key) DO NOTHING`.
3. `available_at = now()`, `attempt_count = 0`, `processed_at = null`.
4. If the raw upsert is a no-op duplicate, still use the same dedupe key so a second outbox row is not created.
5. **Never** write the outbox after `Commit` “to keep the callback fast.” Crash between raw and outbox is an unrecoverable silent skip of downstream. Prefer one commit.
6. Checkpoint writes for a history window go in the **same** transaction as the deals of that window **or** not at all (see §9.4). Outbox `history_window_synced` is emitted only when the checkpoint actually advances.

Suggested `dedupe_key` examples:

```text
deal:{broker_id}:{deal_ticket}:persisted
order:{broker_id}:{order_ticket}:rev:{payload_hash}
position:{broker_id}:{position_id}:rev:{volume}:{price}:{time_update}
trade:{reconstructed_trade_id}:completed
intent:{copy_intent_id}:shadow
exec:{execution_intent_id}:approved
```

### 5.5 Claim protocol (`FOR UPDATE SKIP LOCKED`)

One claim loop per process. Filter = **that process’s registered `event_type` set**.

```sql
WITH batch AS (
  SELECT id
  FROM outbox_events
  WHERE processed_at IS NULL
    AND available_at <= now()
    AND (locked_until IS NULL OR locked_until < now())
    AND event_type = ANY(@claimed_types)
  ORDER BY created_at
  LIMIT @batch_size
  FOR UPDATE SKIP LOCKED
)
UPDATE outbox_events o
SET attempt_count = o.attempt_count + 1,
    locked_until  = now() + (@lock_seconds * interval '1 second'),
    locked_by     = @instance_id
FROM batch
WHERE o.id = batch.id
RETURNING o.*;
```

Defaults:

| Knob | Default | Notes |
|---|---|---|
| `Outbox:BatchSize` | 50 | keep small; handlers may do DB work |
| `Outbox:PollIdleMs` | 250 | when batch empty |
| `Outbox:LockSeconds` | 60 | must exceed p99 handler time |
| `Outbox:MaxInFlight` | 8 | parallelism after claim |
| `Outbox:MaxAttempts` | 20 | then poison (still unprocessed) |
| `Outbox:HandlerTimeoutMs` | 30_000 | cancel; treat as retryable |

After claim, dispatch **outside** the claim transaction (connection returned to the pool). Handler uses its own transaction(s). This avoids holding row locks across FIX I/O.

### 5.6 Handler contract

```text
IOutboxHandler
  IReadOnlySet<string> EventTypes { get; }
  Task<OutboxHandleResult> HandleAsync(OutboxRecord row, CancellationToken ct)
```

`OutboxHandleResult`:

| Result | Row mutation | Next |
|---|---|---|
| `Processed` | `processed_at = now()`, clear lock/error | stop |
| `Retry` | `available_at = now() + backoff(attempt)`, clear lock, set `last_error` | claim later |
| `Poison` | leave `processed_at` null, `available_at = timestamptz 'infinity'` or +100 years, set `last_error`, emit `outbox_poison_total` | human replay |
| `Defer` | same as Retry but with handler-supplied `available_at` (e.g. wait for QUOTE) | not a failure |

Rules:

1. Handlers **must be idempotent**. Re-claim after crash is normal.
2. A handler that cannot prove success returns `Retry` or `Poison`, never `Processed`.
3. **Unknown / unparseable payload** → `Poison`, not a tight retry loop.
4. **Dependency down** (DB already in the handler, MT5, FIX, quote stale) → `Retry` or `Defer`. Do not mark processed.
5. A handler **must not** send `NewOrderSingle`. Only `src/Execution` send function may, and only after §11.5 conjunction. The execution handler calls that function.
6. A handler **must not** catch-and-ignore. Exceptions become `Retry` with the message (truncated, redacted).
7. Do not start a handler for an event type the process did not claim.

### 5.7 Success / crash cases

| Scenario | Outcome |
|---|---|
| Commit raw+outbox, process dies before claim | row unprocessed; next loop claims | at-least-once |
| Claim, die before handler | `locked_until` expires; another loop reclaims | at-least-once |
| Handler succeeds, die before `processed_at` | reclaim; handler must no-op on already-applied work | at-least-once |
| Handler throws | Retry + backoff | |
| Duplicate insert of same dedupe key | one row | exactly-once **produce** |
| Two processes claim different type sets | no overlap | |
| Two processes claim the same type | **forbidden** in production. Defense: `Outbox:ConsumerName` + advisory lock per `(consumer_name)` |

### 5.8 Consumer names and exclusive claim

| `consumer_name` | Process | Types |
|---|---|---|
| `mt5-source` | mt5-worker | all `mt5.*`, `trade.completed`, `score.updated`, `notification.event` |
| `fix-dest` | fix-worker | `mt5.source_stale`, `mt5.source_healthy`, `copy.shadow_intent`, `risk.check_requested`, `execution.approved` |

`notification.event` may be consumed by **api** later; until then mt5-worker logs it. Do not let both api and mt5-worker claim it.

Acquire at startup:

```text
pg_advisory_lock( hashtext('outbox:' || consumer_name) )
```

or a row in `system_events` / a small `outbox_consumer_leases` table with the same fencing rules as FIX leases. If the lock is not held, the processor **does not claim**. A second replica sits idle and exposes `outbox_consumer_standby=1`.

### 5.9 Poison and replay

- Poison rows stay forever until an operator (SuperAdmin) sets `available_at = now()`, `attempt_count = 0`, `last_error = null`.
- Replay is **the same handler**. No “just send the order again” admin button.
- Alert when `outbox_poison_total` increases or when oldest unprocessed age > `Outbox:LagAlertSeconds` (default 120).

### 5.10 What the outbox is not

- Not a FIX resend buffer.
- Not an unlimited queue of stale OPEN copy intents (§63 — expiry lives on `copy_intents`, and the handler `Defer`s/`Processed` as expired without sending).
- Not a substitute for `sync_checkpoints`.
- Not Redis.

---

## 6. Unified backoff policy

Backoff is for **availability**. It is **never** permission to resend a possibly-accepted order.

### 6.1 Formula

```text
delay = min(cap, initial * 2^min(attempt, 16)) + Uniform(0, jitter)
honor stoppingToken during the entire delay
reset attempt to 0 on a distinct success (connect, page, logon, handler Processed)
```

Decorrelated jitter is allowed (`delay = min(cap, random(initial, previous*3))`) if unit-tested. Do not use “retry immediately in a hot loop.”

### 6.2 Policy table

| Policy id | Initial | Cap | Jitter | Reset on | Used by |
|---|---|---|---|---|---|
| `mt5.local.reconnect` | 5 s | 60 s | ±20% | Connect + pump-or-request proven | `Mt5ConnectHostedService` local watchdog (match C++ `MT5Watchdog`) |
| `mt5.remote.sse` | 1 s | 30 s | ±20% | SSE connected + health GET ok | remote transport (match `MT5HttpClient`) |
| `mt5.pool.acquire` | n/a | caller budget | n/a | n/a | do **not** exponential-spin Borrow; skip the tick (A15) |
| `mt5.history.page` | 2 s | 60 s | ±20% | full page set persisted | backfill / live deal poll |
| `mt5.discovery` | 5 s | 5 min | ±20% | group/account sweep success | groups / accounts timers |
| `mt5.reconcile` | 10 s | 10 min | ±20% | reconcile run completed | periodic reconcile |
| `outbox.retry` | 1 s | 60 s | full jitter 0–delay | `Processed` | all handlers |
| `fix.reconnect` | 2 s | 60 s | ±20% | Logon OK **this** session | QUOTE and TRADE **independently** |
| `fix.logon.reject` | 15 s | 5 min | ±20% | next Logon OK | invalid Logon / Logout Text(58) — slower than transport drop |
| `fix.lease.renew` | n/a | n/a | n/a | every ≤ ⅓ TTL | not exponential |
| `db.connect` | 1 s | 30 s | ±20% | Npgsql open + `SELECT 1` | both hosts |
| `health.poll` | 1 s | 5 s | none | n/a | gauges only |

### 6.3 Hard bans

```text
FORBIDDEN:
  catch (IOException) { SendNewOrderSingle(sameClOrdId); }     // §34
  catch (IOException) { SendNewOrderSingle(newClOrdId); }      // §34
  catch (GetDealsFailed) { checkpoint.cursor_to = window.To; } // gap
  catch (DbException) { send anyway from memory; }             // §62
  while (!logon) { SendLogon(); Thread.Sleep(50); }            // stampede
  mutate TargetCompID cServer → CSERVER on retry               // §26
```

### 6.4 Interaction with fail-closed

Backoff **continues forever** for connect / ingest / quote / lease renew (the process stays up).  
Backoff **does not** enqueue unbounded `execution.approved` rows while TRADE is down. The producer (risk) must refuse or expire; the consumer `Defer`s until `READY_FOR_EXECUTION` **and** `expires_at` is still in the future. Expired → `Processed` with reason `SIGNAL_EXPIRED`, no send.

---

## 7. Fail-closed matrix

Fail-closed means: **prefer a missed copy / delayed score over a fabricated fill, a double order, or a silent ledger gap.**

### 7.1 Source (mt5-worker)

| Condition | Persist raw? | Advance checkpoint? | Emit outbox? | Copy / shadow OPEN? | Surface |
|---|---|---|---|---|---|
| Broker connected, page complete | yes | yes | yes | downstream decides | `mt5_connected=1` |
| `GetDeals` false / truncated | no new window | **NO** | no `history_window_synced` | n/a | `mt5_history_incomplete_total++` |
| Duplicate ticket | no second row | yes if window complete | no second event | n/a | `mt5_duplicate_deals_total++` |
| Pump fallback (no-pump) | poll only | only after poll success | yes when persist | treat source as **degraded** | `mt5_pump_mode=0`, stale-risk |
| Manager down | no | no | `mt5.source_stale` once | **NO** new OPEN | `mt5_connected=0`, `source_stale_since` |
| In-memory queue overflow | no (dropped) | no | `mt5.source_stale` | **NO** | `mt5_queue_dropped_total` |
| PostgreSQL down | no | no | no | **NO** | worker health fail; retry `db.connect` |
| Password / secrets missing at start | do not connect | n/a | n/a | n/a | **fail that broker**; do not boot a silent empty session |
| Plan map missing for a group | still upsert group | n/a | `mt5.group_upserted` | n/a | mapping is overlay only |
| Reconstruction handler down | raw still commits | yes | row waits | no shadow yet | `mt5_outbox_backlog` |
| Host shutting down | flush bounded queue **with timeout**, then drop remainder + stale | no partial checkpoint | only committed rows | n/a | clean Disconnect |

**Do not invent source trades** (§62).

### 7.2 Destination (fix-worker)

| Condition | QUOTE | TRADE read | Shadow OPEN | Live `35=D` | Notes |
|---|---|---|---|---|---|
| `CTRADER_FIX_ENABLED=false` | off | off | no | no | health-only process |
| `CTRADER_FIX_QUOTE_ENABLED=false` | off | independent | no | no if price required | |
| `CTRADER_FIX_TRADE_SESSION_ENABLED=false` | independent | off | yes (QUOTE only) | no | |
| `REAL_COPY_EXECUTION_ENABLED=false` | allowed | allowed | allowed | **hard no** | default; compile path still refuses |
| QUOTE down or `quote_age > max` | — | — | **no OPEN/INCREASE** | **no** | REDUCE/CLOSE: A23/A24 separate policy |
| TRADE down | — | — | yes | **no** | do not backlog unlimited OPEN |
| Not `READY_FOR_EXECUTION` | — | recon running | yes | **no** | |
| Lease not owned / fenced | — | do not Logon TRADE | yes | **no** | loser does not reconnect TRADE |
| `EXECUTION_STATE_UNKNOWN` any in-flight | — | recover | yes | **no new OPEN**; no resend | |
| Unresolved recon issue | — | — | measure-only | **no** | |
| PostgreSQL down | keep socket? **do not persist-from-memory as authority** | stop READY | no | **no** | §62 |
| `STOP_NEW_EXECUTION` | — | — | no OPEN | **no** | |
| `EMERGENCY_FLATTEN` | — | — | no OPEN | flatten path only, still persist-before-send | |
| Risk engine unhealthy / missing snapshot | — | — | reject | **no** | |
| Intent expired / `max_signal_age` exceeded | — | — | no OPEN | **no** | close policy separate |
| Header Logon not proven | no app messages | no app messages | no | no | A25 §3.6 |
| Production `UseSsl=false` | **refuse start** | **refuse start** | no | no | unless `AllowPlaintext` **and** non-Production |

### 7.3 Cross-cutting

| Condition | Rule |
|---|---|
| Handler exception | Retry; never `Processed` |
| Payload schema unknown | Poison |
| Two TRADE sockets | Must be impossible (lease). Treat a detected second owner as P0: Logout, drop READY, alert |
| Dashboard / API cannot see worker | Do not infer “healthy.” API marks component `unknown` (fail-closed for live promote) |
| Clock jump | Recompute quote_age / lease; do not send during skew > 5 s |

### 7.4 Fail-closed function (execution)

Single function, both flag and runtime (A25 §6.3). Re-check **immediately before socket write**:

```text
MaySendNewOrderSingle():
  CTRADER_FIX_ENABLED
  AND CTRADER_FIX_TRADE_SESSION_ENABLED
  AND REAL_COPY_EXECUTION_ENABLED          -- config floor; runtime cannot turn on if config is false
  AND TRADE == READY_FOR_EXECUTION
  AND lease owned AND fencing token current
  AND risk engine healthy
  AND STOP_NEW_EXECUTION == false
  AND global kill / venue pause == false
  AND (if price required: QUOTE usable AND quote_age <= max)
  AND instrument mapped (Security List, not hardcoded)
  AND execution_intent persisted AND cl_ord_id persisted
  AND status == not_sent
  AND intent not expired
  AND database reachable (write probe in the persist-before-send tx)
```

If any check fails: **do not send**. Persist a `risk_decisions` / execution decision. Return `Retry`/`Defer`/`Processed(rejected)` as appropriate.

`REAL_COPY_EXECUTION_ENABLED=true` with a missing quote is still a reject.

---

## 8. mt5-worker pipelines

Replace `AddHostedService<Worker>()`. Register the services below. Exact class names may vary; **jobs may not be collapsed into one `ExecuteAsync`.**

Shared DI:

```text
IOptions<Mt5WorkerOptions>          -- brokers[], health port, outbox, backoff
IMt5BrokerConnectorFactory          -- one implementation, N instances
IMt5BrokerRegistry                  -- Achiever, StarwaveFX, future
TraderDbContext / ICheckpointStore / IOutboxWriter
IHostedService × N
Serilog + OTel
Health checks on Kestrel (see §12)
```

Startup order (gates, not a single thread):

```text
host start
  → bind options; refuse to start a broker with empty password
  → DB ping (retry db.connect; process stays up)
  → Connect (both brokers, independent)
  → Group discovery (after that broker IsConnected)
  → Account sync (after groups once)
  → Backfill (after accounts; resume checkpoints)
  → Live events + deal-lag poll (may start degraded)
  → Periodic reconcile
  → Outbox processor (mt5-source types) — may start as soon as DB is up
```

Outbox processor does **not** wait for both brokers. Downstream must tolerate “no events yet.”

### 8.1 `Mt5ConnectHostedService`

**Purpose:** own Manager / remote sessions for every enabled broker for the process lifetime.

**Must:**

- Bind `Mt5:Brokers` (see §13). Secrets from env / user-secrets / secret store only.
- Construct **one** `IMt5BrokerConnector` type per `broker_id`.
- `ConnectAsync` with pump where possible. Persist `broker_connections` row: pump vs no-pump, last error (no secrets).
- Watchdog:
  - local: ping / `IsConnected` on interval 30 s; reconnect with `mt5.local.reconnect` (5→60 s).
  - remote: SSE + `/mt5/health`; reconnect with `mt5.remote.sse` (1→30 s).
- Expose `mt5_connected{broker_id}`, `mt5_reconnects{broker_id}`, `mt5_pump_mode{broker_id}`, `source_stale_since{broker_id}`.
- On first disconnect and on restore: enqueue `mt5.source_stale` / `mt5.source_healthy` **once per transition** (dedupe_key `health:{broker_id}:{state}:{epoch_minute}` is too coarse; use `health:{broker_id}:{transition_id}`).
- On shutdown: cancel tokens, `DisconnectAsync`, do not leave Manager slots hung.
- If MT5 is down: retry forever; **do not invent deals**; do not mark backfill complete.

**Must not:**

- Hard-code a single `MT5_SERVER` (C++ `AppConfig` smell).
- Block the host without honoring `stoppingToken`.
- Treat `demo\Maxmaster` as the only group.
- Share one connector instance across two `broker_id`s.

**Fail-closed:** missing secrets → that broker stays `DISABLED` and health is failed; other brokers still start.

### 8.2 `Mt5GroupDiscoveryHostedService`

**Purpose:** dynamic group discovery (§7, §9).

**Must:**

- Run after connect; then every `Mt5:GroupResyncSeconds` (default 300) and on reconnect.
- `GetGroupsAsync` / GroupTotal + details. Upsert `mt5_groups` on `(broker_id, group_name)`.
- Join `plan_group_mappings` **after** upsert. Absence of a plan does **not** drop a group.
- Write `ingestion_events` for the run. Emit `mt5.group_upserted` per new/changed group (hash dedupe).
- Bound concurrency to that broker’s pool.

**Must not:** filter to `MT5_GROUP_*`. Must not call C++ `getMt5Group` as the universe.

**Fail-closed:** incomplete list → do not delete missing groups on a failed fetch. Tombstone only after N consecutive **successful** full lists omit the name (config `Mt5:GroupTombstoneSuccesses`, default 3).

### 8.3 `Mt5AccountSyncHostedService`

**Purpose:** every login the Manager can see, bound to broker + group.

**Must:**

- For each live group: `GetGroupLogins` then `GetUser` + `GetAccount`.
- Upsert `mt5_accounts` on `(broker_id, login)`.
- Append `mt5_account_snapshots` on a slower cadence (`Mt5:SnapshotSeconds`, default 60).
- Apply live `UserAdd` / `UserUpdate` / `UserDelete` via the same writer as the sweep.
- Page / batch; concurrency ≤ pool size (Achiever 8, StarwaveFX 4).
- Emit `mt5.account_upserted` on identity/group/rights change.

**Must not:** use login as a global PK. Must not skip unmapped groups. Must not `CreateUser` / `DealerBalance`.

**Fail-closed:** a single login `GetAccount` failure skips that login, does not abort the broker; increment `mt5_account_fetch_failures`. Full sweep failure does not wipe the table.

### 8.4 `Mt5HistoryBackfillHostedService`

**Purpose:** catch-up `GetDeals` / `GetOrders` / open `GetPositions` until each account is current.

**Pipeline (architecture §12):**

```text
Read checkpoint (broker_id, login, stream)
    ↓
Build server-time window [cursor_to - overlap, server_now]
    ↓
Fetch ALL pages  (false or short-without-eof → abort)
    ↓
Normalize (broker_id + tickets; source symbol as-is)
    ↓
Upsert idempotently + outbox rows
    ↓
Persist checkpoint cursor_to = window.to   -- SAME TRANSACTION
```

**Must:**

- Streams: `deals_backfill`, `orders_history`, `positions_snapshot`.
- Overlap default 5 minutes (config). Rely on ticket upserts, not exact windows.
- First run: from `max(account.registration, Mt5:HistoryFloorUtc)` to now.
- Exhaust pages. C++ contract: follow every cursor or return false. **False = do not advance.**
- Emit `mt5.deal_persisted` / `order` / `position` / `history_window_synced`.
- Metric `mt5_backfill_lag{broker_id}` = now − max persisted deal time.
- Fairness: round-robin logins; do not starve live poll (share the pool; live poll has higher priority on Borrow).

**Must not:** reconstruct logical trades here (Phase 2 handler). Must not fabricate ticks. Must not blind-copy.

**Fail-closed:** see §7.1 truncated fetch.

### 8.5 `Mt5LiveEventsHostedService`

**Purpose:** keep raw tables current without recrawling all history every second.

**Must:**

- Drain `SubscribeAsync` / event queue on a **dedicated** task. Callbacks enqueue only (§72.6).
- Persist `Position*` → `mt5_positions_current`; `Order*` → `mt5_orders`; `User*` → account writer; `Deal*` **if they fire**.
- **There is no `PUMP_MODE_DEALS`.** Also run a **short-lag deal poll** (`Mt5:LiveDealPollSeconds`, default 2) for recently active logins, plus a slower sweep (`Mt5:LiveDealSweepSeconds`, default 30) of all logins: `GetDeals(checkpoint − overlap, server_now)`.
- Validate → dedup → persist raw → outbox → commit together.
- If pump is off: set degraded, keep poll, publish stale/degraded. Do not pretend live.
- Count `mt5_events_total`, `mt5_deals_total`, `mt5_duplicate_deals_total`.

**Must not:** assume `OnDealAdd` is the live feed. Must not `CacheExecutedDeal` / `SendTrade`. Must not call scoring or FIX. Must not block the pump with EF.

**Fail-closed:** DB down → bound the queue, then drop + stale (do not OOM, do not invent).

### 8.6 `Mt5ReconciliationHostedService`

**Purpose:** Phase 1 acceptance “reconciliation working.”

**Must:**

- Timer `Mt5:ReconcileSeconds` (default 300) and **forced** window after reconnect.
- Re-pull deals/orders/positions for `[now − ReconcileLookback, now]` (default 24 h, config).
- Missing raw rows take the **same** upsert + outbox path as live.
- Mismatched payload: do not silently overwrite; write `ingestion_events` + revision / hash column.
- After reconnect: reconcile **before** claiming “live healthy.”

**Fail-closed:** reconcile fetch failure → keep previous raw rows; increment `mt5_reconcile_failures`; do not advance a reconcile checkpoint.

### 8.7 Checkpoint store (not necessarily its own timer)

Table `sync_checkpoints` (A20):

```text
scope_type, scope_id, stream_name, login null,
cursor_from, cursor_to, last_entity_ticket, payload_hash,
updated_at, status (running|ok|failed), error
```

**Law:** write `cursor_to` only after the corresponding raw rows (and their outbox rows) **committed**. Never persist a checkpoint in the same breath as a failed or truncated `GetDeals`.

On restart: backfill + live poll resume from checkpoint, then reconcile.

### 8.8 `OutboxProcessorHostedService` (`consumer_name=mt5-source`)

See §5. Phase 1 handlers:

| Type | Handler |
|---|---|
| `mt5.deal_persisted` | Phase 1: no-op `Processed` after optional reconstruction **enqueue** (Phase 2 will do real work). Prefer leaving a real `ReconstructionHandler` that is feature-flagged `Reconstruction:Enabled=false` → `Processed` skip. |
| `mt5.order_persisted` / `position_upserted` | same |
| `mt5.history_window_synced` | reconstruction catch-up when enabled |
| `mt5.account_upserted` / `group_upserted` | notify / metric |
| `mt5.source_stale` | **also claimed by fix-dest** — **do not dual-claim**. Only **fix-worker** consumes health for risk. mt5-worker **produces** only. |
| `trade.completed` | scoring when `Scoring:Enabled` |
| `score.updated` | `NotificationEvent` + trader_states |
| `notification.event` | structured log; later SignalR via API |

Correction vs §5.8: `mt5.source_stale` / `healthy` are **produced** by mt5-worker and **consumed** by fix-worker only. Remove them from `mt5-source` claim set.

### 8.9 What mt5-worker never hosts

- QuickFIX sessions
- `MaySendNewOrderSingle`
- Shadow fill simulation (needs destination quote cache — fix-worker)
- Risk engine decisions that approve live size

---

## 9. fix-worker pipelines

Replace the template loop. Host stays a host: flags, lease, session loops, dest outbox, recon.

`CTraderFixOptions` is a starting sketch. Before first Logon:

- Do **not** keep the stub default `TargetCompId = "CSERVER"` as a silent mutate of the issued form `cServer`. Make both configurable; prove Logon (A25 §3).
- `RealCopyExecutionEnabled` default **false** in every committed config.
- Add `Enabled` (master), `AllowPlaintext`, `DiagnosticLogonOnly`, `ResetSeqNum`.

Startup order:

```text
host start
  → bind flags; Production + UseSsl=false + !AllowPlaintext → FATAL (do not connect)
  → DB ping
  → acquire QUOTE lease (and TRADE lease if TRADE flag on)
  → QUOTE connect / Logon (if flags)
  → TRADE connect / Logon (if flags) — still NOT ready
  → Security List + persist destination_symbols
  → MD subscribe + quote cache
  → TRADE: block new exec → MassStatus + Positions → compare DB → READY or BLOCKED
  → dest outbox processor
  → periodic recon
  → unknown-state recovery loop
  → kill-switch observer
```

Never: Logon → drain `execution.approved` → `NewOrderSingle`.

### 9.1 `FixOptionsValidationHostedService`

Fail the host (or keep sessions `DISABLED` with failed health) when:

- Production plaintext without explicit allow
- TRADE enabled on more than one configured destination account without a lease key per account
- `RealCopyExecutionEnabled=true` in a committed `appsettings.json` checked into the repo (CI test). Runtime true is an **ops** event.

### 9.2 `FixSessionLeaseHostedService`

Implements A25 §4. Recommended: PostgreSQL `fix_session_leases` + fencing token (Redis optional later; Infrastructure already references StackExchange.Redis but it is unused).

```text
session_key examples:
  pepperstone-1369850-QUOTE
  pepperstone-1369850-TRADE
```

Rules:

- Acquire `UPDATE … WHERE leased_until < now() OR owner = me` returning `fencing_token++`.
- Renew ≤ ⅓ of TTL (default TTL 15 s, renew 5 s).
- Losing the lease: Logout if socket owned, mark in-flight `EXECUTION_STATE_UNKNOWN`, **do not** reconnect from the loser.
- Every outbound TRADE application message persist includes the fencing token. Token ≠ current → drop send.
- Standby replica: health `fix_lease_held=0`, no sockets.

QUOTE uses the same mechanism (duplicate quotes are cheaper than duplicate fills, still corrupt freshness).

### 9.3 `CTraderQuoteSessionHostedService`

Independent session object (A25 §2). TLS 5211 default.

States: `DISABLED → CONNECTING → LOGGING_ON → LOGGED_ON → SUBSCRIBING → QUOTING → RECONNECT_BACKOFF → …`

**Must:**

- Own socket, seq, heartbeat, metrics, store files.
- Heartbeat 30 s default. **Quote age is liveness**, not Heartbeat (feed may omit 35=0 while streaming).
- Reconnect with `fix.reconnect`. Invalid Logon → `fix.logon.reject` (slower).
- Do not share seq with TRADE.
- On Logon OK: allow Security List + MD. Do not send `35=D`.

**Fail-closed:** disconnect → mark quotes unusable immediately (do not use last bid after session death without an explicit grace that is still measured as `quote_age`).

### 9.4 `CTraderTradeSessionHostedService`

Independent session. TLS 5212. Application send **disabled** until §7.2 conjunction.

States include `RECONCILING → READY_FOR_EXECUTION | BLOCKED_INCONSISTENT`.

**Must:**

- Logon only while lease owned.
- After Logon: **block new executions**, run startup recon (§9.6), only then READY.
- On disconnect: drop READY, backoff, **do not flush a send queue**.
- Inbound ER / PositionReport / rejects → persist on the inbound task, not the engine thread.

**Must not:** send `35=D` because an outbox row exists. The execution handler asks `MaySendNewOrderSingle()`.

### 9.5 `FixSecurityListHostedService` + quote cache

After QUOTE (preferred) or TRADE Logon:

```text
SecurityListRequest (35=x, 559=0)
  → persist destination_symbols (instrument id, name, digits)
  → find XAUUSD by name (tag 1007); tag 55 is the Long id
  → MarketDataRequest snapshot + incremental
  → upsert destination_quotes + memory cache
```

Do not hardcode instrument id. Reject quotes with `bid<=0`, `ask<=0`, `ask<bid`.

Shadow and risk **read this cache**; they do not subscribe themselves.

### 9.6 `FixStartupReconciliationHostedService`

On every TRADE Logon / leadership win (architecture §42):

```text
block new executions
  → OrderMassStatusRequest (35=AF, type=7)
  → RequestForPositions (35=AN)
  → consume ER + PositionReport
  → compare fix_orders / destination_positions
  → persist execution_reconciliation_runs + issues
  → READY_FOR_EXECUTION only if no execution-impacting open issue
```

Empty book may yield BusinessMessageReject — treat as empty **only** if RoE/text matches; otherwise BLOCKED.

### 9.7 `FixPeriodicReconciliationHostedService`

Timer `Fix:ReconcileSeconds` (default 60). Same compare as startup. Execution-impacting issue → drop READY, alert (§43, A23).

### 9.8 `UnknownExecutionRecoveryHostedService`

Owns the only legal exit from `sent_ack_unknown` (A25 §5.4):

```text
OrderStatusRequest by ClOrdID
  → else MassStatus
  → consume ER
  → RequestForPositions
  → adopt venue state OR mark not_on_venue
  → never allocate a new cl_ord_id unless not_on_venue AND positions unchanged
```

Runs on timer and on TRADE Logon. Does **not** send `35=D`.

### 9.9 `KillSwitchObserverHostedService`

Poll `system_events` / flags (API writes the row). Modes (`KillSwitchMode`):

| Mode | Effect on this worker |
|---|---|
| `None` | flags still apply |
| `StopNewExecution` | `MaySendNewOrderSingle` false; shadow OPEN false |
| `EmergencyFlatten` | OPEN false; flatten intents may send reducing orders **only if** TRADE ready, lease owned, flatten authorized — still persist-before-send |

Runtime cannot set `REAL_COPY_EXECUTION_ENABLED=true` if config is false.

### 9.10 `OutboxProcessorHostedService` (`consumer_name=fix-dest`)

| Type | Handler | Phase |
|---|---|---|
| `mt5.source_stale` / `healthy` | update risk snapshot `source_broker_healthy`; reject OPEN while stale | 1+ |
| `copy.shadow_intent` | A24 shadow engine (QUOTE only) | 5 |
| `risk.check_requested` | A23 risk; persist `risk_decisions`; maybe enqueue `execution.approved` | 8 |
| `execution.approved` | persist-before-send → `MaySendNewOrderSingle` → send or refuse | 8 |

Phase 4: processor may run with **only** health types registered (or idle).  
Phase 5: add shadow.  
Phase 7: still no `execution.approved` handler registered **or** handler always refuses send.  
Phase 8: register send path; flag still default false.

Shadow handler fail-closed (A24): no usable quote → `Defer` or `Processed(rejected)`, **never** invent a fill.

Execution handler fail-closed:

```text
if !MaySendNewOrderSingle(): persist reject/defer; do not write socket
if persist of outbound row fails: do not send
if socket write indeterminate: status = sent_ack_unknown; do not retry send
```

### 9.11 What fix-worker never hosts

- `IMt5BrokerConnector`
- Raw `mt5_deals` as the write path (it may **read** reconstructed trades / intents)
- Scoring / XGBoost
- A second TRADE session “for watching prod”

---

## 10. End-to-end pipelines (happy path + crash)

### 10.1 Phase 1 ingest (mt5-worker only)

```text
Connect
  → Groups
  → Accounts
  → Backfill deals/orders/positions + outbox + checkpoint
  → Live queue + deal-lag poll + outbox
  → Reconcile
  → mt5-source outbox: log / optional reconstruct flag-off
```

Crash mid-window: restart resumes checkpoint; overlap + ticket upsert ⇒ no dup PK, no silent skip if checkpoint law held.

### 10.2 Phase 2–3 (still no FIX send)

```text
mt5.deal_persisted → ReconstructionHandler
  → upsert reconstructed_trades
  → outbox trade.completed
      → ScoreHandler → trader_scores + score.updated
          → NotificationHandler
```

### 10.3 Phase 5 shadow (fix-worker)

```text
trade.completed (or copy policy in mt5/application)
  → persist copy_intents (expires_at, action_class)
  → outbox copy.shadow_intent
      → fix-worker ShadowHandler
          → destination quote
          → persist shadow_orders / fills / positions
          → NEVER 35=D
```

### 10.4 Phase 8 live (still flag-off until gates)

```text
copy_intent (LIVE trader)
  → outbox risk.check_requested
      → RiskHandler → risk_decisions
          → if Approve AND flags: outbox execution.approved
              → ExecutionHandler
                  → persist execution_intents + cl_ord_id (not_sent)
                  → MaySendNewOrderSingle
                  → socket 35=D
                  → ER persist → dest position → recon
```

Disconnect after write: `sent_ack_unknown` → §9.8 only.

### 10.5 Stale source / stale quote

```text
MT5 down → mt5.source_stale → fix-worker sets source unhealthy
  → Risk / Shadow OPEN fail-closed
  → ingestion keeps retrying

QUOTE down → quote unusable
  → Shadow/Live OPEN fail-closed
  → TRADE may stay up; READY may remain for REDUCE/CLOSE per A23
```

---

## 11. Host composition

### 11.1 mt5-worker `Program.cs` (target shape)

```text
CreateApplicationBuilder
  ConfigureSerilog / OTel
  AddOptions<Mt5WorkerOptions>().Bind("Mt5").ValidateOnStart()
  AddDbContextPool<TraderDbContext>
  AddSingleton broker registry + connector factory
  AddSingleton ICheckpointStore, IOutboxWriter
  AddHostedService<Mt5ConnectHostedService>
  AddHostedService<Mt5GroupDiscoveryHostedService>
  AddHostedService<Mt5AccountSyncHostedService>
  AddHostedService<Mt5HistoryBackfillHostedService>
  AddHostedService<Mt5LiveEventsHostedService>
  AddHostedService<Mt5ReconciliationHostedService>
  AddHostedService<OutboxProcessorHostedService>()  // named mt5-source
  AddHealthChecks (DB, each broker, outbox lag, backlog)
  Listen Kestrel health-only (default http://127.0.0.1:5081)
```

Delete template `Worker`.

### 11.2 fix-worker `Program.cs` (target shape)

```text
CreateApplicationBuilder
  ConfigureSerilog / OTel
  AddOptions<CTraderFixOptions>().Bind("CTraderFix").ValidateOnStart()
  AddDbContextPool<TraderDbContext>
  AddQuickFixn + cTrader dictionary
  AddSingleton quote/trade session types (not one shared engine state)
  AddHostedService<FixOptionsValidationHostedService>
  AddHostedService<FixSessionLeaseHostedService>
  AddHostedService<CTraderQuoteSessionHostedService>
  AddHostedService<CTraderTradeSessionHostedService>
  AddHostedService<FixSecurityListHostedService>
  AddHostedService<FixStartupReconciliationHostedService>
  AddHostedService<FixPeriodicReconciliationHostedService>
  AddHostedService<UnknownExecutionRecoveryHostedService>
  AddHostedService<KillSwitchObserverHostedService>
  AddHostedService<OutboxProcessorHostedService>()  // named fix-dest
  AddHealthChecks (DB, quote, trade, lease, READY, unknown-states, outbox)
  Listen Kestrel health-only (default http://127.0.0.1:5082)
```

Delete template `Worker`.

### 11.3 Shutdown

Both hosts:

1. Stop claiming new outbox batches.
2. Drain in-flight handlers up to `ShutdownDrainSeconds` (default 15).
3. mt5: stop live enqueue; best-effort persist; Disconnect connectors.
4. fix: if TRADE owned — **Logout**, then release lease. If Logout fails, still release; next owner reconciles. Never send during shutdown.
5. Flush logs.

`CancellationToken` is honored in every backoff sleep.

### 11.4 Health ports vs FIX/MT5 ports

Health is **localhost HTTP**. It is not the Manager port and not 5211/5212. API scrapes it for `GET /api/v1/health` (A26). Unauthenticated `/health/live` = process up. Authenticated/internal `/health/ready` = dependencies.

Ready **does not** require `READY_FOR_EXECUTION` on fix-worker (that would make orchestrators kill a reconciling TRADE session). Separate gauges:

```text
fix_trade_ready_for_execution  0|1
```

---

## 12. Metrics and logs

### 12.1 MT5 (architecture §58 + pipeline)

```text
mt5_connected{broker_id}
mt5_reconnects{broker_id}
mt5_pump_mode{broker_id}
mt5_events_total{broker_id,kind}
mt5_deals_total{broker_id}
mt5_duplicate_deals_total{broker_id}
mt5_backfill_lag{broker_id}
mt5_outbox_backlog                 -- source types
mt5_queue_dropped_total
mt5_history_incomplete_total
mt5_account_fetch_failures
mt5_reconcile_failures
mt5_pool_available{broker_id}
source_stale_since{broker_id}      -- unix ts, 0 if healthy
```

### 12.2 FIX

```text
fix_quote_connected
fix_trade_connected
fix_trade_ready_for_execution
fix_logon_failures{session}
fix_reconnects{session}
fix_inbound_messages_total{session,msg_type}
fix_outbound_messages_total{session,msg_type}
fix_rejects_total
fix_business_rejects_total
fix_execution_reports_total
fix_unknown_execution_states
fix_lease_held{session}
fix_lease_lost_total
fix_fenced_sends_total
fix_newordersingle_refused_total{reason}
fix_queue_dropped_total
```

`fix_newordersingle_refused_total{reason="flag_off"}` must increment when a handler even *asks* to send while the flag is false. A zero send count alone is not proof of the gate (A08: absence ≠ control).

### 12.3 Outbox

```text
outbox_backlog{consumer}
outbox_oldest_age_seconds{consumer}
outbox_claimed_total{consumer,event_type}
outbox_processed_total{consumer,event_type}
outbox_retry_total{consumer,event_type}
outbox_poison_total{consumer,event_type}
outbox_handler_duration_ms{consumer,event_type}
```

### 12.4 Logs

Structured. Include §4.2 ids. Redact secrets centrally.  
Do **not** log `"Worker running at: {time}"` every second.

---

## 13. Configuration (committed defaults)

### 13.1 mt5-worker

```json
{
  "Health": { "Url": "http://127.0.0.1:5081" },
  "Outbox": {
    "ConsumerName": "mt5-source",
    "BatchSize": 50,
    "PollIdleMs": 250,
    "LockSeconds": 60,
    "MaxInFlight": 8,
    "MaxAttempts": 20,
    "HandlerTimeoutMs": 30000,
    "LagAlertSeconds": 120,
    "ClaimedTypes": [
      "mt5.deal_persisted",
      "mt5.order_persisted",
      "mt5.position_upserted",
      "mt5.account_upserted",
      "mt5.group_upserted",
      "mt5.history_window_synced",
      "trade.completed",
      "score.updated",
      "notification.event"
    ]
  },
  "Reconstruction": { "Enabled": false },
  "Scoring": { "Enabled": false },
  "Mt5": {
    "GroupResyncSeconds": 300,
    "SnapshotSeconds": 60,
    "LiveDealPollSeconds": 2,
    "LiveDealSweepSeconds": 30,
    "ReconcileSeconds": 300,
    "ReconcileLookbackHours": 24,
    "HistoryOverlapSeconds": 300,
    "GroupTombstoneSuccesses": 3,
    "QueueCapacity": 10000,
    "Brokers": []
  }
}
```

Brokers (secrets **not** in git):

```env
MT5__Brokers__0__BrokerId=<guid>
MT5__Brokers__0__DisplayName=Achiever
MT5__Brokers__0__Server=57.128.141.65
MT5__Brokers__0__Port=443
MT5__Brokers__0__Login=2027
MT5__Brokers__0__Password=<SECRET>
MT5__Brokers__0__ServerName=AchieverGlobalMarkets-Server
MT5__Brokers__0__Mode=local
MT5__Brokers__0__PoolSize=8

MT5__Brokers__1__BrokerId=<guid>
MT5__Brokers__1__DisplayName=StarwaveFX
MT5__Brokers__1__Server=84.201.6.142
MT5__Brokers__1__Port=443
MT5__Brokers__1__Login=9904
MT5__Brokers__1__Password=<SECRET>
MT5__Brokers__1__Mode=local
MT5__Brokers__1__PoolSize=4
```

`Mt5BrokerOptions.RemoteUrl` is required only when `Mode=remote`. Do not provision `CreateUser` fields on this worker.

### 13.2 fix-worker

```json
{
  "Health": { "Url": "http://127.0.0.1:5082" },
  "Outbox": {
    "ConsumerName": "fix-dest",
    "ClaimedTypes": [
      "mt5.source_stale",
      "mt5.source_healthy",
      "copy.shadow_intent",
      "risk.check_requested",
      "execution.approved"
    ]
  },
  "CTraderFix": {
    "Enabled": true,
    "Host": "live-us-eqx-01.p.c-trader.com",
    "AccountId": "",
    "Password": "",
    "UseSsl": true,
    "AllowPlaintext": false,
    "DiagnosticLogonOnly": false,
    "HeartbeatIntervalSec": 30,
    "ResetSeqNum": true,
    "MaxQuoteAgeMs": 5000,
    "QuoteEnabled": true,
    "TradeSessionEnabled": true,
    "RealCopyExecutionEnabled": false,
    "LeaseTtlSeconds": 15,
    "ReconcileSeconds": 60,
    "Quote": {
      "SslPort": 5211,
      "PlainPort": 5201,
      "SenderCompId": "live.pepperstone.1369850",
      "TargetCompId": "cServer",
      "TargetSubId": "QUOTE",
      "SenderSubId": "QUOTE"
    },
    "Trade": {
      "SslPort": 5212,
      "PlainPort": 5202,
      "SenderCompId": "live.pepperstone.1369850",
      "TargetCompId": "cServer",
      "TargetSubId": "TRADE",
      "SenderSubId": ""
    }
  }
}
```

Committed default `TargetCompId` is the **issued-form spelling** `cServer`. If diagnostic Logon fails, ops sets an **explicit** override (e.g. `CSERVER`) that is logged. The stub `CTraderFixOptions` currently defaults `CSERVER` — **change that when implementing**, do not treat the stub as RoE.

Password from user-secrets / env `CTraderFix__Password` only.

---

## 14. Suggested types (implementation task, not this file)

```text
src/Application/Outbox/
  IOutboxWriter.cs
  IOutboxHandler.cs
  OutboxEnvelope.cs
  OutboxHandleResult.cs
  OutboxProcessor.cs
  OutboxBackoff.cs

src/Application/Mt5/
  IMt5BrokerConnector.cs          -- architecture §6
  ICheckpointStore.cs
  Mt5IngestPipeline.cs            -- validate/dedup/persist/outbox

src/Infrastructure/Outbox/
  EfOutboxWriter.cs
  PostgresOutboxClaimer.cs        -- SKIP LOCKED
  OutboxProcessorHostedService.cs

src/Infrastructure/Persistence/
  Entities/OutboxEvent.cs         -- fix the current DbContext fiction
  Configurations/OutboxEventConfiguration.cs
  Entities/SyncCheckpoint.cs
  Migrations/…

apps/mt5-worker/Hosting/
  Mt5ConnectHostedService.cs
  Mt5GroupDiscoveryHostedService.cs
  Mt5AccountSyncHostedService.cs
  Mt5HistoryBackfillHostedService.cs
  Mt5LiveEventsHostedService.cs
  Mt5ReconciliationHostedService.cs

apps/fix-worker/Hosting/
  FixOptionsValidationHostedService.cs
  FixSessionLeaseHostedService.cs
  CTraderQuoteSessionHostedService.cs
  CTraderTradeSessionHostedService.cs
  FixSecurityListHostedService.cs
  FixStartupReconciliationHostedService.cs
  FixPeriodicReconciliationHostedService.cs
  UnknownExecutionRecoveryHostedService.cs
  KillSwitchObserverHostedService.cs

src/Execution/MaySendNewOrderSingle.cs
```

Do not create a third worker project for outbox.

---

## 15. Tests (acceptance for this spec)

Align with A27. These are **required** before claiming the pipelines exist.

### 15.1 Unit

| Test | Must prove |
|---|---|
| `OutboxWriter_SameContext_EnqueuesInPendingTransaction` | no extra connection |
| `OutboxDedupe_SameKey_DoesNotInsertSecondRow` | UNIQUE |
| `OutboxBackoff_Attempts_CapAndJitter` | policy `outbox.retry` |
| `Checkpoint_NotAdvanced_WhenGetDealsFalse` | fail-closed |
| `Checkpoint_NotAdvanced_WhenPageTruncated` | fail-closed |
| `LiveCallback_DoesNotCallFix` | enqueue only |
| `MaySendNewOrderSingle_FalseWhenFlagOff` | even if TRADE logged on |
| `MaySendNewOrderSingle_FalseWhenDbDown` | |
| `MaySendNewOrderSingle_FalseWhenQuoteStale` | OPEN path |
| `MaySendNewOrderSingle_FalseWhenNotReady` | |
| `MaySendNewOrderSingle_FalseWhenFenced` | |
| `Header_DoesNotMutate_cServer` | |
| `QuoteAndTrade_BackoffState_Independent` | |
| `KillSwitch_StopNew_BlocksSend` | |

### 15.2 Integration

| Test | Must prove |
|---|---|
| `OutboxProcessingTests` (A27) | same commit; crash before process → retry; idempotent handler |
| `OutboxDoesNotCallFixFromCallbackTests` | |
| `Mt5BackfillRestartTests` | kill mid-window; no missing tickets; no dup PK |
| `DualBrokerIsolationTests` | login/ticket clash impossible |
| `OutboxClaim_SkipLocked_TwoConsumersDifferentTypes` | no cross-claim |
| `OutboxPoison_DoesNotBlockIngest` | raw still commits |
| `FixLease_SecondInstance_CannotOwnTrade` | |
| `UnknownExecution_NoResendOnDisconnect` | |
| `StartupRecon_BlocksReady` | |
| `RealExecutionDisabledIntegrationTests` | refuse `35=D` |

Do not use Pepperstone `1369850` as the first FIX integration account.

---

## 16. Phase gating (what to register when)

| Phase | mt5-worker | fix-worker |
|---|---|---|
| 1 | connect, groups, accounts, backfill, live, reconcile, outbox produce + source consume (log/no-op reconstruct) | **optional** dest outbox for `source_stale` only; **no** sockets required for Phase 1 exit |
| 2 | enable `Reconstruction:Enabled` | still no send |
| 3 | enable `Scoring:Enabled` | — |
| 4 | — | QUOTE session, Security List, quotes, dashboard health; `RealCopy=false` |
| 5 | produce `copy.shadow_intent` | shadow handler |
| 6 | ML is **not** a worker pipeline | — |
| 7 | — | TRADE read + recon; still no `35=D` |
| 8 | produce `risk.check_requested` | risk + execution handlers; flag still default false |

Phase 1 exit does **not** require fix-worker QUOTE. Phase 4 does not require TRADE send. Do not start Phase 8 until A28 / §68 / §70 gates are evidenced.

---

## 17. Risks if this spec is ignored

| ID | Risk | Why it is P0/P1 |
|---|---|---|
| R1 | God `Worker.ExecuteAsync` | reconnects block backfill or live drops (A07) |
| R2 | Live deals only via `OnDealAdd` | **zero** live deals on this SDK |
| R3 | Checkpoint on partial `GetDeals` | permanent silent gaps |
| R4 | Outbox after commit / or none | crash loses downstream or couples callback to FIX |
| R5 | Both workers claim all types | double reconstruct / double shadow / double send |
| R6 | Backoff used as send-retry | double fill |
| R7 | Two TRADE replicas | duplicate ER + duplicate orders |
| R8 | Plan-map filter | miss `demo\Maxmaster` and every non-yo group |
| R9 | Identity without `broker_id` | Achiever/Starwave ticket collision |
| R10 | Flag not encoded | first `35=D` is live on 1369850 |
| R11 | Stub `TargetCompId=CSERVER` | silent case change vs issued `cServer` |
| R12 | Linux `MT5_MODE=local` | cannot load Manager DLL |
| R13 | Secrets in appsettings | A19 / §55 |
| R14 | Treat template heartbeat as health | dashboard lies |
| R15 | `TraderDbContext` fiction shipped | compile theater; no real outbox |

---

## 18. Implementation sequence (when authorized)

1. Real `OutboxEvent` + `SyncCheckpoint` entities, EF configs, versioned migration. Delete/replace the non-compiling `DbSet` stubs.
2. `IOutboxWriter` + SKIP LOCKED claimer + hosted processor. Unit tests first.
3. `IMt5BrokerConnector` + connect/watchdog hosted service for two brokers. No groups yet = still not Phase 1.
4. Groups → accounts → backfill+checkpoints → live+deal poll → reconcile.
5. Bind flags on fix-worker; refuse `35=D`; diagnostic Logon-only mode.
6. Lease + two sessions + Security List + quotes (Phase 4).
7. Dest outbox: source-health + shadow (Phase 5).
8. TRADE recon (Phase 7). Unknown-state recovery **before** any send path.
9. `MaySendNewOrderSingle` + persist-before-send (Phase 8 code, flag false).

---

## 19. Acceptance (this spec)

Pipelines are **DONE** only when all of the following are measured:

```text
[ ] Template Worker delay loops are gone from both hosts
[ ] mt5-worker runs the six source hosted jobs + source outbox processor
[ ] Achiever and StarwaveFX have independent connectors and checkpoints
[ ] GetDeals failure does not advance a checkpoint (test)
[ ] Raw + outbox commit atomically (test)
[ ] Crash after commit, before handle, retries once and is idempotent (test)
[ ] Poison row does not block ingest (test)
[ ] mt5-source and fix-dest claim disjoint type sets (test)
[ ] Callback / pump thread never calls FIX (test)
[ ] fix-worker has independent QUOTE and TRADE hosted sessions
[ ] TRADE lease fencing prevents a second owner (test)
[ ] MaySendNewOrderSingle is false when flag/db/quote/ready/lease fail (tests)
[ ] Disconnect after send marks unknown and does not resend (test)
[ ] Backoff policies match §6.2 and honor stoppingToken
[ ] Production plaintext FIX refuses to start
[ ] Secrets are not in committed appsettings
[ ] Health ports export §12 gauges; API does not invent “connected”
[ ] REAL_COPY_EXECUTION_ENABLED default false is read by code, not by folklore
```

Until then, Phase 1 / 4 / 7 / 8 remain **not done**, regardless of build success.

---

*End of A64. Product source was not modified. This file is the only output of the task.*
