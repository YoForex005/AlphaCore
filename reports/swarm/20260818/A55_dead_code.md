# A55 — Dead `dotnet new` template code

| Field | Value |
|---|---|
| Agent | A55 (dead-template inventory) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source edited | **No** |
| Output | this file only |
| Snapshot | live `rg` + `Test-Path` + SHA-256 after concurrent 2026-08-18 swarm writes |

**Asked:** grep `D:\Prop` for `Class1`, `weatherforecast`, `UnitTest1`; list dead template code; check the claim that the solution “only has Mt5 + Mt5Worker”.

---

## Verdict

| Check | Measured result |
|---|---|
| Live product `Class1.cs` | **0 files.** All five `dotnet new classlib` leftovers have been deleted. |
| Live product `UnitTest1.cs` | **0 files.** Both xUnit placeholders have been deleted. |
| Live `weatherforecast` / `WeatherForecast` | **Still present.** Stock ASP.NET Core 8 minimal-API sample is the **only** HTTP route in `TraderIntelligence.Api`. |
| Adjacent live templates (not in the three grep terms) | Both workers are still `dotnet new worker` 1-second log loops. |
| Claim: “`.sln` only has Mt5 + Mt5Worker” | **FALSE.** `D:\Prop\Mt5TraderIntelligence.sln` lists **10** C# projects (plus 3 solution folders). |
| Hollow projects after template deletion | `src/Application` and both test projects now have **zero** `.cs` sources. |
| Stale `bin/` / `obj/` | Application, Mt5 (Release), and both test assemblies still contain the ASCII type names `Class1` / `UnitTest1`. |

Dead **source** that a later implementer must still remove or replace: the WeatherForecast API surface, the two worker loops, and the unused API/worker template knobs listed in §6. Do **not** resurrect `Class1` or `UnitTest1`.

---

## Method

