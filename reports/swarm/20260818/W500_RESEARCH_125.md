# W500_RESEARCH_125 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **125** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 125 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins listed only as **group counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk (`2026-08-18T08:42:16.8519545Z`). Not re-probed. |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes a prior C# connector that walked only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray` under `D:\Prop\src`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK Get/Request pairing; sits with cache/sink APIs at `MT5APIManager.h:672`; `PUMP_MODE_USERS=0x1` fills it; `IMTAdminAPI` has **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS` |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | Header section `//--- clients and trade accounts request` at L407–411; same method exists on Admin (L1173) which cannot pump users |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `NativeMt5BrokerConnector.ReadAccountsForGroup` L223 — only C# call site |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` (L224–225) |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| YoPips C++ product never uses the cache walk for ALL traders | **CONFIRMED** | `UserGetByGroup` **0** hits and `UserRequestArray` **0** hits under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`. ALL-logins there is `UserLogins` (also a request API). |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins (ACL-visible). Probe UTC `2026-08-18T08:42:16.8519545Z`. Re-summed this slot. |
| Copy to cTrader sends live orders | **NO** | `SAFE_BY_ABSENCE` — no `35=D` builder in product `*.cs`; only outbound MsgType is `(35, "A")`; `RealCopyEnabled` forced `false` |
| Risk to capital | **NONE** | Fetch is Manager **read**. Destination cannot emit NewOrderSingle from this process. |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

**Slot 125 verdict: CONFIRMED.**

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

C# wrapper adds `PUMP_MODE_NONE = 0` (C++ header has no NONE name). `Connect(..., 0)` is a legal no-pump session. Cache `UserGet*` is then empty. Official WebAPI sample names the same integer (`Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs:42`).

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

Same pairing on groups (`GroupGet` L207 vs `GroupRequest` L208 / `GroupRequestArray` L212) and positions (`PositionGetByGroup` / `PositionGetByLogins` L531–532 vs `PositionRequestByGroup` L534).

| Call | Kind | Needs pump bit? |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | **local pump cache** | users/accounts in pump scope |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | **no** |
| `UserLogins` | **network** (login list only) | **no** |
| `UserAccountRequest` / `UserAccountRequestArray` | **network** | **no** |

R010 (`R010_csharp_manager.md` L213–214) independently recorded the same split after reflecting `CIMTManagerAPI`:

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

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

