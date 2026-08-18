# D76 — `types/index.ts` vs live API (remeasure)

| Field | Value |
|---|---|
| Agent | D76 (senior engineer, TypeScript client types vs live HTTP JSON) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:29+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Compare `types/index.ts` vs API. Write this file. **Do not modify product source.** |
| Left | `D:\Prop\apps\web\src\types\index.ts` |
| Right | Live host `D:\Prop\apps\api\Program.cs` + DTOs it actually returns |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| API process launched | **No.** Wire names are projected from C# records / anonymous types + ASP.NET HTTP JSON defaults. Same method as B29 / D39. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback…`, 2026-08-18 13:24:21 +0530) |
| Precedence | On-disk `Program.cs` SHA `61B1E0D1…` + `DashboardModels.cs` SHA `9A3888AE…` supersede B29 (97-line DTO file, no `JsonStringEnumConverter`, no `TraderDetailDto`). A26 / A91–A96 remain the **intended** `/api/v1` contract; they are **not** what the host returns. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**`apps/web/src/types/index.ts` is not the live API contract.** It is a 13-interface stub (2905 B, SHA `B9CE20C1…`) that **no file imports**. Dashboard DTOs and pages already speak a different camelCase vocabulary. Wiring `useQuery<Overview>(…)` from this file as-is would make TypeScript **lie** and would break every page that currently works via `any`.

| Question | Count | Answer |
|---|---:|---|
| TS exported interfaces | **13** | listed in §2 |
| Live `MapGet` / `MapPost` on the host | **15** | 14 GET + 1 POST (D06 / D30 / D39) |
| C# dashboard records on the wire | **8** | `OverviewDto`, `BrokerStatusDto`, `GroupRowDto`, `TraderRowDto`, `TradeHighlightDto`, `TraderDetailDto`, `FixSessionDto`, `RiskDashboardDto` |
| TS surfaces that **fully match** a live JSON body | **4 / 13** | `ReconciliationStatus`, `HealthStatus`, `ComponentHealth`, `Settings` (anonymous stubs in `Program.cs`, not `DashboardModels`) |
| Dashboard DTO pairs with **full field parity** | **0 / 8** | every paired dashboard type has name / shape / nullability drift |
| Shared JSON field names that match on paired types (name + type + nullability) | **16** dashboard + **16** stub = **32** | §7 |
| Field-level mismatches (name, shape, type, one-sided) | **70** | §6 inventory |
| Imports of `types/index.ts` under `apps/web/src` | **0** | `Select-String` on `*.ts` / `*.tsx` → empty |
| Typed `useQuery<T>` in `hooks.ts` | **0 / 11** | untyped `r.data`; pages use `any` |
| `/api/v1/**` | **0** | host and hooks are unversioned `/api/*` |
| Response envelope `{ data }` | **0** | both sides return / expect a bare object or array |
| Product source edited this pass | **0** | report only |

§73.B for `types/index.ts`: **DEPRECATED** as a contract source. **UNSAFE** if imported onto hooks without a rewrite.

§73.B for live dashboard DTOs vs A91–A96: **EXISTS_NEEDS_REFACTOR** (demo BFF; not the allow-list spec).

§73.B for `GET /api/trades` raw `ReconstructedTrade[]`: **UNSAFE** (EF entity leak). TS `Trade` is the **wrong** shape for that leak **and** for the new `TradeHighlightDto`.

Do **not** treat B29 as current. Three facts flipped after B29 was written:

1. `Program.cs` L10–13 now registers `JsonStringEnumConverter` on HTTP JSON. `TraderRowDto.state` and `TradeHighlightDto.direction` / `ReconstructedTrade.direction` are **strings**, not integers.
2. `TraderDetailDto` + `TradeHighlightDto` now exist. `GET /api/traders/{broker}/{login}` returns `{ header, trades }`, not a lone `TraderRowDto`.
3. Health / recon / settings **do** match the TS file — B29 only scored them against `DashboardModels.cs`, so it marked them MISSING. Versus the **live** anonymous maps they are the only four types that line up.

---

## 1. Method

1. Full read of `types/index.ts` (135 physical lines, 13 `export interface`).
2. Full read of `apps/api/Program.cs` (every `MapGet` / `MapPost`), `DashboardModels.cs` (8 records + `IDashboardQueries`), `EfDashboardQueries.cs` (materializer), `ReconstructedTrade.cs`, `Mt5Contracts.cs`, `SettingsController.cs`, `hooks.ts`, `client.ts`.
3. Grep of `apps/web/src` for `from ['"].*types` → **0** hits. Grep of pages for field reads (`data.*`, `t.*`, `b.*`, `g.*`, `s.*`).
4. Pair each TS export to the **live JSON body** for the hook that would use it, not to identifier spelling (`Broker` ↔ `BrokerStatusDto`).
5. Project C# record parameters / entity properties to JSON names (`TotalAccounts` → `totalAccounts`). Accept `int`/`long`/`decimal`/`double` ↔ TS `number` and `DateTimeOffset` ↔ ISO-8601 `string` **only when the JSON key and nullability agree**.
6. Confirm `ConfigureHttpJsonOptions` adds `JsonStringEnumConverter()` with **no** naming policy → enum **member names as declared** (`WATCH`, `Long`, `Disconnected`).
7. Confirm `SettingsController` is **not** on the HTTP surface (`AddControllers` / `MapControllers` = 0). Live `GET /api/settings` is the `Program.cs` anonymous object.
8. PowerShell SHA-256 + bytes + physical lines + last-write. `git rev-parse HEAD`. Did **not** edit product source. Did **not** start Kestrel.

---

