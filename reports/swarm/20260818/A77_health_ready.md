# A77 — `/health` and `/ready` for API, MT5 worker, FIX worker

| Field | Value |
|---|---|
| Agent | A77 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A77_health_ready.md` |
| Product source edited | **No** |
| Status | Binding implementation spec (pre-code) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§5, 28, 40–43, 46–47, 52, 55, 57–59, 62, 67–71 |
| Binding siblings | A06, A07, A08, A25, A26, A41, A46, A48, A49, A50, A51, A53, A54, A65 |
| Binding task law | **Ready requires DB. FIX ready does not require REAL execution.** |

This file owns the **orchestrator probe contract** for the three product processes. It does not implement. It does not replace the authenticated dashboard inventory (`GET /api/v1/system/health`).

---

## 0. Verdict

Today **no process exposes `/health` or `/ready`.**

| Process | Path | Measured state |
|---|---|---|
| `apps/api` | `/health`, `/ready` | **MISSING.** Only `GET /weatherforecast`. |
| `apps/mt5-worker` | `/health`, `/ready` | **MISSING.** Worker SDK host; no Kestrel. `Worker.cs` is a 1 s log loop. |
| `apps/fix-worker` | `/health`, `/ready` | **MISSING.** Same template. No flags, no FIX, no DB. |

Earlier swarm notes used four different probe names (`/health`, `/health/ready`, `/health/live`, `/api/v1/health/live`). **This document freezes two paths** and supersedes those names for orchestrators.

### Binding law (do not weaken)

```text
GET /health   → liveness. Process can answer. No dependency I/O.
GET /ready    → readiness. Hard-requires PostgreSQL. 200 or 503.

REAL_COPY_EXECUTION_ENABLED MUST NOT gate FIX /ready.
READY_FOR_EXECUTION MUST NOT gate FIX /ready.
TRADE / QUOTE Logon MUST NOT gate FIX /ready.
Venue sockets MUST NOT gate any process /ready.
Worker heartbeats MUST NOT gate API /ready.
```

Reason the FIX rule exists: architecture §41 default is `REAL_COPY_EXECUTION_ENABLED=false`. That is the **production success state** for Phases 4–7 and for first useful version (§69). If `/ready` required real execution, compose / systemd / any future orchestrator would **never mark `fix-worker` ready** and would restart it forever. That is a P0 design defect, not a conservative safety gate.

Real-send safety lives in the **send function** (A25 §6.3, A49 §3.2), not in the probe.

---

## 1. What this is / is not

### 1.1 This spec owns

- Path names, HTTP codes, JSON shapes for **process probes**.
- The **gating matrix** (what may flip `/ready` to 503).
- How workers grow a private HTTP listener without becoming a second dashboard.
- How probes interact with §62 (stay-alive on venue/DB failure) and §41 (execution flag).
- Compose / systemd probe commands (A65 replacement).

### 1.2 This spec does **not** own

| Concern | Owner |
|---|---|
| Authenticated System Health page | A06 `/api/v1/system/health`, A26 §6.14, React `/health` |
| Overview health strip | A26 `GET /api/v1/overview` |
| FIX QUOTE/TRADE cards | A26 `GET /api/v1/fix/sessions` |
| §58 Prometheus names | A50 |
| TRADE lease / fencing | A46 |
| Kill switch | A48 |
| Feature-flag send gate | A49 |
| C++ sidecar `GET /mt5/health` | A16 (Manager connected?). **Different process, different path.** |

### 1.3 Non-goals

- Kubernetes / Helm (architecture §71: do not build k8s yet). The same two paths work for compose, systemd `ExecStartPost`, and a later k8s `httpGet` without rename.
- A public internet health page.
- Scraping Manager / cServer from the API to “confirm” ready.
- Using `/ready` as a license to send `NewOrderSingle`.

---

## 2. Path unification (supersedes earlier names)

Canonical probe paths, **identical on all three processes**:

| Method | Path | Auth | Role |
|---|---|---|---|
| `GET` | `/health` | anonymous | Liveness |
| `GET` | `/ready` | anonymous | Readiness |

**Not** under `/api/v1`. Probes must remain reachable when JWT/IdP is down, and must never require a dashboard role (A51).

### 2.1 Retired names (do not implement as the primary probe)

| Earlier name | Source | Disposition |
|---|---|---|
| `GET /health/ready` | A06, A51, A54 | **Alias only** if a later coding task wants one 301/308 or a duplicate map. Prefer **not** to add it. New code uses `/ready`. |
| `GET /health/live` | — | Do not add. `/health` is liveness. |
| `GET /api/v1/health/live` | A26, A65 | **Do not implement.** Compose (A65) must switch to `GET /health`. |
| `GET /api/v1/health` | A26 §6.14 | **Keep as authenticated dashboard inventory** *or* fold into `GET /api/v1/system/health` (A06). **Not a probe.** |
| `GET /api/health` | `apps/web` hook today | Dashboard client debt. React must not poll the orchestrator probe for the System Health page. |
| `GET /health/mt5` | A54 §6.3 | Optional **private diagnostic** on `mt5-worker` only. Not `/ready`. |
| `GET /health/fix` | — | Optional private diagnostic on `fix-worker`. Not `/ready`. |
| `GET /weatherforecast` | template | **Delete** when probes land (A06, A30, A55). Must not remain as the compose probe. |

### 2.2 Dashboard vs probe (do not collapse)

```text
Orchestrator / compose / systemd
    GET /health          process alive?
    GET /ready           this process can use Postgres?

