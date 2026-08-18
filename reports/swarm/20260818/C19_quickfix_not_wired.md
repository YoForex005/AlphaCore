# C19 — QuickFIX/n package not referenced yet; simulator only

| Field | Value |
|---|---|
| Agent | C19 (package-wiring verify only) |
| Date | 2026-08-18T13:25:48+05:30 |
| Assigned | Confirm QuickFIX/n package not referenced yet; simulator only. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\C19_quickfix_not_wired.md` |
| Product source modified | **No.** This report is the only write. |
| Method | Full read of every product `*.csproj` + `Directory.Build.props`; restore graphs (`project.assets.json`, `*.nuget.dgspec.json`, `*.nuget.cache`, `deps.json`, `FileListAbsolute.txt`); grep of `src/`, `apps/`, `tests/` for `QuickFix` / `QuickFIXn` / initiator types; full read of `Fix.CTrader` sources + `apps/fix-worker`; `git show HEAD` vs worktree; SHA-256 of measured files. |
| Binding pin | `A35_quickfixn_packages.md` — official `QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1** |
| Siblings | A05 (stale empty-stub), B05 (wrong `QuickFix.Net` 1.8.0 pin), A68 (simulator design), A101 / C07 (worker has no send path) |
| HEAD at measure | `398a14200ec65714c4077eed55c46808382ca1e3` |
| `Directory.Packages.props` | **Does not exist** (no Central Package Management) |

**Honesty rule:** a `PackageReference` that no C# type consumes is still a reference. A `LoggedOn` row is not a session. A pipe-delimited string factory is not QuickFIX/n. Official **QuickFIX/n** (`QuickFIXn.*` from QuickFIXEngine.org) is a different family from unofficial `QuickFix.Net`.

---

## 0. Verdict

**CONFIRMED (worktree). Official QuickFIX/n is not referenced anywhere. The only FIX path on disk is the in-process pipe-delimited simulator — and even that is unwired.**

| Question | Measured answer |
|---|---|
| Is official **QuickFIX/n** (`QuickFIXn.Core` / `QuickFIXn.FIX44` / any `QuickFIXn.*`) a `PackageReference` on any product project? | **No.** Never in git history of `*.csproj` / `*.props` / `*.cs` / `*.json`. |
| Is it pinned in `Directory.Build.props` / `Directory.Packages.props`? | **No.** `Directory.Build.props` has no package versions. `Directory.Packages.props` **does not exist**. |
| Does any product C# `using QuickFix` / implement `IApplication` / construct `SocketInitiator`? | **No.** Zero hits under `src/`, `apps/`, `tests/`. |
| Does restore / `deps.json` / `bin/` pull `QuickFix.dll` or `QuickFIXn*.dll`? | **No.** Restore expected packages for `Fix.CTrader` = **FluentValidation 11.9.2 only** (transitive via Application). `Get-ChildItem` for `QuickFix*.dll` / `QuickFIXn*.dll` under `src/`, `apps/`, `tests/` = **empty**. |
| What is the FIX “engine” today? | **Simulator only:** `FixSimulationHarness` + `FixMessageParser` (pipe/`|` strings, checksum). No TCP, no SSL, no SessionSettings, no dictionary XML. |
| Does `apps/fix-worker` call the simulator or any FIX engine? | **No.** Worker stamps `FixSessionStates` in EF and logs. It never constructs `FixSimulationHarness`. |
| Do unit/integration tests call the simulator? | **No.** `FixSimulationHarness` / `SimulateLogon*` / `SimulateExecutionReport*` appear **only** in the harness file itself. There is no `tests/Fix` project. |

**HEAD caveat (do not paper over):** committed `TraderIntelligence.Fix.CTrader.csproj` still contains unofficial `<PackageReference Include="QuickFix.Net" Version="1.8.0" />`. That is **not** QuickFIX/n (`A35` forbids it). Worktree has **deleted** that line (unstaged). No product `.cs` file has ever imported the package (`git grep QuickFix HEAD -- '*.cs'` = empty). Restore on the current worktree no longer lists it.

Classification:

| Slice | Class |
|---|---|
| Official QuickFIX/n 1.14.1 pair | **MISSING** — not referenced yet |
| Unofficial `QuickFix.Net` 1.8.0 on **HEAD** | **DEAD / WRONG FAMILY** — unused `PackageReference` |
| Unofficial `QuickFix.Net` 1.8.0 on **worktree** | **REMOVED** (unstaged; not committed) |
| Live initiator / QUOTE:5211 / TRADE:5212 / SSL | **MISSING** |
| cTrader RoE data dictionary (`FIX44-CSERVER.xml`) | **MISSING** (no `*.xml` / `*.cfg` under product trees) |
| In-process simulator (`FixSimulationHarness`) | **EXISTS** — string factory only |
| Simulator wired into worker / tests / Application ports | **NOT WIRED** |
| Live `NewOrderSingle` (`35=D`) | **SAFE_BY_ABSENCE** (no builder, no send) |

