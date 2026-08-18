# W500_RESEARCH_173 — `Api.csproj` TargetFramework: `net8.0` without windows/x64 vs `MT5APIManager64`

| Field | Value |
|---|---|
| Slot | **173** |
| Agent | W500_RESEARCH_173 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_173.md` |
| Assigned | Check `Api.csproj` `TargetFramework`. Claim: **`net8.0` without windows/x64 cannot load `MT5APIManager64`**. Goal overlay: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only product-adjacent write (plus INDEX / SWARM_LOG catalog). |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Password / proxy / FIX keys named only. No tag 554, no proxy auth, no manager passwords, no login dump. Flag booleans only. |
| Live attach this pass | **No.** No Manager reconnect. No FIX send. Census is prior same-day JSON (`2026-08-18T08:42:16Z`). |
| Method | Independent `read_file` + `grep` + `list_dir` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current `TraderIntelligence.Api.csproj`, nuget dgspec/cache, GeneratedMSBuildEditorConfig, FileListAbsolute, Debug `deps.json` + `runtimeconfig.json` + output dirs (including `bases/`), sibling host csproj + restore caches, `src/Mt5` connector, ingest/DI/FIX session, vendor `MT5APIManager.h` `LoadLibraryW`, R021 PE inspect JSON, live census JSON, YoPips CMake + `GetAllGroups`. **No product edit.** |
| Same-topic siblings (do not treat as this file) | `W500_RESEARCH_13.md`, `W500_RESEARCH_33.md`, `W500_RESEARCH_53.md`, `W500_RESEARCH_73.md`, `W500_RESEARCH_93.md`, `W500_RESEARCH_113.md`, `W500_RESEARCH_133.md`, `W500_RESEARCH_153.md` — same question, earlier slots. This file is a **slot-173 re-measure**, not a copy. Slot 153 claimed `W500_RESEARCH_133.md` was missing; **it is on disk** (`D:\Prop\reports\swarm\20260818\W500_RESEARCH_133.md`). |
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
| Stale `bin\Debug\net8.0\` (pre-retarget) has the trio | **No** — directory listing has **zero** `MT5API*` / `MetaQuotes*`; that folder’s `deps.json` has **0** MetaQuotes strings | leftover; do not run |
| Isolated `net8.0` x64 can `SMTManagerAPIFactory.Initialize` on Windows when native DLL is beside exe | **Yes (R021)** `MT_RET_OK` (0) | PE-load **not** gated by `-windows` |
| Isolated `net8.0` **x86** can map this AMD64 PE | **No** — PE machine `0x8664`; CS8012; no x86 runtime (`0x80008083`) | **x64 required** |
| Linux / compose `mcr.microsoft.com/dotnet/sdk:8.0` can `LoadLibraryW` this PE | **No** | **Windows OS required** |
| Portable `net8.0` **host** can `ProjectReference` `src/Mt5` (`net8.0-windows`) | **No — NU1201** (still true for `mt5-worker`, `fix-worker`, Integration tests) | product-graph gate |
| Remaining hosts still `net8.0` | **`mt5-worker`**, **`fix-worker`**, **`tests/Integration`** — restore `success: false` | `EXISTS_NEEDS_REFACTOR` |
| `mt5-worker` output has `MT5APIManager64.dll` | **No** — `bin\Debug\net8.0\` has `TraderIntelligence.Mt5.dll` only; **zero** vendor trio; `deps.json` 0 MetaQuotes | cannot `Initialize` |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | **Yes, path + prior live census** — Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** (re-summed this slot; not re-attached) | catalog-only |
| Copy to cTrader may send live orders | **No** — `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `CTraderFixSession` emits only `35=A`; product hop `35=D` = **0** | **`SAFE_BY_ABSENCE`** |
| Lab `REAL_COPY_EXECUTION_ENABLED` | **`.env` L73 = `true`**; DI binds it (`DependencyInjection` L41). Flag is **armed**. Sender is still **absent**. | flag true ≠ send |
| `/api/settings` `FEATURE_COPY_TRADING_ENABLED` | **literal `true`** (`Program.cs` L77) | pipeline ON, send still off |
| Residual standalone demo tool | `CTraderFixDemoTestTrade.Build("D")` exists; **not** called by API/copy hosted services; demo-host gate | not the copy hop |
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

Path: `D:\Prop\apps\api\TraderIntelligence.Api.csproj`

