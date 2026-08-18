# A78 — Unique `(broker_id, deal_ticket)` upsert, `ingestion_events`, duplicate metrics

| Field | Value |
|---|---|
| Agent | A78 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A78_deal_idempotency.md` |
| Product source edited | **No** |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§10–13, 45, 57–58, 60, 67 Phase 1, 72.3 / 72.6–7 |
| Adjacent specs (do not contradict) | A01 identity VOs, A02 O1–O5, A03 table gap, A04 in-memory ring, A07 worker loops, A09/A10/A27 tests, A17/A18 C++ ledger, A20 catalog, A21 reconstruct-time skip, A30 I1, A37 enums, A38 volume, A41 ingest TX, A50 metric names |
| Status | specification only — **no product source modified** |

This file is the **binding persist-time deal-idempotency design** for Phase 1 (“idempotency proven”). It does not implement code. When Increment 1 / Phase 1 writers land, they must follow this file.

Reconstruction apply-time “already applied this ticket” (A21 §6 step 1) is a **different layer**. It must not share `mt5_duplicate_deals_total`.

---

## 0. Verdict (honest)

Phase 1 exit requires **duplicate deal / event insert does not create duplicate rows** (A28) and **`mt5_duplicate_deals_total`** (§58, A50). That is **not proven**. Pieces exist; the durable path does not.

| Capability | Classification | Evidence |
|---|---|---|
| §10 compound identity law | specified, not encoded | no `BrokerDealTicket` VO (`A01`); `Mt5Deal.BrokerId` + `DealTicket` exist as loose fields |
| `mt5_deals` UNIQUE `(broker_id, deal_ticket)` | **INTENDED / BROKEN** | `Mt5DealsConfiguration` declares `HasIndex("broker_id", "deal_ticket").IsUnique()` but targets **missing type** `Mt5Deals`; Domain type is `Mt5Deal`. No EF migration. |
| Idempotent upsert (`ON CONFLICT DO NOTHING`) | **MISSING** | `ITradingStore.UpsertDealAsync` is a bool port with **no implementation** |
| `DealDeduplicator` pure function | **MISSING** | named in A30; no type under `src/Application/Ingestion` |
| `ingestion_events` table / entity / writer | **MISSING** | listed §11 / A20 #12; not in Domain; not in `TraderDbContext` |
| `mt5_deals_total` / `mt5_duplicate_deals_total` | **MISSING** | A50 names frozen; no `Meter` / Prometheus (A50 §0) |
| Payload-hash first-write-wins | **MISSING** | C++ ledger has SHA-256 + `ON CONFLICT DO NOTHING`; C# has neither |
| Durable vs in-memory dedup | **UNSAFE if relied on** | `cacheRecentDeal` last-32 tickets, **ticket only**, no `broker_id` (`mt5_manager.cpp`) |
| §60 unit `Mt5DealDeduplicationTests` | **MISSING** | A09 / A27 inventory only |
| Integration backfill/restart | **MISSING** | A10 `Mt5BackfillRestartTests` |
| Worker using the writer | **MISSING** | `apps/mt5-worker/Worker.cs` is a 1 s log loop |

Do **not** treat C++ `mt5_deals_ledger` / `mt5_raw_events` as the product tables. Different names, `server_key` not `broker_id`, opt-in `MT5SDK_WITH_POSTGRES`, no in-repo migration (A03, A17). Copy the **law** (immutable, `DO NOTHING`, new event for a correction). Do not wire the C++ store as Phase 1 SoT.

---

## 1. Binding law (quoted)

### 1.1 §10 Multi-broker identity

Never assume login or ticket IDs are globally unique.

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

All source-side tables must carry `broker_id`.

Achiever deal ticket `1001` and StarwaveFX deal ticket `1001` are **two rows**. A unique index on `deal_ticket` alone is a **§10 violation**.

### 1.2 §11 Raw layer

```text
The raw layer should be as immutable as practical.
Corrections should be auditable.
```

`ingestion_events` is in the §11 table list. It is **not** optional for Phase 1 audit. A20 keeps it even though §45’s “full initial set” omitted the name.

### 1.3 §12 Ingestion pattern

Historical backfill (per broker/account):

```text
Read checkpoint → Fetch history → Normalize → Upsert idempotently → Persist checkpoint
```

Live:

```text
MT5 event → validate → deduplicate → persist raw record → write transactional outbox event → commit
```

**Phase 1 pin (A41 §14.1, this file wins over a naive reading of §12):** the ingest transaction writes **raw rows + `ingestion_events` only**. It does **not** enqueue `score-update` / `shadow-copy-intent` / `risk-check-request`. Reconstruction (later TX) writes `outbox_events`. Callbacks stay light (§72.6). Persist before async processing (§72.7).

### 1.4 §58 Metrics (names frozen by A50)

```text
mt5_deals_total
mt5_duplicate_deals_total
```

Do not rename to dotted OTel (`mt5.deals`). Do not prefix `ti_`. Do not copy C++ `propfirm_*` / `terminal_*`.

### 1.5 Phase 1 acceptance (§67 / A28)

```text
idempotency proven
Duplicate deal / event insert does not create duplicate rows
```

---

## 2. What “unique upsert” means (and does not)

| Term | Meaning here |
|---|---|
| **Identity** | Natural key `(broker_id, deal_ticket)`. Surrogate `id uuid` is for FKs/logs only (A20). |
| **Upsert** | `INSERT … ON CONFLICT (broker_id, deal_ticket) DO NOTHING`. First durable payload **wins**. |
| **Idempotent** | Replaying the same Manager page, overlapping a checkpoint, or seeing the same live event twice yields **one** `mt5_deals` row. |
| **Duplicate** | A delivery whose `(broker_id, deal_ticket)` already exists. Counted. Not an error. |
| **Conflict** | Duplicate key **and** a different canonical payload hash. Still **no overwrite**. Audited. |
| **Not an upsert** | `ON CONFLICT DO UPDATE` of price/profit/volume. That silently rewrites history. **Forbidden** on `mt5_deals`. |

`ITradingStore.UpsertDealAsync` returning `bool` is **lossy**. Implementation must return a four-way outcome (see §5). The current bool may stay as a thin adapter (`true` = `Inserted` only) until the port is replaced by `IRawMt5RecordWriter`.

---

## 3. Identity types (Domain)

Add before any writer (A01, A30). Equality is **both parts**.

```text
src/Domain/Identifiers/BrokerId.cs          # Guid wrapper; not a server name
src/Domain/Identifiers/BrokerDealTicket.cs  # (BrokerId, long DealTicket)
```

Rules:

| Rule | Detail |
|---|---|
| `BrokerId` | `brokers.id` UUID. Never `MT5_SERVER_NAME`, never host:port, never manager login. |
| `DealTicket` | MT5 deal ticket as `bigint` / `long` (A20: lab values fit signed 64-bit). |
| Ticket `0` | **Invalid.** Reject at validate. Do not persist. Do not synthesize a high-bit ticket (that is `CacheExecutedDeal` for a **write** venue; this product is read-only). |
| Negative ticket | **Invalid.** Reject. |
| Cross-broker | `Equals` / `GetHashCode` include `BrokerId`. Same ticket, different broker → not equal. |

`NormalizedDeal.BrokerId` is currently `string` (`D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs`). Reconstruction must consume the **latest persisted row per** `BrokerDealTicket` (A21). Persist-time identity is UUID + ticket, not the string broker code.

---

## 4. `mt5_deals` schema pin (identity + hash only)

A20 #8 + this file. Table name `mt5_deals`. Entity name **`Mt5Deal`** (singular). Configure `IEntityTypeConfiguration<Mt5Deal>`. Do **not** invent a second type `Mt5Deals`.

```sql
CREATE TABLE mt5_deals (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    broker_id       uuid NOT NULL REFERENCES brokers (id),
    deal_ticket     bigint NOT NULL,
    login           bigint NOT NULL,
    order_ticket    bigint NOT NULL,
    position_id     bigint NOT NULL,
    symbol          text NOT NULL,              -- source symbol as-is (§16)
    action          integer NOT NULL,           -- IMTDeal::EnDealAction (A37)
    entry           integer NOT NULL,           -- IMTDeal::EnDealEntry (A37)
    volume_native   bigint NOT NULL,            -- MT5 integer units (A38); not lots
    price           numeric(18,8) NOT NULL,
    profit          numeric(18,8) NOT NULL,
    commission      numeric(18,8) NOT NULL,
    swap            numeric(18,8) NOT NULL,
    deal_time       timestamptz NOT NULL,       -- broker/server time, UTC
    comment         text NULL,
    payload_hash    text NOT NULL,
    ingested_at     timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT mt5_deals_identity_uk UNIQUE (broker_id, deal_ticket),
    CONSTRAINT mt5_deals_ticket_chk CHECK (deal_ticket > 0),
    CONSTRAINT mt5_deals_hash_chk CHECK (payload_hash ~ '^[0-9a-f]{64}$')
);

