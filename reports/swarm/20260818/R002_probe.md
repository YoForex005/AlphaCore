# R002 — How to run live MT5 group discovery (`mt5_group_probe`)

| Field | Value |
|---|---|
| Agent | R002 (operator probe runbook) |
| Date | 2026-08-18 |
| Assigned | Read `D:\Prop\mt5-sdk` tests/probes `mt5_group_probe`. Document how to run live group discovery. Write this report. Do not modify product source. |
| Artifact | `D:\Prop\reports\swarm\20260818\R002_probe.md` |
| Product source modified | **No.** C# product, C++ SDK, tests, CMake, `.env*`, and vendor tree were not edited. |
| Live Manager attach this pass | **Not executed.** This is a runbook, not a `connection.success` proof. |
| Probe binary on disk | **ABSENT** (no `mt5_group_probe.exe` under `D:\Prop`) |
| SDK build tree | **ABSENT** (`D:\Prop\mt5-sdk\build` does not exist) |
| Binding siblings | A14, A18 §3.1, A39, A40, A75, A84, A105 §3.3, C42, D67, D68, E004 §9 |

---

## 0. Verdict (do not greenwash)

Live group discovery for a **manager login** is already implemented as a **Windows-only operator executable**: `mt5_group_probe`. It is **not** CI, **not** `add_test`, **not** the C# `mt5-worker`, and **not** a hosted service.

It enumerates **every client group the connected manager is authorized to see** via `MT5Manager::GetAllGroups` → `IMTManagerAPI::GroupTotal` + `GroupNext`. It is **mapping-blind**: `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` are loaded by `AppConfig` and then **ignored**.

**This pass did not run it.** There is no on-disk probe JSON, no `mt5_group_probe.exe`, and no `mt5-sdk/build`. C42 remains in force: Achiever / StarwaveFX Manager sessions are **not proven**. Filling `.env` and reading this file does not become G01 PASS.

Default CMake (`MT5SDK_BUILD_PROBES=OFF`) **does not build** the probe. An operator must opt in, compile on Windows x64, point credentials at **one** manager login, and run the exe by hand.

---

## 1. What this tool is

| Item | Path / fact |
|---|---|
| Source | `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` (165 lines, 5688 B) |
| SHA-256 | `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` (matches A18 / D66 / E004) |
| CMake target | `mt5_group_probe` |
| Gate | `if(MT5SDK_BUILD_PROBES AND WIN32)` in `D:\Prop\mt5-sdk\CMakeLists.txt` 164–173 |
| CTest | **No.** Real network. Off by default so CI cannot dial a broker. |
| Twin (not this runbook) | `mt5_news_calendar_probe` — news/calendar, different pump, not group discovery |
| Transport | **Local Manager API only** (`MT5Manager`). `MT5_MODE=remote` is a hard refuse (exit 3). |
| Brokers per run | **One.** `AppConfig` has a single `MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD`. Achiever and StarwaveFX need two runs with two env sets. |

README (`D:\Prop\mt5-sdk\README.md` 151–154): the two probes “read `.env`, open a real connection and print a JSON report.”

Header comment in the probe:

> enumerates all groups visible to the configured manager login … Credentials are never echoed: only group names, the server display name, and counts are emitted.

That is the **operator proof** that manager-visible set (A) is non-empty (A39). It is **not** account creation, not plan mapping, not C# ingestion.

---

## 2. Measured tree (2026-08-18)

