# W500 RESEARCH 25 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | 25 |
| Date | 2026-08-18 |
| Agent | W500_RESEARCH_25 |
| Role | Senior engineer, read-only confirmation |
| Product source modified | none |
| Secrets | none printed (manager logins in catalog seed are already public in-repo; passwords not copied) |

**Assigned topic:** Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for ALL traders. Goal: fetch ALL Achiever + Starwave groups and ALL manager traders; copy to cTrader must not send live orders yet (no loss).

**Honesty:** A001 (`A001_native_connector.md`) is **stale**. It describes a prior C# connector that called only `UserGetByGroup` / `UserAccountGetByGroup`. The file on disk today is request-first. This report quotes the current tree.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK pairing + placement; YoPips C++ never calls it; C++ comments on sibling `UserAccountGet` = “in-memory pump cache”; Admin API has `UserRequestArray` but **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS` |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | SDK section `//--- clients and trade accounts request` at `MT5APIManager.h:407–411`; same method exists on `IMTAdminAPI` (no user pump); C# connector calls it first |
| Current Prop C# path for ALL traders | **CONFIRMED** | `ReadAccountsForGroup`: `UserRequestArray` → (error only) `UserGetByGroup` → (if still empty) `UserLogins` + `UserRequestByLogins` |
| ALL Achiever + Starwave groups + traders | **CODE + MEASURED** | `GetAccountsAsync(null)` walks every name from `GroupRequestArray("*")`. Live probe 2026-08-18T08:42:16Z: **18 groups / 8460 logins** |
| Copy to cTrader cannot send live orders | **CONFIRMED — no loss** | `RealCopyEnabled = false`; FIX session builds only `35=A`; zero `35=D` / `SendTrade` / `DealerSend` under `D:\Prop\src` |

**One-liner:** `Get*` = local pump cache; `Request*` / `UserLogins` = server. The live census must (and now does) go through `UserRequestArray` (with `UserLogins` as the empty-array completeness net). `UserGetByGroup` is only a cache fallback and is **not** sufficient for ALL traders when pump is off or incomplete.

---

## 1. SDK contract — Get vs Request is the whole API

Source: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`  
(identical declarations in `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`)

### 1.1 Pump bits that fill the cache `Get*` reads

```125:144:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      ...
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

`UserTotal` / `UserGet` / `UserGetByGroup` / `UserGetByLogins` are only populated when `PUMP_MODE_USERS` (or `FULL`) actually completed. They do not hit the trade server.

### 1.2 Canonical pair on a single login

```250:261:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  UserTotal(void)=0;
   virtual MTAPIRES  UserGet(const uint64_t login,IMTUser* user)=0;
   virtual MTAPIRES  UserRequest(const uint64_t login,IMTUser *user)=0;
   ...
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   ...
   virtual MTAPIRES  UserAccountGet(const uint64_t login,IMTAccount* account)=0;
   virtual MTAPIRES  UserAccountRequest(const uint64_t login,IMTAccount* account)=0;
```

Throughout this header the naming law is:

| Name | Source | Needs pump bit |
|---|---|---|
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` / `UserTotal` | **local pump cache** | `PUMP_MODE_USERS` |
| `UserAccountGet` / `UserAccountGetByGroup` | **local pump cache** | user/account pump |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` | **network** | no |
| `UserAccountRequest` / `UserAccountRequestArray` | **network** | no |
| `UserLogins` | **network login list** | no |

### 1.3 `UserRequestArray` is explicitly a request API

```407:411:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

`group` is a **group mask** (same language as `UserLogins` / Administrator “Groups”: comma templates, `!` exclude, `*` wildcards — A39 / `CMTStr::CheckGroupMask`). Passing each discovered group name, or `"*"`, is the complete manager-ACL enumerator.

### 1.4 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

Sibling cache reader:

```742:743:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

### 1.5 Admin API proves the split

`IMTAdminAPI` (`MT5APIManager.h:785`) pumps **only mail/news**. It has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup`. It **does** expose the request enumerator:

```1172:1173:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Same Get/Request split on groups: `GroupTotal`/`GroupNext`/`GroupGet` = cache (`PUMP_MODE_GROUPS`); `GroupRequest`/`GroupRequestArray` = network (`IMTManagerAPI:205–212`).

---

## 2. YoPips C++ PropFirm — production comments + what they actually call

