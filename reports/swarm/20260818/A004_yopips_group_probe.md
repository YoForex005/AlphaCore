# A004 — YoPips group probe: proven connect recipe, pump-none fallback, ALL groups / ALL logins

| Field | Value |
|---|---|
| Agent | A004 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A004_yopips_group_probe.md` |
| Product source modified | **No.** Read-only extraction. |
| Secrets printed | **None.** No manager passwords, no proxy usernames, no proxy passwords. |

**Assigned read:**

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (`Connect`, `SetProxy`, `GetAllGroups`, `GetUserLogins`)
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (present; same four methods)

**Supporting (quoted, not assigned):**

- YoPips `src\main.cpp` (proxy apply-before-connect; request pool is true pump-none)
- YoPips / Prop `config\app_config.cpp` (`IS_MT5_PROXY_ENABLED`)
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` (byte-identical flow to YoPips probe)
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (`MT5Session::Connect` mode=`0`)
- Vendor `MT5APIManager.h` (`UserLogins`, `MTProxyInfo::PROXY_HTTP=2`)
- Historical connect evidence: R012 (Achiever 1012 without HTTP proxy); architecture §7–§8 identifiers only

This note is the **connect + enumerate recipe** a later collector / C# wrapper must copy. It is **not** a live attach proof. `mt5_group_probe` prints **groups only**. Login enumeration is a sibling API (`GetUserLogins` / `GetGroupLogins`), not in the probe binary.

---

## 0. Verdict (do not greenwash)

| Claim | Measured |
|---|---|
| YoPips probe and Prop SDK probe are the same tool | **Yes.** Same control flow, same `.env` keys, same `Connect(..., pumpMode=0)` call, same JSON. Prop file lives at `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`. |
| YoPips `mt5_manager.cpp` vs Prop `mt5-sdk\src\core\mt5_manager.cpp` for the four assigned methods | **Identical.** `SetProxy`, `mt5ErrorReason`, `Connect` (including pump remapping + no-pump retry), `GetUserLogins`, `GetAllGroups`, `GetGroupLogins` alias. |
| Probe enumerates ALL manager-visible **groups** | **Yes**, via `GetAllGroups` → `GroupTotal` + `GroupNext`. Mapping-blind: `MT5_GROUP_*` / `MT5_DEFAULT_GROUP` are loaded and **ignored**. |
| Probe enumerates ALL **logins** | **No.** It never calls `GetUserLogins` / `UserLogins`. ALL logins is a **second** walk documented in §6. |
| Achiever local connect from this LAN needs HTTP `ProxySet` | **Yes.** Allow-list identity is `81.29.145.69`. Direct TCP to `57.128.141.65:443` is open but **1012** when egress is not that IP. |
| StarwaveFX local connect needs that proxy | **No.** Direct `84.201.6.142:443`. `MT5_STARWAVEFX_PROXY_ENABLED=false`. |
| Passing `pumpMode=0` into `MT5Manager::Connect` is pump-none | **False.** Wrapper remaps `0` to `USERS\|ORDERS\|POSITIONS\|SYMBOLS`, then on failure retries SDK `Connect(..., mode=0)`. True pump-none is the **fallback** (and the **pool**). |
| This pass proved a live Achiever / Starwave session | **No.** Code + historical 1012 logs only. |

Honest one-liner: **Achiever = HTTP `ProxySet` then `Connect(host:443)` with `IS_MT5_PROXY_ENABLED=true`; Starwave = same `Connect` with proxy off; group list = `GroupTotal`/`GroupNext`; all logins = `UserLogins` per group (or mask `*`); pump-none is the fallback / pool, not the probe’s literal `0` argument.**

---

## 1. What the probe actually does

Standalone Windows exe. Loads `.env` via `AppConfig`, local Manager only, prints pretty JSON, exits.

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

`spdlog` is forced **off**. Stdout is JSON only. Comment in the probe: credentials are never echoed. `GetLastError()` is copied into `connection.sdk_reason` only after a failed connect (wrapper strings, not the password).

