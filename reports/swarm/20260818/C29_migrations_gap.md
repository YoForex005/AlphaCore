# C29 — EF migrations gap: no `Migrations/` folder; InMemory + `EnsureCreated` only

| Field | Value |
|---|---|
| Agent | C29 (migrations gap, read-only) |
| Date | 2026-08-18 |
| Assigned question | No Migrations folder? InMemory `EnsureCreated` only? |
| Artifact | `D:\Prop\reports\swarm\20260818\C29_migrations_gap.md` |
| Product source modified | **No.** Report only. |
| Workspace | `D:\Prop` |
| Law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§60** (integration: PostgreSQL migrations), **§72.3** (“Use migrations.”) |
| Mapping contract | `D:\Prop\reports\swarm\20260818\A61_efcore_schema.md` §3.3 |
| Migration catalog | `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md` §3 (`0001`–`0015`) |
| Table catalog | `D:\Prop\reports\swarm\20260818\A20_table_catalog.md` (union **47**; §45 **43**) |
| Related (do not treat as this file) | B03 (infra gap), B19 / C06 (DbContext), B07 / C07 (workers), B08 / A90 (tests), A65 / B37 / C12 (compose) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**Yes. There is no EF `Migrations/` folder anywhere in the product tree. Schema authority is `Database.EnsureCreatedAsync()` on three hosts. The default provider is EF InMemory.**

This is not a partial migration set. It is **zero** versioned migrations, **zero** `__EFMigrationsHistory` writers, **zero** `Database.Migrate()` call sites, **zero** `IDesignTimeDbContextFactory`, and **zero** Testcontainers / Postgres migration tests.

| Item | Measured | Target (A61 §3.3 / A30 / §72.3) | Class |
|---|---|---|---|
| `src/Infrastructure/Persistence/Migrations/` | **does not exist** | versioned EF migrations | **MISSING** |
| Any `Migrations/` dir under `D:\Prop` excluding `bin/` `obj/` `node_modules/` `reports/` | **0** | same | **MISSING** |
| Companion `Persistence/Sql/*.sql` | **does not exist** | A30 companion reviewed SQL | **MISSING** |
| `IDesignTimeDbContextFactory<T>` | **0** C# matches | factory for `dotnet ef` | **MISSING** |
| `MigrationsAssembly(...)` | **0** | Infrastructure assembly | **MISSING** |
| `Database.Migrate` / `MigrateAsync` | **0** in `*.cs` | hosts or a migrator job | **MISSING** |
| `EnsureCreatedAsync` | **3 hosts** (api, mt5-worker, fix-worker) | **Forbidden** | **UNSAFE** |
| Default DI when CS empty / `<SECRET>` | `UseInMemoryDatabase("trader-intelligence")` | PostgreSQL 15+ / 16 | **UNSAFE** |
| `UseNpgsql` without migrations | present, unused in default launch | + retry + snake_case + `Migrate` | **EXISTS_NEEDS_REFACTOR** |
| `Microsoft.EntityFrameworkCore.Design` 8.0.4 | referenced on Infrastructure | keep | **EXISTS_AND_GOOD** (package) / **MISSING** (capability) |
| `Microsoft.EntityFrameworkCore.Tools` / `dotnet-ef` pin | **0** | startup project + tools | **MISSING** |
| Integration `PostgresMigrationTests` / Testcontainers / Respawn | **0** | §60 item 1 | **MISSING** |
| Integration tests that touch a store | InMemory only (`Guid.NewGuid()` name) | Postgres `Migrate()` | **EXISTS_NEEDS_REFACTOR** |

**Score:** migrations **0 / 15** (A30 `0001`–`0015`). Schema history rows **0**. §60 “PostgreSQL migrations” integration area **FAIL**. Do not treat a green `dotnet test` or a running demo API as evidence that Postgres can be created, upgraded, or shared.

---

## 1. Method

Read-only. Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were not edited.