CREATE INDEX mt5_deals_broker_login_time_ix
    ON mt5_deals (broker_id, login, deal_time);
CREATE INDEX mt5_deals_broker_order_ix
    ON mt5_deals (broker_id, order_ticket);
CREATE INDEX mt5_deals_broker_position_ix
    ON mt5_deals (broker_id, position_id);
CREATE INDEX mt5_deals_broker_symbol_time_ix
    ON mt5_deals (broker_id, symbol, deal_time);
```

**Do not unique:**

| Candidate | Why not |
|---|---|
| `deal_ticket` alone | §10 |
| `(broker_id, order_ticket)` | one order → many deals (A20 §8) |
| `(broker_id, position_id)` | many deals per lifecycle |
| `(broker_id, login, deal_ticket)` | login is correlation; ticket is unique **per broker** already |

Current configuration (`D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5DealsConfiguration.cs`) maps `volume` as `decimal`/`bigint`, `open_time` / `close_time`, `entry_type`, `reason`. **That is the wrong deal shape.** A deal is a point event (`deal_time`). Close time belongs on `reconstructed_trades` / positions, not on `mt5_deals`. Fix the mapping when implementing; do not persist the shadow-property layout.

### 4.1 Canonical payload hash

SHA-256, lowercase hex, 64 chars (same law as `mt5_ledger::Store::isSha256Hex`).

Hash **only** these fields, in this order, UTF-8, pipe-separated, no spaces:

```text
{broker_id:D}|{deal_ticket}|{login}|{order_ticket}|{position_id}|{symbol}|{action:d}|{entry:d}|{volume_native}|{price:G29}|{profit:G29}|{commission:G29}|{swap:G29}|{deal_time:O}|{comment ?? ""}
```

- `{broker_id:D}` = Guid `"D"` format (lowercase hex with hyphens).
- `{deal_time:O}` = ISO-8601 round-trip UTC (`2026-08-18T12:00:00.0000000+00:00`).
- Decimals: invariant culture, no thousands separators.
- **Exclude** `id`, `ingested_at`, `payload_hash`.
- Do **not** JSON-canonicalize with unordered keys; the pipe format is the v1 pin (`payload_schema_version = 1`).

`DeterministicGuid.FromString` (`D:\Prop\src\Mt5\Utils\DeterministicGuid.cs`) may optionally set `id` from `deal:{broker_id:D}:{deal_ticket}`. It is **not** a substitute for `mt5_deals_identity_uk`. Default `gen_random_uuid()` is enough because `DO NOTHING` never inserts a second row.

---

## 5. Persist-time state machine

### 5.1 Validate (before any write)

`Mt5DealValidator` (A30). Fail closed. **Do not** increment `mt5_deals_total` or `mt5_duplicate_deals_total`.

| Check | Reject if |
|---|---|
| `broker_id` | empty / unknown |
| `deal_ticket` | `<= 0` |
| `login` | `<= 0` |
| `deal_time` | default / year < 2000 |
| `symbol` | null (empty allowed only for non-trading actions; still persist if action is balance-like) |
| `volume_native` | overflow / negative |
| `action` / `entry` | outside A37 enums |

Rejected deals write **`ingestion_events` only** (`outcome=rejected`) so the collector can prove it saw the row. They never touch `mt5_deals`.

### 5.2 Dedup decision (pure; no I/O)

```text
src/Application/Ingestion/DealDeduplicator.cs
src/Application/Ingestion/DealUpsertOutcome.cs
```

```csharp
public enum DealUpsertOutcome
{
    Inserted = 0,           // new mt5_deals row
    DuplicateSame = 1,      // key exists, hash equal
    DuplicateConflict = 2,  // key exists, hash differs
    Rejected = 3            // validator failed (no mt5_deals write)
}

