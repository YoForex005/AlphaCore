# A07 — `apps/mt5-worker` audit (Worker.cs template delay loop)

**Date:** 2026-08-18  
**Auditor:** senior engineer (read-only)  
**Scope:** `D:\Prop\apps\mt5-worker` vs architecture §§7–12 and §67 Phase 1  
**Product source modified:** none  
**Verdict:** **FAIL — Phase 1 ingestion has not started.** The C# worker is the stock `dotnet new worker` template. It does not connect to any broker, does not enumerate groups/accounts, does not backfill, does not subscribe to live events, and has no checkpoints or outbox. **0 / 7 required hosted services exist.** **0 / 8 Phase 1 deliverables are met.**

---

## 1. Measured state (honest)

| Metric | Measured value |
|---|---|
| Required hosted services present | **0 / 7** |
| Phase 1 deliverables met | **0 / 8** |
| `Worker.cs` business logic | **none** — `while` + `Task.Delay(1000)` + `LogInformation` |
| Brokers configured in worker | **0** (no Achiever, no StarwaveFX) |
| C# `IMt5BrokerConnector` | **MISSING** |
| C# EF `DbContext` / migrations | **MISSING** |
| C# `sync_checkpoints` / `outbox_events` | **MISSING** |
| Worker tests that prove ingestion | **0** (`tests/Unit` and `tests/Integration` are empty `Fact` stubs) |
| Adjacent C++ collector wired into this worker | **no** |

Do not treat a successful `dotnet build` of this project as ingestion progress. The Release output exists (`apps/mt5-worker/bin/Release/net8.0/TraderIntelligence.Mt5Worker.exe`) because the template compiles. It does not ingest.

---

## 2. Evidence — current worker is a template delay loop

### 2.1 `Worker.cs` (entire implementation)

```1:23:D:\Prop\apps\mt5-worker\Worker.cs
namespace TraderIntelligence.Mt5Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
```

This is the Microsoft Worker Service template, not a collector. The 1-second delay is not a backfill poll, not a reconnect backoff, and not an outbox tick. It is a heartbeat that proves the host process is alive.

### 2.2 `Program.cs` — single hosted service, no composition

```1:7:D:\Prop\apps\mt5-worker\Program.cs
using TraderIntelligence.Mt5Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
```

Absent (all required for Phase 1):

- options / broker registry binding
- Serilog / OpenTelemetry
- EF Core / Npgsql / Redis registration
- `IMt5BrokerConnector` (or factory) per broker
- any of the seven hosted services listed in §5
- health checks / `mt5_connected` gauge

### 2.3 Project and config

`TraderIntelligence.Mt5Worker.csproj`:

- `Microsoft.NET.Sdk.Worker`, `net8.0`, `UserSecretsId` present and unused
- only package: `Microsoft.Extensions.Hosting` 8.0.1
- project refs: Domain, Application, Infrastructure, Mt5 — all empty `Class1` stubs
- **no** native MT5 DLL copy, **no** P/Invoke, **no** HTTP client to `mt5-sdk`

`appsettings.json` and `appsettings.Development.json` contain only logging levels. No `MT5_*`, no connection strings, no broker list.

`Properties/launchSettings.json` sets `DOTNET_ENVIRONMENT=Development` only.

### 2.4 Downstream C# layers the worker already references (all empty)

| Project | Packages that hint at intent | Actual types |
|---|---|---|
| `src/Domain` | none | empty `Class1` |
| `src/Application` | FluentValidation 11.9.2 | empty `Class1` |
| `src/Infrastructure` | EF Core Design 8.0.4, Npgsql.EFCore 8.0.4, StackExchange.Redis 2.8.0 | empty `Class1` — **no DbContext, no migrations** |
| `src/Mt5` | none | empty `Class1` — **no `IMt5BrokerConnector`** |

Architecture §6 specifies a broker-agnostic connector. It exists only as a C# sketch in the markdown file, not in the product tree.

### 2.5 Twin template (not in scope, same smell)

`apps/fix-worker/Worker.cs` is the same 1-second delay template. Confirms both workers were scaffolded and left untouched.

---

## 3. Architecture contract this worker must satisfy

### 3.1 §7 Achiever + startup/resync sequence

