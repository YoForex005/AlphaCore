# W500_RESEARCH_60 — `MT5APIManager.h` request APIs work without pump

| Field | Value |
|---|---|
| Slot | **60** |
| Agent | W500_RESEARCH_60 |
| Date | 2026-08-18 |
| Topic | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work **without pump**. Goal: fetch **ALL** Achiever+Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Group names and counts only. |
| Method | `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Live counts from existing probe JSON. **This slot did not re-attach to Manager or FIX.** |
| Sibling reports (same angle, earlier slots) | `W500_RESEARCH_0.md`, `W500_RESEARCH_12.md`, `W500_RESEARCH_20.md`. This slot **re-reads current trees**; it does not inherit their wording. `A001_native_connector.md` is **stale** vs `src`. |

**Honesty rule:** `*Request*` / `*RequestArray*` / `UserLogins` are **network RPCs**. `*Get*` / `*Total*` / `*Next*` / `*GetByGroup*` are **pump-cache**. A live census that used `GroupRequestArray` does **not** prove that session had `pump_mode=0` unless `PumpEnabled` was recorded. The probe JSON does **not** record `PumpEnabled`. Do not claim C++ `GetAllGroups` is no-pump-complete — it is cache-only.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| The five assigned APIs exist on `IMTManagerAPI` | **Yes** | header lines **212 / 254 / 410 / 520 / 534** (`MTManagerAPIVersion 5570`, 30 Jan 2026) |
| Those five are **request** (network), not pump-cache | **Yes** | paired against `GroupGet` / `UserGet` / `UserGetByGroup` / `PositionGet` / `PositionGetByGroup`; deals have **no** `DealGet` and **no** `PUMP_MODE_DEALS` |
| They work with `Connect(..., pump_mode=0)` | **Yes (SDK + product comments + pool)** | `IMTAdminAPI` pump bits are only MAIL/NEWS yet still expose `UserLogins` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`; YoPips/Prop C++ fallback + pool connect with literal `0` and still call `UserLogins` / `DealRequest` / `PositionRequest` |
| C# live path uses them first | **Yes** | `NativeMt5BrokerConnector`: `GroupRequestArray("*")`, `UserRequestArray`, `UserLogins` fallback, `DealRequestByGroup`, `PositionRequestByGroup`. **No branch on `_pumpEnabled`.** |
| ALL Achiever + Starwave groups + manager traders fetched | **Yes (measured 2026-08-18T08:42:16Z)** | Achiever **8 / 6512 / 1506**; Starwave **10 / 1948 / 478**; total **18 groups / 8460 traders / 1984 open pos**. Includes empty groups. |
| Copy to cTrader can place a live order from this process | **No** | `RealCopyEnabled` forced `false`; product C# has **zero** `35=D` builders; FIX session sends only `35=A` logon; shadow rows are `SHADOW_ONLY` |
| Risk to capital from fetch + this copy path | **None** | read-only Manager request + simulated shadow. No `DealerSend` / `OrderAdd` / `DealerBalance` / `NewOrderSingle` on the live C# connector |

One-line:

```text
Request APIs are network RPCs (pump optional). C# already uses them to list ALL 18 groups / 8460 manager traders. cTrader copy cannot emit 35=D.
```

---

## 1. Header inventory (`IMTManagerAPI`)

Vendored file (identical signatures in YoPips — same line numbers):

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`

Pin:

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

`Connect` takes an explicit pump bitmask. **Zero bits = no pump.**

```164:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

### 1.1 `EnPumpModes` — what pump actually fills

