# D19 — `TraderDbContext` tables vs architecture §45

| Field | Value |
|---|---|
| Agent | D19 (senior engineer, DbContext ↔ §45 table census, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only pass; product source not edited) |
| Artifact | `D:\Prop\reports\swarm\20260818\D19_dbcontext.md` |
| Workspace | `D:\Prop` |
| Subject | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§45** (lines 1672–1731) “Recommended Core Database Tables”; identity **§10**; raw layer **§11**; outbox **§12–§13**; execution extras **§44** |
| Key catalog | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (union **47**; §45 = **43** names) |
| Target EF contract | `D:\Prop\reports\swarm\20260818\A61_efcore_schema.md` (not implemented) |
| Prior census | B19 (table gap), B21 (type pairing), B33 (Domain ↔ §45), C06 (compound keys), C29 (migrations), C60 (`mt5_xau_ticks`) |
| ML hold | A52 / A104 / C44 — `model_*` stay unbuilt as **services** until Phase 6; they remain §45 schema gaps |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict

**FAIL — 18 / 43 §45 tables exist as `DbSet` + inline `ToTable`. 25 §45 tables are absent. Coverage is 41.9% by table-name presence and 0% by A20/A61 completeness.**

This is a D-wave **re-measure** of the same file B19 already scored. The counts have **not** moved. `TraderDbContext` is still a **demo skeleton**: 20 `DbSet`s, 20 inline fluent maps, 0 split configurations, 0 named UNIQUEs, 0 foreign keys, 0 versioned migrations.

| Metric | Count | Note |
|---|---:|---|
| §45 full initial set | **43** | Architecture lines 1677–1730, counted from the law file |
| `DbSet<T>` properties | **20** | `TraderDbContext.cs` lines 12–31 |
| `ToTable(...)` maps | **20** | `OnModelCreating` lines 35–172 |
| §45 tables present by **name** | **18** | §3 PRESENT |
| §45 tables **missing** | **25** | §4 MISSING |
| Extra vs §45 (mapped anyway) | **2** | `execution_intents` (§44, keep); `kill_switches` (not §45) |
| Domain entity files | **20** | one type per file; all 20 have a `DbSet` |
| `IEntityTypeConfiguration<T>` | **0** | `Persistence\Configurations\` is empty |
| Named A20 UNIQUE (`HasDatabaseName`) | **0** | |
| `HasOne` / `HasForeignKey` | **0** | |
| Checked-in EF `Migrations/` | **0** | C29 still holds; hosts call `EnsureCreatedAsync` |
| Table-name coverage | **18/43 = 41.9%** | column/index/FK completeness is far lower |

Honest class of the current model:

| Item | Class |
|---|---|
| `TraderDbContext` as a compile-time store for the InMemory demo path | `EXISTS_NEEDS_REFACTOR` |
| §45-complete PostgreSQL model | `MISSING` |
| Split configuration classes (A61) | `MISSING` |
| Versioned migrations (§72.3) | `MISSING` |
| `model_*` tables | `MISSING` — **correct** under A52 Phase 6 hold; still a §45 gap |
| `kill_switches` table | Extra; A20 derives current mode from `system_events` |
| Default DI (empty / `<SECRET>` CS → InMemory) | `UNSAFE` as a production store |

Do **not** treat 18 named tables as an 18-table schema. Without snake_case convention, named UNIQUEs, FKs, or migrations, this is a LINQ surface over InMemory (or a PascalCase `EnsureCreated` dump if someone points Npgsql at an empty Postgres).

---

## 1. What was read (no product edits)

| Path | Role | Measured |
|---|---|---|
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | subject | **174** lines (file), **151** non-blank; **5951** bytes; SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | A61 split maps | **0** files |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | consumer | LINQ upserts by `(BrokerId, …)`, not `ON CONFLICT` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | provider | SHA-256 `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380`; InMemory or bare `UseNpgsql` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | packages | EF Design **8.0.4**, InMemory **8.0.4**, Npgsql.EF **8.0.4**, Redis **2.8.0**. **No** `EFCore.NamingConventions` |
| `D:\Prop\src\Domain\Entities\*.cs` | persist types | **20** files / **20** `public sealed class` |
| `D:\Prop\apps\api\Program.cs`, `apps\mt5-worker\Program.cs`, `apps\fix-worker\Program.cs` | schema apply | each calls `EnsureCreatedAsync()`; **0** `Migrate` / `MigrateAsync` |
| Architecture §45 | law | quoted in §2 |
| A20 / A61 | UNIQUE + column contract | used when judging “present but wrong” |

Grep under `D:\Prop\src` for `IEntityTypeConfiguration`, `ApplyConfigurationsFromAssembly`, `HasDatabaseName`, `HasForeignKey`, `UseSnakeCase`, `MigrationsAssembly`: **0 hits**.

No `Migrations/` directory under `src/`, `apps/`, or `tests/` (excluding `bin`/`obj`).

---

## 2. Architecture §45 — full initial set (verbatim)

Source: `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1672–1731.

