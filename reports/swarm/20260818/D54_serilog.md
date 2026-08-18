# D54 — Serilog package used?

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D54_serilog.md` |
| Agent | D54 (senior engineer, Serilog package-use recensus only) |
| Date | 2026-08-18 13:39:20 +05:30 |
| Measured at | 2026-08-18T13:38:57+05:30 |
| Workspace | `D:\Prop` |
| Assigned | Confirm whether the Serilog package is used. Write this report. **Do not modify product source.** |
| Primary files | `D:\Prop\apps\api\TraderIntelligence.Api.csproj`, `D:\Prop\apps\api\Program.cs`, `D:\Prop\apps\api\appsettings.json`, `D:\Prop\apps\api\appsettings.Development.json` |
| Adjacent hosts | `D:\Prop\apps\mt5-worker\`, `D:\Prop\apps\fix-worker\` |
| Law | Architecture v2 §5 (stack name), §57 (structured ids + central redaction), A50 (binding Serilog / OTel spec), A102 (8.x pin) |
| Relates | A06, A29 T05, A50, A55, A76, A102, B06 §3.2, C04 (`UseSerilog` = 0), **C25** (prior measure; config is stale) |
| Product source modified | **No.** This report is the only product-adjacent write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**The Serilog NuGet package is referenced. It is not used as a logging pipeline.**

`apps/api` has a committed, restored, copy-local `<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />`. Worktree API `appsettings.json` and `appsettings.Development.json` now contain a `"Serilog"` JSON block. **Zero** product `*.cs` files under `apps/`, `src/`, or `tests/` mention `Serilog`, `UseSerilog`, `AddSerilog`, `Log.Logger`, or `LoggerConfiguration` (85 authored `.cs` files counted). `Program.cs` still starts with `WebApplication.CreateBuilder` and never swaps the logger. Workers have **no** Serilog package, **no** Serilog DLLs, and **no** `"Serilog"` config.

Do **not** treat the package line, the DLL copy, or the new JSON block as “structured logging implemented.” Dead config is not `UseSerilog()`.

| Question | Answer |
|---|---|
| Is a Serilog package on the product graph? | **Yes.** Direct `Serilog.AspNetCore` **8.0.2** on `D:\Prop\apps\api\TraderIntelligence.Api.csproj` line 11. Same line in HEAD and worktree. |
| Is that the only product `PackageReference`? | **Yes.** Grep of every `*.csproj` under `D:\Prop` → **one** `Serilog*` reference. |
| Restored / copy-local? | **Yes.** `obj\project.assets.json` `Serilog.AspNetCore/8.0.2`; `deps.json` host dependency; nine `Serilog*.dll` under `bin\Debug\net8.0`. |
| Does any product C# call Serilog? | **No. 0 hits** in 85 authored `*.cs` files (`apps` + `src` + `tests`, exclude `bin`/`obj`). |
| `UseSerilog` / `AddSerilog` / `Log.Logger` / `CreateLogger` / `ReadFrom.Configuration` in `Program.cs`? | **0 / 0 / 0 / 0 / 0** (4731-byte worktree file). |
| `"Serilog"` JSON in API config? | **Yes on worktree** (both `appsettings*.json`). **No on HEAD** (`HEAD:apps/api/appsettings.json` is MEL-only `"Logging"`). `appsettings.Development.json` is **untracked**. |
| Does that JSON run? | **No.** `Serilog.Settings.Configuration` is on disk but nothing calls `ReadFrom.Configuration` / `UseSerilog`. The block is unread. |
| What logger actually runs if the API starts? | Default ASP.NET Core / MEL from `WebApplication.CreateBuilder`. Worktree API config **removed** the MEL `"Logging"` section (present on HEAD), so host levels are framework defaults. |
| Workers? | **No package. No DLLs. No JSON.** MEL `ILogger<Worker>` only. |
| CPM / `Directory.Packages.props`? | **Does not exist.** Version is inline `8.0.2`. A102 wants `8.0.3`. |
| `Directory.Build.props` mention? | **No.** |
| OpenTelemetry / redactor / enricher types? | **MISSING.** No `Observability/` tree. No `SensitiveDataEnricher`. No `FixWireRedactor`. |
| Classification — package reference | **`EXISTS_NEEDS_REFACTOR`** (present, host unused). |
| Classification — Serilog JSON | **`EXISTS_NEEDS_REFACTOR`** (dead config; would also be incomplete vs A50 if wired). |
| Classification — pipeline / §57 / A50 | **`MISSING`**. |

Do **not** delete `Serilog.AspNetCore` as cleanup. Do **not** claim §57 logging from the JSON sketch. Wire later per A50 (redaction **before** options bind), or the first `{@Options}` / QuickFIX `FileLog` will print secrets / tag **554**.

---

## 1. Method

| Source | Action |
|---|---|
| API csproj | Full read of `D:\Prop\apps\api\TraderIntelligence.Api.csproj` (21 physical lines). |
| API host | Full read of `D:\Prop\apps\api\Program.cs` (95 physical / 86 non-blank / 4731 bytes). Token counts via .NET regex on the raw file. |
| API config | Full read of `appsettings.json` (50 lines / 1254 bytes) + `appsettings.Development.json` (21 lines / 478 bytes). |
| Other product csproj | Full read of Domain, Application, Infrastructure, Mt5, Fix.CTrader, both workers, both test projects. |
| Product C# grep | `Serilog` / `UseSerilog` / `AddSerilog` / `Log.Logger` / `LoggerConfiguration` over `apps/`, `src/`, `tests/` `*.cs` (exclude `bin`/`obj`). **0** matches. 85 files. |
| Restore graph | `apps\api\obj\project.assets.json` (`Serilog.AspNetCore/8.0.2` + 8 transitive Serilog packages); `TraderIntelligence.Api.csproj.nuget.dgspec.json`; `bin\Debug\net8.0\TraderIntelligence.Api.deps.json` host `dependencies`. |
| Output assemblies | `Get-ChildItem bin\Debug\net8.0\Serilog*.dll` + `FileVersionInfo`. Worker bins: empty glob. |
| Worker assets | `Select-String Serilog` on both worker `project.assets.json` → **0**. |
| Generated usings | `TraderIntelligence.Api.GlobalUsings.g.cs` — stock Web SDK; `Microsoft.Extensions.Logging` only. |
| Observability tree | Recursive search for `Observability/` under `src/` + `apps/` → **none**. |
| Binding specs | Architecture §5 lines 216–231, §57 lines 2108–2133; A50; A102 §6.3; C25 (13:26:36). |
| Git | `git rev-parse HEAD`; `git hash-object` vs `HEAD:` blobs; `git status --short`; `git show HEAD:apps/api/appsettings.json`; `git show HEAD:apps/api/Program.cs`. |

No `dotnet add`, no `UseSerilog` patch, no package bump, no `appsettings` edit, no process launch.

---

## 2. Measured files (non-`bin` / non-`obj`)

SHA-256 via `Get-FileHash`. Bytes = `Length`. Physical lines = `Get-Content.Count`.

### 2.1 API host

| Path | Bytes | Physical lines | SHA-256 | vs HEAD |
|---|---:|---:|---|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | **Equal.** Blob `188adce4e084ac942d2c75f6df9bd090b172512c`. |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | **Dirty.** wt `9d623e1da0adc050b15a6d04f330169be2a125e5` ≠ HEAD `93279323bc65ad18f3190fdb601b7fea3b4b027e`. |
| `D:\Prop\apps\api\appsettings.json` | 1254 | 50 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | **Dirty.** wt `df04d38acb150aa0772d4b3358b94ca817085179` ≠ HEAD `10f68b8c8b4f796baf8ddeee7551b6a52b9437cc`. |
| `D:\Prop\apps\api\appsettings.Development.json` | 478 | 21 | `181B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B048` | **Not in HEAD.** Untracked / new on disk. Blob `ebbaba0ffcb7af4ea02bc5012f1de584bd725730`. |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | Dirty (C50). Irrelevant to Serilog. |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | `1BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F` | Dirty. No Serilog keys. |
| `D:\Prop\Directory.Build.props` | 269 | 9 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` | No Serilog. |

