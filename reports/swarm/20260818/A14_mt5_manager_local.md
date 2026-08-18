# A14 — MT5Manager local transport

**Agent:** A14  
**Date:** 2026-08-18  
**Scope (read-only):** `D:\Prop\mt5-sdk\src\core\mt5_manager.h`, `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp`  
**Supporting evidence (not modified):** `imt5_client.h`, `mt5_types.h`, `CMakeLists.txt`, `README.md`, `config/app_config.h`, `vendor/MetaTrader5SDK/Include/MT5APIManager.h`, `mt5_http_client.cpp`, `mt5_pool.cpp`, `tests/mt5_group_probe.cpp`  
**Product source:** not modified.

This note describes how the local `IMT5Client` implementation talks to a live MT5 server: native Windows Manager API, in-process group walk, and the (non-paged) deal-history path.

---

## 1. What “local transport” is

`MT5Manager` is the **local-SDK** `IMT5Client`. It is not HTTP. It loads MetaQuotes’ Manager API DLL, creates an `IMTManagerAPI*`, and issues Manager-API calls over the broker’s manager channel (`server:port`, 30 s timeout).

| Piece | Fact |
|---|---|
| Interface | `class MT5Manager : public IMT5Client, IMTManagerSink, IMTPositionSink, IMTDealSink, IMTOrderSink, IMTUserSink` (`mt5_manager.h:19–24`) |
| Pair | `MT5HttpClient` is the other `IMT5Client` (remote microservice). Consumer code binds the interface; `AppConfig::mt5_mode` (`"local"` default) picks the impl. |
| Selection | `README.md`: **local** = native Manager API, lowest latency, Windows-only, consumes a manager connection slot. **remote** = HTTP, cross-platform, shared pool. |
| Config | `app_config.h:13–25` — `mt5_mode="local"` drives `MT5Manager`/`MT5Pool`; needs `mt5_server`, `mt5_port` (443), `mt5_login`, `mt5_password`. Optional SOCKS5/SOCKS4/HTTP proxy. |
| Lifetime not on the interface | `Initialize` / `Connect` / `Disconnect` / `SetProxy` are `MT5Manager`-only (`mt5_manager.h:29–35`). `IMT5Client` only exposes `IsConnected` / `GetLastError`. |

Call path after `Initialize` + `Connect`:

```
caller
  → MT5Manager (m_mutex around almost every SDK call)
    → IMTManagerAPI* m_manager
      → MT5APIManager64*.dll (LoadLibrary)
        → TCP to <server>:<port>  (optional ProxySet first)
```

There is no named-pipe / localhost sidecar. “Local” means **in-process native DLL + direct manager TCP**, not “loopback HTTP”.

### 1.1 Factory + connect

`Initialize(dllPath)` (`mt5_manager.cpp:16–31`):

1. `m_factory.Initialize(dllPath.c_str())` — `CMTManagerAPIFactory` (`MT5APIManager.h:1719–1744`) `FindLibrary` then `LoadLibraryW`.
2. `m_factory.CreateManager(MTManagerAPIVersion, &m_manager)`.
3. Fail-closed on either `MT_RET_OK` miss.

