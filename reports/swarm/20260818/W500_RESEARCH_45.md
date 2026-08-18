# W500_RESEARCH_45 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **45** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 45 |
| Topic | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. Logins listed only as group **counts**. |
| This slot live-attached | **No.** Census is the same-day `LiveBrokerProbe` artifact already on disk. |

**Honesty rule:** A001 (`A001_native_connector.md`) is **stale**. It describes a prior C# connector that walked only `UserGetByGroup` / `UserAccountGetByGroup`. The file on disk today is request-first. Do not greenwash “EX5 decompiled”, “≥95% parity”, or live copy.

---

## 0. Verdict

| Claim | Result | Evidence |
|---|---|---|
| `UserGetByGroup` is **pump-cache** | **CONFIRMED** | SDK pairing + section placement; `PUMP_MODE_USERS` fills it; Admin API has **no** `UserGetByGroup` and **no** `PUMP_MODE_USERS`; YoPips C++ `src\` never calls it |
| `UserRequestArray` is the **network request** enumerator | **CONFIRMED** | SDK section `//--- clients and trade accounts request` at `MT5APIManager.h:407–411`; same method exists on `IMTAdminAPI` (no user pump) |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** | `ReadAccountsForGroup` L223 |
| `UserGetByGroup` is only a **hard-fail fallback** | **CONFIRMED** | Called only when request retcode is not `OK` / `OK_NONE` / `NOTFOUND` |
| Empty array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** | L227–232 |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** | 18 groups / 8460 logins (ACL-visible). Probe UTC `2026-08-18T08:42:16Z` |
| Copy to cTrader sends live orders | **NO** | `SAFE_BY_ABSENCE` — no `35=D` builder; `RealCopyEnabled` forced `false` |

**One-liner:** `Get*` = local pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls `UserRequestArray` for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

---

## 1. SDK naming law (Manager API 5570)

Header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Pin: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"`.  
Same declarations in `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

`Connect` takes a pump mask. Cache `Get*` / `Total` / `Next` only fill when the matching bit was accepted:

```124:144:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      PUMP_MODE_ACTIVITY      =0x00000002,   // pump users online activity
      // ...
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      // ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

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

### 1.3 `UserGetByGroup` sits with cache/sink APIs, not the request block

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

Sibling cache batch for balances:

```742:743:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserAccountGetByGroup(LPCWSTR mask,IMTAccountArray* accounts)=0;
   virtual MTAPIRES  UserAccountGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTAccountArray* accounts)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty. `UserRequestArray` / `UserLogins` do **not** require that bit.

### 1.4 Admin API has the request enumerator and **no** user-cache Get

`IMTAdminAPI` (`MT5APIManager.h:785`) pumps **only mail/news**:

```789:795:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      //--- enumeration ranges
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

It has **no** `PUMP_MODE_USERS` and **no** `UserGetByGroup`. It **does** expose:

```1172:1173:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
```

If `UserGetByGroup` were a server pull it would exist on Admin. It does not. It is manager-pump cache only.

Header grep (`UserGetByGroup` under both trees):

| Path | Hits |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | **1** (L672, `IMTManagerAPI` only) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | **1** (same) |
| YoPips `src\` | **0** |

`UserRequestArray` exists twice in the header (Manager L410 + Admin L1173). YoPips `src\` never calls it.

---

## 2. YoPips C++ confirms the same Get=cache / Request=network law

Grep of `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` for `UserGetByGroup` / `UserRequestArray`: **0 / 0**.

Login universe is **`UserLogins`** (network), not cache:

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

Account snapshot is explicitly documented as cache-then-request:

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

`terminal_state_service.cpp` repeats: there is no `PUMP_MODE_ACCOUNTS`; `UserAccountGet` is empty until warmed; miss falls through to a synchronous network request.

Connect in YoPips also retries **no-pump** when pump connect fails (`mt5_manager.cpp` L114–134): “request API available”. Same fallback exists in Prop C# (`ConnectCore` L89–111).

Group listing in YoPips is still **cache** `GroupTotal`/`GroupNext` (`GetAllGroups` L962–981). That is the **old** wrapper. Prop C# is the path that implements A39’s request-API fallback (`GroupRequestArray("*")` first).

---

## 3. Prop C# live path (current disk)

Grep under `D:\Prop\src\*.cs`: the **only** `UserRequestArray` / `UserGetByGroup` / `UserLogins` calls are `NativeMt5BrokerConnector.cs`.

### 3.1 Brokers registered = Achiever + Starwave only

`LiveMt5Registration.CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances (`BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx`). DI (`DependencyInjection.cs` L45–46) registers those and **never** `FakeMt5BrokerConnector`. Dummy seed is refused: `HasRealPasswords` must be true or startup throws.

