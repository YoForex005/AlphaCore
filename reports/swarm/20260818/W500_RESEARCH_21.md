# W500_RESEARCH_21 — YoPips `mt5_manager.cpp` Connect fallback (pump-none) + proxy `IP:port` / `login:password`

| Field | Value |
|---|---|
| Slot | **21** |
| Agent | W500_RESEARCH_21 |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (source re-read this pass; live census pin `2026-08-18T08:42:16.8519545+00:00`) |
| Assigned | Read YoPips `mt5_manager.cpp` Connect fallback to pump-none and proxy address `IP:port` auth `login:password`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** Proxy user/password, `MT5_PASSWORD`, `MT5_STARWAVEFX_PASSWORD`, `CTRADER_FIX_PASSWORD` named only. |
| Live Manager `Connect` this pass | **Not re-run.** Census below is the existing `LiveBrokerProbe` artifact, not a new attach. |

**Honesty rule:** `Connect(..., 0)` on the C++ **wrapper** is **not** pump-none on the first try. True pump-none is the **fallback** (`mode=0` to the SDK) and the **pool** (`MT5Session::Connect`). `GroupTotal`/`GroupNext` after a no-pump session is **not** a complete group list. Live send being impossible today is **`SAFE_BY_ABSENCE`**, not a unit-tested `35=D` refuse-on-LoggedOn gate.

---

## 0. Verdict

**PASS (recipe + no-loss).** YoPips `MT5Manager::Connect` applies proxy as `address="IP:port"` + `auth="login:password"`, then retries the SDK with `pump_mode=0` when the first (pumped) `Connect` fails. Prop `mt5-sdk` is the same wrapper. The C# collector copies the fallback as `PUMP_MODE_NONE` and enumerates with **request** APIs (`GroupRequestArray("*")` + `UserRequestArray`), which is how ALL manager-visible groups/traders were measured. Copy-to-cTrader **cannot** place a live order: no `35=D` builder, `RealCopyEnabled` forced `false`.

| Claim | Result | Class |
|---|---|---|
| YoPips Connect retries pump-none (`mode=0`) after first fail | **Yes** — `mt5_manager.cpp` 114–135 | `EXISTS_AND_GOOD` |
| Proxy `address` formatted `IP:port`; `auth` formatted `login:password` | **Yes** — `SetProxy` 42–49 and Connect re-apply 82–87 | `EXISTS_AND_GOOD` |
| Passing wrapper `pumpMode=0` is already pump-none | **No** — remapped to `USERS\|ORDERS\|POSITIONS\|SYMBOLS` first | **trap** |
| C++ `GetAllGroups` is no-pump complete | **No** — cache `GroupTotal`/`GroupNext` only; no `GroupRequestArray` | completeness hole |
| C# collector is no-pump complete for groups | **Yes** — `GroupRequestArray("*")` first | `EXISTS_AND_GOOD` |
| Achiever local connect on this LAN needs HTTP proxy | **Yes** — else **1012** on pump **and** no-pump (YoPips log 2026-07-16) | measured fail |
| Starwave local connect needs that proxy | **No** — `ProxyEnabled=false`; no whitelist | recipe |
| ALL Achiever+Starwave groups/traders measured | **Yes (prior probe)** — 8+10 groups, 6512+1948 traders | census pin |
| Copy path can send live `NewOrderSingle` / `35=D` | **No** | **`SAFE_BY_ABSENCE`** |
| Capital at risk from this process | **None** | no-loss operating mode |

One-line:

```text
ProxySet(address="IP:port", auth="login:password") BEFORE Connect;
first SDK Connect uses a pump mask; on fail retry SDK mode=0 (pump-none);
enumerate with GroupRequestArray("*") + UserRequestArray (not GroupNext cache);
do not emit 35=D.
```

---

## 1. Files read (this pass)

