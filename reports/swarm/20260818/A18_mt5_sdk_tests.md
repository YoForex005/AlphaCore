# A18 — mt5-sdk tests vs live probes (C# must-not-break)

**Agent:** A18  
**Date:** 2026-08-18  
**Scope:** `D:\Prop\mt5-sdk\tests\*` + `D:\Prop\mt5-sdk\README.md` + CMake wiring + the types/impls those tests actually lock  
**Product source:** not modified  

This is a contract pin for the C#/.NET port (`TraderIntelligence.Mt5`, `TraderIntelligence.Mt5Worker`). The C++ suite is the measured behavior. C# must match it; it must not invent a friendlier remote API, retry a trade the broker may already have seen, or overwrite ledger evidence.

---

## 1. Inventory (six files, two CMake gates)

| File | Role | CMake target | Registered with CTest? | Network / DB |
| --- | --- | --- | --- | --- |
| `tests/mt5_time_window_test.cpp` | hermetic unit | `MT5SDK_BUILD_TESTS=ON` | yes | none |
| `tests/mt5_ledger_store_test.cpp` | hermetic unit (validators only) | `MT5SDK_BUILD_TESTS=ON` **and** `MT5SDK_WITH_POSTGRES=ON` | yes, when built | none (no `PgPool` call) |
| `tests/mt5_news_calendar_test.cpp` | hermetic unit | `MT5SDK_BUILD_TESTS=ON` | yes | none (remote path is a hard “unsupported”, never POSTs) |
| `tests/mt5_http_client_pool_timeout_test.cpp` | hermetic unit | `MT5SDK_BUILD_TESTS=ON` | yes | none (injected fake curl tokens) |
| `tests/mt5_group_probe.cpp` | live operator probe | `MT5SDK_BUILD_PROBES=ON` **and** `WIN32` | **no** | real Manager API |
| `tests/mt5_news_calendar_probe.cpp` | live operator probe | `MT5SDK_BUILD_PROBES=ON` **and** `WIN32` | **no** | real Manager API **or** remote HTTP (remote still does not invent a calendar bridge) |

README (`D:\Prop\mt5-sdk\README.md` §Tests and probes):

- Default build: **neither** tests nor probes.
- `MT5SDK_BUILD_TESTS=ON` → `ctest` unit binaries. Quote: *“The unit tests are hermetic — no MT5 server, no database.”*
- `MT5SDK_BUILD_PROBES=ON` → two standalone diagnostics. Quote: *“they read `.env`, open a real connection and print a JSON report.”*

CMake (`D:\Prop\mt5-sdk\CMakeLists.txt`):

- Hermetic list is explicit: `mt5_time_window_test`, `mt5_ledger_store_test`, `mt5_news_calendar_test`, `mt5_http_client_pool_timeout_test`.
- `mt5_ledger_store_test` is skipped unless Postgres is compiled in (header pulls `PgPool`). The binary still never opens a connection.
- Probes are **Windows-only**, copy `MT5APIManager64.dll` / `MetaQuotes.MT5ManagerAPI64.dll` / `MetaQuotes.MT5CommonAPI64.dll` beside the exe, and are **not** `add_test`.

C# must keep the same split: CI unit tests stay hermetic; live probes stay opt-in operator tools.

---

## 2. Hermetic tests (what they lock)

### 2.1 `mt5_time_window_test` — history window arithmetic

**Fixture:** in-process `FakeMt5Client : IMT5Client`. No sockets.

| Case | Input | Locked result |
| --- | --- | --- |
| Connected + sane server time | `serverTime = 1778066185`, lookback `365*24*3600` | `to == serverTime`, `from == to - lookback`, `source == "mt5_server_time"`, `usedFallback == false` |
| Disconnected | `IsConnected() == false` | `to` is host `time(nullptr)` (captured around the call), `from == to - lookback`, `source == "host_time_fallback"`, `usedFallback == true` |
| Explicit bounds | `explicitFrom=1000`, `explicitTo=2000` (even while disconnected) | `from=1000`, `to=2000`, `source == "explicit"` — host/server unused for bounds |
| Parse epoch | `"1778066185"`, `endOfDay=false` | `1778066185` |
| Parse garbage | `"not-a-date"` | `0` |

Implementation extras C# must keep even though the test does not name them (`src/services/mt5_time_window.cpp`):

