# D52 — csproj QuickFIX? **No official QuickFIX/n on any product project**

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D52_qfn.md` |
| Agent | D52 (csproj QuickFIX/n package-reference remeasure) |
| Date | 2026-08-18T13:38:58+05:30 |
| Assigned | `csproj QuickFIX?` Write this file. Do not modify product source. |
| Product source modified | **No** |
| Test source modified | **No** |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`main`) |
| CPM | `Directory.Packages.props` **does not exist** |
| Binding pin | `A35_quickfixn_packages.md` — official `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** |
| Siblings | C19 (same package question, earlier worktree), D05 (Fix.CTrader census), A35 (pin only), B05 (stale `QuickFix.Net` 1.8.0 on HEAD) |
| Method | Full read of every product `*.csproj` + `Directory.Build.props`; `Select-String` over `D:\Prop\{src,apps,tests}` `*.csproj`/`*.props`/`*.cs`/`*.json`/`*.cfg`/`*.xml`; restore graphs (`project.assets.json`, `*.nuget.dgspec.json`, `project.nuget.cache`, `deps.json`, `FileListAbsolute.txt`); `Get-ChildItem` for `QuickFix*.dll`; `git show` / `git diff` / `git grep` / `git log -S QuickFix`; SHA-256 via `Get-FileHash` (worktree) and `git cat-file` (HEAD blob). |

**Honesty rule:** official **QuickFIX/n** is the `QuickFIXn.*` family from QuickFIXEngine.org (`A35`). Unofficial `QuickFix.Net` is a **different package id**. A deleted-but-uncommitted `PackageReference` is not a restore. A leftover nupkg in `%USERPROFILE%\.nuget\packages` is not a project reference. A `ProjectReference` to `Fix.CTrader` is not an engine.

---

## 0. Verdict

**No. No product `.csproj` on the worktree references official QuickFIX/n. The only historical QuickFix string in git is unofficial `QuickFix.Net` 1.8.0 on HEAD `Fix.CTrader.csproj`; the worktree has deleted that line (unstaged).**

| Question | Measured answer |
|---|---|
| Does any worktree product `*.csproj` contain `QuickFIXn.Core` / `QuickFIXn.FIX44` / any `QuickFIXn.*`? | **No.** Zero hits. |
| Does any worktree product `*.csproj` contain `QuickFix.Net` / `QuickFix` / `QuickFIX`? | **No.** Zero hits. |
| Is the A35 pair pinned in `Directory.Build.props` / `Directory.Packages.props`? | **No.** Props has no package versions. `Directory.Packages.props` **does not exist**. |
| Does HEAD still list a QuickFix package? | **Yes — unofficial only:** `<PackageReference Include="QuickFix.Net" Version="1.8.0" />` in `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj`. |
| Is that unofficial line still on disk? | **No.** Unstaged deletion (`git diff` is exactly that one line). |
| Does current restore pull `QuickFix.dll` / `QuickFIXn*.dll`? | **No.** `Fix.CTrader` expected packages = **FluentValidation 11.9.2 only** (transitive via Application). `deps.json` libraries = this project + FluentValidation + Application + Domain. |
| Does `%USERPROFILE%\.nuget\packages` still contain a QuickFix nupkg? | **Yes, leftover:** `quickfix.net\1.8.0` only. **No** `quickfixn.core` / `quickfixn.fix44` folder. |
| Does any product C# `using QuickFix` / construct `SocketInitiator`? | **No.** Zero matches under `src/`, `apps/`, `tests/`. |

Classification:

| Slice | Class |
|---|---|
| Official QuickFIX/n 1.14.1 (`QuickFIXn.Core` + `QuickFIXn.FIX44`) | **MISSING** — never a `PackageReference` in this repo |
| Deprecated `QuickFIXn.FIX4.4` / FIX5 / FIXT11 | **absent** (correct; A35 forbids them) |
| Unofficial `QuickFix.Net` 1.8.0 on **HEAD** | **DEAD / WRONG FAMILY** — unused `PackageReference` |
| Unofficial `QuickFix.Net` 1.8.0 on **worktree** | **REMOVED** (unstaged; not committed) |
| Live initiator / SSL / `SessionSettings` / RoE dictionary | **MISSING** |
| This agent adding the A35 pair | **NOT DONE** (read-only; product source not modified) |

One-line answer: **csproj QuickFIX? Worktree = no. HEAD = unofficial `QuickFix.Net` 1.8.0 only, never official QuickFIX/n.**

---

## 1. What “QuickFIX” is allowed to mean

`A35` pin (quoted, **not implemented**):

```xml
<PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
<PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
```

