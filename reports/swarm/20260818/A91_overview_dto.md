# A91 — Overview page health tiles (§47) and required API DTOs

| Field | Value |
|---|---|
| Agent | A91 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A91_overview_dto.md` |
| Source of truth | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§47 Overview Page** |
| Supporting architecture | §5 stack, §10 compound identity, §22 trader states, §24 shadow P&L, §27–§28 FIX sessions, §31 destination quotes, §40–§41 flags / kill switches, §42–§43 / §54 reconciliation, §46 nav, §52 FIX cards, §53 risk, §55 secrets, §58 metrics, §59 RBAC, §62 stale-source, §69 first useful |
| Binding siblings (do not contradict) | `A06` §3.2/§4.3, `A20` tables, `A24` shadow P&L, `A25` session states, `A26` §2 / §6.1 / §7, `A47` recon, `A48` kill switches, `A49` flags, `A53` failure/health, `A58` broker registry, `A62` §7.2 / §10.1, `A63` §4.1 |
| Product source edited | **No.** This file is the only write. |

This is the implementable contract for the §47 Overview **tile grid**, the three **health tiles**, the shared header-strip chips, and the **one-call** `GET /api/v1/overview` DTO. It does not implement the endpoint.

---

## 0. Verdict (measured 2026-08-18)

| Surface | Path | State |
|---|---|---|
| Architecture §47 | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1762–1790 | Binding tile list |
| API | `D:\Prop\apps\api\Program.cs` | Stock `GET /weatherforecast` only. **No** `/api/v1/overview` |
| Application DTO | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` `OverviewDto` | **EXISTS — incomplete.** Flat ints/decimals + three `bool` health flags. Missing envelope, `generatedAt`, nested objects, free margin / margin level, structured health, flags, quality, per-broker stale |
| Query port | same file `IDashboardQueries.GetOverviewAsync` | Declared, **no implementation** |
| React type | `D:\Prop\apps\web\src\types\index.ts` `Overview` | **EXISTS — wrong.** `fixHealthy: boolean` only; no three health tiles |
| React hook | `D:\Prop\apps\web\src\api\hooks.ts` `useOverview` | Calls `/api/overview` (missing `v1`) |
| Overview page | `apps/web/src/pages` | **MISSING** (route exists in `App.tsx`) |

Do not treat the current `OverviewDto` bools as the contract. A boolean cannot express `STALE`, `DEGRADED`, `UNKNOWN`, or “TRADE session not started.”

---

## 1. Architecture §47 (verbatim tile list)

Quoted in full:

```text
Total MT5 accounts
Connected source brokers

XAUUSD traders
Traders with >= 3 completed trades

Watch
Shadow
Live candidates
Live copied
Risk blocked

Shadow P&L
Destination real P&L

Current XAU gross exposure
Current XAU net exposure
Destination free margin
Destination margin level

MT5 ingestion health
FIX quote health
FIX trade health
```

That is **18 tiles**. The last **three** are health tiles. The first **15** are KPI tiles. All 18 must render from **one** GET. The UI must not invent a 19th “overall green” score that hides a red health tile.

§47 does **not** list kill-switch / execution flags. A26 §5.3 / A62 §7.2 still require those chips on the **global header**, sourced from the same GET (`flags` + `health`). They are not extra Overview tiles; they must still be on the DTO.

---

## 2. Scope and non-goals

### 2.1 In scope

1. Tile inventory, layout, click-through, empty/null painting.
2. The three health tiles: status enum, reason codes, evaluation tables, per-component extras.
3. Wire DTO (JSON), C# records, TypeScript types.
4. Query sources (A20 tables + worker heartbeats). Honest first-useful zeros / `null` / `UNKNOWN`.
5. SignalR `ops.header` + `overview.updated` subsets.
6. RBAC, secrets denylist, test names.

### 2.2 Out of scope

- Implementing `GET /api/v1/overview` or React pages (later coding wave).
- System Health page (`GET /api/v1/health`, A63 §5.1). Overview **summarizes**; it does not replace that page.
- Brokers / FIX / Risk / Reconciliation page DTOs (A26 §6.2–6.13).
- Mutations. Overview is **read-only**.
- ML probability, live `NewOrderSingle`, guessed instrument IDs, fabricated MFE/MAE.

---

## 3. Shared wire conventions (inherit A26 §2)

| Item | Contract |
|---|---|
| Path | `GET /api/v1/overview` |
| Auth | `Authorization: Bearer <access_token>` |
| Roles | all authenticated: SuperAdmin, RiskManager, Analyst, ReadOnly |
| Envelope | `{ "data": { … } }` — never a bare object |
| JSON names | camelCase |
| Time | ISO-8601 UTC with `Z` |
| Money / P&L | JSON `number`, server `decimal(18,2)` |
| XAU quantity | JSON `number`, server `decimal(18,8)` |
| Tickets / FIX ids | **string** (never JS `number`) |
| `brokerId` | UUID string |
| Login | JSON `number` (MT5 login integer) only on nested broker extras; not on the 18 tiles |
| Cache | `Cache-Control: no-store` |
| Correlation | echo `X-Correlation-Id` |
| Mutations | **none** |
| Idempotency | n/a |

### 3.1 Errors

Same problem envelope as A26 §2.2.

| HTTP | `error.code` | When |
|---|---|---|
| 401 | `UNAUTHENTICATED` | missing / expired token |
| 403 | `FORBIDDEN` | should not happen for ReadOnly+; reserved |
| 422 | `SECRET_FIELD_REJECTED` | denylisted query key (none expected) |
| 503 | `DEPENDENCY_UNAVAILABLE` | Postgres down (cannot compute snapshot) |

If Redis is down but Postgres is up: still **200** with `generatedAt` from the DB snapshot. Redis is not the authority for Overview counts (A20). If the MT5 / FIX workers are down: still **200** with honest `UNKNOWN` / `UNHEALTHY` health tiles. Do **not** fail the whole page because TRADE is not started.

Anonymous `/health` liveness is **not** this resource.

### 3.2 Enums used on this page

`healthStatus` (A26 §2.3) — **only** these five values, on every health tile and header chip:

```text
HEALTHY
DEGRADED
UNHEALTHY
STALE
UNKNOWN
```

`DISABLED` is **not** a `healthStatus`. A63 §4.1 used `"status": "DISABLED"` on `fixTrade`. That is a **contract bug**. Session-off is `status: UNKNOWN` + `reasonCode: SESSION_DISABLED` + `sessionEnabled: false`.

`connectionStatus` (broker extras only):

```text
CONNECTED | CONNECTING | DISCONNECTED | RECONNECTING | DISABLED
```

`traderState` (domain `TraderState`, §22) — full nine values live in `trader_states`. Overview **rollups** are five counts only (section 6.2).

