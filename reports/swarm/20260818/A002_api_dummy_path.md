# A002 — API dummy path: FakeMt5 / 10001 seed, TFM, password fail-closed, Fix.CTrader ref

| Field | Value |
|---|---|
| Agent | A002 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A002_api_dummy_path.md` |
| Product source modified | **No.** Report only. |
| Assigned files | `apps/api/Program.cs`, `apps/api/TraderIntelligence.Api.csproj`, `src/Infrastructure/DependencyInjection.cs`, `src/Infrastructure/Seeding/DemoSeeder.cs`, `src/Infrastructure/Seeding/BrokerCatalogSeed.cs` |
| Supporting reads | `src/Infrastructure/TraderIntelligence.Infrastructure.csproj`, `src/Infrastructure/Mt5Live/LiveMt5Registration.cs`, `src/Mt5/TraderIntelligence.Mt5.csproj`, `src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`, `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj`, `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` |
| Secrets | **Not printed.** Config **key names** only. |

Classification (honest): **EXISTS_NEEDS_REFACTOR** with a **compile break** on Infrastructure + a **split-brain** host (DI fail-closed vs startup still seeding FakeMt5).

---

## 0. Four answers (measured)

| Question | Answer |
|---|---|
| Is the running API still seeding FakeMt5 / logins 10001? | **YES.** `Program.cs` still calls `DemoSeeder.SeedAsync` after `EnsureCreatedAsync`. `DemoSeeder` still `DemoBrokerFactory.CreateDefault()` (those are `FakeMt5BrokerConnector` instances) and rebuilds **10001, 10002, 10003, 99001**. `/api/health` still advertises FakeMt5. `/api/ops/resync` still hardcodes the same four logins. `BrokerCatalogSeed` is **not** on the startup path. |
| Is TFM `net8.0` instead of `net8.0-windows` x64? | **YES for the API.** `TraderIntelligence.Api.csproj` is `<TargetFramework>net8.0</TargetFramework>` with **no** `PlatformTarget`. Downstream `Infrastructure` and `Mt5` are `net8.0-windows` + `PlatformTarget` x64. A `net8.0` web host referencing `net8.0-windows` is the wrong RID/TFM pairing for the Manager64 stack. |
| Does DI throw without real passwords? | **YES.** `AddTraderIntelligence` throws `InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.")` when `LiveMt5Registration.HasRealPasswords` is false. That check requires **both** `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` to be non-empty, not contain `<SECRET>`, and not contain `(a/c`. |
| Does Infrastructure compile with `CTraderFixLogonHostedService` without a Fix.CTrader project reference? | **NO.** DI registers the type; the type lives only in `TraderIntelligence.Fix.CTrader.Hosting`; `TraderIntelligence.Infrastructure.csproj` references Domain + Application + Mt5 only. No `using` for the Fix namespace. Expected compiler error: **CS0246**. |

---

## 1. Running API still seeds FakeMt5 + login 10001

### 1.1 Host startup path (`Program.cs`)

Startup **always** creates the database and invokes `DemoSeeder` (not `BrokerCatalogSeed`):

```84:93:D:\Prop\apps\api\Program.cs
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

Health still lies that the live Manager is FakeMt5:

```26:33:D:\Prop\apps\api\Program.cs
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
```

Ops resync still rebuilds the four demo logins (not live account discovery):

```73:82:D:\Prop\apps\api\Program.cs
app.MapPost("/api/ops/resync", async (DealIngestionService ingestion, ReconstructionScoringService scoring, CancellationToken ct) =>
{
    var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var to = DateTimeOffset.UtcNow;
    var a = await ingestion.SyncBrokerAsync("ACHIEVER", from, to, ct);
    var s = await ingestion.SyncBrokerAsync("STARWAVEFX", from, to, ct);
    foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        await scoring.RebuildTraderAsync(login >= 99000 ? "STARWAVEFX" : "ACHIEVER", login, ct);
    return Results.Ok(new { achieverDeals = a, starwaveDeals = s });
});
```

### 1.2 `DemoSeeder` still composes FakeMt5 off-container

After writing Achiever/StarwaveFX catalog rows, it **ignores** the DI `IBrokerRegistry` and builds a second in-memory FakeMt5 pair:

```126:138:D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs
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

`DemoBrokerFactory.CreateDefault()` is defined next to `FakeMt5BrokerConnector` and hardcodes login **10001** on Achiever:

```95:125:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
    public static (FakeMt5BrokerConnector Achiever, FakeMt5BrokerConnector Starwave) CreateDefault()
    {
        // ...
        var achiever = new FakeMt5BrokerConnector(
            "ACHIEVER",
            // ...
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            deals: BuildAchieverDeals(t0));
        // starwave: Mt5AccountDto(99001, ...)
```

`SeedAsync` no-ops only if **any** broker row already exists (`if (await db.Brokers.AnyAsync(ct)) return;`). First boot of an empty in-memory or fresh EnsureCreated DB **will** insert FakeMt5 deals and score 10001.

### 1.3 `BrokerCatalogSeed` is dead on the API path

`BrokerCatalogSeed.EnsureAsync` writes Achiever/StarwaveFX catalog + XAUUSD + kill switch + disconnected FIX rows. It does **not** call FakeMt5 or score 10001.

Workspace grep (`*.cs`): the type is referenced **only** in its own file. `Program.cs` never calls it. Catalog-only seed is unused by the running API.

---

## 2. TFM: API is still `net8.0` (not `net8.0-windows` x64)

`D:\Prop\apps\api\TraderIntelligence.Api.csproj`:

```15:19:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

No `PlatformTarget`. No `RuntimeIdentifier`. Project refs include Infrastructure (`..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj`).

`Infrastructure` **is** windows/x64:

```20:25:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
```

`Mt5` (pulled transitively) is the same `net8.0-windows` + `x64` and copies `MT5APIManager64.dll` / `MetaQuotes.MT5ManagerAPI64.dll`.

**Implication:** even if passwords exist and native connectors register, the API host TFM is the portable `net8.0` graph. Manager64 wants a windows x64 process. This is not “API already retargeted.” Stale `Infrastructure/bin/Debug/net8.0/` and `net8.0-windows/` folders both exist; the **csproj of record** for Infrastructure is `net8.0-windows`. The **csproj of record** for the API is still `net8.0`.

---

## 3. DI throws without real passwords (dummy disabled at registration)

```33:37:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

Gate implementation (values not quoted):

```10:15:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

```49:52:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

`CreateConnectors` builds **only** `NativeMt5BrokerConnector` (no FakeMt5). Empty/placeholder passwords still construct native objects if the throw is bypassed; the throw is the intended fail-closed.

**Split-brain:** `builder.Services.AddTraderIntelligence(...)` runs **before** `app.Build()` and before `DemoSeeder`. On a machine without both real password keys:

1. `AddTraderIntelligence` throws.
2. The host never reaches `DemoSeeder`.

So: dummy **registration** is disabled; dummy **seed source** is still in the tree and still called if DI succeeds. If someone later weakens the throw, first boot will still paint 10001 FakeMt5 rows. Health/resync already assume that dummy world even when DI is native-only.

In-memory DB fallback is independent of the password throw:

```24:31:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence-live"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }
```

Missing `DATABASE_URL` / connection string does **not** throw. Missing MT5 passwords **do**.

FIX password is **not** in this throw. `CTraderFixLogonHostedService` (if it compiled) skips logon when `CTRADER_FIX_PASSWORD` is empty or contains `<SECRET>` — it does not fail the API process.

---

## 4. Infrastructure cannot compile `CTraderFixLogonHostedService` without Fix.CTrader

DI usings (no Fix namespace):

```1:13:D:\Prop\src\Infrastructure\DependencyInjection.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Dashboard;
using TraderIntelligence.Infrastructure.Hosting;
using TraderIntelligence.Infrastructure.Mt5Live;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Mt5.Connectors;
```

Registration:

```46:48:D:\Prop\src\Infrastructure\DependencyInjection.cs
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        return services;
```

`TraderIntelligence.Infrastructure.Hosting` contains **only** `LiveIngestHostedService`. There is no second `CTraderFixLogonHostedService` under `src/Infrastructure`.

The only type definition:

```7:12:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
namespace TraderIntelligence.Fix.CTrader.Hosting;

public sealed class CTraderFixLogonHostedService : BackgroundService
{
```

Infrastructure project references (complete list):

```3:7:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\Mt5\TraderIntelligence.Mt5.csproj" />
  </ItemGroup>
```

No `..\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`. Fix.CTrader itself is `net8.0` (not windows). Adding the reference would compile the type name **if** a `using TraderIntelligence.Fix.CTrader.Hosting;` (or FQN) is also added; **today neither exists**.

Therefore: **Infrastructure does not compile as written.** API (`AddTraderIntelligence`) cannot start from a clean build until this is fixed. This report did **not** run `dotnet build`; the break is a static name-resolution fact (CS0246).

---

## 5. Contradiction map (do not greenwash)

| Surface | Dummy/FakeMt5 | Live native | Compile |
|---|---|---|---|
| `AddTraderIntelligence` | Throws unless both MT5 password keys look real | Registers `NativeMt5BrokerConnector` ×2 | **Broken** on `CTraderFixLogonHostedService` |
| `Program.cs` seed | **Always** `DemoSeeder` → FakeMt5 10001… | Never calls `BrokerCatalogSeed` | Unreachable if DI/compile fails |
| `/api/health` | Hardcoded FakeMt5 healthy=true | Does not probe Manager | Compiles (anonymous object) |
| `/api/ops/resync` | Scores 10001–10003, 99001 | Uses container `DealIngestionService` (native if DI succeeded) | Compiles |
| `BrokerCatalogSeed` | No FakeMt5 | Catalog/FIX placeholder rows only | Compiles; **uncalled** |

Related leftover: `apps/mt5-worker/Worker.cs` still iterates `10001, 10002, 10003, 99001` after live `SyncBrokerAsync`. Same dummy login set, different host. Out of A002’s assigned files; noted so the dummy path is not treated as API-only.

---

## 6. What would have to change (not done here)

1. API csproj: `net8.0-windows` + `PlatformTarget` x64 if this process is meant to load Manager64.
2. Either delete `DemoSeeder` from `Program.cs` (use `BrokerCatalogSeed` or live ingest only) or keep FakeMt5 **out** of any host that claims “dummy disabled.”
3. Stop hardcoding 10001 on `/api/health` and `/api/ops/resync`.
4. Add Fix.CTrader `ProjectReference` + `using` **or** remove `AddHostedService<CTraderFixLogonHostedService>()` from Infrastructure until a worker owns FIX.

---

## 7. Verdict

The **policy comment** in DI (“Dummy/fake broker data is disabled”) is **not** the running API’s seed path. The API project is still **`net8.0`**, still **calls `DemoSeeder`**, still **scores login 10001 from FakeMt5**, and still **advertises FakeMt5 on `/api/health`**. DI **does** throw without real `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD`. Infrastructure **does not** compile `CTraderFixLogonHostedService` without a Fix.CTrader project reference (and a using). `BrokerCatalogSeed` is the unused non-Fake catalog writer.

**PASS/FAIL vs “dummy path removed”:** **FAIL.**