`git status --short -- apps/api`: ` M Program.cs`, ` M Properties/launchSettings.json`, ` M TraderIntelligence.Api.http`, ` M appsettings.json`, `?? Controllers/`. **csproj is clean.** The Serilog package line is **committed** (initial tree). It was never wired after that.

`Program.cs` SHA matches D06 (`61B1E0D1…`, 4731 bytes). C25’s `E914FA98…` / 4658 bytes is an **older worktree** of the same host (still zero Serilog tokens).

`Controllers/SettingsController.cs` is untracked. Full read: MVC + Redis settings; **no** Serilog types. `Program.cs` does not `AddControllers` / `MapControllers`. Irrelevant to this question.

### 2.2 Workers (no Serilog surface)

| Path | Bytes | SHA-256 | Serilog? |
|---|---:|---|---|
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | **No** package. Only `Microsoft.Extensions.Hosting` 8.0.1. HEAD blob = worktree. |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | Dirty vs HEAD. No `UseSerilog`. |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | MEL `ILogger<Worker>` only. |
| `D:\Prop\apps\mt5-worker\appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | MEL `"Logging"` only. |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | **No** package. HEAD = worktree. |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | Dirty vs HEAD. No `UseSerilog`. |
| `D:\Prop\apps\fix-worker\Worker.cs` | 2093 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | MEL `ILogger<Worker>` only. |
| `D:\Prop\apps\fix-worker\appsettings.json` | 137 | `AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33` | Same MEL stub as MT5 worker. |

Worker `appsettings.Development.json` files are byte-identical to the base worker `appsettings.json` (same SHA).

### 2.3 Libraries and tests (no Serilog package)

| Project | Serilog `PackageReference` |
|---|---|
| `src/Domain/TraderIntelligence.Domain.csproj` | **None.** Zero packages (keep it that way — A01). |
| `src/Application/TraderIntelligence.Application.csproj` | **None.** FluentValidation 11.9.2 only. |
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | **None.** EF Design/InMemory 8.0.4, Npgsql 8.0.4, StackExchange.Redis 2.8.0. |
| `src/Mt5/TraderIntelligence.Mt5.csproj` | **None.** |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | **None.** |
| `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | **None.** |
| `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | **None.** |

`Directory.Packages.props` **does not exist**.

---

## 3. Package is on the API — evidence

### 3.1 Direct reference (authoritative)

`D:\Prop\apps\api\TraderIntelligence.Api.csproj` lines 9–13:

```xml
  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>
