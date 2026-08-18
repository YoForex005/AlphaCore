# W500_RESEARCH_181 — YoPips `mt5_manager.cpp` Connect pump-none fallback + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **181** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_181 |
| Topic | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy address `IP:port` auth `login:password` |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secret values printed | **None** (manager / proxy / FIX passwords classified only; never copied) |
| Live Connect this pass | **Not re-run.** Census below is independently re-summed from prior measured fetch `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00`. This slot remasured the C++/C# recipe against current sources. |
| Sibling remasures (same topic) | `W500_RESEARCH_1.md`, `W500_RESEARCH_21.md`, `W500_RESEARCH_61.md`, `W500_RESEARCH_101.md`, `W500_RESEARCH_141.md` — Connect/SetProxy **unchanged**. Slots that claimed DI+hosted pin `RealCopyEnabled=false` / `.env` false are **stale**. Slot 141 “product `35=D` = 0 builders” is **stale as a tree-wide claim**: a **demo-only** `Build("D")` helper now exists (not the copy hop). |

## Verdict

**CONFIRMED_WITH_GROUPS_CACHE_GAP**

YoPips `MT5Manager::Connect` **does** retry the vendor SDK as `Connect(..., pump_mode=0, 30000)` after the first (pumped) connect fails. That SDK `0` is true pump-none. Proxy packing **is** the vendor contract: `MTProxyInfo.address = "IP:port"` and `MTProxyInfo.auth = "login:password"` (64-wchar buffers). Prop `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` is the **same** block (L33–150), remasured this slot against YoPips.

It is **not** enough, by itself, to claim “ALL groups” from the YoPips wrapper after that fallback:

1. Wrapper `pumpMode=0` is **not** pump-none on the **first** SDK call. `mt5_manager.h` defaults `pumpMode=0`; the body remaps `0` → `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS` (`0x00000289` = 649) and **omits `PUMP_MODE_GROUPS` (`0x00000100`)**.
2. True pump-none is the **retry** (`mode=0`) and the **pool** (`MT5Session::Connect` first call is already `0`).
3. `GetAllGroups` / `GetGroupDetails` / `LogAvailableGroups` walk the **local group cache** (`GroupTotal` + `GroupNext`). `GroupRequestArray` hits under YoPips `src\` = **0**. After a no-pump fallback that cache can be empty while the manager ACL still has groups.
4. ALL traders **can** be fetched on pump-none via the request API `UserLogins` (`GetUserLogins` / `GetGroupLogins`). `UserRequestArray` hits under YoPips `src\` = **0**.

The live ALL-groups / ALL-traders path is Prop C# `NativeMt5BrokerConnector`: first pump `GROUPS|USERS|POSITIONS` (`0x00000181` = 385), fallback `PUMP_MODE_NONE`, then `GroupRequestArray("*")` + `UserRequestArray` / `UserLogins`. Prior measured census, re-summed this slot from `groupNames[].accounts`: Achiever **8 / 6512** (HTTP proxy, 7212.5885 ms) + Starwave **10 / 1948** (direct, 6413.478 ms) = **18 / 8460**.

Copy-to-cTrader **cannot place a live order from the scoring/copy hop today** (`SAFE_BY_ABSENCE` on that path): `CTraderFixSession.BuildLogon` emits `35=A` only (file 135 lines); `CopyTradingService.NewOrderSingleImplemented = false` (const) and `VenueReconciled = false` (const); every persisted `RiskDecisionRecord.AllowFixSend` is hardcoded **false**. Hosted copy only calls `GenerateShadowIntentsAsync`.

**Residual (do not greenwash):**

- DI **binds** `REAL_COPY_EXECUTION_ENABLED` from env (`DependencyInjection.cs` L41). Lab `D:\Prop\.env` L73 is `true`. Hosted FIX **does not** re-pin false. `/api/settings` exposes the bound flag.
- A **separate** demo harness `CTraderFixDemoTestTrade` now builds MsgType `D` via `Build("D", …)` (L139 flatten, L163 open qty `1`, L197 close). Caller is `tools/DemoFixTestTrade` only (not DI, not copy hosted service). Gate: `demo-` host + `demo.` SenderCompID + refuse account `1369850`. That is **not** the copy hop. Do not treat slot 141 “0 builders anywhere” as current.

Risk to capital on the **copy** path is **NONE**. Do not run the demo harness against a live book.

---

## 0. Files remasured (this pass)

| Path | What was measured |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L33–150, L315–327, L956–1017, L1119–1145 | `SetProxy`, Connect remap + no-pump retry, `GetUserLogins`, cache-only `GetAllGroups`, YoPips `DealerSendOrder` (prop-firm path, not cTrader) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` L31–34, L186, L205–206 | Default `pumpMode=0`; `m_pumpMode`; `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` L499–508 | `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` L25–83, L211–223, L663–681 | Pool is **true** `mode=0` first; same proxy packing; `GetAllGroups` also cache-only |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` L201–276 | `IS_MT5_PROXY_ENABLED` → `SetProxy` **before** Connect; pool always `mode=0` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` L113–114 | Probe passes wrapper `0` and comments “No pump mode required” — **false** for first SDK call |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` L167–172 | Binds `IS_MT5_PROXY_ENABLED`, **not** `MT5_PROXY_ENABLED` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.h` L52 | Default `mt5_proxy_enabled = false` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` L76–92, L125–164, L206–212, L254, L410, L672 | Vendor `MTProxyInfo`, `EnPumpModes`, `Connect`, `GroupRequestArray`, `UserLogins`, `UserRequestArray`, `UserGetByGroup` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIConstants.h` L46 | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env` | `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_PORT=49527`, `MT5_PROXY_ENABLED=true`; **`IS_MT5_PROXY_ENABLED` absent** (values that are secrets not copied) |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L33–150, L962–981 | **Line-identical** Connect/SetProxy + cache-only `GetAllGroups` to YoPips (this pass) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | C#: proxy `host:port` / `user:pass`; pump then `PUMP_MODE_NONE`; `GroupRequestArray("*")`; `UserRequestArray` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51 | Catalog = `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135 lines; `BuildLogon` is `35=A` only; one `WriteAsync` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | **New vs slot 141.** Three `Build("D")` writes. Demo-host gate. Not wired to copy. |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | Only caller of the demo sender. Writes `DEMO_FIX_TEST_TRADE.json`. **Not** a hosted service. |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled = false` POCO default; TLS 5211/5212; `TargetCompId=cServer` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only (QUOTE 5211 / TRADE 5212). **Does not** assign `_runtime.RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L36–48 | Fail-closed on missing MT5 passwords; **binds** `RealCopyEnabled` from env (not forced false) |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; SHADOW only |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Calls `GenerateShadowIntentsAsync` only |
| `D:\Prop\apps\fix-worker\Worker.cs` | Even if config flag true, **no** `35=D`; stamps Disconnected |
| `D:\Prop\apps\api\Program.cs` L55, L71–76 | `/api/settings` exposes `runtime.RealCopyEnabled` (bound, not hardcoded false) |
| `D:\Prop\.env` L15–17, L27, L49, L57, L73 | Achiever proxy host/port + Starwave host + FIX host/target + `REAL_COPY_EXECUTION_ENABLED=true` (secret values not copied) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Prior live census writeup |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 8+10 groups; 6512+1948 logins; no password keys |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | This LAN needs Achiever HTTP hop; historical 1012 with proxy off |

Grep remasure (this slot):