| Path | Why |
|---|---|
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Assigned: `SetProxy`, `Connect` remap + no-pump retry |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.h` | `Connect(..., pumpMode = 0)` default |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | True pump-none session (`mode=0` first) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_types.h` | `ProxyConfig` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\main.cpp` | Apply `SetProxy` before Connect; pool comment |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\config\app_config.cpp` | Toggle key `IS_MT5_PROXY_ENABLED` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\runtime-backend-3001.out.log` | Historical **1012** pump + no-pump |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Extracted copy — same Connect/SetProxy body |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | `MTProxyInfo`, `Connect`, `GroupRequestArray`, `UserLogins` |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Probe calls wrapper `Connect(..., 0)` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | C# ProxySet + `PUMP_MODE_NONE` fallback + request enumerators |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever `ACHIEVER_PROXY_*`; Starwave `ProxyEnabled=false` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType **only** `A` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; forces `RealCopyEnabled=false` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog = `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Census writer (no order send) |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Measured 18 / 8460 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Census table |
| `D:\Prop\reports\swarm\20260818\R012_proxy.md` | Egress vs allow-list; toggle bug |
| `D:\Prop\reports\swarm\20260818\A004_yopips_group_probe.md` | Sibling recipe (still binding on remap trap) |
| `D:\Prop\reports\swarm\20260818\E002_no_live_send.md` / `E034_no_35d.md` / `A003_fix_noloss.md` | No-loss pins |

Stale sibling: `A001_native_connector.md` said C# had **no** pump-none retry. **Current** `NativeMt5BrokerConnector.cs` **does** retry `PUMP_MODE_NONE` (lines 101–111). Do not copy A001 for Connect.

---

## 2. YoPips Connect fallback (measured)

Default on the wrapper is `pumpMode = 0` (`mt5_manager.h` 31–32). That **0 is remapped** before the first SDK call.

```71:135:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::Connect(const std::wstring& server, uint64_t login,
                         const std::wstring& password, uint64_t pumpMode) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager) return false;

    // Apply proxy before connect — skipped if already applied (avoids redundant ProxySet calls)
    if (m_proxyConfig.enabled && !m_proxyApplied) {
        MTProxyInfo proxy = {};
        proxy.enable = 1;
        proxy.type   = (int32_t)m_proxyConfig.type;

        std::wstring addrPort = m_proxyConfig.address + L":" + std::to_wstring(m_proxyConfig.port);
        wcsncpy_s(proxy.address, addrPort.c_str(), _countof(proxy.address) - 1);

        if (!m_proxyConfig.login.empty()) {
            std::wstring auth = m_proxyConfig.login + L":" + m_proxyConfig.password;
            wcsncpy_s(proxy.auth, auth.c_str(), _countof(proxy.auth) - 1);
        }

        m_manager->ProxySet(proxy);
        m_proxyApplied = true;
        spdlog::info("MT5 proxy applied before connect: type={} address={}:{}",
                     m_proxyConfig.type,
                     StringUtils::toUtf8(m_proxyConfig.address),
                     m_proxyConfig.port);
    }
    // ... Subscribe(this); remap mode==0 to USERS|ORDERS|POSITIONS|SYMBOLS ...
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        if (res != MT_RET_OK) {
            spdlog::error("MT5 Connect (no-pump fallback) also failed: {}", res);
            // ...
            return false;
        }
        m_connected = true;
        m_pumpMode = false;
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
        return true;
    }
```

### 2.1 Three different “mode = 0” meanings

| Caller | First SDK `pump_mode` | On fail | `m_pumpMode` |
|---|---|---|---|
| `MT5Manager::Connect(0)` / default / YoPips `main.cpp` / `mt5_group_probe` | Remap to `USERS(1) \| ORDERS(8) \| POSITIONS(128) \| SYMBOLS(512)` = **649**. **Omits `PUMP_MODE_GROUPS` (256).** | Retry SDK **`0`** (true pump-none) | `false` on fallback |
| `MT5Manager::Connect(nonzero mask)` | That mask | Same retry SDK `0` | `false` on fallback |
| `MT5Session::Connect` (request pool) | SDK **`0` immediately** — no remap | No second try | N/A (request session) |

Vendor C++ header has **no** `PUMP_MODE_NONE` name. C# `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE = 0` (R010). Same integer.

Pump-success path (wrapper): `PositionSubscribe` / `DealSubscribe` / `OrderSubscribe` / `UserSubscribe`; `m_pumpMode=true`. Fallback path: **no** those four sinks. Comment at 118–121: `GetDeals` / `DealRequest` work without the pump.

