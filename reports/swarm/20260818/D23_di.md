# D23 — `AddTraderIntelligence` composition-root inventory

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D23_di.md` |
| Agent | D23 (composition root / `DependencyInjection.cs`) |
| Date | 2026-08-18 |
| Assigned | Read `DependencyInjection.cs`. Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| SUT | `D:\Prop\src\Infrastructure\DependencyInjection.cs` |
| Bytes / lines / SHA-256 | **1900** / **44** source lines / `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` |
| Last write | 2026-08-18 13:14:18 |
| Container | `Microsoft.Extensions.DependencyInjection` via `AddTraderIntelligence` (.NET 8) |
| Prior related | C05 (cycle question). This file is an independent inventory of **what is actually registered**. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest, measured)

`AddTraderIntelligence` is a **demo composition stub**, not a production composition root.

It registers **10** services: one EF `DbContext` (InMemory **or** Npgsql), two pre-built `FakeMt5BrokerConnector` singletons, a `BrokerRegistry`, EF store + dashboard queries, a reconstructor, a scorer, and two scoped application services. That is enough to boot `apps/api`, `apps/mt5-worker`, and `apps/fix-worker` against an in-process fake catalog.

It does **not** register live MT5, QuickFIX/n, Redis, outbox, risk, shadow, volume/symbol options, migrations, or host validation.

The most dangerous line is the **silent InMemory fallback**:

```19:25:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence"));
        }
```

Measured host configs: API `ConnectionStrings:TraderIntelligence` is `""`; both workers have **no** connection-string key at all; `docker-compose.yml` starts Postgres + Redis but does **not** pass `ConnectionStrings__TraderIntelligence` or `DATABASE_URL` to `api`. Default run = **EF InMemory**, even when Postgres is up.

Providing a real Npgsql string still does **not** switch brokers to live Manager API. Lines 31–33 always call `DemoBrokerFactory.CreateDefault()`. Real DB + fake MT5 is the only “production-looking” path this method can open.

**Overall class: `EXISTS_NEEDS_REFACTOR`.** The method is a real, compiling DAG. It is not the architecture composition root.

---

## 1. Method

Read in full (product source, **no edits**):

| Path | Bytes | SHA-256 | Written |
|---|---:|---|---|
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | — |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | 2026-08-18 13:18:05 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | 2026-08-18 13:12:48 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 7407 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | 2026-08-18 13:12:48 |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 2026-08-18 13:13:42 |
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | 12307 | `E20457B398DB6CCC5F78ADE295A340CBC0646F5668F9F79F6AFBCC09D35741DD` | 2026-08-18 13:20:12 |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | 8143 | `ECA2EEE8D1AE030A08DA29A4A9C72AAB75883FF93709FC324B9404DD1F689B34` | 2026-08-18 13:08:10 |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | — | `8430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB132` | — |
| `D:\Prop\apps\api\Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | 2026-08-18 13:22:04 |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | 2026-08-18 13:15:01 |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | 2026-08-18 13:15:01 |
| `D:\Prop\apps\api\appsettings.json` | 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` | 2026-08-18 13:15:01 |
| `D:\Prop\apps\mt5-worker\appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | 2026-08-18 12:54:40 |
| `D:\Prop\apps\fix-worker\appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | 2026-08-18 12:54:18 |

Also read: `TraderDbContext` ctor, `EfTradingStore` ctor, `EfDashboardQueries` ctor, `TradeReconstructor` ctor, `BaselineScorer`, `BrokerRegistry`, `DemoBrokerFactory.CreateDefault`, `IMt5BrokerConnector` / `IBrokerRegistry`, `IDashboardQueries`, `IBrokerConnector` (orphan), `Mt5BrokerOptions`, `CTraderFixOptions`, `FixSessionOwnership`, `RiskEngine`, `ShadowCopyEngine`, `SymbolNormalizer`, `VolumeConverter`, `SeedingAndStoreTests`, `docker-compose.yml`, worker loops.

Grep: `AddTraderIntelligence`, `AddSingleton` / `AddScoped` / `AddDbContext`, `ValidateOnBuild`, `ValidateScopes`, `UseInMemoryDatabase`, `DATABASE_URL`, `GetRequiredService` under `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`.

Did **not** start hosts. Did **not** run `dotnet` against product. Did **not** edit anything under `src/`, `apps/`, `tests/`, `mt5-sdk/`.

---

## 2. The file (complete)

```1:44:D:\Prop\src\Infrastructure\DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Dashboard;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Mt5.Connectors;

