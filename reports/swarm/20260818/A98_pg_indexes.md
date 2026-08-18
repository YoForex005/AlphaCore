# A98 — PostgreSQL indexes: 5,000 accounts, deals `(broker, login, time)`, reconstructed trades, outbox pending

| Field | Value |
|---|---|
| Agent | A98 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A98_pg_indexes.md` |
| Product source edited | **No** |
| Engine | PostgreSQL 16 (A65). EF Core 8.0.4 + Npgsql 8.0.4 (packages present). |
| Law | Architecture v2 §§3, 10–15, 45, 50, 58; A20 catalog; A21 reconstruction; A30 migrations; A41 outbox; A61 EF names |
| Scope | Access-path indexes for the four hot sets. Design only. No migration applied. |

This file is the **index contract** for the first useful census (~5,000 source logins). It does not create tables, does not `EnsureCreated`, and does not edit `TraderDbContext`. A later increment emits a **new versioned** EF migration that matches these names.

---

## 0. Verdict (measured)

| Question | Answer |
|---|---|
| Are these four index families on a live Postgres? | **No.** `src/Infrastructure/Persistence/Migrations/` is empty. Hosts fall back to **EF InMemory** when `ConnectionStrings:TraderIntelligence` is missing or contains `<SECRET>` (`DependencyInjection.cs`). InMemory cannot prove btree, partial, or `SKIP LOCKED`. |
| Does fluent API already sketch some of them? | **Partial.** `TraderDbContext` has the account identity UK, the deal identity UK, and `(BrokerId, Login, DealTime)`. Outbox has the **wrong** drain index. Reconstructed trades has a **non-unique** 4-column sketch, not the A21 identity. |
| Is Kafka required at 5,000 accounts? | **No.** Architecture §13: Postgres outbox is the bus. Indexes must keep the pending working set tiny. |
| Can v1 ship without partitioning? | **Yes** at the planning envelope in §3. Partition `mt5_deals` / `reconstructed_trades` only after a measured table size or seq-scan on the login-time index. |
| Classification of current product indexes | **EXISTS_NEEDS_REFACTOR** for deals login-time and account identity. **MISSING** for reconstructed access + unique lifecycle. **UNSAFE-shape** for outbox pending (full `ProcessedAt` btree). |

Honest status: **FAIL as a 5,000-account data plane.** The two deal indexes are the only sketches that match the architecture. Nothing is applied. Dashboard `/api/trades` filters `login` without `broker_id` (violates §10 and cannot use the compound indexes).

---

## 1. Binding laws

| # | Law | Source |
|---|---|---|
| L1 | Never unique `login`, `deal_ticket`, `order_ticket`, or `position_id` alone. | §10 |
| L2 | All source tables carry `broker_id NOT NULL`. | §10, A20 |
| L3 | Deal identity is `(broker_id, deal_ticket)`. One order / one position → many deals. Do **not** unique those pairs on `mt5_deals`. | A20 §5.2, A61 §4.3 |
| L4 | Reconstruction input is **all deals for one `(broker_id, login)` ordered by time then ticket**. | `EfTradingStore.LoadDealsAsync`, A21 |
| L5 | First-3 counting uses **completed** reconstructed rows with `canonical_symbol = 'XAUUSD'` only. | §15, A21 |
| L6 | Outbox drain is `FOR UPDATE SKIP LOCKED` on the **pending** subset. Processed history must not sit on the claim index. | A41 §8, A61 §5.4 |
| L7 | At ~5,000 users do not introduce Kafka, extra schemas, or ClickHouse “because scale.” | §13, A30 |
| L8 | Redis is not an index authority for accounts, deals, trades, or outbox. | §5, A03 |
| L9 | Snake_case names, A20 `*_uk` / A61 `{table}_{cols}_ix`. Explicit `HasDatabaseName`. | A61 §2.2 |
| L10 | C++ `mt5_ledger` uniques on `(server_key, …)` are a **sibling** tree. Do not copy them as the v2 collector indexes. | A59 |

---

## 2. Current fluent map (read-only)

Source: `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`. `Configurations/` is empty. No `UseSnakeCaseNamingConvention()`. If `EnsureCreated` ever hit Postgres, columns would be quoted PascalCase (`"BrokerId"`), not `broker_id`. Treat the fluent indexes as **intent**, not applied DDL.

| Table | Fluent index | Unique | vs this contract |
|---|---|---|---|
| `mt5_accounts` | `(BrokerId, Login)` | YES | **Keep** as `mt5_accounts_identity_uk`. Missing group + stale indexes. |
| `mt5_deals` | `(BrokerId, DealTicket)` | YES | **Keep** as `mt5_deals_identity_uk`. |
| `mt5_deals` | `(BrokerId, Login, DealTime)` | no | **Keep and widen** to include `deal_ticket` (see §5). |
| `mt5_positions_current` | `(BrokerId, PositionTicket)` | YES | Identity OK. **Missing** `(broker_id, login)` used by `ReplacePositionsAsync`. |
| `reconstructed_trades` | `(BrokerId, Login, PositionId, OpenedAt)` | **no** | Wrong unique. `OpenedAt` is not `lifecycle_seq`. Missing explorer / first-3 indexes. |
| `outbox_events` | `(ProcessedAt)` | no | **Wrong.** Indexes the whole history. Pending is `processed_at IS NULL`. |
| `trader_scores` | `(BrokerId, Login)` | YES | Needed for 5k leaderboard join. Out of the four families; keep. |
| `sync_checkpoints` | `(BrokerId, Login, Stream)` | YES | 5k × streams. Keep; A59 may widen with `scope_type` later. |
| `trader_score_history` | `(BrokerId, Login, RecordedAt)` | no | Scoring history; not this file’s hot path. |

`OutboxEvent` today (`Domain/Entities/OutboxEvent.cs`) has `Type`, `AggregateId` (string), `PayloadJson`, `OccurredAt`, `ProcessedAt`, `Attempts`, `LastError`, `CorrelationId`. It does **not** have `status`, `next_attempt_at`, `available_at`, `idempotency_key`, or `dedupe_key`. Drain indexes must be specified for **both** the thin entity and the A41/A61 target (see §7).

---

## 3. Planning envelope at ~5,000 accounts

Architecture §3: “5,000+ MT5 trader accounts.” Two source brokers (Achiever + StarwaveFX). These numbers are **planning bounds**, not measured production counts. Do not cite them as “we have N deals.”

| Set | Rows (planning) | Why btree is enough |
|---|---|---|
| `mt5_accounts` | ~5,000–8,000 | Fits in a few MB. Indexes exist for **identity + filters**, not size. |
| `sync_checkpoints` | ~5,000 × 3–5 streams ≈ 15k–40k | Tiny. Unique `(broker_id, login, stream)` is the point. |
| `mt5_positions_current` | open book only; hundreds–low thousands | Delete-on-close. `(broker_id, login)` for replace. |
| `mt5_deals` | conservative 0.4M; upper 2.5M (5k × 500) | Btree on `(broker_id, login, deal_time, deal_ticket)` stays < 200 MB at 2.5M. No RANGE partition in v1. |
| `reconstructed_trades` | ~0.1M–0.8M | One lifecycle per several deals. Explorer is time-range on completed XAUUSD. |
| `outbox_events` **pending** | tens–low thousands if drain works | **Must stay a working set.** History may grow; claim index is **partial**. |
| `outbox_events` processed | unbounded over time | Leave the claim index. Optional later partition / archive. Not v1. |

BRIN on `deal_time` alone is **not** a substitute for the login-time btree. Reconstruction and backfill are **per account**, not “all deals last hour.”

`pg_trgm` on `login::text` for dashboard `q=` contains is **not** required at 5k. Sequential filter of one broker’s accounts is cheaper than another GIN. Add only after a measured p95 on `GET /api/v1/mt5/accounts?q=`.

---

## 4. Family A — `mt5_accounts` (the 5,000-login census)

### 4.1 Access paths that must hit an index

| Path | Code / spec | Predicate / order |
|---|---|---|
| Upsert one login | `EfTradingStore.UpsertAccountAsync` | `broker_id = $1 AND login = $2` |
| Compound FK parent | A20 / A61 | UNIQUE `(broker_id, login)` |
| Group census | `EfDashboardQueries.GetGroupsAsync`; A63 `GET /mt5/groups` | `broker_id = $1 AND group_name = $2` → `COUNT(*)` |
| Paged account list | A63 `GET /mt5/accounts?brokerId&groupId` | `(broker_id, group_name)` + keyset on `login` |
| Stale / last-sync | A07 health, backfill scheduler | `broker_id = $1 ORDER BY last_synced_at` (or `last_event_at` once that column exists) |
| Overview total | `CountAsync()` on accounts | Seq scan of 5k is fine; no extra index |

### 4.2 Required indexes

| Name | Definition | Unique |
|---|---|---|
| `mt5_accounts_identity_uk` | `(broker_id, login)` | **YES** — §10 |
| `mt5_accounts_group_ix` | `(broker_id, group_name)` | no |
| `mt5_accounts_last_sync_ix` | `(broker_id, last_synced_at DESC NULLS LAST)` | no |

A20/A61 also name `mt5_accounts_last_event_ix (broker_id, last_event_at DESC)`. Domain `Mt5Account` today has `LastSyncedAt` / `LastAccessAt`, **not** `last_event_at`. Index the column that exists (`last_synced_at`). When `last_event_at` is added, add the A61 index in the **same** versioned migration as the column. Do not index a phantom column.

Optional covering (only if `EXPLAIN` on the paged list shows heap fetches as the cost):

```text
mt5_accounts_group_ix  (broker_id, group_name, login)
  INCLUDE (balance, equity, leverage, last_synced_at)