| Path | Bytes | SHA-256 | Role |
|---|---:|---|---|
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | 5688 | `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` | Probe |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | 6206 | `98345532CA0D33888E919D14F680B933EB60C6C2A2CE85DBBF1F0D05419719E9` | Probe gate + `mt5sdk_copy_runtime_dlls` |
| `D:\Prop\mt5-sdk\README.md` | 6843 | `8E62708EB0DA53E483579A78CECE5B7A981BFD1B05CE91D22A81487538A59D5C` | Build / probe docs |
| `D:\Prop\mt5-sdk\config\app_config.h` | 2824 | `2EE8B969C4B069A340053D6F5A868D1E7E38769F6A8A2AD74C80626D1FF38B83` | Env surface |
| `D:\Prop\mt5-sdk\config\app_config.cpp` | 6370 | `512304425FC42E61563754CF8ED40786E52977A1EF4F975450B1CB6E764FC6BE` | Process env then `.env` then defaults |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | 62958 | `C25AD8CA9ACFBC5B64AB101C5BCDFCD1CF3CA6FE362BFCD2FC84EDC2EA2AFA98` | `Initialize` / `Connect` / `GetAllGroups` |
| `D:\Prop\mt5-sdk\.env.example` | 4999 | `937F7CB0A6912A05BEE0E5B672C696D6D4B41F63FFD530D2451C56715020C47C` | Placeholder template (A18’s “missing” note is **stale**) |
| `D:\Prop\mt5-sdk\tests\mt5_news_calendar_probe.cpp` | 7733 | `006BB24D4F16AAE6D7326461D87241326715F034C681EB3745660CFAF14C3874` | Sibling probe (same DLL / `.env` rules) |

Existence checks (this pass):

| Path | State |
|---|---|
| `D:\Prop\mt5-sdk\build` | **ABSENT** |
| `D:\Prop\mt5-sdk\MetaTrader5SDK\` | **ABSENT** (probe’s `Initialize` path) |
| `D:\Prop\mt5-sdk\.env` | **ABSENT** (gitignored; not created) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll` | **EXISTS** (7 185 272 B) |
| `…\MetaQuotes.MT5ManagerAPI64.dll` | **EXISTS** (396 872 B) |
| `…\MetaQuotes.MT5CommonAPI64.dll` | **EXISTS** (1 046 632 B) |
| Recursive `mt5_group_probe.exe` under `D:\Prop` | **NONE** |
| Host toolchain | CMake at `C:\Program Files\CMake\bin\cmake.exe`; vcpkg at `C:\tools\vcpkg` (`VCPKG_ROOT` unset); VS 2022 BuildTools at `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools`; `cl.exe` not on the default PATH |

Repo-root `D:\Prop\.env` **exists** (3484 B, SHA-256 `A4EF94B990EE389C7E7900B599A60AE10E0C16E96E4B5DA612302759958982D7`, gitignored). Key **names** only: `MT5_MODE=local`, `MT5_PORT=443`, `MT5_SERVER` / `MT5_LOGIN` / `MT5_SERVER_NAME` / several `MT5_GROUP_*` present, `MT5_PASSWORD` present and **redacted** (not the documented `<SECRET>` / `replace_with_manager_password` placeholders). Proxy and remote keys **absent**. **This report does not print values, does not claim they are valid, and does not treat that file as a live attach.** The probe will **not** see this file unless the process cwd is `D:\Prop` or `PROPFIRM_SOURCE_DIR` points there.

---

## 3. Discovery law the probe is proving

A39’s three lists — do not mix them:

| # | Object | What it is |
|---|---|---|
| **A** | `IMTManagerAPI::GroupTotal` / `GroupNext` | **Discovery.** Groups this manager login may see. Server already applied the manager ACL. |
| B | `IMTConManager` allowed-group **masks** | ACL templates (`*`, `demo\*`). Inspect in Administrator; do not re-filter in app code. |
| C | `MT5_GROUP_*` / `MT5AccountHelper::getMt5Group` | **Write-path** names for new / promoted accounts. A **subset** of (A). |

`mt5_group_probe` prints **(A) only**. It never intersects with (C). If the printed list is smaller than expected, fix the manager record in MT5 Administrator (allowed groups / IP whitelist / rights). Do not hard-code plan names into the probe.

`GetAllGroups` (`mt5_manager.cpp` 962–982): lock, require connected, `GroupCreate`, walk `0 .. GroupTotal()`, push UTF-8 `grp->Group()` on `MT_RET_OK`, `Release`, **return true**. Empty-but-successful is valid (ACL may be empty) **and** is also the cold-cache case (see §8).

Remote `MT5HttpClient::GetAllGroups` (`GET /mt5/groups`) exists but this probe **will not call it**. Do not “fix” a remote refuse by silently switching transports (A18 §6).

---

## 4. Control flow (what the exe does)

`spdlog` is forced **off**. Stdout is JSON only (pretty, indent 2). No password, no proxy password, no API key.

