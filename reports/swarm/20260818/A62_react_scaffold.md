# A62 — React Dashboard Scaffold Plan (`apps/web`)

**Artifact:** `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md`  
**Date:** 2026-08-18  
**Agent:** A62  
**Status:** Planning only. **No product source was modified.**  
**Scope:** Folder tree, routes, page inventory, data-layer layout, and implementation sequence for the operational dashboard. This file does **not** authorize creating or rewriting `apps/web` source.

| Field | Value |
|---|---|
| Product tree | `D:\Prop\apps\web` |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§§46–54** (nav + page widgets); supporting **§5, §10, §41, §55, §58, §59, §66, §69.12, §72.5/72.19** |
| API / RBAC contract | `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` (**binding** for paths, JSON, roles) |
| Earlier API sketch | `D:\Prop\reports\swarm\20260818\A06_api_audit.md` (stale on `apps/web` existence; superseded by A26 on routes/roles) |
| Phases | A28 Phase 3 ships the trader dashboard; Phase 4–5 add FIX + shadow tiles; live flatten / real copy stay gated |

---

## 0. Verdict

`apps/web` is a **broken Vite stub**, not a §46–54 dashboard.

Honest measured state (disk, 2026-08-18):

| Check | Result |
|---|---|
| Vite + React 18 + TS + Tailwind host | **PRESENT** (`package.json`, `vite.config.ts`, `index.html`) |
| TanStack Query + React Router wired in `main.tsx` | **PRESENT** |
| SignalR helper | **PARTIAL** — file exists, **wrong hub path** (`/hubs/dashboard` vs A26 `/hubs/ops`) |
| Page modules imported by `App.tsx` | **MISSING** — 13 imports, 0 page files |
| Layout / components / types / `api/client.ts` | **MISSING** (empty dirs; hooks import a client that does not exist) |
| Login, Models, Live Copy, Audit routes | **MISSING** |
| ECharts | **MISSING** — `package.json` pins **Recharts**, not ECharts |
| Auth / RBAC / secret-safe client | **MISSING** |
| Talks to real API | **NO** — `apps/api` is still `GET /weatherforecast` only (A06) |
| §69.12 “show all of this in React” | **FAIL** — cannot render any required widget |

Do not treat `App.tsx` import names as a finished scaffold. A later coding wave must **replace** the stub against this plan + A26, not grow the broken hooks as-is.

---

## 1. Binding sources and precedence

When documents disagree, implementers use this order:

1. Architecture law (`…Architecture_v2.md`) for **what** must appear (nav labels, widgets, no secrets).
2. **A26** for **how** the browser talks to the API (paths, envelopes, query keys, RBAC, SignalR event names).
3. This file (A62) for **where** those pages live under `apps/web`.
4. A06 / A11 are **audit snapshots**. A06 still says `apps/web` is absent and proposes some different paths (`/api/v1/risk/snapshot`, `/traders/{traderId}`). **Do not implement A06 paths.**

Resolved conflicts (do not re-litigate in UI code):

| Topic | Wrong / stale | Binding |
|---|---|---|
| API prefix | stub `GET /api/overview` | A26 `GET /api/v1/...` |
| Groups route | stub `/groups` | A26 `/mt5-groups` |
| Trader identity | A06 `{traderId}` | A26 `/traders/{brokerId}/{login}` (§10) |
| Manager-login mask | A06 `2027` → `20**` | A26 keep **last two** digits (`**27`) |
| Hub | stub `/hubs/dashboard` | A26 `/hubs/ops` |
| Risk GET | stub `/api/risk/status` | A26 `/api/v1/risk/dashboard` |
| Group `enabledForAnalysis` write | A06 Analyst+ | A26 SuperAdmin **or** RiskManager |
| Charts | current `recharts` | **ECharts** (this task + §5 “ECharts or Recharts”) |
| Settings write | stub `PUT /api/settings` whole object | A26 PATCHes of **non-secret** slices only |
| Flatten / promote / enable-live | stub POST with no confirm | A26 typed `confirmPhrase` + audit |

---

## 2. Stack (normative for the next coding wave)

Architecture §5 + this assignment:

```text
React 18
TypeScript (strict)
Vite 5
TanStack Query v5
React Router v6
@microsoft/signalr
ECharts + echarts-for-react
Tailwind CSS 3
```

| Library | Role | Rule |
|---|---|---|
| TanStack Query | **All** server state | Query keys from §8. Invalidate from SignalR. Do not store GET payloads in Zustand. |
| React Router | URL is source of filters where shareable | Leaderboard filters live in search params. |
| SignalR | Live header + page tiles | Hub `/hubs/ops`. JWT via negotiate header or `access_token` query. Same sanitizer as REST. |
| ECharts | Score / lot / holding-time / state histograms | One wrapper. No Recharts in new code. Remove `recharts` when the wave lands. |
| Zustand | **Ephemeral UI only** | Confirm-modal open, sidebar collapsed, connection banner. §5: “only when genuinely needed.” |
| Axios **or** `fetch` | One HTTP client | Stub already imported axios in `hooks.ts`. Either is fine; pick **one** in `api/client.ts`. |
| MSW / Storybook / Next.js / Redux | — | **Do not add.** §71 / §72.20. |

Vite dev server stays on **port 3000**. Proxy `/api` and `/hubs` to the API (`https://localhost:7294` or `http://localhost:5160`). Do not put MT5/FIX/DB secrets in `VITE_*`.

`.env.example` (placeholders only, §55):

