# R006 — How to build `mt5_group_probe` on Windows (from `mt5-sdk/CMakeLists.txt`)

| Field | Value |
|---|---|
| Agent | R006 (senior engineer, CMake read + Windows toolchain measure) |
| Date | 2026-08-18 13:53:51 +05:30 (2026-08-18T08:23:51Z) |
| Assigned | Read `mt5-sdk` CMakeLists; document how to build `mt5_group_probe` on Windows. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R006_cmake.md` |
| Binding source | `D:\Prop\mt5-sdk\CMakeLists.txt` (173 lines, 6206 B) |
| Supporting | `D:\Prop\mt5-sdk\README.md`, `tests/mt5_group_probe.cpp`, `config/app_config.{h,cpp}`, `src/core/mt5_manager.{h,cpp}` |
| Adjacent (read, not rewritten) | A14, A18, A54, A105, C20, D66, E004, E022 |
| Product source modified | **No.** This report is the only write under the assignment. `D:\Prop\mt5-sdk` and `D:\Prop\src` were not edited. |
| CMakeLists SHA-256 | `98345532CA0D33888E919D14F680B933EB60C6C2A2CE85DBBF1F0D05419719E9` (MATCH D66) |
| Nested SDK HEAD | `a8f3fe85bc0adf109acb5ec72ed8adb2c0a289df` (single extraction commit; D66/C20) |
| This agent configured CMake? | **No.** |
| This agent compiled the probe? | **No.** |
| `mt5_group_probe.exe` on disk? | **No** (recursive search under `D:\Prop` empty). |
| `D:\Prop\mt5-sdk\build\` | **Absent.** `.gitignore` lists `build/`, `build-*/`, `out/`, `cmake-build-*/`. |

Honest one-liner: **`mt5_group_probe` is a Windows-only opt-in executable. Default CMake does not create the target. On this machine the recipe is MSVC 2022 x64 + vcpkg `x64-windows` + `-DMT5SDK_BUILD_PROBES=ON`.**

---

## 0. Verdict

| Question | Answer | Evidence |
|---|---|---|
| Does default configure produce `mt5_group_probe`? | **No.** | `option(MT5SDK_BUILD_PROBES … OFF)` (`CMakeLists.txt` 17) |
| What flag turns the target on? | `-DMT5SDK_BUILD_PROBES=ON` | `CMakeLists.txt` 17, 164; `README.md` 75, 151–154 |
| Is that flag enough by itself? | **No.** Also requires `WIN32`. | `if(MT5SDK_BUILD_PROBES AND WIN32)` (`CMakeLists.txt` 164) |
| Is it a CTest test? | **No.** Not `add_test`. Operator diagnostic only. | `CMakeLists.txt` 158–173 vs 134–156 |
| Can Linux CMake emit this exe? | **No.** Gate is `WIN32`. Silent skip (no FATAL). | `CMakeLists.txt` 164 |
| Can it build without the native Manager sources? | **No.** Probe includes `mt5_manager.h` (`Windows.h` + `MT5APIManager.h`). Those `.cpp` files are appended only `if(WIN32)`. | `CMakeLists.txt` 49–57; `mt5_manager.h` 3–4 |
| Postgres / Drogon required? | **No.** Leave both `OFF`. Probe does not use them. | `CMakeLists.txt` 14–15, 60–72 |
| Unit tests required? | **No.** Separate gate `MT5SDK_BUILD_TESTS`. | `CMakeLists.txt` 16, 134–156 |
| Manifest / presets in-tree? | **None.** No `vcpkg.json`, no `CMakePresets.json`. Classic toolchain-file mode. | `D:\Prop\mt5-sdk` file list |
| Live attach proven by this report? | **No.** Build recipe only. Probe not run. | this file |

Classification (architecture §73.B): CMake wiring is **`EXISTS_AND_GOOD`**. The probe binary is **`MISSING`** on this disk (never built here). That is not a CMake bug.

---

## 1. What CMake actually declares

`project(mt5sdk LANGUAGES CXX)` with `CMAKE_CXX_STANDARD 20` required. `cmake_minimum_required(VERSION 3.20)`.

### 1.1 Options (all cache `BOOL`, all default OFF)

| Option | Default | Effect on `mt5_group_probe` |
|---|---|---|
| `MT5SDK_BUILD_PROBES` | `OFF` | **Must be ON.** Only then is `add_executable(mt5_group_probe …)` reached, and only on Windows. |
| `MT5SDK_BUILD_TESTS` | `OFF` | Irrelevant. Probes are not in `MT5SDK_TESTS` and are not `add_test`. |
| `MT5SDK_WITH_POSTGRES` | `OFF` | Irrelevant. Adds `pg_pool` / ledger / account helper; `find_package(PostgreSQL)`. Leave OFF. |
| `MT5SDK_WITH_DROGON` | `OFF` | Irrelevant. Adds `mt5_tick_bridge`; `find_package(Drogon CONFIG)`. Leave OFF. |

Cache path (not a BOOL option):

| Cache | Default | Probe impact |
|---|---|---|
| `MT5_SDK_DIR` | `${CMAKE_CURRENT_SOURCE_DIR}/vendor/MetaTrader5SDK` | Must contain `Include/MT5APIManager.h` or configure **FATAL_ERROR**. Also sets `MT5_LIB_DIR` used by `mt5sdk_copy_runtime_dlls`. |

```33:37:D:\Prop\mt5-sdk\CMakeLists.txt
if(NOT EXISTS "${MT5_INCLUDE_DIR}/MT5APIManager.h")
    message(FATAL_ERROR
        "MetaTrader 5 SDK headers not found under ${MT5_INCLUDE_DIR}. "
        "Set -DMT5_SDK_DIR=<path-to-MetaTrader5SDK>.")