### 2.1 Cache vs network is documented on the sibling API

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp:339–348`:

```text
// Cache-first: UserAccountGet reads the in-memory pump cache (sub-ms) and
// works only when this login's group is pump-synchronized. Fall back to the
// network UserAccountRequest when the cache misses (no pump, or login not in
// the synchronized scope).
```

That is the same Get/Request law as `UserGet`/`UserGetByGroup` vs `UserRequest`/`UserRequestArray`.

### 2.2 ALL traders in a group = `UserLogins` (request), never `UserGetByGroup`

`GetUserLogins` (`mt5_manager.cpp:315–328`) calls `m_manager->UserLogins(...)`.  
`GetGroupLogins` (`:1015–1017`) is an alias of that.

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `UserGetByGroup` / `UserRequestArray`:

| Symbol | Hits in `src\` |
|---|---:|
| `UserGetByGroup` | **0** |
| `UserRequestArray` | **0** |
| `UserLogins` | used (`mt5_manager.cpp:322`, `mt5_pool.cpp:217`) |
| `UserRequest` (single login) | used for record fetch (`mt5_manager.cpp:252, 287, 1028, 1050, 1072`) |

C++ PropFirm’s “ALL logins in this group” path is **network `UserLogins`**, then per-login **network `UserRequest`**. It never treats the pump cache as the census.

### 2.3 C++ groups are still cache-only (contrast; Prop C# is ahead)

`GetAllGroups` / `GetGroupDetails` (`mt5_manager.cpp:962–1013`) walk `GroupTotal` + `GroupNext`. They do **not** call `GroupRequestArray`. That is fine when `PUMP_MODE_GROUPS` completed; it is empty on a no-pump connect. Default C++ pump mask (`:104–107`) is `USERS|ORDERS|POSITIONS|SYMBOLS` — **groups bit omitted**. A39 already flagged this.

C++ connect (`:114–135`) does retry `Connect(..., 0)` so request APIs stay available when pump is refused.

---

## 3. Prop C# connector — request-first (current disk, not A001)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`  
Grep under `D:\Prop\src\*.cs`: the **only** `UserRequestArray` / `UserGetByGroup` / `UserLogins` calls are this file.

### 3.1 Connect: pump, then `PUMP_MODE_NONE` so request APIs still work

```89:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            ...
            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            ...
            _pumpEnabled = false;
```

When the second connect wins, `_pumpEnabled = false`. `UserGetByGroup` would then return empty. The request path must be first — and it is.

### 3.2 Groups: `GroupRequestArray("*")` first; cache only if that list is empty

```155:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
                if (res == MTRetCode.MT_RET_OK || res == MTRetCode.MT_RET_OK_NONE)
                {
                    for (uint i = 0; i < arr.Total(); i++)
                    {
                        ...
                        AddGroup(list, seen, g);
                    }
                }
            ...
            if (list.Count == 0)
            {
                ...
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
```

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
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
```

Accounts follow the same split (`UserAccountRequestArray` then `UserAccountGetByGroup`).

No `Take` / page size / login allow-list in `D:\Prop\src\Mt5`. Dedup is by login dictionary only.

### 3.4 What that fallback means (measured, not hoped)

| `UserRequestArray` retcode | Next step | Cache used? |
|---|---|---|
| `OK` / `OK_NONE` | keep array; if `Total()==0` then `UserLogins` | no |
| `NOTFOUND` | skip `UserGetByGroup`; if empty then `UserLogins` | no |
| any other error | `UserGetByGroup` (cache); if still empty then `UserLogins` | **only as backup** |
| pump = none | cache empty; `UserLogins` still fills | cache cannot complete the census |

So:

- **Primary ALL-traders path = `UserRequestArray`.**
- **Completeness net = `UserLogins` + `UserRequestByLogins`.**
- **`UserGetByGroup` is pump-cache only and is not the ALL path.**

### 3.5 Residual hole (do not greenwash)

`UserGetByGroup` / `UserAccountGetByGroup` **return codes are discarded**. Harmless if `UserRequestArray` succeeded; if the request failed and the cache is empty, `UserLogins` still runs.

If `UserRequestArray` ever returned `OK` with a **truncated non-empty** array, `UserLogins` would not run. MetaQuotes does not document pagination on this call. Live probe (below) returned **6295** users in `demo\yo-2step` in one walk, so this tree has measured a large-group request success. Still: there is no `users.Total()` vs `UserLogins.Length` equality check. That is the remaining completeness risk.

---

## 4. Achiever + Starwave — both brokers, every manager-visible group

### 4.1 Product universe is exactly those two

`D:\Prop\src\Domain\Brokers\BrokerCodes.cs`: `ACHIEVER`, `STARWAVEFX`.

`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs:20–49` builds **two** `NativeMt5BrokerConnector` instances (Achiever + HTTP proxy env; StarwaveFX direct). No other live source-MT5 brokers are registered. Factory does **not** apply `MT5_DEFAULT_GROUP` / plan maps / `Take`.

`D:\Prop\src\Infrastructure\DependencyInjection.cs:45–46` registers every connector from that factory. Dummy/fake is refused when passwords are missing (`:35–36`).

### 4.2 Ingest asks for the entire book

`D:\Prop\src\Application\Ingestion\DealIngestionService.cs:47` and `:61`:

```csharp
var accounts = await connector.GetAccountsAsync(null, ct);
```

`null` = every group from `GetGroupsCore()` = every name `GroupRequestArray("*")` returned.

`LiveIngestHostedService` (`D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`) iterates `registry.All()` (both brokers), `SyncCatalogAsync` then `SyncBrokerAsync`. Scoring walks `store.ListLoginsAsync` — whatever the catalog upserted — with **no** first-N cap.

Dashboard `GetTradersAsync` (`EfDashboardQueries.cs:85–128`) returns **all** `Mt5Accounts` rows (optional broker/state filter only). The only `Take` in that file is `Take(20)` on risk reject reasons (`:204`), not the trader census.

### 4.3 Measured live census (not a theory)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe `LiveBrokerProbe` utc `2026-08-18T08:42:16.8519545+00:00`. Passwords not written.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK (HTTP proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | OK (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` **6295**, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups: `Starwave\cent\FX1\grp1` 11, `grp2` 4; `Starwave\demo\FX2\grp1` 170, `grp2` 1735; `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

These are **all groups those two manager logins can see**. Groups outside the manager ACL cannot appear (`GroupRequestArray` is already ACL-filtered). Empty groups (`demo\yo-instant`, several Starwave real grps) are listed — the walk is not “skip empty”.

Write-up: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` (path recorded as `GroupRequestArray` + `UserRequestArray`).

