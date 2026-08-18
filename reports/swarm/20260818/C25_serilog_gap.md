# C25 — Serilog package is on the API; `Program` does not use it

| Field | Value |
|---|---|
| Agent | C25 (senior engineer, Serilog wiring gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:26:36+05:30 |
| Workspace | `D:\Prop` |
| Assigned | Confirm Serilog package on API but `Program` does not use it yet. Write this report. **Do not modify product source.** |
| Primary files | `D:\Prop\apps\api\TraderIntelligence.Api.csproj`, `D:\Prop\apps\api\Program.cs` |
| Adjacent hosts | `D:\Prop\apps\mt5-worker\`, `D:\Prop\apps\fix-worker\` |
| Law | Architecture v2 §5 (Serilog in the stack), §57 (structured ids + central redaction), A50 (binding Serilog / OTel spec) |
| Relates | A06, A29 T05, A50, A55, A76, A102, B06 §3.2, C04 (`UseSerilog` = 0) |
| Product source modified | **No.** This report is the only write. |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`) |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**Confirmed. The API references `Serilog.AspNetCore` 8.0.2. `Program.cs` never uses it.**

The package is a real, restored, copied-to-output NuGet reference (csproj + `project.assets.json` + `TraderIntelligence.Api.deps.json` + nine `Serilog*.dll` under `bin\Debug\net8.0`). It is **not** a comment, a CPM pin-only name, or a transitive leak from another project. It is also **not** a feature. There is no `using Serilog`, no `UseSerilog()`, no `AddSerilog()`, no `Log.Logger`, no `LoggerConfiguration`, no `"Serilog"` JSON block, no enricher, no redactor, and no `ILogger<T>` in the API host.

`Program.cs` is **not** the stock weatherforecast template anymore (that claim in A50 is **stale**). The current host maps demo dashboard routes, Swagger, CORS, and `AddTraderIntelligence`. Swashbuckle is now **partially** wired. Serilog is still a **dead reference**.

| Question | Answer |
|---|---|
| Does `apps/api` reference a Serilog package? | **Yes.** Direct `<PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />` (csproj line 11). Same line in HEAD and worktree. |
| Is that package restored / on the compile+runtime graph? | **Yes.** `obj\project.assets.json` `Serilog.AspNetCore/8.0.2`; deps.json lists it as a host dependency; nine Serilog assemblies sit next to `TraderIntelligence.Api.dll`. |
| Does `Program.cs` call `UseSerilog` / `AddSerilog` / `Log.Logger`? | **No. Zero hits** on the worktree file (95 physical lines) and on HEAD (weatherforecast template). |
| Does any other product `*.cs` mention Serilog? | **No.** Grep of `apps/`, `src/`, `tests/` `*.cs` (exclude `bin`/`obj`) → **0** matches. |
| Which product `*.csproj` files reference Serilog? | **Exactly one:** `D:\Prop\apps\api\TraderIntelligence.Api.csproj`. Workers, Domain, Application, Infrastructure, Fix.CTrader, Mt5, both test projects: **none**. |
| Is there a `Directory.Packages.props` pin? | **No.** File does not exist. Version is inline `8.0.2`. A102 wants CPM `8.0.3`; that is not on disk. |
| Does config select Serilog sinks / levels? | **No.** Both API `appsettings*.json` files have only MEL `"Logging": { "LogLevel": … }`. Zero `"Serilog"` keys. |
| What logger actually runs if the API starts? | Default ASP.NET Core / MEL console logger from `WebApplication.CreateBuilder`. Serilog DLLs load as unused assemblies. |
| Are workers on Serilog? | **No.** `Microsoft.Extensions.Hosting` 8.0.1 only. `Worker` classes use `ILogger<Worker>` (MEL). No Serilog DLLs under either worker `bin\`. |
| OpenTelemetry? | **MISSING.** No `OpenTelemetry*` package, no `Meter`, no OTLP. |
| Central redaction (A50 / A76 / §57)? | **MISSING.** Comment on `CTraderFixOptions.Password` is not a control. `appsettings.json` already has `"CTrader": { "Password": "" }`. |
| Classification of the API Serilog reference | **`EXISTS_NEEDS_REFACTOR`** (package present, host unused). Pipeline itself is **`MISSING`**. |

Do **not** delete the package as cleanup. Do **not** treat the DLL copy as “logging implemented.” Wire it later per A50 (redaction **before** options bind), or the first `{@Options}` / QuickFIX `FileLog` will print tag **554**.

---

## 1. Method

| Source | Action |
|---|---|
| API csproj | Full read of `D:\Prop\apps\api\TraderIntelligence.Api.csproj` (21 physical lines). |
| API host | Full read of `D:\Prop\apps\api\Program.cs` (95 physical / 86 non-blank). Token counts via .NET regex on the raw file. |
| API config | Full read of `appsettings.json` + `appsettings.Development.json`. `Select-String Serilog` → no hits. |
| Restore graph | `apps\api\obj\project.assets.json` (`Serilog.AspNetCore/8.0.2` + 8 transitive Serilog packages); `TraderIntelligence.Api.csproj.nuget.dgspec.json` line 60; `bin\Debug\net8.0\TraderIntelligence.Api.deps.json` host `dependencies`. |
| Output assemblies | `Get-ChildItem bin\Debug\net8.0\Serilog*.dll` + `FileVersionInfo`. |
| Product-wide grep | `Serilog` / `UseSerilog` / `AddSerilog` / `Log.Logger` / `LoggerConfiguration` / `OpenTelemetry` over `apps/`, `src/`, `tests/` `*.cs` and `*.csproj` (exclude `bin`/`obj`). |
| Workers | Full read of both `Program.cs`, both `Worker.cs`, both worker csproj, both worker `appsettings.json`. Worker `bin` Serilog glob → empty. |
| Binding specs | Architecture §5 lines 216–231, §57 lines 2108–2133; A50 §0 / §3; A102 §6.3; C04 token table; B06 §3.2. |
| Git | `git rev-parse HEAD`; `git show HEAD:apps/api/Program.cs`; `git show HEAD:apps/api/TraderIntelligence.Api.csproj`; `git hash-object` vs `HEAD:` blobs. |
| Observability tree | Recursive search for `Observability/` and product `Logging/` under `src/` + `apps/` → **none**. |

No `dotnet add`, no `UseSerilog` patch, no package bump, no `appsettings` edit, no process launch.

---

## 2. Measured files (non-`bin` / non-`obj`)

SHA-256 via `Get-FileHash`. Bytes = `Length`. Physical lines = `Get-Content.Count`.

### 2.1 API host

| Path | Bytes | Physical lines | SHA-256 |
|---|---:|---:|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | 21 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\Program.cs` | 4658 | 95 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` |
| `D:\Prop\apps\api\appsettings.json` | 431 | 21 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | 8 | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | 41 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` |

