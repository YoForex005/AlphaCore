# B07 — `apps/mt5-worker` and `apps/fix-worker` gap vs architecture / A64

| Field | Value |
|---|---|
| Agent | B07 (workers gap) |
| Date | 2026-08-18 |
| Scope | `D:\Prop\apps\mt5-worker`, `D:\Prop\apps\fix-worker`, and the Application / Infrastructure / Mt5 / Fix.CTrader types they actually call |
| Product source modified | **No** |
| Binding specs | Architecture v2 §§7–13, 25–34, 41–43, 56–67, 72; `A07_mt5_worker_audit.md`; `A08_fix_worker_audit.md`; `A64_worker_pipelines.md` |
| Method | Read current worker hosts + DI + connectors + store + seeder + dashboard queries + tests. File hashes via SHA-256. Earlier A07/A08/A64 *measured-state* sections are **stale** and are not reused as current evidence. |

**Honesty rule:** a demo loop that upserts fixture deals is not Phase 1. A row that says `LoggedOn` is not a FIX session. A green `dotnet build` is not ingestion. Absence of `NewOrderSingle` is not an implemented flag gate.

---

## 0. Verdict

**FAIL — both workers are demo god-loops, not the A64 pipelines.**

They have moved past the stock `dotnet new worker` 1-second log (A07/A08 snapshot). That is the only real progress. They still do **not** implement source authority (MT5) or destination authority (cTrader FIX).

| Score | Measured |
|---|---|
| mt5-worker required hosted jobs (A64 §8 / A07 §5) | **0 / 7** (one `Worker.ExecuteAsync` instead) |
| fix-worker required hosted jobs (A64 §9) | **0 / 10** (one session-row heartbeat instead) |
| Phase 1 deliverables (§67) | **0 / 8** production; demo theater only |
| Phase 4 deliverables (§67) | **0 / 7** |
| Phase 7 deliverables (§67) | **0 / 6** |
| `NewOrderSingle` send possible if process starts | **No** (SAFE_BY_ABSENCE) |
| `MaySendNewOrderSingle` implemented | **No** |
| Dashboard / DB can show FIX healthy without a socket | **Yes** (UNSAFE health lie) |
| Worker tests that host `TraderIntelligence.Mt5Worker` / `FixWorker` | **0** |
| EF migrations | **0** |
| Outbox producer + claim loop | **MISSING** |
| Checkpoints read/written by either worker | **MISSING** (entity exists, unused) |
| Real Achiever / StarwaveFX / cTrader sockets | **0** |

`docs/architecture.md` currently says workers are an “ingest/score loop” and “FIX heartbeat/status only” on the way to a first useful version. That sentence describes **demo theater**. It is not Phase 1, 4, or 7 acceptance.

Classification of the two hosts:

| Component | Class |
|---|---|
| `apps/mt5-worker` project + sln membership | `EXISTS_AND_GOOD` (scaffold) |
| `apps/mt5-worker/Program.cs` | `EXISTS_NEEDS_REFACTOR` |
| `apps/mt5-worker/Worker.cs` god loop | `DEPRECATED` — delete when real jobs register |
| `apps/fix-worker` project + sln membership | `EXISTS_AND_GOOD` (scaffold) |
| `apps/fix-worker/Program.cs` | `EXISTS_NEEDS_REFACTOR` |
| `apps/fix-worker/Worker.cs` fake session heartbeat | **`UNSAFE`** (writes Ready/LoggedOn + `LastInboundAt`) |
| Fake connectors registered as the only `IMt5BrokerConnector` | `DEPRECATED` as a production transport; `EXISTS_AND_GOOD` as a unit fixture |
| `DemoSeeder` on worker boot | `UNSAFE` if Postgres is ever shared |
| Live `35=D` path | `MISSING` — **SAFE_BY_ABSENCE**, not a control |

Do **not** treat these processes as collectors, quote engines, or execution workers.

---

## 1. Stale swarm snapshots (do not quote as current)

A07, A08, A29, and A64 §2 described both hosts as:

```text
while + Task.Delay(1000) + "Worker running at: {time}"
AddHostedService<Worker>() only
no DI, no DbContext, no connector
```

That was true earlier on 2026-08-18. It is **false now**. Current `Worker.cs` hashes:

| File | SHA-256 | Bytes |
|---|---|---:|
| `D:\Prop\apps\mt5-worker\Worker.cs` | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 1882 |
| `D:\Prop\apps\fix-worker\Worker.cs` | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | 1971 |

The **gap list in A64 §§8–9 is still the binding target**. Only the “current measured state” paragraphs in those docs are stale. This file replaces those paragraphs.

---

## 2. Inventory (source of truth, exclude `bin/` / `obj/`)

### 2.1 `apps/mt5-worker`

| Path | Bytes | SHA-256 | Role | Class |
|---|---:|---|---|---|
| `Worker.cs` | 1882 | `57499700…B92B` | 30 s ingest + reconstruct + score god loop | DEPRECATED |
| `Program.cs` | 859 | `2FACC25C…0A79` | `AddTraderIntelligence` + `EnsureCreated` + `DemoSeeder` + one hosted service | EXISTS_NEEDS_REFACTOR |
| `TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028…B91C` | net8 Worker SDK; refs Domain, Application, Infrastructure, Mt5; Hosting 8.0.1 only | Scaffold OK |
| `appsettings.json` | 137 | `AB16B7B7…FF33` | Logging levels only | Missing broker/DB/outbox |
| `appsettings.Development.json` | 137 | `AB16B7B7…FF33` | Identical | Missing |
| `Properties/launchSettings.json` | 296 | `8E2A7548…3E36` | `DOTNET_ENVIRONMENT=Development` only | No `MT5__*` / `DATABASE_URL` |

