# D49 — `GetTraderAsync` vs A93 (detail is thin)

| Field | Value |
|---|---|
| Agent | D49 (senior engineer; trader-detail thinness) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:38:39+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\D49_detail_thin.md` |
| Assigned | Compare `GetTraderAsync` vs A93. Write this file. Do not modify product source. |
| Law | Architecture v2 **§51**; A93 wire contract (wins for this resource); A21 first-3; A22 scores; A26 §6.5 (superseded where it conflicts); A45 MFE/MAE; A63 catalog; A69 states; A92 leaderboard row |
| Product source modified | **No** |
| Test source modified | **No** |
| Live HTTP / Postgres / `EXPLAIN` | **Not run.** Route and payload claims are from source as read. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

A93 is binding for the trader-detail **resource**. A92 stays binding for the leaderboard **list**. This file does not re-open score formulas (A22) or reconstruction (A21).

---

## 0. Verdict

**`GetTraderAsync` is still a four-line leaderboard-row lookup. It is not the A93 trader-detail document.** A93 §8 and §13 forbid exactly this mapping: do **not** use `IDashboardQueries.GetTraderAsync → TraderRowDto` as the §51 payload, and the method **must** return A93 `TraderDetailDto?`.

The HTTP route has moved since A93 was written. `GET /api/traders/{broker}/{login}` now calls `GetTraderDetailAsync`, which **still calls `GetTraderAsync` for the header** and then appends every reconstructed trade with a boolean `isFirstThree`. That is a thin-plus list, not A93’s required `data.firstThree` three-slot block. Naming the new record `TraderDetailDto` is a collision, not compliance.

| Question | Answer |
|---|---|
| Does `GetTraderAsync` exist? | **Yes.** `EfDashboardQueries` L119–123. |
| What does it return? | `TraderRowDto?` — the **same 14-field A92 list row** as `GetTradersAsync`. |
| Does it match A93 `TraderDetailDto`? | **No.** 0 / 13 always-on root objects. 0 / 16 §51 blocks. |
| Does it embed `firstThree`? | **No.** No trades at all. |
| Does the HTTP GET still bind to it? | **No.** Host binds `GetTraderDetailAsync`. `GetTraderAsync` is now the **header loader** for that method. |
| Is `GetTraderDetailAsync` A93? | **No.** `Header` + `Trades[]`. No envelope, no identity/overview/scores/features/books/map, no 3-slot waiting cards. |
| Keyed `(broker, login)` SQL? | **No.** Reloads the entire leaderboard (`GetTradersAsync`) then `FirstOrDefault` by login. Unique index on `trader_scores (BrokerId, Login)` is unused. |
| Tests T1–T16? | **0 / 16.** `tests/` has **0** hits for `GetTraderAsync`, `GetTraderDetailAsync`, `TraderDetailDto`, `TradeHighlightDto`. |
| A93 §15 “weatherforecast / DTO missing / page absent”? | **Stale.** Host, types, and page now exist — still **thin**. |

Honest one-liner: **`GetTraderAsync` remains the A92 row; A93 remains unimplemented; the new `GetTraderDetailAsync` is a header-plus-table wrapper around the same thin row.**

| Surface | Class vs A93 |
|---|---|
| `GetTraderAsync` | **`MISSING`** as the §51 resource (method exists; contract does not). **`UNSAFE`** as a 5k lookup (full leaderboard reload). |
| Application `TraderDetailDto` (`Header`, `Trades`) | **`EXISTS_NEEDS_REFACTOR`** — stole the A93 type name for an A26-thin shape. |
| `GetTraderDetailAsync` | **`EXISTS_NEEDS_REFACTOR`** vs a demo table; **`MISSING`** vs A93 `FirstThreeBlock` + 13-object root. |
| `GET /api/v1/traders/{brokerId}/{login}` | **`MISSING`** (unversioned `/api/traders/{broker}/{login}` only). |
| React `types/index.ts` A93 TS | **`MISSING`** (unused stub with `isFirst3` / numeric tickets). |

§51 first-useful item (A57 / A93 §15 last paragraph) is **not** done.

---

## 1. Method (this pass)

1. Re-read `IDashboardQueries` + records, `EfDashboardQueries.GetTraderAsync` / `GetTraderDetailAsync`, `Program.cs` route, React `TraderDetailPage` / `hooks.ts` / `types/index.ts`.
2. Re-hash SHA-256 and physical lines (PowerShell `Get-FileHash` + `Get-Content` count).
3. Re-read A93 in full (invariants D1–D15, endpoint, `FirstThreeBlock`, root DTO, §8 domain map, §13 C# names, T1–T16).
4. Cross-check architecture §51, A26 §6.5, A63 catalog, A92 row, A21 `CompletedXauUsdTrades`, persisted `ReconstructedTrade` / `TraderScore` columns.
5. Grep `tests/` and product call sites.
6. **Did not** start the API, **did not** open Postgres, **did not** edit product source.

---

## 2. File identity (re-measured 2026-08-18T13:38:39+05:30)

| File | Bytes | Physical lines | SHA-256 | LastWriteUtc |
|---|---:|---:|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | **8708** | **205** | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | `2026-08-18T08:05:15Z` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | **3088** | **114** | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | `2026-08-18T08:04:59Z` |
| `D:\Prop\apps\api\Program.cs` | **4731** | **95** | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | `2026-08-18T08:05:15Z` |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | **2402** | **56** | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` | `2026-08-18T08:05:59Z` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `2026-08-18T07:46:00Z` |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | `2026-08-18T07:38:18Z` |
| `D:\Prop\reports\swarm\20260818\A93_trader_detail_dto.md` | 48323 | 1177 | `82FA5BDD468F23BD4D7D77E43C968F757B4B208D7A71A8A4F0582F0E3A87E4BA` | `2026-08-18T07:48:56Z` |

