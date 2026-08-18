# C58 — Outbox entity exists; no dispatcher

| Field | Value |
|---|---|
| Agent | C58 (outbox dispatcher gap, read-only) |
| Date | 2026-08-18 |
| Assigned question | **Outbox entity exists but no dispatcher.** Confirm produce vs consume. Write this report. |
| Artifact | `D:\Prop\reports\swarm\20260818\C58_outbox_dispatcher.md` |
| Product source modified | **No.** Report only. |
| Workspace | `D:\Prop` |
| Law | Architecture v2 **§12** (live flow: persist raw → write transactional outbox → commit; then background workers drain) and **§13** (PostgreSQL outbox, not Kafka; five v1 kinds) |
| Binding specs | A41 (outbox design), A61 §5 (EF map), A64 §5 / §8.8 / §9.10 (hosted processors), A59 (ingest + `OutboxDispatchJob`), A30 (`0007` + `PostgresOutbox*`), A90 / A10 (integration tests), A50 / §58 (`mt5_outbox_backlog`) |
| Related measured reviews | B02 L5/O6–O9, B03, B07 §7, B19 row 11, C05 (DI), C06 (indexes), C07 (workers), C29 (no migrations) |
| Supersedes | A01 / A03 / A41 §3 / `_index_extract.tsv` claim that `outbox_events` / `DbSet<OutboxEvent>` are **MISSING**. Entity + table map **now exist**. Produce and drain still **MISSING**. |
| Does **not** supersede | A41 as the *target* design. A61 as the *target* fluent map. A64 as the *target* host composition. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**Yes. The outbox is a parked table, not a bus.**

`OutboxEvent` + `OutboxEventType` + `TraderDbContext.OutboxEvents` → `outbox_events` exist. Nothing in product C# **inserts** a row. Nothing **claims**, **dispatches**, **acks**, or **metrics** a row. The two `BackgroundService` hosts do not drain `outbox_events`. Reconstruction and scoring run **in-process on the ingest tick**, which is the opposite of §12.

| Slice | Measured | Target | Class |
|---|---|---|---|
| Domain type `OutboxEvent` | 10 properties, `Domain/Entities` | A41/A61 claim/lease/dedupe shape | `EXISTS_NEEDS_REFACTOR` |
| Domain enum `OutboxEventType` | 5 §13 kinds, **int** | kebab-case **text** + Phase-1 ingest strings | `EXISTS_NEEDS_REFACTOR` |
| EF `DbSet` + `ToTable("outbox_events")` | yes | yes | `EXISTS_AND_GOOD` (name only) |
| Fluent map quality | PK + **non-unique** `ProcessedAt` | A61: `jsonb`, named UK, pending partial | `EXISTS_NEEDS_REFACTOR` / **Outbox FAIL** (B19) |
| Same-TX writer (`IOutboxWriter` / `ITransactionalOutbox`) | **0** types, **0** `OutboxEvents.Add` | A41 §13 / A61 §5.3 | `MISSING` |
| Dispatcher / processor (`IOutboxProcessor`, SKIP LOCKED, hosted loop) | **0** | A41 §8, A64 `OutboxProcessorHostedService` | `MISSING` |
| Handlers (`IOutboxHandler` per type) | **0** | A41 §13, A64 §8.8 / §9.10 | `MISSING` |
| `IEventBus` seam (outbox now, broker later) | **0** | §13 last sentence | `MISSING` |
| Kafka / NATS / Rabbit / MassTransit | **0** packages, **0** clients | correctly absent | `EXISTS_AND_GOOD` |
| Migrations `0007` / `outbox_handler_receipts` / `outbox_poison_events` | **0** | A30, A41 §6 | `MISSING` |
| Integration `OutboxProcessingTests` | **0** | §60 / A10 / A90 | `MISSING` |
| `mt5_outbox_backlog` | **not computed**; API hardcodes `0` | §58 / A50 | `UNSAFE` (ops lie) |
| C++ `terminal_*outbox*` in `mt5-sdk` | different tree | do **not** port | `DEPRECATED` for this product |

