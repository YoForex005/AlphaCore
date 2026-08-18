# W500_RESEARCH_133 — `Api.csproj` TargetFramework: `net8.0` without windows/x64 vs `MT5APIManager64`

| Field | Value |
|---|---|
| Slot | **133** |
| Agent | W500_RESEARCH_133 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_133.md` |
| Assigned | Check `Api.csproj` `TargetFramework`. Claim: **`net8.0` without windows/x64 cannot load `MT5APIManager64`**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only product-adjacent write (plus INDEX / SWARM_LOG catalog). |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** |
| Secret values printed | **None.** Password keys named only. No tag 554, no proxy auth, no manager passwords, no account balances dumped. |
| Method | Independent `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current `TraderIntelligence.Api.csproj`, nuget dgspec/cache, GeneratedMSBuildEditorConfig, FileListAbsolute, Debug output dirs + `deps.json` + `runtimeconfig.json`, `src/Mt5` + Infrastructure + ingest/DI/FIX, vendor `MT5APIManager.h` `LoadLibraryW`, R021 PE inspect JSON, live census JSON. **No Manager reconnect. No FIX send. No product edit.** |
| Siblings (same topic, earlier slots) | `W500_RESEARCH_33.md`, `W500_RESEARCH_93.md`, `W500_RESEARCH_113.md` — same question; this file re-measures, does not copy. |

**Honesty rule:** `-windows` on the TFM is **not** what `LoadLibraryW` checks. `LoadLibraryW` checks **OS + PE machine + file present**. Isolated `net8.0` x64 on Windows **can** initialize the factory (R021). In **this product graph**, a portable `net8.0` **host** cannot `ProjectReference` `net8.0-windows` `src/Mt5` / `Infrastructure` (**NU1201**) and therefore cannot stage the trio beside the exe. Do not collapse those two facts. Do not claim “EX5 decompiled” or “95% live copy.”

---

## 0. Verdict (binding)

**PASS — current `Api.csproj` is already `net8.0-windows` + `PlatformTarget` x64; Manager trio is in the Windows output; live catalog path can load `MT5APIManager64`; copy stays off (`SAFE_BY_ABSENCE`).**

The folklore sentence “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is **half-true**. Split it:

| Claim | Measured result | Class |
|---|---|---|
| Current `apps/api/TraderIntelligence.Api.csproj` TFM | **`net8.0-windows`** + **`PlatformTarget` x64** | `EXISTS_AND_GOOD` |
| `RuntimeIdentifier` / `win-x64` RID pin | **Absent** on every product `.csproj` and `Directory.Build.props` | not required for LoadLibrary; process is still x64 via `PlatformTarget` |
| API restore against `src/Mt5` + `Infrastructure` | **`success: true`** (`obj/project.nuget.cache`, `dgSpecHash` `6bXcKBewNHo=`, `logs: []`) | `EXISTS_AND_GOOD` |
| Trio next to current API exe | **Yes** — `bin\Debug\net8.0-windows\{MT5APIManager64, MetaQuotes.MT5ManagerAPI64, MetaQuotes.MT5CommonAPI64}.dll` (FileListAbsolute L1–3) | `EXISTS_AND_GOOD` |
| Manager already mapped in this output | **Yes** — `bases/AchieverGlobalMarkets-Server/` (`*-2027.dat`) + `bases/StarwaveFX-Server/` (`*-9904.dat`) | prior live session |
| Stale `bin\Debug\net8.0\` (pre-retarget) has the trio | **No** — folder listing has **zero** `MT5API*` / `MetaQuotes*`; `deps.json` has **0** MetaQuotes strings | leftover; do not run |
| Isolated `net8.0` x64 can `SMTManagerAPIFactory.Initialize` on Windows when native DLL is beside exe | **Yes (R021)** `MT_RET_OK` | PE-load **not** gated by `-windows` |
| Isolated `net8.0` **x86** can map this AMD64 PE | **No** — PE machine `0x8664`; CS8012; no x86 runtime here | **x64 required** |
| Linux / WSL2 / Wine `net8.0` can `LoadLibraryW` this PE | **No** | **Windows OS required** |
| Compose `api` service (`mcr.microsoft.com/dotnet/sdk:8.0` + `dotnet run --project apps/api`) | **Cannot** host this TFM or this PE | Linux image; comment on disk already says workers stay on Windows |
| Portable `net8.0` **host** can `ProjectReference` `src/Mt5` (`net8.0-windows`) | **No — NU1201** (still true for `mt5-worker`, `fix-worker`, Integration tests) | product-graph gate |
| Remaining hosts still `net8.0` | **`mt5-worker`**, **`fix-worker`**, **`tests/Integration`** — restore `success: false` | `EXISTS_NEEDS_REFACTOR` |
| Fetch ALL Achiever + Starwave groups + ALL manager traders | **Yes, path + prior live census** — Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** (probe `2026-08-18T08:42:16Z`; this slot did **not** re-attach) | catalog-only |
| Copy to cTrader may send live orders | **No** — `CTraderFixSession` emits only `35=A`; product `35=D` = **0**; `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persisted `AllowFixSend=false` | **`SAFE_BY_ABSENCE`** |
| Residual operator arm | Lab `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` is now bound by DI (`DependencyInjection.cs` L41). That arms `LiveRuntimeStatus.RealCopyEnabled`. It does **not** create a sender. | flag true ≠ send |
| Risk to capital from this process | **None** | Manager read + FIX Logon only |