| Action | Result |
|---|---|
| `Get-ChildItem -Recurse -Directory -Filter Migrations` under `D:\Prop`, exclude `bin`/`obj`/`node_modules`/`reports` | **empty** |
| `Test-Path` on `Persistence\Migrations`, `Persistence\Sql`, `Infrastructure\Migrations`, `apps\api\Migrations`, `tests\Integration\Migrations` | all **False** |
| `Get-ChildItem` `Persistence\Configurations` | **0 children** |
| Grep `*.cs` for `EnsureCreated`, `UseInMemoryDatabase`, `UseNpgsql`, `Migrate`, `IDesignTimeDbContextFactory`, `MigrationsAssembly` | hosts + DI + 2 test lines only; **no** `Migrate*` |
| Grep `*.cs` / `*.csproj` for `Testcontainers`, `Respawn`, `WebApplicationFactory`, `Database.Migrate` | **0** |
| Grep `*.csproj` / `*.json` for `Microsoft.EntityFrameworkCore.Tools`, `dotnet-ef` | **0** |
| Grep `mt5-sdk\src` for `CREATE TABLE` | **0** (C++ assumes tables exist) |
| SHA-256 of the files below | computed 2026-08-18 |

Did **not** run `dotnet ef`, did **not** start hosts, did **not** hit Compose Postgres. There is nothing for `dotnet ef migrations list` to list.

---

## 2. Measured files (2026-08-18)

Product files only (hashes SHA-256, sizes bytes):

| Bytes | SHA-256 | Path |
|---:|---|---|
| 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `D:\Prop\apps\api\Program.cs` |
| 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `D:\Prop\apps\mt5-worker\Program.cs` |
| 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `D:\Prop\apps\fix-worker\Program.cs` |
| 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` | `D:\Prop\apps\api\appsettings.json` |
| 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | `D:\Prop\apps\mt5-worker\appsettings.json` |
| 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | `D:\Prop\apps\fix-worker\appsettings.json` |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |
| 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `D:\Prop\docker-compose.yml` |
| 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |

```text
D:\Prop\src\Infrastructure\Persistence\
  Configurations\          ← EMPTY (0 files; no IEntityTypeConfiguration)
  EfTradingStore.cs
  TraderDbContext.cs       ← 20 DbSets, inline fluent, no factory
  Migrations\              ← MISSING (directory does not exist)
  Sql\                     ← MISSING (A30 companion SQL never created)

D:\Prop\apps\api\Migrations\                 ← MISSING
D:\Prop\apps\mt5-worker\                     ← no schema files
D:\Prop\apps\fix-worker\                     ← no schema files
D:\Prop\tests\Integration\Migrations\        ← MISSING
```

**Absent (required by A61 Appendix B / A30 §3):**

- `Persistence/TraderIntelligenceDbContext.cs` (target name; stub is `TraderDbContext`)
- `Persistence/TraderIntelligenceDbContextFactory.cs`
- `Persistence/Migrations/202608180001_*.cs` … `202608180015_*.cs`
- `Persistence/Sql/<same_name>.sql`
- `Persistence/Conventions/PgModelConventions.cs`
- all 43 `Persistence/Configurations/*Configuration.cs`

---

## 3. Host schema path = `EnsureCreatedAsync` only

Every process that builds a `TraderDbContext` at startup uses the same two-step ritual: create-from-model, then `DemoSeeder`.

| Host | File | Lines | After `Build()` |
|---|---|---|---|
| API | `D:\Prop\apps\api\Program.cs` | 84–93 | `EnsureCreatedAsync()` then `DemoSeeder.SeedAsync` |
| mt5-worker | `D:\Prop\apps\mt5-worker\Program.cs` | 11–20 | same |
| fix-worker | `D:\Prop\apps\fix-worker\Program.cs` | 11–20 | same |

API:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(/* ... */);
}
```

Workers (mt5 and fix are the same 10-line block except usings):

```csharp
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(/* ... */);
}
```

`EnsureCreated` / `EnsureCreatedAsync` creates the **current** model as a blank schema. It does **not**:

- write `__EFMigrationsHistory`
- apply a versioned Up script
- survive a later model change (no incremental DDL)
- record which of A30 `0001`–`0015` is present
- refuse to run if the live schema is a different shape

A61 §3.3 and A61 §11 list `EnsureCreated` in hosts as **Forbidden** (“no migration history”). Architecture §72.3 is one line: **Use migrations.**

Three hosts each call it. That is three schema authorities, not one migrator job.

---

## 4. Default store is InMemory — Compose Postgres is unused

`AddTraderIntelligence` (`DependencyInjection.cs` 19–29):

```csharp
var connection = configuration.GetConnectionString("TraderIntelligence")
                 ?? configuration["DATABASE_URL"];

