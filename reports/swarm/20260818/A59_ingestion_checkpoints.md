# A59 — Architecture §12: `sync_checkpoints`, historical backfill, live events, periodic reconciliation, idempotent upserts on `(broker_id, ticket)`

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A59_ingestion_checkpoints.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §12 (lines 525–571), plus §§6–11, §13, §45, §57–60, §62, §67 Phase 1, §68, §72.6–7 |
| Catalog alignment | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` §5.8 `sync_checkpoints` / `ingestion_events` / `outbox_events`; §5.2 raw keys |
| Sequence alignment | `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md` Increment 3 (I3) + migration `0007` |
| Date | 2026-08-18 |
| Status | **Design only.** Product source was **not** modified. |
| Phase | Unlocks Phase 1 — Reliable MT5 ingestion (§67). Not a claim that Phase 1 is done. |

---

## 0. Verdict (measured, not aspirational)

Architecture §12 is the correct ingestion pattern. It is **not implemented** as a C# collector loop.

```text
Historical Backfill
+
Live Event Subscription
+
Periodic Reconciliation
```

Honest state on 2026-08-18:

| Capability | Measured state | Classification |
|---|---|---|
| §12 three-loop pattern in `apps/mt5-worker` | Template `while` + `Task.Delay(1000)` only | **MISSING** |
| `sync_checkpoints` table / entity / store | Named in `TraderDbContext` (`DbSet<SyncCheckpoints>`) and A20/A30. **No entity type, no `SyncCheckpointsConfiguration.cs`, no migration.** | **MISSING** |
| Idempotent upsert on `(broker_id, deal_ticket)` | EF sketch `Mt5DealsConfiguration` has `HasIndex("broker_id","deal_ticket").IsUnique()`. No writer, no `ON CONFLICT` SQL, no hash/revision. Domain `Mt5Deal` has `BrokerId` + `DealTicket`. | **EXISTS_NEEDS_REFACTOR** (index intent only) |
| Live validate → dedup → persist → outbox → commit | No live loop. `IBrokerConnector.SubscribeEventsAsync` is declared, not implemented. | **MISSING** |
| Periodic source reconciliation | No job. Do not confuse with destination `execution_reconciliation_*` (§42–44). | **MISSING** |
| Transactional outbox | `OutboxEventType` enum exists. No `outbox_events` writer. | **MISSING** |
| C++ `mt5_ledger::Store` | Real `ON CONFLICT DO NOTHING` on `(server_key, source_event_id)` and `(server_key, deal_ticket, revision_no)`. Identity is **`server_key`**, not `broker_id`. Parallel stack, not the v2 collector. | **EXISTS_NEEDS_REFACTOR** (sibling, not a substitute) |
| `GetDeals` completeness | C++ local `MT5Manager::GetDeals` is **one** `DealRequest` (no `DealRequestPage`). HTTP client pages cursors. History index can lag **>40 s**. `OnDealAdd` is expected to stay silent (`NO PUMP_MODE_DEALS`). | Binding constraint for this design |

**Phase 1 exit is not met.** This file pins the design so Increment 3 can be implemented without inventing a second cursor table or a global ticket key.

---

## 1. Binding laws (do not weaken)

Quoted / derived; all are mandatory for this design.

| # | Law | Source |
|---|---|---|
| L1 | Never treat login or ticket as globally unique. Identity is `broker_id + login`, `broker_id + deal_ticket`, `broker_id + order_ticket`, `broker_id + position_id`. | §10 |
| L2 | All source-side tables carry `broker_id NOT NULL`. | §10, A20 |
| L3 | Two brokers must not share a deals cursor. No single global checkpoint. | A20 §5.8 |
| L4 | Raw layer is as immutable as practical. Corrections are auditable. | §11 |
| L5 | Backfill is: read checkpoint → fetch history → normalize → **upsert idempotently** → persist checkpoint. Per **broker/account**. | §12 |
| L6 | Live is: validate → deduplicate → persist raw → write transactional outbox → commit. Then a **background** worker drains the outbox. | §12–13 |
| L7 | MT5 callbacks stay lightweight. Persist/queue only. No reconstruction, scoring, shadow, risk, or FIX on the pump / subscribe enumerator. | §12, §32, §72.6 |
| L8 | Persist before asynchronous processing. Crash after persist must be recoverable. | §72.7 |
| L9 | Do not invent source trades when MT5 is unavailable. Retry. Expose stale-source. Do not open new copy from stale source. | §62 |
| L10 | Completeness of `GetDeals([from,to])` requires following every page/cursor, or return `false`. Callers treat `false` as `dependency_unavailable` and **must not advance the checkpoint**. | `IMT5Client` contract, A12 |
| L11 | Destination FIX reconciliation (`execution_reconciliation_runs`) is **not** this design. Same word, different book. Source reconcile compares Manager history vs `mt5_*`. | §42–44 vs §12 |
| L12 | Redis is not the authority for orders, positions, or balances. Checkpoints live in PostgreSQL. | §5, §62 |
| L13 | Persist **all** deals (not only XAUUSD). Canonical tagging is reconstruction (Phase 2 / I4). | A30 I3 |
| L14 | Do not persist C++ `CacheExecutedDeal` synthetic high-bit tickets as `mt5_deals`. Those keys are a local ring hack for `SendTrade`, not broker evidence. Source collector is not the dealer. | `mt5_manager.cpp`, A04 |

---

## 2. Current surfaces this design binds to

Do not invent a second connector. Bind to what exists, and name the gaps.

| Surface | Path | Role in §12 |
|---|---|---|
| Architecture sketch | v2 §6 `IMt5BrokerConnector` | Conceptual. Exact members may match the SDK. |
| C# port (exists) | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | `GetGroupsAsync`, `GetAccountsAsync`, `GetDealsAsync(login, from, to)`, `GetPositionsAsync`, `GetServerTimeAsync`, `SubscribeEventsAsync`. **No `GetOrdersAsync`.** No implementation class. |
| Broker options | `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs` | `BrokerId` is first-class. `Mode` = `local` \| `remote`. |
| Domain deal | `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | `BrokerId`, `DealTicket`, `Login`, `OrderTicket`, `PositionId`, native volume, `DealTime`. |
| Domain recon input | `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | Same compound identity as string `BrokerId` + tickets. |
| EF unique (sketch) | `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5DealsConfiguration.cs` | Unique `(broker_id, deal_ticket)`. Maps shadow type `Mt5Deals` — **name mismatch** with domain `Mt5Deal`. |
| EF unique (sketch) | `Mt5AccountsConfiguration` | Unique `(broker_id, login)`. |
| EF unique (sketch) | `Mt5PositionsConfiguration` | Unique `(broker_id, position_ticket)` on table name `mt5_positions` — catalog name is **`mt5_positions_current`**. |
| EF unique (sketch) | `Mt5GroupsConfiguration` | Unique `(broker_id, group_id)` — catalog unique is **`(broker_id, group_name)`**. |
| Worker | `D:\Prop\apps\mt5-worker\Worker.cs` | Template. Must become supervisor of the three loops + outbox drain (A30 I3 jobs). |
| C++ history | `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` `GetDeals` | One-shot `DealRequest`. Must be treated as **incomplete-until-paged** when implementing the collector fetch. |
| C++ HTTP history | `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` `GetDeals` | Cursor / page loop; returns `false` on incomplete continuation. **This is the completeness contract to copy.** |
| C++ evidence ledger | `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp` | `ON CONFLICT DO NOTHING`. Map later; do not dual-write as a second SoT. |
| Time windows | `D:\Prop\mt5-sdk\src\services\mt5_time_window.h` | Windows must use **MT5 server time**, not host wall clock. |
| A20 catalog | `sync_checkpoints` UNIQUE `(scope_type, scope_id, stream_name)` | General scope (BROKER / VENUE / GLOBAL). |
| A30 migration note | `sync_checkpoints (broker_id, login, stream)` | Account-grained backfill. **This design unifies both** (section 4). |

`TraderDbContext` currently lists `DbSet<SyncCheckpoints>` and `ApplyConfiguration(new SyncCheckpointsConfiguration())` but **those types are not in the tree**. Treat that as a stub, not a store.

---

## 3. Why three loops, not one

| Loop | What it guarantees | What it cannot guarantee |
|---|---|---|
| **Historical backfill** | Restart-safe catch-up of the time-indexed Manager history (`DealRequest` / `DealRequestPage`). First useful census of ~5,000 logins. | Completeness of the **last ~40+ seconds** (history index lag). Completeness if `OnDealAdd` never fires. |
| **Live events** | Low-latency persist of pump/SSE events (`DealAdd`/`DealUpdate`/`Position*`/`Order*`/`User*`) so reconstruction is not waiting on `DealRequest`. | Completeness. `IMTManagerAPI` has **no `PUMP_MODE_DEALS`**. `OnDealAdd` is documented as likely silent. SSE/HTTP bridges can drop. |
| **Periodic reconciliation** | The **completeness** loop: re-fetch a trailing window (and, less often, open positions / pending orders / account census) and upsert anything the other two missed or that the broker later corrected. | Instantaneous live latency. Must not be the only path if we want trade-close → reconstruct in seconds. |

Rule: **backfill + reconcile prove the ledger; live accelerates it.** Phase 1 “idempotency proven” means the three loops can race and still leave **one row** per `(broker_id, ticket)`.

```text
                    ┌─────────────────────────────┐
                    │  IBrokerConnector / broker  │
                    │  (one instance per broker)  │
                    └──────────────┬──────────────┘
           ┌───────────────────────┼───────────────────────┐
           ▼                       ▼                       ▼
   HistoricalBackfill        LiveIngestion           SourceReconcile
   (per login stream)        (event enqueue)         (trailing window)
           │                       │                       │
           └───────────────┬───────┴───────────┬───────────┘
                           ▼                   ▼
                 RawMt5RecordWriter     SyncCheckpointStore
                 ON CONFLICT identity   after successful commit
                           │
                           ▼
                    same Postgres txn
                 raw row + ingestion_events
                 + outbox_events (if new/changed)
                           │
                           ▼
                    OutboxDispatchJob
                 (reconstruct / score / …)
