# D83 — `ShadowPortfolioPage.tsx`: is it the §46 / A26 Shadow Portfolio?

| Field | Value |
|---|---|
| Agent | D83 (senior engineer; Shadow Portfolio **page** only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:57+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `ShadowPortfolioPage.tsx`. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` |
| Route | `/shadow` via `App.tsx` `path="shadow"` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) is the only write. |
| Method | Full `read_file` of the 14-line page. Cross-read `App.tsx`, `DashboardLayout.tsx`, `hooks.ts`, unused `types/index.ts`, `OverviewPage`, `TraderDetailPage`, `TradersPage`, `LiveCopyPage` (sibling stub), `Program.cs` maps, `IDashboardQueries` / `DashboardModels.cs`, `EfDashboardQueries` shadow sum, `ShadowCopyEngine`, `ShadowOrder`, `CopyIntent`, `PersistDemoShadowAsync`, `DemoSeeder` dest quote, `CopyIntentExpiry`, architecture §24 / §46 / §47 / §51 / §69.11, A24 §16.2, A26 §5.2 / §6.9 / §9, A57 item 11–12, A62, A63 §5.7 / §7.1 / §7.2, A91, A97. PowerShell SHA-256 + bytes + physical / non-blank lines + last-write. Product `*.ts`/`*.tsx`/`*.cs` grep for `useShadow`, `shadow/portfolio`, `GetShadow`, `ShadowPortfolio`. No `tsc`, no `npm`, no HTTP, no product edit. |
| Binding law | Architecture **§46** label `Shadow Portfolio`; **§24** persist list; A26 `/shadow` + `GET /api/v1/shadow/portfolio`; A63 first-useful catalog (shadow GET is **in** v1); A62 empty-safe Phase-3 page; A24 prices from cTrader **QUOTE**, not source MT5 last-deal. |
| Precedence | On-disk tree supersedes A62/B10/B22 “file missing.” Architecture §46 wins for the **label**. A26 wins for the **route** and JSON. A63 wins for whether the GET is a **§69** requirement (yes). D48 / D41 win for “are shadow rows written?” (yes, demo). This file wins for “does the React page show them?” |
| Sibling (do not treat as this file) | D16 engine; B18 dest-quote slice; D47 CopyIntent-after-SHADOW; D48 six seed rows; D71 expiry unused; C37 Live Copy chrome; C13 item 11 (stale “no writers”) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **page-contract census**. It is **not** a claim that §24 / A24 §19 / §69.11 are implemented, that dest quotes are live Pepperstone, or that Overview “Shadow P&L” is A24 `shadow_performance`.

---

## 0. Verdict

**The §46 Shadow Portfolio *page* is missing. The `/shadow` chrome is not.**

Do not answer “missing” from A62 §3 / B10 / B22. Those snapshots either predate `ShadowPortfolioPage.tsx` (13:16:43) or only counted the file. Do not answer “present” from the sidebar either. The leaf is abbreviated **`Shadow`**, the module is a **14-line static stub with zero imports**, and **none** of the A26 §6.9 / A63 §5.7 contract exists.

Unlike Live Copy (C37): A63 parks `GET /api/v1/live/portfolio` **out of v1**. `GET /api/v1/shadow/portfolio` is **in** the first-useful catalog (A57 item 11, A63 §7.1, A62 “page exists from Phase 3 as empty-safe”). Missing shadow GET is a **§69.11 / §69.12 blocker**, not an allowed empty.

The backend **now writes** demo `shadow_orders` (D48: **6** rows after `SeedAsync`, logins **10001** × 3 and **99001** × 3; Overview `ShadowPnl` would paint **248.20** = Σ `SourceVsShadowSlippage`). This page **does not fetch, table, or even mention those rows.** An operator who opens `/shadow` sees two paragraphs and an empty book — while `/overview` already lies that “Shadow P&L” is a number.

| Layer | Required | Now | Classification |
|---|---|---|---|
| §46 nav label | `Shadow Portfolio` | **`Shadow`** | **WRONG** |
| A26 route | `/shadow` | `/shadow` | **EXISTS_AND_GOOD** (route only) |
| Page module | A62 `pages/shadow/ShadowPortfolioPage.tsx` | `pages/ShadowPortfolioPage.tsx` (628 B, 14 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Hook | `useShadow` / `GET /api/v1/shadow/portfolio` | **none** (`hooks.ts` 11 queries, no shadow) | **MISSING** |
| API | A26 / A63 `GET /api/v1/shadow/portfolio` (+ positions / performance / copy-intents) | **no `MapGet`** | **MISSING** (and **in** v1 — not like Live) |
| DTO / query | `pnl`, `openCount`, `closedCount`, `positions[]` + `quoteAgeMs` | no type, no `GetShadowPortfolioAsync` | **MISSING** |
| Empty-safe table | A62: show counts + dest-quoted rows or honest `[]` | two static `<p>` + one box | **MISSING** |
| Persist behind the page | §24 `shadow_copy_order` / fill / position / pnl | thin `shadow_orders` only; D48 writes **6** demo entries | **EXISTS_NEEDS_REFACTOR** (rows) / **MISSING** (book) |
| Page copy vs writers | honest empty **or** “demo backfill, no approval” | claims orders appear **after CopyIntent is approved** | **WRONG** (see §4) |
| Mutations on this page | **none** (A26 §9) | none | **ALIGNED** |
| Live `NewOrderSingle` | off | off; page says so | **ALIGNED** |
| Invented dest rows in the UI | forbidden | none painted | **ALIGNED** (safe-by-absence **on this page**) |

**One-line:** Architecture §46 requires the Shadow Portfolio leaf; the dashboard has a policy stub. Six demo `shadow_orders` exist in the store and never reach this component.

**Direct answer to “is Shadow Portfolio missing?”**

| If the question means… | Answer |
|---|---|
| Is there no file / no route / no sidebar entry? | **No.** File + `/shadow` + abbreviated nav exist. SHA unchanged since 13:16:43. |
| Is the architecture §46 / A26 / A63 Shadow Portfolio page implemented? | **Yes — missing.** |
| Must first-useful (§69) ship this GET + empty-safe book? | **Yes.** A63 lists `/shadow/portfolio` in v1. A57 item 11 is this page. |
| Must it ship a live Pepperstone dest book? | **No.** Honest `positions: []` + dest-quote quality is the empty-safe contract. Inventing a live tape is forbidden. |
| Do seed rows make the page PASS? | **No.** D48 rows are not §24, and this file does not read them. |

This report does **not** authorize creating or rewriting product files.

---

## 1. Binding sources

| Role | Path |
|---|---|
| Architecture law (§46 main navigation) | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758 — label **`Shadow Portfolio`** |
| Why the page exists | same file **§24** (L985–1018): dest QUOTE pricing; persist `shadow_copy_order` / `shadow_copy_fill` / `shadow_position` / `shadow_pnl` / `source_vs_shadow_slippage` |
| Phase | **§ Phase 5** L2546–2556: intents, entries/exits, dest pricing, shadow P&L, source-vs-shadow |
| Adjacent widgets | **§47** Shadow count + Shadow P&L (Overview — **not** this leaf); **§51** trader-detail shadow book |
| First useful | **§69.11** shadow-copy selected traders on dest quotes; **§69.12** show it in React |
| Route + JSON | `A26_dashboard_api_spec.md` §5.2 `/shadow`; §6.9 `GET /api/v1/shadow/portfolio`; §9 mutations **none** |
| First-useful API spelling | `A63_api_catalog.md` §5.7 / §7.1 — `GET /api/v1/shadow/portfolio` **in** v1; extras `positions`, `performance`, `copy-intents`. Live GET is **out** (§7.2) |
| Empty-safe UI | `A62_react_scaffold.md` L196 / L406 / L743 — Phase 3 page; widgets = pnl, open/closed counts, positions marked with dest quote + `quoteAgeMs` |
| Item 11/12 inventory | `A57_first_useful_version.md` L436 / L430; `A91_overview_dto.md` (Overview tiles only) |
| Hub | `A97_signalr_events.md` topic `shadow.portfolio` `{ pnl, openCount }` — v1 **named**, not a must-ship topic |
| Engine / persist (not this page) | D16, B18, D47, D48, D71 |
| Prior page census | C08 / D08 / B20 / B31 / C13 / C54 — file present, static, no hook |

When documents disagree:

| Topic | A26 | A63 | This report |
|---|---|---|---|
| Extra GETs on `/shadow` | §9 additional **empty** | `positions`, `performance`, `copy-intents` | Primary GET is the gate. Extras are A63 first-useful **shadow** extras, not a second page. |
| Envelope | `{ data: { pnl, openCount, closedCount, positions[] } }` | same + `currency`, `selectedTraderCount`, nested `quote` | Do **not** ship both shapes. First-useful host work binds **A63** + A26 position row (`shadowPositionId`, dest `entryPrice`/`markPrice`/`quoteAgeMs`). |
| Live sibling | `/live/portfolio` required for full dashboard | **out of v1** | C37. Do not confuse with this leaf. |

---

## 2. What architecture §46 actually requires

### 2.1 Verbatim nav (the only §46 body)

`# 46. React Dashboard` / “Main navigation” (`…Architecture_v2.md` L1739–1758):

```text
Overview
Brokers
MT5 Groups
Traders
Trader Detail
Trade Explorer
Scoring
Models
Shadow Portfolio
Live Copy Portfolio
cTrader FIX
Risk
Reconciliation
System Health
Audit
Settings
```

**16 labels.** Binding sidebar = **15 leaves** (Trader Detail is a child route), this order, **exact strings**. A26 §5.3: *“Left nav labels **exactly** as §46.”*

§46 does **not** list widgets for Shadow. Widget/API law is A26 §6.9 + A62 §10.9 + A24 + §24.

### 2.2 Why this leaf is not Live, Overview, or Detail

| | Shadow Portfolio | Live Copy Portfolio | Overview “Shadow” tile | Trader Detail shadow |
|---|---|---|---|---|
| §46 order | after Models | after Shadow | §47 KPI | §51 embed |
| Route | `/shadow` | `/live` | `/overview` | `/traders/:brokerId/:login` |
| Tape | cTrader **QUOTE** only | QUOTE + **TRADE** `NewOrderSingle` when flagged | count of `TraderState.SHADOW` + a P&L number | per-login book |
| A26 GET | `/api/v1/shadow/portfolio` | `/api/v1/live/portfolio` | `/api/v1/overview` | `/traders/…/shadow` (A63) |
| Phase / §69 | 5 / **§69.11 must show** | 8 / A63 **out of v1** | tile only | embed |
| Current file | this stub | `LiveCopyPage` 8-line stub (C37) | `OverviewPage` paints `data.shadow` + `data.shadowPnl` | no shadow table |

`/live` + `LiveCopyPage.tsx` does **not** satisfy this row. Overview “Shadow” is a **state count**, not this page. Overview “Shadow P&L” is **Σ slippage** (D16 / D21 / D48), not A24 performance.

### 2.3 A26 §6.9 contract (binding JSON)

`GET /api/v1/shadow/portfolio` (`A26` L719–746):

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

**Hard rule (A26 L746, A24, A62 L743):** prices come from the cTrader **QUOTE** session, not source MT5 last-deal.

A26 §9: primary GET `/shadow/portfolio`; additional **none**; **mutations: none**. Promote / enable-live / flatten live on Risk/Settings, not here.

A62 empty-safe: page may exist from Phase 3 with `positions: []`. It must still paint **pnl / open / closed / dest quote age**. A 14-line policy essay is not empty-safe.

A63 first-useful empty body (when no dest book):

```json
{
  "data": {
    "pnl": 0,
    "currency": "USD",
    "openCount": 0,
    "closedCount": 0,
    "selectedTraderCount": 0,
    "quote": { "instrumentId": null, "bid": null, "ask": null, "quoteAgeMs": null, "healthy": false }
  }
}
```

That JSON **does not exist** on the host. The page would not consume it if it did.

---

## 3. What is on disk now (fresh census)

### 3.1 Page — entire module

`D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx`

| | |
|---|---|
| Bytes | **628** |
| Physical / non-blank lines | **14 / 14** |
| Written | 2026-08-18 13:16:43 |
| SHA-256 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` |
| Unchanged vs C08 / D08 / B22 | **Yes** (same SHA) |

```tsx
export default function ShadowPortfolioPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Shadow portfolio</h1>
      <p className="text-gray-400 text-sm">
        Shadow fills use the cTrader QUOTE session, not source MT5 ticks. Open/increase intents expire when stale.
        Live NewOrderSingle remains disabled.
      </p>
      <div className="border border-gray-800 rounded p-4 text-gray-300 text-sm">
        Demo seed reconstructs and scores traders. Shadow orders are created only after a CopyIntent is approved for SHADOW state.
      </div>
    </div>
  );
}
```

| Check | Measured |
|---|---|
| `import` statements | **0** |
| Hooks / `useQuery` / `client.get` | **0** |
| `useShadow` / `useCopyIntents` | **0** (symbol does not exist in `hooks.ts`) |
| Tables / `MetricCard` / lists | **0** |
| Loading / error / empty-table states | **0** |
| `JSON.stringify` | **0** (does not dump payload — good; also does not read one) |
| `<input>` / `<form>` / buttons | **0** |
| SignalR `onEvent` | **0** |
| `formatters.ts` / `StatusBadge` / `recharts` | **0** (those modules are unused repo-wide) |

| A26 / A62 widget | Present? |
|---|---|
| H1 (any) | yes — sentence case `Shadow portfolio`, not the §46 string |
| `pnl` / `openCount` / `closedCount` | **no** |
| Positions table (empty-safe) | **no** |
| `shadowPositionId` / dest `entryPrice` / `markPrice` / `quoteAgeMs` | **no** |
| Dest bid/ask / quote health | **no** |
| `GET /api/v1/shadow/portfolio` | **no fetch** |
| Additional `GET /shadow/positions` / `/performance` / `/copy-intents` | **no** |
| Enable / flatten / send buttons | **no** (correct) |
| Invented rows painted | **no** (correct honesty **on this surface**) |

`pages/shadow/` **does not exist**. No `routes/paths.ts`. No `src/api/shadow.ts`.

### 3.2 Router — `App.tsx`

SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` · 2062 B · 42 lines · 13:20:38.