if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
{
    services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
}
else
{
    services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
}
```

Measured connection-string surfaces:

| Surface | `ConnectionStrings:TraderIntelligence` | `DATABASE_URL` |
|---|---|---|
| `apps/api/appsettings.json` | `""` (empty) | absent |
| `apps/api/appsettings.Development.json` | absent | absent |
| `apps/api/Properties/launchSettings.json` | no CS env | no `DATABASE_URL` |
| `apps/mt5-worker/appsettings.json` | **no `ConnectionStrings` key at all** | absent |
| `apps/fix-worker/appsettings.json` | **no `ConnectionStrings` key at all** | absent |
| `docker-compose.yml` `api` service | **not set** (only `ASPNETCORE_ENVIRONMENT=Development`) | not set |

Default launch of API, mt5-worker, and fix-worker therefore all take the InMemory branch.

InMemory facts that matter here:

1. The database name `"trader-intelligence"` is **per process**, not a shared server. Three hosts = three isolated demo worlds. The dashboard cannot see worker writes unless an operator injects a real CS.
2. InMemory does not implement PostgreSQL unique indexes, partial indexes, `jsonb`, `timestamptz`, `xid`/`xmin`, `ON CONFLICT`, or `SKIP LOCKED`. `SeedingAndStoreTests.Deal_upsert_is_idempotent` is an application-level upsert, not a PG unique proof.
3. `EnsureCreated` on InMemory is a model materialize, not a schema migration. It cannot fail the way a missing PG extension or a drift would.
4. The `else` `UseNpgsql(connection)` path has **no** `MigrationsAssembly`, **no** `EnableRetryOnFailure`, **no** `UseSnakeCaseNamingConvention`, **no** command timeout, and **no** subsequent `Migrate()`. Pointing a host at Compose Postgres today would still `EnsureCreated` the stub model (PascalCase columns; see §7).

`docker-compose.yml` starts `postgres:16` (`ti` / `ti_dev_only` / `trader_intelligence`) and `redis:7`. The `api` service does **not** receive `ConnectionStrings__TraderIntelligence`. Compose Postgres stays empty. There is no `migrate` one-shot service.

---

## 5. Design-time: package without a factory

`TraderIntelligence.Infrastructure.csproj` already references:

- `Microsoft.EntityFrameworkCore.Design` **8.0.4** (`PrivateAssets=all`)
- `Microsoft.EntityFrameworkCore.InMemory` **8.0.4**
- `Npgsql.EntityFrameworkCore.PostgreSQL` **8.0.4**

Missing for `dotnet ef`:

| Need | Present? |
|---|---|
| `IDesignTimeDbContextFactory<TraderDbContext>` (or target `TraderIntelligenceDbContext`) | **No** |
| `MigrationsAssembly` on `UseNpgsql` | **No** |
| EF Design on startup project `apps/api` (A30: add it there) | **No** — API csproj has Swashbuckle / Serilog / SignalR only |
| `Microsoft.EntityFrameworkCore.Tools` / pinned `dotnet-ef` | **No** |
| Env `TI_POSTGRES` reader | **No** |

A61 §3.2 factory is specified and **not implemented**. `dotnet ef migrations add InitialSection45 --project src/Infrastructure --startup-project apps/api` has no design-time Npgsql context to build. Even if someone added a factory tomorrow, `--output-dir Persistence/Migrations` would be the first file in a folder that does not exist yet.

---

## 6. Tests do not apply migrations

`D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` references `Microsoft.EntityFrameworkCore.InMemory` 8.0.4. It does **not** reference Testcontainers, Npgsql, or Respawn.

`SeedingAndStoreTests` (the only non-placeholder integration class) builds a private InMemory store twice:

```csharp
var options = new DbContextOptionsBuilder<TraderDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
```

It never calls `EnsureCreated` (InMemory materializes on first use) and never calls `Migrate`. `UnitTest1.cs` is `PlaceholderRemoved` / `Integration_project_loads` → `Assert.True(true)`.

§60 required integration area **“PostgreSQL migrations”** is therefore **0 classes**. A90 named `Empty_database_applies_all_migrations`, `Apply_is_idempotent_on_second_run`, `Migrate_does_not_call_ensure_created` — none of those files exist.

A passing `dotnet test` on this project is **not** evidence of:

- a blank `postgres:16` applying Up scripts
- `__EFMigrationsHistory` row count = migration file count
- idempotent second `Migrate()`
- data surviving re-apply
- InMemory being rejected when `TI_TEST_PG` is set

---

## 7. What `EnsureCreated` would actually emit if Npgsql were used

`TraderDbContext` has **20** `DbSet`s and **20** `ToTable(...)` names. Fluent mapping is inline. `Configurations/` is empty. No `HasColumnName`, no `HasDatabaseName`, no `UseSnakeCaseNamingConvention()`, no `HasDefaultSchema("public")`.

| `ToTable` today | In A30 I1 (`0001`–`0007`)? | In §45 / A61 43? | Notes |
|---|---|---|---|
| `brokers` | yes (`0002`) | yes | columns would be `"Code"` not `code` |
| `mt5_groups` | yes (`0003`) | yes | |
| `mt5_accounts` | yes (`0004`) | yes | |
| `mt5_deals` | yes (`0006`) | yes | |
| `mt5_positions_current` | yes (`0006`) | yes | UK uses `PositionTicket`, not A61 `PositionId` |
| `sync_checkpoints` | yes (`0007`) | yes | UK includes `Login` (A61 wants stream-scoped) |
| `outbox_events` | yes (`0007`) | yes | no `status`/`available_at` index from A30 |
| `audit_logs` | yes (`0007`) | yes | |
| `canonical_instruments` | I4 (`0008`) | yes | |
| `source_symbol_mappings` | I4 (`0008`) | yes | |
| `reconstructed_trades` | I4 (`0009`) | yes | 4-col index is **not unique** (C06) |
| `trader_scores` | I5 (`0010`) | yes | |
| `trader_score_history` | I5 (`0010`) | yes | |
| `destination_quotes` | I7 (`0011`) | yes | PK only |
| `fix_sessions` | I7 (`0012`) | yes | `Qualifier` globally unique (wrong vs venue) |
| `copy_intents` | I8 (`0013`) | yes | |
| `shadow_orders` | I8 (`0014`) | yes | PK only |
| `risk_decisions` | I8 (`0015`) | yes | |
| `execution_intents` | **not in v1 A30 list** | A20 extra | premature vs A30 “do not create yet” |
| `kill_switches` | **not in A30 catalog** | not in §45 43 | extra demo table |

A30 I1 tables **not** in this context (so `EnsureCreated` would **never** create them):

```text
schema_meta
broker_connections
plan_group_mappings
mt5_account_snapshots
mt5_symbols
mt5_xau_ticks
mt5_orders
ingestion_events
system_events
```

Later A30 tables also absent: `trader_feature_snapshots`, `trader_states`, `trader_risk_flags`, `execution_venues`, `destination_symbols`, `fix_session_events`, `copy_allocations`, `shadow_fills`, `shadow_positions`, `shadow_performance`, `risk_events`.

C++ `mt5-sdk` writes `mt5_raw_events` and `mt5_deals_ledger` (`mt5_ledger_store.cpp`) and contains **no** `CREATE TABLE` in `mt5-sdk/src`. That is a **second**, also-unmigrated schema assumption. It is not created by `TraderDbContext.EnsureCreated` either.

If an operator set a real CS **today**:

1. Hosts call `EnsureCreatedAsync` → PascalCase quoted columns, 20 stub tables, **no** `__EFMigrationsHistory`.
2. A later `dotnet ef migrations add` + `Database.Migrate()` would see an empty history and try to `CREATE` tables that already exist → fail.
3. Model edits (add `mt5_orders`, rename a column, add a UK name) would **not** upgrade the live database. `EnsureCreated` is a no-op when *any* tables already exist.
4. API + two workers would race `EnsureCreated` + `DemoSeeder`. Seeder bails if `Brokers.Any()`, so the first process wins the demo world; the others skip. That is not a migration.

This is why A61 forbids `EnsureCreated` as schema authority: it poisons the first real Postgres with an unversioned stub.

---

## 8. Contract vs repo (binding quotes)

Architecture §60 integration tests, required:

```text
PostgreSQL migrations
```

Architecture §72.3:

```text
3. Use migrations.
```

A61 §3.3:

| Rule | Detail |
|---|---|
| Folder | `Persistence/Migrations/` |
| First migration | `20260818_InitialSection45` (or tool timestamp) |
| `EnsureCreated` | **Forbidden** in hosts |
| Tests | `PostgresMigrationTests` against Testcontainers Postgres 16 |

A61 §11 forbidden: `EnsureCreated` in hosts — “no migration history”.

A30 §3: fifteen versioned files under `src/Infrastructure/Persistence/Migrations/`, never squash, companion SQL under `Persistence/Sql/`. **0 of 15 exist.**

A90 / B08: §60 item 1 is **MISSING**; InMemory + `EnsureCreated` cannot close it.

---

## 9. Scorecard

```text
Persistence/Migrations/ directory                    0
Persistence/Sql/ directory                           0
EF migration .cs files                               0
Companion .sql files                                 0
__EFMigrationsHistory writers                        0
Database.Migrate / MigrateAsync call sites           0
IDesignTimeDbContextFactory                          0
MigrationsAssembly on UseNpgsql                      0
EnsureCreatedAsync in hosts                          3   (api, mt5-worker, fix-worker)
Default provider (empty / <SECRET> CS)               InMemory "trader-intelligence"
UseNpgsql branch (operator-injected CS only)         yes, no migrate
Compose api → Postgres connection                    not wired
Integration tests using InMemory                     2 facts
Integration tests using Postgres / Migrate           0
Testcontainers / Respawn                             0
A30 migrations present                               0 / 15
§45 tables creatable via versioned Up                0 / 43
§60 "PostgreSQL migrations"                          FAIL
§72.3 Use migrations                                 FAIL
```

---

## 10. What must land later (not done in this report)

Product source is **not** changed here. When I1 is implemented, the binding order is A61 / A30, not “call `EnsureCreated` against Compose”:

1. Replace / supersede `TraderDbContext` with `TraderIntelligenceDbContext` + 43 `IEntityTypeConfiguration` + snake_case + named `*_uk` (A61). Domain types first for missing tables.
2. Add `IDesignTimeDbContextFactory` reading `TI_POSTGRES` / `ConnectionStrings__TraderIntelligence` (placeholders only).
3. `dotnet ef migrations add` → **new** files under `src/Infrastructure/Persistence/Migrations/` (never hand-write Up SQL as the only artifact; never `EnsureCreated` once the first migration exists).
4. Hosts: delete `EnsureCreatedAsync`. Apply via `Database.MigrateAsync` **or** a dedicated Linux/CI migrator job (A54 / A65). One authority, not three.
5. Remove or strictly gate the InMemory fallback (A90: lab hazard). Default launch must fail closed if Postgres is required, or use an explicit `TI_ALLOW_INMEMORY=1` that tests assert is off in the Postgres fixture.
6. Wire Compose `api` (and a one-shot `migrate` service) to `ConnectionStrings__TraderIntelligence` after migrations exist. Do not `CREATE TABLE` in an init `.sql` that races EF.
7. `PostgresMigrationTests` on Testcontainers `postgres:16`: blank apply, idempotent re-apply, no `EnsureCreated`, history row count, data survives re-apply (A90 §5).
8. Do not treat C++ `mt5_raw_events` / `mt5_deals_ledger` as created by this context. If the collector stays, it needs its own versioned DDL — still not `EnsureCreated` from C#.

---

## 11. What this report does not claim

- Did not run `dotnet ef` or `dotnet test`.
- Did not start `docker compose` or prove the `ti` role can log in.
- Did not claim the 20 `ToTable` names are a production schema (C06 / B19 / B03 already scored the model).
- Did not claim Design 8.0.4 is unused in restore — only that design-time **capability** is missing.
- Did not modify product source.

**Bottom line:** No Migrations folder. Schema path is InMemory (default) plus `EnsureCreated` on all three hosts. That is a demo stub, not architecture §60 / §72.3 / A61.
