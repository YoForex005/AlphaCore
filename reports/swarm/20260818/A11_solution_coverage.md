# A11 — `Mt5TraderIntelligence.sln` coverage vs on-disk projects

**Agent:** A11 (senior engineer audit)  
**Date:** 2026-08-18  
**Workspace:** `D:\Prop`  
**Product source modified:** no  
**Scope:** solution membership only. Does not assess implementation completeness except as a coverage caveat.

## Verdict

**None of the named existing product projects are missing from the solution.**

The parenthetical checklist — Domain, Application, Infrastructure, Fix.CTrader, Api, FixWorker, tests — **all exist on disk and all are already listed in** `D:\Prop\Mt5TraderIntelligence.sln`.

- Product `.csproj` on disk: **10**
- Product `.csproj` in solution: **10**
- Named-checklist gaps: **0**
- Architecture §66 *extra* projects that exist as `.csproj` but are omitted from the `.sln`: **0** (those extras do not exist on disk)

Honest restatement: this is a **solution-membership PASS**. It is **not** an implementation PASS. The ten projects are scaffold (template `Class1` / weather-forecast API / empty `Test1` / stock `BackgroundService` loops).

---

## Method

| Source | Path |
|---|---|
| Solution | `D:\Prop\Mt5TraderIntelligence.sln` (95 lines, VS 17 / format 12.00) |
| Architecture §66 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` heading `# 66. Suggested Repository Structure` (line 2423) |
| On-disk product trees | `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` |
| Adjacent trees | `D:\Prop\services` (empty), `D:\Prop\docs` (empty), `D:\Prop\mt5-sdk` (CMake C++) |

Every `Project(...)` entry in the `.sln` was matched to a file that exists at the relative path. Every product `TraderIntelligence*.csproj` under `src\`, `apps\`, and `tests\` was matched back to a `Project` line. Vendor SDK examples under `mt5-sdk\vendor\` were inventoried only to exclude them from the product gap list.

---

## 1. What the solution actually contains

Three **solution folders** (type `{2150E333-8FDC-42A3-9474-1A3956D46DE8}`) and **ten** C# projects (type `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`).

### Solution folders

| Folder | GUID | Nested children |
|---|---|---|
| `src` | `{284C6660-B861-4055-A709-12AF82034A9B}` | Domain, Application, Infrastructure, Mt5, Fix.CTrader |
| `apps` | `{91FA4D5C-2CC5-44A5-9805-42A3EEA00429}` | Api, Mt5Worker, FixWorker |
| `tests` | `{355D29D7-21E1-4D1D-A68D-93FFB512E960}` | Tests.Unit, Tests.Integration |

`NestedProjects` maps all ten C# GUIDs. No orphan GUID. No C# project left at the solution root.

### C# projects in the `.sln`

| # | Solution name | Relative path (from `D:\Prop`) | GUID | Nested under | Disk exists | Debug+Release Build.0 |
|---|---|---|---|---|---|---|
| 1 | `TraderIntelligence.Mt5` | `src\Mt5\TraderIntelligence.Mt5.csproj` | `{CCD4D49A-9F3E-4795-AA56-CFBF87526E94}` | `src` | yes | yes |
| 2 | `TraderIntelligence.Mt5Worker` | `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | `{31DFD31A-7E82-4968-912F-397C3E7DEE61}` | `apps` | yes | yes |
| 3 | `TraderIntelligence.Domain` | `src\Domain\TraderIntelligence.Domain.csproj` | `{A70A2194-62B9-4B2E-96DB-9725BEA5D9D7}` | `src` | yes | yes |
| 4 | `TraderIntelligence.Application` | `src\Application\TraderIntelligence.Application.csproj` | `{8A0BB7FD-D1CC-46B3-9C0C-6A2408866F36}` | `src` | yes | yes |
| 5 | `TraderIntelligence.Infrastructure` | `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `{14EDD461-7C2D-43AC-BC2B-F2DCAC644491}` | `src` | yes | yes |
| 6 | `TraderIntelligence.Fix.CTrader` | `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | `{76085664-A639-4C0D-8F92-264963416855}` | `src` | yes | yes |
| 7 | `TraderIntelligence.Api` | `apps\api\TraderIntelligence.Api.csproj` | `{D17266FA-2F65-4F00-9701-BC5DD52B8439}` | `apps` | yes | yes |
| 8 | `TraderIntelligence.FixWorker` | `apps\fix-worker\TraderIntelligence.FixWorker.csproj` | `{63112B54-6D05-481D-B2F6-99AF3A795192}` | `apps` | yes | yes |
| 9 | `TraderIntelligence.Tests.Unit` | `tests\Unit\TraderIntelligence.Tests.Unit.csproj` | `{AA462E32-BFEF-4CB8-BDC6-B47F3609DFF9}` | `tests` | yes | yes |
| 10 | `TraderIntelligence.Tests.Integration` | `tests\Integration\TraderIntelligence.Tests.Integration.csproj` | `{40962285-3955-4D71-8C25-4596EDA39D98}` | `tests` | yes | yes |

