# W500_RESEARCH_62 — YoPips `mt5_group_probe`: ALL groups, never echo passwords (slot 62)

| Field | Value |
|---|---|
| Slot | **62** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_62.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. This report + swarm-index lines are the only writes. |
| Secrets printed | **None.** Key **names** only. No `MT5_PASSWORD`, proxy auth, or `CTRADER_FIX_PASSWORD` values. |
| This slot built/ran the C++ probe or attached Manager | **No.** Source + on-disk census only. |

**Honesty:** the C++ probe is a **groups-only, single-broker** operator tool. A green probe JSON is not “~5,000 accounts discovered.” ALL logins are a **sibling** walk. Wanting copy **and** no loss does not license `35=D`.

---

## 0. Verdict (do not greenwash)

| Claim | Measured this slot |
|---|---|
| YoPips probe enumerates **ALL manager-visible groups** | **Yes, by design.** `MT5Manager::GetAllGroups` → SDK `GroupTotal` + `GroupNext` + `grp->Group()`. Mapping-blind: `AppConfig` loads `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` and the probe **never reads them**. |
| Probe enumerates **ALL manager traders** | **No.** Grep of the probe TU for `UserLogins` / `GetUserLogins` / `GetGroupLogins` / `UserRequest` = **0 hits**. |
| Probe echoes manager / proxy / FIX passwords on stdout | **No.** Success/failure JSON keys are only `{probe, connection{success,reason?,sdk_reason?,server?}, success, total, groups[]}`. `spdlog` is forced **off** at `main` L81 **before** `AppConfig::load`. |
| YoPips probe ≡ Prop `mt5-sdk` probe | **Yes.** Both files are 165 lines, same control flow, same `.env` keys, same `Connect(..., 0)`, same JSON. `GetAllGroups` in both `mt5_manager.cpp` files is the same 962–982 walk. |
| C++ `mt5_group_probe.exe` on disk | **Absent.** YoPips generate graph has `build\mt5_group_probe.vcxproj` (`EXCLUDE_FROM_ALL`). `build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt` is **empty**. Neither `build\Debug\` nor `build\Release\` lists the exe. `D:\Prop\mt5-sdk` has **no** `build/` tree. |
| `Connect(..., pumpMode=0)` is pump-none | **False on first try.** Wrapper remaps `0` → `USERS\|ORDERS\|POSITIONS\|SYMBOLS` (**omits `PUMP_MODE_GROUPS`**), then retries SDK `mode=0`. Empty `groups: []` after a clean connect is **unproven**, not “broker has zero groups.” |
| Product path that **does** fetch ALL groups + ALL traders | C# `NativeMt5BrokerConnector` (`GroupRequestArray("*")` then cache walk; `UserRequestArray` / `UserGetByGroup` / `UserLogins`) + `DealIngestionService.SyncCatalogAsync(..., GetAccountsAsync(null))`. Dual-broker host: `LiveMt5Registration.CreateConnectors`. Operator dump: `tools/LiveBrokerProbe`. |
| Prior live census (not this process) | `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`, `probe: LiveBrokerProbe`, note *“Passwords never written.”* Grep of that file for `"password"` / `"PASSWORD"` = **0 hits**. Achiever **8 / 6512 / 1506**, Starwave **10 / 1948 / 478**, total **18 / 8460 / 1984**. |
| Copy to cTrader can place a live order today | **No.** `SAFE_BY_ABSENCE`: `CTraderFixSession.BuildLogon` emits `(35, "A")` only; `D:\Prop\src` has **0** `SendTrade` / `DealerSend` / `35=D` senders; `RealCopyEnabled` hardcoded `false`; `CanPromoteToLive => false`. |

**One-liner:** the proven probe is local Manager `Initialize` → optional `SetProxy` → `Connect(host:port, login, password, 0)` → `GetAllGroups` → sort/unique → password-free JSON; ALL traders are `UserLogins` / `UserRequestArray` (C# `GetAccountsAsync(null)` already measured 18/8460); `35=D` stays unbuilt.

---

## 1. Files read this slot (absolute)

| Path | Role |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | Assigned probe (165 lines). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy`, `mt5ErrorReason`, `Connect` remap+fallback, `GetServerDisplayName`, `GetUserLogins`, `GetAllGroups`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `GetLastError()` returns `m_lastError` only. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | True pump-none (`Connect(..., 0)`). **Not** used by the probe. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | `.env` keys; `IS_MT5_PROXY_ENABLED`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` | `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` 453–502. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | `GroupTotal` / `GroupNext` / `GroupRequestArray` / `UserLogins`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.vcxproj` | Target generated; exe **not** produced. |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Line-identical twin. |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same `GetAllGroups` 962–982. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | `option(MT5SDK_BUILD_PROBES OFF)` + `if(MT5SDK_BUILD_PROBES AND WIN32)`. |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Completeness: groups **and** traders. |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors (Achiever + Starwave). |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Read-only connector surface (no send). |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)`. |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog then deals; no send. |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType **`A` only**. |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces `RealCopyEnabled = false`. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` at construction. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false`. |
| `D:\Prop\apps\fix-worker\Worker.cs` | Flag true still cannot emit `35=D`. |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Dual-broker dump; `note = "Passwords never written."` |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Prior measured census. |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Human summary of that dump. |

Siblings (quoted, not re-run): `A004_yopips_group_probe.md`, `W500_RESEARCH_2.md`, `W500_RESEARCH_22.md`, `R002_probe.md`, `R006_cmake.md`.

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

### 2.1 How the target exists

| Tree | How the target exists |
|---|---|
| YoPips | `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` — always in the generate graph, **not** built by default (`CMakeLists.txt` 453–502). Sources: probe TU + `mt5_manager.cpp` + `mt5_http_client.cpp` + `app_config.cpp`. POST_BUILD copies `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MetaQuotes.MT5CommonAPI64.dll` beside the exe. |
| Prop `mt5-sdk` | `option(MT5SDK_BUILD_PROBES "…" OFF)` (`CMakeLists.txt` 17) and `if(MT5SDK_BUILD_PROBES AND WIN32)` (164–173). Links `mt5sdk`. Same three DLLs via `mt5sdk_copy_runtime_dlls`. |

This slot listed YoPips `build\`: `mt5_group_probe.vcxproj` is present; `FileListAbsolute.txt` is empty; `build\Debug\` and `build\Release\` contain `propfirm_backend.exe` and unit tests, **not** `mt5_group_probe.exe`. Prop `mt5-sdk` has no `build/` directory.

### 2.2 `main()` control flow (YoPips L80–164)

```
spdlog::set_level(off)                          // L81 — first statement
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

