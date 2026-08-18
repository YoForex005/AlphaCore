# A95 — Risk dashboard DTO including kill-switch state (Architecture §53)

**Artifact:** `D:\Prop\reports\swarm\20260818\A95_risk_page_dto.md`  
**Date:** 2026-08-18  
**Agent:** Grok Build subagent A95  
**Product source modified:** **none** (this file only)  
**Status:** Binding read-model / JSON contract for the Risk page. Specification only — do not treat current `RiskDashboardDto` as this contract.

| Field | Value |
|---|---|
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§53** (page widgets); **§39–§41**, **§55**, **§57**, **§59** |
| HTTP / envelope | A26 (path, camelCase, envelopes, secret denylist) |
| Kill-switch semantics | **A48** (two independent controls; flatten is a run, not a bool) |
| Engine reason codes | A23 |
| First-useful split sketch | A06 / A63 (`/risk/snapshot`) — **same `data` object**, not a second shape |
| UI file (later) | A62 `pages/risk/RiskDashboardPage.tsx` route `/risk` |
| RBAC | A51 + A48 (flatten = SuperAdmin + step-up). A26 §10.2 “RiskManager may flatten” is **superseded**. |

This document owns the **GET body** the Risk page is allowed to render, including a complete kill-switch state object. Mutations stay in A48. Limits write stays in A26 `PATCH /api/v1/risk/limits`. Cluster rollups stay out (A60 V1-5).

---

## 0. Verdict

Architecture §53 is a **display contract**. It names fifteen facts the Risk page must show. It does **not** authorize a single `string KillSwitch`, a mutually exclusive `KillSwitchMode`, or a boolean that means both “flatten is available” and “flatten is running.”

| §53 widget | Binding JSON path (this file) |
|---|---|
| Execution account equity if available | `data.account.equity` + `data.account.available` |
| Balance | `data.account.balance` |
| Free margin | `data.account.freeMargin` |
| Margin level | `data.account.marginLevel` |
| Daily P&L | `data.performance.dailyPnl` |
| Current drawdown | `data.performance.currentDrawdown` |
| XAU long quantity | `data.xau.longQuantity` |
| XAU short quantity | `data.xau.shortQuantity` |
| Net XAU exposure | `data.xau.netQuantity` |
| (implied gross, useful on the same card) | `data.xau.grossQuantity` |
| Risk by copied trader | `data.riskByCopiedTrader[]` |
| Risk by source broker | `data.riskBySourceBroker[]` |
| Rejected copy intents | `data.rejectedIntents[]` |
| Reasons for rejection | `data.rejectedIntents[].reasonCode` + `.reason` |
| `STOP_NEW_EXECUTION` **state** | `data.killSwitch.stopNewExecution` **plus** version / actor / time / reason / source |
| `EMERGENCY_FLATTEN` **availability** | `data.killSwitch.emergencyFlattenAvailable` **plus** `flattenPhase` / `emergencyFlattenAllowedForMe` |

**Two independent indicators. Never one traffic light.**

Honest measured tree (2026-08-18):

| Item | Path / evidence | Class |
|---|---|---|
| Architecture §53 list | `Architecture_v2.md` L1953–1978 | **LAW** |
| A26 sketch | `controls.stopNewExecution` + `emergencyFlattenAvailable` booleans only | **INCOMPLETE** vs A48 |
| A48 snapshot | full two-control object on `GET /api/v1/risk/kill-switch` | **SEMANTICS BINDING** |
| Application DTO | `src/Application/Dashboard/DashboardModels.cs` `RiskDashboardDto` — `string KillSwitch` | **§40 VIOLATION** |
| Query | `src/Infrastructure/Dashboard/EfDashboardQueries.cs` `GetRiskAsync` — zeros + `Mode.ToString()` + `None` default | **UNSAFE / STUB** |
| Entity | `Domain/Entities/KillSwitch.cs` exclusive `KillSwitchMode` | **UNSAFE if treated as SoT** |
| API route | `apps/api/Program.cs` weatherforecast only | **MISSING** |
| Web types | `apps/web/src/types/index.ts` `RiskStatus.emergencyFlatten: boolean` | **CONFLATED** |
| Web hook | `hooks.ts` `GET /api/risk/status` | **STALE PATH** |
| Risk page module | `apps/web/src/pages/` empty; `App.tsx` imports `RiskPage` | **MISSING FILE** |
| Tests of this DTO | `tests/` — none | **MISSING** |

This file is the contract a later coding wave must implement. It does not implement.

---

## 1. Precedence (do not re-litigate)

When siblings disagree, implementers use this order **for the Risk page read model**:

1. Architecture **§53 / §40 / §55 / §59** — what must appear; two controls; no secrets.
2. **A48** — stop-new latch + flatten **phase machine**; fail-closed empty row; who may flatten.
3. **This file (A95)** — exact JSON, null vs 0, role redaction, SignalR, TypeScript/C# names.
4. **A26** — `GET /api/v1/risk/dashboard` path, success/error envelopes, pagination, secret denylist.
5. **A23** — `reasonCode` catalog and which engine outcome produced the reject row.
6. **A63 / A06** — first-useful may ship zeros / `available:false` / flatten unavailable; path alias only.
7. Current C# / React stubs — **replace**, do not grow.

Resolved conflicts:

| Topic | Wrong / stale | Binding |
|---|---|---|
| GET path | stub `/api/risk/status`; A06/A63 `/risk/snapshot` as a *different* shape | **`GET /api/v1/risk/dashboard`**. Alias `GET /api/v1/risk/snapshot` **must return the identical `data` object**. |
| Kill-switch field | `string KillSwitch` / `KillSwitchMode.ToString()` / `None` | Nested `data.killSwitch` (A48 snapshot **embedded**) |
| Flatten in GET | `emergencyFlatten: boolean` or A26 two-bool `controls` | `flattenPhase` + desk `emergencyFlattenAvailable` + caller `emergencyFlattenAllowedForMe` |
| No kill row | Current code → `"None"` (implies safe / off) | Treat as **`stopNewExecution: true`**, `stopNewSource: "boot_fail_closed"` (A48 §3.3) |
| Flatten role | A26 / A62: RiskManager may flatten | **SuperAdmin + step-up only** (A48 §8, A51) |
| Flatten mutation shape | A26 single `POST .../emergency-flatten` + phrase `EMERGENCY_FLATTEN` | A48 request / confirm / abort / ack. GET never returns a raw confirm token. |
| A26 `data.controls` bag | two booleans + limits | **Superseded.** Limits live at `data.limits`. Kill state lives at `data.killSwitch`. Do not emit a second writable copy. |
| Cluster / ML | unused keys “for later” | **Forbidden** until that phase is scheduled (A60, A52) |
| Real-copy flag | treated as a kill switch | Display-only on this page (`killSwitch.realCopyExecutionEnabled`). Enable/disable is Settings (A49). |

---

## 2. Transport

| Item | Contract |
|---|---|
| Method / path | `GET /api/v1/risk/dashboard` |
| Alias (first useful) | `GET /api/v1/risk/snapshot` → **same handler, same body** |
| Focused sibling | `GET /api/v1/risk/kill-switch` → `{ "data": <same as data.killSwitch> }` (A48 §10.1) |
| Auth | Bearer on every call. Unauthenticated → 401 `UNAUTHENTICATED`. |
| Policy | `Risk.KillSwitch.Read` / page GET: **ReadOnly+** (all four §59 roles). |
| Envelope | A26 single-resource `{ "data": { … } }` |
| Content type | `application/json; charset=utf-8` |
| Field names | camelCase |
| Time | ISO-8601 UTC with `Z` |
| Money / P&L | JSON number, server `decimal(18,2)` |
| Quantity (XAU) | JSON number, server `decimal(18,8)` |
| Margin level | JSON number (percent, e.g. `1240.5`) or `null` |
| Margin usage | JSON number in `[0, 1]` |
| Tickets / ids | UUID and FIX ids as **string**. MT5 `login` as JSON number (A26). |
| Concurrency | response header `ETag` (see §8) |
| Correlation | echo `X-Correlation-Id` |
| Cache | `Cache-Control: no-store`. Redis may cache the assembled snapshot; miss → PostgreSQL; DB down → **503** `DEPENDENCY_UNAVAILABLE` (do not invent a green book). |