```

---

## 4. `sync_checkpoints` — the only cursor table

Do **not** create `fix_checkpoints`, `deal_cursors`, or per-stream tables. A20 open question #4: venue cursors use the **same** table with `scope_type = 'VENUE'`.

### 4.1 Identity (unifies A20 + A30)

A20 UNIQUE `(scope_type, scope_id, stream_name)` cannot hold per-login deal cursors without stuffing login into `stream_name` or `scope_id`. A30 UNIQUE `(broker_id, login, stream)` cannot hold venue/global rows cleanly.

**Pinned unique key** (PostgreSQL 15+ `NULLS NOT DISTINCT`):

```text
sync_checkpoints_uk (scope_type, scope_id, login, stream_name)
```

| `scope_type` | `scope_id` | `login` | Typical `stream_name` |
|---|---|---|---|
| `BROKER` | `brokers.id` | `NULL` | `groups`, `accounts`, `symbols`, `ticks_xau` |
| `BROKER_ACCOUNT` | `brokers.id` | MT5 login | `deals_backfill`, `orders_history`, `positions_reconcile`, `account_snapshot` |
| `VENUE` | `execution_venues.id` | `NULL` | `security_list`, `order_mass_status` (later; not Phase 1) |
| `GLOBAL` | nil UUID | `NULL` | reserved; **never** a deals cursor |

`scope_id` for source rows **is** `broker_id`. Two brokers never share a row. `login` is first-class so 5,000 account cursors stay queryable.

Broker-wide deal backfill (optional bulk `DealRequestByGroup`) still writes **per-login** `BROKER_ACCOUNT` checkpoints when applying rows, so a restart mid-group does not re-scan finished logins as if they were new.

### 4.2 Target columns (key-relevant; not a migration to apply)

| Column | Type | Purpose |
|---|---|---|
| `id` | `uuid` PK | Surrogate |
| `scope_type` | `text` | `BROKER` / `BROKER_ACCOUNT` / `VENUE` / `GLOBAL` |
| `scope_id` | `uuid` | Broker or venue |
| `login` | `bigint` NULL | Required iff `BROKER_ACCOUNT` |
| `stream_name` | `text` | See §4.3 |
| `cursor_kind` | `text` | `TIME_TICKET` \| `REVISION` \| `SNAPSHOT` |
| `cursor_from_sec` | `bigint` NULL | Inclusive MT5 server-time seconds of the **next** fetch origin (already includes overlap) |
| `cursor_to_sec` | `bigint` NULL | Last successfully **closed** window end (server seconds) |
| `cursor_ticket` | `bigint` NULL | Last successfully applied ticket at `cursor_to_sec` (tie-break) |
| `high_water_sec` | `bigint` NULL | Max deal/order time successfully applied (monotonic; used for lag) |
| `high_water_ticket` | `bigint` NULL | Ticket at the high-water instant |
| `overlap_sec` | `int` NOT NULL DEFAULT 120 | Re-fetch tail. Must exceed measured `DealRequest` index lag (~40 s+) + skew |
| `status` | `text` | `IDLE` / `RUNNING` / `FAILED` / `PAUSED` / `CAUGHT_UP` |
| `lease_owner` | `text` NULL | Worker instance id |
| `lease_until` | `timestamptz` NULL | Lease expiry |
| `fencing_token` | `bigint` NOT NULL DEFAULT 0 | Incremented on each acquire; writers must echo it |
| `last_started_at` | `timestamptz` | |
| `last_success_at` | `timestamptz` | Dashboard “last successful history sync” (§48) |
| `last_error_at` | `timestamptz` | |
| `last_error` | `text` | No secrets |
| `rows_applied` | `bigint` NOT NULL DEFAULT 0 | Lifetime applied (including idempotent hits) |
| `rows_inserted` | `bigint` NOT NULL DEFAULT 0 | New identity rows |
| `duplicate_hits` | `bigint` NOT NULL DEFAULT 0 | `ON CONFLICT` no-ops |
| `incomplete_fetches` | `bigint` NOT NULL DEFAULT 0 | `GetDeals == false` |
| `payload` | `jsonb` | Stream-specific extras (page offset, last group name, …) |

CHECK:

```text
scope_type IN ('BROKER','BROKER_ACCOUNT','VENUE','GLOBAL')
(scope_type <> 'BROKER_ACCOUNT') OR (login IS NOT NULL)
(scope_type <> 'BROKER') OR (login IS NULL)
overlap_sec >= 60
```

FK: `scope_id → brokers.id` when `scope_type IN ('BROKER','BROKER_ACCOUNT')`.

### 4.3 Stream catalog (Phase 1 source)

| Stream | Scope | Fetch | Upsert target | Cursor |
|---|---|---|---|---|
| `groups` | BROKER | `GetGroupsAsync` (all manager groups, **not** §9 plan subset) | `mt5_groups` `(broker_id, group_name)` | `SNAPSHOT` (revision / last full enum time) |
| `accounts` | BROKER | Per group `GetAccountsAsync` / `GetGroupLogins` | `mt5_accounts` `(broker_id, login)` | `SNAPSHOT` + `payload.last_group` for resume mid-enum |
| `symbols` | BROKER | Symbol walk | `mt5_symbols` `(broker_id, source_symbol)` | `SNAPSHOT` |
| `deals_backfill` | BROKER_ACCOUNT | `GetDealsAsync(login, from, to)` **paged** | `mt5_deals` `(broker_id, deal_ticket)` | `TIME_TICKET` |
| `orders_history` | BROKER_ACCOUNT | History orders when the connector exposes them | `mt5_orders` `(broker_id, order_ticket)` | `TIME_TICKET` |
| `positions_reconcile` | BROKER_ACCOUNT | `GetPositionsAsync(login)` | `mt5_positions_current` `(broker_id, position_id)` | `SNAPSHOT` |
| `account_snapshot` | BROKER_ACCOUNT | `GetUser` / `GetAccount` | `mt5_account_snapshots` append + `mt5_accounts` upsert | `SNAPSHOT` |
| `deals_live` | BROKER | Optional watermark only | **not a skip cursor** | `TIME_TICKET` high-water for `mt5_backfill_lag` |
| `ticks_xau` | BROKER | Only if source actually yields ticks | `mt5_xau_ticks` | `TIME_TICKET`; empty + `system_events` if unsupported (§17) |

`deals_live` **must not** be used to drop live events. Live dedup is the unique ticket key, not a time cursor. The live watermark exists so the dashboard can show “last event” and so reconcile knows the live tail.

### 4.4 Lease + fencing (single writer per row)

Backfill and reconcile for the same `(scope, stream)` must not run two overlapping window advances.

Acquire (same transaction as the first write, or immediately before the fetch):

```text
UPDATE sync_checkpoints
   SET status = 'RUNNING',
       lease_owner = :worker,
       lease_until = now() + interval '2 minutes',
       fencing_token = fencing_token + 1,
       last_started_at = now()
 WHERE scope_type = :t AND scope_id = :id
   AND login IS NOT DISTINCT FROM :login
   AND stream_name = :stream
   AND (lease_until IS NULL OR lease_until < now() OR lease_owner = :worker)
 RETURNING fencing_token, cursor_from_sec, cursor_to_sec, cursor_ticket, overlap_sec;
