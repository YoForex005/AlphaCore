# W500_RESEARCH_1 — YoPips `mt5_manager.cpp` Connect pump-none fallback + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **1** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_1 |
| Topic | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy address `IP:port` auth `login:password` |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secret values printed | **None** (proxy/manager/FIX passwords classified only) |
| Live Connect this pass | **Not re-run.** Census numbers below are the prior measured fetch at `2026-08-18T08:45Z` (`LIVE_MANAGER_FETCH_MEASURED.md`). This slot is a source/recipe audit. |

## Verdict

**CONFIRMED_WITH_GROUPS_CACHE_GAP**

YoPips `MT5Manager::Connect` **does** retry `IMTManagerAPI::Connect(..., pump_mode=0, 30000)` after a pump connect fails. Proxy packing **is** `address = IP:port` and `auth = login:password` (`MTProxyInfo`, 64-wchar buffers). That is the correct Manager-API recipe for Achiever from this LAN.

It is **not** sufficient, by itself, to claim “ALL groups” after the fallback:

- Passing `pumpMode=0` into the **wrapper** is **not** pump-none on the first SDK call. The wrapper remaps `0` to `USERS|ORDERS|POSITIONS|SYMBOLS` and **omits `PUMP_MODE_GROUPS`**.
- True pump-none is the **retry** (`mode=0`) and the **pool** session (`MT5Session::Connect` first call is already `0`).
- `GetAllGroups` / `GetGroupDetails` walk the **local group cache** (`GroupTotal`/`GroupNext`). `GroupRequestArray(L"*")` is unused in YoPips `src/`. After a no-pump fallback that cache can be empty while the manager still sees groups.
- ALL traders **can** be fetched on pump-none via `UserLogins` (request API). `GetUserLogins` / `GetGroupLogins` already wrap it. `UserRequestArray` is unused in YoPips C++.

Prop C# `NativeMt5BrokerConnector` already copies the YoPips fallback **and** uses `GroupRequestArray("*")` + `UserRequestArray` / `UserLogins`. Prior measured census: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**.

cTrader copy **cannot place a live order today**: `CTraderFixSession.BuildLogon` emits `35=A` only; no `35=D` builder; `RealCopyExecutionEnabled` default **false**; hosted service forces `_runtime.RealCopyEnabled = false`. Risk to capital on the copy path is **NONE** (`SAFE_BY_ABSENCE`). Do not enable `REAL_COPY_EXECUTION_ENABLED`.

---

## 0. Files read (this pass)

| Path | What was measured |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy`, `Connect` remap + no-pump retry, `GetUserLogins`, `GetAllGroups`, cache→request fallbacks |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `Connect(..., pumpMode=0)` default, `m_pumpMode`, `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` L501–508 | `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` L25–84, L211–223 | Pool is **true** `mode=0` first; same proxy packing |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` L201–276 | `IS_MT5_PROXY_ENABLED` → `SetProxy` before Connect; pool always `mode=0` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | Probe calls `Connect(..., 0)` (wrapper remaps) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` L167–172 | Env keys the C++ code actually binds |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` L76–92, L125–144, L162–164, L206–212, L254 | Vendor `MTProxyInfo`, `EnPumpModes`, `Connect`, `GroupRequestArray`, `UserLogins` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L33–150 | **Identical** Connect/SetProxy block to YoPips |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | C# copy: pump then `PUMP_MODE_NONE`; proxy `host:port` / `user:pass`; `GroupRequestArray` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `BuildLogon` is `35=A` only |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; forces `RealCopyEnabled=false` |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Prior live census |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | Local Achiever needs HTTP proxy (1012 without it) |

`GroupRequestArray` under YoPips `src\` = **0 hits**. `35=D` under `D:\Prop\src\Fix.CTrader` product send path = **0** (name/log/comment only).

---

## 1. Vendor contract (do not invent)

```76:92:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
struct MTProxyInfo
  {
   enum
     {
      PROXY_SOCKS4   =0,
      PROXY_SOCKS5   =1,
      PROXY_HTTP     =2,
      PROXY_FIRST    =PROXY_SOCKS4,
      PROXY_LAST     =PROXY_HTTP
     };
   int32_t           enable;
   int32_t           type;
   wchar_t           address[64];            // IP:port of proxy server
   wchar_t           auth[64];               // login:password
  };
