# D84 — `ReconciliationPage.tsx` vs architecture §54

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D84_reconpage.md` |
| Agent | D84 (senior engineer, Reconciliation page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:40+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `ReconciliationPage.tsx`. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\ReconciliationPage.tsx` |
| Adjacent (read, not edited) | `hooks.ts`, `client.ts`, `types/index.ts`, `App.tsx`, `DashboardLayout.tsx`, `main.tsx`, `apps/api/Program.cs`, `DashboardModels.cs`, `ReconciliationIssueType.cs` |
| Product source modified | **No.** This report (plus catalog pins in `INDEX.md` / `SWARM_LOG.md`) is the only write. |
| Test source modified | **No** |
| Method | Full `read_file` of the 12-line page + hook + host map + unused TS stub + dashboard port. PowerShell `Get-FileHash SHA256` + byte / physical-line / last-write / `git status`. Cross-check architecture §46 / §54 / §70.14; A26 §6.13; A47; A62 §10.8; A63; A96; C08 / C13 / D08 / D38 / D39. **API process was not launched.** No HTTP capture. |
| Precedence | Architecture **§54 widgets** win. A26 path + JSON win. A96 is the allow-list DTO. A63 decides first-useful honesty (`cTrader` may be `NEVER`; zeros must not look healthy). On-disk worktree supersedes A62/A96/A100 “no page file.” Git HEAD still has **no** `pages/` tree. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **page close-read**. It is **not** a claim that the comparer, dest book, or `/api/v1/reconciliation` exist. It is **not** a go-live PASS.

---

## 0. Verdict

**The `/reconciliation` chrome exists. The architecture §54 Reconciliation dashboard does not.**

`ReconciliationPage.tsx` is a 12-line title + one honest sentence + `<pre>{JSON.stringify(data, null, 2)}</pre>`. The hook hits the demo map `GET /api/reconciliation/status`, which returns **`UtcNow` plus three zeros** on every request. That payload is **not** A26 / A96. Painted zeros with a “just now” clock look like a clean successful reconcile. They are a stub.

