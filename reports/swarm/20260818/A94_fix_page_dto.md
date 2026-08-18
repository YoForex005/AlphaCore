# A94 — cTrader FIX Page DTO (Architecture §52)

| Field | Value |
|---|---|
| Agent | A94 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A94_fix_page_dto.md` |
| Product source modified | **No** |
| Scope | Allow-list JSON / C# / TypeScript DTOs for the React **cTrader FIX** page. Quote card vs Trade card. Password never leaves the secret store. |
| Binding law | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§52** (lines 1903–1949). Supporting: §27–§28, §30–§31, §41–§43, §46, §48, §55–§57, §59, §69.9–10, §72.5. |
| Sibling specs (do not fork) | A06 §3.7 / §4.9, A08, A19, A20 (`fix_sessions`, `destination_quotes`, `fix_session_events`), A25 §7.8, A26 §3 + §6.11, A44 §6.6, A46 §15.3, A47 §12.4, A49, A51 `dash.read`, A57 item 9–10, A62 §10.6, A63 §2 + §5.8, A76 (log tags; same denylist on the wire) |
| Classification | Page DTO = **MISSING on the wire**. Internal `FixSessionDto` exists and is **not** this contract. |

This file is the **implementable page-DTO lock** for `/fix`. It does not implement controllers, React pages, or migrations. It does not invent a second architecture. If this file and v2 disagree, **v2 wins**. If this file and A26/A63 disagree on the FIX **page** envelope, **this file wins** (A26 remains the dashboard-wide catalog; A63 remains the first-useful route list).

---

## 0. Verdict (honest, measured 2026-08-18)

| Question | Result |
|---|---|
| Does architecture §52 require two cards? | **Yes.** `QUOTE SESSION` and `TRADE SESSION`. |
| Does §52 allow showing the FIX password? | **No.** Last line of the section: `Never show FIX password.` |
| Does `GET /api/v1/fix/sessions` exist? | **No.** `D:\Prop\apps\api\Program.cs` is still the weatherforecast template. |
| Is there an internal dashboard DTO? | **Yes, insufficient.** `TraderIntelligence.Application.Dashboard.FixSessionDto` is a **single flattened row** used as `IReadOnlyList<FixSessionDto>`. |
| Does the query leak a password today? | **No password column is selected.** `EfDashboardQueries.GetFixSessionsAsync` maps `FixSessionState` + latest `DestinationQuoteSnapshot`. `CTraderFixOptions.Password` is not in that path. Absence of a leak is **not** an allow-list control. |
| Does the query satisfy §52? | **No.** One list, quote fields copied onto TRADE, no SSL flag, no heartbeat/test, no `errors[]`, no last execution report, no last reconciliation, no `secretConfigured`, PascalCase enum `Status` (`ReadyForMarketData`), `QuoteAgeSeconds` instead of `quoteAgeMs`. |
| Does React have the page? | **No.** `App.tsx` imports `./pages/FixSessionsPage` but `D:\Prop\apps\web\src\pages\` has **zero** files. Types exist as `FixSession[]`. Hook calls unversioned `/api/fix/sessions`. Nav label is `FIX`, not `cTrader FIX`. |
| First-useful honesty | TRADE card **must still exist** (A06 / A63). It may be `DISABLED` / not started. Do not hide the venue. |

**Gap id (A29):** `U08` — cTrader FIX page (QUOTE+TRADE cards) — **MISSING**.

---

## 1. Source of law — §52 verbatim

Quoted from architecture v2, `# 52. cTrader FIX Page`:

```text
Show separate cards:

QUOTE SESSION
TRADE SESSION

Each displays:

Host
SSL Port
Connected?
Logged on?
Session status
Last inbound
Last outbound
Message sequence state
Reconnect count
Last heartbeat/test request
Errors

For QUOTE also show:

XAUUSD mapped?
Instrument ID
Bid
Ask
Quote age
Spread

For TRADE show:

Execution enabled?
Open orders
Open destination positions
Last execution report
Last reconciliation

Never show FIX password.
```

That last sentence is **hard law**, same family as §48 (“No secret values”), §55 (never expose FIX / cTrader account password to React), §57 (never log authentication tags containing passwords), §72.5 (never expose secrets to the browser).

---

## 2. What this DTO is / is not

### 2.1 In scope

1. Allow-list JSON for the `/fix` page and its two optional singleton GETs.
2. C# record shapes the API must serialize (proposed names; not on disk).
3. TypeScript types the React page must consume.
4. Mapping from `FixSessionState` / `DestinationQuoteSnapshot` / flags / recon — **projection**, never entity dump.
5. Secret denylist and sanitizer fail-closed rule for this page, SignalR `fix.session` / `quote.xauusd`, CSV, and `audit_logs.after`.
6. Honest empty / first-useful payload (QUOTE down or unmapped; TRADE `DISABLED`).

### 2.2 Out of scope

| Topic | Owner |
|---|---|
| QuickFIX/n session objects, TLS, Logon | A25, A31–A36 |
| TRADE lease / fencing | A46 |
| Startup / periodic recon algorithm | A47 |
| Instrument discovery rules | A44, §30 |
| Settings PATCH for non-secret FIX host/ports | A26 §6.16 — password still **422** |
| Live `NewOrderSingle` | Forbidden until §68 / §70. Flag stays false (A49) |
| Implementing controllers or `CTraderFixPage.tsx` | Later coding increment. **This agent does not touch product source.** |

