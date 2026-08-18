# W500_RESEARCH_182 — YoPips `mt5_group_probe`: ALL groups, never echo passwords

| Field | Value |
|---|---|
| Slot | **182** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_182.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. Writes: this report + `INDEX.md` / `SWARM_LOG.md` lines. |
| Secrets printed | **None.** Key **names** only. No `MT5_PASSWORD`, `MT5_PROXY_PASSWORD`, `ACHIEVER_PROXY_PASSWORD`, or `CTRADER_FIX_PASSWORD` values. |
| This slot built/ran the C++ probe or attached Manager | **No.** Source + on-disk census only. |

**Honesty:** the C++ probe is a **groups-only, single-broker** operator tool. A green `{success:true, total:N}` JSON is **not** “~5,000 accounts discovered.” ALL logins are a **sibling** request walk. Wanting copy **and** no loss does **not** license `35=D`.

Siblings on the same assigned topic (re-read this slot; not used as proof of a new attach): `W500_RESEARCH_2.md`, `W500_RESEARCH_22.md`, `W500_RESEARCH_62.md`, `W500_RESEARCH_82.md`, `W500_RESEARCH_102.md`, `W500_RESEARCH_122.md`, `W500_RESEARCH_142.md`, `A004_yopips_group_probe.md`, `R002_probe.md`.

---

## 0. Verdict (do not greenwash)

| Claim | Measured this slot |
|---|---|
| YoPips probe enumerates **ALL manager-visible groups** | **Yes, by design.** `MT5Manager::GetAllGroups` → SDK `GroupTotal` + `GroupNext` + UTF-8 `grp->Group()`. Mapping-blind: `AppConfig` loads `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` and the probe **never reads those fields**. |
| Probe enumerates **ALL manager traders** | **No.** Grep of `tests/mt5_group_probe.cpp` for `UserLogins` / `GetUserLogins` / `GetGroupLogins` / `UserRequest` / `GroupRequestArray` / `MT5_GROUP` / `MT5_DEFAULT` = **0 hits**. Password tokens exist only for connect. |
| Probe echoes manager / proxy / FIX passwords on stdout | **No.** Success/failure JSON keys are only `{probe, connection{success,reason?,sdk_reason?,server?}, success, total, groups[]}`. `spdlog` is forced **off** at `main` L81 **before** `AppConfig::load`. |
| YoPips probe ≡ Prop `mt5-sdk` probe | **Yes.** Both files were read in full this slot (165 lines each). Same control flow, same `.env` keys, same `Connect(..., 0)`, same JSON constructors. Prop SHA-256 previously measured (D66): `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33` (5688 B, 165 lines). This slot did **not** re-hash. |
| C++ `mt5_group_probe.exe` on disk | **Absent.** YoPips generate graph has `build\mt5_group_probe.vcxproj` (`EXCLUDE_FROM_ALL` at `CMakeLists.txt` 459). This slot re-read `build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt` (**empty**) and `mt5_group_probe.Build.CppClean.log` (**empty**). `build\Debug\` and `build\Release\` list `propfirm_backend.exe` + unit tests, **not** the probe exe. `D:\Prop\mt5-sdk` has **no** `build/` tree. |
| `Connect(..., pumpMode=0)` is pump-none | **False on first try.** Wrapper remaps `0` → `USERS\|ORDERS\|POSITIONS\|SYMBOLS` and **omits `PUMP_MODE_GROUPS` (0x00000100)**. On fail it retries SDK `mode=0`. Empty `groups: []` after a clean connect is **unproven**, not “broker has zero groups.” |
| One process lists Achiever **and** Starwave | **No.** C++ `AppConfig` is one `(server, login, password)` triple. Slot 182 needs **two env runs**, or the C# dual-connector host. |
| Product path that **does** fetch ALL groups + ALL traders | C# `NativeMt5BrokerConnector` (`GroupRequestArray("*")` then cache walk; `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins`) + `DealIngestionService.SyncCatalogAsync(..., GetAccountsAsync(null))`. Dual-broker host: `LiveMt5Registration.CreateConnectors`. Operator dump: `tools/LiveBrokerProbe`. |
| Prior live census (not this process) | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`, `probe: LiveBrokerProbe`, note *“Passwords never written.”* This slot grepped that file for `password` / `PASSWORD` = **0 hits**. Achiever **8 / 6512 / 1506**, Starwave **10 / 1948 / 478**, total **18 / 8460 / 1984**. Per-group counts re-summed below. |
| Copy to cTrader can place a live order today | **No on the product hop.** `CTraderFixSession.BuildLogon` emits `(35, "A")` only; grep of `D:\Prop\src` `*.{cs,json,csproj}` for literal `35=D` / `(35, "D")` = **0**; `D:\Prop\src\Mt5` has **0** `SendTrade` / `DealerSend` / `UserPasswordChange`; `CopyTradingService.NewOrderSingleImplemented = false`; `VenueReconciled = false`; `AllowFixSend` written `false`; `CanPromoteToLive => false`. Residual (not the copy hop): `CTraderFixDemoTestTrade` can `Build("D")` (tag 35 = msgType) but **refuses** non-`demo-` hosts, `live.` senders, and account `1369850`; only caller is `tools/DemoFixTestTrade`. DI may arm `REAL_COPY_EXECUTION_ENABLED`; that is a **flag**, not a sender. |

