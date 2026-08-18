# C05 — Infrastructure `DependencyInjection` + `DemoSeeder`: circular-reference review

| Field | Value |
|---|---|
| Agent | C05 (senior engineer, DI / composition only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\C05_di_review.md` |
| Assigned question | Read `Infrastructure/DependencyInjection` and `DemoSeeder`. Circular refs? |
| Product source modified | **No.** This report is the only write. |
| Workspace | `D:\Prop` |
| Primary SUTs | `D:\Prop\src\Infrastructure\DependencyInjection.cs`, `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` |
| Container | `Microsoft.Extensions.DependencyInjection` via `AddTraderIntelligence` (.NET 8) |

Classification (architecture §73.B): `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest, measured)

**No circular constructor graph. No circular project references. No container cycle between `AddTraderIntelligence` and `DemoSeeder`.**

`AddTraderIntelligence` is a DAG. `DemoSeeder` is a **static** method, not a registered service, so it cannot participate in an MS.DI ctor cycle. Hosts call the two surfaces **sequentially** (register → `Build()` → scoped `SeedAsync`), never as mutual constructor dependencies.

The real composition defect is **not** a cycle. It is a **split graph**:

1. `AddTraderIntelligence` calls `DemoBrokerFactory.CreateDefault()` and registers those two `FakeMt5BrokerConnector` instances as singletons.
2. `DemoSeeder.SeedAsync` calls `DemoBrokerFactory.CreateDefault()` **again**, `new BrokerRegistry(...)`, and `new DealIngestionService(...)`, ignoring the container’s `IBrokerRegistry` / `DealIngestionService`.

That is dual composition + duplicate fake-connector instances. Classification: **EXISTS_NEEDS_REFACTOR**. It will not throw `InvalidOperationException: A circular dependency was detected...`. It will silently ingest from a **second** in-memory catalog while later workers ingest from the **first**.

`ValidateOnBuild` / `ValidateScopes` are **not** enabled on any host. Cycle absence is from static ctor/project inspection, not from a container validate pass.

---

## 1. Method

Read in full (product source, no edits):

| Path | Bytes | SHA-256 | Written |
|---|---:|---|---|
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | 2026-08-18 13:14:18 |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 4942 | `139D8F872DC473F0C5381AF2393BDBBE60E1D9A2A5179DD1D1737E04CCC00BEF` | 2026-08-18 13:18:05 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 4277 | `87B74E715AD05732D7383E6DA0D038F828CE67053028CDD067A8E9C7BE6E7A07` | 2026-08-18 13:09:51 |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | 9020 | `05103CE5D8F73CD8096E949F736D21594F7FA0033AEA179C9CB47C0EE1D673DB` | 2026-08-18 13:12:48 |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 7049 | `AE7C1B1B01B1A5732ECD257AFEEB930D7D0052670F715E35F6A76E98A03F16E4` | 2026-08-18 13:13:42 |

Also read: `TraderDbContext`, `EfDashboardQueries`, `TradeReconstructor` ctor, `BaselineScorer`, `VolumeConverter` ctor, `SymbolNormalizer` ctor, `Mt5Contracts` ports, host `Program.cs` (api / mt5-worker / fix-worker), host `.csproj` project refs, `SeedingAndStoreTests`, worker loops.

Grep: `AddTraderIntelligence`, `AddSingleton` / `AddScoped`, `GetRequiredService`, `ValidateOnBuild`, `ValidateScopes` under `D:\Prop\src` and `D:\Prop\apps`.

Did **not** start hosts. Did **not** run `dotnet` against product. Did **not** edit anything under `src/`, `apps/`, `tests/`, `mt5-sdk/`.

---

## 2. What is registered

`AddTraderIntelligence` (`DependencyInjection.cs` lines 17–42):