public static class DealDeduplicator
{
    public static DealUpsertOutcome Decide(string incomingHash, string? existingHash)
    {
        if (existingHash is null) return DealUpsertOutcome.Inserted;
        if (existingHash == incomingHash) return DealUpsertOutcome.DuplicateSame;
        return DealUpsertOutcome.DuplicateConflict;
    }
}
```

The writer still **must** hit the unique index. The function exists so unit tests do not need Postgres and so the four outcomes stay named.

### 5.3 Writer SQL (same transaction as `ingestion_events`)

Port: `IRawMt5RecordWriter` (A02 O4, A30). Scaffolding `ITradingStore.UpsertDealAsync` must call this; do not grow a second persist path.

```sql
INSERT INTO mt5_deals (
    broker_id, deal_ticket, login, order_ticket, position_id,
    symbol, action, entry, volume_native, price, profit, commission, swap,
    deal_time, comment, payload_hash, ingested_at
) VALUES (
    @broker_id, @deal_ticket, @login, @order_ticket, @position_id,
    @symbol, @action, @entry, @volume_native, @price, @profit, @commission, @swap,
    @deal_time, @comment, @payload_hash, @now
)
ON CONFLICT (broker_id, deal_ticket) DO NOTHING
RETURNING id, payload_hash;
```

| `RETURNING` | Outcome | `mt5_deals` mutation | Metric |
|---|---|---|---|
| one row, our hash | `Inserted` | inserted | `mt5_deals_total{broker}` ++ |
| no row | see below | **none** | `mt5_duplicate_deals_total{broker}` ++ |

On no row:

```sql
SELECT payload_hash
  FROM mt5_deals
 WHERE broker_id = @broker_id AND deal_ticket = @deal_ticket;
