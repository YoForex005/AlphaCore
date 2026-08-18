# A12 — `IMT5Client` interface map

**Source (read-only, not modified):** `D:\Prop\mt5-sdk\src\core\imt5_client.h`  
**Lines:** 1–177 (file read in full)  
**Date:** 2026-08-18  
**Agent:** A12  
**Scope:** group discovery, deals, orders, positions, ticks, events  
**Product source:** not modified

---

## 1. Role of the interface

`IMT5Client` is the abstract, transport-agnostic contract for MT5 operations.

```15:17:D:\Prop\mt5-sdk\src\core\imt5_client.h
class IMT5Client {
public:
    virtual ~IMT5Client() = default;
```

Comment on the class (lines 12–14):

> Abstract interface for MT5 operations.  
> Implemented by `MT5Manager` (local SDK) and `MT5HttpClient` (remote microservice).  
> All services and controllers depend on this interface, not the concrete implementation.

Includes: `"mt5_types.h"`, `<string>`, `<vector>`, `<cstdint>`.

Forward declaration only (no SDK headers on this surface):

```10:10:D:\Prop\mt5-sdk\src\core\imt5_client.h
class IMTTickSink;
```

Rationale (lines 7–9): keep the interface transport-agnostic; only the local-SDK implementation (`MT5Manager`) needs the full `IMTTickSink` definition.

Pure-virtual methods are **required** on every implementation. Methods with bodies are **optional** (default = unsupported / no-op / fail-closed). That distinction is load-bearing for remote vs pump clients.

---

## 2. Highlighted methods (explicit ask)

| Method | Kind | Default | Contract in one line |
|---|---|---|---|
| `GetAllGroups` | **pure virtual** | none | Fill `vector<string>` of group names. |
| `GetGroupDetails` | **pure virtual** | none | Fill `vector<GroupDetail>` of group records. |
| `GetDeals` | **pure virtual** | none | Complete-history `[from,to]` or return `false`. |
| `SubscribeTicks` | virtual + default | `false` | Register `IMTTickSink*`; fail-closed if no live stream. |
| `GetEventQueue` | **pure virtual** | none | Return the pump/SSE event queue by reference. |

Quoted signatures:

```29:29:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual MT5EventQueue& GetEventQueue() = 0;
```

```65:65:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) = 0;
```

```127:127:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool SubscribeTicks(IMTTickSink* sink) { (void)sink; return false; }
```

```165:166:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
```

---

## 3. Event queue

Section header in source: `// ===== Event Queue =====` (line 26).

```27:29:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // Returns a reference to the event queue for pump event consumption.
    // MT5Manager populates this from SDK callbacks; MT5HttpClient populates it from SSE.
    virtual MT5EventQueue& GetEventQueue() = 0;
```

**Signature**

```cpp
virtual MT5EventQueue& GetEventQueue() = 0;
```

**Notes**

- Pure virtual: every client **must** expose a queue.
- Return is a **non-const reference** — callers consume/pump events from it.
- Dual population path:
  - `MT5Manager` — SDK callbacks.
  - `MT5HttpClient` — SSE.
- Type `MT5EventQueue` comes from `mt5_types.h` (not defined in this header).
- This is the only dedicated event-queue accessor. Tick *events* are a separate sink (`SubscribeTicks`), not this queue.

Related lifecycle (not the queue itself, but used by event consumers):

```20:24:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool IsConnected() const = 0;

    // Returns a human-readable reason for the last connection failure.
    // Empty string when connected or no attempt made yet.
    virtual std::string GetLastError() const { return ""; }
```

```cpp
virtual bool IsConnected() const = 0;
virtual std::string GetLastError() const;   // default: return ""
```

`GetLastError` is optional (default empty). `IsConnected` is required.

---

## 4. Group discovery

Section header in source: `// ===== Group Operations =====` (line 163).

```164:167:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual uint32_t GroupTotal() = 0;
    virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
    virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
    virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

**Signatures**

```cpp
virtual uint32_t GroupTotal() = 0;
virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;
virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;
virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

All four are **pure virtual**. Group discovery is a required capability on every `IMT5Client`.

