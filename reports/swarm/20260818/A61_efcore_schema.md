# A61 — EF Core 8 + Npgsql Mappings (§45)

**Document:** `D:\Prop\reports\swarm\20260818\A61_efcore_schema.md`  
**Date:** 2026-08-18  
**Status:** Implementation design. **Not product source.** No `.cs` files, migrations, or packages were changed.  
**Engine:** EF Core **8.0.4** + `Npgsql.EntityFrameworkCore.PostgreSQL` **8.0.4** → PostgreSQL 15+ (16 recommended for Testcontainers).  
**Source of law:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§45** (full initial set), with keys from **§10**, outbox from **§12–§13**, field lists from **§9, §11, §14, §16–§22, §24, §27–§28, §31, §33, §35–§40, §55, §57, §63**.  
**Key catalog:** `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (UNIQUE names, compound identities). This file maps that catalog onto EF Core 8 fluent configurations.  
**Aliases (do not create second tables):** `mt5_symbol_metadata` → `mt5_symbols`; `mt5_ticks_xauusd` → `mt5_xau_ticks`; `shadow_copy_order` → `shadow_orders`; `shadow_copy_fill` → `shadow_fills`; `shadow_position` → `shadow_positions`; `shadow_pnl` / `source_vs_shadow_slippage` → `shadow_performance` (plus fill-level slippage columns, not extra tables).

**§45 table count:** **43**. Every one is specified below.  
**Not in the §45 list** (do not pretend they are): `ingestion_events`, `execution_intents`, `execution_reconciliation_runs`, `execution_reconciliation_issues`. Those live in A20 / §11 / §44. Appendix A reserves them so implementers do not invent a second name.

---

## 0. Verdict (honest)

| Item | State |
|---|---|
| `src/Infrastructure` packages | EF Core Design 8.0.4 + Npgsql.EF 8.0.4 already referenced |
| Complete §45 model | **MISSING** — this document is the mapping contract |
| Checked-in migrations | **0** |
| Working `IEntityTypeConfiguration` for all 43 tables | **0** |
| Existing stub | `TraderDbContext` + `BrokersConfiguration` are incomplete and must be **replaced**, not extended blindly (see §1) |
| Product source modified by this agent | **No** |

Do not treat this markdown as a compiled model. Implementers generate a **new versioned EF migration** after coding the configurations. Never `EnsureCreated` in production.

---

## 1. Current tree vs this design

Measured 2026-08-18 (read-only):

| Path | What is there | Relation to this design |
|---|---|---|
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4, EF Design 8.0.4 | Keep versions paired. Add `EFCore.NamingConventions` 8.0.3 when implementing. Pin `Npgsql` 8.0.4+ explicitly (restore currently pulls **8.0.3**). |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `TraderDbContext` with 21 `DbSet`s | **Supersede.** Target name (A10): `TraderIntelligenceDbContext`. Several `DbSet` types (`Brokers`, `OutboxEvents`, `CopyIntents`, …) **do not exist** in Domain. `FixSessionStates` is not a §45 table (`fix_sessions` is). |
| `D:\Prop\src\Infrastructure\Persistence\Configurations\BrokersConfiguration.cs` | Shadow properties on type `Brokers`; table `brokers`; unique `code` | Shape is directionally right. Type name is wrong (`Broker`, not `Brokers`). Columns incomplete (`name`/`code`/`created_at` only). **Replace.** |
| `D:\Prop\src\Domain\Entities\*.cs` | Partial records (`Broker`, `Mt5Deal`, …) | Persistence-shaped but **not** complete vs A20/§45. EF maps **target** property sets in §8, not today’s incomplete records. Domain stays attribute-free. |
| Domain enums | `OutboxEventType`, `TraderState`, `DealAction`, `DealEntry`, `CopyIntentAction`, `RiskDecisionOutcome`, `FixSessionQualifier`, `FixSessionStatus`, `ExecutionOrderStatus`, `PriceSource`, `FeatureQuality`, `KillSwitchMode`, `TradeDirection`, `ReconciliationIssueType` | Store as **`text`** using the architecture token (see §4.5). |

Layering (binding):

```text
Domain entity / enum          — no EF, no [Column], no Npgsql
Infrastructure configuration  — IEntityTypeConfiguration<T>, ToTable, indexes, FKs
Infrastructure DbContext      — DbSet + ApplyConfigurationsFromAssembly
Application                   — IOutboxWriter, repositories; never builds the model
```

EF must **not** invent Domain types. If a table has no Domain type yet, add the Domain type first (A01), then this mapping. Persistence-only exception: `OutboxEvent`, `AuditLog`, `SyncCheckpoint`, `SystemEvent` may live under `Infrastructure.Persistence.Entities` **or** Domain — pick one and keep it. Recommended: Domain types `OutboxEvent`, `SyncCheckpoint`, `AuditLog`, `SystemEvent` (A01 already named them).

---

## 2. Global conventions (apply to every table)

### 2.1 Packages (implementation-time)

| Package | Version | Why |
|---|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | **8.0.4** (already referenced) | Provider |
| `Microsoft.EntityFrameworkCore.Design` | **8.0.4** (already referenced) | Migrations |
| `Npgsql` | **8.0.4 or later 8.0.x** | Pin; do not leave transitive 8.0.3 |
| `EFCore.NamingConventions` | **8.0.3** | `UseSnakeCaseNamingConvention()` |

Do **not** mix EF 8 with EF 9. Do **not** add Kafka packages.

### 2.2 PostgreSQL naming = snake_case

Binding:

| Layer | Style | Example |
|---|---|---|
| C# type | PascalCase singular | `Mt5Deal` |
| C# property | PascalCase | `DealTicket`, `BrokerId` |
| PostgreSQL table | **snake_case**, exact §45 name | `mt5_deals` |
| PostgreSQL column | **snake_case** | `deal_ticket`, `broker_id` |
| PostgreSQL unique | A20 `*_uk` names | `mt5_deals_identity_uk` |
| PostgreSQL index | `{table}_{cols}_ix` | `mt5_deals_broker_login_time_ix` |
| PostgreSQL check | `{table}_{col}_ck` | `trader_states_state_ck` |
| PostgreSQL FK | `{table}_{ref}_fk` | `mt5_deals_account_fk` |

How to get snake_case (do both):

1. **Convention (safety net):**

```csharp
services.AddDbContext<TraderIntelligenceDbContext>(o =>
    o.UseNpgsql(cs, npg =>
    {
        npg.MigrationsAssembly(typeof(TraderIntelligenceDbContext).Assembly.FullName);
        npg.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
        npg.CommandTimeout(60);
    })
    .UseSnakeCaseNamingConvention()
    .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll));
```

`UseSnakeCaseNamingConvention()` converts `DealTicket` → `deal_ticket`, `Mt5Deal` → `mt5_deal` (**wrong table name**). Therefore:

2. **Explicit `ToTable("<§45 name>")` on every entity** so the table is `mt5_deals`, not `mt5_deal`.
3. **Explicit `HasDatabaseName(...)` on every unique / named index / FK / check** so A20 names win over convention-generated names.
4. Optional belt-and-suspenders: `builder.Property(x => x.DealTicket).HasColumnName("deal_ticket");` on identity and money columns. After the naming convention is registered this is redundant but documents the contract in the configuration class.

**Do not** set `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`. Store UTC as `timestamptz` via `DateTimeOffset`.

### 2.3 Default CLR → PostgreSQL types

Set once in `ConfigureConventions`:

```csharp
protected override void ConfigureConventions(ModelConfigurationBuilder c)
{
    c.Properties<Guid>().HaveColumnType("uuid");
    c.Properties<DateTimeOffset>().HaveColumnType("timestamptz");
    c.Properties<DateTimeOffset?>().HaveColumnType("timestamptz");
    c.Properties<string>().HaveColumnType("text");
    c.Properties<decimal>().HavePrecision(20, 8);
    c.Properties<decimal?>().HavePrecision(20, 8);
}
```

| CLR | PostgreSQL | Notes |
|---|---|---|
| `Guid` / `Guid?` | `uuid` | PK default `gen_random_uuid()` (PG 13+ built-in) |
| `DateTimeOffset` | `timestamptz` | Persist UTC. Never `timestamp without time zone` |
| `string` | `text` | No `varchar(n)` unless a wire limit exists (FIX CompID) |
| `decimal` money / price / qty | `numeric(20,8)` | Never `double` / `real` in durable tables |
| `decimal` score 0–100 | `numeric(5,2)` | `trader_scores`, history |
| `ulong` login / ticket / native volume | `bigint` via converter | MT5 `uint64`; lab values fit signed `bigint` |
| `int` / `long` | `integer` / `bigint` | |
| `bool` | `boolean` | |
| JSON documents | `jsonb` | outbox payload, feature components, audit details, reason arrays |
| Enums | `text` | Architecture token, not the default int (see §4.5) |
| Identity `long` | `bigint GENERATED ALWAYS AS IDENTITY` | `audit_logs`, `mt5_xau_ticks` |

**`ulong` converter** (shared):

```csharp
public static class PgUInt64
{
    public static readonly ValueConverter<ulong, long> Converter = new(
        v => checked((long)v),
        v => unchecked((ulong)v));
}
```

Apply to: `Login`, `SourceLogin`, `DealTicket`, `OrderTicket`, `PositionId` / `PositionTicket`, raw `Volume` / `VolumeExt`. Throw on write if the value exceeds `long.MaxValue`.

**Do not** map `double` for prices. Domain `ReconstructedTrade` already uses `decimal`.

### 2.4 Surrogate PK vs natural UNIQUE

A20 law, implemented in EF as:

```csharp
builder.HasKey(x => x.Id);
builder.Property(x => x.Id)
    .HasColumnName("id")
    .HasColumnType("uuid")
    .HasDefaultValueSql("gen_random_uuid()")
    .ValueGeneratedOnAdd();

builder.HasIndex(x => new { x.BrokerId, x.DealTicket })
    .IsUnique()
    .HasDatabaseName("mt5_deals_identity_uk");