Query (all optional):

| Query | Default | Max | Notes |
|---|---|---|---|
| `rejectedLimit` | `20` | `100` | Size of `rejectedIntents` window |
| `includeFlattenDetail` | `false` | — | If `true` **and** caller is RiskManager or SuperAdmin, populate `killSwitch.flattenRun` (A48 run + counts). ReadOnly/Analyst: ignored (field stays `null`). |
| `book` | omitted | — | `SHADOW` \| `LIVE` \| `AUTO`. Default `AUTO` = LIVE dest book if any known dest XAU position exists, else SHADOW numbers (still labeled). Never mix silently. |

No other query parameters. No `reveal*` on this resource.

HTTP 200 is the success code even when `account.available === false` or books are zeros. Missing data is represented **inside** the DTO, not as 404.

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | `rejectedLimit` not an int in range |
| 401 | `UNAUTHENTICATED` | no/expired token |
| 403 | `FORBIDDEN` | authenticated but policy deny (should not happen for ReadOnly+ on GET) |
| 503 | `DEPENDENCY_UNAVAILABLE` | PostgreSQL unavailable — fail closed; UI shows last-good only if it already has one |

---

## 3. Complete `data` object

### 3.1 Canonical JSON (Phase 8 example — live dest book, stop-new ON, flatten idle)

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:04:00.123Z",
    "canonicalSymbol": "XAUUSD",
    "destinationAccount": "1369850",
    "book": "LIVE",
    "account": {
      "equity": 25000.00,
      "balance": 24810.50,
      "freeMargin": 84210.55,
      "usedMargin": 1989.45,
      "marginLevel": 1240.5,
      "currency": "USD",
      "asOf": "2026-08-18T12:03:58.000Z",
      "available": true,
      "unavailableReason": null
    },
    "performance": {
      "dailyPnl": -120.40,
      "currentDrawdown": 380.00,
      "highWaterEquity": 25380.00,
      "currency": "USD",
      "asOf": "2026-08-18T12:03:58.000Z",
      "window": "DESTINATION_TRADING_DAY"
    },
    "xau": {
      "canonicalSymbol": "XAUUSD",
      "longQuantity": 2.10,
      "shortQuantity": 0.40,
      "netQuantity": 1.70,
      "grossQuantity": 2.50,
      "openPositionCount": 3,
      "asOf": "2026-08-18T12:03:58.000Z"
    },
    "utilization": {
      "xauGross": 0.2500,
      "xauNet": 0.3400,
      "dailyLoss": 0.2408,
      "drawdown": 0.3800,
      "margin": 0.0236,
      "openPositions": 0.1500
    },
    "riskByCopiedTrader": [
      {
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "brokerCode": "ACHIEVER",
        "login": 6100421,
        "traderState": "LIVE",
        "netQuantity": 0.50,
        "longQuantity": 0.50,
        "shortQuantity": 0.00,
        "allocatedNotional": 1200.00,
        "unrealizedPnl": 12.40,
        "realizedPnlToday": -4.10,
        "lastRejectReasonCode": null
      }
    ],
    "riskBySourceBroker": [
      {
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "displayName": "Achiever",
        "brokerCode": "ACHIEVER",
        "netQuantity": 1.70,
        "longQuantity": 2.10,
        "shortQuantity": 0.40,
        "traderCount": 4,
        "rejectedIntentCount24h": 2
      }
    ],
    "rejectedIntents": [
      {
        "copyIntentId": "e4444444-0000-4000-8000-000000000201",
        "riskDecisionId": "f5555555-0000-4000-8000-000000000301",
        "at": "2026-08-18T09:12:00.000Z",
        "outcome": "REJECT",
        "reasonCode": "QUOTE_STALE",
        "reason": "quote_age exceeded max_quote_age",
        "brokerId": "a1111111-0000-4000-8000-000000000001",
        "brokerCode": "ACHIEVER",
        "login": 7001888,
        "exposureClass": "OPEN_EXPOSURE",
        "requestedQuantity": 0.05,
        "canonicalSymbol": "XAUUSD"
      }
    ],
    "rejectedIntentsTotal": 2,
    "rejectedIntentsWindow": "RECENT",
    "killSwitch": {
      "stopNewExecution": true,
      "stopNewVersion": 4,
      "stopNewChangedAt": "2026-08-18T11:50:00.000Z",
      "stopNewChangedBy": {
        "actorId": "3a0c9b12-1111-4aaa-8bbb-0123456789ab",
        "actorName": "ops@example.com",
        "actorKind": "Human",
        "actorRole": "RiskManager"
      },
      "stopNewSource": "operator",
      "stopNewReason": "QUOTE gap — halt new copy",
      "flattenPhase": "idle",
      "flattenRunId": null,
      "flattenVersion": 1,
      "flattenProgress": null,
      "flattenReason": null,
      "flattenStartedAt": null,
      "flattenFinishedAt": null,
      "flattenRun": null,
      "emergencyFlattenAvailable": false,
      "emergencyFlattenAllowedForMe": false,
      "emergencyFlattenUnavailableReasons": [
        "FLATTEN_MUTATION_NOT_SHIPPED",
        "REAL_COPY_PATH_NOT_READY"
      ],
      "realCopyExecutionEnabled": false,
      "realCopyConfigFloor": false
    },
    "limits": {
      "limitsVersion": 7,
      "updatedAt": "2026-08-17T18:00:00.000Z",
      "updatedByActorId": "3a0c9b12-1111-4aaa-8bbb-0123456789ab",
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
      "maxPriceMove": 3.0,
      "maxSlippage": 0.50,
      "maxMarginUsage": 0.70,
      "blockMartingale": true,
      "blockAbnormalSizing": true,
      "allowRiskReductionWhileStopNew": true
    },
    "venue": {
      "quoteLoggedOn": true,
      "tradeLoggedOn": false,
      "tradeStatus": "DOWN",
      "reconciled": false,
      "readyForExecution": false,
      "knownDestXauPositionCount": 0,
      "flattenMutationShipped": false
    },
    "capabilities": {
      "canActivateStopNew": true,
      "canDeactivateStopNew": true,
      "deactivateStopNewBlockedReason": null,
      "canRequestFlatten": false,
      "canConfirmFlatten": false,
      "canAbortFlatten": false,
      "canAcknowledgeFlatten": false,
      "canPatchLimits": true
    }
  }
}
```

`destinationAccount` is the **destination execution account id as already stored** (cTrader / cServer). It is **not** a password. Do not add FIX `RawData`, account password, or MT5 manager password here (§55).

### 3.2 First-useful / execution-off example (must still ship every key)

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:04:00.123Z",
    "canonicalSymbol": "XAUUSD",
    "destinationAccount": null,
    "book": "NONE",
    "account": {
      "equity": null,
      "balance": null,
      "freeMargin": null,
      "usedMargin": null,
      "marginLevel": null,
      "currency": "USD",
      "asOf": null,
      "available": false,
      "unavailableReason": "EXECUTION_DISABLED"
    },
    "performance": {
      "dailyPnl": 0.00,
      "currentDrawdown": 0.00,
      "highWaterEquity": null,
      "currency": "USD",
      "asOf": null,
      "window": "DESTINATION_TRADING_DAY"
    },
    "xau": {
      "canonicalSymbol": "XAUUSD",
      "longQuantity": 0,
      "shortQuantity": 0,
      "netQuantity": 0,
      "grossQuantity": 0,
      "openPositionCount": 0,
      "asOf": null
    },
    "utilization": {
      "xauGross": 0,
      "xauNet": 0,
      "dailyLoss": 0,
      "drawdown": 0,
      "margin": null,
      "openPositions": 0
    },
    "riskByCopiedTrader": [],
    "riskBySourceBroker": [],
    "rejectedIntents": [],
    "rejectedIntentsTotal": 0,
    "rejectedIntentsWindow": "RECENT",
    "killSwitch": {
      "stopNewExecution": true,
      "stopNewVersion": 0,
      "stopNewChangedAt": "2026-08-18T12:04:00.123Z",
      "stopNewChangedBy": null,
      "stopNewSource": "boot_fail_closed",
      "stopNewReason": "no durable kill-switch row; fail closed",
      "flattenPhase": "idle",
      "flattenRunId": null,
      "flattenVersion": 0,
      "flattenProgress": null,
      "flattenReason": null,
      "flattenStartedAt": null,
      "flattenFinishedAt": null,
      "flattenRun": null,
      "emergencyFlattenAvailable": false,
      "emergencyFlattenAllowedForMe": false,
      "emergencyFlattenUnavailableReasons": [
        "FLATTEN_MUTATION_NOT_SHIPPED",
        "TRADE_NOT_LOGGED_ON",
        "NO_KNOWN_DEST_XAU_POSITION",
        "REAL_COPY_PATH_NOT_READY"
      ],
      "realCopyExecutionEnabled": false,
      "realCopyConfigFloor": false
    },
    "limits": {
      "limitsVersion": 0,
      "updatedAt": null,
      "updatedByActorId": null,
      "maxLossPerSelectedTrader": 500.00,
      "maxDailyExecutionAccountLoss": 2000.00,
      "maxPortfolioDrawdown": 3000.00,
      "maxXauGross": 20.0,
      "maxXauNet": 10.0,
      "maxPositionQuantity": 5.0,
      "maxOpenPositions": 20,
      "maxAllowedSpread": 2.0,
      "maxQuoteAgeMs": 3000,
      "maxSourceSignalAgeMs": 15000,
      "maxPriceMove": 3.0,
      "maxSlippage": 1.5,
      "maxMarginUsage": 0.70,
      "blockMartingale": true,
      "blockAbnormalSizing": true,
      "allowRiskReductionWhileStopNew": true
    },
    "venue": {
      "quoteLoggedOn": false,
      "tradeLoggedOn": false,
      "tradeStatus": "DOWN",
      "reconciled": false,
      "readyForExecution": false,
      "knownDestXauPositionCount": 0,
      "flattenMutationShipped": false
    },
    "capabilities": {
      "canActivateStopNew": false,
      "canDeactivateStopNew": false,
      "deactivateStopNewBlockedReason": null,
      "canRequestFlatten": false,
      "canConfirmFlatten": false,
      "canAbortFlatten": false,
      "canAcknowledgeFlatten": false,
      "canPatchLimits": false
    }
  }
}
```

