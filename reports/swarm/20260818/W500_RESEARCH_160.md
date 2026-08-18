# W500_RESEARCH_160 — `MT5APIManager.h` request APIs work without pump

| Field | Value |
|---|---|
| Slot | **160** |
| Agent | W500_RESEARCH_160 |
| Date | 2026-08-18 |
| Topic | Read `MT5APIManager.h` `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup`. Confirm those **request** APIs work **without pump**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders (no capital loss). |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** Flag booleans, group names, counts, retcodes only. |
| Live attach this slot | **No.** Census re-summed from existing probe JSON. |
| Method | Fresh `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Header pin `MTManagerAPIVersion 5570` / `30 Jan 2026`. YoPips + Prop `mt5_manager.cpp` Connect / `GetUserLogins` / `GetAllGroups`. `mt5_pool.cpp` `Connect(..., 0)`. Ingest, FIX, DI, demo-test sender, probe JSON. |
| Same-angle siblings | `W500_RESEARCH_0.md`, `20`, `40`, `60`, `80`, `100`, `120`, `140`. This slot **re-reads the current trees**. `A001_native_connector.md` (“zero `GroupRequestArray` under `src`”) is **stale**. Slots 80/100/120 “DI/hosted force `RealCopyEnabled=false`” is **stale vs current DI L41**. Slot 140 “product `*.cs` have zero `35=D`” is **stale** — see §7.2. |

**Honesty rule:** `*Request*` / `*RequestArray*` / `UserLogins` are **network RPCs**. `*Get*` / `*Total*` / `*Next*` / `*GetByGroup*` are **pump-cache**. A live census that used `GroupRequestArray` does **not** prove that session had `pump_mode=0` unless `PumpEnabled` was recorded. The probe JSON does **not** record `PumpEnabled`. Do not claim C++ `GetAllGroups` is no-pump-complete — it is cache-only.

---

## 0. Verdict (binding)

| Claim | Result | Class |
|---|---|---|
| The five assigned APIs exist on `IMTManagerAPI` | **Yes** | header lines **212 / 254 / 410 / 520 / 534** (`MTManagerAPIVersion 5570`, 30 Jan 2026). YoPips header same text + same version. |
| Those five are **request** (network), not pump-cache | **Yes** | paired against `GroupGet` / `UserGet` / `UserGetByGroup` / `PositionGet` / `PositionGetByGroup`. Deals have **no** `DealGet` and **no** `PUMP_MODE_DEALS` (0 hits in this header). |
| They work with `Connect(..., pump_mode=0)` | **Yes (SDK + product comments + pool)** | `IMTAdminAPI` pump bits are MAIL/NEWS only yet still expose `UserLogins` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`. YoPips/Prop C++ fallback + pool connect with literal `0` and still call `UserLogins` / `DealRequest` / `PositionRequest`. |
| C# live path uses them first | **Yes** | `NativeMt5BrokerConnector`: `GroupRequestArray("*")` L155, `UserRequestArray` L223, `UserLogins` L230 fallback, `DealRequestByGroup` L307, `PositionRequestByGroup` L344. **`_pumpEnabled` is write-only — never a fetch gate** (5 hits, all writes/getter). |
| ALL Achiever + Starwave groups + manager traders fetched | **Yes (measured 2026-08-18T08:42:16Z; not re-probed)** | Achiever **8 / 6512 / 1506**; Starwave **10 / 1948 / 478**; total **18 groups / 8460 traders / 1984 open pos**. Independent re-sum of JSON `accounts` fields this slot. Includes empty groups. |
| Copy to cTrader can place a live order from the **product hosted path** | **No** | Hosted `CTraderFixSession.BuildLogon` is `(35, "A")` only. `CopyTradingService.NewOrderSingleImplemented = false`. Persist writes `AllowFixSend = false`. **0** `ExecutionIntent` writers. `CanPromoteToLive => false`. Residual: `CTraderFixDemoTestTrade` can emit `35=D` under a **demo-host gate**; it is **not** called from API/workers/DI. |
| Risk to capital from fetch + product copy path | **None** | Manager **read** request RPCs + FIX Logon only. Classification **`SAFE_BY_ABSENCE`** on the live hop. Residual: `.env` `REAL_COPY_EXECUTION_ENABLED=true` is now **bound** by DI (not forced false). Flag does not create a product sender. |