**One-liner:** a `DbSet` is not a dispatcher. Until a writer enlists in the domain transaction **and** a worker claims `processed_at IS NULL` with `FOR UPDATE SKIP LOCKED`, §12/§13 are unimplemented.

Do **not** treat `EnsureCreated` creating an empty `outbox_events` table, or `/api/health.outboxBacklog = 0`, as evidence that the outbox works.

---

## 1. Method

Read-only. Product trees (`src/`, `apps/`, `tests/`, `mt5-sdk/src/`) were not edited.

| Action | Result |
|---|---|
| Full read of `OutboxEvent.cs`, `OutboxEventType.cs`, `TraderDbContext` outbox block | entity + enum + thin fluent map |
| Full read of `EfTradingStore`, `DealIngestionService`, `ReconstructionScoringService`, `DemoSeeder`, both workers, API `Program.cs`, DI | **zero** outbox produce/consume |
| Grep product `*.cs` for `OutboxEvent`, `OutboxEvents`, `OutboxEventType`, `IOutbox*`, `IEventBus`, `SKIP LOCKED`, `pg_notify`, `TransactionalOutbox` | hits only on entity, enum, `DbSet`, fluent `ToTable` |
| Grep `tests/` for `Outbox` | **0** |
| Grep `*.cs` / `*.csproj` / `*.json` for Kafka / MassTransit / NATS / Rabbit / Service Bus | **0** |
| `Test-Path` `Persistence/Outbox`, `Persistence/Migrations`, `Persistence/Configurations/*` | Outbox dir **false**; Migrations **false**; Configurations **0 files** |
| `Get-ChildItem` `*Outbox*` under `src/`, `apps/`, `tests/` | **only** `OutboxEvent.cs` + `OutboxEventType.cs` |
| Hosted-service census | **2** `AddHostedService<Worker>` (mt5 + fix). No processor. |
| SHA-256 of the files below | computed 2026-08-18 |

Did **not** start hosts, did **not** hit Compose Postgres, did **not** `COUNT(*)` a live `outbox_events` table. Default DI is EF InMemory (`C29`). A live empty table would still prove nothing: there is no writer.

---

## 2. Files hashed (this pass)

Product files only (SHA-256, sizes bytes). Hashes match C07 / C29 for the overlapping hosts.

| Bytes | SHA-256 | Path |
|---:|---|---|
| 546 | `78108643D4C8E25DBEA767C30145366B3337C59D6E39EA3F613B480CDE6649A8` | `D:\Prop\src\Domain\Entities\OutboxEvent.cs` |
| 211 | `163ED842EE9AF0C94EA912A91845F31C8644F2A1A373A67C77E7FA16154BAADA` | `D:\Prop\src\Domain\Enums\OutboxEventType.cs` |
| 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` |
| 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` |
| 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `D:\Prop\apps\api\Program.cs` |
| 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `D:\Prop\apps\mt5-worker\Program.cs` |
| 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | `D:\Prop\apps\mt5-worker\Worker.cs` |
| 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `D:\Prop\apps\fix-worker\Program.cs` |
| 1971 | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | `D:\Prop\apps\fix-worker\Worker.cs` |
| 3119 | `2BB1EE244B3D5412E701A72B815DB39B8996BC83F5747911C17BA497820F2EFD` | `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` |

Absent paths (expected by A30 / A41 / A64; `Test-Path` false or 0 children):

```text
src/Application/Abstractions/IOutboxWriter.cs
src/Application/Abstractions/IOutboxProcessor.cs
src/Application/Abstractions/ITransactionalOutbox.cs
src/Application/Abstractions/IEventBus.cs
src/Infrastructure/Persistence/Outbox/
src/Infrastructure/Persistence/Configurations/OutboxEventConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180007_CheckpointsOutboxEvents.cs
apps/mt5-worker/Hosting/OutboxDispatchJob.cs
apps/*/Hosting/OutboxProcessorHostedService.cs
tests/Integration/Outbox/OutboxProcessingTests.cs
```

---

## 3. Binding law (what “dispatcher” means here)

Architecture §12 live flow (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 553–571):

```text
MT5 event
   ↓
validate
   ↓
deduplicate
   ↓
persist raw record
   ↓
write transactional outbox event
   ↓
commit

Then background workers process the outbox.
This avoids coupling MT5 callbacks directly to ML or execution.
```