- Import: `import ShadowPortfolioPage from './pages/ShadowPortfolioPage';` (line 10)
- Route: `<Route path="shadow" element={<ShadowPortfolioPage />} />` (line 31)
- **No** `/models`. **No** `/login`. No catch-all → `/overview`.

C08 / D08 / D38 already proved 15/15 imports match `pages/`. Shadow is one of those 15. File-existence is **closed**.

### 3.3 Sidebar — `DashboardLayout.tsx`

SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` · 1854 B · 44 lines · 13:20:38.

```text
… Scoring /scoring
  Shadow   /shadow          ← not “Shadow Portfolio”
  Live     /live            ← not “Live Copy Portfolio”
  FIX      /fix             ← not “cTrader FIX”
…
```

**14** `NavLink`s. Shadow is item 7 (`nav` index 6). Label fails exact-label law. No header strip (A26 §5.3). `startConnection()` still dials `/hubs/dashboard` (D50: API has **no** `MapHub`).

### 3.4 Hooks / types

`hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` · 1935 B · 53 lines · 13:16:00.

Exports: `useOverview`, `useBrokers`, `useGroups`, `useTraders`, `useTraderDetail`, `useTrades`, `useFixSessions`, `useRiskStatus`, `useReconciliation`, `useHealth`, `useSettings`.

**No** `useShadow` / `useShadowPortfolio` / `useCopyIntents` / `useShadowPositions`. D39: pages with **no** hook = Shadow, Live, Audit.

`types/index.ts` SHA-256 `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` · **0 imports** from any page (D08).

- `Overview.shadowPnl` exists as a **client fiction** (`totalBrokers` / `tradersByState` — not the live `OverviewDto`).
- `TraderDetail.shadowPositions: Position[]` exists as a **client fiction** (`ticket` / `lots` / `currentPrice`) — **not** A26 shadow rows (`shadowPositionId` + `quoteAgeMs`).
- **No** `ShadowPortfolio` / `ShadowPosition` type matching A26 §6.9.
- `TraderDetailPage` never reads `shadowPositions`. The field is dead.

### 3.5 Product grep (apps + src, exclude bin/obj/node_modules)

| Pattern | Product hits |
|---|---|
| `useShadow` | **0** |
| `shadow/portfolio` | **0** |
| `GetShadowPortfolio` / `GetShadowAsync` | **0** |
| `ShadowPortfolio` (C# / TS type) | **0** (only the React function name + import) |
| `ShadowPortfolioPage` | `App.tsx` L10/L31 + this file |

Nearby names that **must not** be confused with the page:

| Symbol | Where | What it is |
|---|---|---|
| `OverviewDto.Shadow` / `ShadowPnl` | `DashboardModels.cs` | state **count** + Σ slippage |
| `TraderRowDto.ShadowPnl` | same | **hardcoded `0`** in `GetTradersAsync` (EfDashboardQueries L106) |
| `TraderState.SHADOW` | Domain enum + scorer | selection state, not a dest book |
| `ShadowCopyEngine` | `Domain\Shadow\` | taker-touch calculator (D16); **now constructed** by `PersistDemoShadowAsync` |
| `ShadowOrder` / `shadow_orders` | entity + EF | thin fill-shaped row; D48 writes **6** |
| `CopyIntent` `Status = "SHADOW_ONLY"` | store writer | **not** A26 `APPROVED` / `SHADOWED` |
| `OutboxEventType.ShadowCopyIntent = 2` | enum | **not** stamped; writer uses `ScoreUpdate` (D47 / D48) |

---

## 4. Sentence-by-sentence: what the stub claims vs measured product

The page has **four** factual claims. They are the only “product” this file ships.

| # | On-page text | Law | Measured | Honest? |
|---|---|---|---|---|
| 1 | “Shadow fills use the cTrader QUOTE session, not source MT5 ticks.” | A24 §1.4 / A26 L746 / C60 / D56 | `SimulateEntry` takes a `DestinationQuote` bid/ask (not source last-deal). Demo fill uses **seeded** `DestinationQuoteSnapshot` (`Bid=2399.45`, `Ask=2399.85`, `VenueInstrumentId=null`). `/api/health` FIX: `healthy=false`, `"no live TLS socket"`. D41 item 9 **FAIL**. | **Policy: correct. Live QUOTE session: false.** Source ticks are correctly **not** the fill. Calling the seed book “the cTrader QUOTE session” overclaims §69.9. |
| 2 | “Open/increase intents expire when stale.” | A24 §6 / §63 / A73; `CopyIntentExpiry` | Writer sets `ExpiresAt = trade.OpenedAt.AddSeconds(15)` then **fills immediately**. D48: those timestamps are **June 2026** — already stale at seed time. D71: `CopyIntentExpiry` has **0** product callers; **0** readers of `ExpiresAt`. Engine has no expiry. | **FALSE as behavior.** A write-only `ExpiresAt` is decoration. |
| 3 | “Live NewOrderSingle remains disabled.” | §41 / A101 / C37 / D43 | `GET /api/settings` `REAL_COPY_EXECUTION_ENABLED=false`. No `35=D` builder. `CanPromoteToLive` hard-false. Live page restates the flag. | **TRUE.** Keep. |
| 4 | “Demo seed reconstructs and scores traders.” | Demo path | `DemoSeeder` → Fake tape → `RebuildTraderAsync` → `BaselineScorer`. | **TRUE.** |
| 5 | “Shadow orders are created only after a CopyIntent is **approved** for SHADOW state.” | A24: persist intent → Risk → then fill | `PersistDemoShadowAsync` (L251–337): if `state == SHADOW` **and** a dest-quote **row** exists → `new CopyIntent { Status = "SHADOW_ONLY" }` → `SimulateEntry` → `ShadowOrders.Add` **in the same SaveChanges**. No `RiskEngine`. No `APPROVED`. A26 `copyIntentStatus` has no `SHADOW_ONLY`. Gate is `TraderState.SHADOW`, not intent approval. | **FALSE.** C13/C54/D16 “no writers” is **stale**. The current writer is a **demo backfill**, not an approval workflow. The page still describes the workflow that was never built. |

Claim 5 is the one that will mislead an operator the most: they will wait for an approval UI that does not exist, while six `SHADOW_ONLY` rows already sit in `shadow_orders`.

---

## 5. API / Application / persistence (no shadow BFF)

### 5.1 `apps/api/Program.cs`

SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` · 4731 B · 95 lines · 13:35:15.

