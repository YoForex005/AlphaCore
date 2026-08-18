# W500_RESEARCH_180 — `MT5APIManager.h` request APIs work without pump

| Field | Value |
|---|---|
| Slot | **180** |
| Agent | W500_RESEARCH_180 |
| Date | 2026-08-18 |
| Topic | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm request APIs work **without pump**. Goal: fetch **ALL** Achiever+Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** Flag booleans, group names, and counts only. No manager / proxy / FIX passwords. |
| Method | Fresh `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Header pin `MTManagerAPIVersion 5570` / `30 Jan 2026` on **both** vendor trees. YoPips + Prop `mt5_manager.cpp` Connect / `GetUserLogins` / `GetAllGroups`. `mt5_pool.cpp` `Connect(..., 0)`. Ingest, FIX session, DI, copy hop, probe JSON. **This slot did not re-attach to Manager or FIX.** |
| Sibling reports (same angle) | `W500_RESEARCH_0.md`, `W500_RESEARCH_20.md`, `W500_RESEARCH_60.md`, `W500_RESEARCH_80.md`, `W500_RESEARCH_100.md`, `W500_RESEARCH_120.md`, `W500_RESEARCH_140.md`. This slot **re-reads the current trees**. `W500_RESEARCH_160.md` is **absent** on disk. `A001_native_connector.md` (“zero `GroupRequestArray` under `src`”) is **stale**. Slots 80/100/120 “DI/hosted force `RealCopyEnabled=false`” is **stale vs current DI L41**. |

**Honesty rule:** `*Request*` / `*RequestArray*` / `UserLogins` are **network RPCs**. `*Get*` / `*Total*` / `*Next*` / `*GetByGroup*` are **pump-cache**. A live census that used `GroupRequestArray` does **not** prove that session had `pump_mode=0` unless `PumpEnabled` was recorded. The probe JSON does **not** record `PumpEnabled`. Do not claim C++ `GetAllGroups` is no-pump-complete — it is cache-only.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| The five assigned APIs exist on `IMTManagerAPI` | **Yes** | header lines **212 / 254 / 410 / 520 / 534** (`MTManagerAPIVersion 5570`, 30 Jan 2026) |
| Those five are **request** (network), not pump-cache | **Yes** | paired against `GroupGet` / `UserGet` / `UserGetByGroup` / `PositionGet` / `PositionGetByGroup`; deals have **no** `DealGet` and **no** `PUMP_MODE_DEALS` (0 hits in this header) |
| They work with `Connect(..., pump_mode=0)` | **Yes (SDK + product comments + pool)** | `IMTAdminAPI` pump bits are only MAIL/NEWS yet still expose `UserLogins` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`; YoPips/Prop C++ fallback + pool connect with literal `0` and still call `UserLogins` / `DealRequest` / `PositionRequest` |
| C# live path uses them first | **Yes** | `NativeMt5BrokerConnector`: `GroupRequestArray("*")` L155, `UserRequestArray` L223, `UserLogins` L230 fallback, `DealRequestByGroup` L307, `PositionRequestByGroup` L344. **`_pumpEnabled` is write-only — never a fetch gate** (assignments L30/36/96/110/140; 0 branch reads). |
| ALL Achiever + Starwave groups + manager traders fetched | **Yes (measured 2026-08-18T08:42:16Z; not re-probed)** | Achiever **8 / 6512 / 1506**; Starwave **10 / 1948 / 478**; total **18 groups / 8460 traders / 1984 open pos**. Independent re-sum of JSON `accounts` fields this slot. Includes empty groups. |
| Copy to cTrader can place a live order from the product host / copy hop | **No** | `CTraderFixSession` sends only `(35, "A")`. Product host has **0** `35=D` string literals. `CopyTradingService.NewOrderSingleImplemented = false`. Persist writes `AllowFixSend = false`. **0** `ExecutionIntent` writers (`CountAsync` only). `CanPromoteToLive => false`. |
| Risk to capital from fetch + this copy path | **None** | Manager **read** request RPCs + FIX Logon only. Classification **`SAFE_BY_ABSENCE`**. Residual: DI binds env `REAL_COPY_EXECUTION_ENABLED` (lab `.env` previously measured `true`); flag does not create a sender. Standalone `CTraderFixDemoTestTrade` can emit MsgType `D` on **demo** hosts only and is **not** registered in `AddTraderIntelligence`. |

