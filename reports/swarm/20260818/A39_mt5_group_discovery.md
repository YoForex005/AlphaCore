# A39 — Dynamically enumerate ALL groups for a manager login

**Agent:** A39  
**Date:** 2026-08-18  
**Product source modified:** none (read-only)  
**Sources (quoted, not modified):**

- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`IMTManagerAPI`, `IMTAdminAPI`, factory)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h` (`IMTConGroup`, `IMTConGroupArray`, `IMTConGroupSink`)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigManager.h` (`IMTConManager` allowed-group ACL)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIConstants.h` (`MTAPIRES`)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` (`CMTStr::CheckGroupMask`)
- `D:\Prop\mt5-sdk\src\core\imt5_client.h`, `mt5_types.h`, `mt5_manager.{h,cpp}`, `mt5_pool.{h,cpp}`, `mt5_http_client.{h,cpp}`
- `D:\Prop\mt5-sdk\src\services\mt5_account_helper.{h,cpp}` (plan → group **write** path only)
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`
- `D:\Prop\src\Domain\Entities\Mt5Group.cs`
- SDK examples: `Examples\Server\APIExtension\PluginInstance.cpp`, `Examples\Report\Trades.Standard.Reports\Reports\DealsHistory.cpp`, `Examples\Web\PHP\mt5_api\mt5_group.php`, `Examples\Web\NET\...\MTGroupBase.cs`

**Architecture law (binding):** group discovery must enumerate **every client group the connected manager login is authorized to see**. It must **not** be limited to `MT5AccountHelper` / `.env` plan mappings.

---

## 1. Verdict (true measured state)

The **correct discovery primitive already exists** on the local Manager API wrapper and does **not** consult plan mappings:

`IMT5Client::GetAllGroups` / `GetGroupDetails` → `IMTManagerAPI::GroupTotal` + `GroupNext` over a reused `IMTConGroup*` from `GroupCreate()`.

That is the full manager-visible set. The server has already applied the manager’s allowed-group ACL. Application code must not intersect the result with `getMt5Group(planType, phase)` or the nine `MT5_GROUP_*` env strings.

Honest gaps (do not greenwash):

| Gap | Evidence |
|---|---|
| Default `MT5Manager::Connect` pump mask **omits** `PUMP_MODE_GROUPS` | `mt5_manager.cpp` default = `USERS\|ORDERS\|POSITIONS\|SYMBOLS` |
| `GroupRequest` / `GroupRequestArray` **unused** by the wrapper | Present only on `IMTManagerAPI` (not on `IMTAdminAPI`) |
| `MT5HttpClient::GetGroupDetails` always returns `false` | Comment: “requires direct SDK via MT5Manager” |
| `MT5Session` (pool) has `GetAllGroups` but **no** `GetGroupDetails` | `mt5_pool.h` group block |
| Probe uses `Connect(..., pumpMode=0)` then `GetAllGroups` | `mt5_group_probe.cpp` |
| `LogAvailableGroups` logs at most 50 names | diagnostic only; not a discovery API |
| HTTP `/mt5/groups/{name}/logins` does not URL-encode names | backslash / spaces will break |

`GetAllGroups` itself does **not** filter by plan. The risk is **callers** that treat the plan map as the universe of groups.

---

## 2. What “ALL groups for a manager login” means

There are **three** group lists. Mixing them is the usual bug.

| # | Object | Method | What it returns |
|---|---|---|---|
| A | `IMTManagerAPI` (connected as **that** manager) | `GroupTotal` / `GroupNext` / `GroupGet` / `GroupRequest` / `GroupRequestArray` | **Actual** `IMTConGroup` configs this login may see. **This is discovery.** |
| B | `IMTConManager` (manager **record**) | `GroupTotal` / `GroupNext` → `LPCWSTR` | **ACL masks**, not groups. Examples: `*`, `demo\*`, `real\*`, `demo\yo-2step`. Configured in Administrator → Managers. |
| C | `MT5AccountHelper::Mt5GroupConfig` | `getMt5Group(plan, phase)` | **Write-path** names for new / promoted accounts. A **subset** of (A). |

**ALL = (A).**  
The only legitimate filter is the server-side ACL (B), already applied before (A) is visible.  
(C) is how we *choose a group when creating a user*. It is not an enumerator.

Admin-unrestricted view of every group on the trade server is **not** required for this product. That would be `IMTAdminAPI` (`CreateAdmin`) or a manager whose ACL mask is `*`. This tree uses `CMTManagerAPIFactory::CreateManager` → `IMTManagerAPI` only.

---

## 3. SDK surface (Manager API)

Vendored header: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`  
Version pin: `MTManagerAPIVersion 5570` / `MTManagerAPIDate L"30 Jan 2026"`.