Drift vs C36 / D21 (same day, earlier): `EfDashboardQueries` was 7407 B / 168 lines / `37A4DDD2…715EF4ACE`. It has grown by `GetTraderDetailAsync` (L125–160). **Do not reuse the C36 SHA for this class.** D21’s “7/7 methods” and “HTTP = `GetTraderAsync`” rows are stale (now **8** port methods; HTTP = `GetTraderDetailAsync`). A93 §15 host snapshot is also stale — see §14.

---

## 3. What each artifact is

### 3.1 A93 (binding contract)

`A93_trader_detail_dto.md` freezes `GET /api/v1/traders/{brokerId}/{login}` as a **single-resource envelope** whose `data` is a 13-object allow-list DTO. Architecture §51’s sixteen paint blocks are first-class JSON, not a footnote. The first 3 completed reconstructed XAUUSD lifecycles are an embedded `FirstThreeBlock` with **exactly three slots** (empty slots are `waiting: true`, `trade: null`). Full history is a **sub-resource**, not this GET.

A93 wins over A26 §6.5 (boolean `firstThreeTradesHighlighted`, scores on `[0,1]`, side `BUY`/`SELL`) and over A63’s “header + `isFirstThree` flag” sketch.

Normative bans (quoted in substance):

- §8: **Do not** use `IDashboardQueries.GetTraderAsync → TraderRowDto` as this payload.
- §13: `IDashboardQueries.GetTraderAsync` **must** return A93 `TraderDetailDto?`. Keep `GetTradersAsync` on `TraderRowDto`.

### 3.2 `GetTraderAsync` (measured implementation)

```119:123:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }
```

Four statements. No extra projection. No first-3. No account overview. No 404 helper. Identity key is `(broker code, login)`, not `(brokers.id UUID, login)`.

### 3.3 Application `TraderDetailDto` (name collision)

```59:73:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record TradeHighlightDto(
    long PositionId,
    string SourceSymbol,
    string CanonicalSymbol,
    TradeDirection Direction,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal NetRealizedPnl,
    decimal MaxVolumeLots,
    bool Completed,
    bool IsFirstThree);

public sealed record TraderDetailDto(
    TraderRowDto Header,
    IReadOnlyList<TradeHighlightDto> Trades);
```

This is **not** A93 §5. Same type name, two fields. A93’s implementer path was `apps/api/Contracts/TraderDetailDto.cs` with 14 nested records. That folder does not exist.

---

## 4. Port and HTTP map (remeasured)

```104:114:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public interface IDashboardQueries
{
    Task<OverviewDto> GetOverviewAsync(CancellationToken ct);
    Task<IReadOnlyList<BrokerStatusDto>> GetBrokersAsync(CancellationToken ct);
    Task<IReadOnlyList<GroupRowDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct);
    Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct);
    Task<TraderDetailDto?> GetTraderDetailAsync(string broker, long login, CancellationToken ct);
    Task<IReadOnlyList<FixSessionDto>> GetFixSessionsAsync(CancellationToken ct);
    Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct);
}
```

```57:60:D:\Prop\apps\api\Program.cs
app.MapGet("/api/traders", (IDashboardQueries q, string? broker, string? state, CancellationToken ct) =>
    q.GetTradersAsync(broker, state, ct));
app.MapGet("/api/traders/{broker}/{login:long}", (IDashboardQueries q, string broker, long login, CancellationToken ct) =>
    q.GetTraderDetailAsync(broker, login, ct));
```

