# D31 — `apps/mt5-worker` Worker.cs + Program.cs (measured host)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D31_mt5w.md` |
| Agent | D31 (mt5-worker host reconfirm) |
| Date | 2026-08-18 |
| Snapshot UTC | 2026-08-18T08:04:53Z |
| Assigned | Read `apps/mt5-worker` `Worker.cs` and `Program.cs`. Write this file. |
| Product source modified | **No** |
| Test source modified | **No** |
| Method | Full read of the host pair + csproj/appsettings/launchSettings. Re-read the types they actually resolve (`AddTraderIntelligence`, `DealIngestionService`, `ReconstructionScoringService`, `DemoSeeder`, `FakeMt5BrokerConnector`, `EfTradingStore`). SHA-256 of host and adjacent files. Grep of the worker project for send verbs. Tests and compose checked for any `Mt5Worker` host. Independent of A07’s *template-loop* snapshot; hashes match B07/C07. |

**Honesty rule:** a 30 s Fake `SyncBrokerAsync` is not Achiever. `EnsureCreated` + `DemoSeeder` is not Phase 1. A log line that says “Execution copy is not performed here” is true, and is **SAFE_BY_ABSENCE**, not an implemented gate. A07’s “1-second `Worker running at:` template” is **stale**. Do not recycle it as current evidence.

---

## 0. Verdict

**FAIL as a collector. SAFE_BY_ABSENCE as a sender.**

`apps/mt5-worker` is a thin .NET 8 Worker host that:

1. Boots with `AddTraderIntelligence` (always two `FakeMt5BrokerConnector` singletons).
2. Calls `Database.EnsureCreatedAsync` then `DemoSeeder.SeedAsync` (same seed as API and fix-worker).
3. Registers **one** hosted service: `TraderIntelligence.Mt5Worker.Worker`.
4. Every **30 seconds** calls `DealIngestionService.SyncBrokerAsync` for `ACHIEVER` and `STARWAVEFX` over a **host-clock** window `[UtcNow-30d, UtcNow+1m]`, then `ReconstructionScoringService.RebuildTraderAsync` for the four hard-coded logins `{10001, 10002, 10003, 99001}`.

That is a **demo rescore timer**, not A64’s seven source jobs.

| Score | Measured |
|---|---|
| A64 §8 required hosted jobs | **0 / 7** (one `ExecuteAsync` god loop) |
| Architecture §67 Phase 1 deliverables | **0 / 8** production (demo Fake only) |
| Live Achiever / StarwaveFX sockets | **0** |
| Checkpoints read or written | **0** (`SyncCheckpoint` unused by this host) |
| Outbox produce / claim | **0** |
| Worker-host tests | **0** |
| Health port `127.0.0.1:5081` / Serilog / OTel | **MISSING** |
| `NewOrderSingle` / MT5 `SendTrade` / `CreateUser` / `DealerBalance` | **None** — `SAFE_BY_ABSENCE` |
| Fixture deals inside the 30-day worker window (clock 2026-08-18) | **0 of 18** (all stamped 2026-06-01 / 2026-06-02) |

`docs/architecture.md` currently says workers are an “ingest/score loop.” That sentence describes **this file pair**. It is not Phase 1 acceptance.

Classification of *this* host:

| Component | Class |
|---|---|
| `TraderIntelligence.Mt5Worker.csproj` / sln membership | `EXISTS_AND_GOOD` (scaffold) |
| `Program.cs` | `EXISTS_NEEDS_REFACTOR` |
| `Worker.cs` god loop | `DEPRECATED` — delete when A64 jobs register |
| `appsettings*` / `launchSettings` | `EXISTS_NEEDS_REFACTOR` (logging only) |
| `DemoSeeder` on this process | `UNSAFE` if a shared Postgres is ever configured |
| Fake connectors as the only transport | `DEPRECATED` as production; `EXISTS_AND_GOOD` as a fixture |
| Live send path | `MISSING` — **SAFE_BY_ABSENCE** |

Do **not** treat this process as a Manager collector, a quote engine, or an execution worker.

---

## 1. Inventory (source of truth; exclude `bin/` / `obj/`)

Measured 2026-08-18T08:04:53Z. Hashes identical to B07/C07 for the host files.