```

```162:164:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual void      ProxySet(const MTProxyInfo &proxy)=0;
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

`pump_mode=0` on the **SDK** call is pump-none (no subscriptions). C# WebAPI sample names that `PUMP_MODE_NONE = 0x00000000`. C++ `EnPumpModes` has no named `NONE`; `0` is the none value.

Pump flags used below (`MT5APIManager.h` L127–143):

| Flag | Value | In YoPips default remap? |
|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | **Yes** |
| `PUMP_MODE_ORDERS` | `0x00000008` | **Yes** |
| `PUMP_MODE_POSITIONS` | `0x00000080` | **Yes** |
| `PUMP_MODE_SYMBOLS` | `0x00000200` | **Yes** |
| `PUMP_MODE_GROUPS` | `0x00000100` | **No** |
| `PUMP_MODE_FULL` | `0xffffffff` | No |
| none | `0` | Fallback / pool only |

Default remap OR = `0x00000289`. Completeness APIs:

| API | Header line | Kind |
|---|---|---|
| `GroupTotal` / `GroupNext` | 206–207 | **Cache** |
| `GroupRequestArray(mask)` | 212 | **Network** — ALL groups this manager ACL allows |
| `UserLogins(group, logins, total)` | 254 | **Network** — ALL logins matching group mask |
| `UserRequestArray` | 410 | **Network** — full user records |

---

## 2. Proxy packing — `address=IP:port`, `auth=login:password`

Measured in both `SetProxy` and the Connect-time re-apply. Comments in source are literal.

```33:57:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::SetProxy(uint32_t type, const std::wstring& address, uint32_t port,
                          const std::wstring& proxyLogin, const std::wstring& proxyPassword) {
    // ...
    // Format address as "IP:port"
    std::wstring addrPort = address + L":" + std::to_wstring(port);
    wcsncpy_s(proxy.address, addrPort.c_str(), _countof(proxy.address) - 1);

    // Format auth as "login:password"
    if (!proxyLogin.empty()) {
        std::wstring auth = proxyLogin + L":" + proxyPassword;
        wcsncpy_s(proxy.auth, auth.c_str(), _countof(proxy.auth) - 1);
    }

    m_manager->ProxySet(proxy);
    m_proxyConfig   = { true, type, address, port, proxyLogin, proxyPassword };
    m_proxyApplied  = false;  // force re-application on next Connect() (hot-reload safety)
```

Connect re-applies the same packing if `m_proxyConfig.enabled && !m_proxyApplied` (L76–96). Logs **type + host + port only** — not the auth string. Good.

Same packing on `MT5Session::SetProxy` / Connect (`mt5_pool.cpp` L25–72).

Constraints (measured, not guessed):

- `address[64]` / `auth[64]` are **wchar**. `IP:port` fits. `login:password` must stay under 63 wide chars or it is silently truncated (`wcsncpy_s` + `_countof-1`).
- Empty proxy login ⇒ `auth` left zeroed (no `:`). Empty password with a non-empty login still writes `login:`.
- `ProxySet` is **void**. There is no retcode check in C++. C# `NativeMt5BrokerConnector.ApplyProxy` **does** check `MT_RET_OK`.
- `SetProxy` sets `m_proxyApplied=false` so the next `Connect` re-sends `ProxySet` (hot-reload).

### Env keys (do not mix)