```text
brokers
broker_connections
mt5_groups
plan_group_mappings

mt5_accounts
mt5_account_snapshots
mt5_orders
mt5_deals
mt5_positions_current
mt5_symbols
mt5_xau_ticks

reconstructed_trades
canonical_instruments
source_symbol_mappings

trader_feature_snapshots
trader_scores
trader_score_history
trader_states
trader_risk_flags

model_versions
model_predictions
model_evaluations

shadow_orders
shadow_fills
shadow_positions
shadow_performance

copy_intents
copy_allocations

risk_decisions
risk_events

execution_venues
destination_symbols
destination_quotes

fix_sessions
fix_session_events
fix_orders
fix_execution_reports
destination_positions

source_destination_links

sync_checkpoints
outbox_events
audit_logs
system_events
```

**43 names.** Counted from the architecture file, not from A20.

Aliases that must **not** be extra tables (A20 §1):

| Alias (other sections) | Canonical §45 name |
|---|---|
| `mt5_symbol_metadata` (§11) | `mt5_symbols` |
| `mt5_ticks_xauusd` (§11) | `mt5_xau_ticks` |
| `shadow_copy_order` / `shadow_copy_fill` / `shadow_position` (§24) | `shadow_orders` / `shadow_fills` / `shadow_positions` |
| `shadow_pnl` / `source_vs_shadow_slippage` (§24) | `shadow_performance` |

§44 / §11 names **not** repeated in §45 (still required before live FIX / durable ingest; A20 extras): `execution_intents`, `execution_reconciliation_runs`, `execution_reconciliation_issues`, `ingestion_events`.

---

## 3. Master matrix — every §45 table vs `TraderDbContext`

Legend: **P** = present (`DbSet` + `ToTable` exact name). **M** = missing. **X** = extra (not in §45). Identity quality is A20 UNIQUE, not “a Guid PK exists.”

