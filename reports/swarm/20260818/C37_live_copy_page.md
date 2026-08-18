# C37 — Architecture §46 Live Copy Portfolio: missing?

| Field | Value |
|---|---|
| Agent | C37 (senior engineer, Live Copy Portfolio page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 13:22:04+ (API `Program.cs`); web chrome 13:20:38 (`App.tsx`, `DashboardLayout.tsx`, `LiveCopyPage.tsx`) |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Question | Architecture **§46** lists **Live Copy Portfolio**. Is it missing? |
| Product source modified | **No.** This report (plus index/log pins) is the only write. |
| Method | Read architecture §46 + §§32–35 / §41 / §44 / §51 / §69–70; A26 §5.2–§5.3 / §6.10 / §9; A30 increment 6; A62 / A63; A91–A93 / A97; re-hash current `LiveCopyPage.tsx`, router, nav, hooks, API, `IDashboardQueries`, EF, domain entities. Product `*.cs`/`*.ts`/`*.tsx` grep for `live/portfolio`, `LiveCopyPortfolio`, `useLive`, `GetLivePortfolio`. |
| Precedence | Architecture **§46 labels** win. A26 paths + page contract win. A63 decides first-useful **API**. A30/A52/A100/A101 forbid a **working** live book. On-disk tree supersedes A62 “Live Copy Portfolio missing” and B10/B22 “absent file”. |

**Not this report:** Models (B31/C36), Audit (C38), Shadow depth (B18), FIX send (C07), enabling `REAL_COPY_EXECUTION_ENABLED`.

---

## 0. Verdict

**The §46 Live Copy Portfolio *page* is missing. The `/live` chrome is not.**

Do not answer “missing” from A62 §3 / B10 / B22. Those snapshots predate `LiveCopyPage.tsx` (13:20:38). Do not answer “present” from the sidebar either. The leaf is abbreviated **`Live`**, the module is an 8-line static stub, and **none** of the A26 §6.10 contract exists.

| Layer | Required | Now | Classification |
|---|---|---|---|
| §46 nav label | `Live Copy Portfolio` | **`Live`** | **WRONG** |
| A26 route | `/live` | `/live` | **PRESENT** |
| Page module | A62 `pages/live/LiveCopyPortfolioPage.tsx` | `pages/LiveCopyPage.tsx` (321 B, 8 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Hook | `useLive` / `GET /api/v1/live/portfolio` | **none** | **MISSING** |
| API | A26 `GET /api/v1/live/portfolio` | **no map** | **MISSING** (A63: **out of v1** — absence is allowed for §69) |
| DTO / query | `realCopyExecutionEnabled`, `pnl`, `openCount`, `positions[]` | no type, no `GetLivePortfolioAsync` | **MISSING** |
| Book tables | §44 `destination_positions`, `fix_orders`, `source_destination_links` | **no entities** | **MISSING** (correct until Phase 8) |
| Mutations on this page | **none** (A26 §9) | none | **ALIGNED** |
| Live `NewOrderSingle` | off until §68/§70 | off (`SAFE_BY_ABSENCE`) | **ALIGNED** |

**One-line:** Architecture §46 requires the Live Copy Portfolio leaf; the dashboard has a stub route, not the page.

**Direct answer to “Missing?”**

| If the question means… | Answer |
|---|---|
| Is there no file / no route / no sidebar entry? | **No.** File + `/live` + abbreviated nav exist. |
| Is the architecture §46 / A26 Live Copy Portfolio page implemented? | **Yes — missing.** |
| Must first-useful (§69) ship a working live book? | **No.** A63 parks `GET /api/v1/live/portfolio`. Empty-safe chrome is still the A26/A62 read story. |

This report does **not** authorize creating or rewriting product files.

---

## 1. Binding sources

| Role | Path |
|---|---|
| Architecture law (§46 main navigation) | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758 |
| Live copy pipeline (why the page exists) | same file **§32–§35** (intent → persist → risk → FIX → ER → dest position → reconcile); **§41** flag; **§44** dest tables |
| Adjacent widgets | **§47** “Live copied” + dest real P&L; **§50** “Live allocation”; **§51** “Live copied positions / Live P&L” |
| First useful vs live send | **§69** (React of I1–I11; **no** live copy); **§70** (14 live-FIX items, still flagged) |
| Route + exact-label law + page GET | `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2–§5.3, §6.10, §9 |
| Target module + empty-safe rule | `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` (`/live` → `LiveCopyPortfolioPage.tsx`; always show the flag; `positions` may be `[]`) |
| First-useful API | `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` §7.2 — `GET /api/v1/live/portfolio` **out of v1** |
| Increment 6 policy | `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md` — *“Do not add Models, Live Copy Portfolio as working pages. Nav entries may exist as ‘not in v1’.”* |
| Prior nav census (three-item) | `B31_nav_gaps.md` — Live = shell only. This file **narrows** to Live Copy only and re-measures. |
| Stale “file absent” | A62 §3; B10 §5; B22 §5. **Superseded for file existence** by C08 / this file. |

When documents disagree: **§46 label wins**. A26 path + JSON win. A63 decides whether the **API** must exist for §69. A30 forbids a **working** book, not an honest empty leaf.

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

**16 labels.** A62 resolves Trader Detail to `/traders/:brokerId/:login` (not a dead sidebar leaf). Binding sidebar = **15 leaves**, this order, **exact strings**. A26 §5.3: *“Left nav labels **exactly** as §46.”*

§46 does **not** list widgets for Live Copy. The page is still a first-class leaf. Widget/API law is A26 §6.10 + §§32–35 + §41.

### 2.2 Why this leaf is not Shadow

| | Shadow Portfolio | Live Copy Portfolio |
|---|---|---|
| §46 order | after Models | after Shadow |
| Route | `/shadow` | `/live` |
| Tape | cTrader **QUOTE** only | QUOTE + **TRADE** `NewOrderSingle` when flagged |
| A26 GET | `/api/v1/shadow/portfolio` | `/api/v1/live/portfolio` |
| Extra fields | shadow position ids, `quoteAgeMs` | `realCopyExecutionEnabled`, `destinationPositionId`, `copyIntentId`, `executionIntentId`, `clOrdId`, `state` |
| Phase | 5 / §69.11 | 8 / §70 |

`/shadow` + `ShadowPortfolioPage.tsx` (also a static stub) does **not** satisfy this row. Overview “Live candidates” is a **state count**, not this page.

### 2.3 Pipeline the page is supposed to project (§32–§35)

```text
Source MT5 event → CopyIntent (persist) → RiskEngine → ExecutionIntent (persist)
  → FIX worker → NewOrderSingle → ExecutionReport(s) → destination position → Reconcile
```

Never send FIX from an MT5 callback. Unique `cl_ord_id` persisted **before** send. Unknown send → `EXECUTION_STATE_UNKNOWN`, then recon — not a blind retry. Mapping is explicit: reconstructed source trade → dest orders → dest cTrader position ID(s).

§41 default: `REAL_COPY_EXECUTION_ENABLED=false`. Connect / quotes / request orders-positions is allowed; **new** `NewOrderSingle` is not.

### 2.4 A26 §6.10 contract (binding JSON)

`GET /api/v1/live/portfolio` — same envelope as shadow, **plus**:

```json
{
  "data": {
    "realCopyExecutionEnabled": false,
    "pnl": 0.00,
    "openCount": 0,
    "positions": [
      {
        "destinationPositionId": "123456789",
        "copyIntentId": "…",
        "executionIntentId": "…",
        "clOrdId": "TI-20260818-000123",
        "brokerId": "…",
        "login": 6100421,
        "canonicalSymbol": "XAUUSD",
        "side": "BUY",
        "quantity": 0.05,
        "entryPrice": 2398.40,
        "unrealizedPnl": 4.10,
        "state": "FILLED"
      }
    ]
  }
}
```

**Hard rule (A26 L780):** when `REAL_COPY_EXECUTION_ENABLED=false`, `positions` **may** be `[]`; the response **must still return the flag**.

A26 §9 page matrix: primary GET `/live/portfolio`; additional `/copy-intents`; **mutations: none**. Enable-live and flatten live on Risk/Settings, not here.

A26 §5.3 header strip (every page, including this one): `REAL_COPY_EXECUTION_ENABLED`, `STOP_NEW_EXECUTION`, FIX QUOTE, FIX TRADE, MT5 ingest.

A62: *“Always show `realCopyExecutionEnabled`; positions may be `[]`.”* Target file: `pages/live/LiveCopyPortfolioPage.tsx`.

A97 v1 hub topics include `shadow.portfolio`. **No** `live.portfolio` topic in the v1 must-ship list (correct — execution off).

---

## 3. What is on disk now (fresh census)

### 3.1 Page — entire module

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`

| | |
|---|---|
| Bytes | **321** |
| Physical / non-blank lines | **8 / 8** |
| Written | 2026-08-18 13:20:38 |
| SHA-256 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |

```tsx
export default function LiveCopyPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Live copy portfolio</h1>
      <p className="text-amber-300 text-sm">REAL_COPY_EXECUTION_ENABLED is false. This page will stay empty until go-live gates pass.</p>
    </div>
  );
}
```

| A26 / A62 widget | Present? |
|---|---|
| H1 (any) | yes — sentence case, not the §46 string |
| `realCopyExecutionEnabled` from API | **no** — string literal |
| `pnl` / `openCount` | **no** |
| Positions table (empty-safe) | **no** |
| `destinationPositionId` / `copyIntentId` / `executionIntentId` / `clOrdId` / `state` | **no** |
| `GET /api/v1/live/portfolio` | **no fetch** |
| Additional `GET /copy-intents` | **no** |
| Enable / flatten / send buttons | **no** (correct) |
| Invented rows | **no** (correct honesty) |

`pages/live/` **does not exist**. No `LiveCopyPortfolioPage.tsx`. No `routes/paths.ts`.

### 3.2 Router — `App.tsx`

SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` · 2062 B · 42 lines · 13:20:38.

- Import: `import LiveCopyPage from './pages/LiveCopyPage';` (line 11)
- Route: `<Route path="live" element={<LiveCopyPage />} />` (line 32)
- **No** `/models`. **No** `/login`. No catch-all → `/overview`.

C08 already proved 15/15 imports match `pages/`. Live is one of those 15. File-existence is **closed**.

### 3.3 Sidebar — `DashboardLayout.tsx`

SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` · 1854 B · 44 lines · 13:20:38.

```text
… Scoring /scoring
  Shadow   /shadow          ← not “Shadow Portfolio”
  Live     /live            ← not “Live Copy Portfolio”
  FIX      /fix             ← not “cTrader FIX”
…
```

**14** `NavLink`s. Live is item 8. Label fails exact-label law. No header strip. SignalR `startConnection()` still dials `/hubs/dashboard` (C28: API has **no** hub).

### 3.4 Hooks / types

`hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` · 1935 B · 53 lines · 13:16:00.

Exports: `useOverview`, `useBrokers`, `useGroups`, `useTraders`, `useTraderDetail`, `useTrades`, `useFixSessions`, `useRiskStatus`, `useReconciliation`, `useHealth`, `useSettings`. **No** `useLive` / `useLivePortfolio` / `useCopyIntents`.

`types/index.ts` SHA-256 `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081`.

- `TraderDetail.livePositions: Position[]` exists as a **client fiction** (`ticket` / `lots` / `currentPrice`) — not A26 live rows.
- **No** `LivePortfolio` / `LivePosition` type with `destinationPositionId` + `clOrdId`.
- `TraderDetailPage` never reads `livePositions`. `GetTraderAsync` returns `TraderRowDto` (no book arrays). The field is dead.

### 3.5 Product grep (apps + src, exclude bin/obj/node_modules)

| Pattern | Product hits |
|---|---|
| `live/portfolio` | **0** |
| `LiveCopyPortfolio` | **0** |
| `useLive` | **0** |
| `GetLivePortfolio` | **0** |
| `realCopyExecutionEnabled` (camel, A26 name) | **0** in product TS/C# |

Nearby names that **do** exist and must not be confused with the page:

| Symbol | Where | What it is |
|---|---|---|
| `realCopyEnabled` | `OverviewDto`, `RiskDashboardDto`, Overview/Risk pages | boolean on **other** pages; Overview hardcodes `false` in `EfDashboardQueries` |
| `REAL_COPY_EXECUTION_ENABLED` | Settings JSON (`false`); `LiveCopyPage` **literal text** | flag name, not a read |
| `CTrader:RealCopyExecutionEnabled` | `CTraderFixOptions` default **false**; fix-worker log only | send license; C07: cannot turn `35=D` on |
| `TraderState.LIVE` / `LIVE_CANDIDATE` | Domain enum + Overview counts | selection state, not dest positions |
| `CopyIntent` / `ExecutionIntent` | Domain + `copy_intents` / `execution_intents` | persist vocabulary; **no** live-portfolio query |

---

## 4. API / Application / persistence (no live BFF)

### 4.1 `apps/api/Program.cs`

SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` · 4658 B · 95 lines · 13:22:04.

Live maps (unversioned, anonymous):

```text
GET  /health
GET  /api/health
GET  /api/risk/status
GET  /api/reconciliation/status
GET  /api/settings          ← featureFlags.REAL_COPY_EXECUTION_ENABLED = false (literal)
GET  /ready
GET  /api/overview
GET  /api/brokers
GET  /api/groups
GET  /api/traders
GET  /api/traders/{broker}/{login}
GET  /api/fix/sessions
GET  /api/risk
GET  /api/trades
POST /api/ops/resync
```

**Zero** of: `/api/v1/live/portfolio`, `/api/live/portfolio`, `/api/v1/copy-intents`, `/api/v1/settings/execution`.

`GET /api/settings` is the only JSON that names the flag. `LiveCopyPage` does not call it.

### 4.2 `IDashboardQueries`

`DashboardModels.cs` SHA-256 `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` · 97 lines.

Seven methods: Overview, Brokers, Groups, Traders, Trader, FIX, Risk. **No** `GetLivePortfolioAsync`. **No** `LivePortfolioDto`.

`OverviewDto` has `int Live` + `bool RealCopyEnabled` + `decimal DestinationRealPnl`. `EfDashboardQueries.GetOverviewAsync` sets `Live` = count of `TraderState.LIVE` scores, `DestinationRealPnl = 0`, `RealCopyEnabled = false` (hard-coded, not options).

`OverviewPage` paints Watch / Shadow / Live **candidates** / Risk blocked. It **does not** paint `data.live` (§47 “Live copied” — B20 already flagged). That is Overview, not this nav row, but it is the same missing dest book.

`TraderRowDto` has **no** `liveAllocation` (A92 requires it; **0** while execution is off is the honest value).

### 4.3 EF / §44 tables the page would read

`TraderDbContext.cs` SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`.

| §44 / §45 table | Mapped? |
|---|---|
| `copy_intents` | **yes** (`CopyIntent`, unique `IdempotencyKey`) |
| `risk_decisions` | **yes** |
| `execution_intents` | **yes** (unique `ClOrdId`) |
| `destination_quotes` | **yes** |
| `fix_sessions` | **yes** |
| `shadow_orders` | **yes** (shadow, not live) |
| `fix_orders` | **no** |
| `fix_execution_reports` | **no** |
| `destination_positions` | **no** |
| `source_destination_links` | **no** |
| `copy_allocations` | **no** |
| `execution_reconciliation_*` | **no** |

Entities exist (`CopyIntent.cs` 759 B, `ExecutionIntent.cs` 756 B). **Nothing lists them as a live book.** No seeder rows are required for an honest empty GET.

A74: source→dest mapping **MISSING**. A101: dest position table **MISSING**. That is why a truthful `/live/portfolio` today would be `{ realCopyExecutionEnabled: false, pnl: 0, openCount: 0, positions: [] }` — and even that GET is **not** implemented.

### 4.4 Send path (must stay off)

C07 + this pass: no `35=D` builder. `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false**. Fix-worker only stamps session rows and logs the flag. Safety is **absence**, not a tested gate (A101). **Do not** enable send to “fill” this page.

---

## 5. Adjacent §46 surfaces (same missing book)

Recorded so “Live Copy missing?” is not reduced to one filename.

| Surface | Architecture | Now |
|---|---|---|
| Overview “Live copied” | §47 | DTO field `live` exists; **page never renders it** (B20) |
| Overview dest real P&L | §47 | rendered; query **hardcodes 0** |
| Leaderboard “Live allocation” | §50 / A92 `liveAllocation` | **absent** from `TraderRowDto` |
| Trader Detail live book | §51 / A93 `livePositions` | page is score tiles only; TS field unused; API has no embed |
| Header strip flag | A26 §5.3 | **absent** from `DashboardLayout` |
| Settings execution toggle | A26 `PATCH /settings/execution` | Settings is `JSON.stringify`; A63 parks the PATCH |
| SignalR live book | A97 | no `live.portfolio` topic (ok for v1); hub itself **MISSING** (C28) |

These do **not** replace the `/live` page. Filling Overview’s unused `data.live` count is still not a dest-position book.

---

## 6. §46 × A26 × disk (this leaf only)

| Check | Required | Measured | Status |
|---|---|---|---|
| Sidebar leaf exists | yes | yes (`/live`) | **PRESENT** |
| Exact label | `Live Copy Portfolio` | `Live` | **FAIL** |
| Order | after Shadow, before cTrader FIX | yes | **PASS** |
| Route | `/live` | `/live` | **PASS** |
| A62 path | `pages/live/LiveCopyPortfolioPage.tsx` | `pages/LiveCopyPage.tsx` | **WRONG** name / folder |
| Empty-safe table | flag + `positions[]` | two sentences | **FAIL** |
| Flag from API | A26 field | JSX literal | **FAIL** |
| Versioned GET | `/api/v1/live/portfolio` | none | **FAIL** |
| `/copy-intents` | additional read | none | **FAIL** |
| Page mutations | none | none | **PASS** |
| Fake dest rows | forbidden | none | **PASS** |
| Header five chips | A26 §5.3 | none | **FAIL** (shell) |
| First-useful API | A63 **out** | no GET | **ALIGNED with A63** |
| Working live book | A30 / §70 **not in v1** | no book | **ALIGNED with A30** |

**Score for “is the §46 page here?”:** chrome **2/4** (route + order). Contract **0/6** (label, module, GET, DTO, table, flag-from-API). Honesty **2/2** (no fake rows, no page mutations).

---

## 7. First-useful vs later (so a coding wave does not over-build)

| Surface | §69 / A63 first useful | §46 / A26 full |
|---|---|---|
| Live Copy nav | allowed as “not in v1” / execution-off stub | **required** exact label |
| Empty-safe UI (flag + empty table) | A62 yes; A30 allows non-working | required |
| `GET /api/v1/live/portfolio` | **out of v1** | required; `positions: []` + flag while off |
| `GET /copy-intents` | A63 lists it under **shadow** extras, not a live page | A26 additional for `/live` |
| Dest positions / FIX orders | Phase 7–8 | required to show a real book |
| `NewOrderSingle` | **forbidden** | §70 + A100 19/19 + A101 14/14 + explicit flag |
| Overview / Detail live widgets | dest P&L may be 0; do not invent | same honesty |

A30 increment 6 is **not** a license to omit the leaf forever. It **is** a license to keep the book empty and the GET unimplemented until Phase 8.

Current stub’s *intent* (flag off → empty) is directionally right. It still fails exact-label law and does not consume a contract DTO.

---

## 8. What earlier reports claimed

| Claim | When | C37 (this pass) |
|---|---|---|
| A62 §3: “**Live Copy Portfolio missing**” (no route) | morning / empty `pages/` | **Stale.** `/live` + file exist. |
| A29 U07: Shadow / Live pages MISSING | Phase 0 | **Stale for files.** Still true for widgets. |
| B10: Live Copy `/live` **absent** / MISSING | 12-link sidebar | **Stale.** 14 links; Live is one. |
| B22: Live Copy never imported / never written | 13 page files | **Stale.** Written 13:20:38 with Audit. |
| B20 / B31: `LiveCopyPage` stub, label `Live`, no GET | 13:20:38+ | **Still true.** This file is the Live-only pin. |
| C08: Live present; imported; static stub | 13:23 | **Still true.** Import match ≠ page complete. |
| A63: `/live/portfolio` out of v1 | spec | **Still true.** Do not treat missing GET as a §69 blocker. |

Use **this file** for “is Live Copy Portfolio missing vs §46?”. Do not recreate the file from B22’s missing-module list.

---

## 9. What a later wave must not do

1. Do **not** treat `LiveCopyPage.tsx` as absent. Do not create a second `pages/live/LiveCopyPortfolioPage.tsx` beside it without a single-module coding task.
2. Do **not** treat `/shadow` or Overview “Live candidates” as this leaf.
3. Do **not** invent dest positions, `clOrdId`s, or non-zero dest P&L while execution is off.
4. Do **not** enable `REAL_COPY_EXECUTION_ENABLED` or emit `NewOrderSingle` to populate the table.
5. Do **not** add page-level flatten / enable-live buttons (A26: mutations **none** on `/live`).
6. Do **not** ship `/api/v1/live/portfolio` as a §69 first-useful requirement. If a later wave adds it, empty-safe `{ flag: false, pnl: 0, openCount: 0, positions: [] }` is the only honest body.
7. Do **not** leave the sidebar as `Live` if the task is “§46 exact nav.”
8. Do **not** add a second frontend or a `pages/` tree under `D:\Prop\src`.
9. Do **not** copy source-MT5 P&L into dest / live fields (A91).
10. This report does **not** authorize product edits.

---

## 10. Direct answers (copy-out)

### Architecture 46 has Live Copy Portfolio. Missing?

**Yes — as the §46 / A26 page. No — as a routed stub.**

| | Nav leaf | Exact §46 label | Route | Page module | Data contract |
|---|---|---|---|---|---|
| **Live Copy Portfolio** | Present as **`Live`** | **No** | `/live` present | 321 B / 8-line stub | **Missing** `GET /api/v1/live/portfolio` |

- **File missing?** **No.** `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`.
- **A62 module missing?** **Yes** (`pages/live/LiveCopyPortfolioPage.tsx`).
- **Working live portfolio missing?** **Yes**, and **must stay** empty until §68 + §70 + explicit flag.
- **§69 blocker?** **No** (A63 out of v1). **§46 dashboard completeness blocker?** **Yes.**

### Did this pass change product source?

**No.**

---

## Evidence pins

- Architecture §46: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1735–1758.
- Pipeline / flag / dest tables: same file §32–§35 (L1266–1398), §41 (L1564–1590), §44 (L1645–1668).
- A26 route + contract: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2 L299, §6.10 L750–780, §9 L1210.
- A62 target: `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` L197, L318, L407, L429.
- A63 out of v1: `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` L1013.
- A30: `D:\Prop\reports\swarm\20260818\A30_implementation_sequence.md` L775.
- Stub: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` — 321 B, SHA-256 `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82`.
- Router: `D:\Prop\apps\web\src\App.tsx` L11, L32. SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` L13 `{ to: '/live', label: 'Live' }`. SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 queries, none live. SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`.
- API: `D:\Prop\apps\api\Program.cs` — 15 maps; no `/live*`. SHA-256 `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9`.
- Queries: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` — 7 methods. SHA-256 `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439`.
- EF: `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` — `copy_intents` / `execution_intents` yes; `destination_positions` no. SHA-256 `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB`.
- Domain: `D:\Prop\src\Domain\Entities\CopyIntent.cs`, `ExecutionIntent.cs`; `TraderState.LIVE` in `Enums\TraderState.cs`.
- Flag default: `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35 `= false`.
- Prior: B20 §2.1, B31 §6, C08 §5–§6. Supersedes A62/B10/B22 on **file** absence only.
