# W500_RESEARCH_65 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **65** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 65 |
| Topic | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins appear only as **counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk. |

**Honesty:** A001 (`A001_native_connector.md`) is **stale**. It describes a prior C# connector that walked only `UserGetByGroup` / `UserAccountGetByGroup` and had no `PUMP_MODE_NONE` retry. The file on disk today is request-first. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK placement with `UserGetByLogins` (not the request block); filled only when `PUMP_MODE_USERS` completed; **absent** from `IMTAdminAPI` (Admin has no user pump); YoPips C++ `src\` never calls it |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | Header section `//--- clients and trade accounts request` at `MT5APIManager.h:407–411`; same method exists on Admin (`:1173`) which cannot pump users |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `ReadAccountsForGroup` L223 |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins (ACL-visible). Probe UTC `2026-08-18T08:42:16Z` |
| Copy to cTrader sends live orders | **NO** | `SAFE_BY_ABSENCE` — no `35=D` builder; `RealCopyEnabled` forced `false` |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

**Slot 65 verdict: CONFIRMED.** `UserGetByGroup` is pump-cache. `UserRequestArray` is the request path for ALL manager-visible traders. Achiever + Starwave are fetched that way. cTrader copy cannot send live orders yet. Risk to capital: **NONE**.

---

## 1. SDK naming law (Manager API 5570)

Header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Pin: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"` (L11–12).  
Same declarations in `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

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
      PUMP_MODE_SYMBOLS       =0x00000200,   // pump symbol configurations
      // ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

There is **no** `PUMP_MODE_NONE` name in C++. C# wrapper adds `PUMP_MODE_NONE = 0`. Pump `0` means the user cache is empty.

### 1.1 Get vs Request is a paired API

The header pairs cache `Get` with network `Request` on the same objects:

```250:261:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  UserTotal(void)=0;
   virtual MTAPIRES  UserGet(const uint64_t login,IMTUser* user)=0;
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   virtual MTAPIRES  UserGroup(const uint64_t login,MTAPISTR& group)=0;
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   // ...
   virtual MTAPIRES  UserAccountGet(const uint64_t login,IMTAccount* account)=0;
   virtual MTAPIRES  UserAccountRequest(const uint64_t login,IMTAccount* account)=0;
```

| Call | Kind | Needs pump bit? |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | **local pump cache** | users/accounts in pump scope |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | no |
| `UserLogins` | **network** (login list only) | no |
| `UserAccountRequest` / `UserAccountRequestArray` | **network** | no |
| `GroupTotal` / `GroupNext` / `GroupGet` | **local pump cache** | `PUMP_MODE_GROUPS` |
| `GroupRequest` / `GroupRequestArray` | **network** | no |

R010 (C# wrapper reflection, same day) states the same pairing (`D:\Prop\reports\swarm\20260818\R010_csharp_manager.md` L213–214):

> `GroupTotal` / `GroupNext` / `GroupGet` / `UserTotal` / `UserGet` / `UserGetByGroup` read the **local pump cache**.  
> `GroupRequest` / `GroupRequestArray` / `UserRequest` / `UserRequestArray` / `UserLogins` / `DealRequest*` hit the **server** and do **not** require the matching pump bit.

### 1.2 `UserRequestArray` sits in the request block

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

`GroupRequestArray` is the matching group enumerator (`MT5APIManager.h:212`).

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

Sibling account-cache batch:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty. `UserRequestArray` / `UserLogins` do **not** require that bit.

### 1.4 Admin API proves the split

`IMTAdminAPI` (`MT5APIManager.h:785`) pumps **only mail/news**:

```788:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

It has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup`. Header grep of `UserGetByGroup` under both Prop and YoPips SDK trees: **one hit**, Manager L672 only. Admin **does** expose the request enumerator:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

---

## 2. YoPips C++ — never uses `UserGetByGroup`; comments name the cache

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (this slot):

| Symbol | Hits in `src\` |
|---|---:|
| `UserGetByGroup` | **0** |
| `UserRequestArray` | **0** |
| `UserLogins` | used (`mt5_manager.cpp` L322, `mt5_pool.cpp` L217) |

YoPips enumerates traders with **`UserLogins`** (request). Account rows are cache-first with an explicit comment that the Get is pump memory:

```339:348:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    // Cache-first: UserAccountGet reads the in-memory pump cache (sub-ms) and
    // works only when this login's group is pump-synchronized. Fall back to the
    // network UserAccountRequest when the cache misses (no pump, or login not in
    // the synchronized scope).
    MTAPIRES res = m_manager->UserAccountGet(login, account);
    if (res != MT_RET_OK) {
        spdlog::debug("MT5 UserAccountGet miss for login {}: res={}, trying UserAccountRequest", login, res);
        res = m_manager->UserAccountRequest(login, account);
    }