Remote HTTP is a **hard refuse** (L86–90). `MT5HttpClient` is never constructed.

One `AppConfig` triple per process. Achiever and Starwave are **two runs**.

---

## 3. How ALL groups are enumerated (the proven primitive)

### 3.1 Probe call

```129:150:D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp
        std::vector<std::string> groups;
        if (!manager.GetAllGroups(groups)) {
            json result = failure("ERROR: groups_api_unavailable");
            result["connection"]["success"] = true;
            result["connection"]["server"]  = serverDisplay;
            ...
        }

        std::sort(groups.begin(), groups.end());
        groups.erase(std::unique(groups.begin(), groups.end()), groups.end());
        // JSON: probe, connection{success,server}, success, total, groups
```

### 3.2 Wrapper (YoPips and Prop, `mt5_manager.cpp` 962–982)

```962:982:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
    uint32_t total = m_manager->GroupTotal();
    ...
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

| Step | Behavior |
|---|---|
| Guard | `m_manager && m_connected` else **false** (probe exit 5). |
| `GroupCreate()` null | **false**. |
| Loop | Every `GroupNext(i) == MT_RET_OK` pushes UTF-8 `grp->Group()`. Skipped rows are **not** retried. |
| `total == 0` | Still **true** with empty vector. |
| Sort | Probe only (`std::sort` + `std::unique`). Wrapper does not sort. |

Vendor (`MT5APIManager.h` 205–212): cache walk is `GroupTotal` / `GroupNext` / `GroupGet`. The **request** enumerator is `GroupRequestArray(LPCWSTR mask, IMTConGroupArray*)`. **`MT5Manager` never calls `GroupRequestArray`.** Completeness without a group pump is therefore not guaranteed by the C++ wrapper.

This is manager-visible set **(A)**. Server already applied `IMTConManager` ACL **(B)**. Plan env strings **(C)** (`MT5_GROUP_2STEP_DEMO`, …, `MT5_DEFAULT_GROUP`) are **not** consulted.

### 3.3 What is **not** ALL groups

| Anti-pattern | Why |
|---|---|
| Union of `MT5_GROUP_*` | Write-path subset (`app_config.cpp` 156–164). Probe never reads those fields. |
| `MT5_DEFAULT_GROUP` | One label. |
| `IMTConManager::GroupNext` masks (`*`, `demo\*`) | ACL templates, not names. |
| HTTP `GET /mt5/groups` | Probe refuses `MT5_MODE=remote` (exit 3). |
| Hard-coded Achiever/Starwave plan names | Mapping, not discovery. |

### 3.4 Pump remap (copy this or you will lie about empty lists)

```102:122:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
    MTAPIRES res = m_manager->Connect(..., mode, 30000);
    if (res != MT_RET_OK) {
        res = m_manager->Connect(..., 0, 30000);   // true pump-none fallback
```

| Caller passes | First SDK `Connect` | On fail |
|---|---|---|
| `0` (probe L114) | Default pump **without `PUMP_MODE_GROUPS`** | SDK `mode=0` (cache-cold) |
| Need guaranteed group cache | Must pass `PUMP_MODE_GROUPS` (or `FULL`) into the **wrapper** | Fallback is still cache-cold |

True pump-none is the **pool** (`mt5_pool.cpp` 75–77: SDK `mode=0` directly). The probe does **not** use the pool.

Probe comment L113 (“No pump mode required — keep traffic minimal”) is **intent**, not the first SDK call.

**Slot-62 implication:** do not declare “Achiever/Starwave have N groups” from a probe `total: 0`. Re-run with `PUMP_MODE_GROUPS` or use the C# `GroupRequestArray("*")` path.

---

## 4. How passwords are never echoed (measured, not hoped)

The probe **must** hold `MT5_PASSWORD` (and maybe `MT5_PROXY_PASSWORD`) in memory to connect. The safety contract is **stdout / logs never contain those values**.

| Channel | What is written | Password? |
|---|---|---|
| Success JSON (L142–151) | `probe`, `connection.success`, `connection.server`, `success`, `total`, `groups[]` | **No.** No login, no host:port, no proxy stanza. |
| Failure JSON (`failure()`, L68–76) | `reason` = fixed English token; optional `sdk_reason` = `GetLastError()` | **No.** Wrapper strings are code maps, not the secret. |
| `connection.server` | `GetServerDisplayName()` → `NetworkServer`, then **drop** if the string is only `0-9.:[]` (`mt5_manager.cpp` 168–183) | **No.** Bare IP:port is suppressed. |
| `spdlog` | Forced `level::off` at `main` L81 **before** `AppConfig::load` | Wrapper would log login **number** + proxy host:port, never the password. Probe silences even that. |
| `mt5ErrorReason` (L61–68) | 7 / 1012 / 5 / 3 / generic `"… error code N"` | Mentions “login/password” as **words**, never the value. |
| `SetProxy` (L33–57) | `proxy.auth = "login:password"` sent to SDK only; log is `type` + address + port | **No** auth in logs. |
| Exception path (L157–163) | `"ERROR: exception " + e.what()` | `AppConfig::load` / filesystem; `Connect` returns `bool`, does not throw the secret. |
| `hasLocalConfig` (L43–47) | Checks non-empty password, then prints `missing_manager_credentials` | Existence only. |

JSON constructors used on every path have **no** `password` / `login` / `proxy` keys. There is no `dump` of `AppConfig`.

`GetLastError()` (`mt5_manager.h` 38–41) returns `m_lastError`, written **only** when **both** pump and no-pump `Connect` fail (`mt5_manager.cpp` 123–125). It is never the password buffer.

**Copy this pattern for Slot 62 collectors:** persist `{broker, group names, login ints, counts}`. Never persist `MT5_PASSWORD`, `ACHIEVER_PROXY_PASSWORD`, `CTRADER_FIX_PASSWORD`. The live dump already follows that (`LIVE_GROUPS_AND_TRADERS.json` L5: `"Passwords never written. Groups and manager logins only."`; this slot grepped that file for `"password"` / `"PASSWORD"` → **0**).

---

## 5. Connect recipe for Achiever + Starwave (identifiers only)

Shared lifecycle:

1. `MT5Manager::Initialize(dllPath)` — factory + `CreateManager`.
2. Optional `SetProxy` **before** `Connect`.
3. `Connect(L"host:port", login, passwordW, pump)` → SDK `Connect(server, login, password, L"" /* cert */, mode, 30000)` (`mt5_manager.cpp` 114).
4. Request APIs. Always `Disconnect`.

`AppConfig` keys the probe actually reads (`app_config.cpp` 145–172):

| Key | Field |
|---|---|
| `MT5_MODE` | must be `local` (default) |
| `MT5_SERVER` / `MT5_PORT` (default 443) | endpoint |
| `MT5_LOGIN` / `MT5_PASSWORD` | manager triple |
| `IS_MT5_PROXY_ENABLED` | master toggle (**not** `MT5_PROXY_ENABLED`) |
| `MT5_PROXY_TYPE` / `ADDRESS` / `PORT` / `LOGIN` / `PASSWORD` | tunnel |

`MT5_PROXY_ENABLED=true` with `IS_MT5_PROXY_ENABLED` unset leaves the probe on the **direct** path.

Published identifiers (architecture v2; **not** secrets):

| Step | Achiever (this LAN) | StarwaveFX |
|---|---|---|
| Process | run 1 | run 2 (new env) |
| Server:port | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login | `2027` | `9904` |
| Proxy | **HTTP `SetProxy` required** (`IS_MT5_PROXY_ENABLED=true`; allow-list host `81.29.145.69`) | **must not** (`false` / unset) |
| Historical fail if wrong | **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`) | n/a |
| C# product keys (do **not** mix into the C++ probe) | `ACHIEVER_PROXY_*` | `MT5_STARWAVEFX_*`, `ProxyEnabled=false` |

