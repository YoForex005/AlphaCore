# A20 — Complete Database Table Catalog

**Source:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Sections used:** §10 Multi-Broker Identity Rules, §11 Raw MT5 Data Layer, §44 cTrader Execution Tables, §45 Recommended Core Database Tables  
**Supporting field lists (keys only, not extra tables):** §9 plan mappings, §14 reconstructed trade, §16 symbol mapping, §22 trader states, §24 shadow persist, §31 destination quote, §33 execution intent / ClOrdID, §35 source↔destination mapping, §36 copy timing, §57 correlation IDs, §63 CopyIntent expiry  
**Scope:** target PostgreSQL catalog for the v2 architecture. Suggested PK / UNIQUE only — no product-source schema change.  
**Date:** 2026-08-18  
**Engine:** PostgreSQL (architecture §5). Durable source of truth is the database, not Redis.

---

## 1. Naming unification (do not create both)

§11, §24, and §45 use overlapping names. Canonicalize to the **§45 name** unless noted. Aliases are **not** extra tables.

| Alias (do not persist as a second table) | Canonical table | Reason |
|---|---|---|
| `mt5_symbol_metadata` (§11) | `mt5_symbols` (§45) | Same source-symbol metadata |
| `mt5_ticks_xauusd` (§11) | `mt5_xau_ticks` (§45) | Same optional source tick stream |
| `shadow_copy_order` (§24) | `shadow_orders` (§45) | Same shadow order |
| `shadow_copy_fill` (§24) | `shadow_fills` (§45) | Same shadow fill |
| `shadow_position` (§24) | `shadow_positions` (§45) | Same shadow position |
| `shadow_pnl` / `source_vs_shadow_slippage` (§24) | `shadow_performance` (§45) | Rollup / attribution, not a second entity |

Tables listed in §11 or §44 but **omitted from the §45 “full initial set”** are **in-scope** and included below:

| Extra table | Listed in | Why it stays |
|---|---|---|
| `ingestion_events` | §11 | Raw immutable ingest / dedup evidence |
| `execution_intents` | §44, §33 | Required before any FIX send |
| `execution_reconciliation_runs` | §44, §42–43 | Startup + periodic reconcile |
| `execution_reconciliation_issues` | §44, §43 | Durable mismatch records |

**Union count: 47 tables.**

---

## 2. Multi-broker identity law (§10) — binding

Never treat login or ticket IDs as globally unique.

Required compound identities:

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

All **source-side** tables must carry `broker_id NOT NULL`.

### How this catalog applies the law

| Entity | Natural unique key | Surrogate PK still used? |
|---|---|---|
| Source trader / account | `(broker_id, login)` | Yes (`id uuid`) so FKs stay stable if a login is recycled on a *different* broker |
| Source deal | `(broker_id, deal_ticket)` | Yes |
| Source order | `(broker_id, order_ticket)` | Yes |
| Source / reconstructed position | `(broker_id, position_id)` | Yes (`reconstructed_trades.id` is the `source_trade_id` used everywhere else) |
| Destination / FIX | **not** `broker_id`. Use `(venue_id, …)` plus venue-native IDs | Yes |
| Copy / risk / execution chain | UUID intent IDs + unique `cl_ord_id` | Yes; still *carry* `source_broker_id + source_login` as correlation, not as the row’s only identity |

`source_broker_id` on destination/copy tables is the **same** `brokers.id` as source `broker_id`. Naming prefix `source_` exists only to avoid colliding with destination account identifiers.

### Types (suggested)

| Column | Type | Notes |
|---|---|---|
| `id` / `*_id` (surrogate) | `uuid` | Default `gen_random_uuid()` |
| `broker_id` | `uuid` | FK → `brokers.id` |
| `login` / `source_login` | `bigint` | MT5 login is `uint64`; values in this lab fit signed `bigint` |
| `deal_ticket` / `order_ticket` / `position_id` | `bigint` | MT5 tickets |
| Timestamps | `timestamptz` | Store UTC |
| Money / price / volume (raw) | `numeric` | Do not use `double` in durable tables |
| FIX `cl_ord_id` | `text` | Unique client order ID; persist **before** send (§33) |

---

## 3. Key-design conventions

1. **Surrogate UUID PK** on every entity that is referenced by outbox, audit, SignalR, or logs (`copy_intent_id`, `risk_decision_id`, `execution_intent_id`, `correlation_id`).
2. **Natural UNIQUE** is the real identity. Application upserts/idempotency use the UNIQUE, not the UUID.
3. **Partial UNIQUE** (`WHERE col IS NOT NULL`) for IDs assigned only after a venue ack (cServer order ID, dest position ID, FIX ExecID).
4. **Raw layer is as immutable as practical** (§11). `mt5_deals` / `mt5_orders` upsert on the compound ticket key; corrections are new revisions or audit rows, not silent overwrites of history.
5. **`mt5_positions_current` is the exception:** it is the live book (delete on close). History lives in deals + reconstructed trades.
6. **Do not unique-constrain globally** on `login`, `deal_ticket`, `order_ticket`, `position_id`, source `symbol`, or FIX tag 55.
7. **Redis is not a key authority** for orders, positions, or balances (§5).

---

## 4. Master catalog

Legend: **S** = source-side (`broker_id` required). **G** = global / registry. **D** = destination / FIX. **X** = cross-cutting (carries `source_broker_id` but is not a source ticket store).