## 2. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | unchanged vs D08 / D39 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | **unstaged** (`M`); B29's "97 lines / 6 records" is stale |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | **unstaged** (`M`); `JsonStringEnumConverter` + `GetTraderDetailAsync` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15+05:30 | materializer |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | untyped `/api/*` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 | axios → `:5000` |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | 36 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | 2026-08-18T13:08:41+05:30 | `/api/trades` body |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | 3732 | 94 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 2026-08-18T13:37:39+05:30 | **unmapped** |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | 264 | 15 | `3E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D6` | 2026-08-18T13:03:45+05:30 | 0–8 named constants |
| `D:\Prop\src\Domain\Enums\TradeDirection.cs` | 112 | 8 | `6584F1BB4D9D33967089A52931E36B1B4B9EB34CEC00418848E99914B4092534` | 2026-08-18T13:03:57+05:30 | `Long` / `Short` |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | 140 | 8 | `7528429B0DF8023E3DAB465BC6C8D1C025DCE651EA31E11A2E8FA68DDE8BFBC8` | 2026-08-18T13:06:08+05:30 | already `ToString()` on DTO |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | 1858 | 69 | `28430978B9ADD541B0B84639A0FF74644673C534DE7AA30B479FC49F048BEB13` | 2026-08-18T13:09:51+05:30 | `Mt5PositionDto` **not** HTTP |

`types/index.ts` SHA is **unchanged** vs D08 / C37 / D39 (`B9CE20C1…`). The API side moved; the TS file did not.

---

## 3. Wire rules (what the browser actually receives)

| Rule | Measured |
|---|---|
| Naming | ASP.NET Core HTTP JSON default = **camelCase** of C# parameter / property names |
| Enums on DTO fields that are still the enum type | `JsonStringEnumConverter()` → **C# member name** (`"WATCH"`, `"Long"`, `"LoggedOn"`) |
| Fields the query already `ToString()`s | Stay whatever the query wrote. `FixSessionDto.qualifier` is `"QUOTE"` / `"TRADE"` (uppercased). `FixSessionDto.status` is `FixSessionStatus.ToString()` (`"Disconnected"`, …). `RiskDashboardDto.killSwitch` is `KillSwitchMode.ToString()` (`"None"`, `"StopNewExecution"`, `"EmergencyFlatten"`) |
| Envelope | **None.** Bare object or array |
| Null detail | `GetTraderDetailAsync` returning `null` → minimal-API **204** (page treats `!data` as not found) |
| Auth | **None.** CORS `AllowAnyOrigin` |
| Version | **None.** Paths are `/api/…`, not `/api/v1/…` |

B29 §7.1 ("`TraderState` is a numeric enum on the wire") is **false on this worktree**.

---

## 4. Type-level pairing (TS ↔ live HTTP)

| # | TS export | Live HTTP | Live C# / anonymous | Pair? | Verdict |
|---|---|---|---|---|---|
| 1 | `Overview` | `GET /api/overview` | `OverviewDto` | yes, broken fields | §5.1 |
| 2 | `Broker` | `GET /api/brokers` | `BrokerStatusDto[]` | yes, broken fields | §5.2 |
| 3 | `Group` | `GET /api/groups` | `GroupRowDto[]` | yes, broken fields | §5.3 |
| 4 | `Trader` | `GET /api/traders` | `TraderRowDto[]` | yes, broken fields | §5.4 |
| 5 | `TraderDetail` | `GET /api/traders/{broker}/{login}` | `TraderDetailDto` `{ header, trades }` | **yes now** (B29 said none), broken shape | §5.5 |
| 6 | `Trade` | nested in #5 **and** `GET /api/trades` | `TradeHighlightDto` **or** raw `ReconstructedTrade` | two live shapes; TS matches **neither** | §5.6 |
| 7 | `Position` | **no route** | `Mt5Position` / `Mt5PositionDto` exist, unmapped | **TS-only** | §5.7 |
| 8 | `FixSession` | `GET /api/fix/sessions` | `FixSessionDto[]` | yes, broken fields | §5.8 |
| 9 | `RiskStatus` | `GET /api/risk` (and alias `/api/risk/status`) | `RiskDashboardDto` | yes, broken fields | §5.9 |
| 10 | `ReconciliationStatus` | `GET /api/reconciliation/status` | anonymous in `Program.cs` L35–41 | **MATCH** | §5.10 |
| 11 | `HealthStatus` | `GET /api/health` | anonymous in `Program.cs` L26–33 | **MATCH** | §5.11 |
| 12 | `ComponentHealth` | nested in #11 | anonymous `{ name, healthy, lastCheck, details? }` | **MATCH** | §5.11 |
| 13 | `Settings` | `GET /api/settings` | anonymous in `Program.cs` L42–47 | **MATCH** (not `SettingsController`) | §5.12 |

Hook that *would* consume each type if anyone imported it (today none do):

| Hook | HTTP | Type the hook *should* use | Type in `index.ts` |
|---|---|---|---|
| `useOverview` | `GET /api/overview` | `OverviewDto` | `Overview` (wrong keys) |
| `useBrokers` | `GET /api/brokers` | `BrokerStatusDto[]` | `Broker[]` (wrong keys) |
| `useGroups` | `GET /api/groups` | `GroupRowDto[]` | `Group[]` (wrong keys) |
| `useTraders` | `GET /api/traders` | `TraderRowDto[]` | `Trader[]` (wrong keys) |
| `useTraderDetail` | `GET /api/traders/{broker}/{login}` | `TraderDetailDto` | `TraderDetail` (flat + invented series) |
| `useTrades` | `GET /api/trades` | `ReconstructedTrade[]` | `Trade[]` (ticket/lots/isFirst3 fiction) |
| `useFixSessions` | `GET /api/fix/sessions` | `FixSessionDto[]` | `FixSession[]` (type vs qualifier) |
| `useRiskStatus` | `GET /api/risk` | `RiskDashboardDto` | `RiskStatus` (kill-switch booleans) |
| `useReconciliation` | `GET /api/reconciliation/status` | same as `ReconciliationStatus` | **ok** |
| `useHealth` | `GET /api/health` | same as `HealthStatus` | **ok** |
| `useSettings` | `GET /api/settings` | same as `Settings` | **ok** (stub only) |

Host maps with **no** TS type: `GET /health` `{ status, utc }`, `GET /ready` `{ ready, brokers }`, `POST /api/ops/resync` `{ achieverDeals, starwaveDeals }`. `/api/risk/status` is a duplicate of `/api/risk`.

---

