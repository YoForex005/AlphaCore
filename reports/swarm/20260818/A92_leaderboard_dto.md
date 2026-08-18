# A92 — Trader Leaderboard query filters, sort, and JSON contract

| Field | Value |
|---|---|
| Agent | A92 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A92_leaderboard_dto.md` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§50** |
| Supporting law | §§10, 15, 18, 22–24, 46, 49, 51, 55, 59, 69 (items 7–8), 72.5 |
| Sibling specs | A20 tables, A22 `baseline.v1` scores, A24 shadow P&L, A26 dashboard API, A43 sizing, A51 RBAC, A52 ML-not-yet, A57 rank item, A62 React URL, A63 first-useful catalog, A69 states |
| Product source modified | **No** |
| Status | Binding implementer contract. Not code. Not OpenAPI generation. |

**This file wins** for `GET /api/v1/traders` — query parameter names, AND/OR rules, sort grammar, default order, null policy, score **wire scale**, list envelope, and the leaderboard **row DTO**.  
A26 / A63 remain the catalog for every *other* route. Where they sketch the leaderboard and this file is more specific, **this file replaces the sketch**.

---

## 0. What §50 requires (quoted)

Columns:

```text
Broker
Login
Group
Completed XAU trades

Net source P&L
Early score
ML probability
Risk score

Martingale flag
Averaging-down flag
Lot escalation flag

Current state
Shadow P&L
Live allocation
Last scored
```

Filters:

```text
broker
group
state
score
risk
trade count
martingale
date
```

Architecture §50 does **not** name HTTP, types, defaults, or sort. Those are locked here so React (`/traders`) and the API implement one contract.

---

## 1. Current measured state (honest)

| Surface | Path | Classification |
|---|---|---|
| Architecture §50 | columns + 8 filter words | specified, not implemented |
| `GET /api/v1/traders` | `D:\Prop\apps\api\Program.cs` | **MISSING** (host is still `GET /weatherforecast`) |
| Application sketch | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` `TraderRowDto` + `IDashboardQueries.GetTradersAsync(string? broker, string? state)` | **EXISTS_NEEDS_REFACTOR** — two filters only; no `brokerId`, no `liveAllocation`, flags flattened, scores non-nullable |
| Domain scores | `TraderScore` / `BaselineScorer` | scores are **`[0, 100]`** (`RiskScore` / `BehaviorScore` / `EarlyQualityScore`) |
| Persistence | `trader_scores` unique `(BrokerId, Login)` in `TraderDbContext` | current-row exists; no query API |
| React stub | `apps/web/src/api/hooks.ts` `useTraders` | **non-normative** — calls `/api/traders` (unversioned), uses `minScore`/`maxScore`, envelope `{ items, total }` |
| A26 §6.4 / A63 §5.4 | filter table + example JSON | sketch; **0–1 score examples are superseded** (see §14) |

No ranking query exists. A57 item 8 remains **MISSING**.

---

## 2. Binding laws

| ID | Law |
|---|---|
| L1 | Source identity is always `{ brokerId, login }`. Login is **never** globally unique (§10). |
| L2 | One row per `(brokerId, login)`. Never fold two brokers’ same login into one line. |
| L3 | Leaderboard **Early score** **is** `early_quality_score` (A22 §7, A63). Wire name: `earlyScore`. |
| L4 | Official scores use window `EXPANDING` and family `baseline.v1`. Do not rank `FIRST3` or `PROVISIONAL` on this list. |
| L5 | **Wire scale for `earlyScore`, `riskScore`, `behaviorScore` is `[0, 100]`**, `decimal` quantized to 2 places (`Round2`, A22 §2.5). Higher `earlyScore` = better. Higher `riskScore` = riskier. |
| L6 | `mlProbability` is a **probability** in `[0, 1]` or **`null`**. In first useful / until a promoted model exists it is **always `null`**. Never stub `0`. Render `—` (A52, A62). |
| L7 | `N < 3` (or never scored): `earlyScore`, `riskScore`, `behaviorScore`, `lastScoredAt` are **`null`**. Do **not** emit `0` as a fake score. State is `INSUFFICIENT_DATA` unless a higher-priority A69 override already fired. |
| L8 | Filters combine with **AND**. Repeatable `state` is **OR inside the set**. |
| L9 | Default sort is the A57 ranking chain (see §6). Unscored rows are **never** first: `NULLS LAST` on every sort key. |
| L10 | Allow-listed query keys only. Unknown key → `400 VALIDATION_FAILED`. Denylisted secret key → `422 SECRET_FIELD_REJECTED`. |
| L11 | No secrets on the row, in `query` echo, in errors, or in SignalR patches (§55). |
| L12 | Roles: any authenticated role (`ReadOnly+` / A51 `dash.read`). Unauthenticated → `401`. |
| L13 | GET is read-only. This resource does not mutate state. `PATCH .../state` is A63 / A69, not this file. |
| L14 | Count only **completed reconstructed XAUUSD** lifecycles for `completedXauTrades` and `netSourcePnl` (§15, A22 I1). |
| L15 | Shadow P&L is destination-quote marked (A24). Source P&L must not be copied into `shadowPnl`. |
| L16 | JSON camelCase, `application/json; charset=utf-8`, times ISO-8601 UTC with `Z`. |

---

## 3. Endpoint

