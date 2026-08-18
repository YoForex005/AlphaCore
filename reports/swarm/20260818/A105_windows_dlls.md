# A105 — Windows-only `mt5-worker` + copy-dlls from `vendor/Libs`

| Field | Value |
|---|---|
| Agent | A105 (senior engineer, evidence-only) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Binding spec | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§5 Deployment** |
| Supporting spec | Same file §§7–8 (`MT5_MODE=local`), §66 (`/apps/mt5-worker`) |
| Adjacent swarm notes (read, not rewritten) | `A07_mt5_worker_audit.md`, `A14_mt5_manager_local.md`, `A16_mt5_http_client.md`, `A18_mt5_sdk_tests.md`, `A54_deployment_split.md`, `A56_risk_list.md`, `A65_docker_compose.md` |
| Product source modified | **No.** This file is the only write. |

Classification vocabulary is architecture §73.B:

```text
EXISTS_AND_GOOD
EXISTS_NEEDS_REFACTOR
MISSING
DEPRECATED
UNSAFE
```

---

## 0. Mandate (do not reinterpret)

Architecture **§5 Deployment**:

```text
Docker where compatible
Windows Worker if MT5 Manager DLL requires Windows
Linux for API/Postgres/Redis/Python/React if appropriate
```

```text
Do not force native MT5 SDK components into Linux containers
if the SDK does not support it cleanly.
```

Architecture **§7** pins Achiever to `MT5_MODE=local`. Local means **in-process** `LoadLibraryW` of `MT5APIManager64.dll`, then Manager TCP to the broker. It is not loopback HTTP, not Wine, not a Linux container with a PE copied in.

This note pins two operational facts:

1. **`apps/mt5-worker` must run on Windows x64** the moment it loads the Manager DLL (Phase 1 default).
2. **copy-dlls** is a real, already-written CMake contract: copy three files from `mt5-sdk/vendor/MetaTrader5SDK/Libs/` beside the consuming exe. The C# worker does **not** call that contract today.

---

## 1. Verdict

| Question | Answer |
|---|---|
| Must `mt5-worker` run on Windows to load `MT5APIManager64.dll`? | **Yes. Measured.** The DLL is PE32+ AMD64 (`0x8664`). The loader is `LoadLibraryW`. There is no `dlopen` path. |
| Can a Linux `mt5-worker` container `LoadLibrary` the same file? | **No. `UNSAFE`.** ELF host cannot load a PE. Architecture §5 forbids forcing it. |
| Does Wine / Proton / `box64` count? | **No. `UNSAFE`.** Unsupported, non-deterministic, not §5. |
| Does `MT5_MODE=remote` make the C# process Linux-legal as Phase 1? | **Not as the default.** Remote still needs a **Windows** process that owns the DLL. §7 says `local`. |
| What is “copy-dlls”? | CMake function `mt5sdk_copy_runtime_dlls(<target>)` in `mt5-sdk/CMakeLists.txt` 120–129. Source dir = `vendor/MetaTrader5SDK/Libs`. **WIN32 only.** |
| Which files must sit beside a local-mode exe? | The CMake trio: `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MetaQuotes.MT5CommonAPI64.dll`. |
| Does the C# worker copy them today? | **No.** `TraderIntelligence.Mt5Worker.csproj` has no RID, no `Content`, no post-build copy. Release output has **0** vendor Manager DLLs. |
| Does the C# worker load the DLL today? | **No.** DI registers `FakeMt5BrokerConnector` only (`Infrastructure/DependencyInjection.cs` 31–34). No `DllImport`, no C++/CLI, no `NativeLibrary.Load`. |

Honest one-liner: **Windows owns `LoadLibrary` of `MT5APIManager64.dll`. copy-dlls is `mt5sdk_copy_runtime_dlls` from `vendor/Libs`. The C# worker is a Windows host with neither the copy step nor the load.**

---

## 2. Why the worker must be Windows (measured, not folklore)

### 2.1 The file that must be loaded is a Windows x64 PE

Measured 2026-08-18 from `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`:

| File | Bytes | DOS | Optional magic | Machine | Kind | SHA-256 |
|---|---:|---|---|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | `MZ` | `0x020B` PE32+ | `0x8664` AMD64 | native (no CLR COM dir) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `MZ` | `0x020B` PE32+ | `0x8664` AMD64 | CLR (COM descriptor present) | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `MZ` | `0x020B` PE32+ | `0x8664` AMD64 | CLR (COM descriptor present) | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

These are **not** ELF `.so`, not .NET `AnyCPU`, not Linux-portable. `MT5APIManager64.dll` is a native Win32 image. A Linux process cannot map it.

Sibling files in the same folder (not in the CMake copy list, same OS family unless noted):

| File | Bytes | Machine | Notes |
|---|---:|---|---|
| `MT5APIManager64avx.dll` | 8,705,968 | `0x8664` | CPU-variant search (`FindLibrary`) |
| `MT5APIManager64avx2.dll` | 8,396,256 | `0x8664` | CPU-variant search (`FindLibrary`) |
| `MT5APIManager64arm.dll` | 10,542,520 | `0xAA64` ARM64 | ARM64 Windows only |
| `MT5APIGateway64*.dll` | — | AMD64 / ARM64 | **Gateway** API. Not Manager. Do not copy for the worker. |
| `MetaQuotes.MT5GatewayAPI64.dll` | 189,160 | `0x8664` | Gateway CLR wrapper. Not for `mt5-worker`. |
| `MetaQuotes.MT5WebAPI.dll` | 247,608 | `0x014C` i386 PE32 | **32-bit.** Do not copy next to a 64-bit worker. |

### 2.2 The loader is Win32 `LoadLibraryW`

`CMTManagerAPIFactory::Initialize` (`vendor/MetaTrader5SDK/Include/MT5APIManager.h` 1719–1744):

```text
FindLibrary(dll_path, path)
LoadLibraryW(path)
GetProcAddress(..., MTManagerVersion / MTManagerCreateExt / MTAdminCreateExt)
```

Shutdown is `FreeLibrary`. There is no POSIX `dlopen` / `dlsym` branch.

`MT5Manager::Initialize` (`mt5-sdk/src/core/mt5_manager.cpp` 16–31) is a thin fail-closed wrapper around that factory, then `CreateManager(MTManagerAPIVersion, &m_manager)`.

`MT5Pool::Initialize` (`mt5_pool.cpp` 888–914) calls the same `m_factory.Initialize(dllPath)`.

The C++ header cannot even compile off Windows:

```3:4:D:\Prop\mt5-sdk\src\core\mt5_manager.h
#include <Windows.h>
#include "MT5APIManager.h"
```

### 2.3 CMake already refuses to compile local mode off Windows

`mt5-sdk/CMakeLists.txt` 49–57:

```text
# MT5Manager, MT5Pool and MT5Watchdog bind the native MetaQuotes Manager API,
# which ships as Windows DLLs only. On other platforms the HTTP client remains
# available and the local-mode transport is simply absent.
if(WIN32)
    list(APPEND MT5SDK_SOURCES
        src/core/mt5_manager.cpp
        src/core/mt5_pool.cpp
        src/core/mt5_watchdog.cpp
    )
endif()
```

Operator probes that actually `Initialize` the factory are gated `if(MT5SDK_BUILD_PROBES AND WIN32)` (`CMakeLists.txt` 164).

`mt5-sdk/README.md`:

> **local** — native MetaQuotes Manager API. Lowest latency, Windows-only, consumes a manager connection slot on the broker.

> C++20 compiler. **MSVC 2022 for local mode (the Manager API is Windows x64 only).**

`mt5_types.h` 119–123 keeps order-type constants as plain integers so non-SDK TUs can compile **without** “pulling in the Windows-only SDK headers.”

### 2.4 What “local” is (so nobody fakes it on Linux)

```text
apps/mt5-worker  (must be a Windows x64 process)
  → IMt5BrokerConnector  (C#; not implemented for native yet)
    → MT5Manager / MT5Pool
      → CMTManagerAPIFactory
        → LoadLibraryW(MT5APIManager64*.dll)
          → TCP to <MT5_SERVER>:<MT5_PORT>   (optional ProxySet first)
```

There is no named pipe implied by `local`. Local = **in-process native DLL + direct manager TCP**.

