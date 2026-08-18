# D60 — `Mt5TraderIntelligence.sln` project list (remeasured)

| Field | Value |
|---|---|
| Agent | D60 (senior engineer, solution project list) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:39:42+05:30 (2026-08-18T08:09:42Z) |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\Mt5TraderIntelligence.sln` |
| Lines / bytes | **94** / **7019** |
| SHA-256 | `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4` |
| LastWriteTimeUtc | **2026-08-18T07:32:00.5167368Z** |
| Format | Visual Studio Solution File, Format Version **12.00**; `# Visual Studio Version 17`; `VisualStudioVersion = 17.0.31903.59`; `MinimumVisualStudioVersion = 10.0.40219.1` |
| Product source modified | **No.** This report is the only write. The `.sln` and every product `.csproj` were read, not edited. |
| CLI | **`dotnet sln` was not run.** Membership is the `Project(...)` text + `Test-Path` + SHA-256. |
| Precedence | D-wave remasurement of the same sln bytes as C57 / B09. Supersedes C57 on **surrounding disk facts** (authored `.cs` counts, scratch `.csproj` set, `docs/` file list). Does **not** authorize `dotnet sln add` or empty §66 assemblies. |

---

## 0. Verdict

**13 `Project(` rows: 3 solution folders + 10 C# product projects. 10/10 product `.csproj` exist on disk. 0 dangling solution paths. 0 product `TraderIntelligence*.csproj` omitted.**

| Question | Count | Answer |
|---|---:|---|
| `Project(` rows in the `.sln` | **13** | 3 folders + 10 C# |
| C# projects (type `{FAE04EC0-…}`) | **10** | all resolve |
| Solution folders (type `{2150E333-…}`) | **3** | `src`, `apps`, `tests` |
| C# GUIDs in `NestedProjects` | **10/10** | no orphan, no double-nest, none at root |
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

Sln identity is **unchanged** since B09 / C57:

| Pin | B09 | C57 | D60 |
|---|---|---|---|
| Bytes | 7019 | 7019 | **7019** |
| SHA-256 | `AD503007…C5B5A7B4` | same | **same** |
| LastWriteTimeUtc | (same file) | 2026-08-18T07:32:00.5167368Z | **same** |

---

## 1. Method

| Step | What was used | What was not used |
|---|---|---|
| Read solution | Full file `D:\Prop\Mt5TraderIntelligence.sln` (13 `Project(` rows, lines 6–31) | `dotnet sln list` / `add` / `remove` |
| Existence | `Test-Path` on every sln-relative `.csproj` | MSBuild evaluation / `dotnet build` |
| Hash / size | `Get-FileHash -Algorithm SHA256`; `Get-Item.Length` | |
| Product census | Recursive `*.csproj` / `*.sln` under `D:\Prop`, excluding `bin` / `obj` / `node_modules` | |
| Project files | Read all 10 product `.csproj` + `Directory.Build.props` | No edits |
| Authored `.cs` | `Get-ChildItem -Recurse -Filter *.cs` excluding `bin`/`obj` | Compile |

`Get-Item` / `Get-FileHash` ran successfully (read metadata only). **`dotnet sln` was not invoked**, so this report does not quote a CLI exit code. The reconstructed path list matches A88 / B09 / C57 character-for-character.

---

## 2. All `Project(` entries (declaration order)

Type GUIDs:

| Type GUID | Meaning |
|---|---|
| `{2150E333-8FDC-42A3-9474-1A3956D46DE8}` | Visual Studio solution folder (not a buildable project) |
| `{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}` | Legacy C# project (SDK-style `.csproj` still uses this type in this sln) |

No `ProjectSection(SolutionItems)`. No `ExtensibilityGlobals`. No `web` / `docs` / `repo` folder. `HideSolutionNode = FALSE`.

Configurations: `Debug|Any CPU`, `Release|Any CPU`. Every C# GUID has `ActiveCfg` + `Build.0` for both. Solution folders correctly have **no** `ProjectConfigurationPlatforms` rows. All 13 project GUIDs are unique.

### 2.1 Solution folders (3)

