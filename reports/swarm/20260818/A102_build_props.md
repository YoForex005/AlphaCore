# A102 — `Directory.Build.props` for net8 / nullable / implicit usings, plus extra NuGet pins

| Field | Value |
|---|---|
| Agent | A102 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A102_build_props.md` |
| Workspace | `D:\Prop` |
| Product source modified | **No.** This file is a recommendation only. |
| Peers (do not contradict) | A11 (solution hygiene), A30 I0 (`Directory.Build.props`), A35 (QuickFIX/n pin), A50 (Serilog 8.x), A02 (`FluentValidation` 11.9.2), A03 (EF / Npgsql 8.0.4) |

This report answers two questions:

1. What should `Directory.Build.props` contain for this `net8.0` solution?
2. Which **extra** NuGet versions to pin (Serilog, EF **8.0.4**, QuickFIX), given **FluentValidation is already 11.9.2**?

It does **not** edit `D:\Prop\Directory.Build.props`, any `.csproj`, or introduce `Directory.Packages.props`. A later increment (A30 I0) applies this.

---

## 0. Verdict (pin this)

| Item | Decision |
|---|---|
| Root `Directory.Build.props` | **Keep and strengthen** at `D:\Prop\Directory.Build.props`. Inherit `net8.0`, `Nullable=enable`, `ImplicitUsings=enable`. |
| `TargetFramework` | **`net8.0`** in the root props. Drop the ten duplicated copies from `.csproj` when applying. |
| `LangVersion` | **`12`** (C# 12, the net8 language). Do **not** leave `latest` — an SDK 9/10 box would compile C# 13/14 into a net8 binary. |
| Warnings | `TreatWarningsAsErrors=true` on product (`src/`, `apps/`). `false` on `tests/` via `Directory.Build.targets`. |
| Package versions | **Not** in `Directory.Build.props`. Put them in a sibling **`Directory.Packages.props`** (CPM). |
| FluentValidation | **Keep 11.9.2.** Do not take 11.11 / 11.12 / **12.x**. |
| EF family | **Stay on 8.0.4** (already in tree). Do not take 8.0.19+, 9.x, or 10.x. |
| Serilog | Stay on the **8.x** line that matches net8. Pin `Serilog.AspNetCore` **8.0.3** (last 8.x). Do not take 9.0.0 / 10.0.0. |
| QuickFIX | **Replace** unofficial `QuickFix.Net` 1.8.0 with official **`QuickFIXn.Core` 1.14.1** + **`QuickFIXn.FIX44` 1.14.1** (A35). |
| `global.json` | Add at repo root, SDK `8.0.100` + `rollForward: latestFeature`. No `allowPrerelease`. |

NuGet.org “latest” on 2026-08-18 is **not** the pin: EF latest is **10.0.11**, Serilog.AspNetCore latest is **10.0.0**, FluentValidation latest is **12.1.1**. Those are net10 / major-bump lines. This product is net8 until a coordinated TFM move.

---

## 1. Measured current state (honest)

A11 (same day, earlier) said there was no `Directory.Build.props`. That is **stale**. On disk now:

| Path | State |
|---|---|
| `D:\Prop\Directory.Build.props` | **EXISTS** — incomplete |
| `D:\Prop\Directory.Packages.props` | **MISSING** |
| `D:\Prop\Directory.Build.targets` | **MISSING** |
| `D:\Prop\global.json` | **MISSING** |
| `D:\Prop\nuget.config` | **MISSING** |

Current `Directory.Build.props` (verbatim):

```xml
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

Gaps vs A30 I0 and this pin:

| Property | Now | Should be |
|---|---|---|
| `TargetFramework` | absent (duplicated in every `.csproj`) | `net8.0` here |
| `LangVersion` | `latest` | `12` |
| `TreatWarningsAsErrors` | `false` | `true` on product |
| CPM | versions in 10 `.csproj` files | `Directory.Packages.props` |

All ten product `.csproj` files already set `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`. Inheritance would make those three lines redundant. They do not hurt if left until a cleanup PR.

C++ `mt5-sdk/` and Vite `apps/web/` ignore MSBuild props. No change required there.

---

## 2. Recommended `Directory.Build.props`

