# E003 — React route × API endpoint matrix (`apps/web` × `apps/api`)

| Field | Value |
|---|---|
| Agent | E003 (senior engineer, route matrix only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:47:06+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\E003_route_matrix.md` |
| Assigned | List all React routes in `apps/web` and matching API endpoints. Write this file. **Do not modify product source.** |
| Workspace | Vite app is `D:\Prop\apps\web` (not under `D:\Prop\src`). API host is `D:\Prop\apps\api`. |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full read of `App.tsx`, `DashboardLayout.tsx`, `main.tsx`, every `pages/*.tsx` (15), `api/{client,hooks,signalr}.ts`, `types/index.ts`, `vite.config.ts`, `package.json`; full read of `apps/api/Program.cs`, `Controllers/SettingsController.cs`, `TraderIntelligence.Api.http`, `TraderIntelligence.Api.csproj`, `launchSettings.json`; `IDashboardQueries` + `DashboardModels.cs` + `ReconstructedTrade.cs`; PowerShell SHA-256 + byte / physical-line / last-write; `git rev-parse` + `git status --short` on the four dirty subjects; grep `MapGet`/`MapPost`/`MapHub`/`AddControllers`/`path=`/`client.get`. **API process was not launched.** Route list is from source, not HTTP capture. |
| Binding law | Architecture v2 §46 nav; A26 §5.2 route map + §6 page contracts; A63 first-useful `/api/v1` catalog |
| Prior (do not collapse) | D08 (web census), D30 (API maps), D38 (router table), D39 (hooks vs maps), A26 (spec), A63 (catalog). File SHAs of `App.tsx` / `hooks.ts` / `Program.cs` are **unchanged** vs those reports. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **route × endpoint matrix**. It is **not** a claim that widgets match §47–§54, that `/api/v1` exists, that RBAC is on, or that the dashboard is first-useful.

---

## 0. Verdict

**Worktree React has 16 destinations (1 index redirect + 15 pages) under one `DashboardLayout`. 12 of 15 pages fetch unversioned `GET /api/*`. The API host has 15 live maps (14 GET + 1 POST). 11 hooks hit 11 of those maps. 3 pages have no fetch. 4 host maps have no React consumer. SignalR `/hubs/dashboard` is client-only (404 on the host). `SettingsController` is dead (no `AddControllers` / `MapControllers`). Zero `/api/v1/**` routes exist.**

| Question | Count | Answer |
|---|---:|---|
| React destinations in `App.tsx` | **16** | 1 index redirect + 15 page routes |
| Sidebar `NavLink`s | **14** | Trader Detail is a child route, not a nav row |
| Page files on disk | **15** | 15/15 imported; 0 orphans |
| Pages that call a hook | **12** | Scoring reuses `useTraders`; Shadow / Live / Audit call **none** |
| `useQuery` hooks | **11** | all `client.get('/api/…')`; 0 mutations |
| Live API maps (`Program.cs`) | **15** | 14 `MapGet` + 1 `MapPost` |
| Hook path exists on host | **11 / 11** | demo coincidence, not A26/A63 |
| Host maps with a React consumer | **11 / 15** | `/health`, `/ready`, `/api/risk/status`, `POST /api/ops/resync` unused |
| `/api/v1/**` maps | **0** | A26 / A63 catalog is **MISSING** on this host |
| `MapHub` / `AddSignalR` | **0** | client dials `/hubs/dashboard` → expected **404** |
| Wired MVC controllers | **0** | `SettingsController` exists on disk and is **not** mapped |
| Auth / RBAC / Vite `/api` proxy | **0** | CORS `AllowAnyOrigin`; axios `baseURL` `http://localhost:5000` |

§73.B for this **demo BFF pairing**: **EXISTS_NEEDS_REFACTOR**.  
§73.B for A26/A63 contract: **MISSING** (`/api/v1` + `/hubs/ops` + `/login` + RBAC).  
§73.B for `POST /api/ops/resync`: **UNSAFE** (anonymous write, no React caller).  
§73.B for `GET /api/trades`: **UNSAFE** as a contract (raw EF `ReconstructedTrade` entities).  
§73.B for `/hubs/dashboard`: **MISSING** on the host.

Do **not** treat 11/11 demo-path hits as catalog compliance. Do **not** treat page chrome as a working Live book, Shadow book, Audit log, or Models surface.

---

## 1. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) |
|---|---:|---:|---|---|
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38+05:30 |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39+05:30 |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02+05:30 |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 |
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | 2026-08-18T13:06:19+05:30 |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | 3732 | 94 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | 2026-08-18T13:37:39+05:30 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38+05:30 |
| `D:\Prop\apps\api\Properties\launchSettings.json` | 1125 | 41 | `BC0228981D29FFCF8D94737AF3446D96AB29415ACFF494687250265E266CD7F0` | 2026-08-18T13:32:01+05:30 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 |

Git vs HEAD `398a142`: **unstaged** `M` on `apps/web/src/App.tsx`, `apps/web/src/layouts/DashboardLayout.tsx`, `apps/web/src/api/hooks.ts`, `apps/api/Program.cs`. HEAD checkout does **not** have `/live` or `/audit` routes (D38). Quote the **worktree** table.

Vite `server.port` = **3000**. Axios `baseURL` = `import.meta.env.VITE_API_URL || 'http://localhost:5000'`. API `http` profile = `http://localhost:5000`. No Vite `/api` or `/hubs` proxy.

---

## 2. React route table (authoritative: `App.tsx`)

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

Single `BrowserRouter` in `main.tsx` (no `basename`, no `createBrowserRouter`, no `path="*"`, no auth wrapper). Layout `useEffect` calls `startConnection()` → SignalR `${VITE_API_URL||http://localhost:5000}/hubs/dashboard`. Failed start is `console.warn` only.

| # | Browser URL | `path` | Page | Sidebar | In-page nav |
|---|---|---|---|---|---|
| 0 | `/` | `index` | `<Navigate to="/overview" replace />` | n/a | — |
| 1 | `/overview` | `overview` | `OverviewPage` | Overview | — |
| 2 | `/brokers` | `brokers` | `BrokersPage` | Brokers | — |
| 3 | `/groups` | `groups` | `GroupsPage` | Groups | **≠** A26 `/mt5-groups` |
| 4 | `/traders` | `traders` | `TradersPage` | Traders | `<Link to=/traders/${t.broker}/${t.login}>` |
| 5 | `/traders/:brokerId/:login` | `traders/:brokerId/:login` | `TraderDetailPage` | **no** | `useParams()`; value is broker **code**, not UUID |
| 6 | `/trades` | `trades` | `TradeExplorerPage` | Trades | — |
| 7 | `/scoring` | `scoring` | `ScoringPage` | Scoring | — |
| 8 | `/shadow` | `shadow` | `ShadowPortfolioPage` | Shadow | — |
| 9 | `/live` | `live` | `LiveCopyPage` | Live | unstaged vs HEAD; 8-line stub |
| 10 | `/fix` | `fix` | `FixSessionsPage` | FIX | — |
| 11 | `/risk` | `risk` | `RiskPage` | Risk | — |
| 12 | `/reconciliation` | `reconciliation` | `ReconciliationPage` | Recon | — |
| 13 | `/health` | `health` | `SystemHealthPage` | Health | — |
| 14 | `/audit` | `audit` | `AuditPage` | Audit | unstaged vs HEAD; 8-line stub |
| 15 | `/settings` | `settings` | `SettingsPage` | Settings | — |

**Not in the router:** `/login`, `/models`, `/mt5-groups`, catch-all `*`. Unknown URLs paint layout chrome + empty `<Outlet />` (A26 wants redirect to `/overview`).

---

## 3. Master matrix — React route → hook → live API → catalog

Match column:

- **HIT** — page hook path equals a live `MapGet` (demo unversioned `/api/*`).
- **REUSE** — page uses another page's endpoint (no dedicated map).
- **NONE** — page has no fetch.
- **ORPHAN-API** — host map with no React consumer (listed in §5).
- **DEAD** — file exists, not mapped.
- **MISS** — catalog / client target not on the host.

| Browser route | Page file | Page SHA-256 | Hook | HTTP the browser sends | Live `Program.cs` map | Handler / body | A26 / A63 catalog | Match |
|---|---|---|---|---|---|---|---|---|
| `/` | (redirect) | — | none | none | n/a | — | landing `/overview` | HIT (redirect only) |
| `/overview` | `OverviewPage.tsx` | `6497193F…` | `useOverview` | `GET /api/overview` | L54 `MapGet("/api/overview")` | `IDashboardQueries.GetOverviewAsync` → `OverviewDto` | `GET /api/v1/overview` | **HIT** demo / **MISS** catalog |
| `/brokers` | `BrokersPage.tsx` | `274754E3…` | `useBrokers` | `GET /api/brokers` | L55 | `GetBrokersAsync` → `BrokerStatusDto[]` | `GET /api/v1/brokers` | **HIT** / **MISS** |
| `/groups` | `GroupsPage.tsx` | `4F787482…` | `useGroups` | `GET /api/groups` | L56 | `GetGroupsAsync` → `GroupRowDto[]` | `GET /api/v1/mt5/groups` | **HIT** / **MISS** (path name too) |
| `/traders` | `TradersPage.tsx` | `0AF0FF5B…` | `useTraders({})` | `GET /api/traders` | L57–58 `?broker&state` | `GetTradersAsync` → `TraderRowDto[]` | `GET /api/v1/traders` | **HIT** / **MISS** (no envelope, no A92 filters) |
| `/traders/:brokerId/:login` | `TraderDetailPage.tsx` | `C849449B…` | `useTraderDetail` | `GET /api/traders/{broker}/{login}` | L59–60 `{broker}/{login:long}` | `GetTraderDetailAsync` → `TraderDetailDto?` (`header`+`trades`) or **204** if null | `GET /api/v1/traders/{brokerId}/{login}` | **HIT** / **MISS** (`:brokerId` is a **code**) |
| `/trades` | `TradeExplorerPage.tsx` | `7EE11EB9…` | `useTrades` | `GET /api/trades` | L63–71 | raw `ReconstructedTrade` last 200; `login` filter used; `broker` query **accepted unused** | `GET /api/v1/trades` | **HIT** / **MISS** (EF leak) |
| `/scoring` | `ScoringPage.tsx` | `F417592E…` | `useTraders({})` | `GET /api/traders` | same as `/traders` | same `TraderRowDto[]` | `GET /api/v1/scoring/summary` | **REUSE** / **MISS** (no scoring map) |
| `/shadow` | `ShadowPortfolioPage.tsx` | `608C8C2D…` | **none** | — | **no** `/api/shadow/*` | static copy only | `GET /api/v1/shadow/portfolio` (+ positions, performance, copy-intents) | **NONE** / **MISS** |
| `/live` | `LiveCopyPage.tsx` | `F85CF339…` | **none** | — | **no** `/api/live/*` | 8-line “execution off” stub | A26 live book (not first-useful) | **NONE** / **MISS** (correct while send is off) |
| `/fix` | `FixSessionsPage.tsx` | `EC933266…` | `useFixSessions` (5 s) | `GET /api/fix/sessions` | L61 | `GetFixSessionsAsync` → `FixSessionDto[]` | `GET /api/v1/fix/sessions` (+ `/quote`, `/trade`, events) | **HIT** / **MISS** (sessions only) |
| `/risk` | `RiskPage.tsx` | `FC4C5F05…` | `useRiskStatus` (5 s) | `GET /api/risk` | L62 | `GetRiskAsync` → `RiskDashboardDto` | `GET /api/v1/risk/snapshot` (+ rejections, kill-switch, stop-new) | **HIT** / **MISS** |
| `/reconciliation` | `ReconciliationPage.tsx` | `BC036D09…` | `useReconciliation` | `GET /api/reconciliation/status` | L35–41 | **hardcoded** `{ lastReconciliation: UtcNow, unknownPositions:0, mismatches:0, orphanFills:0 }` | `GET /api/v1/reconciliation` (+ runs, issues, run, ack) | **HIT** / **MISS** (stub body) |
| `/health` | `SystemHealthPage.tsx` | `03BDBC76…` | `useHealth` (10 s) | `GET /api/health` | L26–33 | **hardcoded** inventory (FakeMt5 “healthy”, QUOTE “no live TLS”, redis false, `outboxBacklog:0`) | `GET /api/v1/health` + `/health` + `/health/ready` | **HIT** (`/api/health`) / **MISS** catalog; liveness `/health` unused by UI |
| `/audit` | `AuditPage.tsx` | `8DE2F9B0…` | **none** | — | **no** `/api/audit*` | 8-line stub | `GET /api/v1/audit/logs` | **NONE** / **MISS** |
| `/settings` | `SettingsPage.tsx` | `57D41B90…` | `useSettings` | `GET /api/settings` | L42–47 | **hardcoded** `{ riskLimits, featureFlags.REAL_COPY_EXECUTION_ENABLED=false, two broker labels }` | `GET /api/v1/settings/public` | **HIT** / **MISS** |
| (layout, all pages) | `DashboardLayout.tsx` | `48F7073E…` | `startConnection` | `WS /hubs/dashboard` | **no** `MapHub` | expected **404**; `console.warn` | A26/A63 `/hubs/ops` | **MISS** |

`types/index.ts` is **unused** by hooks and pages (`any` everywhere). Axios `r.data` is the raw body (no `{ data: … }` envelope).

---

## 4. Hook inventory (`hooks.ts`)

| # | Hook | Query key | Path | Extra | Consumers |
|---|---|---|---|---|---|
| 1 | `useOverview` | `['overview']` | `GET /api/overview` | — | `OverviewPage` |
| 2 | `useBrokers` | `['brokers']` | `GET /api/brokers` | — | `BrokersPage` |
| 3 | `useGroups` | `['groups']` | `GET /api/groups` | — | `GroupsPage` |
| 4 | `useTraders` | `['traders', filters]` | `GET /api/traders` `?broker&state` | — | `TradersPage`, `ScoringPage` |
| 5 | `useTraderDetail` | `['trader', broker, login]` | `GET /api/traders/${broker}/${login}` | `enabled: !!broker && !!login` | `TraderDetailPage` |
| 6 | `useTrades` | `['trades']` | `GET /api/trades` | **no** query params | `TradeExplorerPage` |
| 7 | `useFixSessions` | `['fix-sessions']` | `GET /api/fix/sessions` | `refetchInterval: 5000` | `FixSessionsPage` |
| 8 | `useRiskStatus` | `['risk']` | `GET /api/risk` | `refetchInterval: 5000` | `RiskPage` |
| 9 | `useReconciliation` | `['reconciliation']` | `GET /api/reconciliation/status` | — | `ReconciliationPage` (`JSON.stringify`) |
| 10 | `useHealth` | `['health']` | `GET /api/health` | `refetchInterval: 10000` | `SystemHealthPage` (`JSON.stringify`) |
| 11 | `useSettings` | `['settings']` | `GET /api/settings` | — | `SettingsPage` (`JSON.stringify`) |

0 `useMutation`. 0 `useQuery<T>`. QueryClient defaults (`main.tsx`): retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000`.

---

## 5. Complete live API surface (`Program.cs`)

Fifteen anonymous maps. CORS `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`. JSON enums via `JsonStringEnumConverter`. Startup: `EnsureCreatedAsync` + `DemoSeeder`. **No** `AddAuthentication`, `AddAuthorization`, `AddSignalR`, `AddControllers`, `MapControllers`, `MapHub`, `UseSwaggerUI`, `/api/v1`.

| # | Method | Path | `Program.cs` | Body | React consumer | `.http` sample? |
|---|---|---|---|---|---|---|
| 1 | `GET` | `/health` | 25 | `{ status, utc }` process liveness | **none** | yes |
| 2 | `GET` | `/api/health` | 26–33 | hardcoded inventory (honest-ish fake MT5 / no TLS) | `/health` page | **no** |
| 3 | `GET` | `/api/risk/status` | 34 | `RiskDashboardDto` | **none** (alias of #13) | **no** |
| 4 | `GET` | `/api/reconciliation/status` | 35–41 | hardcoded zeros + `UtcNow` | `/reconciliation` | **no** |
| 5 | `GET` | `/api/settings` | 42–47 | hardcoded flags / limits / broker labels | `/settings` | **no** |
| 6 | `GET` | `/ready` | 48–52 | `{ ready: true, brokers }` via `CountAsync` | **none** | **no** |
| 7 | `GET` | `/api/overview` | 54 | `OverviewDto` | `/overview` | yes |
| 8 | `GET` | `/api/brokers` | 55 | `BrokerStatusDto[]` | `/brokers` | yes |
| 9 | `GET` | `/api/groups` | 56 | `GroupRowDto[]` | `/groups` | yes |
| 10 | `GET` | `/api/traders` | 57–58 | `TraderRowDto[]` | `/traders` **and** `/scoring` | yes |
| 11 | `GET` | `/api/traders/{broker}/{login:long}` | 59–60 | `TraderDetailDto?` | `/traders/:brokerId/:login` | **no** |
| 12 | `GET` | `/api/fix/sessions` | 61 | `FixSessionDto[]` | `/fix` | yes |
| 13 | `GET` | `/api/risk` | 62 | same as #3 | `/risk` | yes |
| 14 | `GET` | `/api/trades` | 63–71 | raw `ReconstructedTrade` × ≤200 | `/trades` | **no** |
| 15 | `POST` | `/api/ops/resync` | 73–82 | ingest ACHIEVER+STARWAVEFX; rebuild logins 10001/10002/10003/99001 | **none** | **no** |

### 5.1 Implicit / unmapped (still relevant to the matrix)

| Method | Path | If process is up | Notes |
|---|---|---|---|
| `GET` | `/swagger/v1/swagger.json` | **200 in Development** | `UseSwagger()` without UI. Schema of the 15 maps. |
| `GET` | `/swagger` | **404** | `UseSwaggerUI()` never called. All three `launchSettings` profiles still `launchUrl: swagger`. |
| `GET` | `/weatherforecast` | **404** | Template route **GONE**. |
| `GET`/`WS` | `/hubs/dashboard` | **404** | React layout target. |
| `GET`/`WS` | `/hubs/ops` | **404** | A26/A63 hub. |
| `*` | `/api/v1/**` | **404** | Entire catalog. |
| `GET` | `/api/settings` via `SettingsController` | **not reached** | Dead controller; live body is the `MapGet` on L42. |
| `PUT` | `/api/settings` | **404** | Controller `Put` would write Redis keys; **not mapped**. No React `PUT`. |

---

## 6. Dead controller (not in the live matrix)

`D:\Prop\apps\api\Controllers\SettingsController.cs` (`[Route("api/settings")]`, `HttpGet` + `HttpPut`, injects `IConnectionMultiplexer` + `IConfiguration`).

`TraderIntelligence.Api.csproj` is `Microsoft.NET.Sdk.Web` but `Program.cs` never calls `AddControllers()` or `MapControllers()`. The class is compile-visible and **unroutable**.

If it were mapped it would **collide** with live `GET /api/settings` and would expose a Redis-backed `PUT` (risk limits + feature flags, not passwords). Today the browser only sees the hardcoded `MapGet`. Do not treat the controller as a matching endpoint.

---

## 7. A26 §5.2 / architecture §46 vs disk

| A26 route | Worktree React | Live API pair | Catalog API | Status |
|---|---|---|---|---|
| `/login` | **absent** | none | `POST /api/v1/auth/login` | **MISSING** both sides |
| `/overview` | yes | `GET /api/overview` | `GET /api/v1/overview` | demo pair only |
| `/brokers` | yes | `GET /api/brokers` | `GET /api/v1/brokers` | demo pair only |
| `/mt5-groups` | **wrong** `/groups` | `GET /api/groups` | `GET /api/v1/mt5/groups` | path drift both sides |
| `/traders` | yes | `GET /api/traders` | `GET /api/v1/traders` | demo pair only |
| `/traders/:brokerId/:login` | yes (code, not UUID) | `GET /api/traders/{broker}/{login}` | `GET /api/v1/traders/{brokerId}/{login}` | param semantics drift |
| `/trades` | yes | `GET /api/trades` (EF) | `GET /api/v1/trades` | demo pair; contract **UNSAFE** |
| `/scoring` | yes | **reuses** traders list | `GET /api/v1/scoring/summary` | page without scoring API |
| `/models` | **absent** (C39 / Phase 6 closed) | none | none in first-useful | **MISSING_BY_DESIGN** |
| `/shadow` | chrome only | **none** | `GET /api/v1/shadow/*` | page without API |
| `/live` | stub | **none** | not first-useful | chrome only (honest) |
| `/fix` | yes | `GET /api/fix/sessions` | `GET /api/v1/fix/sessions` | demo pair; no `/quote` `/trade` |
| `/risk` | yes | `GET /api/risk` | `GET /api/v1/risk/snapshot` | demo pair |
| `/reconciliation` | yes | stub `GET /api/reconciliation/status` | `GET /api/v1/reconciliation` | stub pair |
| `/health` | yes | stub `GET /api/health` | `GET /api/v1/health` | stub pair |
| `/audit` | stub | **none** | `GET /api/v1/audit/logs` | chrome only |
| `/settings` | yes | stub `GET /api/settings` | `GET /api/v1/settings/public` | stub pair |
| `*` → `/overview` | **absent** | n/a | required | **FAIL** |

A63 first-useful extras with **no** React route and **no** live map: `GET /health/ready` (host has `/ready` instead), `GET /api/v1/system/events`, auth quartet, `GET /api/v1/brokers/{id}`, `PATCH /api/v1/mt5/groups/{id}`, `GET /api/v1/mt5/accounts`, trader subresources (`features`, `risk-flags`, `lot-timeline`, `holding-times`, `shadow`, `scores`), `PATCH .../state`, `GET /api/v1/copy-intents`, `GET /api/v1/fix/quote|trade`, `GET /api/v1/risk/rejections|kill-switch`, `POST /api/v1/risk/stop-new-execution`, recon run/ack.

---

## 8. DTO fields the wired pages actually read

Honesty check: a **HIT** in §3 means the HTTP path exists. It does **not** mean the page fields match the catalog DTO.

| Page | Fields read from the response | Server type |
|---|---|---|
| Overview | `totalAccounts`, `connectedBrokers`, `xauTraders`, `tradersWithThreeTrades`, `watch`, `shadow`, `liveCandidates`, `riskBlocked`, `shadowPnl`, `destinationRealPnl`, `mt5Healthy`, `quoteHealthy`, `tradeHealthy`, `realCopyEnabled` | `OverviewDto` (camelCase). Page does **not** render `live`, `xauGross`, `xauNet`. |
| Brokers | `code`, `displayName`, `server`, `managerLoginMasked`, `groupCount`, `accountCount`, `connected` | `BrokerStatusDto`. Mask is numeric (`login/100*100`); page appends extra `**`. |
| Groups | `broker`, `group`, `accounts`, `enabledForAnalysis`, `planMapping` | `GroupRowDto`. |
| Traders / Scoring | `broker`, `login`, `group`, `completedXauTrades`, `netSourcePnl`, `earlyScore`, `riskScore`, `behaviorScore` (Scoring only; **not on DTO** → `?? 0`), `martingale`, `averagingDown`, `lotEscalation`, `state` | `TraderRowDto`. No `behaviorScore` property — Scoring always shows `0.0`. |
| Trader Detail | `data.header ?? data` then `broker`, `login`, `state`, `completedXauTrades`, `earlyScore`, `riskScore`, `netSourcePnl`, `martingale`, `averagingDown`, `mlProbability`; `data.trades[]` `positionId`, `canonicalSymbol`, `netRealizedPnl`, `isFirstThree` | `TraderDetailDto`. Compatible with the current host. |
| Trades | `id`, `login`, `canonicalSymbol`, `direction`, `openedAt`, `closedAt`, `maxVolumeLots`, `netRealizedPnl`, `completed` | **EF entity**, not `TradeHighlightDto`. |
| FIX | `qualifier`, `host`, `port`, `connected`, `loggedOn`, `status`, `inboundSeq`, `outboundSeq`, `reconnectCount`, `bid`, `ask`, `quoteAgeSeconds`, `instrumentId`, `executionEnabled` | `FixSessionDto[]`. |
| Risk | `killSwitch`, `realCopyEnabled`, `dailyPnl`, `xauNet`, `recentRejectReasons` | `RiskDashboardDto`. Page ignores `drawdown`, `xauLong`, `xauShort`. |
| Recon / Health / Settings | entire JSON dump | anonymous / hardcoded objects. |

---

## 9. Counts (do not greenwash)

| Bucket | N |
|---|---:|
| React destinations | 16 |
| Pages with a matching live GET | **12** (Scoring counted as reuse of traders) |
| Pages with a **dedicated** live GET | **11** |
| Pages with zero HTTP | **3** (Shadow, Live, Audit) |
| Live maps consumed by React | **11** |
| Live maps unused by React | **4** (`/health`, `/ready`, `/api/risk/status`, `POST /api/ops/resync`) |
| Catalog `/api/v1` routes implemented | **0** |
| SignalR hubs implemented | **0** |
| Auth routes implemented | **0** |
| A26 named routes at the specified path | **14 / 17** (`/login` absent, `/models` absent, `/mt5-groups` is `/groups`) |

---

## 10. Remainders (matrix layer only)

1. **Replace**, do not grow, the unversioned `/api/*` client against A26 / A63 (`/api/v1` + envelopes + RBAC).
2. **Retarget** SignalR to `/hubs/ops` and **map a hub**, or stop dialing `/hubs/dashboard` from the layout.
3. **`/groups` vs `/mt5-groups`** — path contract; keep a redirect if `/groups` is bookmarked.
4. **`/scoring`** needs `GET /api/v1/scoring/summary` (and trader score history). Reusing the leaderboard is not a scoring API.
5. **`/shadow` and `/audit`** have chrome and **no** maps. First-useful requires those GETs.
6. **`/live`** staying empty while `REAL_COPY_EXECUTION_ENABLED=false` is honest. Do not invent a live book.
7. **Do not add `/models`** as a working page (C39 / Phase 6 closed).
8. **`/login` + `RequireAuth`** — every current destination is public chrome (C18).
9. **`path="*"` → `/overview`** — unknown URLs currently blank the outlet.
10. **Do not wire `SettingsController` as-is** — it would collide with `MapGet("/api/settings")` and add an anonymous Redis `PUT`.
11. **`POST /api/ops/resync`** is an unauthenticated mutation with no UI. Keep it off the public internet.
12. **`GET /api/trades`** must become an allow-listed DTO; stop returning EF entities.

None of these were implemented this pass.

---

## 11. Evidence

- Router: `D:\Prop\apps\web\src\App.tsx` SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`
- Layout + nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`
- SignalR client: `D:\Prop\apps\web\src\api\signalr.ts` L10 `/hubs/dashboard`
- Host: `D:\Prop\apps\api\Program.cs` SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` — 14 `MapGet` + 1 `MapPost`
- Dead controller: `D:\Prop\apps\api\Controllers\SettingsController.cs` SHA-256 `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F`
- DTOs: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` SHA-256 `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496`
- Law: A26 §5.2 (`D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` L285–307); A63 §7.1 catalog
- Adjacent measured reports: D30 (host), D38 (router), D39 (hooks)
- Git: HEAD `398a14200ec65714c4077eed55c46808382ca1e3`; worktree dirty on App / layout / hooks / Program.cs
- Product source edited: **No.**
