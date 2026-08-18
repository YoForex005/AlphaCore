# W500_RESEARCH_185 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **185** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 185 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Group names + **counts** only. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact on disk (`2026-08-18T08:42:16.8519545+00:00`). Group-account columns **re-summed**. Not re-probed. |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes a prior C# walk that used only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray` under `D:\Prop\src`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy. Slots that still say DI **pins** `RealCopyEnabled=false` are also **stale** — DI now binds env `REAL_COPY_EXECUTION_ENABLED`; safety is **sender absence**, not the flag.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK Get/Request pairing; sits with cache/sink APIs at `MT5APIManager.h:672`; `PUMP_MODE_USERS=0x00000001` fills it; `IMTAdminAPI` has **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS` |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | Header section `//--- clients and trade accounts request` at L407–411; same method exists on Admin (L1173) which cannot pump users |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `NativeMt5BrokerConnector.ReadAccountsForGroup` L223 — only C# call site under `D:\Prop\src` |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` (L224–225) |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| YoPips C++ product never uses the cache walk for ALL traders | **CONFIRMED** | `UserGetByGroup` **0** hits and `UserRequestArray` **0** hits under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`. ALL-logins there is `UserLogins` (also a request API). |
| `_pumpEnabled` does **not** gate the enumerator | **CONFIRMED** | Writes at L96 / L110 / L140 + public getter L36 only. `ReadAccountsForGroup` never reads it. |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins (ACL-visible). Probe UTC `2026-08-18T08:42:16.8519545+00:00`. Re-summed this slot. |
| Copy to cTrader sends live orders | **NO** | Hosted `CTraderFixSession` outbound MsgType is `(35, "A")` only (135/135). `NewOrderSingleImplemented=false`. Persist `AllowFixSend=false`. `VenueReconciled=false`. Product `*.cs` literal `35=D` **0**. |
| Risk to capital | **NONE** | Fetch is Manager **read**. Destination copy hop cannot emit NewOrderSingle from this process. |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

**Slot 185 verdict: CONFIRMED.**

---

## 1. SDK naming law (Manager API 5570)

Header (Prop vendor pin): `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Same declarations: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

`Connect` takes a pump mask. Cache `Get*` / `Total` / `Next` only fill when the matching bit was accepted:

```124:144:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      PUMP_MODE_ACTIVITY      =0x00000002,   // pump users online activity
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_ORDERS        =0x00000008,   // pump orders
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      // ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

C# wrapper adds `PUMP_MODE_NONE = 0` (C++ header has no NONE name). `Connect(..., 0)` is a legal no-pump session. Cache `UserGet*` is then empty. `UserRequestArray` / `UserLogins` do **not** require `PUMP_MODE_USERS`.

### 1.1 Get vs Request is a paired API

The header pairs cache `Get` with network `Request` on the same objects:

```250:261:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  UserTotal(void)=0;
   virtual MTAPIRES  UserGet(const uint64_t login,IMTUser* user)=0;
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   virtual MTAPIRES  UserGroup(const uint64_t login,MTAPISTR& group)=0;
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserAccountGet(const uint64_t login,IMTAccount* account)=0;
   virtual MTAPIRES  UserAccountRequest(const uint64_t login,IMTAccount* account)=0;
```

| Cache (`Get*` / `Total` / `Next`) | Network (`Request*` / `UserLogins`) | Pump bit |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | `UserRequest` / `UserRequestArray` / `UserRequestByLogins` / `UserLogins` | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` / `UserAccountGetByLogins` | `UserAccountRequest` / `UserAccountRequestArray` / `UserAccountRequestByLogins` | users/accounts pump |
| `GroupTotal` / `GroupNext` / `GroupGet` | `GroupRequest` / `GroupRequestArray` | `PUMP_MODE_GROUPS` |
| `PositionGet` / `PositionGetByGroup` | `PositionRequest` / `PositionRequestByGroup` | `PUMP_MODE_POSITIONS` |

`Get*` reads the in-process pump cache. After `Connect(..., 0)` that cache is empty. `Request*` always hits the trade server. Using `UserGetByGroup` alone on a no-pump session silently returns **zero** rows.

### 1.2 `UserRequestArray` is explicitly a request API

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

Header grep (`UserRequestArray`): Manager L410 + Admin L1173.

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

Sibling account cache pull:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty. `UserRequestArray` / `UserLogins` do **not** require that bit.

### 1.4 Admin API proves the cache/request split

```789:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

Admin has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup` (header grep this slot: **one** hit, Manager L672 only). It **does** expose:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Header grep this slot:

| Symbol | Prop vendor header | YoPips vendor header |
|---|---|---|
| `UserGetByGroup` | **1** (Manager L672) | **1** (Manager L672) |
| `UserRequestArray` | **2** (Manager L410, Admin L1173) | **2** (same lines) |

---

## 2. YoPips C++ product never calls `UserGetByGroup`

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `UserGetByGroup` / `UserRequestArray`: **0 / 0**.

ALL-logins there is the **request** API `UserLogins`:

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    ...
}
```