| Item | Contract |
|---|---|
| Method / path | `GET /api/v1/traders` |
| Auth | `Authorization: Bearer <access_token>` |
| Content-Type (response) | `application/json; charset=utf-8` |
| Request body | **None**. Filters and sort are the query string. |
| Idempotency-Key | Not used (GET). |
| Correlation | Echo `X-Correlation-Id`. |

There is **no** `POST /api/v1/traders/query` in v1. Clients that prefer an object build the same shape as §4 and serialize it to the query string (A62 `searchParams.ts`).

Parsed query + response **are** the JSON contract.

---

## 4. Parsed request object (`LeaderboardQuery`)

This is what the server binds from the query string and what it **echoes** under `query` in the response (resolved defaults filled in).

```json
{
  "brokerId": null,
  "broker": null,
  "groupId": null,
  "group": null,
  "state": [],
  "minEarlyScore": null,
  "maxEarlyScore": null,
  "minRiskScore": null,
  "maxRiskScore": null,
  "minCompletedXauTrades": null,
  "maxCompletedXauTrades": null,
  "martingale": null,
  "averagingDown": null,
  "lotEscalation": null,
  "scoredFrom": null,
  "scoredTo": null,
  "q": null,
  "enabledForAnalysis": "true",
  "sort": [
    { "field": "earlyScore", "dir": "desc" },
    { "field": "riskScore", "dir": "asc" },
    { "field": "completedXauTrades", "dir": "desc" },
    { "field": "brokerId", "dir": "asc" },
    { "field": "login", "dir": "asc" }
  ],
  "page": 1,
  "pageSize": 50
}
```

`null` / `[]` means “filter not applied.” `enabledForAnalysis` and `sort` / `page` / `pageSize` are **always** present in the echo because they have defaults.

---

## 5. Query filters

### 5.1 Map from architecture §50 words

| §50 word | Query parameter(s) | Combine |
|---|---|---|
| broker | `brokerId` (uuid) and/or `broker` (registry code) | AND with everything else; both set must resolve to the **same** broker |
| group | `group` (exact path) and/or `groupId` (uuid) | both set must resolve to the same group |
| state | `state` (repeatable) | **OR** among values, AND with other filters |
| score | `minEarlyScore`, `maxEarlyScore` | inclusive range on `earlyScore` |
| risk | `minRiskScore`, `maxRiskScore` | inclusive range on `riskScore` (higher = riskier) |
| trade count | `minCompletedXauTrades`, `maxCompletedXauTrades` | inclusive range on `N` |
| martingale | `martingale` | exact bool match on current flag |
| date | `scoredFrom`, `scoredTo` | inclusive on `lastScoredAt` |

**Sibling flag filters** (not named in the §50 word list, but the three flag **columns** exist; A26/A63 already list them):

| Query | Column |
|---|---|
| `averagingDown` | Averaging-down flag |
| `lotEscalation` | Lot escalation flag |

**Operational extras** (needed to operate the page; not extra ranking features):

| Query | Purpose |
|---|---|
| `q` | login **contains** (digits) |
| `enabledForAnalysis` | group analysis gate (§49) |
| `sort` | see §6 |
| `page`, `pageSize` | see §7 |

Do **not** add `plan`, `minBehaviorScore`, `universe`, `hasMl`, or free-text name search in v1.

### 5.2 Parameter dictionary

Types are **after** bind. Encoding is `application/x-www-form-urlencoded` on the query string.

