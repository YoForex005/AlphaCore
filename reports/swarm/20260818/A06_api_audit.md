# A06 — `apps/api` audit vs architecture §§46–54, 55, 59

| Field | Value |
|---|---|
| Agent | A06 |
| Date | 2026-08-18 |
| Scope | `D:\Prop\apps\api` only (product source **not** modified) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §§46–54, 55, 59; first-useful bar §69 |
| Method | Source inspection. API was **not** launched. No HTTP traffic captured. |
| Classification (arch §73) | **MISSING** dashboard/auth surface; host skeleton **EXISTS_NEEDS_REFACTOR**; weatherforecast **DEPRECATED** |

---

## 1. Verdict

**`TraderIntelligence.Api` is still the stock ASP.NET Core 8 weatherforecast minimal-API template.** It is not a dashboard BFF. It cannot serve React §§46–54. It has no authentication, no RBAC (§59), no audit trail, no secret-redaction contract (§55), no health/readiness, no SignalR hub, and no EF/Redis wiring despite project references.

Honest measured state:

| Check | Result |
|---|---|
| Dashboard APIs (§§46–54) | **0 / required. MISSING.** |
| Auth + RBAC (§59) | **MISSING.** Entire host is anonymous. |
| Secrets to React (§55) | **No secret leak today** — no broker/FIX/DB config is served. **Also no redaction layer** for when those exist. |
| First useful version (§69 item 12: “show all of this in React”) | **Blocked.** `apps/web` does not exist; API has nothing to bind to. |
| Production-safe default | **FAIL.** `AllowedHosts=*`, anonymous `GET /weatherforecast`, `launchUrl` = weatherforecast. |

Do not treat package references (Swashbuckle, Serilog, SignalR.Common) as implemented features. They are unused.

---

## 2. What exists on disk (evidence)

### 2.1 Project

