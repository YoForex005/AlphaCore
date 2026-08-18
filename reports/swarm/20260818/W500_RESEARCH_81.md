# W500_RESEARCH_81 — YoPips `mt5_manager.cpp` Connect pump-none fallback + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **81** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_81 |
| Topic | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy address `IP:port` auth `login:password` |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secret values printed | **None** (proxy/manager/FIX passwords classified only; lengths not re-quoted from `.env`) |
| Live Connect this pass | **Not re-run.** Census cited below is the prior measured fetch (`LIVE_MANAGER_FETCH_MEASURED.md`, 2026-08-18). This slot is a source/recipe re-audit of current disk. |
| Sibling (same topic, slot 1) | `W500_RESEARCH_1.md` — this file independently re-read the same trees; no Connect/SetProxy drift found. |

## Verdict

**CONFIRMED_WITH_GROUPS_CACHE_GAP**

YoPips `MT5Manager::Connect` **does** retry `IMTManagerAPI::Connect(..., pump_mode=0, 30000)` after a pumped connect fails. Proxy packing **is** vendor-correct: `MTProxyInfo.address = IP:port` and `MTProxyInfo.auth = login:password` (`wchar_t[64]` each). That is the Manager-API recipe Achiever needs from this LAN (`PROXY_HTTP` to allow-list hop `81.29.145.69:49527`). Starwave must stay **direct**.

It is **not** sufficient, by itself, to claim “ALL groups” after the fallback:

1. Passing `pumpMode=0` into the **wrapper** is **not** pump-none on the first SDK call. The wrapper remaps `0` → `USERS|ORDERS|POSITIONS|SYMBOLS` (`0x00000289`) and **omits `PUMP_MODE_GROUPS` (`0x00000100`)**.
2. True pump-none is the **retry** (`mode=0`) and the **pool** session (`MT5Session::Connect` first call is already `0`).
3. `GetAllGroups` / `GetGroupDetails` walk the **local group cache** (`GroupTotal`/`GroupNext`). `GroupRequestArray` hits under YoPips `src\` = **0**. After a no-pump fallback that cache can be empty while the manager still sees groups.
4. ALL traders **can** be fetched on pump-none via `UserLogins` (network request). `GetUserLogins` / `GetGroupLogins` wrap it. `UserRequestArray` is unused in YoPips C++ `src\`.

Prop C# `NativeMt5BrokerConnector` already copies the YoPips fallback **and** is stricter for ALL-groups: first pump includes `PUMP_MODE_GROUPS`; retry is `PUMP_MODE_NONE`; groups walk `GroupRequestArray("*")` first. Prior measured census: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**.

cTrader copy **cannot place a live order today**: `CTraderFixSession.BuildLogon` emits `35=A` only; product `*.cs` has **0** `35=D` literals; `RealCopyExecutionEnabled` default **false**; DI and hosted logon both force `RealCopyEnabled = false`. Risk to capital on the copy path is **NONE** (`SAFE_BY_ABSENCE`). Do not enable `REAL_COPY_EXECUTION_ENABLED`.

---

## 0. Files read (this pass)

| Path | Lines / what was measured |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | **1559** lines. `SetProxy` 33–58; `mt5ErrorReason` 61–68; `Connect` 71–150; `GetUserLogins` 315–328; `GetAllGroups` 962–981; `GetGroupLogins` 1015–1016 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `Connect(..., pumpMode=0)` default L31–32; `m_pumpMode`; `ProxyConfig` L205–206 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` | `ProxyConfig` L501–508 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | `SetProxy` 25–46 same packing; `Connect` 48–84 **SDK `0` first** |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` | L201–276: `IS_MT5_PROXY_ENABLED` → `SetProxy` before Connect; pool always mode=0 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | L167–172: env keys actually bound |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` | L113–114: `Connect(..., 0)` with false “no pump required” comment |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | `MTProxyInfo` 76–92; `EnPumpModes` 125–144; `ProxySet`/`Connect` 162–164; `GroupTotal`/`GroupNext`/`GroupRequestArray` 205–212; `UserLogins` 254; `UserRequestArray` 410 |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | **1559** lines. Connect/SetProxy/`GetUserLogins`/`GetAllGroups` **byte-identical** to YoPips on the measured blocks |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | **459** lines. Pump then `PUMP_MODE_NONE`; proxy `host:port`/`user:pass`; `GroupRequestArray("*")` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` (hard) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | **135** lines. `BuildLogon` is `(35, "A")` only |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` L35 |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; `_runtime.RealCopyEnabled = false` L68 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` L38–42; Native ×2 only |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` — no `Take(` cap |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | In-memory simulate only; no FIX write |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Prior live census |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Creds present (names only); `35=D` OFF |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | This LAN egress ≠ allow-list; Achiever needs HTTP hop |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` | `PUMP_MODE_NONE = 0x00000000` L42 |

`GroupRequestArray` under YoPips `src\` = **0 hits**. `GroupRequestArray` under `D:\Prop\mt5-sdk\src` = **0 hits**. `SendTrade`/`DealerSend` under `D:\Prop\src` `*.cs` = **0**. `35=D` under `D:\Prop` `*.cs`/`*.cpp`/`*.h`/`*.json` = **0**.

---

## 1. Vendor contract (do not invent)

```76:92:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
struct MTProxyInfo
  {
   enum
     {
      PROXY_SOCKS4   =0,                     // SOCKS4
      PROXY_SOCKS5   =1,                     // SOCKS5
      PROXY_HTTP     =2,                     // HTTP (including NTLM)
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

C++ `EnPumpModes` has **no** named `NONE`. SDK `pump_mode=0` is pump-none (no subscriptions). Official C# WebAPI sample names it:

```40:43:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs
    public enum EnPumpModes
      {
      PUMP_MODE_NONE = 0x00000000
      };
```

Pump flags used by this slot (`MT5APIManager.h` L127–143):

| Flag | Value | YoPips wrapper remap when `pumpMode==0`? | Prop C# first Connect? |
|---|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | **Yes** | **Yes** |
| `PUMP_MODE_ORDERS` | `0x00000008` | **Yes** | No |
| `PUMP_MODE_POSITIONS` | `0x00000080` | **Yes** | **Yes** |
| `PUMP_MODE_SYMBOLS` | `0x00000200` | **Yes** | No |
| `PUMP_MODE_GROUPS` | `0x00000100` | **No** | **Yes** |
| `PUMP_MODE_FULL` | `0xffffffff` | No | No |
| none | `0` | Fallback / pool only | Fallback (`PUMP_MODE_NONE`) |

YoPips default remap OR = `0x00000289`. Prop C# first mask = `GROUPS|USERS|POSITIONS` = `0x00000181`.

Completeness APIs (same header):

| API | Header line | Kind | Used by YoPips `MT5Manager`? |
|---|---|---|---|
| `GroupTotal` / `GroupNext` | 206–207 | **Cache** | **Yes** (`GetAllGroups`) |
| `GroupRequest` | 208 | Network, one name | unused in wrapper walk |
| `GroupRequestArray(mask)` | 212 | **Network** — ALL groups this manager ACL allows | **No** (0 hits in `src\`) |
| `UserLogins(group, logins, total)` | 254 | **Network** — ALL logins matching group mask | **Yes** |
| `UserRequestArray` | 410 | **Network** — full user records | **No** |
| `UserRequestByLogins` | 671 | **Network** | **No** |

---

## 2. Proxy packing — `address=IP:port`, `auth=login:password`

Measured in both `SetProxy` and the Connect-time re-apply. Source comments are literal vendor format.

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

Connect re-applies the same packing if `m_proxyConfig.enabled && !m_proxyApplied` (L76–96). Logs **type + host + port only** — not the auth string. Same packing on `MT5Session::SetProxy` / Connect (`mt5_pool.cpp` L25–72). Prop C# `ApplyProxy` (L115–130) uses the same strings: `address = $"{host}:{port}"`, `auth = $"{user}:{password}"`, type hardcoded `PROXY_HTTP`.

Constraints (measured):

- `address[64]` / `auth[64]` are **wchar**. `IP:port` fits. `login:password` must stay under 63 wide chars or it is silently truncated (`wcsncpy_s` + `_countof-1`).
- Empty proxy login ⇒ `auth` left zeroed (no `:`). Empty password with a non-empty login still writes `login:`.
- C++ `ProxySet` is **void**; no retcode check. C# `ApplyProxy` **does** check `MT_RET_OK` and throws.
- `SetProxy` sets `m_proxyApplied=false` so the next `Connect` re-sends `ProxySet`.
- `enable` is always `1` when packing; there is no `ProxySet` disable path besides not calling it.

### Env keys (do not mix)

| Layer | Master toggle | Host / port / user / pass | Type |
|---|---|---|---|
| YoPips C++ (`app_config.cpp` 167–172) | **`IS_MT5_PROXY_ENABLED`** (default **false**) | `MT5_PROXY_ADDRESS`, `MT5_PROXY_PORT`, `MT5_PROXY_LOGIN`, `MT5_PROXY_PASSWORD` | `MT5_PROXY_TYPE` = `HTTP`/`SOCKS4`/`SOCKS5` |
| YoPips `.env` may also have | `MT5_PROXY_ENABLED` | same family | **Not read** by `AppConfig` (R012 toggle bug) |
| Prop C# (`LiveMt5Registration.cs` 30–45) | **`ACHIEVER_PROXY_ENABLED`** | `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD` | Hardcoded `PROXY_HTTP` |
| Starwave C# | forced `ProxyEnabled = false` L45 | n/a | **direct** — do not reuse Achiever hop |

Type map in `main.cpp` L207–209: `SOCKS4`→0, `HTTP`→2, else SOCKS5→1.

Achiever allow-list egress (non-secret, C55/R012): **`81.29.145.69`**. Intended hop: Manager `PROXY_HTTP` to **`81.29.145.69:49527`**. This desktop public egress was measured **`106.219.132.213`**. TCP to Achiever `:443` is OPEN; failure without proxy is **1012** `MT_RET_AUTH_MANAGER_IPBLOCK`, not reachability. Starwave does **not** need this proxy.

Historical YoPips local starts with `IS_MT5_PROXY_ENABLED` unset logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump (R012). Fallback does **not** cure an IP block.

`mt5ErrorReason` (`mt5_manager.cpp` 61–68) — set **only** when **both** attempts fail:

| Code | Meaning | Typical cause here |
|---|---|---|
| 1012 | IP blocked | Achiever without presenting `81.29.145.69` |
| 7 | Network timeout | proxy/firewall |
| 5 | No connection | host/port |
| 3 | Auth failed | manager password |
| other | generic | — |

C# `Describe` (L444–454) maps the same 7/1012/3/5 plus 10/9.

---

## 3. Connect fallback — pump first, then pump-none

```71:135:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
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
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // fail: m_lastError = mt5ErrorReason(res); return false
        // ok:   m_connected=true; m_pumpMode=false; no Position/Deal/Order/UserSubscribe
    }
}
```

### Binding facts

1. **Wrapper `pumpMode=0` ≠ SDK `pump_mode=0`.** Header default is `pumpMode = 0`. `main.cpp` L230 calls `Connect(server, login, password)` with that default. `mt5_group_probe.cpp` L113–114 passes `0` on purpose (“No pump mode required”) — that comment is **false** for the first SDK call.
2. First SDK call uses remapped mask **without `PUMP_MODE_GROUPS`**. Even a **successful** pump can leave `GroupTotal()==0` until something else fills the group cache.
3. Fallback is a **second** `Connect(..., L"", 0, 30000)` with empty cert, same 30s timeout. Comment (L118–121): pump can fail when IP is not yet whitelisted **for pump** while request-only still works. That is a **different** failure from 1012 (blocked for all manager access).
4. Fallback success: `m_pumpMode=false`, `m_newsPumpModeEnabled=false`, sinks for Position/Deal/Order/User **not** subscribed. `Subscribe(this)` already ran (manager connect/disconnect sink only).
5. Fallback does **not** unsubscribe `this` if the first Connect left a half-session. Vendor `Connect` is assumed to replace the session.
6. `password_cert` is always `L""`.
7. News pump is recorded only when the **caller** passed a nonzero mask containing `PUMP_MODE_NEWS` or `PUMP_MODE_FULL`. Default remap never enables news.

### Three Connect styles in this tree

| Caller | First `pump_mode` | Fallback | Resulting pump flag |
|---|---|---|---|
| `MT5Manager::Connect` (`main`, probe) | remapped default `0x289` (or caller mask) | SDK `0` | `m_pumpMode` true/false |
| `MT5Session::Connect` (request pool) | SDK **`0` immediately** (`mt5_pool.cpp` L74–76) | none | n/a |
| Prop `NativeMt5BrokerConnector.ConnectCore` | `GROUPS\|USERS\|POSITIONS` (`0x181`) | `PUMP_MODE_NONE` | `_pumpEnabled` |

Pool comment in `main.cpp` L241–242: pool is initialized **even if pump fails**, because pool sessions are request-only.

---

## 4. What still works on pump-none (ALL traders vs ALL groups)

| Method | Lines | After `m_pumpMode=false` | ALL-complete? |
|---|---|---|---|
| `GetAllGroups` | 962–981 | `GroupTotal`/`GroupNext` cache | **No.** Empty list + `return true` is a valid outcome |
| `GetGroupDetails` | 984–1013 | same cache | **No** |
| `GetUserLogins` / `GetGroupLogins` | 315–328 / 1015–1016 | `UserLogins` network | **Yes, for the mask** (`*` or each group name) |
| `GetAccount` | 339–348 cache then `UserAccountRequest` | **Yes** (explicit no-pump comment) | per login |
| `GetPositions` | cache then `PositionRequest` | **Yes** per login | |
| `GetOrders` | skips cache when `!m_pumpMode` | **Yes** per login | |
| `GetDeals` | `DealRequest` | **Yes** (always network) | per login + window |

`GetUserLogins` fail-closed: `res != MT_RET_OK || !raw_logins` → `false`. An empty group that returns a null pointer looks like an API failure. Caller must treat that carefully.

**ALL traders recipe (YoPips APIs, pump-none safe):**

```
SetProxy(HTTP, host, port, login, password)   // Achiever only; before Connect
Connect(server:port, managerLogin, password)  // remapped pump, then mode=0
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

## 5. Prop C# already copied the recipe (and closed the groups gap)

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

`GetGroupsCore` (L144–186): `GroupRequestArray("*")` first; `GroupTotal`/`GroupNext` only if the request list is empty.

`GetAccountsCore(null)` (L189–214): walks **every** group from `GetGroupsCore`, then per group `UserRequestArray` → `UserGetByGroup` → `UserLogins`+`UserRequestByLogins`.

`DealIngestionService.SyncCatalogAsync` (L38–51): `GetGroupsAsync` + `GetAccountsAsync(null)` — this is the product ALL-groups/ALL-traders path.

| Item | YoPips `MT5Manager` | Prop `NativeMt5BrokerConnector` |
|---|---|---|
| First pump includes `GROUPS` | **No** | **Yes** |
| No-pump retry | **Yes** (`0`) | **Yes** (`PUMP_MODE_NONE`) |
| Proxy `IP:port` / `login:password` | **Yes** | **Yes** |
| `GroupRequestArray("*")` | **Unused** | **First** in `GetGroupsCore` |
| Cache `GroupNext` | primary | only if request list empty |
| `UserRequestArray` / `UserLogins` | logins only | request array + logins fallback |
| `ProxySet` retcode | ignored | throws |

`A001_native_connector.md` (“no no-pump retry”, “zero GroupRequestArray in src”) is **stale** vs current C# disk. `A004` (“wrapper remaps 0; fallback is true none; `GroupRequestArray` unused”) is **still true** for YoPips C++.

---

## 6. Measured Achiever + Starwave census (prior live fetch — not this pass)

From `LIVE_MANAGER_FETCH_MEASURED.md` / `CREDENTIALS_AND_COPY_STATUS.md`. Path: `GroupRequestArray` + `UserRequestArray`. Dummy seed off.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| Achiever | OK via HTTP proxy | 8 | 6512 | 1506 |
| StarwaveFX | OK direct | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (manager-visible): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23). Sum **6512**.