Achiever is a **first-class source** with manager login `2027`, server `57.128.141.65:443`, default group `demo\Maxmaster` (not exclusive), pool size 8, outbound whitelist `81.29.145.69`, optional proxy. Password is secret. Proxy credentials must never be logged.

Startup/resync (this is the worker’s connect → groups → accounts → history contract):

```text
Connect
  ↓
Enumerate groups
  ↓
Upsert groups
  ↓
Enumerate accounts
  ↓
Associate accounts with broker + group
  ↓
Sync history
```

`demo\Maxmaster` must not be treated as the only group.

### 3.2 §8 StarwaveFX

Second independent source: login `9904`, server `84.201.6.142:443`, pool size 4, no whitelist today, proxy must still be designable. Same connector type as Achiever — **not** a second codebase.

### 3.3 §9 Plan-to-group mapping

Preserve `MT5_GROUP_*` mappings (`demo\yo-2step`, `contest\yo-*`, etc.) as **optional plan labels only**.

Correct: Manager API discovers **all** groups, then optional plan mapping attaches.  
Incorrect: known plan mappings decide which groups are fetched.

### 3.4 §10 Multi-broker identity

Every persisted row must carry `broker_id`. Compound keys: `broker_id + login`, `broker_id + deal_ticket`, `broker_id + order_ticket`, `broker_id + position_id`. Login/ticket uniqueness must never be assumed across Achiever and StarwaveFX.

### 3.5 §11 Raw MT5 data layer (immutable-as-practical)

Worker write-side tables:

```text
mt5_accounts
mt5_account_snapshots
mt5_orders
mt5_deals
mt5_positions_current
mt5_groups
mt5_symbol_metadata
mt5_ticks_xauusd   # only if source SDK/feed actually supports it
sync_checkpoints
ingestion_events
```

§45 also lists `brokers`, `broker_connections`, `plan_group_mappings`, `mt5_symbols`, `mt5_xau_ticks`, `outbox_events`. None exist in the C# repo. C++ `mt5_ledger_store` writes a **different** schema (`mt5_raw_events`, `mt5_deals_ledger`) and is not called from this worker.

### 3.6 §12 Ingestion pattern (the three loops this process must run)

```text
Historical Backfill
+
Live Event Subscription
+
Periodic Reconciliation
```

Backfill per broker/account: read checkpoint → fetch history → normalize → upsert idempotently → persist checkpoint.

Live: MT5 event → validate → deduplicate → persist raw → write transactional outbox → **single commit**. Then a **separate** worker processes the outbox. Do not couple Manager callbacks to ML or execution.

### 3.7 §67 Phase 1 — Reliable MT5 ingestion (acceptance)

```text
Achiever connected
StarwaveFX connected
all groups discovered
accounts synchronized
history backfilled
live deals persisted
idempotency proven
reconciliation working
```

Phase 2 reconstruction / first-3-trade counter is **out of scope** for this worker’s first useful version, but the raw deals/orders/positions it persists are the only input Phase 2 can use. Fabricating reconstructed trades in this worker would violate §11.

### 3.8 Related rules the worker must not violate

| Rule | Implication for mt5-worker |
|---|---|
| §5 Windows Worker if Manager DLL requires Windows | Host this process on Windows when using `MT5_MODE=local`. Do not force the native DLL into Linux. |
| §13 PostgreSQL outbox, no Kafka day one | Outbox table + hosted processor. Event-bus abstraction later. |
| §57 Structured logs | `correlation_id`, `broker_id`, `source_login`. Never log passwords / proxy auth. |
| §58 MT5 metrics | `mt5_connected`, `mt5_reconnects`, `mt5_events_total`, `mt5_deals_total`, `mt5_duplicate_deals_total`, `mt5_backfill_lag`, `mt5_outbox_backlog` |
| §60 Tests | Deal dedup unit tests; integration: backfill/restart + outbox processing |
| §62 MT5 unavailable | Do not invent trades. Retry. Expose stale-source. Do not enable copy from stale source. |
| §72.6–7 | Callbacks lightweight. Persist before async processing. |
| §14 | Persist Order/Deal/Position separately. Do not collapse them in the collector. |

---