| Method | In | Out | Purpose |
|---|---|---|---|
| `GroupTotal` | — | `uint32_t` | Count of groups known to the manager. |
| `GetAllGroups` | `groups` (out) | `bool` | Name-only enumeration (`std::string`, **not** `wstring`). |
| `GetGroupDetails` | `groups` (out) | `bool` | Structured records (`GroupDetail` from `mt5_types.h`). |
| `GetGroupLogins` | `group` (`wstring`) | `logins` + `bool` | Logins that belong to one group. |

**Encoding inconsistency (load-bearing):**

- Group **names** on `GetAllGroups` / `GetGroupDetails` / `UpdateUser` / `UpdateUserGroup` / `GetGroupSymbols` use `std::string`.
- Group **selector** on `GetGroupLogins` and `GetUserLogins` uses `std::wstring`.

Callers must not assume one string type for “group” across the interface.

**Adjacent group-aware APIs (not in the Group Operations block):**

```38:38:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
```

```34:34:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool UpdateUser(uint64_t login, const std::string& group) = 0;
```

```116:116:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual std::vector<std::string> GetGroupSymbols(const std::string& groupName) { (void)groupName; return {}; }
```

```171:171:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool UpdateUserGroup(uint64_t login, const std::string& group) = 0;
```

```cpp
virtual bool GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;
virtual bool UpdateUser(uint64_t login, const std::string& group) = 0;
virtual std::vector<std::string> GetGroupSymbols(const std::string& groupName);  // default: {}
virtual bool UpdateUserGroup(uint64_t login, const std::string& group) = 0;
```

`GetUserLogins` vs `GetGroupLogins`: both take `const std::wstring& group` and fill `vector<uint64_t>`. Both are pure virtual. This header does not distinguish their semantics; treat them as potentially overlapping until implementations are checked.

`GetGroupSymbols` is optional (default empty vector). It is a **symbol** helper keyed by group name, not a group-discovery method.

---

## 5. Deals

Section header in source: `// ===== Deal Operations =====` (line 61).

```65:88:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) = 0;

    // Return locally event-cached deals for `login` whose deal.time is in [from,to].
    // Populated SYNCHRONOUSLY from OnDealAdd/OnDealUpdate on the pump MT5Manager so a
    // just-closed deal is queryable instantly — long before the broker's network
    // DealRequest history index reflects it (which empirically lags >40s on demo).
    // Default: unsupported -> empty, false. Only the pump MT5Manager overrides it;
    // pool/remote clients keep returning false and lose nothing (they never had a
    // local deal-event stream in the first place).
    virtual bool GetRecentDeals(uint64_t login, int64_t from, int64_t to,
                                std::vector<DealData>& out) {
        (void)login; (void)from; (void)to; (void)out; return false;
    }

    // Insert one already-executed deal into the synchronous recent-deals ring so a
    // just-opened/just-closed trade is queryable by getTradeHistory INSTANTLY —
    // without waiting on the broker's network DealRequest history index (which
    // empirically lags >40s on demo) and without depending on the deals pump (there
    // is NO PUMP_MODE_DEALS in the MT5 SDK, so OnDealAdd/OnDealUpdate likely never
    // fire at runtime). Callers synthesize this from a successful SendTrade result
    // (deal/position/login/symbol/time) right after execution. Default: no-op, so
    // pool/remote clients that have no local ring lose nothing. ONLY the pump
    // MT5Manager overrides it, and it MUST be the SAME instance GetRecentDeals reads.
    virtual void CacheExecutedDeal(const DealData& deal) { (void)deal; }
```

**Signatures**

```cpp
virtual bool GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) = 0;

virtual bool GetRecentDeals(uint64_t login, int64_t from, int64_t to,
                            std::vector<DealData>& out);
    // default: (void)…; return false;

virtual void CacheExecutedDeal(const DealData& deal);
    // default: (void)deal;  // no-op
```

### 5.1 `GetDeals` — complete-history contract (required)

Comment (lines 62–64):

> Complete-history contract: implementations must follow every provider page/cursor for `[from,to]`, or return false. Callers treat false as `dependency_unavailable` and must not make a pass/fail decision.