One-line:

```text
Api.csproj = net8.0-windows + x64 (restore OK; trio in net8.0-windows output; bases/ 2027+9904).
RID win-x64 is NOT set. Isolated net8.0 x64 CAN LoadLibrary on Windows.
net8.0 host cannot ProjectReference Mt5 (NU1201). x86 / Linux cannot.
ALL groups/traders = GroupRequestArray("*") + UserRequestArray (18/8460).
No 35=D. Env REAL_COPY=true is armed but sender missing. No capital at risk.
```

A012 / A002 “API is still `net8.0`” is **stale**. Do not retarget the API again. Do **not** add a `35=D` sender in this task. Slots 33 / 93 / 113 asked the same question; this file independently re-read the same csproj + caches + output and agrees.

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

`D:\Prop\Directory.Build.props` still does **not** set TFM, RID, or PlatformTarget (LangVersion / Nullable / ImplicitUsings / Deterministic only). The API pin is **local to this csproj**. Product `*.csproj` + `*.props` have **0** `RuntimeIdentifier` / `EnableWindowsTargeting` / `win-x64` pins. `win-x64` is **not** the load bit; `PlatformTarget=x64` + Windows OS + native sibling are.

### 1.1 Restore graph agrees with the csproj

`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`:

| Field | Value |
|---|---|
| `originalTargetFrameworks` | **`net8.0-windows`** |
| Restore TFM / alias | **`net8.0-windows7.0`** / `net8.0-windows` |
| ProjectReferences | Application, Domain, Fix.CTrader, **Infrastructure**, **Mt5** |

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
| `MT5APIManager64.dll` | **Yes** (FileListAbsolute L3) |
| `MetaQuotes.MT5ManagerAPI64.dll` | **Yes** (L2) |
| `MetaQuotes.MT5CommonAPI64.dll` | **Yes** (L1) |
| `TraderIntelligence.Api.exe` | **Yes** |
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

