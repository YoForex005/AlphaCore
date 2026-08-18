# W500_RESEARCH_162 — YoPips `mt5_group_probe`: ALL groups, never echo passwords

| Field | Value |
|---|---|
| Slot | **162** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_162.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. Writes: this report + `INDEX.md` / `SWARM_LOG.md` pins. |
| Secrets printed | **None.** Key **names** only. No `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `MT5_PROXY_PASSWORD`, `ACHIEVER_PROXY_PASSWORD`, or `CTRADER_FIX_PASSWORD` values. |
| This slot built/ran the C++ probe or attached Manager | **No.** Source + on-disk census only. |

**Honesty:** the C++ probe is a **groups-only, single-broker** operator tool. A green `{success:true, total:N}` JSON is **not** “~5,000 accounts discovered.” ALL logins are a **sibling** request walk (`UserLogins` / `UserRequestArray`). Wanting copy **and** no loss does **not** license `35=D`.

Siblings on the same assigned topic (re-read this slot; **not** used as proof of a new attach): `W500_RESEARCH_2.md`, `W500_RESEARCH_22.md`, `W500_RESEARCH_42.md`, `W500_RESEARCH_62.md`, `W500_RESEARCH_82.md`, `W500_RESEARCH_102.md`, `W500_RESEARCH_122.md`, `W500_RESEARCH_142.md`, `A004_yopips_group_probe.md`, `R002_probe.md`.

---

## 0. Verdict (do not greenwash)

| Claim | Measured this slot |
|---|---|
| YoPips probe enumerates **ALL manager-visible groups** | **Yes, by design.** `MT5Manager::GetAllGroups` walks SDK `GroupTotal` + `GroupNext` and pushes UTF-8 `grp->Group()` names. Mapping-blind: `AppConfig` loads `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` and the probe **never reads those fields**. |
| That walk is the official **request** enumerator | **No.** Official request API is `GroupRequestArray` (`MT5APIManager.h` L212). `GroupTotal`/`GroupNext` (L205–206) are the **pump cache**. C++ `GetAllGroups` never calls `GroupRequestArray`. |
| Probe enumerates **ALL manager traders** | **No.** Grep of `tests/mt5_group_probe.cpp` for `UserLogins` / `GetUserLogins` / `GetGroupLogins` / `UserRequest` / `GroupRequestArray` / `MT5_GROUP` / `MT5_DEFAULT` = **0 hits**. Password tokens exist only for connect. |
| Probe echoes manager / proxy / FIX passwords on stdout | **No.** Success/failure JSON keys are only `{probe, connection{success,reason?,sdk_reason?,server?}, success, total, groups[]}`. `spdlog` is forced **off** at `main` L81 **before** `AppConfig::load`. |
| YoPips probe ≡ Prop `mt5-sdk` probe | **Yes.** Both files were read in full this slot (165 lines each). Same control flow, same `.env` keys, same `Connect(..., 0)`, same JSON constructors. Prop SHA-256 previously measured (D66): `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` (5688 B). This slot did **not** re-hash. |
| C++ `mt5_group_probe.exe` on disk | **Absent.** YoPips generate graph has `build\mt5_group_probe.vcxproj` (`EXCLUDE_FROM_ALL` at `CMakeLists.txt` 459). This slot re-read `build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt` (**empty**) and `mt5_group_probe.Build.CppClean.log` (**empty**). `build\Release\` lists `propfirm_backend.exe` + unit tests + Manager DLLs, **not** the probe exe. `D:\Prop\mt5-sdk` has **no** `build/` tree. |
| `Connect(..., pumpMode=0)` is pump-none | **False on first try.** Wrapper remaps `0` → `USERS\|ORDERS\|POSITIONS\|SYMBOLS` and **omits `PUMP_MODE_GROUPS` (0x00000100)**. On fail it retries SDK `mode=0`. Empty `groups: []` after a clean connect is **unproven**, not “broker has zero groups.” |
| One process lists Achiever **and** Starwave | **No.** C++ `AppConfig` is one `(server, login, password)` triple. Slot 162 needs **two env runs**, or the C# dual-connector host. |
| Product path that **does** fetch ALL groups + ALL traders | C# `NativeMt5BrokerConnector` (`GroupRequestArray("*")` then cache walk; `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`) + `DealIngestionService.SyncCatalogAsync(..., GetAccountsAsync(null))`. Dual-broker host: `LiveMt5Registration.CreateConnectors`. Operator dump: `tools/LiveBrokerProbe`. |
| Prior live census (not this process) | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`, `probe: LiveBrokerProbe`, note *“Passwords never written.”* This slot grepped that file for `password` / `PASSWORD` = **0 hits**. Achiever **8 / 6512 / 1506**, Starwave **10 / 1948 / 478**, total **18 / 8460 / 1984**. Per-group counts re-summed below. |
| Copy to cTrader can place a live order today | **No.** `SAFE_BY_ABSENCE`: `CTraderFixSession.BuildLogon` emits `(35, "A")` only; this slot grepped `D:\Prop\src` for `35=D` / `(35, "D")` = **0**; `D:\Prop\src` has **0** `SendTrade` / `DealerSend` / `UserPasswordChange`; `CopyTradingService.NewOrderSingleImplemented = false`; `VenueReconciled = false`; persist `AllowFixSend = false`; `CanPromoteToLive => false`. Residual: DI binds `REAL_COPY_EXECUTION_ENABLED` from config (lab `.env` key may be `true`); that arms a **flag**, not a sender. YoPips `src` has **0** `cTrader` / `35=D` / `NewOrderSingle` / `QuickFIX` hits. |

