# A84 — Confirm `IMT5Client` `GroupTotal` / `GetAllGroups` / `GetGroupDetails` and manager implementation

**Agent:** A84  
**Date:** 2026-08-18  
**Product source modified:** none (read-only)  
**Output (this file only):** `D:\Prop\reports\swarm\20260818\A84_group_total_impl.md`

**Sources (quoted, not modified):**

- `D:\Prop\mt5-sdk\src\core\imt5_client.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.h`
- `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`
- `D:\Prop\mt5-sdk\src\core\mt5_types.h` (`GroupDetail`)
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.h` / `mt5_http_client.cpp` (second `IMT5Client` impl)
- `D:\Prop\mt5-sdk\src\core\mt5_pool.h` / `mt5_pool.cpp` (pool session; **not** `IMT5Client`)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`IMTManagerAPI` cache APIs)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h` (`IMTConGroup` getters)
- `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp`

C# under `D:\Prop\src` has **no** `GroupTotal` / `GetAllGroups` / `GetGroupDetails` symbols. These three live only on the C++ `IMT5Client` surface.

---

## 1. Verdict (true measured state)

| Question | Answer |
|---|---|
| Do `GroupTotal`, `GetAllGroups`, `GetGroupDetails` exist on `IMT5Client`? | **Yes.** All three are **pure virtual** (`= 0`) in the `// ===== Group Operations =====` block. Every `IMT5Client` must implement them. |
| Does `MT5Manager` implement them? | **Yes.** `MT5Manager : public IMT5Client` overrides all three and walks the **local Manager-API group cache**: `IMTManagerAPI::GroupTotal` + `GroupCreate` + `GroupNext` on a reused `IMTConGroup*`. |
| Does the walk consult plan / `.env` group maps? | **No.** Names and details come only from `IMTConGroup` fields. |
| Does the walk use `GroupRequest` / `GroupRequestArray`? | **No.** Those exist on `IMTManagerAPI` and are unused by these three methods. |
| Is the other `IMT5Client` (`MT5HttpClient`) a full peer? | **Partial.** `GroupTotal` and `GetAllGroups` are REST proxies. `GetGroupDetails` is a stub that **always returns `false`**. |
| Does `MT5Session` (pool) implement the same trio? | **No.** It has `GroupTotal` + `GetAllGroups` only. It is **not** an `IMT5Client` and has **no** `GetGroupDetails`. |

---

## 2. They exist on `IMT5Client`

File: `D:\Prop\mt5-sdk\src\core\imt5_client.h`

The interface comment names both concrete clients:

```12:17:D:\Prop\mt5-sdk\src\core\imt5_client.h
// Abstract interface for MT5 operations.
// Implemented by MT5Manager (local SDK) and MT5HttpClient (remote microservice).
// All services and controllers depend on this interface, not the concrete implementation.
class IMT5Client {
public:
    virtual ~IMT5Client() = default;
```

The group block (pure virtual — no default bodies):

```163:167:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // ===== Group Operations =====
    virtual uint32_t GroupTotal() = 0;
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
    virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

Signatures:

```cpp
virtual uint32_t GroupTotal() = 0;
virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
```

| Method | Kind | In | Out | Meaning |
|---|---|---|---|---|
| `GroupTotal` | pure virtual | — | `uint32_t` | Count of groups in the connected manager’s **local config cache**. |
| `GetAllGroups` | pure virtual | `groups` (out) | `bool` | Name-only list (`std::string`, UTF-8). |
| `GetGroupDetails` | pure virtual | `groups` (out) | `bool` | Structured `GroupDetail` records. |

`GetGroupLogins` is adjacent on the same block (also pure virtual). It is **not** one of the three methods this note confirms; `MT5Manager` implements it as `return GetUserLogins(group, logins);`.

Encoding: names on `GetAllGroups` / `GetGroupDetails` are `std::string`. The group **selector** on `GetGroupLogins` / `GetUserLogins` is `std::wstring`.

`GroupDetail` is defined in `mt5_types.h` (included by `imt5_client.h`):

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

---

## 3. `MT5Manager` declares the overrides

`MT5Manager` is the local-SDK `IMT5Client`:

```18:24:D:\Prop\mt5-sdk\src\core\mt5_manager.h
// MT5 Manager API Wrapper - implements IMT5Client interface
class MT5Manager : public IMT5Client,
                   public IMTManagerSink,
                   public IMTPositionSink,
                   public IMTDealSink,
                   public IMTOrderSink,
                   public IMTUserSink {
```

Group overrides + diagnostic (not on the interface):

```126:138:D:\Prop\mt5-sdk\src\core\mt5_manager.h
    // ===== Group Operations =====
    uint32_t GroupTotal() override;
    bool GetAllGroups(std::vector<std::string>& groups) override;
    bool GetGroupDetails(std::vector<GroupDetail>& groups) override;
    bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) override;

    // ===== User Modification =====
    bool UpdateUserLeverage(uint64_t login, uint32_t leverage) override;
    bool UpdateUserGroup(uint64_t login, const std::string& group) override;
    bool UpdateUserRights(uint64_t login, uint64_t rights) override;

    // ===== Diagnostics =====
    void LogAvailableGroups();