```

Pool sessions say the same (`mt5_pool.cpp` L234–237):

> Cache-first: try the in-memory pump cache (`UserAccountGet`) before the network `UserAccountRequest`. Pool sessions connect mode=0/no-pump so this cache is normally empty and the fallback runs.

That is the same Get/Request law as `UserGet`/`UserGetByGroup` vs `UserRequest`/`UserRequestArray`. YoPips **does not** treat `UserGetByGroup` as a census API.

---

## 3. Prop C# live path — request first

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`  
Grep under `D:\Prop\src\*.cs`: the **only** `UserRequestArray` / `UserGetByGroup` / `UserLogins` / `UserRequestByLogins` calls are this file (L223, L225, L230, L232).

### 3.1 Connect tries pump, then `PUMP_MODE_NONE`

```89:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                // ...
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            // ...
            _connected = true;
            _pumpEnabled = false;
```

When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path **must** be first — and it is.

### 3.2 Groups: `GroupRequestArray("*")` then cache walk

`GetGroupsCore` (L144–186) requests `"*"` first. Only if the request array is empty does it walk `GroupTotal`/`GroupNext` (pump cache). Mask `"*"` = every group this manager ACL may see.

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

`GetAccountsCore(null)` (L189–214) walks **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup`. Dedupes by login. **No** `Take(` / `Skip` under `D:\Prop\src\Mt5`.

```216:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        var rows = new List<Mt5AccountDto>();
        var users = _manager!.UserCreateArray();
        var accounts = _manager.UserCreateAccountArray();
        try
        {
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

| Step | Call | Kind | When |
|---|---|---|---|
| 1 | `UserRequestArray(gname, users)` | **network** (full user records) | always first |
| 2 | `UserGetByGroup(gname, users)` | **pump cache** | only unexpected retcode (not OK / OK_NONE / NOTFOUND) |
| 3 | `UserLogins` + `UserRequestByLogins` | **network** | if the array is still empty |

Balances use the same split: `UserAccountRequestArray` first, then cache `UserAccountGetByGroup` (L235–237).

**So: `UserRequestArray` is the ALL-traders request path.** `UserGetByGroup` is **not** the primary enumerator. Using it alone on a `PUMP_MODE_NONE` session would silently return **zero** traders.

### 3.4 Ingest asks for the full book

`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` both call `GetAccountsAsync(null, ct)` (`DealIngestionService.cs` L48, L62). That is the ALL-groups / ALL-traders entry. Deals use `DealRequestByGroup` per group name; positions use `PositionRequestByGroup("*")`. Those are **read** Manager APIs. They do not place destination orders. There is no `SendTrade` / `DealerSend` / `OrderAdd` under `D:\Prop\src`.

### 3.5 Residuals (not a contradiction)

| Residual | Why it is not “UserGetByGroup is the request path” |
|---|---|
| `UserGetByGroup` fallback | Only on hard `UserRequestArray` failure. If that happens **and** `_pumpEnabled==false`, step 2 is empty; step 3 (`UserLogins`) still request-path. |
| Partial `UserRequestArray` accepted | `UserLogins` runs only when `users.Total() == 0`. If the request returns some-but-not-all users, missing logins are not recovered. MetaQuotes does not document pagination. Live probe pulled **6295** users from `demo\yo-2step` in one walk, so a large-group request has been measured. There is still no `users.Total()` vs `UserLogins.Length` equality check. |
| `UserGetByGroup` / `UserAccountGetByGroup` retcodes discarded | Harmless if the request succeeded; if request failed and cache is empty, `UserLogins` still runs. |
| ACL-visible only | `"*"` cannot see groups the manager login is forbidden. That is not a cache miss; it is server ACL. |

---

## 4. Measured ALL Achiever + Starwave census

This slot did **not** reconnect. Same-day artifact:

- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` — probe `LiveBrokerProbe`, UTC `2026-08-18T08:42:16.8519545+00:00`, `"note": "Passwords never written. Groups and manager logins only."`
- Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` (path recorded as `GroupRequestArray` + `UserRequestArray`)
- Status pin: `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy (`elapsedMs` 7212.6) | **8** | **6512** | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct (`elapsedMs` 6413.5) | **10** | **1948** | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever groups (sum 2+179+4+5+4+6295+0+23 = **6512**):

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

Starwave groups (sum 11+4+170+1735+22+0+0+4+0+2 = **1948**):

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

These are **all groups + all logins this manager ACL can see**. Groups the manager cannot see are outside this login’s permission set. Dummy/FakeMt5 seed is **off** (`LiveMt5Registration.HasRealPasswords` required).

`LIVE_GROUPS_AND_TRADERS.json` does **not** record `PumpEnabled`. The connector **tries pump first**, then `PUMP_MODE_NONE`. The **fetch** path is still `GroupRequestArray` / `UserRequestArray`, so the census is valid even if pump succeeded.

---

## 5. Copy to cTrader cannot send live orders (no loss)

| Gate | Measured |
|---|---|
| `CTraderFixSession` outbound MsgType | **Only** `(35, "A")` Logon (`CTraderFixSession.cs` L96). File is 135 lines; `WriteAsync` once; sockets disposed. |
| Product `*.cs` grep `35=D` | **0** under `D:\Prop\src` |
| Product `*.cs` grep `NewOrderSingle` as a send builder | **0** in `Fix.CTrader\Sessions` |
| `RealCopyEnabled` | Forced **false** at DI (`DependencyInjection.cs` L38–41) and again after FIX logon (`CTraderFixLogonHostedService.cs` L68) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (`CTraderFixOptions.cs` L35) |
| FIX worker | Even if `CTrader:RealCopyExecutionEnabled=true`, worker **refuses** and stamps `NewOrderSingle remains off.` (`apps/fix-worker/Worker.cs` L40–46) |
| MT5 order send | **0** `SendTrade` / `DealerSend` / `OrderAdd` under `D:\Prop\src` |
| Manager fetch | Read-only (`GroupRequestArray` / `UserRequestArray` / `DealRequest*` / `PositionRequest*`) |

Logon is for quotes/recon proof only. Snapshot copy-note when the flag is false (`LiveRuntimeStatus.cs` L42–43):

> `NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.`

**SAFE_BY_ABSENCE:** there is no NewOrderSingle builder to arm. Fetching 8460 traders cannot open a cTrader position.

This slot did not send FIX and did not print `CTRADER_FIX_PASSWORD` / manager / proxy secrets.

---

## 6. Stale reports (do not reuse)

| File | Why stale |
|---|---|
| `A001_native_connector.md` | Says traders = `UserGetByGroup` only; `UserRequestArray` unused; no pump-none Connect. All three are **false** on today’s `NativeMt5BrokerConnector.cs`. |
| `A010_prior_swarm.md` | “Live Manager Connect never measured succeeding” — superseded by `LIVE_GROUPS_AND_TRADERS.json`. |

R010’s table (`UserGetByGroup` = cache, `UserRequestArray` = network) remains correct. Sibling slots 5 / 25 / 45 asked the same pairing; this slot **re-read** the current tree and the JSON, not those reports.

---

## 7. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (SDK pairing + Admin absence + YoPips comments + unused in C++ `src`).
- [x] `UserRequestArray` confirmed **request path** and **primary ALL-traders enumerator** in Prop C#.
- [x] `GetAccountsAsync(null)` walks **every** Achiever + Starwave group the manager can see.
- [x] Live census 18 / 8460 cited from existing JSON (this slot did not re-attach).
- [x] cTrader copy **cannot** send live orders (`35=D` absent; flag forced false).
- [x] No secrets printed. Product source not edited.
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.

---

## 8. Sources (absolute paths)

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`

---

**Slot 65 verdict: CONFIRMED.** `UserGetByGroup` is pump-cache. `UserRequestArray` is the request path for ALL manager-visible traders. Achiever + Starwave are fetched that way. cTrader copy cannot send live orders yet.