First-useful **limits numeric defaults** in the example match current `Domain/Risk/RiskEngine.cs` `RiskLimits` so the page can render a limits card before `PATCH /risk/limits` exists. Once a durable limits document exists, the DTO **must** project that document, not the code defaults. `limitsVersion: 0` means “seed / not yet operator-set.”

---

## 4. Field dictionary

### 4.1 Envelope / identity

| JSON | Type | Null? | Rule |
|---|---|---|---|
| `generatedAt` | string datetime | no | Query clock, UTC. |
| `canonicalSymbol` | string | no | v1 always `"XAUUSD"`. Never a raw FIX `55`. |
| `destinationAccount` | string | **yes** | Dest execution account. `null` when unknown / not configured. Never a secret. |
| `book` | enum | no | `NONE` \| `SHADOW` \| `LIVE`. See §2 `book` query. Shadow quantities must not be labeled `LIVE`. |

### 4.2 `account` — “equity **if available**”

§53’s “if available” is a first-class flag. **Do not fabricate** destination equity from source MT5 balances.

| JSON | Type | Null? | Rule |
|---|---|---|---|
| `equity` / `balance` / `freeMargin` / `usedMargin` / `marginLevel` | number | yes | All null when `available === false`. |
| `currency` | string | no | Display currency; v1 `"USD"`. |
| `asOf` | string datetime | yes | Venue snapshot time. |
| `available` | bool | no | `true` only when a dest account snapshot exists and is not stale beyond configured account-snapshot TTL. |
| `unavailableReason` | enum | yes | Required when `available === false`. |

`unavailableReason` catalog:

```text
EXECUTION_DISABLED          -- REAL_COPY off and no TRADE account snapshot
TRADE_NOT_LOGGED_ON
VENUE_UNREAD                -- never received a dest account snapshot
SNAPSHOT_STALE
NOT_RECONCILED
DATABASE_UNAVAILABLE        -- must not appear on a 200 body; that is 503
```

**Null vs 0:** unknown equity is `null`. A known flat account is `0.00` with `available: true`.

### 4.3 `performance`

Daily P&L and current drawdown are **destination execution-account** figures (A23 §3.4), not source-trader P&L.

| JSON | Rule |
|---|---|
| `dailyPnl` | Signed. Loss is negative. Unknown window → `0.00` **only** when `book === NONE` and no dest snapshot; otherwise compute. Do not copy source-trader P&L into this field. |
| `currentDrawdown` | `>= 0`. Distance below `highWaterEquity`. Unknown → `0.00` with `highWaterEquity: null`. |
| `window` | `DESTINATION_TRADING_DAY` (v1). Do not silently switch to UTC calendar without changing this tag. |

### 4.4 `xau`

Identities from dest `destination_positions` when `book === LIVE`, else from the shadow book when `book === SHADOW`.

```text
netQuantity   = longQuantity - shortQuantity
grossQuantity = |longQuantity| + |shortQuantity|
```

Server computes both. Client must not re-derive from a partial payload.

`openPositionCount` is dest (or shadow) open XAU positions, not source MT5 positions.

### 4.5 `utilization`

Derived gauges for the page (current / cap). Values in `[0, +∞)` — may exceed `1.0` if a cap is already breached (the card must show overflow, not clamp).

| Key | Numerator | Denominator |
|---|---|---|
| `xauGross` | `xau.grossQuantity` | `limits.maxXauGross` |
| `xauNet` | `abs(xau.netQuantity)` | `limits.maxXauNet` |
| `dailyLoss` | `max(0, -performance.dailyPnl)` | `limits.maxDailyExecutionAccountLoss` |
| `drawdown` | `performance.currentDrawdown` | `limits.maxPortfolioDrawdown` |
| `margin` | dest margin usage if known | `limits.maxMarginUsage` — **null** if account unavailable |
| `openPositions` | `xau.openPositionCount` | `limits.maxOpenPositions` |

Divide-by-zero → `null` for that key (do not emit `Infinity` / `NaN`).

### 4.6 `riskByCopiedTrader[]`

Compound identity **`brokerId` + `login`** (§10). Never a bare login.