| # | Sln name | Path column | GUID | Nested C# children |
|---|---|---|---|---|
| F1 | `src` | `src` | `{284C6660-B861-4055-A709-12AF82034A9B}` | Domain, Application, Infrastructure, Mt5, Fix.CTrader |
| F2 | `apps` | `apps` | `{91FA4D5C-2CC5-44A5-9805-42A3EEA00429}` | Api, Mt5Worker, FixWorker |
| F3 | `tests` | `tests` | `{355D29D7-21E1-4D1D-A68D-93FFB512E960}` | Tests.Unit, Tests.Integration |

### 2.2 C# projects (10) — declaration order, do not rewrite

| # | Sln name | Relative path | GUID | Folder | Disk | Sdk | TFM | `Build.0` Debug+Release |
|---|---|---|---|---|---|---|---|---|
| 1 | `TraderIntelligence.Mt5` | `src\Mt5\TraderIntelligence.Mt5.csproj` | `{CCD4D49A-9F3E-4795-AA56-CFBF87526E94}` | `src` | yes | `Microsoft.NET.Sdk` | net8.0 | yes |
| 2 | `TraderIntelligence.Mt5Worker` | `apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | `{31DFD31A-7E82-4968-912F-397C3E7DEE61}` | `apps` | yes | `Microsoft.NET.Sdk.Worker` | net8.0 | yes |
| 3 | `TraderIntelligence.Domain` | `src\Domain\TraderIntelligence.Domain.csproj` | `{A70A2194-62B9-4B2E-96DB-9725BEA5D9D7}` | `src` | yes | `Microsoft.NET.Sdk` | net8.0 | yes |
| 4 | `TraderIntelligence.Application` | `src\Application\TraderIntelligence.Application.csproj` | `{8A0BB7FD-D1CC-46B3-9C0C-6A2408866F36}` | `src` | yes | `Microsoft.NET.Sdk` | net8.0 | yes |
| 5 | `TraderIntelligence.Infrastructure` | `src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `{14EDD461-7C2D-43AC-BC2B-F2DCAC644491}` | `src` | yes | `Microsoft.NET.Sdk` | net8.0 | yes |
| 6 | `TraderIntelligence.Fix.CTrader` | `src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | `{76085664-A639-4C0D-8F92-264963416855}` | `src` | yes | `Microsoft.NET.Sdk` | net8.0 | yes |
| 7 | `TraderIntelligence.Api` | `apps\api\TraderIntelligence.Api.csproj` | `{D17266FA-2F65-4F00-9701-BC5DD52B8439}` | `apps` | yes | `Microsoft.NET.Sdk.Web` | net8.0 | yes |
| 8 | `TraderIntelligence.FixWorker` | `apps\fix-worker\TraderIntelligence.FixWorker.csproj` | `{63112B54-6D05-481D-B2F6-99AF3A795192}` | `apps` | yes | `Microsoft.NET.Sdk.Worker` | net8.0 | yes |
| 9 | `TraderIntelligence.Tests.Unit` | `tests\Unit\TraderIntelligence.Tests.Unit.csproj` | `{AA462E32-BFEF-4CB8-BDC6-B47F3609DFF9}` | `tests` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | net8.0 | yes |
| 10 | `TraderIntelligence.Tests.Integration` | `tests\Integration\TraderIntelligence.Tests.Integration.csproj` | `{40962285-3955-4D71-8C25-4596EDA39D98}` | `tests` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | net8.0 | yes |

`NestedProjects` (child → parent), sln lines 83–92:

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

## 3. Per-project identity (disk)

| Disk path | In sln | Sdk | Bytes | SHA-256 |
|---|---|---|---:|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | yes | `Microsoft.NET.Sdk` | 218 | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | yes | `Microsoft.NET.Sdk` | 433 | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | yes | `Microsoft.NET.Sdk` | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | yes | `Microsoft.NET.Sdk` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` | yes | `Microsoft.NET.Sdk` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | yes | `Microsoft.NET.Sdk.Web` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | yes | `Microsoft.NET.Sdk.Worker` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | yes | `Microsoft.NET.Sdk.Worker` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | 1113 | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` | yes | `Microsoft.NET.Sdk` (`IsTestProject`) | 1328 | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` |