Architecture §13 (lines 575–595): use a **PostgreSQL transactional outbox** for five kinds — trade-completed, score-update, shadow-copy-intent, risk-check-request, notification — and only later migrate **behind an event-bus abstraction**. Do not introduce Kafka on day one.

A41 + A64 pin the *implementation* of “background workers process the outbox”:

1. **Produce** — `IOutboxWriter.WriteAsync` on the **same** `DbContext` / Npgsql transaction as the domain write. `ON CONFLICT DO NOTHING` on a unique idempotency/dedupe key.
2. **Claim** — raw SQL `FOR UPDATE SKIP LOCKED` on the pending subset. EF 8 LINQ cannot emit this (A61 §5.4).
3. **Dispatch** — `IOutboxHandler` allow-list per host (`mt5-source` vs `fix-dest`).
4. **Ack** — second transaction (or same handler TX with receipt): `processed_at` / `status='processed'`. Crash between side-effect and ack → at-least-once redelivery. Handlers must be idempotent.
5. **Never** send FIX `NewOrderSingle` from an outbox handler. Live send is a separate `execution_intents` poller (A41 §4, A42).

A “dispatcher” in this report is steps 2–4 hosted as `OutboxProcessorHostedService` / `OutboxDispatchJob`. It is **not** `Database.EnsureCreated`, **not** a `DbSet`, and **not** a hardcoded health field.

---

## 4. What exists (entity + map only)

### 4.1 Domain entity — entire file

```1:16:D:\Prop\src\Domain\Entities\OutboxEvent.cs
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class OutboxEvent
{
    public Guid Id { get; set; }
    public OutboxEventType Type { get; set; }
    public string AggregateId { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public string? CorrelationId { get; set; }
}
```

Location is `Domain/Entities`, not A01’s `Domain/Platform` or A30’s `Domain/Outbox`. That is acceptable if Application never sees EF types (A41 §13). The **shape** is not acceptable as a drain table.

### 4.2 Domain enum — entire file

```1:10:D:\Prop\src\Domain\Enums\OutboxEventType.cs
namespace TraderIntelligence.Domain.Enums;

public enum OutboxEventType
{
    TradeCompleted = 0,
    ScoreUpdate = 1,
    ShadowCopyIntent = 2,
    RiskCheckRequest = 3,
    NotificationEvent = 4
}
```

The five §13 kinds exist as C# identifiers. Wire/SQL names in A41 are kebab-case (`trade-completed`, …). A61 §5.5 wants the column as free **text**, plus Phase-1 ingest strings (`DealPersisted` / `mt5.deal_persisted`). The enum has **no ingest kinds**. EF will persist `Type` as **int** (no `HasConversion`, no `HasColumnType("text")` anywhere under `src/` — grep **0**).

### 4.3 EF mapping — entire outbox block

```22:22:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
```

```108:113:D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs
        modelBuilder.Entity<OutboxEvent>(e =>
        {
            e.ToTable("outbox_events");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProcessedAt);
        });
```

That is the **entire** persistence contract: table name, surrogate PK, one non-unique index on `ProcessedAt`. No `HasDatabaseName`. No unique dedupe. No partial “pending” index. No `jsonb`. No snake_case convention (`UseSnakeCaseNamingConvention` grep: **0**). `Persistence/Configurations/` is empty — no `OutboxEventConfiguration`.

`EnsureCreated` on the three hosts (api, mt5-worker, fix-worker) will create whatever this model is, **if** a real Postgres connection string is injected. Default launch is InMemory (`C29`). InMemory cannot prove unique indexes, `jsonb`, or `SKIP LOCKED`.

### 4.4 Column gap vs A61 §5.1 / A41 §6.2