| JSON | Rule |
|---|---|
| `brokerId` | UUID string |
| `brokerCode` | display code (`ACHIEVER`, …) |
| `login` | MT5 login |
| `traderState` | A26 `traderState` enum |
| `netQuantity` / `longQuantity` / `shortQuantity` | this trader’s dest (or shadow) XAU |
| `allocatedNotional` | booked allocation in account currency; `0` if none |
| `unrealizedPnl` / `realizedPnlToday` | dest/shadow for this trader; `0` if flat |
| `lastRejectReasonCode` | most recent 24h reject for this trader, or `null` |

Sort: `|netQuantity|` desc, then `|unrealizedPnl|` desc. Empty array when no copied traders — **do not omit the key**.

No ML probability on this page (A52).

### 4.7 `riskBySourceBroker[]`

One row per source broker that currently contributes dest/shadow XAU **or** had a reject in 24h (so a broker that is only failing still appears).

Do **not** add `riskByCluster` (A60).

### 4.8 `rejectedIntents[]`

Recent window, newest first. Full history is `GET /api/v1/risk/rejections` (A63) — same item shape.

Include any `risk_decisions` row whose `outcome` is not a send-approving `APPROVE` with `allowFixSend` irrelevant — i.e. `REJECT`, `REDUCE_SIZE` (listed because the operator needs to see cuts), `PAUSE_TRADER`, `PAUSE_VENUE`, `GLOBAL_STOP`.

`REDUCE_SIZE` that still sent is **not** a reject; omit those. `REDUCE_SIZE` that died (`approvedQuantity == 0` or never persisted an execution intent) **is** included with `outcome: "REDUCE_SIZE"`.

`reasonCode` is the **stable** catalog in §7. `reason` is the human/detail string already stored on `risk_decisions`. Do not invent a second prose generator in the API.

---

## 5. `killSwitch` — the §53/§40 state object

This object **is** A48 §10.1, embedded so the Risk page does not need a second round-trip to paint the two indicators. `GET /api/v1/risk/kill-switch` returns this object as `data` with no wrapping siblings.

### 5.1 Invariants

```text
stopNewExecution ∈ { true, false }          -- latch, not a mode
flattenPhase     ∈ FlattenPhase             -- run machine, not a bool
```

Illegal projections (review reject):

| Projection | Why illegal |
|---|---|
| `"killSwitch": "None"` / `"StopNewExecution"` / `"EmergencyFlatten"` | Exclusive enum as state (current `RiskDashboardDto`) |
| `"emergencyFlatten": true` meaning *either* available *or* running | Conflates §53 availability with A48 phase |
| One `status: "HALTED"` traffic light | Hides “stop-new ON + flatten PARTIAL_FAILED” |
| `stopNewExecution: false` when no durable row | Violates A48 fail-closed |
| Confirm token / phrase / step-up secret on GET | §55 / A48 §8.3 |
| Flatten source MT5 or shadow positions in this object | Wrong book |

### 5.2 Stop-new fields

| JSON | Type | Rule |
|---|---|---|
| `stopNewExecution` | bool | Durable latch. Default **true** if no row. |
| `stopNewVersion` | int | Optimistic concurrency for activate/deactivate. `0` = synthesized fail-closed. |
| `stopNewChangedAt` | datetime | Last audited transition; for synthesized row use `generatedAt`. |
| `stopNewChangedBy` | actor \| null | Null on synthesized boot row or system-only if actor not projected. |
| `stopNewSource` | enum | `operator` \| `engine_global_stop` \| `flatten_side_effect` \| `boot_fail_closed` |
| `stopNewReason` | string | Required. Min 1 char on a real row. |

Actor object:

```json
{
  "actorId": "uuid",
  "actorName": "ops@example.com",
  "actorKind": "Human",
  "actorRole": "RiskManager"
}
```

`actorKind`: `Human` \| `System`. `actorRole`: `SuperAdmin` \| `RiskManager` \| `Analyst` \| `ReadOnly` \| `System`.  
Never put IP, user-agent, password, or token here.

### 5.3 Flatten fields

| JSON | Type | Rule |
|---|---|---|
| `flattenPhase` | enum | A48 §4.2: `idle` \| `confirm_pending` \| `active` \| `completed` \| `partial_failed` \| `aborted` |
| `flattenRunId` | uuid \| null | Current or last un-acked run; `null` when idle with nothing to ack |
| `flattenVersion` | int | Concurrency for flatten commands |
| `flattenProgress` | object \| null | Required when phase ∈ {`active`,`completed`,`partial_failed`,`aborted`}: `{ targetCount, closedCount, failedCount, blockedCount }` |
| `flattenReason` | string \| null | Operator reason of the current/last run |
| `flattenStartedAt` / `flattenFinishedAt` | datetime \| null | |
| `flattenRun` | object \| null | **Redacted** unless `includeFlattenDetail=true` and role ≥ RiskManager. Never includes token hash or raw token. |

`flattenRun` (privileged detail):

```json
{
  "flattenRunId": "uuid",
  "phase": "active",
  "destinationAccount": "1369850",
  "canonicalSymbol": "XAUUSD",
  "requestedAt": "2026-08-18T12:00:00.000Z",
  "requestedByActorId": "uuid",
  "confirmedAt": "2026-08-18T12:00:20.000Z",
  "correlationId": "uuid",
  "targetCount": 3,
  "closedCount": 1,
  "failedCount": 0,
  "blockedCount": 1,
  "targets": [
    {
      "flattenTargetId": "uuid",
      "destinationPositionId": "string",
      "side": "BUY",
      "quantity": 0.40,
      "status": "sent",
      "clOrdId": "TI-20260818-000099",
      "lastError": null
    }
  ]
}
```

`clOrdId` is an operational id (§57), not a secret. Do not add FIX password tags.

### 5.4 Availability (what §53 asked for)

Two booleans, computed, **never stored**:

```text
emergencyFlattenAvailable =
    flattenPhase == idle
    AND venue.flattenMutationShipped == true
    AND venue.tradeLoggedOn == true
    AND venue.knownDestXauPositionCount >= 1
    AND venue.readyForExecution == true
       /* dest book is known enough to close; REAL_COPY may still be false (A25/A48) */

emergencyFlattenAllowedForMe =
    emergencyFlattenAvailable
    AND caller.role == SuperAdmin
    AND caller has step-up capability enrolled
```

ReadOnly / Analyst / RiskManager still **receive** both booleans so the desk can see that flatten exists and is idle. Their `emergencyFlattenAllowedForMe` is **false**. Availability is **not** a permission grant (A48 §10.1).

v1 / until Phase 8 flatten mutation ships:

```text
venue.flattenMutationShipped = false
emergencyFlattenAvailable    = false
emergencyFlattenAllowedForMe = false
```

`emergencyFlattenUnavailableReasons` is an array of closed codes (0..n), stable for UI copy:

```text
FLATTEN_MUTATION_NOT_SHIPPED
FLATTEN_NOT_IDLE
TRADE_NOT_LOGGED_ON
NOT_READY_FOR_EXECUTION
NO_KNOWN_DEST_XAU_POSITION
ROLE_CANNOT_FLATTEN          -- only on AllowedForMe path; do not put this on desk-level Available
STEP_UP_NOT_ENROLLED
REAL_COPY_PATH_NOT_READY     -- informational; flatten does not require REAL_COPY=true
```

`REAL_COPY_PATH_NOT_READY` may appear on `UnavailableReasons` as **info** even though A48 allows flatten closes without `REAL_COPY`. It must **not** be the sole reason `emergencyFlattenAvailable` is false.

### 5.5 Real-copy flag (display only)

| JSON | Rule |
|---|---|
| `realCopyExecutionEnabled` | Effective runtime AND of config floor and audited promote (A49). |
| `realCopyConfigFloor` | Config/env floor. PATCH cannot raise effective above this. |