| # | Table | Layer | Origin | Side | Mutability | Suggested PK | Suggested UNIQUE / identity |
|---|---|---|---|---|---|---|---|
| 1 | `brokers` | registry | §45 | G | mutable metadata | `id uuid` | `code` |
| 2 | `broker_connections` | registry | §45 | S | mutable (no secrets) | `id uuid` | `(broker_id, connection_name)` |
| 3 | `mt5_groups` | raw MT5 | §11, §45 | S | upsert | `id uuid` | `(broker_id, group_name)` |
| 4 | `plan_group_mappings` | registry | §9, §45 | S | mutable overlay | `id uuid` | `(broker_id, plan_code, environment)` |
| 5 | `mt5_accounts` | raw MT5 | §11, §45 | S | upsert | `id uuid` | **`(broker_id, login)`** |
| 6 | `mt5_account_snapshots` | raw MT5 | §11, §45 | S | append | `id uuid` | `(broker_id, login, snapshot_at)` |
| 7 | `mt5_orders` | raw MT5 | §11, §45 | S | upsert / immutable | `id uuid` | **`(broker_id, order_ticket)`** |
| 8 | `mt5_deals` | raw MT5 | §11, §45 | S | upsert / immutable | `id uuid` | **`(broker_id, deal_ticket)`** |
| 9 | `mt5_positions_current` | raw MT5 | §11, §45 | S | mutable live book | `id uuid` | **`(broker_id, position_id)`** |
| 10 | `mt5_symbols` | raw MT5 | §11 as metadata, §45 | S | upsert | `id uuid` | `(broker_id, source_symbol)` |
| 11 | `mt5_xau_ticks` | raw MT5 | §11, §45 | S | append | `id bigint` identity | `(broker_id, source_symbol, time_msc, flags, ingest_seq)` |
| 12 | `ingestion_events` | raw MT5 | §11 | S | append / idempotent | `id uuid` | `(broker_id, source_event_id)` |
| 13 | `reconstructed_trades` | reconstruction | §14, §45 | S | upsert by position | `id uuid` (= `source_trade_id`) | **`(broker_id, position_id)`** |
| 14 | `canonical_instruments` | mapping | §16, §45 | G | mutable | `id uuid` | `canonical_symbol` |
| 15 | `source_symbol_mappings` | mapping | §16, §45 | S | upsert | `id uuid` | `(broker_id, source_symbol)` |
| 16 | `trader_feature_snapshots` | scoring | §17–20, §45 | S | append | `id uuid` | `(broker_id, login, completed_trade_count, feature_schema_version)` |
| 17 | `trader_scores` | scoring | §18, §22, §45 | S | current row | `id uuid` | `(broker_id, login, score_kind)` |
| 18 | `trader_score_history` | scoring | §22, §45 | S | append | `id uuid` | `(broker_id, login, completed_trade_count, score_kind, model_version_id)` |
| 19 | `trader_states` | scoring | §22, §45 | S | current row | `id uuid` | **`(broker_id, login)`** |
| 20 | `trader_risk_flags` | scoring | §18, §45 | S | current flags | `id uuid` | `(broker_id, login, flag_code)` |
| 21 | `model_versions` | ML | §19–21, §45 | G | insert-only versions | `id uuid` | `(model_name, version)` |
| 22 | `model_predictions` | ML | §19–20, §45 | S | append | `id uuid` | `(model_version_id, broker_id, login, completed_trade_count)` |
| 23 | `model_evaluations` | ML | §21, §45 | G | append | `id uuid` | `(model_version_id, evaluation_split, metric_set_version)` |
| 24 | `shadow_orders` | shadow | §24, §45 | X | insert | `id uuid` | `shadow_cl_ord_id` |
| 25 | `shadow_fills` | shadow | §24, §45 | X | append | `id uuid` | `(shadow_order_id, fill_seq)` |
| 26 | `shadow_positions` | shadow | §24, §45 | X | upsert | `id uuid` | `(source_broker_id, source_trade_id)` |
| 27 | `shadow_performance` | shadow | §24, §45 | X | current + dated | `id uuid` | `(source_broker_id, login, period_grain, period_start)` |
| 28 | `copy_intents` | copy | §32, §44–45 | X | insert (expire, don’t rewrite identity) | `id uuid` (= `copy_intent_id`) | `(source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| 29 | `copy_allocations` | copy | §38, §45 | X | insert | `id uuid` | `(copy_intent_id, destination_account)` |
| 30 | `risk_decisions` | risk | §32, §39, §44–45 | X | append | `id uuid` (= `risk_decision_id`) | `(copy_intent_id, decision_seq)` |
| 31 | `risk_events` | risk | §39–40, §45 | X | append | `id uuid` | none (audit stream) |
| 32 | `execution_venues` | dest | §44–45 | D | mutable metadata | `id uuid` (= `venue_id`) | `venue_code` |
| 33 | `destination_symbols` | dest | §16, §30, §44–45 | D | upsert from SecurityList | `id uuid` | `(venue_id, instrument_id)` |
| 34 | `destination_quotes` | dest | §31, §44–45 | D | upsert latest | `id uuid` | `(venue_id, instrument_id)` |
| 35 | `fix_sessions` | dest | §27–28, §44–45 | D | mutable session | `id uuid` | `(venue_id, session_qualifier)` |
| 36 | `fix_session_events` | dest | §44–45 | D | append | `id uuid` | none |
| 37 | `execution_intents` | dest | §33, §44 | X | insert before send | `id uuid` (= `execution_intent_id`) | `cl_ord_id` |
| 38 | `fix_orders` | dest | §33, §44–45 | D | upsert by ClOrdID | `id uuid` | `cl_ord_id`; partial `(venue_id, dest_order_id)` |
| 39 | `fix_execution_reports` | dest | §33–34, §44–45 | D | append / dedup | `id uuid` | `(venue_id, exec_id)` |
| 40 | `destination_positions` | dest | §35, §44–45 | D | upsert | `id uuid` | `(venue_id, destination_account, destination_position_id)` |
| 41 | `source_destination_links` | dest | §35, §44–45 | X | upsert | `id uuid` | `(source_broker_id, source_trade_id, link_role, execution_intent_id)` |
| 42 | `execution_reconciliation_runs` | dest | §42–44 | D | append | `id uuid` | none |
| 43 | `execution_reconciliation_issues` | dest | §43–44 | D | upsert per run | `id uuid` | `(run_id, issue_fingerprint)` |
| 44 | `sync_checkpoints` | ops | §11–12, §45 | S+D | upsert | `id uuid` | `(scope_type, scope_id, stream_name)` |
| 45 | `outbox_events` | ops | §12–13, §45 | X | insert once | `id uuid` | `(aggregate_type, aggregate_id, event_type, dedupe_key)` |
| 46 | `audit_logs` | ops | §45, §59 | G | append | `id bigint` identity | none |
| 47 | `system_events` | ops | §45, §40 | G | append | `id uuid` | none |

---

## 5. Detailed key cards

Each card lists: purpose, §10 `broker_id` rule, PK, UNIQUE, useful non-unique indexes, and FK notes. Column lists are **key-relevant only** (enough to justify the constraint), not a full DDL dump.

---

### 5.1 Registry / broker

#### `brokers`

Broker registry for Achiever, StarwaveFX, and future source brokers (§6, §45).

| | |
|---|---|
| Side | Global |
| `broker_id` | This table **defines** it (`id`) |
| PK | `id uuid` |
| UNIQUE | `brokers_code_uk (code)` — e.g. `ACHIEVER`, `STARWAVEFX` |
| UNIQUE (optional) | `brokers_server_name_uk (server_name)` if server names are globally distinct |
| Do not unique | manager login, host:port (may be reused or rotated) |

#### `broker_connections`

Non-secret connection profile (host, port, manager login **number**, pool size, mode, proxy-enabled flag). Passwords stay in secret storage (§7–8, §55).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** FK → `brokers.id` |
| PK | `id uuid` |
| UNIQUE | `broker_connections_name_uk (broker_id, connection_name)` |
| UNIQUE (typical 1:1) | `broker_connections_one_primary_uk (broker_id) WHERE is_primary` |
| Do not store | passwords, proxy credentials |

---

### 5.2 Raw MT5 layer (§11)

All tables in this subsection **must** include `broker_id`.

#### `mt5_groups`

Dynamically discovered groups. Plan mapping must **not** filter which groups are stored (§7, §9).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `mt5_groups_broker_name_uk (broker_id, group_name)` |
| Notes | `group_name` is broker-local (`demo\Maxmaster`, `demo\yo-2step`). Same string on two brokers is two rows. |

#### `plan_group_mappings`

Optional overlay only. Multiple plan codes may share one group (CORE_DEMO and 2STEP_DEMO both `demo\yo-2step` in §9).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `plan_group_mappings_plan_uk (broker_id, plan_code, environment)` |
| Do **not** unique | `(broker_id, group_name)` — many plans → one group |
| FK | `(broker_id, group_name)` → `mt5_groups (broker_id, group_name)` |

#### `mt5_accounts`

Canonical source trader identity.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`mt5_accounts_identity_uk (broker_id, login)`** — §10 |
| INDEX | `(broker_id, group_name)`, `(broker_id, last_event_at DESC)` |
| FK | `broker_id` → `brokers`; group association by `(broker_id, group_name)` |

`login` alone is **not** unique. Achiever login `1001` and StarwaveFX login `1001` are different traders.

#### `mt5_account_snapshots`

Point-in-time balance/equity/margin (UserData / AccountData style). Append-only.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `mt5_account_snapshots_uk (broker_id, login, snapshot_at)` |
| INDEX | `(broker_id, login, snapshot_at DESC)` |
| FK | `(broker_id, login)` → `mt5_accounts (broker_id, login)` |

If two collectors can snapshot the same second, add `source` to the UNIQUE: `(broker_id, login, snapshot_at, snapshot_source)`.

#### `mt5_orders`

Raw orders. Immutable as practical; upsert on the compound ticket.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`mt5_orders_identity_uk (broker_id, order_ticket)`** — §10 |
| INDEX | `(broker_id, login, time_setup)`, `(broker_id, position_id)` where present |
| FK | `(broker_id, login)` → `mt5_accounts` |
| Carry | `login`, `source_symbol`, `position_id` (nullable until known) |

#### `mt5_deals`

Raw deals. Dedup metric `mt5_duplicate_deals_total` (§58) is defined against this key.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`mt5_deals_identity_uk (broker_id, deal_ticket)`** — §10 |
| INDEX | `(broker_id, login, deal_time)`, `(broker_id, order_ticket)`, `(broker_id, position_id)`, `(broker_id, source_symbol, deal_time)` |
| FK | `(broker_id, login)` → `mt5_accounts` |
| Carry | `login`, `order_ticket`, `position_id`, `source_symbol` |

Do **not** unique `(broker_id, order_ticket)` — one order may produce multiple deals.

#### `mt5_positions_current`

Live open positions only. Closed positions are removed; history is deals + `reconstructed_trades`.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`mt5_positions_current_identity_uk (broker_id, position_id)`** — §10 |
| INDEX | `(broker_id, login)`, `(broker_id, source_symbol)` |
| FK | `(broker_id, login)` → `mt5_accounts` |

MT5 position tickets are not reused on the same server in normal operation. If a future broker reuses them after close, this table is still safe (row is gone). `reconstructed_trades` would then need `(broker_id, position_id, opened_at)` — see open question in §8.

#### `mt5_symbols` *(§11 `mt5_symbol_metadata`)*

Per-broker contract metadata (digits, contract size, volume min/step/max, trade mode).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `mt5_symbols_uk (broker_id, source_symbol)` |
| Do not unique | `source_symbol` globally (`XAUUSD.`, `XAUUSDm`, `GOLD` are broker-local) |

#### `mt5_xau_ticks` *(§11 `mt5_ticks_xauusd`)*

Optional source tick stream for exact MFE/MAE (§17). Only persist if the source SDK/feed supports it. Do not silently substitute cTrader quotes.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id bigint GENERATED ALWAYS AS IDENTITY` (volume) |
| UNIQUE | `mt5_xau_ticks_uk (broker_id, source_symbol, time_msc, flags, ingest_seq)` |
| INDEX | `(broker_id, source_symbol, time_msc)` |
| Partition | recommend `RANGE (time_msc)` or `RANGE (tick_time)` |