endif()
```

Measured: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` **exists** (133 640 B; D66 SHA-256 `00F8F0C82DCAF551A9B21D32CE6351B7B8920AB5084E34BED78D73CE4DCEEB33`). Default `MT5_SDK_DIR` is correct. Do not override unless the vendor tree is moved.

### 1.2 Always-required packages (even for the probe)

```19:21:D:\Prop\mt5-sdk\CMakeLists.txt
find_package(nlohmann_json CONFIG REQUIRED)
find_package(spdlog CONFIG REQUIRED)
find_package(CURL REQUIRED)
```

The probe TU includes `<nlohmann/json.hpp>` and `<spdlog/spdlog.h>`. It links `PRIVATE mt5sdk`. `mt5sdk` is a **STATIC** library that **PUBLIC**-links all three packages, and on WIN32 always compiles `mt5_http_client.cpp` (curl) even though the group probe never constructs `MT5HttpClient`.

So a probe-only build still needs **nlohmann-json + spdlog + curl**. It does **not** need libpq or Drogon.

README (`D:\Prop\mt5-sdk\README.md` 50–52) pins those to **vcpkg**. There is no FetchContent / submodule fallback.

### 1.3 Windows-only native sources (required by the probe)

```49:57:D:\Prop\mt5-sdk\CMakeLists.txt
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

Always compiled into `mt5sdk` (any platform):

- `config/app_config.cpp`
- `src/core/chart_timeframe.cpp`
- `src/core/mt5_http_client.cpp`
- `src/services/mt5_time_window.cpp`

`mt5_group_probe.cpp` includes `mt5_manager.h`. That header starts with `#include <Windows.h>` then `#include "MT5APIManager.h"`. A non-WIN32 `mt5sdk` would not even contain `MT5Manager` object code; CMake therefore does not add the probe off Windows.

### 1.4 The probe target itself

```164:173:D:\Prop\mt5-sdk\CMakeLists.txt
if(MT5SDK_BUILD_PROBES AND WIN32)
    foreach(probe_name mt5_group_probe mt5_news_calendar_probe)
        add_executable(${probe_name} tests/${probe_name}.cpp)
        target_link_libraries(${probe_name} PRIVATE mt5sdk)
        mt5sdk_copy_runtime_dlls(${probe_name})
        if(MSVC)
            target_compile_options(${probe_name} PRIVATE /W3 /utf-8 /bigobj)
        endif()
    endforeach()
endif()
```

Facts from that block:

