# A03 — `src/Infrastructure` audit vs architecture v2 §§11, 13, 44, 45

| Field | Value |
|---|---|
| Agent | A03 senior engineer (read-only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\src\Infrastructure` |
| Spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` |
| Sections compared | §11 Raw MT5 Data Layer; §13 transactional outbox; §44 cTrader execution tables; §45 recommended core tables. Redis authority from §5 / §28 used as a hard constraint. |
| Product source modified | **No.** This audit wrote only this report. |
| Verdict | **FAIL — skeleton only.** Packages for EF Core / Npgsql / Redis are present and version-aligned. There is no persistence implementation. PostgreSQL is not yet the durable source of truth in the .NET tree. Redis is unused, therefore it is not currently authoritative for orders/positions/balances — vacuously compliant, not designed-compliant. |

Classification vocabulary is architecture §73.B:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

---

## 1. Honest measured state

Do not treat `TraderIntelligence.Infrastructure` as a data layer.

Measured facts:

- Source files under the project (excluding `bin/` and `obj/`): **2**.
  - `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` (871 bytes, SHA-256 `9F285E6C9C7663D65DCB24265752684CB3D47E630D21E6FCEF320B6718A24002`, written 2026-08-18 12:55:08).
  - `D:\Prop\src\Infrastructure\Class1.cs` (80 bytes, SHA-256 `B5E70DFB3E6CAB342B3A7A1F9AC147C025742B9697D6502282A910A293A0D3CB`, written 2026-08-18 12:54:15).
- Product `.cs` files in the project: **1** (`Class1.cs`).
- `DbContext` / `UseNpgsql` / `AddDbContext` / `IEntityTypeConfiguration` / `IDesignTimeDbContextFactory` / `Migrate(` in any `D:\Prop\**\*.cs`: **0 matches**.
- `ConnectionMultiplexer` / `IConnectionMultiplexer` / `StackExchange.Redis` **usings** in any `D:\Prop\**\*.cs`: **0 matches**.
- `.sql` migrations or EF `Migrations/` folder under `src/Infrastructure`: **none**.
- Hosts (`apps/api`, `apps/mt5-worker`, `apps/fix-worker`) reference the project but do not register a DbContext, outbox dispatcher, or Redis multiplexer. `apps/api/Program.cs` is still the WeatherForecast template. `appsettings.json` has **no** `ConnectionStrings`, Postgres, or Redis keys.

This is Phase 0 audit evidence, not a working Phase 1 ingestion stack.

---

## 2. Inventory of `src/Infrastructure`

### 2.1 On-disk product tree

```text
D:\Prop\src\Infrastructure\
  TraderIntelligence.Infrastructure.csproj
  Class1.cs
  bin\   (Debug net8.0 build outputs)
  obj\   (restore + compile intermediates)
```

No folders for `Persistence`, `Migrations`, `Outbox`, `Redis`, `Health`, `DependencyInjection`, or entity configurations.

### 2.2 Project file (verbatim)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
    <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

Layering of project references is correct for clean architecture: Infrastructure → Application → Domain. Domain and Application are themselves empty `Class1` templates (`D:\Prop\src\Domain\Class1.cs`, `D:\Prop\src\Application\Class1.cs`). There are no repository ports, no outbox port, no unit-of-work, no entity types for Infrastructure to implement.

Solution membership is correct: `Mt5TraderIntelligence.sln` includes `TraderIntelligence.Infrastructure` (`{14EDD461-7C2D-43AC-BC2B-F2DCAC644491}`).

Consumers that already reference the project (still with zero DI wiring):

| Consumer | Path | Uses Infrastructure types? |
|---|---|---|
| API | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | No |
| MT5 worker | `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | No |
| FIX worker | `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | No |
| Integration tests | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | No (template `UnitTest1`) |

### 2.3 `Class1.cs` (verbatim)

```csharp
namespace TraderIntelligence.Infrastructure;

public class Class1
{

}
```

`dotnet new classlib` leftover. No members, no attributes, no EF types, no Redis types. **DEPRECATED** as a product type. It should be deleted when implementation starts; it is not a persistence primitive.

Compile input hash (`obj\Debug\net8.0\TraderIntelligence.Infrastructure.csproj.CoreCompileInputs.cache`) is `81662c6d38b27970d9b6eb73cca86a6c59765cd9b0397b5229ac56cb9def0897` and the only product compile input is `Class1.cs`.

---

## 3. Package audit (EF / Npgsql / Redis)

Restore succeeded (`obj\project.nuget.cache` `"success": true`). Direct references and the relevant restore graph:

| Package | Declared | Restored | Role vs v2 | Classification |
|---|---|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | 8.0.4, `PrivateAssets=all` | 8.0.4 | Design-time migrations. `PrivateAssets=all` is correct so Design/Roslyn does not leak to API/workers. | **EXISTS_AND_GOOD** (package hygiene only) |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.4 | 8.0.4 | §5 “Entity Framework Core or existing proven data layer” + “Npgsql”; §254–260 PostgreSQL as SoT. | **EXISTS_AND_GOOD** (package only) |
| `Microsoft.EntityFrameworkCore` | not direct | 8.0.4 (transitive) | Required runtime. Version-locked to the Npgsql EF provider. | **EXISTS_AND_GOOD** (transitive) |
| `Microsoft.EntityFrameworkCore.Relational` | not direct | 8.0.4 | Relational mapping / migrations runtime. | **EXISTS_AND_GOOD** (transitive) |
| `Npgsql` | not direct | **8.0.3** | ADO.NET provider pulled by Npgsql.EF 8.0.4. | **EXISTS_NEEDS_REFACTOR** (patch skew; see below) |
| `StackExchange.Redis` | 2.8.0 | 2.8.0 | §5 / §262–276 cache + coordination client. | **EXISTS_AND_GOOD** as a *client* choice; implementation **MISSING** |
| `Microsoft.EntityFrameworkCore.Tools` | absent | — | Optional CLI convenience; Design is enough if `dotnet-ef` is installed. | **MISSING** (low severity) |
| `Microsoft.Extensions.Hosting.Abstractions` / DI extension package | absent as direct | only what EF pulls | Needed for a future `AddInfrastructure(IServiceCollection, IConfiguration)`. | **MISSING** (low; can stay implicit) |
| `Microsoft.Extensions.Caching.StackExchangeRedis` | absent | — | Not required. Raw `StackExchange.Redis` is the right primitive for locks/leases/scores. IDistributedCache would be the wrong abstraction for fencing tokens. | **EXISTS_AND_GOOD** (correct omission) |
| Kafka / MassTransit / RabbitMQ / NATS | absent | — | §13: do not introduce a broker on day one. | **EXISTS_AND_GOOD** (correct omission) |

### 3.1 What the package set gets right

- Target framework `net8.0` matches architecture §5 (C# / .NET 8+).
- EF Core 8.0.4 and `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 are **version-paired**. That is the hard compatibility rule for this provider.
- Design is isolated with `PrivateAssets=all`.
- Redis client is present; no competing cache product.
- No premature Kafka package, matching §13 (“Do not preemptively introduce distributed infrastructure”).

### 3.2 Package gaps / hygiene (not implementation)

1. **No `DbContext`, so Design is dead weight.** `dotnet ef migrations add` will fail until a context and a startup / `IDesignTimeDbContextFactory` exist. Classification: Design package **EXISTS_AND_GOOD**, design-time *capability* **MISSING**.
2. **Npgsql 8.0.3 vs provider 8.0.4.** Restore graph in `obj\project.assets.json` and `bin\Debug\net8.0\TraderIntelligence.Infrastructure.deps.json` both pin `Npgsql/8.0.3`. Not a functional blocker at this empty stage. When implementation starts, pin a current 8.0.x Npgsql that the 8.0.4 (or a later 8.0.x) provider supports, and bump the whole EF 8.0.4 line together. Do not mix EF 8 with EF 9.
3. **No connection-string / Redis configuration surface** in any host `appsettings*.json`. Secrets must stay out of Git (§55). The gap today is the opposite: there is not even a placeholder `ConnectionStrings:Postgres` / `Redis` key or `.env.example`.
4. **No Redis usage policy type.** A raw `IConnectionMultiplexer` in the composition root later would make it easy to persist orders/positions/balances in Redis. Architecture forbids that. The package itself is not unsafe; the missing wrapper is the control.
5. **Design restore graph is heavy** (Roslyn 4.5.0, Humanizer, `System.Text.Json` 8.0.0 via DependencyModel). Acceptable while `PrivateAssets=all`. Do not “fix” by adding more packages.
6. Integration tests already reference `Microsoft.EntityFrameworkCore.InMemory` 8.0.4 (`tests/Integration/TraderIntelligence.Tests.Integration.csproj`) but have no tests. InMemory is acceptable for mapper/unit tests; **PostgreSQL migrations and outbox processing must be Testcontainers/real Postgres** (§60). Do not let InMemory become the only “integration” proof.

`bin\Debug\net8.0\TraderIntelligence.Infrastructure.runtimeconfig.json` exists for a class library. The csproj does not set `OutputType`. Harmless build artifact; not an executable entry point.

---

## 4. Architecture requirements (extracted)

### 4.1 §5 / §28 Redis rule (binding, even though Redis is §5 not §11/13/44/45)

PostgreSQL is the durable source of truth (§254–260).

Redis is allowed only for:

- live scores
- short-lived cache
- distributed execution-session ownership
- short-lived locks
- live dashboard data

Hard ban (quoted):

> Do not use Redis as the authoritative store for orders, positions, or balances.

§28 adds: a Redis lease **with fencing token** is one legal way to implement single-active FIX TRADE ownership, but **“The database must remain the authority for execution state.”**

§62: if the database is unavailable, execution must fail closed. Do not run critical real execution solely from volatile memory (Redis included).

### 4.2 §11 — Raw MT5 data layer

Store source data **before** interpreting it. Raw layer as immutable as practical. Corrections auditable.

| Table (§11) | Notes |
|---|---|
| `mt5_accounts` | |
| `mt5_account_snapshots` | |
| `mt5_orders` | |
| `mt5_deals` | |
| `mt5_positions_current` | current book; still durable in Postgres, not Redis |
| `mt5_groups` | |
| `mt5_symbol_metadata` | name **conflicts** with §45 `mt5_symbols` |
| `mt5_ticks_xauusd` | optional if SDK/feed supports it; name **conflicts** with §45 `mt5_xau_ticks` |
| `sync_checkpoints` | backfill/live resume |
| `ingestion_events` | **not** listed in §45 |

### 4.3 §13 — Outbox instead of Kafka

Use a **PostgreSQL transactional outbox** in the same commit as the raw persist (§12 live flow: validate → deduplicate → persist raw → write outbox row → commit). Background workers drain the outbox.

Event kinds called out:

- trade-completed
- score-update requests
- shadow-copy intents
- risk-check requests
- notification events

§45 names the table `outbox_events`. §58 wants metric `mt5_outbox_backlog`. §60 integration tests must cover outbox processing.

No Kafka, no “event bus product” until measured throughput requires it — hide a future broker behind an abstraction **later**, not now.

### 4.4 §44 — cTrader execution tables

Durable Postgres tables:

| Table (§44) | In §45? |
|---|---|
| `execution_venues` | yes |
| `fix_sessions` | yes |
| `fix_session_events` | yes |
| `destination_symbols` | yes |
| `destination_quotes` | yes |
| `copy_intents` | yes |
| `risk_decisions` | yes |
| `execution_intents` | **no** |
| `fix_orders` | yes |
| `fix_execution_reports` | yes |
| `destination_positions` | yes |
| `source_destination_links` | yes |
| `execution_reconciliation_runs` | **no** |
| `execution_reconciliation_issues` | **no** |

Orders, execution reports, and destination positions are **Postgres tables**. They must not be Redis hashes/streams as the system of record.

### 4.5 §45 — recommended core set (full initial)

`brokers`, `broker_connections`, `mt5_groups`, `plan_group_mappings`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols`, `mt5_xau_ticks`, `reconstructed_trades`, `canonical_instruments`, `source_symbol_mappings`, `trader_feature_snapshots`, `trader_scores`, `trader_score_history`, `trader_states`, `trader_risk_flags`, `model_versions`, `model_predictions`, `model_evaluations`, `shadow_orders`, `shadow_fills`, `shadow_positions`, `shadow_performance`, `copy_intents`, `copy_allocations`, `risk_decisions`, `risk_events`, `execution_venues`, `destination_symbols`, `destination_quotes`, `fix_sessions`, `fix_session_events`, `fix_orders`, `fix_execution_reports`, `destination_positions`, `source_destination_links`, `sync_checkpoints`, `outbox_events`, `audit_logs`, `system_events`.

### 4.6 Spec self-inconsistency (must resolve before any migration)

Architecture v2 disagrees with itself on names and membership. Implementing “all of §11 + §44 + §45” blindly will produce duplicate or missing tables.

| Topic | §11 | §44 | §45 | Audit ruling (recommendation only; not implemented) |
|---|---|---|---|---|
| Symbol metadata | `mt5_symbol_metadata` | — | `mt5_symbols` | One table. Prefer `mt5_symbols` (§45 is the “full initial set”) with a metadata JSON/column set covering §11. |
| XAU ticks | `mt5_ticks_xauusd` | — | `mt5_xau_ticks` | One table. Prefer `mt5_xau_ticks`. |
| Ingestion audit | `ingestion_events` | — | absent (`system_events` / `audit_logs` nearby) | Keep `ingestion_events` for raw collector audit; do not overload `outbox_events`. |
| Outbox | implied by §12/§13 | — | `outbox_events` | **Required.** Name: `outbox_events`. |
| Execution intents | — | `execution_intents` | absent | **Required** for §24/§32/§41 flow. Add to the core set. |
| Reconciliation | — | `execution_reconciliation_runs`, `execution_reconciliation_issues` | absent | **Required** for §42/§43/§54. Add to the core set. |
| Copy allocations / risk events | — | absent | `copy_allocations`, `risk_events` | Keep; they are not Redis. |
| Scores | — | — | `trader_scores` + history | Postgres is authoritative. Redis may *cache* the live score (§5). |

This naming conflict is **MISSING product decision**, not an Infrastructure code defect. Do not generate migrations until names are pinned in a schema ADR.

---

## 5. Table-by-table gap matrix

Every row below is **MISSING** in `src/Infrastructure`. There is no entity, no `DbSet`, no configuration, no migration, no SQL, no repository.

**Union count:** 49 distinct table names across §11 ∪ §44 ∪ §45 (after listing aliases separately). **Implemented:** 0. **Migrations:** 0.

### 5.1 Identity / broker config (§45)

| Table | § | Class |
|---|---|---|
| `brokers` | 45 | MISSING |
| `broker_connections` | 45 | MISSING |
| `mt5_groups` | 11, 45 | MISSING |
| `plan_group_mappings` | 45 | MISSING |

### 5.2 Raw MT5 (§11, §45)

| Table | § | Class |
|---|---|---|
| `mt5_accounts` | 11, 45 | MISSING |
| `mt5_account_snapshots` | 11, 45 | MISSING |
| `mt5_orders` | 11, 45 | MISSING — must be Postgres, never Redis SoT |
| `mt5_deals` | 11, 45 | MISSING |
| `mt5_positions_current` | 11, 45 | MISSING — must be Postgres, never Redis SoT |
| `mt5_symbol_metadata` | 11 | MISSING (alias of `mt5_symbols`) |
| `mt5_symbols` | 45 | MISSING (alias of `mt5_symbol_metadata`) |
| `mt5_ticks_xauusd` | 11 | MISSING (alias of `mt5_xau_ticks`) |
| `mt5_xau_ticks` | 45 | MISSING (alias of `mt5_ticks_xauusd`) |
| `sync_checkpoints` | 11, 45 | MISSING |
| `ingestion_events` | 11 | MISSING |

### 5.3 Reconstruction / scoring / ML (§45; not §11/44 but in the “full initial set”)

| Table | Class |
|---|---|
| `reconstructed_trades` | MISSING |
| `canonical_instruments` | MISSING |
| `source_symbol_mappings` | MISSING |
| `trader_feature_snapshots` | MISSING |
| `trader_scores` | MISSING (Postgres SoT; Redis cache allowed later) |
| `trader_score_history` | MISSING |
| `trader_states` | MISSING |
| `trader_risk_flags` | MISSING |
| `model_versions` | MISSING |
| `model_predictions` | MISSING |
| `model_evaluations` | MISSING |

### 5.4 Shadow copy (§45)

| Table | Class |
|---|---|
| `shadow_orders` | MISSING — not Redis |
| `shadow_fills` | MISSING |
| `shadow_positions` | MISSING — not Redis |
| `shadow_performance` | MISSING |

### 5.5 Copy / risk / execution (§44, §45)

| Table | § | Class |
|---|---|---|
| `copy_intents` | 44, 45 | MISSING |
| `copy_allocations` | 45 | MISSING |
| `risk_decisions` | 44, 45 | MISSING |
| `risk_events` | 45 | MISSING |
| `execution_intents` | 44 | MISSING |
| `execution_venues` | 44, 45 | MISSING |
| `destination_symbols` | 44, 45 | MISSING |
| `destination_quotes` | 44, 45 | MISSING |
| `fix_sessions` | 44, 45 | MISSING |
| `fix_session_events` | 44, 45 | MISSING |
| `fix_orders` | 44, 45 | MISSING — Postgres SoT |
| `fix_execution_reports` | 44, 45 | MISSING |
| `destination_positions` | 44, 45 | MISSING — Postgres SoT |
| `source_destination_links` | 44, 45 | MISSING |
| `execution_reconciliation_runs` | 44 | MISSING |
| `execution_reconciliation_issues` | 44 | MISSING |

### 5.6 Platform (§13, §45)

| Table | § | Class |
|---|---|---|
| `outbox_events` | 13, 45 | MISSING — **P0 for any live ingest** |
| `audit_logs` | 45 | MISSING |
| `system_events` | 45 | MISSING |

Immutability / auditable corrections (§11) are also **MISSING**: no revision columns, no “append-only deal” mapping, no temporal tables, no `xmin`/row-version strategy in this project.

---

## 6. Outbox gap (§13)

Required shape (from §12 + §13), none of which exists in Infrastructure:

```text
MT5 event
  → validate
  → deduplicate
  → persist raw record
  → write transactional outbox event
  → commit
then background worker processes outbox
```

| Outbox capability | Class | Evidence |
|---|---|---|
| `outbox_events` table / EF entity | MISSING | no SQL, no `DbSet` |
| Same-transaction write with raw MT5 row | MISSING | no `DbContext.SaveChanges` / `BEGIN` |
| Dedup key (broker + source event id) | MISSING | — |
| Dispatcher / polling worker | MISSING | `apps/mt5-worker/Worker.cs` is the generic worker template; no outbox loop |
| Poison / retry / backoff columns | MISSING | — |
| Event-type enum covering the five §13 kinds | MISSING | Domain has no types |
| `mt5_outbox_backlog` metric (§58) | MISSING | — |
| Integration test “outbox processing” (§60) | MISSING | `tests/Integration/UnitTest1.cs` is empty |
| Kafka / extra broker | correctly absent | **EXISTS_AND_GOOD** (non-introduction) |

Without an outbox, any future MT5 callback that calls scoring or FIX directly would violate §12. That coupling does not exist yet because ingestion itself does not exist in this project.

---

## 7. Redis authority analysis (binding)

### 7.1 Current .NET behavior

| Question | Answer |
|---|---|
| Is Redis referenced? | Yes — `StackExchange.Redis` 2.8.0 on the Infrastructure csproj. |
| Is Redis connected at runtime by this project? | **No.** No multiplexer, no options, no keys, no hosts configured. |
| Are orders stored in Redis? | **No.** |
| Are positions stored in Redis? | **No.** |
| Are balances stored in Redis? | **No.** |
| Is Redis the SoT for execution state? | **No.** There is no execution state at all. |

Classification of *current* Redis usage: **MISSING** (allowed cache/lock/lease features) and **not UNSAFE** (the ban is not violated because nothing writes).

Vacuous compliance is not a control. The next implementation must add a typed Redis façade that only exposes:

- score cache (TTL, rebuildable from `trader_scores`)
- dashboard projections (TTL, rebuildable from Postgres)
- short locks
- FIX session lease + fencing token (§28)

and that **does not** expose generic `StringSet("order:…")` / `HashSet("position:…")` / `HashSet("balance:…")` helpers.

### 7.2 Adjacent Redis risk (outside this project; do not copy)

`D:\Prop\mt5-sdk\src\services\metrics_service.h` documents a **“Redis fast outbox”** (`terminal_fast_outbox_frames_total`, `terminal_pg_outbox_frames_total`, mirror backlog gauges). That is a different tree (`mt5-sdk`, C++/libpq). It is **not** `src/Infrastructure`.

If that pattern is ported into this .NET Infrastructure, it must stay a *speed path that mirrors into Postgres* and must **never** make Redis the authority for orders, positions, or balances. Architecture §13 wants the **PostgreSQL** transactional outbox as the initial bus. A Redis-first outbox for trading events would be **UNSAFE** relative to v2.

### 7.3 What would be UNSAFE later (not present today)

- Redis hashes as the live order/position/balance book with Postgres as optional archive.
- Serving `/positions` or risk limits from Redis when Postgres is down (§62 requires fail-closed).
- FIX TRADE leadership in Redis **without** a fencing token and **without** Postgres execution rows (`fix_orders`, `destination_positions`, `execution_intents`).
- Caching `mt5_positions_current` or `destination_positions` without a documented TTL and a Postgres rebuild path.

None of those exist in `src/Infrastructure` today.

---

## 8. Persistence-shaped types that should exist and do not

Expected Infrastructure surface for §§11/13/44/45 (none present):

| Expected artifact | Class |
|---|---|
| `TradingDbContext : DbContext` | MISSING |
| `IDesignTimeDbContextFactory<TradingDbContext>` or host `UseNpgsql` | MISSING |
| `AddInfrastructure(this IServiceCollection, IConfiguration)` | MISSING |
| Npgsql snake_case / `NpgsqlEnableLegacyTimestampBehavior` policy | MISSING |
| Fluent entity configs (`IEntityTypeConfiguration<>`) | MISSING |
| Idempotent unique indexes (broker+ticket, ClOrdID, outbox id) | MISSING |
| `IOutboxWriter` + dispatcher | MISSING |
| `IRedisConnection` + lease/lock + score cache | MISSING |
| Health checks (Postgres + Redis) | MISSING |
| EF migrations assembly + checked-in migrations | MISSING |
| Repository implementations of Application ports | MISSING (ports also MISSING in Application) |
| Serilog/OTel enrichers for db/redis (§5 lists them; API has Serilog package only) | MISSING in this project |

`Class1` does not substitute for any row above. **DEPRECATED.**

---

## 9. Adjacent persistence (context only — not Infrastructure)

A C++ libpq path already exists and must not be mistaken for the v2 .NET schema:

| Artifact | Path | Relation to §§11/13/44/45 |
|---|---|---|
| `PgPool` | `D:\Prop\mt5-sdk\src\db\pg_pool.h` | Generic libpq pool. Not EF. Not in Infrastructure. |
| `mt5_raw_events` insert | `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.cpp` | Immutable raw event ledger. **Not** in the v2 table list (closest: §11 raw layer / `ingestion_events`). |
| `mt5_deals_ledger` insert | same file | Revisioned deals. **Not** named `mt5_deals`. |
| `mt5_account_sequence` | `mt5_account_helper.cpp` | Sequence helper. Not in v2 list. |

Classification of that sibling relative to *this* audit target: **out of scope / parallel stack**. It does **not** implement §45. It does **not** fill the Infrastructure gap. When .NET Infrastructure is built, decide explicitly: wrap this ledger, migrate it, or keep it as the native collector’s private store and map into `mt5_*` v2 tables. Do not silently create a second SoT.

Hosts that will consume Infrastructure later still have no connection strings:

- `D:\Prop\apps\api\appsettings.json` — Logging + AllowedHosts only.
- `D:\Prop\apps\api\Program.cs` — WeatherForecast.
- `D:\Prop\apps\mt5-worker\Program.cs` / `apps\fix-worker\Program.cs` — `AddHostedService<Worker>()` only.

---

## 10. Consolidated classification

### 10.1 Project / packages

| Item | Class | Severity |
|---|---|---|
| `net8.0` class library in `/src/Infrastructure` | EXISTS_AND_GOOD | — |
| Project refs to Domain + Application | EXISTS_AND_GOOD | — |
| Absence of Kafka packages | EXISTS_AND_GOOD | — |
| `Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.4 | EXISTS_AND_GOOD | — |
| `Microsoft.EntityFrameworkCore.Design` 8.0.4 `PrivateAssets=all` | EXISTS_AND_GOOD | — |
| `StackExchange.Redis` 2.8.0 package | EXISTS_AND_GOOD | — |
| Transitive Npgsql 8.0.3 vs provider 8.0.4 | EXISTS_NEEDS_REFACTOR | low |
| `Class1.cs` template | DEPRECATED | low |
| DbContext + mappings + migrations | MISSING | **P0** |
| Outbox table + same-commit writer + dispatcher | MISSING | **P0** |
| All §11 raw tables | MISSING | **P0** (Phase 1) |
| All §44 execution tables | MISSING | P2 (Phase 7–8; schema can be reserved earlier) |
| Remaining §45 tables | MISSING | P1–P3 by phase |
| Redis façade with SoT ban | MISSING | P1 (before any Redis write) |
| Redis currently SoT for orders/positions/balances | not present | **not UNSAFE today** |
| Kafka introduced | not present | not DEPRECATED (correctly never added) |

### 10.2 Severity legend used above

- **P0** — cannot start Phase 1 (reliable MT5 ingestion, §67) without it.
- **P1** — required as soon as scores/dashboard/locks exist.
- **P2** — required before FIX TRADE / live copy.
- **P3** — ML/shadow extras; still Postgres, still not Redis SoT.

No component is **UNSAFE** in the running .NET product because almost nothing runs. The failure mode is **absence**, not a Redis SoT violation.

---

## 11. Risk list (Infrastructure-scoped)

| Risk | Why it matters | Mitigation when implementation starts |
|---|---|---|
| Dual schema: C++ `mt5_raw_events` / `mt5_deals_ledger` vs v2 `mt5_deals` | Two truths for the same broker deal | Explicit mapping or a single writer |
| Implementing §11 and §45 names as separate tables | Duplicate symbol/tick stores | Pin names in an ADR first |
| Starting Redis before Postgres schema | Easy to park live positions in Redis | Create `fix_orders` / `destination_positions` / `mt5_positions_current` first; Redis second |
| Redis lease without fencing token | Split-brain FIX TRADE (§28) | Token in Postgres `fix_sessions` row + Redis lease |
| EF InMemory as the only test | §60 requires Postgres migration + outbox tests | Testcontainers Postgres |
| `EnsureCreated` in production | no migration history | checked-in EF migrations only |
| Putting connection strings in `appsettings.json` | §55 | env / user-secrets / vault; placeholders in `.env.example` only |
| Worker callbacks writing scores/FIX in-process | violates §12/§13 | outbox then worker |
| Treating `Class1` / empty hosts as “layer exists” | greenwash | this report |

---

## 12. What this audit does **not** claim

- It does **not** claim PostgreSQL is deployed or empty in any live database. No DB was connected. Schema presence in a server was **not** measured.
- It does **not** claim the C++ ledger tables are absent or present in a running cluster — only that their *writers* exist in `mt5-sdk` and their *names* are not the v2 set.
- It does **not** claim Redis is safe for production. It claims Redis is **unused** in this project.
- It does **not** produce migrations, entities, or DI. Product source was not modified.

---

## 13. Implementation sequence (audit recommendation only)

Do not start this until a schema ADR resolves §11 vs §45 names. Then, in this project only:

1. Delete `Class1.cs`. Add `Persistence/TradingDbContext.cs` + design-time factory.
2. Add `outbox_events` + Phase 1 raw tables (`brokers`, `broker_connections`, `mt5_groups`, `plan_group_mappings`, `mt5_accounts`, `mt5_account_snapshots`, `mt5_orders`, `mt5_deals`, `mt5_positions_current`, `mt5_symbols`, `sync_checkpoints`, `ingestion_events`, `audit_logs`).
3. Same-transaction outbox writer + worker dispatcher + `mt5_outbox_backlog`.
4. Integration test against real Postgres: migrate → insert deal + outbox → dispatcher marks processed; restart-safe.
5. Redis façade last, with an explicit allow-list (scores, locks, FIX lease+fence, dashboard TTL). Code review gate: no Redis writes for order/position/balance documents.
6. Reserve §44 tables before FIX TRADE, even if writers come in Phase 7–8.

---

## 14. Scorecard

| Axis | Score | Notes |
|---|---|---|
| Package selection vs §5 | 8 / 10 | Right three packages; unused. |
| §11 raw layer | 0 / 10 | Zero tables. |
| §13 outbox | 0 / 10 | Zero rows, zero dispatcher; Kafka correctly not added. |
| §44 execution tables | 0 / 10 | Zero tables. |
| §45 core set | 0 / 10 | 0 / 43 names (plus §11/§44-only extras). |
| Redis SoT ban | 10 / 10 *vacuous* | Unused ≠ designed. Treat as **unproven** for go-live. |
| Overall Infrastructure readiness for Phase 1 | **0 / 10** | Skeleton. Not a data layer. |

**PASS/FAIL for architecture v2 §§11, 13, 44, 45: FAIL.**

Evidence roots: `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj`, `D:\Prop\src\Infrastructure\Class1.cs`, `D:\Prop\src\Infrastructure\obj\project.assets.json`, `D:\Prop\src\Infrastructure\bin\Debug\net8.0\TraderIntelligence.Infrastructure.deps.json`, `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5, 11–13, 28, 44–45, 58, 60, 62, 73.