```125:144:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      PUMP_MODE_ACTIVITY      =0x00000002,   // pump users online activity
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_ORDERS        =0x00000008,   // pump orders
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      PUMP_MODE_SYMBOLS       =0x00000200,   // pump symbol configurations
      ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

| Flag | Value | Local cache it fills |
|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | `UserGet` / `UserTotal` / `UserGetByGroup` |
| `PUMP_MODE_ORDERS` | `0x00000008` | `OrderGet*` / `OrderGetOpen` |
| `PUMP_MODE_POSITIONS` | `0x00000080` | `PositionGet*` / `PositionGetByGroup` |
| `PUMP_MODE_GROUPS` | `0x00000100` | `GroupTotal` / `GroupNext` / `GroupGet` |
| `PUMP_MODE_FULL` | `0xffffffff` | everything pumpable |
| *(no bits)* | `0` | **request-only session** |

The C++ enum does **not** name `PUMP_MODE_NONE`. There is **no** `PUMP_MODE_DEALS`. The C# wrapper names the zero mask `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs:101`). Vendor Web-API sample names it too:

```40:43:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs
    public enum EnPumpModes
      {
      PUMP_MODE_NONE = 0x00000000
      };
```

### 1.2 The five assigned request APIs (network)

| API | Header line | Signature | Alloc / lifetime |
|---|---:|---|---|
| `GroupRequestArray` | 212 | `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` | caller `GroupCreateArray` + `Release` |
| `UserLogins` | 254 | `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` | **server** allocates; caller `Free` |
| `UserRequestArray` | 410 | `UserRequestArray(LPCWSTR group, IMTUserArray* users)` | caller `UserCreateArray` |
| `DealRequestByGroup` | 520 | `DealRequestByGroup(LPCWSTR group, int64_t from, int64_t to, IMTDealArray* deals)` | caller `DealCreateArray`; **not paged** |
| `PositionRequestByGroup` | 534 | `PositionRequestByGroup(LPCWSTR group, IMTPositionArray* positions)` | caller `PositionCreateArray` |

Quoted:

```211:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```252:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   virtual MTAPIRES  UserGroup(const uint64_t login,MTAPISTR& group)=0;
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```408:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

```520:535:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  PositionRequestByGroup(LPCWSTR group,IMTPositionArray* positions)=0;
```

Mask language is `CMTStr::CheckGroupMask` (`MT5APIStr.h:775–809`): comma-separated templates, `*` wildcards via `CheckGroupTemplate`, leading `!` = exclude. Same language as Administrator “Groups” on the manager. Mask `"*"` = every group **this manager ACL can see**, not a plan-mapping list.

Related request siblings (not assigned, but same class):

| API | Line | Used by C# connector? |
|---|---:|---|
| `GroupRequest` (single name) | 208 | no |
| `UserRequest` / `UserRequestByLogins` | 252 / 671 | `UserRequestByLogins` as login fallback |
| `UserAccountRequest` / `UserAccountRequestArray` | 261 / 411 | **yes** (`UserAccountRequestArray`) |
| `DealRequest(login, from, to)` | 270 | **yes** (per-login path) |
| `DealRequestPage` | 526 | **no** — group deal array is unpaged |
| `PositionRequest(login)` | 282 | **yes** |

### 1.3 Cache twins (require the matching pump bit)

| Cache (pump) | Line | Request twin | Pump bit |
|---|---:|---|---|
| `GroupTotal` / `GroupNext` / `GroupGet` | 205–207 | `GroupRequest` (208) / `GroupRequestArray` (212) | `PUMP_MODE_GROUPS` |
| `UserTotal` / `UserGet` | 250–251 | `UserRequest` (252) / `UserLogins` (254) / `UserRequestArray` (410) | `PUMP_MODE_USERS` |
| `UserGetByGroup` / `UserGetByLogins` | 672–673 | `UserRequestArray` / `UserRequestByLogins` (671) | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | 260 / 742 | `UserAccountRequest` / `UserAccountRequestArray` (261 / 411) | account cache from user pump |
| `PositionGet` / `PositionGetByGroup` | 280–281 / 286 | `PositionRequest` (282) / `PositionRequestByGroup` (534) | `PUMP_MODE_POSITIONS` |
| *(no `DealGet`)* | — | `DealRequest` (269–270) / `DealRequestByGroup` (520) | **none exists** |

The pairing is the measured proof that `*Request*` is not a cache read. Deals are the strongest case: the SDK never offered a deal pump, so `DealRequest*` **must** be a server query.