**One-liner:** the proven C++ probe is local Manager `Initialize` → optional `SetProxy` → `Connect(host:port, login, password, 0)` → `GetAllGroups` (`GroupTotal`+`GroupNext` names only) → sort/unique → password-free JSON; ALL traders are the sibling `UserLogins` / `UserRequestArray` walk (C# `GetAccountsAsync(null)` already measured 18/8460); `35=D` stays unbuilt.

**Risk to capital: NONE.** This slot is read-only research. The product cannot emit `NewOrderSingle`.

---

## 1. Files read this slot (absolute)

| Path | Role |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | Assigned probe (165 lines). Read in full. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy` 33–58, `mt5ErrorReason` 61–69, `Connect` remap+fallback 71–150, `GetServerDisplayName` 168–184, `GetUserLogins` 315–328, `GetAllGroups` 962–982. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `GetLastError()` returns `m_lastError` only. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | True pump-none (`Connect(..., 0)` L74–76). **Not** used by the probe. `GetAllGroups` 663–681 same walk. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp` | Remote `GetAllGroups` 659–664 exists; probe **refuses** `MT5_MODE=remote`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` | `mt5_password` + `MT5_GROUP_*` catalog (probe ignores catalog). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | `.env` keys; `IS_MT5_PROXY_ENABLED` (not `MT5_PROXY_ENABLED`). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` | `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` 453–502. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | `PUMP_MODE_GROUPS=0x00000100` L133; `GroupTotal`/`GroupNext` 205–206; `GroupRequestArray` 212; `UserLogins` 254; `UserRequestArray` 410. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h` | `Group()` L637 is the name; `AuthPasswordMin` L649 is a **policy length**, not a secret. Probe never reads it. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.vcxproj` | Target generated; exe **not** produced. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt` | **Empty** (re-read this slot). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\mt5_group_probe.Build.CppClean.log` | **Empty** (re-read this slot). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Release\` | Manager DLLs + `propfirm_backend.exe` + tests; **no** `mt5_group_probe.exe`. |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Line-identical twin (read in full). |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same `Connect` remap (102–122) and `GetAllGroups` 962–982. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | `option(MT5SDK_BUILD_PROBES OFF)` + `if(MT5SDK_BUILD_PROBES AND WIN32)`. |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Completeness: groups **and** traders. Connect includes `PUMP_MODE_GROUPS`. |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors (Achiever + Starwave). Starwave `ProxyEnabled = false`. |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Read-only connector surface (no send). Account DTO has **no** password field. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)`. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType **`A` only**. 135 lines. |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; log line says NewOrderSingle still unimplemented. |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default **false**. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled` bound from `REAL_COPY_EXECUTION_ENABLED` (not hardcoded). |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented = false`; `VenueReconciled = false`; `AllowFixSend = false`; SHADOW_ONLY. |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Armed-flag note still says no ticket will be sent. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false`. |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Dual-broker dump; `note = "Passwords never written."` |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Prior measured census. |

---

## 2. What the proven probe actually is

Standalone Windows exe. Not CI. Not `add_test`. Not the C# worker. Not a hosted service.

Header contract (both trees, lines 1–8):

```1:8:D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp
// MT5 Group Probe — enumerates all groups visible to the configured manager login.
//
// Standalone tool. Loads `.env` via AppConfig, connects to the MT5 manager
// server, calls MT5Manager::GetAllGroups(), prints sorted group names as a
// JSON array, then exits.
//
// Credentials are never echoed: only group names, the server display name,
// and counts are emitted.
```

YoPips CMake (`CMakeLists.txt` 453–459): `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` — generated into the VS solution, **not** built by default `ALL_BUILD`. POST_BUILD copies `MT5APIManager64.dll` + two MetaQuotes companions beside the exe **if** the target is built.

Prop `mt5-sdk` (`CMakeLists.txt` 17 + 164–173): `option(MT5SDK_BUILD_PROBES OFF)` and `if(MT5SDK_BUILD_PROBES AND WIN32)`. README L151–154: operator diagnostics, excluded from the default build.

This slot listed YoPips `build\Release\`: `propfirm_backend.exe`, unit tests, Manager DLLs — **no** `mt5_group_probe.exe`. FileListAbsolute + CppClean are empty. Prop `mt5-sdk` has no `build/` directory. **This slot did not compile or run the probe.**

---

## 3. How the probe enumerates groups (control flow)

`main` (`mt5_group_probe.cpp` 80–165), in order:

1. **`spdlog::set_level(spdlog::level::off)`** — first statement. Silences `MT5Manager` info/error lines that would otherwise print the **login number** (`mt5_manager.cpp` L148: `"MT5 connected to {} with login {}"`). Password is not in that format string either.
2. **`AppConfig::load(configPath())`** — `PROPFIRM_SOURCE_DIR/.env` if present, else `./.env`. Loads `MT5_SERVER` / `MT5_PORT` / `MT5_LOGIN` / `MT5_PASSWORD` / `MT5_MODE` / `IS_MT5_PROXY_ENABLED` + proxy fields.
3. **Refuse remote.** `MT5_MODE=remote` → JSON failure, **exit 3**. The HTTP client *has* `GET /mt5/groups` (`mt5_http_client.cpp` 659–664) but the probe never constructs `MT5HttpClient`.
4. **Credential presence only.** `hasLocalConfig` requires non-empty `mt5_server`, `mt5_login != 0`, non-empty `mt5_password`. Failure string is `"ERROR: missing_manager_credentials"` — **not** which field, **not** the value. Exit 2.
5. **`MT5Manager::Initialize(sourceDir()/MetaTrader5SDK/Libs)`**. Note: Prop vendored DLLs live at `mt5-sdk/vendor/MetaTrader5SDK/Libs`. The probe’s relative `MetaTrader5SDK/Libs` path matches the YoPips tree, not the Prop vendor layout. Init fail → `"ERROR: sdk_init_failed"`, exit 2.
6. **Optional proxy.** If `mt5_proxy_enabled`, `SetProxy(type, address, port, login, password)`. Default type is **SOCKS5** (`MTProxyInfo::PROXY_SOCKS5`); `"HTTP"` / `"SOCKS4"` override. Invalid tuple → `"ERROR: proxy_config_invalid"`, exit 2. YoPips master toggle is **`IS_MT5_PROXY_ENABLED`** (`app_config.cpp` 172), **not** `MT5_PROXY_ENABLED`. C# Achiever uses `ACHIEVER_PROXY_*` + `PROXY_HTTP`. Do not copy the probe’s SOCKS5 default onto Achiever.
7. **`Connect(server:port, login, password, 0)`**. Password exists only as a local `wstring` passed into the Manager API. It is never inserted into JSON.
8. **On connect fail:** `failure("ERROR: connect_failed")` plus optional `connection.sdk_reason = manager.GetLastError()`. `GetLastError()` returns `m_lastError`, which `Connect` sets via `mt5ErrorReason(res)` — human text + **numeric retcode only** (7 / 1012 / 5 / 3 / default). Comment at L117–118: SDK reason “never contains the password.” Exit 4.
9. **`GetServerDisplayName()`** — `NetworkServer` UTF-8, trimmed; if the string is only digits/`.`/`:`/`[]` it returns empty (hides a raw IP:port). Success JSON uses this as `connection.server`.
10. **`GetAllGroups(groups)`**. Fail → `"ERROR: groups_api_unavailable"` with `connection.success=true` + `server`, then `Disconnect`, exit 5.
11. **`sort` + `unique`**. Dedupes names. Mapping catalog is not applied.
12. **Stdout JSON** `{probe, connection{success,server}, success, total, groups}` then `Disconnect`, exit 0.

Exit map: **0** ok · **2** missing/init/proxy/exception · **3** remote · **4** connect · **5** groups API.

---

## 4. How passwords stay off the wire / stdout

Measured silence, not a hope:

| Channel | What happens |
|---|---|
| Success JSON | Keys: `probe`, `connection.success`, `connection.server`, `success`, `total`, `groups[]`. **No** `password`, `login`, `mt5_password`, `proxy`, `auth`. |
| Failure JSON | `probe`, `connection.success=false`, `connection.reason`, `success=false`, `total=0`, `groups=[]`. Connect-fail may add `connection.sdk_reason` (retcode prose). |
| `spdlog` | Forced **off** before `.env` load. Even if it were on, `Connect` logs server + **login id**, never the password. `SetProxy` logs type + address + port, never `auth`. |
| `GetLastError` | `m_lastError` from `mt5ErrorReason` (codes 7/1012/5/3/default). No secret interpolation. |
| Group object | Probe reads **`grp->Group()`** only (`MT5APIConfigGroup.h` L637). `AuthPasswordMin` (L649) is a minimum-length **policy**, unused here. |
| AppConfig | Holds `mt5_password` / `mt5_proxy_password` in memory. Probe never dumps the struct. `hasLocalConfig` tests `.empty()` only. |
| Proxy auth | Packed as `login:password` into `MTProxyInfo.auth` for the SDK (`mt5_manager.cpp` 46–49). Never printed. |
| LiveBrokerProbe (sibling dump) | Serializes `login` / `group` / `leverage` / `balance` / `equity` only. File note: *“Passwords never written.”* Grep of `LIVE_GROUPS_AND_TRADERS.json` for `password`/`PASSWORD` = **0**. |
| C# DTOs | `Mt5AccountDto` has Login/GroupName/Leverage/Balance/Equity/Margin/MarginFree/Profit. **No password field.** |

Exception path (`catch`) prefixes `e.what()` with `"ERROR: exception "`. That is still not a password field. If a future wrapper put the password in an exception string, this would leak — current `Connect`/`GetAllGroups` do not.

---

## 5. `GetAllGroups` is a cache walk (completeness caveat)

YoPips / Prop `MT5Manager::GetAllGroups` (`mt5_manager.cpp` 962–982):

```962:982:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint32_t total = m_manager->GroupTotal();
    groups.clear();
    groups.reserve(total);

    IMTConGroup* grp = m_manager->GroupCreate();
    if (!grp) return false;

    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
    grp->Release();

    spdlog::info("MT5 GetAllGroups: returned {} groups", groups.size());
    return true;
}
```

Official Manager header (`MT5APIManager.h`):

| API | Line | Role |
|---|---|---|
| `PUMP_MODE_GROUPS = 0x00000100` | 133 | Pump **group configurations** into cache |
| `GroupTotal` / `GroupNext` | 205–206 | Walk **cache** |
| `GroupRequest` | 208 | Network fetch **one** name |
| `GroupRequestArray(mask, …)` | 212 | Network fetch **mask** (`"*"` = all) |
| `UserLogins(group, …)` | 254 | Network login list for a group |
| `UserRequestArray(group, …)` | 410 | Network full user records |

`Connect` remap (`mt5_manager.cpp` 102–122): caller `pumpMode==0` becomes `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS`. **`PUMP_MODE_GROUPS` is omitted.** If that Connect succeeds, `GroupTotal()` may be **0** even when the manager can see groups via `GroupRequestArray("*")`. On fail, fallback is true SDK `mode=0` (no pump) — still no groups cache.

`MT5Session` (pool) Connects with literal `0` and is **not** on the probe path.

**Implication for “ALL groups”:** the C++ probe *intends* the full manager-visible name list. Completeness is **unproven** until someone either (a) adds `PUMP_MODE_GROUPS` or `GroupRequestArray("*")` to the C++ walk, or (b) uses the C# connector that already does (a). Empty `groups: []` after exit 0 is a **cache miss**, not a census.

C# `NativeMt5BrokerConnector.GetGroupsCore` (L144–186) does the complete thing:

1. `GroupRequestArray("*", arr)` — request path first.
2. Only if that list is empty: `GroupTotal` + `GroupNext` fallback.
3. Connect first try is `PUMP_MODE_GROUPS | USERS | POSITIONS` (L89–92), then `PUMP_MODE_NONE`.

That is why the **proven** Achiever+Starwave census is `LiveBrokerProbe`, not `mt5_group_probe.exe`.

---

## 6. ALL traders are a sibling walk (not in this binary)

C++ already has the APIs; the probe does not call them.

`MT5Manager::GetUserLogins` (315–328) → SDK `UserLogins(group, raw, total)` → copy + `Free`.  
`GetGroupLogins` (1015–1016) is an alias.  
C# `ReadAccountsForGroup` (`NativeMt5BrokerConnector.cs` 216–233):

1. `UserRequestArray(gname, users)` — primary **network** enumerator.
2. Hard fail (not OK / OK_NONE / NOTFOUND) → `UserGetByGroup` (pump cache; needs `PUMP_MODE_USERS`; Admin-class managers often lack it).
3. If still empty → `UserLogins` + `UserRequestByLogins`.
4. Account balances: `UserAccountRequestArray` then `UserAccountGetByGroup`.

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` and unions by login. `DealIngestionService` L45–49 / L61–62 uses that. `LiveBrokerProbe` L25–26 does the same.