```text
VITE_API_URL=
```

Empty `VITE_API_URL` means “same origin / Vite proxy”. Never a password, never a connection string.

---

## 3. What exists on disk today (do not greenwash)

Complete tree under `D:\Prop\apps\web` (recursive listing, 2026-08-18):

```text
apps/web/
  index.html                          369 B   dark shell, title "MT5 Trader Intelligence"
  package.json                        739 B
  postcss.config.js
  tailwind.config.js
  tsconfig.json                       strict, include src only
  tsconfig.node.json
  vite.config.ts                      port 3000, no proxy, no alias
  src/
    App.tsx                           routes 13 pages; files not on disk
    main.tsx                          QueryClient + BrowserRouter
    index.css                         Tailwind directives only
    api/
      hooks.ts                        3042 B  imports missing ./client and ../types
      signalr.ts                      899 B   /hubs/dashboard, no JWT
    components/                       EMPTY
    layouts/                          EMPTY
    pages/                            EMPTY
    types/                            EMPTY
    utils/                            EMPTY
```

`package.json` dependencies (measured):

```text
react ^18.3.1
react-dom ^18.3.1
react-router-dom ^6.26.0
@tanstack/react-query ^5.51.0
axios ^1.7.0
recharts ^2.12.0
@microsoft/signalr ^8.0.0
```

Dev: Vite 5, TS 5.5, Tailwind 3.4, `@vitejs/plugin-react`. **No** `echarts`, **no** `zustand`, **no** `@tanstack/react-query-devtools`, **no** test runner.

`App.tsx` current route table (broken — targets do not exist):

| Path | Import | §46 label? |
|---|---|---|
| `/` → `/overview` | `OverviewPage` | Overview |
| `/overview` | | Overview |
| `/brokers` | `BrokersPage` | Brokers |
| `/groups` | `GroupsPage` | **Wrong path** (must be `/mt5-groups`) |
| `/traders` | `TradersPage` | Traders |
| `/traders/:brokerId/:login` | `TraderDetailPage` | Trader Detail |
| `/trades` | `TradeExplorerPage` | Trade Explorer |
| `/scoring` | `ScoringPage` | Scoring |
| `/shadow` | `ShadowPortfolioPage` | Shadow Portfolio |
| `/fix` | `FixSessionsPage` | cTrader FIX |
| `/risk` | `RiskPage` | Risk |
| `/reconciliation` | `ReconciliationPage` | Reconciliation |
| `/health` | `SystemHealthPage` | System Health |
| `/settings` | `SettingsPage` | Settings |
| — | — | **Models missing** |
| — | — | **Live Copy Portfolio missing** |
| — | — | **Audit missing** |
| — | — | **Login missing** |

`hooks.ts` also encodes a **non-A26** contract (`/api/groups`, `/api/risk/status`, unconfirmed flatten, `PUT /api/settings`). Treat the file as **DEPRECATED** for the coding wave.

---

## 4. Gap vs architecture §§46–54

Status: **MISSING** | **STUB** | **WRONG** | **N/A**.

| § | Page | Widgets required by architecture | Status |
|---|---|---|---|
| 46 | Shell + 16 nav items | Exact labels; professional ops dashboard | **STUB** (router names only; 3 nav items absent) |
| 47 | Overview | Account/broker counts; XAU / ≥3-trade counts; Watch/Shadow/Live candidates/Live copied/Risk blocked; shadow P&L; dest real P&L; XAU gross/net; dest free margin + margin level; MT5 / FIX quote / FIX trade health | **MISSING** |
| 48 | Brokers | Per broker: display name, connection, server, **masked** manager login, group/account counts, ingest rate, last event, last history sync, pool usage, reconnects. **No secrets.** | **MISSING** |
| 49 | MT5 Groups | Broker, group, accounts, enabled for analysis, plan mapping, last discovered, last synced | **MISSING** |
| 50 | Trader Leaderboard | 15 columns + filters (broker, group, state, score, risk, trade count, martingale, date) | **MISSING** |
| 51 | Trader Detail | Account; XAU history with first-3 highlight; score timeline; risk flags; behavior; lot timeline; holding-time; SL/TP; drawdown; MFE/MAE when valid; shadow + live books; source↔destination map | **MISSING** |
| 52 | cTrader FIX | Independent QUOTE + TRADE cards; host/port/logon/seq/heartbeat/errors; QUOTE XAU map/bid/ask/age/spread; TRADE execution flag / open orders / dest positions / last ER / last recon. **Never FIX password.** | **MISSING** |
| 53 | Risk | Equity/balance/free margin/margin level; daily P&L; drawdown; XAU long/short/net; risk by trader; risk by source broker; rejected intents + reasons; `STOP_NEW_EXECUTION`; `EMERGENCY_FLATTEN` availability | **MISSING** |
| 54 | Reconciliation | Last MT5 recon; last cTrader recon; unknown external; missing internal; order/qty mismatches; orphan fills; unresolved execution states. Nothing unresolved silently ignored. | **MISSING** |

§46 also names pages that are not given widget lists in 47–54. They still belong in the tree (A26 §5.2 / §9):