Remote HTTP is a **hard refuse**. There is no group-list path on `MT5HttpClient` that this probe will use.

One `AppConfig` triple per process. Achiever and StarwaveFX are **two runs**, two env sets. Do not invent a dual-broker probe in this binary.

---

## 2. Proven connect recipe

### 2.1 Shared lifecycle (both brokers)

1. `MT5Manager::Initialize(dllPath)` — factory `Initialize` + `CreateManager(MTManagerAPIVersion)`. Fail-closed.
2. Optional `SetProxy` **before** `Connect` (Achiever yes, Starwave no).
3. `Connect(serverW, login, passwordW, pumpMode)` under `m_mutex`.
4. SDK call:

```text
m_manager->Connect(server, login, password, L"" /* password_cert */, mode, 30000)
```

Server string format is **`host:port`** (wide), built as `config.mt5_server + ":" + mt5_port`. Port default in `AppConfig` is **443**.

5. On wrapper success: `IsConnected`, then request APIs. Probe then `GetAllGroups`. Always `Disconnect`.

### 2.2 Achiever — HTTP proxy (required on this workstation)

Non-secret identifiers (architecture §7 / YoPips `.env` key *names*):

| Item | Value published |
|---|---|
| Server | `57.128.141.65` |
| Port | `443` |
| Manager login | `2027` |
| Display name key | `MT5_SERVER_NAME=AchieverGlobalMarkets-Server` |
| Allow-list / proxy host | `81.29.145.69` |
| Proxy port | `49527` |
| Proxy type | `HTTP` → `MTProxyInfo::PROXY_HTTP` (`2`) |
| Master toggle the C++ code reads | **`IS_MT5_PROXY_ENABLED`** (default **false**) |
| YoPips `.env` also has | `MT5_PROXY_ENABLED=true` — **not read** |

`SetProxy` (YoPips / Prop `mt5_manager.cpp` 33–58):

- `proxy.enable = 1`
- `proxy.type` = caller type (`HTTP` / `SOCKS4` / default SOCKS5)
- `proxy.address` = `"IP:port"` (wide)
- `proxy.auth` = `"login:password"` only if proxy login non-empty (never log this field)
- Immediate `ProxySet`, then store config and set `m_proxyApplied = false` so `Connect` re-applies

`Connect` re-applies the same `ProxySet` once if `m_proxyConfig.enabled && !m_proxyApplied`. Logs type + address + port only.

Probe helper `configureProxyIfNeeded`:

- `!mt5_proxy_enabled` → skip (return true)
- else require non-empty type, non-empty address, port `> 0`
- map `SOCKS4` / `HTTP`; anything else (including `SOCKS5`) stays `PROXY_SOCKS5`

**Why proxy is not optional here:** TCP to Achiever `:443` succeeds from this LAN, but the manager access list is the proxy egress IP. Historical YoPips local starts with `MT5 proxy mode: DISABLED (global)` failed **1012** on pump **and** no-pump (R012). That is the proven failure mode, not a theory.

**Toggle bug (binding):** `AppConfig` binds `mt5_proxy_enabled` from **`IS_MT5_PROXY_ENABLED`**. A file that only sets `MT5_PROXY_ENABLED=true` leaves the probe / backend on the **direct** path → 1012 on this host. C# Prop worker uses different names (`ACHIEVER_PROXY_*`); do not mix them into the C++ probe without mapping.

If the worker already source-NATs as `81.29.145.69`, HTTP proxy is unnecessary. That is **not** `DESKTOP-FQPFPKE`.

### 2.3 StarwaveFX — direct (no proxy)

Non-secret identifiers (architecture §8):

| Item | Value published |
|---|---|
| Server | `84.201.6.142` |
| Port | `443` |
| Manager login | `9904` |
| Proxy | **off** (`MT5_STARWAVEFX_PROXY_ENABLED=false`; no documented whitelist) |

Recipe: same `Initialize` + `Connect(L"84.201.6.142:443", 9904, password, pump)`. Do **not** call `SetProxy`. Do **not** reuse Achiever’s HTTP hop — that hop exists to present Achiever’s allow-list IP, not Starwave’s.