| Name | Bind type | Repeatable | Default | Predicate |
|---|---|---|---|---|
| `brokerId` | uuid | no | omitted | `row.brokerId = brokerId` |
| `broker` | string | no | omitted | case-insensitive match on `brokers.code` (`ACHIEVER`, `STARWAVEFX`, …) then same as `brokerId` |
| `groupId` | uuid | no | omitted | `mt5_groups.id = groupId` |
| `group` | string | no | omitted | **exact** match on stored group path (`demo\yo-2step`). Case-sensitive. `/` is **not** an alias for `\`. URL-encode `\` as `%5C` |
| `state` | `TraderState` token | **yes** | omitted = all states | `row.state IN (...)` |
| `minEarlyScore` | number `[0, 100]` | no | omitted | `earlyScore >= min` (**implies** `earlyScore IS NOT NULL`) |
| `maxEarlyScore` | number `[0, 100]` | no | omitted | `earlyScore <= max` (implies not null) |
| `minRiskScore` | number `[0, 100]` | no | omitted | `riskScore >= min` (implies not null) |
| `maxRiskScore` | number `[0, 100]` | no | omitted | `riskScore <= max` (implies not null) |
| `minCompletedXauTrades` | int `>= 0` | no | omitted | `completedXauTrades >= min` |
| `maxCompletedXauTrades` | int `>= 0` | no | omitted | `completedXauTrades <= max` |
| `martingale` | bool | no | omitted | `flags.martingale = value` |
| `averagingDown` | bool | no | omitted | `flags.averagingDown = value` |
| `lotEscalation` | bool | no | omitted | `flags.lotEscalation = value` |
| `scoredFrom` | datetime UTC | no | omitted | `lastScoredAt >= scoredFrom` (implies not null) |
| `scoredTo` | datetime UTC | no | omitted | `lastScoredAt <= scoredTo` (implies not null) |
| `q` | string | no | omitted | `login::text LIKE '%' \|\| digits(q) \|\| '%'` |
| `enabledForAnalysis` | `true` \| `false` \| `any` | no | **`true`** | see §5.8 |
| `sort` | string `field:dir` | **yes** (max **5** client keys) | A57 chain §6.3 | see §6 |
| `page` | int `>= 1` | no | **1** | 1-based |
| `pageSize` | int `1..200` | no | **50** | reject `> 200` (do not silent-clamp) |

Unknown names, including camelCase typos (`minScore`, `broker_id`, `sortBy`, `order`) → **400**.

### 5.3 Boolean bind

Accepted (case-insensitive) after trim:

| True | False |
|---|---|
| `true`, `1`, `yes` | `false`, `0`, `no` |

Empty value (`?martingale=`) → treat as **omitted**. Any other token → 400.

Omitted ≠ `false`. Omitted means “do not filter on this flag.”

### 5.4 `state` bind

Legal tokens — **exact**, case-sensitive, A69 / §22 vocabulary only:

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

Two equivalent encodings (union, de-duplicated, order does not matter):

```text
state=SHADOW&state=WATCH
state=SHADOW,WATCH
```

Both may appear; the set is the union. Empty token (`state=` or `state=,WATCH`) → 400.  
Aliases (`Shadow`, `shadow`, `LIVE_COPIED`, `PROVEN_PROFITABLE`) → 400.  
Max **9** distinct values (the full enum). More → 400.

### 5.5 Score / risk range

- Inclusive on both ends.
- Bind as `decimal`. More than 2 fractional digits → Round2 for the predicate (document the rounded value in `query` echo).
- `min* > max*` (when both present) → 400.
- Value `< 0` or `> 100` → 400.
- **Null exclusion:** any bound **drops** rows whose score is `null`. A client that wants only official scores should send `minCompletedXauTrades=3` **or** any score bound. There is no `scored=true` flag.

`mlProbability` is **not** filterable in v1 (it is always null).

### 5.6 Trade-count range

- Integers only (`3.5` → 400).
- `minCompletedXauTrades > maxCompletedXauTrades` → 400.
- No implicit floor. The **React page** should seed `minCompletedXauTrades=3` in the URL for the useful ranking view (A62 persist-in-URL). The API does not force that floor.

### 5.7 Date range (`date` → last scored)

- Field: `lastScoredAt` (A22 current-row `as_of` / `LastScoredAt`).
- Full instant: `2026-08-18T11:40:00.000Z` (any ISO-8601 UTC; normalize to `Z` in the echo).
- **Date-only** `YYYY-MM-DD`:
  - `scoredFrom=2026-08-18` → `2026-08-18T00:00:00.000Z`
  - `scoredTo=2026-08-18` → `2026-08-18T23:59:59.999Z`
- Offset-less local datetimes → 400 (do not assume broker server time).
- `scoredFrom > scoredTo` after normalize → 400.
- Either bound **excludes** `lastScoredAt == null` (unscored / `N < 3`).

This is **not** account registration date and **not** last-deal time.

### 5.8 `enabledForAnalysis` (group gate)

Universe is `mt5_accounts` joined to `mt5_groups` on `(broker_id, group_name)`.

| Value | Predicate |
|---|---|
| omitted / `true` | group `enabled_for_analysis = true`. Account whose group is **unknown** (null / not yet discovered) is **included** (fail open on missing metadata, not on disabled). |
| `false` | group exists and `enabled_for_analysis = false` |
| `any` | no group-enabled predicate |

Do not hide disabled groups by inventing a second endpoint. Ops uses `any` + `group=` to inspect a named path.

### 5.9 `q` (login contains)

- Trim; strip all non-digits; remaining length **1..20**.
- After strip, empty → 400 (`q` was present but not a login fragment).
- Match is **substring** on the decimal representation of `login` with no leading zeros (`610` matches `6100421`, not `0610…`).
- Does **not** search group, broker name, or comments.

### 5.10 Broker / group identity resolution

1. If `broker` set: resolve `brokers.code` (ordinal ignore-case). Unknown code → **404** `NOT_FOUND` (broker), not an empty page.
2. If `brokerId` set: unknown uuid → **404**.
3. If both set and they resolve to different brokers → **400** (`BROKER_FILTER_CONFLICT` in `error.details`).
4. `group` / `groupId` analogously; unknown → **404**. Group path is broker-scoped: if `brokerId`/`broker` is also set and the group belongs to another broker → **400** (`GROUP_BROKER_MISMATCH`).
5. If only `group` is set (no broker): exact path match **across** brokers. Two brokers can theoretically share a path string; both rows may appear (compound identity still distinguishes them).

### 5.11 What is **not** a filter

| Reject | Why |
|---|---|
| Rank-by-P&L as default | A22 I9 / §3 — Early score is the ranking key |
| `minNetSourcePnl` | not in §50 filter list; P&L remains a **column + sort key** only |
| `minShadowPnl` / `minLiveAllocation` | same |
| `password`, `mt5Password`, FIX secrets | 422 denylist |
| `revealLogin` | SuperAdmin broker detail only (A26/A63), not this list |

---

## 6. Sort contract

### 6.1 Grammar

```text
sort = field [ ":" dir ]
dir  = "asc" | "desc"          // case-insensitive
field = allow-listed camelCase // case-sensitive
```

Examples:

```text
sort=earlyScore:desc
sort=earlyScore:desc&sort=riskScore:asc
sort=earlyScore
```

Bare `sort=earlyScore` (no `:dir`) → `earlyScore:desc`.

Repeatable `sort`. **Maximum 5 client-supplied keys.** A 6th → 400.  
Comma form `sort=earlyScore:desc,riskScore:asc` is **accepted** as an equivalent to two `sort=` params (left-to-right). Mixing comma and repeats is allowed; flatten left-to-right.

Empty `sort=` → 400.  
Unknown field → 400 (`UNKNOWN_SORT_FIELD`).  
Unknown dir (`sort=earlyScore:up`) → 400.  
Duplicate field (same `field` twice) → 400.

### 6.2 Allow-list (JSON field → SQL)

| `field` | Source column / expression | Notes |
|---|---|---|
| `earlyScore` | `trader_scores.early_quality_score` | default primary |
| `riskScore` | `trader_scores.risk_score` | |
| `behaviorScore` | `trader_scores.behavior_score` | extra field; allowed |
| `mlProbability` | `model_predictions.p` (promoted only) | all-null in v1; order is stable via tie-break |
| `completedXauTrades` | `trader_scores.completed_xau_trades` else count of completed XAU reconstructions | |
| `netSourcePnl` | sum of completed XAU `reconstructed_trades` net | |
| `shadowPnl` | current shadow book P&L (A24) | |
| `liveAllocation` | open destination XAU qty allocated to this source trader | 0 until live copy |
| `lastScoredAt` | `trader_scores.last_scored_at` | |
| `login` | `mt5_accounts.login` | |
| `brokerId` | `mt5_accounts.broker_id` | uuid ordinal |
| `broker` | `brokers.display_name`, then `broker_id` | display label |
| `brokerDisplayName` | alias of `broker` | |
| `group` | `mt5_accounts.group_name` | UTF-8 ordinal |
| `state` | `trader_states.state` / `trader_scores.current_state` | **enum ordinal** (A69 / `TraderState`: `INSUFFICIENT_DATA=0` … `DISQUALIFIED=8`), **not** A–Z |
| `martingale` | current flag | `false < true` for `asc` |
| `averagingDown` | current flag | |
| `lotEscalation` | current flag | |

Rejected (examples): `score`, `minEarlyScore`, `pnl`, `early_quality_score`, `createdAt`, `balance`.

### 6.3 Default sort (when `sort` omitted)

Locked from A57 item 8 + stable identity:

```text
earlyScore          DESC  NULLS LAST
riskScore           ASC   NULLS LAST
completedXauTrades  DESC  NULLS LAST
brokerId            ASC
login               ASC
```

This is the **ranking** definition for “sort traders by the deterministic early/quality score.”

If the client sends a partial `sort` list, the server **appends** any missing of `{ brokerId asc, login asc }` at the end (without counting against the 5-key cap) so pagination is stable. Do not append `earlyScore`/`riskScore`/`completedXauTrades` unless the client omitted `sort` entirely.

Echo the **full resolved** list (client keys + appended identity) in `query.sort`.

### 6.4 NULLS LAST (mandatory)

For every nullable key (`earlyScore`, `riskScore`, `behaviorScore`, `mlProbability`, `lastScoredAt`, and any outer-joined P&L that can be null):

```text
ASC  → NULLS LAST
DESC → NULLS LAST
```

Unscored traders (`N < 3`) therefore sit **below** official scores on the default ranking. They are not hidden unless a score bound or `minCompletedXauTrades` excludes them.

Boolean nulls: flags are never null (default `false` when no `trader_risk_flags` row).

### 6.5 Determinism

Same `(dataset, query, sort)` → same `data[]` order, including ties. No `RANDOM()`, no “recently viewed,” no client-local reorder of `data`.

---

## 7. Pagination

| Item | Rule |
|---|---|
| Style | Offset page, **not** cursor (A26/A63) |
| `page` | 1-based. `< 1` → 400 |
| `pageSize` | default **50**, max **200**. `0`, negative, or `> 200` → 400 |
| `totalItems` | `COUNT(*)` of the **same** WHERE as the page (filters only; sort does not change the count) |
| `totalPages` | `0` if `totalItems = 0`, else `ceil(totalItems / pageSize)` |
| `page > totalPages` | **200** with `data: []` and the real totals. Not 404 |

`OFFSET (page - 1) * pageSize LIMIT pageSize` after `ORDER BY` §6.

---

## 8. Validation and errors

Envelope (A26/A63):

```json
{
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "minEarlyScore must be <= maxEarlyScore.",
    "details": {
      "fields": [
        { "name": "minEarlyScore", "reason": "RANGE_INVERTED" }
      ]
    },
    "correlationId": "b7c1e2d3-0000-4000-8000-000000000001"
  }
}
```

Never echo submitted secrets, tokens, or raw query strings that contain denylisted keys.

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | bad type, unknown key/field/dir/state, inverted range, `pageSize>200`, empty `q` after strip |
| 401 | `UNAUTHENTICATED` | missing/expired bearer |
| 403 | `FORBIDDEN` | authenticated but no `dash.read` (should not happen for the four roles) |
| 404 | `NOT_FOUND` | `broker` / `brokerId` / `group` / `groupId` does not exist |
| 422 | `SECRET_FIELD_REJECTED` | query name matches denylist (`password`, `passwd`, `secret`, `pwd`, `rawdata`, `connectionstring`, `privatekey`, `proxyuser`, …) |
| 503 | `DEPENDENCY_UNAVAILABLE` | Postgres down |

`details.fields[].reason` allow-list: `UNKNOWN_QUERY_KEY`, `UNKNOWN_SORT_FIELD`, `UNKNOWN_SORT_DIR`, `UNKNOWN_STATE`, `UNKNOWN_BOOL`, `INVALID_UUID`, `INVALID_NUMBER`, `INVALID_INT`, `INVALID_DATETIME`, `OUT_OF_RANGE`, `RANGE_INVERTED`, `BROKER_FILTER_CONFLICT`, `GROUP_BROKER_MISMATCH`, `Q_EMPTY`, `SORT_LIMIT`, `DUPLICATE_SORT_FIELD`, `PAGE_OUT_OF_RANGE`.

---

## 9. Response JSON contract

### 9.1 Envelope

```json
{
  "data": [ { } ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 612,
  "totalPages": 13,
  "generatedAt": "2026-08-18T12:00:00.000Z",
  "query": {
    "brokerId": null,
    "broker": null,
    "groupId": null,
    "group": null,
    "state": [],
    "minEarlyScore": null,
    "maxEarlyScore": null,
    "minRiskScore": null,
    "maxRiskScore": null,
    "minCompletedXauTrades": 3,
    "maxCompletedXauTrades": null,
    "martingale": null,
    "averagingDown": null,
    "lotEscalation": null,
    "scoredFrom": null,
    "scoredTo": null,
    "q": null,
    "enabledForAnalysis": "true",
    "sort": [
      { "field": "earlyScore", "dir": "desc" },
      { "field": "riskScore", "dir": "asc" },
      { "field": "completedXauTrades", "dir": "desc" },
      { "field": "brokerId", "dir": "asc" },
      { "field": "login", "dir": "asc" }
    ],
    "page": 1,
    "pageSize": 50
  }
}
```

`query` is the **resolved** bind (defaults filled, date-only expanded, score bounds Round2, `state` de-duplicated). Clients persist **request** params in the URL; they must not write the echo back as a different schema.

Do not wrap as `{ items, total }` (React stub). Do not omit `totalItems`.

### 9.2 Row DTO (`TraderLeaderboardRow`)

Every §50 column plus the compound identity required to open §51.

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "brokerCode": "ACHIEVER",
  "brokerDisplayName": "Achiever",
  "login": 6100421,
  "groupId": "b2222222-0000-4000-8000-000000000010",
  "group": "demo\\yo-2step",
  "completedXauTrades": 7,
  "netSourcePnl": 1840.50,
  "earlyScore": 71.00,
  "behaviorScore": 68.00,
  "mlProbability": null,
  "riskScore": 22.00,
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
```

Unscored / `N < 3` example:

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "brokerCode": "ACHIEVER",
  "brokerDisplayName": "Achiever",
  "login": 7001888,
  "groupId": "b2222222-0000-4000-8000-000000000010",
  "group": "demo\\yo-2step",
  "completedXauTrades": 2,
  "netSourcePnl": 40.00,
  "earlyScore": null,
  "behaviorScore": null,
  "mlProbability": null,
  "riskScore": null,
  "flags": {
    "martingale": false,
    "averagingDown": false,
    "lotEscalation": false
  },
  "state": "INSUFFICIENT_DATA",
  "shadowPnl": 0.00,
  "liveAllocation": 0.00,
  "lastScoredAt": null
}
```

`additionalProperties` on the row is **forbidden**. Do not serialize EF entities, manager login, passwords, tickets as numbers > 2^53, or raw `TraderScore`.

### 9.3 Field dictionary

| JSON | Type | Null | Scale / unit | §50 column | Source of truth |
|---|---|---|---|---|---|
| `brokerId` | uuid string | no | — | (identity) | `mt5_accounts.broker_id` |
| `brokerCode` | string | no | `ACHIEVER` / `STARWAVEFX` / … | (identity) | `brokers.code` |
| `brokerDisplayName` | string | no | — | Broker | `brokers.display_name` |
| `login` | number (int64, MT5-safe) | no | integer login | Login | `mt5_accounts.login` |
| `groupId` | uuid string | **yes** | — | (nav) | `mt5_groups.id` if resolved |
| `group` | string | **yes** | MT5 path, JSON-escaped `\\` | Group | `mt5_accounts.group_name` |
| `completedXauTrades` | int | no | count `>= 0` | Completed XAU trades | scorer `N` / reconstruction count |
| `netSourcePnl` | number | no | USD `decimal(18,2)` | Net source P&L | sum completed XAU reconstructed net |
| `earlyScore` | number \| null | yes | **`[0, 100]`**, 2 dp | Early score | `early_quality_score` |
| `behaviorScore` | number \| null | yes | **`[0, 100]`**, 2 dp | **extra** (not a §50 column; A22 sibling) | `behavior_score` |
| `mlProbability` | number \| null | yes | `[0, 1]` or null | ML probability | promoted model only; **null in v1** |
| `riskScore` | number \| null | yes | **`[0, 100]`**, 2 dp; higher = riskier | Risk score | `risk_score` |
| `flags.martingale` | bool | no | — | Martingale flag | `trader_risk_flags` / `TraderScore.Martingale` |
| `flags.averagingDown` | bool | no | — | Averaging-down flag | same |
| `flags.lotEscalation` | bool | no | — | Lot escalation flag | same |
| `state` | enum string | no | A69 token | Current state | `trader_states.state` (fallback `TraderScore.CurrentState`) |
| `shadowPnl` | number | no | USD `decimal(18,2)` | Shadow P&L | A24 shadow book; `0.00` if never shadowed |
| `liveAllocation` | number | no | destination XAU **quantity** `decimal(18,8)` | Live allocation | sum of open live copy qty for this source trader; **`0` while execution is off** |
| `lastScoredAt` | string \| null | yes | ISO-8601 UTC `Z` | Last scored | `trader_scores.last_scored_at` |

`behaviorScore` may be omitted from the **React table** (it is not a §50 column). It **must** be present on the wire so sort `behaviorScore` and the scoring page can reuse the row without a second fetch.

Flags stay nested. Do not also emit top-level `martingale` (DashboardModels sketch).

`login` is a JSON number because MT5 logins fit in IEEE-754 safely. Tickets / FIX ids on **other** resources stay strings (A26).

### 9.4 Forbidden on the row

```text
password, mt5Password, managerLogin, managerLoginMasked,
proxyUsername, proxyPassword, fixPassword, RawData,
connectionString, accountPassword, senderSubId,
deals[], positions[], feature component dumps, model weights
```

---

## 10. Worked examples

### 10.1 Default ranking (first useful UI seed)

```http
GET /api/v1/traders?minCompletedXauTrades=3&page=1&pageSize=50
Authorization: Bearer <token>
```

Resolved sort = A57 chain. Only official scores (`N >= 3`) if the UI sent the seed; API itself does not inject `minCompletedXauTrades`.

### 10.2 Architecture §50 filter set, fully expressed

```http
GET /api/v1/traders?brokerId=a1111111-0000-4000-8000-000000000001&group=demo%5Cyo-2step&state=SHADOW&state=WATCH&minEarlyScore=55&maxEarlyScore=82&minRiskScore=0&maxRiskScore=40&minCompletedXauTrades=3&martingale=false&scoredFrom=2026-08-01&scoredTo=2026-08-18&sort=earlyScore:desc&sort=riskScore:asc&page=1&pageSize=50
```

AND of: that broker, exact group, state ∈ {SHADOW, WATCH}, early score ∈ [55, 82], risk ∈ [0, 40], `N >= 3`, not martingale, last scored in `[2026-08-01T00:00:00.000Z, 2026-08-18T23:59:59.999Z]`.

### 10.3 Code + login search

```http
GET /api/v1/traders?broker=ACHIEVER&q=6100&enabledForAnalysis=any
```

### 10.4 Minimal 200 body (empty book)

```json
{
  "data": [],
  "page": 1,
  "pageSize": 50,
  "totalItems": 0,
  "totalPages": 0,
  "generatedAt": "2026-08-18T12:00:00.000Z",
  "query": {
    "brokerId": null,
    "broker": null,
    "groupId": null,
    "group": null,
    "state": [],
    "minEarlyScore": null,
    "maxEarlyScore": null,
    "minRiskScore": null,
    "maxRiskScore": null,
    "minCompletedXauTrades": null,
    "maxCompletedXauTrades": null,
    "martingale": null,
    "averagingDown": null,
    "lotEscalation": null,
    "scoredFrom": null,
    "scoredTo": null,
    "q": null,
    "enabledForAnalysis": "true",
    "sort": [
      { "field": "earlyScore", "dir": "desc" },
      { "field": "riskScore", "dir": "asc" },
      { "field": "completedXauTrades", "dir": "desc" },
      { "field": "brokerId", "dir": "asc" },
      { "field": "login", "dir": "asc" }
    ],
    "page": 1,
    "pageSize": 50
  }
}
```

---

## 11. JSON Schema (draft 2020-12)

Normative for the **response**. Request schema is the bind of §5 (query string), echoed as `LeaderboardQuery`.

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://prop.local/schemas/trader-leaderboard-v1.json",
  "title": "TraderLeaderboardResponse",
  "type": "object",
  "additionalProperties": false,
  "required": ["data", "page", "pageSize", "totalItems", "totalPages", "generatedAt", "query"],
  "properties": {
    "data": {
      "type": "array",
      "items": { "$ref": "#/$defs/TraderLeaderboardRow" }
    },
    "page": { "type": "integer", "minimum": 1 },
    "pageSize": { "type": "integer", "minimum": 1, "maximum": 200 },
    "totalItems": { "type": "integer", "minimum": 0 },
    "totalPages": { "type": "integer", "minimum": 0 },
    "generatedAt": { "type": "string", "format": "date-time" },
    "query": { "$ref": "#/$defs/LeaderboardQuery" }
  },
  "$defs": {
    "traderState": {
      "type": "string",
      "enum": [
        "INSUFFICIENT_DATA",
        "EARLY_SCORE",
        "WATCH",
        "SHADOW",
        "LIVE_CANDIDATE",
        "LIVE",
        "PAUSED",
        "RISK_BLOCKED",
        "DISQUALIFIED"
      ]
    },
    "sortDir": { "type": "string", "enum": ["asc", "desc"] },
    "sortField": {
      "type": "string",
      "enum": [
        "earlyScore",
        "riskScore",
        "behaviorScore",
        "mlProbability",
        "completedXauTrades",
        "netSourcePnl",
        "shadowPnl",
        "liveAllocation",
        "lastScoredAt",
        "login",
        "brokerId",
        "broker",
        "brokerDisplayName",
        "group",
        "state",
        "martingale",
        "averagingDown",
        "lotEscalation"
      ]
    },
    "enabledForAnalysis": { "type": "string", "enum": ["true", "false", "any"] },
    "score100": { "type": ["number", "null"], "minimum": 0, "maximum": 100 },
    "prob01": { "type": ["number", "null"], "minimum": 0, "maximum": 1 },
    "SortKey": {
      "type": "object",
      "additionalProperties": false,
      "required": ["field", "dir"],
      "properties": {
        "field": { "$ref": "#/$defs/sortField" },
        "dir": { "$ref": "#/$defs/sortDir" }
      }
    },
    "LeaderboardQuery": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "brokerId", "broker", "groupId", "group", "state",
        "minEarlyScore", "maxEarlyScore", "minRiskScore", "maxRiskScore",
        "minCompletedXauTrades", "maxCompletedXauTrades",
        "martingale", "averagingDown", "lotEscalation",
        "scoredFrom", "scoredTo", "q",
        "enabledForAnalysis", "sort", "page", "pageSize"
      ],
      "properties": {
        "brokerId": { "type": ["string", "null"], "format": "uuid" },
        "broker": { "type": ["string", "null"] },
        "groupId": { "type": ["string", "null"], "format": "uuid" },
        "group": { "type": ["string", "null"] },
        "state": { "type": "array", "items": { "$ref": "#/$defs/traderState" } },
        "minEarlyScore": { "$ref": "#/$defs/score100" },
        "maxEarlyScore": { "$ref": "#/$defs/score100" },
        "minRiskScore": { "$ref": "#/$defs/score100" },
        "maxRiskScore": { "$ref": "#/$defs/score100" },
        "minCompletedXauTrades": { "type": ["integer", "null"], "minimum": 0 },
        "maxCompletedXauTrades": { "type": ["integer", "null"], "minimum": 0 },
        "martingale": { "type": ["boolean", "null"] },
        "averagingDown": { "type": ["boolean", "null"] },
        "lotEscalation": { "type": ["boolean", "null"] },
        "scoredFrom": { "type": ["string", "null"], "format": "date-time" },
        "scoredTo": { "type": ["string", "null"], "format": "date-time" },
        "q": { "type": ["string", "null"] },
        "enabledForAnalysis": { "$ref": "#/$defs/enabledForAnalysis" },
        "sort": {
          "type": "array",
          "minItems": 1,
          "items": { "$ref": "#/$defs/SortKey" }
        },
        "page": { "type": "integer", "minimum": 1 },
        "pageSize": { "type": "integer", "minimum": 1, "maximum": 200 }
      }
    },
    "TraderLeaderboardRow": {
      "type": "object",
      "additionalProperties": false,
      "required": [
        "brokerId", "brokerCode", "brokerDisplayName", "login",
        "groupId", "group", "completedXauTrades", "netSourcePnl",
        "earlyScore", "behaviorScore", "mlProbability", "riskScore",
        "flags", "state", "shadowPnl", "liveAllocation", "lastScoredAt"
      ],
      "properties": {
        "brokerId": { "type": "string", "format": "uuid" },
        "brokerCode": { "type": "string", "minLength": 1 },
        "brokerDisplayName": { "type": "string", "minLength": 1 },
        "login": { "type": "integer", "minimum": 1 },
        "groupId": { "type": ["string", "null"], "format": "uuid" },
        "group": { "type": ["string", "null"] },
        "completedXauTrades": { "type": "integer", "minimum": 0 },
        "netSourcePnl": { "type": "number" },
        "earlyScore": { "$ref": "#/$defs/score100" },
        "behaviorScore": { "$ref": "#/$defs/score100" },
        "mlProbability": { "$ref": "#/$defs/prob01" },
        "riskScore": { "$ref": "#/$defs/score100" },
        "flags": {
          "type": "object",
          "additionalProperties": false,
          "required": ["martingale", "averagingDown", "lotEscalation"],
          "properties": {
            "martingale": { "type": "boolean" },
            "averagingDown": { "type": "boolean" },
            "lotEscalation": { "type": "boolean" }
          }
        },
        "state": { "$ref": "#/$defs/traderState" },
        "shadowPnl": { "type": "number" },
        "liveAllocation": { "type": "number" },
        "lastScoredAt": { "type": ["string", "null"], "format": "date-time" }
      }
    }
  }
}
```

