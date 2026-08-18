# A90 — Integration class list: Postgres migrations, outbox, restart backfill

| Field | Value |
|---|---|
| Agent | A90 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A90_integration_class_list.md` |
| Product source edited | **No.** This file is the only write. |
| Project | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §10–13, §44–45, §58, §60 (first three bullets), §62–63, §67 Phase 1, §72.3 / §72.6–7 |
| Adjacent law (do not fork) | A03, A10, A20, A27, A30 I1/I3, A41 §17.2, A59 §13.2, A64 |

**Scope:** the three §60 integration bullets that are **database-backed**:

```text
PostgreSQL migrations
MT5 backfill/restart
outbox processing
```

**Out of this file:** QuickFIX/n session configuration, FIX parse/build, ExecutionReport handling, position reconciliation, unknown-execution recovery (A10 / A27 / A68). Replay (A67). Unit reconstruction/scoring (A09 / A27). Do not grow this list into a second full inventory.

**Status:** class list + seam note. **0** of these classes exist on disk. This document is not a green test run.

---

## 0. Verdict

| Gate | Required | On disk 2026-08-18 | Evidence |
|---|---:|---|---|
| §60 PostgreSQL migrations | 1 area | **0 classes** | no `Migrations/`; no `Database.Migrate()` test |
| §60 MT5 backfill/restart | 1 area | **0 classes** | `DealIngestionService` has no checkpoint |
| §60 outbox processing | 1 area | **0 classes** | `OutboxEvent` is a row shape; no writer/processor |
| Runnable integration facts | — | **0** | no `[Fact]` under `tests/Integration` |
| Testcontainers package | required for the three bullets | **absent** | Integration csproj |
| EF InMemory package | optional **non-proof** seam | present 8.0.4 | Integration + Infrastructure |

A passing `dotnet test` on this project today is **not** evidence of Postgres, migrations, outbox, or restart-safe backfill. `AddTraderIntelligence` currently falls back to `UseInMemoryDatabase("trader-intelligence")` when the connection string is missing or contains `<SECRET>` (`src/Infrastructure/DependencyInjection.cs`). That fallback is a **lab hazard**, not an integration strategy.

---

## 1. Binding note — EF InMemory vs Testcontainers

This is the decision implementers must not reopen without measurement.

### 1.1 Rule

| Seam | Legal use in `tests/Integration` | Counts as §60 proof? |
|---|---|---|
| **Testcontainers PostgreSQL 16** (`postgres:16-alpine`) | migrations, unique/CHECK/`jsonb`/`timestamptz`, `ON CONFLICT`, `FOR UPDATE SKIP LOCKED`, lease reclaim, two-connection visibility, crash/restart durability | **Yes** |
| Lab Compose Postgres (`A65`, `Host=127.0.0.1;Port=5432`) | allowed **only** as a fallback when Docker Desktop is unavailable, via `TI_TEST_PG` connection string. Same assertions as Testcontainers. Isolated database name per run. | **Yes**, if it is real Postgres |
| **EF Core InMemory** | application orchestration smoke against `ITradingStore` / `DealIngestionService` / `FakeMt5BrokerConnector` when the assertion does **not** depend on SQL | **No** |
| `EnsureCreated()` on Npgsql | forbidden as a stand-in for versioned migrations | **No** |
| SQLite “close enough” | forbidden | **No** |
| Redis | forbidden as outbox or checkpoint authority | **No** |

Architecture §60 item 1 is the literal string **“PostgreSQL migrations.”** InMemory does not run EF/Npgsql migrations, does not enforce unique indexes, does not store `jsonb`, does not implement `SKIP LOCKED`, and cannot prove a second connection is isolated from an uncommitted outbox row.

A03: *InMemory is acceptable for mapper/unit tests; PostgreSQL migrations and outbox processing must be Testcontainers/real Postgres.*  
A41 §17.2: *EF InMemory cannot prove `SKIP LOCKED`, unique conflicts, or `jsonb`.*  
A59 §13.2: *real Postgres / Testcontainers — not InMemory as the only proof.*

### 1.2 What InMemory **cannot** prove (do not write these as InMemory facts)

- `Database.Migrate()` / `__EFMigrationsHistory`
- Unique `(broker_id, deal_ticket)` / `(broker_id, login)` / `(event_type, idempotency_key)`
- `ON CONFLICT DO NOTHING` vs EF `AnyAsync` + `Add` race
- Same-transaction raw + outbox: connection B cannot see rows until commit
- `FOR UPDATE SKIP LOCKED` (two processors, one row)
- Lease reclaim of `processing` + expired `locked_until`
- `jsonb` payload CHECKs / `jsonb_typeof(payload) = 'object'`
- Partial indexes (`WHERE status = 'pending'`)
- `pg_advisory_xact_lock` per `(broker_id, login)`
- `LISTEN/NOTIFY` wake-up (optional; polling remains correctness)
- Restart of a **process** with durable rows still present
- `CITEXT` / `timestamptz` / `numeric` rounding vs `double`

### 1.3 What InMemory **may** host (labeled, never counted)

Only classes in §8 (`Seam=InMemory`). They exist so a developer can exercise `DealIngestionService` + `FakeMt5BrokerConnector` without Docker. They **must** carry:

```csharp
[Trait("Category", "Integration")]
[Trait("Seam", "InMemory")]
[Trait("CountsAs60", "false")]
```

If CI needs a single “integration” filter, use `Seam=Testcontainers`. Do not run InMemory facts as the Phase 1 gate.

### 1.4 Product default is the wrong seam

```23:29:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }
```

Integration collection fixtures **must not** call `AddTraderIntelligence` without an explicit Npgsql connection string. A dedicated test that `UseInMemoryDatabase` is **not** selected when `TI_TEST_PG` / Testcontainers is present is listed in §5 (`PostgresFixtureSafetyTests`).

Infrastructure currently **fails to compile**: `DependencyInjection.cs` / `DemoSeeder.cs` reference `TraderIntelligence.Mt5.Connectors` but `TraderIntelligence.Infrastructure.csproj` does not reference `src/Mt5`. Measured 2026-08-18:

```text
error CS0234: The type or namespace name 'Mt5' does not exist in the namespace 'TraderIntelligence'
```

Do not treat a stale `bin/Debug` DLL as a green SUT. Product source is not edited by this agent.

### 1.5 `DemoSeeder` is not a test fixture

`src/Infrastructure/Seeding/DemoSeeder.cs` embeds live lab identifiers (Achiever/StarwaveFX hosts, manager logins, `live-us-eqx-01.p.c-trader.com`, sender `live.pepperstone.1369850`). Integration tests seed **synthetic** brokers (`ACHIEVER` / `STARWAVEFX` codes are fine; hosts must be `test.invalid`, manager login `0`, no FIX password, no live SenderCompID). Do not call `DemoSeeder.SeedAsync` from this suite.

---

## 2. Measured product surface (what tests can bind to today)

Honest snapshot. Names below are **targets** unless marked **EXISTS**.

| Type | Path | State vs this list |
|---|---|---|
| `TraderDbContext` | `src/Infrastructure/Persistence/TraderDbContext.cs` | **EXISTS.** 20 `DbSet`s, fluent `OnModelCreating`. No `Migrations/`. No `IDesignTimeDbContextFactory`. |
| `EfTradingStore` | `src/Infrastructure/Persistence/EfTradingStore.cs` | **EXISTS.** `ITradingStore` upserts groups/accounts/deals/positions; **no** checkpoint; **no** outbox; deal dedupe is `AnyAsync` then `Add` (not `ON CONFLICT`). |
| `DealIngestionService` | `src/Application/Ingestion/DealIngestionService.cs` | **EXISTS.** Full-window `GetDealsAsync(from,to)` per account. **No** read/persist checkpoint. **No** paging. **No** outbox. |
| `ReconstructionScoringService` | same file | **EXISTS.** Rebuilds trades + `TraderScore`. Not an outbox handler. |
| `OutboxEvent` | `src/Domain/Entities/OutboxEvent.cs` | **EXISTS, thin.** `Type`, `AggregateId`, `PayloadJson`, `OccurredAt`, `ProcessedAt`, `Attempts`, `LastError`, `CorrelationId`. Missing A41 columns (`status`, `idempotency_key`, `event_type` text, `payload jsonb`, lease, `expires_at`). Unique index today = **none** (only `ProcessedAt` index). |
| `OutboxEventType` | `src/Domain/Enums/OutboxEventType.cs` | **EXISTS.** Five values only. |
| `SyncCheckpoint` | `src/Domain/Entities/SyncCheckpoint.cs` | **EXISTS, thin.** Unique `(BrokerId, Login, Stream)`. Missing A59 `scope_type` / fencing / overlap / lease. Unused by ingestion. |
| `IOutboxWriter` / `IOutboxProcessor` | Application | **MISSING** (A02 O6/O8, A41 §13) |
| `IHistoricalBackfillService` / `ISyncCheckpointStore` | Application | **MISSING** (A02 O1/O2, A59 §14) |
| `FakeMt5BrokerConnector` / `DemoBrokerFactory` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | **EXISTS.** Deterministic Achiever/StarwaveFX deals. No incomplete-page / crash-mid-page API yet. |
| `IMt5BrokerConnector` | `src/Application/Contracts/Mt5Contracts.cs` | **EXISTS.** |
| `IBrokerConnector` | `src/Mt5/Connectors/IBrokerConnector.cs` | **EXISTS** (older surface: `SubscribeEventsAsync`). Do not invent a third connector. |
| EF `Configurations/*.cs` | `src/Infrastructure/Persistence/Configurations/` | **empty folder** (plural shadow types removed). |
| `apps/mt5-worker` | `Worker.cs` | template `Task.Delay(1000)`. No backfill job, no outbox drain. |
| Integration tests | `tests/Integration` | csproj only. **No** `.cs` test files. Packages: xunit 2.5.3, FluentAssertions 6.12.0, **InMemory 8.0.4**. No Testcontainers, no Respawn, **no `src/Mt5` reference**. |

`TraderDbContext` tables mapped today (inline):

```text
brokers, mt5_groups, mt5_accounts, mt5_deals, mt5_positions_current,
reconstructed_trades, canonical_instruments, source_symbol_mappings,
trader_scores, trader_score_history, outbox_events, sync_checkpoints,
copy_intents, risk_decisions, execution_intents, shadow_orders,
destination_quotes, fix_sessions, audit_logs, kill_switches
```

Uniques that **do** exist in the fluent model and **must** be proven on Postgres (InMemory will silently allow duplicates):

| Table | Unique |
|---|---|
| `brokers` | `code` |
| `mt5_groups` | `(broker_id, name)` |
| `mt5_accounts` | `(broker_id, login)` |
| `mt5_deals` | `(broker_id, deal_ticket)` |
| `mt5_positions_current` | `(broker_id, position_ticket)` |
| `canonical_instruments` | `code` |
| `source_symbol_mappings` | `(broker_id, source_symbol)` |
| `trader_scores` | `(broker_id, login)` |
| `sync_checkpoints` | `(broker_id, login, stream)` |
| `copy_intents` | `idempotency_key` |
| `execution_intents` | `cl_ord_id` |
| `fix_sessions` | `qualifier` |

`outbox_events` has **no** unique today. A41 requires `(event_type, idempotency_key)`. Migration tests must **fail** until that constraint lands — do not weaken the assertion to match the thin entity.

---

## 3. Naming, layout, traits

Namespace root: `TraderIntelligence.Tests.Integration`.  
Folder = last namespace segment.  
File = `{ClassName}.cs`.  
Class suffix: `Tests` (fixtures are not `*Tests`).

```text
tests/Integration/
  Fixtures/
    PostgresCollection.cs
    PostgresFixture.cs
    InMemoryDbFixture.cs
    FakeClock.cs
  Fakes/
    ScriptedMt5HistorySource.cs
    RecordingOutboxHandler.cs
    NoOpFixAdapter.cs
  Persistence/
    PostgreSqlMigrationTests.cs
    CoreSchemaContractTests.cs
    UniqueIndexContractTests.cs
    MigrationIdempotencyTests.cs
    PostgresFixtureSafetyTests.cs
  Outbox/
    OutboxAtomicCommitTests.cs
    OutboxProcessingTests.cs
    OutboxSkipLockedConcurrencyTests.cs
    OutboxLeaseReclaimTests.cs
    OutboxPoisonAndReplayTests.cs
    OutboxExpiryNoCatchUpTests.cs
    OutboxBackfillDoesNotEnqueueShadowOrRiskTests.cs
    OutboxDoesNotCallFixFromCallbackTests.cs
    OutboxRiskApproveDoesNotSendFixTests.cs
    OutboxIdempotencyPersistenceTests.cs
    OutboxHandlerReceiptTests.cs
  Mt5/
    Mt5BackfillRestartTests.cs
    Mt5LiveIngestIdempotencyTests.cs
    Mt5LiveThenRestartNoDupesTests.cs
    Mt5AccountSyncCheckpointTests.cs
    Mt5GroupDiscoveryIdempotencyTests.cs
    Mt5ReconciliationFillsGapsTests.cs
    Mt5IncompleteFetchDoesNotAdvanceCheckpointTests.cs
    Mt5DualBrokerIsolationTests.cs
    Mt5CheckpointLeaseFencingTests.cs
    Mt5OverlapWindowRestartTests.cs
  InMemory/
    InMemoryDealIngestionOrchestrationTests.cs
    InMemoryTradingStoreSmokeTests.cs
```

Every Testcontainers class:

```csharp
[Collection(PostgresCollection.Name)]
[Trait("Category", "Integration")]
[Trait("Seam", "Testcontainers")]
[Trait("CountsAs60", "true")]
[Trait("Area", "Migrations")] // or Outbox | Backfill
```

xUnit collection: one Postgres 16 container per test assembly run. Respawn (or `TRUNCATE … CASCADE`) between facts. Apply migrations **once** in `IAsyncLifetime.InitializeAsync`.

Do not add empty classes as decoration. If the SUT is missing, either omit the file or use a single skipped fact whose skip message names the missing type. Prefer omit.

Delete any future `UnitTest1.cs`. Do not `Assert.True(true)`.

---

## 4. Packages and project wiring (when a coding agent is assigned)

Change `tests/Integration/TraderIntelligence.Tests.Integration.csproj` only in a later coding task. Pin versions with the rest of the 8.0.4 EF line.

| Package | Version pin | Why |
|---|---|---|
| `Testcontainers` | 3.10.0 (or current 3.10.x / 4.x that still targets net8) | container lifecycle |
| `Testcontainers.PostgreSQL` | same line | `PostgreSqlBuilder` → `postgres:16-alpine` |
| `Respawn` | 6.2.1 | reset between facts without rebuilding the container |
| `Npgsql` | 8.0.x matching Infrastructure | raw SQL assertions / second connection |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.4 | **keep** for §8 only |
| `xunit` / `FluentAssertions` | already 2.5.3 / 6.12.0 | stay |

Project references:

| Reference | Today | Required for this list |
|---|---|---|
| `src/Domain` | yes | yes |
| `src/Application` | yes | yes |
| `src/Infrastructure` | yes | yes |
| `src/Fix.CTrader` | yes | **not required** for these three bullets |
| `src/Mt5` | **no** | **yes** — `FakeMt5BrokerConnector` |
| `apps/mt5-worker` | no | no (extract services into libraries) |

CI: skip Testcontainers facts only when `TI_SKIP_CONTAINERS=1` **and** `TI_TEST_PG` is unset. If both are unset, the suite must **fail** (missing Postgres is not a pass). Never skip because “InMemory ran.”

---

## 5. Shared fixtures (not `*Tests`)

| Type | Path | Job |
|---|---|---|
| `PostgresCollection` | `Fixtures/PostgresCollection.cs` | `[CollectionDefinition]` + `ICollectionFixture<PostgresFixture>` |
| `PostgresFixture` | `Fixtures/PostgresFixture.cs` | Start `postgres:16-alpine` (user `ti`, db `ti_test`, tmpfs optional). If `TI_TEST_PG` is set, use that instead (no container). Call `Database.Migrate()` once. Expose `CreateDbContext()` and `CreateNpgsqlConnection()`. Disable InMemory. Fail fast if the provider is not Npgsql. |
| `DatabaseReset` | same file or helper | Respawn on `public` (ignore `__EFMigrationsHistory`) **or** `TRUNCATE` listed tables `CASCADE` in FK-safe order |
| `InMemoryDbFixture` | `Fixtures/InMemoryDbFixture.cs` | `UseInMemoryDatabase(Guid.NewGuid().ToString())` for §8 only |
| `FakeClock` | `Fixtures/FakeClock.cs` | Freeze `occurred_at` / `source_event_time` / `expires_at` / lease now. Required for expiry + overlap tests |
| `ScriptedMt5HistorySource` | `Fakes/ScriptedMt5HistorySource.cs` | Extends `FakeMt5BrokerConnector`: page size, throw after N `GetDealsAsync` calls, return `incomplete` (when that API exists), emit the same ticket twice, dual-broker identical numeric tickets. **No** live Manager API |
| `RecordingOutboxHandler` | `Fakes/RecordingOutboxHandler.cs` | Records envelopes; can throw transient/permanent; never opens a socket |
| `NoOpFixAdapter` | `Fakes/NoOpFixAdapter.cs` | Counts `NewOrderSingle` attempts; must stay 0 on ingest/outbox paths |

`PostgresFixture` assertions in `InitializeAsync`:

1. Connection string host is `127.0.0.1` / `localhost` / container hostname — never `*.p.c-trader.com`.
2. `context.Database.ProviderName` contains `Npgsql`.
3. `REAL_COPY_EXECUTION_ENABLED` is not `true` in the test host.

### 5.1 `PostgresFixtureSafetyTests` (meta, still Testcontainers)

| Method | Must prove |
|---|---|
| `Fixture_uses_npgsql_not_in_memory` | Provider is Npgsql |
| `Fixture_does_not_call_demo_seeder` | No live host / SenderCompID rows after `InitializeAsync` |
| `Missing_postgres_does_not_silently_use_in_memory` | If container start fails and `TI_TEST_PG` unset, collection fails (not InMemory fallback) |

---

## 6. Cluster A — PostgreSQL migrations (§60 item 1)

SUT: versioned EF migrations against `TraderDbContext` (A30 names `0001`–`0007`; A20 is the catalog). Until migrations exist, these classes are the **acceptance list**, not a green run.

Canonical names (A27 + A10 unified):

### 6.1 `Persistence.PostgreSqlMigrationTests`

Seam: **Testcontainers**. A10 `PostgresMigrationTests` / A30 `MigrationTests` → **this class**.

| Method | Must prove |
|---|---|
| `Empty_database_applies_all_migrations` | `Database.Migrate()` on a blank `postgres:16` succeeds; `__EFMigrationsHistory` has one row per migration file |
| `Apply_is_idempotent_on_second_run` | Second `Migrate()` is a no-op; table count unchanged |
| `Migrate_does_not_call_ensure_created` | Test host never invokes `EnsureCreated` (grep-level: fixture helper throws if called) |
| `Down_is_not_required_but_up_is_versioned` | Migration ids are monotonic / timestamped; no hand-edited prod schema |

### 6.2 `Persistence.CoreSchemaContractTests`

Seam: **Testcontainers**. A27 `CoreSchemaContractTests`.

Query `information_schema.tables` / `pg_indexes` after migrate.

**Phase 1 required tables** (must exist before claiming I1/I3):

```text
brokers
mt5_groups
mt5_accounts
mt5_deals
mt5_positions_current
sync_checkpoints
outbox_events
```

**Present on `TraderDbContext` today — assert once they are migrated:**

```text
reconstructed_trades, canonical_instruments, source_symbol_mappings,
trader_scores, trader_score_history, copy_intents, risk_decisions,
execution_intents, shadow_orders, destination_quotes, fix_sessions,
audit_logs, kill_switches
```

**A20/A41 required and still missing from the fluent model — tests must FAIL until added (do not delete the fact):**

```text
ingestion_events
outbox_handler_receipts
outbox_poison_events
```

Optional later (do not block Phase 1 ingest): `mt5_orders`, `mt5_account_snapshots`, `broker_connections`, `plan_group_mappings`, `system_events`, execution tables in §44.

| Method | Must prove |
|---|---|
| `Phase1_core_tables_exist` | list above |
| `Source_tables_have_broker_id_not_null` | `mt5_groups`, `mt5_accounts`, `mt5_deals`, `mt5_positions_current`, `sync_checkpoints` (when scope is broker) |
| `Positions_table_is_mt5_positions_current_not_mt5_positions` | catalog name (A20 / A59 gap #3) |
| `Outbox_and_checkpoint_tables_exist` | `outbox_events`, `sync_checkpoints` |
| `No_kafka_or_broker_side_tables` | no `kafka_*` / `inbox_offset` tables (§13, A80) |

### 6.3 `Persistence.UniqueIndexContractTests`

Seam: **Testcontainers**. This is the reason InMemory is illegal here.

| Method | Must prove |
|---|---|
| `Duplicate_broker_code_is_rejected` | `brokers.code` |
| `Duplicate_deal_ticket_same_broker_is_rejected` | `(broker_id, deal_ticket)` — second insert throws unique violation |
| `Same_deal_ticket_two_brokers_is_allowed` | §10 compound identity |
| `Duplicate_account_login_same_broker_is_rejected` | `(broker_id, login)` |
| `Same_login_two_brokers_is_allowed` | Achiever 10001 vs StarwaveFX 10001 |
| `Outbox_idempotency_unique_is_enforced` | `(event_type, idempotency_key)` per A41 — **currently missing in model; fact stays red until migration** |
| `Copy_intent_idempotency_key_is_unique` | `copy_intents.idempotency_key` |
| `Execution_clordid_is_unique` | `execution_intents.cl_ord_id` |
| `Checkpoint_unique_is_per_scope_not_global` | two brokers cannot share one deals cursor (A20/A59) |

### 6.4 `Persistence.MigrationIdempotencyTests`

Seam: **Testcontainers**.

| Method | Must prove |
|---|---|
| `Fresh_apply_then_insert_then_reapply_preserves_rows` | seed one deal; `Migrate()` again; row + ticket still there |
| `Schema_hash_or_column_set_is_stable` | `mt5_deals` has `deal_ticket`, `broker_id`, `volume_native`/`volume` as documented by the mapping — fail if fluent model and database diverge |

---

## 7. Cluster B — Outbox processing (§60 item 3)

SUT (architecture / A41; **MISSING** today):

- `IOutboxWriter` / `TransactionalOutboxWriter` — same ambient EF/Npgsql transaction as the domain write
- `IOutboxProcessor` / `PostgresOutboxProcessor` — reclaim, expire, `FOR UPDATE SKIP LOCKED`, dispatch, ack
- Handlers: `TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent`
- Tables: `outbox_events`, `outbox_handler_receipts`, `outbox_poison_events`

Bind to `OutboxEventType` as it exists. SQL `event_type` stays kebab-case (A41). Do not invent Kafka.

### 7.1 `Outbox.OutboxAtomicCommitTests`

Seam: **Testcontainers**. A30 `OutboxCommitTests`. Two Npgsql connections.

| Method | Must prove |
|---|---|
| `Raw_deal_and_outbox_row_commit_together` | after commit, connection B sees both `mt5_deals` and `outbox_events` |
| `Rollback_removes_both` | throw after `Add` before commit → connection B sees **zero** deals and **zero** outbox rows |
| `Uncommitted_outbox_is_invisible_to_second_connection` | isolation: processor connection cannot claim an uncommitted row |
| `On_conflict_do_nothing_does_not_update_payload` | first durable payload wins (A41 §7.4) |

### 7.2 `Outbox.OutboxProcessingTests`

Seam: **Testcontainers**. A27 / A41 canonical name.

| Method | Must prove |
|---|---|
| `Processor_marks_processed_and_is_idempotent` | one pending row → processed; second `ProcessBatch` is a no-op |
| `Crash_after_commit_before_mark_retries_once` | kill handler after domain write, before ack → redelivery → receipt / unique → still one domain row |
| `Failed_handler_does_not_lose_the_event` | transient throw → row stays claimable (`pending` + future `next_attempt_at`) |
| `Five_event_types_are_the_only_claimable_values` | CHECK or enum map; unknown type is permanent poison |
| `Risk_reject_is_processed_not_poison` | A41 §9.1 `handler` class |
| `Notification_does_not_require_redis_for_ack` | Redis down after `system_events` insert still `processed` (if Redis is even configured) |

### 7.3 `Outbox.OutboxSkipLockedConcurrencyTests`

Seam: **Testcontainers**. A41. **Impossible on InMemory.**

| Method | Must prove |
|---|---|
| `Two_processors_one_row_one_claim` | parallel `ProcessBatchAsync` → `Handle` called once |
| `Two_processors_two_rows_split` | two pending → each claimed at most once; both processed |
| `Dedicated_type_allow_list_is_honored` | mt5-worker types do not claim `risk-check-request`; fix-worker does not claim `trade-completed` (A41 §12, A64) |

### 7.4 `Outbox.OutboxLeaseReclaimTests`

Seam: **Testcontainers**. A41 §8.3.

| Method | Must prove |
|---|---|
| `Stuck_processing_past_locked_until_is_reclaimed` | FakeClock + one claim → expire lease → second processor handles once |
| `Reclaim_does_not_increment_attempt_again` | attempt counted on original claim only |
| `Heartbeat_extends_lease_for_long_score_job` | optional; skip until handler heartbeat exists |

### 7.5 `Outbox.OutboxPoisonAndReplayTests`

Seam: **Testcontainers**. A41 §9.4–9.5.

| Method | Must prove |
|---|---|
| `Max_attempts_moves_row_to_poisoned_and_snapshot` | `outbox_poison_events` inserted; claim index no longer returns the row |
| `Permanent_error_poisons_immediately` | missing required payload field |
| `Replay_without_force_does_not_duplicate_domain` | receipt unique blocks re-handle |
| `Expired_shadow_is_not_replayed` | operator replay refused when `expires_at` is past |

### 7.6 `Outbox.OutboxExpiryNoCatchUpTests`

Seam: **Testcontainers**. A41 `NoBlindCatchUpTests` + §63.

| Method | Must prove |
|---|---|
| `Shadow_past_expires_at_is_expired_not_handled` | no `shadow_orders` / `execution_intents` |
| `Risk_past_expires_at_is_expired` | no `risk_decisions` send path |
| `Trade_completed_has_no_expires_at_and_still_drains` | long outage still scores |

### 7.7 `Outbox.OutboxBackfillDoesNotEnqueueShadowOrRiskTests`

Seam: **Testcontainers**. A41 §11 / A59 L7.

| Method | Must prove |
|---|---|
| `Historical_backfill_writes_zero_shadow_copy_intent` | completed old XAU trades in fixture |
| `Historical_backfill_writes_zero_risk_check_request` | same |
| `Backfill_may_enqueue_one_score_update_per_trader_watermark` | key `score-update:{broker}:{login}:backfill-watermark:{checkpoint}` |
| `Live_completion_may_enqueue_trade_completed` | contrast path |

### 7.8 `Outbox.OutboxDoesNotCallFixFromCallbackTests`

Seam: **Testcontainers** (or InMemory **plus** `NoOpFixAdapter` — still **not** §60 outbox proof by itself). A27 / A41 / §32.

| Method | Must prove |
|---|---|
| `Live_ingest_path_does_not_invoke_new_order_single` | `NoOpFixAdapter.SendCount == 0` |
| `Live_ingest_path_does_not_resolve_ifix_session` | DI of ingest host has no TRADE sender |
| `Only_ioutbox_writer_is_called_from_callback_surface` | when live ingest exists |

### 7.9 `Outbox.OutboxRiskApproveDoesNotSendFixTests`

Seam: **Testcontainers**. A41.

| Method | Must prove |
|---|---|
| `Approve_persists_execution_intent_not_sent_only` | `ExecutionOrderStatus` not-sent; `NoOpFixAdapter.SendCount == 0` |
| `Outbox_redelivery_does_not_insert_second_execution_intent` | unique on `copy_intent_id` / `cl_ord_id` |

### 7.10 `Outbox.OutboxIdempotencyPersistenceTests`

Seam: **Testcontainers**.

| Method | Must prove |
|---|---|
| `Producer_key_trade_completed_is_stable` | `trade-completed:{broker_id}:{reconstructed_trade_id}` |
| `Producer_key_score_update_after_trade_is_stable` | A41 §7.1 |
| `Duplicate_enqueue_is_one_row` | `ON CONFLICT DO NOTHING` |
| `Reversal_produces_two_shadow_keys` | different `exposure_class` |

### 7.11 `Outbox.OutboxHandlerReceiptTests`

Seam: **Testcontainers**. A41 §6.3.

| Method | Must prove |
|---|---|
| `Successful_handle_writes_receipt` | `(handler_name, idempotency_key)` |
| `Redelivery_hits_receipt_and_acks` | no second score history row |

---

## 8. Cluster C — MT5 backfill / restart (§60 item 2)

SUT (architecture §12 / A59; **partial** today):

```text
Read checkpoint → Fetch history → Normalize → Upsert idempotently → Persist checkpoint
```

`DealIngestionService.SyncBrokerAsync` is **not** this loop. It walks every account over a caller-supplied `[from,to]` and never reads `SyncCheckpoints`. Tests must target `IHistoricalBackfillService` / `SyncCheckpointStore` **once those types exist**. Until then, do not rename `DealIngestionService` into a false green.

`ScriptedMt5HistorySource` is the only broker. Never open Manager API from this suite.

### 8.1 `Mt5.Mt5BackfillRestartTests`

Seam: **Testcontainers**. A10 / A27 / A59 `BackfillRestartTests`.

| Method | Must prove |
|---|---|
| `Backfill_upserts_deals_by_broker_login_ticket` | first run inserts N unique tickets |
| `Killed_mid_page_resumes_from_checkpoint_without_duplicates` | script throws after K deals; new host process (new `DbContext`, same database) resumes; `mt5_deals` count = unique tickets |
| `Second_full_backfill_does_not_increase_row_count` | rerun → 0 new identities; `duplicate_hits` or equivalent increments |
| `Checkpoint_moves_only_after_successful_chunk_commit` | crash before persist checkpoint → next run re-fetches overlap, still no dupes |
| `All_symbols_are_persisted_not_only_xauusd` | A59 L13 — include a non-XAU deal in the script |

### 8.2 `Mt5.Mt5LiveIngestIdempotencyTests`

Seam: **Testcontainers**. A27.

| Method | Must prove |
|---|---|
| `Live_deal_then_backfill_same_ticket_is_one_row` | |
| `Backfill_then_live_same_ticket_is_one_row` | |
| `Same_payload_does_not_enqueue_second_outbox` | if live writes `DealPersisted` / ingest audit |

### 8.3 `Mt5.Mt5LiveThenRestartNoDupesTests`

Seam: **Testcontainers**. A59.

| Method | Must prove |
|---|---|
| `Replay_same_event_batch_twice_one_deal_row` | |
| `Process_restart_replays_unacked_live_queue_without_dupes` | durable raw + checkpoint / outbox |

### 8.4 `Mt5.Mt5AccountSyncCheckpointTests`

Seam: **Testcontainers**. A59. ~5,000-account **shape**, not 5,000 real logins.

| Method | Must prove |
|---|---|
| `Mid_group_enum_resume_does_not_reupsert_finished_logins_as_new` | script 5 logins; kill after 2; resume; 5 `mt5_accounts` rows |
| `Accounts_stream_is_per_broker` | Achiever cursor ≠ StarwaveFX cursor |
| `Plan_group_map_is_not_used_as_filter` | extra group still stored (A59 / §9) |

Use a 5-login fixture, not 5,000. Scale is an ops soak, not this class.

### 8.5 `Mt5.Mt5GroupDiscoveryIdempotencyTests`

Seam: **Testcontainers**. A59.

| Method | Must prove |
|---|---|
| `Full_group_walk_twice_same_broker_name_set` | unique `(broker_id, name)` |
| `Same_group_string_on_two_brokers_is_two_rows` | |

### 8.6 `Mt5.Mt5ReconciliationFillsGapsTests`

Seam: **Testcontainers**. A59. Source reconcile ≠ cTrader `execution_reconciliation_*`.

| Method | Must prove |
|---|---|
| `Reconcile_inserts_tickets_missing_from_partial_backfill` | |
| `Reconcile_repairs_current_position_book` | `ReplacePositions` / upsert current book |
| `Reconcile_does_not_mark_multi_month_backfill_complete` | separate `deals_reconcile` stream (A59 Q3) |

### 8.7 `Mt5.Mt5IncompleteFetchDoesNotAdvanceCheckpointTests`

Seam: **Testcontainers**. A59 L10 / IMT5Client completeness.

| Method | Must prove |
|---|---|
| `GetDeals_incomplete_leaves_cursor_unchanged` | script returns false / incomplete |
| `Empty_last_10_seconds_does_not_mark_caught_up` | 40s+ history lag (A59 §10) |
| `Overlap_sec_is_at_least_120_on_resume` | |

### 8.8 `Mt5.Mt5DualBrokerIsolationTests`

Seam: **Testcontainers**. A27 / §10 / §69.1–3.

| Method | Must prove |
|---|---|
| `Identical_numeric_tickets_on_achiever_and_starwave_are_two_rows` | |
| `Identical_logins_on_two_brokers_are_two_accounts` | |
| `Checkpoints_are_not_shared` | |

### 8.9 `Mt5.Mt5CheckpointLeaseFencingTests`

Seam: **Testcontainers**. A59 fencing token.

| Method | Must prove |
|---|---|
| `Stale_worker_cannot_advance_checkpoint_with_old_token` | |
| `Two_workers_same_stream_only_one_lease_holder` | |

### 8.10 `Mt5.Mt5OverlapWindowRestartTests`

Seam: **Testcontainers**.

| Method | Must prove |
|---|---|
| `Resume_window_starts_at_cursor_minus_overlap` | not at last ticket + 1 second with no overlap |
| `Deals_in_overlap_are_idempotent_hits_not_new_rows` | |

---

## 9. InMemory-only (explicitly not §60)

These two classes are the **only** legal InMemory residents in this project. They exist because `FakeMt5BrokerConnector` + `EfTradingStore` can already run a happy-path ingest without Docker. They must not be renamed to `Mt5BackfillRestartTests`.

### 9.1 `InMemory.InMemoryDealIngestionOrchestrationTests`

Seam: **InMemory**. `CountsAs60=false`.

| Method | May prove (orchestration only) |
|---|---|
| `SyncBroker_inserts_demo_achiever_deals` | `DealIngestionService` + `DemoBrokerFactory` + `EfTradingStore` on InMemory |
| `Second_sync_returns_zero_new_inserts_in_this_provider` | `AnyAsync` path — **not** a unique-index proof |
| `Reconstruction_scoring_runs_after_ingest` | `ReconstructionScoringService` |

### 9.2 `InMemory.InMemoryTradingStoreSmokeTests`

Seam: **InMemory**. `CountsAs60=false`.

| Method | May prove |
|---|---|
| `ResolveBrokerId_by_code` | |
| `UpsertDeal_returns_false_on_second_call_same_ctx` | same `DbContext` instance only |

If these two pass and Testcontainers classes fail or are absent, Phase 1 is **not** done.

---

## 10. Name unification (do not create duplicates)

| Earlier name | Home | A90 canonical |
|---|---|---|
| A10 `PostgresMigrationTests` | Persistence | `PostgreSqlMigrationTests` |
| A27 `PostgreSqlMigrationTests` | Persistence | `PostgreSqlMigrationTests` |
| A30 `MigrationTests` | Persistence | `PostgreSqlMigrationTests` |
| A27 `CoreSchemaContractTests` | Persistence | `CoreSchemaContractTests` |
| A10 `OutboxProcessingTests` | Outbox | `OutboxProcessingTests` |
| A30 `OutboxCommitTests` | Outbox | `OutboxAtomicCommitTests` |
| A41 `SkipLockedConcurrencyTests` | Outbox | `OutboxSkipLockedConcurrencyTests` |
| A41 `PoisonAndReplayTests` | Outbox | `OutboxPoisonAndReplayTests` |
| A41 `LeaseReclaimTests` | Outbox | `OutboxLeaseReclaimTests` |
| A41 `BackfillDoesNotEnqueueShadowOrRiskTests` | Outbox | `OutboxBackfillDoesNotEnqueueShadowOrRiskTests` |
| A41 `NoBlindCatchUpTests` | Outbox | `OutboxExpiryNoCatchUpTests` |
| A41 `RiskApproveDoesNotSendFixTests` | Outbox | `OutboxRiskApproveDoesNotSendFixTests` |
| A10/A27 `Mt5BackfillRestartTests` | Mt5 | `Mt5BackfillRestartTests` |
| A59 `BackfillRestartTests` | Mt5 | `Mt5BackfillRestartTests` |
| A59 `LiveThenRestartNoDupesTests` | Mt5 | `Mt5LiveThenRestartNoDupesTests` |
| A59 `ReconciliationFillsGapsTests` | Mt5 | `Mt5ReconciliationFillsGapsTests` |
| A59 `OutboxProcessAfterCrashTests` | Outbox | `OutboxProcessingTests` (crash method) |
| A59 `AccountSyncCheckpointTests` | Mt5 | `Mt5AccountSyncCheckpointTests` |
| A59 `GroupDiscoveryIdempotencyTests` | Mt5 | `Mt5GroupDiscoveryIdempotencyTests` |
| A27 `DualBrokerIsolationTests` | Mt5 | `Mt5DualBrokerIsolationTests` |
| A27 `OutboxDoesNotCallFixFromCallbackTests` | Outbox | `OutboxDoesNotCallFixFromCallbackTests` |

One class per row. Do not add both A10 and A27 names.

---

## 11. Implementation order (matches A30 I1 → I3, A41 §18, A59 §14)

Do not write 26 empty test classes. Add each class when the SUT can fail for a real reason.

| Order | Product first | Then add |
|---|---|---|
| 1 | Versioned EF migrations + `IDesignTimeDbContextFactory`; Infrastructure references `src/Mt5` if DI stays | `PostgreSqlMigrationTests`, `CoreSchemaContractTests`, `UniqueIndexContractTests`, `PostgresFixtureSafetyTests` |
| 2 | `TransactionalOutboxWriter` + unique `(event_type, idempotency_key)` | `OutboxAtomicCommitTests`, `OutboxIdempotencyPersistenceTests` |
| 3 | `PostgresOutboxProcessor` + receipts | `OutboxProcessingTests`, `OutboxSkipLockedConcurrencyTests`, `OutboxLeaseReclaimTests`, `OutboxHandlerReceiptTests` |
| 4 | Poison / expiry / type allow-lists | `OutboxPoisonAndReplayTests`, `OutboxExpiryNoCatchUpTests` |
| 5 | `ISyncCheckpointStore` + `IHistoricalBackfillService` + scripted incomplete fetch | `Mt5BackfillRestartTests`, `Mt5IncompleteFetchDoesNotAdvanceCheckpointTests`, `Mt5OverlapWindowRestartTests`, `Mt5DualBrokerIsolationTests` |
| 6 | Account/group streams | `Mt5AccountSyncCheckpointTests`, `Mt5GroupDiscoveryIdempotencyTests` |
| 7 | Live ingest + source reconcile | `Mt5LiveIngestIdempotencyTests`, `Mt5LiveThenRestartNoDupesTests`, `Mt5ReconciliationFillsGapsTests`, `Mt5CheckpointLeaseFencingTests` |
| 8 | Reconstruction enqueue + handler policy | `OutboxBackfillDoesNotEnqueueShadowOrRiskTests`, `OutboxDoesNotCallFixFromCallbackTests`, `OutboxRiskApproveDoesNotSendFixTests` |

Optional anytime: §9 InMemory smokes. They never gate Phase 1.

---

## 12. Safety (binding)

- No live Manager API. No `57.128.141.65` / `84.201.6.142` from `DemoSeeder`.
- No live FIX host `live-us-eqx-01.p.c-trader.com`, no account `1369850`.
- `REAL_COPY_EXECUTION_ENABLED` stays false. Outbox tests must not send `NewOrderSingle`.
- Backfill must not enqueue stale `shadow-copy-intent` / `risk-check-request` (§63, A41 §11).
- Kafka / Redis-as-outbox are out of scope (A80, A03).
- Secrets never in fixtures. Synthetic `SenderCompID=TEST.TRADE` if a later FIX class lands in this project — not this file’s job.

---

## 13. Count

| Lane | Test classes | Support types | §60 proof? |
|---|---:|---:|---|
| Persistence (migrations) | 5 | — | yes |
| Outbox | 11 | — | yes (except 7.8 if run InMemory-only) |
| Backfill / restart | 10 | — | yes |
| InMemory smokes | 2 | — | **no** |
| Fixtures / fakes | — | 7 (`PostgresCollection`, `PostgresFixture`, `InMemoryDbFixture`, `FakeClock`, `ScriptedMt5HistorySource`, `RecordingOutboxHandler`, `NoOpFixAdapter`) | — |
| **Total named** | **28** | **7** | **26 proof + 2 non-proof** |

§60 first three bullets coverage (1:1, expanded not replaced):

| §60 bullet | Primary class | Supporting classes |
|---|---|---|
| PostgreSQL migrations | `PostgreSqlMigrationTests` | `CoreSchemaContractTests`, `UniqueIndexContractTests`, `MigrationIdempotencyTests`, `PostgresFixtureSafetyTests` |
| MT5 backfill/restart | `Mt5BackfillRestartTests` | nine other `Mt5.*` classes in §8 |
| outbox processing | `OutboxProcessingTests` | ten other `Outbox.*` classes in §7 |

A27 listed 14 integration classes for **all eight** §60 bullets. This file **replaces** only the first three rows of that table with the expanded set above. The five FIX/reconcile classes in A27 remain authoritative and are **not** duplicated here.

---

## 14. Explicit non-goals

- Implementing product source, migrations, or the test project from this agent.
- FIX harness classes (A10 §61, A27 §7, A68).
- Replay project (A67).
- Claiming “idempotency proven” because InMemory `AnyAsync` returned false.
- 5,000-account soak inside xUnit.
- Using `EnsureCreated` to paper over missing migrations.
- Calling `DemoSeeder` from CI.

---

## 15. Disposition

| Metric | Value |
|---|---|
| §60 areas in scope | 3 (migrations, backfill/restart, outbox) |
| Canonical proof classes named | 26 |
| InMemory non-proof classes named | 2 |
| Classes present in `tests/Integration` | **0** |
| Testcontainers package | **absent** |
| InMemory package | present (Integration + Infrastructure DI fallback) |
| Versioned migrations | **absent** |
| Outbox writer / processor | **MISSING** |
| Checkpointed backfill | **MISSING** (`DealIngestionService` is a full-window sync) |
| Infrastructure compile (this audit) | **FAIL** (`TraderIntelligence.Mt5` not referenced) |
| Product source changed by A90 | **No** |

Implement the classes in §11 when the corresponding SUTs exist. Until Testcontainers facts are green, **do not** treat InMemory smokes, `DemoSeeder`, or `UseInMemoryDatabase("trader-intelligence")` as Phase 1 or §60 exit evidence.
