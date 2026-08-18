# B31 — Dashboard nav vs architecture §46 (Models, Live Copy, Audit)

| Field | Value |
|---|---|
| Agent | B31 (senior engineer, nav-gap only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 13:20:38+ (file timestamps on `App.tsx`, `DashboardLayout.tsx`, `LiveCopyPage.tsx`, `AuditPage.tsx`) |
| Workspace | `D:\Prop` |
| Question | Dashboard nav vs architecture **§46** — are **Models**, **Live Copy Portfolio**, and **Audit** missing? |
| Product source modified | **No.** This report is the only write. |
| Method | Read architecture §46 + A26 §5.2/§5.3/§6.8/§6.10/§6.15; read current `DashboardLayout.tsx`, `App.tsx`, every `pages/*.tsx`; census `hooks.ts` + `apps/api/Program.cs` + `IDashboardQueries`; SHA-256 + byte/line census; cross-check B10 / B22 (now stale on Live/Audit files). |

**Precedence:** architecture §46 for **labels and order**. A26 for **routes and page contracts**. A62 `NAV_ITEMS` for the resolved sidebar (Trader Detail is a detail route, not a dead leaf). A63 for first-useful API subset. On-disk tree supersedes A62/B10/B22 file-existence claims.

---

## 0. Verdict

**Models is missing. Live Copy and Audit are no longer missing as files/routes/nav entries, but they are not §46 pages.**

Do not answer “three items missing” from B10/B22. Those reports measured a 12-link sidebar at ~13:19. At **13:20:38** another wave added `LiveCopyPage.tsx`, `AuditPage.tsx`, `/live`, `/audit`, and two abbreviated sidebar links. **`ModelsPage` was never added.**

| Item | §46 label | A26 route | Sidebar? | Route? | Page file? | Hook? | API? | Classification |
|---|---|---|---|---|---|---|---|---|
| Models | `Models` | `/models` | **No** | **No** | **No** | **No** | **No** | **MISSING** |
| Live Copy Portfolio | `Live Copy Portfolio` | `/live` | Yes, label **`Live`** (wrong) | Yes | `LiveCopyPage.tsx` (321 B stub) | **No** | **No** | **EXISTS_NEEDS_REFACTOR** (chrome) / **MISSING** (contract) |
| Audit | `Audit` | `/audit` | Yes, label **exact** | Yes | `AuditPage.tsx` (324 B stub) | **No** | **No** | **EXISTS_NEEDS_REFACTOR** (chrome) / **MISSING** (contract) |

Direct answers:

1. **Models missing?** **Yes — fully.** Not in the sidebar, not in `App.tsx`, no `pages/ModelsPage.tsx`, no `useModels`, no `GET /api/v1/models`, no promote. Scoring is **not** Models.
2. **Live Copy missing?** **No longer absent from the shell.** Present as abbreviated **`Live`** → `/live` → static copy. **Yes missing** as the A26 Live Copy Portfolio (`GET /api/v1/live/portfolio`, `realCopyExecutionEnabled`, positions `[]`).
3. **Audit missing?** **No longer absent from the shell.** Present as **`Audit`** → `/audit` → static copy. **Yes missing** as the A26/A51/A63 audit reader (`GET /api/v1/audit` or `/api/v1/audit/logs`, filters, sanitizer, ReadOnly 403). `audit_logs` is mapped in EF; nothing reads it.

Sidebar vs §46 (exact-label law, A26 §5.3 / A62 §6): **14 links on disk, 15 required sidebar leaves.** Missing leaf = **Models**. Live + Audit exist with non-contract depth. Eight other labels are abbreviated. Groups path is `/groups`, not `/mt5-groups`.

This report does **not** authorize creating or rewriting product files.

---

## 1. Binding sources

| Role | Path |
|---|---|
| Architecture law (§46 main navigation) | `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758 |
| Models contract | same file **§21** (evaluate later); A26 §6.8; A52 Phase 6 hold; A104 “if present, say Phase 6 not open” |
| Live Copy contract | architecture **§32–§35**, **§41**; A26 §6.10; A30 increment 6 “nav may exist as not in v1” |
| Audit contract | architecture **§59**; A26 §6.15; A51 hide ReadOnly/Analyst; A63 `GET /api/v1/audit/logs` **in** first-useful |
| Route map + “labels **exactly** as §46” | `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2–§5.3 |
| Resolved sidebar (15 items; Trader Detail not a leaf) | `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` lines 417–436 |
| First-useful API subset | `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` (`/models` and `/live/portfolio` **out of v1**; audit GET **in**) |
| Stale “all three absent” snapshots | `B10_web_gap.md` §5, `B22_web_missing_pages.md` §5 (pre-13:20:38) |

When documents disagree: **§46 labels win**. A26 paths win. A63 decides what a first-useful **API** must serve. A30/A52/A104 forbid a **working** Models/Live book before those phases; they do **not** forbid an honest empty nav entry.

---

## 2. Architecture §46 required navigation (verbatim)

From `…Architecture_v2.md` #46 “React Dashboard” / “Main navigation”:

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

**16 labels.** A62 resolves Trader Detail to a leaderboard drill-down (`/traders/:brokerId/:login`), sidebar highlight stays on Traders. Binding sidebar = **15 leaves**, this order, **exact strings**. Login is A26-only (`/login`), not a §46 nav row.

A26 §5.3 (binding for chrome): *“Left nav labels **exactly** as §46.”*

---

## 3. What is on disk now

### 3.1 Sidebar — `DashboardLayout.tsx` lines 5–20

SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` · 1854 bytes · written 13:20:38.

```text
Overview              /overview
Brokers               /brokers
Groups                /groups          ← not “MT5 Groups”, not /mt5-groups
Traders               /traders
Trades                /trades          ← not “Trade Explorer”
Scoring               /scoring
Shadow                /shadow          ← not “Shadow Portfolio”
Live                  /live            ← not “Live Copy Portfolio”
FIX                   /fix             ← not “cTrader FIX”
Risk                  /risk
Recon                 /reconciliation  ← not “Reconciliation”
Health                /health          ← not “System Health”
Audit                 /audit           ← exact §46 label
Settings              /settings
```

**14** `NavLink`s. No Models. No Trader Detail leaf (acceptable per A62). No login. No header strip (`REAL_COPY_EXECUTION_ENABLED`, `STOP_NEW_EXECUTION`, FIX QUOTE/TRADE, MT5 ingest).

### 3.2 Router — `App.tsx`

SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` · 2062 bytes · written 13:20:38.

Imported pages (15): Overview, Brokers, Groups, Traders, TraderDetail, TradeExplorer, Scoring, ShadowPortfolio, **LiveCopy**, FixSessions, Risk, Reconciliation, SystemHealth, **Audit**, Settings.

Routes: `/` → `/overview`, plus the 14 sidebar paths, plus `/traders/:brokerId/:login`. **No `/models`. No `/login`.** Unknown URLs are not redirected to `/overview` (A26 default).

### 3.3 `pages/` census

`D:\Prop\apps\web\src\pages` — **15** `.tsx` files. No subfolders. **No `ModelsPage.tsx`.** No `LoginPage.tsx`.

| File | Bytes | Written | SHA-256 |
|---|---:|---|---|
| `AuditPage.tsx` | 324 | 13:20:38 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| `LiveCopyPage.tsx` | 321 | 13:20:38 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| `ShadowPortfolioPage.tsx` | 628 | 13:16:43 | `608C8C2D2D0F3FE89EC7632159217191809EB92805051EE9529626B78AE36276` |
| `ScoringPage.tsx` | 1288 | 13:16:43 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` |
| other 11 pages | — | 13:16:00–13:16:43 | unchanged vs B22 |

`hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` — **no** `useModels` / `useLive` / `useAudit`. `types/index.ts` has `livePositions` on trader detail only; no Model / Audit / Live-portfolio types.

---

## 4. §46 × current nav (1:1)

| # | §46 label | Required route (A26) | Sidebar label now | Route now | Page module | Status |
|---|---|---|---|---|---|---|
| 1 | Overview | `/overview` | Overview | `/overview` | `OverviewPage.tsx` | **PRESENT** (label exact) |
| 2 | Brokers | `/brokers` | Brokers | `/brokers` | `BrokersPage.tsx` | **PRESENT** |
| 3 | MT5 Groups | `/mt5-groups` | **Groups** | **`/groups`** | `GroupsPage.tsx` | **WRONG** path + label |
| 4 | Traders | `/traders` | Traders | `/traders` | `TradersPage.tsx` | **PRESENT** |
| 5 | Trader Detail | `/traders/:brokerId/:login` | *(not a leaf)* | yes | `TraderDetailPage.tsx` | **PRESENT** as drill-down (A62) |
| 6 | Trade Explorer | `/trades` | **Trades** | `/trades` | `TradeExplorerPage.tsx` | **WRONG** label |
| 7 | Scoring | `/scoring` | Scoring | `/scoring` | `ScoringPage.tsx` | **PRESENT** (not Models) |
| 8 | **Models** | **`/models`** | **—** | **—** | **—** | **MISSING** |
| 9 | Shadow Portfolio | `/shadow` | **Shadow** | `/shadow` | `ShadowPortfolioPage.tsx` | **WRONG** label; static-ish |
| 10 | **Live Copy Portfolio** | **`/live`** | **Live** | `/live` | `LiveCopyPage.tsx` | **PARTIAL** — stub + wrong label |
| 11 | cTrader FIX | `/fix` | **FIX** | `/fix` | `FixSessionsPage.tsx` | **WRONG** label |
| 12 | Risk | `/risk` | Risk | `/risk` | `RiskPage.tsx` | **PRESENT** |
| 13 | Reconciliation | `/reconciliation` | **Recon** | `/reconciliation` | `ReconciliationPage.tsx` | **WRONG** label |
| 14 | System Health | `/health` | **Health** | `/health` | `SystemHealthPage.tsx` | **WRONG** label |
| 15 | **Audit** | **`/audit`** | **Audit** | `/audit` | `AuditPage.tsx` | **PARTIAL** — stub, label exact |
| 16 | Settings | `/settings` | Settings | `/settings` | `SettingsPage.tsx` | **PRESENT** |
| — | Login (A26, not §46) | `/login` | — | — | — | **MISSING** |

**Exact-label score:** 6 / 15 sidebar leaves match §46 text (`Overview`, `Brokers`, `Traders`, `Scoring`, `Risk`, `Audit`, `Settings` = 7 if Audit is counted; Live does **not** match). **Models = 0/4 surfaces.**

---

## 5. Models — fully missing

### 5.1 Disk

Grep of `D:\Prop\apps\web` for `ModelsPage`, `/models`, `useModels`: **zero** hits outside this investigation’s expected absences. No file, no import, no `Route path="models"`, no nav `{ to: '/models' }`.

Scoring (`/scoring`, title “Deterministic scoring”, copy “XGBoost is not active”) is **§18 baseline**, not §21 Models. Do not treat it as the Models leaf.

### 5.2 Required later (not implemented)

| Layer | Binding | Required | Now |
|---|---|---|---|
| Nav | §46 + A26 §5.3 + A62 | Label **`Models`**, order after Scoring | absent |
| Route | A26 §5.2 | `/models` | absent (falls through; no redirect to `/overview`) |
| Page | A62 `pages/models/ModelsPage.tsx` | version table; empty list honest | absent |
| GET | A26 §6.8 | `GET /api/v1/models` → `{ data: [ { modelVersionId, name, status, metrics, promotedAt } ] }` | no map in `Program.cs` |
| Promote | A26 §6.8; SuperAdmin + `confirmPhrase: "PROMOTE_MODEL"` | Phase 6 only; **forbidden** to self-promote | absent (correct) |
| First-useful API | A63 | **`/models` out of v1** | aligned — do not stub fake ML |

### 5.3 Phase policy (do not over-build)

- Architecture §1 / §18 / §67 Phase 6 / A52: ML is **not** open. §69 first useful version does **not** include Models working.
- A30 increment 6: *“Do not add Models, Live Copy Portfolio as **working** pages. Nav entries may exist as ‘not in v1’.”*
- A62: *“Nav yes, promote later. Empty list is honest. No self-promotion.”*
- A104: *“Models page (if present) shows ‘Phase 6 not open’ / no production model. Do not draw a sparkline of fake probabilities.”*

**Honest gap:** the **nav leaf** is missing. A later wave may add `/models` that renders “Phase 6 not open” / `data: []`. It must **not** invent `mlProbability`, promote, or a training UI.

---

## 6. Live Copy Portfolio — shell only

### 6.1 What landed at 13:20:38

`LiveCopyPage.tsx` entire body (321 bytes):

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

Nav label is **`Live`**, not **`Live Copy Portfolio`**. Route `/live` matches A26. No hook. Hard-coded flag text; does not read `OverviewDto.RealCopyEnabled` or `GET /api/settings`.

### 6.2 Required vs now

| Layer | Binding | Required | Now |
|---|---|---|---|
| Nav | §46 exact | `Live Copy Portfolio` | `Live` |
| Route | A26 | `/live` | `/live` |
| GET | A26 §6.10 | `GET /api/v1/live/portfolio` with `realCopyExecutionEnabled`, `pnl`, `openCount`, `positions[]` | **no endpoint** |
| Empty-safe | A26: when execution off, `positions` may be `[]`; **still return the flag** | flag is a string in JSX | 
| Mutations | A26 page matrix | none on this page (flatten/enable live live on Risk/Settings) | none |
| First-useful API | A63 | `/live/portfolio` **out of v1** | no GET is acceptable for §69; empty-safe UI is still the A62/A26 read story |
| Domain | `CopyIntent`, `ExecutionIntent`, `ShadowOrder` | dest live book | no live-portfolio query |

`IDashboardQueries` (`DashboardModels.cs` lines 88–97) has Overview / Brokers / Groups / Traders / Trader / FIX / Risk. **No** `GetLivePortfolioAsync`. `Program.cs` maps no `/api/live*`.

### 6.3 Do not confuse with Shadow

`/shadow` + `ShadowPortfolioPage` is the Phase 5 book (also thin, no hook). §46 requires **both** leaves. Overview shows Shadow / Live **candidates** KPIs; it does **not** render `data.live` (“Live copied”) — B10 already flagged that widget hole. That is Overview §47, not this nav row.

A30 allows a “not in v1” nav entry. The current stub’s *intent* (execution off → empty) is directionally right. It still fails exact-label law and does not consume a contract DTO.

---

## 7. Audit — shell only

### 7.1 What landed at 13:20:38

`AuditPage.tsx` entire body (324 bytes):

```tsx
export default function AuditPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Audit</h1>
      <p className="text-gray-400 text-sm">Manual overrides, kill-switch changes, and mapping edits must land here. RBAC is not enabled in the demo seed.</p>
    </div>
  );
}
```

Sidebar label **`Audit`** matches §46. Route `/audit` matches A26. Copy admits RBAC is off. **No table. No filters. No fetch. Invents no rows** (good) **and shows no rows** (not a reader).

### 7.2 Required vs now

| Layer | Binding | Required | Now |
|---|---|---|---|
| Nav | §46 exact | `Audit` | `Audit` |
| Visibility | A51 §5 / A26 §10.1 | hide from ReadOnly **and** Analyst in A51; A26 GET allows Analyst | always visible; **no roles** |
| GET | A26 §6.15 `GET /api/v1/audit`; A51/A63 `GET /api/v1/audit/logs` | query `actorId`, `action`, `from`, `to`, `entityType`, `entityId`; sanitized `before`/`after` | **no map** |
| ReadOnly | A26 / A62 | 403 → `ErrorState`, not a fake empty table | n/a (anonymous) |
| Persistence | `AuditLog` + `DbSet<AuditLog>` → `audit_logs` | append-only on privileged verbs (§59) | table mapped; **no writer** on `POST /api/ops/resync` (anonymous, unaudited); **no reader** |
| First-useful | A63 | audit GET **is** in the v1 catalog | **gap vs first-useful**, unlike Models/Live |

Domain entity (`src/Domain/Entities/AuditLog.cs`): `Id`, `Actor`, `Role`, `Action`, `Target`, `PayloadJson`, `At`. Narrower than A26 (`actorEmail`, `entityType`, `entityId`, `reason`, `correlationId`, `before`, `after`). Exists ≠ Audit page.

A51 still says “React Audit page does not exist yet.” **Stale** for the file; still true for the reader.

---

## 8. API / Application confirmation (no BFF for the three)

`D:\Prop\apps\api\Program.cs` live maps (unversioned, anonymous):

```text
GET  /health
GET  /api/health
GET  /api/risk/status
GET  /api/reconciliation/status
GET  /api/settings
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