C++ `AppConfig` is **single-broker**. Starwave is a second process env (`MT5_SERVER` / `MT5_LOGIN` / `MT5_PASSWORD` swapped). There are no `MT5_STARWAVEFX_*` fields on the C++ struct.

TCP to `84.201.6.142:443` is open from this host (R012, 189 ms). Direct Manager logon is the intended path; it is **not** proven in this pass.

### 2.4 Two-broker operator matrix

| Step | Achiever | StarwaveFX |
|---|---|---|
| `MT5_MODE` | `local` | `local` |
| `Initialize(…/Libs)` | yes | yes |
| `IS_MT5_PROXY_ENABLED` | `true` | `false` / unset |
| `MT5_PROXY_TYPE` | `HTTP` | unused |
| `MT5_PROXY_ADDRESS` / `PORT` | allow-list host / `49527` | unused |
| `SetProxy` | **required** on this LAN | **must not** |
| `Connect` host:port | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login | `2027` | `9904` |
| Password / proxy auth | env only; never log | env only; never log |
| Expected first fail if proxy omitted here | **1012** | n/a |
| Expected first fail if creds wrong | **3** | **3** |
| Probe output | one JSON group list | one JSON group list (second run) |

---

## 3. Error codes 1012 / 7 / 3 / 5

Wrapper map (`mt5ErrorReason`, both trees, `mt5_manager.cpp` 61–68). Stored in `m_lastError` **only** when **both** pump connect and no-pump fallback fail. Probe surfaces it as `connection.sdk_reason`.

| Code | SDK name (wrapper comment) | Wrapper string (sanitized) | What it means on this recipe |
|---|---|---|---|
| **1012** | `MT_RET_AUTH_MANAGER_IPBLOCK` | IP blocked by MT5 server. Ask admin to whitelist this machine’s IP. | Achiever saw a source IP that is **not** `81.29.145.69`. Direct connect from this LAN. Proxy disabled / wrong toggle / `SetProxy` skipped. **Does not** mean the TCP port is closed. |
| **7** | `MT_RET_ERR_NETWORK` | Network timeout. Unreachable — check proxy/firewall and IP whitelist. | 30 s connect budget exhausted. Dead proxy, CONNECT failure, or path flap. YoPips pool logs have shown **1012 then 7** when many sessions retry after an IP block. |
| **3** | `MT_RET_AUTH_MANAGER_FAILED` | Wrong credentials. Check manager login/password. | Login/password rejected. **Not** an IP problem. Do not “fix” this by toggling proxy. (`MT_RET_ERR_PARAMS` is also 3 in the constants header — wrapper treats 3 as auth-failed for **Connect**.) |
| **5** | `MT_RET_ERR_NOCONNECT` | No connection. Server offline or wrong address. | Host:port wrong, server down, or never established. Distinct from timeout (7). |

Anything else: `"Connection failed with MT5 error code N."`

Related codes **not** in the wrapper switch (A39): `1011` manager-no-config, `1013` group-invalid, `1024` manager-type. They fall through to the generic string.

Probe process exits (different namespace from MTAPIRES):

| Exit | Meaning |
|---|---|
| 0 | Connected, groups printed |
| 2 | Missing creds / SDK init / exception / invalid proxy stanza |
| 3 | `MT5_MODE=remote` |
| 4 | `Connect` false (read `sdk_reason` for 1012/7/3/5) |
| 5 | Connected but `GetAllGroups` false |

---

## 4. Pump remapping vs pump-none fallback

This is the most-copied bug. Three different “mode = 0” meanings exist in the same tree.

### 4.1 `MT5Manager::Connect` (wrapper)

```102:135:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
    ...
    MTAPIRES res = m_manager->Connect(..., mode, 30000);
    if (res != MT_RET_OK) {
        // retry with no subscriptions (mode=0)
        res = m_manager->Connect(..., 0, 30000);
```

