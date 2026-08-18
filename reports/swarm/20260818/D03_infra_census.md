# D03 — `src/Infrastructure` census (current tree)

| Field | Value |
|---|---|
| Agent | D03 (Infrastructure census, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (hashes + full source read) |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\src\Infrastructure` |
| Product source modified | **No.** This report is the only write. |
| Supersedes for *tree / file identity* | `A03_infrastructure_audit.md` (empty `Class1` era — **stale**) |
| Does **not** supersede | B03 / B19 / B21 (table/type gap), C05 (DI), C06 (keys), C27 (Redis), C29 (migrations), C35 (Mt5 layering), C36 (query perf), C58 (outbox) |
| Law | Architecture v2 §§5, 10–13, 44–45, 60, 72.3; A20 catalog; A61 EF contract |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**Not empty. Not production. Five C# types, 20 `DbSet`s, demo EF InMemory by default.**

`Class1.cs` is gone. `TraderIntelligence.Infrastructure` is a compiling demo persistence slice: one `DbContext`, one store adapter, one dashboard query adapter, one static seeder, one DI extension. It implements the current Application ports (`ITradingStore` 8/8, `IDashboardQueries` 7/7). It is **not** the A20/A61 catalog, **not** a migrated PostgreSQL schema, and **not** a Redis coordination layer.

| Metric | Measured | Target | Class |
|---|---|---|---|
| Product files (exclude `bin/` `obj/`) | **6** (5 `.cs` + 1 `.csproj`) | Persistence + 43 configs + factory + Redis + Outbox | `EXISTS_NEEDS_REFACTOR` |
| Product `.cs` physical lines | **774** | — | — |
| Public types | **5** | — | — |
| `Class1.cs` | **absent** | leftover deleted | `EXISTS_AND_GOOD` |
| `DbSet` / `Entity<T>` | **20 / 20** (1:1 Domain entities) | 43 §45 + A20 extras | `EXISTS_NEEDS_REFACTOR` |
| §45 table **names** present | **18 / 43** (41.9%) | 43 | `MISSING` remainder |
| A20 union names present | **19 / 47** (`execution_intents` extra vs §45; `kill_switches` not in A20) | 47 | `MISSING` remainder |
| `IEntityTypeConfiguration<T>` | **0** (`Persistence/Configurations/` empty) | 43 | `MISSING` |
| EF `Migrations/` | **0** | versioned, never `EnsureCreated` | `MISSING` |
| `IDesignTimeDbContextFactory` / `Migrate()` | **0** | required for `dotnet ef` | `MISSING` |
| Default provider (empty / `<SECRET>` CS) | `UseInMemoryDatabase("trader-intelligence")` | PostgreSQL 15+/16 | `UNSAFE` as SoT |
| `UseNpgsql` | present when CS is non-empty and not `<SECRET>` | + snake_case + retry + `Migrate` | `EXISTS_NEEDS_REFACTOR` |
| `StackExchange.Redis` 2.8.0 | package only; **0** usings | A46 lease / A99 keys | `MISSING` (impl) |
| Outbox produce / drain | **0** `OutboxEvents.Add` | §12–13 | `MISSING` |
| Hosts using this project | API, mt5-worker, fix-worker, Integration tests | same | wired |

Do **not** treat a green demo API or `EnsureCreated` as evidence that Postgres, Redis, or the outbox work.

---

## 1. Method

Read-only. Did **not** edit `src/`, `apps/`, `tests/`, `mt5-sdk/`. Did **not** start hosts. Did **not** run `dotnet ef`.

| Action | Result |
|---|---|
| `Get-ChildItem -Recurse` under `src\Infrastructure`, exclude `bin`/`obj` | **6** product files |
| SHA-256 + bytes + last-write of those 6 | §2 |
| Full read of all 5 `.cs` + csproj | types, methods, DI, fluent maps |
| `Test-Path` Configurations / Migrations / Redis / Outbox | Configurations exists, **0** children; others **false** |
| Grep `Class1` under Infrastructure | **0** |
| Grep `ConnectionMultiplexer` / `IEntityTypeConfiguration` / `Migrate` / `HasForeignKey` / `HasColumnName` in product `*.cs` | **0** (except `UseNpgsql` / `UseInMemoryDatabase` in DI) |
| Grep consumers of `AddTraderIntelligence` / `TraderDbContext` / `DemoSeeder` | 3 hosts + Integration tests |
| Cross-check Domain 20 entity types vs 20 `DbSet`s | 1:1 (B21 still holds) |

---

## 2. On-disk product tree

```text
D:\Prop\src\Infrastructure\
  TraderIntelligence.Infrastructure.csproj
  DependencyInjection.cs
  Dashboard\
    EfDashboardQueries.cs
  Persistence\
    TraderDbContext.cs
    EfTradingStore.cs
    Configurations\          ← EMPTY (0 files)
  Seeding\
    DemoSeeder.cs
  bin\                       ← Debug + Release net8.0 outputs (not product source)
  obj\                       ← restore + compile intermediates (not product source)
```

**Absent directories (required by A61 Appendix B / A41 / A46 / A99):**

- `Persistence/Migrations/`
- `Persistence/Sql/`
- `Persistence/Outbox/`
- `Persistence/Conventions/`
- `Persistence/Converters/`
- `Persistence/Interceptors/`
- `Redis/`
- `Leases/` / `Locks/`
- `Health/`

`Class1.cs` is **not** in the tree.

---

## 3. File identity (re-measured 2026-08-18)

Physical lines include blanks. SHA-256 uppercase. Last-write is local filesystem time.

| Bytes | SHA-256 | Phys. lines | Non-blank | Last write | Path |
|---:|---|---:|---:|---|---|
| 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | 25 | 21 | 2026-08-18 13:15:01 | `TraderIntelligence.Infrastructure.csproj` |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 44 | 39 | 2026-08-18 13:14:18 | `DependencyInjection.cs` |
| 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | 168 | 150 | 2026-08-18 13:14:18 | `Dashboard\EfDashboardQueries.cs` |
| 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | 250 | 231 | 2026-08-18 13:12:48 | `Persistence\EfTradingStore.cs` |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 174 | 151 | 2026-08-18 13:12:48 | `Persistence\TraderDbContext.cs` |
| 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | 138 | 127 | 2026-08-18 13:18:05 | `Seeding\DemoSeeder.cs` |

| Totals | |
|---|---|
| Product files | **6** |
| Product `.cs` | **5** |
| Product `.cs` physical lines | **774** |
| Product `.cs` non-blank lines | **698** |
| Product bytes (6 files) | **30 255** |

Compile-input hash (`obj\Debug\net8.0\TraderIntelligence.Infrastructure.csproj.CoreCompileInputs.cache`): `69cb4044cd86e6d1eec03ee1d01f8ddeed89248cd206356bee24291c6e69f188`.

Restore: `obj\project.nuget.cache` `"success": true`.

---

## 4. Public types (complete)

| Type | Kind | Namespace | File | Role |
|---|---|---|---|---|
| `DependencyInjection` | `static class` | `TraderIntelligence.Infrastructure` | `DependencyInjection.cs` | Composition root `AddTraderIntelligence` |
| `EfDashboardQueries` | `sealed class` | `TraderIntelligence.Infrastructure.Dashboard` | `Dashboard\EfDashboardQueries.cs` | `IDashboardQueries` |
| `EfTradingStore` | `sealed class` | `TraderIntelligence.Infrastructure.Persistence` | `Persistence\EfTradingStore.cs` | `ITradingStore` |
| `TraderDbContext` | `sealed class` : `DbContext` | `TraderIntelligence.Infrastructure.Persistence` | `Persistence\TraderDbContext.cs` | EF model |
| `DemoSeeder` | `static class` | `TraderIntelligence.Infrastructure.Seeding` | `Seeding\DemoSeeder.cs` | Idempotent demo seed |

**No** `internal` types. **No** interfaces declared in this project (ports live in Application). **No** `IEntityTypeConfiguration<T>`. **No** `IDesignTimeDbContextFactory<T>`. **No** Redis / outbox / lease types.

Private helper in this project: `EfDashboardQueries.MaskLogin(long)` only.

---

## 5. Project file and packages

`TargetFramework` = `net8.0`. `ImplicitUsings` + `Nullable` enabled.

### 5.1 Project references

| Reference | Why | Layering |
|---|---|---|
| `..\Domain\TraderIntelligence.Domain.csproj` | entities, enums, `TradeReconstructor`, `BaselineScorer` | correct |
| `..\Application\TraderIntelligence.Application.csproj` | `ITradingStore`, `IDashboardQueries`, DTOs, `DealIngestionService` | correct |
| `..\Mt5\TraderIntelligence.Mt5.csproj` | `DemoBrokerFactory` / `FakeMt5BrokerConnector` / `BrokerRegistry` | **composition leak** (C35). Persistence files do **not** import Mt5. Only DI + seeder do. |

DAG: `Domain ← Application ← Mt5`; `Infrastructure → Domain, Application, Mt5`. **No cycle.** Mt5 does not reference Infrastructure. Infrastructure does **not** reference `Fix.CTrader`.

Solution membership: `Mt5TraderIntelligence.sln` project `{14EDD461-7C2D-43AC-BC2B-F2DCAC644491}`.

### 5.2 Direct PackageReference

| Package | Version | Assets | Used in `.cs`? | Class |
|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 8.0.4 | `PrivateAssets=all` | no (design-time only) | `EXISTS_AND_GOOD` (hygiene) / `MISSING` (capability: no factory) |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.4 | runtime | yes (`UseInMemoryDatabase`) | `EXISTS_NEEDS_REFACTOR` — default SoT |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.4 | runtime | yes (`UseNpgsql`) | `EXISTS_AND_GOOD` (package) |
| `StackExchange.Redis` | 2.8.0 | runtime | **no** | `EXISTS_AND_GOOD` (client choice) / `MISSING` (impl) |

### 5.3 Relevant restore graph (`obj\project.nuget.cache` + `deps.json`)

| Package | Restored | Notes |
|---|---|---|
| `Microsoft.EntityFrameworkCore` | 8.0.4 | transitive via Npgsql EF / InMemory |
| `Microsoft.EntityFrameworkCore.Relational` | 8.0.4 | transitive |
| `Npgsql` | **8.0.3** | patch skew vs provider 8.0.4 |
| `FluentValidation` | 11.9.2 | **via Application**, not a direct Infra ref |
| `EFCore.NamingConventions` | **absent** | no snake_case |
| `Microsoft.EntityFrameworkCore.Tools` / `dotnet-ef` pin | **absent** | |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | **absent** | correct omission (A99 wants raw client) |
| Kafka / MassTransit / NATS / Rabbit | **absent** | correct omission (§13) |

`deps.json` runtime dependencies of `TraderIntelligence.Infrastructure/1.0.0`: Design 8.0.4, InMemory 8.0.4, Npgsql.EF 8.0.4, StackExchange.Redis 2.8.0, Application, Domain, **Mt5**.

---

## 6. DI census (`AddTraderIntelligence`)

Signature: `IServiceCollection.AddTraderIntelligence(this IServiceCollection, IConfiguration)`.

Connection resolution:

```text
configuration.GetConnectionString("TraderIntelligence")
  ?? configuration["DATABASE_URL"]

if empty/whitespace OR contains "<SECRET>"
    → UseInMemoryDatabase("trader-intelligence")
else
    → UseNpgsql(connection)
```

`apps/api/appsettings.json` has `"TraderIntelligence": ""` → **InMemory on default launch**.

| # | Service | Lifetime | Implementation | Notes |
|---|---|---|---|---|
| 1 | `TraderDbContext` | Scoped (`AddDbContext`) | InMemory or Npgsql | No `QueryTrackingBehavior.NoTracking`. No `MigrationsAssembly`. No retry. No snake_case. |
| 2 | `IMt5BrokerConnector` | Singleton (instance) | `DemoBrokerFactory.CreateDefault().Achiever` | Fake |
| 3 | `IMt5BrokerConnector` | Singleton (instance) | same factory `.Starwave` | Fake |
| 4 | `IBrokerRegistry` | Singleton (factory) | `new BrokerRegistry(GetServices<IMt5BrokerConnector>())` | |
| 5 | `ITradingStore` | Scoped | `EfTradingStore` | |
| 6 | `IDashboardQueries` | Scoped | `EfDashboardQueries` | |
| 7 | `TradeReconstructor` | Singleton | concrete | optional `VolumeConverter` / `SymbolNormalizer` **not registered** |
| 8 | `BaselineScorer` | Singleton | concrete | |
| 9 | `DealIngestionService` | Scoped | Application | |
| 10 | `ReconstructionScoringService` | Scoped | Application | |

**Not registered here:** `DemoSeeder` (static), Redis multiplexer, outbox writer/processor, `RiskEngine`, `ShadowCopyEngine`, any FIX type, `IDbContextFactory`, health contributors, Serilog/OTel.

`ValidateOnBuild` / `ValidateScopes` are **not** set by this extension.

C05 still holds: **no constructor cycle**. Seeder rebuilds a **second** `DemoBrokerFactory` + `DealIngestionService` outside the container (`EXISTS_NEEDS_REFACTOR`).

---

## 7. `TraderDbContext` census

Ctor: `TraderDbContext(DbContextOptions<TraderDbContext>)`. No `OnConfiguring`. All maps inline in `OnModelCreating` (lines 33–173). Pattern: `ToTable` + `HasKey(Id)` + optional `HasIndex`. Only `Broker.Code` / `DisplayName` have `HasMaxLength`.

### 7.1 DbSets (20) — complete

| # | `DbSet` | CLR type | Table | §45 | A20 # | Unique index configured | Store writes? | Dashboard reads? | Seeder writes? |
|---|---|---|---|---|---|---|---|---|---|
| 1 | `Brokers` | `Broker` | `brokers` | Y | 1 | `Code` | Resolve by Code | Y | Y |
| 2 | `Mt5Groups` | `Mt5Group` | `mt5_groups` | Y | 3 | `(BrokerId, Name)` | Upsert | Y | via ingest |
| 3 | `Mt5Accounts` | `Mt5Account` | `mt5_accounts` | Y | 5 | `(BrokerId, Login)` | Upsert | Y | via ingest |
| 4 | `Mt5Deals` | `Mt5Deal` | `mt5_deals` | Y | 8 | `(BrokerId, DealTicket)` + lookup `(BrokerId, Login, DealTime)` | Insert-if-absent | N | via ingest |
| 5 | `Mt5Positions` | `Mt5Position` | `mt5_positions_current` | Y | 9 | `(BrokerId, PositionTicket)` | Replace-all per login | N | via ingest |
| 6 | `ReconstructedTrades` | `ReconstructedTrade` | `reconstructed_trades` | Y | 13 | **non-unique** `(BrokerId, Login, PositionId, OpenedAt)` | Replace-all per login | Y (PnL group) | via scoring |
| 7 | `CanonicalInstruments` | `CanonicalInstrument` | `canonical_instruments` | Y | 14 | `Code` | N | N | Y (XAUUSD) |
| 8 | `SourceSymbolMappings` | `SourceSymbolMapping` | `source_symbol_mappings` | Y | 15 | `(BrokerId, SourceSymbol)` | **N** | **N** | **N** |
| 9 | `TraderScores` | `TraderScore` | `trader_scores` | Y | 17 | `(BrokerId, Login)` | Upsert | Y | via scoring |
| 10 | `TraderScoreHistory` | `TraderScoreHistory` | `trader_score_history` | Y | 18 | **non-unique** `(BrokerId, Login, RecordedAt)` | Append on score | N | via scoring |
| 11 | `OutboxEvents` | `OutboxEvent` | `outbox_events` | Y | 45 | **non-unique** `ProcessedAt` | **N** | **N** | **N** |
| 12 | `SyncCheckpoints` | `SyncCheckpoint` | `sync_checkpoints` | Y | 44 | `(BrokerId, Login, Stream)` | **N** | **N** | **N** |
| 13 | `CopyIntents` | `CopyIntent` | `copy_intents` | Y | 28 | `IdempotencyKey` | **N** | **N** | **N** |
| 14 | `RiskDecisions` | `RiskDecisionRecord` | `risk_decisions` | Y | 30 | **non-unique** `CopyIntentId` | **N** | Y (empty) | **N** |
| 15 | `ExecutionIntents` | `ExecutionIntent` | `execution_intents` | **N** (§44) | 37 | `ClOrdId` | **N** | **N** | **N** |
| 16 | `ShadowOrders` | `ShadowOrder` | `shadow_orders` | Y | 24 | PK only | **N** | Y (sum → 0) | **N** |
| 17 | `DestinationQuotes` | `DestinationQuoteSnapshot` | `destination_quotes` | Y | 34 | PK only | **N** | Y | Y (1 fake XAU) |
| 18 | `FixSessionStates` | `FixSessionState` | `fix_sessions` | Y | 35 | `Qualifier` **global** unique | **N** | Y | Y (QUOTE+TRADE) |
| 19 | `AuditLogs` | `AuditLog` | `audit_logs` | Y | 46 | PK only | **N** | **N** | **N** |
| 20 | `KillSwitches` | `KillSwitch` | `kill_switches` | **N** | not in A20 | PK only | **N** | Y | Y (Mode=None) |

Filename vs type: `Domain\Entities\DestinationQuote.cs` declares `DestinationQuoteSnapshot`. B21: **0** missing types.

Compound unique indexes: **7**. Compound non-unique: **3**. Single-column unique: **5**. `HasAlternateKey` / `HasForeignKey` / `HasDatabaseName` / `HasColumnName` / `UseSnakeCaseNamingConvention`: **0**. Composite `HasKey`: **0** (all surrogate `Guid Id`).

### 7.2 §45 names **not** mapped (25)

`broker_connections`, `plan_group_mappings`, `mt5_account_snapshots`, `mt5_orders`, `mt5_symbols`, `mt5_xau_ticks`, `trader_feature_snapshots`, `trader_states`, `trader_risk_flags`, `model_versions`, `model_predictions`, `model_evaluations`, `shadow_fills`, `shadow_positions`, `shadow_performance`, `copy_allocations`, `risk_events`, `execution_venues`, `destination_symbols`, `fix_session_events`, `fix_orders`, `fix_execution_reports`, `destination_positions`, `source_destination_links`, `system_events`.

`model_*` stay missing **on purpose** until Phase 6 (A52). Still a §45 gap.

A20 extras also missing: `ingestion_events`, `execution_reconciliation_runs`, `execution_reconciliation_issues`.

---

## 8. `EfTradingStore` — `ITradingStore` 8/8

Port: `Application\Ingestion\DealIngestionService.cs` (`ITradingStore`). Implementation is complete against the **current** port. The port itself is a demo ingest/score surface, not A41/A59/A61.

| Method | Persist | `SaveChanges` | Semantics | Gaps |
|---|---|---|---|---|
| `ResolveBrokerIdAsync` | Brokers | no | `SingleAsync` by `Code` | throws if unknown |
| `UpsertGroupAsync` | `mt5_groups` | yes | insert all fields; **update only `Currency` + `LastSyncedAt`** | drops Company / margins / digits / ConnectionsAllowed on update |
| `UpsertAccountAsync` | `mt5_accounts` | yes | insert most fields; **update GroupName, Balance, Equity, LastSyncedAt only** | skips Leverage / Margin / MarginFree / Profit on update |
| `UpsertDealAsync` | `mt5_deals` | yes | insert-if-absent on `(BrokerId, DealTicket)`; returns `bool` inserted | no `ON CONFLICT`; no outbox; no checkpoint |
| `ReplacePositionsAsync` | `mt5_positions_current` | yes | delete login book + insert | `TimeUpdate = UtcNow`; no `Swap` from DTO (DTO has none) |
| `LoadDealsAsync` | read | no | order `DealTime`, `DealTicket` → `NormalizedDeal` | `NormalizedDeal.BrokerId` is **string broker code**, not Guid |
| `ReplaceReconstructedAsync` | `reconstructed_trades` | yes | delete login rows + insert **new Guids** | `Id` is **not** a stable `source_trade_id` |
| `UpsertScoreAsync` | scores + history | yes | upsert current; **always append** history | no `score_kind`; state/flags live on score row |

Every mutating method is its **own transaction** (`SaveChanges` per call). `DealIngestionService.SyncBrokerAsync` therefore commits once per group, per account, per deal, per position-replace. **Not** §12 same-TX (raw + outbox).

Unused by the store (mapped, never written here): outbox, checkpoints, copy intents, risk decisions, execution intents, shadow orders, quotes, FIX sessions, audit, kill switch, canonical, symbol mappings.

---

## 9. `EfDashboardQueries` — `IDashboardQueries` 7/7

| Method | What it actually queries | Hardcoded / theater |
|---|---|---|
| `GetOverviewAsync` | account count; enabled broker count; **all** `TraderScores` in memory; shadow slippage sum; 2 FIX rows | `DestinationRealPnl=0`, `XauGross=0`, `XauNet=0`, `RealCopyEnabled=false`. `Mt5Healthy = brokers > 0`. Quote/Trade healthy = seeded enum ∈ {LoggedOn, Ready*, Reconciling} |
| `GetBrokersAsync` | brokers + N+1 group/account counts | `Connected=true` always; `LastEventAt=UtcNow`; login masked `/100*100` |
| `GetGroupsAsync` | all groups + broker dict + N+1 account counts | no pagination |
| `GetTradersAsync` | **full** scores, brokers, accounts + reconstructed PnL group | `MlProbability=null`; trader `ShadowPnl=0`; filter/sort in memory |
| `GetTraderAsync` | **reloads entire leaderboard** then `FirstOrDefault` | same |
| `GetFixSessionsAsync` | all sessions + latest quote | `ExecutionEnabled=false`; quote age from wall clock |
| `GetRiskAsync` | latest kill switch + 20 reject reasons | `DailyPnl/Drawdown/XauLong/XauShort/XauNet=0`; `RealCopyEnabled=false` |

`AsNoTracking`: **4** sites, all inside `GetTradersAsync`. Tests of this class: **0**.

C36 still holds: demo-cheap, **UNSAFE** as a 5k-account Postgres path.

---

## 10. `DemoSeeder` census

`SeedAsync(TraderDbContext, ITradingStore, ReconstructionScoringService, CancellationToken)`.

Guard: `if (await db.Brokers.AnyAsync(ct)) return;` — **idempotent** at broker-row granularity only.

Fixed Guids:

| Id | Meaning |
|---|---|
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1` | Achiever |
| `aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2` | StarwaveFX |
| `bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1` | XAUUSD instrument |
| `cccccccc-cccc-cccc-cccc-ccccccccccc1` | QUOTE session |
| `cccccccc-cccc-cccc-cccc-ccccccccccc2` | TRADE session |
| `dddddddd-dddd-dddd-dddd-ddddddddddd1` | kill switch |

Then: `ingestion.SyncBrokerAsync` for both codes over 2026-01-01 … 2026-12-31; `RebuildTraderAsync` for logins `10001, 10002, 10003, 99001`.

Seeded FIX theater (no socket): QUOTE `ReadyForMarketData` host `live-us-eqx-01.p.c-trader.com:5211`; TRADE `LoggedOn` `:5212`; `SenderCompId=live.pepperstone.1369850`; `TargetCompId=cServer`. Fake quote Bid/Ask `2399.45` / `2399.85`.

Broker rows carry **live targeting** (not passwords): Achiever `57.128.141.65:443` manager `2027`; Starwave `84.201.6.142:443` manager `9904`. No password fields on `Broker`.

Seeder constructs its **own** `DemoBrokerFactory` + `BrokerRegistry` + `DealIngestionService` (ignores container instances). Dual fake catalogs (C05).

---

## 11. Consumers (outside this project)

| Consumer | Path | How it uses Infrastructure |
|---|---|---|
| API | `D:\Prop\apps\api` | `AddTraderIntelligence`; `EnsureCreated` + `DemoSeeder`; `IDashboardQueries` on 7 routes; `TraderDbContext` on `/ready` + `/api/trades`; `ITradingStore` only for seed |
| MT5 worker | `D:\Prop\apps\mt5-worker` | same boot; `DealIngestionService` + `ReconstructionScoringService` every 30 s (Fake connectors) |
| FIX worker | `D:\Prop\apps\fix-worker` | same boot; **does not** use Fix.CTrader types; 15 s heartbeat stamps `fix_sessions` LoggedOn / ReadyForMarketData |
| Integration tests | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | 2 facts, **InMemory** `Guid` names: seed+score; deal upsert idempotent |
| Unit tests | `D:\Prop\tests\Unit` | **no** project ref to Infrastructure |
| Swarm scratch | `reports\swarm\20260818\_tmp_c23_empty` | eval only; not product |

Host schema path: **`Database.EnsureCreatedAsync()` × 3**. Zero `Migrate()`.

API `/api/health` hard-codes `database.healthy=true`, `redis.healthy=false`, `outboxBacklog=0` — **not** Infrastructure types.

---

## 12. Tests that touch this project

| Test | Provider | Asserts |
|---|---|---|
| `Demo_seed_discovers_groups_reconstructs_and_scores` | EF InMemory unique name | 2 brokers; >2 groups; deals >0; completed XAU exists; login 10001 has 3 XAU and is not LIVE; 10002 is `RISK_BLOCKED`; 2 FIX rows; `TargetCompId == cServer` |
| `Deal_upsert_is_idempotent` | EF InMemory | first insert true, second false, count=1 |

**No** Testcontainers. **No** Postgres migration test. **No** `EfDashboardQueries` test. **No** DI `ValidateOnBuild` test. **No** Redis test.

---

## 13. What this project does **not** contain

| Capability | Evidence | Class |
|---|---|---|
| Versioned EF migrations | no `Migrations/` dir; no `Migrate()` | `MISSING` |
| Design-time factory | 0 `IDesignTimeDbContextFactory` | `MISSING` |
| Split fluent configs | `Configurations/` empty | `MISSING` |
| Snake_case / named UKs / FKs | 0 `HasColumnName` / `HasDatabaseName` / `HasForeignKey` | `MISSING` |
| Redis multiplexer / A46 lease / A99 façade | 0 `using StackExchange.Redis` | `MISSING` |
| Outbox writer + SKIP LOCKED dispatcher | 0 `OutboxEvents.Add`; C58 | `MISSING` |
| Sync checkpoint I/O | `DbSet` only | `MISSING` |
| Copy / risk / execution / shadow / audit writers | `DbSet` only | `MISSING` |
| Tick / order / symbol / snapshot tables | no types | `MISSING` |
| Health / ready contributors | hosts hard-code JSON | `MISSING` |
| Serilog / OTel | not this project | `MISSING` (hosts) |
| RBAC | none | `MISSING` |

---

## 14. Classification roll-up

| Slice | Class |
|---|---|
| Project as a compiling demo data plane | `EXISTS_NEEDS_REFACTOR` |
| `Class1` leftover | gone — `EXISTS_AND_GOOD` |
| Package set (EF 8.0.4 + Npgsql EF 8.0.4 + Redis 2.8.0) | `EXISTS_AND_GOOD` (choice) |
| Default InMemory SoT | `UNSAFE` for anything beyond local demo |
| `EnsureCreated` on 3 hosts | `UNSAFE` vs §72.3 / A61 |
| `TraderDbContext` vs §45 | 18/43 names — `EXISTS_NEEDS_REFACTOR` / remainder `MISSING` |
| `ITradingStore` / `IDashboardQueries` adapters | `EXISTS_AND_GOOD` vs current ports; `EXISTS_NEEDS_REFACTOR` vs architecture |
| Demo seeder | `EXISTS_NEEDS_REFACTOR` (works; dual factory; FIX theater; live host/login numbers) |
| Redis implementation | `MISSING` |
| Outbox bus | `MISSING` |
| Migrations | `MISSING` |
| Infra → Mt5 project reference | `EXISTS_NEEDS_REFACTOR` (C35: OK for demo, invert before native Manager) |

**Score (name presence only):** §45 **18/43**. A20 union **19/47** if counting `execution_intents`, **20 mapped tables** including `kill_switches`. Proper A61 maps / migrations: **0/43**.

---

## 15. Binding sibling reports

| Report | Question this census does not re-litigate |
|---|---|
| `B03_infra_gap.md` | §45 gap score |
| `B19_dbcontext_gap.md` | per-table mapping quality + 25 missing |
| `B21_dbcontext_type_mismatch.md` | DbSet ↔ Domain type existence (PASS) |
| `C05_di_review.md` | no DI cycle; split seeder graph |
| `C06_dbcontext_review.md` | compound keys = unique indexes, not composite PKs |
| `C27_redis_gap.md` | package without lease |
| `C29_migrations_gap.md` | zero `Migrations/` |
| `C35_layering.md` | Infra → Mt5 acceptable for demo only |
| `C36_query_perf.md` | dashboard N+1 / full-table loads |
| `C58_outbox_dispatcher.md` | parked `outbox_events` |

---

## 16. One-screen inventory

```text
src/Infrastructure  (net8.0 classlib)
  files:     6 product (5 cs + csproj)   774 cs lines
  types:     DependencyInjection, TraderDbContext, EfTradingStore,
             EfDashboardQueries, DemoSeeder
  ports:     ITradingStore 8/8   IDashboardQueries 7/7
  tables:    20 DbSets   18/43 §45 names   0 migrations   0 IEntityTypeConfiguration
  packages:  EF Design 8.0.4, InMemory 8.0.4, Npgsql.EF 8.0.4, Redis 2.8.0 unused
  default:   UseInMemoryDatabase("trader-intelligence")
  consumers: api, mt5-worker, fix-worker, Integration (2 InMemory facts)
  not here:  Redis, outbox bus, checkpoints I/O, FIX, live MT5, ticks, RBAC
```