```

Application upserts / idempotency use the **UNIQUE**, never the UUID. `ON CONFLICT` targets the unique constraint name.

### 2.5 Required `broker_id` on every source-side table (§10)

EF: `.IsRequired()` on `BrokerId` for every **S** table. Destination tables use `VenueId`, not `BrokerId`, as the uniqueness root. Cross tables carry `SourceBrokerId` (`uuid NOT NULL`) as correlation.

Never:

```csharp
builder.HasIndex(x => x.DealTicket).IsUnique(); // FORBIDDEN
builder.HasIndex(x => x.Login).IsUnique();      // FORBIDDEN
```

### 2.6 Mutability / concurrency

| Kind | EF pattern |
|---|---|
| Append-only (deals, orders after first upsert, snapshots, history, outbox insert, audit, ERs, ticks) | No `UPDATE` of business columns after insert except documented upserts. Optional `xmin` concurrency token on mutable rows only. |
| Live book (`mt5_positions_current`, `destination_quotes`, `destination_positions`, `trader_scores`, `trader_states`, `fix_sessions`) | `xmin` as silent concurrency token |
| Outbox | `processed_at` + `attempt_count` updated by processor only |

```csharp
builder.Property<uint>("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

Do **not** add `xmin` to append-only tables (wastes mapping noise).

### 2.7 Secrets (§55)

**Never map** password, proxy password, FIX tag 554, RawData. `broker_connections` stores host/port/manager login **number** / pool / flags only. Connection secrets stay in the secret store. `audit_logs.details` must not contain secret keys; enforce in the writer, not in SQL.

### 2.8 Default schema

`public`. No extra schema in v1.

```csharp
modelBuilder.HasDefaultSchema("public");
```

---

## 3. `TraderIntelligenceDbContext`

Target (replace the stub):

```text
D:\Prop\src\Infrastructure\Persistence\TraderIntelligenceDbContext.cs
D:\Prop\src\Infrastructure\Persistence\TraderIntelligenceDbContextFactory.cs
D:\Prop\src\Infrastructure\Persistence\Conventions\PgModelConventions.cs
D:\Prop\src\Infrastructure\Persistence\Converters\PgUInt64.cs
D:\Prop\src\Infrastructure\Persistence\Converters\EnumTextConverters.cs
D:\Prop\src\Infrastructure\Persistence\Configurations\{Entity}Configuration.cs   // 43 files
```

### 3.1 DbSets — one per §45 table

| # | `DbSet<T>` | Entity | Table |
|---|---|---|---|
| 1 | `Brokers` | `Broker` | `brokers` |
| 2 | `BrokerConnections` | `BrokerConnection` | `broker_connections` |
| 3 | `Mt5Groups` | `Mt5Group` | `mt5_groups` |
| 4 | `PlanGroupMappings` | `PlanGroupMapping` | `plan_group_mappings` |
| 5 | `Mt5Accounts` | `Mt5Account` | `mt5_accounts` |
| 6 | `Mt5AccountSnapshots` | `Mt5AccountSnapshot` | `mt5_account_snapshots` |
| 7 | `Mt5Orders` | `Mt5Order` | `mt5_orders` |
| 8 | `Mt5Deals` | `Mt5Deal` | `mt5_deals` |
| 9 | `Mt5PositionsCurrent` | `Mt5PositionCurrent` | `mt5_positions_current` |
| 10 | `Mt5Symbols` | `Mt5Symbol` | `mt5_symbols` |
| 11 | `Mt5XauTicks` | `Mt5XauTick` | `mt5_xau_ticks` |
| 12 | `ReconstructedTrades` | `ReconstructedTrade` | `reconstructed_trades` |
| 13 | `CanonicalInstruments` | `CanonicalInstrument` | `canonical_instruments` |
| 14 | `SourceSymbolMappings` | `SourceSymbolMapping` | `source_symbol_mappings` |
| 15 | `TraderFeatureSnapshots` | `TraderFeatureSnapshot` | `trader_feature_snapshots` |
| 16 | `TraderScores` | `TraderScore` | `trader_scores` |
| 17 | `TraderScoreHistory` | `TraderScoreHistory` | `trader_score_history` |
| 18 | `TraderStates` | `TraderStateRecord` | `trader_states` |
| 19 | `TraderRiskFlags` | `TraderRiskFlag` | `trader_risk_flags` |
| 20 | `ModelVersions` | `ModelVersion` | `model_versions` |
| 21 | `ModelPredictions` | `ModelPrediction` | `model_predictions` |
| 22 | `ModelEvaluations` | `ModelEvaluation` | `model_evaluations` |
| 23 | `ShadowOrders` | `ShadowOrder` | `shadow_orders` |
| 24 | `ShadowFills` | `ShadowFill` | `shadow_fills` |
| 25 | `ShadowPositions` | `ShadowPosition` | `shadow_positions` |
| 26 | `ShadowPerformance` | `ShadowPerformance` | `shadow_performance` |
| 27 | `CopyIntents` | `CopyIntent` | `copy_intents` |
| 28 | `CopyAllocations` | `CopyAllocation` | `copy_allocations` |
| 29 | `RiskDecisions` | `RiskDecision` | `risk_decisions` |
| 30 | `RiskEvents` | `RiskEvent` | `risk_events` |
| 31 | `ExecutionVenues` | `ExecutionVenue` | `execution_venues` |
| 32 | `DestinationSymbols` | `DestinationSymbol` | `destination_symbols` |
| 33 | `DestinationQuotes` | `DestinationQuote` | `destination_quotes` |
| 34 | `FixSessions` | `FixSession` | `fix_sessions` |
| 35 | `FixSessionEvents` | `FixSessionEvent` | `fix_session_events` |
| 36 | `FixOrders` | `FixOrder` | `fix_orders` |
| 37 | `FixExecutionReports` | `FixExecutionReport` | `fix_execution_reports` |
| 38 | `DestinationPositions` | `DestinationPosition` | `destination_positions` |
| 39 | `SourceDestinationLinks` | `SourceDestinationLink` | `source_destination_links` |
| 40 | `SyncCheckpoints` | `SyncCheckpoint` | `sync_checkpoints` |
| 41 | `OutboxEvents` | `OutboxEvent` | `outbox_events` |
| 42 | `AuditLogs` | `AuditLog` | `audit_logs` |
| 43 | `SystemEvents` | `SystemEvent` | `system_events` |

`TraderStateRecord` avoids colliding with enum `TraderState`. Table is still `trader_states`.

`OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.HasDefaultSchema("public");
    modelBuilder.HasPostgresExtension("pgcrypto"); // optional if PG < 13; skip on PG 16
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(TraderIntelligenceDbContext).Assembly);
    base.OnModelCreating(modelBuilder);
}
```

Do **not** `ApplyConfiguration(new BrokersConfiguration())` by hand once the assembly scan is in place.

### 3.2 Design-time factory

```csharp
public sealed class TraderIntelligenceDbContextFactory
    : IDesignTimeDbContextFactory<TraderIntelligenceDbContext>
{
    public TraderIntelligenceDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("TI_POSTGRES")
                 ?? "Host=127.0.0.1;Port=5432;Database=trader_intelligence;Username=ti;Password=ti";
        var opts = new DbContextOptionsBuilder<TraderIntelligenceDbContext>()
            .UseNpgsql(cs, n => n.MigrationsAssembly(typeof(TraderIntelligenceDbContext).Assembly.FullName))
            .UseSnakeCaseNamingConvention()
            .Options;
        return new TraderIntelligenceDbContext(opts);
    }
}
```

Connection strings belong in env / user-secrets (`ConnectionStrings__TraderIntelligence`). Placeholders only in `.env.example`.

### 3.3 Migrations

| Rule | Detail |
|---|---|
| Assembly | `TraderIntelligence.Infrastructure` |
| Folder | `Persistence/Migrations/` |
| History table | `__ef_migrations_history` (default) |
| First migration name | `20260818_InitialSection45` (timestamp from `dotnet ef`, not hand-named if the tool wins) |
| Down | Not required for production rollback policy; **Up must be versioned** (A10) |
| `EnsureCreated` | **Forbidden** in hosts |
| Tests | `PostgresMigrationTests` against Testcontainers Postgres 16 (A10) |

---

## 4. Compound unique indexes — `(BrokerId, Ticket)` and the rest of §10

This is the hard identity law. Implement as **unique indexes**, not as composite PKs (surrogate `id uuid` remains the PK).

### 4.1 Ticket / login identities (must exist)

| Law | Entity | Properties | Constraint name | Table |
|---|---|---|---|---|
| `broker_id + login` | `Mt5Account` | `BrokerId`, `Login` | `mt5_accounts_identity_uk` | `mt5_accounts` |
| `broker_id + login` | `TraderStateRecord` | `BrokerId`, `Login` | `trader_states_identity_uk` | `trader_states` |
| `broker_id + deal_ticket` | `Mt5Deal` | `BrokerId`, `DealTicket` | `mt5_deals_identity_uk` | `mt5_deals` |
| `broker_id + order_ticket` | `Mt5Order` | `BrokerId`, `OrderTicket` | `mt5_orders_identity_uk` | `mt5_orders` |
| `broker_id + position_id` | `Mt5PositionCurrent` | `BrokerId`, `PositionId` | `mt5_positions_current_identity_uk` | `mt5_positions_current` |
| `broker_id + position_id` | `ReconstructedTrade` | `BrokerId`, `PositionId` | `reconstructed_trades_position_uk` | `reconstructed_trades` |

Fluent template (copy per ticket table):

```csharp
builder.HasIndex(e => new { e.BrokerId, e.DealTicket })
    .IsUnique()
    .HasDatabaseName("mt5_deals_identity_uk");
```

`Login` / tickets: `HasConversion(PgUInt64.Converter).HasColumnType("bigint").IsRequired()`.

### 4.2 Same law, extra discriminator columns

| Entity | Unique columns | Name |
|---|---|---|
| `Mt5AccountSnapshot` | `BrokerId, Login, SnapshotAt` | `mt5_account_snapshots_uk` |
| `TraderScore` | `BrokerId, Login` *(wide current row; see §8.16)* | `trader_scores_uk` |
| `TraderRiskFlag` | `BrokerId, Login, FlagCode` | `trader_risk_flags_uk` |
| `TraderFeatureSnapshot` | `BrokerId, Login, CompletedTradeCount, FeatureSchemaVersion` | `trader_feature_snapshots_uk` |
| `TraderScoreHistory` | `BrokerId, Login, CompletedTradeCount, ScoreKind, ModelVersionId` | `trader_score_history_uk` |
| `Mt5Group` | `BrokerId, GroupName` | `mt5_groups_broker_name_uk` |
| `PlanGroupMapping` | `BrokerId, PlanCode, Environment` | `plan_group_mappings_plan_uk` |
| `Mt5Symbol` | `BrokerId, SourceSymbol` | `mt5_symbols_uk` |
| `SourceSymbolMapping` | `BrokerId, SourceSymbol` | `source_symbol_mappings_uk` |
| `Mt5XauTick` | `BrokerId, SourceSymbol, TimeMsc, Flags, IngestSeq` | `mt5_xau_ticks_uk` |
| `ModelPrediction` | `ModelVersionId, BrokerId, Login, CompletedTradeCount` | `model_predictions_uk` |

### 4.3 Must stay **non-unique**

| Tempting index | Why forbidden |
|---|---|
| `login` | Collides across brokers |
| `deal_ticket` / `order_ticket` / `position_id` | Collides across brokers |
| `source_symbol` globally | Broker-local strings |
| `(broker_id, order_ticket)` on `mt5_deals` | One order → many deals |
| `(broker_id, position_id)` on `mt5_deals` | Many deals per position |
| `(broker_id, group_name)` on `plan_group_mappings` | Many plans → one group |
| `cl_ord_id` on `fix_execution_reports` | Many ERs per order |
| `(source_broker_id, source_trade_id)` on `copy_intents` or `source_destination_links` | Multiple actions / roles |

### 4.4 Partial UNIQUE (EF `HasFilter`)

| Entity | Columns | Filter | Name |
|---|---|---|---|
| `BrokerConnection` | `BrokerId` | `is_primary` | `broker_connections_one_primary_uk` |
| `ModelVersion` | `ModelName` | `is_production` | `model_versions_one_prod_uk` |
| `FixOrder` | `VenueId, DestOrderId` | `dest_order_id IS NOT NULL` | `fix_orders_dest_uk` |
| `RiskDecision` | `CopyIntentId` | `is_final` (optional) | `risk_decisions_final_uk` |
| `ShadowOrder` | `CopyIntentId` | `copy_intent_id IS NOT NULL` | `shadow_orders_intent_uk` |

```csharp
builder.HasIndex(e => new { e.VenueId, e.DestOrderId })
    .IsUnique()
    .HasFilter("dest_order_id IS NOT NULL")
    .HasDatabaseName("fix_orders_dest_uk");
```

### 4.5 Enum → `text` tokens

Store the **architecture string**, not the CLR name when they differ.

| Enum | DB tokens |
|---|---|
| `TraderState` | `INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED` |
| `OutboxEventType` | `TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent` plus raw ingest strings (`DealPersisted`, …) — column is `text`, not a PG enum |
| `CopyIntentAction` | `OPEN_EXPOSURE`, `INCREASE_EXPOSURE`, `REDUCE_EXPOSURE`, `CLOSE_EXPOSURE` |
| `RiskDecisionOutcome` | `approve`, `reduce_size`, `reject`, `pause_trader`, `pause_venue`, `global_stop` |
| `FixSessionQualifier` | `QUOTE`, `TRADE` |
| `ExecutionOrderStatus` | `not_sent`, `sent_ack_unknown`, `accepted`, `partially_filled`, `filled`, `rejected`, `cancelled`, `EXECUTION_STATE_UNKNOWN` |
| `TradeDirection` | `LONG`, `SHORT` (or `Buy`/`Sell` on order-side columns — keep **side** vs **position direction** distinct) |
| `DealAction` / `DealEntry` | persist **integer** as `integer` **and** optional `text` name. Identity of the raw layer is the SDK numeric (`IMTDeal::EnDealAction`). Prefer `action smallint` + generated/check. Recommended: `smallint` matching the SDK, plus a **comment**, not a second source of truth. |
| `PriceSource` | `ACHIEVER_MT5_TICKS`, `STARWAVE_MT5_TICKS`, `BAR_APPROXIMATION`, `CTRADER_QUOTE_SESSION`, `UNKNOWN` |
| `FeatureQuality` | `EXACT`, `APPROXIMATE`, `UNAVAILABLE` |
| `KillSwitchMode` | `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN` (never a single boolean) |

`HasConversion` with an explicit map. Add `HasCheckConstraint` listing the tokens for state machines that A20 CHECKs (`trader_states`, `fix_sessions.session_qualifier`).

Raw MT5 `DealAction`/`DealEntry`: **do not** stringify as the only stored value. Store `action smallint NOT NULL` / `entry smallint NOT NULL` so a Manager dump round-trips. Optional view or computed text is fine.

---

## 5. Outbox (`outbox_events`) — §12 / §13 / §45

P0. Same PostgreSQL transaction as the raw/domain write. Kafka is **not** day-one.

### 5.1 Entity

```text
OutboxEvent
```

| Property | Column | PG type | Null | Notes |
|---|---|---|---|---|
| `Id` | `id` | `uuid` | NO | PK, `gen_random_uuid()` |
| `BrokerId` | `broker_id` | `uuid` | YES | Set when the aggregate is source-scoped |
| `SourceLogin` | `source_login` | `bigint` | YES | §57 correlation |
| `AggregateType` | `aggregate_type` | `text` | NO | e.g. `mt5_deal`, `reconstructed_trade`, `copy_intent` |
| `AggregateId` | `aggregate_id` | `uuid` | NO | Surrogate of the written row |
| `EventType` | `event_type` | `text` | NO | §13 five kinds + ingest kinds |
| `DedupeKey` | `dedupe_key` | `text` | NO | Producer-assigned; e.g. `{broker_id}:{deal_ticket}:persisted` |
| `Payload` | `payload` | `jsonb` | NO | Schema-versioned document |
| `PayloadSchemaVersion` | `payload_schema_version` | `integer` | NO | `> 0` |
| `CorrelationId` | `correlation_id` | `uuid` | YES | §57 |
| `SourceTradeId` | `source_trade_id` | `uuid` | YES | |
| `CopyIntentId` | `copy_intent_id` | `uuid` | YES | |
| `CreatedAt` | `created_at` | `timestamptz` | NO | `now()` |
| `AvailableAt` | `available_at` | `timestamptz` | NO | Default `created_at`; backoff updates this |
| `ProcessedAt` | `processed_at` | `timestamptz` | YES | NULL = pending |
| `AttemptCount` | `attempt_count` | `integer` | NO | Default 0 |
| `LastError` | `last_error` | `text` | YES | Truncate; never FIX passwords |
| `LockedUntil` | `locked_until` | `timestamptz` | YES | Optional lease if not using `SKIP LOCKED` only |
| `LockedBy` | `locked_by` | `text` | YES | Worker instance id |

`Payload` CLR type: `JsonDocument` or `string`. Prefer `JsonDocument` + `HasColumnType("jsonb")`. Do not use `json` (no binary).

### 5.2 Indexes

| Name | Columns / expression | Unique | Purpose |
|---|---|---|---|
| `outbox_events_pkey` | `id` | PK | |
| `outbox_events_dedupe_uk` | `(aggregate_type, aggregate_id, event_type, dedupe_key)` | **YES** | Idempotent insert |
| `outbox_events_dispatcher_ix` | `(available_at, created_at) WHERE processed_at IS NULL` | no | Drain |
| `outbox_events_broker_ix` | `(broker_id, created_at DESC)` | no | Ops / metric slices |

```csharp
public sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> b)
    {
        b.ToTable("outbox_events", t => t.HasComment("PostgreSQL transactional outbox (§12–13, §45)."));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.BrokerId).HasColumnName("broker_id");
        b.Property(x => x.SourceLogin).HasColumnName("source_login").HasConversion(PgUInt64.Converter);
        b.Property(x => x.AggregateType).HasColumnName("aggregate_type").IsRequired();
        b.Property(x => x.AggregateId).HasColumnName("aggregate_id").IsRequired();
        b.Property(x => x.EventType).HasColumnName("event_type").IsRequired();
        b.Property(x => x.DedupeKey).HasColumnName("dedupe_key").IsRequired();
        b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.PayloadSchemaVersion).HasColumnName("payload_schema_version").IsRequired();
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id");
        b.Property(x => x.SourceTradeId).HasColumnName("source_trade_id");
        b.Property(x => x.CopyIntentId).HasColumnName("copy_intent_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired();
        b.Property(x => x.AvailableAt).HasColumnName("available_at").HasDefaultValueSql("now()").IsRequired();
        b.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        b.Property(x => x.AttemptCount).HasColumnName("attempt_count").HasDefaultValue(0).IsRequired();
        b.Property(x => x.LastError).HasColumnName("last_error");
        b.Property(x => x.LockedUntil).HasColumnName("locked_until");
        b.Property(x => x.LockedBy).HasColumnName("locked_by");

        b.HasIndex(x => new { x.AggregateType, x.AggregateId, x.EventType, x.DedupeKey })
            .IsUnique()
            .HasDatabaseName("outbox_events_dedupe_uk");

        b.HasIndex(x => new { x.AvailableAt, x.CreatedAt })
            .HasFilter("processed_at IS NULL")
            .HasDatabaseName("outbox_events_dispatcher_ix");

        b.HasIndex(x => new { x.BrokerId, x.CreatedAt })
            .HasDatabaseName("outbox_events_broker_ix");
    }
}
```

### 5.3 Same-transaction writer (Application/Infrastructure)

```text
await using var tx = await db.Database.BeginTransactionAsync();
db.Mt5Deals.Add(deal);                 // or Update on identity UK
db.OutboxEvents.Add(new OutboxEvent {  // same DbContext
    AggregateType = "mt5_deal",
    AggregateId   = deal.Id,
    EventType     = "DealPersisted",   // later: TradeCompleted after reconstruction
    DedupeKey     = $"{deal.BrokerId}:{deal.DealTicket}:persisted",
    Payload       = payload,
    PayloadSchemaVersion = 1,
    BrokerId      = deal.BrokerId,
    SourceLogin   = deal.Login,
});
await db.SaveChangesAsync();
await tx.CommitAsync();
```

`SaveChanges` on one context is already one transaction if no ambient split. Prefer an explicit transaction when the writer also updates `sync_checkpoints`.

**Idempotent insert:** catch unique violation on `outbox_events_dedupe_uk` and treat as success (already queued). Do **not** update payload on conflict.

**Forbidden:** write the outbox on a second connection / second `SaveChanges` after the raw commit. **Forbidden:** publish from the MT5 Manager callback before commit. **Forbidden:** handler that sends `NewOrderSingle`.

### 5.4 Drain query (processor)

EF 8 cannot emit `FOR UPDATE SKIP LOCKED` from LINQ. Use raw SQL on the same context:

```sql
SELECT *
FROM outbox_events
WHERE processed_at IS NULL
  AND available_at <= now()
ORDER BY created_at
FOR UPDATE SKIP LOCKED
LIMIT @batch;
```

Then dispatch; on success:

```csharp
await db.OutboxEvents
    .Where(x => x.Id == id && x.ProcessedAt == null)
    .ExecuteUpdateAsync(s => s
        .SetProperty(x => x.ProcessedAt, now)
        .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1));
```

On failure: increment `attempt_count`, set `last_error`, set `available_at = now() + backoff`. Poison rows stay `processed_at IS NULL` and alarm. Metric `mt5_outbox_backlog` = `COUNT(*) FILTER (WHERE processed_at IS NULL)`.

Handlers **must** be idempotent (replay after crash between commit-of-work and mark-processed).

### 5.5 Event types (§13)

| Token | When written | Typical aggregate |
|---|---|---|
| `TradeCompleted` | reconstructed trade `completed=true` upserted | `reconstructed_trade` |
| `ScoreUpdate` | after trade N close | `trader_score` / `reconstructed_trade` |
| `ShadowCopyIntent` | shadow engine should simulate | `copy_intent` or `reconstructed_trade` |
| `RiskCheckRequest` | live copy path after intent persist | `copy_intent` |
| `NotificationEvent` | dashboard / ops | varies |
| `DealPersisted` / `OrderPersisted` / `PositionUpserted` / `AccountUpserted` | Phase 1 ingest | `mt5_*` |

Column stays free `text`. Do not create a PostgreSQL `ENUM` type (painful to extend). C# `OutboxEventType` covers the five; extra ingest kinds are strings.

---

## 6. Foreign keys (identity-preserving)

Prefer **compound FKs** on source tables so a join cannot cross brokers. Parent must have a matching UNIQUE (already listed).

| Child | FK columns | Parent | On delete |
|---|---|---|---|
| `broker_connections.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `mt5_groups.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `plan_group_mappings.(broker_id, group_name)` | compound | `mt5_groups.(broker_id, group_name)` | Restrict |
| `mt5_accounts.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `mt5_account_snapshots.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `mt5_orders.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `mt5_deals.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `mt5_positions_current.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `mt5_symbols.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `mt5_xau_ticks.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `reconstructed_trades.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `reconstructed_trades.canonical_symbol` | `canonical_symbol` | `canonical_instruments.canonical_symbol` | Restrict |
| `source_symbol_mappings.broker_id` | `broker_id` | `brokers.id` | Restrict |
| `source_symbol_mappings.canonical_symbol` | `canonical_symbol` | `canonical_instruments.canonical_symbol` | Restrict |
| `trader_states / scores / history / features / flags .(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `model_predictions.model_version_id` | `model_version_id` | `model_versions.id` | Restrict |
| `model_predictions.(broker_id, login)` | compound | `mt5_accounts.(broker_id, login)` | Restrict |
| `model_evaluations.model_version_id` | `model_version_id` | `model_versions.id` | Restrict |
| `copy_intents.source_trade_id` | `source_trade_id` | `reconstructed_trades.id` | Restrict |
| `copy_allocations.copy_intent_id` | `copy_intent_id` | `copy_intents.id` | Restrict |
| `risk_decisions.copy_intent_id` | `copy_intent_id` | `copy_intents.id` | Restrict |
| `shadow_orders.copy_intent_id` | `copy_intent_id` | `copy_intents.id` | Restrict |
| `shadow_fills.shadow_order_id` | `shadow_order_id` | `shadow_orders.id` | Restrict |
| `shadow_positions.source_trade_id` | `source_trade_id` | `reconstructed_trades.id` | Restrict |
| `destination_symbols.venue_id` | `venue_id` | `execution_venues.id` | Restrict |
| `destination_quotes.(venue_id, instrument_id)` | compound | `destination_symbols.(venue_id, instrument_id)` | Restrict |
| `fix_sessions.venue_id` | `venue_id` | `execution_venues.id` | Restrict |
| `fix_session_events.session_id` | `session_id` | `fix_sessions.id` | Restrict |
| `fix_orders.venue_id` | `venue_id` | `execution_venues.id` | Restrict |
| `fix_execution_reports.venue_id` | `venue_id` | `execution_venues.id` | Restrict |
| `destination_positions.venue_id` | `venue_id` | `execution_venues.id` | Restrict |
| `source_destination_links.source_trade_id` | `source_trade_id` | `reconstructed_trades.id` | Restrict |
| `outbox_events.broker_id` | `broker_id` | `brokers.id` | Restrict (nullable child) |

Fluent compound FK:

```csharp
builder.HasOne<Mt5Account>()
    .WithMany()
    .HasForeignKey(d => new { d.BrokerId, d.Login })
    .HasPrincipalKey(a => new { a.BrokerId, a.Login })
    .HasConstraintName("mt5_deals_account_fk")
    .OnDelete(DeleteBehavior.Restrict);
```

`HasPrincipalKey` requires the parent UNIQUE (already `mt5_accounts_identity_uk`). Do **not** cascade-delete raw history.

`fix_orders.cl_ord_id` → `execution_intents.cl_ord_id` is in Appendix A (parent table is not in §45). Until `execution_intents` exists, `fix_orders.cl_ord_id` is unique but **not** an FK.

---

## 7. Shared column recipes

### 7.1 Audit timestamps

Most mutable/registry tables:

| Column | Default |
|---|---|
| `created_at timestamptz NOT NULL` | `now()` |
| `updated_at timestamptz NOT NULL` | `now()`; set in `SaveChanges` interceptor |

Append-only tables: `created_at` only.

### 7.2 Correlation bag (§57)

On copy / risk / execution / shadow / outbox / audit when known:

`correlation_id uuid`, `source_broker_id uuid`, `source_login bigint`, `source_trade_id uuid`, `copy_intent_id uuid`, `risk_decision_id uuid`, `cl_ord_id text`.

### 7.3 Raw MT5 volume

Store **classic Manager `Volume()`** as `bigint` (`lots * 10_000`, A38). Optional companion `volume_lots numeric(20,8)` is **not** required in v1 — convert in Domain `VolumeConverter`. If both are stored, `volume` is authoritative.

Destination / shadow / allocation quantities are **venue units** as `numeric(20,8)`, never raw MT5 integers.

### 7.4 Money / price

`numeric(20,8)` for price, PnL, commission, swap, fees, spread, slippage. Scores: `numeric(5,2)`.

---

## 8. Per-table mappings (all 43)

Each card is the implementer’s `IEntityTypeConfiguration<T>` contract. Side: **G** global, **S** source (`broker_id` required), **D** destination, **X** cross.

---

### 8.1 `brokers` — G — `Broker`

Registry for Achiever, StarwaveFX, future source brokers.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `Code` | `code` | text | NO — `ACHIEVER`, `STARWAVEFX` |
| `Name` | `name` | text | NO |
| `ServerName` | `server_name` | text | YES |
| `IsEnabled` | `is_enabled` | boolean | NO default true |
| `Kind` | `kind` | text | NO default `MT5_SOURCE` |
| `CreatedAt` / `UpdatedAt` | `created_at` / `updated_at` | timestamptz | NO |

**Do not store** host, port, manager login, passwords here — those belong on `broker_connections` / secrets.

| Index | Unique | Name |
|---|---|---|
| `code` | YES | `brokers_code_uk` |
| `server_name` WHERE `server_name IS NOT NULL` | YES optional | `brokers_server_name_uk` |

**Gap vs current `Broker` record:** today’s type mixes connection fields (server/port/login/proxy). Split: `Broker` = registry row; `BrokerConnection` = non-secret profile.

---

### 8.2 `broker_connections` — S — `BrokerConnection`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO FK |
| `ConnectionName` | `connection_name` | text | NO — e.g. `primary` |
| `IsPrimary` | `is_primary` | boolean | NO |
| `Mode` | `mode` | text | NO — `local` / `network` |
| `Host` | `host` | text | NO |
| `Port` | `port` | integer | NO |
| `ManagerLogin` | `manager_login` | bigint | NO — login **number** only |
| `ServerName` | `server_name` | text | YES |
| `PoolSize` | `pool_size` | integer | NO |
| `ProxyEnabled` | `proxy_enabled` | boolean | NO |
| `ProxyType` | `proxy_type` | text | YES |
| `ProxyHost` | `proxy_host` | text | YES |
| `ProxyPort` | `proxy_port` | integer | YES |
| `IsEnabled` | `is_enabled` | boolean | NO |
| `CreatedAt` / `UpdatedAt` | | timestamptz | NO |

**No password columns.**

| Index | Unique | Name |
|---|---|---|
| `(broker_id, connection_name)` | YES | `broker_connections_name_uk` |
| `(broker_id) WHERE is_primary` | YES | `broker_connections_one_primary_uk` |

---

### 8.3 `mt5_groups` — S — `Mt5Group`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `GroupName` | `group_name` | text | NO — broker-local (`demo\Maxmaster`) |
| `Currency` | `currency` | text | YES |
| `CurrencyDigits` | `currency_digits` | integer | YES |
| `Company` | `company` | text | YES |
| `MarginCall` | `margin_call` | numeric(20,8) | YES |
| `MarginStopOut` | `margin_stop_out` | numeric(20,8) | YES |
| `ConnectionsAllowed` | `connections_allowed` | integer | YES |
| `LastDiscoveredAt` | `last_discovered_at` | timestamptz | YES |
| `LastSyncedAt` | `last_synced_at` | timestamptz | YES |
| `Payload` | `payload` | jsonb | YES — leftover Manager fields |

| Index | Unique | Name |
|---|---|---|
| `(broker_id, group_name)` | YES | `mt5_groups_broker_name_uk` |

Upsert on that UK. Do **not** filter which groups are stored by plan mapping (§9).

Current Domain `Mt5Group.Name` maps to `group_name` (not `name`).

---

### 8.4 `plan_group_mappings` — S — `PlanGroupMapping`

Overlay only. Several plans may share one group.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `PlanCode` | `plan_code` | text | NO — `2STEP_DEMO`, `CORE_REAL`, … |
| `Environment` | `environment` | text | NO — `demo` / `real` / `contest` |
| `GroupName` | `group_name` | text | NO |
| `IsEnabled` | `is_enabled` | boolean | NO |
| `CreatedAt` / `UpdatedAt` | | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(broker_id, plan_code, environment)` | YES | `plan_group_mappings_plan_uk` |

Do **not** unique `(broker_id, group_name)`. Compound FK to `mt5_groups`.

---

### 8.5 `mt5_accounts` — S — `Mt5Account`

Canonical source trader identity.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `GroupName` | `group_name` | text | YES |
| `Name` | `name` | text | YES |
| `Leverage` | `leverage` | integer | YES |
| `Currency` | `currency` | text | YES |
| `Balance` / `Equity` / `Margin` / `MarginFree` / `Profit` | same snake | numeric(20,8) | YES — latest snapshot denorm |
| `IsEnabled` | `is_enabled` | boolean | NO default true |
| `LastEventAt` | `last_event_at` | timestamptz | YES |
| `LastSyncedAt` | `last_synced_at` | timestamptz | YES |
| `CreatedAt` / `UpdatedAt` | | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, login)`** | YES | **`mt5_accounts_identity_uk`** |
| `(broker_id, group_name)` | no | `mt5_accounts_group_ix` |
| `(broker_id, last_event_at DESC)` | no | `mt5_accounts_last_event_ix` |

Current Domain `GroupId` is **wrong** for §45 — associate by `(broker_id, group_name)`, not a required UUID FK.

---

### 8.6 `mt5_account_snapshots` — S — `Mt5AccountSnapshot`

Append-only point-in-time book.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `SnapshotAt` | `snapshot_at` | timestamptz | NO |
| `SnapshotSource` | `snapshot_source` | text | YES — `userdata` / `accountdata` / `reconcile` |
| `Balance` / `Equity` / `Margin` / `MarginFree` / `Profit` / `Credit` | | numeric(20,8) | YES |
| `Leverage` | `leverage` | integer | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(broker_id, login, snapshot_at)` | YES | `mt5_account_snapshots_uk` |
| `(broker_id, login, snapshot_at DESC)` | no | `mt5_account_snapshots_lookup_ix` |