| Item | A93 | Measured |
|---|---|---|
| Method | `GET` | `GET` |
| Path | `/api/v1/traders/{brokerId}/{login}` | `/api/traders/{broker}/{login:long}` |
| `brokerId` | UUID of `brokers.id` | **Broker `Code`** (`ACHIEVER` / `STARWAVEFX`). React links ` /traders/${t.broker}/${t.login}` |
| `login` | `1 … 2^63-1` | `:long` accepts `0` and negatives |
| Envelope | `{ "data": { … } }` | **Flat** record (`header` + `trades`) |
| Auth / RBAC | Bearer, ReadOnly+ | **Anonymous.** CORS `AllowAnyOrigin` |
| Missing trader | `404 NOT_FOUND` + `{ brokerId, login }` | Query returns `null` → Minimal API **204 No Content** (D39). No error body |
| `include` | csv embeds | **Absent** |
| Sub-resources (`/trades`, `/scores`, `/features`, `/risk-flags`, `/lot-timeline`, `/holding-times`, `/shadow`, `/live-positions`, `/source-destination-map`) | required | **0.** Raw `GET /api/trades` dumps EF entities (login filter; `broker` ignored) |
| Mutations | `PATCH .../state` | **Absent** |
| SignalR `trader.firstThree` | optional for FUV | **Absent** (`/hubs/ops` not mapped) |
| JSON tokens | `SHADOW`, `LONG`, `EXACT`, `TRADE_1` | `JsonStringEnumConverter` with **member names**. `TraderState` is already `SHADOW`. `TradeDirection` serializes **`Long` / `Short`**, not `LONG` / `SHORT` |

`GetTraderAsync` is **not** on the route table. Call graph:

```text
GET /api/traders/{broker}/{login}
  → GetTraderDetailAsync
       → GetTraderAsync
            → GetTradersAsync(broker, state: null)   // 4 full-table reads
            → FirstOrDefault(login)
       → Brokers.SingleOrDefault(Code == broker)
       → ReconstructedTrades WHERE BrokerId && Login  // unbounded
```

React `useTraderDetail` still hits the unversioned path (`hooks.ts` L23–28, SHA unchanged since 07:46). The **page** (08:05) already unwraps `data.header ?? data` and `data.trades ?? []`, so it tracks the thin-plus DTO, not A93.

---

## 5. `GetTraderAsync` — what the four lines actually do

`GetTradersAsync` (L75–117) then a login filter:

| Step | SQL / CPU | Why it is not a detail GET |
|---|---|---|
| 1 | `TraderScores.AsNoTracking().ToListAsync()` | All score rows. Unique `(BrokerId, Login)` unused |
| 2 | `Brokers.AsNoTracking().ToDictionaryAsync(Id)` | All brokers, to resolve `Code` |
| 3 | `Mt5Accounts.AsNoTracking().ToListAsync()` | All accounts; later `FirstOrDefault` per score (**O(n²)**) for `GroupName` only |
| 4 | `ReconstructedTrades` `WHERE Completed` `GROUP BY (BrokerId, Login)` Σ `NetRealizedPnl` | **No** `canonical_symbol = 'XAUUSD'`. Mixes non-XAU into `NetSourcePnl` |
| 5 | In-memory filter `Broker.Equals(broker, OrdinalIgnoreCase)` | Path uses **code**, not UUID |
| 6 | `FirstOrDefault(t => t.Login == login)` | Second half of §10 identity; still not `WHERE b.code = $1 AND s.login = $2` |

Constructor of the returned row (`TraderRowDto`):

| Argument | Source | Honest? |
|---|---|---|
| `Broker` | `brokers.code` | Yes (not `brokerId`) |
| `Login` | `trader_scores.login` | Yes |
| `Group` | `mt5_accounts.group_name` | Yes if account exists |
| `CompletedXauTrades` | `trader_scores.completed_xau_trades` **cache** | A93: **recompute** from eligible rows; log mismatch |
| `NetSourcePnl` | Σ **all** completed reconstructed PnL | A93: eligible **XAU** only |
| `EarlyScore` | `trader_scores.early_quality_score` | Value ok if scorer is; **no** `earlyQualityScore` twin |
| `MlProbability` | literal `null` | **Correct** (A52 / A93 D9) |
| `RiskScore` | `trader_scores.risk_score` | Value ok; no `behaviorScore` |
| `Martingale` / `AveragingDown` / `LotEscalation` | score bools | Partial flags; no `abnormalSizing` / `severeRisk` |
| `State` | `trader_scores.current_state` | No `stateReason`, no A69 existence fallback |
| `ShadowPnl` | literal `0` | **Painted.** Not dest-quote marked book |
| `LastScored` | `last_scored_at` | JSON name is `lastScored`, not A93 `lastScoredAt` |

`TraderScore.BehaviorScore` is **persisted and dropped**. The thin row is thinner than the score table.