| Pattern | Tree | Hits |
|---|---|---:|
| `GroupRequestArray` | YoPips `src\` | **0** (header-only at `MT5APIManager.h` L212) |
| `UserRequestArray` | YoPips `src\` | **0** (header-only at L410) |
| `UserGetByGroup` | YoPips `src\` | **0** (header-only at L672) |
| `GroupRequestArray` | `D:\Prop\src` | **1** (`NativeMt5BrokerConnector.cs` L155 `"*"`) |
| `UserRequestArray` | `D:\Prop\src` | **1** (`NativeMt5BrokerConnector.cs` L223) |
| `35=D` / `(35, "D")` | `D:\Prop\src` `*.cs` | **0** (literal). **Not** the whole story — see `Build("D")`. |
| `Build("D"` | `D:\Prop\src` | **3** (`CTraderFixDemoTestTrade.cs` L139 / L163 / L197) |
| `CTraderFixDemoTestTrade` callers | `D:\Prop` | tool `tools/DemoFixTestTrade/Program.cs` only (plus reports) |
| `SendTrade` / `DealerSend` / `OrderSend` | `D:\Prop\src` | **0** |
| `RealCopyEnabled =` | `D:\Prop\src` | **1** (DI L41 env bind only) |
| `IS_MT5_PROXY_ENABLED` | YoPips `.env` | **0** (key absent) |
| `MT5_PROXY_ENABLED` | YoPips `.env` | **1** (`true`; unread by `app_config`) |

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

SDK `pump_mode=0` is pump-none (no subscriptions). C++ `EnPumpModes` has **no** named `NONE`. Prop C# uses `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs` L101). Same integer.

Pump flags (`MT5APIManager.h` L127–143):

| Flag | Value | In YoPips default remap (`pumpMode==0`)? | In Prop C# first pump? |
|---|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | **Yes** | **Yes** |
| `PUMP_MODE_ORDERS` | `0x00000008` | **Yes** | No |
| `PUMP_MODE_POSITIONS` | `0x00000080` | **Yes** | **Yes** |
| `PUMP_MODE_SYMBOLS` | `0x00000200` | **Yes** | No |
| `PUMP_MODE_GROUPS` | `0x00000100` | **No** | **Yes** |
| `PUMP_MODE_FULL` | `0xffffffff` | No | No |
| none | `0` | Fallback / pool only | Fallback |

Default C++ remap OR = `0x1 | 0x8 | 0x80 | 0x200` = **`0x00000289` (649)**.
C# first pump OR = `0x100 | 0x1 | 0x80` = **`0x00000181` (385)**.

Completeness APIs:

| API | Header | Kind | Used by YoPips C++? | Used by Prop C#? |
|---|---|---|---|---|
| `GroupTotal` / `GroupNext` | L206–207 | **Cache** | **Yes** (`GetAllGroups`) | Fallback only if request array empty |
| `GroupRequestArray(mask)` | L212 | **Network** — ALL groups this manager ACL allows | **No** (0 hits in `src\`) | **Yes** (`"*"`) |
| `UserLogins(group, …)` | L254 | **Network** — ALL logins matching group mask | **Yes** | Yes (third fallback) |
| `UserRequestArray` | L410 | **Network** — full user records | **No** | **Yes** (primary) |
| `UserGetByGroup` | L672 | **Cache** — needs `PUMP_MODE_USERS` | **No** | Fallback only on hard request fail |

`MT_RET_AUTH_MANAGER_IPBLOCK = 1012` is official (`MT5APIConstants.h` L46: “IP address unallowed for manager”). YoPips maps it in `mt5ErrorReason` (L64). C# `Describe` maps 1012 the same way (`NativeMt5BrokerConnector.cs` L447).

---

## 2. Proxy packing — `address=IP:port`, `auth=login:password`

Measured in both `SetProxy` and the Connect-time re-apply. Source comments are literal.

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

Connect re-applies the same packing if `m_proxyConfig.enabled && !m_proxyApplied` (L76–96). Logs **type + host + port only** — not the auth string.

Same packing on `MT5Session::SetProxy` / Connect (`mt5_pool.cpp` L25–72). Prop C# `ApplyProxy` (`NativeMt5BrokerConnector.cs` L115–129) writes `address = $"{host}:{port}"` and `auth = $"{user}:{pass}"` with `type = PROXY_HTTP`. C# **checks** `ProxySet` retcode; C++ `ProxySet` is `void` and is not checked.

Constraints (measured):

- `address[64]` / `auth[64]` are **wchar**. `IP:port` fits (`81.29.145.69:49527` = 20 chars). `login:password` must stay under 63 wide chars or it is silently truncated (`wcsncpy_s` + `_countof-1`).
- Empty proxy login ⇒ `auth` left zeroed (no `:`). Empty password with a non-empty login still writes `login:`.
- `SetProxy` sets `m_proxyApplied=false` so the next `Connect` re-sends `ProxySet` (hot-reload).

### Env keys (do not mix; values not printed)

| Layer | Master toggle | Host / port / user / pass | Type |
|---|---|---|---|
| YoPips C++ (`app_config.cpp` L167–172) | **`IS_MT5_PROXY_ENABLED`** (default **false** via `get_bool`) | `MT5_PROXY_ADDRESS`, `MT5_PROXY_PORT`, `MT5_PROXY_LOGIN`, `MT5_PROXY_PASSWORD` | `MT5_PROXY_TYPE` = `HTTP`/`SOCKS4`/`SOCKS5` |
| YoPips `.env` on disk | `MT5_PROXY_ENABLED=true` | host `81.29.145.69` port `49527` type `HTTP` | **`IS_MT5_PROXY_ENABLED` is absent** — AppConfig therefore leaves `mt5_proxy_enabled=false` |
| Prop C# (`LiveMt5Registration.cs` L30–45) | **`ACHIEVER_PROXY_ENABLED`** (`.env` L15 `true`) | `ACHIEVER_PROXY_HOST=81.29.145.69`, `ACHIEVER_PROXY_PORT=49527`, username/password keys | Hardcoded `PROXY_HTTP` |
| Starwave C# | forced `ProxyEnabled = false` | `MT5_STARWAVEFX_SERVER=84.201.6.142` | direct |

Type map in `main.cpp` L207–209: `SOCKS4`→0, `HTTP`→2, else SOCKS5→1.

Achiever allow-list egress (non-secret, R012/C55): **`81.29.145.69`**. Intended hop: Manager `PROXY_HTTP` to **`81.29.145.69:49527`**. This desktop public egress was measured **`106.219.132.213`** (R012). TCP to Achiever `:443` is OPEN; failure mode without proxy is **1012**, not reachability. Starwave (`84.201.6.142:443`) does **not** need this proxy (`ProxyEnabled=false` hardcoded; `MT5_STARWAVEFX_PROXY*` unread by `CreateConnectors`).

Historical YoPips local starts with `IS_MT5_PROXY_ENABLED` unset logged `MT5 proxy mode: DISABLED (global)` then **1012** on pump **and** no-pump (R012). Fallback does **not** cure an IP block.

`mt5ErrorReason` (`mt5_manager.cpp` L61–68):

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
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // fail: m_lastError = mt5ErrorReason(res); return false
        // ok:   m_connected=true; m_pumpMode=false; no Position/Deal/Order/UserSubscribe
    }
    // pump ok: subscribe four sinks; m_pumpMode=true
}
```

### Binding facts (remasured)

1. **Wrapper `pumpMode=0` ≠ SDK `pump_mode=0`.** `mt5_manager.h` L31–32 defaults `pumpMode = 0`. `main.cpp` L230 calls `Connect(server, login, password)` with that default. `mt5_group_probe.cpp` L113–114 passes `0` on purpose (“No pump mode required”) — that comment is **false** for the first SDK call.
2. First SDK call uses remapped mask **without `PUMP_MODE_GROUPS`**. Even a **successful** pump can leave `GroupTotal()==0` until something else fills the group cache.
3. Fallback SDK call is true pump-none (`0`, 30 s). Sinks are **not** subscribed. `m_pumpMode=false`. Request APIs remain available (`UserRequest`, `UserLogins`, `DealRequest`, `UserAccountRequest`, …).
4. Fallback does **not** retry a different proxy, host, or login. Same `server` / `login` / `password` / already-applied `ProxySet`.
5. Timeout is hard-coded **30000** ms on both attempts.
6. News pump flags (`PUMP_MODE_NEWS` / `PUMP_MODE_FULL`) are recorded but **not** part of the default remap.

### Who is already true pump-none on first try

| Caller | First SDK pump | Fallback | `m_pumpMode` / equivalent |
|---|---|---|---|
| YoPips / Prop `MT5Manager::Connect(0)` / `main.cpp` / `mt5_group_probe` | Remap 649 (`USERS\|ORDERS\|POSITIONS\|SYMBOLS`) | SDK `0` | `false` on fallback |
| YoPips `MT5Session::Connect` (`mt5_pool.cpp` L74–76) | **`0` immediately** | none | request session |
| Prop `NativeMt5BrokerConnector.ConnectCore` | `GROUPS\|USERS\|POSITIONS` = `0x181` (385) | `PUMP_MODE_NONE` | `_pumpEnabled` |

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var endpoint = $"{_opt.Server}:{_opt.Port}";
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                LastError = null;
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
```

C# first pump **includes GROUPS** (the C++ default does not). C# fallback is the same integer `0`. Fetch path does not depend on the cache: `GetGroupsCore` prefers `GroupRequestArray("*")`.

---

## 4. ALL groups / ALL traders — what actually completes

### 4.1 YoPips C++ — groups are cache-only

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    // ...
    uint32_t total = m_manager->GroupTotal();
    // GroupNext(i, grp) → groups.push_back(grp->Group())
    spdlog::info("MT5 GetAllGroups: returned {} groups", groups.size());
    return true;
}
```

`GetGroupDetails` (L984–1013) and pool `MT5Session::GetAllGroups` (L663–681) are the same walk. After `Connect(..., 0)` fallback, `GroupTotal()` can be **0** and the function still returns **true** with `groups: []`. That is **unproven**, not “broker has zero groups.”

`main.cpp` L231–234 logs `GroupTotal()` immediately after Connect. That number is **not** a census.

### 4.2 YoPips C++ — traders are request-complete

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is a one-line wrap (L1015–1017). This is a **network** call. It works in no-pump mode. ALL manager traders = call it once per group name (or a mask the ACL accepts). `UserGetByGroup` is **cache** and will be empty on pump-none — do not use it as the enumerator.

### 4.3 Prop C# — the measured ALL path

`GetGroupsCore` (`NativeMt5BrokerConnector.cs` L144–186):

1. `GroupRequestArray("*")` into `GroupCreateArray`.
2. Only if that list is empty: `GroupTotal` / `GroupNext`.

`ReadAccountsForGroup` (L216–270):

1. `UserRequestArray(gname)`.
2. Else `UserGetByGroup`.
3. If still empty: `UserLogins` then `UserRequestByLogins`.
4. `UserAccountRequestArray` for balances.

`DealIngestionService.SyncCatalogAsync` calls `GetGroupsAsync` then `GetAccountsAsync(null)` — `null` means **every discovered group**, not a plan subset (`DealIngestionService.cs` L45–48). DI refuses `FakeMt5BrokerConnector` when real passwords are present (`DependencyInjection.cs` L36–37).

`NativeMt5BrokerConnector` has **no** `SendTrade` / `DealerSend` / `OrderSend`. Read-only Manager surface.

YoPips C++ **does** implement `DealerSend` / `DealerBalance` (`mt5_manager.cpp` L369+, L1119+). That is the **YoPips prop-firm** challenge execution path, not Prop’s copy-to-cTrader hop. This slot’s “no live orders” constraint is the **cTrader FIX** send path.

### 4.4 Prior measured census (not re-attached this slot)

Source: `LIVE_GROUPS_AND_TRADERS.json` probe `LiveBrokerProbe`, utc **2026-08-18T08:42:16.8519545+00:00**. Passwords never written. Slot 181 re-summed `groupNames[].accounts`.

**Achiever** (`connected=true`, 7212.5885 ms, HTTP proxy): **8 groups / 6512 accounts / 1506 open positions**

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Sum 2+179+4+5+4+6295+0+23 = **6512**.

**StarwaveFX** (`connected=true`, 6413.478 ms, `accounts=1948` at JSON L45648, direct): **10 groups / 1948 accounts / 478 open positions**

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

Sum 11+4+170+1735+22+0+0+4+0+2 = **1948**.

**Total: 18 groups / 8460 manager traders / 1984 open positions.** These are **all groups this manager login can see**. Groups outside the ACL are invisible and must not be invented.

Do **not** treat YoPips `mt5_group_probe` output as this census. The probe uses cache `GetAllGroups` after wrapper `Connect(0)`.

---

## 5. Copy to cTrader — no live orders from the copy hop (no loss)

Measured send-path, not a marketing flag.

| Check | Evidence | Result |
|---|---|---|
| Product FIX session | `CTraderFixSession.BuildLogon` L94–108 (file ends L135) | **`35=A` only**. Tags 49/56/50/57/553/554. One `WriteAsync` (L49). |
| Copy hop `35=D` | `CopyTradingService` + `CTraderFixSession` | **No NewOrderSingle.** Consts block the hop. |
| Demo harness | `CTraderFixDemoTestTrade` L139 / L163 / L197 | **`Build("D")` exists.** Demo-host gate. Qty open = `"1"`. Not called by copy hosted service or DI. |
| Literal `35=D` / `(35, "D")` in `D:\Prop\src` `*.cs` | grep this slot | **0** (narrow). Use `Build("D")` for honesty. |
| MT5 native send | grep `SendTrade`/`DealerSend`/`OrderSend` in `D:\Prop\src` | **0** |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled` L35 | **false** (POCO default only) |
| DI | `DependencyInjection.cs` L39–41 | **Binds env.** Lab `.env` L73 = `true` ⇒ runtime flag **armed**. |
| Hosted FIX | `CTraderFixLogonHostedService.cs` (112 lines) | Logon only. **0** `_runtime.RealCopyEnabled = …` assignments. |
| Copy service | `CopyTradingService.cs` L15–16, L192, L198–204 | `NewOrderSingleImplemented=false`; `VenueReconciled=false`; persist `AllowFixSend=false`; status `SHADOW_ONLY` |
| Hosted copy | `CopyTradingHostedService.cs` L28–30 | `GenerateShadowIntentsAsync` only |
| Settings API | `Program.cs` L71–76 | Exposes `runtime.RealCopyEnabled` (bound). No PUT/PATCH. |
| fix-worker | `Worker.cs` L21–46 | Reads `CTrader:RealCopyExecutionEnabled` (default false). **Still has no 35=D.** Stamps TRADE `Disconnected`. |
| Runtime snapshot | `LiveRuntimeStatus.cs` L42–43 | Armed ⇒ “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” |
| Risk persist | `CopyTradingService` L192 | `AllowFixSend = false` **after** `Evaluate`. Engine *could* compute true if armed+reconciled; persist overwrites. |

Venue is **Pepperstone / cServer execution**, not an LP. Hosted path env: `CTRADER_FIX_HOST=demo-us-eqx-01.p.c-trader.com`; QUOTE TLS **5211**; TRADE TLS **5212**; `TargetCompID=cServer`; tag 553 = integer account id.

Honest gate before any future **copy-hop** live `35=D`: TRADE logon + reconciliation READY + risk approve + explicit `REAL_COPY_EXECUTION_ENABLED=true` **and** a real `GuardedNewOrderSingle` on the copy path that does not exist yet. The env flag is already true; the copy sender is not. Until then copy intents stay SHADOW. **Constraint wins over destination.**

The demo tool is a **different** path. Do not invoke it as part of ALL-groups fetch. Do not treat it as copy-to-cTrader.

---

## 6. Recipe that actually fetches ALL (no live copy orders)

**Achiever (this LAN):**

1. `ProxySet` `enable=1`, `type=PROXY_HTTP (2)`, `address="81.29.145.69:49527"`, `auth="login:password"` (from `ACHIEVER_PROXY_*` / YoPips `MT5_PROXY_*`).
2. `Connect(host:443, managerLogin, password, PUMP_MODE_GROUPS|USERS|POSITIONS, 30000)`.
3. On any fail: retry `Connect(..., PUMP_MODE_NONE / 0, 30000)`.
4. Groups: `GroupRequestArray("*")` — **not** `GroupTotal` unless that array is empty **and** `PUMP_MODE_GROUPS` completed.
5. Traders: `UserRequestArray` per group, else `UserLogins` + `UserRequestByLogins`.

**Starwave:** same Connect/request recipe with **proxy off**.

**YoPips C++ if reused as a collector:** do **not** pass `pumpMode=0` expecting the first call to be request-only. Either use the pool (`mode=0` first) **and** add `GroupRequestArray("*")`, or pass a **nonzero** wrapper mask that includes `PUMP_MODE_GROUPS`. Also set **`IS_MT5_PROXY_ENABLED=true`** — `MT5_PROXY_ENABLED=true` in `.env` is **unread**.

**Never:** send copy-hop `35=D`. The lab flag is already true; do not add a copy sender. Do not run `DemoFixTestTrade` as part of this fetch.

---

## 7. Honesty

- This slot **did not** re-attach a Manager session and **did not** open a FIX TRADE socket.
- Census numbers are the **prior** `LiveBrokerProbe` artifact, independently re-summed from `groupNames[].accounts` (6512 + 1948 = 8460).
- YoPips pump-none fallback is **real** and is the correct second try. It does **not** populate the group cache. It does **not** bypass 1012.
- Prop `mt5-sdk` `mt5_manager.cpp` Connect/SetProxy is **identical** to YoPips. The C# collector is the improved copy (GROUPS bit + request arrays).
- Toggle bug is still live on YoPips: `.env` has `MT5_PROXY_ENABLED`; code reads `IS_MT5_PROXY_ENABLED`. Prop C# uses a different, present key (`ACHIEVER_PROXY_ENABLED=true`).
- Dummy/Fake seed is refused when real MT5 passwords exist.
- **Stale vs this slot:** W500_RESEARCH_1 / 101 claimed DI+hosted force `RealCopyEnabled=false` and `.env` false. Current remasure: DI binds env; hosted does not pin; `.env` L73 is `true`.
- **Stale vs this slot:** W500_RESEARCH_141 tree-wide “`35=D` / `(35, "D")` = 0 ⇒ no builder.” Literal grep still 0. `CTraderFixDemoTestTrade.Build("D")` is now **3** writes. Copy hop remains unimplemented.
- **Not** “EX5 decompiled.” **Not** 95% live copy-trading. Copy-hop live `NewOrderSingle` count remains **0**.
- Source vs slots 1 / 21 / 61 / 101 / 141: Connect/SetProxy **unchanged**. Slot 181 remasured; it does not invent a new Manager recipe.

---

## 8. Slot-181 one-liner

Fallback `Connect(..., 0)` exists; proxy packs `IP:port` + `login:password`; wrapper `0` remaps and omits GROUPS; YoPips `GetAllGroups` is cache-only; ALL traders via `UserLogins`; ALL groups via C# `GroupRequestArray("*")` (measured 18/8460); copy hop has no `35=D`; demo harness `Build("D")` exists but is not the copy path; env `REAL_COPY=true` is armed; risk to capital on copy **NONE**.
