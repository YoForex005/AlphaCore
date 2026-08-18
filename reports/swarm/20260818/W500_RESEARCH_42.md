# W500 Slot 42 — YoPips `mt5_group_probe`: enumerate ALL groups without echoing passwords

| Field | Value |
|---|---|
| Slot | **42** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_42.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch ALL Achiever + Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Product source modified | **No.** Read-only. |
| Secrets printed | **None.** No manager passwords, no proxy auth, no FIX password, no `.env` values. |
| This slot live-attached? | **No.** Source + on-disk census only. Did not build or run the C++ exe. Did not open Manager TCP or FIX sockets. |

---

## 0. Verdict (do not greenwash)

| Claim | Measured this slot |
|---|---|
| YoPips probe enumerates **ALL manager-visible group names** | **Yes, by design.** `GetAllGroups` → SDK `GroupTotal` + `GroupNext`. Mapping-blind: `AppConfig` loads `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` and the probe **never reads those fields**. |
| Probe enumerates **ALL manager traders** | **No.** The 165-line binary never calls `GetUserLogins` / `GetGroupLogins` / `UserLogins` / `UserRequestArray`. Trader census is a **sibling** walk. |
| Probe echoes passwords | **No.** Stdout JSON is `{probe, connection{success, server\|reason, sdk_reason?}, success, total, groups[]}`. Password is a `Connect` / `SetProxy` argument only. `spdlog` is forced **off** before `AppConfig::load`. |
| YoPips probe ≡ Prop `mt5-sdk` probe | **Yes (line-identical control flow).** Both files are 165 lines; same keys, same exit codes, same `Connect(..., 0)`. D66 previously hashed Prop file `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33`. This slot compared text, did not re-hash. |
| C++ `mt5_group_probe.exe` on this host | **ABSENT.** YoPips `build/Release` and `build/Debug` have Manager DLLs (from other targets) but **no** `mt5_group_probe.exe`. `build/mt5_group_probe.dir/Debug/` has an **empty** `FileListAbsolute.txt` + empty `CppClean.log` — vcxproj exists (`EXCLUDE_FROM_ALL`), never compiled. `D:\Prop\mt5-sdk\build` **does not exist**. |
| ALL Achiever + Starwave groups + traders already measured | **Yes, by C# `LiveBrokerProbe` / `NativeMt5BrokerConnector`, not by this C++ exe.** Artifact `LIVE_GROUPS_AND_TRADERS.json` (`utc` `2026-08-18T08:42:16.8519545+00:00`): Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460**. Grep of that JSON for `password` hits **one** line: the note `"Passwords never written. Groups and manager logins only."` |
| Copy-to-cTrader can send a live order today | **No.** `CTraderFixSession` builds **35=A Logon only**. `src/Fix.CTrader` grep for `NewOrderSingle` / `35=D` hits only a comment + a log line. `RealCopyEnabled` forced **false**. Copy rows are `SHADOW_ONLY`. |

Honest one-liner: **the proven C++ probe prints every group name the manager ACL already allows and never the password; ALL traders need a second request walk (`UserLogins` / `UserRequestArray`) which the C# native connector already ran; cTrader may Logon but cannot place.**

---

## 1. What the YoPips probe actually is

Source (assigned): `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` (165 lines).

Byte-twin: `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`.

Header comment is the contract:

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

CMake (YoPips `CMakeLists.txt` 453–502):

- `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)`
- Sources: `tests/mt5_group_probe.cpp` + `src/core/mt5_manager.cpp` + `src/core/mt5_http_client.cpp` + `config/app_config.cpp`
- `PROPFIRM_SOURCE_DIR="${CMAKE_SOURCE_DIR}"` so `.env` resolves next to the YoPips tree
- POST_BUILD copies `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MetaQuotes.MT5CommonAPI64.dll` beside the exe
- Default `ALL_BUILD` / `Release` **does not** produce the exe (`EXCLUDE_FROM_ALL`)

CMake (Prop `mt5-sdk/CMakeLists.txt` 17, 164–173):