**One-liner:** the proven probe is local Manager `Initialize` → optional `SetProxy` → `Connect(host:port, login, password, 0)` → `GetAllGroups` → sort/unique → password-free JSON; ALL traders are `UserLogins` / `UserRequestArray` (C# `GetAccountsAsync(null)` already measured 18/8460); `35=D` stays unbuilt.

**Risk to capital: NONE.** This slot is read-only research. The product cannot emit `NewOrderSingle`.

---

## 1. Files read this slot (absolute)

| Path | Role |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | Assigned probe (165 lines). Read in full. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy` 33–58, `mt5ErrorReason` 61–69, `Connect` remap+fallback 71–150, `GetServerDisplayName` 168–184, `GetUserLogins` 315–328, `GetAllGroups` 962–982. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `GetLastError()` returns `m_lastError` only (L38–41). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | True pump-none (`Connect(..., 0)` L74–76). **Not** used by the probe. `GetAllGroups` 663–681 same walk. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp` | Remote `GetAllGroups` 659–664 exists; probe **refuses** `MT5_MODE=remote`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` | `mt5_password` + `MT5_GROUP_*` catalog (probe ignores catalog). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | `.env` keys; `IS_MT5_PROXY_ENABLED` (not `MT5_PROXY_ENABLED`). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` | `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` 453–502. **Not** WIN32-gated (unlike Prop). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | `PUMP_MODE_GROUPS=0x00000100` L133; `GroupTotal`/`GroupNext` 205–206; `GroupRequestArray` 212; `UserLogins` 254; `UserRequestArray` 410. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.vcxproj` | Target generated; exe **not** produced. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt` | **Empty** (re-read this slot). |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Line-identical twin (read in full). |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same `Connect` remap (102–122) and `GetAllGroups` 962–982. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | `option(MT5SDK_BUILD_PROBES OFF)` + `if(MT5SDK_BUILD_PROBES AND WIN32)` 164–173. |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Completeness: groups **and** traders. |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors (Achiever + Starwave). Starwave `ProxyEnabled = false`. |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Read-only connector surface (no send). Account DTO has **no** password field. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)`. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType **`A` only**. One `WriteAsync` (L49). |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; log line says NewOrderSingle still unimplemented. |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled` default **false**. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled` bound from `REAL_COPY_EXECUTION_ENABLED` (not hardcoded). |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented = false`; `VenueReconciled = false`; `AllowFixSend = false`; SHADOW_ONLY. |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Armed-flag note still says no ticket will be sent. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false` L211. |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Dual-broker dump; `note = "Passwords never written."` |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Prior measured census. |
| `D:\Prop\reports\swarm\20260818\D66_sdk.md` | Prior SHA-256 of Prop probe TU. |

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

Control flow measured from the 165-line TU:

```
AppConfig::load(sourceDir/.env else ./.env)
  MT5_MODE=remote                         → fail, exit 3
  missing server / login==0 / empty pwd   → ERROR: missing_manager_credentials, exit 2
  Initialize(sourceDir/MetaTrader5SDK/Libs)
    fail                                  → ERROR: sdk_init_failed, exit 2
  configureProxyIfNeeded                  → SetProxy iff IS_MT5_PROXY_ENABLED
    incomplete type/address/port          → ERROR: proxy_config_invalid, exit 2
  Connect(L"host:port", login, password, 0)
    fail                                  → ERROR: connect_failed [+ sdk_reason], exit 4
  GetAllGroups(groups)
    fail                                  → ERROR: groups_api_unavailable, exit 5
                                          (connection.success=true; server display attached)
  sort + unique names
  print {probe, connection{success,server}, success, total, groups[]}
  Disconnect
  exit 0
