# W500_RESEARCH_105 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **105** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 105 |
| Topic | Confirm `UserGetByGroup` is **pump-cache** and `UserRequestArray` is the **request** path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins cited only as **counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk (`2026-08-18T08:42:16Z`). |

**Honesty rule:** `A001_native_connector.md` is **stale**. It describes an older C# walk that used only `UserGetByGroup` / `UserAccountGetByGroup` and claimed “zero hits for `UserRequestArray`.” The file on disk today is **request-first**. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy.

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

Same-day C# wrapper notes (`R010_csharp_manager.md`):

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

It has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup` (header grep this slot: **one** hit, Manager L672 only). It **does** expose:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Header grep (`UserGetByGroup` under both vendor trees): **1 hit each**, Manager L672.  
Header grep (`UserRequestArray`): Manager L410 + Admin L1173.

---

## 2. YoPips C++ product never calls `UserGetByGroup`

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `UserGetByGroup` / `UserRequestArray`: **0 / 0**.  
Hits exist only in the vendor header (`MetaTrader5SDK\Include\MT5APIManager.h` L410 / L672 / L1173).

The C++ wrapper enumerates traders with the **request** login list:

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

Same `UserLogins` walk in `mt5_pool.cpp` L211–222. Groups in that tree are **cache-only**:

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    // GroupTotal + GroupNext walk — pump group cache
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

Connect in YoPips remaps `pumpMode==0` to a subset of pump bits, then **retries SDK `Connect(..., 0)`** if the pumped connect fails (`mt5_manager.cpp` L102–122). Comment on the fallback: *“GetDeals / DealRequest works without the pump.”* Request APIs survive `pump_mode=0`. Cache `UserGetByGroup` does not.

Prop `mt5-sdk\src` also has **zero** `UserGetByGroup` / `UserRequestArray` calls. Those wrappers are not the live Prop enumerator.

---

## 3. Prop C# live path — request first, cache only on hard fail

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).  
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
                LastError = null;
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

`GetAccountsCore` L189–213: `group == null` → walk **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup` per name. Dedup by login. **No** `.Take(...)` on this walk.

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
```

Order of operations (measured from the file, not from A001):

1. **Primary (network):** `UserRequestArray(gname, users)` — ALL user records for that group mask.  
2. **Fallback (pump-cache):** `UserGetByGroup` **only** if the request retcode is a hard fail (not `OK` / `OK_NONE` / `NOTFOUND`).  
3. **Fallback (network):** if the array is still empty, `UserLogins` then `UserRequestByLogins`.  
4. Money fields: `UserAccountRequestArray` first, then cache `UserAccountGetByGroup` on hard fail (L235–237).

### 3.4 Residual (not a live-order risk)

If `UserRequestArray` returns `OK` / `OK_NONE` / `NOTFOUND` with a **partial** array (`Total() > 0` but not the full group), `UserGetByGroup` is skipped and `UserLogins` is skipped. Completeness then depends on the request RPC. The same-day probe still measured 6512 + 1948 logins across 18 groups, including empty groups (`demo\yo-instant` = 0, three Starwave real groups = 0). That is **manager-ACL-visible ALL**, not “every login on the broker that this manager cannot see.”

---

## 4. Ingest / probe walk ALL groups + ALL users (no dummy)

| Caller | Call | Filter |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L45–49 | `GetGroupsAsync` + `GetAccountsAsync(null)` | none |
| `DealIngestionService.SyncBrokerAsync` L61–62 | same, then deals/positions | none |
| `LiveIngestHostedService` L41–56 | `registry.All()` → both brokers → `SyncCatalogAsync` | none |
| `LiveBrokerProbe` L19–26 | `CreateConnectorsFromEnvironment()` → `GetAccountsAsync(null)` | none |
| `LiveMt5Registration.CreateConnectors` | Native ×2 only (`ACHIEVER` + `STARWAVEFX`) | `FakeMt5` not registered |

DI (`DependencyInjection.cs` L35–46) **throws** unless both real passwords pass `IsSecret`, then registers only `LiveMt5Registration.CreateConnectors`. Starwave `ProxyEnabled = false` (L45). Achiever may use the HTTP hop from env. Fetch is **flag-blind** — `RealCopyEnabled` is not consulted by the Manager walk.

---

## 5. Measured census (same day; this slot did not re-attach)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` UTC **`2026-08-18T08:42:16.8519545+00:00`**. Passwords never written.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK, 7213 ms (HTTP proxy) | 8 | 6512 | 1506 | `GroupRequestArray("*")` + per-group `UserRequestArray` |
| STARWAVEFX | OK, 6413 ms (direct) | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

