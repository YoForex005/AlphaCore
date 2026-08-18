# W500_RESEARCH_80 — `MT5APIManager.h` request APIs work without pump

- **slot:** 80
- **date:** 2026-08-18
- **angle:** Read vendor `IMTManagerAPI` (`GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup`). Confirm those **request** calls work with `pump_mode=0`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders (no capital loss).
- **method:** `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Header `MT5APIManager.h` (`MTManagerAPIVersion 5570`, `MTManagerAPIDate L"30 Jan 2026"`). YoPips/Prop `mt5_manager.cpp` Connect + `GetUserLogins` + `GetAllGroups`. `mt5_pool.cpp` `Connect(..., 0)`. Product FIX session, DI flag, ingest, probe. **No secrets printed. This slot did not re-attach live.**
- **verdict:** **PASS_REQUEST_APIS_NO_PUMP**
- **risk_to_capital:** **NONE**

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Where are the five request APIs declared? | `IMTManagerAPI` in `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`. Same text in YoPips `...\MetaTrader5SDK\Include\MT5APIManager.h`. |
| `GroupRequestArray` | **L212.** `virtual MTAPIRES GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)=0;` |
| `UserLogins` | **L254.** `virtual MTAPIRES UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)=0;` |
| `UserRequestArray` | **L410.** `virtual MTAPIRES UserRequestArray(LPCWSTR group, IMTUserArray* users)=0;` |
| `DealRequestByGroup` | **L520.** `virtual MTAPIRES DealRequestByGroup(LPCWSTR group, const int64_t from, const int64_t to, IMTDealArray* deals)=0;` |
| `PositionRequestByGroup` | **L534.** `virtual MTAPIRES PositionRequestByGroup(LPCWSTR group, IMTPositionArray* positions)=0;` |
| Do they require pump? | **No.** Request = network pull. Pump only fills the **Get/Next/Total** cache. `Connect(..., pump_mode=0)` is a legal session (C# names it `PUMP_MODE_NONE`). |
| Does Prop C# use the request path? | **Yes, primary.** `NativeMt5BrokerConnector` L155 / L223 / L230 / L307 / L344. Connect retries `PUMP_MODE_NONE` (L101) and still calls the same methods. No `_pumpEnabled` gate on fetch. |
| ALL Achiever + Starwave groups + traders? | **Yes, last measured.** Probe `LIVE_GROUPS_AND_TRADERS.json` `utc=2026-08-18T08:42:16.8519545+00:00`: Achiever **8 / 6512 / 1506**, Starwave **10 / 1948 / 478**, total **18 / 8460 / 1984**. Path = `GroupRequestArray("*")` + per-group `UserRequestArray`. |
| Can copy send a live cTrader order? | **No.** `CTraderFixSession.BuildLogon` emits only `(35, "A")`. Product `D:\Prop\src` has **0** `35=D`. `RealCopyEnabled` forced `false` in DI L41 and again after FIX logon L68. |
| Risk to capital? | **NONE.** Manager **read** APIs + FIX Logon only. Connector has 0 hits for `DealerSend` / `OrderSend` / `TradeRequest` / `DealerBalance` / `UserAdd`. |

## 2. Vendor contract — five request signatures

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Class: `IMTManagerAPI` (starts L121). Admin twin `IMTAdminAPI` starts L785.

```211:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```252:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   virtual MTAPIRES  UserGroup(const uint64_t login,MTAPISTR& group)=0;
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

```519:535:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- deals database
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  PositionRequestByGroup(LPCWSTR group,IMTPositionArray* positions)=0;
```

`Connect` takes an independent `pump_mode` bitfield. Zero is legal:

```164:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

C++ `EnPumpModes` (L125–144) names `USERS=0x1`, `POSITIONS=0x80`, `GROUPS=0x100`, `FULL=0xffffffff`. **No** `PUMP_MODE_NONE` enumerator. **No** `PUMP_MODE_DEALS`. C# wrapper and the official Web-API sample name the zero mask `PUMP_MODE_NONE = 0x00000000` (`Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` L42; R010 reflection). Same integer.

