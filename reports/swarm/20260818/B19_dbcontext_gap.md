# B19 — `TraderDbContext` vs architecture §45 table gap

| Field | Value |
|---|---|
| Agent | B19 (senior engineer, DbContext / configuration gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only pass) |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§45** (lines 1672–1731), identity **§10**, raw layer **§11**, outbox **§12–§13**, execution extras **§44** |
| Key catalog | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (47-table union; §45 = 43 names) |
| Target EF contract | `D:\Prop\reports\swarm\20260818\A61_efcore_schema.md` (not implemented) |
| ML hold | `D:\Prop\reports\swarm\20260818\A52_ml_not_yet.md` — `model_*` stay missing on purpose until Phase 6 |

---

## 0. Verdict

**FAIL — 18 / 43 §45 tables exist as `DbSet` + inline fluent maps. 25 §45 tables are absent. `Persistence/Configurations` is empty. There is no `IEntityTypeConfiguration<T>`, no `ApplyConfigurationsFromAssembly`, no versioned EF migration, no foreign key, no named UNIQUE from A20.**

Do not treat the current model as a §45 schema. It is a **skeleton**: table names for a subset of the catalog, keys that are only approximately §10, columns that are Domain property defaults (PascalCase unless a naming convention is registered — it is not).

| Metric | Count | Note |
|---|---:|---|
| §45 full initial set | **43** | Architecture lines 1677–1730 |
| Present in `TraderDbContext` (name match) | **18** | See §3 |
| Missing vs §45 | **25** | See §4 |
| Extra vs §45 | **2** | `execution_intents` (§44, not §45); `kill_switches` (not §45) |
| `IEntityTypeConfiguration<T>` files | **0** | Folder exists, empty |
| Named A20 UNIQUE constraints (`HasDatabaseName`) | **0** | |
| `HasOne` / `HasForeignKey` | **0** | |
| Checked-in EF `Migrations/` | **0** | grep `Migrations` in `D:\Prop\src\**\*.cs` = none |
| Coverage | **18/43 = 41.9%** table-name presence only | Column/index/FK completeness is far lower |

Honest classification (architecture §73.B vocabulary):

| Item | Class |
|---|---|
| `TraderDbContext` as a compile-time store for the demo/in-memory path | `EXISTS_NEEDS_REFACTOR` |
| §45-complete model | `MISSING` |
| Split configuration classes | `MISSING` |
| Versioned PostgreSQL migrations | `MISSING` |
| `broker_connections` split (secrets/host off `brokers`) | `MISSING` (fields crammed onto `Broker`) |
| `model_*` tables | `MISSING` — **correct** under A52 Phase 6 hold; still a §45 gap |
| `kill_switches` table | Extra; A20 says derive current mode from `system_events` |

---

## 1. What was read (no product edits)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 174 lines; 20 `DbSet`s; all fluent maps inline in `OnModelCreating` |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\` | **Empty directory.** `Get-ChildItem -Force` listed the folder only. |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | Consumer of the 20 sets; upserts by LINQ, not `ON CONFLICT` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `UseInMemoryDatabase` or `UseNpgsql(connection)` — **no** snake_case, **no** migrations assembly, **no** retry |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | EF Core 8.0.4 + Npgsql.EF 8.0.4 + InMemory 8.0.4. **No** `EFCore.NamingConventions` |
| `D:\Prop\src\Domain\Entities\*.cs` | 20 types (19 files; `DestinationQuote.cs` holds `DestinationQuoteSnapshot`) |
| Architecture §45 | Quoted in §2 |
| A20 / A61 | Used as the UNIQUE / column contract when judging “present but wrong” |

Grep across `D:\Prop\src` for `IEntityTypeConfiguration` and `ApplyConfigurationsFromAssembly`: **0 hits**.

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

**43 names.** Aliases that must **not** be extra tables (A20 §1): `mt5_symbol_metadata` → `mt5_symbols`; `mt5_ticks_xauusd` → `mt5_xau_ticks`; `shadow_copy_*` → `shadow_*`.