Human / React System Health
    GET /api/v1/system/health     (ReadOnly+, A06)
    GET /api/v1/overview          health strip
    GET /api/v1/fix/sessions      QUOTE / TRADE cards
    GET /api/v1/brokers           source connection
```

`/ready` answers **“should this instance receive work / stay in the replica set?”**  
Dashboard answers **“is the platform operationally healthy?”**

Those are different questions. A FIX worker that is logged off QUOTE, `REAL_COPY=false`, and `READY_FOR_EXECUTION=false` is **probe-ready** if Postgres answers, and **dashboard-degraded**. That is correct.

---

## 3. Shared semantics

### 3.1 `/health` — liveness

**Meaning:** the process is running, the HTTP stack can accept a connection, and the request thread is not wedged.

**Must**

- Return **200** with a tiny JSON body as soon as the listener is bound.
- Do **zero** dependency I/O: no Postgres, no Redis, no MT5, no FIX, no disk lease, no DNS to cServer.
- Stay 200 during a Postgres outage (A53 §4.5: process stay-alive).
- Stay 200 when Achiever / Starwave / QUOTE / TRADE are down (§62: retry, do not invent, expose stale).
- Stay 200 when `REAL_COPY_EXECUTION_ENABLED=false`.
- Finish in **≤ 50 ms** p99 on an idle box. No `async` DB.

**Must not**

- Restart-loop the process because a broker is down.
- Dump options, connection strings, or flags that include secrets.
- Redirect (no HTTPS 30x — A65 compose footgun).

**During shutdown:** once `ApplicationStopping` fires, `/health` **may** stay 200 until the listener closes (liveness “still here”). `/ready` must already be 503 (see §3.3).

**If `/health` fails:** the process is dead or the listener is stuck. Restart is appropriate.

### 3.2 `/ready` — readiness

**Meaning:** this process has finished startup **and can talk to PostgreSQL**, which is the authority for every durable write (§5, §13, §62 “Database unavailable”).

**Hard gate (all three processes)**

```text
postgres: SELECT 1  (or equivalent CanConnect + trivial command)
          completes successfully inside the probe timeout
```

No connection string → **not ready** (the process cannot meet “Ready requires DB”).

Empty / unreachable / auth-fail / TLS-fail / too-slow Postgres → **503**.

**Must not gate `/ready` (all three)**

| Check | Why it is reported, not gating |
|---|---|
| Redis PING | Cache / lease liveness (A46). Restarting on Redis blip flaps FIX sockets. A54’s “API ready = Postgres + Redis” is **superseded** for probes. Dashboard still shows Redis. |
| MT5 Manager connected | §62: retry; expose stale-source. Restarting the collector on a broker outage **loses pump state** and creates a reconnect storm. |
| FIX QUOTE logged on | Quote age is a **risk** input, not a process-ready bit. |
| FIX TRADE logged on | Phase 4 may run with TRADE off. |
| `READY_FOR_EXECUTION` | §42 recon state. Blocks **sends**, not process membership. |
| `REAL_COPY_EXECUTION_ENABLED` | **This file’s hard law.** Default `false`. Must not 503. |
| `CTRADER_FIX_*` session flags off | Worker still does health / lease housekeeping (A49). |
| TRADE / QUOTE lease held | Loser must stay up as standby. Failing `/ready` on “not owner” → restart → lease flap → duplicate reports (A25/A46 P0). |
| `STOP_NEW_EXECUTION` | Operational latch (A48). Process stays ready. |
| Outbox backlog / poison | A41 dashboard signal. Do not restart the writer. |
| Worker heartbeats (API) | Otherwise deploying `mt5-worker` takes the API out of rotation and the dashboard cannot show “worker down”. |
| Quote age / XAU mapped | Risk / Phase 4 dashboard. |
| Migrations not applied (optional extra) | See §3.4. |

**HTTP**

| Condition | Code | `status` |
|---|---|---|
| All gating checks pass | **200** | `ready` |
| Any gating check fail or timeout | **503** | `not_ready` |
| Process is stopping | **503** | `not_ready` |
| Wrong method | **405** | — |
| Probe path on a process that has not bound HTTP yet | connection fail | orchestrator treats as down |

No 401/403 on these two paths. No cookies required.

### 3.3 Drain

```text
ApplicationStarted  → /health 200, /ready follows DB
ApplicationStopping → /ready 503 immediately (stop new work / stop receiving)
                    → in-flight requests finish
                    → /health stays 200 until listener close
                    → process exit