| # | §45 table | `DbSet` | CLR type | Status | Identity configured | A20 UNIQUE | Quality |
|---|---|---|---|---|---|---|---|
| 1 | `brokers` | `Brokers` | `Broker` | **P** | `Code` unique | `brokers_code_uk (code)` | Partial. Host/port/manager login/proxy live **on this row** (belongs on `broker_connections`). No `kind`. UK unnamed. |
| 2 | `broker_connections` | — | — | **M** | — | `(broker_id, connection_name)` | Secrets-free connection profile does not exist. |
| 3 | `mt5_groups` | `Mt5Groups` | `Mt5Group` | **P** | `(BrokerId, Name)` unique | `mt5_groups_broker_name_uk (broker_id, group_name)` | Close. Column is `Name` not `group_name`. `PlanMapping` string on the group (overlay belongs in `plan_group_mappings`). |
| 4 | `plan_group_mappings` | — | — | **M** | — | `(broker_id, plan_code, environment)` | Overlay table missing. |
| 5 | `mt5_accounts` | `Mt5Accounts` | `Mt5Account` | **P** | `(BrokerId, Login)` unique | **`mt5_accounts_identity_uk`** | **§10 key is correct.** Latest snapshot denormalized on the row. No `name`/`currency`/`is_enabled`. |
| 6 | `mt5_account_snapshots` | — | — | **M** | — | `(broker_id, login, snapshot_at)` | No append-only equity tape. |
| 7 | `mt5_orders` | — | — | **M** | — | **`(broker_id, order_ticket)`** | §10 order identity has no table. |
| 8 | `mt5_deals` | `Mt5Deals` | `Mt5Deal` | **P** | `(BrokerId, DealTicket)` unique; `(BrokerId, Login, DealTime)` lookup | **`mt5_deals_identity_uk`** | **§10 deal key is correct.** `VolumeNative` is `ulong` (no bigint converter). Enums default **int**. No `time_msc` / `payload`. |
| 9 | `mt5_positions_current` | `Mt5Positions` | `Mt5Position` | **P** | `(BrokerId, PositionTicket)` unique | **`(broker_id, position_id)`** | Table name good. Property is `PositionTicket` not `PositionId`. No `xmin`. |
| 10 | `mt5_symbols` | — | — | **M** | — | `(broker_id, source_symbol)` | Per-broker contract metadata missing. |
| 11 | `mt5_xau_ticks` | — | — | **M** | — | `(broker_id, source_symbol, time_msc, flags, ingest_seq)` | Exact MFE tape missing (C60). |
| 12 | `reconstructed_trades` | `ReconstructedTrades` | `ReconstructedTrade` | **P** | **non-unique** `(BrokerId, Login, PositionId, OpenedAt)` | **`(broker_id, position_id)`** | **Identity FAIL.** A20 requires UNIQUE `(broker_id, position_id)`. Volumes are lots `decimal`, not native `bigint`. `Id` is regenerated on replace. |
| 13 | `canonical_instruments` | `CanonicalInstruments` | `CanonicalInstrument` | **P** | `Code` unique | `canonical_instruments_symbol_uk (canonical_symbol)` | Property `Code` vs `canonical_symbol`. Only 3 columns. |
| 14 | `source_symbol_mappings` | `SourceSymbolMappings` | `SourceSymbolMapping` | **P** | `(BrokerId, SourceSymbol)` unique | `source_symbol_mappings_uk` | Unique shape good. Maps to GUID, not `canonical_symbol` text + FK. |
| 15 | `trader_feature_snapshots` | — | — | **M** | — | `(broker_id, login, completed_trade_count, feature_schema_version)` | Leakage-safe features missing. |
| 16 | `trader_scores` | `TraderScores` | `TraderScore` | **P** | `(BrokerId, Login)` unique | `(broker_id, login, score_kind)` or collapsed wide row | Wide current row allowed. **Also stores** `CurrentState` + martingale/averaging/lot flags (`trader_states` + `trader_risk_flags`). No `score_kind`. |
| 17 | `trader_score_history` | `TraderScoreHistory` | `TraderScoreHistory` | **P** | **non-unique** `(BrokerId, Login, RecordedAt)` | `(broker_id, login, completed_trade_count, score_kind, model_version_id)` | Append exists; identity is time, not trade-count + kind. |
| 18 | `trader_states` | — | folded into `TraderScore.CurrentState` | **M** | — | **`(broker_id, login)`** | No current-state table. |
| 19 | `trader_risk_flags` | — | folded into `TraderScore` bools | **M** | — | `(broker_id, login, flag_code)` | No flag rows. |
| 20 | `model_versions` | — | — | **M** | — | `(model_name, version)` | Phase 6 hold (A52). Still a §45 gap. |
| 21 | `model_predictions` | — | — | **M** | — | `(model_version_id, broker_id, login, completed_trade_count)` | Same hold. |
| 22 | `model_evaluations` | — | — | **M** | — | `(model_version_id, evaluation_split, metric_set_version)` | Same hold. |
| 23 | `shadow_orders` | `ShadowOrders` | `ShadowOrder` | **P** | PK only | `shadow_cl_ord_id` | **No unique business key.** Flattened fill (qty/price/slippage) — fills belong in `shadow_fills`. |
| 24 | `shadow_fills` | — | — | **M** | — | `(shadow_order_id, fill_seq)` | |
| 25 | `shadow_positions` | — | — | **M** | — | `(source_broker_id, source_trade_id)` | Engine DTO in `Domain.Shadow` is **not** this table. |
| 26 | `shadow_performance` | — | flattened onto `ShadowOrder.SourceVsShadowSlippage` | **M** | — | `(source_broker_id, login, period_grain, period_start)` | |
| 27 | `copy_intents` | `CopyIntents` | `CopyIntent` | **P** | `IdempotencyKey` unique | `(source_broker_id, source_login, source_trade_id, source_event_id, action)` | Opaque string UK is not the A20 natural key. No `source_event_id`. Status is free `string`. |
| 28 | `copy_allocations` | — | — | **M** | — | `(copy_intent_id, destination_account)` | Sizing result table missing. |
| 29 | `risk_decisions` | `RiskDecisions` | `RiskDecisionRecord` | **P** | **non-unique** `CopyIntentId` | `(copy_intent_id, decision_seq)` | Can store many rows; cannot upsert by seq. No quote-age/spread / `is_final`. |
| 30 | `risk_events` | — | — | **M** | — | none (append stream) | Distinct from `risk_decisions`. |
| 31 | `execution_venues` | — | — | **M** | — | `venue_code` | Root for all FIX uniqueness. Absence is why `fix_sessions` unique is global. |
| 32 | `destination_symbols` | — | — | **M** | — | `(venue_id, instrument_id)` | Security List missing. |
| 33 | `destination_quotes` | `DestinationQuotes` | `DestinationQuoteSnapshot` | **P** | PK only | `(venue_id, instrument_id)` | **No upsert key.** No `venue_id`. Latest-quote-per-instrument cannot be enforced. |
| 34 | `fix_sessions` | `FixSessionStates` | `FixSessionState` | **P** | `Qualifier` unique **globally** | `(venue_id, session_qualifier)` | **Wrong uniqueness.** One global QUOTE and one global TRADE forever. Qualifier stored as int enum. |
| 35 | `fix_session_events` | — | — | **M** | — | none | Logon/logout/resend/leadership stream missing. |
| 36 | `fix_orders` | — | — | **M** | — | `cl_ord_id`; partial `(venue_id, dest_order_id)` | Venue-visible order missing. |
| 37 | `fix_execution_reports` | — | — | **M** | — | `(venue_id, exec_id)` | Durable ER missing. |
| 38 | `destination_positions` | — | — | **M** | — | `(venue_id, destination_account, destination_position_id)` | |
| 39 | `source_destination_links` | — | — | **M** | — | `(source_broker_id, source_trade_id, link_role, execution_intent_id)` | Scale-in / partial / reversal map missing. |
| 40 | `sync_checkpoints` | `SyncCheckpoints` | `SyncCheckpoint` | **P** | `(BrokerId, Login, Stream)` unique | `(scope_type, scope_id, stream_name)` | Too narrow. Forces a **login** on every cursor. Cannot represent broker-level, venue, or global streams. |
| 41 | `outbox_events` | `OutboxEvents` | `OutboxEvent` | **P** | **non-unique** `ProcessedAt` only | `(aggregate_type, aggregate_id, event_type, dedupe_key)` | **Outbox FAIL.** No `AggregateType`, no `DedupeKey`, no `jsonb`, no `broker_id`. Cannot implement §12 same-commit idempotent insert. |
| 42 | `audit_logs` | `AuditLogs` | `AuditLog` | **P** | PK only | `id bigint IDENTITY`; no UNIQUE | PK is `Guid`, not identity `bigint`. No `(occurred_at)` / actor indexes. |
| 43 | `system_events` | — | — | **M** | — | none | Platform health / kill-switch **events**. A20: current mode is derived from this stream. |