### 1.4 `IMTAdminAPI` — request APIs cannot require user/group/position pump

Admin connect still takes `pump_mode`, but the Admin enum has **no** USERS / GROUPS / POSITIONS bits:

```788:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

Admin still has:

| API | Admin line |
|---|---:|
| `DealRequestByGroup` | 1099 |
| `UserLogins` | 1172 |
| `UserRequestArray` | 1173 |
| `PositionRequestByGroup` | 1268 |

Admin group config is cache-only (`GroupTotal`/`GroupNext`/`GroupGet` at 910–912) — **no** `GroupRequest` / `GroupRequestArray`. Group discovery without pump is therefore **Manager-only**. This product uses `CreateManager` → `CIMTManagerAPI` / `IMTManagerAPI`, not Admin.

### 1.5 Independent of Manager pump: Web API `UserLogins`

Vendor Web API sends HTTP `WEB_CMD_USER_USER_LOGINS` (`Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Protocol\MTUserBase.cs:347+`). There is no pump on that transport. Same command name, same group-mask argument.

---

## 2. Product comments that treat request as no-pump-valid

These are **not** vendor prose. They are production comments written against this same header, and they match the Get/Request pairing.

### 2.1 YoPips / Prop `MT5Manager::Connect` fallback

Both trees (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` and `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`) are identical on Connect:

```114:134:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);

        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump; this lets journal
        // sync and other request-only operations function even when the pump
        // connection is unavailable (IP not yet whitelisted for pump, etc.)
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        ...
        m_pumpMode = false;
        ...
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

Wrapper `pumpMode==0` is **not** passed through as SDK `0`. It is remapped to `USERS|ORDERS|POSITIONS|SYMBOLS` (omits `GROUPS`) first; only a failed pump connect retries literal `0`.

### 2.2 Pool sessions are *born* request-only

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

The same session then calls `UserLogins` (request) and cache-first `UserAccountGet` with **`UserAccountRequest` fallback** because the cache is empty:

```212:223:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
bool MT5Session::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

```235:238:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Cache-first: try the in-memory pump cache (UserAccountGet) before the
    // network UserAccountRequest. Pool sessions connect mode=0/no-pump so this
    // cache is normally empty and the fallback runs — harmless.
```

`GetPositions` is the same pairing (`PositionGet` then `PositionRequest`). `GetDeals` never even tries a cache — it only calls `DealRequest`.

### 2.3 C++ `GetAllGroups` is still cache-only (gap, not this slot’s C# path)

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
    uint32_t total = m_manager->GroupTotal();
    ...
    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
```

Zero hits for `GroupRequestArray` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup` under YoPips `src\`. `GetUserLogins` **is** the request API (`UserLogins`, lines 315–327). Completeness without pump for **groups** requires `GroupRequestArray("*")`, which the C++ wrapper does not call.

---

## 3. C# live path — request first, pump optional

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). Implements `IMt5BrokerConnector`, `IMt5BulkDealReader`, `IMt5BulkPositionReader`.

### 3.1 Connect: pump preferred, `PUMP_MODE_NONE` fallback keeps the same request surface

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
            ...
            _connected = true;
            _pumpEnabled = false;
```

`GetGroups` / `GetAccounts` / `GetGroupDeals` / `GetGroupPositions` do **not** branch on `_pumpEnabled`. They call request APIs either way. Pump is an optimization for cache fallbacks, not a completeness gate.

`DealerSend` / `OrderAdd` / `DealerBalance` — **zero hits** under `D:\Prop\src\Mt5`. The connector is read-only.

### 3.2 Groups — `GroupRequestArray("*")` then cache only if empty

```152:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var arr = _manager!.GroupCreateArray();
            try
            {
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
            }
            finally { arr.Release(); }

            if (list.Count == 0)
            {
                ... GroupTotal / GroupNext cache walk ...
            }
```

This is the no-pump-complete enumerator. Mask is `"*"`, not a plan list. Empty-group names are kept (`AddGroup` only skips blank/duplicate names). Cache walk is a **fallback**, not the primary.