If two collectors can collide in the same timestamp, widen UNIQUE to include `snapshot_source` in a later migration.

---

### 8.7 `mt5_orders` — S — `Mt5Order`

Raw orders. Upsert on compound ticket. As immutable as practical.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `OrderTicket` | `order_ticket` | bigint | NO |
| `Login` | `login` | bigint | NO |
| `PositionId` | `position_id` | bigint | YES |
| `SourceSymbol` | `source_symbol` | text | YES |
| `State` | `state` | integer | YES — Manager order state |
| `Type` | `type` | integer | YES |
| `Side` | `side` | integer | YES |
| `VolumeInitial` / `VolumeCurrent` | `volume_initial` / `volume_current` | bigint | YES — classic units |
| `PriceOrder` / `PriceSL` / `PriceTP` | | numeric(20,8) | YES |
| `TimeSetup` / `TimeDone` / `TimeUpdate` | | timestamptz | YES |
| `Comment` | `comment` | text | YES |
| `Payload` | `payload` | jsonb | YES |
| `IngestedAt` | `ingested_at` | timestamptz | NO |
| `Revision` | `revision` | integer | NO default 1 |

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, order_ticket)`** | YES | **`mt5_orders_identity_uk`** |
| `(broker_id, login, time_setup)` | no | `mt5_orders_login_time_ix` |
| `(broker_id, position_id)` WHERE `position_id IS NOT NULL` | no | `mt5_orders_position_ix` |

Corrections: increment `revision` or write `audit_logs`. Do not silently drop history.

---

### 8.8 `mt5_deals` — S — `Mt5Deal`

Raw deals. Dedup metric `mt5_duplicate_deals_total` is defined against this UK.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `DealTicket` | `deal_ticket` | bigint | NO |
| `Login` | `login` | bigint | NO |
| `OrderTicket` | `order_ticket` | bigint | YES |
| `PositionId` | `position_id` | bigint | YES |
| `SourceSymbol` | `source_symbol` | text | YES |
| `Action` | `action` | smallint | NO — SDK `EnDealAction` |
| `Entry` | `entry` | smallint | YES — SDK `EnDealEntry` |
| `Volume` | `volume` | bigint | YES — classic units |
| `Price` | `price` | numeric(20,8) | YES |
| `Profit` / `Commission` / `Swap` / `Fee` | | numeric(20,8) | YES |
| `DealTime` | `deal_time` | timestamptz | YES |
| `TimeMsc` | `time_msc` | bigint | YES |
| `Comment` | `comment` | text | YES |
| `PayloadHash` | `payload_hash` | text | YES |
| `Payload` | `payload` | jsonb | YES |
| `IngestedAt` | `ingested_at` | timestamptz | NO |
| `IngestionEventId` | `ingestion_event_id` | uuid | YES — no FK until Appendix A table exists |

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, deal_ticket)`** | YES | **`mt5_deals_identity_uk`** |
| `(broker_id, login, deal_time)` | no | `mt5_deals_login_time_ix` |
| `(broker_id, order_ticket)` | no | `mt5_deals_order_ix` |
| `(broker_id, position_id)` | no | `mt5_deals_position_ix` |
| `(broker_id, source_symbol, deal_time)` | no | `mt5_deals_symbol_time_ix` |