```

Then `DealDeduplicator.Decide(incoming, existing)` → `DuplicateSame` or `DuplicateConflict`.

**Never:**

```sql
ON CONFLICT (broker_id, deal_ticket) DO UPDATE SET ...   -- forbidden
```

**Never** catch `23505` and swallow without classifying. Prefer `DO NOTHING … RETURNING` so a unique violation is not an exception on the happy duplicate path.

### 5.4 Concurrent writers

Two workers (backfill overlap + live poll, or two process replicas) may insert the same ticket in the same millisecond.

1. Both compute the same hash.
2. One `INSERT` wins; the other `DO NOTHING`.
3. Both write `ingestion_events` with the **same** `source_event_id` (`deal:{ticket}`) → second `DO NOTHING` (see §6.4).
4. Exactly one `mt5_deals` row. Duplicate counter increments **once per losing attempt** (each process that observed `DO NOTHING`).

In-memory rings (`cacheRecentDeal`, last 32) are **not** a correctness layer. They may be used only as a connector-side hint. Durable identity is Postgres.

Redis is **not** a deal key authority (A03).

### 5.5 Corrections (broker changed a deal)

MT5 can theoretically update a deal (`OnDealUpdate`). Product law:

1. Do **not** overwrite `mt5_deals`.
2. Classify `DuplicateConflict`.
3. Persist an `ingestion_events` row with a **new** `source_event_id` (see §6.3) so the new payload is durable.
4. Increment `mt5_duplicate_deals_total` (it is still an idempotent hit on the identity key).
5. Increment additive `mt5_deal_payload_conflicts_total` (this file, not §58). Alert if the rate is non-zero in prod.
6. Reconstruction continues to read **the first** `mt5_deals` row. A later revision table is a **new catalog entry**, not v1.

This matches C++ ledger commentary (“never updates historical broker evidence; corrections require a new revision and a new source event”) without adopting `mt5_deals_ledger`.

### 5.6 Streams that must share the writer

| Stream | Why overlap happens |
|---|---|
| `backfill` | checkpoint safety window re-fetches the last N minutes (A07) |
| `live` | deal-lag poll because there is **no** `PUMP_MODE_DEALS` (A04, A07, A14) |
| `reconcile` | periodic `GetDeals(last − overlap, server_now)` |

One `IRawMt5RecordWriter`. Three callers. Same SQL. Same metrics.

Advance `sync_checkpoints` **only** after the batch commit succeeds. Incomplete `GetDeals` pages → do not advance (A07). Overlap is safe **because** this upsert is idempotent.

### 5.7 What backfill must not enqueue

A41: no shadow / risk / score outbox from historical catch-up. Duplicate suppression here is what makes overlap cheap; it is not a license to emit stale copy intents (§63).

---

## 6. `ingestion_events`

### 6.1 Role

Immutable collector evidence for **every ingest attempt that passed the socket**. Complements `mt5_deals`; does **not** replace it. Distinct from:

| Table | Role | May this table substitute? |
|---|---|---|
| `outbox_events` | delivery queue (A41) | **No** |
| `audit_logs` | human / RBAC (A51) | **No** |
| `system_events` | dashboard ops facts | **No** |
| `mt5_raw_events` (C++) | other tree, `server_key` | **No** |

Backfill writes `ingestion_events` even when **no** outbox row exists (A41 §6.5). That is how Phase 1 proves “we saw this ticket” without starting scoring.

### 6.2 DDL (pin for migration `202608180007` / A30)

```sql
CREATE TABLE ingestion_events (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    broker_id               uuid NOT NULL REFERENCES brokers (id),
    source_event_id         text NOT NULL,
    login                   bigint NULL,
    entity_type             text NOT NULL,
    entity_ticket           bigint NULL,
    event_kind              text NOT NULL,
    stream                  text NOT NULL,
    outcome                 text NOT NULL,
    payload                 jsonb NOT NULL,
    payload_hash            text NOT NULL,
    payload_schema_version  integer NOT NULL DEFAULT 1,
    ingestion_run_id        uuid NULL,
    correlation_id          uuid NULL,
    occurred_at             timestamptz NULL,
    ingested_at             timestamptz NOT NULL DEFAULT now(),
    duration_ms             integer NULL,
    counts                  jsonb NULL,
    error_class             text NULL,
    CONSTRAINT ingestion_events_source_uk UNIQUE (broker_id, source_event_id),
    CONSTRAINT ingestion_events_hash_chk CHECK (payload_hash ~ '^[0-9a-f]{64}$'),
    CONSTRAINT ingestion_events_entity_chk CHECK (entity_type IN
        ('deal','order','position','account','group','run')),
    CONSTRAINT ingestion_events_kind_chk CHECK (event_kind IN
        ('add','update','delete','clean','sync','snapshot','started','completed','failed')),
    CONSTRAINT ingestion_events_stream_chk CHECK (stream IN
        ('backfill','live','reconcile','discovery')),
    CONSTRAINT ingestion_events_outcome_chk CHECK (outcome IN
        ('inserted','duplicate','payload_conflict','rejected','ok','failed'))
);