| Layer | Required | Now | §73.B |
|---|---|---|---|
| §46 nav label | `Reconciliation` | sidebar **`Recon`** | **WRONG** (label) |
| A26 route | `/reconciliation` | `/reconciliation` | **PRESENT** |
| Page module | A62 `pages/reconciliation/ReconciliationPage.tsx` | `pages/ReconciliationPage.tsx` (490 B, 12 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Git | committed page | **`?? apps/web/src/pages/`** (entire folder untracked) | worktree-only |
| Hook | `useReconciliation` → `GET /api/v1/reconciliation` | `GET /api/reconciliation/status` | **DEPRECATED** as contract |
| Host map | A26 snapshot + runs + issues | hardcoded anonymous object | **UNSAFE** (clean-book lie) |
| DTO | A96 `ReconciliationPageDto` (two clocks, nine counts, issues, honesty) | `{ lastReconciliation, unknownPositions, mismatches, orphanFills }` | **MISSING** |
| `IDashboardQueries.GetReconciliationAsync` | required | **0** recon methods on the port | **MISSING** |
| §54 widgets painted | 8 named tiles (2 clocks + 6 families) | **0** tiles; raw JSON | **MISSING** |
| A96 nine issue keys always visible | 9 tiles even at 0 | 3 collapsed ints | **MISSING** |
| cTrader `NEVER` banner | required while Phase 7 not started | **absent** | **MISSING** |
| Mutations (run / ack) | RiskManager+ | **0** | **MISSING** (allowed until Phase 7 for cTrader; MT5 run still specified) |
| RBAC / envelope / SignalR | A26 §2 / A97 `reconciliation.issue` | anonymous GET; no hub | **MISSING** |

**One-line:** File + route exist; operator sees a JSON dump of invented zeros; §54 is not implemented.

**Direct answers**

| If the question means… | Answer |
|---|---|
| Is there no file / no route / no sidebar entry? | **No.** Worktree has file + `/reconciliation` + abbreviated **Recon**. |
| Is the architecture §54 / A26 / A96 page implemented? | **No — missing.** |
| Can first-useful (§69 / A63) ship this JSON dump? | **No.** A63 requires an MT5 tile and an honest `NEVER` cTrader tile. Zeros + `UtcNow` fail that bar. |
| Does a clean checkout of HEAD `398a142` contain this file? | **No.** `apps/web/src/pages/` is entirely untracked. HEAD `App.tsx` already imports `./pages/ReconciliationPage`. |

This report does **not** authorize creating, moving, or rewriting product files.

---

## 1. Binding law

| Rank | Source | Owns |
|---|---|---|
| 1 | Architecture **§54** (`…Architecture_v2.md` L1982–1998) | Two clocks + six issue families; **“Nothing unresolved should be silently ignored.”** |
| 1b | Architecture **§46** L1754 | Exact nav label **`Reconciliation`** |
| 1c | Architecture **§70.14** / §12 / §42–43 | Inconsistent book blocks new execution; MT5 clock ≠ cTrader clock |
| 2 | A26 §5.2, §6.13, §9 | Path `/reconciliation`; `GET /api/v1/reconciliation`; runs/issues/ack/run; camelCase; envelope `{ data }` |
| 3 | A96 | Allow-list records, nine issue tokens, honesty flags, first-useful example 6.1 (`gate=NEVER`, `emptyIssuesMeanAllClear=false`) |
| 4 | A47 | Dest comparer / gate / `READY_FOR_EXECUTION`; ACK is not a bypass |
| 5 | A63 §5.10 / first-useful honesty | MT5 tile must exist; cTrader may be `NEVER`; do not paint healthy |
| 6 | A62 §10.8 | Page plan: two header tiles, nine always-visible counts, open-issue table, not-READY banner |

**Wrong / stale (do not implement from these):**

| Topic | Wrong | Binding |
|---|---|---|
| Hook path | `/api/reconciliation/status` | `GET /api/v1/reconciliation` |
| Stub type | `{ lastReconciliation, unknownPositions, mismatches, orphanFills }` | A96 `ReconciliationPage` |
| One clock | single `lastReconciliation` | **Two** clocks: `mt5` + `cTrader` |
| Empty list = healthy | omit zeros / hide tiles / return 0 with `UtcNow` | All nine types always present; `NEVER` ≠ `HEALTHY` |
| Nav | `"Recon"` | §46 **`Reconciliation`** |
| File path | A62 nested `pages/reconciliation/…` is a later-wave suggestion | Do not move this file from this report |
| A96/A100 “no page file” | measured against an empty `pages/` | **Superseded for worktree existence** by this file |

---

## 2. Measured files (this pass)

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\pages\ReconciliationPage.tsx` | 490 | 12 | `BC036D09A78AECBABD47A8DD9AC0B58E934C7DBDF51930B136545797BEFE8886` | 2026-08-18T13:16:43+05:30 | **`??` untracked** |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | `M` (unstaged) |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 | clean vs HEAD |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | clean vs HEAD |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | `M` vs HEAD (Live+Audit only) |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38+05:30 | `M` vs HEAD |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | `M` vs HEAD |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | `M` vs HEAD |
| `D:\Prop\src\Domain\Enums\ReconciliationIssueType.cs` | 286 | 12 | `11609961FD2FD8B3C978775BEB71F43E2C70F36802377E90C5C237513B20C914` | 2026-08-18T13:06:08+05:30 | clean vs HEAD |

HEAD: `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530).

Page SHA `BC036D09…` is **unchanged** vs B20 / B22 / C08 / D08. The file has not grown since 13:16:43. `git ls-files apps/web/src/pages` is **empty** — every page module, including this one, is worktree-only.

