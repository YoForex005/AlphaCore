# D07 — `mt5-worker` and `fix-worker` census

| Field | Value |
|---|---|
| Agent | D07 (workers census) |
| Date | 2026-08-18 |
| Assigned | Inventory `apps/mt5-worker` and `apps/fix-worker`. Write this file. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\D07_workers_census.md` |
| Product source modified | **No** |
| Test source modified | **No** |
| Snapshot time (UTC) | 2026-08-18T08:04:48Z (`fix-worker/Worker.cs` last write) |
| Method | Full read of both hosts + hashes + `deps.json` + DI + seeder + connectors + compose + sln + tests. A64 used as **target** pipeline catalog, not as measured state. |
| Sibling measured files | A07/A08 (stale 1 s template), B07/C07 (stale on fix-worker heartbeat **lie**; hashes below), A64 (target), A54/A65/A77/A105 (deploy/health/DLL) |
| Honesty rule | A Fake `ConnectAsync` is not Achiever. A `Disconnected` row is not a FIX session. Absence of `35=D` is `SAFE_BY_ABSENCE`, not an implemented gate. |

---

## 0. Verdict

Two .NET 8 Worker SDK hosts exist, are in `Mt5TraderIntelligence.sln`, and compile. They are **demo loops**, not the A64 authority-separated pipelines.

| Process | Path | Measured role today |
|---|---|---|
| `TraderIntelligence.Mt5Worker` | `D:\Prop\apps\mt5-worker` | 30 s Fake ingest + reconstruct + score for **four hard-coded logins** |
| `TraderIntelligence.FixWorker` | `D:\Prop\apps\fix-worker` | 15 s UPDATE of `fix_sessions` to **`Disconnected`** + error text. **No socket.** |

| Question | Measured answer |
|---|---|
| Live MT5 Manager / HTTP bridge? | **No.** Only `FakeMt5BrokerConnector`. |
| Live cTrader FIX QUOTE/TRADE? | **No.** `Fix.CTrader` is an unused project reference. |
| `NewOrderSingle` / `35=D` / MT5 `SendTrade` possible if started? | **No** (`SAFE_BY_ABSENCE`). |
| A64 hosted jobs present? | **0 / 7** MT5, **0 / 10** FIX. One `Worker` each. |
| Health `/health` `/ready` on workers? | **Missing.** No Kestrel. Spec ports `5081`/`5082` unused. |
| Outbox produce + claim? | **Missing.** Entity exists; workers never touch `outbox_events`. |
| Checkpoints? | **Missing.** Entity exists; unused. |
| Worker host tests? | **0.** Neither test project references the worker csprojs. |
| In docker-compose? | **No.** Comment forbids Linux-container of native MT5. |
| Native Manager DLLs next to exe? | **0.** |
| Default store | EF **InMemory** `"trader-intelligence"` (empty connection string). |

Classification:

| Component | Class |
|---|---|
| Project + sln membership (both) | `EXISTS_AND_GOOD` (scaffold) |
| `Program.cs` (both) | `EXISTS_NEEDS_REFACTOR` |
| `mt5-worker/Worker.cs` god loop | `DEPRECATED` vs A64 §8 |
| `fix-worker/Worker.cs` (current) | `EXISTS_NEEDS_REFACTOR` — honest `Disconnected` stamp; still not a session |
| Fake connectors as sole transport | `DEPRECATED` as production; `EXISTS_AND_GOOD` as fixture |
| `DemoSeeder` on worker boot | `UNSAFE` if Postgres is shared (forges QUOTE Ready / TRADE LoggedOn until fix-worker overwrites) |
| Live send path | `MISSING` — `SAFE_BY_ABSENCE` |
| Health / Serilog / OTel / Redis lease | `MISSING` |

Do **not** treat either process as a collector, quote engine, or execution worker.

---

## 1. Product-file inventory (exclude `bin/` / `obj/`)

Six files per host. No `Hosting/`, `Configuration/`, `Health/`, Dockerfile, README, or tests under either folder.

### 1.1 `D:\Prop\apps\mt5-worker`

| Path | Bytes | Lines | EOL | SHA-256 | Role |
|---|---:|---:|---|---|---|
| `Program.cs` | 859 | 22 | LF | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | Host + `EnsureCreated` + `DemoSeeder` + one hosted service |
| `Worker.cs` | 1882 | 45 | LF | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s Fake ingest + score |
| `TraderIntelligence.Mt5Worker.csproj` | 840 | 20 | CRLF | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | net8 Worker SDK |
| `appsettings.json` | 137 | 8 | CRLF | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Logging only |
| `appsettings.Development.json` | 137 | 8 | CRLF | same as Production | Logging only |
| `Properties/launchSettings.json` | 296 | 12 | CRLF | `8E2A7548E3EBFF12FDB3E078E06ADA944E3ABB83BA8F9128746542CAA8AA3E36` | `DOTNET_ENVIRONMENT=Development` |

`UserSecretsId` = `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1`. Unused. No `secrets.json`.

### 1.2 `D:\Prop\apps\fix-worker`

| Path | Bytes | Lines | EOL | SHA-256 | Role |
|---|---:|---:|---|---|---|
| `Program.cs` | 859 | 22 | LF | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | Same boot as mt5-worker (namespace only) |
| `Worker.cs` | **2093** | 51 | LF | **`92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2`** | 15 s `fix_sessions` → `Disconnected` |
| `TraderIntelligence.FixWorker.csproj` | 856 | 20 | CRLF | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | net8 Worker SDK |
| `appsettings.json` | 137 | 8 | CRLF | `AB16B7B7…FF33` | **Byte-identical** to mt5-worker |
| `appsettings.Development.json` | 137 | 8 | CRLF | same | Logging only |
| `Properties/launchSettings.json` | 296 | 12 | CRLF | `25A750D823B04F229FBC49A690F997E969276FFB1A5E5F4EF198DF7DE7CBEF9B` | Dev env only |

`UserSecretsId` = `dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79`. Unused.

`Program.cs` pair: same length 859; first byte difference at offset **25** (`using TraderIntelligence.Infrastructure` vs `using TraderIntelligence.FixWorker`). After usings, the bodies are the same composition.

`appsettings.json` of both hosts is **byte-identical**.

### 1.3 Stale hashes (do not quote as current)

| File | Old SHA (B07/C07) | Current SHA | What changed |
|---|---|---|---|
| `fix-worker/Worker.cs` | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` (1971 B) | `92A8F492…B0E2` (2093 B), mtime **2026-08-18T08:04:48Z** | Stopped stamping QUOTE `ReadyForMarketData` / TRADE `LoggedOn` / `LastInboundAt`. Now writes `Disconnected` + `LastError`. |
| mt5-worker `Worker.cs` / both `Program.cs` / csprojs / appsettings | unchanged vs B07/C07 | same | — |