Existence rule (A93 §5.1): `200` if **any** of `mt5_accounts`, `trader_scores`, `trader_states`, or a reconstructed XAU row exists. `GetTraderAsync` returns a row only when a **score** exists **and** its `BrokerId` is in `brokers`. Account-only or reconstructed-only traders are silent misses (then 204 on the HTTP wrapper).

---

## 6. A93 always-on root vs `GetTraderAsync`

A93 §5 `data` (no `include` required):

| # | A93 root | On `GetTraderAsync` | On `GetTraderDetailAsync` |
|---:|---|---|---|
| 1 | `generatedAt` | **absent** | **absent** |
| 2 | `identity` (10 fields) | 3 scalars flattened (`broker`, `login`, `group`, `state`) | same, inside `header` |
| 3 | `accountOverview` (10 fields or `null`) | **absent** (`Mt5Account` has balance/equity/margin/leverage — never read) | **absent** |
| 4 | `firstThree` (`FirstThreeBlock`, 3 slots) | **absent** | boolean on each dumped trade; **no** block / slots / badges |
| 5 | `completedXauTrades` | `header.completedXauTrades` (cache) | same |
| 6 | `openXauSourcePositions` | **absent** (`Mt5Positions` unused) | **absent** |
| 7 | `scores` (version, window, 3 scores, freeze, …) | 3 scalars (`earlyScore`, `riskScore`, `mlProbability`) | same |
| 8 | `riskFlags` (5 bools) | 3 bools | same |
| 9 | `behaviorFeatures` + `mfeMae` | only `netSourcePnl` (wrong universe) | same |
| 10 | `shadow` (`BookSummaryDto`) | `shadowPnl = 0` | same |
| 11 | `live` (`LiveBookSummaryDto`) | **absent** | **absent** |
| 12 | `sourceDestinationPreview` (0–3 links) | **absent** | **absent** |
| 13 | `embeds` (always present, lists empty) | **absent** | **absent** |

**GetTraderAsync coverage of A93 always-on roots: 0 / 13.**  
Generous scalar overlap: **~11 named numbers/bools** out of **~120** A93 keys. That is the definition of thin.

---

## 7. Architecture §51 sixteen blocks

Quoted from architecture v2 §51 (A93 §0):

| # | §51 block | `GetTraderAsync` | `GetTraderDetailAsync` + current page |
|---:|---|---|---|
| 1 | Account overview | **NO** | **NO** (8 chips: state / N / early / risk / net / MG / avg / ML) |
| 2 | XAU trade history | **NO** | **Partial** — **all** reconstructed rows, any symbol, unbounded |
| 3 | First 3 trades highlighted | **NO** | **Partial** — `isFirstThree` column + footnote. No `TRADE_1..3` cards, no waiting slots |
| 4 | Score timeline | **NO** (`TraderScoreHistory` unused) | **NO** |
| 5 | Risk flags | 3 of 5 bools as chips | same |
| 6 | Behavior features | net PnL only | same |
| 7 | Lot-size timeline | **NO** | max lots on each row; no series |
| 8 | Holding-time distribution | **NO** | **NO** |
| 9 | SL/TP behavior | **NO** (`InitialSl`/`InitialTp` exist on entity, not mapped) | **NO** |
| 10 | Drawdown | **NO** | **NO** |
| 11 | MFE/MAE when valid | **NO** — fields omitted, not honest `UNAVAILABLE` | **NO** |
| 12 | Shadow copied positions | **NO** | **NO** |
| 13 | Shadow P&L | painted `0` | painted `0` |
| 14 | Live copied positions | **NO** | **NO** |
| 15 | Live P&L | **NO** | **NO** |
| 16 | Source-to-destination mapping | **NO** | **NO** |

`GetTraderAsync` paints **0 / 16** blocks.  
`GetTraderDetailAsync` + page paints **~2 partial / 16**.

A26’s boolean `firstThreeTradesHighlighted` is also absent. A93 replaced it with `firstThree.highlighted`. Neither name is on the wire.

---

## 8. Field matrix — `TraderRowDto` vs A93

JSON names assume ASP.NET `JsonSerializerDefaults.Web` (camelCase). Enums use member names.

