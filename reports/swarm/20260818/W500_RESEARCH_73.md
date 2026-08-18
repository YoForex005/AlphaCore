# W500_RESEARCH_73 — `Api.csproj` TargetFramework vs `MT5APIManager64` (slot 73)

| Field | Value |
|---|---|
| Slot | **73** |
| Agent | W500_RESEARCH_73 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_73.md` |
| Assigned | Check `Api.csproj` `TargetFramework`. Claim: **`net8.0` without windows/x64 cannot load `MT5APIManager64`**. Goal overlay: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Password / proxy / FIX keys named only. No tag 554 values, no proxy auth, no manager passwords, no login dump. |
| Method | Independent `read_file` + `grep` + `list_dir` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current `TraderIntelligence.Api.csproj`, nuget dgspec/cache, Debug `FileListAbsolute` + `deps.json` + `runtimeconfig.json` + output dirs, `src/Mt5` connector, ingest/DI/FIX session, vendor `MT5APIManager.h` `LoadLibraryW`, R021 PE inspect JSON, live census JSON. **No Manager reconnect. No FIX send. No product edit.** |
| Same-topic siblings (do not treat as this file) | `W500_RESEARCH_13.md`, `W500_RESEARCH_33.md`, `W500_RESEARCH_53.md` — same question, earlier slots. This file is a **slot-73 re-measure**, not a copy. |
| Stale (do not cite as current API TFM) | `A012_worker_tfm.md`, `A002_api_dummy_path.md` (quote API as portable `net8.0`). `W500_SLICE_5.md` (API NU1201). `A105` (`src/Mt5` portable + Fake only). R021 NU1201 row for `apps/api` is **historical**. |

**Honesty rule:** `-windows` on the TFM is **not** what `LoadLibraryW` checks. `LoadLibraryW` checks **OS + PE machine + file present**. Isolated `net8.0` x64 on Windows **can** initialize the factory (R021). In **this product graph**, a portable `net8.0` **host** cannot `ProjectReference` `net8.0-windows` `src/Mt5` / `Infrastructure` (**NU1201**) and therefore cannot stage the trio beside the exe. Do not collapse those two facts. Do not claim “EX5 decompiled,” “95% live copy,” or a fresh 2026-08-18 Manager attach from this slot.

---

## 0. Verdict (binding)

**PASS — current `TraderIntelligence.Api.csproj` is already `net8.0-windows` + `PlatformTarget` x64; Manager trio is in the Windows output; catalog path can load `MT5APIManager64`; copy stays off (`SAFE_BY_ABSENCE`). Residual FAIL: `mt5-worker` / `fix-worker` / Integration still `net8.0` + NU1201.**

There is **no** file named `Api.csproj`. The assigned host is `D:\Prop\apps\api\TraderIntelligence.Api.csproj`.

The folklore sentence “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is **half-true**. Split it:

| Claim | Measured this slot | Class |
|---|---|---|
| Current API TFM | **`net8.0-windows`** + **`PlatformTarget` x64** (`csproj` L18–19) | `EXISTS_AND_GOOD` |
| API restore against `src/Mt5` + `Infrastructure` | **`success: true`**, `"logs": []` (`obj/project.nuget.cache`) | `EXISTS_AND_GOOD` |
| Restore TFM alias | `originalTargetFrameworks` = **`net8.0-windows`**; framework id **`net8.0-windows7.0`** | `EXISTS_AND_GOOD` |
| Trio next to current API exe | **Yes** — `bin\Debug\net8.0-windows\{MT5APIManager64, MetaQuotes.MT5ManagerAPI64, MetaQuotes.MT5CommonAPI64}.dll` | `EXISTS_AND_GOOD` |
| Stale `bin\Debug\net8.0\` (pre-retarget) has the trio | **No** — that folder’s `deps.json` has **0** `MetaQuotes` / `MT5APIManager` tokens | leftover; do not run |
| Isolated `net8.0` x64 can `SMTManagerAPIFactory.Initialize` on Windows when native DLL is beside exe | **Yes (R021)** `MT_RET_OK` (0) | PE-load **not** gated by `-windows` |
| Isolated `net8.0` **x86** can map this AMD64 PE | **No** — PE machine `0x8664`; CS8012; no x86 runtime (`0x80008083`) | **x64 required** |
| Linux / compose `mcr.microsoft.com/dotnet/sdk:8.0` can `LoadLibraryW` this PE | **No** | **Windows OS required** |
| Portable `net8.0` **host** can `ProjectReference` `src/Mt5` (`net8.0-windows`) | **No — NU1201** (still true for `mt5-worker`, `fix-worker`, Integration tests) | product-graph gate |
| Remaining hosts still `net8.0` | **`mt5-worker`**, **`fix-worker`**, **`tests/Integration`** — restore `success: false` | `EXISTS_NEEDS_REFACTOR` |
| `mt5-worker` output has `MT5APIManager64.dll` | **No** — `bin\Debug\net8.0\` has `TraderIntelligence.Mt5.dll` only; **zero** vendor trio | cannot load |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | **Yes, path + prior live census** — Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** | catalog-only |
| Copy to cTrader may send live orders | **No** — `RealCopyEnabled` forced `false`; `CTraderFixSession` emits only `35=A`; product `35=D` = **0** | **`SAFE_BY_ABSENCE`** |
| Risk to capital from this process | **None** | Manager read + FIX Logon only |

One-line:

```text
Api host = net8.0-windows + x64 (restore OK; trio in net8.0-windows output; bases/ proves prior LoadLibrary).
net8.0 host cannot ProjectReference Mt5 (NU1201). Isolated net8.0 x64 CAN LoadLibrary on Windows (R021).
x86 / Linux cannot. ALL groups/traders = GroupRequestArray("*") + UserRequestArray (18/8460 prior).
No 35=D. RealCopyEnabled=false. No capital at risk.
```

A012 / A002 “API is still `net8.0`” is **stale**. Do not retarget the API again. Do **not** add a `35=D` sender in this task.

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

No `<RuntimeIdentifier>`. Bitness is pinned by `PlatformTarget` x64, not by `win-x64` RID.

`D:\Prop\Directory.Build.props` still does **not** set TFM, RID, or `PlatformTarget` (LangVersion / Nullable / ImplicitUsings / TreatWarningsAsErrors / Deterministic only). The API pin is **local to this csproj**.

### 1.1 Restore graph agrees with the csproj

`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`:

| Field | Value |
|---|---|
| `originalTargetFrameworks` | **`net8.0-windows`** |
| Restore TFM / alias | **`net8.0-windows7.0`** / `net8.0-windows` |
| ProjectReferences | Application, Domain, Fix.CTrader, **Infrastructure**, **Mt5** |
| `runtimeIdentifierGraphPath` | `...\sdk\8.0.424\PortableRuntimeIdentifierGraph.json` (no publish RID) |

`D:\Prop\apps\api\obj\project.nuget.cache`: **`"success": true`**, `"logs": []`, `projectFilePath` = `D:\\Prop\\apps\\api\\TraderIntelligence.Api.csproj`.

`D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`:

```text
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
build_property.UsingMicrosoftNETSdkWeb = true
```

`_SupportedPlatformList = Linux,macOS,Windows` is the **SDK default** for a Web project. It does **not** make this process ELF-safe. The PE still requires Windows + AMD64.

### 1.2 Output that can actually load Manager64

`FileListAbsolute` L1–3 + `list_dir` of `D:\Prop\apps\api\bin\Debug\net8.0-windows\`:

| File | Present? |
|---|---|
| `MT5APIManager64.dll` | **Yes** (FileListAbsolute line 3) |
| `MetaQuotes.MT5ManagerAPI64.dll` | **Yes** (line 2) |
| `MetaQuotes.MT5CommonAPI64.dll` | **Yes** (line 1) |
| `TraderIntelligence.Api.exe` | **Yes** |
| Manager `bases/` cache dirs `AchieverGlobalMarkets-Server/` + `StarwaveFX-Server/` | **Yes** (process has already mapped the native DLL) |
| `bases/AchieverGlobalMarkets-Server/users/users-2027.dat` | **Yes** (manager login id only; password not printed) |
| `bases/StarwaveFX-Server/users/users-9904.dat` | **Yes** (same) |

`TraderIntelligence.Api.deps.json` (Windows output) records:

```text
MetaQuotes.MT5ManagerAPI64/5.5570.0.0  fileVersion 5.0.0.5584
MetaQuotes.MT5CommonAPI64/5.5570.0.0   fileVersion 5.0.0.5584
```

Native `MT5APIManager64.dll` is copied as content, **not** a deps.json assembly (expected: it has `HasClr=false`).

`runtimeconfig.json` `tfm` is still `"net8.0"` with `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` only — **normal** for a `net8.0-windows` Web SDK project that does not pull WindowsDesktop. That string does **not** mean the project is portable `net8.0`.

Stale leftover `D:\Prop\apps\api\bin\Debug\net8.0\`: **zero** MetaQuotes / `MT5APIManager64` files; **zero** `MetaQuotes` strings in that folder’s `deps.json`. It still contains `TraderIntelligence.Mt5.dll` from an earlier copy. Running that old exe cannot `Initialize`. Do not use it.

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

---

## 2. What “cannot load `MT5APIManager64`” actually means

### 2.1 The file (PE, not folklore)

Measured in `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json` from `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`:

| File | Bytes | Machine | Kind | SHA-256 |
|---|---:|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | **`0x8664` AMD64** | **native** (`HasClr=false`) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | mixed-mode C++/CLI (Framework 4.7.2, CorFlags `NATIVE_ENTRYPOINT`) | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | same mixed-mode family | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

`MT5APIManager64.dll` imports `KERNEL32.dll`, `WS2_32.dll`, `ADVAPI32.dll`, `CRYPT32.dll`, `USER32.dll`, `SHELL32.dll`, `ole32.dll`, `OLEAUT32.dll`, `IPHLPAPI.DLL` — **Win32 only**. Not ELF. Not AnyCPU. Not `net8.0` IL. Magic `0x020B` PE32+. Subsystem `WINDOWS_GUI`. ExportCount 5.

Vendor factory (`MT5APIManager.h` 1719–1744):

```1719:1726:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
inline MTAPIRES CMTManagerAPIFactory::Initialize(LPCWSTR dll_path/*=NULL*/)
  {
   wchar_t  path[MAX_PATH]={};
   if(!FindLibrary(dll_path,path,_countof(path)-1))
      return(MT_RET_ERR_NOTFOUND);
   if((m_hmodule=::LoadLibraryW(path))==NULL)
      return(MT_RET_ERR_NOTFOUND);
```

Loader is **`LoadLibraryW`**. Missing file → `MT_RET_ERR_NOTFOUND` (13). There is no `dlopen` branch. C++ wrapper `#include <Windows.h>` (`mt5-sdk/src/core/mt5_manager.h`). CMake only compiles local Manager sources `if(WIN32)`.

Do **not** copy `MetaQuotes.MT5WebAPI.dll` next to this process: PE32 **i386** (`0x014C`), different API.

### 2.2 Isolated TFM experiment (R021) — do not over-claim `-windows`

R021 built throwaway `net8.0` and `net8.0-windows` x64 projects that `<Reference>` the vendor wrappers directly (not via `src/Mt5`). Host: Windows 10.0.26200 x64; SDK **8.0.424**; runtime **Microsoft.NETCore.App 8.0.30**.

Isolated `RefNet8.csproj` is **`net8.0` + `PlatformTarget` x64** and copies `MT5APIManager64.dll`. Isolated `RefNet8Win.csproj` is **`net8.0-windows` + x64**. Isolated `RefNet8x86.csproj` is **`net8.0` + `PlatformTarget` x86**.

| Host TFM | PlatformTarget | Compile | `Initialize` with native sibling | `Initialize` without native sibling |
|---|---|---|---|---|
| `net8.0` | x64 | PASS | **`MT_RET_OK` (0)** | `MT_RET_ERR_NOTFOUND` (13) |
| `net8.0-windows` | x64 | PASS | **`MT_RET_OK`** | same |
| `net8.0` | AnyCPU (64-bit OS) | PASS | **OK on this box** | — |
| `net8.0` | x86 | CS8012; no x86 runtime (`0x80008083`) | **cannot map AMD64** | — |

R021 also: wrapper `Assembly.LoadFrom` OK; collectible ALC → `BadImageFormatException` (mixed-mode); `SMTManagerAPIFactory.ManagerAPIVersion` = **5570**; date **30 Jan 2026**.

So: **missing `-windows` is not the PE-load blocker.** Missing **Windows OS**, missing **AMD64 process**, or missing **`MT5APIManager64.dll` beside `Initialize`** are.

### 2.3 Product-graph gate (why API *did* need `-windows` + x64)

`src/Mt5` and `src/Infrastructure` are `net8.0-windows` + x64. NuGet will **not** let a `net8.0` exe `ProjectReference` a `net8.0-windows7.0` library.

**Still broken (measured restore caches, this pass):**

| Project | TFM now | Restore |
|---|---|---|
| `apps/api` | **`net8.0-windows` + x64** | **`success: true`** |
| `tools/LiveBrokerProbe` | **`net8.0-windows` + x64** | probe that produced 18/8460 |
| `apps/mt5-worker` | **`net8.0`** (no PlatformTarget) | **`success: false`** NU1201 vs Infrastructure **and** Mt5 |
| `apps/fix-worker` | **`net8.0`** (no PlatformTarget) | **`success: false`** NU1201 vs Infrastructure |
| `tests/Integration` | **`net8.0`** | **`success: false`** NU1201 vs Infrastructure **and** Mt5 |
| `tests/Unit` | **`net8.0`** | portable refs only (Domain/Application/Fix.CTrader) — **does not** load Manager64 |

`mt5-worker` dgspec `originalTargetFrameworks` = **`net8.0`**. Cache logs (this slot):

```text
NU1201: Project TraderIntelligence.Infrastructure is not compatible with net8.0
        (.NETCoreApp,Version=v8.0). Project supports: net8.0-windows7.0
NU1201: Project TraderIntelligence.Mt5 is not compatible with net8.0
        (.NETCoreApp,Version=v8.0). Project supports: net8.0-windows7.0
```

`mt5-worker` is the architecture-§5 owner of `LoadLibrary`. It **cannot restore today**. Stale `D:\Prop\apps\mt5-worker\bin\Debug\net8.0\` listing (this slot): `TraderIntelligence.Mt5.dll` is present; **`MT5APIManager64.dll` / both MetaQuotes wrappers are absent**. Launching that leftover exe cannot `Initialize`.

The API host currently **is** the process that loads Manager64 (`bases/` cache dirs prove it). That contradicts “Linux API, Windows worker” but matches the files on disk.

`fix-worker` does **not** need Manager64. Shared `AddTraderIntelligence` is a leak; TFM should stay portable **if** it stops referencing `Infrastructure`.

Domain / Application / Fix.CTrader remain portable `net8.0` — correct; they do not bind the PE.

`docker-compose.yml` `api` service uses `mcr.microsoft.com/dotnet/sdk:8.0` + `dotnet run --project apps/api/...`. Comment L30: “Native MT5 Manager DLL workers stay on Windows hosts.” A Linux container **cannot** `LoadLibraryW` this PE even after the Windows TFM retarget. Do not treat compose `api` as a Manager host.

---

## 3. ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Registration (both brokers, no dummy)

`DependencyInjection.AddTraderIntelligence` **throws** if real `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` are missing (`HasRealPasswords`). Dummy/Fake is not substituted.

`LiveMt5Registration.CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector` instances: `BrokerCodes.Achiever` and `BrokerCodes.StarwaveFx`. Starwave `ProxyEnabled = false` (hardcoded). Achiever proxy comes from `ACHIEVER_PROXY_*` (values not printed).

`LiveIngestHostedService` then `ConnectAsync` + `SyncCatalogAsync` for every registered connector (`registry.All()`). Fail path: log + `No dummy data will be substituted.`

`Program.cs` `/api/ops/resync` loops `ACHIEVER` then `STARWAVEFX` through the same catalog path.

### 3.2 “ALL” in code (request APIs, not a 3-name demo list)

`DealIngestionService.SyncCatalogAsync`:

1. `GetGroupsAsync` → `GetGroupsCore`
2. `GetAccountsAsync(null)` → walk **every** group name, no `Take()`

`GetGroupsCore` primary: `_manager.GroupRequestArray("*", arr)` (L155). Fallback if empty: `GroupTotal` / `GroupNext` (pump cache). Mask `*` = every group this **manager ACL** may see. Server groups outside the manager record stay invisible by design.

`ReadAccountsForGroup` primary: `UserRequestArray(gname, users)` (L223). Fallbacks: `UserGetByGroup` (pump), then `UserLogins` + `UserRequestByLogins`. Also `UserAccountRequestArray` / `UserAccountGetByGroup`. Dedup by login.

Dashboard `GetTradersAsync` is **account-driven**, not scores-only:

```99:119:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        foreach (var account in accounts)
        {
            ...
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                ...
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
```

Hosted scoring after ingest is `ListLoginsWithDealsAsync` (deals-only freshness). That is **not** a catalog hide.

### 3.3 YoPips C++ (comparison, not this host)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups` (L962–981) is **cache-only** `GroupTotal` + `GroupNext`. That wrapper does **not** call `GroupRequestArray` / `UserRequestArray`. The Prop C# connector is the request-complete path.

### 3.4 Live census already measured (not re-run this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` — probe `2026-08-18T08:42:16.8519545+00:00`. Note: “Passwords never written. Groups and manager logins only.”

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK via HTTP proxy | **8** | **6512** | 1506 |
| STARWAVEFX | OK direct | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | 1984 |

Achiever groups (account counts sum to 6512): `contest\yo-{1step,2step,instant,payp}`, `demo\yo-{1step,2step,instant,payp}`. Largest bucket `demo\yo-2step` = 6295. `demo\Maxmaster` is **absent**.

Starwave groups (account counts sum to 1948): `Starwave\cent\FX1\grp{1,2}`, `Starwave\demo\FX2\grp{1,2}`, `Starwave\real\FX3\grp{1..5}` + `LP`.

`CREDENTIALS_AND_COPY_STATUS.md`: `/api/groups` = **18**, `/api/traders` = **8460**. That is manager-ACL-visible inventory, not “every account on the broker servers.”

This slot did **not** reconnect. Counts are prior same-day measurements. Re-fetch if ACL changes; do not invent rows.

---

## 4. Copy to cTrader — no live orders (no loss)

| Gate | Measured this slot |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | Forced **`false`** in `AddTraderIntelligence` L41 (“Live NewOrderSingle is not implemented”) and again after FIX logon (`CTraderFixLogonHostedService` L68) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | Default **`false`** (L35) |
| `/api/settings` `REAL_COPY_EXECUTION_ENABLED` | Bound to `runtime.RealCopyEnabled` (false). `FEATURE_COPY_TRADING_ENABLED` hardcoded **false** |
| `CTraderFixSession` outbound tag 35 | **`"A"` Logon only** (`BuildLogon` L96). One `ssl.WriteAsync`. `using` sockets dispose before return — no keep-alive TRADE writer |
| Product `35=D` / `(35, "D")` / `MsgType = "D"` | **0** in `src/` + `apps/` + `tests/` `*.cs` / `*.json` (this grep) |
| `NewOrderSingle` tokens | Comments / logs / `MayRetryNewOrderSingle` helper **name** only — not a sender |
| Shadow path | `ShadowCopyEngine.SimulateEntry` is in-process math |
| YoPips C++ `src` used as a send path from this API | **No** |

`CTraderFixLogonHostedService` may TLS-logon QUOTE **5211** and TRADE **5212**. Logon ≠ NewOrderSingle. Snapshot copy: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

`fix-worker` L46: even if config says real copy, worker “still refuses NewOrderSingle until risk/reconciliation gates pass.” That worker **cannot restore** today (NU1201) and has **no** `35=D` builder.

**`SAFE_BY_ABSENCE`.** Vacuous “cannot send because nothing emits `35=D`” is the current safety outcome, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do **not** tick Architecture §70.12 from this file. Do **not** enable the flag. Do **not** add a sender here.

---

## 5. Slot-73 delta vs siblings 13 / 33 / 53

| Item | Slot 13 / 33 / 53 | Slot 73 (this re-read) |
|---|---|---|
| API TFM | `net8.0-windows` + x64 | **Unchanged** |
| API restore | `success: true` | **Unchanged** |
| Trio in `bin\Debug\net8.0-windows\` | present | **Still present** (FileListAbsolute L1–3) |
| Workers + Integration `net8.0` + NU1201 | fail | **Still fail** (caches re-read) |
| `mt5-worker` leftover `bin\Debug\net8.0` trio | noted as missing | **Reconfirmed missing** via `list_dir` |
| API `bases/` Achiever + Starwave | present | **Still present** (2027 / 9904 cache files) |
| Live census | 18 / 8460 | **Not re-attached**; same JSON still the last measure |
| Product `35=D` | 0 | **Still 0** (fresh grep) |

No TFM regression. No live-send regression. Worker graph still broken.

---

## 6. Stale reports (do not cite as current TFM)

| File | What it said | Now |
|---|---|---|
| `A012_worker_tfm.md` | API TFM = `net8.0`; 3/3 hosts portable | API **is** `net8.0-windows` x64. Workers + Integration still `net8.0` + NU1201 |
| `A002_api_dummy_path.md` | API still `net8.0` | Same as A012 — stale |
| `A105_windows_dlls.md` | `src/Mt5` portable `net8.0`; Fake only | `src/Mt5` is `net8.0-windows` x64; API Windows bin **has** the trio; DI is Native + fail-closed |
| `W500_SLICE_5.md` | API still `net8.0` so NU1201 | Superseded by current csproj + `success: true` |

R021’s PE table and NU1201 *mechanism* remain valid.

---

## 7. What this file does **not** prove

- A fresh live Manager connect in this slot (census is prior same-day JSON).
- That `mt5-worker` can load the DLL (it **cannot restore**; leftover bin has no trio).
- Linux API legality (current API **is** a Windows Manager process; compose Linux image still cannot `LoadLibraryW`).
- Coded refuse of a future `35=D` while TRADE is LoggedOn (`GATE_INCOMPLETE`).
- Phase 8 / §68 / §70 readiness. Those remain **0** for live send.
- Manager-ACL-invisible groups on either broker.

---

## 8. File inventory (read, not modified)

- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`
- `D:\Prop\apps\api\obj\project.nuget.cache`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.csproj.FileListAbsolute.txt`
- `D:\Prop\apps\api\bin\Debug\net8.0-windows\TraderIntelligence.Api.deps.json` (+ `runtimeconfig.json` + directory listing + `bases/`)
- `D:\Prop\apps\api\bin\Debug\net8.0\` (stale; no trio)
- `D:\Prop\Directory.Build.props`
- `D:\Prop\docker-compose.yml`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj` + `obj\project.nuget.cache` + `obj\*.dgspec.json` + `bin\Debug\net8.0\`
- `D:\Prop\apps\fix-worker\TraderIntelligence.FixWorker.csproj` + `obj\project.nuget.cache`
- `D:\Prop\tests\Integration\TraderIntelligence.Tests.Integration.csproj` + `obj\project.nuget.cache`
- `D:\Prop\tests\Unit\TraderIntelligence.Tests.Unit.csproj`
- `D:\Prop\tools\LiveBrokerProbe\LiveBrokerProbe.csproj`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`Initialize` / `LoadLibraryW`)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`GetAllGroups`)
- `D:\Prop\reports\swarm\20260818\R021_dll_load.md` + `_tmp_r021_dll_load\pe_inspect.json` + `RefNet8*.csproj`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (group names + counts only)

Written:

- `D:\Prop\reports\swarm\20260818\W500_RESEARCH_73.md` (this file)

---

## 9. Assigned answers (do not paraphrase away)

1. **What is `Api.csproj` `TargetFramework` right now?**  
   **`net8.0-windows`**, with **`PlatformTarget` x64**. Restore TFM `net8.0-windows7.0`. Cache `success: true`. There is no file literally named `Api.csproj`.

2. **Does `net8.0` without windows/x64 fail to load `MT5APIManager64`?**  
   **Split:**  
   - **Without Windows OS** — **cannot** (`LoadLibraryW` / PE32+).  
   - **Without x64** — **cannot** (image is `0x8664`).  
   - **Without `-windows` on this product host** — **cannot restore** `src/Mt5` (**NU1201**), so the trio never lands beside the exe.  
   - **Isolated `net8.0` x64 on Windows with the native DLL present** — **can** `Initialize` (R021 `MT_RET_OK`). `-windows` is not the PE-load bit.

3. **Can this API fetch ALL Achiever + Starwave groups and ALL manager traders?**  
   **Yes, by code + prior live measure.** `GroupRequestArray("*")` + per-group `UserRequestArray`. Census **18 groups / 8460 traders**. ACL-visible only. This slot did not re-attach.

4. **Can copy-to-cTrader send a live order from this process?**  
   **No.** `RealCopyEnabled=false`. Only FIX MsgType built on the wire path is Logon `35=A`. **`SAFE_BY_ABSENCE`.** Risk to capital: **none**.

**Do not add a `35=D` sender. Do not flip the flag. Product source was not modified.**