Mask language for `GroupRequestArray` / `UserLogins` / `UserRequestArray` / `*ByGroup` is `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809): comma templates, leading `!` exclude, `*` wildcard via `CheckGroupTemplate`. Literal `"*"` = every group **this manager ACL** may see.

### 2.1 Admin vs Manager

| API | `IMTManagerAPI` | `IMTAdminAPI` |
|---|---|---|
| `GroupRequest` / `GroupRequestArray` | L208 / **L212** | **Absent.** Only `GroupTotal` / `GroupNext` / `GroupGet` (L910–912) |
| `UserLogins` | L254 | L1172 |
| `UserRequestArray` | L410 | L1173 |
| `DealRequestByGroup` | L520 | L1099 |
| `PositionRequestByGroup` | L534 | L1268 |

Stay on **Manager**. Product field is `CIMTManagerAPI? _manager`. Admin cannot enumerate groups without the config cache.

## 3. Request vs Get — why pump is optional

Vendor naming is the contract:

| Domain | Cache (needs pump bit) | Network (no pump) |
|---|---|---|
| Groups | `GroupTotal` / `GroupNext` / `GroupGet` — `PUMP_MODE_GROUPS=0x100` | `GroupRequest` / **`GroupRequestArray`** |
| Users | `UserGet` / **`UserGetByGroup` (L672)** / `UserTotal` — `PUMP_MODE_USERS=0x1` | `UserRequest` / **`UserRequestArray`** / **`UserLogins`** / `UserRequestByLogins` (L671) |
| Accounts | `UserAccountGet` / **`UserAccountGetByGroup` (L742)** | `UserAccountRequest` / `UserAccountRequestArray` (L411) |
| Positions | `PositionGet` / **`PositionGetByGroup` (L286)** — `PUMP_MODE_POSITIONS=0x80` | `PositionRequest` / **`PositionRequestByGroup`** |
| Deals | **None.** `grep DealGet` on `MT5APIManager.h` = **0**. No `PUMP_MODE_DEALS`. | `DealRequest` (L269–270) / **`DealRequestByGroup`** / `DealRequestPage` |

`Get*` reads the in-process pump cache. After `Connect(..., 0)` that cache is empty. `Request*` always hits the trade server. Using `UserGetByGroup` / `PositionGetByGroup` / `GroupNext` alone on a no-pump session silently returns **zero** rows. That is why the product walk is Request-first.

## 4. Six independent proofs that Request works at `pump_mode=0`

### 4.1 Official header: deals have no cache API

`IMTManagerAPI` never declares `DealGet`. Deals cannot be pumped. `DealRequest` / `DealRequestByGroup` are the **only** manager fetch. If they required pump they would be unusable on every Manager session.

### 4.2 YoPips / Prop `MT5Manager::Connect` comments (identical files)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L114–134  
`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L114–134 (byte-same block)

```118:134:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump; this lets journal
        // sync and other request-only operations function even when the pump
        // connection is unavailable (IP not yet whitelisted for pump, etc.)
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        ...
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

Same file, `GetAccount` L339–344: `UserAccountGet` is “in-memory pump cache”; fallback is `UserAccountRequest` “when the cache misses (**no pump**, or login not in the synchronized scope)”.  
`GetPositions` L403–408: `PositionGet` then `PositionRequest`.  
`GetOrders` L440–443: if `!m_pumpMode` go **straight** to `OrderRequestOpen`.  
`GetDeals` L492–493: **only** `DealRequest` — no cache attempt.  
`CacheExecutedDeal` L539–540: “there is **NO PUMP_MODE_DEALS** in the MT5 SDK”.

### 4.3 Pool sessions are request-only by design

`D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` L75–77:

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

Same session still calls:

| Call | Line | Notes |
|---|---|---|
| `UserLogins` | L218 | assigned request enumerator |
| `UserRequest` | L143 | per-login user |
| `UserAccountRequest` | L241 | after cache miss |
| `PositionRequest` | L301 | after empty `PositionGet` (comment L293–297: pool cache is normally empty) |
| `DealRequest` | L564 | history |

YoPips `mt5_pool.cpp` is the same recipe (`Connect` L76 `mode=0`; `UserLogins` L217).

### 4.4 C# connector retries `PUMP_MODE_NONE` and keeps the same `_manager`

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

`Ensure()` (L436–439) only checks `_connected`. There is **no** `if (!_pumpEnabled) return`. Fetch is request-complete after fallback.

### 4.5 Wrapper remaps wrapper-`0` then still falls back to SDK-`0`

YoPips `Connect` L102–108: if the **caller** passes `pumpMode==0`, the wrapper **rewrites** it to `USERS|ORDERS|POSITIONS|SYMBOLS` (omits `PUMP_MODE_GROUPS`). If that pumped connect fails, it retries the SDK with literal `0`. So:

- Wrapper argument `0` ≠ SDK `0`.
- The **fallback** is the real no-pump session.
- `GetAllGroups` after that fallback is cache-only (`GroupTotal`/`GroupNext`, L962–981) and may return `[]`. Completeness without pump is `GroupRequestArray("*")`, which **this C++ wrapper never calls** (`grep` of YoPips `src` = **0** for `GroupRequestArray` / `UserRequestArray` / `PositionRequestByGroup` / `DealRequestByGroup`).
- `GetUserLogins` **does** call SDK `UserLogins` (L322). That path is request-complete even when `GetAllGroups` is empty. `GetGroupLogins` is an alias (L1015–1016).

Prop C# does **not** inherit that groups-cache gap.

### 4.6 Live census used the request walk

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L24–29) calls `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")` on the same `NativeMt5BrokerConnector`. Artifact `LIVE_GROUPS_AND_TRADERS.json` recorded **non-zero** groups **and** empty groups (`demo\yo-instant` = 0, three Starwave `real` grps = 0). A cache-only walk after a cold no-pump session cannot produce that. The JSON does **not** record `PumpEnabled`; the **fetch methods** are still Request-first, so the census is valid whether pump succeeded or the NONE fallback ran.