| # | Registration | Lifetime | Implementation / factory | Constructor deps |
|---|---|---|---|---|
| 1 | `TraderDbContext` | Scoped (`AddDbContext`) | InMemory `"trader-intelligence"` if connection is empty / contains `"<SECRET>"`; else `UseNpgsql` | `DbContextOptions<TraderDbContext>` (framework) |
| 2 | `IMt5BrokerConnector` | Singleton (instance) | `DemoBrokerFactory.CreateDefault().Achiever` | none (pre-built) |
| 3 | `IMt5BrokerConnector` | Singleton (instance) | `DemoBrokerFactory.CreateDefault().Starwave` | none (pre-built) |
| 4 | `IBrokerRegistry` | Singleton (factory) | `new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>())` | `IEnumerable<IMt5BrokerConnector>` |
| 5 | `ITradingStore` | Scoped | `EfTradingStore` | `TraderDbContext` |
| 6 | `IDashboardQueries` | Scoped | `EfDashboardQueries` | `TraderDbContext` |
| 7 | `TradeReconstructor` | Singleton | concrete | optional `VolumeConverter?`, `SymbolNormalizer?` — **neither registered** |
| 8 | `BaselineScorer` | Singleton | concrete | none |
| 9 | `DealIngestionService` | Scoped | concrete | `IBrokerRegistry`, `ITradingStore` |
| 10 | `ReconstructionScoringService` | Scoped | concrete | `ITradingStore`, `TradeReconstructor`, `BaselineScorer` |

**Not registered** (so they cannot close a cycle through the container):

- `DemoSeeder` (static class)
- `VolumeConverter`, `SymbolNormalizer`
- `RiskEngine`, `ShadowCopyEngine`
- any FIX / QuickFIX / session-ownership type
- Redis / `IConnectionMultiplexer`
- outbox dispatcher, `IDbContextFactory`, `IDesignTimeDbContextFactory`

`DemoSeeder` is never `AddScoped` / `AddSingleton` / `AddTransient`. Hosts invoke `DemoSeeder.SeedAsync(...)` as a static call after `Build()`.

---

## 3. Constructor DAG (container)

```text
DbContextOptions<TraderDbContext>          (framework)
            │
            ▼
     TraderDbContext                       scoped
       │              │
       ▼              ▼
EfTradingStore    EfDashboardQueries       both scoped
 (ITradingStore)  (IDashboardQueries)
       │
       ├──────────────────────────────┐
       ▼                              ▼
DealIngestionService         ReconstructionScoringService     both scoped
       │                              │
       ▼                              ├── TradeReconstructor  singleton
IBrokerRegistry                       └── BaselineScorer      singleton
       │
       ▼
IMt5BrokerConnector × 2                singleton instances
(pre-built FakeMt5BrokerConnector)
```

Edges, one line each:

- `TraderDbContext` → `DbContextOptions<TraderDbContext>`
- `EfTradingStore` → `TraderDbContext`
- `EfDashboardQueries` → `TraderDbContext`
- `BrokerRegistry` → `IEnumerable<IMt5BrokerConnector>`
- `DealIngestionService` → `IBrokerRegistry`, `ITradingStore`
- `ReconstructionScoringService` → `ITradingStore`, `TradeReconstructor`, `BaselineScorer`
- `TradeReconstructor` → (unregistered optional) `VolumeConverter?`, `SymbolNormalizer?`
- `BaselineScorer` → ∅
- `FakeMt5BrokerConnector` instances → ∅ (not container-constructed)

**Back-edges: none.** Topological order exists: options → db → store/queries → registry → ingestion / scoring. `ReconstructionScoringService` does not depend on `DealIngestionService`. `ITradingStore` does not depend on scoring or ingestion. `IBrokerRegistry` factory only calls `GetServices<IMt5BrokerConnector>()`, not `IBrokerRegistry` — no factory re-entrancy.

`DealIngestionService` and `ReconstructionScoringService` share `ITradingStore`. Shared dependency is a **diamond**, not a cycle.

### 3.1 Captive-dependency check (lifetime)

| Consumer | Lifetime | Dep | Dep lifetime | Captive? |
|---|---|---|---|---|
| `DealIngestionService` | scoped | `IBrokerRegistry` | singleton | no (scoped → singleton is legal) |
| `DealIngestionService` | scoped | `ITradingStore` | scoped | no |
| `ReconstructionScoringService` | scoped | `ITradingStore` | scoped | no |
| `ReconstructionScoringService` | scoped | `TradeReconstructor` / `BaselineScorer` | singleton | no |
| `EfTradingStore` / `EfDashboardQueries` | scoped | `TraderDbContext` | scoped | no |
| `IBrokerRegistry` | singleton | `IMt5BrokerConnector` | singleton | no |
| `TradeReconstructor` | singleton | scoped anything | — | no (optionals unregistered) |

