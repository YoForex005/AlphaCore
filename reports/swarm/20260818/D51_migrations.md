# D51 — Migrations folder? Measured: **no**

| Field | Value |
|---|---|
| Agent | D51 (senior engineer, migrations-folder census, read-only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 ~13:40 local (second pass after a transient `Configurations` file appeared and vanished mid-census) |
| Assigned | Migrations folder? Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D51_migrations.md` |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This report is the only write. |
| Law | Architecture v2 **§60** (“PostgreSQL migrations”), **§72.3** (“Use migrations.”) |
| Folder contract | `A61_efcore_schema.md` §3.3 (`Persistence/Migrations/`) |
| Catalog | `A30_implementation_sequence.md` §3 (`0001`–`0015`) |
| Prior measure | `C29_migrations_gap.md` — **still holds** on the folder question. This file re-measures and records drift. |
| Related | D19 (DbContext vs §45), D23 (DI), D03 (infra tree), A90 (test classes), A65 (compose), C47.1 (next increment owns the first real migration) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**There is no EF `Migrations/` folder.** Not under `src/Infrastructure/Persistence/`, not under `apps/`, not under `tests/`, not in `git ls-files`. Schema authority on every host is still `Database.EnsureCreatedAsync()`. Default provider is still EF InMemory.

This is not a partial set. It is **zero** versioned Up scripts, **zero** `__EFMigrationsHistory` writers, **zero** `Database.Migrate()` / `MigrateAsync()` call sites, **zero** `IDesignTimeDbContextFactory`, **zero** companion `Persistence/Sql/*.sql`, and **zero** Testcontainers Postgres migration tests.

| Item | Measured now | Target | Class |
|---|---|---|---|
| `src/Infrastructure/Persistence/Migrations/` | **directory does not exist** | A61 §3.3 / A30 §3 | **MISSING** |
| Any product-tree `Migrations/` (exclude `bin/` `obj/` `node_modules/` `reports/`) | **0** | same | **MISSING** |
| `git ls-files` matching `Migrat` / `Persistence/Sql` / `*ModelSnapshot*` | **0** | checked-in history | **MISSING** |
| A30 files `202608180001` … `202608180015` | **0 / 15** (`Test-Path` all false) | never squash | **MISSING** |
| `Persistence/Sql/` | **does not exist** | companion reviewed SQL | **MISSING** |
| `IDesignTimeDbContextFactory<T>` | **0** in product `*.cs` | `dotnet ef` | **MISSING** |
| `MigrationsAssembly(...)` | **0** | Infrastructure assembly | **MISSING** |
| `Database.Migrate` / `MigrateAsync` | **0** | one schema authority | **MISSING** |
| `EnsureCreatedAsync` | **3 hosts** (api, mt5-worker, fix-worker) | **Forbidden** (A61 §3.3) | **UNSAFE** |
| Default DI (empty / `<SECRET>` CS) | `UseInMemoryDatabase("trader-intelligence")` | PostgreSQL 16 | **UNSAFE** |
| `UseNpgsql` | present if operator injects a real `ConnectionStrings:TraderIntelligence` or `DATABASE_URL` **without** `<SECRET>` | + retry + snake_case + `Migrate` | **EXISTS_NEEDS_REFACTOR** |
| `Microsoft.EntityFrameworkCore.Design` 8.0.4 | on Infrastructure | keep | **EXISTS_AND_GOOD** (package) / **MISSING** (capability) |
| `Microsoft.EntityFrameworkCore.Tools` / `dotnet-ef` pin / `EFCore.NamingConventions` | **0** in `*.csproj` | A61 §2.1 | **MISSING** |
| On-disk `IEntityTypeConfiguration<T>` | **0** (`Configurations/` is an **empty** directory) | 43 split maps | **MISSING** |
| HEAD-tracked configs (5 files) | **deleted in worktree** (`git status` `D`) | never were migrations | **DEPRECATED** / leftover |
| Integration `PostgresMigrationTests` / Testcontainers / Respawn | **0** | §60 item 1 | **MISSING** |
| §60 “PostgreSQL migrations” | **FAIL** | required integration area | **FAIL** |
| §72.3 “Use migrations.” | **FAIL** | senior-engineer rule | **FAIL** |

**Score:** migrations **0 / 15**. Schema-history rows that this tree can write: **0**. Do not treat a green `dotnet test`, a running demo API, or Compose `postgres:16` as evidence that PostgreSQL can be created, upgraded, or shared.

`DealReason.Migration = 15` is an MT5 Manager deal-reason enum (`A82`). It is **not** an EF migration.

---

## 1. Method

Read-only. Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were **not** edited by this agent.

| Action | Result |
|---|---|
| `Test-Path` on `Persistence\Migrations`, `Persistence\Sql`, `Infrastructure\Migrations`, `apps\*\Migrations`, `tests\*\Migrations` | all **False** |
| `Get-ChildItem -Recurse -Directory -Filter Migrations` under `D:\Prop`, exclude `bin`/`obj`/`node_modules`/`reports` | **empty** |
| `git ls-files` `Migrat` / `*.sql` / `ModelSnapshot` / `Designer` under product | **empty** (vendor PHP dump under `mt5-sdk/vendor/.../sql-dump.sql` only) |
| `Test-Path` each A30 `202608180001`–`015` filename + three named test class paths | all **false** |
| SHA-256 + bytes + last-write of Persistence / hosts / DI / csproj / compose / tests | §2 |
| Grep product `*.cs` for `EnsureCreated`, `Migrate`, `IDesignTimeDbContextFactory`, `MigrationsAssembly`, `ApplyConfigurationsFromAssembly`, `IEntityTypeConfiguration` | **3** `EnsureCreatedAsync`; **0** of the rest on disk |
| Grep `*.csproj` for `EFCore.NamingConventions`, `EntityFrameworkCore.Tools`, `Testcontainers`, `Respawn` | **0** |
| `git status` / `git ls-files` / `git show HEAD:` on Persistence | HEAD had 5 stub configs + a different `TraderDbContext`; **no** `Migrations/` at HEAD either |
| `dotnet build` Infrastructure Release | first attempt **FAIL** (transient bad config); second attempt **PASS** after that file disappeared (see §9) |
| C++ `mt5-sdk/src` `CREATE TABLE` | **0**; ledger **INSERT**s into tables this context never creates |

Did **not** run `dotnet ef` (nothing to list). Did **not** start hosts. Did **not** hit Compose Postgres. Did **not** dump `.env` secrets (only confirmed `DATABASE_URL` uses the `<SECRET>` sentinel).

---

## 2. Measured files (2026-08-18 ~13:40)

Product files only. Hashes SHA-256. Sizes bytes.

| Bytes | SHA-256 | Last write | Path |
|---:|---|---|---|
| 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | 13:15:01 | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 13:14:18 | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 13:12:48 | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 12097 | `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` | 13:35:59 | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 5082 | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` | 13:34:59 | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 13:35:15 | `D:\Prop\apps\api\Program.cs` |
| 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | 13:15:01 | `D:\Prop\apps\mt5-worker\Program.cs` |
| 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | 13:15:01 | `D:\Prop\apps\fix-worker\Program.cs` |
| 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | 13:37:36 | `D:\Prop\apps\api\appsettings.json` |
| 478 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | 13:37:35 | `D:\Prop\apps\api\appsettings.Development.json` |
| 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | 12:54:40 | `D:\Prop\apps\mt5-worker\appsettings.json` |
| 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | 12:54:18 | `D:\Prop\apps\fix-worker\appsettings.json` |
| 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | 12:55:15 | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | 13:17:42 | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |
| 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | 13:18:05 | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | 13:18:40 | `D:\Prop\docker-compose.yml` |

```text
D:\Prop\src\Infrastructure\Persistence\
  Configurations\          ← EXISTS as a directory; 0 children on disk
  EfTradingStore.cs
  TraderDbContext.cs       ← 20 DbSets, inline fluent, no factory
  Migrations\              ← MISSING (directory does not exist)
  Sql\                     ← MISSING

D:\Prop\apps\api\Migrations\                 ← MISSING
D:\Prop\apps\mt5-worker\Migrations\          ← MISSING
D:\Prop\apps\fix-worker\Migrations\          ← MISSING
D:\Prop\tests\Integration\Migrations\        ← MISSING
D:\Prop\tests\Integration\Persistence\       ← MISSING
```

`TraderDbContext.cs` hash is **unchanged vs C29 / D19** (`AFB195AC…`, 174 lines, 20 `DbSet`s, 20 `ToTable`s). The folder question did not move.

### Drift vs C29 (same day, later clock)

| Surface | C29 | D51 now | Migration-relevant? |
|---|---|---|---|
| `Persistence/Migrations/` | missing | **still missing** | no |
| `TraderDbContext.cs` | `AFB195AC…` / 5951 B | **same** | no |
| `DependencyInjection.cs` | `EF0E0E46…` / 1900 B | **same** | no |
| Worker `Program.cs` | both 859 B, same hashes | **same** | no |
| API `Program.cs` | 4658 B `E914FA98…` | 4731 B `61B1E0D1…` | still `EnsureCreatedAsync` at line 87 |
| API `appsettings.json` | 431 B; empty `TraderIntelligence` CS (C29) | 1254 B; `ConnectionStrings:Postgres` (unused key) | **yes** — key name does not match DI |
| `EfTradingStore.cs` | 9020 B `05103CE5…` | 12097 B `DC03BBE6…` | store grew; still no `ON CONFLICT` / migrate |
| `DemoSeeder.cs` | 4942 B `139D8F87…` | 5082 B `A6416491…` | still first-empty-broker seed |
| `Configurations/` | 0 children | 0 children (HEAD 5 files **deleted**; see §8) | still not migrations |
| Root `.env.example` | present in A103 | **`Test-Path` False** | hygiene; not a migration |

C29 is **not stale** on the folder / `Migrate` / A30 score. It **is** stale on API `appsettings` key names and on the HEAD-vs-worktree config deletion.

---

## 3. Binding quotes

Architecture §60 integration tests, required:

```text
PostgreSQL migrations
```

Architecture §72.3:

```text
3. Use migrations.
```

A61 §3.3 (folder contract):

| Rule | Detail |
|---|---|
| Assembly | `TraderIntelligence.Infrastructure` |
| Folder | `Persistence/Migrations/` |
| History table | `__ef_migrations_history` (default) |
| First migration name | `20260818_InitialSection45` (or tool timestamp) |
| `EnsureCreated` | **Forbidden** in hosts |
| Tests | `PostgresMigrationTests` against Testcontainers Postgres 16 |

A30 §3: fifteen versioned files under `src/Infrastructure/Persistence/Migrations/`, never squash, companion SQL under `Persistence/Sql/`. **0 of 15 exist.** Later (not v1) names `2026xxxxxx_ModelRegistry.cs` / `2026xxxxxx_FixTradeAndExecution.cs` also **do not exist**.

C47.1 is the increment that is supposed to emit the first real migration and delete `EnsureCreated`. It is **PLAN**, not code.

---

## 4. A30 catalog — every expected file is absent

`Test-Path` on each path below = **False**.

| Id | Expected EF file | Tables (A30) | On disk |
|---|---|---|---|
| 0001 | `202608180001_ExtensionsAndSystem.cs` | `pgcrypto`; `schema_meta` | **no** |
| 0002 | `202608180002_BrokersAndConnections.cs` | `brokers`, `broker_connections` | **no** |
| 0003 | `202608180003_GroupsAndPlanMappings.cs` | `mt5_groups`, `plan_group_mappings` | **no** |
| 0004 | `202608180004_AccountsAndSnapshots.cs` | `mt5_accounts`, `mt5_account_snapshots` | **no** |
| 0005 | `202608180005_SymbolsAndSourceTicks.cs` | `mt5_symbols`, `mt5_xau_ticks` | **no** |
| 0006 | `202608180006_OrdersDealsPositions.cs` | `mt5_orders`, `mt5_deals`, `mt5_positions_current` | **no** |
| 0007 | `202608180007_CheckpointsOutboxEvents.cs` | `sync_checkpoints`, `outbox_events`, `ingestion_events`, `system_events`, `audit_logs` | **no** |
| 0008 | `202608180008_CanonicalSymbols.cs` | `canonical_instruments`, `source_symbol_mappings` | **no** |
| 0009 | `202608180009_ReconstructedTrades.cs` | `reconstructed_trades` | **no** |
| 0010 | `202608180010_TraderScoresAndFlags.cs` | feature snapshots, scores, history, states, flags | **no** |
| 0011 | `202608180011_VenuesAndDestinationQuotes.cs` | venues, dest symbols, dest quotes | **no** |
| 0012 | `202608180012_FixQuoteSessions.cs` | `fix_sessions`, `fix_session_events` | **no** |
| 0013 | `202608180013_CopyIntents.cs` | `copy_intents`, `copy_allocations` | **no** |
| 0014 | `202608180014_ShadowBook.cs` | shadow orders/fills/positions/performance | **no** |
| 0015 | `202608180015_ShadowRiskDecisions.cs` | `risk_decisions`, `risk_events` | **no** |

Companion `Persistence/Sql/<same_name>.sql` — **0 / 15**.

A30 I1 test `tests/Integration/Persistence/MigrationTests.cs` — **missing**. A10/A90 names `PostgresMigrationTests` / `PostgreSqlMigrationTests` — **missing**.

---

## 5. Host schema path = `EnsureCreatedAsync` only

Every process that builds a `TraderDbContext` at startup uses the same two-step ritual: create-from-model, then `DemoSeeder`.

| Host | File | Lines | After `Build()` |
|---|---|---|---|
| API | `D:\Prop\apps\api\Program.cs` | 84–93 | `EnsureCreatedAsync()` then `DemoSeeder.SeedAsync` |
| mt5-worker | `D:\Prop\apps\mt5-worker\Program.cs` | 11–20 | same |
| fix-worker | `D:\Prop\apps\fix-worker\Program.cs` | 11–20 | same |

API (worktree, hash `61B1E0D1…`):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(/* ... */);
}
```

Workers (mt5 and fix are the same 10-line block except usings; hashes unchanged vs C29):

```csharp
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(/* ... */);
}
```

`EnsureCreated` / `EnsureCreatedAsync` materializes the **current** model as a blank schema. It does **not**:

- write `__EFMigrationsHistory`
- apply a versioned Up script
- survive a later model change (no incremental DDL)
- record which of A30 `0001`–`0015` is present
- refuse to run if the live schema is a different shape

When *any* tables already exist, `EnsureCreated` is a **no-op**. A later `dotnet ef migrations add` + `Database.Migrate()` against that database sees an empty history and tries to `CREATE` tables that already exist → fail. That is why A61 forbids `EnsureCreated` as schema authority: it poisons the first real Postgres with an unversioned stub.

Three hosts each call it. That is **three schema authorities**, not one migrator job (A54 / A65).

HEAD `apps/api/Program.cs` was still the weatherforecast template. HEAD workers did not touch a `DbContext`. `EnsureCreated` is a **worktree** addition. It is still not a migration.

---

## 6. Default store is InMemory — Compose Postgres is unused

`AddTraderIntelligence` (`DependencyInjection.cs` 19–29; hash **unchanged**):

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

| Surface | What is there | What DI reads |
|---|---|---|
| `apps/api/appsettings.json` | `ConnectionStrings:Postgres` (localhost / `postgres` / **empty password**) and `Redis`. **No** `TraderIntelligence` key. | `GetConnectionString("TraderIntelligence")` → **null**; no `DATABASE_URL` key → **InMemory** |
| `apps/api/appsettings.Development.json` | same unused `Postgres` key | same |
| `apps/api/Properties/launchSettings.json` | no CS env / no `DATABASE_URL` | InMemory |
| `apps/mt5-worker/appsettings.json` (+ Development) | logging only | InMemory |
| `apps/fix-worker/appsettings.json` (+ Development) | logging only | InMemory |
| `docker-compose.yml` `api` service | only `ASPNETCORE_ENVIRONMENT=Development` | InMemory (Postgres container is unused by the API) |
| `D:\Prop\.env` | `DATABASE_URL=…Password=<SECRET>` | even if a host loaded `.env` (they do not, by default), the `<SECRET>` substring **forces InMemory** |
| `D:\Prop\.env.example` | **`Test-Path` False** at measure time | A30 I0 hygiene gap |

Default launch of API, mt5-worker, and fix-worker therefore all take the InMemory branch.

InMemory facts that matter here:

1. The name `"trader-intelligence"` is **per process**, not a shared server. Three hosts = three isolated demo worlds.
2. InMemory does not implement PostgreSQL unique indexes, partial indexes, `jsonb`, `timestamptz`, `xid`/`xmin`, `ON CONFLICT`, or `SKIP LOCKED`.
3. `EnsureCreated` on InMemory is a model materialize, not a schema migration.
4. The `else` `UseNpgsql(connection)` path has **no** `MigrationsAssembly`, **no** `EnableRetryOnFailure`, **no** `UseSnakeCaseNamingConvention`, **no** command timeout, and **no** subsequent `Migrate()`. Pointing a host at Compose Postgres today would still `EnsureCreated` the stub model (PascalCase columns).

`docker-compose.yml` starts `postgres:16` (`ti` / `ti_dev_only` / `trader_intelligence`) and `redis:7`. There is no `migrate` one-shot service. Compose Postgres stays empty unless an operator bypasses this DI.

The new `ConnectionStrings:Postgres` key is a **name mismatch**, not a wiring. A61 / D23 expect `ConnectionStrings:TraderIntelligence` or `DATABASE_URL`.

---

## 7. Design-time: package without a factory

`TraderIntelligence.Infrastructure.csproj` already references:

- `Microsoft.EntityFrameworkCore.Design` **8.0.4** (`PrivateAssets=all`)
- `Microsoft.EntityFrameworkCore.InMemory` **8.0.4**
- `Npgsql.EntityFrameworkCore.PostgreSQL` **8.0.4**

Missing for `dotnet ef`:

| Need | Present? |
|---|---|
| `IDesignTimeDbContextFactory<TraderDbContext>` (or target `TraderIntelligenceDbContext`) | **No** |
| `MigrationsAssembly` on `UseNpgsql` | **No** |
| EF Design on startup project `apps/api` | **No** — API csproj is Swashbuckle / Serilog / SignalR only |
| `Microsoft.EntityFrameworkCore.Tools` / pinned `dotnet-ef` | **No** |
| `EFCore.NamingConventions` | **No** |
| Env `TI_POSTGRES` reader | **No** |
| Target context name `TraderIntelligenceDbContext` | **No** — stub is still `TraderDbContext` |

A61 §3.2 factory is specified and **not implemented**. `dotnet ef migrations add InitialSection45 --project src/Infrastructure --startup-project apps/api` has no design-time Npgsql context to build. Even if someone added a factory tomorrow, `--output-dir Persistence/Migrations` would be the **first** file in a folder that does not exist yet.

---

## 8. HEAD had stub configs. Worktree deleted them. None of that is a `Migrations/` folder.

`git ls-files -- src/Infrastructure/Persistence` at measure time:

```text
src/Infrastructure/Persistence/Configurations/BrokersConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5AccountsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5DealsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5GroupsConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5PositionsConfiguration.cs
src/Infrastructure/Persistence/TraderDbContext.cs
```

`git status --short` on the same tree: those five configs are **`D`** (deleted on disk). `Configurations/` remains as an empty directory. `TraderDbContext.cs` is **`M`**. `EfTradingStore.cs` and `DependencyInjection.cs` are **untracked**.

HEAD `BrokersConfiguration` (blob `3c750cef…`, 837 bytes) is **not** a migration. It is `IEntityTypeConfiguration<Brokers>` against a **plural type that does not exist** in current Domain (`Broker` is singular). Shadow properties (`id`, `code`, `name`, `created_at`). Unique on `code`. No `Migration` attribute, no `Up`/`Down`, no snapshot.

HEAD `TraderDbContext` called `ApplyConfiguration(new BrokersConfiguration())` (and listed many more configuration types that were **never committed** — `ReconstructedTradesConfiguration`, `CanonicalInstrumentsConfiguration`, …). HEAD `DbSet` types were also plural (`Brokers`, `Mt5Deals`, `ShadowFills`, …). Current Domain / worktree context uses singular entity names.

`git ls-tree -r HEAD` matching `Migrat` / `Persistence/Sql` = **empty**. **Git has never contained an EF migration.**

Do **not** count deleted HEAD configs as “migrations started.” They were incomplete fluent maps against the wrong CLR names.

---

## 9. Transient file during this census (not on disk at close)

Mid-measure (~13:37–13:39) a file existed at:

```text
D:\Prop\src\Infrastructure\Persistence\Configurations\ReconstructedTradesConfiguration.cs
```

1772 bytes, SHA-256 `E9581103DE593B4087AA24A63D6D1DD402E39292F4706957CBC994CC4589373B`, last write 13:37:35. It implemented `IEntityTypeConfiguration<ReconstructedTrades>` (plural type **does not exist**; Domain type is `ReconstructedTrade`). Columns were a different shape (`deal_ticket` unique, `volume` as `decimal` mapped `bigint`, `DateTime created_at`). `TraderDbContext` does **not** call `ApplyConfigurationsFromAssembly` / `ApplyConfiguration`, so even a correct class would have been **dead**.

`dotnet build` Infrastructure Release **failed** with CS0246 (`ReconstructedTrades`, and a transient `DestinationQuoteSnapshot` miss while `DestinationQuote.cs` was being rewritten by another agent).

By ~13:39:33 the file was **gone**. `Configurations/` empty again. Second `dotnet build` Infrastructure Release: **0 warnings, 0 errors**.

This report does **not** restore or delete that file. It is recorded so a later wave does not treat a green build as “configurations never existed today.” It also is **not** an EF migration.

---

## 10. Tests do not apply migrations

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

## 11. What `EnsureCreated` would emit if Npgsql were used

`TraderDbContext` has **20** `DbSet`s and **20** `ToTable(...)` names. Fluent mapping is inline. On-disk `Configurations/` is empty. No `HasColumnName`, no `HasDatabaseName`, no `UseSnakeCaseNamingConvention()`, no `HasDefaultSchema("public")`.

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
| `reconstructed_trades` | I4 (`0009`) | yes | 4-col index is **not unique** (C06 / D19) |
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

Later A30 / §45 tables also absent: `trader_feature_snapshots`, `trader_states`, `trader_risk_flags`, `execution_venues`, `destination_symbols`, `fix_session_events`, `copy_allocations`, `shadow_fills`, `shadow_positions`, `shadow_performance`, `risk_events`, plus ML `model_*` (A52 hold).

If an operator set a real `TraderIntelligence` / `DATABASE_URL` **today**:

1. Hosts call `EnsureCreatedAsync` → PascalCase quoted columns, 20 stub tables, **no** `__EFMigrationsHistory`.
2. A later `dotnet ef migrations add` + `Database.Migrate()` would see an empty history and try to `CREATE` tables that already exist → fail.
3. Model edits would **not** upgrade the live database.
4. API + two workers would race `EnsureCreated` + `DemoSeeder`. Seeder bails if `Brokers.Any()`, so the first process wins the demo world; the others skip. That is not a migration.

D19 coverage (18 / 43 §45 names) is the model, not a migrated schema. Table-name presence ≠ versioned Up.

---

## 12. Second, also-unmigrated schema (C++ ledger)

C++ `mt5-sdk/src/services/mt5_ledger_store.cpp` **INSERT**s into:

- `mt5_raw_events` (`ON CONFLICT (server_key, source_event_id)`)
- `mt5_deals_ledger` (`ON CONFLICT (server_key, deal_ticket, revision_no)`)

Grep of `mt5-sdk/src` for `CREATE TABLE` = **0**. Those tables are **not** `ToTable` names on `TraderDbContext`. They are not in A30 `0001`–`0015`. They are a second schema assumption with **no** versioned DDL in this repo (the only product-adjacent `.sql` is vendor `mt5-sdk/vendor/MetaTrader5SDK/Examples/Web/PHP/web_registration/sql-dump.sql`, unrelated).

Do not treat C# `EnsureCreated` as creating the C++ ledger.

---

## 13. Scorecard

```text
Persistence/Migrations/ directory                    0
Persistence/Sql/ directory                           0
EF migration .cs files                               0
Companion .sql files                                 0
git-tracked migration files                          0
__EFMigrationsHistory writers                        0
Database.Migrate / MigrateAsync call sites           0
IDesignTimeDbContextFactory                          0
MigrationsAssembly on UseNpgsql                      0
On-disk IEntityTypeConfiguration                     0
HEAD stub configs (deleted in worktree)              5   (not migrations)
EnsureCreatedAsync in hosts                          3   (api, mt5-worker, fix-worker)
Default provider (empty / <SECRET> / wrong CS key)   InMemory "trader-intelligence"
UseNpgsql branch (operator-injected CS only)         yes, no migrate
API ConnectionStrings:Postgres                       present, unused by DI
Compose api → Postgres connection                    not wired
Integration tests using InMemory                     2 facts
Integration tests using Postgres / Migrate           0
Testcontainers / Respawn                             0
A30 migrations present                               0 / 15
§45 tables creatable via versioned Up                0 / 43
§60 "PostgreSQL migrations"                          FAIL
§72.3 Use migrations                                 FAIL
Infrastructure Release build (after transient gone)  PASS
```

---

## 14. What must land later (not done in this report)

Product source is **not** changed here. When C47.1 / A30 I1 is implemented, the binding order is A61 / A30, not “call `EnsureCreated` against Compose”:

1. Finish Domain types for the missing §45 tables **first** (do not fake-ready `model_*` services; A52).
2. Replace / supersede `TraderDbContext` with `TraderIntelligenceDbContext` + 43 `IEntityTypeConfiguration` + snake_case + named `*_uk` (A61). Do not revive HEAD plural-type stub configs.
3. Add `IDesignTimeDbContextFactory` reading `TI_POSTGRES` / `ConnectionStrings__TraderIntelligence` (placeholders only). Align the API `appsettings` key with what DI actually reads — or change DI to the new key **once**, deliberately.
4. `dotnet ef migrations add` → **new** files under `src/Infrastructure/Persistence/Migrations/` (never hand-write Up SQL as the only artifact; never `EnsureCreated` once the first migration exists).
5. Hosts: delete `EnsureCreatedAsync`. Apply via `Database.MigrateAsync` **or** a dedicated Linux/CI migrator job (A54 / A65). One authority, not three.
6. Remove or strictly gate the InMemory fallback (A90: lab hazard). Default launch must fail closed if Postgres is required, or use an explicit `TI_ALLOW_INMEMORY=1` that tests assert is off in the Postgres fixture.
7. Wire Compose `api` (and a one-shot `migrate` service) to the real connection-string key after migrations exist. Do not `CREATE TABLE` in an init `.sql` that races EF.
8. `PostgresMigrationTests` on Testcontainers `postgres:16`: blank apply, idempotent re-apply, no `EnsureCreated`, history row count, data survives re-apply (A90 §5).
9. If the C++ collector stays, give `mt5_raw_events` / `mt5_deals_ledger` their own versioned DDL — still not `EnsureCreated` from C#.

---

## 15. What this report does not claim

- Did not run `dotnet ef` (no factory, no folder).
- Did not start `docker compose` or prove the `ti` role can log in.
- Did not claim the 20 `ToTable` names are a production schema (D19: 18/43 names, 0/43 A20-complete).
- Did not claim Design 8.0.4 is unused in restore — only that design-time **capability** is missing.
- Did not treat the mid-census `ReconstructedTradesConfiguration.cs` as a lasting product file.
- Did not print `.env` secrets. The measured `DATABASE_URL` uses the `<SECRET>` sentinel and would be rejected by DI even if loaded.
- Did not modify product source.

**Bottom line:** No Migrations folder. Never has been, at HEAD or in the worktree. Schema path is InMemory (default, including the unused `ConnectionStrings:Postgres` key) plus `EnsureCreated` on all three hosts. That is a demo stub, not architecture §60 / §72.3 / A61.