```

Do **not** start with INCLUDE. 5k heaps are cheap.

### 4.3 Current product gaps

- Identity UK sketched. Group + last-sync **missing**.
- `GetGroupsAsync` runs `CountAsync` **per group** (N+1). The group index makes each count an index-only count; the N+1 is still an application bug (one `GROUP BY` is the fix). This file does not change that code.
- `GetTradersAsync` loads **all** accounts into memory. At 5k that is acceptable; do not add indexes to paper over the full table read.

### 4.4 Target DDL (not applied)

```sql
-- 0004 / accounts increment (A30). Names pinned.

CREATE UNIQUE INDEX mt5_accounts_identity_uk
    ON mt5_accounts (broker_id, login);

CREATE INDEX mt5_accounts_group_ix
    ON mt5_accounts (broker_id, group_name);

CREATE INDEX mt5_accounts_last_sync_ix
    ON mt5_accounts (broker_id, last_synced_at DESC NULLS LAST);
```

EF:

```csharp
e.HasIndex(x => new { x.BrokerId, x.Login })
    .IsUnique()
    .HasDatabaseName("mt5_accounts_identity_uk");
e.HasIndex(x => new { x.BrokerId, x.GroupName })
    .HasDatabaseName("mt5_accounts_group_ix");
e.HasIndex(x => new { x.BrokerId, x.LastSyncedAt })
    .IsDescending(false, true)
    .HasDatabaseName("mt5_accounts_last_sync_ix");
