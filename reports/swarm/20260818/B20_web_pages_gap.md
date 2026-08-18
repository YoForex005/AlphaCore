# B20 — `apps/web` pages vs architecture §§46–54

| Field | Value |
|---|---|
| Agent | B20 (senior engineer, React page gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (second pass after Live/Audit stubs landed) |
| Workspace | `D:\Prop` |
| Product tree | `D:\Prop\apps\web\src` (23 files) |
| Architecture | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` **§§46–54** (nav + widgets). Supporting: §5 frontend, §55 secrets, §59 login/RBAC, §69.12 “show all of this in React” |
| Binding siblings | A26 (routes/API), A62 (scaffold), A91–A96 (page DTOs), A29 U01–U11 |
| Product source modified | **No.** This report is the only write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**§46–54 is not a finished dashboard. It is a Vite shell with 15 page modules, of which one required §46 page is still absent (`Models`) and nine of the sixteen nav destinations are stubs or thin tables that cannot paint the specified widgets.**

Honest measured state (disk, this pass):

| Check | Result |
|---|---|
| Vite + React 18 + TS + Router + TanStack Query | **PRESENT** |
| Page files under `src/pages` | **15** `.tsx` modules |
| §46 nav destinations that have a route **and** a file | **15 / 16** (`Models` is the only missing page module) |
| §46 nav labels used **exactly** | **0 / 16** (abbreviated: Groups / Trades / Shadow / Live / FIX / Recon / Health; Trader Detail and Models omitted from the sidebar) |
| §47–54 widgets painted as specified | **~34 / ~90** named facts (see §3–§10). None of §§47–54 is `EXISTS_AND_GOOD`. |
| Login (`/login`, §59) | **MISSING** (not in §46 list; still required to operate the shell) |
| Header strip (A26 §5.3) | **MISSING** |
| Charts (score / lot / holding-time) | **MISSING** — `recharts` is in `package.json` and **never imported** |
| Auth / RBAC / secret sanitizer | **MISSING** |
| §69.12 first-useful React bar | **FAIL** — demo-seed tables, not the 12-item operating bar |

Do **not** treat “15 page files exist” as §§46–54 complete. A29 U01–U11 (`MISSING`) is **stale** on file existence and **still correct** on widget completeness for Models and for every page whose required facts are not on screen.

**Overall: FAIL for architecture compliance. PASS for “the route table no longer 404s on the implemented names.” Those are different questions.**

---

## 1. Method

| Source | Path |
|---|---|
| Architecture law | `Architecture_v2.md` L1735–1999 (§46 nav; §47–54 widget lists) |
| Route table | `D:\Prop\apps\web\src\App.tsx` (2062 B) |
| Sidebar | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` (1854 B, 14 `nav` entries) |
| Pages | `D:\Prop\apps\web\src\pages\*.tsx` (15 files, listed in §2) |
| Hooks | `D:\Prop\apps\web\src\api\hooks.ts` (unversioned `/api/*`) |
| Types (orphaned) | `D:\Prop\apps\web\src\types\index.ts` — **zero imports** from any page |
| API actually called | `D:\Prop\apps\api\Program.cs` + `src\Application\Dashboard\DashboardModels.cs` + `src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| Prior specs (stale on “pages empty” / “weatherforecast”) | A06, A26, A29, A57, A62, A91–A96 |

Every required §46 label and every required §47–54 fact was checked against the current JSX. Nothing answered from A62’s empty-`pages/` snapshot. Product source was not edited.

---

## 2. What exists on disk (`apps/web/src`)

```text
src/
  App.tsx                         2062 B   15 child routes + index → /overview
  main.tsx                         648 B   QueryClient + BrowserRouter; no AuthProvider
  index.css                         62 B   Tailwind directives
  api/client.ts                    232 B   axios → VITE_API_URL or http://localhost:5000
  api/hooks.ts                    1935 B   11 hooks; no models/shadow/live/audit
  api/signalr.ts                   899 B   /hubs/dashboard (A26 is /hubs/ops); pages never subscribe
  components/MetricCard.tsx        521 B   used
  components/StatusBadge.tsx       699 B   UNUSED
  layouts/DashboardLayout.tsx     1854 B   14-item left nav; no header strip
  pages/  (15 modules — table below)
  types/index.ts                  2905 B   UNUSED; field names diverge from live API
  utils/formatters.ts              947 B   UNUSED
```

No `src/auth/`, `src/hubs/`, `src/pages/models/`, `LoginPage.tsx`, `ModelsPage.tsx`, tests, or Vite `/api` proxy (`vite.config.ts` is port 3000 only).

### 2.1 Route table vs §46

Architecture §46 main navigation (verbatim order):

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

| §46 label | Required route (A26 §5.2) | `App.tsx` path | Page module | Sidebar label | Page class |
|---|---|---|---|---|---|
| Overview | `/overview` | `/overview` | `OverviewPage.tsx` | Overview | **EXISTS_NEEDS_REFACTOR** |
| Brokers | `/brokers` | `/brokers` | `BrokersPage.tsx` | Brokers | **EXISTS_NEEDS_REFACTOR** |
| MT5 Groups | `/mt5-groups` | **`/groups`** (wrong) | `GroupsPage.tsx` | Groups | **EXISTS_NEEDS_REFACTOR** |
| Traders | `/traders` | `/traders` | `TradersPage.tsx` | Traders | **EXISTS_NEEDS_REFACTOR** |
| Trader Detail | `/traders/:brokerId/:login` | present | `TraderDetailPage.tsx` | **not in nav** | **EXISTS_NEEDS_REFACTOR** |
| Trade Explorer | `/trades` | `/trades` | `TradeExplorerPage.tsx` | Trades | **EXISTS_NEEDS_REFACTOR** |
| Scoring | `/scoring` | `/scoring` | `ScoringPage.tsx` | Scoring | **EXISTS_NEEDS_REFACTOR** |
| **Models** | **`/models`** | **absent** | **no file** | **absent** | **MISSING** |
| Shadow Portfolio | `/shadow` | `/shadow` | `ShadowPortfolioPage.tsx` | Shadow | **EXISTS_NEEDS_REFACTOR** (static stub) |
| Live Copy Portfolio | `/live` | `/live` | `LiveCopyPage.tsx` (321 B) | Live | **EXISTS_NEEDS_REFACTOR** (static stub) |
| cTrader FIX | `/fix` | `/fix` | `FixSessionsPage.tsx` | FIX | **EXISTS_NEEDS_REFACTOR** |
| Risk | `/risk` | `/risk` | `RiskPage.tsx` | Risk | **EXISTS_NEEDS_REFACTOR** |
| Reconciliation | `/reconciliation` | `/reconciliation` | `ReconciliationPage.tsx` | Recon | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |
| System Health | `/health` | `/health` | `SystemHealthPage.tsx` | Health | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |
| Audit | `/audit` | `/audit` | `AuditPage.tsx` (324 B) | Audit | **EXISTS_NEEDS_REFACTOR** (static stub) |
| Settings | `/settings` | `/settings` | `SettingsPage.tsx` | Settings | **EXISTS_NEEDS_REFACTOR** (`JSON.stringify`) |
| *(Login, §59)* | `/login` | **absent** | **no file** | n/a | **MISSING** |

**Missing page modules (no file, no route):** `ModelsPage` (`/models`).  
**Missing operating page (outside 46–54 but required to use the shell):** `LoginPage` (`/login`).  
**Wrong path:** groups live at `/groups`, not A26 `/mt5-groups`.  
**Dead nav risk:** `/live` and `/audit` now resolve; `/models` still 404s if typed. There is no catch-all → `/overview`.

### 2.2 Missing pages (the list this report exists to produce)

1. **Models** — architecture §46 line “Models”. No `pages/ModelsPage.tsx`, no route, no hook, no GET. A26 §6.8 `GET /api/v1/models` is also absent on the API. Honest empty list would be acceptable (A52: ML not yet); **silence is not**.
2. **Login** — not named in §46, required by §59 / A26 §5.2 to reach any of the above under RBAC. Entire app is anonymous.

These are **not** missing as files (they exist as stubs and must not be counted as absent), but they are **missing as pages** in the architecture sense (no widgets, no data):

3. **Live Copy Portfolio** — `LiveCopyPage.tsx` is two sentences. No positions table, no `realCopyExecutionEnabled` from API, no `/api/v1/live/portfolio`.
4. **Audit** — `AuditPage.tsx` is two sentences. No table, no `GET /api/v1/audit`. Domain `AuditLog` exists; UI does not read it.
5. **Shadow Portfolio** — static copy only. No hook, no book, no P&L.
6. **Reconciliation / System Health / Settings** — raw `JSON.stringify` of a stub payload. Not the dashboards §46/§54 name.

---

## 3. §47 Overview — widget gap

Required (18 tiles). Source: architecture L1766–1789. Page: `OverviewPage.tsx` (12 `MetricCard`s + a real-copy banner). Hook: `GET /api/overview` → `OverviewDto`.

| §47 tile | Painted? | Evidence |
|---|---|---|
| Total MT5 accounts | **YES** | `data.totalAccounts` |
| Connected source brokers | **PARTIAL** | `data.connectedBrokers` — query counts **enabled** brokers (`Brokers.Count(b => b.Enabled)`), not live connections |
| XAUUSD traders | **YES** | `data.xauTraders` |
| Traders with >= 3 completed trades | **YES** | `data.tradersWithThreeTrades` |
| Watch | **YES** | `data.watch` |
| Shadow | **YES** | `data.shadow` |
| Live candidates | **YES** | `data.liveCandidates` |
| **Live copied** | **NO** | DTO field `live` exists; **page never reads it** |
| Risk blocked | **YES** | `data.riskBlocked` |
| Shadow P&L | **YES** | `data.shadowPnl` (currently sum of `SourceVsShadowSlippage`, not a real book) |
| Destination real P&L | **YES** | `data.destinationRealPnl` — query hardcodes **0** |
| Current XAU gross exposure | **NO** | DTO `xauGross` hardcoded 0; **not rendered** |
| Current XAU net exposure | **NO** | DTO `xauNet` hardcoded 0; **not rendered** |
| Destination free margin | **NO** | not on `OverviewDto` |
| Destination margin level | **NO** | not on `OverviewDto` |
| MT5 ingestion health | **PARTIAL** | boolean `mt5Healthy` → `OK`/`DOWN`. Cannot express `STALE` / `DEGRADED` / `UNKNOWN` (A91) |
| FIX quote health | **PARTIAL** | collapsed into one card `Q / T` |
| FIX trade health | **PARTIAL** | same card |

**Score: 9 complete + 4 partial + 5 missing = 18.**  
Twelve `MetricCard`s are on screen; QUOTE and TRADE share one card. Extra (not a §47 tile): real-copy banner. A26 wants that on the **global header**, not instead of the five missing tiles.

---

## 4. §48 Brokers — widget gap

Required 11 facts per broker. Page: 7 columns. Hook: `GET /api/brokers` → `BrokerStatusDto`.

| §48 field | Painted? | Notes |
|---|---|---|
| Display name | **YES** | `displayName` |
| Connection status | **PARTIAL** | query hardcodes `Connected = true` |
| Server | **YES** | |
| Manager login masked | **PARTIAL / WRONG** | API `MaskLogin` keeps high digits (`2027` → `2000`); JSX then appends `**` → `2000**`. A26 keep-last-two is `**27` |
| Group count | **YES** | |
| Account count | **YES** | |
| Deal ingest rate | **NO** | not on DTO, not on page |
| Last event | **NO** | DTO `lastEventAt` is `UtcNow`; page ignores it |
| Last successful history sync | **NO** | |
| Connection pool usage | **NO** | |
| Reconnect count | **NO** | |

**No secret values on this page** (password never requested). That is absence, not a sanitizer.

---

## 5. §49 MT5 Groups — widget gap

Required 7 columns. Page: 5. Hook: `GET /api/groups` (A26 is `GET /api/v1/mt5/groups`). DTO: `GroupRowDto`.

| §49 column | Painted? |
|---|---|
| Broker | **YES** |
| Group | **YES** |
| Accounts | **YES** |
| Enabled for analysis | **YES** |
| Plan mapping | **YES** |
| Last discovered | **NO** — DTO `lastDiscovered` unused |
| Last synced | **NO** — DTO `lastSynced` unused |

H1 text is “MT5 Groups”; sidebar says “Groups”; URL is `/groups`.

---

## 6. §50 Trader Leaderboard — widget gap

Required 15 columns + 8 filters. Page: 9 columns, **zero filter controls**. Hook: `useTraders({})` — `broker`/`state` params exist on the hook and are never passed from the UI.

| §50 column | Painted? |
|---|---|
| Broker | **YES** |
| Login | **YES** (links to detail) |
| Group | **YES** |
| Completed XAU trades | **YES** |
| Net source P&L | **YES** |
| Early score | **YES** |
| ML probability | **NO** (DTO `mlProbability` is always `null`; column still required — show `null` / “not trained”) |
| Risk score | **YES** |
| Martingale / Averaging-down / Lot escalation | **PARTIAL** — mashed into one `Flags` cell (`MG`/`AVG`/`ESC`) |
| Current state | **YES** |
| Shadow P&L | **NO** — DTO `shadowPnl` unused (query hardcodes 0) |
| Live allocation | **NO** — not on `TraderRowDto` |
| Last scored | **NO** — DTO `lastScored` unused |

| §50 filter | UI? | API? |
|---|---|---|
| broker | **NO** | query string only |
| group | **NO** | **NO** |
| state | **NO** | query string only |
| score | **NO** | **NO** |
| risk | **NO** | **NO** |
| trade count | **NO** | **NO** |
| martingale | **NO** | **NO** |
| date | **NO** | **NO** |

**Filters: 0 / 8.** Ranking is `OrderByDescending(earlyScore)` in `EfDashboardQueries` — not user-controlled.

---

## 7. §51 Trader Detail — widget gap

Required 16 blocks. Page: 8 `Info` chips + a footnote. API: `GetTraderAsync` returns the **same** `TraderRowDto` as the list. A93’s detail payload is **MISSING**.

| §51 block | Painted? |
|---|---|
| Account overview | **PARTIAL** — broker/login/state/trades/scores/P&L only |
| XAU trade history | **NO** |
| First 3 trades highlighted | **NO** — footnote only; no `firstThree` block (A93 D4) |
| Score timeline | **NO** (no chart lib used) |
| Risk flags | **PARTIAL** — martingale + averaging; no lot-escalation chip, no engine reasons |
| Behavior features | **NO** |
| Lot-size timeline | **NO** |
| Holding-time distribution | **NO** |
| SL/TP behavior | **NO** |
| Drawdown | **NO** |
| MFE/MAE when valid | **NO** |
| Shadow copied positions | **NO** |
| Shadow P&L | **NO** |
| Live copied positions | **NO** |
| Live P&L | **NO** |
| Source-to-destination mapping | **NO** |

`mlProbability` is shown as “not trained” — honest. Charts cannot appear: `recharts` unused, `echarts` not installed.

---

## 8. §52 cTrader FIX — widget gap

Required: two **separate** cards (QUOTE, TRADE) with shared + session-specific facts. Page maps whatever `GET /api/fix/sessions` returns. If the list is empty, the page is blank (no dedicated empty QUOTE/TRADE cards). Password is not rendered (**good**, by absence).

Shared (each session):

| Fact | Painted? |
|---|---|
| Host | **YES** (concatenated with port) |
| SSL Port | **PARTIAL** — port only; no SSL flag |
| Connected? | **YES** |
| Logged on? | **YES** |
| Session status | **YES** |
| Last inbound | **NO** — DTO `lastInbound` unused |
| Last outbound | **NO** — DTO `lastOutbound` unused |
| Message sequence state | **YES** — `inboundSeq / outboundSeq` |
| Reconnect count | **YES** |
| Last heartbeat / test request | **NO** |
| Errors | **NO** — DTO `lastError` unused |

QUOTE extra:

| Fact | Painted? |
|---|---|
| XAUUSD mapped? | **NO** |
| Instrument ID | **YES** |
| Bid | **YES** if `bid != null` |
| Ask | **YES** if `bid != null` |
| Quote age | **YES** (`quoteAgeSeconds`) |
| Spread | **NO** |

TRADE extra:

| Fact | Painted? |
|---|---|
| Execution enabled? | **WRONG PLACE** — printed on **every** card, including QUOTE |
| Open orders | **NO** |
| Open destination positions | **NO** |
| Last execution report | **NO** |
| Last reconciliation | **NO** |

A94 remains the wire contract. Current `FixSessionDto` is a flattened row, not two typed cards.

---

## 9. §53 Risk Dashboard — widget gap

Required 15 facts. Page: 4 cards + a string list. Hook: `GET /api/risk` → `RiskDashboardDto` (zeros + `KillSwitch` string). A95 is **not** this shape.

| §53 fact | Painted? |
|---|---|
| Execution account equity | **NO** |
| Balance | **NO** |
| Free margin | **NO** |
| Margin level | **NO** |
| Daily P&L | **YES** (query hardcodes 0) |
| Current drawdown | **NO** — DTO `drawdown` unused (also hardcoded 0) |
| XAU long quantity | **NO** — DTO `xauLong` unused |
| XAU short quantity | **NO** — DTO `xauShort` unused |
| Net XAU exposure | **YES** |
| Risk by copied trader | **NO** |
| Risk by source broker | **NO** |
| Rejected copy intents | **PARTIAL** — reasons only, no trader / time / intent id |
| Reasons for rejection | **PARTIAL** — same list |
| `STOP_NEW_EXECUTION` state | **PARTIAL** — single `killSwitch` string (`None` default). Violates A48 two-control law |
| `EMERGENCY_FLATTEN` availability | **NO** |

Extra card `Real copy ON/OFF` is not a §53 substitute for flatten availability.

---

## 10. §54 Reconciliation Dashboard — widget gap

Required 8 facts + “nothing unresolved should be silently ignored.” Page is:

```tsx
<pre>{JSON.stringify(data, null, 2)}</pre>
```

API stub (`Program.cs`):

```text
{ lastReconciliation, unknownPositions, mismatches, orphanFills }
```

| §54 fact | Painted as a widget? | On stub JSON? |
|---|---|---|
| Last successful **MT5** reconciliation | **NO** | one undifferentiated timestamp |
| Last successful **cTrader** reconciliation | **NO** | **NO** |
| Unknown external positions | **NO** | count only |
| Missing internal positions | **NO** | **NO** |
| Order mismatches | **NO** | folded into `mismatches` |
| Quantity mismatches | **NO** | folded into `mismatches` |
| Orphan fills | **NO** | count only |
| Unresolved execution states | **NO** | **NO** |

A JSON dump of four integers is **not** a reconciliation dashboard. Unresolved issues can be dropped because they are never listed. U10 stays failed.

---

## 11. §46 pages that 47–54 do not specify (still required)

| Page | File | What it does | Gap |
|---|---|---|---|
| Trade Explorer | `TradeExplorerPage.tsx` | Table of last 200 `ReconstructedTrades` | No broker filter, no first-3 mark, no XAU-only guarantee, no MFE/MAE |
| Scoring | `ScoringPage.tsx` | Reuses `useTraders`; 4 score columns | No `GET /scoring/summary`, no version `baseline.v1`, reads `behaviorScore` which **is not** on `TraderRowDto` (renders `0.0`) |
| Models | — | — | **MISSING** entire page |
| Shadow Portfolio | `ShadowPortfolioPage.tsx` | Static policy text | No book, no P&L, no hook |
| Live Copy Portfolio | `LiveCopyPage.tsx` | Static “flag is false” | Correct honesty; still no empty-safe table / flag from API |
| System Health | `SystemHealthPage.tsx` | `JSON.stringify(/api/health)` | No component grid, no §58 metrics |
| Audit | `AuditPage.tsx` | Static text | No rows; `AuditLog` unused |
| Settings | `SettingsPage.tsx` | `JSON.stringify(/api/settings)` | No PATCH, no symbol map, no non-secret FIX form |

---

## 12. Shell, stack, and contract gaps (block every page)

| Item | Architecture / A26 | Measured |
|---|---|---|
| Left nav labels **exactly** as §46 | §46 + A26 §5.3 | Abbreviated; Models + Trader Detail omitted |
| Header strip: `REAL_COPY`, `STOP_NEW`, MT5 / QUOTE / TRADE health | A26 §5.3 | **MISSING** |
| Auth + 4 roles | §59 | **MISSING** — CORS `AllowAnyOrigin`, no JWT |
| API prefix `/api/v1` + envelopes | A26 §2 | Hooks call unversioned `/api/overview`, `/api/groups`, `/api/risk`, `/api/reconciliation/status` |
| SignalR `/hubs/ops` | A26 §7 | Client builds `/hubs/dashboard`; layout `startConnection()`; **`onEvent` never used** |
| ECharts or Recharts for §51 charts | §5 | `recharts` listed, **0 imports**. No `echarts`. |
| Zustand for ephemeral UI | §5 | **not in `package.json`** |
| Typed DTOs | A91–A96 | `types/index.ts` unused; pages use `any` |
| Secret denylist client | §48, §52, §55 | **MISSING** (no password shown today) |
| Catch-all → `/overview` | A26 §5.2 | **MISSING** (`/models` is a blank outlet) |

Unused / dead UI code (do not count as features): `StatusBadge.tsx`, `utils/formatters.ts`, `types/index.ts`, `recharts` dependency.

---

## 13. A29 U-series — honest reclass (pages only)

A29 §4.8 still says every U01–U11 is `MISSING`. File existence has moved. Widget completeness has not.

| ID | Component | A29 (stale) | This pass |
|---|---|---|---|
| U01 | Overview | MISSING | **EXISTS_NEEDS_REFACTOR** (9 complete + 4 partial; 5 tiles missing) |
| U02 | Brokers | MISSING | **EXISTS_NEEDS_REFACTOR** (6/11 fields) |
| U03 | MT5 Groups | MISSING | **EXISTS_NEEDS_REFACTOR** (5/7 columns; wrong path) |
| U04 | Trader leaderboard | MISSING | **EXISTS_NEEDS_REFACTOR** (11/15 cols; 0/8 filters) |
| U05 | Trader detail | MISSING | **EXISTS_NEEDS_REFACTOR** (header chips only) |
| U06 | Trade explorer / Scoring / Models | MISSING | **SPLIT:** explorer + scoring = **EXISTS_NEEDS_REFACTOR**; **Models = MISSING** |
| U07 | Shadow / Live copy | MISSING | **SPLIT:** both files exist as **static stubs** (`EXISTS_NEEDS_REFACTOR`) |
| U08 | cTrader FIX cards | MISSING | **EXISTS_NEEDS_REFACTOR** (flat cards; TRADE extras missing) |
| U09 | Risk dashboard | MISSING | **EXISTS_NEEDS_REFACTOR** (4/15 facts) |
| U10 | Reconciliation | MISSING | **EXISTS_NEEDS_REFACTOR** (JSON dump; 0/8 widgets) |
| U11 | Health / Audit / Settings | MISSING | **SPLIT:** three files, all stubs / JSON dumps |

`EXISTS_AND_GOOD` count for §§46–54 pages: **0**.

---

## 14. First useful version (§69.12)

§69 item 12: “Show all of this in React” means items 1–11 (both brokers, groups, ~5k accounts, XAU capture, reconstruction, first-3, baseline score, ranking, QUOTE logon, instrument ID, shadow on destination quotes) are **visible**.

The current UI can render **demo-seeded** account/trader counts if the API is up on `:5000`. It cannot show live ingestion health, a discovered Pepperstone instrument, a shadow book, or two honest FIX cards. §69.12 remains **No**.

---

## 15. What a later coding wave must add (inventory only — not authorized here)

Do not implement from this list in this agent. Product source stays untouched.

1. **Create** `ModelsPage` + `/models` + nav label **Models** (empty-safe list; no promote).
2. **Create** `LoginPage` + `/login` when §59 is implemented.
3. **Rename** nav labels to the §46 strings; add **Trader Detail** only as a child of Traders (or keep it out of the sidebar **if** product decides §46 “Trader Detail” is a route, not a nav row — A26 treats it as a route; either way Models is still missing).
4. **Fix path** `/groups` → `/mt5-groups` (redirect old URL).
5. **Paint** the missing §47–54 facts listed in §§3–10. Do not grow the `JSON.stringify` pages.
6. **Replace** hooks with A26 `/api/v1/**` + A91–A96 DTOs. Delete or stop using `types/index.ts` as-is.
7. **Subscribe** SignalR to `/hubs/ops` or stop pretending the hub is live.
8. **Add** header strip. **Add** leaderboard filters in the URL. **Add** first-3 highlight on detail.

---

## 16. Evidence pin (page SHA-256 prefixes, this pass)

| File | Bytes | SHA-256 (first 16 hex) |
|---|---|---|
| `App.tsx` | 2062 | `A0E92C9779A0C777` |
| `layouts/DashboardLayout.tsx` | 1854 | `48F7073E50B75B37` |
| `pages/OverviewPage.tsx` | 2078 | `6497193F190445CC` |
| `pages/BrokersPage.tsx` | 1266 | `274754E3DD14D4D8` |
| `pages/GroupsPage.tsx` | 1228 | `4F7874826403712D` |
| `pages/TradersPage.tsx` | 1604 | `0AF0FF5BD2EE6B7B` |
| `pages/TraderDetailPage.tsx` | 1592 | `6CAE0FC902D8DFDB` |
| `pages/TradeExplorerPage.tsx` | 1321 | `7EE11EB97DBBE0ED` |
| `pages/ScoringPage.tsx` | 1288 | `F417592E7ECC16F1` |
| `pages/ShadowPortfolioPage.tsx` | 628 | `608C8C2D2D0F3FE8` |
| `pages/LiveCopyPage.tsx` | 321 | `F85CF339AAD7B2A9` |
| `pages/FixSessionsPage.tsx` | 1312 | `EC93326688719E10` |
| `pages/RiskPage.tsx` | 1148 | `FC4C5F05E1FF998F` |
| `pages/ReconciliationPage.tsx` | 490 | `BC036D09A78AECBA` |
| `pages/SystemHealthPage.tsx` | 369 | `03BDBC76CBEFEE4A` |
| `pages/AuditPage.tsx` | 324 | `8DE2F9B0AA9B1479` |
| `pages/SettingsPage.tsx` | 459 | `57D41B908C591238` |

**Models: no file.** Login: no file.

---

## 17. One-line close

**Missing page: Models. Missing operating page: Login. Missing dashboards in the architecture sense: Live Copy, Audit, Shadow, Reconciliation, Health, Settings (stubs). Missing widgets: Live copied + exposures + dest margin on Overview; 5 broker ops fields; 2 group timestamps; 4 leaderboard columns + all 8 filters; almost all of Trader Detail; FIX TRADE extras + spread/heartbeat/errors; 11 of 15 Risk facts; all 8 Reconciliation facts.**