`IMt5BrokerConnector` has **no** send method. Product `src` grep `SendTrade` / `DealerSend` / `UserPasswordChange` = **0**.

---

## 7. Dual-broker: one C++ `.env` cannot do both

C++ `AppConfig` is a **single** triple (`MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD`). One `mt5_group_probe` process = one manager.

C# `LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector`s:

| Broker | Server keys | Proxy |
|---|---|---|
| Achiever | `MT5_SERVER` / `MT5_PORT` / `MT5_LOGIN` / `MT5_PASSWORD` | `ACHIEVER_PROXY_*` (HTTP when enabled) |
| StarwaveFX | `MT5_STARWAVEFX_*` | **`ProxyEnabled = false` hardcoded** (L45). Do not reuse Achiever’s HTTP hop. |

`HasRealPasswords` dual-ANDs both password keys (presence / `IsSecret` only). This slot did not print values.

To list ALL Achiever + ALL Starwave groups from the C++ probe you must run it **twice** with two env files. The product host already does both in one process.

---

## 8. Prior live census (re-summed; not re-attached)

File: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`utc`: **2026-08-18T08:42:16.8519545+00:00** · `probe`: `LiveBrokerProbe` · `envLoaded`: true · `note`: *Passwords never written. Groups and manager logins only.*  
This slot grepped that JSON for `password`/`PASSWORD` = **0**.