```

Fillfactor **80** on `mt5_accounts` (equity/balance upserts). Not on the unique index itself unless a later vacuum report says so.

---

## 5. Family B — `mt5_deals` by `(broker_id, login, time)`

This is the reconstruction and backfill spine. If this index is wrong, every `RebuildTraderAsync` and every per-login `DealRequest` window becomes a 2.5M-row filter.

### 5.1 Access paths

| Path | Predicate / order | Index used |
|---|---|---|
| Dedup insert | `broker_id, deal_ticket` | `mt5_deals_identity_uk` |
| `LoadDealsAsync` | `WHERE broker_id = $1 AND login = $2 ORDER BY deal_time, deal_ticket` | **login-time (widened)** |
| Backfill / reconcile window | `WHERE broker_id = $1 AND login = $2 AND (deal_time, deal_ticket) > ($from, $last_ticket) AND deal_time < $to` | same |
| Rebuild one position | `WHERE broker_id = $1 AND position_id = $2` | `mt5_deals_position_ix` |
| Order → deals | `WHERE broker_id = $1 AND order_ticket = $2` | `mt5_deals_order_ix` (non-unique) |
| Symbol window (ops / later MFE join) | `WHERE broker_id = $1 AND source_symbol = $2 AND deal_time >= $3` | `mt5_deals_symbol_time_ix` |
| `mt5_duplicate_deals_total` | unique-violation on identity UK | identity UK |

Current `LoadDealsAsync` loads **all** deals for the login (no time predicate) then reconstructs in memory. The 3-column fluent index already matches the `WHERE`. The `ORDER BY deal_time, deal_ticket` is **not** fully covered: PostgreSQL may sort the ticket tie-break. At a few hundred deals/login that sort is noise; at a multi-year prop account it is not. Widen the index.

Checkpoint resume (A59 `TIME_TICKET`) **requires** the 4th column. Do not invent a second `(login, deal_ticket)` index.

### 5.2 Required indexes

| Name | Definition | Unique | Phase |
|---|---|---|---|
| `mt5_deals_identity_uk` | `(broker_id, deal_ticket)` | **YES** | 0006 (A30) |
| `mt5_deals_login_time_ix` | `(broker_id, login, deal_time, deal_ticket)` | no | 0006 — **widen** A20/A61 3-col |
| `mt5_deals_position_ix` | `(broker_id, position_id)` | no | 0006 |
| `mt5_deals_order_ix` | `(broker_id, order_ticket)` | no | 0006 |
| `mt5_deals_symbol_time_ix` | `(broker_id, source_symbol, deal_time)` | no | after first-3 if Trade Explorer ever lists raw deals (it must not). Defer if disk is tight. |

Column name pin: Domain `Symbol` → PostgreSQL `source_symbol` (A61). Fluent today has no `HasColumnName`. The symbol index is specified against the **A61 column**.

`deal_time` is `timestamptz` (UTC). Do not store MT5 server seconds in a separate btree unless ingest adds `time_msc`; if `time_msc` lands, put it **after** `deal_time` or replace `deal_time` in the 4-col index with `(broker_id, login, time_msc, deal_ticket)` and keep `deal_time` as a stored column only. Do not maintain two time indexes on the same table in v1.

### 5.3 Forbidden on `mt5_deals`

| Tempting | Why |
|---|---|
| UNIQUE `(deal_ticket)` | Cross-broker collision (§10) |
| UNIQUE `(broker_id, order_ticket)` | One order → many deals |
| UNIQUE `(broker_id, position_id)` | Many deals per lifecycle |
| UNIQUE `(broker_id, login, deal_time)` | Two deals can share a second |
| BRIN `(deal_time)` as the reconstruction index | Wrong leading column |
| Hash index on `deal_ticket` | No `ORDER BY`; not WAL-safe enough to prefer over btree UK |

### 5.4 Target DDL (not applied)

```sql
CREATE UNIQUE INDEX mt5_deals_identity_uk
    ON mt5_deals (broker_id, deal_ticket);

