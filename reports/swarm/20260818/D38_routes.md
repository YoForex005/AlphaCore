# D38 — `App.tsx` + `DashboardLayout` route table

| Field | Value |
|---|---|
| Agent | D38 (senior engineer, React route table only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:02+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `App.tsx` and layout. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\App.tsx` + `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` |
| Entry / host | `D:\Prop\apps\web\src\main.tsx`, `D:\Prop\apps\web\index.html`, `D:\Prop\apps\web\vite.config.ts` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full `read_file` of `App.tsx` (42 lines), `DashboardLayout.tsx` (44 lines), `main.tsx`, `index.html`, `vite.config.ts`, `hooks.ts`, `signalr.ts`, `TradersPage.tsx`, `TraderDetailPage.tsx`, `LiveCopyPage.tsx`, `AuditPage.tsx`; `list_dir` of `src/pages`; PowerShell SHA-256 + byte / physical-line / last-write; `git diff` vs HEAD `398a142`; compare A26 §5.2 / §5.3 and architecture §46 |
| Binding law | Architecture v2 §46 (nav inventory, lines 1735–1758); A26 §5.2 route map; A26 §5.3 shell; C39 (Models absence); C18 (no RBAC) |
| Prior census | A62 (13 imports, 0 page files — **stale**); B22 (13 files); B31 (nav gaps); C08 / C53 (15 files + Live/Audit chrome); D08 (15 pages / 16 destinations / 14 nav, SHA of this `App.tsx` already `A0E92C9779A0C777…`) |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **route + layout census**. It is **not** a claim that §§47–54 widgets are painted, that `/api/v1` exists, that RBAC is on, or that the dashboard is first-useful.

---

## 0. Verdict

**Worktree router is a single `BrowserRouter` tree: 16 destinations (index redirect + 15 pages) all nested under `DashboardLayout`. 14 sidebar `NavLink`s. Every `App.tsx` page import resolves. No `Login`, no `Models`, no catch-all, no auth guard, no header strip.**

`App.tsx` SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` and `DashboardLayout.tsx` SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` are **unchanged vs D08** (remeasured 13:37:02). Both files are **unstaged** vs HEAD: HEAD is missing `/live` and `/audit`. Do not treat a clean checkout as this route table.

| Question | Count | Answer |
|---|---:|---|
| Router files | **1** | `App.tsx` is the only `<Routes>` table |
| Layout files | **1** | `layouts/DashboardLayout.tsx` |
| Destinations in `App.tsx` | **16** | 1 index redirect + 15 page routes |
| `./pages/*` default imports | **15** | 15/15 on disk |
| Import whose file is missing | **0** | module resolution will not fail |
| Page file not imported | **0** | no orphans |
| Sidebar `nav` rows | **14** | Trader Detail is a child route, not a nav row |
| A26 §5.2 routes at the **specified path** | **14 / 17** | `/login` absent; `/models` absent; `/mt5-groups` is `/groups` |
| Exact §46 left-nav labels | **7 / 16** | 7 exact; 7 abbreviated; 2 absent (Models, Trader Detail) |
| `path="*"` catch-all → `/overview` | **0** | unknown URLs paint empty `<Outlet />` |
| Auth / role wrappers | **0** | no `RequireAuth`, `RequireRole`, `AuthProvider` |
| A26 §5.3 header strip | **0** | layout is sidebar + `<Outlet />` only |
| Product source edited this pass | **0** | report only |

§73.B for the **route table + shell** (this file's subject): **EXISTS_NEEDS_REFACTOR**.  
§73.B for `/login`: **MISSING**.  
§73.B for `/models`: **MISSING_BY_DESIGN** while Phase 6 is closed (C39) — do not add a working Models surface to “complete” the nav.  
§73.B for `/groups` vs A26 `/mt5-groups`: **EXISTS_NEEDS_REFACTOR** (file present; path ≠ contract).

---

## 1. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | **unstaged** (`M`); worktree blob `e297d559d709f5917dbaa91e06838d992949e966`; HEAD blob `6555ae04bed9ee7061d9e4acd09c9606ac0bd4d4` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38+05:30 | **unstaged** (`M`); worktree blob `8ece6a4583f088314a249546f03d6a64b7161527`; HEAD blob `1b40c4f6f6a7a2d74d43713b242e10fe7de5d543` |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39+05:30 | clean vs HEAD |
| `D:\Prop\apps\web\index.html` | 369 | 12 | `080656C860AC6F8C1FAB242789DEEF0803EC278028D8B0F24115A14536FDB8FD` | 2026-08-18T13:06:06+05:30 | (host only) |
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | 2026-08-18T13:06:19+05:30 | no `base`, no `/api` proxy |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | unversioned `/api/*` |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02+05:30 | `/hubs/dashboard` (A26 is `/hubs/ops`) |

`git diff --stat` vs HEAD (this pass): `App.tsx` **+4**, `DashboardLayout.tsx` **+2**. The six inserted lines are Live + Audit import/route/nav. `main.tsx` is unchanged.

No `src/routes/`, `src/auth/`, `src/hubs/`, `LoginPage.tsx`, `ModelsPage.tsx`, `RequireAuth`, `RequireRole`, or `AuthProvider` under `D:\Prop\apps\web\src`. Grep for `path="*"`, `basename`, `createBrowserRouter`, `createHashRouter`: **zero** hits.

`package.json` pins `react-router-dom` `^6.26.0`. Vite `server.port` is **3000**. No `basename`. HTML history URLs are `/overview`, not `/#/overview`.

---

## 2. Mount tree

```text
index.html  #title "MT5 Trader Intelligence"
  #root
    main.tsx
      React.StrictMode
        QueryClientProvider          # retry:2, refetchOnWindowFocus:false, staleTime:30s
          BrowserRouter              # no basename, no future flags
            App                      # ONLY <Routes> table
              Routes
                Route element=<DashboardLayout>
                  startConnection()  # SignalR /hubs/dashboard, once
                  aside nav[14]
                  main <Outlet />
                    [child routes — §3]
```

There is no second router. There is no lazy `React.lazy` / `Suspense`. Every page is a static default import.

`DashboardLayout` is the **only** layout. Login (when it exists) is specified by A26 as **outside** this shell. Current table puts every route, including the index redirect, **inside** the dashboard chrome.

---

## 3. Authoritative route table (`App.tsx` lines 19–41)

Quoted from disk (worktree). Import list is lines 3–17 (15 page modules + `DashboardLayout`). Every imported page symbol is used exactly once as a `<Route element={…} />`.

```19:41:D:\Prop\apps\web\src\App.tsx
export default function App() {
  return (
    <Routes>
      <Route element={<DashboardLayout />}>
        <Route index element={<Navigate to="/overview" replace />} />
        <Route path="overview" element={<OverviewPage />} />
        <Route path="brokers" element={<BrokersPage />} />
        <Route path="groups" element={<GroupsPage />} />
        <Route path="traders" element={<TradersPage />} />
        <Route path="traders/:brokerId/:login" element={<TraderDetailPage />} />
        <Route path="trades" element={<TradeExplorerPage />} />
        <Route path="scoring" element={<ScoringPage />} />
        <Route path="shadow" element={<ShadowPortfolioPage />} />
        <Route path="live" element={<LiveCopyPage />} />
        <Route path="fix" element={<FixSessionsPage />} />
        <Route path="risk" element={<RiskPage />} />
        <Route path="reconciliation" element={<ReconciliationPage />} />
        <Route path="health" element={<SystemHealthPage />} />
        <Route path="audit" element={<AuditPage />} />
        <Route path="settings" element={<SettingsPage />} />
      </Route>
    </Routes>
  );
}
```

| # | Browser URL | `path` | Element | In sidebar? | Param / extra |
|---|---|---|---|---|---|
| 0 | `/` | `index` | `<Navigate to="/overview" replace />` | n/a | default landing matches A26 |
| 1 | `/overview` | `overview` | `OverviewPage` | yes | |
| 2 | `/brokers` | `brokers` | `BrokersPage` | yes | |
| 3 | `/groups` | `groups` | `GroupsPage` | yes | **≠** A26 `/mt5-groups` |
| 4 | `/traders` | `traders` | `TradersPage` | yes | |
| 5 | `/traders/:brokerId/:login` | `traders/:brokerId/:login` | `TraderDetailPage` | **no** | only parameterized route |
| 6 | `/trades` | `trades` | `TradeExplorerPage` | yes | |
| 7 | `/scoring` | `scoring` | `ScoringPage` | yes | |
| 8 | `/shadow` | `shadow` | `ShadowPortfolioPage` | yes | |
| 9 | `/live` | `live` | `LiveCopyPage` | yes | **unstaged vs HEAD** |
| 10 | `/fix` | `fix` | `FixSessionsPage` | yes | |
| 11 | `/risk` | `risk` | `RiskPage` | yes | |
| 12 | `/reconciliation` | `reconciliation` | `ReconciliationPage` | yes | |
| 13 | `/health` | `health` | `SystemHealthPage` | yes | |
| 14 | `/audit` | `audit` | `AuditPage` | yes | **unstaged vs HEAD** |
| 15 | `/settings` | `settings` | `SettingsPage` | yes | |

No `path="login"`. No `path="models"`. No `path="mt5-groups"`. No `path="*"`.

Relative child paths (no leading `/`) are correct for a parent with no `path`. Combined with `BrowserRouter` they resolve as `/overview`, `/groups`, etc.

---

## 4. Layout nav (`DashboardLayout.tsx` lines 5–20)

```5:20:D:\Prop\apps\web\src\layouts\DashboardLayout.tsx
const nav = [
  { to: '/overview', label: 'Overview', icon: '◈' },
  { to: '/brokers', label: 'Brokers', icon: '⛁' },
  { to: '/groups', label: 'Groups', icon: '⊞' },
  { to: '/traders', label: 'Traders', icon: '⚑' },
  { to: '/trades', label: 'Trades', icon: '⇄' },
  { to: '/scoring', label: 'Scoring', icon: '★' },
  { to: '/shadow', label: 'Shadow', icon: '◐' },
  { to: '/live', label: 'Live', icon: '▶' },
  { to: '/fix', label: 'FIX', icon: '⚡' },
  { to: '/risk', label: 'Risk', icon: '⚠' },
  { to: '/reconciliation', label: 'Recon', icon: '⟳' },
  { to: '/health', label: 'Health', icon: '♥' },
  { to: '/audit', label: 'Audit', icon: '☰' },
  { to: '/settings', label: 'Settings', icon: '⚙' },
];
```

Each row is a `NavLink` with `isActive` → `bg-blue-600/20 text-blue-300`. Brand text in the aside is `MT5 Intelligence` (not the A26 / `index.html` title `MT5 Trader Intelligence`). `useEffect(() => { startConnection(); }, [])` runs once per layout mount. Pages never call `onEvent`.

| Nav `to` | Label | Icon | `App.tsx` path | Path match | §46 exact label? |
|---|---|---|---|---|---|
| `/overview` | Overview | ◈ | `overview` | yes | **yes** |
| `/brokers` | Brokers | ⛁ | `brokers` | yes | **yes** |
| `/groups` | Groups | ⊞ | `groups` | yes (both ≠ A26) | **no** — §46 is `MT5 Groups` |
| `/traders` | Traders | ⚑ | `traders` | yes | **yes** |
| `/trades` | Trades | ⇄ | `trades` | yes | **no** — §46 is `Trade Explorer` |
| `/scoring` | Scoring | ★ | `scoring` | yes | **yes** |
| `/shadow` | Shadow | ◐ | `shadow` | yes | **no** — §46 is `Shadow Portfolio` |
| `/live` | Live | ▶ | `live` | yes | **no** — §46 is `Live Copy Portfolio` |
| `/fix` | FIX | ⚡ | `fix` | yes | **no** — §46 is `cTrader FIX` |
| `/risk` | Risk | ⚠ | `risk` | yes | **yes** |
| `/reconciliation` | Recon | ⟳ | `reconciliation` | yes | **no** — §46 is `Reconciliation` |
| `/health` | Health | ♥ | `health` | yes | **no** — §46 is `System Health` |
| `/audit` | Audit | ☰ | `audit` | yes | **yes** |
| `/settings` | Settings | ⚙ | `settings` | yes | **yes** |

Every sidebar `to` has a matching `App.tsx` route. No sidebar link 404s against this table.

**Not in the sidebar (by design or gap):**

| Item | Why |
|---|---|
| `/` | redirect only |
| `/traders/:brokerId/:login` | reached from `TradersPage` `<Link>` |
| `/login` | no route |
| `/models` | no route (C39: do not implement as a working page) |

A26 §5.3: left-nav labels **exactly** as §46; every §46 item visible to all authenticated roles. Current shell fails both: labels are abbreviated, Models and Trader Detail are omitted.

**Header strip (A26 §5.3):** `REAL_COPY_EXECUTION_ENABLED`, `STOP_NEW_EXECUTION`, FIX QUOTE health, FIX TRADE health, MT5 ingestion health — **MISSING**. Layout is `aside` + `main > Outlet`. No top bar.

---

## 5. A26 §5.2 / architecture §46 vs disk

Architecture §46 main navigation (verbatim order, `Architecture_v2.md` lines 1741–1758): Overview, Brokers, MT5 Groups, Traders, Trader Detail, Trade Explorer, Scoring, Models, Shadow Portfolio, Live Copy Portfolio, cTrader FIX, Risk, Reconciliation, System Health, Audit, Settings. A26 §5.2 adds `/login` (architecture §59). Default authenticated landing: `/overview`. Unknown routes → `/overview`.

| §46 / A26 item | Required route (A26) | In worktree `App.tsx`? | In HEAD `App.tsx`? | File under `pages/`? | Nav label | §73.B (route) |
|---|---|---|---|---|---|---|
| Login | `/login` | **no** | no | no | n/a | **MISSING** |
| Overview | `/overview` | yes | yes | `OverviewPage.tsx` | Overview | **EXISTS_NEEDS_REFACTOR** |
| Brokers | `/brokers` | yes | yes | `BrokersPage.tsx` | Brokers | **EXISTS_NEEDS_REFACTOR** |
| MT5 Groups | `/mt5-groups` | **wrong** `/groups` | same wrong | `GroupsPage.tsx` | Groups | **EXISTS_NEEDS_REFACTOR** |
| Traders | `/traders` | yes | yes | `TradersPage.tsx` | Traders | **EXISTS_NEEDS_REFACTOR** |
| Trader Detail | `/traders/:brokerId/:login` | yes | yes | `TraderDetailPage.tsx` | not in nav | **EXISTS_NEEDS_REFACTOR** |
| Trade Explorer | `/trades` | yes | yes | `TradeExplorerPage.tsx` | Trades | **EXISTS_NEEDS_REFACTOR** |
| Scoring | `/scoring` | yes | yes | `ScoringPage.tsx` | Scoring | **EXISTS_NEEDS_REFACTOR** |
| Models | `/models` | **no** | no | no | absent | **MISSING_BY_DESIGN** (C39 / Phase 6 closed) |
| Shadow Portfolio | `/shadow` | yes | yes | `ShadowPortfolioPage.tsx` | Shadow | **EXISTS_NEEDS_REFACTOR** |
| Live Copy Portfolio | `/live` | yes | **no** | `LiveCopyPage.tsx` (8-line stub) | Live | **EXISTS_NEEDS_REFACTOR** (chrome only) |
| cTrader FIX | `/fix` | yes | yes | `FixSessionsPage.tsx` | FIX | **EXISTS_NEEDS_REFACTOR** |
| Risk | `/risk` | yes | yes | `RiskPage.tsx` | Risk | **EXISTS_NEEDS_REFACTOR** |
| Reconciliation | `/reconciliation` | yes | yes | `ReconciliationPage.tsx` | Recon | **EXISTS_NEEDS_REFACTOR** |
| System Health | `/health` | yes | yes | `SystemHealthPage.tsx` | Health | **EXISTS_NEEDS_REFACTOR** |
| Audit | `/audit` | yes | **no** | `AuditPage.tsx` (8-line stub) | Audit | **EXISTS_NEEDS_REFACTOR** (chrome only) |
| Settings | `/settings` | yes | yes | `SettingsPage.tsx` | Settings | **EXISTS_NEEDS_REFACTOR** |

Score vs A26 §5.2 (17 named routes):

| Bucket | N | Routes |
|---|---:|---|
| Exact path present (worktree) | **14** | overview, brokers, traders, traders/:brokerId/:login, trades, scoring, shadow, live, fix, risk, reconciliation, health, audit, settings |
| Present at a **different** path | **1** | `/groups` instead of `/mt5-groups` |
| Absent | **2** | `/login`, `/models` |
| Extra vs A26 | **1** | `/` index redirect (allowed; is the default landing) |
| Missing required redirect | **1** | no `path="*"` → `/overview` |

A clean checkout of HEAD is **12 exact + 1 wrong + 4 absent** (`/live` and `/audit` drop out). Quote the worktree table only if the six unstaged lines remain.

---

## 6. In-app navigation besides the sidebar

Grep of `apps/web/src` for `Link`, `NavLink`, `useNavigate`, `useParams`, `Navigate`:

| Site | API | Target |
|---|---|---|
| `App.tsx` L23 | `<Navigate to="/overview" replace />` | `/` → `/overview` |
| `DashboardLayout.tsx` L31 | `NavLink to={n.to}` | 14 sidebar URLs |
| `TradersPage.tsx` L28 | `<Link to={\`/traders/${t.broker}/${t.login}\`}>` | detail URL uses **broker code**, not A26 UUID `brokerId` |
| `TraderDetailPage.tsx` L5 | `useParams()` → `{ brokerId, login }` | param name `brokerId` is filled with the **code** from the leaderboard link |

No `useNavigate`. No breadcrumb back-link from detail to `/traders`. No other programmatic routes.

Param contract drift (route-layer, not widget-layer): A26 names the segment `:brokerId` (UUID). The only producer of that URL interpolates `t.broker` (string code). The page then forwards that same string to `GET /api/traders/${broker}/${login}`. The **path template** matches A26; the **value semantics** do not.

---

## 7. What the layout does *not* do

| A26 / A62 shell item | On disk |
|---|---|
| `AuthProvider` around the router | **MISSING** — `main.tsx` is QueryClient + BrowserRouter only |
| `RequireAuth` wrapping `DashboardLayout` | **MISSING** — C18 still binding |
| `RequireRole` per page | **MISSING** |
| `/login` outside the dashboard chrome | **MISSING** |
| Left-nav labels exactly as §46 | **FAIL** — 7/16 exact |
| Header strip (`REAL_COPY`, `STOP_NEW`, MT5 / QUOTE / TRADE) | **MISSING** |
| Unknown URL → `/overview` | **FAIL** — empty `<Outlet />` under the chrome |
| `routes/paths.ts` + `NAV_ITEMS` (A62) | **MISSING** — labels duplicated as a local `nav` array |
| Vite `base` / React Router `basename` | none (root `/`) |
| Vite proxy `/api` + `/hubs` (A62) | **MISSING** — `vite.config.ts` is plugin + `port: 3000` only |
| SignalR hub path A26 `/hubs/ops` | client dials `/hubs/dashboard` (C28: API maps **no** hub) |

`startConnection()` is a layout side-effect, not a route. A failed hub start is `console.warn` only; the route table still renders.

---

## 8. HEAD vs worktree (do not collapse)

`git status --short` on the two subject files: ` M apps/web/src/App.tsx` and ` M apps/web/src/layouts/DashboardLayout.tsx`.

HEAD `App.tsx` (blob `6555ae04…`) already has the 13-page table + index redirect. It does **not** import or route `LiveCopyPage` / `AuditPage`. HEAD `DashboardLayout` (blob `1b40c4f6…`) has a 12-item nav (no Live, no Audit).

Worktree added:

```diff
+import LiveCopyPage from './pages/LiveCopyPage';
+import AuditPage from './pages/AuditPage';
+<Route path="live" element={<LiveCopyPage />} />
+<Route path="audit" element={<AuditPage />} />
+{ to: '/live', label: 'Live', icon: '▶' },
+{ to: '/audit', label: 'Audit', icon: '☰' },
```

Those four destinations exist on disk as 8-line stubs (`LiveCopyPage.tsx` 321 B / SHA `F85CF339…`; `AuditPage.tsx` 324 B / SHA `8DE2F9B0…`). Routing them is chrome, not a Live book or an audit log (C37 / C38 / C53).

---

## 9. Page-import 1:1 (router will resolve)

| `App.tsx` import | File | Default export name | Bytes | SHA-256 (this pass) |
|---|---|---|---:|---|
| `OverviewPage` | `pages/OverviewPage.tsx` | `OverviewPage` | 2078 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` |
| `BrokersPage` | `pages/BrokersPage.tsx` | `BrokersPage` | 1266 | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` |
| `GroupsPage` | `pages/GroupsPage.tsx` | `GroupsPage` | 1228 | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` |
| `TradersPage` | `pages/TradersPage.tsx` | `TradersPage` | 1604 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` |
| `TraderDetailPage` | `pages/TraderDetailPage.tsx` | `TraderDetailPage` | **2402** | **`C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2`** |
| `TradeExplorerPage` | `pages/TradeExplorerPage.tsx` | `TradeExplorerPage` | 1321 | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` |
| `ScoringPage` | `pages/ScoringPage.tsx` | `ScoringPage` | 1288 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` |
| `ShadowPortfolioPage` | `pages/ShadowPortfolioPage.tsx` | `ShadowPortfolioPage` | 628 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` |
| `LiveCopyPage` | `pages/LiveCopyPage.tsx` | `LiveCopyPage` | 321 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| `FixSessionsPage` | `pages/FixSessionsPage.tsx` | `FixSessionsPage` | 1312 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` |
| `RiskPage` | `pages/RiskPage.tsx` | `RiskPage` | 1148 | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` |
| `ReconciliationPage` | `pages/ReconciliationPage.tsx` | `ReconciliationPage` | 490 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` |
| `SystemHealthPage` | `pages/SystemHealthPage.tsx` | `SystemHealthPage` | 369 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` |
| `AuditPage` | `pages/AuditPage.tsx` | `AuditPage` | 324 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `SettingsPage` | `pages/SettingsPage.tsx` | `SettingsPage` | 459 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` |

14/15 page SHAs match D08. **`TraderDetailPage` changed after D08**: D08 had 1592 B / SHA `6CAE0FC9…` (13:16:00); this pass is 2402 B / 56 lines / SHA `C849449B…` (13:35:59). The **route** (`traders/:brokerId/:login`) did not change. This report does not re-audit widgets.

---

## 10. Route → hook (data path, not a completeness claim)

| Route | Hook in the page | Client path (unversioned) | A26 primary GET |
|---|---|---|---|
| `/overview` | `useOverview` | `GET /api/overview` | `GET /api/v1/overview` |
| `/brokers` | `useBrokers` | `GET /api/brokers` | `GET /api/v1/brokers` |
| `/groups` | `useGroups` | `GET /api/groups` | `GET /api/v1/mt5/groups` |
| `/traders` | `useTraders({})` | `GET /api/traders` | `GET /api/v1/traders` |
| `/traders/:brokerId/:login` | `useTraderDetail` | `GET /api/traders/{broker}/{login}` | `GET /api/v1/traders/{brokerId}/{login}` |
| `/trades` | `useTrades` | `GET /api/trades` | `GET /api/v1/trades` |
| `/scoring` | `useTraders({})` reuse | `GET /api/traders` | `GET /api/v1/scoring/summary` |
| `/shadow` | **none** | — | `GET /api/v1/shadow/portfolio` |
| `/live` | **none** | — | `GET /api/v1/live/portfolio` |
| `/fix` | `useFixSessions` | `GET /api/fix/sessions` (5 s) | `GET /api/v1/fix/sessions` |
| `/risk` | `useRiskStatus` | `GET /api/risk` (5 s) | `GET /api/v1/risk/dashboard` |
| `/reconciliation` | `useReconciliation` | `GET /api/reconciliation/status` | `GET /api/v1/reconciliation` |
| `/health` | `useHealth` | `GET /api/health` (10 s) | `GET /api/v1/health` |
| `/audit` | **none** | — | `GET /api/v1/audit` |
| `/settings` | `useSettings` | `GET /api/settings` | `GET /api/v1/settings` |

Zero hooks talk `/api/v1`. Scoring reuses the leaderboard hook. Shadow / Live / Audit have routes and no fetch. That is a **data-layer** remainder; the route table still mounts the pages.

---

## 11. Unknown-URL behaviour (measured from source)

`react-router-dom` v6: a parent layout route with no matching child renders the layout and an empty `<Outlet />`. There is no `path="*"` and no sibling catch-all.

Consequence:

| URL | What paints |
|---|---|
| `/` | replace-navigate to `/overview` |
| `/overview` (and the other 14 page paths) | layout + page |
| `/mt5-groups` | layout chrome + **empty main** (A26's real groups URL is a miss) |
| `/models` | layout chrome + **empty main** |
| `/login` | layout chrome + **empty main** (login would be inside the dashboard if it existed) |
| `/no-such-page` | layout chrome + **empty main** |
| `/traders/` (trailing only) | matches `traders` list, not detail |

A26: unknown routes → `/overview`. Current: unknown routes stay on the bad URL and look like a blank dashboard. **FAIL**.

---

## 12. Stale-vs-later

| Prior claim | This pass |
|---|---|
| A62: 13 imports, 0 page files; Live/Audit/Models/Login missing | **Superseded** for Live/Audit **files + routes** (worktree). Login still missing. Models still absent (C39). Pages exist. |
| A62: `/groups` instead of `/mt5-groups` | **Still true.** |
| B22: 13 imports / 13 files; no Live/Audit | **Superseded.** 15/15. |
| B31: Models + Live + Audit nav gaps | Models still absent. Live + Audit nav **present on worktree**, absent on HEAD. |
| C08 / D08: 15 routes + index; SHA `A0E92C9779A0C777…` | **Confirmed.** Same `App.tsx` / layout SHAs. D08 page-depth sections still apply except `TraderDetailPage` SHA (changed 13:35:59). |
| C18: no auth on the router | **Confirmed.** |
| C37 / C38 / C53: Live/Audit are chrome stubs | **Confirmed.** Route existence ≠ page completeness. |
| C39: do not create Models to complete nav | **Confirmed.** Absence of `/models` is not a D38 coding ticket. |

---

## 13. Honest remainders (route layer only)

Do **not** treat this list as a widget build ticket. Route-table remainders only:

1. **`/login` + `RequireAuth`** — A26 §5.2 / §5.3 / C18. Every current destination is public chrome.
2. **`path="*"` → `/overview`** — A26 unknown-route rule. `/mt5-groups` and `/models` currently blank the outlet.
3. **`/groups` → `/mt5-groups`** (and matching nav `to`) — path contract, not a new page file. Needs a redirect from the old path if anything bookmarked `/groups`.
4. **Commit or revert** the six unstaged Live/Audit lines. HEAD does not have those routes.
5. **Nav labels** — A26 §5.3 wants §46 strings verbatim (`MT5 Groups`, `Trade Explorer`, `Shadow Portfolio`, `Live Copy Portfolio`, `cTrader FIX`, `Reconciliation`, `System Health`). Optional: add Trader Detail only if it can be a real dest (it cannot without params).
6. **Do not add `/models` as a working page** — C39 / Phase 6 closed. An honest “Phase 6 not open” placeholder is optional, not required for §69.
7. **Header strip** — layout gap (A26 §5.3), not a new `<Route>`.
8. **`:brokerId` semantics** — leaderboard interpolates broker **code**. Rename the param or stop calling it `brokerId`.

None of these were implemented this pass.

---

## 14. Evidence

- Router: `D:\Prop\apps\web\src\App.tsx` lines 1–41 (full file). SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`. 2062 bytes, 42 physical lines.
- Layout: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 1–44 (full file). SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`. 1854 bytes, 44 physical lines.
- Entry: `D:\Prop\apps\web\src\main.tsx` lines 14–21 (`QueryClientProvider` → `BrowserRouter` → `App`).
- Host: `D:\Prop\apps\web\index.html` line 9 `#root`; `vite.config.ts` `server.port = 3000`, no `base`.
- Law: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1739–1758; `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2 lines 285–307, §5.3 lines 309–313.
- Git: HEAD `398a14200ec65714c4077eed55c46808382ca1e3`; `git diff` +4 / +2 on the two subject files.
- Product source edited: **No.**
