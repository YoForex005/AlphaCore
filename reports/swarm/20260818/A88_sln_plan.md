# A88 — Plan: add all projects to `Mt5TraderIntelligence.sln` with nested `src` / `apps` / `tests` folders

| Field | Value |
|---|---|
| Agent | A88 (senior engineer, plan only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Target file | `D:\Prop\Mt5TraderIntelligence.sln` (95 lines, VS 17 / format 12.00) |
| Product source modified | **No.** This report is the only write. |
| Precedence | Architecture §66 (“adapt to the existing repo; do not create duplicates”) + on-disk `.csproj` census. A11 is a 2026-08-18 membership snapshot; **superseded here** on `Directory.Build.props` (now present) and `apps/web` (now present). |

---

## 0. Verdict

**Do not re-add the ten product `.csproj` files. They are already in the solution and already nested under `src` / `apps` / `tests`.**

Measured just now (`dotnet sln D:\Prop\Mt5TraderIntelligence.sln list`, exit 0):

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

| Check | Result |
|---|---|
| Product `TraderIntelligence*.csproj` on disk | **10** |
| Same 10 listed in the `.sln` | **yes** |
| Solution folders `src`, `apps`, `tests` exist | **yes** |
| All 10 C# GUIDs appear in `NestedProjects` | **yes** |
| Orphan C# project at solution root | **none** |
| Existing product `.csproj` missing from `.sln` | **none** |
| `apps/web` in Solution Explorer | **no** (Vite/React; no `.csproj`) |
| First-party C++ `mt5-sdk` in `.sln` | **no** (CMake; keep out unless a later decision) |
| Vendor MetaQuotes examples in product `.sln` | **correctly absent** |

An implementation pass that blindly runs `dotnet sln add` on the ten projects will either no-op or fail with “already in the solution.” That is not a defect.

**What this plan authorizes later (not this file):** optional Solution Explorer visibility for `apps/web` and repo files; a freeze on existing project GUIDs; a recipe for *future* projects. It does **not** authorize creating empty §66 assemblies, wrapping CMake, or importing vendor `.csproj` files.

---

## 1. Binding rules

1. **One product solution:** `D:\Prop\Mt5TraderIntelligence.sln`. Do not create a second `.sln` at repo root.
2. **Three solution folders only** for product code: `src`, `apps`, `tests`. Map 1:1 to the disk roots `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`.
3. **No extra click-through folders** (`src/Domain`, `apps/api`, …). Each leaf is one project. Deeper nesting adds noise.
4. **Preserve existing GUIDs.** Every C# GUID is already wired in `ProjectConfigurationPlatforms` and `NestedProjects`. Regenerating them is a churn-only change.
5. **Add via `dotnet sln`** for `.csproj` membership. Hand-edit only for solution folders / `SolutionItems` that the CLI cannot express cleanly.
6. **Do not create `.csproj` files to “fill” architecture §66.** §66 is a *possible* tree. Domain already hosts Reconstruction / Scoring / Shadow / Risk / Execution types. Empty sibling projects would duplicate.
7. **Never add** anything under `D:\Prop\mt5-sdk\vendor\`.
8. **This pass does not touch product source.** A later coding wave may edit the `.sln` only.

---

## 2. Current solution (measured)

### 2.1 Solution folders (type `{2150E333-8FDC-42A3-9474-1A3956D46DE8}`)

| Folder | GUID | Keep? |
|---|---|---|
| `src` | `{284C6660-B861-4055-A709-12AF82034A9B}` | **keep** |
| `apps` | `{91FA4D5C-2CC5-44A5-9805-42A3EEA00429}` | **keep** |
| `tests` | `{355D29D7-21E1-4D1D-A68D-93FFB512E960}` | **keep** |

### 2.2 C# projects (type `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`)

All ten have `Debug\|Any CPU` and `Release\|Any CPU` with `ActiveCfg` + `Build.0`. Solution folders correctly have **no** config rows.

| # | Name | Relative path | GUID | Nested under | Sdk |
|---|---|---|---|---|---|
| 1 | `TraderIntelligence.Mt5` | `src\Mt5\TraderIntelligence.Mt5.csproj` | `{CCD4D49A-9F3E-4795-AA56-CFBF87526E94}` | `src` | `Microsoft.NET.Sdk` |
| 2 | `TraderIntelligence.Mt5Worker` | `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | `{31DFD31A-7E82-4968-912F-397C3E7DEE61}` | `apps` | `Microsoft.NET.Sdk.Worker` |
| 3 | `TraderIntelligence.Domain` | `src\Domain\TraderIntelligence.Domain.csproj` | `{A70A2194-62B9-4B2E-96DB-9725BEA5D9D7}` | `src` | `Microsoft.NET.Sdk` |
| 4 | `TraderIntelligence.Application` | `src\Application\TraderIntelligence.Application.csproj` | `{8A0BB7FD-D1CC-46B3-9C0C-6A2408866F36}` | `src` | `Microsoft.NET.Sdk` |
| 5 | `TraderIntelligence.Infrastructure` | `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `{14EDD461-7C2D-43AC-BC2B-F2DCAC644491}` | `src` | `Microsoft.NET.Sdk` |
| 6 | `TraderIntelligence.Fix.CTrader` | `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | `{76085664-A639-4C0D-8F92-264963416855}` | `src` | `Microsoft.NET.Sdk` |
| 7 | `TraderIntelligence.Api` | `apps\api\TraderIntelligence.Api.csproj` | `{D17266FA-2F65-4F00-9701-BC5DD52B8439}` | `apps` | `Microsoft.NET.Sdk.Web` |
| 8 | `TraderIntelligence.FixWorker` | `apps\fix-worker\TraderIntelligence.FixWorker.csproj` | `{63112B54-6D05-481D-B2F6-99AF3A795192}` | `apps` | `Microsoft.NET.Sdk.Worker` |
| 9 | `TraderIntelligence.Tests.Unit` | `tests\Unit\TraderIntelligence.Tests.Unit.csproj` | `{AA462E32-BFEF-4CB8-BDC6-B47F3609DFF9}` | `tests` | `Microsoft.NET.Sdk` (`IsTestProject`) |
| 10 | `TraderIntelligence.Tests.Integration` | `tests\Integration\TraderIntelligence.Tests.Integration.csproj` | `{40962285-3955-4D71-8C25-4596EDA39D98}` | `tests` | `Microsoft.NET.Sdk` (`IsTestProject`) |

`NestedProjects` (`.sln` lines 82–93) already maps every C# GUID → one of the three folder GUIDs. No orphan. No double-nest.

### 2.3 Target Solution Explorer (already true)

```text
Mt5TraderIntelligence
├── src
│   ├── TraderIntelligence.Domain
│   ├── TraderIntelligence.Application
│   ├── TraderIntelligence.Infrastructure
│   ├── TraderIntelligence.Mt5
│   └── TraderIntelligence.Fix.CTrader
├── apps
│   ├── TraderIntelligence.Api
│   ├── TraderIntelligence.Mt5Worker
│   └── TraderIntelligence.FixWorker
└── tests
    ├── TraderIntelligence.Tests.Unit
    └── TraderIntelligence.Tests.Integration
```

Visual Studio sorts children alphabetically inside each folder. The `.sln` declaration order (Mt5 / Mt5Worker first, then Domain…) is historical and **must not** be rewritten just to look nicer.

---

## 3. Disk census vs solution

### 3.1 Product `.csproj` — all in

| Disk path | In sln | Nested folder |
|---|---|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | yes | `src` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | yes | `src` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | yes | `src` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | yes | `src` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | yes | `src` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | yes | `apps` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | yes | `apps` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | yes | `apps` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | yes | `tests` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | yes | `tests` |

**Product `.csproj` files not in the solution: 0.**

### 3.2 Present on disk, not a C# project

| Path | Kind | Add to `.sln`? |
|---|---|---|
| `D:\Prop\apps\web\` (`package.json`, Vite + React 18) | frontend app | **Optional visibility only** — solution folder `web` under `apps` + a few `SolutionItems`. Not buildable by MSBuild. |
| `D:\Prop\Directory.Build.props` | repo MSBuild defaults | **Optional** `SolutionItems` at root (so VS shows it). Already auto-imported by every project; listing it does not change build. |
| `D:\Prop\docs\architecture.md` | overview note | **Optional** `docs` solution folder + `SolutionItems`. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | C++20 first-party SDK | **Do not add** in this wave. Different toolchain. Documented out-of-band (`cmake --build`). |
| `D:\Prop\services\` | empty directory | **Do not add.** No `ml-service` yet (A52 / §69: ML is not first useful version). |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\**\*.csproj` | MetaQuotes samples | **Never add.** |

### 3.3 Architecture §66 nodes that are **not** missing from the `.sln`

These are **uncreated**, not forgotten:

| §66 path | On disk 2026-08-18 | Action |
|---|---|---|
| `/apps/web` | yes (no `.csproj`) | optional SolutionItems only |
| `/apps/api`, `/apps/mt5-worker`, `/apps/fix-worker` | yes | already in sln |
| `/src/Domain`, `/Application`, `/Infrastructure`, `/Mt5`, `/Fix.CTrader` | yes | already in sln |
| `/src/TradeReconstruction`, `/Scoring`, `/Shadow`, `/Risk`, `/Execution` | **no** (logic lives as folders *inside* Domain) | **do not create projects** |
| `/tests/Unit`, `/Integration` | yes | already in sln |
| `/tests/Replay`, `/Fix`, `/Risk` | **no** | add only when A27 inventory is implemented as real projects |
| `/services/ml-service` | **no** | out of first-useful-version scope |

---

## 4. What a later implementation wave should do

Three phases. Phase A is a verify-only no-op. Phase B is optional Solution Explorer hygiene. Phase C is the recipe for *future* `.csproj` files.

### Phase A — Verify membership (required, expected no-op)

From `D:\Prop`:

```powershell
dotnet sln .\Mt5TraderIntelligence.sln list
```

Accept only the ten paths in §0. Then confirm nesting without rewriting GUIDs:

```powershell
Select-String -Path .\Mt5TraderIntelligence.sln -Pattern 'NestedProjects' -Context 0,12
```

Expected ten mappings (child → parent):

```
{CCD4D49A-9F3E-4795-AA56-CFBF87526E94} = {284C6660-B861-4055-A709-12AF82034A9B}   # Mt5            → src
{31DFD31A-7E82-4968-912F-397C3E7DEE61} = {91FA4D5C-2CC5-44A5-9805-42A3EEA00429}   # Mt5Worker      → apps
{A70A2194-62B9-4B2E-96DB-9725BEA5D9D7} = {284C6660-B861-4055-A709-12AF82034A9B}   # Domain         → src
{8A0BB7FD-D1CC-46B3-9C0C-6A2408866F36} = {284C6660-B861-4055-A709-12AF82034A9B}   # Application    → src
{14EDD461-7C2D-43AC-BC2B-F2DCAC644491} = {284C6660-B861-4055-A709-12AF82034A9B}   # Infrastructure → src
{76085664-A639-4C0D-8F92-264963416855} = {284C6660-B861-4055-A709-12AF82034A9B}   # Fix.CTrader    → src
{D17266FA-2F65-4F00-9701-BC5DD52B8439} = {91FA4D5C-2CC5-44A5-9805-42A3EEA00429}   # Api            → apps
{63112B54-6D05-481D-B2F6-99AF3A795192} = {91FA4D5C-2CC5-44A5-9805-42A3EEA00429}   # FixWorker      → apps
{AA462E32-BFEF-4CB8-BDC6-B47F3609DFF9} = {355D29D7-21E1-4D1D-A68D-93FFB512E960}   # Tests.Unit     → tests
{40962285-3955-4D71-8C25-4596EDA39D98} = {355D29D7-21E1-4D1D-A68D-93FFB512E960}   # Tests.Integration → tests
```

**If (and only if) a product `.csproj` is later removed from the sln**, re-add with the matching folder. Do **not** run these today against the current file — they will error “already contains”:

```powershell
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Domain\TraderIntelligence.Domain.csproj               --solution-folder src
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Application\TraderIntelligence.Application.csproj     --solution-folder src
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Infrastructure\TraderIntelligence.Infrastructure.csproj --solution-folder src
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Mt5\TraderIntelligence.Mt5.csproj                     --solution-folder src
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj     --solution-folder src
dotnet sln .\Mt5TraderIntelligence.sln add .\apps\api\TraderIntelligence.Api.csproj                    --solution-folder apps
dotnet sln .\Mt5TraderIntelligence.sln add .\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj       --solution-folder apps
dotnet sln .\Mt5TraderIntelligence.sln add .\apps\fix-worker\TraderIntelligence.FixWorker.csproj       --solution-folder apps
dotnet sln .\Mt5TraderIntelligence.sln add .\tests\Unit\TraderIntelligence.Tests.Unit.csproj           --solution-folder tests
dotnet sln .\Mt5TraderIntelligence.sln add .\tests\Integration\TraderIntelligence.Tests.Integration.csproj --solution-folder tests
```

`dotnet sln add --solution-folder X` creates folder `X` if missing and writes `NestedProjects`. It does **not** support re-nesting an already-listed project; that requires `dotnet sln remove` + `add` (GUID change risk) or a surgical `NestedProjects` edit. **Do not remove/re-add just to nest — nesting is already correct.**

Build / test gate after any sln edit:

```powershell
dotnet build .\Mt5TraderIntelligence.sln -c Debug
dotnet test  .\Mt5TraderIntelligence.sln -c Debug --no-build
```

Both must stay green vs the pre-edit baseline. Sln membership changes must not alter compile output.

### Phase B — Optional Solution Explorer visibility (recommended, sln-only)

Goal: Solution Explorer matches the repo the operators actually open, without pretending Vite or CMake are MSBuild projects.

**B1. `apps/web` as a nested solution folder**

`dotnet sln` cannot attach non-project files. Hand-insert after the existing `apps` folder project (keep folder GUID stable):

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "web", "web", "{A88WEB00-0A62-4C18-9B7E-2F1D6C8A9041}"
	ProjectSection(SolutionItems) = preProject
		apps\web\package.json = apps\web\package.json
		apps\web\vite.config.ts = apps\web\vite.config.ts
		apps\web\tsconfig.json = apps\web\tsconfig.json
		apps\web\index.html = apps\web\index.html
	EndProjectSection
EndProject
```

Add one `NestedProjects` line so `web` sits under `apps`, not at the root:

```
		{A88WEB00-0A62-4C18-9B7E-2F1D6C8A9041} = {91FA4D5C-2CC5-44A5-9805-42A3EEA00429}
```

Do **not** add every file under `apps/web/src`. Four entry files are enough to jump from VS to the Vite app. Do **not** invent a `TraderIntelligence.Web.esproj` unless a later wave explicitly wants `dotnet` to launch Vite.

**B2. Repo files as `SolutionItems`**

```
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "docs", "docs", "{A88DOC00-5C7E-4D18-8A33-9F0C1E6B2D44}"
	ProjectSection(SolutionItems) = preProject
		docs\architecture.md = docs\architecture.md
	EndProjectSection
EndProject
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "repo", "repo", "{A88REPO0-3B59-4F02-91AE-6C8D2F4B0A15}"
	ProjectSection(SolutionItems) = preProject
		Directory.Build.props = Directory.Build.props
	EndProjectSection
EndProject
```

Leave `docs` and `repo` at solution root (siblings of `src` / `apps` / `tests`). Do not nest them under `src`.

**B3. Optional `ExtensibilityGlobals`**

VS 17 usually writes this on first save. Harmless to add once; do not churn the GUID later:

```
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {A88SLN00-7E11-4A02-9C44-5B8F2D1A0C73}
	EndGlobalSection
```

**B4. Do not change project-type GUIDs**

Entries use legacy C# type `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`. SDK-style `{9A19103F-16F7-4668-BE54-9A1E7A4F7556}` also works. Converting all ten is cosmetic. Leave them.

### Phase C — Recipe when a *new* product `.csproj` is created

Only after the project exists on disk with real code (not an empty `Class1`).

| New disk path | `--solution-folder` |
|---|---|
| `src/<Name>/TraderIntelligence.<Name>.csproj` | `src` |
| `apps/<kebab>/TraderIntelligence.<Name>.csproj` | `apps` |
| `tests/<Lane>/TraderIntelligence.Tests.<Lane>.csproj` | `tests` |

```powershell
dotnet sln .\Mt5TraderIntelligence.sln add .\tests\Replay\TraderIntelligence.Tests.Replay.csproj --solution-folder tests
dotnet sln .\Mt5TraderIntelligence.sln add .\tests\Fix\TraderIntelligence.Tests.Fix.csproj       --solution-folder tests
```

Candidates from A27 (not authorized by this plan to create):

- `tests/Replay` → `TraderIntelligence.Tests.Replay`
- `tests/Fix` → `TraderIntelligence.Tests.Fix`

A27 explicitly does **not** want a separate `tests/Risk` project (risk tests stay in Unit / Integration / Fix). Honor that.

If Domain later splits (only if a reviewer proves the assembly is too large):

```powershell
dotnet sln .\Mt5TraderIntelligence.sln add .\src\Risk\TraderIntelligence.Risk.csproj --solution-folder src
```

Until then, keep Reconstruction / Scoring / Shadow / Risk / Execution **inside** `TraderIntelligence.Domain`.

---

## 5. What must never enter this solution

| Item | Why |
|---|---|
| `mt5-sdk/vendor/**/*.csproj` and `All-Examples.sln` | MetaQuotes samples. License/noise. Not product. |
| Empty §66 `src/TradeReconstruction` etc. | Duplicates Domain folders; §66 says do not create unnecessarily. |
| `services/ml-service` stub | A52 / §69: ML is not first useful version. |
| CMake `mt5-sdk` as a fake `.csproj` | Lies about the toolchain. Build with CMake on Windows x64 (A54). |
| A `TraderIntelligence.Web` MSBuild project that only shells `npm` | Optional later. Not required to “add all projects.” |
| Second solution at repo root | Splits `dotnet build` / VS F5. |

---

## 6. Reference graph (membership is complete; edges are not the sln’s job)

Current `ProjectReference` edges (all targets are **in** the sln):

```
Domain
  ↑
Application
  ↑
  ├── Infrastructure  →  Api, Mt5Worker, FixWorker, Tests.Integration
  ├── Mt5             →  Mt5Worker
  └── Fix.CTrader     →  FixWorker, Tests.Unit, Tests.Integration
```

Known **reference holes** (do **not** “fix” them by sln edits):

| Observer | Does not reference | Not a sln defect |
|---|---|---|
| `Api` | `Mt5`, `Fix.CTrader` | API must not load Manager DLL / FIX sessions |
| `Tests.Unit` | `Infrastructure`, `Mt5` | add only when those units exist |
| `Tests.Integration` | `Mt5`, `Api`, workers | add when host fixtures exist |

Adding a project to the `.sln` does not add a `ProjectReference`. Keep those concerns separate.

---

## 7. Acceptance gates for a later sln edit

A sln change is **PASS** only if all of these are true:

1. `dotnet sln list` still shows exactly the ten product `.csproj` paths in §0 (plus any *new* product project that was intentionally created on disk).
2. `NestedProjects` still maps every C# GUID to `src` / `apps` / `tests` (or a child of `apps` such as `web`).
3. No vendor path appears.
4. Existing ten C# GUIDs are **unchanged**.
5. `dotnet build Mt5TraderIntelligence.sln -c Debug` and `-c Release` succeed.
6. `dotnet test Mt5TraderIntelligence.sln` exit code matches the pre-edit baseline (today: projects load; test *content* is a different audit).
7. Product `.cs` / `.csproj` source files are untouched unless a *new* project was an agreed deliverable.

A sln change is **FAIL** if it:

- creates empty §66 projects “to match the diagram,”
- imports `mt5-sdk/vendor`,
- moves projects to the solution root,
- regenerates GUIDs,
- or claims “projects added” when the ten were already members.

---

## 8. Recommended later-wave sequence (if Phase B is approved)

1. Snapshot `git diff --stat -- Mt5TraderIntelligence.sln` (should be empty before edit).
2. Insert `web` / `docs` / `repo` folder blocks + `NestedProjects` line for `web` (§4 Phase B).
3. Optionally add `ExtensibilityGlobals`.
4. Re-run Phase A verify + §7 build/test gates.
5. Commit **only** `Mt5TraderIntelligence.sln` with message: `chore(sln): nest web/docs/repo solution items under existing folders`.

Do **not** combine this with Domain/API/worker code changes.

---

## 9. Honest restatement

| Question | Answer |
|---|---|
| Are all existing product projects in `Mt5TraderIntelligence.sln`? | **Yes. 10/10.** |
| Are they nested under `src` / `apps` / `tests`? | **Yes.** Folders and `NestedProjects` already exist. |
| What should an implementer add *today*? | **Nothing required.** Phase A is verify-only. |
| What is still missing from Solution Explorer? | `apps/web` (Vite), `docs/architecture.md`, `Directory.Build.props` as items — optional Phase B. |
| What is still missing from the *product* (not the sln)? | §66 extras that were never created; C++ SDK lives in CMake; web has no `.csproj`. |

**Sln coverage: 10/10 product `.csproj`, already nested. Implementation of this plan’s Phase A is a no-op. Phase B is cosmetic visibility only.**

---

## Evidence pins

- Solution: `D:\Prop\Mt5TraderIntelligence.sln` lines 6–31 (`Project`), 40–81 (`ProjectConfigurationPlatforms`), 82–93 (`NestedProjects`).
- CLI: `dotnet sln D:\Prop\Mt5TraderIntelligence.sln list` (2026-08-18) — ten paths, exit 0.
- Architecture §66: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 2423–2470.
- Prior membership audit: `D:\Prop\reports\swarm\20260818\A11_solution_coverage.md` (stale on `Directory.Build.props` and `apps/web`).
- Web exists: `D:\Prop\apps\web\package.json`.
- Repo MSBuild defaults: `D:\Prop\Directory.Build.props`.
- Future test projects: `D:\Prop\reports\swarm\20260818\A27_test_inventory.md`.
- Deploy split (why CMake stays out): `D:\Prop\reports\swarm\20260818\A54_deployment_split.md`.
