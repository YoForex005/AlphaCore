# W500 Slot 22 — YoPips `mt5_group_probe`: enumerate ALL groups without echoing passwords

| Field | Value |
|---|---|
| Slot | **22** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_22.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch ALL Achiever + Starwave groups and ALL manager traders; copy to cTrader must not send live orders (no loss). |
| Product source modified | **No.** Read-only. |
| Secrets printed | **None.** No manager passwords, no proxy auth, no FIX password. |
| This slot live-attached? | **No.** Code + on-disk census artifacts only. |

---

## 0. Verdict (do not greenwash)

| Claim | Measured |
|---|---|
| YoPips probe enumerates **ALL manager-visible group names** | **Yes, by design.** `GetAllGroups` → SDK `GroupTotal` + `GroupNext`. Mapping-blind: `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` are loaded by `AppConfig` and **never read** by the probe. |
| Probe enumerates **ALL manager traders** | **No.** The binary never calls `GetUserLogins` / `UserLogins` / `UserRequestArray`. Trader census is a **sibling** walk. |
| Probe echoes passwords | **No.** Stdout JSON is `{probe, connection{success,server\|reason,sdk_reason?}, success, total, groups[]}`. Password is used only as a `Connect` argument. `spdlog` is forced **off**. |
| YoPips probe ≡ Prop SDK probe | **Yes (line-identical).** Both files are 165 lines, same control flow, same JSON keys. Prop SHA-256 previously measured (D66): `040671CAC30929A99181F0C79621B5E2EED36516AF1D8B49DF80B84F0C191E33`. This slot did not re-hash. |
| C++ probe exe on disk this host | **Absent.** YoPips `build/Release` and `build/Debug` have no `mt5_group_probe.exe` (`EXCLUDE_FROM_ALL`). `D:\Prop\mt5-sdk\build` does not exist. |
| ALL Achiever + Starwave groups + traders already measured | **Yes, by C# `LiveBrokerProbe` / `NativeMt5BrokerConnector`, not by this C++ exe.** Artifact `LIVE_GROUPS_AND_TRADERS.json` (utc `2026-08-18T08:42:16Z`): Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460**. Zero `password` keys in that JSON. |
| Copy-to-cTrader can send a live order today | **No.** `CTraderFixSession` builds **35=A Logon only**. No `35=D` / `NewOrderSingle` builder. `RealCopyEnabled` forced **false**. Copy rows are `SHADOW_ONLY`. |

Honest one-liner: **the proven C++ probe prints every group name the manager ACL already allows, never the password; ALL traders require a second request walk (`UserLogins` / `UserRequestArray`) which the C# native connector already ran; cTrader may Logon but cannot place.**

---

## 1. What the YoPips probe actually is

Source: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` (165 lines).

Byte-twin: `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`.

Header comment (lines 1–8) is the contract:

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

CMake (YoPips): `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` in `CMakeLists.txt` 453–502. Operator-only; default `ALL_BUILD` / `Release` does **not** produce the exe. POST_BUILD copies `MT5APIManager64.dll` + two MetaQuotes DLLs beside the exe.

CMake (Prop `mt5-sdk`): target exists only `if(MT5SDK_BUILD_PROBES AND WIN32)` (`CMakeLists.txt` 164–173). Default `OFF`.

This slot listed:

| Path | `mt5_group_probe.exe` |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Release\` | **ABSENT** |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\build\Debug\` | **ABSENT** |
| `D:\Prop\mt5-sdk\build\` | **directory ABSENT** |

Do not claim this slot ran the C++ probe.

---

## 2. Control flow (proven recipe)

```
spdlog::set_level(off)                          // silence wrapper logs
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

Remote HTTP is a **hard refuse** (lines 86–91). There is no group-list path on `MT5HttpClient` that this binary will use.

---

## 3. How ALL groups are enumerated (no allow-list)

### 3.1 Probe call

```129:150:D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp
        std::vector<std::string> groups;
        if (!manager.GetAllGroups(groups)) {
            ...
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
```

### 3.2 Wrapper (identical in YoPips and Prop `mt5_manager.cpp`)

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

Vendor surface (`MT5APIManager.h` 205–212): `GroupTotal` / `GroupNext` read the **local group cache**. `GroupRequestArray(LPCWSTR mask, …)` is the **network** wildcard enumerator (mask `*` = every group this manager may see). The C++ wrapper **does not call** `GroupRequestArray`.

Three lists that must not be mixed (A39):

| Set | What | Probe uses? |
|---|---|---|
| **(A)** `IMTManagerAPI` `GroupTotal`/`GroupNext`/`GroupRequestArray` | Actual `IMTConGroup` names this login may see. **This is ALL groups.** | **Yes** — cache walk only |
| **(B)** `IMTConManager` group masks (`*`, `demo\*`) | ACL templates, not names | No |
| **(C)** `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` | Write-path plan map (subset of A) | Loaded, **ignored** |