### 2.3 Cardinal rules (do not weaken)

1. **Two independent cards. Always.** The page payload is `{ quote, trade }`, never `FixSessionDto[]` as the public contract. A missing TRADE row is serialized as `sessionStatus: "DISABLED"` with zeros / nulls — **not** omitted.
2. **Do not share a sequence counter** across cards (§27). `quote.sequence` and `trade.sequence` are different objects.
3. **Never one session with only a qualifier.** `session: "QUOTE"` on a TRADE-shaped object is a contract bug.
4. **Allow-list DTOs only.** Never serialize `FixSessionState`, `CTraderFixOptions`, `IConfiguration`, or QuickFIX `SessionSettings`.
5. **Never password.** Not as a key, not as a value, not as `****`, not as length, not as `RawData`, not as tag `554`, not in events, not in problem details.
6. **Instrument ID is discovered** (§30, A44). UI and DTO never hardcode another account’s tag `55`.
7. **`executionEnabled` is the runtime flag**, not `loggedOn`. Logged-on TRADE + `REAL_COPY_EXECUTION_ENABLED=false` → `executionEnabled: false` (A49).
8. Pepperstone/cServer is an **execution venue**, not an LP (A87). Field names stay `session` / `venue`, never `lp`.

---

## 3. HTTP surface this page owns

Aligns with A06 §4.9 and A63 §5.8. Roles: **ReadOnly+** (`dash.read`, A51). No FIX logon / password / config `POST` from React.

| Method | Path | Returns | Page use |
|---|---|---|---|
| `GET` | `/api/v1/fix/sessions` | `FixPageDto` | **Primary.** Both cards in one paint. |
| `GET` | `/api/v1/fix/quote` | `QuoteSessionCardDto` | QUOTE-only refresh / first-useful poll. |
| `GET` | `/api/v1/fix/trade` | `TradeSessionCardDto` | TRADE-only refresh. First useful: `DISABLED` is valid. |
| `GET` | `/api/v1/fix/sessions/{session}/events` | paged `FixSessionEventDto` | Card footer. `{session}` = `QUOTE` \| `TRADE` only. |

Conventions (A26 §2 / A63 §1): `/api/v1`, Bearer JWT, camelCase JSON, ISO-8601 UTC `Z`, `X-Correlation-Id`, tickets / FIX ids as **string**.

Envelopes:

```json
{ "data": { "quote": {}, "trade": {} } }
```

```json
{ "data": { } }
```

```json
{ "data": [], "page": 1, "pageSize": 50, "totalItems": 0, "totalPages": 0 }
```

React Query keys (A26 §13, do not poll secrets):

```text
['fix']
['fix', 'sessions']
['fix', 'quote']
['fix', 'trade']
['fix', 'events', 'QUOTE' | 'TRADE', page]
```

Invalidate `['fix']` from SignalR `fix.session` and `quote.xauusd`. Polling `GET /fix/sessions` every 5s is acceptable until the hub exists. The current hook’s `/api/fix/sessions` path is **not** this contract.

**Mutations:** none on this page. Non-secret FIX host/ports/comp ids live on Settings (`PATCH /api/v1/settings/fix`, SuperAdmin). Password field **must not exist**. If any denylisted key is posted: `422 SECRET_FIELD_REJECTED`, do not persist (A26 §3.3).

---

## 4. Never-password denylist (this page)

Hard law from §52, §55, A26 §3, A63 §2, A76.

### 4.1 Forbidden keys (any casing, any nesting, including `_` / `-`)

| Class | Examples | Why |
|---|---|---|
| FIX password | `password`, `passwd`, `pwd`, `fixPassword`, `CTRADER_FIX_PASSWORD`, `Password` | §52 / §55 |
| Generic FIX auth blobs | `rawData`, `RawData`, `secureData`, `encryptedPassword`, `newPassword` | tags 96 / 91 / 1401 / 925 |
| cTrader account secret | any account-login password for destination `1369850` | §55 |
| Broker-issued SubIDs | `senderSubId`, `targetSubId`, `SenderSubID`, `TargetSubID` | A63 v1 omit; broker-issued |
| FIX username-as-secret | do not echo tag `553` as `username` / `fixUsername` | A76; `senderCompId` is enough |
| Options dump | whole `CTraderFixOptions` | contains `Password` + `AccountId` |
| Entity dump | whole `FixSessionState` | contains `SenderSubId` |
| Infra | `connectionString`, `redisPassword`, `privateKey`, `clientSecret` | §55 |
| Session | refresh token, access token in GET body | A26 |

`secretConfigured` is **boolean metadata**. It is not a secret. Name-regex sanitizers that drop `*secret*` must **allow-list** this one property (or name the sanitizer on `password|passwd|pwd|rawdata|…` as A26 already does — that regex does **not** match `secretConfigured`).

### 4.2 Forbidden values

Even if the key is innocent (`detail`, `text`, `lastError`, `message`, `raw`):