- Missing `explicitTo` uses validated MT5 server time, else host.
- Sane server time: `>= 946684800` (2000-01-01 UTC) and `<= host + 7 days`.
- Missing `explicitFrom` → `to - lookbackSeconds`; clamp `from` to `>= 0`.
- Date-only `YYYY-MM-DD` is accepted; `endOfDay=true` means `23:59:59` local via `mktime` (inclusive full-day). Digits-only is always epoch seconds, never a date.
- `GetServerTime` / `IsConnected` exceptions become fallback, not a thrown API error.

`IMT5Client::GetDeals` contract (not asserted in this binary, but this window is what callers pass): complete `[from,to]` history or return `false`. Callers must treat `false` as `dependency_unavailable` and **must not** make a pass/fail trading decision on partial pages.

### 2.2 `mt5_ledger_store_test` — write-side validators only

No INSERT. Locks `mt5_ledger::Store::isSha256Hex` / `isValid`.

| Rule | Must accept | Must reject |
| --- | --- | --- |
| SHA-256 hex | exactly 64 hex digits (`64 × 'a'`) | `"not-a-hash"` (wrong length / not hex) |
| `RawEvent` | `serverKey`, `sourceEventId`, `entityType` non-empty; `payload` is a JSON **object**; `payloadSchemaVersion > 0`; `eventKind` ∈ `{add, update, delete, clean, sync, snapshot}`; valid hash | `eventKind == "invalid"`; null/non-object payload; empty keys |
| `DealRevision` | `serverKey` non-empty; `dealTicket > 0`; `revisionNo > 0`; `accountLogin > 0`; `brokerTime` non-empty; `rawEventId` non-empty; valid hash; `currency` absent **or** exactly 3 **uppercase** letters | `currency == "usd"` |

Header comments C# must not break (`src/services/mt5_ledger_store.h` / `.cpp`):

- Store accepts **no credentials**.
- Duplicate `(server_key, source_event_id)` → return the original UUID (`ON CONFLICT DO NOTHING` + re-select). Never update the first payload.
- Duplicate `(server_key, deal_ticket, revision_no)` → no-op. Corrections require a **new** `revision_no` and a **new** source event. Historical broker evidence is immutable.
- Default money strings: `profit/commission/swap/fee` default `"0"` if the C# model ports those fields.

### 2.3 `mt5_news_calendar_test` — JSON DTO + remote fail-closed

No live HTTP. Constructs `MT5HttpClient("http://127.0.0.1:9", "test-key", 1, 1)` only to prove the calendar method **does not** call the discarded port.

Locked constants / helpers (`mt5_types.h` `namespace mt5_news_calendar`):

| Symbol | Value / rule |
| --- | --- |
| `kCalendarFlag` | `0x0008` |
| `kNoBodyFlag` | `0x0004` |
| `kDefaultMaxItems` | `100` |
| `kMaxItemsLimit` | `1000` |
| `normalizedMaxItems(0)` | `100` |
| `normalizedMaxItems(>1000)` | `1000` (test serializes `max_items=5000` → parse back as `1000`) |
| `matchesWindow(ts, from, to)` | half-open `[from, to)`: `100` and `199` in `[100,200)` match; `99` and `200` do not |
| `isCalendarFlag` | `(flags & 0x0008) != 0` |
| `hasUnsupportedFilters` | any of `currency` / `country` / `impact` non-empty → true (provider does not filter server-side) |

JSON contracts:

- `MT5CalendarQuery` round-trips `from`, `to`, `max_items` (clamped), `include_body`.
- `MT5NewsCalendarItem` serializes `body` (empty string when unloaded), `body_present`, `body_size`, `body_loaded`. Absence of body text is **not** absence of body metadata.
- `MT5NewsCalendarResult.metadata` must carry `news_next_error_count`, `body_request_error_count`, `partial`, `returned_count`, `supported`, `status`, `source`.
- Round-trip parse must restore `success`, `items[0].id`, and those metadata fields.

Remote `GetNewsCalendarItems` (hard-coded in `MT5HttpClient`, not a network 501):

```
success = false
metadata.supported = false
metadata.status = "unsupported"
reason contains "no remote bridge endpoint"
metadata.include_body echoed from the query
```

C# **must not** invent `GET/POST /mt5/news` (or similar) and pretend calendar works in remote mode. The remote microservice has no calendar bridge. Fabricating one would desync C++/C# and the live probe.

Default `IMT5Client::GetNewsCalendarItems` (unoverridden clients) is the same shape with reason *“MT5 news/calendar provider is not implemented for this client.”* `GetCalendarEvents` is an alias.