**No singleton holds a scoped `TraderDbContext`.** That is the usual hidden “cycle-like” blow-up (`Cannot consume scoped service from singleton`). It is not present.

### 3.2 `TradeReconstructor` optional-ctor landmine (not a cycle)

```18:22:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
        _symbols = symbols ?? new SymbolNormalizer();
    }
```

`AddSingleton<TradeReconstructor>()` lets MS.DI pick this ctor. On .NET 8, an **unregistered** parameter with a default uses that default (`null` → internal `VolumeConverter.Manager` / `new SymbolNormalizer()`). Current graph therefore resolves.

If a later change registers `VolumeConverter` or `SymbolNormalizer` as scoped, this singleton **becomes** a captive dependency. `VolumeConverter(decimal scale = 10000)` would also become constructible if registered without a factory. That is a future foot-gun, not a cycle today.

---

## 4. Project / assembly graph (also acyclic)

```text
Domain  (0 project refs)
   ▲
   │
Application  → Domain
   ▲
   ├── Mt5          → Domain, Application
   ├── Fix.CTrader  → Domain, Application
   └── Infrastructure → Domain, Application, Mt5
            ▲
            ├── apps/api
            ├── apps/mt5-worker  (also refs Domain, Application, Mt5 — redundant, not cyclic)
            ├── apps/fix-worker  (also refs Domain, Application, Fix.CTrader)
            └── tests/Integration
```

Measured refs:

- `TraderIntelligence.Infrastructure.csproj` → Domain, Application, Mt5. **Does not** reference Api / workers / Fix.CTrader / tests.
- `TraderIntelligence.Application.csproj` → Domain only. **Does not** reference Infrastructure.
- `TraderIntelligence.Mt5.csproj` → Domain, Application. **Does not** reference Infrastructure.
- `TraderIntelligence.Domain.csproj` → nothing.

**No project cycle.** Clean-architecture direction is preserved: Infrastructure implements Application ports; Application cannot see `TraderDbContext` / `DemoSeeder` / `EfTradingStore`.

`Infrastructure → Mt5` exists solely so composition can reach `DemoBrokerFactory` / `BrokerRegistry` / `FakeMt5BrokerConnector`. Layering smell (infra library acting as composition root + depending on the fake adapter), **not** a cycle. Classification: **EXISTS_NEEDS_REFACTOR** (move composition to hosts later).

---

## 5. `DemoSeeder` vs the container — no mutual reference

`DependencyInjection` does **not** mention `DemoSeeder`.
`DemoSeeder` does **not** mention `AddTraderIntelligence` / `IServiceCollection` / `IServiceProvider`.

Seeder signature:

```16:20:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
    public static async Task SeedAsync(
        TraderDbContext db,
        ITradingStore store,
        ReconstructionScoringService scoring,
        CancellationToken ct)
```

Those three are **parameters**, not service-locator lookups. The method cannot form a container cycle.

Host wiring (same shape in api, mt5-worker, fix-worker):

```80:88:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>(),
        CancellationToken.None);
}
```

Order is linear: `AddTraderIntelligence` → `Build()` → `CreateScope()` → resolve `TraderDbContext` + `ITradingStore` + `ReconstructionScoringService` → `SeedAsync`. Because scoring is resolved at startup, **any cycle involving scoring → store → db would have thrown on first host start**. None of the three `Program.cs` files wrap seed in a circular-dependency catch.

`CreateScope()` is correct: scoped `TraderDbContext` is not pulled from the root provider.

### 5.1 Same-scope dual handle (diamond, not cycle)

Inside one host scope:

```text
TraderDbContext  ──┬── passed into SeedAsync as `db`
                   └── injected into EfTradingStore (`store`)
                              └── injected into ReconstructionScoringService (`scoring`)
```

`SeedAsync` therefore holds `db` and `store` (and `scoring` which holds the same `store` / same `db`). Two names, **one scoped instance**. Writes:

1. catalog rows via `db.*` + `db.SaveChangesAsync`
2. groups / accounts / deals / positions via `store` (also `SaveChangesAsync` per upsert)
3. reconstructed trades + scores via `scoring` → `store`

That is a **dual write path** on one change tracker. After step 1’s `SaveChangesAsync` the tracker is clean; steps 2–3 do not re-enter step 1. Not a cycle. Risk if a future edit mixes unsaved `db` graph mutations with `store` upserts on the same tracked `Broker` / `Mt5Deal` rows.