`featureQuality` (§17): `EXACT | APPROXIMATE | UNAVAILABLE` — used on P&L / margin honesty.

`fixSessionStatus` (A25 / A63) — TRADE/QUOTE extras only, **not** the tile `status`:

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

---

## 4. Tile grid (layout is binding)

A62 §10.1 layout, refined:

| Row | Tiles | React module (when coded) |
|---|---|---|
| 1 | Total MT5 accounts · Connected source brokers · XAUUSD traders · Traders with ≥ 3 completed trades | `OverviewKpiGrid` |
| 2 | Watch · Shadow · Live candidates · Live copied · Risk blocked | `OverviewStateRow` |
| 3 | Shadow P&L · Destination real P&L | `OverviewPnlPair` |
| 4 | XAU gross · XAU net · Destination free margin · Destination margin level | `OverviewExposureRow` |
| 5 | MT5 ingestion · FIX QUOTE · FIX TRADE | `OverviewHealthRow` |

No chart on Overview. No sparkline required for first useful.

**Header strip (every page, including Overview):** Real-copy flag · Stop-new flag · MT5 · QUOTE · TRADE. Source: same DTO `flags` + `health`. Health row on Overview **duplicates** the three health chips and **adds** `detail` + extras. Do not invent a fourth health chip that merges the three.

Click-through (Overview itself has no mutations):

| Tile | Navigate |
|---|---|
| Total MT5 accounts | `/brokers` |
| Connected source brokers | `/brokers` |
| XAUUSD traders | `/traders` |
| ≥ 3 completed | `/traders?minCompletedXauTrades=3` |
| Watch / Shadow / Live candidates / Live copied / Risk blocked | `/traders?state=<rollup>` |
| Shadow P&L | `/shadow` |
| Destination real P&L | `/live` |
| Gross / net / margin | `/risk` |
| MT5 ingestion | `/health` (System Health) |
| FIX QUOTE | `/fix` |
| FIX TRADE | `/fix` |

---

## 5. Health tiles — the §47 three

### 5.1 Common health object

Every health tile is a `HealthTileDto`. `status` + `detail` are the A26 §6.1 minimum. A91 **requires** the extra fields so the tile can be painted without a second GET.

```json
{
  "status": "UNKNOWN",
  "reasonCode": "NOT_STARTED",
  "detail": "not started",
  "checkedAt": "2026-08-18T12:00:00.000Z",
  "lastHealthyAt": null,
  "ageMs": null
}
```

| Field | Type | Required | Rule |
|---|---|---|---|
| `status` | `healthStatus` | yes | worst-wins inside the tile (section 5.2) |
| `reasonCode` | string enum (section 5.3) | yes | machine; UI may show as caption under the dot |
| `detail` | string | yes | human, **no secrets**, ≤ 200 chars, no stack traces, no hosts with credentials |
| `checkedAt` | datetime | yes | snapshot time (usually = `generatedAt`) |
| `lastHealthyAt` | datetime \| null | yes | last time this tile was `HEALTHY`; `null` if never |
| `ageMs` | int \| null | yes | `now − last meaningful event` (ingest / inbound / heartbeat). `null` if never |

Tile-specific extras hang off the same object (`brokers`, `quote`, `trade`). Unknown extras must be ignored by old clients.

### 5.2 Worst-wins precedence (single tile)

```text
UNHEALTHY > STALE > DEGRADED > UNKNOWN > HEALTHY
```

A tile that is both stale and disconnected is `UNHEALTHY` (`reasonCode` = the disconnect reason; `detail` may mention stale). Never average three brokers into a green “2 of 3.”

### 5.3 `reasonCode` catalog (health tiles only)

Shared:

| Code | Meaning |
|---|---|
| `OK` | Tile is `HEALTHY` |
| `NOT_STARTED` | Worker / session never produced a heartbeat this process life |
| `WORKER_HEARTBEAT_STALE` | Worker process heartbeat older than `max_worker_heartbeat` |
| `SESSION_DISABLED` | Feature flag off; session object must not be connected |
| `DEPENDENCY_UNKNOWN` | Not enough durable state to classify (fail honest, not green) |

MT5 ingestion:

| Code | Typical status |
|---|---|
| `ALL_BROKERS_CONNECTED` | `HEALTHY` |
| `BROKER_DISCONNECTED` | `UNHEALTHY` if **all** expected down; `DEGRADED` if some down |
| `SOURCE_STALE` | `STALE` (all expected connected, ≥1 broker past `max_source_staleness`) |
| `PARTIAL_BROKERS` | `DEGRADED` (expected N≥2, connected 1..N-1, none stale-enough to override) |
| `NO_PUMP_FALLBACK` | `DEGRADED` or `STALE` per A53 (no-pump + poll lag) |
| `OUTBOX_BACKLOG` | `DEGRADED` when `mt5_outbox_backlog` ≥ warn threshold |

FIX QUOTE:

| Code | Typical status |
|---|---|
| `QUOTE_LOGGED_ON` | `HEALTHY` (mapped + fresh quote) |
| `QUOTE_FIX_UNAVAILABLE` | `UNHEALTHY` |
| `QUOTE_STALE` | `STALE` (`quoteAgeMs` > `max_quote_age`) |
| `XAUUSD_UNMAPPED` | `DEGRADED` (logged on, Security List has no canonical XAUUSD) |
| `QUOTE_NO_TICK` | `DEGRADED` (logged on, mapped, never received a tick) |

FIX TRADE:

| Code | Typical status |
|---|---|
| `TRADE_READY` | `HEALTHY` — Logon + `READY_FOR_EXECUTION` + no open unknowns + `REAL_COPY_EXECUTION_ENABLED=true` |
| `REAL_EXECUTION_DISABLED` | `DEGRADED` — session may be logged on / reconciled; live send is off (A26 example) |
| `NOT_RECONCILED` | `DEGRADED` — logged on, not `READY_FOR_EXECUTION` |
| `TRADE_FIX_UNAVAILABLE` | `UNHEALTHY` |
| `BLOCKED_INCONSISTENT` | `UNHEALTHY` |
| `UNRESOLVED_EXECUTION_STATE` | `UNHEALTHY` |
| `SESSION_DISABLED` | `UNKNOWN` — `CTRADER_FIX_TRADE_SESSION_ENABLED=false` or first-useful TRADE dark |

`detail` examples (allowed): `"both brokers connected"`, `"Achiever stale 95s"`, `"logged on; XAUUSD instrument 185; quote 140ms"`, `"logged on; real execution disabled"`, `"not required for first useful"`. Forbidden: passwords, RawData, connection strings, full exception text.

### 5.4 MT5 ingestion health — evaluation