`ingest_seq` is collector-assigned per `(broker_id, source_symbol, time_msc, flags)` so two ticks in the same millisecond stay unique. Exact-duplicate deliveries: same key → `ON CONFLICT DO NOTHING`.

#### `ingestion_events` *(§11 only)*

Immutable ingest evidence: validate → deduplicate → persist raw → outbox (§12). Complements (does not replace) `mt5_deals` / `mt5_orders`.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `ingestion_events_source_uk (broker_id, source_event_id)` |
| UNIQUE (optional hash) | `ingestion_events_payload_uk (broker_id, payload_hash)` if the collector hashes the payload |
| INDEX | `(broker_id, login, occurred_at)`, `(broker_id, entity_type, entity_ticket)` |
| Carry | `login` (nullable for group-level events), `entity_type`, `entity_ticket`, `event_kind`, `payload`, `payload_hash` |

`source_event_id` must be constructed so it is unique **per broker** (e.g. `deal:{ticket}` / `order:{ticket}:{state}` / `user:{login}:{revision}`). Do not use a global event id from two managers.

---

### 5.3 Reconstruction and symbol mapping

#### `reconstructed_trades`

One row = one completed (or in-progress) **position lifecycle**, not one deal (§14–15). `id` is the platform `source_trade_id`.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`reconstructed_trades_position_uk (broker_id, position_id)`** — §10 + §14 |
| INDEX | `(broker_id, login, closed_at)`, `(broker_id, login, completed, canonical_symbol)`, `(broker_id, canonical_symbol, closed_at)` |
| FK | `(broker_id, login)` → `mt5_accounts`; `canonical_symbol` → `canonical_instruments` |
| Carry | fields from §14: `login`, `position_id`, `canonical_symbol`, `source_symbol`, direction, VWAP, volumes, PnL legs, flags, `completed` |