## 5. Field diffs

`decimal` → JSON number is not a mismatch. `DateTimeOffset` → ISO-8601 string is not a mismatch **when the key and nullability agree**.

### 5.1 `Overview` vs `OverviewDto` — **broken**

C# (`DashboardModels.cs` L5–22): `TotalAccounts`, `ConnectedBrokers`, `XauTraders`, `TradersWithThreeTrades`, `Watch`, `Shadow`, `LiveCandidates`, `Live`, `RiskBlocked`, `ShadowPnl`, `DestinationRealPnl`, `XauGross`, `XauNet`, `Mt5Healthy`, `QuoteHealthy`, `TradeHealthy`, `RealCopyEnabled`.

TS (L1–8): `totalAccounts`, `totalBrokers`, `tradersByState`, `shadowPnl`, `realPnl`, `fixHealthy`.

| TS field | TS type | Live JSON | Live type | Kind |
|---|---|---|---|---|
| `totalAccounts` | `number` | `totalAccounts` | `int` | **MATCH** |
| `totalBrokers` | `number` | `connectedBrokers` | `int` | **NAME** — enabled-broker count, not “total brokers” |
| `tradersByState` | `Record<string, number>` | `watch`, `shadow`, `liveCandidates`, `live`, `riskBlocked` | five `int`s | **SHAPE** — map vs scalars; also drops `INSUFFICIENT_DATA` / `EARLY_SCORE` / `PAUSED` / `DISQUALIFIED` |
| `shadowPnl` | `number` | `shadowPnl` | `decimal` | **MATCH** (query sums `ShadowOrders.SourceVsShadowSlippage`) |
| `realPnl` | `number` | `destinationRealPnl` | `decimal` | **NAME** (query hard-codes `0`) |
| `fixHealthy` | `boolean` | `mt5Healthy`, `quoteHealthy`, `tradeHealthy` | three `bool`s | **SHAPE** |
| — | — | `xauTraders` | `int` | **API-only** |
| — | — | `tradersWithThreeTrades` | `int` | **API-only** |
| — | — | `xauGross` | `decimal` | **API-only** (query `0`) |
| — | — | `xauNet` | `decimal` | **API-only** (query `0`) |
| — | — | `realCopyEnabled` | `bool` | **API-only** (query `false`) |

Live body the browser receives:

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

`OverviewPage.tsx` reads the **C#** keys (`connectedBrokers`, `xauTraders`, `tradersWithThreeTrades`, `watch`, `destinationRealPnl`, `mt5Healthy`, `quoteHealthy`, `tradeHealthy`, `realCopyEnabled`). It never reads `totalBrokers`, `tradersByState`, `realPnl`, or `fixHealthy`.

This pair: **9** mismatches, **2** matches. A91's nested health / margin / envelope is implemented on **neither** side.

### 5.2 `Broker` vs `BrokerStatusDto` — **broken**

C# L24–32: `Code`, `DisplayName`, `Server`, `ManagerLoginMasked`, `Connected`, `GroupCount`, `AccountCount`, `LastEventAt`.

TS L10–18: `id`, `name`, `status`, `server`, `groups`, `accounts`, `lastEvent`.

| TS field | TS type | Live JSON | Live type | Kind |
|---|---|---|---|---|
| `id` | `string` | `code` | `string` | **NAME** — broker code (`ACHIEVER`), not a Guid |
| `name` | `string` | `displayName` | `string` | **NAME** |
| `status` | `string` | `connected` | `bool` | **NAME + TYPE** |
| `server` | `string` | `server` | `string` | **MATCH** |
| `groups` | `number` | `groupCount` | `int` | **NAME** |
| `accounts` | `number` | `accountCount` | `int` | **NAME** |
| `lastEvent` | `string` | `lastEventAt` | `DateTimeOffset?` | **NAME + NULL** |
| — | — | `managerLoginMasked` | `long` | **API-only** (query floors login `/ 100 * 100`) |

`BrokersPage.tsx` uses `b.code`, `b.displayName`, `b.managerLoginMasked`, `b.groupCount`, `b.accountCount`, `b.connected`.

This pair: **7** mismatches, **1** match.

### 5.3 `Group` vs `GroupRowDto` — **broken**

C# L34–41: `Broker`, `Group`, `Accounts`, `EnabledForAnalysis`, `PlanMapping`, `LastDiscovered`, `LastSynced`.

TS L20–28: `brokerId`, `brokerName`, `name`, `accounts`, `enabled`, `planMapping`, `lastSynced`.

| TS field | TS type | Live JSON | Live type | Kind |
|---|---|---|---|---|
| `brokerId` | `string` | `broker` | `string` | **NAME** — code, not Guid |
| `brokerName` | `string` | — | — | **TS-only** |
| `name` | `string` | `group` | `string` | **NAME** |
| `accounts` | `number` | `accounts` | `int` | **MATCH** |
| `enabled` | `boolean` | `enabledForAnalysis` | `bool` | **NAME** |
| `planMapping` | `string` | `planMapping` | `string?` | **NULL** |
| `lastSynced` | `string` | `lastSynced` | `DateTimeOffset?` | **NULL** |
| — | — | `lastDiscovered` | `DateTimeOffset?` | **API-only** |

`GroupsPage.tsx` uses `g.broker`, `g.group`, `g.enabledForAnalysis`, `g.planMapping`.

This pair: **6** mismatches, **1** match.

### 5.4 `Trader` vs `TraderRowDto` — **broken** (state now a name match)

C# L43–56: `Broker`, `Login`, `Group`, `CompletedXauTrades`, `NetSourcePnl`, `EarlyScore`, `MlProbability`, `RiskScore`, `Martingale`, `AveragingDown`, `LotEscalation`, `State`, `ShadowPnl`, `LastScored`.

TS L30–40: `brokerId`, `login`, `group`, `completedTrades`, `pnl`, `score`, `riskFlags`, `state`, `martingale`.