`UserRequestByLogins` is the request sibling (used by C# after `UserLogins`). `UserGetByGroup` / `UserGetByLogins` are the cache siblings. Account cache twin is later:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

### 1.4 Admin API proves the split (no user pump ⇒ no `UserGetByGroup`)

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

Grep of `UserGet` in that header: **3 hits**, all on `IMTManagerAPI` (L251 `UserGet`, L672 `UserGetByGroup`, L673 `UserGetByLogins`). **Zero** `UserGet*` on Admin.

Admin still exposes the request enumerator:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

---

## 2. YoPips C++ — Get = cache, Request / UserLogins = network

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` (this slot):

| Symbol | Hits in `src\` |
|---|---:|
| `UserGetByGroup` | **0** |
| `UserRequestArray` | **0** |
| `UserLogins` | **yes** — `MT5Manager::GetUserLogins` L315–327; `MT5Session::GetUserLogins` in `mt5_pool.cpp` L211–217 |

YoPips never walks the user pump cache for a group census. ALL logins = `UserLogins` (request). Same Get/Request law is written in product comments on the sibling account API:

```339:348:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    // Cache-first: UserAccountGet reads the in-memory pump cache (sub-ms) and
    // works only when this login's group is pump-synchronized. Fall back to the
    // network UserAccountRequest when the cache misses (no pump, or login not in
    // the synchronized scope).
    MTAPIRES res = m_manager->UserAccountGet(login, account);
    if (res != MT_RET_OK) {
        spdlog::debug("MT5 UserAccountGet miss for login {}: res={}, trying UserAccountRequest", login, res);
        res = m_manager->UserAccountRequest(login, account);
```

Identical comment + fallback exists in Prop `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` L339–348.

`GetAllGroups` in YoPips is **cache-only** (`GroupTotal` + `GroupNext`, L962–981). That is why the Prop C# collector must use `GroupRequestArray("*")` as primary — pump-none would otherwise miss groups. Groups and users follow the same law: `Get*`/`Total`/`Next` = cache; `Request*`/`UserLogins` = server.

---

## 3. Prop C# — request-first ALL-traders walk

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines, this slot).  
Grep of `UserRequestArray` / `UserGetByGroup` / `UserLogins` under `D:\Prop\src\*.cs`: **only this file**, three call sites (L223 / L225 / L230).

### 3.1 Connect: pump preferred, `PUMP_MODE_NONE` keeps request APIs

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

When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path must be first — and it is. There is **no** `if (_pumpEnabled)` gate on the enumerator.

### 3.2 Groups: `GroupRequestArray("*")` then cache walk only if empty

`GetGroupsCore` L144–185: `GroupRequestArray("*")`; if `list.Count == 0`, walk `GroupTotal` / `GroupNext`. Mask `*` = every group this manager ACL may see.

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

```189:210:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
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

`group == null` (ingest + probe) walks **every** discovered name. Dedup by login. **No** `.Take(`, **no** plan-map filter, **no** dummy 10001/10002 substitution on this type.

```216:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        // ...
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

| `UserRequestArray` retcode | Next step | Cache used? |
|---|---|---|
| `OK` / `OK_NONE` | if `Total()==0` then `UserLogins` | no |
| `NOTFOUND` | skip `UserGetByGroup`; if empty then `UserLogins` | no |
| any other error | `UserGetByGroup` (cache); if still empty then `UserLogins` | **only as backup** |

So:

- **Primary ALL-traders path = `UserRequestArray`.**
- **`UserGetByGroup` is pump-cache only and is not the ALL path.**
- Completeness net when the request returns an empty array = `UserLogins` + `UserRequestByLogins` (both network).

Balances overlay the same split (`UserAccountRequestArray` L235 then cache `UserAccountGetByGroup` L237). A missing account row still emits the login with zeros — the user walk, not the account overlay, is the census.

### 3.4 Who asks for ALL?

| Caller | Call | Filter |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L48 | `GetAccountsAsync(null, ct)` | none |
| `DealIngestionService.SyncBrokerAsync` L62 | `GetAccountsAsync(null, ct)` | none |
| `LiveIngestHostedService` | `SyncCatalogAsync` per registered Native connector | none |
| `tools/LiveBrokerProbe/Program.cs` L26 | `GetAccountsAsync(null)` | none |

`DealIngestionService` is 146 lines, **0** `Take(`/`Skip`. Dummy `FakeMt5BrokerConnector` exists as a type but API `Program.cs` has **0** `FakeMt5` / `DemoSeeder` hits; DI (`DependencyInjection.cs` L35–46) fail-closes unless both real passwords are present, then registers Native connectors only, `RealCopyEnabled = false`.

---

## 4. Live census (measured, not re-attached this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` (`tools/LiveBrokerProbe/Program.cs`) — `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.  
UTC: **`2026-08-18T08:42:16.8519545+00:00`**. Passwords not written. JSON note: `"Passwords never written. Groups and manager logins only."`

This slot **re-summed** the per-group `accounts` fields. Did **not** re-connect.

| Broker | Connect (prior) | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy, 7213 ms | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

### Achiever (re-sum)

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

`2+179+4+5+4+6295+0+23 = 6512`. Matches JSON `"accounts": 6512`.

Largest single request success in this tree: **6295** users in `demo\yo-2step` in one `UserRequestArray` walk. That is measured evidence the request enumerator is not a 200-row window.

### Starwave (re-sum)

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

`11+4+170+1735+22+0+0+4+0+2 = 1948`. Matches JSON `"accounts": 1948`.

These are **all groups / all logins these two manager ACLs can see**. Server-side groups outside the ACL would not appear. Empty groups (`demo\yo-instant`, three Starwave real groups) are still listed — the walk is name-complete, not “skip empty.”

**Honesty on pump bit for that run:** the JSON does **not** record `PumpEnabled`. The connector **tries pump first**, then `PUMP_MODE_NONE`. The **fetch** path is still `GroupRequestArray` / `UserRequestArray`, so the census is valid even if pump succeeded.

---

## 5. Copy to cTrader cannot send live orders (no loss)

Safety is **`SAFE_BY_ABSENCE`**, not a unit-tested refuse-on-LoggedOn-TRADE gate.

| Check | Measured |
|---|---|
| `CTraderFixSession.cs` (135 lines) `35=D` / `NewOrderSingle` | **0** |
| Only outbound MsgType | `(35, "A")` Logon (`BuildLogon` L96) |
| Socket lifetime | one `WriteAsync` of Logon, then dispose (`TryLogonAsync` L48–50) |
| Product `D:\Prop\src\*.cs` `35=D` / `(35, "D")` | **0** |
| YoPips `src\` cTrader `35=D` | **0** |
| `RealCopyEnabled` DI pin | `DependencyInjection.cs` L40–41 `false` (“Live NewOrderSingle is not implemented”) |
| Hosted logon re-pin | `CTraderFixLogonHostedService.cs` L68 `= false`; L70 log “NewOrderSingle still disabled” |
| POCO default | `CTraderFixOptions.RealCopyExecutionEnabled` L35 `= false` |
| Runtime note when false | `LiveRuntimeStatus.Snapshot` L42–43: “NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.” |

Manager `UserRequestArray` / `UserGetByGroup` / `DealRequestByGroup` / `PositionRequestByGroup` are **reads**. They do not open a destination position.

FIX TRADE logon (if the password is present) is **not** a send. Architecture §68 is still 0/19 and §70 is still 0/14 — this slot does **not** tick those.

---

## 6. Residuals (do not greenwash)

1. **Partial `UserRequestArray` accepted.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returned `OK` with a truncated non-empty array, missing logins would not be recovered. MetaQuotes does not document pagination on this call. Live probe returned 6295 users in one group, so large-group success is measured. There is still no `users.Total()` vs `UserLogins.Length` equality check.
2. **`UserGetByGroup` / `UserAccountGetByGroup` return codes are discarded.** Harmless if `UserRequestArray` succeeded; if the request failed and the cache is empty, `UserLogins` still runs.
3. **A001 is stale.** Do not quote it as current product state.
4. **Census age.** Same-day 08:42Z artifact. Slot 125 did not re-attach. Counts can drift as traders are created/deleted.
5. **ACL ceiling.** “ALL” = all manager-visible groups/logins, not “every login on the trade server.”
6. **YoPips `GetAllGroups` is still cache-only.** Product C# already uses `GroupRequestArray`. Do not regress to the C++ probe as the collector.

---

## 7. What “fetch ALL” means on this tree

```
Connect (pump GROUPS|USERS|POSITIONS, else PUMP_MODE_NONE)
  GroupRequestArray("*")            // ALL ACL-visible groups (request)
    else GroupTotal/GroupNext       // cache, only if request empty
  for each name:
    UserRequestArray(name)          // ALL users in that group (request)  ← PRIMARY
      else UserGetByGroup(name)     // pump cache, HARD FAIL only
      if still empty:
        UserLogins + UserRequestByLogins   // request completeness net
    UserAccountRequestArray(name)   // balances (request)
      else UserAccountGetByGroup    // cache overlay
```

`UserGetByGroup` is **not** sufficient for ALL traders when pump is off or incomplete. `UserRequestArray` is.

---

## 8. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (SDK pairing + Admin absence + YoPips Get/Request comments + unused in C++ `src`).
- [x] `UserRequestArray` confirmed **request path** and **primary ALL-traders enumerator** in Prop C# (`ReadAccountsForGroup` L223).
- [x] Empty-array net is **network** `UserLogins` + `UserRequestByLogins`, not more cache.
- [x] Ingest/probe ask `GetAccountsAsync(null)` — every group, no `Take`.
- [x] Live census 8/6512 + 10/1948 = 18/8460 re-summed from `LIVE_GROUPS_AND_TRADERS.json` (not re-probed).
- [x] Copy-to-cTrader cannot place: `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**.
- [x] No secrets printed. No product source edited.
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.

---

## 9. Absolute paths cited

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\A001_native_connector.md` (**stale**)
- `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md`

**End of W500_RESEARCH_125.**
