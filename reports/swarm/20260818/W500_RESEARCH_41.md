# W500_RESEARCH_41 — YoPips `mt5_manager.cpp` Connect pump-none fallback + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **41** |
| Agent | W500_RESEARCH_41 (senior engineer, read-only) |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_41.md` |
| Topic | Read YoPips `mt5_manager.cpp` **Connect fallback to pump-none** and proxy packing **`address = IP:port`**, **`auth = login:password`**. |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Config / `.env` / `appsettings` edited | **No.** `.env` files were **read** for key names / non-secret host:port only. |
| Secret values printed | **None.** Manager / proxy / FIX passwords classified PRESENT only. Auth strings never copied. |
| Live Connect this pass | **Not re-run.** No Manager attach, no HTTP CONNECT, no FIX send. Census quoted from `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`utc=2026-08-18T08:42:16Z`). |
| Method | `read_file` / `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. |
| Siblings (do not treat as this file) | W500_RESEARCH_1 (same topic, slot 1), A004, A001 (stale on C# fallback), R012, LIVE_MANAGER_FETCH_MEASURED, E002, A003, W500_RESEARCH_8 / 10 / 23 |

Independent re-read of the same recipe as slot 1. Verdict is from **this** pass’s source bytes, not a copy of that report.

---

## 0. Verdict (binding)

**CONFIRMED_WITH_GROUPS_CACHE_GAP**

| Claim | Result | Class |
|---|---|---|
| YoPips `MT5Manager::Connect` retries SDK `Connect(..., pump_mode=0, 30000)` after a pump fail | **Yes** | `mt5_manager.cpp` 114–135. Identical block in `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`. |
| Wrapper `Connect(..., pumpMode=0)` is already pump-none on the first SDK call | **No** | `if (mode == 0)` remaps to `USERS\|ORDERS\|POSITIONS\|SYMBOLS` (`0x00000289`). True none is the **retry** and the **pool**. |
| Proxy packing is official `address=IP:port` and `auth=login:password` | **Yes** | `SetProxy` 42–49 and Connect re-apply 82–87. Matches vendor `MTProxyInfo` comments. |
| Achiever from this LAN needs that HTTP hop | **Yes** | Allow-list `81.29.145.69`. Type `PROXY_HTTP=2`. Hop `81.29.145.69:49527`. Direct egress ≠ allow-list → **1012**. |
| Starwave needs that hop | **No** | C# `ProxyEnabled = false`. Census: **OK direct**. |
| YoPips `GetAllGroups` after pump-none is ALL groups | **No** | Cache walk `GroupTotal`/`GroupNext`. `GroupRequestArray` **unused** under YoPips `src\`. Empty `[]` + `true` is ambiguous. |
| ALL manager traders can be listed on pump-none | **Yes** | `UserLogins` is a **network** request (`GetUserLogins` 315–328). Mask `*` or each group name. |
| Prop C# already has fallback **and** a complete enumerator | **Yes** | `NativeMt5BrokerConnector.ConnectCore` pump then `PUMP_MODE_NONE`; `GetGroupsCore` uses `GroupRequestArray("*")`. |
| Last measured ALL-groups / ALL-traders census | **18 / 8460** | Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct). Path: `GroupRequestArray` + `UserRequestArray`. **Not re-proven this slot.** |
| Copy to cTrader can send a live order in this process | **No** | `BuildLogon` is `35=A` only. **Zero** `35=D` builders. `RealCopyEnabled` forced **false**. `SAFE_BY_ABSENCE`. |

Honest one-liner:

```text
SetProxy packs address="IP:port" and auth="login:password" (64-wchar).
MT5Manager::Connect remaps wrapper 0 → USERS|ORDERS|POSITIONS|SYMBOLS,
then on fail retries SDK pump_mode=0 (true pump-none, request APIs live).
GetAllGroups after that fallback is a COLD CACHE — not ALL groups.
ALL traders = UserLogins (or C# UserRequestArray). ALL groups = GroupRequestArray("*").
Achiever: HTTP ProxySet 81.29.145.69:49527 before Connect. Starwave: direct.
cTrader copy: 35=A logon only. No 35=D. REAL_COPY=false. Capital risk = NONE.
```

---

## 1. Files measured (this pass)

| Path | What was measured |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | `SetProxy` 33–58; `mt5ErrorReason` 61–68; `Connect` 71–150; `GetUserLogins` 315–328; `GetAccount` cache→request 339–348; `GetPositions` 396–426; `GetOrders` **no-pump branch** 440–443; `GetDeals` `DealRequest` 485–509; `GetAllGroups` 962–981; `GetGroupDetails` 984–1013; `GetGroupLogins` alias 1015–1016; `LogAvailableGroups` 1089–1106; `DealerSendOrder`/`SendTrade` 1119+ (MT5 dealer, **not** cTrader) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `Connect(..., pumpMode = 0)` default; `m_pumpMode`; `ProxyConfig` / `m_proxyApplied` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` 499–508 | `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` 25–84, 731–748 | Pool `Connect` is **true** `mode=0` first; same proxy packing; `GetAllGroups` still cache-only |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` 201–276 | `IS_MT5_PROXY_ENABLED` → `SetProxy` **before** Connect; pool init even if pump fails |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` 49–65, 113–114 | Probe `Connect(..., 0)` — wrapper remaps; comment “No pump mode required” is **false** for the first SDK call |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` 167–172 | Env keys C++ actually binds |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env` | `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`, `MT5_PROXY_ENABLED=true`. **`IS_MT5_PROXY_ENABLED` absent.** Login/password keys not quoted. |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` 76–92, 125–144, 162–164, 206–212, 254, 410 | Vendor `MTProxyInfo`, `EnPumpModes`, `Connect`, `GroupRequestArray`, `UserLogins`, `UserRequestArray` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` 33–150, 315–328, 962–981 | **Byte-identical** Connect / SetProxy / GetUserLogins / GetAllGroups to YoPips |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | C# pump then `PUMP_MODE_NONE`; `address=host:port` / `auth=user:pass`; `GroupRequestArray` first |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `BuildLogon` tag 35=`A` only; write path is that one buffer |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | QUOTE 5211 / TRADE 5212; forces `_runtime.RealCopyEnabled = false` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` 38–41 | DI pins `RealCopyEnabled = false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` 41–44 | Snapshot copyNote: no capital at risk |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Prior live census |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | `probe=LiveBrokerProbe`, note “Passwords never written” |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | This LAN egress ≠ allow-list; historical 1012 with proxy disabled |

`GroupRequestArray` under YoPips `src\` = **0 hits**.  
`SendTrade` / `DealerSend` / `35=D` under `D:\Prop\src\Mt5` = **0 hits**.  
`35=D` under `D:\Prop\src\Fix.CTrader` product send path = **0** (name/log/comment only).

---

## 2. Vendor contract (do not invent)

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

SDK `pump_mode=0` is pump-none (no subscriptions). C++ `EnPumpModes` has **no** named `NONE`; official .NET WebAPI sample names it `PUMP_MODE_NONE = 0x00000000`. Same integer.

| Flag | Value | YoPips default remap (`mode==0`) | C# first pump |
|---|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | **Yes** | **Yes** |
| `PUMP_MODE_ORDERS` | `0x00000008` | **Yes** | No |
| `PUMP_MODE_POSITIONS` | `0x00000080` | **Yes** | **Yes** |
| `PUMP_MODE_SYMBOLS` | `0x00000200` | **Yes** | No |
| `PUMP_MODE_GROUPS` | `0x00000100` | **No** | **Yes** |
| none | `0` | **Fallback only** | **Fallback** (`PUMP_MODE_NONE`) |

YoPips remapped OR = **`0x00000289`**. C# first pump OR = **`0x00000181`**.

Completeness APIs (vendor):

| API | Header | Kind | Used by YoPips `MT5Manager`? |
|---|---|---|---|
| `GroupTotal` / `GroupNext` | 206–207 | **Pump cache** | **Yes** — `GetAllGroups` |
| `GroupRequestArray(mask)` | 212 | **Network** — ALL groups this manager ACL allows | **No** |
| `UserLogins(group, …)` | 254 | **Network** — ALL logins matching the mask | **Yes** — `GetUserLogins` |
| `UserRequestArray` | 410 | **Network** — full user records | **No** (C# uses it) |

---

## 3. Proxy packing — `address=IP:port`, `auth=login:password`

Measured in **both** `SetProxy` and the Connect-time re-apply. Source comments are literal, not inferred.

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

Connect re-applies the same packing when `m_proxyConfig.enabled && !m_proxyApplied` (76–96). Logs **type + host + port only** — not `auth`. Good.

Same packing on `MT5Session::SetProxy` / `Connect` (`mt5_pool.cpp` 25–72).

Constraints (measured):

- `address[64]` / `auth[64]` are **wchar**. `81.29.145.69:49527` fits. `login:password` must stay under **63** wide chars or it is silently truncated (`wcsncpy_s` + `_countof-1`).
- Empty proxy login ⇒ `auth` left zeroed (no `:`). Non-empty login + empty password still writes `login:`.
- Vendor `ProxySet` is **void**. C++ does **not** check a retcode. C# `ApplyProxy` **does** throw if not `MT_RET_OK`.
- `SetProxy` sets `m_proxyApplied=false` so the **next** `Connect` re-sends `ProxySet` (hot-reload).
- `enable=1` is hardcoded when `SetProxy` is called. There is no “disable proxy” call besides skipping `SetProxy`.

### Env keys — do not mix C++ and C# names

| Layer | Master toggle | Host / port / user / pass | Type |
|---|---|---|---|
| YoPips C++ (`app_config.cpp` 167–172) | **`IS_MT5_PROXY_ENABLED`** (default **false**) | `MT5_PROXY_ADDRESS`, `MT5_PROXY_PORT`, `MT5_PROXY_LOGIN`, `MT5_PROXY_PASSWORD` | `MT5_PROXY_TYPE` = `HTTP`/`SOCKS4`/`SOCKS5` (else SOCKS5) |
| YoPips `.env` on disk (this pass) | `MT5_PROXY_ENABLED=true` | address/port present (non-secret host above) | `HTTP` |
| YoPips `AppConfig` actually reads | **`IS_MT5_PROXY_ENABLED`** | same `MT5_PROXY_*` | same |
| **`IS_MT5_PROXY_ENABLED` in that `.env`** | **Absent** | — | C++ local start logs `MT5 proxy mode: DISABLED (global)` unless the operator exports the `IS_` key |
| Prop C# (`LiveMt5Registration.cs` 30–45) | **`ACHIEVER_PROXY_ENABLED`** | `ACHIEVER_PROXY_HOST` / `PORT` / `USERNAME` / `PASSWORD` | Hardcoded `PROXY_HTTP` |
| Prop `.env` (this pass) | `ACHIEVER_PROXY_ENABLED=true` | host `81.29.145.69`, port `49527` | user/pass **PRESENT**, not printed |
| Starwave C# | forced `ProxyEnabled = false` | n/a | **direct** |

Type map in `main.cpp` 207–209: `SOCKS4`→0, `HTTP`→2, else SOCKS5→1.

Achiever allow-list egress (non-secret, C55): **`81.29.145.69`**. Intended hop: Manager `PROXY_HTTP` to **`81.29.145.69:49527`**. R012: this desktop public egress was **`106.219.132.213`**. TCP to Achiever `:443` is OPEN; failure without presenting the allow-list IP is **1012** `MT_RET_AUTH_MANAGER_IPBLOCK`, not reachability.

Pump-none fallback **does not cure 1012**. Historical YoPips local starts with proxy disabled failed 1012 on **both** pump and no-pump (R012).

`mt5ErrorReason` (`mt5_manager.cpp` 61–68) — stored in `m_lastError` **only** when **both** attempts fail:

| Code | Meaning | Typical cause here |
|---|---|---|
| 1012 | IP blocked (`MT_RET_AUTH_MANAGER_IPBLOCK`) | Achiever without presenting `81.29.145.69` |
| 7 | Network timeout | proxy/firewall |
| 5 | No connection | host/port |
| 3 | Auth failed | manager password |
| other | generic code string | — |

Successful fallback **clears** `m_lastError`.

---

## 4. Connect fallback — pump first, then pump-none

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
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // fail → m_lastError = mt5ErrorReason(res); return false
        // ok   → m_connected=true; m_pumpMode=false; no Position/Deal/Order/UserSubscribe
    }
    // pump ok: subscribe four sinks; m_pumpMode=true
}
```

### Binding facts

1. **Wrapper `pumpMode=0` ≠ SDK `pump_mode=0`.** Header default is `pumpMode = 0` (`mt5_manager.h` 31–32). `main.cpp` 230 calls `Connect(server, login, password)` with that default. `mt5_group_probe.cpp` 114 passes `0` on purpose — the probe comment is **wrong** for the first SDK call.
2. First SDK call uses remapped mask **`0x289` without `PUMP_MODE_GROUPS`**. A **successful** pump can still leave `GroupTotal()==0`.
3. Fallback is a **second** `Connect(..., L"", 0, 30000)`: empty cert, same 30 s timeout. Worst case the wrapper blocks **~60 s** (A15). Comment 118–121: pump can fail when IP is not yet whitelisted **for pump** while request-only still works. That is a **different** failure from 1012 (blocked for all manager access).
4. Fallback success: `m_pumpMode=false`, `m_newsPumpModeEnabled=false`, sinks for Position/Deal/Order/User **not** subscribed. `Subscribe(this)` already ran (connect/disconnect sink only).
5. Fallback does **not** unsubscribe `this` if the first Connect left a half-session. Vendor `Connect` is assumed to replace the session.
6. `password_cert` is always `L""`. Password is never logged (login number + server only).

### Three Connect styles in this tree

| Caller | First `pump_mode` | Fallback | Pump flag after success |
|---|---|---|---|
| `MT5Manager::Connect` (`main`, probe) | remapped `0x289` (or caller mask) | SDK `0` | `m_pumpMode` true / false |
| `MT5Session::Connect` (request pool) | SDK **`0` immediately** (`mt5_pool.cpp` 74–76) | none | n/a |
| Prop `NativeMt5BrokerConnector.ConnectCore` | `GROUPS\|USERS\|POSITIONS` (`0x181`) | `PUMP_MODE_NONE` | `_pumpEnabled` |

`main.cpp` 241–242: pool is initialized **even if pump fails**, because pool sessions are request-only. That is the production “true pump-none” path in YoPips.

---

## 5. What still works on pump-none (ALL traders vs ALL groups)

| Method | Lines | After `m_pumpMode=false` | ALL-complete? |
|---|---|---|---|
| `GetAllGroups` | 962–981 | `GroupTotal`/`GroupNext` cache | **No.** Empty list + `return true` is valid |
| `GetGroupDetails` | 984–1013 | same cache | **No** |
| `LogAvailableGroups` | 1089–1106 | same cache; logs first 50 | diagnostic only |
| `GetUserLogins` / `GetGroupLogins` | 315–328 / 1015–1016 | `UserLogins` network | **Yes, for the mask** (`*` or each group name) |
| `GetUser` | `UserRequest` | network | per login |
| `GetAccount` | 339–348 cache then `UserAccountRequest` | **Yes** (explicit no-pump comment) | per login |
| `GetPositions` | 405–408 `PositionGet` then `PositionRequest` | **Yes** per login | |
| `GetOrders` | **440–443 skip cache when `!m_pumpMode`** → `OrderRequestOpen` | **Yes** per login | Measured this slot: fallback is first-class, not an afterthought |
| `GetDeals` | 492 `DealRequest` | **Yes** (always network) | per login + window; **no** `DealRequestPage` |

`GetUserLogins` fail-closed: `res != MT_RET_OK || !raw_logins` → `false`. An empty group that returns a null pointer looks like an API failure. Callers must not treat that as “zero traders exist.”

**ALL-traders recipe (YoPips APIs, pump-none safe):**

```
SetProxy(HTTP, host, port, login, password)   // Achiever only; BEFORE Connect
Connect(server:port, managerLogin, password)  // remapped pump, then mode=0
// groups:
//   vendor GroupRequestArray("*")  — NOT implemented in MT5Manager
//   or GetAllGroups IFF GroupTotal()>0 after a GROUPS pump
// traders:
GetUserLogins(L"*")            // or per group name from the complete group list
GetUser(login) + GetAccount(login)
```

**Do not** treat `GetAllGroups()==[]` after fallback as “broker has no groups.”

`GetUserLogins(L"*")` does **not** need the group cache. If the manager ACL is “all groups,” one mask walk is the complete login set. If ACL is a subset, `*` still returns only what that manager may see — that **is** “ALL manager traders.” Groups outside the ACL are invisible by design (LIVE_MANAGER_FETCH_MEASURED).

YoPips `DealerSendOrder` / `SendTrade` / `DealerBalance` **exist** on the Manager handle. They are the **prop-firm MT5 dealer** path, not Prop’s cTrader copy path. This slot did not call them. Prop C# `NativeMt5BrokerConnector` has **no** send/dealer method.

---

## 6. Prop C# already copied the recipe (and closed the groups gap)

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

`GetGroupsCore` (144–186): `GroupRequestArray("*")` **first**; `GroupTotal`/`GroupNext` only if the request list is empty.

`ReadAccountsForGroup` (216–233): `UserRequestArray` first; `UserGetByGroup` on hard fail; `UserLogins` + `UserRequestByLogins` if still empty.

| Item | YoPips `MT5Manager` | Prop `NativeMt5BrokerConnector` |
|---|---|---|
| First pump includes `GROUPS` | **No** | **Yes** |
| No-pump retry | **Yes** (`0`) | **Yes** (`PUMP_MODE_NONE`) |
| Proxy `IP:port` / `login:password` | **Yes** | **Yes** |
| `GroupRequestArray("*")` | **Unused** | **First** in `GetGroupsCore` |
| Cache `GroupNext` | primary | only if request list empty |
| `UserRequestArray` / `UserLogins` | logins only | request array + logins fallback |
| `ProxySet` retcode | ignored | throws |
| MT5 `DealerSend` / `SendTrade` | **present** (YoPips dealer) | **absent** |

`A001_native_connector.md` (“C# has no no-pump retry”, “zero `GroupRequestArray` under `D:\Prop\src`”) is **stale** vs current disk. `A004` remains **true** for YoPips C++.

---

## 7. Measured Achiever + Starwave census (prior live fetch — not this pass)

From `LIVE_MANAGER_FETCH_MEASURED.md` + `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded: true`, note: “Passwords never written”). Path: **`GroupRequestArray` + `UserRequestArray`**. Dummy seed off.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23). `demo\Maxmaster` is **absent**.

