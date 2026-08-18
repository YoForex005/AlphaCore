# D08 — `apps/web/src` page census

| Field | Value |
|---|---|
| Agent | D08 (senior engineer, web `src` inventory / every-page census only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:33:46+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Target | `D:\Prop\apps\web\src` (entire tree) + every page under `src/pages` |
| Product source modified | **No.** This report is the only write. |
| Method | `list_dir` of `apps/web/src` and `src/pages`; `read_file` of all 26 `src` files + `App.tsx` routes + `DashboardLayout` nav + `hooks.ts`; PowerShell `Get-ChildItem` + `Get-FileHash SHA256` + byte / physical-line / last-write census; compare A26 §5.2 and architecture §46 |
| Precedence | On-disk tree supersedes A62 (empty `pages/`), B22 (13 files), B10. File existence is not widget completeness. C39 remains binding for Models (Phase 6 closed). |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**`D:\Prop\apps\web\src` is a Vite + React 18 SPA with exactly 15 page modules, 16 routed destinations (index redirect + 15 pages), and 14 sidebar links. Every `App.tsx` import resolves. Zero orphan page files. Zero `LoginPage` / `ModelsPage`.**

| Question | Count | Answer |
|---|---:|---|
| Files under `apps/web/src` (recursive, no `node_modules`) | **26** | listed in §2 |
| `src/pages/*.tsx` | **15** | listed in §3 |
| `App.tsx` `./pages/*` default imports | **15** | 15/15 on disk |
| Import whose file is missing | **0** | router will not fail module resolution |
| Page file not imported by `App.tsx` | **0** | no orphans |
| Default export name ≠ import binding | **0** | 15/15 `export default function <SameName>` |
| Sidebar `nav` entries | **14** | Trader Detail is a child route, not a nav row |
| A26 §5.2 routes present under the specified path | **14 / 17** | `/login` absent; `/models` absent; `/mt5-groups` is `/groups` |
| Other `pages/` dirs under `D:\Prop` (exclude vendor/bin/obj/node_modules) | **1** | only `D:\Prop\apps\web\src\pages` |
| `pages/` under `D:\Prop\src` | **0** | looking only at the C# workspace is a false miss |
| Catch-all → `/overview` | **0** | unmatched paths render an empty `<Outlet />` |
| Product source edited this pass | **0** | report only |

This is a **file + route census**. It is **not** a claim that §§47–54 widgets are painted, that `/api/v1` exists, or that the dashboard is first-useful.

---

## 1. Method

| Source | Path / action |
|---|---|
| Tree | `list_dir` `D:\Prop\apps\web\src` (api, components, layouts, pages, types, utils) |
| Pages dir | `list_dir` `D:\Prop\apps\web\src\pages` — **15** `*.tsx` |
| Router | `D:\Prop\apps\web\src\App.tsx` (2062 B, 42 physical lines) |
| Nav shell | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` (1854 B, 44 physical lines) |
| Entry | `D:\Prop\apps\web\src\main.tsx` — `QueryClient` + `BrowserRouter` |
| Hooks | `D:\Prop\apps\web\src\api\hooks.ts` — 11 `useQuery` hooks, unversioned `/api/*` |
| Every page | `read_file` of all 15 `pages/*.tsx` (full files; none truncated) |
| File census | PowerShell `Get-ChildItem -Recurse -File` + `Get-FileHash -Algorithm SHA256` + `.Length` + `Get-Content` line count + `LastWriteTime` |
| Extra hunt | `Get-ChildItem` name match `Login\|Models` under `src` → **0 files**; `pages/` dirs under `D:\Prop` excluding vendor/bin/obj/node_modules → **one** |
| Binding nav | architecture §46 (`…Architecture_v2.md` lines 1739–1758); A26 §5.2 route map |
| Prior census | B22 (13 files, 13:19:28); C08 (15 files, 13:23:27); B20; C37/C38/C39/C53 |
| App root (outside `src`, noted only) | `D:\Prop\apps\web\index.html` mounts `/src/main.tsx`; Vite port 3000, **no** `/api` proxy |

No `tsc`, no `npm`, no `vite`, no product edit.

---

## 2. Full `apps/web/src` tree (26 files)

```text
D:\Prop\apps\web\src\
  App.tsx                         2062 B   15 child routes + index → /overview
  main.tsx                         648 B   QueryClient + BrowserRouter; no AuthProvider
  index.css                         62 B   Tailwind @tailwind base/components/utilities
  api/client.ts                    232 B   axios → VITE_API_URL or http://localhost:5000
  api/hooks.ts                    1935 B   11 hooks; no models/shadow/live/audit
  api/signalr.ts                   899 B   /hubs/dashboard (A26 is /hubs/ops); pages never subscribe
  components/MetricCard.tsx        521 B   used by Overview + Risk
  components/StatusBadge.tsx       699 B   UNUSED (0 imports)
  layouts/DashboardLayout.tsx     1854 B   14-item left nav; startConnection(); no header strip
  pages/                          15 modules — §3
  types/index.ts                  2905 B   UNUSED (0 imports); fields diverge from live API
  utils/formatters.ts              947 B   UNUSED (0 imports)
```

No `src/auth/`, `src/hubs/`, `src/pages/**/` subfolders, tests, `LoginPage.tsx`, or `ModelsPage.tsx`.

SHA-256 + last write (PowerShell, this pass):

| Path under `src/` | Bytes | Phys. lines | SHA-256 | Last write (local) |
|---|---:|---:|---|---|
| `api/client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06 |
| `api/hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00 |
| `api/signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02 |
| `App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38 |
| `components/MetricCard.tsx` | 521 | 11 | `1C6A6AF96D39C3B0C5BA4544337E19EDE460279BB61DF547687BDAF85B36991B` | 2026-08-18T13:08:39 |
| `components/StatusBadge.tsx` | 699 | 17 | `F6ECFE83230269617140C286CFFA0F065D488EF5FB665FA75E8E4586D60F807E` | 2026-08-18T13:08:38 |
| `index.css` | 62 | 3 | `7A8B07838661BED82405F51991A10179BA7782D7C2ACAFF54777B49F020BAE77` | 2026-08-18T13:06:39 |
| `layouts/DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38 |
| `main.tsx` | 648 | 22 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39 |
| `types/index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18 |
| `utils/formatters.ts` | 947 | 31 | `FD0214751EA05923973B4EBD73E35EE21AC3D253FDE61538F4616EC2DC3B6F66` | 2026-08-18T13:08:39 |

Page file hashes are in §3. All 15 page SHAs are **unchanged** vs C08 (13:23:27). `App.tsx` SHA `A0E92C9779A0C777…` is **unchanged** vs C08.

---

## 3. Every page file (authoritative list)

`D:\Prop\apps\web\src\pages\` contains **exactly** these 15 files. Alphabetical as returned by `list_dir`. No `.ts`, no index barrel, no subfolders.

| # | File | Bytes | Phys. lines | SHA-256 | Last write (local) | Default export |
|---|---|---:|---:|---|---|---|
| 1 | `AuditPage.tsx` | 324 | 8 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` | 2026-08-18T13:20:38 | `AuditPage` |
| 2 | `BrokersPage.tsx` | 1266 | 37 | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` | 2026-08-18T13:16:00 | `BrokersPage` |
| 3 | `FixSessionsPage.tsx` | 1312 | 26 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | 2026-08-18T13:16:43 | `FixSessionsPage` |
| 4 | `GroupsPage.tsx` | 1228 | 34 | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` | 2026-08-18T13:16:00 | `GroupsPage` |
| 5 | `LiveCopyPage.tsx` | 321 | 8 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 2026-08-18T13:20:38 | `LiveCopyPage` |
| 6 | `OverviewPage.tsx` | 2078 | 35 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | 2026-08-18T13:16:00 | `OverviewPage` |
| 7 | `ReconciliationPage.tsx` | 490 | 12 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` | 2026-08-18T13:16:43 | `ReconciliationPage` |
| 8 | `RiskPage.tsx` | 1148 | 25 | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | 2026-08-18T13:16:43 | `RiskPage` |
| 9 | `ScoringPage.tsx` | 1288 | 33 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 2026-08-18T13:16:43 | `ScoringPage` |
| 10 | `SettingsPage.tsx` | 459 | 12 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | 2026-08-18T13:16:43 | `SettingsPage` |
| 11 | `ShadowPortfolioPage.tsx` | 628 | 14 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | 2026-08-18T13:16:43 | `ShadowPortfolioPage` |
| 12 | `SystemHealthPage.tsx` | 369 | 11 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` | 2026-08-18T13:16:43 | `SystemHealthPage` |
| 13 | `TradeExplorerPage.tsx` | 1321 | 39 | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` | 2026-08-18T13:16:43 | `TradeExplorerPage` |
| 14 | `TraderDetailPage.tsx` | 1592 | 34 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` | 2026-08-18T13:16:00 | `TraderDetailPage` |
| 15 | `TradersPage.tsx` | 1604 | 42 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` | 2026-08-18T13:16:00 | `TradersPage` |

**Files that do not exist (confirmed 0 hits):**

- `LoginPage.tsx`
- `ModelsPage.tsx`
- any `*Page.tsx` outside `src/pages`

---

## 4. Router: every destination (1:1 with disk)

`App.tsx` lines 3–17 import; lines 21–39 route. All 15 imported symbols are used exactly once as a `<Route element={…} />`. `main.tsx` wraps `<App />` in `<BrowserRouter>`. All routes sit under `<Route element={<DashboardLayout />}>`.

| # | URL (BrowserRouter) | `App.tsx` `path` | Page module | Sidebar? | Hook |
|---|---|---|---|---|---|
| 0 | `/` | `index` | `<Navigate to="/overview" replace />` | n/a | none |
| 1 | `/overview` | `overview` | `OverviewPage` | yes — Overview | `useOverview` → `GET /api/overview` |
| 2 | `/brokers` | `brokers` | `BrokersPage` | yes — Brokers | `useBrokers` → `GET /api/brokers` |
| 3 | `/groups` | `groups` | `GroupsPage` | yes — Groups | `useGroups` → `GET /api/groups` |
| 4 | `/traders` | `traders` | `TradersPage` | yes — Traders | `useTraders({})` → `GET /api/traders` |
| 5 | `/traders/:brokerId/:login` | `traders/:brokerId/:login` | `TraderDetailPage` | **no** (linked from leaderboard) | `useTraderDetail` → `GET /api/traders/{broker}/{login}` |
| 6 | `/trades` | `trades` | `TradeExplorerPage` | yes — Trades | `useTrades` → `GET /api/trades` |
| 7 | `/scoring` | `scoring` | `ScoringPage` | yes — Scoring | `useTraders({})` (reuse; no scoring API) |
| 8 | `/shadow` | `shadow` | `ShadowPortfolioPage` | yes — Shadow | **none** |
| 9 | `/live` | `live` | `LiveCopyPage` | yes — Live | **none** |
| 10 | `/fix` | `fix` | `FixSessionsPage` | yes — FIX | `useFixSessions` → `GET /api/fix/sessions` (5 s) |
| 11 | `/risk` | `risk` | `RiskPage` | yes — Risk | `useRiskStatus` → `GET /api/risk` (5 s) |
| 12 | `/reconciliation` | `reconciliation` | `ReconciliationPage` | yes — Recon | `useReconciliation` → `GET /api/reconciliation/status` |
| 13 | `/health` | `health` | `SystemHealthPage` | yes — Health | `useHealth` → `GET /api/health` (10 s) |
| 14 | `/audit` | `audit` | `AuditPage` | yes — Audit | **none** |
| 15 | `/settings` | `settings` | `SettingsPage` | yes — Settings | `useSettings` → `GET /api/settings` |

No `path="*"` catch-all. A26: unknown routes → `/overview`. Current: unmatched URL still paints the layout chrome with an empty `<Outlet />`.

Detail-link fact: `TradersPage` navigates to ``/traders/${t.broker}/${t.login}`` (broker **code**, not A26 UUID `brokerId`).

---

## 5. Sidebar vs router

`DashboardLayout` `nav` (lines 5–20) has **14** links. Layout also calls `startConnection()` once (SignalR `/hubs/dashboard`). Pages never call `onEvent`.

| Nav `to` | Label | Icon | `App.tsx` path | Match |
|---|---|---|---|---|
| `/overview` | Overview | ◈ | `overview` | yes |
| `/brokers` | Brokers | ⛁ | `brokers` | yes |
| `/groups` | Groups | ⊞ | `groups` | yes (both ≠ A26 `/mt5-groups`) |
| `/traders` | Traders | ⚑ | `traders` | yes |
| `/trades` | Trades | ⇄ | `trades` | yes |
| `/scoring` | Scoring | ★ | `scoring` | yes |
| `/shadow` | Shadow | ◐ | `shadow` | yes |
| `/live` | Live | ▶ | `live` | yes |
| `/fix` | FIX | ⚡ | `fix` | yes |
| `/risk` | Risk | ⚠ | `risk` | yes |
| `/reconciliation` | Recon | ⟳ | `reconciliation` | yes |
| `/health` | Health | ♥ | `health` | yes |
| `/audit` | Audit | ☰ | `audit` | yes |
| `/settings` | Settings | ⚙ | `settings` | yes |

No sidebar link points at a missing module. Labels are **abbreviated** vs architecture §46 (`Groups` vs `MT5 Groups`, `Live` vs `Live Copy Portfolio`, `FIX` vs `cTrader FIX`, `Recon` vs `Reconciliation`, `Health` vs `System Health`). A26 §5.3 requires labels **exactly** as §46. Trader Detail and Models are not sidebar items.

Header strip (A26 §5.3: `REAL_COPY`, `STOP_NEW`, MT5 / QUOTE / TRADE health) is **MISSING** from the layout.

---

## 6. Every A26 / §46 page vs disk

Architecture §46 main navigation (verbatim order): Overview, Brokers, MT5 Groups, Traders, Trader Detail, Trade Explorer, Scoring, Models, Shadow Portfolio, Live Copy Portfolio, cTrader FIX, Risk, Reconciliation, System Health, Audit, Settings. A26 §5.2 adds `/login` (architecture §59).

| §46 / A26 item | Required route (A26) | In `App.tsx`? | File under `pages/`? | Nav label | §73.B (file/route) |
|---|---|---|---|---|---|
| Login | `/login` | no | no | n/a | **MISSING** (never created; required to operate the shell under RBAC) |
| Overview | `/overview` | yes | `OverviewPage.tsx` | Overview | **EXISTS_NEEDS_REFACTOR** |
| Brokers | `/brokers` | yes | `BrokersPage.tsx` | Brokers | **EXISTS_NEEDS_REFACTOR** |
| MT5 Groups | `/mt5-groups` | **wrong path** `/groups` | `GroupsPage.tsx` | Groups | **EXISTS_NEEDS_REFACTOR** (file present; path ≠ A26) |
| Traders | `/traders` | yes | `TradersPage.tsx` | Traders | **EXISTS_NEEDS_REFACTOR** |
| Trader Detail | `/traders/:brokerId/:login` | yes | `TraderDetailPage.tsx` | not in nav | **EXISTS_NEEDS_REFACTOR** |
| Trade Explorer | `/trades` | yes | `TradeExplorerPage.tsx` | Trades | **EXISTS_NEEDS_REFACTOR** |
| Scoring | `/scoring` | yes | `ScoringPage.tsx` | Scoring | **EXISTS_NEEDS_REFACTOR** |
| Models | `/models` | no | no | absent | **MISSING** as a file. C39: **MISSING_BY_DESIGN** while Phase 6 is closed — do not create to “complete” nav |
| Shadow Portfolio | `/shadow` | yes | `ShadowPortfolioPage.tsx` | Shadow | **EXISTS_NEEDS_REFACTOR** (static, no hook) |
| Live Copy Portfolio | `/live` | yes | `LiveCopyPage.tsx` | Live | **EXISTS_NEEDS_REFACTOR** (8-line stub) |
| cTrader FIX | `/fix` | yes | `FixSessionsPage.tsx` | FIX | **EXISTS_NEEDS_REFACTOR** |
| Risk | `/risk` | yes | `RiskPage.tsx` | Risk | **EXISTS_NEEDS_REFACTOR** |
| Reconciliation | `/reconciliation` | yes | `ReconciliationPage.tsx` | Recon | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |
| System Health | `/health` | yes | `SystemHealthPage.tsx` | Health | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |
| Audit | `/audit` | yes | `AuditPage.tsx` | Audit | **EXISTS_NEEDS_REFACTOR** (8-line stub) |
| Settings | `/settings` | yes | `SettingsPage.tsx` | Settings | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |

**Missing page files (would be new files, not replacements):**

1. `LoginPage` — `/login`
2. `ModelsPage` — `/models` (optional honest placeholder only; C39 forbids a working Models surface)

B22 listed Live Copy and Audit as missing files. **That remainder is closed on disk** (both written 2026-08-18T13:20:38, same timestamp as the `App.tsx` / layout update that wired them).

---

## 7. Every page — what it actually renders

Present ≠ finished. Depth vs architecture §§47–54 is **not** claimed. This section is the honest shape of each page as read this pass.

### 7.1 `OverviewPage` — `/overview`

- Imports: `MetricCard`, `useOverview`.
- States: loading / API error (mentions port 5000) / null data.
- H1 “Overview” + first-useful banner (“Live FIX send is off”).
- **12** `MetricCard`s: MT5 accounts, Brokers, XAU traders, ≥ 3 trades, Watch, Shadow, Live candidates, Risk blocked, Shadow P&L, Dest. real P&L, MT5 health, QUOTE/TRADE.
- Footer banner: `data.realCopyEnabled` ON/OFF; “Trade #3 never auto-promotes to LIVE.”
- DTO fields **not** painted: `live`, `xauGross`, `xauNet`.
- Class: table + tiles, API-backed.

### 7.2 `BrokersPage` — `/brokers`

- Hook: `useBrokers`.
- Table columns (7): Code, Name, Server, Manager, Groups, Accounts, Status.
- Manager cell appends `**` after already-masked `managerLoginMasked`.
- Status from `b.connected` (`connected` / `down`).
- Class: table, API-backed.

### 7.3 `GroupsPage` — `/groups` (A26 wanted `/mt5-groups`)

- Hook: `useGroups`.
- H1 “MT5 Groups”. Subcopy: plan mappings are labels only.
- Table columns (5): Broker, Group, Accounts, Analysis, Plan.
- DTO `lastDiscovered` / `lastSynced` unused.
- Class: table, API-backed.

### 7.4 `TradersPage` — `/traders`

- Hook: `useTraders({})` — hook accepts `broker`/`state`; UI passes neither. **0 filters.**
- H1 “Trader leaderboard”.
- Table columns (9): Broker, Login (Link), Group, XAU trades, Net P&L, Early, Risk, Flags (`MG`/`AVG`/`ESC`), State.
- Login links to `/traders/${t.broker}/${t.login}`.
- Unused DTO on page: `mlProbability`, `shadowPnl`, `lastScored`.
- Class: table, API-backed.

### 7.5 `TraderDetailPage` — `/traders/:brokerId/:login`

- `useParams` + `useTraderDetail(brokerId, login)`.
- 8 `Info` chips: State, Completed XAU, Early score, Risk score, Net P&L, Martingale, Averaging down, ML probability (`null` → “not trained”).
- Footnote about first 3 trades. **No** trade table, charts, first-3 block, shadow/live books.
- Class: header chips only. Same `TraderRowDto` as the list (A93 detail payload not used).

### 7.6 `TradeExplorerPage` — `/trades`

- Hook: `useTrades`.
- Table columns (8): Login, Symbol, Dir, Opened, Closed, Lots, Net, Done.
- No broker filter, no first-3 mark, no MFE/MAE.
- Class: table, API-backed.

### 7.7 `ScoringPage` — `/scoring`

- Reuses `useTraders({})`. No dedicated scoring endpoint.
- H1 “Deterministic scoring”. Honest line: “XGBoost is not active.”
- Columns (5): Trader, Early quality, Behavior (`behaviorScore ?? 0` — field **not** on `TraderRowDto`, paints `0.0`), Risk, State.
- Class: table, API-backed via list reuse.

### 7.8 `ShadowPortfolioPage` — `/shadow`

- **No hook.** Static policy text only (QUOTE fills, intent expiry, live NOS disabled, demo-seed note).
- Class: static stub.

### 7.9 `LiveCopyPage` — `/live`

- **No hook.** 8 lines. H1 “Live copy portfolio”. Amber: `REAL_COPY_EXECUTION_ENABLED is false. This page will stay empty until go-live gates pass.`
- Class: static stub (honest empty; not a book).

### 7.10 `FixSessionsPage` — `/fix`

- Hook: `useFixSessions`.
- H1 “cTrader FIX”. Password never shown. TargetCompID note `cServer`.
- One card per session: qualifier, host:port, connected/loggedOn, status, seq in/out, reconnects, bid/ask/age if `bid != null`, instrument id, execution enabled (printed on **every** card).
- Unused DTO: `lastInbound`, `lastOutbound`, `lastError`.
- Empty list → blank grid (no dedicated empty QUOTE/TRADE cards).
- Class: cards, API-backed.

### 7.11 `RiskPage` — `/risk`

- Hook: `useRiskStatus`. Uses `MetricCard`.
- 4 cards: Kill switch, Real copy, Daily P&L, XAU net.
- “Recent rejects” string list (`recentRejectReasons`).
- Unused DTO on page: `drawdown`, `xauLong`, `xauShort`.
- Class: tiles + list, API-backed.

### 7.12 `ReconciliationPage` — `/reconciliation`

- Hook: `useReconciliation`.
- Title + one sentence + `<pre>{JSON.stringify(data, null, 2)}</pre>`.
- Class: JSON dump.

### 7.13 `SystemHealthPage` — `/health`

- Hook: `useHealth`.
- Title + `<pre>{JSON.stringify(data, null, 2)}</pre>`.
- Class: JSON dump.

### 7.14 `AuditPage` — `/audit`

- **No hook.** 8 lines. H1 “Audit”. Static sentence about overrides / kill-switch / mappings; “RBAC is not enabled in the demo seed.”
- Class: static stub.

### 7.15 `SettingsPage` — `/settings`

- Hook: `useSettings`.
- Title + “Secrets are never returned to the browser.” + `JSON.stringify`.
- No PATCH, no symbol-map form, no non-secret FIX form.
- Class: JSON dump.

---

## 8. Hooks inventory (what pages can call)

`D:\Prop\apps\web\src\api\hooks.ts` exports **11** functions. None use `/api/v1`.

| Hook | HTTP | Used by |
|---|---|---|
| `useOverview` | `GET /api/overview` | OverviewPage |
| `useBrokers` | `GET /api/brokers` | BrokersPage |
| `useGroups` | `GET /api/groups` | GroupsPage |
| `useTraders` | `GET /api/traders` | TradersPage, ScoringPage |
| `useTraderDetail` | `GET /api/traders/:broker/:login` | TraderDetailPage |
| `useTrades` | `GET /api/trades` | TradeExplorerPage |
| `useFixSessions` | `GET /api/fix/sessions` | FixSessionsPage |
| `useRiskStatus` | `GET /api/risk` | RiskPage |
| `useReconciliation` | `GET /api/reconciliation/status` | ReconciliationPage |
| `useHealth` | `GET /api/health` | SystemHealthPage |
| `useSettings` | `GET /api/settings` | SettingsPage |

**Hooks that do not exist:** `useLive`, `useAudit`, `useShadow`, `useModels`, `useScoring`, `useLogin`. Live / Audit / Shadow therefore cannot be API-backed without new hooks (out of scope here).

Client: axios, `baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5000'`, timeout 15 s.

---

## 9. Unused / dead `src` (do not count as pages)

| File | Why unused |
|---|---|
| `components/StatusBadge.tsx` | 0 imports from any page or layout |
| `types/index.ts` | 0 imports; field names diverge from live API (`totalBrokers` vs `connectedBrokers`, etc.) |
| `utils/formatters.ts` | 0 imports (`formatPrice`, `formatVolume`, `timeAgo`, `pnlColor`, `pnlBg`) |
| `recharts` (package.json, outside `src`) | 0 imports anywhere under `src` |
| `api/signalr.ts` `onEvent` | layout starts the hub; no page subscribes |

These are **not** extra pages.

---

## 10. What earlier reports claimed vs this pass

| Prior claim | This pass |
|---|---|
| A62: 13 imports, 0 page files / `pages/` EMPTY | **FALSE.** 15 files |
| B10 / B22: 13 page modules; Live + Audit never created | **STALE.** 15 now; Live + Audit on disk and routed |
| B22: App.tsx SHA `D83CE347…` (1854 B) | **Superseded.** 2062 B, SHA `A0E92C9779A0C777…` |
| B22: 12 nav links | **STALE.** 14 nav links |
| C08: 15 files, hashes above | **CONFIRMED.** Same 15 SHAs, same `App.tsx` / layout SHAs |
| C37/C53: Live chrome present, book missing | **CONFIRMED** |
| C38/C53: Audit chrome present, table missing | **CONFIRMED** |
| C39: Models absent by design (Phase 6 closed) | **CONFIRMED** — still no file / route / hook |
| Login / Models missing as files | **Still true** |
| `/groups` ≠ `/mt5-groups` | **Still true** |

Do not use B22’s 13-row table as the current import proof. Use this file or C08.

---

## 11. Direct answers

### What is in `D:\Prop\apps\web\src`?

Twenty-six source files: entry (`main.tsx`, `App.tsx`, `index.css`), 3 API modules, 2 components, 1 layout, 15 pages, 1 unused types barrel, 1 unused formatters module.

### List every page.

1. `OverviewPage` → `/overview`
2. `BrokersPage` → `/brokers`
3. `GroupsPage` → `/groups`
4. `TradersPage` → `/traders`
5. `TraderDetailPage` → `/traders/:brokerId/:login`
6. `TradeExplorerPage` → `/trades`
7. `ScoringPage` → `/scoring`
8. `ShadowPortfolioPage` → `/shadow`
9. `LiveCopyPage` → `/live`
10. `FixSessionsPage` → `/fix`
11. `RiskPage` → `/risk`
12. `ReconciliationPage` → `/reconciliation`
13. `SystemHealthPage` → `/health`
14. `AuditPage` → `/audit`
15. `SettingsPage` → `/settings`

Plus the index redirect `/` → `/overview`. There is no sixteenth page file.

### Do `App.tsx` imports match `pages/`?

**Yes.** 15 default imports, 15 files, 15 matching `export default function` names, 15 routes that consume those bindings. Zero dangling. Zero orphans.

### Are there extra page files `App.tsx` does not import?

**No.**

### What page files are still missing vs the dashboard contract?

Only `LoginPage` (`/login`) and `ModelsPage` (`/models`). Groups exists but is not on the A26 path. Models absence is **by design** until Phase 6 (C39). Login is required for §59 / A26 and is simply not built.

### Did this pass change product source?

**No.**

---

## 12. What a later wave must not do

1. Do **not** recreate the 15 existing page files to “fix” A62 or B22.
2. Do **not** invent a second `pages/` tree under `D:\Prop\src`.
3. Do **not** treat `/groups` vs `/mt5-groups` as a missing file (it is a path mismatch).
4. Do **not** treat Live / Audit as missing files; they are stubs.
5. Do **not** create a working Models page to complete §46 (C39 / A52 / A63). An honest “Phase 6 not open” placeholder is optional, not required for §69.
6. Do **not** create Login in this report’s wave; it is listed only as a spec remainder.
7. Do **not** overwrite existing `.tsx` without a versioned coding task.
8. Do **not** treat this census as §§47–54 widget PASS.

---

## Evidence pins

- Tree: `D:\Prop\apps\web\src` — **26** files, measured 2026-08-18T13:33:46+05:30.
- Pages dir: `D:\Prop\apps\web\src\pages\` — **15** `*.tsx`, names and SHAs in §3.
- Router: `D:\Prop\apps\web\src\App.tsx` lines 3–17 (imports), 21–39 (routes). SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 5–20 (14 `to` paths). SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`.
- Only product `pages/` directory: `D:\Prop\apps\web\src\pages`.
- Binding routes: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2.
- Binding labels: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1739–1758.
- Same-day 15-file confirm: `C08_web_pages_review.md`.
- Live/Audit chrome: `C37_live_copy_page.md`, `C38_audit_page.md`, `C53_nav_complete.md`.
- Models hold: `C39_models_page.md`.
- Stale empty / 13-file claims: `A62_react_scaffold.md`, `B22_web_missing_pages.md`.

---

## One-line close

**15 pages on disk and routed (`/overview` `/brokers` `/groups` `/traders` `/traders/:brokerId/:login` `/trades` `/scoring` `/shadow` `/live` `/fix` `/risk` `/reconciliation` `/health` `/audit` `/settings`); 14 sidebar links; no Login; no Models; Groups is the wrong path; Live/Audit/Shadow are stubs; Recon/Health/Settings are JSON dumps; product source not touched.**