`MT5_MODE=remote` (`MT5HttpClient`, libcurl) is portable **source**. It is not a Linux Manager. Something Windows-side still has to load the DLL. That sidecar is **not** in this repo (`A16`). Pointing a Linux worker at a missing URL is not Phase 1.

### 2.5 WSL2 / Docker / portable TFM traps

| Host | Legal for `LoadLibrary(MT5APIManager64.dll)`? |
|---|---|
| Windows 11 / Windows Server x64 process | **Yes.** Preferred. |
| Windows Server Core x64 | Yes. |
| WSL2 Ubuntu | **No.** WSL2 is Linux. Run the worker as a **Windows** process; Postgres may live in WSL. |
| `mcr.microsoft.com/dotnet/aspnet:8.0` (Linux) | **No. `UNSAFE`.** |
| Wine / Proton | **No. `UNSAFE`.** |
| `dotnet publish -r linux-x64` of `mt5-worker` | Produces a host that **cannot** load the DLL. Do not call that “done.” |
| Portable `net8.0` TFM with no RID (today’s csproj) | Compiles on any OS. **Runtime load is still Windows-only.** “.NET is cross-platform” ≠ “mt5-worker is cross-platform.” |

Current Release host (`apps/mt5-worker/bin/Release/net8.0/TraderIntelligence.Mt5Worker.exe`) is itself PE AMD64 `0x8664` (this machine is Windows). That only proves the **apphost** is a PE. It does **not** prove Manager DLLs are present or loaded.

---

## 3. copy-dlls from `vendor/Libs`

### 3.1 Canonical implementation (already in tree)

There is **no** script named `copy-dlls.ps1` / `CopyDlls` target under `D:\Prop`. The official copy-dlls is the CMake function `mt5sdk_copy_runtime_dlls`.

Source root:

```text
MT5_SDK_DIR  = ${CMAKE_CURRENT_SOURCE_DIR}/vendor/MetaTrader5SDK
MT5_LIB_DIR  = ${MT5_SDK_DIR}/Libs
             = D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs
```

Override: `-DMT5_SDK_DIR=<path-to-MetaTrader5SDK>` if the SDK is installed elsewhere. The directory **must** contain `Include/` and `Libs/`. Missing `Include/MT5APIManager.h` is a CMake `FATAL_ERROR`.

Runtime set (`CMakeLists.txt` 114–118):

```cmake
set(MT5SDK_RUNTIME_DLLS
    "${MT5_LIB_DIR}/MT5APIManager64.dll"
    "${MT5_LIB_DIR}/MetaQuotes.MT5ManagerAPI64.dll"
    "${MT5_LIB_DIR}/MetaQuotes.MT5CommonAPI64.dll"
)
```

Copy function (`CMakeLists.txt` 120–129):

```cmake
function(mt5sdk_copy_runtime_dlls target)
    if(NOT WIN32)
        return()
    endif()
    foreach(dll IN LISTS MT5SDK_RUNTIME_DLLS)
        add_custom_command(TARGET ${target} POST_BUILD
            COMMAND ${CMAKE_COMMAND} -E copy_if_different
                "${dll}" "$<TARGET_FILE_DIR:${target}>")
    endforeach()
endfunction()
```

Facts:

| Fact | Evidence |
|---|---|
| Name | `mt5sdk_copy_runtime_dlls` |
| Source | `vendor/MetaTrader5SDK/Libs/` (or `${MT5_SDK_DIR}/Libs`) |
| Destination | `$<TARGET_FILE_DIR:${target}>` — **beside the consuming exe**, not into `PATH` |
| Mode | `copy_if_different` POST_BUILD |
| Non-Windows | **No-op.** Returns immediately. |
| Who calls it in-tree | Operator probes only (`mt5_group_probe`, `mt5_news_calendar_probe`) when `MT5SDK_BUILD_PROBES AND WIN32` |
| Who is told to call it | Any parent that links `mt5sdk::mt5sdk` and wants local mode (`README.md` 83–93) |

README contract:

```cmake
add_subdirectory(external/mt5-sdk)
target_link_libraries(my_app PRIVATE mt5sdk::mt5sdk)
# Local mode loads the MetaQuotes DLLs at runtime — copy them next to the binary.
mt5sdk_copy_runtime_dlls(my_app)
```