`UserSecretsId` = `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1`. Unused. No secrets are loaded.

### 2.2 `apps/fix-worker`

| Path | Bytes | SHA-256 | Role | Class |
|---|---:|---|---|---|
| `Worker.cs` | 1971 | `B48033A5…0D48` | 15 s UPDATE of `fix_sessions` timestamps/status | UNSAFE |
| `Program.cs` | 859 | `05732C24…D7CC` | Same seeder composition as mt5-worker | EXISTS_NEEDS_REFACTOR |
| `TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBF…5DB4` | net8 Worker SDK; refs Domain, Application, Infrastructure, Fix.CTrader | Scaffold OK |
| `appsettings.json` | 137 | `AB16B7B7…FF33` | Logging only — **byte-identical** to mt5-worker | Missing venue flags |
| `appsettings.Development.json` | 137 | `AB16B7B7…FF33` | Identical | Missing |
| `Properties/launchSettings.json` | 296 | `25A750D8…EF9B` | `DOTNET_ENVIRONMENT=Development` only | No `CTrader*` / `REAL_COPY*` |

`UserSecretsId` = `dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79`. Unused.

`Program.cs` of the two hosts differs only by the `using TraderIntelligence.*Worker` namespace. Both seed the **same** demo universe, including FIX session rows and a static XAU quote.

### 2.3 Types the workers actually resolve

| Type | Path | Used by |
|---|---|---|
| `AddTraderIntelligence` | `src/Infrastructure/DependencyInjection.cs` | both `Program.cs` |
| `TraderDbContext` | `src/Infrastructure/Persistence/TraderDbContext.cs` | both |
| `DemoSeeder` | `src/Infrastructure/Seeding/DemoSeeder.cs` | both `Program.cs` |
| `DealIngestionService` | `src/Application/Ingestion/DealIngestionService.cs` | mt5-worker only |
| `ReconstructionScoringService` | same file | mt5-worker + seeder |
| `IMt5BrokerConnector` / `BrokerRegistry` / `FakeMt5BrokerConnector` / `DemoBrokerFactory` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` + `src/Application/Contracts/Mt5Contracts.cs` | DI + ingestion |
| `EfTradingStore` | `src/Infrastructure/Persistence/EfTradingStore.cs` | ingestion / scoring |
| `FixSessionState` | `src/Domain/Entities/FixSessionState.cs` | fix-worker heartbeat |

### 2.4 Types that exist and are **not** used by either worker

| Type | Why it matters |
|---|---|
| `IBrokerConnector` + `SubscribeEventsAsync` | Parallel unused port with `ulong` logins and Domain entities |
| `Mt5BrokerOptions` | Never `Bind` / `ValidateOnStart` |
| `CTraderFixOptions` | Never bound; worker reads a different config key |
| `FixSessionOwnership` + in-memory fencing lock | Lease not acquired |
| `FixMessageParser` / `FixSimulationHarness` | Test helpers only |
| `QuickFix.Net` 1.8.0 on `Fix.CTrader.csproj` | **Wrong package** (A35 pin is `QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1). **Zero** C# `using QuickFix` |
| `RiskEngine`, `ShadowCopyEngine`, `ExecutionOrderStateMachine`, `ClOrdIdFactory` | Domain engines; not hosted |
| `OutboxEvent`, `SyncCheckpoint` | DbSets exist; workers never read/write them |
| `CopyIntent`, `ExecutionIntent`, `KillSwitch`, `RiskDecisionRecord` | Tables exist; workers ignore them |
| `IDashboardQueries` | API only |
| C++ `mt5-sdk` (`MT5Manager`, `MT5Pool`, `MT5Watchdog`, `MT5HttpClient`) | Not referenced by the C# worker |

No Dockerfile, no compose, no health Kestrel port (`5081` / `5082` in A64 §11). No Serilog. No OpenTelemetry. `StackExchange.Redis` is referenced by Infrastructure and unused.

---

## 3. What the processes actually do

### 3.1 Shared boot (both hosts)

```1:22:D:\Prop\apps\mt5-worker\Program.cs
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ReconstructionScoringService>(),
        CancellationToken.None);
}

host.Run();
```

`apps/fix-worker/Program.cs` is the same sequence.

Problems baked into boot:

1. **`EnsureCreated` instead of a versioned migration** — violates architecture §72.3. Schema drift will not upgrade.
2. **Both workers seed source brokers, deals, scores, FIX rows, and a static quote.** Destination authority is not allowed to write the source ledger (A64 §3.1 / §9.11).
3. **Seeder short-circuits on `Brokers.Any()`.** First process wins. Second process on a shared Postgres sees a universe it did not validate.
4. **Seeder inserts FIX QUOTE as `ReadyForMarketData` and TRADE as `LoggedOn` with `TargetCompId = cServer`.** Those statuses are *earned* after Logon + Security List + (for TRADE) MassStatus/Positions. They are fabricated here.
5. **Seeder writes a static destination quote** (`2399.45` / `2399.85`, `VenueInstrumentId = null`) so dashboard quote age looks live until it goes stale — then the fix-worker heartbeat still claims the *session* is ready.