Starwave groups: `Starwave\cent\FX1\grp1` (11), `grp2` (4), `Starwave\demo\FX2\grp1` (170), `grp2` (1735), `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `LP` (2). Sum **1948**.

If the trade server has more groups, they are **outside this manager ACL**. That is still “ALL manager groups.”

This slot did **not** re-attach. Treat 8/6512 + 10/1948 as last measured, not re-proven here.

---

## 7. Copy to cTrader — no live orders (no loss)

Goal constraint: fetch may run; **copy must not send live orders**.

| Check | Measured |
|---|---|
| `CTraderFixSession.BuildLogon` | tags start `(35, "A")` only (`CTraderFixSession.cs` L96). File is 135 lines; one `WriteAsync` (the logon). Sockets disposed. |
| `35=D` / `NewOrderSingle` builder in product `*.cs`/`*.json` | **0 hits** (`SAFE_BY_ABSENCE`) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (L35) |
| DI | `_runtime.RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” (`DependencyInjection.cs` L38–42) |
| Hosted service after logon | `_runtime.RealCopyEnabled = false` (`CTraderFixLogonHostedService.cs` L68) |
| Log line | `"NewOrderSingle still disabled"` (L70) |
| `LiveRuntimeStatus.copyNote` when flag false | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| `ShadowCopyEngine` | `SimulateEntry`/`SimulateExit` only — no socket |
| `D:\Prop\src` C# `SendTrade`/`DealerSend` | **0** |
| YoPips C++ `SendTrade`/`DealerSendOrder`/`DealerBalance` | **Exist** on the **MT5** prop-firm backend. Not Prop’s cTrader copy path. This slot does not invoke them. |