That is the whole copy-dlls rule. Do not invent a second unofficial copy list.

### 3.2 Why three files, not one

| File | Role |
|---|---|
| `MT5APIManager64.dll` | Native Manager factory. This is what `LoadLibraryW` maps. **Required** for C++ `MT5Manager` / `MT5Pool`. |
| `MetaQuotes.MT5ManagerAPI64.dll` | Managed (CLR) Manager wrapper used by vendor .NET examples (`Examples/Manager/*Example.NET`, HintPath `..\..\..\Libs\MetaQuotes.MT5ManagerAPI$(TargetNamePostfix).dll`). CMake copies it so a future C# / C++/CLI host can reference it without a second hunt. |
| `MetaQuotes.MT5CommonAPI64.dll` | Managed common types; HintPath sibling of the Manager wrapper. |

C++ local mode **loads** the native DLL. The two `MetaQuotes.*` assemblies are **not** `LoadLibrary`’d by `CMTManagerAPIFactory`. They are still part of the **published runtime set** so C# and C++ consumers share one copy contract. Do not drop them from a worker publish “because C++ only needs one file” unless a later measured design proves the managed wrappers will never sit in this process.

CMake does **not** copy AVX / AVX2 / ARM variants. `FindLibrary` will fall back to `MT5APIManager64.dll` when the preferred CPU file is absent. That is acceptable **if** the vanilla DLL is the same SDK drop (hashes above). Mixing an AVX2 DLL from a different MetaQuotes install with this header set is `UNSAFE` (API version / struct layout).

### 3.3 How `FindLibrary` searches (copy destination must match this)

`MT5APIManager.h` 1769–1831, in order:

1. If `dll_path` is passed: `{dll_path}\{preferred}` then `{dll_path}\MT5APIManager64.dll`.
2. Preferred name: `MT5APIManager64avx2.dll` / `avx` / `arm` / default, from CPU (`VersionAVX` / `_M_ARM64`).
3. Walk **three** parent directories of the exe (`GetModuleFileNameW`): `{folder}\{preferred}`, `{folder}\libs\{preferred}`, then the default name.
4. Else: copy the **bare filename** into `path` and return **`true`**. `LoadLibraryW` then uses the process DLL search / **PATH**.

Implications for copy-dlls and for the C# worker:

- Putting the trio **next to `TraderIntelligence.Mt5Worker.exe`** satisfies step 3 even if `Initialize(NULL)` is used.
- Passing an absolute directory (`...\vendor\MetaTrader5SDK\Libs` or a staged `.\libs`) is better: step 1 wins, PATH is never consulted.
- PATH fallback always “succeeds” at the Find stage. A planted or leftover `MT5APIManager64.dll` on PATH will load. Production must pin an absolute `dllPath` and refuse a miss (`A56` R11 / §2.2).
- Probe code today passes `sourceDir()/MetaTrader5SDK/Libs` (`tests/mt5_group_probe.cpp` 99), **not** `vendor/MetaTrader5SDK/Libs`. That path only works if `PROPFIRM_SOURCE_DIR` / cwd is laid out that way. CMake copy-dlls uses the vendored `Libs/` path. **Do not copy the probe’s relative layout into the C# worker.** Use `mt5-sdk/vendor/MetaTrader5SDK/Libs` (or `MT5_SDK_DIR/Libs`).

### 3.4 What copy-dlls must **not** do

| Action | Class | Why |
|---|---|---|
| `COPY` the trio into a Linux API/worker image | `UNSAFE` | PE on ELF. A54/A65 already forbid this. |
| Copy `MT5APIGateway64*.dll` / `MetaQuotes.MT5GatewayAPI64.dll` next to `mt5-worker` | `DEPRECATED` if proposed | Different API. Worker is Manager, not Gateway. |
| Copy `MetaQuotes.MT5WebAPI.dll` | `UNSAFE` | `0x014C` i386. Wrong bitness. |
| Copy `MT5APIManager64arm.dll` onto an AMD64 host as the only file | `UNSAFE` | `0xAA64`. `LoadLibrary` fails. |
| Skip copy and rely on a random MetaQuotes install on PATH | `UNSAFE` | Version skew + DLL planting. |
| Check the vendor DLLs into `apps/mt5-worker/bin/` in git | `DEPRECATED` | Stage at build/publish from `vendor/Libs`. The vendor tree is already in the repo. |
| Redistribute `vendor/MetaTrader5SDK/` outside this private tree | licence | README: MetaQuotes SDK is **not** ours to sublicense. |