**Zero** of: `/api/v1/models`, `/api/v1/models/{id}/promote`, `/api/v1/live/portfolio`, `/api/v1/copy-intents`, `/api/v1/audit`, `/api/v1/audit/logs`.

`IDashboardQueries` / `EfDashboardQueries` implement none of those three reads.

`hooks.ts` consumers: overview, brokers, groups, traders, trader detail, trades, fix, risk, reconciliation, health, settings. **Not** models / live / audit.

---

## 9. Other §46 chrome gaps (same measurement, not the three)

Recorded so “nav vs §46” is not reduced to three names:

| Gap | Evidence |
|---|---|
| Exact labels | 8 leaves abbreviated (`Groups`, `Trades`, `Shadow`, `Live`, `FIX`, `Recon`, `Health`; Models absent) |
| Groups path | `/groups` + `GET /api/groups` vs A26 `/mt5-groups` + `/api/v1/mt5/groups` |
| Header strip | A26 §5.3 five chips — **absent** from `DashboardLayout` |
| Auth / `/login` | no `LoginPage`, no `RequireAuth`, `main.tsx` is QueryClient + BrowserRouter only |
| RBAC | Audit always shown; A51 would hide it from ReadOnly/Analyst |
| Catch-all | A26 unknown → `/overview`; not implemented |
| `paths.ts` / `NAV_ITEMS` | A62 required single source — **not created**; labels live as a raw array |