`ON CONFLICT ON CONSTRAINT mt5_deals_identity_uk DO NOTHING` for exact-duplicate deliveries. Current Domain `PositionTicket` → column `position_id`. Current Domain `Symbol` → `source_symbol`.

---

### 8.9 `mt5_positions_current` — S — `Mt5PositionCurrent`

Live book only. **Delete** the row on close. History is deals + `reconstructed_trades`.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `PositionId` | `position_id` | bigint | NO |
| `Login` | `login` | bigint | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `Action` | `action` | smallint | YES |
| `Volume` | `volume` | bigint | NO |
| `PriceOpen` / `PriceCurrent` / `PriceSl` / `PriceTp` | | numeric(20,8) | YES |
| `Profit` / `Swap` | | numeric(20,8) | YES |
| `TimeCreate` / `TimeUpdate` | | timestamptz | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, position_id)`** | YES | **`mt5_positions_current_identity_uk`** |
| `(broker_id, login)` | no | `mt5_positions_current_login_ix` |
| `(broker_id, source_symbol)` | no | `mt5_positions_current_symbol_ix` |

Entity name is **not** `Mt5Position` (ambiguous with history). Table name is **not** `mt5_positions`.

---

### 8.10 `mt5_symbols` — S — `Mt5Symbol`

§11 alias: `mt5_symbol_metadata`. One table.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `Description` | `description` | text | YES |
| `Digits` | `digits` | integer | YES |
| `ContractSize` | `contract_size` | numeric(20,8) | YES |
| `VolumeMin` / `VolumeStep` / `VolumeMax` | | numeric(20,8) | YES — **lots**, not native ints |
| `TradeMode` | `trade_mode` | integer | YES |
| `CurrencyBase` / `CurrencyProfit` / `CurrencyMargin` | | text | YES |
| `Payload` | `payload` | jsonb | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(broker_id, source_symbol)` | YES | `mt5_symbols_uk` |