Pool session is the same RPC (`mt5_pool.cpp` L211–222). Comment on pump-none fallback (`mt5_manager.cpp` L118–121): *“GetDeals / DealRequest works without the pump.”* Request APIs survive `pump_mode=0`. Cache `UserGetByGroup` does not.

Connect remaps `pumpMode==0` to a subset of pump bits (`PUMP_MODE_USERS|ORDERS|POSITIONS|SYMBOLS`, **omits GROUPS**), then retries SDK `Connect(..., 0)` if the pumped connect fails (L102–122). When the second connect wins, cache group/user walks are empty.

Prop `mt5-sdk\src` also has **zero** `UserGetByGroup` / `UserRequestArray` calls. Those wrappers are not the live Prop enumerator.

C++ `GetUserLogins` fail-closes on a null pointer (`if (res != MT_RET_OK || !raw_logins) return false`). An empty group can look like an API failure. C# uses `UserRequestArray` first and treats `MT_RET_OK_NONE` / `MT_RET_ERR_NOTFOUND` as empty-ok.

---

## 3. Prop C# product walk is request-first

File (459/459 lines this slot): `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

Grep under `D:\Prop\src`: the **only** `UserRequestArray` / `UserGetByGroup` calls are this file (L223, L225).

### 3.1 Connect tries pump, then `PUMP_MODE_NONE`

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
            _pumpEnabled = false;
```

Mask `GROUPS|USERS|POSITIONS` = `0x00000100 | 0x00000001 | 0x00000080` = `0x181` = 385.

When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path **must** be first — and it is. `_pumpEnabled` is **not** consulted by `ReadAccountsForGroup`. Grep of `_pumpEnabled` in this file: writes L30/L96/L110/L140 + public getter L36. **Zero** reads in fetch methods.

### 3.2 ALL groups: `GroupRequestArray("*")` first

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

`MT5_GROUP_*` mapping keys are **not** a filter. Mask `"*"` is the Manager ACL-visible universe.

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

```189:210:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }
```

`null` / whitespace = **every** name from `GetGroupsCore()`. Dedup by login. **No** `Take` / account-count knob.

### 3.4 Per-group enumerator (the assigned pairing)

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

Order:

1. **Primary (network):** `UserRequestArray(gname, users)` — ALL user records for that group mask.
2. **Fallback (pump-cache):** `UserGetByGroup` **only** if the request retcode is a hard fail (not `OK` / `OK_NONE` / `NOTFOUND`).
3. **Second request path:** if the array is still empty, `UserLogins` + `UserRequestByLogins`.
4. Accounts follow the same request-then-cache split (`UserAccountRequestArray` → `UserAccountGetByGroup`).

```
GetAccountsAsync(null)
  └─ GetGroupsCore()
        1. GroupRequestArray("*")     // network
        2. else GroupTotal/GroupNext  // pump cache
  └─ for each name:
        1. UserRequestArray           // network — ALL traders for that group
        2. UserGetByGroup             // only on hard fail
        3. if Total()==0: UserLogins + UserRequestByLogins
```

If `UserRequestArray` returns `OK` / `OK_NONE` / `NOTFOUND` with a **partial** array (`Total() > 0` but not the full group), `UserGetByGroup` is skipped and `UserLogins` is skipped. Completeness then depends on the request RPC. The same-day probe still measured 6512 + 1948 logins across 18 groups, including empty groups (`demo\yo-instant` = 0, three Starwave real groups = 0). That is **manager-ACL-visible ALL**, not “every login on the broker that this manager cannot see.”

### 3.5 Product callers pass `null` (no group filter)

| Caller | Call | Effect |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L48 | `GetAccountsAsync(null, ct)` | persist every manager-visible login |
| `DealIngestionService.SyncBrokerAsync` L62 | `GetAccountsAsync(null, ct)` | same, then deals/positions |
| `tools/LiveBrokerProbe/Program.cs` L26 | `GetAccountsAsync(null, …)` | wrote `LIVE_GROUPS_AND_TRADERS.json` |

Ingest `Take(` = **0** (cap-removed; leftover `Take(200)` is `GET /api/trades` only).

---

## 4. Measured census (re-summed this slot, not re-attached)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` · UTC `2026-08-18T08:42:16.8519545+00:00` · `envLoaded: true` · note *“Passwords never written.”*

Path in the JSON brokers: `GroupRequestArray` + `UserRequestArray` via `GetGroupsAsync` + `GetAccountsAsync(null)`.

### Achiever (HTTP proxy) — 7212.5885 ms — `connected: true`

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
| **sum** | **6512** |

`2+179+4+5+4+6295+0+23 = 6512`. Matches JSON `"groups": 8, "accounts": 6512, "openPositions": 1506`.

### StarwaveFX (direct) — 6413.478 ms — `connected: true`

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
| **sum** | **1948** |