- `option(MT5SDK_BUILD_PROBES … OFF)`
- Target exists only `if(MT5SDK_BUILD_PROBES AND WIN32)`
- Links `mt5sdk` + `mt5sdk_copy_runtime_dlls`

This slot listed:

| Path | `mt5_group_probe.exe` |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Release\` | **ABSENT** (other exes + 3 Manager DLLs present) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Debug\` | **ABSENT** (same) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\` | vcxproj side-effects only; `FileListAbsolute.txt` **empty** |
| `D:\Prop\mt5-sdk\build\` | **directory ABSENT** |

Do not claim this slot ran the C++ probe.

---

## 2. Control flow (the proven recipe)

```
spdlog::set_level(off)                          // silence wrapper logs (Connect/SetProxy/GetAllGroups)
AppConfig::load(PROPFIRM_SOURCE_DIR/.env || ./.env)
  MT5_MODE=="remote"                            → JSON fail, exit 3
  missing server / login==0 / empty password    → ERROR: missing_manager_credentials, exit 2
  Initialize(sourceDir/MetaTrader5SDK/Libs)     → fail → sdk_init_failed, exit 2
  configureProxyIfNeeded                        → SetProxy iff IS_MT5_PROXY_ENABLED
    incomplete type/address/port                → proxy_config_invalid, exit 2
  Connect(L"host:port", login, password, 0)     → fail → connect_failed + sdk_reason, exit 4
  GetAllGroups(groups)                          → fail → groups_api_unavailable (connection.success=true), exit 5
  sort + unique
  print {probe, connection{success,server}, success, total, groups[]}
  Disconnect
  exit 0
```

One `AppConfig` triple per process. Achiever and StarwaveFX are **two runs**, two env sets. The probe never loops brokers.

Remote HTTP is a **hard refuse** (probe lines 86–91). `MT5HttpClient::GetAllGroups` *does* exist (`GET /mt5/groups`, `mt5_http_client.cpp` 659–664) but this binary never constructs `MT5HttpClient`. `GetGroupDetails` on HTTP is unimplemented and always returns false (666–669). The probe is local-manager only.

---

## 3. How ALL groups are enumerated (no allow-list)

### 3.1 Probe call

```129:156:D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp
        std::vector<std::string> groups;
        if (!manager.GetAllGroups(groups)) {
            json result = failure("ERROR: groups_api_unavailable");
            result["connection"]["success"] = true;
            result["connection"]["server"]  = serverDisplay;
            std::cout << result.dump(2) << std::endl;
            manager.Disconnect();
            return 5;
        }

        std::sort(groups.begin(), groups.end());
        groups.erase(std::unique(groups.begin(), groups.end()), groups.end());

        json result = {
            {"probe", "mt5_group_probe"},
            {"connection", {
                {"success", true},
                {"server", serverDisplay}
            }},
            {"success", true},
            {"total", groups.size()},
            {"groups", groups}
        };

        std::cout << result.dump(2) << std::endl;

        manager.Disconnect();
        return 0;