Do not unique `source_symbol` globally.

---

### 8.11 `mt5_xau_ticks` — S — `Mt5XauTick`

Optional source tick stream for exact MFE/MAE. Do not substitute cTrader quotes.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | **bigint IDENTITY** | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `TickTime` | `tick_time` | timestamptz | NO |
| `TimeMsc` | `time_msc` | bigint | NO |
| `Bid` / `Ask` / `Last` | | numeric(20,8) | YES |
| `Volume` | `volume` | bigint | YES |
| `Flags` | `flags` | integer | NO |
| `IngestSeq` | `ingest_seq` | integer | NO — collector tie-break per `(broker, symbol, time_msc, flags)` |
| `IngestedAt` | `ingested_at` | timestamptz | NO |

```csharp
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).UseIdentityAlwaysColumn();
```

| Index | Unique | Name |
|---|---|---|
| `(broker_id, source_symbol, time_msc, flags, ingest_seq)` | YES | `mt5_xau_ticks_uk` |
| `(broker_id, source_symbol, time_msc)` | no | `mt5_xau_ticks_lookup_ix` |

Partitioning (`RANGE (tick_time)` or `time_msc`) is **out of first migration**. Add later with raw SQL. `ON CONFLICT DO NOTHING` for exact duplicates.

---

### 8.12 `reconstructed_trades` — S — `ReconstructedTrade`

One row = one position lifecycle. `Id` **is** `source_trade_id` used everywhere else.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK = `source_trade_id` |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `PositionId` | `position_id` | bigint | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `Direction` | `direction` | text | NO — `LONG`/`SHORT` |
| `OpenedAt` | `opened_at` | timestamptz | NO |
| `ClosedAt` | `closed_at` | timestamptz | YES until complete |
| `EntryVwap` / `ExitVwap` | | numeric(20,8) | YES |
| `InitialVolume` / `MaxVolume` / `ClosedVolume` | | bigint | YES — native; or numeric lots — **pick native bigint** to match deals |
| `GrossRealizedPnl` / `Commission` / `Swap` / `Fees` / `NetRealizedPnl` | | numeric(20,8) | YES |
| `DealCount` / `OrderCount` | | integer | YES |
| `InitialSl` / `InitialTp` / `FinalSl` / `FinalTp` | | numeric(20,8) | YES |
| `WasScaledIn` / `WasPartialClose` / `WasAveragedDown` | | boolean | NO default false |
| `Completed` | `completed` | boolean | NO |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

Current Domain `IsCompleted` → column **`completed`** (architecture name). `PositionTicket` → `position_id`.

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, position_id)`** | YES | **`reconstructed_trades_position_uk`** |
| `(broker_id, login, closed_at)` | no | `reconstructed_trades_login_closed_ix` |
| `(broker_id, login, completed, canonical_symbol)` | no | `reconstructed_trades_count_ix` |
| `(broker_id, canonical_symbol, closed_at)` | no | `reconstructed_trades_symbol_closed_ix` |

First-3-trade counting: `completed AND canonical_symbol = 'XAUUSD'`. Position-ticket reuse after close is an A20 open question — do **not** weaken this UK in v1.

---

### 8.13 `canonical_instruments` — G — `CanonicalInstrument`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `CanonicalSymbol` | `canonical_symbol` | text | NO — seed `XAUUSD` |
| `Description` | `description` | text | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `canonical_symbol` | YES | `canonical_instruments_symbol_uk` |

Current Domain `Symbol` → `canonical_symbol`.

Seed in migration: `INSERT … XAUUSD` (idempotent).

---

### 8.14 `source_symbol_mappings` — S — `SourceSymbolMapping`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `SourceSymbol` | `source_symbol` | text | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | NO |
| `IsEnabled` | `is_enabled` | boolean | NO |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

Current Domain `CanonicalInstrumentId` is acceptable as **additional** FK, but the architecture identity is the **symbol string**. Map both: required `canonical_symbol` + optional `canonical_instrument_id`.

| Index | Unique | Name |
|---|---|---|
| `(broker_id, source_symbol)` | YES | `source_symbol_mappings_uk` |
| `(canonical_symbol, broker_id)` | no | `source_symbol_mappings_canon_ix` |

Never assume the string `XAUUSD` on the wire.

---

### 8.15 `trader_feature_snapshots` — S — `TraderFeatureSnapshot`

A22 persist contract + A20 UK.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `CompletedTradeCount` | `completed_trade_count` | integer | NO — A22 `n` |
| `AsOf` | `as_of` | timestamptz | NO |
| `Window` | `window` | text | NO — `FIRST3` / `EXPANDING` |
| `FeatureSchemaVersion` | `feature_schema_version` | text | NO — A22 `score_version` |
| `PriceSource` | `price_source` | text | NO |
| `FeatureQuality` | `feature_quality` | text | NO |
| `MfeMaeUsed` | `mfe_mae_used` | boolean | NO |
| `Components` | `components` | jsonb | NO — A22 `component_json` |
| `CreatedAt` | `created_at` | timestamptz | NO |

Wide numeric feature columns from A22 (`net`, `pf`, `lot_cv`, …) may live **inside** `components` jsonb in v1 **or** as explicit `numeric` columns. Recommended: jsonb for the full breakdown + the UK columns above. Do not invent a second snapshot table.

| Index | Unique | Name |
|---|---|---|
| `(broker_id, login, completed_trade_count, feature_schema_version)` | YES | `trader_feature_snapshots_uk` |
| `(broker_id, login, as_of DESC)` | no | `trader_feature_snapshots_lookup_ix` |

Leakage: a trade-#3 row may only use data available at that close (§20).

---

### 8.16 `trader_scores` — S — `TraderScore`

**Decision (ADR, this file):** one **wide current row** per trader (A22 + current Domain), not one row per `score_kind`. UNIQUE collapses to `(broker_id, login)` as A20 allows.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `CompletedTradeCount` | `completed_trade_count` | integer | NO |
| `AsOf` | `as_of` | timestamptz | NO |
| `ScoreVersion` | `score_version` | text | NO |
| `Window` | `window` | text | NO default `EXPANDING` |
| `RiskScore` | `risk_score` | numeric(5,2) | NO |
| `BehaviorScore` | `behavior_score` | numeric(5,2) | NO |
| `EarlyQualityScore` | `early_quality_score` | numeric(5,2) | NO |
| `MlProbability` | `ml_probability` | numeric(8,6) | YES |
| `SevereRisk` | `severe_risk` | boolean | NO |
| `LastEvent` | `last_event` | text | YES |
| `LastScoredAt` | `last_scored_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

Do **not** persist `CurrentState` on this table as the authority — `trader_states` is the state SoT. Optional denorm `state text` is allowed if it is always written in the same transaction as `trader_states`.

| Index | Unique | Name |
|---|---|---|
| `(broker_id, login)` | YES | `trader_scores_uk` |

If a later version needs per-kind rows, that is a **new versioned migration**, not a second meaning of this table.

---

### 8.17 `trader_score_history` — S — `TraderScoreHistory`

Append-only. Replay of the same close must hit the UK, not update-in-place.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `CompletedTradeCount` | `completed_trade_count` | integer | NO |
| `ScoreKind` | `score_kind` | text | NO — `risk` / `behavior` / `early_quality` / `ml_probability` / `bundle` |
| `ModelVersionId` | `model_version_id` | uuid | NO — sentinel `00000000-0000-0000-0000-0000000000ba` for baseline |
| `Score` | `score` | numeric(8,6) | YES if `score_kind=bundle` |
| `RiskScore` / `BehaviorScore` / `EarlyQualityScore` | | numeric(5,2) | YES — fill when `score_kind='bundle'` |
| `State` | `state` | text | YES |
| `Window` | `window` | text | NO |
| `ScoreVersion` | `score_version` | text | NO |
| `ScoredAt` | `scored_at` | timestamptz | NO |
| `Payload` | `payload` | jsonb | YES |

**NULL handling:** do not leave `model_version_id` NULL (breaks UNIQUE). Use the baseline sentinel UUID. Alternatively `NULLS NOT DISTINCT` (PG 15+) — pick **sentinel** so EF 8 does not need a custom unique filter.

| Index | Unique | Name |
|---|---|---|
| `(broker_id, login, completed_trade_count, score_kind, model_version_id)` | YES | `trader_score_history_uk` |
| `(broker_id, login, scored_at DESC)` | no | `trader_score_history_lookup_ix` |

Current Domain type (`TraderScoreId` + three scores, no broker/login) is **not** mappable as-is. Replace the record shape before configuring.

**v1 writer:** insert one `score_kind='bundle'` row per compute (wide scores). That satisfies A22 “append-only copy of every official compute” without three nearly-identical rows.

---

