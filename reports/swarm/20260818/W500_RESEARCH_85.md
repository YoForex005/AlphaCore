# W500_RESEARCH_85 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **85** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 85 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins listed only as **group counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk (`2026-08-18T08:42:16Z`). |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes a prior C# connector that walked only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK Get/Request pairing; sits with cache/sink APIs at `MT5APIManager.h:672`; `PUMP_MODE_USERS` fills it; `IMTAdminAPI` has **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS` |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | Header section `//--- clients and trade accounts request` at L407–411; same method exists on Admin (L1173) which cannot pump users |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `NativeMt5BrokerConnector.ReadAccountsForGroup` L223 |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` (L224–225) |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins (ACL-visible). Probe UTC `2026-08-18T08:42:16Z` |
| Copy to cTrader sends live orders | **NO** | `SAFE_BY_ABSENCE` — no `35=D` builder; `RealCopyEnabled` forced `false` |
| Risk to capital | **NONE** | Fetch is Manager **read**. Destination cannot emit NewOrderSingle from this process. |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

---

## 1. SDK naming law (Manager API 5570)

Header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Pin: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"` (L11–12).  
Same declarations in `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

`Connect` takes a pump mask (`L164`). Cache `Get*` / `Total` / `Next` only fill when the matching bit was accepted:

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

C# wrapper adds `PUMP_MODE_NONE = 0` (C++ header has no NONE name). `Connect(..., 0)` is a legal no-pump session. Cache `UserGet*` is then empty.

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

Same pairing on groups (`GroupGet` L207 vs `GroupRequest` L208 / `GroupRequestArray` L212) and positions (`PositionGetByGroup` L286 vs `PositionRequestByGroup` L534).

| Call | Kind | Needs pump bit? |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | **local pump cache** | users/accounts in pump scope |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | **no** |
| `UserLogins` | **network** (login list only) | **no** |
| `UserAccountRequest` / `UserAccountRequestArray` | **network** | **no** |

R010 (C# wrapper reflection, same day) states the same pairing:

> `GroupTotal` / `GroupNext` / `GroupGet` / `UserTotal` / `UserGet` / `UserGetByGroup` read the **local pump cache**.  
> `GroupRequest` / `GroupRequestArray` / `UserRequest` / `UserRequestArray` / `UserLogins` / `DealRequest*` hit the **server** and do **not** require the matching pump bit.

### 1.2 `UserRequestArray` is explicitly a request API

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

Section comment is `request`, not `database` / `sink`. The `group` argument is a **mask** (same language as `UserLogins` / Administrator group filters). Per-group exact name or `"*"` are both legal; ACL still applies server-side.

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

`UserGetByGroup` is adjacent to `UserGetByLogins` (cache by login list) and the account **sink** subscribe/unsubscribe pair. The sibling account cache pull is later:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty. `UserRequestArray` / `UserLogins` do **not** require that bit.

### 1.4 Admin API is the negative proof

`IMTAdminAPI` (`MT5APIManager.h:785`) pumps **only mail/news**:

```789:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

It has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup` (header grep: **one** hit, Manager L672 only). It **does** expose:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Header grep (`UserGetByGroup` under both trees): **1 hit each**, Manager L672.  
Header grep (`UserRequestArray`): Manager L410 + Admin L1173.

---

## 2. YoPips C++ product never calls `UserGetByGroup`

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `UserGetByGroup` / `UserRequestArray`: **0 / 0**.

The C++ wrapper enumerates traders with the **request** login list:

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

Same `UserLogins` walk in `mt5_pool.cpp` L211–222. Groups in that tree are **cache-only**:

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(...) {
    uint32_t total = m_manager->GroupTotal();
    // GroupNext walk — pump group cache
}
```

Account money uses the same Get/Request law, with an explicit comment that `*Get` is pump memory:

```339:348:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    // Cache-first: UserAccountGet reads the in-memory pump cache (sub-ms) and
    // works only when this login's group is pump-synchronized. Fall back to the
    // network UserAccountRequest when the cache misses (no pump, or login not in
    // the synchronized scope).
    MTAPIRES res = m_manager->UserAccountGet(login, account);
    if (res != MT_RET_OK) {
        res = m_manager->UserAccountRequest(login, account);
    }
```