All ten GUIDs are unique. All ten have `Debug|Any CPU` and `Release|Any CPU` with `ActiveCfg` + `Build.0`. Solution folders correctly have **no** `ProjectConfigurationPlatforms` rows.

There is no `SolutionItems` section. Architecture docs are not attached to the solution.

---

## 2. Named checklist (the question this audit was asked)

> List which existing projects are NOT in the solution (Domain, Application, Infrastructure, Fix.CTrader, Api, FixWorker, tests).

| Named item | On-disk project | In `.sln`? | Evidence |
|---|---|---|---|
| Domain | `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | **IN** | sln line 14 |
| Application | `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | **IN** | sln line 16 |
| Infrastructure | `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | **IN** | sln line 18 |
| Fix.CTrader | `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | **IN** | sln line 20 |
| Api | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | **IN** | sln line 22 |
| FixWorker | `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | **IN** | sln line 24 |
| tests | `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | **IN** | sln line 28 |
| tests | `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | **IN** | sln line 30 |

**Existing named projects NOT in the solution: none.**

Also present on disk (not in the parenthetical, but part of product coverage):

| Extra existing product project | In `.sln`? |
|---|---|
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | **IN** (sln line 8) |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | **IN** (sln line 12) |

Those two are the original pair the solution was built around. They are **not** gaps.

---

## 3. Full product `.csproj` census (disk → sln)

Product tree only. Vendor SDK excluded here (see §6).

| Disk path | In sln | Sdk |
|---|---|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | yes | `Microsoft.NET.Sdk` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | yes | `Microsoft.NET.Sdk` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | yes | `Microsoft.NET.Sdk` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | yes | `Microsoft.NET.Sdk` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | yes | `Microsoft.NET.Sdk` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | yes | `Microsoft.NET.Sdk.Web` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | yes | `Microsoft.NET.Sdk.Worker` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | yes | `Microsoft.NET.Sdk.Worker` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) |

**Product `.csproj` files on disk that are NOT in `Mt5TraderIntelligence.sln`: none.**

No `Directory.Build.props`, `Directory.Packages.props`, or `global.json` at `D:\Prop`. That is a repo-hygiene note, not a missing project.

---

## 4. Architecture §66 suggested extras

Source: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §66 (lines 2423–2470). The section says *“Adapt to the existing repo; do not create duplicates unnecessarily.”* and then lists a *possible* tree.

### §66 tree vs reality

| §66 path | Exists on disk? | Has a product `.csproj`? | In `.sln`? | Classification |
|---|---|---|---|---|
| `/apps/web` | **no** | no | no | suggested extra, not created |
| `/apps/api` | yes | `TraderIntelligence.Api` | yes | covered |
| `/apps/mt5-worker` | yes | `TraderIntelligence.Mt5Worker` | yes | covered |
| `/apps/fix-worker` | yes | `TraderIntelligence.FixWorker` | yes | covered |
| `/services/ml-service` | **no** (`D:\Prop\services` is an empty directory) | no | no | suggested extra, not created |
| `/src/Domain` | yes | `TraderIntelligence.Domain` | yes | covered |
| `/src/Application` | yes | `TraderIntelligence.Application` | yes | covered |
| `/src/Infrastructure` | yes | `TraderIntelligence.Infrastructure` | yes | covered |
| `/src/Mt5` | yes | `TraderIntelligence.Mt5` | yes | covered |
| `/src/TradeReconstruction` | **no** | no | no | suggested extra, not created |
| `/src/Scoring` | **no** | no | no | suggested extra, not created |
| `/src/Shadow` | **no** | no | no | suggested extra, not created |
| `/src/Risk` | **no** | no | no | suggested extra, not created |
| `/src/Execution` | **no** | no | no | suggested extra, not created |
| `/src/Fix.CTrader` | yes | `TraderIntelligence.Fix.CTrader` | yes | covered |
| `/tests/Unit` | yes | `TraderIntelligence.Tests.Unit` | yes | covered |
| `/tests/Integration` | yes | `TraderIntelligence.Tests.Integration` | yes | covered |
| `/tests/Replay` | **no** | no | no | suggested extra, not created |
| `/tests/Fix` | **no** | no | no | suggested extra, not created |
| `/tests/Risk` | **no** | no | no | suggested extra, not created |
| `/docs/*.md` (10 named docs) | **no** (`D:\Prop\docs` is empty) | n/a | no `SolutionItems` | docs gap, not a `.csproj` gap |

### §66 extras that are *not* existing projects (therefore cannot be “missing from the solution”)

These are **proposed**, not omitted:

1. `apps/web` — React operational dashboard (§66 + §1: “React is appropriate for the operational dashboard”). No `package.json`, no `.csproj`, no folder.
2. `services/ml-service` — Python/XGBoost scoring service. Empty parent `D:\Prop\services` only.
3. `src/TradeReconstruction`
4. `src/Scoring`
5. `src/Shadow`
6. `src/Risk`
7. `src/Execution`
8. `tests/Replay`
9. `tests/Fix`
10. `tests/Risk`

**Count:** 10 suggested extra *code* nodes. **Existing-but-unlisted:** 0. **Uncreated:** 10.

Do **not** treat this as a solution-file defect. §66 explicitly allows adapting to the existing repo and avoiding duplicate projects. The current `.sln` already includes every §66 node that has been materialized as a .NET project.

---

## 5. Existing *non-.NET* project not in the `.sln`

| Path | Kind | In `Mt5TraderIntelligence.sln`? | Note |
|---|---|---|---|
| `D:\Prop\mt5-sdk\CMakeLists.txt` (`project(mt5sdk LANGUAGES CXX)`) | CMake C++20 library + optional tests/probes | **no** | Different toolchain. No `.csproj`. Not expected in a C# solution unless someone adds a CMake/vcxproj wrapper. |

This is the only **first-party** buildable project on disk that is outside the `.sln`. It is a coverage fact, not a broken `.sln` path.

C++ unit/probe sources under `D:\Prop\mt5-sdk\tests\` (e.g. `mt5_ledger_store_test.cpp`, `mt5_time_window_test.cpp`) are likewise outside the .NET solution. They are gated by `MT5SDK_BUILD_TESTS` / `MT5SDK_BUILD_PROBES` in CMake.

---

## 6. Vendor `.csproj` files correctly excluded

Under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\` there are MetaQuotes sample `.csproj` / `.sln` files (TextFeeder.NET, UniFeeder.NET, BalanceExample.NET, DealerExample.NET, MetaQuotes.MT5WebAPI, WebTrader, plus `All-Examples.sln`).

These are **not** product projects. They must stay out of `Mt5TraderIntelligence.sln`. Not counted as coverage gaps.

---

## 7. Project-reference graph (membership is complete; edges are thin)

All `ProjectReference` targets resolve to projects that are **in** the solution. No dangling reference to a missing `.csproj`.

```
Domain
  ↑
Application
  ↑
  ├── Infrastructure  →  Api, Mt5Worker, FixWorker, Tests.Integration
  ├── Mt5             →  Mt5Worker
  └── Fix.CTrader     →  FixWorker, Tests.Unit, Tests.Integration
```

Notable **non-membership** issues (reference holes, not sln holes):

| Observer | Does not reference | Implication |
|---|---|---|
| `TraderIntelligence.Api` | `Mt5`, `Fix.CTrader` | API cannot compile against either adapter until a reference is added |
| `TraderIntelligence.Tests.Unit` | `Infrastructure`, `Mt5` | unit tests cannot see persistence or MT5 adapter |
| `TraderIntelligence.Tests.Integration` | `Mt5`, `Api`, `Mt5Worker`, `FixWorker` | integration project does not host/start the workers or API |
| No project | §66 `TradeReconstruction` / `Scoring` / `Shadow` / `Risk` / `Execution` | those assemblies do not exist |

These are design/scaffold facts. They do **not** change the answer “which existing projects are not in the solution.”

---

## 8. Scaffold depth (do not confuse with sln coverage)

Every library under `src\` is a default `Class1` in its project namespace:

- `D:\Prop\src\Domain\Class1.cs` — `namespace TraderIntelligence.Domain`
- `D:\Prop\src\Application\Class1.cs` — `namespace TraderIntelligence.Application`
- `D:\Prop\src\Infrastructure\Class1.cs` — `namespace TraderIntelligence.Infrastructure`
- `D:\Prop\src\Mt5\Class1.cs` — `namespace TraderIntelligence.Mt5`
- `D:\Prop\src\Fix.CTrader\Class1.cs` — `namespace TraderIntelligence.Fix.CTrader`

Hosts:

- `D:\Prop\apps\api\Program.cs` — stock `WebApplication` + `/weatherforecast`
- `D:\Prop\apps\mt5-worker\Worker.cs` — `BackgroundService` that logs once per second
- `D:\Prop\apps\fix-worker\Worker.cs` — same template

Tests:

- `D:\Prop\tests\Unit\UnitTest1.cs` — empty `[Fact] Test1`
- `D:\Prop\tests\Integration\UnitTest1.cs` — empty `[Fact] Test1`

Packages are already wired (FluentValidation, EF Core + Npgsql, StackExchange.Redis, Serilog, Swashbuckle, xUnit, Moq, FluentAssertions) but there is no domain/application code using them.

**Sln coverage: 10/10 product projects. Behavioral coverage: ~0.**

---

## 9. Direct answers

### Which existing product projects are NOT in `Mt5TraderIntelligence.sln`?

**None.**

### Specifically: Domain, Application, Infrastructure, Fix.CTrader, Api, FixWorker, tests?

**All in.** Tests are two projects (`Tests.Unit`, `Tests.Integration`), both nested under the `tests` solution folder.

### Did architecture §66 suggest extra projects, and are those missing from the sln?

§66 suggested extras: `apps/web`, `services/ml-service`, `src/TradeReconstruction`, `src/Scoring`, `src/Shadow`, `src/Risk`, `src/Execution`, `tests/Replay`, `tests/Fix`, `tests/Risk`.

They are **not in the sln because they are not on disk**. That is an architecture-backlog gap, not a “project exists but was forgotten in the `.sln`” gap.

### Anything else existing that the sln does not build?

Yes: first-party `D:\Prop\mt5-sdk` (CMake C++). Vendor MetaQuotes examples (correctly excluded).

---

## 10. What this audit does *not* recommend doing in this pass

No product source was changed. For a later implementation pass (not this file):

1. Do **not** add empty §66 projects to the `.sln` just to “match the diagram.” §66 says do not create duplicates unnecessarily. Create `TradeReconstruction` / `Risk` / `Execution` / etc. when there is code to put in them.
2. Do **not** add vendor SDK `.csproj` files to the product solution.
3. If Visual Studio / `dotnet sln` should also build the C++ SDK, that is a separate decision (CMake integration or a documented out-of-band build). It is not a missing C# project.

---

## Evidence pins

- Solution: `D:\Prop\Mt5TraderIntelligence.sln` lines 6–31 (`Project` entries), 40–81 (configs), 82–93 (`NestedProjects`).
- Architecture: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2423–2470.
- Product `.csproj` roots: `D:\Prop\src\*\TraderIntelligence.*.csproj`, `D:\Prop\apps\*\TraderIntelligence.*.csproj`, `D:\Prop\tests\*\TraderIntelligence.Tests.*.csproj`.
- Empty §66 parents: `D:\Prop\services\`, `D:\Prop\docs\` (directories exist, no children / no `.csproj`).
- C++ SDK: `D:\Prop\mt5-sdk\CMakeLists.txt` line 2 `project(mt5sdk LANGUAGES CXX)`.
