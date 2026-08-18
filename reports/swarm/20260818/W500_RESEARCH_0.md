# W500_RESEARCH_0 — Manager request APIs without pump; ALL Achiever+Starwave traders; no live cTrader orders

| Field | Value |
|---|---|
| Slot | **0** |
| Agent | W500_RESEARCH_0 |
| Date | 2026-08-18 |
| Product source modified | **No.** Read-only. |
| Secrets printed | **None.** No manager / proxy / FIX passwords. |

**Assigned topic:** Read `MT5APIManager.h` `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `PositionRequestByGroup`, `DealRequestByGroup`. Confirm request APIs work **without pump**. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy to cTrader must **not** send live orders yet (no loss).

---

## 0. Verdict (do not greenwash)

| Claim | Measured |
|---|---|
| The five named calls are **network request** APIs, not pump-cache `Get`/`Next` | **Yes.** Header naming + YoPips/Prop wrappers treat `*Request*` / `UserLogins` as valid after `Connect(..., pump_mode=0)`. |
| They exist on `IMTManagerAPI` (the interface this product uses) | **Yes.** All five. `GroupRequestArray` is **Manager-only** — `IMTAdminAPI` does **not** have it. |
| They require `PUMP_MODE_GROUPS` / `USERS` / `POSITIONS` | **No.** Pump fills the **local cache** used by `GroupTotal`/`GroupNext`/`UserGet*`/`PositionGet*`. Request APIs pull from the server. |
| C# live path already uses the request APIs | **Yes.** `NativeMt5BrokerConnector` (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`). |
| C++ `MT5Manager::GetAllGroups` is no-pump complete | **No.** It walks `GroupTotal`/`GroupNext` only. Empty cache after pump-none can look like “zero groups.” |
| ALL manager-visible Achiever+Starwave traders already fetched once | **Yes — live census 2026-08-18.** Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460**. Path: `GroupRequestArray` + `UserRequestArray`. |
| Copy to cTrader can place a live order today | **No.** No `35=D` builder. `RealCopyEnabled` pinned **false**. CopyIntents are `SHADOW_ONLY`. **`SAFE_BY_ABSENCE`.** |

**One-liner:** Request APIs (`GroupRequestArray("*")` → `UserRequestArray` / `UserLogins` → `PositionRequestByGroup` / `DealRequestByGroup`) enumerate every group and login the two manager ACLs allow, **without pumping**. That fetch cannot open a cTrader position because NewOrderSingle does not exist.

---

## 1. Header pin

File: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Same text at: `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h`  
Version: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"`.

Connect always takes a `pump_mode` bitfield. **Zero bits = no pump.**

```164:164:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  Connect(LPCWSTR server,uint64_t login,LPCWSTR password,LPCWSTR password_cert,uint64_t pump_mode,uint32_t timeout=INFINITE)=0;
```

`IMTManagerAPI::EnPumpModes` (`L124–144`):

| Flag | Value | What it pumps into the **local cache** |
|---|---|---|
| `PUMP_MODE_USERS` | `0x00000001` | users → `UserGet` / `UserTotal` / `UserGetByGroup` |
| `PUMP_MODE_ORDERS` | `0x00000008` | orders → `OrderGet*` |
| `PUMP_MODE_POSITIONS` | `0x00000080` | positions → `PositionGet*` |
| `PUMP_MODE_GROUPS` | `0x00000100` | group configs → `GroupTotal` / `GroupNext` / `GroupGet` |
| `PUMP_MODE_FULL` | `0xffffffff` | everything |
| *(no bits)* | `0` | **request-only session** |

The C++ enum does **not** name `PUMP_MODE_NONE`. The C# wrapper does: `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE` (used at `NativeMt5BrokerConnector.cs:101`). Web-API sample also names `PUMP_MODE_NONE = 0x00000000` (`Examples\Web\NET\MetaQuotes.MT5WebAPI\MT5WebAPI.cs`).

`IMTAdminAPI::EnPumpModes` (`L789–795`) has only `MAIL` / `NEWS` / `FULL`. Admin is **not** this product’s connect path (`CreateManager` → `IMTManagerAPI` / `CIMTManagerAPI`).

---

## 2. The five assigned APIs (signatures)

### 2.1 `IMTManagerAPI` (use this)