### 2.4 `mt5_http_client_pool_timeout_test` — pool, no-send vs attempted, clamps

Uses `MT5HttpClientPoolTestAccess` + fake `CURL*` tokens (`0x1000+n`). **Zero** real sockets. Five scenarios.

**Trade request used:** `MT5TradeOp::MarketOrder`, login `10001`, `EURUSD`, `mt5_op::BUY` (`0`), volume `10000` (native units, not lots).

**Success fake body:** `{"success":true,"order_id":12345}` → `SendTrade.ok == true`. `order_id` maps to `result_order` only.

#### A. Saturated checkout, spurious wakeup, reuse

- Pool size `1`, acquire timeout `80` ms.
- First `SendTrade` holds the only handle at the perform boundary.
- Second `SendTrade` is woken repeatedly via `notify_all` **without** a free handle.
- Locked:
  - waiter returns **no-send**: `supported && !ok && pre_submit_failure`
  - `retcode == 0`
  - `mt5_transport_latency_ms` **absent**
  - wall time `>= 50 ms` and `< 800 ms` (absolute deadline, not “wait forever on CV spam”)
  - fake `requestOperation` invoked **once** (the waiter never submitted)
- After release: first call succeeds; a third call reuses the same token (`createCalls==1`, `resetCalls==2`, `cleanupCalls==1`).

#### B. Unavailable + clamp

| Construct | Locked |
| --- | --- |
| `poolSize=0` | immediate no-send; no wait (`elapsed < 100 ms`); `requestCalls==0`; no transport latency |
| all `createHandle` return null (`poolSize=3`) | no-send; `requestCalls==0`; `effectiveCapacity==0` |
| `poolSize=-4`, `acquireTimeoutMs=0` | acquire timeout clamped to **1 ms**; capacity **0** |
| `poolSize=100`, `acquireTimeoutMs=9000` | timeout clamped to **5000 ms**; capacity **64**; exactly 64 create + 64 cleanup on destroy |
| default acquire in this harness | **100 ms** (ctor default) |

#### C. Shutdown wakes waiter and drains lease

- Active call in-flight; second call waiting for a handle; `shutdownPool()`.
- Waiter ready within 500 ms, classified **no-send**, no transport latency, **did not** enter `requestOperation`.
- Shutdown **must not** `cleanup` the in-flight token until the lease returns.
- In-flight call still succeeds; then exactly one cleanup.

#### D. Setup vs attempt exception classification

| Failure | Internal `httpPost` JSON | `SendTrade` |
| --- | --- | --- |
| setup return (no `markPerformStarted`) or setup throw | `code == "MT5_HTTP_REQUEST_SETUP_FAILED"`, message exactly `"MT5 HTTP request setup failed before submission"`, latency pointer cleared | `supported && !ok && pre_submit_failure && retcode==0 && !mt5_transport_latency_ms` |
| throw **after** `markPerformStarted` | (via SendTrade) | `!ok && !pre_submit_failure && mt5_transport_latency_ms.has_value()` — **attempted / ambiguous** |
| perform returns `CURLE_OPERATION_TIMEDOUT` after boundary | | same: attempted, timing present |
| later success | | lease still reusable |

No-send codes (`isNoSendCode`) — these are the **only** codes that set `pre_submit_failure` after a remote POST attempt mapping:

- `MT5_HTTP_POOL_ACQUIRE_TIMEOUT`
- `MT5_HTTP_POOL_UNAVAILABLE`
- `MT5_HTTP_CLIENT_SHUTTING_DOWN`
- `MT5_HTTP_REQUEST_SETUP_FAILED`

Not no-send (broker **may** have seen the request): `MT5_HTTP_REQUEST_FAILED`, `MT5_HTTP_INVALID_RESPONSE`.

Sanitized messages: exception text from setup/perform **must not** leak into `MT5TradeResult.message` / JSON `message` for the setup-failed path. The test asserts the canned string.

#### E. Timing idempotence + unsupported bypass

- `finishPerform()` twice must **not** recompute duration (`firstFinishMs == secondFinishMs`).
- `MT5TradeOp::ModifyOrder` (and by implementation any non-`MarketOrder`): `!supported && !ok`, **zero** pool borrow / HTTP.
- Remote supports **only** `MarketOrder` → `POST /mt5/dealer/order`. Pending / modify position / modify order / cancel / close = unsupported (product maps that to HTTP 501). Do not fabricate endpoints.