Starwave groups: `Starwave\cent\FX1\grp1/2`, `Starwave\demo\FX2\grp1/2`, `Starwave\real\FX3\grp1–5` + `LP` (10 names). Three real groups have **0** accounts and are still listed — evidence the walk is group-first.

If the trade server has more groups, they are **outside this manager ACL**. That is still “ALL manager groups.”

This slot did **not** re-attach. Treat 8/6512 + 10/1948 as last measured, not re-proven here.

---

## 8. Copy to cTrader — no live orders (no loss)

Goal split (do not collapse):

| Job | Live I/O allowed? | Capital at risk? |
|---|---|---|
| **A.** Fetch ALL Achiever + Starwave groups and ALL manager traders | **Yes** — Manager **read** | **No** |
| **B.** Copy those traders to cTrader | **Not yet** — SHADOW / CopyIntent / FIX logon only | **Would be yes** the moment `35=D` exists |

Job A does **not** license Job B.

| Check | Measured |
|---|---|
| `CTraderFixSession.TryLogonAsync` | Builds **one** outbound buffer via `BuildLogon`, then `ssl.ReadAsync`. No second writer. |
| `BuildLogon` | fields start `(35, "A")` (`CTraderFixSession.cs` 96). Tags 34/49/56/50/57/52/98/108/141/553/554 only. |
| `35=D` / `NewOrderSingle` builder in `Fix.CTrader` | **Absent** (comment + log string only) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (L35) |
| Hosted service after logon | `_runtime.RealCopyEnabled = false` (`CTraderFixLogonHostedService.cs` 68) |
| Log line | `"NewOrderSingle still disabled"` (L70) |
| DI | `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” (`DependencyInjection.cs` 38–41) |
| Snapshot | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` (`LiveRuntimeStatus.cs` 43–44) |
| `D:\Prop\.env` `REAL_COPY_EXECUTION_ENABLED` | **`false`** |
| Ports / TargetCompID | QUOTE TLS **5211**, TRADE TLS **5212**, `56=cServer` (case preserved). Logon ≠ send license. |