## 4. Adjacent capability that is **not** this worker

`D:\Prop\mt5-sdk` is a real C++20 Manager wrapper extracted from a prop-firm backend. It is **not referenced** by `TraderIntelligence.Mt5Worker`. Treating it as “already done Phase 1” would be greenwashing.

What it actually provides:

| Capability | C++ location | Usable as-is by C# worker? |
|---|---|---|
| Transport-agnostic client | `imt5_client.h` (`GetAllGroups`, `GetGroupLogins`, `GetAccount`, `GetDeals`, `GetOrders`, `GetPositions`, `GetEventQueue`) | **No** — no C# binding |
| Local Manager + pump sinks | `mt5_manager.{h,cpp}` | Windows-only native |
| Request pool | `mt5_pool.{h,cpp}` | same |
| HTTP remote client | `mt5_http_client.{h,cpp}` | possible future transport if a sidecar exposes HTTP |
| Reconnect watchdog | `mt5_watchdog.{h,cpp}` | single `MT5Manager`, not a broker registry |
| Deal/ledger writes | `mt5_ledger_store.{h,cpp}` | different table names; optional `MT5SDK_WITH_POSTGRES` |
| Plan → group for **account creation** | `mt5_account_helper` | **wrong direction** for §9 discovery |
| Config | `app_config.{h,cpp}` | **single broker** (`MT5_SERVER` / `MT5_LOGIN`). No `MT5_STARWAVEFX_*` |

Critical SDK facts the future live-events service must design around (already documented in the C++ comments, not invented here):

1. Default pump flags are `USERS | ORDERS | POSITIONS | SYMBOLS`. There is **no `PUMP_MODE_DEALS`** in `MT5APIManager.h`. `OnDealAdd` is subscribed but is **expected to be silent**. Live deal persistence **cannot** wait for deal callbacks.
2. Connect may fall back to **no-pump** (request-only) if pump connect fails (IP not whitelisted). Live events then stop; `GetDeals` still works. Worker must surface this as stale/degraded, not pretend to be live.
3. `GetDeals` contract: follow every page/cursor for `[from,to]` or return false. False = dependency unavailable — do **not** advance the checkpoint.
4. `CacheExecutedDeal` exists because this deployment **places** trades. Trader Intelligence is a **read/collector**. That synthetic path does not populate 5,000 source accounts. Live deal capture here must be **history polling + position/order pump + reconciliation**, not SendTrade side-effects.

C++ `AppConfig` also defaults real groups to `Flexy\yo-*` in `mt5_account_helper` while architecture §9 lists `contest\yo-*`. That is a provisioning-map drift issue for later; it must **not** drive which groups this worker fetches.

---

## 5. Required hosted services (replace the template loop)

The single `Worker` class must be replaced. A 1-second `while` cannot own connection, discovery, backfill, live, checkpoints, and outbox without becoming an untestable god loop. Register **seven coordinated `IHostedService` / `BackgroundService` types** (names below are the required jobs; exact class names may vary).

Composition rule: one **broker registry**, two (later N) connector instances, shared persistence. Do not clone services per broker.

Startup order (matches §7):

```text
host start
  → Connect (both brokers)
  → Enumerate groups (all Manager-visible)
  → Accounts (all groups)
  → Backfill (from checkpoints)
  → Live events (pump + deal-lag poll)
  → Checkpoints (writer used by backfill/live/reconcile)
  → Outbox processor (independent consumer)
  → periodic reconciliation (part of §12; can live on backfill or its own timer)
```

Gate: do not start live copy / Phase 2 consumers until connect is healthy **or** until stale-source is explicitly published. Ingestion itself must keep retrying (§62).

---

### 5.1 Connect hosted service

**Purpose:** own Manager sessions for Achiever and StarwaveFX for the life of the process.

**Must do:**

- Load secret-safe config (§7, §8, §56): server, port, login, password, mode, pool size, server name, proxy, whitelist notes. Passwords and proxy auth from env/user-secrets only. Never log them.
- Construct **one** `IMt5BrokerConnector` implementation; instantiate once per `broker_id`.
- `ConnectAsync` with pump where possible; record pump vs no-pump in `broker_connections`.
- Watchdog: ping / `IsConnected`, reconnect with bounded exponential backoff, increment `mt5_reconnects`.
- Expose `mt5_connected{broker_id}` and a durable “source stale since” timestamp.
- On shutdown: cancel tokens, disconnect cleanly, do not leave Manager slots hung.
- If MT5 is down: retry forever; **do not invent deals**; do not mark backfill complete.