**Expected brokers** = catalog rows in `brokers` with `enabled = true` (A58: Achiever + StarwaveFX). Do not count a future empty slot.

A broker is **connected** when `broker_connections` (or the worker snapshot) reports `connectionStatus=CONNECTED` **and** the worker heartbeat for that `broker_id` is within `max_worker_heartbeat`.

A broker is **source-stale** when A53 § holds:

```text
now − last_persisted_source_event_time > max_source_staleness
```

or no-pump fallback **and** deal-poll lag exceeds the same threshold. Thresholds are **configuration**, not constants in this file.

| Condition | `status` | `reasonCode` |
|---|---|---|
| No worker heartbeat, no `broker_connections` rows | `UNKNOWN` | `NOT_STARTED` |
| Worker heartbeat stale | `UNHEALTHY` | `WORKER_HEARTBEAT_STALE` |
| Expected ≥1, connected = 0 | `UNHEALTHY` | `BROKER_DISCONNECTED` |
| Connected 1..N-1 of N | `DEGRADED` | `PARTIAL_BROKERS` |
| All connected, ≥1 `source_stale` | `STALE` | `SOURCE_STALE` |
| All connected, none stale, outbox ≥ warn | `DEGRADED` | `OUTBOX_BACKLOG` |
| All connected, none stale, backlog ok | `HEALTHY` | `ALL_BROKERS_CONNECTED` / `OK` |

A53: **do not paint Live copied as an implied success while `SOURCE_STALE`.** The Live-copied KPI may still show the durable count; the MT5 tile must not be green.

`ageMs` = `now − max(lastEventAt across expected brokers)`.

#### MT5 extras (on `health.mt5Ingestion`)

```json
{
  "expectedBrokers": 2,
  "connectedBrokers": 0,
  "sourceStaleBrokerCount": 0,
  "lastEventAt": null,
  "outboxBacklog": 0,
  "brokers": [
    {
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "code": "ACHIEVER",
      "displayName": "Achiever",
      "connectionStatus": "DISCONNECTED",
      "sourceStale": false,
      "lastEventAt": null,
      "backfillLagSeconds": null,
      "dealIngestPerMinute": null
    }
  ]
}
```

**Forbidden on extras:** `password`, raw `managerLogin`, proxy user/password, connection strings.

### 5.5 FIX QUOTE health — evaluation

Independent of TRADE (architecture §27). QUOTE may be `HEALTHY` while TRADE is `UNKNOWN`.

| Condition | `status` | `reasonCode` |
|---|---|---|
| `CTRADER_FIX_ENABLED=false` or `CTRADER_FIX_QUOTE_ENABLED=false` | `UNKNOWN` | `SESSION_DISABLED` |
| No fix-worker heartbeat / no `fix_sessions` QUOTE row | `UNKNOWN` | `NOT_STARTED` |
| `DOWN` / `ERROR` / logon failed | `UNHEALTHY` | `QUOTE_FIX_UNAVAILABLE` |
| `CONNECTING` / `RECONNECTING` / `LOGON_SENT` | `DEGRADED` | `QUOTE_FIX_UNAVAILABLE` (detail = current `sessionStatus`) |
| `LOGGED_ON` or `READY_FOR_MARKET_DATA`, XAUUSD not mapped | `DEGRADED` | `XAUUSD_UNMAPPED` |
| Mapped, no bid/ask ever | `DEGRADED` | `QUOTE_NO_TICK` |
| Mapped, `quoteAgeMs` > `max_quote_age` | `STALE` | `QUOTE_STALE` |
| Mapped, fresh bid/ask | `HEALTHY` | `QUOTE_LOGGED_ON` / `OK` |

`ageMs` = `quoteAgeMs` when a quote exists, else `now − lastInboundAt`.

Instrument ID is **discovered** from Security List (§30). Never hardcode Pepperstone tag 55 / a lab `123456`. If unmapped, `instrumentId` is `null` — not a guessed string.

#### QUOTE extras (on `health.fixQuote`)

```json
{
  "sessionEnabled": true,
  "sessionStatus": "DOWN",
  "loggedOn": false,
  "xauusdMapped": false,
  "instrumentId": null,
  "bid": null,
  "ask": null,
  "spread": null,
  "quoteAgeMs": null,
  "lastInboundAt": null
}
```

Prices are destination cTrader QUOTE, **not** source MT5 last-deal (A24).

### 5.6 FIX TRADE health — evaluation

First useful version (§69) does **not** require TRADE send. The tile **must still exist**. Do not hide it. Do not paint `HEALTHY` because “we are not using TRADE.”

`REAL_COPY_EXECUTION_ENABLED` is a **flag**, not a session state (A48 / A49). It still **overlays** TRADE health so the A26 example remains true: logged on + execution off → `DEGRADED`.

| Condition | `status` | `reasonCode` |
|---|---|---|
| TRADE flag off, or first-useful TRADE dark, no socket | `UNKNOWN` | `SESSION_DISABLED` |
| No worker / no TRADE row | `UNKNOWN` | `NOT_STARTED` |
| `DOWN` / `ERROR` / no lease when flag on | `UNHEALTHY` | `TRADE_FIX_UNAVAILABLE` |
| `BLOCKED_INCONSISTENT` | `UNHEALTHY` | `BLOCKED_INCONSISTENT` |
| Open `UNRESOLVED_EXECUTION_STATE` (or any open recon issue that blocks send) | `UNHEALTHY` | `UNRESOLVED_EXECUTION_STATE` |
| `CONNECTING` / `LOGON_SENT` / `RECONNECTING` | `DEGRADED` | `TRADE_FIX_UNAVAILABLE` |
| `LOGGED_ON` or `RECONCILING`, not ready | `DEGRADED` | `NOT_RECONCILED` |
| `READY_FOR_EXECUTION`, `REAL_COPY_EXECUTION_ENABLED=false` | `DEGRADED` | `REAL_EXECUTION_DISABLED` |
| Inbound older than 2× heartbeat while supposedly logged on | `STALE` | `WORKER_HEARTBEAT_STALE` (detail: last inbound) |
| `READY_FOR_EXECUTION` **and** real copy enabled **and** no open unknowns | `HEALTHY` | `TRADE_READY` / `OK` |

Never require flatten-availability to make this tile green.

#### TRADE extras (on `health.fixTrade`)

```json
{
  "sessionEnabled": false,
  "sessionStatus": "DISABLED",
  "loggedOn": false,
  "executionEnabled": false,
  "readyForExecution": false,
  "openUnresolvedStates": 0,
  "openReconIssueCount": 0,
  "lastReconciliationAt": null,
  "lastReconciliationStatus": null,
  "lastInboundAt": null
}
```

