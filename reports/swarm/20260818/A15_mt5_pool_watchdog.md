# A15 — MT5 pool + watchdog (pool size, acquire timeout, reconnect backoff)

**Agent:** A15  
**Date:** 2026-08-18  
**Scope (read-only):** `D:\Prop\mt5-sdk\src\core\mt5_pool.h`, `mt5_pool.cpp`, `mt5_watchdog.h`, `mt5_watchdog.cpp`  
**Supporting evidence (read-only):** `config/app_config.{h,cpp}`, `.env.example`, `README.md`, `mt5_manager.cpp`, `mt5_tick_bridge.cpp`, `mt5_http_client.{h,cpp}`, architecture v2 §7 / §56  
**Product source modified:** none

---

## 1. Verdict (true measured state)

Two **separate** supervision paths exist. They do not call each other.

| Layer | What it owns | Size | Acquire timeout | Reconnect backoff |
|---|---|---|---|---|
| `MT5Pool` | Request-only Manager API sessions (`Connect` mode=`0`, no pump) | Default **8**; env `MT5_POOL_SIZE` | `Borrow(timeoutMs)` default **5000 ms**; callers may pass a tighter budget | **No exponential backoff.** Dead-session `Connect` is a single attempt. Init/healthCheck use a **fixed 5000 ms** connect timeout. `Borrow` of a dead session uses **remaining acquire budget**, floor **50 ms**. |
| `MT5Watchdog` | The **pump** `MT5Manager` (not the pool) | N/A (one manager) | N/A | Check every **30 s**. On drop: wait **5 → 10 → 20 → 40 → 60 s** (cap), then `MT5Manager::Connect` (SDK timeout **30000 ms**). Reset to **5 s** on success. |

Honest gap: this tree **loads** `MT5_POOL_SIZE` into `AppConfig::mt5_pool_size` but has **no call site** that passes it into `MT5Pool::Initialize`. `MT5Watchdog` and `MT5Pool::healthCheck()` also have **no in-tree scheduler**. The numbers below are the **library contracts**, not a proven production wiring.

---

## 2. Architecture: `MT5_POOL_SIZE`

### 2.1 Dual-connection local-mode design

Local transport (`MT5_MODE=local`) is **two connection classes**, not one:

```
                    ┌─────────────────────────────────────────┐
  env MT5_POOL_SIZE │  AppConfig.mt5_pool_size  (default 8)   │
                    └──────────────────┬──────────────────────┘
                                       │ intended argument
                                       ▼
  HTTP / trade / poll ──► MT5Pool ──► N × MT5Session
                          Borrow()     each: own IMTManagerAPI + m_mutex
                          Return()     Connect(..., mode=0, timeout)
                          healthCheck()  idle Ping + reconnect
                                       │
                                       │ isolated from
                                       ▼
  pump / sinks / events ──► MT5Manager ──► 1 × IMTManagerAPI
                            m_mutex        Connect(..., PUMP_MODE_*, 30000)
                            OnTick/OnDeal  watched by MT5Watchdog
```

Evidence:

- `MT5Pool` comment: “Pool of MT5 sessions for HTTP request handling. Separate from the pump mode connection in MT5Manager.” (`mt5_pool.h` ~106–107)
- Session connect is explicit no-pump: `m_manager->Connect(..., 0, timeoutMs)` (`mt5_pool.cpp` 75–77)
- `MT5Manager::Connect` defaults `mode==0` to `PUMP_MODE_USERS|ORDERS|POSITIONS|SYMBOLS` and uses **30000 ms** (`mt5_manager.cpp` 101–114)
- Each `MT5Session` has its own `m_mutex` + `IMTManagerAPI`. `SendTrade` is documented as “true N-way concurrency instead of serializing on the pump `m_mutex`” (`mt5_pool.cpp` 360–363)
- Historical charts stay on request sessions so `ChartRequest` never owns the pump mutex (`mt5_pool.h` 69–73)

`MT5_POOL_SIZE` is therefore **broker manager-slot count**, not CPU count. `.env.example` says so:

> Concurrent manager sessions held open by MT5Pool. Size to your broker's manager connection limit, not to your CPU count. default: 8

A live pool of size *N* plus the pump manager consumes **N+1** manager connections (plus any failed-but-constructed sessions that still exist in `m_sessions` but were never pushed to `m_available`).