These are **not** kill-switch controls. The Risk page may show a read-only badge. The enable control lives on Settings with its own confirm (A26 / A49).

### 5.6 Phase → UI chrome (normative)

| `flattenPhase` | Indicator label | Severity | Stop-new card |
|---|---|---|---|
| `idle` | `Idle` | neutral | independent |
| `confirm_pending` | `Confirm pending` | warning | forced ON (A48 side-effect) |
| `active` | `Active ({closed}/{target} closed)` | critical | ON; cannot clear |
| `completed` | `Completed — acknowledge` | success / needs ack | still ON |
| `partial_failed` | `Partial failed — unresolved` | critical | still ON; must not silently clear |
| `aborted` | `Aborted — acknowledge` | warning | still ON |

UI **must** render stop-new and flatten as **two widgets**. Partial-failed is an operational issue (A48 §10.2).

---

## 6. `limits`, `venue`, `capabilities`

### 6.1 `limits` — §39 document projection

Names lock to architecture §39 / A26 §6.12 / current `RiskLimits`:

```text
maxLossPerSelectedTrader
maxDailyExecutionAccountLoss
maxPortfolioDrawdown
maxXauGross
maxXauNet
maxPositionQuantity
maxOpenPositions
maxAllowedSpread
maxQuoteAgeMs
maxSourceSignalAgeMs
maxPriceMove
maxSlippage
maxMarginUsage
blockMartingale
blockAbnormalSizing
allowRiskReductionWhileStopNew
```

Plus `limitsVersion`, `updatedAt`, `updatedByActorId`.

Do **not** add `maxAllocationPerCluster*` until A60 ships (A60 V1-5).

`PATCH /api/v1/risk/limits` uses `If-Match` of the dashboard `ETag` **or** `limitsVersion` in the body. This GET is the document the form hydrates.

### 6.2 `venue` — minimum to compute flatten availability honestly

This is **not** the FIX page. Four booleans + a count + a ship flag. Sequence numbers, passwords, and `RawData` stay off this DTO.

`tradeStatus` uses A26 `sessionStatus` subset: `DOWN` \| `CONNECTING` \| `LOGGED_ON` \| `LOGGED_OUT` \| `RECONNECTING` \| `SEQUENCE_RESET` \| `ERROR` \| `RECONCILING` \| `READY_FOR_EXECUTION`.

### 6.3 `capabilities` — server-computed for **this caller**

The React page must not infer buttons from a role string in localStorage (A26 §11). It uses this object.

| Field | True when |
|---|---|
| `canActivateStopNew` | role ∈ {RiskManager, SuperAdmin} |
| `canDeactivateStopNew` | same roles **and** `flattenPhase == idle` |
| `deactivateStopNewBlockedReason` | `FLATTEN_NOT_IDLE` or `null` |
| `canRequestFlatten` | SuperAdmin **and** flatten mutation shipped **and** phase `idle` (step-up still required at POST time) |
| `canConfirmFlatten` | SuperAdmin **and** phase `confirm_pending` |
| `canAbortFlatten` | SuperAdmin **and** phase ∈ {`confirm_pending`,`active`} |
| `canAcknowledgeFlatten` | role ∈ {RiskManager, SuperAdmin} **and** phase ∈ {`completed`,`partial_failed`,`aborted`} |
| `canPatchLimits` | role ∈ {RiskManager, SuperAdmin} |

ReadOnly / Analyst: all `can*` false. They still **see** state.

---

## 7. `reasonCode` catalog (rejected intents)

Closed set for v1. API must not emit a free-string as `reasonCode`. Unknown stored reasons map to `UNSPECIFIED` **and** keep the original text in `reason` (do not drop evidence).

```text
STOP_NEW_EXECUTION
EMERGENCY_FLATTEN_ACTIVE
QUOTE_STALE
QUOTE_UNAVAILABLE
SPREAD_TOO_WIDE
PRICE_MOVED_TOO_FAR
MAX_SLIPPAGE_EXCEEDED
SIGNAL_STALE
INTENT_EXPIRED
INTENT_INCOMPLETE
VENUE_UNHEALTHY
NOT_RECONCILED
EXECUTION_STATE_UNKNOWN
LIMIT_GROSS
LIMIT_NET
LIMIT_DAILY_LOSS
LIMIT_DRAWDOWN
LIMIT_TRADER_LOSS
LIMIT_OPEN_POSITIONS
LIMIT_POSITION_QUANTITY
LIMIT_MARGIN
SIZE_BELOW_MIN
MARTINGALE_BLOCK
ABNORMAL_SIZING
EXECUTION_DISABLED
DATABASE_UNAVAILABLE
UNSPECIFIED
```

Map from current `RiskEngine` stub strings (when that engine is replaced, keep these codes):

| Today’s `Reason` string | `reasonCode` |
|---|---|
| `STOP_NEW_EXECUTION` | `STOP_NEW_EXECUTION` |
| `EMERGENCY_FLATTEN_BLOCKS_NEW` | `EMERGENCY_FLATTEN_ACTIVE` |
| `VENUE_NOT_RECONCILED` | `NOT_RECONCILED` |
| `VENUE_UNHEALTHY` | `VENUE_UNHEALTHY` |
| `QUOTE_MISSING` | `QUOTE_UNAVAILABLE` |
| `QUOTE_STALE` | `QUOTE_STALE` |
| `SPREAD_TOO_WIDE` | `SPREAD_TOO_WIDE` |
| `PRICE_MOVED_TOO_FAR` | `PRICE_MOVED_TOO_FAR` |
| `SIGNAL_STALE` | `SIGNAL_STALE` |
| `MAX_LOSS_PER_TRADER` | `LIMIT_TRADER_LOSS` |
| `MAX_DAILY_EXECUTION_LOSS` | `LIMIT_DAILY_LOSS` |
| `MAX_PORTFOLIO_DRAWDOWN` | `LIMIT_DRAWDOWN` |
| `MAX_OPEN_POSITIONS` | `LIMIT_OPEN_POSITIONS` |
| `MAX_POSITION_QUANTITY` | `LIMIT_POSITION_QUANTITY` |
| `MAX_XAU_GROSS` | `LIMIT_GROSS` |
| `MAX_XAU_NET` | `LIMIT_NET` |
| `MAX_MARGIN_USAGE` | `LIMIT_MARGIN` |
| `MARTINGALE_BLOCK` | `MARTINGALE_BLOCK` |
| `ABNORMAL_SIZING_BLOCK` | `ABNORMAL_SIZING` |

`outcome` on the item uses A26 `riskDecision`: `APPROVE` \| `REDUCE_SIZE` \| `REJECT` \| `PAUSE_TRADER` \| `PAUSE_VENUE` \| `GLOBAL_STOP`.

---

## 8. ETag and freshness

```text
ETag = W/"risk:{stopNewVersion}:{flattenVersion}:{limitsVersion}:{accountAsOfOr0}:{xauAsOfOr0}"
```

Weak tag: the dashboard is a projection. `If-Match` on `PATCH /risk/limits` and on stop-new POST (A48 `expectedVersion`) must use the **component versions**, not require clients to parse this string. Echo versions inside the JSON so the UI can send them.

`generatedAt` is not part of the tag (clock would bust every GET).

---

## 9. Role redaction (GET is not “one JSON for everyone”)

Same schema for all roles. Fields go `null` / empty — **keys stay**.