A07/A08 still describe the original 1 s `"Worker running at: {time}"` template. That is **false** now.

---

## 2. Project identity

| | `mt5-worker` | `fix-worker` |
|---|---|---|
| Assembly / exe | `TraderIntelligence.Mt5Worker` 1.0.0 | `TraderIntelligence.FixWorker` 1.0.0 |
| SDK | `Microsoft.NET.Sdk.Worker` | same |
| TFM | `net8.0` (portable; **no** `RuntimeIdentifier`) | same |
| Nullable / implicit usings | on | on |
| Direct package | `Microsoft.Extensions.Hosting` **8.0.1** | same |
| Project refs | Domain, Application, Infrastructure, **Mt5** | Domain, Application, Infrastructure, **Fix.CTrader** |
| Sln GUID | `{31DFD31A-7E82-4968-912F-397C3E7DEE61}` | `{63112B54-6D05-481D-B2F6-99AF3A795192}` |
| Sln folder | `apps` `{91FA4D5C-…}` | same |
| Build configs | Debug + Release listed | same |
| Release output on disk | `bin/Release/net8.0/TraderIntelligence.Mt5Worker.exe` (152 064 B, 2026-08-18T07:26:38Z) | **No** `bin/Release/` |
| Debug exe | 152 064 B, 2026-08-18T07:49:31Z | 152 064 B, 2026-08-18T07:49:32Z |
| `runtimeconfig` | `Microsoft.NETCore.App` 8.0.0 | same |
| RID pin `win-x64` | **Absent** (A105 required for local Manager) | N/A (managed only) |

---

## 3. Host composition (`Program.cs`)