**Present 18 / Missing 25 / Extra 2 (below).**

---

## 4. Missing §45 tables (25) — why they matter

| # | Table | Layer | Domain type today | Why it matters |
|---|---|---|---|---|
| 1 | `broker_connections` | registry | **none** | Host/port/manager login **number** / pool / proxy flags. Passwords stay in secrets. Today those non-secret fields sit on `brokers`. |
| 2 | `plan_group_mappings` | registry | **none** | Overlay only (`broker_id, plan_code, environment`). `Mt5Group.PlanMapping` is not this table. |
| 3 | `mt5_account_snapshots` | raw | **none** | Append-only balance/equity. `mt5_accounts` denormalizes latest only. |
| 4 | `mt5_orders` | raw | **none** | §10 `broker_id + order_ticket`. Deals cannot reconstruct order state alone. |
| 5 | `mt5_symbols` | raw | **none** | Per-broker contract metadata. Alias of §11 `mt5_symbol_metadata`. |
| 6 | `mt5_xau_ticks` | raw | **none** | Optional exact MFE/MAE tape. Alias of §11 `mt5_ticks_xauusd`. C60: MFE unavailable. |
| 7 | `trader_feature_snapshots` | scoring | **none** | Leakage-safe as-of features. Required before any ML (A52). |
| 8 | `trader_states` | scoring | folded into `TraderScore.CurrentState` | One current state per `(broker_id, login)`. |
| 9 | `trader_risk_flags` | scoring | folded into `TraderScore` bools | `(broker_id, login, flag_code)`. |
| 10 | `model_versions` | ML | **none** | §45 name. **Do not add empty tables to look ready** (A52 / A104). Still a §45 gap. |
| 11 | `model_predictions` | ML | **none** | Same hold. |
| 12 | `model_evaluations` | ML | **none** | Same hold. |
| 13 | `shadow_fills` | shadow | **none** | `(shadow_order_id, fill_seq)`. |
| 14 | `shadow_positions` | shadow | **none** | `(source_broker_id, source_trade_id)`. |
| 15 | `shadow_performance` | shadow | flattened onto `ShadowOrder.SourceVsShadowSlippage` | Grain `LIFETIME`/`DAY`/`TRADE`. |
| 16 | `copy_allocations` | copy | **none** | `(copy_intent_id, destination_account)` sizing result. |
| 17 | `risk_events` | risk | **none** | Append-only kill/pause/flatten stream. Distinct from `risk_decisions`. |
| 18 | `execution_venues` | dest | **none** | `venue_id` root for all FIX tables. Absence is why `fix_sessions` unique is global. |
| 19 | `destination_symbols` | dest | **none** | Security List `(venue_id, instrument_id)`. |
| 20 | `fix_session_events` | dest | **none** | Logon/logout/resend/leadership. |
| 21 | `fix_orders` | dest | **none** | Venue-visible order; unique `cl_ord_id`. |
| 22 | `fix_execution_reports` | dest | **none** | Durable ER; unique `(venue_id, exec_id)`. |
| 23 | `destination_positions` | dest | **none** | `(venue_id, destination_account, destination_position_id)`. |
| 24 | `source_destination_links` | dest | **none** | Scale-in / partial / reversal mapping. |
| 25 | `system_events` | ops | **none** | Platform health / kill-switch **events**. A20: current mode is derived from this stream, not a second table. |