| §46 label | Route | First-useful? |
|---|---|---|
| Trade Explorer | `/trades` | Yes (reconstructed XAU list) |
| Scoring | `/scoring` | Yes (baseline summary; no weights) |
| Models | `/models` | **Nav yes, promote later.** Empty list is honest. No self-promotion. |
| Shadow Portfolio | `/shadow` | Phase 5 data; page exists from Phase 3 as empty-safe |
| Live Copy Portfolio | `/live` | Always show `realCopyExecutionEnabled`; positions may be `[]` |
| System Health | `/health` | Yes (§58 projection, no host creds) |
| Audit | `/audit` | Yes for SuperAdmin/RiskManager/Analyst; ReadOnly 403 |
| Settings | `/settings` | Non-secret flags + symbol map names only |

---

## 5. Target folder structure

Adapt the existing repo (`§66`). Do **not** create a second frontend. One-folder-per-§46-item under `src/pages` (A26 §5.1).

```text
D:\Prop\apps\web\
  index.html
  package.json
  vite.config.ts                 # port 3000, /api+/hubs proxy, @ alias
  tsconfig.json                  # paths: "@/*" → "src/*"
  tsconfig.node.json
  tailwind.config.js
  postcss.config.js
  .env.example                   # VITE_API_URL=   (no secrets)
  src\
    main.tsx                     # QueryClient, Router, AuthProvider
    App.tsx                      # route table only
    index.css
    vite-env.d.ts
    routes\
      paths.ts                   # PATHS + NAV_ITEMS (exact §46 labels)
      index.tsx                  # optional RouteObject map
    auth\
      AuthProvider.tsx           # holds access token in memory
      RequireAuth.tsx
      RequireRole.tsx
      useAuth.ts
      useRole.ts                 # GET /auth/me only — never localStorage role
      can.ts                     # A26 §10 / §11 matrix
      session.ts                 # refresh via cookie; no refresh token in JS
    api\
      client.ts                  # Bearer, X-Correlation-Id, 401→refresh, envelope unwrap
      queryKeys.ts               # single source of keys
      errors.ts                  # ApiError from A26 envelope
      auth.ts
      overview.ts
      brokers.ts
      groups.ts
      traders.ts
      trades.ts
      scoring.ts
      models.ts
      shadow.ts
      live.ts
      copyIntents.ts
      fix.ts
      risk.ts
      reconciliation.ts
      health.ts
      audit.ts
      settings.ts
    hubs\
      opsHub.ts                  # HubConnection to /hubs/ops
      useOpsHub.ts               # start/stop with token
      useHeaderLive.ts           # ops.header → query cache
      invalidate.ts              # event → queryClient.invalidateQueries
    types\
      enums.ts                   # traderState, healthStatus, issue types, …
      envelopes.ts
      auth.ts
      overview.ts
      brokers.ts
      groups.ts
      traders.ts
      trades.ts
      scoring.ts
      models.ts
      shadow.ts
      live.ts
      fix.ts
      risk.ts
      reconciliation.ts
      health.ts
      audit.ts
      settings.ts
      copyIntents.ts
    pages\
      login\
        LoginPage.tsx
      overview\
        OverviewPage.tsx
        OverviewKpiGrid.tsx
        OverviewHealthRow.tsx
      brokers\
        BrokersPage.tsx
        BrokerCard.tsx
      mt5-groups\
        Mt5GroupsPage.tsx
      traders\
        TraderLeaderboardPage.tsx
        TraderFilters.tsx
        TraderTable.tsx
      trader-detail\
        TraderDetailPage.tsx
        AccountOverview.tsx
        TradeHistoryTable.tsx
        ScoreTimelineChart.tsx
        LotTimelineChart.tsx
        HoldingTimeChart.tsx
        RiskFlagsPanel.tsx
        BehaviorFeaturesPanel.tsx
        ShadowLiveBooks.tsx
        SourceDestinationMap.tsx
        CopyControl.tsx
      trades\
        TradeExplorerPage.tsx
      scoring\
        ScoringPage.tsx
      models\
        ModelsPage.tsx
        PromoteModelDialog.tsx
      shadow\
        ShadowPortfolioPage.tsx
      live\
        LiveCopyPortfolioPage.tsx
      fix\
        CTraderFixPage.tsx
        QuoteSessionCard.tsx
        TradeSessionCard.tsx
        FixEventLog.tsx
      risk\
        RiskDashboardPage.tsx
        ExposurePanels.tsx
        RejectedIntentsTable.tsx
        RiskLimitsForm.tsx
        StopNewExecutionControl.tsx
        EmergencyFlattenControl.tsx
      reconciliation\
        ReconciliationPage.tsx
        IssueTable.tsx
      health\
        SystemHealthPage.tsx
      audit\
        AuditPage.tsx
      settings\
        SettingsPage.tsx
        ExecutionFlagsForm.tsx
        SymbolMappingTable.tsx
        FixNonSecretForm.tsx
    layouts\
      DashboardLayout.tsx        # left nav + header strip + <Outlet/>
      Sidebar.tsx
      HeaderStrip.tsx            # five live flags/healths
      PageHeader.tsx
    components\
      ui\
        Button.tsx
        Badge.tsx
        DataTable.tsx
        Pagination.tsx
        EmptyState.tsx
        ErrorState.tsx
        Spinner.tsx
        Modal.tsx
        ConfirmPhraseModal.tsx   # type the action name
        KpiCard.tsx
        HealthDot.tsx
      charts\
        EChart.tsx               # thin echarts-for-react wrapper
        theme.ts                 # dark ops theme; no business formulas
      traders\
        TraderStateBadge.tsx
        ScoreCell.tsx
        FirstThreeMark.tsx
      secrets\
        denylist.ts              # client fail-closed if a denylisted key appears
    stores\
      uiStore.ts                 # sidebar, toasts, modal
    lib\
      format.ts                  # UTC timestamps, money, qty
      ids.ts                     # tickets / clOrdId stay string
      searchParams.ts            # leaderboard filter codec
```