Both hosts:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();
// EnsureCreatedAsync + DemoSeeder.SeedAsync
host.Run();
```

| Boot step | Measured |
|---|---|
| Generic host | Yes (`Host.CreateApplicationBuilder`) |
| Kestrel / HTTP | **No** |
| `AddTraderIntelligence` | Yes — always Fake connectors + InMemory-or-Npgsql |
| Hosted services | **Exactly one** `Worker` |
| Schema | `EnsureCreatedAsync` — **no** EF migrations |
| Seed | `DemoSeeder` if `Brokers` empty |
| Serilog / OTel | **No** (API has Serilog; workers do not) |
| Options bind (`Mt5BrokerOptions` / `CTraderFixOptions`) | **No** |
| Health checks | **No** |
| User secrets | Host builder loads them in Development; **no secrets exist** |

`CreateApplicationBuilder` also wires JSON + environment variables + (Dev) user-secrets. That does **not** map `REAL_COPY_EXECUTION_ENABLED` or `MT5_*` onto the types the workers read.

`fix-worker/Program.cs` never `using`s a `TraderIntelligence.Fix.CTrader.*` type. The adapter DLL is a compile-time reference only.

---

## 4. Runtime loops (`Worker.cs`)

### 4.1 mt5-worker — 30 s Fake ingest + score

Interval: **30 seconds**. Catch-all retry (non-cancel). Log at start: `"Execution copy is not performed here."`

Per tick:

1. Resolve `DealIngestionService` + `ReconstructionScoringService` from a new scope.
2. Window: `[UtcNow-30d, UtcNow+1m]`.
3. `SyncBrokerAsync(ACHIEVER)` then `SyncBrokerAsync(STARWAVEFX)`.
4. `RebuildTraderAsync` for logins **`10001, 10002, 10003, 99001`** (99001 → StarwaveFX).

Call graph:

```text
Worker.ExecuteAsync
  → DealIngestionService.SyncBrokerAsync
       → IBrokerRegistry.Get(code)
       → IMt5BrokerConnector.ConnectAsync        // Fake: _connected = true
       → GetGroups / GetAccounts(null)
       → GetDeals(login, from, to)
       → GetPositions(login)                     // empty fixture list
       → ITradingStore upserts (per-row SaveChanges)
  → ReconstructionScoringService.RebuildTraderAsync
       → LoadDeals → TradeReconstructor → BaselineScorer → UpsertScore + history
