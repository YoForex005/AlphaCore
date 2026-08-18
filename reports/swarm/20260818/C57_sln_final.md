# C57 — Final `Mt5TraderIntelligence.sln` membership (text parse, no `dotnet sln`)

| Field | Value |
|---|---|
| Agent | C57 (senior engineer, solution membership final) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T (this pass). Sln `LastWriteTimeUtc` **2026-08-18T07:32:00.5167368Z** |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\Mt5TraderIntelligence.sln` |
| Size | **7019** bytes |
| SHA-256 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` |
| Format | Visual Studio Solution File, Format Version **12.00**; `# Visual Studio Version 17`; `VisualStudioVersion = 17.0.31903.59` |
| Product source modified | **No.** This report is the only write. The `.sln` and every product `.csproj` were read, not edited. |
| CLI | **`dotnet sln` was not run.** Task constraint: do not invoke `dotnet sln` when execute is not assumed. Membership is the `Project(...)` text + `Test-Path` + SHA-256. |
| Precedence | **Final membership snapshot** for this wave. Same sln bytes/hash as B09. Supersedes A11 / A88 / B09 on *surrounding* disk facts (test sources, Integration→Mt5 reference, `docs/` count, scratch `.csproj`). Does **not** authorize `dotnet sln add` or empty §66 assemblies. |

---

## 0. Verdict

**10/10 product `.csproj` files are in the solution. 0 dangling solution paths. 0 product `.csproj` omitted.**

