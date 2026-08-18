# A001 — Native C# connector: ALL groups/traders vs pump cache

**Agent:** A001  
**Date:** 2026-08-18  
**Product source modified:** none (read-only)  
**Output:** `D:\Prop\reports\swarm\20260818\A001_native_connector.md`  
**Secrets:** none printed. Proxy `auth` described as `user:pass` shape only.

**Sources (quoted, not modified):**

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`IMTManagerAPI` 5570)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (pump-none fallback)
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (same fallback + `UserLogins` + cache→request)
- Prior notes: `A39_mt5_group_discovery.md`, `A84_group_total_impl.md`, `R012_proxy.md`, `R020_manager_apis.md`

---

## 0. Verdict (true measured state)

| Question | Answer |
|---|---|
| Does `NativeMt5BrokerConnector` fetch **ALL** groups from the trade server via request API? | **No.** Groups come from the **local pump cache**: `GroupTotal` + `GroupNext`. |
| Does it fetch **ALL** traders via request API? | **No.** Traders come from the **local pump cache**: `UserGetByGroup` + `UserAccountGetByGroup`. |
| Does a successful `PUMP_MODE_GROUPS \| PUMP_MODE_USERS` connect *usually* fill that cache with every group/user the manager ACL allows? | **Yes, if pump completes.** The walk then equals “all manager-visible”. That is **not** the same as a guaranteed server pull. |
| Pump-none fallback (YoPips `Connect(..., 0)` on pump fail)? | **Missing.** First `Connect` with pump flags is fatal. |
| Server request APIs that *would* work with `pump=0`? | Present on SDK; **unused** in C# except deals/positions. |
| Achiever HTTP proxy `address=IP:port` `auth=user:pass`? | **Format is correct** when `ProxyEnabled` and host are set. |

Honest one-liner: **groups and traders are pump-cache subsets; deals (and per-login positions) are server requests; no no-pump retry.**

---

## 1. Connect path — pump required, no fallback

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

`ConnectAsync` → `ConnectCore` (lines 36, 55–110).

Pump mask (lines 96–99):

```csharp
var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
           | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
           | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
```

SDK `EnPumpModes` (`MT5APIManager.h` 125–144):

| Flag | Value | What it fills |
|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | User records → `UserTotal` / `UserGet` / `UserGetByGroup` |
| `PUMP_MODE_POSITIONS` | `0x00000080` | Open positions cache (`PositionGet*`) |
| `PUMP_MODE_GROUPS` | `0x00000100` | Group **configs** → `GroupTotal` / `GroupNext` / `GroupGet` |

On non-`MT_RET_OK` (lines 100–105): set `LastError`, `_connected = false`, **throw**. There is **no** second `Connect(..., pumpMode: 0, ...)`.

### YoPips / Prop C++ pump-none fallback (absent here)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` 114–135 (same text in `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`):

1. `Connect(..., mode, 30000)` with pump flags.
2. On fail: log `retrying without pump mode`.
3. `Connect(..., 0, 30000)` — **no subscriptions**.
4. If that succeeds: `m_pumpMode = false`; request APIs (`DealRequest`, `UserLogins`, `UserRequestArray`, `GroupRequestArray`) still work.
5. Comment: pump can fail when **IP not yet whitelisted for pump** while request-only still works.

C# gap: Achiever 1012 / pump-only ACL / timeout → **hard fail**. No request-only session.

Also missing vs YoPips:

- `Subscribe` / `PositionSubscribe` / `DealSubscribe` / `OrderSubscribe` / `UserSubscribe` (no live sinks).
- Cache→network fallback (`UserAccountGet` miss → `UserAccountRequest`; `PositionGet` miss → `PositionRequest`). C# positions already use `PositionRequest` (good). Accounts do **not**.

---

## 2. Groups — cache walk, not `GroupRequestArray`

Public: `GetGroupsAsync` → `GetGroupsCore` (lines 40–41, 123–151).

| Step | C# call | Line | SDK | Source |
|---|---|---|---|---|
| Count | `_manager.GroupTotal()` | 128 | `IMTManagerAPI::GroupTotal` h:205 | **local cache** |
| Object | `_manager.GroupCreate()` | 130 | `GroupCreate` h:199 | heap |
| Walk | `_manager.GroupNext(i, grp)` | 135 | `GroupNext` h:206 | **local cache** |
| Fields | `Group()`, `Currency()`, `CurrencyDigits()`, `Company()`, `MarginCall()`, `MarginStopOut()`, `PermissionsFlags()` | 137–144 | `IMTConGroup` | pumped config |

`PermissionsFlags & 0x2` maps to `PERMISSION_ENABLE_CONNECTION = 0x00000002` (`MT5APIConfigGroup.h` 463). Not a discovery filter.

### SDK request API **not called**

`IMTManagerAPI` (`MT5APIManager.h` 208–212):