```211:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```254:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```408:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

```520:535:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  PositionRequestByGroup(LPCWSTR group,IMTPositionArray* positions)=0;
```

Related request siblings (same “no pump” family):

| Call | Line | Role |
|---|---|---|
| `GroupRequest(name)` | 208 | one group by exact name |
| `UserRequest(login)` | 252 | one user |
| `UserRequestByLogins(logins, n)` | 671 | hydrate users after `UserLogins` |
| `UserAccountRequest(login)` | 261 | one account snapshot |
| `PositionRequest(login)` | 282 | open positions for one login |
| `DealRequest(login, from, to)` | 270 | deals for one login |
| `DealRequestByGroupSymbol` | 735 | deals filtered by group+symbol |

### 2.2 `IMTAdminAPI` (do not use for this product)

| Call | Admin line | Present? |
|---|---|---|
| `UserLogins` | 1172 | yes |
| `UserRequestArray` | 1173 | yes |
| `DealRequestByGroup` | 1099 | yes |
| `PositionRequestByGroup` | 1268 | yes |
| `GroupRequestArray` | — | **NO** |
| `GroupRequest` | — | **NO** |

Admin enumerates groups via `GroupTotal`/`GroupNext` (admin config cache). Discovery for manager login `2027` / `9904` must stay on **`IMTManagerAPI`**.

### 2.3 Mask language (same for all five `group` / `mask` args)

`CMTStr::CheckGroupMask` (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` L775–809): comma-separated templates, `*` wildcards, leading `!` = exclude.

| Mask | Meaning |
|---|---|
| `*` | every group **this manager ACL already allows** |
| `demo\*` | demo subtree only |
| `demo\yo-2step` | one exact group |
| `demo\*,!demo\yo-instant` | demo minus instant |

`*` is “all **visible** groups,” not “bypass ACL.” Extra server groups the manager cannot see will not appear. That is correct.

### 2.4 Output objects

`GroupRequestArray` writes an `IMTConGroupArray` (`MT5APIConfigGroup.h` L800–821): `Total()` / `Next(i)` → `IMTConGroup*`. Caller `GroupCreateArray()` then `Release()`.

`UserLogins` is the odd one: **server allocates** `uint64_t*`; caller **must** `IMTManagerAPI::Free`. Empty/`MT_RET_OK_NONE` can yield a null pointer — C++ `GetUserLogins` treats that as **false** (stricter than “zero logins”).

`from`/`to` on `DealRequestByGroup` are Unix seconds.

---

## 3. Request vs pump (do not mix)

SDK convention is consistent across this header:

| Pattern | Source | Pump required for a **complete** result? |
|---|---|---|
| `GroupTotal` / `GroupNext` / `GroupGet` | local cache | **Yes** (`PUMP_MODE_GROUPS`) |
| `GroupRequest` / `GroupRequestArray` | network | **No** |
| `UserGet` / `UserGetByGroup` / `UserGetByLogins` | local cache | **Yes** (`PUMP_MODE_USERS`) |
| `UserRequest` / `UserRequestArray` / `UserRequestByLogins` / `UserLogins` | network | **No** |
| `UserAccountGet` / `UserAccountGetByGroup` | local cache | **Yes** |
| `UserAccountRequest` / `UserAccountRequestArray` | network | **No** |
| `PositionGet` / `PositionGetByGroup` | local cache | **Yes** (`PUMP_MODE_POSITIONS`) |
| `PositionRequest` / `PositionRequestByGroup` | network | **No** |
| `DealRequest` / `DealRequestByGroup` | network (history; no deal pump cache used for dump) | **No** |

Wrapper comments already encode this:

```339:344:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    // Cache-first: UserAccountGet reads the in-memory pump cache (sub-ms) and
    // works only when this login's group is pump-synchronized. Fall back to the
    // network UserAccountRequest when the cache misses (no pump, or login not in
    // the synchronized scope).
```

```118:134:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
        // Pump mode failed — retry with no subscriptions (mode=0).
        // GetDeals / DealRequest works without the pump; this lets journal
        // sync and other request-only operations function even when the pump
        // connection is unavailable (IP not yet whitelisted for pump, etc.)
        ...
        spdlog::warn("MT5 connected in no-pump mode — real-time events disabled, request API available");
```

```75:77:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
    // Connect WITHOUT pump mode - this is a request-handling session
    MTAPIRES res = m_manager->Connect(..., 0, timeoutMs); // mode=0 means no pump
```

YoPips `mt5_group_probe.cpp` L113: “No pump mode required for group enumeration.” That comment is **intent**. The C++ `MT5Manager::Connect(0)` **remaps** `0` to `USERS|ORDERS|POSITIONS|SYMBOLS` first, then retries true `0`. Pool sessions skip the remap and are the clean pump-none path.

---

## 4. What the product actually calls