| Path | Bytes | Non-blank lines | SHA-256 | Role |
|---|---:|---:|---|---|
| `D:\Prop\apps\mt5-worker\Worker.cs` | 1882 | 40 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s ingest + reconstruct + score |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | 19 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | DI + `EnsureCreated` + `DemoSeeder` + one hosted service |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | 840 | 17 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | `Microsoft.NET.Sdk.Worker`, net8.0 |
| `D:\Prop\apps\mt5-worker\appsettings.json` | 137 | 8 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging levels only |
| `D:\Prop\apps\mt5-worker\appsettings.Development.json` | 137 | 8 | same as Production | Identical |
| `D:\Prop\apps\mt5-worker\Properties\launchSettings.json` | 296 | 12 | `8E2A7548E3EBFF12FDB3E078E06ADA944E3ABB83BA8F9128746542CAA8AA3E36` | `DOTNET_ENVIRONMENT=Development` only |

`UserSecretsId` = `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1`. Unused. No secrets are loaded.

csproj packages: `Microsoft.Extensions.Hosting` **8.0.1** only.

Project references:

- `src\Domain\TraderIntelligence.Domain.csproj`
- `src\Application\TraderIntelligence.Application.csproj`
- `src\Infrastructure\TraderIntelligence.Infrastructure.csproj`
- `src\Mt5\TraderIntelligence.Mt5.csproj`

No Dockerfile. No compose service (`docker-compose.yml` line 30: “Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.”). That isolation is correct for a **future** native Manager; it does not make *this* C# loop a collector.

`tests/Unit` and `tests/Integration` have **zero** references to `TraderIntelligence.Mt5Worker` / `apps/mt5-worker`.

Adjacent types this host actually resolves (read, not owned by this project):

| Type | Path | SHA-256 |
|---|---|---|
| `AddTraderIntelligence` | `src/Infrastructure/DependencyInjection.cs` | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| `DealIngestionService` + `ReconstructionScoringService` | `src/Application/Ingestion/DealIngestionService.cs` | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` |
| `DemoSeeder` | `src/Infrastructure/Seeding/DemoSeeder.cs` | `A641649125EE9D1041FF91DCA08980BD44588FE18FAFE7491D3880962ED1FE20` |
| `EfTradingStore` | `src/Infrastructure/Persistence/EfTradingStore.cs` | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` |
| `FakeMt5BrokerConnector` + `DemoBrokerFactory` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` |
| `IMt5BrokerConnector` | `src/Application/Contracts/Mt5Contracts.cs` | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` |
| `BrokerCodes` | `src/Domain/Brokers/BrokerCodes.cs` | `CF4165CE7A317B0282B9149B078E5D1E630F72524190AB20E0952BECBBAE1182` |

---

## 2. `Program.cs` — what the process actually starts

Entire file (22 physical lines):

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

`apps/fix-worker/Program.cs` is the same sequence (namespace only). `apps/api/Program.cs` also `EnsureCreated` + `DemoSeeder`. Three hosts, one seeder, **no** versioned migration.

### 2.1 What boot does

| Step | Measured behavior |
|---|---|
| `Host.CreateApplicationBuilder` | Generic host. No Kestrel health port. No Serilog. No OTel. |
| `AddTraderIntelligence` | See §2.2. Always Fake brokers. In-memory DB unless an operator injects a DSN. |
| `AddHostedService<Worker>` | The **only** `IHostedService`. A64’s seven jobs are not registered. |
| `EnsureCreatedAsync` | Creates the current EF model in whatever provider was registered. **Violates** architecture §72.3 (versioned migrations). Schema will not upgrade. |
| `DemoSeeder.SeedAsync` | Short-circuits on `Brokers.Any()`. First process to win writes brokers, XAU canonical, FIX QUOTE=`ReadyForMarketData`, TRADE=`LoggedOn`, static dest quote `2399.45`/`2399.85`, kill switch `None`, then `SyncBrokerAsync` with window **`2026-01-01 … 2026-12-31`**, then scores the four logins. |
| `CancellationToken.None` on seed | Host shutdown during seed is not cooperative. |
| `host.Run()` | Blocks. No `DisconnectAsync` on stop (that lives only inside the unused Fake API). |

### 2.2 DI this host inherits (always demo)

```17:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
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
        // scoped store + DealIngestionService + ReconstructionScoringService
```

Consequences for **this** host:

1. Worker `appsettings` have **no** connection string → default launch is `UseInMemoryDatabase("trader-intelligence")`. That name is **per process**. API, mt5-worker, and fix-worker are three isolated demo worlds unless an operator injects Postgres. Dashboard cannot see this worker’s writes in the default launch.
2. Even when `DATABASE_URL` is a real DSN, connectors stay **in-memory fixtures**. There is no `IMt5BrokerConnectorFactory`, no `Mt5:Brokers` bind, no password refuse-to-start, no HTTP client, no native Manager.
3. `Mt5BrokerOptions` is never `Configure`/`Bind`/`ValidateOnStart`.
4. C++ `mt5-sdk` (`MT5Manager`, `MT5Pool`, `MT5Watchdog`, `MT5HttpClient`) is **not referenced**.

### 2.3 What `DemoSeeder` writes that a source worker must not own

A64 §3.1 / §8.9: mt5-worker is **source authority**. It must not invent destination session health.

The seeder this `Program.cs` calls writes:

- `Broker` rows with live-looking hosts (`57.128.141.65:443` login `2027`; `84.201.6.142:443` login `9904`). Fake never reads those fields.
- `FixSessionState` QUOTE `ReadyForMarketData` and TRADE `LoggedOn` against `live-us-eqx-01.p.c-trader.com` / `live.pepperstone.1369850` / `cServer`.
- A static `DestinationQuoteSnapshot` (`VenueInstrumentId = null`).
- `KillSwitchMode.None`.
- 18 canned XAUUSD deals via a **year-wide** window (this is the only path that actually lands the June fixtures).

If two of {api, mt5-worker, fix-worker} share Postgres, first boot wins the seed; the others no-op on `Brokers.Any()`. The source host still stamped FIX health if it won the race.

### 2.4 Absent from `Program.cs` (all required for Phase 1)

- Options bind / broker password refuse
- Serilog / OpenTelemetry
- EF migrations (`Migrate()`)
- `IMt5BrokerConnectorFactory` / real transport
- Any of the seven A64 hosted services
- Health checks / `mt5_connected` gauge
- `ICheckpointStore` / `IOutboxWriter` registration
- Shutdown `DisconnectAsync`

---

## 3. `Worker.cs` — the 30-second god loop

Entire implementation (45 physical lines):

```1:45:D:\Prop\apps\mt5-worker\Worker.cs
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;

namespace TraderIntelligence.Mt5Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceScopeFactory _scopes;

    public Worker(ILogger<Worker> logger, IServiceScopeFactory scopes)
    {
        _logger = logger;
        _scopes = scopes;
    }

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
}
```

This is **not** the stock `dotnet new worker` 1 s log (A07). It is still **one** `ExecuteAsync`. A64 §8: “jobs may not be collapsed into one `ExecuteAsync`.”

### 3.1 Call graph (no send)

```text
Worker.ExecuteAsync                                   // 30 s tick
  ├─ DealIngestionService.SyncBrokerAsync("ACHIEVER")
  │    ├─ IBrokerRegistry.Get
  │    ├─ IMt5BrokerConnector.ConnectAsync            // Fake: _connected = true
  │    ├─ GetGroupsAsync → UpsertGroupAsync           // 3 fixture names
  │    └─ GetAccountsAsync(null)
  │         per account:
  │           UpsertAccountAsync
  │           GetDealsAsync(login, now-30d, now+1m)   // host clock; 0 fixture hits
  │           UpsertDealAsync (insert-only)
  │           GetPositionsAsync → ReplacePositions    // factory list is empty → wipe
  ├─ SyncBrokerAsync("STARWAVEFX")                    // 1 group, 1 account; same window
  └─ ReconstructionScoringService.RebuildTraderAsync × 4
       LoadDeals → TradeReconstructor → BaselineScorer → UpsertScore
       (+ TraderScoreHistory row every cycle)
```

`IMt5BrokerConnector` verbs used: `ConnectAsync`, `GetGroupsAsync`, `GetAccountsAsync`, `GetDealsAsync`, `GetPositionsAsync`.

`IMt5BrokerConnector` verbs **not** used: `DisconnectAsync`, `IsConnectedAsync`.

`IMt5BrokerConnector` verbs that **do not exist** (so this worker cannot send): `Send*`, `Place*`, `NewOrder*`, `Dealer*`, `CreateUser`, `GetOrders`, `GetServerTime`, `Subscribe*`.