**Do not create** in this scaffold wave:

```text
src/mocks/          unless API is still weatherforecast and a coder is blocked — prefer empty-safe pages
src/features/       duplicate of pages/
e2e/                later; A27 does not require React component tests for §69.12
public/secrets*     never
```

Existing empty dirs (`components`, `layouts`, `pages`, `types`, `utils`) are reused. `utils/` is **not** in the target tree — use `lib/` so formatters stay out of React. Leave `utils/` empty or delete it in the coding wave; do not keep both.

---

## 6. Route map (A26 §5.2 + architecture §46)

Nav labels **exactly** as §46. Default authenticated landing: `/overview`. Unknown routes → `/overview`. `/login` is outside the shell.

| Route | Page module | Architecture | Primary GET |
|---|---|---|---|
| `/login` | `pages/login/LoginPage.tsx` | §59 | `POST /api/v1/auth/login` |
| `/overview` | `pages/overview/OverviewPage.tsx` | **§47** | `GET /api/v1/overview` |
| `/brokers` | `pages/brokers/BrokersPage.tsx` | **§48** | `GET /api/v1/brokers` |
| `/mt5-groups` | `pages/mt5-groups/Mt5GroupsPage.tsx` | **§49** | `GET /api/v1/mt5/groups` |
| `/traders` | `pages/traders/TraderLeaderboardPage.tsx` | **§50** | `GET /api/v1/traders` |
| `/traders/:brokerId/:login` | `pages/trader-detail/TraderDetailPage.tsx` | **§51** | `GET /api/v1/traders/{brokerId}/{login}` |
| `/trades` | `pages/trades/TradeExplorerPage.tsx` | §46 | `GET /api/v1/trades` |
| `/scoring` | `pages/scoring/ScoringPage.tsx` | §46, §18, §22 | `GET /api/v1/scoring/summary` |
| `/models` | `pages/models/ModelsPage.tsx` | §46, §21 | `GET /api/v1/models` |
| `/shadow` | `pages/shadow/ShadowPortfolioPage.tsx` | §46, §24 | `GET /api/v1/shadow/portfolio` |
| `/live` | `pages/live/LiveCopyPortfolioPage.tsx` | §46, §32–35 | `GET /api/v1/live/portfolio` |
| `/fix` | `pages/fix/CTraderFixPage.tsx` | **§52** | `GET /api/v1/fix/sessions` |
| `/risk` | `pages/risk/RiskDashboardPage.tsx` | **§53** | `GET /api/v1/risk/dashboard` |
| `/reconciliation` | `pages/reconciliation/ReconciliationPage.tsx` | **§54** | `GET /api/v1/reconciliation` |
| `/health` | `pages/health/SystemHealthPage.tsx` | §46, §58 | `GET /api/v1/health` |
| `/audit` | `pages/audit/AuditPage.tsx` | §46, §59 | `GET /api/v1/audit` |
| `/settings` | `pages/settings/SettingsPage.tsx` | §46, §41, §55 | `GET /api/v1/settings` |

`paths.ts` must export both the path constants and the sidebar list so labels cannot drift.

Suggested `NAV_ITEMS` (order = §46):

