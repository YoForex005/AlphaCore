# W500_RESEARCH_20 — `MT5APIManager.h` request APIs work without pump

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_20.md` |
| Slot | **20** |
| Date | 2026-08-18 |
| Topic | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work without pump. Goal: fetch ALL Achiever+Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** |
| Method | Read vendor header + C# native connector + YoPips/Prop C++ wrappers + FIX send path + live census JSON. No new live attach in this slot. |

**Honesty rule:** `*Request*` / `*RequestArray*` / `UserLogins` are **network RPCs**. `*Get*` / `*Total*` / `*Next*` / `*GetByGroup*` are **pump-cache**. A live census that used `GroupRequestArray` does **not** prove the session had `pump_mode=0` unless `PumpEnabled` was recorded. Do not claim C++ `GetAllGroups` is no-pump-complete — it is cache-only.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| The five assigned APIs exist on `IMTManagerAPI` | **Yes** | header lines 212 / 254 / 410 / 520 / 534 |
| Those five are **request** (network), not pump-cache | **Yes** | paired against `GroupGet` / `UserGet` / `UserGetByGroup` / `PositionGet` / `PositionGetByGroup`; deals have **no** `DealGet` and **no** `PUMP_MODE_DEALS` |
| They work with `Connect(..., pump_mode=0)` | **Yes (SDK + product comments)** | `IMTAdminAPI` pump bits are only MAIL/NEWS yet still expose `UserLogins` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`; C++ fallback + pool connect with literal `0` and still call request APIs |
| C# live path uses them first | **Yes** | `NativeMt5BrokerConnector` `GroupRequestArray("*")`, `UserRequestArray`, `UserLogins` fallback, `DealRequestByGroup`, `PositionRequestByGroup` |
| ALL Achiever + Starwave groups + manager traders fetched | **Yes (measured 2026-08-18T08:42:16Z)** | Achiever **8 / 6512 / 1506**; Starwave **10 / 1948 / 478**; total **18 groups / 8460 traders / 1984 open pos**. Includes empty groups. |
| Copy to cTrader can place a live order from this process | **No** | `RealCopyEnabled` forced `false`; product C# has **zero** `35=D` builders; FIX session sends only `35=A` logon; shadow rows are `SHADOW_ONLY` |
| Risk to capital from fetch + this copy path | **None** | read-only Manager request + simulated shadow. No `DealerSend` / `OrderAdd` / `NewOrderSingle` on the live C# connector |

One-line:

```text
Request APIs are network RPCs (pump optional). C# already uses them to list ALL 18 groups / 8460 manager traders. cTrader copy cannot emit 35=D.
```

---

## 1. Header inventory (`IMTManagerAPI`)

Vendored file (identical signatures in YoPips):

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`

`Connect` takes an explicit pump bitmask. `0` means no pump bits:

```164:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

`EnPumpModes` (`125:144`) has `USERS`, `ORDERS`, `POSITIONS`, `GROUPS`, `SYMBOLS`, … and `PUMP_MODE_FULL=0xffffffff`. There is **no** `PUMP_MODE_DEALS`. C#/Web wrappers name the zero mask `PUMP_MODE_NONE = 0x00000000` (`MetaQuotes.MT5WebAPI.cs:42`).

### 1.1 Assigned request APIs (network)

| API | Header line | Signature | Alloc / lifetime |
|---|---:|---|---|
| `GroupRequestArray` | 212 | `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` | caller `GroupCreateArray` + `Release` |
| `UserLogins` | 254 | `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` | **server** allocates; caller `Free` |
| `UserRequestArray` | 410 | `UserRequestArray(LPCWSTR group, IMTUserArray* users)` | caller `UserCreateArray` |
| `DealRequestByGroup` | 520 | `DealRequestByGroup(LPCWSTR group, int64_t from, int64_t to, IMTDealArray* deals)` | caller `DealCreateArray`; **not paged** |
| `PositionRequestByGroup` | 534 | `PositionRequestByGroup(LPCWSTR group, IMTPositionArray* positions)` | caller `PositionCreateArray` |