### Achiever (connected, 7212.6 ms) — 8 groups / 6512 accounts / 1506 positions

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |
| **sum** | **6512** |

2+179+4+5+4+6295+0+23 = **6512**. Matches `accounts`.

### StarwaveFX (connected, 6413.5 ms) — 10 groups / 1948 accounts / 478 positions

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |
| **sum** | **1948** |

11+4+170+1735+22+0+0+4+0+2 = **1948**. Matches `accounts`.

**Totals: 18 groups / 8460 traders / 1984 open positions.**

Zero-account groups (`demo\yo-instant`, three Starwave `real` rows) are still **groups**. A groups-only probe that omitted them would be incomplete. The C# request walk kept them.

This slot **did not** open a Manager TCP session. The 08:42Z JSON is the last measured attach.

---

## 9. Copy to cTrader must not send live orders (no loss)

Wanting “fetch ALL then copy” does **not** turn on `NewOrderSingle`. Measured gates:

| Gate | Location | State |
|---|---|---|
| Outbound FIX MsgType | `CTraderFixSession.BuildLogon` L96 | **`(35, "A")` only.** One `WriteAsync` (the logon). Sockets disposed. |
| `35=D` / `(35, "D")` in `D:\Prop\src` | grep this slot | **0** |
| `NewOrderSingle` identifier as a sender | `CTraderFixSession.cs` 135/135 | **0** (identifier appears only in comments / flags elsewhere) |
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L16 | `const bool` **false** |
| `VenueReconciled` | same L15 | `const bool` **false** |
| Persist `AllowFixSend` | same L192 | written **false** |
| Hypothetical send branch | L198 | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — then still sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (no writer) |
| Else | L204 | `SHADOW_ONLY` + local `ShadowCopyEngine.SimulateEntry` |
| Promotion | `BaselineScorer.CanPromoteToLive` L211 | `=> false`. `FromBaseline` reachable set has **no LIVE**. |
| FIX options default | `CTraderFixOptions.RealCopyExecutionEnabled` L35 | **false** |
| YoPips C++ `src` | grep `cTrader` / `35=D` / `NewOrderSingle` / `QuickFIX` | **0** |
| Product MT5 send | grep `SendTrade` / `DealerSend` | **0** |