The unused twin port `IBrokerConnector` (`ulong` logins, Domain entities, `GetServerTime`, `SubscribeEventsAsync`) has **zero** implementers and is not resolved here.

### 3.2 Hard-coded universe

| Constant | Value | Why it is a gap |
|---|---|---|
| Brokers | `BrokerCodes.Achiever` (`"ACHIEVER"`), `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`) | Not discovered from config / `IBrokerRegistry.All()` |
| Window | `UtcNow.AddDays(-30)` … `UtcNow.AddMinutes(1)` | Host clock, not `GetServerTime`. A64 §4.1 forbidden for history |
| Period | 30 s | Not a reconnect backoff, not a deal-lag poll, not a reconcile window |
| Logins scored | `{10001, 10002, 10003, 99001}` | Not “every synchronized login” |
| Broker heuristic | `login >= 99000 ? STARWAVEFX : ACHIEVER` | Breaks the day a second Starwave login is `< 99000` or Achiever uses a high login |
| Fixture deal epoch | `2026-06-01T08:00:00Z` (+ hours / +1 day) | Outside the 30-day window on this snapshot date |

Demo factory contents the loop *would* see if the window included them:

| Broker | Groups | Accounts | Deals | Positions |
|---|---:|---:|---:|---:|
| ACHIEVER | 3 (`demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`) | 10001, 10002, 10003 | 12 (6 closed XAU round-trips) | 0 |
| STARWAVEFX | 1 (`real\standard`) | 99001 | 6 (3 closed XAU round-trips) | 0 |

Login `10003` has **no** deals. It is still rescored every 30 s.

### 3.3 The 30-day window misses the fixtures

`FakeMt5BrokerConnector.GetDealsAsync` filters `d.Login == login && d.Time >= from && d.Time <= to`.

Clock of this report: **2026-08-18**. Worker `from` ≈ **2026-07-19**. All 18 fixture deals sit on **2026-06-01 / 2026-06-02**.

After `DemoSeeder` (year-wide window) has already upserted those deals, each 30 s tick:

- re-upserts 4 groups / 4 accounts (`LastSyncedAt` moves),
- inserts **zero** new deals,
- `ReplacePositions` with an **empty** list → **deletes** `mt5_positions` for those four logins every cycle,
- rebuilds scores from whatever is already in the store,
- appends **4** `TraderScoreHistory` rows every 30 s.

That is not backfill. It is a score-recompute timer on seeder rows, plus a position-book wipe.

### 3.4 Law vs this loop

| Law | What `Worker.cs` does |
|---|---|
| A64: do not collapse jobs into one `ExecuteAsync` | Connect, discovery, accounts, history, reconstruction, scoring share one 30 s tick |
| Server time, not host clock (A64 §4.1) | `DateTimeOffset.UtcNow ± 30d` |
| Checkpoint + page-to-EOF (A07 §5.4 / A64 §8.4) | No `sync_checkpoints` I/O; Fake returns one in-memory page and cannot return `false` |
| Truncated `GetDeals` → do not advance | Nothing to advance |
| Outbox same commit as raw (A64 §5 / §12) | `EfTradingStore` `SaveChanges` **per** group / account / deal / position; no outbox row |
| Do not reconstruct on the ingest stack (A64 §3.3, §8.4) | `RebuildTraderAsync` immediately after `SyncBrokerAsync` |
| `Reconstruction:Enabled` / `Scoring:Enabled` default false in Phase 1 | Always on; flags unread |
| Discover all Manager groups | Discovers whatever the Fake list contains |
| Score every synchronized login | Hard-coded four logins |
| Live events + deal-lag poll | No `Subscribe*`, no 2 s poll, no pump/no-pump gauge |
| Periodic reconcile | Absent |
| Orders history | Port has **no** `GetOrders` |
| `GetServerTime` | Only on unused `IBrokerConnector` |
| Watchdog 5→60 s | `ConnectAsync` sets a bool |
| Do not invent source trades | Honored **only** because Fake deals are fixtures. The 30-day window **cannot see them** |
| Disconnect on shutdown | Loop just cancels; no `DisconnectAsync` |
| Execution copy | Log line is true. There is no send function to refuse |