| Param | Type | Role |
|---|---|---|
| `login` | `uint64_t` | Account |
| `from` | `int64_t` | Inclusive start (same epoch convention as other time fields; not restated here) |
| `to` | `int64_t` | Inclusive end |
| `out` | `vector<DealData>&` | Filled history |

**Fail-closed rule:** `false` ≠ “no deals”. `false` = provider did not complete the range. Callers must **not** treat an empty `out` after `false` as a pass.

Paging/cursor following is mandatory when the provider paginates. Partial fills that drop pages are a contract violation.

### 5.2 `GetRecentDeals` — local event cache (optional)

- Window: `deal.time` in `[from,to]` for `login`.
- Population: **synchronously** from `OnDealAdd` / `OnDealUpdate` on the **pump** `MT5Manager`.
- Purpose: a just-closed deal is queryable **instantly**, before the broker `DealRequest` history index (comment: empirical lag **>40s on demo**).
- Default: unsupported → empty + `false`.
- Who overrides: **only** the pump `MT5Manager`.
- Pool/remote clients keep the default; they never had a local deal-event stream.

### 5.3 `CacheExecutedDeal` — synthetic ring insert (optional)

- Inserts one already-executed `DealData` into the same recent-deals ring `GetRecentDeals` reads.
- Why it exists:
  1. Broker history index lags (>40s demo).
  2. Comment states there is **NO `PUMP_MODE_DEALS`** in the MT5 SDK, so `OnDealAdd` / `OnDealUpdate` **likely never fire at runtime**.
- Callers synthesize from a successful `SendTrade` result (`deal` / `position` / `login` / `symbol` / `time`) **right after execution**.
- Default: no-op.
- **Instance identity:** only the pump `MT5Manager` overrides it, and it **MUST** be the **same instance** `GetRecentDeals` reads.

### 5.4 Deal-adjacent (not in Deal Operations)

Balance methods emit `dealId` but are not history APIs:

```44:47:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool DealerBalance(uint64_t login, double amount, const std::wstring& comment,
                               uint32_t type, uint64_t& dealId) = 0;
    virtual bool Deposit(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
    virtual bool Withdraw(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
```

```cpp
virtual bool DealerBalance(uint64_t login, double amount, const std::wstring& comment,
                           uint32_t type, uint64_t& dealId) = 0;
virtual bool Deposit(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
virtual bool Withdraw(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;
```

All three are pure virtual. They **create** balance deals; they do not enumerate them.

---

## 6. Orders

Section header in source: `// ===== Order Operations =====` (line 52).

```53:59:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // Enumerate a login's OPEN (pending) orders. Used to verify that an order
    // ticket targeted by modify/cancel actually belongs to the owned login.
    // Default returns false (unsupported) so callers fail closed when the active
    // client cannot enumerate orders (e.g. the remote HTTP client).
    virtual bool GetOrders(uint64_t login, std::vector<OrderData>& out) {
        (void)login; (void)out; return false;
    }
```

**Signature**

```cpp
virtual bool GetOrders(uint64_t login, std::vector<OrderData>& out);
    // default: return false;
```

**Notes**

- **Open / pending only** — not full order history.
- Use: ownership check before modify/cancel (ticket must belong to the owned `login`).
- Optional; default `false` (fail-closed). Comment names the remote HTTP client as a typical non-enumerator.
- `false` = unsupported / cannot enumerate — **not** “this login has no open orders”.

### 6.1 Order placement / mutate (Dealer + SendTrade)

```145:146:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool DealerSendOrder(uint64_t login, const std::wstring& symbol, uint32_t action,
                                 uint64_t volume, double price, uint64_t& orderId) = 0;
```

```cpp
virtual bool DealerSendOrder(uint64_t login, const std::wstring& symbol, uint32_t action,
                             uint64_t volume, double price, uint64_t& orderId) = 0;
```

Pure virtual. Out-param `orderId`. This is dealer send, not enumeration.