---

## 5. Copy to cTrader — no live orders, no capital at risk

| Gate | State | Evidence |
|---|---|---|
| Runtime flag | `RealCopyEnabled = false` hardcoded | `DependencyInjection.cs:38–42`; `CTraderFixLogonHostedService.cs:68` forces it false again after logon |
| Options default | `RealCopyExecutionEnabled = false` | `CTraderFixOptions.cs:32–35` |
| FIX builder | only `35=A` Logon | `CTraderFixSession.cs:96` `(35, "A")`. No `35=D` constructor |
| Grep `D:\Prop\src` | no `SendTrade` / `DealerSend` / `OrderAdd` / `TradeRequest` / `35=D` | only comments + “NewOrderSingle still disabled” |
| Copy persistence | `Status = "SHADOW_ONLY"` + `ShadowCopyEngine.SimulateEntry` | `EfTradingStore.cs:307–319` |
| Status snapshot | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` | `LiveRuntimeStatus.cs:42–43` |

`CTraderFixLogonHostedService` may open QUOTE (5211) and TRADE (5212) **logon only**. Log line: `"NewOrderSingle still disabled"`. A TRADE logon without `35=D` cannot place an order.

Dashboard `Take(20)` / reconstructed-trades HTTP `Take(200)` (A014) are **display** caps, not Manager enumeration and not FIX send.

---

## 6. Stale reports — do not reuse

| Report | Why stale vs this slot |
|---|---|
| `A001_native_connector.md` | Says traders = `UserGetByGroup` only; `UserRequestArray` unused; no pump-none Connect. All three are **false** on today’s `NativeMt5BrokerConnector.cs`. |
| `A39` C++ wrapper note | Still true for YoPips C++ (`GroupTotal` only). Not true for Prop C# (`GroupRequestArray` first). |

R010’s table (`UserGetByGroup` = cache, `UserRequestArray` = network) remains correct.

---

## 7. Checklist vs the assigned goal

- [x] `UserGetByGroup` confirmed **pump-cache** (SDK + Admin absence + YoPips comments + unused in C++ src).
- [x] `UserRequestArray` confirmed **request path** and **primary ALL-traders enumerator** in Prop C#.
- [x] Completeness net: `UserLogins` + `UserRequestByLogins` if the user array is empty (covers no-pump).
- [x] Groups: `GroupRequestArray("*")` first; not plan-map filtered.
- [x] Brokers: Achiever + StarwaveFX only; ingest `GetAccountsAsync(null)` for both.
- [x] Measured: 18 groups, 8460 manager traders (2026-08-18 probe).
- [x] cTrader copy cannot send live orders (`35=A` only; flags false; shadow-only intents).
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.

---

## 8. Risk to capital

**None from this path.** Manager walk is read-only (`User*` / `Group*` / `DealRequest*` / `PositionRequest*`). Destination FIX does not emit `NewOrderSingle`. `RealCopyEnabled` is forced false. A catalog/ingest miss is a **completeness** risk (missed trader on the dashboard), not a live-order risk.