| Target column / property | Current CLR | Present? |
|---|---|---|
| `id` | `Id` Guid | yes |
| `event_type` text | `Type` **enum → int** | wrong type |
| `payload` jsonb | `PayloadJson` **string**, no column type | wrong type |
| `payload_schema_version` | — | **no** |
| `aggregate_type` | — | **no** |
| `aggregate_id` uuid | `AggregateId` **string** | wrong type |
| `idempotency_key` / `dedupe_key` | — | **no** |
| `broker_id` / `source_login` | — | **no** |
| `correlation_id` uuid | `CorrelationId` **string?** | wrong type |
| `causation_id` | — | **no** |
| `status` (`pending`/`processing`/`processed`/…) | implied only by `ProcessedAt == null` | **no** status machine |
| `attempt_count` | `Attempts` | name-only |
| `max_attempts` | — | **no** |
| `next_attempt_at` / `available_at` | — | **no** |
| `locked_until` / `locked_by` | — | **no** |
| `expires_at` | — | **no** (required for shadow/risk) |
| `created_at` | `OccurredAt` only | mixed clock |
| `processed_at` | `ProcessedAt` | yes |
| `last_error` | `LastError` | yes |
| `copy_intent_id` / `source_trade_id` | — | **no** |
| `outbox_events_dedupe_uk` | — | **no** |
| `outbox_events_dispatcher_ix` (pending partial) | `HasIndex(ProcessedAt)` full | **wrong** |
| `outbox_handler_receipts` | — | **no table** |
| `outbox_poison_events` | — | **no table** |

B19 already scored this row **Outbox FAIL**. C58 confirms the map has not grown since that hash (`AFB195AC…`).

Without a unique `(event_type, idempotency_key)` or `(aggregate_type, aggregate_id, event_type, dedupe_key)`, a writer **cannot** implement §12 idempotent enqueue. Without a pending partial + `available_at`, a poller **cannot** claim safely at ~5k accounts (A98).

---

## 5. Produce path — **MISSING**

### 5.1 No Application port

`src/Application` contains only `Contracts/`, `Dashboard/`, `Ingestion/`. Grep of Application C# for `Outbox`, `IOutbox`, `IEventBus`: **0**.

`ITradingStore` (`DealIngestionService.cs` lines 8–18) can upsert groups/accounts/deals/positions, load deals, replace reconstructed trades, upsert scores. It **cannot** write an outbox row.

`AddTraderIntelligence` (`DependencyInjection.cs`) registers ten services. C05 already listed “outbox dispatcher” as **not registered**. Recheck: still no `IOutboxWriter`, `IOutboxProcessor`, `IOutboxHandler`, `IEventBus`, `IDbContextFactory`.

### 5.2 Store never touches `OutboxEvents`

`EfTradingStore` is the only `ITradingStore` implementation. It references `TraderDbContext` and writes `Mt5Groups`, `Mt5Accounts`, `Mt5Deals`, `Mt5Positions`, `ReconstructedTrades`, `TraderScores`, `TraderScoreHistory`.

Grep of all product `*.cs` for `OutboxEvents` / `new OutboxEvent`:

| File | Hits |
|---|---|
| `TraderDbContext.cs` | `DbSet` + `ToTable` only |
| every other `.cs` | **0** |

There is no `db.OutboxEvents.Add(...)`. Seeder does not seed outbox rows. Dashboard queries never count them.

### 5.3 Each store method commits alone

Every `EfTradingStore` mutating method ends with `await _db.SaveChangesAsync(ct)`:

| Method | Commit |
|---|---|
| `UpsertGroupAsync` | own `SaveChanges` |
| `UpsertAccountAsync` | own `SaveChanges` |
| `UpsertDealAsync` | own `SaveChanges` after insert |
| `ReplacePositionsAsync` | own `SaveChanges` |
| `ReplaceReconstructedAsync` | own `SaveChanges` |
| `UpsertScoreAsync` | own `SaveChanges` (score + history) |

`DealIngestionService.SyncBrokerAsync` loops accounts and calls those methods one by one. Even if someone appended `OutboxEvents.Add` tomorrow **after** `UpsertDealAsync` returned, it would be a **second transaction** — the §12 failure mode A61 §5.3 forbids (“write the outbox on a second connection / second `SaveChanges` after the raw commit”).

`UpsertDealAsync` already returns `false` on `(BrokerId, DealTicket)` duplicate. That is application-level dedupe of the **deal**, not an outbox `ON CONFLICT DO NOTHING`.

### 5.4 Reconstruction / score are in-process, not produced