Pool sessions spell out the no-pump case (`mt5_pool.cpp` L234–237):

> Cache-first: try the in-memory pump cache (`UserAccountGet`) before the network `UserAccountRequest`. Pool sessions connect mode=0/no-pump so this cache is normally empty and the fallback runs — harmless.

That is the same Get/Request law as `UserGet`/`UserGetByGroup` vs `UserRequest`/`UserRequestArray`.

Connect in YoPips remaps `pumpMode==0` to a subset of pump bits, then **retries SDK `Connect(..., 0)`** if the pumped connect fails (`mt5_manager.cpp` L102–122). Comment on the fallback: *“GetDeals / DealRequest works without the pump.”* Request APIs survive `pump_mode=0`. Cache `UserGetByGroup` does not.

Prop `mt5-sdk\src` also has **zero** `UserGetByGroup` / `UserRequestArray` calls. Those wrappers are not the live Prop enumerator.

---

## 3. Prop C# live path — request first, cache only on hard fail

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).  
Grep under `D:\Prop\src\*.cs`: the **only** `UserRequestArray` / `UserGetByGroup` / `UserLogins` calls are this file (L223, L225, L230).

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
                // ...
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            // ...
            _pumpEnabled = false;
```

When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path **must** be first — and it is. `_pumpEnabled` is **not** consulted by `ReadAccountsForGroup`.

### 3.2 Groups: `GroupRequestArray("*")` then cache walk

`GetGroupsCore` L155: `_manager.GroupRequestArray("*", arr)`.  
Fallback only if the request list is empty: `GroupTotal` / `GroupNext` (L169–182). That cache walk needs `PUMP_MODE_GROUPS`. The request walk does not.

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

`GetAccountsCore` L189–213: `group == null` → walk **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup` per name. Dedup by login. **No** `.Take(...)` on this walk (src `Take(` hits are dashboard reject-reasons and FIX checksum only).

`DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62 both call `GetAccountsAsync(null, ct)`.  
`LiveIngestHostedService` L56 calls `SyncCatalogAsync`.  
`LiveBrokerProbe` L26 calls `GetAccountsAsync(null)` — that is how the census JSON was built.

```216:237:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

            var acctReq = _manager.UserAccountRequestArray(gname, accounts);
            if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
                _manager.UserAccountGetByGroup(gname, accounts);
```

| Step | API | Kind | When |
|---:|---|---|---|
| 1 | `UserRequestArray(gname, users)` | **network request** | **always first** |
| 2 | `UserGetByGroup(gname, users)` | **pump cache** | only unexpected retcode (not OK / OK_NONE / NOTFOUND) |
| 3 | `UserLogins` + `UserRequestByLogins` | **network request** | `users.Total() == 0` |
| 4 | `UserAccountRequestArray` then `UserAccountGetByGroup` | request then cache | balances only |

So: **`UserRequestArray` is the ALL-traders request path.** `UserGetByGroup` is **not** the primary enumerator. Using it alone on a `PUMP_MODE_NONE` session would silently return **zero** traders.

### 3.4 Residual completeness (honest, not a verdict flip)

| Residual | Why it is not “UserGetByGroup is the request path” |
|---|---|
| `UserGetByGroup` fallback | Only on hard `UserRequestArray` failure. If that happens **and** `_pumpEnabled==false`, step 2 is empty; step 3 (`UserLogins`) still request-path. |
| Partial `UserRequestArray` accepted | `UserLogins` runs only when `users.Total() == 0`. If the request returned some but not all users, missing logins are not recovered. MetaQuotes does not document pagination. Live probe returned **6295** users in `demo\yo-2step` in **one** walk — measured large-group success. Still no `users.Total()` vs `UserLogins.Length` equality check. |
| `UserGetByGroup` / `UserAccountGetByGroup` retcodes discarded | Harmless if the request succeeded; if request failed and cache is empty, `UserLogins` still runs. |
| ACL ceiling | “ALL traders” = every login these two manager accounts may see. Server groups outside ACL are invisible by design. |

---

## 4. Measured census (not re-probed this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `GetGroupsAsync` + `GetAccountsAsync(null)` (L25–26).  
Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` (path recorded as `GroupRequestArray` + `UserRequestArray`).  
UTC: `2026-08-18T08:42:16.8519545+00:00`. Passwords never written.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy (7213 ms) | **8** | **6512** | 1506 | `GroupRequestArray("*")` + per-group `UserRequestArray` |
| STARWAVEFX | OK direct | **10** | **1948** | 478 | same |
| **Total** | | **18** | **8460** | 1984 | |