Logon (QUOTE `:5211` / TRADE `:5212`, `TargetCompID=cServer`) is **not** a send license. Architecture §41 / §68 / §70: do not set `REAL_COPY_EXECUTION_ENABLED=true`.

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
- YoPips `src` still has **0** `GroupRequestArray` call sites (re-grepped this pass).
- Prop C# **does** have the request-array walk; that is how 18/8460 was measured.
- `A001` “C# has no pump-none fallback / no GroupRequestArray” is **stale**.
- `A004` “wrapper remaps 0; fallback is true none; `GroupRequestArray` unused” is **still true** for YoPips C++.
- Live census 18/8460 is **last measured** (`LIVE_MANAGER_FETCH_MEASURED.md`), not re-run here.
- No proxy password, manager password, or FIX password is written in this file.
- Product source was not edited.

---

## 10. Slot-81 contract

| Field | Value |
|---|---|
| `slot` | `81` |
| `verdict` | `CONFIRMED_WITH_GROUPS_CACHE_GAP` |
| `risk_to_capital` | `NONE` — no `35=D`; `REAL_COPY` forced false; this slot is read-only |
| `evidence` | YoPips `mt5_manager.cpp` L33–57 proxy `IP:port`/`login:password`; L102–135 remap-then-`Connect(...,0)`; `GetAllGroups` L962–981 cache-only; `UserLogins` L315–328 request; Prop C# already has fallback + `GroupRequestArray`; FIX `BuildLogon` is `35=A` only |
