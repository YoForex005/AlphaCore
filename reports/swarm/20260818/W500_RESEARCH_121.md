# W500_RESEARCH_121 — YoPips `mt5_manager.cpp` Connect pump-none fallback + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **121** |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_121 |
| Topic | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy address `IP:port` auth `login:password` |
| Goal | Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss) |
| Product source modified | **No** |
| Secret values printed | **None** (manager / proxy / FIX passwords classified only; never copied) |
| Live Connect this pass | **Not re-run.** Census is the prior measured fetch `LIVE_GROUPS_AND_TRADERS.json` utc `2026-08-18T08:42:16.8519545+00:00` (`LiveBrokerProbe`). This slot is a source/recipe remasure. |

## Verdict

**CONFIRMED_WITH_GROUPS_CACHE_GAP**

YoPips `MT5Manager::Connect` **does** retry the vendor SDK as `Connect(..., pump_mode=0, 30000)` after the first (pumped) connect fails. That SDK `0` is true pump-none. Proxy packing **is** the vendor contract: `MTProxyInfo.address = "IP:port"` and `MTProxyInfo.auth = "login:password"` (64-wchar buffers). Prop `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L33–150 is the **same** block (SetProxy + Connect remap + no-pump retry).

It is **not** enough, by itself, to claim “ALL groups” from the YoPips C++ wrapper after that fallback:

1. Wrapper `pumpMode=0` is **not** pump-none on the **first** SDK call. `mt5_manager.h` defaults `pumpMode=0`; the body remaps `0` → `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS` (`0x00000289` = 649) and **omits `PUMP_MODE_GROUPS` (`0x00000100`)**.
2. True pump-none is the **retry** (`mode=0`) and the **pool** (`MT5Session::Connect` first call is already `0`).
3. `GetAllGroups` / `GetGroupDetails` / `LogAvailableGroups` walk the **local group cache** (`GroupTotal` + `GroupNext`). `GroupRequestArray` has **0 hits** under YoPips `src\`. After a no-pump fallback that cache can be empty while the manager ACL still has groups. `GetAllGroups` still returns `true` with `size()==0`.
4. ALL traders **can** be fetched on pump-none via the request API `UserLogins` (`GetUserLogins` / `GetGroupLogins`). `UserRequestArray` is unused in YoPips C++.

The live ALL-groups / ALL-traders path is Prop C# `NativeMt5BrokerConnector`: first pump `GROUPS|USERS|POSITIONS`, fallback `PUMP_MODE_NONE`, then `GroupRequestArray("*")` + `UserRequestArray` / `UserLogins`. Prior measured census: Achiever **8 / 6512** (HTTP proxy) + Starwave **10 / 1948** (direct) = **18 / 8460**.

cTrader copy **cannot place a live order today**: `CTraderFixSession.BuildLogon` emits `35=A` only; **0** `35=D` builders under `D:\Prop\src`; `RealCopyExecutionEnabled` default **false**; DI and the hosted logon service both force `RealCopyEnabled = false`; `CopyTradingService.NewOrderSingleImplemented = false` (const) and records `AllowFixSend = false`. Risk to capital on the copy path is **NONE** (`SAFE_BY_ABSENCE`). Do not set `REAL_COPY_EXECUTION_ENABLED=true`.

---

## 0. Files read (this pass)

| Path | What was measured |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L33–150, L315–327, L956–1016 | `SetProxy`, Connect remap + no-pump retry, `GetUserLogins`, cache-only `GetAllGroups` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` L31–34, L186, L205–206 | Default `pumpMode=0`; `m_pumpMode`; `m_proxyApplied` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` L499–508 | `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` L25–83, L211–223, L663–681 | Pool is **true** `mode=0` first; same proxy packing; `GetAllGroups` also cache-only |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` L201–276 | `IS_MT5_PROXY_ENABLED` → `SetProxy` **before** Connect; pool always `mode=0` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\tests\mt5_group_probe.cpp` L113–114 | Probe passes wrapper `0` and comments “No pump mode required” — **false** for first SDK call |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` L167–172 | Binds `IS_MT5_PROXY_ENABLED`, **not** `MT5_PROXY_ENABLED` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\.env` (keys only) | `MT5_PROXY_TYPE=HTTP`, `MT5_PROXY_ADDRESS=81.29.145.69`, `MT5_PROXY_ENABLED=true`; **`IS_MT5_PROXY_ENABLED` absent** |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` L76–92, L125–164, L206–212, L254, L410 | Vendor `MTProxyInfo`, `EnPumpModes`, `Connect`, `GroupRequestArray`, `UserLogins`, `UserRequestArray` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIConstants.h` L46 | `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` L42 | C# sample names `PUMP_MODE_NONE = 0x00000000` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L33–150, L315–327, L956–1016 | **Same** Connect/SetProxy/`GetAllGroups` as YoPips |
| `D:\Prop\mt5-sdk\.env.example` L66 | Documents the real toggle: `IS_MT5_PROXY_ENABLED` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L60–130, L144–233, L442–455 | C#: proxy `host:port` / `user:pass`; pump then `PUMP_MODE_NONE`; `GroupRequestArray("*")`; `UserRequestArray` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L23–47 | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` L48–62 | Catalog-only: `Connect` + `SyncCatalogAsync` (groups/accounts). No FIX send. |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L38–51 | `GetGroupsAsync` + `GetAccountsAsync(null)` = all groups / all logins |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` L89–109 | `BuildLogon` is `35=A` only |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L32–35 | `RealCopyExecutionEnabled = false`; TLS 5211/5212; `TargetCompId=cServer` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L48–68 | Logon only; `_runtime.RealCopyEnabled = false` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L38–41 | Forces `RealCopyEnabled = false` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L15–16, L193–204 | `NewOrderSingleImplemented=false`; `AllowFixSend=false`; else `SHADOW_ONLY` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | In-memory simulate; no socket |
| `D:\Prop\apps\fix-worker\Worker.cs` L21–46 | Even if config flag true, **no** `35=D`; stamps Disconnected |
| `D:\Prop\apps\api\Program.cs` L70–76 | `/api/settings` exposes `runtime.RealCopyEnabled` (forced false) + `FEATURE_COPY_TRADING_ENABLED=false` |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Wrote the census JSON (logins/balances only; “Passwords never written”) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Prior live census narrative |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 8+10 groups; 6512+1948 logins; no password keys |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | This LAN needs Achiever HTTP hop; historical 1012 with proxy off |

Grep this pass (workspace `D:\Prop\src` + YoPips `src\`):

| Pattern | Hits |
|---|---|
| `GroupRequestArray` under YoPips `src\` | **0** |
| `UserRequestArray` under YoPips `src\` | **0** |
| `GroupRequestArray` under Prop `mt5-sdk\src` | **0** |
| `SendTrade` / `DealerSend` / `OrderSend` under `D:\Prop\src\Mt5` | **0** |
| `35=D` / `(35, "D")` under `D:\Prop\src` | **0** |
| `35=A` builder | **1** (`CTraderFixSession.BuildLogon`) |

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

SDK `pump_mode=0` is pump-none (no subscriptions). C++ `EnPumpModes` has **no** named `NONE`. C# WebAPI sample names it `PUMP_MODE_NONE = 0x00000000` (`MetaQuotes.MT5WebAPI.cs` L42). Prop C# uses `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs` L101). Same integer.

Pump flags (`MT5APIManager.h` L127–143):

| Flag | Value | In YoPips default remap (`pumpMode==0`)? | In Prop C# first Connect? |
|---|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | **Yes** | **Yes** |
| `PUMP_MODE_ORDERS` | `0x00000008` | **Yes** | No |
| `PUMP_MODE_POSITIONS` | `0x00000080` | **Yes** | **Yes** |
| `PUMP_MODE_SYMBOLS` | `0x00000200` | **Yes** | No |
| `PUMP_MODE_GROUPS` | `0x00000100` | **No** | **Yes** |
| `PUMP_MODE_FULL` | `0xffffffff` | No | No |
| none | `0` | Fallback / pool only | Fallback (`PUMP_MODE_NONE`) |

Default YoPips remap OR = `0x1 | 0x8 | 0x80 | 0x200` = **`0x00000289` (649)**.

C# first pump OR = `0x100 | 0x1 | 0x80` = **`0x00000181` (385)** — includes GROUPS, omits ORDERS/SYMBOLS.

Completeness APIs:

| API | Header | Kind | Used by YoPips C++? | Used by Prop C#? |
|---|---|---|---|---|
| `GroupTotal` / `GroupNext` | L206–207 | **Cache** | **Yes** (`GetAllGroups`) | Fallback only if request array empty |
| `GroupRequestArray(mask)` | L212 | **Network** — ALL groups this manager ACL allows | **No** (0 hits in `src\`) | **Yes** (`"*"`) |
| `UserLogins(group, …)` | L254 | **Network** — ALL logins matching group mask | **Yes** | Yes (third fallback) |
| `UserRequestArray` | L410 | **Network** — full user records | **No** | **Yes** (primary) |

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
| YoPips `.env` on disk | `MT5_PROXY_ENABLED=true` | same family; host `81.29.145.69` type `HTTP` | **`IS_MT5_PROXY_ENABLED` is absent** — AppConfig therefore leaves `mt5_proxy_enabled=false` |
| Prop `mt5-sdk\.env.example` | documents **`IS_MT5_PROXY_ENABLED`** | empty defaults | off |
| Prop C# (`LiveMt5Registration.cs` L30–45) | **`ACHIEVER_PROXY_ENABLED`** | `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD` | Hardcoded `PROXY_HTTP` |
| Starwave C# | forced `ProxyEnabled = false` | n/a | direct |

Type map in `main.cpp` L207–209: `SOCKS4`→0, `HTTP`→2, else SOCKS5→1.

Achiever allow-list egress (non-secret, R012/C55): **`81.29.145.69`**. Intended hop: Manager `PROXY_HTTP` to **`81.29.145.69:49527`**. This desktop public egress was measured **`106.219.132.213`**. TCP to Achiever `:443` is OPEN; failure mode without proxy is **1012**, not reachability. Starwave (`MT5_STARWAVEFX_*`, `ProxyEnabled=false`) does **not** need this proxy.

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

```101:135:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    // Connect with pump mode for real-time events
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

        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump; this lets journal
        // sync and other request-only operations function even when the pump
        // connection is unavailable (IP not yet whitelisted for pump, etc.)
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
```

On fallback success: `m_connected=true`, `m_pumpMode=false`, **no** `Position/Deal/Order/UserSubscribe`. Comment at L118–121 is honest: request APIs (`DealRequest`, `UserLogins`, `UserAccountRequest`) remain available.

On first-call success (L138–149): sink subscribe + `m_pumpMode=true`.

Callers:

| Caller | Wrapper `pumpMode` | First SDK `pump_mode` | Fallback |
|---|---|---|---|
| `main.cpp` L230 `Connect(server, login, password)` | default **0** | remapped **649** (no GROUPS) | SDK **0** |
| `mt5_group_probe.cpp` L114 `Connect(..., 0)` | explicit **0** (comment claims “no pump”) | remapped **649** | SDK **0** |
| `MT5Session::Connect` (`mt5_pool.cpp` L75–76) | n/a | **0** first | none (already pump-none) |
| Prop C# `ConnectCore` L89–101 | n/a | `GROUPS\|USERS\|POSITIONS` (385) | `PUMP_MODE_NONE` |

Implication for ALL-groups: if the probe/main path lands on the fallback, YoPips `GetAllGroups` can return **0 names** even though `UserLogins` per known mask would still work. The C# product path does **not** have that gap because it uses `GroupRequestArray("*")`.

---

## 4. ALL groups + ALL manager traders

### YoPips C++ (cache groups / request traders)

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    // ...
    uint32_t total = m_manager->GroupTotal();
    // GroupNext(i) → groups.push_back(grp->Group())
    spdlog::info("MT5 GetAllGroups: returned {} groups", groups.size());
    return true;
}
```