`FindLibrary` (`MT5APIManager.h:1769–1831`) prefers AVX2 / AVX / ARM64 variants, then `MT5APIManager64.dll`, then `.\libs\`, then `PATH`. CMake copies three runtime DLLs beside the exe (`CMakeLists.txt:114–118`):

- `MT5APIManager64.dll`
- `MetaQuotes.MT5ManagerAPI64.dll`
- `MetaQuotes.MT5CommonAPI64.dll`

`Connect(server, login, password, pumpMode)` (`mt5_manager.cpp:71–149`):

1. Under `m_mutex`. Apply stored `ProxySet` once if configured (`IP:port` + optional `login:password`).
2. `Subscribe(this)` as `IMTManagerSink`.
3. Default pump (when `pumpMode == 0`): `PUMP_MODE_USERS | ORDERS | POSITIONS | SYMBOLS`. **Not** `PUMP_MODE_GROUPS`. **Not** news. There is **no** `PUMP_MODE_DEALS` in the SDK enum (`MT5APIManager.h:127–143`).
4. `m_manager->Connect(server, login, password, L"" /* password_cert */, mode, 30000)`.
5. On failure: retry `Connect(..., mode=0, 30000)` — **request-only** fallback. `GetDeals` / `DealRequest` still work; sinks are not subscribed; `m_pumpMode = false`.
6. On pump success: `PositionSubscribe` / `DealSubscribe` / `OrderSubscribe` / `UserSubscribe`. `m_pumpMode = true`.

Disconnect unsubscribes the four data sinks + manager sink, then `Disconnect()`, then `m_eventQueue.stop()`.

Error mapping (`mt5_manager.cpp:61–68`): 7 network timeout, 1012 manager IP block, 5 no connect, 3 bad credentials. Anything else is a generic code string.

### 1.2 How the local channel is used after connect

Almost every public method takes `m_mutex`, then requires `m_manager && m_connected`.

Two request styles:

| Style | Examples | Meaning |
|---|---|---|
| Cache-first, then network | `GetAccount` (`UserAccountGet` → `UserAccountRequest`), `GetPositions` (`PositionGet` → `PositionRequest`), `GetOrders` (`OrderGetOpen` → `OrderRequestOpen` only on **cache error**, not on empty) | Pump cache is sub-ms; network is a blocking RTT under `m_mutex`. |
| Network-only | `GetDeals` (`DealRequest` only), `GetUser` (`UserRequest`), `DealerBalance`, `DealerSend`, `ChartRequest` | Header comment: SDK has **no** local deal cache / **no** `DealGet`. |
| Config walk | `GroupTotal` / `GroupNext` / `GroupGet`, `SymbolTotal` / `SymbolNext` | In-process manager config objects. |

Pump sinks (`OnPosition*`, `OnDeal*`, `OnOrder*`, `OnUser*`) push `MT5Event` onto `m_eventQueue` for a worker. They do **not** take `m_mutex`. `OnDealAdd` / `OnDealUpdate` also write the recent-deals ring under `m_recentDealsMutex` only.

`SendTrade` is a full local dealer path (`TA_DEALER_*` + `DealerSend(request, nullptr, dealerQueueId)`). `DealerSend` with `sink==nullptr` is treated as synchronous. After success it **unlocks `m_mutex`** before a second `DealRequest(ticket)` used only to map deal → `PositionID`. Transport latency is recorded as `mt5_transport_latency_ms`.

`GetRecentDeals` / `CacheExecutedDeal` are **not** the broker history API. They exist because `DealRequest`’s server-side index lags (comment: >40 s on demo) and because `OnDealAdd` is expected to stay silent (no `PUMP_MODE_DEALS`). The live ring population path is `CacheExecutedDeal` after a successful `SendTrade`.

---

## 2. Windows-only Manager API

This is a hard platform cut, not a `#ifdef` inside the `.cpp`.

| Layer | Evidence |
|---|---|
| Header | `mt5_manager.h:3` `#include <Windows.h>` then `MT5APIManager.h`. Will not compile on non-Windows. |
| CMake | `CMakeLists.txt:49–57` — `mt5_manager.cpp`, `mt5_pool.cpp`, `mt5_watchdog.cpp` are appended **only** `if(WIN32)`. Comment: Manager API “ships as Windows DLLs only. On other platforms the HTTP client remains available and the local-mode transport is simply absent.” |
| Runtime copy | `mt5sdk_copy_runtime_dlls` is a no-op unless `WIN32` (`CMakeLists.txt:120–123`). |
| Factory | `LoadLibraryW` / `GetProcAddress` / `FreeLibrary` (`MT5APIManager.h:1726–1755`). |
| Toolchain | `README.md:48` — MSVC 2022; Manager API is **Windows x64 only**. |
| Probes | `mt5_group_probe` / `mt5_news_calendar_probe` built only `if(MT5SDK_BUILD_PROBES AND WIN32)`. |
| Types used | `wcsncpy_s`, `MTAPISTR`, `LPCWSTR` passwords, `::time` fallback in `GetServerTime`. |

Implications:

- Linux/macOS consumers cannot link `MT5Manager`. They must use `MT5HttpClient`.
- Local mode consumes a **manager license / IP-whitelist slot** on the broker (`MT_RET_AUTH_MANAGER_IPBLOCK` = 1012).
- Group-detail and live tick subscribe (`SubscribeTicks` → `TickSubscribe`) are local-SDK features; the HTTP client stubs several of these.

---

## 3. Group enumeration

All group walks take `m_mutex` and require a connected manager. They use the **config cache** APIs (`GroupTotal` / `GroupNext` / `GroupGet`), not `GroupRequest` / `GroupRequestArray`.

Native surface (`MT5APIManager.h:199–212`):

```
GroupCreate / GroupSubscribe / GroupUnsubscribe
GroupTotal / GroupNext(pos, group) / GroupGet(name) / GroupRequest(name)
GroupUpdate / GroupUpdateBatch
GroupCreateArray / GroupRequestArray(mask)
```

`MT5Manager` uses only `GroupCreate`, `GroupTotal`, `GroupNext`, `GroupGet`. It never calls `GroupSubscribe`, `GroupRequest`, or `GroupRequestArray`. Default connect pump does **not** include `PUMP_MODE_GROUPS`. Live group-config change notifications are therefore not wired.

### 3.1 `GroupTotal`

```956:960:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
uint32_t MT5Manager::GroupTotal() {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return 0;
    return m_manager->GroupTotal();
}
```

Disconnected → `0` (not distinguishable from “server has zero groups”).

### 3.2 `GetAllGroups` — name walk