`ReconstructionScoringService.RebuildTraderAsync` loads deals, reconstructs, `ReplaceReconstructedAsync`, scores, `UpsertScoreAsync`. No `TradeCompleted`. No `ScoreUpdate`. No `NotificationEvent`.

`ShadowCopyEngine` and `RiskEngine` are pure Domain calculators. They are **not** registered in DI (C05). Nobody persists `CopyIntent` / `RiskDecisionRecord` / `ExecutionIntent` / `ShadowOrder` except empty `DbSet`s. Those tables cannot be reached from an outbox that is never written, and they are not written another way either.

---

## 6. Consume path — **MISSING** (the dispatcher)

### 6.1 Hosted services that exist

Product `AddHostedService` / `BackgroundService` census (all `*.cs`):

| Host | Registration | Loop |
|---|---|---|
| `apps/mt5-worker` | `AddHostedService<Worker>()` | 30 s Fake ingest + `RebuildTraderAsync` for four fixture logins |
| `apps/fix-worker` | `AddHostedService<Worker>()` | 15 s stamp of `fix_sessions.LastInboundAt` / status |
| `apps/api` | **none** | ASP.NET endpoints only (correct: A41 says **do not** run the processor in the API) |

A64 required `OutboxProcessorHostedService` on **both** workers (`consumer_name=mt5-source` and `fix-dest`). B07 scored those jobs **MISSING**. Recheck 2026-08-18: still **MISSING**. Worker hashes unchanged from C07.

### 6.2 mt5-worker is the anti-pattern §12 exists to prevent

```17:44:D:\Prop\apps\mt5-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            // SyncBrokerAsync(Achiever) + SyncBrokerAsync(StarwaveFx)
            // RebuildTraderAsync for logins {10001,10002,10003,99001}
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
```

Call graph (no outbox at any hop):

```text
Worker.ExecuteAsync
  → DealIngestionService.SyncBrokerAsync
       → FakeMt5BrokerConnector.Get*
       → ITradingStore.SaveChanges per row
  → ReconstructionScoringService.RebuildTraderAsync
       → LoadDeals → TradeReconstructor → ReplaceReconstructed
       → BaselineScorer → UpsertScore + history
```

A59 / A64 law: the **only** path into reconstruction / scoring / shadow / copy-intent is `outbox_events`. Current worker **inlines** Phase 2/3 on the same timer as ingest (B07 §6.2). Crash mid-`RebuildTraderAsync` after deals committed and before scores committed → no outbox to retry; next tick rebuilds from scratch (replace-all reconstructed rows). That is a demo loop, not at-least-once delivery.

### 6.3 fix-worker does not claim `risk-check-request`

`fix-worker/Worker.cs` reads `FixSessionStates` and writes heartbeat timestamps. It never queries `OutboxEvents`, `CopyIntents`, `RiskDecisions`, or `ExecutionIntents`. A41 §12: fix-worker `ClaimTypes = [ "risk-check-request" ]`. Not implemented.

FIX send remains **off by absence** (C07). That is still correct. The missing dispatcher is **not** an excuse to hang `35=D` off a future outbox retry.

### 6.4 No claim SQL, no lease, no notify

Grep of product `*.cs` / `*.sql` (there is no `Persistence/Sql/`):

| Token | Hits |
|---|---|
| `FOR UPDATE` | **0** |
| `SKIP LOCKED` | **0** |
| `pg_notify` / `LISTEN` | **0** |
| `ExecuteUpdateAsync` on outbox | **0** |
| `locked_until` / `LockedUntil` | **0** |
| `FromSql` / `SqlQuery` on outbox | **0** |

There is nothing to call a dispatcher. A future LINQ `Where(x => x.ProcessedAt == null)` would be **wrong** even if written: A61 forbids it; two workers would race without `SKIP LOCKED`.

---

## 7. Health / metrics lie

`GET /api/health` (`apps/api/Program.cs` lines 26–33):

```csharp
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo connector" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = true, lastCheck = DateTimeOffset.UtcNow } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
```

`outboxBacklog` is a **literal `0`**. It does not query `OutboxEvents.Count(e => e.ProcessedAt == null)`. The React type `HealthStatus.outboxBacklog` (`apps/web/src/types/index.ts`) will display that zero.

