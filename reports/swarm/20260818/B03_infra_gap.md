# B03 — Infrastructure gap vs architecture §45 (43 tables)

| Field | Value |
|---|---|
| Agent | B03 (Infrastructure gap, read-only) |
| Date | 2026-08-18 |
| Target | `D:\Prop\src\Infrastructure` |
| Spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§45** (full initial set) |
| Keys / mapping contract | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md`, `D:\Prop\reports\swarm\20260818\A61_efcore_schema.md` |
| Prior audit (stale) | `D:\Prop\reports\swarm\20260818\A03_infrastructure_audit.md` (measured empty `Class1` — **superseded by this file**) |
| Product source modified | **No.** Report only. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**Not Class1. Not empty. Not §45.**

`Class1.cs` is **gone** (`grep Class1` over `D:\Prop\**\*.cs` / `*.csproj` = **0**). Infrastructure now has a compiling demo persistence slice: `TraderDbContext` + `EfTradingStore` + `EfDashboardQueries` + `DemoSeeder` + `AddTraderIntelligence`. That is a **demo stub**, not the architecture catalog.

| Metric | Measured now | §45 / A61 target |
|---|---|---|
| Product source files (exclude `bin/` `obj/`) | **6** | Persistence + 43 configs + factory + converters |
| `Class1.cs` | **absent** | delete leftover (already done) |
| DbContext | `TraderDbContext` (1 file, inline fluent) | `TraderIntelligenceDbContext` + `IDesignTimeDbContextFactory` |
| `IEntityTypeConfiguration<T>` | **0** (`Persistence/Configurations/` is empty) | **43** (one per §45 table) |
| `ToTable(...)` names that match a §45 table | **18 / 43** | 43 |
| Named A20 unique constraints (`HasDatabaseName`) | **0** | all identity UKs |
| Snake-case columns (`HasColumnName` / `UseSnakeCaseNamingConvention`) | **0** | all columns |
| EF `Migrations/` | **0** | versioned, never `EnsureCreated` |
| `EnsureCreatedAsync` in hosts | **present** (api, mt5-worker, fix-worker) | **Forbidden** (A61 §3.3) |
| Redis usage (`ConnectionMultiplexer`) | **0** (package only) | locks/leases only; not SoT |
| Default database when `ConnectionStrings:TraderIntelligence` is empty | **EF InMemory** `"trader-intelligence"` | PostgreSQL 15+ / 16 |

**Score vs §45:** **18/43 tables named** (41.9%), **0/43 properly mapped**, **0/43 migrated**. Do not treat `TraderDbContext` as the production model.

---

## 1. Measured tree (2026-08-18)

Product files only (hashes SHA-256, sizes bytes):

| Bytes | SHA-256 | Path |
|---|---|---|
| 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| 4894 | `AAB9A2FA08AFCDACE1E270CE5A5FD4186853FA1008012244296538651B6D7164` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |

```text
D:\Prop\src\Infrastructure\
  TraderIntelligence.Infrastructure.csproj
  DependencyInjection.cs
  Dashboard\EfDashboardQueries.cs
  Persistence\
    TraderDbContext.cs          ← stub; replace, do not keep a second context
    EfTradingStore.cs           ← demo upserts; no outbox, no same-tx
    Configurations\             ← EMPTY (no IEntityTypeConfiguration)
    Migrations\                 ← MISSING
  Seeding\DemoSeeder.cs
  bin\  obj\                    ← build outputs only