```

FIX worker on stopping: Logout + release lease (A25 §4.3) **after** `/ready` is already 503, so a peer can acquire without this instance being considered ready.

### 3.4 Schema-applied (optional second DB gate)

Once migrations exist (A20 / A30 I1):

- Prefer the postgres check to be `SELECT 1` **plus** `SELECT 1 FROM "__EFMigrationsHistory" LIMIT 1` (or `SELECT 1 FROM brokers LIMIT 0`).
- Missing catalog → **not ready**. An empty-but-migrated database **is** ready (no required seed rows).
- **Do not** run migrations from the probe.
- **Do not** `SELECT COUNT(*)` on `mt5_deals`.

Until the first migration is in the tree, `SELECT 1` alone is the gate.

### 3.5 Timeouts and cache

| Knob | Default | Notes |
|---|---|---|
| `/health` budget | 50 ms | Sync, no I/O |
| `/ready` overall | 2.0 s | Hard cancel |
| Postgres command timeout | 1.0 s | Separate from Npgsql default |
| Redis (non-gating) budget | 200 ms | If it times out, report `fail`, still 200 if DB passed |
| Result cache | **1 s** for `/ready` only | Prevents probe stampede. `/health` never cached. |
| Cache key | per process, not per caller | |

On cache hit, return the previous status **and** `checkedAt` from the cached sample. Do not serve a cached **200** older than 1 s.

### 3.6 HTTPS / HTTP

A65: `UseHttpsRedirection()` + compose HTTP healthcheck = 30x loop.

Binding:

- `/health` and `/ready` are **HTTP**. They must **not** 301/302 to HTTPS.
- Register them **before** HTTPS redirection, or exclude these two paths.
- Production TLS terminates at nginx (A54). Upstream probes hit `http://127.0.0.1:<port>/health`.

---

## 4. Shared JSON contract

`Content-Type: application/json; charset=utf-8`. UTC timestamps with `Z`. camelCase. Allow-list only (A26 §3). **Never** connection strings, passwords, proxy credentials, FIX tag 554, manager passwords, lease tokens, or raw `IConfiguration`.

### 4.1 `GET /health`

```json
{
  "status": "ok",
  "service": "api",
  "instanceId": "api-01",
  "utc": "2026-08-18T12:00:00.000Z"
}
```

| Field | Values |
|---|---|
| `status` | `ok` (only; if you cannot write this, you cannot answer) |
| `service` | `api` \| `mt5-worker` \| `fix-worker` |
| `instanceId` | hostname + short boot uuid (same id the lease row uses). Not a secret. |
| `utc` | clock now |

No `checks` object. No flags. No metrics dump (A50: probes are not `/metrics`).

### 4.2 `GET /ready`

```json
{
  "status": "ready",
  "service": "fix-worker",
  "instanceId": "fix-worker-01",
  "utc": "2026-08-18T12:00:00.000Z",
  "checkedAt": "2026-08-18T12:00:00.000Z",
  "checks": {
    "process": { "status": "pass", "detail": "started" },
    "postgres": { "status": "pass", "latencyMs": 4 }
  },
  "info": {
    "realCopyExecutionEnabled": false
  }
}
```

Not-ready example (DB down; process stays up — `/health` would still be 200):

```json
{
  "status": "not_ready",
  "service": "fix-worker",
  "instanceId": "fix-worker-01",
  "utc": "2026-08-18T12:00:01.000Z",
  "checkedAt": "2026-08-18T12:00:01.000Z",
  "checks": {
    "process": { "status": "pass", "detail": "started" },
    "postgres": { "status": "fail", "latencyMs": 1000, "code": "DATABASE_UNAVAILABLE" }
  },
  "info": {
    "realCopyExecutionEnabled": false
  }
}
```

| Field | Rule |
|---|---|
| `status` | `ready` iff **every gating** check is `pass`. Else `not_ready`. |
| `checks.*.status` | `pass` \| `fail` \| `skip` |
| `checks.*.code` | Stable reason token when not pass (`DATABASE_UNAVAILABLE`, `PROCESS_STARTING`, `PROCESS_STOPPING`). |
| `checks.*.detail` | Short, non-secret. **Never** the Npgsql exception which embeds the host/user. Map to a class. |
| `info` | **Non-gating** snapshot. Presence of a false flag must not change HTTP status. |

`info` is how operators see `REAL_COPY=false` **on a 200**. That is the intended picture.

Forbidden keys anywhere in these two bodies (fail closed / omit — A26 sanitizer):

```text
password, passwd, secret, pwd, rawData, connectionString,
privateKey, proxyUser, proxyPassword, CTRADER_FIX_PASSWORD,
MT5_PASSWORD, fencingToken, confirmToken
```

---

## 5. Gating matrix (normative)

Legend: **GATE** = may force `/ready` 503. **INFO** = may appear under `info` / extra non-gating `checks`. **ABSENT** = do not even report on the probe (dashboard only).

| Check | API `/ready` | MT5 `/ready` | FIX `/ready` |
|---|---|---|---|
| Process started / not stopping | **GATE** | **GATE** | **GATE** |
| Postgres `SELECT 1` | **GATE** | **GATE** | **GATE** |
| Connection string present | **GATE** (via postgres fail) | **GATE** | **GATE** |
| Schema / migrations table exists | **GATE** once migrations ship | **GATE** once migrations ship | **GATE** once migrations ship |
| Redis | INFO | INFO (optional) | INFO (optional) |
| `REAL_COPY_EXECUTION_ENABLED` | INFO | ABSENT (mt5 never sends) | **INFO — never GATE** |
| `CTRADER_FIX_ENABLED` / QUOTE / TRADE flags | INFO (settings mirror) | ABSENT | INFO |
| QUOTE connected / logged on | ABSENT | ABSENT | INFO |
| TRADE connected / logged on | ABSENT | ABSENT | INFO |
| `READY_FOR_EXECUTION` | ABSENT | ABSENT | INFO |
| Lease held / fencing token | ABSENT | ABSENT | INFO (boolean `leaseHeld` only; **no token**) |
| Achiever connected | ABSENT | INFO | ABSENT |
| StarwaveFX connected | ABSENT | INFO | ABSENT |
| Pump vs no-pump / stale-source | ABSENT | INFO | ABSENT |
| Outbox backlog | ABSENT | INFO | INFO |
| `STOP_NEW_EXECUTION` | ABSENT | ABSENT | INFO |
| Worker heartbeat age | ABSENT (dashboard) | — | — |
| XAU instrument mapped | ABSENT | ABSENT | INFO |
| Quote age | ABSENT | ABSENT | ABSENT (dashboard / risk) |