```

`spdlog` is forced **off** at L81 before any config load. Stdout is JSON only.

Remote HTTP is a **hard refuse** (L86–90). `MT5HttpClient::GetAllGroups` (`/mt5/groups`) exists in YoPips but this probe will not construct it.

One `AppConfig` triple per process. Achiever and StarwaveFX are **two runs**, two env sets. Do not invent a dual-broker probe in this binary.

CMake delta (measured this slot):

| Tree | How the target is created |
|---|---|
| YoPips `CMakeLists.txt` 459 | Always in generate graph; `EXCLUDE_FROM_ALL` (must `--target`). **No** `WIN32` / `MT5SDK_BUILD_PROBES` gate. |
| Prop `mt5-sdk/CMakeLists.txt` 17 + 164 | `option(MT5SDK_BUILD_PROBES OFF)` and `if(MT5SDK_BUILD_PROBES AND WIN32)`. Default configure **does not** create the target. |

---

## 3. How ALL groups are enumerated (no password on the wire of stdout)

### 3.1 Wrapper walk used by the probe

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

What is **not** copied into the vector:

- passwords (manager, investor, proxy, FIX)
- `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` allow-lists
- user logins
- group currency / company / margin (those live on `GetGroupDetails`, unused by the probe)

`grp->Group()` is the configuration **name** only (`IMTConGroup`). The probe then sorts and `unique`s before print.

### 3.2 Official Manager APIs (header)

| SDK method | Header | Kind | Probe uses it? |
|---|---|---|---|
| `GroupTotal` | `MT5APIManager.h` 205 / 910 | cache count | **Yes**, via `GetAllGroups` |
| `GroupNext` | 206 / 911 | cache walk | **Yes** |
| `GroupRequestArray(mask)` | 212 | network request | **No** (C# completeness path) |
| `UserLogins` | 254 / 1172 | network login list | **No** |
| `UserRequestArray` | 410 / 1173 | network user records | **No** |
| `PUMP_MODE_GROUPS` | 133 `=0x00000100` | pump bit | **Not set** by probe’s wrapper `0` remap |

### 3.3 Pump-mode trap (do not treat wrapper `0` as pump-none)

Probe comment L113: “No pump mode required for group enumeration.” **Intent**, not first-call behavior.

`MT5Manager::Connect` (`mt5_manager.cpp` 102–122):

1. If caller `pumpMode == 0`, remap to `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS`.
2. That remap **omits** `PUMP_MODE_GROUPS`.
3. SDK `Connect(..., remapped, 30000)`.
4. On failure, retry SDK `Connect(..., 0, 30000)` — true pump-none; `GetAllGroups` then walks a **cold cache**.

True first-shot pump-none is `MT5Session::Connect` in `mt5_pool.cpp` L74–76 (`mode=0` with no remap). The probe does **not** use the pool.

C# `NativeMt5BrokerConnector.ConnectCore` is the safer completeness recipe: first pump is `PUMP_MODE_GROUPS | USERS | POSITIONS` (L89–92), then `PUMP_MODE_NONE` fallback; groups are requested with `GroupRequestArray("*")` (L155) and only then cache `GroupTotal`/`GroupNext` (L174–180).

### 3.4 Mapping-blind

`AppConfig::load` fills `mt5_default_group` and `MT5_GROUP_2STEP_*` / `1STEP_*` / `INSTANT_*` / `CORE_*` / `PASSFIRST_*` (`app_config.cpp` 151–164). The probe never reads `config.mt5_group_*` or `config.mt5_default_group`. Printed `groups[]` is **whatever the manager login is allowed to see**.

---

## 4. How passwords stay off stdout

Measured from the 165-line TU + wrappers. Key **names** only.

| Path | What happens | Password on stdout? |
|---|---|---|
| `spdlog::set_level(off)` L81 | Silences `Connect` / `GetAllGroups` / `SetProxy` info/error logs **before** `.env` load | No |
| `hasLocalConfig` L43–47 | Tests `!mt5_password.empty()` only | No |
| `configureProxyIfNeeded` L49–66 | Passes proxy login/password into `SetProxy`; `SetProxy` logs **type + address + port** only (`mt5_manager.cpp` 55–56, 92–95) | No |
| `Connect(..., password, 0)` L111–114 | Wide password is an SDK argument, never a JSON field | No |
| Connect fail L116–124 | `failure("ERROR: connect_failed")` + optional `sdk_reason` from `GetLastError()` | No. `mt5ErrorReason` maps 7/1012/5/3/default to **code strings**. |
| Success JSON L142–151 | `{probe, connection{success,server}, success, total, groups}` | No secret keys |
| `GetServerDisplayName` 168–184 | `NetworkServer` UTF-8; if the string is only `0-9.:[]` it is **dropped** (no raw IP echo as “server”) | No password |
| Exception L157–163 | `e.what()` prefixed `ERROR: exception ` | No password in this TU’s throw sites |
| Remote / missing-creds / init / proxy fail | `failure(reason)` only | No |

JSON constructors this slot counted:

- `failure()` keys: `probe`, `connection.success`, `connection.reason`, `success`, `total`, `groups`
- success keys: `probe`, `connection.success`, `connection.server`, `success`, `total`, `groups`
- connect-fail extra: `connection.sdk_reason`

Zero of: `password`, `mt5_password`, `proxy_password`, `auth`, `554`.

`password` appears in the TU only as:

- L46 emptiness check
- L65 `SetProxy` argument
- L111 local wide string for `Connect`
- L114 `Connect` argument
- L118 comment

`AppConfig` loads `MT5_PASSWORD` / `MT5_PROXY_PASSWORD` into memory. The probe never dumps the struct.

Residual if someone later runs the C++ probe against Achiever: default `proxyType` is `PROXY_SOCKS5` (L57) unless `MT5_PROXY_TYPE` is `HTTP`. C# `NativeMt5BrokerConnector.ApplyProxy` hardcodes `PROXY_HTTP`. Do not copy the SOCKS5 default onto this LAN.

---

## 5. ALL manager traders are a sibling walk

Grep of the assigned TU for `UserLogins` / `GetUserLogins` / `GetGroupLogins` / `UserRequest` / `GroupRequestArray` = **0**.

The proven C++ sibling (not compiled into this exe):

```315:328:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint64_t* raw_logins = nullptr;
    uint32_t total = 0;

    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;

    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is an alias (`mt5_manager.cpp` 1015–1016).