C++ copy contract is the same trio, **WIN32 only** (`D:\Prop\mt5-sdk\CMakeLists.txt` L114–128 `mt5sdk_copy_runtime_dlls`). YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` L55–66 POST_BUILDs the same three files beside the native exe. That backend is **MSVC Win32**, not `net8.0`.

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

Loader is **`LoadLibraryW`**. Missing file → `MT_RET_ERR_NOTFOUND` (13). There is no `dlopen` branch. CMake only copies the trio `if(WIN32)`.

Do **not** copy `MetaQuotes.MT5WebAPI.dll` next to this process: PE32 **i386** (`0x014C`), different API.

### 2.2 Isolated TFM experiment (R021) — do not over-claim `-windows`

R021 built throwaway `net8.0` and `net8.0-windows` x64 projects that `<Reference>` the vendor wrappers directly (not via `src/Mt5`):

| Host TFM | PlatformTarget | Compile | `Initialize` with native sibling | `Initialize` without native sibling |
|---|---|---|---|---|
| `net8.0` | x64 | PASS | **`MT_RET_OK` (0)** | `MT_RET_ERR_NOTFOUND` (13) |
| `net8.0-windows` | x64 | PASS | **`MT_RET_OK`** | same |
| `net8.0` | AnyCPU (64-bit OS) | PASS | **OK on this box** | — |
| `net8.0` | x86 | CS8012; no x86 runtime (`0x80008083`) | **cannot map AMD64** | — |

So: **missing `-windows` is not the PE-load blocker.** Missing **Windows OS**, missing **AMD64 process**, or missing **`MT5APIManager64.dll` beside `Initialize`** are.

### 2.3 Product-graph gate (why API *did* need `-windows` + x64)

`src/Mt5` and `src/Infrastructure` are `net8.0-windows` + x64. NuGet will **not** let a `net8.0` exe `ProjectReference` a `net8.0-windows7.0` library:

| Host (measured `project.nuget.cache` now) | TFM | Restore | Error |
|---|---|---|---|
| `apps/api` | `net8.0-windows` | **`success: true`** | none |
| `apps/mt5-worker` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure **and** Mt5 |
| `apps/fix-worker` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure |
| `tests/Integration` | **`net8.0`** | **`success: false`** | NU1201 vs Infrastructure **and** Mt5 |
| `tools/LiveBrokerProbe` | `net8.0-windows` + x64 | used for the 08:42Z census | restore graph matches API |

`mt5-worker` GeneratedMSBuildEditorConfig still says `build_property.TargetFramework = net8.0`. Its `bin\Debug\net8.0\` listing has `TraderIntelligence.Mt5.dll` but **zero** `MT5APIManager64.dll` / `MetaQuotes.MT5*`. Its `deps.json` has **0** MetaQuotes strings. That stale worker output cannot `Initialize`.

Portable libraries that **should** stay `net8.0` (no Manager PE): Domain, Application, Fix.CTrader. That split is correct.

---

## 3. ALL Achiever + Starwave groups and ALL manager traders

### 3.1 Fetch path (no cap)

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51):

1. `GetGroupsAsync` → `GroupRequestArray("*")`, fallback `GroupTotal`/`GroupNext`.
2. `GetAccountsAsync(null, …)` → walks **every** group, then `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`.

There is **no** `Take(` in ingest. Residual `Take(200)` is only `GET /api/trades` and `GET /api/copy/intents` (dashboard slices), not catalog.

`LiveIngestHostedService` iterates `registry.All()` (both Native connectors), catalog then deals then `ListLoginsWithDealsAsync` scoring. Dummy `FakeMt5BrokerConnector` / `DemoSeeder` are **not** referenced from `apps/api`. DI throws if either password fails `IsSecret`.

`GetTradersAsync` (`EfDashboardQueries.cs` L85–128) is **account-driven** left-join of scores — every `Mt5Accounts` row is returned. Not scores-only.

### 3.2 Prior live census (not re-probed this slot)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK (HTTP proxy) | **8** | **6512** | **1506** |
| StarwaveFX | OK (direct) | **10** | **1948** | **478** |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23). Sum **6512**.

Starwave groups: `Starwave\cent\FX1\grp1` (11), `grp2` (4), `Starwave\demo\FX2\grp1` (170), `grp2` (1735), `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `Starwave\real\FX3\LP` (2). Sum **1948**.

`GetAccountsAsync(null)` is what produced those 8460 rows. This is **all manager users**, not a 200-row dashboard slice.

Residual if someone runs `apps/mt5-worker` after a TFM fix: `Worker.cs` still scores only hardcoded `10001, 10002, 10003, 99001`. The **API** hosted ingest does **not** use those dummy logins.

---

## 4. Copy to cTrader must not send live orders (no loss)

Measured product path — **no reconnect, no send** this slot:

| Gate | Location | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService` L16 | **`const false`** |
| `VenueReconciled` | `CopyTradingService` L15 | **`const false`** |
| Persisted `AllowFixSend` | `CopyTradingService` L192 | **hardcoded `false`** even if `Evaluate` would approve |
| Live-send branch | L198 | requires LIVE + NOS + reconciled — **unreachable** |
| FIX outbound MsgType | `CTraderFixSession.BuildLogon` L96 | **only `(35, "A")`** |
| Product `35=D` / `NewOrderSingle` builder | `src/Fix.CTrader` | **0 hits** (comment + log string only) |
| Socket lifetime | `TryLogonAsync` | TCP+TLS disposed after logon reply |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default | **`false`** |
| Committed `appsettings.json` | `FeatureFlags.LiveCopyEnabled` | **`false`** |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED` | **`true`** (boolean only; no secret) |
| DI bind | `DependencyInjection.cs` L41 | copies env `true` onto `LiveRuntimeStatus.RealCopyEnabled` |
| `/api/settings` | `Program.cs` L76 | exposes `runtime.RealCopyEnabled` (can show true) |
| `CopyTradingHostedService` | L30 | logs “Live NewOrderSingle still blocked” |

Even if an operator leaves `.env` armed, there is **no method** that can emit `35=D`. Safety is **`SAFE_BY_ABSENCE`**, not a single flag. `RiskEngine.Evaluate` can set `AllowFixSend` only when `RealExecutionEnabled && Reconciled && VenueHealthy`; copy persist then **overwrites** that to `false`. Do **not** implement a sender in this slot.

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
| Directory.Build.props | repo root | **no TFM** | no | — |

---

## 6. What not to do

1. Do **not** retarget `Api.csproj` again. It is already `net8.0-windows` + x64.
2. Do **not** claim “`net8.0` cannot LoadLibrary.” Isolated `net8.0` x64 **can**. Product `net8.0` hosts **cannot restore** `src/Mt5`.
3. Do **not** run `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.exe`. Trio missing.
4. Do **not** copy `MT5APIManager64.dll` into the Linux compose image.
5. Do **not** implement `35=D` / NewOrderSingle. Goal is catalog + no loss.
6. Do **not** treat `.env` `REAL_COPY_EXECUTION_ENABLED=true` as a send license.
7. Do **not** print manager / FIX / proxy secrets. This report does not.

---

## 7. Residual (honest, not this slot’s job)

| Residual | Why it matters |
|---|---|
| `mt5-worker` / `fix-worker` / Integration still `net8.0` | NU1201; cannot be the live Manager host |
| Stale `apps/api/bin/Debug/net8.0/` | leftover pre-retarget output |
| `.env` REAL_COPY=`true` now bound by DI | dashboard can show armed; sender still absent |
| `mt5-worker` Worker scores 4 dummy logins | irrelevant until worker TFM is fixed; API ingest is the live path |
| No `RuntimeIdentifier=win-x64` | not required for LoadLibrary; optional pin for publish |

---

## 8. Sources (absolute)

- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`
- `D:\Prop\apps\api\obj\project.nuget.cache`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`
- `D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.csproj.FileListAbsolute.txt`
- `D:\Prop\apps\api\bin\Debug\net8.0-windows\` (+ `bases\`)
- `D:\Prop\apps\api\bin\Debug\net8.0\`
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt`
- `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.json`
- `D:\Prop\reports\swarm\20260818\R021_dll_load.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`

**Slot 133 complete. Product tree untouched. No live order sent. Risk to capital: NONE.**
