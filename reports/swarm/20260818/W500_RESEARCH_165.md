# W500_RESEARCH_165 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **165** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 165 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins listed only as **group counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk (`2026-08-18T08:42:16.8519545+00:00`). Re-summed this slot. Not re-probed. |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes a prior C# walk that used only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray` under `D:\Prop\src`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy. Slots that still say DI **pins** `RealCopyEnabled=false` are also **stale** — lab `.env` L73 is `true` and `DependencyInjection.cs` L41 binds it; safety is **sender absence**, not the flag.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK Get/Request pairing; sits with cache/sink APIs at `MT5APIManager.h:672`; `PUMP_MODE_USERS=0x00000001` fills it; `IMTAdminAPI` has **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS` (Admin pump bits are MAIL/NEWS only) |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | Header section `//--- clients and trade accounts request` at L407–411; same method exists on Admin (L1173) which cannot pump users |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `NativeMt5BrokerConnector.ReadAccountsForGroup` L223 — only C# call site under `D:\Prop\src` |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` (L224–225) |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| YoPips / Prop C++ product never uses the cache walk for ALL traders | **CONFIRMED** | `UserGetByGroup` **0** hits and `UserRequestArray` **0** hits under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` and `D:\Prop\mt5-sdk\src`. ALL-logins there is `UserLogins` (also a request API). |
| `_pumpEnabled` does **not** gate the enumerator | **CONFIRMED** | Writes at L96 / L110 / L140 + public getter L36 only. `ReadAccountsForGroup` never reads it. |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins / 1984 open positions (ACL-visible). Probe UTC `2026-08-18T08:42:16.8519545+00:00`. Re-summed this slot. |
| Copy-to-cTrader hop sends live orders | **NO** | `SAFE_BY_ABSENCE` on the copy hop — `CTraderFixSession` outbound MsgType is only `(35, "A")`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `VenueReconciled=false` |
| Risk to capital | **NONE** | Fetch is Manager **read**. Destination copy hop cannot emit NewOrderSingle from this process. |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

**Slot 165 verdict: CONFIRMED.**

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

Same pairing on groups (`GroupGet` L207 vs `GroupRequest` L208 / `GroupRequestArray` L212) and positions (`PositionGetByGroup` L286 vs `PositionRequestByGroup` L534). Deals have **no** `DealGet` and **no** `PUMP_MODE_DEALS` — only `DealRequest*`.

Independent C# Manager notes (`R010_csharp_manager.md` L213–214):

> `GroupTotal` / `GroupNext` / `GroupGet` / `UserTotal` / `UserGet` / `UserGetByGroup` read the **local pump cache**.  
> `GroupRequest` / `GroupRequestArray` / `UserRequest` / `UserRequestArray` / `UserLogins` / `DealRequest*` hit the **server** and do **not** require the matching pump bit.

YoPips `mt5_manager.cpp` L339–348 states the same law in product comments for the account twin: `UserAccountGet` is “in-memory pump cache (sub-ms)” and “works only when this login's group is pump-synchronized”; miss → `UserAccountRequest`.

| Method family | Where it reads | Pump required |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | **no** |
| `UserLogins` | **network** (login list only) | **no** |
| `UserAccountGet` / `UserAccountGetByGroup` | **local pump cache** | user/account pump |
| `UserAccountRequest` / `UserAccountRequestArray` | **network** | **no** |

### 1.2 `UserRequestArray` is explicitly a request API

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

`group` is a group **mask** (same language as `UserLogins`). Per-group exact names are what the measured collector used. Mask `*` is legal; ACL is still applied server-side.

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