**Invariant:** `status` is computed **only** from GATE rows. Adding an INFO check later must not change HTTP codes. Tests in §12 lock this.

---

## 6. API (`apps/api`)

### 6.1 Role

Linux BFF (A54). Serves React. Reads/writes Postgres. Must not load `TraderIntelligence.Mt5`. Must not open Manager or FIX sockets.

### 6.2 Listener

Existing Kestrel (`http://localhost:5160` lab, `http://+:8080` compose). Map the two paths on the **same** public port. Do not add a second listen just for probes.

Anonymous. Rate-limit lightly if needed; do not put JWT middleware in front.

### 6.3 `/health`

Process + Kestrel. No `AddDbContext` ping.

### 6.4 `/ready` GATE

1. `ApplicationStarted` and not `ApplicationStopping`.
2. Postgres: open the API role connection (`ti_api` when roles exist — A54 §6.1) and `SELECT 1`.

**Not GATE:** Redis, SignalR hub started, JWT signing key present (if missing, authenticated routes 500; probes still 200 so ops can see the process), worker heartbeats, `REAL_COPY`.

If the JWT signing key is missing, that is a **dashboard** defect, not a reason to flap the replica. Optionally report `info.authConfigured: false`.

### 6.5 `info` (API)

```json
{
  "realCopyExecutionEnabled": false,
  "redis": { "status": "pass", "latencyMs": 1 },
  "authConfigured": true
}
```

`realCopyExecutionEnabled` is the **effective** floor (config AND settings store — A49 §3.4). Always safe to show. Never a secret.

### 6.6 Why API `/ready` does not include workers

A06 suggested “Postgres + Redis + worker heartbeats.” That is the **dashboard** aggregator.

If API `/ready` required `mt5-worker` heartbeat:

- Taking the Windows collector down for a DLL upgrade would 503 the Linux API.
- React could not load Overview to display “ingestion stale.”
- A broker outage that delayed heartbeats would restart **API** pods, which cannot fix MT5.

API remains ready so it can tell the truth about everyone else via `/api/v1/system/health`.

### 6.7 Composition notes (later coding — not this task)

```text
apps/api/Program.cs
  UseSerilog + redaction FIRST (A50)
  AddDbContext<TraderDbContext>
  AddHealthChecks()
    .AddCheck<ProcessStartedCheck>("process", tags: ["ready"])
    .AddCheck<PostgresReadyCheck>("postgres", tags: ["ready"])
  MapGet("/health", ...)   // no tags that hit DB
  MapGet("/ready", ...)    // run ready-tagged checks only
  exclude these two from UseHttpsRedirection
  delete WeatherForecast
```

Do not use the default HealthChecks JSON as the public contract. Map through the DTO in §4 so workers and API match.

Packages when implementing: `AspNetCore.HealthChecks.NpgSql` **or** a 15-line `NpgsqlConnection` check in Infrastructure. Either is fine. Do not add HealthChecks UI.

---

## 7. MT5 worker (`apps/mt5-worker`)

### 7.1 Role

Windows collector (A07, A54). Writes raw MT5 + checkpoints + outbox to Postgres. **Never** a FIX client (A49 §4).

Today: `Microsoft.NET.Sdk.Worker`, no HTTP, no DbContext registration.

### 7.2 Listener

Add a **private** Kestrel (or `IHostedService` HTTP) dedicated to probes.

| Item | Value |
|---|---|
| Port | **9101** (A54 optional `:9101`) |
| Bind | `127.0.0.1` on a workstation; VPC / private NIC in production. **Not** `0.0.0.0` public. **Not** published to the internet. |
| Env | `MT5_WORKER_PROBE_URL=http://127.0.0.1:9101` |
| Paths | `/health`, `/ready` (same contract) |
| Auth | none on loopback; if bound off-box, require a **network ACL**, not a browser cookie. Optional shared `X-Probe-Key` later — not v1. |

Linux API must **not** be required to scrape this port for `/ready`. Durable source status is `broker_connections` in Postgres (A54 §6.3). API dashboard may optionally scrape 9101 if Postgres is briefly stale; that is **not** this probe’s gating story.

Do **not** put Manager routes (`/mt5/groups`, `/mt5/health` in the A16 sidecar sense) on this listener. That is Mode B (A54) and a different service.

### 7.3 `/health`

Process alive. The current 1 s `LogInformation` loop is **not** a health implementation; it is noise (A07). When probes exist, delete that heartbeat log.

### 7.4 `/ready` GATE