- FIX tag `554=` / `553=` / `96=` / `925=` / `1401=` payloads
- `Password=` connection-string fragments
- Official RoE sample password `passw0rd!` if it ever appears in a fixture that reaches the API
- Any `CTraderFixOptions.Password` string

Redact to the three-character literal `***` (A76). Do **not** pad. Do **not** hash into the JSON. Prefer **drop the field**.

### 4.3 Sanitizer (mandatory before serialize)

Same four steps as A26 §3.4 / A63 §2.3, applied to REST, SignalR, CSV, `audit_logs.after`, and this page’s `errors[].text`:

1. Drop properties whose names match `(?i)(password|passwd|pwd|rawdata|connectionstring|privatekey|proxyuser)`.
2. Redact `Password=` substrings.
3. Replace FIX tags `553` / `554` / `96` / `91` / `925` / `1401` / `1403` values with `***`.
4. **Fail closed** if a secret remains — `500` / `DEPENDENCY_UNAVAILABLE` with a generic message. Never send a half-redacted card.

Events endpoint: **no raw Logon**. Persist / return `eventType`, timestamps, seq, reject codes — not `35=A` wire.

EF: project **selected columns**. Do not load a password column into memory for dashboard queries (A26 §13.2). `fix_sessions` must never have a password column (A57: persist session row, never password).

### 4.4 Allowed secret-adjacent metadata

| Field | Type | Notes |
|---|---|---|
| `secretConfigured` | `boolean` | `true` iff the secret store has a non-empty FIX password. Never the value. |
| `secretLastRotatedAt` | `string \| null` | ISO-8601 if the store exposes rotation. Optional. |
| `senderCompId` | `string` | Published non-secret (`live.pepperstone.1369850` shape). |
| `targetCompId` | `string` | Preserve issued case (`cServer` ≠ `CSERVER`). |
| Destination account **number** | **omit on this page** | `senderCompId` already identifies the venue. Do not add `accountId` / `1369850` as a separate card field. |

---

## 5. Wire enums

Do **not** emit C# enum integers (`FixSessionQualifier.Quote = 0`). The browser speaks these strings.

### 5.1 `session` / `sessionQualifier`

```text
QUOTE
TRADE
```

Map `FixSessionQualifier.Quote → "QUOTE"`, `Trade → "TRADE"`. Path `{session}` is the same union; anything else → `404 NOT_FOUND`.

### 5.2 `sessionStatus` (page-facing)

Architecture §52 says “Session status” without a list. Domain `FixSessionStatus` is richer than A26’s short union. **This page uses the richer allow-list** so operators can see recon vs ready without reading logs.

```text
DOWN
CONNECTING
LOGON_SENT
LOGGED_ON
RECONCILING
READY_FOR_MARKET_DATA
READY_FOR_EXECUTION
LOGGED_OUT
RECONNECTING
SEQUENCE_RESET
DISABLED
ERROR
```

| Domain `FixSessionStatus` | Wire `sessionStatus` |
|---|---|
| `Disconnected` | `DOWN` |
| `Connecting` | `CONNECTING` |
| `LogonSent` | `LOGON_SENT` |
| `LoggedOn` | `LOGGED_ON` |
| `Reconciling` | `RECONCILING` |
| `ReadyForMarketData` | `READY_FOR_MARKET_DATA` |
| `ReadyForExecution` | `READY_FOR_EXECUTION` |
| `LogoutSent` | `LOGGED_OUT` |
| `Error` | `ERROR` |
| *(no row + session flag off)* | `DISABLED` |
| *(reconnect in progress)* | `RECONNECTING` |
| *(seq reset in progress)* | `SEQUENCE_RESET` |

Rules:

- QUOTE never uses `READY_FOR_EXECUTION` or `RECONCILING`. QUOTE healthy terminal is `READY_FOR_MARKET_DATA` (or `LOGGED_ON` if Security List / MD is not yet up).
- TRADE never uses `READY_FOR_MARKET_DATA`. TRADE must **not** jump `LOGGED_ON → READY_FOR_EXECUTION` (A46). Recon sits in between.
- `CTRADER_FIX_ENABLED=false` or the per-session flag off → that card is `DISABLED` even if a stale `fix_sessions` row exists.
- Do not emit `ReadyForMarketData` (PascalCase domain name) on the wire.

### 5.3 Booleans §52 names as JSON

| §52 | JSON |
|---|---|
| Connected? | `connected` |
| Logged on? | `loggedOn` |
| XAUUSD mapped? | `xauusd.mapped` |
| Execution enabled? | `executionEnabled` |

`connected` is TCP/TLS up. `loggedOn` is Logon accepted. A card may be `connected: true, loggedOn: false` (`CONNECTING` / `LOGON_SENT`). `READY_FOR_*` implies both true.

### 5.4 `lastReconciliation.status` (TRADE only, A47 §12.4)

```text
READY_FOR_EXECUTION
BLOCKED_INCONSISTENT
BLOCKED_PENDING_RECON
BLOCKED_STALE
NEVER
```

First useful / no TRADE session: `NEVER`. Distinct from `sessionStatus`.

### 5.5 Event types (events GET)

Allow-list (extend later, do not pass through raw `35=`):