---

## 10. What B10 / B22 said, and what superseded them

| Claim | When | B31 (13:20:38+) |
|---|---|---|
| B10: “Login / Models / Live Copy / Audit **Absent**”; 12 abbreviated nav items | earlier 2026-08-18 | Live + Audit **files and links exist**. Models + Login still absent. Sidebar is **14** items. |
| B22: 13 page files; 13/13 imports; 4 spec pages never written | 13:19:28 | **15** page files. Live + Audit added and imported. Models + Login still never written. |
| A62 §0 “pages EMPTY”; A51 “Audit page does not exist” | morning snapshots | **Stale.** |

Use **this file** for the three-item question. Do not recreate Live/Audit files from B22’s missing-module list.

---

## 11. First-useful vs later (so a coding wave does not over-build)

| Surface | §69 / A63 first useful | §46 / A26 full |
|---|---|---|
| Models nav | optional “Phase 6 not open” leaf | required exact label |
| Models GET / promote | **out** | Phase 6; SuperAdmin + phrase |
| Live Copy nav | empty-safe “execution off” | required exact label |
| Live GET | **out** of A63 | A26 empty `positions` + flag |
| Audit nav | **yes** | required |
| Audit GET | **yes** (`/api/v1/audit/logs`, RiskManager+) | A26 `/api/v1/audit` (path spelling still open — pick one catalog and do not implement both) |