---

## 12. SignalR subset (does not replace GET)

A63 events `trader.score` and `trader.state` may patch a visible row. Payload is a **subset** of the row, never larger:

```json
{
  "brokerId": "a1111111-0000-4000-8000-000000000001",
  "login": 6100421,
  "state": "SHADOW",
  "earlyScore": 71.00,
  "riskScore": 22.00,
  "behaviorScore": 68.00,
  "completedXauTrades": 7,
  "lastScoredAt": "2026-08-18T11:40:00.000Z"
}
```

Same scale and null rules. The client must not invent a full row from a patch. After a patch, filters still apply: if the new `state` / score no longer matches the active query, **remove** the row locally or refetch.

---

## 13. Implementation notes (non-normative)

Projection join (logical):

```text
mt5_accounts a
  JOIN brokers b              ON b.id = a.broker_id
  LEFT JOIN mt5_groups g      ON g.broker_id = a.broker_id AND g.group_name = a.group_name
  LEFT JOIN trader_scores s   ON s.broker_id = a.broker_id AND s.login = a.login
  LEFT JOIN trader_states st  ON st.broker_id = a.broker_id AND st.login = a.login
  LEFT JOIN trader_risk_flags (pivot current martingale / averaging_down / lot_escalation)
  LEFT JOIN shadow_performance / open shadow PnL rollup
  LEFT JOIN live allocation rollup (0 if none)
```