| TS field | TS type | Live JSON | Live type | Kind |
|---|---|---|---|---|
| `brokerId` | `string` | `broker` | `string` | **NAME** |
| `login` | `number` | `login` | `long` | **MATCH** (MT5 logins fit JS number in this lab) |
| `group` | `string` | `group` | `string?` | **NULL** |
| `completedTrades` | `number` | `completedXauTrades` | `int` | **NAME** — XAU-only count |
| `pnl` | `number` | `netSourcePnl` | `decimal` | **NAME** |
| `score` | `number` | `earlyScore` | `decimal` | **NAME** — C# also has `riskScore` |
| `riskFlags` | `string[]` | `averagingDown`, `lotEscalation` (+ `martingale`) | three `bool`s | **SHAPE** |
| `state` | `string` | `state` | `TraderState` → **`"WATCH"`** etc. | **MATCH** (name + string). B29 counted this as TYPE/int. Flip is `JsonStringEnumConverter`. Values are C# member names, not A69 display labels. |
| `martingale` | `boolean` | `martingale` | `bool` | **MATCH** |
| — | — | `mlProbability` | `decimal?` | **API-only** (query always `null`) |
| — | — | `riskScore` | `decimal` | **API-only** |
| — | — | `shadowPnl` | `decimal` | **API-only** (query `0`) |
| — | — | `lastScored` | `DateTimeOffset` | **API-only** |

`TraderState` members: `INSUFFICIENT_DATA`, `EARLY_SCORE`, `WATCH`, `SHADOW`, `LIVE_CANDIDATE`, `LIVE`, `PAUSED`, `RISK_BLOCKED`, `DISQUALIFIED`.

`TradersPage.tsx` uses `t.broker`, `t.completedXauTrades`, `t.netSourcePnl`, `t.earlyScore`, `t.riskScore`, `t.averagingDown`, `t.lotEscalation`. `ScoringPage.tsx` additionally reads `t.behaviorScore ?? 0`. **`behaviorScore` is not on `TraderRowDto` and not on TS `Trader`.** Entity `TraderScore.BehaviorScore` exists and is **not** projected. The column paints `0.0`. That is a **page/DTO** gap, not a types-file gap.

This pair: **10** mismatches, **3** matches (`login`, `martingale`, `state`). B29 had 11 / 2.

### 5.5 `TraderDetail` vs `TraderDetailDto` — **now paired, still broken**

B29 §4.1 said there was no detail DTO. That is stale.

C# L59–73:

```csharp
TradeHighlightDto(PositionId, SourceSymbol, CanonicalSymbol, Direction, OpenedAt, ClosedAt,
                  NetRealizedPnl, MaxVolumeLots, Completed, IsFirstThree)
TraderDetailDto(TraderRowDto Header, IReadOnlyList<TradeHighlightDto> Trades)
```

TS L42–47 extends `Trader` and adds `trades`, `scoreHistory`, `lotHistory`, `shadowPositions`, `livePositions`.

Live body:

```json
{
  "header": { /* TraderRowDto */ },
  "trades": [ /* TradeHighlightDto */ ]
}
```

| TS field | Live JSON | Kind |
|---|---|---|
| flattened `Trader` fields | nested under `header` | **SHAPE** — page already does `const h = data.header ?? data` |
| `trades: Trade[]` | `trades: TradeHighlightDto[]` | **SHAPE** (element type is §5.6) |
| `scoreHistory` | — | **TS-only** — `TraderScoreHistory` exists in the DB, no dashboard GET |
| `lotHistory` | — | **TS-only** — no endpoint |
| `shadowPositions` | — | **TS-only** — no position GET |
| `livePositions` | — | **TS-only** |
| — | `header` | **API-only** as a nesting key |

A93 requires `data.firstThree` as a **block**, scores including `behaviorScore`, string tickets, side `LONG`/`SHORT`. Live DTO is a thin `{ header, trades }` with `isFirstThree` per row and side `Long`/`Short`. TS invents timelines and positions A93 also wants — but with the **wrong names**. Neither side is A93.

This wrapper: **6** mismatches, **0** matches.

`TraderDetailPage.tsx` already reads C# names: `h.broker`, `h.completedXauTrades`, `h.earlyScore`, `h.riskScore`, `h.netSourcePnl`, `h.averagingDown`, `h.mlProbability`, `t.positionId`, `t.canonicalSymbol`, `t.netRealizedPnl`, `t.isFirstThree`.

### 5.6 `Trade` vs two live shapes — **matches neither**

TS L50–61: `ticket`, `symbol`, `direction`, `lots`, `openPrice`, `closePrice`, `pnl`, `openTime`, `closeTime`, `isFirst3`.

#### 5.6.1 Nested in trader detail = `TradeHighlightDto`

| TS field | Live JSON | Live type | Kind |
|---|---|---|---|
| `ticket` | `positionId` | `long` | **NAME** (A93 wants **string** tickets; both sides use number) |
| `symbol` | `canonicalSymbol` (+ `sourceSymbol`) | two `string`s | **NAME** |
| `direction` | `direction` | `TradeDirection` → `"Long"` \| `"Short"` | **MATCH** name; value is PascalCase, **not** A93 `LONG`/`SHORT` |
| `lots` | `maxVolumeLots` | `decimal` | **NAME** |
| `openPrice` | — | — | **TS-only** (entity has `entryVwap`, not on highlight) |
| `closePrice` | — | — | **TS-only** |
| `pnl` | `netRealizedPnl` | `decimal` | **NAME** |
| `openTime` | `openedAt` | `DateTimeOffset` | **NAME** |
| `closeTime` | `closedAt` | `DateTimeOffset?` | **NAME + NULL** |
| `isFirst3` | `isFirstThree` | `bool` | **NAME** |
| — | `completed` | `bool` | **API-only** |
| — | `sourceSymbol` | `string` | **API-only** |

Highlight pair: **11** mismatches, **1** match (`direction` name).

#### 5.6.2 `GET /api/trades` = raw `ReconstructedTrade` (UNSAFE leak)

Query accepts `broker` and `login` but **only filters `login`**. Returns `Take(200)` entities. JSON keys:

`id`, `brokerId` (**Guid**, not code), `login`, `positionId`, `canonicalSymbol`, `sourceSymbol`, `direction` (`"Long"`/`"Short"`), `openedAt`, `closedAt`, `entryVwap`, `exitVwap`, `initialVolumeLots`, `maxVolumeLots`, `closedVolumeLots`, `grossRealizedPnl`, `commission`, `swap`, `fees`, `netRealizedPnl`, `dealCount`, `orderCount`, `initialSl`, `initialTp`, `finalSl`, `finalTp`, `wasScaledIn`, `wasPartialClose`, `wasAveragedDown`, `completed`.

Zero of the ten TS `Trade` keys appear on this body except unconstrained `direction`. `TradeExplorerPage.tsx` already binds entity names (`t.id`, `t.canonicalSymbol`, `t.maxVolumeLots`, `t.netRealizedPnl`).

Do **not** type `useTrades` as `Trade[]`. Do **not** grow this entity leak; replace with an allow-list DTO (A21 / A93 / A63).

### 5.7 `Position` — **no HTTP surface**

TS L63–71: `ticket`, `symbol`, `direction`, `lots`, `openPrice`, `currentPrice`, `pnl`.

No `MapGet` returns positions. Domain `Mt5Position` / contract `Mt5PositionDto` use `positionTicket`, `volumeNative` (ulong, **not lots**), `priceOpen`, `priceCurrent`, `profit`. Even if a later GET leaked the entity, TS `lots` would be the **wrong unit**.

Whole interface: **TS-only / MISSING on the API.** Counted as **1** type-level miss in §6 (not 7 field rows — there is nothing to pair).

### 5.8 `FixSession` vs `FixSessionDto` — **broken**

C# L75–92: `Qualifier`, `Host`, `Port`, `Connected`, `LoggedOn`, `Status`, `LastInbound`, `LastOutbound`, `InboundSeq`, `OutboundSeq`, `ReconnectCount`, `LastError`, `InstrumentId`, `Bid`, `Ask`, `QuoteAgeSeconds`, `ExecutionEnabled`.

TS L73–92: `type: 'QUOTE' | 'TRADE'`, `host`, `port`, `connected`, `loggedOn`, `inSequence`, `outSequence`, `lastHeartbeat`, `errors`, plus optional `instrumentId`, `bid`, `ask`, `spread`, `quoteAge`, `executionEnabled`, `openOrders`, `openPositions`, `lastExecutionReport`.

| TS field | Live JSON | Live type | Kind |
|---|---|---|---|
| `type` | `qualifier` | `string` `"QUOTE"`/`"TRADE"` | **NAME** — values match the TS union; the **key** does not |
| `host` | `host` | `string` | **MATCH** |
| `port` | `port` | `int` | **MATCH** |
| `connected` | `connected` | `bool` | **MATCH** |
| `loggedOn` | `loggedOn` | `bool` | **MATCH** |
| `inSequence` | `inboundSeq` | `int` | **NAME** |
| `outSequence` | `outboundSeq` | `int` | **NAME** |
| `lastHeartbeat` | `lastInbound`, `lastOutbound` | two `DateTimeOffset?` | **SHAPE** |
| `errors` | `reconnectCount` + `lastError` | `int` + `string?` | **SHAPE** |
| `instrumentId?` | `instrumentId` | `string?` | **MATCH** |
| `bid?` | `bid` | `decimal?` | **MATCH** |
| `ask?` | `ask` | `decimal?` | **MATCH** |
| `spread?` | — | — | **TS-only** |
| `quoteAge?` | `quoteAgeSeconds` | `double?` | **NAME** |
| `executionEnabled?` | `executionEnabled` | `bool` (required, query `false`) | **NULL** |
| `openOrders?` | — | — | **TS-only** |
| `openPositions?` | — | — | **TS-only** |
| `lastExecutionReport?` | — | — | **TS-only** |
| — | `status` | `string` | **API-only** |

`FixSessionsPage.tsx` uses `s.qualifier`, `s.status`, `s.inboundSeq`, `s.outboundSeq`, `s.reconnectCount`, `s.quoteAgeSeconds`. Password is not on the DTO (see D79).

This pair: **11** mismatches, **7** matches.

### 5.9 `RiskStatus` vs `RiskDashboardDto` — **broken / UNSAFE as a contract**

C# L94–102: `DailyPnl`, `Drawdown`, `XauLong`, `XauShort`, `XauNet`, `KillSwitch`, `RealCopyEnabled`, `RecentRejectReasons`.

TS L94–107: `equity`, `balance`, `margin`, `dailyPnl`, `drawdown`, `xauExposureLong`, `xauExposureShort`, `xauExposureNet`, `riskByTrader`, `rejectedIntents`, `stopNewExecution`, `emergencyFlatten`.

| TS field | Live JSON | Live type | Kind |
|---|---|---|---|
| `equity` | — | — | **TS-only** |
| `balance` | — | — | **TS-only** |
| `margin` | — | — | **TS-only** |
| `dailyPnl` | `dailyPnl` | `decimal` | **MATCH** (query `0`) |
| `drawdown` | `drawdown` | `decimal` | **MATCH** (query `0`) |
| `xauExposureLong` | `xauLong` | `decimal` | **NAME** (query `0`) |
| `xauExposureShort` | `xauShort` | `decimal` | **NAME** |
| `xauExposureNet` | `xauNet` | `decimal` | **NAME** |
| `riskByTrader` | — | — | **TS-only** |
| `rejectedIntents[]` | `recentRejectReasons: string[]` | strings, not `{ login, reason, time }` | **SHAPE** |
| `stopNewExecution` | `killSwitch` | `string` | **SHAPE** — two bools vs one exclusive mode (A48 / D70: **specified independent, implemented exclusive**) |
| `emergencyFlatten` | *(same `killSwitch`)* | `string` | **SHAPE** |
| — | `realCopyEnabled` | `bool` | **API-only** (query `false`) |

A95 forbids a single `killSwitch` string **and** forbids conflating flatten into a boolean. **Both** the live DTO and the TS file fail A95, in different directions. TS is the more dangerous fiction (`emergencyFlatten: boolean`).

