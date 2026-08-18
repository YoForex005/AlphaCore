# A26 — React Dashboard Pages, API Contracts, and RBAC

**Artifact:** `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md`  
**Date:** 2026-08-18  
**Status:** Specification only. No product source was modified.  
**Source of truth:** `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`

| Architecture section | Used for |
|---|---|
| §5 Frontend / API stack | React + ASP.NET Core + SignalR contract surface |
| §10 Multi-broker identity | Compound keys (`brokerId` + `login` / tickets) |
| §16–18, §22–24 | Trader detail, scores, states, shadow fields |
| §39–43 | Risk, kill switches, reconciliation |
| §44–45 | Durable entities the API projects |
| **§46 React Dashboard** | Navigation / page inventory |
| **§47 Overview** | Overview widgets + `GET /overview` |
| **§48 Brokers** | Brokers page + secret-safe broker DTO |
| **§49 MT5 Groups** | Groups page + filters |
| **§50 Trader Leaderboard** | Traders list + query contract |
| **§51 Trader Detail** | Trader detail resources |
| **§52 cTrader FIX** | FIX session cards (password never returned) |
| **§53 Risk Dashboard** | Risk snapshot + privileged actions |
| **§54 Reconciliation** | Reconciliation runs / issues |
| §55 Security | Secret denylist (never send to React) |
| **§59 Authentication and RBAC** | Roles + privileged-action matrix |
| §72.5 / §72.19 | Never expose secrets to the browser; audit overrides |

This document is the implementable contract for `/apps/web` (React/TS/Vite) talking to `/apps/api` (ASP.NET Core). Current API is a stub (`GET /weatherforecast` only). These routes do not exist yet.

---

## 1. Scope

Specify:

1. React pages from §46–§54 (and the remaining §46 nav items needed to operate the dashboard).
2. HTTP + SignalR API contracts with explicit JSON shapes.
3. The §59 RBAC matrix.
4. A hard **never-send-secrets** rule for every response, hub payload, log, and audit detail.

Out of scope: implementation, OpenAPI generation, MQ5, FIX engine internals, ML training APIs beyond what the Models page needs.

---

## 2. Shared conventions

### 2.1 Transport

| Item | Contract |
|---|---|
| API base | `/api/v1` |
| Auth | `Authorization: Bearer <access_token>` on every `/api/v1/**` call except `POST /auth/login` and `GET /health/live` |
| Content type | `application/json; charset=utf-8` |
| Time | ISO-8601 UTC with `Z` (`2026-08-18T12:04:00.123Z`) |
| Field names | camelCase JSON (ASP.NET default) |
| Money / PnL | JSON `number`, server `decimal(18,2)` unless noted |
| Quantity (XAU) | JSON `number`, server `decimal(18,8)` |
| Scores / probabilities | JSON `number` in `[0, 1]` unless noted as `0–100` |
| Compound trader id | path `/traders/{brokerId}/{login}` — login is never globally unique (§10) |
| Tickets / FIX ids | **string** (64-bit / alphanumeric; do not use JS number) |
| Pagination | `page` (1-based), `pageSize` (default 50, max 200) |
| Sort | `sort=field:asc\|desc` (repeatable) |
| Idempotency | all state-changing POSTs accept `Idempotency-Key` (UUID) |
| Correlation | API echoes `X-Correlation-Id`; every error includes `correlationId` |

### 2.2 Success / error envelopes

List:

```json
{
  "data": [],
  "page": 1,
  "pageSize": 50,
  "totalItems": 0,
  "totalPages": 0
}
```

Single resource:

```json
{
  "data": {}
}
```

Mutation accepted:

```json
{
  "data": {
    "accepted": true,
    "actionId": "8f2c1a0e-3b77-4d91-9c1a-2e6f0b8d4a11",
    "auditId": "c91e0d44-0a1b-4c2d-9e3f-112233445566",
    "status": "APPLIED"
  }
}
```

Error (never include submitted secrets, tokens, or connection strings):

```json
{
  "error": {
    "code": "FORBIDDEN",
    "message": "Role ReadOnly cannot request EMERGENCY_FLATTEN.",
    "details": {},
    "correlationId": "b7c1e2d3-0000-4000-8000-000000000001"
  }
}
```

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | Bad query / body |
| 401 | `UNAUTHENTICATED` | Missing/expired token |
| 403 | `FORBIDDEN` | Authenticated but role cannot |
| 404 | `NOT_FOUND` | Unknown broker/trader/run |
| 409 | `CONFLICT` | Stale version / already applied |
| 412 | `PRECONDITION_FAILED` | Reconciliation not `READY_FOR_EXECUTION` |
| 422 | `SECRET_FIELD_REJECTED` | Client sent a denylisted secret field |
| 423 | `KILL_SWITCH_ACTIVE` | Action blocked by `STOP_NEW_EXECUTION` |
| 429 | `RATE_LIMITED` | Privileged-action throttle |
| 503 | `DEPENDENCY_UNAVAILABLE` | DB / Redis / workers down |

### 2.3 Common enums

`healthStatus`: `HEALTHY` | `DEGRADED` | `UNHEALTHY` | `STALE` | `UNKNOWN`

`connectionStatus`: `CONNECTED` | `CONNECTING` | `DISCONNECTED` | `RECONNECTING` | `DISABLED`

`traderState` (§22):

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

Overview counts (§47) roll these up as: Watch, Shadow, Live candidates, Live copied, Risk blocked.

`riskDecision`: `APPROVE` | `REDUCE_SIZE` | `REJECT` | `PAUSE_TRADER` | `PAUSE_VENUE` | `GLOBAL_STOP` (§39)