### 2.2 Config surface

| Knob | Default | Loader | Clamp |
|---|---|---|---|
| `MT5_POOL_SIZE` → `AppConfig::mt5_pool_size` | **8** | process env, then `.env`, then default (`app_config.cpp` 116) | **none** |
| `MT5Pool::Initialize(..., poolSize)` | **8** (`mt5_pool.h` 119, `m_poolSize` 161) | caller-supplied | **none** |
| `MT5_HTTP_POOL_SIZE` (curl handles, remote mode) | **8** | same loader | ctor clamp **0..64** |
| `MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS` | **100** | same loader | **1..5000** |

Architecture v2 (not code): Achiever `MT5_POOL_SIZE=8`; StarwaveFX `MT5_STARWAVEFX_POOL_SIZE=4`. The extracted SDK `AppConfig` has **no** `MT5_STARWAVEFX_POOL_SIZE` field — broker-specific sizing is an outer-product concern.

`MT5_MODE=remote` does **not** use `MT5Pool` / `MT5_POOL_SIZE`. Remote uses `MT5HttpClient` curl-handle pool (`MT5_HTTP_POOL_SIZE`) and SSE reconnect (`1000 ms → 30000 ms`).

### 2.3 Init semantics (what the size actually means)

`MT5Pool::Initialize` (`mt5_pool.cpp` 888–952):

1. Stashes `dllPath`, server, login, password, `m_poolSize = poolSize`, and a snapshot of `m_proxy` under `m_mutex`, then **releases the lock**.
2. `CMTManagerAPIFactory::Initialize` **outside** the lock.
3. Builds **exactly `poolSize`** `MT5Session` objects. Each: `Initialize` → optional `SetProxy` → `Connect(..., 5000)`.
4. **Every** session object is published into `m_sessions` (including connect failures).
5. Only **successfully connected** sessions are pushed to `m_available`.
6. Returns true iff `connected > 0`. Log: `"MT5Pool: {}/{} sessions connected"`.

So `m_poolSize` / `totalSessions()` is the **configured / constructed** count, not the live available count. A pool of 8 with 3 connect failures still reports `totalSessions()==8` and `availableSessions()==3`. Failed sessions sit in `m_sessions` and are **never** added to `m_available` at init — they can only become usable if a later `Borrow` somehow checked them out (it cannot; they are not queued) or if a future change reused `m_sessions` (today `healthCheck` only walks `m_available`).

Blocking I/O is kept off `m_mutex` so concurrent `Borrow(timeoutMs)` is not stalled for `O(poolSize * 5s)` during init. Worst-case init wall clock is sequential **~poolSize × 5000 ms** of `Connect` (8 × 5 s = **40 s** if every session times out).

### 2.4 Who borrows, and how size interacts with acquire

In this tree the only coded borrower is `MT5TickBridge` poll path (`mt5_tick_bridge.cpp` 314–320):

```cpp
MT5Pool::ScopedSession session(*pool_, /*timeoutMs=*/200);
if (!session) return;   // skip this poll tick; never fall back to pump
```

`Borrow` comments also cite `terminal_quote_hub.cpp:148 timeoutMs=300`. That file is **not present** under `D:\Prop`. Treat 300 ms as a documented intended caller, not a measured in-tree site.

Implication of size 8 + 200 ms tick borrow: if ≥8 request sessions are checked out (trades, account reads, charts), the tick poll **skips** rather than blocking the Drogon loop or touching the pump mutex.

### 2.5 In-tree wiring status (do not greenwash)

| Expected consumer | Found in `D:\Prop`? |
|---|---|
| `pool.Initialize(dll, server, login, password, config.mt5_pool_size)` | **No** |
| Periodic `pool.healthCheck()` | **No** |
| `MT5Watchdog` construct / `start()` | **No** (class compiled on WIN32 via `CMakeLists.txt` 52–57 only) |
| `MT5TickBridge(..., pool)` | Class exists (`MT5SDK_WITH_DROGON`); no app wiring here |

`README.md` “Use it from another project” sample constructs **only** `MT5Manager` and never `MT5Pool` / `MT5Watchdog`. The env knob is real; the extracted SDK does not auto-apply it.

---

## 3. Acquire timeout (`MT5Pool::Borrow`)