Timeout: **30000 ms per attempt** → worst case ~60 s inside one `Connect`.

`password_cert` is always `L""`.

### 2.2 True pump-none: pool (not the wrapper first hop)

```74:76:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

YoPips `main.cpp` 240–242: pool is initialized **regardless of pump status** so request sessions can succeed when the full-pump connection fails. That is the operator meaning of “fallback to pump-none.”

### 2.3 Prop C++ copy

`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` lines 33–135 (`SetProxy`, `mt5ErrorReason`, `Connect` remap + retry) match the YoPips body read this pass. Treat them as one wrapper.

---

## 3. Proxy address `IP:port` and auth `login:password`

Vendor layout (`MT5APIManager.h` 77–92):

| Field | Type | Meaning |
|---|---|---|
| `enable` | `int32_t` | 1 = on |
| `type` | `int32_t` | `PROXY_SOCKS4=0`, `PROXY_SOCKS5=1`, `PROXY_HTTP=2` |
| `address[64]` | `wchar_t` | **`IP:port` of proxy server** (SDK comment) |
| `auth[64]` | `wchar_t` | **`login:password`** (SDK comment) |

YoPips `SetProxy` (33–57) **and** the Connect re-apply (77–95) both format:

```text
proxy.address = address + L":" + port
proxy.auth    = login + L":" + password     // only if login non-empty
m_manager->ProxySet(proxy)
```

Logs print **type + address + port only**. Auth is never logged. That matches architecture “never log proxy credentials.”

Buffer risk (not printed as a secret): both fields are **64 wchar**. `wcsncpy_s(..., _countof-1)` truncates. R012 lengths: YoPips proxy login 15 / password 18 → `auth` 34 wchar; Prop Achiever user 15 / password 15 → `auth` 31 wchar. Both fit. A longer password would silently truncate and look like “1012/7” not “auth too long.”

### 3.1 Who turns the proxy on

C++ `AppConfig` (`app_config.cpp` 130–135):

| Env key | Bound field | Default |
|---|---|---|
| `MT5_PROXY_TYPE` | `mt5_proxy_type` | empty |
| `MT5_PROXY_ADDRESS` | `mt5_proxy_address` | empty |
| `MT5_PROXY_PORT` | `mt5_proxy_port` | 0 |
| `MT5_PROXY_LOGIN` | `mt5_proxy_login` | empty |
| `MT5_PROXY_PASSWORD` | `mt5_proxy_password` | empty |
| **`IS_MT5_PROXY_ENABLED`** | `mt5_proxy_enabled` | **false** |

**`MT5_PROXY_ENABLED` is not read.** YoPips production `.env` has `MT5_PROXY_ENABLED=true` and **no** `IS_MT5_PROXY_ENABLED` (R012). Result: `main.cpp` logs `MT5 proxy mode: DISABLED (global)` and connects **direct**.

YoPips `main.cpp` 201–225: `SetProxy` only if `config.mt5_proxy_enabled` and type/address/port complete; HTTP maps to `MTProxyInfo::PROXY_HTTP`. Incomplete stanza → warn and **direct**. Same keys applied to the pool (`setProxyConfig`) at 249–268.

### 3.2 Achiever vs Starwave (non-secret identifiers only)

| Item | Achiever | StarwaveFX |
|---|---|---|
| Manager host:port | `57.128.141.65:443` | `84.201.6.142:443` |
| Manager login | `2027` | `9904` |
| Proxy | **Required on this LAN** | **Off** |
| C++ keys | `IS_MT5_PROXY_ENABLED=true` + `MT5_PROXY_TYPE=HTTP` + address/port/login/password | leave toggle false |
| C# keys | `ACHIEVER_PROXY_ENABLED=true` + `ACHIEVER_PROXY_HOST/PORT/USERNAME/PASSWORD` | `ProxyEnabled = false` hardcoded in `LiveMt5Registration` |
| Proxy host:port (public) | `81.29.145.69:49527` HTTP CONNECT | unused |
| Allow-list identity | `81.29.145.69` | none documented |
| This desktop egress (R012) | `106.219.132.213` ≠ allow-list | n/a |

C# apply (`NativeMt5BrokerConnector.ApplyProxy` 115–129): `type = PROXY_HTTP`, `address = $"{host}:{port}"`, `auth = $"{user}:{password}"` (empty auth if no user). Same SDK struct as C++.

---

## 4. Historical proof the fallback exists — and that proxy still matters

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\runtime-backend-3001.out.log` 2026-07-16T15:49:48 local:

| Line (file) | Message |
|---|---|
| 8 | `MT5 proxy mode: DISABLED (global)` |
| 16 | `MT5 Connect failed: 1012 — retrying without pump mode` |
| 18 | `MT5 Connect (no-pump fallback) also failed: 1012` |
| 20–35 | Pool sessions: **1012** (and two **7** timeouts); `0/8` ready |

Wrapper map (`mt5ErrorReason`): **1012** = `MT_RET_AUTH_MANAGER_IPBLOCK`. Pump-none does **not** bypass the allow-list. TCP to `:443` can be OPEN (R012, 309 ms) and still 1012.

Implication for slot 21: **do not treat pump-none as a substitute for `ProxySet`.** Fallback only drops subscriptions. The source IP Achiever sees is still this NAT **unless** HTTP CONNECT presents `81.29.145.69`.

---

## 5. How ALL groups and ALL manager traders are fetched

### 5.1 What the C++ wrapper actually lists

`GetAllGroups` (YoPips / Prop `mt5_manager.cpp` 962–982):

1. `GroupTotal()` — **pump cache**.
2. `GroupNext(i)` for `i in [0, total)`.
3. Returns **true** even when `total == 0`.

`GetUserLogins` (315–327): SDK `UserLogins(group, raw, total)` — **network request**, works in no-pump. Alias: `GetGroupLogins`.

Vendor complete no-pump group enumerator (`MT5APIManager.h` 212):

```text
GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)
```

**Unused** by `MT5Manager`. After a successful **pump-none** fallback, `GetAllGroups` can print `[]` while the manager ACL still has groups. Probe comment “No pump mode required” is **intent**; the wrapper first try is still a pump mask **without** `PUMP_MODE_GROUPS`.

Do **not** use C++ `GetAllGroups` alone as the ALL-groups proof.

### 5.2 What the C# collector uses (this is the ALL path)

`NativeMt5BrokerConnector.ConnectCore` (88–111):

1. First: `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS` (includes groups; omits orders/symbols vs C++ default).
2. On fail: `PUMP_MODE_NONE` (0). Stay connected; `_pumpEnabled=false`.
3. `LastError` includes `proxy={true|false}` but **not** the password.

`GetGroupsCore` (144–186):

1. **`GroupRequestArray("*")`** — request, wildcard, manager-ACL complete.
2. If that list is empty: fall back to `GroupTotal`/`GroupNext`.

`GetAccountsCore(null)` (189–213): every group name from step 1, then `ReadAccountsForGroup`:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. accounts: `UserAccountRequestArray` else `UserAccountGetByGroup`

`DealIngestionService.SyncCatalogAsync` (44–48): `GetGroupsAsync` + `GetAccountsAsync(null)` — no group-name allow-list, no `Take(200)`.

`LiveBrokerProbe` (19–29): same two calls + `GetGroupPositionsAsync("*")`. Writes `LIVE_GROUPS_AND_TRADERS.json`. No `DealerSend` / `OrderAdd` / FIX.

### 5.3 Measured census (do not re-state as a new attach)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`.

| Broker | connected | elapsedMs | groups | accounts | openPositions |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 7212.5885 | 8 | 6512 | 1506 |
| STARWAVEFX | true | 6413.478 | 10 | 1948 | 478 |
| **Total** | | | **18** | **8460** | **1984** |

Achiever names (accounts sum **6512**):  
`contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave names (accounts sum **1948**):  
`Starwave\cent\FX1\grp1` 11, `grp2` 4; `Starwave\demo\FX2\grp1` 170, `grp2` 1735; `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

These are **all groups those two manager logins can see**. Groups outside the manager ACL are invisible by design (LIVE_MANAGER_FETCH_MEASURED.md). Zero-account groups (`demo\yo-instant`, three Starwave real grps) are still listed — evidence the walk is group-first, not “skip empty.”