```

`IMt5BrokerConnector` verbs: Connect / Disconnect / IsConnected / GetGroups / GetAccounts / GetDeals / GetPositions. **No send.**

Twin port `IBrokerConnector` (`src/Mt5/Connectors/IBrokerConnector.cs`) has `SubscribeEventsAsync` and `ulong` logins. **Zero implementers. Unused.**

### 4.2 Fixture universe the Fake connector holds

`DemoBrokerFactory.CreateDefault()` (also used by DI and seeder):

| Broker | Code | Groups | Logins | Closed XAU round-trips | Deal tickets |
|---|---|---|---|---:|---|
| Achiever | `ACHIEVER` | `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` | 10001, 10002, 10003 | 3 + 3 + 0 | 12 deals |
| StarwaveFX | `STARWAVEFX` | `real\standard` | 99001 | 3 | 6 deals |

Volume scale on fakes: `10_000` (lots → native). Deal times are anchored at **2026-06-01 08:00 UTC** (Starwave +1 day).

**Window trap (date-dependent):** on 2026-08-18 the worker window starts ~2026-07-19. Fixture deals are June 2026 → **`GetDeals` returns 0** on every worker tick. Seeder uses `[2026-01-01, 2026-12-31]` so first boot still persists the 18 deals. Subsequent ticks only rebuild scores and **wipe positions** (`ReplacePositionsAsync` with the empty Fake list).

Hard-coded logins mean a real Manager catalog of ~5 000 accounts would still score four rows.

### 4.3 fix-worker — 15 s session-row stamp (current)

Interval: **15 seconds**. Reads `CTrader:RealCopyExecutionEnabled` (default **false**). Start log claims `REAL_COPY_EXECUTION_ENABLED={Enabled}` but the **name it binds is `CTrader:RealCopyExecutionEnabled`**, not the env `REAL_COPY_EXECUTION_ENABLED`.

Per tick (current `Worker.cs`):

| Row | Status written | Extra |
|---|---|---|
| QUOTE | `FixSessionStatus.Disconnected` | `UpdatedAt=now`, `LastError="No live QUOTE socket. Simulator/demo only."` |
| TRADE | `FixSessionStatus.Disconnected` | `UpdatedAt=now`, `LastError="No live TRADE socket. NewOrderSingle remains off."` |

If `real==true`: extra **warning log only**. Status is still `Disconnected`. No socket, no `35=D`.

What this loop does **not** do: TCP/TLS, Logon, seq files, heartbeat 35=0, Security List, MD, MassStatus, Positions, lease, kill-switch read, outbox claim.

---

## 5. DI census (`AddTraderIntelligence`)

Registered by both hosts (`src/Infrastructure/DependencyInjection.cs`):

| Registration | Lifetime | Used by mt5-worker | Used by fix-worker |
|---|---|---|---|
| `TraderDbContext` InMemory **or** Npgsql | Scoped | seeder + ingest/score | seeder + heartbeat |
| `IMt5BrokerConnector` Achiever Fake | Singleton | ingest | seeder only |
| `IMt5BrokerConnector` Starwave Fake | Singleton | ingest | seeder only |
| `IBrokerRegistry` | Singleton | ingest | seeder only |
| `ITradingStore` → `EfTradingStore` | Scoped | ingest + score | seeder |
| `IDashboardQueries` → `EfDashboardQueries` | Scoped | **unused** | **unused** |
| `TradeReconstructor` | Singleton | score | seeder |
| `BaselineScorer` | Singleton | score | seeder |
| `DealIngestionService` | Scoped | Worker tick | seeder |
| `ReconstructionScoringService` | Scoped | Worker tick | seeder |

Never registered (A64 required):

`Mt5BrokerOptions`, connector factory, live/local/remote transports, `CTraderFixOptions`, QuickFIX initiator, `FixSessionOwnership`, quote/trade sessions, `IOutboxWriter`, `IOutboxProcessor`, `ICheckpointStore`, health checks, Serilog, OTel, Redis multiplexer.

DB choice:

```text
ConnectionStrings:TraderIntelligence  OR  DATABASE_URL
  empty / missing / contains "<SECRET>"  →  UseInMemoryDatabase("trader-intelligence")
  else                                   →  UseNpgsql(connection)
