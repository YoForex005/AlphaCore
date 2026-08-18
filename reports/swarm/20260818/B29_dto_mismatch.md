# B29 — TypeScript dashboard types vs `DashboardModels.cs`

| Field | Value |
|---|---|
| Agent | B29 (DTO / JSON contract mismatch) |
| Date | 2026-08-18 |
| Left | `D:\Prop\apps\web\src\types\index.ts` (135 lines, 13 exported interfaces) |
| Right | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` (97 lines, 6 records + `IDashboardQueries`) |
| Product source edited | **No.** This report is the only write. |
| Wire assumption | ASP.NET Core minimal APIs, `System.Text.Json`, default **camelCase** names. `Program.cs` does **not** register `JsonStringEnumConverter`, so C# enums on the wire are **integers** unless the query already `ToString()`s them. |

---

## 0. Verdict

The two files are **not the same contract**. They describe the same six dashboard surfaces (overview, brokers, groups, traders, FIX sessions, risk) but almost every JSON key disagrees. The TypeScript file is a **stale / invented** client model. The C# records are what `/api/overview`, `/api/brokers`, `/api/groups`, `/api/traders`, `/api/traders/{broker}/{login}`, `/api/fix/sessions`, and `/api/risk` actually return.

| Metric | Count |
|---|---|
| TS interfaces | **13** |
| C# DTO records | **6** (`OverviewDto`, `BrokerStatusDto`, `GroupRowDto`, `TraderRowDto`, `FixSessionDto`, `RiskDashboardDto`) |
| Paired surfaces | **6** (all six C# records have a loosely corresponding TS interface) |
| TS interfaces with **no** C# DTO | **7** |
| C# DTO records with **no** TS interface | **0** (name pairing only; field parity fails) |
| Shared JSON field names that match on a paired type | **12** (listed in §6) |
| Field-level mismatches (name, shape, type, or one-sided) | **64** (listed in §3–§5) |
| Imports of `apps/web/src/types/index.ts` | **0** — file is unused |
| React pages | Consume **C# camelCase** via `any`, not the TS interfaces |

**Do not type hooks from `types/index.ts` as-is.** That would make TypeScript lie about the live payload. Align TS to `DashboardModels.cs` (or emit both from one source). Pages already assume the C# names.

---

## 1. Method

1. Read both files in full.
2. Pair types by dashboard surface, not by identifier spelling (`Broker` ↔ `BrokerStatusDto`).
3. Project C# record parameters to JSON names (`TotalAccounts` → `totalAccounts`).
4. Classify each field as **MATCH**, **NAME**, **SHAPE**, **TYPE/NULL**, **TS-only**, or **C#-only**.
5. Confirm live handlers in `D:\Prop\apps\api\Program.cs` and materialization in `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`.
6. Confirm pages under `D:\Prop\apps\web\src\pages` use C# names (`data.connectedBrokers`, `t.completedXauTrades`, `s.qualifier`, `data.killSwitch`) and never import `types/index.ts`.
7. Did **not** edit product source.

---

## 2. Type-level pairing

| # | TS export | C# type | Pair? | Notes |
|---|---|---|---|---|
| 1 | `Overview` | `OverviewDto` | yes, broken fields | See §3.1 |
| 2 | `Broker` | `BrokerStatusDto` | yes, broken fields | See §3.2 |
| 3 | `Group` | `GroupRowDto` | yes, broken fields | See §3.3 |
| 4 | `Trader` | `TraderRowDto` | yes, broken fields | See §3.4 |
| 5 | `TraderDetail` | **none** | **TS-only** | `GetTraderAsync` returns `TraderRowDto?`, same as the list row |
| 6 | `Trade` | **none** | **TS-only** | Not a dashboard DTO. `GET /api/trades` returns raw `ReconstructedTrade` entities |
| 7 | `Position` | **none** | **TS-only** | No dashboard position DTO |
| 8 | `FixSession` | `FixSessionDto` | yes, broken fields | See §3.5 |
| 9 | `RiskStatus` | `RiskDashboardDto` | yes, broken fields | See §3.6 |
| 10 | `ReconciliationStatus` | **none** | **TS-only** | Anonymous object in `Program.cs` `/api/reconciliation/status` |
| 11 | `HealthStatus` | **none** | **TS-only** | Anonymous object in `Program.cs` `/api/health` |
| 12 | `ComponentHealth` | **none** | **TS-only** | Nested in the health anonymous type |
| 13 | `Settings` | **none** | **TS-only** | Anonymous object in `Program.cs` `/api/settings` |
| — | *(no TS type)* | `IDashboardQueries` | n/a | Query port, not a wire DTO |

C# method → HTTP → TS hook (hooks are untyped):

| `IDashboardQueries` | Route | Hook | TS type the hook *should* use if aligned |
|---|---|---|---|
| `GetOverviewAsync` | `GET /api/overview` | `useOverview` | `OverviewDto` shape, **not** `Overview` |
| `GetBrokersAsync` | `GET /api/brokers` | `useBrokers` | `BrokerStatusDto[]` |
| `GetGroupsAsync` | `GET /api/groups` | `useGroups` | `GroupRowDto[]` |
| `GetTradersAsync` | `GET /api/traders` | `useTraders` | `TraderRowDto[]` |
| `GetTraderAsync` | `GET /api/traders/{broker}/{login}` | `useTraderDetail` | `TraderRowDto \| null` — **not** `TraderDetail` |
| `GetFixSessionsAsync` | `GET /api/fix/sessions` | `useFixSessions` | `FixSessionDto[]` |
| `GetRiskAsync` | `GET /api/risk` and `GET /api/risk/status` | `useRiskStatus` (hits `/api/risk`) | `RiskDashboardDto` |

---

## 3. Field mismatches on paired types

JSON names on the C# side. `decimal` → JSON number is accepted (not counted as a mismatch). `DateTimeOffset` → ISO-8601 string is accepted **when the JSON key matches**.

### 3.1 `Overview` vs `OverviewDto`

C# (`DashboardModels.cs` L5–22): `TotalAccounts`, `ConnectedBrokers`, `XauTraders`, `TradersWithThreeTrades`, `Watch`, `Shadow`, `LiveCandidates`, `Live`, `RiskBlocked`, `ShadowPnl`, `DestinationRealPnl`, `XauGross`, `XauNet`, `Mt5Healthy`, `QuoteHealthy`, `TradeHealthy`, `RealCopyEnabled`.

TS (`types/index.ts` L1–8): `totalAccounts`, `totalBrokers`, `tradersByState`, `shadowPnl`, `realPnl`, `fixHealthy`.

| TS field | TS type | C# JSON field(s) | C# type | Kind |
|---|---|---|---|---|
| `totalAccounts` | `number` | `totalAccounts` | `int` | **MATCH** |
| `totalBrokers` | `number` | `connectedBrokers` | `int` | **NAME** — same idea, different key |
| `tradersByState` | `Record<string, number>` | `watch`, `shadow`, `liveCandidates`, `live`, `riskBlocked` | five `int`s | **SHAPE** — map vs five scalars. Also drops `INSUFFICIENT_DATA` / `EARLY_SCORE` / `PAUSED` / `DISQUALIFIED` |
| `shadowPnl` | `number` | `shadowPnl` | `decimal` | **MATCH** (wire number) |
| `realPnl` | `number` | `destinationRealPnl` | `decimal` | **NAME** |
| `fixHealthy` | `boolean` | `mt5Healthy`, `quoteHealthy`, `tradeHealthy` | three `bool`s | **SHAPE** — one bool vs three health flags |
| — | — | `xauTraders` | `int` | **C#-only** |
| — | — | `tradersWithThreeTrades` | `int` | **C#-only** |
| — | — | `xauGross` | `decimal` | **C#-only** |
| — | — | `xauNet` | `decimal` | **C#-only** |
| — | — | `realCopyEnabled` | `bool` | **C#-only** |

Live overview JSON the browser actually receives (camelCase of `OverviewDto`):

```json
{
  "totalAccounts": 0,
  "connectedBrokers": 0,
  "xauTraders": 0,
  "tradersWithThreeTrades": 0,
  "watch": 0,
  "shadow": 0,
  "liveCandidates": 0,
  "live": 0,
  "riskBlocked": 0,
  "shadowPnl": 0,
  "destinationRealPnl": 0,
  "xauGross": 0,
  "xauNet": 0,
  "mt5Healthy": false,
  "quoteHealthy": false,
  "tradeHealthy": false,
  "realCopyEnabled": false
}
```

`OverviewPage.tsx` reads those C# keys. It never reads `totalBrokers`, `tradersByState`, `realPnl`, or `fixHealthy`.

Mismatch count this pair: **9** (2 NAME + 2 SHAPE + 5 C#-only). Matches: 2.

### 3.2 `Broker` vs `BrokerStatusDto`

C# (L24–32): `Code`, `DisplayName`, `Server`, `ManagerLoginMasked`, `Connected`, `GroupCount`, `AccountCount`, `LastEventAt`.

TS (L10–18): `id`, `name`, `status`, `server`, `groups`, `accounts`, `lastEvent`.

| TS field | TS type | C# JSON field | C# type | Kind |
|---|---|---|---|---|
| `id` | `string` | `code` | `string` | **NAME** — C# is broker code (`ACHIEVER`), not a Guid |
| `name` | `string` | `displayName` | `string` | **NAME** |
| `status` | `string` | `connected` | `bool` | **NAME + TYPE** — string vs boolean |
| `server` | `string` | `server` | `string` | **MATCH** |
| `groups` | `number` | `groupCount` | `int` | **NAME** |
| `accounts` | `number` | `accountCount` | `int` | **NAME** |
| `lastEvent` | `string` | `lastEventAt` | `DateTimeOffset?` | **NAME + NULL** — required string vs nullable timestamp |
| — | — | `managerLoginMasked` | `long` | **C#-only** |

`BrokersPage.tsx` uses `b.code`, `b.displayName`, `b.managerLoginMasked`, `b.groupCount`, `b.accountCount`, `b.connected`.

Mismatch count this pair: **7**. Matches: 1 (`server`).

### 3.3 `Group` vs `GroupRowDto`

C# (L34–41): `Broker`, `Group`, `Accounts`, `EnabledForAnalysis`, `PlanMapping`, `LastDiscovered`, `LastSynced`.

TS (L20–28): `brokerId`, `brokerName`, `name`, `accounts`, `enabled`, `planMapping`, `lastSynced`.

| TS field | TS type | C# JSON field | C# type | Kind |
|---|---|---|---|---|
| `brokerId` | `string` | `broker` | `string` | **NAME** — C# is broker **code**, not a Guid |
| `brokerName` | `string` | — | — | **TS-only** |
| `name` | `string` | `group` | `string` | **NAME** |
| `accounts` | `number` | `accounts` | `int` | **MATCH** |
| `enabled` | `boolean` | `enabledForAnalysis` | `bool` | **NAME** |
| `planMapping` | `string` | `planMapping` | `string?` | **NULL** — required vs nullable |
| `lastSynced` | `string` | `lastSynced` | `DateTimeOffset?` | **NULL** — required string vs nullable timestamp |
| — | — | `lastDiscovered` | `DateTimeOffset?` | **C#-only** |

`GroupsPage.tsx` uses `g.broker`, `g.group`, `g.enabledForAnalysis`, `g.planMapping`.

Mismatch count this pair: **6**. Matches: 1 (`accounts`). Name `planMapping` / `lastSynced` match; nullability does not.

### 3.4 `Trader` vs `TraderRowDto`

C# (L43–56): `Broker`, `Login`, `Group`, `CompletedXauTrades`, `NetSourcePnl`, `EarlyScore`, `MlProbability`, `RiskScore`, `Martingale`, `AveragingDown`, `LotEscalation`, `State`, `ShadowPnl`, `LastScored`.

TS (L30–40): `brokerId`, `login`, `group`, `completedTrades`, `pnl`, `score`, `riskFlags`, `state`, `martingale`.

| TS field | TS type | C# JSON field | C# type | Kind |
|---|---|---|---|---|
| `brokerId` | `string` | `broker` | `string` | **NAME** — code, not Guid |
| `login` | `number` | `login` | `long` | **MATCH** (MT5 logins fit JS number) |
| `group` | `string` | `group` | `string?` | **NULL** — required vs nullable |
| `completedTrades` | `number` | `completedXauTrades` | `int` | **NAME** — C# is XAU-only count |
| `pnl` | `number` | `netSourcePnl` | `decimal` | **NAME** |
| `score` | `number` | `earlyScore` | `decimal` | **NAME** — C# also has separate `riskScore` |
| `riskFlags` | `string[]` | `averagingDown`, `lotEscalation` (+ `martingale`) | three `bool`s | **SHAPE** — string tags vs booleans |
| `state` | `string` | `state` | `TraderState` enum | **TYPE** — name matches; wire is **integer 0–8**, not `"WATCH"` |
| `martingale` | `boolean` | `martingale` | `bool` | **MATCH** |
| — | — | `mlProbability` | `decimal?` | **C#-only** (query currently always `null`) |
| — | — | `riskScore` | `decimal` | **C#-only** |
| — | — | `shadowPnl` | `decimal` | **C#-only** (query currently `0`) |
| — | — | `lastScored` | `DateTimeOffset` | **C#-only** |

`TraderState` (`Domain\Enums\TraderState.cs`): `INSUFFICIENT_DATA=0`, `EARLY_SCORE=1`, `WATCH=2`, `SHADOW=3`, `LIVE_CANDIDATE=4`, `LIVE=5`, `PAUSED=6`, `RISK_BLOCKED=7`, `DISQUALIFIED=8`. Without `JsonStringEnumConverter`, `TradersPage` `{t.state}` prints `2`, not `WATCH`.

`GetTraderAsync` returns this same row. There is no `trades`, `scoreHistory`, `lotHistory`, `shadowPositions`, or `livePositions` on the C# dashboard contract.

`TraderDetailPage.tsx` already reads C# keys (`data.broker`, `completedXauTrades`, `earlyScore`, `riskScore`, `netSourcePnl`, `averagingDown`, `mlProbability`).

Mismatch count this pair: **11**. Matches: 2 (`login`, `martingale`).

### 3.5 `FixSession` vs `FixSessionDto`

C# (L59–75): `Qualifier`, `Host`, `Port`, `Connected`, `LoggedOn`, `Status`, `LastInbound`, `LastOutbound`, `InboundSeq`, `OutboundSeq`, `ReconnectCount`, `LastError`, `InstrumentId`, `Bid`, `Ask`, `QuoteAgeSeconds`, `ExecutionEnabled`.

TS (L73–92): `type`, `host`, `port`, `connected`, `loggedOn`, `inSequence`, `outSequence`, `lastHeartbeat`, `errors`, plus optional `instrumentId`, `bid`, `ask`, `spread`, `quoteAge`, `executionEnabled`, `openOrders`, `openPositions`, `lastExecutionReport`.

| TS field | TS type | C# JSON field | C# type | Kind |
|---|---|---|---|---|
| `type` | `'QUOTE' \| 'TRADE'` | `qualifier` | `string` | **NAME** — query already emits `"QUOTE"` / `"TRADE"` via `ToString().ToUpperInvariant()` |
| `host` | `string` | `host` | `string` | **MATCH** |
| `port` | `number` | `port` | `int` | **MATCH** |
| `connected` | `boolean` | `connected` | `bool` | **MATCH** |
| `loggedOn` | `boolean` | `loggedOn` | `bool` | **MATCH** |
| `inSequence` | `number` | `inboundSeq` | `int` | **NAME** |
| `outSequence` | `number` | `outboundSeq` | `int` | **NAME** |
| `lastHeartbeat` | `string` | `lastInbound`, `lastOutbound` | two `DateTimeOffset?` | **SHAPE** |
| `errors` | `number` | `reconnectCount` + `lastError` | `int` + `string?` | **SHAPE** |
| `instrumentId?` | `string` | `instrumentId` | `string?` | **MATCH** (optional / nullable) |
| `bid?` | `number` | `bid` | `decimal?` | **MATCH** |
| `ask?` | `number` | `ask` | `decimal?` | **MATCH** |
| `spread?` | `number` | — | — | **TS-only** |
| `quoteAge?` | `number` | `quoteAgeSeconds` | `double?` | **NAME** |
| `executionEnabled?` | `boolean` | `executionEnabled` | `bool` | **NULL** — TS optional, C# required (`false` today) |
| `openOrders?` | `number` | — | — | **TS-only** |
| `openPositions?` | `number` | — | — | **TS-only** |
| `lastExecutionReport?` | `string` | — | — | **TS-only** |
| — | — | `status` | `string` | **C#-only** — `FixSessionStatus.ToString()` |

`FixSessionsPage.tsx` uses `s.qualifier`, `s.status`, `s.inboundSeq`, `s.outboundSeq`, `s.reconnectCount`, `s.quoteAgeSeconds`.

Mismatch count this pair: **11**. Matches: 7.

### 3.6 `RiskStatus` vs `RiskDashboardDto`

C# (L77–86): `DailyPnl`, `Drawdown`, `XauLong`, `XauShort`, `XauNet`, `KillSwitch`, `RealCopyEnabled`, `RecentRejectReasons`.

TS (L94–107): `equity`, `balance`, `margin`, `dailyPnl`, `drawdown`, `xauExposureLong`, `xauExposureShort`, `xauExposureNet`, `riskByTrader`, `rejectedIntents`, `stopNewExecution`, `emergencyFlatten`.

| TS field | TS type | C# JSON field | C# type | Kind |
|---|---|---|---|---|
| `equity` | `number` | — | — | **TS-only** |
| `balance` | `number` | — | — | **TS-only** |
| `margin` | `number` | — | — | **TS-only** |
| `dailyPnl` | `number` | `dailyPnl` | `decimal` | **MATCH** (query currently `0`) |
| `drawdown` | `number` | `drawdown` | `decimal` | **MATCH** (query currently `0`) |
| `xauExposureLong` | `number` | `xauLong` | `decimal` | **NAME** |
| `xauExposureShort` | `number` | `xauShort` | `decimal` | **NAME** |
| `xauExposureNet` | `number` | `xauNet` | `decimal` | **NAME** |
| `riskByTrader` | `{ login; risk }[]` | — | — | **TS-only** |
| `rejectedIntents` | `{ login; reason; time }[]` | `recentRejectReasons` | `IReadOnlyList<string>` | **SHAPE** — objects vs reason strings |
| `stopNewExecution` | `boolean` | `killSwitch` | `string` | **SHAPE** — two bools vs one mode string (`None` / `StopNewExecution` / `EmergencyFlatten`) |
| `emergencyFlatten` | `boolean` | *(same `killSwitch`)* | `string` | **SHAPE** (counted with the kill-switch pair once more as a distinct TS field) |
| — | — | `realCopyEnabled` | `bool` | **C#-only** |

`KillSwitch` is already a **string** on the DTO (`(ks?.Mode ?? KillSwitchMode.None).ToString()`), so this one does **not** suffer numeric-enum serialization.

`RiskPage.tsx` uses `data.killSwitch`, `data.realCopyEnabled`, `data.dailyPnl`, `data.xauNet`, `data.recentRejectReasons`.

Mismatch count this pair: **10**. Matches: 2.

---

## 4. TypeScript-only types (no `DashboardModels` counterpart)

These seven interfaces have **zero** fields in `DashboardModels.cs`. They are not dashboard DTOs.

### 4.1 `TraderDetail` (L42–47)

Extends `Trader` and adds:

| Field | Type | Backend fact |
|---|---|---|
| `trades` | `Trade[]` | Not returned by `GetTraderAsync` |
| `scoreHistory` | `{ date; score }[]` | `TraderScoreHistory` exists in the DB, not on this DTO |
| `lotHistory` | `{ date; lots }[]` | No DTO / no endpoint |
| `shadowPositions` | `Position[]` | No DTO |
| `livePositions` | `Position[]` | No DTO |

### 4.2 `Trade` (L50–61)

| Field | Type |
|---|---|
| `ticket` | `number` |
| `symbol` | `string` |
| `direction` | `string` |
| `lots` | `number` |
| `openPrice` | `number` |
| `closePrice` | `number` |
| `pnl` | `number` |
| `openTime` | `string` |
| `closeTime` | `string` |
| `isFirst3` | `boolean` |

`GET /api/trades` is **not** this shape. It serializes `ReconstructedTrade` (`Id`, `BrokerId`, `Login`, `PositionId`, `CanonicalSymbol`, `SourceSymbol`, `Direction` as **int**, `OpenedAt`, `ClosedAt`, `EntryVwap`, `ExitVwap`, `InitialVolumeLots`, `MaxVolumeLots`, `ClosedVolumeLots`, `GrossRealizedPnl`, `Commission`, `Swap`, `Fees`, `NetRealizedPnl`, `DealCount`, `OrderCount`, SL/TP, flags, `Completed`). `TradeExplorerPage.tsx` already reads those entity names (`t.canonicalSymbol`, `t.maxVolumeLots`, `t.netRealizedPnl`). Out of scope for `DashboardModels.cs` but the TS `Trade` type is still wrong for the only trades API.

### 4.3 `Position` (L63–71)

`ticket`, `symbol`, `direction`, `lots`, `openPrice`, `currentPrice`, `pnl`. No dashboard DTO. Domain has `Mt5Position` / `Mt5PositionDto` in `Application\Contracts\Mt5Contracts.cs` (different names: `PositionTicket`, `VolumeNative`, `PriceOpen`, `PriceCurrent`, `Profit`).

### 4.4 `ReconciliationStatus` (L109–114)

`lastReconciliation`, `unknownPositions`, `mismatches`, `orphanFills`. Implemented as an anonymous type in `Program.cs` L31–37, not in `DashboardModels.cs`. Field names happen to match that anonymous object. **Not a DashboardModels mismatch** except “type lives in the wrong file / is unofficial.”

### 4.5 `HealthStatus` + `ComponentHealth` (L116–129)

TS `HealthStatus`: `mt5Connections[]`, `fixSessions[]`, `database`, `redis`, `outboxBacklog`.

Anonymous `/api/health` in `Program.cs` L22–29 is close (`mt5Connections`, `fixSessions`, `database`, `redis`, `outboxBacklog`). Nested objects omit `details` on some rows. **Not declared in `DashboardModels.cs`.** Overview health is the three bools on `OverviewDto`, not this type.

### 4.6 `Settings` (L131–135)

`riskLimits`, `featureFlags`, `brokerConfigs`. Matches the anonymous `/api/settings` object in `Program.cs` L38–43 well enough. **Not in `DashboardModels.cs`.**

---

## 5. Inventory of every mismatch

Kind legend: **N** name, **S** shape, **T** type/nullability, **TS** TypeScript-only field, **CS** C#-only field, **MISSING** whole TS type with no C# DTO.

| # | Surface | TS | C# JSON / type | Kind |
|---|---|---|---|---|
| 1 | Overview | `totalBrokers` | `connectedBrokers` | N |
| 2 | Overview | `tradersByState` | `watch` + `shadow` + `liveCandidates` + `live` + `riskBlocked` | S |
| 3 | Overview | `realPnl` | `destinationRealPnl` | N |
| 4 | Overview | `fixHealthy` | `mt5Healthy` + `quoteHealthy` + `tradeHealthy` | S |
| 5 | Overview | — | `xauTraders` | CS |
| 6 | Overview | — | `tradersWithThreeTrades` | CS |
| 7 | Overview | — | `xauGross` | CS |
| 8 | Overview | — | `xauNet` | CS |
| 9 | Overview | — | `realCopyEnabled` | CS |
| 10 | Broker | `id` | `code` | N |
| 11 | Broker | `name` | `displayName` | N |
| 12 | Broker | `status: string` | `connected: bool` | N+T |
| 13 | Broker | `groups` | `groupCount` | N |
| 14 | Broker | `accounts` | `accountCount` | N |
| 15 | Broker | `lastEvent` | `lastEventAt` | N+T |
| 16 | Broker | — | `managerLoginMasked` | CS |
| 17 | Group | `brokerId` | `broker` | N |
| 18 | Group | `brokerName` | — | TS |
| 19 | Group | `name` | `group` | N |
| 20 | Group | `enabled` | `enabledForAnalysis` | N |
| 21 | Group | `planMapping: string` | `planMapping: string?` | T |
| 22 | Group | `lastSynced: string` | `lastSynced: DateTimeOffset?` | T |
| 23 | Group | — | `lastDiscovered` | CS |
| 24 | Trader | `brokerId` | `broker` | N |
| 25 | Trader | `group: string` | `group: string?` | T |
| 26 | Trader | `completedTrades` | `completedXauTrades` | N |
| 27 | Trader | `pnl` | `netSourcePnl` | N |
| 28 | Trader | `score` | `earlyScore` | N |
| 29 | Trader | `riskFlags: string[]` | `averagingDown` + `lotEscalation` | S |
| 30 | Trader | `state: string` | `state: TraderState` (JSON **int**) | T |
| 31 | Trader | — | `mlProbability` | CS |
| 32 | Trader | — | `riskScore` | CS |
| 33 | Trader | — | `shadowPnl` | CS |
| 34 | Trader | — | `lastScored` | CS |
| 35 | TraderDetail | whole interface | no DTO; detail = `TraderRowDto` | MISSING |
| 36 | Trade | whole interface | not in DashboardModels | MISSING |
| 37 | Position | whole interface | not in DashboardModels | MISSING |
| 38 | FIX | `type` | `qualifier` | N |
| 39 | FIX | `inSequence` | `inboundSeq` | N |
| 40 | FIX | `outSequence` | `outboundSeq` | N |
| 41 | FIX | `lastHeartbeat` | `lastInbound` + `lastOutbound` | S |
| 42 | FIX | `errors` | `reconnectCount` + `lastError` | S |
| 43 | FIX | `spread` | — | TS |
| 44 | FIX | `quoteAge` | `quoteAgeSeconds` | N |
| 45 | FIX | `executionEnabled?` | `executionEnabled` required | T |
| 46 | FIX | `openOrders` | — | TS |
| 47 | FIX | `openPositions` | — | TS |
| 48 | FIX | `lastExecutionReport` | — | TS |
| 49 | FIX | — | `status` | CS |
| 50 | Risk | `equity` | — | TS |
| 51 | Risk | `balance` | — | TS |
| 52 | Risk | `margin` | — | TS |
| 53 | Risk | `xauExposureLong` | `xauLong` | N |
| 54 | Risk | `xauExposureShort` | `xauShort` | N |
| 55 | Risk | `xauExposureNet` | `xauNet` | N |
| 56 | Risk | `riskByTrader` | — | TS |
| 57 | Risk | `rejectedIntents[]` | `recentRejectReasons: string[]` | S |
| 58 | Risk | `stopNewExecution` | `killSwitch: string` | S |
| 59 | Risk | `emergencyFlatten` | *(same `killSwitch`)* | S |
| 60 | Risk | — | `realCopyEnabled` | CS |
| 61 | Recon | `ReconciliationStatus` | not in DashboardModels | MISSING |
| 62 | Health | `HealthStatus` | not in DashboardModels | MISSING |
| 63 | Health | `ComponentHealth` | not in DashboardModels | MISSING |
| 64 | Settings | `Settings` | not in DashboardModels | MISSING |

**64 mismatches.** Shared matching fields are only the 12 in §6.

---

## 6. Fields that do match (complete list)

Accepting `int`/`long`/`decimal`/`double` ↔ TS `number`, and `DateTimeOffset` ↔ TS `string` **only when the JSON name and nullability agree**.

| Surface | JSON name | C# | TS |
|---|---|---|---|
| Overview | `totalAccounts` | `int` | `number` |
| Overview | `shadowPnl` | `decimal` | `number` |
| Broker | `server` | `string` | `string` |
| Group | `accounts` | `int` | `number` |
| Trader | `login` | `long` | `number` |
| Trader | `martingale` | `bool` | `boolean` |
| FIX | `host` | `string` | `string` |
| FIX | `port` | `int` | `number` |
| FIX | `connected` | `bool` | `boolean` |
| FIX | `loggedOn` | `bool` | `boolean` |
| FIX | `instrumentId` | `string?` | `string?` |
| FIX | `bid` / `ask` | `decimal?` | `number?` |
| Risk | `dailyPnl` | `decimal` | `number` |
| Risk | `drawdown` | `decimal` | `number` |

`planMapping` and `lastSynced` share names on Group but **not** nullability — they are in the mismatch list, not here. `state` shares a name but not a JSON type.

---

## 7. Serialization traps (affect TS even after a rename)

1. **`TraderState` is a numeric enum on the wire.** `TraderRowDto.State` is the enum itself. No `JsonStringEnumConverter` in `apps/api/Program.cs`. JSON is `0`…`8`. TS `state: string` is wrong. Pages print the number.
2. **`FixSessionDto.Qualifier` and `Status` and `RiskDashboardDto.KillSwitch` are already `string`s** in the record (query `ToString()`s them). Those three do **not** have the enum-number problem.
3. **`FixSessionQualifier` C# names are `Quote` / `Trade`.** The query uppercases them to `QUOTE` / `TRADE`, which matches the TS union **values** but not the TS **property name** (`type` vs `qualifier`).
4. **No response envelope.** Both sides assume a bare object / array. Architecture notes (`A26` / `A91`) wanted `{ data: … }`. Neither file has that envelope today — not a TS-vs-C# mismatch.
5. **`GET /api/traders/{broker}/{login}` is a list-row, not a detail aggregate.** Typing the hook as `TraderDetail` would be a compile-time fiction.

---

## 8. Runtime vs types (why the UI still works)

| Layer | Aligned to |
|---|---|
| `DashboardModels.cs` | Source of truth for the six GETs |
| `EfDashboardQueries.cs` | Builds those six records |
| `apps/api/Program.cs` | Maps the six GETs (+ extra anonymous health/recon/settings/trades) |
| `apps/web/src/api/hooks.ts` | Untyped `r.data` |
| Pages | C# camelCase via `any` |
| `apps/web/src/types/index.ts` | **Unused.** Zero imports |

The types file cannot break the running dashboard today. It **will** break the dashboard if someone wires `useQuery<Overview>(…)` without first rewriting the interfaces.

One page already drifts past both files: `ScoringPage.tsx` reads `t.behaviorScore`, which is **not** on `TraderRowDto` and **not** on TS `Trader`. That is a page/DTO gap, recorded here only so it is not blamed on `types/index.ts`.

---

## 9. Suggested TS names if someone later mirrors C# (do not implement in this wave)

Product source was not edited. For a later typed-client pass, the TS interfaces should look like this (JSON names only):

```ts
export interface OverviewDto {
  totalAccounts: number;
  connectedBrokers: number;
  xauTraders: number;
  tradersWithThreeTrades: number;
  watch: number;
  shadow: number;
  liveCandidates: number;
  live: number;
  riskBlocked: number;
  shadowPnl: number;
  destinationRealPnl: number;
  xauGross: number;
  xauNet: number;
  mt5Healthy: boolean;
  quoteHealthy: boolean;
  tradeHealthy: boolean;
  realCopyEnabled: boolean;
}