---

## 5. Extra tables (mapped, not in the §45 list)

| Table | `DbSet` | Origin | Keep? |
|---|---|---|---|
| `execution_intents` | `ExecutionIntents` | §44 / §33 / A20 #37 | **Yes, required** before any FIX send. Unique on `ClOrdId` is the one mapping that already matches A20. Still missing `source_event_id`, venue, status-as-text tokens, `execution_intents_risk_uk`. |
| `kill_switches` | `KillSwitches` | Product-only; A48 sketches a singleton; **not** in §45 | **Do not treat as §45 complete.** A20 §5.8: current kill-switch state comes from latest `system_events` (`STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN`). A second current-state table is optional later (`system_flags`), not this name. |

Not present and not in §45 (A20 extras, still missing): `ingestion_events`, `execution_reconciliation_runs`, `execution_reconciliation_issues`.

A20 union score if counted: **19 / 47** (`18` §45 + `execution_intents`). `kill_switches` is outside the union.

---

## 6. Measured `DbSet` → `ToTable` inventory (all 20)

From `TraderDbContext.cs` lines 12–31 and 35–172.

| # | `DbSet` property | CLR `T` | `ToTable` | In §45? | Unique / index actually configured |
|---|---|---|---|---|---|
| 1 | `Brokers` | `Broker` | `brokers` | Yes | `Code` unique; `Code`/`DisplayName` max 32/128 |
| 2 | `Mt5Groups` | `Mt5Group` | `mt5_groups` | Yes | `(BrokerId, Name)` unique |
| 3 | `Mt5Accounts` | `Mt5Account` | `mt5_accounts` | Yes | `(BrokerId, Login)` unique |
| 4 | `Mt5Deals` | `Mt5Deal` | `mt5_deals` | Yes | `(BrokerId, DealTicket)` unique; `(BrokerId, Login, DealTime)` non-unique |
| 5 | `Mt5Positions` | `Mt5Position` | `mt5_positions_current` | Yes | `(BrokerId, PositionTicket)` unique |
| 6 | `ReconstructedTrades` | `ReconstructedTrade` | `reconstructed_trades` | Yes | **non-unique** `(BrokerId, Login, PositionId, OpenedAt)` |
| 7 | `CanonicalInstruments` | `CanonicalInstrument` | `canonical_instruments` | Yes | `Code` unique |
| 8 | `SourceSymbolMappings` | `SourceSymbolMapping` | `source_symbol_mappings` | Yes | `(BrokerId, SourceSymbol)` unique |
| 9 | `TraderScores` | `TraderScore` | `trader_scores` | Yes | `(BrokerId, Login)` unique |
| 10 | `TraderScoreHistory` | `TraderScoreHistory` | `trader_score_history` | Yes | **non-unique** `(BrokerId, Login, RecordedAt)` |
| 11 | `OutboxEvents` | `OutboxEvent` | `outbox_events` | Yes | **non-unique** `ProcessedAt` |
| 12 | `SyncCheckpoints` | `SyncCheckpoint` | `sync_checkpoints` | Yes | `(BrokerId, Login, Stream)` unique |
| 13 | `CopyIntents` | `CopyIntent` | `copy_intents` | Yes | `IdempotencyKey` unique |
| 14 | `RiskDecisions` | `RiskDecisionRecord` | `risk_decisions` | Yes | **non-unique** `CopyIntentId` |
| 15 | `ExecutionIntents` | `ExecutionIntent` | `execution_intents` | **No** (§44) | `ClOrdId` unique |
| 16 | `ShadowOrders` | `ShadowOrder` | `shadow_orders` | Yes | PK only |
| 17 | `DestinationQuotes` | `DestinationQuoteSnapshot` | `destination_quotes` | Yes | PK only |
| 18 | `FixSessionStates` | `FixSessionState` | `fix_sessions` | Yes | `Qualifier` unique globally |
| 19 | `AuditLogs` | `AuditLog` | `audit_logs` | Yes | PK only |
| 20 | `KillSwitches` | `KillSwitch` | `kill_switches` | **No** | PK only |

