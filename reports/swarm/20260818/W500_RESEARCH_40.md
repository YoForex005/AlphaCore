# W500_RESEARCH_40 — Request APIs work without pump (`GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`)

- **slot:** 40
- **date:** 2026-08-18
- **angle:** Confirm `IMTManagerAPI` **Request** methods fetch ALL Achiever + Starwave groups and ALL manager-visible traders **without** pump cache. Copy-to-cTrader must not send live orders (no capital loss).
- **method:** `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of vendor `MT5APIManager.h` signatures, C++ `MT5Manager`/`MT5Session` connect+request paths, C# `NativeMt5BrokerConnector` (458 lines), ingest/FIX gates. No secrets printed. This slot did **not** re-run live Connect; live counts come from the existing same-day probe artifact.
- **verdict:** **PASS** — Request APIs are server RPCs. They do not require `pump_mode != 0`. Prop C# uses them as the primary ALL enumerator. Live send is off.

## 1. Question and answer

| Question | Measured answer |
|---|---|
| Do `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup` exist on Manager API? | **Yes.** `IMTManagerAPI` in `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (API **5570**, `30 Jan 2026`). Same five signatures, same line numbers, in YoPips `...\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`. |
| Do they require pump? | **No.** They are **network Request** methods. Pump bits only populate the local **Get/Total/Next** cache and sinks. `Connect(..., pump_mode=0)` is a documented/used session mode. |
| Can ALL manager-visible groups be listed without `PUMP_MODE_GROUPS`? | **Yes**, via `GroupRequestArray(L"*")`. Cache `GroupTotal`/`GroupNext` is **not** complete after a no-pump connect. |
| Can ALL manager-visible traders be listed without `PUMP_MODE_USERS`? | **Yes**, via `UserRequestArray(group)` and/or `UserLogins(group)` (+ optional `UserRequestByLogins`). Cache `UserGetByGroup` is empty without the user pump. |
| Can open positions / deal history be read without pump? | **Yes.** `PositionRequest` / `PositionRequestByGroup` and `DealRequest` / `DealRequestByGroup` are network. `PositionGet` / `PositionGetByGroup` are cache-only. There is no historical `DealGet`. |
| Does Prop C# use the request path? | **Yes.** `NativeMt5BrokerConnector` primary: `GroupRequestArray("*")`, per-group `UserRequestArray`, `UserLogins` fallback, `DealRequestByGroup`, `PositionRequestByGroup`. Connect retries `PUMP_MODE_NONE` and still calls those methods. |
| Does YoPips C++ wrapper use `GroupRequestArray`? | **No.** `GetAllGroups` is cache `GroupTotal`/`GroupNext` only. Pool sessions connect `mode=0`. That wrapper is **not** no-pump-complete for groups. |
| Live ALL census (prior same-day probe)? | Achiever **8 / 6512 / 1506**; Starwave **10 / 1948 / 478**; total **18 groups / 8460 traders / 1984 positions**. |
| Does copy-to-cTrader send live orders? | **No.** FIX session builds only `35=A` Logon. No `35=D`. `RealCopyEnabled` forced `false`. Manager fetch has no `DealerSend` / `DealerBalance` / `OrderAdd`. |

## 2. Vendor contract — five Request APIs

Header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Class: `IMTManagerAPI` starts L121. Version pins L11–12:

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