### 3.2 DI always installs the demo factory

```17:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
    public static IServiceCollection AddTraderIntelligence(this IServiceCollection services, IConfiguration configuration)
    {
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

        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        // ...
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        return services;
    }
```

Even when `DATABASE_URL` is a real Postgres DSN, connectors stay **in-memory fixtures**. There is no `IMt5BrokerConnectorFactory`, no `Mt5:Brokers` bind, no password refuse-to-start, no remote HTTP client, no native Manager.

Default worker `appsettings` have **no** connection string → each process gets `UseInMemoryDatabase("trader-intelligence")`. That name is **per process**. API, mt5-worker, and fix-worker are three isolated demo worlds unless an operator injects Postgres. Dashboard cannot see worker writes in the default launch.

Worker `appsettings` also do not bind `.env.example` keys (`MT5_*`, `CTRADER_FIX_*`, `REAL_COPY_EXECUTION_ENABLED`, `DATABASE_URL`). The committed example is **unread** by these hosts.

### 3.3 mt5-worker loop

```17:44:D:\Prop\apps\mt5-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var ingestion = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
                var scoring = scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>();
                var from = DateTimeOffset.UtcNow.AddDays(-30);
                var to = DateTimeOffset.UtcNow.AddMinutes(1);
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "MT5 sync cycle failed; will retry. No source trades invented.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
```

`DealIngestionService.SyncBrokerAsync` does: `Connect` → `GetGroups` → upsert groups → `GetAccounts(null)` → per account upsert + `GetDeals(from,to)` + `GetPositions` replace. That *shape* matches §7’s connect/groups/accounts/history **sketch**. It is still not Phase 1:

| Law | What the loop does |
|---|---|
| A64: jobs must not collapse into one `ExecuteAsync` | Connect, discovery, accounts, history, reconstruction, scoring share one 30 s tick |
| Server time, not host clock (A64 §4.1) | `DateTimeOffset.UtcNow ± 30d` |
| Checkpoint + page-to-EOF (A07 §5.4) | No `sync_checkpoints` I/O; Fake returns one in-memory page |
| Truncated `GetDeals` → do not advance | Nothing to advance; Fake cannot return `false` |
| Outbox same commit as raw (§12) | `EfTradingStore` `SaveChanges` **per group / account / deal / position**; no outbox row |
| Do not reconstruct on the ingest stack (A64 §3.3, §8.4) | `RebuildTraderAsync` immediately after `SyncBrokerAsync` |
| `Reconstruction:Enabled` / `Scoring:Enabled` default false in Phase 1 | Always on; flags unread |
| Discover all Manager groups | Discovers whatever the Fake list contains (3 Achiever + 1 Starwave) |
| Score every synchronized login | Hardcoded `{10001,10002,10003,99001}`; `login >= 99000` picks broker |
| Live events + deal-lag poll | No `Subscribe*`, no 2 s poll, no pump/no-pump gauge |
| Periodic reconcile | Absent |
| Orders history | `IMt5BrokerConnector` has **no** `GetOrders` |
| `GetServerTime` | Only on unused `IBrokerConnector` |
| Watchdog 5→60 s | `ConnectAsync` sets a bool |
| Do not invent source trades | Honored **only** because Fake deals are fixtures. The 30-day window **cannot see them** (see §3.4) |
| Disconnect on shutdown | Loop just cancels; no `DisconnectAsync` |

### 3.4 Demo deals are outside the worker window

`DemoBrokerFactory` stamps deals at `2026-06-01T08:00:00Z` (and +hours / +1 day).

This report’s clock is **2026-08-18**. Worker window = `[now-30d, now+1m]` ≈ `2026-07-19 … 2026-08-18`.

Fake `GetDeals` filters `d.Time >= from && d.Time <= to`. After `DemoSeeder` (which uses `2026-01-01 … 2026-12-31`) the 30 s loop **re-fetches zero deals**. It still:

- re-upserts the four groups / four accounts,
- `ReplacePositions` with an **empty** list (factory does not seed positions) → **wipes** `mt5_positions_current` for those logins every cycle,
- rebuilds scores for the four hard-coded logins from whatever is already in the store.

That is not backfill. It is a score-recompute timer on fixture rows.

### 3.5 Store behavior the worker inherits

`EfTradingStore`:

- Deal identity `(broker_id, deal_ticket)` — **good** (integration test `Deal_upsert_is_idempotent`).
- Deal insert is insert-only; a later correction **cannot** land (A07 wants revision / `ingestion_events`, not silent rewrite — also not a second row).
- `ReplacePositions` is delete-all-for-login then insert. A failed/empty fetch **erases** the book. Fail-open vs A64 §7.1.
- Group update only writes `Currency` + `LastSyncedAt` (other fields frozen after first insert).
- Account update writes group/balance/equity only (margin/profit/leverage drift ignored on update).
- Reconstruction `ReplaceReconstructedAsync` deletes-and-inserts per login; `TraderScore.Id = Guid.NewGuid()` on every rebuild then upsert-by-(broker,login) — history rows accumulate every 30 s (`TraderScoreHistory` insert always).
- **No** `IOutboxWriter`. **No** checkpoint write.