```

Worker `appsettings` have **neither** key. Default = isolated in-process memory per process. Two workers + API = **three separate** InMemory databases unless an operator injects the same Postgres URL.

---

## 6. Tables / entities the workers actually touch

EF maps 19 sets on `TraderDbContext`. Worker-relevant writes:

| Table | Seeder (both hosts) | mt5-worker tick | fix-worker tick |
|---|---|---|---|
| `brokers` | insert Achiever + StarwaveFX (IDs `aaaa…aaa1/2`) | read via `ResolveBrokerId` | — |
| `canonical_instruments` | XAUUSD | — | — |
| `fix_sessions` | QUOTE ReadyForMarketData + TRADE LoggedOn (Pepperstone/cServer demo ids) | — | **overwrite both to Disconnected** |
| `destination_quotes` | static 2399.45 / 2399.85 | — | — |
| `kill_switches` | `None` | **unread** | **unread** |
| `mt5_groups` / `mt5_accounts` / `mt5_deals` | upsert via ingest | upsert (deals 0 if window misses) | seeder only |
| `mt5_positions_current` | replace (empty) | **replace empty every 30 s** | seeder only |
| `reconstructed_trades` | replace per login | replace 4 logins | seeder only |
| `trader_scores` + `trader_score_history` | upsert | upsert 4 logins | seeder only |
| `outbox_events` | — | — | — |
| `sync_checkpoints` | — | — | — |
| `copy_intents` / `risk_decisions` / `execution_intents` / `shadow_orders` / `audit_logs` | — | — | — |

Seeder identity that is **not a live session**:

| Field | Seeded value |
|---|---|
| Host | `live-us-eqx-01.p.c-trader.com` |
| QUOTE port / TRADE port | 5211 / 5212 |
| SenderCompId | `live.pepperstone.1369850` |
| TargetCompId | `cServer` |
| Achiever server / login | `57.128.141.65:443` / 2027 |
| Starwave server / login | `84.201.6.142:443` / 9904 |

Those strings document venues. Neither worker opens them.

If API and fix-worker share Postgres: seeder (API or first worker) writes Ready/LoggedOn; fix-worker then marks Disconnected every 15 s. Dashboard `GetOverview` treats `Disconnected` as **not** quote/trade healthy. `GET /api/health` still **hardcodes** `healthy: true` and ignores the table.

---

## 7. Configuration / env / flags

### 7.1 Committed worker config

Logging levels only (`Default=Information`, `Microsoft.Hosting.Lifetime=Information`). **No** `ConnectionStrings`, `CTrader`, `Mt5`, `Outbox`, `Health`, `Serilog`.

`launchSettings.json`: `DOTNET_ENVIRONMENT=Development` only. No URLs (correct for Worker SDK).

### 7.2 Keys the processes actually read

| Key | Reader | Default | Effect |
|---|---|---|---|
| `ConnectionStrings:TraderIntelligence` | `AddTraderIntelligence` | empty | InMemory |
| `DATABASE_URL` | same | unset | InMemory |
| `CTrader:RealCopyExecutionEnabled` | fix-worker only | `false` | log line + optional warning; **status unchanged** |
| `DOTNET_ENVIRONMENT` | host | Development via launchSettings | user-secrets load (empty) |

### 7.3 Architecture / `.env.example` keys **unread** by either worker

`MT5_*`, `MT5_STARWAVEFX_*`, `MT5_GROUP_*`, `CTRADER_FIX_*`, `REAL_COPY_EXECUTION_ENABLED`, `REDIS_URL`, `RISK_*`, `FEATURE_*`, `MT5_VOLUME_SCALE`, `MT5_PASSWORD_ENCRYPTION_KEY`, `ACHIEVER_EGRESS_IP`.

Env `REAL_COPY_EXECUTION_ENABLED=true` does **not** bind to `CTrader:RealCopyExecutionEnabled`. Flip of that env **does not** change worker behavior.

API `apps/api/appsettings.json` has a `CTrader` block with `RealCopyExecutionEnabled: false`. Workers do **not** share that file.

### 7.4 Secrets

No live passwords in worker appsettings. Seeder/options sketches hold hostnames and manager logins (non-secret identifiers). `.env.example` uses `<SECRET>` placeholders. UserSecrets IDs exist; no secrets files under the worker trees.

---

## 8. Package / assembly census

Direct PackageReference (both csprojs): `Microsoft.Extensions.Hosting` 8.0.1.

Transitive (from Debug `TraderIntelligence.Mt5Worker.deps.json`):

| Package | Version |
|---|---|
| FluentValidation | 11.9.2 (via Application; **unused** in workers) |
| Microsoft.EntityFrameworkCore (+ Abstractions, Analyzers, Relational, InMemory) | 8.0.4 |
| Microsoft.Extensions.* (Hosting, Logging, Configuration, DI, Options, Caching, Diagnostics, FileProviders) | 8.0.0–8.0.2 |
| Npgsql | 8.0.3 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 8.0.4 |
| StackExchange.Redis | 2.8.0 (**0** C# usings; DLL still copied) |
| Pipelines.Sockets.Unofficial | 2.2.8 (Redis transitive) |
| System.IO.Pipelines | 5.0.1 |
| System.Diagnostics.EventLog | 8.0.1 |

fix-worker extra assemblies: `TraderIntelligence.Fix.CTrader` 1.0.0.

**Absent from both graphs:** `QuickFIXn.Core`, `QuickFIXn.FIX44`, `QuickFix.Net`, Serilog, OpenTelemetry, AspNetCore health, native `MT5APIManager64.dll`.

Project assemblies on the worker output: Domain, Application, Infrastructure, Mt5; FixWorker also Fix.CTrader.

---

## 9. Jobs vs A64 target pipelines

### 9.1 mt5-worker — required hosted jobs (A64 §8 / §11.1)

| Required job | Present? |
|---|---|
| `Mt5ConnectHostedService` | **No** (Fake `ConnectAsync` inside god loop) |
| `Mt5GroupDiscoveryHostedService` | **No** (GetGroups inside same loop) |
| `Mt5AccountSyncHostedService` | **No** |
| `Mt5HistoryBackfillHostedService` | **No** (rolling 30-day poll; no checkpoint) |
| `Mt5LiveEventsHostedService` | **No** (`SubscribeEventsAsync` unused) |
| `Mt5ReconciliationHostedService` | **No** |
| `OutboxProcessorHostedService` (`mt5-source`) | **No** |
| Checkpoint store | Entity only |

**Score: 0 / 7.** Reconstruction + scoring run **on the ingest tick**, which A64 forbids as the long-term shape (§12: persist+outbox, then handlers).

### 9.2 fix-worker — required hosted jobs (A64 §9 / §11.2)

| Required job | Present? |
|---|---|
| `FixOptionsValidationHostedService` | **No** |
| `FixSessionLeaseHostedService` | **No** (`FixSessionOwnership` unused; Redis unused) |
| `CTraderQuoteSessionHostedService` | **No** |
| `CTraderTradeSessionHostedService` | **No** |
| `FixSecurityListHostedService` + quote cache | **No** |
| `FixStartupReconciliationHostedService` | **No** |
| `FixPeriodicReconciliationHostedService` | **No** |
| `UnknownExecutionRecoveryHostedService` | **No** |
| `KillSwitchObserverHostedService` | **No** (seeded row unread) |
| `OutboxProcessorHostedService` (`fix-dest`) | **No** |

**Score: 0 / 10.**

`src/Fix.CTrader` contents (not hosted): `CTraderFixOptions` (defaults include live host + Pepperstone SenderCompId + `cServer`), `FixMessageParser` (test pipe parser), `FixSimulationHarness` (unit-test strings), `FixSessionOwnership` + in-memory fencing lock.

### 9.3 Domain engines not hosted

`RiskEngine`, `ShadowCopyEngine`, `ExecutionOrderStateMachine`, `ClOrdIdFactory`, `QuantityNormalizer`, `CopyIntentExpiry`. mt5-worker scoring uses `TradeReconstructor` + `BaselineScorer` only.

---

## 10. Health, metrics, logs, queues

| Probe / signal | Spec | Measured |
|---|---|---|
| Worker `GET /health` | A77 liveness; A64 `127.0.0.1:5081` / `:5082` | **Missing** (no listener) |
| Worker `GET /ready` | A77; DB required; FIX ready must **not** require real send | **Missing** |
| Prometheus names (§58 / A64 §12) | `mt5_connected`, `fix_quote_connected`, `outbox_backlog`, … | **Missing** |
| Serilog | A50 / A102 | **Missing** on workers |
| OpenTelemetry | C26 | **Missing** |
| Structured correlation ids | A64 §4.2 | **Missing** |
| In-memory MT5/FIX queues | A64 §4.4 | **Missing** |
| Outbox claim (`SKIP LOCKED`) | A64 §5 | **Missing** |
| API `/health` | exists | `"ok"` only |
| API `/api/health` | exists | **hardcoded** demo healthy; `outboxBacklog: 0` |

Logging: generic `ILogger<Worker>` console. Two information/warning strings. No redaction sink (nothing secret is logged today).

---

## 11. Deployment / OS / native

| Item | Measured |
|---|---|
| `docker-compose.yml` services | `postgres`, `redis`, `api` only |
| Compose comment | `"Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers."` |
| Dockerfile anywhere (product tree) | **0** |
| `services/` folder | **empty** (no extra worker) |
| mt5-worker on Linux | Compiles. **Cannot** `LoadLibrary` `MT5APIManager64.dll`. Not wired anyway. |
| Vendor Manager DLLs in `apps/mt5-worker/bin` | **0** |
| C# `DllImport` / `NativeLibrary` in `apps/` + `src/` | **0** |
| `mt5-sdk` CMake copy-dlls | Exists for C++ only; **not** invoked by the C# worker |
| fix-worker OS | Linux OK in principle (managed). Still no FIX engine. |
| Replica / lease | None. Two fix-worker processes would both stamp the same two rows if they share Postgres. Harmless today (no socket). Fatal later without A46 lease. |

README run recipe starts **API + web only**. It does not document `dotnet run` of either worker.

---

## 12. Tests that touch workers

| Project | References worker csproj? | Hosts `Worker`? |
|---|---|---|
| `tests/Unit` | **No** | No. Refs Domain, Application, Fix.CTrader |
| `tests/Integration` | **No** | No. Refs Domain, Application, Infrastructure, Fix.CTrader, Mt5 |

Closest coverage: `SeedingAndStoreTests` exercises `DemoSeeder` + `EfTradingStore` (same path as worker boot). That is **not** a host test.

Zero tests assert: 30 s / 15 s loops, Fake window miss, Disconnected stamp, flag non-binding, InMemory isolation, no `35=D`.

---

## 13. Safety / send

| Path | Possible today? |
|---|---|
| FIX `35=D` NewOrderSingle | **No** — no builder, no initiator, no socket |
| MT5 `SendTrade` / dealer send | **No** — no verb on `IMt5BrokerConnector`; C++ SDK not referenced |
| Source broker order from worker | **No** |
| Shadow fill persistence | **No** (engine exists, unhosted) |
| Live promote | **No** |

`MaySendNewOrderSingle` (A64 §7.4 conjunction) is **not implemented**. Send is off because the send function does not exist.

Current fix-worker **no longer forges** `LoggedOn` / `ReadyForMarketData`. That is an honesty improvement vs B07/C07. Remaining lies:

1. `DemoSeeder` still writes Ready/LoggedOn + a live-looking host/comp id.
2. API `/api/health` hardcodes healthy.
3. `GET /api/settings` feature flag `REAL_COPY_EXECUTION_ENABLED=false` is a static JSON blob, not the worker runtime.
4. Start log on fix-worker names `REAL_COPY_EXECUTION_ENABLED` while binding a different key.

---

## 14. Adjacent types (exist, unused by hosts)

| Type | Path | Why it matters |
|---|---|---|
| `IBrokerConnector` | `src/Mt5/Connectors/IBrokerConnector.cs` | Unused twin of `IMt5BrokerConnector` |
| `Mt5BrokerOptions` | `src/Mt5/Configuration/Mt5BrokerOptions.cs` | Never bound |
| `DeterministicGuid` | `src/Mt5/Utils/DeterministicGuid.cs` | Unused by worker |
| `CTraderFixOptions` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Never bound; `RealCopyExecutionEnabled` default false |
| `FixSessionOwnership` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` | In-memory lock; never acquired |
| `FixMessageParser` / `FixSimulationHarness` | `src/Fix.CTrader/Parsing`, `Testing` | Tests only |
| `OutboxEvent` / `OutboxEventType` | Domain + `outbox_events` | Parked table (C58) |
| `SyncCheckpoint` | Domain + `sync_checkpoints` | Parked |
| `KillSwitch` | seeded `None` | Unread |
| C++ `MT5Manager` / `MT5Pool` / `MT5Watchdog` / `MT5HttpClient` | `mt5-sdk/` | Not referenced |