`copyIntentStatus`: `PENDING` | `APPROVED` | `REDUCED` | `REJECTED` | `EXPIRED` | `EXECUTED` | `SHADOWED`

`reconciliationIssueType` (§43, §54):

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

`featureQuality` (§17): `EXACT` | `APPROXIMATE` | `UNAVAILABLE`

---

## 3. Never send secrets

Hard law from §48 (“No secret values”), §52 (“Never show FIX password”), §55, §59, §72.5, §72.7.

### 3.1 Denylist — never in any JSON, SignalR frame, CSV export, or audit `after` blob

| Class | Examples (never keys, never values) |
|---|---|
| MT5 Manager passwords | `mt5Password`, `password`, `Password`, `MT5_PASSWORD` |
| MT5 proxy credentials | `proxyUsername`, `proxyPassword`, `ACHIEVER_PROXY_*` |
| cTrader / FIX passwords | `fixPassword`, `CTRADER_FIX_PASSWORD`, RawData / tag 96 |
| cTrader account password | any account-login secret for 1369850 |
| Database / Redis | connection strings, `Password=` fragments, Redis AUTH |
| Vault / cloud | `clientSecret`, `privateKey`, PEM/PKCS material |
| Session secrets | refresh-token raw value in GET bodies; FIX `RawData` |
| Auth tags | FIX Logon password fields (also never logged, §57) |

### 3.2 Allowed non-secret operational fields

Safe to show: broker display name, server host, port, SSL flag, **masked** manager login, group names, account counts, FIX host, SSL ports, SenderCompId / TargetCompId / session qualifier, `secretConfigured: true|false`, `secretLastRotatedAt` if the secret store exposes rotation metadata.

Manager login masking (§48): return `managerLoginMasked` only. Algorithm: keep last two digits, replace the rest with `*`. Example login `2027` → `"**27"`. Never return the raw manager login in list views. SuperAdmin may request `GET /brokers/{id}?revealLogin=true` which still returns **only the login integer**, never the password.

### 3.3 Write path

The dashboard **is not a secret vault**.

- Settings PATCH **must not** accept password / proxy-credential / FIX-password fields.
- If any denylisted key appears in a request body or query string, reject with `422 SECRET_FIELD_REJECTED` and do not persist.
- Secret rotation happens via environment variables, OS secret store, or Vault (§55). `.env.example` stays placeholder-only.
- `GET` responses never echo a write-only secret because write-only secrets are not accepted.

### 3.4 Response sanitizer (server-side, mandatory)

Before serialize:

1. Drop any property whose name matches `(?i)(password|passwd|secret|pwd|rawdata|connectionstring|privatekey|proxyuser)`.
2. Redact values matching `Password=` in strings.
3. Replace FIX tag 96 / authentication blobs with `"***"`.
4. Fail the request closed if a secret is detected after sanitizer (do not send a half-redacted payload that still contains the secret).

---

## 4. Authentication

`POST /api/v1/auth/login`

Request:

```json
{
  "email": "ops@example.com",
  "password": "user-dashboard-password"
}
```

The **user’s** dashboard password is accepted only on this endpoint. It is never logged, never written to `audit_logs.detail`, and never returned.

Response `200`:

```json
{
  "data": {
    "accessToken": "<jwt>",
    "expiresAt": "2026-08-18T13:04:00.000Z",
    "user": {
      "id": "3a0c9b12-1111-4aaa-8bbb-0123456789ab",
      "email": "ops@example.com",
      "displayName": "Ops",
      "role": "RiskManager"
    }
  }
}
```

Refresh token: **httpOnly, Secure, SameSite=Strict** cookie. Never put the refresh token in JSON.

`POST /api/v1/auth/refresh` — cookie in, new access token out (same user object).  
`POST /api/v1/auth/logout` — revoke refresh family.  
`GET /api/v1/auth/me` — current user object only.

`role` is exactly one of (§59):

```text
SuperAdmin
RiskManager
Analyst
ReadOnly
```

JWT claims: `sub`, `email`, `role`, `jti`, `exp`. Do not put broker FIX credentials or MT5 secrets in claims.

---

## 5. React application

### 5.1 Stack (§5)

React + TypeScript + Vite + React Router + TanStack Query. SignalR for live tiles. ECharts or Recharts for score / lot / holding-time charts. Zustand only for ephemeral UI (selected filters, confirm-modal state).

Suggested tree (does not exist yet; do not create from this spec):

```text
/apps/web
  /src
    /pages          # one folder per §46 nav item
    /api            # typed fetchers matching this contract
    /auth           # role gates
    /components
    /hubs           # SignalR subscriptions
```

### 5.2 Route map (§46)

| Route | Page | Architecture |
|---|---|---|
| `/login` | Login | §59 |
| `/overview` | Overview | §47 |
| `/brokers` | Brokers | §48 |
| `/mt5-groups` | MT5 Groups | §49 |
| `/traders` | Trader Leaderboard | §50 |
| `/traders/:brokerId/:login` | Trader Detail | §51 |
| `/trades` | Trade Explorer | §46 |
| `/scoring` | Scoring | §46, §18, §22 |
| `/models` | Models | §46, §21 |
| `/shadow` | Shadow Portfolio | §46, §24 |
| `/live` | Live Copy Portfolio | §46, §32–§35 |
| `/fix` | cTrader FIX | §52 |
| `/risk` | Risk Dashboard | §53 |
| `/reconciliation` | Reconciliation | §54 |
| `/health` | System Health | §46, §58 |
| `/audit` | Audit | §46, §59 |
| `/settings` | Settings (non-secret) | §46, §41, §55 |