First-3-trade counting uses **completed** rows with `canonical_symbol = XAUUSD` only.

#### `canonical_instruments`

Platform symbol identity. First instrument is `XAUUSD` (§16).

| | |
|---|---|
| Side | Global |
| `broker_id` | **No** |
| PK | `id uuid` |
| UNIQUE | `canonical_instruments_symbol_uk (canonical_symbol)` |

#### `source_symbol_mappings`

`broker/source symbol → canonical` (§16). Never assume the string `XAUUSD`.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `source_symbol_mappings_uk (broker_id, source_symbol)` |
| INDEX | `(canonical_symbol, broker_id)` |
| FK | `broker_id` → `brokers`; `canonical_symbol` → `canonical_instruments`; optional `(broker_id, source_symbol)` → `mt5_symbols` |

---

### 5.4 Scoring / trader state / ML

All trader-scoped tables use **`(broker_id, login)`**, never `login` alone.

#### `trader_feature_snapshots`

Features observable **as of** a given completed-trade count. Leakage rule (§20): a trade-#3 snapshot may only use data available at that close.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `trader_feature_snapshots_uk (broker_id, login, completed_trade_count, feature_schema_version)` |
| INDEX | `(broker_id, login, computed_at DESC)` |
| Carry | `price_source`, `feature_quality` (§17); `as_of` = trade-#N close time |

#### `trader_scores`

**Current** scores. History is `trader_score_history`.

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `trader_scores_uk (broker_id, login, score_kind)` |
| `score_kind` | `risk` / `behavior` / `early_quality` / `ml_probability` (§18, §50) |

If the product only ever stores one blended current score, UNIQUE collapses to `(broker_id, login)`.

#### `trader_score_history`

Append-only rescoring after trade 3, 4, 5, … (§22).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `trader_score_history_uk (broker_id, login, completed_trade_count, score_kind, model_version_id)` |
| INDEX | `(broker_id, login, scored_at DESC)` |
| `model_version_id` | NULL for deterministic baseline rows; use a sentinel UUID `00000000-…-base` **or** make the UNIQUE `NULLS NOT DISTINCT` (PG 15+) |

#### `trader_states`

One current state per source trader (§22).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | **`trader_states_identity_uk (broker_id, login)`** |
| CHECK | `state IN ('INSUFFICIENT_DATA','EARLY_SCORE','WATCH','SHADOW','LIVE_CANDIDATE','LIVE','PAUSED','RISK_BLOCKED','DISQUALIFIED')` |
| FK | `(broker_id, login)` → `mt5_accounts` |

State transitions belong in `audit_logs` / `system_events`; do not overwrite without an audit row.

#### `trader_risk_flags`

Current flags (martingale, averaging-down, lot escalation, …).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `trader_risk_flags_uk (broker_id, login, flag_code)` |
| INDEX | `(flag_code, is_active)` WHERE active |

If flag history is required, do **not** unique on that triple; add `raised_at` and unique `(broker_id, login, flag_code, raised_at)` or keep history in `risk_events`.

#### `model_versions`

Promotable model artifacts. Promotion is audited; no self-promotion (§71).

| | |
|---|---|
| Side | Global |
| `broker_id` | **No** |
| PK | `id uuid` |
| UNIQUE | `model_versions_uk (model_name, version)` |
| UNIQUE (optional) | `model_versions_one_prod_uk (model_name) WHERE is_production` |

#### `model_predictions`

One prediction per (model, trader, as-of trade N). Must not include future features (§20).