**Product `.csproj` not in the sln: none.**

Repo MSBuild defaults: `D:\Prop\Directory.Build.props` exists (269 bytes, SHA-256 `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0`) — auto-imported; **not** a sln member. No `Directory.Packages.props`, no `global.json`, no `nuget.config` at repo root.

Note: `TraderIntelligence.Mt5.csproj` and `TraderIntelligence.Fix.CTrader.csproj` share SHA-256 `0AD91D39…` because both are the same 419-byte Domain+Application class-lib stub. That is a **content coincidence**, not a sln duplicate.

Worker `UserSecretsId` (not sln members; recorded so they are not mistaken for extra projects):

| Project | UserSecretsId |
|---|---|
| `TraderIntelligence.Mt5Worker` | `dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1` |
| `TraderIntelligence.FixWorker` | `dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79` |

---

## 4. `ProjectReference` graph (membership complete; edges are not sln work)

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

Known **reference holes** (do **not** “fix” by sln edits):

| Observer | Does not reference | Not a sln defect |
|---|---|---|
| `Api` | `Mt5` (direct), `Fix.CTrader` | API must not load Manager DLL / FIX sessions |
| `Tests.Unit` | `Infrastructure`, `Mt5` | add only when those units exist |
| `Tests.Integration` | `Api`, workers | add when host fixtures exist |

Adding a project to the `.sln` does not add a `ProjectReference`.

---

## 5. Non-product `.csproj` / `.sln` (correctly excluded)

### 5.1 Swarm scratch (7 `.csproj`, not product)

| Path | In product sln? | Action |
|---|---|---|
| `D:\Prop\reports\swarm\20260818\_tmp_c23_empty\C23EmptyEval.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_c31_recon\C31ReconAdv.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_c32_score\C32ScoreEval.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_d11_recon\D11ReconBugs.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_d27_parser\D27ParserEval.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_d37_eval\D37SeedEval.csproj` | **no** | Keep out |
| `D:\Prop\reports\swarm\20260818\_tmp_d57_mfe\D57MfeEval.csproj` | **no** | Keep out |

C57 listed only `C23EmptyEval`. **Stale scratch census.** None of these belong in `Mt5TraderIntelligence.sln`.

### 5.2 Vendor MetaQuotes samples (6 `.csproj`, 33 `.sln`)