| Field | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| account / performance / xau / utilization | yes | yes | yes | yes |
| riskBy* / rejectedIntents | yes | yes | yes | yes |
| `killSwitch` latch + phase + availability | yes | yes | yes | yes |
| `killSwitch.flattenRun` / target rows | **null** | **null** | if requested | if requested |
| `capabilities.*` | all false | all false | stop-new + limits + ack | + flatten verbs |
| `GET /audit` (sibling, not this DTO) | 403 | 403 | yes | yes |

Never vary `emergencyFlattenAvailable` by role. Vary `emergencyFlattenAllowedForMe` and `capabilities`.

Denied privileged **mutations** still write `audit_logs` (A48 §9). GET does not write audit.

---

## 10. SignalR

Hub: `/hubs/ops` (A26). Secret sanitizer applies.

| Event | Payload | UI |
|---|---|---|
| `risk.state` | **entire** `data.killSwitch` (not the old two-bool subset) | Risk cards + shell banner |
| `risk.dashboard` | entire `data` | full invalidate |
| `risk.rejection` | one `rejectedIntents[]` item | prepend + bump `rejectedIntentsTotal` |
| `risk.limits` | `data.limits` | limits card |
| `quote.xauusd` | A26 quote tick | do **not** recompute dest book client-side from quotes |

TanStack Query key: `['risk', 'dashboard']`.  
Focused kill-switch key: `['risk', 'kill-switch']` (same object).  
Invalidate both on `risk.state` / any kill-switch mutation success.

A26’s `risk.state = { stopNewExecution, emergencyFlattenAvailable, realCopyExecutionEnabled }` is **superseded**. A two-bool frame cannot represent `partial_failed`.

Poll fallback: 5s only if the hub is down. Never poll a secret endpoint (there isn’t one here).

---

## 11. C# records (replace `RiskDashboardDto`)

Recommended later types (not created by this task). Do **not** keep `string KillSwitch`.

```csharp
// Application/Dashboard/RiskDashboardDtos.cs  (future)

public sealed record RiskDashboardDto(
    DateTimeOffset GeneratedAt,
    string CanonicalSymbol,
    string? DestinationAccount,
    string Book,
    RiskAccountDto Account,
    RiskPerformanceDto Performance,
    RiskXauBookDto Xau,
    RiskUtilizationDto Utilization,
    IReadOnlyList<RiskByTraderDto> RiskByCopiedTrader,
    IReadOnlyList<RiskByBrokerDto> RiskBySourceBroker,
    IReadOnlyList<RejectedIntentDto> RejectedIntents,
    int RejectedIntentsTotal,
    string RejectedIntentsWindow,
    KillSwitchStateDto KillSwitch,
    RiskLimitsDto Limits,
    RiskVenueDto Venue,
    RiskCapabilitiesDto Capabilities);

public sealed record RiskAccountDto(
    decimal? Equity,
    decimal? Balance,
    decimal? FreeMargin,
    decimal? UsedMargin,
    decimal? MarginLevel,
    string Currency,
    DateTimeOffset? AsOf,
    bool Available,
    string? UnavailableReason);

public sealed record RiskPerformanceDto(
    decimal DailyPnl,
    decimal CurrentDrawdown,
    decimal? HighWaterEquity,
    string Currency,
    DateTimeOffset? AsOf,
    string Window);

public sealed record RiskXauBookDto(
    string CanonicalSymbol,
    decimal LongQuantity,
    decimal ShortQuantity,
    decimal NetQuantity,
    decimal GrossQuantity,
    int OpenPositionCount,
    DateTimeOffset? AsOf);

public sealed record RiskUtilizationDto(
    decimal? XauGross,
    decimal? XauNet,
    decimal? DailyLoss,
    decimal? Drawdown,
    decimal? Margin,
    decimal? OpenPositions);

public sealed record RiskByTraderDto(
    Guid BrokerId,
    string BrokerCode,
    long Login,
    string TraderState,
    decimal NetQuantity,
    decimal LongQuantity,
    decimal ShortQuantity,
    decimal AllocatedNotional,
    decimal UnrealizedPnl,
    decimal RealizedPnlToday,
    string? LastRejectReasonCode);

public sealed record RiskByBrokerDto(
    Guid BrokerId,
    string DisplayName,
    string BrokerCode,
    decimal NetQuantity,
    decimal LongQuantity,
    decimal ShortQuantity,
    int TraderCount,
    int RejectedIntentCount24h);

public sealed record RejectedIntentDto(
    Guid CopyIntentId,
    Guid RiskDecisionId,
    DateTimeOffset At,
    string Outcome,
    string ReasonCode,
    string Reason,
    Guid BrokerId,
    string BrokerCode,
    long Login,
    string ExposureClass,
    decimal RequestedQuantity,
    string CanonicalSymbol);

public sealed record KillSwitchActorDto(
    Guid ActorId,
    string ActorName,
    string ActorKind,
    string ActorRole);

public sealed record KillSwitchProgressDto(
    int TargetCount,
    int ClosedCount,
    int FailedCount,
    int BlockedCount);

public sealed record KillSwitchStateDto(
    bool StopNewExecution,
    int StopNewVersion,
    DateTimeOffset StopNewChangedAt,
    KillSwitchActorDto? StopNewChangedBy,
    string StopNewSource,
    string StopNewReason,
    string FlattenPhase,
    Guid? FlattenRunId,
    int FlattenVersion,
    KillSwitchProgressDto? FlattenProgress,
    string? FlattenReason,
    DateTimeOffset? FlattenStartedAt,
    DateTimeOffset? FlattenFinishedAt,
    FlattenRunDetailDto? FlattenRun,
    bool EmergencyFlattenAvailable,
    bool EmergencyFlattenAllowedForMe,
    IReadOnlyList<string> EmergencyFlattenUnavailableReasons,
    bool RealCopyExecutionEnabled,
    bool RealCopyConfigFloor);

public sealed record RiskLimitsDto(
    int LimitsVersion,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedByActorId,
    decimal MaxLossPerSelectedTrader,
    decimal MaxDailyExecutionAccountLoss,
    decimal MaxPortfolioDrawdown,
    decimal MaxXauGross,
    decimal MaxXauNet,
    decimal MaxPositionQuantity,
    int MaxOpenPositions,
    decimal MaxAllowedSpread,
    int MaxQuoteAgeMs,
    int MaxSourceSignalAgeMs,
    decimal MaxPriceMove,
    decimal MaxSlippage,
    decimal MaxMarginUsage,
    bool BlockMartingale,
    bool BlockAbnormalSizing,
    bool AllowRiskReductionWhileStopNew);

public sealed record RiskVenueDto(
    bool QuoteLoggedOn,
    bool TradeLoggedOn,
    string TradeStatus,
    bool Reconciled,
    bool ReadyForExecution,
    int KnownDestXauPositionCount,
    bool FlattenMutationShipped);

public sealed record RiskCapabilitiesDto(
    bool CanActivateStopNew,
    bool CanDeactivateStopNew,
    string? DeactivateStopNewBlockedReason,
    bool CanRequestFlatten,
    bool CanConfirmFlatten,
    bool CanAbortFlatten,
    bool CanAcknowledgeFlatten,
    bool CanPatchLimits);
```

`IDashboardQueries.GetRiskAsync` must return **this** shape (or a dedicated `IRiskDashboardQuery`). Current `RiskDashboardDto(string KillSwitch, …)` is retired in the same change set.

JSON serialization: ASP.NET default camelCase. Enums as **strings** (`idle`, not `0`). Do not serialize `KillSwitchMode`.

---

## 12. TypeScript (replace `RiskStatus`)

Later file: `apps/web/src/types/risk.ts`. Hook: `GET /api/v1/risk/dashboard` → `data`.