Wrapper connect codes surfaced as `connection.sdk_reason`:

| Code | Wrapper string | Meaning |
|---|---|---|
| **1012** | IP blocked | Egress IP not on manager allow-list (Achiever without HTTP hop on this desktop). |
| **7** | Network timeout | Path / proxy / firewall. |
| **3** | Wrong credentials | Manager login/password rejected. Do **not** “fix” with proxy. |
| **5** | No connection | Wrong host or server down. |

Probe process exits: `0` ok, `2` config/init, `3` remote, `4` connect, `5` groups API.

---

## 6. ALL manager traders — **not** in the probe

Vendor (`MT5APIManager.h` 254): `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)`. Server allocates; caller `Free`s. `group` is a **mask** (`*`, `demo\*`, comma lists).

Wrapper (`mt5_manager.cpp` 315–327):

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is an alias (1015–1016). Empty / false is **not** “zero traders” — the wrapper does not distinguish `MT_RET_OK_NONE`.

**Proven composition for Slot 62:**

```
GetAllGroups(names)            # or GroupRequestArray("*")
for name in names:
    GetUserLogins(toWide(name), logins)   # false → mark group incomplete
    union (broker_id, login)
```

SDK-legal one-shot: `GetUserLogins(L"*")`. Use `*` only as “all groups **this manager may see**.”