```

`hasLocalConfig` only tests `!mt5_server.empty() && mt5_login != 0 && !mt5_password.empty()`. It does **not** consult `MT5_GROUP_*`. Grep of the probe for `MT5_GROUP_` / `getMt5Group` / `mt5_default_group` is empty.

### 3.2 Wrapper (YoPips `mt5_manager.cpp` 962–982; Prop twin identical)

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

Vendor surface (`MT5APIManager.h` 205–212):

| API | Kind | Probe / wrapper uses? |
|---|---|---|
| `GroupTotal` / `GroupNext` | **Local group cache** | **Yes** — this is the entire C++ walk |
| `GroupGet` / `GroupRequest` | Single name | No |
| `GroupRequestArray(LPCWSTR mask, …)` | **Network** wildcard enumerator (`*` = every group this manager may see) | **No** in C++ wrapper. **Yes** in C# `GetGroupsCore` |

Three lists that must not be mixed:

| Set | What | Probe uses? |
|---|---|---|
| **(A)** `IMTManagerAPI` `GroupTotal` / `GroupNext` / `GroupRequestArray` | Actual `IMTConGroup` names this login may see. **This is ALL groups.** | **Yes** — cache walk only |
| **(B)** `IMTConManager` group masks (`*`, `demo\*`) | ACL templates, not names | No |
| **(C)** `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` | Write-path plan map (subset of A) | Loaded by `AppConfig` 151–164, **ignored** |

`total == 0` still returns **true** (empty vector). Probe then prints `success: true, total: 0, groups: []`. That is **ambiguous**: ACL empty **or** cold cache after no-pump fallback. Completeness without pump is `GroupRequestArray(L"*")`.

`LogAvailableGroups` (wrapper 1089–1106) caps at **50** names and is spdlog-only. Not a discovery API. Probe does not call it.

`GetGroupDetails` (984–1013) walks the same cache and adds currency / company / margin flags. Probe does not call it.

### 3.3 Pump remapping trap (copy this exactly)

Probe comment (line 113): “No pump mode required for group enumeration — keep traffic minimal.” It then passes `pumpMode=0`.

That is **not** pump-none on the first try.

`MT5Manager::Connect` remaps `0` (`mt5_manager.cpp` 102–108):

```102:122:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
    ...
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        ...
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
```

Vendor bits (`MT5APIManager.h` 125–143):

| Flag | Value |
|---|---|
| `PUMP_MODE_USERS` | `0x00000001` |
| `PUMP_MODE_ORDERS` | `0x00000008` |
| `PUMP_MODE_POSITIONS` | `0x00000080` |
| `PUMP_MODE_GROUPS` | `0x00000100` |
| `PUMP_MODE_SYMBOLS` | `0x00000200` |
| `PUMP_MODE_FULL` | `0xffffffff` |

Default remapped mask **omits `PUMP_MODE_GROUPS`**. On first-connect fail the wrapper retries SDK `Connect(..., 0)` = true pump-none (request API only). After that fallback, `GroupTotal` can be 0 even when groups exist.

| Caller passes | First SDK Connect | On fail |
|---|---|---|
| `0` (this probe) | Default pump **without GROUPS** | Request-only `mode=0` |
| Need guaranteed group **cache** | Must pass `PUMP_MODE_GROUPS` (or `FULL`) into the **wrapper** | Fallback is still cache-cold |

C# `NativeMt5BrokerConnector.ConnectCore` (lines 89–101) is the **fixed** recipe:

1. First pump: `PUMP_MODE_GROUPS | USERS | POSITIONS`
2. Fallback: `PUMP_MODE_NONE`
3. `GetGroupsCore` prefers `GroupRequestArray("*")` and only then walks `GroupTotal`/`GroupNext` if the request array is empty

**That** is how the measured 8+10 census was fetched, not by running the C++ exe.

---

## 4. How passwords are not echoed (measured surfaces)

| Surface | What happens | Secret printed? |
|---|---|---|
| File header (lines 7–8) | States credentials are never echoed | words only |
| `spdlog::set_level(off)` **before** `AppConfig::load` (line 81) | Wrapper `Connect` / `SetProxy` / `GetAllGroups` / `Initialize` logs suppressed | no |
| `hasLocalConfig` (43–47) | Checks `!mt5_password.empty()` only | no value |
| `configureProxyIfNeeded` (49–66) | Passes proxy login/password into `SetProxy`; never prints | no |
| `SetProxy` (if spdlog were on) | Logs `type` + **address:port** only (`mt5_manager.cpp` 55–56, 92–95). Auth is written into `MTProxyInfo.auth` as `login:password` for the SDK, never logged | no |
| `Connect` (if spdlog were on) | Logs UTF-8 **server** + **login number** (`mt5_manager.cpp` 148) | no password |
| Failed connect JSON (115–124) | `connection.reason=ERROR: connect_failed`; optional `sdk_reason` = `mt5ErrorReason(code)` | no |
| Success JSON | `server` = `NetworkServer()` display name; `groups[]` = group **names** | no |
| Exception JSON (157–163) | `ERROR: exception ` + `e.what()`. `AppConfig::load` can throw `stoull` on `MT5_LOGIN`; that message does **not** include `MT5_PASSWORD` | no value observed |
| `GetLastError` comment (probe 117–118) | Explicitly: SDK reason string never contains the password | documented |
| JSON keys `password` / `Password` in probe source | **0** (grep: `password` appears only as local `Connect` / `SetProxy` / emptiness-check arguments) | — |
| Success JSON fields **omitted** | manager login number, host:port, proxy auth, `MT5_GROUP_*` map | — |

`mt5ErrorReason` (`mt5_manager.cpp` 61–68):

| Code | Canned text |
|---|---|
| 7 | Network timeout (`MT_RET_ERR_NETWORK`) |
| 1012 | IP blocked (`MT_RET_AUTH_MANAGER_IPBLOCK`) |
| 5 | No connection (`MT_RET_ERR_NOCONNECT`) |
| 3 | `"Wrong credentials … Check MT5 manager login/password in config."` — the **word** password, not the secret |
| other | `"Connection failed with MT5 error code N."` |

`GetServerDisplayName` (`mt5_manager.cpp` 168–184) calls `NetworkServer`. If the result is empty after trim **or** consists only of `0-9.:[]` (bare IP:port), it returns `""`. Success JSON can therefore have an empty `connection.server` without leaking the endpoint.

**Residual (not a password echo):** if `e.what()` ever included a filesystem path that itself contained a secret, the exception arm would forward it. `AppConfig::load` does not throw the password.

---

## 5. How ALL manager traders are enumerated (not in this binary)

Wrapper already exists; probe does not call it.

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
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

`GetGroupLogins` is an alias (`mt5_manager.cpp` 1015–1016). Vendor: `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` (`MT5APIManager.h` 254). `group` is a **mask** (`*`, `demo\*`, comma lists).

Honesty vs some sibling notes: this wrapper does **not** explicitly reject an empty `group` string. It forwards `L""` to the SDK and returns false only if `!m_manager`, `!m_connected`, `res != MT_RET_OK`, or `raw_logins == nullptr`. Do not invent `[]` on false. The safe ALL-logins mask is `UserLogins(L"*")` (still ACL-bounded).

Proven composition (matches measured C# collector):

```
GetAllGroups(names)            # or GroupRequestArray("*")
for name in names:
    UserLogins(name) / UserRequestArray(name)
    union into (broker_code, login)