```

If zero rows: insert the checkpoint (`ON CONFLICT DO NOTHING`) and retry, **or** skip (another worker holds the lease).

Every subsequent checkpoint write **must** include `AND fencing_token = :token`. A stolen lease makes the old worker’s advance a no-op. Fetched-but-not-yet-committed upserts remain safe because they are idempotent.

Heartbeat: extend `lease_until` every N seconds while `RUNNING`. On process death the lease expires; another worker resumes from the last **committed** cursor.

Do **not** put this lease only in Redis. Redis may cache the owner for the dashboard; PostgreSQL is the lock authority.

### 4.5 Advance rules (the whole point of a checkpoint)

1. **Never advance** if `GetDealsAsync` / page loop returned `false`, threw, or returned a partial page without a continuation token (HTTP `has_more` without cursor). Increment `incomplete_fetches`, set `FAILED`, keep the old cursor.
2. **Never advance past an uncommitted upsert.** Cursor update is in the **same transaction** as the last batch of raw upserts + outbox, **or** in a following transaction only after the previous commit is visible.
3. **Never advance on empty-because-unavailable.** Empty + `true` (no deals in window) **may** advance. Empty + `false` must not.
4. **Overlap is mandatory** for `TIME_TICKET` streams:

```text
next_from_sec = max(0, committed_to_sec - overlap_sec)
```

   Re-fetching the tail is cheap because upserts are idempotent. This is how we absorb the >40 s `DealRequest` index lag and late `OnDealUpdate`.

5. **Ticket tie-break.** Many deals share the same `deal.time` (1-second resolution on `DealData.time`). After applying a window, store `(cursor_to_sec, cursor_ticket = max ticket among rows with time == cursor_to_sec)`. The next apply still re-upserts the overlap; the ticket is for metrics and for optional “skip already-applied in-memory” only — **never** as a substitute for `ON CONFLICT`.
6. **Do not rewind** `high_water_sec` except on an explicit operator “reset checkpoint” audit action. `cursor_from_sec` may sit behind high-water because of overlap.
7. First run: if no row exists, `cursor_from_sec = configured_epoch` (default: `min(account.registration_sec, server_now - backfill_horizon_sec)`). Horizon is a broker option (suggest 365 days). Scoring “first 3 completed XAUUSD lifecycles” needs **enough** history; do not default to “last 24 h” for the initial backfill.

---

## 5. Historical backfill

Implements §12 “for each broker/account” and Phase 1 “history backfilled” / “restart-safe” (A28).

### 5.1 Per-login algorithm (`deals_backfill`)

```text
for each enabled broker (Achiever, StarwaveFX, future):
  ensure connector.IsConnected
  server_now = GetServerTimeAsync()          -- not DateTimeOffset.UtcNow
  pick next BROKER_ACCOUNT login (fair scheduler, §5.3)
  acquire lease on (BROKER_ACCOUNT, broker_id, login, 'deals_backfill')
  from = checkpoint.cursor_from_sec
         ?? min(registration, server_now - horizon)
  while from < server_now and !cancel:
    to = min(from + chunk_sec, server_now)   -- chunk e.g. 7 days
    deals = GetDealsComplete(login, from, to)  -- ALL pages or fail
    if deals is unavailable:
       mark FAILED; break                   -- do not invent, do not advance
    normalize each deal (broker_id stamped here)
    reject synthetic / ticket==0
    BEGIN
      for each deal: upsert_deal(...)       -- §8
      persist checkpoint:
         cursor_to_sec = to
         cursor_from_sec = to - overlap_sec
         high_water = max(high_water, max deal.time)
         fencing_token must match
      COMMIT
    from = to - overlap_sec                 -- only after commit
  release lease → CAUGHT_UP if to reached server_now