One-line:

```text
Five Request APIs are network RPCs (pump optional). C# already uses them to list ALL 18 groups / 8460 manager traders. Product copy cannot emit 35=D (SAFE_BY_ABSENCE). Env REAL_COPY=true is armed but unused.
```

---

## 1. Header inventory (`IMTManagerAPI`)

Vendored file (identical signatures + same version in YoPips):

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`

Pin (both trees L11–12):

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

Class `IMTManagerAPI` starts L121. Admin twin `IMTAdminAPI` starts L785.

### 1.1 The five assigned signatures

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

```519:534:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- deals database
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  PositionRequestByGroup(LPCWSTR group,IMTPositionArray* positions)=0;
```

`Connect` takes an independent `pump_mode` bitfield. Zero is legal:

```164:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

C++ `EnPumpModes` (L125–144): `USERS=0x00000001`, `ORDERS=0x00000008`, `POSITIONS=0x00000080`, `GROUPS=0x00000100`, `SYMBOLS=0x00000200`, `FULL=0xffffffff`. **No** `PUMP_MODE_NONE` name. **No** `PUMP_MODE_DEALS` (grep of this header = **0**). C# Manager wrapper and official Web-API sample name the zero mask `PUMP_MODE_NONE = 0x00000000` (`Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` L42). Product C# uses that name at connector L101.

Mask language for `GroupRequestArray` / `UserLogins` / `UserRequestArray` / `*ByGroup` is `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809): comma templates, leading `!` exclude, `*` via `CheckGroupTemplate`. Literal `"*"` = every group **this manager ACL** may see.

Related request siblings (same contract, not the assigned five): `UserRequestByLogins` L671, `UserAccountRequestArray` L411, `PositionRequest` L282, `DealRequest` L269–270, `DealRequestPage` L526, `PositionRequestByGroupSymbol` L543.

### 1.2 Admin vs Manager

| API | `IMTManagerAPI` | `IMTAdminAPI` |
|---|---|---|
| Pump enum | Full `EnPumpModes` (USERS/GROUPS/POSITIONS/…) L125–144 | **MAIL + NEWS only** L789–795 |
| `GroupRequest` / `GroupRequestArray` | L208 / **L212** | **Absent.** Only `GroupTotal` / `GroupNext` / `GroupGet` (L910–912) |
| `UserLogins` | L254 | L1172 |
| `UserRequestArray` | L410 | L1173 |
| `DealRequestByGroup` | L520 | L1099 |
| `PositionRequestByGroup` | L534 | L1268 |

Admin still exposes **four of the five** request APIs while its pump enum **cannot** fill a user/group/position cache. Independent header proof that **Request ≠ pump**. Stay on **Manager**. Product field is `CIMTManagerAPI? _manager`.

---

## 2. Request vs Get — why pump is optional

Vendor naming is the contract:

| Domain | Cache (needs pump bit) | Network (no pump) |
|---|---|---|
| Groups | `GroupTotal` / `GroupNext` / `GroupGet` — `PUMP_MODE_GROUPS=0x100` | `GroupRequest` / **`GroupRequestArray`** |
| Users | `UserGet` / **`UserGetByGroup` (L672)** / `UserTotal` — `PUMP_MODE_USERS=0x1` | `UserRequest` / **`UserRequestArray`** / **`UserLogins`** / `UserRequestByLogins` (L671) |
| Accounts | `UserAccountGet` / **`UserAccountGetByGroup` (L742)** | `UserAccountRequest` / `UserAccountRequestArray` (L411) |
| Positions | `PositionGet` / **`PositionGetByGroup` (L286)** — `PUMP_MODE_POSITIONS=0x80` | `PositionRequest` / **`PositionRequestByGroup`** |
| Deals | **None.** `grep DealGet` on `MT5APIManager.h` = **0**. No `PUMP_MODE_DEALS`. | `DealRequest` (L269–270) / **`DealRequestByGroup`** / `DealRequestPage` |

`Get*` reads the in-process pump cache. After `Connect(..., 0)` that cache is empty. `Request*` always hits the trade server. Using `UserGetByGroup` / `PositionGetByGroup` / `GroupNext` alone on a no-pump session silently returns **zero** rows. That is why the product walk is Request-first.

---

## 3. Six independent proofs that Request works at `pump_mode=0`

### 3.1 Official header: deals have no cache API

`IMTManagerAPI` never declares `DealGet`. Deals cannot be pumped. `DealRequest` / `DealRequestByGroup` are the **only** manager fetch. If they required pump they would be unusable on every Manager session.

### 3.2 YoPips / Prop `MT5Manager::Connect` comments (identical files)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` L114–134  
`D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L114–134 (same block)

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

### 3.3 Pool sessions are request-only by design

`D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` L75–77 (YoPips twin L74–76):

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(server.c_str(), login, password.c_str(), L"",
                                       0, timeoutMs); // mode=0 means no pump
```