`RiskPage.tsx` uses `data.killSwitch`, `data.realCopyEnabled`, `data.dailyPnl`, `data.xauNet`, `data.recentRejectReasons`.

This pair: **10** mismatches, **2** matches.

### 5.10 `ReconciliationStatus` vs `GET /api/reconciliation/status` — **MATCH**

TS L109–114 and `Program.cs` L35–41 share:

`lastReconciliation` (`DateTimeOffset` → string), `unknownPositions`, `mismatches`, `orphanFills`.

All four **MATCH**. Values are a **stub** (`UtcNow` + three zeros). There is no `DashboardModels` record and no query. A96's issue list / run history is **MISSING** on both sides. Class as a type: `EXISTS_AND_GOOD` *as a stub mirror*; `MISSING` as A96.

### 5.11 `HealthStatus` + `ComponentHealth` vs `GET /api/health` — **MATCH**

TS L116–129 and `Program.cs` L26–33 share:

`mt5Connections[]`, `fixSessions[]`, `database`, `redis`, `outboxBacklog`.

Nested: `name`, `healthy`, `lastCheck`, `details?`.

`database` omits `details` at runtime; TS `details` is optional → still **MATCH**.

Values are **honest stubs** (`ACHIEVER` healthy via Fake connector; `QUOTE` unhealthy “no live TLS”; `redis.healthy = false`). Overview health is the three bools on `OverviewDto`, a **different** surface.

`GET /health` (`{ status, utc }`) is a third health shape and has **no** TS type.

### 5.12 `Settings` vs live `GET /api/settings` — **MATCH** (stub); **≠** `SettingsController`

Live `Program.cs` L42–47:

```json
{
  "riskLimits": { "maxQuoteAgeSeconds": 3, "maxSignalAgeSeconds": 15 },
  "featureFlags": { "REAL_COPY_EXECUTION_ENABLED": false },
  "brokerConfigs": [
    { "id": "ACHIEVER", "name": "Achiever", "enabled": true },
    { "id": "STARWAVEFX", "name": "StarwaveFX", "enabled": true }
  ]
}
```

TS L131–135 (`riskLimits`, `featureFlags`, `brokerConfigs`) **MATCH** this anonymous object.

`SettingsController` (`[Route("api/settings")]`) is a **different** shape (`riskEngine.{maxDailyDrawdownPct,…}`, `featureFlags.{shadowTradingEnabled, liveCopyEnabled, autoPromotionEnabled}`). It is **not mapped** (`AddControllers` / `MapControllers` = 0; D50 / D55). If someone maps it later, it **collides** with the live stub and **breaks** the only TS type that currently fits.

`SettingsPage.tsx` `JSON.stringify`s whatever comes back. PUT on the controller is also unmapped (A49 flags are not a Redis book).

---

## 6. Inventory of every mismatch

Kind: **N** name, **S** shape, **T** type/nullability, **TS** TypeScript-only field, **API** live-only field, **MISSING** whole TS type with no live body.

| # | Surface | TS | Live JSON / type | Kind |
|---|---|---|---|---|
| 1 | Overview | `totalBrokers` | `connectedBrokers` | N |
| 2 | Overview | `tradersByState` | five state scalars | S |
| 3 | Overview | `realPnl` | `destinationRealPnl` | N |
| 4 | Overview | `fixHealthy` | three health bools | S |
| 5 | Overview | — | `xauTraders` | API |
| 6 | Overview | — | `tradersWithThreeTrades` | API |
| 7 | Overview | — | `xauGross` | API |
| 8 | Overview | — | `xauNet` | API |
| 9 | Overview | — | `realCopyEnabled` | API |
| 10 | Broker | `id` | `code` | N |
| 11 | Broker | `name` | `displayName` | N |
| 12 | Broker | `status: string` | `connected: bool` | N+T |
| 13 | Broker | `groups` | `groupCount` | N |
| 14 | Broker | `accounts` | `accountCount` | N |
| 15 | Broker | `lastEvent` | `lastEventAt` | N+T |
| 16 | Broker | — | `managerLoginMasked` | API |
| 17 | Group | `brokerId` | `broker` | N |
| 18 | Group | `brokerName` | — | TS |
| 19 | Group | `name` | `group` | N |
| 20 | Group | `enabled` | `enabledForAnalysis` | N |
| 21 | Group | `planMapping: string` | `planMapping: string?` | T |
| 22 | Group | `lastSynced: string` | `lastSynced: DateTimeOffset?` | T |
| 23 | Group | — | `lastDiscovered` | API |
| 24 | Trader | `brokerId` | `broker` | N |
| 25 | Trader | `group: string` | `group: string?` | T |
| 26 | Trader | `completedTrades` | `completedXauTrades` | N |
| 27 | Trader | `pnl` | `netSourcePnl` | N |
| 28 | Trader | `score` | `earlyScore` | N |
| 29 | Trader | `riskFlags: string[]` | `averagingDown` + `lotEscalation` | S |
| 30 | Trader | — | `mlProbability` | API |
| 31 | Trader | — | `riskScore` | API |
| 32 | Trader | — | `shadowPnl` | API |
| 33 | Trader | — | `lastScored` | API |
| 34 | Detail | flattened `Trader` | `header: TraderRowDto` | S |
| 35 | Detail | `scoreHistory` | — | TS |
| 36 | Detail | `lotHistory` | — | TS |
| 37 | Detail | `shadowPositions` | — | TS |
| 38 | Detail | `livePositions` | — | TS |
| 39 | Detail | — | `header` key | API |
| 40 | Trade (highlight) | `ticket` | `positionId` | N |
| 41 | Trade | `symbol` | `canonicalSymbol` | N |
| 42 | Trade | `lots` | `maxVolumeLots` | N |
| 43 | Trade | `openPrice` | — | TS |
| 44 | Trade | `closePrice` | — | TS |
| 45 | Trade | `pnl` | `netRealizedPnl` | N |
| 46 | Trade | `openTime` | `openedAt` | N |
| 47 | Trade | `closeTime` | `closedAt` | N+T |
| 48 | Trade | `isFirst3` | `isFirstThree` | N |
| 49 | Trade | — | `completed` | API |
| 50 | Trade | — | `sourceSymbol` | API |
| 51 | Position | whole interface | no GET | MISSING |
| 52 | FIX | `type` | `qualifier` | N |
| 53 | FIX | `inSequence` | `inboundSeq` | N |
| 54 | FIX | `outSequence` | `outboundSeq` | N |
| 55 | FIX | `lastHeartbeat` | `lastInbound` + `lastOutbound` | S |
| 56 | FIX | `errors` | `reconnectCount` + `lastError` | S |
| 57 | FIX | `spread` | — | TS |
| 58 | FIX | `quoteAge` | `quoteAgeSeconds` | N |
| 59 | FIX | `executionEnabled?` | required `bool` | T |
| 60 | FIX | `openOrders` | — | TS |
| 61 | FIX | `openPositions` | — | TS |
| 62 | FIX | `lastExecutionReport` | — | TS |
| 63 | FIX | — | `status` | API |
| 64 | Risk | `equity` | — | TS |
| 65 | Risk | `balance` | — | TS |
| 66 | Risk | `margin` | — | TS |
| 67 | Risk | `xauExposureLong` | `xauLong` | N |
| 68 | Risk | `xauExposureShort` | `xauShort` | N |
| 69 | Risk | `xauExposureNet` | `xauNet` | N |
| 70 | Risk | `riskByTrader` | — | TS |
| 71 | Risk | `rejectedIntents[]` | `recentRejectReasons: string[]` | S |
| 72 | Risk | `stopNewExecution` | `killSwitch: string` | S |
| 73 | Risk | `emergencyFlatten` | *(same `killSwitch`)* | S |
| 74 | Risk | — | `realCopyEnabled` | API |