1. Host started; connect / ingest hosted services **registered** (not “both brokers logged in”).
2. Postgres: ingest role (`ti_mt5_ingest`) `SELECT 1`.

**Not GATE:** Achiever socket, StarwaveFX socket, pump vs no-pump, whitelist 1012, backfill complete, outbox drain.

A worker that is retrying Manager connect while writing nothing is still **ready** if it can persist the moment a broker answers. Restarting it because Achiever is down **violates §62** (“Continue retrying”).

### 7.5 `info` (MT5)

```json
{
  "brokers": [
    { "brokerCode": "ACHIEVER", "connected": false, "staleSince": "2026-08-18T11:55:00.000Z" },
    { "brokerCode": "STARWAVEFX", "connected": false, "staleSince": null }
  ],
  "outboxBacklog": 0
}
```

`connected: false` + HTTP 200 is **legal and expected** during a source outage. Dashboard paints STALE from `broker_connections`, not from this probe’s status code.

**Never** include manager login, password, proxy user/password, server whitelist notes that embed credentials.

Do not put `realCopyExecutionEnabled` on this process. The collector does not read that flag as a send license (A49 §4). Omitting it prevents a future “the MT5 box is not ready for live copy” misread.

### 7.6 Interaction with C++ `GET /mt5/health`

A16’s HTTP client probes a **sidecar** `GET /mt5/health` meaning “Manager session connected.” That is **venue** liveness for `MT5_MODE=remote`.

| Path | Process | Means |
|---|---|---|
| `GET /health` | `apps/mt5-worker` | C# process up |
| `GET /ready` | `apps/mt5-worker` | C# process + Postgres |
| `GET /mt5/health` | optional C++ sidecar | Manager connected (A16) |

Do not alias them. A compose healthcheck on the C# worker must hit **9101 `/ready`**, not `/mt5/health`.

---

## 8. FIX worker (`apps/fix-worker`)

### 8.1 Role

Linux QuickFIX/n host (A08, A25). Only process allowed to own QUOTE/TRADE sockets and the send gate. Default §41:

```env
CTRADER_FIX_ENABLED=true
CTRADER_FIX_QUOTE_ENABLED=true
CTRADER_FIX_TRADE_SESSION_ENABLED=true
REAL_COPY_EXECUTION_ENABLED=false
```

`/ready` must succeed in that default.

### 8.2 Listener

| Item | Value |
|---|---|
| Port | **9102** |
| Bind | `127.0.0.1` / private NIC. Not public. |
| Env | `FIX_WORKER_PROBE_URL=http://127.0.0.1:9102` |
| Paths | `/health`, `/ready` |

Do not expose QuickFIX admin, sequence files, or a “send test order” route on this port.

### 8.3 `/health`

Process + probe listener. No Logon. No `SELECT 1`. No Redis. No lease renew.

A53 §4.5: on DB outage the process **stays alive** and FIX sockets **may** stay logged on so they do not flap. Therefore `/health` stays **200** while `/ready` is **503**. That pair is how ops distinguish “dead process” from “venue up, ledger down.”

### 8.4 `/ready` GATE

1. Host started; not stopping.
2. Postgres `SELECT 1` (FIX worker uses a dedicated role when roles exist; until then the same app string as other Linux services).

That is the **entire** gate.

### 8.5 `/ready` MUST NOT require REAL execution

This is the title constraint. Encode it as tests, not comments.

`REAL_COPY_EXECUTION_ENABLED` is a **send license**, necessary but not sufficient for `35=D` (A25 §6.3, A49 §3.2). It is **not** a process-readiness license.

| Situation | `/health` | `/ready` if DB up | May send 35=D? |
|---|---|---|---|
| Default flags (`REAL_COPY=false`) | 200 | **200** | **no** |
| `REAL_COPY=true` but TRADE not reconciled | 200 | **200** | **no** (`READY_FOR_EXECUTION` missing) |
| `REAL_COPY=true`, TRADE ready, lease owned, risk ok | 200 | **200** | yes (send gate) |
| `REAL_COPY=false`, TRADE logged on, recon clean | 200 | **200** | **no** |
| `CTRADER_FIX_ENABLED=false` (housekeeping only) | 200 | **200** | **no** |
| `CTRADER_FIX_TRADE_SESSION_ENABLED=false` | 200 | **200** | **no** |
| `CTRADER_FIX_DIAGNOSTIC_LOGON_ONLY=true` | 200 | **200** | **no** |
| QUOTE down / stale quotes | 200 | **200** | no for priced OPEN (risk) |
| TRADE TCP down | 200 | **200** | no |
| Not lease owner | 200 | **200** | no |
| `STOP_NEW_EXECUTION` on | 200 | **200** | no for new copy |
| Unresolved `EXECUTION_STATE_UNKNOWN` | 200 | **200** | no |
| Postgres down | 200 | **503** | **no** (§62 fail closed; cannot persist-before-send) |
| Postgres down, sockets still logged on | 200 | **503** | **no** (A53 §4.5) |
| Missing `ConnectionStrings:Postgres` | 200 | **503** | no |
| Process stopping | 200 then exit | **503** | no |

If a future engineer adds `if (!flags.RealCopyExecutionEnabled) return 503;` that is a **spec violation**. Reviewers reject it even if tests were “updated to match.”

