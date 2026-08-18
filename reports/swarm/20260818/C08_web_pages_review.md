# C08 — `apps/web/src/pages` census vs `App.tsx` imports

| Field | Value |
|---|---|
| Agent | C08 (senior engineer, web pages / router import review only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:23:27+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Target | `D:\Prop\apps\web\src\pages\` + `D:\Prop\apps\web\src\App.tsx` |
| Product source modified | **No.** This report is the only write. |
| Method | `list_dir` on `apps/web/src/pages`; read every page + `App.tsx` + `DashboardLayout.tsx`; SHA-256 + physical-line census; compare A26 §5.2 and architecture §46 |
| Precedence | On-disk tree supersedes A62 §0 / §3 and B22 (13-file snapshot). A26 + architecture §46 remain binding for required nav that was never created. |

---

## 0. Verdict

**`App.tsx` imports match `pages/` 15/15.**

`list_dir` of `D:\Prop\apps\web\src\pages` returned **15** `*.tsx` files. `App.tsx` lines 3–17 import **15** page modules. Every import resolves to a file that `export default function <SameName>()`. Zero dangling imports. Zero orphan page files. The only product `pages/` directory is this one.

This is **not** the same snapshot as B22 (13 files, Live/Audit missing). Live and Audit files exist now and are wired. B10 / B22 “13 pages / Live+Audit absent” is **stale**.

Import match ≠ dashboard complete. Two A26 / §46 pages are still **never written** (`Login`, `Models`). Groups is present under the **wrong path** (`/groups` vs A26 `/mt5-groups`). Several pages are static stubs. Hooks still call unversioned `/api/*`.

| Question | Count | Answer |
|---|---|---|
| `list_dir` files under `apps/web/src/pages` | **15** | not empty |
| `App.tsx` `./pages/*` default imports | **15** | all present on disk |
| Import whose file is missing | **0** | router will not fail module resolution |
| Page file not imported by `App.tsx` | **0** | no orphans |
| Default export name ≠ import binding | **0** | 15/15 `export default function <Name>` |
| Other `pages/` dirs under `D:\Prop` (exclude vendor/bin/obj/node_modules) | **1** | only `apps\web\src\pages` |
| `pages/` under `D:\Prop\src` | **0** | looking only at the C# workspace is a false miss |
| A26 / §46 pages with **no** file and **no** route | **2** | Login, Models |
| `pages/` empty? | — | **No** |
| Import graph healthy? | — | **Yes** |

Do **not** recreate the 15 files. Do **not** treat this as a broken stub with missing modules. Depth vs A26 is a later coding task.

---

## 1. Method

| Source | Path / action |
|---|---|
| Router | `D:\Prop\apps\web\src\App.tsx` (2062 bytes, 42 physical lines, SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`, written 2026-08-18T13:20:38+05:30) |
| Nav shell | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` (1854 bytes, SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`) |
| Directory listing | `list_dir` `D:\Prop\apps\web\src\pages` — **15** files, names below |
| File census | PowerShell `Get-ChildItem` + `Get-FileHash SHA256` + physical / non-blank line counts |
| Export check | `export default function` in every `*.tsx` under `pages/` |
| Other `pages/` dirs | recursive directory search under `D:\Prop`, exclude `vendor` / `bin` / `obj` / `node_modules` |
| Binding nav | architecture §46 (`…Architecture_v2.md` lines 1739–1758); A26 §5.2 route map |
| Prior census | `B22_web_missing_pages.md` (13 files; Live/Audit listed as never created) |
| Stale empty claim | `A62_react_scaffold.md` §0 / §3 (`pages/` EMPTY) |

No `tsc`, no `npm`, no product edit.

---

## 2. `list_dir` result (authoritative)

`D:\Prop\apps\web\src\pages\` contains **exactly** these 15 files (alphabetical, as returned by `list_dir`):

```text
AuditPage.tsx
BrokersPage.tsx
FixSessionsPage.tsx
GroupsPage.tsx
LiveCopyPage.tsx
OverviewPage.tsx
ReconciliationPage.tsx
RiskPage.tsx
ScoringPage.tsx
SettingsPage.tsx
ShadowPortfolioPage.tsx
SystemHealthPage.tsx
TradeExplorerPage.tsx
TraderDetailPage.tsx
TradersPage.tsx
```

Empty-directory check: **FAIL** (directory has content). File count: **15**. All files are `.tsx`. No `.ts`, no index barrel, no subfolders.

There is **no** `pages/` under `D:\Prop\src`. The assigned path `apps/web/src/pages` lives at `D:\Prop\apps\web\src\pages`.

No `LoginPage.tsx` and no `ModelsPage.tsx` anywhere under `D:\Prop\apps\web\src`.

---

## 3. `App.tsx` imports vs disk (1:1)

`App.tsx` lines 3–17 import; lines 21–39 route. All 15 imported symbols are used exactly once as a `<Route element={…} />`. No unused page import.

| # | Import (relative) | Binding | Route | File on disk | Bytes | Phys. lines | SHA-256 | Default export |
|---|---|---|---|---|---:|---:|---|---|
| 1 | `./pages/OverviewPage` | `OverviewPage` | `/` → `/overview`, `/overview` | `OverviewPage.tsx` | 2078 | 35 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | `OverviewPage` |
| 2 | `./pages/BrokersPage` | `BrokersPage` | `/brokers` | `BrokersPage.tsx` | 1266 | 37 | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` | `BrokersPage` |
| 3 | `./pages/GroupsPage` | `GroupsPage` | `/groups` | `GroupsPage.tsx` | 1228 | 34 | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` | `GroupsPage` |
| 4 | `./pages/TradersPage` | `TradersPage` | `/traders` | `TradersPage.tsx` | 1604 | 42 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` | `TradersPage` |
| 5 | `./pages/TraderDetailPage` | `TraderDetailPage` | `/traders/:brokerId/:login` | `TraderDetailPage.tsx` | 1592 | 34 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` | `TraderDetailPage` |
| 6 | `./pages/TradeExplorerPage` | `TradeExplorerPage` | `/trades` | `TradeExplorerPage.tsx` | 1321 | 39 | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` | `TradeExplorerPage` |
| 7 | `./pages/ScoringPage` | `ScoringPage` | `/scoring` | `ScoringPage.tsx` | 1288 | 33 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | `ScoringPage` |
| 8 | `./pages/ShadowPortfolioPage` | `ShadowPortfolioPage` | `/shadow` | `ShadowPortfolioPage.tsx` | 628 | 14 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | `ShadowPortfolioPage` |
| 9 | `./pages/LiveCopyPage` | `LiveCopyPage` | `/live` | `LiveCopyPage.tsx` | 321 | 8 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | `LiveCopyPage` |
| 10 | `./pages/FixSessionsPage` | `FixSessionsPage` | `/fix` | `FixSessionsPage.tsx` | 1312 | 26 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | `FixSessionsPage` |
| 11 | `./pages/RiskPage` | `RiskPage` | `/risk` | `RiskPage.tsx` | 1148 | 25 | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | `RiskPage` |
| 12 | `./pages/ReconciliationPage` | `ReconciliationPage` | `/reconciliation` | `ReconciliationPage.tsx` | 490 | 12 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` | `ReconciliationPage` |
| 13 | `./pages/SystemHealthPage` | `SystemHealthPage` | `/health` | `SystemHealthPage.tsx` | 369 | 11 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` | `SystemHealthPage` |
| 14 | `./pages/AuditPage` | `AuditPage` | `/audit` | `AuditPage.tsx` | 324 | 8 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` | `AuditPage` |
| 15 | `./pages/SettingsPage` | `SettingsPage` | `/settings` | `SettingsPage.tsx` | 459 | 12 | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | `SettingsPage` |

`App.tsx` also imports `DashboardLayout` from `./layouts/DashboardLayout` (present) and `{ Routes, Route, Navigate }` from `react-router-dom`. `main.tsx` wraps `<App />` in `<BrowserRouter>`.

No catch-all route. A26 says unknown routes → `/overview`. Current router: unmatched paths render the layout with an empty `<Outlet />`.

---

## 4. Sidebar vs router

`DashboardLayout` nav (lines 5–20) has **14** links. `TraderDetailPage` is intentionally not a sidebar item; `TradersPage` links to `/traders/${t.broker}/${t.login}`.

| Nav `to` | Label | `App.tsx` path | Match |
|---|---|---|---|
| `/overview` | Overview | `overview` | yes |
| `/brokers` | Brokers | `brokers` | yes |
| `/groups` | Groups | `groups` | yes (both ≠ A26 `/mt5-groups`) |
| `/traders` | Traders | `traders` | yes |
| `/trades` | Trades | `trades` | yes |
| `/scoring` | Scoring | `scoring` | yes |
| `/shadow` | Shadow | `shadow` | yes |
| `/live` | Live | `live` | yes |
| `/fix` | FIX | `fix` | yes |
| `/risk` | Risk | `risk` | yes |
| `/reconciliation` | Recon | `reconciliation` | yes |
| `/health` | Health | `health` | yes |
| `/audit` | Audit | `audit` | yes |
| `/settings` | Settings | `settings` | yes |

No sidebar link points at a missing module. Nav labels are **abbreviated** vs architecture §46 exact strings (`Groups` vs `MT5 Groups`, `Live` vs `Live Copy Portfolio`, `FIX` vs `cTrader FIX`, `Recon` vs `Reconciliation`, `Health` vs `System Health`). A26 §5.3 requires labels **exactly** as §46.

---

## 5. Spec pages (A26 §5.2 / architecture §46)

| §46 / A26 item | Required route (A26) | In `App.tsx`? | File under `pages/`? | Status |
|---|---|---|---|---|
| Login | `/login` | no | no | **MISSING** (never created) |
| Overview | `/overview` | yes | `OverviewPage.tsx` | present; imported |
| Brokers | `/brokers` | yes | `BrokersPage.tsx` | present; imported |
| MT5 Groups | `/mt5-groups` | **wrong path** `/groups` | `GroupsPage.tsx` | file present; path ≠ A26 |
| Traders | `/traders` | yes | `TradersPage.tsx` | present; imported |
| Trader Detail | `/traders/:brokerId/:login` | yes | `TraderDetailPage.tsx` | present; imported |
| Trade Explorer | `/trades` | yes | `TradeExplorerPage.tsx` | present; imported |
| Scoring | `/scoring` | yes | `ScoringPage.tsx` | present; imported |
| Models | `/models` | no | no | **MISSING** |
| Shadow Portfolio | `/shadow` | yes | `ShadowPortfolioPage.tsx` | present; imported (static, no hook) |
| Live Copy Portfolio | `/live` | yes | `LiveCopyPage.tsx` | present; imported (static stub) |
| cTrader FIX | `/fix` | yes | `FixSessionsPage.tsx` | present; imported |
| Risk | `/risk` | yes | `RiskPage.tsx` | present; imported |
| Reconciliation | `/reconciliation` | yes | `ReconciliationPage.tsx` | present; imported |
| System Health | `/health` | yes | `SystemHealthPage.tsx` | present; imported |
| Audit | `/audit` | yes | `AuditPage.tsx` | present; imported (static stub) |
| Settings | `/settings` | yes | `SettingsPage.tsx` | present; imported |

**Missing modules (would be new files, not replacements):**

1. `LoginPage` — `/login`
2. `ModelsPage` — `/models`

This report does **not** authorize creating them.

B22 listed Live Copy and Audit as missing. **That remainder is closed on disk.** Both files were written 2026-08-18T13:20:38+05:30 (same timestamp as the `App.tsx` / layout update that wired them).

---

## 6. Present ≠ finished (do not confuse with missing files)

The 15 files exist and export the names `App.tsx` imports. Several are thin. That is **implementation depth**, out of the “do imports match?” question:

| File | Hook / data | Honest shape (read this pass) |
|---|---|---|
| `OverviewPage.tsx` | `useOverview` | Metric grid + real-copy banner |
| `BrokersPage.tsx` | `useBrokers` | Table; extra `**` after already-masked login |
| `GroupsPage.tsx` | `useGroups` | Table; title “MT5 Groups”; route is `/groups` |
| `TradersPage.tsx` | `useTraders` | Leaderboard table; detail link uses `t.broker` (code), not A26 UUID `brokerId` |
| `TraderDetailPage.tsx` | `useTraderDetail` | Params + metric tiles |
| `TradeExplorerPage.tsx` | `useTrades` | Reconstructed-trade table |
| `ScoringPage.tsx` | `useTraders` (reuse) | Score columns only; no dedicated scoring API |
| `ShadowPortfolioPage.tsx` | none | Static copy only |
| `LiveCopyPage.tsx` | none | 8-line stub: real copy off, page empty until gates |
| `FixSessionsPage.tsx` | `useFixSessions` | Session cards |
| `RiskPage.tsx` | `useRiskStatus` | Kill switch / P&L tiles + reject list |
| `ReconciliationPage.tsx` | `useReconciliation` | Title + `JSON.stringify` dump |
| `SystemHealthPage.tsx` | `useHealth` | Title + `JSON.stringify` dump |
| `AuditPage.tsx` | none | 8-line stub; no audit hook exists in `hooks.ts` |
| `SettingsPage.tsx` | `useSettings` | Title + `JSON.stringify` dump |

`hooks.ts` has no `useLive` / `useAudit`. Live and Audit cannot be “API-backed” without new hooks (out of scope here).

Hooks still call **non-A26** `/api/...` paths, not `/api/v1/...`. Widget completeness vs architecture §§47–54 is **not** claimed.

---

## 7. What earlier reports claimed vs this pass

| Prior claim | This pass |
|---|---|
| A62: 13 imports, 0 page files / `pages/` EMPTY | **FALSE.** 15 files |
| B10 / B22: 13 page modules | **STALE.** 15 now |
| B22: Live Copy + Audit never created | **FALSE now.** `LiveCopyPage.tsx` + `AuditPage.tsx` imported and routed |
| B22: App.tsx SHA `D83CE347…` (1854 bytes) | **Superseded.** App.tsx is 2062 bytes, SHA `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| B22: 12 nav links | **STALE.** 14 nav links (Live + Audit added) |
| Login / Models missing | **Still true** |
| `/groups` ≠ `/mt5-groups` | **Still true** |

Do not use B22’s 13-row table as the current import proof.

Note: A29’s “C08” row is **XGBoost / train split**, a different workstream. This file is the C-wave **web pages review** named by the orchestrator (`C08_web_pages_review.md`).

---

## 8. Direct answers

### What is in `apps/web/src/pages`?

Fifteen `.tsx` files at `D:\Prop\apps\web\src\pages` (list in §2). Not under `D:\Prop\src`.

### Do `App.tsx` imports match?

**Yes.** 15 default imports, 15 files, 15 matching `export default function` names, 15 routes that consume those bindings. Zero dangling. Zero orphans.

### Are there extra page files `App.tsx` does not import?

**No.**

### What page *files* are still missing vs the dashboard contract?

Only Login (`/login`) and Models (`/models`). Groups exists but is not on the A26 path.

### Did this pass change product source?

**No.**

---

## 9. What a later wave must not do

1. Do **not** recreate the 15 existing page files to “fix” A62 or B22.
2. Do **not** invent a second `pages/` tree under `D:\Prop\src`.
3. Do **not** treat `/groups` vs `/mt5-groups` as a missing file (it is a path mismatch).
4. Do **not** treat Live / Audit as missing files anymore; they are stubs.
5. Do **not** create Login / Models in this report’s wave; they are listed only as a spec remainder.
6. Do **not** overwrite existing `.tsx` without a versioned coding task.

---

## Evidence pins

- Router: `D:\Prop\apps\web\src\App.tsx` lines 3–17 (imports), 21–39 (routes). SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Pages dir: `D:\Prop\apps\web\src\pages\` — `list_dir` 15 files, measured 2026-08-18T13:23:27+05:30.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 5–20 (14 `to` paths). SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Stale empty claim: `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md`.
- Stale 13-file census: `D:\Prop\reports\swarm\20260818\B22_web_missing_pages.md`.
- Binding routes: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2.
- Binding labels: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1739–1758.
- Only product `pages/` directory: `D:\Prop\apps\web\src\pages`.
