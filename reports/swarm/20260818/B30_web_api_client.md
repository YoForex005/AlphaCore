# B30 — Web API client (`hooks.ts` + `signalr.ts`)

| Field | Value |
|---|---|
| Agent | B30 (senior engineer, React data-layer audit only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:21:08+05:30 |
| Workspace | `D:\Prop` |
| Assigned files | `D:\Prop\apps\web\src\api\hooks.ts`, `D:\Prop\apps\web\src\api\signalr.ts` |
| Adjacent (read, not assigned) | `client.ts`, `types/index.ts`, 13 pages, `DashboardLayout.tsx`, `main.tsx`, `vite.config.ts`, `apps/api/Program.cs`, `Application/Dashboard/DashboardModels.cs` |
| Product source modified | **No.** This report is the only write. |
| Method | Full-file read of both assigned sources + the axios singleton they import; SHA-256 + byte census; consumer grep across `apps/web/src`; live-route map against `Program.cs`; contract map against A26 / A62 / A63 / A97. No `npm`, no `tsc`, no product edit. |
| Precedence | On-disk files supersede A62 §3 / A91 §0 / A94 §12 / A95 §0 (those still say `pages/` empty or `client.ts` missing). A26 + A97 remain binding for the *replacement* paths. Demo `Program.cs` routes are **not** the catalog. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest)

**Both files exist. Both are demo stubs. Neither is the A26 / A62 / A97 client.**

`hooks.ts` is a TanStack Query v5 wrapper around **unversioned** `GET /api/*` paths. Those paths **do** match the current demo host (`apps/api/Program.cs` §3 in B06). They **do not** match the binding catalog (`/api/v1/**`). There is **no** mutation hook, **no** Bearer, **no** envelope unwrap, **no** generic DTO, **no** `queryKeys.ts`.

`signalr.ts` builds a module-level singleton to **`/hubs/dashboard`**. The binding hub is **`/hubs/ops`** (A26 §7, A63 §6, A97 §2.1). `apps/api` has **no** `AddSignalR` / `MapHub` (B06). `startConnection()` is called from the shell and **swallows** the inevitable failure. `onEvent` has **zero** consumers.

`D:\Prop\src\apps\web\src\api\` does **not** exist. The product frontend lives at `D:\Prop\apps\web`. Looking only under the C# workspace produces a false “no client” reading.

| Question | Answer |
|---|---|
| Do the assigned files exist? | **Yes** |
| Do they compile as modules? | **Yes** (axios + `@tanstack/react-query` + `@microsoft/signalr` are in `package.json`) |
| Do they talk to the **demo** API? | **11/11 GET hooks** hit a live `MapGet` (or the `/api/risk` twin of `/api/risk/status`) |
| Do they talk to the **catalog** API? | **0/11** use `/api/v1`. **0/1** hub uses `/hubs/ops` |
| Auth / RBAC / correlation / 401-refresh | **MISSING** |
| Envelope `{ data, page, error }` unwrap | **MISSING** — `r.data` is the raw axios body |
| Typed `useQuery<T>` | **0/11** — pages use `any` |
| `types/index.ts` imported by hooks or pages | **0** (dead file) |
| Mutations (`useMutation`) | **0** |
| SignalR JWT / `Subscribe` / sanitizer / invalidate | **0** |
| §69.12 “show all of this in React” via this client | **FAIL** as a catalog client. **PARTIAL** as a demo paint layer |

**Class:** `hooks.ts` = `EXISTS_NEEDS_REFACTOR` (also `DEPRECATED` as a contract source). `signalr.ts` = `EXISTS_NEEDS_REFACTOR` + **wrong hub name** (`DEPRECATED` path). `client.ts` (imported) = `EXISTS_NEEDS_REFACTOR`. Do **not** grow these files into the production client. A later coding wave replaces them against A26 §2 / A62 §§5+8+9 / A97 — same as A62 §3 already ordered.

Do **not** treat “pages render against port 5000” as catalog compliance. Demo coincidence is not the contract.

---

## 1. Measured files (2026-08-18T13:21:08+05:30)

| Bytes | SHA-256 | Path |
|---|---|---|
| 1935 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `D:\Prop\apps\web\src\api\hooks.ts` (53 lines) |
| 899 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | `D:\Prop\apps\web\src\api\signalr.ts` (28 lines) |
| 232 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | `D:\Prop\apps\web\src\api\client.ts` (9 lines; imported by hooks) |
| 2905 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | `D:\Prop\apps\web\src\types\index.ts` (dead; **not** imported) |

`api/` contains **exactly** those three TypeScript files. Absent vs A62 §5:

```text
api/queryKeys.ts
api/errors.ts
api/auth.ts
api/overview.ts … api/settings.ts   (1:1 fetcher split)
hubs/opsHub.ts
hubs/useOpsHub.ts
hubs/invalidate.ts
.env.example                         (VITE_API_URL=)
src/vite-env.d.ts
```

`Test-Path` of `.env`, `.env.example`, `vite-env.d.ts`, `queryKeys.ts`, `src/hubs` = **all False**.

A62 §3 listed `hooks.ts` at **3042 B** and said it imported a missing `./client` and `../types`. **Stale.** Current `hooks.ts` is **1935 B**, imports `./client` (now present), does **not** import types, and contains **no** flatten / `PUT /api/settings` / `useEmergencyFlatten` (grep over `apps/web/src` = 0). The dangerous mutation stubs A62 warned about are **already gone**. What remains is a GET-only demo client.

---

## 2. `client.ts` (the transport hooks actually use)

```1:9:D:\Prop\apps\web\src\api\client.ts
import axios from 'axios';