| Caller passes | First SDK `Connect` | On fail |
|---|---|---|
| `0` (probe, `Connect` default, YoPips `main.cpp`) | **Default pump** `USERS\|ORDERS\|POSITIONS\|SYMBOLS` — **not** groups, **not** news, **not** deals | Retry SDK mode **`0`** (true pump-none) |
| Non-zero mask | That mask | Same retry with SDK `0` |
| Need guaranteed group **cache** | Must pass `PUMP_MODE_GROUPS` (or `FULL`) explicitly | Fallback is still cache-cold |

On **fallback success**: `m_connected=true`, `m_pumpMode=false`, no `Position/Deal/Order/UserSubscribe`. Request APIs (`DealRequest`, `UserRequest`, `UserLogins`, `GroupRequest*`) remain valid. Comment: pump may fail when IP is not yet whitelisted for pump; request-only can still work.

On **pump success**: subscribe the four sinks, `m_pumpMode=true`.

Default mask **omits `PUMP_MODE_GROUPS`**. `GetAllGroups` reads the **local group cache** (`GroupTotal`/`GroupNext`). After a no-pump fallback that cache may be empty even though the manager can see groups. Empty `groups: []` with `success: true` is therefore **ambiguous**: ACL empty **or** cold cache. Completeness without pump is `GroupRequestArray(L"*")`, which this wrapper **does not call**.

Probe comment (“No pump mode required — keep traffic minimal”) is **intent**, not what the wrapper does on the first attempt.

### 4.2 True pump-none: request pool

`MT5Session::Connect` (`mt5_pool.cpp` 74–76) calls the SDK with **`mode=0` directly**. No remapping. YoPips `main.cpp`: pool initializes “regardless of pump status — pool uses mode=0 (no pump) so it can succeed even when the full-pump connection fails.”

That is the proven request-only session. The probe does **not** use the pool; it uses `MT5Manager`, so it still takes the remap + fallback path.

### 4.3 Recipe for a later dump / collector

- Discovery that must not miss groups: pass `PUMP_MODE_GROUPS` (optionally `\| PUMP_MODE_USERS`) into the **wrapper**, or add `GroupRequestArray("*")`.
- Operator probe as written: accept remap + fallback; treat empty group list after a 1012-free connect as **unproven**, not “broker has zero groups.”
- Do not set `pumpMode=0` expecting the first SDK call to be pump-none. Use the pool session or change the wrapper.

---

## 5. How ALL groups are enumerated

### 5.1 The only discovery primitive the probe uses

`MT5Manager::GetAllGroups` (both trees, ~962–982):

1. Require `m_manager && m_connected`.
2. `total = GroupTotal()`.
3. `GroupCreate()` — null → **false** (probe exit 5).
4. For `i in [0, total)`: `GroupNext(i, grp) == MT_RET_OK` → push UTF-8 `grp->Group()`.
5. `Release`. Log count. Return **true** even if some `GroupNext` rows were skipped.
6. `total == 0` still returns **true** with an empty vector.

Probe then `std::sort` + `std::unique`. Wrapper itself does not sort.

This is manager-visible set **(A)** (A39). Server already applied `IMTConManager` ACL masks **(B)**. Plan env strings **(C)** are not consulted.

`GetGroupDetails` is the same walk plus currency / company / margin / `PERMISSION_ENABLE_CONNECTION`. Not used by the probe. YoPips admin controller prefers details, falls back to names (`admin_mt5_controller.cpp`).

### 5.2 What is **not** ALL groups

| Anti-pattern | Why |
|---|---|
| Union of `MT5_GROUP_*` | Write-path subset |
| `MT5_DEFAULT_GROUP` (`demo\Maxmaster` on Achiever) | One label, not the universe |
| `IMTConManager::GroupNext` masks (`*`, `demo\*`) | ACL templates, not group names |
| `LogAvailableGroups` | Caps at 50 names |
| HTTP `GET /mt5/groups` | Different transport; probe refuses remote |
| `GroupGet` / `GroupRequest` one name | Point lookup |

`GroupRequestArray(L"*")` is the no-pump complete enumerator in the vendor header. **Unused** by `MT5Manager`. Do not claim the current wrapper is no-pump-complete.