```154:161:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual MT5TradeResult SendTrade(const MT5TradeRequest& req) {
        (void)req;
        MT5TradeResult r;
        r.supported = false;
        r.ok = false;
        r.message = "Trade operation not supported by this MT5 client";
        return r;
    }
```

```cpp
virtual MT5TradeResult SendTrade(const MT5TradeRequest& req);
    // default: supported=false, ok=false, message="Trade operation not supported by this MT5 client"
```

Comment (lines 148–153): single entry point for **market/pending place**, **SL/TP modify**, and **pending-order cancel**. Default is UNSUPPORTED so clients that cannot perform the op (comment: remote HTTP client for modify/cancel) fail closed and the service returns **HTTP 501**.

`SendTrade` is the mutate path; `GetOrders` is the enumerate/verify path.

---

## 7. Positions

Section header in source: `// ===== Position Operations =====` (line 49).

```50:50:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetPositions(uint64_t login, std::vector<PositionData>& out) = 0;
```

**Signature**

```cpp
virtual bool GetPositions(uint64_t login, std::vector<PositionData>& out) = 0;
```

**Notes**

- Pure virtual — every client must implement position enumeration.
- Snapshot of current positions for one `login`.
- No position-history API, no single-ticket getter, no close-position method on this interface.
- Position close/modify goes through `SendTrade` (see §6.1).
- Type `PositionData` is from `mt5_types.h`.

Contrast with orders: positions are **required**; open-order enumeration is **optional**.

---

## 8. Ticks

Two surfaces: **poll** (Symbol Operations) and **subscribe** (event-driven tick feed).

### 8.1 Poll / last-tick

Section header: `// ===== Symbol Operations =====` (line 109).

```113:114:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual bool GetTickLast(const std::wstring& symbol, TickData& out) = 0;
    virtual bool GetAllTicksLast(std::vector<TickData>& out) { (void)out; return false; }
```

**Signatures**

```cpp
virtual bool GetTickLast(const std::wstring& symbol, TickData& out) = 0;
virtual bool GetAllTicksLast(std::vector<TickData>& out);  // default: return false
```

| Method | Kind | Meaning |
|---|---|---|
| `GetTickLast` | required | Last tick for one symbol (`wstring` name). |
| `GetAllTicksLast` | optional | Last tick for all subscribed/known symbols; default unsupported. |

Supporting symbol enumeration (not ticks, but needed to poll):

```110:116:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual uint32_t SymbolTotal() = 0;
    virtual bool GetSymbol(uint32_t pos, SymbolData& out) = 0;
    virtual bool GetSymbolByName(const std::wstring& name, SymbolData& out) = 0;
    virtual bool GetTickLast(const std::wstring& symbol, TickData& out) = 0;
    virtual bool GetAllTicksLast(std::vector<TickData>& out) { (void)out; return false; }
    virtual std::vector<std::string> GetManagerSymbols() { return {}; }
    virtual std::vector<std::string> GetGroupSymbols(const std::string& groupName) { (void)groupName; return {}; }
```

```cpp
virtual uint32_t SymbolTotal() = 0;
virtual bool GetSymbol(uint32_t pos, SymbolData& out) = 0;
virtual bool GetSymbolByName(const std::wstring& name, SymbolData& out) = 0;
virtual std::vector<std::string> GetManagerSymbols();                     // default: {}
virtual std::vector<std::string> GetGroupSymbols(const std::string& groupName);  // default: {}
```

Symbol **name** on get/tick APIs is `wstring`. Manager/group symbol **lists** are `vector<string>`. Same encoding split as groups.

### 8.2 Event-driven subscribe — `SubscribeTicks`

Section header: `// ===== Event-driven Tick Feed (transport-agnostic) =====` (line 118).

```119:128:D:\Prop\mt5-sdk\src\core\imt5_client.h
    // Register/unregister an MT5 SDK tick sink so the QuoteHub can ingest ticks
    // event-driven (IMTTickSink::OnTick) instead of polling GetTickLast.
    // The default returns false (unsupported) so a client that cannot deliver a
    // live tick stream (e.g. the remote HTTP client) makes the caller fall back
    // to polling rather than silently believing it is subscribed.
    // CONTRACT: OnTick fires on the SDK pump thread — the sink MUST only enqueue
    // and return immediately; it must not block, send on a socket, hit the DB,
    // or re-enter this client under its manager mutex.
    virtual bool SubscribeTicks(IMTTickSink* sink) { (void)sink; return false; }
    virtual bool UnsubscribeTicks(IMTTickSink* sink) { (void)sink; return false; }
```