Every entity uses surrogate `HasKey(x => x.Id)` (`Guid`). There is **zero** composite PK, **zero** `HasAlternateKey`, **zero** `HasDatabaseName`. That PK shape matches A61; the missing piece is the **named** natural UNIQUE.

---

## 7. §10 compound-identity vs this model

Architecture §10 (lines 479–496): never treat login or ticket IDs as globally unique. Required compounds:

```text
broker_id + login
broker_id + deal_ticket
broker_id + order_ticket
broker_id + position_id
```

| Law | Required table / UNIQUE | In `TraderDbContext` |
|---|---|---|
| `broker_id + login` | `mt5_accounts` | **Yes** `(BrokerId, Login)` |
| `broker_id + login` | `trader_states` | **No table** |
| `broker_id + deal_ticket` | `mt5_deals` | **Yes** `(BrokerId, DealTicket)` |
| `broker_id + order_ticket` | `mt5_orders` | **No table** |
| `broker_id + position_id` | `mt5_positions_current` | **Approximate** `(BrokerId, PositionTicket)` |
| `broker_id + position_id` | `reconstructed_trades` | **No** — index is non-unique and includes `OpenedAt` |

No source table globally-uniques `Login` / `DealTicket` / `PositionTicket` alone. That part is clean.

Destination uniqueness is **not** venue-scoped: `fix_sessions.Qualifier` is globally unique; `destination_quotes` has no `(venue_id, instrument_id)`.