Architecture §41 / §68 / §70: do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. §68 is still 0/19; §70 is still 0/14. `SAFE_BY_ABSENCE` is the current no-loss outcome, **not** a passed go-live review.

---

## 9. Recipe to copy (collector / future C++)

```
Achiever:
  IS_MT5_PROXY_ENABLED=true          // C++ name — NOT MT5_PROXY_ENABLED
  MT5_PROXY_TYPE=HTTP
  MT5_PROXY_ADDRESS=81.29.145.69     // non-secret allow-list / hop host
  MT5_PROXY_PORT=49527
  MT5_PROXY_LOGIN / PASSWORD from secret store (never log)
  SetProxy(PROXY_HTTP, address, port, login, password)
      // packs address="IP:port", auth="login:password"
  Connect(server:443, managerLogin, password, PUMP_MODE_GROUPS|USERS)  // pass a NONZERO mask
  if fail: wrapper already retries Connect(..., 0)

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
  FIX 35=A logon optional (QUOTE 5211 / TRADE 5212)
  FORBID 35=D / 35=F / 35=G / REAL_COPY=true
```

Do **not** pass `pumpMode=0` expecting the first YoPips `Connect` to be request-only. Use the pool, or pass an explicit nonzero mask (include `PUMP_MODE_GROUPS` if you will walk `GroupNext`).