### 3.2 Pump is attempted, then dropped — cache cannot be the ALL path

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

### 3.3 ALL traders: `GetAccountsAsync(null)` → every group → `UserRequestArray`

Ingest:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`null` group = entire book (`GetAccountsCore` L194–203 walks every name from `GetGroupsCore`). Groups themselves come from `GroupRequestArray("*")`, then cache `GroupTotal`/`GroupNext` only if the request array is empty. No `Take` / `Skip` / `pageSize` / plan-map filter in this file (grep on `NativeMt5BrokerConnector.cs`: **0**).

Per group:

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

| Step | Call | Kind | When |
|---|---|---|---|
| 1 | `UserRequestArray(gname, users)` | **network / ALL records in group** | always first |
| 2 | `UserGetByGroup(gname, users)` | **pump cache** | only unexpected retcode (not OK / OK_NONE / NOTFOUND) |
| 3 | `UserLogins` + `UserRequestByLogins` | **network** | only if `users.Total() == 0` |

So: **`UserRequestArray` is the ALL-traders request path.** `UserGetByGroup` is **not** the primary enumerator. Using it alone on a `PUMP_MODE_NONE` session would silently return **zero** traders.

A001’s “Zero hits for `UserRequestArray` under `D:\Prop\src`” is **false on current disk**.

### 3.4 Residual completeness (not a cache-vs-request confusion)

| Residual | Why it is not “UserGetByGroup is the request path” |
|---|---|
| `UserGetByGroup` fallback | Only on hard `UserRequestArray` failure. If that happens **and** `_pumpEnabled==false`, step 2 is empty; step 3 (`UserLogins`) still request-path. |
| Partial `UserRequestArray` accepted | `UserLogins` runs only when `users.Total() == 0`. If the request returned some but not all users, missing logins are not recovered. MetaQuotes does not document pagination. Live probe returned **6295** users in `demo\yo-2step` in one walk — large-group request works on this tree. There is still **no** `users.Total() == UserLogins.Length` assert. |
| `UserGetByGroup` / `UserAccountGetByGroup` retcodes discarded | Harmless if the request succeeded; if request failed and cache is empty, `UserLogins` still runs. |
| Manager ACL | Server hides groups this manager cannot see. “ALL” = all **visible** groups/logins, not the broker’s entire universe. |
| Per-group vs mask `"*"` | Product walks each discovered name. Equivalent to `UserRequestArray("*")` / `UserLogins("*")` if the group list is complete. |

---

## 4. Measured census (same day, not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` — `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Passwords never written.

UTC `2026-08-18T08:42:16.8519545+00:00`.

| Broker | Connect | Groups | Traders | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK via HTTP proxy, 7213 ms | **8** | **6512** | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK direct, 6413 ms | **10** | **1948** | 478 | same |