Mask language is `CMTStr::CheckGroupMask` (`MT5APIStr.h:775–809`): comma-separated templates, `*` wildcards, leading `!` exclude. Same language as Administrator “Groups” on the manager. Mask `"*"` = every group **this manager ACL can see**, not a plan-mapping list.

Repeated on `IMTAdminAPI` (except `GroupRequestArray` — Admin has **no** group request):

| API | Admin line |
|---|---:|
| `DealRequestByGroup` | 1099 |
| `UserLogins` | 1172 |
| `UserRequestArray` | 1173 |
| `PositionRequestByGroup` | 1268 |

### 1.2 Cache twins (require the matching pump bit)

| Cache (pump) | Line | Request twin | Pump bit |
|---|---:|---|---|
| `GroupTotal` / `GroupNext` / `GroupGet` | 205–207 | `GroupRequest` (208) / `GroupRequestArray` (212) | `PUMP_MODE_GROUPS` |
| `UserTotal` / `UserGet` | 250–251 | `UserRequest` (252) / `UserLogins` (254) / `UserRequestArray` (410) | `PUMP_MODE_USERS` |
| `UserGetByGroup` / `UserGetByLogins` | 672–673 | `UserRequestArray` / `UserRequestByLogins` (671) | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | 260 / 742 | `UserAccountRequest` / `UserAccountRequestArray` (261 / 411) | account cache from user pump |
| `PositionGet` / `PositionGetByGroup` | 280–281 / 286 | `PositionRequest` (282) / `PositionRequestByGroup` (534) | `PUMP_MODE_POSITIONS` |
| *(no `DealGet`)* | — | `DealRequest` (269–270) / `DealRequestByGroup` (520) | **none exists** |

The pairing is the measured proof that `*Request*` is not a cache read. Deals are the strongest case: the SDK never offered a deal pump, so `DealRequest*` **must** be a server query.

### 1.3 `IMTAdminAPI` pump enum (request APIs cannot require user/group/position pump)

```788:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

Admin still has `UserLogins`, `UserRequestArray`, `DealRequestByGroup`, `PositionRequestByGroup`. Admin group config is cache-only (`GroupTotal`/`GroupNext`/`GroupGet` at 910–912) — **no** `GroupRequest` / `GroupRequestArray`. Group discovery without pump is therefore **Manager-only**.

### 1.4 Independent of Manager pump: Web API `UserLogins`

Vendor Web API sends HTTP `WEB_CMD_USER_USER_LOGINS` (`MTUserBase.cs:347–372`). There is no pump on that transport. Same command name, same group-mask argument.

---

## 2. Product wiring — C# uses request first (correct for no-pump)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

### 2.1 Connect: pump preferred, `PUMP_MODE_NONE` fallback keeps the same request surface

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
            // ...
            _connected = true;
            _pumpEnabled = false;
```

`GetGroups` / `GetAccounts` / `GetGroupDeals` / `GetGroupPositions` do **not** branch on `_pumpEnabled`. They call request APIs either way.

### 2.2 Groups — `GroupRequestArray("*")` then cache only if empty

```155:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
                if (res == MTRetCode.MT_RET_OK || res == MTRetCode.MT_RET_OK_NONE)
                {
                    for (uint i = 0; i < arr.Total(); i++)
                    {
                        var g = arr.Next(i);
                        if (g is null)
                            continue;
                        AddGroup(list, seen, g);
                    }
                }
            // if list.Count == 0 → GroupTotal / GroupNext cache walk
```

This is the no-pump-complete enumerator. Mask is `"*"`, not a plan list.

### 2.3 Traders — `UserRequestArray` → cache `UserGetByGroup` → `UserLogins` + `UserRequestByLogins`