### 3.1 Connect as the manager

```cpp
virtual MTAPIRES Connect(LPCWSTR server, uint64_t login, LPCWSTR password,
                         LPCWSTR password_cert, uint64_t pump_mode,
                         uint32_t timeout=INFINITE)=0;
```

Pump flags that matter for groups (`IMTManagerAPI::EnPumpModes`):

| Flag | Value | Role |
|---|---|---|
| `PUMP_MODE_GROUPS` | `0x00000100` | Pump group **configurations** into the local cache that `GroupTotal` / `GroupNext` / `GroupGet` read |
| `PUMP_MODE_USERS` | `0x00000001` | Needed later for `UserTotal` / cache `UserGet`; not required to list group names |
| `PUMP_MODE_FULL` | `0xffffffff` | Everything, including groups |

`IMTAdminAPI::EnPumpModes` has **no** `PUMP_MODE_GROUPS` (admin pump is mail/news/full only). Stay on `IMTManagerAPI`.

### 3.2 Cache API vs request API (do not confuse Get / Request)

`IMTManagerAPI` “clients group configuration” block:

```199:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroup* GroupCreate(void)=0;
   virtual IMTConGroupSymbol* GroupSymbolCreate(void)=0;
   virtual IMTConCommission* GroupCommissionCreate(void)=0;
   virtual IMTConCommTier* GroupTierCreate(void)=0;
   virtual MTAPIRES  GroupSubscribe(IMTConGroupSink* sink)=0;
   virtual MTAPIRES  GroupUnsubscribe(IMTConGroupSink* sink)=0;
   virtual uint32_t  GroupTotal(void)=0;
   virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupGet(LPCWSTR name,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupRequest(LPCWSTR name,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupUpdate(IMTConGroup* group)=0;
   virtual MTAPIRES  GroupUpdateBatch(IMTConGroup** configs,const uint32_t config_total,MTAPIRES* results)=0;
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

| Call | Source | Pump required? | Use for discovery |
|---|---|---|---|
| `GroupCreate` | heap object | no | Always; caller `Release()`s |
| `GroupTotal` + `GroupNext` | **local cache** | Yes for a guaranteed-complete cache (`PUMP_MODE_GROUPS`) | Primary enumerator used by `GetAllGroups` |
| `GroupGet(name)` | **local cache** | same | Point lookup; used by `CreateUser` pre-check and `GetGroupSymbols` |
| `GroupRequest(name)` | **network**, one name | no | Refresh one group when cache misses |
| `GroupRequestArray(mask)` | **network**, wildcard | no | Complete / no-pump enumerator. Mask `*` = all groups this manager may see |
| `GroupSubscribe` + `IMTConGroupSink` | events | `PUMP_MODE_GROUPS` | Incremental add/update/delete after first snapshot |
| `GroupUpdate*` | write config | `RIGHT_CFG_GROUPS` | **Not** discovery |

`IMTAdminAPI` has `GroupTotal` / `GroupNext` / `GroupGet` / `GroupUpdate` / `GroupDelete` but **no** `GroupRequest` / `GroupRequestArray`. The request-API path is Manager-only.

### 3.3 `IMTConGroup` fields that discovery should copy

Name is a hierarchical path (`LPCWSTR Group()`), typically `demo\yo-2step`, `Flexy\yo-instant`. Other getters already mapped by `GetGroupDetails`:

| `IMTConGroup` | `GroupDetail` (`mt5_types.h`) |
|---|---|
| `Group()` | `name` |
| `Currency()` | `currency` |
| `CurrencyDigits()` | `currency_digits` |
| `Company()` | `company` |
| `MarginCall()` | `margin_call` |
| `MarginStopOut()` | `margin_stop_out` |
| `PermissionsFlags() & PERMISSION_ENABLE_CONNECTION` (`0x00000002`) | `connections_allowed` |

Not currently copied (available if a later upsert needs them): `Server()`, `AuthMode()`, `TradeFlags()`, `MarginMode()`, `DemoLeverage()`, `LimitOrders()`, `LimitPositions()`, `LimitSymbols()`, `SymbolTotal()` (per-group symbol overrides), `CommissionTotal()`.

### 3.4 Manager ACL (B) — inspect, do not invent a second filter

```216:222:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigManager.h
   //--- allowed groups list
   virtual MTAPIRES  GroupAdd(LPCWSTR path)=0;
   virtual MTAPIRES  GroupUpdate(const uint32_t pos,LPCWSTR path)=0;
   virtual MTAPIRES  GroupShift(const uint32_t pos,const int32_t shift)=0;
   virtual MTAPIRES  GroupDelete(const uint32_t pos)=0;
   virtual uint32_t  GroupTotal(void) const=0;
   virtual LPCWSTR   GroupNext(const uint32_t pos) const=0;