```
main
  AppConfig::load(configPath())          # process env > file > defaults
  if mt5_mode == "remote"                → JSON fail, exit 3
  if server empty OR login==0 OR password empty
                                         → ERROR: missing_manager_credentials, exit 2
  MT5Manager::Initialize(sourceDir/MetaTrader5SDK/Libs)
    fail                                 → ERROR: sdk_init_failed, exit 2
  optional SetProxy (if IS_MT5_PROXY_ENABLED)
    incomplete type/address/port         → ERROR: proxy_config_invalid, exit 2
  Connect(L"host:port", login, password, pumpMode=0)
    fail                                 → ERROR: connect_failed [+ connection.sdk_reason], exit 4
  GetAllGroups(groups)
    fail                                 → ERROR: groups_api_unavailable, connection.success=true, exit 5
  sort + unique
  print {probe, connection{success, server}, success, total, groups[]}
  Disconnect
  exit 0
  exception                              → ERROR: exception … / unknown_exception, exit 2
```

`configPath()`:

1. `sourceDir()/.env` if that file exists.
2. Else cwd `./.env`.

`sourceDir()` is the compile-time `PROPFIRM_SOURCE_DIR` string if that macro is defined, else `"."` (**runtime cwd**, not the source tree). **This CMakeLists does not define `PROPFIRM_SOURCE_DIR`.** Unless a parent project adds `-DPROPFIRM_SOURCE_DIR=...`, the probe looks for `.env` and `MetaTrader5SDK/Libs` relative to **whatever directory you launch from**.

`hasLocalConfig` requires all three of: non-empty `MT5_SERVER`, `MT5_LOGIN != 0`, non-empty `MT5_PASSWORD`. Port defaults to **443** if unset.

Connect string is `MT5_SERVER` + `:` + `MT5_PORT` as a wide string. `MT5_SERVER` must be a host or IP **with no scheme** (`.env.example` 23).

`Connect(..., 0)` is **not** request-only at the wrapper. `MT5Manager::Connect` remaps `pumpMode==0` to `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS` (timeout 30 s). That mask **omits** `PUMP_MODE_GROUPS` (`0x00000100`). If that connect fails, it retries with true `pump_mode=0` (request-only). Probe comment “keep traffic minimal” is therefore only half true: first attempt still pumps users/orders/positions/symbols.

On connect failure the JSON may add `connection.sdk_reason` from `GetLastError()` (`mt5ErrorReason`): timeout / IP block / no-connect / wrong credentials / numeric code. Those strings are SDK-derived, not the password.

---

## 5. How to build (Windows x64 only)

Linux / WSL: the native Manager objects and the probe targets are **not compiled** (`if(WIN32)` on `mt5_manager.cpp`; probes require `WIN32`). Do not try to force them into a Linux image (A54).

Host this machine already has: CMake, VS 2022 BuildTools, vcpkg at `C:\tools\vcpkg`. `cl.exe` is not on PATH until the BuildTools environment is loaded.

### 5.1 One-time: Developer PowerShell + vcpkg packages

```powershell
& "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\Launch-VsDevShell.ps1" -Arch amd64
$env:VCPKG_ROOT = "C:\tools\vcpkg"
# first time only:
& "$env:VCPKG_ROOT\vcpkg.exe" install nlohmann-json:x64-windows spdlog:x64-windows curl:x64-windows
```

Postgres / Drogon are **not** required for this probe (`MT5SDK_WITH_POSTGRES` / `MT5SDK_WITH_DROGON` stay OFF).

### 5.2 Configure + build the probe only

From `D:\Prop\mt5-sdk` (PowerShell — do not chain with `&&`):

```powershell
Set-Location D:\Prop\mt5-sdk
cmake -B build -S . `
  -DCMAKE_TOOLCHAIN_FILE=C:\tools\vcpkg\scripts\buildsystems\vcpkg.cmake `
  -DVCPKG_TARGET_TRIPLET=x64-windows `
  -DMT5SDK_BUILD_PROBES=ON `
  -DMT5SDK_BUILD_TESTS=OFF
cmake --build build --config Release --target mt5_group_probe
```

Expected exe (typical MSVC multi-config):

`D:\Prop\mt5-sdk\build\Release\mt5_group_probe.exe`

CMake POST_BUILD (`mt5sdk_copy_runtime_dlls`) copies these **beside the exe**:

- `MT5APIManager64.dll`
- `MetaQuotes.MT5ManagerAPI64.dll`
- `MetaQuotes.MT5CommonAPI64.dll`

Source of those files: `vendor/MetaTrader5SDK/Libs/` (or `-DMT5_SDK_DIR=`). AVX / AVX2 / ARM variants are **not** copied. `FindLibrary` will fall back to the vanilla `MT5APIManager64.dll` when the CPU-preferred name is missing (A105). That is acceptable **if** the vanilla DLL is this same SDK drop (header pin `MTManagerAPIVersion 5570` / `30 Jan 2026`).

Do **not** `ctest` this target. Do **not** add it to `dotnet test`.

Optional: `-DPROPFIRM_SOURCE_DIR=D:/Prop/mt5-sdk` so `.env` resolution is pinned to the SDK tree regardless of cwd. **This tree does not set that today.** Adding it is a product/CMake change — out of scope for this report.

---

## 6. How to configure (no secrets in git)

`AppConfig::load` order per key: **process environment**, then the `.env` file, then the built-in default.

### 6.1 Create a private `.env` the probe can see

`D:\Prop\mt5-sdk\.env` is gitignored and currently **missing**. Operator copy (do not commit):

```powershell
Copy-Item D:\Prop\mt5-sdk\.env.example D:\Prop\mt5-sdk\.env
```

Minimum keys the probe actually **uses**:

```env
MT5_MODE=local
MT5_SERVER=<MT5_MANAGER_HOST>
MT5_PORT=443
MT5_LOGIN=<MANAGER_LOGIN>
MT5_PASSWORD=<SECRET>
```

`MT5_LOGIN` is a **manager** account, not a trading login (`.env.example` 23–24).

Optional proxy (brokers that whitelist one egress IP). Master switch is `IS_MT5_PROXY_ENABLED`. If that is true/1/yes/on, **all** of `MT5_PROXY_TYPE`, `MT5_PROXY_ADDRESS`, and `MT5_PROXY_PORT > 0` are required or the probe exits 2 (`proxy_config_invalid`). Types: `SOCKS5` (default if type string is anything other than `SOCKS4` / `HTTP`), `SOCKS4`, `HTTP`. Login/password on the proxy are optional.

```env
IS_MT5_PROXY_ENABLED=false
MT5_PROXY_TYPE=
MT5_PROXY_ADDRESS=
MT5_PROXY_PORT=0
MT5_PROXY_LOGIN=
MT5_PROXY_PASSWORD=
```

**Unused by this probe (safe to leave, must not be treated as the universe of groups):**

`MT5_GROUP_*`, `MT5_DEFAULT_GROUP`, `MT5_POOL_SIZE`, `MT5_SERVER_NAME`, `MT5_REMOTE_URL`, `MT5_API_KEY`, HTTP pool knobs, `MT5_PASSWORD_ENCRYPTION_KEY`, `DATABASE_*`, `LOG_*`.

`LOG_LEVEL` is ignored: the probe calls `spdlog::set_level(off)` after load.

### 6.2 Two venues = two runs

The product has Achiever and StarwaveFX (architecture §7–§8). The C++ probe has **one** credential triple. To discover both:

```powershell
# example — values from the operator sheet, never committed
$env:MT5_MODE = "local"
$env:MT5_SERVER = "<ACHIEVER_HOST>"
$env:MT5_PORT = "443"
$env:MT5_LOGIN = "<ACHIEVER_MANAGER>"
$env:MT5_PASSWORD = "<SECRET>"
# then run the exe (section 7)

$env:MT5_SERVER = "<STARWAVEFX_HOST>"
$env:MT5_LOGIN = "<STARWAVEFX_MANAGER>"
$env:MT5_PASSWORD = "<SECRET>"
# run again
```

Process env **wins** over `.env`. Unset or overwrite between runs so you do not attach the second venue with the first password.

### 6.3 Do not use `MT5_MODE=remote`

The probe prints this and exits **3**:

```json
{
  "probe": "mt5_group_probe",
  "connection": {
    "success": false,
    "reason": "MT5_MODE=remote: group enumeration requires local manager mode"
  },
  "success": false,
  "total": 0,
  "groups": []
}
```