### 3.1 Contract

| API | Default | Meaning |
|---|---|---|
| `MT5Pool::Borrow(int timeoutMs = 5000)` | **5000 ms** | Hard ceiling on **total** checkout latency (CV wait **plus** optional dead-session reconnect). Returns `nullptr` on timeout / failed reconnect. Caller **must** `Return()`. |
| `MT5Pool::ScopedSession(pool, timeoutMs = 5000)` | **5000 ms** | RAII `Borrow` / `Return`. Bool-false if acquire failed. |
| `MT5Session::Connect(..., timeoutMs = 30000)` | **30000 ms** | Per-session SDK connect. Pool **overrides** this to 5000 (init/health) or remaining Borrow budget. |
| Tick poll borrow | **200 ms** | Event-loop safe. |
| Quoted quote-hub borrow | **300 ms** | Comment only; file absent here. |

There is **no** `MT5_POOL_ACQUIRE_TIMEOUT_MS` env key. Native-pool acquire timeout is a **call-site argument**, unlike remote HTTP (`MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS`, default 100, clamp 1..5000).

### 3.2 Algorithm (`mt5_pool.cpp` 969–1027)

1. `deadline = now + timeoutMs`.
2. Lock `m_mutex`. `m_cv.wait_until(deadline, !m_available.empty())`. On expiry: warn `"Borrow timeout after {}ms"`, return `nullptr`.
3. Pop front of `m_available`, **unlock**.
4. If `session->IsConnected()`: return it (no Ping on the hot path).
5. Else remaining = `deadline - now`:
   - If `remaining < 50` (`kMinReconnectMs`): `Return(session)`, return `nullptr`.
   - Else `Connect(server, login, password, remaining)`. On failure: `Return(session)`, return `nullptr`.
6. Success: return the live session.

`timeoutMs` is therefore **not** “CV wait only”. A stale checkout cannot add a hidden extra 5 s `Connect` on top of a 200 ms budget.

`IsConnected()` is an atomic flag, **not** a Ping. A half-dead TCP session with `m_connected==true` is handed out and fails at the next Manager call. Healing of still-flagged-connected zombies is `healthCheck()`’s job (`Ping` via `TimeServer() > 0`).

### 3.3 Return / available

`Return` (`1029–1034`): null-safe; push + `notify_one`. No health check on return.

`availableSessions()`: `m_available.size()` under lock.

`Shutdown`: drain queue, `Disconnect` every session, `m_sessions.clear()`, `m_factory.Shutdown()`. Shutdown holds `m_mutex` across `Disconnect()` (unlike Borrow/healthCheck/Initialize).

### 3.4 Contrast: HTTP handle acquire (not `MT5_POOL_SIZE`)

`MT5HttpClient::borrowHandle`: wait up to `m_poolAcquireTimeoutMs` (default **100 ms**) for a curl easy handle. Statuses: Acquired / TimedOut / ShuttingDown / Unavailable. No reconnect of the handle; SSE reconnect is a different loop (see §5).

---

## 4. Pool reconnect (no backoff)

`MT5Pool` reconnect is **single-shot**. There is no 5/10/20/40/60 sequence on the pool.

| Path | When | Connect timeout | On failure |
|---|---|---|---|
| `Initialize` | Startup, each of `poolSize` sessions | **5000 ms** fixed | Session stays in `m_sessions`, **not** queued; pool still inits if ≥1 connected |
| `Borrow` dead session | `!IsConnected()` after checkout | **min(remaining, caller budget)**, require ≥ **50 ms** | Session returned to queue; caller gets `nullptr` |
| `healthCheck` | Idle sessions drained from `m_available` | **5000 ms** fixed after `Disconnect()` | Session **still returned** to `m_available` (dead or live). No leak. |

`healthCheck` (`1041–1097`):

1. Under lock, **drain all idle** sessions into a local vector (checked-out sessions are **not** pinged).
2. Unlock. For each: if `!Ping()` → `Disconnect()` → `Connect(..., 5000)`.
3. Relock. Push **all** (including failed reconnects) back; `notify_one` per session.

Worst-case healthCheck I/O: `available × (Ping + optional 5 s Connect)`. Because the drain empties `m_available`, concurrent `Borrow` waiters sit on the CV until sessions are pushed back. That is intentional (same checkout as Borrow) but means a reconnect storm of *K* idle sessions can delay new borrows by **~K × 5 s** even though `m_mutex` is not held.