CREATE INDEX ingestion_events_broker_login_ix
    ON ingestion_events (broker_id, login, ingested_at DESC);
CREATE INDEX ingestion_events_entity_ix
    ON ingestion_events (broker_id, entity_type, entity_ticket);
CREATE INDEX ingestion_events_run_ix
    ON ingestion_events (ingestion_run_id);
CREATE INDEX ingestion_events_stream_ix
    ON ingestion_events (broker_id, stream, ingested_at DESC);
```

A20’s optional UNIQUE `(broker_id, payload_hash)` is **rejected for v1**. The same payload can legally appear as a per-deal first-write **and** inside a run-level `counts` document; a global hash unique would block legitimate run rows. Per-deal identity is `source_event_id`, not the hash.

### 6.3 `source_event_id` construction

ASCII, lowercase, colon-separated, **unique per broker**. Never a wall-clock. Never `correlation_id`. Never a Manager-global event id reused across Achiever and StarwaveFX (A20).

| Case | `source_event_id` | `entity_type` | `event_kind` | `outcome` |
|---|---|---|---|---|
| First persist of a deal | `deal:{deal_ticket}` | `deal` | `add` | `inserted` |
| Replay, same hash | `deal:{deal_ticket}` | `deal` | `add` | *(no second row — `DO NOTHING`)* |
| Same ticket, different hash | `deal:{deal_ticket}:conflict:{payload_hash}` | `deal` | `update` | `payload_conflict` |
| Validation reject | `deal:{deal_ticket}:rejected:{payload_hash}` | `deal` | `add` | `rejected` |
| Run started | `run:{stream}:{ingestion_run_id:D}:started` | `run` | `started` | `ok` |
| Run completed | `run:{stream}:{ingestion_run_id:D}:completed` | `run` | `completed` | `ok` |
| Run failed | `run:{stream}:{ingestion_run_id:D}:failed` | `run` | `failed` | `failed` |

`{deal_ticket}` is the decimal ticket. `{ingestion_run_id:D}` is the Guid `"D"` format.

Orders / positions / accounts / groups (same writer family, not expanded here):

```text
order:{order_ticket}:{state}
position:{position_id}:snapshot
account:{login}:{revision_bucket}
group:{group_name}
```

`revision_bucket` for accounts is a coarse time or snapshot id — not this file’s pin. Deal ids above **are** the pin.

### 6.4 Insert SQL

```sql
INSERT INTO ingestion_events (
    broker_id, source_event_id, login,
    entity_type, entity_ticket, event_kind, stream, outcome,
    payload, payload_hash, payload_schema_version,
    ingestion_run_id, correlation_id, occurred_at, ingested_at,
    duration_ms, counts, error_class
) VALUES (
    @broker_id, @source_event_id, @login,
    @entity_type, @entity_ticket, @event_kind, @stream, @outcome,
    @payload::jsonb, @payload_hash, 1,
    @run_id, @correlation_id, @occurred_at, @now,
    @duration_ms, @counts::jsonb, @error_class
)
ON CONFLICT (broker_id, source_event_id) DO NOTHING;
```

First payload wins. Do **not** `DO UPDATE`. A duplicate live+backfill delivery of `deal:123` leaves the original `outcome=inserted` row. Run-level `counts.duplicates` is how later deliveries are counted in the ledger.

### 6.5 Per-deal payload (jsonb)

Keep it small. No secrets. Source symbol, tickets, numeric fields, hash. Example:

```json
{
  "deal_ticket": 881122,
  "login": 6100421,
  "order_ticket": 77001,
  "position_id": 55001,
  "symbol": "XAUUSD.",
  "action": 0,
  "entry": 0,
  "volume_native": 100,
  "price": 2345.12,
  "profit": 0,
  "commission": -0.35,
  "swap": 0,
  "deal_time": "2026-08-18T12:00:01.0000000+00:00",
  "comment": null
}
```

`payload_hash` on the row is the §4.1 hash of this canonical field set (not a hash of pretty-printed JSON).

### 6.6 Run-level `counts`

Written on `completed` / `failed` only:

```json
{
  "fetched": 180,
  "inserted": 12,
  "duplicates": 167,
  "payload_conflicts": 0,
  "rejected": 1
}
```

`fetched = inserted + duplicates + payload_conflicts + rejected` for deals in that run. Incomplete fetch (`GetDeals == false`) → `event_kind=failed`, `error_class=dependency_unavailable`, **do not** persist a checkpoint (A07).

### 6.7 Transaction boundaries

**Live / backfill deal batch (A41 §14.1):**

```text
BEGIN
  FOR each validated deal
      INSERT mt5_deals ON CONFLICT DO NOTHING
      INSERT ingestion_events ON CONFLICT DO NOTHING   -- deal:{ticket} or conflict/rejected id
  INSERT ingestion_events run:…:completed              -- or failed
  -- optional: UPDATE sync_checkpoints (same TX as last successful page)