Anonymous maps (unversioned):

```text
GET  /health
GET  /api/health
GET  /api/risk/status
GET  /api/reconciliation/status
GET  /api/settings          ← REAL_COPY_EXECUTION_ENABLED = false
GET  /ready
GET  /api/overview          ← Shadow count + ShadowPnl (slippage sum)
GET  /api/brokers
GET  /api/groups
GET  /api/traders
GET  /api/traders/{broker}/{login}
GET  /api/fix/sessions
GET  /api/risk
GET  /api/trades
POST /api/ops/resync
```

**Zero** of: `/api/v1/shadow/portfolio`, `/api/shadow/portfolio`, `/api/v1/shadow/positions`, `/api/v1/shadow/performance`, `/api/v1/copy-intents`, `/api/v1/traders/{broker}/{login}/shadow`.

`GET /api/overview` is the only JSON that names shadow. `ShadowPortfolioPage` does not call it.

### 5.2 `IDashboardQueries`

`DashboardModels.cs` SHA-256 `A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` · 3088 B · 114 lines · 13:34:59.

Eight methods (7 listed + `GetTraderDetailAsync`): Overview, Brokers, Groups, Traders, Trader, TraderDetail, FIX, Risk. **No** `GetShadowPortfolioAsync`. **No** `ShadowPortfolioDto`. **No** `ShadowPositionDto`.

