# P500_S051 — No UI button fires a live order

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S051_no_manual_fire.md` |
| Agent | P500_S051 (read-only web; no-manual-fire pin) |
| Slot | **S051** |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (source read of `apps/web`; no product edit; no `npm` / `vite` / HTTP launch) |
| Workspace | `D:\Prop` (Vite app is `D:\Prop\apps\web`, **not** under `D:\Prop\src`) |
| Assigned | Read `apps/web` `LiveCopyPage` and related hooks. Write this file. **There is no UI button that fires a live order. Good. Do not add one. Do not edit product.** |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| Secrets printed | **None.** |
| Binding law | A26 §9 (`/live` mutations: **none**); Architecture §41 / §70 (`REAL_COPY_EXECUTION_ENABLED` off; no `NewOrderSingle` until gates); A30 increment 6 (no working live book); A63 parks `GET /api/v1/live/portfolio`; A101 item 12 (no live send) |
| Predecessors (independent re-read, not copied as verdict) | C37, D81, D39, E002, P500_S002, P500_S021, P500_S025, A26 §9 |
| Method | Full `read_file` of `LiveCopyPage.tsx`, `hooks.ts`, `client.ts`, `signalr.ts`, `App.tsx`, `DashboardLayout.tsx`, `types/index.ts`, `main.tsx`, and every other page under `apps/web/src/pages/`. Product-tree grep of `apps/web/src/**/*.{ts,tsx}` for `<button`, `type="submit"`, `useMutation`, `client.(post\|put\|patch\|delete)`, `useLive` / `usePlace` / `useSend` / `useFire` / `useFlatten` / `useEnable`, `NewOrderSingle`, `onClick`. **No product edit.** |

This is a **no-manual-fire census**. It is **not** a §68 / §69 / §70 PASS and does **not** authorize adding a Send / Flatten / Enable-live control.

---

## 0. Verdict (binding)

**CONFIRMED. There is no UI button that fires a live order. Do not add one.**

| Check | Result | Class |
|---|---|---|
| `LiveCopyPage` has a Send / Place / Fire / Flatten / Enable-live control | **No.** Entire module is H1 + one amber `<p>`. **0** imports, **0** hooks, **0** buttons, **0** forms. | **ALIGNED** (A26 §9) |
| Related hooks can POST an order | **No.** `hooks.ts` is **11** `useQuery` GETs. **0** `useMutation`. | **ALIGNED** |
| `client.ts` exposes a write helper | **No.** Axios instance only; no `post`/`put`/`patch`/`delete` wrapper. Pages never call `client.post`. | **ALIGNED** |
| Any `apps/web/src` `<button>` / `type="submit"` / `onClick` | **0 hits** | **ALIGNED** |
| Any `useMutation` / `useLive` / `usePlace` / `useSend` / `useFire` | **0 hits** | **ALIGNED** |
| SignalR can invoke a send | **No.** `on` / `off` only; no `invoke`. | **ALIGNED** |
| Clickable chrome that could be mistaken for fire | `NavLink`s + one `Link` to trader detail. Navigation only. | **NOT a send** |
| Product edited this slot | **No.** | report only |

**One-line:**

```text
/live is a static warning stub.
hooks.ts is GET-only.
apps/web/src has zero <button>, zero useMutation, zero client.post.
Do not add a live-order fire control.
```

---

## 1. `LiveCopyPage.tsx` (entire module)

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx`

```tsx
export default function LiveCopyPage() {
  return (
    <div className="space-y-3">
      <h1 className="text-2xl font-semibold text-white">Live copy portfolio</h1>
      <p className="text-amber-300 text-sm">Copy intents may be recorded as SHADOW only. Pepperstone/cTrader NewOrderSingle is disabled so this process cannot open a losing live position. Gates still required: FIX TRADE logon + recon + risk approve + REAL_COPY_EXECUTION_ENABLED.</p>
    </div>
  );
}
```

| Probe | Measured |
|---|---|
| Imports | **0** |
| Hooks | **0** |
| JSX | wrapper `div`, `h1`, `p` |
| `<button>` / `<form>` / `<input>` / `<select>` | **0** |
| `onClick` / `onSubmit` | **0** |
| `client.get` / `client.post` / `fetch` | **0** |
| `useQuery` / `useMutation` | **0** |
| SignalR | **0** |
| Enable / flatten / send / fire / place / copy-now | **0** (not even as English verbs except “NewOrderSingle is **disabled**”) |
| Dest book / positions table | **0** |

The page **cannot** fire a live order because it has no event handler, no HTTP client, and no control that a user can click except the shared sidebar (navigation).

C37 / D81 quoted an **older** body (`REAL_COPY_EXECUTION_ENABLED is false. This page will stay empty until go-live gates pass.`). That string is **gone**. Current amber copy (also measured by P500_S025) is the binding text. The **no-button** fact is unchanged.