| Layer | Master toggle | Host / port / user / pass | Type |
|---|---|---|---|
| YoPips C++ (`app_config.cpp` 167–172) | **`IS_MT5_PROXY_ENABLED`** (default **false**) | `MT5_PROXY_ADDRESS`, `MT5_PROXY_PORT`, `MT5_PROXY_LOGIN`, `MT5_PROXY_PASSWORD` | `MT5_PROXY_TYPE` = `HTTP`/`SOCKS4`/`SOCKS5` (default SOCKS5 if type empty **and** enabled+host+port set) |
| YoPips `.env` also may have | `MT5_PROXY_ENABLED` | same family | **Not read** by `AppConfig` (R012 / A004 toggle bug) |
| Prop C# (`LiveMt5Registration.cs` 30–45) | **`ACHIEVER_PROXY_ENABLED`** | `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD` | Hardcoded `PROXY_HTTP` |
| Starwave C# | forced `ProxyEnabled = false` | n/a | direct |

Type map in `main.cpp` L207–209: `SOCKS4`→0, `HTTP`→2, else SOCKS5→1.

Achiever allow-list egress (non-secret, C55): **`81.29.145.69`**. Intended hop: Manager `PROXY_HTTP` to **`81.29.145.69:49527`**. This desktop public egress was measured **`106.219.132.213`** (R012). TCP to Achiever `:443` is OPEN; failure mode without proxy is **1012** `MT_RET_AUTH_MANAGER_IPBLOCK`, not reachability. Starwave does **not** need this proxy.

Historical YoPips local starts with `IS_MT5_PROXY_ENABLED` unset logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump (R012). Fallback does **not** cure an IP block.

`mt5ErrorReason` (`mt5_manager.cpp` 61–68):

| Code | Meaning | Typical cause here |
|---|---|---|
| 1012 | IP blocked | Achiever without presenting `81.29.145.69` |
| 7 | Network timeout | proxy/firewall |
| 5 | No connection | host/port |
| 3 | Auth failed | manager password |
| other | generic | — |

`m_lastError` is set **only** when **both** pump and no-pump fail (L123–127). A successful fallback clears it.

---

## 3. Connect fallback — pump first, then pump-none

```71:149:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::Connect(const std::wstring& server, uint64_t login,
                         const std::wstring& password, uint64_t pumpMode) {
    // Apply proxy before connect if enabled && !applied
    m_manager->Subscribe(this);

    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
    // ...
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // on fail: m_lastError = mt5ErrorReason(res); return false
        // on ok:  m_connected=true; m_pumpMode=false; no Position/Deal/Order/UserSubscribe
    }
    // pump ok: subscribe four sinks; m_pumpMode=true
}
```

### Binding facts

1. **Wrapper `pumpMode=0` ≠ SDK `pump_mode=0`.** `mt5_manager.h` L31–32 defaults `pumpMode = 0`. `main.cpp` L230 calls `Connect(server, login, password)` with that default. `mt5_group_probe.cpp` L114 passes `0` on purpose (“No pump mode required”) — that comment is **false** for the first SDK call.
2. First SDK call uses remapped mask **without `PUMP_MODE_GROUPS`**. Even a **successful** pump can leave `GroupTotal()==0` until something else fills the group cache.
3. Fallback is a **second** `Connect(..., L"", 0, 30000)` with empty cert, same 30s timeout. Comment (L118–121): pump can fail when IP is not yet whitelisted **for pump** while request-only still works. That is a **different** failure from 1012 (blocked for all manager access).
4. Fallback success: `m_pumpMode=false`, `m_newsPumpModeEnabled=false`, sinks for Position/Deal/Order/User **not** subscribed. `Subscribe(this)` already ran (manager connect/disconnect sink only).
5. Fallback does **not** unsubscribe `this` if the first Connect left a half-session. Vendor `Connect` is assumed to replace the session.
6. `password_cert` is always `L""`.

### Three Connect styles in this tree

| Caller | First `pump_mode` | Fallback | Resulting `m_pumpMode` |
|---|---|---|---|
| `MT5Manager::Connect` (`main`, probe) | remapped default `0x289` (or caller mask) | SDK `0` | `true` / `false` |
| `MT5Session::Connect` (request pool) | SDK **`0` immediately** (`mt5_pool.cpp` L74–76) | none | n/a (session has no pump flag) |
| Prop `NativeMt5BrokerConnector.ConnectCore` | `GROUPS\|USERS\|POSITIONS` | `PUMP_MODE_NONE` | `_pumpEnabled` |

