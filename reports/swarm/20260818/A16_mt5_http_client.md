# A16 — `MT5HttpClient` REST / SSE / timeout inventory

**Agent:** A16 (senior engineer, read-only of product source)  
**Date:** 2026-08-18  
**Sources (quoted, not modified):**

- `D:\Prop\mt5-sdk\src\core\mt5_http_client.h`
- `D:\Prop\mt5-sdk\src\core\mt5_http_client.cpp`
- Supporting read-only context: `imt5_client.h`, `mt5_types.h`, `config/app_config.h`, `mt5-sdk/README.md`, `apps/mt5-worker/`

**Product source was not modified.**

---

## 1. Role

`MT5HttpClient` is the **remote** implementation of `IMT5Client`. It does not load the MetaQuotes Manager API. Every supported operation is delegated to a remote MT5 microservice over REST (libcurl + nlohmann JSON). Pump events arrive on a dedicated SSE thread and are pushed into `MT5EventQueue`.

Header contract (defaults):

```cpp
MT5HttpClient(const std::string& baseUrl, const std::string& apiKey,
              int timeoutMs = 5000, int poolSize = 8,
              int poolAcquireTimeoutMs = 100);
void connect();  // starts SSE + (comment) connection health monitor
```

`connect()` only starts `sseLoop()`. Health is **not** a background monitor; `IsConnected()` lazily GETs `/mt5/health` with a 5 s cache.

Auth on every REST and SSE request:

```text
X-API-Key: <apiKey>
```

REST also sends `Content-Type: application/json`. SSE sends `Accept: text/event-stream`.

URL construction: trailing slash is stripped from `baseUrl`; paths below are appended as written. **No URL-encoding** of group names, symbol names, or deal cursors.

---

## 2. REST paths that exist (quote the literals)

All paths are relative to `m_baseUrl` (example in `AppConfig`: `http://10.0.0.5:9100`).

| Method | Path literal / pattern | C++ method | Request body / query |
| --- | --- | --- | --- |
| `GET` | `/mt5/health` | `IsConnected` | none; success when JSON `success && connected` |
| `POST` | `/mt5/users` | `CreateUser` | `UserParams` serialized as JSON; response `login` |
| `GET` | `/mt5/users/` + login | `GetUser` | response `data` → `UserData` |
| `PUT` | `/mt5/users/` + login + `/group` | `UpdateUser`, `UpdateUserGroup` | `{"group": <string>}` |
| `DELETE` | `/mt5/users/` + login | `DeleteUser` | none |
| `PUT` | `/mt5/users/` + login + `/password` | `ChangePassword` | `{"password": <utf8>, "type": <uint32>}` |
| `POST` | `/mt5/users/` + login + `/check-password` | `CheckPassword` | `{"password": <utf8>, "type": <uint32>}` |
| `GET` | `/mt5/users/logins?group=` + utf8(group) | `GetUserLogins` | response `logins` |
| `GET` | `/mt5/accounts/` + login | `GetAccount` | response `data` → `AccountData` |
| `POST` | `/mt5/accounts/` + login + `/balance` | `DealerBalance` | `{"amount", "comment", "type"}`; response `deal_id` |
| `POST` | `/mt5/accounts/` + login + `/deposit` | `Deposit` | `{"amount", "comment"}`; response `deal_id` |
| `POST` | `/mt5/accounts/` + login + `/withdraw` | `Withdraw` | `{"amount", "comment"}`; response `deal_id` |
| `GET` | `/mt5/accounts/` + login + `/positions` | `GetPositions` | response `data` array |
| `GET` | `/mt5/accounts/` + login + `/deals?from=` + from + `&to=` + to [`&cursor=` …] or [`&page=` + n + `&limit=1000`] | `GetDeals` | see §2.1 |
| `GET` | `/mt5/symbols/count` | `SymbolTotal` | response `count` |
| `GET` | `/mt5/symbols/` + pos | `GetSymbol` | response `data` |
| `GET` | `/mt5/symbols/name/` + utf8(name) | `GetSymbolByName` | response `data` |
| `GET` | `/mt5/symbols/` + utf8(symbol) + `/tick` | `GetTickLast` | response `data` |
| `POST` | `/mt5/dealer/order` | `DealerSendOrder`, `SendTrade` (market only) | see §2.2 |
| `GET` | `/mt5/groups/count` | `GroupTotal` | response `count` |
| `GET` | `/mt5/groups` | `GetAllGroups` | response `groups` |
| `GET` | `/mt5/groups/` + utf8(group) + `/logins` | `GetGroupLogins` | response `logins` |
| `PUT` | `/mt5/users/` + login + `/leverage` | `UpdateUserLeverage` | `{"leverage": <uint32>}` |
| `PUT` | `/mt5/users/` + login + `/rights` | `UpdateUserRights` | `{"rights": <uint64>}` |
| `GET` | `/mt5/server/time` | `GetServerTime` | response `time` |
| `GET` | `/mt5/events/stream` | `sseLoop` | SSE, not JSON REST |