`sessionStatus: "DISABLED"` is valid here (FIX state machine). The **tile** `status` beside it is `UNKNOWN`, not `DISABLED`.

### 5.7 Implicit API / DB (not a §47 tile)

A53 says the Overview health **strip** must not lie if the API/DB is dead. If Postgres is down, this GET is **503** and the shell paints last-known header chips or `UNKNOWN`. Do **not** add a fourth Overview health tile named “API” or “Postgres.” Those belong on System Health (`GET /api/v1/health`).

### 5.8 Paint rules

| `status` | Dot | Number / caption |
|---|---|---|
| `HEALTHY` | green | `detail` |
| `DEGRADED` | amber | `detail` |
| `STALE` | amber, pulsed | `detail` + `ageMs` via `timeAgo` |
| `UNHEALTHY` | red | `detail` |
| `UNKNOWN` | gray | `detail` (usually “not started”) |

Do not map `status` to a boolean. Do not use a single `fixHealthy`.

Header chips reuse `health.*.status` only (no extras). Overview row 5 shows extras: MT5 per-broker line, QUOTE bid/ask/age, TRADE session + execution flag.

---

## 6. KPI tiles — definitions and sources

All counts are **durable Postgres** (A20). Redis may cache the snapshot; cache miss recomputes. Do not count in-memory worker dictionaries as authority.

### 6.1 Account / broker counts (`data.accounts`)

| §47 tile | JSON path | Type | Definition | Source |
|---|---|---|---|---|
| Total MT5 accounts | `accounts.totalMt5Accounts` | int ≥ 0 | `COUNT(*)` of `mt5_accounts` | `mt5_accounts` |
| Connected source brokers | `accounts.connectedSourceBrokers` | int ≥ 0 | expected-enabled brokers whose live connection is `CONNECTED` **and** heartbeat fresh | `brokers` ⨝ `broker_connections` + worker heartbeat |
| XAUUSD traders | `accounts.xauusdTraders` | int ≥ 0 | distinct `(broker_id, login)` with ≥ 1 `reconstructed_trades` row `canonical_symbol = 'XAUUSD'` | `reconstructed_trades` |
| Traders with ≥ 3 completed trades | `accounts.tradersWithMinThreeCompletedTrades` | int ≥ 0 | distinct `(broker_id, login)` with ≥ 3 **completed** XAUUSD reconstructed trades (`completed = true`) | `reconstructed_trades` or `trader_scores.completed_xau_trades >= 3` (must match scorer) |

`connectedSourceBrokers` is **not** `COUNT(*) FROM brokers`. A registered-but-down broker is not connected.

XAUUSD traders is **not** “all MT5 accounts.” Login is never globally unique (§10): the distinct key is the compound pair.

Until reconstruction is persisted: all four numbers are `0` (or connected = `0` if workers are down). **Do not seed demo counts.**

### 6.2 State rollup (`data.traderStates`)

§47 names five buckets. Domain has nine `TraderState` values. Rollup is **exact**; leftover states are **not** shown as Overview tiles (they live on Scoring).

| §47 tile | JSON | Domain values counted |
|---|---|---|
| Watch | `traderStates.watch` | `WATCH` |
| Shadow | `traderStates.shadow` | `SHADOW` |
| Live candidates | `traderStates.liveCandidates` | `LIVE_CANDIDATE` |
| Live copied | `traderStates.liveCopied` | `LIVE` |
| Risk blocked | `traderStates.riskBlocked` | `RISK_BLOCKED` |

**Not on Overview:** `INSUFFICIENT_DATA`, `EARLY_SCORE`, `PAUSED`, `DISQUALIFIED`.