| Tool | Scope |
|---|---|
| `rg` / workspace grep | `Class1`, `weatherforecast`, `WeatherForecast`, `UnitTest1` under `D:\Prop` |
| `Get-ChildItem` + `Test-Path` | product trees `src\`, `apps\`, `tests\` excluding `bin\` / `obj\` |
| `Get-FileHash -Algorithm SHA256` | remaining template files |
| `Select-String` on `.sln` | `Project("{FAE04EC0` = C# project count |
| ASCII scan of built DLLs | leftover type names after source delete |
| Sibling swarm notes | A01, A02, A06, A08, A09, A10, A11 (historical `Class1` / `UnitTest1` quotes) |

Vendor `D:\Prop\mt5-sdk\vendor\` is **out of product dead-code scope**. Recursive search there found **0** `Class1.cs` and **0** `UnitTest1.cs`. Reports under `D:\Prop\reports\` still mention the three names (19 / 6 / 8 files) as **audit history**, not product source.

Concurrent agents deleted `Class1` / `UnitTest1` during this pass. First grep in this session still saw `src\Application\Class1.cs`, `src\Mt5\Class1.cs`, `tests\Unit\UnitTest1.cs`, and `tests\Integration\UnitTest1.cs`. Final snapshot (this file) is the live disk state.

---

## 1. Grep — the three requested names

### 1.1 `Class1` (product source, final snapshot)

**No hits** in `*.cs` / `*.csproj` / `*.http` / `*.json` under `src\`, `apps\`, `tests\`.

`Test-Path` all **False**:

| Path | Exists now |
|---|---|
| `D:\Prop\src\Application\Class1.cs` | no |
| `D:\Prop\src\Domain\Class1.cs` | no |
| `D:\Prop\src\Infrastructure\Class1.cs` | no |
| `D:\Prop\src\Mt5\Class1.cs` | no |
| `D:\Prop\src\Fix.CTrader\Class1.cs` | no |

Those five files were the stock `dotnet new classlib` type (`public class Class1 { }`, 77 bytes, namespaces matching the project). Earlier today A01/A02/A03/A05 quoted them verbatim. This session still read `Application\Class1.cs` before it disappeared:

```csharp
namespace TraderIntelligence.Application;

public class Class1
{

}
```

SHA-256 while it existed (this session): `9AE31B0EC5A04B900962BC0CB2CC40591DCE6B5357ECEE3EE83612F8861C69EF` (77 bytes, 2026-08-18 12:54:15).

No product consumer ever referenced `TraderIntelligence.*.Class1`. It was never a port, entity, or use-case.

### 1.2 `UnitTest1` (product source, final snapshot)

**No hits** under `tests\` product source. `Test-Path` both **False**:

| Path | Exists now |
|---|---|
| `D:\Prop\tests\Unit\UnitTest1.cs` | no |
| `D:\Prop\tests\Integration\UnitTest1.cs` | no |

A09 captured the Visual Studio xUnit placeholder before deletion:

```csharp
namespace TraderIntelligence.Tests.Unit;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
    }
}
```

Integration was the same empty `[Fact] Test1` in `TraderIntelligence.Tests.Integration`. A10 called a passing `dotnet test` on that fact **false green**. Deleting the file is correct; replacing it with a renamed `UnitTest1` would not be.

Product `*.cs` count under `D:\Prop\tests` (exclude `obj`/`bin`): **0**.

### 1.3 `weatherforecast` / `WeatherForecast` (product source, final snapshot)

**Live.** Three product files, seven lines:

| File | Hits |
|---|---|
| `D:\Prop\apps\api\Program.cs` | `MapGet("/weatherforecast")`, `new WeatherForecast`, `record WeatherForecast(...)` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `launchUrl`: `weatherforecast` on profiles `http`, `https`, `IIS Express` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | `GET {{TraderIntelligence.Api_HostAddress}}/weatherforecast/` |

This is the **only remaining named template from the three-term grep**.

---

## 2. Solution membership — “only Mt5 + Mt5Worker” is false

Product solution: `D:\Prop\Mt5TraderIntelligence.sln`  
SHA-256: `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` (7019 bytes).

| Kind | Count |
|---|---:|
| Solution folders (`{2150E333-…}`) | **3** (`src`, `apps`, `tests`) |
| C# projects (`{FAE04EC0-…}`) | **10** |
| On-disk product `TraderIntelligence*.csproj` | **10** (1:1 with sln) |

C# projects **in the sln** (order as written):

| # | Project | Relative path |
|---:|---|---|
| 1 | `TraderIntelligence.Mt5` | `src\Mt5\TraderIntelligence.Mt5.csproj` |
| 2 | `TraderIntelligence.Mt5Worker` | `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` |
| 3 | `TraderIntelligence.Domain` | `src\Domain\TraderIntelligence.Domain.csproj` |
| 4 | `TraderIntelligence.Application` | `src\Application\TraderIntelligence.Application.csproj` |
| 5 | `TraderIntelligence.Infrastructure` | `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| 6 | `TraderIntelligence.Fix.CTrader` | `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` |
| 7 | `TraderIntelligence.Api` | `apps\api\TraderIntelligence.Api.csproj` |
| 8 | `TraderIntelligence.FixWorker` | `apps\fix-worker\TraderIntelligence.FixWorker.csproj` |
| 9 | `TraderIntelligence.Tests.Unit` | `tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| 10 | `TraderIntelligence.Tests.Integration` | `tests\Integration\TraderIntelligence.Tests.Integration.csproj` |

All ten have `Debug|Any CPU` and `Release|Any CPU` `ActiveCfg` + `Build.0`. Nested under the three folders. No orphan GUID.

**There is no product `.sln` that contains only Mt5 + Mt5Worker.** The only other `.sln` trees are vendor examples under `mt5-sdk\vendor\MetaTrader5SDK\Examples\` (out of scope).

Related but different fact (A08): `build-release.log` restored/built a **subset** (Mt5 + Mt5Worker + Domain + Application + Infrastructure). That is a **build log**, not solution membership. Do not collapse “this log built two hosts” into “the sln has two projects.”

A11 already recorded the same 10-project membership. This audit re-read the file; the count has not changed.

---

## 3. Live dead template (keep / delete list)

### 3.1 WeatherForecast API — **DELETE**

`D:\Prop\apps\api\Program.cs` (861 bytes, SHA-256 `560342F352F9EDCADEBD492319D3732A75335D7E9ED434BD3BB6DA0DDEF60134`) is still the stock `dotnet new web` / `webapi` sample:

```csharp
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
```

No `/api/v1`, no auth, no health, no SignalR hub, no Swagger middleware (package is referenced, never called). `AllowedHosts` is `*` in `appsettings.json`.

Companion template files:

| Path | SHA-256 | Bytes | Dead piece |
|---|---|---:|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | `353BB5D9718D6F86F218C1CE0885A55D8F49F68C249F04DA18E363DEA334543A` | 157 | `GET …/weatherforecast/` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | `5CFA6A2415C1A52C29430B489475A05EEF0EBC8A5A2A00DB8880E0C30683C66E` | 1149 | three `launchUrl` = `weatherforecast`; IIS Express anonymous |

**Action (when implementation is authorized):** delete the route, the `WeatherForecast` record, the `.http` sample, and the `launchUrl`. Do not leave `/weatherforecast` beside real routes.

### 3.2 Worker 1-second loops — **REPLACE** (template, not in the three-term grep)

Identical `dotnet new worker` bodies except namespace. They compile; they do not ingest MT5 or speak FIX.

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\mt5-worker\Worker.cs` | 628 | `39BA69F6E0C81A1571866C3414A4A83D2955557F140261B914DDD05F97E3F8F8` |
| `D:\Prop\apps\mt5-worker\Program.cs` | 181 | `39CD6970BD367319DC0319DCD9B7B41AEA2EDA5B166547F9DCB515FB78555394` |
| `D:\Prop\apps\fix-worker\Worker.cs` | 628 | `54CDAF8A3A480BFC70383E1A554551A99A6743B528E364994E3417156D885542` |
| `D:\Prop\apps\fix-worker\Program.cs` | 181 | `8CB687EF5DC83EBBBDC728C57C3A7236686E6BB0F80AC1191DCF5EF0658AAF0A` |