A63 and A26 **disagree on the audit path** (`/audit/logs` vs `/audit`). Binding for implementation is A63 for first-useful host work; do not ship two readers.

---

## 12. What a later wave must not do

1. Do **not** treat Scoring as Models.
2. Do **not** invent `mlProbability`, model weights, or a promote button before Phase 6 (A52, A104, §71).
3. Do **not** recreate `LiveCopyPage.tsx` / `AuditPage.tsx` as if they were missing files.
4. Do **not** leave sidebar labels abbreviated if the task is “§46 exact nav.”
5. Do **not** add a second frontend or a second `pages/` tree under `D:\Prop\src`.
6. Do **not** dump `AuditLog` entities or secrets into JSON. Sanitizer on `before`/`after`.
7. Do **not** enable live `NewOrderSingle` to “fill” the Live page. Empty + flag is honest.
8. Do **not** hide the Audit route from the API with UI-only checks; RBAC is the authority (A51).
9. This report does **not** authorize product edits.

---

## 13. Direct answers (copy-out)

### Are Models, Live Copy, and Audit missing from the dashboard nav vs architecture §46?

| | Nav leaf | Exact §46 label | Route | Page module | Data contract |
|---|---|---|---|---|---|
| **Models** | **Missing** | — | **Missing** `/models` | **Missing** | **Missing** (and correctly out of A63 v1) |
| **Live Copy Portfolio** | Present as **`Live`** | **No** | `/live` present | 321 B stub | **Missing** `GET /api/v1/live/portfolio` |
| **Audit** | Present | **Yes** | `/audit` present | 324 B stub | **Missing** `GET /api/v1/audit[ /logs ]`; first-useful gap |

**One-line:** Models is gone from the information architecture; Live Copy and Audit were stubbed into the sidebar after B22 and still fail the §46/A26 page contracts.

### Did this pass change product source?

**No.**

---

## Evidence pins

- Architecture §46: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` lines 1735–1758.
- A26 routes + exact-label law: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` §5.2–§5.3, §6.8, §6.10, §6.15.
- A62 `NAV_ITEMS`: `D:\Prop\reports\swarm\20260818\A62_react_scaffold.md` lines 417–436.
- Sidebar: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` lines 5–20. SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Router: `D:\Prop\apps\web\src\App.tsx`. SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Stubs: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (321 B), `AuditPage.tsx` (324 B). No `ModelsPage.tsx`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 queries, none for the three.
- API: `D:\Prop\apps\api\Program.cs` — 15 maps; no models/live/audit.
- Queries: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` `IDashboardQueries` — 7 methods, none for the three.
- Audit table only: `D:\Prop\src\Domain\Entities\AuditLog.cs`; `TraderDbContext` `DbSet<AuditLog>` + `ToTable("audit_logs")`.
- Supersedes B10 §5 / B22 §5 on Live/Audit **file** absence. Does not supersede their widget-depth findings.