```

**Absent (required by A61 Appendix B):**

- `Persistence/TraderIntelligenceDbContext.cs`
- `Persistence/TraderIntelligenceDbContextFactory.cs`
- `Persistence/Conventions/PgModelConventions.cs`
- `Persistence/Converters/PgUInt64.cs`
- `Persistence/Converters/EnumTextConverters.cs`
- `Persistence/Interceptors/UtcAuditInterceptor.cs`
- all 43 `Persistence/Configurations/*Configuration.cs`
- `Persistence/Migrations/`
- `Persistence/Outbox/*`
- `Redis/*`

A03 claimed `Class1.cs` + packages only. That snapshot is **stale**. This file is the current gap.

---

## 2. What §45 actually lists (43 tables, not 45)

Architecture §45 “Full initial set” is **43 names**. A20’s 47 is the union of §45 + §11 `ingestion_events` + §44 `execution_intents` / `execution_reconciliation_runs` / `execution_reconciliation_issues`. This report’s primary contract is **§45 = 43**. Appendix A below lists the four extras so implementers do not invent a second name.

Verbatim §45 list:

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

`model_*` tables **are in §45**. A52 forbids building the ML **service** now; it does **not** forbid creating empty schema mappings. Create the three tables. Do not add XGBoost / `/services/ml-service`.

---

## 3. Coverage matrix — every §45 table

Legend: **MAPPED** = `ToTable` exists on `TraderDbContext` (still stub quality). **MISSING** = no DbSet, no configuration, no Domain type.

| # | Table | Entity (A61) | Domain type now | `TraderDbContext` | Config file | Class |
|---|---|---|---|---|---|---|
| 1 | `brokers` | `Broker` | `Broker` (connection fields mixed in) | `Brokers` → `brokers` | none | **EXISTS_NEEDS_REFACTOR** |
| 2 | `broker_connections` | `BrokerConnection` | **none** | none | none | **MISSING** |
| 3 | `mt5_groups` | `Mt5Group` | `Mt5Group` (`Name` not `GroupName`; `PlanMapping` on group) | `Mt5Groups` | none | **EXISTS_NEEDS_REFACTOR** |
| 4 | `plan_group_mappings` | `PlanGroupMapping` | **none** | none | none | **MISSING** |
| 5 | `mt5_accounts` | `Mt5Account` | `Mt5Account` | `Mt5Accounts` | none | **EXISTS_NEEDS_REFACTOR** |
| 6 | `mt5_account_snapshots` | `Mt5AccountSnapshot` | **none** | none | none | **MISSING** |
| 7 | `mt5_orders` | `Mt5Order` | **none** | none | none | **MISSING** |
| 8 | `mt5_deals` | `Mt5Deal` | `Mt5Deal` (`Symbol` not `SourceSymbol`; enum not SDK `smallint`) | `Mt5Deals` | none | **EXISTS_NEEDS_REFACTOR** |
| 9 | `mt5_positions_current` | `Mt5PositionCurrent` | `Mt5Position` (wrong type name; UK on `PositionTicket` not `PositionId`) | `Mt5Positions` | none | **EXISTS_NEEDS_REFACTOR** |
| 10 | `mt5_symbols` | `Mt5Symbol` | **none** | none | none | **MISSING** |
| 11 | `mt5_xau_ticks` | `Mt5XauTick` | **none** | none | none | **MISSING** |
| 12 | `reconstructed_trades` | `ReconstructedTrade` | `ReconstructedTrade` (UK is `(BrokerId,Login,PositionId,OpenedAt)` not `(broker_id,position_id)`) | `ReconstructedTrades` | none | **EXISTS_NEEDS_REFACTOR** |
| 13 | `canonical_instruments` | `CanonicalInstrument` | `CanonicalInstrument` (`Code` not `CanonicalSymbol`) | `CanonicalInstruments` | none | **EXISTS_NEEDS_REFACTOR** |
| 14 | `source_symbol_mappings` | `SourceSymbolMapping` | `SourceSymbolMapping` | `SourceSymbolMappings` | none | **EXISTS_NEEDS_REFACTOR** |
| 15 | `trader_feature_snapshots` | `TraderFeatureSnapshot` | **none** | none | none | **MISSING** |
| 16 | `trader_scores` | `TraderScore` | `TraderScore` (no `score_kind`; flags inlined) | `TraderScores` | none | **EXISTS_NEEDS_REFACTOR** |
| 17 | `trader_score_history` | `TraderScoreHistory` | `TraderScoreHistory` (UK is time, not trade-count + kind + model) | `TraderScoreHistory` | none | **EXISTS_NEEDS_REFACTOR** |
| 18 | `trader_states` | `TraderStateRecord` | none (state is a column on `TraderScore`) | none | none | **MISSING** |
| 19 | `trader_risk_flags` | `TraderRiskFlag` | none (three bools on `TraderScore`) | none | none | **MISSING** |
| 20 | `model_versions` | `ModelVersion` | **none** | none | none | **MISSING** (schema only; no ML service) |
| 21 | `model_predictions` | `ModelPrediction` | **none** | none | none | **MISSING** (schema only) |
| 22 | `model_evaluations` | `ModelEvaluation` | **none** | none | none | **MISSING** (schema only) |
| 23 | `shadow_orders` | `ShadowOrder` | `ShadowOrder` (no `shadow_cl_ord_id`; no UK) | `ShadowOrders` | none | **EXISTS_NEEDS_REFACTOR** |
| 24 | `shadow_fills` | `ShadowFill` | **none** | none | none | **MISSING** |
| 25 | `shadow_positions` | `ShadowPosition` | **none** | none | none | **MISSING** |
| 26 | `shadow_performance` | `ShadowPerformance` | **none** | none | none | **MISSING** |
| 27 | `copy_intents` | `CopyIntent` | `CopyIntent` (UK is `IdempotencyKey`, not A20 compound) | `CopyIntents` | none | **EXISTS_NEEDS_REFACTOR** |
| 28 | `copy_allocations` | `CopyAllocation` | **none** | none | none | **MISSING** |
| 29 | `risk_decisions` | `RiskDecision` | `RiskDecisionRecord` (wrong type name; index not `(copy_intent_id, decision_seq)`) | `RiskDecisions` | none | **EXISTS_NEEDS_REFACTOR** |
| 30 | `risk_events` | `RiskEvent` | **none** | none | none | **MISSING** |
| 31 | `execution_venues` | `ExecutionVenue` | **none** | none | none | **MISSING** |
| 32 | `destination_symbols` | `DestinationSymbol` | **none** | none | none | **MISSING** |
| 33 | `destination_quotes` | `DestinationQuote` | `DestinationQuoteSnapshot` (no `venue_id`; no UK) | `DestinationQuotes` | none | **EXISTS_NEEDS_REFACTOR** |
| 34 | `fix_sessions` | `FixSession` | `FixSessionState` (UK on `Qualifier` only, not `(venue_id, session_qualifier)`) | `FixSessionStates` | none | **EXISTS_NEEDS_REFACTOR** |
| 35 | `fix_session_events` | `FixSessionEvent` | **none** | none | none | **MISSING** |
| 36 | `fix_orders` | `FixOrder` | **none** | none | none | **MISSING** |
| 37 | `fix_execution_reports` | `FixExecutionReport` | **none** | none | none | **MISSING** |
| 38 | `destination_positions` | `DestinationPosition` | **none** | none | none | **MISSING** |
| 39 | `source_destination_links` | `SourceDestinationLink` | **none** | none | none | **MISSING** |
| 40 | `sync_checkpoints` | `SyncCheckpoint` | `SyncCheckpoint` (UK is `(BrokerId,Login,Stream)` not `(scope_type,scope_id,stream_name)`) | `SyncCheckpoints` | none | **EXISTS_NEEDS_REFACTOR** |
| 41 | `outbox_events` | `OutboxEvent` | `OutboxEvent` (no dedupe UK; no `aggregate_type`; **never written** by `EfTradingStore`) | `OutboxEvents` | none | **EXISTS_NEEDS_REFACTOR** |
| 42 | `audit_logs` | `AuditLog` | `AuditLog` (`Id` is `Guid`; A20 wants `bigint GENERATED ALWAYS AS IDENTITY`) | `AuditLogs` | none | **EXISTS_NEEDS_REFACTOR** |
| 43 | `system_events` | `SystemEvent` | **none** | none | none | **MISSING** |

**Counts:** MAPPED stub **18**. MISSING **25**. Proper A61 configurations **0**.

### 3.1 Tables on the stub that are **not** §45

| Table | Origin | Current mapping | Action |
|---|---|---|---|
| `execution_intents` | §44 / §33 | `ExecutionIntents` → `execution_intents`, UK on `ClOrdId` | **Keep in the same DbContext** (Appendix A). Not a §45 table. |
| `kill_switches` | **not** in §45 / §44 / §11 | `KillSwitches` → `kill_switches` | **Do not treat as §45.** A48 wants `kill_switch_state` (two independent controls) + `risk_events` / `audit_logs`. Current exclusive `KillSwitchMode` row is **UNSAFE** if used as the latch. |

### 3.2 §11 / §44 extras still MISSING (A20 union 47)

| Table | Entity | UNIQUE | Why |
|---|---|---|---|
| `ingestion_events` | `IngestionEvent` | `(broker_id, source_event_id)` | §11 raw evidence; do not overload `outbox_events` |
| `execution_reconciliation_runs` | `ExecutionReconciliationRun` | none | §42–44 |
| `execution_reconciliation_issues` | `ExecutionReconciliationIssue` | `(run_id, issue_fingerprint)` | §43–44 |

`execution_intents` is already a stub DbSet (see 3.1).

---

## 4. Why the 18 mapped tables still FAIL A61

`TraderDbContext.OnModelCreating` is a single inline method. It sets `ToTable` + `HasKey(Id)` + a few anonymous unique indexes. It does **not**:

| A61 / A20 rule | Current stub |
|---|---|
| `ApplyConfigurationsFromAssembly` | no |
| `HasDefaultSchema("public")` | no |
| `UseSnakeCaseNamingConvention()` + explicit `ToTable` | no package `EFCore.NamingConventions`; no `HasColumnName` → Postgres columns will be CLR PascalCase (`DealTicket`, `BrokerId`) if quoted. **FAIL** A61 Appendix C |
| `HasDatabaseName("mt5_deals_identity_uk")` etc. | all indexes unnamed |
| `decimal` → `numeric(20,8)` / scores `numeric(5,2)` | default |
| `DateTimeOffset` → `timestamptz` convention | default |
| `ulong` converter `PgUInt64` | `VolumeNative` is `ulong` with no converter (Npgsql will reject or map badly on real PG) |
| Deal `action`/`entry` as SDK `smallint` | stored as EF enum (default `integer` named enum, not SDK numeric + comment) |
| Enums as architecture **text** tokens | default int |
| `xmin` concurrency on live-book tables | no |
| FKs (`broker_id` → `brokers`, accounts, venues) | **none declared** |
| `gen_random_uuid()` PK default | no |
| Secrets stay off `brokers` | `Broker` has `Server`, `Port`, `ManagerLogin`, `ProxyHost` — belongs on `broker_connections` (numbers only; **no password**, which is correctly absent) |
| Same-transaction outbox with raw write (§12–13) | `EfTradingStore` calls `SaveChangesAsync` per group/account/deal/position/score; **never inserts `OutboxEvent`** |
| Design-time factory | **MISSING** — `dotnet ef migrations add` has no factory |
| `Migrate()` | hosts call `EnsureCreatedAsync()` — **UNSAFE** as schema authority |

Current unique indexes vs A20 (even on mapped tables):

| Table | Stub unique | Required A20 name / columns |
|---|---|---|
| `brokers` | `Code` (unnamed) | `brokers_code_uk (code)` |
| `mt5_groups` | `(BrokerId, Name)` | `mt5_groups_broker_name_uk (broker_id, group_name)` |
| `mt5_accounts` | `(BrokerId, Login)` | `mt5_accounts_identity_uk (broker_id, login)` — columns OK, **name missing** |
| `mt5_deals` | `(BrokerId, DealTicket)` | `mt5_deals_identity_uk` — columns OK, **name missing** |
| `mt5_positions_current` | `(BrokerId, PositionTicket)` | `mt5_positions_current_identity_uk (broker_id, position_id)` — **column name wrong** |
| `reconstructed_trades` | `(BrokerId, Login, PositionId, OpenedAt)` | `reconstructed_trades_position_uk (broker_id, position_id)` — **too wide / wrong** |
| `canonical_instruments` | `Code` | `canonical_instruments_symbol_uk (canonical_symbol)` |
| `source_symbol_mappings` | `(BrokerId, SourceSymbol)` | `source_symbol_mappings_uk` — columns OK, name missing |
| `trader_scores` | `(BrokerId, Login)` | `trader_scores_uk (broker_id, login, score_kind)` unless product commits to one blended row |
| `trader_score_history` | `(BrokerId, Login, RecordedAt)` | `trader_score_history_uk (broker_id, login, completed_trade_count, score_kind, model_version_id)` |
| `outbox_events` | index on `ProcessedAt` only | `outbox_events_dedupe_uk (aggregate_type, aggregate_id, event_type, dedupe_key)` |
| `sync_checkpoints` | `(BrokerId, Login, Stream)` | `(scope_type, scope_id, stream_name)` |
| `copy_intents` | `IdempotencyKey` | `copy_intents_idem_uk (source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| `risk_decisions` | non-unique `CopyIntentId` | `risk_decisions_uk (copy_intent_id, decision_seq)` |
| `shadow_orders` | **none** | `shadow_orders_clord_uk (shadow_cl_ord_id)` |
| `destination_quotes` | **none** | `destination_quotes_uk (venue_id, instrument_id)` |
| `fix_sessions` | `Qualifier` only | `(venue_id, session_qualifier)` |
| `audit_logs` | none | PK should be `bigint IDENTITY`, not uuid |

---

## 5. Runtime wiring (not a schema win)

`DependencyInjection.AddTraderIntelligence`:

- Empty / `<SECRET>` / missing connection → `UseInMemoryDatabase("trader-intelligence")`.
- Else `UseNpgsql(connection)` with **no** `MigrationsAssembly`, **no** retry, **no** snake_case, **no** command timeout.
- API `appsettings.json` has `"ConnectionStrings": { "TraderIntelligence": "" }` → **InMemory is the live demo path**.
- Worker `appsettings.json` has **no** connection string at all → same InMemory.

Hosts (`apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) all:

```csharp
var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
await db.Database.EnsureCreatedAsync();
await DemoSeeder.SeedAsync(...);
```

`DemoSeeder` seeds two brokers (Achiever + StarwaveFX), one `XAUUSD` instrument, two FIX session rows, one quote, one kill-switch, then fake-connector ingest + score rebuild. That proves the stub compiles. It does **not** prove §45.

Packages (csproj):

| Package | Declared | Restored | Gap |
|---|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.4 | 8.0.4 | keep pair |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.4, `PrivateAssets=all` | 8.0.4 | keep |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.4 | 8.0.4 | OK for unit/demo; **not** migration proof |
| `Npgsql` | not pinned | **8.0.3** (transitive) | pin **8.0.4+** |
| `EFCore.NamingConventions` | absent | — | add **8.0.3** |
| `StackExchange.Redis` | 2.8.0 | 2.8.0 | unused; implementation MISSING |

Project references: Infrastructure → Domain, Application, **Mt5**. The Mt5 reference is a layering smell (Infrastructure should not need fake connectors to register DI). Out of scope for the table list; note only.

---

## 6. Exact files to create (implementation contract)

**One DbContext. One name.** A03/A30 said `TradingDbContext`. A10/A61 said `TraderIntelligenceDbContext`. The tree today has `TraderDbContext`. Create **exactly one**:

```text
TraderIntelligenceDbContext : DbContext
```

Do **not** add `TradingDbContext` as a second type. Replace the stub. After the new context compiles, delete or thin-wrap `TraderDbContext` so hosts/tests retarget. Two contexts = two models = guaranteed drift.

### 6.1 Persistence core (create)

| Create this file | Type | Role |
|---|---|---|
| `D:\Prop\src\Infrastructure\Persistence\TraderIntelligenceDbContext.cs` | `TraderIntelligenceDbContext` | 43 `DbSet`s + Appendix A; `HasDefaultSchema("public")`; `ApplyConfigurationsFromAssembly`; `ConfigureConventions` |
| `D:\Prop\src\Infrastructure\Persistence\TraderIntelligenceDbContextFactory.cs` | `IDesignTimeDbContextFactory<TraderIntelligenceDbContext>` | `dotnet ef`; read `TI_POSTGRES` / `ConnectionStrings__TraderIntelligence`; **no secrets in source** |
| `D:\Prop\src\Infrastructure\Persistence\Conventions\PgModelConventions.cs` | static helpers | uuid / timestamptz / numeric(20,8) / text |
| `D:\Prop\src\Infrastructure\Persistence\Converters\PgUInt64.cs` | `ValueConverter<ulong,long>` | login / tickets / native volume |
| `D:\Prop\src\Infrastructure\Persistence\Converters\EnumTextConverters.cs` | converters | architecture tokens (A61 §4.5) |
| `D:\Prop\src\Infrastructure\Persistence\Interceptors\UtcAuditInterceptor.cs` | interceptor | `created_at` / `updated_at` UTC |

`TraderIntelligenceDbContext` DbSets — copy this list; one property per §45 table:

```text
DbSet<Broker>                      Brokers
DbSet<BrokerConnection>            BrokerConnections
DbSet<Mt5Group>                    Mt5Groups
DbSet<PlanGroupMapping>            PlanGroupMappings
DbSet<Mt5Account>                  Mt5Accounts
DbSet<Mt5AccountSnapshot>          Mt5AccountSnapshots
DbSet<Mt5Order>                    Mt5Orders
DbSet<Mt5Deal>                     Mt5Deals
DbSet<Mt5PositionCurrent>          Mt5PositionsCurrent
DbSet<Mt5Symbol>                   Mt5Symbols
DbSet<Mt5XauTick>                  Mt5XauTicks
DbSet<ReconstructedTrade>          ReconstructedTrades
DbSet<CanonicalInstrument>         CanonicalInstruments
DbSet<SourceSymbolMapping>         SourceSymbolMappings
DbSet<TraderFeatureSnapshot>       TraderFeatureSnapshots
DbSet<TraderScore>                 TraderScores
DbSet<TraderScoreHistory>          TraderScoreHistory
DbSet<TraderStateRecord>           TraderStates
DbSet<TraderRiskFlag>              TraderRiskFlags
DbSet<ModelVersion>                ModelVersions
DbSet<ModelPrediction>             ModelPredictions
DbSet<ModelEvaluation>             ModelEvaluations
DbSet<ShadowOrder>                 ShadowOrders
DbSet<ShadowFill>                  ShadowFills
DbSet<ShadowPosition>              ShadowPositions
DbSet<ShadowPerformance>           ShadowPerformance
DbSet<CopyIntent>                  CopyIntents
DbSet<CopyAllocation>              CopyAllocations
DbSet<RiskDecision>                RiskDecisions
DbSet<RiskEvent>                   RiskEvents
DbSet<ExecutionVenue>              ExecutionVenues
DbSet<DestinationSymbol>           DestinationSymbols
DbSet<DestinationQuote>            DestinationQuotes
DbSet<FixSession>                  FixSessions
DbSet<FixSessionEvent>             FixSessionEvents
DbSet<FixOrder>                    FixOrders
DbSet<FixExecutionReport>          FixExecutionReports
DbSet<DestinationPosition>         DestinationPositions
DbSet<SourceDestinationLink>       SourceDestinationLinks
DbSet<SyncCheckpoint>              SyncCheckpoints
DbSet<OutboxEvent>                 OutboxEvents
DbSet<AuditLog>                    AuditLogs
DbSet<SystemEvent>                 SystemEvents
```

Appendix A (same context, not §45, do not skip if the system must ingest/execute):

```text
DbSet<IngestionEvent>                    IngestionEvents
DbSet<ExecutionIntent>                   ExecutionIntents          (type already exists)
DbSet<ExecutionReconciliationRun>        ExecutionReconciliationRuns
DbSet<ExecutionReconciliationIssue>      ExecutionReconciliationIssues
```

`OnModelCreating` must **not** inline 43 tables. Scan configurations only:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("public");
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(TraderIntelligenceDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

### 6.2 One `IEntityTypeConfiguration<T>` per §45 table (create all 43)

All under `D:\Prop\src\Infrastructure\Persistence\Configurations\`.

| # | Create this file | `ToTable` | Required UNIQUE (`HasDatabaseName`) |
|---|---|---|---|
| 1 | `BrokerConfiguration.cs` | `brokers` | `brokers_code_uk (code)` |
| 2 | `BrokerConnectionConfiguration.cs` | `broker_connections` | `broker_connections_name_uk (broker_id, connection_name)`; partial `broker_connections_one_primary_uk (broker_id) WHERE is_primary` |
| 3 | `Mt5GroupConfiguration.cs` | `mt5_groups` | `mt5_groups_broker_name_uk (broker_id, group_name)` |
| 4 | `PlanGroupMappingConfiguration.cs` | `plan_group_mappings` | `plan_group_mappings_plan_uk (broker_id, plan_code, environment)` — **do not** unique `(broker_id, group_name)` |
| 5 | `Mt5AccountConfiguration.cs` | `mt5_accounts` | `mt5_accounts_identity_uk (broker_id, login)` |
| 6 | `Mt5AccountSnapshotConfiguration.cs` | `mt5_account_snapshots` | `mt5_account_snapshots_uk (broker_id, login, snapshot_at)` |
| 7 | `Mt5OrderConfiguration.cs` | `mt5_orders` | `mt5_orders_identity_uk (broker_id, order_ticket)` |
| 8 | `Mt5DealConfiguration.cs` | `mt5_deals` | `mt5_deals_identity_uk (broker_id, deal_ticket)` + index `(broker_id, login, deal_time)` |
| 9 | `Mt5PositionCurrentConfiguration.cs` | `mt5_positions_current` | `mt5_positions_current_identity_uk (broker_id, position_id)` |
| 10 | `Mt5SymbolConfiguration.cs` | `mt5_symbols` | `mt5_symbols_uk (broker_id, source_symbol)` |
| 11 | `Mt5XauTickConfiguration.cs` | `mt5_xau_ticks` | `mt5_xau_ticks_uk (broker_id, source_symbol, time_msc, flags, ingest_seq)`; PK `bigint IDENTITY` |
| 12 | `ReconstructedTradeConfiguration.cs` | `reconstructed_trades` | `reconstructed_trades_position_uk (broker_id, position_id)` |
| 13 | `CanonicalInstrumentConfiguration.cs` | `canonical_instruments` | `canonical_instruments_symbol_uk (canonical_symbol)` |
| 14 | `SourceSymbolMappingConfiguration.cs` | `source_symbol_mappings` | `source_symbol_mappings_uk (broker_id, source_symbol)` |
| 15 | `TraderFeatureSnapshotConfiguration.cs` | `trader_feature_snapshots` | `trader_feature_snapshots_uk (broker_id, login, completed_trade_count, feature_schema_version)` |
| 16 | `TraderScoreConfiguration.cs` | `trader_scores` | `trader_scores_uk (broker_id, login, score_kind)` or documented collapse to `(broker_id, login)` |
| 17 | `TraderScoreHistoryConfiguration.cs` | `trader_score_history` | `trader_score_history_uk (broker_id, login, completed_trade_count, score_kind, model_version_id)` |
| 18 | `TraderStateRecordConfiguration.cs` | `trader_states` | `trader_states_identity_uk (broker_id, login)` + CHECK on state tokens |
| 19 | `TraderRiskFlagConfiguration.cs` | `trader_risk_flags` | `trader_risk_flags_uk (broker_id, login, flag_code)` |
| 20 | `ModelVersionConfiguration.cs` | `model_versions` | `model_versions_uk (model_name, version)` |
| 21 | `ModelPredictionConfiguration.cs` | `model_predictions` | `model_predictions_uk (model_version_id, broker_id, login, completed_trade_count)` |
| 22 | `ModelEvaluationConfiguration.cs` | `model_evaluations` | `model_evaluations_uk (model_version_id, evaluation_split, metric_set_version)` |
| 23 | `ShadowOrderConfiguration.cs` | `shadow_orders` | `shadow_orders_clord_uk (shadow_cl_ord_id)` |
| 24 | `ShadowFillConfiguration.cs` | `shadow_fills` | `shadow_fills_uk (shadow_order_id, fill_seq)` |
| 25 | `ShadowPositionConfiguration.cs` | `shadow_positions` | `shadow_positions_source_uk (source_broker_id, source_trade_id)` |
| 26 | `ShadowPerformanceConfiguration.cs` | `shadow_performance` | `shadow_performance_uk (source_broker_id, login, period_grain, period_start)` |
| 27 | `CopyIntentConfiguration.cs` | `copy_intents` | `copy_intents_idem_uk (source_broker_id, source_login, source_trade_id, source_event_id, action)` |
| 28 | `CopyAllocationConfiguration.cs` | `copy_allocations` | `copy_allocations_uk (copy_intent_id, destination_account)` |
| 29 | `RiskDecisionConfiguration.cs` | `risk_decisions` | `risk_decisions_uk (copy_intent_id, decision_seq)` |
| 30 | `RiskEventConfiguration.cs` | `risk_events` | none (append-only) |
| 31 | `ExecutionVenueConfiguration.cs` | `execution_venues` | `execution_venues_code_uk (venue_code)` |
| 32 | `DestinationSymbolConfiguration.cs` | `destination_symbols` | `destination_symbols_instr_uk (venue_id, instrument_id)` |
| 33 | `DestinationQuoteConfiguration.cs` | `destination_quotes` | `destination_quotes_uk (venue_id, instrument_id)` |
| 34 | `FixSessionConfiguration.cs` | `fix_sessions` | `(venue_id, session_qualifier)` |
| 35 | `FixSessionEventConfiguration.cs` | `fix_session_events` | none |
| 36 | `FixOrderConfiguration.cs` | `fix_orders` | `cl_ord_id`; partial `(venue_id, dest_order_id) WHERE dest_order_id IS NOT NULL` = `fix_orders_dest_uk` |
| 37 | `FixExecutionReportConfiguration.cs` | `fix_execution_reports` | `(venue_id, exec_id)` |
| 38 | `DestinationPositionConfiguration.cs` | `destination_positions` | `(venue_id, destination_account, destination_position_id)` |
| 39 | `SourceDestinationLinkConfiguration.cs` | `source_destination_links` | `(source_broker_id, source_trade_id, link_role, execution_intent_id)` |
| 40 | `SyncCheckpointConfiguration.cs` | `sync_checkpoints` | `(scope_type, scope_id, stream_name)` |
| 41 | `OutboxEventConfiguration.cs` | `outbox_events` | `outbox_events_dedupe_uk (aggregate_type, aggregate_id, event_type, dedupe_key)` |
| 42 | `AuditLogConfiguration.cs` | `audit_logs` | PK `bigint GENERATED ALWAYS AS IDENTITY` |
| 43 | `SystemEventConfiguration.cs` | `system_events` | none |

Appendix A configurations (create with the same conventions):

| Create this file | `ToTable` | UNIQUE |
|---|---|---|
| `IngestionEventConfiguration.cs` | `ingestion_events` | `ingestion_events_source_uk (broker_id, source_event_id)` |
| `ExecutionIntentConfiguration.cs` | `execution_intents` | `cl_ord_id` |
| `ExecutionReconciliationRunConfiguration.cs` | `execution_reconciliation_runs` | none |
| `ExecutionReconciliationIssueConfiguration.cs` | `execution_reconciliation_issues` | `(run_id, issue_fingerprint)` |

**Do not** create `KillSwitchConfiguration.cs` as a §45 table. If A48 is implemented later, that is `kill_switch_state` + `risk_events`, not `kill_switches`.

### 6.3 Domain types that must exist **before** those configurations (blocker)

EF must not invent persistence-only types for business tables. Create (or rename) these under `D:\Prop\src\Domain\Entities\` first. This report does **not** create them.

**Create (25 §45 types missing today):**

```text
D:\Prop\src\Domain\Entities\BrokerConnection.cs
D:\Prop\src\Domain\Entities\PlanGroupMapping.cs
D:\Prop\src\Domain\Entities\Mt5AccountSnapshot.cs
D:\Prop\src\Domain\Entities\Mt5Order.cs
D:\Prop\src\Domain\Entities\Mt5Symbol.cs
D:\Prop\src\Domain\Entities\Mt5XauTick.cs
D:\Prop\src\Domain\Entities\TraderFeatureSnapshot.cs
D:\Prop\src\Domain\Entities\TraderStateRecord.cs
D:\Prop\src\Domain\Entities\TraderRiskFlag.cs
D:\Prop\src\Domain\Entities\ModelVersion.cs
D:\Prop\src\Domain\Entities\ModelPrediction.cs
D:\Prop\src\Domain\Entities\ModelEvaluation.cs
D:\Prop\src\Domain\Entities\ShadowFill.cs
D:\Prop\src\Domain\Entities\ShadowPosition.cs
D:\Prop\src\Domain\Entities\ShadowPerformance.cs
D:\Prop\src\Domain\Entities\CopyAllocation.cs
D:\Prop\src\Domain\Entities\RiskEvent.cs
D:\Prop\src\Domain\Entities\ExecutionVenue.cs
D:\Prop\src\Domain\Entities\DestinationSymbol.cs
D:\Prop\src\Domain\Entities\FixSessionEvent.cs
D:\Prop\src\Domain\Entities\FixOrder.cs
D:\Prop\src\Domain\Entities\FixExecutionReport.cs
D:\Prop\src\Domain\Entities\DestinationPosition.cs
D:\Prop\src\Domain\Entities\SourceDestinationLink.cs
D:\Prop\src\Domain\Entities\SystemEvent.cs
```

**Rename / split (existing types, wrong name or mixed concerns):**

| Current file | Target type | Note |
|---|---|---|
| `Mt5Position.cs` | `Mt5PositionCurrent` | table `mt5_positions_current`; identity column `PositionId` |
| `DestinationQuote.cs` (`DestinationQuoteSnapshot`) | `DestinationQuote` | add `VenueId` + `InstrumentId` |
| `FixSessionState.cs` | `FixSession` | add `VenueId`; UK `(VenueId, Qualifier)` |
| `RiskDecisionRecord.cs` | `RiskDecision` | add `DecisionSeq` |
| `Broker.cs` | keep `Broker`; move host/port/login/proxy to `BrokerConnection` | no secrets |
| `CanonicalInstrument.cs` | property `CanonicalSymbol` (not only `Code`) | UK on `canonical_symbol` |
| `AuditLog.cs` | `long Id` identity **or** keep uuid and document A20 exception | pick one; A20 says bigint |
| `OutboxEvent.cs` | add `AggregateType`, `EventType` text, `DedupeKey` | required for `outbox_events_dedupe_uk` |
| `SyncCheckpoint.cs` | `ScopeType` + `ScopeId` + `StreamName` | not login-only |
| `CopyIntent.cs` | `SourceBrokerId`, `SourceEventId`, `Action` in UK | `IdempotencyKey` may be a stored projection of that tuple |
| `ShadowOrder.cs` | `ShadowClOrdId` | required unique |
| `TraderScore.cs` | stop being the only home of state/flags | those belong in `trader_states` / `trader_risk_flags` |

**Appendix A Domain types to create:**

```text
D:\Prop\src\Domain\Entities\IngestionEvent.cs
D:\Prop\src\Domain\Entities\ExecutionReconciliationRun.cs
D:\Prop\src\Domain\Entities\ExecutionReconciliationIssue.cs
```

`ExecutionIntent.cs` already exists.

No EF attributes on Domain types. Mapping lives only in Infrastructure configurations.

### 6.4 First migration (generate after configs compile — do not hand-write)

```text
D:\Prop\src\Infrastructure\Persistence\Migrations\
  <timestamp>_InitialSection45.cs
  <timestamp>_InitialSection45.Designer.cs
  TraderIntelligenceDbContextModelSnapshot.cs
```

Command shape (implementer; not run by this agent):

```text
dotnet ef migrations add InitialSection45
  --project D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
  --startup-project D:\Prop\apps\api\TraderIntelligence.Api.csproj
  --context TraderIntelligenceDbContext
  --output-dir Persistence/Migrations
```

Hosts must switch `EnsureCreatedAsync` → `Database.MigrateAsync` (or a migrator job). `EnsureCreated` is **forbidden** once migrations exist (A61).

### 6.5 Companion Infrastructure files (not tables, but the catalog is useless without them)

Create with the table work, or the 43 tables will sit unused:

```text
D:\Prop\src\Infrastructure\Persistence\Outbox\PostgresOutboxWriter.cs
D:\Prop\src\Infrastructure\Persistence\Outbox\PostgresOutboxProcessor.cs
D:\Prop\src\Infrastructure\Redis\RedisConnection.cs          # locks/leases only
```

`EfTradingStore` must be rewritten so raw deal upsert + outbox insert share **one** `SaveChangesAsync`. Today it does not.

### 6.6 Package edits (csproj only — not done here)

| Action | Package | Version |
|---|---|---|
| Add | `EFCore.NamingConventions` | 8.0.3 |
| Add (pin) | `Npgsql` | 8.0.4 or later 8.0.x |
| Keep paired | `Npgsql.EntityFrameworkCore.PostgreSQL` + EF Design | 8.0.4 |
| Do not add | Kafka / MassTransit / ML packages | — |

---

## 7. Implementation order (do not boil the ocean in one commit)

Matches A61 §12 / A30 Phase 1. Still **design** until coded.

1. Domain types in §6.3 (missing + renames). No EF.
2. Persistence core in §6.1 + naming package.
3. **P0 configurations (13):** `brokers`, `broker_connections`, `mt5_groups`, `plan_group_mappings`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols`, `sync_checkpoints`, `outbox_events`, `audit_logs`. Plus Appendix A `ingestion_events`.
4. Same-transaction outbox writer + `SKIP LOCKED` processor.
5. `canonical_instruments`, `source_symbol_mappings`, `reconstructed_trades`.
6. Scoring: `trader_feature_snapshots`, `trader_scores`, `trader_score_history`, `trader_states`, `trader_risk_flags`. `model_*` empty tables only.
7. Shadow four + copy/risk + destination/FIX (still Postgres **before** any TRADE socket).
8. One versioned migration + `PostgresMigrationTests` (Testcontainers Postgres 16). Required probe tables: A61 Appendix C.

P0 identity laws that must appear in that first migration:

```text
mt5_accounts_identity_uk
mt5_deals_identity_uk
mt5_orders_identity_uk
mt5_positions_current_identity_uk
reconstructed_trades_position_uk
outbox_events_dedupe_uk
```

Never unique-constrain `login`, `deal_ticket`, `order_ticket`, `position_id`, or `source_symbol` **alone**.

---

## 8. Do-not list

- Do not keep `Class1.cs` (already gone — do not recreate).
- Do not keep three DbContext names (`TraderDbContext` + `TradingDbContext` + `TraderIntelligenceDbContext`).
- Do not put all 43 mappings in `OnModelCreating`.
- Do not `EnsureCreated` in production hosts once migrations exist.
- Do not use InMemory as the §45 proof.
- Do not store passwords / FIX tag 554 / RawData on `brokers` or `broker_connections`.
- Do not create alias tables (`mt5_symbol_metadata`, `mt5_ticks_xauusd`, `shadow_copy_order`, `shadow_pnl`).
- Do not treat `kill_switches` as a §45 table.
- Do not build `/services/ml-service` because `model_*` tables are created.
- Do not make Redis authoritative for orders, positions, or balances.
- Do not globally unique `login` / tickets.

---

## 9. Acceptance bar for a later “§45 done” claim

All of the following must be **measured**, not asserted:

1. `TraderIntelligenceDbContext` exposes **43** §45 `DbSet`s and `ToTable` names match the verbatim list in §2.
2. `Persistence/Configurations/` contains **43** `IEntityTypeConfiguration` files (plus 4 Appendix A if claimed).
3. `information_schema.columns` for those tables is **snake_case**. A single `DealTicket` / `BrokerId` column is FAIL.
4. `pg_indexes` / `pg_constraint` contain the A20 `*_uk` names in §6.2 / §7.
5. A versioned migration applied twice on Testcontainers Postgres 16 is idempotent (`PostgresMigrationTests`).
6. One ingest write inserts `mt5_deals` + `outbox_events` in the **same** transaction.
7. Hosts no longer call `EnsureCreatedAsync` as the schema path.

Until then the honest label is: **demo stub, 18/43 named, 0/43 migrated.**

---

## 10. Cross-references

| Doc | Use |
|---|---|
| Architecture §45 | table name list (43) |
| Architecture §10 | `broker_id + login/ticket/position` law |
| Architecture §11 / §44 | Appendix A extras |
| Architecture §12–13 | outbox same-commit |
| `A20_table_catalog.md` | PK / UNIQUE / side |
| `A61_efcore_schema.md` | fluent contract, converters, file tree |
| `A03_infrastructure_audit.md` | stale empty-tree snapshot |
| `A30_implementation_sequence.md` | P0 order (`TradingDbContext` name is obsolete — use this file’s name) |
| `A41_outbox_design.md` | writer / processor |
| `A48_kill_switch.md` | do not persist exclusive `KillSwitchMode` as §45 |
| `A52_ml_not_yet.md` | schema for `model_*` only; no trainer |

---

No product source was modified. This file is the B03 gap report and the exact create-list for architecture §45.