`OverviewDto.Shadow` = count of `TraderState.SHADOW`. `OverviewDto.ShadowPnl` = `Sum(SourceVsShadowSlippage)` (EfDashboardQueries L21). `TraderRowDto.ShadowPnl` is constructed as **`0`** (L106).

D21’s SHA for this file (`7A69C0E7…`, 97 lines) is **stale**. Shape of the hole is unchanged: still no shadow query.

### 5.3 What the store actually writes (so the page’s emptiness is not “no data”)

`EfTradingStore.PersistDemoShadowAsync` SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36` · 12097 B · 13:35:59.

Called from `ReconstructionScoringService.RebuildTraderAsync` **after** `UpsertScoreAsync` (D47: yes, after score `SHADOW`).

D48 InMemory `SeedAsync` (`_tmp_d48_shadow/stdout.txt`):

```text
EXPECTED_SHADOW_ORDERS=6
EXPECTED_COPY_INTENTS=6
EXPECTED_SLIP_SUM=248.20
DASH_SHADOW_COUNT=2 DASH_SHADOW_PNL=248.20
VERDICT=YES_SIX_SHADOW_ROWS_VIA_REBUILD
```

| Login | State | Shadow rows | Notes |
|---|---|---:|---|
| 10001 ACHIEVER | `SHADOW` | 3 | dest ask 2399.85 (long) / bid 2399.45 (short); delay 80 ms → **no** 0.05 overlay |
| 10002 ACHIEVER | `RISK_BLOCKED` | 0 | outbox only |
| 10003 ACHIEVER | `INSUFFICIENT_DATA` | 0 | outbox only |
| 99001 STARWAVEFX | `SHADOW` | 3 | all long @ 2399.85 |

Those six rows are **entry-only**, priced off an invented dest book, `SimulateExit` never called, `RiskEngine` unused, intents already expired on the clock. They are **still more data than this page shows.**

`TraderDbContext` maps `shadow_orders` PK-only and `copy_intents` + unique `IdempotencyKey`. **No** `shadow_fills` / `shadow_positions` / `shadow_pnl` / `shadow_performance` tables (§24 / A20). A fill cannot FK `fill_quote_id`.

### 5.4 Engine (why dest-quote authority is still not this page’s to claim)

`ShadowCopyEngine.cs` SHA-256 `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9` — **unchanged** vs D16 / B18.

On the demo path `modeledDelay = 80ms` so F1’s ±0.05 overlay does **not** fire (D48 `overlay=NO`). Long entry = dest ask. That is the one correct dest-touch sentence. The page does not display `Price`, `Spread`, or `QuoteAge`. `QuoteAge` is **dropped** on persist (`ShadowOrder` has no age column). Even a future hook on `shadow_orders` cannot reconstruct `quoteAgeMs`.

---

## 6. Adjacent §46 / §47 / §51 surfaces (same missing book)

Recorded so “Shadow Portfolio missing?” is not reduced to one filename.

| Surface | Architecture | Now |
|---|---|---|
| Overview “Shadow” count | §47 | painted; = traders in `TraderState.SHADOW` (**2** after seed) |
| Overview “Shadow P&L” | §47 / A91 | painted; query **Σ slippage = 248.20**, **not** dest MTM P&L (D16 F, D48) |
| Leaderboard `shadowPnl` | A92 | DTO field exists; query **hardcodes 0**; `TradersPage` **does not render** it |
| Trader Detail shadow book | §51 / A93 | `TraderDetailPage` is score chips + reconstructed-trade table (updated 13:35:59). **No** shadow rows. TS `shadowPositions` unused. |
| Header strip | A26 §5.3 | **absent** from `DashboardLayout` |
| SignalR `shadow.portfolio` | A97 | topic named; hub itself **MISSING** (D50). Page never `onEvent`s. |
| `quote.xauusd` hub | A26 / A97 | would feed FIX / Risk / **Shadow**; no hub, no subscriber |

These do **not** replace the `/shadow` page. Painting Overview’s slippage sum as “Shadow P&L” while `/shadow` is a paragraph is the current operator-visible lie.

---

## 7. §46 × A26 × disk (this leaf only)

| Check | Required | Measured | Status |
|---|---|---|---|
| Sidebar leaf exists | yes | yes (`/shadow`) | **PRESENT** |
| Exact label | `Shadow Portfolio` | `Shadow` | **FAIL** |
| Order | after Models (or after Scoring while Models absent), before Live | yes (after Scoring, before Live) | **PASS** (Models leaf missing by C39) |
| Route | `/shadow` | `/shadow` | **PASS** |
| A62 path | `pages/shadow/ShadowPortfolioPage.tsx` | `pages/ShadowPortfolioPage.tsx` | **WRONG** name / folder |
| Empty-safe table | pnl + open/closed + dest-quoted rows or `[]` | two sentences + a box | **FAIL** |
| Dest quote on each row | `quoteAgeMs` + dest px | **no** | **FAIL** |
| Versioned GET | `/api/v1/shadow/portfolio` | none | **FAIL** |
| Hook | `useShadow` | none | **FAIL** |
| Page mutations | none | none | **PASS** |
| Fake dest rows **in UI** | forbidden | none | **PASS** |
| Copy matches writers | honest empty **or** demo-backfill | “approved CopyIntent” | **FAIL** |
| Header five chips | A26 §5.3 | none | **FAIL** (shell) |
| First-useful API | A63 **in** v1 | no GET | **§69 BLOCKER** |
| Live NOS | off | off | **PASS** |

**Score for “is the §46 page here?”**

- Chrome **2/4** (route + order). Label fail. Module-path fail.
- Contract **0/7** (GET, DTO, hook, table, counts, `quoteAgeMs`, dest mark).
- Honesty **2/4** (no UI fake rows, no page mutations; copy lies on approval + expiry).
- First-useful **0/1** (A63 GET missing).

Do **not** round chrome into “Shadow Portfolio exists.”

---

## 8. First-useful vs later (so a coding wave does not over-build)

| Surface | §69 / A63 first useful | §46 / A26 full |
|---|---|---|
| Shadow nav | **required** (A57 item 11–12) | **required** exact label |
| Empty-safe UI (counts + empty table + dest-quote health) | **required** | required |
| `GET /api/v1/shadow/portfolio` | **in v1** | required |
| `GET /shadow/positions` / `/performance` / `/copy-intents` | A63 extras | A26 §9 extra empty — pick A63 if implementing v1 |
| Six demo `SHADOW_ONLY` rows | may be listed **if** labeled demo + dest-quote quality + **not** called P&L | must not be sold as Phase 5 sample (C14 G14) |
| Dest QUOTE TLS + discovered instrument | §69.9–10 (separate items; still FAIL) | required for a **true** dest book |
| `NewOrderSingle` | **forbidden** | §70 |
| Overview / Detail shadow widgets | dest P&L may be `null`/unpriced; do not invent | same honesty |

A30 increment 6 is **not** a license to omit this leaf. It **is** a license to keep Live empty. Shadow is the Phase-5 / §69 surface.

Current stub’s *intent* (QUOTE not source ticks; NOS off) is directionally right. It still fails exact-label law, does not consume a contract DTO, and **misstates** how demo rows are created.

---

## 9. Finding list (this file + the hole it sits in)

Severity: **BLOCK** = A26/A63/§69 acceptance fail if this shipped as Shadow Portfolio. **HIGH** = operator-visible lie. **MED** = chrome / audit hole.

### F1 — BLOCK — No GET, no hook, no DTO

A63 first-useful `GET /api/v1/shadow/portfolio` is unmapped. `IDashboardQueries` has no shadow method. `hooks.ts` has no `useShadow`. The page cannot become API-backed without new code on **three** layers. This is the §69.11/12 miss.

### F2 — BLOCK — Empty-safe book UI is absent while demo rows exist

A62: Phase 3 page must still show pnl / open / closed / `positions[]`. Measured: static copy. D48: **6** `shadow_orders` after seed. The operator cannot see them on the page whose job is to show them. Overview’s “Shadow P&L” **248.20** is the only painted number, and it is the wrong grain.

### F3 — HIGH — Page copy invents an approval workflow

“Shadow orders are created only after a CopyIntent is approved for SHADOW state” is **false** on the only writer (`Status = "SHADOW_ONLY"`, same transaction as the fill, no Risk). C13’s “no CopyIntent pipeline writes them” is **also** false now. Both sentences must die together.

### F4 — HIGH — “Intents expire when stale” is not enforced

`CopyIntentExpiry` unused (D71). `ExpiresAt` written, never read. Demo fills use source `OpenedAt+15s` that is months stale at seed time and still persist. The page states the law as if it were a gate.

### F5 — HIGH — “cTrader QUOTE session” overclaim

Dest-touch **type** is correct (not source ticks). The book is a seeder snapshot with `VenueInstrumentId = null` and no TLS (D41 items 9–10 FAIL). Keep “not source MT5 ticks.” Do not claim a live QUOTE session until one exists.

### F6 — MED — Exact-label law

Sidebar `Shadow` ≠ `Shadow Portfolio`. H1 `Shadow portfolio` ≠ §46. A26 §5.3.

### F7 — MED — Dead client types invite a second contract

Unused `TraderDetail.shadowPositions: Position[]` (`ticket`/`lots`) is **not** A26. A later wave must not bind that fiction.

### F8 — MED — No SignalR, wrong hub name

A97 `shadow.portfolio` / `quote.xauusd` never subscribed. Layout dials `/hubs/dashboard`; A26 is `/hubs/ops`; D50: no hub. Polling GET is enough for v1; do not block the page on SignalR.

### F9 — MED — Persist cannot support the DTO even if the page fetched `shadow_orders`

No `shadowPositionId` book, no mark, no `quoteAgeMs` column, no `fill_quote_id`, no open/closed status, qty is source lots echoed (D16 F9). A truthful first GET today is A63’s **empty** quote-unhealthy body **or** a list explicitly labeled “demo entry-only, quality unverified” — not a silent map of the six rows into A26 `unrealizedPnl`.

---

## 10. What earlier reports claimed vs this pass

| Claim | When | D83 (this pass) |
|---|---|---|
| A62 §3 / B10 / B22: Shadow file missing or pages empty | morning / 13-file snapshot | **Stale.** File + `/shadow` exist since 13:16:43. |
| C08 / D08 / B20 / B31: static, no hook | 13:16–13:33 | **Still true.** Same SHA `608C8C2D…`. |
| C13 item 11: “no CopyIntent pipeline writes them” | earlier C wave | **Stale on writers.** D47/D48: writer exists. **Still true on the page** (it shows nothing). |
| D16: “Web page is a static paragraph” + “no product writer” | D16 engine review | Paragraph **still true**. Writer claim **stale**. |
| D41 item 11 DEMO / item 12 PARTIAL | current FUV | **Confirmed.** This page is why 12 cannot show 11. |
| A63: shadow GET in v1; live GET out | spec | **Still true.** Do not treat missing shadow GET as “allowed like Live.” |
| C37: `/shadow` does not satisfy Live | Live-only pin | **Confirmed the converse:** `/live` does not satisfy Shadow. |

Use **this file** for “is Shadow Portfolio missing vs §46 / A26 / A63?”. Do not recreate the module from B22’s list.

---

## 11. What a later wave must not do

1. Do **not** treat `ShadowPortfolioPage.tsx` as absent. Do not create a second `pages/shadow/ShadowPortfolioPage.tsx` beside it without a single-module coding task.
2. Do **not** treat `/live`, Overview “Shadow” / “Shadow P&L”, or Detail chips as this leaf.
3. Do **not** invent dest P&L, `markPrice`, or live `quoteAgeMs` from source last-deal or from `SourceVsShadowSlippage`.
4. Do **not** bind Overview’s **248.20** slippage sum as A26 `pnl`.
5. Do **not** silently map the six `SHADOW_ONLY` rows into A26 `positions` without quality / demo labeling / dest-quote id.
6. Do **not** enable `REAL_COPY_EXECUTION_ENABLED` or emit `NewOrderSingle` to “fill” this page.
7. Do **not** add page-level promote / flatten / approve buttons (A26: mutations **none** on `/shadow`).
8. Do **not** leave the sidebar as `Shadow` if the task is “§46 exact nav.”
9. Do **not** add a second frontend or a `pages/` tree under `D:\Prop\src`.
10. Do **not** use unused `types/index.ts` `shadowPositions` as the contract.
11. Do **not** claim C13/D16 “zero writers” or C54 “has never written a shadow fill.” Cite D47/D48.
12. Do **not** treat SignalR as a v1 gate (D50). Ship the GET first.
13. This report does **not** authorize product edits.

---

## 12. Direct answers (copy-out)

### Is `ShadowPortfolioPage` the architecture §46 Shadow Portfolio?

**No — as the §46 / A26 / A63 page. Yes — as a routed stub.**

| | Nav leaf | Exact §46 label | Route | Page module | Data contract | Demo rows visible? |
|---|---|---|---|---|---|---|
| **Shadow Portfolio** | Present as **`Shadow`** | **No** | `/shadow` present | 628 B / 14-line stub | **Missing** `GET /api/v1/shadow/portfolio` | **No** (6 rows in store; 0 on page) |

- **File missing?** **No.** `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` SHA-256 `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276`.
- **A62 module missing?** **Yes** (`pages/shadow/ShadowPortfolioPage.tsx`).
- **Working dest-quoted book missing?** **Yes.** Engine + demo writer ≠ page.
- **§69 blocker?** **Yes** (A63 in v1; A57 item 11). Contrast Live: **not** a §69 blocker (C37).
- **Does the page send FIX or show secrets?** **No.**

### Did this pass change product source?

**No.**

---

## Evidence pins

- Architecture §46: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1735–1758.
- §24 persist list: same file L985–1018. Phase 5: L2546–2556. §69.11–12.
- A26 route + contract: `A26_dashboard_api_spec.md` §5.2 L298, §6.9 L719–746, §9 L1209.
- A62 target + widgets: `A62_react_scaffold.md` L196, L406, L743.
- A63 in v1: `A63_api_catalog.md` §5.7 L631–658, §7.1 L982–985, §8 L1033. Live **out**: §7.2 L1013.
- A57 item 11 UI: `A57_first_useful_version.md` L436.
- Stub: `D:\Prop\apps\web\src\pages\ShadowPortfolioPage.tsx` — 628 B, 14 lines, SHA-256 `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276`, 2026-08-18T13:16:43.
- Router: `D:\Prop\apps\web\src\App.tsx` L10, L31. SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` L12 `{ to: '/shadow', label: 'Shadow' }`. SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 queries, none shadow. SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`.
- API: `D:\Prop\apps\api\Program.cs` — 15 maps; no `/shadow*`. SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E`.
- Queries: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` — no shadow method. SHA-256 `A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496`.
- Overview lie grain: `EfDashboardQueries.cs` L21 `Sum(SourceVsShadowSlippage)`. SHA-256 `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B6`.
- Writer: `EfTradingStore.cs` L251–337. SHA-256 `DC03BBE6897F257005BF8583A7050D6771C2CF34D01F0C5F1B49098CB0555C36`.
- Engine: `ShadowCopyEngine.cs` SHA-256 `F41578F95EBAE3E6CC4424536C26DFA9ADEFB0625A9B846266688DF0A6F898A9`.
- Seed rows: `D48_shadow_rows.md` + `_tmp_d48_shadow/stdout.txt` (`SEED1_SHADOW_ORDERS=6`, `DASH_SHADOW_PNL=248.20`).
- Intent-after-SHADOW: `D47_copyintent.md`. Expiry unused: `D71_expire.md`.
- Live sibling: `C37_live_copy_page.md`. Census: `C08_web_pages_review.md`, `D08_web_census.md`. FUV: `D41_fuv_now.md`. Hub: `D50_signalr.md`.

---

## One-line close

**`/shadow` is a 14-line policy stub (SHA `608C8C2D…`); label is `Shadow` not `Shadow Portfolio`; no hook and no `GET /api/v1/shadow/portfolio` (a §69 in-v1 gap); six demo `SHADOW_ONLY` rows exist and are invisible here; copy about “approved CopyIntent” and “expire when stale” is false; NOS-off and “not source ticks” are the only true sentences; product source not touched.**
