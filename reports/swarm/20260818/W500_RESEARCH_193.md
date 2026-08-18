# W500_RESEARCH_193 — `Api.csproj` TargetFramework: `net8.0` without windows/x64 vs `MT5APIManager64`

| Field | Value |
|---|---|
| Slot | **193** |
| Agent | W500_RESEARCH_193 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_193.md` |
| Assigned | Check `Api.csproj` `TargetFramework`. Claim: **`net8.0` without windows/x64 cannot load `MT5APIManager64`**. Goal overlay: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only product-adjacent write (plus INDEX / SWARM_LOG catalog). |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Password / proxy / FIX keys named only. No tag 554 values, no proxy auth, no manager passwords, no login dump. Flag booleans only. |
| Live attach this pass | **No.** No Manager reconnect. No FIX send. Census is prior same-day JSON (`2026-08-18T08:42:16Z`). |
| Method | Independent `read_file` + `grep` + `list_dir` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current `TraderIntelligence.Api.csproj`, nuget dgspec/cache, GeneratedMSBuildEditorConfig, FileListAbsolute, Debug `deps.json` + `runtimeconfig.json` + output dirs (including `bases/`), sibling host csproj + restore caches, `src/Mt5` connector, ingest/DI/FIX session, vendor `MT5APIManager.h` `LoadLibraryW`, R021 PE inspect + run logs, live census JSON, YoPips CMake trio copy. **No product edit.** |
| Same-topic siblings (do not treat as this file) | `W500_RESEARCH_13.md`, `W500_RESEARCH_33.md`, `W500_RESEARCH_53.md`, `W500_RESEARCH_73.md`, `W500_RESEARCH_93.md`, `W500_RESEARCH_113.md`, `W500_RESEARCH_133.md`, `W500_RESEARCH_153.md`, `W500_RESEARCH_173.md` — same question, earlier slots. This file is a **slot-193 re-measure**, not a copy. |
| Stale (do not cite as current) | `A012_worker_tfm.md`, `A002_api_dummy_path.md` (quote API as portable `net8.0`). `W500_SLICE_5.md` (API NU1201). `A105` (`src/Mt5` portable + Fake only). R021 §6 NU1201 row for `apps/api` is **historical**. Slots that said API `RealCopyEnabled` is forced false / FEATURE literal false are **stale vs this tree**. |

**Honesty rule:** `-windows` on the TFM is **not** what `LoadLibraryW` checks. `LoadLibraryW` checks **OS + PE machine + file present**. Isolated `net8.0` x64 on Windows **can** initialize the factory (R021). In **this product graph**, a portable `net8.0` **host** cannot `ProjectReference` `net8.0-windows` `src/Mt5` / `Infrastructure` (**NU1201**) and therefore cannot stage the trio beside the exe. Do not collapse those two facts. Do not claim “EX5 decompiled,” “95% live copy,” or a fresh Manager attach from this slot.

There is **no** file named `Api.csproj`. The assigned host is `D:\Prop\apps\api\TraderIntelligence.Api.csproj`.

---

## 0. Verdict (binding)

**PASS — current `TraderIntelligence.Api.csproj` is already `net8.0-windows` + `PlatformTarget` x64; Manager trio is in the Windows output; catalog path can load `MT5APIManager64`; copy stays off (`SAFE_BY_ABSENCE`) even though lab `REAL_COPY_EXECUTION_ENABLED` is env-bound `true`. Residual FAIL: `mt5-worker` / `fix-worker` / Integration still `net8.0` + NU1201.**

The folklore sentence “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is **half-true**. Split it:

| Claim | Measured this slot | Class |
|---|---|---|
| Current API TFM | **`net8.0-windows`** + **`PlatformTarget` x64** (`csproj` L18–19) | `EXISTS_AND_GOOD` |
| `RuntimeIdentifier` / `win-x64` RID pin | **Absent** — 0 hits in product `*.csproj` / `Directory.Build.props` | not required for LoadLibrary; process is still x64 via `PlatformTarget` |
| API restore against `src/Mt5` + `Infrastructure` | **`success: true`**, `"logs": []`, `dgSpecHash` `6bXcKBewNHo=` | `EXISTS_AND_GOOD` |
| Restore TFM alias | `originalTargetFrameworks` = **`net8.0-windows`**; framework id **`net8.0-windows7.0`** | `EXISTS_AND_GOOD` |
| Generated editorconfig | `build_property.TargetFramework = net8.0-windows` (min platform 7.0) | `EXISTS_AND_GOOD` |
| Trio next to current API exe | **Yes** — FileListAbsolute L1–3 + directory listing of `bin\Debug\net8.0-windows\` | `EXISTS_AND_GOOD` |
| Manager already mapped in this output | **Yes** — `bases/AchieverGlobalMarkets-Server/` (`*-2027.dat`) + `bases/StarwaveFX-Server/` (`*-9904.dat`) | prior live session |
| Stale `bin\Debug\net8.0\` (pre-retarget) has the trio | **No** — listing has **zero** `MT5API*` / `MetaQuotes*`; that folder’s `deps.json` has **0** MetaQuotes strings | leftover; do not run |
| Isolated `net8.0` x64 can `SMTManagerAPIFactory.Initialize` on Windows when native DLL is beside exe | **Yes (R021)** `Initialize(null)=MT_RET_OK (0)` in `RefNet8.run.txt` | PE-load **not** gated by `-windows` |
| Isolated `net8.0-windows` x64 | **Yes** — `RefNet8Win.run.txt` same `MT_RET_OK` | TFM extra is for NuGet, not LoadLibrary |
| Isolated `net8.0` **x86** can map this AMD64 PE | **No** — PE machine `0x8664`; R021 CS8012 + no x86 runtime (`0x80008083`) | **x64 required** |
| Linux / compose `mcr.microsoft.com/dotnet/sdk:8.0` can `LoadLibraryW` this PE | **No** (`docker-compose.yml` L17 + comment L30) | **Windows OS required** |
| Portable `net8.0` **host** can `ProjectReference` `src/Mt5` (`net8.0-windows`) | **No — NU1201** (still true for `mt5-worker`, `fix-worker`, Integration tests) | product-graph gate |
| Remaining hosts still `net8.0` | **`mt5-worker`**, **`fix-worker`**, **`tests/Integration`** — restore `success: false` | `EXISTS_NEEDS_REFACTOR` |
| `mt5-worker` output has `MT5APIManager64.dll` | **No** — `bin\Debug\net8.0\` has `TraderIntelligence.Mt5.dll` only; **zero** vendor trio; `deps.json` 0 MetaQuotes | cannot `Initialize` |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | **Yes, path + prior live census** — Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** (re-summed this slot; not re-attached) | catalog-only |
| Copy to cTrader may send live orders | **No** — `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `CTraderFixSession` emits only `35=A`; product hop `35=D` = **0** | **`SAFE_BY_ABSENCE`** |
| Lab `REAL_COPY_EXECUTION_ENABLED` | **`.env` L73 = `true`**; DI binds it (`DependencyInjection` L41). Flag is **armed**. Sender is still **absent**. | flag true ≠ send |
| `/api/settings` `FEATURE_COPY_TRADING_ENABLED` | **literal `true`** (`Program.cs` L77) | pipeline ON, send still off |
| Residual standalone demo tool | `CTraderFixDemoTestTrade.Build("D")` exists; **not** called by API/copy hosted services; demo-host gate refuses live account `1369850` | not the copy hop |
| Risk to capital from this process | **None** | Manager read + FIX Logon only |

