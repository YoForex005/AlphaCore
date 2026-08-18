# W500_RESEARCH_5 — UserGetByGroup is pump-cache; UserRequestArray is the ALL-traders request path

| Field | Value |
|---|---|
| Slot | **5** |
| Date | 2026-08-18 |
| Agent | W500 research subagent, slot 5 |
| Topic | Confirm `UserGetByGroup` is pump-cache and `UserRequestArray` is the request path for **ALL** traders. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |
| Product source modified | **None.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. |

**Honesty rule:** A001 (`UserGetByGroup` as the *only* C# user walk) is **stale**. This note quotes files as they sit on disk *now*. Do not greenwash “EX5 decompiled” or live copy.

---

## 0. Verdict

| Claim | Result |
|---|---|
| `UserGetByGroup` is the **local pump-cache** enumerator | **CONFIRMED** |
| `UserRequestArray` is the **server request** enumerator for full user records | **CONFIRMED** |
| Product ALL-traders path uses `UserRequestArray` **first** | **CONFIRMED** (`ReadAccountsForGroup`) |
| `UserGetByGroup` is only a **fallback** after a hard request retcode | **CONFIRMED** |
| Empty request array then uses **network** `UserLogins` + `UserRequestByLogins` | **CONFIRMED** |
| ALL Achiever + Starwave groups + ALL manager-visible traders | **CODE + MEASURED CENSUS** — 18 groups / 8460 logins (ACL-visible) |
| Copy to cTrader sends live orders | **NO.** `SAFE_BY_ABSENCE` — no `35=D`, `RealCopyEnabled` forced `false` |

**One-liner:** `Get*` = pump memory; `Request*` / `UserLogins` = server. The live C# collector already pulls the request path for every discovered group on both owned brokers. That is a **read**. It cannot place a cTrader order.

---

## 1. SDK naming law (Manager API 5570)

Header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Pin: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"`.  
Same declarations in `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`.

Connect takes a pump mask. Cache `Get*` / `Total` / `Next` only fill when the matching bit was accepted:

```124:144:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      // ...
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      // ...
      PUMP_MODE_FULL          =0xffffffff    // full pumping
     };
```

Paired **Get (cache)** vs **Request (network)** is consistent across the interface:

| Object | Cache (needs pump bit) | Network (works at `pump=0`) |
|---|---|---|
| One user | `UserGet` h:251 | `UserRequest` h:252 |
| Users by group / mask | **`UserGetByGroup`** h:672 | **`UserRequestArray`** h:410 (and h:1173 on Admin) |
| Users by login list | `UserGetByLogins` h:673 | `UserRequestByLogins` h:671 |
| Login list only | — | `UserLogins` h:254 |
| One account | `UserAccountGet` h:260 | `UserAccountRequest` h:261 |
| Accounts by group | `UserAccountGetByGroup` h:742 | `UserAccountRequestArray` h:411 |
| Groups | `GroupTotal` / `GroupNext` / `GroupGet` h:205–207 | `GroupRequest` / `GroupRequestArray` h:208, 212 |
| Positions | `PositionGet*` | `PositionRequest*` |

`UserRequestArray` sits under the explicit **request** block:

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

`UserGetByGroup` sits with the other **Get** cache batch APIs (same family as `UserGet` / `UserGetByLogins`):

```668:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- trade accounts sinks
   virtual MTAPIRES  UserAccountSubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserAccountUnsubscribe(IMTAccountSink* sink)=0;
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

`PUMP_MODE_USERS = 0x1` is what fills that cache. `PUMP_MODE_NONE` / connect `pump_mode=0` leaves `UserGetByGroup` empty. `UserRequestArray` / `UserLogins` do **not** require that bit.

C# reflection of the same surface (`R010_csharp_manager.md` §3.4 / §5.2):

- cache: `UserTotal` / `UserGet` / **`UserGetByGroup`**
- network: `UserRequest` / **`UserRequestArray`** / `UserLogins` / `UserRequestByLogins`

---

## 2. YoPips production comments (same SDK, live PropFirm backend)

YoPips does **not** call `UserGetByGroup` for census. Login universe is **`UserLogins`** (network):

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

The Get/Request split is documented on the sibling account API in the same file:

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

Same pattern for positions (`PositionGet` = pump cache, `PositionRequest` = network, lines 403–408) and orders (`OrderGetOpen` vs `OrderRequestOpen`). Pool sessions connect `mode=0` so the cache is **normally empty** and the request fallback is the live path (`mt5_pool.cpp` 234–240).

Pump-none fallback (request APIs still work when pump ACL/IP fails):

```114:134:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
    if (res != MT_RET_OK) {
        spdlog::error("MT5 Connect failed: {} — retrying without pump mode", res);
        res = m_manager->Connect(server.c_str(), login, password.c_str(), L"", 0, 30000);
        // ...
        m_pumpMode = false;
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

C# `NativeMt5BrokerConnector.ConnectCore` now mirrors that: pump `GROUPS|USERS|POSITIONS` first, then `PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs` 89–111).

---

## 3. Product C# ALL-traders walk (current disk, not A001)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

### 3.1 Groups first — request, then cache

`GetGroupsCore` (144–187):

1. `GroupRequestArray("*", arr)` — **network**, every group this manager ACL may see.
2. Only if that list is empty: `GroupTotal` / `GroupNext` — **pump cache**.

### 3.2 Accounts — every group, no `Take`

`GetAccountsAsync(null)` → `GetAccountsCore(null)` (189–214):

- `group` null/whitespace → walk **every** name from `GetGroupsCore()`.
- One `ReadAccountsForGroup` per name; de-dupe by login.
- **Zero** `Take` / `Skip` / page size in this file.

Ingest asks for the full book:

```47:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Both owned brokers are constructed; there is no third live source:

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(... BrokerCodes.Achiever ...);
        var starwave = new NativeMt5BrokerConnector(... BrokerCodes.StarwaveFx ...);
        return new IMt5BrokerConnector[] { achiever, starwave };
```

`BrokerCodes`: `ACHIEVER`, `STARWAVEFX` only.

### 3.3 Per-group user fill — request first

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

| Step | API | Kind | When |
|---|---|---|---|
| 1 | `UserRequestArray(gname, users)` | **network / ALL records in group** | always first |
| 2 | `UserGetByGroup(gname, users)` | **pump cache** | only unexpected retcode (not OK / OK_NONE / NOTFOUND) |
| 3 | `UserLogins` + `UserRequestByLogins` | **network** | `users.Total()==0` after 1–2 |
| 4 | `UserAccountRequestArray` then `UserAccountGetByGroup` | request then cache | balances/equity overlay |

So: **`UserRequestArray` is the ALL-traders request path.** `UserGetByGroup` is **not** the primary enumerator. Using it alone on a `PUMP_MODE_NONE` session would silently return **zero** traders.

A001’s “Zero hits for `UserRequestArray` under `D:\Prop\src`” is **false on current disk**.

---

## 4. Measured live census (same connector)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`.  
Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`utc` 2026-08-18T08:42:16Z).  
Write-up: `LIVE_MANAGER_FETCH_MEASURED.md`.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK (HTTP proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | OK (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (accounts): `contest\yo-1step` 2, `contest\yo-2step` 179, `contest\yo-instant` 4, `contest\yo-payp` 5, `demo\yo-1step` 4, `demo\yo-2step` 6295, `demo\yo-instant` 0, `demo\yo-payp` 23.

Starwave groups (accounts): `Starwave\cent\FX1\grp1` 11, `grp2` 4, `demo\FX2\grp1` 170, `grp2` 1735, `real\FX3\grp1` 22, `grp2` 0, `grp3` 0, `grp4` 4, `grp5` 0, `LP` 2.

**ALL means every group/login this manager login is authorized to see.** Server ACL is already applied. Admin-unrestricted “every group on the box” would be `IMTAdminAPI`, which this product does **not** use.

Empty groups (`demo\yo-instant`, several `Starwave\real\FX3\*`) are kept. That is completeness, not a miss.

Dashboard `/api/trades` still `Take(200)` — reconstructed-trade **page**, not Manager census (A014). Ingest / connector have no account `Take`.

---

## 5. Copy to cTrader must not send live orders (no loss)

Goal is catalog + shadow / logon-only. Live NewOrderSingle is **off**.

| Gate | Measured |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** (`CTraderFixOptions.cs` 32–35) |
| DI arming | `LiveRuntimeStatus.RealCopyEnabled = false` with comment “Do not arm a flag that cannot be honored safely.” (`DependencyInjection.cs` 38–41) |
| FIX logon host | forces `_runtime.RealCopyEnabled = false`; log “NewOrderSingle still disabled” (`CTraderFixLogonHostedService.cs` 68–71) |
| FIX session builder | `35=A` Logon only. No `35=D` / `(35, "D")` / `MsgType="D"` under `D:\Prop\src\Fix.CTrader` (**0 hits**) |
| FIX worker | even if config true, **no sender**; logs refuse (`apps\fix-worker\Worker.cs` 21–46) |
| Settings API | `FEATURE_COPY_TRADING_ENABLED = false`; `REAL_COPY_EXECUTION_ENABLED` = runtime flag (forced false) |
| Shadow | `ShadowCopyEngine` simulates fills in-process; no socket |

`CTraderFixSession.BuildLogon` emits tags 35=A, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. That is **session proof**, not copy.

Manager `UserRequestArray` / `DealRequest*` / `PositionRequest*` are **reads**. They do not call `DealerSend` / `SendTrade` (those exist in YoPips C++ PropFirm execution, **not** on this C# ingest path).

**Risk to capital from this slot: none.** Fetching 8460 logins cannot open a Pepperstone ticket. Live send remains `SAFE_BY_ABSENCE`. Do **not** add a `35=D` builder to “finish copy.”

---

## 6. Completeness / residual holes (not a refutation)

| Residual | Why it is not “UserGetByGroup is the request path” |
|---|---|
| Manager ACL | Server already filters. ALL = visible set. |
| `UserGetByGroup` fallback | Only on hard `UserRequestArray` failure. If that happens **and** `_pumpEnabled==false`, step 2 is empty; step 3 (`UserLogins`) still request-path. |
| `GroupRequestArray` empty → cache groups | Same Get/Request split. Pump-none + empty request array → 0 groups → 0 traders. Not observed on the 2026-08-18 probe. |
| Per-group vs mask `"*"` | Product walks each discovered name. Equivalent to `UserRequestArray("*")` / `UserLogins("*")` if the group list is complete. |
| Account overlay | `UserAccountRequestArray` is request; `UserAccountGetByGroup` cache fallback. Missing account row → balance/equity 0, login still counted from `CIMTUser`. |
| A001 / W500_SLICE_0 quotes | Stale connector (cache-only users). Superseded by A014 + this file. |

---

## 7. Sources (quoted, not modified)

- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Domain\Brokers\BrokerCodes.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (counts only; no passwords)
- `D:\Prop\reports\swarm\20260818\A014_live_path_now.md`
- `D:\Prop\reports\swarm\20260818\R010_csharp_manager.md`
- `D:\Prop\reports\swarm\20260818\A39_mt5_group_discovery.md`

---

## 8. Slot-5 contract

| Field | Value |
|---|---|
| `slot` | 5 |
| `verdict` | **CONFIRMED** |
| `evidence` | SDK `UserGetByGroup` (h:672) is pump-cache (`PUMP_MODE_USERS`); `UserRequestArray` (h:410) is the request enumerator. C# `ReadAccountsForGroup` calls `UserRequestArray` first, then cache `UserGetByGroup` only on hard fail, then `UserLogins`+`UserRequestByLogins`. Live probe: Achiever 8/6512 + Starwave 10/1948. |
| `risk_to_capital` | **none** — Manager census is read-only; no `35=D`; `RealCopyEnabled` forced false. |
