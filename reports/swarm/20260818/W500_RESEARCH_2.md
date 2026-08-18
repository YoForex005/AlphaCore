# W500_RESEARCH_2 — YoPips `mt5_group_probe`: enumerate ALL groups, never echo passwords (slot 2)

| Field | Value |
|---|---|
| Slot | **2** |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_2.md` |
| Assigned | Read YoPips `mt5_group_probe.cpp`. How does a proven probe enumerate all groups without echoing passwords? Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** Read-only. This report is the only write. |
| Secrets printed | **None.** No `MT5_PASSWORD`, no proxy auth, no `CTRADER_FIX_PASSWORD` values. Key **names** only. |
| This pass ran the probe / attached Manager / sent FIX | **No.** Source + prior measured census only. |

**Honesty rule:** the C++ probe is a **groups-only, single-broker** operator tool. A green probe JSON is not “~5,000 accounts discovered.” ALL logins are a **sibling** walk (`UserLogins` / C# `GetAccountsAsync`). Wanting copy **and** no loss does not license `35=D`.

---

## 0. Verdict (do not greenwash)

| Claim | Measured |
|---|---|
| YoPips probe enumerates **ALL manager-visible groups** | **Yes**, via `MT5Manager::GetAllGroups` → SDK `GroupTotal` + `GroupNext`. Mapping-blind: `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` are loaded by `AppConfig` and **ignored**. |
| Probe enumerates **ALL manager traders** | **No.** It never calls `GetUserLogins` / `UserLogins` / `UserGetByGroup`. |
| Probe echoes manager / proxy / FIX passwords | **No.** Stdout JSON keys are `{probe, connection{success,reason?,sdk_reason?,server?}, success, total, groups[]}`. `spdlog` is forced **off**. |
| YoPips probe ≡ Prop `mt5-sdk` probe | **Yes** (same 165-line control flow, same `.env` keys, same `Connect(..., 0)`, same JSON). |
| One process lists Achiever **and** Starwave | **No** (C++ `AppConfig` is one triple). Slot 2 needs **two env runs**, or the C# dual-connector host. |
| `Connect(..., pumpMode=0)` is pump-none | **False** on first try. Wrapper remaps `0` → `USERS\|ORDERS\|POSITIONS\|SYMBOLS`, then retries SDK `mode=0`. Default mask **omits `PUMP_MODE_GROUPS`**. Empty `groups: []` after a clean connect is **unproven**, not “broker has zero groups.” |
| Product path that **does** fetch ALL groups + ALL traders | `NativeMt5BrokerConnector` (`GroupRequestArray("*")` then cache walk; `UserRequestArray` / `UserGetByGroup` / `UserLogins`) + `DealIngestionService.SyncCatalogAsync(..., GetAccountsAsync(null))`. |
| Prior live census (not this process) | Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** (`LIVE_MANAGER_FETCH_MEASURED.md`, `LIVE_GROUPS_AND_TRADERS.json` note: “Passwords never written”). |
| Copy to cTrader can place a live order today | **No.** `SAFE_BY_ABSENCE`: no `35=D` builder; `RealCopyEnabled` hardcoded `false`; `CanPromoteToLive => false`. |

**One-liner:** the proven probe is local Manager `Initialize` → optional `SetProxy` → `Connect(host:port, login, password, 0)` → `GetAllGroups` → sort/unique → password-free JSON; ALL traders are `UserLogins` per name (or C# `GetAccountsAsync(null)`); `35=D` stays unbuilt.

---

## 1. Files read (absolute)

| Path | Role |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | Assigned probe (165 lines). |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy`, `Connect` remap+fallback, `GetAllGroups`, `GetUserLogins`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `GetLastError` = `m_lastError` only. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | True pump-none (`Connect(..., 0)`); **not** used by the probe. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | `.env` keys; `IS_MT5_PROXY_ENABLED`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt` | `mt5_group_probe` `EXCLUDE_FROM_ALL`. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | `GroupTotal` / `GroupNext` / `GroupRequestArray` / `UserLogins`. |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Byte-identical flow to YoPips. |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Same four methods as YoPips wrapper. |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | Opt-in `MT5SDK_BUILD_PROBES AND WIN32`. |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Slot-2 completeness: groups **and** traders. |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors (Achiever + Starwave). |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog then deals; no send. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)`. |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Read-only connector surface (no `SendTrade`). |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType **`A` only**. |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces `RealCopyEnabled = false`. |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` at construction. |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | `CanPromoteToLive => false`. |
| `D:\Prop\apps\fix-worker\Worker.cs` | Flag true still cannot emit `35=D`. |

Siblings (quoted, not re-run): `A004_yopips_group_probe.md`, `A003_fix_noloss.md`, `LIVE_MANAGER_FETCH_MEASURED.md`, `CREDENTIALS_AND_COPY_STATUS.md`.

---

## 2. What the proven probe actually is

Standalone Windows exe. Not CI. Not `add_test`. Not the C# worker. Not a hosted service.

| Tree | How the target exists |
|---|---|
| YoPips | `add_executable(mt5_group_probe EXCLUDE_FROM_ALL …)` — always in the generate graph, **not** built by default (`CMakeLists.txt` 453–502). Sources: probe TU + `mt5_manager.cpp` + `mt5_http_client.cpp` + `app_config.cpp`. POST_BUILD copies the three Manager DLLs beside the exe. |
| Prop `mt5-sdk` | **`option(MT5SDK_BUILD_PROBES OFF)`** and **`if(MT5SDK_BUILD_PROBES AND WIN32)`** (`CMakeLists.txt` 17, 164–173). Links `mt5sdk`. Same three DLLs via `mt5sdk_copy_runtime_dlls`. |

Header comment (both trees, lines 1–8):

```
enumerates all groups visible to the configured manager login
… GetAllGroups() … JSON array …
Credentials are never echoed: only group names, the server display name, and counts
```

`main()` control flow (YoPips `tests/mt5_group_probe.cpp` 80–164):

```
spdlog::set_level(off)
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