Other SendTrade rules the test leans on (impl `mt5_http_client.cpp`):

- Transport latency is **perform duration only** (excludes checkout, JSON build, post-submit lookups).
- Do **not** alias `result_deal := result_order`. Remote body today only guarantees `order_id`. Missing `deal_id` / `position_id` stay `0` (omit on the wire in the service layer).
- SL/TP flags are **not** applied atomically in remote mode (message suffix if `set_sl` / `set_tp`).
- Auth header for real HTTP is `X-API-Key`. Connect timeout 2000 ms. Trailing slash stripped from base URL.

---

## 3. Live probes (not CI, real credentials)

Both load `AppConfig` from `{sourceDir}/.env` else `./.env`. `PROPFIRM_SOURCE_DIR` overrides source dir. Logging is forced off. Passwords are never printed.

### 3.1 `mt5_group_probe`

Purpose: enumerate **every group the manager login can see**. Architecture law: plan-to-group env maps (`MT5_GROUP_*`) must not decide the fetch set.

| Step | Behavior |
| --- | --- |
| `MT5_MODE=remote` | fail JSON, exit **3**. HTTP client has no group-list this probe will use. |
| missing `mt5_server` / `mt5_login==0` / empty password | exit **2**, reason `ERROR: missing_manager_credentials` |
| `Initialize(sourceDir/MetaTrader5SDK/Libs)` fail | exit **2**, `ERROR: sdk_init_failed` |
| proxy enabled but incomplete | exit **2**, `ERROR: proxy_config_invalid` |
| `Connect(server:port, login, password, pumpMode=0)` fail | exit **4**, `ERROR: connect_failed` + optional `sdk_reason` from `GetLastError()` (never the password) |
| `GetAllGroups` fail | exit **5**, `ERROR: groups_api_unavailable`, `connection.success=true` |
| success | exit **0**, groups **sorted + unique**, `total`, `connection.server` = display name |

JSON envelope: `{probe, connection{success, reason?, server?, sdk_reason?}, success, total, groups[]}`.

Proxy types: `SOCKS5` (default), `SOCKS4`, `HTTP`. Toggle env: `IS_MT5_PROXY_ENABLED`.

**Note:** probe DLL path is `MetaTrader5SDK/Libs` under source dir, not CMake’s `vendor/MetaTrader5SDK/Libs`. Operator layout must match or init fails. C# probe ports should document the same.

### 3.2 `mt5_news_calendar_probe`

| Mode | Provider | What it does |
| --- | --- | --- |
| `local` (default) | `MT5Manager` | Init SDK, optional proxy, `Connect(..., PUMP_MODE_NEWS)`, `GetNewsCalendarItems({max_items=5, include_body=false})` |
| `remote` | `MT5HttpClient` | No Manager connect. Calls `GetNewsCalendarItems` which **always** returns unsupported (see §2.3). Sample is `null`. `connection.success` is false with the metadata reason. |

Exit: `0` iff top-level `success` is true, else `2`.

Success/local JSON includes compact metadata (`supported`, `status`, `reason`, `source`, counts, `partial`, pump flags, `max_items`, `include_body`) and a **single** sample item (`id`, `provider_event_id`, `subject`, `category`, `timestamp`, `language`, `flags`, `is_calendar`, `body_present`, `body_size`, `body_loaded`) — **no body text**, no credentials.

Blocked envelope uses `metadata.status == "blocked"`, `source == "none"`.

C# must not:

- Request news without `PUMP_MODE_NEWS` in local mode and then claim “broker has no calendar”.
- Dump news bodies or manager passwords into probe stdout.
- Treat remote unsupported as a transport outage of a missing URL; it is a **capability** miss.

---

## 4. What C# must not break

Treat these as frozen contracts for `TraderIntelligence.Mt5` / worker / any HTTP façade. Current C# trees are stubs (`src/Mt5/Class1.cs`, `apps/mt5-worker/Worker.cs`); do not “simplify” while filling them in.

### 4.1 Transports and interface

- One client interface (`IMT5Client` / `IMt5BrokerConnector`). Two impls: **local** Manager API (Windows x64, consumes a manager slot) and **remote** HTTP.
- Select with `MT5_MODE=local|remote`. Do not fork business logic per broker (Achiever / StarwaveFX / future).
- Local is Windows-only. Do not ship Manager DLLs in Linux containers.
- Runtime DLLs must sit beside the consuming exe: `MT5APIManager64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MetaQuotes.MT5CommonAPI64.dll`.
- Native Manager API is the source of truth for groups/accounts/deals/positions. Remote is a microservice façade, not a second product.