```

Read the connected manager’s masks with:

```cpp
IMTConManager* me = api->ManagerCreate();
api->ManagerCurrent(me);
for (uint32_t i = 0; i < me->GroupTotal(); ++i)
    log(me->GroupNext(i));   // "*", "demo\*", "real\*", ...
me->Release();
```

Relevant rights (`IMTConManager::EnManagerRights`): `RIGHT_CFG_GROUPS` (16) configure groups; `RIGHT_GRP_DETAILS_MARGIN` (45) / `RIGHT_GRP_DETAILS_COMMISSION` (51) edit subsets; `RIGHT_ACC_READ` (25) / `RIGHT_ACC_MANAGER` (27) accounts **inside** those groups. Missing `RIGHT_CFG_GROUPS` still allows **reading** group configs the ACL permits; it blocks `GroupUpdate`.

Do **not** re-implement ACL in application code. If (A) is smaller than expected, fix the manager record in Administrator, do not hard-code extra names.

### 3.5 Mask language (for `GroupRequestArray` and `UserLogins`)

`CMTStr::CheckGroupMask` (`MT5APIStr.h`): comma-separated templates, leading `!` = exclude, `*` wildcards via `CheckGroupTemplate`. Same language as Administrator “Groups” on the manager and as the `group` argument of `UserLogins` / `UserRequestArray`.

Examples that mean “all groups this manager may see”:

- `*`
- empty ACL on an unrestricted manager (server already expanded)

Do **not** pass plan names (`demo\yo-2step,Flexy\yo-2step,...`) as the discovery mask.

### 3.6 Retcodes that discovery must handle

From `MT5APIConstants.h`:

| Code | Value | Typical meaning here |
|---|---|---|
| `MT_RET_OK` | 0 | row filled |
| `MT_RET_OK_NONE` | 1 | no data (empty set is valid) |
| `MT_RET_ERR_PARAMS` | 3 | null `IMTConGroup*` / bad mask |
| `MT_RET_ERR_NOTFOUND` | 13 | `GroupGet` / `GroupRequest` unknown name, or `GroupNext` past end |
| `MT_RET_AUTH_MANAGER_NOCONFIG` | 1011 | login is not a manager |
| `MT_RET_AUTH_MANAGER_IPBLOCK` | 1012 | IP not on manager access list |
| `MT_RET_AUTH_GROUP_INVALID` | 1013 | group config not initialized (server restart) |
| `MT_RET_AUTH_MANAGER_TYPE` | 1024 | connection type not permitted |

`Get`/`Next` returning `13` for one index is skippable; `GroupCreate()` returning null is fatal (`false`).

---

## 4. How the wrappers already enumerate (do not re-limit them)

### 4.1 Contract — `IMT5Client`

```163:167:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // ===== Group Operations =====
    virtual uint32_t GroupTotal() = 0;
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
    virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