COMMIT
```

Then increment process counters from the in-memory tallies of that TX (so a rolled-back TX does not move Prometheus).

**Do not** write `outbox_events` in this TX in Phase 1.

**Do not** call scoring, reconstruction, shadow, risk, or FIX from this TX.

### 6.8 Domain / Infrastructure types (when implementing)

```text
src/Domain/Ingestion/IngestionEvent.cs
src/Infrastructure/Persistence/Configurations/IngestionEventConfiguration.cs
```

`TraderDbContext` today has no `DbSet` for this type. Add it with migration `0007` (A30). Do not overload `OutboxEvent`.

---

## 7. Duplicate metrics

### 7.1 Frozen §58 / A50 instruments

| Instrument | Type | Unit | Attributes | When to increment |
|---|---|---|---|---|
| `mt5_deals_total` | Counter | `{deal}` | `broker` | once per **committed** `Inserted` row |
| `mt5_duplicate_deals_total` | Counter | `{deal}` | `broker` | once per attempt that resolved `DuplicateSame` **or** `DuplicateConflict` after a committed TX |
| `mt5_events_total` | Counter | `{event}` | `broker`, `event_kind` | every accepted collector event; `event_kind=deal` for this path |

`broker` values: `achiever` \| `starwavefx` (A50). Never manager login. Never `deal_ticket` as a label (A50 §7.2 **forbidden**).

A50 description pin:

- `mt5_deals_total` — “persisted deals (after validate)”
- `mt5_duplicate_deals_total` — “idempotent hits (same broker+ticket)”

Both `DuplicateSame` and `DuplicateConflict` are idempotent hits on the identity key. Validation failures are **not**.

### 7.2 Additive instruments (this file; not §58)

Low cardinality. Optional for v1 dashboards; **required** before calling Phase 1 “idempotency proven” in ops (so a correction is not invisible inside the duplicate counter).

| Instrument | Type | Attributes | When |
|---|---|---|---|
| `mt5_deal_payload_conflicts_total` | Counter | `broker` | `DuplicateConflict` committed |
| `mt5_deal_validation_failures_total` | Counter | `broker`, `reason` | `Rejected`; `reason` closed set: `ticket` \| `login` \| `time` \| `volume` \| `enum` \| `other` |

Do **not** add `stream` or `login` labels to `mt5_duplicate_deals_total` without amending A50 §7.1. Stream breakdown lives in `ingestion_events.counts` / SQL:

```sql
SELECT stream,
       SUM((counts->>'inserted')::bigint)   AS inserted,
       SUM((counts->>'duplicates')::bigint) AS duplicates,
       SUM((counts->>'payload_conflicts')::bigint) AS conflicts
  FROM ingestion_events
 WHERE entity_type = 'run'
   AND event_kind = 'completed'
   AND broker_id = @broker
   AND ingested_at >= now() - interval '24 hours'
 GROUP BY stream;
```

### 7.3 What must never increment `mt5_duplicate_deals_total`

| Event | Why |
|---|---|
| Reconstruction sees a ticket already applied (A21) | Different layer; no second persist |
| Two brokers share a numeric ticket | **Not a duplicate** — two inserts, two `mt5_deals_total` |
| Unique-index reject on a **programmer error** (null broker) | Validation / exception; not a deal duplicate |
| Outbox `ON CONFLICT` (A41) | `outbox_duplicate_enqueue_total` |
| FIX duplicate `ExecID` (A34) | FIX metrics, not MT5 |
| `cacheRecentDeal` in-process hit | Not durable; do not export as §58 |

### 7.4 In-process tally → export

`DealIngestionService.SyncBrokerAsync` today returns `insertedDeals` only (`true` from `UpsertDealAsync`). Required return / metric surface:

```csharp
public sealed record DealIngestBatchResult(
    int Fetched,
    int Inserted,
    int Duplicates,
    int PayloadConflicts,
    int Rejected);
