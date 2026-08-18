# E025 — `DashboardLayout` nav vs `pages/`

| Field | Value |
|---|---|
| Agent | E025 (senior engineer, sidebar × page inventory only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:14+05:30 |
| Artifact | `D:\Prop\reports\swarm\20260818\E025_nav.md` |
| Assigned | List `DashboardLayout` nav vs pages. Write this file. **Do not modify product source.** |
| Workspace | Vite app is `D:\Prop\apps\web` (not under `D:\Prop\src`) |
| Product source modified | **No.** This report plus catalog notes in `INDEX.md` / `SWARM_LOG.md` are the only writes. |
| Method | Full `read_file` of `DashboardLayout.tsx` (44 lines) and `App.tsx` (42 lines); `list_dir` of `src/pages`; first-line `export default` + `<h1>` grep on all 15 pages; `Link`/`NavLink`/`useParams`/`Navigate` grep under `apps/web/src`; `Test-Path` for `LoginPage` / `ModelsPage` / `src/auth` / `src/routes`; PowerShell SHA-256 + bytes + physical lines + newline kind + last-write; `git show HEAD` of both chrome files; `git ls-tree HEAD -- apps/web/src/pages` (empty); compare architecture §46 and A26 §5.2 / §5.3 / A62 `NAV_ITEMS`. No `tsc`, no `npm`, no HTTP, no product edit. |
| Binding law | Architecture v2 §46 (lines 1739–1758); A26 §5.2 route map + §5.3 exact labels; A62 suggested `NAV_ITEMS` (Trader Detail is not a sidebar leaf); C39 Models hold |
| Prior (do not collapse) | D38 (full route table), D08 / C08 (page census), C53 (Live/Audit chrome), B31 (nav gaps), E003 (route × API). **Chrome SHAs unchanged** vs those files. |
| HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **sidebar ↔ page-file join**. It is **not** a claim that §§47–54 widgets are painted, that `/api/v1` exists, that RBAC is on, or that the dashboard is first-useful.

---

## 0. Verdict

**Worktree sidebar is a 14-row `nav` array. Every `to` has a matching `App.tsx` child route and a default-exported page file. There are 15 page modules: 14 are sidebar leaves, 1 (`TraderDetailPage`) is reached only from the leaderboard. Zero dead nav links. Zero orphan page files. Spec still missing `LoginPage` and `ModelsPage`.**

A clean checkout of HEAD is **not** this table: HEAD `DashboardLayout` has **12** links (no Live, no Audit), HEAD `App.tsx` imports 13 pages, and `git ls-tree HEAD -- apps/web/src/pages` is **empty**. The entire `pages/` directory is untracked (`??`). Quote the worktree join only while those 15 files remain on disk.

| Question | Count | Answer |
|---|---:|---|
| Sidebar `nav` rows | **14** | `DashboardLayout.tsx` lines 5–20 |
| `App.tsx` destinations | **16** | 1 index redirect + 15 page routes |
| Files under `pages/` | **15** | 15/15 imported; 0 orphans |
| Nav `to` with no route | **0** | no sidebar 404 against this table |
| Nav `to` with no page file | **0** | every import resolves |
| Page file with no `App.tsx` import | **0** | no orphans |
| Page file with no sidebar row | **1** | `TraderDetailPage` — A62: not a sidebar leaf |
| Exact §46 labels on sidebar | **7 / 16** | 7 exact; 7 abbreviated; 2 absent (Models, Trader Detail) |
| A62 sidebar leaves present | **14 / 15** | Models is the missing leaf |
| A26 §5.2 routes at the **specified path** | **14 / 17** | `/login` absent; `/models` absent; `/groups` ≠ `/mt5-groups` |
| Header strip (A26 §5.3) | **0** | layout is `aside` + `<Outlet />` only |
| Auth / role filter on nav | **0** | Audit is shown to everyone |
| Product source edited this pass | **0** | report only |

§73.B for the **nav × pages join**: **EXISTS_NEEDS_REFACTOR** (wired, labels/paths/spec leaves incomplete).  
§73.B for `/login`: **MISSING**.  
§73.B for `/models`: **MISSING_BY_DESIGN** while Phase 6 is closed (C39) — do not add a working Models surface to “complete” the nav.  
§73.B for `/groups` vs A26 `/mt5-groups`: **EXISTS_NEEDS_REFACTOR**.

Honest one-liner: **14/14 nav items hit a real page. 15/15 pages are routed. Nav is not §46-complete and HEAD does not have this nav.**

---

## 1. Measured files (this pass)

| Path | Bytes | Phys. lines | NL | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|---|
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | CRLF | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38.3009629+05:30 | **unstaged** (`M`); worktree blob `8ece6a4583f088314a249546f03d6a64b7161527`; HEAD blob `1b40c4f6f6a7a2d74d43713b242e10fe7de5d543` |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | CRLF | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38.3114705+05:30 | **unstaged** (`M`); worktree blob `e297d559d709f5917dbaa91e06838d992949e966`; HEAD blob `6555ae04bed9ee7061d9e4acd09c9606ac0bd4d4` |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | CRLF | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39.5424698+05:30 | clean vs HEAD |

`git diff --stat` vs HEAD: `App.tsx` **+4**, `DashboardLayout.tsx` **+2** (Live + Audit import/route/nav). Chrome SHAs match D08 / D38 / C53 / E003 (remeasured 13:50:14).

`Test-Path` **False**: `D:\Prop\apps\web\src\pages\LoginPage.tsx`, `ModelsPage.tsx`, `D:\Prop\apps\web\src\auth`, `D:\Prop\apps\web\src\routes`, `D:\Prop\src\apps\web\src\pages`. No second layout. No `paths.ts` / `NAV_ITEMS` module (A62). Labels live only in the local `const nav = […]`.

---

## 2. Sidebar as written (`DashboardLayout.tsx` lines 5–20)

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

Each row is a `NavLink`. `isActive` → `bg-blue-600/20 text-blue-300`. React Router v6 `end` defaults to `false`, so `/traders/:brokerId/:login` **does** highlight **Traders** (A62 prefix rule, accidental not coded). Brand text is `MT5 Intelligence` (not `index.html` / A26 title `MT5 Trader Intelligence`). `useEffect(() => { startConnection(); }, [])` is a layout side-effect, not a nav row.

There is no header strip. There is no role filter. Audit is the 13th of 14 links for every visitor.

---

## 3. Pages on disk (`D:\Prop\apps\web\src\pages`)

15 `*.tsx` files. All **untracked** (`?? apps/web/src/pages/`). HEAD has **zero** files under that folder.

| # | File | Default export | Bytes | Phys. | NL | SHA-256 | Last write |
|---:|---|---|---:|---:|---|---|---|
| 1 | `OverviewPage.tsx` | `OverviewPage` | 2078 | 35 | LF | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | 13:16:00 |
| 2 | `BrokersPage.tsx` | `BrokersPage` | 1266 | 37 | LF | `274754E3DD14D4D89C62F14A8E8A69204C8DBDD7AF479CD367122E68CCB9C460` | 13:16:00 |
| 3 | `GroupsPage.tsx` | `GroupsPage` | 1228 | 34 | LF | `4F7874826403712D8AB6A0C5C85E9FD95D7C18F5E02B600E0C5384F387C91E65` | 13:16:00 |
| 4 | `TradersPage.tsx` | `TradersPage` | 1604 | 42 | LF | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` | 13:16:00 |
| 5 | `TraderDetailPage.tsx` | `TraderDetailPage` | 2402 | 56 | LF | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` | **13:35:59** |
| 6 | `TradeExplorerPage.tsx` | `TradeExplorerPage` | 1321 | 39 | LF | `7EE11EB97DBBE0ED077E72A5E15D0DA8EC9E53F8D60CBD76B48985449016AE31` | 13:16:43 |
| 7 | `ScoringPage.tsx` | `ScoringPage` | 1288 | 33 | LF | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 13:16:43 |
| 8 | `ShadowPortfolioPage.tsx` | `ShadowPortfolioPage` | 628 | 14 | LF | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` | 13:16:43 |
| 9 | `LiveCopyPage.tsx` | `LiveCopyPage` | 321 | 8 | LF | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 13:20:38 |
| 10 | `FixSessionsPage.tsx` | `FixSessionsPage` | 1312 | 26 | LF | `EC93326688719E10D3ED5CB275D9BF1E7113C7F61EEA99803F42E1EA268BB886` | 13:16:43 |
| 11 | `RiskPage.tsx` | `RiskPage` | 1148 | 25 | LF | `FC4C5F05E1FF998FC1172E7F6C181821944066A40B577678B6DD9D0A24C1D8CF` | 13:16:43 |
| 12 | `ReconciliationPage.tsx` | `ReconciliationPage` | 490 | 12 | LF | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` | 13:16:43 |
| 13 | `SystemHealthPage.tsx` | `SystemHealthPage` | 369 | 11 | LF | `03BDBC76CBEFEE4ADC125B491E9B46BC62E35E052AD99BE874E8BCF0E25EDD2A` | 13:16:43 |
| 14 | `AuditPage.tsx` | `AuditPage` | 324 | 8 | LF | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` | 13:20:38 |
| 15 | `SettingsPage.tsx` | `SettingsPage` | 459 | 12 | LF | `57D41B908C591238ACD375E62EA870E0B373B168F53D036137A045AC91CE03F4` | 13:16:43 |

14/15 page SHAs match D08. `TraderDetailPage` is the later blob (D38 already recorded 2402 B / `C849449B…`). This report does not re-audit widgets.

`App.tsx` imports (lines 3–17) = these 15 symbols, each used once as `<Route element={…} />`. Module resolution will not fail on the worktree.

---

## 4. Join matrix — nav `to` × route × page × labels

Order = sidebar order (not alphabetical).

| # | Nav `to` | Nav label | Icon | `App.tsx` `path` | Page file | Page `<h1>` | §46 exact label | Path = A26? | In HEAD nav? |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | `/overview` | Overview | ◈ | `overview` | `OverviewPage.tsx` | Overview | **yes** | yes | yes |
| 2 | `/brokers` | Brokers | ⛁ | `brokers` | `BrokersPage.tsx` | Brokers | **yes** | yes | yes |
| 3 | `/groups` | Groups | ⊞ | `groups` | `GroupsPage.tsx` | **MT5 Groups** | **no** (`MT5 Groups`) | **no** (A26 `/mt5-groups`) | yes (same wrong) |
| 4 | `/traders` | Traders | ⚑ | `traders` | `TradersPage.tsx` | Trader leaderboard | **yes** (list name) | yes | yes |
| 5 | `/trades` | Trades | ⇄ | `trades` | `TradeExplorerPage.tsx` | Trade explorer | **no** (`Trade Explorer`) | yes | yes |
| 6 | `/scoring` | Scoring | ★ | `scoring` | `ScoringPage.tsx` | Deterministic scoring | **yes** | yes | yes |
| 7 | `/shadow` | Shadow | ◐ | `shadow` | `ShadowPortfolioPage.tsx` | Shadow portfolio | **no** (`Shadow Portfolio`) | yes | yes |
| 8 | `/live` | Live | ▶ | `live` | `LiveCopyPage.tsx` | Live copy portfolio | **no** (`Live Copy Portfolio`) | yes | **no** |
| 9 | `/fix` | FIX | ⚡ | `fix` | `FixSessionsPage.tsx` | **cTrader FIX** | **no** (`cTrader FIX`) | yes | yes |
| 10 | `/risk` | Risk | ⚠ | `risk` | `RiskPage.tsx` | Risk | **yes** | yes | yes |
| 11 | `/reconciliation` | Recon | ⟳ | `reconciliation` | `ReconciliationPage.tsx` | **Reconciliation** | **no** (`Reconciliation`) | yes | yes |
| 12 | `/health` | Health | ♥ | `health` | `SystemHealthPage.tsx` | System health | **no** (`System Health`) | yes | yes |
| 13 | `/audit` | Audit | ☰ | `audit` | `AuditPage.tsx` | Audit | **yes** | yes | **no** |
| 14 | `/settings` | Settings | ⚙ | `settings` | `SettingsPage.tsx` | Settings | **yes** | yes | yes |

**14/14 nav rows join a page.** No sidebar URL is a dangling `to`.

Label score on the 14 rendered links (each row counted once):

| Bucket | N | Items |
|---|---:|---|
| Nav label == §46 | **7** | Overview, Brokers, Traders, Scoring, Risk, Audit, Settings |
| Nav abbreviated; page `<h1>` == §46 | **3** | Groups → `MT5 Groups`; FIX → `cTrader FIX`; Recon → `Reconciliation` |
| Nav abbreviated; page `<h1>` sentence-case §46 | **4** | Trades → Trade explorer; Shadow → Shadow portfolio; Live → Live copy portfolio; Health → System health |

Extra h1 drift on an otherwise exact nav row: Scoring page heading is `Deterministic scoring`, not `Scoring`. Traders page heading is `Trader leaderboard` (A26 list name), not `Traders`.

A26 §5.3: left-nav labels **exactly** as §46. Current shell **FAIL**s that rule on 7 of 14 visible items.

---

## 5. Pages / spec items **not** in the sidebar

| Item | File / route | Why not in `nav` | Class |
|---|---|---|---|
| `/` | index `<Navigate to="/overview" replace />` | redirect, not a page | allowed |
| Trader Detail | `TraderDetailPage.tsx` → `traders/:brokerId/:login` | A62: not a sidebar leaf; opened from `TradersPage` `<Link to={/traders/${t.broker}/${t.login}}>` | **EXISTS_NEEDS_REFACTOR** (param is broker **code**, A26 names UUID `brokerId`) |
| Models | **no file**, no route, no nav | C39 / Phase 6 closed | **MISSING_BY_DESIGN** |
| Login | **no file**, no route | A26 §5.2; must sit **outside** this layout | **MISSING** |
| Catch-all `*` | none | A26: unknown → `/overview`. Current: empty `<Outlet />` under chrome | **MISSING** |

In-app links besides the 14 `NavLink`s (full `apps/web/src` grep):

| Site | API | Target |
|---|---|---|
| `App.tsx` L23 | `<Navigate to="/overview" replace />` | `/` → `/overview` |
| `DashboardLayout.tsx` L31 | `NavLink to={n.to}` | 14 sidebar URLs |
| `TradersPage.tsx` L28 | `<Link to={`/traders/${t.broker}/${t.login}`}>` | only producer of the detail URL |
| `TraderDetailPage.tsx` L5 | `useParams()` → `{ brokerId, login }` | no back-link to `/traders` |

No `useNavigate`. No `LoginPage`. No `ModelsPage`. No other `Link`.

---

## 6. Architecture §46 / A26 / A62 vs this nav

Architecture §46 main navigation (verbatim order, `Architecture_v2.md` lines 1741–1758):

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

A62 `NAV_ITEMS` (15 sidebar leaves): same list **minus** Trader Detail (detail route). A26 §5.2 adds `/login` (outside the shell) and names Groups `/mt5-groups`.

| §46 / A26 item | Required route | In nav? | Nav label | Page file | In `App.tsx`? |
|---|---|---|---|---|---|
| Login | `/login` | n/a (outside shell) | — | **no** | **no** |
| Overview | `/overview` | yes | Overview | yes | yes |
| Brokers | `/brokers` | yes | Brokers | yes | yes |
| MT5 Groups | `/mt5-groups` | yes (wrong `to`) | Groups | yes | **wrong** `/groups` |
| Traders | `/traders` | yes | Traders | yes | yes |
| Trader Detail | `/traders/:brokerId/:login` | **no** (correct) | — | yes | yes |
| Trade Explorer | `/trades` | yes | Trades | yes | yes |
| Scoring | `/scoring` | yes | Scoring | yes | yes |
| Models | `/models` | **no** | — | **no** | **no** |
| Shadow Portfolio | `/shadow` | yes | Shadow | yes | yes |
| Live Copy Portfolio | `/live` | yes | Live | yes (8-line stub) | yes |
| cTrader FIX | `/fix` | yes | FIX | yes | yes |
| Risk | `/risk` | yes | Risk | yes | yes |
| Reconciliation | `/reconciliation` | yes | Recon | yes | yes |
| System Health | `/health` | yes | Health | yes | yes |
| Audit | `/audit` | yes | Audit | yes (8-line stub) | yes |
| Settings | `/settings` | yes | Settings | yes | yes |

A62 “15 sidebar items visible to every authenticated role”: **14 present, 1 absent (Models)**. Trader Detail omission is **correct**.

Sidebar order vs §46 (ignoring omitted Models + Trader Detail): Overview → Brokers → Groups → Traders → Trades → Scoring → Shadow → Live → FIX → Risk → Recon → Health → Audit → Settings. That is §46 order with the two omissions removed. **Order PASS. Completeness FAIL (Models). Labels FAIL (7 abbreviated).**

---

## 7. HEAD vs worktree (do not collapse)

`git ls-tree -r --name-only HEAD -- apps/web/src/pages` → **empty**. HEAD still has `App.tsx` + `DashboardLayout.tsx`. A clean checkout therefore **imports 13 page modules that are not in git**.

HEAD `DashboardLayout` (blob `1b40c4f6…`) `nav` is 12 rows — worktree minus Live and Audit:

```text
/overview /brokers /groups /traders /trades /scoring /shadow
/fix /risk /reconciliation /health /settings
```

HEAD `App.tsx` (blob `6555ae04…`) matches that 12-leaf table plus `traders/:brokerId/:login` plus the index redirect. It does **not** import `LiveCopyPage` / `AuditPage`.

Worktree inserted six lines:

```diff
+import LiveCopyPage from './pages/LiveCopyPage';
+import AuditPage from './pages/AuditPage';
+<Route path="live" element={<LiveCopyPage />} />
+<Route path="audit" element={<AuditPage />} />
+{ to: '/live', label: 'Live', icon: '▶' },
+{ to: '/audit', label: 'Audit', icon: '☰' },
```

Those two destinations exist on disk as 8-line stubs (C37 / C38 / C53 / D81 / D82). Routing them is chrome, not a Live book or an audit log.

---

## 8. What the join does **not** prove

| Claim someone might read from “nav complete” | Measured |
|---|---|
| Every §46 leaf is a sidebar item | **False** — Models absent; Trader Detail correctly not a leaf |
| Labels match §46 | **False** — 7/14 exact |
| Groups URL is A26 | **False** — `/groups` |
| Pages implement §§47–54 | **Not measured here** (see D77–D84 / D81–D83) |
| Live / Audit are real pages | **False** — 8-line stubs, no hooks |
| RBAC hides Audit | **False** — no role gate |
| Header strip exists | **False** |
| Unknown URLs redirect | **False** — empty outlet (`/models`, `/login`, `/mt5-groups`, `/no-such-page`) |
| HEAD checkout matches this table | **False** — 12-item nav, no `pages/` in git |

---

## 9. Stale-vs-later

| Prior claim | This pass |
|---|---|
| A62: 13 imports, 0 page files; Live/Audit/Models/Login missing | **Superseded** for files. Worktree has 15 pages. Live + Audit are in nav. Login + Models still missing. |
| B22: 13 files; no Live/Audit | **Superseded.** |
| B31: Models + Live + Audit nav gaps | Models still absent. Live + Audit **present on worktree**, absent on HEAD. |
| C53 “nav complete” filename | Chrome hole for Live/Audit is closed. §46 nav is **not** complete. |
| D08 / D38 / E003 chrome SHAs | **Confirmed.** Same `App.tsx` / layout SHAs at 13:50:14. |
| C39: do not create Models to complete nav | **Confirmed.** Absence of `/models` is not an E025 coding ticket. |

---

## 10. Remainders (nav layer only)

Do **not** treat this list as a widget build ticket.

1. **Do not add `/models` as a working page** — C39 / Phase 6 closed.
2. **`/login` lives outside this layout** — A26 §5.2 / C18. Not a 15th `nav` row.
3. **Labels** — A26 §5.3 wants verbatim §46 strings on the 7 abbreviated rows (`MT5 Groups`, `Trade Explorer`, `Shadow Portfolio`, `Live Copy Portfolio`, `cTrader FIX`, `Reconciliation`, `System Health`). Three of those strings already exist as page `<h1>`s.
4. **`/groups` → `/mt5-groups`** (and matching `to`) — path contract. Needs a redirect from `/groups` if anything bookmarked it.
5. **Commit or revert** the six unstaged Live/Audit lines **and** the untracked `pages/` tree. HEAD cannot render this nav.
6. **Header strip** — layout gap (A26 §5.3), not a new `nav` row.
7. **`end` / prefix** — Traders highlight on the detail URL is accidental RR v6 default. Do not set `end` on that `NavLink` without an explicit prefix rule.

None of these were implemented this pass.

---

## 11. Evidence

- Layout: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 5–20 (`const nav`) and 30–36 (`NavLink` map). SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`. 1854 bytes, 44 physical lines, CRLF.
- Router: `D:\Prop\apps\web\src\App.tsx` lines 3–17 (15 page imports) and 21–39 (16 destinations). SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Pages: `D:\Prop\apps\web\src\pages\` — 15 `*.tsx`, all untracked vs HEAD `398a142`.
- Only non-nav page link: `D:\Prop\apps\web\src\pages\TradersPage.tsx` line 28.
- Law: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §46 lines 1739–1758; `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2 lines 285–307, §5.3 lines 309–313; A62 `NAV_ITEMS` lines 417–436.
- Git: HEAD `398a14200ec65714c4077eed55c46808382ca1e3`; HEAD layout blob `1b40c4f6…` (12 nav); worktree layout blob `8ece6a45…` (14 nav); `git ls-tree HEAD -- apps/web/src/pages` empty.
- Product source edited: **No.**