`N` and flags on `TraderScore` may be used when they are the current UPSERT (DealIngestion / ReconstructionScoringService). Official `N < 3` still forces **null** scores on the wire even if the current entity stores a provisional number (BaselineScorer today writes a capped quality for `N < 3` — the **API must null it** per L7).

Suggested indexes (when the query is implemented):

```text
trader_scores (early_quality_score DESC NULLS LAST, risk_score ASC, completed_xau_trades DESC, broker_id, login)
trader_scores (broker_id, login) UNIQUE
mt5_accounts (broker_id, login) UNIQUE
mt5_accounts (broker_id, group_name)
trader_states (state, broker_id, login)
trader_risk_flags (flag_code, broker_id, login) WHERE ended_at IS NULL
```

Redis may cache current scores (A03); invalidation is the scorer’s job. The list is not a second source of truth.

`IDashboardQueries.GetTradersAsync` must be replaced with `GetTradersAsync(LeaderboardQuery, CancellationToken) → TraderLeaderboardResponse`. Do not grow the two-string sketch.

React: persist the **request** query in the URL (A62). Map this contract, not `minScore` / `/api/traders` / `{ items, total }`.

---

## 14. Contradictions this file supersedes

| Source | Sketch | Binding here |
|---|---|---|
| A26 §2.1 | scores / probabilities `[0, 1]` unless noted 0–100 | Leaderboard `earlyScore` / `riskScore` / `behaviorScore` are **0–100** (A22 + live `BaselineScorer`) |
| A26 §6.4 example | `"earlyScore": 0.71` | `"earlyScore": 71.00` |
| A26 / A63 default sort | `earlyScore:desc` only | A57 chain + identity (§6.3) |
| A63 v1 row | scores may be `0` | **`null` when `N < 3` / never scored** |
| `TraderRowDto` | `Broker` string, no `brokerId`, no `liveAllocation`, flat bools, non-null scores | §9.2 row |
| `IDashboardQueries` | `(broker, state)` only | full `LeaderboardQuery` |
| `useTraders` | `/api/traders`, `minScore`, `{ items, total }` | `/api/v1/traders`, `minEarlyScore`, envelope §9.1 |
| Architecture §50 “score” / “risk” / “date” | unspecified bounds | `min/maxEarlyScore`, `min/maxRiskScore`, `scoredFrom`/`scoredTo` |