export interface BrokerStatusDto {
  code: string;
  displayName: string;
  server: string;
  managerLoginMasked: number;
  connected: boolean;
  groupCount: number;
  accountCount: number;
  lastEventAt: string | null;
}

export interface GroupRowDto {
  broker: string;
  group: string;
  accounts: number;
  enabledForAnalysis: boolean;
  planMapping: string | null;
  lastDiscovered: string | null;
  lastSynced: string | null;
}

export type TraderStateWire = 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8; // until JsonStringEnumConverter

export interface TraderRowDto {
  broker: string;
  login: number;
  group: string | null;
  completedXauTrades: number;
  netSourcePnl: number;
  earlyScore: number;
  mlProbability: number | null;
  riskScore: number;
  martingale: boolean;
  averagingDown: boolean;
  lotEscalation: boolean;
  state: TraderStateWire;
  shadowPnl: number;
  lastScored: string;
}

export interface FixSessionDto {
  qualifier: string;
  host: string;
  port: number;
  connected: boolean;
  loggedOn: boolean;
  status: string;
  lastInbound: string | null;
  lastOutbound: string | null;
  inboundSeq: number;
  outboundSeq: number;
  reconnectCount: number;
  lastError: string | null;
  instrumentId: string | null;
  bid: number | null;
  ask: number | null;
  quoteAgeSeconds: number | null;
  executionEnabled: boolean;
}