Never treat `login` as globally unique. Persist `(broker_id, login)`.

---

## 7. Product path that actually fetches ALL groups + ALL traders

The C++ probe cannot hold two brokers. The C# host can.

`LiveMt5Registration.CreateConnectors` (`LiveMt5Registration.cs` 20–49) builds **two** `NativeMt5BrokerConnector`s:

- Achiever (`BrokerCodes.Achiever` = `"ACHIEVER"`): `MT5_SERVER` / `MT5_PORT` / `MT5_LOGIN` / `MT5_PASSWORD` + `ACHIEVER_PROXY_*` (HTTP).
- Starwave (`BrokerCodes.StarwaveFx` = `"STARWAVEFX"`): `MT5_STARWAVEFX_SERVER` / `PORT` / `LOGIN` / `PASSWORD`, `ProxyEnabled = false`.

`IMt5BrokerConnector` (`Mt5Contracts.cs` 53–63) is **read-only**: Connect / Groups / Accounts / Deals / Positions. **Zero** `SendTrade` / `DealerSend` symbols under `D:\Prop\src` (this slot grepped; **0 hits**).

### 7.1 Groups (more complete than the probe)

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` 144–186):

1. `GroupRequestArray("*")` — the no-pump complete enumerator the C++ wrapper **does not** call.
2. If still empty: `GroupTotal` + `GroupNext` (same walk as the probe).
3. Dedup by name (ordinal ignore-case).

Connect pump is `PUMP_MODE_GROUPS | USERS | POSITIONS` (L89–91), then `PUMP_MODE_NONE` fallback — **fixes** the probe’s missing `PUMP_MODE_GROUPS`.

### 7.2 Traders (the probe never does this)

`GetAccountsAsync(null)` → every group name, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup(gname)`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. merge `UserAccountRequestArray` / `UserAccountGetByGroup`