`GetGroupDetails` (L984–1012) is the same walk. `GetGroupLogins` is an alias of `GetUserLogins` (L1015–1016).

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    uint64_t* raw_logins = nullptr;
    uint32_t total = 0;
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`UserLogins` is a **network** request (`MT5APIManager.h` L254). It works on pump-none. Completeness is “all logins matching the group mask this manager may see,” not “every login on the server.”

### Prop C# (request groups + request traders) — this is the measured ALL path

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            // ...
            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
```

`GetGroupsCore` (L144–186): `GroupRequestArray("*")` first; only if that list is empty, walk `GroupTotal`/`GroupNext`.

`GetAccountsCore(null)` (L189–213): for **every** group name, `ReadAccountsForGroup`:

1. `UserRequestArray(gname)` (network)
2. else `UserGetByGroup` (pump cache)
3. if still empty: `UserLogins` + `UserRequestByLogins`

`DealIngestionService.SyncCatalogAsync` calls `GetAccountsAsync(null, ct)` — no `Take()`, no group filter. `LiveIngestHostedService` only catalogs (groups/accounts); it does not send FIX.

### Prior measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` utc `2026-08-18T08:42:16.8519545+00:00`  
Note in JSON: `"Passwords never written. Groups and manager logins only."`

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | 1984 | |