### 8.6 `info` (FIX) — flags visible, never gating

```json
{
  "realCopyExecutionEnabled": false,
  "ctraderFixEnabled": true,
  "quoteEnabled": true,
  "tradeSessionEnabled": true,
  "diagnosticLogonOnly": false,
  "quote": { "loggedOn": false, "sessionStatus": "DISCONNECTED" },
  "trade": { "loggedOn": false, "sessionStatus": "DISCONNECTED", "readyForExecution": false },
  "leaseHeld": false,
  "stopNewExecution": false
}
```

Rules:

- Booleans and session enums only. **No** password, RawData, SenderSubID-if-secret, fencing token, ClOrdID lists.
- `realCopyExecutionEnabled: false` on a **200** is the documented happy path for Phases 4–7.
- `readyForExecution: false` on a **200** is the documented happy path until §42 passes.
- `leaseHeld: false` on a **200** is required so a standby replica is not killed.

### 8.7 Why venue / lease / recon must not gate

| If `/ready` required… | Failure mode |
|---|---|
| `REAL_COPY=true` | Default production never becomes ready. Compose `depends_on: service_healthy` loops. First useful version (§69) cannot include a FIX worker. |
| TRADE `READY_FOR_EXECUTION` | Every recon mismatch restarts the worker, which **drops the socket**, which **resets seq** (RoE `141=Y`), which **restarts recon**, which never converges. |
| QUOTE logged on | Quote gap (normal) restarts the worker and also tears TRADE if they share a process — worse than A25’s “QUOTE fail must not tear TRADE TCP.” |
| Lease held | Standby replica 503 → orchestrator restart → both sides fight the lease → **duplicate ExecutionReports** (A25 §4, official FAQ). |
| Redis PING | Redis blip restarts FIX, flaps Logon, loses in-flight unknown-state context. A46 already says: Redis down ⇒ do not send; it does **not** say restart. |
| Password configured | Tempting, but a missing secret is an INFO `secretConfigured: false`. Restarting will not create a Vault entry. |

Send-path fail-closed (A49) already covers every row above. Probes must not duplicate that as process death.

### 8.8 DB down + sockets up (A53 alignment)

```text
Postgres timeout
    → /ready 503  (GATE)
    → /health 200 (stay-alive)
    → do not Logout just because the probe failed
    → do not send 35=D / F / G
    → do not enqueue OPEN/INCREASE in memory
    → on DB return: TRADE re-reconcile before READY_FOR_EXECUTION
```

`/ready` 503 here is **load-balancer drain / “do not route new work to a box that cannot persist.”** It is not “kill the FIX session.”

systemd / compose: use `/ready` for **readiness**, `/health` for **liveness**. If an operator mistakenly points **liveness** at `/ready`, a DB blip **restarts** FIX and violates A53. Document that in the unit file (see §10).

---

## 9. What “Ready requires DB” means in code

Single shared check in Infrastructure (suggested; not created by this task):

```text
src/Infrastructure/Health/PostgresReadyCheck.cs
src/Infrastructure/Health/ProcessLifetimeReadyCheck.cs
src/Infrastructure/Health/ProbeResponseWriter.cs
src/Application/Health/ProbeDtos.cs          # allow-list records
```

All three hosts call the same `PostgresReadyCheck` against **their** connection string.

Implementation rules:

1. Use a **dedicated** short-lived command (`SELECT 1`). Do not borrow a scoped `TraderDbContext` that might be mid-transaction on the request thread (API). A separate `NpgsqlDataSource` / `IDbContextFactory` is fine.
2. Command timeout 1 s. On cancel, `fail` + `DATABASE_UNAVAILABLE`.
3. Swallow and classify exceptions. Log at Warning with **redacted** message (A50). Do not put `ex.ToString()` in the HTTP body.
4. Do not run this check on `/health`.
5. Do not treat Redis as a substitute if Postgres fails (A03 / A46: Redis is not the book).
6. Fail closed if the connection string is null/empty **before** attempting a connect (avoids a 15 s DNS hang to `localhost`).

Workers today do not register EF. When they do, the probe is the first consumer — bind options **after** Serilog redaction (A50 §3.2).

---

## 10. Orchestrator wiring

Architecture §71: no Kubernetes required. Still specify probes so compose/systemd do not invent a third path.

### 10.1 Docker Compose (supersedes A65 temporary `/weatherforecast`)

| Service | Liveness | Readiness |
|---|---|---|
| `postgres` | `pg_isready` (unchanged) | same |
| `redis` | `redis-cli ping` | same |
| `api` | `curl -fsS http://127.0.0.1:8080/health` | `curl -fsS http://127.0.0.1:8080/ready` |
| `mt5-worker` | not in Linux compose (Windows) | Windows service, §10.2 |
| `fix-worker` | `curl -fsS http://127.0.0.1:9102/health` | `curl -fsS http://127.0.0.1:9102/ready` |

Compose `healthcheck.test` for `api` and `fix-worker` should use **`/ready`** (include DB) **or** split into a readiness-only depends. Official Compose v2 has a single `healthcheck`; use `/ready` there **only if** liveness is not also that check.

**Preferred:** compose `healthcheck` = `/health` (liveness, no DB flap). Application `depends_on: condition: service_started` plus an entrypoint wait-for-postgres for first boot. Do **not** use `/ready` as the **only** healthcheck if that will `restart: unless-stopped` on a 30 s DB blip.