The catch filter `ex is not OperationCanceledException` plus “No source trades invented” is honest about invention. It still swallows every other failure and retries in 30 s with **no** backoff policy (`mt5.local.reconnect` / `mt5.history.page` from A64 §6 are unimplemented).

---

## 4. Store behavior the worker inherits

`EfTradingStore` (not in this project; the loop’s only persistence):

| Method | Behavior the 30 s tick relies on | Hazard |
|---|---|---|
| `UpsertDealAsync` | Identity `(broker_id, deal_ticket)`; insert-only | Good dedup. A later correction **cannot** land (no revision / `ingestion_events`) |
| `ReplacePositionsAsync` | Delete-all-for-login then insert | Empty Fake list **erases** the book every cycle. Fail-open vs A64 §7.1 |
| `UpsertGroupAsync` (update) | Writes `Currency` + `LastSyncedAt` only | Other group fields frozen after first insert |
| `UpsertAccountAsync` (update) | Group / balance / equity / `LastSyncedAt` | Margin / profit / leverage drift ignored on update |
| `ReplaceReconstructedAsync` | Delete-and-insert per login | Crash mid-rebuild leaves raw + reconstructed inconsistent; no outbox retry |
| `UpsertScoreAsync` | Upsert-by-(broker, login); **always** inserts `TraderScoreHistory` | Unbounded history: 4 rows × 2/min × 24 h ≈ **11 520** rows/day |
| Checkpoint / outbox | **Not on the interface** | `SyncCheckpoint` and `OutboxEvent` exist as entities; this worker never touches them |

`ITradingStore` has no `GetOrders`, no snapshots table writer, no `ingestion_events`.

---

## 5. A64 hosted-job gap (this process only)

A64 §8: **delete** template / god-loop `Worker`. Register the jobs below. Exact class names may vary; **jobs may not be one loop**.

| # | Required job | Present? | What exists instead |
|---|---|---|---|
| 1 | `Mt5ConnectHostedService` | **MISSING** | `Fake.ConnectAsync` sets `_connected = true` inside the 30 s tick |
| 2 | `Mt5GroupDiscoveryHostedService` | **MISSING** | Inlined `GetGroups` on Fake list; no 300 s resync; no tombstone; no `ingestion_events` |
| 3 | `Mt5AccountSyncHostedService` | **MISSING** | Inlined four fixture accounts; no snapshots; no UserAdd/Update/Delete |
| 4 | `Mt5HistoryBackfillHostedService` | **MISSING** | 30-day host-clock `GetDeals`; no orders; no checkpoint; window misses fixture history |
| 5 | `Mt5LiveEventsHostedService` | **MISSING** | No queue, no deal-lag poll (2 s), no sweep (30 s), no pump gauge |
| 6 | `Mt5ReconciliationHostedService` | **MISSING** | — |
| 7 | `OutboxProcessorHostedService` (`mt5-source`) | **MISSING** | `outbox_events` mapped, never written |
| — | Checkpoint store (A07 5.6 / A64 §8.7) | **ENTITY ONLY** | `SyncCheckpoint` unused |
| — | Health + Serilog + OTel + `127.0.0.1:5081` | **MISSING** | — |

**0 / 7 hosted jobs.**

What A64 says this process must **never** host — current:

| Forbidden on mt5-worker | Current |
|---|---|
| QuickFIX / `35=D` | Absent (**good**) |
| Shadow fill simulation | Absent (**good**) |
| Risk approve-to-send | Absent (**good**) |
| Reconstruct/score on Manager pump | Reconstruct/score on the **same** timer as ingest (**bad**; there is no pump) |
| `CreateUser` / `DealerBalance` / `SendTrade` | Absent (**good**) |

Grep of `D:\Prop\apps\mt5-worker` for `SendTrade`, `NewOrderSingle`, `35=D`, `OrderSend`, `DealerSend`, `MaySendNewOrderSingle`, `CreateUser`, `DealerBalance`: **0 hits**.

---

## 6. Phase 1 scorecard (architecture §67, this host)

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

Reconstruction and baseline scoring **exist as Domain services** and **run inside this tick** for four logins. That is not Phase 2/3 exit:

- No `source_symbol_mappings` drive (seeder adds `CanonicalInstrument` XAUUSD only).
- No outbox `mt5.deal_persisted` → `ReconstructionHandler` → `trade.completed` → `ScoreHandler`.
- Flags unread.
- No worker-level tests of the hosted loop.