### 8.18 `trader_states` — S — `TraderStateRecord`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `State` | `state` | text | NO |
| `PrevState` | `prev_state` | text | YES |
| `Reason` | `reason` | text | YES |
| `CompletedTradeCount` | `completed_trade_count` | integer | YES |
| `AsOf` | `as_of` | timestamptz | YES |
| `ChangedAt` | `changed_at` | timestamptz | NO |
| `Actor` | `actor` | text | NO — `system:baseline.v1` / `user:<id>` |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| **`(broker_id, login)`** | YES | **`trader_states_identity_uk`** |

```csharp
builder.ToTable(t => t.HasCheckConstraint(
    "trader_states_state_ck",
    "state IN ('INSUFFICIENT_DATA','EARLY_SCORE','WATCH','SHADOW','LIVE_CANDIDATE','LIVE','PAUSED','RISK_BLOCKED','DISQUALIFIED')"));
```

State transitions also write `audit_logs` / `system_events`. Do not overwrite without an audit row.

---

### 8.19 `trader_risk_flags` — S — `TraderRiskFlag`

**Current** flags. History goes to `risk_events`.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `FlagCode` | `flag_code` | text | NO — `martingale`, `averaging_down`, … |
| `Severity` | `severity` | text | NO — `watch` / `severe` |
| `IsActive` | `is_active` | boolean | NO |
| `OpenedAt` / `EndedAt` | | timestamptz | ended YES |
| `OpenedN` / `EndedN` | | integer | ended YES |
| `Evidence` | `evidence` | jsonb | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(broker_id, login, flag_code)` | YES | `trader_risk_flags_uk` |
| `(flag_code) WHERE is_active` | no | `trader_risk_flags_active_ix` |

---

### 8.20 `model_versions` — G — `ModelVersion`

Insert-only versions. Promotion is audited. No self-promotion (§71).

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ModelName` | `model_name` | text | NO |
| `Version` | `version` | text | NO |
| `IsProduction` | `is_production` | boolean | NO |
| `ArtifactUri` | `artifact_uri` | text | YES — not a secret blob |
| `TrainedAt` | `trained_at` | timestamptz | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |
| `Notes` | `notes` | text | YES |

| Index | Unique | Name |
|---|---|---|
| `(model_name, version)` | YES | `model_versions_uk` |
| `(model_name) WHERE is_production` | YES | `model_versions_one_prod_uk` |

---

### 8.21 `model_predictions` — S — `ModelPrediction`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ModelVersionId` | `model_version_id` | uuid | NO |
| `BrokerId` | `broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `CompletedTradeCount` | `completed_trade_count` | integer | NO |
| `Probability` | `probability` | numeric(8,6) | NO |
| `Label` | `label` | smallint | YES |
| `AsOf` | `as_of` | timestamptz | NO |
| `FeaturesRef` | `features_ref` | uuid | YES — snapshot id |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(model_version_id, broker_id, login, completed_trade_count)` | YES | `model_predictions_uk` |

Must not include future features (§20).

---

### 8.22 `model_evaluations` — G — `ModelEvaluation`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ModelVersionId` | `model_version_id` | uuid | NO |
| `EvaluationSplit` | `evaluation_split` | text | NO — `validation` / `final_test` / `live_shadow` |
| `MetricSetVersion` | `metric_set_version` | text | NO |
| `Metrics` | `metrics` | jsonb | NO — top 1/5/10/20%, CVaR, … |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(model_version_id, evaluation_split, metric_set_version)` | YES | `model_evaluations_uk` |

`broker_id` is **not** in the identity. Optional slice inside `metrics`.

---

### 8.23 `shadow_orders` — X — `ShadowOrder`

§24 `shadow_copy_order`. Persist before simulated send.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ShadowClOrdId` | `shadow_cl_ord_id` | text | NO — `SHDW-` prefix |
| `CopyIntentId` | `copy_intent_id` | uuid | YES |
| `SourceBrokerId` | `source_broker_id` | uuid | NO |
| `SourceLogin` | `source_login` | bigint | NO |
| `SourceTradeId` | `source_trade_id` | uuid | YES |
| `SourceEventId` | `source_event_id` | text | NO |
| `DestinationAccount` | `destination_account` | text | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | NO |
| `DestinationSymbolId` | `destination_symbol_id` | text | YES |
| `ActionClass` | `action_class` | text | NO |
| `Side` | `side` | text | NO |
| `RequestedQty` / `RemainingQty` | | numeric(20,8) | NO |
| `ExpectedDestinationPx` / `SourcePrice` | | numeric(20,8) | YES |
| `DecisionQuoteId` | `decision_quote_id` | uuid | YES |
| `Status` | `status` | text | NO |
| `RejectReason` | `reject_reason` | text | YES |
| `CorrelationId` | `correlation_id` | uuid | YES |
| `CreatedAt` / `SimSendAt` / `CompletedAt` | | timestamptz | sim/completed YES |

| Index | Unique | Name |
|---|---|---|
| `shadow_cl_ord_id` | YES | `shadow_orders_clord_uk` |
| `(source_broker_id, source_login, source_event_id, action_class)` | YES | `shadow_orders_idem_uk` |
| `copy_intent_id` WHERE NOT NULL | YES optional | `shadow_orders_intent_uk` |
| `(source_broker_id, source_login, created_at)` | no | `shadow_orders_src_ix` |

`source_broker_id` is uuid (A20), **not** text (A24 type slip).

---

### 8.24 `shadow_fills` — X — `ShadowFill`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ShadowOrderId` | `shadow_order_id` | uuid | NO |
| `ShadowPositionId` | `shadow_position_id` | uuid | YES |
| `FillSeq` | `fill_seq` | integer | NO |
| `Qty` / `Price` | | numeric(20,8) | NO |
| `FillQuoteId` | `fill_quote_id` | uuid | YES — required except UNPRICED close (no row) |
| `FillQuality` | `fill_quality` | text | NO — `LIVE` / `STALE_QUOTE` |
| `Liquidity` | `liquidity` | text | NO default `TAKER` |
| `Commission` | `commission` | numeric(20,8) | YES |
| `CommissionCcy` | `commission_ccy` | text | YES |
| `ModelVersion` | `model_version` | text | YES |
| `FilledAt` | `filled_at` | timestamptz | NO |
| `AssumptionNotes` | `assumption_notes` | text | YES |
| `SourcePrice` | `source_price` | numeric(20,8) | YES |
| `SignedSlippage` / `AdverseSlippage` | | numeric(20,8) | YES — §24 `source_vs_shadow_slippage` folded here |
| `QuoteAgeMs` / `SignalAgeMs` | | integer | YES |
| `SourceBrokerId` | `source_broker_id` | uuid | YES |
| `SourceTradeId` | `source_trade_id` | uuid | YES |

| Index | Unique | Name |
|---|---|---|
| `(shadow_order_id, fill_seq)` | YES | `shadow_fills_uk` |
| `(source_broker_id, source_trade_id)` | no | `shadow_fills_src_ix` |

Do **not** create table `source_vs_shadow_slippage`.

---

### 8.25 `shadow_positions` — X — `ShadowPosition`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `SourceBrokerId` | `source_broker_id` | uuid | NO |
| `SourceLogin` | `source_login` | bigint | NO |
| `SourceTradeId` | `source_trade_id` | uuid | NO |
| `DestinationSymbolId` | `destination_symbol_id` | text | YES |
| `Side` | `side` | text | NO |
| `OpenQty` / `MaxQty` / `ClosedQty` | | numeric(20,8) | NO |
| `EntryVwap` / `ExitVwap` | | numeric(20,8) | exit YES |
| `OpenedAt` / `ClosedAt` | | timestamptz | closed YES |
| `Status` | `status` | text | NO |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `(source_broker_id, source_trade_id)` | YES | `shadow_positions_source_uk` |
| `(source_broker_id, source_login)` WHERE open | no | `shadow_positions_open_ix` |

---

### 8.26 `shadow_performance` — X — `ShadowPerformance`

Book rollup (A24). Grain is explicit.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `SourceBrokerId` | `source_broker_id` | uuid | NO |
| `Login` | `login` | bigint | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | YES |
| `PeriodGrain` | `period_grain` | text | NO — `LIFETIME` / `DAY` / `TRADE` |
| `PeriodStart` | `period_start` | timestamptz | NO — for `TRADE`, use trade open/close as the grain key |
| `SourceTradeId` | `source_trade_id` | uuid | YES — required when grain=`TRADE` |
| `Realized` / `Unrealized` / `Net` / `Commission` / `Swap` / `MaxDrawdown` | | numeric(20,8) | YES |
| `TradeCount` / `WinCount` / `LossCount` | | integer | YES |
| `GrossXauQty` / `NetXauQty` | | numeric(20,8) | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(source_broker_id, login, period_grain, period_start)` | YES | `shadow_performance_uk` |

For `TRADE` grain, implementers **must** set `period_start` to a stable timestamp derived from `source_trade_id` (e.g. `opened_at`) so the UK does not need a fifth column in v1. Do not create `shadow_pnl` as a second table; per-position marks can be rows with `period_grain='TRADE'`.

Check: `period_grain IN ('LIFETIME','DAY','TRADE')`.

---

### 8.27 `copy_intents` — X — `CopyIntent`

Created from a source event **before** risk and **before** FIX. Must expire (§63).

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK = `copy_intent_id` |
| `SourceBrokerId` | `source_broker_id` | uuid | NO |
| `SourceLogin` | `source_login` | bigint | NO |
| `SourceTradeId` | `source_trade_id` | uuid | NO |
| `SourceEventId` | `source_event_id` | text | NO |
| `Action` | `action` | text | NO |
| `Status` | `status` | text | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | NO |
| `SourceEventTime` / `CollectorReceiveTime` / `DecisionTime` | | timestamptz | YES |
| `ExpiresAt` | `expires_at` | timestamptz | NO |
| `MaxSignalAge` | `max_signal_age` | interval or bigint ms | NO — store `max_signal_age_ms integer` |
| `CorrelationId` | `correlation_id` | uuid | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(source_broker_id, source_login, source_trade_id, source_event_id, action)` | YES | `copy_intents_idem_uk` |
| `(status, expires_at)` | no | `copy_intents_expiry_ix` |
| `(source_broker_id, source_login, created_at DESC)` | no | `copy_intents_src_ix` |

Expire; do not rewrite identity.

---

### 8.28 `copy_allocations` — X — `CopyAllocation`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `CopyIntentId` | `copy_intent_id` | uuid | NO |
| `VenueId` | `venue_id` | uuid | YES |
| `DestinationAccount` | `destination_account` | text | NO |
| `SourceVolumeNative` | `source_volume_native` | bigint | YES |
| `CanonicalNotional` | `canonical_notional` | numeric(20,8) | YES |
| `DestinationQty` | `destination_qty` | numeric(20,8) | NO |
| `AllocationReason` | `allocation_reason` | text | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(copy_intent_id, destination_account)` | YES | `copy_allocations_uk` |

---

### 8.29 `risk_decisions` — X — `RiskDecision`

Risk is the final authority. Multiple decisions per intent allowed.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK = `risk_decision_id` |
| `CopyIntentId` | `copy_intent_id` | uuid | NO |
| `DecisionSeq` | `decision_seq` | integer | NO |
| `Decision` | `decision` | text | NO |
| `IsFinal` | `is_final` | boolean | NO |
| `SourceBrokerId` / `SourceLogin` | | uuid / bigint | YES |
| `RequestedQtyIn` / `ApprovedQtyOut` | | numeric(20,8) | YES |
| `PrimaryReason` | `primary_reason` | text | YES |
| `ReasonCodes` | `reason_codes` | jsonb | YES — text[] also acceptable |
| `QuoteAgeMs` / `SignalAgeMs` | | integer | YES |
| `Spread` / `PriceDeviation` | | numeric(20,8) | YES |
| `QuoteId` | `quote_id` | uuid | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(copy_intent_id, decision_seq)` | YES | `risk_decisions_uk` |
| `(copy_intent_id) WHERE is_final` | YES optional | `risk_decisions_final_uk` |
| `(decision, created_at DESC)` | no | `risk_decisions_decision_ix` |
| `(source_broker_id, source_login, created_at DESC)` | no | `risk_decisions_src_ix` |

