# A93 — Trader Detail DTO (Architecture §51)

| Field | Value |
|---|---|
| Agent | A93 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\A93_trader_detail_dto.md` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§51 Trader Detail Page** |
| Binding sections | §§3, 10, 14–18, 22–24, 32–35, 39, 45–46, 50–51, 55, 59, 69, 72.5 |
| Sibling specs | A21 reconstruction + first-3 cursor; A22 `baseline.v1` scores; A23 risk; A24 shadow; A26 dashboard API §6.5; A38 volume lots; A44 symbols; A45 MFE/MAE; A51 RBAC; A62 React §10.5; A63 first-useful catalog; A69 trader states |
| Product source modified | **None.** Spec only. |

This file is the **implementable wire contract** for the Trader Detail page. It freezes the `GET /api/v1/traders/{brokerId}/{login}` payload, including the **first 3 completed XAUUSD trades as a first-class highlighted block**. It does not implement the endpoint.

**Current measured host:** `D:\Prop\apps\api\Program.cs` is still the weatherforecast template. **0** of the routes below exist. `IDashboardQueries.GetTraderAsync` (`D:\Prop\src\Application\Dashboard\DashboardModels.cs`) returns a leaderboard `TraderRowDto` only — **not** this payload. React `apps/web/src/types/index.ts` `TraderDetail` / `Trade.isFirst3` is a **non-normative stub**.

---

## 0. Why this file exists

Architecture §51 requires the detail page to show:

```text
Account overview
XAU trade history
First 3 trades highlighted

Score timeline
Risk flags
Behavior features

Lot-size timeline
Holding-time distribution
SL/TP behavior
Drawdown
MFE/MAE when valid

Shadow copied positions
Shadow P&L
Live copied positions
Live P&L

Source-to-destination mapping
```

A26 §6.5 sketched a thin header plus a `firstThreeTradesHighlighted` boolean. A63 listed the route and a trade-row `isFirstThree` flag. Neither is enough to paint §51 in one honest GET, and they disagree on score scale, side vocabulary, and volume field names.

**A93 wins** for the trader-detail resource. Leaderboard (`GET /api/v1/traders`) stays A63. Reconstruction rules stay A21. Score *formulas* stay A22. State *machine* stays A69.

---

## 1. Binding invariants

| ID | Invariant |
|---|---|
| D1 | Compound identity is always `{ brokerId, login }`. Login is **never** globally unique (§10). Path is `/traders/{brokerId}/{login}`, **not** A06 `/traders/{traderId}`. |
| D2 | “First 3 trades” means the first **3 completed reconstructed XAUUSD position lifecycles** for that compound key (§15, A21). |
| D3 | Do **not** count order placement, deal fill, partial close, SL/TP modification, balance/credit, non-XAU, still-open, or **dirty** lifecycles as a first-3 slot. |
| D4 | The detail GET **always embeds** the first-3 highlight block (`data.firstThree`). A boolean alone is not compliant. |
| D5 | Trade #3 closure latches `EARLY_SCORE_ELIGIBLE`. It does **not** emit `PROVEN_PROFITABLE`. That token is not a state and must never appear on the wire (A69 S3). |
| D6 | At `completedXauTrades == 3`, `state` ∈ `{ EARLY_SCORE, WATCH, SHADOW, PAUSED, RISK_BLOCKED, DISQUALIFIED }`. **Forbidden:** `LIVE`, `LIVE_CANDIDATE`. |
| D7 | Trade #3 + high score → **SHADOW only**. Never auto-live (§23, A69 S5). |
| D8 | Scores `riskScore`, `behaviorScore`, `earlyQualityScore` are **`[0, 100]`** `decimal(5,2)` (A22). This **supersedes** A26 §2.1 / §6.5 examples that used `[0, 1]`. `mlProbability` stays `[0, 1]` or `null`. |
| D9 | `mlProbability` is **always `null` in v1**. Do not stub a fake model (A52, A63). |
| D10 | MFE/MAE numbers are published only when A45 says they are valid. Never fabricate from closed deals / VWAP / destination quotes (§17). |
| D11 | Tickets, position ids, `clOrdId`, destination ids are JSON **string**. Login is JSON **number** (MT5 login fits `int64` in this lab). |
| D12 | Side is `LONG` \| `SHORT` (A21 / domain `TradeDirection`). Not `BUY`/`SELL` on this resource. |
| D13 | Volume on the wire is **lots** (`decimal`), already converted (A38). Never raw `Volume()` integers (`*10_000`). Never “lots = lots” into cTrader `OrderQty`. |
| D14 | Allow-list DTO. Never serialize EF entities, `Broker.ManagerLogin` raw, FIX/MT5 passwords, connection strings, or `CTraderFixOptions`. |
| D15 | Same `(reconstructed XAU set, scores, flags)` → bit-identical JSON numbers (no `DateTime.UtcNow` inside mappers except `generatedAt`). |

---

## 2. Endpoint

| Item | Contract |
|---|---|
| Method | `GET` |
| Path | `/api/v1/traders/{brokerId}/{login}` |
| Roles | ReadOnly+ (A51 / A63) |
| Auth | `Authorization: Bearer <access_token>` |
| `brokerId` | UUID of `brokers.id` |
| `login` | decimal integer text of the MT5 login (`^[0-9]+$`, `1 … 2^63-1`) |
| Success | `200` + single-resource envelope |
| Missing trader | `404 NOT_FOUND` |
| Bad path | `400 VALIDATION_FAILED` (`brokerId` not UUID, `login` not a positive integer) |

Optional query (all default **off** except `firstThree`, which is **always** present):

| Query | Type | Default | Effect |
|---|---|---|---|
| `include` | csv | *(empty)* | Extra embeds: `scoreHistory`, `lotTimeline`, `holdingTimes`, `shadowPositions`, `livePositions`, `sourceDestinationMap`, `openSourcePositions`, `recentTrades` |
| `recentTradesLimit` | int | `10` | Cap for `recentTrades` when included (1–50) |
| `scoreHistoryLimit` | int | `50` | Cap for embedded score history (1–200) |

**Always returned** (no `include` needed): identity, account overview, first-3 highlight block, current scores, risk flags, behavior features (honest nulls), shadow/live **summaries**, generated metadata.

Full XAU history remains `GET /api/v1/traders/{brokerId}/{login}/trades` (A63 §5.5). The detail GET does **not** dump hundreds of trades. It **does** dump the highlighted first three.

Mutations are **not** this DTO:

| Method | Path | Roles | Notes |
|---|---|---|---|
| `PATCH` | `/api/v1/traders/{brokerId}/{login}/state` | RiskManager+ | A63 / A69. Body `{ "state", "reason" }`. Not `LIVE` / `LIVE_CANDIDATE` in v1 → `409`. |
| `POST` | `/api/v1/traders/{brokerId}/{login}/copy-control` | RiskManager+ | A26 later surface. **Not first useful.** Prefer `PATCH .../state`. |

---

## 3. Shared wire rules

Same envelope / error / sanitizer as A63 §1–2.

```json
{ "data": { /* TraderDetailDto */ } }
```

| Item | Rule |
|---|---|
| JSON | camelCase, UTF-8 |
| Time | ISO-8601 UTC with `Z` |
| Money / PnL | JSON number, server `decimal(18,2)` |
| Prices | JSON number, server `decimal(18,5)` (XAU) |
| Lots | JSON number, server `decimal(18,8)` |
| Scores | JSON number, server `decimal(5,2)`, range `[0, 100]` |
| Rates (SL/TP, win) | JSON number in `[0, 1]` |
| `featureQuality` | `EXACT` \| `APPROXIMATE` \| `UNAVAILABLE` |
| `priceSource` | `UNKNOWN` \| `ACHIEVER_MT5_TICKS` \| `STARWAVE_MT5_TICKS` \| `BAR_APPROXIMATION` \| `CTRADER_QUOTE_SESSION` |
| `traderState` | the nine A69 tokens, exact spelling |
| `side` | `LONG` \| `SHORT` |
| `scoreWindow` | `EXPANDING` \| `FIRST3` \| `PROVISIONAL` |
| `generatedAt` | mapper clock, the only “now” on the payload |

`priceSource = CTRADER_QUOTE_SESSION` is legal on **shadow/live marks**. It is **illegal** as the source of MFE/MAE on this trader (A45). If a row would mix them, omit the numbers.

---

## 4. First-3 highlight contract (normative)

This is the payload piece §51 names as **“First 3 trades highlighted.”**

### 4.1 Eligibility (same as A21 `First3State` + A22 §2.1)

A reconstructed row is **first-3 eligible** iff **all** of:

```text
completed            == true
canonicalSymbol      == "XAUUSD"
closedAt             IS NOT NULL
closedVolumeLots     >  0
dirty                == false   // A21; treat missing column as false
```

Stable order for the trader:

```text
ORDER BY closedAt ASC, openedAt ASC, reconstructedTradeId ASC
```

`sequence` is the 1-based index in that list.

| `sequence` | Highlight rank | Badge | Extra |
|---:|---:|---|---|
| 1 | 1 | `TRADE_1` | first completed XAU lifecycle |
| 2 | 2 | `TRADE_2` | |
| 3 | 3 | `TRADE_3` | closing this row latches `EARLY_SCORE_ELIGIBLE` |
| ≥ 4 | — | *(none)* | never `isFirstThree` |

`isFirstThree ≡ (sequence ∈ {1,2,3})`.

The set of highlighted keys **does not change** after trade #4. Later reconstructions that rewrite an earlier lifecycle **may** change prices/PnL of a highlighted row (reconstruction is the source of truth) but they must not steal rank 1–3 from a later close.

### 4.2 `data.firstThree`

Always present. Always **exactly three slots**. Missing trades are `trade: null` with `waiting: true` — the React page can render three highlight cards without inventing rows.

```text
FirstThreeBlock
  highlighted              bool     // true iff at least one slot is filled
  complete                 bool     // true iff all three slots are filled (N >= 3)
  count                    int      // 0..3, number of filled slots
  eligibleForEarlyScore    bool     // latched; true iff complete (A21)
  earlyScoreEligibleAt     instant? // closedAt of rank 3; null if !complete
  window                   "FIRST3"
  trades                   FirstThreeTradeDto[count]   // filled only, rank order
  slots                    FirstThreeSlotDto[3]        // always length 3