```text
CONNECT
DISCONNECT
LOGON
LOGOUT
HEARTBEAT_MISS
TEST_REQUEST
RESEND
REJECT
BUSINESS_REJECT
SEQUENCE_RESET
LEADERSHIP_ACQUIRE
LEADERSHIP_YIELD
LEADERSHIP_FENCE
ERROR
```

`LOGON` events record `success: true|false` and a **reason code**. Never the Logon body.

---

## 6. Field map — §52 → DTO

### 6.1 Shared card (`FixSessionCardBaseDto`)

| §52 line | JSON path | Type | Source (when implemented) | Notes |
|---|---|---|---|---|
| (card title) | `session` | `QUOTE` \| `TRADE` | qualifier | Also echoed as `sessionQualifier` for A26 compatibility |
| Host | `host` | string | `FixSessionState.Host` / options | e.g. `live-us-eqx-01.p.c-trader.com` |
| SSL Port | `sslPort` | number | QUOTE `5211` / TRADE `5212` when `useSsl` | **Not** the plain `5201`/`5202` production default (§25) |
| (implied) | `useSsl` | boolean | options, default `true` | |
| Connected? | `connected` | boolean | session / watchdog | |
| Logged on? | `loggedOn` | boolean | Logon state | |
| Session status | `sessionStatus` | enum §5.2 | mapped domain + flags | |
| Last inbound | `lastInboundAt` | string \| null | `LastInboundAt` | ISO-8601 `Z` |
| Last outbound | `lastOutboundAt` | string \| null | `LastOutboundAt` | |
| Message sequence state | `sequence.nextSender` | number | `OutboundSeq` | Next to send. Independent per card |
| | `sequence.nextTarget` | number | `InboundSeq` | Next expected inbound |
| Reconnect count | `reconnectCount` | number ≥ 0 | `ReconnectCount` | |
| Last heartbeat/test request | `lastHeartbeatAt` | string \| null | session metrics | Split — architecture lists both on one line |
| | `lastTestRequestAt` | string \| null | session metrics | |
| Errors | `errors` | `FixSessionErrorDto[]` | `LastError` + event tail | Never raw FIX. Empty array ≠ hide the list |
| (A26) | `senderCompId` | string | `SenderCompId` | Safe |
| (A26) | `targetCompId` | string | `TargetCompId` | Preserve case |
| (A26) | `secretConfigured` | boolean | secret store **presence** | Only secret metadata |

`FixSessionErrorDto`:

```json
{
  "at": "2026-08-18T11:58:00.000Z",
  "code": "LOGON_REJECT",
  "text": "session rejected"
}
```

`text` runs the sanitizer. Cap 20 most recent on the card. Full history is the events GET.

### 6.2 QUOTE extras (`xauusd`)

| §52 | JSON | Type | Rule |
|---|---|---|---|
| XAUUSD mapped? | `xauusd.mapped` | boolean | `true` **iff** an active, non-stale, non-ambiguous `destination_symbols` row exists (A44 §6.6). Not “bid is non-null.” |
| Instrument ID | `xauusd.instrumentId` | string \| null | Persisted venue id. JSON **string**. `null` when unmapped. Never hardcode. |
| Bid | `xauusd.bid` | number \| null | Latest `destination_quotes` for that id |
| Ask | `xauusd.ask` | number \| null | |
| Quote age | `xauusd.quoteAgeMs` | number \| null | `now - quote_received_at` in **milliseconds**. Not seconds. |
| Spread | `xauusd.spread` | number \| null | `ask - bid` when both present; else null. Do not fabricate. |

Also allowed on the quote object (not required by §52): `xauusd.receivedAt`, `xauusd.venueTimestamp`, `xauusd.stale` (`quoteAgeMs > MaxQuoteAgeMs`).

QUOTE card **must not** include `executionEnabled`, `openOrders`, `lastExecutionReport`, `lastReconciliation`, ownership, or fencing.

### 6.3 TRADE extras

| §52 | JSON | Type | Rule |
|---|---|---|---|
| Execution enabled? | `executionEnabled` | boolean | `REAL_COPY_EXECUTION_ENABLED` **and** send-gate conjunction (A49). Default **false**. |
| Open orders | `openOrders` | integer ≥ 0 | **Internal** count after last applied recon repair (A47 §12.4) |
| Open destination positions | `openDestinationPositions` | integer ≥ 0 | Same. XAU dest positions, not MT5 |
| Last execution report | `lastExecutionReport` | object | See below. Null fields when none |
| Last reconciliation | `lastReconciliation` | object | A47 status enum |

```json
"lastExecutionReport": {
  "at": null,
  "execType": null,
  "ordStatus": null,
  "clOrdId": null,
  "destinationOrderId": null
}
```

`clOrdId` / `destinationOrderId` are **strings**. `execType` / `ordStatus` are FIX-coded strings already persisted (`150` / `39` decoded or raw code) — never a raw `35=8` body.

```json
"lastReconciliation": {
  "at": null,
  "status": "NEVER",
  "readyForExecution": false
}
```

TRADE card **must not** include `xauusd` bid/ask. Quotes belong on QUOTE (and on SignalR `quote.xauusd` for other pages). `EfDashboardQueries` today copies the latest quote onto **every** session row — that is a contract bug this DTO forbids.

### 6.4 TRADE ownership strip (A46 §15.3 — supporting, not a second card)