---

## 2. Related hooks — GET only

`D:\Prop\apps\web\src\api\hooks.ts` — 11 exports, every one `useQuery` + `client.get`.

| Hook | HTTP | Method | Used by `LiveCopyPage`? |
|---|---|---|---|
| `useOverview` | `/api/overview` | GET | no |
| `useBrokers` | `/api/brokers` | GET | no |
| `useGroups` | `/api/groups` | GET | no |
| `useIngestStatus` | `/api/ingest/status` | GET | no |
| `useTraders` | `/api/traders` | GET | no |
| `useTraderDetail` | `/api/traders/:broker/:login` | GET | no |
| `useTrades` | `/api/trades` | GET | no |
| `useFixSessions` | `/api/fix/sessions` | GET | no |
| `useRiskStatus` | `/api/risk` | GET | no |
| `useReconciliation` | `/api/reconciliation/status` | GET | no |
| `useHealth` | `/api/health` | GET | no |
| `useSettings` | `/api/settings` | GET | no |

**Hooks that do not exist (and must not be added for a fire button):**

- `useLive` / `useLivePortfolio`
- `useCopyIntents`
- `usePlaceOrder` / `useSendOrder` / `useFireOrder`
- `useFlatten` / `useEnableLive` / `useToggleRealCopy`

`useMutation` is **not imported**. TanStack Query is used as a **read cache** only (`main.tsx` configures `queries`, not `mutations`).

---

## 3. Client + SignalR (no write path)

### 3.1 `D:\Prop\apps\web\src\api\client.ts`

```ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 60000,
  headers: { 'Content-Type': 'application/json' },
});

export default client;
```

Axios *could* `post` if a caller used it. **No caller does.** Grep of `apps/web/src` for `client.post` / `client.put` / `client.patch` / `client.delete` / `axios.post` = **0** (the only `axios` hit is `axios.create`).

### 3.2 `D:\Prop\apps\web\src\api\signalr.ts`

`HubConnectionBuilder` → `/hubs/dashboard`. Exports: `getConnection`, `startConnection`, `onEvent`.

- `on` / `off` only.
- **No** `connection.invoke(...)`.
- **No** send / place / flatten method name.

`DashboardLayout` starts the connection on mount. `LiveCopyPage` never registers a handler.

---

## 4. Whole `apps/web/src` census (so `/live` is not the only leaf)

Grep scope: `apps/web/src/**/*.{ts,tsx}`.

| Pattern | Hits | Meaning |
|---|---:|---|
| `<button` | **0** | no button element |
| `type="submit"` / `type='submit'` | **0** | no form submit |
| `onClick` | **0** | no click handler |
| `useMutation` | **0** | no write hook |
| `client.post` / `.put` / `.patch` / `.delete` | **0** | no REST write |
| `fetch(` | **0** | no raw fetch |
| `useLive` / `usePlace` / `useSend` / `useFire` / `useFlatten` / `useEnable` | **0** | no fire hook names |
| `NewOrderSingle` | **3** | English **disabled** copy on Live / Shadow / Overview |
| `REAL_COPY_EXECUTION_ENABLED` | **1** | Live page warning (gates still required) |

Interactive elements that **do** exist:

| Element | Where | What it does |
|---|---|---|
| `NavLink` × 14 | `DashboardLayout.tsx` | route change |
| `Link` × 1 | `TradersPage.tsx` → `/traders/:broker/:login` | route change |

Neither issues HTTP. Neither mentions order / send / copy-now.

### 4.1 Every page vs fire control

| Route | Module | Fire / send / flatten / enable control? |
|---|---|---|
| `/live` | `LiveCopyPage.tsx` | **No** — static warning |
| `/shadow` | `ShadowPortfolioPage.tsx` | **No** — static “Live NewOrderSingle remains disabled.” |
| `/overview` | `OverviewPage.tsx` | **No** — MetricCards + “Live FIX NewOrderSingle is off” |
| `/risk` | `RiskPage.tsx` | **No** — kill-switch / real-copy **display** only; no toggle |
| `/fix` | `FixSessionsPage.tsx` | **No** — session cards; `executionEnabled` is text |
| `/settings` | `SettingsPage.tsx` | **No** — `JSON.stringify` dump; no PUT |
| `/audit` | `AuditPage.tsx` | **No** — static copy |
| `/traders` | `TradersPage.tsx` | **No** — table + detail `Link` |
| `/traders/:id` | `TraderDetailPage.tsx` | **No** — chips + first-3 table; “Live promotion is not automatic.” |
| `/trades` | `TradeExplorerPage.tsx` | **No** — reconstructed source trades |
| `/scoring` | `ScoringPage.tsx` | **No** — score table |
| `/brokers` | `BrokersPage.tsx` | **No** — table |
| `/groups` | `GroupsPage.tsx` | **No** — table |
| `/reconciliation` | `ReconciliationPage.tsx` | **No** — JSON dump |
| `/health` | `SystemHealthPage.tsx` | **No** — JSON dump |