### 3.6 fix-worker loop

```19:48:D:\Prop\apps\fix-worker\Worker.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            if (quote is not null)
            {
                quote.LastInboundAt = DateTimeOffset.UtcNow;
                quote.Status = FixSessionStatus.ReadyForMarketData;
            }

            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            if (trade is not null)
            {
                trade.LastInboundAt = DateTimeOffset.UtcNow;
                trade.Status = real ? FixSessionStatus.LoggedOn : FixSessionStatus.LoggedOn;
            }

            await db.SaveChangesAsync(stoppingToken);
            if (real)
                _logger.LogWarning("Real copy is enabled in config, but worker still refuses NewOrderSingle until risk/reconciliation gates pass.");

            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
    }
```

This is **worse than the old 1 s log**.

| Observation | Why it is a gap / hazard |
|---|---|
| No socket, no QuickFIX initiator, no TLS 5211/5212 | Phase 4/7 not started |
| Writes `LastInboundAt = UtcNow` | Dashboard `GetFixSessions` treats this as live inbound |
| Forces QUOTE → `ReadyForMarketData` | Status is supposed to mean Security List + MD are up |
| Forces TRADE → `LoggedOn` **regardless of `real`** | Ternary is a no-op; both branches `LoggedOn` |
| Never `Reconciling` / `ReadyForExecution` / `Disconnected` | Cannot represent truth |
| Config key `CTrader:RealCopyExecutionEnabled` | Not in *this* host’s `appsettings`. Exists on **API** `appsettings.json`. Worker default is `false` unless someone copies API config or sets env `CTrader__RealCopyExecutionEnabled` |
| Log text claims `REAL_COPY_EXECUTION_ENABLED` | That env name is **not** the key that is read |
| “refuses NewOrderSingle” | There is no send function to refuse. Log is theater |
| No `try/catch` | First DB failure kills the hosted service |
| Does not touch `OwnerHeld` / fencing | Lease unused |
| Does not consume dest-family outbox | No `copy.shadow_intent` / `execution.approved` handlers |
| Does not observe `KillSwitch` | Kill switch row is seeded and ignored |
| Seq numbers stay at seeder `1` / `1` | Heartbeat does not increment; looks like a frozen session with a moving clock |

Dashboard coupling (API process, if it shared the DB):

```39:42:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
            brokers > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution,
            false);
```

`GetBrokers` hard-codes `Connected = true`. API `GET /api/health` independently hard-codes Achiever + QUOTE healthy. A64 risk **R14** (“treat template heartbeat as health”) is now **realized in product code**.

---

## 4. Dual ports and package pins (worker-adjacent)

### 4.1 Two connector interfaces

| | `IMt5BrokerConnector` (used) | `IBrokerConnector` (dead) |
|---|---|---|
| File | `src/Application/Contracts/Mt5Contracts.cs` | `src/Mt5/Connectors/IBrokerConnector.cs` |
| Login type | `long` | `ulong` |
| Groups/accounts/deals | DTOs | Domain entities |
| `GetServerTime` | no | yes |
| `SubscribeEventsAsync` | no | yes |
| `GetOrders` | no | no |
| Implementor | `FakeMt5BrokerConnector` | **none** |

A64 §14 wanted one `IMt5BrokerConnector` in Application. The unused `IBrokerConnector` is a second sketch. Do not implement both.

### 4.2 QuickFIX pin