Path: `D:\Prop\Directory.Build.props` (repo root, next to `Mt5TraderIntelligence.sln`). MSBuild auto-imports this for every project under the root.

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>12</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <EnforceCodeStyleInBuild>false</EnforceCodeStyleInBuild>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>8</AnalysisLevel>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>moderate</NuGetAuditLevel>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>
</Project>
```

### Why each property

| Property | Value | Why |
|---|---|---|
| `TargetFramework` | `net8.0` | Architecture / A30 / every existing csproj. One place to change when (later) moving to net10. |
| `LangVersion` | `12` | Official C# for net8. `latest` follows the **installed SDK**, not the TFM. |
| `Nullable` | `enable` | Already on all ten projects. NRT is required for the risk/FIX/quantity code. |
| `ImplicitUsings` | `enable` | Already on. `Microsoft.NET.Sdk` / `.Web` / `.Worker` each get the right global-using set. |
| `TreatWarningsAsErrors` | `true` | A30 I0. Overridden to `false` for tests (see §3). |
| `Deterministic` | `true` | Already on. Reproducible Release builds. |
| `ContinuousIntegrationBuild` | when `CI=true` | Stable paths / SourceLink when a CI job sets the env var. Harmless locally. |
| `EnforceCodeStyleInBuild` | `false` | Do not fail the build on IDE000x / formatting until an `.editorconfig` exists. |
| `EnableNETAnalyzers` + `AnalysisLevel` | `true` / `8` | CA rules at the net8 set. Do not set `latest` (that tracks the SDK). |
| `NuGetAudit*` | on / `all` / `moderate` | Surface vulnerable transitive packages. Fail the build only after the first audit baseline is green. |
| `ManagePackageVersionsCentrally` | `true` | Versions live in `Directory.Packages.props`. Csproj `PackageReference` items lose their `Version=` attribute. |
| `CentralPackageTransitivePinningEnabled` | `true` | Stops a transitive bump (e.g. QF/n pulling `Microsoft.Extensions.Logging.Abstractions` 8.0.3 vs 8.0.0) from floating. |

### What must **not** go in `Directory.Build.props`

- `PackageReference` / `PackageVersion` items — that is CPM (`Directory.Packages.props`).
- `UserSecretsId` — per-host, already on the two workers.
- `IsPackable` / `IsTestProject` — per-project.
- RuntimeIdentifiers, Docker, or PublishAot — none of the hosts need them in v1.

---

## 3. Recommended `Directory.Build.targets` (test exception)

`Directory.Build.props` is imported **before** the project body, so `$(IsTestProject)` is still unset there. Put the override in `D:\Prop\Directory.Build.targets` (imported **after** the csproj):

```xml
<Project>
  <PropertyGroup Condition="'$(IsTestProject)' == 'true'">
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
</Project>
```

`tests/Unit` and `tests/Integration` already set `IsTestProject=true`. Product projects stay strict.

Do **not** add a second `tests/Directory.Build.props` unless a later test project sits outside `tests/` — a child `Directory.Build.props` replaces the parent unless it `<Import>`s it, which is a common foot-gun.

---

## 4. Recommended `global.json`

```json
{
  "sdk": {
    "version": "8.0.100",
    "rollForward": "latestFeature",
    "allowPrerelease": false
  }
}
```

`latestFeature` accepts any installed 8.0.x (8.0.404, 8.0.415, …) and rejects SDK 9/10. Pair with `LangVersion=12` so a leftover SDK 10 on the PATH cannot change language or default package graphs.

Dockerfiles in A65 already use `mcr.microsoft.com/dotnet/sdk:8.0` / `aspnet:8.0`. Keep that. When CPM is added, those Dockerfiles must also `COPY Directory.Packages.props ./` next to the existing `COPY Directory.Build.props ./` line (A65 §5.1).

---

## 5. Extra NuGet pins (CPM)

File: `D:\Prop\Directory.Packages.props`. After this exists, every `PackageReference` in a csproj **must drop `Version=`**.

```xml
<Project>
  <ItemGroup>
    <!-- Validation — already 11.9.2; do not bump -->
    <PackageVersion Include="FluentValidation" Version="11.9.2" />
    <PackageVersion Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />

    <!-- EF Core 8 LTS family — stay on 8.0.4 together -->
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="8.0.4" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.4" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.4" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Relational" Version="8.0.4" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />

    <!-- Official QuickFIX/n — last net8 engine (A35). Not QuickFix.Net. -->
    <PackageVersion Include="QuickFIXn.Core" Version="1.14.1" />
    <PackageVersion Include="QuickFIXn.FIX44" Version="1.14.1" />

    <!-- Serilog 8.x (matches net8). Do not take 9.x / 10.x. -->
    <PackageVersion Include="Serilog.AspNetCore" Version="8.0.3" />
    <PackageVersion Include="Serilog.Extensions.Hosting" Version="8.0.0" />
    <PackageVersion Include="Serilog.Settings.Configuration" Version="8.0.4" />
    <PackageVersion Include="Serilog.Formatting.Compact" Version="3.0.0" />
    <PackageVersion Include="Serilog.Enrichers.Environment" Version="3.0.1" />
    <PackageVersion Include="Serilog.Enrichers.Thread" Version="4.0.0" />
    <PackageVersion Include="Serilog.Sinks.Console" Version="6.0.0" />
    <PackageVersion Include="Serilog.Sinks.File" Version="6.0.0" />

    <!-- Hosts / ASP.NET 8 — already referenced; pin so they cannot float to 9/10 -->
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="8.0.1" />
    <PackageVersion Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.3" />

    <!-- Already in tree; centralize so they cannot drift -->
    <PackageVersion Include="Swashbuckle.AspNetCore" Version="6.6.2" />
    <PackageVersion Include="StackExchange.Redis" Version="2.8.0" />

    <!-- Test stack — already in tree -->
    <PackageVersion Include="coverlet.collector" Version="6.0.0" />
    <PackageVersion Include="FluentAssertions" Version="6.12.0" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageVersion Include="Moq" Version="4.20.70" />
    <PackageVersion Include="xunit" Version="2.5.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="2.5.3" />
  </ItemGroup>