csproj worktree blob `188adce4e084ac942d2c75f6df9bd090b172512c` == `HEAD:apps/api/TraderIntelligence.Api.csproj`. The Serilog line is **committed** (initial commit `6c414477`, 2026-08-18). It was never wired after that.

`Program.cs` worktree blob `42abd595e2114fbce2a86090213d7f0d68306e64` ≠ HEAD blob `93279323bc65ad18f3190fdb601b7fea3b4b027e`. Worktree is the demo BFF (C04). HEAD is still the weatherforecast template. **Neither** file uses Serilog. `git status`: ` M apps/api/Program.cs` (and ` M apps/api/appsettings.json`).

Hashes for `Program.cs` / csproj / both appsettings match C04’s table. Same snapshot.

### 2.2 Workers (no Serilog surface)

| Path | Bytes | SHA-256 | Serilog? |
|---|---:|---|---|
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | **No** package. Only `Microsoft.Extensions.Hosting` 8.0.1. |
| `D:\Prop\apps\mt5-worker\Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | No `UseSerilog`. |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | **No** package. |
| `D:\Prop\apps\fix-worker\Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | No `UseSerilog`. |

Matches C07 inventory. Worker `appsettings.json` is MEL-only (`AB16B7B75D012475E615A41C21000C9215C6E02CD70B9C2618D25D885AA6FF33`).

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

This is the **only** `Serilog*` `PackageReference` in the whole product tree (every `*.csproj` under `D:\Prop` excluding `bin`/`obj`/`_tmp`).

`Directory.Build.props` exists and does **not** mention Serilog. `Directory.Packages.props` **does not exist**. Version is not centrally pinned.

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

Those transitive versions are **what 8.0.2 actually pulls**. They are **not** the A102 CPM table (`Formatting.Compact` 3.0.0, `Sinks.Console`/`File` 6.0.0, `Settings.Configuration` 8.0.4). When a later wave wires Serilog, bump via CPM — do not freeze 8.0.2’s older transitives as the product pin.

`D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.deps.json` host dependencies include `"Serilog.AspNetCore": "8.0.2"` next to SignalR.Common and Swashbuckle. 61 `Serilog` string hits in that file. The graph is live.

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

Copy-local does **not** mean `CreateLogger` ran. Presence of `Serilog.Sinks.File.dll` is **not** a file sink. Presence of `Serilog.Formatting.Compact.dll` is **not** CLEF.

`apps\mt5-worker\bin` and `apps\fix-worker\bin`: **no** `Serilog*` files.