```

Never treat `login` as globally unique. Persist `(broker_id, login)`.

C# production walk (`NativeMt5BrokerConnector.GetAccountsCore` / `ReadAccountsForGroup`, lines 189–270):

1. If `group` is null → every name from `GetGroupsCore`.
2. Per group: `UserRequestArray` → fallback `UserGetByGroup` → if still empty, `UserLogins` then `UserRequestByLogins`.
3. Pair with `UserAccountRequestArray` / `UserAccountGetByGroup`.
4. Emit login, group, leverage, balance, equity, margin, marginFree, profit — **no password fields**.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs` 31–44, 73–83) serializes `{login, group, leverage, balance, equity}` plus the note `"Passwords never written. Groups and manager logins only."` Hosted ingest (`LiveIngestHostedService` → `DealIngestionService.SyncCatalogAsync` 38–51) is the same pair: `GetGroupsAsync` then `GetAccountsAsync(null)`.

---

## 6. Two-broker operator matrix (identifiers only — no secrets)

C++ `AppConfig` is **single-broker**. Bindings (`app_config.cpp` 145–172):

| Env key | Field |
|---|---|
| `MT5_MODE` (default `local`) | `mt5_mode` |
| `MT5_SERVER` | `mt5_server` |
| `MT5_PORT` (default 443) | `mt5_port` |
| `MT5_LOGIN` | `mt5_login` (`stoull`; empty → 0) |
| `MT5_PASSWORD` | `mt5_password` |
| `IS_MT5_PROXY_ENABLED` (default false) | `mt5_proxy_enabled` |
| `MT5_PROXY_TYPE` / `_ADDRESS` / `_PORT` / `_LOGIN` / `_PASSWORD` | proxy |