v2 §50 still wins on **which columns and which filter families exist**. This file only fills types, names, and sort.

---

## 15. Acceptance tests (must exist before claiming the endpoint is done)

| ID | Given | Expect |
|---|---|---|
| T01 | Fixture of 3 official + 2 `N<3` traders, no query | Official rows ordered by early desc, risk asc, `N` desc, then identity; `N<3` **after** them (`NULLS LAST`); their scores JSON-null |
| T02 | Same fixture, twice | Bit-identical `data` order |
| T03 | `minCompletedXauTrades=3` | The two `N<3` rows absent; `totalItems` matches |
| T04 | `state=SHADOW&state=WATCH` | Union only; `LIVE` excluded |
| T05 | `state=shadow` | 400 `UNKNOWN_STATE` |
| T06 | `minEarlyScore=80&maxEarlyScore=10` | 400 `RANGE_INVERTED` |
| T07 | `minEarlyScore=55` | Rows with `earlyScore == null` excluded |
| T08 | `martingale=true` | Only flagged; omitted returns both |
| T09 | `broker=ACHIEVER` vs that broker’s uuid | Same `totalItems` and first row identity |
| T10 | `broker=ACHIEVER&brokerId=<starwave uuid>` | 400 `BROKER_FILTER_CONFLICT` |
| T11 | `broker=NOPE` | 404 |
| T12 | `group=demo%5Cyo-2step` | Exact path; `demo/yo-2step` does not match |
| T13 | `q=6100` | Login substring; `q=abc` → 400 `Q_EMPTY` |
| T14 | `scoredFrom=2026-08-18&scoredTo=2026-08-18` | Instant window that UTC day; null `lastScoredAt` excluded |
| T15 | `sort=netSourcePnl:desc` | Ordered by source P&L; identity appended; `query.sort` echoes both |
| T16 | `sort=nope:desc` | 400 `UNKNOWN_SORT_FIELD` |
| T17 | `sort` six keys | 400 `SORT_LIMIT` |
| T18 | `pageSize=201` | 400 (not clamped to 200) |
| T19 | `page=99` on a 1-page book | 200, `data: []`, real `totalItems` |
| T20 | `?password=x` | 422 `SECRET_FIELD_REJECTED` |
| T21 | `?minScore=10` | 400 `UNKNOWN_QUERY_KEY` |
| T22 | No bearer | 401 |
| T23 | v1 book | every `mlProbability` is JSON `null` |
| T24 | Replay gold file (A57) | Same ordered logins as `ScoreComputationFromReplayTests` |

---

## 16. Out of scope

- Trader detail (`GET /api/v1/traders/{brokerId}/{login}`) — A26 §6.5 / A63 §5.4.
- `PATCH` state, copy-control, CSV export, cursor pagination, faceted counts.
- Ranking by net P&L, win rate, or ML.
- Inventing `mlProbability`.
- Materialized rank snapshots (`rank_as_of`) — live current scores are enough for §69.
- Product source / OpenAPI codegen from this file.

---

## 17. Done when

1. `GET /api/v1/traders` binds §5, sorts §6, pages §7, returns §9, and fails §8.
2. T01–T24 pass on a fixture; order is deterministic.
3. React `/traders` consumes this envelope (not `{ items, total }`) and persists the request query in the URL.
4. Score wire values match A22 `[0, 100]` and `BaselineScorer` (null when unofficial).
5. No secret field appears in JSON.

Until then the leaderboard remains **MISSING**. Do not claim §50 or A57 item 8 done.