</Project>
```

`FluentValidation.DependencyInjectionExtensions`, `Microsoft.EntityFrameworkCore` (runtime), `Microsoft.EntityFrameworkCore.Relational`, the official QuickFIX pair, and the extra Serilog packages are **not yet direct references** (except `Serilog.AspNetCore` 8.0.2 on the API). CPM may list them before a csproj consumes them. That is the point of the pin table.

Csproj consumption stays explicit. Domain stays package-free (A01 / A50). Do not add a blanket `PackageReference` in `Directory.Build.props`.

---

## 6. Per-package rationale

### 6.1 FluentValidation — **already 11.9.2, keep it**

| | |
|---|---|
| Where now | `src/Application/TraderIntelligence.Application.csproj` — `FluentValidation` **11.9.2** |
| Use | Unused so far (A02). Validators belong in Application when written. |
| Pin | **11.9.2** |
| Companion (when wiring DI) | `FluentValidation.DependencyInjectionExtensions` **11.9.2** (same major.minor.patch) |
| Do not add | `FluentValidation.AspNetCore` (legacy MVC filter pipeline; not used) |

NuGet latest on 2026-08-18 is **12.1.1** (12.0.0 = 2025-05-05, breaking). 11.x continued as 11.11.0 / 11.12.0. **Do not take 12** in v1: net8-only TFM on 12.x, upgrade guide required, no product validators exist yet so there is no benefit. **Do not silently patch 11.9.2 → 11.12.0** either — freeze what the Application csproj already restored.

### 6.2 EF Core + Npgsql — **8.0.4, stay**

| Package | In tree | Pin |
|---|---|---|
| `Microsoft.EntityFrameworkCore.Design` | Infrastructure 8.0.4 (`PrivateAssets=all`) | **8.0.4** |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure 8.0.4 | **8.0.4** |
| `Microsoft.EntityFrameworkCore.InMemory` | Integration tests 8.0.4 | **8.0.4** |
| `Microsoft.EntityFrameworkCore` | transitive only | **8.0.4** (pin transitive) |
| `Microsoft.EntityFrameworkCore.Relational` | transitive only | **8.0.4** |

Microsoft’s rule (quoted on the 8.0.4 nupkg): *install the same version of all EF Core packages*. Mixing Design 8.0.4 with InMemory 8.0.19 (or Relational 10.0.11) is unsupported.

Why **not** the latest 8.0.x patch (8.0.19 / 8.0.20 exist on the 8 LTS train):

- The user / repo already chose **8.0.4**.
- A03 recorded 8.0.4 as the aligned set.
- EF8 is LTS until **2026-11-10**. 8.0.4 is still on that train.
- A later security increment can bump the **entire** 8.0.x family together (Design + InMemory + Relational + Npgsql EF). Never bump one package.

Why **not** 9.x / 10.x:

- Current NuGet latest `Microsoft.EntityFrameworkCore` is **10.0.11** (net10 only).
- Current `Npgsql.EntityFrameworkCore.PostgreSQL` latest is **10.0.3** and requires `Microsoft.EntityFrameworkCore >= 10.0.4 && < 11.0.0`.
- That is a TFM + provider + migration rewrite, not a pin tweak.

Keep `PrivateAssets=all` on `Microsoft.EntityFrameworkCore.Design` in the Infrastructure csproj (assets metadata stays in the csproj; only `Version=` moves to CPM).

Do **not** add SQL Server / SQLite / Cosmos providers.

### 6.3 Serilog — **8.x only, AspNetCore 8.0.3**

Official versioning (Serilog.AspNetCore readme, also A50): *if you target .NET 8.x, choose an 8.x Serilog.AspNetCore*.

| Package | Role | Pin | In tree today |
|---|---|---|---|
| `Serilog.AspNetCore` | API host (`UseSerilog` / `AddSerilog`) | **8.0.3** | **8.0.2** on `apps/api` (referenced, unused — A50) |
| `Serilog.Extensions.Hosting` | both workers | **8.0.0** (last 8.x of this id) | missing |
| `Serilog.Settings.Configuration` | `appsettings` | **8.0.4** | missing (transitive via AspNetCore) |
| `Serilog.Formatting.Compact` | CLEF / `CompactJsonFormatter` (A50 §3.4) | **3.0.0** | missing (transitive) |
| `Serilog.Enrichers.Environment` | `MachineName` | **3.0.1** | missing (transitive) |
| `Serilog.Enrichers.Thread` | `ThreadId` (debug only) | **4.0.0** | missing (transitive) |
| `Serilog.Sinks.Console` | stdout | **6.0.0** | missing (transitive) |
| `Serilog.Sinks.File` | lab only; **off in prod** (A50) | **6.0.0** | missing (transitive) |

`Serilog.AspNetCore` 8.x history (nuget.org, 2026-08-18):

| Version | Date | Note |
|---|---|---|
| 8.0.2 | 2024-07-31 | **Currently referenced** by the API; named by A50 |
| **8.0.3** | 2024-10-11 | **Last 8.x.** Pin this. |
| 9.0.0 | 2024-12-09 | net9 line — skip |
| 10.0.0 | 2025-11-28 | net10 line; current “latest” — skip |

8.0.2 → 8.0.3 is a patch on the same TFM train. Prefer 8.0.3 in CPM over freezing the slightly older 8.0.2 that happens to sit in the csproj. If a later agent wants zero package churn in the same PR as CPM, 8.0.2 is an acceptable temporary pin — **do not** leave it once Serilog is actually wired.

`Serilog.Extensions.Hosting` 8.0.0 is the last 8.x of that package; 9.0.0 / 10.0.0 pull `Microsoft.Extensions.*` 9/10. Workers must reference **8.0.0** directly (A50 §3.1). The API does **not** add it — `Serilog.AspNetCore` already depends on it.

Do **not** add Seq / Elasticsearch / OpenTelemetry packages in this pin set (A50: stdout compact JSON first). OTel is a later increment with its own versions, still on the 8.x Microsoft.Extensions line.

Do **not** add `Serilog` (the core package) as a direct pin unless a class library logs without the host packages. Prefer `ILogger<T>` from MEL.

### 6.4 QuickFIX — official 1.14.1, not `QuickFix.Net` 1.8.0

| Package | Pin | Why |
|---|---|---|
| `QuickFIXn.Core` | **1.14.1** | Last official engine that still targets **net8.0** (+ net10.0). Published 2026-06-05. |
| `QuickFIXn.FIX44` | **1.14.1** | Official FIX 4.4 types + stock `DataDictionary/FIX44.xml`. Same version as Core. |
| `QuickFix.Net` | **do not pin; remove** | Deprecated third-party id, last **1.8.0** (2018-02-23), **.NET Framework only**, NuGet text: *DO NOT USE THIS ANYMORE*. Suggested alternative: `QuickFIXn.Core`. |

Current tree (this is a defect, not a pin):

```xml
<!-- src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj -->
<PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