```

The native handle these methods call is `IMTManagerAPI* m_manager` (`mt5_manager.h:183`), created in `Initialize` via `CMTManagerAPIFactory::CreateManager`. Almost every public method (including the three below) takes `m_mutex` then requires `m_manager && m_connected`.

---

## 4. How `MT5Manager` implements them

All three sit under `// ==================== Group Operations ====================` in `mt5_manager.cpp`. They are **cache walks**, not network request APIs.

### 4.1 SDK primitives they call

Vendored `IMTManagerAPI` “clients group configuration” block:

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

Used by the three methods: **`GroupTotal`**, **`GroupCreate`**, **`GroupNext`**, plus `IMTConGroup::Release()`.

Not used by the three methods: `GroupGet`, `GroupRequest`, `GroupRequestArray`, `GroupSubscribe`, `GroupUpdate*`.

(`GroupGet` **is** used elsewhere on `MT5Manager`: `CreateUser` pre-check, `GetGroupSymbols`.)

`GroupTotal` / `GroupNext` / `GroupGet` read the **local pumped config cache**. Completeness of that cache depends on `PUMP_MODE_GROUPS` (see §5).

### 4.2 `GroupTotal`

```956:960:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
uint32_t MT5Manager::GroupTotal() {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return 0;
    return m_manager->GroupTotal();
}
```

Behavior:

1. Take `m_mutex`.
2. If the SDK pointer is null or `m_connected` is false → **`0`** (not an error code; same as an empty cache).
3. Else forward to `IMTManagerAPI::GroupTotal()`.

No logging. No `GroupCreate`. This is a count of whatever is already in the manager’s local group-config cache.

### 4.3 `GetAllGroups`

```962:982:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
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

    spdlog::info("MT5 GetAllGroups: returned {} groups", groups.size());
    return true;
}
```

Behavior:

1. Take `m_mutex`. Fail closed (`false`) if not connected.
2. Snapshot `total = m_manager->GroupTotal()`.
3. `groups.clear()` then `reserve(total)`.
4. Allocate one reusable `IMTConGroup*` via `GroupCreate()`. If that returns null → `false` (vector may already have been cleared).
5. For each index `[0, total)` call `GroupNext(i, grp)`. On `MT_RET_OK` only, push `StringUtils::toUtf8(grp->Group())`. Non-OK rows are **skipped**, not fatal.
6. `grp->Release()`.
7. Log the **pushed** count (may be `< total` if some `GroupNext` failed).
8. Return **`true`** even if the vector is empty or some rows were skipped.

Name source (`IMTConGroup`):

```637:638:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   virtual LPCWSTR   Group(void) const=0;
   virtual MTAPIRES  Group(LPCWSTR group)=0;
```

### 4.4 `GetGroupDetails`

```984:1013:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetGroupDetails(std::vector<GroupDetail>& groups) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    uint32_t total = m_manager->GroupTotal();
    groups.clear();
    groups.reserve(total);

    IMTConGroup* grp = m_manager->GroupCreate();
    if (!grp) return false;

    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            GroupDetail d;
            d.name               = StringUtils::toUtf8(grp->Group());
            d.currency           = StringUtils::toUtf8(grp->Currency());
            d.currency_digits    = grp->CurrencyDigits();
            d.company            = StringUtils::toUtf8(grp->Company());
            d.margin_call        = grp->MarginCall();
            d.margin_stop_out    = grp->MarginStopOut();
            // PERMISSION_ENABLE_CONNECTION = 0x00000002
            d.connections_allowed = (grp->PermissionsFlags() & 0x00000002) != 0;
            groups.push_back(std::move(d));
        }
    }
    grp->Release();

    spdlog::info("MT5 GetGroupDetails: returned {} groups", groups.size());
    return true;
}
```

Same control flow as `GetAllGroups` (mutex, connected check, `GroupTotal`, `GroupCreate`, `GroupNext` loop, skip non-OK, `Release`, log, return `true`). Extra work is field copy from `IMTConGroup` into `GroupDetail`.

Field map:

| `GroupDetail` | `IMTConGroup` getter | Notes |
|---|---|---|
| `name` | `Group()` | `LPCWSTR` → UTF-8 |
| `currency` | `Currency()` | `LPCWSTR` → UTF-8 |
| `currency_digits` | `CurrencyDigits()` | `uint32_t` |
| `company` | `Company()` | `LPCWSTR` → UTF-8 |
| `margin_call` | `MarginCall()` | `double` (%) |
| `margin_stop_out` | `MarginStopOut()` | `double` (%) |
| `connections_allowed` | `PermissionsFlags() & 0x00000002` | hardcoded mask, not the enum name |

SDK getters:

```642:644:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   //--- EnPermissionsFlags
   virtual uint64_t    PermissionsFlags(void) const=0;
   virtual MTAPIRES  PermissionsFlags(const uint64_t flags)=0;
```

```651:653:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   //--- company name
   virtual LPCWSTR   Company(void) const=0;
   virtual MTAPIRES  Company(LPCWSTR company)=0;
```

```669:672:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   //--- deposit currency
   virtual LPCWSTR   Currency(void) const=0;
   virtual MTAPIRES  Currency(LPCWSTR currency)=0;
   virtual uint32_t  CurrencyDigits(void) const=0;
```

```719:724:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   //--- Margin Call level value
   virtual double    MarginCall(void) const=0;
   virtual MTAPIRES  MarginCall(const double level)=0;
   //--- Sto-Out level value
   virtual double    MarginStopOut(void) const=0;
   virtual MTAPIRES  MarginStopOut(const double level)=0;
```

Permission bit (comment in `GetGroupDetails` matches the enum):

```459:463:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Config\MT5APIConfigGroup.h
   enum EnPermissionsFlags
     {
      PERMISSION_NONE              =0x00000000,  // default
      PERMISSION_CERT_CONFIRM      =0x00000001,  // certificate confirmation neccessary
      PERMISSION_ENABLE_CONNECTION =0x00000002,  // clients connections allowed
```

Not copied (available on `IMTConGroup` if a later upsert needs them): `Server()`, `AuthMode()`, `TradeFlags()`, `DemoLeverage()`, `LimitOrders()`, `LimitPositions()`, `LimitSymbols()`, per-group `SymbolTotal()`, `CommissionTotal()`.

### 4.5 Shared implementation facts

- **One heap object per call.** Both enumerators reuse a single `GroupCreate()` result and overwrite it with each `GroupNext`. Caller always `Release()`s it on the success path after create.
- **No plan filter.** There is no call into `MT5AccountHelper`, `AppConfig`, or `MT5_GROUP_*` env vars.
- **No network refresh.** `GroupRequest` / `GroupRequestArray` are not called, so a cold / incomplete cache is what you get.
- **Fail vs skip.**
  - `!m_manager || !m_connected` → `GroupTotal` returns `0`; the two getters return `false`.
  - `GroupCreate()` null → `false` (after `clear()`).
  - `GroupNext != MT_RET_OK` → skip that index; method still returns `true`.
  - Empty cache (`total == 0`) → `true` and an empty vector.
- **Thread safety.** The entire walk is under `m_mutex`. Concurrent pump callbacks that do not take `m_mutex` are not blocked by these methods’ lock from the other direction — but these methods **do** block other `m_mutex` users for the full `GroupTotal` walk.
- **UTF-8.** Wide SDK strings are converted with `StringUtils::toUtf8`.

### 4.6 Diagnostic twin (not on `IMT5Client`)

`LogAvailableGroups` is the same walk, capped at **50** names, void return, no out-vector:

```1089:1106:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
void MT5Manager::LogAvailableGroups() {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return;

    uint32_t total = m_manager->GroupTotal();
    spdlog::info("MT5 available groups ({} total):", total);

    IMTConGroup* grp = m_manager->GroupCreate();
    if (!grp) return;

    for (uint32_t i = 0; i < total && i < 50; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            spdlog::info("  [{}] {}", i, StringUtils::toUtf8(grp->Group()));
        }
    }
    if (total > 50) spdlog::info("  ... and {} more", total - 50);
    grp->Release();
}
```

This is diagnostics only. Discovery callers must use `GetAllGroups` / `GetGroupDetails`, which have **no** 50-cap.

---

## 5. Cache completeness depends on pump mode

`GroupTotal` / `GroupNext` read the **local** group-config cache. The SDK flag that fills that cache is:

```133:134:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
      PUMP_MODE_SYMBOLS       =0x00000200,   // pump symbol configurations
```

`MT5Manager::Connect` default when the caller passes `pumpMode == 0`:

```101:108:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
    // Connect with pump mode for real-time events
    uint64_t mode = pumpMode;
    if (mode == 0) {
        mode = IMTManagerAPI::PUMP_MODE_USERS |
               IMTManagerAPI::PUMP_MODE_ORDERS |
               IMTManagerAPI::PUMP_MODE_POSITIONS |
               IMTManagerAPI::PUMP_MODE_SYMBOLS;
    }
```