**Total: 18 groups, 8460 manager traders.**

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `Starwave\demo\FX2\grp1` 170, `grp2` 1735, `Starwave\real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `Starwave\real\FX3\LP` 2.

Empty groups (`demo\yo-instant`, three Starwave real groups) are **present** in the group list — the walk did not drop zero-account groups. That is ALL **visible** groups.

This slot did **not** re-attach. Counts are the probe file, not a new Connect.

---

## 5. Copy to cTrader does not send live orders (no loss)

Goal constraint: fetch ALL traders **and** do not put capital at risk.

| Gate | State | Evidence |
|---|---|---|
| `RealCopyEnabled` | **forced false** | `DependencyInjection.cs` L40–41: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.” |
| Hosted FIX logon | **35=A only**; flag reset false | `CTraderFixLogonHostedService.cs` L68–70 |
| Options default | `RealCopyExecutionEnabled = false` | `CTraderFixOptions.cs` L32–35 |
| Session builder | only outbound MsgType is **`A`** | `CTraderFixSession.BuildLogon` L96 `(35, "A")` |
| `35=D` / `(35, "D")` / `MsgType="D"` in `Fix.CTrader` | **0 hits** | no NewOrderSingle encoder |
| `SendTrade` / `DealerSend` / `OrderSend` in `D:\Prop\src` | **0 hits** | Manager path is read-only |
| Shadow | in-memory simulate | `ShadowCopyEngine.SimulateEntry` — no socket |
| FIX worker | even if flag flipped, **no sender** | `apps/fix-worker/Worker.cs` L45–46 logs refuse; stamps TRADE `NewOrderSingle remains off` |
| Settings API | exposes `runtime.RealCopyEnabled` | cannot emit `35=D` because there is no builder |

`CTraderFixSession.TryLogonAsync` writes one TLS Logon and reads one reply. Tags: 35=A, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. No OrderQty. No ClOrdID send. TRADE logon is session proof / future recon, **not** copy.

Manager `UserRequestArray` / `DealRequest*` / `PositionRequest*` are **reads**. They do not place destination orders.

**Honest split:** wanting live copy **and** no loss is not deliverable today. Copy requires a `35=D` that does not exist. No-loss currently holds by **`SAFE_BY_ABSENCE`**, not by a tested refuse-on-LoggedOn-TRADE gate. Do not add a sender in this slot.

---

## 6. Stale reports (do not reuse)

| File | Why stale |
|---|---|
| `A001_native_connector.md` | Says traders = `UserGetByGroup` only; `UserRequestArray` unused; no pump-none Connect. All three are **false** on today’s `NativeMt5BrokerConnector.cs`. |
| Early `W500_SLICE_0.md` | Same old cache-only walk. Later slices (`W500_SLICE_100`+) already describe request-first. |

R010’s table (`UserGetByGroup` = cache, `UserRequestArray` = network) remains correct.

---

## 7. Checklist

- [x] `UserGetByGroup` confirmed **pump-cache** (SDK pairing + Admin absence + YoPips comments + unused in C++ `src`).
- [x] `UserRequestArray` confirmed **request path** and **primary ALL-traders enumerator** in Prop C#.
- [x] Ingest asks `GetAccountsAsync(null)` for **both** Achiever and Starwave.
- [x] Same-day measured census: **8/6512 + 10/1948 = 18/8460**.
- [x] Copy path cannot send live orders (`35=D` absent; flag false).
- [x] No secrets printed. Product source not edited.
- [ ] Optional harden (not done this slot): assert `UserRequestArray.Total() == UserLogins.Length` per group and log retcodes of the cache fallback.
- [ ] This slot did not re-run `LiveBrokerProbe`.

---

## 8. Files read

| Path | Why |
|---|---|
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | SDK Get/Request/pump/Admin |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | live walk |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | two brokers |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | catalog host |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logon, no send |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | flag default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | copy note |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | simulate only |
| `D:\Prop\apps\fix-worker\Worker.cs` | refuse even if flag true |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | UserLogins + UserAccountGet comments |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | census tool |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | measured 18/8460 |
| `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md` | C# cache/request table |

---

**Slot 45 verdict: CONFIRMED.** `UserGetByGroup` is pump-cache. `UserRequestArray` is the request path for ALL manager-visible traders. Achiever + Starwave are fetched that way. cTrader copy cannot send live orders yet.