Connect takes an independent `pump_mode` bitfield. Zero is a legal value (C# names it `PUMP_MODE_NONE`; C++ header has no NONE enumerator — `0` is just no bits):

```124:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
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
...
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

There is **no** header constraint that Request methods require a matching pump bit. Pump enumerators describe **what is pushed into the local cache / sinks**, not which RPCs are legal.

### 2.1 Exact signatures (Manager)

| API | Header line | Signature | Role |
|---|---:|---|---|
| `GroupRequestArray` | 212 | `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` | Network wildcard group-config snapshot |
| `UserLogins` | 254 | `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` | Network login list; caller `Free()`s the buffer |
| `UserRequestArray` | 410 | `UserRequestArray(LPCWSTR group, IMTUserArray* users)` | Network full `IMTUser` records by group mask |
| `DealRequestByGroup` | 520 | `DealRequestByGroup(LPCWSTR group, int64_t from, int64_t to, IMTDealArray*)` | Network deal history window for a group mask |
| `PositionRequestByGroup` | 534 | `PositionRequestByGroup(LPCWSTR group, IMTPositionArray*)` | Network open-position book for a group mask |

```198:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroup* GroupCreate(void)=0;
   ...
   virtual uint32_t  GroupTotal(void)=0;
   virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupGet(LPCWSTR name,IMTConGroup* group)=0;
   ...
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```250:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  UserTotal(void)=0;
   virtual MTAPIRES  UserGet(const uint64_t login,IMTUser* user)=0;
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

```519:536:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  PositionRequestByGroup(LPCWSTR group,IMTPositionArray* positions)=0;
   virtual MTAPIRES  PositionRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTPositionArray* positions)=0;
```

Related request siblings used by the C# connector (also no-pump):

| API | Line | Use |
|---|---:|---|
| `GroupRequest` | 208 | Single group, network |
| `UserRequest` | 252 | Single login, network |
| `UserRequestByLogins` | 671 | Batch users by login list, network |
| `UserAccountRequest` | 261 | Single account snapshot, network |
| `UserAccountRequestArray` | 411 | Group account snapshots, network |
| `DealRequest(login, from, to)` | 270 | Per-login history, network |
| `PositionRequest(login)` | 282 | Per-login positions, network |

### 2.2 Get vs Request (why pump is optional)

Vendor naming is consistent: **Get/Total/Next = local cache; Request = server RPC**.

| Domain | Cache (needs matching pump bit) | Request (works at `pump_mode=0`) |
|---|---|---|
| Groups | `GroupTotal` / `GroupNext` / `GroupGet` (`PUMP_MODE_GROUPS=0x100`) | `GroupRequest` / `GroupRequestArray` |
| Users | `UserTotal` / `UserGet` / `UserGetByGroup` (L672) / `UserGetByLogins` (`PUMP_MODE_USERS=0x1`) | `UserRequest` / `UserRequestArray` / `UserLogins` / `UserRequestByLogins` |
| Accounts | `UserAccountGet` / `UserAccountGetByGroup` (L742) | `UserAccountRequest` / `UserAccountRequestArray` / `UserAccountRequestByLogins` |
| Positions | `PositionGet` / `PositionGetByGroup` (L286) (`PUMP_MODE_POSITIONS=0x80`) | `PositionRequest` / `PositionRequestByGroup` / `PositionRequestByLogins` |
| Orders | `OrderGet` / `OrderGetOpen` / `OrderGetByGroup` (`PUMP_MODE_ORDERS=0x8`) | `OrderRequest` / `OrderRequestOpen` / `OrderRequestByGroup` |
| Deals | `DealSubscribe` sink only (no historical `DealGet`) | `DealRequest` / `DealRequestByGroup` / `DealRequestByLogins` / `DealRequestPage` |

`DealRequest*` never had a cache twin. History is always a network pull. That is why C++ `MT5Manager::Connect` comments (L118–134) say **GetDeals / DealRequest works without the pump** and why no-pump fallback still enables journal sync.

### 2.3 Admin API is not the group-request surface

`IMTAdminAPI` starts L785. Its `EnPumpModes` only lists MAIL/NEWS/FULL (L789–795). Group config on Admin is cache+mutate only:

```899:916:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients group configuration
   ...
   virtual uint32_t  GroupTotal(void)=0;
   virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupGet(LPCWSTR name,IMTConGroup* group)=0;
   ...
   virtual MTAPIRES  GroupReserved3(void)=0;
```

**No** `GroupRequest` / `GroupRequestArray` on Admin.  
Admin **does** expose the other four request APIs later: `DealRequestByGroup` L1099, `UserLogins` L1172, `UserRequestArray` L1173, `PositionRequestByGroup` L1268.

Prop C# uses `CIMTManagerAPI` (Manager), not Admin. Group completeness without pump is therefore **Manager `GroupRequestArray` only**.

### 2.4 Mask language (`*` = ALL this manager may see)

`GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup` all take a group mask. `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809):