### 4.1 C# native connector (the live census path) — request-first

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`

Connect (`L88–111`):

1. First try pump `GROUPS|USERS|POSITIONS`.
2. On any fail, **retry `PUMP_MODE_NONE`**.
3. Stay connected either way. Fetch does **not** switch to `GroupNext`.

Then:

| Product method | SDK call | Pump? |
|---|---|---|
| `GetGroupsCore` | `GroupRequestArray("*")`; **only if empty**, walk `GroupTotal`/`GroupNext` | Request first |
| `ReadAccountsForGroup` | `UserRequestArray(gname)` → if fail `UserGetByGroup` → if still empty `UserLogins` + `UserRequestByLogins` | Request first |
| balances | `UserAccountRequestArray` → fallback `UserAccountGetByGroup` | Request first |
| `GetGroupDealsCore` | `DealRequestByGroup` in 14-day windows | Request only |
| `GetGroupPositionsCore` | `PositionRequestByGroup` → fallback `PositionGetByGroup` | Request first |

`GetAccountsAsync(null)` walks **every** name from `GetGroupsAsync` (no `Take(200)`, no plan-env filter).

`LiveMt5Registration` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`) builds **two** `NativeMt5BrokerConnector`s: Achiever (optional HTTP proxy) + Starwave (proxy off). FakeMt5 is **not** registered when real passwords exist (`DependencyInjection.cs` L35–36 throws if secrets are placeholders).