Risk paints `killSwitch` and `realCopyEnabled` as **MetricCard values**. There is no switch, checkbox, or confirm dialog.

### 4.2 Types that look like a live book (they are unused)

`D:\Prop\apps\web\src\types\index.ts` defines `TraderDetail.livePositions: Position[]` with `ticket` / `lots` / `currentPrice`. **Zero pages import the barrel.** `TraderDetailPage` reads `header` + `trades` only. This is client fiction, not a fire path.

---

## 5. Router / nav (chrome is not a trigger)

`App.tsx` L11 / L32:

```tsx
import LiveCopyPage from './pages/LiveCopyPage';
// …
<Route path="live" element={<LiveCopyPage />} />
```

`DashboardLayout.tsx` L13:

```text
{ to: '/live', label: 'Live', icon: '▶' }
```

The play-icon is a **nav glyph**, not an execute control. Clicking it mounts the static stub.

---

## 6. Why this is good (and what must not happen next)

A26 §9: `/live` primary GET is `/live/portfolio`; additional `/copy-intents`; **mutations: none**. Enable-live and flatten belong on Risk/Settings **when those mutations exist and gates pass** — they do not exist today, and this slot does **not** add them.

Backend send is independently **`SAFE_BY_ABSENCE`** (E002 / P500_S002 / P500_S021): no `35=D` builder, no QuickFIX initiator, no `GuardedNewOrderSingle`. A UI fire button would be a **new** capital-risk surface even if the backend still could not send. **Do not add the button in anticipation of a sender.**

| Forbidden follow-up | Why |
|---|---|
| “Copy now” / “Send NewOrderSingle” / “Go live” on `/live` | A26 §9 mutations **none** |
| Flatten-all on `/live` | flatten is a kill-switch action, not this page |
| Toggle `REAL_COPY_EXECUTION_ENABLED` from Settings UI | no PUT wired; would be an anonymous enable vector |
| `useMutation` + `POST /api/orders` “just for demo” | demo click can still be a live path later |
| Treat sidebar ▶ as execute | it is a `NavLink` |

---

## 7. What this file does **not** prove

- Live FIX Logon (still not proven).
- A coded refuse-path when TRADE is LoggedOn (no sender to refuse).
- That a later mapped `SettingsController` PUT cannot flip a Redis flag (controller is unmapped; even if mapped it is not a sender).
- Phase 8 / §70 readiness.

Absence of a UI fire button + absence of a `35=D` sender = **two independent safeties**. Neither is a tested gate.

---

## 8. Assigned answers (do not paraphrase away)

1. **Is there a UI button that fires a live order?**  
   **No.** `LiveCopyPage` has no controls. `hooks.ts` is GET-only. `apps/web/src` has zero `<button>`, zero `onClick`, zero `useMutation`, zero `client.post`.

2. **Is that good?**  
   **Yes.** A26 §9 forbids mutations on `/live`. Manual fire would bypass remaining gates (FIX TRADE logon + recon + risk approve + `REAL_COPY_EXECUTION_ENABLED`) and create a one-click loss path.

3. **Should one be added?**  
   **No.** Do not add one.

4. **Was product edited?**  
   **No.**

---

## Evidence pins

- Subject: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` — 8 lines, 0 imports, 0 hooks, 0 buttons.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` — 11 `useQuery` GETs; no `useMutation`.
- Client: `D:\Prop\apps\web\src\api\client.ts` — axios instance only.
- SignalR: `D:\Prop\apps\web\src\api\signalr.ts` — `on`/`off` only.
- Router: `D:\Prop\apps\web\src\App.tsx` L32 `path="live"`.
- Nav: `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` L13 `{ to: '/live', label: 'Live' }`.
- Types: `D:\Prop\apps\web\src\types\index.ts` `livePositions` unused.
- Adjacent honesty: `OverviewPage.tsx` L15 (“Live FIX NewOrderSingle is off”); `ShadowPortfolioPage.tsx` L7; `RiskPage.tsx` display-only; `TraderDetailPage.tsx` L44 (“Live promotion is not automatic.”).
- Law: A26 §9 (`D:\Prop\reports\swarm\20260818\A26_dashboard_api_spec.md`); Architecture §41 / §70.
- Siblings: C37 / D81 (stub), D39 (hooks), E002 / P500_S002 / P500_S021 (no sender), P500_S025 (UI honesty).

---

## One-line close

**No UI control on `/live` (or anywhere in `apps/web/src`) can fire a live order; hooks are GET-only; do not add a send button; product source was not modified.**