### 3.5 C# worker: copy-dlls is **MISSING**

Measured on `D:\Prop\apps\mt5-worker`:

| Check | Result |
|---|---|
| `TraderIntelligence.Mt5Worker.csproj` `RuntimeIdentifier` / `win-x64` | **Absent** |
| `Content` / `None` / `CopyToOutputDirectory` of any `Libs\*.dll` | **Absent** |
| `Directory.Build.props` copy step | **Absent** (lang/nullable only) |
| `DllImport` / `NativeLibrary` / `LoadLibrary` in `src/` + `apps/mt5-worker` | **None** (grep) |
| Vendor `MT5API*.dll` / `MetaQuotes.MT5*.dll` under `apps/mt5-worker/bin` | **0 files** |
| What *is* next to the Release exe | Managed product + NuGet deps (`Npgsql`, EF, Redis, …). `TraderIntelligence.Mt5.dll` is the C# stub assembly, **not** the MetaQuotes DLL. |

`src/Mt5/TraderIntelligence.Mt5.csproj` is portable `net8.0`, no `AllowUnsafeBlocks`, no native items.

`src/Mt5/Configuration/Mt5BrokerOptions.cs` documents `Mode` `"local"` / `"remote"` and defaults **`Mode = "remote"`**. That default is **not** architecture §7 (`local`). It is also unused: DI never binds this options type to a live connector.

Current process path (2026-08-18, after later swarm slices; A07’s 1 Hz log template is stale):

```text
Program.cs
  AddTraderIntelligence()
    FakeMt5BrokerConnector ×2   (DemoBrokerFactory)
    InMemory or Npgsql
    DemoSeeder.SeedAsync
  Worker.ExecuteAsync
    DealIngestionService.SyncBrokerAsync(Achiever / StarwaveFx)
      connector.GetGroups / GetAccounts / GetDeals / GetPositions
        → in-process fake lists
```

A green `dotnet run` of `mt5-worker` is **not** “Manager DLL loaded.” It is demo ingest. Shipping that exe to a Windows VM **without** copy-dlls still cannot go live.

### 3.6 Required C# copy-dlls (design only — not implemented here)

When a coding agent is allowed to touch product source, the worker must grow a Windows-only copy that is the **same trio** as CMake, from the **same folder**.

Suggested shape (do **not** apply in this pass):

```xml
<PropertyGroup>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <Mt5VendorLibs>$(MSBuildThisFileDirectory)..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs</Mt5VendorLibs>
</PropertyGroup>

<ItemGroup>
  <None Include="$(Mt5VendorLibs)\MT5APIManager64.dll"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest"
        Link="MT5APIManager64.dll"
        Condition="Exists('$(Mt5VendorLibs)\MT5APIManager64.dll')" />
  <None Include="$(Mt5VendorLibs)\MetaQuotes.MT5ManagerAPI64.dll"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest"
        Link="MetaQuotes.MT5ManagerAPI64.dll"
        Condition="Exists('$(Mt5VendorLibs)\MetaQuotes.MT5ManagerAPI64.dll')" />
  <None Include="$(Mt5VendorLibs)\MetaQuotes.MT5CommonAPI64.dll"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest"
        Link="MetaQuotes.MT5CommonAPI64.dll"
        Condition="Exists('$(Mt5VendorLibs)\MetaQuotes.MT5CommonAPI64.dll')" />
</ItemGroup>
```

Publish (A54):

```text
dotnet publish apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj -c Release -r win-x64 --self-contained false
```

Smoke after copy (Windows):

1. The three files exist next to `TraderIntelligence.Mt5Worker.exe`.
2. SHA-256 matches the table in §2.1 (or a pinned newer SDK drop recorded in the run log).
3. PE machine is `0x8664` (`dumpbin /headers` or the same byte check used for this report).
4. Fail the job if a Linux agent produced a non-PE worker artifact and called it the collector.