Source: `trader_states` current row per `(broker_id, login)` (A20 #19). If that table is empty, all five are `0`.

While `REAL_COPY_EXECUTION_ENABLED=false`, `liveCopied` **must stay 0**. Promoting to `LIVE` is 409 (A63). Do not display a fake live-copied count.

### 6.3 P&L pair (`data.pnl`)

| §47 tile | JSON | Type | Definition |
|---|---|---|---|
| Shadow P&L | `pnl.shadowPnl` | `number` or `null` | Book **net** (realized + conservative unrealized) for canonical XAUUSD, lifetime grain |
| Destination real P&L | `pnl.destinationRealPnl` | `number` | Realized + conservative unrealized on the **destination** cTrader book |
| (unit) | `pnl.currency` | `"USD"` | always USD for this product |
| (honesty) | `pnl.shadowPnlQuality` | `featureQuality` | see below |
| (honesty) | `pnl.destinationPnlQuality` | `featureQuality` | see below |

**Shadow (A24 §10.4):** sum `shadow_performance.net` for `canonical_symbol = XAUUSD` at lifetime grain, **or** equivalent sum of per-position `shadow_pnl.net` where `mark_quality != UNPRICED`.

| Mark situation | `shadowPnl` | `shadowPnlQuality` |
|---|---|---|
| All open marks `LIVE` | number | `EXACT` |
| Any open mark `STALE_QUOTE` | last number still published | `APPROXIMATE` |
| Any open mark `UNPRICED` and no other positions | **`null`** | `UNAVAILABLE` |
| Mix of priced + unpriced | priced net only; `detail` not required | `APPROXIMATE` |
| No shadow book | `0.00` | `EXACT` (empty book is honestly zero) |

A24: **zero is a lie** when the position is `UNPRICED`. Overview must not coerce that to `0.00`.

**Destination real P&L:**

| Situation | `destinationRealPnl` | `destinationPnlQuality` |
|---|---|---|
| `REAL_COPY_EXECUTION_ENABLED=false` (first useful) | `0.00` | `EXACT` (no live book; zero is true) |
| Live on, dest snapshot available | number | `EXACT` or `APPROXIMATE` if quote stale |
| Live on, dest snapshot missing | `null` | `UNAVAILABLE` |

Never copy source-MT5 P&L into `destinationRealPnl`. Never copy shadow net into destination.

### 6.4 Exposure + destination margin (`data.exposure`)

These four tiles are the **destination execution account** book (A26 §6.1), not the source MT5 book, not the shadow book.

| §47 tile | JSON | Type | Definition |
|---|---|---|---|
| Current XAU gross | `exposure.grossQuantity` | number | `longQty + shortQty` of **open destination** XAUUSD, lots |
| Current XAU net | `exposure.netQuantity` | number | `longQty − shortQty` (sign: + long) |
| Destination free margin | `exposure.destinationFreeMargin` | number \| **null** | dest account free margin |
| Destination margin level | `exposure.destinationMarginLevel` | number \| **null** | dest margin level (% as the venue reports it) |

Also required on the object (not extra tiles):

| Field | Rule |
|---|---|
| `canonicalSymbol` | always `"XAUUSD"` |
| `longQuantity` / `shortQuantity` | included so Risk can share the same snapshot; Overview may not show them as tiles |
| `book` | always `"DESTINATION"` |
| `marginQuality` | `EXACT` \| `UNAVAILABLE` |

While live execution is off: `grossQuantity = 0`, `netQuantity = 0`, `destinationFreeMargin = null`, `destinationMarginLevel = null`, `marginQuality = UNAVAILABLE`. **Do not invent** Pepperstone equity/margin.

Do **not** put shadow open qty into these four tiles. Shadow exposure belongs on `/shadow`. (Optional future field `shadowGrossQuantity` is **not** in v1.)

---

## 7. Flags (header, not §47 tiles)

```json
"flags": {
  "realCopyExecutionEnabled": false,
  "stopNewExecution": false,
  "emergencyFlattenAvailable": false
}
```

| Field | Source | First useful |
|---|---|---|
| `realCopyExecutionEnabled` | env / settings `REAL_COPY_EXECUTION_ENABLED` (A49) | **false** |
| `stopNewExecution` | durable kill latch (A48). **Independent** of flatten and of the execution flag | false unless latched |
| `emergencyFlattenAvailable` | SuperAdmin/RiskManager **and** TRADE path exists **and** dest book non-empty. First useful: **false** (A06: do not ship working flatten before TRADE send) | **false** |

A single `bool killSwitch` is a §40 violation (A48). Do not add it.

Overview **does not** POST stop-new or flatten. Those live on `/risk`.

---

## 8. `GET /api/v1/overview` — normative JSON

### 8.1 First-useful empty snapshot (honest)

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
      "currency": "USD",
      "shadowPnlQuality": "EXACT",
      "destinationPnlQuality": "EXACT"
    },
    "exposure": {
      "canonicalSymbol": "XAUUSD",
      "book": "DESTINATION",
      "longQuantity": 0,
      "shortQuantity": 0,
      "grossQuantity": 0,
      "netQuantity": 0,
      "destinationFreeMargin": null,
      "destinationMarginLevel": null,
      "marginQuality": "UNAVAILABLE"
    },
    "health": {
      "mt5Ingestion": {
        "status": "UNKNOWN",
        "reasonCode": "NOT_STARTED",
        "detail": "not started",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": null,
        "ageMs": null,
        "expectedBrokers": 2,
        "connectedBrokers": 0,
        "sourceStaleBrokerCount": 0,
        "lastEventAt": null,
        "outboxBacklog": 0,
        "brokers": []
      },
      "fixQuote": {
        "status": "UNKNOWN",
        "reasonCode": "NOT_STARTED",
        "detail": "not started",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": null,
        "ageMs": null,
        "sessionEnabled": true,
        "sessionStatus": "DOWN",
        "loggedOn": false,
        "xauusdMapped": false,
        "instrumentId": null,
        "bid": null,
        "ask": null,
        "spread": null,
        "quoteAgeMs": null,
        "lastInboundAt": null
      },
      "fixTrade": {
        "status": "UNKNOWN",
        "reasonCode": "SESSION_DISABLED",
        "detail": "not required for first useful",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": null,
        "ageMs": null,
        "sessionEnabled": false,
        "sessionStatus": "DISABLED",
        "loggedOn": false,
        "executionEnabled": false,
        "readyForExecution": false,
        "openUnresolvedStates": 0,
        "openReconIssueCount": 0,
        "lastReconciliationAt": null,
        "lastReconciliationStatus": null,
        "lastInboundAt": null
      }
    },
    "flags": {
      "realCopyExecutionEnabled": false,
      "stopNewExecution": false,
      "emergencyFlattenAvailable": false
    }
  }
}
```

`fixQuote.sessionEnabled` may be `true` (flag default, A49) even when the session has never logged on. That is `UNKNOWN` / `NOT_STARTED`, not `HEALTHY`.

### 8.2 Populated example (post-§69, still execution-off)

Matches A26 §6.1 numbers, with A91 health extras filled.

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
      "liveCopied": 0,
      "riskBlocked": 19
    },
    "pnl": {
      "shadowPnl": 12450.25,
      "destinationRealPnl": 0.00,
      "currency": "USD",
      "shadowPnlQuality": "EXACT",
      "destinationPnlQuality": "EXACT"
    },
    "exposure": {
      "canonicalSymbol": "XAUUSD",
      "book": "DESTINATION",
      "longQuantity": 0,
      "shortQuantity": 0,
      "grossQuantity": 0,
      "netQuantity": 0,
      "destinationFreeMargin": null,
      "destinationMarginLevel": null,
      "marginQuality": "UNAVAILABLE"
    },
    "health": {
      "mt5Ingestion": {
        "status": "HEALTHY",
        "reasonCode": "OK",
        "detail": "both brokers connected",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": "2026-08-18T12:00:00.000Z",
        "ageMs": 1800,
        "expectedBrokers": 2,
        "connectedBrokers": 2,
        "sourceStaleBrokerCount": 0,
        "lastEventAt": "2026-08-18T11:59:58.200Z",
        "outboxBacklog": 0,
        "brokers": [
          {
            "brokerId": "a1111111-0000-4000-8000-000000000001",
            "code": "ACHIEVER",
            "displayName": "Achiever",
            "connectionStatus": "CONNECTED",
            "sourceStale": false,
            "lastEventAt": "2026-08-18T11:59:58.200Z",
            "backfillLagSeconds": 2.1,
            "dealIngestPerMinute": 18.4
          },
          {
            "brokerId": "a1111111-0000-4000-8000-000000000002",
            "code": "STARWAVEFX",
            "displayName": "StarwaveFX",
            "connectionStatus": "CONNECTED",
            "sourceStale": false,
            "lastEventAt": "2026-08-18T11:59:57.010Z",
            "backfillLagSeconds": 3.4,
            "dealIngestPerMinute": 6.2
          }
        ]
      },
      "fixQuote": {
        "status": "HEALTHY",
        "reasonCode": "OK",
        "detail": "logged on; XAUUSD mapped; quote 140ms",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": "2026-08-18T12:00:00.000Z",
        "ageMs": 140,
        "sessionEnabled": true,
        "sessionStatus": "READY_FOR_MARKET_DATA",
        "loggedOn": true,
        "xauusdMapped": true,
        "instrumentId": "185",
        "bid": 2401.12,
        "ask": 2401.28,
        "spread": 0.16,
        "quoteAgeMs": 140,
        "lastInboundAt": "2026-08-18T12:00:00.000Z"
      },
      "fixTrade": {
        "status": "DEGRADED",
        "reasonCode": "REAL_EXECUTION_DISABLED",
        "detail": "logged on; real execution disabled",
        "checkedAt": "2026-08-18T12:00:00.000Z",
        "lastHealthyAt": null,
        "ageMs": 11000,
        "sessionEnabled": true,
        "sessionStatus": "READY_FOR_EXECUTION",
        "loggedOn": true,
        "executionEnabled": false,
        "readyForExecution": true,
        "openUnresolvedStates": 0,
        "openReconIssueCount": 0,
        "lastReconciliationAt": "2026-08-18T11:50:00.000Z",
        "lastReconciliationStatus": "READY_FOR_EXECUTION",
        "lastInboundAt": "2026-08-18T11:59:49.000Z"
      }
    },
    "flags": {
      "realCopyExecutionEnabled": false,
      "stopNewExecution": false,
      "emergencyFlattenAvailable": false
    }
  }
}
```