Treat Domain engines as **reusable libraries**. Treat this wiring as **wrong-phase coupling**.

---

## 7. Safety

### 7.1 Live money

| Question | Answer |
|---|---|
| Can this worker send FIX `35=D` if started now? | **No** |
| Can this worker call MT5 `SendTrade` / `OrderSend` / `DealerSend`? | **No** |
| Can `REAL_COPY_EXECUTION_ENABLED=true` turn send on? | **No** (unread by this host) |
| Are venue passwords in worker appsettings? | **No** |
| Is `MaySendNewOrderSingle` implemented here? | **No** |
| Persist-before-send? | N/A (nothing to send) |

**SAFE_BY_ABSENCE** for money leaving an account. **FAIL** as an auditable control. The log line “Execution copy is not performed here” is true because there is no send function, not because a conjunction refused a send.

### 7.2 Unsafe *today* (not about sending)

| ID | Issue | Severity |
|---|---|---|
| U1 | `DemoSeeder` on worker boot writes FIX Ready/LoggedOn + dest quote | **P1** — source process stamps destination health |
| U2 | `EnsureCreated` instead of a versioned migration | **P1** schema |
| U3 | Default in-memory DB hides worker writes from API; shared Postgres would surface U1 | **P0** on first real DSN |
| U4 | `ReplacePositions` empty wipe every 30 s | **P1** ledger |
| U5 | Reconstruct/score on the ingest stack | **P1** coupling; crash mid-rebuild; no outbox retry |
| U6 | Score history unbounded insert every 30 s × 4 logins | **P2** |
| U7 | `login >= 99000` broker heuristic | **P2** identity |

### 7.3 Correctly absent (keep)

- No `NewOrderSingle` builder.
- No MT5 `SendTrade` / `CreateUser` / `DealerBalance` on this worker.
- No Kafka / third copy-worker.
- No committed FIX/MT5 passwords in worker JSON.
- C++ `CacheExecutedDeal` / `SendTrade` not referenced.

---

## 8. Tests and deployment

| Check | Measured |
|---|---|
| Host-level test of `TraderIntelligence.Mt5Worker` | **0** |
| `Checkpoint_NotAdvanced_WhenGetDealsFalse` | **MISSING** |
| `Mt5BackfillRestartTests` | **MISSING** |
| `OutboxProcessingTests` | **MISSING** |
| Integration `SeedingAndStoreTests` | Proves seeder + Fake + store; **does not** host this `Worker` |
| `docker-compose.yml` service | **None** (comment only; correct OS split, not a collector) |
| sln membership | **Yes** — `{31DFD31A-7E82-4968-912F-397C3E7DEE61}` |

Default topology vs A64 §3:

| Rule | Current |
|---|---|
| Two processes, one Postgres | Default: separate in-memory DBs |
| mt5-worker = source authority | Also runs Phase 2/3 scoring; also seeds FIX rows |
| Windows for `MT5_MODE=local` | No local mode at all |
| Health port 5081 | Missing |

---

## 9. Stale snapshots (do not quote as current)

| File | What it said | Current truth |
|---|---|---|
| `A07_mt5_worker_audit.md` | `while` + `Task.Delay(1000)` + `"Worker running at:"`; `Program.cs` is `AddHostedService` only; Domain/Application empty `Class1` | **False now.** This `Worker` is the 30 s Fake ingest/score loop. Layers are real types. Phase 1 is **still FAIL**. |
| `A64_worker_pipelines.md` §2 “measured current state” | Template 1 s loop; no connector | **Stale paragraph.** The **job list in A64 §§8–9 remains the binding target.** |
| `docs/architecture.md` “ingest/score loop” | Sounds like progress | Describes demo theater. Not §67 Phase 1. |

B07 / C07 hashes for `Worker.cs` / `Program.cs` **still match** this snapshot. D31 does not reopen the fix-worker; C07 remains the send-off pin for both hosts.

---

## 10. Risks if someone “just keeps filling `Worker.cs`”

A64 §17 R1 is no longer hypothetical. The god loop is in tree.