---

## 15. Census scorecard

| Slice | Count / state |
|---|---|
| Product files per host | **6** |
| Hosted services per host | **1** (`Worker`) |
| HTTP endpoints on workers | **0** |
| Config keys with non-log values | **0** committed |
| Live transports | **0** |
| A64 MT5 jobs | **0 / 7** |
| A64 FIX jobs | **0 / 10** |
| Outbox produce | **0** |
| Outbox consume | **0** |
| Checkpoint R/W | **0** |
| Worker tests | **0** |
| Compose services | **0** |
| Native DLLs in output | **0** |
| QuickFIX packages | **0** |
| Fake fixture deals | **18** (12 Achiever + 6 Starwave) |
| Hard-coded scored logins | **4** |
| mt5 tick | 30 s |
| fix tick | 15 s |
| Default DB | InMemory `"trader-intelligence"` |
| Real send | **OFF** (`SAFE_BY_ABSENCE`) |

---

## 16. Evidence (absolute paths)

- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\appsettings.Development.json`
- `D:\Prop\apps\mt5-worker\Properties\launchSettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs` (current SHA `92A8F492…`; B07/C07 SHA is stale)
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj`
- `D:\Prop\apps\fix-worker\appsettings.json`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Fix.CTrader\` (unhosted)
- `D:\Prop\docker-compose.yml`
- `D:\Prop\Mt5TraderIntelligence.sln`
- `D:\Prop\.env.example`
- Target (not measured state): `D:\Prop\reports\swarm\20260818\A64_worker_pipelines.md`

---

## 17. What this census is not

- Not a go-live pass.
- Not “Phase 1 ingestion working.”
- Not “FIX session healthy.”
- Not permission to add `35=D` to the 15 s loop.
- Not a claim that B07/C07 hashes are still current for `fix-worker/Worker.cs`.

Product source was **not** modified by this agent.