```ts
export type FlattenPhase =
  | 'idle'
  | 'confirm_pending'
  | 'active'
  | 'completed'
  | 'partial_failed'
  | 'aborted';

export type RiskBook = 'NONE' | 'SHADOW' | 'LIVE';

export type StopNewSource =
  | 'operator'
  | 'engine_global_stop'
  | 'flatten_side_effect'
  | 'boot_fail_closed';

export interface KillSwitchActor {
  actorId: string;
  actorName: string;
  actorKind: 'Human' | 'System';
  actorRole: 'SuperAdmin' | 'RiskManager' | 'Analyst' | 'ReadOnly' | 'System';
}

export interface KillSwitchState {
  stopNewExecution: boolean;
  stopNewVersion: number;
  stopNewChangedAt: string;
  stopNewChangedBy: KillSwitchActor | null;
  stopNewSource: StopNewSource;
  stopNewReason: string;
  flattenPhase: FlattenPhase;
  flattenRunId: string | null;
  flattenVersion: number;
  flattenProgress: {
    targetCount: number;
    closedCount: number;
    failedCount: number;
    blockedCount: number;
  } | null;
  flattenReason: string | null;
  flattenStartedAt: string | null;
  flattenFinishedAt: string | null;
  flattenRun: FlattenRunDetail | null;
  emergencyFlattenAvailable: boolean;
  emergencyFlattenAllowedForMe: boolean;
  emergencyFlattenUnavailableReasons: string[];
  realCopyExecutionEnabled: boolean;
  realCopyConfigFloor: boolean;
}

export interface RiskDashboard {
  generatedAt: string;
  canonicalSymbol: 'XAUUSD' | string;
  destinationAccount: string | null;
  book: RiskBook;
  account: {
    equity: number | null;
    balance: number | null;
    freeMargin: number | null;
    usedMargin: number | null;
    marginLevel: number | null;
    currency: string;
    asOf: string | null;
    available: boolean;
    unavailableReason: string | null;
  };
  performance: {
    dailyPnl: number;
    currentDrawdown: number;
    highWaterEquity: number | null;
    currency: string;
    asOf: string | null;
    window: string;
  };
  xau: {
    canonicalSymbol: string;
    longQuantity: number;
    shortQuantity: number;
    netQuantity: number;
    grossQuantity: number;
    openPositionCount: number;
    asOf: string | null;
  };
  utilization: {
    xauGross: number | null;
    xauNet: number | null;
    dailyLoss: number | null;
    drawdown: number | null;
    margin: number | null;
    openPositions: number | null;
  };
  riskByCopiedTrader: RiskByTrader[];
  riskBySourceBroker: RiskByBroker[];
  rejectedIntents: RejectedIntent[];
  rejectedIntentsTotal: number;
  rejectedIntentsWindow: string;
  killSwitch: KillSwitchState;
  limits: RiskLimits;
  venue: RiskVenue;
  capabilities: RiskCapabilities;
}

export interface RiskByTrader {
  brokerId: string;
  brokerCode: string;
  login: number;
  traderState: string;
  netQuantity: number;
  longQuantity: number;
  shortQuantity: number;
  allocatedNotional: number;
  unrealizedPnl: number;
  realizedPnlToday: number;
  lastRejectReasonCode: string | null;
}

export interface RiskByBroker {
  brokerId: string;
  displayName: string;
  brokerCode: string;
  netQuantity: number;
  longQuantity: number;
  shortQuantity: number;
  traderCount: number;
  rejectedIntentCount24h: number;
}

export interface RejectedIntent {
  copyIntentId: string;
  riskDecisionId: string;
  at: string;
  outcome: string;
  reasonCode: string;
  reason: string;
  brokerId: string;
  brokerCode: string;
  login: number;
  exposureClass: string;
  requestedQuantity: number;
  canonicalSymbol: string;
}

export interface RiskLimits {
  limitsVersion: number;
  updatedAt: string | null;
  updatedByActorId: string | null;
  maxLossPerSelectedTrader: number;
  maxDailyExecutionAccountLoss: number;
  maxPortfolioDrawdown: number;
  maxXauGross: number;
  maxXauNet: number;
  maxPositionQuantity: number;
  maxOpenPositions: number;
  maxAllowedSpread: number;
  maxQuoteAgeMs: number;
  maxSourceSignalAgeMs: number;
  maxPriceMove: number;
  maxSlippage: number;
  maxMarginUsage: number;
  blockMartingale: boolean;
  blockAbnormalSizing: boolean;
  allowRiskReductionWhileStopNew: boolean;
}

export interface RiskVenue {
  quoteLoggedOn: boolean;
  tradeLoggedOn: boolean;
  tradeStatus: string;
  reconciled: boolean;
  readyForExecution: boolean;
  knownDestXauPositionCount: number;
  flattenMutationShipped: boolean;
}

export interface RiskCapabilities {
  canActivateStopNew: boolean;
  canDeactivateStopNew: boolean;
  deactivateStopNewBlockedReason: string | null;
  canRequestFlatten: boolean;
  canConfirmFlatten: boolean;
  canAbortFlatten: boolean;
  canAcknowledgeFlatten: boolean;
  canPatchLimits: boolean;
}

export interface FlattenRunDetail {
  flattenRunId: string;
  phase: FlattenPhase;
  destinationAccount: string;
  canonicalSymbol: string;
  requestedAt: string;
  requestedByActorId: string;
  confirmedAt: string | null;
  correlationId: string;
  targetCount: number;
  closedCount: number;
  failedCount: number;
  blockedCount: number;
  targets: Array<{
    flattenTargetId: string;
    destinationPositionId: string;
    side: string;
    quantity: number;
    status: string;
    clOrdId: string | null;
    lastError: string | null;
  }>;
}
```

UI rules that this DTO enables (A62 §10.7, corrected):

- Two widgets. Flatten button **disabled** (not hidden) when `!capabilities.canRequestFlatten` or `!emergencyFlattenAllowedForMe`; tooltip = `emergencyFlattenUnavailableReasons`.
- Stop-new clear disabled when `deactivateStopNewBlockedReason != null`.
- `account.available === false` → render “unavailable” not `0` equity.
- Do not wire `useEmergencyFlatten` until `venue.flattenMutationShipped` is true (A63).

Stale `RiskStatus` and `GET /api/risk/status` are deleted in the same UI change set.

---

## 13. Assembly rules (query)

One application service, one DB round (plus optional Redis cache) per GET.

```text
1. Authn / role → capabilities
2. Load kill-switch snapshot (A48 IKillSwitchQuery)
      missing row → synthesize fail-closed (do not persist on GET)
3. Load limits document (or engine seed with limitsVersion=0)
4. Load dest account snapshot if any; else account.available=false
5. Load dest XAU positions if book LIVE possible; else shadow book; else zeros
6. Aggregate riskByCopiedTrader / riskBySourceBroker from the same book
7. Load recent risk_decisions window; map reason → reasonCode
8. Load venue bits from fix_sessions + last recon run (A47)
9. Compute utilization, flatten availability, ETag
10. Redact flattenRun unless allowed
11. Return 200
```

Fail closed:

- DB down → 503, no body that claims a healthy book.
- Redis stale “stop-new off” **must not** override PostgreSQL (A48 §6). Cache after commit only.
- Source MT5 `mt5_positions` **must not** fill `data.xau` when `book === LIVE`.

Current `EfDashboardQueries.GetRiskAsync` violates 2, 4, 5, 6, 7 (reasons only, no identity), and 8. It is a placeholder.

---

## 14. Mutations vs this GET

This DTO is read-only. After a successful mutation the API returns A26’s mutation envelope **and** the client invalidates `['risk','dashboard']`. Optionally mutations may include `data.killSwitch` in the mutation response — if they do, it **must** be this same object.