| `TraderRowDto` JSON | A93 home | Match? |
|---|---|---|
| `broker` | `identity.brokerCode` | **Partial.** A93 also requires `brokerId` (UUID) and `brokerDisplayName` |
| `login` | `identity.login` | **Yes** (int64 number) |
| `group` | `identity.group` | **Partial.** No `groupId`, `planMapping`, `enabledForAnalysis` |
| `completedXauTrades` | root + `scores.completedXauTrades` | **Partial.** Cache, not recomputed eligible N |
| `netSourcePnl` | `behaviorFeatures.netSourcePnl` | **No.** All-completed Σ, not eligible XAU |
| `earlyScore` | `scores.earlyScore` **and** `earlyQualityScore` (must be equal) | **Partial.** Twin field missing; no `version=baseline.v1`, no `window`, no `earlyScoreEligible`, no `firstThreeFrozen` |
| `mlProbability` | `scores.mlProbability` | **Yes** (`null`) |
| `riskScore` | `scores.riskScore` | **Partial.** `behaviorScore` dropped |
| `martingale` | `riskFlags.martingale` | Yes |
| `averagingDown` | `riskFlags.averagingDown` | Yes |
| `lotEscalation` | `riskFlags.lotEscalation` | Yes |
| *(missing)* | `riskFlags.abnormalSizing`, `severeRisk` | **No** — A93 says `false` until flags exist; fields not emitted |
| `state` | `identity.state` | **Partial.** No `stateReason` (A69 `R0`…`R9`) |
| `shadowPnl` | `shadow.pnl` | **No.** Literal `0`; not a book summary |
| `lastScored` | `scores.lastScoredAt` | **Rename.** Value is the timestamp |

`TradeHighlightDto` (only on `GetTraderDetailAsync`, **not** on `GetTraderAsync`):

| Highlight JSON | A93 `FirstThreeTradeDto` / `ReconstructedTradeDto` | Match? |
|---|---|---|
| `positionId` (number) | `positionTicket` (**string**) | **No** (D11) |
| `sourceSymbol` | `sourceSymbol` | Yes |
| `canonicalSymbol` | `canonicalSymbol` | Yes |
| `direction` = `Long`/`Short` | `side` = `LONG`/`SHORT` | **No** (name + token) |
| `openedAt` / `closedAt` | `openTime` / `closeTime` | **Rename** |
| `netRealizedPnl` | `netSourcePnl` | **Rename** |
| `maxVolumeLots` | `maxVolumeLots` + alias `volumeLots` | **Partial** (no initial/closed/volumeLots) |
| `completed` | `isCompleted` | **Rename** |
| `isFirstThree` | `isFirstThree` | Flag only. No `sequence`, `firstThreeRank`, `badge`, `closedAsEarlyScoreEligible`, `earlyScoreTrigger` |
| *(missing)* | `reconstructedTradeId`, `brokerId`, prices, SL/TP, dirty, deal/order counts, fees, scale/partial/avg flags | **No** |

`GetTraderAsync` contributes **zero** of the highlight contract. That is why this report is titled **detail thin**.

---

## 9. Invariants D1–D15

| ID | A93 rule | `GetTraderAsync` | Verdict |
|---|---|---|---|
| D1 | Compound `{ brokerId UUID, login }`; path `/traders/{brokerId}/{login}` | `string broker` = **code**; unversioned `/api/traders/{broker}/{login}` | **FAIL** |
| D2 | First 3 = first 3 **completed reconstructed XAUUSD** lifecycles | No trades | **FAIL** |
| D3 | Do not count orders, partials, open, non-XAU, dirty | N/A (nothing ranked) | **FAIL** (required work not done) |
| D4 | Always embed `data.firstThree` (boolean alone is non-compliant) | No block | **FAIL** |
| D5 | Trade #3 latches `EARLY_SCORE_ELIGIBLE`; never `PROVEN_PROFITABLE` | Token not emitted | **FAIL** (cannot prove the latch) |
| D6 | At N=3, state ∉ `{ LIVE, LIVE_CANDIDATE }` | Echoes score state; no guard | **UNPROVEN** (not this method’s job today) |
| D7 | Trade #3 + high score → SHADOW only | Not enforced here | **UNPROVEN** |
| D8 | Scores `[0, 100]` `decimal(5,2)` | Passthrough of persisted scores | **PASS** *if* `BaselineScorer` wrote them (A22). No scale convert. |
| D9 | `mlProbability` always `null` in v1 | Literal `null` | **PASS** |
| D10 | MFE/MAE only when A45 valid; else omit numbers + `UNAVAILABLE` | Fields absent | **FAIL** (honest null + meta required) |
| D11 | Tickets / ids JSON string; login number | Login number **PASS**. No tickets on this DTO | **PARTIAL** |
| D12 | Side `LONG` \| `SHORT` | Not on this DTO | **N/A** (fails on highlight path: `Long`) |
| D13 | Volume in lots | Not on this DTO | **N/A** (highlight has `maxVolumeLots` only) |
| D14 | Allow-list; no manager login / passwords | `TraderRowDto` is allow-list; no secrets | **PASS** |
| D15 | Same inputs → bit-identical numbers; only `generatedAt` is now | No `generatedAt`. `ShadowPnl` constant 0. PnL from live GROUP BY | **PARTIAL** |

**PASS: D8 (conditional), D9, D14.** Everything that makes the page a **detail** page fails.

---

## 10. First-3: A21/A93 vs the only code that tries