`UserRequestByLogins` (L671) is the request sibling (used by C# after `UserLogins`). `UserGetByGroup` / `UserGetByLogins` are the cache siblings. Account cache twin:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

**Grep of this header for `UserGetByGroup`:** **exactly one** declaration (L672, `IMTManagerAPI` only).

### 1.4 Admin API cannot pump users, yet still exposes `UserRequestArray`

```788:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

Admin L1172–1173 still has `UserLogins` + `UserRequestArray`. There is **no** Admin `UserGetByGroup`. That is independent header proof that the Request enumerator is not a pump-cache walk.

---

## 2. Product C# walk is request-first (A001 is stale)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

Connect tries pump first (`PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS` = `0x00000181`), then `PUMP_MODE_NONE`. Fetch never branches on `_pumpEnabled`:

```89:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

`_pumpEnabled` writes: L96 / L110 / L140. Public getter L36. **Zero reads** inside `GetGroupsCore` / `GetAccountsCore` / `ReadAccountsForGroup`. Completeness does not depend on whether the first Connect accepted the pump bit.

### 2.1 Groups — `GroupRequestArray("*")` first

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
                // cache fallback: GroupTotal / GroupNext
```

Cache `GroupTotal`/`GroupNext` is used only if the request array is empty. That fallback **would** miss groups on pump-none. The primary path does not need pump.

### 2.2 Traders — `UserRequestArray` then cache then `UserLogins`

`GetAccountsAsync(null)` → `GetAccountsCore(null)` walks **every** group name from `GetGroupsCore()` (L201–203). Per group:

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

| Step | API | Kind | When |
|---|---|---|---|
| 1 | `UserRequestArray(gname)` | **network** | always first |
| 2 | `UserGetByGroup(gname)` | **pump-cache** | only hard fail (not OK / OK_NONE / NOTFOUND) |
| 3 | `UserLogins` + `UserRequestByLogins` | **network** | request/cache array still empty |
| 4 | `UserAccountRequestArray` | **network** | always first for balances |
| 5 | `UserAccountGetByGroup` | **pump-cache** | account request hard fail |

Empty group (`OK_NONE` / `NOTFOUND` / `Total()==0`) is **not** treated as a reason to trust cache. Soft-empty stays empty unless `UserLogins` returns IDs.

Dedup is by login (`Dictionary<ulong, Mt5AccountDto>`). No `Take` / `Skip` on the account walk.

### 2.3 Callers that ask for ALL traders

| Caller | Call | Meaning |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` | `GetAccountsAsync(null)` L48 | catalog upsert of every login |
| `DealIngestionService.SyncBrokerAsync` | `GetAccountsAsync(null)` L62 + per-group `DealRequestByGroup` | same census, then deals |
| `LiveIngestHostedService` | `ingest.SyncCatalogAsync` per connector | both brokers |
| `tools/LiveBrokerProbe` | `GetAccountsAsync(null)` L26 | measured JSON |

`LiveMt5Registration.CreateConnectors` builds **two** `NativeMt5BrokerConnector`s: Achiever (proxy from env) + Starwave (`ProxyEnabled = false` hardcoded L45).

Grep of `UserRequestArray` / `UserGetByGroup` under `D:\Prop\src`: **only** `NativeMt5BrokerConnector.cs` L223 / L225.

---

## 3. YoPips / Prop C++ is request via `UserLogins`, not `UserGetByGroup`

| Symbol | YoPips `src\` | Prop `mt5-sdk\src\` |
|---|---|---|
| `UserRequestArray` | **0** | **0** |
| `UserGetByGroup` | **0** | **0** |
| `UserLogins` | `mt5_manager.cpp` L322, `mt5_pool.cpp` L217 | same wrapper |

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    // ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is a one-line wrap of `GetUserLogins` (YoPips L1015–1017). That is the C++ ALL-traders request path. C++ `GetUserLogins` fail-closes on a null pointer — an empty group can look like API failure. C# treats `OK_NONE` / `NOTFOUND` as empty-ok and prefers `UserRequestArray`.

C++ `GetAllGroups` is **cache-only** (`GroupTotal` + `GroupNext`, YoPips L962–981). Completeness without pump for **groups** requires `GroupRequestArray("*")`, which the C++ wrapper does **not** call. The **measured** ALL-groups / ALL-traders collector is Prop C#, not YoPips C++.

YoPips Connect (L102–122) also retries `Connect(..., 0)` when pump fails, with an in-source comment that `DealRequest` works without the pump. Same Get/Request law.

---

## 4. Measured census (re-summed this slot; not re-probed)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` UTC **`2026-08-18T08:42:16.8519545+00:00`**. Path: `GroupRequestArray("*")` + per-group `UserRequestArray`. Dummy seed off. Passwords not written.

### 4.1 Achiever (HTTP proxy) — 8 groups / 6512 traders / 1506 positions — 7212.5885 ms

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |
| **sum** | **6512** |

Re-sum: `2+179+4+5+4+6295+0+23 = 6512`. Matches JSON `accounts: 6512`.

### 4.2 StarwaveFX (direct, `ProxyEnabled=false`) — 10 groups / 1948 traders / 478 positions — 6413.478 ms

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |
| **sum** | **1948** |

Re-sum: `11+4+170+1735+22+0+0+4+0+2 = 1948`. Matches JSON `accounts: 1948`.

**Total: 18 groups / 8460 manager-visible traders / 1984 open positions.**

These are **all groups these two manager logins can see**. Server groups outside the manager ACL are invisible by design. Probe JSON does **not** record `_pumpEnabled`; completeness still comes from `GroupRequestArray` / `UserRequestArray`, which do not need the pump.

Honesty: this slot did **not** re-attach. Counts are a re-sum of the 08:42Z artifact.

---

## 5. Copy to cTrader does not send live orders (no loss)

Fetch APIs (`GroupRequestArray` / `UserRequestArray` / `UserLogins` / `UserAccountRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`) are **read** RPCs. They do not call `DealerSend`, `DealerBalance`, `OrderAdd`, or `TradeAccountSet`. Grep of those write names under `D:\Prop\src\Mt5`: **0**. Fetching 8460 traders does not place an order.

### 5.1 Live FIX session — logon only

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines):