Trader **logins** live in the JSON; they are **not** copied here (PII / account identifiers). Counts only.

---

## 6. Copy to cTrader — must not send live orders (no loss)

Goal pairing: fetch the full Manager catalog **and** keep capital at zero. Those are compatible **only** while `35=D` does not exist.

| Gate | Measured |
|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | Forced **`false`** in `DependencyInjection.cs` 40–41 (“Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”) |
| `CTraderFixLogonHostedService` | TLS logon QUOTE `:5211` + TRADE `:5212`; then `_runtime.RealCopyEnabled = false` again (line 68). Log: “NewOrderSingle still disabled.” |
| `CTraderFixSession.BuildLogon` | Tag **35=`A` only**. Username tag 553 = integer account id (not SenderCompID). |
| Literal `35=D` / `(35,"D")` / `MsgType="D"` in product C# | **0** (E034; re-grep of `Fix.CTrader` this pass: only `35={msgType}` in a reject string) |
| `NewOrderSingle` in `NativeMt5BrokerConnector` | **0** — no `DealerSend` / `OrderAdd` / `TradeRequest` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| API `/api/settings` | hardcoded `REAL_COPY_EXECUTION_ENABLED=false` |
| Ingest after catalog | `DealRequest` / `DealRequestByGroup` + reconstruct/score. **Read** path. |

Honest gate (A003, still binding):

```text
ALLOW:  Manager Connect + GroupRequestArray/UserRequestArray + DealRequest
        FIX 35=A logon / diagnostics
FORBID: 35=D NewOrderSingle, 35=F/G, REAL_COPY_EXECUTION_ENABLED=true
```

Flipping the flag would **still** not send: there is no builder. That is **`SAFE_BY_ABSENCE`**, not a coded choke. Do not enable the flag. Do not add a sender in this slot.

---

## 7. Operator recipe (Achiever + Starwave, this workstation)

```
ACHIEVER (this LAN):
  ProxySet(HTTP, address="81.29.145.69:49527", auth="login:password")
  Connect("57.128.141.65:443", login=2027, password, pump)
  if fail → Connect(..., pump_mode=0)
  groups  = GroupRequestArray("*")     // not GroupNext cache
  traders = UserRequestArray(each group) [or UserLogins + UserRequestByLogins]

STARWAVE:
  do NOT SetProxy
  Connect("84.201.6.142:443", login=9904, password, pump)
  if fail → Connect(..., pump_mode=0)
  same request enumerators

COPY:
  persist catalog / deals / scores
  FIX logon only
  RealCopyEnabled stays false
```

C++ probe/backend: set **`IS_MT5_PROXY_ENABLED=true`**, not `MT5_PROXY_ENABLED`.  
C# worker: **`ACHIEVER_PROXY_ENABLED=true`** + host/port/user/pass (already wired).  
Never log `auth`. Never treat wrapper `Connect(0)` as already pump-none.

---

## 8. What this slot did **not** do

- Did not print or copy any password or proxy username.
- Did not run a new Manager `Connect` or HTTP CONNECT.
- Did not edit YoPips or Prop product source.
- Did not enable `REAL_COPY_EXECUTION_ENABLED`.
- Did not claim EX5 decompile / 95% copy-trading live.
- Did not claim C++ `GetAllGroups` is ALL-groups after no-pump (it is not).

---

## 9. Done criteria

- [x] YoPips `Connect` remap + pump-none retry quoted with line evidence.
- [x] Proxy `IP:port` + `login:password` formatting quoted; credentials not printed.
- [x] Achiever needs HTTP proxy; Starwave does not; 1012-on-both-attempts logged.
- [x] ALL-groups / ALL-traders path identified (`GroupRequestArray` + `UserRequestArray`); live census 18 / 8460 cited from existing JSON.
- [x] Copy-to-cTrader live send **off** (`35=A` only; `RealCopyEnabled=false`).
- [x] Product source not modified.

**Slot 21 verdict: PASS.** Recipe is in YoPips `mt5_manager.cpp`; Prop C++ matches; C# collector implements the safe request-complete variant; capital risk from live copy is **none** until a `35=D` sender is built (forbidden now).
