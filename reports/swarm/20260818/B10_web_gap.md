# B10 — `apps/web` existence check, gap analysis, and Vite React TS page plan

| Field | Value |
|---|---|
| Agent | B10 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\B10_web_gap.md` |
| Scope | `D:\Prop\apps\web` measured vs architecture §§5, 46–55, 58–59, 66, 69.12, 71–73; contracts A26 / A62 / A63 / A91 |
| Product source edited | **No.** This file is the only write. |
| Classification vocabulary | Architecture §73.B |

This document answers three questions:

1. Does `D:\Prop\apps\web` exist?
2. What is the honest gap vs the operational dashboard in architecture §§46–54?
3. What Vite + React + TypeScript pages must a later **authorized** coding wave implement?

It does **not** authorize creating, rewriting, or deleting files under `apps/web`, `apps/api`, or `src/`.

---

## 0. Verdict

**`D:\Prop\apps\web` exists.** It is a Vite 5 + React 18 + TypeScript host with a dark Tailwind shell, 13 page modules, TanStack Query hooks, and an axios client pointed at unversioned `/api/*`.

It is **not** the architecture §46–54 professional ops dashboard. It is **not** §69 item 12 (“show all of this in React”).

| Check | Measured result | §73.B |
|---|---|---|
| Folder `D:\Prop\apps\web` | **Present** (2026-08-18) | — |
| Vite + React 18 + TS + Tailwind | Present and wired | **EXISTS_AND_GOOD** (host) |
| Router + TanStack Query + axios | Present | **EXISTS_AND_GOOD** (wiring) |
| Page modules on disk | **13** `.tsx` files | **EXISTS_NEEDS_REFACTOR** |
| §46 nav (16 labels, exact text) | 12 abbreviated items; 4 missing | **EXISTS_NEEDS_REFACTOR** |
| Login / Models / Live Copy / Audit | **Absent** | **MISSING** |
| Auth + RBAC + `GET /auth/me` | **Absent** | **MISSING** |
| A26 `/api/v1/**` + envelopes | Hooks call `/api/*`; no unwrap | **EXISTS_NEEDS_REFACTOR** |
| SignalR `/hubs/ops` | Client hits `/hubs/dashboard`; API has **no** hub | **EXISTS_NEEDS_REFACTOR** (client) / **MISSING** (server) |
| ECharts widgets (§51/§46) | `recharts` in `package.json`, **zero imports**; no `echarts` | **MISSING** |
| Secret-safe types + denylist | Types unused; no denylist | **MISSING** |
| `npm` lockfile / `node_modules` | **Absent** — `tsc && vite build` cannot run as-is | **MISSING** |
| Docker / nginx | **Absent** | **MISSING** (A65; not a §69 gate) |
| Talks to A26 contract | **No** | — |
| §69.12 first-useful React | **FAIL** | — |

Do not greenwash this as “dashboard 80% done.” The 13 pages are thin tables / JSON dumps bound to a demo BFF. A later coding wave must **retarget A26**, not grow the current hooks as the contract.

---

## 1. Binding sources and precedence

When documents disagree, implementers use this order:

1. Architecture law (`D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`) for **what** must appear (nav labels, widgets, no secrets).
2. **A26** for **how** the browser talks to the API (paths, envelopes, roles, SignalR event names).
3. **A63** for the **first-useful** subset (Phases 0–5). Live flatten / enable-real / model promote stay out of §69.
4. **A62** for the **target folder tree** (still the right shape). Its “empty pages / 0 files” census is **stale**.
5. **This file (B10)** for the **2026-08-18 measured** `apps/web` + current `apps/api` consumer map.
6. A06 / A29 / A57 / A91 are **audit snapshots**. Several still say `apps/web` is absent or `src/pages` is empty. **Do not treat those sentences as current disk state.**

Resolved conflicts (do not re-litigate in UI code):

| Topic | Current stub / stale audit | Binding |
|---|---|---|
| API prefix | `GET /api/overview` | A26 `GET /api/v1/overview` |
| Groups route | stub `/groups` + `/api/groups` | A26 `/mt5-groups` + `/api/v1/mt5/groups` |
| Trader identity | stub broker **code** (`ACHIEVER`) | A26 `/traders/{brokerId}/{login}` — `brokerId` is UUID (§10) |
| Manager-login mask | `MaskLogin` zeros last two digits; UI appends extra `**` | A26 keep **last two** digits (`2027` → `"**27"`) |
| Hub | stub `/hubs/dashboard` | A26 `/hubs/ops` |
| Risk GET | stub `/api/risk` (+ unused `/api/risk/status`) | A26 `/api/v1/risk/dashboard` |
| Health GET | stub `/api/health` (hardcoded demo) | A26 `/api/v1/health` (or A63 `/api/v1/system/health`). Probes stay `/health` + `/ready` (A77) |
| Settings | stub `GET /api/settings` whole object | A26 `GET /api/v1/settings` + PATCHes of **non-secret** slices |
| Charts | unused `recharts` | ECharts (A62 + §5 “ECharts or Recharts”; new code = ECharts) |
| Overview DTO | flat `OverviewDto` bools | A26 / A91 nested `accounts` / `traderStates` / `pnl` / `exposure` / `health` / `flags` |
| Trades | raw EF `ReconstructedTrade[]` | A26 allow-list DTO; tickets / ids as **string** |
| Enums | C# numeric `TraderState` on the wire | A26 string unions (`WATCH`, …) |

---

## 2. Existence check (disk, 2026-08-18)

### 2.1 Root

```text
D:\Prop\apps\web\                 EXISTS
  index.html                     369 B    dark shell, title "MT5 Trader Intelligence"
  package.json                   739 B
  postcss.config.js               87 B
  tailwind.config.js             169 B
  tsconfig.json                  585 B    strict, include src only, no @ alias
  tsconfig.node.json             223 B
  vite.config.ts                 169 B    port 3000, no proxy, no alias
  src\
    App.tsx                     1854 B    13 routes + redirect
    main.tsx                     648 B    QueryClient + BrowserRouter
    index.css                     62 B    Tailwind directives only
    api\
      client.ts                  232 B
      hooks.ts                  1935 B
      signalr.ts                 899 B
    components\
      MetricCard.tsx             521 B
      StatusBadge.tsx            699 B    unused by current pages
    layouts\
      DashboardLayout.tsx       1756 B
    pages\                      13 files (see §2.2)
    types\
      index.ts                  2905 B    unused by pages (`any` instead)
    utils\
      formatters.ts              947 B    unused by pages
```

**Absent (confirmed `Test-Path = False`):**

```text
apps/web/node_modules
apps/web/package-lock.json
apps/web/pnpm-lock.yaml
apps/web/yarn.lock
apps/web/.env
apps/web/.env.example
apps/web/.env.local
apps/web/Dockerfile
apps/web/nginx.conf
apps/web/vite-env.d.ts
apps/web/src/vite-env.d.ts
apps/web/src/auth/
apps/web/src/hubs/
apps/web/src/routes/
apps/web/src/lib/
```

`apps/web` is **not** in `Mt5TraderIntelligence.sln` (correct — Vite has no `.csproj`). A88 may later add Solution Explorer shortcuts. Not a B10 action.

### 2.2 Page files that exist

| File | Bytes | Bound hook | Route today |
|---|---:|---|---|
| `OverviewPage.tsx` | 2078 | `useOverview` | `/overview` |
| `BrokersPage.tsx` | 1266 | `useBrokers` | `/brokers` |
| `GroupsPage.tsx` | 1228 | `useGroups` | `/groups` |
| `TradersPage.tsx` | 1604 | `useTraders` | `/traders` |
| `TraderDetailPage.tsx` | 1592 | `useTraderDetail` | `/traders/:brokerId/:login` |
| `TradeExplorerPage.tsx` | 1321 | `useTrades` | `/trades` |
| `ScoringPage.tsx` | 1288 | `useTraders` (wrong resource) | `/scoring` |
| `ShadowPortfolioPage.tsx` | 628 | **none** (static copy) | `/shadow` |
| `FixSessionsPage.tsx` | 1312 | `useFixSessions` | `/fix` |
| `RiskPage.tsx` | 1148 | `useRiskStatus` | `/risk` |
| `ReconciliationPage.tsx` | 490 | `useReconciliation` | `/reconciliation` |
| `SystemHealthPage.tsx` | 369 | `useHealth` | `/health` |
| `SettingsPage.tsx` | 459 | `useSettings` | `/settings` |

**Page files that do not exist:** `LoginPage`, `ModelsPage`, `LiveCopyPortfolioPage`, `AuditPage`.

### 2.3 `package.json` (measured)

```text
name: mt5-trader-intelligence
scripts: dev / build (tsc && vite build) / preview
deps:
  react ^18.3.1
  react-dom ^18.3.1
  react-router-dom ^6.26.0
  @tanstack/react-query ^5.51.0
  axios ^1.7.0
  recharts ^2.12.0          # never imported
  @microsoft/signalr ^8.0.0
dev:
  vite ^5.3.4
  typescript ^5.5.3
  tailwindcss ^3.4.6
  @vitejs/plugin-react ^4.3.0
```

**Not present:** `echarts`, `echarts-for-react`, `zustand`, `@tanstack/react-query-devtools`, Vitest / Playwright, MSW.

`recharts` appears only in `package.json`. Grep of `apps/web/src` found **zero** `recharts` / `echarts` / `zustand` / `localStorage` imports.

### 2.4 Vite / TS host details

- Dev server: **port 3000** (`vite.config.ts`). No `/api` or `/hubs` proxy.
- Axios + SignalR default: `import.meta.env.VITE_API_URL || 'http://localhost:5000'`.
- Current API `launchSettings.json` **http** profile is `http://localhost:5000` (matches the fallback). `TraderIntelligence.Api.http` still says `:5160` — stale file, not the running profile.
- CORS on the API is `AllowAnyOrigin` / any header / any method — the browser can reach `:5000` from `:3000` today. That is **not** a production policy.
- `tsconfig.json` is `strict: true` but `noUnusedLocals` / `noUnusedParameters` are **false**. No `paths` alias.
- QueryClient defaults: `retry: 2`, `refetchOnWindowFocus: false`, `staleTime: 30_000`.
- No `vite-env.d.ts` → `import.meta.env.VITE_API_URL` is untyped.

---

## 3. What the current API actually serves the browser

B10 is a **web** gap. The consumer contract on disk today is `apps/api/Program.cs` + `DashboardModels.cs` + `EfDashboardQueries.cs`. This is **not** A26. It is the thing the 13 pages compile against.

| Method | Path today | Page | Shape |
|---|---|---|---|
| GET | `/health` | (probe, unused by React) | `{ status, utc }` |
| GET | `/ready` | unused | `{ ready, brokers }` |
| GET | `/api/health` | System Health | **Hardcoded** ACHIEVER/QUOTE healthy, redis false |
| GET | `/api/overview` | Overview | flat `OverviewDto` |
| GET | `/api/brokers` | Brokers | `BrokerStatusDto[]` |
| GET | `/api/groups` | Groups | `GroupRowDto[]` |
| GET | `/api/traders?broker&state` | Traders, Scoring | `TraderRowDto[]` |
| GET | `/api/traders/{broker}/{login}` | Trader Detail | same `TraderRowDto` (no sub-resources) |
| GET | `/api/trades?broker&login` | Trade Explorer | **raw** `ReconstructedTrade` entities, take 200 |
| GET | `/api/fix/sessions` | FIX | `FixSessionDto[]` |
| GET | `/api/risk` | Risk | `RiskDashboardDto` |
| GET | `/api/risk/status` | unused by hooks | same as `/api/risk` |
| GET | `/api/reconciliation/status` | Recon | **Hardcoded** zeros + `DateTimeOffset.UtcNow` |
| GET | `/api/settings` | Settings | **Hardcoded** limits + flags + two broker names |
| POST | `/api/ops/resync` | **no UI** | demo ingest + rescore |

**Missing entirely (A26):** `/api/v1/**`, auth, SignalR `/hubs/ops`, scoring summary, models, shadow portfolio, live portfolio, copy-intents, trader sub-resources, recon issues, audit, envelopes, `Idempotency-Key`, secret sanitizer.

**Present but unsafe / dishonest for a dashboard:**

1. Entire host is **anonymous**. CORS is wide open.
2. `GET /api/trades` serializes EF entities (`brokerId` GUID, `positionId` as JSON number).
3. `TraderState` has **no** `JsonStringEnumConverter` registered in `DependencyInjection` or `Program.cs`. Wire values are **integers** (`WATCH = 2`). Pages print `{t.state}` → operator sees `2`.
4. `BrokerStatusDto.ManagerLoginMasked` is `long`, not a mask string. `EfDashboardQueries.MaskLogin` does `login / 100 * 100` (`2027` → `2000`). `BrokersPage` then renders `{b.managerLoginMasked}**` → **`2000**`**. Architecture / A26: `"**27"`.
5. `GET /api/health` and `/api/reconciliation/status` invent healthy/zero snapshots. That is fabricated ops state.
6. `OverviewDto` health is three **bools**. A91: a boolean cannot express `STALE` / `DEGRADED` / `UNKNOWN`.
7. `CTrader:Password` exists in `apps/api/appsettings.json` as an empty string. Settings GET currently does **not** echo it (good). There is still **no** sanitizer for the day someone serializes options.

`POST /api/ops/resync` is a demo operator action with no RBAC and no UI. Do not wire a React button to it in first useful.

---

## 4. Stack vs architecture §5

| §5 item | Current | Gap |
|---|---|---|
| React | 18.3 | none |
| TypeScript | 5.5 strict | add `@/*` paths; add `vite-env.d.ts` |
| Vite | 5.3 | add `/api` + `/hubs` proxy; keep port 3000 |
| TanStack Query | v5 wired | split fetchers; A26 query keys; stop `any` |
| React Router | v6 wired | add login + 4 missing routes; exact §46 labels |
| Zustand | absent | add **only** if confirm-modal / sidebar need it (A62) |
| SignalR | client only, wrong URL | retarget `/hubs/ops` + JWT; do not invent a second socket |
| ECharts or Recharts | Recharts **declared**, unused | add ECharts; remove Recharts in the same wave |
| No LLM / Next / Redux / MSW / Storybook | correctly absent | keep absent (A80 / §71) |

Classification of the **stack choice**: **EXISTS_AND_GOOD**. Classification of the **implementation**: **EXISTS_NEEDS_REFACTOR**.

---

## 5. Navigation vs architecture §46

Architecture nav (exact labels, this order):

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

Current `DashboardLayout` nav:

```text
Overview              /overview
Brokers               /brokers
Groups                /groups          ← wrong label + wrong path
Traders               /traders
Trades                /trades          ← wrong label
Scoring               /scoring
Shadow                /shadow          ← wrong label
FIX                   /fix             ← wrong label
Risk                  /risk
Recon                 /reconciliation  ← wrong label
Health                /health          ← wrong label
Settings              /settings
```

| §46 label | Required route (A26) | Today | Status |
|---|---|---|---|
| Overview | `/overview` | present | **EXISTS_NEEDS_REFACTOR** |
| Brokers | `/brokers` | present | **EXISTS_NEEDS_REFACTOR** |
| MT5 Groups | `/mt5-groups` | `/groups` | **EXISTS_NEEDS_REFACTOR** |
| Traders | `/traders` | present | **EXISTS_NEEDS_REFACTOR** |
| Trader Detail | `/traders/:brokerId/:login` | present (param name ok; value is broker **code**) | **EXISTS_NEEDS_REFACTOR** |
| Trade Explorer | `/trades` | present | **EXISTS_NEEDS_REFACTOR** |
| Scoring | `/scoring` | present (wrong data source) | **EXISTS_NEEDS_REFACTOR** |
| Models | `/models` | **absent** | **MISSING** |
| Shadow Portfolio | `/shadow` | static placeholder | **EXISTS_NEEDS_REFACTOR** |
| Live Copy Portfolio | `/live` | **absent** | **MISSING** |
| cTrader FIX | `/fix` | present (array of sessions, not two cards) | **EXISTS_NEEDS_REFACTOR** |
| Risk | `/risk` | present | **EXISTS_NEEDS_REFACTOR** |
| Reconciliation | `/reconciliation` | JSON dump | **EXISTS_NEEDS_REFACTOR** |
| System Health | `/health` | JSON dump | **EXISTS_NEEDS_REFACTOR** |
| Audit | `/audit` | **absent** | **MISSING** |
| Settings | `/settings` | JSON dump | **EXISTS_NEEDS_REFACTOR** |
| Login (A26, not in §46 list) | `/login` | **absent** | **MISSING** |

Trader Detail is a **detail route**, not a sidebar leaf. Sidebar should highlight **Traders** on `/traders/:brokerId/:login`.

Header strip required by A26 §5.3 / A62 §7.2 (Real copy / Stop-new / MT5 / FIX QUOTE / FIX TRADE) — **MISSING**. Layout starts SignalR and renders only a left rail + `<Outlet/>`.

---

## 6. Per-page gap vs §§47–54 (and remaining §46)

Status codes: **MISSING** | **STUB** | **WRONG** | **PARTIAL** | **N/A**.

### 6.1 Overview — §47 (18 tiles)

Current page: 12 `MetricCard`s + a real-copy banner. Bound to flat `OverviewDto`.

| Widget (architecture) | Current field | Status |
|---|---|---|
| Total MT5 accounts | `totalAccounts` | **PARTIAL** (right idea, wrong DTO nest) |
| Connected source brokers | `connectedBrokers` | **PARTIAL** (counts `Brokers.Enabled`, not live connections) |
| XAUUSD traders | `xauTraders` | **PARTIAL** |
| Traders with ≥ 3 completed trades | `tradersWithThreeTrades` | **PARTIAL** |
| Watch | `watch` | **PARTIAL** |
| Shadow | `shadow` | **PARTIAL** |
| Live candidates | `liveCandidates` | **PARTIAL** |
| Live copied | `live` exists on DTO | **WRONG — not rendered** |
| Risk blocked | `riskBlocked` | **PARTIAL** |
| Shadow P&L | `shadowPnl` | **WRONG** — query sums `ShadowOrders.SourceVsShadowSlippage`, not book P&L (A24) |
| Destination real P&L | `destinationRealPnl` hardcoded `0` | **PARTIAL** (0 is honest while live off; still must come from A26 `pnl.destinationRealPnl`) |
| Current XAU gross | `xauGross` hardcoded `0` | **WRONG — not rendered** |
| Current XAU net | `xauNet` hardcoded `0` | **WRONG — not rendered** |
| Destination free margin | not on DTO | **MISSING** |
| Destination margin level | not on DTO | **MISSING** |
| MT5 ingestion health | `mt5Healthy` bool | **WRONG** (need `healthStatus`) |
| FIX quote health | `quoteHealthy` bool | **WRONG** |
| FIX trade health | `tradeHealthy` bool | **WRONG** |

`INSUFFICIENT_DATA` / `EARLY_SCORE` / `PAUSED` / `DISQUALIFIED` are not Overview tiles (correct). Do not invent a 19th “overall green” score (A91).

### 6.2 Brokers — §48

| Widget | Current | Status |
|---|---|---|
| Display name | `displayName` | **PARTIAL** |
| Connection status | `connected` bool | **WRONG** — need `connectionStatus` enum |
| Server | `server` only (no port / SSL / `serverName`) | **PARTIAL** |
| Manager login masked | `long` + extra `**` | **WRONG / UNSAFE-adjacent** (mask algorithm inverted) |
| Group / account count | present | **PARTIAL** |
| Deal ingest rate | absent | **MISSING** |
| Last event | DTO has `lastEventAt`; **not rendered** | **WRONG** |
| Last successful history sync | absent | **MISSING** |
| Connection pool usage | absent | **MISSING** |
| Reconnect count | absent | **MISSING** |
| Secrets | not shown | **EXISTS_AND_GOOD** (this page) |

`StatusBadge` exists and is unused.

### 6.3 MT5 Groups — §49

| Column | Current | Status |
|---|---|---|
| Broker | `g.broker` (code) | **PARTIAL** |
| Group | `g.group` | **PARTIAL** |
| Accounts | `g.accounts` | **PARTIAL** |
| Enabled for analysis | `enabledForAnalysis` | **PARTIAL** (read-only; no PATCH) |
| Plan mapping | `planMapping` | **PARTIAL** |
| Last discovered | DTO `lastDiscovered`; **not rendered** | **WRONG** |
| Last synced | DTO `lastSynced`; **not rendered** | **WRONG** |
| Filters | none | **MISSING** |
| Route `/mt5-groups` | `/groups` | **WRONG** |

Copy on the page (“plan mappings do not filter ingestion”) is correct vs §9 / A40.

### 6.4 Trader Leaderboard — §50

Rendered columns: Broker, Login, Group, XAU trades, Net P&L, Early, Risk, Flags, State.

| Column | Status |
|---|---|
| Broker / Login / Group / Completed XAU / Net source P&L / Early / Risk / three flags / State | **PARTIAL** |
| ML probability | **MISSING** (must render `—` when null; never `0`) |
| Shadow P&L | DTO field `shadowPnl` always `0`; **not rendered** |
| Live allocation | **MISSING** |
| Last scored | DTO `lastScored`; **not rendered** |
| Filters (broker, group, state, score, risk, trade count, martingale, date) | **MISSING** (`useTraders({})` always) |
| URL-persisted filters | **MISSING** |
| `TraderState` as string | **WRONG** (numeric enum) |
| Early score format `.toFixed(1)` | **WRONG** if A26 scores are `[0,1]` — confirm A22 scale before painting |

Row link `/traders/${t.broker}/${t.login}` matches **today’s** API (broker code). A26 requires UUID `brokerId`.

### 6.5 Trader Detail — §51

Current page reprints the leaderboard row as eight info tiles. No trades table. No charts.

| Block | Status |
|---|---|
| Account overview (balance / equity / currency / leverage) | **MISSING** |
| XAU trade history + first-3 highlight | **MISSING** |
| Score timeline (ECharts) | **MISSING** |
| Risk flags | **PARTIAL** (martingale + averaging; lot escalation / abnormalSizing not shown) |
| Behavior features (holding p50/p90, SL/TP rates, drawdown) | **MISSING** |
| Lot-size timeline | **MISSING** |
| Holding-time distribution | **MISSING** |
| MFE/MAE only when `featureQuality === EXACT` | **MISSING** (must never fabricate — A45) |
| Shadow / live books | **MISSING** |
| Source-to-destination map | **MISSING** |
| Pause / resume copy | **MISSING** (correctly absent until RBAC) |
| `mlProbability ?? 'not trained'` | **PARTIAL** (honest empty) |

`GET /api/traders/{broker}/{login}` returns the same `TraderRowDto` as the list. There are no sub-resources.

### 6.6 cTrader FIX — §52

Current page maps `data = []` as cards. That matches **today’s** `FixSessionDto[]`. A26 is `{ quote, trade }` — two **independent** objects, never “one session with a qualifier.”

| Shared field | Status |
|---|---|
| Host / port | **PARTIAL** (port not labeled SSL) |
| Connected / logged on / status | **PARTIAL** |
| Last inbound / outbound | DTO has them; **not rendered** |
| Sequence | inbound/outbound ints | **PARTIAL** (A26 wants `nextSender` / `nextTarget`) |
| Reconnect count | **PARTIAL** |
| Last heartbeat / test request | **MISSING** |
| Errors | DTO `lastError`; **not rendered** |
| QUOTE: mapped / instrument / bid / ask / age / spread | **PARTIAL** (no mapped flag, no spread) |
| TRADE: execution enabled / open orders / dest positions / last ER / last recon | only `executionEnabled` | **PARTIAL** |
| Never show FIX password | **EXISTS_AND_GOOD** (not on DTO) |
| Hardcoded tag 55 | not hardcoded | **EXISTS_AND_GOOD** (`instrumentId ?? 'not discovered yet'`) |

Copy about `TargetCompID = cServer` is correct vs §26.

### 6.7 Risk — §53

| Widget | Status |
|---|---|
| Equity / balance / free margin / margin level | **MISSING** |
| Daily P&L / drawdown | daily P&L only (DTO drawdown unused) | **PARTIAL** |
| XAU long / short / net | net only | **PARTIAL** |
| Risk by copied trader | **MISSING** |
| Risk by source broker | **MISSING** |
| Rejected intents + reason codes | string list `recentRejectReasons` | **PARTIAL** |
| `STOP_NEW_EXECUTION` | collapsed into `killSwitch` string | **WRONG** |
| `EMERGENCY_FLATTEN` availability | **MISSING** |
| Confirm-phrase flatten / stop-new mutations | **MISSING** (correct for §69 **not** to ship flatten) | flatten = **do not build** (A63 / A80); stop-new read + later write is in scope for Phase 8 / safety |

A63: emergency flatten, enable-real, and credential writes are **not** first-useful mutations. The **tiles** for flatten availability and stop-new state still belong on the GET (A26 / A48).

### 6.8 Reconciliation — §54

Current page: `<pre>{JSON.stringify(data)}</pre>` of a hardcoded `{ lastReconciliation, unknownPositions, mismatches, orphanFills }`.

| Widget | Status |
|---|---|
| Last successful MT5 recon | **MISSING** (one fake timestamp) |
| Last successful cTrader recon | **MISSING** |
| UNKNOWN_EXTERNAL / MISSING_INTERNAL / ORDER / QTY / SIDE / ORPHAN_FILL / ORPHAN_ER / UNEXPECTED_FILL / UNRESOLVED_STATE | **MISSING** (collapsed counts) |
| Issue table + ack | **MISSING** |
| “Nothing unresolved silently ignored” | **FAIL** — zeros are invented |

### 6.9 Remaining §46 pages

| Page | Status | Notes |
|---|---|---|
| Login | **MISSING** | email + dashboard password; never cache password |
| Trade Explorer | **STUB** | no filters; no first-3; no broker column; ids as Guid/number |
| Scoring | **WRONG** | reuses trader list; `behaviorScore ?? 0` **fabricates 0** (`TraderRowDto` has no behavior field even though `TraderScore.BehaviorScore` exists server-side) |
| Models | **MISSING** | empty list is honest before Phase 6; nav still required |
| Shadow Portfolio | **STUB** | static paragraph; no positions; no quote age |
| Live Copy Portfolio | **MISSING** | always show `realCopyExecutionEnabled: false` + empty positions |
| System Health | **STUB** | dumps `/api/health` demo object; A77: do not poll the orchestrator probe |
| Audit | **MISSING** | entity `AuditLog` exists in Domain; no GET; ReadOnly → 403 |
| Settings | **STUB** | dumps hardcoded flags; no PATCH UI (good for now) |

---

## 7. Data-layer / client gap

### 7.1 Hooks vs A26 query keys

Current keys: `overview`, `brokers`, `groups`, `traders`, `trader`, `trades`, `fix-sessions`, `risk`, `reconciliation`, `health`, `settings`.

A26 / A62 require a single `api/queryKeys.ts` (including `mt5-groups`, trader sub-resources, `scoring`, `models`, `shadow`, `live`, `copy-intents`, `audit`, envelope unwrap).

Polling: `useFixSessions` / `useRiskStatus` every 5s; `useHealth` every 10s. Replace with SignalR invalidation + slower fallback (15–30s). Never poll secrets.

### 7.2 Types

`src/types/index.ts` does **not** match `OverviewDto` / A26 / the pages. Pages use `any`. Treat the file as **DEPRECATED** for the coding wave. Replace with allow-list DTOs (A62 `src/types/*`). Never clone `src/Domain/Entities` (those include `ManagerLogin`).

Tickets, `clOrdId`, destination position ids: **string** in TS. Login stays decimal text in the path.

### 7.3 SignalR

```text
apps/web/src/api/signalr.ts  →  ${BASE}/hubs/dashboard   (no JWT)
apps/api/Program.cs          →  no MapHub
```

A26 hub: `/hubs/ops`. Events: `ops.header`, `overview.updated`, `broker.health`, `fix.session`, `quote.xauusd`, `risk.state`, `reconciliation.issue`, `trader.score`.

§69.12 can paint from REST polling (A06 / A57). Scaffold the hub module so pages do not invent a second websocket. Do not block first-useful on live tiles.

### 7.4 Unused / deprecated client files

| File | Disposition for the next coding wave |
|---|---|
| `src/api/hooks.ts` | **DEPRECATED** — wrong prefix, wrong resources, no envelopes |
| `src/api/signalr.ts` | **DEPRECATED** — wrong hub, no JWT |
| `src/types/index.ts` | **DEPRECATED** — wrong shapes |
| `src/utils/formatters.ts` | fold into `src/lib/format.ts` (A62); do not keep both |
| `src/App.tsx` | keep as route table; add missing routes; `/groups` → `/mt5-groups` |
| `src/api/client.ts` | keep axios **or** fetch — pick one; add Bearer, `X-Correlation-Id`, 401→refresh, envelope unwrap, denylist fail-closed |
| `MetricCard.tsx` / `StatusBadge.tsx` | replace with A62 `KpiCard` / `HealthDot` / `TraderStateBadge` or adapt |

---

## 8. Auth, RBAC, secrets

| Control | Current | Status |
|---|---|---|
| `POST /api/v1/auth/login` | absent | **MISSING** |
| Access token in memory | absent | **MISSING** |
| Refresh httpOnly cookie | absent | **MISSING** |
| `GET /auth/me` | absent | **MISSING** |
| Roles SuperAdmin / RiskManager / Analyst / ReadOnly | absent | **MISSING** |
| `can.ts` / `RequireRole` | absent | **MISSING** |
| Confirm-phrase modal | absent | **MISSING** |
| Client denylist | absent | **MISSING** |
| `localStorage` role | correctly unused | **EXISTS_AND_GOOD** |
| Password field on Settings | correctly absent | **EXISTS_AND_GOOD** |
| `VITE_*` secrets | none | **EXISTS_AND_GOOD** |

A51: identity + RBAC + `audit_logs` writer = **MISSING** on the API. React cannot fake this. First-useful still needs at least login + role-gated GETs (A63 §4). Until that exists, pages must fail closed on 401 — **not** invent scores.

Destructive UI (later phases, **not** §69): `ConfirmPhraseModal` for `EMERGENCY_FLATTEN`, `ENABLE_REAL_EXECUTION`, `PROMOTE_MODEL`. Do **not** add flatten / enable-live / promote buttons in the first-useful wave (A63, A80, §71).

---

## 9. Target Vite React TS page plan

This is the implementable page plan for a **later** coding task. Product source stays untouched until that task is explicit.

### 9.1 Stack (normative)

```text
React 18
TypeScript (strict)
Vite 5 — port 3000
TanStack Query v5
React Router v6
@microsoft/signalr
ECharts + echarts-for-react
Tailwind CSS 3
axios  (already chosen; do not add a second HTTP client)
Zustand  only if confirm-modal / sidebar collapse need it
```

Do **not** add: Next.js, Redux, MSW, Storybook, Kafka UI, LLM chat, self-promoting model widgets (A80 / §71).

`.env.example` (placeholders only, §55):

```text
VITE_API_URL=
```

Empty `VITE_API_URL` means same-origin / Vite proxy. Never a password or connection string.

`vite.config.ts` target (plan only):

```ts
server: {
  port: 3000,
  proxy: {
    '/api':  { target: process.env.VITE_API_PROXY ?? 'http://localhost:5000', changeOrigin: true },
    '/hubs': { target: process.env.VITE_API_PROXY ?? 'http://localhost:5000', changeOrigin: true, ws: true },
  },
},
resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
```

### 9.2 Route + page module map

Nav labels **exactly** as §46. Default authenticated landing: `/overview`. Unknown routes → `/overview`. `/login` is outside the shell.

| Route | Module | Architecture | Primary GET (A26) | First-useful? |
|---|---|---|---|---|
| `/login` | `pages/login/LoginPage.tsx` | §59 | `POST /api/v1/auth/login` | Yes (or empty-safe 401 shell) |
| `/overview` | `pages/overview/OverviewPage.tsx` | **§47** | `GET /api/v1/overview` | **Yes** |
| `/brokers` | `pages/brokers/BrokersPage.tsx` | **§48** | `GET /api/v1/brokers` | **Yes** |
| `/mt5-groups` | `pages/mt5-groups/Mt5GroupsPage.tsx` | **§49** | `GET /api/v1/mt5/groups` | **Yes** |
| `/traders` | `pages/traders/TraderLeaderboardPage.tsx` | **§50** | `GET /api/v1/traders` | **Yes** |
| `/traders/:brokerId/:login` | `pages/trader-detail/TraderDetailPage.tsx` | **§51** | `GET /api/v1/traders/{brokerId}/{login}` | **Yes** (tables first; charts Phase 3+) |
| `/trades` | `pages/trades/TradeExplorerPage.tsx` | §46 | `GET /api/v1/trades` | **Yes** |
| `/scoring` | `pages/scoring/ScoringPage.tsx` | §46, §18, §22 | `GET /api/v1/scoring/summary` | **Yes** |
| `/models` | `pages/models/ModelsPage.tsx` | §46, §21 | `GET /api/v1/models` | Nav yes; empty list honest; **no promote** in §69 |
| `/shadow` | `pages/shadow/ShadowPortfolioPage.tsx` | §46, §24 | `GET /api/v1/shadow/portfolio` | **Yes** (empty-safe until Phase 5) |
| `/live` | `pages/live/LiveCopyPortfolioPage.tsx` | §46, §32–35 | `GET /api/v1/live/portfolio` | Show flag + `[]`; no send |
| `/fix` | `pages/fix/CTraderFixPage.tsx` | **§52** | `GET /api/v1/fix/sessions` | **Yes** (QUOTE live; TRADE may be DISABLED) |
| `/risk` | `pages/risk/RiskDashboardPage.tsx` | **§53** | `GET /api/v1/risk/dashboard` | Read yes; **no flatten button** in §69 |
| `/reconciliation` | `pages/reconciliation/ReconciliationPage.tsx` | **§54** | `GET /api/v1/reconciliation` | MT5 recon yes; cTrader may be N/A |
| `/health` | `pages/health/SystemHealthPage.tsx` | §46, §58 | `GET /api/v1/health` | **Yes** |
| `/audit` | `pages/audit/AuditPage.tsx` | §46, §59 | `GET /api/v1/audit` | Yes for overrides; ReadOnly 403 |
| `/settings` | `pages/settings/SettingsPage.tsx` | §46, §41, §55 | `GET /api/v1/settings` | **Read-only** in §69 |

Suggested `NAV_ITEMS` (order = §46):

```text
Overview              /overview
Brokers               /brokers
MT5 Groups            /mt5-groups
Traders               /traders
Trade Explorer        /trades
Scoring               /scoring
Models                /models
Shadow Portfolio      /shadow
Live Copy Portfolio   /live
cTrader FIX           /fix
Risk                  /risk
Reconciliation        /reconciliation
System Health         /health
Audit                 /audit
Settings              /settings
```

### 9.3 Shell (every authenticated page)

**Left nav:** all 15 sidebar items visible to every authenticated role (A26 §5.3). Hiding buttons is UX only. API is the authority.

**Header strip** (always visible), source `GET /api/v1/overview` `flags` + `health`, kept hot by `ops.header`:

| Chip | Field | Visual |
|---|---|---|
| Real copy | `flags.realCopyExecutionEnabled` | **OFF by default.** Red when true |
| Stop-new | `flags.stopNewExecution` | Amber when true. Not the flatten chip |
| MT5 ingest | `health.mt5Ingestion.status` | HealthDot |
| FIX QUOTE | `health.fixQuote.status` | HealthDot |
| FIX TRADE | `health.fixTrade.status` | HealthDot |

`healthStatus`: `HEALTHY` | `DEGRADED` | `UNHEALTHY` | `STALE` | `UNKNOWN`.

**User menu:** `displayName`, `role` from `GET /api/v1/auth/me`. Logout → `POST /api/v1/auth/logout`.

### 9.4 Page widgets (what to build)

Empty-safe rule: if the API is still the demo `/api/*` or weatherforecast leftover, show `ErrorState` with `correlationId`. **Do not invent** scores, instrument IDs, MFE/MAE, or live P&L. Null / empty / 0 must mean what A26 says.

#### Overview — 18 tiles, no chart

| Widget | DTO path |
|---|---|
| Total MT5 accounts | `accounts.totalMt5Accounts` |
| Connected source brokers | `accounts.connectedSourceBrokers` |
| XAUUSD traders | `accounts.xauusdTraders` |
| Traders with ≥ 3 completed trades | `accounts.tradersWithMinThreeCompletedTrades` |
| Watch / Shadow / Live candidates / Live copied / Risk blocked | `traderStates.*` |
| Shadow P&L | `pnl.shadowPnl` + `pnl.currency` |
| Destination real P&L | `pnl.destinationRealPnl` (0 while live off) |
| Current XAU gross / net | `exposure.grossQuantity` / `netQuantity` |
| Destination free margin / margin level | `exposure.destinationFreeMargin` / `destinationMarginLevel` (`—` if null) |
| MT5 / FIX quote / FIX trade health | `health.*` |

Layout: 4-count row; 5-state row; P&L pair; exposure + margin; health row. Click-through: accounts → `/traders`, brokers → `/brokers`, health → `/health` or `/fix`.

#### Brokers — one card per source broker

Display name, `connectionStatus`, server + port + SSL + `serverName`, **`managerLoginMasked` only**, group/account counts, `dealIngestPerMinute`, last event, last history sync, pool `size/inUse/idle`, reconnects, `secretConfigured`, proxy `enabled/host/port/credentialConfigured`.

**Forbidden in types and UI:** `password`, raw `managerLogin`, `proxyUsername`, `proxyPassword`. SuperAdmin `?revealLogin=true` still returns **login integer only**.

#### MT5 Groups — exact columns

```text
Broker | Group | Accounts | Enabled for analysis | Plan mapping | Last discovered | Last synced
```

Filters: broker, enabled, plan, name contains. PATCH `{ enabledForAnalysis }` is SuperAdmin / RiskManager (A26) — Analyst+ in A63. Hide the toggle until the write endpoint exists.

#### Trader Leaderboard — 15 columns + URL filters

```text
Broker | Login | Group | Completed XAU trades
Net source P&L | Early score | ML probability | Risk score
Martingale | Averaging-down | Lot escalation
Current state | Shadow P&L | Live allocation | Last scored
```

Filters AND: broker, group, state (repeatable), min/max early, min/max risk, min completed XAU, martingale / averagingDown / lotEscalation, scoredFrom/To, login `q`. Default sort `earlyScore:desc`. Persist in the URL.

`mlProbability` is `null` until a promoted model exists — render `—`, never `0`.

`TraderStateBadge` uses §22 vocabulary (`INSUFFICIENT_DATA` … `DISQUALIFIED`). Overview rollup names (Watch, Shadow, …) stay on §47 only.

#### Trader Detail

| Block | UI | API |
|---|---|---|
| Account overview | KPI | `accountOverview` |
| XAU history | table; `isFirstThree` highlighted | `.../trades` |
| Score timeline | ECharts line | `.../score-history` |
| Risk flags | four explicit flags | `riskFlags` |
| Behavior | holding p50/p90, SL/TP, drawdown | `behaviorFeatures` |
| Lot timeline | ECharts | `.../lot-timeline` |
| Holding-time | ECharts histogram | `.../holding-time` |
| MFE/MAE | only if `featureQuality === EXACT` | else “unavailable” |
| Shadow / live books | tables | `.../shadow-positions`, `.../live-positions` |
| Source ↔ dest map | table | `.../source-destination-map` |
| Pause / resume | SuperAdmin / RiskManager + reason | `POST .../copy-control` — **after** RBAC |

Score formulas stay on the server (A22). React only plots returned numbers.

#### cTrader FIX — two independent cards

Never share sequence. Never render password / RawData / tag 96. `instrumentId` is discovered — UI never hardcodes Pepperstone `55`.

QUOTE extra: mapped?, instrument ID, bid, ask, quote age, spread.  
TRADE extra: execution enabled?, open orders, dest positions, last ER, last recon. While TRADE is not in first-useful, the card still renders with `sessionStatus: DISABLED` / `executionEnabled: false`.

#### Risk

Account equity/balance/free margin/margin level (or `available: false`); daily P&L; drawdown; XAU long/short/net/gross; risk by trader; risk by source broker; rejected intents with `reasonCode`; `STOP_NEW_EXECUTION`; `EMERGENCY_FLATTEN` **availability** (button disabled / omitted in §69).

Do not collapse stop-new, flatten, and real-copy into one toggle.

#### Reconciliation

Header: last MT5 success, last cTrader success. Every issue type visible even at 0. Table of open issues. No silent drop. Banner when cTrader is not `READY_FOR_EXECUTION`.

#### Other pages

| Page | Widgets |
|---|---|
| Login | email + dashboard password; no Query cache of password |
| Trade Explorer | same row as detail trades; filters broker/login/from/to/side/minPnl; default `canonicalSymbol=XAUUSD`; first-3 mark |
| Scoring | baseline name + outputs; last run; `countsByState` bar (ECharts); **no weights** |
| Models | version table; empty is correct; **no promote dialog in §69** |
| Shadow | pnl, open/closed counts, positions with dest quote + `quoteAgeMs` (QUOTE, not MT5 last-deal) |
| Live | same + `realCopyExecutionEnabled` + dest ids / `clOrdId` as strings |
| System Health | MT5 / reconstruction / scoring / FIX groups from §58 / A63; no host creds |
| Audit | filterable log; sanitized `before`/`after`; ReadOnly 403 |
| Settings | execution flags (read), symbol mapping **names**, FIX **non-secret** host/ports/comp ids. **No password field.** |

### 9.5 Target folder tree

Reuse `apps/web`. Do **not** create a second frontend. Adapt A62:

```text
D:\Prop\apps\web\
  index.html
  package.json
  vite.config.ts
  tsconfig.json                 # paths: "@/*" → "src/*"
  .env.example                  # VITE_API_URL=
  src\
    main.tsx
    App.tsx
    index.css
    vite-env.d.ts
    routes\paths.ts             # PATHS + NAV_ITEMS
    auth\                       # AuthProvider, RequireAuth, RequireRole, useAuth, can.ts
    api\                        # client, queryKeys, errors, one fetcher file per domain
    hubs\                       # opsHub.ts, useOpsHub.ts, invalidate.ts
    types\                      # allow-list DTOs + string unions
    pages\                      # one folder per §46 item (see A62 §5)
    layouts\                    # DashboardLayout, Sidebar, HeaderStrip, PageHeader
    components\
      ui\                       # Button, Badge, DataTable, EmptyState, ErrorState, Modal, ConfirmPhraseModal, KpiCard, HealthDot
      charts\                   # EChart.tsx + dark theme
      traders\                  # TraderStateBadge, ScoreCell, FirstThreeMark
      secrets\denylist.ts
    stores\uiStore.ts           # ephemeral only
    lib\                        # format.ts, ids.ts, searchParams.ts
```

**Do not create:** `src/mocks/`, `src/features/` (duplicate of pages), `e2e/` as a §69 gate, `public/secrets*`.

Existing `utils/` is not in the target tree — use `lib/`. Leave `utils/` empty or delete it in the coding wave; do not keep both.

### 9.6 Query keys (copy from A62; do not invent a third set)

```text
auth.me                         ['auth', 'me']
overview                        ['overview']
brokers                         ['brokers']
groups(filters)                 ['mt5-groups', filters]
traders(filters)                ['traders', filters]
trader(brokerId, login)         ['trader', brokerId, login]
traderTrades(...)               ['trader', brokerId, login, 'trades', page]
trades(filters)                 ['trades', filters]
scoring                         ['scoring']
models                          ['models']
shadow                          ['shadow']
live                            ['live']
fix                             ['fix']
risk                            ['risk']
reconciliation                  ['reconciliation']
health                          ['health']
audit(filters)                  ['audit', filters]
settings                        ['settings']
```

### 9.7 Types (wire unions)

```ts
export type TraderState =
  | 'INSUFFICIENT_DATA' | 'EARLY_SCORE' | 'WATCH' | 'SHADOW'
  | 'LIVE_CANDIDATE' | 'LIVE' | 'PAUSED' | 'RISK_BLOCKED' | 'DISQUALIFIED';

export type HealthStatus = 'HEALTHY' | 'DEGRADED' | 'UNHEALTHY' | 'STALE' | 'UNKNOWN';
export type Role = 'SuperAdmin' | 'RiskManager' | 'Analyst' | 'ReadOnly';
export type FeatureQuality = 'EXACT' | 'APPROXIMATE' | 'UNAVAILABLE';
```

Do not import C# numeric enums into TS.

Client denylist (defense in depth; server sanitizer remains mandatory):

```text
(?i)(password|passwd|secret|pwd|rawdata|connectionstring|privatekey|proxyuser)
```

If a GET or hub payload matches: do not render, do not cache, show a generic error.

---

## 10. Implementation sequence (later authorized wave)

Do **not** start this from B10.

| Step | Deliver | Unblocks | API dependency |
|---|---|---|---|
| S0 | Types + `queryKeys` + `client` + denylist + `can.ts` + `paths.ts` + `.env.example` + Vite proxy | compile | none |
| S1 | AuthProvider + Login + shell (exact §46 labels + header chips with ErrorState) | RBAC UI | login/me **or** 401 empty-safe shell |
| S2 | Overview 18 tiles + Brokers + Groups + Leaderboard + Detail **tables** (no charts) + Trade Explorer | §69.12 core | `/api/v1/overview`, `/brokers`, `/mt5/groups`, `/traders`, `/trades` |
| S3 | Scoring summary + System Health + Settings **read** + Shadow/Live empty-safe + Models empty list | rest of §46 reads | `/scoring/summary`, `/health`, `/settings`, portfolios, `/models` |
| S4 | ECharts on Detail + Scoring state histogram | §51 widgets | score-history / lot / holding |
| S5 | FIX two cards + Reconciliation issue types + Risk **read** tiles | §52–54 | `/fix/sessions`, `/reconciliation`, `/risk/dashboard` |
| S6 | Privileged mutations that are **in** first-useful: group `enabledForAnalysis`, trader pause/resume, stop-new (if A63 later allows). Audit page | A51 | write endpoints + audit GET |
| S7 | SignalR `/hubs/ops` invalidation; delete stub `hooks.ts` / `signalr.ts`; drop `recharts`; `npm ci` lockfile | live header | `/hubs/ops` |

**First useful React (§69.12, A28 Phase 3–5):** S0–S3 plus FIX QUOTE tile (S5 QUOTE half) plus honest empty shadow book. Charts can trail. Models promote, live flatten, enable-real are **not** Phase 3/§69 exit.

**API blocker:** until `/api/v1/overview` exists with the A26 nest, the UI must fail closed or show `ErrorState`. Do not keep painting the flat demo DTO as if it were A26. A temporary adapter is allowed only if labeled `COMPAT` and deleted in the same PR that lands `/api/v1`.

---

## 11. What not to build in the web wave

From §71 / A80 / A63 / this gap:

| Item | Reason |
|---|---|
| Kafka / K8s / ClickHouse / LLM chat / DNN / RL UIs | §71 |
| Second frontend (`apps/dashboard`, Next.js) | §66 adapt existing |
| `emergency-flatten` button in §69 | A63 / §70 |
| Enable-real-execution control in §69 | §41 default false |
| Model self-promote / auto-promote widget | §71 |
| Settings password / proxy / FIX RawData fields | §55 — **422** if sent |
| Hardcoded cTrader instrument id `55` | §30 / §72.13 |
| Client-side scoring / predicted price / candlesticks | A22 / A62 |
| Fabricated MFE/MAE / ML probability `0` | A45 / A52 |
| `localStorage` role or JWT | A26 §11 |
| `GET /weatherforecast` consumer | template leftover |
| `POST /api/ops/resync` button | unaudited demo |
| MSW fake leaderboard scores | fail closed instead |

Absence of those UIs is **EXISTS_AND_GOOD**.

---

## 12. Stale swarm documents (do not copy their web census)

These files were true earlier on 2026-08-18 and are **wrong on `apps/web` existence / page count / API consumer** now:

| File | Stale claim | Current measured |
|---|---|---|
| A06 | `apps/web` absent; API is weatherforecast only | web exists; API has unversioned `/api/*` demo |
| A11 / A29 | `apps/web` MISSING | folder + 13 pages |
| A57 §69.12 | `src/pages` = 0 files; `tsc` fails on missing imports | 13 pages exist; App imports resolve |
| A62 §0 / §3 | empty `pages/` / `layouts/` / `types/` | filled |
| A91 §0 | Overview page MISSING; `IDashboardQueries` unimplemented | `OverviewPage.tsx` + `EfDashboardQueries` exist |

A26 / A62 **contracts and target tree** remain binding. Only their “does not exist yet” sentences are stale.

---

## 13. Honesty metrics

| Metric | Value |
|---|---|
| `apps/web` exists? | **Yes** — `D:\Prop\apps\web` |
| §46 nav items with a route | 12 / 16 (Detail is a child route; Models / Live / Audit / Login missing) |
| Page `.tsx` files | 13 |
| Pages that are more than a table or `<pre>` | Overview, Brokers, Groups, Traders, Detail, FIX, Risk, Scoring, Trade Explorer — all still **thin** |
| Pages that hit a real query (not hardcoded / static) | 10 (Shadow is static; Health/Recon/Settings dump demo JSON) |
| A26 endpoints consumed | **0** |
| ECharts / Recharts components in `src` | **0** |
| Auth screens | **0** |
| React tests | **0** |
| Lockfile / `node_modules` | **0** |
| Secrets rendered by current pages | **0 observed** (mask bug is not a password leak) |
| §69.12 | **FAIL** |

Classification of `apps/web` as a whole: **EXISTS_NEEDS_REFACTOR**.

---

## 14. Acceptance for this plan (and for a future web PR)

Plan-level (this document):

- [x] Existence of `D:\Prop\apps\web` recorded with file sizes and dates.
- [x] Every §46 nav item has a target route and a page module path.
- [x] §47–§54 widget lists mapped to current stubs **and** A26 JSON.
- [x] Current tree classified honestly (refactor, not “done”, not “missing”).
- [x] First-useful vs later-phase mutations split.
- [x] Product source not modified.

Future PR (not this task):

- [ ] `npm ci` + `npm run build` (`tsc && vite build`) succeeds.
- [ ] No `recharts` import remains.
- [ ] No password / proxy / FIX RawData field in `src/types`.
- [ ] No `localStorage` role.
- [ ] Trader URLs are `/traders/:brokerId/:login` with UUID `brokerId`.
- [ ] Nav labels match §46 exactly; groups live at `/mt5-groups`.
- [ ] Overview paints all 18 tiles (honest nulls allowed).
- [ ] First-3 highlight exists on Detail + Trade Explorer.
- [ ] Confirm phrases required on flatten / enable-live / promote **when those UIs are later added**.
- [ ] Authenticated ReadOnly can see both brokers, groups, reconstructed XAU, first-3, deterministic ranks, QUOTE instrument + age, shadow P&L — from persisted data, no secrets in JSON.

---

## 15. What this agent did **not** do

- Did not create, edit, or delete any file under `D:\Prop\apps\web`, `D:\Prop\apps\api`, or `D:\Prop\src`.
- Did not run `npm install` / `npm run build` (no lockfile; would mutate the tree).
- Did not add `echarts` / `zustand` to `package.json`.
- Did not implement pages, hubs, or API controllers.
- Did not invent dashboard JSON beyond A26 / A63 / A91.
- Did not claim EX5 / backend decompile progress (out of scope).
- Did not mark §69.12 done.

---

*End of B10. Product source was not modified.*