-- Reconstruction + TIME_TICKET checkpoint. Widens A20/A61 3-col.
CREATE INDEX mt5_deals_login_time_ix
    ON mt5_deals (broker_id, login, deal_time, deal_ticket);

CREATE INDEX mt5_deals_position_ix
    ON mt5_deals (broker_id, position_id);

CREATE INDEX mt5_deals_order_ix
    ON mt5_deals (broker_id, order_ticket);

-- Optional v1.1
CREATE INDEX mt5_deals_symbol_time_ix
    ON mt5_deals (broker_id, source_symbol, deal_time);
```

Append-only: fillfactor **100**. `ON CONFLICT ON CONSTRAINT mt5_deals_identity_uk DO NOTHING` (A78 / A61). The UK **is** the duplicate metric.

Adjacent: `ReplacePositionsAsync` deletes `mt5_positions_current` by `(broker_id, login)`. Add (not one of the four families, but required at 5k or the replace becomes a seq scan as the live book grows):

```sql
CREATE INDEX mt5_positions_current_login_ix
    ON mt5_positions_current (broker_id, login);
```

Identity remains `mt5_positions_current_identity_uk (broker_id, position_id)` — Domain property is `PositionTicket`; column pin is `position_id` (A61).

### 5.5 EXPLAIN contract (Testcontainers, when a coder is assigned)

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT *
FROM mt5_deals
WHERE broker_id = $1
  AND login = $2
ORDER BY deal_time, deal_ticket;
```