§52 does not name lease fields. A46 extends the TRADE card. Include them on TRADE only:

| A46 line | JSON | Type |
|---|---|---|
| Owner instance | `ownership.ownerInstance` | string \| null |
| Fencing token | `ownership.fencingToken` | number \| null | Monotonic token. **Not** a password. Never a Redis AUTH. |
| Lease TTL remaining | `ownership.leaseTtlMs` | number \| null |
| Ownership state | `ownership.state` | `NONE` \| `HELD` \| `FENCED` \| `EXPIRED` \| `UNKNOWN` |
| Ready for execution? | `ownership.readyForExecution` | boolean | Must match recon SUCCESS ∧ lease held ∧ flags. Duplicate of `lastReconciliation.readyForExecution` is OK if they cannot disagree. |
| Last acquire / yield / fence | `ownership.lastAcquireAt` / `lastYieldAt` / `lastFenceAt` | string \| null |

First useful: all null / `NONE` / false. Never `redisPassword`.

---

## 7. Canonical JSON

### 7.1 `GET /api/v1/fix/sessions` — first useful / honest empty

TRADE may be disabled. QUOTE may be down. Instrument may be undiscovered. **Both cards still present.**

```json
{
  "data": {
    "quote": {
      "session": "QUOTE",
      "sessionQualifier": "QUOTE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5211,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
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
        "spread": null,
        "receivedAt": null,
        "venueTimestamp": null,
        "stale": true
      },
      "secretConfigured": true
    },
    "trade": {
      "session": "TRADE",
      "sessionQualifier": "TRADE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5212,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
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
      "lastExecutionReport": {
        "at": null,
        "execType": null,
        "ordStatus": null,
        "clOrdId": null,
        "destinationOrderId": null
      },
      "lastReconciliation": {
        "at": null,
        "status": "NEVER",
        "readyForExecution": false
      },
      "ownership": {
        "ownerInstance": null,
        "fencingToken": null,
        "leaseTtlMs": null,
        "state": "NONE",
        "readyForExecution": false,
        "lastAcquireAt": null,
        "lastYieldAt": null,
        "lastFenceAt": null
      },
      "secretConfigured": true
    }
  }
}
```

`secretConfigured: true` in this example means “ops populated the vault,” not “password is in JSON.” If the vault is empty, `false`. Demo/dev with no secret store → `false`.

### 7.2 Same GET — QUOTE healthy, TRADE logged on, send still off (Phase 7 shape)

```json
{
  "data": {
    "quote": {
      "session": "QUOTE",
      "sessionQualifier": "QUOTE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5211,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
      "connected": true,
      "loggedOn": true,
      "sessionStatus": "READY_FOR_MARKET_DATA",
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
        "spread": 0.16,
        "receivedAt": "2026-08-18T12:00:00.060Z",
        "venueTimestamp": "2026-08-18T12:00:00.040Z",
        "stale": false
      },
      "secretConfigured": true
    },
    "trade": {
      "session": "TRADE",
      "sessionQualifier": "TRADE",
      "host": "live-us-eqx-01.p.c-trader.com",
      "sslPort": 5212,
      "useSsl": true,
      "senderCompId": "live.pepperstone.1369850",
      "targetCompId": "cServer",
      "connected": true,
      "loggedOn": true,
      "sessionStatus": "RECONCILING",
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
        "ordStatus": null,
        "clOrdId": null,
        "destinationOrderId": null
      },
      "lastReconciliation": {
        "at": "2026-08-18T11:50:00.000Z",
        "status": "BLOCKED_PENDING_RECON",
        "readyForExecution": false
      },
      "ownership": {
        "ownerInstance": "fix-worker-01",
        "fencingToken": 7,
        "leaseTtlMs": 8400,
        "state": "HELD",
        "readyForExecution": false,
        "lastAcquireAt": "2026-08-18T11:40:00.000Z",
        "lastYieldAt": null,
        "lastFenceAt": null
      },
      "secretConfigured": true
    }
  }
}
```

`"185"` in the healthy example is **illustrative**. Production must persist the Security List id for **this** account (A44). Tests must not assert a hardcoded Pepperstone id from another login.

### 7.3 `GET /api/v1/fix/quote` / `GET /api/v1/fix/trade`

Same objects as `data.quote` / `data.trade`, wrapped in `{ "data": { ...card } }`. Do not return the sibling card. Do not return a one-element array.

### 7.4 `GET /api/v1/fix/sessions/{session}/events`

```json
{
  "data": [
    {
      "eventId": "f1111111-0000-4000-8000-000000000001",
      "session": "QUOTE",
      "at": "2026-08-18T11:59:00.000Z",
      "eventType": "LOGON",
      "success": true,
      "code": null,
      "text": "logged on",
      "sequence": { "nextSender": 2, "nextTarget": 2 }
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 1,
  "totalPages": 1
}
```

Forbidden on an event row: `raw`, `fixRaw`, `message`, `body`, `tags`, `password`, `rawData`, any `35=A` string.

Unknown `{session}` → `404`. ReadOnly+ may read.

---

## 8. Proposed C# allow-list (not implemented by this agent)

Place later under `TraderIntelligence.Application.Dashboard` (or `...Contracts.Fix`). **Do not** extend `FixSessionDto` with optional quote fields and keep returning `IReadOnlyList<FixSessionDto>` as the public API.

