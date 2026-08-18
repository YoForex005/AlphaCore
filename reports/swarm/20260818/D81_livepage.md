# D81 — `LiveCopyPage.tsx` (measured stub, not the §46 book)

| Field | Value |
|---|---|
| Agent | D81 (senior engineer, `LiveCopyPage.tsx` only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:03+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `LiveCopyPage.tsx`. Write this file. **Do not modify product source.** |
| Subject | `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` |
| Adjacent (read, not edited) | `App.tsx`, `DashboardLayout.tsx`, `hooks.ts`, `client.ts`, `signalr.ts`, `types/index.ts`, `ShadowPortfolioPage.tsx`, `OverviewPage.tsx`, `RiskPage.tsx`, `SettingsPage.tsx`, `AuditPage.tsx`, `TraderDetailPage.tsx`, `apps/api/Program.cs`, `SettingsController.cs`, `DashboardModels.cs`, `EfDashboardQueries.cs`, `CTraderFixOptions.cs` |
| Product source modified | **No.** This report (plus catalog pins in `INDEX.md` / `SWARM_LOG.md`) is the only write. |
| Method | Full `read_file` of the 8-line module + router/nav/hooks/types/API maps. PowerShell `Get-FileHash SHA256` + byte / physical-line / last-write. `git status` / `ls-files`. Grep `useLive`, `live/portfolio`, `LiveCopyPortfolio`, `GetLivePortfolio`, `realCopyExecutionEnabled`, `destination_positions` in product `*.tsx`/`*.ts`/`*.cs`. Cross-check architecture §46, A26 §5.2 / §6.10 / §9, A30 increment 6, A62, A63 §7.2, C37, C53, D08, D38, D39. **No** `npm`, `tsc`, `vite`, or HTTP launch. |
| Precedence | On-disk `LiveCopyPage.tsx` SHA `F85CF339…` is the page. Architecture **§46 label** wins for nav text. A26 path + JSON win for the book. A63 parks `GET /api/v1/live/portfolio` (out of §69). A30 forbids a **working** live book. C37 remains the §46 “missing?” pin; this file re-measures the module after later API/web deltas. |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **page-module census**. It is **not** a go-live PASS, a §69 PASS, or permission to enable `NewOrderSingle`.

---

## 0. Verdict

**`LiveCopyPage.tsx` is an untracked 321-byte static stub. `/live` chrome exists. The A26 §6.10 Live Copy Portfolio book does not.**

The module is **byte-identical** to C37 (13:20:38). Adjacent surfaces moved (trader-detail DTO, unused chart widgets, a dead `SettingsController` with a different flag name). None of those movements touched this file, added a hook, or mapped `/live/portfolio`.

| Layer | Required | Measured | §73.B |
|---|---|---|---|
| File | a `/live` page module | `pages/LiveCopyPage.tsx` (321 B, 8 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| A62 path | `pages/live/LiveCopyPortfolioPage.tsx` | flat `pages/LiveCopyPage.tsx`; no `pages/live/` | **WRONG** name / folder |
| Route | `/live` | `App.tsx` L32 `path="live"` | **PRESENT** |
| §46 nav label | `Live Copy Portfolio` | sidebar **`Live`** | **WRONG** |
| H1 | §46 string (or honest empty-safe title) | `Live copy portfolio` (sentence case) | **WRONG** vs §46; not a book |
| Hook | `useLive` → `GET /api/v1/live/portfolio` | **none** (page has **0** imports) | **MISSING** |
| API | A26 `GET /api/v1/live/portfolio` | **no map** (A63: out of v1 — allowed for §69) | **MISSING** (aligned with A63) |
| DTO | `realCopyExecutionEnabled`, `pnl`, `openCount`, `positions[]` | no type, no `GetLivePortfolioAsync` | **MISSING** |
| Flag on page | from API | JSX **literal** “is false” | **FAIL** |
| Empty-safe table | flag + `positions: []` | two sentences, no table | **FAIL** |
| Mutations on `/live` | **none** (A26 §9) | none | **ALIGNED** |
| Fake dest rows | forbidden | none | **ALIGNED** |
| Live `NewOrderSingle` | off until §68/§70 | off (`SAFE_BY_ABSENCE`) | **ALIGNED** |

**One-line:** File + route + abbreviated nav = chrome. Eight lines of static amber text = not the page.

**Direct answers**

| Question | Answer |
|---|---|
| Does `LiveCopyPage.tsx` exist? | **Yes.** `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`. Not under `D:\Prop\src`. |
| Is it in git HEAD `398a142`? | **No.** `??` untracked. A clean checkout does **not** have this file. |
| Has the file changed since C37 / C53 / D08? | **No.** Same SHA `F85CF339…`, same 321 B / 8 lines / 13:20:38. |
| Does it fetch anything? | **No.** Zero imports. Zero hooks. Zero `client.get`. |
| Is the §46 / A26 Live Copy Portfolio implemented? | **No.** |
| Must §69 ship a working live book? | **No.** A63 parks the GET. Empty-safe chrome is still the A26/A62 read story. |
| Did this pass change product source? | **No.** |

---

## 1. Entire module (verbatim)

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`

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

| | |
|---|---|
| Bytes | **321** |
| Physical lines | **8** |
| Non-blank lines | **8** |
| Last write (local) | 2026-08-18T13:20:38 |
| SHA-256 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` |
| Git | **`??` untracked** — `git ls-files` does not know the path |
| Default export | `LiveCopyPage` (matches `App.tsx` binding) |
| Imports | **0** |
| Hooks | **0** |
| JSX nodes | 1 wrapper `div`, 1 `h1`, 1 `p` |
| Tailwind | `space-y-3`, `text-2xl font-semibold text-white`, `text-amber-300 text-sm` (C48: content glob covers this file) |
| Buttons / forms / tables | **0** |
| `useQuery` / `useMutation` / `useParams` | **0** |
| SignalR `onEvent` | **0** |
| New widgets (`PnlChart`, `QuoteDisplay`, `ScoreGauge`) | **0** imports — those files exist (13:37) and this page does not use them |

Honesty of the *text* is directionally right (execution is off; the book should stay empty). Honesty of the *implementation* is not: the flag is a string literal, not a read of options / settings / overview DTO.

---

## 2. Router, nav, git

| Path | Bytes | Phys. lines | SHA-256 | Last write | Git vs HEAD `398a142` |
|---|---:|---:|---|---|---|
| `apps/web/src/pages/LiveCopyPage.tsx` | 321 | 8 | `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82` | 13:20:38 | **`??` untracked** |
| `apps/web/src/App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 13:20:38 | **`M` unstaged** (adds Live + Audit) |
| `apps/web/src/layouts/DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 13:20:38 | **`M` unstaged** (adds Live + Audit links) |
| `apps/web/src/api/hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 13:16:00 | **`M`** — still **no** live hook |
| `apps/web/src/api/client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 13:08:06 | unused by this page |
| `apps/web/src/api/signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 13:08:02 | layout starts `/hubs/dashboard`; this page never `onEvent` |
| `apps/web/src/types/index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 13:08:18 | unused barrel; `TraderDetail.livePositions` is client fiction |

`App.tsx` L11 / L32:

```tsx
import LiveCopyPage from './pages/LiveCopyPage';
// …
<Route path="live" element={<LiveCopyPage />} />
```

`DashboardLayout.tsx` L13 (nav item 8 of 14, after Shadow, before FIX — order **PASS**):

```text
{ to: '/live', label: 'Live', icon: '▶' }
```

A26 §5.3: left-nav labels **exactly** as §46 (`Live Copy Portfolio`). Measured label is **`Live`**. Header strip (`REAL_COPY`, `STOP_NEW`, MT5 / QUOTE / TRADE) is still **absent** from the layout.

No `pages/live/` directory. No `LiveCopyPortfolioPage.tsx`. No `routes/paths.ts`. Looking under `D:\Prop\src\apps\web\…` is a false miss (C53).

---

## 3. A26 / A62 widget checklist (this page only)

| A26 §6.10 / A62 widget | Present? |
|---|---|
| H1 (any) | yes — sentence case, not the §46 string |
| `realCopyExecutionEnabled` from API | **no** — string literal |
| `pnl` | **no** |
| `openCount` | **no** |
| Positions table (empty-safe) | **no** |
| `destinationPositionId` | **no** |
| `copyIntentId` | **no** |
| `executionIntentId` | **no** |
| `clOrdId` | **no** |
| `state` (dest fill) | **no** |
| `GET /api/v1/live/portfolio` | **no fetch** |
| Additional `GET /copy-intents` | **no** |
| Enable / flatten / send buttons | **no** (correct — A26 §9 mutations **none**) |
| Invented dest rows | **no** (correct) |
| Loading / error / empty states | **no** — always the same two sentences |

A26 §6.10 (binding JSON) requires, even while execution is off:

```json
{ "data": { "realCopyExecutionEnabled": false, "pnl": 0.00, "openCount": 0, "positions": [] } }
```

A26 L780: when `REAL_COPY_EXECUTION_ENABLED=false`, `positions` **may** be `[]`; the response **must still return the flag**. The page never asks.

A26 §9: primary GET `/live/portfolio`; additional `/copy-intents`; **mutations: none**. Enable-live and flatten live belong on Risk/Settings, not here.

A62: always show `realCopyExecutionEnabled`; positions may be `[]`. Target file: `pages/live/LiveCopyPortfolioPage.tsx`.

---

## 4. Hooks / types — still no live client

`hooks.ts` still exports exactly **11** `useQuery` wrappers. None is live.

| Hook | HTTP | Used by `LiveCopyPage`? |
|---|---|---|
| `useOverview` | `GET /api/overview` | no |
| `useBrokers` | `GET /api/brokers` | no |
| `useGroups` | `GET /api/groups` | no |
| `useTraders` | `GET /api/traders` | no |
| `useTraderDetail` | `GET /api/traders/:broker/:login` | no |
| `useTrades` | `GET /api/trades` | no |
| `useFixSessions` | `GET /api/fix/sessions` | no |
| `useRiskStatus` | `GET /api/risk` | no |
| `useReconciliation` | `GET /api/reconciliation/status` | no |
| `useHealth` | `GET /api/health` | no |
| `useSettings` | `GET /api/settings` | **no** — this is the only JSON that names `REAL_COPY_EXECUTION_ENABLED` |

**Hooks that do not exist:** `useLive`, `useLivePortfolio`, `useCopyIntents`.

`types/index.ts` has `TraderDetail.livePositions: Position[]` with `ticket` / `lots` / `currentPrice`. That is **not** the A26 live row (`destinationPositionId` + `clOrdId` + `copyIntentId`). Zero pages import the barrel. `TraderDetailPage` (now 2402 B, SHA `C849449B…`, last write 13:35:59) reads `header` + a first-3 trade table. It still does **not** read `livePositions`.

`client.ts` `baseURL` is `VITE_API_URL || 'http://localhost:5000'`. Irrelevant until a hook exists.

---

## 5. API / queries / persistence (still no live BFF)

### 5.1 `apps/api/Program.cs`

SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` · 4731 B · 95 lines · 13:35:15.

Changed since C37 (`E914FA98…` / 4658 B): trader-detail map now calls `GetTraderDetailAsync`. **Still zero** of:

- `/api/v1/live/portfolio`
- `/api/live/portfolio`
- `/api/v1/copy-intents`
- `/api/v1/settings/execution`

Fifteen maps remain (14 GET + `POST /api/ops/resync`). `GET /api/settings` is still the only JSON that names the flag:

```csharp
featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false }
```

Hard-coded `false`. `LiveCopyPage` does not call it. `SettingsPage` dumps the blob via `useSettings` and is **not** this leaf.

No `AddControllers()` / `MapControllers()`. Minimal hosting model only.

### 5.2 Dead `SettingsController` (new since C37 — do not confuse with this page)

`D:\Prop\apps\api\Controllers\SettingsController.cs` · 3732 B · 94 lines · 13:37:39 · SHA-256 `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F`.

- `[Route("api/settings")]` GET + PUT.
- Flag name is **`LiveCopyEnabled`** / Redis `settings:flags:live_copy`, **not** `REAL_COPY_EXECUTION_ENABLED`.
- PUT writes Redis keys if `IConnectionMultiplexer` resolves.
- **Not routed.** `Program.cs` never `AddControllers` / `MapControllers`. The live `GET /api/settings` is the anonymous `MapGet` in `Program.cs`, which **wins** if both were mapped (duplicate path).
- Web SDK will compile the controller. That is **not** a live endpoint today.
- If a later wave maps controllers without deleting this type, PUT becomes an **anonymous write** of a live-copy flag (A63 parks `PUT /api/v1/settings` as a secret-leak / enable vector). Out of this page’s scope; recorded so nobody treats it as the Live Copy book.

`LiveCopyPage` does not import or call this controller.

### 5.3 `IDashboardQueries`

`DashboardModels.cs` SHA-256 `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` · 3088 B · 114 lines · 13:34:59.

Eight methods: Overview, Brokers, Groups, Traders, Trader, **TraderDetail** (new vs C37), FIX, Risk. **No** `GetLivePortfolioAsync`. **No** `LivePortfolioDto`.

`OverviewDto` still has `int Live` + `bool RealCopyEnabled` + `decimal DestinationRealPnl`. `EfDashboardQueries.GetOverviewAsync` (SHA `328D0924…`, 8708 B, 13:35:15) sets:

- `Live` = count of `TraderState.LIVE` scores
- `DestinationRealPnl = 0` (literal)
- `XauGross = 0`, `XauNet = 0` (literals)
- `RealCopyEnabled = false` (literal, last ctor arg)

Overview paints **Live candidates**, not `data.live`. Dest real P&L tile is hardcoded 0. That is Overview, not this nav row.

`TraderRowDto` still has **no** `liveAllocation` (A92). Honest value while execution is off would be `0`.

### 5.4 Dest tables the page would read

Grep of product `*.cs` for `destination_positions` / `DestinationPosition` / `fix_orders`: **0** hits under `D:\Prop\src`. C37 / D19 still hold: `copy_intents` + `execution_intents` mapped; dest position / FIX order / link tables **MISSING**. A truthful `/live/portfolio` today would be `{ realCopyExecutionEnabled: false, pnl: 0, openCount: 0, positions: [] }` — and that GET is **not** implemented.

### 5.5 Send path (must stay off)

`CTraderFixOptions.RealCopyExecutionEnabled` default **false** (SHA `A354BBEA…`). Fix-worker only logs the flag. No `35=D` builder. Safety is **absence**, not a tested gate (A101). **Do not** enable send to populate this page.

---

## 6. Adjacent surfaces (same missing dest book)

Recorded so `/live` is not confused with other “live” strings.

| Surface | Architecture / A26 | Now (this pass) | Relation to this page |
|---|---|---|---|
| Sidebar leaf | §46 `Live Copy Portfolio` | **`Live`** → `/live` | chrome only |
| Shadow Portfolio | `/shadow` | 628 B static stub, SHA `608C8C2D…` | **not** this leaf |
| Overview “Live candidates” | §47 state count | painted from `data.liveCandidates` | selection state, not dest positions |
| Overview “Live copied” | §47 `live` | DTO field exists; **page never renders it** | still not this leaf |
| Overview dest real P&L | §47 | tile renders; query **hardcodes 0** | not this leaf |
| Overview `realCopyEnabled` | §41 | painted OFF from hardcoded `false` | closest honest flag; **not consumed by `LiveCopyPage`** |
| Risk “Real copy” | A26 risk | `data.realCopyEnabled ? ON : OFF` | same flag, other page |
| Settings JSON | A26 settings GET | `featureFlags.REAL_COPY_EXECUTION_ENABLED = false` | `LiveCopyPage` does not call `useSettings` |
| Trader Detail live book | §51 / A93 | header + first-3 table; no dest positions | page grew; book still missing |
| `types` `livePositions` | client fiction | unused | do not treat as A26 |
| Header strip | A26 §5.3 | **absent** | shell gap |
| SignalR `live.portfolio` | A97 not in v1 must-ship | hub itself **MISSING** (D50) | ok for v1 |
| New `PnlChart` / `QuoteDisplay` / `ScoreGauge` | none required here | on disk 13:37; **0** page imports from `LiveCopyPage` | unused chrome |
| `SettingsController.LiveCopyEnabled` | **not** the §41 flag | compiled, **unmapped** | different name; dead |

`/shadow` + Overview “Live candidates” do **not** satisfy this row.

---

## 7. Scorecard (this leaf only)

| Check | Required | Measured | Status |
|---|---|---|---|
| File exists | yes | yes | **PASS** |
| In git HEAD | expected for a shipped leaf | **untracked** | **FAIL** (lost on clean clone) |
| Sidebar leaf | yes | yes (`/live`) | **PASS** |
| Exact §46 label | `Live Copy Portfolio` | `Live` | **FAIL** |
| Order | after Shadow, before cTrader FIX | yes | **PASS** |
| Route | `/live` | `/live` | **PASS** |
| A62 module path | `pages/live/LiveCopyPortfolioPage.tsx` | `pages/LiveCopyPage.tsx` | **FAIL** |
| Empty-safe table | flag + `positions[]` | two sentences | **FAIL** |
| Flag from API | A26 field | JSX literal | **FAIL** |
| Versioned GET | `/api/v1/live/portfolio` | none | **FAIL** (A63-aligned) |
| `/copy-intents` | additional read | none | **FAIL** |
| Page mutations | none | none | **PASS** |
| Fake dest rows | forbidden | none | **PASS** |
| Header five chips | A26 §5.3 | none | **FAIL** (shell) |
| First-useful API | A63 **out** | no GET | **ALIGNED** |
| Working live book | A30 / §70 **not in v1** | no book | **ALIGNED** |
| `NewOrderSingle` | off | off | **ALIGNED** |

Chrome **3/5** (file, route, order; fail label + git). Contract **0/6** (module path, GET, DTO, table, flag-from-API, copy-intents). Honesty **2/2** (no fake rows, no page mutations).

§73.B for the **module**: `EXISTS_NEEDS_REFACTOR`.  
§73.B for the **A26 book**: `MISSING`.  
§73.B for **live send**: `SAFE_BY_ABSENCE` (not a tested gate).

---

## 8. What changed vs C37 / D08 (so this is not a copy-paste)

| Claim | C37 (13:22) / D08 (13:33) | D81 (13:43) |
|---|---|---|
| `LiveCopyPage.tsx` SHA / bytes / lines / write | `F85CF339…` / 321 / 8 / 13:20:38 | **unchanged** |
| `App.tsx` / layout SHAs | `A0E92C97…` / `48F7073E…` | **unchanged** |
| `hooks.ts` SHA | `5FDC969C…` | **unchanged** — still 11 hooks, no live |
| `Program.cs` | `E914FA98…` 4658 B | `61B1E0D1…` 4731 B — trader-detail only |
| `DashboardModels` | 7 methods, SHA `7A69C0E7…` | 8 methods (`GetTraderDetailAsync`), SHA `9A3888AE…` |
| `TraderDetailPage` | 1592 B chips only | 2402 B + first-3 table; **still no live book** |
| Chart widgets | 2 components (`MetricCard`, unused `StatusBadge`) | + `PnlChart` / `QuoteDisplay` / `ScoreGauge` (13:37); **unused here** |
| `SettingsController` | not on disk | 13:37:39, unmapped, **`LiveCopyEnabled` ≠ §41 flag** |
| `GET /api/v1/live/portfolio` | missing | **still missing** |
| Dest position entity | missing | **still missing** |

C37’s verdict on the **page** still holds. Use **this file** for the 13:43 re-measure (git untracked + dead settings controller + unused widgets). Use C37 for the original §46 “missing?” framing.

---

## 9. What a later wave must not do

1. Do **not** treat `LiveCopyPage.tsx` as absent. Do not create a second `pages/live/LiveCopyPortfolioPage.tsx` beside it without a single-module coding task.
2. Do **not** treat `/shadow`, Overview “Live candidates”, or Risk “Real copy” as this leaf.
3. Do **not** invent dest positions, `clOrdId`s, or non-zero dest P&L while execution is off.
4. Do **not** enable `REAL_COPY_EXECUTION_ENABLED` or emit `NewOrderSingle` to populate a table.
5. Do **not** add page-level flatten / enable-live buttons (A26: mutations **none** on `/live`).
6. Do **not** ship `/api/v1/live/portfolio` as a §69 first-useful requirement. If a later wave adds it, empty-safe `{ realCopyExecutionEnabled: false, pnl: 0, openCount: 0, positions: [] }` is the only honest body.
7. Do **not** leave the sidebar as `Live` if the task is “§46 exact nav.”
8. Do **not** add a second frontend or a `pages/` tree under `D:\Prop\src`.
9. Do **not** copy source-MT5 P&L into dest / live fields (A91).
10. Do **not** wire `SettingsController` as the live-copy switch. Flag name is wrong; PUT would be anonymous; A63 parks settings writes.
11. Do **not** assume a clean clone has this page — it is **untracked**.
12. This report does **not** authorize product edits.

---

## 10. Copy-out answers

### What is `LiveCopyPage.tsx`?

An 8-line default-export React function. Title “Live copy portfolio”. Amber sentence that `REAL_COPY_EXECUTION_ENABLED is false` and the page will stay empty until go-live gates pass. No data.

### Is Live Copy Portfolio done?

**No.** Chrome yes. Book no.

| | Nav leaf | Exact §46 label | Route | Page module | Data contract |
|---|---|---|---|---|---|
| Live Copy Portfolio | Present as **`Live`** | **No** | `/live` present | 321 B / 8-line stub, **untracked** | **Missing** `GET /api/v1/live/portfolio` |

### Did this pass change product source?

**No.**

---

## Evidence pins

- Subject: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` — 321 B, 8 lines, SHA-256 `F85CF339AAD7B2A9F639DA83466DC7949EF765EB3321C985044044978010BC82`, 13:20:38, **untracked**.
- Router: `D:\Prop\apps\web\src\App.tsx` L11, L32. SHA-256 `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` L13 `{ to: '/live', label: 'Live' }`. SHA-256 `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21`.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 queries, none live. SHA-256 `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20`.
- Types: `D:\Prop\apps\web\src\types\index.ts` L47 `livePositions` unused. SHA-256 `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081`.
- API: `D:\Prop\apps\api\Program.cs` — 15 maps; no `/live*`. SHA-256 `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E`.
- Dead settings type: `D:\Prop\apps\api\Controllers\SettingsController.cs` SHA-256 `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F`.
- Queries: `D:\Prop\src\Application\Dashboard\DashboardModels.cs` SHA-256 `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496`.
- Materializer: `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L42 `false` for `RealCopyEnabled`; dest P&L `0`. SHA-256 `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60`.
- Flag default: `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35 `= false`. SHA-256 `A354BBEA4665EE217A46B7536BECACE8F73BB1DB3693A195B8C66FF716753308`.
- Architecture §46: `D:\Prop\MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` L1735–1758.
- A26 §6.10 / §9: `D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md` L750–780, L1210.
- A63 out of v1: `D:\Prop\reports\swarm\20260818\A63_api_catalog.md` L1013.
- Prior: C37 (page missing, chrome not), C53 (file exists), D08 (census), D38 (routes), D39 (hooks). This file **supersedes C37 only on measured-at + git untracked + SettingsController + unused widgets**. C37 still wins for the original §46 question framing.

---

## One-line close

**`LiveCopyPage.tsx` is an untracked 321-byte / 8-line static stub (SHA `F85CF339…`, unchanged since 13:20:38): `/live` + sidebar `Live` exist; no hook, no `/api/v1/live/portfolio`, no dest book; product source not touched.**