```223:237:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
            }

            var acctReq = _manager.UserAccountRequestArray(gname, accounts);
```

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore` (`189:213`). No `Take(200)`. Empty groups stay listed (0 users is success).

### 2.4 Deals / positions — request-by-group

```307:307:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
```

```344:346:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.PositionRequestByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    res = _manager.PositionGetByGroup(mask, arr);
```

Deals are sliced into 14-day windows (`Windows`, L355–366). `DealRequestByGroup` itself is **not paged** (`DealRequestPage` exists at header 526 and is unused). That is a memory/timeout risk on `demo\yo-2step` (6295 accounts), not a capital risk.

Ingest (`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync`) calls `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupDealsAsync` per group + `GetGroupPositionsAsync("*")`. That is the ALL-groups / ALL-traders / ALL-open-pos path.

C# connector has **zero** `DealerSend` / `DealerBalance` / `UserAdd` / `OrderAdd` / `PositionUpdate` hits under `D:\Prop\src\Mt5`. It is read-only.

---

## 3. C++ / YoPips wrappers — request APIs work; group list is **not** request-complete

Prop `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` and YoPips `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` are the same Connect + `GetUserLogins` text (compared this slot).

### 3.1 Connect fallback documents request-without-pump

```114:134:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump; this lets journal
        // sync and other request-only operations function even when the pump
        // connection is unavailable (IP not yet whitelisted for pump, etc.)
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // ...
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

Trap: `MT5Manager::Connect(pumpMode=0)` **rewrites** `0` to `USERS|ORDERS|POSITIONS|SYMBOLS` (L103–107) **before** the first native call. Only the inner fallback passes literal `0`. Callers who pass `0` thinking “no pump” actually request a pump first.

### 3.2 Pool sessions are the clean no-pump proof in-tree

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

Same session then calls `UserLogins` (L218), `UserRequest` (L184), `UserAccountRequest` after cache miss (L239–241), `PositionRequest` after empty cache (L293–301), `DealRequest` (L564). Comments state the cache is “normally empty” on `mode=0` and **must** fall through to the network call.

### 3.3 `GetUserLogins` is the assigned `UserLogins` RPC

```315:327:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    // ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is an alias (L1015–1016). Fail-closed on `raw_logins == nullptr`: an **empty** group can look like an API failure. C# avoids that by treating `UserRequestArray` total `0` as success and only calling `UserLogins` when it needs a login list.

### 3.4 `GetAllGroups` does **not** call `GroupRequestArray`

```962:981:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    uint32_t total = m_manager->GroupTotal();
    // GroupNext cache walk only
}
```

Same on `MT5Session::GetAllGroups` (`mt5_pool.cpp:731–748`). Zero `GroupRequestArray` hits under either C++ tree’s `src/core`. A no-pump C++ session can therefore return `groups: []` with `success: true` even when the manager owns many groups. **Do not use the C++ wrapper for ALL-group discovery without `PUMP_MODE_GROUPS` or a `GroupRequestArray("*")` add.** The live C# path already has that add.

C++ also never wraps `UserRequestArray`, `DealRequestByGroup`, or `PositionRequestByGroup`. Per-login `DealRequest` / `PositionRequest` still work without pump.

---

## 4. Live ALL-groups / ALL-traders (measured, not this slot’s attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `Connect` + `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`  
UTC: `2026-08-18T08:42:16.8519545+00:00`  
Passwords: not written. `PumpEnabled` was **not** logged (first Connect tries pump; request APIs ran either way).

| Broker | Connect | Groups | Traders | Open positions | How |
|---|---|---:|---:|---:|---|
| Achiever | OK | **8** | **6512** | **1506** | HTTP whitelist proxy |
| StarwaveFX | OK | **10** | **1948** | **478** | direct |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (all `USD`; empty group still returned):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | **0** |
| `demo\yo-payp` | 23 |

StarwaveFX groups:

| Group | CCY | Accounts |
|---|---|---:|
| `Starwave\cent\FX1\grp1` | USC | 11 |
| `Starwave\cent\FX1\grp2` | USC | 4 |
| `Starwave\demo\FX2\grp1` | USD | 170 |
| `Starwave\demo\FX2\grp2` | USD | 1735 |
| `Starwave\real\FX3\grp1` | USD | 22 |
| `Starwave\real\FX3\grp2` | USD | **0** |
| `Starwave\real\FX3\grp3` | USD | **0** |
| `Starwave\real\FX3\grp4` | USD | 4 |
| `Starwave\real\FX3\grp5` | USD | **0** |
| `Starwave\real\FX3\LP` | USD | 2 |

Empty groups (`demo\yo-instant`, three Starwave real grps) prove the enumerator is `GroupRequestArray` / group-config, not “groups that happened to have pumped users.” Sum of per-group account counts = 6512 and 1948. Dashboard `/api/traders` = 8460, `/api/groups` = 18 (`CREDENTIALS_AND_COPY_STATUS.md`). That is ALL manager-visible groups and ALL manager-visible traders for those two logins.

ACL caveat (do not overclaim): `"*"` cannot see a group the manager login is forbidden to read. This census is complete for **these** manager accounts, not a proof of every group on the broker if ACL hides any.

---

## 5. Copy to cTrader — no live orders (no loss)

| Gate | Measured | Evidence |
|---|---|---|
| `LiveRuntimeStatus.RealCopyEnabled` | forced `false` | `DependencyInjection.cs:40–41`; logon host sets it `false` again (`CTraderFixLogonHostedService.cs:68`) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` | `CTraderFixOptions.cs:32–35` |
| FIX outbound `35` | **only `A` (Logon)** on the live session | `CTraderFixSession.cs:96` `(35, "A")` |
| Product `35=D` / `(35, "D")` | **0 hits** | re-grep this slot; E034 83 product `*.cs` |
| `NewOrderSingle` sender | **MISSING** | comments / log strings only (E002) |
| Native connector trade writes | **0** | no `DealerSend` / `OrderAdd` under `src\Mt5` |
| Shadow path | DB only | `CopyIntent.Status = "SHADOW_ONLY"`; `ShadowCopyEngine.SimulateEntry` (`EfTradingStore.cs:307–313`) |
| FIX QUOTE/TRADE | logon-only | `CTraderFixLogonHostedService` persists session rows; never sends D/F/G/H |