| Fact | Meaning |
|---|---|
| Two targets | Turning probes on also builds `mt5_news_calendar_probe`. There is no CMake option to build only the group probe. Use `--target mt5_group_probe` to compile just one exe; the sibling is still in the generate graph. |
| Sources | Single TU: `tests/mt5_group_probe.cpp`. |
| Link | `PRIVATE mt5sdk` only. Alias `mt5sdk::mt5sdk` exists but probes do not use it. |
| CTest | **Not registered.** |
| POST_BUILD | `mt5sdk_copy_runtime_dlls` — see §4. |
| MSVC flags on the exe | `/W3 /utf-8 /bigobj` only. No extra `/MT`. |

### 1.5 Flags inherited from `mt5sdk` (MSVC)

Applied to the **library** (`CMakeLists.txt` 98–106). Definitions are **PUBLIC**, so the probe TU sees them:

```text
_WIN32_WINNT=0x0A00          # Windows 10+
NOMINMAX
WIN32_LEAN_AND_MEAN
_CRT_SECURE_NO_WARNINGS
```

Library compile options are **PRIVATE** (`/W3 /utf-8 /bigobj`); the probe repeats `/W3 /utf-8 /bigobj` on itself.

CMake does **not** set `CMAKE_MSVC_RUNTIME_LIBRARY`. Default is `/MD` (DLL CRT), which matches vcpkg triplet `x64-windows` (dynamic). Do **not** pass `-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded` against this triplet — CRT / ABI mismatch with curl/spdlog.

CMake does **not** define `PROPFIRM_SOURCE_DIR`. The probe’s `#ifdef PROPFIRM_SOURCE_DIR` is therefore **off** unless a parent project adds it. Runtime `sourceDir()` is `"."` (§5).

---

## 2. Windows host recipe (this machine, measured)

This agent measured the host; it did **not** run configure/build.

| Tool | Measured 2026-08-18 13:53 +05:30 |
|---|---|
| OS | Windows (user_info). Need **x64** process. Manager DLLs are PE32+ `0x8664` (A105). |
| CMake | `C:\Program Files\CMake\bin\cmake.exe` **4.4.0** (≥ 3.20). Default generator marked `* Visual Studio 17 2022`. |
| MSVC | Visual Studio **Build Tools 2022** at `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools`. Toolset **14.44.35207**. `cl.exe` is **not** on the default PATH (normal for vsdevcmd). |
| Windows SDK | `10.0.26100.0` |
| Ninja | **Not** on PATH. Prefer the VS generator. |
| vcpkg | `C:\tools\vcpkg\vcpkg.exe` version `2026-07-13-bf04c909…`. `VCPKG_ROOT` **unset**. Toolchain **exists**: `C:\tools\vcpkg\scripts\buildsystems\vcpkg.cmake`. |
| vcpkg triplet needed | `x64-windows` (README 64; dynamic `/MD`) |
| Installed packages | `nlohmann-json:x64-windows` 3.12.0#2; `spdlog:x64-windows` 1.17.0#1; `curl:x64-windows` 8.21.0#1. Also present but **not** required for the probe: `libpq`, `drogon`. |

CMake configs exist:

- `C:\tools\vcpkg\installed\x64-windows\share\nlohmann_json`
- `C:\tools\vcpkg\installed\x64-windows\share\spdlog`
- `C:\tools\vcpkg\installed\x64-windows\share\curl`

Without `-DCMAKE_TOOLCHAIN_FILE=…\vcpkg.cmake`, `find_package(nlohmann_json CONFIG REQUIRED)` **will FATAL** on a clean Windows box.

### 2.1 Configure + build (PowerShell)

Working directory does not matter if `-S` / `-B` are absolute. Recommended out-of-source dir: `D:\Prop\mt5-sdk\build` (gitignored).

```powershell
cmake -B D:\Prop\mt5-sdk\build -S D:\Prop\mt5-sdk `
  -G "Visual Studio 17 2022" -A x64 `
  -DCMAKE_TOOLCHAIN_FILE=C:\tools\vcpkg\scripts\buildsystems\vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-windows `
  -DMT5SDK_BUILD_PROBES=ON