```

`GetDealsComplete` is the L10 wrapper:

- Remote: already paged in `MT5HttpClient::GetDeals` (max 10_000 requests; identical cursor → fail).
- Local: **must** call `DealRequestPage(login, from, to, offset, total)` until a short page. Today’s `MT5Manager::GetDeals` does **not**. The C# collector must not treat a single `DealRequest` success as proof of completeness for wide windows. Until the native wrapper pages, keep chunks small enough that a silent cap is unlikely, and still run reconcile.

`GetDealsAsync` on `IBrokerConnector` currently takes `DateTimeOffset`. Convert with server-time seconds via `GetServerTimeAsync` + the same rules as `resolveMt5TimeWindow`. Store `timestamptz` on the row; keep seconds on the checkpoint.

### 5.2 Normalize (before upsert)

Stamp and validate **before** touching Postgres:

| Check | Action |
|---|---|
| `BrokerId` missing / default | Fail the batch (programming error). Never infer from login. |
| `DealTicket == 0` | Drop + metric `mt5_invalid_deals_total{reason="zero_ticket"}`. Do not synthesize. |
| `Login == 0` | Drop + metric. |
| Ticket in synthetic high-bit range used by `CacheExecutedDeal` | Drop. Collector is not the dealer. |
| `Action` not in `DealAction` | Persist raw `action` as integer **and** `Unknown` only if the domain enum is extended; do not drop — balance/credit deals are real. Reconstruction later ignores non-trading (`NormalizedDeal.IsTradingDeal`). |
| `VolumeNative` | Persist **native** integer (`IMTDeal::Volume()`, scale 10_000 — see `VolumeConverter` / A37). Do not convert to lots on ingest. |
| `DealTime` | From MT5 server seconds → `timestamptz`. Do not substitute host now. |
| `Symbol` | Persist source string as-is (`XAUUSD.`, `GOLD`, …). No canonical filter. |

Surrogate `Mt5Deal.Id`: either `gen_random_uuid()` on insert, or `DeterministicGuid.FromString($"{brokerId:N}:{dealTicket}")` (`D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`). Unique identity remains `(broker_id, deal_ticket)`, **not** the GUID. Deterministic GUIDs make outbox `aggregate_id` stable across retries.

### 5.3 Scheduling ~5,000 accounts

Do not `GetDeals` all logins on one thread for all history on startup.

| Rule | Value |
|---|---|
| Fairness | Round-robin / min-`last_success_at` heap per broker. Recently live logins (live watermark or open positions) get a higher priority **lane**, not exclusive lockout of cold accounts. |
| Concurrency | Cap in-flight history requests per broker (start: 2–4). Manager `DealRequest` is network + mutex-serialized on the local pump (`m_mutex`). Over-parallelizing fights the same connection. |
| Chunk | 1–7 days per commit. Smaller chunks → more frequent restart-safe advances. |
| Horizon | Configurable. Initial full backfill may take hours; that is acceptable. `mt5_backfill_lag` is `server_now - min(high_water_sec)` over accounts still not `CAUGHT_UP`. |
| Groups | `groups` + `accounts` streams run first (or continuously) so new logins appear as `BROKER_ACCOUNT` checkpoint rows. Plan mappings (§9) **never** filter discovery. |
| Dual broker | Independent schedulers. Achiever pool size 8 / StarwaveFX 4 (architecture §7–8) are connection-pool hints, not a shared cursor. |

### 5.4 Orders and positions during backfill

- **Positions:** not a time series. After deals for a login are `CAUGHT_UP` (or periodically), run `positions_reconcile`: `GetPositionsAsync` → upsert open rows → **delete** (or mark closed) any `mt5_positions_current` row for that `(broker_id, login)` whose `position_id` was **not** returned. History of closed positions lives in `mt5_deals` + `reconstructed_trades`, never in the current book (A20).
- **Orders:** `IBrokerConnector` has no history `GetOrdersAsync`. Pending `GetOrders` exists only on C++ `IMT5Client` (open/pending). Phase 1 can persist pending orders on reconcile when the connector is extended. Do not block deal backfill on orders.
- **Account snapshots:** append-only `(broker_id, login, snapshot_at)`. Do not overwrite history.

### 5.5 Restart

Crash mid-window: lease expires; next worker re-fetches `[cursor_from_sec, …]` which already includes overlap. Duplicate tickets hit `ON CONFLICT` and increment `duplicate_hits`. Integration test required: `BackfillRestartTests` (A30 I3, §60 “MT5 backfill/restart”).

---

## 6. Live events

### 6.1 Subscribe contract

`IBrokerConnector.SubscribeEventsAsync` (`Mt5BrokerEvent`):

```text
BrokerId, Login, DealTicket?, Deal?, PositionTicket?, Position?, EventTimeUtc, EventType
```

Expected `EventType` values (normalize at the adapter; do not invent ML types here):

```text
DealAdd | DealUpdate | DealDelete
OrderAdd | OrderUpdate | OrderDelete
PositionAdd | PositionUpdate | PositionDelete
UserUpdate | AccountUpdate
```

C++ already pushes these onto `MT5EventQueue` from sinks (`OnDealAdd` / `OnOrder*` / `OnPosition*` / user sinks). The **adapter** (native or HTTP SSE) must:

1. Stamp `BrokerId` from the connector instance (never from payload).
2. Enqueue and return. **No DB, no HTTP, no reconstruction** on the pump thread (`IMT5Client::SubscribeTicks` / deal sink contract).
3. Bounded queue; drop-oldest + metric if saturated. Drops are recovered by reconcile, not by blocking the Manager pump.

### 6.2 Live apply path (C# worker, not the sink)

Exactly §12, one `TraderDbContext` transaction (or explicit `BEGIN`):

```text
MT5 event (dequeued)
   ↓
validate (same rules as §5.2)
   ↓
deduplicate via unique key (not an in-memory set)
   ↓
persist raw record (upsert §8)
   ↓
write ingestion_events (append / ON CONFLICT DO NOTHING)
   ↓
write outbox_events iff the raw identity is new OR payload_hash changed
   ↓
commit
   ↓