```cpp
virtual MTAPIRES  GroupRequest(LPCWSTR name,IMTConGroup* group)=0;
virtual IMTConGroupArray* GroupCreateArray(void)=0;
virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

`GroupRequestArray("*")` is the **no-pump complete enumerator** for every group the manager ACL allows (A39). **Zero** hits under `D:\Prop\src`.

| If pump of `PUMP_MODE_GROUPS` completed | `GroupTotal`/`GroupNext` ≈ ALL manager-visible groups |
|---|---|
| If pump incomplete / `pump=0` / connect never retried | `GroupTotal()==0` or a **subset**; `GetGroupsCore` returns that list with **no** error |
| Manager ACL is a mask (`demo\*`) | Server already filtered; cache cannot show more than ACL |

`GetAccountsCore` (lines 165–176) repeats the same `GroupTotal`/`GroupNext` walk when `group` is null — again cache-only, then one user pull **per cached name**.

---

## 3. Traders — cache `Get*ByGroup`, not `UserRequestArray` / `UserLogins`

Public: `GetAccountsAsync(string? group)` → `GetAccountsCore` (lines 43–44, 153–221).

Per group name (179–217):

| C# call | Line | SDK | Source |
|---|---|---|---|
| `UserCreateArray()` | 181 | h:408 | heap |
| `UserCreateAccountArray()` | 182 | h:409 | heap |
| `UserGetByGroup(gname, users)` | 185 | h:672 | **pump user cache** |
| `UserAccountGetByGroup(gname, accounts)` | 186 | h:742 | **pump account cache** |

Return codes of both `Get*` calls are **ignored**. Empty array is treated as “no traders”.

### SDK request / login-list APIs **not called**

`MT5APIManager.h`:

| API | Line | Role |
|---|---|---|
| `UserLogins(group, logins, logins_total)` | 254 | **Network** list of every login in group (YoPips `MT5Manager::GetUserLogins` 315–328) |
| `UserRequestArray(group, users)` | 410 | **Network** full user records |
| `UserAccountRequestArray(group, accounts)` | 411 | **Network** account records |
| `UserRequest(login)` / `UserAccountRequest(login)` | 252, 261 | Single-login network |
| `UserRequestByLogins` / `UserAccountRequestByLogins` | 671, 740 | Batch network |

Zero hits for `UserRequestArray`, `UserLogins`, `UserAccountRequestArray` under `D:\Prop\src`.

YoPips/Prop C++ uses `UserLogins` for “all traders in group” and cache-first `UserAccountGet` with **`UserAccountRequest` fallback** (`mt5_manager.cpp` 339–348). C# has neither.

| Condition | What C# returns |
|---|---|
| Pump `USERS` completed for that group | Users in cache ≈ ALL ACL-visible traders in that group |
| Pump in progress / group not synced | **Subset** (or empty) |
| `pump=0` (if someone later connected that way) | `UserGetByGroup` empty — **silent miss** |
| Caller passes a group name | Only that name; no `*` mask |
| Caller passes null | Only traders in **cached** group names |

Not a hardcoded plan-map filter. The universe is still **whatever the pump put in memory**.

---

## 4. Deals and positions — mixed (request vs unused group request)

### Deals — server request (good)

| Method | C# | Lines | SDK |
|---|---|---|---|
| `GetDealsAsync` | `DealRequest(login, fromUnix, toUnix, arr)` | 223–237 | h:270 — **network** |
| `GetGroupDealsAsync` | `DealRequestByGroup(group, from, to, arr)` | 240–255 | h:520 — **network** |

Empty/`NOTFOUND`/`OK_NONE` swallowed; other codes throw. Independent of user pump.

### Positions — per-login request only

`GetPositionsCore` (258–292) calls `PositionRequest(login, arr)` (h:282) — **network**, good for one login.

**Not used:**

| API | Header | Why it matters |
|---|---|---|
| `PositionRequestByGroup(group, positions)` | h:534 | ALL open positions in a group **without** pump |
| `PositionGetByGroup(mask, positions)` | h:286 | Cache only |
| `PositionGet(login, ...)` | h:280–281 | Cache; YoPips tries this first |

C# pumps `PUMP_MODE_POSITIONS` but never reads the position cache. Bulk “all traders’ positions” would need N `PositionRequest`s or one `PositionRequestByGroup`. Neither group path exists on the C# connector.

---

## 5. SDK methods named in the brief vs C# usage

| SDK (`IMTManagerAPI`) | Header | Used by `NativeMt5BrokerConnector`? |
|---|---|---|
| `GroupRequestArray` | 212 | **No** |
| `UserRequestArray` | 410 | **No** |
| `UserLogins` | 254 | **No** |
| `PositionRequestByGroup` | 534 | **No** |
| `DealRequestByGroup` | 520 | **Yes** — `GetGroupDealsCore` L248 |

---

## 6. Achiever HTTP proxy format

SDK `MTProxyInfo` (`MT5APIManager.h` 76–92):

```
enable : int32
type   : PROXY_SOCKS4=0 / PROXY_SOCKS5=1 / PROXY_HTTP=2
address[64] : IP:port
auth[64]    : login:password
```

C# `ConnectCore` 83–93:

```csharp
if (_opt.ProxyEnabled && !string.IsNullOrWhiteSpace(_opt.ProxyHost))
{
    var proxy = new MTProxyInfo
    {
        enable = 1,
        type = MTProxyInfo.Type.PROXY_HTTP,
        address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
        auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
    };
    _manager.ProxySet(proxy);
}
```

| Check | Result |
|---|---|
| `address = host:port` | **Matches** SDK comment and YoPips `address + L":" + port` (`mt5_manager.cpp` 42–44, 82–83) |
| `auth = user:pass` when user non-empty | **Matches** YoPips 46–49 / 85–87 |
| Empty user → `auth=""` | **OK** (no `user:` leftover) |
| Type | **HTTP** only (`PROXY_HTTP=2`). No SOCKS switch |
| Applied before `Connect` | **Yes** (83–93 then 95–99) |
| Buffer 64 wchar | C# wrapper owns marshaling; host:port `81.29.145.69:49527` fits |

Wiring (`LiveMt5Registration.cs` 20–33): Achiever reads `ACHIEVER_PROXY_ENABLED`, `ACHIEVER_PROXY_HOST`, `ACHIEVER_PROXY_PORT`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD`. StarwaveFX `ProxyEnabled = false` (42). Seed host/port (non-secret) in `BrokerCatalogSeed.cs` 27–29: `81.29.145.69:49527`, `ProxyEnabled=true` — **catalog only**; live `ProxySet` uses env via `LiveMt5Registration`, not the seed row.