Product completeness (C# `NativeMt5BrokerConnector`):

1. **Groups:** `GroupRequestArray("*")` L155; if empty, `GroupTotal` + `GroupNext` L174–180.
2. **Traders:** `GetAccountsAsync(null)` walks every group name; per group `UserRequestArray` L223; cache `UserGetByGroup` only on hard fail; empty → `UserLogins` + `UserRequestByLogins` L230–232.
3. **Ingest:** `DealIngestionService.SyncCatalogAsync` L45–49 calls `GetGroupsAsync` + `GetAccountsAsync(null)` (no `Take` cap).
4. **Dual broker:** `LiveMt5Registration.CreateConnectors` builds Achiever (optional HTTP proxy) + Starwave (`ProxyEnabled = false` L45).
5. **Operator dump:** `tools/LiveBrokerProbe/Program.cs` writes `LIVE_GROUPS_AND_TRADERS.json` with `note = "Passwords never written. Groups and manager logins only."` Account rows are `{login, group, leverage, balance, equity}` — no password field (`Mt5AccountDto` has none).

---

## 6. Prior live census (not this attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`utc = 2026-08-18T08:42:16.8519545+00:00`  
`probe = LiveBrokerProbe`  
This-slot grep of that file for `password` / `PASSWORD` = **0 hits**.

This slot did **not** re-attach. Counts below are re-summed from the on-disk JSON `groupNames[].accounts`.

### Achiever (HTTP proxy; 8 groups / 6512 traders / 1506 positions)

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

`2+179+4+5+4+6295+0+23 = 6512` matches header `accounts: 6512`.

### StarwaveFX (direct; 10 groups / 1948 traders / 478 positions)

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

`11+4+170+1735+22+0+0+4+0+2 = 1948` matches header `accounts: 1948`.

**Total manager-visible catalog: 18 groups / 8460 traders / 1984 open positions.**

These are **all groups those two manager logins can see**. If the servers have more groups, they are outside this permission set. Do not treat C++ `mt5_group_probe` stdout as this census — the exe is absent and this slot did not run it.

---

## 7. Copy to cTrader must not send live orders (no loss)

Measured this slot (source, not a live TRADE pcap):

| Gate | Evidence | State |
|---|---|---|
| Outbound FIX MsgType | `CTraderFixSession.BuildLogon` L96 is `(35, "A")`. One `WriteAsync` L49. Sockets disposed (`using` TcpClient + SslStream). | Logon only |
| Literal `35=D` in `D:\Prop\src` | Grep `35=D` / `(35, "D")` over `*.{cs,json,csproj}` | **0 hits** |
| Demo operator `Build("D")` | `CTraderFixDemoTestTrade.cs` L139/163/197 via `Build(msgType)` L255 `(35, msgType)`. Caller: `tools/DemoFixTestTrade` only. Gate L43–47 refuses live host/sender/account `1369850`. | **Not** wired into `CopyTradingService` / logon host |
| Native MT5 send in `D:\Prop\src\Mt5` | Grep `SendTrade` / `DealerSend` / `UserPasswordChange` | **0 hits** |
| Sender implemented? | `CopyTradingService.NewOrderSingleImplemented = false` (const L16) | Blocker always present |
| Venue recon | `CopyTradingService.VenueReconciled = false` (const L15) | Blocker always present |
| Risk allow | `AllowFixSend = false` written at persist L192; live-send `if` at L198 is dead | No hop |
| Promotion | `BaselineScorer.CanPromoteToLive => false` L211 | No auto LIVE |
| Armed flag | `DependencyInjection.cs` L41: `RealCopyEnabled = configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` | Flag **may be true**. Does **not** create a sender. |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled` default `false` L35 | Unused by `BuildLogon` |
| Hosted copy | `CopyTradingHostedService` logs “Live NewOrderSingle still blocked”; persist status `SHADOW_ONLY` | Shadow only |
| Hosted FIX | `CTraderFixLogonHostedService` L69: “NewOrderSingle still unimplemented” | Logon only |
| YoPips C++ `src` | Has MT5 `DealerSendOrder` / `SendTrade` for the **prop-firm** product. **0** cTrader FIX `35=D` senders. | Different product |

`LiveRuntimeStatus.Snapshot` copy note when the flag is armed: *“REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”*

Slots that still say “DI hardcodes `RealCopyEnabled=false`” (e.g. 108/122) are **stale on the flag**. They remain correct on **absence of a sender**.

**Do not add `35=D` to satisfy “copy.”** Fetch-all + shadow is the no-loss path.

---

## 8. How to build / run (not executed this slot)

YoPips (target exists in generate graph, `EXCLUDE_FROM_ALL`):

```text
cmake --build D:\Projects\YoPips\Backend\C++ Backend PropFirm\build --config Release --target mt5_group_probe
```

Prop SDK (target **not** in default generate; needs `-DMT5SDK_BUILD_PROBES=ON` **and** WIN32):

```text
cmake -S D:\Prop\mt5-sdk -B D:\Prop\mt5-sdk\build -G "Visual Studio 17 2022" -A x64 -DMT5SDK_BUILD_PROBES=ON
cmake --build D:\Prop\mt5-sdk\build --config Release --target mt5_group_probe
```

Run (local only; one broker per env):

```text
MT5_MODE=local
# fill MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD  (values stay in .env)
# Achiever on this LAN: IS_MT5_PROXY_ENABLED=true + MT5_PROXY_TYPE=HTTP (not SOCKS5 default)
# Starwave: proxy off; do not reuse Achiever hop
.\mt5_group_probe.exe
```

Expected stdout shape (password-free):

```json
{
  "probe": "mt5_group_probe",
  "connection": { "success": true, "server": "<NetworkServer display or omitted>" },
  "success": true,
  "total": 0,
  "groups": []
}
```

This slot produced **no** such JSON. Completeness for **both** brokers + **all** traders remains the C# `LiveBrokerProbe` artifact in §6.

---

## 9. Residuals / do-not-claim

1. This slot did **not** `Connect` to Achiever or Starwave.
2. `mt5_group_probe.exe` is **not** on disk.
3. Wrapper `Connect(0)` remaps and omits `PUMP_MODE_GROUPS`. Do not treat an empty probe list as a broker fact.
4. C++ probe ≠ dual-broker census. Use `LiveMt5Registration` / `LiveBrokerProbe`.
5. C++ `GetAllGroups` is cache (`GroupTotal`/`GroupNext`). Completeness request is `GroupRequestArray("*")`.
6. `REAL_COPY_EXECUTION_ENABLED` may be `true` in `.env` and DI will arm the **flag**. Sender still missing. Keep it that way until §68/§70 + risk/recon sit on the hop.
7. Do not print or log manager / proxy / FIX passwords. The probe’s JSON contract is the pattern to copy.
8. C++ probe default proxy type is SOCKS5. Achiever on this LAN needs HTTP. Do not copy L57 default into the C# worker.
9. `CTraderFixDemoTestTrade.Build("D")` exists as a **demo-gated operator** helper. It is not the copy pipeline. Do not treat “no `35=D` string” as “no MsgType D constructor anywhere.” Do not invoke that tool against live Pepperstone.

---

## 10. Slot 182 close

| Item | Value |
|---|---|
| Verdict | **CONFIRMED_GROUPS_ONLY_NO_PASSWORD_ECHO** |
| Evidence | YoPips + Prop `mt5_group_probe.cpp` L1–165; `GetAllGroups` = `GroupTotal`+`GroupNext`; JSON has no secret keys; `spdlog` off; traders 0 hits in TU; prior LiveBrokerProbe 8/6512 + 10/1948; copy hop `35=A` only; demo helper `Build("D")` ungated from copy |
| Live attach this slot | **No** |
| Probe exe | **Absent** |
| Copy live send | **Impossible today** (`SAFE_BY_ABSENCE`) |
| Risk to capital | **NONE** |