Achiever groups (counts only): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23. Sum **6512**.

Starwave groups (counts only): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `grp2` 1735, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `real\FX3\LP` 2. Sum **1948**.

`demo\yo-instant` = 0 and three Starwave real groups = 0 are **empty groups**, not missed walks. The probe listed the group name with account count 0.

`LIVE_GROUPS_AND_TRADERS.json` does **not** record `PumpEnabled`. The connector **tries pump first**. The **fetch** path is still `GroupRequestArray` / `UserRequestArray`, so the census is valid even if pump succeeded. Header + YoPips `Connect(...,0)` + `DealRequest` comment independently prove the same request calls work at `pump_mode=0`. This slot did **not** re-attach live.

---

## 5. Copy to cTrader cannot send live orders (no loss)

| Gate | State | Path |
|---|---|---|
| `CTraderFixSession.BuildLogon` | only outbound MsgType is `(35, "A")` Logon | `CTraderFixSession.cs` L96 |
| `35=D` / `NewOrderSingle` builder | **absent** in `src\Fix.CTrader` | grep: comments/flags only |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** | `CTraderFixOptions.cs` L35 |
| DI pin | `RealCopyEnabled = false` — “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” | `DependencyInjection.cs` L40–41 |
| After FIX logon | `_runtime.RealCopyEnabled = false` again | `CTraderFixLogonHostedService.cs` L68 |
| Runtime snapshot | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` | `LiveRuntimeStatus.cs` L42–44 |
| Fix-worker | even if config were true: **logs a warning and still does not send** | `apps\fix-worker\Worker.cs` L21–46 |

Manager `UserRequestArray` / `UserGetByGroup` / `DealRequest*` / `PositionRequest*` are **read**. They do not open a destination position.

**Risk to capital: NONE (`SAFE_BY_ABSENCE`).** Do not enable `REAL_COPY_EXECUTION_ENABLED`. There is still no builder to honor it.

---

## 6. Stale documents (do not reuse)

| File | Why stale vs today’s tree |
|---|---|
| `A001_native_connector.md` | Says traders = `UserGetByGroup` only; `UserRequestArray` unused under `D:\Prop\src`; no pump-none Connect. All three are **false** on today’s `NativeMt5BrokerConnector.cs` (L101, L223, L230). |
| `A005_dashboard_traders.md` / `A007_store_batch.md` | Describe `UserGetByGroup` as the walk. That was the old connector. |
| `A010_prior_swarm.md` | “Live Achiever/Starwave Connect has never been measured succeeding.” Superseded by `LIVE_MANAGER_FETCH_MEASURED.md` + JSON `08:42Z`. |

R010’s table (`UserGetByGroup` = cache, `UserRequestArray` = network) remains correct.  
Sibling slots 5 / 25 / 45 asked the same question; this slot **re-read the current files** and agrees.

---

## 7. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (SDK pairing + Admin absence + YoPips comments + unused in C++ `src`).
- [x] `UserRequestArray` confirmed **request path** and **primary ALL-traders enumerator** in Prop C#.
- [x] `GetAccountsAsync(null)` walks every group; ingest and probe use that.
- [x] Live census cited (not re-run): Achiever 8 / 6512, Starwave 10 / 1948.
- [x] Copy-to-cTrader cannot place (`35=D` absent; `RealCopyEnabled=false`).
- [x] No secrets printed. No product source edited.
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.

---

## 8. Slot 85 verdict

**CONFIRMED.** `UserGetByGroup` is pump-cache. `UserRequestArray` is the request path for ALL manager-visible traders. Achiever + Starwave are fetched that way. cTrader copy cannot send live orders yet. Risk to capital: **NONE**.