Achiever groups (account counts from the JSON):

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

Starwave groups:

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

These are **all groups this manager login can see**. If the server has more groups, they are outside this manager's permission set. Zero-account groups (demo\yo-instant, three Starwave real grps) are still **present** in the group list — evidence the walk is group-complete, not “only groups that have traders.”

Honesty: this slot did **not** attach a new Manager session. Numbers are the 08:42Z probe, not a 121-fresh live recount.

---

## 5. Copy to cTrader must not send live orders (no loss)

Independent gates (all must fail closed; they do):

| Gate | Evidence | State |
|---|---|---|
| FIX NewOrderSingle builder | `CTraderFixSession` only writes `(35, "A")` L96. Grep `35=D` / `(35, "D")` under `D:\Prop\src` = **0** | **absent** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** L35 | off |
| DI runtime pin | `DependencyInjection.cs` L40–41: `RealCopyEnabled = false` + comment “Live NewOrderSingle is not implemented” | forced off |
| FIX hosted service | `CTraderFixLogonHostedService.cs` L68: `_runtime.RealCopyEnabled = false` after QUOTE/TRADE logon | forced off |
| API surface | `Program.cs` L75–76: `REAL_COPY_EXECUTION_ENABLED = runtime.RealCopyEnabled`; `FEATURE_COPY_TRADING_ENABLED = false` | off / unused |
| FIX worker | `Worker.cs` L21–46: reads `CTrader:RealCopyExecutionEnabled` for **log only**; stamps both sessions Disconnected; “worker still refuses NewOrderSingle” | no send |
| Copy service | `CopyTradingService`: `NewOrderSingleImplemented = false`, `VenueReconciled = false` (consts); every `RiskDecisionRecord.AllowFixSend = false`; else status `SHADOW_ONLY`; hypothetical live branch is `LIVE_SEND_BLOCKED_UNIMPLEMENTED` | no send |
| Shadow engine | `ShadowCopyEngine.SimulateEntry/Exit` — in-memory fill, no socket | no send |
| MT5 dealer from Prop C# | `SendTrade` / `DealerSend` / `OrderSend` under `src\Mt5` = **0** | no send |
| Ingest | `LiveIngestHostedService` + `DealIngestionService.SyncCatalogAsync` are Manager **read** (groups/accounts/deals/positions) | no send |