Remote HTTP is a **hard refuse**. `MT5HttpClient` is never constructed.

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

### 3.2 Wrapper (both trees, `mt5_manager.cpp` 962–982)

1. Require `m_manager && m_connected`.
2. `total = GroupTotal()`.
3. `GroupCreate()` — null → **false** (probe exit 5).
4. For `i in [0, total)`: `GroupNext(i, grp) == MT_RET_OK` → push UTF-8 `grp->Group()`.
5. `Release`. Return **true** even if some rows were skipped.
6. `total == 0` still returns **true** with an empty vector.

Vendor (`MT5APIManager.h` 205–212): cache walk is `GroupTotal` / `GroupNext` / `GroupGet`. The **request** enumerator is `GroupRequestArray(LPCWSTR mask, IMTConGroupArray*)`. **`MT5Manager` never calls `GroupRequestArray`.** Completeness without a group pump is therefore not guaranteed by the C++ wrapper.

This is manager-visible set **(A)**. Server already applied `IMTConManager` ACL **(B)**. Plan env strings **(C)** (`MT5_GROUP_2STEP_DEMO`, …, `MT5_DEFAULT_GROUP`) are **not** consulted.

### 3.3 What is **not** ALL groups

| Anti-pattern | Why |
|---|---|
| Union of `MT5_GROUP_*` | Write-path subset |
| `MT5_DEFAULT_GROUP` | One label |
| `IMTConManager::GroupNext` masks (`*`, `demo\*`) | ACL templates, not names |
| HTTP `GET /mt5/groups` | Probe refuses `MT5_MODE=remote` |
| Hard-coded Achiever/Starwave plan names | Mapping, not discovery |

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
| `0` (probe) | Default pump **without `PUMP_MODE_GROUPS`** | SDK `mode=0` (cache-cold) |
| Need guaranteed group cache | Must pass `PUMP_MODE_GROUPS` (or `FULL`) into the **wrapper** | Fallback is still cache-cold |