If a single check must be chosen: **`/health`** for `restart` policy; document `/ready` for load balancers.

A65’s `curl … /weatherforecast` and the planned `/api/v1/health/live` are **replaced** by `/health`.

### 10.2 systemd (Windows MT5 / Linux FIX)

```text
# liveness — restart the unit only if this fails
ExecStartPost=  (wait until /health 200)

WatchdogSec= or a sidecar curl loop against /health

# readiness — used by nginx upstream / lb; NOT by Restart=
# GET http://127.0.0.1:9101/ready   (mt5)
# GET http://127.0.0.1:9102/ready   (fix)
```

**Never** configure `Restart=always` on `/ready` 503.

### 10.3 Ports (lab)

| Process | App port | Probe port | Probe bind |
|---|---|---|---|
| API | 5160 / 8080 | **same** | existing |
| MT5 worker | none (worker) | **9101** | loopback / private |
| FIX worker | none (worker) | **9102** | loopback / private |

---

## 11. Security

1. Anonymous by design (A51). That is acceptable because the body is an allow-list and the worker ports are private.
2. Sanitizer (A26 §3.4) runs on `/ready` `info` the same as on dashboard JSON.
3. Do not log the full `/ready` body at Information if it later grows broker error strings. Log `status`, `service`, failed check names, reason codes.
4. Do not add `/ready?verbose=1` that dumps `IConfiguration`.
5. `AllowedHosts=*` on API (today) is unrelated; probes still must not leak secrets when that is fixed.
6. Worker probe ports are **not** CORS-enabled and not reachable from React. The browser talks only to the Linux API.

---

## 12. Tests (add when coding; names lock here)

None of these exist (A09/A27 empty stubs). They are acceptance for this spec.

| Class | Must prove |
|---|---|
| `Health.LivenessDoesNotTouchDatabaseTests` | `/health` 200 with Npgsql disabled / connection string pointed at a closed port. |
| `Health.ReadyRequiresPostgresTests` | `/ready` 503 when DB down; 200 when `SELECT 1` works. All three hosts. |
| `Health.ReadyDoesNotRequireRedisTests` | Redis down, Postgres up → `/ready` 200; `info.redis.status=fail` allowed. |
| `Health.FixReadyDoesNotRequireRealCopyTests` | **Title test.** `REAL_COPY_EXECUTION_ENABLED=false`, Postgres up → FIX `/ready` **200** and body `info.realCopyExecutionEnabled=false`. |
| `Health.FixReadyDoesNotRequireReadyForExecutionTests` | TRADE `LoggedOn` or `Disconnected`, `readyForExecution=false` → 200. |
| `Health.FixReadyDoesNotRequireLogonTests` | Both sessions `DISABLED` / disconnected → 200 if DB up. |
| `Health.FixReadyDoesNotRequireLeaseTests` | `leaseHeld=false` → 200. |
| `Health.FixReadyDoesNotRequireQuoteTests` | QUOTE flag off / stale → 200. |
| `Health.Mt5ReadyDoesNotRequireBrokerSocketTests` | Both brokers disconnected → 200 if DB up. |
| `Health.ApiReadyDoesNotRequireWorkerHeartbeatTests` | No `mt5-worker` / `fix-worker` rows → API `/ready` 200. |
| `Health.Ready503OnShutdownTests` | `ApplicationStopping` → `/ready` 503. |
| `Health.ProbeDoesNotLeakSecretsTests` | Connection string with `Password=`; FIX password in options; body + logs contain neither. |
| `Health.ProbePathsAreAnonymousTests` | No `Authorization` header → 200/503, never 401. |
| `Health.HttpsRedirectionDoesNotBreakProbesTests` | HTTP GET `/health` is 200, not 30x. |
| `Health.WeatherforecastRemovedWhenProbesShipTests` | `/weatherforecast` 404 after I6. |

`Health.FixReadyDoesNotRequireRealCopyTests` is **blocking** for any `fix-worker` HTTP merge.

---

## 13. Implementation checklist (later coding task)

Do not execute in this artifact.

1. Shared DTOs + `PostgresReadyCheck` + redacting writer in Infrastructure.
2. API: map `/health` + `/ready`; exclude from HTTPS redirect; delete weatherforecast; replace `TraderIntelligence.Api.http` sample.
3. MT5 worker: FrameworkReference `Microsoft.AspNetCore.App` (or tiny listener); bind **9101**; keep Worker SDK for hosted services.
4. FIX worker: same on **9102**.
5. Bind `ConnectionStrings:Postgres` (or `DATABASE_URL`) on all three. Fail ready if missing.
6. Compose A65: replace `/weatherforecast` and `/api/v1/health/live` with `/health`.
7. Tests in §12 green against Testcontainers Postgres (Integration) + in-memory lifetime fakes (Unit).
8. Dashboard remains `GET /api/v1/system/health` (auth). Do not point React at `:9101` / `:9102`.

Suggested files (not created now):