**Must not do:**

- Hard-code a single `MT5_SERVER` the way C++ `AppConfig` does today.
- Block the entire host forever in `ExecuteAsync` without honoring `stoppingToken`.
- Couple connect success to “we only sync `demo\Maxmaster`”.

**Current:** MISSING. Template loop does not open a socket.

**Classification:** `MISSING`

---

### 5.2 Enumerate-groups hosted service

**Purpose:** dynamic group discovery. This is the §7 / §9 correctness hinge.

**Must do:**

- After connect (and on a periodic resync, e.g. every N minutes and on reconnect): `GetGroupsAsync` / `GroupTotal` + `GetGroupDetails`.
- Upsert `mt5_groups` keyed by `(broker_id, group_name)` with currency, margin call/stop-out, company, connections-allowed.
- Write `ingestion_events` for the run (started/completed/failed).
- Optionally join `plan_group_mappings` **after** upsert. Mapping absence must not drop a group.
- Metrics: groups discovered per broker; diff vs previous snapshot.

**Must not do:**

- Filter the Manager list to `MT5_GROUP_*` env values.
- Assume `demo\Maxmaster` is sufficient.
- Use C++ `MT5AccountHelper::getMt5Group` as the discovery source (that API is for **creating** accounts).

**Current:** MISSING. C++ `mt5_group_probe` can list groups as an operator tool; it is not a hosted service and is single-broker.

**Classification:** `MISSING` (probe is `EXISTS_AND_GOOD` as a diagnostic only)

---

### 5.3 Accounts hosted service

**Purpose:** synchronize every login the Manager can see and bind it to broker + group.

**Must do:**

- For each upserted group: `GetGroupLogins` / `GetUserLogins`, then `GetUser` + `GetAccount`.
- Upsert `mt5_accounts` on `(broker_id, login)` with group, leverage, rights, registration, last access.
- Write `mt5_account_snapshots` on a slower cadence (balance/equity/margin/profit). Snapshots are time-series; do not overwrite history in place if a correction is needed — prefer a new snapshot row (§11 auditable corrections).
- Handle `UserAdd` / `UserUpdate` / `UserDelete` from the live queue as incremental upserts (same writer as the full sweep).
- ~5,000 accounts: page/batch, bound concurrency to the Manager pool (`MT5_POOL_SIZE` 8 / Starwave 4). Do not open a new Manager connection per login.

**Must not do:**

- Use login as a global primary key.
- Skip groups that are not in the plan map.
- Provision / `CreateUser` / `DealerBalance` in this worker. Those belong to the old prop-firm control plane, not Trader Intelligence Phase 1.

**Current:** MISSING.

**Classification:** `MISSING`

---

### 5.4 Backfill hosted service

**Purpose:** historical `GetDeals` / `GetOrders` / (open) `GetPositions` until each account is caught up.

**Must do (per §12):**

```text
Read checkpoint
    ↓
Fetch history
    ↓
Normalize (broker_id + tickets, source symbol as-is)
    ↓
Upsert idempotently
    ↓
Persist checkpoint
```

- Checkpoint key recommendation: `(broker_id, login, stream)` where `stream ∈ {deals, orders, positions, accounts}`.
- Windowing: use server time (`GetServerTime`), not host clock. C++ `resolveMt5TimeWindow` is the existing arithmetic; port the contract, do not invent host-local ranges.
- `GetDeals(login, from, to)` must exhaust pages. Incomplete fetch → **do not** advance checkpoint; increment a failure counter; retry.
- Overlap the previous checkpoint by a small safety window, then rely on idempotent upserts (`ON CONFLICT (broker_id, deal_ticket) DO NOTHING` or hash-compare).
- First run: from account registration (or a configured floor) to now. Subsequent: `last_success_to`.
- Persist raw `mt5_deals` / `mt5_orders` / `mt5_positions_current` **before** any reconstruction consumer sees them.
- Emit `mt5_backfill_lag` (now − max persisted deal time per broker).
- Same transaction (or strictly ordered writes) as outbox “history-synced” events if downstream needs a signal.