All four are **pure virtual**. Adjacent, not discovery: `GetUserLogins` (same SDK `UserLogins`), `GetGroupSymbols` (optional; `SymbolExist` against one group), `UpdateUser` / `UpdateUserGroup` (write).

`GroupDetail` (`mt5_types.h`):

```60:68:D:\Prop\mt5-sdk\src\core\mt5_types.h
struct GroupDetail {
    std::string name;              // group name (e.g. "real\challenge_phase1_10k")
    std::string currency;          // deposit currency (e.g. "USD")
    uint32_t    currency_digits = 2;
    std::string company;           // company label on this group
    double      margin_call    = 0; // margin call level (%)
    double      margin_stop_out= 0; // stop-out level (%)
    bool        connections_allowed = false; // PERMISSION_ENABLE_CONNECTION
};
```

Names on `GetAllGroups` / `GetGroupDetails` are **UTF-8 `std::string`**. Selectors on `GetGroupLogins` / `GetUserLogins` are **`std::wstring`**. Callers convert at the boundary (`StringUtils::toUtf8` / `toWide`).

### 4.2 Local — `MT5Manager` (canonical)

```962:1012:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint32_t total = m_manager->GroupTotal();
    groups.clear();
    groups.reserve(total);

    IMTConGroup* grp = m_manager->GroupCreate();
    if (!grp) return false;

    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
    grp->Release();
    ...
    return true;
}
```

`GetGroupDetails` is the same loop, filling `GroupDetail`. **No plan check. No name prefix. No env intersection.**

Same loop (names only) on `MT5Session::GetAllGroups` (`mt5_pool.cpp`). Pool sessions connect with `pump_mode = 0` (request-only). That is why a **`GroupRequestArray(L"*")` fallback** belongs in the architecture: a cold cache can make `GroupTotal() == 0` even when the manager owns many groups.

SDK examples use two equivalent walk styles; both visit **all cache rows**, not a product subset:

```cpp
// Style 1 — wrapper + DealsHistory.cpp
for (uint32_t pos = 0, total = api->GroupTotal(); pos < total; ++pos)
    if (api->GroupNext(pos, group) == MT_RET_OK) emit(group);

// Style 2 — Server APIExtension PluginInstance.cpp
for (uint32_t pos = 0; api->GroupNext(pos, group) == MT_RET_OK; ++pos)
    emit(group);
```

### 4.3 Remote — `MT5HttpClient`

| Method | HTTP | Completeness |
|---|---|---|
| `GroupTotal` | `GET /mt5/groups/count` → `count` | count only |
| `GetAllGroups` | `GET /mt5/groups` → `success` + `groups[]` | **must** be the same full (A) list the local wrapper would return |
| `GetGroupDetails` | none | **always `false`** |
| `GetGroupLogins` | `GET /mt5/groups/{utf8(name)}/logins` | one group; name not URL-encoded |

The remote microservice is a **transport**, not a second policy. Its `/mt5/groups` handler must call the same `GroupTotal`/`GroupNext` (or `GroupRequestArray("*")`) on the manager connection. Returning only plan-mapped names would violate this architecture.

`mt5_group_probe` correctly refuses `MT5_MODE=remote` (exit 3): remote `GetGroupDetails` is unimplemented and the probe is local-manager only.

### 4.4 Live probe

`D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`: load `.env`, `CreateManager`, `Connect(..., 0)`, `GetAllGroups`, sort+unique, print JSON `{ probe, connection, success, total, groups }`. Credentials are never echoed. This is the operator proof that (A) is non-empty for the configured login.

---

## 5. Binding algorithm (specify this; do not implement here)

Discovery is a **read** of the manager-visible config, then an upsert. Plan maps are not an input.