---

### 8.30 `risk_events` — X — `RiskEvent`

Kill-switch, pause/resume, flatten, limit-breach. Append-only. **No UNIQUE.**

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `EventType` | `event_type` | text | NO — `STOP_NEW_EXECUTION` ≠ `EMERGENCY_FLATTEN` |
| `SourceBrokerId` / `SourceLogin` | | | YES — NULL for venue-global |
| `VenueId` | `venue_id` | uuid | YES |
| `Actor` | `actor` | text | YES |
| `Payload` | `payload` | jsonb | YES |
| `OccurredAt` | `occurred_at` | timestamptz | NO |
| `CorrelationId` | `correlation_id` | uuid | YES |

| Index | Unique | Name |
|---|---|---|
| `(event_type, occurred_at DESC)` | no | `risk_events_type_ix` |
| `(source_broker_id, source_login, occurred_at DESC)` | no | `risk_events_src_ix` |

Do not conflate the two kill-switch modes into one boolean column.

---

### 8.31 `execution_venues` — D — `ExecutionVenue`

Pepperstone / cServer is an **external execution venue**, not a source broker.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK = `venue_id` |
| `VenueCode` | `venue_code` | text | NO — `PEPPERSTONE_CSERVER` |
| `DisplayName` | `display_name` | text | NO |
| `FixAccountId` | `fix_account_id` | text | YES — `1369850` |
| `Kind` | `kind` | text | NO default `CTRADER_CSERVER` |
| `IsEnabled` | `is_enabled` | boolean | NO |
| `CreatedAt` / `UpdatedAt` | | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `venue_code` | YES | `execution_venues_code_uk` |
| `fix_account_id` WHERE NOT NULL | YES optional | `execution_venues_account_uk` |

**No FIX password.**

---

### 8.32 `destination_symbols` — D — `DestinationSymbol`

Persisted Security List. Do not hardcode instrument IDs.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `VenueId` | `venue_id` | uuid | NO |
| `InstrumentId` | `instrument_id` | text | NO — cTrader numeric id as text |
| `VenueSymbol` | `venue_symbol` | text | NO — tag 55, not assumed `XAUUSD` |
| `CanonicalSymbol` | `canonical_symbol` | text | YES |
| `Digits` | `digits` | integer | YES |
| `QtyMin` / `QtyStep` / `QtyMax` | | numeric(20,8) | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(venue_id, instrument_id)` | YES | `destination_symbols_instr_uk` |
| `(venue_id, venue_symbol)` | YES | `destination_symbols_name_uk` |
| `(canonical_symbol, venue_id)` | no | `destination_symbols_canon_ix` |

---

### 8.33 `destination_quotes` — D — `DestinationQuote`

**Latest** quote only. Upsert in place. No history table in §45.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `VenueId` | `venue_id` | uuid | NO |
| `InstrumentId` | `instrument_id` | text | NO |
| `Bid` / `Ask` | | numeric(20,8) | NO |
| `Spread` | `spread` | numeric(20,8) | YES — may be stored generated |
| `QuoteReceivedAt` | `quote_received_at` | timestamptz | NO |
| `VenueTimestamp` | `venue_timestamp` | timestamptz | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `(venue_id, instrument_id)` | YES | `destination_quotes_uk` |

`quote_age` is computed at read (`now() - quote_received_at`), not a stored column (stale the moment it is written). Risk rejects stale quotes (§31).

---

### 8.34 `fix_sessions` — D — `FixSession`

Independent QUOTE and TRADE objects. Do **not** share a sequence counter.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `VenueId` | `venue_id` | uuid | NO |
| `SessionQualifier` | `session_qualifier` | text | NO — `QUOTE` / `TRADE` |
| `SessionKey` | `session_key` | text | YES — `pepperstone-1369850-QUOTE` |
| `SenderCompId` / `TargetCompId` | | text | NO |
| `SenderSubId` / `TargetSubId` | | text | YES — configurable, do not guess |
| `Status` | `status` | text | NO |
| `IncomingSeq` / `OutgoingSeq` | | bigint | YES — application-visible; wire reset is RoE |
| `LastInboundAt` / `LastOutboundAt` | | timestamptz | YES |
| `ReconnectCount` | `reconnect_count` | integer | NO default 0 |
| `OwnerInstance` | `owner_instance` | text | YES |
| `FencingToken` | `fencing_token` | uuid | YES — TRADE leadership |
| `LeaseUntil` | `lease_until` | timestamptz | YES |
| `LastLogoutText` | `last_logout_text` | text | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `(venue_id, session_qualifier)` | YES | `fix_sessions_uk` |
| `(venue_id, sender_comp_id, target_comp_id, session_qualifier)` | YES | `fix_sessions_comp_uk` |

```csharp
builder.ToTable(t => t.HasCheckConstraint(
    "fix_sessions_qualifier_ck",
    "session_qualifier IN ('QUOTE','TRADE')"));
```

**No password / RawData columns.** Database remains authority for execution state; Redis lease if used must carry the same `fencing_token`.

Stub `FixSessionStates` is **not** this table. Name it `FixSession` / `fix_sessions`.

---

### 8.35 `fix_session_events` — D — `FixSessionEvent`

Logon, logout, heartbeat miss, resend, reject, leadership change. Append-only. **No UNIQUE.**

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `SessionId` | `session_id` | uuid | NO |
| `VenueId` | `venue_id` | uuid | NO |
| `SessionQualifier` | `session_qualifier` | text | NO |
| `EventType` | `event_type` | text | NO |
| `Detail` | `detail` | text | YES |
| `Payload` | `payload` | jsonb | YES |
| `OccurredAt` | `occurred_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(session_id, occurred_at DESC)` | no | `fix_session_events_session_ix` |
| `(venue_id, session_qualifier, occurred_at DESC)` | no | `fix_session_events_venue_ix` |

---

### 8.36 `fix_orders` — D — `FixOrder`

Venue-visible order. Same `cl_ord_id` as the (appendix) execution intent.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ClOrdId` | `cl_ord_id` | text | NO |
| `VenueId` | `venue_id` | uuid | NO |
| `DestOrderId` | `dest_order_id` | text | YES — after ack |
| `DestinationAccount` | `destination_account` | text | NO |
| `SourceBrokerId` / `SourceLogin` / `SourceTradeId` | | | source NO on copy-originated |
| `CanonicalSymbol` | `canonical_symbol` | text | YES |
| `Side` | `side` | text | NO |
| `RequestedQty` / `CumQty` / `LeavesQty` | | numeric(20,8) | YES |
| `Status` | `status` | text | NO |
| `FencingToken` | `fencing_token` | uuid | YES |
| `CreatedAt` / `UpdatedAt` | | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `cl_ord_id` | YES | `fix_orders_clord_uk` |
| `(venue_id, dest_order_id) WHERE dest_order_id IS NOT NULL` | YES | `fix_orders_dest_uk` |
| `(venue_id, destination_account, status)` | no | `fix_orders_acct_ix` |

Never retry `NewOrderSingle` on the same `cl_ord_id`.

---

### 8.37 `fix_execution_reports` — D — `FixExecutionReport`

