# B06 — `apps/api` weatherforecast leftovers and replacement endpoints

| Field | Value |
|---|---|
| Agent | B06 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B06_api_gap.md` |
| Scope | `D:\Prop\apps\api` only (product source **not** modified) |
| Law | Architecture v2 §§46–55, 59, 69; A26 dashboard contract; A63 first-useful catalog; A77 `/health`+`/ready` |
| Method | Source inspection of every non-`bin`/`obj` file under `apps/api`. SHA-256 of those files. Cross-check vs A06 / A26 / A55 / A63 / A77. API **not** launched. No HTTP traffic captured. |
| Relates | A06 (stale host snapshot), A26, A63, A77, A55, `apps/web` consumer (read-only, not edited) |

---

## 1. Verdict

**The C# WeatherForecast template is gone from `Program.cs`. Two leftover weatherforecast artifacts remain in `apps/api`.** They are not a live HTTP route. They are dead launch / REST-client leftovers that still name a path the host no longer maps.

Honest measured state (2026-08-18, files as hashed below):

| Check | Result |
|---|---|
| `MapGet("/weatherforecast")` / `record WeatherForecast` | **ABSENT** from `Program.cs` |
| Leftover `weatherforecast` string in product source | **2 hits** — `.http` sample + IIS Express `launchUrl` |
| Live HTTP surface | **15** anonymous unversioned maps (`/health`, `/ready`, 12 `GET /api/*`, 1 `POST /api/ops/resync`) |
| `/api/v1/**` catalog (A63) | **0 / 46** routes. Prefix **MISSING** |
| Auth / RBAC / audit (§59) | **MISSING.** Entire host is anonymous, including `POST /api/ops/resync` |
| SignalR hub | **MISSING** (`AddSignalR` / `MapHub` not called). Web still targets `/hubs/dashboard` |
| First-useful replacement set | **Specified below.** Do not keep weatherforecast beside it. Do not treat unversioned `/api/*` as the catalog. |

A06 / A26 / A55 / A63 / A77 still describe `Program.cs` as “stock weatherforecast only.” **That snapshot is stale.** This file supersedes those host-state claims for `apps/api`. The leftover **names** they listed in `.http` and IIS Express `launchUrl` are still true.

Classification (arch §73):

| Component | Class |
|---|---|
| `apps/api` host | **EXISTS_NEEDS_REFACTOR** |
| `GET /weatherforecast` route + `WeatherForecast` record | **GONE** (was DEPRECATED) |
| `.http` + IIS `launchUrl` leftovers | **DEPRECATED** — delete / retarget |
| Unversioned `/api/*` demo maps | **EXISTS_NEEDS_REFACTOR** — not the v1 contract |
| `/api/v1` + auth + hub + sanitizer | **MISSING** |

---

## 2. Inventory of leftover weatherforecast (product source)

Grep of `D:\Prop\apps\api` (excluding `bin/` / `obj/`): **2 matches, 2 files.**  
Grep of `apps/api/bin` and `apps/api/obj`: **0 matches.**  
No `WeatherForecast.cs`. No `Controllers/WeatherForecastController.cs`.

### 2.1 File hashes (non-build)

| Path | Bytes | SHA-256 |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | 4503 | `13CF80036BDE8832122D1D059902AFF0C557CE8341C51255C2C76E7F8ADA62B4` |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 157 | `353BB5D9718D6F86F218C1CE0885A55D8F49F68C249F04DA18E363DEA334543A` |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1133 | `E092DE590CC74329369741650044845BFFF555D87B75D7B0C3C80E00F6E00E78` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` |
| `D:\Prop\apps\api\appsettings.json` | 431 | `8DCE4CBECDD1F8E7B03DDF1C25430BACCD05795D64B19798A6B0CDAACE85902B` |
| `D:\Prop\apps\api\appsettings.Development.json` | 127 | `73F95F9E0CEB205FC1C4DC50C07697FCFA29D7087868C2AEF1D504CB38C771EC` |

`.http` SHA-256 is **unchanged** from A55 (still the stock Visual Studio REST Client sample).  
`launchSettings.json` SHA-256 **changed** from A55 `5CFA6A24…` — `http` / `https` `launchUrl` moved to `swagger`; **IIS Express did not.**

### 2.2 Leftover A — REST Client sample (entire file is template)

`D:\Prop\apps\api\TraderIntelligence.Api.http` (complete):

```http
@TraderIntelligence.Api_HostAddress = http://localhost:5160

GET {{TraderIntelligence.Api_HostAddress}}/weatherforecast/
Accept: application/json

###
```

| Token | Why leftover |
|---|---|
| `http://localhost:5160` | Stock `dotnet new webapi` Kestrel port. Current `http` profile is `http://localhost:5000` (matches Vite `VITE_API_URL` default). Hitting `:5160` will miss the running API. |
| `GET …/weatherforecast/` | Stock sample. **Host does not map this path.** Client will get 404 if pointed at a live process. |
| Trailing slash | Template. Irrelevant once the request is deleted. |

**Replace with:** a `.http` file that exercises the **replacement endpoints in §5**, host `http://localhost:5000`, starting with `GET /health` and `GET /ready`. Do not add weatherforecast as a “compat” alias.

### 2.3 Leftover B — IIS Express launch URL

`D:\Prop\apps\api\Properties\launchSettings.json`:

```json
"http": {
  "launchUrl": "swagger",
  "applicationUrl": "http://localhost:5000"
},
"https": {
  "launchUrl": "swagger",
  "applicationUrl": "https://localhost:7294;http://localhost:5000"
},
"IIS Express": {
  "launchBrowser": true,
  "launchUrl": "weatherforecast",
  ...
}
```

| Profile | `launchUrl` | Status |
|---|---|---|
| `http` | `swagger` | Weatherforecast **removed**. Half-migration: `UseSwagger()` is on in Development; **`UseSwaggerUI()` is not**, so `/swagger` is likely 404. |
| `https` | `swagger` | Same. |
| `IIS Express` | **`weatherforecast`** | **LEFTOVER.** Browser opens a path the host does not serve. |

IIS Express still: `windowsAuthentication: false`, `anonymousAuthentication: true`, `http://localhost:18720`, `sslPort: 44389`.

**Replace with:** `"launchUrl": "health"` (orchestrator probe, always 200, no secrets) **or** `"swagger"` only after `UseSwaggerUI()` is wired and **Development-only**. Never leave `weatherforecast`.

### 2.4 What is **not** leftover anymore (do not re-delete)

These existed in A06 / A29 / A55 snapshots and are **gone** from current `Program.cs`:

```text
app.MapGet("/weatherforecast", () =>
    Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        )));

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary);
```

Also gone vs A06:

- `UseHttpsRedirection()` (A65 footgun) — **not present**
- Sole-route weatherforecast — **replaced by 15 other maps**
- Unused project refs — `Program.cs` now calls `AddTraderIntelligence`, `IDashboardQueries`, `TraderDbContext`, `DealIngestionService`, `DemoSeeder`

**Do not resurrect the WeatherForecast record.** A GET that 404s is the correct end state.

### 2.5 Out of `apps/api` but still naming weatherforecast

Not product leftovers in this project; listed so a later compose / docs pass does not re-probe the dead path.

| Location | Note |
|---|---|
| `reports/swarm/20260818/A65_docker_compose.md` | Sketch `curl …/weatherforecast` as a **temporary** compose probe. No `docker-compose.yml` under `apps/api`. A77 **supersedes**: probe `GET /health`. |
| A06, A26, A29, A55, A63, A77 host-state paragraphs | Stale: they still say `Program.cs` is weatherforecast-only. |

---

## 3. Current measured host (what actually replaced the template in code)

`D:\Prop\apps\api\Program.cs` (91 lines). Composition:

- `AddTraderIntelligence(configuration)` → in-memory EF if `ConnectionStrings:TraderIntelligence` is empty (it is `""`), else Npgsql
- `AddEndpointsApiExplorer` + `AddSwaggerGen`
- CORS default policy: **`AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`**
- Development: `UseSwagger()` only (no UI)
- Startup: `EnsureCreatedAsync` + `DemoSeeder.SeedAsync`
- **No** `AddAuthentication`, `AddAuthorization`, `UseSerilog`, `AddSignalR`, `MapHub`, `UserSecretsId`

### 3.1 Live maps (complete)

| # | Method | Path | Body / source | Auth |
|---|---|---|---|---|
| 1 | `GET` | `/health` | `{ status: "ok", utc }` — no DB I/O | anonymous |
| 2 | `GET` | `/ready` | `{ ready: true, brokers }` via `CountAsync(db.Brokers)` | anonymous |
| 3 | `GET` | `/api/health` | **Hardcoded** demo inventory (`mt5Connections` healthy, `redis` unhealthy, `outboxBacklog: 0`) | anonymous |
| 4 | `GET` | `/api/overview` | `IDashboardQueries.GetOverviewAsync` | anonymous |
| 5 | `GET` | `/api/brokers` | `GetBrokersAsync` | anonymous |
| 6 | `GET` | `/api/groups` | `GetGroupsAsync` | anonymous |
| 7 | `GET` | `/api/traders` | `GetTradersAsync(broker, state)` | anonymous |
| 8 | `GET` | `/api/traders/{broker}/{login}` | `GetTraderAsync` — **same DTO as list row**, not §51 detail | anonymous |
| 9 | `GET` | `/api/trades` | **Raw `ReconstructedTrades` EF entities**, last 200, `broker` query unused | anonymous |
| 10 | `GET` | `/api/fix/sessions` | `GetFixSessionsAsync` | anonymous |
| 11 | `GET` | `/api/risk` | `GetRiskAsync` | anonymous |
| 12 | `GET` | `/api/risk/status` | **Alias** of `/api/risk` | anonymous |
| 13 | `GET` | `/api/reconciliation/status` | **Hardcoded** `{ lastReconciliation: now, unknownPositions: 0, mismatches: 0, orphanFills: 0 }` | anonymous |
| 14 | `GET` | `/api/settings` | **Hardcoded** flags + broker names | anonymous |
| 15 | `POST` | `/api/ops/resync` | Syncs `ACHIEVER` + `STARWAVEFX` from 2026-01-01; rebuilds logins `10001,10002,10003,99001` | anonymous |

These are **demo / sketch** routes. They are **not** the replacement contract. They share names with A26/A63 but:

- unversioned (`/api` not `/api/v1`)
- no envelope (`data` / `page` / `error.correlationId`)
- no Bearer / roles / audit
- several bodies are invented constants (`/api/health`, `/api/reconciliation/status`, `/api/settings`)
- `/api/trades` serializes EF (allow-list law violated)
- `POST /api/ops/resync` is **not** in A63 and is an unaudited mutation

`apps/web/src/api/hooks.ts` already calls this unversioned set (except resync). A63 explicitly says those paths are **non-normative** and first-useful React must retarget `/api/v1` + `/hubs/ops`.

### 3.2 Packages still unused as features

| Package | Wired? |
|---|---|
| `Swashbuckle.AspNetCore 6.6.2` | Partial: `AddSwaggerGen` + `UseSwagger`. No UI. |
| `Serilog.AspNetCore 8.0.2` | **No** `UseSerilog` |
| `Microsoft.AspNetCore.SignalR.Common 8.0.4` | **No** hub. Common is still the wrong host package. |

### 3.3 Config that is not weatherforecast but is adjacent risk

`appsettings.json` now has `ConnectionStrings:TraderIntelligence` (empty) and a `CTrader` block (`Host`, `AccountId`, `Password: ""`, flags). Password is empty today. **Do not later bind this options object into a dashboard DTO.** Dashboard is not a vault (A26 §3, A63 §2).

`AllowedHosts: "*"` remains.

---

## 4. What weatherforecast is supposed to be replaced **by**

Binding sources, in precedence order for this gap:

1. **A77** — process probes (`/health`, `/ready`). Supersedes A26 `/api/v1/health/live` and A65 compose `/weatherforecast`.
2. **A63 §7.1** — first-useful REST + one hub. This is the implementable catalog that replaces the template surface.
3. **A26** — full dashboard (includes Phase 6–8 pages). Extra routes in A26 that A63 marks **out of first useful** must not ship as v1.
4. Architecture §§46–54 (pages), §55 (no secrets), §59 (RBAC).

Weatherforecast was a **liveness demo + browser launch target + `.http` sample**. Those three jobs map as follows.

### 4.1 Direct 1:1 replacements for the leftover jobs

| Leftover job | Delete | Replace with | Why this path |
|---|---|---|---|
| Anonymous “is the process up?” GET | `GET /weatherforecast` | **`GET /health`** | A77. Body `{ "status": "ok" }` only. **No** inventory, no `utc` required, no dependency I/O. Current map is close; drop extra fields later. |
| Orchestrator / compose / IIS probe | `curl …/weatherforecast` (A65 sketch) | **`GET /health`** | A77 §2. **Do not** implement `/api/v1/health/live`. |
| Readiness (DB can answer) | *(template had none)* | **`GET /ready`** | A77. Hard-requires Postgres `SELECT 1`. Current `/ready` counts brokers on in-memory/Npgsql and always returns `ready: true` — **PARTIAL**. |
| Browser `launchUrl` | `weatherforecast` (IIS Express) | **`health`** (safe) or Dev-only Swagger UI | Do not launch an unauthenticated dashboard page. |
| REST Client sample | entire `.http` weatherforecast request | Samples in §5.3 | Host `http://localhost:5000`. |
| OpenAPI “something to click” | weather summaries | Dev `UseSwagger` + `UseSwaggerUI` **or** nothing in prod | A63: Swagger UI in production is out of scope / SuperAdmin-only. |

**Retired names — do not add as the weatherforecast replacement:**

| Path | Disposition |
|---|---|
| `GET /weatherforecast` | **404.** Never alias. |
| `GET /api/v1/health/live` | A26/A65. **Do not implement** (A77). |
| `GET /health/live` | Do not add. `/health` is liveness. |
| `GET /health/ready` | A63 listed it; A77 retires it. Prefer `/ready` only. |
| `GET /api/health` | Web-hook debt. Not a probe. Replace with `GET /api/v1/health` (authenticated inventory). |

### 4.2 Probe vs dashboard (do not collapse)

```text
Orchestrator / compose / systemd / IIS ping
    GET /health     process alive?     ← weatherforecast's only honest job
    GET /ready      this process can use Postgres?

Human / React System Health
    GET /api/v1/health            authenticated inventory (A63 §5.1)
    GET /api/v1/overview          header strip (§47)
    GET /api/v1/fix/sessions      QUOTE / TRADE cards (§52)
    GET /api/v1/brokers           source connection (§48)
```

---

## 5. Replacement endpoint catalog (first useful — implement this set)

Base: **`/api/v1`**. JSON camelCase, ISO-8601 UTC `Z`, allow-list DTOs only.  
Auth: `Authorization: Bearer <access_token>` on every `/api/v1/**` and `/hubs/**` except login.  
Anonymous: **`GET /health`**, **`GET /ready`**, **`POST /api/v1/auth/login`** only.  
Envelopes, errors, denylist, RBAC: A63 §§1–3. Mutations write `audit_logs`.

**Count:** 2 probes + 46 HTTP routes + 1 hub (A63 §7.1). That is the surface that **replaces** weatherforecast, not “weatherforecast plus a few `/api` maps.”

### 5.1 Probes (anonymous)

| Method | Path | Roles | Replaces weatherforecast? | Purpose |
|---|---|---|---|---|
| `GET` | `/health` | anonymous | **Yes — liveness** | `{ "status": "ok" }`. Zero I/O. ≤ 50 ms. Stay 200 if brokers/FIX/DB down. |
| `GET` | `/ready` | anonymous | Readiness (template had none) | Postgres required. 200 or 503. Must **not** require `REAL_COPY_EXECUTION_ENABLED` or FIX Logon. |

### 5.2 First-useful REST + hub (A63 §7.1 — binding)

#### Health / events

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/health` | ReadOnly+ | System Health page inventory (MT5 brokers, reconstruction, scoring, FIX, postgres/redis). **Not** a probe. |
| `GET` | `/api/v1/system/events` | ReadOnly+ | Recent `system_events`. Pageable. |

#### Auth (§59)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | anonymous | JWT access + httpOnly Secure SameSite=Strict refresh. Never echo password. |
| `POST` | `/api/v1/auth/logout` | any auth | Revoke refresh family. |
| `POST` | `/api/v1/auth/refresh` | refresh cookie | Rotate access token. |
| `GET` | `/api/v1/auth/me` | any auth | `{ id, email, displayName, role }` |

#### Overview / settings / audit

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/overview` | ReadOnly+ | One §47 snapshot + header flags. `destinationRealPnl` may be `0` while execution is off. Do not invent margin. |
| `GET` | `/api/v1/settings/public` | ReadOnly+ | Non-secret flags only (`realCopyExecutionEnabled: false`, FIX quote/trade enabled, XAU map **names/IDs**). |
| `GET` | `/api/v1/audit/logs` | RiskManager+ | Filter `actorId`, `action`, `from`, `to`. ReadOnly → 403. |

#### Brokers (§48, §69.1)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/brokers` | ReadOnly+ | Achiever + StarwaveFX (+ future). `managerLoginMasked` only. No passwords. |
| `GET` | `/api/v1/brokers/{brokerId}` | ReadOnly+ | Same row + last **error class**. `?revealLogin=true` SuperAdmin only — integer login, never password. |

**No credential `PUT`/`PATCH` in v1.**

#### Groups / accounts (§49, §69.2–3)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/mt5/groups` | ReadOnly+ | Every discovered group. Query `brokerId`, `enabledForAnalysis`, `plan`, `q`. |
| `PATCH` | `/api/v1/mt5/groups/{groupId}` | Analyst+ | Toggle `enabledForAnalysis` only. Audited `GROUP_ANALYSIS_UPDATE`. |
| `GET` | `/api/v1/mt5/accounts` | ReadOnly+ | Paged (~5k). Filter `brokerId`, `groupId`, `q`. |

#### Traders (§50–51, §69.7–8, §69.11)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/traders` | ReadOnly+ | Leaderboard. Filters: broker, group, state, score, risk, trade count, martingale, date. |
| `GET` | `/api/v1/traders/{brokerId}/{login}` | ReadOnly+ | Detail header + scores + flags. 404 if compound key missing. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/features` | ReadOnly+ | Baseline behavior features. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/risk-flags` | ReadOnly+ | Martingale / averaging-down / lot escalation / abnormal sizing. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/lot-timeline` | ReadOnly+ | Lot-size series. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/holding-times` | ReadOnly+ | Holding-time histogram. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/shadow` | ReadOnly+ | This trader’s shadow positions + P&L. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/trades` | ReadOnly+ | Reconstructed XAU history. Flag `isFirstThree`. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/scores` | ReadOnly+ | Current score + timeline. `mlProbability` nullable until Phase 6. |
| `PATCH` | `/api/v1/traders/{brokerId}/{login}/state` | RiskManager+ | `WATCH` \| `SHADOW` \| `PAUSED` \| `RISK_BLOCKED` only. `LIVE` / `LIVE_CANDIDATE` → **409** while execution off. Audited. |

#### Trades / scoring

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/trades` | ReadOnly+ | Cross-trader reconstructed XAU. Page max 200. **Allow-list DTO**, not EF entities. |
| `GET` | `/api/v1/scoring/summary` | ReadOnly+ | Last run, scored count, `baseline.v1`, counts by state. |

#### Shadow (§24, §69.11)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/shadow/portfolio` | ReadOnly+ | Aggregate P&L, open/closed counts, selected traders. |
| `GET` | `/api/v1/shadow/positions` | ReadOnly+ | Query `status=open\|closed\|all`, `brokerId`, `login`. |
| `GET` | `/api/v1/shadow/performance` | ReadOnly+ | Source vs shadow series (dest-quote priced). `from`, `to`, `grain=day\|hour`. |
| `GET` | `/api/v1/copy-intents` | ReadOnly+ | Worker-created intents. Clients do not POST intents. |

#### FIX (§52, §69.9–10)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/fix/sessions` | ReadOnly+ | QUOTE + TRADE cards. Password never returned. |
| `GET` | `/api/v1/fix/quote` | ReadOnly+ | XAU mapped?, instrument ID, bid, ask, age, spread. |
| `GET` | `/api/v1/fix/trade` | ReadOnly+ | `executionEnabled` **must be false** in v1. |
| `GET` | `/api/v1/fix/sessions/{session}/events` | ReadOnly+ | `{session}` = `QUOTE` \| `TRADE`. **No raw Logon.** |

#### Risk (§53)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/risk/snapshot` | ReadOnly+ | Equity/margin if known; daily P&L; drawdown; XAU long/short/net. Nulls when unread. |
| `GET` | `/api/v1/risk/rejections` | ReadOnly+ | Rejected copy intents + reason codes. |
| `GET` | `/api/v1/risk/kill-switch` | ReadOnly+ | `{ stopNewExecution, emergencyFlattenAvailable: false, realCopyExecutionEnabled: false }` |
| `POST` | `/api/v1/risk/stop-new-execution` | RiskManager+ | Set/clear `STOP_NEW_EXECUTION`. Does **not** flatten. Audited. |

#### Reconciliation (§54)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/reconciliation` | ReadOnly+ | Latest MT5 + latest cTrader run (cTrader may be `never`). Open-issue counts. |
| `GET` | `/api/v1/reconciliation/runs` | ReadOnly+ | Query `venue=MT5\|CTRADER`. |
| `GET` | `/api/v1/reconciliation/issues` | ReadOnly+ | Query `type`, `status`, `venue`. Empty list ≠ hidden. |
| `POST` | `/api/v1/reconciliation/run` | RiskManager+ | Body `{ "venue": "MT5", "reason": "manual" }`. Does not invent fills. |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/ack` | RiskManager+ | Body `{ "reason": "…" }`. |

#### SignalR

| Hub | Path | Roles | Purpose |
|---|---|---|---|
| `OpsHub` | **`/hubs/ops`** | ReadOnly+ | Push overview, FIX quote, ingest health. Same redaction as REST. |

Web stub `apps/web/src/api/signalr.ts` uses `/hubs/dashboard`. **That name is out of catalog.** First useful must retarget `/hubs/ops`. Polling `/api/v1/overview` + `/api/v1/fix/quote` is enough to unblock first paint (A06 §4.14).

### 5.3 Replacement `.http` samples (when a later coding task is authorized)

Host variable must be `http://localhost:5000` (current `http` profile), **not** `:5160`.

```http
@host = http://localhost:5000

### liveness (replaces GET /weatherforecast/)
GET {{host}}/health
Accept: application/json

###
GET {{host}}/ready
Accept: application/json

###
POST {{host}}/api/v1/auth/login
Content-Type: application/json

{ "email": "ops@example.com", "password": "<SECRET>" }

###
GET {{host}}/api/v1/overview
Authorization: Bearer {{access_token}}
Accept: application/json

###
GET {{host}}/api/v1/brokers
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/mt5/groups
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/traders?page=1&pageSize=50
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/trades?page=1&pageSize=50
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/fix/sessions
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/risk/snapshot
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/reconciliation
Authorization: Bearer {{access_token}}

###
GET {{host}}/api/v1/health
Authorization: Bearer {{access_token}}
```

Do **not** put a real password in the committed `.http` file. Placeholder `<SECRET>` only (§55).

### 5.4 Page → replacement API (architecture §46)

| §46 page | Primary replacement GET | Extra | v1 mutation |
|---|---|---|---|
| Overview | `/api/v1/overview` | hub `overview.updated` | none |
| Brokers | `/api/v1/brokers` | `/brokers/{id}` | none in v1 |
| MT5 Groups | `/api/v1/mt5/groups` | `/mt5/accounts` | `PATCH` analysis |
| Traders | `/api/v1/traders` | | none |
| Trader Detail | `/api/v1/traders/{brokerId}/{login}` | trades, scores, features, flags, lot, holding, shadow | `PATCH …/state` |
| Trade Explorer | `/api/v1/trades` | | none |
| Scoring | `/api/v1/scoring/summary` | | none |
| Shadow Portfolio | `/api/v1/shadow/portfolio` | positions, performance, copy-intents | via trader state |
| cTrader FIX | `/api/v1/fix/sessions` | `/fix/quote`, `/fix/trade`, events | none |
| Risk | `/api/v1/risk/snapshot` | rejections, kill-switch | `POST` stop-new |
| Reconciliation | `/api/v1/reconciliation` | runs, issues | run, ack |
| System Health | `/api/v1/health` | `/system/events` | none |
| Settings | `/api/v1/settings/public` | | none in v1 |
| Audit | `/api/v1/audit/logs` | | none (GET only) |
| Models | **out of v1** | | promote is Phase 6 |
| Live Copy Portfolio | **out of v1** | | execution off |

Web today has pages for the first-useful set except Audit / Models / Live. Shadow page does **not** call an API yet.

---

## 6. Current stub → replacement map (do not ship stubs as v1)

| Current map | Replacement | Gap |
|---|---|---|
| `GET /health` `{status, utc}` | `GET /health` `{status:"ok"}` | Extra `utc`. Acceptable until tightened. **This is the weatherforecast replacement.** |
| `GET /ready` `{ready, brokers}` | `GET /ready` A77 checks | Always `ready: true`; counts brokers instead of `SELECT 1` + 503. In-memory DB makes “ready” a lie in prod. |
| `GET /api/health` hardcoded | `GET /api/v1/health` | Invented `healthy: true` for MT5/FIX. Redis “not required” is a comment, not a check. |
| `GET /api/overview` flat `OverviewDto` | `GET /api/v1/overview` nested envelope | Shape ≠ A63 §4.1. Missing dest margin, stop-new, flatten-available, health **enums**. `XauGross`/`XauNet` hardcoded `0`. |
| `GET /api/brokers` | `GET /api/v1/brokers` | Missing ingest rate, last sync, pool, reconnects, `connectionStatus` enum. Mask is `login/100*100` (numeric), not `"**27"` string (A63 §2.2). `Connected` hardcoded `true`. `LastEventAt` = `UtcNow` (invented). |
| `GET /api/groups` | `GET /api/v1/mt5/groups` | Path wrong. No PATCH. |
| *(missing)* | `GET /api/v1/mt5/accounts` | **MISSING** |
| `GET /api/traders` | `GET /api/v1/traders` | Filters incomplete. No pagination envelope. `MlProbability` null (correct). `ShadowPnl` hardcoded `0`. |
| `GET /api/traders/{broker}/{login}` | `GET /api/v1/traders/{brokerId}/{login}` + sub-resources | Returns **list row**, not §51 detail. Sub-routes **MISSING**. |
| `GET /api/trades` raw EF | `GET /api/v1/trades` DTO | **UNSAFE** allow-list miss. `broker` query ignored. |
| `GET /api/fix/sessions` | `GET /api/v1/fix/sessions` + `/quote` + `/trade` + events | Combined DTO. `/quote` `/trade` **MISSING**. Password not returned today (good). |
| `GET /api/risk` + `/api/risk/status` | `GET /api/v1/risk/snapshot` + `/rejections` + `/kill-switch` | Duplicate alias. Exposure fields `0`. No stop-new POST. |
| `GET /api/reconciliation/status` hardcoded zeros | `GET /api/v1/reconciliation` + runs + issues | Invented clean recon. **Worse than empty.** |
| `GET /api/settings` hardcoded | `GET /api/v1/settings/public` | Includes `brokerConfigs`; no secret dump today. Path/shape wrong. |
| `POST /api/ops/resync` | **Not in catalog** | Delete or replace with an audited, role-gated ops job **later**. Do not keep as anonymous demo. |
| *(missing)* | `/api/v1/auth/*` | **MISSING** |
| *(missing)* | `/api/v1/shadow/*`, `/copy-intents` | **MISSING** |
| *(missing)* | `/api/v1/scoring/summary` | **MISSING** |
| *(missing)* | `/hubs/ops` | **MISSING.** Web `/hubs/dashboard` is the wrong name. |

---

## 7. Must **not** ship as a weatherforecast replacement

A63 §7.2 + A26 extras that are **out of first useful**:

| Path | Why |
|---|---|
| `GET /weatherforecast` | Template. 404. |
| `POST /api/v1/risk/emergency-flatten` | Phase 8. TRADE + confirm phrase. |
| `PATCH /api/v1/risk/limits` | Not a §69 item. |
| `PATCH /api/v1/settings/execution` | Live enable; §41 / §68 / §70. |
| `PUT /api/v1/settings` | Secret-leak vector. |
| `GET /api/v1/models` + promote | Phase 6. Do not stub fake ML. |
| `GET /api/v1/live/portfolio` | Execution off. |
| `PUT /api/v1/brokers/{id}/credentials` | Secrets must not go React → API. |
| `PATCH /api/v1/settings/fix` | Host/ports via env/secret store. |
| `GET /api/v1/config` / `/debug/config` | Secret leak. |
| `POST /api/ops/resync` (current) | Anonymous mutation. Not v1. |
| `/hubs/dashboard` | Non-normative stub name. |

---

## 8. Severity (this audit)

| ID | Sev | Finding |
|---|---|---|
| B06-01 | **HIGH** | `TraderIntelligence.Api.http` still `GET /weatherforecast/` on `:5160`. Dead sample; will confuse the next implementer. Replace with §5.3. |
| B06-02 | **MED** | IIS Express `launchUrl` is still `weatherforecast`. `http`/`https` already left it. |
| B06-03 | **BLOCKER** (product, not leftover) | **0** `/api/v1` routes, **0** auth, **0** hub. Unversioned `/api/*` is not the replacement. |
| B06-04 | **HIGH** | Anonymous `POST /api/ops/resync` + raw EF `/api/trades` + hardcoded health/recon. These are **new** unsafety, not template leftovers. |
| B06-05 | **HIGH** | CORS `AllowAnyOrigin` + `AllowedHosts=*` + fully anonymous dashboard GETs. |
| B06-06 | **MED** | `launchUrl: swagger` without `UseSwaggerUI()`. Half-migration from weatherforecast. |
| B06-07 | **MED** | Serilog referenced, unused. SignalR.Common referenced, unused. |
| B06-08 | **LOW** | A-series host-state paragraphs still say weatherforecast is the only route. Docs drift. |

B06-01 and B06-02 are the **leftover weatherforecast** items this task asked to list. Everything else is the gap those leftovers must not be “fixed” into.

---

## 9. Acceptance for deleting weatherforecast leftovers

When a later coding task is authorized (this report does **not** edit product source):

- [ ] `rg -i weatherforecast D:\Prop\apps\api` (excluding `bin`/`obj`) → **0**
- [ ] `GET /weatherforecast` → **404** (no alias)
- [ ] `GET /health` → 200 `{ "status": "ok" }` with no DB I/O
- [ ] `GET /ready` → 200/503 per A77 (Postgres)
- [ ] `.http` host is `:5000` (or whatever launchSettings `http` is) and samples the §5 catalog
- [ ] All three launch profiles: `launchUrl` is `health` or Dev swagger — **never** `weatherforecast`
- [ ] Compose / Dockerfile probe (if added) uses `/health`, not `/weatherforecast`
- [ ] Dashboard routes live under `/api/v1` with allow-list DTOs, Bearer, and no EF entity dump

---

## 10. What this audit did **not** do

- Did not modify `apps/api` or any product source.
- Did not `dotnet run` / hit `:5000` or `:5160`. Route list is from `MapGet`/`MapPost` as read.
- Did not implement `/api/v1`, auth, or the hub.
- Did not invent scores, MFE/MAE, or live P&L.
- Did not treat A06’s “0 dashboard endpoints” as still true — that claim is **stale**; the endpoints that exist are **unversioned stubs**, not the catalog.