---

## 8. Mapping conventions that are still absent

All 20 maps live inside `OnModelCreating` (lines 33–173). Pattern:

```csharp
modelBuilder.Entity<T>(e =>
{
    e.ToTable("...");
    e.HasKey(x => x.Id);
    e.HasIndex(...);   // sometimes .IsUnique()
    // Broker only: HasMaxLength on Code / DisplayName
});
```

| A61 convention | Present? |
|---|---|
| `HasDefaultSchema("public")` | No |
| `ApplyConfigurationsFromAssembly` | No |
| `UseSnakeCaseNamingConvention()` | No (package not referenced) |
| `HasDatabaseName` on indexes / FKs / checks | No |
| `HasColumnName` / `HasColumnType` | No (except implicit) |
| `HasDefaultValueSql("gen_random_uuid()")` | No — store assigns `Guid.NewGuid()` |
| `timestamptz` / `numeric(20,8)` / `jsonb` | No |
| `ulong` → `bigint` converter | No |
| Enum → architecture **text** tokens | No — EF default **integer** |
| `HasCheckConstraint` | No |
| `xmin` concurrency on live-book tables | No |
| Compound FKs (`HasPrincipalKey`) | No |
| Partial UNIQUE (`HasFilter`) | No |
| `IDesignTimeDbContextFactory` | No |
| `ConfigureConventions` | No |
| `MigrationsAssembly` / `Database.Migrate` | No |

Without a naming convention, Npgsql will persist **PascalCase** column names (`DealTicket`, not `deal_ticket`) unless a later convention is added. `ToTable` is the only snake_case that is guaranteed today.

DI (`DependencyInjection.cs` 19–29): empty/`<SECRET>` connection string → **InMemory** `trader-intelligence`. Real `ConnectionStrings:TraderIntelligence` → `UseNpgsql` with **no** retry, **no** snake_case, **no** migrations assembly. Three hosts then call `EnsureCreatedAsync()`. There is nothing to migrate.

---

## 9. Enum persistence (will not match architecture tokens)

Unconfigured enums persist as **integers**. Architecture / A61 want **text** tokens (except raw deal `action`/`entry`, which should be SDK `smallint`).

| CLR enum | Stored today (default) | Required token examples |
|---|---|---|
| `TraderState` | 0..n | `INSUFFICIENT_DATA`, `WATCH`, `SHADOW`, … |
| `OutboxEventType` | int | `TradeCompleted`, `ScoreUpdate`, … |
| `CopyIntentAction` | int | `OPEN_EXPOSURE`, … (CLR is `OpenExposure`) |
| `RiskDecisionOutcome` | int | `approve` / `reduce_size` / … (CLR is `Approve`) |
| `FixSessionQualifier` | int | `QUOTE` / `TRADE` (CLR is `Quote` / `Trade`) |
| `ExecutionOrderStatus` | int | `not_sent` / `EXECUTION_STATE_UNKNOWN` / … |
| `DealAction` / `DealEntry` | underlying integer | **Keep numeric** (`smallint`); do not stringify as the only store |
| `KillSwitchMode` | integer | `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` |
| `TradeDirection` | integer | architecture side tokens |

A PostgreSQL dump of today’s model cannot be compared to architecture check constraints.

---

## 10. §44 overlay (so the gap is not under-counted)

| §44 table | In DbContext? |
|---|---|
| `execution_venues` | No |
| `fix_sessions` | Yes (wrong unique) |
| `fix_session_events` | No |
| `destination_symbols` | No |
| `destination_quotes` | Yes (no UK) |
| `copy_intents` | Yes |
| `risk_decisions` | Yes |
| `execution_intents` | Yes (`ClOrdId` unique — best FIX-related map) |
| `fix_orders` | No |
| `fix_execution_reports` | No |
| `destination_positions` | No |
| `source_destination_links` | No |
| `execution_reconciliation_runs` | No |
| `execution_reconciliation_issues` | No |

