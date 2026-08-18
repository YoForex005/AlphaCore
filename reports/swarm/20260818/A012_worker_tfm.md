# A012 — Worker / API TFM: who is still `net8.0` vs `net8.0-windows` x64

| Field | Value |
|---|---|
| Agent | A012 (senior engineer, evidence-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A012_worker_tfm.md` |
| Workspace | `D:\Prop` |
| Assigned files | `D:\Prop\apps\api\TraderIntelligence.Api.csproj`, `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`, `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` |
| Supporting reads | `D:\Prop\Directory.Build.props`, `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`, `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj`, `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj`, `D:\Prop\src\Application\TraderIntelligence.Application.csproj`, `D:\Prop\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj`, `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`, `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`, `D:\Prop\src\Infrastructure\DependencyInjection.cs`, host `Program.cs` + `bin\*\net8.0\*.runtimeconfig.json` + `*.deps.json`, restore `obj\*.csproj.nuget.dgspec.json` |
| Peers (do not contradict) | `A002_api_dummy_path.md` (API still `net8.0`), `A105_windows_dlls.md` (worker output has 0 vendor Manager DLLs), `R021_dll_load.md` (isolated `net8.0` x64 **can** load the PE; product `net8.0` → `net8.0-windows` is **NU1201**) |
| Product source modified | **No.** This file is the only write. |
| Secrets | **None printed.** Config **key names** and UserSecrets **IDs** only. No passwords, no connection strings. |

Classification: **EXISTS_NEEDS_REFACTOR** (all three hosts still portable `net8.0`). Clean restore of the current graph is **FAIL NU1201**. Stale `bin\*\net8.0` outputs **do not** contain MetaQuotes Manager DLLs.

---

## 0. Answers (measured)

| Question | Answer |
|---|---|
| Which of api / mt5-worker / fix-worker still target `net8.0` instead of `net8.0-windows` x64? | **All three.** Every host csproj is `<TargetFramework>net8.0</TargetFramework>`. **None** set `PlatformTarget`, `RuntimeIdentifier`, `EnableWindowsTargeting`, or `UseWindowsForms`. |
| Does `Directory.Build.props` override them to Windows x64? | **No.** Root props set LangVersion / Nullable / ImplicitUsings / Deterministic only. No TFM. No RID. |
| Will those hosts load MetaQuotes Manager DLLs as they sit today? | **No as a product graph.** (1) Clean restore: **NU1201** — `net8.0` cannot `ProjectReference` `net8.0-windows7.0` (`Infrastructure`, `Mt5`). (2) Stale Debug/Release outputs under `bin\*\net8.0\` have **zero** of `MetaQuotes.MT5CommonAPI64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MT5APIManager64.dll`. (3) Isolated fact from R021: a **Windows AMD64** .NET 8 process *can* load those PEs if they sit beside the exe — TFM `net8.0` vs `net8.0-windows` is not the PE blocker. The **product** hosts do not copy them and do not restore against the Windows libraries. |
| Does FIX need Manager64? | **No.** cTrader FIX is TLS + managed parser. `fix-worker` still **pulls** `Infrastructure` → `Mt5` and calls `AddTraderIntelligence`, so it is on the same Manager64 type-load path if the graph ever compiles. That is a leak, not a FIX requirement. |

Honest one-liner: **api, mt5-worker, and fix-worker are all still portable `net8.0` AnyCPU hosts. Only `src/Mt5` and `src/Infrastructure` are `net8.0-windows` + x64. The hosts will not restore that graph, and their existing `net8.0` bins will not `LoadLibrary` Manager64 because the trio is not next to the exe.**

---

## 1. Host csproj census (the assigned three)

`D:\Prop\Directory.Build.props` does **not** set `TargetFramework` or `PlatformTarget`:

```1:9:D:\Prop\Directory.Build.props
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

| Project | Path | TFM (csproj) | `PlatformTarget` | `RuntimeIdentifier` | SDK | ProjectReferences that are Windows |
|---|---|---|---|---|---|---|
| API | `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | **`net8.0`** | absent | absent | `Microsoft.NET.Sdk.Web` | `Infrastructure` (`net8.0-windows`) |
| MT5 worker | `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` | **`net8.0`** | absent | absent | `Microsoft.NET.Sdk.Worker` | `Infrastructure` + **`Mt5`** (both `net8.0-windows`) |
| FIX worker | `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` | **`net8.0`** | absent | absent | `Microsoft.NET.Sdk.Worker` | `Infrastructure` (`net8.0-windows`); also `Fix.CTrader` (`net8.0`) |

Verbatim host TFM blocks:

```15:19:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

```3:8:D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1</UserSecretsId>
  </PropertyGroup>
```

```3:8:D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-TraderIntelligence.FixWorker-400770db-b19b-4432-8d23-92415fa24b79</UserSecretsId>
  </PropertyGroup>
```

UserSecrets IDs exist on both workers. This report does **not** read or print secret values.

### 1.1 Restore graph agrees with the csproj (not with Windows)

Last recorded `originalTargetFrameworks` in nuget dgspec:

| Host dgspec | `originalTargetFrameworks` | RID graph |
|---|---|---|
| `D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json` | `net8.0` | `...\sdk\8.0.424\PortableRuntimeIdentifierGraph.json` |
| `D:\Prop\apps\mt5-worker\obj\TraderIntelligence.Mt5Worker.csproj.nuget.dgspec.json` | `net8.0` | same Portable graph |
| `D:\Prop\apps\fix-worker\obj\TraderIntelligence.FixWorker.csproj.nuget.dgspec.json` | `net8.0` | same Portable graph |

The **same** mt5-worker dgspec already records the downstream libraries as `net8.0-windows` / restore TFM `net8.0-windows7.0`:

- `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` → `"net8.0-windows"`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` → `"net8.0-windows"`

That is exactly the NU1201 pair R021 measured: a `net8.0` exe **cannot** consume a `net8.0-windows7.0` `ProjectReference`. Isolated `net8.0` **can** `<Reference>` the vendor DLL by HintPath; that is a different graph than these hosts.

---

## 2. Library TFM split (why the hosts are stuck)

| Project | TFM | `PlatformTarget` | MetaQuotes `<Reference>` / copy-dlls |
|---|---|---|---|
| `src/Domain` | `net8.0` | — | no |
| `src/Application` | `net8.0` | — | no |
| `src/Fix.CTrader` | `net8.0` | — | no (FIX is not Manager64) |
| `src/Mt5` | **`net8.0-windows`** | **x64** | **yes** — CommonAPI64 + ManagerAPI64 + `None` copy of `MT5APIManager64.dll` |
| `src/Infrastructure` | **`net8.0-windows`** | **x64** | no direct HintPath; `ProjectReference` to `Mt5` |

`src/Mt5` is the only project that binds the official trio:

```6:29:D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    ...
    <TargetFramework>net8.0-windows</TargetFramework>
    ...
    <PlatformTarget>x64</PlatformTarget>
```

Measured: `D:\Prop\src\Mt5\bin\Release\net8.0-windows\` **does** contain the trio (`MetaQuotes.MT5CommonAPI64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MT5APIManager64.dll`). That copy contract stops at the library output. It does **not** land in any of the three host `net8.0` output folders.

---

## 3. Will they load MetaQuotes DLLs?

Three different questions. Do not collapse them.

### 3.1 Clean MSBuild of the three hosts — **NO (will not even restore)**

R021 already compiled this tree after `Mt5`/`Infrastructure` retargeted:

| Project | Result |
|---|---|
| `apps/mt5-worker` `net8.0` → refs Mt5 + Infrastructure | **FAIL NU1201** (`net8.0` cannot reference `net8.0-windows7.0`) |
| `apps/api` `net8.0` → refs Infrastructure | **FAIL NU1201** |
| `apps/fix-worker` `net8.0` → refs Infrastructure | **same mismatch** (not a special case) |

Until the hosts become `net8.0-windows` + x64 (or the Manager wrapper is isolated behind a Windows-only project the portable host does not reference), there is no new output to load anything.

### 3.2 Stale `bin\*\net8.0` outputs already on disk — **NO**

Existing host outputs (API Debug; both workers Debug + mt5-worker Release) are folders named `net8.0`. Runtimeconfig `tfm` is `"net8.0"` (API also frames `Microsoft.AspNetCore.App`; workers only `Microsoft.NETCore.App`). **No** `runtimeOptions.includedFrameworks` Windows desktop moniker. **No** RID.

File census of vendor Manager images beside the exe:

| Output folder | `MetaQuotes.MT5*.dll` | `MT5APIManager64.dll` | `TraderIntelligence.Mt5.dll` |
|---|---|---|---|
| `D:\Prop\apps\api\bin\Debug\net8.0\` | **absent** | **absent** | present (stale portable build) |
| `D:\Prop\apps\mt5-worker\bin\Debug\net8.0\` | **absent** | **absent** | present |
| `D:\Prop\apps\mt5-worker\bin\Release\net8.0\` | **absent** | **absent** | present |
| `D:\Prop\apps\fix-worker\bin\Debug\net8.0\` | **absent** | **absent** | present (transitive via Infrastructure) |

`TraderIntelligence.Mt5Worker.deps.json` `runtimeTarget.name` is `.NETCoreApp,Version=v8.0`. Grep of that deps graph: **no** `MetaQuotes` entries. The managed wrappers were never recorded as runtime assets of the worker.

Native load path in product code:

```66:70:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

`LiveMt5Registration.CreateConnectors` sets `NativeDllDirectory = Path.GetFullPath(AppContext.BaseDirectory)` (the host output dir). Factory `Initialize` is the C++/CLI wrapper around `LoadLibraryW` of `MT5APIManager64.dll` (A105 / vendor `MT5APIManager.h`). R021 measured `Initialize` without that native file → `MT_RET_ERR_NOTFOUND`.

Worse: constructing `NativeMt5BrokerConnector` JITs types from `MetaQuotes.MT5CommonAPI` / `MetaQuotes.MT5ManagerAPI`. Those assemblies are **not** in the host output. Type-load is `FileNotFoundException` / `BadImageFormatException` **before** `ConnectAsync`.

### 3.3 Isolated PE fact (R021) — **YES on Windows x64 if files are present**

Do not over-claim the TFM as the PE blocker:

| Isolated experiment (R021) | Result |
|---|---|
| `net8.0` x64 `<Reference>` Manager64 + CommonAPI64 | compile **PASS** |
| `net8.0-windows` x64 same | compile **PASS** |
| Windows x64 process `SMTManagerAPIFactory.Initialize` with trio beside exe | **PASS** |
| Same without `MT5APIManager64.dll` | `MT_RET_ERR_NOTFOUND` |
| Linux / Wine / 32-bit | **NO** (PE32+ `0x8664`; loader is `LoadLibraryW`) |

So: **retargeting hosts to `net8.0-windows` + `PlatformTarget` x64 is still the honest product TFM**, because they are Windows-only Manager processes. It is required to **restore** against `src/Mt5`. It is **not** a magic extra bit that makes the PE loadable — `net8.0` x64 already could, in isolation.

### 3.4 Who would even attempt the load?

All three hosts call `builder.Services.AddTraderIntelligence(...)`. DI now fail-closes without real `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` (values not printed) and then `CreateConnectors` → two `NativeMt5BrokerConnector` instances. `LiveIngestHostedService` later calls `ConnectAsync` → `SMTManagerAPIFactory.Initialize`.

| Host | Needs Manager64 for its job? | Would attempt load if graph compiled + passwords present? |
|---|---|---|
| `mt5-worker` | **Yes** (local Manager ingest) | **Yes** — direct `ProjectReference` to `Mt5` + shared DI |
| `api` | **Should not** (architecture §5: Linux-capable API) | **Yes** — transitive `Infrastructure` → `Mt5`; same DI |
| `fix-worker` | **No** (cTrader FIX TLS) | **Yes** — same DI leak; FIX itself never `DllImport`s Manager64 |

Architecture still wants: Windows worker owns `LoadLibrary`; do not force Manager PEs into a portable/Linux API or FIX process.

---

## 4. AnyCPU vs x64 (hosts)

None of the three set `<PlatformTarget>x64</PlatformTarget>` or `<RuntimeIdentifier>win-x64</RuntimeIdentifier>`. SDK default for `net8.0` is AnyCPU, `Prefer32Bit` false → a 64-bit OS typically starts a 64-bit process. R021 AnyCPU on a 64-bit box loaded the wrapper. That is **not** a pin:

- A 32-bit process cannot map AMD64 mixed-mode images (`BadImageFormatException`).
- Prefer32Bit / `dotnet --arch x86` / 32-bit IIS would fail.
- Vendor samples pin **x64**. Product `Mt5` already pins x64. Hosts do not.

---

## 5. What “done” looks like (recommendation only — this agent did not edit csproj)

| Process | Required TFM | Why |
|---|---|---|
| `apps/mt5-worker` | **`net8.0-windows` + `PlatformTarget` x64** (optional `RuntimeIdentifier=win-x64`) | Only host that should `LoadLibrary` Manager64. Must `ProjectReference` `src/Mt5`. Must copy the CMake trio beside the exe (already declared on `Mt5`; flows once TFM matches). |
| `apps/api` | Stay **`net8.0`** **if** it stops referencing `Infrastructure`/`Mt5` **or** split a Windows-free Infrastructure. If API keeps constructing `NativeMt5BrokerConnector`, it **must** become `net8.0-windows` x64 — and then it is no longer Linux-legal. | Architecture §5. |
| `apps/fix-worker` | Stay **`net8.0`** and **stop** referencing `Infrastructure` that pulls `Mt5`. FIX does not load MetaQuotes. Current shared DI makes the TFM question the same as the API. | Do not drag Manager64 into the FIX process. |

Copy-dlls contract (already on `src/Mt5`, missing on hosts until restore works): `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MetaQuotes.MT5CommonAPI64.dll` from `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`.

Do **not** copy `MetaQuotes.MT5WebAPI.dll` (PE32 i386) or Gateway APIs next to a 64-bit Manager worker.

---

## 6. Adjacent (out of assignment, one line each)

- `tests/Integration` is also historically `net8.0` → same NU1201 vs `Infrastructure`/`Mt5` (R021).
- `Infrastructure` `AddHostedService<CTraderFixLogonHostedService>()` has no `Fix.CTrader` project reference (A002). Separate compile break; not a TFM issue.
- Stale `src\Infrastructure\bin\Debug\net8.0\` and `src\Mt5\bin\Debug\net8.0\` folders still exist from before the Windows retarget. **Csproj of record** for those two is `net8.0-windows`.

---

## 7. Verdict box

| Host | Still `net8.0`? | `net8.0-windows` x64? | Will load MetaQuotes Manager DLLs today? |
|---|---|---|---|
| `apps/api` | **YES** | **NO** | **NO** — NU1201 on clean restore; stale `net8.0` bin has no trio; should not load them even after a TFM fix unless API is intentionally a Windows Manager host |
| `apps/mt5-worker` | **YES** | **NO** | **NO** — same NU1201; Release/Debug `net8.0` bins have **0** vendor Manager DLLs; this **is** the process that must load them after retarget + copy |
| `apps/fix-worker` | **YES** | **NO** | **NO** — same NU1201 / missing trio; FIX protocol does not need them; shared DI would try if the graph compiled |

**Count: 3 / 3 assigned hosts still target `net8.0`. 0 / 3 are `net8.0-windows` x64. 0 / 3 will load MetaQuotes DLLs in the current product outputs.**