`total == 0` still returns **true** (empty vector). Probe then prints `success: true, total: 0, groups: []`. That is **ambiguous**: ACL empty **or** cold cache after no-pump fallback. Completeness without pump is `GroupRequestArray(L"*")`.

`LogAvailableGroups` (wrapper 1089–1106) caps at **50** names. Not a discovery API. Probe does not call it.

### 3.3 Pump remapping trap (copy this exactly)

Probe comment (line 113): “No pump mode required — keep traffic minimal.” It then passes `pumpMode=0`.

`MT5Manager::Connect` **remaps** `0` to `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS` (YoPips / Prop `mt5_manager.cpp` 102–108). That mask **omits** `PUMP_MODE_GROUPS` (`0x00000100`, header line 133). On first-connect fail it retries SDK `Connect(..., 0)` = true pump-none.

| Caller passes | First SDK Connect | On fail |
|---|---|---|
| `0` (this probe) | Default pump **without GROUPS** | Request-only `mode=0` |
| Need guaranteed group **cache** | Must pass `PUMP_MODE_GROUPS` (or `FULL`) into the **wrapper** | Fallback is still cache-cold |

C# `NativeMt5BrokerConnector.ConnectCore` (lines 89–101) is the **fixed** recipe: first pump is `PUMP_MODE_GROUPS | USERS | POSITIONS`, then `PUMP_MODE_NONE`. `GetGroupsCore` then prefers `GroupRequestArray("*")` and only falls back to `GroupTotal`/`GroupNext` if the request array is empty. **That** is how the measured 8+10 census was fetched, not by running the C++ exe.

---

## 4. How passwords are not echoed (measured surfaces)

| Surface | What happens | Password? |
|---|---|---|
| File header | States credentials are never echoed | words only |
| `spdlog::set_level(off)` before `AppConfig::load` | Wrapper `Connect`/`SetProxy`/`GetAllGroups` logs are suppressed | no |
| `hasLocalConfig` | Checks `!mt5_password.empty()` only | no value |
| `configureProxyIfNeeded` | Passes proxy login/password into `SetProxy`; never prints them | no |
| `SetProxy` (if spdlog were on) | Logs `type` + **address:port** only (`mt5_manager.cpp` 55–56, 92–95) | no |
| `Connect` (if spdlog were on) | Logs UTF-8 **server** + **login number** (`mt5_manager.cpp` 148) | no |
| Failed connect JSON | `connection.reason=ERROR: connect_failed`; optional `sdk_reason` = `mt5ErrorReason(code)` — canned strings for 3/5/7/1012, never the secret | no |
| Success JSON | `server` = `NetworkServer()` display name; `groups[]` = group **names** | no |
| Exception JSON | `ERROR: exception ` + `e.what()`. `AppConfig::load` can throw `stoull` on `MT5_LOGIN`; that message does **not** include `MT5_PASSWORD` | no value observed |
| `GetLastError` comment (probe 117–118) | Explicitly: SDK reason string never contains the password | documented |
| JSON keys `password` / `Password` in probe source | **0** (grep: password appears only as local `Connect`/`SetProxy` arguments) | — |

`mt5ErrorReason(3)` text is `"Wrong credentials … Check MT5 manager login/password in config."` — the **word** password, not the secret.

What is **not** printed even on success: manager login number, host:port, proxy auth, `MT5_GROUP_*` map.

**Residual (not a password echo):** if `e.what()` ever included a filesystem path that itself contained a secret, the exception arm would forward it. `AppConfig::load` does not throw the password.

---

## 5. How ALL manager traders are enumerated (not in this binary)

Wrapper already exists; probe does not call it.

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

`GetGroupLogins` is an alias (line 1015–1016). Vendor: `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` (`MT5APIManager.h` 254). `group` is a **mask** (`*`, `demo\*`, comma lists). Empty group → wrapper **false** (do not invent `[]`).