- One `WriteAsync` (L49) of `BuildLogon`.
- Only outbound MsgType: `(35, "A")` at L96.
- Sockets disposed (`using` TcpClient / SslStream).
- No `NewOrderSingle`, no `35=D`, no tag 38 `OrderQty`.

`CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented”. It does **not** re-pin `RealCopyEnabled=false`.

### 5.2 Copy hop — const-blocked + persist-blocked

`D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`:

| Gate | Value |
|---|---|
| `NewOrderSingleImplemented` | `const false` L16 |
| `VenueReconciled` | `const false` L15 |
| Persist `AllowFixSend` | **hard `false`** L192 (even if `RiskEngine` would allow) |
| Live-send `if` | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` L198 — unreachable |
| Status written | `SHADOW_ONLY` (or `LIVE_SEND_BLOCKED_UNIMPLEMENTED` if the if ever fired) |
| Hosted tick | `CopyTradingHostedService` L30: “Live NewOrderSingle still blocked.” |

Blocker list always includes `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` while the const is false.

### 5.3 Residual that does **not** put the copy hop on the wire

| Residual | Honest status |
|---|---|
| Lab `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` | Operator arm. `DependencyInjection.cs` L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. `/api/settings` now **reflects** that bool (L76). |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default **false**; not the copy-hop sender. |
| `apps/fix-worker` | Reads `CTrader:RealCopyExecutionEnabled` **log-only**; stamps FIX rows `Disconnected`. |
| `CTraderFixDemoTestTrade.Build("D", …)` L124 / L155 | **Exists.** Called only from `tools/DemoFixTestTrade`. **Not** called by copy / ingest / API. Demo-gated: host must start `demo-`, sender `demo.`, refuse `live-` / `live.`, refuse account `1369850`. |

Earlier slots that claimed “product `*.cs` has 0 `35=D`” are **slightly stale** because of the demo helper. The **copy pipeline** still cannot emit `35=D`. Architecture §68 is **0/19**, §70 **0/14**, §69 **0/12**. Do not enable a live sender from this fetch work.

---

## 6. What is not proven / residual

| Item | Severity | Note |
|---|---|---|
| Probe JSON omits `_pumpEnabled` | Low | Request APIs do not need it; census still valid |
| C++ `UserLogins` fail-closed on null pointer | Low | Empty group can look like API failure. C# uses `UserRequestArray` first |
| C++ `GetAllGroups` cache-only | Medium (C++ only) | Do not use YoPips probe as ALL-groups on pump-none. C# uses `GroupRequestArray` |
| `A001_native_connector.md` | Stale | Claimed cache-only traders + zero `UserRequestArray` hits |
| Env `REAL_COPY=true` now DI-bound | Residual arm | Next sender would see runtime armed. Current sender missing |
| `CTraderFixDemoTestTrade` | Residual demo helper | Off the copy hop; demo-gated |
| This slot did not live-attach | Honesty | 08:42Z artifact re-summed only |
| ACL ceiling | Expected | 18/8460 is manager-visible, not “every account on the server” |

---

## 7. Checklist

- [x] SDK `UserGetByGroup` located as pump-cache (`IMTManagerAPI` L672; `PUMP_MODE_USERS`; absent on Admin).
- [x] SDK `UserRequestArray` located as network request (`IMTManagerAPI` L410; `IMTAdminAPI` L1173).
- [x] C# product uses `UserRequestArray` first for ALL traders; cache only on hard fail; empty → `UserLogins`.
- [x] Ingest + `LiveBrokerProbe` call `GetAccountsAsync(null)` (every group).
- [x] Achiever + Starwave census re-summed: **8/6512 + 10/1948 = 18/8460**.
- [x] Copy hop cannot send live `35=D` (`SAFE_BY_ABSENCE` + persist `AllowFixSend=false`).
- [x] No secrets printed. Product source not edited. No live attach this slot.

---

## 8. Files read (absolute)

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