```text
Overview              /overview
Brokers               /brokers
MT5 Groups            /mt5-groups
Traders               /traders
Trader Detail         (not a sidebar leaf — opened from leaderboard)
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

Trader Detail is in the §46 list but is a **detail route**, not a dead sidebar link. Sidebar highlights **Traders** when the URL is `/traders/:brokerId/:login`.

ReadOnly visiting `/audit` → API 403 → page `ErrorState` (“role cannot read audit”), not a fake empty table.

---

## 7. Shell (every authenticated page)

### 7.1 Left nav

- All 15 sidebar items visible to every authenticated role (A26 §5.3).
- **Hiding buttons is UX only.** API is the authority.
- Active item: exact path match; prefix match for `/traders`.

### 7.2 Header strip (architecture health + §41 flags)

Always visible. Source: `GET /api/v1/overview` `flags` + `health`, kept hot by hub `ops.header`.

| Chip | Field | Visual |
|---|---|---|
| Real copy | `flags.realCopyExecutionEnabled` | **OFF by default.** Red when true. |
| Stop-new | `flags.stopNewExecution` | Amber when true. Not the same chip as flatten. |
| MT5 ingest | `health.mt5Ingestion.status` | HealthDot |
| FIX QUOTE | `health.fixQuote.status` | HealthDot |
| FIX TRADE | `health.fixTrade.status` | HealthDot |

`healthStatus`: `HEALTHY` | `DEGRADED` | `UNHEALTHY` | `STALE` | `UNKNOWN`.

Do **not** add a third chip that conflates `STOP_NEW_EXECUTION`, `EMERGENCY_FLATTEN`, and `REAL_COPY_EXECUTION_ENABLED` (A26 §10.3).

### 7.3 User menu

`displayName`, `role` from `GET /api/v1/auth/me`. Logout → `POST /api/v1/auth/logout`.

---

## 8. TanStack Query keys and fetchers

A26 §13.6 plus the rest of the matrix. Define **only** in `api/queryKeys.ts`.

```text
auth.me                         ['auth', 'me']
overview                        ['overview']
brokers                         ['brokers']
broker(id)                      ['brokers', id]
groups(filters)                 ['mt5-groups', filters]
traders(filters)                ['traders', filters]
trader(brokerId, login)         ['trader', brokerId, login]
traderTrades(...)               ['trader', brokerId, login, 'trades', page]
traderScoreHistory(...)         ['trader', brokerId, login, 'score-history']
traderLotTimeline(...)          ['trader', brokerId, login, 'lot-timeline']
traderHoldingTime(...)          ['trader', brokerId, login, 'holding-time']
traderShadowPos(...)            ['trader', brokerId, login, 'shadow-positions']
traderLivePos(...)              ['trader', brokerId, login, 'live-positions']
traderMap(...)                  ['trader', brokerId, login, 'source-destination-map']
trades(filters)                 ['trades', filters]
scoring                         ['scoring']
models                          ['models']
shadow                          ['shadow']
live                            ['live']
copyIntents(filters)            ['copy-intents', filters]
fix                             ['fix']
fixEvents(session, page)        ['fix', session, 'events', page]
risk                            ['risk']
reconciliation                  ['reconciliation']
reconIssues(filters)            ['reconciliation', 'issues', filters]
health                          ['health']
audit(filters)                  ['audit', filters]
settings                        ['settings']
```

Rules:

- Default `staleTime` 30s (already in `main.tsx`) is fine for lists.
- **Do not** `refetchInterval` secrets. Stub `useFixSessions` / `useRiskStatus` poll every 5s — replace with SignalR invalidation + a slower fallback (e.g. 15–30s) if the hub is down.
- Tickets, `clOrdId`, destination position ids: **string** in TS. Never `number`.
- Login in the path is the MT5 login integer as decimal text; compound identity is always `(brokerId, login)`.
- Mutations send `Idempotency-Key` and `reason` where A26 requires them.
- On 401: try `POST /api/v1/auth/refresh` (cookie); then `/login`.
- On 422 `SECRET_FIELD_REJECTED`: surface the error; do not retry with the same body.

Fetcher files map 1:1 to A26 §6. Do not put widget math in fetchers.

---

## 9. SignalR (`src/hubs`)

| Item | Contract |
|---|---|
| URL | `{API}/hubs/ops` |
| Auth | same JWT as REST |
| Reconnect | `withAutomaticReconnect` |
| Sanitizer | if a denylisted key appears in a frame, drop the frame and log `SECRET_FIELD_REJECTED` client-side (fail closed) |

| Event | Invalidate / setQueryData |
|---|---|
| `ops.header` | merge into `['overview']` `flags` + `health`; HeaderStrip |
| `overview.updated` | `setQueryData(['overview'], data)` |
| `broker.health` | patch that row in `['brokers']` |
| `fix.session` | `['fix']` + header |
| `quote.xauusd` | `['fix']` quote card; Risk/Shadow mark |
| `risk.state` | `['risk']` controls + header flags |
| `reconciliation.issue` | `['reconciliation']` / issues |
| `trader.score` | `['traders', …]` row + `['trader', brokerId, login]` |

REST polling of `/overview` is enough to **paint** first useful (A06 §4.14). The hub is required for a professional ops shell, not a §69 gate. Scaffold the module anyway so pages do not invent a second websocket.

---

## 10. Page plans (§47–§54 first, then remaining §46)

Empty-safe rule for every page: if the API is still weatherforecast, show `ErrorState` with `correlationId`. **Do not invent** scores, instrument IDs, MFE/MAE, or live P&L. Null / empty / 0 must mean what A26 says (0 dest P&L while execution is off is allowed; fabricated MFE is not).

### 10.1 Overview — §47

**File:** `pages/overview/OverviewPage.tsx`  
**GET:** `/api/v1/overview`  
**Hub:** `overview.updated`, `ops.header`  
**Mutations:** none

Widget → JSON (must all render; use `—` only when the field is legitimately null):

| Widget (architecture text) | DTO path |
|---|---|
| Total MT5 accounts | `accounts.totalMt5Accounts` |
| Connected source brokers | `accounts.connectedSourceBrokers` |
| XAUUSD traders | `accounts.xauusdTraders` |
| Traders with ≥ 3 completed trades | `accounts.tradersWithMinThreeCompletedTrades` |
| Watch / Shadow / Live candidates / Live copied / Risk blocked | `traderStates.*` |
| Shadow P&L | `pnl.shadowPnl` + `pnl.currency` |
| Destination real P&L | `pnl.destinationRealPnl` (0 while live off) |
| Current XAU gross / net | `exposure.grossQuantity` / `netQuantity` |
| Destination free margin / margin level | `exposure.destinationFreeMargin` / `destinationMarginLevel` |
| MT5 ingestion / FIX quote / FIX trade health | `health.*` |

Layout: 4-count row; 5-state row; P&L pair; exposure + margin; health row (duplicates header with detail strings). No chart required on Overview.

### 10.2 Brokers — §48

**File:** `pages/brokers/BrokersPage.tsx`  
**GET:** `/api/v1/brokers` (+ optional detail)  
**PATCH:** `/api/v1/brokers/{id}` SuperAdmin only (`displayName`, `enabledForIngest`, `poolSize`)

One card per source broker (Achiever, StarwaveFX, …):

| Architecture field | DTO |
|---|---|
| Display name | `displayName` |
| Connection status | `connectionStatus` |
| Server | `server` + `port` + `useSsl` + `serverName` |
| Manager login masked | `managerLoginMasked` only |
| Group / account count | `groupCount` / `accountCount` |
| Deal ingest rate | `dealIngestPerMinute` |
| Last event | `lastEventAt` |
| Last successful history sync | `lastSuccessfulHistorySyncAt` |
| Connection pool usage | `pool.size` / `inUse` / `idle` |
| Reconnect count | `reconnectCount` |

**Forbidden on screen and in types:** `password`, raw `managerLogin`, `proxyUsername`, `proxyPassword`. Proxy block may show `enabled`, `host`, `port`, `credentialConfigured`. SuperAdmin `?revealLogin=true` still returns **login integer only**, never the password (A26 §3.2).

### 10.3 MT5 Groups — §49

**File:** `pages/mt5-groups/Mt5GroupsPage.tsx`  
**GET:** `/api/v1/mt5/groups?brokerId&enabledForAnalysis&plan&q`  
**PATCH:** `/api/v1/mt5/groups/{groupId}` `{ enabledForAnalysis }` — SuperAdmin or RiskManager

Columns **exactly**:

```text
Broker | Group | Accounts | Enabled for analysis | Plan mapping | Last discovered | Last synced
```

`planMapping` nullable. Discovery is not limited to known plan maps (§9). Filter bar: broker, enabled, plan, name contains.

### 10.4 Trader Leaderboard — §50

**File:** `pages/traders/TraderLeaderboardPage.tsx`  
**GET:** `/api/v1/traders` with A26 filter query  
**Row click:** `/traders/{brokerId}/{login}`

Columns:

```text
Broker | Login | Group | Completed XAU trades
Net source P&L | Early score | ML probability | Risk score
Martingale | Averaging-down | Lot escalation
Current state | Shadow P&L | Live allocation | Last scored
```

Filters (AND): broker, group, state (repeatable), min/max early score, min/max risk, min completed XAU trades, martingale / averagingDown / lotEscalation, scoredFrom/To, login `q`. Default sort `earlyScore:desc`. Persist filters in the **URL**.

`mlProbability` is `null` until a promoted model exists — render `—`, never `0`.

`TraderStateBadge` uses the §22 vocabulary (`INSUFFICIENT_DATA` … `DISQUALIFIED`). Overview rollup names (Watch, Shadow, …) are labels only on §47.

### 10.5 Trader Detail — §51

**File:** `pages/trader-detail/TraderDetailPage.tsx`  
**GET:** detail + sub-resources in A26 §6.5  
**POST:** `/copy-control` `{ action: PAUSE|RESUME, reason }` — SuperAdmin or RiskManager

| Architecture block | UI | API |
|---|---|---|
| Account overview | KPI: balance, equity, currency, leverage, asOf | `accountOverview` |
| XAU trade history | table; `isFirstThree` highlighted | `.../trades` |
| First 3 trades highlighted | row style + `FirstThreeMark` | `firstThreeTradesHighlighted` + per-row flag |
| Score timeline | **ECharts** line: early / behavior / risk / ml | `.../score-history` |
| Risk flags | four explicit flags | `riskFlags` |
| Behavior features | holding p50/p90, SL/TP rates, drawdown | `behaviorFeatures` |
| Lot-size timeline | **ECharts** step/bar of volume vs time | `.../lot-timeline` |
| Holding-time distribution | **ECharts** histogram | `.../holding-time` |
| SL/TP behavior | rates + hadSl/hadTp on trades | features + trades |
| Drawdown | number from features | `behaviorFeatures.drawdown` |
| MFE/MAE when valid | show only if `featureQuality === EXACT`; else “unavailable” | `mfe`/`mae` null + `UNAVAILABLE` — **never fabricate** (§17) |
| Shadow copied positions + P&L | book table | `.../shadow-positions` + `shadow` |
| Live copied positions + P&L | book table (may be empty) | `.../live-positions` + `live` |
| Source-to-destination mapping | table of trade → intent → dest ids | `.../source-destination-map` |

Score formulas stay on the server (A22). React only plots returned numbers.

### 10.6 cTrader FIX — §52

**File:** `pages/fix/CTraderFixPage.tsx`  
**GET:** `/api/v1/fix/sessions` + `.../{session}/events`  
**Mutations:** none (non-secret FIX config is Settings)

Two **independent** cards. Never share sequence or “one session with a qualifier.”

Common fields: Host, SSL Port, Connected?, Logged on?, Session status, Last inbound, Last outbound, Message sequence state, Reconnect count, Last heartbeat / test request, Errors.

QUOTE extra: XAUUSD mapped?, Instrument ID, Bid, Ask, Quote age, Spread. Instrument ID is discovered (§30) — UI never hardcodes Pepperstone `55`.

TRADE extra: Execution enabled?, Open orders, Open destination positions, Last execution report, Last reconciliation.

`secretConfigured: true|false` is the only secret metadata. **Never** render password, RawData, tag 96.

`sessionStatus` union: `DOWN` | `CONNECTING` | `LOGGED_ON` | `LOGGED_OUT` | `RECONNECTING` | `SEQUENCE_RESET` | `ERROR`.

### 10.7 Risk Dashboard — §53

**File:** `pages/risk/RiskDashboardPage.tsx`  
**GET:** `/api/v1/risk/dashboard`  
**POST/PATCH:** stop-new, emergency-flatten, limits (A26 §6.12)

| Architecture | DTO |
|---|---|
| Execution account equity if available | `account.equity` + `account.available` |
| Balance / free margin / margin level | `account.*` |
| Daily P&L / current drawdown | `performance.*` |
| XAU long / short / net | `xau.longQuantity` / `shortQuantity` / `netQuantity` (+ show gross) |
| Risk by copied trader | `riskByCopiedTrader[]` |
| Risk by source broker | `riskBySourceBroker[]` |
| Rejected copy intents + reasons | `rejectedIntents[]` (`reasonCode` enum) |
| `STOP_NEW_EXECUTION` state | `controls.stopNewExecution` |
| `EMERGENCY_FLATTEN` availability | `controls.emergencyFlattenAvailable` |

Kill-switch rules (do not collapse into one toggle):

| Control | Closes positions? | Confirm | Roles |
|---|---|---|---|
| Stop-new | No | reason required | SuperAdmin, RiskManager |
| Emergency flatten | Yes (attempt) | type `EMERGENCY_FLATTEN` | SuperAdmin, RiskManager |
| Real copy enable | No (gate) | type `ENABLE_REAL_EXECUTION` | SuperAdmin on; SuperAdmin or RiskManager off — **Settings page**, not this toggle |

If `emergencyFlattenAvailable === false` (no TRADE / execution off), the button is disabled with that reason. Do not hide the control.

Limits form: partial PATCH of `controls.limits` with `If-Match` etag.

### 10.8 Reconciliation — §54

**File:** `pages/reconciliation/ReconciliationPage.tsx`  
**GET:** `/api/v1/reconciliation` + `/reconciliation/issues`  
**POST:** ack issue; run recon — SuperAdmin or RiskManager

Header tiles:

```text
Last successful MT5 reconciliation
Last successful cTrader reconciliation
```

Issue counts — every type stays visible even at 0 (empty ≠ hidden):

```text
UNKNOWN_EXTERNAL_POSITION
MISSING_INTERNAL_POSITION
ORDER_MISMATCH
QUANTITY_MISMATCH
SIDE_MISMATCH
ORPHAN_FILL
ORPHAN_EXECUTION_REPORT
UNEXPECTED_FILL
UNRESOLVED_EXECUTION_STATE
```

Table of open issues; status `OPEN` | `ACKNOWLEDGED` | `RESOLVED` | `WONT_FIX_AUDITED`. No silent drop. While cTrader is not `READY_FOR_EXECUTION`, the page must show that banner (live entries will 412).

### 10.9 Remaining §46 pages (needed to operate the dashboard)

| Page | Widgets | Notes |
|---|---|---|
| Login | email + dashboard password | Password never stored in Query cache or logs |
| Trade Explorer | same trade row as detail; filters broker/login/from/to/side/minPnl | Default `canonicalSymbol=XAUUSD` |
| Scoring | baseline name + outputs; last run; `countsByState` bar (**ECharts**) | No weights, no training PII |
| Models | version table; promote dialog | SuperAdmin + phrase `PROMOTE_MODEL`. Empty list is correct before Phase 6 |
| Shadow Portfolio | pnl, open/closed counts, positions marked with dest quote + `quoteAgeMs` | Prices from QUOTE, not MT5 last-deal (A24) |
| Live Copy Portfolio | same + `realCopyExecutionEnabled` + dest ids / clOrdId | Empty positions while flag false |
| System Health | MT5 / reconstruction / scoring / FIX metric groups from §58 | No host credentials |
| Audit | filterable log; `before`/`after` | ReadOnly 403. Sanitized blobs only |
| Settings | execution flags, symbol mappings, FIX **non-secret** host/ports/comp ids | Password field **must not exist**. 422 if sent |

---

## 11. Types (`src/types`)

Mirror A26 JSON. Enums as string unions matching the **wire** values (ASP.NET camelCase / existing uppercase tokens in A26):

```ts
export type TraderState =
  | 'INSUFFICIENT_DATA' | 'EARLY_SCORE' | 'WATCH' | 'SHADOW'
  | 'LIVE_CANDIDATE' | 'LIVE' | 'PAUSED' | 'RISK_BLOCKED' | 'DISQUALIFIED';