`liveCopied` is `0` here even if A26’s illustrative `4` appears in older examples, because execution is off. After go-live, `LIVE` rows may be non-zero **and** `destinationRealPnl` / exposure become real numbers.

---

## 9. Required API DTOs (C#)

Target namespace when implemented: `TraderIntelligence.Application.Dashboard` (or `…Contracts.Overview`). These records **replace** the flat `OverviewDto` in `DashboardModels.cs`. This file does not edit that source.

JSON names are System.Text.Json camelCase defaults.

```csharp
// Wire enums — serialize as strings, exact spellings.
public enum HealthStatus { HEALTHY, DEGRADED, UNHEALTHY, STALE, UNKNOWN }

public enum FeatureQuality { EXACT, APPROXIMATE, UNAVAILABLE }

public enum ConnectionStatus
{
    CONNECTED, CONNECTING, DISCONNECTED, RECONNECTING, DISABLED
}

public sealed record OverviewResponse(OverviewSnapshotDto Data);

public sealed record OverviewSnapshotDto(
    DateTimeOffset GeneratedAt,
    OverviewAccountsDto Accounts,
    OverviewTraderStatesDto TraderStates,
    OverviewPnlDto Pnl,
    OverviewExposureDto Exposure,
    OverviewHealthDto Health,
    OverviewFlagsDto Flags);

public sealed record OverviewAccountsDto(
    int TotalMt5Accounts,
    int ConnectedSourceBrokers,
    int XauusdTraders,
    int TradersWithMinThreeCompletedTrades);

public sealed record OverviewTraderStatesDto(
    int Watch,
    int Shadow,
    int LiveCandidates,
    int LiveCopied,
    int RiskBlocked);

public sealed record OverviewPnlDto(
    decimal? ShadowPnl,
    decimal? DestinationRealPnl,
    string Currency,
    FeatureQuality ShadowPnlQuality,
    FeatureQuality DestinationPnlQuality);

public sealed record OverviewExposureDto(
    string CanonicalSymbol,
    string Book,
    decimal LongQuantity,
    decimal ShortQuantity,
    decimal GrossQuantity,
    decimal NetQuantity,
    decimal? DestinationFreeMargin,
    decimal? DestinationMarginLevel,
    FeatureQuality MarginQuality);

public sealed record OverviewHealthDto(
    Mt5IngestionHealthDto Mt5Ingestion,
    FixQuoteHealthDto FixQuote,
    FixTradeHealthDto FixTrade);

public sealed record HealthTileBase(
    HealthStatus Status,
    string ReasonCode,
    string Detail,
    DateTimeOffset CheckedAt,
    DateTimeOffset? LastHealthyAt,
    int? AgeMs);

public sealed record Mt5BrokerHealthDto(
    Guid BrokerId,
    string Code,
    string DisplayName,
    ConnectionStatus ConnectionStatus,
    bool SourceStale,
    DateTimeOffset? LastEventAt,
    double? BackfillLagSeconds,
    double? DealIngestPerMinute);

public sealed record Mt5IngestionHealthDto(
    HealthStatus Status,
    string ReasonCode,
    string Detail,
    DateTimeOffset CheckedAt,
    DateTimeOffset? LastHealthyAt,
    int? AgeMs,
    int ExpectedBrokers,
    int ConnectedBrokers,
    int SourceStaleBrokerCount,
    DateTimeOffset? LastEventAt,
    int OutboxBacklog,
    IReadOnlyList<Mt5BrokerHealthDto> Brokers);

public sealed record FixQuoteHealthDto(
    HealthStatus Status,
    string ReasonCode,
    string Detail,
    DateTimeOffset CheckedAt,
    DateTimeOffset? LastHealthyAt,
    int? AgeMs,
    bool SessionEnabled,
    string SessionStatus,
    bool LoggedOn,
    bool XauusdMapped,
    string? InstrumentId,   // string — venue id is not a JS-safe assumption
    decimal? Bid,
    decimal? Ask,
    decimal? Spread,
    int? QuoteAgeMs,
    DateTimeOffset? LastInboundAt);

public sealed record FixTradeHealthDto(
    HealthStatus Status,
    string ReasonCode,
    string Detail,
    DateTimeOffset CheckedAt,
    DateTimeOffset? LastHealthyAt,
    int? AgeMs,
    bool SessionEnabled,
    string SessionStatus,
    bool LoggedOn,
    bool ExecutionEnabled,
    bool ReadyForExecution,
    int OpenUnresolvedStates,
    int OpenReconIssueCount,
    DateTimeOffset? LastReconciliationAt,
    string? LastReconciliationStatus,
    DateTimeOffset? LastInboundAt);

public sealed record OverviewFlagsDto(
    bool RealCopyExecutionEnabled,
    bool StopNewExecution,
    bool EmergencyFlattenAvailable);

public interface IOverviewQueries
{
    Task<OverviewSnapshotDto> GetOverviewAsync(CancellationToken ct);
}
```

`IDashboardQueries.GetOverviewAsync` may keep the same name if the return type is changed to `OverviewSnapshotDto`. Do not keep a parallel bool `OverviewDto` “for convenience.”

Header-strip subset (also the SignalR `ops.header` payload):

```csharp
public sealed record OpsHeaderDto(
    DateTimeOffset GeneratedAt,
    OverviewFlagsDto Flags,
    OverviewHealthDto Health);
```

`Health` in the header may omit `brokers[]` extras to keep the frame small; `status` / `reasonCode` / `detail` on all three tiles are mandatory.

---

## 10. Required TypeScript DTOs

Replace `apps/web/src/types` `Overview` when the page is coded. Do not keep `fixHealthy: boolean`.