`Ping` (`mt5_pool.cpp` 95–100): `TimeServer() > 0` under the session mutex. False if `!m_manager || !m_connected`.

Proxy: `setProxyConfig` before `Initialize`. `Connect` applies `ProxySet` once (`m_proxyApplied`), then on next `SetProxy` reset. Reconnect reuses stored server/login/password; proxy is re-applied only if not already applied on that session object.

---

## 5. `MT5Watchdog` reconnect backoff (pump manager only)

### 5.1 Config (`mt5_watchdog.h`)

```cpp
struct Config {
    std::wstring server;
    uint64_t login = 0;
    std::wstring password;
    int check_interval_sec = 30;
};
```

No env keys. Interval is a constructor field (default **30**). Credentials are a copy used only for `m_mt5.Connect(...)`.

### 5.2 Loop (`mt5_watchdog.cpp` 20–72)

```
start():
  m_healthy = m_mt5.IsConnected()
  spawn watchLoop

watchLoop:
  sleep check_interval_sec (1 s slices; abort if !m_running)
  connected = m_mt5.IsConnected()      // flag, NOT Ping / TimeServer
  m_lastPingTime = time(nullptr)       // name is misleading; no ping
  if connected:
      m_healthy = true
      m_consecutiveFailures = 0
      backoffSec = 5
      continue
  // disconnected
  m_healthy = false
  ++m_consecutiveFailures
  ++m_reconnectAttempts
  sleep backoffSec (1 s slices)
  reconnected = m_mt5.Connect(server, login, password)
  if ok: reset healthy, failures=0, backoffSec=5
  else:  backoffSec = min(backoffSec * 2, 60)
```

### 5.3 Backoff table

| Consecutive failed reconnect | Pre-attempt sleep |
|---|---|
| first disconnect after healthy | **5 s** |
| after 1 failed Connect | **10 s** |
| after 2 | **20 s** |
| after 3 | **40 s** |
| after 4+ | **60 s** (cap) |

Cadence around that: **30 s** health poll, **then** the backoff sleep, **then** `Connect`. `MT5Manager::Connect` itself may block up to **30 s + 30 s** (pump attempt + no-pump fallback, `mt5_manager.cpp` 114, 122).

Reset: any `IsConnected()==true` poll **or** a successful `Connect` resets `backoffSec` to 5 and clears `m_consecutiveFailures`. `m_reconnectAttempts` is cumulative for the process lifetime (`getStatus()`).

### 5.4 What the watchdog does **not** do

- Does **not** call `MT5Pool::healthCheck()`.
- Does **not** Ping (`TimeServer`). Relies on `MT5Manager::IsConnected()` (`m_connected` atomic, set in `Connect` / `OnDisconnect`).
- Does **not** supervise individual pool sessions.
- Does **not** apply proxy itself; `MT5Manager::Connect` re-applies stored `m_proxyConfig` if needed.
- Has **no in-tree `start()`**.

`getStatus()` JSON: `connected`, `lastPing`, `reconnectAttempts`, `consecutiveFailures`, `uptimeSeconds`.

---

## 6. Timeouts — single cheat sheet

| Constant | Value | Site |
|---|---|---|
| `MT5_POOL_SIZE` / `mt5_pool_size` / `Initialize` default / `m_poolSize` | **8** | `app_config.h:25`, `.env.example:41`, `mt5_pool.h:119,161` |
| `Borrow` / `ScopedSession` default | **5000 ms** | `mt5_pool.h:124,132` |
| Init per-session `Connect` | **5000 ms** | `mt5_pool.cpp:928` |
| HealthCheck `Connect` | **5000 ms** | `mt5_pool.cpp:1072` |
| Borrow dead-session min reconnect budget | **50 ms** | `mt5_pool.cpp:1006` |
| `MT5Session::Connect` API default (unused by pool) | **30000 ms** | `mt5_pool.h:25` |
| Tick-bridge borrow | **200 ms** | `mt5_tick_bridge.cpp:316` |
| Quoted quote-hub borrow | **300 ms** | comment only (`mt5_pool.cpp:972`) |
| Watchdog check interval | **30 s** | `mt5_watchdog.h:19` |
| Watchdog backoff start / reset | **5 s** | `mt5_watchdog.cpp:21,40,65` |
| Watchdog backoff cap | **60 s** | `mt5_watchdog.cpp:70` |
| Watchdog backoff sequence | **5, 10, 20, 40, 60** | `min(*2, 60)` |
| Pump `MT5Manager::Connect` timeout | **30000 ms** (×2 on pump-fail fallback) | `mt5_manager.cpp:114,122` |
| `MT5_HTTP_POOL_SIZE` | **8** (clamp 0..64) | remote, not this pool |
| `MT5_HTTP_POOL_ACQUIRE_TIMEOUT_MS` | **100** (clamp 1..5000) | remote |
| HTTP SSE reconnect | **1000 → 30000 ms** | `mt5_http_client.cpp:787–829` |