### 4.2 Trade result machine (double-submit hazard)

`MT5TradeResult` fields that C# must keep semantically:

| Field | Meaning |
| --- | --- |
| `supported` | false → this client cannot do the op (HTTP 501). **No** broker call. |
| `ok` | MT5/remote accepted. |
| `retcode` | raw MTAPIRES; **0** on no-send. |
| `result_order` / `result_deal` / `result_position` | distinct ids. Never copy order → deal. |
| `mt5_transport_latency_ms` | set **only** if perform/DealerSend actually started. |
| `pre_submit_failure` | **only** if DealerSend / HTTP perform never started. Safe to retry. If false and `!ok`, treat as **ambiguous** — do not blindly resubmit. |

Pool checkout timeout / empty pool / shutdown / request-setup failure = no-send. Post-boundary curl timeout / throw / HTTP 4xx parse = attempted.

C# must not:

- Retry a trade when `pre_submit_failure == false`.
- Expose checkout time as `mt5_transport_latency_ms`.
- Wake a waiter and treat “CV signaled” as “handle acquired”.
- Clean up an in-flight handle on shutdown.
- Send pending/modify/cancel/close through remote HTTP.
- Convert MT5 volume as lots without the native-unit scale the C++ request already uses (`volume` is MT5 integer units).

### 4.3 HTTP pool knobs

- Acquire timeout clamp **[1, 5000] ms**. Default **100**.
- Pool size clamp **[0, 64]**. Default **8**. Negative → 0 (unavailable, no-send, no wait).
- Config keys: `MT5_HTTP_TIMEOUT_MS`, `MT5_HTTP_POOL_SIZE`, `MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS` (already clamped in `AppConfig::load`).
- Spurious `notify` must not reset the acquire deadline.
- Setup-failure JSON `code` / `message` strings above are part of the contract.

### 4.4 News / calendar

- Do not add remote calendar endpoints to “make the probe green”.
- Clamp `max_items` at 1000; default 100.
- Window is `[from, to)`.
- Calendar bit is `0x0008`. Currency/country/impact filters are **unsupported** (caller-side only).
- Preserve body metadata without requiring body bytes.
- Keep `partial`, `news_next_error_count`, `body_request_error_count` — do not drop error counters to fake `success`.

### 4.5 Ledger / reconstruction

- SHA-256 hex only, 64 chars.
- Event kinds closed set.
- Currency ISO-like: 3 uppercase or omit.
- Idempotent insert; **never UPDATE** a stored raw event or deal revision.
- New broker correction = new revision + new source event.

### 4.6 Time windows and deals

- Prefer MT5 server time; host fallback when disconnected / insane / exception.
- Explicit `from`/`to` win.
- `GetDeals` must follow every page/cursor or return false. Incomplete + `has_more` without a new cursor = failure. Never accept partial history as complete.
- `GetRecentDeals` / `CacheExecutedDeal` exist because broker `DealRequest` history can lag **>40 s**. Only the pump local manager implements them. Remote/pool stay unsupported (`false`). Do not fake a ring on HTTP.

### 4.7 Groups and config

- Discover **all** manager-visible groups. Plan maps (`MT5_GROUP_2STEP_DEMO=demo\yo-2step`, etc.) are optional labels only.
- `demo\Maxmaster` is not the only Achiever group.
- Config resolution: **process env → `.env` → built-in default**.
- Blank `MT5_SERVER_NAME` derives from endpoint host (remote URL or `MT5_SERVER`), else `"MT5"`.
- Proxy: `IS_MT5_PROXY_ENABLED`, types SOCKS5/SOCKS4/HTTP. Never log proxy or manager passwords. `.env` stays gitignored.
- `AppConfig` in this SDK is MT5 + Postgres/logging keys only. Do not drag payment/KYC/email into the MT5 worker config surface.

### 4.8 Metrics / identity

- Trade-stage metric names in `metrics_service.h` are stable labels. **Never** put login / user / request id on a metric label.
- Probe / API JSON must not echo secrets. `GetLastError` is already treated as non-secret but still not a password field.

### 4.9 Test policy for the C# port