Non-blank lines on the page: **11**. Default export: `export default function ReconciliationPage`. Import graph: **one** import (`useReconciliation`). No `MetricCard`, no types import, no SignalR, no mutation.

---

## 3. Entire page source (authoritative)

`D:\Prop\apps\web\src\pages\ReconciliationPage.tsx` in full:

```tsx
import { useReconciliation } from '../api/hooks';

export default function ReconciliationPage() {
  const { data } = useReconciliation();
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-2">Reconciliation</h1>
      <p className="text-sm text-gray-400 mb-4">Unresolved venue differences block new execution.</p>
      <pre className="bg-gray-950 border border-gray-800 rounded p-4 text-sm text-gray-200">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
```

What the module actually does:

| Behavior | Measured |
|---|---|
| Heading | literal `Reconciliation` (matches §46; nav does not) |
| Subtitle | “Unresolved venue differences block new execution.” — correct **copy** of §70.14 / A47. Not wired to a gate. |
| Data | destructures `{ data }` only. Ignores `isLoading`, `isError`, `error`, `isFetching`, `status`. |
| Render | one `<pre>` of `JSON.stringify(data, null, 2)`. No tiles, table, banner, buttons. |
| Loading | `data === undefined` → `JSON.stringify(undefined, null, 2)` is JS `undefined` → React child is empty. Operator sees title + sentence + blank `<pre>`. Same as error. |
| Typing | untyped. Does **not** import `ReconciliationStatus` from `types/index.ts` (that stub is **0** importers). |
| Compare to `RiskPage` | Risk has a loading sentence + four `MetricCard`s. This page is the same class as `SystemHealthPage` / `SettingsPage` (JSON dump). |

The subtitle is the only architecture-true string on the page. The JSON that follows it contradicts the sentence.

---

## 4. Route and chrome

From worktree `App.tsx` L14 + L35:

```text
import ReconciliationPage from './pages/ReconciliationPage';
<Route path="reconciliation" element={<ReconciliationPage />} />
```

HEAD `App.tsx` already has the same import and route (Live + Audit are the only unstaged additions). There is no `path="*"` (D38). Unknown URLs do not redirect here.

From `DashboardLayout.tsx` L16:

```text
{ to: '/reconciliation', label: 'Recon', icon: '⟳' }
```

| Contract | Disk |
|---|---|
| A26 path `/reconciliation` | **match** |
| §46 label `Reconciliation` | **`Recon`** — abbreviated, same class as Live / FIX / Health (D38: 7 exact / 7 abbreviated) |
| A26 §5.3 header strip (clocks, kill switch, role) | **absent** — layout is sidebar + `<Outlet />` |
| Auth wrapper | **absent** (D53: entire API anonymous) |

---

## 5. Hook and host payload

`hooks.ts` L43–45 (SHA `5FDC969C…`, unchanged vs D08 / D39):

```ts
export function useReconciliation() {
  return useQuery({ queryKey: ['reconciliation'], queryFn: () => client.get('/api/reconciliation/status').then(r => r.data) });
}
```

| Hook property | Measured |
|---|---|
| Query key | `['reconciliation']` — name matches A26/A63 key list; **path does not** |
| HTTP | `GET /api/reconciliation/status` |
| A26 / A63 | `GET /api/v1/reconciliation` — **0** `/api/v1` in any hook (D39) |
| Envelope unwrap | `r.data` is axios body. Host returns a **bare** object, not `{ data: … }` |
| `refetchInterval` | **none** (FIX/risk poll 5 s; health 10 s; recon is `staleTime` 30 s only from `main.tsx`) |
| `useQuery<T>` | **none** |
| Mutations | **0** (`useMutation` does not exist in `hooks.ts`) |

Axios `client.ts`: `baseURL = import.meta.env.VITE_API_URL || 'http://localhost:5000'`, timeout 15 s. Vite has **no** `/api` proxy (D38).

Host `Program.cs` L35–41 (SHA `61B1E0D1…`):