True pump-none is the **pool** (`mt5_pool.cpp` 75–76: SDK `mode=0` directly). The probe does **not** use the pool.

Probe comment “No pump mode required — keep traffic minimal” is **intent**, not the first SDK call.

**Slot-2 implication:** do not declare “Achiever/Starwave have N groups” from a probe `total: 0`. Re-run with `PUMP_MODE_GROUPS` or use the C# `GroupRequestArray("*")` path.

---

## 4. How passwords are never echoed (measured, not hoped)

The probe **must** hold `MT5_PASSWORD` (and maybe `MT5_PROXY_PASSWORD`) in memory to connect. The safety contract is **stdout / logs never contain those values**.

| Channel | What is written | Password? |
|---|---|---|
| Success JSON | `probe`, `connection.success`, `connection.server`, `success`, `total`, `groups[]` | **No.** No login, no host:port, no proxy stanza. |
| Failure JSON | `reason` = fixed English token; optional `sdk_reason` = `GetLastError()` | **No.** Wrapper strings are code maps, not the secret. |
| `connection.server` | `GetServerDisplayName()` → `NetworkServer`, then **drop** if the string is only `0-9.:[]` (`mt5_manager.cpp` 168–183) | **No.** Bare IP:port is suppressed. |
| `spdlog` | Forced `level::off` at `main` line 81 **before** `AppConfig::load` | Wrapper would log login **number** + proxy host:port, never the password. Probe silences even that. |
| `mt5ErrorReason` | 7 / 1012 / 5 / 3 / generic `"… error code N"` | Mentions “login/password” as **words**, never the value. |
| `SetProxy` | `proxy.auth = "login:password"` sent to SDK only; log is `type` + address + port | **No** auth in logs. |
| Exception path | `"ERROR: exception " + e.what()` | `AppConfig::load` / filesystem; Connect returns `bool`, does not throw the secret. |
| `hasLocalConfig` | Checks non-empty password, then prints `missing_manager_credentials` | Existence only. |

JSON constructor used on every path (`failure()`, success object) has **no** `password` / `login` / `proxy` keys. There is no `dump` of `AppConfig`.

`GetLastError()` (`mt5_manager.h` 38–41) returns `m_lastError`, which is written **only** when **both** pump and no-pump `Connect` fail (`mt5_manager.cpp` 123–125). It is never the password buffer.

**Copy this pattern for Slot 2 collectors:** persist `{broker, group names, login ints, counts}`. Never persist `MT5_PASSWORD`, `ACHIEVER_PROXY_PASSWORD`, `CTRADER_FIX_PASSWORD`. The live dump already follows that (`LIVE_GROUPS_AND_TRADERS.json` line 5: `"Passwords never written. Groups and manager logins only."`).

---

## 5. Connect recipe for Achiever + Starwave (identifiers only)

Shared lifecycle:

1. `MT5Manager::Initialize(dllPath)` — factory + `CreateManager`.
2. Optional `SetProxy` **before** `Connect`.
3. `Connect(L"host:port", login, passwordW, pump)` → SDK `Connect(server, login, password, L"" /* cert */, mode, 30000)`.
4. Request APIs. Always `Disconnect`.

`AppConfig` keys the probe actually reads:

| Key | Field |
|---|---|
| `MT5_MODE` | must be `local` (default) |
| `MT5_SERVER` / `MT5_PORT` (default 443) | endpoint |
| `MT5_LOGIN` / `MT5_PASSWORD` | manager triple |
| `IS_MT5_PROXY_ENABLED` | master toggle (**not** `MT5_PROXY_ENABLED`) |
| `MT5_PROXY_TYPE` / `ADDRESS` / `PORT` / `LOGIN` / `PASSWORD` | tunnel |

`MT5_PROXY_ENABLED=true` with `IS_MT5_PROXY_ENABLED` unset leaves the probe on the **direct** path.