One-line:

```text
Five Request APIs are network RPCs (pump optional). C# already uses them to list ALL 18 groups / 8460 manager traders. Product copy hop cannot emit 35=D (SAFE_BY_ABSENCE). Env REAL_COPY may be armed; sender still missing.
```

---

## 1. Header inventory (`IMTManagerAPI`)

Vendored file (identical signatures + same version in YoPips):

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`

Pin (both trees, this slot re-read YoPips L11–12):

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

The C++ enum does **not** name `PUMP_MODE_NONE`. There is **no** `PUMP_MODE_DEALS` (grep of this header: **0 hits**). There is **no** `DealGet(` (grep: **0 hits**). The C# wrapper names the zero mask `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs:101`). Vendor Web-API sample names it too:

```40:43:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs
    public enum EnPumpModes
      {
      PUMP_MODE_NONE = 0x00000000
      };
```

C# first-attempt pump mask is `GROUPS|USERS|POSITIONS` = `0x100|0x1|0x80` = **`0x181` (385)**. Fallback is integer `0`.

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

Mask language is `CMTStr::CheckGroupMask` (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` L775–809): comma-separated templates, `*` wildcards via `CheckGroupTemplate`, leading `!` = exclude. Same language as Administrator “Groups” on the manager. Mask `"*"` = every group **this manager ACL can see**, not a plan-mapping list.

Related request siblings (not assigned, but same class):

| API | Line | Used by C# connector? |
|---|---:|---|
| `GroupRequest` (single name) | 208 | no |
| `UserRequest` / `UserRequestByLogins` | 252 / 671 | `UserRequestByLogins` as login fallback (L232) |
| `UserAccountRequest` / `UserAccountRequestArray` | 261 / 411 | **yes** (`UserAccountRequestArray` L235) |
| `DealRequest(login, from, to)` | 270 | **yes** (per-login path L284) |
| `DealRequestPage` | 526 | **no** — group deal array is unpaged |
| `PositionRequest(login)` | 282 | **yes** (L327) |

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

Admin still has four of the five assigned request APIs:

| API | Admin line |
|---|---:|
| `DealRequestByGroup` | 1099 |
| `UserLogins` | 1172 |
| `UserRequestArray` | 1173 |
| `PositionRequestByGroup` | 1268 |

Admin group config is cache-only (`GroupTotal`/`GroupNext`/`GroupGet` at 910–912) — **no** `GroupRequest` / `GroupRequestArray` (Admin group block ends at `GroupReserved4` L916). Group discovery without pump is therefore **Manager-only**. This product uses `SMTManagerAPIFactory.CreateManager` → `CIMTManagerAPI` / `IMTManagerAPI`, not Admin.

`UserGetByGroup` is **absent** on Admin (Manager-only at L672). That is independent proof `UserGetByGroup` is a pump-cache helper, while `UserRequestArray` is the network enumerator.

### 1.5 Independent of Manager pump: Web API `UserLogins`

Vendor Web API sends HTTP `WEB_CMD_USER_USER_LOGINS` (`Examples\Web\NET\MetaQuotes.MT5WebAPI\Common\Protocol\MTUserBase.cs`). There is no pump on that transport. Same command name, same group-mask argument.

---

## 2. Product comments that treat request as no-pump-valid

These are **not** vendor prose. They are production comments written against this same header, and they match the Get/Request pairing.

### 2.1 YoPips / Prop `MT5Manager::Connect` fallback

