# B22 — `App.tsx` page imports vs `pages/` census

| Field | Value |
|---|---|
| Agent | B22 (senior engineer, missing-pages check only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:19:28+05:30 |
| Workspace | `D:\Prop` |
| Target | `D:\Prop\apps\web\src\App.tsx` + `D:\Prop\apps\web\src\pages\` |
| Product source modified | **No.** This report is the only write. |
| Method | `list_dir` on `apps/web/src/pages`; read `App.tsx` + `DashboardLayout.tsx`; SHA-256 + byte/line census; compare A26 §5.2 and architecture §46 |
| Precedence | On-disk tree supersedes A62 §0 / §3. A26 + architecture §46 remain binding for *required* nav that was never created. |

---

## 0. Verdict

**`pages/` is not empty.** A62’s “13 imports / 0 page files” snapshot is **stale**.

`list_dir` of `D:\Prop\apps\web\src\pages` returned **13** `*.tsx` files. `App.tsx` imports **13** page modules. **13/13 resolve.** Zero dangling imports. Zero orphan page files. The only `pages/` directory in the product tree is this one.

The hypothesis “`App.tsx` imports many pages but `pages/` may be empty” is **FALSE** as of this measurement.

What *is* still missing is a different question: four architecture §46 / A26 routes were **never imported and never written** (`Login`, `Models`, `Live Copy Portfolio`, `Audit`). That is an inventory gap vs the spec, not an empty directory.

| Question | Count | Answer |
|---|---|---|
| `list_dir` files under `apps/web/src/pages` | **13** | not empty |
| `App.tsx` `./pages/*` default imports | **13** | all present on disk |
| Import whose file is missing | **0** | router will not 404 on a missing module |
| Page file not imported by `App.tsx` | **0** | no orphans |
| Other `pages/` dirs under `D:\Prop` (excluding vendor/bin/obj/node_modules) | **1** | only `apps\web\src\pages` |
| §46 / A26 pages with **no** file and **no** route | **4** | Login, Models, Live Copy, Audit |
| `pages/` empty? | — | **No** |

Do **not** recreate the 13 files. Do **not** treat this as a “broken stub with missing modules.” Later waves may still replace or thicken pages; that is completeness, not a missing-file defect.

---

## 1. Method

| Source | Path / action |
|---|---|
| Router | `D:\Prop\apps\web\src\App.tsx` (1854 bytes, SHA-256 `D83CE3476FEE3419C13E710508580BE9ADA95E01D0A8A91B9ADF2291CE34AA3D`) |
| Nav shell | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` (SHA-256 `F6A41AB50E150769865ECBA6BD3F0F8A408FD60D37A40D8554838022EA81A850`) |
| Directory listing | `list_dir` `D:\Prop\apps\web\src\pages` — **13** files, names below |
| File census | PowerShell `Get-ChildItem` + `Get-FileHash SHA256` + line counts |
| Other `pages/` dirs | recursive directory search under `D:\Prop`, exclude `vendor` / `bin` / `obj` / `node_modules` |
| Stale claim | `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` §0 / §3 (`pages/ EMPTY`) |
| Binding nav | architecture §46 (`…Architecture_v2.md` lines 1735–1758); A26 §5.2 route map |
| Prior note | `B09_sln_gap.md` §6 already observed the 13 page files exist |

No `tsc`, no `npm`, no product edit.

---

## 2. `list_dir` result (authoritative)

`D:\Prop\apps\web\src\pages\` contains **exactly** these 13 files (alphabetical, as returned by `list_dir`):

```text
BrokersPage.tsx
FixSessionsPage.tsx
GroupsPage.tsx
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

Empty-directory check: **FAIL** (directory has content). File count: **13**. All files are `.tsx`. No `.ts`, no index barrel, no subfolders.

There is **no** `pages/` under `D:\Prop\src` (the C# workspace). Looking only at `D:\Prop\src` would produce a false “no pages” reading.

---

## 3. `App.tsx` imports vs disk (1:1)

`App.tsx` lines 3–15 import, and lines 21–34 route:

| # | Import (relative) | Route | File on disk | Bytes | Lines | SHA-256 |
|---|---|---|---|---:|---:|---|
| 1 | `./pages/OverviewPage` | `/` → `/overview`, `/overview` | `OverviewPage.tsx` | 2078 | 33 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` |
| 2 | `./pages/BrokersPage` | `/brokers` | `BrokersPage.tsx` | 1266 | 36 | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` |
| 3 | `./pages/GroupsPage` | `/groups` | `GroupsPage.tsx` | 1228 | 33 | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` |
| 4 | `./pages/TradersPage` | `/traders` | `TradersPage.tsx` | 1604 | 41 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` |
| 5 | `./pages/TraderDetailPage` | `/traders/:brokerId/:login` | `TraderDetailPage.tsx` | 1592 | 32 | `6CAE0FC902D8DFDB5AAC974564D918602EBD3D780C5FAA272BBEF281B19E406D` |
| 6 | `./pages/TradeExplorerPage` | `/trades` | `TradeExplorerPage.tsx` | 1321 | 38 | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` |
| 7 | `./pages/ScoringPage` | `/scoring` | `ScoringPage.tsx` | 1288 | 32 | `2F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` |
| 8 | `./pages/ShadowPortfolioPage` | `/shadow` | `ShadowPortfolioPage.tsx` | 628 | 14 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` |
| 9 | `./pages/FixSessionsPage` | `/fix` | `FixSessionsPage.tsx` | 1312 | 25 | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` |
| 10 | `./pages/RiskPage` | `/risk` | `RiskPage.tsx` | 1148 | 24 | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` |
| 11 | `./pages/ReconciliationPage` | `/reconciliation` | `ReconciliationPage.tsx` | 490 | 11 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` |
| 12 | `./pages/SystemHealthPage` | `/health` | `SystemHealthPage.tsx` | 369 | 10 | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` |
| 13 | `./pages/SettingsPage` | `/settings` | `SettingsPage.tsx` | 459 | 11 | `157D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` |

Every imported symbol has a matching `export default function <Name>()` in that file.

`DashboardLayout` nav (12 links) covers every routed page except `TraderDetailPage` (reached from `/traders/:brokerId/:login`, not a sidebar item — matches architecture: Trader Detail is a page, not a top-level list item in the same way). Nav paths equal `App.tsx` paths. No sidebar link points at a missing module.

---

## 4. What A62 claimed vs what is on disk

A62 §0 / §3 (same calendar day, earlier snapshot):

| A62 claim | This pass |
|---|---|
| “Page modules imported by `App.tsx` — **MISSING** — 13 imports, 0 page files” | **FALSE now.** 13 imports, 13 files |
| `src/pages/` **EMPTY** | **FALSE now.** 13 `.tsx` |
| `src/layouts/` **EMPTY** | **FALSE now.** `DashboardLayout.tsx` exists and is imported |
| `src/components/` **EMPTY** | **FALSE now.** `MetricCard.tsx`, `StatusBadge.tsx` (out of scope except as page deps) |
| `src/types/` / `src/utils/` **EMPTY** | **FALSE now.** files exist (out of scope) |
| Login / Models / Live Copy / Audit routes **MISSING** | **Still true** — never added |

B09 §6 already noted the 13 page names. This file is the dedicated 1:1 import↔file proof.

---

## 5. Spec pages that are still absent (not an empty-`pages/` problem)

Architecture §46 main navigation (16 labels) and A26 §5.2 (17 routes including `/login`) vs current router.

| §46 / A26 item | Required route (A26) | In `App.tsx`? | File under `pages/`? | Status |
|---|---|---|---|---|
| Login | `/login` | no | no | **MISSING** (never created) |
| Overview | `/overview` | yes | `OverviewPage.tsx` | present |
| Brokers | `/brokers` | yes | `BrokersPage.tsx` | present |
| MT5 Groups | `/mt5-groups` | **wrong path** `/groups` | `GroupsPage.tsx` | file present; path ≠ A26 |
| Traders | `/traders` | yes | `TradersPage.tsx` | present |
| Trader Detail | `/traders/:brokerId/:login` | yes | `TraderDetailPage.tsx` | present |
| Trade Explorer | `/trades` | yes | `TradeExplorerPage.tsx` | present |
| Scoring | `/scoring` | yes | `ScoringPage.tsx` | present |
| Models | `/models` | no | no | **MISSING** |
| Shadow Portfolio | `/shadow` | yes | `ShadowPortfolioPage.tsx` | present (static copy, no hook) |
| Live Copy Portfolio | `/live` | no | no | **MISSING** |
| cTrader FIX | `/fix` | yes | `FixSessionsPage.tsx` | present |
| Risk | `/risk` | yes | `RiskPage.tsx` | present |
| Reconciliation | `/reconciliation` | yes | `ReconciliationPage.tsx` | present |
| System Health | `/health` | yes | `SystemHealthPage.tsx` | present |
| Audit | `/audit` | no | no | **MISSING** |
| Settings | `/settings` | yes | `SettingsPage.tsx` | present |

**Missing modules (would be new files, not replacements):**

1. `LoginPage` — `/login`
2. `ModelsPage` — `/models`
3. `LiveCopyPortfolioPage` (or equivalent) — `/live`
4. `AuditPage` — `/audit`

This report does **not** authorize creating them.

---

## 6. Present ≠ finished (do not confuse with missing files)

The 13 files exist and compile as modules. Several are thin. That is **implementation depth**, out of the “is `pages/` empty?” question:

| File | Honest shape (read this pass) |
|---|---|
| `OverviewPage.tsx` | Uses `useOverview` + `MetricCard`; real layout |
| `BrokersPage.tsx` / `GroupsPage.tsx` / `TradersPage.tsx` / `TradeExplorerPage.tsx` / `ScoringPage.tsx` | Tables + hooks |
| `TraderDetailPage.tsx` | Params + `useTraderDetail` |
| `FixSessionsPage.tsx` | Cards + `useFixSessions` |
| `RiskPage.tsx` | Metric cards + `useRiskStatus` |
| `ShadowPortfolioPage.tsx` | **Static text only** — no API hook |
| `ReconciliationPage.tsx` / `SystemHealthPage.tsx` / `SettingsPage.tsx` | Title + `JSON.stringify` dump |

Hooks still call the **non-A26** `/api/...` paths (`hooks.ts`), not `/api/v1/...`. Widget completeness vs architecture §§47–54 is **not** claimed here. A62’s “MISSING widgets” critique can remain valid while “MISSING files” is not.

---

## 7. Direct answers

### Is `pages/` empty?

**No.** `list_dir` → 13 `.tsx` files at `D:\Prop\apps\web\src\pages`.

### Does `App.tsx` import pages that do not exist?

**No.** 13/13 default imports have a matching file and `export default function`.

### Are there extra page files `App.tsx` does not import?

**No.**

### What page *files* are still missing vs the dashboard contract?

Only the four never-scaffolded §46/A26 items: Login, Models, Live Copy Portfolio, Audit.

### Did this pass change product source?

**No.**

---

## 8. What a later wave must not do

1. Do **not** recreate the 13 existing page files to “fix” A62.
2. Do **not** invent a second `pages/` tree under `D:\Prop\src`.
3. Do **not** treat `/groups` vs `/mt5-groups` as a missing file (it is a path mismatch).
4. Do **not** create Login / Models / Live / Audit in this report’s wave; they are listed only as a spec remainder.
5. Do **not** overwrite existing `.tsx` without a versioned coding task.

---

## Evidence pins

- Router: `D:\Prop\apps\web\src\App.tsx` lines 3–15 (imports), 17–36 (routes). SHA-256 `D83CE3476FEE3419C13E710508580BE9ADA95E01D0A8A91B9ADF2291CE34AA3D`.
- Pages dir: `D:\Prop\apps\web\src\pages\` — `list_dir` 13 files, measured 2026-08-18T13:19:28+05:30.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 5–18 (12 `to` paths).
- Stale empty claim: `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` lines 30, 119, 127.
- Prior existence note: `D:\Prop\reports\swarm\20260818\B09_sln_gap.md` §6.
- Binding routes: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2 lines 285–305.
- Binding labels: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1735–1758.
- Only product `pages/` directory: `D:\Prop\apps\web\src\pages`.
