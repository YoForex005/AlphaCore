# D39 — `hooks.ts` vs `Program.cs` endpoints

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D39_hooks.md` |
| Agent | D39 (senior engineer, React hooks vs API host route matrix) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:37:27+05:30 |
| Assigned | Read `hooks.ts` vs `Program.cs` endpoints. Write this file. Do not modify product source. |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No** |
| Subjects | `D:\Prop\apps\web\src\api\hooks.ts`, `D:\Prop\apps\api\Program.cs` |
| Adjacent (read, not edited) | `client.ts`, `signalr.ts`, `types/index.ts`, 15 pages, `DashboardLayout.tsx`, `main.tsx`, `vite.config.ts`, `TraderIntelligence.Api.http`, `DashboardModels.cs`, `EfDashboardQueries.cs`, `ReconstructedTrade.cs`, `TraderScore.cs` |
| Method | Full-file read of both subjects + every consumer. PowerShell `Get-FileHash SHA256` + byte / physical-line / last-write census. Grep `MapGet`/`MapPost`/`MapHub`/`AddSignalR`/`useQuery`/`useMutation`/`/api/`. Cross-check A26 / A63 / A92 / A93 / B30 / C04 / D02 / D08. **API process was not launched.** No HTTP capture. |
| Precedence | On-disk `Program.cs` (SHA `61B1E0D1…`, 13:35:15) supersedes C04 / B30 / D02 on trader-detail. Demo unversioned `/api/*` is **not** the A26/A63 catalog. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

---

## 0. Verdict

**Eleven `useQuery` hooks call eleven live unversioned `GET /api/*` maps. That is a demo coincidence, not a catalog client.**

| Question | Measured answer |
|---|---|
| Do all hook URLs exist on the host? | **Yes. 11 / 11.** Every `client.get('/api/…')` has a matching `MapGet`. |
| Do all host maps have a hook? | **No. 11 / 15.** Four host maps have no hook (`GET /health`, `GET /ready`, `GET /api/risk/status`, `POST /api/ops/resync`). |
| Do hooks hit `/api/v1/**` (A26 / A63)? | **0 / 11.** |
| Is there a mutation hook? | **0.** Host has one write: anonymous `POST /api/ops/resync`. |
| Is SignalR mapped? | **No.** `signalr.ts` targets `/hubs/dashboard`. `Program.cs` has **0** `AddSignalR` / `MapHub`. |
| Typed `useQuery<T>`? | **0 / 11.** Pages use `any`. `types/index.ts` is unused. |
| Envelope unwrap `{ data }`? | **No.** `r.data` is the raw axios body. Host returns bare DTOs / anonymous objects / EF entities. |
| Auth / RBAC / correlation? | **MISSING** on both sides. CORS is `AllowAnyOrigin`. |
| Trader detail still a list row? | **No longer.** Host now calls `GetTraderDetailAsync` → `TraderDetailDto { header, trades }`. `TraderDetailPage` already reads `data.header ?? data`. **B30 / C04 / D02 are stale on this row.** |

**Class:** `hooks.ts` = `EXISTS_NEEDS_REFACTOR` (demo GET wrapper; `DEPRECATED` as a contract source). `Program.cs` maps = `EXISTS_NEEDS_REFACTOR` as a first-useful BFF, `DEPRECATED` as A26. `POST /api/ops/resync` = `UNSAFE` (anonymous write). `/api/trades` = `UNSAFE` (raw EF entity leak). `/hubs/dashboard` = `MISSING` on the host.

Do **not** treat 11/11 demo-path hits as `/api/v1` compliance. Do **not** grow these hooks into the production client; replace against A26 §2 / A62 / A63 / A97.

---

## 1. Measured files (this pass)

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) |
|---|---:|---:|---|---|
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00 |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06 |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02 |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18 |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15 |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | 36 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | 2026-08-18T13:08:41 |
| `D:\Prop\apps\api\TraderIntelligence.Api.http` | 193 | 9 | `2AEC0F4A6058853646C0719EB7443AE5286C1467C52B164DB8BD83430580F651` | 2026-08-18T13:20:38 |
| `D:\Prop\apps\web\vite.config.ts` | 169 | 7 | `626F3F347D91B59AD3ED07EADD64ED34ACB541E34EB2876C0AB44D86FAC7AFE1` | 2026-08-18T13:06:19 |

`hooks.ts` SHA is **unchanged** vs D08 / B30. `Program.cs` has **grown** since C04 (4658 B / `E914FA98…` → 4731 B / `61B1E0D1…`). The delta is the trader-detail map (`GetTraderAsync` → `GetTraderDetailAsync`) plus the new `TraderDetailDto` / `TradeHighlightDto` types (DashboardModels last write 13:34:59; queries last write 13:35:15).

No `useMutation`. No `queryKeys.ts`. No Vite `/api` proxy. Axios `baseURL` is `import.meta.env.VITE_API_URL || 'http://localhost:5000'`, which matches the `http` launch profile (`applicationUrl: http://localhost:5000`).

---

## 2. Complete hook inventory

Entire `hooks.ts` is 11 exports, all `useQuery`, all `client.get(path).then(r => r.data)`. Inherited QueryClient (`main.tsx`): retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000`.

| # | Hook | Query key | HTTP | Extra | Consumers |
|---|---|---|---|---|---|
| 1 | `useOverview` | `['overview']` | `GET /api/overview` | — | `OverviewPage` |
| 2 | `useBrokers` | `['brokers']` | `GET /api/brokers` | — | `BrokersPage` |
| 3 | `useGroups` | `['groups']` | `GET /api/groups` | — | `GroupsPage` |
| 4 | `useTraders` | `['traders', filters]` | `GET /api/traders` + `params: { broker?, state? }` | — | `TradersPage` (`{}`), `ScoringPage` (`{}`) |
| 5 | `useTraderDetail` | `['trader', broker, login]` | `GET /api/traders/${broker}/${login}` | `enabled: !!broker && !!login`; `login` is `string` | `TraderDetailPage` |
| 6 | `useTrades` | `['trades']` | `GET /api/trades` | **no** `broker`/`login` params | `TradeExplorerPage` |
| 7 | `useFixSessions` | `['fix-sessions']` | `GET /api/fix/sessions` | `refetchInterval: 5000` | `FixSessionsPage` |
| 8 | `useRiskStatus` | `['risk']` | `GET /api/risk` | `refetchInterval: 5000` | `RiskPage` |
| 9 | `useReconciliation` | `['reconciliation']` | `GET /api/reconciliation/status` | — | `ReconciliationPage` (`JSON.stringify`) |
| 10 | `useHealth` | `['health']` | `GET /api/health` | `refetchInterval: 10000` | `SystemHealthPage` (`JSON.stringify`) |
| 11 | `useSettings` | `['settings']` | `GET /api/settings` | — | `SettingsPage` (`JSON.stringify`) |

Pages with **no** hook: `ShadowPortfolioPage`, `LiveCopyPage`, `AuditPage`. Scoring has **no** scoring endpoint; it reuses the trader list.

---

## 3. Complete `Program.cs` map inventory

Fourteen `MapGet` + one `MapPost` (**15** maps). Zero controllers. Zero `MapGroup("/api/v1")`. Zero `AddAuthentication`. Zero `AddSignalR`. JSON enums via `JsonStringEnumConverter`. CORS `AllowAnyHeader` + `AllowAnyMethod` + `AllowAnyOrigin`. Startup: `EnsureCreatedAsync` + `DemoSeeder`.

| # | Method | Path | Handler (disk) | Wire body | Hook? |
|---|---|---|---|---|---|
| 1 | `GET` | `/health` | anonymous `{ status, utc }` | process liveness | **no** |
| 2 | `GET` | `/api/health` | hardcoded inventory | ACHIEVER “healthy”, QUOTE “no live TLS”, redis false, `outboxBacklog: 0` | `useHealth` |
| 3 | `GET` | `/api/risk/status` | `q.GetRiskAsync` | `RiskDashboardDto` | **no** (alias of #13) |
| 4 | `GET` | `/api/reconciliation/status` | hardcoded zeros + `UtcNow` | `{ lastReconciliation, unknownPositions, mismatches, orphanFills }` | `useReconciliation` |
| 5 | `GET` | `/api/settings` | hardcoded object | `riskLimits`, `featureFlags.REAL_COPY_EXECUTION_ENABLED=false`, two broker labels | `useSettings` |
| 6 | `GET` | `/ready` | `CountAsync(db.Brokers)` | `{ ready: true, brokers }` — always ready | **no** |
| 7 | `GET` | `/api/overview` | `q.GetOverviewAsync` | `OverviewDto` | `useOverview` |
| 8 | `GET` | `/api/brokers` | `q.GetBrokersAsync` | `BrokerStatusDto[]` | `useBrokers` |
| 9 | `GET` | `/api/groups` | `q.GetGroupsAsync` | `GroupRowDto[]` | `useGroups` |
| 10 | `GET` | `/api/traders` | `q.GetTradersAsync(broker, state)` | `TraderRowDto[]` (no envelope, no page) | `useTraders` |
| 11 | `GET` | `/api/traders/{broker}/{login:long}` | `q.GetTraderDetailAsync` | `TraderDetailDto?` (`header` + `trades`) or **204** if null | `useTraderDetail` |
| 12 | `GET` | `/api/fix/sessions` | `q.GetFixSessionsAsync` | `FixSessionDto[]` | `useFixSessions` |
| 13 | `GET` | `/api/risk` | `q.GetRiskAsync` | same as #3 | `useRiskStatus` |
| 14 | `GET` | `/api/trades` | `db.ReconstructedTrades` last 200 | **raw EF entities** | `useTrades` |
| 15 | `POST` | `/api/ops/resync` | ingest both brokers + rebuild 4 logins | `{ achieverDeals, starwaveDeals }` | **no** |

`.http` samples 7 GETs (`/health`, `/api/overview`, `/api/brokers`, `/api/groups`, `/api/traders`, `/api/fix/sessions`, `/api/risk`). It does **not** sample trader detail, trades, health inventory, settings, recon, `/ready`, or resync.

---

## 4. Matrix: hook path × host × A26 catalog

| Hook | Disk path | Host map | Body source | A26 / A63 catalog | Demo hit | Catalog hit |
|---|---|---|---|---|---|---|
| `useOverview` | `/api/overview` | **yes** | `OverviewDto` (query) | `GET /api/v1/overview` | yes | **no** |
| `useBrokers` | `/api/brokers` | **yes** | `BrokerStatusDto[]` | `GET /api/v1/brokers` | yes | **no** |
| `useGroups` | `/api/groups` | **yes** | `GroupRowDto[]` | `GET /api/v1/mt5/groups` | yes | **no** (resource name too) |
| `useTraders` | `/api/traders?broker&state` | **yes** | `TraderRowDto[]` | `GET /api/v1/traders` (A92 filters + envelope) | yes | **no** |
| `useTraderDetail` | `/api/traders/{broker}/{login}` | **yes** (`login:long`) | `TraderDetailDto` **now** | `GET /api/v1/traders/{brokerId}/{login}` (A93) | yes | **no** |
| `useTrades` | `/api/trades` | **yes** | raw `ReconstructedTrade` | `GET /api/v1/trades` allow-list DTO | yes | **no** |
| `useFixSessions` | `/api/fix/sessions` | **yes** | `FixSessionDto[]` | `GET /api/v1/fix/sessions` (A94) | yes | **no** |
| `useRiskStatus` | `/api/risk` | **yes** | `RiskDashboardDto` | `GET /api/v1/risk/dashboard` (A26); A63 `…/risk/snapshot` | yes | **no** |
| `useReconciliation` | `/api/reconciliation/status` | **yes** | **hardcoded lie** | `GET /api/v1/reconciliation` | yes | **no** |
| `useHealth` | `/api/health` | **yes** | **hardcoded inventory** | `GET /api/v1/health` (live probe is `/health` or `/api/v1/health/live`) | yes | **no** |
| `useSettings` | `/api/settings` | **yes** | **hardcoded flags** | `GET /api/v1/settings` | yes | **no** |

**Demo-path hit rate: 11/11. Catalog-path hit rate: 0/11.**

A95’s “`hooks.ts` calls `/api/risk/status`” is **false for this file**. The hook calls `/api/risk`. The host still serves `/api/risk/status` as an unused alias.

---

## 5. Host-only endpoints (no hook)

| Path | Why it exists | Why no hook is correct / not |
|---|---|---|
| `GET /health` | process liveness `{ status, utc }` | Orchestrator probe. React uses `/api/health` instead. Fine. |
| `GET /ready` | broker count; always `ready: true` | No UI. Fine. |
| `GET /api/risk/status` | twin of `/api/risk` | Dead alias. Do **not** retarget the hook here. Catalog is `/api/v1/risk/dashboard`. |
| `POST /api/ops/resync` | anonymous demo ingest + score rebuild for logins `10001,10002,10003,99001` from `2026-01-01` | **Correctly unwired** from React. **UNSAFE** if left anonymous when the host is reachable. |

---

## 6. Client-only / missing catalog surfaces

| Client path or needed hook | Host | Notes |
|---|---|---|
| `signalr.ts` → `${BASE}/hubs/dashboard` | **MISSING** (`0` `MapHub`) | Binding hub is `/hubs/ops` (A26 §7, A97). `DashboardLayout` calls `startConnection()` and swallows the failure. `onEvent` has **zero** page consumers. |
| Scoring `GET /api/v1/scoring/summary` | **MISSING** | `ScoringPage` reuses `useTraders`. |
| Shadow `GET /api/v1/shadow/*` | **MISSING** | Static copy. |
| Live `GET /api/v1/live/portfolio` | **MISSING** | Static “flag is false” copy. |
| Audit `GET /api/v1/audit` | **MISSING** | Static copy. |
| Models `GET /api/v1/models` | **MISSING** | Page does not exist (D08). |
| Auth `POST /api/v1/auth/*` | **MISSING** | No `LoginPage`. |
| Flatten / `PUT /settings` | **absent** | **Positive** vs the A62-era stub. Do not re-add. |

---

## 7. Identity, filters, and routing mismatches

| Topic | Hook | Host | Effect |
|---|---|---|---|
| Trader login type | `login: string` from `useParams` | `{login:long}` | Non-numeric login → **404** from routing before the handler. `enabled` only blocks empty string. |
| Trader identity | path `broker` + `login` | path `broker` (code, not Guid) + `login` | Compound identity **shape** matches §10. |
| Leaderboard filters | only `broker?`, `state?` | same two query params | A92 group / score range / flags / page / sort are **absent** on both. Pages pass `{}`. |
| State parse | string passthrough | `Enum.TryParse<TraderState>(state, true)` | Unknown state → unfiltered list (host), not 400. |
| Trades filters | **none** | `broker?` declared, **unused**; `login?` applied | Trade Explorer cannot filter. Host `broker` is dead code. |
| Missing trader | axios empty/`undefined` on **204** | `GetTraderDetailAsync` returns `null` → Minimal API **204** | Page: “Trader not found.” Acceptable for demo. Catalog wants **404** + error envelope. |
| Polling | FIX/risk 5 s; health 10 s | no hub to invalidate | Noisy stand-in for missing SignalR. |

---

## 8. Payload alignment (what pages actually read)

`hooks.ts` does **not** import `../types`. Grep `from ['"].*types` under `apps/web/src` = **0**. Pages bind ASP.NET camelCase from `DashboardModels.cs` / anonymous objects / EF, **not** the dead TypeScript interfaces.

| Page | Fields read | Host field | Match? |
|---|---|---|---|
| Overview | `totalAccounts`, `connectedBrokers`, `xauTraders`, `tradersWithThreeTrades`, `watch`, `shadow`, `liveCandidates`, `riskBlocked`, `shadowPnl`, `destinationRealPnl`, `mt5Healthy`, `quoteHealthy`, `tradeHealthy`, `realCopyEnabled` | `OverviewDto` same names | **yes** (query impl hardcodes dest PnL / XAU gross-net / `RealCopyEnabled=false`) |
| Brokers | `code`, `displayName`, `server`, `managerLoginMasked`, `groupCount`, `accountCount`, `connected` | `BrokerStatusDto` | **yes** (`connected` is hardcoded `true` in the query) |
| Groups | `broker`, `group`, `accounts`, `enabledForAnalysis`, `planMapping` | `GroupRowDto` | **yes** |
| Traders | `broker`, `login`, `group`, `completedXauTrades`, `netSourcePnl`, `earlyScore`, `riskScore`, flags, `state` | `TraderRowDto` | **yes** |
| Scoring | `earlyScore`, **`behaviorScore ?? 0`**, `riskScore`, `state` | `TraderRowDto` has **no** `behaviorScore` | **NO.** Column paints `0.0`. Entity `TraderScore.BehaviorScore` exists and is **not** projected. |
| Trader detail | `data.header ?? data`, `data.trades`, `positionId`, `canonicalSymbol`, `netRealizedPnl`, `isFirstThree` | `TraderDetailDto` + `TradeHighlightDto` | **yes, after 13:35 host change.** Fallback `?? data` is leftover from the old row-DTO map. |
| Trades | `id`, `login`, `canonicalSymbol`, `direction`, `openedAt`, `closedAt`, `maxVolumeLots`, `netRealizedPnl`, `completed` | EF `ReconstructedTrade` | **yes as a leak.** `BrokerId` Guid also serializes. No allow-list DTO. |
| FIX | `qualifier`, `host`, `port`, `connected`, `loggedOn`, `status`, seq, `reconnectCount`, `bid`/`ask`/`quoteAgeSeconds`, `instrumentId`, `executionEnabled` | `FixSessionDto` | **yes** (qualifier is enum `ToString().ToUpperInvariant()` → `QUOTE`/`TRADE`) |
| Risk | `killSwitch`, `realCopyEnabled`, `dailyPnl`, `xauNet`, `recentRejectReasons` | `RiskDashboardDto` | **yes** (PnL/exposure are zeros in the query) |
| Recon / Health / Settings | `JSON.stringify(data)` | hardcoded objects | **shape happens to match** dead TS `ReconciliationStatus` / `HealthStatus` / `Settings` |

Dead `types/index.ts` still uses `totalBrokers`, `tradersByState`, `Broker.id`, `Group.brokerId` — **none** of those names are on the wire. Do not type the hooks against that file without rewriting it.

---

## 9. Transport facts (why 11/11 can look “green” in a browser)

| Layer | Measured |
|---|---|
| Axios base | `VITE_API_URL` or `http://localhost:5000` |
| Vite proxy | **none** (`vite.config.ts` is port 3000 only) |
| API listen | `launchSettings` `http` profile `:5000` |
| CORS | `AllowAnyOrigin` — cross-origin from `:3000` is not blocked |
| Bearer / 401 refresh | **none** |
| `X-Correlation-Id` | **none** |
| Envelope | **none** |
| Secret sanitizer | **none** (C04: safe-by-absence of secret-bearing types) |

A62 said empty `VITE_API_URL` should mean same-origin + proxy. The stub does the opposite.

---

## 10. Stale prior reports (do not copy)

| Report | Claim | Now |
|---|---|---|
| B30 §3.1 / C04 #11 / D02 API table | `GET /api/traders/{broker}/{login}` returns the **same** `TraderRowDto` via `GetTraderAsync` | **False.** Lines 59–60 call `GetTraderDetailAsync`. Query builds `TraderDetailDto(header, highlights)`. |
| A06 / A63 “current host” | weatherforecast only | **Gone** (C04). |
| A62 `hooks.ts` 3042 B + `PUT /settings` + flatten | old stub | **Gone.** Current file 1935 B, GET-only. |
| A95 `hooks.ts` → `/api/risk/status` | hook path | Hook is `/api/risk`. |

---

## 11. Classification roll-up

| Slice | Class |
|---|---|
| 11 demo GET path strings vs 11 `MapGet`s | `EXISTS_AND_GOOD` (string match only) |
| `useTraderDetail` vs `TraderDetailDto` | `EXISTS_AND_GOOD` as a demo pair (as of 13:35) |
| Unversioned `/api/*` as the contract | `DEPRECATED` |
| Catalog `/api/v1/**` | `MISSING` |
| Envelope + typed hooks | `MISSING` |
| Mutation hooks | `MISSING` (resync correctly unwired) |
| `POST /api/ops/resync` | `UNSAFE` (anonymous) |
| `GET /api/trades` EF leak | `UNSAFE` (allow-list law; no secrets on the entity) |
| Hardcoded health / recon / settings | `EXISTS_NEEDS_REFACTOR` (demo paint; recon/health are lies) |
| `/hubs/dashboard` | `MISSING` on host; path `DEPRECATED` vs `/hubs/ops` |
| Scoring `behaviorScore` | `MISSING` on wire; page fabricates `0` |
| `types/index.ts` | `DEPRECATED` / dead |

---

## 12. Honesty — not claimed

- Not claimed: first-useful dashboard (§69 still 0/12 until live MT5 + live QUOTE + real shadow + catalog client).
- Not claimed: `/api/v1` exists.
- Not claimed: SignalR works.
- Not claimed: health / recon / settings bodies are measured from workers or Postgres.
- Not claimed: `connected: true` on brokers or `mt5Healthy` from overview is a live Manager session (C42).
- Not claimed: FIX `loggedOn` on the wire is a TLS Logon (C43 / D22 seeder forges session status).
- API process was **not** started this pass. Route list is from source, not a live OPTIONS/Swagger dump.

**Counts to quote:** hooks **11**; host maps **15** (14 GET + 1 POST); hook→host path hits **11/11**; host→hook coverage **11/15**; catalog hits **0/11**; mutation hooks **0**; SignalR maps **0**.