```csharp
app.MapGet("/api/reconciliation/status", () => Results.Ok(new
{
    lastReconciliation = DateTimeOffset.UtcNow,
    unknownPositions = 0,
    mismatches = 0,
    orphanFills = 0
}));
```

| Field on the wire | Source | Honesty |
|---|---|---|
| `lastReconciliation` | `DateTimeOffset.UtcNow` **per request** | **LIE.** Not a checkpoint. Not a run. Looks like “reconciled just now.” |
| `unknownPositions` | literal `0` | **LIE** if read as “we compared and found none.” No dest book, no comparer. |
| `mismatches` | literal `0` | Collapses ORDER + QUANTITY + SIDE. Always zero. |
| `orphanFills` | literal `0` | Collapses ORPHAN_FILL + ORPHAN_EXECUTION_REPORT. Always zero. |
| `mt5` / `cTrader` tiles | **absent** | One clock. §54 requires two. |
| `openIssueCounts` nine keys | **absent** | |
| `issues[]` | **absent** | Silent drop of the preview list A26 includes. |
| `honesty` / `alerts` | **absent** | First-useful 6.1 requires `CTRADER_NEVER` + `MT5_NEVER`. |
| `readyForExecution` | **absent** | |
| DB / `IDashboardQueries` | **not called** | Unlike `/api/risk` / `/api/overview`. Pure hardcoded handler. |

Unused stub in `types/index.ts` L109–114 (0 imports):

```ts
export interface ReconciliationStatus {
  lastReconciliation: string;
  unknownPositions: number;
  mismatches: number;
  orphanFills: number;
}
```

That interface matches the **lie**, not A96. Replacing the page with a typed dump of the same four fields would still fail §54.

---

## 6. Architecture §54 widget matrix

Quoted §54 (L1986–1998):

```text
Last successful MT5 reconciliation
Last successful cTrader reconciliation

Unknown external positions
Missing internal positions
Order mismatches
Quantity mismatches
Orphan fills
Unresolved execution states
```

Quoted: **“Nothing unresolved should be silently ignored.”**

| # | §54 widget | Painted as a widget? | Buried in JSON? | Honest? |
|---|---|---|---|---|
| 1 | Last successful **MT5** reconciliation | **No** | `lastReconciliation = UtcNow` (wrong clock name, wrong value) | **No** |
| 2 | Last successful **cTrader** reconciliation | **No** | **No field** | **No** — Phase 7 not started must show `NEVER`, not hide |
| 3 | Unknown external positions | **No** | `unknownPositions: 0` | **No** |
| 4 | Missing internal positions | **No** | **No field** | Silent omission |
| 5 | Order mismatches | **No** | folded into `mismatches: 0` | Silent merge |
| 6 | Quantity mismatches | **No** | folded into `mismatches: 0` | Silent merge |
| 7 | Orphan fills | **No** | `orphanFills: 0` | **No** |
| 8 | Unresolved execution states | **No** | **No field** | Silent omission |

**Score: 0 / 8 widgets.** A62 / A96 extra tokens (`SIDE_MISMATCH`, `ORPHAN_EXECUTION_REPORT`, `UNEXPECTED_FILL`) are also absent as tiles. Always-visible-at-zero is **not** implemented — the page does not even have count tiles.

“Silently ignored” applies twice: (1) issue families with no field; (2) a comparer that never ran, presented as zeros.

---

## 7. A96 snapshot field matrix

A96 `ReconciliationPageDto` / TS `ReconciliationPage` vs what this page can show:

| A96 field | On stub wire? | Page reads it? |
|---|---|---|
| `generatedAt` | no | no |
| `mt5` (`lastSuccessfulAt`, `status`, checkpoints, per-broker rows) | no | no |
| `cTrader` (`gate`, `health`, `readyForExecution`, mass-status / pos counts) | no | no |
| `book` (internal vs venue working/open) | no | no |
| `openIssueCounts` (nine SCREAMING_SNAKE keys) | no | no |
| `issues[]` + `issuesTruncated` | no | no |
| `totalOpenIssues` / `totalAcknowledgedIssues` | no | no |
| `alerts[]` (`CTRADER_NEVER`, `MT5_NEVER`, …) | no | no |
| `honesty.emptyIssuesMeanAllClear` | no | no |
| `honesty.readyForExecution` | no | no |