Do **not** treat the filename `C19_quickfix_not_wired` as “FIX is done.” It confirms the engine package is **absent** and the only codec is the simulator. That is the measured, intended-for-now state — not a §61 / §70 pass.

---

## 1. Binding law (what “QuickFIX/n” means)

`A35_quickfixn_packages.md` pin (quoted meaning, not implemented):

```xml
<PackageReference Include="QuickFIXn.Core" Version="1.14.1" />
<PackageReference Include="QuickFIXn.FIX44" Version="1.14.1" />
```

| Id | Allowed? | Present? |
|---|---|---|
| `QuickFIXn.Core` 1.14.1 | **Required** for a live adapter | **No** |
| `QuickFIXn.FIX44` 1.14.1 | **Required** (same version as Core) | **No** |
| `QuickFIXn.FIX4.4` (deprecated name) | **Do not add** | **No** |
| `QuickFIXn.FIX50*` / `QuickFIXn.FIXT11` | **Do not add** | **No** |
| `QuickFix.Net` / `QuickFix.Net.NetCore` / bare `QuickFIXn` | **Do not add** (`A35` unofficial-fork ban) | **HEAD yes / worktree no** |

Architecture §5: prefer QuickFIX/n; do not write a raw `TcpClient` engine. Product C# also has **zero** `TcpClient` / `SocketInitiator` / `IInitiator` / `SessionSettings` hits.

---

## 2. Every product `PackageReference` (worktree, 2026-08-18)

Scanned: `D:\Prop\apps\**\*.csproj`, `D:\Prop\src\**\*.csproj`, `D:\Prop\tests\**\*.csproj`. Excluded `bin/`, `obj/`, `node_modules/`, `vendor/`, `_tmp_*`.

| Project | Direct packages | QuickFIX/n? |
|---|---|---|
| `src/Domain/TraderIntelligence.Domain.csproj` | **none** | No |
| `src/Application/TraderIntelligence.Application.csproj` | `FluentValidation` 11.9.2 | No |
| **`src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj`** | **none** (Domain + Application project refs only) | **No** |
| `src/Mt5/TraderIntelligence.Mt5.csproj` | none (Domain + Application) | No |
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | EF Design 8.0.4, EF InMemory 8.0.4, Npgsql.EF 8.0.4, StackExchange.Redis 2.8.0 | No |
| `apps/api/TraderIntelligence.Api.csproj` | SignalR.Common 8.0.4, Serilog.AspNetCore 8.0.2, Swashbuckle 6.6.2 | No |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `Microsoft.Extensions.Hosting` 8.0.1 | **No** (project-refs Fix.CTrader) |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | `Microsoft.Extensions.Hosting` 8.0.1 | No |
| `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | coverlet, FA, Test.Sdk, Moq, xunit | **No** (project-refs Fix.CTrader) |
| `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | coverlet, FA, EF InMemory, Test.Sdk, xunit | **No** (project-refs Fix.CTrader) |

Worktree `Fix.CTrader.csproj` in full:

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

`git grep -n "QuickFIXn" -- '*.csproj' '*.props' '*.cs' '*.json'` → **empty**.

---

## 3. HEAD vs worktree (the only QuickFix string in product git)

`git log -p --all -S 'QuickFix' -- '*.csproj' '*.props' '*.cs'` yields a single event: initial commit `6c414477` added unofficial `QuickFix.Net` 1.8.0. No later commit added `QuickFIXn.*`. No `.cs` file ever referenced it.

**HEAD** (`src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj`, blob `1b394e9d4fc9b469ac0a4757b2d75fb7922d2e7b`):

```xml
<PackageReference Include="QuickFix.Net" Version="1.8.0" />
```

**Worktree** (blob `529a3a1c11def916ce8038388af8a7a2913505a9`) — `git diff` is exactly the deletion of that one line:

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

B05 already recorded that even when the 1.8.0 line existed, `deps.json` listed only Domain + Application + FluentValidation because no type from the package was referenced. Current worktree restore has gone one step further and dropped the reference entirely.

---

## 4. Restore / runtime graph (measured, not inferred)

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