- comma-separated templates
- leading `!` = exclude
- `*` wildcards via `CheckGroupTemplate`

Mask `"*"` means **every group this manager ACL is allowed to see**, not every group on the server. That is the product definition of ALL.

## 3. Independent C++ proof that Request works at `pump_mode=0`

YoPips / Prop C++ pool is a **request-only** session. Comment and call are explicit:

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

On that same no-pump session the pool **does** call:

- `UserRequest` (L143, L184) — “network, not cache”
- `UserLogins` (L218)
- `UserAccountRequest` after empty `UserAccountGet` (L235–238: “Pool sessions connect mode=0/no-pump so this cache is normally empty”)
- `PositionRequest` after empty `PositionGet` (L293–301: cache empty on mode=0 **must** fall back or open positions would wrongly return empty)
- `OrderRequestOpen` after empty `OrderGetOpen` (L329–334: “Pool sessions connect WITHOUT pump mode, so that cache is never populated”)

`MT5Manager::Connect` prefers a pump mask, then **retries `pump_mode=0`** and keeps the session:

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
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

`GetDeals` always uses `DealRequest` (network), never a pump cache (`mt5_manager.cpp` L492–493).

`GetUserLogins` on both `MT5Manager` and `MT5Session` is a direct `UserLogins` RPC (`mt5_manager.cpp` L315–327; `mt5_pool.cpp` L212–223) plus `Free(raw_logins)`. It is used on the no-pump pool session.

**Gap in the C++ wrapper (do not greenwash):** `GetAllGroups` / `GetGroupDetails` still walk **cache only**:

```962:981:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
    uint32_t total = m_manager->GroupTotal();
    ...
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
```

Same in YoPips `mt5_pool.cpp` L663–681. **Zero** `GroupRequestArray` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup` hits under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`. After a no-pump connect, C++ `GetAllGroups` can return `success + []` even when the manager owns groups. Completeness without pump is **not** the C++ wrapper; it is the C# connector (and any future C++ call to `GroupRequestArray("*")`).

## 4. Prop C# live path — Request first, pump optional

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

### 4.1 Connect: try pump, keep Request session if pump fails

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                ...
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            ...
            _connected = true;
            _pumpEnabled = false;
```

`PUMP_MODE_NONE = 0` (R010 reflection). After this fallback `_pumpEnabled` is false and **the same** `_manager` is used for every Request call below. There is no “bail if !PumpEnabled” gate.

Achiever applies HTTP `ProxySet` (`address=host:port`, `auth=user:pass`, `PROXY_HTTP`) **before** Connect (L115–129). Starwave `ProxyEnabled` is hardcoded false (`LiveMt5Registration.cs` L45). Passwords not printed.

### 4.2 Groups — `GroupRequestArray("*")` first

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
                        if (g is null) continue;
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

This is the A39 no-pump complete enumerator. Cache `GroupNext` runs **only if** the request list is empty (pump-hot cache salvage, or empty ACL).

### 4.3 Traders — `UserRequestArray` then `UserLogins`

`GetAccountsAsync(null)` walks **every** name from `GetGroupsCore` (L189–214). Per group (`ReadAccountsForGroup` L216–271):

1. `UserRequestArray(gname, users)` — network full records
2. On hard fail (not OK / OK_NONE / NOTFOUND): `UserGetByGroup` (cache; useful only if user pump is up)
3. If `users.Total()==0`: `UserLogins(gname, out loginRes)` then `UserRequestByLogins` (both network)
4. `UserAccountRequestArray` then cache `UserAccountGetByGroup`

```223:232:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

No `Take(200)`. No plan-map filter. No dummy substitution on this type.

### 4.4 Deals / positions — group Request

```307:308:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
```

Windows are 14-day slices (`Windows` L355–366). Per-login path is `DealRequest` (L284).

```344:347:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.PositionRequestByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    res = _manager.PositionGetByGroup(mask, arr);
```

Ingest asks `GetGroupPositionsAsync("*")` (`DealIngestionService.cs` L82–85). Request first; cache `PositionGetByGroup` only if Request hard-fails.

### 4.5 Ingest asks for ALL