**§44 score: 5 / 14** by name (`fix_sessions`, `destination_quotes`, `copy_intents`, `risk_decisions`, `execution_intents`).

---

## 11. Layer fold / do-not-count

Do **not** treat these as closing a missing §45 table:

| Looks like | Actually | Missing table it does **not** close |
|---|---|---|
| `Broker.Server` / `Port` / `ManagerLogin` / `Proxy*` | connection fields crammed onto `brokers` | `broker_connections` |
| `Mt5Group.PlanMapping` | free string on the group | `plan_group_mappings` |
| `Mt5Account.Balance` / `Equity` | latest denormalized | `mt5_account_snapshots` |
| `TraderScore.CurrentState` | enum column | `trader_states` |
| `TraderScore.Martingale` / `AveragingDown` / `LotEscalation` | three bools | `trader_risk_flags` |
| `ShadowOrder.SourceVsShadowSlippage` + fill fields | flattened fill | `shadow_fills`, `shadow_performance` |
| `KillSwitch` singleton | product extra | `system_events` |
| `Domain.Shadow.ShadowFill` / `ShadowPosition` | engine DTOs, not Entities | `shadow_fills`, `shadow_positions` |
| `Domain.Scoring.FeatureSnapshot` | scorer DTO | `trader_feature_snapshots` |

---

## 12. Recensus vs B19 / B33 / C06 / C29

| Claim from earlier report | D19 re-measure |
|---|---|
| B19: 18/43 present, 25 missing, 2 extra | **Unchanged** |
| B21: every `DbSet<T>` has a Domain type | **Unchanged** (20/20) |
| B33: 18 persist types for §45, 25 types missing | **Unchanged** |
| C06: no composite PKs; 7 compound unique indexes | **Unchanged** (groups, accounts, deals, positions, mappings, scores, checkpoints) |
| C29: 0 migrations; 3× `EnsureCreatedAsync` | **Unchanged** |
| C60: `mt5_xau_ticks` missing | **Unchanged** |
| A61 target `TraderIntelligenceDbContext` + 43 `*Configuration.cs` | **Still not implemented** |

B19 is **not stale**. D19 confirms the same skeleton with a file hash so later waves can detect drift.

---

## 13. What “done” would look like (not implemented here)

Product source was **not** edited. Binding next increment (A61 + C47.1), when someone is assigned to implement:

1. Add Domain types for the 25 missing §45 tables **first** (except do not fake-ready `model_*` services; schema-only types are allowed later under A52).
2. Replace inline `OnModelCreating` with `IEntityTypeConfiguration<T>` + `ApplyConfigurationsFromAssembly`.
3. Pin A20 UNIQUE names via `HasDatabaseName`, add §10 FKs, snake_case convention, `ulong`→`bigint`, enum **text** tokens (except deal action/entry).
4. Emit a **new versioned** EF migration. Never overwrite. Never `EnsureCreated` in a host that talks to shared Postgres.
5. Keep `execution_intents`. Do not promote `kill_switches` as the §45 kill-switch store; prefer `system_events` as the authority.

---

## 14. Scoreboard (honest)

| Gate | Score | Class |
|---|---|---|
| §45 table **names** mapped | **18 / 43** | `EXISTS_NEEDS_REFACTOR` (name only) |
| §45 tables with A20 UNIQUE + columns | **0 / 43** | `MISSING` as a complete map |
| §45 tables absent | **25 / 43** | `MISSING` |
| Extra tables | **2** | 1 required (§44), 1 optional/wrong name |
| A20 union tables mapped | **19 / 47** | `execution_intents` counted |
| Split configurations | **0 / 43** | `MISSING` |
| Versioned migrations | **0 / 15** (A30 `0001`–`0015`) | `MISSING` |
| §10 identities that exist and match | **2** (`mt5_accounts`, `mt5_deals`) | others missing or wrong |
| Default store | InMemory + `EnsureCreated` | `UNSAFE` for shared Postgres |

**Do not claim a §45 schema.** `TraderDbContext` is a 20-set demo surface covering 18 of 43 recommended core table **names**. Measured SHA-256 of the file is `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`.