```17:22:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

It **ProjectReferences** Domain + Application + Infrastructure + **Mt5** + Fix.CTrader (`csproj` L3–8). That is a **Windows Manager host**, not a Linux-portable API.

`D:\Prop\Directory.Build.props` still does **not** set TFM, RID, or PlatformTarget (LangVersion / Nullable / ImplicitUsings / TreatWarningsAsErrors=false / Deterministic only). The API pin is **local to this csproj**. Product `*.csproj` + `*.props` have **0** `RuntimeIdentifier` / `EnableWindowsTargeting` / `win-x64` pins. `win-x64` is **not** the load bit; `PlatformTarget=x64` + Windows OS + native sibling are.

### 1.1 Restore graph agrees with the csproj

`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`:

| Field | Value |
|---|---|
| `originalTargetFrameworks` | **`net8.0-windows`** |
| Restore TFM / alias | **`net8.0-windows7.0`** / `net8.0-windows` |
| ProjectReferences | Application, Domain, Fix.CTrader, **Infrastructure**, **Mt5** |
| RID graph | `C:\Program Files\dotnet\sdk\8.0.424\PortableRuntimeIdentifierGraph.json` |

`D:\Prop\apps\api\obj\project.nuget.cache`: **`"success": true`**, `"logs": []`, `dgSpecHash` `6bXcKBewNHo=`.

`D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`:

```text
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
build_property.UsingMicrosoftNETSdkWeb = true
build_property._SupportedPlatformList = Linux,macOS,Windows
```

`_SupportedPlatformList` is the default Web SDK list. It does **not** mean this exe can run on Linux.

### 1.2 Output that can actually load Manager64

`FileListAbsolute` L1–3 + directory listing of `D:\Prop\apps\api\bin\Debug\net8.0-windows\`:

| File / dir | Present? |
|---|---|
| `MT5APIManager64.dll` | **Yes** (FileListAbsolute L3; `read_file` sees a binary PE) |
| `MetaQuotes.MT5ManagerAPI64.dll` | **Yes** (L2) |
| `MetaQuotes.MT5CommonAPI64.dll` | **Yes** (L1) |
| `TraderIntelligence.Api.exe` | **Yes** (L6) |
| Manager `bases/AchieverGlobalMarkets-Server/` (`users-2027.dat`, `positions-2027.dat`, …) | **Yes** |
| Manager `bases/StarwaveFX-Server/` (`users-9904.dat`, `positions-9904.dat`, …) | **Yes** |

Those `bases/` cache dirs are written by the native Manager after `LoadLibraryW` + `Connect`. They prove a prior Windows API process **did** map `MT5APIManager64.dll` against both owned brokers (manager logins **2027** / **9904**). This slot did **not** re-attach.

`TraderIntelligence.Api.deps.json` (Windows output) records:

```text
MetaQuotes.MT5ManagerAPI64/5.5570.0.0  fileVersion 5.0.0.5584  type=reference
MetaQuotes.MT5CommonAPI64/5.5570.0.0   fileVersion 5.0.0.5584  type=reference
```

`runtimeconfig.json` `tfm` is still `"net8.0"` with `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` only — **normal** for a `net8.0-windows` Web SDK project that does not pull WindowsDesktop. That string does **not** mean the project is portable `net8.0`.

Stale leftover `D:\Prop\apps\api\bin\Debug\net8.0\`: **zero** `MT5API*` / `MetaQuotes*` files; **zero** `MetaQuotes` strings in that folder’s `deps.json`. The leftover folder **does** contain `TraderIntelligence.Mt5.dll` — a stale IL copy that cannot `Initialize` without the native sibling. Do not run that exe.

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

Once the host TFM matches, MSBuild flows the trio into `net8.0-windows`. That is why the current API output has the files and the stale `net8.0` output does not.

`LiveMt5Registration.CreateConnectors` sets `NativeDllDirectory = Path.GetFullPath(AppContext.BaseDirectory)` — the host output dir. `NativeMt5BrokerConnector.ConnectCore` then:

```66:70:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