If the process later **links** `mt5sdk` as a native/C++/CLI host instead of P/Invoke, call `mt5sdk_copy_runtime_dlls` on **that** native target and do not maintain a second file list.

Optional AVX2: only add `MT5APIManager64avx2.dll` to the copy set if lab CPUs should take the `FindLibrary` fast path **and** the file is from the **same** SDK drop. Never copy ARM onto AMD64.

### 3.7 Vendor .NET examples are not the worker’s copy model

`Examples/Manager/BalanceExample.NET` / `DealerExample.NET` reference `MetaQuotes.MT5*$(TargetNamePostfix).dll` via `HintPath` into `..\..\..\Libs`. They do **not** `CopyToOutputDirectory` the native `MT5APIManager64.dll`. `TextFeeder.NET` `CopyToOutputDirectory` items are `_books.txt` / `_news.txt` / `_ticks.txt`, not Manager DLLs.

Do not copy that example layout into `TraderIntelligence.Mt5Worker.csproj`. The worker contract is **CMake’s three-file set beside the exe**.

---

## 4. How this sits on the deployment split

Unchanged from A54 / A65; restated so copy-dlls has an owner:

| Process | OS | Loads `MT5APIManager64.dll`? | copy-dlls? |
|---|---|---|---|
| `apps/mt5-worker` | **Windows Server x64** (VM / bare metal) | **Yes** (when native/local is wired) | **Yes** — trio from `vendor/Libs` beside the exe |
| Optional HTTP sidecar | Windows only | Yes | Same trio beside **that** exe |
| `apps/api` | Linux | **No** | **No.** Must not reference `src/Mt5` native load. |
| `apps/fix-worker` | Linux preferred | **No** | **No.** QuickFIX/n is managed. |
| Postgres / Redis / React / later Python | Linux | **No** | **No.** |
| Linux `docker-compose` | Linux | **No** | Must **not** `COPY` `vendor/MetaTrader5SDK/Libs`. |

Achiever egress `81.29.145.69` is a **Windows-worker / proxy** constraint. It is not a reason to put the DLL on Linux.

Lab on this Windows box: run the worker as a Windows process; point it at Postgres on localhost / WSL forwarded port. Do not run local-mode worker *inside* WSL2.

---

## 5. Classification (this slice)

| Item | Class | Evidence |
|---|---|---|
| Architecture §5 Windows-worker sentence | `EXISTS_AND_GOOD` | architecture lines 310–318 |
| Vendor Manager DLLs as PE32+ AMD64 | `EXISTS_AND_GOOD` | §2.1 hashes / `0x8664` / `0x020B` |
| `LoadLibraryW` factory | `EXISTS_AND_GOOD` | `MT5APIManager.h` 1719–1755 |
| CMake `if(WIN32)` local sources | `EXISTS_AND_GOOD` | `CMakeLists.txt` 49–57 |
| `mt5sdk_copy_runtime_dlls` from `vendor/Libs` | `EXISTS_AND_GOOD` | `CMakeLists.txt` 108–129, README 83–93 |
| Probe `mt5sdk_copy_runtime_dlls(...)` wiring | `EXISTS_AND_GOOD` | `CMakeLists.txt` 164–168 |
| Probe `Initialize` path `MetaTrader5SDK/Libs` vs vendored `vendor/.../Libs` | `EXISTS_NEEDS_REFACTOR` | `mt5_group_probe.cpp` 99; A18 |
| C# worker RID `win-x64` | `MISSING` | csproj TFM `net8.0` only |
| C# worker copy-dlls from `vendor/Libs` | `MISSING` | no Content/post-build; 0 DLLs in `bin` |
| C# native / P/Invoke / C++/CLI load of `MT5APIManager64.dll` | `MISSING` | Fake connector only |
| `Mt5BrokerOptions.Mode` default `"remote"` | `EXISTS_NEEDS_REFACTOR` | conflicts with §7 `local`; unused at runtime |
| Forcing DLL into Linux compose / API image | not present (keep it that way) | adding it would be `UNSAFE` |
| Wine / Linux `LoadLibrary` of the PE | `UNSAFE` if proposed | §2.5 |
| PATH-only discovery in production | `UNSAFE` | `FindLibrary` always returns true at last step |