namespace TraderIntelligence.Infrastructure;

public static class DependencyInjection
{
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

        services.AddScoped<ITradingStore, EfTradingStore>();
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        return services;
    }
}
```

Single public surface: `AddTraderIntelligence(IServiceCollection, IConfiguration)`. No options type, no environment switch, no `IHostEnvironment` branch, no `ValidateOnBuild`.

Usings are all live. No unused imports in this file.

---

## 3. What is registered (complete table)

| # | Service | Lifetime | Implementation / factory | How constructed | Constructor deps |
|---|---|---|---|---|---|
| 1 | `TraderDbContext` | Scoped (`AddDbContext` default) | InMemory name `"trader-intelligence"` **or** `UseNpgsql(connection)` | framework | `DbContextOptions<TraderDbContext>` |
| 2 | `IMt5BrokerConnector` | Singleton **instance** | `DemoBrokerFactory.CreateDefault().Achiever` (`BrokerCode = "ACHIEVER"`) | **at registration**, not resolve | none |
| 3 | `IMt5BrokerConnector` | Singleton **instance** | `CreateDefault().Starwave` (`BrokerCode = "STARWAVEFX"`) | at registration | none |
| 4 | `IBrokerRegistry` | Singleton factory | `new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>())` | first resolve | `IEnumerable<IMt5BrokerConnector>` |
| 5 | `ITradingStore` | Scoped | `EfTradingStore` | container | `TraderDbContext` |
| 6 | `IDashboardQueries` | Scoped | `EfDashboardQueries` | container | `TraderDbContext` |
| 7 | `TradeReconstructor` | Singleton | concrete | container | optional `VolumeConverter?`, `SymbolNormalizer?` — **neither registered** |
| 8 | `BaselineScorer` | Singleton | concrete | container | none |
| 9 | `DealIngestionService` | Scoped | concrete | container | `IBrokerRegistry`, `ITradingStore` |
| 10 | `ReconstructionScoringService` | Scoped | concrete | container | `ITradingStore`, `TradeReconstructor`, `BaselineScorer` |

`AddDbContext` is **not** pooled (`AddDbContextPool` absent). Sensitive-data logging is **off**. No `IDbContextFactory<TraderDbContext>`. No `IDesignTimeDbContextFactory`.

Keyed services (NET 8 `AddKeyedSingleton`) are **not** used. The two connectors are distinguished only by `BrokerCode` inside `BrokerRegistry`.

`GetRequiredService<IMt5BrokerConnector>()` (singular) would return the **last** registration (`starwave`). No host does that today. All production-looking paths go through `IBrokerRegistry.Get(code)` or `GetServices`.

---

## 4. Constructor DAG (acyclic)

```text
IConfiguration
        │  (read once, at AddTraderIntelligence)
        ▼
DbContextOptions<TraderDbContext>     framework
        │
        ▼
 TraderDbContext                      scoped
   │              │
   ▼              ▼
EfTradingStore   EfDashboardQueries   scoped
 (ITradingStore) (IDashboardQueries)
   │
   ├──────────────────────────────┐
   ▼                              ▼
DealIngestionService     ReconstructionScoringService
   │                              │
   ▼                              ├── TradeReconstructor  singleton
IBrokerRegistry                   └── BaselineScorer      singleton
   │
   ▼
IMt5BrokerConnector × 2           singleton instances
(FakeMt5BrokerConnector Achiever + Starwave)
```

Back-edges: **none**. Diamond (`ITradingStore` shared by ingestion + scoring) is not a cycle. No singleton holds a scoped `TraderDbContext`. Captive-dependency check on the current graph: **clean**.

`TradeReconstructor` optional ctor:

```18:22:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
        _symbols = symbols ?? new SymbolNormalizer();
    }