`GetTraderAsync` does not rank trades. The HTTP wrapper does, so the gap is recorded here because A93 assigned that duty to `GetTraderAsync`.

A93 §4.1 eligibility (all required):

```text
completed == true
canonicalSymbol == "XAUUSD"
closedAt IS NOT NULL
closedVolumeLots > 0
dirty == false          // missing column on persisted entity → treat false
ORDER BY closedAt ASC, openedAt ASC, reconstructedTradeId ASC
```

Domain already has the right **in-memory** helper and does **not** use it:

```60:76:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(...)
    {
        return Reconstruct(...)
            .Where(t => t.Completed && t.IsXauUsd && t.EligibleForFirstThree)
            .OrderBy(t => t.ClosedAt)
            .ThenBy(t => t.OpenedAt)
            .ToList();
    }
    public bool IsEarlyScoreEligible(...) =>
        CountCompletedXauUsdTrades(...) >= 3;
```

`EligibleForFirstThree` is cleared when the position saw `BuyCanceled` / `SellCanceled`. That flag **is not persisted** on `ReconstructedTrade`. The dashboard query cannot honor D3 dirty exclusion from EF rows.

Measured ranker (`GetTraderDetailAsync` L135–157):

```text
ORDER BY ClosedAt ?? OpenedAt
first = Completed && CanonicalSymbol == "XAUUSD" && firstThree < 3
```

| A93 | Measured ranker |
|---|---|
| Filter then take 3; dump only those in `firstThree.trades` | Dump **all** rows; flag first three that match |
| `closedAt IS NOT NULL` | Not required — completed + null close can still rank |
| `closedVolumeLots > 0` | Not checked (`ClosedVolumeLots` unused) |
| `dirty == false` | Column absent; canceled positions can keep `Completed=true` after persist |
| Sort `closedAt, openedAt, id` | `ClosedAt ?? OpenedAt` only — open rows interleave by open time |
| Exactly 3 slots, waiting cards | No slots |
| `sequence` 1..3, badges `TRADE_1..3` | Boolean |
| Rank 3: `earlyScoreTrigger = EARLY_SCORE_ELIGIBLE` | Not emitted |
| N≥4 does not steal ranks 1–3 | Holds **if** the same filter/order is stable; sort key is weaker than A93 |
| Detail GET does **not** dump hundreds of trades | Unbounded list for the login |

Worked A93 T3 (2 completed XAU + 1 partial + 1 open):

| | A93 | `GetTraderAsync` | `GetTraderDetailAsync` |
|---|---|---|---|
| `firstThree.count` | `2` | field absent | field absent |
| slot 3 | `waiting: true` | — | — |
| partial / open in `firstThree.trades` | no | — | they **appear** in `trades` with `isFirstThree=false` |
| HTTP | 200 + envelope | would 200/204 a row | 200 `{ header, trades: [all] }` |

That is not T3.

---

## 11. `GetTraderDetailAsync` — thin-plus, still not A93

Recorded so nobody treats the new method as closing D49.

```125:160:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderDetailDto?> GetTraderDetailAsync(string broker, long login, CancellationToken ct)
    {
        var header = await GetTraderAsync(broker, login, ct);
        if (header is null)
            return null;
        // ... load ALL reconstructed trades for (BrokerId, Login)
        // ... mark first three Completed && CanonicalSymbol == XAUUSD
        return new TraderDetailDto(header, highlights);
    }
```

| Fact | Detail |
|---|---|
| Depends on the thin method | Header **is** `GetTraderAsync`. Every A93 hole in §5–§9 is inherited |
| Extra I/O | +1 broker-by-code + 1 unbounded trade list |
| Broker missing after header hit | Returns `TraderDetailDto(header, [])` — odd; should not happen if header resolved `Code` |
| Type name | `TraderDetailDto` **collides** with A93. Future implementers will “already have” the type |
| Page | 8 chips + 4-column table (`Pos`, `Symbol`, `Net`, `First 3`) + footnote. Matches B20’s “chips + footnote,” plus a table |

This is a demo explorer, not Increment 6 / A93 §18.

---

## 12. Data the store already has and the thin GET ignores

Queryable today, unused by `GetTraderAsync`:

| Already on disk | A93 field it would feed |
|---|---|
| `Mt5Account.Balance/Equity/Margin/MarginFree/Profit/Leverage/LastSyncedAt` | `accountOverview.*` |
| `Mt5Group.PlanMapping` (+ currency if added) | `identity.planMapping`, `accountOverview.currency` |
| `Broker.Id`, `DisplayName` | `identity.brokerId`, `brokerDisplayName` |
| `TraderScore.BehaviorScore` | `scores.behaviorScore` |
| `TraderScoreHistory` (id, scores, state, `RecordedAt`) | `embeds.scoreHistory` / `/scores` — no `trigger`/`window` yet (A93: emit `RESCORED`) |
| `ReconstructedTrade` prices, volumes, SL/TP, scale/partial/avg, fees, deal/order counts, `Id` | almost all of `ReconstructedTradeDto` |
| `Mt5Positions` | `openXauSourcePositions` / `openSourcePositions` |
| `ShadowOrders` | shadow book (entity is an **order**, not a position — A93: zeros until `shadow_positions`) |
| `CopyIntent` / `ExecutionIntent` | `sourceDestinationPreview` (`mode=NONE`/`SHADOW`) |
| `FeatureSnapshot.MaeMfeQuality = Unavailable` | `mfe`/`mae` null + `UNAVAILABLE` (honest default) |

`GetTraderAsync` reads scores + accounts + all-completed PnL and throws the rest away. Thinness is a **projection choice**, not a missing table for the header/overview/first-3 **shape**. Dirty / lifecycle / FIRST3-freeze / `trader_states` remain real persist gaps (A93 §8).

---

## 13. React + TypeScript

| Layer | Measured | A93 §12 |
|---|---|---|
| Route | `/traders/:brokerId/:login` (param is **code**) | `/traders/:brokerId/:login` with UUID |
| Hook | `GET /api/traders/${broker}/${login}` | `GET /api/v1/traders/${brokerId}/${login}` |
| Query key | `['trader', broker, login]` | same shape — ok |
| `types/index.ts` `TraderDetail` | extends list `Trader`; `trades`, `scoreHistory`, `lotHistory`, `shadowPositions`, `livePositions` | unused (0 imports). `Trade.isFirst3`, `ticket: number` |
| Page | `data.header ?? data`; table uses `positionId` / `canonicalSymbol` / `netRealizedPnl` / `isFirstThree` | Must render `firstThree.slots[3]`, badges, waiting cards, `INSUFFICIENT_DATA` banner |
| Unused types | `StatusBadge`, `formatters` (D08) | A93 TS is **normative for `/apps/web`** and is not this file |

The stub `isFirst3` and the live `isFirstThree` are **different names**. The page talks to the C# highlight DTO via `any`. A93’s TS contract has not been adopted.

---

## 14. Acceptance tests T1–T16

A93 §16. Grep of `D:\Prop\tests` for `GetTraderAsync` / `GetTraderDetailAsync` / `TraderDetailDto` / `firstThree` / `TradeHighlight`: **0 hits**.

| ID | Expect | Status |
|---|---|---|
| T1 | Unknown key → 404, no `data` | **FAIL** (204, no body) |
| T2 | Account N=0 → 3 waiting slots, scores null, `INSUFFICIENT_DATA` | **FAIL** (no slots; no score row ⇒ miss) |
| T3 | 2 XAU + partial + open → `count=2`, slot 3 waiting | **FAIL** |
| T4 | Non-XAU only → `count=0` | **FAIL** (no `count`) |
| T5 | Dirty completed XAU excluded | **FAIL** (dirty not persisted / not filtered) |
| T6 | Third close → `EARLY_SCORE_ELIGIBLE`, no `PROVEN_PROFITABLE`, not LIVE | **FAIL** (trigger absent) |
| T7 | N=7 → still 3 highlighted; `/trades` has 7 | **FAIL** (no `/trades` sub-resource; detail dumps all) |
| T8 | `earlyScore === earlyQualityScore` | **FAIL** (`earlyQualityScore` absent) |
| T9 | `mlProbability` JSON `null` | **PASS** on the thin row |
| T10 | No ticks → mfe/mae null + `UNAVAILABLE` | **FAIL** (meta absent) |
| T11 | Alias `XAUUSDm` / `GOLD` counts if mapped | **UNPROVEN** here (recon may; detail uses persisted `CanonicalSymbol`) |
| T12 | Same book twice → bit-identical first-three ids | **UNPROVEN** (no `reconstructedTradeId` on highlight) |
| T13 | Achiever 1001 vs StarwaveFX 1001 are distinct | **PARTIAL** (code+login; not UUID+login) |
| T14 | Live flag off → live pnl 0, empty dest ids | **FAIL** (`live` object absent) |
| T15 | Sanitizer / denylist | **UNPROVEN** (allow-list DTO helps; no sanitizer) |
| T16 | High score at N=3 → not LIVE | **UNPROVEN** on this GET |

**1 / 16** (T9 only). A93 §18: do not call §51 done before T1–T16.

Demo seed rebuilds logins `10001`, `10002`, `10003`, `99001`. That is enough to **write** T3/T6/T13 later. It does not make `GetTraderAsync` pass them.

---

## 15. Stale snapshots (do not copy)