const client = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000',
  timeout: 15000,
  headers: { 'Content-Type': 'application/json' },
});

export default client;
```

| Capability (A26 §2 / A62 §5) | Measured |
|---|---|
| One HTTP client | **Yes** — axios 1.7 |
| Base URL | `VITE_API_URL` or hardcoded `http://localhost:5000` |
| Vite proxy `/api` + `/hubs` | **MISSING** (`vite.config.ts` is port 3000 only, 169 B) |
| `Authorization: Bearer` | **MISSING** — no interceptor |
| `X-Correlation-Id` | **MISSING** |
| 401 → `POST /api/v1/auth/refresh` → `/login` | **MISSING** |
| A26 envelope unwrap (`body.data`) | **MISSING** |
| `ApiError` from `error.code` / `correlationId` | **MISSING** |
| `Idempotency-Key` on POST | **N/A** — no POSTs in this layer |
| Secret-field client denylist | **MISSING** |
| `withCredentials` for refresh cookie | **MISSING** |

Defaulting an empty `VITE_API_URL` to `http://localhost:5000` **happens to** match `apps/api` launch profile `http` (`applicationUrl: http://localhost:5000`). A62 §2 says empty `VITE_API_URL` means **same origin / Vite proxy**. The stub does the opposite: it always cross-origin hits :5000, and there is **no** proxy. Demo works if the API is on :5000. HTTPS profile `:7294` is unused. CORS on the API is `AllowAnyOrigin` (B06) so the browser is not blocked — that is a **host** hole, not a client feature.

No `vite-env.d.ts`, so `import.meta.env.VITE_API_URL` is an untyped escape.

---

## 3. `hooks.ts` — complete inventory

Source (entire file):

```1:53:D:\Prop\apps\web\src\api\hooks.ts
import { useQuery } from '@tanstack/react-query';
import client from './client';

export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
}
// … 10 more GET hooks; no mutations …
export function useSettings() {
  return useQuery({ queryKey: ['settings'], queryFn: () => client.get('/api/settings').then(r => r.data) });
}
```