Dashboard copy note when the flag is false (`LiveRuntimeStatus.cs:42–43`):

```text
NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.
```

`SAFE_BY_ABSENCE`: flipping a display flag does not create a sender. Do not enable `REAL_COPY_EXECUTION_ENABLED`. Do not add `35=D` in a fetch task.

---

## 6. What this slot did **not** re-measure

| Item | Status |
|---|---|
| New Achiever/Starwave attach | **Not run** this slot. Census cited from `LIVE_GROUPS_AND_TRADERS.json`. |
| `PumpEnabled` on that attach | **Unknown.** Request APIs were used; pump may also have been up. |
| Isolated `Connect(..., 0)` then `GroupRequestArray` lab | **Not run.** Proven by SDK pairing + Admin pump enum + pool `mode=0` + C++ comments. |
| `DealRequestByGroup` 90-day completeness vs `DealRequestPage` | **Unproven.** Ingest windows 14 days; array is unpaged. |
| Groups hidden by manager ACL | **Unobservable** from this login. |

---

## 7. Implications for the goal

1. **Use the C# request path** (already wired) to fetch ALL groups and ALL traders. Do **not** fall back to C++ `GetAllGroups` on a no-pump session.
2. **Request APIs do not need pump.** Prefer `GroupRequestArray("*")`, `UserRequestArray` / `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup`. Cache `*Get*` is an optimization only when `_pumpEnabled`.
3. **Keep copy shadow-only.** Fetch + score + `SHADOW_ONLY` intents cannot take a live loss. A TRADE logon is not a send.
4. Operational (not capital) risk: `DealRequestByGroup` on `demo\yo-2step` over long windows can be huge. Keep the 14-day slices. Do not add live `35=D` to “make copy work.”

---

## 8. Sources (absolute)

| Path | Why |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | `IMTManagerAPI` / `IMTAdminAPI` signatures |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | `CheckGroupMask` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | live request path |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | ALL groups / ALL accounts ingest |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Achiever + Starwave connectors |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound `35=A` only |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | simulation, no socket |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `SHADOW_ONLY` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | no-pump fallback + `UserLogins`; cache-only `GetAllGroups` |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | `Connect(..., 0)` + request APIs |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | same Connect / `GetUserLogins` |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | census probe |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 / 1984 |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | dashboard match; NewOrderSingle off |

**Slot 20 verdict: PASS** — request APIs work without pump; C# already fetches ALL manager-visible Achiever+Starwave groups and traders; copy cannot send live orders.