bump deals_live high-water (best-effort; may be a separate tiny txn)
```

Then `OutboxDispatchJob` processes `outbox_events`. That is the **only** path into reconstruction / scoring / shadow / copy-intent.

### 6.3 Dedup is durable, not process-local

Live + backfill + reconcile will deliver the same deal many times:

- `OnDealAdd` then `OnDealUpdate`
- live then lagged `DealRequest`
- overlap re-fetch
- worker restart
- dual subscribe after reconnect

In-memory sets die with the process. Dedup key is the table UNIQUE.

`ingestion_events` unique `(broker_id, source_event_id)` (A20):

```text
source_event_id =
  deal:{ticket}:{event_kind}          -- add|update|delete
  order:{ticket}:{event_kind}:{state}
  position:{ticket}:{event_kind}
  user:{login}:{revision}
```

If the bridge does not supply a stable event id, **derive** it as above. Do not use a host GUID as the unique key (that would store every retry).

Exact-duplicate payload (`payload_hash` equal): `ON CONFLICT DO NOTHING` on `ingestion_events`, no outbox, increment `mt5_duplicate_deals_total`.

### 6.4 `DealDelete` / canceled deals

- `DEAL_BUY_CANCELED` / `DEAL_SELL_CANCELED` are **new tickets or the same ticket with a new action**. Persist the row. Reconstruction treats them as non-lifecycle unless later specified.
- `DealDelete` event: **do not physically delete** `mt5_deals`. Set `deleted_at` (column to add on the projection) and append `ingestion_events` `event_kind=delete`. Reconcile can confirm absence from a fresh `GetDeals` window.
- `PositionDelete`: remove from `mt5_positions_current` only.

### 6.5 Live is optional for completeness, required for latency

If `SubscribeEventsAsync` is empty forever (no `PUMP_MODE_DEALS`, HTTP bridge without SSE):

- Backfill + reconcile still satisfy Phase 1 “live deals persisted” **only** if reconcile interval ≤ index lag + chunk, and we honestly expose `mt5_live_events_total=0`.
- Do **not** fake live rows from `CacheExecutedDeal`.
- Prefer adding a **short-interval reconcile** (e.g. 15–30 s) for logins with open positions or recent deals so close → reconstruct does not wait for the hourly job.

### 6.6 Reconnect

On connector drop:

1. Set `mt5_connected=0`, increment `mt5_reconnects`.
2. Do not invent deals.
3. Mark source stale in `system_events`.
4. After reconnect: run `positions_reconcile` for recently active logins + a trailing `deals` reconcile window of at least `overlap_sec` (suggest 10 minutes).
5. Resume subscribe. Do not reset `deals_backfill` cursors to epoch.

---

## 7. Periodic reconciliation (source ledger)

This is §12’s third loop and Phase 1 “reconciliation working”. It is **not** §42 startup FIX reconcile.

### 7.1 What “reconciled” means here

For a broker, as of `GetServerTime()` minus a safety lag `L` (default 120 s, ≥ overlap):

| Check | Method | Mismatch |
|---|---|---|
| Deal coverage | `GetDealsComplete(login, from, to)` vs `mt5_deals` in that window | Missing local → upsert. Extra local (not deleted, not canceled) → `system_events` / reconcile issue row. Hash mismatch → correction path §8.3 |
| Open positions | `GetPositionsAsync(login)` vs `mt5_positions_current` | Missing local → upsert. Extra local → delete current book. Qty/side/symbol mismatch → upsert projection + issue |
| Account census | group login lists vs `mt5_accounts` | Missing local → upsert account. Extra local → `is_active=false` (do not delete) |
| Groups | `GetGroupsAsync` vs `mt5_groups` | Upsert; do not delete vanished groups (keep last_discovered) |

Do **not** fail closed on a single login `GetDeals == false`; skip that login, increment `incomplete_fetches`, continue others. The **broker** is stale if the connector is down (§62).

### 7.2 Cadence

| Lane | Interval (starting point) | Window |
|---|---|---|
| Hot deals | 15–60 s | Last `overlap_sec` … 15 min, logins with live events or open positions |
| Warm deals | 5 min | Last 2 hours, logins with `last_success` older than interval |
| Cold deals | 1 h | Last 24 h, remaining logins (fair walk) |
| Positions | 30–60 s hot / 5 min all open | Full snapshot per login |
| Groups + accounts | 5–15 min | Full enum; resume via `accounts` checkpoint `payload.last_group` |
| Deep audit | Daily | Re-fetch last 7 days for a sample / all; compare counts |

Tune from metrics, not from hope. The hot lane exists specifically because live sinks may be silent and `DealRequest` lags.

### 7.3 Issue durability

Do **not** reuse `execution_reconciliation_issues` (destination). If a source issue table is not in the 47-table catalog, persist:

- `system_events` (`event_type = source_reconcile_mismatch`) with `broker_id`, login, ticket, fingerprint
- counters: `mt5_reconcile_missing_total`, `mt5_reconcile_extra_total`, `mt5_reconcile_hash_mismatch_total`

A dedicated `source_reconciliation_issues` table is **out of catalog**. Do not create it in Phase 1. Catalog union is closed at 47 (A20).

### 7.4 Interaction with checkpoints

Reconcile **does** update `positions_reconcile` / `account_snapshot` snapshot cursors (`last_success_at`).

Reconcile **does not** jump `deals_backfill.cursor_to_sec` forward past un-backfilled history. It may refresh `high_water_sec` when it applies a newer deal. It uses its own stream if needed:

```text
stream_name = deals_reconcile   -- optional; or reuse deals_backfill with a payload.reconcile_from_sec
```

Prefer a **separate** `deals_reconcile` stream on the same table so a hot 15-minute reconcile cannot mark a 365-day backfill `CAUGHT_UP`.

### 7.5 Stale-source flag

If any of these hold, source is stale for copy (later phases; still emit `system_events` in Phase 1):

- connector disconnected
- `server_now - max(deals_live.high_water, deals_reconcile.last_success)` > `stale_source_sec` (suggest 120 s once `CAUGHT_UP`, else “backfilling” is a different dashboard state)
- `incomplete_fetches` rising without successes

Dashboard §48: `Last event`, `Last successful history sync`, `Deal ingest rate`.

---

## 8. Idempotent upserts on `(broker_id, ticket)`

This is the atomic property that makes the three loops safe.

### 8.1 Identity matrix (source ticket stores)

| Table | UNIQUE | Ticket column | Mutability |
|---|---|---|---|
| `mt5_deals` | `(broker_id, deal_ticket)` | deal ticket | Projection of latest known raw deal; evidence in `ingestion_events` |
| `mt5_orders` | `(broker_id, order_ticket)` | order ticket | Same |
| `mt5_positions_current` | `(broker_id, position_id)` | position ticket | Live book; delete on close |
| `mt5_accounts` | `(broker_id, login)` | login (not a ticket) | Upsert census |
| `mt5_groups` | `(broker_id, group_name)` | name | Upsert discovery |
| `mt5_symbols` | `(broker_id, source_symbol)` | symbol | Upsert metadata |
| `ingestion_events` | `(broker_id, source_event_id)` | event id | Append / DO NOTHING |
| `mt5_account_snapshots` | `(broker_id, login, snapshot_at)` | n/a | Append |
| `mt5_xau_ticks` | `(broker_id, source_symbol, time_msc, flags, ingest_seq)` | n/a | Append / DO NOTHING |

**Never** unique `deal_ticket` alone. Achiever ticket `1001` and StarwaveFX ticket `1001` are different facts.

**Never** unique `(broker_id, order_ticket)` on `mt5_deals` (one order → many deals).

EF sketches already intend the deal/account/position uniques. They must be **constraints**, not just indexes, so `ON CONFLICT` works.

Domain types today (`Mt5Deal`, `Mt5Account`, `Mt5Position`) carry `BrokerId` + ticket/login. That is the correct equality. Shadow EF types (`Mt5Deals`, `Brokers`, …) should map **those** classes, not a parallel plural model.

### 8.2 SQL shape — deals (canonical)

Suggested writer SQL (design; Infrastructure implements via EF or Npgsql):

```sql
INSERT INTO mt5_deals (
    id, broker_id, deal_ticket, login, order_ticket, position_id,
    symbol, action, entry, volume_native, price, profit, commission, swap,
    deal_time, comment, payload_hash, revision_no,
    first_seen_at, last_seen_at, ingested_at
) VALUES (
    :id, :broker_id, :deal_ticket, :login, :order_ticket, :position_id,
    :symbol, :action, :entry, :volume_native, :price, :profit, :commission, :swap,
    :deal_time, :comment, :payload_hash, 1,
    now(), now(), now()
)
ON CONFLICT (broker_id, deal_ticket) DO UPDATE SET
    last_seen_at = now()
    -- remaining columns only if payload changed; see 8.3