**Residual (do not confuse with a sender):** `DependencyInjection.cs` L41 binds `LiveRuntimeStatus.RealCopyEnabled` from config key `REAL_COPY_EXECUTION_ENABLED` (case-insensitive `"true"`). Lab `.env` may set that key true. `LiveRuntimeStatus` then says *“REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”* Older slots that claimed “flag hardcoded false” are **stale**. The **sender is still absent**. `SAFE_BY_ABSENCE` ≠ a go-live PASS.

Logon hosted service (`CTraderFixLogonHostedService.cs` L69) logs QUOTE/TRADE logon bits and repeats that NewOrderSingle is unimplemented. Logon ≠ order.

---

## 10. Recipe to fetch ALL groups + ALL traders (without live send)

Do **not** treat a future `mt5_group_probe.exe` JSON as the dual-broker trader census.

| Goal | Use |
|---|---|
| Operator group names, one broker, password-free stdout | Build YoPips `mt5_group_probe` (`--target mt5_group_probe`) **or** Prop `mt5-sdk` with `-DMT5SDK_BUILD_PROBES=ON` on WIN32. Run twice (Achiever env, Starwave env). Treat empty `groups` as cache-gap until `GroupRequestArray` is added. |
| ALL groups + ALL traders, both brokers, already measured | `tools/LiveBrokerProbe` → `LIVE_GROUPS_AND_TRADERS.json`. `GetGroupsAsync` + `GetAccountsAsync(null)`. |
| Product ingest | `DealIngestionService.SyncCatalogAsync` — same connector surface. |
| Completeness implementation | C# `GetGroupsCore` / `ReadAccountsForGroup`. Prefer **request** APIs (`GroupRequestArray("*")`, `UserRequestArray`). |