**Consumed A96 fields: 0.** First-useful example 6.1 (`mt5.status=UNKNOWN`, `cTrader.gate=NEVER`, two WARNING alerts, `emptyIssuesMeanAllClear=false`) is **not** what the operator would see even if the API were honest — the page cannot render those keys as widgets.

A96 listed C# file `src/Application/Dashboard/ReconciliationDtos.cs`: **not on disk.** `IDashboardQueries` (current 8 methods after `GetTraderDetailAsync`) still has **0** `GetReconciliation*`. Grep of `src/` + `apps/` `*.cs` for `GetReconciliation` / `ExecutionReconciliation` / `execution_reconciliation`: **0**.

Domain `ReconciliationIssueType` (SHA `11609961…`) still has **7** values and is still missing `OrderMismatch` and `OrphanFill` (A96 §3.2). That domain gap is real; it does **not** excuse dropping those tiles from the page DTO.

A96 §15 contract tests (`ReconciliationPageContractTests`, `ReconciliationHonestyTests`, `NoSecretsInReconDtoTests`, …): **0** files under `tests/`.

---

## 8. Endpoints this page does not call

| A26 / A96 | On host? | Hook? | Page? |
|---|---|---|---|
| `GET /api/v1/reconciliation` | **no** | no | no |
| `GET /api/v1/reconciliation/runs` | **no** | no | no |
| `GET /api/v1/reconciliation/issues` | **no** | no | no |
| `POST /api/v1/reconciliation/run` | **no** | no | no |
| `POST /api/v1/reconciliation/issues/{id}/ack` | **no** | no | no |
| `POST /api/v1/reconciliation/issues/{id}/accept-external` | **no** (Phase 7 → 404 is honest) | no | no |
| SignalR `reconciliation.issue` on `/hubs/ops` | hub **not mapped** (D50) | layout dials `/hubs/dashboard` and swallows failure | page does not subscribe |

Demo coincidence: `GET /api/reconciliation/status` **does** exist and the hook **does** hit it (D39 11/11 demo paths). That is **not** catalog compliance.

---

## 9. Honesty analysis (the actual risk)

The subtitle says unresolved differences block execution. The body then dumps a payload that can only be read as “reconciled now, nothing open.”

| Operator inference from the dump | Truth |
|---|---|
| Reconcile ran at `lastReconciliation` | Handler stamped request time. No run table. No checkpoint read. |
| Book is clean (`0` / `0` / `0`) | Comparer **MISSING**. Dest tables **MISSING**. Source `system_events` **MISSING**. |
| cTrader is fine (no second clock) | Phase 7 **not started**. Required state is `NEVER` / not `HEALTHY` (A96). |
| Missing-internal / unresolved-state are zero | Fields omitted. Zero is not computed. |
| Execution may proceed | `REAL_COPY` is still off (SAFE_BY_ABSENCE, C07). Gate is not on this page. Live NOS remains blocked **elsewhere**, not because this page said so. |

C13 already scored this row: `GET /api/reconciliation/status` — **static zeros, not DB**. This pass reconfirms. The lie is **worse** than a blank stub (`LiveCopyPage` / `AuditPage`): those pages do not invent a successful clock.

`JSON.stringify` of a four-field stub also cannot redact secrets. Today the stub has none. When a later wave attaches real tickets / notes, a dump is the wrong surface (A96 denylist / A76). Not a current leak.

---

## 10. Loading, error, RBAC, polling