```

```text
FirstThreeSlotDto
  rank                     1 | 2 | 3
  badge                    "TRADE_1" | "TRADE_2" | "TRADE_3"
  waiting                  bool     // true iff trade is null
  trade                    FirstThreeTradeDto | null
```

`highlighted` is **not** “the UI decided to color something.” It is `count >= 1`. Clients must still honor per-row `isFirstThree` on the trades sub-resource.

`complete == false` is the honest empty/partial state. Do **not** pad with synthetic trades, do **not** pull open positions into a slot, do **not** promote a non-XAU close.

### 4.3 `FirstThreeTradeDto`

Same body as `ReconstructedTradeDto` (section 6.2) **plus** highlight fields. Highlight fields are server-computed; clients must not re-rank.

```text
# highlight (required on this object)
sequence                   int      // 1, 2, or 3
isFirstThree               true     // always true here
firstThreeRank             1|2|3    // == sequence
badge                      TRADE_1|TRADE_2|TRADE_3
closedAsEarlyScoreEligible bool     // true only on rank 3
earlyScoreTrigger          "EARLY_SCORE_ELIGIBLE" | null
                                             // non-null only on rank 3
```

`earlyScoreTrigger` is the **event name**, not a trader state. Rank 3 does **not** set `state` to a fictional `PROVEN_PROFITABLE`.

### 4.4 What the React page must do with this block

A62 §10.5 (`FirstThreeMark`, highlighted rows) maps as follows. Styling is UI-owned; the API does **not** send CSS.

| Slot | UI |
|---|---|
| `waiting == false` | Card + history-row highlight; show `badge`; show side / lots / open–close / net PnL |
| `rank == 3` && filled | Extra callout: “Early-score eligible at `closedAt`” using `earlyScoreTrigger` |
| `waiting == true` | Empty card: “Waiting for trade {rank}” |
| `complete == false` | Banner: `INSUFFICIENT_DATA` until N=3 (unless A69 override already fired) |
| History table | Every row with `isFirstThree` uses the same badge; rows 4+ unhighlighted |

Do not compute first-3 in the browser from a deal list.

### 4.5 Worked slot matrices

**N = 0** (account exists, no completed XAU):

```text
highlighted=false  complete=false  count=0  eligibleForEarlyScore=false
slots = [ (1, waiting), (2, waiting), (3, waiting) ]
trades = []
```

**N = 2** (one partial close and five `GOLD` aliases already reconstructed as XAU **do** count if they completed; a still-open ticket does **not**):

```text
highlighted=true   complete=false  count=2  eligibleForEarlyScore=false
slots = [ trade#1, trade#2, (3, waiting) ]
```

**N = 3** (third lifecycle goes flat):

```text
highlighted=true   complete=true   count=3  eligibleForEarlyScore=true
earlyScoreEligibleAt = closedAt of rank 3
slots[3].trade.closedAsEarlyScoreEligible = true
slots[3].trade.earlyScoreTrigger = "EARLY_SCORE_ELIGIBLE"
state ∉ { LIVE, LIVE_CANDIDATE }
```

**N = 7**: firstThree still shows **only** ranks 1–3. Trade 4–7 appear only on `.../trades` (and optional `recentTrades`). Their `isFirstThree` is `false`.

---

## 5. `TraderDetailDto` (root `data`)

```text
TraderDetailDto
  generatedAt              instant
  identity                 TraderIdentityDto
  accountOverview          AccountOverviewDto | null
  firstThree               FirstThreeBlock          // §4 — required
  completedXauTrades       int                      // N, eligible definition
  openXauSourcePositions   int
  scores                   TraderScoresDto
  riskFlags                RiskFlagsDto
  behaviorFeatures         BehaviorFeaturesDto
  shadow                   BookSummaryDto
  live                     LiveBookSummaryDto
  sourceDestinationPreview SourceDestinationLinkDto[]  // 0..3, first-three only
  embeds                   TraderDetailEmbedsDto    // present; lists empty unless include=
```

`firstThreeTradesHighlighted` (A26 name) is **not** a root field. Use `firstThree.highlighted`. Implementers may emit the A26 alias **only if** it equals `firstThree.highlighted`; new clients must read the block.

### 5.1 `TraderIdentityDto`

| Field | Type | Source | Notes |
|---|---|---|---|
| `brokerId` | uuid | `brokers.id` / `mt5_accounts.broker_id` | |
| `brokerCode` | string | `brokers.code` | `ACHIEVER` \| `STARWAVEFX` \| future |
| `brokerDisplayName` | string | `brokers.display_name` | |
| `login` | int64 | `mt5_accounts.login` | |
| `group` | string \| null | `mt5_accounts.group_name` | broker-local path |
| `groupId` | uuid \| null | `mt5_groups.id` | null if group not discovered |
| `planMapping` | string \| null | `mt5_groups.plan_mapping` | overlay only; do not invent |
| `state` | `traderState` | `trader_states` else `trader_scores.current_state` else `INSUFFICIENT_DATA` | one of nine |
| `stateReason` | string \| null | A69 rule id (`R0`…`R9`) | omit if never resolved |
| `enabledForAnalysis` | bool | group flag; default true if unknown | |

**Existence rule:** `200` if **any** of `mt5_accounts`, `trader_scores`, `trader_states`, or a reconstructed XAU row exists for the compound key. Else `404`.

If an account exists and scoring has never run: `state = INSUFFICIENT_DATA`, `scores.*` numeric fields `null`, `firstThree` still computed live from `reconstructed_trades`.

### 5.2 `AccountOverviewDto`

| Field | Type | Source | Notes |
|---|---|---|---|
| `balance` | decimal(18,2) | `mt5_accounts.balance` | |
| `equity` | decimal(18,2) | `mt5_accounts.equity` | |
| `margin` | decimal(18,2) | `mt5_accounts.margin` | |
| `marginFree` | decimal(18,2) | `mt5_accounts.margin_free` | |
| `profit` | decimal(18,2) | `mt5_accounts.profit` | floating source P&L |
| `leverage` | int | `mt5_accounts.leverage` | |
| `currency` | string \| null | `mt5_groups.currency` | **null if unknown** — do not default `"USD"` unless the group says so |
| `currencyDigits` | int \| null | `mt5_groups.currency_digits` | |
| `asOf` | instant \| null | `mt5_accounts.last_synced_at` | |

If the account row is missing but a score exists (should be rare): `accountOverview = null`. UI shows `—`.

**Forbidden:** manager login, passwords, comment blobs that might hold credentials.

### 5.3 `TraderScoresDto`

| Field | Type | Range | Notes |
|---|---|---|---|
| `version` | string | | `baseline.v1` (A22). Never a silent unversioned formula. |
| `window` | `scoreWindow` \| null | | Operational current row is `EXPANDING` once N≥3; `PROVISIONAL` must **not** be used for ranking |
| `riskScore` | number \| null | `[0, 100]` | higher = riskier |
| `behaviorScore` | number \| null | `[0, 100]` | higher = healthier process |
| `earlyQualityScore` | number \| null | `[0, 100]` | ranking key; **not** raw net P&L (A22 I9) |
| `earlyScore` | number \| null | `[0, 100]` | **MUST equal** `earlyQualityScore`. Leaderboard alias (A63). |
| `mlProbability` | number \| null | `[0, 1]` | **v1 = always null** |
| `lastScoredAt` | instant \| null | | |
| `completedXauTrades` | int | | echo of root N |
| `earlyScoreEligible` | bool | | `firstThree.eligibleForEarlyScore` |
| `firstThreeFrozen` | FirstThreeFrozenScoresDto \| null | | snapshot written **once** when rank 3 closed (`window=FIRST3`) |

`FirstThreeFrozenScoresDto`: `{ "riskScore", "behaviorScore", "earlyQualityScore", "asOf" }` — the frozen FIRST3 window (A22 §2.3). `null` until N≥3 and the scorer has persisted it. Do not recompute a fake freeze in the API if the history row is missing; leave `null`.

All three live scores `null` when N<3 and only a provisional diagnostic exists. Do **not** put `PROVISIONAL` numbers into `earlyQualityScore` (they must not rank).

### 5.4 `RiskFlagsDto`

Closed set for v1 (A22 / `TraderScore` / `FeatureSnapshot`):

| Field | Type | Meaning |
|---|---|---|
| `martingale` | bool | size-up after a loss (A22 / `BaselineScorer`) |
| `averagingDown` | bool | any lifecycle `wasAveragedDown` or flag table |
| `lotEscalation` | bool | size-up independent of last PnL |
| `abnormalSizing` | bool | A22 `FLAG_ABNORMAL_SIZING`; false if scorer has not emitted it |
| `severeRisk` | bool | A22 `severe_risk` / A69 R-rules |

No free-text flag soup on this object. Extra flags go on `GET .../risk-flags` later.

### 5.5 `BehaviorFeaturesDto`

Computed on the **EXPANDING** eligible set (or empty). Honest nulls beat invented percentiles.

| Field | Type | Notes |
|---|---|---|
| `netSourcePnl` | decimal(18,2) | Σ net realized on eligible XAU |
| `grossProfit` | decimal(18,2) | |
| `grossLoss` | decimal(18,2) | absolute |
| `profitFactor` | decimal \| null | null if no losses and no wins; A22 cap applies when present |
| `winRate` | number \| null | `[0,1]`; null if N=0 |
| `holdingTimeSecondsP50` | number \| null | from hold seconds; null if N=0 |
| `holdingTimeSecondsP90` | number \| null | null if N<2 |
| `averageHoldSeconds` | number \| null | |
| `slUseRate` | number \| null | fraction with `initialSl` present |
| `tpUseRate` | number \| null | fraction with `initialTp` present |
| `drawdown` | decimal(18,2) | max peak-to-trough of cumulative **source** net (A22 `max_dd`) |
| `scaledInRate` | number \| null | fraction `wasScaledIn` |
| `partialCloseRate` | number \| null | fraction `wasPartialClose` |
| `tradeFrequencyPerDay` | number \| null | |
| `mfe` | number \| null | **mean** MFE in price units; see §5.6 |
| `mae` | number \| null | **mean** MAE in price units |
| `mfeMae` | MfeMaeMetaDto | always present |

Percentiles: nearest-rank on the sorted hold list. Do not interpolate a p90 from a single trade — leave `holdingTimeSecondsP90 = null` when N<2.

### 5.6 `MfeMaeMetaDto` (A45)

| Field | Type | Rule |
|---|---|---|
| `valid` | bool | `featureQuality ∈ {EXACT, APPROXIMATE}` **and** both `mfe` and `mae` non-null **and** `priceSource` in the catalog **and** `priceSource ≠ CTRADER_QUOTE_SESSION` |
| `featureQuality` | enum | `UNAVAILABLE` if not valid |
| `priceSource` | enum | `UNKNOWN` if not valid |
| `usedInScores` | bool | A22: true only when `featureQuality == EXACT` |

If `valid == false`: `mfe` and `mae` on the parent **must** be `null`. UI copy: “MFE/MAE unavailable” — never `0`.

A26 said “show only if EXACT.” A45 allows APPROXIMATE **with the quality badge**. A93 follows A45: show the number + badge when `valid`; scores still ignore anything that is not `EXACT` (`usedInScores=false`).

### 5.7 `BookSummaryDto` / `LiveBookSummaryDto`

Shadow (`BookSummaryDto`):

| Field | Type | Notes |
|---|---|---|
| `selected` | bool | state ∈ `{ SHADOW, LIVE_CANDIDATE, LIVE }` (A69 S9) |
| `openPositions` | int | |
| `closedPositions` | int | |
| `unrealizedPnl` | decimal(18,2) | dest-quote marked (A24) |
| `realizedPnl` | decimal(18,2) | |
| `pnl` | decimal(18,2) | unrealized + realized |
| `currency` | string \| null | destination account currency if known |
| `quote` | `{ instrumentId, bid, ask, quoteAgeMs, healthy }` | dest QUOTE; nulls if unmapped |

Live (`LiveBookSummaryDto`) extends that with:

| Field | Type | Notes |
|---|---|---|
| `realCopyExecutionEnabled` | bool | **false** in v1 |
| `allocation` | decimal(18,8) | dest qty allocated to this trader; 0 if off |
| `openPositions` / `pnl` | | **0 / empty** while execution is off. Do not copy shadow numbers into live. |

### 5.8 `sourceDestinationPreview`

Zero to three `SourceDestinationLinkDto` rows, **only** for highlighted trades, rank order. Full map is the sub-resource.

```text
SourceDestinationLinkDto
  reconstructedTradeId     uuid-as-string
  firstThreeRank           1|2|3 | null
  copyIntentId             uuid-as-string | null
  executionIntentId        uuid-as-string | null
  clOrdId                  string | null
  destinationOrderIds      string[]
  destinationPositionIds   string[]
  mode                     "NONE" | "SHADOW" | "LIVE"
  copyStatus               string | null    // copy_intents.status
```

v1: `mode` is `NONE` or `SHADOW`. `LIVE` only after real send exists. `executionIntentId` / dest ids empty while send is off.

### 5.9 `TraderDetailEmbedsDto`

Always present so clients can null-check one path.

```text
embeds
  scoreHistory             ScoreHistoryItemDto[]     // [] unless include=scoreHistory
  lotTimeline              LotTimelinePointDto[]     // [] unless include=lotTimeline
  holdingTimes             HoldingTimeHistogramDto | null
  shadowPositions          ShadowPositionDto[]
  livePositions            LivePositionDto[]
  sourceDestinationMap     SourceDestinationLinkDto[]
  openSourcePositions      OpenSourcePositionDto[]
  recentTrades             ReconstructedTradeDto[]   // not a substitute for firstThree
```

---

## 6. Nested item DTOs

### 6.1 `ReconstructedTradeDto` (history + explorer)

Used by `GET .../trades`, `GET /api/v1/trades`, `embeds.recentTrades`. First-3 objects are this **plus** §4.3.

| Field | Type | Source / rule |
|---|---|---|
| `reconstructedTradeId` | string (uuid) | `reconstructed_trades.id` |
| `brokerId` | uuid | |
| `login` | int64 | |
| `positionTicket` | string | `position_id` as decimal text |
| `lifecycleSeq` | int | A21; default `1` if column not yet migrated |
| `sequence` | int | 1-based among **eligible** XAU (same order as first-3). Open/dirty/non-XAU rows: `null` |
| `isFirstThree` | bool | `sequence ∈ {1,2,3}` |
| `firstThreeRank` | 1\|2\|3 \| null | null if not first-three |
| `badge` | string \| null | `TRADE_1`… only when highlighted |
| `canonicalSymbol` | string | `XAUUSD` for this page’s primary list |
| `sourceSymbol` | string | opening-deal broker string |
| `side` | `LONG` \| `SHORT` | opening direction; never flips mid-lifecycle |
| `openTime` | instant | `opened_at` |
| `closeTime` | instant \| null | null if `completed=false` |
| `openPrice` | decimal | `entry_vwap` |
| `closePrice` | decimal \| null | `exit_vwap` |
| `initialVolumeLots` | decimal | |
| `maxVolumeLots` | decimal | display “lots” = this unless noted |
| `closedVolumeLots` | decimal | |
| `volumeLots` | decimal | **alias of `maxVolumeLots`** for A26/`Trade.lots` paint |
| `grossSourcePnl` | decimal | `gross_realized_pnl` |
| `commission` | decimal | |
| `swap` | decimal | |
| `fees` | decimal | |
| `netSourcePnl` | decimal | `net_realized_pnl` |
| `hadSl` | bool | `initial_sl IS NOT NULL` |
| `hadTp` | bool | `initial_tp IS NOT NULL` |
| `initialSl` / `initialTp` / `finalSl` / `finalTp` | decimal \| null | |
| `wasScaledIn` | bool | |
| `wasPartialClose` | bool | |
| `wasAveragedDown` | bool | |
| `isCompleted` | bool | |
| `dirty` | bool | A21; excluded from first-3 if true |
| `dealCount` | int | |
| `orderCount` | int | |
| `dealTickets` | string[] | optional; omit on list views if large |

**Do not** expose raw `mt5_deals` as this list. Reconstruction is mandatory (§14).

Open (incomplete) XAU lifecycles may appear on `.../trades?completed=false` with `isFirstThree=false`, `sequence=null`. They never occupy a first-3 slot.

### 6.2 `ScoreHistoryItemDto`

| Field | Type | Notes |
|---|---|---|
| `at` | instant | `recorded_at` |
| `trigger` | string | `TRADE_3_COMPLETE` \| `EARLY_SCORE_ELIGIBLE` \| `RESCORED` \| `STATE_CHANGED` \| `PROVISIONAL` |
| `window` | `scoreWindow` | |
| `state` | `traderState` | |
| `riskScore` | number | `[0,100]` |
| `behaviorScore` | number | |
| `earlyQualityScore` | number | |
| `earlyScore` | number | = `earlyQualityScore` |
| `completedXauTrades` | int | |
| `mlProbability` | null | v1 |

`TRADE_3_COMPLETE` and `EARLY_SCORE_ELIGIBLE` may be the same instant (two history rows or one row with trigger `EARLY_SCORE_ELIGIBLE`). Prefer **one** row: `trigger=EARLY_SCORE_ELIGIBLE`, `completedXauTrades=3`, `window=FIRST3` plus the expanding row written the same transaction (`window=EXPANDING`).

### 6.3 `LotTimelinePointDto`

One point per eligible completed trade (and, if `include` asked, each still-open XAU max volume).

```text
at            instant     // openTime
volumeLots    decimal     // maxVolumeLots
side          LONG|SHORT
sequence      int | null
isFirstThree  bool
reconstructedTradeId  string
```

### 6.4 `HoldingTimeHistogramDto`

```text
bucketsSeconds   number[]   // default [60, 300, 900, 1800, 3600, 14400, +inf]
counts           number[]   // same length; last bucket is “≥ previous”
sampleSize       int
```

Counts use eligible completed holds only.

### 6.5 `ShadowPositionDto` / `LivePositionDto`

Shadow (A24 / A63; dest-quote marked):

```text
shadowPositionId     string
reconstructedTradeId string | null
canonicalSymbol      "XAUUSD"
side                 LONG|SHORT
quantity             decimal   // dest lots, already normalized
entryPrice           decimal   // dest quote
markPrice            decimal | null
unrealizedPnl        decimal
realizedPnl          decimal
quoteAgeMs           number | null
openedAt             instant
open                 bool
```

Live: same idea plus `destinationPositionId` (string), `copyIntentId`, `executionIntentId`, `clOrdId`, `state`. v1 list is empty while `realCopyExecutionEnabled=false`.

### 6.6 `OpenSourcePositionDto`

Source MT5 book (`mt5_positions_current`), XAU only, **not** a first-3 trade.

```text
positionTicket   string
sourceSymbol     string
canonicalSymbol  "XAUUSD" | null   // null if unmapped — omit from XAU views
side             LONG|SHORT
volumeLots       decimal           // converted
priceOpen        decimal
priceCurrent     decimal
profit           decimal
openedAt         instant
```

---

## 7. Canonical JSON example (N = 3, SHADOW)

Illustrative numbers. Scores are 0–100. MFE/MAE honestly unavailable.

```json
{
  "data": {
    "generatedAt": "2026-08-18T12:00:00.000Z",
    "identity": {
      "brokerId": "a1111111-0000-4000-8000-000000000001",
      "brokerCode": "ACHIEVER",
      "brokerDisplayName": "Achiever",
      "login": 6100421,
      "group": "demo\\yo-2step",
      "groupId": "b2222222-0000-4000-8000-000000000010",
      "planMapping": "2STEP_DEMO",
      "state": "SHADOW",
      "stateReason": "R7",
      "enabledForAnalysis": true
    },
    "accountOverview": {
      "balance": 10120.00,
      "equity": 10080.40,
      "margin": 40.00,
      "marginFree": 10040.40,
      "profit": -39.60,
      "leverage": 100,
      "currency": "USD",
      "currencyDigits": 2,
      "asOf": "2026-08-18T11:59:00.000Z"
    },
    "completedXauTrades": 3,
    "openXauSourcePositions": 0,
    "firstThree": {
      "highlighted": true,
      "complete": true,
      "count": 3,
      "eligibleForEarlyScore": true,
      "earlyScoreEligibleAt": "2026-07-03T14:20:00.000Z",
      "window": "FIRST3",
      "trades": [
        {
          "reconstructedTradeId": "d3333333-0000-4000-8000-000000000101",
          "brokerId": "a1111111-0000-4000-8000-000000000001",
          "login": 6100421,
          "positionTicket": "88001001",
          "lifecycleSeq": 1,
          "sequence": 1,
          "isFirstThree": true,
          "firstThreeRank": 1,
          "badge": "TRADE_1",
          "closedAsEarlyScoreEligible": false,
          "earlyScoreTrigger": null,
          "canonicalSymbol": "XAUUSD",
          "sourceSymbol": "XAUUSDm",
          "side": "LONG",
          "openTime": "2026-07-01T08:15:00.000Z",
          "closeTime": "2026-07-01T10:02:00.000Z",
          "openPrice": 2320.40,
          "closePrice": 2325.10,
          "initialVolumeLots": 0.10,
          "maxVolumeLots": 0.10,
          "closedVolumeLots": 0.10,
          "volumeLots": 0.10,
          "grossSourcePnl": 47.00,
          "commission": 0.00,
          "swap": 0.00,
          "fees": 0.00,
          "netSourcePnl": 47.00,
          "hadSl": true,
          "hadTp": false,
          "initialSl": 2310.00,
          "initialTp": null,
          "finalSl": 2310.00,
          "finalTp": null,
          "wasScaledIn": false,
          "wasPartialClose": false,
          "wasAveragedDown": false,
          "isCompleted": true,
          "dirty": false,
          "dealCount": 2,
          "orderCount": 2
        },
        {
          "reconstructedTradeId": "d3333333-0000-4000-8000-000000000102",
          "sequence": 2,
          "isFirstThree": true,
          "firstThreeRank": 2,
          "badge": "TRADE_2",
          "closedAsEarlyScoreEligible": false,
          "earlyScoreTrigger": null,
          "side": "SHORT",
          "volumeLots": 0.10,
          "netSourcePnl": -18.50,
          "isCompleted": true
        },
        {
          "reconstructedTradeId": "d3333333-0000-4000-8000-000000000103",
          "sequence": 3,
          "isFirstThree": true,
          "firstThreeRank": 3,
          "badge": "TRADE_3",
          "closedAsEarlyScoreEligible": true,
          "earlyScoreTrigger": "EARLY_SCORE_ELIGIBLE",
          "side": "LONG",
          "volumeLots": 0.12,
          "netSourcePnl": 22.10,
          "isCompleted": true
        }
      ],
      "slots": [
        { "rank": 1, "badge": "TRADE_1", "waiting": false, "trade": { "sequence": 1 } },
        { "rank": 2, "badge": "TRADE_2", "waiting": false, "trade": { "sequence": 2 } },
        { "rank": 3, "badge": "TRADE_3", "waiting": false, "trade": { "sequence": 3 } }
      ]
    },
    "scores": {
      "version": "baseline.v1",
      "window": "EXPANDING",
      "riskScore": 22.00,
      "behaviorScore": 68.00,
      "earlyQualityScore": 71.00,
      "earlyScore": 71.00,
      "mlProbability": null,
      "lastScoredAt": "2026-07-03T14:20:01.000Z",
      "completedXauTrades": 3,
      "earlyScoreEligible": true,
      "firstThreeFrozen": {
        "riskScore": 22.00,
        "behaviorScore": 68.00,
        "earlyQualityScore": 71.00,
        "asOf": "2026-07-03T14:20:00.000Z"
      }
    },
    "riskFlags": {
      "martingale": false,
      "averagingDown": true,
      "lotEscalation": false,
      "abnormalSizing": false,
      "severeRisk": false
    },
    "behaviorFeatures": {
      "netSourcePnl": 50.60,
      "grossProfit": 69.10,
      "grossLoss": 18.50,
      "profitFactor": 3.7351,
      "winRate": 0.6667,
      "holdingTimeSecondsP50": 6420,
      "holdingTimeSecondsP90": 7200,
      "averageHoldSeconds": 5880,
      "slUseRate": 0.6667,
      "tpUseRate": 0.3333,
      "drawdown": 18.50,
      "scaledInRate": 0.00,
      "partialCloseRate": 0.00,
      "tradeFrequencyPerDay": 1.5,
      "mfe": null,
      "mae": null,
      "mfeMae": {
        "valid": false,
        "featureQuality": "UNAVAILABLE",
        "priceSource": "UNKNOWN",
        "usedInScores": false
      }
    },
    "shadow": {
      "selected": true,
      "openPositions": 0,
      "closedPositions": 0,
      "unrealizedPnl": 0.00,
      "realizedPnl": 0.00,
      "pnl": 0.00,
      "currency": "USD",
      "quote": {
        "instrumentId": null,
        "bid": null,
        "ask": null,
        "quoteAgeMs": null,
        "healthy": false
      }
    },
    "live": {
      "selected": false,
      "realCopyExecutionEnabled": false,
      "allocation": 0,
      "openPositions": 0,
      "closedPositions": 0,
      "unrealizedPnl": 0.00,
      "realizedPnl": 0.00,
      "pnl": 0.00,
      "currency": null,
      "quote": null
    },
    "sourceDestinationPreview": [
      {
        "reconstructedTradeId": "d3333333-0000-4000-8000-000000000101",
        "firstThreeRank": 1,
        "copyIntentId": null,
        "executionIntentId": null,
        "clOrdId": null,
        "destinationOrderIds": [],
        "destinationPositionIds": [],
        "mode": "NONE",
        "copyStatus": null
      }
    ],
    "embeds": {
      "scoreHistory": [],
      "lotTimeline": [],
      "holdingTimes": null,
      "shadowPositions": [],
      "livePositions": [],
      "sourceDestinationMap": [],
      "openSourcePositions": [],
      "recentTrades": []
    }
  }
}
```

The rank-2/3 objects in the example are abbreviated; production JSON must include the full `FirstThreeTradeDto` field set on every filled slot (same keys as rank 1). `slots[i].trade` is the **same object reference** as `trades[i]` (identical bytes), not a stub.

### 7.1 Partial first-3 (N = 1) — highlight still required

```json
{
  "firstThree": {
    "highlighted": true,
    "complete": false,
    "count": 1,
    "eligibleForEarlyScore": false,
    "earlyScoreEligibleAt": null,
    "window": "FIRST3",
    "trades": [ { "sequence": 1, "isFirstThree": true, "badge": "TRADE_1" } ],
    "slots": [
      { "rank": 1, "badge": "TRADE_1", "waiting": false, "trade": { "sequence": 1 } },
      { "rank": 2, "badge": "TRADE_2", "waiting": true, "trade": null },
      { "rank": 3, "badge": "TRADE_3", "waiting": true, "trade": null }
    ]
  },
  "identity": { "state": "INSUFFICIENT_DATA" },
  "scores": {
    "version": "baseline.v1",
    "window": null,
    "riskScore": null,
    "behaviorScore": null,
    "earlyQualityScore": null,
    "earlyScore": null,
    "mlProbability": null,
    "earlyScoreEligible": false,
    "firstThreeFrozen": null
  }
}
```

---

## 8. Domain → DTO map

| DTO | Domain / table today | Gap |
|---|---|---|
| identity | `Broker` + `Mt5Account` + `TraderScore.CurrentState` | `trader_states` table not in EF yet (A20 #19). Fallback: `TraderScore.CurrentState`. |
| accountOverview | `Mt5Account` + `Mt5Group.Currency` | Account has no currency column — join group. |
| firstThree.trades | `ReconstructedTrade` / `ReconstructedTradeResult` filtered by A21 eligibility, ordered as §4.1 | Entity has **no** `dirty`, **no** `lifecycleSeq`. Treat dirty=false, seq=1 until A21 columns exist. |
| sequence / isFirstThree | **computed in the query**, not stored | `TraderScore.CompletedXauTrades` is a cache; **recompute from rows** for the highlight block (cache may lag). If cache ≠ counted N, prefer counted N and log `trader_detail_n_mismatch`. |
| scores | `TraderScore` + last `TraderScoreHistory` | History has no `trigger` / `window` columns yet — emit `trigger=RESCORED` when unknown. |
| FIRST3 freeze | A22 window row | **Missing.** `firstThreeFrozen=null` until persisted. |
| riskFlags | `TraderScore` bools | `abnormalSizing` / `severeRisk` not on entity — `false` until A22 flags table exists. |
| behaviorFeatures | `BaselineScorer.FeatureSnapshot` or `trader_feature_snapshots` | Snapshot table **missing**. Compute from reconstructed rows in the query **or** return nulls for p50/p90 if you will not compute honestly. |
| mfe/mae | A45 feature row | `FeatureSnapshot.MaeMfeQuality` is already `Unavailable`. Keep numbers null. |
| shadow | `ShadowOrder` + A24 positions | Entity is an order, not a position. Summary zeros until `shadow_positions` exists. |
| live | `ExecutionIntent` / dest positions | Empty / zero while flag off. |
| map | `CopyIntent.SourceTradeId` + `ExecutionIntent` | Preview `mode=NONE` if no intent. |

`TradeReconstructor.CompletedXauUsdTrades` (`D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`) already filters `Completed && IsXauUsd` and orders by `ClosedAt`, then `OpenedAt`. The DTO layer must **then** apply `Id` as the third sort key and assign `sequence`. `IsEarlyScoreEligible` ⇔ `count >= 3` is the latch for `firstThree.eligibleForEarlyScore`.

Do **not** use `IDashboardQueries.GetTraderAsync → TraderRowDto` as this payload.

---

## 9. Sub-resources (same item shapes)

Unchanged paths from A63; item DTOs **this file** wins.

| Method | Path | Item |
|---|---|---|
| `GET` | `/api/v1/traders/{brokerId}/{login}/trades` | `ReconstructedTradeDto` paged. Query: `completed` (default true), `from`, `to`, `side`, `page`, `pageSize`. Every row carries `isFirstThree`. |
| `GET` | `/api/v1/traders/{brokerId}/{login}/scores` | `{ current: TraderScoresDto, history: ScoreHistoryItemDto[] }` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/features` | `BehaviorFeaturesDto` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/risk-flags` | `RiskFlagsDto` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/lot-timeline` | `LotTimelinePointDto[]` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/holding-times` | `HoldingTimeHistogramDto` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/shadow` | `BookSummaryDto` + `positions[]` |
| `GET` | `/api/v1/traders/{brokerId}/{login}/live-positions` | `LivePositionDto[]` (empty in v1) |
| `GET` | `/api/v1/traders/{brokerId}/{login}/source-destination-map` | `SourceDestinationLinkDto[]` |

404 if the compound key does not exist (same rule as the detail GET).

---

## 10. SignalR

Hub `/hubs/ops` (A63 §6). Detail page subscribes to `traders`.

| Event | Payload | Client |
|---|---|---|
| `trader.score` | `{ brokerId, login, state, earlyScore, earlyQualityScore, riskScore, behaviorScore, completedXauTrades, lastScoredAt, firstThreeComplete }` | patch `scores` + `identity.state`; **do not** invent first-three trades from this event |
| `trader.state` | `{ brokerId, login, state, stateReason }` | patch identity |
| `trader.firstThree` | `{ brokerId, login, firstThree }` **full `FirstThreeBlock`** | only when a slot fills or a highlighted trade is reconstructed. This is how trade #3 lights the highlight without a full refetch |

`trader.firstThree` is optional for first useful (REST refetch on `trader.score` when `completedXauTrades` changes is enough). If implemented, the block must match the GET byte-for-byte for those fields.

Never put secrets on the hub. Never send raw deals.

---

## 11. Errors, RBAC, secrets

| HTTP | `error.code` | When |
|---|---|---|
| 400 | `VALIDATION_FAILED` | bad UUID / login / `include` token |
| 401 | `UNAUTHENTICATED` | no/expired token |
| 403 | `FORBIDDEN` | (GET is ReadOnly+; mutations only) |
| 404 | `NOT_FOUND` | unknown `(brokerId, login)` |
| 503 | `DEPENDENCY_UNAVAILABLE` | DB down |

`details` on 404: `{ "brokerId", "login" }` only.

**Denylist** (drop / 422 on write): any `password`, `passwd`, `secret`, `pwd`, `rawData`, `connectionString`, `privateKey`, `proxyUser`, FIX tag 96, raw `managerLogin`.

Do not put destination account password, FIX password, or MT5 manager password on this DTO. Masked manager login is **not** needed on trader detail (it is a broker-page field).

---

## 12. TypeScript contract (normative for `/apps/web`)

Replace the stub in `apps/web/src/types/index.ts`. Tickets are `string`. Scores are `0–100`. `isFirst3` is **renamed**.

```ts
export type TraderState =
  | 'INSUFFICIENT_DATA'
  | 'EARLY_SCORE'
  | 'WATCH'
  | 'SHADOW'
  | 'LIVE_CANDIDATE'
  | 'LIVE'
  | 'PAUSED'
  | 'RISK_BLOCKED'
  | 'DISQUALIFIED';

export type TradeSide = 'LONG' | 'SHORT';
export type FeatureQuality = 'EXACT' | 'APPROXIMATE' | 'UNAVAILABLE';
export type FirstThreeBadge = 'TRADE_1' | 'TRADE_2' | 'TRADE_3';

export interface ReconstructedTradeDto {
  reconstructedTradeId: string;
  brokerId: string;
  login: number;
  positionTicket: string;
  lifecycleSeq: number;
  sequence: number | null;
  isFirstThree: boolean;
  firstThreeRank: 1 | 2 | 3 | null;
  badge: FirstThreeBadge | null;
  canonicalSymbol: string;
  sourceSymbol: string;
  side: TradeSide;
  openTime: string;
  closeTime: string | null;
  openPrice: number;
  closePrice: number | null;
  initialVolumeLots: number;
  maxVolumeLots: number;
  closedVolumeLots: number;
  volumeLots: number;
  netSourcePnl: number;
  hadSl: boolean;
  hadTp: boolean;
  wasScaledIn: boolean;
  wasPartialClose: boolean;
  wasAveragedDown: boolean;
  isCompleted: boolean;
  dirty: boolean;
  dealCount: number;
  orderCount: number;
}

export interface FirstThreeTradeDto extends ReconstructedTradeDto {
  sequence: 1 | 2 | 3;
  isFirstThree: true;
  firstThreeRank: 1 | 2 | 3;
  badge: FirstThreeBadge;
  closedAsEarlyScoreEligible: boolean;
  earlyScoreTrigger: 'EARLY_SCORE_ELIGIBLE' | null;
}

export interface FirstThreeSlotDto {
  rank: 1 | 2 | 3;
  badge: FirstThreeBadge;
  waiting: boolean;
  trade: FirstThreeTradeDto | null;
}

export interface FirstThreeBlock {
  highlighted: boolean;
  complete: boolean;
  count: 0 | 1 | 2 | 3;
  eligibleForEarlyScore: boolean;
  earlyScoreEligibleAt: string | null;
  window: 'FIRST3';
  trades: FirstThreeTradeDto[];
  slots: [FirstThreeSlotDto, FirstThreeSlotDto, FirstThreeSlotDto];
}

export interface TraderDetailDto {
  generatedAt: string;
  identity: {
    brokerId: string;
    brokerCode: string;
    brokerDisplayName: string;
    login: number;
    group: string | null;
    groupId: string | null;
    planMapping: string | null;
    state: TraderState;
    stateReason: string | null;
    enabledForAnalysis: boolean;
  };
  accountOverview: {
    balance: number;
    equity: number;
    margin: number;
    marginFree: number;
    profit: number;
    leverage: number;
    currency: string | null;
    currencyDigits: number | null;
    asOf: string | null;
  } | null;
  firstThree: FirstThreeBlock;
  completedXauTrades: number;
  openXauSourcePositions: number;
  scores: {
    version: string;
    window: 'EXPANDING' | 'FIRST3' | 'PROVISIONAL' | null;
    riskScore: number | null;
    behaviorScore: number | null;
    earlyQualityScore: number | null;
    earlyScore: number | null;
    mlProbability: number | null;
    lastScoredAt: string | null;
    completedXauTrades: number;
    earlyScoreEligible: boolean;
    firstThreeFrozen: {
      riskScore: number;
      behaviorScore: number;
      earlyQualityScore: number;
      asOf: string;
    } | null;
  };
  riskFlags: {
    martingale: boolean;
    averagingDown: boolean;
    lotEscalation: boolean;
    abnormalSizing: boolean;
    severeRisk: boolean;
  };
  behaviorFeatures: {
    netSourcePnl: number;
    mfe: number | null;
    mae: number | null;
    mfeMae: {
      valid: boolean;
      featureQuality: FeatureQuality;
      priceSource: string;
      usedInScores: boolean;
    };
    [k: string]: unknown;
  };
  shadow: Record<string, unknown>;
  live: Record<string, unknown>;
  sourceDestinationPreview: unknown[];
  embeds: Record<string, unknown>;
}
```

Fetcher (A62 key): `['trader', brokerId, login]` → `GET /api/v1/traders/${brokerId}/${login}`.

The current stub `useTraderDetail` calls unversioned `/api/traders/...` and types `ticket: number` / `isFirst3`. That path is **not** this contract.

---

## 13. Suggested C# records (spec only — do not add in this change-set)

When Increment 6 (A30) implements the API, names should be:

```text
apps/api/Contracts/TraderDetailDto.cs
  TraderDetailDto
  TraderIdentityDto
  AccountOverviewDto
  FirstThreeBlockDto
  FirstThreeSlotDto
  FirstThreeTradeDto
  ReconstructedTradeDto
  TraderScoresDto
  RiskFlagsDto
  BehaviorFeaturesDto
  MfeMaeMetaDto
  BookSummaryDto
  LiveBookSummaryDto
  SourceDestinationLinkDto
  TraderDetailEmbedsDto
```

JSON enum converter: **string**, uppercase tokens as in this file (`SHADOW`, `LONG`, `EXACT`, `TRADE_1`). Map domain `TradeDirection.Long → "LONG"`, `FeatureQuality.Unavailable → "UNAVAILABLE"`, `PriceSource.AchieverMt5Ticks → "ACHIEVER_MT5_TICKS"`.

`IDashboardQueries.GetTraderAsync` must return `TraderDetailDto?`, not `TraderRowDto?`. Keep `GetTradersAsync` on `TraderRowDto` (leaderboard).

---

## 14. Conflicts resolved

| Topic | Older text | A93 |
|---|---|---|
| Path | A06 `/traders/{traderId}` | `/traders/{brokerId}/{login}` (A26/A63) |
| Score scale | A26 examples `0.71` | **71.00** on `[0,100]` (A22 / `BaselineScorer`) |
| Score names | A26 `earlyScore` only | `earlyQualityScore` + alias `earlyScore` (equal) |
| Side | A26 `BUY`/`SELL` | `LONG`/`SHORT` |
| Volume field | A26 `volume`; stub `lots` | `volumeLots` (+ explicit initial/max/closed) |
| Tickets | stub `number` | **string** |
| Highlight | A26 boolean `firstThreeTradesHighlighted` | **embedded 3-slot block** + per-row flags |
| MFE show rule | A26 EXACT only | A45 valid = EXACT **or** APPROXIMATE + badge; scores use EXACT only |
| First useful mutations | A26 `POST .../copy-control` | A63 `PATCH .../state` |
| Stub `TraderDetail.trades` | full array on detail | firstThree only; full list is sub-resource |
| `PROVEN_PROFITABLE` | mentioned as a non-event | **absent from JSON forever** |

---

## 15. Honest gap (measured 2026-08-18)

| Layer | State |
|---|---|
| HTTP route | **MISSING** (weatherforecast only) |
| `TraderDetailDto` in Application | **MISSING** (`TraderRowDto` only) |
| Persist of first-3 flags | **MISSING** (computed concept; A57 item 6 PARTIAL helpers) |
| `TradeReconstructor.CompletedXauUsdTrades` / `IsEarlyScoreEligible` | **EXISTS** (in-memory) |
| `TraderScore.CompletedXauTrades` writer | **MISSING** |
| React page | imported in `App.tsx`, **file absent**; types stubbed |
| Secrets on this DTO | N/A (no serializer yet) |

First useful (§69.4–8) is **not** done until a fixture with 2 completed XAU + 1 partial + non-XAU fills returns `firstThree.count == 2`, `slots[3].waiting == true`, and the third completed close flips `eligibleForEarlyScore` exactly once **and** those three rows appear highlighted on this payload.

---

## 16. Acceptance tests (when implemented)

| ID | Given | Expect |
|---|---|---|
| T1 | Unknown `(brokerId, login)` | 404, no `data` |
| T2 | Account, N=0 | 200, `firstThree.count=0`, three waiting slots, `state=INSUFFICIENT_DATA`, scores null |
| T3 | 2 completed XAU + 1 partial + 1 open | `count=2`, slot 3 waiting, partial/open not in `trades` |
| T4 | Non-XAU completed lifecycles only | `count=0` |
| T5 | Dirty completed XAU (A21) | excluded from slots |
| T6 | Third eligible close | `complete=true`, rank-3 `earlyScoreTrigger=EARLY_SCORE_ELIGIBLE`, **no** `PROVEN_PROFITABLE`, state ≠ `LIVE`/`LIVE_CANDIDATE` |
| T7 | N=7 | still exactly 3 highlighted; `.../trades` has 7 rows, three with `isFirstThree` |
| T8 | `earlyScore === earlyQualityScore` | always |
| T9 | `mlProbability` | JSON `null` |
| T10 | No tick tape | `mfe`/`mae` null, `featureQuality=UNAVAILABLE` |
| T11 | Alias `XAUUSDm` / `GOLD` | counts as XAU if mapping says so (A21/A44) |
| T12 | Same book twice | bit-identical scores and first-three ids |
| T13 | Achiever 1001 vs StarwaveFX 1001 | two different resources |
| T14 | Live flag off | `live.pnl=0`, `livePositions=[]`, no dest ids invented |
| T15 | Response sanitizer | no denylisted keys |
| T16 | High early score at N=3 | `state=SHADOW` or `WATCH`/`EARLY_SCORE` per A69 — never `LIVE` |

---

## 17. Non-goals

- Do not implement the endpoint in this change-set.
- Do not hand-write MQ5. Do not mutate product source beside this file.
- Do not embed raw MT5 deals, tick tapes, or FIX Logon.
- Do not put ML weights or training rows on the payload.
- Do not treat first-3 source dollar PnL as the ranking key (§3, A22 I9). The highlight is **identity of the window**, not a trophy for “who made the most in three trades.”
- Do not auto-promote to live from this page.

---

## 18. Implementer checklist

1. Add allow-list records in `apps/api/Contracts/` matching §13.
2. Query by `(broker_id, login)`; 404 if D5.1 existence fails.
3. Load reconstructed XAU; apply §4.1 filter + sort; build `FirstThreeBlock` with **three slots**.
4. Map `TraderScore` / features honestly; null what you cannot compute.
5. Emit `GET` + sub-resources; retarget React to `/api/v1` and this TS shape.
6. Tests T1–T16 before calling §51 done.