Eleven exports. All `useQuery`. Shared pattern: `client.get(path).then(r => r.data)`. Three add `refetchInterval`. One has `enabled`. None pass a TypeScript generic. None set `staleTime` (they inherit `main.tsx`: retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000`).

### 3.1 Hook → demo host → binding catalog

| Hook | Query key (disk) | HTTP (disk) | Demo `Program.cs` | A26 binding path | A62 key | Class |
|---|---|---|---|---|---|---|
| `useOverview` | `['overview']` | `GET /api/overview` | **live** `GetOverviewAsync` | `GET /api/v1/overview` | `['overview']` | path **WRONG**; key OK |
| `useBrokers` | `['brokers']` | `GET /api/brokers` | **live** | `GET /api/v1/brokers` | `['brokers']` | path **WRONG**; key OK |
| `useGroups` | `['groups']` | `GET /api/groups` | **live** | `GET /api/v1/mt5/groups` | `['mt5-groups', filters]` | path **and** key **WRONG** |
| `useTraders` | `['traders', filters]` | `GET /api/traders?broker&state` | **live** `GetTradersAsync` | `GET /api/v1/traders` (A92 filters) | `['traders', filters]` | path **WRONG**; filter set **thin** |
| `useTraderDetail` | `['trader', broker, login]` | `GET /api/traders/{broker}/{login}` | **live** — **same row DTO as list**, not §51 | `GET /api/v1/traders/{brokerId}/{login}` | `['trader', brokerId, login]` | path prefix **WRONG**; shape **WRONG** |
| `useTrades` | `['trades']` | `GET /api/trades` | **live** — raw `ReconstructedTrades` EF, last 200; `broker` unused | `GET /api/v1/trades` | `['trades', filters]` | path **WRONG**; key missing filters; **UNSAFE** body (EF leak on host) |
| `useFixSessions` | `['fix-sessions']` | `GET /api/fix/sessions` | **live** | `GET /api/v1/fix/sessions` (A94 two-card DTO) | `['fix']` | path **and** key **WRONG** |
| `useRiskStatus` | `['risk']` | `GET /api/risk` | **live** (`/api/risk` **and** alias `/api/risk/status`) | A26 `GET /api/v1/risk/dashboard`; A63 `…/risk/snapshot` | `['risk']` | path **WRONG**; key OK. A95 “`/api/risk/status`” is **stale** vs this file |
| `useReconciliation` | `['reconciliation']` | `GET /api/reconciliation/status` | **live hardcoded** zeros + `now` | `GET /api/v1/reconciliation` | `['reconciliation']` | path **WRONG**; body is a **lie** (host) |
| `useHealth` | `['health']` | `GET /api/health` | **live hardcoded** inventory | `GET /api/v1/health` (liveness is `/health` or `/api/v1/health/live`) | `['health']` | path **WRONG** |
| `useSettings` | `['settings']` | `GET /api/settings` | **live hardcoded** flags | A26 `GET /api/v1/settings`; A63 first-useful `GET /api/v1/settings/public` | `['settings']` | path **WRONG** |

**Demo-path hit rate: 11/11. Catalog-path hit rate: 0/11.**

A95’s line “`hooks.ts` `GET /api/risk/status`” is **false for the current file**. The hook calls `/api/risk`. The host still serves `/api/risk/status` as an alias of the same query (B06 row 12). Do not “fix” the hook by retargeting the alias; retarget `/api/v1/risk/dashboard` (A26 path wins per A62 §1; A95 JSON wins for the body).

### 3.2 Polling vs A62 / A97

| Hook | `refetchInterval` | A62 / A97 rule |
|---|---|---|
| `useFixSessions` | **5000 ms** | Replace with SignalR `fix.session` / `quote.xauusd` + 15–30 s fallback if hub down. Do not poll secrets (none here, but 5 s against a missing hub is noisy) |
| `useRiskStatus` | **5000 ms** | Same: `risk.state` invalidation |
| `useHealth` | **10000 ms** | `health.updated`; liveness is a different URL |
| others | inherit 30 s stale | OK for lists until hub exists |

### 3.3 Filters and identity

`useTraders({ broker?: string; state?: string })` matches **only** the demo query string. A92 is binding for the leaderboard: group, score range, risk flags, martingale, trade-count, date, sort, page. Those params are **not** on the hook. `TradersPage` and `ScoringPage` both call `useTraders({})` — Scoring has **no** `GET /api/v1/scoring/summary` hook.

`useTraderDetail(broker, login)` takes `login: string` (from the route param). Demo route constraint is `{login:long}`. `enabled: !!broker && !!login` prevents the empty-string call. Compound identity `(brokerId, login)` is the right *shape* (architecture §10). The **payload** is still `TraderRowDto`, not the A93 detail document (no trades, no lot timeline, no holding-time, no source↔dest map). The page already reads `data.earlyScore` / `data.completedXauTrades` — i.e. the **row** DTO — and will break if a later wave returns an A26 `{ data: { header, scores, … } }` envelope without an unwrap.

`useTrades()` accepts **no** `broker` / `login` / page even though the host query string declares `broker` and `login` (and ignores `broker`). Trade Explorer cannot filter.

### 3.4 Hooks that do not exist (catalog / §46)

| Needed by | Binding GET / mutation | Disk |
|---|---|---|
| Login / shell | `POST /api/v1/auth/login\|refresh\|logout`, `GET /auth/me` | **MISSING** |
| Scoring page | `GET /api/v1/scoring/summary` | **MISSING** (page reuses `useTraders`) |
| Models | `GET /api/v1/models` | **MISSING** (page missing too — B22) |
| Shadow | `GET /api/v1/shadow/portfolio\|positions\|performance`, `GET /copy-intents` | **MISSING** (`ShadowPortfolioPage` is static text — B22) |
| Live Copy | `GET /api/v1/live/portfolio` | **MISSING** |
| Trader detail extras | trades / scores / lot-timeline / holding-times / shadow / `POST copy-control` | **MISSING** |
| FIX extras | `GET /fix/quote`, `/fix/trade`, `/fix/sessions/{session}/events` | **MISSING** |
| Risk writes | `GET /risk/rejections`, `/risk/kill-switch`, `POST /risk/stop-new-execution` | **MISSING** |
| Recon extras | `/reconciliation/runs\|issues`, `POST …/run`, `POST …/ack` | **MISSING** |
| Audit | `GET /api/v1/audit` | **MISSING** |
| Groups write | `PATCH /api/v1/mt5/groups/{id}` | **MISSING** |
| Settings writes | sliced `PATCH` only; **never** `PUT /settings` | **MISSING** (good: the old PUT stub is gone) |
| Flatten | `POST /api/v1/risk/emergency-flatten` | **absent — correct for v1** (A63: do not wire until Phase 8) |

Absence of flatten / `PUT /settings` is a **positive** delta vs the A62-era stub. Do not re-add them.

---

## 4. Types vs what the pages actually read

`hooks.ts` does **not** import `../types`. Grep `from ['"].*types` under `apps/web/src` = **0**. `types/index.ts` is **dead code**.

Pages consume **axios `any`** and field names from `DashboardModels.cs` (ASP.NET camelCase), **not** the dead interfaces:

| Page field (disk) | C# source | Dead `types/index.ts` |
|---|---|---|
| `data.connectedBrokers`, `xauTraders`, `tradersWithThreeTrades`, `watch`, `shadowPnl`, `mt5Healthy` | `OverviewDto` | `Overview.totalBrokers`, `tradersByState`, `fixHealthy` |
| `b.code`, `displayName`, `managerLoginMasked`, `groupCount` | `BrokerStatusDto` | `Broker.id`, `name`, `groups` |
| `g.broker`, `g.group`, `enabledForAnalysis` | `GroupRowDto` | `Group.brokerId`, `name`, `enabled` |
| `t.completedXauTrades`, `earlyScore`, `netSourcePnl` | `TraderRowDto` | `Trader.completedTrades`, `score`, `pnl` |
| `s.qualifier`, `inboundSeq`, `quoteAgeSeconds` | `FixSessionDto` | `FixSession.type`, `inSequence`, `quoteAge` |
| `data.killSwitch`, `recentRejectReasons` | `RiskDashboardDto` | `RiskStatus.emergencyFlatten`, `rejectedIntents[]` |
| `t.id`, `canonicalSymbol`, `maxVolumeLots`, `netRealizedPnl` | EF `ReconstructedTrade` | `Trade.ticket`, `symbol`, `lots`, `pnl` |

A91/A92/A94/A95 were right that `types/index.ts` is the **wrong** contract. They were wrong if they implied hooks *use* those types — they do not. Replacing `types/index.ts` without changing hooks is a no-op.

Score scale: pages do `Number(t.earlyScore).toFixed(1)` on the domain **0–100** `TraderRowDto.EarlyScore`. A97 §1.1 forbids dividing by 100. The current pages do **not** divide. Keep it that way.

Tickets / ids: dead `Trade.ticket: number` violates A26 §2.1 (tickets are **string**). The live Trade Explorer uses `t.id` (GUID) as React key — accidental compliance, not a typed contract.

---

## 5. `signalr.ts` — complete inventory

```1:28:D:\Prop\apps\web\src\api\signalr.ts
import * as signalR from '@microsoft/signalr';

const BASE = import.meta.env.VITE_API_URL || 'http://localhost:5000';

let connection: signalR.HubConnection | null = null;

export function getConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${BASE}/hubs/dashboard`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function startConnection() {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try { await conn.start(); } catch (e) { console.warn('SignalR connection failed', e); }
  }
}

export function onEvent<T>(event: string, handler: (data: T) => void) {
  getConnection().on(event, handler);
  return () => getConnection().off(event, handler);
}
```

### 5.1 Consumers

| Export | Call sites |
|---|---|
| `startConnection` | `DashboardLayout.tsx` `useEffect(() => { startConnection(); }, [])` **only** |
| `getConnection` | internal |
| `onEvent` | **none** |

The shell starts a hub and **never subscribes**. No page invalidates a query from a frame. StrictMode double-mount is mostly harmless (`state === Disconnected` guard). There is **no** `stop()` on unmount.

### 5.2 Transport vs A97

| Item | Disk | Binding (A97 §2 / A26 §7) |
|---|---|---|
| Path | `/hubs/dashboard` | **`/hubs/ops`**. `/hubs/dashboard` is **forbidden** (A63 §6) |
| Host | none — API has no `MapHub` | `OpsHub` on `apps/api` only |
| Auth | none | Bearer or negotiate `access_token` |
| `accessTokenFactory` | **MISSING** | required |
| Protocol | JSON default | JSON; MessagePack **off** |
| `Subscribe(topics)` after start / reconnect | **MISSING** | mandatory; then apply snapshots |
| Client methods | none | only `Subscribe` / `Unsubscribe` / `WatchTrader` / `UnwatchTrader` |
| Forbidden methods (`SendOrder`, flatten, passwords) | not present | keep **absent** |
| Envelope `v/event/at/seq/topic/data` | handler receives raw `T` | must ignore unknown `v`; read `data` |
| Sanitizer (fail-closed denylist) | **MISSING** | drop frame on password/secret key |
| Query invalidation | **MISSING** | A62 §9 / A97 §4.3 |
| Error handling | `console.warn` swallow | surface a banner; REST remains SoT |
| Logging | `Warning` | acceptable |

After a failed `start()`, the singleton stays `Disconnected`. `startConnection` is invoked once, so there is **no** retry except SignalR’s own reconnect — which never begins if `start()` never succeeded. That is the measured production behavior today: **one warning in the console, REST still paints**.

### 5.3 Event-name gap

A26 §7 / A97 / A63 §6.1 names the frames the client must handle. **None** are referenced in `apps/web/src`:

```text
ops.header
overview.updated
broker.health
fix.session
fix.health
quote.xauusd
quote.stale
risk.state
reconciliation.issue
trader.score
trader.score.batch
trader.state
scoring.summary
health.updated
shadow.portfolio
alert.raised / alert.cleared / alert.snapshot
hub.error
```

A later wave must **not** `on('overview')` or invent names. Retarget + `Subscribe` + `queryClient.invalidateQueries` / `setQueryData` per A62 §9.

---

## 6. Consumer matrix (pages → hooks)

Confirms B22: 13 page files exist. This table is **data-layer** only.

| Page | Hook used | What it assumes `data` is |
|---|---|---|
| `OverviewPage` | `useOverview` | `OverviewDto` camelCase (not dead `Overview`) |
| `BrokersPage` | `useBrokers` | `BrokerStatusDto[]` |
| `GroupsPage` | `useGroups` | `GroupRowDto[]` |
| `TradersPage` | `useTraders({})` | `TraderRowDto[]` |
| `TraderDetailPage` | `useTraderDetail(brokerId, login)` | **same row**, not A93 |
| `TradeExplorerPage` | `useTrades` | raw EF rows |
| `ScoringPage` | `useTraders({})` | leaderboard rows; **not** scoring summary |
| `ShadowPortfolioPage` | **none** | static copy |
| `FixSessionsPage` | `useFixSessions` | `FixSessionDto[]` |
| `RiskPage` | `useRiskStatus` | `RiskDashboardDto` |
| `ReconciliationPage` | `useReconciliation` | hardcoded status object (`JSON.stringify`) |
| `SystemHealthPage` | `useHealth` | hardcoded inventory (`JSON.stringify`) |
| `SettingsPage` | `useSettings` | hardcoded flags (`JSON.stringify`) |
| `DashboardLayout` | `startConnection` | ignore result |
| Login / Models / Live / Audit | no route (B22) | no hook |

If the API is rewritten to A26 envelopes **before** the client unwraps, **every** table that does `data = []` will iterate the envelope object / crash. Order of a replacement wave: `client.ts` unwrap + typed fetchers **then** host `/api/v1`, or dual-write both shapes. Do not flip the host alone.

---

## 7. Security / secret-safety (client)

| Check | Result |
|---|---|
| Hook or SignalR URL contains a password | **No** |
| Client sends denylisted keys | **No** (no write bodies) |
| Client stores refresh token in `localStorage` | **No** (no auth at all) |
| Client attaches JWT | **No** — will be 401 the moment the host grows auth |
| Client sanitizes hub frames | **No** |
| Settings page dumps `JSON.stringify(data)` | Host payload is flags + broker **names** today (B06). Still a **foot-gun** if `/api/settings` ever grows `CTraderFixOptions` |
| `VITE_*` secrets | None on disk. Fallback URL is a host, not a credential |
| Flatten / enable-live from the browser | **Not wired** (correct) |

`UNSAFE` does **not** apply to these two files as written. The **host** `/api/trades` EF leak and anonymous CORS are B06 issues. The client will faithfully display whatever the host returns, including a future secret, because there is no denylist.

---

## 8. What earlier reports got wrong (do not re-litigate)

| Claim | File | Now |
|---|---|---|
| `hooks.ts` imports missing `./client` | A62 §3 | `client.ts` **exists** |
| `hooks.ts` is 3042 B and has flatten / `PUT /settings` | A62 §3 / §13 | **1935 B**, GET-only |
| `pages/` empty so hooks are unused | A62 / A91 / A94 / A95 | **13 pages consume 10 of 11 hooks** (`useSettings`/`useHealth`/`useReconciliation` dump JSON; shadow uses none) |
| hook calls `/api/risk/status` | A95 | calls **`/api/risk`** |
| `types/index.ts` is the React contract | A91 | **unimported** |
| API is still only `weatherforecast` | A26 §0 / A91 / A94 | **stale** — B06 lists 15 demo maps; hooks target that set |

This file **supersedes** those snapshots **for the React data layer only**. It does **not** supersede A26 paths, A92 leaderboard query grammar, A93/A94/A95/A96 DTO shapes, or A97 hub names.

---

## 9. Replacement sequence (when a coding task is authorized)

Do **not** implement from this file. When authorized:

1. Add `vite-env.d.ts` + `.env.example` with `VITE_API_URL=` (empty). Add Vite proxy `/api` + `/hubs` → `http://localhost:5000`. Change the axios / SignalR default to **empty base** (same origin), not `:5000`.
2. Replace `client.ts`: Bearer from memory, `X-Correlation-Id`, 401→refresh cookie, envelope unwrap, `ApiError`. No secrets in `VITE_*`.
3. Add `queryKeys.ts` **exactly** as A62 §8 (`['fix']` not `['fix-sessions']`; `['mt5-groups', filters]` not `['groups']`).
4. Split fetchers 1:1 with A26 §6. Delete the untyped mega-`hooks.ts` or reduce it to thin `useQuery` wrappers around those fetchers.
5. Retarget every GET to `/api/v1/...`. Groups = `/mt5/groups`. Risk = `/risk/dashboard` (A26) with A95 JSON. Settings first-useful = `/settings/public` (A63) or `/settings` (A26) — pick in the coding wave; **do not** keep `/api/settings`.
6. Replace `signalr.ts` with `hubs/opsHub.ts` → `/hubs/ops` + `accessTokenFactory` + `Subscribe` on start/reconnect + fail-closed sanitizer + invalidation map (A62 §9, A97 §4). Delete `/hubs/dashboard`.
7. Add missing read hooks before new pages: scoring summary, shadow portfolio, copy-intents, health live vs ready, auth/me.
8. Mutations last, with `Idempotency-Key` + `confirmPhrase` where A26 requires them. **Never** reintroduce `PUT /api/settings` or unconfirmed flatten.

Until `apps/api` hosts `/api/v1` + `OpsHub`, the demo hooks are the only thing that paints. Keep them working **or** land client + host in one slice. Do not retarget hooks to `/api/v1` against today’s `Program.cs` — those routes are **absent** (B06).

---

## 10. Direct answers

### Are `hooks.ts` and `signalr.ts` present?

**Yes**, at `D:\Prop\apps\web\src\api\`, not under `D:\Prop\src`.

### Do they implement the dashboard API client?

**No.** They implement a **demo** client for the unversioned `Program.cs` maps. Catalog compliance is **0/11** REST paths and **0/1** hub path.

### Is the SignalR helper usable?

**No.** Wrong URL, no JWT, no `Subscribe`, no consumers of `onEvent`, and the API does not host a hub. `startConnection()` is a swallowed `console.warn`.

### Should a later wave edit these files in place?

**Replace.** Keep axios + TanStack Query + `@microsoft/signalr` (already pinned). Do not keep `/api/groups`, `/api/risk`, `/api/reconciliation/status`, or `/hubs/dashboard` as names.

### Product source edited this pass?

**No.**
)