**Signatures**

```cpp
virtual bool SubscribeTicks(IMTTickSink* sink);     // default: return false
virtual bool UnsubscribeTicks(IMTTickSink* sink);   // default: return false
```

**Contract (from comments, binding):**

1. Consumer: QuoteHub, via `IMTTickSink::OnTick`, instead of polling `GetTickLast`.
2. Default `false` = unsupported. Caller **must fall back to polling**. Never treat a non-overridden client as subscribed.
3. Typical non-subscriber: remote HTTP client.
4. **Thread contract:** `OnTick` runs on the **SDK pump thread**. The sink MUST:
   - only enqueue
   - return immediately
   - **not** block
   - **not** send on a socket
   - **not** hit the DB
   - **not** re-enter this client under its manager mutex
5. Pair: `UnsubscribeTicks` uses the same sink pointer. Default also `false`.
6. Raw `IMTTickSink*` — no ownership stated in this header. Lifetime is the caller’s problem.

`GetEventQueue` is **not** the tick subscription path. Ticks go through `IMTTickSink`; pump/SSE domain events go through `MT5EventQueue`.

---

## 9. Other event-like surfaces (calendar / news)

Not tick or pump events, but named “events” on the interface:

```91:107:D:\Prop\mt5-sdk\src\core\imt5_client.h
    virtual MT5NewsCalendarResult GetNewsCalendarItems(const MT5CalendarQuery& query) {
        (void)query;
        MT5NewsCalendarResult result;
        result.success = false;
        result.metadata.supported = false;
        result.metadata.status = "unsupported";
        result.metadata.source = "none";
        result.metadata.max_items = mt5_news_calendar::normalizedMaxItems(query.max_items);
        result.metadata.include_body = query.include_body;
        result.metadata.reason = "MT5 news/calendar provider is not implemented for this client.";
        result.error_message = result.metadata.reason;
        return result;
    }

    virtual MT5NewsCalendarResult GetCalendarEvents(const MT5CalendarQuery& query) {
        return GetNewsCalendarItems(query);
    }
```

**Signatures**

```cpp
virtual MT5NewsCalendarResult GetNewsCalendarItems(const MT5CalendarQuery& query);
    // default: success=false, supported=false, status="unsupported", source="none"

virtual MT5NewsCalendarResult GetCalendarEvents(const MT5CalendarQuery& query);
    // default: return GetNewsCalendarItems(query);
```

`GetCalendarEvents` is an alias of `GetNewsCalendarItems`. Both default to unsupported. Not related to `GetEventQueue` or `SubscribeTicks`.

---

## 10. Required vs optional (full interface)

### Pure virtual (every implementation must provide)

