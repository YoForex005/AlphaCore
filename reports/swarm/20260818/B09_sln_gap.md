# B09 — `Mt5TraderIntelligence.sln` missing-project gap

| Field | Value |
|---|---|
| Agent | B09 (senior engineer, membership gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:17:22+05:30 |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\Mt5TraderIntelligence.sln` (94 lines, 7019 bytes, VS 17 / format 12.00) |
| SHA-256 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` |
| Product source modified | **No.** This report is the only write. |
| CLI | `dotnet sln D:\Prop\Mt5TraderIntelligence.sln list` — exit 0, **10** paths |
| Precedence | On-disk census + `.sln` text + architecture §66. Supersedes A11 on `Directory.Build.props`, `apps/web`, `docs/architecture.md`, Domain source depth, and test `.cs` files. Does **not** supersede A88’s “do not re-add the ten” plan. |

---

## 0. Verdict

**No existing product `.csproj` is missing from the solution. No solution entry is a dangling path.**

“Missing project” is three different questions. Mixing them is how A11-era notes get re-opened as sln defects.

| Question | Count | Answer |
|---|---|---|
| Product `TraderIntelligence*.csproj` on disk but **not** in the `.sln` | **0** | 10/10 listed |
| `.sln` `Project` path that **does not exist** on disk | **0** | all ten resolve |
| First-party **non-MSBuild** buildable tree not in the `.sln` | **2** | `apps/web` (Vite) and `mt5-sdk` (CMake) — not C# projects |
| Architecture §66 **uncreated** C# assemblies | **5 + 2 proposed test + 1 hold** | not forgotten sln members; they were never created |
| Solution Explorer visibility (no `SolutionItems`) | **3 items** | `apps/web`, `docs/architecture.md`, `Directory.Build.props` |

**Sln membership PASS. Implementation completeness is out of scope.** Do not run `dotnet sln add` against the ten product projects; they already belong. Do not create empty §66 sibling assemblies to “fill” this list.

---

## 1. Method

| Source | Path |
|---|---|
| Solution | `D:\Prop\Mt5TraderIntelligence.sln` (`Project` lines 6–31, configs 40–81, `NestedProjects` 82–93) |
| CLI membership | `dotnet sln D:\Prop\Mt5TraderIntelligence.sln list` (exit 0) |
| Product `.csproj` census | recursive `*.csproj` under `D:\Prop` excluding `bin`/`obj` |
| Architecture §66 | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2423–2470 |
| Prior membership | `A11_solution_coverage.md` (stale), `A88_sln_plan.md` (still correct on the ten) |
| Proposed extra tests | `A27_test_inventory.md` (Replay + Fix; **not** `tests/Risk`) |
| ML hold | `A52_ml_not_yet.md` (`services/ml-service` is Phase 6) |

Every `Project("{FAE04EC0-…}")` path was `Test-Path`’d. Every product `TraderIntelligence*.csproj` under `src\`, `apps\`, `tests\` was matched back to a `Project` line. Vendor trees under `mt5-sdk\vendor\` were inventoried only to exclude them.

---

## 2. What the solution actually contains

Three solution folders (type `{2150E333-8FDC-42A3-9474-1A3956D46DE8}`) and ten C# projects (type `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`).

`dotnet sln list` (measured this pass):

```
apps\api\TraderIntelligence.Api.csproj
apps\fix-worker\TraderIntelligence.FixWorker.csproj
apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj
src\Application\TraderIntelligence.Application.csproj
src\Domain\TraderIntelligence.Domain.csproj
src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj
src\Infrastructure\TraderIntelligence.Infrastructure.csproj
src\Mt5\TraderIntelligence.Mt5.csproj
tests\Integration\TraderIntelligence.Tests.Integration.csproj
tests\Unit\TraderIntelligence.Tests.Unit.csproj
```

### 2.1 Solution folders

| Folder | GUID | Nested C# children |
|---|---|---|
| `src` | `{284C6660-B861-4055-A709-12AF82034A9B}` | Domain, Application, Infrastructure, Mt5, Fix.CTrader |
| `apps` | `{91FA4D5C-2CC5-44A5-9805-42A3EEA00429}` | Api, Mt5Worker, FixWorker |
| `tests` | `{355D29D7-21E1-4D1D-A68D-93FFB512E960}` | Tests.Unit, Tests.Integration |

No `SolutionItems`. No `ExtensibilityGlobals`. No `web` / `docs` / `repo` folder.

### 2.2 C# projects — every path exists

| # | Name | Relative path | GUID | Nested | Disk | Debug+Release `Build.0` |
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

All ten GUIDs unique. All ten appear in `NestedProjects`. All ten have `Debug|Any CPU` and `Release|Any CPU` `ActiveCfg` + `Build.0`. Solution folders correctly have **no** config rows. No C# project sits at solution root.

**Broken `.sln` entries: none.**

---

## 3. Disk → sln census (product `.csproj`)

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

Product `.csproj` files on disk that are **not** in `Mt5TraderIntelligence.sln`: **none.**

There is no other first-party `*.csproj` / `*.vcxproj` / `*.esproj` / `*.fsproj` outside `mt5-sdk\vendor\`.

---

## 4. The missing-project list (classified)

### 4.1 Class A — existing product C# project omitted from the `.sln`

**Empty.**

This is the only class that would justify `dotnet sln add` today.

### 4.2 Class B — `.sln` path with no file on disk

**Empty.** Visual Studio / `dotnet build` will not hit a missing `.csproj` from this solution.

### 4.3 Class C — first-party buildable trees that are **not** C# projects (intentionally outside MSBuild)

These exist on disk and are **not** members. They are coverage facts, not broken `.sln` paths.

| Path | Kind | In sln? | Add? |
|---|---|---|---|
| `D:\Prop\apps\web\` (`package.json`, Vite + React 18, 23 `src` `.ts`/`.tsx` files) | frontend app | **no** | Optional Solution Explorer visibility only (A88 Phase B). Do **not** invent `TraderIntelligence.Web.esproj` in this wave. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` (`project(mt5sdk LANGUAGES CXX)`) | C++20 first-party SDK | **no** | **Do not add.** Different toolchain. Build with CMake (A54 / A88). |

`apps/web` is the only **product app** a Solution Explorer user cannot click. That is a **visibility gap**, not a missing `.csproj`.

### 4.4 Class D — architecture §66 nodes that were **never created** as projects

§66 says *“Adapt to the existing repo; do not create duplicates unnecessarily.”* These are **uncreated**, not forgotten.

| §66 path | On disk 2026-08-18 | Has `.csproj`? | In sln? | Classification |
|---|---|---|---|---|
| `/apps/web` | **yes** (no `.csproj`) | no | no | Class C visibility |
| `/apps/api`, `/mt5-worker`, `/fix-worker` | yes | yes | yes | covered |
| `/src/Domain`, `/Application`, `/Infrastructure`, `/Mt5`, `/Fix.CTrader` | yes | yes | yes | covered |
| `/src/TradeReconstruction` | **no** (logic in `src\Domain\Reconstruction\`) | no | no | do **not** create a sibling project |
| `/src/Scoring` | **no** (logic in `src\Domain\Scoring\`) | no | no | do **not** create |
| `/src/Shadow` | **no** (logic in `src\Domain\Shadow\`) | no | no | do **not** create |
| `/src/Risk` | **no** (logic in `src\Domain\Risk\`) | no | no | do **not** create |
| `/src/Execution` | **no** (logic in `src\Domain\Execution\`) | no | no | do **not** create |
| `/tests/Unit`, `/Integration` | yes | yes | yes | covered (projects exist; **zero** `.cs` sources — see §6) |
| `/tests/Replay` | **no** | no | no | proposed later (`TraderIntelligence.Tests.Replay`, A27 / A67) |
| `/tests/Fix` | **no** | no | no | proposed later (`TraderIntelligence.Tests.Fix`, A27 / A68) |
| `/tests/Risk` | **no** | no | no | A27: **do not** create; risk tests stay in Unit / Integration / Fix |
| `/services/ml-service` | **no** (`D:\Prop\services` is empty) | no | no | A52 Phase-6 **hold**. Correct absence. |
| `/docs/*.md` (11 named) | only `architecture.md` | n/a | no `SolutionItems` | docs backlog, not a `.csproj` gap |

**Uncreated §66 *code* nodes that must not be treated as sln defects:**

1. `src/TradeReconstruction`
2. `src/Scoring`
3. `src/Shadow`
4. `src/Risk`
5. `src/Execution`
6. `tests/Replay` (authorized as a future project only after fixtures exist)
7. `tests/Fix` (same)
8. `tests/Risk` (**do not create**)
9. `services/ml-service` (**do not create** now)

### 4.5 Class E — Solution Explorer items (A88 Phase B, optional, sln-only)

Not projects. Missing from Solution Explorer because there is no `ProjectSection(SolutionItems)`:

| Item | Disk | In sln? |
|---|---|---|
| `apps/web/package.json` (+ vite/tsconfig/index.html) | yes | no |
| `docs/architecture.md` | yes | no |
| `Directory.Build.props` | yes | no |

A11 claimed no `Directory.Build.props` and no `apps/web`. Both are on disk now. A88 already planned the optional folder blocks. This file does **not** authorize editing the `.sln`.

---

## 5. Vendor projects correctly excluded (not gaps)

Six MetaQuotes sample `.csproj` files under `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\`:

| Vendor project | Why excluded |
|---|---|
| `Gateway\TextFeeder.NET\TextFeeder.NET.csproj` | MetaQuotes sample |
| `Gateway\UniFeeder.NET\UniFeeder.NET.csproj` | MetaQuotes sample |
| `Manager\BalanceExample.NET\BalanceExample.NET.csproj` | MetaQuotes sample |
| `Manager\DealerExample.NET\DealerExample.NET.csproj` | MetaQuotes sample |
| `Web\NET\MetaQuotes.MT5WebAPI\MetaQuotes.MT5WebAPI.csproj` | MetaQuotes sample |
| `Web\NET\WebTrader\WebTrader.csproj` | MetaQuotes sample |

Also excluded: vendor `All-Examples.sln` and ~30 sample `.sln` files. **Never add** any path under `mt5-sdk\vendor\` to `Mt5TraderIntelligence.sln`.

---

## 6. Do not confuse sln membership with product completeness

A11’s “every library is `Class1` / `UnitTest1`” snapshot is **stale**. Measured this pass (`.cs` excluding `bin`/`obj`):

| Tree | `.cs` files |
|---|---|
| `src\Domain` | 47 |
| `src\Application` | 3 |
| `src\Infrastructure` | 5 |
| `src\Mt5` | 4 |
| `src\Fix.CTrader` | 4 |
| `apps\api` | 1 |
| `apps\mt5-worker` | 2 |
| `apps\fix-worker` | 2 |
| `tests\Unit` | **0** (only the `.csproj`) |
| `tests\Integration` | **0** (only the `.csproj`) |

`Class1.cs` and `UnitTest1.cs` are gone. The two test **projects** are in the sln; they contain **no test source**. That is a test-content gap (A09/A10/A27), **not** a missing-project gap.

`apps/web` now has pages (Overview, Traders, Groups, Brokers, FixSessions, Reconciliation, Risk, Scoring, Settings, Shadow, SystemHealth, TradeExplorer, TraderDetail). A62’s “13 imports / 0 page files” is stale. Still no `.csproj`.

---

## 7. ProjectReference graph (membership complete; edges are not sln work)

All `ProjectReference` Include paths resolve to projects **in** the sln. No dangling reference to a missing `.csproj`.

```
Domain
  ↑
Application
  ↑
  ├── Infrastructure  →  also references Mt5
  │     ↑
  │     ├── Api
  │     ├── Mt5Worker   →  also references Mt5
  │     ├── FixWorker   →  also references Fix.CTrader
  │     └── Tests.Integration
  ├── Mt5
  └── Fix.CTrader
        ↑
        ├── FixWorker
        ├── Tests.Unit
        └── Tests.Integration
```

A88 §6 omitted the **Infrastructure → Mt5** edge (now present in `TraderIntelligence.Infrastructure.csproj`). That is a reference-graph update, not a sln membership change.

Known **reference holes** (do **not** “fix” by sln edits):

| Observer | Does not reference | Not a sln defect |
|---|---|---|
| `Api` | `Mt5` (direct), `Fix.CTrader` | API must not load Manager DLL / FIX sessions |
| `Tests.Unit` | `Infrastructure`, `Mt5` | add only when those units exist |
| `Tests.Integration` | `Mt5`, `Api`, workers | add when host fixtures exist |

Adding a project to the `.sln` does not add a `ProjectReference`.

---

## 8. Direct answers

### Which existing product projects are missing from `Mt5TraderIntelligence.sln`?

**None. 10/10.**

### Which `.sln` projects are missing on disk?

**None.**

### What *is* missing, if an implementer still asks for a list?

| Missing thing | Class | Action |
|---|---|---|
| `apps/web` in Solution Explorer | C / E | optional A88 Phase B `SolutionItems`; no `.csproj` |
| `mt5-sdk` CMake project in the `.sln` | C | keep out |
| `tests/Replay` (`TraderIntelligence.Tests.Replay`) | D | create only with real replay fixtures (A67) |
| `tests/Fix` (`TraderIntelligence.Tests.Fix`) | D | create only with the in-process venue (A68) |
| `tests/Risk` as its own project | D | **do not create** (A27) |
| `src/TradeReconstruction`, `Scoring`, `Shadow`, `Risk`, `Execution` as assemblies | D | **do not create**; they live inside Domain |
| `services/ml-service` | D | **do not create** (A52) |
| Ten of eleven §66 `docs/*.md` | docs | not a sln project |
| Any `.cs` under `tests\Unit` / `tests\Integration` | test content | not a sln project |

### Did A11 / A88 get this wrong?

- **A88 membership conclusion still holds:** do not re-add the ten.
- **A11 is stale** on `Directory.Build.props` (now present), `apps/web` (now present), `docs/` (now has `architecture.md`), Domain scaffold (`Class1` gone; 47 files), and `UnitTest1.cs` (gone; tests are empty projects).
- **A88 reference graph is slightly stale:** Infrastructure now references Mt5.

---

## 9. What this pass must not do

No product source and no `.sln` was changed. For a later wave:

1. Do **not** `dotnet sln add` the ten existing projects.
2. Do **not** create empty §66 assemblies to match the diagram.
3. Do **not** import `mt5-sdk\vendor\**\*.csproj`.
4. Do **not** wrap CMake as a fake `.csproj`.
5. Do **not** add `services/ml-service` or `tests/Risk`.
6. If Phase B visibility is approved, edit **only** `Mt5TraderIntelligence.sln` and keep the ten C# GUIDs unchanged (A88 §4 / §7).

---

## 10. Honest restatement

| Question | Answer |
|---|---|
| Are all existing product projects in the sln? | **Yes. 10/10.** |
| Are they nested under `src` / `apps` / `tests`? | **Yes.** |
| Broken sln paths? | **None.** |
| Existing-but-unlisted product `.csproj`? | **None.** |
| Missing from Solution Explorer? | `apps/web`, `docs/architecture.md`, `Directory.Build.props` (optional). |
| Missing from the *product* (not the sln)? | Uncreated §66 extras; C++ SDK lives in CMake; test projects have no `.cs`; ML service is on hold. |

**Sln coverage: 10/10 product `.csproj`. Missing-project list for `dotnet sln add`: empty.**

---

## Evidence pins

- Solution: `D:\Prop\Mt5TraderIntelligence.sln` lines 6–31 (`Project`), 40–81 (`ProjectConfigurationPlatforms`), 82–93 (`NestedProjects`). SHA-256 `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4`.
- CLI: `dotnet sln D:\Prop\Mt5TraderIntelligence.sln list` (2026-08-18) — ten paths, exit 0.
- Architecture §66: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2423–2470.
- Prior membership: `D:\Prop\reports\swarm\20260818\A11_solution_coverage.md` (stale), `A88_sln_plan.md` (ten-project plan still valid).
- Web exists: `D:\Prop\apps\web\package.json` + 23 files under `apps\web\src`.
- Repo MSBuild defaults: `D:\Prop\Directory.Build.props`.
- Docs present: `D:\Prop\docs\architecture.md` only.
- Services: `D:\Prop\services\` empty (no `ml-service`).
- Domain hosts §66 extras as folders: `Reconstruction`, `Scoring`, `Shadow`, `Risk`, `Execution`.
- C++ SDK: `D:\Prop\mt5-sdk\CMakeLists.txt` line 2 `project(mt5sdk LANGUAGES CXX)`.
- Vendor `.csproj` count: 6 (all under `mt5-sdk\vendor\`).
- Future test projects: `A27_test_inventory.md`, `A67_replay_harness.md`, `A68_fix_simulator.md`.
- ML hold: `A52_ml_not_yet.md`.