§44 names **not** repeated in §45 (still required before live FIX, A20 extras): `execution_intents`, `execution_reconciliation_runs`, `execution_reconciliation_issues`. §11-only: `ingestion_events`.

---

## 3. Present tables (18) — name match vs mapping quality

`TraderDbContext` `DbSet` → `ToTable(...)` as measured:

| # | `DbSet` | CLR type | Table | §45? | Identity / UNIQUE actually configured | A20 expected UNIQUE | Mapping quality |
|---|---|---|---|---|---|---|---|
| 1 | `Brokers` | `Broker` | `brokers` | Yes | `Code` unique; `Code`/`DisplayName` max 32/128 | `brokers_code_uk (code)` | Partial. Connection host/port/manager login live **on this row** (belongs on `broker_connections`). No `kind`, no named UK. |
| 2 | `Mt5Groups` | `Mt5Group` | `mt5_groups` | Yes | `(BrokerId, Name)` unique | `mt5_groups_broker_name_uk (broker_id, group_name)` | Close. Column is `Name` not `group_name`. `PlanMapping` string on the group (overlay belongs in `plan_group_mappings`). `ConnectionsAllowed` is `bool` vs A61 `integer`. |
| 3 | `Mt5Accounts` | `Mt5Account` | `mt5_accounts` | Yes | `(BrokerId, Login)` unique | **`mt5_accounts_identity_uk`** | **§10 key is correct.** Missing `name`/`currency`/`is_enabled`/`last_event_at`. No group index. No compound FK to `mt5_accounts` children (there are no FKs at all). |
| 4 | `Mt5Deals` | `Mt5Deal` | `mt5_deals` | Yes | `(BrokerId, DealTicket)` unique; `(BrokerId, Login, DealTime)` non-unique | **`mt5_deals_identity_uk`** + 4 lookup indexes | **§10 deal key is correct.** Missing indexes on order/position/symbol. `Symbol` not `source_symbol`. `VolumeNative` is `ulong` (A61 wants `bigint` + converter). `Action`/`Entry` are CLR enums (default **int**, not `smallint` + comment). No `time_msc` / `fee` / `payload` / `payload_hash`. |
| 5 | `Mt5Positions` | `Mt5Position` | `mt5_positions_current` | Yes | `(BrokerId, PositionTicket)` unique | **`mt5_positions_current_identity_uk (broker_id, position_id)`** | Table name good. Property is `PositionTicket` not `PositionId`. A61 wants type `Mt5PositionCurrent`. No login/symbol indexes, no `xmin`. |
| 6 | `ReconstructedTrades` | `ReconstructedTrade` | `reconstructed_trades` | Yes | **non-unique** `(BrokerId, Login, PositionId, OpenedAt)` | **`reconstructed_trades_position_uk (broker_id, position_id)`** | **Identity FAIL.** A20/§10 require UNIQUE `(broker_id, position_id)`. Current index is a lookup and includes `OpenedAt`. Volumes are `decimal` lots, not native `bigint`. `Id` is regenerated on replace (`EfTradingStore.ReplaceReconstructedAsync`) so it is **not** a stable `source_trade_id`. |
| 7 | `CanonicalInstruments` | `CanonicalInstrument` | `canonical_instruments` | Yes | `Code` unique | `canonical_instruments_symbol_uk (canonical_symbol)` | Partial. Property `Code` vs `canonical_symbol`. |
| 8 | `SourceSymbolMappings` | `SourceSymbolMapping` | `source_symbol_mappings` | Yes | `(BrokerId, SourceSymbol)` unique | `source_symbol_mappings_uk` | Unique shape good. Maps to `CanonicalInstrumentId` GUID, not `canonical_symbol` text + FK. No reverse index. |
| 9 | `TraderScores` | `TraderScore` | `trader_scores` | Yes | `(BrokerId, Login)` unique | `(broker_id, login, score_kind)` **or** collapsed wide current row (A61 §8.16) | Wide current row is an allowed collapse. **Wrongly also stores** `CurrentState` and martingale/averaging/lot flags — those are `trader_states` + `trader_risk_flags`. No `score_kind`. |
| 10 | `TraderScoreHistory` | `TraderScoreHistory` | `trader_score_history` | Yes | **non-unique** `(BrokerId, Login, RecordedAt)` | `(broker_id, login, completed_trade_count, score_kind, model_version_id)` | Append exists; identity is time, not trade-count + kind. No `CompletedTradeCount`, no `ModelVersionId`. |
| 11 | `OutboxEvents` | `OutboxEvent` | `outbox_events` | Yes | **non-unique** `ProcessedAt` only | **`outbox_events_dedupe_uk (aggregate_type, aggregate_id, event_type, dedupe_key)`** + pending dispatcher partial | **Outbox FAIL.** No `AggregateType`, no `DedupeKey`, no `jsonb` payload, no `broker_id`, no pending-row index. `Type` is int enum. Cannot implement §12 same-commit idempotent insert. |
| 12 | `SyncCheckpoints` | `SyncCheckpoint` | `sync_checkpoints` | Yes | `(BrokerId, Login, Stream)` unique | `(scope_type, scope_id, stream_name)` | Too narrow. Forces a **login** on every cursor. Cannot represent broker-level (`deals_backfill`), venue (`security_list`), or global streams. `Login` is required `long`. |
| 13 | `CopyIntents` | `CopyIntent` | `copy_intents` | Yes | `IdempotencyKey` unique | `(source_broker_id, source_login, source_trade_id, source_event_id, action)` | Opaque string UK is not the A20 natural key. No `source_event_id`. `BrokerId` not named `source_broker_id`. Status is free `string`. |
| 14 | `RiskDecisions` | `RiskDecisionRecord` | `risk_decisions` | Yes | **non-unique** `CopyIntentId` | `(copy_intent_id, decision_seq)` | Can store many decisions but cannot upsert by seq. No `decision_seq`, no quote-age/spread columns, no `is_final` partial UK. |
| 15 | `ShadowOrders` | `ShadowOrder` | `shadow_orders` | Yes | PK only | `shadow_cl_ord_id` | **No unique business key.** No `shadow_cl_ord_id`. Row is a flattened fill (qty/price/slippage) — fills belong in `shadow_fills`. |
| 16 | `DestinationQuotes` | `DestinationQuoteSnapshot` | `destination_quotes` | Yes | PK only | `(venue_id, instrument_id)` | **No upsert key.** No `venue_id`. Latest-quote-per-instrument cannot be enforced. |
| 17 | `FixSessionStates` | `FixSessionState` | `fix_sessions` | Yes | `Qualifier` unique **globally** | `(venue_id, session_qualifier)` | **Wrong uniqueness.** One global QUOTE and one global TRADE forever. No `venue_id`. Qualifier stored as int enum, not `QUOTE`/`TRADE` text. |
| 18 | `AuditLogs` | `AuditLog` | `audit_logs` | Yes | PK only | `id bigint IDENTITY`; no UNIQUE | PK is `Guid`, not identity `bigint`. No `(occurred_at)` / actor indexes. |