Pool comment in `main.cpp` L241–242: pool is initialized **even if pump fails**, because pool sessions are request-only.

---

## 4. What still works on pump-none (ALL traders vs ALL groups)

| Method | Lines | After `m_pumpMode=false` | ALL-complete? |
|---|---|---|---|
| `GetAllGroups` | 962–981 | `GroupTotal`/`GroupNext` cache | **No.** Empty list + `return true` is a valid outcome |
| `GetGroupDetails` | 984–1013 | same cache | **No** |
| `LogAvailableGroups` | 1089–1106 | same cache; logs first 50 | diagnostic only |
| `GetUserLogins` / `GetGroupLogins` | 315–328 / 1015–1016 | `UserLogins` network | **Yes, for the mask** (use `*` or each group name) |
| `GetUser` | `UserRequest` | network | per login |
| `GetAccount` | 339–348 cache then `UserAccountRequest` | **Yes** (explicit no-pump comment) | per login |
| `GetPositions` | 405–408 `PositionGet` then `PositionRequest` | **Yes** per login | |
| `GetOrders` | 440–443 **skips cache** when `!m_pumpMode` | **Yes** per login | |
| `GetDeals` | 492 `DealRequest` | **Yes** (always network) | per login + window |

`GetUserLogins` fail-closed: `res != MT_RET_OK || !raw_logins` → `false`. An empty group that returns a null pointer looks like an API failure (A14/A56). Caller must treat that carefully.

**ALL traders recipe (YoPips APIs, pump-none safe):**

```
SetProxy(HTTP, host, port, login, password)   // Achiever only; before Connect
Connect(server:port, managerLogin, password)  // pump, then mode=0
// groups:
//   prefer vendor GroupRequestArray("*")  — NOT implemented in MT5Manager
//   or GetAllGroups IFF GroupTotal()>0 after a GROUPS pump
// traders:
GetUserLogins(L"*")            // or per group name from the complete group list
GetUser(login) + GetAccount(login)
```

**Do not** treat `GetAllGroups()==[]` after fallback as “broker has no groups.”

`GetUserLogins(L"*")` does **not** need the group cache. If the manager ACL is “all groups,” one mask walk is the complete login set. If ACL is a subset, `*` still returns only what that manager may see — that **is** “ALL manager traders.”

---

## 5. Prop C# already copied the recipe (A001 is stale)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`:

```88:126:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            // ...
            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            // ...
            address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
            auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
```