**Pass:** `Index Scan` or `Index Only Scan` on `mt5_deals_login_time_ix`. **Fail:** `Seq Scan` on `mt5_deals`. Sort node is fail if the 4-col index exists.

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT 1
FROM mt5_deals
WHERE broker_id = $1 AND deal_ticket = $2;
```

**Pass:** unique index scan on `mt5_deals_identity_uk`.

---

## 6. Family C — `reconstructed_trades`

One row = one position **lifecycle**, not one deal (§14). Dashboard Trade Explorer and first-3 scoring read this table, **not** `mt5_deals` (A63 §5.5).

### 6.1 Conflict pin (do not leave this open)

| Doc | Unique proposed | Problem |
|---|---|---|
| A20 / A61 | `(broker_id, position_id)` | A21 §1.5–1.6: same `position_id` can flatten and reopen (`ENTRY_INOUT`, netting reuse). Second lifecycle would violate the UK. |
| A30 0009 / A21 | `(broker_id, login, position_id, lifecycle_seq)` | Correct identity. `lifecycle_seq` is **not** on Domain `ReconstructedTrade` today. |
| Current EF | `(BrokerId, Login, PositionId, OpenedAt)` non-unique | Weak surrogate. Two lifecycles can share an `opened_at` only if the clock is coarse; still not the A21 key. |

**Pin for indexes (this file):**

1. **UNIQUE** `reconstructed_trades_lifecycle_uk (broker_id, login, position_id, lifecycle_seq)` — A21/A30. Add the column in the same migration as the UK (0009).
2. Do **not** ship A20 `reconstructed_trades_position_uk` on `(broker_id, position_id)` alone. That UK is **wrong** once reversals exist. A20 §8 already flags ticket reuse as an open question; A21 closed it.
3. Until `lifecycle_seq` exists, keep a **non-unique** `(broker_id, login, position_id, opened_at)` only as a temporary lookup. Do not mark it unique.

`login` is in the UK so a compound FK `(broker_id, login)` → `mt5_accounts` stays aligned and a ticket reuse on another login cannot collide.

### 6.2 Access paths

| Path | Predicate / order |
|---|---|
| `ReplaceReconstructedAsync` delete+insert | `WHERE broker_id = $1 AND login = $2` |
| Incremental upsert (target; replace-all is a Phase-2 smell) | `ON CONFLICT (broker_id, login, position_id, lifecycle_seq)` |
| First-3 / scorer | `WHERE broker_id = $1 AND login = $2 AND completed AND canonical_symbol = 'XAUUSD' ORDER BY closed_at` |
| Leaderboard PnL | `WHERE completed GROUP BY broker_id, login` (`EfDashboardQueries.GetTradersAsync`) |
| Trade Explorer (A63) | `completed` (default true), `canonical_symbol` (default `XAUUSD`), optional `broker_id`, `login`, `closed_at BETWEEN from AND to`, `ORDER BY closed_at DESC` |
| Trader detail trades | `broker_id + login` + completed XAUUSD, `ORDER BY closed_at` |
| Open lifecycles (live book vs reconstruction) | `WHERE broker_id = $1 AND login = $2 AND completed = false` |
| PK lookup `source_trade_id` | `id` (already PK) |

`GET /api/trades` today: `Where(t => t.Login == login)` **without** `broker_id`, then `OrderByDescending(OpenedAt).Take(200)`. That query **cannot** use any legal compound index and is a §10 bug. The explorer index below assumes the API is fixed to `(broker_id, login)` or broker-wide `(broker_id, canonical_symbol, closed_at)`.

### 6.3 Required indexes

| Name | Definition | Unique |
|---|---|---|
| `reconstructed_trades_lifecycle_uk` | `(broker_id, login, position_id, lifecycle_seq)` | **YES** |
| `reconstructed_trades_login_ix` | `(broker_id, login)` | no — delete/replace + open scan. **Redundant** as a prefix of the UK **if** the UK exists; skip this standalone index once 0009 lands. |
| `reconstructed_trades_login_closed_ix` | `(broker_id, login, closed_at DESC)` | no |
| `reconstructed_trades_xau_completed_ix` | `(broker_id, login, closed_at DESC) WHERE completed AND canonical_symbol = 'XAUUSD'` | no — first-3 + explorer per trader |
| `reconstructed_trades_explorer_ix` | `(broker_id, closed_at DESC) WHERE completed AND canonical_symbol = 'XAUUSD'` | no — cross-trader explorer |
| `reconstructed_trades_open_ix` | `(broker_id, login) WHERE completed = false` | no — small partial |

Leaderboard `GROUP BY (broker_id, login) WHERE completed`: the UK prefix + filter, or `reconstructed_trades_login_closed_ix`, is enough. Do not add a third `(broker_id, login) WHERE completed`.

A61 `reconstructed_trades_count_ix (broker_id, login, completed, canonical_symbol)` is **superseded** by the two partials above. Partials are smaller and match the only legal first-3 predicate.

### 6.4 Target DDL (not applied)

```sql
ALTER TABLE reconstructed_trades
    ADD COLUMN IF NOT EXISTS lifecycle_seq integer NOT NULL DEFAULT 1;

ALTER TABLE reconstructed_trades
    ADD CONSTRAINT reconstructed_trades_lifecycle_seq_ck
    CHECK (lifecycle_seq >= 1);

CREATE UNIQUE INDEX reconstructed_trades_lifecycle_uk
    ON reconstructed_trades (broker_id, login, position_id, lifecycle_seq);

CREATE INDEX reconstructed_trades_login_closed_ix
    ON reconstructed_trades (broker_id, login, closed_at DESC);

CREATE INDEX reconstructed_trades_xau_completed_ix
    ON reconstructed_trades (broker_id, login, closed_at DESC)
    WHERE completed AND canonical_symbol = 'XAUUSD';

CREATE INDEX reconstructed_trades_explorer_ix
    ON reconstructed_trades (broker_id, closed_at DESC)
    WHERE completed AND canonical_symbol = 'XAUUSD';

CREATE INDEX reconstructed_trades_open_ix
    ON reconstructed_trades (broker_id, login)
    WHERE completed = false;