| Package id | Allowed? | Worktree csproj? | HEAD csproj? |
|---|---|---|---|
| `QuickFIXn.Core` 1.14.1 | **Required** for a live adapter | **No** | **No** |
| `QuickFIXn.FIX44` 1.14.1 | **Required** (same version) | **No** | **No** |
| `QuickFIXn.FIX4.4` (deprecated name) | **Do not add** | **No** | **No** |
| `QuickFIXn.FIX50*` / `QuickFIXn.FIXT11` | **Do not add** | **No** | **No** |
| `QuickFix.Net` / `QuickFix.Net.NetCore` / bare `QuickFIXn` | **Do not add** (A35 unofficial-fork ban) | **No** | **Yes — 1.8.0** |

Architecture §5: prefer QuickFIX/n; do not write a raw `TcpClient` engine. Product C# also has **zero** `TcpClient` / `SocketInitiator` / `IInitiator` / `SessionSettings` hits.

---

## 2. Scan universe (product only)

Product `.csproj` files (10; all in `Mt5TraderIntelligence.sln`):

| Path |
|---|
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` |
| `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj` |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` |
| `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` |
| `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` |
| `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj` |
| `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` |

Shared MSBuild: `D:\Prop\Directory.Build.props` (no `PackageReference`, no versions).

**Absent (confirmed `Test-Path` = false):** `Directory.Packages.props`, `nuget.config`, `global.json`, `packages.config`, any `packages.lock.json`.

Excluded from “product”: `reports/swarm/20260818/_tmp_*/*.csproj` (throwaway eval trees), `apps/web` (no `.csproj`), `mt5-sdk/vendor`.

`Select-String` over all product `*.csproj` + `Directory.Build.props` for `QuickFix|QuickFIX|QuickFIXn|quickfix` → **empty**.

`docker-compose.yml` and `Mt5TraderIntelligence.sln` also have **zero** QuickFIX tokens.

---

## 3. Every product `PackageReference` (worktree)

| Project | Direct packages | Official QF/n? |
|---|---|---|
| `src/Domain` | **none** | No |
| `src/Application` | `FluentValidation` 11.9.2 | No |
| **`src/Fix.CTrader`** | **none** (Domain + Application project refs only) | **No** |
| `src/Mt5` | none (Domain + Application) | No |
| `src/Infrastructure` | EF Design 8.0.4, EF InMemory 8.0.4, Npgsql.EF 8.0.4, StackExchange.Redis 2.8.0 | No |
| `apps/api` | SignalR.Common 8.0.4, Serilog.AspNetCore 8.0.2, Swashbuckle 6.6.2 | No |
| `apps/fix-worker` | `Microsoft.Extensions.Hosting` 8.0.1 | **No** (project-refs Fix.CTrader) |
| `apps/mt5-worker` | `Microsoft.Extensions.Hosting` 8.0.1 | No |
| `tests/Unit` | coverlet 6.0.0, FluentAssertions 6.12.0, Test.Sdk 17.8.0, Moq 4.20.70, xunit 2.5.3, xunit.runner.visualstudio 2.5.3 | **No** (project-refs Fix.CTrader) |
| `tests/Integration` | coverlet 6.0.0, FluentAssertions 6.12.0, EF InMemory 8.0.4, Test.Sdk 17.8.0, xunit 2.5.3, xunit.runner.visualstudio 2.5.3 | **No** (project-refs Fix.CTrader) |

Worktree `Fix.CTrader.csproj` in full (419 bytes, BOM+CRLF):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

`Directory.Build.props` (269 bytes) sets only `LangVersion`, `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors=false`, `Deterministic=true`. No package versions.

---

## 4. HEAD vs worktree (the only QuickFix string in product git)

`git log --oneline -S QuickFix -- '*.csproj' '*.props' '*.cs'` → single event: initial commit `6c41447` added unofficial `QuickFix.Net` 1.8.0. No later commit added `QuickFIXn.*`.

`git grep -n -i 'QuickFix\|QuickFIXn\|QuickFIX' HEAD -- '*.csproj' '*.props' '*.cs' '*.json' '*.sln'` → **one line**:

```
HEAD:src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj:6:    <PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

`git grep` on the worktree (same globs, excluding reports) → **empty**.

**HEAD** blob `1b394e9d4fc9b469ac0a4757b2d75fb7922d2e7b` (`git cat-file` 469 bytes, BOM+LF):

```xml
<PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

**Worktree** blob `529a3a1c11def916ce8038388af8a7a2913505a9`. `git diff -- src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` is exactly:

```diff
-    <PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

`git status --short` includes ` M src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` (unstaged). This agent did not make or revert that edit.

| Surface | Official QuickFIX/n | Unofficial `QuickFix.Net` 1.8.0 | Used by C#? |
|---|---|---|---|
| HEAD csproj | absent | **present** | **No** (`git grep QuickFix HEAD -- '*.cs'` empty) |
| Worktree csproj | absent | **absent** | n/a |
| Restore `project.assets.json` (worktree) | absent | **absent** | — |
| `Fix.CTrader` `deps.json` | absent | **absent** | — |
| User NuGet cache | **no `quickfixn.*` folder** | leftover `quickfix.net\1.8.0` | not referenced |

---

## 5. Restore / runtime graph (measured)

`D:\Prop\src\Fix.CTrader\obj\project.nuget.cache`:

```json
"expectedPackageFiles": [
  "C:\\Users\\ADMIN\\.nuget\\packages\\fluentvalidation\\11.9.2\\fluentvalidation.11.9.2.nupkg.sha512"
]
```

`project.assets.json` `projectFileDependencyGroups.net8.0`:

```text
TraderIntelligence.Application >= 1.0.0
TraderIntelligence.Domain >= 1.0.0
```

Libraries in that assets file (3): `FluentValidation/11.9.2`, `TraderIntelligence.Application/1.0.0`, `TraderIntelligence.Domain/1.0.0`. **No QuickFix / QuickFIXn key.**

`TraderIntelligence.Fix.CTrader.csproj.nuget.dgspec.json` frameworks block for Fix.CTrader has **no** `dependencies` package map — only project references to Domain + Application.

`bin/Debug/net8.0/TraderIntelligence.Fix.CTrader.deps.json` libraries:

```text
TraderIntelligence.Fix.CTrader/1.0.0
FluentValidation/11.9.2
TraderIntelligence.Application/1.0.0
TraderIntelligence.Domain/1.0.0
```

`Get-ChildItem` for `QuickFix*.dll` / `QuickFIXn*.dll` under `src/`, `apps/`, `tests/` → **empty**.

`apps/fix-worker` `deps.json` names that match `Fix`: `TraderIntelligence.FixWorker/1.0.0`, `TraderIntelligence.Fix.CTrader/1.0.0` only. No engine DLL.

`Select-String` of `project.assets.json` under api / fix-worker / mt5-worker / Unit / Integration / Infrastructure for `QuickFix|QuickFIXn|QuickFIX` → **empty**.

---

## 6. Leftover unofficial nupkg (cache only — not a csproj reference)

Folder present: `C:\Users\ADMIN\.nuget\packages\quickfix.net\1.8.0\`.

No sibling folders: `quickfixn.core`, `quickfixn.fix44`, or any other `quickfixn.*`.

| Cache item | Measured |
|---|---|
| Nuspec id | `QuickFix.Net` |
| Version | `1.8.0` |
| Authors | **Quant Edge JSC** (not official `grantb` / `snorris`) |
| Layout | `lib\QuickFix.dll` (20,436,480 bytes), `spec\FIX4x.xml` … `FIXT11.xml`, sample cfg |
| nupkg SHA-256 | `6D58A7E2EBFA90A47C76A8DC515E9959CE4C9A3FB1F14F189798DBCAECD8D12E` (5,141,637 bytes) |

This is the A35-banned unofficial family. It is **not** `QuickFIXn.Core` 1.14.1. Current worktree restore does **not** list it in `expectedPackageFiles`. Treat the cache folder as a leftover from an earlier restore of HEAD, not as wiring.

---

## 7. C# still has no engine types

Grep of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` (exclude `bin/`/`obj/`) for:

`QuickFix`, `QuickFIX`, `QuickFIXn`, `using QuickFix`, `SocketInitiator`, `ThreadedSocketInitiator`, `IInitiator`, `IApplication`, `SessionSettings`, `FileStorePath`, `FileLogPath`, `FileStoreFactory`, `FileLogFactory`

→ **no matches** in `*.cs` / `*.csproj` / `*.props` / `*.json` / `*.config` / `*.xml` / `*.cfg`.

No product files named `FIX44*.xml` or `*.cfg` under `src/`, `apps/`, `tests/`.

`src/Fix.CTrader` product sources (exclude `bin/`/`obj/`):

| File | SHA-256 | QuickFIX/n? |
|---|---|---|
| `TraderIntelligence.Fix.CTrader.csproj` | `0AD91D39…` | **No** packages |
| `Configuration/CTraderFixOptions.cs` | `A354BBEA…` | Options bag only |
| `Parsing/FixMessageParser.cs` | `C58681E7…` | Hand-rolled `\|`/SOH parse |
| `Services/FixSessionOwnership.cs` | `30029E29…` | In-memory fence |
| `Services/CTraderQuoteService.cs` (untracked) | `7D2FDE1D…` | Dictionary-tag helper; **no** QF types |
| `Testing/FixSimulationHarness.cs` | `99A28D8F…` | Pipe-delimited string factory |