---

## 4. Missing §45 tables (25)

| # | Table | Layer | Domain type today | Why it matters |
|---|---|---|---|---|
| 1 | `broker_connections` | registry | **none** | Host/port/manager login **number** / pool / proxy flags. Passwords stay in secrets. Today those non-secret fields sit on `brokers`. |
| 2 | `plan_group_mappings` | registry | **none** | Overlay only (`broker_id, plan_code, environment`). `Mt5Group.PlanMapping` is not this table. |
| 3 | `mt5_account_snapshots` | raw | **none** | Append-only balance/equity. `mt5_accounts` denormalizes latest only. |
| 4 | `mt5_orders` | raw | **none** | §10 `broker_id + order_ticket`. Deals cannot reconstruct order state alone. |
| 5 | `mt5_symbols` | raw | **none** | Per-broker contract metadata. Alias of §11 `mt5_symbol_metadata`. |
| 6 | `mt5_xau_ticks` | raw | **none** | Optional exact MFE/MAE tape. Alias of §11 `mt5_ticks_xauusd`. |
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

## 5. Extra tables (not in the §45 list)

| Table | `DbSet` | Origin | Keep? |
|---|---|---|---|
| `execution_intents` | `ExecutionIntents` | §44 / §33 / A20 #37 | **Yes, required** before any FIX send. Unique on `ClOrdId` is the one mapping that already matches A20. Still missing `source_event_id`, venue, status-as-text tokens, `execution_intents_risk_uk`. |
| `kill_switches` | `KillSwitches` | Product-only; A48 sketches a singleton; **not** in §45 | **Do not treat as §45 complete.** A20 §5.8 / §8 Q5: current kill-switch state comes from latest `system_events` (`STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN`). A second current-state table is optional later (`system_flags`), not this name. |