A05 (earlier the same day) recorded **zero** FIX packages. A later edit added the **wrong** id. A35 is binding: only `QuickFIXn.*` from QuickFIXEngine.org. 1.15 **drops net8**. Keep Core and FIX44 versions **identical**.

Transitive (pin because CPM transitive pinning is on):

```text
Microsoft.Extensions.Logging.Abstractions >= 8.0.3
```

That is why §5 pins `Microsoft.Extensions.Logging.Abstractions` **8.0.3** — QF/n 1.14.1 needs it; do not let it float to 10.0.x.

Do **not** add: `QuickFIXn.FIX4.4` (deprecated name), `QuickFIXn.FIXT11`, `QuickFIXn.FIX50*`, `QuickFix.Net.NetCore`, `QuickFIXn` (no suffix).

Dictionary / SSL / session settings are A35 + A36, not this file. CPM only owns the two package versions.

---

## 7. Where each pin is consumed (when applying)

| Package | Project that should `PackageReference` it (no Version=) |
|---|---|
| `FluentValidation` | `src/Application` (already) |
| `FluentValidation.DependencyInjectionExtensions` | `apps/api` (and workers if they validate) — **when** validators exist |
| EF Design + Npgsql | `src/Infrastructure` (already) |
| EF InMemory | `tests/Integration` (already) |
| `QuickFIXn.Core` + `QuickFIXn.FIX44` | `src/Fix.CTrader` **instead of** `QuickFix.Net` |
| `Serilog.AspNetCore` | `apps/api` (already; bump 8.0.2 → 8.0.3 via CPM) |
| `Serilog.Extensions.Hosting` | `apps/mt5-worker`, `apps/fix-worker` |
| Other Serilog 8.x / sinks | hosts that need a **direct** reference; otherwise leave transitive |
| `Microsoft.Extensions.Hosting` | both workers (already 8.0.1) |
| `Microsoft.AspNetCore.SignalR.Common` | `apps/api` (already 8.0.4) |
| `Swashbuckle.AspNetCore` | `apps/api` (already 6.6.2) |
| `StackExchange.Redis` | `src/Infrastructure` (already 2.8.0) |
| test packages | the two test csprojs (already) |

