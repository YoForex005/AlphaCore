# C56 — `Directory.Build.props` (measured; A30 I0 / A102 not applied)

| Field | Value |
|---|---|
| Agent | C56 (senior engineer, MSBuild defaults only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T07:59:08.5950668Z |
| Artifact | `D:\Prop\reports\swarm\20260818\C56_directory_build.md` |
| Workspace assigned | `D:\Prop\src` (file is **not** under `src/`) |
| Product tree | Repo-root `D:\Prop\Directory.Build.props` |
| Assigned | Read `Directory.Build.props`. Write this report. **Do not modify product source.** |
| Product source modified | **No.** This report (plus index / swarm-log) is the only write. |
| Method | Full read + SHA-256 + hex dump of the root props; `Test-Path` of sibling MSBuild files; recursive search for nested `Directory.Build.*` / `Directory.Packages.props` / `global.json` / `nuget.config` / `.editorconfig` (exclude `node_modules` / `bin` / `obj` / `vendor`); full read of all ten product `*.csproj`; `dotnet msbuild -getProperty` on each; Domain preprocess import graph; `dotnet --info`; `git show HEAD:Directory.Build.props` vs worktree; sln / compose / Dockerfile census. **Did not** edit `.csproj`, props, targets, or packages. **Did not** `dotnet build` the solution this pass. |
| Law | A30 §4 I0 (create props: net8, nullable, treat warnings as errors on product). A102 (binding strengthen + CPM plan; **does not** edit the file). Architecture §73 classification. |
| Relates | A11 (stale “no props”), A30 I0, A65 §5.1 (proposed `COPY Directory.Build.props`), A88 / B09 (optional SolutionItems), A102, A105 (no DLL copy step), B01, B41 / C24 (no `applicationUrl`), C19 / C25 / C27 / C28 (no package pins). |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |
| Props blob | git `c495f5457edb86fcc41268ed64ec5faefce9916f` — **identical** to `HEAD:Directory.Build.props` |
| First commit of file | `6c414477f632416031b851171d3354fe2a232594` (2026-08-18 13:12:17 +0530, Initial commit) |
| Worktree vs HEAD | **Clean** (`git diff --stat HEAD -- Directory.Build.props` empty) |
| SDK on this box | **8.0.424** only. `dotnet --info`: **global.json file: Not found.** |

Classification vocabulary is architecture §73: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE` / `GONE`.

---

## 0. Verdict

**The file exists at the repo root. It is incomplete. A30 I0 and A102 are not applied.**

Assigned path `D:\Prop\src\Directory.Build.props` does **not** exist (`Test-Path` = `False`). The single product file is:

`D:\Prop\Directory.Build.props`

269 bytes, 9 physical lines, LF, no BOM. SHA-256 **`5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0`**. Same hash as C19 / C28. LastWriteUtc `2026-08-18T07:35:12.1515604Z`.

It sets five properties only: `LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=false`, `Deterministic=true`. MSBuild **does** import it: Domain preprocess walks `GetDirectoryNameOfFileAbove` from `D:\Prop\src\Domain` to `D:\Prop\Directory.Build.props` and inlines those five lines before `Microsoft.Common.props`. Evaluated values on all ten product projects match.

It is **not** a finished I0 hygiene file:

| A30 I0 / A102 requirement | On disk | Class |
|---|---|---|
| Root `Directory.Build.props` exists | **Yes** | `EXISTS_NEEDS_REFACTOR` |
| `TargetFramework=net8.0` in the props | **Absent** (duplicated in every `.csproj`) | incomplete |
| `LangVersion=12` | **`latest`** | incomplete (follows the installed SDK, not the TFM) |
| `Nullable=enable` | **Yes** (props + every csproj) | `EXISTS_AND_GOOD` |
| `ImplicitUsings=enable` | **Yes** (props + every csproj) | `EXISTS_AND_GOOD` |
| `TreatWarningsAsErrors=true` on product | **`false`**, no product override | I0 **not met** |
| `Directory.Build.targets` test exception | **MISSING** | `MISSING` |
| `Directory.Packages.props` / CPM | **MISSING** | `MISSING` |
| `global.json` pin SDK 8 | **MISSING** | `MISSING` |
| Nested `src/` or `apps/` props (would hide the root) | **None** | `EXISTS_AND_GOOD` (absence) |

Honest one-liner: **`Directory.Build.props` is a five-line language/nullable/deterministic default. It does not pin net8, C# 12, warnings-as-errors, or package versions. A11/A30 “MISSING” is stale; A102 is still a plan.**

Do **not** treat this file as “repo hygiene done.” Do **not** create a second copy under `src/` or `apps/`. Do **not** put `PackageReference` / `PackageVersion` here.

---

## 1. Method (what this pass did and did not do)

| Source | Action |
|---|---|
| Root props | Full read. `Get-FileHash SHA256`. `Get-Content.Count`. `Format-Hex` (BOM / newline). |
| Siblings | `Test-Path` of `Directory.Build.targets`, `Directory.Packages.props`, `Directory.Build.rsp`, `global.json`, `nuget.config` / `NuGet.Config`, `.editorconfig`, `stylecop.json`, plus `src/` / `apps/` / `tests/` child props. |
| Nested search | Recursive `Directory.Build.props` / `.targets` / `Directory.Packages.props` / `global.json` / `nuget.config` / `.editorconfig` under `D:\Prop`, excluding `node_modules`, `bin`, `obj`, `vendor`. Result: **only** `D:\Prop\Directory.Build.props`. |
| Ten product csproj | Full read + SHA-256. Grep `TargetFramework` / `Nullable` / `ImplicitUsings` / `LangVersion` / `TreatWarningsAsErrors` / `IsTestProject` / `PackageReference`. |
| Evaluated properties | `dotnet msbuild <csproj> -getProperty:…` on all ten. Extra Domain query for `MaxSupportedLangVersion`, `WarningsAsErrors`, `NoWarn`. |
| Import proof | `dotnet msbuild Domain.csproj -pp` — import block names `D:\Prop\Directory.Build.props` and inlines the five properties. Scratch preprocess deleted after extract (not a report). |
| Scratch inherit | `_tmp_c23_empty\C23EmptyEval.csproj` (under `reports/`) evaluates the same `LangVersion` / `Nullable` / `TreatWarningsAsErrors` / `Deterministic` — walk-up reaches the root. |
| Git | `rev-parse HEAD`; `hash-object` vs `HEAD:`; `log --follow`; porcelain / `diff --stat` for this file. |
| Hosts | `dotnet --info` / `--list-sdks` / `--list-runtimes`. |
| sln / Docker | `Mt5TraderIntelligence.sln` has **no** `Directory.Build` / `SolutionItems` text. **Zero** `Dockerfile*` under the product tree. `docker-compose.yml` exists (687 B) and does not `COPY` the props. |
| Generated usings | `src/Domain/obj/Release/net8.0/TraderIntelligence.Domain.GlobalUsings.g.cs` is the SDK class-library set (System / Linq / Tasks…). Confirms `ImplicitUsings` is live. |

No `dotnet add`, no props edit, no CPM file, no `global.json`, no csproj strip of duplicated TFM lines.

---

## 2. Measured file

### 2.1 Identity

| Field | Value |
|---|---|
| Path | `D:\Prop\Directory.Build.props` |
| `D:\Prop\src\Directory.Build.props` | **Does not exist** |
| Bytes | **269** |
| Physical lines | **9** |
| SHA-256 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` |
| LastWriteUtc | `2026-08-18T07:35:12.1515604Z` |
| Newline | **LF** (`0A` only; no `0D`) |
| BOM | **None** (first bytes `3C 50 72` = `<Pr`) |
| Git index | tracked; blob `c495f5457edb86fcc41268ed64ec5faefce9916f` |
| vs HEAD | **byte-identical** |

### 2.2 Verbatim (entire file)

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

No `<ItemGroup>`. No `PackageReference`. No `PackageVersion`. No `TargetFramework`. No `ManagePackageVersionsCentrally`. No `RuntimeIdentifier`. No `LaunchProfile` / `applicationUrl`. No native `Content` / copy-dlls. No `UserSecretsId`.

### 2.3 Import proof (Domain preprocess)

SDK `Microsoft.Common.props` (from `C:\Program Files\dotnet\sdk\8.0.424`) resolves:

```text
GetDirectoryNameOfFileAbove($(MSBuildProjectDirectory), 'Directory.Build.props')
→ D:\Prop
→ Import D:\Prop\Directory.Build.props
```

The preprocess dump inlines the same five properties, then continues into `Microsoft.Common.props`. `ImportDirectoryBuildProps` is the SDK default (`true`). There is no `Directory.Build.targets` for the matching after-project import (`_DirectoryBuildTargetsFile` would be `Directory.Build.targets`; `Test-Path` = `False`).

---

## 3. Property-by-property (written vs evaluated)

SDK on this machine: **8.0.424** / MSBuild **17.11.48**. Only one SDK is installed. `MaxSupportedLangVersion` on Domain = **`12.0`**. So *today* `LangVersion=latest` compiles **C# 12**. That is an accident of the box, not a pin.

| Property | In props | Evaluated (all 10 product projects) | A102 / A30 want | Note |
|---|---|---|---|---|
| `LangVersion` | `latest` | `latest` | **`12`** | Follows the **installed SDK**. A second SDK 9/10 on PATH with no `global.json` would raise the language into a `net8.0` binary. |
| `Nullable` | `enable` | `enable` | `enable` | Also restated in every csproj. |
| `ImplicitUsings` | `enable` | `enable` | `enable` | Also restated in every csproj. Domain `GlobalUsings.g.cs` is the class-lib set. |
| `TreatWarningsAsErrors` | `false` | `false` | **`true` on product** | I0 fail. Tests would inherit `true` too unless `Directory.Build.targets` exists. |
| `Deterministic` | `true` | `true` | `true` | Only unique non-duplicated win in this file. |
| `TargetFramework` | **absent** | `net8.0` (from each csproj) | `net8.0` **here** | Ten copies. Props do not own the TFM. |
| `ManagePackageVersionsCentrally` | absent | **empty** | `true` | CPM off. |
| `CentralPackageTransitivePinningEnabled` | absent | **empty** | `true` | n/a until CPM. |
| `EnableNETAnalyzers` | absent | `true` | `true` | **SDK default** for net5+, not this file. |
| `AnalysisLevel` | absent | **`latest`** | **`8`** | SDK default tracks the SDK, same class of risk as `LangVersion=latest`. |
| `NuGetAudit` | absent | `true` | `true` | SDK default. |
| `NuGetAuditMode` | absent | **`direct`** | A102: **`all`** | Transitives not in the audit surface. |
| `NuGetAuditLevel` | absent | **`low`** | A102: **`moderate`** | |
| `ContinuousIntegrationBuild` | absent | empty | `true` when `CI=true` | Not set. |
| `EnforceCodeStyleInBuild` | absent | `false` | `false` | SDK default. |
| `WarningLevel` | absent | `8` | (net8 default) | |
| `WarningsAsErrors` | absent | `;NU1605;SYSLIB0011` | — | SDK still promotes those two even with `TreatWarningsAsErrors=false`. |
| `NoWarn` | absent | `1701;1702` | — | SDK default (unused assembly refs). |
| `IsTestProject` | absent | `true` on Unit + Integration; empty elsewhere | per-project | Tests already set it; nothing consumes it (no `.targets`). |
| `IsPackable` | absent | `true` on 5 class libs; `false` on API, both workers, both tests | per-project | Class-lib default. Hosts/tests already `false`. |

`LaunchProfile` / `applicationUrl` remain unset (B41 / C24 still hold). No MT5 `Content` copy (A105 still holds).

---

## 4. Sibling files (census)

Recursive product-tree search (exclude `bin` / `obj` / `node_modules` / `vendor`) found **exactly one** of these names: the root props.

| Path | State |
|---|---|
| `D:\Prop\Directory.Build.props` | **EXISTS** (this file) |
| `D:\Prop\Directory.Build.targets` | **MISSING** |
| `D:\Prop\Directory.Packages.props` | **MISSING** |
| `D:\Prop\Directory.Build.rsp` | **MISSING** |
| `D:\Prop\global.json` | **MISSING** (`dotnet --info` agrees) |
| `D:\Prop\nuget.config` / `NuGet.Config` | **MISSING** |
| `D:\Prop\.editorconfig` | **MISSING** |
| `D:\Prop\src\Directory.Build.props` | **MISSING** (correct — a child file would **replace** the parent unless it `<Import>`s it) |
| `D:\Prop\apps\Directory.Build.props` | **MISSING** (correct) |
| `D:\Prop\tests\Directory.Build.props` | **MISSING** (correct; A102 forbids a second copy) |

C++ `mt5-sdk/` and Vite `apps/web/` ignore MSBuild props. No change required there.

Scratch `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\C23EmptyEval.csproj` is **not** a product project. It still inherits the root props by walk-up (`LangVersion=latest`, `Nullable=enable`, `TreatWarningsAsErrors=false`, `Deterministic=true`). Do not put future scratch trees under `D:\Prop` if a different language/warning policy is needed.

---

## 5. Ten product `.csproj` vs inheritance

Every product project restates `TargetFramework=net8.0`, `Nullable=enable`, `ImplicitUsings=enable`. **None** set `LangVersion`, `TreatWarningsAsErrors`, or `Deterministic` — those three come only from the root props.

| Project | SHA-256 | Sdk | TFM / NRT / IU in csproj | Packages (inline `Version=`) | `IsPackable` eval | Notes |
|---|---|---|---|---|---|---|
| `src/Domain/TraderIntelligence.Domain.csproj` | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` | `Microsoft.NET.Sdk` | yes | **none** | `true` | package-free (A01 / A50). 218 B / 9 lines. |
| `src/Application/TraderIntelligence.Application.csproj` | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` | `Microsoft.NET.Sdk` | yes | `FluentValidation` **11.9.2** | `true` | 433 B / 17 lines. |
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | `Microsoft.NET.Sdk` | yes | EF Design **8.0.4**, EF InMemory **8.0.4**, Npgsql EF **8.0.4**, Redis **2.8.0** | `true` | 1035 B / 25 lines. |
| `src/Mt5/TraderIntelligence.Mt5.csproj` | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `Microsoft.NET.Sdk` | yes | **none** | `true` | 419 B / 14 lines. |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `Microsoft.NET.Sdk` | yes | **none** (worktree) | `true` | **Same SHA as Mt5** (refs + TFM block only). Official QuickFIX/n still **MISSING** (C19). |
| `apps/api/TraderIntelligence.Api.csproj` | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | `Microsoft.NET.Sdk.Web` | yes | SignalR.Common **8.0.4**, Serilog.AspNetCore **8.0.2**, Swashbuckle **6.6.2** | `false` | 803 B / 21 lines. |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | `Microsoft.NET.Sdk.Worker` | yes | Hosting **8.0.1** | `false` | `UserSecretsId` present. 840 B / 20 lines. |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | `Microsoft.NET.Sdk.Worker` | yes | Hosting **8.0.1** | `false` | `UserSecretsId` present. 856 B / 20 lines. |
| `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | `Microsoft.NET.Sdk` | yes | coverlet 6.0.0, FA 6.12.0, Test.Sdk 17.8.0, Moq 4.20.70, xunit 2.5.3 ×2 | `false` | `IsTestProject=true`. 1113 B / 31 lines. |
| `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | `Microsoft.NET.Sdk` | yes | coverlet 6.0.0, FA 6.12.0, EF InMemory 8.0.4, Test.Sdk 17.8.0, xunit 2.5.3 ×2 | `false` | `IsTestProject=true`. 1328 B / 33 lines. |

Evaluated `LangVersion` / `Nullable` / `ImplicitUsings` / `TreatWarningsAsErrors` / `Deterministic` / `TargetFramework` are **identical** across all ten: `latest` / `enable` / `enable` / `false` / `true` / `net8.0`.

Duplicated TFM / NRT / IU lines are redundant once the props exist. They do not hurt. A102 §9 may strip them in a later hygiene PR; this report does not.

---

## 6. Packages are **not** in this file

`Directory.Build.props` contains **zero** package versions. `Directory.Packages.props` does **not** exist. Every `PackageReference` still has inline `Version=`.

That matches C19 / C25 / C27 / C28: no central pin for QuickFIX/n, Serilog, SignalR, or Redis.

Inline inventory (worktree, this pass) — **not** a pin table, just what restore sees:

| Package | Where | Version |
|---|---|---|
| `FluentValidation` | Application | 11.9.2 |
| `Microsoft.EntityFrameworkCore.Design` | Infrastructure | 8.0.4 |
| `Microsoft.EntityFrameworkCore.InMemory` | Infrastructure + Integration | 8.0.4 |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | Infrastructure | 8.0.4 |
| `StackExchange.Redis` | Infrastructure | 2.8.0 |
| `Microsoft.AspNetCore.SignalR.Common` | API | 8.0.4 |
| `Serilog.AspNetCore` | API | 8.0.2 |
| `Swashbuckle.AspNetCore` | API | 6.6.2 |
| `Microsoft.Extensions.Hosting` | both workers | 8.0.1 |
| test stack | Unit / Integration | coverlet 6.0.0, FA 6.12.0, Test.Sdk 17.8.0, Moq 4.20.70 (Unit only), xunit 2.5.3 |

A102’s recommended CPM (`Serilog.AspNetCore` 8.0.3, official `QuickFIXn.*` 1.14.1, `ManagePackageVersionsCentrally=true`) is **not on disk**. Apply that in a later increment from A102, not from this file.

Vendor `mt5-sdk\…\*.csproj` are .NET Framework 4.5 / 4.7.2 examples under `vendor/`. They do **not** sit under the walk-up of a product project in a way that this props should target them; they are ignored here.

---

## 7. Docker / solution / A65

| Check | Result |
|---|---|
| `Mt5TraderIntelligence.sln` mentions `Directory.Build.props` | **No.** SHA-256 `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` (7019 B). Folders `src` / `apps` / `tests` only. Optional SolutionItems (A88 / B09) **not** added. Auto-import does not need the sln row. |
| Product `Dockerfile*` | **None** on disk. A65 §5.1 `COPY Directory.Build.props ./` is a **proposed** Dockerfile, not a measured file. |
| `docker-compose.yml` | **EXISTS** (687 B, B37). No `COPY`, no app image. I0 compose is Postgres + Redis only. |
| `apps/web` | Vite. Ignores this props file. |

When someone later adds the A65 Dockerfiles, they must `COPY Directory.Build.props ./` (and, if CPM lands, `Directory.Packages.props`, `Directory.Build.targets`, `global.json`). That is A65 / A102 work, not this pass.

---

## 8. Stale claims (keep the old files; use this one for “does the props file exist?”)

| Report | Claim | Now |
|---|---|---|
| A11 §3 | No `Directory.Build.props` / `Directory.Packages.props` / `global.json` | Props **exist**. The other two are still missing. |
| A30 §1 baseline | `Directory.Build.props` **MISSING** | **Stale.** File is in the initial commit. |
| A30 §4 I0 | Create props with net8 + nullable + **warnings as errors on product** | File exists; **warnings-as-errors is still false**; TFM not centralized. I0 **incomplete**. |
| A102 | Strengthen to C# 12 + CPM + `global.json` | **Plan only.** Bytes / hash unchanged since C19 / C28. |
| A88 / B09 | Optional sln SolutionItems | Still not listed. Not a functional gap. |

A102 remains the apply spec. This report does **not** re-author the recommended XML.

---

## 9. What a later agent may change (not this report)

Binding order is A102 §9. Restated as measured gaps only:

1. Add `Directory.Packages.props` (versions only). Strip `Version=` from the ten csproj `PackageReference` items.
2. Replace this props file with A102 §2 (`net8.0`, `LangVersion=12`, product `TreatWarningsAsErrors=true`, CPM flags, `AnalysisLevel=8`, NuGetAudit `all`/`moderate`).
3. Add `Directory.Build.targets` so `IsTestProject=true` keeps `TreatWarningsAsErrors=false`.
4. Add `global.json` SDK `8.0.100` + `rollForward: latestFeature` + `allowPrerelease: false`.
5. Do **not** create `src/Directory.Build.props` or `apps/Directory.Build.props`.
6. Do **not** put package versions in `Directory.Build.props`.
7. Do **not** flip `TreatWarningsAsErrors` in the same PR as a large feature if the tree currently has nullable warnings — fix those first or I0 goes red. This pass did not re-run `dotnet build`, so the current warning inventory is **not** re-measured here (B01 Domain was 0/0).

Exit remains A30 I0: `dotnet build Mt5TraderIntelligence.sln` still green. No trading-behavior change.

---

## 10. Explicit non-goals / non-claims

- Did not edit `Directory.Build.props`, any `.csproj`, the `.sln`, or introduce CPM / `global.json`.
- Did not enable live FIX, copy Manager DLLs, or add Serilog / SignalR / QuickFIX/n wiring.
- Did not claim “EX5 decompiled” or any trading-platform completeness. This file is MSBuild hygiene only.
- Did not claim `LangVersion=latest` is C# 12 on every machine — only on **this** SDK 8.0.424 box (`MaxSupportedLangVersion=12.0`).
- Did not treat `Deterministic=true` as SourceLink / CI path-mapping (`ContinuousIntegrationBuild` is empty).
- C++ `mt5-sdk` and `apps/web` are out of scope for this import.

---

## 11. Sources

| Source | What it established |
|---|---|
| `D:\Prop\Directory.Build.props` | Five properties; 269 B; SHA-256 `5ACD33B0…`; LF; no BOM |
| Domain `-pp` import | Walk-up resolves this exact path; properties inline before `Microsoft.Common.props` |
| `dotnet msbuild -getProperty` × 10 | Evaluated table in §3 / §5 |
| `dotnet --info` | SDK 8.0.424 only; no `global.json` |
| Ten `TraderIntelligence*.csproj` | Duplicated TFM / NRT / IU; inline package versions |
| `git show HEAD:Directory.Build.props` | Worktree == HEAD == initial-commit content |
| A30 §4 I0 | net8 + nullable + treat warnings as errors on product |
| A102 | Recommended strengthen + CPM; not applied |
| A11 / A30 §1 | “MISSING” — stale for existence |
| C19 / C28 | Same SHA-256; no package pins in this file |
| A65 §5.1 | Proposed Docker `COPY`; no Dockerfile on disk |
| A105 §3.5 | No native copy step in this file |
| B41 / C24 | No `applicationUrl` here |

---

## 12. One-line summary

**`D:\Prop\Directory.Build.props` exists (269 B, SHA-256 `5ACD33B0…`, imported by all ten projects) and only sets `LangVersion=latest`, nullable, implicit usings, `TreatWarningsAsErrors=false`, and `Deterministic=true`. A30 I0 / A102 (C# 12, product warnings-as-errors, centralized `net8.0`, CPM, `global.json`) are not on disk. Do not put a second copy under `src/`.**