| Area | Signature |
|---|---|
| Lifecycle | `virtual bool IsConnected() const = 0;` |
| Events | `virtual MT5EventQueue& GetEventQueue() = 0;` |
| Users | `virtual bool CreateUser(const UserParams& params, uint64_t& outLogin) = 0;` |
| Users | `virtual bool GetUser(uint64_t login, UserData& out) = 0;` |
| Users | `virtual bool UpdateUser(uint64_t login, const std::string& group) = 0;` |
| Users | `virtual bool DeleteUser(uint64_t login) = 0;` |
| Users | `virtual bool ChangePassword(uint64_t login, const std::wstring& password, uint32_t type = 0) = 0;` |
| Users | `virtual bool CheckPassword(uint64_t login, const std::wstring& password, uint32_t type = 0) = 0;` |
| Users | `virtual bool GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;` |
| Account | `virtual bool GetAccount(uint64_t login, AccountData& out) = 0;` |
| Balance | `virtual bool DealerBalance(uint64_t login, double amount, const std::wstring& comment, uint32_t type, uint64_t& dealId) = 0;` |
| Balance | `virtual bool Deposit(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;` |
| Balance | `virtual bool Withdraw(uint64_t login, double amount, const std::wstring& comment, uint64_t& dealId) = 0;` |
| Positions | `virtual bool GetPositions(uint64_t login, std::vector<PositionData>& out) = 0;` |
| Deals | `virtual bool GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) = 0;` |
| Symbols | `virtual uint32_t SymbolTotal() = 0;` |
| Symbols | `virtual bool GetSymbol(uint32_t pos, SymbolData& out) = 0;` |
| Symbols | `virtual bool GetSymbolByName(const std::wstring& name, SymbolData& out) = 0;` |
| Ticks (poll) | `virtual bool GetTickLast(const std::wstring& symbol, TickData& out) = 0;` |
| Dealer | `virtual bool DealerSendOrder(uint64_t login, const std::wstring& symbol, uint32_t action, uint64_t volume, double price, uint64_t& orderId) = 0;` |
| Groups | `virtual uint32_t GroupTotal() = 0;` |
| Groups | `virtual bool GetAllGroups(std::vector<std::string>& groups) = 0;` |
| Groups | `virtual bool GetGroupDetails(std::vector<GroupDetail>& groups) = 0;` |
| Groups | `virtual bool GetGroupLogins(const std::wstring& group, std::vector<uint64_t>& logins) = 0;` |
| Users | `virtual bool UpdateUserLeverage(uint64_t login, uint32_t leverage) = 0;` |
| Users | `virtual bool UpdateUserGroup(uint64_t login, const std::string& group) = 0;` |
| Users | `virtual bool UpdateUserRights(uint64_t login, uint64_t rights) = 0;` |
| Time | `virtual int64_t GetServerTime() = 0;` |

### Defaulted (optional / fail-closed)

| Area | Signature | Default behavior |
|---|---|---|
| Lifecycle | `virtual std::string GetLastError() const` | `return ""` |
| Orders | `virtual bool GetOrders(uint64_t login, std::vector<OrderData>& out)` | `return false` |
| Deals | `virtual bool GetRecentDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out)` | `return false` |
| Deals | `virtual void CacheExecutedDeal(const DealData& deal)` | no-op |
| News | `virtual MT5NewsCalendarResult GetNewsCalendarItems(const MT5CalendarQuery& query)` | unsupported result |
| News | `virtual MT5NewsCalendarResult GetCalendarEvents(const MT5CalendarQuery& query)` | delegates to news |
| Ticks (poll) | `virtual bool GetAllTicksLast(std::vector<TickData>& out)` | `return false` |
| Symbols | `virtual std::vector<std::string> GetManagerSymbols()` | `return {}` |
| Symbols | `virtual std::vector<std::string> GetGroupSymbols(const std::string& groupName)` | `return {}` |
| Ticks (push) | `virtual bool SubscribeTicks(IMTTickSink* sink)` | `return false` |
| Ticks (push) | `virtual bool UnsubscribeTicks(IMTTickSink* sink)` | `return false` |
| Charts | `virtual bool GetChart(const std::wstring& symbol, const std::string& timeframe, int64_t from, int64_t to, uint32_t limit, std::vector<ChartBarData>& out)` | `return false` |
| Trade | `virtual MT5TradeResult SendTrade(const MT5TradeRequest& req)` | `supported=false`, `ok=false`, HTTP-501 path |

---

## 11. Boolean semantics (do not collapse)

On this interface, `false` is **not** uniformly “empty result”:

| Method | `false` means |
|---|---|
| `GetDeals` | History incomplete / dependency unavailable — **do not** decide pass/fail |
| `GetRecentDeals` | Cache unsupported (or no local stream) |
| `GetOrders` | Enumeration unsupported (e.g. HTTP client) |
| `GetAllGroups` / `GetGroupDetails` / `GetGroupLogins` | Implementation-defined failure (no default; required) |
| `GetPositions` | Implementation-defined failure (required) |
| `GetTickLast` | Implementation-defined failure (required) |
| `SubscribeTicks` / `UnsubscribeTicks` | No live stream — **fall back to poll** |
| `GetAllTicksLast` / `GetChart` | Unsupported |