Twin key `MT5_PROXY_ENABLED` is **not** read by C++ `AppConfig`.

| Step | Achiever | StarwaveFX |
|---|---|---|
| `MT5_MODE` | `local` | `local` |
| C++ proxy master key | `IS_MT5_PROXY_ENABLED=true` on this LAN | **false** / unset |
| Proxy type | `HTTP` (`PROXY_HTTP=2`) | unused |
| `Connect` host:port (published) | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login (published) | `2027` | `9904` |
| Password / proxy auth | env only; never log | env only; never log |
| Historical first fail if proxy omitted here | **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`) | n/a |
| C# live connector keys | `MT5_*` + `ACHIEVER_PROXY_*` | `MT5_STARWAVEFX_*`, `ProxyEnabled=false` |

C# `LiveMt5Registration.CreateConnectors` (lines 23–47) does **not** read `IS_MT5_PROXY_ENABLED`. Achiever proxy is `ACHIEVER_PROXY_ENABLED` / `HOST` / `PORT` / `USERNAME` / `PASSWORD`. Starwave is hardcoded `ProxyEnabled = false`. Do not mix the C++ toggle into the C# worker without mapping.

---

## 7. Measured census (already on disk — not this slot)

`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, utc `2026-08-18T08:42:16.8519545+00:00`).

This slot re-read the JSON header + both broker summaries. Counts:

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | 1984 | |

Achiever names (manager-visible set only): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23). Sum 2+179+4+5+4+6295+0+23 = **6512**.

Starwave names: `Starwave\cent\FX1\grp1` (11), `grp2` (4), `Starwave\demo\FX2\grp1` (170), `grp2` (1735), `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `Starwave\real\FX3\LP` (2). Sum 11+4+170+1735+22+0+0+4+0+2 = **1948**.

If the trade server has more groups, they are **outside this manager’s ACL**. The probe / connector cannot invent them. That is ALL for these logins.

JSON trader rows: `{login, group, leverage, balance, equity}` — no master/investor password. This slot did not dump login rows.

---

## 8. Copy to cTrader must not send live orders (no loss)

| Gate | Measured |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (`CTraderFixOptions.cs` 32–35) |
| `AddTraderIntelligence` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” (`DependencyInjection.cs` 38–42) |
| `CTraderFixLogonHostedService` | After QUOTE/TRADE `TryLogonAsync`, **forces** `_runtime.RealCopyEnabled = false` (line 68). Log: “NewOrderSingle still disabled”. |
| `CTraderFixSession` | Builds **35=A** Logon only (tags 35=A, 98, 108, 141, 553 username, 554 password). **No** `35=D` field list. Grep of `src/Fix.CTrader` for `NewOrderSingle` / `35=D`: options comment + hosted-service log line only. |
| `apps/fix-worker/Worker.cs` | Reads `CTrader:RealCopyExecutionEnabled` default false. Even if true, **only logs a warning** and stamps both FIX rows `Disconnected` / “NewOrderSingle remains off.” |
| Shadow path | `EfTradingStore` writes `CopyIntent.Status = "SHADOW_ONLY"` (line 307) and `ShadowCopyEngine.SimulateEntry` — in-memory fill, no socket write. |
| Snapshot copyNote | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` (`LiveRuntimeStatus.cs` 42–44) |

Logon ≠ send. A successful FIX 35=A on ports 5211/5212 does **not** arm execution. There is no function that emits FIX `MsgType=D` to a socket. Safety today is **SAFE_BY_ABSENCE** plus explicit `false` flags — not a unit-tested `GuardedNewOrderSingle` choke. Do **not** flip `REAL_COPY_EXECUTION_ENABLED` / `CTrader__RealCopyExecutionEnabled` to true.

