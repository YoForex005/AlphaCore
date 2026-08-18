# W500_RESEARCH_145 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **145** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 145 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins listed only as **group counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk (`2026-08-18T08:42:16.8519545+00:00`). Re-summed. Not re-probed. |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes a prior C# walk that used only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray` under `D:\Prop\src`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy. Slots that still say DI **pins** `RealCopyEnabled=false` are also **stale** — `.env` L73 is `true` and DI binds it; safety is **sender absence**, not the flag.

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
| Copy to cTrader sends live orders | **NO** | `SAFE_BY_ABSENCE` — product `*.cs` `35=D` **0**; only outbound MsgType is `(35, "A")`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `VenueReconciled=false` |
| Risk to capital | **NONE** | Fetch is Manager **read**. Destination cannot emit NewOrderSingle from this process. |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

**Slot 145 verdict: CONFIRMED.**

---

## 1. SDK naming law (Manager API 5570)

Header (Prop vendor pin): `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Same declarations: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

`Connect` takes a pump mask. Cache `Get*` / `Total` / `Next` only fill when the matching bit was accepted:

```124:144:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
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

```250:261:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  UserTotal(void)=0;
   virtual MTAPIRES  UserGet(const uint64_t login,IMTUser* user)=0;
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   virtual MTAPIRES  UserGroup(const uint64_t login,MTAPISTR& group)=0;
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserAccountGet(const uint64_t login,IMTAccount* account)=0;
   virtual MTAPIRES  UserAccountRequest(const uint64_t login,IMTAccount* account)=0;
```

Same pairing on groups (`GroupGet` L207 vs `GroupRequest` L208 / `GroupRequestArray` L212) and positions (`PositionGetByGroup` vs `PositionRequestByGroup`).

Independent C# Manager notes (`R010_csharp_manager.md` L213–214):

> `GroupTotal` / `GroupNext` / `GroupGet` / `UserTotal` / `UserGet` / `UserGetByGroup` read the **local pump cache**.  
> `GroupRequest` / `GroupRequestArray` / `UserRequest` / `UserRequestArray` / `UserLogins` / `DealRequest*` hit the **server** and do **not** require the matching pump bit.

| Method family | Where it reads | Pump required |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | **no** |
| `UserLogins` | **network** (login list only) | **no** |

### 1.2 `UserRequestArray` is explicitly a request API

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

`group` is a group **mask** (same language as `UserLogins`). Per-group exact names are what the measured collector used.

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