C++ copy contract is the same trio, **WIN32 only** (`D:\Prop\mt5-sdk\CMakeLists.txt` L114–128 `mt5sdk_copy_runtime_dlls` returns early `if(NOT WIN32)`). YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` L55–66 POST_BUILDs the same three files beside the native exe. That backend is **MSVC Win32**, not `net8.0`.

---

## 2. What “cannot load `MT5APIManager64`” actually means

### 2.1 The file (PE, not folklore)

Measured in `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json` from `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`:

| File | Bytes | Machine | Kind | SHA-256 |
|---|---:|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | **`0x8664` AMD64** | **native** (`HasClr=false`) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | mixed-mode C++/CLI (Framework 4.7.2, CorFlags `NATIVE_ENTRYPOINT`) | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | same mixed-mode family | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

`MT5APIManager64.dll` imports `KERNEL32.dll`, `WS2_32.dll`, `ADVAPI32.dll`, `CRYPT32.dll`, `USER32.dll`, `SHELL32.dll`, `ole32.dll`, `OLEAUT32.dll`, `IPHLPAPI.DLL` — **Win32 only**. Not ELF. Not AnyCPU. Not `net8.0` IL. Subsystem `WINDOWS_GUI`. Magic `PE32+`.

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

Loader is **`LoadLibraryW`**. Missing file → `MT_RET_ERR_NOTFOUND` (13). There is no `dlopen` branch. CMake only copies the trio `if(WIN32)`.

Do **not** copy `MetaQuotes.MT5WebAPI.dll` next to this process: PE32 **i386** (`0x014C`), different API (`E5611F63…`).

### 2.2 Isolated TFM experiment (R021) — do not over-claim `-windows`

R021 built throwaway `net8.0` and `net8.0-windows` x64 projects that `<Reference>` the vendor wrappers directly (not via `src/Mt5`). Host: Windows 10.0.26200 x64; SDK **8.0.424**; runtime **8.0.30**.

| Host TFM | PlatformTarget | Compile | `Initialize` with native sibling | `Initialize` without native sibling |
|---|---|---|---|---|
| `net8.0` | x64 | PASS (0/0) | **`MT_RET_OK` (0)** | `MT_RET_ERR_NOTFOUND` (13) |
| `net8.0-windows` | x64 | PASS | **`MT_RET_OK`** | same |
| `net8.0` | AnyCPU (64-bit OS) | PASS | **OK on this box** | — |
| `net8.0` | x86 | CS8012; no x86 runtime (`0x80008083`) | **cannot map AMD64** | — |

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

`mt5-worker` GeneratedMSBuildEditorConfig still says `build_property.TargetFramework = net8.0`. Its `bin\Debug\net8.0\` listing has `TraderIntelligence.Mt5.dll` but **zero** `MT5APIManager64.dll` / `MetaQuotes.MT5*`. Its `deps.json` has **0** MetaQuotes strings. That stale worker output cannot `Initialize`.

Portable libraries that **should** stay `net8.0` (no Manager PE): Domain, Application, Fix.CTrader. That split is correct.

---

## 3. ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Fetch path (no cap)

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51):

1. `GetGroupsAsync` → `GroupRequestArray("*")`, fallback `GroupTotal`/`GroupNext`.
2. `GetAccountsAsync(null, …)` → walks **every** group, then `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`.

There is **no** `Take(` in ingest (0 hits in that file). Residual `Take(200)` is only dashboard slices (`GET /api/trades`, `GET /api/copy/intents`), not catalog.

`LiveIngestHostedService` iterates `registry.All()` (both Native connectors), catalog then deals then `ListLoginsWithDealsAsync` scoring. Dummy `FakeMt5BrokerConnector` / `DemoSeeder` are **not** referenced from `apps/api`. DI throws if either password fails `IsSecret`.

`GetTradersAsync` (`EfDashboardQueries.cs` L85–128) is **account-driven** left-join of scores — every `Mt5Accounts` row is returned. Not scores-only.

YoPips C++ `MT5Manager::GetAllGroups` (`mt5_manager.cpp` L962–981) walks `GroupTotal` + `GroupNext` (pump cache). Prop C# prefers the request RPC first (`GroupRequestArray("*")`), then falls back to the same total/next walk. Both are **all groups**, no hardcoded group list.

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

Residual if someone runs `apps/mt5-worker` after a TFM fix: `Worker.cs` L31 still scores only hardcoded `10001, 10002, 10003, 99001`. The **API** hosted ingest does **not** use those dummy logins.

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
| Socket lifetime | `TryLogonAsync` | TCP+TLS disposed after logon reply |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default | **`false`** |
| Committed `appsettings.json` | `FeatureFlags.LiveCopyEnabled` | **`false`** (dead `CSERVER`+5201/5202 JSON is unbound) |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED` | **`true`** (boolean only; no secret) |
| DI bind | `DependencyInjection.cs` L41 | copies env `true` onto `LiveRuntimeStatus.RealCopyEnabled` |
| `/api/settings` | `Program.cs` L76 | exposes `runtime.RealCopyEnabled` (can show true) |
| `CopyTradingHostedService` | L30 | logs “Live NewOrderSingle still blocked” |
| `CTraderFixLogonHostedService` | L69 | logs “NewOrderSingle still unimplemented”; does **not** re-pin flag false |

`RiskEngine.Evaluate` can set `AllowFixSend` only when `RealExecutionEnabled && Reconciled && VenueHealthy && KillSwitch==None`. Copy persist then **overwrites** that to `false`. Do **not** implement a sender in this slot.

Standalone residual (not the copy hop): `CTraderFixDemoTestTrade.SendAsync` can emit `Build("D", …)` (L126 / L157). It is called only from `tools/DemoFixTestTrade/Program.cs`. Gate at L42–56 refuses unless host starts with `demo-` and SenderCompId starts with `demo.`. API / `CopyTradingService` / `CTraderFixLogonHostedService` do **not** call it.

`docker-compose.yml` L16–30 runs the API as Linux `mcr.microsoft.com/dotnet/sdk:8.0`. That container **cannot** load the PE and **cannot** restore this TFM. Compose comment L30: “Native MT5 Manager DLL workers stay on Windows hosts.”

---

## 5. TFM matrix (remeasured)

| Project | Path | TargetFramework | PlatformTarget | Manager trio copy |
|---|---|---|---|---|
| **Api** | `apps/api/TraderIntelligence.Api.csproj` | **`net8.0-windows`** | **x64** | via Mt5 ProjectReference |
| Infrastructure | `src/Infrastructure/...` | `net8.0-windows` | x64 | via Mt5 |
| Mt5 | `src/Mt5/...` | `net8.0-windows` | x64 | **Yes** (`CopyToOutputDirectory`) |
| LiveBrokerProbe | `tools/LiveBrokerProbe/...` | `net8.0-windows` | x64 | via Mt5 |
| Domain | `src/Domain/...` | `net8.0` | (default) | no — correct |
| Application | `src/Application/...` | `net8.0` | (default) | no — correct |
| Fix.CTrader | `src/Fix.CTrader/...` | `net8.0` | (default) | no — correct |
| Mt5Worker | `apps/mt5-worker/...` | **`net8.0`** | unset | **NU1201**; stale `net8.0` bin has no trio |
| FixWorker | `apps/fix-worker/...` | **`net8.0`** | unset | **NU1201** (refs Infrastructure) |
| Unit tests | `tests/Unit/...` | `net8.0` | unset | does **not** ref Mt5 — OK |
| Integration tests | `tests/Integration/...` | **`net8.0`** | unset | **NU1201** vs Infra+Mt5 |
| DemoFixTestTrade | `tools/DemoFixTestTrade/...` | `net8.0` | unset | no Manager; FIX demo tool only |
| Directory.Build.props | repo root | **no TFM** | no | — |

---

## 6. What not to do

1. Do **not** retarget `Api.csproj` again. It is already `net8.0-windows` + x64.
2. Do **not** claim “`net8.0` cannot LoadLibrary.” Isolated `net8.0` x64 **can**. Product `net8.0` hosts **cannot restore** `src/Mt5`.
3. Do **not** run `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.exe`. Trio missing.
4. Do **not** copy `MT5APIManager64.dll` into the Linux compose image.
5. Do **not** implement `35=D` / NewOrderSingle on the product hop. Goal is catalog + no loss.
6. Do **not** treat `.env` `REAL_COPY_EXECUTION_ENABLED=true` as a send license.
7. Do **not** print manager / FIX / proxy secrets. This report does not.
8. Do **not** run `tools/DemoFixTestTrade` against a live Pepperstone account.

---

## 7. Residual (honest, not this slot’s job)

| Residual | Why it matters |
|---|---|
| `mt5-worker` / `fix-worker` / Integration still `net8.0` | NU1201; cannot be the live Manager host |
| Stale `apps/api/bin/Debug/net8.0/` | leftover pre-retarget output |
| `.env` REAL_COPY=`true` now bound by DI | dashboard can show armed; sender still absent |
| `mt5-worker` Worker scores 4 dummy logins | irrelevant until worker TFM is fixed; API ingest is the live path |
| No `RuntimeIdentifier=win-x64` | not required for LoadLibrary; optional pin for publish |
| `CTraderFixDemoTestTrade` can emit `D` on demo hosts | standalone tool; not the copy hop |
| Compose Linux `dotnet run` of this TFM | cannot restore / cannot LoadLibrary |

---

## 8. Sources (absolute)

- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`
- `D:\Prop\apps\api\obj\project.nuget.cache`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.csproj.FileListAbsolute.txt`
- `D:\Prop\apps\api\bin\Debug\net8.0-windows\` (+ `bases\`)
- `D:\Prop\apps\api\bin\Debug\net8.0\`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\mt5-worker\obj\project.nuget.cache`
- `D:\Prop\apps\mt5-worker\bin\Debug\net8.0\`
- `D:\Prop\apps\fix-worker\obj\project.nuget.cache`
- `D:\Prop\tests\Integration\obj\project.nuget.cache`
- `D:\Prop\tools\LiveBrokerProbe\LiveBrokerProbe.csproj`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\Directory.Build.props`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json`
- `D:\Prop\reports\swarm\20260818\R021_dll_load.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\W500_RESEARCH_133.md`
- `D:\Prop\reports\swarm\20260818\W500_RESEARCH_153.md`

**Slot 173 complete. Product tree untouched. No live order sent. Risk to capital: NONE.**