Not present and not in §45 (A20 extras, still missing): `ingestion_events`, `execution_reconciliation_runs`, `execution_reconciliation_issues`.

---

## 6. Configurations folder — measured empty

```text
D:\Prop\src\Infrastructure\Persistence\
  Configurations\          ← empty (0 files)
  EfTradingStore.cs
  TraderDbContext.cs
```

A61 §1 mentioned a historical `BrokersConfiguration.cs`. **It is not on disk now.** A61 §3 target of 43 `*Configuration.cs` files + `ApplyConfigurationsFromAssembly` has **not** been implemented.

All 20 entity maps live inside `TraderDbContext.OnModelCreating` (lines 33–173). Pattern per entity:

```csharp
modelBuilder.Entity<T>(e =>
{
    e.ToTable("...");
    e.HasKey(x => x.Id);
    e.HasIndex(...);   // sometimes .IsUnique()
    // Broker only: HasMaxLength on Code / DisplayName
});
```

What this does **not** do (A61 global conventions):

| Convention | Present? |
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
| `HasCheckConstraint` (`trader_states`, `QUOTE`/`TRADE`) | No |
| `xmin` concurrency on live-book tables | No |
| Compound FKs (`HasPrincipalKey`) | No |
| Partial UNIQUE (`HasFilter`) | No |
| `IDesignTimeDbContextFactory` | No |
| `ConfigureConventions` | No |

Without a naming convention, Npgsql will persist **PascalCase** column names (`DealTicket`, not `deal_ticket`) unless a later convention is added. `ToTable` is the only snake_case that is guaranteed today.

DI (`DependencyInjection.cs` 22–29): empty/`<SECRET>` connection string → **InMemory** `trader-intelligence`. Real `ConnectionStrings:TraderIntelligence` → `UseNpgsql` with **no** migrations assembly and **no** `Migrate()`. There is nothing to migrate.

---

## 7. §10 compound-identity matrix vs this model

| Law | Required table / UNIQUE | In `TraderDbContext` |
|---|---|---|
| `broker_id + login` | `mt5_accounts` | **Yes** `(BrokerId, Login)` |
| `broker_id + login` | `trader_states` | **No table** |
| `broker_id + deal_ticket` | `mt5_deals` | **Yes** `(BrokerId, DealTicket)` |
| `broker_id + order_ticket` | `mt5_orders` | **No table** |
| `broker_id + position_id` | `mt5_positions_current` | **Approximate** `(BrokerId, PositionTicket)` |
| `broker_id + position_id` | `reconstructed_trades` | **No** — index is non-unique and includes `OpenedAt` |

No source table globally-uniques `Login` / `DealTicket` alone. That part is clean.