cmake --build D:\Prop\mt5-sdk\build --config Release --target mt5_group_probe
```

README’s bash form (`README.md` 62–65) is the same configure plus a full `--build` with **no** `--target` and **no** `-DMT5SDK_BUILD_PROBES=ON`. That README snippet therefore builds `mt5sdk` only (static `.lib`), **not** the probe. The probe flag is documented later in the options table / “Tests and probes” section, not in the first `cmake -B` example. Do not copy the first README block and expect `mt5_group_probe.exe`.

### 2.2 Why `-A x64` is mandatory

Vendor Manager DLLs and `MT5APIManager.h` factory load `MT5APIManager64.dll` (and AVX/AVX2/ARM64 variants). A Win32 (`-A Win32`) generate would produce a 32-bit exe that cannot `LoadLibrary` those PE32+ images. `MetaQuotes.MT5WebAPI.dll` is the 32-bit leftover; CMake does **not** copy it (A105).

### 2.3 Multi-config vs Ninja

| Generator | `CMAKE_BUILD_TYPE` | Output path |
|---|---|---|
| `Visual Studio 17 2022` (this host’s default) | Ignored. Use `--config Release`. | `build\Release\mt5_group_probe.exe` |
| `Ninja` (single-config) | Set `-DCMAKE_BUILD_TYPE=Release` at configure. Must run from a **x64** vcvars prompt (`cl` on PATH). | `build\mt5_group_probe.exe` |
| `Ninja Multi-Config` | `--config Release` at build. | `build\Release\mt5_group_probe.exe` |

Ninja is **not** installed here. Use VS 2022.

Do not use MinGW/Msys generators. `WIN32` would be true so CMake would add the target, but `mt5_manager.cpp` uses `wcsncpy_s`, `Windows.h`, and the MetaQuotes MSVC headers. README: **“MSVC 2022 for local mode (the Manager API is Windows x64 only).”**

### 2.4 What a successful generate must show

CMake should report project `mt5sdk`, C++20, and create `mt5_group_probe.vcxproj` (VS generator). If `MT5SDK_BUILD_PROBES` was forgotten, `--target mt5_group_probe` fails with “target does not exist”. Reconfigure; do not hand-add a vcxproj.

Optional: `-DMT5SDK_BUILD_TESTS=ON` in the same tree is harmless and independent. Leave Postgres/Drogon OFF unless you intend to compile those seams.

### 2.5 Expected artifacts after `--config Release --target mt5_group_probe`

| Path | Who puts it there |
|---|---|
| `build\Release\mt5_group_probe.exe` | `add_executable` |
| `build\Release\mt5sdk.lib` | `add_library(mt5sdk STATIC …)` (also `build\mt5sdk.dir\Release\`) |
| `build\Release\MT5APIManager64.dll` | `mt5sdk_copy_runtime_dlls` POST_BUILD |
| `build\Release\MetaQuotes.MT5ManagerAPI64.dll` | same |
| `build\Release\MetaQuotes.MT5CommonAPI64.dll` | same |
| `build\Release\libcurl.dll`, `spdlog.dll`, `fmt.dll`, `z.dll`, `libcrypto-3-x64.dll`, `libssl-3-x64.dll`, brotli, … | vcpkg applocal (`VCPKG_APPLOCAL_DEPS` default ON for Windows). **Not** listed in `MT5SDK_RUNTIME_DLLS`. |

CMake does **not** copy `MT5APIManager64avx.dll` / `avx2` / `arm`. Vanilla `MT5APIManager64.dll` is enough: `FindLibrary` falls back to it (A105 §3.3; `MT5APIManager.h` 1789–1798).

---

## 3. Target / source graph (probe-only)

```text
mt5_group_probe.exe
  tests/mt5_group_probe.cpp
  PRIVATE → mt5sdk.lib  (STATIC)
              PUBLIC includes: src/, config/, ${MT5_SDK_DIR}/Include
              PUBLIC link:
                nlohmann_json::nlohmann_json
                spdlog::spdlog
                CURL::libcurl
              WIN32 sources:
                config/app_config.cpp
                src/core/chart_timeframe.cpp
                src/core/mt5_http_client.cpp
                src/services/mt5_time_window.cpp
                src/core/mt5_manager.cpp      ← probe actually calls this
                src/core/mt5_pool.cpp
                src/core/mt5_watchdog.cpp
              POST_BUILD copy from ${MT5_SDK_DIR}/Libs:
                MT5APIManager64.dll
                MetaQuotes.MT5ManagerAPI64.dll
                MetaQuotes.MT5CommonAPI64.dll