## 5. Product wiring — ALL groups / ALL traders

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).  
Implements `IMt5BrokerConnector` + `IMt5BulkDealReader` + `IMt5BulkPositionReader`.

### 5.1 Groups — `GroupRequestArray("*")` first

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
                ...
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
```

- Mask is the literal `"*"`, not `MT5_DEFAULT_GROUP`, not `contest\*`, not a plan map.
- Loop bound is `arr.Total()`. **No `Take`.**
- Cache `GroupNext` runs **only if** the request list is empty.

### 5.2 Traders — `UserRequestArray` then `UserLogins`

`GetAccountsAsync(null)` → `GetAccountsCore` walks **every** name from `GetGroupsCore` (L201–202). Per group:

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
            if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
                _manager.UserAccountGetByGroup(gname, accounts);
```

Order is **request → cache → request**. `UserGetByGroup` is a fallback, not the enumerator. Using it alone on `PUMP_MODE_NONE` would silently return **zero** traders.

`UserLogins` with mask `"*"` is also a legal one-shot “all logins this manager may see” (header L254; YoPips `GetUserLogins(L"*")`). Product currently fans out per group name instead. Completeness is the same if the group list is complete.

### 5.3 Deals / positions — group request

| Method | Call | Lines |
|---|---|---|
| `GetDealsCore` | `DealRequest(login, from, to, arr)` | L284 |
| `GetGroupDealsCore` | **`DealRequestByGroup(group, from, to, arr)`** | L307 |
| `GetPositionsCore` | `PositionRequest(login, arr)` | L327 |
| `GetGroupPositionsCore` | **`PositionRequestByGroup(mask, arr)`** then cache `PositionGetByGroup` | L344–346 |

Deal windows are 14-day slices (`Windows` L355–366). Ingest uses the bulk path:

- `DealIngestionService.SyncCatalogAsync` L45–49: `GetGroupsAsync` + `GetAccountsAsync(null)`.
- `SyncBrokerAsync` L65–85: per-group `GetGroupDealsAsync` + `GetGroupPositionsAsync("*")`.

`LiveIngestHostedService` runs that catalog for **every** registered connector (exactly two: Achiever + Starwave). Fail-closed: no dummy substitute (`L70`).

### 5.4 Both brokers are this type

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L23–49):

| Broker | Connector | Proxy |
|---|---|---|
| `BrokerCodes.Achiever` | `NativeMt5BrokerConnector` | optional HTTP `host:port` / `user:pass` when `ACHIEVER_PROXY_ENABLED` |
| `BrokerCodes.StarwaveFx` | `NativeMt5BrokerConnector` | **`ProxyEnabled = false` hardcoded** (L45). Env `MT5_STARWAVEFX_PROXY_*` unread. |

DI throws unless both password keys pass `IsSecret` (`DependencyInjection.cs` L35–36). Fake connector is not registered on the live host.

`grep` of the five symbols under `D:\Prop\src` (product, not vendor): **only** `NativeMt5BrokerConnector.cs` (5 call sites listed in §1).

## 6. Live census (not re-run this slot)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, note `"Passwords never written. Groups and manager logins only."`  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md`. Path recorded: `GroupRequestArray` + `UserRequestArray`.

| Broker | Connect | Groups | Traders | Open pos | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | `connected: true` (HTTP proxy) | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | `connected: true` (direct) | 10 | 1948 | 478 | (same artifact) |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (re-sum 2+179+4+5+4+6295+0+23 = **6512**):

`contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` **0**, `demo\yo-payp` 23.

Starwave groups (re-sum 11+4+170+1735+22+0+0+4+0+2 = **1948**):