export interface RiskDashboardDto {
  dailyPnl: number;
  drawdown: number;
  xauLong: number;
  xauShort: number;
  xauNet: number;
  killSwitch: string;
  realCopyEnabled: boolean;
  recentRejectReasons: string[];
}
```

Drop or relocate `TraderDetail`, `Trade`, `Position`, `ReconciliationStatus`, `HealthStatus`, `ComponentHealth`, `Settings` — they do not belong to `DashboardModels.cs`.

If string enums are desired, add `JsonStringEnumConverter` on the API **and** keep C# `ToString()` fields as strings (do not double-convert).

---

## 10. Sources

| Path | Role |
|---|---|
| `D:\Prop\apps\web\src\types\index.ts` | Left (stale client types) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | Right (live DTOs + query port) |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Actual field population |
| `D:\Prop\apps\api\Program.cs` | Route → DTO / anonymous types |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | Numeric enum 0–8 |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | `None` / `StopNewExecution` / `EmergencyFlatten` |
| `D:\Prop\src\Domain\Enums\FixSessionQualifier.cs` | `Quote` / `Trade` (uppercased in query) |
| `D:\Prop\apps\web\src\api\hooks.ts` | Untyped fetches |
| `D:\Prop\apps\web\src\pages\*.tsx` | Consume C# names via `any` |

**End of B29.** Types file and `DashboardModels.cs` are mismatched; C# is the live contract; TS `index.ts` is unused and should not be treated as source of truth.