Same Prop session still calls:

| Call | Line | Notes |
|---|---|---|
| `UserLogins` | L218 | assigned request enumerator |
| `UserAccountRequest` | L241 | after cache miss |
| `DealRequest` | L493 (manager) / session twin | history |

If `UserLogins` required pump, the entire YoPips/Prop pool would be dead on every journal/account read. It is not designed that way.

### 3.4 C# connector retries `PUMP_MODE_NONE` and keeps the same `_manager`

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

First pump mask = `0x100 | 0x1 | 0x80` = **`0x181` (385)**. `Ensure()` (L436–439) only checks `_connected`. Grep of `_pumpEnabled` / `PumpEnabled` in `D:\Prop\src` = **5 hits**, all in this file, all **writes or a public getter**. Fetch never branches on pump.

### 3.5 Wrapper remaps wrapper-`0` then still falls back to SDK-`0`

YoPips/Prop `Connect` L102–108: if the **caller** passes `pumpMode==0`, the wrapper **rewrites** it to `USERS|ORDERS|POSITIONS|SYMBOLS` (omits `PUMP_MODE_GROUPS`). If that pumped connect fails, it retries the SDK with literal `0`. So:

- Wrapper argument `0` ≠ SDK `0`.
- The **fallback** is the real no-pump session.
- `GetAllGroups` after that fallback is cache-only (`GroupTotal`/`GroupNext`, L962–981) and may return `[]`. Completeness without pump is `GroupRequestArray("*")`, which **this C++ wrapper never calls** (`grep` of Prop `mt5-sdk\src` and YoPips `src` = **0** for `GroupRequestArray` / `UserRequestArray` / `PositionRequestByGroup` / `DealRequestByGroup`).
- `GetUserLogins` **does** call SDK `UserLogins` (L322). That path is request-complete even when `GetAllGroups` is empty. `GetGroupLogins` is an alias (L1015–1016).

Prop C# does **not** inherit that groups-cache gap.

### 3.6 Live census used the request walk

`LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L24–29) calls `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")` on the same `NativeMt5BrokerConnector`. Artifact `LIVE_GROUPS_AND_TRADERS.json` recorded **non-zero** groups **and** empty groups (`demo\yo-instant` = 0, three Starwave `real` grps = 0). A cache-only walk after a cold no-pump session cannot produce that mixed non-zero + explicit-zero catalog. The JSON does **not** record `PumpEnabled`; the **fetch methods** are still Request-first, so the census is valid whether pump succeeded or the NONE fallback ran.

---

## 4. Product wiring — ALL groups / ALL traders

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).  
Implements `IMt5BrokerConnector` + `IMt5BulkDealReader` + `IMt5BulkPositionReader`.

### 4.1 Groups — `GroupRequestArray("*")` first

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
                // cache fallback GroupTotal/GroupNext only if request returned empty