### 3.4 Generated usings do not import Serilog

`D:\Prop\apps\api\obj\Debug\net8.0\TraderIntelligence.Api.GlobalUsings.g.cs` is the stock Web SDK list (`Microsoft.AspNetCore.*`, `Microsoft.Extensions.Logging`, `System.*`). No `global using Serilog`.

---

## 4. `Program` does not use it — evidence

### 4.1 Worktree `Program.cs` (what would run from disk)

Composition, in order, as read:

1. `WebApplication.CreateBuilder(args)` — default MEL.
2. `AddTraderIntelligence(builder.Configuration)`.
3. `ConfigureHttpJsonOptions` + `JsonStringEnumConverter`.
4. `AddEndpointsApiExplorer` + `AddSwaggerGen`.
5. CORS `AllowAnyHeader` / `AllowAnyMethod` / `AllowAnyOrigin`.
6. `UseCors()`. Development: `UseSwagger()` only (no UI).
7. Fifteen anonymous maps (`/health`, `/api/*`, `/ready`).
8. Startup scope: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`.
9. `app.Run()`.

Token counts on the raw 4658-byte file (exact, case-sensitive):

| Token | Hits |
|---|---:|
| `Serilog` | **0** |
| `UseSerilog` | **0** |
| `AddSerilog` | **0** |
| `Log.Logger` | **0** |
| `LoggerConfiguration` | **0** |
| `using Serilog` | **0** |
| `ILogger` | **0** |
| `AddSwaggerGen` | 1 |
| `UseSwagger` | 1 |
| `AddSignalR` / `MapHub` | **0** |

C04 independently counted `UseSerilog` = 0 on the **same SHA**. This file is not a Serilog host that C04 missed.

Contrast: Swashbuckle moved from “referenced unused” (A06 / early B06) to **partially used**. Serilog did not.

### 4.2 HEAD `Program.cs` (committed)

`git show HEAD:apps/api/Program.cs` is the template:

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
app.UseHttpsRedirection();
app.MapGet("/weatherforecast", () => { … });
app.Run();
```

Also **zero** Serilog tokens. The package has been a dead reference since the initial commit that added the three NuGet lines.

### 4.3 Config is MEL, not Serilog