`src/Domain` and `src/Mt5`: **no** PackageReference after this change.

---

## 8. Existing csproj versions vs recommended CPM (drift table)

| Package | Today | CPM pin | Action |
|---|---|---|---|
| `FluentValidation` | 11.9.2 | **11.9.2** | keep |
| `Microsoft.EntityFrameworkCore.Design` | 8.0.4 | **8.0.4** | keep |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 8.0.4 | **8.0.4** | keep |
| `Microsoft.EntityFrameworkCore.InMemory` | 8.0.4 | **8.0.4** | keep |
| `Serilog.AspNetCore` | 8.0.2 | **8.0.3** | patch bump at CPM time |
| `QuickFix.Net` | **1.8.0** | **remove** | replace with official pair |
| `QuickFIXn.Core` / `QuickFIXn.FIX44` | missing | **1.14.1 / 1.14.1** | add |
| `Microsoft.Extensions.Hosting` | 8.0.1 | **8.0.1** | keep |
| `Microsoft.AspNetCore.SignalR.Common` | 8.0.4 | **8.0.4** | keep |
| `Swashbuckle.AspNetCore` | 6.6.2 | **6.6.2** | keep |
| `StackExchange.Redis` | 2.8.0 | **2.8.0** | keep |
| `coverlet.collector` | 6.0.0 | **6.0.0** | keep |
| `FluentAssertions` | 6.12.0 | **6.12.0** | keep |
| `Microsoft.NET.Test.Sdk` | 17.8.0 | **17.8.0** | keep |
| `Moq` | 4.20.70 | **4.20.70** | keep |
| `xunit` / `xunit.runner.visualstudio` | 2.5.3 | **2.5.3** | keep |