The C++ `MT5Manager` **does** implement `SendTrade` / `DealerSendOrder` (`mt5_manager.h` 117–124). The **group probe never calls them** (grep of probe: only `GetAllGroups`). Do not reuse this connect recipe as a dealer session.

---

## 9. Probe exit codes vs MTAPIRES

| Process exit | Meaning |
|---|---|
| 0 | Connected; sorted unique group names printed |
| 2 | Missing creds / SDK init / invalid proxy / exception |
| 3 | `MT5_MODE=remote` |
| 4 | `Connect` false — read `sdk_reason` (3 / 5 / 7 / 1012) |
| 5 | Connected but `GetAllGroups` false |

`GetAllGroups` returns false only when `!m_manager`, `!m_connected`, or `GroupCreate()` fails. Empty cache is still exit 0.

---

## 10. Recipe to copy (collector / next operator run)

**Groups (C++ probe, as written):**

1. `MT5_MODE=local`. Fill `MT5_SERVER` / `MT5_PORT` / `MT5_LOGIN` / `MT5_PASSWORD` for **one** broker.
2. Achiever on this LAN: `IS_MT5_PROXY_ENABLED=true`, `MT5_PROXY_TYPE=HTTP`, address/port/auth in env. Starwave: leave proxy **off**.
3. Build: YoPips `--target mt5_group_probe` (EXCLUDE_FROM_ALL) **or** Prop `-DMT5SDK_BUILD_PROBES=ON` then `--target mt5_group_probe`.
4. Run; persist stdout JSON. Confirm no password keys. Empty `groups[]` after success is **unproven** until `PUMP_MODE_GROUPS` or `GroupRequestArray("*")`.
5. Repeat with the other broker’s env.

**Traders (do not wait for a C++ probe change — already measured in C#):**

1. Same connect as `NativeMt5BrokerConnector` (`GROUPS|USERS|POSITIONS`, then `NONE`).
2. `GroupRequestArray("*")` then per-group `UserRequestArray` / `UserLogins`.
3. Persist `(broker, login, group, leverage, balance, equity)` only.

**cTrader copy (no loss):**

1. QUOTE/TRADE Logon only (`35=A`).
2. Keep `RealCopyEnabled=false`.
3. Write `CopyIntent` `SHADOW_ONLY` + `ShadowOrder` from `ShadowCopyEngine`.
4. Never call `SendTrade` / `DealerSend` / `NewOrderSingle`.

---

## 11. Honesty / open items

- This slot **did not** build or run `mt5_group_probe.exe`.
- This slot **did not** open a new Manager TCP session or a new FIX socket.
- ALL-groups + ALL-traders census is **already proven** on 2026-08-18 by `LiveBrokerProbe` (C#), which implements the probe’s safety contract **plus** `GroupRequestArray` and the trader walk the C++ probe lacks.
- C++ `Connect(0)` is **not** pump-none on the first try; default pump **omits** `PUMP_MODE_GROUPS`.
- C++ `GetAllGroups` after no-pump fallback can be empty while groups exist.
- C++ `AppConfig` cannot hold Achiever and Starwave at once.
- `IS_MT5_PROXY_ENABLED` vs `MT5_PROXY_ENABLED` vs `ACHIEVER_PROXY_*` is a real foot-gun.
- `GetUserLogins` does **not** special-case empty mask (some sibling notes overstated that).
- Live send remains off by **absence** of a 35=D builder, not by a tested refuse-path.
- `fix-worker` currently stamps FIX rows Disconnected even when the hosted logon service may have logged on — extra conservative, not a send path.

---

## 12. Sources (absolute)

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` (453–502)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`Connect`, `SetProxy`, `mt5ErrorReason`, `GetAllGroups`, `GetUserLogins`, `GetServerDisplayName`)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_http_client.cpp` (659–677; unused by probe)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` / `app_config.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Release\` / `build\Debug\` (exe census)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\mt5_group_probe.dir\Debug\`
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\CMakeLists.txt` (17, 164–173)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- Siblings: A004, A39, A84, D66, E002, R002, R006, W500_RESEARCH_22