`UserRequestByLogins` (L671) is the request sibling (used by C# after `UserLogins`). `UserGetByGroup` / `UserGetByLogins` are the cache siblings. Account cache twin is later:

```742:743:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty.

### 1.4 Admin API proves the split (no user pump ⇒ no `UserGetByGroup`)

`IMTAdminAPI` pump bits (header L789–795) are **only** `PUMP_MODE_MAIL` and `PUMP_MODE_NEWS`. No `PUMP_MODE_USERS`. Admin still exposes:

```1172:1173:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

Grep of `UserGet` in that header: **3 hits**, all on `IMTManagerAPI` (L251 `UserGet`, L672 `UserGetByGroup`, L673 `UserGetByLogins`). **Zero** `UserGet*` on Admin.

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Header grep this slot:

| Symbol | Prop vendor header | YoPips vendor header |
|---|---|---|
| `UserRequestArray` | L410 (Manager) + L1173 (Admin) | same |
| `UserGetByGroup` | L672 (Manager only) | same |

---

## 2. YoPips C++ product never calls `UserGetByGroup`

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` this slot:

| Symbol | Hits |
|---|---:|
| `UserGetByGroup` | **0** |
| `UserRequestArray` | **0** |
| `UserLogins` | **yes** — `mt5_manager.cpp` L322, `mt5_pool.cpp` L217, HTTP client wrap |

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint64_t* raw_logins = nullptr;
    uint32_t total = 0;

    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;

    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

`GetGroupLogins` is a one-line wrap (L1015–1017). This is a **network** call. It works in no-pump mode.

C++ `GetUserLogins` fail-closes on a null pointer (`if (res != MT_RET_OK || !raw_logins) return false`). An empty group can look like an API failure. C# uses `UserRequestArray` first and treats `MT_RET_OK_NONE` / `MT_RET_ERR_NOTFOUND` as empty-ok.

Connect in YoPips remaps `pumpMode==0` to a subset of pump bits, then **retries SDK `Connect(..., 0)`** if the pumped connect fails (`mt5_manager.cpp` L102–122). Comment on the fallback: *“GetDeals / DealRequest works without the pump.”* Request APIs survive `pump_mode=0`. Cache `UserGetByGroup` does not.

Prop `mt5-sdk\src` also has **zero** `UserGetByGroup` / `UserRequestArray` calls. Those wrappers are not the live Prop enumerator.

---

## 3. Prop C# ALL-traders path is request-first

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

Grep of `UserRequestArray` / `UserGetByGroup` / `UserLogins` under `D:\Prop\src\*.cs`: **only this file**, four call sites (L223 / L225 / L230 / L232).

### 3.1 Connect tries pump, then none

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
            // ...
            _connected = true;
            _pumpEnabled = false;
```

Mask `GROUPS|USERS|POSITIONS` = `0x00000181` = 385. When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path must be first — and it is.

`_pumpEnabled` is **write-only** except the public getter (`PumpEnabled`). Grep this slot: L30, L36, L96, L110, L140. **No** `if (_pumpEnabled)` on the enumerator.

### 3.2 Groups: `GroupRequestArray("*")` first

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
                // GroupTotal / GroupNext — pump-cache fallback
```

ALL ACL-visible groups = request `*`. Cache `GroupNext` only if the request array is empty.

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`group == null` (ingest + probe) walks **every** discovered name. Dedup by login. **No** `.Take(`, **no** plan-map filter, **no** dummy 10001/10002 substitution on this type. Grep `Take(200)` under `D:\Prop\src\*.cs` this slot: **0**.

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

| `UserRequestArray` retcode | Next step | Cache used? |
|---|---|---|
| `OK` / `OK_NONE` | if `Total()==0` then `UserLogins` | no |
| `NOTFOUND` | skip `UserGetByGroup`; if empty then `UserLogins` | no |
| any other error | `UserGetByGroup` (cache); if still empty then `UserLogins` | **only as backup** |

So:

1. **Primary (network):** `UserRequestArray(gname, users)` — ALL user records for that group mask.
2. **Fallback (pump-cache):** `UserGetByGroup` **only** if the request retcode is a hard fail (not `OK` / `OK_NONE` / `NOTFOUND`).
3. **Completeness net (network):** if the user array is still empty, `UserLogins` + `UserRequestByLogins`.

If `UserRequestArray` returns `OK` / `OK_NONE` / `NOTFOUND` with a **partial** array (`Total() > 0` but not the full group), `UserGetByGroup` is skipped and `UserLogins` is skipped. Completeness then depends on the request RPC. The same-day probe still measured 6512 + 1948 logins across 18 groups, including empty groups (`demo\yo-instant` = 0, three Starwave real groups = 0). That is **manager-ACL-visible ALL**, not “every login on the broker that this manager cannot see.”

Balances overlay the same split (`UserAccountRequestArray` L235 then cache `UserAccountGetByGroup` L237). A missing account row still emits the login with zeros — the user walk, not the account overlay, is the census.

### 3.4 Who asks for ALL?

| Caller | Call | Filter |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L48 | `GetAccountsAsync(null, ct)` | none |
| `DealIngestionService.SyncBrokerAsync` L62 | `GetAccountsAsync(null, ct)` | none |
| `LiveIngestHostedService` | `SyncCatalogAsync` per registered Native connector | none |
| `tools/LiveBrokerProbe/Program.cs` L26 | `GetAccountsAsync(null)` | none |

`DealIngestionService` has **0** `Take(`/`Skip`. Dummy `FakeMt5BrokerConnector` exists as a type but API host DI (`DependencyInjection.cs` L35–46) fail-closes unless both real passwords are present, then registers Native connectors only.

---

## 4. Live census (measured, not re-attached this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` (`tools/LiveBrokerProbe/Program.cs`) — `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.  
UTC: **`2026-08-18T08:42:16.8519545+00:00`**. Passwords not written. JSON note: `"Passwords never written. Groups and manager logins only."`

This slot **re-summed** the per-group `accounts` fields. Did **not** re-connect.

| Broker | Connect (prior) | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy, 7212.5885 ms | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct, 6413.478 ms | 10 | 1948 | 478 | same |
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

| Check | Measured this slot |
|---|---|
| `CTraderFixSession.cs` (135 lines) `35=D` / `NewOrderSingle` | **0** |
| Only outbound MsgType | `(35, "A")` Logon (`BuildLogon` L96) |
| Socket lifetime | one `WriteAsync` of Logon, then dispose (`TryLogonAsync` L48–50) |
| Product `D:\Prop\src` `35=D` / `(35, "D")` | **0** |
| YoPips `src\` `35=D` | **0** |
| `CopyTradingService.NewOrderSingleImplemented` | `const bool` **false** (L16) |
| `CopyTradingService.VenueReconciled` | `const bool` **false** (L15) |
| Persist | `AllowFixSend = false` hardcoded (`CopyTradingService` L192) |
| LIVE send branch | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — first three of four are structurally false; even if entered, status is `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L198–200) |
| POCO default | `CTraderFixOptions.RealCopyExecutionEnabled` L35 `= false` |
| Runtime note when flag true | `LiveRuntimeStatus.Snapshot` L42–43: “REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.” |

**Flag honesty (do not copy stale slots 65/85/105/125 item “DI pins false”):**  
`DependencyInjection.cs` L41 binds `REAL_COPY_EXECUTION_ENABLED` from configuration. Lab `.env` L73 is `true`. `/api/settings` now exposes `runtime.RealCopyEnabled` (`apps/api/Program.cs` L76). Hosted FIX logon **no longer** re-pins false (`CTraderFixLogonHostedService.cs` L68–70 logs `RealCopyArmed={Armed}`). That is an **operator arm**, not a send path. Fetching 8460 traders does not place an order.

Manager `UserRequestArray` / `UserGetByGroup` / `DealRequestByGroup` / `PositionRequestByGroup` are **reads**. They do not open a destination position.

FIX TRADE logon (if the password is present) is **not** a send. Architecture §68 is still 0/19 and §70 is still 0/14 — this slot does **not** tick those.

---

## 6. Residuals (do not greenwash)

1. **Partial `UserRequestArray` accepted.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returned `OK` with a truncated non-empty array, missing logins would not be recovered. MetaQuotes does not document pagination on this call. Live probe returned 6295 users in one group, so large-group success is measured. There is still no `users.Total()` vs `UserLogins.Length` equality check.
2. **`UserGetByGroup` / `UserAccountGetByGroup` return codes are discarded.** Harmless if `UserRequestArray` succeeded; if the request failed and the cache is empty, `UserLogins` still runs.
3. **A001 is stale.** Do not quote it as current product state.
4. **Census age.** Same-day 08:42Z artifact. Slot 145 did not re-attach. Counts can drift as traders are created/deleted.
5. **ACL ceiling.** “ALL” = all manager-visible groups/logins, not “every login on the trade server.”
6. **YoPips `GetAllGroups` is still cache-only.** Product C# already uses `GroupRequestArray`. Do not regress to the C++ probe as the collector.
7. **`.env` REAL_COPY is `true` and DI binds it.** Safety is sender absence + `AllowFixSend=false` persist + `NewOrderSingleImplemented=false`. Do not claim the flag is pinned false.

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

- [x] `UserGetByGroup` confirmed **pump-cache** (`PUMP_MODE_USERS`; absent on Admin; unused in YoPips `src`).
- [x] `UserRequestArray` confirmed **network request** (header `request` block + Admin still has it).
- [x] C# ALL-traders path is `UserRequestArray` first (L223); cache only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.
- [x] Ingest/probe ask `GetAccountsAsync(null)` — every group, no `Take`.
- [x] Live census 8/6512 + 10/1948 = 18/8460 re-summed from `LIVE_GROUPS_AND_TRADERS.json` (not re-probed).
- [x] Copy-to-cTrader cannot place: `35=D` absent (`SAFE_BY_ABSENCE`). Risk to capital **NONE**.
- [x] No secrets printed. No product source edited.
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.

---

## 9. Absolute paths cited

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md`
- `D:\Prop\reports\swarm\20260818\A001_native_connector.md` (**stale**)