WHERE mt5_deals.payload_hash IS DISTINCT FROM EXCLUDED.payload_hash
RETURNING (xmax = 0) AS inserted, payload_hash;
```

`payload_hash` = SHA-256 hex of a **canonical JSON** of broker-owned fields (not `id`, not ingest timestamps). Same algorithm as C++ `mt5_ledger` (`isSha256Hex`).

Companion:

```sql
INSERT INTO ingestion_events (
    id, broker_id, source_event_id, entity_type, entity_ticket,
    login, event_kind, occurred_at, payload, payload_hash, ingestion_run_id
) VALUES (...)
ON CONFLICT (broker_id, source_event_id) DO NOTHING
RETURNING id;
```

Outbox (same transaction, only if deal inserted **or** hash changed):

```sql
INSERT INTO outbox_events (
    id, aggregate_type, aggregate_id, event_type, dedupe_key, payload, created_at
) VALUES (
    gen_random_uuid(),
    'mt5_deal',
    :deal_id,
    'DealPersisted',          -- not TradeCompleted; reconstruction emits that later
    :broker_id || ':' || :deal_ticket || ':' || :payload_hash,
    :payload,
    now()
)
ON CONFLICT (aggregate_type, aggregate_id, event_type, dedupe_key) DO NOTHING;
```

`OutboxEventType` today is `TradeCompleted | ScoreUpdate | ShadowCopyIntent | RiskCheckRequest | NotificationEvent`. Add a **raw persist** type (name pin: `DealPersisted` / `RawDealUpserted`) rather than overloading `TradeCompleted`. A deal is not a logical trade (§14).

### 8.3 Correction policy (immutability vs upsert)

A20: “upsert on the compound ticket key; corrections are new revisions or audit rows, **not silent overwrites of history**.”

C++ ledger: **never** updates a revision; new `revision_no` + `ON CONFLICT DO NOTHING`.

Pinned hybrid for v2 `mt5_deals` (one current row, as catalogued):

| Incoming vs stored | `mt5_deals` | `ingestion_events` | `outbox_events` |
|---|---|---|---|
| New ticket | INSERT | INSERT | INSERT `DealPersisted` |
| Same ticket, same hash | `last_seen_at` only (optional) | DO NOTHING | none — `mt5_duplicate_deals_total++` |
| Same ticket, different hash | UPDATE projection; `revision_no += 1`; keep `first_seen_at` | INSERT new `source_event_id` (`deal:{ticket}:update:{revision}`) | INSERT `DealCorrected` |
| Delete event | set `deleted_at`; do not DROP | INSERT `delete` | INSERT `DealDeleted` |

The **previous** payload remains in `ingestion_events.payload` (jsonb). That is the audit trail. Do not add `mt5_deals_ledger` as a second SoT in .NET.

If C++ `mt5_deals_ledger` keeps running in the same database, treat it as the native collector’s private evidence. Map into `mt5_deals` with `broker_id` resolved from `server_key` via `brokers` / `broker_connections`. **One writer** of `mt5_deals` (the C# `RawMt5RecordWriter`). Dual writers will fight revisions.

### 8.4 Orders

Same pattern: `ON CONFLICT (broker_id, order_ticket)`. State transitions (placed → filled → canceled) are expected hash changes. Always append `ingestion_events`.

### 8.5 Positions (current book)

```sql
INSERT INTO mt5_positions_current (...)
ON CONFLICT (broker_id, position_id) DO UPDATE SET
    volume = EXCLUDED.volume,
    price_current = EXCLUDED.price_current,
    profit = EXCLUDED.profit,
    swap = EXCLUDED.swap,
    time_update = EXCLUDED.time_update,
    last_seen_at = now();
```

Close path: `DELETE FROM mt5_positions_current WHERE broker_id = :b AND position_id = :p` (or `login = :l AND position_id NOT IN (:open_set)` on snapshot reconcile).

Do not unique-constrain quantity.

### 8.6 Groups and accounts

```sql
INSERT INTO mt5_groups (..., group_name, last_discovered_at)
ON CONFLICT (broker_id, group_name) DO UPDATE
   SET last_discovered_at = now(), /* mutable metadata */;
```

Catalog unique is **`(broker_id, group_name)`**, not `(broker_id, group_id)`. The current `Mt5GroupsConfiguration` unique on `group_id` is **wrong** relative to A20 and must be corrected when the real entity is mapped. Group names are broker-local (`demo\Maxmaster`, `demo\yo-2step`).

```sql
INSERT INTO mt5_accounts (..., login, last_synced_at)
ON CONFLICT (broker_id, login) DO UPDATE
   SET group_name = EXCLUDED.group_name,
       balance = EXCLUDED.balance,
       equity = EXCLUDED.equity,
       last_synced_at = now();