**Must not do:**

- Advance checkpoints on partial pages.
- Reconstruct logical trades here (§14 is Phase 2).
- Fabricate MFE/MAE or ticks (§1.5). Only store `mt5_ticks_xauusd` if the SDK subscription actually delivers them.
- Blind catch-up of **copy** intents (§63). Backfill is source-of-truth only.

**Current:** MISSING. No checkpoint table. No deal writer in C#.

**Classification:** `MISSING`

---

### 5.5 Live-events hosted service

**Purpose:** keep raw tables current after backfill without recrawling all history every second.

**Must do:**

- Drain `SubscribeAsync` / `MT5EventQueue` on a **dedicated** thread/task. Callbacks must only enqueue (§72.6). Persistence happens on this service, not on the Manager pump thread.
- Persist `Position*` → `mt5_positions_current`; `Order*` → `mt5_orders`; `User*` → accounts path; `Deal*` **if they ever fire**.
- Because **there is no `PUMP_MODE_DEALS`**, also run a **short-lag deal poll** per recently active login (and a slower sweep of all logins): `GetDeals(last_checkpoint − overlap, server_now)`. This is how “live deals persisted” is actually achieved on this SDK.
- Validate → dedup (`broker_id + deal_ticket` + optional payload hash) → persist raw → write outbox row → **commit together** (§12).
- If pump mode is off (connect fallback): set `mt5_connected` degraded, keep the deal poll, expose stale-source. Do not silently skip live persistence.
- Count `mt5_events_total`, `mt5_deals_total`, `mt5_duplicate_deals_total`.
- Do not call scoring, shadow copy, or FIX from this path.

**Must not do:**

- Assume `OnDealAdd` is a reliable live feed (C++ comments say it likely never fires).
- Use `CacheExecutedDeal` / `SendTrade` as the live path — this product is not the execution venue on the source.
- Block the pump thread with DB I/O.

**Current:** MISSING.

**Classification:** `MISSING`

---

### 5.6 Checkpoints hosted service (or shared checkpoint store used by 5.4 / 5.5)

**Purpose:** durable, per-stream resume tokens so a process crash does not re-fetch from epoch and does not skip gaps.

**Must do:**

- Table `sync_checkpoints` (§11 / §45) at minimum:

```text
broker_id
login               -- nullable for broker-wide streams (groups)
stream              -- deals | orders | positions | groups | accounts
cursor_from
cursor_to           -- last successfully committed server time / ticket
last_entity_ticket  -- optional tie-break
payload_hash        -- optional
updated_at
status              -- running | ok | failed
error
```

- **Read-modify-write** only after a successful idempotent upsert of the corresponding raw rows.
- Never persist a checkpoint in the same breath as a failed or truncated `GetDeals`.
- On restart: backfill and live poll **resume from checkpoint**, then reconcile.
- Emit `ingestion_events` for each run (id, broker, stream, counts, duration).
- Integration test required (§60): kill worker mid-backfill, restart, prove no missing tickets and no duplicate primary keys.

**May be** a small domain service consumed by backfill/live/reconcile rather than its own timer loop. It still must exist as a **first-class hosted concern** with a single writer implementation. Do not leave cursors in memory or Redis as the source of truth.

**Current:** MISSING. C++ ledger has `ingestion_run_id` on raw events but no checkpoint table and no resume API.

**Classification:** `MISSING`

---

### 5.7 Outbox hosted service

**Purpose:** transactional decoupling. Collector commits raw + outbox; a second loop publishes.

**Must do:**

- `outbox_events` (§13 / §45), same PostgreSQL transaction as the raw deal/order/position/account write:

```text
id
broker_id
aggregate_type      -- mt5_deal | mt5_order | mt5_position | mt5_account | mt5_group
aggregate_key       -- broker_id + ticket/login
event_type          -- deal_added | deal_updated | ...
payload             -- jsonb, schema-versioned
created_at
processed_at        -- null until claimed
available_at
attempt_count
last_error
```