```ts
export type HealthStatus =
  | 'HEALTHY'
  | 'DEGRADED'
  | 'UNHEALTHY'
  | 'STALE'
  | 'UNKNOWN';

export type FeatureQuality = 'EXACT' | 'APPROXIMATE' | 'UNAVAILABLE';

export type ConnectionStatus =
  | 'CONNECTED'
  | 'CONNECTING'
  | 'DISCONNECTED'
  | 'RECONNECTING'
  | 'DISABLED';

export interface HealthTileBase {
  status: HealthStatus;
  reasonCode: string;
  detail: string;
  checkedAt: string;
  lastHealthyAt: string | null;
  ageMs: number | null;
}

export interface Mt5BrokerHealth {
  brokerId: string;
  code: string;
  displayName: string;
  connectionStatus: ConnectionStatus;
  sourceStale: boolean;
  lastEventAt: string | null;
  backfillLagSeconds: number | null;
  dealIngestPerMinute: number | null;
}

export interface Mt5IngestionHealth extends HealthTileBase {
  expectedBrokers: number;
  connectedBrokers: number;
  sourceStaleBrokerCount: number;
  lastEventAt: string | null;
  outboxBacklog: number;
  brokers: Mt5BrokerHealth[];
}

export interface FixQuoteHealth extends HealthTileBase {
  sessionEnabled: boolean;
  sessionStatus: string;
  loggedOn: boolean;
  xauusdMapped: boolean;
  instrumentId: string | null;
  bid: number | null;
  ask: number | null;
  spread: number | null;
  quoteAgeMs: number | null;
  lastInboundAt: string | null;
}

export interface FixTradeHealth extends HealthTileBase {
  sessionEnabled: boolean;
  sessionStatus: string;
  loggedOn: boolean;
  executionEnabled: boolean;
  readyForExecution: boolean;
  openUnresolvedStates: number;
  openReconIssueCount: number;
  lastReconciliationAt: string | null;
  lastReconciliationStatus: string | null;
  lastInboundAt: string | null;
}

export interface OverviewAccounts {
  totalMt5Accounts: number;
  connectedSourceBrokers: number;
  xauusdTraders: number;
  tradersWithMinThreeCompletedTrades: number;
}

export interface OverviewTraderStates {
  watch: number;
  shadow: number;
  liveCandidates: number;
  liveCopied: number;
  riskBlocked: number;
}

export interface OverviewPnl {
  shadowPnl: number | null;
  destinationRealPnl: number | null;
  currency: 'USD' | string;
  shadowPnlQuality: FeatureQuality;
  destinationPnlQuality: FeatureQuality;
}

export interface OverviewExposure {
  canonicalSymbol: 'XAUUSD' | string;
  book: 'DESTINATION';
  longQuantity: number;
  shortQuantity: number;
  grossQuantity: number;
  netQuantity: number;
  destinationFreeMargin: number | null;
  destinationMarginLevel: number | null;
  marginQuality: FeatureQuality;
}

export interface OverviewHealth {
  mt5Ingestion: Mt5IngestionHealth;
  fixQuote: FixQuoteHealth;
  fixTrade: FixTradeHealth;
}

export interface OverviewFlags {
  realCopyExecutionEnabled: boolean;
  stopNewExecution: boolean;
  emergencyFlattenAvailable: boolean;
}

export interface OverviewSnapshot {
  generatedAt: string;
  accounts: OverviewAccounts;
  traderStates: OverviewTraderStates;
  pnl: OverviewPnl;
  exposure: OverviewExposure;
  health: OverviewHealth;
  flags: OverviewFlags;
}

export interface OverviewEnvelope {
  data: OverviewSnapshot;
}

export interface OpsHeader {
  generatedAt: string;
  flags: OverviewFlags;
  health: OverviewHealth;
}
```

Fetcher (when coded): `GET /api/v1/overview` → `OverviewEnvelope`. Query key: `['overview']` (A62 §8). Poll 15–30 s if hub down. **No** `refetchInterval` on secrets (there are none).

Paint:

- Counts: integer, thousands separators.
- P&L: `formatPrice`; color via existing `pnlColor`. If quality is `UNAVAILABLE`, show `—` not `0.00`.
- Margin null: `—`.
- Health: `HealthDot` from `status` + `detail` under the label.

---

## 11. Query plan (implementation notes, not code)

One snapshot, one DB round-trip preferred (`IOverviewQueries`). Suggested reads:

| Block | Read |
|---|---|
| `accounts.totalMt5Accounts` | `SELECT count(*) FROM mt5_accounts` |
| `connectedSourceBrokers` | enabled `brokers` ⨝ latest `broker_connections` + in-process heartbeat |
| `xauusdTraders` | `COUNT(DISTINCT (broker_id, login)) FROM reconstructed_trades WHERE canonical_symbol = 'XAUUSD'` |
| `tradersWithMinThreeCompletedTrades` | `COUNT(*) FROM trader_scores WHERE completed_xau_trades >= 3` **or** equivalent completed-trade group; must match A22 |
| `traderStates.*` | `SELECT current_state, count(*) FROM trader_states GROUP BY 1` then map |
| `pnl.shadowPnl` | lifetime `shadow_performance` for XAUUSD (A24) |
| `pnl.destinationRealPnl` | dest position + fill rollup; **0** if flag off |
| `exposure.*` | open `destination_positions` XAUUSD; dest account snapshot for margin |
| `health.mt5Ingestion` | `broker_connections` + `sync_checkpoints` / last `ingestion_events` + `mt5_outbox_backlog` metric |
| `health.fixQuote` | `fix_sessions` where qualifier=`QUOTE` + latest `destination_quotes` for mapped XAUUSD |
| `health.fixTrade` | `fix_sessions` TRADE + last `execution_reconciliation_runs` + count open issues |
| `flags` | options + `kill_switch` row(s) — **two independent bits**, not `KillSwitchMode` exclusive (A48) |

If a table does not exist yet: that field is `0` / `null` / `UNKNOWN`. Do not create dummy rows to make the dashboard look busy.

Compound identity: never `COUNT(DISTINCT login)`.

---

## 12. SignalR

Hub `/hubs/ops` (A26 §7). Overview page:

| Event | Payload | Client |
|---|---|---|
| `overview.updated` | full `OverviewSnapshot` (`data`) | `setQueryData(['overview'], …)` |
| `ops.header` | `OpsHeader` (`flags` + `health`) | merge into `['overview']`; update HeaderStrip |
| `fix.session` | session subset | patch `health.fixQuote` / `health.fixTrade` |
| `broker.health` | one broker extra | patch `health.mt5Ingestion.brokers[i]` and recompute tile `status` **on the server** (client must not re-run worst-wins on partial data except as optimistic; prefer invalidate) |
| `quote.xauusd` | `{ instrumentId, bid, ask, quoteAgeMs, spread, at }` | patch QUOTE extras; do not set `HEALTHY` client-side |
| `risk.state` | three flags | patch `flags` |