```38:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` iterates `registry.All()` (Achiever + Starwave from `LiveMt5Registration.CreateConnectors`). Dummy/fake is refused when passwords are missing (`DependencyInjection.cs` L35–36).

Stale report `A001_native_connector.md` (“zero `GroupRequestArray` hits under `src`”) is **wrong for the current file**. Current source has all five Request calls.

## 5. Measured ALL census (same-day artifact — this slot did not re-attach)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs`  
It calls the **same** connector: `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. No passwords written.

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`utc=2026-08-18T08:42:16.8519545+00:00`, `probe=LiveBrokerProbe`.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK, 7213 ms, HTTP proxy | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK, direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | manager ACL |

Achiever group names (counts only): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave group names (counts only): `Starwave\cent\FX1\grp1` 11, `...\grp2` 4, `Starwave\demo\FX2\grp1` 170, `...\grp2` 1735, `Starwave\real\FX3\grp1` 22, `...\grp2` 0, `...\grp3` 0, `...\grp4` 4, `...\grp5` 0, `...\LP` 2.

Zero-account groups are still **listed**. That is evidence `GroupRequestArray("*")` is not “groups that happen to have users in the pump cache.”

**ALL = manager-visible.** Groups outside these two manager ACLs are invisible by design. Do not claim “every group on the broker server.”

## 6. Copy to cTrader — no live orders, no loss

| Gate | Evidence |
|---|---|
| FIX outbound is Logon only | `CTraderFixSession.BuildLogon` field 35=`A` (`CTraderFixSession.cs` L96). No `35=D` anywhere under `D:\Prop\src\Fix.CTrader`. |
| NewOrderSingle not implemented | `CTraderFixOptions.RealCopyExecutionEnabled` default **false** (L35). DI comment: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” `RealCopyEnabled = false` (`DependencyInjection.cs` L40–41). Forced false again after FIX logon (`CTraderFixLogonHostedService.cs` L68–70). |
| Runtime banner | `LiveRuntimeStatus.Snapshot` copyNote: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” (L42–43) |
| Manager fetch is read-only | `grep` `DealerSend\|DealerBalance\|OrderAdd\|TradeRequest` in `NativeMt5BrokerConnector.cs`: **0 hits**. Reads only. |
| Shadow is simulated | `ShadowCopyEngine.SimulateEntry` / `SimulateExit` — in-memory fill math, no socket. |
| Hosted FIX | `TryLogonAsync` writes Logon, reads reply, returns. No order sender. |

Live send stays off until a later increment implements `35=D` **and** an explicit go-live flag. That increment is out of this slot.

## 7. Honesty / residual risk

1. **This slot did not re-run live Connect.** Counts are the 2026-08-18 08:42 UTC probe. If the manager ACL or server population changed after that, this file does not re-measure it.
2. **Partial `GroupRequestArray` is treated as complete.** Cache fallback runs only when `list.Count==0`. A truncated non-empty request is not compared to `GroupTotal()`.
3. **Partial `UserRequestArray` is treated as complete.** `UserLogins` recovery runs only when `users.Total()==0`.
4. **`DealRequestByGroup` is not paged** in the C# wrapper (SDK has `DealRequestPage` at header L526). Huge groups + long windows can hit manager/memory limits. Windows are 14 days; ingest uses −90 d.
5. **C++ YoPips `GetAllGroups` is still cache-only.** Do not use that wrapper as the no-pump ALL-groups proof. Use C# `GroupRequestArray` or add the same call to C++.
6. **Pump is still useful** for live sinks (`OnDealAdd`, ticks) and for sub-ms `Get*` cache. This slot only claims **census/history Request APIs do not depend on it**.
7. **Not 95% copy-trading.** Not EX5 decompile. Not live execution.

## 8. Verdict

**PASS.** Vendor `IMTManagerAPI` Request APIs (`GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup`) are network RPCs. They work on `Connect(..., pump_mode=0)`. Prop C# uses them as the primary ALL enumerator for Achiever (proxy) and Starwave (direct). Same-day live probe: **18 groups / 8460 manager traders**. Copy-to-cTrader cannot place: **no `35=D`, `RealCopyEnabled=false`**. Risk to capital from this path: **none**.