`bin/Debug/net8.0/TraderIntelligence.Fix.CTrader.deps.json` libraries: this project, FluentValidation 11.9.2, Application, Domain. **No QuickFix / QuickFIXn library entry.**

`FileListAbsolute.txt` output assemblies: `TraderIntelligence.Fix.CTrader.*`, `TraderIntelligence.Application.*`, `TraderIntelligence.Domain.*`. **No `QuickFix.dll`.**

`apps/fix-worker` `PackageReference` = Hosting 8.0.1 only. Its `project.assets.json` has **zero** `QuickFix` matches. Worker `deps.json` top-level dependencies: Hosting, Application, Domain, Fix.CTrader, Infrastructure.

`Fix.CTrader.csproj.nuget.dgspec.json` frameworks block for Fix.CTrader has **no** `dependencies` package map — only project references.

---

## 5. Missing engine types (product C#)

Grep of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` for:

`QuickFix`, `QuickFIX`, `QuickFIXn`, `using QuickFix`, `SocketInitiator`, `ThreadedSocketInitiator`, `IInitiator`, `IApplication`, `SessionSettings`, `FileStorePath`, `FileLogPath`, `FileStoreFactory`, `FileLogFactory`

→ **no matches** in `*.cs` / `*.csproj` / `*.props` / `*.json` / `*.config` / `*.xml`.

No product files named `*.cfg`, `FIX44*.xml`, or `FIX44-CSERVER.xml` under `src/`, `apps/`, `tests/`.

`NewOrderSingle` / `35=D` in product C# is comments + a Domain helper name (`MayRetryNewOrderSingle`) + worker log text. There is no message builder and no send.

---

## 6. Simulator only — inventory

`D:\Prop\src\Fix.CTrader\` product sources (exclude `bin/` / `obj/`):

| File | Lines (content) | Role | QuickFIX/n? |
|---|---:|---|---|
| `Configuration/CTraderFixOptions.cs` | 55 | Host/port/CompID options. Defaults include live host `live-us-eqx-01.p.c-trader.com`. **Not bound** to an initiator. | No |
| `Parsing/FixMessageParser.cs` | 120 | Hand-rolled pipe/`SOH` parse + checksum. Comment: “intended for unit tests.” | No |
| `Testing/FixSimulationHarness.cs` | 185 | Generates pipe-delimited Logon / ER / SecurityList / MD / disconnect **strings**. | No |
| `Services/FixSessionOwnership.cs` | 114 | In-memory fencing lock. Comment says Redis in production. **Not** a FIX session. | No |

Harness header (worktree):

```csharp
/// <summary>
/// Generates cTrader-like FIX responses for unit tests (no live FIX connection required).
/// All returned messages use '|' separators as accepted by <see cref="FixMessageParser"/>.
/// </summary>
public sealed class FixSimulationHarness
{
    private readonly FixMessageParser _parser = new();
```

What it can emit (string factory, not a venue):

- `SimulateLogonSuccess` / `SimulateLogonFail`
- `SimulateExecutionReport_{New,Fill,PartialFill,Canceled,Rejected,Expired,UnknownState}`
- `SimulateDuplicateExecutionReport` (identity)
- `SimulateDisconnect` (heartbeat placeholder, not a dropped socket)
- `SimulateSecurityList` / `SimulateMarketDataSnapshot` (simplified / non-RoE tags, e.g. 1320/1321 for bid/ask)

What it cannot do (so “simulator only” is **not** “§61 done”):

- Accept a `NewOrderSingle` and reply
- Own a book / sequence store
- Open or refuse a TCP/SSL socket
- Drive Application ports used by a future live adapter
- Be selected via `VenueMode=InProcess` (that type does not exist)

`A68_fix_simulator.md` already classified this harness as **EXISTS_NEEDS_REFACTOR** (fixture builder, not the venue). C19 re-measures the same files after later edits; the class is unchanged in kind.

---

## 7. Worker is not the simulator and not QuickFIX/n

`apps/fix-worker/TraderIntelligence.FixWorker.csproj` references Fix.CTrader as a **project**, not as an engine host. The only NuGet is `Microsoft.Extensions.Hosting` 8.0.1.

Worktree `Worker.cs` (`SHA-256` `B48033A5…`):

- Reads `CTrader:RealCopyExecutionEnabled` (default false).
- Every 15 s stamps QUOTE `ReadyForMarketData` and TRADE `LoggedOn` on EF rows.
- Logs that it “refuses NewOrderSingle” when the flag is true — but **there is no send function to refuse**.
- Does **not** `new FixSimulationHarness()`, does **not** parse FIX, does **not** open a socket.

`Program.cs` composes Infrastructure + `DemoSeeder`. Seeder inserts QUOTE `ReadyForMarketData` and TRADE `LoggedOn` against host `live-us-eqx-01.p.c-trader.com` **without a session**. Dashboard `EfDashboardQueries` will report those rows as logged-on. That is an **ops lie**, not wiring.

HEAD `Worker.cs` is still the template 1 s “Worker running at” loop (blob `f02ff093…`). Neither HEAD nor worktree hosts QuickFIX/n.

---

## 8. Tests do not consume the simulator

| Project | Refs Fix.CTrader? | Refs QuickFIX/n? | Calls harness? |
|---|---|---|---|
| `tests/Unit` | Yes | No | **No** |
| `tests/Integration` | Yes | No | **No** |
| `tests/Fix` | **project missing** | — | — |

Grep of `D:\Prop\tests` for `FixSimulationHarness`, `SimulateLogon`, `SimulateExecutionReport`, `FixMessageParser` → **no matches**.

So “simulator only” means: **the only FIX codec that exists is the unused harness**, not “tests run through a simulated venue.”

---

## 9. Stale reports (use this file for the package question)

| Report | Claim | C19 measure |
|---|---|---|
| A05 | Empty `Class1`, zero packages, no simulator | **Stale.** Four `.cs` files + harness exist. |
| A49 / A50 / A57 | `QuickFix.Net` 1.11.2 | **Stale version.** HEAD was 1.8.0; worktree has **0**. |
| A68 / A100 / A101 / A102 / B05 / B07 / B08 | `QuickFix.Net` 1.8.0 on disk | **True of HEAD.** **False of worktree** (line deleted, restore dropped). Still **not** QuickFIX/n. |
| A10 | QuickFIX/n not referenced anywhere including Fix.CTrader | **Still true** for official `QuickFIXn.*`. |
| A35 | Pin 1.14.1 pair; checklist items unchecked | **Still unchecked.** |

---

## 10. Hashes (worktree unless noted)

| Path | SHA-256 |
|---|---|
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` **worktree** | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` |
| same file **HEAD** (via `git show` bytes) | `649D5E9B3D70DE1CEDA8AD3C19416A00F3EED8ACDA71131F413BD485B8D283D0` |
| `src/Fix.CTrader/Testing/FixSimulationHarness.cs` | `99A28D8F3F49028706C75B9C4DC46B4CCB3FF98E90AAED3B4B874DD1B4351616` |
| `src/Fix.CTrader/Parsing/FixMessageParser.cs` | `C58681E761D43052B53D2A8D00883C461A9E3CEB5B7DF8995D50F8155F710E3D` |
| `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308` |
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` |
| `apps/fix-worker/Worker.cs` | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` |
| `apps/fix-worker/Program.cs` | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` |
| `Directory.Build.props` | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` |

Git blobs (worktree `git hash-object`): csproj `529a3a1c…`, harness `19dd8a7a…`, parser `f6aca38c…`, options `f2cd089d…`, ownership `2e42ef70…`, FixWorker csproj `4026c392…`, Worker `f5d12863…`, Program `6a7443a3…`.

HEAD blobs: csproj `1b394e9d…`, harness `433326b2…`, parser `c799bb15…`, Worker `f02ff093…`, Program `57e5142a…`.

---

## 11. What this does **not** authorize

1. Do **not** add `QuickFix.Net` back. If an engine is added later, it must be the A35 pair (`QuickFIXn.Core` + `QuickFIXn.FIX44` **1.14.1**), plus a cTrader RoE dictionary — not generic FIX44 alone.
2. Do **not** write a `TcpClient` FIX engine (`A05`, `A35`, architecture §1.8 / §5).
3. Do **not** treat seeder/worker `LoggedOn` as evidence of a wired session (`C07`, `A101`).
4. Do **not** treat `FixSimulationHarness` as a completed §61 venue. It is a fixture builder, unwired, with several non-RoE tags (`A68`).
5. Do **not** point the simulator or any future initiator at account `1369850` / `*.c-trader.com` as a first test. Live `NewOrderSingle` stays off.
6. This agent did **not** modify product source. Committing the worktree csproj deletion is a separate change-control decision.

---

## 12. One-line answer to the assigned question

**Yes: official QuickFIX/n is not referenced yet (never has been). HEAD still lists unused unofficial `QuickFix.Net` 1.8.0; the worktree removed that line. The only FIX codec on disk is `FixSimulationHarness` + `FixMessageParser`; the worker and tests do not call it; there is no live initiator.**