`CTraderFixSession.TryLogonAsync` opens a TLS socket, writes **one** logon, reads the reply, **disposes** the socket. Even a successful TRADE `35=A` does not leave a session that could later emit `35=D`.

`LiveRuntimeStatus.Snapshot` copyNote when flag is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Even if someone does, there is still no `35=D` encoder — the worker will only log a warning. Capital risk on the copy path remains **NONE** until a NewOrderSingle builder exists **and** the pins are lifted.

---

## 6. Slot-121 recipe (what to use; what not to)

For **ALL Achiever + Starwave groups and traders** from this LAN:

1. Achiever: `ProxySet` type `PROXY_HTTP`, `address="81.29.145.69:49527"`, `auth="login:password"` **before** `Connect`. Toggle key on C# is `ACHIEVER_PROXY_ENABLED`. Toggle key on YoPips C++ is **`IS_MT5_PROXY_ENABLED`** (not `MT5_PROXY_ENABLED`).
2. Starwave: **direct** (`ProxyEnabled=false`). Do not reuse the Achiever hop.
3. Connect: pump (`GROUPS` included) then fallback `pump_mode=0`. Wrapper `Connect(..., 0)` on YoPips is **not** pump-none on the first call.
4. Groups: `GroupRequestArray("*")` — **not** YoPips `GetAllGroups` after a no-pump fallback.
5. Traders: `UserRequestArray` per group, then `UserLogins` if empty.
6. Copy: leave `RealCopyEnabled=false`. Catalog/ingest only. No `35=D`.

---

## 7. Honesty

- This is **not** “EX5 decompiled” and not 95% copy-trading live.
- It **is** a remasure of the YoPips Connect/proxy recipe plus the prior Manager census of every group and every login the two manager accounts can see.
- YoPips C++ `GetAllGroups` after pump-none is a **known completeness gap**. The live 18/8460 number comes from Prop C# request APIs, not from YoPips `GroupNext`.
- No secrets printed. No product source edited. No live order sent. No new Manager attach this slot.