### 5.2 Early-exit is not recursive

```22:23:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        if (await db.Brokers.AnyAsync(ct))
            return;
```

Three hosts all seed. On a shared Postgres this is a **race** (two `AnyAsync == false` then unique-index blow-up on `brokers.code`), not recursion. InMemory name `"trader-intelligence"` is per process, so api / mt5-worker / fix-worker each get their own empty store and each seed once.

---

## 6. The actual defect: split composition (looks like a cycle, is a fork)

Seeder body after catalog save:

```124:136:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

Compared with DI:

```31:41:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        services.AddSingleton<IMt5BrokerConnector>(achiever);
        services.AddSingleton<IMt5BrokerConnector>(starwave);
        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        // ...
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
```

| Object | Container instance | Seeder instance | Same object? |
|---|---|---|---|
| `FakeMt5BrokerConnector` Achiever | `CreateDefault()` at registration | second `CreateDefault()` at seed | **No** |
| `FakeMt5BrokerConnector` Starwave | same | same | **No** |
| `BrokerRegistry` | factory singleton | `new BrokerRegistry(...)` | **No** |
| `DealIngestionService` | scoped, unused by seeder | `new DealIngestionService(...)` | **No** |
| `ITradingStore` / `EfTradingStore` | scoped, passed in | used | **Yes** |
| `ReconstructionScoringService` | scoped, passed in | used | **Yes** |
| `TradeReconstructor` / `BaselineScorer` | singletons inside scoring | used via scoring | **Yes** |
| `TraderDbContext` | scoped, passed in | used | **Yes** |

So seed **reads fake deals from graph B** and **writes through graph A’s store**. After seed, `apps/mt5-worker` and `POST /api/ops/resync` resolve the **container** `DealIngestionService` and read fake deals from **graph A**.

Today both factories build identical catalogs (`FakeMt5BrokerConnector` is immutable after `CreateDefault` unless someone calls `AddDeal`). Upserts are idempotent on `(BrokerId, DealTicket)` (`EfTradingStore.UpsertDealAsync`). Demo therefore **converges**. The fork is still a defect:

- `AddDeal` / live mutation on the DI singletons will **not** be seen by a re-seed (seed already returned because brokers exist).
- A re-seed after `Brokers` truncate would ingest from **new** factory copies, not the live singletons.
- Tests (`SeedingAndStoreTests`) never go through `AddTraderIntelligence`; they construct store + scoring by hand and let the seeder build its own connectors. Container graph is **untested**.

Classification of the fork: **EXISTS_NEEDS_REFACTOR**. Not a circular reference.

### 6.1 Why this is easy to misread as a cycle

A reader sees:

- DI creates connectors → registry → `DealIngestionService`
- Seeder creates connectors → registry → `DealIngestionService`
- Seeder also takes `ReconstructionScoringService` which takes `ITradingStore`
- `ITradingStore` is the same type the container’s `DealIngestionService` takes

That is two **independent** fans into `ITradingStore`, not A→B→A.

---

## 7. Host / test consumers (resolve order)

| Consumer | Resolves at startup | Later resolves | Cycle risk |
|---|---|---|---|
| `apps/api/Program.cs` | `TraderDbContext`, `ITradingStore`, `ReconstructionScoringService` + static seed | Minimal APIs: `IDashboardQueries`, `TraderDbContext`, `DealIngestionService` + scoring on `/api/ops/resync` | none on current graph |
| `apps/mt5-worker` | same seed trio | `Worker` → scope → `DealIngestionService` + `ReconstructionScoringService` every 30s | none |
| `apps/fix-worker` | same seed trio | `Worker` → scope → `TraderDbContext` only | none |
| `SeedingAndStoreTests` | **no container** | `new TraderDbContext` + `new EfTradingStore` + `new ReconstructionScoringService` | n/a |

`DealIngestionService` is registered but **never resolved during seed**. First container resolve of that type is `/api/ops/resync` or the MT5 worker loop. Its ctor graph is still acyclic (registry + store only).

---

## 8. Adjacent issues (not circular, do not greenwash)

These are in the same two files and must not be mistaken for “cycle PASS ⇒ composition PASS”.

| Issue | Where | Class |
|---|---|---|
| Dual `CreateDefault()` / seeder `new`s ingestion | §6 | **EXISTS_NEEDS_REFACTOR** |
| `DemoSeeder` not in DI; three hosts copy-paste seed | api + both workers | **EXISTS_NEEDS_REFACTOR** |
| `EnsureCreatedAsync` instead of migrations | all three `Program.cs` | **UNSAFE** vs A61 (out of this review’s cycle question) |
| Empty `ConnectionStrings:TraderIntelligence` → InMemory | `DependencyInjection.cs` 19–25; `apps/api/appsettings.json` | demo-only; not a cycle |
| No `ValidateOnBuild` / `ValidateScopes` | all hosts | **MISSING** (would catch future cycles / captives) |
| Integration tests bypass `AddTraderIntelligence` | `SeedingAndStoreTests.cs` | container graph untested |
| Orphan `IBrokerConnector` still on disk | `D:\Prop\src\Mt5\Connectors\IBrokerConnector.cs` | **DEPRECATED** (B24); unused by DI/seeder; cannot cycle |
| Domain `RiskEngine` / `ShadowCopyEngine` unregistered | Domain | **MISSING** from composition (gap, not cycle) |
| Redis package unused | Infrastructure csproj | **MISSING** implementation |
| Seeder embeds live-shaped FIX `SenderCompId` / host / manager logins | `DemoSeeder.cs` 29–101 | catalog demo data; security is A19’s lane |
| Three-process seed race on real Postgres | hosts | race, not cycle |
| `Infrastructure` references `Mt5` for fakes | csproj | layering, not cycle |

Unused usings in `DemoSeeder` (`Domain.Reconstruction`, `Domain.Scoring`) are noise only.

---

## 9. What this review does **not** claim

- That `AddTraderIntelligence` is complete vs architecture §§6, 13, 45, 56. It is a **demo stub** (B03: 18/43 tables, no Redis, no outbox).
- That connectors are production Manager API. They are `FakeMt5BrokerConnector` only.
- That a host was launched and the container validated. Inspection only.
- That “no cycle” means “safe to add `VolumeConverter` / a second `DbContext` / a hosted seeder that takes `DealIngestionService` without re-checking.”
- That EF entity navigations form or do not form CLR cycles. `TraderDbContext` maps tables with **no** `HasOne` / `HasMany` / navigation properties in the fluent block. Irrelevant to MS.DI.

---

## 10. Answers to the assigned question

| Question | Answer |
|---|---|
| Circular ctor refs inside `AddTraderIntelligence`? | **No.** DAG, §3. |
| Circular project refs involving Infrastructure / Application / Mt5 / Domain? | **No.** §4. |
| Circular ref between `DependencyInjection` and `DemoSeeder`? | **No.** Seeder is static; DI does not reference it; hosts call them in series. §5. |
| Captive scoped-from-singleton? | **No** on the current graph. §3.1. |
| Factory re-entrancy (`IBrokerRegistry` resolving itself)? | **No.** Factory only enumerates `IMt5BrokerConnector`. |
| Anything that *behaves* like a dangerous loop? | **Split graph**, not a loop: seeder `new`s a second connector/registry/ingestion stack. §6. |
| Would MS.DI throw `circular dependency` on current code? | **No**, if `TradeReconstructor` optional defaults behave as documented for .NET 8. |

**Overall class for the assigned check:** cycle question **PASS** (none found). Composition quality **EXISTS_NEEDS_REFACTOR** (forked factory + seeder bypasses container `DealIngestionService`).

### Recommended fix (not applied — product source frozen)

When someone is allowed to edit product code:

1. Delete seeder’s `CreateDefault` / `new BrokerRegistry` / `new DealIngestionService`.
2. Change `SeedAsync` to take `DealIngestionService` (or `IBrokerRegistry`) from the same scope the hosts already open.
3. Keep a **single** `DemoBrokerFactory.CreateDefault()` inside `AddTraderIntelligence`.
4. Turn on `ValidateOnBuild = true` and `ValidateScopes = true` on all three hosts.
5. Add one integration test that builds `ServiceCollection` + `AddTraderIntelligence` and resolves `DealIngestionService` + `ReconstructionScoringService` + `DemoSeeder.SeedAsync` through that provider.

Until then: **no circular references to fix.**