Gaps vs YoPips (not format bugs):

- No `wcsncpy` truncate log; long `auth` could be truncated by the native struct (64 wchar).
- No re-apply / `m_proxyApplied` hot-reload.
- If `ACHIEVER_PROXY_ENABLED` is not `true`, Achiever connects **direct** (R012: this lab egress is not the allow-list → **1012**).

---

## 7. Gap list (actionable)

| ID | Severity | Gap | Evidence |
|---|---|---|---|
| G1 | High | Groups = cache only; no `GroupRequestArray("*")` | `GetGroupsCore` 128–135; no symbol in `src` |
| G2 | High | Traders = cache only; no `UserRequestArray` / `UserLogins` | `GetAccountsCore` 185–186 |
| G3 | High | No pump-none `Connect(..., 0)` fallback | `ConnectCore` 99–105 vs YoPips 114–135 |
| G4 | High | `UserGetByGroup` / `UserAccountGetByGroup` return codes discarded | L185–186 |
| G5 | Med | No cache→request fallback for accounts | YoPips 339–348 |
| G6 | Med | No `PositionRequestByGroup` for census | L258–265 only per-login `PositionRequest` |
| G7 | Med | `GroupNext` failure = `continue` (holes in index walk) | L135–136 |
| G8 | Low | Pumps `POSITIONS` but never `PositionGet*` | L96–98 vs L265 |
| G9 | Low | No `GroupSubscribe` / `UserSubscribe`; snapshot only | connector has no sinks |
| G10 | Ops | Achiever proxy format OK; enable flag must be true or Connect is direct | `LiveMt5Registration` 27–31; R012 |

**What is already correct for “all history / all open pos for one login”:** `DealRequest`, `DealRequestByGroup`, `PositionRequest`. Those do not depend on the user/group pump.

**What is not “ALL groups / ALL traders”:** any path that only walks `GroupTotal`/`GroupNext`/`UserGetByGroup` without a `*Request*` fallback.

---

## 8. Recommended complete enumerator (not implemented)

For a pump-optional, manager-ACL-complete snapshot (matches A39 + YoPips request path):

1. `ProxySet` HTTP `address=IP:port` `auth=user:pass` (already).
2. `Connect` with `PUMP_MODE_GROUPS|USERS|POSITIONS` (already).
3. **On fail:** `Connect(..., 0, timeout)` (YoPips). Mark session `pump=false`.
4. Groups: `GroupRequestArray("*", arr)` (or cache walk **if** `GroupTotal()>0` else request).
5. Traders: for each group (or mask `*`), `UserRequestArray` + `UserAccountRequestArray`, **or** `UserLogins` then `UserAccountRequestByLogins`.
6. Positions census: `PositionRequestByGroup(mask)`.
7. Deals: keep `DealRequest` / `DealRequestByGroup`.

Until 3–5 exist, do **not** claim the C# connector enumerates every group or every trader on the manager.

---

## 9. Checklist

- [x] `NativeMt5BrokerConnector.cs` read in full (330 lines).
- [x] SDK `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup` located in `MT5APIManager.h`.
- [x] YoPips pump-none fallback compared (`mt5_manager.cpp` 114–135).
- [x] Achiever proxy format checked (`address=IP:port`, `auth=user:pass`, `PROXY_HTTP`); passwords not printed.
- [x] Verdict: **cache subsets** for groups/traders; **request** for deals and per-login positions.