`TraderIntelligence.Fix.CTrader.csproj` references `QuickFix.Net` **1.8.0**. A35 forbids unofficial ids and pins `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1**. No product `.cs` file imports the package. Classification: `UNSAFE` to keep as a silent wrong pin; `MISSING` as an engine.

### 4.3 Options vs worker config vs `.env.example`

Three disjoint naming schemes, **none** bound on the worker hosts:

| Scheme | Example | Who reads it today |
|---|---|---|
| A64 / options class | `CTraderFix:RealCopyExecutionEnabled`, `Mt5:Brokers:0:Password` | nobody |
| API `appsettings` | `CTrader:RealCopyExecutionEnabled` | API file only; fix-worker *would* read it if that file were its config |
| `.env.example` | `REAL_COPY_EXECUTION_ENABLED`, `MT5_LOGIN`, `CTRADER_FIX_*` | nobody in C# |

`.env.example` also contains live venue *identifiers* (Achiever `2027` / `57.128.141.65`, Starwave `9904` / `84.201.6.142`, Pepperstone `1369850`, `live.pepperstone.1369850`) and `MT5_STARWAVEFX_PROVISIONING_ENABLED=true`. Workers do not load it; A75 still wanted those identifiers off the committed example. Out of scope to rewrite here.

`CTraderFixOptions` defaults `TargetCompId = "cServer"` (issued-form spelling). A64 §13.2 said the stub defaulted `CSERVER` — that sentence is stale. `FixSimulationHarness` still emits `CSERVER` in simulated replies.

---

## 5. Hosted-service gap (binding A64 composition)

A64 §11: **delete template `Worker`**. Register the jobs below. Exact class names may vary; **jobs may not be one loop**.

### 5.1 mt5-worker — required jobs

| # | Required job | Present? | What exists instead |
|---|---|---|---|
| 1 | `Mt5ConnectHostedService` | **MISSING** | `Fake.ConnectAsync` sets `_connected = true` inside the 30 s tick |
| 2 | `Mt5GroupDiscoveryHostedService` | **MISSING** | Inlined `GetGroups` on Fake list; no 300 s resync; no tombstone; no `ingestion_events` |
| 3 | `Mt5AccountSyncHostedService` | **MISSING** | Inlined four fixture accounts; no snapshots table; no UserAdd/Update/Delete |
| 4 | `Mt5HistoryBackfillHostedService` | **MISSING** | 30-day host-clock `GetDeals`; no orders; no checkpoint; window misses fixture history |
| 5 | `Mt5LiveEventsHostedService` | **MISSING** | No queue, no deal-lag poll (2 s), no sweep (30 s), no pump gauge |
| 6 | `Mt5ReconciliationHostedService` | **MISSING** | — |
| 7 | `OutboxProcessorHostedService` (`mt5-source`) | **MISSING** | `outbox_events` table mapped, never written |
| — | Checkpoint store (A07 5.6) | **ENTITY ONLY** | `SyncCheckpoint` unused |
| — | Health + Serilog + OTel + `127.0.0.1:5081` | **MISSING** | — |

**0 / 7 hosted jobs.**

### 5.2 fix-worker — required jobs

| # | Required job | Present? | What exists instead |
|---|---|---|---|
| 1 | `FixOptionsValidationHostedService` | **MISSING** | Flat `GetValue` of one bool |
| 2 | `FixSessionLeaseHostedService` | **MISSING** | `FixSessionOwnership` unused |
| 3 | `CTraderQuoteSessionHostedService` | **MISSING** | DB status stamped `ReadyForMarketData` |
| 4 | `CTraderTradeSessionHostedService` | **MISSING** | DB status stamped `LoggedOn` |
| 5 | `FixSecurityListHostedService` | **MISSING** | No `35=x`; seeder quote has null instrument id |
| 6 | `FixStartupReconciliationHostedService` | **MISSING** | — |
| 7 | `FixPeriodicReconciliationHostedService` | **MISSING** | — |
| 8 | `UnknownExecutionRecoveryHostedService` | **MISSING** | Domain FSM exists; not hosted |
| 9 | `KillSwitchObserverHostedService` | **MISSING** | Seeded `KillSwitchMode.None`; unread |
| 10 | `OutboxProcessorHostedService` (`fix-dest`) | **MISSING** | — |
| — | Health `127.0.0.1:5082` | **MISSING** | — |

**0 / 10 hosted jobs.**

### 5.3 What A64 says these processes must never host

| Forbidden on mt5-worker | Current |
|---|---|
| QuickFIX / `35=D` | Absent (good) |
| Shadow fill simulation | Absent (good) |
| Risk approve-to-send | Absent (good) |
| Reconstruct/score on Manager pump | Reconstruct/score on the **same** timer as ingest (bad; there is no pump) |
| `CreateUser` / `DealerBalance` / `SendTrade` | Absent (good) |

| Forbidden on fix-worker | Current |
|---|---|
| `IMt5BrokerConnector` | Not resolved in `Worker`, but **Program seeds source deals via DemoSeeder** (bad) |
| Raw `mt5_deals` write path | Seeder writes them (bad) |
| Scoring / XGBoost | Absent (good) |
| Second TRADE “watch” session | Absent (good) |

---

## 6. Phase scorecards (architecture §67)

### 6.1 Phase 1 — Reliable MT5 ingestion (mt5-worker)

| Deliverable | Status | Evidence |
|---|---|---|
| Achiever connected | **FAIL** | Fake bool; no `57.128.141.65:443`, no login `2027`, no Manager slot |
| StarwaveFX connected | **FAIL** | Same Fake, different in-memory list; no `84.201.6.142:443` / `9904` |
| all groups discovered | **FAIL** | 4 fixture names only; not Manager `GroupTotal` |
| accounts synchronized | **FAIL** | 4 fixture logins; not ~5,000; no snapshots |
| history backfilled | **FAIL** | No checkpoint; host-clock 30 d; orders missing; June fixture outside window |
| live deals persisted | **FAIL** | No live path; SDK has no `PUMP_MODE_DEALS` and worker does not poll |
| idempotency proven | **PARTIAL / not on worker** | Store test exists; no backfill-restart / worker-host test |
| reconciliation working | **FAIL** | No reconcile job |

**Phase 1: 0 / 8.** Demo seeder + Fake is not an exit.

### 6.2 Phase 2–3 (hosted on this process today, illegally early)

Reconstruction and baseline scoring **exist as Domain services** and **run inside the mt5-worker tick** for four logins. That is not Phase 2/3 exit:

- No `source_symbol_mappings` drive (seeder adds `CanonicalInstrument` XAUUSD only).
- No outbox `mt5.deal_persisted` → `ReconstructionHandler` → `trade.completed` → `ScoreHandler`.
- Flags `Reconstruction:Enabled` / `Scoring:Enabled` unread.
- No worker-level tests of the hosted loop.

Treat Domain engines as **reusable libraries**. Treat the worker wiring as **wrong-phase coupling**.

### 6.3 Phase 4 — cTrader QUOTE (fix-worker)

| Deliverable | Status |
|---|---|
| SSL FIX quote session | **MISSING** |
| Logon / session health | **FAIL** — health is forged |
| Security List | **MISSING** |
| XAU instrument mapping | **MISSING** (`VenueInstrumentId` null) |
| Live XAU quote | **MISSING** (static seed) |
| Quote persistence / cache | Seed row only; no MD handler |
| Dashboard health | **UNSAFE** — would show healthy if DB shared |

**Phase 4: 0 / 7.**

### 6.4 Phase 7 — TRADE read / recon

| Deliverable | Status |
|---|---|
| SSL FIX trade session | **MISSING** |
| `OrderMassStatusRequest` | **MISSING** |
| `RequestForPositions` | **MISSING** |
| ExecutionReport parser | Harness only; not a session parser |
| PositionReport parser | **MISSING** |
| Reconciliation | **MISSING** |
| Real `NewOrderSingle` disabled | **VACUOUS PASS** (no builder/sender) |

**Phase 7: 0 / 6 implemented.**

Phase 5 shadow / Phase 8 execution engines exist as **pure Domain** types. fix-worker does not call them. Keep it that way until A64 §16 phase table.

---

## 7. Outbox and checkpoint gap (shared primitive)

### 7.1 `OutboxEvent` vs A64 §5.2

Current entity: `Id`, enum `Type` (`TradeCompleted`, `ScoreUpdate`, `ShadowCopyIntent`, `RiskCheckRequest`, `NotificationEvent`), `AggregateId`, `PayloadJson`, `OccurredAt`, `ProcessedAt`, `Attempts`, `LastError`, `CorrelationId`.

Missing vs A64: string `event_type` (incl. `mt5.*`), `schema_version`, `dedupe_key`, `available_at`, `locked_until`, `locked_by`, `broker_id`, `venue_id`, `source_login`, UNIQUE `(aggregate_type, aggregate_id, event_type, dedupe_key)`, claim index.

Index today: `ProcessedAt` only. No `SKIP LOCKED` claimer. No `IOutboxWriter`. No `IOutboxHandler`. Enum has **no ingest types**.

### 7.2 `SyncCheckpoint` vs A64 §8.7 / A07 §5.6

Current: `BrokerId`, required `Login`, `Stream` default `"deals"`, `LastTimestamp`, `LastTicket`, `UpdatedAt`. Unique `(BrokerId, Login, Stream)`.

Missing: nullable login (broker-wide streams), `cursor_from` / `cursor_to`, `payload_hash`, `status`, `error`, streams `orders_history` / `positions_snapshot` / `groups` / `accounts`.

**Law not implemented:** never persist `cursor_to` on a truncated/false `GetDeals`. There is no writer to violate it yet.

---

## 8. Safety

### 8.1 Live copy

| Question | Answer |
|---|---|
| Can either worker send `35=D` if started now? | **No** |
| Can `REAL_COPY_EXECUTION_ENABLED=true` in `.env` turn send on? | **No** (unread) |
| Can `CTrader:RealCopyExecutionEnabled=true` turn send on? | **No** (only flips a log line + a no-op ternary) |
| Are venue passwords in worker appsettings? | **No** |
| Is TLS the production default for FIX? | Unset on this host (`CTraderFixOptions.UseSsl` default true is unused) |
| Is TRADE single-owner? | Unset. Safe only while disconnected |
| `MaySendNewOrderSingle` conjunction (A64 §7.4) | **MISSING** |
| Persist-before-send | **MISSING** |

**SAFE_BY_ABSENCE** for money leaving the account. **FAIL** as an auditable control (A08’s most important finding still holds).

### 8.2 Unsafe *today* (not about sending)

| ID | Issue | Severity |
|---|---|---|
| U1 | fix-worker forges `LastInboundAt` + Ready/LoggedOn | **P0 ops** — dashboard / operators will believe QUOTE/TRADE are up |
| U2 | `DemoSeeder` on both workers + API | **P1** — source writes from dest process; three-way EnsureCreated |
| U3 | In-memory default hides the lie from API **unless** Postgres is configured; the moment `DATABASE_URL` is set, U1 becomes visible | **P0** on first real DB |
| U4 | `ReplacePositions` empty wipe every 30 s | **P1** ledger |
| U5 | Reconstruct/score on ingest stack | **P1** coupling; crash mid-rebuild leaves raw+scores inconsistent; no outbox retry |
| U6 | Wrong QuickFIX package sitting on the adapter project | **P2** trap for the first real session |
| U7 | `EnsureCreated` | **P1** schema |
| U8 | Hard-coded healthy brokers in `EfDashboardQueries.GetBrokers` | **P1** (API, but fed by worker seed) |
| U9 | Score history unbounded insert every 30 s × 4 logins | **P2** |

### 8.3 Correctly absent (keep)

- No `NewOrderSingle` builder.
- No MT5 `SendTrade` / `CreateUser` / `DealerBalance` on the worker.
- No Kafka / third copy-worker.
- No committed FIX/MT5 passwords in worker JSON.
- `RealCopyExecutionEnabled` default in the unused options class is `false`.
- Domain `RiskEngine` still requires `RealExecutionEnabled` + reconciled + kill switch before `AllowFixSend` (library only).

---

## 9. Tests

`tests/Unit` and `tests/Integration` **do not reference** the worker projects.

| Test | Proves | Does **not** prove |
|---|---|---|
| `SeedingAndStoreTests.Demo_seed_discovers_groups_reconstructs_and_scores` | Seeder + Fake + store + recon + score on in-memory DB | Hosted worker, checkpoints, live poll, two processes, Postgres |
| `SeedingAndStoreTests.Deal_upsert_is_idempotent` | Second deal ticket is a no-op | Same-transaction outbox; truncated page; restart |
| Domain unit tests (recon, score, risk, volume, symbol, execution FSM) | Library behavior | Worker composition |
| `UnitTest1` / `PlaceholderRemoved` | Assemblies load | Nothing |

A64 §15 required tests (`Checkpoint_NotAdvanced_WhenGetDealsFalse`, `Mt5BackfillRestartTests`, `OutboxProcessingTests`, `MaySendNewOrderSingle_FalseWhenFlagOff`, `FixLease_SecondInstance_CannotOwnTrade`, `RealExecutionDisabledIntegrationTests`, …) are **all missing**.

There is no `tests/Fix`, `tests/Replay`, or worker WebApplicationFactory.

---

## 10. Cross-process / deployment gap (A54 / A64 §3)

| Rule | Current |
|---|---|
| Two processes, one Postgres | Default: two (three with API) **separate** in-memory databases |
| mt5-worker = source authority | Also runs Phase 2/3 scoring; fix-worker seeds source |
| fix-worker = dest authority | Also seeds brokers/deals/scores |
| Replica: one TRADE owner | No lease; would be unsafe the moment a socket exists |
| Windows for `MT5_MODE=local` | No local mode at all |
| Health ports 5081 / 5082 | Missing |
| Docker | Missing |

---

## 11. Full classification (this slice)

| ID | Component | Class |
|---|---|---|
| W01 | `apps/mt5-worker` csproj / sln | EXISTS_AND_GOOD |
| W02 | `apps/fix-worker` csproj / sln | EXISTS_AND_GOOD |
| W03 | mt5 `Program.cs` | EXISTS_NEEDS_REFACTOR |
| W04 | fix `Program.cs` | EXISTS_NEEDS_REFACTOR |
| W05 | mt5 `Worker.cs` | DEPRECATED |
| W06 | fix `Worker.cs` | UNSAFE |
| W07 | worker `appsettings*` | EXISTS_NEEDS_REFACTOR (logging-only) |
| W08 | `AddTraderIntelligence` | EXISTS_NEEDS_REFACTOR (always Fake; in-memory default) |
| W09 | `DemoBrokerFactory` / `FakeMt5BrokerConnector` | EXISTS_AND_GOOD as fixture; DEPRECATED as host transport |
| W10 | `IMt5BrokerConnector` (DTO port) | EXISTS_NEEDS_REFACTOR (no orders/time/subscribe/false-GetDeals) |
| W11 | `IBrokerConnector` | DEPRECATED (duplicate unused port) |
| W12 | `DealIngestionService` | EXISTS_NEEDS_REFACTOR (no checkpoint/outbox/server time) |
| W13 | `ReconstructionScoringService` | EXISTS_AND_GOOD as library; UNSAFE on ingest stack in Phase 1 |
| W14 | `EfTradingStore` | EXISTS_NEEDS_REFACTOR |
| W15 | `TraderDbContext` mappings | EXISTS_NEEDS_REFACTOR (no migrations; outbox/checkpoint shapes short) |
| W16 | `DemoSeeder` on workers | UNSAFE |
| W17 | `OutboxEvent` + processor | MISSING (entity stub only) |
| W18 | `SyncCheckpoint` writer | MISSING |
| W19 | Seven / ten hosted jobs | MISSING |
| W20 | `Mt5BrokerOptions` bind | MISSING |
| W21 | `CTraderFixOptions` bind + four flags | MISSING |
| W22 | `FixSessionOwnership` | EXISTS_NEEDS_REFACTOR (in-memory; unused) |
| W23 | QuickFIX/n official engine | MISSING |
| W24 | `QuickFix.Net` 1.8.0 reference | DEPRECATED / UNSAFE pin |
| W25 | C++ `mt5-sdk` wired into worker | MISSING |
| W26 | Health / metrics / Serilog / OTel | MISSING |
| W27 | Worker host tests | MISSING |
| W28 | `MaySendNewOrderSingle` | MISSING |
| W29 | No `35=D` send path | EXISTS_AND_GOOD (absence) |
| W30 | `docs/architecture.md` “first useful version” wording | DEPRECATED as a progress claim |

---

## 12. Risks if someone “just keeps filling `Worker.cs`”

A64 §17 R1 is no longer hypothetical. The god loop is in tree.

| ID | Risk | Why P0/P1 |
|---|---|---|
| R1 | Keep growing `ExecuteAsync` | Reconnect, backfill of 5,000 accounts, live poll, and scoring will starve each other |
| R2 | Plug a real connector into current DI without checkpoints | 30-day host-clock scrape; gaps; no resume |
| R3 | Trust Fake `GetDeals` completeness | Real SDK pages; false/short page must not advance a cursor that does not exist yet |
| R4 | Point workers at Postgres as-is | U1 health lie + dual seeder + EnsureCreated become production |
| R5 | Implement `35=D` in this heartbeat | No lease, no recon, no persist-before-send, no flag conjunction |
| R6 | Live deals via `OnDealAdd` only | Zero live deals on this Manager SDK (A07 §4) |
| R7 | Filter groups to `MT5_GROUP_*` | Miss `demo\Maxmaster` and every non-yo group |
| R8 | `login >= 99000` broker heuristic | Breaks the day a second Starwave login is `< 99000` or Achiever uses a high login |
| R9 | Linux + `MT5_MODE=local` later | Native DLL will not load; remote sidecar not in this host |
| R10 | Treat seeder 4-account score as Phase 3 | First-useful-version greenwash |

---

## 13. Implementation sequence (audit only — do not implement in this task)

Respect A64 §16 / A28. This is **not** a license to code.

1. **Stop the health lie.** Until QUOTE/TRADE sockets exist, `fix_sessions.status` must stay `Disconnected` (or the heartbeat must be deleted). Do not write `LastInboundAt` without an inbound message.
2. **Remove `DemoSeeder` from worker `Program.cs`.** Seed is an API/dev concern, or a one-shot tool. fix-worker must not write `mt5_*`.
3. Replace `EnsureCreated` with a versioned migration once schema is real (out of this file’s edit scope).
4. Split mt5-worker into the seven A64 jobs. Delete `TraderIntelligence.Mt5Worker.Worker`.
5. Real `IOutboxWriter` + SKIP LOCKED processor (`mt5-source`). Persist raw + outbox in **one** transaction. Phase 1 handlers may no-op + log. **Do not** call `RebuildTraderAsync` from the ingest tick.
6. Checkpoint writer with the fail-closed law. Tests in A64 §15 before claiming backfill.
7. One `IMt5BrokerConnector` implementation behind a factory; bind `Mt5:Brokers`; refuse empty passwords; wire C++ local **or** HTTP remote. Kill `IBrokerConnector` or merge it.
8. Live deal **poll** (2 s active / 30 s sweep). Do not wait for deal pump.
9. fix-worker: bind four flags (`CTRADER_FIX_ENABLED`, `QUOTE`, `TRADE_SESSION`, `REAL_COPY_EXECUTION_ENABLED=false`). Implement `MaySendNewOrderSingle` **before** any initiator. Unit-test refuse.
10. Replace `QuickFix.Net` 1.8.0 with A35 pins. Independent QUOTE then TRADE. Lease. Security List. Quotes. Startup recon. Dest outbox.
11. Only then Phase 5 shadow handler and Phase 8 send path, flag still false.

Do not start Phase 8. Do not add Kafka. Do not share QUOTE/TRADE sequence files.

---

## 14. Acceptance (when this gap is closed)

Copy of A64 §19, still **unchecked**:

```text
[ ] Template / god-loop Worker gone from both hosts
[ ] mt5-worker runs six source jobs + source outbox processor
[ ] Achiever and StarwaveFX have independent connectors and checkpoints
[ ] GetDeals failure does not advance a checkpoint (test)
[ ] Raw + outbox commit atomically (test)
[ ] Crash after commit, before handle, retries once and is idempotent (test)
[ ] Poison row does not block ingest (test)
[ ] fix-worker does not stamp Ready/LoggedOn without a session
[ ] LastInboundAt moves only on real inbound
[ ] MaySendNewOrderSingle is false when flag off (test)
[ ] No 35=D builder until Phase 8 + §68 gates
[ ] Worker projects have host-level tests
```

---

## 15. Evidence appendix — file list read

Not modified:

- `D:\Prop\apps\mt5-worker\*` (Worker, Program, csproj, appsettings*, launchSettings)
- `D:\Prop\apps\fix-worker\*` (same set)
- `D:\Prop\apps\api\Program.cs`, `apps\api\appsettings.json`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs`
- `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`
- `D:\Prop\src\Domain\Entities\{OutboxEvent,SyncCheckpoint,FixSessionState,Broker,Mt5Deal,Mt5Account,CopyIntent,ExecutionIntent,KillSwitch}.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\.env.example`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\reports\swarm\20260818\{A07,A08,A28,A29,A35,A64,A75}_*.md`
- Architecture v2 §§67–68

Written:

- `D:\Prop\reports\swarm\20260818\B07_workers_gap.md` (this file)

---

## 16. Bottom line

`apps/mt5-worker` is a **30-second demo rescore timer** in front of an in-memory Fake broker. It does not connect to Achiever or StarwaveFX, does not checkpoint, does not outbox, does not poll live deals, and couples reconstruction/scoring onto the ingest stack.

`apps/fix-worker` is a **15-second session-row forger**. It does not speak FIX 4.4. It will make any shared database claim QUOTE is ready and TRADE is logged on. It still cannot send `NewOrderSingle` — that is safety by absence, not a gate.

A64’s required composition is **0 / 7** on the source host and **0 / 10** on the destination host. Phase 1 / 4 / 7 are **not started** as measured systems. Delete both `Worker` classes when the real hosted services land. Do not grow them.