Destination uniqueness is **not** venue-scoped: `fix_sessions.Qualifier` is globally unique; `destination_quotes` has no `(venue_id, instrument_id)`.

---

## 8. Enum persistence (will not match architecture tokens)

Unconfigured enums persist as **integers**. Architecture / A61 want **text** tokens (except raw deal `action`/`entry`, which should be SDK `smallint`).

| CLR enum | Stored today (default) | Required token examples |
|---|---|---|
| `TraderState` | 0..8 | `INSUFFICIENT_DATA`, `WATCH`, `SHADOW`, … |
| `OutboxEventType` | 0..4 | `TradeCompleted`, `ScoreUpdate`, … |
| `CopyIntentAction` | 0..3 | `OPEN_EXPOSURE`, … (CLR is `OpenExposure`) |
| `RiskDecisionOutcome` | 0..5 | `approve` / `reduce_size` / … (CLR is `Approve`) |
| `FixSessionQualifier` | 0..1 | `QUOTE` / `TRADE` (CLR is `Quote` / `Trade`) |
| `ExecutionOrderStatus` | 0..7 | `not_sent` / `EXECUTION_STATE_UNKNOWN` / … |
| `DealAction` / `DealEntry` | underlying integer | **Keep numeric** (`smallint`); do not stringify as the only store |
| `KillSwitchMode` | integer | `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` |

A PostgreSQL dump of today’s model cannot be compared to architecture check constraints.

---

## 9. Domain types that exist vs tables that do not

20 Domain entity files under `D:\Prop\src\Domain\Entities\`:

| File | Type | Mapped table |
|---|---|---|
| `Broker.cs` | `Broker` | `brokers` |
| `Mt5Group.cs` | `Mt5Group` | `mt5_groups` |
| `Mt5Account.cs` | `Mt5Account` | `mt5_accounts` |
| `Mt5Deal.cs` | `Mt5Deal` | `mt5_deals` |
| `Mt5Position.cs` | `Mt5Position` | `mt5_positions_current` |
| `ReconstructedTrade.cs` | `ReconstructedTrade` | `reconstructed_trades` |
| `CanonicalInstrument.cs` | `CanonicalInstrument` | `canonical_instruments` |
| `SourceSymbolMapping.cs` | `SourceSymbolMapping` | `source_symbol_mappings` |
| `TraderScore.cs` | `TraderScore` | `trader_scores` |
| `TraderScoreHistory.cs` | `TraderScoreHistory` | `trader_score_history` |
| `OutboxEvent.cs` | `OutboxEvent` | `outbox_events` |
| `SyncCheckpoint.cs` | `SyncCheckpoint` | `sync_checkpoints` |
| `CopyIntent.cs` | `CopyIntent` | `copy_intents` |
| `RiskDecisionRecord.cs` | `RiskDecisionRecord` | `risk_decisions` |
| `ExecutionIntent.cs` | `ExecutionIntent` | `execution_intents` (extra vs §45) |
| `ShadowOrder.cs` | `ShadowOrder` | `shadow_orders` |
| `DestinationQuote.cs` | `DestinationQuoteSnapshot` | `destination_quotes` |
| `FixSessionState.cs` | `FixSessionState` | `fix_sessions` |
| `AuditLog.cs` | `AuditLog` | `audit_logs` |
| `KillSwitch.cs` | `KillSwitch` | `kill_switches` (extra vs §45) |

**No Domain type** for the 25 missing §45 tables. A61 is explicit: add the Domain type first, then the configuration. Persistence must not invent types.

---

## 10. §44 / §11 overlay (so this gap is not under-counted)

### 10.1 §44 execution list

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

§44 coverage by table name: **6 / 14**.

### 10.2 §11 raw list

| §11 name | Canonical | In DbContext? |
|---|---|---|
| `mt5_accounts` | same | Yes |
| `mt5_account_snapshots` | same | No |
| `mt5_orders` | same | No |
| `mt5_deals` | same | Yes |
| `mt5_positions_current` | same | Yes |
| `mt5_groups` | same | Yes |
| `mt5_symbol_metadata` | `mt5_symbols` | No |
| `mt5_ticks_xauusd` | `mt5_xau_ticks` | No |
| `sync_checkpoints` | same | Yes (wrong key) |
| `ingestion_events` | §11 only | No |

---

## 11. Scorecard

```text
§45 tables          43
  present           18   (name only)
  missing           25
  extra              2   (execution_intents keep; kill_switches do not count as §45)