One-line:

```text
Api host = net8.0-windows + x64 (restore OK; trio in net8.0-windows output; bases/ 2027+9904).
RID win-x64 is NOT set. Isolated net8.0 x64 CAN LoadLibrary on Windows (R021).
net8.0 host cannot ProjectReference Mt5 (NU1201). x86 / Linux cannot.
ALL groups/traders = GroupRequestArray("*") + UserRequestArray (18/8460 prior).
No product 35=D. NewOrderSingleImplemented=false. Env REAL_COPY may be true. No capital at risk.
```

Do **not** retarget the API again. Do **not** add a `35=D` sender in this task.

---

## 1. Assigned file — `TraderIntelligence.Api.csproj` (measured now)

Path: `D:\Prop\apps\api\TraderIntelligence.Api.csproj` (23 lines, `Microsoft.NET.Sdk.Web`).

```17:22:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

It **ProjectReferences** Domain + Application + Infrastructure + **Mt5** + Fix.CTrader (`csproj` L3–8). That is a **Windows Manager host**, not a Linux-portable API.

```3:8:D:\Prop\apps\api\TraderIntelligence.Api.csproj
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
```

No `<RuntimeIdentifier>`. Bitness is pinned by `PlatformTarget` x64, not by `win-x64` RID. Grep of `D:\Prop` product `*.csproj` + `Directory.Build.props` for `RuntimeIdentifier` this slot: **0 hits**.

`D:\Prop\Directory.Build.props` still does **not** set TFM, RID, or `PlatformTarget` (LangVersion / Nullable / ImplicitUsings / TreatWarningsAsErrors / Deterministic only). The API pin is **local to this csproj**.

### 1.1 Restore graph agrees with the csproj

`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`:

| Field | Value |
|---|---|
| `originalTargetFrameworks` | **`net8.0-windows`** |
| Restore TFM / alias | **`net8.0-windows7.0`** / `net8.0-windows` |
| ProjectReferences | Application, Domain, Fix.CTrader, **Infrastructure**, **Mt5** |

`D:\Prop\apps\api\obj\project.nuget.cache`: **`"success": true`**, `"logs": []`. Hash `dgSpecHash` = `6bXcKBewNHo=`.

`D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`:

```text
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
build_property.UsingMicrosoftNETSdkWeb = true
```

### 1.2 Output that can actually load Manager64

`FileListAbsolute` (`D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.csproj.FileListAbsolute.txt`) L1–3 + directory listing of `D:\Prop\apps\api\bin\Debug\net8.0-windows\`:

| File | Present? |
|---|---|
| `MT5APIManager64.dll` | **Yes** (FileListAbsolute L3) |
| `MetaQuotes.MT5ManagerAPI64.dll` | **Yes** (L2) |
| `MetaQuotes.MT5CommonAPI64.dll` | **Yes** (L1) |
| `TraderIntelligence.Api.exe` | **Yes** |
| Manager `bases/` cache dirs `AchieverGlobalMarkets-Server/` + `StarwaveFX-Server/` | **Yes** (process has already mapped the native DLL) |

`bases/` contents observed this slot (manager login digits only; not secrets):

- `bases/AchieverGlobalMarkets-Server/users/users-2027.dat`
- `bases/AchieverGlobalMarkets-Server/positions/positions-2027.dat`
- `bases/StarwaveFX-Server/users/users-9904.dat`
- `bases/StarwaveFX-Server/positions/positions-9904.dat`

`TraderIntelligence.Api.deps.json` (Windows output) records:

```text
MetaQuotes.MT5ManagerAPI64/5.5570.0.0  fileVersion 5.0.0.5584  type=reference
MetaQuotes.MT5CommonAPI64/5.5570.0.0   fileVersion 5.0.0.5584  type=reference
```

`runtimeconfig.json` `tfm` is still `"net8.0"` with `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` only — **normal** for a `net8.0-windows` Web SDK project that does not pull WindowsDesktop. That string does **not** mean the project is portable `net8.0`. Do not cite `runtimeconfig.json` as the TFM source of truth; cite the csproj + dgspec + editorconfig.

Stale leftover `D:\Prop\apps\api\bin\Debug\net8.0\`: listing has Application/Domain/Infrastructure/Mt5 **managed** assemblies and **zero** `MT5APIManager64.dll` / `MetaQuotes.*`. Grep of that folder for those tokens = **0**. Its `deps.json` first-level dependencies are Application + Domain + Infrastructure only (no Mt5 / no Fix.CTrader / no MetaQuotes). Running that old exe cannot `Initialize`. Operator trap. Do not use it.

Copy contract lives on `src/Mt5` (not on the API csproj):

```6:29:D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      ...
    <None Include="..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    ...
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Measured: `D:\Prop\src\Mt5\bin\Debug\net8.0-windows\` **does** contain the trio. Once the host TFM matches, MSBuild flows those files into `apps/api/bin/Debug/net8.0-windows/`. That is why the current API output has the files and the stale `net8.0` output does not.

`LiveMt5Registration.CreateConnectors` sets `NativeDllDirectory = Path.GetFullPath(AppContext.BaseDirectory)` — the host output dir. `NativeMt5BrokerConnector.ConnectCore` then:

```66:70:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