Treat required-method `false` as hard failure unless the concrete implementation documents empty-as-success (this header does not).

---

## 12. Data types referenced (defined in `mt5_types.h`, not here)

| Type | Used by |
|---|---|
| `MT5EventQueue` | `GetEventQueue` |
| `UserParams`, `UserData` | user ops |
| `AccountData` | `GetAccount` |
| `PositionData` | `GetPositions` |
| `OrderData` | `GetOrders` |
| `DealData` | `GetDeals`, `GetRecentDeals`, `CacheExecutedDeal` |
| `SymbolData` | `GetSymbol`, `GetSymbolByName` |
| `TickData` | `GetTickLast`, `GetAllTicksLast` |
| `ChartBarData` | `GetChart` |
| `GroupDetail` | `GetGroupDetails` |
| `MT5TradeRequest`, `MT5TradeResult` | `SendTrade` |
| `MT5CalendarQuery`, `MT5NewsCalendarResult` | news/calendar |
| `IMTTickSink` | `SubscribeTicks` / `UnsubscribeTicks` (forward-declared only) |

This map does not invent fields for those types.

---

## 13. Section index in the header

| Lines | Banner |
|---|---|
| 19–24 | Lifecycle (`IsConnected`, `GetLastError`) |
| 26–29 | **Event Queue** (`GetEventQueue`) |
| 31–38 | User Operations |
| 40–41 | Account Operations |
| 43–47 | Balance Operations (`dealId` out) |
| 49–50 | **Position Operations** (`GetPositions`) |
| 52–59 | **Order Operations** (`GetOrders`) |
| 61–88 | **Deal Operations** (`GetDeals`, `GetRecentDeals`, `CacheExecutedDeal`) |
| 90–107 | News/Calendar (`GetCalendarEvents`) |
| 109–116 | Symbol Operations (`GetTickLast`, `GetAllTicksLast`) |
| 118–128 | **Event-driven Tick Feed** (`SubscribeTicks`, `UnsubscribeTicks`) |
| 130–142 | Historical Chart Data |
| 144–146 | Dealer Operations (`DealerSendOrder`) |
| 148–161 | Guarded Trade Execution (`SendTrade`) |
| 163–167 | **Group Operations** (`GroupTotal`, `GetAllGroups`, `GetGroupDetails`, `GetGroupLogins`) |
| 169–172 | User Modification |
| 174–175 | Server Time |

---

## 14. Implementation hints stated in this header (not verified here)

| Client | What this header claims |
|---|---|
| `MT5Manager` (local SDK / pump) | Implements the interface; fills `GetEventQueue` from SDK callbacks; **only** this instance should override `GetRecentDeals` + `CacheExecutedDeal`; owns the live `IMTTickSink` path. |
| `MT5HttpClient` (remote) | Implements the interface; fills `GetEventQueue` from SSE; typically cannot enumerate orders; typically cannot `SubscribeTicks`; `SendTrade` modify/cancel may stay unsupported (HTTP 501). |
| Pool / remote | Keep deal-cache defaults (`false` / no-op); they have no local deal-event stream. |

Concrete `.cpp` bodies were **not** read. Claims above are comments on this interface only.

---

## 15. Extracted facts for downstream agents

1. **`GetAllGroups` / `GetGroupDetails`** are required group-discovery APIs; names are `std::string`; details use `GroupDetail`.
2. **`GetDeals`** is required complete-history; `false` = `dependency_unavailable`, never a pass.
3. **`GetRecentDeals` + `CacheExecutedDeal`** exist because history lags (>40s) and there is no `PUMP_MODE_DEALS`; they must share one pump `MT5Manager` instance.
4. **`GetOrders`** is optional, open/pending only, fail-closed — ownership gate for modify/cancel.
5. **`GetPositions`** is required; no other position method exists.
6. **`SubscribeTicks`** is optional, fail-closed to poll; `OnTick` is pump-thread, enqueue-only.
7. **`GetEventQueue`** is required; SDK callbacks vs SSE; distinct from the tick sink.

---

*End of A12 map. Product source untouched.*