| | |
|---|---|
| Side | Source |
| `broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `model_predictions_uk (model_version_id, broker_id, login, completed_trade_count)` |
| FK | `model_version_id` → `model_versions`; `(broker_id, login)` → `mt5_accounts` |

#### `model_evaluations`

Out-of-sample evaluation rows (top 1/5/10/20% vs baselines, §21).

| | |
|---|---|
| Side | Global (may slice by broker in payload, not in identity) |
| `broker_id` | optional **dimension**, not required in PK |
| PK | `id uuid` |
| UNIQUE | `model_evaluations_uk (model_version_id, evaluation_split, metric_set_version)` |

`evaluation_split` examples: `validation`, `final_test`, `live_shadow`. Chronological split only.

---

### 5.5 Shadow copy (§24, §45)

Shadow models the destination venue using the **cTrader QUOTE** session. Rows carry `source_broker_id + source_login + source_trade_id` for correlation; they are **not** source ticket stores.

#### `shadow_orders`

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** (same as `broker_id`) |
| PK | `id uuid` |
| UNIQUE | `shadow_orders_clord_uk (shadow_cl_ord_id)` |
| UNIQUE (optional) | `shadow_orders_intent_uk (copy_intent_id)` if 1:1 |
| INDEX | `(source_broker_id, source_login, created_at)` |
| FK | `copy_intent_id` → `copy_intents`; `source_trade_id` → `reconstructed_trades.id` |

#### `shadow_fills`

| | |
|---|---|
| Side | Cross |
| PK | `id uuid` |
| UNIQUE | `shadow_fills_uk (shadow_order_id, fill_seq)` |
| INDEX | `(source_broker_id, source_trade_id)` |
| FK | `shadow_order_id` → `shadow_orders` |

#### `shadow_positions`

One shadow position per source reconstructed trade in the default model (scale-in/out updates the same row).

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `shadow_positions_source_uk (source_broker_id, source_trade_id)` |
| INDEX | `(source_broker_id, source_login, is_open)` |
| FK | `(source_broker_id, source_trade_id)` → `reconstructed_trades (broker_id, id)` |

#### `shadow_performance`

Rollup including source-vs-shadow slippage (§24). Grain is explicit so daily and lifetime rows can coexist.

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `shadow_performance_uk (source_broker_id, login, period_grain, period_start)` |
| `period_grain` | `LIFETIME` / `DAY` / `TRADE` |
| For `TRADE` grain | include `source_trade_id` in the UNIQUE instead of `period_start` |

---

### 5.6 Copy + risk (§32–40, §44–45)

#### `copy_intents`

Created from a source event **before** risk and **before** FIX (§32). Must expire (§63).

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** |
| PK | `id uuid` (`copy_intent_id`) |
| UNIQUE | `copy_intents_idem_uk (source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| INDEX | `(status, expires_at)`, `(source_broker_id, source_login, created_at DESC)` |
| Carry | `source_event_time`, `collector_receive_time`, `decision_time`, `expires_at`, `max_signal_age` (§36, §63) |
| `action` | `OPEN_EXPOSURE` / `INCREASE_EXPOSURE` / `REDUCE_EXPOSURE` / `CLOSE_EXPOSURE` (§64) |

Idempotency is the unique key: the same source event + action cannot create two intents.

#### `copy_allocations`

Normalized sizing result (§38): source volume → canonical notional → destination quantity.

| | |
|---|---|
| Side | Cross |
| PK | `id uuid` |
| UNIQUE | `copy_allocations_uk (copy_intent_id, destination_account)` |
| FK | `copy_intent_id` → `copy_intents`; `destination_account` scoped by `venue_id` |
| Carry | source volume, dest qty, min/step checks, allocation reason |

#### `risk_decisions`

Risk is the final authority (§39). Multiple decisions per intent are allowed (re-eval after quote change).

| | |
|---|---|
| Side | Cross |
| PK | `id uuid` (`risk_decision_id`) |
| UNIQUE | `risk_decisions_uk (copy_intent_id, decision_seq)` |
| INDEX | `(decision, created_at DESC)`, `(source_broker_id, source_login, created_at DESC)` |
| `decision` | `approve` / `reduce_size` / `reject` / `pause_trader` / `pause_venue` / `global_stop` |
| Carry | quote age, spread, price-move, reason codes (`PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, …) |

If the product enforces a single final decision, add UNIQUE `(copy_intent_id) WHERE is_final`.

#### `risk_events`

Kill-switch, pause/resume, flatten requests, limit-breach notifications. Append-only.

| | |
|---|---|
| Side | Cross |
| `broker_id` / `source_broker_id` | NULL for venue-global events; NOT NULL for trader-scoped |
| PK | `id uuid` |
| UNIQUE | **none** |
| INDEX | `(event_type, occurred_at DESC)`, `(source_broker_id, source_login, occurred_at DESC)` |
| Do not conflate | `STOP_NEW_EXECUTION` vs `EMERGENCY_FLATTEN` (§40) — different `event_type` |

---

### 5.7 Destination / cTrader FIX (§44 + extras)

Destination identity is **`(venue_id, venue-native id)`**, not `broker_id`. Always persist `source_broker_id` on link/intent/order rows for logs (§57).

#### `execution_venues`

Pepperstone / cServer account is an **external execution venue**, not an LP unless contractually true (§1.6).

| | |
|---|---|
| Side | Dest |
| `broker_id` | **No** (source brokers are not venues) |
| PK | `id uuid` (`venue_id`) |
| UNIQUE | `execution_venues_code_uk (venue_code)` e.g. `PEPPERSTONE_CSERVER` |
| UNIQUE (optional) | `(fix_account_id)` if one venue row per cTrader account |

#### `destination_symbols`

Persisted Security List mapping. Do not hardcode instrument IDs (§16, §30). Never assume FIX tag 55 = `"XAUUSD"`.

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `destination_symbols_instr_uk (venue_id, instrument_id)` |
| UNIQUE | `destination_symbols_name_uk (venue_id, venue_symbol)` |
| INDEX | `(canonical_symbol, venue_id)` |
| FK | `venue_id` → `execution_venues`; `canonical_symbol` → `canonical_instruments` |

#### `destination_quotes`

**Latest** quote per venue instrument (§31). Risk rejects stale quotes.

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `destination_quotes_uk (venue_id, instrument_id)` — upsert in place |
| INDEX | none required beyond UK |
| Carry | `bid`, `ask`, `quote_received_at`, `venue_timestamp` (nullable), `spread`, `quote_age` generated or computed |

§44/45 list a single table. Tick-level quote **history** is not a required initial table; if added later, it must be a **new** versioned table, not a second meaning of this one.

#### `fix_sessions`

Independent QUOTE and TRADE objects (§27). Separate sequence / heartbeat / reconnect state. One active TRADE owner (§28).

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `fix_sessions_uk (venue_id, session_qualifier)` |
| CHECK | `session_qualifier IN ('QUOTE','TRADE')` |
| UNIQUE (wire) | `fix_sessions_comp_uk (venue_id, sender_comp_id, target_comp_id, session_qualifier)` |
| Carry | sequence state, last inbound/outbound, reconnect count, ownership lease / fencing token |
| Do not unique | a single sequence counter shared across QUOTE+TRADE (forbidden) |

`SenderSubID` / `TargetSubID` are attributes, not keys.

#### `fix_session_events`

Logon, logout, heartbeat miss, resend, reject, leadership change.

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | **none** |
| INDEX | `(session_id, occurred_at DESC)`, `(venue_id, session_qualifier, occurred_at DESC)` |
| FK | `session_id` → `fix_sessions` |

#### `execution_intents` *(§44, §33 — missing from §45 list)*

Persist **before** sending NewOrderSingle. Enables unknown-state recovery.

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** |
| PK | `id uuid` (`execution_intent_id`) |
| UNIQUE | **`execution_intents_clord_uk (cl_ord_id)`** — §33, §70.4 |
| UNIQUE | `execution_intents_risk_uk (risk_decision_id)` (one send per approved decision) |
| INDEX | `(status, created_at)`, `(source_broker_id, source_login, created_at)` |
| Carry | §33: `cl_ord_id`, `source_broker_id`, `source_login`, `source_trade_id`, `source_event_id`, `destination_account`, `canonical_symbol`, `side`, `requested_quantity`, `created_at`, `status` |
| `status` | `not_sent` / `sent_ack_unknown` / `accepted` / `partially_filled` / `filled` / `rejected` / `cancelled` / `EXECUTION_STATE_UNKNOWN` |

Never retry NewOrderSingle on the same `cl_ord_id`. A replacement gets a **new** row and new `cl_ord_id`.

#### `fix_orders`

Venue-visible order state. Same `cl_ord_id` as the intent.

| | |
|---|---|
| Side | Dest |
| `source_broker_id` | **NOT NULL** on copy-originated orders |
| PK | `id uuid` |
| UNIQUE | `fix_orders_clord_uk (cl_ord_id)` |
| UNIQUE | `fix_orders_dest_uk (venue_id, dest_order_id) WHERE dest_order_id IS NOT NULL` |
| FK | `cl_ord_id` → `execution_intents.cl_ord_id`; `venue_id` → `execution_venues` |
| INDEX | `(venue_id, destination_account, status)` |

#### `fix_execution_reports`

Every ExecutionReport is durable. Duplicate reports are detected by ExecID (§70.5).

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `fix_execution_reports_exec_uk (venue_id, exec_id)` |
| INDEX | `(cl_ord_id, transact_time)`, `(venue_id, dest_order_id)`, `(exec_type, created_at DESC)` |
| Carry | `cl_ord_id`, `dest_order_id`, `destination_position_id`, `last_qty`, `last_px`, `leaves_qty`, `ord_status`, `exec_type` |

`exec_id` uniqueness is **per venue**, not global.

#### `destination_positions`

cTrader position IDs from ExecutionReport / PositionReport (§35).

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `destination_positions_uk (venue_id, destination_account, destination_position_id)` |
| INDEX | `(venue_id, canonical_symbol, is_open)`, `(venue_id, side)` |
| Do not unique | quantity (it changes) |

#### `source_destination_links`

Explicit mapping: source reconstructed trade → dest orders → dest position IDs (§35). Supports scale-in, partial close, reversal. One source event is **not** forever one dest order.

| | |
|---|---|
| Side | Cross |
| `source_broker_id` | **NOT NULL** |
| PK | `id uuid` |
| UNIQUE | `source_destination_links_uk (source_broker_id, source_trade_id, link_role, execution_intent_id)` |
| INDEX | `(venue_id, destination_position_id)`, `(source_broker_id, source_login)` |
| `link_role` | `ENTRY` / `SCALE_IN` / `PARTIAL_CLOSE` / `CLOSE` / `REVERSAL` |
| FK | `source_trade_id` → `reconstructed_trades.id`; `execution_intent_id` → `execution_intents`; dest position by `(venue_id, destination_account, destination_position_id)` |

Do **not** unique `(source_broker_id, source_trade_id)` alone.

#### `execution_reconciliation_runs` *(§44)*

Startup (§42) and periodic (§43) compare of internal orders/positions vs cServer.

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` (`run_id`) |
| UNIQUE | **none** (many runs) |
| INDEX | `(venue_id, started_at DESC)`, `(run_type, status)` |
| `run_type` | `STARTUP` / `PERIODIC` / `POST_DISCONNECT` / `MANUAL` |
| Carry | blocked-new-execution flag, result (`READY_FOR_EXECUTION` or not) |

#### `execution_reconciliation_issues` *(§44)*

Durable mismatches: unknown external position, missing internal, qty/side mismatch, orphan ER, unexpected fill (§43).

| | |
|---|---|
| Side | Dest |
| PK | `id uuid` |
| UNIQUE | `execution_reconciliation_issues_uk (run_id, issue_fingerprint)` |
| INDEX | `(venue_id, issue_type, resolved_at)` WHERE unresolved |
| `issue_fingerprint` | stable hash of `(issue_type, venue_id, dest_order_id, destination_position_id, cl_ord_id)` so the same break upserts inside one run |

Nothing unresolved may be silently ignored (§54).

---

### 5.8 Operations

#### `sync_checkpoints`

Backfill + live + reconcile cursors (§12).

| | |
|---|---|
| Side | Source **and** dest (scoped) |
| `broker_id` | required when `scope_type = 'BROKER'` |
| PK | `id uuid` |
| UNIQUE | `sync_checkpoints_uk (scope_type, scope_id, stream_name)` |
| `scope_type` | `BROKER` / `VENUE` / `GLOBAL` |
| `scope_id` | `broker_id` or `venue_id` (uuid as text/uuid; prefer uuid) |
| `stream_name` | examples: `groups`, `accounts`, `deals_backfill`, `deals_live`, `orders_history`, `positions_reconcile`, `security_list`, `order_mass_status` |

Do not use a single global checkpoint. Two brokers must not share a deals cursor.

#### `outbox_events`

PostgreSQL transactional outbox (§12–13). Written in the same commit as the raw/domain row.

| | |
|---|---|
| Side | Cross |
| PK | `id uuid` |
| UNIQUE | `outbox_events_dedupe_uk (aggregate_type, aggregate_id, event_type, dedupe_key)` |
| INDEX | `(processed_at NULLS FIRST, created_at)` for the dispatcher |
| Typical aggregates | reconstructed trade, score request, copy intent, risk check, notification |
| Carry | `broker_id` / `source_login` / correlation ids when applicable (§57) |

`dedupe_key` is producer-assigned (often the natural identity, e.g. `{broker_id}:{deal_ticket}:persisted`).

#### `audit_logs`

Manual overrides, RBAC actions, config changes (§59, §72.19).

| | |
|---|---|
| Side | Global |
| PK | `id bigint GENERATED ALWAYS AS IDENTITY` |
| UNIQUE | **none** |
| INDEX | `(occurred_at DESC)`, `(actor_id, occurred_at DESC)`, `(correlation_id)`, `(broker_id, source_login)` |
| Never log | passwords, FIX password tags, proxy credentials (§55, §57) |

#### `system_events`

Platform health / mode changes (FIX connected, stale-source, kill-switch state, ML unavailable).

| | |
|---|---|
| Side | Global |
| PK | `id uuid` |
| UNIQUE | **none** |
| INDEX | `(event_type, occurred_at DESC)`, `(severity, occurred_at DESC)` |

Current kill-switch **state** should be derived from the latest `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` row (or a tiny `system_flags` table later). Do not invent that extra table in this catalog.

---

## 6. Compound-identity matrix (§10 applied)

Every source-side UNIQUE that implements the law:

| Law | Table | UNIQUE constraint name | Columns |
|---|---|---|---|
| `broker_id + login` | `mt5_accounts` | `mt5_accounts_identity_uk` | `(broker_id, login)` |
| `broker_id + login` | `trader_states` | `trader_states_identity_uk` | `(broker_id, login)` |
| `broker_id + login` | `mt5_account_snapshots` | `mt5_account_snapshots_uk` | `(broker_id, login, snapshot_at)` |
| `broker_id + login` | `trader_scores` | `trader_scores_uk` | `(broker_id, login, score_kind)` |
| `broker_id + login` | `trader_risk_flags` | `trader_risk_flags_uk` | `(broker_id, login, flag_code)` |
| `broker_id + deal_ticket` | `mt5_deals` | `mt5_deals_identity_uk` | `(broker_id, deal_ticket)` |
| `broker_id + order_ticket` | `mt5_orders` | `mt5_orders_identity_uk` | `(broker_id, order_ticket)` |
| `broker_id + position_id` | `mt5_positions_current` | `mt5_positions_current_identity_uk` | `(broker_id, position_id)` |
| `broker_id + position_id` | `reconstructed_trades` | `reconstructed_trades_position_uk` | `(broker_id, position_id)` |

Same law, carried as **correlation** (not the row’s only identity) on:

| Table | Required compound columns |
|---|---|
| `copy_intents` | `(source_broker_id, source_login, source_trade_id, …)` |
| `execution_intents` | `(source_broker_id, source_login, source_trade_id, source_event_id)` |
| `fix_orders` | `(source_broker_id, source_login, source_trade_id)` |
| `source_destination_links` | `(source_broker_id, source_trade_id)` |
| `shadow_*` | `(source_broker_id, source_login)` and/or `source_trade_id` |
| `model_predictions` | `(broker_id, login)` |
| `ingestion_events` | `(broker_id, …)` |
| `outbox_events` / logs | `broker_id` + `source_login` when known |

Destination analogues (do **not** use `broker_id` as the uniqueness root):

| Destination law | Table | UNIQUE |
|---|---|---|
| venue + instrument | `destination_symbols` | `(venue_id, instrument_id)` |
| venue + latest quote | `destination_quotes` | `(venue_id, instrument_id)` |
| venue + session | `fix_sessions` | `(venue_id, session_qualifier)` |
| client order id | `execution_intents`, `fix_orders` | `cl_ord_id` |
| venue + ExecID | `fix_execution_reports` | `(venue_id, exec_id)` |
| venue + account + position | `destination_positions` | `(venue_id, destination_account, destination_position_id)` |

---

## 7. Suggested foreign keys (identity-preserving)

Prefer FKs on the **compound natural key** for source entities so a join cannot cross brokers.

```text
broker_connections.broker_id                         → brokers.id
mt5_groups.broker_id                                 → brokers.id
plan_group_mappings.(broker_id, group_name)          → mt5_groups.(broker_id, group_name)
mt5_accounts.broker_id                               → brokers.id
mt5_account_snapshots.(broker_id, login)             → mt5_accounts.(broker_id, login)
mt5_orders.(broker_id, login)                        → mt5_accounts.(broker_id, login)
mt5_deals.(broker_id, login)                         → mt5_accounts.(broker_id, login)
mt5_positions_current.(broker_id, login)             → mt5_accounts.(broker_id, login)
mt5_symbols.broker_id                                → brokers.id
mt5_xau_ticks.broker_id                              → brokers.id
ingestion_events.broker_id                           → brokers.id

reconstructed_trades.(broker_id, login)              → mt5_accounts.(broker_id, login)
reconstructed_trades.canonical_symbol                → canonical_instruments.canonical_symbol
source_symbol_mappings.broker_id                     → brokers.id
source_symbol_mappings.canonical_symbol              → canonical_instruments.canonical_symbol

trader_states.(broker_id, login)                     → mt5_accounts.(broker_id, login)
trader_scores.(broker_id, login)                     → mt5_accounts.(broker_id, login)
trader_score_history.(broker_id, login)              → mt5_accounts.(broker_id, login)
trader_feature_snapshots.(broker_id, login)          → mt5_accounts.(broker_id, login)
trader_risk_flags.(broker_id, login)                 → mt5_accounts.(broker_id, login)
model_predictions.model_version_id                   → model_versions.id
model_predictions.(broker_id, login)                 → mt5_accounts.(broker_id, login)
model_evaluations.model_version_id                   → model_versions.id

copy_intents.source_trade_id                         → reconstructed_trades.id
copy_allocations.copy_intent_id                      → copy_intents.id
risk_decisions.copy_intent_id                        → copy_intents.id
execution_intents.copy_intent_id                     → copy_intents.id
execution_intents.risk_decision_id                   → risk_decisions.id
execution_intents.source_trade_id                    → reconstructed_trades.id

shadow_orders.copy_intent_id                         → copy_intents.id
shadow_fills.shadow_order_id                         → shadow_orders.id
shadow_positions.source_trade_id                     → reconstructed_trades.id

destination_symbols.venue_id                         → execution_venues.id
destination_quotes.(venue_id, instrument_id)         → destination_symbols.(venue_id, instrument_id)
fix_sessions.venue_id                                → execution_venues.id
fix_session_events.session_id                        → fix_sessions.id
fix_orders.venue_id                                  → execution_venues.id
fix_orders.cl_ord_id                                 → execution_intents.cl_ord_id
fix_execution_reports.venue_id                       → execution_venues.id
destination_positions.venue_id                       → execution_venues.id
source_destination_links.source_trade_id             → reconstructed_trades.id
source_destination_links.execution_intent_id         → execution_intents.id
execution_reconciliation_issues.run_id               → execution_reconciliation_runs.id
```

Composite FKs require matching UNIQUE constraints on the parent (already specified above).

---

## 8. What must stay non-unique

| Candidate people will wrongly unique | Why not |
|---|---|
| `login` | Collides across brokers (§10) |
| `deal_ticket` / `order_ticket` / `position_id` | Collides across brokers (§10) |
| `source_symbol` | `XAUUSD` / `XAUUSD.` / `GOLD` are broker-local (§16) |
| `mt5_deals (broker_id, order_ticket)` | One order → many deals |
| `mt5_deals (broker_id, position_id)` | Many deals per position lifecycle |
| `plan_group_mappings (broker_id, group_name)` | Several plans share one group (§9) |
| `source_destination_links (source_trade_id)` | Scale-in / partial close / reversal (§35) |
| `fix_execution_reports (cl_ord_id)` | Many ERs per order (acks, partials, fills) |
| `copy_intents (source_trade_id)` | Separate OPEN vs CLOSE vs scale events |
| Tag 55 / venue symbol globally | Instrument IDs are venue-issued (§16, §74) |
| Shared FIX sequence across QUOTE+TRADE | Forbidden (§27) |

---

## 9. Open questions (do not invent a second table)

1. **Position-ticket reuse after close.** Default UNIQUE `(broker_id, position_id)` on `reconstructed_trades` assumes MT5-like non-reuse. If a future broker reuses tickets, widen to `(broker_id, position_id, opened_at)` **in a later versioned migration**. Do not pre-emptively weaken the §10 key.
2. **`destination_quotes` history.** Not in §45. Keep this table as latest-only. A history table would be a new catalog entry.
3. **`trader_risk_flags` current vs history.** Catalog treats it as **current**. Historical raises go to `risk_events`.
4. **`sync_checkpoints` for venues.** Same table, `scope_type='VENUE'`. Do not create `fix_checkpoints`.
5. **Tick uniqueness.** SDK ticks expose `time_msc` + `flags` without a ticket. `ingest_seq` is the suggested tie-break; it is collector-local, not a broker ticket.

---

## 10. Coverage checklist

| Origin | Tables required | In this catalog |
|---|---|---|
| §11 raw list | 10 names (8 entities + 2 aliases) | Yes; aliases folded; `ingestion_events` kept |
| §44 execution list | 14 | Yes; including 3 not repeated in §45 |
| §45 full initial set | 43 names | Yes |
| §10 compound identity | 4 laws + `broker_id` on all source tables | Applied in §4–§6 |
| Union | **47** physical tables | **47** |

No product source was modified. This file is the A20 catalog only.
