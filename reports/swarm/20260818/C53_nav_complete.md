# C53 — LiveCopyPage + AuditPage existence (nav chrome)

| Field | Value |
|---|---|
| Agent | C53 (senior engineer, Live/Audit file + route + sidebar confirm only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:28:21+05:30 |
| Workspace | `D:\Prop\src` (Vite app is **not** under `D:\Prop\src`) |
| Assigned question | Confirm `LiveCopyPage` and `AuditPage` exist. Write this report. |
| Product source modified | **No.** This report is the only write. |
| Method | `list_dir` on `apps/web/src/pages`; `read_file` of both pages + `App.tsx` + `DashboardLayout.tsx` + `hooks.ts`; SHA-256 + byte/physical-line census; `Test-Path` on workspace-relative miss; grep for `useLive` / `useAudit` / `/api/v1/live` / `/api/v1/audit` |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

**Precedence:** on-disk tree supersedes B10 / B22 (“Live + Audit never created”). A26 + architecture §46 remain binding for **contract depth** and **exact labels**. File existence is not page completeness.

---

## 0. Verdict

**YES. Both pages exist, export the expected default, and are wired.**

| Question | Answer | Evidence |
|---|---|---|
| Does `D:\Prop\src\apps\web\src\pages\LiveCopyPage.tsx` exist? | **No** | `Test-Path` false; no `apps/` under `src/` |
| Does `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` exist? | **Yes** | 321 B, SHA-256 below |
| Does `D:\Prop\apps\web\src\pages\AuditPage.tsx` exist? | **Yes** | 324 B, SHA-256 below |
| Default export `LiveCopyPage`? | **Yes** | line 1 `export default function LiveCopyPage()` |
| Default export `AuditPage`? | **Yes** | line 1 `export default function AuditPage()` |
| `App.tsx` imports both? | **Yes** | L11, L16 |
| Router mounts both? | **Yes** | `path="live"` L32, `path="audit"` L37 |
| Sidebar links both? | **Yes** | `/live` label **`Live`**, `/audit` label **`Audit`** |
| `useLive` / `useAudit` hooks? | **No** | `hooks.ts` has neither |
| `GET /api/v1/live/portfolio`? | **No** | `apps/api/Program.cs` has no `/live` and no `/api/v1/*` |
| `GET /api/v1/audit` (or `/audit/logs`)? | **No** | same; unversioned `/api/*` only, no audit map |
| `ModelsPage` / `/models`? | **No** | still never created |
| `LoginPage` / `/login`? | **No** | still never created |

Honest one-liner: **Live and Audit are on disk and reachable. Do not recreate the files. Do not treat B22 “Live+Audit MISSING” as current. Do not claim §46 nav or A26 contracts are done.**

| Item | Chrome (file + route + nav) | A26 contract | Class |
|---|---|---|---|
| Live Copy Portfolio | **present** (`/live` → stub) | **missing** (no hook, no API, empty static copy) | **`EXISTS_NEEDS_REFACTOR`** |
| Audit | **present** (`/audit` → stub) | **missing** (no hook, no GET, no filters, no RBAC) | **`EXISTS_NEEDS_REFACTOR`** |

“Nav complete” in this filename means **the B22 Live/Audit hole is closed in the shell**. It does **not** mean the sidebar matches architecture §46 (14 links vs 15 required leaves; **Models** still absent; eight labels abbreviated).

---

## 1. Method

| Source | Path / action |
|---|---|
| Assigned look-under-src | `Test-Path` `D:\Prop\src\apps\web\src\pages\{LiveCopyPage,AuditPage}.tsx` → **False** |
| Actual pages | `read_file` `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (full 8 lines); `AuditPage.tsx` (full 8 lines) |
| Directory | `list_dir` `D:\Prop\apps\web\src\pages` — **15** `*.tsx` including both names |
| Router | `D:\Prop\apps\web\src\App.tsx` (full 42 physical lines) |
| Sidebar | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` (full 44 physical lines) |
| Hooks | `D:\Prop\apps\web\src\api\hooks.ts` (1935 B) — no live/audit exports |
| API | grep `audit` / `live/portfolio` / `/models` / `/login` in `D:\Prop\apps\api\Program.cs` — **0 hits** |
| Extra hunt | recursive `*Live*` / `*Audit*` / `*Models*` / `*Login*` under `apps/web/src` — only the two page files |
| Metrics | PowerShell `Get-Item`, `Get-FileHash SHA256`, LF-count physical lines |
| Stale “absent” snapshots | `B10_web_gap.md`, `B22_web_missing_pages.md` (13 files, pre-13:20:38) |
| Later same-day confirmations | `C08_web_pages_review.md`, `B31_nav_gaps.md` (already listed both files) |
| Binding contracts | A26 §5.2 `/live` + `/audit`; A26 §6.10 `GET /api/v1/live/portfolio`; A26 §6.15 `GET /api/v1/audit`; architecture §46 labels |

No `npm`, no `tsc`, no `vite`, no product edit.

---

## 2. File identity (disk, this pass)

### 2.1 `LiveCopyPage.tsx`

| Metric | Value |
|---|---|
| Absolute path | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` |
| Workspace-relative miss | `D:\Prop\src\apps\web\src\pages\LiveCopyPage.tsx` — **absent** |
| Bytes | **321** |
| Physical lines | **8** (LF, file ends with newline) |
| Non-blank lines | **8** |
| Newlines | **LF** (not CRLF) |
| SHA-256 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| Created (local) | `2026-08-18T13:20:38.2749244+05:30` |
| Last write (local) | `2026-08-18T13:20:38.2759253+05:30` |
| Default export | `export default function LiveCopyPage()` |
| Imports | **none** (no hook, no router, no types) |

Full source (8 lines):

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

Heading is sentence-case **“Live copy portfolio”**, not the §46 label **“Live Copy Portfolio”**. Copy is static. No `positions: []`, no `realCopyExecutionEnabled` field from the API.

### 2.2 `AuditPage.tsx`

| Metric | Value |
|---|---|
| Absolute path | `D:\Prop\apps\web\src\pages\AuditPage.tsx` |
| Workspace-relative miss | `D:\Prop\src\apps\web\src\pages\AuditPage.tsx` — **absent** |
| Bytes | **324** |
| Physical lines | **8** (LF, file ends with newline) |
| Non-blank lines | **8** |
| Newlines | **LF** (not CRLF) |
| SHA-256 | `8DE2F9B0AA9B14798C1C6F548E41837F6B5FF80869A3DBBFC91A13769A8E7B38` |
| Created (local) | `2026-08-18T13:20:38.2759253+05:30` |
| Last write (local) | `2026-08-18T13:20:38.2759253+05:30` |
| Default export | `export default function AuditPage()` |
| Imports | **none** |

Full source (8 lines):

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

H1 **“Audit”** matches §46. No table, no filters (`actorId`, `action`, `from`, `to`, `entityType`, `entityId`), no sanitizer, no ReadOnly 403 handling.

Hashes and sizes match C08 / B31. Files are **unchanged** since 13:20:38.

---

## 3. Router + sidebar wiring

### 3.1 `App.tsx`

| Metric | Value |
|---|---|
| Path | `D:\Prop\apps\web\src\App.tsx` |
| Bytes | **2062** |
| Physical lines | **42** |
| SHA-256 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` |
| Last write | `2026-08-18T13:20:38.3114705+05:30` |
| Newlines | **CRLF** |

Relevant lines:

- L11 `import LiveCopyPage from './pages/LiveCopyPage';`
- L16 `import AuditPage from './pages/AuditPage';`
- L32 `<Route path="live" element={<LiveCopyPage />} />`
- L37 `<Route path="audit" element={<AuditPage />} />`

Both imports resolve to files that exist. No dangling import. No unused page import. Routes match A26 §5.2 paths `/live` and `/audit`.

Page-import count remains **15**. Still **no** `ModelsPage`, **no** `LoginPage`. Unknown paths are not redirected to `/overview` (A26 default still unimplemented).

### 3.2 `DashboardLayout.tsx`

| Metric | Value |
|---|---|
| Path | `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` |
| Bytes | **1854** |
| Physical lines | **44** |
| SHA-256 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` |
| Last write | `2026-08-18T13:20:38.3009629+05:30` |
| Newlines | **CRLF** |

Nav array (14 `NavLink`s), Live + Audit rows:

```text
{ to: '/live',  label: 'Live',  icon: '▶' }   // L13 — path yes, label ≠ §46 “Live Copy Portfolio”
{ to: '/audit', label: 'Audit', icon: '☰' }   // L18 — path yes, label exact
```

No sidebar entry for Models. No header strip (`REAL_COPY_EXECUTION_ENABLED`, `STOP_NEW_EXECUTION`, FIX QUOTE/TRADE, MT5 ingest).

---

## 4. `pages/` census (this pass)

`D:\Prop\apps\web\src\pages` — **15** files, alphabetical, same set as C08:

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

`LiveCopyPage.tsx` and `AuditPage.tsx` are the newest pair (both written 13:20:38). The other 13 predate them (13:16:00–13:16:43). No `ModelsPage.tsx`. No `LoginPage.tsx`. No `pages/` under `D:\Prop\src`.

---

## 5. What is still not complete

B22 listed four spec pages as never created. Status **now**:

| Spec item | A26 route | File | Route | Sidebar | Hook | API | Now |
|---|---|---|---|---|---|---|---|
| Live Copy Portfolio | `/live` | **yes** | **yes** | yes (`Live`) | **no** | **no** | chrome closed; contract open |
| Audit | `/audit` | **yes** | **yes** | yes (`Audit`) | **no** | **no** | chrome closed; contract open |
| Models | `/models` | **no** | **no** | **no** | **no** | **no** | **still MISSING** (A52/A104 Phase 6 hold) |
| Login | `/login` | **no** | **no** | n/a (not §46) | **no** | **no** | **still MISSING** |

`hooks.ts` SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` (1935 B). Exports: `useOverview`, `useBrokers`, `useGroups`, `useTraders`, `useTraderDetail`, `useTrades`, `useFixSessions`, `useRiskStatus`, `useReconciliation`, `useHealth`, `useSettings`. **No** `useLive`, `useAudit`, `useModels`.

`apps/api/Program.cs` maps unversioned `GET /api/overview|brokers|groups|traders|fix/sessions|risk|trades|health|settings|reconciliation/status`. **No** `/api/v1/live/portfolio`. **No** `/api/v1/audit`. A63 first-useful includes audit GET; live portfolio is out of v1. A100/A101 still fail — the stub’s “stay empty until go-live gates pass” is the honest product state, not a finished book.

A26 §5.3 still fails: left-nav labels must be **exactly** §46. Current abbreviated set: Groups, Trades, Shadow, **Live**, FIX, Recon, Health.

---

## 6. Stale-report table

| Prior claim | This pass |
|---|---|
| A62 `pages/` EMPTY / 0 files | **FALSE** — 15 files |
| B10 / B22 Live Copy **MISSING** (no file, no route) | **STALE** as of 13:20:38 — file + `/live` exist |
| B10 / B22 Audit **MISSING** (no file, no route) | **STALE** as of 13:20:38 — file + `/audit` exist |
| C08 15/15 import match, Live+Audit listed | **STILL TRUE** — hashes unchanged |
| B31 “Models missing; Live/Audit chrome present, contract missing” | **STILL TRUE** |
| “Nav complete vs §46” | **FALSE** — Models leaf missing; labels abbreviated |

Do **not** delete B10/B22. They are earlier snapshots. Use this file (or C08/B31) for Live/Audit **file existence**.

---

## 7. Do / do-not

**Do**

- Treat `LiveCopyPage.tsx` and `AuditPage.tsx` as existing modules.
- Keep `/live` and `/audit` as the A26 paths.
- Keep `REAL_COPY_EXECUTION_ENABLED=false` until A100 + A101 PASS.

**Do not**

- Recreate either page file.
- Hand-write a live book or a fake audit log.
- Enable live FIX / real copy from this finding.
- Claim dashboard nav is §46-complete.
- Look for these files under `D:\Prop\src`.

This report does **not** authorize creating or rewriting product files.

---

## 8. One-line close

**`LiveCopyPage` and `AuditPage` exist at `D:\Prop\apps\web\src\pages\`, are default-exported, imported by `App.tsx`, routed at `/live` and `/audit`, and linked in the sidebar. They are 8-line stubs. Models + Login + A26 APIs remain absent.**
