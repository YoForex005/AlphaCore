# A97 — SignalR Hub Events: Live Scores, FIX Health, Quotes, Alerts

| Field | Value |
|---|---|
| Agent | A97 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A97_signalr_events.md` |
| Status | Binding implementation spec. **No product source modified.** |
| Law | Architecture v2 §§4–5, 13, 18, 22, 25–31, 40–41, 46–53, 55, 57–59, 62, 69, 72.5 |
| Parents | A26 §7 (hub names), A63 §6 (first-useful catalog), A22 (scores), A25 (FIX sessions), A41 (outbox `notification-event` + Redis relay), A48 (kill switch), A50 / A76 (redaction), A51 (RBAC), A53 (failure codes) |
| Product source edited | **No** |

This file is the **implementable contract** for live dashboard events. It expands A26 §7 and A63 §6 for the four families the header strip and ops pages actually need: **live scores**, **FIX health**, **destination quotes**, **alerts**. REST remains the source of truth. The hub is a push of **allow-listed subsets** of those GET DTOs.

Workers do **not** host SignalR. `apps/api` is the only hub host.

---

## 0. Measured current state (honest)

| Check | Result | Evidence |
|---|---|---|
| `AddSignalR` / `MapHub` | **MISSING** | `D:\Prop\apps\api\Program.cs` is still weatherforecast (A06) |
| Hub host package | **WRONG PACKAGE** | `Microsoft.AspNetCore.SignalR.Common 8.0.4` on `TraderIntelligence.Api.csproj` — Common is not a hub host. Need the shared framework `Microsoft.AspNetCore.App` SignalR (already on Web SDK) |
| `OpsHub` | **MISSING** | No `Hubs/` folder under `apps/api` |
| React client | **STALE PATH** | `D:\Prop\apps\web\src\api\signalr.ts` targets `/hubs/dashboard` |
| Redis relay | **MISSING** | A41 spec only |
| `system_events` table | **MISSING** | A20 catalog, not migrated |
| Score / FIX / quote push | **MISSING** | Domain entities exist; no publisher |

Do not treat the web stub or the Common package as an implemented hub. First useful React may poll REST (`GET /overview`, `GET /fix/quote`, `GET /api/v1/health`) until this hub exists (A06 §4.14, A63 §6). The hub is **recommended**, not a §69 gate.

---

## 1. Binding decisions (do not re-litigate)

When earlier swarm notes disagree, this file wins **for SignalR only**. REST paths stay with A26 / A63.

| Topic | Wrong / stale | Binding in A97 |
|---|---|---|
| Hub path | stub `/hubs/dashboard` | **`/hubs/ops`** (`OpsHub`) |
| Extra hubs (`ScoresHub`, `FixHub`, `QuotesHub`, `AlertsHub`) | tempting | **Forbidden in v1.** One hub, topic subscriptions |
| Score scale | A26 examples `0.71` (unit interval) | Hub + domain **`0–100`** (`baseline.v1`, `TraderScore`). Alias `earlyScore` **is** `earlyQualityScore` on that scale |
| `mlProbability` | invent a number to fill the tile | **Always `null` in v1** (A52, A63) |
| Quote liveness | Heartbeat on QUOTE | **`quoteAgeMs`**, not Heartbeat (A25 §2.5, §7.2) |
| TRADE “healthy” | `LoggedOn` | `LOGGED_ON` is not `READY_FOR_EXECUTION`. Health tile must not lie |
| Secrets on the wire | dump session options / raw Logon | **Never.** Same denylist as A26 §3 / A76 |
| Mutations over SignalR | `SendOrder`, `SetPassword`, flatten | **Forbidden.** Privileged actions stay REST + audit |
| Authority | last hub frame | **PostgreSQL.** Hub is a cache of the last allow-listed snapshot |
| Redis | outbox / order book | Relay + short-lived last-value **only**. Not SoT (arch §5, A41) |

### 1.1 Score scale (explicit)

| Layer | Unit | Source |
|---|---|---|
| Domain `TraderScore.EarlyQualityScore` / `RiskScore` / `BehaviorScore` | `decimal` **0–100**, `Round2` | A22 §§5–7, `D:\Prop\src\Domain\Entities\TraderScore.cs` |
| Hub `trader.score` / `trader.score.batch` | same **0–100** | this file |
| REST leaderboard `earlyScore` | same **0–100** when implemented | A26 0–1 examples are **non-normative** |
| `mlProbability` | `[0, 1]` or `null` | `null` until a promoted model exists |

A client that divides by 100 “to match A26 samples” is a bug.

---

## 2. Transport

### 2.1 Hub

| Item | Contract |
|---|---|
| Class | `OpsHub` : `Hub` |
| Path | `/hubs/ops` |
| Protocol | SignalR JSON protocol. MessagePack **off** in v1 (easier redaction review) |
| Transports | WebSockets preferred; Long Polling allowed. Server-Sent Events optional |
| Auth | Every connection: `Authorization: Bearer <access_token>` **or** negotiate query `access_token`. Same JWT as `/api/v1/**` |
| Roles | `ReadOnly` / `Analyst` / `RiskManager` / `SuperAdmin` (A51 policy `ReadOnlyPlus`) |
| Anonymous | **401** then close. `/health` is not this hub |
| CORS | Same origins as REST. Credentials only if the cookie refresh path is used; access token is still required on the hub |
| Keepalive | Server ping 15s; client timeout 30s |
| Reconnect | Client `withAutomaticReconnect`. After reconnect the client **must** call `Subscribe` again and treat REST as truth until the snapshot arrives |

Suggested composition (later coding task — **do not create from this file**):

```text
apps/api/Hubs/OpsHub.cs
apps/api/Realtime/OpsEventRelay.cs          # Redis subscriber → Clients
apps/api/Realtime/HubPayloadSanitizer.cs    # fail-closed denylist
src/Application/Realtime/OpsEventEnvelope.cs
src/Application/Realtime/IOpsEventPublisher.cs
```

`apps/mt5-worker` and `apps/fix-worker` publish to Redis (or insert `system_events` and let the API poll). They **never** `MapHub`.

### 2.2 Redis relay (A41 §10.5)

| Channel | Payload | Publisher |
|---|---|---|
| `ops:events` | one envelope JSON (this file §3) | outbox `notification-event` handler, FIX quote loop, score handler |
| `ops:last:{topic}` | last-value snapshot string | same, after persist |

If Redis is down: persist `system_events` / domain rows anyway; mark outbox `processed`; dashboard falls back to REST poll. Do **not** fail a score commit or a FIX persist because the hub is dark.

Do not use Redis Streams as the trading outbox. Do not put passwords in Redis keys or values.

### 2.3 Produce path (legal)

```text
domain persist (Postgres, same TX as outbox when applicable)
        ↓
outbox notification-event  OR  dedicated last-value writer (quotes)
        ↓
worker handler
        ↓
sanitize → envelope
        ↓
SET ops:last:{topic}   +   PUBLISH ops:events
        ↓
apps/api OpsEventRelay
        ↓
HubPayloadSanitizer (second time, fail closed)
        ↓
Clients.Group(topic).SendAsync(eventName, envelope)
```

Never: MT5 callback → `IHubContext`. Never: QuickFIX `FromApp` → hub without persist. Never: send unsanitized `CTraderFixOptions`.

---

## 3. Envelope (every server → client frame)

One shape. The SignalR method name equals `event`.

```json
{
  "v": 1,
  "event": "quote.xauusd",
  "at": "2026-08-18T12:00:00.140Z",
  "seq": 18801,
  "correlationId": "b7c1e2d3-0000-4000-8000-000000000001",
  "topic": "quotes",
  "data": {}
}
```

| Field | Type | Rule |
|---|---|---|
| `v` | int | Envelope schema. Current **1**. Unknown `v` → client ignores the frame |
| `event` | string | Exact token from §5–§8 |
| `at` | ISO-8601 UTC `Z` | Server clock when the envelope was built |
| `seq` | int64 as **JSON number** if `< 2^53`, else **string** | Monotonic **per topic per API process**. Not a FIX sequence. After reconnect, do not assume continuity |
| `correlationId` | uuid string or `null` | Required on score / alert / FIX state-change. Quotes may be `null` |
| `topic` | string | Group the client joined (§4) |
| `data` | object | Allow-listed payload. Never the raw EF entity |

Tickets, ClOrdIDs, instrument IDs, destination position IDs: **string**. Login: JSON number (MT5 login fits). Money / qty: JSON number (server `decimal`).

Error frame (connection-level only; not a substitute for REST errors):

```json
{
  "v": 1,
  "event": "hub.error",
  "at": "2026-08-18T12:00:00.000Z",
  "seq": 1,
  "correlationId": "…",
  "topic": "control",
  "data": {
    "code": "FORBIDDEN",
    "message": "Role ReadOnly cannot subscribe to audit.",
    "details": {}
  }
}
```

`hub.error` **never** echoes the rejected token, password, or request body.

---

## 4. Groups, client methods, snapshots

### 4.1 Topics (v1)

| Topic | Events | Typical page |
|---|---|---|
| `header` | `ops.header` | shell (all pages) |
| `scores` | `trader.score`, `trader.score.batch`, `trader.state`, `scoring.summary` | Leaderboard, Scoring, Detail |
| `fix` | `fix.session`, `fix.health` | FIX, Health, header |
| `quotes` | `quote.xauusd`, `quote.stale` | FIX, Shadow, Risk |
| `alerts` | `alert.raised`, `alert.cleared`, `alert.snapshot` | header bell, Health |
| `overview` | `overview.updated` | Overview (A26; not expanded here) |
| `brokers` | `broker.health` | Brokers, Health |
| `shadow` | `shadow.portfolio` | Shadow, Overview |
| `risk` | `risk.state` | Risk, header |
| `recon` | `reconciliation.issue` | Reconciliation |
| `health` | `health.updated` | System Health |

v1 implementers **must** ship `header`, `scores`, `fix`, `quotes`, `alerts`. The rest stay as named in A26/A63 and may be empty until those pages exist.

Unknown topic name → `hub.error` `VALIDATION_FAILED`, do not join.

### 4.2 Client → server (only these)

| Method | Args | Effect |
|---|---|---|
| `Subscribe` | `string[] topics` | Join groups. Idempotent. Then server pushes **snapshots** for those topics (§4.3) |
| `Unsubscribe` | `string[] topics` | Leave groups |
| `WatchTrader` | `brokerId: Guid`, `login: long` | Join `trader:{brokerId}:{login}` for per-trader `trader.score` / `trader.state`. Max **20** watches per connection |
| `UnwatchTrader` | `brokerId`, `login` | Leave that group |

Forbidden client methods (must not exist; if invoked via raw protocol, ignore + `hub.error` `FORBIDDEN`):

```text
SendOrder, CancelOrder, Flatten, SetPassword, SetFixPassword,
EnableRealExecution, InjectFix, PublishQuote, ScoreTrader,
AcknowledgeAlert, any method accepting a payload with a denylisted key
```

Alert acknowledge, kill switch, flatten, trader pause: **REST only** (A26 / A48 / A63). Hub is read-only.

### 4.3 Snapshot-after-subscribe

Immediately after a successful `Subscribe`, the server sends, in this order:

1. `ops.header` if `header` or any topic was subscribed (cheap; keeps the strip honest).
2. Topic snapshot:
   - `scores` → `scoring.summary` + optional `trader.score.batch` of **currently SHADOW / WATCH / LIVE_CANDIDATE / LIVE / RISK_BLOCKED** only (not all ~5k `INSUFFICIENT_DATA` rows).
   - `fix` → one `fix.session` per qualifier (`QUOTE`, `TRADE`).
   - `quotes` → one `quote.xauusd` (nulls if unmapped / no print).
   - `alerts` → `alert.snapshot` of **open** alerts (max 50, newest first).
3. Then live deltas.

Client algorithm: `Subscribe` → apply snapshots → apply deltas. On reconnect, invalidate TanStack keys (`['traders']`, `['fix']`, `['overview']`, …) and GET REST; do not invent a gap-fill from `seq`.

### 4.4 Cadence and coalescing (mandatory)

Backfill of ~5,000 accounts must not melt the browser.

| Family | Emit when | Coalesce | Do not |
|---|---|---|---|
| Scores | Official `EXPANDING` / `FIRST3` persist; or state change | Per `(brokerId, login)` 250ms. Backfill watermark → **batch only** | One frame per deal, per feature component |
| FIX health | Session **state** change; else snapshot every **5s** while subscribed | Latest wins | Raw inbound FIX 35=* |
| Quotes | Bid/ask/stale **change**; or 250ms tick if streaming | Latest wins; **100–250ms** | Every MarketDataIncrementalRefresh |
| Alerts | Raise / clear only | Dedupe `(kind, subjectType, subjectId)` 5s | Heartbeat “still down” every second |

If only `quoteAgeMs` increased and the quote is not crossing `maxQuoteAgeMs`, skip the frame until the 250ms tick.

---

## 5. Live scores

### 5.1 Purpose

Push official `baseline.v1` results so the leaderboard, trader detail, and Scoring page do not poll. Domain calculator stays pure (A22 §11). Hub publishes **after** `trader_scores` + `trader_score_history` commit.

### 5.2 Events

| Event | When | Subscribers |
|---|---|---|
| `trader.score` | One trader official rescore **or** watched trader | Leaderboard row, Detail |
| `trader.score.batch` | Backfill watermark, startup snapshot, burst > 10 scores / 250ms | Leaderboard, Scoring |
| `trader.state` | `ResolveState` changed `prev_state` → `state` | Leaderboard, Shadow, Detail |
| `scoring.summary` | Counts-by-state or `lastRunAt` changed (coalesce 1s) | Scoring, Overview |

`EARLY_SCORE_ELIGIBLE` is an **event token**, not a `traderState` (A22 §9). It travels on `trader.score.data.lastEvent`, never as `state`.

Never emit `PROVEN_PROFITABLE`. Never emit `LIVE` / `LIVE_CANDIDATE` at `completedXauTrades == 3` (A22 I4–I5). The hub does not re-run the state machine; it projects persisted state. If a buggy publisher sends `LIVE` at `n == 3`, the **sanitizer drops the frame** and logs `SCORE_WIRE_INVARIANT` (no payload dump of features).

### 5.3 `trader.score` data

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "brokerDisplayName": "Achiever",
  "login": 6100421,
  "completedXauTrades": 7,
  "scoreVersion": "baseline.v1",
  "window": "EXPANDING",
  "earlyQualityScore": 71.00,
  "earlyScore": 71.00,
  "behaviorScore": 68.00,
  "riskScore": 22.00,
  "mlProbability": null,
  "state": "SHADOW",
  "prevState": "WATCH",
  "lastEvent": "RESCORED",
  "severeRisk": false,
  "flags": {
    "martingale": false,
    "averagingDown": true,
    "lotEscalation": false
  },
  "featureQuality": "EXACT",
  "mfeMaeUsed": true,
  "lastScoredAt": "2026-08-18T11:40:00.000Z",
  "trigger": "TRADE_7_COMPLETE"
}
```

| Field | Rule |
|---|---|
| `brokerId` + `login` | Compound identity. Login is **never** globally unique (§10) |
| `earlyScore` | Wire alias of `earlyQualityScore`. Same number. Both present so A26 name and A22 name cannot drift |
| Scores | `0–100`, two decimal places, no NaN/Inf |
| `mlProbability` | `null` in v1 |
| `window` | `EXPANDING` \| `FIRST3` \| `PROVISIONAL`. `PROVISIONAL` (`n < 3`) **must not** go to the leaderboard group; only `WatchTrader` |
| `lastEvent` | `EARLY_SCORE_ELIGIBLE` \| `RESCORED` \| `STATE_CHANGED` |
| `trigger` | Stable token, e.g. `TRADE_3_COMPLETE`, `TRADE_N_COMPLETE`, `MANUAL_RESCORE`, `BACKFILL_WATERMARK` |
| `flags` | Current **active** flags only. Do not send `evidence_json` |
| `featureQuality` | `EXACT` \| `APPROXIMATE` \| `UNAVAILABLE` |

**Omit:** component breakdown (`martingale_risk`, …), raw `NET` $, account balance, challenge plan, destination P&L, MFE/MAE **values** (quality flag is enough on the live tile). Detail charts stay on REST `GET /traders/{brokerId}/{login}/scores`.

### 5.4 `trader.score.batch` data

```json
{
  "reason": "BACKFILL_WATERMARK",
  "count": 2,
  "items": [
    { "brokerId": "…", "login": 6100421, "earlyQualityScore": 71.00, "earlyScore": 71.00, "riskScore": 22.00, "behaviorScore": 68.00, "state": "SHADOW", "lastScoredAt": "2026-08-18T11:40:00.000Z" }
  ]
}
```

`items.length` ≤ **50**. Larger watermarks → multiple batches. `reason`: `SNAPSHOT` \| `BACKFILL_WATERMARK` \| `COALESCE`.

### 5.5 `trader.state` data

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "login": 6100421,
  "state": "RISK_BLOCKED",
  "prevState": "SHADOW",
  "reason": "R3:FLAG_MARTINGALE",
  "n": 6,
  "asOf": "2026-08-18T12:10:00.000Z",
  "actor": "system:baseline.v1"
}
```

`state` / `prevState` are exactly A22 / domain `TraderState` strings:

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

`actor` is `system:baseline.v1` or `user:<uuid>`. Never an email if that is not already on `GET /auth/me` for this role. Never a credential.

Manual `PATCH /traders/{brokerId}/{login}/state` (A63) also emits `trader.state` after audit commit. Hub is not the write path.

### 5.6 `scoring.summary` data

Subset of `GET /api/v1/scoring/summary` (A63 §5.6):

```json
{
  "baseline": { "name": "baseline.v1" },
  "lastRunAt": "2026-08-18T11:40:00.000Z",
  "scoredCount": 612,
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
```

No weights, no training-set dump, no feature store.

### 5.7 Produce rules

1. Publisher is the `score-update` outbox handler (A41), after the unique `(broker_id, login, as_of, n, score_version, window)` insert.
2. Redelivery: same idempotency key → **do not** emit a second hub frame (or emit with the same `correlationId` and let the client upsert).
3. Backfill: **no** per-row `trader.score` to topic `scores`. Batch or `WatchTrader` only (A41 §11).
4. `INSUFFICIENT_DATA` provisional diagnostics never rank and never broadcast.

---

## 6. FIX health

### 6.1 Purpose

Independent QUOTE and TRADE cards (§52, A25 §2). The header strip reads QUOTE health, TRADE health, and “logged on ≠ ready to send.”

Domain entity today: `D:\Prop\src\Domain\Entities\FixSessionState.cs` (`Qualifier`, `Status`, host, port, CompIDs, seq, reconnects, `OwnerHeld`, `LastError`). Wire status follows **A63** (richer than the current C# enum). When the enum is extended, map; do not invent a third vocabulary on the hub.

### 6.2 Events

| Event | When | Subscribers |
|---|---|---|
| `fix.session` | State / seq / XAU map / lease change; 5s snapshot | FIX page, header |
| `fix.health` | Rolled-up QUOTE + TRADE for the strip (coalesce 1s) | `ops.header` consumers; Health |

One `fix.session` frame = **one** qualifier. Never merge QUOTE and TRADE into one state machine.

### 6.3 `sessionStatus` (wire)

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

Map from current domain `FixSessionStatus` (until the enum is widened):

| Domain (`FixSessionStatus`) | Wire |
|---|---|
| `Disconnected` | `DOWN` |
| `Connecting` | `CONNECTING` |
| `LogonSent` | `LOGON_SENT` |
| `LoggedOn` | `LOGGED_ON` |
| `Reconciling` | `RECONCILING` |
| `ReadyForMarketData` | `READY_FOR_MARKET_DATA` |
| `ReadyForExecution` | `READY_FOR_EXECUTION` |
| `LogoutSent` | `LOGOUT_SENT` |
| `Error` | `ERROR` |

Flag-off sessions report `DISABLED`, not `DOWN`.

### 6.4 `fix.session` data

```json
{
  "session": "QUOTE",
  "host": "live-us-eqx-01.p.c-trader.com",
  "sslPort": 5211,
  "useSsl": true,
  "senderCompId": "live.pepperstone.1369850",
  "targetCompId": "cServer",
  "sessionQualifier": "QUOTE",
  "connected": true,
  "loggedOn": true,
  "sessionStatus": "READY_FOR_MARKET_DATA",
  "lastInboundAt": "2026-08-18T12:00:00.200Z",
  "lastOutboundAt": "2026-08-18T12:00:00.050Z",
  "sequence": { "nextSender": 10442, "nextTarget": 18801 },
  "reconnectCount": 1,
  "lastHeartbeatAt": "2026-08-18T11:59:55.000Z",
  "lastTestRequestAt": null,
  "leaseHeld": true,
  "secretConfigured": true,
  "errors": [
    { "at": "2026-08-18T11:00:00.000Z", "code": "LOGON_REJECTED", "text": "invalid credentials" }
  ],
  "xauusd": {
    "mapped": true,
    "instrumentId": "185",
    "bid": 2401.12,
    "ask": 2401.28,
    "quoteAgeMs": 140,
    "spread": 0.16
  }
}
```

TRADE frame: same header fields; **no** `xauusd` (quotes are not TRADE’s job). Add:

```json
{
  "session": "TRADE",
  "sessionStatus": "DISABLED",
  "executionEnabled": false,
  "readyForExecution": false,
  "openOrders": 0,
  "openDestinationPositions": 0,
  "unknownExecutionStates": 0,
  "lastExecutionReport": {
    "at": null,
    "execType": null,
    "clOrdId": null,
    "ordStatus": null
  },
  "lastReconciliation": {
    "at": null,
    "status": "NEVER"
  }
}
```

v1: `executionEnabled` **must be false**. `REAL_COPY_EXECUTION_ENABLED` default false (A25 §6, A63). Do not paint TRADE `HEALTHY` for live send when the session is only `LOGGED_ON`.

`lastReconciliation.status`: `READY_FOR_EXECUTION` \| `BLOCKED_INCONSISTENT` \| `BLOCKED_PENDING_RECON` \| `BLOCKED_STALE` \| `NEVER` (A47).

`errors[].text` is cTrader `Text (58)` **after** redaction. If tag 554 / `Password=` / `RawData` appears, replace the entire `text` with `***` and keep `code`.

### 6.5 `fix.health` data (header)

```json
{
  "quote": {
    "status": "HEALTHY",
    "sessionStatus": "READY_FOR_MARKET_DATA",
    "loggedOn": true,
    "quoteAgeMs": 140,
    "detail": "logged on; XAU mapped"
  },
  "trade": {
    "status": "DISABLED",
    "sessionStatus": "DISABLED",
    "loggedOn": false,
    "readyForExecution": false,
    "executionEnabled": false,
    "detail": "not required for first useful"
  }
}
```

`status` (`healthStatus`): `HEALTHY` \| `DEGRADED` \| `UNHEALTHY` \| `STALE` \| `UNKNOWN` \| `DISABLED`.

| QUOTE `status` | When |
|---|---|
| `HEALTHY` | Logged on **and** XAU mapped **and** `quoteAgeMs ≤ maxQuoteAgeMs` |
| `STALE` | Logged on but `quoteAgeMs > maxQuoteAgeMs` (or no print yet after map) |
| `DEGRADED` | Connecting / reconnecting / mapped=false |
| `UNHEALTHY` | `ERROR` / `DOWN` while the session is enabled |
| `DISABLED` | Flag off |
| `UNKNOWN` | Worker never reported |

| TRADE `status` | When |
|---|---|
| `DISABLED` | Flag off or first-useful (A63) |
| `HEALTHY` | `READY_FOR_EXECUTION` **and** lease held **and** `unknownExecutionStates == 0` |
| `DEGRADED` | `LOGGED_ON` / `RECONCILING` / unknowns > 0 / execution flag false while session up |
| `UNHEALTHY` | `ERROR` / `DOWN` / `BLOCKED_INCONSISTENT` / lease lost while enabled |

**Heartbeat absence on QUOTE while quotes stream is not UNHEALTHY** (A25). `quoteAgeMs` is the liveness input.

### 6.6 FIX fields that must never appear

| Forbidden | Why |
|---|---|
| `password`, `fixPassword`, `CTRADER_FIX_PASSWORD` | §52 / §55 |
| `rawData`, tag 96 / 554 / 925 / 91 / 89 / 1401 / 1403 | A76 |
| `senderSubId` | A63: omit entirely in v1 (broker-issued; not needed on the card) |
| Full Logon / Logout raw | Replayable; 553+554 pair |
| `ownerInstance` (hostname + pid + boot uuid) | Unnecessary on the glass; `leaseHeld` is enough |
| Fencing token | Internal send guard, not a dashboard field |
| Username tag 553 as `username` | Use destination account **number** only if already on REST; never next to a password field |

Allowed: host, SSL port, `useSsl`, `senderCompId`, `targetCompId`, qualifier `QUOTE`/`TRADE`, seq ints, reconnect count, `secretConfigured`, instrument id, bid/ask/age/spread, `executionEnabled`, open counts, last ER **ids** (`clOrdId` string), reconciliation status.

`Username` 553 is the numeric login `1369850`. It **may** appear as destination account on REST; on the hub prefer omitting it from `fix.session` (CompID already contains it). Never name a property `username` or `accountPassword`.

---

## 7. Quotes

### 7.1 Purpose

cTrader **QUOTE** session is the destination price for shadow mark-to-market, slippage checks, and pre-trade gates (A25 §7.2, A24, A63). Source MT5 last-deal is **not** a quote.

Entity today: `DestinationQuoteSnapshot` (`CanonicalSymbol`, `VenueInstrumentId`, `Bid`, `Ask`, `ReceivedAt`, `VenueTimestamp`) — `D:\Prop\src\Domain\Entities\DestinationQuote.cs`.

v1 symbol: **XAUUSD only**. Do not stream a book of every SecurityList row.

### 7.2 Events

| Event | When |
|---|---|
| `quote.xauusd` | Bid/ask change, map change, or coalesced age tick |
| `quote.stale` | Crossing `maxQuoteAgeMs` **or** returning fresh. Also raise/clear alert `QUOTE_STALE` (§8) |

### 7.3 `quote.xauusd` data

```json
{
  "canonicalSymbol": "XAUUSD",
  "instrumentId": "185",
  "mapped": true,
  "bid": 2401.12,
  "ask": 2401.28,
  "mid": 2401.20,
  "spread": 0.16,
  "quoteAgeMs": 140,
  "maxQuoteAgeMs": 1500,
  "stale": false,
  "receivedAt": "2026-08-18T12:00:00.000Z",
  "venueTimestamp": "2026-08-18T12:00:00.000Z",
  "sessionStatus": "READY_FOR_MARKET_DATA"
}
```

| Field | Rule |
|---|---|
| `instrumentId` | **string**. Discovered from SecurityList tag 55 (A25 §7.1). Never hardcoded from another account |
| `mapped` | `false` ⇒ bid/ask/spread/mid/age are `null` |
| `mid` | `(bid + ask) / 2` when both present; else `null` |
| `spread` | `ask - bid`; `null` if either missing; never negative (if venue crossed, still send the raw spread and set `stale: true`) |
| `quoteAgeMs` | `nowUtc - receivedAt` in whole ms. **Our clock**, not Heartbeat |
| `maxQuoteAgeMs` | Current risk limit (A26 default 1500). Sent so the UI does not hardcode |
| `stale` | `!mapped` OR `quoteAgeMs == null` OR `quoteAgeMs > maxQuoteAgeMs` |
| `venueTimestamp` | Venue time if present; else `null`. Do not invent |

Unmapped / no print (honest empty):

```json
{
  "canonicalSymbol": "XAUUSD",
  "instrumentId": null,
  "mapped": false,
  "bid": null,
  "ask": null,
  "mid": null,
  "spread": null,
  "quoteAgeMs": null,
  "maxQuoteAgeMs": 1500,
  "stale": true,
  "receivedAt": null,
  "venueTimestamp": null,
  "sessionStatus": "DOWN"
}
```

### 7.4 `quote.stale` data

```json
{
  "canonicalSymbol": "XAUUSD",
  "stale": true,
  "quoteAgeMs": 2400,
  "maxQuoteAgeMs": 1500,
  "reason": "QUOTE_STALE"
}
```

`reason`: `QUOTE_STALE` \| `QUOTE_UNAVAILABLE` \| `QUOTE_FIX_UNAVAILABLE` \| `FRESH` (clear).

### 7.5 Produce rules

1. Only `CTraderQuoteSession` / quote persist path writes these events.
2. Persist `destination_quotes` first; then last-value Redis; then publish.
3. Do not send TRADE incremental, depth, or raw `35=X` / `35=W` bodies.
4. Do not send source MT5 ticks on this topic.
5. Shadow / risk consumers treat `stale: true` as **no new live copy** (A25 §6.3, A53). The hub does not make that decision; it reports the same numbers REST would.

---

## 8. Alerts

### 8.1 Purpose

Operator-visible, durable, allow-listed notifications. Source: outbox `notification-event` → `system_events` → Redis → hub (A41 §10.5, A20 `system_events`).

Alerts are **not** the audit log. Privileged mutations still write `audit_logs` and may **also** raise an alert.

v1 hub is **not** the ack channel. Open alerts stay open until a later REST `POST /api/v1/alerts/{id}/ack` exists (RiskManager+). Until that route ships, `alert.cleared` is **system-only** (condition healed).

### 8.2 Events

| Event | When |
|---|---|
| `alert.raised` | New open row (or severity upgrade) |
| `alert.cleared` | Condition healed or (later) audited ack |
| `alert.snapshot` | After `Subscribe(["alerts"])` — open alerts only |

### 8.3 Shared alert object

```json
{
  "alertId": "cc888888-0000-4000-8000-000000000700",
  "kind": "QUOTE_STALE",
  "severity": "warning",
  "title": "Destination XAUUSD quote stale",
  "body": "quoteAgeMs 2400 exceeded maxQuoteAgeMs 1500",
  "code": "QUOTE_STALE",
  "subjectType": "fix_session",
  "subjectId": "QUOTE",
  "brokerId": null,
  "login": null,
  "dedupeKey": "QUOTE_STALE:fix_session:QUOTE",
  "openedAt": "2026-08-18T12:00:02.000Z",
  "clearedAt": null,
  "correlationId": "b7c1e2d3-0000-4000-8000-000000000011"
}
```

| Field | Rule |
|---|---|
| `severity` | `info` \| `warning` \| `critical` (A41) |
| `kind` / `code` | Closed set §8.4. Do not free-type worker exceptions |
| `title` / `body` | Human text **after** sanitizer. No host credentials, no stack traces, no FIX raw |
| `subjectType` | `fix_session` \| `broker` \| `trader` \| `quote` \| `risk` \| `reconciliation` \| `outbox` \| `system` |
| `brokerId` + `login` | Set when the subject is a source trader; else null |
| `dedupeKey` | `{kind}:{subjectType}:{subjectId}`. Re-raise within 5s is a no-op |

`alert.snapshot`:

```json
{
  "generatedAt": "2026-08-18T12:00:05.000Z",
  "openCount": 1,
  "items": [ { "alertId": "…", "kind": "QUOTE_STALE", "severity": "warning", "title": "…", "openedAt": "…" } ]
}
```

`items.length` ≤ **50**. `openCount` is the real total (may be > 50). Client GETs REST if it needs the rest (when that list exists).

### 8.4 Closed `kind` set (v1)

Do not invent kinds in publishers. Unknown kind → persist `system_events` with `kind=UNKNOWN_DROPPED` internally; **do not** send to the hub.

| Kind | Severity default | Raise when | Clear when |
|---|---|---|---|
| `FIX_QUOTE_DOWN` | critical | QUOTE enabled and `DOWN`/`ERROR` | QUOTE `LOGGED_ON` or better |
| `FIX_TRADE_DOWN` | critical | TRADE enabled and `DOWN`/`ERROR` | TRADE `LOGGED_ON` or better |
| `FIX_LOGON_FAILED` | critical | Logout 35=5 after Logon, or no response | Successful Logon |
| `FIX_LEASE_LOST` | critical | TRADE lease lost while enabled | New owner held + reconciled |
| `QUOTE_STALE` | warning | `quoteAgeMs > maxQuoteAgeMs` or unmapped while QUOTE up | Fresh mapped quote |
| `QUOTE_UNAVAILABLE` | warning | No print / session disabled while shadow needs a mark | First usable print |
| `TRADE_NOT_READY` | warning | TRADE up but not `READY_FOR_EXECUTION` | Gate passes |
| `EXECUTION_STATE_UNKNOWN` | critical | Any `sent_ack_unknown` | Reconciled (adopted or `not_on_venue`) |
| `RECONCILIATION_ISSUE` | warning | New OPEN dest/source issue | Issue terminal **and** no other OPEN of same type if we key by type+id |
| `MT5_BROKER_DOWN` | critical | Source connector down | `CONNECTED` |
| `MT5_SOURCE_STALE` | warning | Ingest lag over threshold | Lag recovered |
| `STOP_NEW_EXECUTION` | warning | Latch turned **on** | Latch cleared |
| `EMERGENCY_FLATTEN` | critical | Flatten **requested** (not “done”) | Flatten cycle terminal (success/partial/failed) — still show REST |
| `REAL_EXECUTION_ENABLED` | critical | Flag false → true | Flag true → false |
| `RISK_REJECTED` | info | Live/shadow reject with codes in §8.5 | n/a (ephemeral; auto-expire 15 min) or no clear |
| `TRADER_RISK_BLOCKED` | warning | State → `RISK_BLOCKED` | State leaves `RISK_BLOCKED` |
| `TRADER_DISQUALIFIED` | warning | State → `DISQUALIFIED` | Audited reclaim |
| `OUTBOX_POISON` | critical | Row moved to poison | Manual replay processed |
| `DATABASE_UNAVAILABLE` | critical | API/worker cannot reach Postgres | Ready check ok |
| `SCORING_FAILED` | warning | Official score handler failed (no numbers invented) | Next successful run |

`ml.not_in_use` is **not** an alert in v1 (A52). Do not spam “ML unavailable.”

### 8.5 `RISK_REJECTED` body codes (allow-listed)

Copy A23 / A63 reason codes into `code` / `body` only:

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
TRADE_FIX_UNAVAILABLE
EXECUTION_STATE_UNKNOWN
```

Include `brokerId`, `login`, `copyIntentId` (uuid string). Never include requested raw FIX, account password, or destination credentials.

### 8.6 Header integration

`ops.header` (A26) must include a **count**, not the full alert list:

```json
{
  "flags": {
    "realCopyExecutionEnabled": false,
    "stopNewExecution": false,
    "emergencyFlattenAvailable": false
  },
  "health": {
    "mt5Ingestion": { "status": "UNKNOWN", "detail": "not started" },
    "fixQuote": { "status": "UNKNOWN", "detail": "not started" },
    "fixTrade": { "status": "DISABLED", "detail": "not required for first useful" }
  },
  "alerts": {
    "openCount": 0,
    "criticalCount": 0,
    "highestSeverity": null
  }
}
```

`emergencyFlattenAvailable` is **false** in first useful (A63). Do not advertise a flatten button through the hub.

---

## 9. Related events (names frozen, payloads not re-specified)

Keep A26/A63 names so the React query invalidation table stays one list:

| Event | Invalidate | Notes |
|---|---|---|
| `ops.header` | `['overview']` header slice | §8.6 |
| `overview.updated` | `['overview']` | Full overview `data` subset |
| `broker.health` | `['brokers']`, `['health']` | One broker list row; **masked** login only |
| `health.updated` | `['health']` | `GET /api/v1/health` subset |
| `shadow.portfolio` | `['shadow']`, `['overview']` | `{ pnl, openCount }` only |
| `risk.state` | `['risk']`, header | `{ stopNewExecution, emergencyFlattenAvailable: false, realCopyExecutionEnabled: false }` |
| `reconciliation.issue` | `['reconciliation']` | One issue object; no FIX raw |

Payloads are **subsets** of the corresponding GET DTO. Never larger.

---

## 10. Never send secrets (hub-specific restatement)

Same law as A26 §3, A63 §2, A50, A76. Applies to **every** frame, including `hub.error`, snapshots, and Redis last-value.

### 10.1 Denylist (names and values)

Drop any property whose name matches:

```text
(?i)(password|passwd|secret|pwd|rawdata|connectionstring|privatekey|proxyuser|authorization|accesstoken|refreshtoken)
```

Redact values containing `Password=`, `554=`, `96=`. Replacement token is the three-character literal `***` (A76). Do not pad. Do not hash into the frame.

| Class | Examples |
|---|---|
| MT5 Manager password | `mt5Password`, `MT5_PASSWORD` |
| Proxy credentials | `proxyUsername`, `proxyPassword` |
| FIX / cTrader password | `fixPassword`, `CTRADER_FIX_PASSWORD`, tag 554 / 96 / 925 |
| Raw Logon | any `8=FIX.4.4` blob |
| DB / Redis | connection strings, Redis AUTH |
| JWT | access/refresh tokens in payloads |
| `senderSubId` | omit in v1 |

### 10.2 Fail closed

Order:

1. Build allow-listed DTO (never serialize `CTraderFixOptions`, `IConfiguration`, EF entities, `FixSessionState` as-is — `SenderSubId` lives on that entity).
2. Name denylist.
3. Value redaction.
4. Invariants (`n==3` ⇒ state ∉ {`LIVE`,`LIVE_CANDIDATE`}; TRADE `executionEnabled==false` while v1).
5. If a secret remains → **drop the frame**, increment `ops_hub_dropped_secret_total`, log `HUB_SECRET_BLOCKED` **without** the payload.

Do not send a half-redacted object.

### 10.3 What is safe

Display names, hosts, ports, SSL flags, `senderCompId` / `targetCompId`, qualifier, `secretConfigured`, masked manager login (`**27` — last two digits, A26), instrument IDs, bid/ask, health enums, scores 0–100, trader login numbers, ClOrdID strings, correlation ids.

---

## 11. Auth, RBAC, rate limits

| Rule | Contract |
|---|---|
| Connect | JWT valid, role in {ReadOnly, Analyst, RiskManager, SuperAdmin} |
| Subscribe | All v1 topics in §4.1 except nothing here is Audit-only. **No** `audit` topic in v1 |
| `WatchTrader` | Same as `GET /traders/{brokerId}/{login}` (ReadOnly+) |
| Writes | None |
| Per-connection | Max 16 topics, 20 trader watches |
| Per-user | 5 hub connections; extra `429` then close |
| Server fan-out | Quotes ≤ 8/s/connection; scores ≤ 20/s/connection; alerts ≤ 5/s/connection |

Wrong role / expired JWT mid-session: complete current send, then `hub.error` `UNAUTHENTICATED` and disconnect. Do not leave a zombie ReadOnly socket after logout — `POST /auth/logout` should drop connections with that `jti` when feasible; if not, short JWT TTL + client stop.

---

## 12. Client binding (`apps/web`)

When a later wave retargets the stub (`A62`):

```text
wrong:  ${VITE_API_URL}/hubs/dashboard
right:  ${VITE_API_URL}/hubs/ops
        + accessTokenFactory from the in-memory access token
        + withAutomaticReconnect
```

Do not put the access token in logs. Do not put FIX/MT5 secrets in `VITE_*`.

Suggested handlers (names = `event`):

```text
on('ops.header',        invalidate header + flags)
on('trader.score',      upsert leaderboard row; if watching, detail)
on('trader.score.batch', upsert many)
on('trader.state',      upsert state cell)
on('scoring.summary',   Scoring page)
on('fix.session',       FIX card + header)
on('fix.health',        header)
on('quote.xauusd',      FIX / Shadow / Risk mark)
on('quote.stale',       banner)
on('alert.raised',      bell + toast if critical)
on('alert.cleared',     remove from open list)
on('alert.snapshot',    replace open list)
```

TanStack keys (A26 §13): `['overview']`, `['brokers']`, `['traders', filters]`, `['trader', brokerId, login]`, `['fix']`, `['risk']`, `['reconciliation']`. Add `['alerts']`, `['quotes','XAUUSD']`, `['scoring']`.

Zustand stores **connection banner + confirm-modal only**, not scores or quotes.

---

## 13. TypeScript types (normative for the client)

```ts
export type HealthStatus =
  | 'HEALTHY' | 'DEGRADED' | 'UNHEALTHY' | 'STALE' | 'UNKNOWN' | 'DISABLED';

export type TraderState =
  | 'INSUFFICIENT_DATA' | 'EARLY_SCORE' | 'WATCH' | 'SHADOW'
  | 'LIVE_CANDIDATE' | 'LIVE' | 'PAUSED' | 'RISK_BLOCKED' | 'DISQUALIFIED';

export type FixSessionStatus =
  | 'DISABLED' | 'CONNECTING' | 'LOGON_SENT' | 'LOGGED_ON' | 'RECONCILING'
  | 'READY_FOR_MARKET_DATA' | 'READY_FOR_EXECUTION' | 'BLOCKED_INCONSISTENT'
  | 'LOGOUT_SENT' | 'RECONNECTING' | 'ERROR' | 'DOWN';

export type AlertSeverity = 'info' | 'warning' | 'critical';

export interface OpsEnvelope<T> {
  v: 1;
  event: string;
  at: string;
  seq: number | string;
  correlationId: string | null;
  topic: string;
  data: T;
}

export interface TraderScoreLive {
  brokerId: string;
  login: number;
  completedXauTrades: number;
  scoreVersion: string;
  window: 'EXPANDING' | 'FIRST3' | 'PROVISIONAL';
  earlyQualityScore: number;
  earlyScore: number;
  behaviorScore: number;
  riskScore: number;
  mlProbability: number | null;
  state: TraderState;
  lastEvent: 'EARLY_SCORE_ELIGIBLE' | 'RESCORED' | 'STATE_CHANGED';
  lastScoredAt: string;
}

export interface QuoteXauusdLive {
  canonicalSymbol: 'XAUUSD';
  instrumentId: string | null;
  mapped: boolean;
  bid: number | null;
  ask: number | null;
  mid: number | null;
  spread: number | null;
  quoteAgeMs: number | null;
  maxQuoteAgeMs: number;
  stale: boolean;
  receivedAt: string | null;
  sessionStatus: FixSessionStatus;
}

export interface AlertItem {
  alertId: string;
  kind: string;
  severity: AlertSeverity;
  title: string;
  body: string;
  code: string;
  subjectType: string;
  subjectId: string;
  brokerId: string | null;
  login: number | null;
  openedAt: string;
  clearedAt: string | null;
  correlationId: string | null;
}
```

Do not check these types into product from this task.

---

## 14. Metrics (hub)

Names stay in the A50 frozen style (no `ti_` prefix):

```text
ops_hub_connections
ops_hub_disconnects_total
ops_hub_subscribe_total          # label: topic
ops_hub_frames_sent_total        # label: event
ops_hub_frames_coalesced_total
ops_hub_dropped_secret_total
ops_hub_dropped_invariant_total
ops_hub_relay_lag_ms
```

**Forbidden labels:** login, user id, request id, ClOrdID, password hashes (A50 / A18). Topic + event name only.

---

## 15. Test acceptance (when a later wave implements)

1. Unauthenticated negotiate → 401. No frames.
2. ReadOnly can `Subscribe(['scores','fix','quotes','alerts'])` and receives snapshots.
3. `Subscribe(['nope'])` → `hub.error` `VALIDATION_FAILED`.
4. Client method `SetPassword` does not exist; raw invoke ignored.
5. `trader.score` at `completedXauTrades == 3` never has `state` `LIVE` or `LIVE_CANDIDATE`.
6. `mlProbability` is JSON `null`.
7. `earlyQualityScore` and `earlyScore` are identical and in `0–100`.
8. Backfill of 100 scores produces `trader.score.batch` (≤50/frame), not 100 `trader.score` on `scores`.
9. QUOTE `fix.health.status == STALE` when logged on and `quoteAgeMs > maxQuoteAgeMs`.
10. TRADE v1 frame has `executionEnabled: false`.
11. Quote stream coalesces to ≤ 8/s.
12. Frame built from an object that includes `Password` / `554=` is **dropped**; client sees nothing; metric increments.
13. Redis down: score still persists; REST GET shows it; hub may be quiet.
14. Workers have no `MapHub`.
15. `apps/web` uses `/hubs/ops`, not `/hubs/dashboard`.
16. Alert `kind` outside §8.4 never reaches the client.
17. `senderSubId`, `fixPassword`, raw Logon absent from every fixture payload.

---

## 16. Implementation checklist (coding task, not this file)

1. Keep Web SDK SignalR; do not rely on `SignalR.Common` as the host (A06).
2. Add `OpsHub` + JWT + `ReadOnlyPlus`.
3. Allow-list DTOs + sanitizer (shared with REST).
4. Redis `ops:events` subscriber in `apps/api` only.
5. Score handler publishes after persist (A41 `score-update` / `notification-event`).
6. FIX worker publishes session + quote last-value (no raw FIX).
7. Alert kinds from `system_events` only.
8. Retarget `apps/web/src/api/signalr.ts` to `/hubs/ops`.
9. Green §15 tests.
10. Delete `/weatherforecast` in the same API wave (A63) — out of scope for this document.

---

## 17. Non-goals

- Multiple hubs or `/hubs/dashboard`.
- MessagePack, Kafka, Redis Streams-as-SoT.
- Streaming all 5,000 `INSUFFICIENT_DATA` scores.
- Streaming non-XAU quotes.
- Hub mutations (orders, passwords, flatten, ack).
- ML live probabilities.
- Painting TRADE as ready to send in v1.
- Hand-written MQ5. Product source is **not** modified by this agent.

---

## 18. Event index (v1 must implement)

| Direction | Name | Topic | Family |
|---|---|---|---|
| S→C | `trader.score` | `scores` + `trader:{id}` | Scores |
| S→C | `trader.score.batch` | `scores` | Scores |
| S→C | `trader.state` | `scores` + `trader:{id}` | Scores |
| S→C | `scoring.summary` | `scores` | Scores |
| S→C | `fix.session` | `fix` | FIX health |
| S→C | `fix.health` | `fix`, `header` | FIX health |
| S→C | `quote.xauusd` | `quotes` | Quotes |
| S→C | `quote.stale` | `quotes` | Quotes |
| S→C | `alert.raised` | `alerts` | Alerts |
| S→C | `alert.cleared` | `alerts` | Alerts |
| S→C | `alert.snapshot` | `alerts` | Alerts |
| S→C | `ops.header` | `header` | Header / alerts count |
| S→C | `hub.error` | `control` | Control |
| C→S | `Subscribe` | — | Control |
| C→S | `Unsubscribe` | — | Control |
| C→S | `WatchTrader` | — | Scores |
| C→S | `UnwatchTrader` | — | Scores |

---

*End of A97. Product source was not modified. No secrets in this file.*