---

## 2. What “cannot load `MT5APIManager64`” actually means

### 2.1 The file (PE, not folklore)

Measured in `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json` from `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`:

| File | Bytes | Machine | Kind | SHA-256 |
|---|---:|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | **`0x8664` AMD64** | **native** (`HasClr=false`) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | mixed-mode C++/CLI (Framework 4.7.2, CorFlags `NATIVE_ENTRYPOINT`) | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | same mixed-mode family | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

`MT5APIManager64.dll` imports `KERNEL32.dll`, `WS2_32.dll`, `ADVAPI32.dll`, `CRYPT32.dll`, `USER32.dll`, `SHELL32.dll`, `ole32.dll`, `OLEAUT32.dll`, `IPHLPAPI.DLL` — **Win32 only**. Not ELF. Not AnyCPU. Not `net8.0` IL.

Vendor factory (`MT5APIManager.h` 1719–1727):

```1719:1727:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
inline MTAPIRES CMTManagerAPIFactory::Initialize(LPCWSTR dll_path/*=NULL*/)
  {
   wchar_t  path[MAX_PATH]={};
   if(!FindLibrary(dll_path,path,_countof(path)-1))
      return(MT_RET_ERR_NOTFOUND);
   if((m_hmodule=::LoadLibraryW(path))==NULL)
      return(MT_RET_ERR_NOTFOUND);
```