`EfDashboardQueries.GetOverviewAsync` does not read outbox at all.

Architecture §58 / A50 name `mt5_outbox_backlog` (gauge, `{event}` / `broker`). No OpenTelemetry meter, no Prometheus name, no worker exporter. A hardcoded zero is **`UNSAFE`** for ops: a real backlog (if a writer were added first) would stay invisible.

---

## 8. Tests — **MISSING**

`tests/` grep `Outbox`: **0**.

`SeedingAndStoreTests` asserts brokers, groups, deals, reconstructed XAU trades, scores, FIX `cServer`. It never asserts `db.OutboxEvents`. After a successful seed, outbox count is **0** (no writer). The test would still pass if the table were dropped from the model.

A10 / A27 / A90 required at least:

| Class | What it must prove | Status |
|---|---|---|
| `OutboxAtomicCommitTests` | deal + outbox same commit; rollback drops both | **MISSING** |
| `OutboxIdempotencyPersistenceTests` | unique key + `ON CONFLICT DO NOTHING` | **MISSING** |
| `OutboxProcessingTests` | processor marks processed exactly once | **MISSING** |
| `OutboxSkipLockedConcurrencyTests` | two pollers, one row, one claim | **MISSING** |
| `OutboxProcessAfterCrashTests` | commit then crash before ack → redelivery | **MISSING** |
| `OutboxDoesNotCallFixFromCallbackTests` | ingest path has zero `NewOrderSingle` | **MISSING** (vacuously true today) |

InMemory cannot host the SKIP LOCKED / unique-violation cases (A41 §17.2, A90). Those tests need Testcontainers Postgres. That stack is also **MISSING** (`C29`).

---

## 9. Kafka / C++ outbox — do not confuse with this gap

### 9.1 Kafka is correctly absent

No `Confluent.Kafka`, MassTransit, NATS, RabbitMQ, or Azure Service Bus package or client in product `*.cs` / `*.csproj` / `*.json`. §13 / §71 / A80: **do not** fill the dispatcher hole with a broker. `IEventBus` v1, when written, **is** `IOutboxWriter`.

### 9.2 `mt5-sdk` “fast outbox” is a different product

`D:\Prop\mt5-sdk\src\services\metrics_service.h` exports `terminal_fast_outbox_*` / `terminal_pg_outbox_frames_total`. A41 §2: Redis-first terminal outbox is **UNSAFE** as trading SoT and must **not** be ported into `src/Infrastructure` as the §13 bus. C58 does not treat those counters as a dispatcher for `outbox_events`.

---

## 10. Adjacent parked entities (same failure mode)

These `DbSet`s are mapped and almost unused — same “type exists, no worker” pattern. They are **not** substitutes for a dispatcher.

| Entity / table | Written by product C#? | Consumed? |
|---|---|---|
| `SyncCheckpoint` / `sync_checkpoints` | **no** | **no** (ingest uses host-clock 30 d window) |
| `CopyIntent` / `copy_intents` | **no** | **no** |
| `RiskDecisionRecord` / `risk_decisions` | **no** (dashboard counts 0 rejects) | read-only empty |
| `ExecutionIntent` / `execution_intents` | **no** | **no** (this is the *other* poller, not outbox) |
| `ShadowOrder` / `shadow_orders` | **no** | overview sums slippage → `0` |
| `AuditLog` / `audit_logs` | **no** | **no** |

`SyncCheckpoint` is the ingest cursor. Without it **and** without outbox, restart-safe §12 backfill is also missing (A59). Do not implement a dispatcher that assumes checkpoints exist.

---

## 11. Stale reports (keep on disk)

| Report | Stale sentence | C58 replacement |
|---|---|---|
| A01, A03, A41 §3, `_index_extract.tsv` | `outbox_events` **MISSING**; 0 `DbSet` | Entity + `DbSet` + `ToTable` **exist** (thin) |
| A02 / A41 “workers are 1 s template loops” | template `Task.Delay(1000)` | C07/B07: 30 s Fake ingest / 15 s session stamp. Still **no** processor. |
| A41 “Infrastructure has no DbContext” | — | `TraderDbContext` exists (C06/C29) |
| B21 | DbSet types exist in Domain | still true for `OutboxEvent` |