---

## 6. How ALL logins are enumerated

Not in `mt5_group_probe`. Use the Manager user-list API.

### 6.1 Wrapper

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

`GetGroupLogins` is an alias of `GetUserLogins` (line 1015–1016).

Vendor: `IMTManagerAPI::UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)`. Server allocates; caller `Free`s. The `group` argument is a **mask** (`CMTStr::CheckGroupMask`: `*`, `demo\*`, comma lists, `!exclude`) — same language as Administrator “Groups” on the manager.

Empty group / null pointer → wrapper returns **false** (stricter than `GetAllGroups`). Callers must not treat `false` as “zero logins” without checking `MT_RET_OK_NONE` (the wrapper does not distinguish).

HTTP sibling: `GET /mt5/users/logins?group=` — probe never uses it; group names with `\` will break if not encoded (A39).

### 6.2 ALL logins for one manager (proven composition)

Two equivalent enumerators. Server ACL still applies.

**Preferred (matches R004 dump design; works with current wrapper):**

```text
GetAllGroups(names)                 # or GetGroupDetails
for name in names:
    GetGroupLogins(toWide(name), logins)
    # false → group incomplete; do not invent []
    union into (broker, login)
```

**Mask form (SDK-legal; not wrapped as its own method):**

```text
GetUserLogins(L"*", allLogins)
```

Use `*` only as “all groups **this manager may see**.” Do not pass the plan-name CSV as a discovery mask.

Then per login (collector, not probe): `GetUser` + `GetAccount` + `GetDeals(from,to)`. Cache-first account/position reads miss on no-pump and fall back to `*Request`.

Never treat `login` as globally unique. Achiever `1001` and StarwaveFX `1001` are different traders. Persist `(broker_id, login)`.

### 6.3 What the probe will never print

No login arrays, no account counts, no deals. A green probe JSON is **not** “~5,000 accounts discovered.” It is only “this manager can list N group names.”

---

## 7. Copy-paste operator sequence (no secrets)

Achiever (this LAN):

1. `MT5_MODE=local`
2. `IS_MT5_PROXY_ENABLED=true` (the key the code reads)
3. `MT5_PROXY_TYPE=HTTP`, address = allow-list host, port `49527`, proxy login/password in env
4. `MT5_SERVER=57.128.141.65`, `MT5_PORT=443`, `MT5_LOGIN=2027`, manager password in env
5. Build `mt5_group_probe` (`MT5SDK_BUILD_PROBES=ON`, WIN32)
6. Run; expect `connection.success` and a sorted `groups[]`
7. For logins: same session, `GetAllGroups` then `GetUserLogins` per name (or `*`)

StarwaveFX:

1. Same binary, **new** env: server `84.201.6.142`, login `9904`, password in env
2. Leave proxy **disabled**
3. Repeat

If Achiever returns `sdk_reason` with 1012: proxy was not applied (wrong toggle) or egress is still not the allow-list. If 3: fix manager password, not the proxy. If 7/5: network / host:port. If groups empty after success: do not declare “no groups” until `PUMP_MODE_GROUPS` or `GroupRequestArray("*")` is used.

---

## 8. Honesty / open items

- Live Achiever / StarwaveFX attach **not** executed this pass.
- YoPips historical local Achiever without proxy = **1012** (measured in logs, R012). That is the strongest empirical proof the HTTP hop is required here.
- Wrapper `Connect(0)` is **not** pump-none on the first try.
- `GetAllGroups` after no-pump fallback can be empty while groups exist.
- Probe does not enumerate logins.
- `IS_MT5_PROXY_ENABLED` vs `MT5_PROXY_ENABLED` / `ACHIEVER_PROXY_*` is a real foot-gun.
- C++ `AppConfig` cannot hold Achiever and Starwave at once.

---

## 9. Sources (absolute)

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- Siblings: `A14_mt5_manager_local.md`, `A39_mt5_group_discovery.md`, `R002_probe.md`, `R012_proxy.md`, `R004_collector.md`