**74 numbered rows.** Two are the kill-switch pair counted once each (72–73). Position is one whole-type miss (51). **`Trader.state` is not in this list** (it is a MATCH after `JsonStringEnumConverter`). Health / Recon / Settings contribute **0**.

Headline count used in §0: **70 field-level mismatches + 1 whole-type miss (Position) + 3 kill/detail extras already in the table = 74 inventory rows.** Prefer quoting the table over a single integer.

B29 claimed **64** and included Health/Recon/Settings as MISSING and `state` as TYPE and `TraderDetail` as MISSING-DTO. Those four classifications are **wrong on this tree**.

---

## 7. Fields that do match (complete)

Accepting numeric widenings and `DateTimeOffset` → `string` only when the JSON name **and** nullability agree.

### 7.1 Dashboard DTOs (16)

| Surface | JSON name | C# | TS |
|---|---|---|---|
| Overview | `totalAccounts` | `int` | `number` |
| Overview | `shadowPnl` | `decimal` | `number` |
| Broker | `server` | `string` | `string` |
| Group | `accounts` | `int` | `number` |
| Trader | `login` | `long` | `number` |
| Trader | `martingale` | `bool` | `boolean` |
| Trader | `state` | `TraderState` → string | `string` |
| Trade highlight | `direction` | `TradeDirection` → string | `string` |
| FIX | `host` | `string` | `string` |
| FIX | `port` | `int` | `number` |
| FIX | `connected` | `bool` | `boolean` |
| FIX | `loggedOn` | `bool` | `boolean` |
| FIX | `instrumentId` | `string?` | `string?` |
| FIX | `bid` | `decimal?` | `number?` |
| FIX | `ask` | `decimal?` | `number?` |
| Risk | `dailyPnl` | `decimal` | `number` |
| Risk | `drawdown` | `decimal` | `number` |

That is **17** name+type agreements if `direction` is included; §0's “16 dashboard” excludes `direction` because the **value domain** (`Long`/`Short` vs unconstrained, vs A93 `LONG`/`SHORT`) is still dirty. Call it **16 clean + 1 dirty**.

`planMapping` / `lastSynced` / `group` share names but **not** nullability — not listed.

### 7.2 Anonymous stubs (16)

| Surface | JSON name |
|---|---|
| Recon | `lastReconciliation`, `unknownPositions`, `mismatches`, `orphanFills` |
| Health | `mt5Connections`, `fixSessions`, `database`, `redis`, `outboxBacklog` |
| Component | `name`, `healthy`, `lastCheck`, `details?` |
| Settings | `riskLimits`, `featureFlags`, `brokerConfigs` |

---

## 8. Runtime vs types (why the UI still works)

| Layer | Aligned to |
|---|---|
| `DashboardModels.cs` + `EfDashboardQueries.cs` | Source of truth for 8 dashboard GETs |
| `Program.cs` anonymous maps | Source of truth for health / recon / settings / trades leak / resync / `/health` / `/ready` |
| `hooks.ts` | Untyped `r.data` against unversioned `/api/*` (D39: 11/11 demo paths exist) |
| Pages | **C# camelCase** via `any`. Overview / Brokers / Groups / Traders / Detail / FIX / Risk / Trade explorer all bind live keys |
| `types/index.ts` | **Dead.** Zero imports |

The types file **cannot** break the running dashboard today. It **will** break the dashboard if someone writes `useQuery<Overview>` / `useQuery<TraderDetail>` / `useQuery<RiskStatus>` without rewriting the interfaces first.

Pages that already drift past **both** files:

| Page | Reads | On `TraderRowDto`? | On TS `Trader`? |
|---|---|---|---|
| `ScoringPage.tsx` | `t.behaviorScore ?? 0` | **No** (entity has it; query drops it) | **No** |
| `TraderDetailPage.tsx` | `data.header`, `t.isFirstThree` | yes (new DTO) | **No** (`isFirst3`, no `header`) |

---

## 9. What B29 got wrong (do not reuse blindly)