---

## 7. Concurrency / lock rules (why the timeouts are written this way)

`m_mutex` + `m_cv` protect only the **queue** (`m_available`, `m_sessions` publish). Per-session MT5 I/O uses `MT5Session::m_mutex`.

**Must not** hold `m_mutex` across `Connect` / `Ping` / `Disconnect` (except `Shutdown`, which does). Comments in `Initialize`, `Borrow`, and `healthCheck` state that a held pool lock stalls every `Borrow` **before** its timed wait, silently exceeding 200/300 ms event-loop budgets. Under a reconnect storm that would be `K × 5 s` of event-loop stall.

`Borrow` of a dead session reconnects **off-lock** so other sessions stay concurrent; only the calling thread’s checkout is capped.

---

## 8. Findings / risks (evidence, not fixes)

1. **`MT5_POOL_SIZE` is not applied in this tree.** Loader exists; `Initialize(..., config.mt5_pool_size)` does not. Default 8 is only live if a consumer passes it.
2. **No clamp on `MT5_POOL_SIZE`.** HTTP pool is clamped; a typo (`8000`, `-1`) is passed straight into the construct loop.
3. **Watchdog ≠ pool watchdog.** Name collision: `MT5Pool::healthCheck` is commented “Watchdog” (`mt5_pool.h:153`) but is a sync method. `MT5Watchdog` is a thread over `MT5Manager` only. Neither is scheduled here.
4. **Pool has no exponential backoff.** Repeated `healthCheck` on a down broker will hammer `Connect(..., 5000)` on every idle session every time a consumer calls it.
5. **Init failures never rejoin `m_available`.** Only Borrow/healthCheck can reconnect sessions that are already queued.
6. **`Borrow` does not Ping.** Stale-but-flagged-connected sessions leak into request paths until `healthCheck` or a failed call.
7. **`lastPing` / “ping all idle sessions”** — watchdog timestamp is wall-clock of the last `IsConnected` poll, not a Manager ping.
8. **`terminal_quote_hub.cpp`** cited in `Borrow` is not in `D:\Prop`. Do not treat 300 ms as a compiled caller here.

---

## 9. Source map

| File | Role |
|---|---|
| `D:\Prop\mt5-sdk\src\core\mt5_pool.h` | `MT5Session`, `MT5Pool`, `ScopedSession`, defaults 8 / 5000 |
| `D:\Prop\mt5-sdk\src\core\mt5_pool.cpp` | Init, Borrow, Return, healthCheck, session ops |
| `D:\Prop\mt5-sdk\src\core\mt5_watchdog.h` | Pump watchdog; interval 30 s |
| `D:\Prop\mt5-sdk\src\core\mt5_watchdog.cpp` | 5–60 s exponential backoff loop |
| `D:\Prop\mt5-sdk\config\app_config.h` | `mt5_pool_size = 8` |
| `D:\Prop\mt5-sdk\config\app_config.cpp` | `MT5_POOL_SIZE` env / `.env` |
| `D:\Prop\mt5-sdk\.env.example` | Documented knob; size to broker limit |
| `D:\Prop\mt5-sdk\src\core\mt5_tick_bridge.cpp` | Only in-tree `Borrow(200)` |
| `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` | Pump connect 30000 ms; fallback no-pump |
| `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` | Achiever=8; StarwaveFX=4 (product-level) |

---

**DONE.** Read-only analysis. Single artifact: this file. Product source untouched.