Differences vs YoPips wrapper (C# is **stricter** for ALL-groups):

| Item | YoPips `MT5Manager` | Prop `NativeMt5BrokerConnector` |
|---|---|---|
| First pump includes `GROUPS` | **No** | **Yes** |
| No-pump retry | **Yes** (`0`) | **Yes** (`PUMP_MODE_NONE`) |
| Proxy `IP:port` / `login:password` | **Yes** | **Yes** |
| `GroupRequestArray("*")` | **Unused** | **First** in `GetGroupsCore` |
| Cache `GroupNext` | primary | only if request list empty |
| `UserRequestArray` / `UserLogins` | logins only | request array + logins fallback |
| `ProxySet` retcode | ignored | throws |

`A001_native_connector.md` (“no no-pump retry”, “zero GroupRequestArray in src”) is **stale** vs current disk. `A014_live_path_now.md` matches current sources.

---

## 6. Measured Achiever + Starwave census (prior live fetch — not this pass)

From `LIVE_MANAGER_FETCH_MEASURED.md` / `CREDENTIALS_AND_COPY_STATUS.md` (2026-08-18T08:45Z). Path: `GroupRequestArray` + `UserRequestArray`. Dummy seed off.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave groups: `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1–5` + `LP` (10 names).

If the trade server has more groups, they are **outside this manager ACL**. That is still “ALL manager groups.”

This slot did **not** re-attach. Treat 8/6512 + 10/1948 as last measured, not re-proven here.

---

## 7. Copy to cTrader — no live orders (no loss)

Goal constraint: fetch may run; **copy must not send live orders**.

| Check | Measured |
|---|---|
| `CTraderFixSession.BuildLogon` | tags start `(35, "A")` only (`CTraderFixSession.cs` L96) |
| `35=D` / `NewOrderSingle` builder in `Fix.CTrader` | **Absent** (comment + log string only) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (L35) |
| Hosted service after logon | `_runtime.RealCopyEnabled = false` (`CTraderFixLogonHostedService.cs` L68) |
| Log line | `"NewOrderSingle still disabled"` (L70) |
| `/api/settings` | `featureFlags.REAL_COPY_EXECUTION_ENABLED` from `runtime.RealCopyEnabled` |
| `D:\Prop\.env` key `REAL_COPY_EXECUTION_ENABLED` | **`false`** (value only; no other secrets quoted) |
| FIX worker | logs the flag; still refuses send |

Logon (QUOTE `:5211` / TRADE `:5212`, `TargetCompID=cServer`) is **not** a send license. Architecture §41 / §68 / §70: do not set `REAL_COPY_EXECUTION_ENABLED=true`.

YoPips C++ **does** have `DealerSendOrder` / `SendTrade` / `DealerBalance` on the **MT5** manager. That is the prop-firm backend, not Prop’s cTrader copy path. This research slot does not invoke it.

---

## 8. Recipe to copy (collector / future C++)

```
Achiever:
  IS_MT5_PROXY_ENABLED=true          // C++ name, not MT5_PROXY_ENABLED
  MT5_PROXY_TYPE=HTTP
  MT5_PROXY_ADDRESS=<allow-list host>   // 81.29.145.69 (non-secret)
  MT5_PROXY_PORT=49527
  MT5_PROXY_LOGIN / PASSWORD from secret store
  SetProxy(PROXY_HTTP, address, port, login, password)   // packs IP:port and login:password
  Connect(server:443, managerLogin, password, PUMP_MODE_GROUPS|USERS)  // pass a NONZERO mask
  if fail: Connect(..., 0) already happens inside wrapper

Starwave:
  proxy off
  same Connect

Enumerate ALL groups:
  GroupRequestArray("*")     // required on pump-none / missing GROUPS pump
  else GroupTotal/GroupNext only if GroupTotal()>0

Enumerate ALL traders:
  UserLogins("*") or UserLogins(each group)
  optional UserRequestArray / UserAccountRequestArray

Copy:
  persist catalog only
  FIX 35=A logon optional
  FORBID 35=D / REAL_COPY=true
```

Do **not** pass `pumpMode=0` expecting the first YoPips `Connect` to be request-only. Use the pool, or pass an explicit nonzero mask (include `PUMP_MODE_GROUPS` if you will walk `GroupNext`).

---

## 9. Honesty / stale pins

- This file is **not** a new live Manager attach.
- YoPips `GetAllGroups` after fallback is **not** ALL-groups complete.
- `A001` “C# has no pump-none fallback / no GroupRequestArray” is **stale**.
- `A004` “wrapper remaps 0; fallback is true none; `GroupRequestArray` unused” is **still true** for YoPips C++.
- Live census 18/8460 is **last measured** (`LIVE_MANAGER_FETCH_MEASURED.md`), not re-run here.
- No proxy password, manager password, or FIX password is written in this file.

---

## 10. Slot-1 contract

| Field | Value |
|---|---|
| `slot` | `1` |
| `verdict` | `CONFIRMED_WITH_GROUPS_CACHE_GAP` |
| `risk_to_capital` | `NONE` — no `35=D`; `REAL_COPY` forced false; this slot is read-only |
| `evidence` | YoPips `mt5_manager.cpp` L33–57 proxy `IP:port`/`login:password`; L102–135 remap-then-`Connect(...,0)`; `GetAllGroups` L962–981 cache-only; `UserLogins` L315–328 request; Prop C# already has fallback + `GroupRequestArray`; FIX `BuildLogon` is `35=A` only |