export type HealthStatus = 'HEALTHY' | 'DEGRADED' | 'UNHEALTHY' | 'STALE' | 'UNKNOWN';
export type Role = 'SuperAdmin' | 'RiskManager' | 'Analyst' | 'ReadOnly';
export type FeatureQuality = 'EXACT' | 'APPROXIMATE' | 'UNAVAILABLE';
```

Do **not** import C# PascalCase enums into TS by hand-copying `TraderState.cs` numeric values. The browser speaks A26 strings.

Domain entities (`Broker`, `Mt5Group`, …) include fields the dashboard **must never see** (`ManagerLogin` raw). Types are **allow-list DTOs**, not clones of `src/Domain/Entities`.

`components/secrets/denylist.ts` (client defense in depth; server sanitizer remains mandatory):

```text
(?i)(password|passwd|secret|pwd|rawdata|connectionstring|privatekey|proxyuser)
```

If a GET or hub payload matches: do not render, do not cache, show a generic error.

---

## 12. Auth and RBAC in the tree

`auth/can.ts` implements A26 §10–§11. `useRole()` reads `['auth','me']` only.

| Action | SuperAdmin | RiskManager | Analyst | ReadOnly |
|---|---|---|---|---|
| All §46 GET except Audit | R | R | R | R |
| Audit GET | R | R | R | — |
| Enable real execution | W | — | — | — |
| Disable real execution | W | W | — | — |
| Change risk limits | W | W | — | — |
| Pause / resume copy | W | W | — | — |
| Symbol mapping | W | — | — | — |
| Stop-new on/off | W | W | — | — |
| Emergency flatten | W | W | — | — |
| Promote model | W | — | — | — |
| Broker / FIX non-secret config | W | — | — | — |
| Group `enabledForAnalysis` | W | W | — | — |
| Recon ack / run | W | W | — | — |
| Any secret field | **422** | **422** | **422** | **422** |

Access token: **memory only** (AuthProvider). Refresh: httpOnly Secure SameSite=Strict cookie. Never persist JWT in `localStorage`.

Destructive UI: `ConfirmPhraseModal` for `EMERGENCY_FLATTEN`, `ENABLE_REAL_EXECUTION`, `PROMOTE_MODEL`. Button disabled unless `can(role, action)`.

---

## 13. ECharts usage (and what not to chart)

| Chart | Page | Series | Honest-null rule |
|---|---|---|---|
| Score timeline | Trader Detail | earlyScore, riskScore, behaviorScore, mlProbability | skip null ml |
| Lot-size timeline | Trader Detail | volume vs `at` | |
| Holding-time histogram | Trader Detail | `bucketsSeconds` × `counts` | |
| State counts | Scoring | `countsByState` | |
| Optional exposure bars | Risk | long/short/net | |

No candlesticks, no “predicted price,” no client-side scoring. Chart theme lives in `components/charts/theme.ts` (dark, matching `index.html` `class="dark"`).

Remove `recharts` from `package.json` in the same coding wave that adds:

```text
echarts
echarts-for-react
zustand          # only if ConfirmPhrase / sidebar actually need it
```

---

## 14. `vite.config.ts` target (coding wave — not applied now)

```ts
// plan only — do not write this into product source from A62
server: {
  port: 3000,
  proxy: {
    '/api':  { target: process.env.VITE_API_PROXY ?? 'http://localhost:5160', changeOrigin: true },
    '/hubs': { target: process.env.VITE_API_PROXY ?? 'http://localhost:5160', changeOrigin: true, ws: true },
  },
},
resolve: { alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) } },
```

`tsconfig.json` needs `"paths": { "@/*": ["src/*"] }`.

---

## 15. Implementation sequence (when a later task is allowed to touch `apps/web`)

Do **not** start this from A62. Product source stays untouched until an explicit coding task.

| Step | Deliver | Depends on API? |
|---|---|---|
| S0 | Types + `queryKeys` + `client` + denylist + `can.ts` + `paths.ts` | No (can compile against A26) |
| S1 | AuthProvider + Login + shell (nav + header chips with ErrorState) | Login/me **or** a temporary 401 empty-safe shell |
| S2 | Overview + Brokers + Groups + Leaderboard + Detail tables (no charts) | `/overview`, `/brokers`, `/mt5/groups`, `/traders` |
| S3 | ECharts on Detail + Scoring | score-history / lot / holding / scoring |
| S4 | FIX cards + System Health + Settings (read-only) | `/fix/sessions`, `/health`, `/settings` |
| S5 | Risk read + Reconciliation read + Shadow/Live empty-safe | `/risk/dashboard`, `/reconciliation`, portfolios |
| S6 | Privileged mutations + ConfirmPhrase + Audit | write endpoints + audit log |
| S7 | SignalR invalidation; delete stub hooks/signalr; drop Recharts | `/hubs/ops` |

First useful React (§69.12, A28 Phase 3) is **S0–S2 plus health chips**. FIX quote tile is Phase 4. Shadow book is Phase 5. Models promote and live flatten are **not** Phase 3 exit.

API blocker (A06-01): until `/api/v1/overview` exists, the UI can only fail closed. That is correct. Do not ship mocked leaderboard scores.

---

## 16. Files the next coding wave should treat as DEPRECATED

| File | Why |
|---|---|
| `src/api/hooks.ts` | Wrong prefix, wrong resources, flatten without confirm, `PUT` settings |
| `src/api/signalr.ts` | Wrong hub, no JWT |
| `src/App.tsx` | Missing 4 routes; `/groups` instead of `/mt5-groups` |

Keep and extend: `index.html` (dark), Tailwind, `main.tsx` QueryClient defaults, `package.json` name `mt5-trader-intelligence`.

---

## 17. Acceptance for this plan (and for a future scaffold PR)

Plan-level (this document):

- [x] Every §46 nav item has a route and a page module path.
- [x] §47–§54 widget lists are mapped to A26 JSON (no silent omission).
- [x] ECharts, TanStack Query, React Router, SignalR locations are specified.
- [x] Secret denylist and RBAC live in named files.
- [x] Current stub is classified honestly (broken, not “85% done”).

Future PR (not this task):

- [ ] `npm run build` (`tsc && vite build`) succeeds with the new tree.
- [ ] No `recharts` import remains.
- [ ] No password / proxy / FIX RawData field in `src/types`.
- [ ] No `localStorage` role.
- [ ] Trader URLs are always `/traders/:brokerId/:login`.
- [ ] Confirm phrases required on flatten / enable-live / promote.
- [ ] Product `.mq5` untouched; no mutations outside a later authorized wave.

---

## 18. What this agent did **not** do

- Did not create or edit any file under `D:\Prop\apps\web`.
- Did not add `echarts` / `zustand` to `package.json`.
- Did not implement pages, hubs, or API controllers.
- Did not invent dashboard JSON beyond A26.
- Did not claim EX5 / backend decompile progress (out of scope).

---

*End of A62. Product source was not modified.*