```

This is the **only** `Serilog*` `PackageReference` in the product tree.

### 3.2 Restore graph

`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json` (net8.0 dependencies):

```json
"Serilog.AspNetCore": {
  "target": "Package",
  "version": "[8.0.2, )"
}
```

`D:\Prop\apps\api\obj\project.assets.json` compile+runtime asset `Serilog.AspNetCore/8.0.2`:

| Field | Value |
|---|---|
| nupkg SHA-512 (assets) | `LNUd1bHsik2E7jSoCQFdeMGAWXjH7eUQ6c2pqm5vl+jGqvxdabYXxlrfaqApjtX5+BfAjW9jTA2EKmPwxknpIA==` |
| Transitive compile-relevant packages | `Serilog` **3.1.1**, `Serilog.Extensions.Hosting` **8.0.0**, `Serilog.Formatting.Compact` **2.0.0**, `Serilog.Settings.Configuration` **8.0.2**, `Serilog.Sinks.Console` **5.0.0**, `Serilog.Sinks.Debug` **2.0.0**, `Serilog.Sinks.File` **5.0.0**, `Serilog.Extensions.Logging` **8.0.0** |
| TFM lib used | `lib/net8.0/Serilog.AspNetCore.dll` |
| **Not** pulled | `Serilog.Enrichers.Environment`, `Serilog.Enrichers.Thread` |

Those transitive versions are **what 8.0.2 actually pulls**. They are **not** the A102 CPM table (`Formatting.Compact` 3.0.0, `Sinks.Console`/`File` 6.0.0, `Settings.Configuration` 8.0.4). When a later wave wires Serilog, bump via CPM — do not freeze 8.0.2’s older transitives as the product pin.

`D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.deps.json` host `TraderIntelligence.Api/1.0.0` dependencies:

```text
Microsoft.AspNetCore.SignalR.Common=8.0.4
Serilog.AspNetCore=8.0.2
Swashbuckle.AspNetCore=6.6.2
TraderIntelligence.Application=1.0.0
TraderIntelligence.Domain=1.0.0
TraderIntelligence.Infrastructure=1.0.0
```

Worker `project.assets.json` files have **zero** `"Serilog` keys.

### 3.3 Assemblies on disk (unused at runtime)