Quoted path strings as they appear in `mt5_http_client.cpp`:

```
"/mt5/health"
"/mt5/users"
"/mt5/users/" + login
"/mt5/users/" + login + "/group"
"/mt5/users/" + login                       // DELETE
"/mt5/users/" + login + "/password"
"/mt5/users/" + login + "/check-password"
"/mt5/users/logins?group=" + group
"/mt5/accounts/" + login
"/mt5/accounts/" + login + "/balance"
"/mt5/accounts/" + login + "/deposit"
"/mt5/accounts/" + login + "/withdraw"
"/mt5/accounts/" + login + "/positions"
"/mt5/accounts/" + login + "/deals?from=" + from + "&to=" + to
"/mt5/symbols/count"
"/mt5/symbols/" + pos
"/mt5/symbols/name/" + name
"/mt5/symbols/" + symbol + "/tick"
"/mt5/dealer/order"
"/mt5/groups/count"
"/mt5/groups"
"/mt5/groups/" + group + "/logins"
"/mt5/users/" + login + "/leverage"
"/mt5/users/" + login + "/rights"
"/mt5/server/time"
"/mt5/events/stream"
```

There is **one** trade-execution endpoint. Header comment:

> The remote MT5 microservice only exposes the single `POST /mt5/dealer/order` endpoint.

### 2.1 `GetDeals` pagination

Base: `/mt5/accounts/{login}/deals?from={from}&to={to}`

Continuation (max **10000** HTTP GETs; incomplete history → `false`, never a partial accept):

1. If `next_cursor` is present (top-level, `pagination.next_cursor`, or `data.next_cursor`) → append `&cursor=` + that value.
2. Else if `page > 1` → append `&page=` + page + `&limit=1000`.
3. `has_more` without a cursor is treated as failure.
4. Response `data` may be an array, or an object with `items` or `deals`.

### 2.2 `POST /mt5/dealer/order`

`DealerSendOrder` body:

```json
{ "login", "symbol", "action", "volume", "price" }
```

`SendTrade` (only `MT5TradeOp::MarketOrder`) body:

```json
{ "login": req.login, "symbol": req.symbol, "action": req.order_type, "volume": req.volume, "price": req.price }
```

Accepted response fields used: `success`, `order_id`, `deal_id`, `position_id`, `message`, `code`.  
`result_deal` / `result_position` stay 0 unless the bridge actually returns those fields. SL/TP are **not** applied atomically in remote mode (message suffix only).

Non-market ops (`pending` / modify / cancel) **do not** call HTTP. Local result: `supported=false`, `ok=false`, message that remote HTTP mode requires the local SDK manager. Comment says the service would return **HTTP 501** if those ops were invented; this client refuses to fabricate extra paths.

### 2.3 Operations with **no** HTTP path

| Interface | Behaviour |
| --- | --- |
| `GetNewsCalendarItems` / `GetCalendarEvents` | Local stub. `supported=false`, `status="unsupported"`, reason: *“no remote bridge endpoint is configured.”* |
| `GetGroupDetails` | Always `false`. Comment: needs direct SDK via `MT5Manager`. |
| `GetOrders` | Inherited default → `false`. |
| `GetChart` | Inherited default → `false`. |
| `SubscribeTicks` / `UnsubscribeTicks` | Inherited default → `false` (caller must poll `GetTickLast`). |
| `GetRecentDeals` / `CacheExecutedDeal` | Inherited no-ops. |
| `GetAllTicksLast`, `GetManagerSymbols`, `GetGroupSymbols` | Inherited empty / false. |

---

## 3. SSE events

**URL:** `{baseUrl}/mt5/events/stream`  
**Transport:** long-lived GET, `Accept: text/event-stream`, `X-API-Key`.  
**Line format expected:** `data: {json}` (prefix length 6, including the space). Other SSE fields (`event:`, `id:`, `retry:`, comments) are ignored. Blank lines are ignored.

JSON payload fields consumed:

- `type` (string)
- `login` (uint64, default 0)
- `data` (object, optional; deserialized by type)

### Event `type` strings that are mapped

These are the **only** strings in `typeMap`. Unknown types are dropped silently.

| SSE `type` | `MT5EventType` | `data` deserialized as |
| --- | --- | --- |
| `PositionAdd` | `PositionAdd` | `PositionData` |
| `PositionUpdate` | `PositionUpdate` | `PositionData` |
| `PositionDelete` | `PositionDelete` | `PositionData` |
| `DealAdd` | `DealAdd` | `DealData` |
| `DealUpdate` | `DealUpdate` | `DealData` |
| `DealDelete` | `DealDelete` | `DealData` |
| `OrderAdd` | `OrderAdd` | `OrderData` |
| `OrderUpdate` | `OrderUpdate` | `OrderData` |
| `OrderDelete` | `OrderDelete` | `OrderData` |
| `UserAdd` | `UserAdd` | `UserData` |
| `UserUpdate` | `UserUpdate` | `UserData` |
| `UserDelete` | `UserDelete` | `UserData` |