| Concern | Disk |
|---|---|
| Loading UI | **none** (blank `<pre>`) |
| Error UI | **none** (same blank `<pre>`; Query retry=2 then silent) |
| Empty-vs-never banner | **none** |
| Not-READY / 412 banner (A62 §10.8) | **none** |
| Role-gated run/ack buttons | **none** (correct absence of a working dest run; incorrect absence of an honest MT5 “never” + disabled run) |
| Auth | page is reachable anonymously; API CORS `AllowAnyOrigin` |
| Live refresh | no interval; no hub invalidation; 30 s staleTime |
| Tailwind | utility classes only; C48 listed this file as “Yes” Tailwind — true, and irrelevant to §54 |

---

## 11. What earlier reports claimed vs this pass

| Prior claim | This pass |
|---|---|
| A62 / A96: `pages/` empty; `App.tsx` imports a missing file | **STALE for worktree.** File exists (13:16:43). **Still true for git HEAD** — `pages/` untracked. |
| A100: “Dashboard `ReconciliationPage` import has **no page file**.” | **STALE for worktree; TRUE for a clean checkout of `398a142`.** |
| B10 / B20 / B22 / C08 / D08: 490 B, 12 lines, SHA `BC036D09…`, JSON dump | **UNCHANGED.** Same blob. |
| B31: nav label **WRONG** (`Recon`) | **UNCHANGED.** |
| D39: hook hits `/api/reconciliation/status`; host is hardcoded zeros + `UtcNow` | **UNCHANGED.** |
| A96 honesty pin “React page has 0 files” | **Superseded** by C08 / D08 / this file for existence. Contract pin still holds. |
| D21: `IDashboardQueries` is 7 methods, no recon | Port now has **8** methods (`GetTraderDetailAsync`, D39). Still **0** recon. |

Do **not** recreate the 12-line file. Do **not** treat SHA `BC036D09…` as a new page. Depth vs §54 is a later coding wave against A96 + A26, not a second stub.

---

## 12. Later-wave build list (not this task)

When a coding wave is authorized, A96 §16 is the sequence. For the **page** specifically:

1. Stop calling `/api/reconciliation/status`. Target `GET /api/v1/reconciliation` and unwrap `{ data }`.
2. Delete or stop using `ReconciliationStatus`. Use A96 TS types (`ReconciliationPage`, nine count keys).
3. Paint **two** clocks. First useful: `mt5.status=UNKNOWN` / `lastSuccessfulAt=null` until checkpoints exist; `cTrader.gate=NEVER` with `CTRADER_NEVER` alert. Never map `NEVER` → healthy.
4. Always show all nine count tiles, including zeros. Table of `issues[]`. Banner when `readyForExecution === false`.
5. `honesty.emptyIssuesMeanAllClear` drives copy: empty + `NEVER` is **not** all-clear.
6. Do not render `JSON.stringify` of the snapshot as the page.
7. Nav label `Reconciliation`. Do not add a working dest comparer to “finish” the page.
8. Contract tests in A96 §15 must exist before anyone marks the page done.

The host must stop returning `UtcNow` + zeros. That handler is **UNSAFE** even without a pretty UI.

---

## 13. What this pass did not do

- Did not launch the API or Vite. Wire shape is from `Program.cs` source, not a captured response.
- Did not edit `ReconciliationPage.tsx`, hooks, types, `Program.cs`, or any other product file.
- Did not add `ReconciliationDtos.cs`, tables, or tests.
- Did not re-score §69 / §68 / §70 (C13 / C14 / A101 / D43 still own those integers).
- Did not claim EX5 / MT5 / FIX live reconcile.

**§73.B for this file’s subject (`ReconciliationPage.tsx` + `/reconciliation` chrome): `EXISTS_NEEDS_REFACTOR`.**  
**§73.B for architecture §54 / A26 / A96 Reconciliation dashboard: `MISSING`.**  
**§73.B for `GET /api/reconciliation/status` as an operator signal: `UNSAFE`.**