- Port the four hermetic binaries as xUnit/NUnit tests with the **same assertions** (clamps, no-send vs attempted, calendar JSON, ledger validators, window sources).
- Do **not** put group/news probes on `dotnet test` in CI.
- Do **not** hit Achiever (`57.128.141.65`) or StarwaveFX (`84.201.6.142`) from unit tests.
- Do not require Postgres for validator tests.
- Fake/inject the HTTP transport the same way C++ injects `RequestDependencies`. A discarded TCP port is not a mock if the method never dials — calendar remote is capability-closed.

### 4.10 Architecture constraints that sit next to these tests

From `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` (C# is the chosen stack):

- Do not assume cTrader is an LP.
- Do not convert MT5 lots blindly to cTrader `OrderQty`.
- Do not compute MFE/MAE from closed deals alone.
- QUOTE and TRADE FIX sessions are independent; no second active TRADE session for the same account.
- Redis is not the book of record for orders/positions/balances.
- Execution default stays disabled until shadow/recon/risk are proven.

Those are product laws; the SDK tests above are the **measurable** MT5-side subset.

---

## 5. Build commands (reference, not executed this pass)

```text
cmake -B build -S D:\Prop\mt5-sdk -DMT5SDK_BUILD_TESTS=ON -DMT5SDK_WITH_POSTGRES=ON  <vcpkg>
cmake --build build --config Release
ctest --test-dir build -C Release --output-on-failure

cmake -B build -S D:\Prop\mt5-sdk -DMT5SDK_BUILD_PROBES=ON   # Windows; not CTest
```

Core lib deps: nlohmann_json + spdlog + libcurl. Postgres and Drogon remain opt-in. C# should not silently take a hard dependency on Drogon/libpq for the equivalent of the core client.

---

## 6. Gaps / caveats (honest)

- `mt5_ledger_store_test` does **not** exercise `recordRawEvent` / `recordDealRevision` SQL. Idempotency `ON CONFLICT` is header/impl law, not a green test. C# must still implement it.
- Time-window test does not cover insane server time, thrown `GetServerTime`, date-only `endOfDay`, or `from < 0` clamp. Those rules live in the .cpp and should be ported.
- HTTP test covers `SendTrade` + internal `httpPost` setup codes. It does not lock `/mt5/health` cache (5 s), SSE, or `GetDeals` pagination. Pagination “never accept partial” is still a must-not-break from `imt5_client.h` + `GetDeals` impl.
- Group probe refuses remote mode; HTTP `GetAllGroups` exists on `MT5HttpClient` but this probe will not use it. Do not “fix” the probe by silently switching to HTTP — remote group list is a different, untested path.
- Probe DLL directory (`MetaTrader5SDK/Libs`) ≠ vendored CMake path (`vendor/MetaTrader5SDK/Libs`).
- README mentions `.env.example`; that file is **not** present under `D:\Prop\mt5-sdk\` in this tree.
- No test covers `MT5Manager` reconnect, tick sink “enqueue-only on pump thread”, or chart aggregation.

---

## 7. One-page C# checklist

1. Hermetic tests stay hermetic; probes stay opt-in and Windows/Manager-real.  
2. `pre_submit_failure` only when the broker never saw the order.  
3. Remote `SendTrade` = market only; everything else `supported=false`, no HTTP.  
4. Remote calendar = unsupported, reason contains `no remote bridge endpoint`.  
5. Pool: timeout 1–5000 ms, size 0–64, deadline survives spurious wakeups, shutdown does not destroy leased handles.  
6. `max_items` clamp 1000; calendar window `[from,to)`; flag `0x0008`.  
7. Ledger: 64-hex hash; closed event kinds; uppercase ISO currency; no overwrite.  
8. Time window: server → host fallback → explicit override.  
9. `GetDeals` complete or fail; never pass/fail on partial history.  
10. Enumerate all manager groups; do not filter to `MT5_GROUP_*`.  
11. Never log passwords / proxy secrets / news bodies in probes.  
12. Never alias deal id to order id.  
13. Volume stays MT5 native units at this boundary.  
14. Metric labels stay fixed-cardinality.

**Sources:** `D:\Prop\mt5-sdk\README.md`, `D:\Prop\mt5-sdk\CMakeLists.txt`, `D:\Prop\mt5-sdk\tests\*.cpp`, `D:\Prop\mt5-sdk\src\core\imt5_client.h`, `mt5_types.h`, `mt5_http_client.{h,cpp}`, `D:\Prop\mt5-sdk\src\services\mt5_time_window.{h,cpp}`, `mt5_ledger_store.{h,cpp}`, `D:\Prop\mt5-sdk\config\app_config.{h,cpp}`.