```csharp
public sealed record FixPageDto(
    QuoteSessionCardDto Quote,
    TradeSessionCardDto Trade);

public sealed record FixSequenceDto(int NextSender, int NextTarget);

public sealed record FixSessionErrorDto(
    DateTimeOffset At,
    string Code,
    string Text);

public sealed record XauQuoteDto(
    bool Mapped,
    string? InstrumentId,
    decimal? Bid,
    decimal? Ask,
    int? QuoteAgeMs,
    decimal? Spread,
    DateTimeOffset? ReceivedAt,
    DateTimeOffset? VenueTimestamp,
    bool Stale);

public sealed record LastExecutionReportDto(
    DateTimeOffset? At,
    string? ExecType,
    string? OrdStatus,
    string? ClOrdId,
    string? DestinationOrderId);

public sealed record LastReconciliationDto(
    DateTimeOffset? At,
    string Status,
    bool ReadyForExecution);

public sealed record TradeOwnershipDto(
    string? OwnerInstance,
    long? FencingToken,
    int? LeaseTtlMs,
    string State,
    bool ReadyForExecution,
    DateTimeOffset? LastAcquireAt,
    DateTimeOffset? LastYieldAt,
    DateTimeOffset? LastFenceAt);

public sealed record QuoteSessionCardDto(
    string Session,
    string SessionQualifier,
    string Host,
    int SslPort,
    bool UseSsl,
    string SenderCompId,
    string TargetCompId,
    bool Connected,
    bool LoggedOn,
    string SessionStatus,
    DateTimeOffset? LastInboundAt,
    DateTimeOffset? LastOutboundAt,
    FixSequenceDto Sequence,
    int ReconnectCount,
    DateTimeOffset? LastHeartbeatAt,
    DateTimeOffset? LastTestRequestAt,
    IReadOnlyList<FixSessionErrorDto> Errors,
    XauQuoteDto Xauusd,
    bool SecretConfigured,
    DateTimeOffset? SecretLastRotatedAt);

public sealed record TradeSessionCardDto(
    string Session,
    string SessionQualifier,
    string Host,
    int SslPort,
    bool UseSsl,
    string SenderCompId,
    string TargetCompId,
    bool Connected,
    bool LoggedOn,
    string SessionStatus,
    DateTimeOffset? LastInboundAt,
    DateTimeOffset? LastOutboundAt,
    FixSequenceDto Sequence,
    int ReconnectCount,
    DateTimeOffset? LastHeartbeatAt,
    DateTimeOffset? LastTestRequestAt,
    IReadOnlyList<FixSessionErrorDto> Errors,
    bool ExecutionEnabled,
    int OpenOrders,
    int OpenDestinationPositions,
    LastExecutionReportDto LastExecutionReport,
    LastReconciliationDto LastReconciliation,
    TradeOwnershipDto Ownership,
    bool SecretConfigured,
    DateTimeOffset? SecretLastRotatedAt);

public sealed record FixSessionEventDto(
    Guid EventId,
    string Session,
    DateTimeOffset At,
    string EventType,
    bool? Success,
    string? Code,
    string? Text,
    FixSequenceDto? Sequence);
```

JSON names are camelCase via ASP.NET defaults (`xauusd` stays `xauusd`, not `xauUsd` — pin `[JsonPropertyName("xauusd")]` on `Xauusd`).

`IDashboardQueries.GetFixSessionsAsync` today returns `IReadOnlyList<FixSessionDto>`. When the API is wired, **replace** that method with `Task<FixPageDto> GetFixPageAsync(...)` (or add it and stop exposing the list). Do not keep both as public contracts.

### 8.1 Mapping notes for `EfDashboardQueries` (current bugs to fix later)

Path: `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` `GetFixSessionsAsync` (lines 125–147).

| Current behavior | Required |
|---|---|
| `sessions.Select` → list | Always emit `{ Quote, Trade }` even if 0–1 rows |
| `Status.ToString()` → `ReadyForMarketData` | Wire enum §5.2 |
| `Port` | `sslPort`; if `useSsl=false` still show configured SSL port and `useSsl:false` (should not be prod) |
| Quote copied onto every row | `xauusd` only on QUOTE |
| `QuoteAgeSeconds` (`double?`) | `quoteAgeMs` (`int?`) |
| `LastError` singular | `errors[]` |
| `ExecutionEnabled: false` hardcoded on **both** | TRADE only; still false until flag |
| No heartbeat / test / ER / recon / ownership | Add nullable fields |
| `FixSessionState` loaded fully | Select columns; never `SenderSubId` / `TargetSubId` into the DTO |
| Demo quote `VenueInstrumentId = null` with a bid | `mapped: false`, `instrumentId: null`. Bid/ask may still show as unmapped tape — UI must not claim “XAUUSD mapped?” |

Demo seeder (`DemoSeeder.cs`) seeds QUOTE `ReadyForMarketData` and TRADE `LoggedOn` **without** a password. Good. It also seeds a quote with `VenueInstrumentId = null` — the page DTO must not infer mapped from a bid.

---

## 9. Proposed TypeScript (React)

Replace `apps/web/src/types/index.ts` `FixSession` (array, mixed extras) when the page is implemented. Do **not** import C# numeric enums.