Every ER is durable. Dedup by venue ExecID (§70.5).

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `VenueId` | `venue_id` | uuid | NO |
| `ExecId` | `exec_id` | text | NO |
| `ClOrdId` | `cl_ord_id` | text | YES |
| `DestOrderId` | `dest_order_id` | text | YES |
| `DestinationPositionId` | `destination_position_id` | text | YES |
| `ExecType` | `exec_type` | text | YES |
| `OrdStatus` | `ord_status` | text | YES |
| `LastQty` / `LastPx` / `LeavesQty` / `CumQty` | | numeric(20,8) | YES |
| `TransactTime` | `transact_time` | timestamptz | YES |
| `RawMsgType` | `raw_msg_type` | text | YES |
| `Payload` | `payload` | jsonb | YES — **redacted** FIX, never tag 554 |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(venue_id, exec_id)` | YES | `fix_execution_reports_exec_uk` |
| `(cl_ord_id, transact_time)` | no | `fix_execution_reports_clord_ix` |
| `(venue_id, dest_order_id)` | no | `fix_execution_reports_order_ix` |
| `(exec_type, created_at DESC)` | no | `fix_execution_reports_type_ix` |

`exec_id` uniqueness is **per venue**, not global.

---

### 8.38 `destination_positions` — D — `DestinationPosition`

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `VenueId` | `venue_id` | uuid | NO |
| `DestinationAccount` | `destination_account` | text | NO |
| `DestinationPositionId` | `destination_position_id` | text | NO |
| `CanonicalSymbol` | `canonical_symbol` | text | YES |
| `InstrumentId` | `instrument_id` | text | YES |
| `Side` | `side` | text | NO |
| `Quantity` | `quantity` | numeric(20,8) | NO |
| `IsOpen` | `is_open` | boolean | NO |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `(venue_id, destination_account, destination_position_id)` | YES | `destination_positions_uk` |
| `(venue_id, canonical_symbol) WHERE is_open` | no | `destination_positions_open_ix` |

Do not unique quantity. Postgres SoT — never Redis.

---

### 8.39 `source_destination_links` — X — `SourceDestinationLink`

Explicit mapping: reconstructed trade → dest orders → dest position. One source event is **not** forever one dest order.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `SourceBrokerId` | `source_broker_id` | uuid | NO |
| `SourceLogin` | `source_login` | bigint | YES |
| `SourceTradeId` | `source_trade_id` | uuid | NO |
| `LinkRole` | `link_role` | text | NO — `ENTRY` / `SCALE_IN` / `PARTIAL_CLOSE` / `CLOSE` / `REVERSAL` |
| `ExecutionIntentId` | `execution_intent_id` | uuid | NO — no FK until Appendix A |
| `ClOrdId` | `cl_ord_id` | text | YES |
| `VenueId` | `venue_id` | uuid | YES |
| `DestinationAccount` | `destination_account` | text | YES |
| `DestinationPositionId` | `destination_position_id` | text | YES |
| `CreatedAt` | `created_at` | timestamptz | NO |

| Index | Unique | Name |
|---|---|---|
| `(source_broker_id, source_trade_id, link_role, execution_intent_id)` | YES | `source_destination_links_uk` |
| `(venue_id, destination_position_id)` | no | `source_destination_links_dest_ix` |
| `(source_broker_id, source_login)` | no | `source_destination_links_src_ix` |

Do **not** unique `(source_broker_id, source_trade_id)` alone.

---

### 8.40 `sync_checkpoints` — S+D — `SyncCheckpoint`

Per-stream cursors. Two brokers must not share a deals cursor.

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `ScopeType` | `scope_type` | text | NO — `BROKER` / `VENUE` / `GLOBAL` |
| `ScopeId` | `scope_id` | uuid | NO — `broker_id` or `venue_id` |
| `StreamName` | `stream_name` | text | NO — `groups`, `accounts`, `deals_backfill`, `deals_live`, `orders_history`, `positions_reconcile`, `security_list`, `order_mass_status` |
| `CursorFrom` / `CursorTo` | | timestamptz | YES |
| `LastEntityTicket` | `last_entity_ticket` | bigint | YES |
| `PayloadHash` | `payload_hash` | text | YES |
| `Status` | `status` | text | NO — `running` / `ok` / `failed` |
| `Error` | `error` | text | YES |
| `UpdatedAt` | `updated_at` | timestamptz | NO |
| `xmin` | `xmin` | xid | concurrency |

| Index | Unique | Name |
|---|---|---|
| `(scope_type, scope_id, stream_name)` | YES | `sync_checkpoints_uk` |

When `scope_type='BROKER'`, `scope_id` **is** `broker_id`. Do not add a second `broker_id` column unless you also CHECK they match. Recommended: only `scope_id`. Writer sets it from `BrokerId`.

Do not create `fix_checkpoints`. Same table, `scope_type='VENUE'`.

Advance checkpoint **only after** successful idempotent upsert of the corresponding raw rows (§12).

---

### 8.41 `outbox_events` — X — `OutboxEvent`

See **§5** (full mapping). Counted here so the 43-table list is complete.

---

### 8.42 `audit_logs` — G — `AuditLog`

Manual overrides, RBAC, config changes. Append-only. **No UNIQUE.**

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | **bigint IDENTITY** | NO PK |
| `OccurredAt` | `occurred_at` | timestamptz | NO |
| `ActorId` | `actor_id` | text | YES |
| `Action` | `action` | text | NO |
| `EntityType` / `EntityId` | | text / text | YES |
| `BrokerId` / `SourceLogin` | | uuid / bigint | YES |
| `CorrelationId` | `correlation_id` | uuid | YES |
| `Details` | `details` | jsonb | YES — **redacted** |

```csharp
builder.Property(x => x.Id).UseIdentityAlwaysColumn();
```

| Index | Unique | Name |
|---|---|---|
| `(occurred_at DESC)` | no | `audit_logs_time_ix` |
| `(actor_id, occurred_at DESC)` | no | `audit_logs_actor_ix` |
| `(correlation_id)` | no | `audit_logs_corr_ix` |
| `(broker_id, source_login)` | no | `audit_logs_src_ix` |

Never log passwords, FIX password tags, proxy credentials.

---

### 8.43 `system_events` — G — `SystemEvent`

Platform health / mode (FIX connected, stale-source, kill-switch, ML unavailable). Append-only. **No UNIQUE.**

| Property | Column | Type | Null |
|---|---|---|---|
| `Id` | `id` | uuid | NO PK |
| `EventType` | `event_type` | text | NO |
| `Severity` | `severity` | text | NO — `info` / `warn` / `error` / `critical` |
| `Payload` | `payload` | jsonb | YES |
| `OccurredAt` | `occurred_at` | timestamptz | NO |
| `CorrelationId` | `correlation_id` | uuid | YES |

| Index | Unique | Name |
|---|---|---|
| `(event_type, occurred_at DESC)` | no | `system_events_type_ix` |
| `(severity, occurred_at DESC)` | no | `system_events_sev_ix` |

Current kill-switch **state** is derived from the latest `STOP_NEW_EXECUTION` / `EMERGENCY_FLATTEN` (`risk_events` or this table). Do **not** invent `system_flags` in this mapping.

---

## 9. SaveChanges interceptor (shared)

Implement `ISaveChangesInterceptor` in Infrastructure:

1. Set `updated_at = DateTimeOffset.UtcNow` on entities that have it.
2. Reject any entity whose CLR type maps a property named `Password`, `ProxyPassword`, `FixPassword`, `RawData`.
3. Optional: auto-enqueue is **not** done here — outbox rows are explicit so the `dedupe_key` stays producer-owned.

Do not put business reconstruction in the interceptor.

---

## 10. Upsert patterns (EF Core 8)

| Table | Pattern |
|---|---|
| Ticket raw (`mt5_deals`, `mt5_orders`) | `INSERT … ON CONFLICT ON CONSTRAINT *_identity_uk DO NOTHING` (duplicate) or `DO UPDATE` of mutable Manager fields + `revision` |
| `mt5_positions_current` | upsert on identity UK; `DELETE` on close |
| `mt5_accounts`, `mt5_groups`, `mt5_symbols`, mappings | upsert on UK |
| `trader_scores`, `trader_states`, `destination_quotes`, `sync_checkpoints` | upsert on UK |
| `outbox_events` | insert; conflict on `outbox_events_dedupe_uk` = already queued |
| History / ER / audit / ticks | insert only |

EF 8: `ExecuteUpdate` / raw SQL `ON CONFLICT` for hot ingest paths. Tracked `Add` + catch `PostgresException.SqlState == "23505"` is acceptable for low-volume registry rows.

Never use `Attach` + overwrite UUID on conflict.

---

## 11. What this mapping must not do

| Forbidden | Why |
|---|---|
| Second tables for §11/§24 aliases | A20 unification |
| Unique `login` / `deal_ticket` / `order_ticket` / `position_id` alone | §10 |
| Redis as SoT for orders, positions, balances | §5 |
| `EnsureCreated` in hosts | no migration history |
| `NpgsqlEnableLegacyTimestampBehavior` | breaks `timestamptz` |
| `double` price columns | rounding |
| Password columns | §55 |
| Kafka / extra broker on day one | §13 |
| Map Domain `TraderScore.CurrentState` as the only state row | `trader_states` is SoT |
| Call FIX or scoring inside `SaveChanges` | §12 |
| Hand-written `.mq5` / mutating product source from this report | task scope |
| Composite PK instead of surrogate + UNIQUE | breaks outbox/audit FKs |

---

## 12. Suggested implementation order (schema only)

Matches A03 / Phase gates. Still **design**, not an implementation claim.

1. Conventions + `TraderIntelligenceDbContext` + factory + naming package.
2. P0 tables: `brokers`, `broker_connections`, `mt5_groups`, `plan_group_mappings`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols`, `sync_checkpoints`, `outbox_events`, `audit_logs`.
3. Same-transaction outbox writer + `SKIP LOCKED` processor.
4. `canonical_instruments`, `source_symbol_mappings`, `reconstructed_trades`.
5. Scoring tables (`trader_*`, `model_*`).
6. Shadow tables.
7. Copy / risk + destination / FIX tables (still Postgres before any TRADE socket).
8. First versioned migration + `PostgresMigrationTests` (A10 required table list includes several of these plus Appendix A `execution_intents`).

---

## 13. Coverage checklist

| Origin | Required | In this mapping |
|---|---|---|
| §45 full initial set | 43 names | **43 / 43** specified |
| §10 compound identities | 4 laws | Unique indexes in §4.1 |
| snake_case PostgreSQL | all tables/columns | §2.2 + explicit `ToTable` / `HasDatabaseName` |
| Outbox | `outbox_events` + drain + same-commit | §5 |
| EF Core 8 + Npgsql 8.0.4 | provider pair | §2.1 |
| Product source edited | none | **none** |

---

## Appendix A — Not in §45, reserve in the same DbContext

A20 / A03 / A10 require these for a working system. They are **not** §45 tables. Map them in a follow-on configuration set (same conventions). Do not invent alternate names.

| Table | Entity | PK | UNIQUE | Why |
|---|---|---|---|---|
| `ingestion_events` | `IngestionEvent` | `id uuid` | `(broker_id, source_event_id)` | §11 raw evidence; do not overload `outbox_events` |
| `execution_intents` | `ExecutionIntent` | `id uuid` | `cl_ord_id`; `(risk_decision_id)` | §33 persist-before-send; A10 test list |
| `execution_reconciliation_runs` | `ExecutionReconciliationRun` | `id uuid` | none | §42–43 |
| `execution_reconciliation_issues` | `ExecutionReconciliationIssue` | `id uuid` | `(run_id, issue_fingerprint)` | §43 |

`execution_intents` column minimum (§33): `execution_intent_id`, `cl_ord_id`, `source_broker_id`, `source_login`, `source_trade_id`, `source_event_id`, `destination_account`, `canonical_symbol`, `side`, `requested_quantity`, `created_at`, `status`. After this table exists, add FK `fix_orders.cl_ord_id` → `execution_intents.cl_ord_id` and `source_destination_links.execution_intent_id` → `execution_intents.id`.

---

## Appendix B — File / type inventory for implementers

```text
src/Infrastructure/Persistence/
  TraderIntelligenceDbContext.cs
  TraderIntelligenceDbContextFactory.cs
  Conventions/PgModelConventions.cs
  Converters/PgUInt64.cs
  Converters/EnumTextConverters.cs
  Interceptors/UtcAuditInterceptor.cs
  Configurations/
    BrokerConfiguration.cs
    BrokerConnectionConfiguration.cs
    Mt5GroupConfiguration.cs
    PlanGroupMappingConfiguration.cs
    Mt5AccountConfiguration.cs
    Mt5AccountSnapshotConfiguration.cs
    Mt5OrderConfiguration.cs
    Mt5DealConfiguration.cs
    Mt5PositionCurrentConfiguration.cs
    Mt5SymbolConfiguration.cs
    Mt5XauTickConfiguration.cs
    ReconstructedTradeConfiguration.cs
    CanonicalInstrumentConfiguration.cs
    SourceSymbolMappingConfiguration.cs
    TraderFeatureSnapshotConfiguration.cs
    TraderScoreConfiguration.cs
    TraderScoreHistoryConfiguration.cs
    TraderStateRecordConfiguration.cs
    TraderRiskFlagConfiguration.cs
    ModelVersionConfiguration.cs
    ModelPredictionConfiguration.cs
    ModelEvaluationConfiguration.cs
    ShadowOrderConfiguration.cs
    ShadowFillConfiguration.cs
    ShadowPositionConfiguration.cs
    ShadowPerformanceConfiguration.cs
    CopyIntentConfiguration.cs
    CopyAllocationConfiguration.cs
    RiskDecisionConfiguration.cs
    RiskEventConfiguration.cs
    ExecutionVenueConfiguration.cs
    DestinationSymbolConfiguration.cs
    DestinationQuoteConfiguration.cs
    FixSessionConfiguration.cs
    FixSessionEventConfiguration.cs
    FixOrderConfiguration.cs
    FixExecutionReportConfiguration.cs
    DestinationPositionConfiguration.cs
    SourceDestinationLinkConfiguration.cs
    SyncCheckpointConfiguration.cs
    OutboxEventConfiguration.cs
    AuditLogConfiguration.cs
    SystemEventConfiguration.cs
  Migrations/                      # generated, versioned, never EnsureCreated
```

One configuration class per table. Do not put all 43 in `OnModelCreating`.

---

## Appendix C — Acceptance probes (for the future migration test)

When implemented, `PostgresMigrationTests.Required_tables_exist` must see at least:

```text
brokers
mt5_deals
mt5_orders
mt5_positions_current
mt5_accounts
outbox_events
sync_checkpoints
reconstructed_trades
copy_intents
risk_decisions
fix_sessions
fix_orders
fix_execution_reports
destination_positions
```

And these unique indexes must exist (query `pg_indexes` / `pg_constraint`):

```text
mt5_accounts_identity_uk
mt5_deals_identity_uk
mt5_orders_identity_uk
mt5_positions_current_identity_uk
reconstructed_trades_position_uk
outbox_events_dedupe_uk
```

Column names in `information_schema.columns` must be **snake_case**. A single `DealTicket` or `BrokerId` column name is a **FAIL**.

---

No product source was modified. This file is the A61 EF Core 8 + Npgsql mapping contract only.