- Processor loop: `FOR UPDATE SKIP LOCKED` batch, dispatch to in-process handlers (later: bus abstraction). At Phase 1 the only required handlers are: mark processed, optional SignalR/log, **no ML, no FIX**.
- Idempotent dispatch (handler must tolerate replay).
- Metric `mt5_outbox_backlog`.
- Bounded retry; poison rows stay unprocessed and alarm; do not block ingestion writers.
- §13 event types that this worker may emit now: raw ingestion notifications only. `score-update`, `shadow-copy`, `risk-check` wait for later phases.

**Must not do:**

- Introduce Kafka.
- Publish from the Manager callback before commit.
- Process outbox on the same call stack as `GetDeals` without a transaction boundary.

**Current:** MISSING. C++ `metrics_service.h` names (`terminal_pg_outbox_frames_total`, Redis “fast outbox”) belong to the old terminal/control-plane, not this architecture’s `outbox_events`.

**Classification:** `MISSING` (C++ metric names are `DEPRECATED` for this product)

---

### 5.8 Periodic reconciliation (required by §12, not a separate user-named service)

Implement as a timer on the backfill service or an eighth hosted service. It is **in** Phase 1 acceptance (“reconciliation working”).

- Re-pull deals/orders/positions for a sliding window; compare to persisted rows.
- Insert missing raw rows (same idempotent path + outbox).
- Corrections: do not silently overwrite; prefer a new revision / `ingestion_events` audit (§11).
- After Manager reconnect: force a reconcile window, then resume live poll.
- Prove with a test that a deal inserted only on the broker side (or fixture) appears after reconcile.

---

## 6. Phase 1 gap matrix

| Phase 1 deliverable | Required service(s) | Status |
|---|---|---|
| Achiever connected | Connect | **FAIL** — no config, no session |
| StarwaveFX connected | Connect | **FAIL** — no second broker anywhere in C#; C++ config is single-broker |
| all groups discovered | Enumerate groups | **FAIL** — not called; plan map must not substitute |
| accounts synchronized | Accounts | **FAIL** |
| history backfilled | Backfill + Checkpoints | **FAIL** |
| live deals persisted | Live events + Outbox | **FAIL** — and SDK has no deal pump |
| idempotency proven | Backfill/Live + tests | **FAIL** — no tests |
| reconciliation working | Reconcile timer | **FAIL** |

§73 classifications for this slice:

| Component | Class |
|---|---|
| `apps/mt5-worker/Worker.cs` template loop | `DEPRECATED` (delete once real services register) |
| `apps/mt5-worker/Program.cs` host | `EXISTS_NEEDS_REFACTOR` |
| C# Domain/Application/Mt5/Infrastructure | `MISSING` (stubs) |
| Architecture `IMt5BrokerConnector` | `MISSING` |
| EF schema §§11/45 | `MISSING` |
| `mt5-sdk` Manager/pool/watchdog | `EXISTS_AND_GOOD` as **transport**, not as this worker |
| `mt5-sdk` AppConfig (one broker) | `EXISTS_NEEDS_REFACTOR` if reused |
| `mt5_account_helper` as discovery | `UNSAFE` for §9 if used to filter groups |
| C++ ledger `mt5_raw_events` / `mt5_deals_ledger` | `EXISTS_NEEDS_REFACTOR` — different names/keys than §11; no `broker_id` compound identity as specified |
| Worker 1s info log | `DEPRECATED` / noisy; violates §57 identifier contract |

---

## 7. Risks if someone “just fills in Worker.cs”