```

Default `lifecycle_seq = 1` is safe for the current reconstructor (it does not yet emit seq). When A21 reversals land, writers **must** increment. Do not backfill a unique `(broker_id, position_id)` first.

### 6.5 EXPLAIN contract

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT *
FROM reconstructed_trades
WHERE broker_id = $1
  AND login = $2
  AND completed
  AND canonical_symbol = 'XAUUSD'
ORDER BY closed_at;
```

**Pass:** `Index Scan` on `reconstructed_trades_xau_completed_ix`.

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT broker_id, login, SUM(net_realized_pnl)
FROM reconstructed_trades
WHERE completed
GROUP BY broker_id, login;
```

At 5k logins / ≤0.8M rows a hash aggregate after index-only or seq scan of a **narrow** completed subset is acceptable. Do not add a covering INCLUDE of `net_realized_pnl` until this query is measured as a dashboard p95 problem.

---

## 7. Family D — `outbox_events` pending (the 5,000-user bus)

Architecture §13: at ~5,000 users, the outbox **is** the broker. The claim query runs every 100–250 ms on two worker hosts (A41 §12). If it seq-scans processed history, `mt5_outbox_backlog` lies and risk/shadow lag.

### 7.1 Two shapes (do not implement both on disk)

| Shape | When | Pending predicate |
|---|---|---|
| **Thin (current entity)** | Only `ProcessedAt` exists | `processed_at IS NULL` |
| **A41 status machine** | `status`, `next_attempt_at`, `available_at`, `event_type` | `status = 'pending'` (expired rows still claimed — A41 §8.2) |

A61 dispatcher `(available_at, created_at) WHERE processed_at IS NULL` is the **interim** if the thin entity is migrated first (0007). A41 `ix_outbox_events_claim` is the **target** once `status` exists. **Replace** the interim; do not keep both partials.

Current `HasIndex(x => x.ProcessedAt)` is neither. It indexes every processed row. `WHERE processed_at IS NULL` can use it only as a widening filter on a fat index. **Drop it** in the first outbox migration.

### 7.2 Required indexes — target (A41, pin)

| Name | Definition | Unique |
|---|---|---|
| `outbox_events_idempotency_uk` | `(event_type, idempotency_key)` | **YES** — A41. Prefer this over A20/A61 `(aggregate_type, aggregate_id, event_type, dedupe_key)` when the A41 column set is implemented. If 0007 lands A61 columns first, create `outbox_events_dedupe_uk` and **add** the A41 UK in the status-machine migration; do not have two competing producer keys in application code. |
| `outbox_events_claim_ix` | `(event_type, next_attempt_at, created_at) WHERE status = 'pending'` | no — **the** drain index |
| `outbox_events_lease_ix` | `(locked_until) WHERE status = 'processing'` | no — reclaim |
| `outbox_events_backlog_ix` | `(status, event_type, created_at)` | no — `mt5_outbox_backlog` gauge |
| `outbox_events_correlation_ix` | `(correlation_id)` | no |
| `outbox_events_source_ix` | `(broker_id, source_login, occurred_at DESC) WHERE broker_id IS NOT NULL` | no |
| `outbox_events_poisoned_ix` | `(updated_at DESC) WHERE status = 'poisoned'` | no |

A30’s `(status, available_at)` without a partial is **insufficient**. A61’s partial without `event_type` forces a dedicated worker to skip other types after fetch. Dedicated workers pass `event_type = ANY(@types)` (A41 claim SQL). Leading `event_type` on the claim index is mandatory.

### 7.3 Interim indexes — thin entity (0007 only)

Use **only** if 0007 cannot yet add `status`:

| Name | Definition |
|---|---|
| `outbox_events_dispatcher_ix` | `(occurred_at, id) WHERE processed_at IS NULL` |
| `outbox_events_dedupe_uk` | wait until `dedupe_key` / `idempotency_key` exists — do not unique `aggregate_id` alone |

Thin claim (EF cannot emit this from LINQ — A61 §5.4):

```sql
SELECT *
FROM outbox_events
WHERE processed_at IS NULL
ORDER BY occurred_at, id
LIMIT 32
FOR UPDATE SKIP LOCKED;
```

### 7.4 Target claim SQL (must match `outbox_events_claim_ix`)

Copied from A41 §8.2 so the index and the query cannot drift:

```sql
WITH claimed AS (
    SELECT id
    FROM outbox_events
    WHERE status = 'pending'
      AND event_type = ANY (@types)
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
SET status = 'processing',
    attempt_count = o.attempt_count + 1,
    last_attempt_at = now(),
    locked_until = now() + (@lease_seconds * interval '1 second'),
    locked_by = @worker_id
FROM claimed
WHERE o.id = claimed.id
RETURNING o.*;
```

**Do not** put `expires_at` in the claim `WHERE` (A41: expired rows must still be claimed and marked `expired`, or `mt5_outbox_backlog` is a lie).

`mt5_outbox_backlog` (A50 / A41 §15) = `COUNT(*) … GROUP BY event_type, status` using `outbox_events_backlog_ix`. Health must also expose `oldest_pending_age_seconds` (`now() - MIN(created_at) FILTER (WHERE status = 'pending')`).

### 7.5 Target DDL (not applied)

```sql
CREATE UNIQUE INDEX outbox_events_idempotency_uk
    ON outbox_events (event_type, idempotency_key);

CREATE INDEX outbox_events_claim_ix
    ON outbox_events (event_type, next_attempt_at, created_at)
    WHERE status = 'pending';

CREATE INDEX outbox_events_lease_ix
    ON outbox_events (locked_until)
    WHERE status = 'processing';

CREATE INDEX outbox_events_backlog_ix
    ON outbox_events (status, event_type, created_at);

CREATE INDEX outbox_events_correlation_ix
    ON outbox_events (correlation_id);

CREATE INDEX outbox_events_source_ix
    ON outbox_events (broker_id, source_login, occurred_at DESC)
    WHERE broker_id IS NOT NULL;

CREATE INDEX outbox_events_poisoned_ix
    ON outbox_events (updated_at DESC)
    WHERE status = 'poisoned';
```

**Do not** INCLUDE `payload` / `payload_json` on the claim index (jsonb bloat). The claim CTE selects `id` only; the UPDATE returns the row by PK.

Fillfactor **70** on `outbox_events` (HOT updates of status/lease). Autovacuum more aggressive than default (see §10).

### 7.6 5,000-account enqueue rules that keep the pending index small

| Rule | Why the pending index cares |
|---|---|
| Live ingest TX writes raw + optional `ingestion_events` only (A41 §14.1) | 5k × deal pump must not insert 5k pending score/shadow rows per tick |
| Backfill enqueues **no** `shadow-copy-intent` / `risk-check-request` (A41 §11) | Years of history would explode the claim index and violate §63 |
| One `trade-completed` per reconstructed id | Producer UK |
| Poison leaves `claim_ix` | `status` change drops the row from the partial |
| Processed leaves `claim_ix` | Same |

If pending exceeds ~10k for more than one poll interval, the bug is **producers** (backfill leak) or a dead worker, not a missing hash index.

### 7.7 EXPLAIN contract

```sql
EXPLAIN (ANALYZE, BUFFERS)
SELECT id
FROM outbox_events
WHERE status = 'pending'
  AND event_type = ANY (ARRAY['trade-completed'])
  AND next_attempt_at <= now()
ORDER BY created_at
LIMIT 32
FOR UPDATE SKIP LOCKED;
```

**Pass:** `Index Scan` on `outbox_events_claim_ix` with a tiny heap fetch. **Fail:** `Seq Scan` or a bitmap over `outbox_events_pkey`.

Seed ≥100k `processed` rows and ≤32 `pending` before this test. That is the 5,000-account shape after a week of drain.

---

## 8. Supporting indexes the four families depend on (minimal)

Not in the title, but 5,000-account ingest will seq-scan without them. Include in the owning A30 migration.

| Table | Name | Definition |
|---|---|---|
| `brokers` | `brokers_code_uk` | UNIQUE `(code)` — `ResolveBrokerIdAsync` |
| `mt5_groups` | `mt5_groups_broker_name_uk` | UNIQUE `(broker_id, name)` — Domain `Name` = `group_name` |
| `sync_checkpoints` | `sync_checkpoints_uk` | UNIQUE `(broker_id, login, stream)` until A59 widens |
| `trader_scores` | `trader_scores_uk` | UNIQUE `(broker_id, login)` — 5k leaderboard |
| `copy_intents` | current `IdempotencyKey` UK | keep; A41 also wants `(source_broker_id, source_event_id, exposure_class)` |
| `execution_intents` | `execution_intents_clord_uk` | UNIQUE `cl_ord_id` — **not** an outbox partition |

`fix_sessions` unique on `Qualifier` alone (current EF) is **wrong** vs A20 `(venue_id, session_qualifier)`. Out of scope except: do not copy that pattern.

---

## 9. What must never be indexed / uniqued

| Candidate | Why |
|---|---|
| Global `login` | Achiever `1001` ≠ StarwaveFX `1001` |
| Global `deal_ticket` / `position_id` | §10 |
| `mt5_deals (broker_id, order_ticket)` UNIQUE | Many deals |
| `reconstructed_trades (broker_id, position_id)` UNIQUE | A21 lifecycle reuse |
| `outbox_events (processed_at)` unfiltered | Fat drain index |
| GIN on `outbox_events.payload` | No query needs it in v1 |
| Trigram on `login` at 5k | Not measured |
| Separate index per broker via partial `WHERE broker_id = '…'` | Two brokers; the leading `broker_id` column is enough |
| Hash indexes | No ordering; login-time and claim need order |
| Expression index on `login::text` | Dashboard `q=` is 5k seq-filter |

---

## 10. Maintenance at this scale

| Object | Setting | Reason |
|---|---|---|
| `mt5_deals` | fillfactor 100, default autovacuum | Append-only |
| `mt5_accounts` | fillfactor 80 | Equity upserts |
| `reconstructed_trades` | fillfactor 90 | Lifecycle upsert |
| `outbox_events` | fillfactor 70; `autovacuum_vacuum_scale_factor = 0.02` (or `autovacuum_vacuum_threshold = 200`) | HOT status/lease updates every poll |
| All four | `CREATE INDEX CONCURRENTLY` on a non-empty lab DB | Never lock 2M deals in a later add |
| Statistics | default is enough at 5k accounts | Revisit if `LoadDealsAsync` picks seq scan |

No `REINDEX` schedule in v1. No Timescale. No Citus. No extra schema.

---

## 11. Name map (fluent today → pinned SQL)

Assume A61 snake_case when the first real migration is written.

| Fluent (today) | Pinned name |
|---|---|
| `HasIndex(BrokerId, Login).IsUnique()` on accounts | `mt5_accounts_identity_uk` |
| `HasIndex(BrokerId, DealTicket).IsUnique()` | `mt5_deals_identity_uk` |
| `HasIndex(BrokerId, Login, DealTime)` | `mt5_deals_login_time_ix` **+ `DealTicket`** |
| `HasIndex(BrokerId, Login, PositionId, OpenedAt)` | replace with `reconstructed_trades_lifecycle_uk` |
| `HasIndex(ProcessedAt)` | **drop**; replace with `outbox_events_claim_ix` / interim dispatcher |

---

## 12. Implementation sequence (when a coding agent is assigned)

This agent does **not** apply these. Order matches A30 so indexes are born with their tables:

| Migration | Indexes from this file |
|---|---|
| 0002 | `brokers_code_uk` |
| 0003 | `mt5_groups_broker_name_uk` |
| 0004 | account identity + group + last-sync |
| 0006 | deal identity + **4-col login-time** + position + order; positions `(broker_id, login)` |
| 0007 | outbox interim **or** A41 claim set if status columns land here; drop any unfiltered `processed_at` index |
| 0009 | reconstructed lifecycle UK + partials + login-closed |

Integration tests (real Postgres / Testcontainers only):

1. Unique violation on `(broker_id, deal_ticket)` increments the duplicate path; a second broker with the same ticket **inserts**.
2. `EXPLAIN` contracts in §5.5, §6.5, §7.7.
3. Two pollers, one pending row → one claim (`SKIP LOCKED`).
4. 100k processed + 32 pending → claim still uses `outbox_events_claim_ix`.
5. Two reconstructed lifecycles same `(broker_id, position_id)` different `lifecycle_seq` → both insert.

Do not accept InMemory green as proof.

---

## 13. Coverage checklist

| Hot path | Required index | In `TraderDbContext` today | On Postgres today |
|---|---|---|---|
| 5k account upsert / FK | `mt5_accounts_identity_uk` | YES (unnamed) | **No** |
| Group census | `mt5_accounts_group_ix` | **No** | **No** |
| Deal dedup | `mt5_deals_identity_uk` | YES (unnamed) | **No** |
| Deals by `(broker, login, time)` | `mt5_deals_login_time_ix` (4-col) | 3-col only | **No** |
| Reconstruct / replace trades | lifecycle UK + login prefix | non-unique 4-col | **No** |
| First-3 XAUUSD | `reconstructed_trades_xau_completed_ix` | **No** | **No** |
| Trade Explorer | `reconstructed_trades_explorer_ix` | **No** | **No** |
| Outbox pending claim | partial claim index | **wrong** full `ProcessedAt` | **No** |
| `mt5_outbox_backlog` | `(status, event_type, created_at)` | **No** | **No** |

**A98 done.** Product source was not modified. The next authorized persistence increment must emit these names in a new versioned migration, not by editing this markdown into live SQL by hand against a lab DB that already has data (use `CONCURRENTLY` + EF migration).