Loader is **`LoadLibraryW`**. Missing file → `MT_RET_ERR_NOTFOUND` (13). There is no `dlopen` branch. `FindLibrary` (h:1769–1783) prefers `MT5APIManager64avx2.dll` / `avx` / `arm`, then vanilla `MT5APIManager64.dll`. CMake copies the vanilla trio only (Prop `mt5-sdk/CMakeLists.txt` L114–118 `mt5sdk_copy_runtime_dlls` is **WIN32 only**; YoPips `CMakeLists.txt` L56–66 `copy_if_different` of the same three files).

### 2.2 Isolated TFM experiment (R021 — not re-run; logs re-read)

`D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\`:

| Isolated project | TFM | Platform | Initialize with trio beside exe |
|---|---|---|---|
| `RefNet8` | `net8.0` | x64 | **`MT_RET_OK` (0)** (`RefNet8.run.txt`) |
| `RefNet8Win` | `net8.0-windows` | x64 | **`MT_RET_OK` (0)** (`RefNet8Win.run.txt`) |
| `LoadNet8` | `net8.0` | x64 | `Assembly.LoadFrom` **OK**; `NativeLibrary.Load` **OK**; collectible ALC **BadImageFormatException** |
| `Initialize` without native `MT5APIManager64.dll` | either TFM | x64 | wrapper still loads; init **`MT_RET_ERR_NOTFOUND` (13)** (`R021_dll_load.md` L120) |
| `RefNet8x86` | `net8.0` | **x86** | CS8012 + no x86 runtime `0x80008083` (`R021_dll_load.md` L96) |

So: **missing `-windows` is not the PE-load blocker.** Missing **Windows OS**, missing **AMD64 process**, or missing **`MT5APIManager64.dll` beside `Initialize`** are.

R021 §6 still lists `apps/api` as `net8.0` → **NU1201**. That row is **stale**. This slot’s API cache is `success: true`.

### 2.3 Product-graph gate (why API *did* need `-windows` + x64)

`src/Mt5` and `src/Infrastructure` are `net8.0-windows` + x64. NuGet will **not** let a `net8.0` exe `ProjectReference` a `net8.0-windows7.0` library:

| Host (measured `project.nuget.cache` now) | TFM | Restore | Error |
|---|---|---|---|
| `apps/api` | `net8.0-windows` | **`success: true`** | none (`dgSpecHash` `6bXcKBewNHo=`) |
| `tools/LiveBrokerProbe` | `net8.0-windows` + x64 | **`success: true`** (`dgSpecHash` `bcTm25qIqWk=`) | used for the 08:42Z census |
| `apps/mt5-worker` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure **and** Mt5 (`BMefpWc6ygQ=`) |
| `apps/fix-worker` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure (`3nFO35Ml2jE=`) |
| `tests/Integration` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure **and** Mt5 (`eWT2aWaUBSk=`) |
| `tests/Unit` | `net8.0` (Domain + Application + Fix.CTrader only) | **`success: true`** | correct — no Mt5/Infra reference |

`mt5-worker` GeneratedMSBuildEditorConfig still says `build_property.TargetFramework = net8.0`. Its `bin\Debug\net8.0\` listing has `TraderIntelligence.Mt5.dll` but **zero** `MT5APIManager64.dll` / `MetaQuotes.MT5*`. Its `deps.json` has **0** MetaQuotes strings. That stale worker output cannot `Initialize`.

Portable libraries that **should** stay `net8.0` (no Manager PE): Domain, Application, Fix.CTrader. That split is correct.

`docker-compose.yml` L17 still runs the API as `mcr.microsoft.com/dotnet/sdk:8.0` (Linux). Comment L30: “Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.” A Linux `dotnet run` of the now-`net8.0-windows` API is the **wrong** host for Manager64 even though the csproj is already Windows.

---

## 3. ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Fetch path (no cap)

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51):

1. `GetGroupsAsync` → `GroupRequestArray("*")`, fallback `GroupTotal`/`GroupNext`.
2. `GetAccountsAsync(null, …)` → walks **every** group, then `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`.

There is **no** `Take(` in ingest (0 hits in that file). Residual `Take(200)` is only dashboard slices (`GET /api/trades`, `GET /api/copy/intents`), not catalog.

`LiveIngestHostedService` iterates both Native connectors (DI registers Achiever + Starwave). Dummy `FakeMt5BrokerConnector` / `DemoSeeder` are **not** referenced from `apps/api`. DI throws if either password fails `IsSecret`.

YoPips C++ copies the same trio (`CMakeLists.txt` L56–66) and walks `GroupTotal` + `GroupNext`. Prop C# prefers the request RPC first (`GroupRequestArray("*")` L155), then falls back to the same total/next walk. Both are **all groups**, no hardcoded group list.

### 3.2 Prior live census (not re-probed this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK (HTTP proxy) | **8** | **6512** | **1506** |
| StarwaveFX | OK (direct) | **10** | **1948** | **478** |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts), re-summed this slot: `contest\yo-1step` (2) + `contest\yo-2step` (179) + `contest\yo-instant` (4) + `contest\yo-payp` (5) + `demo\yo-1step` (4) + `demo\yo-2step` (6295) + `demo\yo-instant` (0) + `demo\yo-payp` (23) = **6512**.

Starwave groups, re-summed this slot: `Starwave\cent\FX1\grp1` (11) + `grp2` (4) + `Starwave\demo\FX2\grp1` (170) + `grp2` (1735) + `Starwave\real\FX3\grp1` (22) + `grp2` (0) + `grp3` (0) + `grp4` (4) + `grp5` (0) + `Starwave\real\FX3\LP` (2) = **1948**.

`GetAccountsAsync(null)` is what produced those 8460 rows. This is **all manager users**, not a 200-row dashboard slice.

Residual if someone runs `apps/mt5-worker` after a TFM fix: `Worker.cs` L31 still scores only hardcoded `10001, 10002, 10003, 99001`. The **API** hosted ingest does **not** use those dummy logins. Worker restore is currently NU1201 anyway.

---

## 4. Copy to cTrader must not send live orders (no loss)

Measured product path — **no reconnect, no send** this slot:

| Gate | Location | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService` L16 | **`const false`** |
| `VenueReconciled` | `CopyTradingService` L15 | **`const false`** |
| Persisted `AllowFixSend` | `CopyTradingService` L192 | **hardcoded `false`** even if `Evaluate` would approve |
| Live-send branch | L198 | requires LIVE + NOS + reconciled — **unreachable** |
| FIX outbound MsgType (product session) | `CTraderFixSession.BuildLogon` L96 | **only `(35, "A")`** |
| Product hop `35=D` / NewOrderSingle builder | `CTraderFixSession.cs` (135 lines) | **0 hits** |
| Product `*.cs` / `*.json` / `*.csproj` literal `35=D` | grep this slot | **0** |
| Socket lifetime | `TryLogonAsync` | TCP+TLS disposed after logon reply |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default | **`false`** |
| Committed `appsettings.json` | `FeatureFlags.LiveCopyEnabled` | **`false`** (dead `CSERVER`+5201/5202 JSON is unbound) |
| Hosted FIX | `CTraderFixLogonHostedService` L48–58 | QUOTE 5211 + TRADE 5212 **logon only**; log line says “NewOrderSingle still unimplemented” |
| DI `RealCopyEnabled` | `DependencyInjection` L41 | binds env `REAL_COPY_EXECUTION_ENABLED` (lab `.env` L73 **`true`**) |
| API settings FEATURE | `Program.cs` L77 | literal **`true`** (pipeline ON, send still off) |

Residual **off-hop** demo helper: `CTraderFixDemoTestTrade` can `Build("D")` (L139 / L163 / L197) but `SendAsync` L43–59 refuses unless host starts with `demo-`, sender starts with `demo.`, and account is **not** live `1369850`. `DemoFixTestTrade` is a standalone `net8.0` tool; API / copy hosted services do **not** call it.

YoPips `src` has **0** cTrader FIX senders (C++ trio copy only).

**`SAFE_BY_ABSENCE`:** even with `REAL_COPY_EXECUTION_ENABLED=true` bound into `LiveRuntimeStatus`, there is no product hop that writes `35=D`. Capital cannot be lost by copy on this tree.

---

## 5. What not to do from this slot

1. Do **not** retarget `TraderIntelligence.Api.csproj` again. It is already `net8.0-windows` + x64.
2. Do **not** add `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` just to “make LoadLibrary work.” RID is unset and not required.
3. Do **not** copy `MT5APIManager64.dll` into the Linux compose image.
4. Do **not** implement `35=D` / NewOrderSingle here. Architecture gates (§68 / §70) are still not 19/19 + 14/14.
5. Do **not** run `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.exe` (stale leftover, no trio).
6. Remaining TFM work is **`mt5-worker` / `fix-worker` / Integration** → `net8.0-windows` + x64 (same as API), not the API.

---

## 6. Sources (absolute)

- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\obj\project.nuget.cache`
- `D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.csproj.FileListAbsolute.txt`
- `D:\Prop\apps\api\bin\Debug\net8.0-windows\` (trio + `bases/` 2027/9904)
- `D:\Prop\apps\api\bin\Debug\net8.0\` (stale leftover, no trio)
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` + `obj\project.nuget.cache`
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` + `obj\project.nuget.cache`
- `D:\Prop\tests\Integration\obj\project.nuget.cache`
- `D:\Prop\tools\LiveBrokerProbe\obj\project.nuget.cache`
- `D:\Prop\Directory.Build.props`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt`
- `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json`
- `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\RefNet8.run.txt`
- `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\RefNet8Win.run.txt`
- `D:\Prop\reports\swarm\20260818\R021_dll_load.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

---

## 7. Slot-193 delta vs slot 173

Same tree, independently re-read. **No product drift** on the assigned csproj (still L18–19 `net8.0-windows` + x64), restore hash still `6bXcKBewNHo=`, trio + `bases/` still present, worker NU1201 hashes unchanged (`BMefpWc6ygQ=` / `3nFO35Ml2jE=` / `eWT2aWaUBSk=`), census re-sum still 18/8460. Extra facts this slot confirmed that 173 also had: compose Linux image cannot `LoadLibrary`; leftover `net8.0` API `deps.json` has 0 MetaQuotes; demo helper `Build("D")` remains off-hop.

**PASS** on the assigned API TFM question. Residual worker graph still FAIL. Copy hop **`SAFE_BY_ABSENCE`**. Risk to capital: **NONE**.