```

After commit:

```text
mt5_deals_total                     += Inserted
mt5_duplicate_deals_total           += Duplicates + PayloadConflicts
mt5_deal_payload_conflicts_total    += PayloadConflicts
mt5_deal_validation_failures_total  += Rejected
mt5_events_total{event_kind=deal}   += Fetched   # or only accepted; pin: Fetched after validate+reject counted separately
```

Pin: `mt5_events_total{event_kind=deal}` increments once per **validated or rejected** deal object the collector handled (includes rejects). Raw socket frames that fail parse increment `event_kind=other` or are dropped with a log — they are not deals.

### 7.5 Logs (A50 / §57)

Structured, snake_case. On every deal persist attempt that is not a hot-path Debug flood:

```text
broker_id
source_login
deal_ticket          # allowed on LOGS, forbidden on metric labels
outcome              # inserted | duplicate | payload_conflict | rejected
stream               # backfill | live | reconcile
ingestion_run_id
correlation_id
payload_hash
```

Never log Manager passwords. Never dump full `payload` at Information if comments later grow PII; Debug only.

### 7.6 Dashboard / health

A26 health JSON uses the same tokens as camelCase. Project:

```text
mt5DealsTotal
mt5DuplicateDealsTotal
mt5DealPayloadConflictsTotal
```

A rising duplicate counter during backfill overlap is **healthy**. A rising **conflict** counter is **not**. Phase 1 “idempotency proven” means:

1. Replay of a recorded `GetDeals` page does not increase `mt5_deals` row count.
2. `mt5_duplicate_deals_total` increases by the replay size.
3. Unique index rejects a second insert in a raw SQL test.
4. Achiever ticket `T` and StarwaveFX ticket `T` produce **two** rows.

---

## 8. Application / Infrastructure contracts

```text
src/Application/Ingestion/DealDeduplicator.cs
src/Application/Ingestion/DealUpsertOutcome.cs
src/Application/Ingestion/DealIngestBatchResult.cs
src/Application/Ingestion/Validators/Mt5DealValidator.cs
src/Application/Abstractions/IRawMt5RecordWriter.cs
src/Application/Observability/ITradingMetrics.cs          # A50

src/Infrastructure/Persistence/Repositories/RawMt5RecordWriter.cs
src/Infrastructure/Persistence/Configurations/Mt5DealConfiguration.cs   # replace Mt5DealsConfiguration
src/Infrastructure/Persistence/Configurations/IngestionEventConfiguration.cs
src/Infrastructure/Persistence/Sql/202608180006_OrdersDealsPositions.sql
src/Infrastructure/Persistence/Sql/202608180007_CheckpointsOutboxEvents.sql
```

`IRawMt5RecordWriter` (minimum):

```csharp
Task<DealUpsertOutcome> UpsertDealAsync(
    Guid brokerId,
    Mt5DealDto deal,
    string stream,                 // backfill | live | reconcile
    Guid ingestionRunId,
    DateTimeOffset now,
    CancellationToken ct);
```

Implementation owns: hash → insert deal → insert ingestion_event → return outcome. Caller owns: run-level event, checkpoint, metrics after **successful commit**.

`DealIngestionService` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs`) is the right *orchestration* sketch (groups → accounts → deals → positions) but it:

- has no checkpoint
- has no `ingestion_events`
- has no metrics
- treats upsert as bool
- is unused by `apps/mt5-worker`

Do not add scoring into this service (it already calls `ReconstructionScoringService` in the same file — that coupling is a later-phase concern and **must not** run inside the persist TX).

---

## 9. Tests (required to claim “idempotency proven”)

### 9.1 Unit — persist-time (A09 #1, A30; **not** A21)

Path pin: `tests/Unit/Ingestion/Mt5DealDeduplicationTests.cs`  
SUT: `DealDeduplicator` + `Mt5DealValidator` + hash helper.

A27’s `Reconstruction.Mt5DealDeduplicationTests` name is **wrong layer**. Reconstruction skip stays in A21 tests. Do not merge them.

| Fact | Must prove |
|---|---|
| `Same_broker_deal_ticket_same_hash_is_DuplicateSame` | Decide(h, h) → DuplicateSame |
| `Same_broker_deal_ticket_different_hash_is_DuplicateConflict` | Decide(h1, h2) → DuplicateConflict |
| `Missing_existing_hash_is_Inserted` | Decide(h, null) → Inserted |
| `Different_broker_same_ticket_are_not_equal` | `BrokerDealTicket` equality |
| `Ticket_zero_is_rejected` | validator |
| `Hash_excludes_ingested_at_and_id` | mutating ingested_at does not change hash |
| `Hash_is_stable_across_culture` | invariant decimals |

### 9.2 Integration — Postgres (A10 #2 + unique index)