1. **God loop.** Putting connect + 5,000-account backfill + live pump + outbox in one `ExecuteAsync` will either block reconnects or drop events. Split the seven services.
2. **Silent live-deal hole.** Designing only around `OnDealAdd` will store zero live deals. Must poll `GetDeals` with checkpoints.
3. **Plan-map filter.** Reusing `MT5_GROUP_*` / `getMt5Group` as the account universe violates §9 and will miss `demo\Maxmaster` and every non-yo group.
4. **Identity clash.** Upserting Achiever login `2027`-side tickets into the same unique key as StarwaveFX will corrupt the raw layer.
5. **Checkpoint on partial history.** Advancing `cursor_to` when `GetDeals` returns false or a short page creates permanent gaps. Prefer false negatives.
6. **Native DLL / OS.** Local Manager is Windows x64. A Linux worker container will not satisfy Phase 1 unless `MT5_MODE=remote` and a Windows sidecar exists. That sidecar is not in `apps/mt5-worker` today.
7. **Secrets.** UserSecretsId is unused. A future commit of `appsettings` with `MT5_PASSWORD` would be a security defect (§55/§72).
8. **Provisioning APIs.** `CreateUser`, `DealerBalance`, `SendTrade` must stay out of this worker. They are write-side prop-firm tools; enabling them here is an unsafe scope expansion.
9. **Tick table.** Do not create fake XAU ticks to satisfy the table list.
10. **False PASS.** A green build or a log line `Worker running at:` is not “Achiever connected”.

---

## 8. Implementation sequence (audit only — not started)

Do **not** implement in this audit. When implementation is authorized, the order that matches §§7–12 / Phase 1 is:

1. Domain contracts: `BrokerId`, `IMt5BrokerConnector`, raw DTOs, checkpoint and outbox records.
2. Infrastructure: EF mappings for §11 + `brokers` / `broker_connections` / `outbox_events` / `sync_checkpoints`; migrations; secret-safe options for two brokers.
3. `src/Mt5`: one connector wrapping native or HTTP transport; **no** business persistence inside the SDK callback.
4. Replace `AddHostedService<Worker>()` with the seven services in §5; delete the delay loop.
5. Unit tests: deal dedup, checkpoint-not-advanced-on-failure, outbox same-transaction invariant (in-memory fakes).
6. Integration tests: migrate Postgres → backfill fixture → restart → no dupes / no gaps; outbox processor drain.
7. Only then: live Achiever, then StarwaveFX, with metrics and stale-source status.

Phase 2 (`ReconstructedTrade`, first-3-trade counter) must not start until Phase 1 items in §6 are **measured** true.

---

## 9. File inventory (this audit)

Read, not modified:

- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\TraderIntelligence.Mt5Worker.csproj`
- `D:\Prop\apps\mt5-worker\appsettings.json`
- `D:\Prop\apps\mt5-worker\appsettings.Development.json`
- `D:\Prop\apps\mt5-worker\Properties\launchSettings.json`
- `D:\Prop\src\Domain\Class1.cs`, `src\Application\Class1.cs`, `src\Infrastructure\Class1.cs`, `src\Mt5\Class1.cs`
- `D:\Prop\mt5-sdk\src\core\imt5_client.h`, `mt5_manager.h`, `mt5_manager.cpp`, `mt5_types.h`, `mt5_watchdog.cpp`
- `D:\Prop\mt5-sdk\src\services\mt5_ledger_store.{h,cpp}`, `mt5_account_helper.h`, `mt5_time_window.h`
- `D:\Prop\mt5-sdk\config\app_config.{h,cpp}`
- `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§4–14, 45, 56–62, 66–67, 72–73

Written:

- `D:\Prop\reports\swarm\20260818\A07_mt5_worker_audit.md` (this file)

---

## 10. Bottom line

`apps/mt5-worker` is a **host process with no collector**. Architecture §§7–12 define a multi-broker, checkpointed, outbox-backed ingestion pipeline. §67 Phase 1 is that pipeline working on Achiever **and** StarwaveFX. None of it is present in the C# worker.

**Required hosted services to replace `Task.Delay(1000)`:**

1. **Connect** — both brokers, pump/no-pump awareness, watchdog, stale-source.
2. **Enumerate groups** — all Manager groups, upsert, plan map is optional labeling only.
3. **Accounts** — every login, `(broker_id, login)`, snapshots, no provisioning.
4. **Backfill** — paged history, idempotent raw upserts, server-time windows.
5. **Live events** — lightweight queue drain **plus** deal-lag poll (no `PUMP_MODE_DEALS`).
6. **Checkpoints** — durable per-broker/login/stream cursors; never advance on partial fetch.
7. **Outbox** — same transaction as raw persist; independent processor; Postgres only.

Until those seven exist and the §6 matrix is re-measured, Phase 1 is **not done**.