```

Unregistered optionals resolve to `null` → `VolumeConverter.Manager` (scale **10 000**, D14) and `new SymbolNormalizer()`. If a later change registers those types as **scoped**, this singleton becomes a captive. Foot-gun, not a current failure.

---

## 5. Connection-string policy (measured)

Resolution order:

1. `configuration.GetConnectionString("TraderIntelligence")` (`ConnectionStrings:TraderIntelligence`)
2. else `configuration["DATABASE_URL"]`
3. if null/whitespace **or** the string contains the literal `"<SECRET>"` → InMemory
4. else `UseNpgsql(connection)` with the raw string (no NpgsqlDataSource, no retry, no naming convention)

| Host file | `ConnectionStrings:TraderIntelligence` | `DATABASE_URL` | Result of this method |
|---|---|---|---|
| `apps/api/appsettings.json` | `""` (empty) | absent | **InMemory** (`IsNullOrWhiteSpace`) |
| `apps/api/appsettings.Development.json` | absent | absent | still empty from base → **InMemory** |
| `apps/mt5-worker/appsettings.json` | **section absent** | absent | **InMemory** |
| `apps/fix-worker/appsettings.json` | **section absent** | absent | **InMemory** |
| `apps/*/appsettings.Development.json` (workers) | logging only | absent | **InMemory** |
| `docker-compose.yml` `api` service | **not set** | **not set** | **InMemory** even with `postgres:` up |
| `.env.example` | `DATABASE_URL=...Password=<SECRET>` | contains `<SECRET>` | would still **InMemory** if copied blindly |

The `"<SECRET>"` substring check is the **one good safety** in this method: a copied `.env.example` cannot open Npgsql with the literal password `<SECRET>`.

The empty-string fallback is **not** good. A missing production connection string should fail host start (A77 / A90). Instead the process looks healthy on a private InMemory store named `"trader-intelligence"`.

InMemory name is **per process**, not per scope. Three hosts = three independent stores. API seed, MT5-worker seed, and FIX-worker seed never share data unless a real Npgsql string is supplied to **all three**.

On a shared Postgres, all three hosts call `EnsureCreatedAsync` + `DemoSeeder.SeedAsync` (hosts, not this file). That is a **race**, not a DI cycle. See C05 §5.2.

`UseNpgsql(connection)` accepts whatever string is left. No check that it is actually a Postgres URI. No `EnableRetryOnFailure`. No snake-case convention. No migrations assembly.

Class for the fallback: **`UNSAFE` as a default for anything beyond a laptop demo.** Class for the `<SECRET>` guard: **`EXISTS_AND_GOOD`**.

---

## 6. Brokers are always fake

```31:34:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

`CreateDefault()` runs **while the host is still building services**, once per process. It materializes two `FakeMt5BrokerConnector` objects with hard-coded groups, logins `10001/10002/10003/99001`, and a fixed 2026-06-01 deal tape (`FakeMt5BrokerConnector.cs` / `DemoBrokerFactory`).

There is no:

- `IOptions<Mt5BrokerOptions>` bind
- env-driven Achiever / Starwave slots (A58)
- live `IMT5Client` / HTTP bridge connector
- feature flag `MT5_USE_FAKE`
- branch on `IHostEnvironment`

A real `DATABASE_URL` therefore means: **Postgres + still-fake MT5**. That is the measured honesty line for C42 / this file.

`BrokerRegistry` indexes by `BrokerCode` with `OrdinalIgnoreCase`. Codes match `BrokerCodes.Achiever` / `BrokerCodes.StarwaveFx` (`"ACHIEVER"`, `"STARWAVEFX"`).

Orphan type `IBrokerConnector` (`D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs`) is **not** registered. B24: **DEPRECATED**. Cannot participate in this graph.

Seeder calls `DemoBrokerFactory.CreateDefault()` **again** and `new DealIngestionService(...)` (C05 §6). This file is graph A; seeder builds graph B. Same catalog today, different instances. Split composition, not a cycle.

---

## 7. What this method does **not** register

Measured against types that already exist on disk, plus architecture first-useful / go-live surfaces:

| Surface | On disk? | In `AddTraderIntelligence`? | Class |
|---|---|---|---|
| `VolumeConverter` | yes | no (reconstructor falls back to `.Manager`) | **MISSING** from DI |
| `SymbolNormalizer` | yes | no (reconstructor `new`s one) | **MISSING** from DI |
| `RiskEngine` | yes | no | **MISSING** |
| `ShadowCopyEngine` | yes | no | **MISSING** |
| `ClOrdIdFactory` / `ExecutionOrderStateMachine` / `QuantityNormalizer` | yes | no | **MISSING** |
| `FixSessionOwnership` / QuickFIX/n / `CTraderFixOptions` bind | Fix.CTrader project exists | no (Infrastructure **does not** reference Fix.CTrader) | **MISSING** |
| `Mt5BrokerOptions` bind | yes | no | **MISSING** |
| `IConnectionMultiplexer` / Redis | package `StackExchange.Redis` 2.8.0 on Infrastructure csproj | **0** C# usages | **MISSING** (package-only) |
| Outbox dispatcher / `IHostedService` for outbox | `OutboxEvent` entity exists | no | **MISSING** |
| `DemoSeeder` | static class | not a service; hosts call it after `Build()` | **EXISTS_NEEDS_REFACTOR** |
| EF migrations / `IDesignTimeDbContextFactory` | none | n/a | **MISSING** |
| `IDbContextFactory<TraderDbContext>` | no | no | **MISSING** (scopes used instead — acceptable for now) |
| Serilog / OTel meters | no product wiring | no | **MISSING** (C25/C26) |
| SignalR hubs | no | no | **MISSING** (C28) |
| RBAC / auth handlers | no | no | **MISSING** (C18) |
| Health checks (`AddHealthChecks`) | no | no | **MISSING** (A77) |
| `ValidateOnBuild` / `ValidateScopes` | hosts | no | **MISSING** |
| Live `NewOrderSingle` sender | no | no | **SAFE_BY_ABSENCE** (do not add here) |

Infrastructure csproj references Domain, Application, Mt5. It does **not** reference Fix.CTrader, Api, or workers. Composition of FIX therefore **cannot** live in this method without a new project reference — and it should not: FIX belongs on `apps/fix-worker`, not in the shared demo extension.

---

## 8. Who calls it

| Caller | Call site | Extra registrations | Resolves from this graph |
|---|---|---|---|
| `apps/api/Program.cs:9` | `builder.Services.AddTraderIntelligence(builder.Configuration)` | Swagger, CORS (AllowAnyOrigin), JSON enum converter | seed: `TraderDbContext`, `ITradingStore`, `ReconstructionScoringService`; HTTP: `IDashboardQueries`, `DealIngestionService`, scoring on `POST /api/ops/resync` |
| `apps/mt5-worker/Program.cs:7` | same | `AddHostedService<Worker>` | seed trio; loop: `DealIngestionService` + `ReconstructionScoringService` every 30s |
| `apps/fix-worker/Program.cs:7` | same | `AddHostedService<Worker>` | seed trio; loop: **`TraderDbContext` only** (heartbeat rows). Does not resolve FIX types because none are registered |
| `tests/Integration/SeedingAndStoreTests.cs` | **never** | n/a | hand-built `TraderDbContext` + `EfTradingStore` + scoring |
| `tests/Unit/*` | **never** | n/a | domain types constructed directly |

Three hosts copy-paste `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`. This extension method does not own seed. It also registers `IDashboardQueries` into **workers**, which never query it. Harmless extra, not a leak.

Container graph is **untested**. No test builds `ServiceCollection` + `AddTraderIntelligence`. A90 already forbids calling this method from integration fixtures without an explicit Npgsql string (InMemory hazard).

---

## 9. Project graph around this file

```text
Domain
  ▲
Application → Domain
  ▲
  ├── Mt5 → Domain, Application
  └── Infrastructure → Domain, Application, Mt5
           ▲
           ├── apps/api
           ├── apps/mt5-worker
           ├── apps/fix-worker
           └── tests/Integration
```

`Infrastructure → Mt5` exists so this file can reach `DemoBrokerFactory` / `BrokerRegistry` / `FakeMt5BrokerConnector`. That makes the infrastructure library a **composition root + fake adapter host**. Layering smell (C35): **`EXISTS_NEEDS_REFACTOR`**. Not a project cycle.

Application cannot see `TraderDbContext` / `AddTraderIntelligence`. Ports (`ITradingStore`, `IDashboardQueries`, `IMt5BrokerConnector`, `IBrokerRegistry`) live in Application. That direction is correct.

---

## 10. Security / honesty (this file only)

| Check | Result |
|---|---|
| Live passwords in `DependencyInjection.cs` | **none** |
| Connection string logged | **no** (not logged at all; also not redacted — there is no log line) |
| Placeholder connect with password `<SECRET>` | **blocked** by substring check |
| Empty connection → fail closed | **no** — InMemory instead. **UNSAFE** default |
| Live MT5 credentials used | **no** — fakes only |
| Redis password used | **no** — Redis not wired |
| `EnableSensitiveDataLogging` | **off** |
| CORS / auth | not this file (API: AllowAnyOrigin, no RBAC) |

Do not read “`<SECRET>` guard exists” as “safe to go live.” The method will happily run a demo store that `/api/health` still describes as `"postgres-or-inmemory"` with `healthy = true` (API, not this file).

---

## 11. Classification scoreboard

| Item | Class | Evidence |
|---|---|---|
| `AddTraderIntelligence` exists and compiles | **EXISTS_NEEDS_REFACTOR** | 44-line demo DAG, §2–3 |
| Ctor / project cycles | **EXISTS_AND_GOOD** | DAG, C05; reconfirmed §4 |
| InMemory fallback on empty / `<SECRET>` | **UNSAFE** as default; `<SECRET>` guard itself **EXISTS_AND_GOOD** | §5 |
| Fake Achiever + Starwave singletons | **EXISTS_AND_GOOD** for demo; **MISSING** live path | §6 |
| `IBrokerRegistry` via `GetServices` | **EXISTS_AND_GOOD** | last-wins singular resolve unused |
| `ITradingStore` / `IDashboardQueries` | **EXISTS_NEEDS_REFACTOR** | EF demo implementations |
| `TradeReconstructor` / `BaselineScorer` singletons | **EXISTS_AND_GOOD** for current ctors | optional deps unregistered |
| `DealIngestionService` / `ReconstructionScoringService` | **EXISTS_AND_GOOD** as scoped use-cases | untested via container |
| Redis package | **MISSING** implementation | csproj only |
| Risk / shadow / FIX / outbox / options | **MISSING** | §7 |
| Host `ValidateOnBuild` / `ValidateScopes` | **MISSING** | all three `Program.cs` |
| Integration coverage of this method | **MISSING** | `SeedingAndStoreTests` bypasses it |
| `IBrokerConnector` | **DEPRECATED** | unused by this graph |
| Live `NewOrderSingle` | **SAFE_BY_ABSENCE** | not registered; keep it that way |

---

## 12. What this review does **not** claim

- That a host was launched or that MS.DI `ValidateOnBuild` was executed. Inspection + hashes only.
- That InMemory seed equals Postgres behavior (it does not; InMemory ignores most indexes / `SKIP LOCKED`).
- That `UseNpgsql` was observed against a live server. Compose does not even pass the string.
- That C05 is wrong. Cycle question remains **PASS**. This file answers a different question: **what is the composition root, and is it production?** Answer: **no**.
- That “no live MT5 in DI” is a go-live PASS. It is **honest demo**. Live connect is still C42 **FAIL**.
- That adding `VolumeConverter` / a second `DbContext` / a hosted seeder is safe without re-checking captives.

---

## 13. Answers to the assigned read

| Question | Answer |
|---|---|
| What is `DependencyInjection.cs`? | One extension method, `AddTraderIntelligence`, 1900 bytes, SHA-256 `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380`. |
| How many services? | **10** registrations (§3). |
| Default database? | EF InMemory `"trader-intelligence"` for every current host config. |
| Live MT5? | **No.** Always `DemoBrokerFactory.CreateDefault()`. |
| Live FIX / Redis / risk / shadow / outbox? | **Not registered.** |
| Cycles / captives? | **None** on the current graph. |
| Is this the architecture composition root? | **No.** Demo stub. **`EXISTS_NEEDS_REFACTOR`.** |
| Product source changed by D23? | **No.** |

### Recommended later fix (not applied — product source frozen)

When someone is allowed to edit product code:

1. Fail start if `ConnectionStrings:TraderIntelligence` / `DATABASE_URL` is empty or contains `<SECRET>` **except** an explicit `TI_ALLOW_INMEMORY=true` demo flag.
2. Keep a **single** `CreateDefault()` (this file). Pass container `DealIngestionService` into `DemoSeeder`.
3. Bind `Mt5BrokerOptions` / live connectors behind a flag; never imply “Postgres up ⇒ MT5 live.”
4. Move host-only services (dashboard queries, FIX, workers) out of the shared extension, or split `AddTraderIntelligencePersistence` vs `AddDemoBrokers`.
5. Turn on `ValidateOnBuild` + `ValidateScopes` on all three hosts.
6. Add one integration test that builds `ServiceCollection` + `AddTraderIntelligence` and resolves the seed trio **without** calling this method against an implicit InMemory when `TI_TEST_PG` is set.

Until then: treat every default host boot as **in-process demo**, not a data plane.