| Path | In product sln? |
|---|---|
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Gateway\TextFeeder.NET\TextFeeder.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Gateway\UniFeeder.NET\UniFeeder.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\BalanceExample.NET\BalanceExample.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Manager\DealerExample.NET\DealerExample.NET.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MetaQuotes.MT5WebAPI.csproj` | **no** |
| `mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\WebTrader\WebTrader.csproj` | **no** |

Also excluded: **33** vendor `*.sln` files under `mt5-sdk\vendor\MetaTrader5SDK\Examples\` (including `All-Examples.sln`). First-party product sln count remains **1**. **Never add** any path under `mt5-sdk\vendor\` to `Mt5TraderIntelligence.sln`.

### 5.3 First-party trees that are not MSBuild projects

| Path | Kind | In sln? | Add? |
|---|---|---|---|
| `D:\Prop\apps\web\` (`package.json` name `mt5-trader-intelligence`, Vite + React 18, **15** page files) | frontend | **no** | Optional A88 Phase B `SolutionItems` only. Do **not** invent a `.esproj`. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` (`project(mt5sdk LANGUAGES CXX)`, C++20) | CMake SDK | **no** | **Keep out.** Different toolchain (A54). |
| `D:\Prop\docs\` | 7 markdown + png/svg | **no** | Optional `SolutionItems`. Not a `.csproj`. |
| `D:\Prop\services\` | empty directory | **no** | No `ml-service` (A52 hold). |
| `D:\Prop\Directory.Build.props` | repo MSBuild defaults | **no** | Optional `SolutionItems`. Already imported. |

`apps/web/src/pages` (15): `AuditPage`, `BrokersPage`, `FixSessionsPage`, `GroupsPage`, `LiveCopyPage`, `OverviewPage`, `ReconciliationPage`, `RiskPage`, `ScoringPage`, `SettingsPage`, `ShadowPortfolioPage`, `SystemHealthPage`, `TradeExplorerPage`, `TraderDetailPage`, `TradersPage`.

`docs/` this pass: `architecture.md`, `architecture.png`, `architecture.svg`, `ctrader-fix.md`, `deployment.md`, `risk.md`, `scoring.md`, `trade-reconstruction.md`, `xauusd-normalization.md`. C57 omitted `deployment.md`.

---

## 6. Architecture §66 vs this sln (uncreated ≠ missing)

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
| `/docs/*.md` | 7 markdown + png/svg | n/a | no `SolutionItems` | docs backlog, not membership |

---

## 7. Do not confuse sln membership with product completeness

Product `*.cs` excluding `bin` / `obj` (this pass):

| Tree | `.cs` files | Files |
|---|---:|---|
| `src\Domain` | **49** | Brokers 1 + Entities 21 + Enums 15 + Execution 4 + Instruments 1 + Reconstruction 3 + Risk 1 + Scoring 1 + Shadow 1 + Volume 1 |
| `src\Application` | **3** | `Mt5Contracts`, `DashboardModels`, `DealIngestionService` |
| `src\Infrastructure` | **5** | `EfDashboardQueries`, `DependencyInjection`, `EfTradingStore`, `TraderDbContext`, `DemoSeeder` |
| `src\Mt5` | **4** | `Mt5BrokerOptions`, `FakeMt5BrokerConnector`, `IBrokerConnector`, `DeterministicGuid` |
| `src\Fix.CTrader` | **5** | `CTraderFixOptions`, `FixMessageParser`, `CTraderQuoteService`, `FixSessionOwnership`, `FixSimulationHarness` |
| `apps\api` | **2** | `Program.cs`, `Controllers\SettingsController.cs` |
| `apps\mt5-worker` | **2** | `Program.cs`, `Worker.cs` |
| `apps\fix-worker` | **2** | `Program.cs`, `Worker.cs` |
| `tests\Unit` | **10** | `BaselineScorerTests`, `DealReasonTests`, `ExecutionAndSizingTests`, `RiskEngineTests`, `SymbolNormalizerTests`, `TradeReconstructionTests`, `UnitTest1`, `VolumeConverterTests`, `Normalization\SourceDestinationQuantityConversionTests`, `Sizing\QuantityNormalizerStepMinMaxTests` |
| `tests\Integration` | **2** | `SeedingAndStoreTests`, leftover `UnitTest1.cs` |
| **Total** | **84** | |

C57 claimed **79** product `.cs` files (Domain 47 / Fix.CTrader 4 / api 1 / Unit 9). **Stale.** Delta this pass: Domain **+2**, Fix.CTrader **+1** (`CTraderQuoteService.cs`), api **+1** (`SettingsController.cs`), Unit **+1** (`DealReasonTests.cs`) = **+5 → 84**. That is test/code **content**, not a missing-project gap.

D01’s Domain “47 authored `.cs`” is likewise stale versus this 49-file listing.

---

## 8. Direct answers

### Which projects are in `Mt5TraderIntelligence.sln`?

**13 entries:** folders `src`, `apps`, `tests`; C# `TraderIntelligence.Mt5`, `.Mt5Worker`, `.Domain`, `.Application`, `.Infrastructure`, `.Fix.CTrader`, `.Api`, `.FixWorker`, `.Tests.Unit`, `.Tests.Integration`.

### Which existing product projects are missing from the sln?

**None. 10/10.**

### Which `.sln` projects are missing on disk?

**None.**

### What *is* outside the sln (and should stay that way unless a later plan says otherwise)?

| Thing | Class | Action |
|---|---|---|
| `apps/web` in Solution Explorer | visibility | optional A88 Phase B; no `.csproj` |
| `mt5-sdk` CMake | other toolchain | keep out |
| Vendor `*.csproj` / `*.sln` | samples | **never** add |
| Seven `_tmp_*` eval `.csproj` | swarm scratch | keep out |
| `tests/Replay`, `tests/Fix` | uncreated | only with real fixtures |
| `tests/Risk` as its own project | uncreated | **do not** create (A27) |
| Domain sibling assemblies (`TradeReconstruction`, …) | uncreated | **do not** create |
| `services/ml-service` | hold | **do not** create (A52) |
| `docs/*`, `Directory.Build.props` | items | optional `SolutionItems` only |

### Did A11 / A88 / B09 / C57 get membership wrong?

- **Membership conclusion still holds:** do not re-add the ten. GUIDs and nesting are unchanged (same SHA-256 as B09 / C57).
- **A11 is stale** on `Directory.Build.props`, `apps/web`, `docs/`, Domain `Class1`, empty tests.
- **A88 Phase A is still a no-op.** Phase B visibility still not applied (no `SolutionItems`).
- **B09 membership is still correct.** B09 is stale on test `.cs` counts and the Integration→Mt5 `ProjectReference`.
- **C57 membership is still correct.** C57 is stale on authored `.cs` totals (79 → 84), scratch `.csproj` count (1 → 7), and `docs/deployment.md`.

---

## 9. What this pass must not do (and did not do)

1. Did **not** run `dotnet sln`.
2. Did **not** `dotnet sln add` the ten existing projects.
3. Did **not** edit `Mt5TraderIntelligence.sln` or any product `.cs` / `.csproj`.
4. Do **not** create empty §66 assemblies to match the diagram.
5. Do **not** import `mt5-sdk\vendor\**`.
6. Do **not** wrap CMake as a fake `.csproj`.
7. Do **not** add `services/ml-service` or `tests/Risk`.
8. Do **not** add the seven swarm `_tmp_*` eval projects.
9. If A88 Phase B visibility is later approved, edit **only** the `.sln` and keep the ten C# GUIDs unchanged.

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
| Sln changed since C57 / B09? | **No.** Same 7019 bytes / same hash / same LastWriteTimeUtc. |
| Missing from Solution Explorer? | `apps/web`, `docs/*`, `Directory.Build.props` (optional). |
| Missing from the *product* (not the sln)? | Uncreated §66 extras; C++ SDK is CMake; QuickFIX still not a package on Fix.CTrader; ML hold. |

**Sln coverage: 10/10 product `.csproj`. `dotnet sln add` list: empty. D-wave project list for 2026-08-18.**

---

## Evidence pins

- Solution: `D:\Prop\Mt5TraderIntelligence.sln` lines 6–31 (`Project`), 33–36 (`SolutionConfigurationPlatforms`), 40–81 (`ProjectConfigurationPlatforms`), 82–93 (`NestedProjects`). SHA-256 `AD5030070166D81EE478B632BCE3F381F2C9D3FFBBC6EB0FDD407047C5B5A7B4`. `LastWriteTimeUtc` 2026-08-18T07:32:00.5167368Z.
- Prior membership: `A11_solution_coverage.md` (stale surroundings), `A88_sln_plan.md` (ten-project plan still valid), `B09_sln_gap.md` (same sln hash; stale test/graph notes), `C57_sln_final.md` (same sln hash; stale `.cs` / scratch / docs counts).
- Architecture §66: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`.
- Web: `D:\Prop\apps\web\package.json` + 15 files under `apps\web\src\pages`.
- CMake: `D:\Prop\mt5-sdk\CMakeLists.txt` line 2 `project(mt5sdk LANGUAGES CXX)`.
- Scratch `.csproj`: seven under `D:\Prop\reports\swarm\20260818\_tmp_*\`.
- Vendor `.csproj` count: **6**. Vendor `.sln` count: **33**.
- Product `.cs` count excluding `bin`/`obj`: **84**.
- Future test projects: `A27_test_inventory.md`, `A67_replay_harness.md`, `A68_fix_simulator.md`.
- ML hold: `A52_ml_not_yet.md`.