DTO is login + group + leverage + balances. **No password fields.**

### 7.3 Host + operator dump

`LiveIngestHostedService`: per connector, `Connect` → `SyncCatalogAsync` (groups + **all** accounts) → deal/position sync → score. On catalog fail it logs *“No dummy data will be substituted.”*

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. No `Take(200)` on this path.

`tools/LiveBrokerProbe/Program.cs`: same two connectors; writes `LIVE_GROUPS_AND_TRADERS.json`; stdout is the same payload; `note = "Passwords never written. Groups and manager logins only."` Account rows are `{login, group, leverage, balance, equity}` only.

### 7.4 Prior measured census (not re-run this slot)

From `LIVE_GROUPS_AND_TRADERS.json` (utc `2026-08-18T08:42:16Z`) and `LIVE_MANAGER_FETCH_MEASURED.md`:

| Broker | Connect | Groups | Traders | Open positions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | direct | 10 | 1948 | 478 | 6413.478 |
| **Total** | | **18** | **8460** | **1984** | |

Achiever names (manager-visible only): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave names (remeasured this slot at JSON L45650–45700): `Starwave\cent\FX1\grp1` (11), `grp2` (4), `Starwave\demo\FX2\grp1` (170), `grp2` (1735), `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `LP` (2).

If the server has more groups, they are **outside this manager’s ACL**. That is still “ALL” for Slot 62.

---

## 8. Copy to cTrader must not send live orders (no loss)

Slot 62 is **fetch + persist**. Execution stays off.

| Gate | Evidence |
|---|---|
| `RealCopyEnabled` constructed `false` | `DependencyInjection.cs` 38–41: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| Forced false after any FIX logon | `CTraderFixLogonHostedService.cs` 68–70 |
| Flag default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) |
| Scoring cannot promote LIVE | `BaselineScorer.CanPromoteToLive => false` (L211) |
| Runtime snapshot copy note | `LiveRuntimeStatus.Snapshot`: if flag false → *“NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.”* |
| FIX outbound MsgType | `CTraderFixSession.BuildLogon` L96: `(35, "A")`. Tags: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. **No `D` / `F` / `G`.** |
| `35=D` / `(35, "D")` builder in `Fix.CTrader` | **0** product senders (this slot grepped) |
| `SendTrade` / `DealerSend` under `D:\Prop\src` | **0** |
| fix-worker | Even if `CTrader:RealCopyExecutionEnabled=true`, it stamps TRADE `Disconnected` and **has no function** that can emit `35=D` (`Worker.cs` 21–46) |
| MT5 write APIs on the C# connector | **Absent** (`IMt5BrokerConnector` is read-only) |

Honest operating mode until risk + recon + §68/§70 PASS:

```
ALLOW:  Manager read (groups, users, deals, positions)
        FIX TLS Logon 35=A (QUOTE/TRADE) for session proof / later recon