`Starwave\cent\FX1\grp1` 11, `grp2` 4, `Starwave\demo\FX2\grp1` 170, `grp2` 1735, `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `Starwave\real\FX3\LP` 2.

Zero-account groups are still listed — evidence the walk is **group-first**, not “skip empty.”  
**ALL = manager-ACL-visible.** Groups outside those two manager records are invisible by design. Do not invent a plan-map filter.

This slot did **not** print logins, passwords, proxy auth, or FIX passwords.

## 7. Copy to cTrader must not send live orders

| Check | Measured |
|---|---|
| `CTraderFixSession.BuildLogon` | Only outbound MsgType is `(35, "A")` (L96). One `WriteAsync` (L49). Sockets disposed. |
| `35=D` / `(35, "D")` under `D:\Prop\src` | **0** |
| `35=D` under `D:\Prop\apps` | **0** |
| `NewOrderSingle` in `Fix.CTrader` | XML comment (`CTraderFixOptions` L33) + log line (`CTraderFixLogonHostedService` L70). **Not a builder.** |
| `RealCopyExecutionEnabled` POCO default | `false` (`CTraderFixOptions.cs` L35) |
| Product `RealCopyEnabled` assignments | DI L41 `= false`; FIX hosted service L68 `= false` again after logon |
| `LiveRuntimeStatus.Snapshot` when false | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| `apps/fix-worker/Worker.cs` | Even if config flag is true: stamps TRADE `Disconnected`, logs refuse, **no send function** (L36–46) |
| Shadow | `ShadowCopyEngine` is in-memory simulate only. No socket. |
| Manager writes from C# connector | **0** `DealerSend` / `DealerBalance` / `OrderSend` / `OrderAdd` / `TradeRequest` / `UserAdd` / `UserUpdate` |

`SAFE_BY_ABSENCE` for `35=D`. Logon `35=A` may go out (QUOTE 5211 / TRADE 5212, `56=cServer`). That cannot place an order.

## 8. Residuals (honest, not FAIL)

1. **This slot did not re-attach.** Census is the 08:42Z probe, not a new Manager session.
2. **Probe JSON does not record `PumpEnabled`.** Completeness argument is the Request-first code path, not a measured pump-none bit on that run.
3. **YoPips `GetAllGroups` is still cache-only.** Do not use `mt5_group_probe` as the ALL-groups collector. Prop C# already has the correct enumerator.
4. **C++ `GetUserLogins` returns `false` when `raw_logins` is null** (L323) even if `total==0`. An empty group can look like an API failure. C# checks `Length: > 0` and continues.
5. **`DealRequestByGroup` is not paged** in C# (14-day windows only). A covering group over 90 days can be large. That is a memory/timeout risk, not a capital risk.
6. **Architecture §68 / §70 stay FAIL.** Absence of a sender is not a passed go-live review. Do not flip `RealCopyExecutionEnabled`.

Stale sibling: `A001_native_connector.md` said `src` had **zero** `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup`. **Wrong for the current 458-line file.**

## 9. Recipe (collector, no live send)

1. `ProxySet` Achiever HTTP `address=IP:port` `auth=login:password` **before** Connect. Starwave: do not `ProxySet`.
2. `Connect(..., PUMP_MODE_GROUPS|USERS|POSITIONS, 30000)`. On any fail: `Connect(..., 0 / PUMP_MODE_NONE, 30000)`.
3. Groups: `GroupRequestArray(L"*")`. Cache `GroupNext` only if empty.
4. Traders: for each name (or mask `*`), `UserRequestArray` + `UserAccountRequestArray`. If users empty: `UserLogins` then `UserRequestByLogins`.
5. Positions census: `PositionRequestByGroup("*")`.
6. Deals: `DealRequestByGroup(name, from, to)` in 14-day slices (or per-login `DealRequest`).
7. Persist `{broker, group names, login ints, counts}`. Never persist manager / proxy / FIX passwords.
8. **Do not** emit FIX `35=D`. Keep `RealCopyEnabled=false`.

## 10. Checklist

- [x] Five request APIs located on `IMTManagerAPI` (L212 / L254 / L410 / L520 / L534)
- [x] Confirmed Request ≠ Get; pump bits documented; no `PUMP_MODE_DEALS`; no `DealGet` on Manager
- [x] YoPips/Prop comments + pool `mode=0` + C# NONE fallback independently prove Request works without pump
- [x] Prop C# primary walk is those five APIs; ingest/`LiveBrokerProbe` use `null` / `"*"`
- [x] Prior live census 18 / 8460 / 1984 via this path (not re-run)
- [x] `35=D` absent; `RealCopyEnabled` forced false; connector has no manager write
- [x] No secrets printed
- [x] Product source not edited

**One-liner:** The five Manager **Request** APIs are network pulls that stay valid at `pump_mode=0`; Prop C# already uses them as the ALL-groups / ALL-traders walk (measured 18/8460); copy cannot lose capital because there is no `35=D` builder.
