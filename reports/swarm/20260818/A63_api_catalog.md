# A63 — First Useful Version API Catalog (REST + SignalR)

| Field | Value |
|---|---|
| Agent | A63 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` |
| Scope | Binding **first useful** REST + SignalR surface. No product source modified. |
| Law | Architecture v2 §§5, 10, 22, 40–41, 46–55, 59, 69, 72.5 |
| Relates | A06 (host is empty), A20 (tables), A22–A25 (score/risk/shadow/FIX), A26 (full later dashboard), A28 (§69 12-item bar) |

This file is the **implementable catalog** for Phases 0–5 + React (§69). It is **not** the Phase 6–8 surface.

**Current measured host:** `D:\Prop\apps\api\Program.cs` is still the ASP.NET 8 weatherforecast template. **0** of the routes below exist. `GET /weatherforecast` is **out of v1** and must be deleted when this catalog is implemented.

**Non-normative sketch:** `apps/web/src/api/hooks.ts` calls unversioned `/api/*` and hub `/hubs/dashboard`. Those paths are **not** this catalog. First useful React must retarget `/api/v1` + `/hubs/ops`.

---

## 0. First useful bar this catalog must show (§69)

| # | Acceptance | API that proves it on the dashboard |
|---|---|---|
| 1 | Connect both MT5 brokers | `GET /brokers`, `GET /health` |
| 2 | Discover all groups | `GET /mt5/groups` |
| 3 | Sync ~5,000 accounts | `GET /mt5/accounts`, overview counts |
| 4–6 | Capture / reconstruct XAU / first-3 | `GET /trades`, `GET /traders/{brokerId}/{login}/trades` |
| 7–8 | Deterministic score + rank | `GET /traders`, `GET /scoring/summary`, score history |
| 9–10 | QUOTE FIX + Pepperstone XAU instrument ID | `GET /fix/sessions`, `GET /fix/quote` |
| 11 | Shadow-copy selected traders on dest quotes | `GET /shadow/*` + `PATCH` trader state |
| 12 | Show all of this in React | every GET below + `OpsHub` |

ML, live `NewOrderSingle`, model promotion, emergency flatten, and credential writes are **not** in this catalog.

---

## 1. Binding conventions

| Item | Contract |
|---|---|
| Base | `/api/v1` |
| Host (dev) | `http://localhost:5160` / `https://localhost:7294` (launchSettings today) |
| Auth | `Authorization: Bearer <access_token>` on **every** `/api/v1/**` and `/hubs/**` except login + liveness |
| Anonymous | `GET /health`, `POST /api/v1/auth/login` only |
| JSON | `application/json; charset=utf-8`, camelCase |
| Time | ISO-8601 UTC with `Z` |
| Identity | Source trader is always `{ brokerId, login }`. Login is **never** globally unique (§10) |
| Tickets / FIX ids | JSON **string** (do not use JS `number` for 64-bit tickets) |
| Money | JSON number, server `decimal(18,2)` |
| XAU qty | JSON number, server `decimal(18,8)`; never “lots = lots” |
| Pagination | `page` (1-based), `pageSize` (default 50, **max 200**) |
| Sort | `sort=field:asc\|desc` (repeatable) |
| Idempotency | mutating `POST`/`PATCH` accept `Idempotency-Key` (UUID) |
| Correlation | echo `X-Correlation-Id`; every error includes `correlationId` |
| Errors | see §1.2 |
| Audit | every mutation writes `audit_logs` (actor, role, action, entity, before/after, correlation_id). After-blob runs the secret sanitizer |
| Allow-list DTOs | never serialize EF entities, `IConfiguration`, `CTraderFixOptions`, or options objects |

### 1.1 Envelopes

List:

```json
{ "data": [], "page": 1, "pageSize": 50, "totalItems": 0, "totalPages": 0 }
```

Single / snapshot:

```json
{ "data": {} }
```

Mutation:

```json
{
  "data": {
    "accepted": true,
    "actionId": "00000000-0000-4000-8000-000000000001",
    "auditId": "00000000-0000-4000-8000-000000000002",
    "status": "APPLIED"
  }
}
```

### 1.2 Errors (never echo secrets, tokens, or connection strings)

```json
{
  "error": {
    "code": "FORBIDDEN",
    "message": "Role ReadOnly cannot change trader state.",
    "details": {},
    "correlationId": "00000000-0000-4000-8000-000000000003"
  }
}
```

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | Bad query / body |
| 401 | `UNAUTHENTICATED` | Missing / expired token |
| 403 | `FORBIDDEN` | Authenticated, role cannot |
| 404 | `NOT_FOUND` | Unknown broker / trader / issue |
| 409 | `CONFLICT` | Live state requested while execution off; stale version |
| 422 | `SECRET_FIELD_REJECTED` | Denylisted key in request |
| 423 | `KILL_SWITCH_ACTIVE` | Mutation blocked by `STOP_NEW_EXECUTION` (v1: only if we later add copy mutations) |
| 429 | `RATE_LIMITED` | Privileged-action throttle |
| 503 | `DEPENDENCY_UNAVAILABLE` | DB / Redis / workers down |

Any request body or query that contains a denylisted key (§2) → **422** and no persist.

### 1.3 Shared enums (wire values)

`healthStatus`: `HEALTHY` | `DEGRADED` | `UNHEALTHY` | `STALE` | `UNKNOWN`

`connectionStatus`: `CONNECTED` | `CONNECTING` | `DISCONNECTED` | `RECONNECTING` | `DISABLED`

`traderState` (domain `TraderState`, architecture §22):

```text
INSUFFICIENT_DATA
EARLY_SCORE
WATCH
SHADOW
LIVE_CANDIDATE
LIVE
PAUSED
RISK_BLOCKED
DISQUALIFIED
```

v1 `PATCH` state may set only: `WATCH` | `SHADOW` | `PAUSED` | `RISK_BLOCKED`.  
`LIVE` / `LIVE_CANDIDATE` → **409** while `REAL_COPY_EXECUTION_ENABLED=false`.

`featureQuality`: `EXACT` | `APPROXIMATE` | `UNAVAILABLE`

`fixSessionStatus` (maps domain `FixSessionStatus` + A25):

```text
DISABLED
CONNECTING
LOGON_SENT
LOGGED_ON
RECONCILING
READY_FOR_MARKET_DATA
READY_FOR_EXECUTION
BLOCKED_INCONSISTENT
LOGOUT_SENT
RECONNECTING
ERROR
DOWN
```

`riskDecision`: `APPROVE` | `REDUCE_SIZE` | `REJECT` | `PAUSE_TRADER` | `PAUSE_VENUE` | `GLOBAL_STOP`

`copyIntentAction`: `OPEN_EXPOSURE` | `INCREASE_EXPOSURE` | `REDUCE_EXPOSURE` | `CLOSE_EXPOSURE`

`reconciliationIssueType`:

```text
UNKNOWN_EXTERNAL_POSITION
MISSING_INTERNAL_POSITION
ORDER_MISMATCH
QUANTITY_MISMATCH
SIDE_MISMATCH
ORPHAN_FILL
ORPHAN_EXECUTION_REPORT
UNEXPECTED_FILL
UNRESOLVED_EXECUTION_STATE
```

`issueStatus`: `OPEN` | `ACKNOWLEDGED` | `RESOLVED` | `WONT_FIX_AUDITED`

`role`: `SuperAdmin` | `RiskManager` | `Analyst` | `ReadOnly`

---

## 2. No secrets (binding — §48, §52, §55, §72.5)

### 2.1 Never in JSON, SignalR, CSV, problem details, or `audit_logs.after`

| Class | Forbidden keys / values |
|---|---|
| MT5 Manager password | `password`, `mt5Password`, `MT5_PASSWORD` |
| MT5 manager login (raw, list views) | `managerLogin` — use `managerLoginMasked` only |
| Proxy credentials | `proxyUsername`, `proxyPassword`, `ACHIEVER_PROXY_USERNAME`, `ACHIEVER_PROXY_PASSWORD` |
| cTrader / FIX password | `fixPassword`, `CTRADER_FIX_PASSWORD`, FIX tag 96 / `RawData` |
| cTrader account password | any secret for destination login `1369850` |
| FIX SenderSubID if broker-issued | omit `senderSubId` entirely in v1 |
| Database / Redis | connection strings, `Password=` fragments, Redis AUTH |
| Vault / keys | `clientSecret`, `privateKey`, PEM/PKCS |
| Session secrets | refresh token in JSON; raw access token in GET bodies |

### 2.2 Safe operational fields

Allowed: display name, server host, port, SSL flag, **masked** manager login, group names, account counts, FIX host, SSL ports, `senderCompId`, `targetCompId`, session qualifier (`QUOTE`/`TRADE`), `secretConfigured: true|false`, `secretLastRotatedAt`, destination account **number** (not password), instrument IDs, bid/ask, health enums.

Manager login mask: keep **last two** digits, replace the rest with `*`. Example `2027` → `"**27"`. SuperAdmin may call `GET /brokers/{brokerId}?revealLogin=true` and receive **only** the integer login, never the password.

### 2.3 Server sanitizer (mandatory before serialize)

1. Drop any property whose name matches `(?i)(password|passwd|secret|pwd|rawdata|connectionstring|privatekey|proxyuser)`.
2. Redact `Password=` substrings.
3. Replace FIX tag 96 / authentication blobs with `"***"`.
4. Fail closed if a secret remains after sanitizer — do not send a half-redacted payload.

Dashboard is **not** a vault. No `PUT`/`PATCH` accepts credentials. Rotation is env / OS secret store / Vault only.

---

## 3. RBAC for v1

Unauthenticated `/api/v1/**` → 401. Wrong role → 403.

| Role | Read listed dashboards | Toggle group analysis | Trader Watch/Shadow/Pause | Stop-new-execution | Audit GET |
|---|---|---|---|---|---|
| ReadOnly | yes | no | no | no | no |
| Analyst | yes | yes | no | no | no |
| RiskManager | yes | yes | yes | yes | yes |
| SuperAdmin | yes | yes | yes | yes | yes |

`revealLogin=true` → SuperAdmin only.

**Not in v1 (404 or 409, never a working mutation):** enable real execution, emergency flatten, promote model, write FIX/MT5 credentials, change symbol mapping, change risk limits.

---

## 4. Supporting surface (required to operate the listed domains)

Not in the user’s domain list, but first useful React cannot function without them.

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `POST` | `/api/v1/auth/login` | anonymous | Issue access JWT. Refresh = httpOnly Secure SameSite=Strict cookie. Never echo password. |
| `POST` | `/api/v1/auth/logout` | any auth | Revoke refresh family. |
| `POST` | `/api/v1/auth/refresh` | refresh cookie | Rotate access token. |
| `GET` | `/api/v1/auth/me` | any auth | `{ id, email, displayName, role }` |
| `GET` | `/api/v1/overview` | ReadOnly+ | One snapshot for Overview + header strip (§47) |
| `GET` | `/api/v1/settings/public` | ReadOnly+ | Non-secret flags only |
| `GET` | `/api/v1/audit/logs` | RiskManager+ | Filter `actorId`, `action`, `from`, `to`. ReadOnly → 403 |

No public register. Seed SuperAdmin out of band.

### 4.1 `GET /api/v1/overview`

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:00:00.000Z",
    "accounts": {
      "totalMt5Accounts": 0,
      "connectedSourceBrokers": 0,
      "xauusdTraders": 0,
      "tradersWithMinThreeCompletedTrades": 0
    },
    "traderStates": {
      "watch": 0,
      "shadow": 0,
      "liveCandidates": 0,
      "liveCopied": 0,
      "riskBlocked": 0
    },
    "pnl": {
      "shadowPnl": 0.00,
      "destinationRealPnl": 0.00,
      "currency": "USD"
    },
    "exposure": {
      "canonicalSymbol": "XAUUSD",
      "grossQuantity": 0,
      "netQuantity": 0,
      "destinationFreeMargin": null,
      "destinationMarginLevel": null
    },
    "health": {
      "mt5Ingestion": { "status": "UNKNOWN", "detail": "not started" },
      "fixQuote": { "status": "UNKNOWN", "detail": "not started" },
      "fixTrade": { "status": "DISABLED", "detail": "not required for first useful" }
    },
    "flags": {
      "realCopyExecutionEnabled": false,
      "stopNewExecution": false,
      "emergencyFlattenAvailable": false
    }
  }
}
```

`destinationRealPnl` stays `0` while execution is off. Do not invent margin fields — `null` is honest.

### 4.2 `GET /api/v1/settings/public`

```json
{
  "data": {
    "realCopyExecutionEnabled": false,
    "fixQuoteEnabled": true,
    "fixTradeSessionEnabled": false,
    "shadowCopyEnabled": true,
    "canonicalSymbol": "XAUUSD",
    "xauMapped": false,
    "destinationInstrumentId": null
  }
}
```

No host passwords, no raw env dump, no `PUT`.

---

## 5. REST catalog — first useful domains

### 5.1 Health

| Method | Path | Auth | Purpose |
|---|---|---|---|
| `GET` | `/health` | anonymous | Process up. Body `{ "status": "ok" }` only. **No** inventory, versions of secrets, or dependency detail. |
| `GET` | `/health/ready` | anonymous | Postgres + Redis + last worker heartbeats. `{ "status": "ok"\|"degraded", "checks": [ { "name", "status", "ageMs" } ] }`. No connection strings. |
| `GET` | `/api/v1/health` | ReadOnly+ | System Health page (§46, §58). |
| `GET` | `/api/v1/system/events` | ReadOnly+ | Recent `system_events` (ingest / FIX / reconnect). Pageable. |

`GET /api/v1/health` shape:

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:00:00.000Z",
    "mt5": {
      "brokers": [
        {
          "brokerId": "…",
          "displayName": "Achiever",
          "connectionStatus": "DISCONNECTED",
          "reconnects": 0,
          "lastEventAt": null,
          "backfillLagSeconds": null,
          "pool": { "size": 8, "inUse": 0, "idle": 0 }
        }
      ],
      "eventsTotal": 0,
      "dealsTotal": 0,
      "duplicateDealsTotal": 0,
      "outboxBacklog": 0
    },
    "reconstruction": {
      "reconstructedTradesTotal": 0,
      "failuresTotal": 0
    },
    "scoring": {
      "lastRunAt": null,
      "baselineVersion": "baseline.v1",
      "shadowCandidates": 0
    },
    "fix": {
      "quote": { "status": "UNKNOWN", "loggedOn": false, "quoteAgeMs": null },
      "trade": { "status": "DISABLED", "loggedOn": false }
    },
    "dependencies": {
      "postgres": { "status": "UNKNOWN" },
      "redis": { "status": "UNKNOWN" }
    }
  }
}
```

---

### 5.2 Brokers (§48, §69.1)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/brokers` | ReadOnly+ | Achiever + StarwaveFX (+ future). |
| `GET` | `/api/v1/brokers/{brokerId}` | ReadOnly+ | Same row + last error **class** (no stack dumps, no connection strings). Query `revealLogin=true` SuperAdmin only. |

**No `PATCH` / `PUT` / credential write in v1.**

List item:

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "code": "ACHIEVER",
  "displayName": "Achiever",
  "connectionStatus": "DISCONNECTED",
  "server": "57.128.141.65",
  "port": 443,
  "serverName": "AchieverGlobalMarkets-Server",
  "useSsl": true,
  "managerLoginMasked": "**27",
  "mode": "local",
  "enabledForIngest": true,
  "groupCount": 0,
  "accountCount": 0,
  "dealIngestPerMinute": 0,
  "lastEventAt": null,
  "lastSuccessfulHistorySyncAt": null,
  "pool": { "size": 8, "inUse": 0, "idle": 8 },
  "reconnectCount": 0,
  "secretConfigured": true,
  "proxy": {
    "enabled": true,
    "host": "81.29.145.69",
    "port": 49527,
    "credentialConfigured": true
  }
}
```

**Forbidden:** `password`, `managerLogin` (unless SuperAdmin reveal), `proxyUsername`, `proxyPassword`, raw connection string.

Tables: `brokers`, `broker_connections` (A20). Domain record `Broker` has `ManagerLogin` — **do not** project the raw field.

---

### 5.3 Groups (+ accounts for the ~5k bar) (§49, §69.2–3)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/mt5/groups` | ReadOnly+ | Every discovered group. Not filtered to known plans (§9). |
| `PATCH` | `/api/v1/mt5/groups/{groupId}` | Analyst+ | Toggle `enabledForAnalysis` only. Audited (`GROUP_ANALYSIS_UPDATE`). |
| `GET` | `/api/v1/mt5/accounts` | ReadOnly+ | Paged (~5k). Filter `brokerId`, `groupId`, `q` (login contains). |

`GET /mt5/groups` query: `brokerId`, `enabledForAnalysis`, `plan`, `q`.

```json
{
  "data": [
    {
      "groupId": "b2222222-0000-4000-8000-000000000010",
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "brokerDisplayName": "Achiever",
      "group": "demo\\yo-2step",
      "currency": "USD",
      "accountCount": 0,
      "enabledForAnalysis": true,
      "planMapping": null,
      "lastDiscoveredAt": null,
      "lastSyncedAt": null
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 0,
  "totalPages": 0
}
```

`PATCH` body: `{ "enabledForAnalysis": false, "reason": "exclude challenge group" }`

Account row (no PII beyond login / group / snapshot numbers already stored):

```json
{
  "accountId": "…",
  "brokerId": "…",
  "login": 6100421,
  "groupId": "…",
  "group": "demo\\yo-2step",
  "balance": 0,
  "equity": 0,
  "leverage": 100,
  "lastSyncedAt": null
}
```

---

### 5.4 Traders (§50, §51, §69.7–8, §69.11)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/traders` | ReadOnly+ | Leaderboard. |
| `GET` | `/api/v1/traders/{brokerId}/{login}` | ReadOnly+ | Detail header + scores + flags. 404 if compound key missing. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/features` | ReadOnly+ | Baseline behavior features. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/risk-flags` | ReadOnly+ | Martingale / averaging-down / lot escalation / abnormal sizing. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/lot-timeline` | ReadOnly+ | Lot-size series. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/holding-times` | ReadOnly+ | Holding-time histogram. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/shadow` | ReadOnly+ | This trader’s shadow positions + P&L. |
| `PATCH` | `/api/v1/traders/{brokerId}/{login}/state` | RiskManager+ | Select Watch / Shadow / Paused / RiskBlocked. Audited. |

Leaderboard query (AND):

| Query | Type | Notes |
|---|---|---|
| `brokerId` | uuid | |
| `group` | string | exact group path |
| `state` | `traderState` | repeatable |
| `minEarlyScore` / `maxEarlyScore` | number | |
| `minRiskScore` / `maxRiskScore` | number | higher = riskier |
| `minCompletedXauTrades` | int | |
| `martingale` / `averagingDown` / `lotEscalation` | bool | |
| `scoredFrom` / `scoredTo` | datetime | |
| `q` | string | login contains |
| `sort` | string | default `earlyScore:desc` |

Leaderboard row:

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "brokerDisplayName": "Achiever",
  "login": 6100421,
  "group": "demo\\yo-2step",
  "completedXauTrades": 0,
  "netSourcePnl": 0,
  "earlyScore": null,
  "mlProbability": null,
  "riskScore": null,
  "behaviorScore": null,
  "flags": {
    "martingale": false,
    "averagingDown": false,
    "lotEscalation": false
  },
  "state": "INSUFFICIENT_DATA",
  "shadowPnl": 0,
  "liveAllocation": 0,
  "lastScoredAt": null
}
```

`mlProbability` is **always `null` in v1**. Do not stub fake ML.

Detail adds account snapshot, `firstThreeTradesHighlighted`, `mfe`/`mae` **only when** `featureQuality === EXACT` (else `null` + `UNAVAILABLE`). Never fabricate MFE/MAE from closed deals (§17).

`PATCH .../state`:

```json
{ "state": "SHADOW", "reason": "selected for shadow book" }
```

Forbidden target states in v1: `LIVE`, `LIVE_CANDIDATE` → 409. `INSUFFICIENT_DATA` / `EARLY_SCORE` / `DISQUALIFIED` are scorer-owned, not operator-set.

---

### 5.5 Trades (§14–15, §46 Trade Explorer, §69.4–6)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/trades` | ReadOnly+ | Cross-trader reconstructed XAU trades. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/trades` | ReadOnly+ | One trader’s reconstructed XAU history. Flag `isFirstThree`. |

Explorer query: `brokerId`, `login`, `from`, `to`, `side` (`LONG`/`SHORT` or `BUY`/`SELL`), `minPnl`, `completed` (default true), `canonicalSymbol` (default `XAUUSD`).

```json
{
  "reconstructedTradeId": "d3333333-0000-4000-8000-000000000100",
  "brokerId": "…",
  "login": 6100421,
  "positionTicket": "123456789",
  "sequence": 1,
  "isFirstThree": true,
  "side": "LONG",
  "canonicalSymbol": "XAUUSD",
  "sourceSymbol": "XAUUSDm",
  "volumeLots": 0.10,
  "openTime": "2026-07-01T08:15:00.000Z",
  "closeTime": "2026-07-01T10:02:00.000Z",
  "openPrice": 2320.40,
  "closePrice": 2325.10,
  "netSourcePnl": 47.00,
  "commission": 0,
  "swap": 0,
  "hadSl": true,
  "hadTp": false,
  "wasScaledIn": false,
  "wasPartialClose": false,
  "wasAveragedDown": false,
  "isCompleted": true,
  "dealCount": 2
}
```

Do **not** expose raw MT5 deals as the Trade Explorer primary list. Reconstruction is mandatory (§14). Volume is converted lots, not raw `ulong` units, plus conversion must be tested (A38).

---

### 5.6 Scores (§18, §22, §69.7–8, A22 `baseline.v1`)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/scoring/summary` | ReadOnly+ | Last run, scored count, baseline version, counts by state. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/scores` | ReadOnly+ | Current score + timeline. |

No `/models`, no promote, no weights, no training-set dump.

```json
{
  "data": {
    "baseline": {
      "name": "baseline.v1",
      "outputs": ["riskScore", "behaviorScore", "earlyQualityScore"]
    },
    "lastRunAt": null,
    "scoredCount": 0,
    "countsByState": {
      "INSUFFICIENT_DATA": 0,
      "EARLY_SCORE": 0,
      "WATCH": 0,
      "SHADOW": 0,
      "LIVE_CANDIDATE": 0,
      "LIVE": 0,
      "PAUSED": 0,
      "RISK_BLOCKED": 0,
      "DISQUALIFIED": 0
    }
  }
}
```

Score history item:

```json
{
  "at": "2026-07-01T10:02:01.000Z",
  "trigger": "TRADE_3_COMPLETE",
  "state": "EARLY_SCORE",
  "earlyQualityScore": 0.64,
  "behaviorScore": 0.61,
  "riskScore": 0.18,
  "completedXauTrades": 3,
  "mlProbability": null
}
```

Wire names match domain `TraderScore` / `TraderScoreHistory`: `riskScore`, `behaviorScore`, `earlyQualityScore`. Leaderboard `earlyScore` **is** `earlyQualityScore`.

---

### 5.7 Shadow (§24, §69.11, A24)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/shadow/portfolio` | ReadOnly+ | Aggregate P&L, open/closed counts, selected traders. |
| `GET` | `/api/v1/shadow/positions` | ReadOnly+ | Open (default) / closed shadow positions. Query `status=open\|closed\|all`, `brokerId`, `login`. |
| `GET` | `/api/v1/shadow/performance` | ReadOnly+ | Source vs shadow series (dest-quote priced). Query `from`, `to`, `grain=day\|hour`. |
| `GET` | `/api/v1/copy-intents` | ReadOnly+ | Worker-created intents. Filter `status`, `brokerId`, `login`. Read-only. |

Prices come from the cTrader **QUOTE** session, never source MT5 last-deal.

```json
{
  "data": {
    "pnl": 0,
    "currency": "USD",
    "openCount": 0,
    "closedCount": 0,
    "selectedTraderCount": 0,
    "quote": {
      "instrumentId": null,
      "bid": null,
      "ask": null,
      "quoteAgeMs": null,
      "healthy": false
    }
  }
}
```

Position item:

```json
{
  "shadowPositionId": "aa666666-0000-4000-8000-000000000400",
  "brokerId": "…",
  "login": 6100421,
  "reconstructedTradeId": "…",
  "canonicalSymbol": "XAUUSD",
  "side": "LONG",
  "quantity": 0.07,
  "entryPrice": 2398.20,
  "markPrice": 2401.10,
  "unrealizedPnl": 20.30,
  "quoteAgeMs": 180,
  "openedAt": "2026-08-18T10:01:00.000Z"
}
```

Copy-intent item (read-only; clients do not POST intents):

```json
{
  "copyIntentId": "…",
  "brokerId": "…",
  "login": 6100421,
  "reconstructedTradeId": "…",
  "action": "OPEN_EXPOSURE",
  "side": "LONG",
  "requestedQuantity": 0.05,
  "status": "SHADOWED",
  "riskDecision": "APPROVE",
  "reasonCode": null,
  "createdAt": "2026-08-18T10:01:00.100Z"
}
```

---

### 5.8 FIX sessions (§52, §69.9–10, A25)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/fix/sessions` | ReadOnly+ | QUOTE + TRADE cards. TRADE may be `DISABLED` / not started. |
| `GET` | `/api/v1/fix/quote` | ReadOnly+ | XAU mapped?, instrument ID, bid, ask, age, spread. |
| `GET` | `/api/v1/fix/trade` | ReadOnly+ | `executionEnabled` **must be false** in v1. Open orders/positions likely empty. |
| `GET` | `/api/v1/fix/sessions/{session}/events` | ReadOnly+ | Recent `fix_session_events`. `{session}` = `QUOTE` \| `TRADE`. **No raw Logon.** |

No FIX logon / password / config `POST` from React.

`GET /fix/sessions`:

```json
{
  "data": {
    "quote": {
      "session": "QUOTE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5211,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
      "sessionQualifier": "QUOTE",
      "connected": false,
      "loggedOn": false,
      "sessionStatus": "DOWN",
      "lastInboundAt": null,
      "lastOutboundAt": null,
      "sequence": { "nextSender": 0, "nextTarget": 0 },
      "reconnectCount": 0,
      "lastHeartbeatAt": null,
      "lastTestRequestAt": null,
      "errors": [],
      "xauusd": {
        "mapped": false,
        "instrumentId": null,
        "bid": null,
        "ask": null,
        "quoteAgeMs": null,
        "spread": null
      },
      "secretConfigured": true
    },
    "trade": {
      "session": "TRADE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5212,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
      "sessionQualifier": "TRADE",
      "connected": false,
      "loggedOn": false,
      "sessionStatus": "DISABLED",
      "lastInboundAt": null,
      "lastOutboundAt": null,
      "sequence": { "nextSender": 0, "nextTarget": 0 },
      "reconnectCount": 0,
      "lastHeartbeatAt": null,
      "lastTestRequestAt": null,
      "errors": [],
      "executionEnabled": false,
      "openOrders": 0,
      "openDestinationPositions": 0,
      "lastExecutionReport": { "at": null, "execType": null, "clOrdId": null, "ordStatus": null },
      "lastReconciliation": { "at": null, "status": "NEVER" },
      "secretConfigured": true
    }
  }
}
```

`instrumentId` is discovered from Security List (§30). Never hardcode.

**Never:** FIX password, `RawData`, account password, `senderSubId`.

---

### 5.9 Risk (§53, §40, A23)

First useful is **shadow-only**. Still expose a read snapshot (zeros / “execution off”) so the UI does not invent fields. `STOP_NEW_EXECUTION` is safe to persist even with send off.

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/risk/snapshot` | ReadOnly+ | Equity/margin if known; daily P&L; drawdown; XAU long/short/net; by-trader; by-broker. Nulls when unread. |
| `GET` | `/api/v1/risk/rejections` | ReadOnly+ | Rejected copy intents + reason codes. Pageable. |
| `GET` | `/api/v1/risk/kill-switch` | ReadOnly+ | `{ stopNewExecution, emergencyFlattenAvailable: false, realCopyExecutionEnabled: false }` |
| `POST` | `/api/v1/risk/stop-new-execution` | RiskManager+ | Set/clear `STOP_NEW_EXECUTION`. Does **not** flatten. Audited. |

`POST` body:

```json
{ "enabled": true, "reason": "spread regime" }
```

`reason` required, 3–500 chars.

Snapshot (honest empty / off):

```json
{
  "data": {
    "account": {
      "equity": null,
      "balance": null,
      "freeMargin": null,
      "marginLevel": null,
      "asOf": null,
      "available": false
    },
    "performance": { "dailyPnl": 0, "currentDrawdown": 0 },
    "xau": {
      "longQuantity": 0,
      "shortQuantity": 0,
      "netQuantity": 0,
      "grossQuantity": 0,
      "book": "SHADOW"
    },
    "riskByCopiedTrader": [],
    "riskBySourceBroker": [],
    "controls": {
      "stopNewExecution": false,
      "emergencyFlattenAvailable": false,
      "realCopyExecutionEnabled": false
    }
  }
}
```

Rejection `reasonCode` (A23 / §37 / §39):

```text
QUOTE_STALE
QUOTE_UNAVAILABLE
SPREAD_TOO_WIDE
PRICE_MOVED_TOO_FAR
MARTINGALE_BLOCK
ABNORMAL_SIZING
VENUE_UNHEALTHY
SIGNAL_STALE
LIMIT_GROSS
LIMIT_NET
LIMIT_DAILY_LOSS
STOP_NEW_EXECUTION
NOT_RECONCILED
INTENT_INCOMPLETE
EXECUTION_DISABLED
```

**Not in v1:** `POST /api/v1/risk/emergency-flatten`, `PATCH /api/v1/risk/limits`. Flatten → **404**. Web stub `useEmergencyFlatten` must not be wired until Phase 8.

---

### 5.10 Reconciliation (§54, §69 Phase 1 MT5 recon)

| Method | Path | Roles | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/reconciliation` | ReadOnly+ | Latest MT5 + latest cTrader run (cTrader may be `never`). Open-issue counts. |
| `GET` | `/api/v1/reconciliation/runs` | ReadOnly+ | Run history. Query `venue=MT5\|CTRADER`. |
| `GET` | `/api/v1/reconciliation/issues` | ReadOnly+ | Open issues. Query `type`, `status`, `venue`. Empty list ≠ hidden. |
| `POST` | `/api/v1/reconciliation/run` | RiskManager+ | Start an MT5 (default) run. Does not invent fills. Body `{ "venue": "MT5", "reason": "manual" }`. |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/ack` | RiskManager+ | Acknowledge. Body `{ "reason": "…" }`. |

```json
{
  "data": {
    "mt5": { "lastSuccessfulAt": null, "status": "UNKNOWN", "runId": null },
    "cTrader": { "lastSuccessfulAt": null, "status": "NEVER", "readyForExecution": false, "runId": null },
    "openIssueCounts": {
      "UNKNOWN_EXTERNAL_POSITION": 0,
      "MISSING_INTERNAL_POSITION": 0,
      "ORDER_MISMATCH": 0,
      "QUANTITY_MISMATCH": 0,
      "SIDE_MISMATCH": 0,
      "ORPHAN_FILL": 0,
      "ORPHAN_EXECUTION_REPORT": 0,
      "UNEXPECTED_FILL": 0,
      "UNRESOLVED_EXECUTION_STATE": 0
    }
  }
}
```

Issue item:

```json
{
  "issueId": "cc888888-0000-4000-8000-000000000600",
  "venue": "MT5",
  "type": "QUANTITY_MISMATCH",
  "status": "OPEN",
  "detectedAt": "2026-08-18T11:48:00.000Z",
  "brokerId": "…",
  "login": 6100421,
  "positionTicket": "123456789",
  "clOrdId": null,
  "internalQuantity": 0.10,
  "externalQuantity": 0.20,
  "note": "history vs reconstructed closed volume"
}
```

Nothing unresolved is dropped (§54). First useful **must** show MT5 reconciliation. cTrader TRADE recon may stay `NEVER` until Phase 7.

---

## 6. SignalR hubs

One hub for first useful. REST polling of `/overview` + `/fix/quote` + `/health` is enough to paint; the hub is **recommended**, not a §69 gate.

| Hub class | Path | Auth | Notes |
|---|---|---|---|
| `OpsHub` | `/hubs/ops` | ReadOnly+ (JWT via `Authorization` or negotiate `access_token`) | Same DTO redaction as REST. |

**Do not implement** `/hubs/dashboard` (web stub). Retarget the client.

No server methods that accept secrets. Client may `JoinGroup("overview")` / `JoinGroup("fix")` / `JoinGroup("traders")` if groups are used; otherwise broadcast.

### 6.1 Server → client events

| Event | Payload | Typical subscribers |
|---|---|---|
| `ops.header` | `{ flags, health }` subset of overview | shell |
| `overview.updated` | overview `data` | Overview |
| `broker.health` | one broker list row | Brokers, Health |
| `health.updated` | `/api/v1/health` `data` | System Health |
| `fix.session` | `{ session, loggedOn, sessionStatus, xauusd? }` | FIX, header |
| `quote.xauusd` | `{ instrumentId, bid, ask, quoteAgeMs, spread, at }` | FIX, Risk, Shadow |
| `trader.score` | `{ brokerId, login, state, earlyScore, riskScore, lastScoredAt }` | Leaderboard, Detail |
| `trader.state` | `{ brokerId, login, state }` | Leaderboard, Shadow |
| `shadow.portfolio` | `{ pnl, openCount }` | Shadow, Overview |
| `risk.state` | `{ stopNewExecution, emergencyFlattenAvailable: false, realCopyExecutionEnabled: false }` | Risk, header |
| `reconciliation.issue` | one issue object | Reconciliation |

Payloads are **subsets** of the GET DTOs. Never larger, never secret-bearing.

### 6.2 Client → server (optional)

| Method | Args | Purpose |
|---|---|---|
| `Subscribe` | `string[] topics` | `overview`, `brokers`, `fix`, `quotes`, `traders`, `shadow`, `risk`, `recon`, `health` |
| `Unsubscribe` | `string[] topics` | |

No `SendOrder`, no `SetPassword`, no raw FIX inject.

---

## 7. Endpoint index (v1 implement this set)

### 7.1 In scope

| Method | Path | Domain |
|---|---|---|
| `GET` | `/health` | Health |
| `GET` | `/health/ready` | Health |
| `GET` | `/api/v1/health` | Health |
| `GET` | `/api/v1/system/events` | Health |
| `POST` | `/api/v1/auth/login` | Auth |
| `POST` | `/api/v1/auth/logout` | Auth |
| `POST` | `/api/v1/auth/refresh` | Auth |
| `GET` | `/api/v1/auth/me` | Auth |
| `GET` | `/api/v1/overview` | Overview |
| `GET` | `/api/v1/settings/public` | Settings |
| `GET` | `/api/v1/audit/logs` | Audit |
| `GET` | `/api/v1/brokers` | Brokers |
| `GET` | `/api/v1/brokers/{brokerId}` | Brokers |
| `GET` | `/api/v1/mt5/groups` | Groups |
| `PATCH` | `/api/v1/mt5/groups/{groupId}` | Groups |
| `GET` | `/api/v1/mt5/accounts` | Groups |
| `GET` | `/api/v1/traders` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}/features` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}/risk-flags` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}/lot-timeline` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}/holding-times` | Traders |
| `GET` | `/api/v1/traders/{brokerId}/{login}/shadow` | Shadow |
| `PATCH` | `/api/v1/traders/{brokerId}/{login}/state` | Traders |
| `GET` | `/api/v1/trades` | Trades |
| `GET` | `/api/v1/traders/{brokerId}/{login}/trades` | Trades |
| `GET` | `/api/v1/scoring/summary` | Scores |
| `GET` | `/api/v1/traders/{brokerId}/{login}/scores` | Scores |
| `GET` | `/api/v1/shadow/portfolio` | Shadow |
| `GET` | `/api/v1/shadow/positions` | Shadow |
| `GET` | `/api/v1/shadow/performance` | Shadow |
| `GET` | `/api/v1/copy-intents` | Shadow |
| `GET` | `/api/v1/fix/sessions` | FIX |
| `GET` | `/api/v1/fix/quote` | FIX |
| `GET` | `/api/v1/fix/trade` | FIX |
| `GET` | `/api/v1/fix/sessions/{session}/events` | FIX |
| `GET` | `/api/v1/risk/snapshot` | Risk |
| `GET` | `/api/v1/risk/rejections` | Risk |
| `GET` | `/api/v1/risk/kill-switch` | Risk |
| `POST` | `/api/v1/risk/stop-new-execution` | Risk |
| `GET` | `/api/v1/reconciliation` | Reconciliation |
| `GET` | `/api/v1/reconciliation/runs` | Reconciliation |
| `GET` | `/api/v1/reconciliation/issues` | Reconciliation |
| `POST` | `/api/v1/reconciliation/run` | Reconciliation |
| `POST` | `/api/v1/reconciliation/issues/{issueId}/ack` | Reconciliation |
| Hub | `/hubs/ops` | SignalR |

**Count:** 46 HTTP routes + 1 hub. Mutations: login/logout/refresh, group analysis, trader state, stop-new, recon run, recon ack.

### 7.2 Out of first useful (do not ship)

| Path | Why |
|---|---|
| `GET /weatherforecast` | Template. Delete. |
| `POST /api/v1/risk/emergency-flatten` | Needs TRADE + confirm + SuperAdmin/RiskManager; Phase 8 |
| `PATCH /api/v1/risk/limits` | Optional later; not a §69 item |
| `PATCH /api/v1/settings/execution` | Live enable; §41 / §68 / §70 |
| `PUT /api/v1/settings` | Web stub; secret-leak vector |
| `GET /api/v1/models` + promote | Phase 6 |
| `GET /api/v1/live/portfolio` | Live copy; execution off |
| `PUT /api/v1/brokers/{id}/credentials` | Secrets must not go React → API |
| `PATCH /api/v1/settings/fix` | Config via env/secret store |
| `GET /api/v1/config` / `/debug/config` | Secret leak |
| `/hubs/dashboard` | Non-normative stub name |
| Swagger UI in production | Disable or SuperAdmin-only |

---

## 8. Page → API matrix (first useful React)

| React route (target) | Primary GET | Extra | Mutation |
|---|---|---|---|
| `/overview` | `/overview` | hub `overview.updated` | none |
| `/brokers` | `/brokers` | `/brokers/{id}`, hub `broker.health` | none |
| `/groups` | `/mt5/groups` | `/mt5/accounts` | `PATCH` analysis |
| `/traders` | `/traders` | hub `trader.score` | none |
| `/traders/:brokerId/:login` | `/traders/{brokerId}/{login}` | trades, scores, features, flags, lot, holding, shadow | `PATCH` state |
| `/trades` | `/trades` | | none |
| `/scoring` | `/scoring/summary` | | none |
| `/shadow` | `/shadow/portfolio` | positions, performance, copy-intents | via trader state |
| `/fix` | `/fix/sessions` | `/fix/quote`, `/fix/trade`, events | none |
| `/risk` | `/risk/snapshot` | rejections, kill-switch | `POST` stop-new |
| `/reconciliation` | `/reconciliation` | runs, issues | run, ack |
| `/health` | `/health` (v1) | `/system/events` | none |

---

## 9. Audit action names (v1)

```text
LOGIN_SUCCESS
LOGIN_FAILURE
LOGOUT
GROUP_ANALYSIS_UPDATE
TRADER_STATE_WATCH
TRADER_STATE_SHADOW
TRADER_STATE_PAUSE
TRADER_STATE_RISK_BLOCKED
STOP_NEW_EXECUTION_ENABLE
STOP_NEW_EXECUTION_DISABLE
RECONCILIATION_RUN
RECONCILIATION_ISSUE_ACK
SECRET_FIELD_REJECTED
```

`LOGIN_FAILURE` stores email + reason code only. Never the attempted password.

---

## 10. Implementation notes (do not treat as done)

1. `apps/api` today: `MapGet("/weatherforecast")` only. Package refs (Swashbuckle, Serilog, SignalR.Common) are unused. Need the framework SignalR host, not Common alone.
2. Project only **allow-list** columns. `CTraderFixOptions.Password` and `Broker.ManagerLogin` must never be selected into a dashboard query.
3. Application layer queries A20 tables: `brokers`, `mt5_groups`, `mt5_accounts`, `reconstructed_trades`, `trader_scores`, `trader_score_history`, `trader_risk_flags`, `shadow_*`, `fix_sessions`, `destination_quotes`, `risk_decisions`, `copy_intents`, `execution_reconciliation_*`, `audit_logs`, `system_events`.
4. `REAL_COPY_EXECUTION_ENABLED` default **false**. v1 DTOs still include the flag typed as `false`.
5. React Query keys: `['overview']`, `['brokers']`, `['traders', filters]`, `['trader', brokerId, login]`, `['trades', filters]`, `['scoring']`, `['shadow']`, `['fix']`, `['risk']`, `['reconciliation']`, `['health']`. Invalidate from hub events.
6. CORS: allow the Vite origin only. Do not leave `AllowedHosts=*` + anonymous dashboard.
7. This catalog does **not** authorize editing `apps/api` or `apps/web`. Implementation is a later coding task.

---

## 11. Acceptance for this catalog

- [x] Health, brokers, groups, traders, trades, scores, shadow, FIX sessions, risk, reconciliation each have REST routes.
- [x] One SignalR hub (`/hubs/ops`) with secret-safe events.
- [x] No response shape includes a password, proxy credential, FIX RawData, DB, or Redis secret.
- [x] Trader identity is always `brokerId` + `login`.
- [x] First useful mutations are only: auth, group analysis, trader shadow/watch/pause, stop-new, recon run/ack.
- [x] Flatten, live enable, models, credential writes are explicitly out.

*End of A63. Product source was not modified.*