```csharp
_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
await Task.Delay(1000, stoppingToken);
```

`Program.cs` in both hosts is only `Host.CreateApplicationBuilder` + `AddHostedService<Worker>()` + `host.Run()`.

Template `UserSecretsId` still on both csproj files:

- Mt5Worker: `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1`
- FixWorker: `dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79`

`appsettings.json` on both workers is Logging-only (no broker / FIX / Redis / Postgres keys). `launchSettings.json` is the stock worker profile.

---

## 4. Already-deleted template source (do not put back)

| Former path | Template | Replaced by (live, this snapshot) |
|---|---|---|
| `src\Domain\Class1.cs` | empty classlib | Real entities / enums / volume / reconstruction / instruments |
| `src\Infrastructure\Class1.cs` | empty classlib | `Persistence\TraderDbContext.cs` + EF configurations |
| `src\Fix.CTrader\Class1.cs` | empty classlib | `Configuration\CTraderFixOptions.cs`, `Parsing\FixMessageParser.cs` |
| `src\Mt5\Class1.cs` | empty classlib | `Configuration\Mt5BrokerOptions.cs`, `Connectors\IBrokerConnector.cs`, `Utils\DeterministicGuid.cs` |
| `src\Application\Class1.cs` | empty classlib | **Nothing.** Project is now source-empty. |
| `tests\Unit\UnitTest1.cs` | empty `[Fact] Test1` | **Nothing.** Project is now source-empty. |
| `tests\Integration\UnitTest1.cs` | empty `[Fact] Test1` | **Nothing.** Project is now source-empty. |

Deleting the placeholder without adding the real type is the right *cleanup*. It is not an implementation PASS for Application or tests.

---

## 5. Hollow projects left after template deletion

Still **in the sln**, still `.csproj` on disk, **zero** product `.cs` files:

| Project | csproj | Product `.cs` now | Notes |
|---|---|---:|---|
| `TraderIntelligence.Application` | `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | **0** | Still references Domain + FluentValidation 11.9.2. FluentValidation is unused (no validators). |
| `TraderIntelligence.Tests.Unit` | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | **0** | xUnit + Moq + FluentAssertions remain. `dotnet test` should now report 0 tests (better than false-green `Test1`). |
| `TraderIntelligence.Tests.Integration` | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | **0** | Same; InMemory EF package unused. |

An empty SDK class library / test project still **builds**. That must not be scored as “layer exists.”

---

## 6. Adjacent template leftovers (not `Class1` / `weatherforecast` / `UnitTest1`)

| Item | Path | Why it is template / dead |
|---|---|---|
| Unused Swashbuckle | `apps\api\TraderIntelligence.Api.csproj` `Swashbuckle.AspNetCore` 6.6.2 | Package only. `Program.cs` has no `AddSwaggerGen` / `UseSwagger`. |
| Unused SignalR package | same csproj `Microsoft.AspNetCore.SignalR.Common` 8.0.4 | No hub, no `AddSignalR`. |
| Unused Serilog package | same csproj `Serilog.AspNetCore` 8.0.2 | Not configured in `Program.cs`. |
| `AllowedHosts: *` | `apps\api\appsettings.json` | Stock web template. |
| Stock API logging | `apps\api\appsettings*.json` | No connection strings, no auth, no CORS policy names. |
| Stock worker logging | `apps\*\appsettings*.json` | No venue / broker config. |
| Worker `UserSecretsId` | both worker csproj | `dotnet new worker` GUID. |
| Unused FluentValidation | `src\Application\*.csproj` | Package with no validators after `Class1` delete. |

`apps\web` is **not** a Vite default leftover. `App.tsx` already routes Overview / Brokers / Groups / Traders / Shadow / FIX / Risk / etc. It is a dashboard shell, not `Class1`.

---

## 7. Stale binaries still carry deleted type names

Source is gone; **last built** assemblies still contain the ASCII identifiers. Rebuild (or `dotnet clean`) is required before anyone treats `bin/` as evidence.

| Assembly | String still present |
|---|---|
| `D:\Prop\src\Application\bin\Debug\net8.0\TraderIntelligence.Application.dll` | `Class1` (offset 1081) |
| `D:\Prop\src\Application\obj\Debug\net8.0\TraderIntelligence.Application.dll` | `Class1` (offset 1081) |
| `D:\Prop\src\Mt5\bin\Release\net8.0\TraderIntelligence.Mt5.dll` | `Class1` (offset 1077) — Release output predates `Class1` delete + new Mt5 sources |
| `D:\Prop\tests\Unit\bin\Debug\net8.0\TraderIntelligence.Tests.Unit.dll` | `UnitTest1` (offset 1237) |
| `D:\Prop\tests\Integration\bin\Debug\net8.0\TraderIntelligence.Tests.Integration.dll` | `UnitTest1` (offset 1237) |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\TraderIntelligence.Mt5Worker.dll` | no `Class1` (worker never compiled that type) |

Do not cite these DLLs as “Class1 still in the product.” Cite them as **stale compile output**.

---

## 8. What is not dead template (avoid over-delete)

| Tree | Why it is not `Class1` |
|---|---|
| `src\Domain\Entities`, `Enums`, `Volume`, `Reconstruction`, `Instruments` | Real types added this swarm after `Class1` delete |
| `src\Infrastructure\Persistence` | `TraderDbContext` + configurations |
| `src\Mt5\Configuration`, `Connectors`, `Utils` | Ports / options, not `Class1` |
| `src\Fix.CTrader\Configuration`, `Parsing` | Options + parser, not `Class1` |
| `apps\web\src\**` | Dashboard routes, not Vite counter/weather sample |
| `mt5-sdk\src\**` | C++ product/SDK wrapper, not `dotnet new` |
| Vendor Examples `*.sln` | Official MetaQuotes samples; do not treat as Prop dead code |

---

## 9. Recommended cleanup (report only — not done here)

When a later coder is authorized to touch product source:

1. **Delete** `/weatherforecast`, `record WeatherForecast`, `TraderIntelligence.Api.http` sample, and `launchUrl` in all three launch profiles.
2. **Replace** (do not rename) both `Worker.cs` 1s loops. Keep the Worker SDK host.
3. **Add** Application ports / validators. Do **not** recreate `Class1.cs`.
4. **Add** real xUnit classes named for the SUT. Do **not** recreate `UnitTest1`.
5. **Clean** `bin`/`obj` (or rebuild) so `Class1` / `UnitTest1` strings leave the assemblies.
6. **Do not** drop Domain / Application / Infrastructure / Fix.CTrader / Api / FixWorker / tests from the sln to “make it only Mt5 + Mt5Worker.” The sln is supposed to contain those ten projects (A11 / architecture §66). Hollow contents ≠ missing membership.

---

## 10. Counts (this snapshot)

| Metric | Value |
|---:|---|
| Live product `Class1.cs` | **0** |
| Live product `UnitTest1.cs` | **0** |
| Live product files with `weatherforecast` / `WeatherForecast` | **3** |
| Live worker template `Worker.cs` | **2** |
| Product `.sln` C# projects | **10** (not 2) |
| Product `.csproj` on disk | **10** |
| Application product `.cs` | **0** |
| Unit + Integration product `.cs` | **0** |
| Product source files modified by A55 | **0** |

**Classification (architecture §73):** WeatherForecast route = **DEPRECATED** / **UNSAFE** anonymous sample. Worker loops = **EXISTS_NEEDS_REFACTOR** (host) / **MISSING** (behavior). Deleted `Class1` / `UnitTest1` = gone, correctly. Empty Application / test projects = **MISSING** contents, membership **IN**. Claim “sln only has Mt5 + Mt5Worker” = **incorrect**.