`tests/Integration/Ingestion/Mt5DealIdempotencyTests.cs` (new; complements `Mt5BackfillRestartTests`):

| Fact | Must prove |
|---|---|
| `Unique_index_rejects_second_insert_of_same_broker_ticket` | raw SQL second insert → 0 rows / `DO NOTHING` |
| `Same_ticket_two_brokers_inserts_two_rows` | Achiever + StarwaveFX |
| `Replay_does_not_change_row_count_or_payload` | writer twice; `COUNT(*)` stable; first hash kept |
| `Conflict_does_not_overwrite_mt5_deals` | second payload different; first row unchanged; extra `ingestion_events` with `…:conflict:…` |
| `Ingestion_event_deal_id_is_idempotent` | two inserts of `deal:{t}` → one evidence row |
| `Batch_commit_rolls_back_metrics_source_of_truth` | throw after insert, before commit → 0 rows |

`tests/Integration/Ingestion/Mt5BackfillRestartTests.cs` (A10):

| Fact | Must prove |
|---|---|
| Crash mid-page does not advance checkpoint | |
| Restart + overlap window does not duplicate `mt5_deals` | |
| `GetDeals == false` does not persist checkpoint | |

### 9.3 Metric tests

| Fact | Must prove |
|---|---|
| Insert increments `mt5_deals_total` only | |
| Replay increments `mt5_duplicate_deals_total` only | |
| Conflict increments duplicate **and** `mt5_deal_payload_conflicts_total` | |
| Helper rejects `deal_ticket` / `source_login` as metric tags | A50 §7.2 |

No live Manager. Use `FakeMt5HistorySource` (A10).

---

## 10. Measured current tree (do not greenwash)

| Path | What it actually does |
|---|---|
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | Fields include `BrokerId`, `DealTicket`, `IngestedAt`. No hash. No uniqueness invariant. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Counts `true` from `UpsertDealAsync`. No store implementation. No events. No metrics. |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | `Mt5DealDto` has tickets + economics. Good input shape. |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\Mt5DealsConfiguration.cs` | Unique index **intent** on `(broker_id, deal_ticket)`. Wrong CLR type `Mt5Deals`. Wrong columns (`open_time`/`close_time`). |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `DbSet<Mt5Deals>` — type does not exist beside `Mt5Deal`. Compiles only if another partial exists (none found). Treat as **broken scaffold**. |
| `D:\Prop\src\Mt5\Utils\DeterministicGuid.cs` | Hash-to-GUID helper. Not wired to deals. |
| `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | Live event carries `DealTicket` + `BrokerId`. Fine. |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Template loop. Does not ingest. |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` `cacheRecentDeal` | Last 32, ticket-only, process RAM. **Not** Phase 1 proof. |
| `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp` | `ON CONFLICT (server_key, source_event_id)` / `(server_key, deal_ticket, revision_no) DO NOTHING`. Law-compatible, **schema-incompatible**. |
| `D:\Prop\mt5-sdk\src\services\metrics_service.h` | No `mt5_duplicate_deals_total`. Deprecated for this product (A50). |

---

## 11. Forbidden list

1. Unique index on `deal_ticket` without `broker_id`.
2. `ON CONFLICT DO UPDATE` of economic fields on `mt5_deals`.
3. Treating `cacheRecentDeal` / Redis / a `HashSet<(long ticket)>` as SoT.
4. Synthesizing deal tickets (`CacheExecutedDeal` high-bit trick) on this read-only collector.
5. Overloading `outbox_events` or `audit_logs` as ingest evidence.
6. Incrementing `mt5_duplicate_deals_total` for cross-broker same-ticket inserts.
7. Putting `deal_ticket` or `source_login` on a Prometheus label.
8. Advancing `sync_checkpoints` on a partial page because “dedup will fix gaps.” Dedup does **not** fill skipped tickets.
9. Calling reconstruction / scoring / FIX from the deal insert TX.
10. Porting `mt5_deals_ledger` as a second product deal table beside `mt5_deals`.
11. Claiming “idempotency proven” without the §9 tests on a real Postgres unique index.

---

## 12. Implementation sequence (pointer only)

Already ordered in A30 Increment 1. This file fills the deal-shaped hole:

1. Domain `BrokerDealTicket` + hash helper + `DealDeduplicator` + validator (unit tests first).
2. Migration `0006` unique `mt5_deals_identity_uk` + `payload_hash`; migration `0007` `ingestion_events`.
3. `RawMt5RecordWriter` with the SQL in §5.3 / §6.4.
4. Wire `mt5_deals_total` / `mt5_duplicate_deals_total` (A50 `MetricNames`).
5. Backfill + live + reconcile all call the same writer.
6. Integration tests §9.2. **Then** tick A28 “idempotency proven.”

No product source was modified to produce this document.