`CTraderQuoteService` is new since C19/D05. It still does **not** reference QuickFIX/n. It consumes `IReadOnlyDictionary<int,string>` (harness tags, including non-RoE 1320/1321). That is not an initiator.

---

## 8. Hashes (this snapshot)

| Path | SHA-256 (on-disk) | git blob |
|---|---|---|
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` **worktree** | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | `529a3a1c11def916ce8038388af8a7a2913505a9` |
| same file **HEAD** (`git cat-file -p`, 469 bytes BOM+LF) | `5E3600E559331EA9BFA2A5AA5244145BBDB63FA44F6402EFEFB6673640AFACFE` | `1b394e9d4fc9b469ac0a4757b2d75fb7922d2e7b` |
| `src/Domain/TraderIntelligence.Domain.csproj` | `E151F959964EB450A5B86B72765E3F9C505645FA9516EAE485743D2B43911C8E` | — |
| `src/Application/TraderIntelligence.Application.csproj` | `44E3448AE56A9D79BF562F6D68B6CC52915E6B334C3F49D7AE9E9C2313AA9DE2` | — |
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | — |
| `src/Mt5/TraderIntelligence.Mt5.csproj` | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | — |
| `apps/api/TraderIntelligence.Api.csproj` | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | — |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | — |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | — |
| `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | `EB7A4ECA27D4953313F58129C6494BE556AE616FDB9260DCA1112D4C2FEC7F50` | — |
| `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | `E749992347A22BB8241B76DA8A9008CFCA2C74F567C070A64D7B7B79B4F6E4F4` | — |
| `Directory.Build.props` | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` | — |

Note: worktree `Fix.CTrader.csproj` and `Mt5.csproj` share SHA-256 `0AD91D39…` because both are BOM+CRLF net8 class libraries with the same two sibling `ProjectReference` includes and **no** packages. That is not evidence of QuickFIX.

Fix.CTrader.csproj LastWriteTimeUtc: `2026-08-18T07:49:07.1579602Z`.

---

## 9. Stale-vs-this-file

| Report | Claim | D52 measure |
|---|---|---|
| A05 | Empty `Class1`, zero packages | **Stale** for source inventory. **Still true** that official QF/n is absent. |
| A35 | Pin 1.14.1 pair; checklist unchecked | **Still unchecked.** This file does not add packages. |
| A49 / A50 / A57 | `QuickFix.Net` 1.11.2 | **Stale version.** HEAD was 1.8.0; worktree has **0**. |
| B05 / A68 / A100 / A102 | `QuickFix.Net` 1.8.0 on disk | **True of HEAD.** **False of worktree.** Still **not** QuickFIX/n. |
| C19 | Official QF/n not referenced; worktree deleted 1.8.0 | **Still true.** D52 remeasures the same csproj SHA `0AD91D39…`. New since C19: untracked `CTraderQuoteService.cs` (still no QF types). |
| D05 | Worktree has no `PackageReference` | **Still true.** |

Use **this file** for the csproj package question. Use C19 for the broader “simulator only / worker unwired” story. Use D32 for current `Worker.cs` status stamps. Use A35 when a later change-controlled agent is allowed to **add** the official pair.

---

## 10. What this does **not** authorize

1. Do **not** add `QuickFix.Net` back. If an engine is added later, it must be the A35 pair (`QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1**), versions kept identical, plus a cTrader RoE dictionary — not generic stock FIX44 alone.
2. Do **not** write a `TcpClient` FIX engine (architecture §5; A35).
3. Do **not** treat a leftover `~\.nuget\packages\quickfix.net\1.8.0` folder as a project reference.
4. Do **not** treat `CTraderQuoteService` or `FixSimulationHarness` as QuickFIX/n.
5. Do **not** enable live `NewOrderSingle`. Absence of the engine package is **SAFE_BY_ABSENCE**, not a go-live pass.
6. This agent did **not** modify product source. Committing the worktree csproj deletion is a separate change-control decision.

---

## 11. One-line answer to the assigned question

**csproj QuickFIX? No — official QuickFIX/n is not a `PackageReference` on any product project (never has been). HEAD still lists unused unofficial `QuickFix.Net` 1.8.0 on `Fix.CTrader.csproj`; the worktree removed that line. Restore and `deps.json` pull FluentValidation only.**