### 3.3 Traders — `UserRequestArray` then cache then `UserLogins`

```216:237:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        ...
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
            if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
                _manager.UserAccountGetByGroup(gname, accounts);
```

`GetAccountsAsync(null)` walks **every** name from `GetGroupsCore` (`GetAccountsCore` L189–214). That is ALL manager-visible traders, not a `Take(200)` slice (current ingest has zero `Take(` — see W500_RESEARCH_35).

`UserGetByGroup` is pump-cache and is only used if `UserRequestArray` returns a hard error. `UserLogins` is the second request path when the user array is empty.

### 3.4 Deals / positions — group request, no pump required

```307:307:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
```

```344:346:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.PositionRequestByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    res = _manager.PositionGetByGroup(mask, arr);
```

Deals are sliced into 14-day windows (`Windows`, L355–366) because `DealRequestByGroup` is **not paged** in this connector (`DealRequestPage` unused). That is a memory/limit concern, not a pump concern.

### 3.5 Ingest asks for ALL, not a plan subset

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs:38–51`):

1. `GetGroupsAsync` → `GroupRequestArray("*")`
2. `GetAccountsAsync(null)` → every group name × `UserRequestArray`
3. Persist batches

`SyncBrokerAsync` then `GetGroupDealsAsync` per group and `GetGroupPositionsAsync("*")`.

Two connectors are registered (`LiveMt5Registration.CreateConnectors`):

| Broker | Code | Proxy |
|---|---|---|
| Achiever | `BrokerCodes.Achiever` = `"ACHIEVER"` | optional HTTP (`ACHIEVER_PROXY_*`) |
| StarwaveFX | `BrokerCodes.StarwaveFx` = `"STARWAVEFX"` | **forced off** (`ProxyEnabled = false`) |

---

## 4. Live census (prior probe; this slot did not re-attach)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs`  
UTC: **2026-08-18T08:42:16.8519545+00:00**  
Note in JSON: `"Passwords never written. Groups and manager logins only."`

The probe uses `LiveMt5Registration.CreateConnectorsFromEnvironment()` — the **same** `NativeMt5BrokerConnector` request walk — then `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.

| Broker | Connected | Groups | Traders | Open positions | Elapsed |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 | 7212.6 ms |
| STARWAVEFX | true | 10 | 1948 | 478 | (second object) |
| **Total** | | **18** | **8460** | **1984** | |

### 4.1 Achiever group book (counts only)

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
| **sum** | **6512** |

### 4.2 StarwaveFX group book (counts only)

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | **0** |
| `Starwave\real\FX3\grp3` | **0** |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | **0** |
| `Starwave\real\FX3\LP` | 2 |
| **sum** | **1948** |

Empty groups (`demo\yo-instant`, three Starwave `real` groups) are present. That is the proof the walk is **ALL manager-visible groups**, not “groups that have traders.”

**Caveats (do not over-claim):**

1. Probe JSON does **not** record `_pumpEnabled`. The connect *may* have succeeded on the first (pump) attempt. Completeness still comes from `GroupRequestArray` / `UserRequestArray`, which do not need the pump.
2. ALL = **manager-ACL-visible**. Groups outside these two manager records are invisible by design.
3. This slot did **not** re-run Connect. Counts are the 08:42Z artifact.

Dashboard remeasure (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` **8460**, `/api/groups` **18**. Same book.

---

## 5. Copy to cTrader must not send live orders (no loss)

### 5.1 No `35=D` builder exists

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` builds **only** Logon:

```96:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
            (553, username),
            (554, password)