### Achiever groups (8 / 6512)

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

### Starwave groups (10 / 1948)

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

Empty groups are **included**. That is what ALL groups means: every name the manager ACL returns, including zeros. Groups outside the manager permission set are not claimed.

Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`.

---

## 6. Copy to cTrader cannot send live orders (no loss)

| Gate | Measured |
|---|---|
| `35=D` / `(35, "D")` in product `*.cs` / `*.json` / `*.csproj` | **0 hits** (this slot grep) |
| `CTraderFixSession.cs` (135 lines) only outbound MsgType | `(35, "A")` Logon (`BuildLogon` L96). One `WriteAsync`. Sockets disposed. |
| `NewOrderSingle` in `Fix.CTrader` | comment (`CTraderFixOptions` L33) + log string (`CTraderFixLogonHostedService` L70). **No builder.** |
| `RealCopyExecutionEnabled` POCO default | `false` (`CTraderFixOptions` L35) |
| DI pin | `RealCopyEnabled = false` (`DependencyInjection.cs` L40–41) |
| Hosted FIX pin | `_runtime.RealCopyEnabled = false` after logon (`CTraderFixLogonHostedService` L68) |
| `.env` | `REAL_COPY_EXECUTION_ENABLED=false` (L73; value only, no secret) |
| FIX worker | even if config true, only logs “refuses NewOrderSingle”; no send function (`Worker.cs` L21–46) |
| Native Manager send | **0** hits for `DealerSend` / `DealerBalance` / `UserAdd` / `SendTrade` under `D:\Prop\src` |
| Shadow | `ShadowCopyEngine` is in-process math; `PersistDemoShadowAsync` is SHADOW_ONLY |

`SAFE_BY_ABSENCE`: there is no NewOrderSingle to fire. Logon (`35=A`) is not copy. Fetching 8460 traders is a Manager **read**. Capital cannot be lost from this process placing a destination order.

Architecture §68 / §70 remain **not PASS**. That is independent of this slot. Do **not** add `35=D`. Do **not** flip `REAL_COPY_EXECUTION_ENABLED`.

---

## 7. What this slot did **not** do

- Did **not** live-attach either manager. Census is the 08:42Z JSON already on disk.  
- Did **not** edit product source.  
- Did **not** print passwords or proxy auth.  
- Did **not** claim groups outside the two manager ACLs.  
- Did **not** treat YoPips `GetAllGroups` (`GroupNext` cache) as the Prop live path.  
- Did **not** treat A001’s “cache-only traders” paragraph as current.

---

## 8. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (`PUMP_MODE_USERS`; absent on Admin).  
- [x] `UserRequestArray` confirmed **network request** (header `request` block + Admin still has it).  
- [x] C# ALL-traders path is `UserRequestArray` first (L223); cache only on hard fail; empty → `UserLogins` + `UserRequestByLogins`.  
- [x] Groups via `GroupRequestArray("*")` first (L155).  
- [x] Ingest/probe use `GetAccountsAsync(null)` — every discovered group.  
- [x] Live census (prior same-day, not re-probed): Achiever **8 / 6512** + Starwave **10 / 1948** = **18 / 8460**.  
- [x] Copy cannot send live orders: `35=D` absent; `RealCopyEnabled=false`. Risk to capital **NONE**.

---

## 9. Sources (absolute paths)

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`  
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`  
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`  
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`  
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`  
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`  
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`  
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`  
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`  
- `D:\Prop\apps\fix-worker\Worker.cs`  
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`  
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`  
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`  
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