| Older claim | This measure |
|---|---|
| A93 §15: host is weatherforecast; 0 routes; `TraderDetailDto` MISSING; React page file absent | Weather route **gone** (D06). Unversioned detail GET exists. Application has a **different** `TraderDetailDto`. `TraderDetailPage.tsx` exists |
| A93 intro: `GetTraderAsync` is what the host returns | Host now returns `GetTraderDetailAsync`. `GetTraderAsync` is the header subroutine — **still** `TraderRowDto` |
| B06 / B20 / B29 / C04 / D02: HTTP = `GetTraderAsync` same as list | **Stale for the HTTP verb.** True for the **header**. D06 / D30 / D39 already noted the retarget |
| D21 / C36: `EfDashboardQueries` 168 / 7407 / `37A4DDD2…`, 7 methods | Now 205 / 8708 / `328D0924…`, **8** methods |
| A26 §6.5 scores `0.71` / side `BUY` / boolean highlight | A93 **supersedes**. Implementation matches **neither** (scores 0–100 passthrough, no highlight block, side `Long` on trades) |

A93 **contract** is not stale. A93 **gap table §15** is.

---

## 16. What must change (spec only — not done in this pass)

A93 §18 checklist, restated against the current fork:

1. **Stop treating `TraderRowDto` as detail.** Either change `GetTraderAsync` to return A93 `TraderDetailDto?` (A93 §13) **or** keep the thin method as a private leaderboard helper and stop giving it the detail name in docs. Do not leave two public `TraderDetailDto` types.
2. Put allow-list records in `apps/api/Contracts/` with A93 names (`FirstThreeBlockDto`, `TraderIdentityDto`, …). Rename or delete the Application 2-field record.
3. Keyed query: `brokers.code` or `brokers.id` + `trader_scores.login` / account / reconstructed existence (D5.1). **Do not** call `GetTradersAsync` for one login.
4. Build `FirstThreeBlock` from A21 eligibility + sort; always 3 slots; do not dump the full book on this GET.
5. Map overview / scores (including `behaviorScore`, `earlyQualityScore == earlyScore`, `version`) / flags / honest feature nulls / shadow+live summaries. `mlProbability` stays null. MFE/MAE stay null + `UNAVAILABLE` until A45 says otherwise.
6. Version the route `/api/v1/traders/{brokerId}/{login}`, envelope `{ data }`, `404` not `204`, ReadOnly+ auth.
7. Retarget React to that payload. Replace `types/index.ts`. Render three slot cards. Do not re-rank in the browser.
8. Tests T1–T16.

Non-goals (A93 §17): do not invent ticks, do not auto-LIVE, do not put first-3 dollar PnL in the ranking key, do not modify product source in this swarm file.

---

## 17. Related reports

| File | Role |
|---|---|
| `A93_trader_detail_dto.md` | Binding detail contract |
| `A92_leaderboard_dto.md` | Binding **list** row — what `GetTraderAsync` actually returns |
| `A21_reconstruction_spec.md` | First-3 eligibility |
| `A26_dashboard_api_spec.md` §6.5 | Superseded sketch |
| `A63_api_catalog.md` | Route catalog; detail still a one-liner there |
| `D06_api_census.md` / `D30_api.md` / `D39_hooks.md` | Host now calls `GetTraderDetailAsync` |
| `D21_queries.md` / `C36_query_perf.md` | Leaderboard reload cost (SHA stale; algorithm of `GetTraderAsync` unchanged) |
| `B20_web_pages_gap.md` / `B29_dto_mismatch.md` | Page/types vs §51 (page now has a table; payload still thin) |

---

## 18. Classification close

| ID | Surface | Class |
|---|---|---|
| D49-1 | `GetTraderAsync` vs A93 payload | **`MISSING`** |
| D49-2 | `GetTraderAsync` as 5k keyed lookup | **`UNSAFE`** (full `GetTradersAsync`) |
| D49-3 | A93 `FirstThreeBlock` on any GET | **`MISSING`** |
| D49-4 | Application `TraderDetailDto` name | **`EXISTS_NEEDS_REFACTOR`** (collision) |
| D49-5 | `GetTraderDetailAsync` | **`EXISTS_NEEDS_REFACTOR`** demo table; **not** A93 |
| D49-6 | `/api/v1` detail + envelope + 404 + RBAC | **`MISSING`** |
| D49-7 | T1–T16 | **`MISSING`** (1 accidental pass: T9) |
| D49-8 | Secrets on this DTO | **PASS** (allow-list; no manager login) |
| D49-9 | `mlProbability` | **PASS** (`null`) |

**§51 / A93 trader detail is not implemented.** `GetTraderAsync` is the thin leaderboard row A93 explicitly rejected. The later `GetTraderDetailAsync` wrapper does not change that fact for the method this file was asked to compare.