```

Grep of `D:\Prop\src` for `35=D` / `NewOrderSingle` / `DealerSend` / `OrderAdd`:

| Hit | Meaning |
|---|---|
| `LiveRuntimeStatus.copyNote` | string: “NewOrderSingle disabled…” |
| `CTraderFixLogonHostedService` log | “NewOrderSingle still disabled” |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false**; comment only |
| `DependencyInjection` | `RealCopyEnabled = false` — “Live NewOrderSingle is not implemented” |
| `ExecutionOrderStateMachine.MayRetryNewOrderSingle` | FSM helper; no socket send |
| Demo/seed last-error strings | text only |

**Zero** product methods assemble `35=D`. Classification: **`SAFE_BY_ABSENCE`**.

### 5.2 Flags stay off even after a successful TRADE logon

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

```68:68:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;
```

`CTraderFixOptions.RealCopyExecutionEnabled` default is `false` (L35). The standalone `apps/fix-worker/Worker.cs` never opens a TRADE socket; it stamps `NewOrderSingle remains off` even if config says true.

### 5.3 Shadow persist never creates an `ExecutionIntent`

`EfTradingStore.PersistDemoShadowAsync` writes `CopyIntent.Status = "SHADOW_ONLY"` and a local `ShadowCopyEngine.SimulateEntry`. No FIX send. No `ExecutionIntent` row.

`ShadowCopyEngine` is arithmetic (bid/ask + modeled 0.05 slip). It has no network.

### 5.4 Manager fetch cannot open a cTrader position

`GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup` are **read** RPCs. They do not call `DealerSend`, `DealerBalance`, `OrderAdd`, or `TradeAccountSet`. Fetching 8460 traders does not place an order.

---

## 6. Residual risks (not capital)

| Risk | Severity | Note |
|---|---|---|
| `DealRequestByGroup` unpaged | Med (ops) | Entire window buffered. 14-day slices mitigate. `DealRequestPage` unused. |
| Probe did not record `PumpEnabled` | Low (honesty) | Does not change request completeness. |
| C++ `GetAllGroups` cache-only | Med if someone uses YoPips wrapper for discovery | Prop C# path is request-complete. |
| Manager ACL ≠ server universe | By design | Two managers, two books. |
| `UserLogins` fail-closed on null pointer in C++ | Low | Empty group can look like API failure (`if (res != MT_RET_OK \|\| !raw_logins)`). C# uses `UserRequestArray` first. |
| In-memory API DB when `DATABASE_URL` is placeholder | Ops | Does not send live orders. |

---

## 7. Stale claims (do not reuse)

| Claim | Where | Current truth |
|---|---|---|
| Zero `GroupRequestArray` / `UserRequestArray` under `src` | `A001_native_connector.md` | **False now.** Both are primary in `NativeMt5BrokerConnector`. |
| Groups/traders are pump-cache only | A001 | **False now.** Request first; cache is fallback. |
| C++ `GetAllGroups` is no-pump complete | some early notes | **False.** Cache `GroupTotal`/`GroupNext` only. |
| Live census proves `pump_mode=0` | if anyone writes that | **Unproven.** JSON has no `PumpEnabled`. |

---

## 8. Sources (absolute paths)

| Path | Why |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Five APIs, pump enum, Admin twins |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | `CheckGroupMask` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | Identical signatures |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Request-first live walk + no-pump fallback |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog ALL groups/accounts |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Pump remap + no-pump retry + `UserLogins` + `DealRequest` |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | Born `mode=0`; still calls `UserLogins` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Same Connect / cache `GetAllGroups` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled = false` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces flag false after logon |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `SHADOW_ONLY` |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Census runner |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 / 1984 |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Dashboard 18 / 8460; copy off |

---

## 9. Checklist

- [x] SDK `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup` located on `IMTManagerAPI`.
- [x] Request vs Get pairing documented; deals have no pump bit.
- [x] `IMTAdminAPI` still exposes four of five request APIs with only MAIL/NEWS pump bits — request cannot require user/group/position pump.
- [x] Pool `Connect(..., 0)` still calls `UserLogins` / `DealRequest` / `PositionRequest`.
- [x] C# connector uses request APIs first and does not branch on `_pumpEnabled`.
- [x] ALL Achiever+Starwave groups+traders measured (18 / 8460), including empty groups.
- [x] cTrader copy cannot emit `35=D`. `RealCopyEnabled` forced false. Risk to capital **NONE**.
- [x] Product source not modified. Secrets not printed.

*End of W500_RESEARCH_60. Product source was not modified. No secrets printed.*