A41 **design** (schema, claim SQL, ports, non-goals) remains binding. Only its **measured current state** table is stale.

---

## 12. Scoreboard

| Capability | Score | Notes |
|---|---|---|
| Entity parked | 1 / 1 | `OutboxEvent` |
| §13 enum vocabulary | 5 / 5 names | wrong storage (int); no ingest kinds |
| EF table name | 1 / 1 | `outbox_events` |
| A61 columns | ~6 / 20+ | see §4.4 |
| A61 indexes | 0 / 3 named | unnamed `ProcessedAt` only |
| Writer | 0 / 1 | |
| Dispatcher / hosted processor | 0 / 2 hosts | mt5-source + fix-dest |
| Handlers | 0 / 5+ | |
| Receipts + poison table | 0 / 2 | |
| Migration `0007` | 0 / 1 | C29: 0 / 15 overall |
| Integration proofs | 0 / 6 listed in §8 | |
| Honest backlog metric | 0 / 1 | hardcoded `0` |
| Kafka correctly not added | PASS | keep it that way |

**Dispatcher: FAIL. Produce: FAIL. Entity: EXISTS_NEEDS_REFACTOR.**

---

## 13. What not to do

1. **Do not** add Kafka / Redis streams / SignalR-as-bus to “have a dispatcher.” SignalR is a dashboard fan-out (C28: package present, no hub). Redis is not SoT (A03, A99).
2. **Do not** send `NewOrderSingle` from any outbox handler. Persist `execution_intents` (`not_sent`); FIX poller is separate (A41, A42, C07).
3. **Do not** call `RebuildTraderAsync` from the MT5 callback or from the current 30 s ingest tick once a writer exists. Ingest TX = raw + optional `DealPersisted` only (A41 §14.1).
4. **Do not** treat `ProcessedAt == null` LINQ as a claim. Use `FOR UPDATE SKIP LOCKED` on real Postgres (A61 §5.4).
5. **Do not** port `mt5-sdk` `terminal_fast_outbox_*`.
6. **Do not** overload `outbox_events` as `audit_logs` / `ingestion_events` / `system_events` (A41 §2).
7. **Do not** enqueue `shadow-copy-intent` / `risk-check-request` on historical backfill (A41, §63).
8. **Do not** keep `/api/health.outboxBacklog = 0` after a writer exists. Query pending rows or omit the field until the metric exists.
9. **Do not** implement the dispatcher in `apps/api`.
10. **Do not** hand-write MQ5 or mutate product source from this report.

---

## 14. When implementation is authorized (files, not this agent)

Pin to A30 + A41 + A61. Order:

1. Widen `OutboxEvent` (or Infrastructure `OutboxEventRecord`) to A61 §5.1. Store `event_type` as **text**. Add `outbox_events_dedupe_uk` + pending `outbox_events_dispatcher_ix`. Versioned EF migration `0007` — **not** another `EnsureCreated` drift (C29).
2. Application ports: `IOutboxWriter`, `IOutboxProcessor`, `IOutboxHandler`, `IEventBus` = writer. Register in `AddTraderIntelligence`.
3. `EfTradingStore` / a dedicated raw writer: **one** `BeginTransaction` for raw row + outbox insert. Stop per-method `SaveChanges` on the live ingest path.
4. `PostgresOutboxProcessor` + `OutboxProcessorHostedService` on mt5-worker (`mt5-source` allow-list) and fix-worker (`risk-check-request` only).
5. Phase-1 handlers may no-op + mark processed. Move `ReconstructionScoringService` **off** the ingest timer onto `TradeCompleted` / `DealPersisted`.
6. Testcontainers: atomic commit, unique conflict, SKIP LOCKED, crash-before-ack. InMemory is not a proof.
7. `mt5_outbox_backlog` from `COUNT(*) FILTER (WHERE processed_at IS NULL)` (or `status='pending'` once that column exists). Replace the health literal.

Until those land, the honest status remains: **outbox entity exists; no dispatcher.**