```962:982:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
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

Facts:

- One reused `IMTConGroup` object; `GroupNext(i)` overwrites it each step.
- `GroupNext` failure is **skipped**, not fatal. Method still returns `true`.
- Names are UTF-8 via `StringUtils::toUtf8`. No sort/dedup here (the group probe sorts + unique after the call).
- Empty `total == 0` still returns `true` with an empty vector (unlike `UserLogins`).

### 3.3 `GetGroupDetails` — same walk, richer DTO

Same `GroupTotal` + `GroupNext` loop (`mt5_manager.cpp:984–1013`). Each hit fills `GroupDetail` (`mt5_types.h:60–68`):

| Field | Source |
|---|---|
| `name` | `grp->Group()` |
| `currency` | `grp->Currency()` |
| `currency_digits` | `grp->CurrencyDigits()` |
| `company` | `grp->Company()` |
| `margin_call` | `grp->MarginCall()` |
| `margin_stop_out` | `grp->MarginStopOut()` |
| `connections_allowed` | `(PermissionsFlags() & 0x00000002) != 0` (`PERMISSION_ENABLE_CONNECTION`) |

Same skip-on-`GroupNext` fail / always-`true` contract. HTTP client **does not** implement this (`mt5_http_client.cpp:666–669` returns `false` — “require direct SDK access”).

### 3.4 Logins in a group

`GetGroupLogins(group)` is an alias of `GetUserLogins` (`mt5_manager.cpp:1015–1017`).

```315:328:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    ...
    uint64_t* raw_logins = nullptr;
    uint32_t total = 0;
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
    m_manager->Free(raw_logins);
    return true;
}
```

This is a **server allocation**, not a `GroupNext` walk. Empty group with `raw_logins == nullptr` returns **false** (stricter than `GetAllGroups`). Caller must not leak — `Free` is on the success path only.

### 3.5 Other group touch-points

| Call site | Use |
|---|---|
| `CreateUser` (`mt5_manager.cpp:218–225`) | `GroupGet(wideName)` as a pre-check; result is only logged (`0=OK`). `UserAdd` still runs even if `GroupGet` fails. |
| `GetGroupSymbols` (`755–782`) | `GroupGet` then `SymbolTotal`/`SymbolNext`; keep symbol iff `SymbolExist(sym, group) == MT_RET_OK`. |
| `UpdateUser` / `UpdateUserGroup` | Write `user->Group(...)` then `UserUpdate`. No existence check. |
| `LogAvailableGroups` (`1089–1106`) | Diagnostic: logs first **50** `GroupNext` names, then `"... and N more"`. |

Operator probe `tests/mt5_group_probe.cpp` is **local-only**: `MT5_MODE=remote` exits 3 (“group enumeration requires local manager mode”). Connects with `pumpMode=0` (request-only), then `GetAllGroups`.

---

## 4. Deal history paging

### 4.1 Interface contract vs local implementation

`IMT5Client::GetDeals` (`imt5_client.h:61–65`):

> Complete-history contract: implementations must follow every provider page/cursor for `[from,to]`, or return false. Callers treat false as `dependency_unavailable` and must not make a pass/fail decision.

**Local `MT5Manager::GetDeals` does not page.** One `DealRequest` for the whole `[from,to]` window.

```485:510:D:\Prop\mt5-sdk\src\core\mt5_manager.cpp
bool MT5Manager::GetDeals(uint64_t login, int64_t from, int64_t to, std::vector<DealData>& out) {
    std::lock_guard<std::mutex> lock(m_mutex);
    if (!m_manager || !m_connected) return false;

    IMTDealArray* deals = m_manager->DealCreateArray();
    if (!deals) return false;

    // DealRequest sends a network request to the MT5 server
    MTAPIRES res = m_manager->DealRequest(login, from, to, deals);

    // MT_RET_OK_NONE (1) and MT_RET_ERR_NOTFOUND (13) mean no deals — not an error
    if (res != MT_RET_OK && res != MT_RET_OK_NONE && res != MT_RET_ERR_NOTFOUND) {
        spdlog::warn("MT5 DealRequest failed for login {}: res={}", login, res);
        deals->Release();
        return false;
    }

    out.clear();
    for (uint32_t i = 0; i < deals->Total(); i++) {
        const IMTDeal* deal = deals->Next(i);
        if (deal) out.push_back(extractDeal(deal));
    }
    deals->Release();
    return true;
}
```

What this is:

- Native overload: `DealRequest(login, from, to, IMTDealArray*)` (`MT5APIManager.h:270`). Time range is **inclusive on the wrapper’s later ring filter**; the SDK call is passed through as-is.
- Empty history is success (`MT_RET_OK_NONE` / `MT_RET_ERR_NOTFOUND`).
- Array walk is `deals->Total()` + `deals->Next(i)` — same pattern as positions/orders. This is **in-array iteration**, not provider paging.
- Held under `m_mutex` for the whole network RTT + copy. Concurrent `GetDeals` serialize with every other manager call.
- Works in **no-pump** fallback (`Connect` comment at `mt5_manager.cpp:119–120`).

What this is **not**:

- No `offset` / `limit` / `cursor`.
- No retry loop.
- No call to `DealRequestPage`.
- No `DealRequestByGroup` / `ByLogins` / `ByTickets`.
- No local deal cache read (`DealGet` does not exist — header `mt5_manager.h:194–195`).

### 4.2 Native page API exists and is unused

`IMTManagerAPI` exposes (`MT5APIManager.h:526`, repeated at `1263`):

```
DealRequestPage(login, from, to, offset, total, IMTDealArray* deals)
```

`rg` over `mt5-sdk/src` finds **zero** call sites. `MT5Session::GetDeals` in `mt5_pool.cpp:557–576` is the same single `DealRequest(login, from, to)` (pool session, not the pump `MT5Manager`, same paging gap).

If a broker/server truncates a one-shot `DealRequest` (large login, wide window), **local transport still returns `true`** with a partial `out`. That violates the complete-history contract if the native call is silently capped. This file does not claim a measured cap; it records that the wrapper **does not** follow `DealRequestPage` and therefore **cannot** prove completeness beyond “one `DealRequest` succeeded”.

### 4.3 Contrast: remote HTTP *does* page

`MT5HttpClient::GetDeals` (`mt5_http_client.cpp:505–548`):

- `GET /mt5/accounts/{login}/deals?from=&to=`
- Follows `next_cursor`, else `page` + `limit=1000`, else `has_more` / `total_pages`
- Hard cap 10 000 HTTP requests
- Duplicate cursor → `false`
- `has_more` without a continuation → `false`
- Loop exhaustion → `false` (“never accept partial history”)

Local and remote therefore do **not** share a paging implementation. Only remote implements the interface’s “follow every page/cursor” sentence literally.

### 4.4 Other deal reads (not history paging)

| Path | API | Role |
|---|---|---|
| `GetDeals` | `DealRequest(login, from, to, array)` | Full-window history. One shot. |
| `SendTrade` post-fill | `DealRequest(ticket, IMTDeal*)` | Map deal → `PositionID`. Unlocked. |
| `GetRecentDeals` | in-process `deque` cap **4096** | Filter `login` + `time ∈ [from,to]`. Always `true`. Shared lock. O(N) over the ring. |
| `cacheRecentDeal` | same deque | Dedup last 32 tickets, push_back, pop_front past cap. Unique lock. Never `m_mutex`. |
| `CacheExecutedDeal` | synthesizes ticket `(1<<63) \| position\|login` if ticket==0 | Live ring fill when pump deals never fire. |
| `OnDealAdd` / `OnDealUpdate` | extract + `cacheRecentDeal` + event queue | Expected silent (no `PUMP_MODE_DEALS`). Debug log is the probe. |

`extractDeal` (`mt5_manager.cpp:1508–1525`) copies: ticket, login, order, position, symbol, action, entry, volume, price, profit, commission, storage, time, comment. No paging metadata.

### 4.5 Time window

`GetDeals` does not clamp `from`/`to`. (`GetChart` does: `to<=0` → `TimeServer()`, `from<0` → 0, `from>to` → false.) A caller that passes an inverted or huge window gets whatever the server returns in **one** array.

---

## 5. Honest gaps / contract mismatches

1. **`GetDeals` is not paged.** Native `DealRequestPage` is unused. Completeness is assumed, not walked. HTTP transport is stricter.
2. **No `PUMP_MODE_DEALS`.** Recent-deals ring is not a substitute for history; it is a 4096-event, arrival-order cache plus synthetic `CacheExecutedDeal` rows.
3. **Default pump omits `PUMP_MODE_GROUPS`.** Group lists come from `GroupNext` on the config cache; there is no `GroupSubscribe` and no `GroupRequest` refresh.
4. **`GetAllGroups` / `GetGroupDetails` return `true` after partial `GroupNext` failures.** Count can be `< GroupTotal()`.
5. **`GetUserLogins` fails closed on a null pointer**, so an empty group may look like an API failure.
6. **`GroupTotal()==0` when disconnected** looks like “no groups”.
7. **Windows-only.** Non-Windows builds have no `MT5Manager` object file.
8. **`CreateUser`’s `GroupGet` is informational** — a missing group does not abort `UserAdd`.

---

## 6. File map

| Path | Role |
|---|---|
| `D:\Prop\mt5-sdk\src\core\mt5_manager.h` | Local client surface, sinks, ring (`kRecentDealCap=4096`), `m_mutex` vs `m_recentDealsMutex` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Init/connect/proxy, group walk, one-shot `DealRequest`, dealer send, sinks |
| `D:\Prop\mt5-sdk\src\core\imt5_client.h` | Transport-agnostic contract, including complete-history `GetDeals` |
| `D:\Prop\mt5-sdk\CMakeLists.txt` | `if(WIN32)` compile + DLL copy |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Factory (`LoadLibraryW`), `DealRequest` / **`DealRequestPage`**, `GroupTotal`/`GroupNext`/`GroupGet` |
| `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp` | Remote paging contrast; `GetGroupDetails` unsupported |
| `D:\Prop\mt5-sdk\tests\mt5_group_probe.cpp` | Live local group enumeration probe |

---

## 7. Verdict (measured from source, not from a live broker)

Local transport is the in-process MetaQuotes **Windows x64 Manager API**: `LoadLibrary` → `IMTManagerAPI::Connect` → mutex-serialized native calls. Group enumeration is a `GroupTotal`/`GroupNext` cache walk (plus `UserLogins` for members). Deal history is a **single** `DealRequest(login, from, to)` with an in-array `Next` walk — **not** `DealRequestPage` and **not** the HTTP cursor loop.

That is the complete A14 reading. No product source was changed.