---

## 6. Failure modes if this note is ignored

| Mistake | What actually happens |
|---|---|
| Publish `mt5-worker` as `linux-x64` and “try the DLL” | `LoadLibrary` / `NativeLibrary.Load` fails. No groups, no deals. Looks like “SDK broken.” |
| Copy trio into `mcr.microsoft.com/dotnet/aspnet:8.0` | Same fail. Image looks complete (`ls` shows `.dll`). Process cannot map them. |
| Ship Windows exe without copy-dlls | Factory `MT_RET_ERR_NOTFOUND` or PATH hijack. Silent “not connected.” |
| Copy Gateway / WebAPI instead of Manager | Wrong API or 32-bit load fail. |
| Mix AVX2 DLL from another SDK drop with this `MT5APIManager.h` | `CreateManager(MTManagerAPIVersion)` fail-closed, or struct desync if version check is bypassed. |
| Run worker in WSL2 because “the repo is on `/mnt/d`” | Linux. DLL will not load. |
| Treat current `Worker` 30 s fake sync as Phase 1 | Demo rows in InMemory/Postgres. Achiever/StarwaveFX never contacted. |

---

## 7. Implementation sequence (authorized later — not this pass)

Do **not** implement in this agent. When product source may change:

1. Pin `RuntimeIdentifier=win-x64` on `TraderIntelligence.Mt5Worker`.
2. Add the §3.6 copy-dlls `ItemGroup` from `mt5-sdk/vendor/MetaTrader5SDK/Libs` (same three names as `MT5SDK_RUNTIME_DLLS`). Fail the build if those files are missing on a Windows agent.
3. Keep `apps/api` and Linux compose free of `vendor/Libs`.
4. When `IMt5BrokerConnector` grows a real local transport: pass an **absolute** `dllPath` to the factory (staged output dir or vendored `Libs`). Do not pass `NULL` and hope PATH works.
5. Record loaded path + file SHA-256 + `MTManagerAPIVersion` at process start (A56 R11).
6. Only then: replace `FakeMt5BrokerConnector` for live Achiever / StarwaveFX. Keep fakes for unit/integration tests that must not load native.

Phase 1 acceptance is still: **Windows** process, **these** DLLs mapped, Manager TCP to live brokers, rows in **Linux** Postgres. A Linux-only compose up is not Phase 1. A Windows worker without copy-dlls is not Phase 1.

---

## 8. File inventory

Read, not modified:

- `D:\Prop\mt5-sdk\CMakeLists.txt` (lines 24–31, 49–57, 108–129, 164–168)
- `D:\Prop\mt5-sdk\README.md` (local vs remote, MSVC 2022, `mt5sdk_copy_runtime_dlls`)
- `D:\Prop\mt5-sdk\src\core\mt5_manager.h` (`#include <Windows.h>`)
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (`Initialize` → factory)
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (`MT5Pool::Initialize`)
- `D:\Prop\mt5-sdk\src\core\mt5_types.h` (Windows-only header comment)
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` (dll path)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`FindLibrary` / `LoadLibraryW`)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\*` (PE + SHA-256)
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\bin\Release\net8.0\` (no vendor Manager DLLs)
- `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj`
- `D:\Prop\src\Mt5\Configuration\Mt5BrokerOptions.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\Directory.Build.props`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §5, §7
- Vendor .NET examples under `mt5-sdk/vendor/MetaTrader5SDK/Examples/Manager/*Example.NET` and `Examples/Gateway/TextFeeder.NET` (HintPath / copy items)

Written:

- `D:\Prop\reports\swarm\20260818\A105_windows_dlls.md` (this file)

---

## 9. Bottom line

`MT5APIManager64.dll` is a native Windows x64 PE. `mt5-worker` must be a Windows x64 process to load it. copy-dlls already exists as `mt5sdk_copy_runtime_dlls`: `copy_if_different` of three files from `mt5-sdk/vendor/MetaTrader5SDK/Libs/` to the exe directory, **WIN32 only**. The C# worker does not invoke that contract, does not pin `win-x64`, and does not load the DLL — it syncs fakes. Do not put the DLL on Linux. Do not call a worker without these files “connected.”