```
precondition: IMTManagerAPI connected as the target manager login
              (prefer pump_mode |= PUMP_MODE_GROUPS for the pump client)

1. Snapshot (required)
   a. IMTConGroup* g = GroupCreate(); fail if null
   b. n = GroupTotal()
   c. if n == 0:
        // cache empty (typical of pool / probe / no-pump)
        arr = GroupCreateArray()
        res = GroupRequestArray(L"*", arr)   // ALL, not a plan list
        if res != MT_RET_OK: fail (dependency_unavailable)
        for i in [0, arr->Total()): emit arr->Next(i)
        arr->Release()
      else:
        for i in [0, n):
            if GroupNext(i, g) == MT_RET_OK: emit g
   d. g->Release()

2. Optional ACL diagnostic (log only; never filter with it)
   ManagerCreate + ManagerCurrent + IMTConManager::GroupNext masks

3. Optional live follow (pump client only)
   GroupSubscribe(IMTConGroupSink) → OnGroupAdd/Update/Delete/Sync
   OnGroupSync ⇒ re-run step 1 (full refresh)

4. Persist
   upsert Domain Mt5Group by (BrokerId, Name) for EVERY emitted name
   set LastDiscoveredAt = now
   do not delete rows that vanished in one failed call
   do not skip names missing from MT5_GROUP_* env

5. Fail closed
   disconnected / GroupCreate null / GroupRequestArray != OK
     → false / dependency_unavailable
   empty-but-successful snapshot is valid (manager ACL may be empty)

6. Never
   for (auto& plan : knownPlans) groups.push_back(getMt5Group(plan, phase))
   if (!isPlanMapped(name)) skip
   hard-coded "demo\\yo-2step", "Flexy\\yo-1step", ...
```

Point lookup of one already-known name (create-user preflight) stays `GroupGet`; on cache miss, `GroupRequest`. That is **not** enumeration.

After discovery, login membership is a **second** pass, still not plan-limited:

```cpp
api->UserLogins(L"*", logins, total);          // all logins in all visible groups
// or per discovered name:
api->UserLogins(wide(name).c_str(), logins, total);
api->UserRequestArray(wide(name).c_str(), users);
```

`GetGroupLogins` today aliases `GetUserLogins` → `IMTManagerAPI::UserLogins`.

---

## 6. Plan mappings are a write-path concern only

`MT5AccountHelper` documents its job as **NEW account creation**:

```7:11:D:\Prop\mt5-sdk\src\services\mt5_account_helper.h
// Centralizes two cross-cutting concerns for NEW account creation:
//   1. Plan-type → MT5 group mapping
//   2. Sequential login number generation (starting at 301100)
```

`getMt5Group(planType, phase)` returns one of ~9 configured strings (`demo\yo-2step`, `Flexy\yo-instant`, …) or throws. Instant funding ignores phase. Unknown plan types do not silently fall back.

Correct split:

| Path | Source of group name | Must |
|---|---|---|
| **Discover** (this document) | `GetAllGroups` / `GetGroupDetails` / `GroupRequestArray("*")` | every manager-visible group |
| **Create / promote account** | `getMt5Group(plan, phase)` | then **validate** that name is in the last discovery set (`GroupGet` == `MT_RET_OK`) before `UserAdd` |
| **Copy / analyse / ledger** | discovered `Mt5Group` rows + `IsEnabledForAnalysis` | may disable a group; must still have discovered it |
| **HTTP `/mt5/groups`** | same as Discover | must not return only env-mapped names |

Domain already has a slot for the full snapshot:

```5:19:D:\Prop\src\Domain\Entities\Mt5Group.cs
public record Mt5Group(
    Guid Id,
    Guid BrokerId,
    string Name,
    string Currency,
    int CurrencyDigits,
    string Company,
    decimal MarginCall,
    decimal MarginStopOut,
    int ConnectionsAllowed,
    bool IsEnabledForAnalysis,
    DateTimeOffset? LastDiscoveredAt,
    DateTimeOffset? LastSyncedAt
);
```

`IsEnabledForAnalysis` is a **product** flag after discovery. It is not an input to the enumerator.