Present that match §10 UNIQUE
  mt5_accounts      YES
  mt5_deals         YES
  mt5_positions     APPROX (PositionTicket)
  reconstructed     NO
  trader_states     NO TABLE

Configurations       0 files
FKs                  0
Named A20 UKs        0
Migrations           0
IEntityTypeConfig    0
```

**First useful persistence increment (not implemented here):** split `IEntityTypeConfiguration` for the 18 existing tables; add A20-named uniques; add `broker_connections`, `mt5_orders`, `mt5_account_snapshots`, `mt5_symbols`, `trader_states`, `trader_risk_flags`, `outbox_events` dedupe UK, `sync_checkpoints` scope key, `execution_venues` so FIX uniques can be venue-scoped; emit a **new versioned** migration. Do **not** create `model_*` tables in that increment (A52).

---

## 12. Evidence quotes

`TraderDbContext` DbSets (lines 12–31):

```csharp
public DbSet<Broker> Brokers => Set<Broker>();
public DbSet<Mt5Group> Mt5Groups => Set<Mt5Group>();
public DbSet<Mt5Account> Mt5Accounts => Set<Mt5Account>();
public DbSet<Mt5Deal> Mt5Deals => Set<Mt5Deal>();
public DbSet<Mt5Position> Mt5Positions => Set<Mt5Position>();
public DbSet<ReconstructedTrade> ReconstructedTrades => Set<ReconstructedTrade>();
public DbSet<CanonicalInstrument> CanonicalInstruments => Set<CanonicalInstrument>();
public DbSet<SourceSymbolMapping> SourceSymbolMappings => Set<SourceSymbolMapping>();
public DbSet<TraderScore> TraderScores => Set<TraderScore>();
public DbSet<TraderScoreHistory> TraderScoreHistory => Set<TraderScoreHistory>();
public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
public DbSet<SyncCheckpoint> SyncCheckpoints => Set<SyncCheckpoint>();
public DbSet<CopyIntent> CopyIntents => Set<CopyIntent>();
public DbSet<RiskDecisionRecord> RiskDecisions => Set<RiskDecisionRecord>();
public DbSet<ExecutionIntent> ExecutionIntents => Set<ExecutionIntent>();
public DbSet<ShadowOrder> ShadowOrders => Set<ShadowOrder>();
public DbSet<DestinationQuoteSnapshot> DestinationQuotes => Set<DestinationQuoteSnapshot>();
public DbSet<FixSessionState> FixSessionStates => Set<FixSessionState>();
public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
public DbSet<KillSwitch> KillSwitches => Set<KillSwitch>();
```

Representative maps — reconstructed-trade **non-unique** vs required unique; outbox **no dedupe**; fix session **global** qualifier:

```csharp
// reconstructed_trades — lookup, not §10 unique
e.HasIndex(x => new { x.BrokerId, x.Login, x.PositionId, x.OpenedAt });

// outbox_events — dispatcher-ish only
e.HasIndex(x => x.ProcessedAt);

// fix_sessions — one QUOTE/TRADE for the whole platform
e.HasIndex(x => x.Qualifier).IsUnique();
```

---

## 13. What this agent did not do

- Did not edit any file under `D:\Prop\src`.
- Did not add configuration classes or migrations.
- Did not invent Domain types for the 25 missing tables.
- Did not claim “schema ready” or “≥95% mapped.”

This file is the B19 gap record only.