Both trees (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` and `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`) are identical on Connect (this slot re-read YoPips L71–149; Prop lines 102–134 match):

```102:134:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
    ...
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", mode, 30000);
    if (res != MT_RET_OK) {
        ...
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

Prop `mt5_pool.cpp` L74–76 and YoPips `mt5_pool.cpp` L74–76 (this slot re-read both):

```74:76:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

The same session then calls `UserLogins` (request) and cache-first `UserAccountGet` with **`UserAccountRequest` fallback** because the cache is empty:

```211:223:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp
bool MT5Session::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

Prop pool comment at L235–238 (this slot re-read):

```235:238:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Cache-first: try the in-memory pump cache (UserAccountGet) before the
    // network UserAccountRequest. Pool sessions connect mode=0/no-pump so this
    // cache is normally empty and the fallback runs — harmless.
```

`GetPositions` is the same pairing (`PositionGet` then `PositionRequest`). `GetDeals` never even tries a cache — it only calls `DealRequest` (`mt5_manager.cpp` L492–493 comment: “DealRequest sends a network request to the MT5 server”).

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

Grep of YoPips `src\` for `GroupRequestArray` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`: **zero hits**. `GetUserLogins` **is** the request API (`UserLogins`, YoPips `mt5_manager.cpp` 315–327). Completeness without pump for **groups** requires `GroupRequestArray("*")`, which the C++ wrapper does not call.

C++ `GetUserLogins` fail-closes on a null pointer (`if (res != MT_RET_OK || !raw_logins) return false`). An empty group can look like an API failure. C# uses `UserRequestArray` first and treats `MT_RET_OK_NONE` / `MT_RET_ERR_NOTFOUND` as empty-ok.

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

`_pumpEnabled` assignments: L30 (field), L36 (public getter), L96, L110, L140. **Zero reads as a branch.** `GetGroups` / `GetAccounts` / `GetGroupDeals` / `GetGroupPositions` do **not** switch on pump. Pump is an optimization for cache fallbacks, not a completeness gate.

Grep of `D:\Prop\src` for `DealerSend` / `OrderAdd` / `DealerBalance`: **zero hits**. The C# connector is read-only. (YoPips `mt5_pool.cpp` **does** wrap `DealerSend` / `DealerBalance` — that is a different binary, not this C# fetch/copy path.)

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

This is the no-pump-complete enumerator. Mask is `"*"`, not a plan list. Empty-group names are kept (`AddGroup` L368–372 only skips blank/duplicate names). Cache walk is a **fallback**, not the primary.

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

`GetAccountsAsync(null)` walks **every** name from `GetGroupsCore` (`GetAccountsCore` L189–214). That is ALL manager-visible traders, not a `Take(200)` slice. Grep of `D:\Prop\src\Mt5` for `Take(`: **0**. Residual `Take(200)` is `GET /api/trades` reconstructed rows, not the Manager census.

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

`SyncBrokerAsync` then `GetGroupDealsAsync` per group (L67–71) and `GetGroupPositionsAsync("*")` (L84–85).

Two connectors are registered (`LiveMt5Registration.CreateConnectors`, 94/94 lines):

| Broker | Code | Proxy |
|---|---|---|
| Achiever | `BrokerCodes.Achiever` = `"ACHIEVER"` | optional HTTP (`ACHIEVER_PROXY_*`) |
| StarwaveFX | `BrokerCodes.StarwaveFx` = `"STARWAVEFX"` | **forced off** (`ProxyEnabled = false` L45) |

---

## 4. Live census (prior probe; this slot did not re-attach)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` (86 lines)  
UTC: **2026-08-18T08:42:16.8519545+00:00**  
Note in JSON: `"Passwords never written. Groups and manager logins only."`

The probe uses `LiveMt5Registration.CreateConnectorsFromEnvironment()` — the **same** `NativeMt5BrokerConnector` request walk — then `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")` (probe L24–29).

| Broker | Connected | Groups | Traders | Open positions | Elapsed |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 8 | 6512 | 1506 | 7212.5885 ms |
| STARWAVEFX | true | 10 | 1948 | 478 | 6413.478 ms |
| **Total** | | **18** | **8460** | **1984** | |

Independent re-sum of JSON `accounts` fields this slot:

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
| **sum** | **6512** (2+179+4+5+4+6295+0+23) |

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
| **sum** | **1948** (11+4+170+1735+22+0+0+4+0+2) |

Empty groups (`demo\yo-instant`, three Starwave `real` groups) are present. That is the proof the walk is **ALL manager-visible groups**, not “groups that have traders.”

**Caveats (do not over-claim):**

1. Probe JSON does **not** record `_pumpEnabled`. The connect *may* have succeeded on the first (pump) attempt. Completeness still comes from `GroupRequestArray` / `UserRequestArray`, which do not need the pump.
2. ALL = **manager-ACL-visible**. Groups outside these two manager records are invisible by design.
3. This slot did **not** re-run Connect. Counts are the 08:42Z artifact.

Dashboard remeasure (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` **8460**, `/api/groups` **18**. Same book.

---

## 5. Copy to cTrader must not send live orders (no loss)

### 5.1 Product host FIX session is Logon-only

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135). The only `ssl.WriteAsync` is `BuildLogon`. Sockets are disposed at method end.

```96:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
            (553, username),
            (554, password)
```

Product `src` `*.cs`/`*.json`/`*.csproj` have **0** literal `35=D` strings. Classification of the **copy hop**: **`SAFE_BY_ABSENCE`**.

Do **not** claim the entire tree has zero NewOrderSingle constructors. Separate leftover `CTraderFixDemoTestTrade.Build("D", …)` at L139 / L163 / L197 **is** a NewOrderSingle. Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (not registered in `AddTraderIntelligence`). Gate at L43–59 refuses unless host starts with `demo-`, sender starts with `demo.`, and account is **not** `1369850`. That tool is **out of the live copy pipeline**. This slot did not invoke it.

### 5.2 Copy hop cannot emit a ticket even if `REAL_COPY` is armed

Current DI **binds** the env flag (slots 80/100/120 “hard-false pin” is **stale**):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` L60–70 **does not** re-pin `RealCopyEnabled=false`. It logs `RealCopyArmed={Armed} NewOrderSingle still unimplemented`.

`/api/settings` L76 exposes `runtime.RealCopyEnabled` (not a hardcoded false). `CTraderFixOptions.RealCopyExecutionEnabled` default is still `false` (L35) and is **not** bound from env `REAL_COPY_EXECUTION_ENABLED`.

`CopyTradingService` (257 lines) is the product hop:

| Gate | Measured |
|---|---|
| `NewOrderSingleImplemented` | `const false` L16 |
| `VenueReconciled` | `const false` L15 |
| Persist `AllowFixSend` | **hardcoded `false`** L192 |
| Live-send `if` | L198 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — currently **unreachable** |
| Else | `Status = "SHADOW_ONLY"` + local `ShadowCopyEngine.SimulateEntry` |
| `ExecutionIntent` writers | **0** (`CountAsync` L38 only) |
| `CanPromoteToLive` | `=> false` (`BaselineScorer.cs` L211) |
| Hosted tick | `CopyTradingHostedService` L30: “Live NewOrderSingle still blocked.” |

`apps/fix-worker/Worker.cs` never opens a TRADE socket. It stamps `Disconnected` and logs a refuse even if `CTrader:RealCopyExecutionEnabled` is true (L21–46).

### 5.3 Manager fetch cannot open a cTrader position

`GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup` are **read** RPCs. They do not call `DealerSend`, `DealerBalance`, `OrderAdd`, or `TradeAccountSet`. Fetching 8460 traders does not place an order.

---

## 6. Residual risks (not capital)

| Risk | Severity | Note |
|---|---|---|
| `DealRequestByGroup` unpaged | Med (ops) | Entire 14-day window buffered. `DealRequestPage` unused. |
| Probe did not record `PumpEnabled` | Low (honesty) | Does not change request completeness. |
| C++ `GetAllGroups` cache-only | Med if someone uses YoPips wrapper for discovery | Prop C# path is request-complete. |
| Manager ACL ≠ server universe | By design | Two managers, two books. |
| C++ `UserLogins` fail-closed on null pointer | Low | Empty group can look like API failure. C# uses `UserRequestArray` first. |
| YoPips pool wraps `DealerSend` | Out of C# path | Do not call that binary from this copy process. |
| Env `REAL_COPY_EXECUTION_ENABLED` may be `true` | Residual (no sender) | DI now binds it. Next sender would see runtime armed. Still no ticket today. |
| `CTraderFixDemoTestTrade` | Out of host hop | Demo-gated MsgType `D`. Not invoked by ingest/copy. |
| In-memory API DB when `DATABASE_URL` is placeholder | Ops | Does not send live orders. |

---

## 7. Stale claims (do not reuse)

| Claim | Where | Current truth |
|---|---|---|
| Zero `GroupRequestArray` / `UserRequestArray` under `src` | `A001_native_connector.md` | **False now.** Both are primary in `NativeMt5BrokerConnector`. |
| Groups/traders are pump-cache only | A001 | **False now.** Request first; cache is fallback. |
| C++ `GetAllGroups` is no-pump complete | some early notes | **False.** Cache `GroupTotal`/`GroupNext` only. |
| Live census proves `pump_mode=0` | if anyone writes that | **Unproven.** JSON has no `PumpEnabled`. |
| C42 “live Manager NOT proven” | `C42_honesty_no_live_mt5.md` | **Stale vs 08:42Z probe.** Live census exists. This slot did not re-attach. |
| DI / hosted force `RealCopyEnabled=false` | W500 80/100/120, A014/A015, CREDENTIALS “forced” | **Stale.** DI L41 binds env; hosted logon no longer re-pins. Sender still unimplemented. |
| Entire tree has zero NewOrderSingle constructors | some W500 35=D slots | **Over-claim.** Demo tool `Build("D")` exists. Product **host/copy hop** still has no sender. |

---

## 8. Sources (absolute paths)

| Path | Why |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Five APIs, pump enum, Admin twins |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | `CheckGroupMask` L775–809 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | Identical signatures / version 5570 |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Request-first live walk + no-pump fallback (458/458) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog ALL groups/accounts |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two connectors; Starwave proxy forced off |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Pump remap + no-pump retry + `UserLogins` + `DealRequest` |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | Born `mode=0`; still calls `UserLogins` / `PositionRequest` / `DealRequest` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Same Connect / cache `GetAllGroups` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | Same `Connect(..., 0)` + `UserLogins` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135/135) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Demo-only leftover MsgType `D` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Env-binds `REAL_COPY`; no sender |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Does not re-pin flag; logs unimplemented |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented=false`; `AllowFixSend=false` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | SHADOW tick only |
| `D:\Prop\apps\fix-worker\Worker.cs` | No TRADE socket; refuses NewOrderSingle |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Census runner |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 18 / 8460 / 1984 at 08:42Z |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Dashboard 18 / 8460 |

---

## 9. Checklist

- [x] SDK `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup` located on `IMTManagerAPI`.
- [x] Request vs Get pairing documented; deals have no pump bit.
- [x] `IMTAdminAPI` still exposes four of five request APIs with only MAIL/NEWS pump bits — request cannot require user/group/position pump. `GroupRequestArray` is Manager-only.
- [x] Pool `Connect(..., 0)` still calls `UserLogins` / `DealRequest` / `PositionRequest`.
- [x] C# connector uses request APIs first and does not branch on `_pumpEnabled`.
- [x] ALL Achiever+Starwave groups+traders measured (18 / 8460), including empty groups (independent re-sum).
- [x] Product copy hop cannot emit `35=D`. `NewOrderSingleImplemented=false`. Persist `AllowFixSend=false`. Risk to capital **NONE**.
- [x] Residual recorded: env `REAL_COPY` may be true and is now DI-bound; demo test-trade leftover is out of host hop.
- [x] Product source not modified. Secrets not printed.

*End of W500_RESEARCH_180. Product source was not modified. No secrets printed.*