```text
src/Application/Health/HealthProbeDtos.cs
src/Infrastructure/Health/PostgresReadyCheck.cs
src/Infrastructure/Health/ProcessLifetimeReadyCheck.cs
src/Infrastructure/Health/ProbeJsonWriter.cs
apps/api/Endpoints/ProbeEndpoints.cs
apps/mt5-worker/Probes/ProbeHostedService.cs
apps/fix-worker/Probes/ProbeHostedService.cs
tests/Unit/Health/FixReadyDoesNotRequireRealCopyTests.cs
tests/Integration/Health/ReadyRequiresPostgresTests.cs
```

---

## 14. Anti-patterns (reject in review)

```text
[DO NOT] Gate FIX /ready on REAL_COPY_EXECUTION_ENABLED
[DO NOT] Gate FIX /ready on READY_FOR_EXECUTION, Logon, lease, or quote age
[DO NOT] Gate MT5 /ready on Manager connected
[DO NOT] Gate API /ready on worker heartbeats or Redis
[DO NOT] Point systemd Restart= or k8s liveness at /ready
[DO NOT] Point compose restart at /ready if a DB blip should keep FIX sockets
[DO NOT] Implement only /api/v1/health/live and skip /health /ready
[DO NOT] Keep /weatherforecast as the compose probe
[DO NOT] Return IConfiguration, options POCOs, or EF entities
[DO NOT] Put fencing tokens or FIX passwords in info
[DO NOT] Treat /ready 200 as permission to send NewOrderSingle
[DO NOT] Logout FIX because /ready returned 503
[DO NOT] Invent source trades when MT5 /ready is 200 but brokers are down
[DO NOT] Run migrations or SELECT large tables from the probe
[DO NOT] Share one probe port on 0.0.0.0 for workers
[DO NOT] Make Linux API scrape Windows Manager to decide its own /ready
```

---

## 15. Traceability

| Requirement | Section |
|---|---|
| User: design `/health` `/ready` for API, MT5, FIX | §2, §6–§8 |
| User: Ready requires DB | §3.2, §5, §9 |
| User: FIX ready does not require REAL execution | §0, §8.5–§8.6, §12 title test |
| §41 default `REAL_COPY=false` | §0, §8.1 |
| §62 MT5 unavailable / retry / stale-source | §7.4, §14 |
| §62 DB unavailable / fail closed / no memory execution | §3.2, §8.8, A53 §4.5 |
| §42 `READY_FOR_EXECUTION` is recon, not a probe | §8.5 |
| §28 / A46 lease must not flap | §5, §8.7 |
| A06 `/health` anonymous; dashboard separate | §2.2, §6.6 |
| A26 `/api/v1/health` inventory | §2.1 — not a probe |
| A49 flags: connect without sending | §8 |
| A50 no metric dump on anonymous live | §4.1 |
| A51 unauthenticated `/health` | §3.2, §11 |
| A54 ports / private worker health | §7.2, §10.3 |
| A65 compose probe | §10.1 |
| §55 / A26 secret denylist | §4.2, §11 |
| §69 first useful does not need live NOS | §0 |

### 15.1 What this file supersedes (probe names only)

| Sibling | Old probe guidance | Now |
|---|---|---|
| A06 §4.2 | `/health` + `/health/ready` (ready = PG + Redis + workers) | `/health` + `/ready`; ready = **DB only** (workers/Redis → dashboard) |
| A26 §6.14 | `/api/v1/health/live` unauthenticated | **`/health`** |
| A54 §6.3 | API `/health/ready` = Postgres + Redis | API `/ready` = **Postgres**; Redis is INFO |
| A65 | compose `/weatherforecast` then `/api/v1/health/live` | compose `/health` |

Dashboard contracts in A06 / A26 are **unchanged**.

---

## 16. Measured tree (2026-08-18) — do not greenwash

| Item | Evidence |
|---|---|
| API HTTP surface | `D:\Prop\apps\api\Program.cs` — `MapGet("/weatherforecast")` only |
| API `.http` | `GET /weatherforecast/` |
| API packages | no `AspNetCore.HealthChecks.*` |
| MT5 worker | `Program.cs` + `Worker.cs` delay loop; no listen port |
| FIX worker | identical template; `CTraderFixOptions.RealCopyExecutionEnabled` default **false** but **unbound** (A49) |
| Infrastructure | `TraderDbContext` exists; **not** registered in any host |
| Web hook | `useHealth()` → `GET /api/health` (not a probe; not implemented) |

Classification:

| Component | Class |
|---|---|
| Probe contract (this file) | **EXISTS_AND_GOOD** (spec only) |
| API `/health` `/ready` | **MISSING** |
| Worker HTTP probes | **MISSING** |
| `REAL_COPY` send gate | **MISSING** (safe by absence of 35=D) |
| Using `/ready` as that gate | **FORBIDDEN** |

---

## 17. What this artifact did not do

- Did not modify anything under `D:\Prop\src`, `D:\Prop\apps`, compose, or tests.
- Did not add packages or listen ports.
- Did not implement HealthChecks middleware.
- Did not change A65 compose files (document-only supersession).
- Did not authorize `REAL_COPY_EXECUTION_ENABLED=true`.

**Bottom line:** three processes, two paths. `/health` is “I am alive.” `/ready` is “I can reach PostgreSQL.” FIX `/ready` is **supposed** to be 200 while real copy is off. Execution safety is the send gate, the lease, recon, and the kill switch — not the orchestrator probe.

---

*End of A77. Product source was not modified.*