```ts
export type FixSessionName = 'QUOTE' | 'TRADE';

export type FixSessionStatus =
  | 'DOWN'
  | 'CONNECTING'
  | 'LOGON_SENT'
  | 'LOGGED_ON'
  | 'RECONCILING'
  | 'READY_FOR_MARKET_DATA'
  | 'READY_FOR_EXECUTION'
  | 'LOGGED_OUT'
  | 'RECONNECTING'
  | 'SEQUENCE_RESET'
  | 'DISABLED'
  | 'ERROR';

export type ReconCardStatus =
  | 'READY_FOR_EXECUTION'
  | 'BLOCKED_INCONSISTENT'
  | 'BLOCKED_PENDING_RECON'
  | 'BLOCKED_STALE'
  | 'NEVER';

export interface FixSequence {
  nextSender: number;
  nextTarget: number;
}

export interface FixSessionError {
  at: string;
  code: string;
  text: string;
}

export interface XauQuote {
  mapped: boolean;
  instrumentId: string | null;
  bid: number | null;
  ask: number | null;
  quoteAgeMs: number | null;
  spread: number | null;
  receivedAt: string | null;
  venueTimestamp: string | null;
  stale: boolean;
}

export interface LastExecutionReport {
  at: string | null;
  execType: string | null;
  ordStatus: string | null;
  clOrdId: string | null;
  destinationOrderId: string | null;
}

export interface LastReconciliation {
  at: string | null;
  status: ReconCardStatus;
  readyForExecution: boolean;
}

export interface TradeOwnership {
  ownerInstance: string | null;
  fencingToken: number | null;
  leaseTtlMs: number | null;
  state: 'NONE' | 'HELD' | 'FENCED' | 'EXPIRED' | 'UNKNOWN';
  readyForExecution: boolean;
  lastAcquireAt: string | null;
  lastYieldAt: string | null;
  lastFenceAt: string | null;
}

interface FixCardBase {
  session: FixSessionName;
  sessionQualifier: FixSessionName;
  host: string;
  sslPort: number;
  useSsl: boolean;
  senderCompId: string;
  targetCompId: string;
  connected: boolean;
  loggedOn: boolean;
  sessionStatus: FixSessionStatus;
  lastInboundAt: string | null;
  lastOutboundAt: string | null;
  sequence: FixSequence;
  reconnectCount: number;
  lastHeartbeatAt: string | null;
  lastTestRequestAt: string | null;
  errors: FixSessionError[];
  secretConfigured: boolean;
  secretLastRotatedAt?: string | null;
}

export interface QuoteSessionCard extends FixCardBase {
  session: 'QUOTE';
  sessionQualifier: 'QUOTE';
  xauusd: XauQuote;
}

export interface TradeSessionCard extends FixCardBase {
  session: 'TRADE';
  sessionQualifier: 'TRADE';
  executionEnabled: boolean;
  openOrders: number;
  openDestinationPositions: number;
  lastExecutionReport: LastExecutionReport;
  lastReconciliation: LastReconciliation;
  ownership: TradeOwnership;
}

export interface FixPage {
  quote: QuoteSessionCard;
  trade: TradeSessionCard;
}
```

Fetcher (later): `GET /api/v1/fix/sessions` → `{ data: FixPage }`. Client denylist (A62): if a GET matches `(?i)(password|passwd|pwd|rawdata|connectionstring|privatekey|proxyuser)`, do not render, do not cache, show a generic error. Server sanitizer remains mandatory.

Page module (A62): `pages/fix/CTraderFixPage.tsx`. Nav label **exactly** `cTrader FIX` (§46). Two independent cards. Never a password input. Never render `secretConfigured` as a password reveal.

---

## 10. SignalR (same DTO family)

Hub `/hubs/ops` (A26 §7). Payloads are **subsets** of the GET cards. Same sanitizer.

| Event | Payload | Invalidates |
|---|---|---|
| `fix.session` | `{ session, connected, loggedOn, sessionStatus, reconnectCount, sequence?, xauusd? }` | `['fix']` |
| `quote.xauusd` | `{ instrumentId, bid, ask, quoteAgeMs, spread, mapped, at }` | `['fix']`, Risk, Shadow |

`xauusd` on `fix.session` is allowed **only** when `session === "QUOTE"`. TRADE session events must not carry bid/ask.

Header strip (all pages) uses `GET /overview` `quoteHealthy` / `tradeHealthy` plus these events — not a third DTO.

---

## 11. RBAC

| Action | ReadOnly | Analyst | RiskManager | SuperAdmin |
|---|---|---|---|---|
| GET sessions / quote / trade / events | R | R | R | R |
| PATCH settings/fix (non-secret) | — | — | — | W (Settings page, not this page) |
| Any password field | — | — | — | **422**, even SuperAdmin from React |
| Enable real execution | — | — | — | Settings + confirm; **not** this page |

Unauthenticated → `401`. This page has no privileged mutation (A51: `dash.read`).

---

## 12. Current tree vs this contract