Default authenticated landing: `/overview`. Unknown routes → `/overview`.

### 5.3 Shell

Left nav labels **exactly** as §46. Each item visible to all authenticated roles. **Action buttons** on a page are hidden or disabled per the RBAC matrix (section 10); hiding is UX only — the API is the authority.

Header strip (all pages): `REAL_COPY_EXECUTION_ENABLED`, `STOP_NEW_EXECUTION`, FIX QUOTE health, FIX TRADE health, MT5 ingestion health. Source: `GET /overview` summary + hub `ops.header`.

Destructive actions (`EMERGENCY_FLATTEN`, enable real execution, promote model) require a typed-confirm modal (`type the action name`) plus `confirmPhrase` in the POST body.

---

## 6. Page + API contracts

### 6.1 Overview — `GET /api/v1/overview`  (§47)

Roles: all authenticated.

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:00:00.000Z",
    "accounts": {
      "totalMt5Accounts": 5120,
      "connectedSourceBrokers": 2,
      "xauusdTraders": 1840,
      "tradersWithMinThreeCompletedTrades": 612
    },
    "traderStates": {
      "watch": 220,
      "shadow": 80,
      "liveCandidates": 12,
      "liveCopied": 4,
      "riskBlocked": 19
    },
    "pnl": {
      "shadowPnl": 12450.25,
      "destinationRealPnl": 0.00,
      "currency": "USD"
    },
    "exposure": {
      "canonicalSymbol": "XAUUSD",
      "grossQuantity": 12.4,
      "netQuantity": 3.1,
      "destinationFreeMargin": 84210.55,
      "destinationMarginLevel": 1240.5
    },
    "health": {
      "mt5Ingestion": { "status": "HEALTHY", "detail": "both brokers connected" },
      "fixQuote": { "status": "HEALTHY", "detail": "logged on" },
      "fixTrade": { "status": "DEGRADED", "detail": "logged on; real execution disabled" }
    },
    "flags": {
      "realCopyExecutionEnabled": false,
      "stopNewExecution": false,
      "emergencyFlattenAvailable": true
    }
  }
}
```

No broker passwords, no FIX password, no destination account password.

---

### 6.2 Brokers — `GET /api/v1/brokers`  (§48)

Roles: all authenticated.

```json
{
  "data": [
    {
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "displayName": "Achiever",
      "connectionStatus": "CONNECTED",
      "server": "57.128.141.65",
      "port": 443,
      "serverName": "AchieverGlobalMarkets-Server",
      "useSsl": true,
      "managerLoginMasked": "**27",
      "mode": "local",
      "groupCount": 42,
      "accountCount": 3900,
      "dealIngestPerMinute": 18.4,
      "lastEventAt": "2026-08-18T11:59:58.010Z",
      "lastSuccessfulHistorySyncAt": "2026-08-18T11:55:00.000Z",
      "pool": {
        "size": 8,
        "inUse": 3,
        "idle": 5
      },
      "reconnectCount": 2,
      "secretConfigured": true,
      "proxy": {
        "enabled": true,
        "host": "81.29.145.69",
        "port": 49527,
        "credentialConfigured": true
      }
    }
  ]
}
```

**Forbidden on this DTO:** `password`, `managerLogin` (raw), `proxyUsername`, `proxyPassword`.

`GET /api/v1/brokers/{brokerId}` — same object.  
`PATCH /api/v1/brokers/{brokerId}` — SuperAdmin only; body may include `displayName`, `enabledForIngest`, `poolSize`. No secrets.

---

### 6.3 MT5 Groups — `GET /api/v1/mt5/groups`  (§49)

Query: `brokerId`, `enabledForAnalysis`, `plan`, `q` (group name contains).

```json
{
  "data": [
    {
      "groupId": "b2222222-0000-4000-8000-000000000010",
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "brokerDisplayName": "Achiever",
      "group": "demo\\yo-2step",
      "accountCount": 810,
      "enabledForAnalysis": true,
      "planMapping": "2STEP_DEMO",
      "lastDiscoveredAt": "2026-08-18T06:00:00.000Z",
      "lastSyncedAt": "2026-08-18T11:55:00.000Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 80,
  "totalPages": 2
}
```

`planMapping` is nullable. Discovery is not limited to known plan maps (§9).

`PATCH /api/v1/mt5/groups/{groupId}` — SuperAdmin or RiskManager:

```json
{ "enabledForAnalysis": false }
```

---

### 6.4 Trader Leaderboard — `GET /api/v1/traders`  (§50)

Filters (all optional, AND):

| Query | Type | Notes |
|---|---|---|
| `brokerId` | uuid | |
| `group` | string | exact group path |
| `state` | `traderState` | repeatable |
| `minEarlyScore` / `maxEarlyScore` | number | |
| `minRiskScore` / `maxRiskScore` | number | higher = riskier |
| `minCompletedXauTrades` | int | |
| `martingale` | bool | |
| `averagingDown` | bool | |
| `lotEscalation` | bool | |
| `scoredFrom` / `scoredTo` | datetime | last scored |
| `q` | string | login contains |
| `sort` | string | default `earlyScore:desc` |

```json
{
  "data": [
    {
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "brokerDisplayName": "Achiever",
      "login": 6100421,
      "group": "demo\\yo-2step",
      "completedXauTrades": 7,
      "netSourcePnl": 1840.50,
      "earlyScore": 0.71,
      "mlProbability": null,
      "riskScore": 0.22,
      "flags": {
        "martingale": false,
        "averagingDown": true,
        "lotEscalation": false
      },
      "state": "SHADOW",
      "shadowPnl": 162.10,
      "liveAllocation": 0.00,
      "lastScoredAt": "2026-08-18T11:40:00.000Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 612,
  "totalPages": 13
}
```

`mlProbability` is `null` until a promoted model exists. Do not invent it.

---

### 6.5 Trader Detail — `GET /api/v1/traders/{brokerId}/{login}`  (§51)

404 if the compound key does not exist.

```json
{
  "data": {
    "identity": {
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "brokerDisplayName": "Achiever",
      "login": 6100421,
      "group": "demo\\yo-2step",
      "state": "SHADOW"
    },
    "accountOverview": {
      "balance": 10120.00,
      "equity": 10080.40,
      "currency": "USD",
      "leverage": 100,
      "asOf": "2026-08-18T11:59:00.000Z"
    },
    "completedXauTrades": 7,
    "firstThreeTradesHighlighted": true,
    "scores": {
      "earlyScore": 0.71,
      "behaviorScore": 0.68,
      "riskScore": 0.22,
      "mlProbability": null,
      "lastScoredAt": "2026-08-18T11:40:00.000Z"
    },
    "riskFlags": {
      "martingale": false,
      "averagingDown": true,
      "lotEscalation": false,
      "abnormalSizing": false
    },
    "behaviorFeatures": {
      "holdingTimeSecondsP50": 1840,
      "holdingTimeSecondsP90": 7200,
      "slUseRate": 0.86,
      "tpUseRate": 0.40,
      "drawdown": 420.00,
      "mfe": 35.2,
      "mae": 18.1,
      "mfeMae": {
        "priceSource": "ACHIEVER_MT5_TICKS",
        "featureQuality": "EXACT"
      }
    },
    "shadow": {
      "openPositions": 1,
      "pnl": 162.10
    },
    "live": {
      "openPositions": 0,
      "pnl": 0.00,
      "allocation": 0.00
    }
  }
}
```

If MFE/MAE cannot be computed honestly (§17), send `mfe`/`mae` as `null` and `featureQuality: "UNAVAILABLE"`. Never fabricate.

#### Sub-resources

`GET /api/v1/traders/{brokerId}/{login}/trades` — reconstructed XAU history.

```json
{
  "data": [
    {
      "reconstructedTradeId": "d3333333-0000-4000-8000-000000000100",
      "sequence": 1,
      "isFirstThree": true,
      "side": "BUY",
      "volume": 0.10,
      "openTime": "2026-07-01T08:15:00.000Z",
      "closeTime": "2026-07-01T10:02:00.000Z",
      "openPrice": 2320.40,
      "closePrice": 2325.10,
      "sourcePnl": 47.00,
      "hadSl": true,
      "hadTp": false,
      "canonicalSymbol": "XAUUSD",
      "sourceSymbol": "XAUUSDm"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 7,
  "totalPages": 1
}
```

`GET .../score-history`

```json
{
  "data": [
    {
      "at": "2026-07-01T10:02:01.000Z",
      "trigger": "TRADE_3_COMPLETE",
      "state": "EARLY_SCORE",
      "earlyScore": 0.64,
      "riskScore": 0.18,
      "mlProbability": null
    }
  ]
}
```

`GET .../lot-timeline` → `{ "data": [ { "at": "...", "volume": 0.10, "side": "BUY" } ] }`  
`GET .../holding-time` → `{ "data": { "bucketsSeconds": [60, 300, 900, 1800, 3600, 14400], "counts": [0, 1, 2, 3, 1, 0] } }`  
`GET .../shadow-positions` / `GET .../live-positions` — see §6.9 / §6.10 item shape.  
`GET .../source-destination-map` (§35, §51):

```json
{
  "data": [
    {
      "reconstructedTradeId": "d3333333-0000-4000-8000-000000000100",
      "copyIntentId": "e4444444-0000-4000-8000-000000000200",
      "executionIntentId": null,
      "destinationOrderIds": [],
      "destinationPositionIds": [],
      "mode": "SHADOW"
    }
  ]
}
```

`POST /api/v1/traders/{brokerId}/{login}/copy-control` — pause / resume (§59). SuperAdmin or RiskManager.

```json
{ "action": "PAUSE", "reason": "Manual review" }
```

`action`: `PAUSE` | `RESUME`. Resulting state `PAUSED` or prior non-blocked state. Always writes `audit_logs`.

---

### 6.6 Trade Explorer — `GET /api/v1/trades`  (§46)

Query: `brokerId`, `login`, `from`, `to`, `side`, `minPnl`, `canonicalSymbol` (default `XAUUSD`). Same item shape as trader trades list.

---

### 6.7 Scoring — `GET /api/v1/scoring/summary`  (§18, §22, §46)

```json
{
  "data": {
    "baseline": {
      "name": "deterministic-v1",
      "outputs": ["riskScore", "behaviorScore", "earlyQualityScore"]
    },
    "lastRunAt": "2026-08-18T11:40:00.000Z",
    "countsByState": {
      "INSUFFICIENT_DATA": 4200,
      "EARLY_SCORE": 400,
      "WATCH": 220,
      "SHADOW": 80,
      "LIVE_CANDIDATE": 12,
      "LIVE": 4,
      "PAUSED": 6,
      "RISK_BLOCKED": 19,
      "DISQUALIFIED": 9
    }
  }
}
```

No model weights, no training-set PII dumps.

---

### 6.8 Models — `GET /api/v1/models`  (§21, §46)

```json
{
  "data": [
    {
      "modelVersionId": "f5555555-0000-4000-8000-000000000300",
      "name": "xgb-xau-early3",
      "status": "TRAINED",
      "createdAt": "2026-08-10T00:00:00.000Z",
      "metrics": {
        "top1PctNetPnl": null,
        "top5PctNetPnl": null,
        "beatsBaselineOutOfSample": false
      },
      "promotedAt": null
    }
  ]
}
```

`status`: `TRAINED` | `EVALUATED` | `PROMOTED` | `RETIRED`.

`POST /api/v1/models/{modelVersionId}/promote` — **SuperAdmin only**. Body:

```json
{ "confirmPhrase": "PROMOTE_MODEL", "reason": "beats baseline on held-out set" }
```

Automated self-promotion is forbidden (§71). Promotion is audited.

---

### 6.9 Shadow Portfolio — `GET /api/v1/shadow/portfolio`  (§24, §46)

```json
{
  "data": {
    "pnl": 1840.22,
    "openCount": 6,
    "closedCount": 140,
    "positions": [
      {
        "shadowPositionId": "aa666666-0000-4000-8000-000000000400",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "login": 6100421,
        "canonicalSymbol": "XAUUSD",
        "side": "BUY",
        "quantity": 0.07,
        "entryPrice": 2398.20,
        "markPrice": 2401.10,
        "unrealizedPnl": 20.30,
        "quoteAgeMs": 180,
        "openedAt": "2026-08-18T10:01:00.000Z"
      }
    ]
  }
}
```

Prices come from the cTrader **QUOTE** session, not source MT5 last-deal.

---

### 6.10 Live Copy Portfolio — `GET /api/v1/live/portfolio`  (§32–§35, §46)

Same shape as shadow, plus:

```json
{
  "data": {
    "realCopyExecutionEnabled": false,
    "pnl": 0.00,
    "openCount": 0,
    "positions": [
      {
        "destinationPositionId": "123456789",
        "copyIntentId": "e4444444-0000-4000-8000-000000000200",
        "executionIntentId": "bb777777-0000-4000-8000-000000000500",
        "clOrdId": "TI-20260818-000123",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "login": 6100421,
        "canonicalSymbol": "XAUUSD",
        "side": "BUY",
        "quantity": 0.05,
        "entryPrice": 2398.40,
        "unrealizedPnl": 4.10,
        "state": "FILLED"
      }
    ]
  }
}
```

When `REAL_COPY_EXECUTION_ENABLED=false`, `positions` may be empty; still return the flag.

---

### 6.11 cTrader FIX — `GET /api/v1/fix/sessions`  (§52)

Two cards, independent session state (§27–§28).

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
      "connected": true,
      "loggedOn": true,
      "sessionStatus": "LOGGED_ON",
      "lastInboundAt": "2026-08-18T12:00:00.200Z",
      "lastOutboundAt": "2026-08-18T12:00:00.050Z",
      "sequence": { "nextSender": 10442, "nextTarget": 18801 },
      "reconnectCount": 1,
      "lastHeartbeatAt": "2026-08-18T11:59:55.000Z",
      "lastTestRequestAt": null,
      "errors": [],
      "xauusd": {
        "mapped": true,
        "instrumentId": "185",
        "bid": 2401.12,
        "ask": 2401.28,
        "quoteAgeMs": 140,
        "spread": 0.16
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
      "connected": true,
      "loggedOn": true,
      "sessionStatus": "LOGGED_ON",
      "lastInboundAt": "2026-08-18T11:58:10.000Z",
      "lastOutboundAt": "2026-08-18T11:58:10.020Z",
      "sequence": { "nextSender": 2201, "nextTarget": 2198 },
      "reconnectCount": 0,
      "lastHeartbeatAt": "2026-08-18T11:59:50.000Z",
      "lastTestRequestAt": null,
      "errors": [],
      "executionEnabled": false,
      "openOrders": 0,
      "openDestinationPositions": 0,
      "lastExecutionReport": {
        "at": null,
        "execType": null,
        "clOrdId": null,
        "ordStatus": null
      },
      "lastReconciliation": {
        "at": "2026-08-18T11:50:00.000Z",
        "status": "READY_FOR_EXECUTION"
      },
      "secretConfigured": true
    }
  }
}
```

**Never** include FIX password, RawData, or account password. `instrumentId` is discovered from Security List (§30), not hardcoded from another account.

`sessionStatus`: `DOWN` | `CONNECTING` | `LOGGED_ON` | `LOGGED_OUT` | `RECONNECTING` | `SEQUENCE_RESET` | `ERROR`

`GET /api/v1/fix/sessions/{session}/events?page=&pageSize=` — recent `fix_session_events` (no raw Logon with password).

---

### 6.12 Risk Dashboard — `GET /api/v1/risk/dashboard`  (§53, §39–§40)

```json
{
  "data": {
    "account": {
      "equity": 25000.00,
      "balance": 25000.00,
      "freeMargin": 84210.55,
      "marginLevel": 1240.5,
      "asOf": "2026-08-18T12:00:00.000Z",
      "available": true
    },
    "performance": {
      "dailyPnl": 0.00,
      "currentDrawdown": 0.00
    },
    "xau": {
      "longQuantity": 2.10,
      "shortQuantity": 0.40,
      "netQuantity": 1.70,
      "grossQuantity": 2.50
    },
    "riskByCopiedTrader": [
      {
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "login": 6100421,
        "netQuantity": 0.50,
        "allocatedNotional": 1200.00,
        "unrealizedPnl": 12.40
      }
    ],
    "riskBySourceBroker": [
      {
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "displayName": "Achiever",
        "netQuantity": 1.70,
        "traderCount": 4
      }
    ],
    "rejectedIntents": [
      {
        "copyIntentId": "e4444444-0000-4000-8000-000000000201",
        "at": "2026-08-18T09:12:00.000Z",
        "reasonCode": "QUOTE_STALE",
        "reason": "quote_age exceeded max_quote_age",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "login": 7001888
      }
    ],
    "controls": {
      "stopNewExecution": false,
      "emergencyFlattenAvailable": true,
      "realCopyExecutionEnabled": false,
      "limits": {
        "maxLossPerSelectedTrader": 250.00,
        "maxDailyExecutionAccountLoss": 500.00,
        "maxPortfolioDrawdown": 1000.00,
        "maxXauGross": 10.0,
        "maxXauNet": 5.0,
        "maxPositionQuantity": 1.0,
        "maxOpenPositions": 20,
        "maxAllowedSpread": 0.80,
        "maxQuoteAgeMs": 1500,
        "maxSourceSignalAgeMs": 3000,
        "maxSlippage": 0.50
      }
    }
  }
}
```

`reasonCode` examples (§37, §39): `QUOTE_STALE` | `SPREAD_TOO_WIDE` | `PRICE_MOVED_TOO_FAR` | `MARTINGALE_BLOCK` | `ABNORMAL_SIZING` | `VENUE_UNHEALTHY` | `SIGNAL_STALE` | `LIMIT_GROSS` | `LIMIT_NET` | `LIMIT_DAILY_LOSS` | `STOP_NEW_EXECUTION` | `NOT_RECONCILED`.

#### Privileged risk mutations (§40, §59)

`POST /api/v1/risk/stop-new-execution` — SuperAdmin, RiskManager

```json
{ "enabled": true, "reason": "spread regime" }
```

Does **not** close existing positions.

`POST /api/v1/risk/emergency-flatten` — SuperAdmin, RiskManager (stronger confirm)

```json
{
  "confirmPhrase": "EMERGENCY_FLATTEN",
  "reason": "uncontrolled destination exposure"
}
```

Server must re-check role, require `confirmPhrase` exact match, rate-limit, and audit. This attempts to close destination positions. It is not the same control as `STOP_NEW_EXECUTION`.

`PATCH /api/v1/risk/limits` — SuperAdmin, RiskManager. Body is a partial of `controls.limits`. Optimistic concurrency: `If-Match` etag from GET.

---

### 6.13 Reconciliation — `GET /api/v1/reconciliation`  (§54)

```json
{
  "data": {
    "mt5": {
      "lastSuccessfulAt": "2026-08-18T11:55:00.000Z",
      "status": "HEALTHY"
    },
    "cTrader": {
      "lastSuccessfulAt": "2026-08-18T11:50:00.000Z",
      "status": "DEGRADED",
      "readyForExecution": false
    },
    "openIssueCounts": {
      "UNKNOWN_EXTERNAL_POSITION": 1,
      "MISSING_INTERNAL_POSITION": 0,
      "ORDER_MISMATCH": 0,
      "QUANTITY_MISMATCH": 2,
      "SIDE_MISMATCH": 0,
      "ORPHAN_FILL": 0,
      "ORPHAN_EXECUTION_REPORT": 0,
      "UNEXPECTED_FILL": 0,
      "UNRESOLVED_EXECUTION_STATE": 1
    },
    "issues": [
      {
        "issueId": "cc888888-0000-4000-8000-000000000600",
        "type": "UNRESOLVED_EXECUTION_STATE",
        "status": "OPEN",
        "detectedAt": "2026-08-18T11:48:00.000Z",
        "clOrdId": "TI-20260818-000099",
        "destinationPositionId": null,
        "internalQuantity": 0.05,
        "externalQuantity": null,
        "note": "NOS sent; disconnect before ER"
      }
    ]
  }
}
```

Nothing unresolved is dropped (§54). `GET /api/v1/reconciliation/issues` supports `type`, `status`, paging.

`POST /api/v1/reconciliation/issues/{issueId}/ack` — SuperAdmin, RiskManager.  
`POST /api/v1/reconciliation/run` — SuperAdmin, RiskManager; starts a run; does not invent fills.

While cTrader is not `READY_FOR_EXECUTION`, new live entries return `412 PRECONDITION_FAILED`.

---

### 6.14 System Health — `GET /api/v1/health`  (§46, §58)

Authenticated. Projects worker metrics without host credentials.

```json
{
  "data": {
    "mt5": {
      "connected": true,
      "reconnects": 3,
      "eventsTotal": 90122,
      "dealsTotal": 44012,
      "duplicateDealsTotal": 18,
      "backfillLagSeconds": 2.1,
      "outboxBacklog": 0
    },
    "reconstruction": {
      "reconstructedTradesTotal": 12044,
      "failuresTotal": 3
    },
    "scoring": {
      "requestsTotal": 800,
      "failuresTotal": 0,
      "shadowCandidates": 80,
      "liveCandidates": 12
    },
    "fix": {
      "quoteConnected": true,
      "tradeConnected": true,
      "logonFailures": 0,
      "reconnects": 1,
      "unknownExecutionStates": 1
    }
  }
}
```

`GET /api/v1/health/live` — unauthenticated `{ "status": "ok" }` for orchestrators. No inventory.

---

### 6.15 Audit — `GET /api/v1/audit`  (§59)

Roles: SuperAdmin, RiskManager, Analyst. ReadOnly → 403.

Query: `actorId`, `action`, `from`, `to`, `entityType`, `entityId`.

```json
{
  "data": [
    {
      "auditId": "c91e0d44-0a1b-4c2d-9e3f-112233445566",
      "at": "2026-08-18T10:00:00.000Z",
      "actorId": "3a0c9b12-1111-4aaa-8bbb-0123456789ab",
      "actorEmail": "ops@example.com",
      "role": "RiskManager",
      "action": "STOP_NEW_EXECUTION_ENABLE",
      "entityType": "risk_controls",
      "entityId": "global",
      "reason": "spread regime",
      "correlationId": "b7c1e2d3-0000-4000-8000-000000000009",
      "before": { "stopNewExecution": false },
      "after": { "stopNewExecution": true }
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 1,
  "totalPages": 1
}
```

`before` / `after` run through the same secret sanitizer. A password change in Vault never appears here as a value.

Every row in the privileged-action list (§59) **must** produce an audit row or the mutation is rolled back.

---

### 6.16 Settings — `GET /api/v1/settings`  (§41, §55, §46)

Non-secret runtime flags and mappings only.

```json
{
  "data": {
    "execution": {
      "ctraderFixEnabled": true,
      "ctraderFixQuoteEnabled": true,
      "ctraderFixTradeSessionEnabled": true,
      "realCopyExecutionEnabled": false
    },
    "fixNonSecret": {
      "host": "live-us-eqx-01.p.c-trader.com",
      "quoteSslPort": 5211,
      "tradeSslPort": 5212,
      "quoteSenderCompId": "live.pepperstone.1369850",
      "tradeSenderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
      "useSsl": true,
      "passwordConfigured": true
    },
    "symbolMappings": [
      {
        "mappingId": "dd999999-0000-4000-8000-000000000700",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "sourceSymbol": "XAUUSDm",
        "canonicalSymbol": "XAUUSD"
      }
    ]
  }
}
```

`PATCH /api/v1/settings/execution` — **SuperAdmin only**

```json
{
  "realCopyExecutionEnabled": true,
  "confirmPhrase": "ENABLE_REAL_EXECUTION",
  "reason": "gates in §68 complete"
}
```

Default remains `false` (§41). Enabling also requires risk-engine healthy + cTrader `READY_FOR_EXECUTION`; otherwise `412`.

`PATCH /api/v1/settings/symbol-mappings` — SuperAdmin only. Array upsert of `{ mappingId?, brokerId, sourceSymbol, canonicalSymbol, destinationInstrumentId? }`. Never a place for FIX passwords.

`PATCH /api/v1/settings/fix` — SuperAdmin only. Non-secret fields: host, ports, SSL flag, sender/target ids, enabled flags. **Password rejected (422).**

---

## 7. SignalR

Hub: `/hubs/ops` (JWT on query `access_token` or negotiate header). Same RBAC as GET of the corresponding page.

| Event | Payload (subset of the GET DTO) | Typical subscribers |
|---|---|---|
| `ops.header` | `{ flags, health }` from overview | shell |
| `overview.updated` | overview `data` | Overview |
| `broker.health` | one broker row | Brokers |
| `fix.session` | `{ session, loggedOn, sessionStatus, xauusd? }` | FIX, header |
| `quote.xauusd` | `{ instrumentId, bid, ask, quoteAgeMs, spread, at }` | FIX, Risk, Shadow |
| `risk.state` | `{ stopNewExecution, emergencyFlattenAvailable, realCopyExecutionEnabled }` | Risk, header |
| `reconciliation.issue` | one issue object | Reconciliation |
| `trader.score` | `{ brokerId, login, state, earlyScore, lastScoredAt }` | Leaderboard, Detail |

Hub payloads obey the secret sanitizer. No Logon RawData, no passwords.

---

## 8. Copy / risk / execution read APIs used by multiple pages

`GET /api/v1/copy-intents?status=&brokerId=&login=`

```json
{
  "data": [
    {
      "copyIntentId": "e4444444-0000-4000-8000-000000000200",
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "login": 6100421,
      "reconstructedTradeId": "d3333333-0000-4000-8000-000000000100",
      "side": "BUY",
      "requestedQuantity": 0.05,
      "status": "SHADOWED",
      "riskDecision": "APPROVE",
      "reasonCode": null,
      "expiresAt": "2026-08-18T10:01:05.000Z",
      "maxSignalAgeMs": 3000,
      "createdAt": "2026-08-18T10:01:00.100Z"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 1,
  "totalPages": 1
}
```

Clients do not create copy intents. Workers do. The dashboard is read-only here except pause/resume and kill switches.

---

## 9. React page → endpoint matrix

| Page | Primary GET | Additional | Mutations |
|---|---|---|---|
| Overview | `/overview` | hub `overview.updated` | none |
| Brokers | `/brokers` | `/brokers/{id}` | `PATCH /brokers/{id}` |
| MT5 Groups | `/mt5/groups` | | `PATCH /mt5/groups/{id}` |
| Traders | `/traders` | | none |
| Trader Detail | `/traders/{brokerId}/{login}` | trades, score-history, lot-timeline, holding-time, positions, map | `POST .../copy-control` |
| Trade Explorer | `/trades` | | none |
| Scoring | `/scoring/summary` | | none |
| Models | `/models` | | `POST /models/{id}/promote` |
| Shadow Portfolio | `/shadow/portfolio` | | none |
| Live Copy Portfolio | `/live/portfolio` | `/copy-intents` | none |
| cTrader FIX | `/fix/sessions` | `/fix/sessions/{s}/events` | none (config via Settings) |
| Risk | `/risk/dashboard` | | stop-new, flatten, limits |
| Reconciliation | `/reconciliation` | `/reconciliation/issues` | ack, run |
| System Health | `/health` | | none |
| Audit | `/audit` | | none |
| Settings | `/settings` | | execution, mappings, FIX non-secret |

---

## 10. RBAC matrix (§59)

Legend: **R** = read, **W** = mutate, **—** = 403.

### 10.1 Pages (GET)

| Resource | SuperAdmin | RiskManager | Analyst | ReadOnly |
|---|---|---|---|---|
| Overview, Brokers, Groups, Traders, Detail, Trades, Scoring, Shadow, Live, FIX, Risk, Health, Settings (GET) | R | R | R | R |
| Models GET | R | R | R | R |
| Audit GET | R | R | R | — |
| `revealLogin=true` on broker | R | — | — | — |

### 10.2 Privileged actions (must be authorized **and** audited)

These are the eight §59 verbs, plus the adjacent mutations this dashboard needs.

| Action | Endpoint | SuperAdmin | RiskManager | Analyst | ReadOnly |
|---|---|---|---|---|---|
| Enable real execution | `PATCH /settings/execution` (`realCopyExecutionEnabled=true`) | W | — | — | — |
| Disable real execution | same (`false`) | W | W | — | — |
| Change risk limits | `PATCH /risk/limits` | W | W | — | — |
| Pause / resume trader copying | `POST /traders/{brokerId}/{login}/copy-control` | W | W | — | — |
| Change symbol mapping | `PATCH /settings/symbol-mappings` | W | — | — | — |
| Activate / clear `STOP_NEW_EXECUTION` | `POST /risk/stop-new-execution` | W | W | — | — |
| Request `EMERGENCY_FLATTEN` | `POST /risk/emergency-flatten` | W | W | — | — |
| Promote a model | `POST /models/{id}/promote` | W | — | — | — |
| Change broker / FIX **non-secret** configuration | `PATCH /brokers/{id}`, `PATCH /settings/fix` | W | — | — | — |
| Toggle group `enabledForAnalysis` | `PATCH /mt5/groups/{id}` | W | W | — | — |
| Ack / run reconciliation | `POST /reconciliation/**` | W | W | — | — |
| Any secret field in a body | all | **422** | **422** | **422** | **422** |

Analyst and ReadOnly have **no** write verbs. UI must not offer those buttons; API still enforces.

### 10.3 Flatten vs stop-new (§40)

| Control | Closes positions? | Confirm phrase | Roles |
|---|---|---|---|
| `STOP_NEW_EXECUTION` | No | not required (reason required) | SuperAdmin, RiskManager |
| `EMERGENCY_FLATTEN` | Yes (attempt) | `EMERGENCY_FLATTEN` | SuperAdmin, RiskManager |
| `REAL_COPY_EXECUTION_ENABLED` | No (gate only) | `ENABLE_REAL_EXECUTION` when turning on | SuperAdmin on; SuperAdmin or RiskManager off |

Do not conflate these three in one toggle.

---

## 11. Frontend role gates (normative)

```text
can(role, action):
  SuperAdmin  → all W in §10.2 except secret fields
  RiskManager → stop-new, flatten, limits, pause/resume, disable-live, group analysis, recon
  Analyst     → GET including audit; no W
  ReadOnly    → GET excluding audit; no W
```

`useRole()` reads `GET /auth/me`. Never trust a role stored only in localStorage.

---

## 12. Audit action names (stable strings)

```text
LOGIN_SUCCESS
LOGIN_FAILURE
LOGOUT
STOP_NEW_EXECUTION_ENABLE
STOP_NEW_EXECUTION_DISABLE
EMERGENCY_FLATTEN_REQUEST
RISK_LIMITS_UPDATE
TRADER_COPY_PAUSE
TRADER_COPY_RESUME
SYMBOL_MAPPING_UPDATE
REAL_EXECUTION_ENABLE
REAL_EXECUTION_DISABLE
MODEL_PROMOTE
BROKER_CONFIG_UPDATE
FIX_CONFIG_UPDATE
GROUP_ANALYSIS_UPDATE
RECONCILIATION_RUN
RECONCILIATION_ISSUE_ACK
SECRET_FIELD_REJECTED
```

`LOGIN_FAILURE` stores email + reason code only. Never the attempted password.

---

## 13. Implementation notes (API team)

1. ASP.NET `JsonSerializer` contract resolver should register the secret-name denylist as a converter, not rely on DTO authors remembering.
2. EF projections for `broker_connections` / `fix_sessions` must **select** non-secret columns. Do not load password columns into memory for dashboard queries.
3. `REAL_COPY_EXECUTION_ENABLED` is both env default and a runtime flag; runtime cannot invent a password.
4. All privileged POSTs require `reason` (string, 3–500 chars) except where `confirmPhrase` already documents intent; flatten and enable-live require both.
5. Correlation id is required on every mutation and is stored on `audit_logs`.
6. React Query keys: `['overview']`, `['brokers']`, `['traders', filters]`, `['trader', brokerId, login]`, `['fix']`, `['risk']`, `['reconciliation']`. Invalidate from SignalR events, do not poll secrets.
7. This spec does not authorize creating `/apps/web` or controllers. It is the contract those artifacts must implement later.

---

## 14. Acceptance checks for this spec

- [ ] Every §46 nav item has a route and a primary GET.
- [ ] §47–§54 widget lists are represented in JSON (no silent omission).
- [ ] §59 four roles and eight privileged verbs appear in the matrix.
- [ ] No response shape includes a password, proxy credential, FIX RawData, DB, or Redis secret.
- [ ] `EMERGENCY_FLATTEN` and `STOP_NEW_EXECUTION` are separate endpoints.
- [ ] Trader identity is always `brokerId` + `login`.
- [ ] Unresolved reconciliation issues remain queryable until terminal audited status.

---

*End of A26. Product source was not modified.*