Parsed events are `m_eventQueue.push(...)`. JSON parse errors are logged and discarded.

There is no SSE tick event. Live quotes are not streamed over this client.

---

## 4. Timeouts and related knobs

### Constructor / pool (header defaults = production defaults)

| Knob | Default | Clamp / note |
| --- | --- | --- |
| `timeoutMs` (`m_timeoutMs`) | **5000** | applied as `CURLOPT_TIMEOUT_MS` on every REST call |
| `poolSize` | **8** | clamped `[0, 64]`; actual capacity = successful `curl_easy_init` count |
| `poolAcquireTimeoutMs` | **100** | clamped `[1, 5000]`; wait for a free handle |

`AppConfig` mirrors the same numbers (`mt5_http_timeout_ms=5000`, `mt5_http_pool_size=8`, `mt5_http_pool_acquire_timeout_ms=100`; env `MT5_HTTP_TIMEOUT_MS`, `MT5_HTTP_POOL_SIZE`, `MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS`).

### Per-REST curl options (`performCurlRequest`)

| Option | Value |
| --- | --- |
| `CURLOPT_TIMEOUT_MS` | `input.timeoutMs` (constructor `timeoutMs`, default **5000**) |
| `CURLOPT_CONNECTTIMEOUT_MS` | **2000** (hardcoded) |

Acquire failure is **pre-submit** (no wire call): JSON `success=false` with codes:

- `MT5_HTTP_POOL_ACQUIRE_TIMEOUT`
- `MT5_HTTP_POOL_UNAVAILABLE` (pool size 0)
- `MT5_HTTP_CLIENT_SHUTTING_DOWN`
- `MT5_HTTP_REQUEST_SETUP_FAILED` (setopt/header failed before `perform`)

Post-submit failures: `MT5_HTTP_REQUEST_FAILED`, `MT5_HTTP_INVALID_RESPONSE`. HTTP status ≥ 400 keeps parsed JSON but forces `success=false`. `SendTrade` treats the four no-send codes as `pre_submit_failure`.

### Health cache

`IsConnected()` reuses the last `/mt5/health` result for **5000 ms**.

### SSE loop (`sseLoop`)

| Option / behaviour | Value |
| --- | --- |
| `CURLOPT_TIMEOUT` | **0** — “No timeout — SSE is a long-lived connection” |
| `CURLOPT_CONNECTTIMEOUT_MS` | **5000** |
| `CURLOPT_LOW_SPEED_LIMIT` | **1** byte |
| `CURLOPT_LOW_SPEED_TIME` | **60** s (dead-stream abort if &lt; 1 byte/min) |
| Reconnect backoff start | **1000 ms** |
| Backoff growth | `min(backoff * 2, 30000)` |
| Max backoff | **30000 ms** |
| Sleep on `curl_easy_init` failure | current backoff |

On disconnect, `m_connected` is set `false`. Backoff is **not** reset on a successful connect (it only grows until the thread stops).

### Other loops (not curl timeouts)

| Loop | Limit |
| --- | --- |
| `GetDeals` continuation | 10000 GETs; page size `limit=1000` |
| `MT5EventQueue::pop` (types, not this client) | default wait **100 ms** |

---

## 5. Architecture note — C# workers should use this transport

Product direction (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`): C# / .NET 8 worker services (`apps/mt5-worker`, `apps/fix-worker`) sit above existing integrations. `mt5-sdk` already exposes two interchangeable `IMT5Client` transports:

- **local** — `MT5Manager` / Manager API (Windows, one connection slot).
- **remote** — this HTTP + SSE client against a shared MT5 microservice.

`apps/mt5-worker` is currently a `BackgroundService` stub (1 s heartbeat). It should **not** grow a second Manager-API stack or invent new REST shapes.

Preferred split:

1. Keep native Manager sessions inside the existing C++ microservice / `MT5Manager` pool (the process that already owns `/mt5/*`).
2. Have the C# MT5 worker speak the **same** contract listed above: REST paths in §2, SSE `/mt5/events/stream` + the 12 event names in §3, timeouts in §4 (`X-API-Key`, 5 s request timeout, 2 s REST connect, 5 s SSE connect, 60 s low-speed).
3. Do not add C++ paths for news/calendar, group details, order enumerate, charts, or tick subscribe — those are explicitly unsupported on this client. If the worker needs them, use **local** `MT5Manager` (or extend the **existing** microservice and then this client), not a parallel protocol.

`src/Mt5` is an empty `Class1` today. A C# client of this transport is the natural home for that assembly; the wire contract is already frozen in `mt5_http_client.cpp`.

---

## 6. Facts for implementers (do not invent)

- Single dealer path: `POST /mt5/dealer/order`.
- No remote news/calendar path.
- No remote group-detail path.
- No remote pending/modify/cancel path.
- No tick SSE.
- Paths and event names above are the complete set present in `mt5_http_client.cpp` as of this read.