- Path: `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- TFM: `net8.0`, nullable, implicit usings
- Project refs: `src\Domain`, `src\Application`, `src\Infrastructure` — **never used in `Program.cs`**
- Packages: `Microsoft.AspNetCore.SignalR.Common 8.0.4`, `Serilog.AspNetCore 8.0.2`, `Swashbuckle.AspNetCore 6.6.2`
- **Not** referenced: JWT/OpenIddict/Identity, health checks, OpenTelemetry, CORS package (built-in unused), EF registration, Redis
- **No** `UserSecretsId` (workers have one; API does not)
- Solution membership: `Mt5TraderIntelligence.sln` project `{D17266FA-2F65-4F00-9701-BC5DD52B8439}`

Downstream layers the API would call are empty stubs (`Class1` only):

- `D:\Prop\src\Domain\Class1.cs`
- `D:\Prop\src\Application\Class1.cs`
- `D:\Prop\src\Infrastructure\Class1.cs` (csproj already has EF Core + Npgsql + StackExchange.Redis; **no DbContext, no Redis registration**)

`apps/web` is absent. React has no host.

### 2.2 Entire HTTP surface

`D:\Prop\apps\api\Program.cs` (complete behavior):

```text
WebApplication.CreateBuilder
UseHttpsRedirection
MapGet("/weatherforecast") → 5 random WeatherForecast records
app.Run()
```

No `AddAuthentication`, `AddAuthorization`, `AddCors`, `AddDbContext`, `AddStackExchangeRedis`, `AddSignalR`, `AddSwaggerGen`, `UseSerilog`, `MapHealthChecks`, `MapHub`, `MapControllers`, or versioned `/api` group.

### 2.3 Config / launch

| File | Content that matters |
|---|---|
| `appsettings.json` | Default `Logging` + `"AllowedHosts": "*"` |
| `appsettings.Development.json` | Logging only |
| `Properties/launchSettings.json` | IIS anonymous + Windows auth off; profiles `http` `:5160`, `https` `:7294`; `launchUrl`: `weatherforecast` |
| `TraderIntelligence.Api.http` | Single `GET /weatherforecast/` |
| `.env.example` | **Does not exist** (arch §55 requires placeholders only) |

No MT5 / FIX / Postgres / Redis / JWT settings in the API project. That is currently **safer** than a half-wired config dump. It is still not a secret store.

### 2.4 Unused packages (do not greenwash)

| Package | Wired? |
|---|---|
| Swashbuckle | No `AddSwaggerGen` / `UseSwagger` |
| Serilog.AspNetCore | No `UseSerilog` |
| SignalR.Common | No hub, no `AddSignalR` (and Common is the wrong package for a host — need `Microsoft.AspNetCore.SignalR` / framework reference) |

---

## 3. Gap vs architecture

Status codes: **MISSING** | **PARTIAL** | **PRESENT** | **N/A (no data yet)**.

### 3.1 §46 React Dashboard — API as BFF

Nav pages that require API data:

`Overview`, `Brokers`, `MT5 Groups`, `Traders`, `Trader Detail`, `Trade Explorer`, `Scoring`, `Models`, `Shadow Portfolio`, `Live Copy Portfolio`, `cTrader FIX`, `Risk`, `Reconciliation`, `System Health`, `Audit`, `Settings`.

| Requirement | Status | Note |
|---|---|---|
| JSON contract per page | MISSING | No DTOs, no versioning |
| Auth’d session for dashboard | MISSING | |
| SignalR / WebSocket live ops | MISSING | Stack §5 names SignalR; package present, unused |
| CORS for Vite origin | MISSING | Will block `apps/web` the day it appears |
| `/api/v1` prefix | MISSING | Only `/weatherforecast` |

### 3.2 §47 Overview Page

Required aggregates: total MT5 accounts; connected source brokers; XAUUSD traders; traders with ≥3 completed trades; counts for Watch / Shadow / Live candidates / Live copied / Risk blocked; Shadow P&L; destination real P&L; current XAU gross/net exposure; destination free margin + margin level; MT5 ingestion health; FIX quote health; FIX trade health.

| Status | MISSING |

First useful version **must** implement this as one snapshot (plus health). Destination real P&L / live-copied counts may be zero while `REAL_COPY_EXECUTION_ENABLED=false` (§41) but the fields still belong on the DTO so the UI does not invent them.

### 3.3 §48 Brokers Page

Required per broker: display name, connection status, server, **manager login masked**, group count, account count, deal ingest rate, last event, last successful history sync, pool usage, reconnect count. **No secret values.**

| Status | MISSING |

There is no broker registry endpoint and no DTO allow-list that would strip `MT5_PASSWORD` / proxy credentials if config were later bound into responses.

### 3.4 §49 MT5 Groups Page

Required columns: broker, group, accounts, enabled-for-analysis, plan mapping, last discovered, last synced.

| Status | MISSING |

A later `PATCH` to toggle “enabled for analysis” is an audited mutation (§59).

### 3.5 §50 Trader Leaderboard

Required columns: broker, login, group, completed XAU trades, net source P&L, early score, ML probability (nullable until Phase 6), risk score, martingale / averaging-down / lot-escalation flags, current state, shadow P&L, live allocation, last scored.

Filters: broker, group, state, score, risk, trade count, martingale, date.

| Status | MISSING |

§69 says first useful version ranks traders with a **deterministic** score. `mlProbability` may be `null`. Do not stub fake ML.

### 3.6 §51 Trader Detail Page

Required: account overview; XAU trade history with first 3 highlighted; score timeline; risk flags; behavior features; lot-size timeline; holding-time distribution; SL/TP behavior; drawdown; MFE/MAE **only when valid** (arch §1: do not fabricate from closed deals); shadow positions/P&L; live positions/P&L; source-to-destination mapping.

| Status | MISSING |

### 3.7 §52 cTrader FIX Page

Two cards: QUOTE and TRADE. Shared: host, SSL port, connected?, logged on?, session status, last inbound/outbound, sequence state, reconnect count, last heartbeat/test request, errors.

QUOTE also: XAUUSD mapped?, instrument ID, bid, ask, quote age, spread.

TRADE also: execution enabled?, open orders, open destination positions, last execution report, last reconciliation.

**Never show FIX password.**

| Status | MISSING |

§69 first useful version requires QUOTE session + discovered Pepperstone XAU instrument ID. TRADE card may be “session disabled / not started” but the endpoint should still exist so the UI does not hide venue health.

### 3.8 §53 Risk Dashboard

Required: execution equity/balance/free margin/margin level; daily P&L; current drawdown; XAU long/short/net; risk by copied trader; risk by source broker; rejected copy intents + reasons; `STOP_NEW_EXECUTION` state; `EMERGENCY_FLATTEN` availability (separate permission — §40).

| Status | MISSING |

First useful version is shadow-only. Still expose a **read** risk snapshot (zeros / “execution off”) and `STOP_NEW_EXECUTION` so the flag cannot be forgotten later. Do **not** ship `EMERGENCY_FLATTEN` as a working mutation before TRADE execution exists.

### 3.9 §54 Reconciliation Dashboard

Required: last successful MT5 reconciliation; last successful cTrader reconciliation; unknown external positions; missing internal positions; order mismatches; quantity mismatches; orphan fills; unresolved execution states. **Nothing unresolved silently ignored.**

| Status | MISSING |

First useful version needs **MT5** reconciliation visibility. cTrader TRADE reconciliation can return “not run / N/A” until Phase 7.

### 3.10 §55 Security — never expose secrets to React

Forbidden in any API/SignalR payload:

```text
MT5 passwords
proxy credentials
cTrader account password
FIX password
database passwords
Redis passwords
```

Required: env / OS secret store / Vault; production secrets not in Git; placeholders only in `.env.example`.

| Control | Status |
|---|---|
| Secrets in `appsettings*.json` | **None present** (good, empty template) |
| `.env.example` | MISSING |
| User secrets / Vault wiring | MISSING on API (workers have `UserSecretsId` only) |
| Response DTO allow-list / redaction | MISSING |
| Central log redaction (arch §57) | MISSING (Serilog unused) |
| Manager login masking | MISSING (no broker DTO) |
| FIX password never serialized | MISSING (no FIX DTO) |
| Config dump / `/config` endpoint | Not present (good) |

Risk if someone “just binds options to JSON”: `IConfiguration` will later hold `MT5_PASSWORD`, `CTRADER_FIX_PASSWORD`, proxy and DB strings. Without an explicit allow-list serializer, those will reach React. **Build redaction before the first real broker DTO.**

### 3.11 §59 Authentication and RBAC

Roles: `SuperAdmin`, `RiskManager`, `Analyst`, `ReadOnly`.

Mutations that **must** be role-gated and written to `audit_logs`:

| Action | Min role | First useful version? |
|---|---|---|
| Enable real execution | SuperAdmin | **No** — keep flag false; endpoint may 409 |
| Change risk limits | RiskManager | Optional read; write can wait |
| Pause / resume trader copying (incl. shadow select) | RiskManager | **Yes** (shadow select / pause) |
| Change symbol mapping | SuperAdmin | Read yes; write later |
| Activate `STOP_NEW_EXECUTION` | RiskManager | **Yes** (safe even with execution off) |
| Request `EMERGENCY_FLATTEN` | SuperAdmin + step-up confirm | **No** |
| Promote a model | SuperAdmin | **No** (no ML yet) |
| Change broker / FIX configuration | SuperAdmin | **No** (env/secret store, not React) |

| Control | Status |
|---|---|
| Authentication middleware | MISSING |
| Role claims | MISSING |
| Authorization policies | MISSING |
| Audit log persistence | MISSING (`audit_logs` is a recommended table, not implemented) |
| Anonymous weatherforecast | **UNSAFE** public surface |

`launchSettings.json` explicitly: `windowsAuthentication: false`, `anonymousAuthentication: true`.

---

## 4. First useful version — endpoints that must exist

Bar = architecture **§69** (no ML, no live NewOrderSingle) + dashboard pages needed to **show** items 1–12 in React + §55/§59 so the UI is not a secret/RBAC hole.

Convention:

- Prefix: `/api/v1`
- Auth: Bearer (or cookie BFF) on **all** `/api/**` and hubs
- Anonymous: `/health` liveness only
- JSON, UTC timestamps, `application/json`
- Pagination: `cursor` or `page`/`pageSize` (max 200) on lists
- Errors: RFC 7807 problem+json
- Every mutating call writes `audit_logs` (actor, role, action, entity, before/after, correlation_id)

`apps/web` is out of this file’s scope; these are the API contracts that page will need.

### 4.1 Auth / identity (§59)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | anonymous | Issue session. Lockout + no password echo. |
| `POST` | `/api/v1/auth/logout` | any auth | Revoke refresh. |
| `POST` | `/api/v1/auth/refresh` | refresh token | Rotate tokens. |
| `GET` | `/api/v1/auth/me` | any auth | `{ id, name, roles[] }` for the shell. |

No “register” from the public internet for v1. Seed SuperAdmin out of band.

### 4.2 Health / system (§47 health strip, §46 System Health)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/health` | anonymous | Process up. No dependency details. |
| `GET` | `/health/ready` | anonymous or auth | Postgres + Redis (+ workers via last heartbeat). No secrets. |
| `GET` | `/api/v1/system/health` | ReadOnly+ | MT5 collectors, outbox backlog, FIX QUOTE, FIX TRADE (or “not started”), API, workers. Stale-source flags (§62). |
| `GET` | `/api/v1/system/events` | ReadOnly+ | Recent `system_events` (ingestion/FIX/reconnect). |

### 4.3 Overview (§47, §69)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/overview` | ReadOnly+ | Single snapshot: account/broker counts; XAU trader counts; ≥3-trade count; Watch/Shadow/LiveCandidate/LiveCopied/RiskBlocked; shadow P&L; dest P&L (0 if execution off); XAU gross/net; dest margin fields (null if unknown); MT5 + FIX QUOTE + FIX TRADE health. |

This is the minimum one-call payload for the Overview page.

### 4.4 Brokers (§48, §69.1)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/brokers` | ReadOnly+ | Achiever + StarwaveFX (+ future). Status, server, **masked** manager login, group/account counts, ingest rate, last event, last history sync, pool usage, reconnects. |
| `GET` | `/api/v1/brokers/{brokerId}` | ReadOnly+ | Same + last error class (no stack dumps with connection strings). |

**Forbidden fields:** password, proxy username/password, raw connection string.

### 4.5 MT5 groups + accounts (§49, §69.2–3)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/mt5/groups` | ReadOnly+ | Broker, group path, account count, enabledForAnalysis, plan mapping, lastDiscovered, lastSynced. |
| `PATCH` | `/api/v1/mt5/groups/{groupId}` | Analyst+ | Toggle `enabledForAnalysis` only. Audited. |
| `GET` | `/api/v1/mt5/accounts` | ReadOnly+ | Paged (~5k). Filter `brokerId`, `group`, `search=login`. No PII beyond login/group/balance snapshot if already stored. |

### 4.6 Traders / leaderboard / detail (§50, §51, §69.4–8)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/traders` | ReadOnly+ | Leaderboard. Query: `broker`, `group`, `state`, `minScore`, `maxRisk`, `minTrades`, `martingale`, `from`, `to`, `sort`. |
| `GET` | `/api/v1/traders/{traderId}` | ReadOnly+ | Account overview + current state + scores + flags. `traderId` = stable `{brokerId}:{login}`. |
| `GET` | `/api/v1/traders/{traderId}/trades` | ReadOnly+ | Reconstructed XAU trades. Flag `isFirstThree`. |
| `GET` | `/api/v1/traders/{traderId}/scores` | ReadOnly+ | Score timeline (deterministic early score required; `mlProbability` nullable). |
| `GET` | `/api/v1/traders/{traderId}/features` | ReadOnly+ | Behavior features used for the baseline. |
| `GET` | `/api/v1/traders/{traderId}/risk-flags` | ReadOnly+ | Martingale / averaging-down / lot escalation / others. |
| `GET` | `/api/v1/traders/{traderId}/lot-timeline` | ReadOnly+ | Lot-size series. |
| `GET` | `/api/v1/traders/{traderId}/holding-times` | ReadOnly+ | Holding-time distribution. |
| `GET` | `/api/v1/traders/{traderId}/shadow` | ReadOnly+ | Shadow positions + shadow P&L. |
| `PATCH` | `/api/v1/traders/{traderId}/state` | RiskManager+ | `Watch` / `Shadow` / `Paused` / `RiskBlocked`. **Not** `LiveCopied` while execution flag is false (409). Audited. |

Optional but useful for Trade Explorer nav:

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/trades` | ReadOnly+ | Cross-trader reconstructed XAU trades. Filters: broker, login, from/to, completed. |

### 4.7 Scoring summary (§46 Scoring, §69.7–8)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/scoring/summary` | ReadOnly+ | Last run time, scored count, baseline version. No model promotion. |

**Out of first useful:** `/models`, promote, MLflow.

### 4.8 Shadow portfolio (§46 Shadow, §69.11)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/shadow/portfolio` | ReadOnly+ | Aggregate shadow P&L, open count, selected traders. |
| `GET` | `/api/v1/shadow/positions` | ReadOnly+ | Open/closed shadow positions (dest quote priced). |
| `GET` | `/api/v1/shadow/performance` | ReadOnly+ | Source vs shadow comparison series. |

### 4.9 cTrader FIX read models (§52, §69.9–10)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/fix/sessions` | ReadOnly+ | QUOTE + TRADE cards. Host, SSL port, connected, loggedOn, status, last in/out, seq, reconnects, last heartbeat, errors. **Never password / SenderSubID secrets if broker-issued secrets.** |
| `GET` | `/api/v1/fix/quote` | ReadOnly+ | XAU mapped?, instrument ID, bid, ask, quote age, spread. |
| `GET` | `/api/v1/fix/trade` | ReadOnly+ | `executionEnabled` (must be false in v1), open orders/positions (likely empty), last ER, last reconciliation. |

No FIX logon/password/config POST from React in v1 (§55 + §59 “change broker/FIX configuration”).

### 4.10 Risk read + stop-new (§53, §40)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/risk/snapshot` | ReadOnly+ | Equity/balance/margin if known; daily P&L; drawdown; XAU long/short/net; by-trader; by-broker. Nulls allowed when venue unread. |
| `GET` | `/api/v1/risk/rejections` | ReadOnly+ | Rejected copy intents + reason codes (`PRICE_MOVED_TOO_FAR`, `QUOTE_STALE`, `SPREAD_TOO_WIDE`, martingale, …). |
| `GET` | `/api/v1/risk/kill-switch` | ReadOnly+ | `{ stopNewExecution, emergencyFlattenAvailable: false }` in v1. |
| `POST` | `/api/v1/risk/stop-new-execution` | RiskManager+ | Set/clear `STOP_NEW_EXECUTION`. Audited. Does not flatten. |

**Not in first useful version:** `POST /api/v1/risk/emergency-flatten` (SuperAdmin, confirm token). Return 404 or 409 “execution not enabled” if called.

### 4.11 Reconciliation (§54, §69 ingestion)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/reconciliation/runs` | ReadOnly+ | Latest MT5 + latest cTrader runs (cTrader may be `never`). |
| `GET` | `/api/v1/reconciliation/issues` | ReadOnly+ | Open issues: unknown external, missing internal, order/qty mismatch, orphan fill, unresolved execution. Empty list ≠ hidden. |

### 4.12 Audit (§46 Audit, §59)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/audit/logs` | RiskManager+ | Filter actor, action, from/to. SuperAdmin sees all. ReadOnly **denied**. |

### 4.13 Settings (read, no secrets) (§46 Settings, §55)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/settings/public` | ReadOnly+ | Non-secret flags: `realCopyExecutionEnabled` (false), `fixQuoteEnabled`, `fixTradeSessionEnabled`, XAU symbol map **names/IDs only**, risk limit **names + values that are already operational** (not credentials). |

No `PUT` settings that persist passwords. No raw env dump.

### 4.14 SignalR (stack §5; optional for first paint)

| Hub | Path | Roles | Purpose |
|---|---|---|---|
| `OpsHub` | `/hubs/ops` | ReadOnly+ | Push overview ticks, FIX quote, ingest health. **Same DTO redaction as REST.** |

REST polling of `/overview` + `/fix/quote` is enough to unblock first useful React. Hub is recommended, not a §69 gate.

### 4.15 Delete / never ship in v1

| Path | Why |
|---|---|
| `GET /weatherforecast` | Template. Remove. |
| `POST /api/v1/execution/enable` | Live copy. §69 / §41 / go-live §68. |
| `POST /api/v1/risk/emergency-flatten` | Needs TRADE + stronger auth. |
| `POST /api/v1/models/{id}/promote` | No ML. |
| `PUT /api/v1/brokers/{id}/credentials` | Secrets must not go React → API body. |
| `GET /api/v1/config` or `/debug/config` | Secret leak vector. |
| Swagger UI in production | Only if locked to SuperAdmin or disabled. |

---

## 5. Response-contract rules (bind these before the first controller)

1. **Allow-list DTOs only.** Never `return brokerOptions` / `IConfiguration` / EF entities that include secret columns.
2. **Mask** manager login (e.g. `2027` → `20**`) on §48 payloads.
3. **Omit** FIX password, account password, proxy user/pass, DB/Redis passwords, connection strings, `SenderSubID`/`TargetSubID` if those are treated as broker-issued secrets.
4. **MFE/MAE**: field present only when tick path made them valid; otherwise `null` + `mfeMaeValid: false`. Do not invent.
5. **ML fields** nullable until Phase 6.
6. **Live** allocation / dest real P&L may be `0` / `null` while execution is off — still typed on the DTO.
7. Structured logs: `correlation_id`, `broker_id`, `source_login` (§57). **Never log FIX/MT5 password tags.**

---

## 6. RBAC matrix for the v1 list

| Role | Read ops dashboards | Toggle group analysis | Trader Watch/Shadow/Pause | Stop-new-execution | Audit log | Flatten / live enable / FIX creds |
|---|---|---|---|---|---|---|
| ReadOnly | yes | no | no | no | no | no |
| Analyst | yes | yes | no | no | no | no |
| RiskManager | yes | yes | yes | yes | yes | no |
| SuperAdmin | yes | yes | yes | yes | yes | not in v1 |

Unauthenticated `/api/**` → 401. Wrong role → 403. Live-enable while flag false → 409.

---

## 7. Implementation notes (not done in this task)

When a later wave replaces the template, expected shape (do not hand-write business logic in `Program.cs`):

```text
apps/api/
  Program.cs                 # composition only
  Auth/                      # JWT or cookie BFF + policies
  Contracts/                 # allow-list DTOs
  Endpoints/ or Controllers/ # thin; call Application
  Hubs/OpsHub.cs             # optional
  Serialization/Redaction.cs
```

Application must grow real queries against the §45 tables (`brokers`, `mt5_groups`, `mt5_accounts`, `reconstructed_trades`, `trader_scores`, `trader_risk_flags`, `shadow_*`, `fix_sessions`, `destination_quotes`, `risk_decisions`, `execution_reconciliation_*`, `audit_logs`, `system_events`). Those tables and the workers are **not** implemented yet; API cannot be “done” before them. That is a dependency, not an excuse to keep weatherforecast.

Remove `WeatherForecast` record + `.http` sample + `launchUrl`.

Add `UserSecretsId` and a repo-root `.env.example` with `<SECRET>` placeholders only (§55–56).

Wire Serilog + request `correlation_id` before any FIX/MT5 options exist so passwords never hit console.

---

## 8. Severity list

| ID | Sev | Finding |
|---|---|---|
| A06-01 | **BLOCKER** | No dashboard API. §69.12 cannot be met. |
| A06-02 | **BLOCKER** | No auth/RBAC. Cannot expose ingestion/FIX/risk later. |
| A06-03 | **HIGH** | Anonymous `GET /weatherforecast` is the only route; replace, do not leave beside real routes. |
| A06-04 | **HIGH** | No DTO redaction / `.env.example` / secret store on API. First “helpful” config endpoint will violate §55. |
| A06-05 | **HIGH** | `AllowedHosts=*`, no CORS policy, no rate limit. |
| A06-06 | **MED** | Swashbuckle/Serilog/SignalR.Common referenced but dead. SignalR.Common is not a hub host. |
| A06-07 | **MED** | API has no `UserSecretsId`; workers do. Inconsistent secret story. |
| A06-08 | **MED** | Project refs empty Domain/Application/Infrastructure — composition fiction. |
| A06-09 | **LOW** | Swagger not registered (avoids an extra anonymous surface for now). |

---

## 9. Classification (arch §73-B)

| Component | Class |
|---|---|
| `apps/api` host / `net8` web project | EXISTS_NEEDS_REFACTOR |
| `Program.cs` weatherforecast | DEPRECATED — delete |
| Dashboard endpoints §§46–54 | MISSING |
| Auth + roles + audit §59 | MISSING |
| Secret-safe React contract §55 | MISSING (no leak **yet**) |
| SignalR ops hub | MISSING |
| Health/ready | MISSING |
| Application/Infrastructure usable by API | MISSING (stubs) |
| `apps/web` consumer | MISSING |

---

## 10. What this audit did **not** do

- Did not run `dotnet run` / hit `:5160`.
- Did not modify `apps/api` or any product source.
- Did not invent fake scores, MFE/MAE, or live P&L.
- Did not specify JWT vs cookie BFF vendor — either is fine if React never sees MT5/FIX/DB secrets.

**Bottom line:** treat `apps/api` as an empty host. The first useful version is a **read-mostly, secret-safe, RBAC-gated** query API for Overview / Brokers / Groups / Traders / Trader Detail / Shadow / FIX QUOTE / System Health / MT5 reconciliation, plus audited `PATCH` trader state, group analysis toggle, and `STOP_NEW_EXECUTION`. Everything else in the §46 nav (Models, Live Copy, Flatten, FIX credential edits) stays out until later phases.