`11+4+170+1735+22+0+0+4+0+2 = 1948`. Matches JSON `"groups": 10, "accounts": 1948, "openPositions": 478`.

**Total: 18 groups / 8460 traders / 1984 open positions.**  
JSON does **not** record `_pumpEnabled`. Completeness still comes from `GroupRequestArray` / `UserRequestArray`, which do not need the pump.

These are **all groups this manager login can see**. If the server has more groups, they are outside this manager’s permission set.

---

## 5. Copy to cTrader cannot send live orders (no loss)

This slot did **not** open a FIX socket.

| Gate | Measured state |
|---|---|
| Hosted session | `CTraderFixSession.BuildLogon` L96 is `(35, "A")` only. File 135/135. One `WriteAsync` (the logon). Sockets disposed. |
| Product `35=D` literal | **0** hits under `D:\Prop\src` `*.cs` |
| Copy hop const | `CopyTradingService.NewOrderSingleImplemented = false` (L16) |
| Venue | `VenueReconciled = false` (L15) |
| Persist | `AllowFixSend = false` forced on every `RiskDecisionRecord` (L192) |
| Live-send branch | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L198) — last two are **const false** |
| Status written | `"SHADOW_ONLY"` (L204) or `"LIVE_SEND_BLOCKED_UNIMPLEMENTED"` |
| Runtime note | `LiveRuntimeStatus.Snapshot` L42–44: even if `RealCopyEnabled`, *“NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”* |
| Logon host | `CTraderFixLogonHostedService` L69: *“NewOrderSingle still unimplemented.”* |

`GroupRequestArray` / `UserRequestArray` / `UserLogins` / `PositionRequestByGroup` / `DealRequestByGroup` are **read** RPCs. They do not call `DealerSend`, `DealerBalance`, `OrderAdd`, or `TradeAccountSet`. Fetching 8460 traders does not place an order.

### Residual (honest, not a hosted send)

1. **DI binds env `REAL_COPY_EXECUTION_ENABLED`.** `DependencyInjection.cs` L41: `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)`. Architecture / POCO / worker fallback still default **false**. Slots that claim the process **pins** false are **stale**. The next sender, if one is written, would see the lab env as armed. **Today there is no sender on the copy hop.**
2. **Standalone demo tool can emit MsgType `D`.** `CTraderFixDemoTestTrade.SendAsync` is called only from `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Gate L43–47 refuses unless `host` starts with `demo-`, `senderCompId` starts with `demo.`, and account is **not** the live Pepperstone id. **Not** wired into API / ingest / `CopyTradingService` / FIX logon host. Do not claim the entire tree has zero NewOrderSingle constructors; claim the **product host / copy hop** cannot send.

---

## 6. Stale artifacts (do not reuse)

| Artifact | Claim | Now |
|---|---|---|
| `A001_native_connector.md` | Traders = cache only; zero `UserRequestArray` under `src` | **False.** L223 is the primary call. |
| `A005_dashboard_traders.md` | walk is every `UserGetByGroup` | **False.** Request first. |
| `A010_prior_swarm.md` | live Manager walk never measured | **False.** Same-day 18/8460 probe exists. |
| Slots 3/57/63/68/83/108 “DI pins REAL_COPY false” | process cannot arm | **Stale.** DI binds env. Safety is `SAFE_BY_ABSENCE`. |
| Slots that say product `*.cs` have zero `35=D` **builders** | entire tree has no `D` | **Narrower truth:** literal `35=D` is 0; `Build("D")` exists on the **demo tool** only. |

---

## 7. Files read / grepped this slot

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (L11–12, L124–144, L200–212, L250–261, L407–411, L668–673, L742–743, L789–795, L1172–1173)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` (same lines)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (Connect fallback L102–135; `GetUserLogins` L315–327)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` (L211–222)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (L48, L62)
- `D:\Prop\tools\LiveBrokerProbe\Program.cs` (L19–29)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (gate L43–59)
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (L15–16, L192, L198, L240–255)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (L41)
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` (L42–44)
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + both `groupNames` blocks)
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`

Grep: `UserGetByGroup` / `UserRequestArray` on Prop `src`, YoPips `src`, both vendor headers, `mt5-sdk`. `35=D` on `D:\Prop\src` `*.cs` = **0**. `_pumpEnabled` on the native connector.

---

## 8. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (`PUMP_MODE_USERS`; absent on Admin).
- [x] `UserRequestArray` confirmed **network request** (header `request` block + Admin still has it).
- [x] C# ALL-traders path is `UserRequestArray` first (L223); cache only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.
- [x] ALL Achiever + Starwave groups/traders: **code + 18/8460 re-sum** (ACL-visible). This slot did not re-attach.
- [x] Copy hop cannot send live `35=D` (`SAFE_BY_ABSENCE`). Residual demo-tool builder is off-host.
- [x] No secrets printed. No product source edited.

**Risk to capital: NONE.**