| Question | Count | Answer |
|---|---|---|
| Product `TraderIntelligence*.csproj` under `src\` / `apps\` / `tests\` | **10** | all exist |
| Same 10 listed as C# `Project` rows in the `.sln` | **10** | all resolve |
| Solution folders `src` / `apps` / `tests` | **3** | all nested correctly |
| C# GUIDs in `NestedProjects` | **10/10** | no orphan, no double-nest |
| `.sln` path that does **not** exist on disk | **0** | |
| First-party product sln besides this file | **0** | only `D:\Prop\Mt5TraderIntelligence.sln` |
| `dotnet sln add` required today | **none** | already members |

This is a **solution-membership PASS**. It is **not** an implementation PASS, a go-live PASS, or a claim that Vite/CMake live inside MSBuild.

Equivalent of `dotnet sln list` reconstructed from C# `Project` lines (sorted like the CLI):

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

---

## 1. Method (why `dotnet sln` is absent)

| Step | What was used | What was not used |
|---|---|---|
| Read solution | Full file `D:\Prop\Mt5TraderIntelligence.sln` (13 `Project(` rows) | `dotnet sln list` / `add` / `remove` |
| Existence | `Test-Path` on every sln-relative `.csproj` | MSBuild evaluation |
| Hash / size | `Get-FileHash -Algorithm SHA256`; `Get-Item.Length` | |
| Product census | Recursive `*.csproj` / `*.sln` under `D:\Prop`, excluding `bin` / `obj` / `node_modules` | |
| Project files | Read all 10 product `.csproj` + `Directory.Build.props` | No edits |

`Get-Item` / `Get-FileHash` ran successfully (read metadata only). **`dotnet sln` was not invoked**, so this report does not quote a CLI exit code. The reconstructed path list matches A88/B09’s earlier `dotnet sln list` output character-for-character.

Sln identity is unchanged since B09:

| Pin | B09 | C57 |
|---|---|---|
| Bytes | 7019 | **7019** |
| SHA-256 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` | **same** |

---

## 2. What the solution text contains

Three solution folders (type `{2150E333-8FDC-42A3-9474-1A3956D46DE8}`) and ten C# projects (legacy type `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}`).

No `ProjectSection(SolutionItems)`. No `ExtensibilityGlobals`. No `web` / `docs` / `repo` folder. `HideSolutionNode = FALSE`.

Configurations: `Debug|Any CPU`, `Release|Any CPU`. Every C# GUID has `ActiveCfg` + `Build.0` for both. Solution folders correctly have **no** `ProjectConfigurationPlatforms` rows.

### 2.1 Solution folders

| Folder | GUID | Nested C# children |
|---|---|---|
| `src` | `{284C6660-B861-4055-A709-12AF82034A9B}` | Domain, Application, Infrastructure, Mt5, Fix.CTrader |
| `apps` | `{91FA4D5C-2CC5-44A5-9805-42A3EEA00429}` | Api, Mt5Worker, FixWorker |
| `tests` | `{355D29D7-21E1-4D1D-A68D-93FFB512E960}` | Tests.Unit, Tests.Integration |

### 2.2 C# projects (declaration order — do not rewrite)

| # | Sln name | Relative path | GUID | Folder | Disk | `Build.0` Debug+Release |
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

All ten GUIDs unique. All ten appear once in `NestedProjects` (sln lines 83–92). No C# project sits at solution root.

`NestedProjects` (child → parent):

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

### 2.3 Target Solution Explorer (already true)

```text
Mt5TraderIntelligence
├── src
│   ├── TraderIntelligence.Application
│   ├── TraderIntelligence.Domain
│   ├── TraderIntelligence.Fix.CTrader
│   ├── TraderIntelligence.Infrastructure
│   └── TraderIntelligence.Mt5
├── apps
│   ├── TraderIntelligence.Api
│   ├── TraderIntelligence.FixWorker
│   └── TraderIntelligence.Mt5Worker
└── tests
    ├── TraderIntelligence.Tests.Integration
    └── TraderIntelligence.Tests.Unit
```

VS sorts alphabetically inside each folder. Declaration order (Mt5 / Mt5Worker first) is historical and **must not** be rewritten for cosmetics.

---

## 3. Disk → sln census (product `.csproj`)

| Disk path | In sln | Sdk | TFM | Bytes | SHA-256 |
|---|---|---|---|---:|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | yes | `Microsoft.NET.Sdk` | net8.0 | 218 | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | yes | `Microsoft.NET.Sdk` | net8.0 | 433 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | yes | `Microsoft.NET.Sdk` | net8.0 | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | yes | `Microsoft.NET.Sdk` | net8.0 | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | yes | `Microsoft.NET.Sdk` | net8.0 | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | yes | `Microsoft.NET.Sdk.Web` | net8.0 | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | yes | `Microsoft.NET.Sdk.Worker` | net8.0 | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | yes | `Microsoft.NET.Sdk.Worker` | net8.0 | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | net8.0 | 1113 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | net8.0 | 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` |

**Product `.csproj` not in the sln: none.**

Repo MSBuild defaults: `D:\Prop\Directory.Build.props` exists (269 bytes, SHA-256 `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0`) — auto-imported; **not** a sln member. No `Directory.Packages.props`, no `global.json`, no `nuget.config` at repo root.

Note: `TraderIntelligence.Mt5.csproj` and `TraderIntelligence.Fix.CTrader.csproj` share SHA-256 `0AD91D39…` because both are the same 419-byte Domain+Application class-lib stub. That is a **content coincidence**, not a sln duplicate.

---

## 4. Non-product `.csproj` / `.sln` (correctly excluded)

### 4.1 Swarm scratch (not product)

| Path | In product sln? | Action |
|---|---|---|
| `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\C23EmptyEval.csproj` | **no** | Keep out. Throwaway C23 eval host. |

### 4.2 Vendor MetaQuotes samples (6 `.csproj`)

| Path | In product sln? |
|---|---|
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Gateway\TextFeeder.NET\TextFeeder.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Gateway\UniFeeder.NET\UniFeeder.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\BalanceExample.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\DealerExample.NET\DealerExample.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MetaQuotes.MT5WebAPI.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\WebTrader\WebTrader.csproj` | **no** |

Also excluded: **33** vendor `*.sln` files under `mt5-sdk\vendor\MetaTrader5SDK\Examples\` (including `All-Examples.sln`). **Never add** any path under `mt5-sdk\vendor\` to `Mt5TraderIntelligence.sln`.

### 4.3 First-party trees that are not MSBuild projects

| Path | Kind | In sln? | Add? |
|---|---|---|---|
| `D:\Prop\apps\web\` (`package.json` name `mt5-trader-intelligence`, Vite + React 18, **15** page files) | frontend | **no** | Optional A88 Phase B `SolutionItems` only. Do **not** invent a `.esproj`. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` (`project(mt5sdk LANGUAGES CXX)`, C++20) | CMake SDK | **no** | **Keep out.** Different toolchain (A54). |
| `D:\Prop\docs\` | markdown + architecture art | **no** | Optional `SolutionItems`. Not a `.csproj`. |
| `D:\Prop\services\` | empty directory | **no** | No `ml-service` (A52 hold). |
| `D:\Prop\Directory.Build.props` | repo MSBuild defaults | **no** | Optional `SolutionItems`. Already imported. |

---

## 5. Architecture §66 vs this sln (uncreated ≠ missing)

§66: *“Adapt to the existing repo; do not create duplicates unnecessarily.”*

| §66 path | On disk this pass | `.csproj`? | In sln? | Classification |
|---|---|---|---|---|
| `/apps/web` | **yes** (no `.csproj`) | no | no | visibility only |
| `/apps/api`, `/mt5-worker`, `/fix-worker` | yes | yes | yes | covered |
| `/src/Domain`, `/Application`, `/Infrastructure`, `/Mt5`, `/Fix.CTrader` | yes | yes | yes | covered |
| `/src/TradeReconstruction` | no (logic in `src\Domain\Reconstruction\`) | no | no | **do not** create sibling project |
| `/src/Scoring` | no (`src\Domain\Scoring\`) | no | no | **do not** create |
| `/src/Shadow` | no (`src\Domain\Shadow\`) | no | no | **do not** create |
| `/src/Risk` | no (`src\Domain\Risk\`) | no | no | **do not** create |
| `/src/Execution` | no (`src\Domain\Execution\`) | no | no | **do not** create |
| `/tests/Unit`, `/Integration` | yes | yes | yes | covered |
| `/tests/Replay` | **no** | no | no | future project only with fixtures (A27/A67) |
| `/tests/Fix` | **no** | no | no | future (A27/A68) |
| `/tests/Risk` | **no** | no | no | A27: **do not** create |
| `/services/ml-service` | **no** | no | no | A52 Phase-6 hold |
| `/docs/*.md` | 6 markdown + png/svg (not the full §66 set) | n/a | no `SolutionItems` | docs backlog, not membership |

---

## 6. `ProjectReference` graph (membership complete; edges are not sln work)

Read from the ten `.csproj` files this pass. Every `Include` resolves to a project **in** the sln. No dangling reference.

```
Domain                         (0 ProjectReference; 0 PackageReference)
  ↑
Application                    → Domain; FluentValidation 11.9.2
  ↑
  ├── Infrastructure           → Domain, Application, Mt5
  │     ↑                        EF Design/InMemory 8.0.4, Npgsql.EF 8.0.4, StackExchange.Redis 2.8.0
  │     ├── Api                → Domain, Application, Infrastructure
  │     ├── Mt5Worker          → Domain, Application, Infrastructure, Mt5
  │     ├── FixWorker          → Domain, Application, Infrastructure, Fix.CTrader
  │     └── Tests.Integration  → Domain, Application, Infrastructure, Fix.CTrader, Mt5
  ├── Mt5                      → Domain, Application
  └── Fix.CTrader              → Domain, Application
        ↑
        ├── FixWorker
        ├── Tests.Unit         → Domain, Application, Fix.CTrader
        └── Tests.Integration
```

B09 §7 said Tests.Integration does **not** reference Mt5. **Stale.** Current `TraderIntelligence.Tests.Integration.csproj` lines 26–30 include `..\..\src\Mt5\TraderIntelligence.Mt5.csproj`.

Known **reference holes** (do **not** “fix” by sln edits):

| Observer | Does not reference | Not a sln defect |
|---|---|---|
| `Api` | `Mt5` (direct), `Fix.CTrader` | API must not load Manager DLL / FIX sessions |
| `Tests.Unit` | `Infrastructure`, `Mt5` | add only when those units exist |
| `Tests.Integration` | `Api`, workers | add when host fixtures exist |

Adding a project to the `.sln` does not add a `ProjectReference`.

---

## 7. Do not confuse sln membership with product completeness

Product `*.cs` excluding `bin` / `obj` (this pass):

| Tree | `.cs` files |
|---|---:|
| `src\Domain` | 47 |
| `src\Application` | 3 |
| `src\Infrastructure` | 5 |
| `src\Mt5` | 4 |
| `src\Fix.CTrader` | 4 |
| `apps\api` | 1 |
| `apps\mt5-worker` | 2 |
| `apps\fix-worker` | 2 |
| `tests\Unit` | **9** |
| `tests\Integration` | **2** |
| **Total** | **79** |

B09 claimed `tests\Unit` / `tests\Integration` had **0** `.cs` files. **Stale.** Unit now has `BaselineScorerTests`, `ExecutionAndSizingTests`, `RiskEngineTests`, `SymbolNormalizerTests`, `TradeReconstructionTests`, `VolumeConverterTests`, `UnitTest1`, plus `Normalization\` and `Sizing\` tests. Integration has `SeedingAndStoreTests` and leftover `UnitTest1.cs`. That is test **content**, not a missing-project gap.

`apps\web\src\pages` now has **15** pages (B09 listed 13; `AuditPage.tsx` and `LiveCopyPage.tsx` exist). Still **no** `.csproj`.

`D:\Prop\docs\` now has `architecture.md`, `architecture.png`, `architecture.svg`, `ctrader-fix.md`, `risk.md`, `scoring.md`, `trade-reconstruction.md`, `xauusd-normalization.md`. Still no `SolutionItems`.

---

## 8. Direct answers

### Which existing product projects are missing from `Mt5TraderIntelligence.sln`?

**None. 10/10.**

### Which `.sln` projects are missing on disk?

**None.**

### What *is* outside the sln (and should stay that way unless a later plan says otherwise)?

| Thing | Class | Action |
|---|---|---|
| `apps/web` in Solution Explorer | visibility | optional A88 Phase B; no `.csproj` |
| `mt5-sdk` CMake | other toolchain | keep out |
| Vendor `*.csproj` / `*.sln` | samples | **never** add |
| `C23EmptyEval.csproj` | swarm scratch | keep out |
| `tests/Replay`, `tests/Fix` | uncreated | only with real fixtures |
| `tests/Risk` as its own project | uncreated | **do not** create (A27) |
| Domain sibling assemblies (`TradeReconstruction`, …) | uncreated | **do not** create |
| `services/ml-service` | hold | **do not** create (A52) |
| `docs/*`, `Directory.Build.props` | items | optional `SolutionItems` only |

### Did A11 / A88 / B09 get membership wrong?

- **Membership conclusion still holds:** do not re-add the ten. GUIDs and nesting are unchanged (same SHA-256 as B09).
- **A11 is stale** on `Directory.Build.props`, `apps/web`, `docs/`, Domain `Class1`, empty tests.
- **A88 Phase A is still a no-op.** Phase B visibility still not applied (no `SolutionItems`).
- **B09 membership is still correct.** B09 is stale on test `.cs` counts and the Integration→Mt5 `ProjectReference`.

---

## 9. What this pass must not do (and did not do)

1. Did **not** run `dotnet sln`.
2. Did **not** `dotnet sln add` the ten existing projects.
3. Did **not** edit `Mt5TraderIntelligence.sln` or any product `.cs` / `.csproj`.
4. Do **not** create empty §66 assemblies to match the diagram.
5. Do **not** import `mt5-sdk\vendor\**`.
6. Do **not** wrap CMake as a fake `.csproj`.
7. Do **not** add `services/ml-service` or `tests/Risk`.
8. If A88 Phase B visibility is later approved, edit **only** the `.sln` and keep the ten C# GUIDs unchanged.

---

## 10. Honest restatement

| Question | Answer |
|---|---|
| Are all existing product projects in the sln? | **Yes. 10/10.** |
| Nested under `src` / `apps` / `tests`? | **Yes.** |
| Broken sln paths? | **None.** |
| Existing-but-unlisted product `.csproj`? | **None.** |
| Extra first-party product `.sln`? | **None.** |
| `dotnet sln` this pass? | **Not run** (text parse + `Test-Path` + SHA-256). |
| Sln changed since B09? | **No.** Same 7019 bytes / same hash. |
| Missing from Solution Explorer? | `apps/web`, `docs/*`, `Directory.Build.props` (optional). |
| Missing from the *product* (not the sln)? | Uncreated §66 extras; C++ SDK is CMake; QuickFIX still not a package on Fix.CTrader; ML hold. |

**Sln coverage: 10/10 product `.csproj`. `dotnet sln add` list: empty. Final membership for this wave.**

---

## Evidence pins

- Solution: `D:\Prop\Mt5TraderIntelligence.sln` lines 6–31 (`Project`), 33–36 (`SolutionConfigurationPlatforms`), 40–81 (`ProjectConfigurationPlatforms`), 82–93 (`NestedProjects`). SHA-256 `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4`. `LastWriteTimeUtc` 2026-08-18T07:32:00.5167368Z.
- Prior membership: `A11_solution_coverage.md` (stale surroundings), `A88_sln_plan.md` (ten-project plan still valid), `B09_sln_gap.md` (same sln hash; stale test/graph notes).
- Architecture §66: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.
- Web: `D:\Prop\apps\web\package.json` + 15 files under `apps\web\src\pages`.
- CMake: `D:\Prop\mt5-sdk\CMakeLists.txt` line 2 `project(mt5sdk LANGUAGES CXX)`.
- Scratch `.csproj`: `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\C23EmptyEval.csproj`.
- Vendor `.csproj` count: **6**. Vendor `.sln` count: **33**.
- Product `.cs` count excluding `bin`/`obj`: **79**.
- Future test projects: `A27_test_inventory.md`, `A67_replay_harness.md`, `A68_fix_simulator.md`.
- ML hold: `A52_ml_not_yet.md`.