Proven composition (matches measured C# collector):

```
GetAllGroups(names)            # or GroupRequestArray("*")
for name in names:
    UserLogins(name) / UserRequestArray(name)
    union into (broker_code, login)
```

Mask form: `UserLogins(L"*")` = all logins this manager may see. Still ACL-bounded.

Never treat `login` as globally unique. Persist `(broker_id, login)`.

C# production walk (`NativeMt5BrokerConnector.GetAccountsCore` / `ReadAccountsForGroup`, lines 189–270):

1. If `group` is null → every name from `GetGroupsCore`.
2. Per group: `UserRequestArray` → fallback `UserGetByGroup` → if still empty, `UserLogins` then `UserRequestByLogins`.
3. Pair with `UserAccountRequestArray` / `UserAccountGetByGroup`.
4. Emit login, group, leverage, balance, equity, margin — **no password fields**.

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs`) serializes that list with explicit note `"Passwords never written. Groups and manager logins only."` Grep of `LIVE_GROUPS_AND_TRADERS.json` found **one** line containing the substring `password`: the note itself.

Hosted ingest (`LiveIngestHostedService` → `DealIngestionService.SyncCatalogAsync`) is the same pair: `GetGroupsAsync` then `GetAccountsAsync(null)`.

---

## 6. Two-broker operator matrix (identifiers only — no secrets)

C++ `AppConfig` is **single-broker**. Bindings (`app_config.cpp` 145–172): `MT5_SERVER`, `MT5_PORT` (default 443), `MT5_LOGIN`, `MT5_PASSWORD`, `IS_MT5_PROXY_ENABLED` (default false), `MT5_PROXY_*`. Twin key `MT5_PROXY_ENABLED` is **not** read.

| Step | Achiever | StarwaveFX |
|---|---|---|
| `MT5_MODE` | `local` | `local` |
| Proxy master key | `IS_MT5_PROXY_ENABLED=true` on this LAN | **false** / unset |
| Proxy type | `HTTP` (`PROXY_HTTP=2`) | unused |
| `Connect` host:port | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login (published) | `2027` | `9904` |
| Password / proxy auth | env only; never log | env only; never log |
| Historical first fail if proxy omitted here | **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`) | n/a |
| C# live connector keys | `MT5_*` + `ACHIEVER_PROXY_*` | `MT5_STARWAVEFX_*`, `ProxyEnabled=false` |

C# `LiveMt5Registration` does **not** read `IS_MT5_PROXY_ENABLED`. Do not mix the C++ toggle into the C# worker without mapping.

---

## 7. Measured census (already on disk — not this slot)

`D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, utc `2026-08-18T08:42:16.8519545+00:00`).

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | 1984 | |

Achiever names (manager-visible set only): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave names: `Starwave\cent\FX1\grp1` (11), `grp2` (4), `Starwave\demo\FX2\grp1` (170), `grp2` (1735), `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `Starwave\real\FX3\LP` (2).

If the trade server has more groups, they are **outside this manager’s ACL**. The probe / connector cannot invent them. That is ALL for these logins.

JSON trader rows: `{login, group, leverage, balance, equity}` — no master/investor password.

---

## 8. Copy to cTrader must not send live orders (no loss)

| Gate | Measured |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (`CTraderFixOptions.cs` 32–35) |
| `AddTraderIntelligence` | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” (`DependencyInjection.cs` 38–42) |
| `CTraderFixLogonHostedService` | After QUOTE/TRADE `TryLogonAsync`, **forces** `_runtime.RealCopyEnabled = false` (line 68). Log: “NewOrderSingle still disabled”. |
| `CTraderFixSession` | Builds **35=A** Logon (tag 553 username, 554 password). **No** `35=D` field list. Grep of `src/Fix.CTrader` for `NewOrderSingle` / `35=D`: options comment + log line only. |
| `apps/fix-worker/Worker.cs` | Reads `CTrader:RealCopyExecutionEnabled` default false. Even if true, **only logs a warning** and stamps TRADE `Disconnected` / “NewOrderSingle remains off.” |
| Shadow path | `EfTradingStore` writes `CopyIntent.Status = "SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` — in-memory fill, no socket write. |
| Snapshot copyNote | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |

Logon ≠ send. A successful FIX 35=A on ports 5211/5212 does **not** arm execution. There is no function that emits FIX `MsgType=D` to a socket. Safety today is **SAFE_BY_ABSENCE** plus explicit `false` flags — not a unit-tested `GuardedNewOrderSingle` choke. Do **not** flip `REAL_COPY_EXECUTION_ENABLED` / `CTrader__RealCopyExecutionEnabled` to true.

The C++ `MT5Manager` **does** implement `SendTrade` / `DealerSendOrder` (YoPips `mt5_manager.h` 117–124). The **group probe never calls them**. Do not reuse this connect recipe as a dealer session.

---

## 9. Probe exit codes vs MTAPIRES

| Process exit | Meaning |
|---|---|
| 0 | Connected; sorted unique group names printed |
| 2 | Missing creds / SDK init / invalid proxy / exception |
| 3 | `MT5_MODE=remote` |
| 4 | `Connect` false — read `sdk_reason` (3 / 5 / 7 / 1012) |
| 5 | Connected but `GetAllGroups` false |

`sdk_reason` map (`mt5ErrorReason`): **1012** IP blocked (Achiever without HTTP proxy on this LAN); **7** network timeout; **3** wrong credentials; **5** no connect. Anything else: `"Connection failed with MT5 error code N."`

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
- Live send remains off by **absence** of a 35=D builder, not by a tested refuse-path.

---

## 12. Sources (absolute)

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` (453–502)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`Connect`, `SetProxy`, `mt5ErrorReason`, `GetAllGroups`, `GetUserLogins`)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` / `app_config.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\CMakeLists.txt`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- Siblings: A004, A39, A84, E002, R002, R006, R012