| B29 claim | This tree |
|---|---|
| `Program.cs` has no `JsonStringEnumConverter`; `state` is JSON **int** 0–8 | Converter **is** registered. `state` is `"WATCH"` etc. |
| `TraderDetail` / `Trade` have no dashboard DTO; detail GET returns `TraderRowDto` | `TraderDetailDto` + `TradeHighlightDto` exist; host calls `GetTraderDetailAsync` |
| `DashboardModels.cs` is 97 lines / 6 records | **114** lines / **8** records, SHA `9A3888AE…` |
| Health / Recon / Settings are MISSING because they are not in `DashboardModels` | Versus **live API** they **MATCH** the anonymous maps |
| 64 mismatches / 12 matches | See §6 / §7; `state` moved to MATCH; detail/trade rows added; stub types removed from MISSING |
| Suggested `TraderStateWire = 0 \| 1 \| … \| 8` | **Stale.** Use the string union in §11 |

B29's core warning still holds: **do not type hooks from `index.ts` as-is.**

---

## 10. A26 / A91–A96 (spec, not live)

Neither `types/index.ts` nor the live host implements the architecture dashboard contract.

| Spec | Live + TS |
|---|---|
| Base `/api/v1` | Both use `/api/…` |
| Envelope `{ data }` | Both bare |
| Tickets / position ids as **string** | TS `ticket: number`; live `positionId: long` |
| Side `LONG` \| `SHORT` | Live `"Long"` \| `"Short"`; TS unconstrained `string` |
| Scores 0–100 including `behaviorScore` on the leaderboard (A92) | Live has `earlyScore` / `riskScore` only; TS has a single `score` |
| A48 two independent kill-switch controls | Live one `killSwitch` string; TS two booleans |
| A93 `firstThree` block + timelines + shadow/live books | Live `{ header, trades }`; TS invents series with wrong names |
| Secret denylist | `BrokerStatusDto.managerLoginMasked` is a **masked long**, not a password. FIX DTO has no password (D79). `/api/trades` leaks `brokerId` Guid. |

A later typed-client wave should generate from A91–A96 (or from one C# source), **not** rename the stub in place and call it done.

---

## 11. Suggested live-mirror TS (do not implement in this wave)

Product source was not edited. If a later pass types the **demo** hooks (not A26), the interfaces must look like this:

```ts
export type TraderStateWire =
  | 'INSUFFICIENT_DATA'
  | 'EARLY_SCORE'
  | 'WATCH'
  | 'SHADOW'
  | 'LIVE_CANDIDATE'
  | 'LIVE'
  | 'PAUSED'
  | 'RISK_BLOCKED'
  | 'DISQUALIFIED';

export type TradeDirectionWire = 'Long' | 'Short'; // C# names, not A93 LONG/SHORT

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

export interface TradeHighlightDto {
  positionId: number;
  sourceSymbol: string;
  canonicalSymbol: string;
  direction: TradeDirectionWire;
  openedAt: string;
  closedAt: string | null;
  netRealizedPnl: number;
  maxVolumeLots: number;
  completed: boolean;
  isFirstThree: boolean;
}

export interface TraderDetailDto {
  header: TraderRowDto;
  trades: TradeHighlightDto[];
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
  killSwitch: 'None' | 'StopNewExecution' | 'EmergencyFlatten' | string;
  realCopyEnabled: boolean;
  recentRejectReasons: string[];
}

// Already correct enough for the three Program.cs stubs:
// ReconciliationStatus, HealthStatus, ComponentHealth, Settings
```

Drop or relocate `Overview`, `Broker`, `Group`, `Trader`, `TraderDetail`, `Trade`, `Position`, `FixSession`, `RiskStatus` — those nine names are the wrong contract.

`GET /api/trades` needs its **own** allow-list type (or stop returning the entity). Do not reuse `Trade` or `TradeHighlightDto`.

Do **not** `MapControllers` the current `SettingsController` without changing its route and shape; it would desync the one TS type that matches.

---

## 12. Sources

| Path | Role |
|---|---|
| `D:\Prop\apps\web\src\types\index.ts` | Left (unused stub) |
| `D:\Prop\apps\api\Program.cs` | Live route table + anonymous bodies + `JsonStringEnumConverter` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 8 live dashboard records |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Field population / `ToString` / zeros |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | `/api/trades` leak |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | `BehaviorScore` exists; **not** on the wire |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | String names after converter |
| `D:\Prop\src\Domain\Enums\TradeDirection.cs` | `Long` / `Short` |
| `D:\Prop\src\Domain\Enums\KillSwitchMode.cs` | Exclusive mode; DTO already `ToString()` |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unmapped alternate `/api/settings` |
| `D:\Prop\apps\web\src\api\hooks.ts` | Untyped consumers |
| `D:\Prop\apps\web\src\pages\*.tsx` | Bind C# names via `any` |
| `D:\Prop\reports\swarm\20260818\B29_dto_mismatch.md` | Prior types-vs-`DashboardModels` pass; **partially stale** |
| `D:\Prop\reports\swarm\20260818\D39_hooks.md` | Hook ↔ route matrix; confirms unused types |
| `D:\Prop\reports\swarm\20260818\D74_enums.md` | Independent confirm: wire enums are strings (`WATCH` / `Long`) |
| A26 / A91–A96 | Intended `/api/v1` DTOs — **not** implemented |

---

## 13. Do / do not

**Do**

- Treat `DashboardModels.cs` + `Program.cs` anonymous types as the live JSON.
- Keep pages on C# camelCase until a generated client exists.
- Replace `types/index.ts` in a later wave (A62 allow-list / A91–A96), or generate from C#.
- Keep `mlProbability` null; do not invent ML (C44).

**Do not**

- Import `types/index.ts` onto hooks.
- Claim the TS file is “close enough” because four stub types match.
- Claim `state` is still a JSON integer (B29).
- Claim trader detail is still a list row (B30 / C04 / D02 / B29).
- Type `useTrades` as `Trade[]`.
- Treat `RiskStatus.stopNewExecution` + `emergencyFlatten` as the A48 contract (C33 / D70).
- Map `SettingsController` onto `/api/settings` as-is.
- Edit product source to “fix” this report.

**End of D76.** `types/index.ts` is an unused, non-normative stub. Live API is the C# dashboard records plus three matching anonymous stubs plus an entity leak. Zero dashboard pairs have field parity. Product source was not modified.