```

### 8.7 What must not be upserted as a ticket

| Bad key | Why |
|---|---|
| `login` only | Cross-broker collision |
| `deal_ticket` only | Cross-broker collision |
| Deterministic GUID only | Fine as PK, illegal as the only uniqueness |
| `(broker_id, symbol, deal_time)` | Two deals in the same second |
| FIX `cl_ord_id` on source tables | Destination identity |
| `server_key` on v2 tables | C++ private; map to `broker_id` first |

### 8.8 Application-level guard

Even with SQL constraints, the writer should:

1. Refuse `broker_id == Guid.Empty`.
2. Refuse `ticket == 0`.
3. Use `ON CONFLICT` — never `SELECT` then `INSERT` (race).
4. Catch unique-violation as a successful idempotent hit if a concurrent worker won the insert.
5. Never catch-and-ignore other SQL errors.

Unit test required: **“MT5 deal deduplication”** (§60) — same `(broker_id, ticket)` twice → one row; same ticket **different** `broker_id` → two rows.

---

## 9. Transactions, outbox, and crash model

### 9.1 Unit of work

| Path | In one Postgres transaction |
|---|---|
| Live single event | raw upsert + `ingestion_events` + optional `outbox_events` |
| Backfill batch | N raw upserts + N ingestion rows + outbox for new/changed + **checkpoint advance** |
| Reconcile batch | same as backfill for the window; checkpoint of `deals_reconcile` / `positions_reconcile` |

If the process dies after COMMIT: outbox dispatcher is restart-safe (`processed_at IS NULL`). If it dies before COMMIT: event is re-delivered by live/backfill/reconcile; upsert is a no-op.

### 9.2 What the outbox is allowed to trigger (later)

§13 kinds (after reconstruction exists):

```text
TradeCompleted
ScoreUpdate
ShadowCopyIntent
RiskCheckRequest
NotificationEvent
```

Phase 1 only needs `DealPersisted` / `DealCorrected` (and maybe `AccountUpserted`). Do **not** emit `TradeCompleted` from a raw deal. `TradeReconstructor` already groups by `(brokerId, login, positionId)` — that runs in the outbox handler, not the sink.

### 9.3 Kafka

Do not introduce it. §13 is explicit. Throughput at ~5,000 accounts is a Postgres outbox problem first.

---

## 10. Time, lag, and the 40-second hole

| Clock | Use |
|---|---|
| `GetServerTimeAsync()` / `IMT5Client::GetServerTime` | Window `to`, lag, checkpoints |
| Host UTC | `ingested_at`, leases, logs (`occurred_at` of our process) |
| `DealData.time` | Deal event time (server seconds). Persist as `deal_time timestamptz` |

`DealRequest` history index can lag **>40 s** after a close (`imt5_client.h`, `mt5_manager.cpp`). Consequences:

1. Backfill/reconcile windows that end at `server_now` will **miss** brand-new deals even when `GetDeals` returns `true` + empty.
2. Therefore `overlap_sec >= 120` and a hot reconcile lane are mandatory, not optional polish.
3. Live persist (if sinks fire) fills the hole. If sinks do not fire, accept reconstruction delay up to `hot_reconcile_interval + lag`.
4. Never mark `CAUGHT_UP` based on “empty last 10 seconds”.

`mt5_backfill_lag` (§58) = `server_now - high_water_sec` for the worst account still in `deals_backfill`, or 0 when all `CAUGHT_UP` and live/reconcile high-water is fresh.

---

## 11. Failure rules (ingestion-specific)

| Failure | Behavior |
|---|---|
| Connector down | No invented rows. Retry connect. `mt5_connected=0`. Stale-source. Checkpoints unchanged. |
| `GetDeals` false / incomplete page | Do not advance that cursor. `incomplete_fetches++`. Try another login. |
| Unique violation | Treat as idempotent success if key matches; else fail the batch. |
| Hash mismatch | Correction path §8.3, never silent drop. |
| Outbox insert fails | Roll back the whole unit of work. Raw and outbox stay paired. |
| Poison payload (unparseable) | `system_events` + skip that event; do not stall the queue forever. Persist raw bytes in `ingestion_events` if `broker_id`+ticket are known. |
| Worker split-brain | Fencing token on checkpoint; upserts still idempotent if both write. |
| Database down | Fail closed. No Redis fallback ledger. §62. |

---

## 12. Metrics, logs, dashboard

### 12.1 Metrics (§58 + extras)

| Metric | Meaning |
|---|---|
| `mt5_connected{broker}` | Gauge |
| `mt5_reconnects{broker}` | Counter |
| `mt5_events_total{broker,event_type}` | Live dequeued |
| `mt5_deals_total{broker}` | Rows inserted (new identity) |
| `mt5_duplicate_deals_total{broker}` | Same hash / DO NOTHING |
| `mt5_deal_corrections_total{broker}` | Hash mismatch updates |
| `mt5_backfill_lag{broker}` | Seconds (see §10) |
| `mt5_outbox_backlog` | `outbox_events` unprocessed |
| `mt5_reconcile_missing_total` | Broker had, we did not |
| `mt5_invalid_deals_total{reason}` | ticket 0, synthetic, … |
| `mt5_incomplete_fetches_total` | L10 failures |

### 12.2 Logs (§57)

Every ingest log line includes `correlation_id`, `broker_id`, `source_login`, ticket, `stream_name`, `fencing_token` when holding a lease. Never log manager passwords / proxy passwords (`Mt5BrokerOptions.Password`).

### 12.3 Dashboard

§48 brokers page: connection, group count, account count, deal ingest rate, last event, last successful history sync, pool usage, reconnects. Source reconcile health is **MT5 ingestion health** (§47), not the later cTrader Reconciliation page.

---

## 13. Tests (acceptance for this design)

Must exist before anyone claims “idempotency proven” (§67 Phase 1, §68, §60).

### 13.1 Unit (`tests/Unit`)

| Class (A27 / A30) | Asserts |
|---|---|
| `Mt5DealDeduplicatorTests` / deal writer tests | Same `(broker_id, ticket)` → one row; different broker → two rows |
| `DealNormalizerTests` | Stamps `BrokerId`; rejects ticket 0; keeps native volume; does not drop `GOLD` |
| `SyncCheckpointAdvanceTests` | No advance on `GetDeals=false`; overlap rewind of `cursor_from`; fencing mismatch refuses write |
| `CompoundIdentityTests` | Ticket equality is both parts |
| `DualBrokerIsolationTests` | Achiever vs StarwaveFX identical numeric tickets coexist |

### 13.2 Integration (`tests/Integration`, real Postgres / Testcontainers — not InMemory as the only proof)

| Class | Asserts |
|---|---|
| `BackfillRestartTests` | Kill after N chunks; resume; row count = unique tickets; checkpoint moved |
| `LiveThenRestartNoDupesTests` | Replay same `Mt5BrokerEvent` batch twice → one `mt5_deals` row, one unprocessed-or-deduped outbox |
| `ReconciliationFillsGapsTests` | Insert subset; reconcile against a fake complete `GetDeals` → missing tickets appear |
| `OutboxProcessAfterCrashTests` | Commit raw+outbox; crash before dispatch; dispatcher marks processed exactly once |
| `AccountSyncCheckpointTests` | Mid-group enum resume via `accounts` payload |
| `GroupDiscoveryIdempotencyTests` | Full group walk twice → same `(broker_id, group_name)` set; plan map not used as filter |

### 13.3 Replay (§60)

Historical recorded Manager events → same `mt5_deals` hashes. Reconstruction is a later stage of the same replay pipeline, not part of the ingest assertion.

---

## 14. Mapping to A30 Increment 3 (implementation homes)

Design-only file list (do not create from this report):

| Home | Responsibility |
|---|---|
| `src/Application/Ingestion/HistoricalBackfillService.cs` | §5 |
| `src/Application/Ingestion/LiveIngestionService.cs` | §6 |
| `src/Application/Ingestion/IngestionReconciliationService.cs` | §7 |
| `src/Application/Ingestion/DealNormalizer.cs` (and order/position) | §5.2 |
| `src/Infrastructure/Persistence/Repositories/RawMt5RecordWriter.cs` | §8 |
| `src/Infrastructure/Persistence/Repositories/SyncCheckpointStore.cs` | §4 |
| `src/Infrastructure/Persistence/Outbox/TransactionalOutboxWriter.cs` | §9 |
| `apps/mt5-worker/Hosting/BackfillJob.cs` | scheduler §5.3 |
| `apps/mt5-worker/Hosting/LivePumpJob.cs` | dequeue subscribe |
| `apps/mt5-worker/Hosting/ReconciliationJob.cs` | §7.2 lanes |
| `apps/mt5-worker/Hosting/OutboxDispatchJob.cs` | drain |
| Migration `0007` | `sync_checkpoints`, `outbox_events`, `ingestion_events` |
| Migration `0006` | unique `(broker_id, deal_ticket)` etc. |

`IBrokerConnector` stays the only broker I/O. Application services must not reference Manager SDK headers.

---

## 15. Gaps the implementer must not paper over

1. **`SyncCheckpoints` / `OutboxEvents` types are referenced and absent.** DbContext will not compile as a complete model until entities + configurations exist and names match domain (`Broker` not `Brokers`, `Mt5Deal` not `Mt5Deals`) **or** the stub is removed.
2. **`Mt5GroupsConfiguration` unique `(broker_id, group_id)` contradicts A20 `(broker_id, group_name)`.** Use the catalog.
3. **`Mt5PositionsConfiguration` table `mt5_positions` contradicts catalog `mt5_positions_current`.** Use the catalog name.
4. **`Mt5DealsConfiguration` columns (`open_time`/`close_time`/`volume` decimal) do not match domain `Mt5Deal` (`DealTime`, `VolumeNative`, action/entry enums).** Writer must follow the domain + §8 columns (`payload_hash`, `revision_no`, `first_seen_at`).
5. **Local `GetDeals` is not paged.** Completeness is unproven on wide windows. Page or keep chunks small **and** reconcile.
6. **Live deals may never arrive via pump.** Completeness is reconcile + backfill.
7. **C++ ledger is not `mt5_deals`.** `server_key` ≠ `broker_id`. Do not dual-write.
8. **`IBrokerConnector` has no orders history API.** Do not block Phase 1 deals on orders.
9. **Do not persist synthetic `CacheExecutedDeal` tickets.**
10. **Do not emit `TradeCompleted` from ingest.**

---

## 16. Open questions (do not invent a second table)

1. **Position-ticket reuse after close** — A20 Q1. Current book is safe (row deleted). `reconstructed_trades` stays `(broker_id, position_id)` until a broker is proven to reuse tickets.
2. **How far back is “full history”?** Default horizon 365 d + registration, configurable. Operator may reset a login checkpoint (audited) to epoch.
3. **Whether to add `deals_reconcile` as its own `stream_name`** — this design says **yes** (same table). If implementers collapse it into `deals_backfill.payload`, they must still prevent a short reconcile from marking a multi-month backfill complete.
4. **`DealPersisted` vs extending `OutboxEventType`** — must be added; do not reuse `TradeCompleted`.
5. **Venue streams** — same table, later phases. No `fix_checkpoints`.

---

## 17. Phase 1 definition of done (ingestion slice)

All of the following, with evidence on disk (tests + metrics), not chat:

```text
[ ] Achiever and StarwaveFX each have isolated checkpoint rows (no shared deals cursor)
[ ] All manager groups upserted on (broker_id, group_name); plan map is overlay only
[ ] ~5,000 accounts upserted on (broker_id, login); restart resumes the accounts stream
[ ] deals_backfill is TIME_TICKET + overlap; restart does not duplicate rows
[ ] GetDeals incompleteness does not advance a cursor
[ ] Live path (if events exist) is validate → dedup → raw → outbox → commit
[ ] Callbacks / subscribe enumerator do no heavy work
[ ] Periodic reconcile inserts missed deals and repairs the current position book
[ ] ON CONFLICT (broker_id, deal_ticket) proven: duplicate feed ⇒ one row
[ ] Same deal_ticket on two brokers ⇒ two rows
[ ] Corrections change hash/revision and append ingestion_events
[ ] mt5_duplicate_deals_total and mt5_backfill_lag are observable
[ ] No synthetic dealer tickets in mt5_deals
[ ] Kafka not introduced
[ ] Redis not used as deal/position/checkpoint SoT
```

Until those boxes have measured evidence, **do not** claim Architecture §12 or Phase 1 ingestion is done.

---

## 18. Sources (read, not modified)

- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§6–13, 45, 48, 57–60, 62, 67–68, 72
- `D:\Prop\reports\swarm\20260818\A20_table_catalog.md`
- `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md`
- `D:\Prop\reports\swarm\20260818\A04_mt5_csharp_vs_sdk.md`
- `D:\Prop\reports\swarm\20260818\A07_mt5_worker_audit.md`
- `D:\Prop\reports\swarm\20260818\A12_imt5_client_map.md`
- `D:\Prop\reports\swarm\20260818\A14_mt5_manager_local.md`
- `D:\Prop\reports\swarm\20260818\A17_ticks_and_ledger.md`
- `D:\Prop\reports\swarm\20260818\A27_test_inventory.md`
- `D:\Prop\reports\swarm\20260818\A28_phases_gates.md`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Domain\Entities\Mt5Deal.cs`
- `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5DealsConfiguration.cs`
- `D:\Prop\mt5-sdk\src\core\imt5_client.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (`GetDeals`, `OnDealAdd`, `cacheRecentDeal`)
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` (`GetDeals` paging)
- `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`DealRequest`, `DealRequestPage`)

**Product source was not modified.** This file is the A59 design only.