Default mask = `USERS | ORDERS | POSITIONS | SYMBOLS`. **`PUMP_MODE_GROUPS` is omitted.** On pump-connect failure the method retries `Connect(..., mode=0)` (request-only).

The group probe also connects with explicit `pumpMode=0` then calls `GetAllGroups`:

```113:114:D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp
        // No pump mode required for group enumeration — keep traffic minimal.
        const bool connected = manager.Connect(server, config.mt5_login, password, 0);
```

```129:130:D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp
        std::vector<std::string> groups;
        if (!manager.GetAllGroups(groups)) {
```

Measured implication (do not greenwash): these three methods **exist and are implemented**, but they only report what is already in the local cache. They do **not** force a `GroupRequestArray(L"*")` snapshot. A complete manager-visible set is guaranteed only if:

- connect included `PUMP_MODE_GROUPS` or `PUMP_MODE_FULL`, **or**
- the server still populated the cache without that flag (observed on some deployments; not proven here), **or**
- a later change adds `GroupRequest` / `GroupRequestArray` (not present today).

---

## 6. Other implementors (so the interface is fully accounted for)

`IMT5Client` has two concrete classes. `MT5Session` is a third walker that is **not** `IMT5Client`.

### 6.1 `MT5HttpClient` — required overrides, details stubbed

```70:73:D:\Prop\mt5-sdk\src\core\mt5_http_client.h
    uint32_t GroupTotal() override;
    bool GetAllGroups(std::vector<std::string>& groups) override;
    bool GetGroupDetails(std::vector<GroupDetail>& groups) override;
    bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) override;
```

```654:670:D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp
uint32_t MT5HttpClient::GroupTotal() {
    auto resp = httpGet("/mt5/groups/count");
    return resp.value("count", (uint32_t)0);
}

bool MT5HttpClient::GetAllGroups(std::vector<std::string>& groups) {
    auto resp = httpGet("/mt5/groups");
    if (!resp.value("success", false)) return false;
    groups = resp["groups"].get<std::vector<std::string>>();
    return true;
}

bool MT5HttpClient::GetGroupDetails(std::vector<GroupDetail>& groups) {
    // Not implemented for HTTP client proxy mode — always returns false.
    // Group detail queries require direct SDK access via MT5Manager.
    return false;
}
```

Remote `GetGroupDetails` never fills `groups` and never hits the SDK. Callers that need `GroupDetail` must be on `MT5Manager` (local).

### 6.2 `MT5Session` — same cache walk, no details, not `IMT5Client`

```79:82:D:\Prop\mt5-sdk\src\core\mt5_pool.h
    // Group Operations
    uint32_t GroupTotal();
    bool GetAllGroups(std::vector<std::string>& groups);
    bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins);
```

```724:749:D:\Prop\mt5-sdk\src\core\mt5_pool.cpp
uint32_t MT5Session::GroupTotal() {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return 0;
    return m_manager->GroupTotal();
}


bool MT5Session::GetAllGroups(std::vector<std::string>& groups) {
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
    return true;
}
```

Same algorithm as `MT5Manager::GetAllGroups` (no log line). **No `GetGroupDetails` on the pool session.**

### 6.3 Test stub

`D:\Prop\mt5-sdk\tests\mt5_time_window_test.cpp` overrides the trio as `return 0` / `return false` so a fake `IMT5Client` can compile. That is not a manager implementation.

---

## 7. Call-graph (local manager path)

```
caller (IMT5Client&)
  → MT5Manager::GroupTotal / GetAllGroups / GetGroupDetails
       [std::lock_guard m_mutex]
       → require m_manager && m_connected
       → IMTManagerAPI::GroupTotal()            // cache count
       → IMTManagerAPI::GroupCreate()           // heap IMTConGroup*
       → IMTManagerAPI::GroupNext(i, grp)       // cache row, i in [0, total)
            → grp->Group() / Currency() / …     // details only
            → StringUtils::toUtf8
       → IMTConGroup::Release()
```

No `GroupRequest`. No `GroupRequestArray`. No plan-map intersection.

---

## 8. What this note does **not** claim

- That a default `Connect(pumpMode=0)` always yields a complete group list. The methods exist; cache fill is a separate pump-flag fact (§5).
- That remote mode can return `GroupDetail`. It cannot (`GetGroupDetails` → `false`).
- That C# `IBrokerConnector` / workers already call these. No matches under `D:\Prop\src`.
- That `GroupTotal()` on a disconnected manager is distinguishable from “zero groups” — both are `0`.