```

Cache `GroupNext` is **only** if the request list is empty. Inverse of the C++ wrapper.

### 4.2 Traders — `UserRequestArray` first, then cache, then `UserLogins`

`GetAccountsCore(null)` walks **every** group from §4.1 (L199–202). Per group (`ReadAccountsForGroup`):

1. `UserRequestArray(gname, users)` L223 — network.
2. Hard fail only → `UserGetByGroup` L225 (pump cache).
3. Still `Total()==0` → `UserLogins` L230 + `UserRequestByLogins` L232.
4. Accounts: `UserAccountRequestArray` first (L235), cache `UserAccountGetByGroup` only on hard fail.

Dedup is `byLogin` (L205–209). No `Take`, no plan-map filter, no dummy substitution.

### 4.3 Deals / positions — request group APIs

| Method | Call | Line |
|---|---|---|
| `GetDealsCore` | `DealRequest(login, from, to)` in 14-day windows | L284 |
| `GetGroupDealsCore` | **`DealRequestByGroup`** same windows | L307 |
| `GetPositionsCore` | `PositionRequest(login)` | L327 |
| `GetGroupPositionsCore` | **`PositionRequestByGroup(mask)`**, cache `PositionGetByGroup` only on hard fail | L344–346 |

Ingest (`DealIngestionService.SyncCatalogAsync` L45–49) calls `GetGroupsAsync` + `GetAccountsAsync(null)`. `SyncBrokerAsync` then `GetGroupDealsAsync` per group + `GetGroupPositionsAsync("*")` (L65–85). Probe uses the same catalog + `"*"` positions. Hosted ingest (`LiveIngestHostedService`) drives that catalog walk for every registered connector (Native ×2 after `HasRealPasswords`).

---

## 5. Last measured Achiever + Starwave census

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`utc=2026-08-18T08:42:16.8519545+00:00`. `note="Passwords never written."` This slot **did not re-attach**.

Re-sum this slot from `groupNames[].accounts`:

| Broker | Connect | Groups | Traders | Open pos | How |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK, 7212.5885 ms (HTTP proxy) | 8 | 6512 | 1506 | `GroupRequestArray("*")` + `UserRequestArray` |
| STARWAVEFX | OK, 6413 ms class (direct; `ProxyEnabled=false` hard pin) | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (sum **6512** = 2+179+4+5+4+6295+0+23): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` **0**, `demo\yo-payp` 23.

Starwave groups (sum **1948** = 11+4+170+1735+22+0+0+4+0+2): `Starwave\cent\FX1\grp1` 11, `...\grp2` 4, `Starwave\demo\FX2\grp1` 170, `...\grp2` 1735, `Starwave\real\FX3\grp1` 22, `...\grp2` 0, `...\grp3` 0, `...\grp4` 4, `...\grp5` 0, `Starwave\real\FX3\LP` 2.

These are **all groups those two manager logins can see**. Server-side groups outside the manager ACL would not appear. Empty groups are **present** (request returned the name with 0 users), which a silent cache miss cannot distinguish from “ACL empty”.