Safety while doing that:

- Do not write passwords into reports (LiveBrokerProbe already omits them).
- Do not emit `35=D`.
- Do not flip `NewOrderSingleImplemented`.
- Do not persist `AllowFixSend=true`.
- Do not promote traders to LIVE (`CanPromoteToLive` stays false).
- Starwave: no proxy. Achiever: HTTP proxy keys, not the C++ SOCKS5 default.

---

## 11. What this slot does **not** claim

- That `mt5_group_probe.exe` was built or run here.
- That C++ `GetAllGroups` is complete without `PUMP_MODE_GROUPS` / `GroupRequestArray`.
- That the probe lists traders.
- That one C++ process covers both brokers.
- That `REAL_COPY_EXECUTION_ENABLED=true` in `.env` can send a ticket.
- That 18/8460 was re-attached at slot 162 time — it was **re-summed** from the 08:42Z file.
- That §68 / §70 go-live gates passed. `SAFE_BY_ABSENCE` is the honest send-path state.

---

## 12. Risk to capital

**NONE.**

- This agent did not attach, did not send FIX, did not call Manager trade APIs.
- Product outbound FIX is Logon `35=A` only.
- Copy pipeline writes SHADOW intents and forces `AllowFixSend=false`.
- `NewOrderSingleImplemented` is a compile-time false.
- C++ probe (when built) is read-only group names.

---

## 13. Pin

**Verdict: `CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO`.**

C++ `mt5_group_probe` prints manager-visible group names via `GetAllGroups` (`GroupTotal`+`GroupNext`), never passwords (`spdlog` off; JSON has no secret keys). Traders are a sibling walk (`UserLogins`/`UserRequestArray`) already measured by `LiveBrokerProbe`: Achiever 8/6512, Starwave 10/1948. Probe exe absent (vcxproj generated, FileListAbsolute empty). No `35=D`. `NewOrderSingleImplemented=false`. This slot did not live-attach. Risk to capital **NONE**.