Remote `GetAllGroups` is a different, untested-by-this-binary path (`GET /mt5/groups`). Remote `GetGroupDetails` is **always false**.

---

## 7. How to run

### 7.1 Recommended (cwd = SDK tree, exe already built)

The probe’s `.env` lookup and the (wrong) `MetaTrader5SDK/Libs` path are both cwd-relative when `PROPFIRM_SOURCE_DIR` is undefined. Launch from `D:\Prop\mt5-sdk` after you have placed `.env` there.

```powershell
Set-Location D:\Prop\mt5-sdk
# After section 5 build:
.\build\Release\mt5_group_probe.exe
# persist the report without putting secrets in chat:
.\build\Release\mt5_group_probe.exe | Set-Content -Encoding utf8 D:\Prop\reports\swarm\20260818\R002_group_probe.json
echo $LASTEXITCODE
```

Do **not** commit the JSON if you later paste live group names that your policy treats as sensitive. Group **names** are not passwords; still keep manager credentials out of the artifact (the probe already does).

### 7.2 Using the repo-root `.env` without copying it

Only if you accept that file as the operator sheet (this report does **not** validate it):

```powershell
Set-Location D:\Prop
D:\Prop\mt5-sdk\build\Release\mt5_group_probe.exe
```

`sourceDir()` is `"."` → `D:\Prop\.env` is used if present. `Initialize` then looks for `D:\Prop\MetaTrader5SDK\Libs` (absent). Init can still succeed because `FindLibrary` walks three parents of the **exe** and CMake already copied `MT5APIManager64.dll` next to it (see §8.1).

### 7.3 Fail-closed dry run (no broker)

Proves the binary starts and the JSON envelope, without a Manager TCP session:

```powershell
Set-Location D:\Prop\mt5-sdk
$env:MT5_MODE = "remote"
.\build\Release\mt5_group_probe.exe
# expect exit 3 and the remote-refuse JSON
Remove-Item Env:MT5_MODE
```

Missing credentials (no `.env`, no process `MT5_SERVER`/`MT5_LOGIN`/`MT5_PASSWORD`) → exit **2**, `ERROR: missing_manager_credentials`. That is the current result if you run from `D:\Prop\mt5-sdk` today (`mt5-sdk\.env` absent).

### 7.4 Preconditions the broker must already have granted

These are Administrator / network facts, not probe flags:

| Check | Why |
|---|---|
| Login is a **manager**, not a trader | `MT_RET_AUTH_MANAGER_NOCONFIG` (1011) / type (1024) |
| This machine’s **egress IP** is on the manager access list | `MT_RET_AUTH_MANAGER_IPBLOCK` (1012). Wrapper maps this to `sdk_reason`. |
| Manager “Groups” ACL is the set you actually want | Server-side filter for (A). `*` = all. |
| `RIGHT_CFG_GROUPS` is **not** required to **read** | Missing it blocks `GroupUpdate`, not `GroupNext`. |
| One free manager **connection slot** | Local mode consumes a slot for the duration of the run. |
| Windows x64 process | Native `MT5APIManager64.dll` is PE64. |

The probe connects, lists, disconnects. It does not subscribe to `IMTConGroupSink`, does not `GroupUpdate`, does not `UserAdd`, does not send trades.

---

## 8. Operator pitfalls (honest)

### 8.1 DLL path: probe string ≠ CMake vendor path

Probe line 99:

`sourceDir() / "MetaTrader5SDK" / "Libs"`

CMake / README vendor path:

`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs`

`D:\Prop\mt5-sdk\MetaTrader5SDK` is **ABSENT**. A18/A105 are correct: the probe’s relative layout is **not** the tree as checked in.

`CMTManagerAPIFactory::FindLibrary` (`MT5APIManager.h` 1769–1831) still usually works **after a CMake probe build**:

1. Try `{dll_path}\{avx2|avx|arm|default}`. That miss is expected here.
2. Walk **three parent directories of the exe**, also `{folder}\libs\`, then the vanilla `MT5APIManager64.dll`.
3. If still missing, return **true** with the bare filename and let `LoadLibraryW` search **PATH** (A105: planting risk). `LoadLibraryW` fail → `sdk_init_failed`.

So: **build with `MT5SDK_BUILD_PROBES=ON` and run the exe that still sits next to the POST_BUILD copies.** Do not copy the probe’s `MetaTrader5SDK/Libs` layout into the C# worker. The worker must use `vendor/MetaTrader5SDK/Libs` (A105).

Operator-only layout (optional, **not** done this pass, not a product edit): a junction `mt5-sdk\MetaTrader5SDK` → `mt5-sdk\vendor\MetaTrader5SDK` makes the probe’s first search path exist.

### 8.2 Empty `total` can be a cold cache, not “no groups”

`GetAllGroups` reads the **local pump cache**. The probe never sets `PUMP_MODE_GROUPS` and never calls `GroupRequestArray(L"*")` (A39 §5 recommended fallback). Therefore:

| Result | Meaning |
|---|---|
| `success=true`, `total>0`, names listed | **Measured (A).** This is the live proof. |
| `success=true`, `total=0`, `groups=[]` | **Ambiguous.** Manager ACL empty **or** cache never pumped. **Do not** write “broker has no groups.” |
| `success=false`, `groups_api_unavailable` | Disconnected / `GroupCreate` null. `connection.success` is still true if connect worked. |
| `success=false`, `connect_failed` | No session. Read `sdk_reason`. |

A later product enumerator must implement A39’s `GroupRequestArray("*")` when `GroupTotal()==0`. **Do not change the probe in this pass** (product source frozen). Treat empty success as **unproven completeness**.

### 8.3 Cwd / `.env` mismatch

| Launch cwd | `.env` the probe sees (no `PROPFIRM_SOURCE_DIR`) | Typical fail |
|---|---|---|
| `D:\Prop\mt5-sdk` | `mt5-sdk\.env` (currently **absent**) then cwd `.env` | exit 2 missing credentials, unless process env is set |
| `D:\Prop` | `D:\Prop\.env` (exists, unvalidated) | may connect if those values are real |
| `D:\Prop\mt5-sdk\build\Release` | `build\Release\.env` (absent) | exit 2 unless process env is set |

Process environment always overrides the file.

### 8.4 Wrapper remaps `pumpMode=0`

Do not document “request-only, no pump” as the measured connect. First attempt pumps users/orders/positions/symbols (still **not** groups). That consumes more server work and still does not fill the group cache. Fallback to true `0` only if that first connect fails.

### 8.5 Not the C# product

C# today does **not** call this binary. `TraderIntelligence.Mt5` live Manager attach is **MISSING** (C42: only `FakeMt5BrokerConnector`). Dashboard “connected” is not a probe. Do not run this exe and then tick G01 from a React cell.

---

## 9. JSON contract and exit codes

Envelope always includes `probe`, `connection`, `success`, `total`, `groups`.

| Exit | `success` | `connection.success` | `connection.reason` / extra | When |
|---:|---|---|---|---|
| 0 | true | true | `connection.server` = `NetworkServer` display name (empty if the SDK returns a host-looking token) | Listed (possibly empty — §8.2) |
| 2 | false | false | `ERROR: missing_manager_credentials` | Server/login/password incomplete |
| 2 | false | false | `ERROR: sdk_init_failed` | Factory `Initialize` / `CreateManager` failed |
| 2 | false | false | `ERROR: proxy_config_invalid` | Proxy enabled but type/address/port incomplete |
| 2 | false | false | `ERROR: exception …` / `ERROR: unknown_exception` | C++ exception |
| 3 | false | false | `MT5_MODE=remote: group enumeration requires local manager mode` | Remote refuse |
| 4 | false | false | `ERROR: connect_failed` + optional `sdk_reason` | Manager TCP / auth / IP / timeout |
| 5 | false | **true** | `ERROR: groups_api_unavailable`; `connection.server` set | Connected but `GetAllGroups` returned false |

Success body:

```json
{
  "probe": "mt5_group_probe",
  "connection": {
    "success": true,
    "server": "<display name from NetworkServer>"
  },
  "success": true,
  "total": 0,
  "groups": []
}
```

`groups` is a sorted, unique UTF-8 string array of `IMTConGroup::Group()` paths (typically `demo\…`, `real\…`, `Flexy\…`). `total` is `groups.size()` **after** unique, not the raw `GroupTotal()`.

**Never** present in stdout: `MT5_PASSWORD`, proxy password, `MT5_API_KEY`, encryption key.

---

## 10. What “done” looks like (and what it is not)

A **measured** live discovery for one venue is **all** of:

1. `mt5_group_probe.exe` built on this Windows host (`MT5SDK_BUILD_PROBES=ON`).
2. Exit code **0**.
3. JSON `connection.success == true` and top-level `success == true`.
4. `total > 0` **or** an independent confirmation that `GroupRequestArray("*")` / Administrator also shows zero (empty ACL). `total == 0` alone is **not** done (§8.2).
5. Artifact saved under `D:\Prop\reports\` (gitignored if it contains venue-specific names you do not want in git).
6. No password in that artifact (already true if you only keep probe stdout).

It is **not**:

- a C# `GetGroups` implementation,
- a `Mt5Group` upsert,
- proof of deal ingestion,
- proof of both brokers from one run,
- a license to enable `REAL_COPY_EXECUTION_ENABLED`,
- an excuse to filter the list down to `MT5_GROUP_*`.

---

## 11. Copy-paste checklist

```text
[ ] Windows x64 + VS 2022 BuildTools + CMake + vcpkg x64-windows (nlohmann-json, spdlog, curl)
[ ] cmake -B build -S D:\Prop\mt5-sdk -DMT5SDK_BUILD_PROBES=ON -DCMAKE_TOOLCHAIN_FILE=<vcpkg> -DVCPKG_TARGET_TRIPLET=x64-windows
[ ] cmake --build build --config Release --target mt5_group_probe
[ ] Confirm build\Release\mt5_group_probe.exe and MT5APIManager64.dll sit together
[ ] Private .env or process env: MT5_MODE=local, MT5_SERVER, MT5_PORT, MT5_LOGIN, MT5_PASSWORD
[ ] MT5_MODE is not remote
[ ] cwd (or PROPFIRM_SOURCE_DIR) actually contains that .env
[ ] Broker has this egress IP on the manager access list
[ ] Manager login has the group ACL you intend to measure
[ ] Run exe; capture stdout + $LASTEXITCODE
[ ] Expect exit 0, success true, groups sorted unique
[ ] If total==0, do not declare “no groups” — cold cache / missing PUMP_MODE_GROUPS
[ ] Repeat with the second broker’s credentials if both venues matter
[ ] Do not commit .env; do not paste passwords into chat or this report
```

---

## 12. Honesty / non-claims

| Claim | This pass |
|---|---|
| “I ran live group discovery” | **False.** Binary not built, not executed. |
| “Achiever groups are …” | **Unknown.** No JSON. |
| “StarwaveFX groups are …” | **Unknown.** No JSON. |
| “C# worker can list groups” | **False** as a live fact (C42). |
| “Remote HTTP can be used for this probe” | **False.** Exit 3 by design. |
| “`.env.example` is missing” (A18 §6) | **Stale.** File is on disk (hash in §2). |
| Product source was patched to fix the DLL path | **False.** Frozen. Documented only. |

**One-liner:** live group discovery is `cmake -DMT5SDK_BUILD_PROBES=ON` then `mt5_group_probe.exe` on Windows with `MT5_MODE=local` and a manager triple the process can see; stdout is a password-free JSON list of **all** manager-visible groups; this agent did not build or attach.

---

## 13. Sources read (not modified)

- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_news_calendar_probe.cpp` (DLL / `.env` twin)
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Prop\mt5-sdk\README.md`
- `D:\Prop\mt5-sdk\config\app_config.{h,cpp}`
- `D:\Prop\mt5-sdk\.env.example`
- `D:\Prop\mt5-sdk\.gitignore`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.{h,cpp}` (`Initialize`, `Connect`, `GetAllGroups`, `GetLastError`)
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` (`GetAllGroups` / `GetGroupDetails` — unused by this probe)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`FindLibrary`, `PUMP_MODE_GROUPS`, `GroupTotal`/`GroupNext`)
- `D:\Prop\reports\swarm\20260818\A14_mt5_manager_local.md`, `A18_mt5_sdk_tests.md`, `A39_mt5_group_discovery.md`, `A75_env_example.md`, `A105_windows_dlls.md`, `C42_honesty_no_live_mt5.md`, `E004_tests.md`