---

## 10. Honesty / stale pins

- This file is **not** a new live Manager attach and **not** a live FIX send.
- YoPips `GetAllGroups` after fallback is **not** ALL-groups complete. That is the **GROUPS_CACHE_GAP**.
- YoPips `.env` `MT5_PROXY_ENABLED=true` is **not** the key `AppConfig` reads. C++ local attach from that file alone **skips** `SetProxy` unless `IS_MT5_PROXY_ENABLED` is exported. Prop C# uses a different, actually-bound `ACHIEVER_PROXY_*` set.
- `A001` “C# has no pump-none fallback / no GroupRequestArray” is **stale**.
- `A004` “wrapper remaps 0; fallback is true none; `GroupRequestArray` unused” is **still true** for YoPips C++.
- Live census 18/8460 is **last measured**, not re-run here.
- YoPips `SendTrade` is an MT5 dealer API. It is **out of scope** for “copy to cTrader” and was not invoked.
- No proxy password, manager password, or FIX password is written in this file.

---

## 11. Slot-41 contract

| Field | Value |
|---|---|
| `slot` | `41` |
| `verdict` | `CONFIRMED_WITH_GROUPS_CACHE_GAP` |
| `risk_to_capital` | `NONE` — no `35=D`; `REAL_COPY` forced false; this slot is read-only |
| `evidence` | YoPips `mt5_manager.cpp` L33–57 proxy `IP:port`/`login:password`; L102–135 remap-then-`Connect(...,0)`; `GetOrders` L440–443 no-pump `OrderRequestOpen`; `GetAllGroups` L962–981 cache-only; `UserLogins` L315–328 request; Prop C# already has fallback + `GroupRequestArray`; FIX `BuildLogon` is `35=A` only |