| Path | Measured state | Vs A94 |
|---|---|---|
| Architecture §52 | Binding text | Source of law |
| `apps/api/Program.cs` | `GET /weatherforecast` only | Routes **MISSING** |
| `Application/Dashboard/DashboardModels.cs` `FixSessionDto` | Flattened list row; `Port`, `InboundSeq`/`OutboundSeq`, `LastError`, `QuoteAgeSeconds`, quote fields on every row | **Superseded** as public page DTO |
| `IDashboardQueries.GetFixSessionsAsync` | Returns the list | Replace / stop exposing |
| `Infrastructure/Dashboard/EfDashboardQueries.cs` | Maps entity + latest quote onto both rows; `ExecutionEnabled: false` | Projection bugs listed in §8.1 |
| `Domain/Entities/FixSessionState.cs` | Has `SenderSubId` / `TargetSubId`; **no password** | Never serialize |
| `Fix.CTrader/Configuration/CTraderFixOptions.cs` | `Password` + `AccountId` “must never be logged” | **Must never be serialized** either |
| `Infrastructure/Seeding/DemoSeeder.cs` | Two session rows; quote id null | Honest unmapped quote |
| `apps/web/src/types/index.ts` `FixSession` | `type: QUOTE\|TRADE` array; mixed optionals | Replace with `FixPage` |
| `apps/web/src/api/hooks.ts` `useFixSessions` | `GET /api/fix/sessions`, 5s poll | Wrong path; wrong shape |
| `apps/web/src/App.tsx` | Imports missing `FixSessionsPage` | Page **MISSING** |
| `apps/web/src/layouts/DashboardLayout.tsx` | Label `FIX` | Must be `cTrader FIX` |
| A26 §6.11 / A63 §5.8 | Two-card JSON sketch | Compatible; this file is the lock + enums + ownership + sanitizer |

---

## 13. Tests that must exist before the page is called done

Product tests are a later increment. Contract they must prove:

| Test | Must prove |
|---|---|
| `FixPageDto.AlwaysTwoCardsTests` | Serializer always has `quote` and `trade` keys when 0, 1, or 2 `fix_sessions` rows exist |
| `FixPageDto.IndependentSequencesTests` | Mutating QUOTE seq does not change TRADE seq in the payload |
| `FixPageDto.QuoteExtrasOnlyOnQuoteTests` | TRADE JSON has no `xauusd`; QUOTE JSON has no `executionEnabled` / `lastExecutionReport` |
| `FixPageDto.PasswordNeverSerializedTests` | Options with a non-empty `Password` still produce JSON that does not contain the value, `password`, `554`, or `RawData` (case-insensitive) |
| `FixPageDto.SenderSubIdOmittedTests` | Entity `SenderSubId` / `TargetSubId` do not appear as keys |
| `FixPageDto.SecretConfiguredIsBooleanTests` | Vault empty → `false`; vault present → `true`; value absent |
| `FixPageDto.MappedRequiresActiveInstrumentTests` | Bid without active `destination_symbols` → `mapped: false`, `instrumentId: null` |
| `FixPageDto.InstrumentIdIsStringTests` | Numeric venue id serializes as JSON string |
| `FixPageDto.QuoteAgeIsMillisecondsTests` | 1.5s age → `1500`, not `1.5` |
| `FixPageDto.SessionStatusWireMapTests` | Every domain enum value maps to §5.2; never PascalCase / integers |
| `FixPageDto.DisabledWhenFlagOffTests` | `CTRADER_FIX_TRADE_SESSION_ENABLED=false` → TRADE `DISABLED` |
| `FixPageDto.ExecutionEnabledNotInferredFromLogonTests` | TRADE `LOGGED_ON` + copy flag false → `executionEnabled: false` |
| `FixPageDto.EventsRedactLogonTests` | Events GET cannot contain `35=A` or tag `554` |
| `FixPageDto.SettingsPassword422Tests` | Any password key on FIX settings PATCH → `422 SECRET_FIELD_REJECTED` |
| `FixPageDto.SanitizerFailClosedTests` | If a secret remains after redaction, no `200` body is sent |

Do **not** use account `1369850` as the first integration test (architecture §61). Use the harness / recorded fixtures.

---

## 14. Acceptance (this spec)

```text
[x] §52 shared fields have a JSON path
[x] QUOTE extras have a JSON path
[x] TRADE extras have a JSON path
[x] Two cards are mandatory in the envelope
[x] Password / RawData / 554 / SenderSubID are forbidden
[x] secretConfigured is the only secret metadata
[x] First-useful TRADE DISABLED payload is specified
[x] Current FixSessionDto / EfDashboardQueries / React types called out as non-compliant
[x] No product source modified
```

Page implementation is **not** accepted until:

```text
[ ] GET /api/v1/fix/sessions returns FixPageDto (not a list, not weatherforecast)
[ ] React /fix renders two cards labeled QUOTE SESSION and TRADE SESSION
[ ] Nav label is cTrader FIX
[ ] Password field does not exist in the page or Settings FIX form
[ ] Contract tests in §13 pass
[ ] TRADE card visible when session is DISABLED
[ ] Instrument ID is the persisted discovery, not a hardcoded 55
```

---

## 15. What this agent did

Wrote **only** `D:\Prop\reports\swarm\20260818\A94_fix_page_dto.md`.

Did **not** modify `apps/`, `src/`, `tests/`, `docs/`, or any other swarm file.