`appsettings.json` (full logging + secrets-adjacent block):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "TraderIntelligence": ""
  },
  "CTrader": {
    "Host": "live-us-eqx-01.p.c-trader.com",
    "AccountId": "1369850",
    "Password": "",
    "UseSsl": true,
    "QuoteEnabled": true,
    "TradeSessionEnabled": true,
    "RealCopyExecutionEnabled": false
  }
}
```

`Select-String -Pattern Serilog` on both API appsettings files: **no matches**.

A50 §8.3’s `"Serilog": { "MinimumLevel": … }` sketch is **not** in product config. Neither is `"OpenTelemetry"`.

`CTrader:Password` is empty today. The key **exists**. A50 §3.2: wire redaction **before** options bind so a later non-empty password never hits console. That order is not implemented.

### 4.4 Product C# has no Serilog types

Grep of every product `*.cs` under `D:\Prop\apps`, `D:\Prop\src`, `D:\Prop\tests` (exclude `bin`/`obj`) for `Serilog|UseSerilog|AddSerilog|Log\.Logger`:

**0 matches.**

There is no `src/Infrastructure/Observability/`, no `SensitiveDataEnricher`, no `FixWireRedactor`, no `tests/Unit/Observability/SerilogPipelineTests.cs`. A50 §2 layout is spec-only.

Workers log through MEL only:

```csharp
_logger.LogInformation("MT5 ingestion worker started. Execution copy is not performed here.");
```

```csharp
_logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);
```

Those `ILogger<Worker>` instances are the generic host logger, not Serilog.

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

Listing Serilog in §5 does not make a `PackageReference` a pipeline.

### 5.2 Architecture §57 — structured ids + central redaction

Every relevant event should carry `correlation_id`, `broker_id`, `source_login`, `source_trade_id`, `copy_intent_id`, `risk_decision_id`, `execution_intent_id`, `cl_ord_id`, `cserver_order_id`, `destination_position_id`, `fix_session`.

> Never log authentication tags containing passwords.
>
> Redact sensitive values centrally.

None of those properties are enriched today. Default MEL will happily interpolate whatever a later call site puts in the template.

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

Worktree `Program.cs` starts with `CreateBuilder` then immediately `AddTraderIntelligence(builder.Configuration)` — the **inverse** of step 5. Options / connection-string bind happens with the default logger and **no** redactor.

A50 §3.1 still says `Serilog.AspNetCore` 8.0.2 is “already referenced, unused.” That clause remains **true**. The parenthetical “`Program.cs` is still weatherforecast” is **false** on the worktree (C04 / this file). The gap did not close when the weather route died.

A06 / A50: wire Serilog + `correlation_id` **before** any FIX/MT5 options exist so passwords never hit console. `CTraderFixOptions.Password` is already documented “Must never be logged.” That XML comment is not a sink filter.

### 5.4 A102 pin (not this wave)

A102 wants `Serilog.AspNetCore` **8.0.3** (last 8.x) via CPM, workers on `Serilog.Extensions.Hosting` **8.0.0**, no 9.x/10.x. Disk is still inline **8.0.2** on the API only. Do not take NuGet “latest” 10.0.0.

---

## 6. Adjacent package honesty (same csproj)

| Package | Version | Wired as a feature? | Class |
|---|---|---|---|
| `Serilog.AspNetCore` | 8.0.2 | **No.** Restored + copied. No host call. | **EXISTS_NEEDS_REFACTOR** |
| `Swashbuckle.AspNetCore` | 6.6.2 | **Partial.** `AddSwaggerGen` + `UseSwagger`. No `UseSwaggerUI`. | EXISTS_NEEDS_REFACTOR (docs UI still missing) |
| `Microsoft.AspNetCore.SignalR.Common` | 8.0.4 | **No.** Common is not the hub host package. No `AddSignalR` / `MapHub`. | EXISTS_NEEDS_REFACTOR (wrong package + unused) |

A06 “do not treat package references as implemented features” still applies to Serilog and SignalR. It no longer applies equally to Swashbuckle.

---

## 7. Classification vs prior reports

| Capability | This measure | Prior note | Drift |
|---|---|---|---|
| API `Serilog.AspNetCore` 8.0.2 reference | **Present** (HEAD = worktree) | A06, A29 T05, A50, A55, B06, C04 | **None** |
| `UseSerilog` / `AddSerilog` | **MISSING** | Same | **None** |
| `Program.cs` weatherforecast | **GONE** on worktree; **present** on HEAD | A50 still says weatherforecast | A50 host-shape **stale**; Serilog gap **not** stale |
| Swashbuckle | Partial | A06 unused | **Drift** — Swagger gen is on |
| Worker Serilog | **MISSING** | A50, B07, C07 | **None** |
| OTel packages | **MISSING** | A50 | **None** |
| Redactor / enrichers / Observability tests | **MISSING** | A50 §10, A76 | **None** |
| `"Serilog"` appsettings block | **MISSING** | A50 §8.3 sketch only | **None** |
| CPM / `Directory.Packages.props` | **MISSING** | A102 spec | **None** |

A29 T05 `EXISTS_NEEDS_REFACTOR` for Serilog remains the correct §73 class.

---

## 8. Findings

| ID | Sev | Finding |
|---|---|---|
| C25-01 | — | **Confirmed:** API has `Serilog.AspNetCore` 8.0.2. `Program` does not use it. Package restored, DLLs copied, zero C# call sites. |
| C25-02 | **MED** | Dead host package. Either wire per A50 (redaction first) or leave it and do **not** claim structured logging. Do not delete in a drive-by cleanup — the name is the intended host package. |
| C25-03 | **HIGH** (when FIX/options logging lands) | Bind order is already `AddTraderIntelligence` before any logger swap. `CTrader:Password` key exists in `appsettings.json`. First `LogInformation("{@Options}", options)` or QuickFIX `FileLog` will violate §57. A76 / A50 still **MISSING**. |
| C25-04 | **MED** | Workers have no Serilog package. A50 requires `Serilog.Extensions.Hosting` 8.0.0 on both. Today they emit MEL template lines only. |
| C25-05 | **LOW** | Transitives from 8.0.2 (`Compact` 2.0.0, Console/File 5.0.0) are older than the A102 8.x pin table. Bump at CPM time (`AspNetCore` 8.0.3), not as a silent major. |
| C25-06 | **LOW** | A50 §0 sentence “`Program.cs` is still weatherforecast” is stale (C04). Do not use that clause as evidence. Use “no `UseSerilog`” instead. |

---

## 9. What a later coding wave must do (not done here)

Follow A50 §11. This agent did **not** implement any of it.

1. RedactionPolicy + FixWireRedactor + A50 §10 unit tests **before** the first real Logon.
2. `UseSerilog` on API; `Serilog.Extensions.Hosting` + `UseSerilog` on both workers.
3. Compact JSON stdout. File sink **off** in prod.
4. Enrichers; `SensitiveDataEnricher` last.
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

**`Serilog.AspNetCore` 8.0.2 is a committed, restored, copy-local API package; worktree and HEAD `Program.cs` both contain zero Serilog API calls; workers have no Serilog at all; structured §57 logging is MISSING.**
)