| Step | Achiever (this LAN) | StarwaveFX |
|---|---|---|
| Process | run 1 | run 2 (new env) |
| Proxy | **HTTP `SetProxy` required** (`IS_MT5_PROXY_ENABLED=true`) | **must not** (`false` / unset) |
| Historical fail if wrong | **1012** (`MT_RET_AUTH_MANAGER_IPBLOCK`) | n/a |
| Direct TCP open ≠ auth | Yes (1012 is ACL, not closed port) | Direct `:443` is the intended path |
| C# product keys (do not mix into the C++ probe) | `ACHIEVER_PROXY_*` | `MT5_STARWAVEFX_*`, `ProxyEnabled=false` |

Wrapper connect codes surfaced as `connection.sdk_reason`:

| Code | Meaning |
|---|---|
| **1012** | Egress IP not on manager allow-list (Achiever without HTTP hop on this desktop). |
| **7** | Network timeout. |
| **3** | Wrong manager credentials. Do **not** “fix” with proxy. |
| **5** | No connection / wrong host. |

Probe process exits: `0` ok, `2` config/init, `3` remote, `4` connect, `5` groups API.

---

## 6. ALL manager traders — **not** in the probe

Vendor (`MT5APIManager.h` 254): `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)`. Server allocates; caller `Free`s. `group` is a **mask** (`*`, `demo\*`, comma lists).

Wrapper (`mt5_manager.cpp` 315–327):

```cpp
MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
if (res != MT_RET_OK || !raw_logins) return false;
logins.assign(raw_logins, raw_logins + total);
m_manager->Free(raw_logins);
```

`GetGroupLogins` is an alias (1015–1016). Empty / false is **not** “zero traders” — the wrapper does not distinguish `MT_RET_OK_NONE`.

**Proven composition for Slot 2:**

```
GetAllGroups(names)            # or GroupRequestArray("*")
for name in names:
    GetUserLogins(toWide(name), logins)   # false → mark group incomplete
    union (broker_id, login)
```

SDK-legal one-shot: `GetUserLogins(L"*")`. Use `*` only as “all groups **this manager may see**.”

Never treat `login` as globally unique. Persist `(broker_id, login)`.

---

## 7. Slot 2 product path (both brokers, groups + traders, still no live send)

C++ probe cannot hold two brokers. The C# host can.

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector`s:

- Achiever: `MT5_SERVER` / `MT5_PORT` / `MT5_LOGIN` / `MT5_PASSWORD` + `ACHIEVER_PROXY_*` (HTTP).
- Starwave: `MT5_STARWAVEFX_SERVER` / `PORT` / `LOGIN` / `PASSWORD`, `ProxyEnabled = false`.

`IMt5BrokerConnector` (`Mt5Contracts.cs` 53–63) is **read-only**: Connect / Groups / Accounts / Deals / Positions. **Zero** `SendTrade` / `DealerSend` / password-change symbols under `D:\Prop\src\Mt5`.

### 7.1 Groups (more complete than the probe)

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` 144–186):

1. `GroupRequestArray("*")` — the no-pump complete enumerator the C++ wrapper **does not** call.
2. If still empty: `GroupTotal` + `GroupNext` (same walk as the probe).
3. Dedup by name (ordinal ignore-case).

Connect pump is `PUMP_MODE_GROUPS | USERS | POSITIONS`, then `PUMP_MODE_NONE` fallback — **fixes** the probe’s missing `PUMP_MODE_GROUPS`.

### 7.2 Traders (the probe never does this)