`D:\Prop\apps\api\bin\Debug\net8.0\`:

| File | Bytes | FileVersion | ProductVersion |
|---|---:|---|---|
| `Serilog.AspNetCore.dll` | 16384 | 8.0.2.0 | 8.0.2+57d78d17… |
| `Serilog.dll` | 140800 | 3.1.1.0 | 3.1.1-main-999d686d… |
| `Serilog.Extensions.Hosting.dll` | 29696 | 8.0.0.0 | 8.0.0+11a23e34… |
| `Serilog.Extensions.Logging.dll` | 29696 | 8.0.0.0 | 8.0.0+2138e8ca… |
| `Serilog.Formatting.Compact.dll` | 10240 | 2.0.0.0 | 2.0.0-main-efbbe3e |
| `Serilog.Settings.Configuration.dll` | 75776 | 8.0.2.0 | 8.0.2-main-d54b133… |
| `Serilog.Sinks.Console.dll` | 38912 | 5.0.0.0 | 5.0.0-main-4f61421 |
| `Serilog.Sinks.Debug.dll` | 7168 | 2.0.0.0 | 2.0.0-master-68cc513 |
| `Serilog.Sinks.File.dll` | 31232 | 5.0.0.0 | 5.0.0-main-7eb21bd… |

Copy-local does **not** mean `CreateLogger` ran. Presence of `Serilog.Sinks.File.dll` is **not** a file sink. Presence of `Serilog.Formatting.Compact.dll` is **not** CLEF. Presence of `Serilog.Settings.Configuration.dll` is **not** `ReadFrom.Configuration`.

`apps\mt5-worker\bin` and `apps\fix-worker\bin`: **no** `Serilog*` files.

### 3.4 Generated usings do not import Serilog

`D:\Prop\apps\api\obj\Debug\net8.0\TraderIntelligence.Api.GlobalUsings.g.cs` is the stock Web SDK list (`Microsoft.AspNetCore.*`, `Microsoft.Extensions.Logging`, `System.*`). No `global using Serilog`.

---

## 4. Package is not used — evidence

### 4.1 Worktree `Program.cs` (what would run from disk)

Composition, in order, as read (SHA `61B1E0D1…`, matches D06):

1. `WebApplication.CreateBuilder(args)` — default MEL.
2. `AddTraderIntelligence(builder.Configuration)` — options / connection-string bind **before** any logger swap.
3. `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`.
4. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
5. CORS `AllowAnyHeader` / `AllowAnyMethod` / `AllowAnyOrigin`.
6. `UseCors()`. Development: `UseSwagger()` only (no UI).
7. Fifteen anonymous maps (`/health`, `/api/*`, `/ready`).
8. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
9. `app.Run()`.

Token counts on the raw 4731-byte file (exact, case-sensitive):

| Token | Hits |
|---|---:|
| `Serilog` | **0** |
| `UseSerilog` | **0** |
| `AddSerilog` | **0** |
| `Log.Logger` | **0** |
| `LoggerConfiguration` | **0** |
| `using Serilog` | **0** |
| `CreateLogger` | **0** |
| `ReadFrom.Configuration` | **0** |
| `ILogger` | **0** |
| `AddSwaggerGen` | 1 |
| `UseSwagger` | 1 |
| `AddSignalR` / `MapHub` | **0** |

C04 counted `UseSerilog` = 0 on an earlier SHA of this host. D06 remeasured this SHA. This file is not a Serilog host that later waves missed.

Contrast: Swashbuckle moved from “referenced unused” (A06) to **partially used**. Serilog did not.

`AddTraderIntelligence` (`D:\Prop\src\Infrastructure\DependencyInjection.cs`) binds `GetConnectionString("TraderIntelligence")` / `DATABASE_URL` and registers InMemory-or-Npgsql. **No** Serilog registration. Current worktree `appsettings.json` uses `ConnectionStrings:Postgres` / `ConnectionStrings:Redis` — the `"TraderIntelligence"` key is **gone**, so the API will take the InMemory branch unless `DATABASE_URL` is set. That is a DI/config drift (D23), not a Serilog call.

### 4.2 HEAD `Program.cs` (committed)

`git show HEAD:apps/api/Program.cs` is the weatherforecast template. Also **zero** Serilog tokens. The package has been a dead reference since the commit that added the three NuGet lines.

### 4.3 Product C# has no Serilog types

Grep of every product `*.cs` under `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\tests` (exclude `bin`/`obj`) for `Serilog|UseSerilog|AddSerilog|Log\.Logger|LoggerConfiguration`:

**0 matches** across **85** files.

There is no `src/Infrastructure/Observability/`, no `SensitiveDataEnricher`, no `FixWireRedactor`, no `tests/Unit/Observability/SerilogPipelineTests.cs`. A50 §2 layout is spec-only.

Workers log through MEL only:

```csharp
_logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
```

```csharp
_logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

Those `ILogger<Worker>` instances are the generic host logger, not Serilog.

### 4.4 Config now *names* Serilog (unread)

This is the **measured drift vs C25** (C25 13:26:36 claimed **no** `"Serilog"` key). Worktree `D:\Prop\apps\api\appsettings.json` (full file, SHA `69D41CAD…`):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=trader_intelligence;Username=postgres;Password=",
    "Redis": "localhost:6379"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ]
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  },
  "CTraderFix": {
    "QuoteHost": "fix.ctrader.com",
    "QuotePort": 5201,
    "TradeHost": "fix.ctrader.com",
    "TradePort": 5202,
    "SenderCompId": "",
    "TargetCompId": "CSERVER",
    "HeartBeatInterval": 30,
    "ResetOnLogon": true,
    "FileStorePath": "./fixstore",
    "FileLogPath": "./fixlogs"
  },
  "RiskEngine": {
    "MaxDailyDrawdownPct": 5.0,
    "MaxPositionSize": 10.0,
    "MaxOpenPositions": 20,
    "KillSwitchEnabled": true,
    "KillSwitchOn": false,
    "StopNewExecutionOn": false,
    "EmergencyFlattenApiKey": ""
  },
  "FeatureFlags": {
    "ShadowTradingEnabled": true,
    "LiveCopyEnabled": false,
    "AutoPromotionEnabled": false
  },
  "AllowedHosts": "*"
}
```

`appsettings.Development.json` (478 bytes, **not in HEAD**): `"Serilog"` with `MinimumLevel:Debug` and `WriteTo: Console` only. No `Enrich` array.

`HEAD:apps/api/appsettings.json` is still:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

So: HEAD has MEL `"Logging"` and **no** Serilog. Worktree **replaced** `"Logging"` with an unread `"Serilog"` block. If the API is started from this worktree, Serilog still does not run; MEL has no appsettings level overrides.

A50 §8.3’s sketch is **closer** to the worktree JSON than C25 found, but it is still **not** a pipeline:

| A50 requirement | Worktree JSON | If someone later `UseSerilog`+`ReadFrom.Configuration` |
|---|---|---|
| Compact JSON / CLEF | `WriteTo: Console` (default text) | **Would not** emit `CompactJsonFormatter` |
| `FromLogContext` | listed | OK (built-in) |
| `WithMachineName` | listed | **Would fail** — `Serilog.Enrichers.Environment` is **not** a 8.0.2 transitive |
| `WithThreadId` | listed | **Would fail** — `Serilog.Enrichers.Thread` is **not** a 8.0.2 transitive |
| `SensitiveDataEnricher` last | absent | §57 redaction still **MISSING** |
| File sink off in prod | no file sink in JSON (DLL is still copy-local) | OK until someone adds `WriteTo: File` |
| Workers same pipeline | worker JSON is MEL-only | **MISSING** |

JSON-with-no-host-call is **not** “Serilog is used.” It is a future landmine: wiring `ReadFrom.Configuration` against this file without adding the two enricher packages will throw at startup.

`CTraderFix:FileLogPath` = `./fixlogs` is a **QuickFIX FileLog** path, not Serilog. A50 / A76: that sink is **UNSAFE** for tag 554 when QuickFIX is wired. It is not a Serilog usage.

`ConnectionStrings:Postgres` contains `Password=` (empty). Central redaction is still **MISSING**. Bind still happens in `AddTraderIntelligence` **before** any logger swap (A50 §3.2 inverted).

---

## 5. Binding law (what “used” would have to mean)

### 5.1 Architecture §5 — stack name is not an implementation

```text
C#
.NET 8+ compatible stack
ASP.NET Core
.NET Worker Services
Entity Framework Core or existing proven data layer
Npgsql
Serilog
OpenTelemetry
```

Listing Serilog in §5 does not make a `PackageReference` or a JSON key a pipeline.

### 5.2 Architecture §57 — structured ids + central redaction

Every relevant event should carry `correlation_id`, `broker_id`, `source_login`, `source_trade_id`, `copy_intent_id`, `risk_decision_id`, `execution_intent_id`, `cl_ord_id`, `cserver_order_id`, `destination_position_id`, `fix_session`.

> Never log authentication tags containing passwords.
>
> Redact sensitive values centrally.

None of those properties are enriched today. Default MEL will interpolate whatever a later call site puts in the template.

### 5.3 A50 — required host order (not present)

A50 §3.2 (mandatory when implementing):

```text
1. Create RedactionPolicy (static, no IConfiguration dump)
2. Create LoggerConfiguration
     .Destructure.With<SensitiveDestructuringPolicy>()
     .Enrich.FromLogContext()
     .Enrich.With<ServiceInstanceEnricher>()
     .Enrich.With<CorrelationIdEnricher>()
     .Enrich.With<ActivityBaggageEnricher>()
     .Enrich.With<SensitiveDataEnricher>()     // LAST enricher
     .WriteTo.Console(new CompactJsonFormatter())
3. Log.Logger = cfg.CreateLogger()
4. UseSerilog()
5. Only then Bind options / AddDbContext / AddQuickFix
```

Worktree `Program.cs` starts with `CreateBuilder` then immediately `AddTraderIntelligence(builder.Configuration)` — the **inverse** of step 5.

A50 §3.1 still says `Serilog.AspNetCore` 8.0.2 is “already referenced, unused.” That clause remains **true**. The parenthetical “`Program.cs` is still weatherforecast” is **false** on the worktree (C04 / C25 / D06). The gap did not close when the weather route died, and it did not close when the `"Serilog"` JSON appeared.

### 5.4 A102 pin (not this wave)

A102 wants `Serilog.AspNetCore` **8.0.3** (last 8.x) via CPM, workers on `Serilog.Extensions.Hosting` **8.0.0**, no 9.x/10.x. Disk is still inline **8.0.2** on the API only. Do not take NuGet “latest” 10.0.0.

---

## 6. Adjacent package honesty (same API csproj)

| Package | Version | Wired as a feature? | Class |
|---|---|---|---|
| `Serilog.AspNetCore` | 8.0.2 | **No.** Restored + copied. No host call. JSON unread. | **EXISTS_NEEDS_REFACTOR** |
| `Swashbuckle.AspNetCore` | 6.6.2 | **Partial.** `AddSwaggerGen` + `UseSwagger`. No `UseSwaggerUI`. | EXISTS_NEEDS_REFACTOR |
| `Microsoft.AspNetCore.SignalR.Common` | 8.0.4 | **No.** Common is not the hub host package. No `AddSignalR` / `MapHub`. | EXISTS_NEEDS_REFACTOR |

A06 “do not treat package references as implemented features” still applies to Serilog and SignalR.

---

## 7. Classification vs prior reports

| Capability | This measure (13:38:57) | C25 (13:26:36) | Drift |
|---|---|---|---|
| API `Serilog.AspNetCore` 8.0.2 reference | **Present** (HEAD = worktree, SHA `A5868FA8…`) | Same | **None** |
| `UseSerilog` / `AddSerilog` / product C# | **MISSING** (0 / 85 files) | Same | **None** |
| `Program.cs` SHA | `61B1E0D1…` / 4731 B (D06) | `E914FA98…` / 4658 B | Host grew; **still 0 Serilog tokens** |
| `Program.cs` weatherforecast | **GONE** on worktree; **present** on HEAD | Same | A50 host-shape still stale |
| `"Serilog"` appsettings block | **Present on worktree, unread** | C25: **MISSING** (431 B file, SHA `8DCE4CBE…`) | **Drift** — JSON added; pipeline did **not** appear |
| MEL `"Logging"` on API | **GONE** on worktree; **present** on HEAD | C25 worktree still had `"Logging"` + `CTrader:Password` | **Drift** — Logging replaced by dead Serilog JSON |
| `appsettings.Development.json` | 478 B, Serilog Debug, **not in HEAD** | C25: 127 B MEL-only SHA `73F95F9E…` | **Drift** |
| Worker Serilog | **MISSING** | Same | **None** |
| OTel packages | **MISSING** | Same (C26) | **None** |
| Redactor / enrichers / Observability tests | **MISSING** | Same (A50 / A76) | **None** |
| CPM / `Directory.Packages.props` | **MISSING** | Same (A102) | **None** |

A29 T05 `EXISTS_NEEDS_REFACTOR` for the package remains the correct §73 class. C25’s “zero `"Serilog"` keys” sentence is **stale**. C25’s “`Program` never uses it” sentence is **still true**.

---

## 8. Findings

| ID | Sev | Finding |
|---|---|---|
| D54-01 | — | **Answer:** Serilog **package is present** on the API (`Serilog.AspNetCore` 8.0.2). Serilog is **not used**. Zero C# call sites. JSON config (new since C25) is unread. Workers have nothing. |
| D54-02 | **MED** | Dead host package + dead `"Serilog"` JSON. Either wire per A50 (redaction first, then `UseSerilog`) or leave both and do **not** claim structured logging. Do not delete the package in a drive-by cleanup — it is the intended host package. |
| D54-03 | **MED** | Worktree `Enrich: WithMachineName, WithThreadId` names packages that **8.0.2 does not pull**. A naïve `UseSerilog` + `ReadFrom.Configuration` will throw. Add `Serilog.Enrichers.Environment` / `Serilog.Enrichers.Thread` only if A50 still wants those enrichers (A50 prefers custom §57 enrichers + Compact JSON, not this sketch). |
| D54-04 | **HIGH** (when FIX/options logging lands) | Bind order is already `AddTraderIntelligence` before any logger swap. `CTraderFix:FileLogPath` = `./fixlogs`. `ConnectionStrings:Postgres` has a `Password=` key. First `LogInformation("{@Options}", options)` or QuickFIX `FileLog` will violate §57. A76 / A50 still **MISSING**. |
| D54-05 | **MED** | Workers have no Serilog package. A50 requires `Serilog.Extensions.Hosting` 8.0.0 on both. Today they emit MEL template lines only. |
| D54-06 | **LOW** | Transitives from 8.0.2 (`Compact` 2.0.0, Console/File 5.0.0) are older than the A102 8.x pin table. Bump at CPM time (`AspNetCore` 8.0.3), not as a silent major. Do not take 9.x / 10.x. |
| D54-07 | **LOW** | C25 “no Serilog JSON” is stale (this file). A50 “`Program.cs` is still weatherforecast” is stale (C04/D06). Keep “no `UseSerilog`” as the evidence sentence. |

---

## 9. What a later coding wave must do (not done here)

Follow A50 §11. This agent did **not** implement any of it.

1. RedactionPolicy + FixWireRedactor + A50 §10 unit tests **before** the first real Logon.
2. `UseSerilog` on API; `Serilog.Extensions.Hosting` + `UseSerilog` on both workers.
3. Compact JSON stdout. File sink **off** in prod. Do not treat QuickFIX `FileLogPath` as the product logger.
4. Enrichers; `SensitiveDataEnricher` last. Do not ship `WithMachineName`/`WithThreadId` without the matching packages — or drop those names and implement A50 enrichers instead.
5. Then bind `CTraderFixOptions` / connection strings / QuickFIX.
6. Pin 8.x via `Directory.Packages.props` (A102). Do not take Serilog 9/10.
7. Do **not** add Seq/Elasticsearch/OpenTelemetry in the same PR unless A50 §11 steps 1–3 are already green.

---

## 10. What this agent did **not** do

- Did not edit any product `*.cs` / `*.csproj` / `appsettings*.json`.
- Did not add `UseSerilog`, did not bump 8.0.2 → 8.0.3, did not create `Directory.Packages.props`.
- Did not add worker packages.
- Did not `dotnet run` the API. “MEL is the active logger” is inferred from the absence of Serilog host calls plus `CreateBuilder` defaults, not from a captured stdout line.
- Did not implement A50 / A76 / OTel.

---

## 11. One-line close

**`Serilog.AspNetCore` 8.0.2 is a committed, restored, copy-local API package; worktree `appsettings` now contains an unread `"Serilog"` block (C25 stale); worktree and HEAD `Program.cs` both contain zero Serilog API calls; workers have no Serilog at all; structured §57 logging is MISSING.**