| Mutation | Spec | Effect on this DTO |
|---|---|---|
| `POST /api/v1/risk/stop-new-execution/activate` | A48 | `stopNewExecution=true`, version++, source `operator` |
| `POST /api/v1/risk/stop-new-execution/deactivate` | A48 | `false` only if flatten idle; else 409, DTO unchanged |
| Flatten request/confirm/abort/ack | A48 | `flattenPhase` / progress / availability |
| `PATCH /api/v1/risk/limits` | A26 | `limits.*` + `limitsVersion` |
| Engine `GLOBAL_STOP` | A23 / A48 | stop-new ON, source `engine_global_stop`; flatten stays idle |
| Settings real-copy | A49 | `realCopyExecutionEnabled` badge only |

GET must not activate, clear, or flatten.

---

## 15. What this DTO must never contain

```text
MT5 / proxy / FIX / cTrader / DB / Redis / Vault passwords
FIX RawData / tag 96 / senderSubId secrets
confirm_token, token hash, step-up password, TOTP seed
raw manager login (use other pages’ mask rules; this DTO has no manager login)
source-trader passwords or PII beyond brokerId+login
ML probabilities / model ids (A52)
riskByCluster / concentration caps (A60)
per-tick quote tape
FIX sequence numbers (those belong on the FIX page)
fabricated MFE/MAE
a single killSwitch string or exclusive mode
```

---

## 16. Phasing

| Phase | What the DTO contains | Honesty rule |
|---|---|---|
| First useful / §69 | Full key set. Account unavailable. XAU zeros or **labeled** `SHADOW`. Flatten available=false. Stop-new **real** (fail closed). Rejects if any shadow/risk decisions exist. | Do not hide keys. Do not claim LIVE book. |
| Phase 7 TRADE read/recon | Account fields may become available from TRADE snapshots. `knownDestXauPositionCount` real. Flatten mutation still unshipped. | `available` follows snapshot quality. |
| Phase 8 | Flatten phase/progress can leave `idle`. Availability can become true. | Partial-failed stays visible until ack. |
| Later | A60 cluster array **additive** only when scheduled. | No unused keys early. |

---

## 17. Tests this contract owns

Add when coding (names lock here + extend A27):

| Class | Must prove |
|---|---|
| `Api.RiskDashboardDtoSchemaTests` | Every §53 path present; `killSwitch` is an object; no `KillSwitch` string. |
| `Api.RiskDashboardDtoKillSwitchSeparationTests` | Payload can be `stopNew=true` **and** `flattenPhase=active` together; never exclusive mode. |
| `Api.RiskDashboardDtoFailClosedNoKillRowTests` | No row → `stopNewExecution=true`, `stopNewSource=boot_fail_closed`, not `"None"`. |
| `Api.RiskDashboardDtoEmptySafeTests` | Execution off → `account.available=false`, null equity, empty arrays, flatten available=false, keys present. |
| `Api.RiskDashboardDtoDoesNotExposeSecretsTests` | Property names/values denylist (A26 §3.1); no confirm token. |
| `Api.RiskDashboardDtoReasonCodeCatalogTests` | Engine strings map to §7; unknown → `UNSPECIFIED` + original `reason`. |
| `Api.RiskDashboardRbacFieldRedactionTests` | ReadOnly cannot see `flattenRun`; SuperAdmin can with query flag; availability bool still present. |
| `Api.RiskDashboardCapabilitiesTests` | RiskManager `canRequestFlatten=false`; SuperAdmin true only when shipped+idle; deactivate blocked when flatten not idle. |
| `Api.RiskDashboardAliasSnapshotParityTests` | `/risk/snapshot` === `/risk/dashboard` body. |
| `Api.RiskDashboardKillSwitchSiblingParityTests` | `/risk/kill-switch`.data === `/risk/dashboard`.data.killSwitch. |
| `Api.RiskDashboardNoClusterKeysTests` | A60 V1-5 — no `riskByCluster`. |
| `Web.RiskPageTwoWidgetsTests` | Page binds `killSwitch.stopNewExecution` and `flattenPhase` separately; no single toggle. |

Go-live checkboxes this file owns (dashboard read path):

```text
[ ] GET /api/v1/risk/dashboard returns the §53 set
[ ] Kill-switch state is two controls, not one string
[ ] Empty / execution-off payload is honest (null equity, flatten unavailable)
[ ] No secrets in the Risk DTO
[ ] Fail-closed when no durable kill row
[ ] SignalR risk.state carries flattenPhase
```

---

## 18. Traceability

| Requirement | Section |
|---|---|
| §53 widget list | §0 table, §3–§5 |
| §40 two controls, do not conflate | §5 |
| §53 flatten **availability** (not a stored ready-secret) | §5.4 |
| §39 limits on the desk | §6.1 |
| §41 / A49 real-copy is not this control | §5.5 |
| §55 / §72.5 no secrets to React | §15, §9 |
| §59 roles | §6.3, §9 |
| §10 compound trader id | §4.6 |
| A48 snapshot / fail-closed / phases | §5, §13 |
| A26 path + envelope | §2 |
| A23 reason codes | §7 |
| A60 no cluster keys | §4.7, §16 |
| A62 page mapping | §12 |
| A63 first-useful zeros | §3.2, §16 |

---

## 19. Anti-patterns (reject in review)

```text
[DO NOT] Ship RiskDashboardDto.KillSwitch as string or KillSwitchMode.ToString()
[DO NOT] Default missing row to None / stop-new OFF
[DO NOT] One red button, one bool, one traffic light
[DO NOT] Let RiskManager capabilities.canRequestFlatten become true
[DO NOT] Put confirm tokens on GET
[DO NOT] Fill dest equity from source MT5 account balance
[DO NOT] Label shadow lots as LIVE
[DO NOT] Omit keys in v1 “to keep it small”
[DO NOT] Add riskByCluster or ML fields “for later”
[DO NOT] Treat REAL_COPY as STOP_NEW_EXECUTION
[DO NOT] Auto-clear stop-new in the DTO when flatten completes
[DO NOT] Implement this by editing product source from this task
```

---

## 20. Recommended later file map (not created)

```text
D:\Prop\src\Application\Dashboard\RiskDashboardDtos.cs
D:\Prop\src\Application\Dashboard\IRiskDashboardQuery.cs
D:\Prop\src\Infrastructure\Dashboard\EfRiskDashboardQuery.cs
D:\Prop\apps\api\Endpoints\RiskDashboardEndpoints.cs
D:\Prop\apps\web\src\types\risk.ts
D:\Prop\apps\web\src\pages\risk\RiskDashboardPage.tsx
D:\Prop\tests\Unit\Api\RiskDashboardDtoKillSwitchSeparationTests.cs
D:\Prop\tests\Unit\Api\RiskDashboardDtoFailClosedNoKillRowTests.cs
```

---

## 21. What this artifact did not do

- Did not modify product source under `D:\Prop\src` or `D:\Prop\apps`.
- Did not add endpoints, migrations, React pages, or tests.
- Did not change A48 mutation protocol, flatten confirm phrase, or persist-before-send rules.
- Did not authorize live flatten, live `NewOrderSingle`, or auto-flatten from daily loss.
- Did not bless the current `string KillSwitch` stub as “close enough.”

**Bottom line:** `GET /api/v1/risk/dashboard` is the §53 page DTO. Kill-switch state is a nested object with an independent stop-new latch and a flatten phase machine, plus computed availability and caller capabilities. A single string or a single boolean is a spec violation. First useful version ships every key honestly empty/off. Flatten stays unavailable until Phase 8 ships the mutation.