`GetAccountsAsync(null)` → every group name, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup(gname)`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. merge `UserAccountRequestArray` / `UserAccountGetByGroup`

DTO is login + group + leverage + balances. **No password fields.**

### 7.3 Host

`LiveIngestHostedService`: per connector, `Connect` → `SyncCatalogAsync` (groups + **all** accounts) → deal/position sync → score. On catalog fail it logs *“No dummy data will be substituted.”*

`DealIngestionService.SyncCatalogAsync`: `GetGroupsAsync` + `GetAccountsAsync(null)` + batch upsert. No `Take(200)` on this path.

### 7.4 Prior measured census (not re-run here)

From `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` and `LIVE_GROUPS_AND_TRADERS.json` (`utc` 2026-08-18T08:42:16Z, `probe: LiveBrokerProbe`):

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | HTTP proxy | 8 | 6512 | 1506 |
| STARWAVEFX | direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever names (manager-visible only): `contest\yo-1step|2step|instant|payp`, `demo\yo-1step|2step|instant|payp`.

Starwave names: `Starwave\cent\FX1\grp{1,2}`, `Starwave\demo\FX2\grp{1,2}`, `Starwave\real\FX3\grp{1-5}` + `LP`.

If the server has more groups, they are **outside this manager’s ACL**. That is still “ALL” for Slot 2.

---

## 8. Copy to cTrader must not send live orders (no loss)

Slot 2 is **fetch + persist**. Execution stays off.

| Gate | Evidence |
|---|---|
| `RealCopyEnabled` constructed `false` | `DependencyInjection.cs` 38–41: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| Forced false after any FIX logon | `CTraderFixLogonHostedService.cs` 68–70 |
| Flag default | `CTraderFixOptions.RealCopyExecutionEnabled = false` |
| Scoring cannot promote LIVE | `BaselineScorer.CanPromoteToLive => false` (line 211) |
| FIX outbound MsgType | `CTraderFixSession.BuildLogon` starts `(35, "A")`. Tags: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. **No `D` / `F` / `G`.** |
| `35=D` / `(35, "D")` builder in `Fix.CTrader` | **0** product senders |
| fix-worker | Even if `CTrader:RealCopyExecutionEnabled=true`, it stamps TRADE `Disconnected` and **has no function** that can emit `35=D` (`Worker.cs` 21–46) |
| MT5 write APIs on the C# connector | **Absent** |

Honest operating mode until risk + recon + §68/§70 PASS:

```
ALLOW:  Manager read (groups, users, deals, positions)
        FIX TLS Logon 35=A (QUOTE/TRADE) for session proof / later recon
FORBID: NewOrderSingle 35=D, cancel/replace 35=F/G,
        DealerSend / SendTrade on source or dest,
        REAL_COPY_EXECUTION_ENABLED=true as a license to trade
```

A logon is **not** a copy. Flipping the flag **cannot** place an order today (`SAFE_BY_ABSENCE`). That is how this tree avoids a live loss on Slot 2. It is **not** a finished no-loss copy engine.

---

## 9. Copy-paste operator sequence (no secrets)

**C++ probe (groups only, one broker per run):**

1. `MT5_MODE=local`.
2. Achiever: `IS_MT5_PROXY_ENABLED=true`, `MT5_PROXY_TYPE=HTTP`, allow-list host/port, proxy auth in env; `MT5_SERVER` / `MT5_PORT=443` / `MT5_LOGIN` / `MT5_PASSWORD` in env.
3. Build YoPips target `mt5_group_probe` (or Prop `-DMT5SDK_BUILD_PROBES=ON` + `--target mt5_group_probe`).
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
- Live 18/8460 numbers are from the earlier `LiveBrokerProbe` artifact, not a new attach.
- C++ `GetAllGroups` after no-pump fallback can be empty while groups exist.
- `IS_MT5_PROXY_ENABLED` vs `MT5_PROXY_ENABLED` vs `ACHIEVER_PROXY_*` remains a foot-gun.
- Probe DLL path `sourceDir()/MetaTrader5SDK/Libs` is the YoPips layout; Prop CMake copies from `vendor/MetaTrader5SDK/Libs`. Do not copy the probe’s relative path into the C# worker.
- §68 / §70 live-send scorecards remain FAIL; Slot 2 must not wait on them and must not “fix” them by sending.

---

## 11. Sources (absolute)

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\CMakeLists.txt`
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
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\reports\swarm\20260818\A004_yopips_group_probe.md`
- `D:\Prop\reports\swarm\20260818\A003_fix_noloss.md`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