```

Public include roots (`CMakeLists.txt` 77–81) are why the probe can `#include "../src/core/mt5_manager.h"` **or** `"core/mt5_manager.h"` / `"app_config.h"`. The probe uses the relative `../` form.

---

## 4. `mt5sdk_copy_runtime_dlls` (build-time, not the probe’s `Initialize` path)

```114:129:D:\Prop\mt5-sdk\CMakeLists.txt
set(MT5SDK_RUNTIME_DLLS
    "${MT5_LIB_DIR}/MT5APIManager64.dll"
    "${MT5_LIB_DIR}/MetaQuotes.MT5ManagerAPI64.dll"
    "${MT5_LIB_DIR}/MetaQuotes.MT5CommonAPI64.dll"
)

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

| Fact | Value |
|---|---|
| Source | `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\` (unless `MT5_SDK_DIR` overridden) |
| Dest | `$<TARGET_FILE_DIR:mt5_group_probe>` = beside the exe |
| Mode | `copy_if_different` POST_BUILD |
| Non-Windows | no-op |
| In-tree callers | **only** the two probes |

This is the official copy-dlls contract (A105). There is no `copy-dlls.ps1`.

---

## 5. After the exe exists (run, not build) — pitfalls CMake does not fix

Building the target is **not** a live group list. This section is so an operator does not treat “compiled” as “connected”.

### 5.1 `.env` lookup

`tests/mt5_group_probe.cpp` 28–41:

1. `sourceDir()` = `PROPFIRM_SOURCE_DIR` if that macro is defined, else `"."`.
2. If `{sourceDir}/.env` exists, load it; else load `".env"` (cwd).
3. `AppConfig::load` then prefers **process environment** over the file (`app_config.cpp` 84–90).

CMake never defines `PROPFIRM_SOURCE_DIR`. Measured: `D:\Prop\mt5-sdk\.env` is **ABSENT** (E022). Root `D:\Prop\.env` exists but is **not** on the probe’s default search path unless cwd is `D:\Prop` or the macro is injected. Copy `.env.example` → `mt5-sdk\.env` (gitignored) or export env vars. Do not commit secrets.

Required for a local run (`hasLocalConfig`):

| Key | Role |
|---|---|
| `MT5_MODE` | Must **not** be `remote` (exit 3). Default in `AppConfig` / `.env.example` is `local`. |
| `MT5_SERVER` | Non-empty |
| `MT5_LOGIN` | Non-zero manager login |
| `MT5_PASSWORD` | Non-empty |
| `MT5_PORT` | Default 443 |

Optional: `IS_MT5_PROXY_ENABLED` + `MT5_PROXY_*`. Plan maps `MT5_GROUP_*` are **ignored** by this probe (A18, A39).

### 5.2 `Initialize` path ≠ CMake `Libs` path

Probe (`mt5_group_probe.cpp` 99):

```text
sourceDir() / "MetaTrader5SDK" / "Libs"
```

CMake copies from:

```text
vendor/MetaTrader5SDK/Libs
```

Those are **different relative paths**. CMake does **not** create a `MetaTrader5SDK/` junction at the source root. `Initialize` therefore usually misses step-1 of `FindLibrary` (`MT5APIManager.h` 1786–1798) and falls through to:

1. three parent folders of the **exe** (and `.\libs\` under each);
2. bare `MT5APIManager64.dll` on **PATH** (`FindLibrary` returns `true` even if the file is absent — `MT5APIManager.h` 1829–1831).

POST_BUILD copy beside the exe is what makes step (1) succeed. **Run the Release exe from a cwd that still leaves those DLLs next to it** (or keep cwd = `build\Release`). Do not relocate the exe without the trio + vcpkg applocal DLLs.

Do **not** copy the probe’s `MetaTrader5SDK/Libs` layout into the C# worker (A105). Worker copy source is `vendor/…/Libs`.

### 5.3 Probe exit codes (A18) — not CMake

| Exit | Meaning |
|---:|---|
| 0 | Groups listed |
| 2 | missing creds / sdk_init / proxy / exception |
| 3 | `MT5_MODE=remote` |
| 4 | `Connect` failed |
| 5 | `GetAllGroups` failed after connect |

Stdout is JSON. Passwords are not printed. `spdlog` is forced off.

This agent did **not** execute the probe. Live Manager attach remains **unproven** (C42).

---

## 6. What not to do

| Action | Why |
|---|---|
| `cmake --build …` without `-DMT5SDK_BUILD_PROBES=ON` at **configure** time | Target does not exist. Changing the option requires **reconfigure**, not just rebuild. |
| Configure on WSL/Linux with the same flag and expect this exe | `if(WIN32)` skips the whole probe block. HTTP client still builds. |
| `dotnet` / `Mt5TraderIntelligence.sln` | The C# tree does **not** reference `mt5-sdk`. D66: gitlink only. No `mt5_group_probe` there. |
| `-A Win32` or ARM64-only host | Manager DLLs in the copy list are AMD64 PE32+. ARM variant exists but is **not** copied. |
| `-DVCPKG_TARGET_TRIPLET=x64-windows-static` without a static curl/spdlog install | This host has **dynamic** `x64-windows` packages. README specifies `x64-windows`. |
| `-DMT5SDK_WITH_POSTGRES=ON` “just in case” | Extra `find_package(PostgreSQL)`. Not needed. libpq is installed here but still leave OFF. |
| Hand-write a `.mq5` or mutate files under `mt5-sdk` | Out of scope. Product source stays untouched. |
| Treat a green compile as group-enumeration proof | Probe is a live operator tool. No `add_test`. No exe on disk today. |
| Redistribute `vendor/MetaTrader5SDK/` | README licence: MetaQuotes SDK is **not** ours to sublicense. Keep private. |

---

## 7. Minimal vs full command matrix

| Goal | Configure extras | Build |
|---|---|---|
| **Group probe only (this ticket)** | `-DMT5SDK_BUILD_PROBES=ON` + vcpkg toolchain + `-A x64` | `--config Release --target mt5_group_probe` |
| Both operator probes | same | `--config Release` (no `--target`) or two `--target`s |
| Hermetic unit tests (not this ticket) | `-DMT5SDK_BUILD_TESTS=ON` (probes stay OFF unless also set) | `--config Release` then `ctest --test-dir build -C Release --output-on-failure` |
| Static lib only | no extras | `--config Release --target mt5sdk` |

---

## 8. File pins used

| Path | Role |
|---|---|
| `D:\Prop\mt5-sdk\CMakeLists.txt` | Binding build law (173 lines; SHA-256 above) |
| `D:\Prop\mt5-sdk\README.md` | Documented cmake/vcpkg/MSVC 2022; first snippet omits the probe flag |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | The only source of the exe; live Manager; mapping-blind |
| `D:\Prop\mt5-sdk\config\app_config.cpp` | Env / `.env` key names |
| `D:\Prop\mt5-sdk\.env.example` | Template; `MT5_MODE=local` |
| `D:\Prop\mt5-sdk\.gitignore` | `build/` ignored; `.env` ignored; vendor Libs **tracked** |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | `FindLibrary` / `LoadLibraryW` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll` | Native runtime (7 185 272 B; A105/D66) |

---

## 9. Honesty close

- CMake wiring for `mt5_group_probe` is complete, Windows-gated, opt-in, and copies the three Manager runtime DLLs beside the exe.
- This host **can** compile it (CMake 4.4.0, VS Build Tools 2022 x64, vcpkg `nlohmann-json`/`spdlog`/`curl` for `x64-windows`).
- This host **has not** compiled it in this pass. No `build/` tree, no exe.
- README’s first `cmake -B` example is **insufficient** for the probe; add `-DMT5SDK_BUILD_PROBES=ON`.
- Running the exe is a **separate** operator step (local `.env`, Manager slot, IP whitelist). Not done here.
