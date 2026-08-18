# A30 — Implementation sequence (§73.C)

**Date:** 2026-08-18  
**Agent:** A30  
**Architecture:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`  
**Sections read:** §66 Suggested Repository Structure, §67 Engineering Phases, §69 First Useful Version, §73.C Implementation sequence; supporting §§5, 6, 11–18, 22–24, 30–31, 36–41, 44–56, 60–65, 71–72.  
**Peer reports used (do not contradict):** A02 (Application ports), A09 (unit-test class names), A11 (solution coverage), A12 (`IMT5Client` map), A15 (pool/watchdog), A16 (`/mt5/*` HTTP contract), A28 (phase/gate checklist).  
**Product source modified:** no.

This file is the §73.C artifact: the **exact files, modules, and migrations** to create, **in order**, for the **first useful version** (§69). It is incremental. It does **not** introduce Kafka, Kubernetes, ClickHouse, an LLM/AI API, deep learning, RL, a microservice mesh, or `services/ml-service`.

---

## 0. What “first useful version” is (and is not)

§69 (12 items). ML is **not** required. Live `NewOrderSingle` is **not** required.

| # | §69 item | Delivering increment | §67 phase |
|---|---------|----------------------|-----------|
| 1 | Connect to both MT5 brokers | I2 | 1 |
| 2 | Discover all groups | I2 | 1 |
| 3 | Synchronize ~5,000 accounts | I2–I3 | 1 |
| 4 | Capture XAUUSD trades correctly | I3 | 1 |
| 5 | Reconstruct logical trades | I4 | 2 |
| 6 | Detect first 3 completed XAUUSD trades | I4 | 2 |
| 7 | Deterministic trader/risk score | I5 | 3 |
| 8 | Rank traders | I5 | 3 |
| 9 | Connect to cTrader QUOTE FIX securely | I7 | 4 |
| 10 | Discover Pepperstone XAUUSD instrument ID | I7 | 4 |
| 11 | Shadow-copy selected traders using destination quotes | I8 | 5 |
| 12 | Show all of this in React | I6 + I9 | 3–5 |

**Stop after I9.** Do not start Phase 6 (ML), Phase 7 (TRADE session / `OrderMassStatusRequest` / `RequestForPositions`), or Phase 8 (live send) in this sequence.

**Do not create in this sequence**

```text
Kafka / MassTransit / NATS / RabbitMQ
Kubernetes manifests / Helm / operators
ClickHouse
services/ml-service/**
src/Execution/**                  (live ExecutionIntent + NewOrderSingle)
apps/fix-worker TRADE send path
model_versions / model_predictions / model_evaluations tables
fix_orders / fix_execution_reports / destination_positions tables
LLM / OpenAI / Semantic Kernel packages
```

**Reuse, do not duplicate**

- Keep the ten existing `.csproj` files in `Mt5TraderIntelligence.sln` (A11).
- Reuse `D:\Prop\mt5-sdk` (`IMT5Client` / `MT5Manager` / `MT5Pool` / `MT5Watchdog`). Do **not** rewrite the Manager API in C#.
- Reuse the existing REST/SSE path literals inventoried in A16. Do **not** invent a second MT5 HTTP dialect.
- Do **not** reuse `mt5_ledger_store` / YoPips `challenge_id` / `user_id` columns. This product’s ledger is the §45 EF schema below.
- Do **not** add empty §66 sibling projects “to match the diagram” (A02, A11). Create `TradeReconstruction`, `Scoring`, `Shadow`, `Risk` only in the increment that adds their first real type.
- Delete scaffold leftovers (`Class1.cs`, `/weatherforecast`, `UnitTest1.cs`) in the increment that replaces them. Do not rename `Class1` into a domain type.

---

## 1. Current baseline (honest)

Measured on 2026-08-18. Behavioral coverage ≈ 0.

| Path | State |
|------|--------|
| `src/Domain/Class1.cs` | empty template |
| `src/Application/Class1.cs` | empty template; FluentValidation unused (A02) |
| `src/Infrastructure/Class1.cs` | empty; EF Core 8 + Npgsql + Redis already referenced |
| `src/Mt5/Class1.cs` | empty |
| `src/Fix.CTrader/Class1.cs` | empty |
| `apps/api/Program.cs` | stock `/weatherforecast` |
| `apps/mt5-worker/Worker.cs` | template 1 Hz log loop |
| `apps/fix-worker/Worker.cs` | template 1 Hz log loop |
| `tests/Unit/UnitTest1.cs` | empty `[Fact]` |
| `tests/Integration/UnitTest1.cs` | empty `[Fact]` |
| `apps/web` | **MISSING** |
| `src/TradeReconstruction`, `Scoring`, `Shadow`, `Risk`, `Execution` | **MISSING** |
| EF migrations / SQL schema | **MISSING** |
| `Directory.Build.props` | **MISSING** |
| `docker-compose.yml` | **MISSING** |
| `mt5-sdk` | real C++ Manager client; **no HTTP server** in this repo (A16 is a *client* of a YoPips-era service that is not here) |

Process topology for v1 (plain processes + one compose file for data stores — **not** K8s):

```text
Windows host
  apps/mt5-collector   :9101   Achiever   (native Manager API, read-only)
  apps/mt5-collector   :9102   StarwaveFX (native Manager API, read-only)
  apps/mt5-worker              C# ingest / backfill / outbox / reconstruct / score
  apps/fix-worker              C# QUOTE FIX + quote persist + shadow
  apps/api                     ASP.NET Core + SignalR
  apps/web                     Vite React (dev server)

docker compose (Linux or Windows Docker)
  postgres:16
  redis:7
```

Outbox = PostgreSQL table + worker poll. **No Kafka.**

---

## 2. Adaptation of §66 to this repo

Create only these **new** product modules during v1, and only when the listed increment lands the first type:

| New module | First increment | Project file |
|------------|-----------------|--------------|
| `apps/mt5-collector` | I2 | `apps/mt5-collector/CMakeLists.txt` (C++20, links `mt5sdk::mt5sdk`) |
| `apps/web` | I6 | `apps/web/package.json` (Vite + React + TS) |
| `src/TradeReconstruction` | I4 | `src/TradeReconstruction/TraderIntelligence.TradeReconstruction.csproj` |
| `src/Scoring` | I5 | `src/Scoring/TraderIntelligence.Scoring.csproj` |
| `src/Shadow` | I8 | `src/Shadow/TraderIntelligence.Shadow.csproj` |
| `src/Risk` | I8 | `src/Risk/TraderIntelligence.Risk.csproj` (shadow-safety subset only) |
| `tests/Replay` | I4 | `tests/Replay/TraderIntelligence.Tests.Replay.csproj` |

Leave `src/Execution`, `services/ml-service`, `tests/Fix` (as a separate project), `tests/Risk` (as a separate project) **uncreated**. Their tests live under existing `tests/Unit` folders (`Risk/`, `Fix/`) until Phase 7–8.

Solution edits happen in the same increment as the new `.csproj`. Nest new C# projects under the existing `src` / `apps` / `tests` solution folders.

---

## 3. Migration catalog (create in this order, never squash)

All migrations live under:

```text
src/Infrastructure/Persistence/Migrations/
```

EF Core is the runner (`dotnet ef` against `TraderIntelligence.Infrastructure`, startup project `apps/api`). Each increment that changes schema **adds** a migration; it does not edit a previously applied one.

Companion reviewed SQL (generated, committed, not hand-applied in prod):

```text
src/Infrastructure/Persistence/Sql/<same_name>.sql
```

| Id | File (EF) | Tables created | Increment |
|----|-----------|----------------|-----------|
| 0001 | `202608180001_ExtensionsAndSystem.cs` | `pgcrypto`; `schema_meta` | I1 |
| 0002 | `202608180002_BrokersAndConnections.cs` | `brokers`, `broker_connections` | I1 |
| 0003 | `202608180003_GroupsAndPlanMappings.cs` | `mt5_groups`, `plan_group_mappings` | I1 |
| 0004 | `202608180004_AccountsAndSnapshots.cs` | `mt5_accounts`, `mt5_account_snapshots` | I1 |
| 0005 | `202608180005_SymbolsAndSourceTicks.cs` | `mt5_symbols`, `mt5_xau_ticks` | I1 |
| 0006 | `202608180006_OrdersDealsPositions.cs` | `mt5_orders`, `mt5_deals`, `mt5_positions_current` | I1 |
| 0007 | `202608180007_CheckpointsOutboxEvents.cs` | `sync_checkpoints`, `outbox_events`, `ingestion_events`, `system_events`, `audit_logs` | I1 |
| 0008 | `202608180008_CanonicalSymbols.cs` | `canonical_instruments`, `source_symbol_mappings` | I4 |
| 0009 | `202608180009_ReconstructedTrades.cs` | `reconstructed_trades` | I4 |
| 0010 | `202608180010_TraderScoresAndFlags.cs` | `trader_feature_snapshots`, `trader_scores`, `trader_score_history`, `trader_states`, `trader_risk_flags` | I5 |
| 0011 | `202608180011_VenuesAndDestinationQuotes.cs` | `execution_venues`, `destination_symbols`, `destination_quotes` | I7 |
| 0012 | `202608180012_FixQuoteSessions.cs` | `fix_sessions`, `fix_session_events` | I7 |
| 0013 | `202608180013_CopyIntents.cs` | `copy_intents`, `copy_allocations` | I8 |
| 0014 | `202608180014_ShadowBook.cs` | `shadow_orders`, `shadow_fills`, `shadow_positions`, `shadow_performance` | I8 |
| 0015 | `202608180015_ShadowRiskDecisions.cs` | `risk_decisions`, `risk_events` | I8 |

**Not in v1 (do not create these migrations yet)**

```text
model_versions, model_predictions, model_evaluations
fix_orders, fix_execution_reports, destination_positions
execution_intents
source_destination_links          (live mapping; shadow uses copy_intents)
execution_reconciliation_runs / execution_reconciliation_issues
```

Identity rule for every source table (§10): compound key includes `broker_id`. Never treat `login` / `deal_ticket` / `order_ticket` / `position_id` as globally unique.

Required unique indexes (create with the owning migration):

```text
0002  brokers.code
0003  mt5_groups (broker_id, group_name)
0004  mt5_accounts (broker_id, login)
0006  mt5_deals (broker_id, deal_ticket)
0006  mt5_orders (broker_id, order_ticket)
0006  mt5_positions_current (broker_id, position_ticket)
0007  outbox_events (id); index (status, available_at)
0007  sync_checkpoints (broker_id, login, stream)
0008  source_symbol_mappings (broker_id, source_symbol)
0009  reconstructed_trades (broker_id, login, position_id, lifecycle_seq)
0010  trader_states (broker_id, login)
0012  fix_sessions (venue_id, session_qualifier)   -- QUOTE only in v1
0013  copy_intents (source_broker_id, source_event_id)
```

---

## 4. Increment 0 — Repo hygiene (Phase 0 closeout, not feature work)

No trading behavior. Allowed before Phase 1 because it does not change production semantics (there are none yet).

### Create

| Path | Why |
|------|-----|
| `Directory.Build.props` | `net8.0`, nullable, treat warnings as errors on product projects |
| `.gitignore` | `bin/`, `obj/`, `.env`, `*.user`, FIX store files, user-secrets, `apps/web/node_modules` |
| `.env.example` | placeholders only; copy of architecture §56 with `<SECRET>` sentinels |
| `docker-compose.yml` | `postgres:16` + `redis:7` only. No app containers, no K8s. |
| `docs/README.md` | index pointing at architecture v2; do not rewrite v2 |

### Change

| Path | Change |
|------|--------|
| `apps/api/appsettings.json` | add `ConnectionStrings:Postgres`, `Redis`, empty `Brokers` / `Fix` sections; **no passwords** |
| `apps/api/appsettings.Development.json` | local compose hostnames |
| `apps/mt5-worker/appsettings.json` | same connection + broker registry (non-secret) |
| `apps/fix-worker/appsettings.json` | FIX QUOTE non-secret host/port/comp ids; `TradeSessionEnabled=false`; `RealCopyExecutionEnabled=false` |

### Do not

- Commit `.env`, manager passwords, FIX password, proxy password.
- Add Kafka/K8s files.
- Add empty `src/Execution` or `services/ml-service`.

**Exit:** `docker compose up -d` brings Postgres + Redis; `dotnet build Mt5TraderIntelligence.sln` still green.

---

## 5. Increment 1 — Persistence foundation

Unlocks every later increment. No broker sockets yet.

### Create — Domain (`src/Domain/`, delete `Class1.cs`)

```text
src/Domain/Common/Entity.cs
src/Domain/Common/BrokerId.cs
src/Domain/Common/CanonicalSymbol.cs
src/Domain/Brokers/Broker.cs
src/Domain/Brokers/BrokerConnection.cs
src/Domain/Brokers/BrokerCode.cs                 # Achiever, StarwaveFX
src/Domain/Mt5/Mt5Group.cs
src/Domain/Mt5/PlanGroupMapping.cs
src/Domain/Mt5/Mt5Account.cs
src/Domain/Mt5/Mt5AccountSnapshot.cs
src/Domain/Mt5/Mt5Symbol.cs
src/Domain/Mt5/Mt5Deal.cs
src/Domain/Mt5/Mt5Order.cs
src/Domain/Mt5/Mt5Position.cs
src/Domain/Mt5/Mt5XauTick.cs
src/Domain/Mt5/Mt5Event.cs
src/Domain/Ingestion/SyncCheckpoint.cs
src/Domain/Ingestion/IngestionEvent.cs
src/Domain/Outbox/OutboxEvent.cs
src/Domain/Outbox/OutboxEventTypes.cs            # TradeCompleted, ScoreUpdateRequested, ShadowCopyRequested, RiskCheckRequested, Notification
src/Domain/Audit/AuditLog.cs
src/Domain/Audit/SystemEvent.cs
```

`CanonicalInstrument` / `ReconstructedTrade` / score / FIX / shadow types wait for I4+.

### Create — Application ports (`src/Application/`, delete `Class1.cs`) — A02 B1–B3, O1–O9 only

```text
src/Application/Abstractions/IMt5BrokerConnector.cs
src/Application/Abstractions/IMt5BrokerRegistry.cs
src/Application/Abstractions/ISyncCheckpointStore.cs
src/Application/Abstractions/IRawMt5RecordWriter.cs
src/Application/Abstractions/ITransactionalOutbox.cs
src/Application/Abstractions/IOutboxWriter.cs
src/Application/Abstractions/IOutboxProcessor.cs
src/Application/Abstractions/IEventBus.cs          # implemented by outbox; Kafka later only behind this
src/Application/Abstractions/IClock.cs
src/Application/Abstractions/IUnitOfWork.cs
src/Application/Ingestion/IHistoricalBackfillService.cs
src/Application/Ingestion/ILiveIngestionService.cs
src/Application/Ingestion/IIngestionReconciliationService.cs
src/Application/Ingestion/DealDeduplicator.cs      # pure function; SUT for A09 #1
src/Application/Ingestion/Validators/Mt5DealValidator.cs
src/Application/Ingestion/Validators/Mt5OrderValidator.cs
src/Application/Ingestion/Validators/Mt5AccountValidator.cs
src/Application/DependencyInjection.cs
```

`IMt5BrokerConnector` starts from architecture §6 and is adjusted to the collector (A12):

```csharp
Task ConnectAsync(CancellationToken ct);
Task DisconnectAsync(CancellationToken ct);
Task<bool> IsConnectedAsync(CancellationToken ct);
Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(CancellationToken ct);
Task<IReadOnlyList<ulong>> GetGroupLoginsAsync(string groupName, CancellationToken ct);
Task<Mt5Account?> GetAccountAsync(ulong login, CancellationToken ct);
Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(ulong login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
Task<IReadOnlyList<Mt5Order>> GetOrdersAsync(ulong login, CancellationToken ct);
Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(ulong login, CancellationToken ct);
Task<IReadOnlyList<Mt5Symbol>> GetSymbolsAsync(CancellationToken ct);
IAsyncEnumerable<Mt5Event> SubscribeAsync(CancellationToken ct);
```

No `CreateUser`, no `DealerSendOrder`, no `SendTrade` on this port. This product’s source side is **read-only**.

### Create — Infrastructure (`src/Infrastructure/`, delete `Class1.cs`)

```text
src/Infrastructure/Persistence/TradingDbContext.cs
src/Infrastructure/Persistence/TradingDbContextFactory.cs
src/Infrastructure/Persistence/Configurations/BrokerConfiguration.cs
src/Infrastructure/Persistence/Configurations/BrokerConnectionConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5GroupConfiguration.cs
src/Infrastructure/Persistence/Configurations/PlanGroupMappingConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5AccountConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5AccountSnapshotConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5SymbolConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5DealConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5OrderConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5PositionConfiguration.cs
src/Infrastructure/Persistence/Configurations/Mt5XauTickConfiguration.cs
src/Infrastructure/Persistence/Configurations/SyncCheckpointConfiguration.cs
src/Infrastructure/Persistence/Configurations/OutboxEventConfiguration.cs
src/Infrastructure/Persistence/Configurations/IngestionEventConfiguration.cs
src/Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs
src/Infrastructure/Persistence/Configurations/SystemEventConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180001_ExtensionsAndSystem.cs
src/Infrastructure/Persistence/Migrations/202608180002_BrokersAndConnections.cs
src/Infrastructure/Persistence/Migrations/202608180003_GroupsAndPlanMappings.cs
src/Infrastructure/Persistence/Migrations/202608180004_AccountsAndSnapshots.cs
src/Infrastructure/Persistence/Migrations/202608180005_SymbolsAndSourceTicks.cs
src/Infrastructure/Persistence/Migrations/202608180006_OrdersDealsPositions.cs
src/Infrastructure/Persistence/Migrations/202608180007_CheckpointsOutboxEvents.cs
src/Infrastructure/Persistence/Sql/202608180001_ExtensionsAndSystem.sql
src/Infrastructure/Persistence/Sql/202608180002_BrokersAndConnections.sql
src/Infrastructure/Persistence/Sql/202608180003_GroupsAndPlanMappings.sql
src/Infrastructure/Persistence/Sql/202608180004_AccountsAndSnapshots.sql
src/Infrastructure/Persistence/Sql/202608180005_SymbolsAndSourceTicks.sql
src/Infrastructure/Persistence/Sql/202608180006_OrdersDealsPositions.sql
src/Infrastructure/Persistence/Sql/202608180007_CheckpointsOutboxEvents.sql
src/Infrastructure/Persistence/Repositories/SyncCheckpointStore.cs
src/Infrastructure/Persistence/Repositories/RawMt5RecordWriter.cs
src/Infrastructure/Persistence/Outbox/PostgresOutboxWriter.cs
src/Infrastructure/Persistence/Outbox/PostgresOutboxProcessor.cs
src/Infrastructure/Persistence/Outbox/OutboxEventBus.cs
src/Infrastructure/Time/SystemClock.cs
src/Infrastructure/Redis/RedisConnection.cs          # cache/locks only; not source of truth
src/Infrastructure/DependencyInjection.cs
src/Infrastructure/Seeding/BrokerSeed.cs             # Achiever + StarwaveFX rows; no secrets
src/Infrastructure/Seeding/PlanGroupMappingSeed.cs   # §9 env mappings; discovery is NOT limited to these
```

### Change

| Path | Change |
|------|--------|
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | add `Microsoft.EntityFrameworkCore` 8.0.4 (runtime; Design already present) |
| `apps/api/Program.cs` | register DbContext; `MapGet("/health")`; **delete** weather forecast |
| `apps/api/TraderIntelligence.Api.http` | replace weather with `/health` |
| `apps/api/TraderIntelligence.Api.csproj` | add `Microsoft.EntityFrameworkCore.Design` (startup for `dotnet ef`) |

### Tests (I1)

```text
tests/Unit/Ingestion/Mt5DealDeduplicationTests.cs     # A09 #1 — first real unit file; delete UnitTest1.cs
tests/Integration/Persistence/MigrationTests.cs       # apply 0001–0007 against Testcontainers Postgres
tests/Integration/Persistence/OutboxCommitTests.cs    # raw deal + outbox in one transaction
```

Change `tests/Integration/TraderIntelligence.Tests.Integration.csproj`: add `Testcontainers.PostgreSQL`. Delete `tests/Integration/UnitTest1.cs`.

**Exit:** migrations apply on empty Postgres; unique index rejects a duplicate `(broker_id, deal_ticket)`; `DealDeduplicator` tests pass.

---

## 6. Increment 2 — Both brokers, all groups, ~5k accounts (§69.1–3)

### New module: read-only collector (reuse `mt5-sdk`)

Do **not** call YoPips. Do **not** implement write/dealer routes. Implement the **read + events** subset of the A16 contract, plus the two gaps A16 called out (`GetGroupDetails`, `GetOrders`).

```text
apps/mt5-collector/CMakeLists.txt
apps/mt5-collector/src/main.cpp
apps/mt5-collector/src/http_server.cpp
apps/mt5-collector/src/http_server.h
apps/mt5-collector/src/routes.cpp
apps/mt5-collector/src/routes.h
apps/mt5-collector/src/sse_hub.cpp
apps/mt5-collector/src/sse_hub.h
apps/mt5-collector/src/read_api.cpp                 # wraps IMT5Client; no SendTrade
apps/mt5-collector/src/read_api.h
apps/mt5-collector/src/auth.cpp                     # X-API-Key only
apps/mt5-collector/src/auth.h
apps/mt5-collector/.env.example
```

Routes to implement (literals must match A16 where they already exist):

```text
GET /mt5/health
GET /mt5/groups
GET /mt5/groups/count
GET /mt5/groups/details                  # NEW — GetGroupDetails (A16: missing)
GET /mt5/groups/{name}/logins
GET /mt5/users/{login}
GET /mt5/users/logins?group=
GET /mt5/accounts/{login}
GET /mt5/accounts/{login}/positions
GET /mt5/accounts/{login}/orders         # NEW — GetOrders (A16: missing)
GET /mt5/accounts/{login}/deals?from=&to=  (+ cursor pagination per A16 §2.1)
GET /mt5/symbols/count
GET /mt5/symbols/{pos}
GET /mt5/symbols/name/{name}
GET /mt5/symbols/{symbol}/tick
GET /mt5/server/time
GET /mt5/events/stream                   # SSE from IMT5Client::GetEventQueue
```

**Refuse** (404/405, never implement in this product):

```text
POST /mt5/users
DELETE /mt5/users/{login}
PUT  /mt5/users/{login}/password
POST /mt5/users/{login}/check-password
POST /mt5/accounts/{login}/balance|deposit|withdraw
POST /mt5/dealer/order
```

Two OS processes (or one binary, two `--broker` configs). Wiring:

- Pump `MT5Manager` + `MT5Watchdog` for SSE (A15).
- `MT5Pool` sized from `MT5_POOL_SIZE` / `MT5_STARWAVEFX_POOL_SIZE` (8 / 4) for request fan-out. **Pass `poolSize` into `MT5Pool::Initialize`** — A15 notes this is currently unwired.
- Achiever proxy from §56 (`ACHIEVER_PROXY_*`) via existing `ProxyConfig`.
- Group discovery = `GetAllGroups` / `GetGroupDetails`. Plan mappings are stored but **must not filter** which groups are fetched (§9).

### Create — C# adapter (`src/Mt5/`, delete `Class1.cs`)

```text
src/Mt5/Http/Mt5CollectorClient.cs              # implements IMt5BrokerConnector
src/Mt5/Http/Mt5CollectorOptions.cs
src/Mt5/Http/Mt5Json.cs                         # maps A16 JSON shapes
src/Mt5/Http/Mt5SseReader.cs
src/Mt5/Registry/Mt5BrokerRegistry.cs
src/Mt5/Registry/BrokerOptions.cs               # Achiever + StarwaveFX from config
src/Mt5/DependencyInjection.cs
```

### Create — Application use-cases

```text
src/Application/Brokers/DiscoverGroupsService.cs
src/Application/Brokers/SynchronizeAccountsService.cs
src/Application/Brokers/BrokerHealthService.cs
```

### Create — Worker host

```text
apps/mt5-worker/Hosting/Worker.cs               # replace template loop
apps/mt5-worker/Hosting/GroupDiscoveryJob.cs
apps/mt5-worker/Hosting/AccountSyncJob.cs
apps/mt5-worker/Hosting/ConnectionSupervisor.cs
apps/mt5-worker/Hosting/Metrics.cs              # mt5_connected, mt5_reconnects (in-process counters)
```

### Change

| Path | Change |
|------|--------|
| `src/Mt5/TraderIntelligence.Mt5.csproj` | `Microsoft.Extensions.Http`, `System.Net.Http.Json` |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | Serilog.Extensions.Hosting |
| `apps/mt5-worker/Program.cs` | DI: Infrastructure + Mt5 + Application jobs |
| `apps/mt5-worker/appsettings.json` | `Brokers:Achiever:CollectorBaseUrl=http://127.0.0.1:9101`, StarwaveFX `:9102` |

### Tests (I2)

```text
tests/Unit/Mt5/Mt5CollectorClientMappingTests.cs
tests/Integration/Mt5/GroupDiscoveryIdempotencyTests.cs
tests/Integration/Mt5/AccountSyncCheckpointTests.cs
```

Live operator probe (optional, not CI): `apps/mt5-collector` against demo only, never commit output with secrets.

**Exit:** both collectors `GET /mt5/health` → `connected=true`; `mt5_groups` contains **all** manager groups (not the §9 subset); `mt5_accounts` approaches the ~5,000-login census with restart-safe checkpoints; dashboard data is not required yet.

---

## 7. Increment 3 — Raw XAUUSD capture, live + backfill + reconcile (§69.4)

### Create — Application

```text
src/Application/Ingestion/HistoricalBackfillService.cs
src/Application/Ingestion/LiveIngestionService.cs
src/Application/Ingestion/IngestionReconciliationService.cs
src/Application/Ingestion/DealNormalizer.cs
src/Application/Ingestion/OrderNormalizer.cs
src/Application/Ingestion/PositionNormalizer.cs
src/Application/Ingestion/XauSymbolFilter.cs          # persist ALL deals; tag XAU later in I4. Do not drop non-XAU.
src/Application/Ingestion/OutboxHandlers/DealPersistedHandler.cs
```

Live path is exactly §12, in one DbContext transaction:

```text
validate → deduplicate → persist raw → write outbox_events → commit
```

MT5/SSE callback on the collector **only enqueues**. C# live loop **must not** reconstruct, score, or shadow in the subscribe enumerator.

### Create — Worker jobs

```text
apps/mt5-worker/Hosting/BackfillJob.cs
apps/mt5-worker/Hosting/LivePumpJob.cs
apps/mt5-worker/Hosting/ReconciliationJob.cs
apps/mt5-worker/Hosting/OutboxDispatchJob.cs
apps/mt5-worker/Hosting/TickPollJob.cs            # GetTickLast for mapped XAU symbols if SubscribeTicks unavailable
```

### Change

| Path | Change |
|------|--------|
| `src/Infrastructure/Persistence/Repositories/RawMt5RecordWriter.cs` | upsert deals/orders/positions/snapshots; never overwrite a deal row — append `ingestion_events` on broker correction |
| `src/Application/Ingestion/DealDeduplicator.cs` | use unique `(broker_id, deal_ticket)` |

### Tests (I3)

```text
tests/Unit/Ingestion/DealNormalizerTests.cs
tests/Integration/Ingestion/BackfillRestartTests.cs
tests/Integration/Ingestion/LiveThenRestartNoDupesTests.cs
tests/Integration/Ingestion/ReconciliationFillsGapsTests.cs
tests/Integration/Ingestion/OutboxProcessAfterCrashTests.cs
```

**Exit:** history backfill resumable; live deals survive collector restart without duplicate rows; reconciliation compares broker `GetDeals` vs `mt5_deals`; `mt5_xau_ticks` populated **only if** the source actually yields ticks (else empty + `system_events` note — do **not** fabricate, §17).

---

## 8. Increment 4 — Reconstruction + first-3 counter (§69.5–6)

### New project (first real types, not an empty shell)

```text
src/TradeReconstruction/TraderIntelligence.TradeReconstruction.csproj
src/TradeReconstruction/TradeReconstructor.cs
src/TradeReconstruction/PositionLifecycle.cs
src/TradeReconstruction/FirstThreeTradeCounter.cs
src/TradeReconstruction/CanonicalInstrumentMapper.cs
src/TradeReconstruction/ReconstructionResult.cs
src/TradeReconstruction/DependencyInjection.cs
```

References: Domain, Application.

### Domain / Application additions

```text
src/Domain/Reconstruction/ReconstructedTrade.cs          # fields from §14
src/Domain/Reconstruction/CanonicalInstrument.cs
src/Domain/Reconstruction/SourceSymbolMapping.cs
src/Domain/Reconstruction/TraderTradeCursor.cs           # completed XAU count
src/Application/Abstractions/ITradeReconstructor.cs
src/Application/Abstractions/ICanonicalInstrumentMapper.cs
src/Application/Reconstruction/ReconstructFromOutboxHandler.cs
src/Application/Reconstruction/SeedCanonicalInstruments.cs   # XAUUSD row only
```

### Infrastructure

```text
src/Infrastructure/Persistence/Configurations/ReconstructedTradeConfiguration.cs
src/Infrastructure/Persistence/Configurations/CanonicalInstrumentConfiguration.cs
src/Infrastructure/Persistence/Configurations/SourceSymbolMappingConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180008_CanonicalSymbols.cs
src/Infrastructure/Persistence/Migrations/202608180009_ReconstructedTrades.cs
src/Infrastructure/Persistence/Sql/202608180008_CanonicalSymbols.sql
src/Infrastructure/Persistence/Sql/202608180009_ReconstructedTrades.sql
src/Infrastructure/Persistence/Repositories/ReconstructedTradeStore.cs
src/Infrastructure/Seeding/XauSourceSymbolSeed.cs        # XAUUSD, XAUUSD., XAUUSDm, XAUUSD.a, GOLD → XAUUSD
```

### New test project

```text
tests/Replay/TraderIntelligence.Tests.Replay.csproj
tests/Replay/Fixtures/partial_close.json
tests/Replay/Fixtures/scale_in.json
tests/Replay/Fixtures/full_close.json
tests/Replay/Fixtures/reversal.json
tests/Replay/Fixtures/first_three.json
tests/Replay/ReplayHarness.cs
tests/Replay/ReconstructionReplayTests.cs
```

Add `tests/Replay` to the solution under the `tests` folder.

### Unit tests — A09 #2–#7 (delete nothing else)

```text
tests/Unit/Reconstruction/TradeReconstructionTests.cs
tests/Unit/Reconstruction/PartialCloseReconstructionTests.cs
tests/Unit/Reconstruction/ScaleInReconstructionTests.cs
tests/Unit/Reconstruction/FullCloseReconstructionTests.cs
tests/Unit/Reconstruction/PositionReversalReconstructionTests.cs
tests/Unit/Normalization/XauCanonicalMappingTests.cs
```

Change `tests/Unit/TraderIntelligence.Tests.Unit.csproj`: add `ProjectReference` to `TradeReconstruction`.

### Change

| Path | Change |
|------|--------|
| `Mt5TraderIntelligence.sln` | add TradeReconstruction + Tests.Replay |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | reference TradeReconstruction |
| `src/Application/Ingestion/OutboxHandlers/DealPersistedHandler.cs` | enqueue reconstruction; do not reconstruct inline in SSE |
| `docs/trade-reconstruction.md` | contract: Order ≠ Deal ≠ Position ≠ logical trade |
| `docs/xauusd-normalization.md` | mapping table + “never guess FIX tag 55” |

**Rules that must be encoded, not commented**

- Count only **completed reconstructed XAUUSD position lifecycles** (§15).
- Partial close / scale-in / SL-TP modify is **not** a new trade.
- Trade #3 sets state `EARLY_SCORE_ELIGIBLE`, **not** `PROVEN_PROFITABLE` / `LIVE`.
- Unknown source symbol stays unmapped; it does **not** become `XAUUSD`.

**Exit:** A09 reconstruction tests + replay fixtures green; first-3 counter matches fixtures; real broker aliases verified against `mt5_symbols` from I2 (manual check recorded in `docs/xauusd-normalization.md`).

---

## 9. Increment 5 — Deterministic baseline + ranking (§69.7–8)

No Python. No XGBoost. No `services/ml-service`.

### New project

```text
src/Scoring/TraderIntelligence.Scoring.csproj
src/Scoring/DeterministicFeatureEngine.cs
src/Scoring/DeterministicScorer.cs
src/Scoring/ScoreStateMachine.cs
src/Scoring/Features/DrawdownCalculator.cs
src/Scoring/Features/MfeMaeCalculator.cs
src/Scoring/Features/MartingaleDetector.cs
src/Scoring/Features/AveragingDownDetector.cs
src/Scoring/Features/LotConsistencyCalculator.cs
src/Scoring/Features/HoldingTimeCalculator.cs
src/Scoring/Features/SlTpBehaviorCalculator.cs
src/Scoring/Features/FeatureQuality.cs            # EXACT vs APPROXIMATE + price_source (§17)
src/Scoring/Ranking/TraderRanker.cs
src/Scoring/DependencyInjection.cs
```

### Domain / Application

```text
src/Domain/Scoring/TraderFeatureSnapshot.cs
src/Domain/Scoring/TraderScore.cs                 # risk_score, behavior_score, early_quality_score
src/Domain/Scoring/TraderScoreHistory.cs
src/Domain/Scoring/TraderState.cs                 # enum §22
src/Domain/Scoring/TraderRiskFlag.cs
src/Domain/Scoring/TraderStateCode.cs             # INSUFFICIENT_DATA, EARLY_SCORE, WATCH, SHADOW, LIVE_CANDIDATE, LIVE, PAUSED, RISK_BLOCKED, DISQUALIFIED
src/Application/Abstractions/IScoringService.cs
src/Application/Abstractions/IScoreUpdateRequestHandler.cs
src/Application/Scoring/ScoreUpdateRequestHandler.cs
src/Application/Scoring/ScoringInput.cs           # §39 triple: candidate, confidence, suggestedAllocation — even though ML is absent
```

Default after trade #3 + high early score: **SHADOW**, never LIVE (§23). `LIVE` / `LIVE_CANDIDATE` must not be assigned by this increment’s state machine except as unreachable enum members.

### Infrastructure

```text
src/Infrastructure/Persistence/Configurations/TraderFeatureSnapshotConfiguration.cs
src/Infrastructure/Persistence/Configurations/TraderScoreConfiguration.cs
src/Infrastructure/Persistence/Configurations/TraderScoreHistoryConfiguration.cs
src/Infrastructure/Persistence/Configurations/TraderStateConfiguration.cs
src/Infrastructure/Persistence/Configurations/TraderRiskFlagConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180010_TraderScoresAndFlags.cs
src/Infrastructure/Persistence/Sql/202608180010_TraderScoresAndFlags.sql
src/Infrastructure/Persistence/Repositories/TraderScoreStore.cs
```

### Tests — A09 #9–#13

```text
tests/Unit/Features/DrawdownCalculatorTests.cs
tests/Unit/Features/MfeMaeCalculatorTests.cs
tests/Unit/Features/MartingaleDetectorTests.cs
tests/Unit/Features/AveragingDownDetectorTests.cs
tests/Unit/Scoring/ScoreStateTransitionTests.cs
tests/Unit/Scoring/DeterministicScorerTests.cs
tests/Unit/Scoring/TraderRankerTests.cs
tests/Replay/ScoringReplayTests.cs
```

`MfeMaeCalculatorTests` **must** include `Refuses_to_fabricate_from_closed_deals_only` (A09). If `mt5_xau_ticks` is empty, features are omitted and `feature_quality=UNAVAILABLE`, not guessed from destination FIX quotes.

### Change

| Path | Change |
|------|--------|
| `Mt5TraderIntelligence.sln` | add Scoring |
| `apps/mt5-worker/*.csproj` | reference Scoring |
| `docs/scoring.md` | formula + state machine; no ML claims |

**Exit:** traders with ≥3 completed XAU trades have a deterministic score and a rank; trade #3 → `EARLY_SCORE` then default `SHADOW`; martingale / averaging-down flags persist on `trader_risk_flags`.

---

## 10. Increment 6 — API + React skeleton (§69.12 start)

Show items 1–8. QUOTE/shadow pages can be empty shells until I7–I9.

### Change — API (`apps/api/`)

```text
apps/api/Program.cs                                 # Serilog, CORS, SignalR, auth, map modules
apps/api/Auth/DashboardAuth.cs                      # cookie or API key; roles ReadOnly + Analyst only
apps/api/Contracts/BrokerDto.cs
apps/api/Contracts/GroupDto.cs
apps/api/Contracts/TraderListDto.cs
apps/api/Contracts/TraderDetailDto.cs
apps/api/Contracts/ReconstructedTradeDto.cs
apps/api/Contracts/ScoreDto.cs
apps/api/Contracts/HealthDto.cs
apps/api/Endpoints/HealthEndpoints.cs
apps/api/Endpoints/BrokerEndpoints.cs
apps/api/Endpoints/GroupEndpoints.cs
apps/api/Endpoints/TraderEndpoints.cs
apps/api/Endpoints/TradeEndpoints.cs
apps/api/Endpoints/ScoreEndpoints.cs
apps/api/Hubs/OpsHub.cs                             # account counts, ingest lag, scores
apps/api/Mapping/DtoMapping.cs
apps/api/Security/SecretRedaction.cs                # never put passwords on DTOs
```

Delete remaining weather types from `Program.cs`.

Do **not** add endpoints that enable real execution, kill-switch flatten, or model promotion.

### New module — web

```text
apps/web/package.json
apps/web/tsconfig.json
apps/web/vite.config.ts
apps/web/index.html
apps/web/src/main.tsx
apps/web/src/App.tsx
apps/web/src/api/client.ts
apps/web/src/api/types.ts
apps/web/src/auth/session.ts
apps/web/src/layout/AppShell.tsx
apps/web/src/layout/Nav.tsx
apps/web/src/pages/OverviewPage.tsx
apps/web/src/pages/BrokersPage.tsx
apps/web/src/pages/GroupsPage.tsx
apps/web/src/pages/TradersPage.tsx
apps/web/src/pages/TraderDetailPage.tsx
apps/web/src/pages/TradeExplorerPage.tsx
apps/web/src/pages/ScoringPage.tsx
apps/web/src/pages/SystemHealthPage.tsx
apps/web/src/pages/SettingsPage.tsx                 # non-secret config only
apps/web/src/pages/FixPage.tsx                      # stub until I7
apps/web/src/pages/ShadowPage.tsx                   # stub until I8
apps/web/src/pages/RiskPage.tsx                     # stub
apps/web/src/pages/ReconciliationPage.tsx           # MT5 reconcile only
apps/web/src/components/TraderTable.tsx
apps/web/src/components/FirstThreeBadge.tsx
apps/web/src/components/ScoreBar.tsx
apps/web/src/components/BrokerStatusCard.tsx
apps/web/src/hooks/useTraders.ts
apps/web/src/hooks/useHealth.ts
```

Stack (§5): React + TypeScript + Vite + TanStack Query + React Router + SignalR client + ECharts or Recharts. Zustand only if a page actually needs it.

**Do not add** Models, Live Copy Portfolio as working pages. Nav entries may exist as “not in v1”.

### Tests

```text
tests/Integration/Api/HealthAndBrokersTests.cs
tests/Integration/Api/TraderLeaderboardTests.cs
tests/Integration/Api/NoSecretsInDtoTests.cs
```

**Exit:** an operator can open Overview / Brokers / Groups / Traders / Trader Detail and see real I1–I5 data. No FIX password, MT5 password, or proxy password in any JSON or bundle.

---

## 11. Increment 7 — cTrader QUOTE FIX only (§69.9–10)

**Hard constraint:** no TRADE session, no `NewOrderSingle`, no `ClOrdID` generator. `CTRADER_FIX_TRADE_SESSION_ENABLED=false`, `REAL_COPY_EXECUTION_ENABLED=false`.

### Change — `src/Fix.CTrader/` (delete `Class1.cs`)

```text
src/Fix.CTrader/QuickFix/CTraderFixSettingsFactory.cs
src/Fix.CTrader/QuickFix/CTraderDataDictionary.md     # notes only; binary dict next to it
src/Fix.CTrader/Spec/cTrader44.xml                    # cTrader RoE subset, not generic FIX.4.4
src/Fix.CTrader/Sessions/CTraderQuoteSession.cs
src/Fix.CTrader/Sessions/QuoteSessionState.cs         # independent sequence / heartbeat
src/Fix.CTrader/Sessions/TlsOptions.cs                # production default SSL 5211
src/Fix.CTrader/Messages/SecurityListClient.cs
src/Fix.CTrader/Messages/MarketDataClient.cs
src/Fix.CTrader/Mapping/DestinationSymbolMapper.cs    # SecurityList → canonical XAUUSD
src/Fix.CTrader/Config/FixHeaderOptions.cs            # SenderCompID, TargetCompID, SenderSubID, TargetSubID all configurable (§26)
src/Fix.CTrader/Logging/FixLogRedactor.cs             # never log password tags
src/Fix.CTrader/DependencyInjection.cs
```

Packages: pin `QuickFIXn.Core` + `QuickFIXn.FIX4.4` (or the QuickFIX/n package that accepts a custom dictionary). Do **not** write a `TcpClient` FIX engine (§1.8).

Do **not** create `CTraderTradeSession.cs` in v1.

### Domain / Application / Infrastructure

```text
src/Domain/Venues/ExecutionVenue.cs
src/Domain/Venues/DestinationSymbol.cs
src/Domain/Venues/DestinationQuote.cs
src/Domain/Fix/FixSession.cs
src/Domain/Fix/FixSessionEvent.cs
src/Application/Abstractions/IQuoteSession.cs
src/Application/Abstractions/IDestinationQuoteCache.cs
src/Application/Fix/QuoteIngestionService.cs
src/Application/Fix/SecurityListSyncService.cs
src/Infrastructure/Persistence/Configurations/ExecutionVenueConfiguration.cs
src/Infrastructure/Persistence/Configurations/DestinationSymbolConfiguration.cs
src/Infrastructure/Persistence/Configurations/DestinationQuoteConfiguration.cs
src/Infrastructure/Persistence/Configurations/FixSessionConfiguration.cs
src/Infrastructure/Persistence/Configurations/FixSessionEventConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180011_VenuesAndDestinationQuotes.cs
src/Infrastructure/Persistence/Migrations/202608180012_FixQuoteSessions.cs
src/Infrastructure/Persistence/Sql/202608180011_VenuesAndDestinationQuotes.sql
src/Infrastructure/Persistence/Sql/202608180012_FixQuoteSessions.sql
src/Infrastructure/Quotes/RedisDestinationQuoteCache.cs   # cache of latest bid/ask + timestamp; DB remains authority
src/Infrastructure/Persistence/Repositories/DestinationQuoteStore.cs
```

Seed `execution_venues` with one row: Pepperstone/cServer. Do **not** seed a guessed instrument ID.

### Change — `apps/fix-worker/`

```text
apps/fix-worker/Hosting/Worker.cs                   # replace template
apps/fix-worker/Hosting/QuoteSessionHost.cs
apps/fix-worker/Hosting/SecurityListJob.cs
apps/fix-worker/Hosting/QuotePersistJob.cs
apps/fix-worker/Program.cs
apps/fix-worker/TraderIntelligence.FixWorker.csproj # QuickFIX ref via Fix.CTrader; Serilog
```

`Worker.cs` must refuse to start a TRADE session even if someone flips the env flag, until Phase 7 code exists. Guard:

```csharp
if (options.TradeSessionEnabled || options.RealCopyExecutionEnabled)
    throw new InvalidOperationException("TRADE/real copy is not built in v1.");
```

### API / web

```text
apps/api/Endpoints/FixQuoteEndpoints.cs
apps/api/Contracts/FixQuoteHealthDto.cs
apps/web/src/pages/FixPage.tsx                      # QUOTE card live; TRADE card = "not in v1"
apps/web/src/components/QuoteTape.tsx
```

### Tests

```text
tests/Unit/Fix/FixHeaderOptionsTests.cs             # SenderSubID/TargetSubID not hardcoded
tests/Unit/Fix/SecurityListXauMappingTests.cs
tests/Unit/Normalization/XauCanonicalMappingTests.cs  # extend: cTrader instrument id
tests/Integration/Fix/QuoteSessionConfigTests.cs    # SSL port 5211 default; dictionary loads
tests/Integration/Fix/QuoteReplayTests.cs           # parse recorded MarketDataIncrementalRefresh (no live account)
```

Recorded FIX fixtures (no secrets):

```text
tests/Integration/Fix/Fixtures/security_list_xau.fix
tests/Integration/Fix/Fixtures/md_incremental_xau.fix
```

**Exit:** QUOTE Logon over TLS; Security List persisted; Pepperstone XAU instrument ID in `destination_symbols`; live bid/ask + `quote_age` on dashboard; TRADE send still impossible.

---

## 12. Increment 8 — Shadow copy (§69.11)

This is the last *engine* increment of the first useful version.

### New projects

```text
src/Shadow/TraderIntelligence.Shadow.csproj
src/Shadow/ShadowCopyEngine.cs
src/Shadow/ShadowFills.cs
src/Shadow/ShadowPositionBook.cs
src/Shadow/ShadowPnlCalculator.cs
src/Shadow/SourceVsShadowAnalyzer.cs
src/Shadow/CopyIntentFactory.cs
src/Shadow/QuantityConverter.cs                     # §38; SUT for A09 #8
src/Shadow/DependencyInjection.cs

src/Risk/TraderIntelligence.Risk.csproj
src/Risk/ShadowRiskEngine.cs                        # IRiskEngine implementation, shadow subset
src/Risk/Rules/StaleQuoteRule.cs
src/Risk/Rules/StaleSignalRule.cs
src/Risk/Rules/SpreadTooWideRule.cs
src/Risk/Rules/PriceMovedTooFarRule.cs
src/Risk/Rules/MartingaleBlockRule.cs
src/Risk/Rules/StopNewShadowRule.cs
src/Risk/Rules/ExposureAction.cs                    # OPEN / INCREASE / REDUCE / CLOSE (§64)
src/Risk/DependencyInjection.cs
```

`ShadowRiskEngine` may `approve` / `reduce` / `reject` **shadow** intents only. It must not have a code path that talks to FIX send.

### Domain / Application

```text
src/Domain/Copy/CopyIntent.cs                       # expires_at, max_signal_age (§63)
src/Domain/Copy/CopyAllocation.cs
src/Domain/Copy/CopyIntentStatus.cs
src/Domain/Shadow/ShadowOrder.cs
src/Domain/Shadow/ShadowFill.cs
src/Domain/Shadow/ShadowPosition.cs
src/Domain/Shadow/ShadowPerformance.cs
src/Domain/Risk/RiskDecision.cs
src/Domain/Risk/RiskEvent.cs
src/Application/Abstractions/ICopyIntentFactory.cs
src/Application/Abstractions/ICopyIntentStore.cs
src/Application/Abstractions/ICopyCandidateEvaluator.cs
src/Application/Abstractions/IRiskEngine.cs
src/Application/Abstractions/IShadowCopyEngine.cs
src/Application/Copy/CopyCandidateEvaluator.cs      # SHADOW state + no severe flags
src/Application/Copy/CreateCopyIntentHandler.cs
src/Application/Shadow/ShadowFromOutboxHandler.cs
```

Do **not** add `IApprovedExecutionIntentService`, `ClOrdIdGenerator`, or `IFixExecutionWorker` send methods.

### Infrastructure

```text
src/Infrastructure/Persistence/Configurations/CopyIntentConfiguration.cs
src/Infrastructure/Persistence/Configurations/CopyAllocationConfiguration.cs
src/Infrastructure/Persistence/Configurations/ShadowOrderConfiguration.cs
src/Infrastructure/Persistence/Configurations/ShadowFillConfiguration.cs
src/Infrastructure/Persistence/Configurations/ShadowPositionConfiguration.cs
src/Infrastructure/Persistence/Configurations/ShadowPerformanceConfiguration.cs
src/Infrastructure/Persistence/Configurations/RiskDecisionConfiguration.cs
src/Infrastructure/Persistence/Configurations/RiskEventConfiguration.cs
src/Infrastructure/Persistence/Migrations/202608180013_CopyIntents.cs
src/Infrastructure/Persistence/Migrations/202608180014_ShadowBook.cs
src/Infrastructure/Persistence/Migrations/202608180015_ShadowRiskDecisions.cs
src/Infrastructure/Persistence/Sql/202608180013_CopyIntents.sql
src/Infrastructure/Persistence/Sql/202608180014_ShadowBook.sql
src/Infrastructure/Persistence/Sql/202608180015_ShadowRiskDecisions.sql
src/Infrastructure/Persistence/Repositories/CopyIntentStore.cs
src/Infrastructure/Persistence/Repositories/ShadowBookStore.cs
src/Infrastructure/Persistence/Repositories/RiskDecisionStore.cs
```

### Worker

```text
apps/fix-worker/Hosting/ShadowJob.cs                # destination quotes + outbox ShadowCopyRequested
apps/mt5-worker/Hosting/CopyIntentFromTradeJob.cs   # after reconstruction; persist intent only
```

Source close → shadow `CLOSE_EXPOSURE` (not a new open). Stale `CopyIntent` (`now > expires_at`) is skipped — no catch-up flood (§63).

### Tests — A09 #8, #14 (shadow subset), #15

```text
tests/Unit/Normalization/SourceDestinationQuantityConversionTests.cs
tests/Unit/Risk/RiskLimitEngineTests.cs             # stale quote/signal, spread, martingale; no live flatten
tests/Unit/Execution/CopyIntentIdempotencyTests.cs  # folder name kept for A09; no ClOrdID
tests/Unit/Shadow/ShadowFillFromQuoteTests.cs
tests/Unit/Shadow/ShadowPnlTests.cs
tests/Unit/Shadow/StaleIntentExpiryTests.cs
tests/Replay/ShadowReplayTests.cs
tests/Integration/Shadow/IdempotentIntentPersistTests.cs
```

**Do not add** `ClOrdIdGeneratorTests` or `ExecutionReportStateTransitionTests` in v1 (A09 #16–17 belong to Phase 7–8).

### Change

| Path | Change |
|------|--------|
| `Mt5TraderIntelligence.sln` | add Shadow, Risk |
| `apps/fix-worker` + `apps/mt5-worker` csproj | reference Shadow, Risk |
| `docs/shadow-copy.md` | pricing uses destination quotes only |

**Exit:** selected `SHADOW` traders generate idempotent `copy_intents`; shadow entries/exits priced from `destination_quotes`; stale quote/signal rejected; source-vs-shadow slippage rows exist; no FIX order leaves the process.

---

## 13. Increment 9 — React complete + §69 sign-off

### Create / finish

```text
apps/web/src/pages/ShadowPage.tsx                   # book, P&L, drift
apps/web/src/pages/TraderDetailPage.tsx             # first-3 highlight, score timeline, shadow book
apps/web/src/pages/FixPage.tsx                      # quote tape + instrument id
apps/web/src/pages/OverviewPage.tsx                 # §47 fields that exist (no live P&L, no TRADE health beyond "disabled")
apps/web/src/pages/ReconciliationPage.tsx           # MT5 last reconcile; cTrader TRADE = N/A
apps/web/src/components/ShadowPnlChart.tsx
apps/web/src/components/SourceVsShadowTable.tsx
apps/api/Endpoints/ShadowEndpoints.cs
apps/api/Contracts/ShadowDto.cs
docs/ctrader-fix.md                                 # QUOTE-only runbook
docs/mt5-integration.md                             # collector + C# adapter
```

### Change

| Path | Change |
|------|--------|
| `apps/api/Hubs/OpsHub.cs` | push quote age, shadow P&L, ingest lag |
| `tests/Integration/Api/ShadowAndQuoteDtoTests.cs` | no secrets; TRADE flags false |

### §69 acceptance checklist (copy into the release note; do not tick without evidence)

```text
[ ] 1. Both collectors connected (Achiever + StarwaveFX health)
[ ] 2. mt5_groups is a full manager dump, not plan-mapping-only
[ ] 3. ~5,000 accounts in mt5_accounts; checkpoints restart-safe
[ ] 4. XAUUSD deals in mt5_deals with broker_id compound keys; dupes rejected
[ ] 5. reconstructed_trades replay tests pass
[ ] 6. first-3 counter = completed XAU lifecycles only
[ ] 7. deterministic risk/behavior/early scores persisted
[ ] 8. Traders page ranks by early_quality_score
[ ] 9. QUOTE FIX TLS logon stable; TRADE not running
[ ] 10. destination_symbols has discovered Pepperstone XAU id (not guessed)
[ ] 11. shadow_orders/fills/positions priced from destination_quotes
[ ] 12. React shows 1–11 without secrets
```

**v1 DONE** when this list is evidenced on disk (test output + a short `reports/` run note). Then — and only then — Phase 6 ML may be judged (separate sequence).

---

## 14. Solution / csproj graph after I9

```text
Domain
  ↑
Application
  ↑
  ├── Infrastructure  → Api, Mt5Worker, FixWorker, Tests.Integration
  ├── Mt5             → Mt5Worker
  ├── TradeReconstruction → Mt5Worker, Tests.Unit, Tests.Replay
  ├── Scoring         → Mt5Worker, Api, Tests.Unit, Tests.Replay
  ├── Shadow          → FixWorker, Mt5Worker, Tests.Unit, Tests.Replay
  ├── Risk            → FixWorker, Mt5Worker, Tests.Unit
  └── Fix.CTrader     → FixWorker, Tests.Unit, Tests.Integration

apps/web                 (npm; not in .sln)
apps/mt5-collector       (CMake; not in .sln — same as mt5-sdk)
```

Add project references in the increment that needs them. Api still must **not** reference `Mt5` or `Fix.CTrader` directly — it reads Postgres (A11 hole stays closed on purpose).

---

## 15. Packages allowed vs forbidden (v1)

**Add when the increment needs them**

```text
Microsoft.EntityFrameworkCore 8.0.4                 (I1)
Testcontainers.PostgreSQL                           (I1)
Microsoft.Extensions.Http                           (I2)
Serilog.Extensions.Hosting                          (I2, I6, I7)
Microsoft.AspNetCore.SignalR                        (I6)
QuickFIXn.Core + QuickFIXn.FIX4.4 (pinned)          (I7)
```

Already present: FluentValidation 11.9.2, Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4, StackExchange.Redis 2.8.0, Serilog.AspNetCore 8.0.2, Swashbuckle 6.6.2, xUnit, Moq, FluentAssertions.

**Do not add**

```text
Confluent.Kafka / MassTransit / RabbitMQ.Client / NATS
KubernetesClient / Helm charts
ClickHouse.Client
Microsoft.SemanticKernel / Azure.AI.OpenAI / any LLM SDK
QuickFIX TRADE send helpers beyond dictionary parse
MediatR unless a later increment proves the ceremony is worth it (default: no)
```

---

## 16. Feature flags for v1 (config only)

Write these in `.env.example` and worker `appsettings.json`. Defaults are the safety story.

```text
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=false
REAL_COPY_EXECUTION_ENABLED=false
SHADOW_COPY_ENABLED=true
STOP_NEW_SHADOW=false
```

`apps/fix-worker` **throws on startup** if TRADE or real-copy is true (I7). There is no code path to flip this without a new Phase 7 increment.

---

## 17. Explicit later sequence (not this file’s job)

When §69 is signed off, a **new** sequence document should list Phase 6–8 files. Expected first files *then* (do not create now):

```text
services/ml-service/pyproject.toml
src/Infrastructure/Persistence/Migrations/2026xxxxxx_ModelRegistry.cs
src/Fix.CTrader/Sessions/CTraderTradeSession.cs
src/Execution/ExecutionIntent.cs
src/Execution/ClOrdIdGenerator.cs
src/Infrastructure/Persistence/Migrations/2026xxxxxx_FixTradeAndExecution.cs
```

---

## 18. Implementation order (one sentence each)

1. **I0** — compose + `.env.example` + gitignore so secrets never land.
2. **I1** — EF schema 0001–0007 + Application ports + `DealDeduplicator` tests.
3. **I2** — read-only `mt5-collector` + C# `Mt5CollectorClient` + both brokers + all groups + account sync.
4. **I3** — backfill + live + reconcile + transactional outbox; prove idempotency.
5. **I4** — `TradeReconstruction` project + first-3 + XAU mapping + replay fixtures.
6. **I5** — `Scoring` project + deterministic rank; SHADOW default; no ML.
7. **I6** — API + React for brokers/groups/traders/scores.
8. **I7** — QuickFIX/n QUOTE TLS + Security List + destination quotes.
9. **I8** — `Shadow` + slim `Risk` + idempotent `copy_intents` on destination prices.
10. **I9** — finish React + tick the twelve §69 boxes with evidence.

Do not start I(n+1) until that increment’s **Exit** paragraph is true.

---

## 19. Traceability

| Architecture demand | This document |
|---------------------|---------------|
| §73.C “exact files/modules/migrations that will change” | §§3–13 file lists |
| §66 adapt, do not duplicate | §2, reuse `mt5-sdk`, no empty sibling projects |
| §67 Phases 0–5 | I0–I9 |
| §69 12-item useful system | §0 table + §13 checklist |
| §71 no Kafka/K8s/LLM | §0 non-goals, §15 forbidden packages |
| §13 outbox instead of Kafka | I1 `outbox_events` + `IEventBus` |
| §72.6–7 lightweight callback, persist first | I3 live path |
| §72.13 discover instrument IDs | I7 Security List |
| §23 trade #3 → SHADOW | I5 state machine |
| A02 ports in Application | I1 / I4 / I5 / I8 Abstractions |
| A09 test class names | cited per increment |
| A16 `/mt5/*` contract | I2 collector routes |

End of A30.