FORBID: NewOrderSingle 35=D, cancel/replace 35=F/G,
        DealerSend / SendTrade on source or dest,
        REAL_COPY_EXECUTION_ENABLED=true as a license to trade
```

A logon is **not** a copy. Flipping the flag **cannot** place an order today (`SAFE_BY_ABSENCE`). That is how this tree avoids a live loss on Slot 62. It is **not** a finished no-loss copy engine.

---

## 9. Copy-paste operator sequence (no secrets)

**C++ probe (groups only, one broker per run):**

1. `MT5_MODE=local`.
2. Achiever: `IS_MT5_PROXY_ENABLED=true`, `MT5_PROXY_TYPE=HTTP`, allow-list host/port, proxy auth in env; `MT5_SERVER` / `MT5_PORT=443` / `MT5_LOGIN` / `MT5_PASSWORD` in env.
3. Build YoPips target `mt5_group_probe` (or Prop `-DMT5SDK_BUILD_PROBES=ON` + `--target mt5_group_probe`). Default `ALL_BUILD` will **not** produce it (`EXCLUDE_FROM_ALL` / option OFF).
4. Run; expect JSON `{success:true, total:N, groups:[...]}` with **no** secret keys.
5. Starwave: **new** env, proxy **off**, repeat.
6. If Achiever `sdk_reason` contains 1012: proxy toggle/key wrong. If 3: manager password, not proxy. If `groups: []` after success: **unproven** — add `PUMP_MODE_GROUPS` or use C# `GroupRequestArray`.

**ALL traders (same session, not the stock probe binary):**

```
GetAllGroups / GroupRequestArray("*")
for each name: UserLogins / UserRequestArray / UserGetByGroup
persist (broker, login, group) — never passwords
```

**Product host already wired for both brokers:** `AddTraderIntelligence` → two native connectors → `LiveIngestHostedService.SyncCatalogAsync`. Keep `RealCopyEnabled=false`. Do **not** add a `35=D` builder in this slot.

---

## 10. Open items (this pass)

- This agent did **not** rebuild `mt5_group_probe.exe` and did **not** open a Manager TCP session.
- Live 18/8460 numbers are from the earlier `LiveBrokerProbe` artifact (`2026-08-18T08:42:16Z`), not a new attach.
- C++ `GetAllGroups` after no-pump fallback can be empty while groups exist.
- `IS_MT5_PROXY_ENABLED` vs `MT5_PROXY_ENABLED` vs `ACHIEVER_PROXY_*` remains a foot-gun.
- Probe DLL path `sourceDir()/MetaTrader5SDK/Libs` is the YoPips layout; Prop CMake copies from `vendor/MetaTrader5SDK/Libs`. Do not copy the probe’s relative path into the C# worker.
- YoPips `build\mt5_group_probe.vcxproj` exists but the exe was never (or is no longer) on disk.
- §68 / §70 live-send scorecards remain FAIL; Slot 62 must not wait on them and must not “fix” them by sending.

---

## 11. Sources (absolute)

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\mt5_group_probe.vcxproj.FileListAbsolute.txt`
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Contracts\Mt5Contracts.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