No other product `PackageReference` exists in this solution.

---

## 9. Apply order (later agent — not this report)

```text
1. Write Directory.Packages.props (versions only).
2. Replace Directory.Build.props with §2.
3. Add Directory.Build.targets (§3) and global.json (§4).
4. Strip Version= from every PackageReference in the ten csprojs.
5. Strip duplicated TargetFramework / Nullable / ImplicitUsings from those csprojs
   (optional in the same PR; safe because they now inherit).
6. In Fix.CTrader: delete QuickFix.Net 1.8.0; add QuickFIXn.Core + QuickFIXn.FIX44.
7. dotnet restore Mt5TraderIntelligence.sln
8. dotnet build Mt5TraderIntelligence.sln -c Release
9. Update A65 Docker COPY lines to include Directory.Packages.props
   (and Directory.Build.targets / global.json).
```

Exit (A30 I0): `dotnet build Mt5TraderIntelligence.sln` still green. No behavior change except the illegal FIX package id going away — that is a restore-graph change, not a trading-behavior change (the adapter still has no live send path).

Do **not** enable `TreatWarningsAsErrors` in the same PR as a large feature. Do it in the hygiene increment. If the tree currently has nullable warnings, fix those first or the I0 build goes red — that is the point.

---

## 10. Explicit non-goals

- Do not hand-write MQ5, mutate EX5, or touch `mt5-sdk`.
- Do not add Kafka, MassTransit, OpenTelemetry, Seq, EF 10, Serilog 10, FluentValidation 12.
- Do not put package versions in `Directory.Build.props`.
- Do not create `Directory.Build.props` under `src/` or `apps/` (would hide the root file).
- Do not vendor QuickFIX dictionaries in this increment (A35 / A36).
- Do not implement `UseSerilog` here (A50). The pin only names the packages.

---

## 11. Sources

| Source | What it established |
|---|---|
| `D:\Prop\Directory.Build.props` | Current incomplete props (`LangVersion=latest`, no TFM, warnings not errors) |
| Ten `TraderIntelligence*.csproj` | Existing Version= values; all already net8 + nullable + implicit usings |
| A30 §4 I0 | Create `Directory.Build.props`: net8, nullable, treat warnings as errors on product |
| A11 §3 | Earlier “no props file” note — now outdated |
| A35 | Official QF/n 1.14.1 pair; reject unofficial ids; 1.15 drops net8 |
| A50 §3.1 | Serilog 8.x host split (AspNetCore on API, Extensions.Hosting on workers) |
| A02 / Application csproj | FluentValidation **11.9.2** already |
| A03 / Infrastructure csproj | EF Design + Npgsql **8.0.4** already |
| https://www.nuget.org/packages/Serilog.AspNetCore/ | 8.0.3 last 8.x; 10.0.0 is current latest |
| https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/8.0.4 | 8.0.4 exists; family must share one version |
| https://www.nuget.org/packages/FluentValidation/ | 11.9.2 frozen here; 12.1.1 is latest major |
| https://www.nuget.org/packages/QuickFix.Net/ | Deprecated; “DO NOT USE”; suggests QuickFIXn.Core |
| https://www.nuget.org/packages/QuickFIXn.Core/1.14.1 | Official engine, net8.0 + net10.0 |
| https://learn.microsoft.com/nuget/consume-packages/central-package-management | CPM = `Directory.Packages.props`, not Build.props |
| Serilog.AspNetCore versioning note | Choose 8.x when targeting net8.x |

---

## 12. One-line summary

**Strengthen `D:\Prop\Directory.Build.props` to `net8.0` + C# 12 + nullable + implicit usings + product warnings-as-errors; put versions in `Directory.Packages.props`; keep FluentValidation 11.9.2 and EF 8.0.4; pin Serilog to the 8.x line (`Serilog.AspNetCore` 8.0.3); replace `QuickFix.Net` 1.8.0 with `QuickFIXn.Core` + `QuickFIXn.FIX44` 1.14.1.**