| ID | Risk | Why P0/P1 |
|---|---|---|
| R1 | Keep growing `ExecuteAsync` | Reconnect, backfill of 5,000 accounts, live poll, and scoring will starve each other |
| R2 | Plug a real connector into current DI without checkpoints | 30-day host-clock scrape; gaps; no resume |
| R3 | Trust Fake `GetDeals` completeness | Real SDK pages; false/short page must not advance a cursor that does not exist yet |
| R4 | Point this worker at Postgres as-is | Dual/triple seeder + `EnsureCreated` + empty position wipe become production |
| R5 | Treat four-login rescore as Phase 3 | First-useful-version greenwash |
| R6 | Live deals via `OnDealAdd` only | Zero live deals on this Manager SDK (A07 §4) |
| R7 | Filter groups to `MT5_GROUP_*` | Miss `demo\Maxmaster` and every non-yo group |
| R8 | Linux + `MT5_MODE=local` later | Native DLL will not load; remote sidecar not in this host |

---

## 11. Implementation sequence (audit only — do not implement in this task)

Respect A64 §16 / A28. This is **not** a license to code.

1. **Remove `DemoSeeder` from this `Program.cs`.** Seed is an API/dev concern or a one-shot tool. This process must not write `fix_sessions` or dest quotes.
2. Replace `EnsureCreated` with a versioned migration once schema is real (out of this file’s edit scope).
3. Split into the seven A64 jobs. Delete `TraderIntelligence.Mt5Worker.Worker`.
4. Real `IOutboxWriter` + SKIP LOCKED processor (`mt5-source`). Persist raw + outbox in **one** transaction. Phase 1 handlers may no-op + log. **Do not** call `RebuildTraderAsync` from the ingest tick.
5. Checkpoint writer with the fail-closed law. Tests in A64 §15 before claiming backfill.
6. One `IMt5BrokerConnector` implementation behind a factory; bind `Mt5:Brokers`; refuse empty passwords; wire C++ local **or** HTTP remote. Kill `IBrokerConnector` or merge it.
7. Live deal **poll** (2 s active / 30 s sweep). Do not wait for deal pump.
8. Health `127.0.0.1:5081` + Serilog + OTel. Host-level tests.

Do not start Phase 8. Do not add Kafka. Do not send FIX from this process.

---

## 12. Acceptance (when this gap is closed)

```text
[ ] God-loop Worker gone from apps/mt5-worker
[ ] mt5-worker runs six source jobs + source outbox processor
[ ] Achiever and StarwaveFX have independent connectors and checkpoints
[ ] GetDeals failure does not advance a checkpoint (test)
[ ] Raw + outbox commit atomically (test)
[ ] Crash after commit, before handle, retries once and is idempotent (test)
[ ] Poison row does not block ingest (test)
[ ] DemoSeeder is not invoked from this Program.cs
[ ] No 35=D / SendTrade path on this host
[ ] Worker project has host-level tests
```

All unchecked as of 2026-08-18T08:04:53Z.

---

## 13. Evidence appendix — files read

Not modified:

- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\appsettings.Development.json`
- `D:\Prop\apps\mt5-worker\Properties\launchSettings.json`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Entities\SyncCheckpoint.cs`
- `D:\Prop\apps\api\Program.cs` (seed twin only)
- `D:\Prop\docker-compose.yml`
- `D:\Prop\docs\architecture.md`
- `D:\Prop\tests\**` (no `Mt5Worker` hits)
- Siblings: `A07`, `A64`, `B07`, `C07`, `C42`

Written:

- `D:\Prop\reports\swarm\20260818\D31_mt5w.md` (this file)

---

## 14. Bottom line

`apps/mt5-worker` `Program.cs` is **EnsureCreated + DemoSeeder + one hosted service**.

`apps/mt5-worker` `Worker.cs` is a **30-second Fake ingest/rescore timer** over four hard-coded logins. On 2026-08-18 its host-clock window **cannot even re-fetch** the June 2026 fixture deals. It still wipes positions and appends score history.

It does **not** connect to Achiever or StarwaveFX, does **not** checkpoint, does **not** outbox, does **not** poll live deals, and couples reconstruction/scoring onto the ingest stack.

It **cannot** send `NewOrderSingle` or MT5 `SendTrade`. That is safety by absence, not a gate.

A64 required composition on this host is **0 / 7**. Phase 1 is **0 / 8**. Delete `Worker` when the real hosted services land. Do not grow it.

**End of D31.** Product source was not modified.