Why the split is load-bearing:

- Brokers add contest / manager / rebate / archive groups that no plan maps to. Those still hold users the manager can see.
- Env defaults (`demo\yo-2step`, `Flexy\yo-*`) can disagree with live names (`contest\yo-*` in architecture v2). Discovery tells the truth; the map can be wrong.
- A manager ACL of `demo\*` must still list every demo subtree group, not just `yo-2step` / `yo-1step`.

---

## 7. Recommended layering (callers)

```
                    ┌─────────────────────────────────────┐
                    │ Administrator → Managers            │
                    │   login L, Groups = ACL masks (B)   │
                    └─────────────────┬───────────────────┘
                                      │ Connect(L)
                                      v
 IMTManagerAPI ── GroupTotal/Next/Get ── cache (PUMP_MODE_GROUPS)
               └─ GroupRequestArray("*") ─ network fallback
                                      │
                                      v
              IMT5Client::GetAllGroups / GetGroupDetails
              (MT5Manager | MT5Session | HTTP /mt5/groups)
                                      │
                    ┌─────────────────┴──────────────────┐
                    v                                    v
         upsert Mt5Group (ALL names)          getMt5Group(plan,phase)
         LastDiscoveredAt                     create / promote only
         never filtered by env                validate ∈ discovered set
```

Pump client (watchdog `MT5Manager`): include `PUMP_MODE_GROUPS`, optionally `GroupSubscribe` for deltas.  
Pool client (`MT5Session`, `pump_mode=0`): prefer `GroupRequestArray("*")` when `GroupTotal()==0`.  
Remote client: `GET /mt5/groups` must implement the same snapshot; `GetGroupDetails` is currently unavailable.

---

## 8. What not to do

1. **Do not** build the group list from `Mt5GroupConfig` / `MT5_GROUP_*`.
2. **Do not** skip names that fail a `yo-` / `challenge` / `funded` regex.
3. **Do not** treat `GroupTotal()==0` on a no-pump session as “broker has no groups” without `GroupRequestArray`.
4. **Do not** confuse `IMTConManager::GroupNext` (masks) with `IMTManagerAPI::GroupNext` (configs).
5. **Do not** confuse `SymbolGroupTotal` / `SymbolGroupNext` (symbol **folders**) with client groups.
6. **Do not** use `IMTAdminAPI` for this product’s manager login.
7. **Do not** URL-build `/mt5/groups/` + raw name without encoding.
8. **Do not** hand-write `.mq5` or mutate product source for this report (not requested).

---

## 9. Implementation gaps (for a later coder; not done here)

These are observations, not edits:

1. Add `PUMP_MODE_GROUPS` to the pump `Connect` default (or OR it in explicitly).
2. If `GroupTotal()==0` and connected, fall back to `GroupRequestArray(L"*")` in `GetAllGroups` / `GetGroupDetails`.
3. Implement `GetGroupDetails` on the HTTP microservice (and stop returning `false` once it exists).
4. Add `GetGroupDetails` on `MT5Session` if pool-backed discovery is required.
5. Return `false` (or a partial-failure flag) if `groups.size() != total` after the `GroupNext` loop.
6. Probe: optionally emit `GetGroupDetails` + `ManagerCurrent` ACL masks for operator diagnosis.

---

## 10. One-page recipe

To dynamically enumerate **ALL** groups for manager login `L`:

1. `CreateManager(MTManagerAPIVersion)` and `Connect(server, L, password, …)` as **that** login (`IMTManagerAPI`, not Admin).
2. Call `IMT5Client::GetAllGroups` / `GetGroupDetails` (already a full `GroupTotal`+`GroupNext` walk).
3. If the cache is empty, `GroupRequestArray(L"*")` — mask `*`, **not** a plan list.
4. Persist every name. Optionally `GroupSubscribe` for later diffs.
5. Use `getMt5Group` **only** when creating or promoting an account, and only after the mapped name appears in the discovered set.

That is the architecture. Plan mappings are not a discovery filter.