`DealIngestionService.SyncCatalogAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L37–50): `GetGroupsAsync` + `GetAccountsAsync(null)` = ALL groups + ALL traders the manager can see. `SyncBrokerAsync` then uses `DealRequestByGroup` per group and `PositionRequestByGroup("*")`.

### 4.2 C++ YoPips / Prop wrapper — request for logins, **cache** for groups

Both trees implement `UserLogins` (`mt5_manager.cpp` ~315–327, `mt5_pool.cpp` ~212–223). That is a true request API and is what `GetGroupLogins` aliases.

`GetAllGroups` / `GetGroupDetails` **never** call `GroupRequestArray`. They only `GroupTotal`+`GroupNext`. After a no-pump connect that cache can be empty while groups exist. **Do not use the C++ probe as the ALL-groups collector unless `GroupRequestArray("*")` is added or `PUMP_MODE_GROUPS` is set.**

`GroupRequestArray` / `UserRequestArray` / `PositionRequestByGroup` / `DealRequestByGroup` have **zero** hits under `D:\Prop\mt5-sdk\src` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`. They exist only in the vendor header (and now in the C# connector).

---

## 5. Recipe: ALL Achiever + ALL Starwave manager traders (no pump)

```
for each broker { Achiever, Starwave }:
  CreateManager (not CreateAdmin)
  ProxySet HTTP only for Achiever on this LAN (allow-list egress)
  Connect(host:443, managerLogin, password, password_cert="", pump_mode=0)
  arr = GroupCreateArray()
  GroupRequestArray(L"*", arr)          // ALL visible groups
  for i in [0, arr.Total()):
      name = arr.Next(i)->Group()
      persist Mt5Group(broker, name)
      UserRequestArray(name, users)     // ALL users in that group
      if users.Total()==0:
          UserLogins(name, logins, n)   // login list only
          UserRequestByLogins(logins, n, users)
      UserAccountRequestArray(name, accounts)   // optional balances
      persist every login as (broker_id, login)  // logins are NOT globally unique
      DealRequestByGroup(name, from, to, deals)  // optional history
  PositionRequestByGroup(L"*", positions)        // optional open book
  Disconnect
```

Two process env sets (C++ `AppConfig` is single-broker). C# `LiveMt5Registration` already holds both.

Non-secret connect identities (architecture + prior A004; **no passwords**):

| Broker | Host:port | Manager login | Proxy on this LAN |
|---|---|---|---|
| Achiever | `57.128.141.65:443` | `2027` | **HTTP required** (else **1012**) |
| StarwaveFX | `84.201.6.142:443` | `9904` | **off** |

Do **not** pass `MT5_GROUP_*` / `demo\yo-2step,Flexy\...` as the discovery mask. That is the write-path plan map, a **subset**.

Do **not** treat `GroupTotal()==0` on a no-pump session as “broker has no groups.”

---

## 6. Measured live census (already done via these APIs)

Source: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`  
Dump: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`  
UTC in dump: `2026-08-18T08:42:16Z`. Passwords not written.

| Broker | Connect | Groups | Traders | Open positions | Fetch path |
|---|---|---:|---:|---:|---|
| ACHIEVER | OK (HTTP proxy) | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | OK (direct) | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever names (manager-visible set): `contest\yo-1step` (2), `contest\yo-2step` (179), `contest\yo-instant` (4), `contest\yo-payp` (5), `demo\yo-1step` (4), `demo\yo-2step` (6295), `demo\yo-instant` (0), `demo\yo-payp` (23).

Starwave names: `Starwave\cent\FX1\grp1` (11), `grp2` (4); `Starwave\demo\FX2\grp1` (170), `grp2` (1735); `Starwave\real\FX3\grp1` (22), `grp2` (0), `grp3` (0), `grp4` (4), `grp5` (0), `Starwave\real\FX3\LP` (2).

Empty groups (`demo\yo-instant`, three Starwave real grps) still count as **discovered**. ALL means names + logins, including zeros.

Dashboard `/api/traders` = 8460, `/api/groups` = 18 (`CREDENTIALS_AND_COPY_STATUS.md`).

**Honesty on pump bit for that run:** `LIVE_GROUPS_AND_TRADERS.json` does **not** record `PumpEnabled`. The connector **tries pump first**, then `PUMP_MODE_NONE`. The **fetch** path is still `GroupRequestArray` / `UserRequestArray`, so the census is valid even if pump succeeded. Header + pool + `DealRequest` comment independently prove the same calls work at `pump_mode=0`. This slot did **not** re-attach live.

---

## 7. Copy to cTrader must not send live orders (no loss)

Goal is **catalog + shadow**, not a live book.

| Gate | Measured state |
|---|---|
| `35=D` / `(35, "D")` / `MsgType="D"` in `D:\Prop\src\Fix.CTrader` | **0 hits** |
| Only FIX builder | `CTraderFixSession.BuildLogon` → `(35, "A")` (Logon) |
| `RealCopyEnabled` | hardcoded `false` in `DependencyInjection.cs` L40–41; reset `false` again after logon (`CTraderFixLogonHostedService.cs` L68) |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled = false` |
| `/api/settings` | exposes `runtime.RealCopyEnabled` (always false) |
| CopyIntent persist | `Status = "SHADOW_ONLY"` (`EfTradingStore.cs` L307) |
| Shadow engine | `SimulateEntry` / `SimulateExit` — no socket |
| Native MT5 connector | **no** `DealerSend` / `DealerBalance` / `OrderAdd` / `TradeBalance` |
| fix-worker | even if config flag true, **no sender**; stamps `NewOrderSingle remains off` |
| LiveBrokerProbe | MT5 read only; does not touch FIX |

`SAFE_BY_ABSENCE`: flipping `REAL_COPY_EXECUTION_ENABLED` **cannot** place an order because there is no NewOrderSingle encoder. Logon `35=A` is session proof, not copy.

Risk to **our** capital from this fetch: **none**. Request APIs are reads. Shadow rows are paper. The only venue write that can exist today is FIX Logon, which does not open exposure.

Risk to **source** traders: none — we do not `UserUpdate` / `DealAdd` / `PositionUpdate` on the Manager session in this path.

---

## 8. Failure codes (connect / request)

| Code | Name (typical) | Meaning on this recipe |
|---|---|---|
| 0 | `MT_RET_OK` | row/array filled |
| 1 | `MT_RET_OK_NONE` | empty-but-success (valid) |
| 3 | params / auth-failed on Connect | bad login/password or null array |
| 5 | no connect | host:port / server down |
| 7 | network / timeout | proxy/firewall |
| 13 | `MT_RET_ERR_NOTFOUND` | empty group or unknown name; C# treats as empty |
| 1012 | manager IP blocked | Achiever saw a non-allow-list egress |

C# `Describe` (`NativeMt5BrokerConnector.cs` L442–454) maps 7 / 1012 / 3 / 5 / 9 / 10. Do not “fix” 3 by toggling proxy.

---

## 9. Binding rules for later implementers

1. **Discover with `GroupRequestArray("*")`**, not `GroupTotal`/`GroupNext`, unless `PUMP_MODE_GROUPS` is on and `GroupTotal()>0`.
2. **Users with `UserRequestArray` per discovered name** (or `UserLogins` + `UserRequestByLogins`). Mask `*` is legal for `UserLogins` but the measured collector used per-group `UserRequestArray`.
3. **Positions/deals with `PositionRequestByGroup` / `DealRequestByGroup`.** Do not rely on `PositionGetByGroup` after pump-none.
4. **Stay on `IMTManagerAPI`.** Admin cannot `GroupRequestArray`.
5. **Persist `(broker_id, login)`.** Achiever `1001` ≠ Starwave `1001`.
6. **Do not filter by `MT5_GROUP_*`.** Contest groups (`contest\yo-*`) are live and hold traders.
7. **Do not send `35=D`.** Catalog + SHADOW only until §68/§70 gates + explicit flag. This slot does not lift that gate.

---

## 10. Sources (absolute)

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h`
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` (same 5570)
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` (Connect remap + no-pump retry + `UserLogins` + cache `GetAllGroups`)
- `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` (true `mode=0` + `UserLogins`)
- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` (same Connect / `UserLogins`)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`
- Siblings: A39, A004, A014, A003, E002

---

## 11. Slot-0 close

Request APIs work without pump. Use them to pull every Achiever and Starwave group/login the two manager records can see (already measured **18 / 8460**). Do not pump for this dump. Do not emit a live cTrader order — there is no sender, and the flag stays false so this process cannot take a market loss.