Dashboard last write (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` = **8460**, `/api/groups` = **18**. Later parent briefs sometimes cite **8463**; that **+3 is unreconciled** and was **not** re-probed here. Pin this slot to the JSON: **18 / 8460 / 1984**.

---

## 6. Copy to cTrader must not send live orders (product hop)

| Control | Evidence | Result |
|---|---|---|
| Outbound FIX MsgType on hosted session | `CTraderFixSession.BuildLogon` L96 is `(35, "A")`. Only `WriteAsync` is that logon (L47–49). Sockets disposed by `using`. | Logon only |
| Product hosted `35=D` | `CTraderFixSession.cs` has **0** `35=D` / `NewOrderSingle`. Hosted service never calls a send builder. | `SAFE_BY_ABSENCE` on live hop |
| `NewOrderSingle` builder on copy hop | `CopyTradingService.NewOrderSingleImplemented = false` (const). Blocker: `"No NewOrderSingle sender — SAFE_BY_ABSENCE"`. AND gate at L198 also requires `VenueReconciled` (const false) + `TraderState.LIVE`. | Cannot arm |
| Persist `AllowFixSend` | Copy service L192 writes `AllowFixSend = false` on every `RiskDecisionRecord`. | Forced off |
| `ExecutionIntent` writers | `grep` `new ExecutionIntent` / `ExecutionIntents.Add` on `D:\Prop\src` = **0** | No send row |
| Auto LIVE | `BaselineScorer.CanPromoteToLive => false` (L211) | Cannot promote |
| Runtime flag | DI L41: `RealCopyEnabled = configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"`. Lab `.env` L73 is **`true`**. Hosted FIX logon **no longer re-pins** false (L68–70 only logs). `/api/settings` now **mirrors** `runtime.RealCopyEnabled` (Program.cs L76). | Flag **armed**; sender still missing |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled = false` L35 | POCO default off |
| Manager write APIs | 0 hits in `src\Mt5` for `DealerSend` / `OrderSend` / `TradeRequest` / `DealerBalance` / `UserAdd` | Read-only connector |
| YoPips C++ as dest sender | 0 cTrader FIX senders under YoPips `src` | Not a dest path |
| Hosted copy tick | `CopyTradingHostedService` L28–30: `GenerateShadowIntentsAsync` only; log says “Live NewOrderSingle still blocked.” | Shadow only |

FIX logon **is not** a send license. LoggedOn QUOTE 5211 / TRADE 5212 + catalog 8460 traders still cannot emit `NewOrderSingle` on the product hop.

`CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY` **false (forced)**” is **stale vs current DI**. Use this slot’s read: env **true**, DI **binds**, sender **absent**.

---

## 7. Residuals / honesty

1. **This slot did not live-attach.** Census is the 08:42Z JSON, not a new Manager session.
2. Probe JSON does **not** record `PumpEnabled`. Fetch is request-first either way.
3. C++ `GetAllGroups` is **not** no-pump-complete. Do not use YoPips `mt5_group_probe` as the ALL-groups proof. Use Prop C# `GroupRequestArray`.
4. C++ wrapper `pumpMode==0` remaps to a **partial pump** (no GROUPS). Only the **fallback** is SDK `0`.
5. `UserGetByGroup` / `PositionGetByGroup` / `GroupNext` remain as **fallbacks**. They are empty without pump. Primary path does not depend on them.
6. Census **8460 vs 8463** is unreconciled. Do not greenwash.
7. Hosted scoring is still `ListLoginsWithDealsAsync` (not every login). Catalog persist is all 8460. That is a **score** residual, not a fetch residual.
8. **`CTraderFixDemoTestTrade` is a real `35=D` builder** at L124–130 and close-out L155. Slot 140 “product `*.cs` have zero `35=D`” is **false now**. Callers: **only** `D:\Prop\tools\DemoFixTestTrade\Program.cs`. **0** hits in `Infrastructure`, `apps`, workers. Hard gate (L42–58) refuses unless `host` starts with `demo-`, `senderCompId` starts with `demo.`, and account is not `1369850`. This is **not** the copy hop. Do not call it from API. Do not treat it as live-send capability.

---

## 8. Files read (absolute)

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs` (L42 `PUMP_MODE_NONE`)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\tools\DemoFixTestTrade\Program.cs`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + group account fields only; no passwords)
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (census table; flag row stale)

---

## 9. Verdict

**PASS_REQUEST_APIS_NO_PUMP.** The five assigned Manager APIs are network RPCs. Pump is optional (deals have no cache; Admin MAIL/NEWS-only enum still has four of five; pool `Connect(...,0)` still calls `UserLogins`; C# retries `PUMP_MODE_NONE` and never gates fetch). Product C# is request-first for ALL manager-visible Achiever + Starwave groups and traders (last measured **18 / 8460 / 1984**). Product copy cannot send a live cTrader order (`CTraderFixSession` is `35=A` only, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`). Env `REAL_COPY=true` is bound but unused. Demo-only `CTraderFixDemoTestTrade` is a residual `35=D` builder outside the live hop. **Risk to capital: NONE.**