Prefer **server-computed** `status`. Clients that merge extras should `invalidateQueries(['overview'])` rather than invent a new `healthStatus`.

Sanitizer: drop the frame if a denylisted key appears (A26 §3 / A62 §9).

REST polling of `/api/v1/overview` is enough to **paint** first useful (A06 §4.14). Hub is not a §69 gate.

---

## 13. Secrets and sanitizer

Overview is a high-traffic payload. Fail closed.

**Never** on this DTO, hub frame, or `detail` string:

```text
MT5 passwords, proxy credentials, cTrader account password,
FIX password, RawData / tag 96, database / Redis connection strings,
private keys, refresh tokens, raw manager login
```

Allowed: broker display names, connection enum, masked nothing (Overview does not show manager login at all — that is §48), FIX `sessionStatus`, discovered `instrumentId`, bid/ask, health enums, flags.

Server sanitizer (A26 §3.4) runs before serialize. If a secret remains, **503/500 fail closed** — do not send a half-redacted Overview.

---

## 14. RBAC

| Role | GET `/api/v1/overview` |
|---|---|
| SuperAdmin | yes |
| RiskManager | yes |
| Analyst | yes |
| ReadOnly | yes |
| anonymous | 401 |

No write verbs. Flatten / stop-new / enable-real-execution are **not** Overview actions.

---

## 15. Gap vs current tree (do not “fix” from this file)

| Current | Required |
|---|---|
| `OverviewDto` bools `Mt5Healthy`, `QuoteHealthy`, `TradeHealthy` | `HealthTile` + extras |
| Missing `destinationFreeMargin` / `destinationMarginLevel` | nullable decimals + `marginQuality` |
| Missing `generatedAt`, `flags`, `currency`, qualities | required |
| `Live` property name | JSON `liveCopied` (A26) |
| `XauTraders` | JSON `xauusdTraders` |
| `TradersWithThreeTrades` | JSON `tradersWithMinThreeCompletedTrades` |
| React `Overview.fixHealthy` | three tiles |
| Hook `/api/overview` | `/api/v1/overview` |
| API weatherforecast | implement later |

A63 `fixTrade.status: "DISABLED"` is **superseded** by this file for Overview. System Health page may keep a session-state field named `status` on the FIX sub-object only if it is documented as `fixSessionStatus`, not `healthStatus`.

---

## 16. Tests (inventory — do not add test projects from this file)

Suggested names for A27 when coded:

| Test | Assert |
|---|---|
| `OverviewDto_EmptySnapshot_IsHonestUnknown` | first-useful JSON: zeros, `UNKNOWN` health, `destinationFreeMargin=null`, flags false |
| `OverviewDto_DoesNotSerializeSecrets` | password-shaped properties stripped / fail closed |
| `OverviewDto_LiveCopied_ZeroWhenExecutionOff` | `LIVE` rows cannot appear; count 0 |
| `OverviewHealth_Mt5_PartialBrokers_IsDegraded` | 1 of 2 connected → `DEGRADED` / `PARTIAL_BROKERS` |
| `OverviewHealth_Mt5_SourceStale_IsStaleNotHealthy` | both connected + stale → `STALE` / `SOURCE_STALE` |
| `OverviewHealth_Quote_StaleTick_IsStale` | logged on, old `quoteAgeMs` → `STALE` |
| `OverviewHealth_Quote_Unmapped_IsDegraded` | no instrument id → `XAUUSD_UNMAPPED` |
| `OverviewHealth_Trade_Disabled_IsUnknownNotHealthy` | first useful TRADE dark → `UNKNOWN` / `SESSION_DISABLED` |
| `OverviewHealth_Trade_ReadyButFlagOff_IsDegraded` | A26 example |
| `OverviewHealth_Trade_StatusNeverDisabledEnum` | `healthStatus` parse rejects `DISABLED` |
| `OverviewPnl_UnpricedShadow_IsNullNotZero` | A24 |
| `OverviewCounts_UseCompoundIdentity` | same login on two brokers counts as two XAU traders |
| `Overview_Get_RequiresAuth` | anonymous 401 |
| `Overview_NoMutationRoutes` | no POST/PATCH/PUT on `/overview` |

---

## 17. Acceptance (this spec)

This document is **done** when a later coder can implement `GET /api/v1/overview` and `OverviewPage` without inventing tile names, health enums, or P&L honesty rules.

The **endpoint** is not done. Measured: **MISSING**.

| # | Check | Status |
|---|---|---|
| 1 | All 18 §47 tiles named with JSON paths | specified |
| 2 | Three health tiles use `healthStatus` (not bool, not `DISABLED`) | specified |
| 3 | First-useful honest empty snapshot | specified |
| 4 | C# + TS DTOs | specified |
| 5 | Secrets denylist | specified |
| 6 | Product source unchanged | **yes** |

---

## 18. Tile → DTO index (quick)

| # | Architecture text | DTO path | Kind |
|---|---|---|---|
| 1 | Total MT5 accounts | `accounts.totalMt5Accounts` | KPI |
| 2 | Connected source brokers | `accounts.connectedSourceBrokers` | KPI |
| 3 | XAUUSD traders | `accounts.xauusdTraders` | KPI |
| 4 | Traders with ≥ 3 completed trades | `accounts.tradersWithMinThreeCompletedTrades` | KPI |
| 5 | Watch | `traderStates.watch` | KPI |
| 6 | Shadow | `traderStates.shadow` | KPI |
| 7 | Live candidates | `traderStates.liveCandidates` | KPI |
| 8 | Live copied | `traderStates.liveCopied` | KPI |
| 9 | Risk blocked | `traderStates.riskBlocked` | KPI |
| 10 | Shadow P&L | `pnl.shadowPnl` + `pnl.shadowPnlQuality` | KPI |
| 11 | Destination real P&L | `pnl.destinationRealPnl` + `pnl.destinationPnlQuality` | KPI |
| 12 | Current XAU gross exposure | `exposure.grossQuantity` | KPI |
| 13 | Current XAU net exposure | `exposure.netQuantity` | KPI |
| 14 | Destination free margin | `exposure.destinationFreeMargin` | KPI (nullable) |
| 15 | Destination margin level | `exposure.destinationMarginLevel` | KPI (nullable) |
| 16 | MT5 ingestion health | `health.mt5Ingestion` | **Health tile** |
| 17 | FIX quote health | `health.fixQuote` | **Health tile** |
| 18 | FIX trade health | `health.fixTrade` | **Health tile** |
| — | Real copy (header) | `flags.realCopyExecutionEnabled` | chip, not §47 |
| — | Stop-new (header) | `flags.stopNewExecution` | chip, not §47 |
| — | Flatten available (header/risk) | `flags.emergencyFlattenAvailable` | chip, not §47 |

End of A91.
