# A013 — Web honesty: dummy logins, fake P&L, live-copy claims, traders timeout, secrets

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\A013_web_honesty.md` |
| Agent | A013 (senior engineer, React pages + `hooks.ts` honesty only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (source read; no HTTP launch; no `.env` values printed) |
| Workspace | `D:\Prop` (Vite app is `D:\Prop\apps\web`, **not** under `D:\Prop\src`) |
| Assigned | Read all pages under `D:\Prop\apps\web\src\pages` and `api/hooks.ts`. Dummy logins? Fake P&L? Claims live copy is on? Timeout too short for all-traders list? No secrets. Write this file. |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** Config key names only. No password, token, or `.env` value. |
| Subjects | 15 page modules + `hooks.ts` + adjacent `client.ts` (timeout) |
| Adjacent (read, not edited) | `App.tsx`, `DashboardLayout.tsx`, `client.ts`, `signalr.ts`, `types/index.ts`, `main.tsx`, `Program.cs`, `EfDashboardQueries.cs`, `DashboardModels.cs` |
| Precedence | On-disk TSX is the UI. Wire values come from unversioned `GET /api/*`. Backend literals (dest P&L `0`, `RealCopyEnabled = false`, recon zeros, health FakeMt5) are **API lies**, not invented in React — except where the page labels them as something they are not. |
| Relates | D39 (hooks), D81 / C37 (live stub), D77 / E031 (overview), E028 / A005 (15 s client + unpaged traders), E030 (demo vs live), C42 / C43 (no live MT5 / FIX) |

Classification: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **web honesty audit**. It is **not** a §68 / §69 / §70 PASS and does **not** authorize `REAL_COPY_EXECUTION_ENABLED=true`.

---

## 0. Binding answers

| Question | Answer | Class |
|---|---|---|
| Hardcoded dummy logins in the web app? | **No.** Pages do not embed `10001` / `10002` / `10003` / `99001` or any username/password form. Logins are rendered from `GET /api/traders` / detail / trades. Dummy logins exist on the **API seeder / resync**, not in React. | **EXISTS_AND_GOOD** (pages) |
| Fake P&L invented in React? | **No numeric literals.** Every P&L tile/cell is `Number(apiField).toFixed(2)`. **Label honesty FAIL** on Overview “Shadow P&L” (API field is Σ slippage) and “Dest. real P&L” / Risk “Daily P&L” (API literals `0`). Scoring paints `behaviorScore ?? 0` (field absent on wire). | **EXISTS_NEEDS_REFACTOR** (labels / zeros) |
| Does the UI claim live copy is ON? | **No.** `/live` is a static “flag is false / stay empty” stub. Overview and Risk paint `realCopyEnabled ? 'ON' : 'OFF'` from a backend constructor `false`. Copy is **never** hardcoded `ON`. Nav label `Live` is chrome, not a status. | **EXISTS_AND_GOOD** (flag text) / stub is **MISSING** book |
| Timeout too short for the all-traders list? | **Yes at §69 scale (~5k). No for the 4-login demo.** Axios `timeout: 15000`. `useTraders` has no override, no paging. `GetTradersAsync` loads all scores + all accounts + all completed-trade PnL, then the page `map`s every row. | **UNSAFE** for 5k; demo-ok |
| Secrets in the browser bundle / pages? | **None found.** No password, token, API key, or Bearer. Settings copy says secrets are never returned; `/api/settings` body has no secret fields. FIX page says password is never shown and does not render one. | **EXISTS_AND_GOOD** (absence) |

**One-liner:**

```text
WEB does not hardcode dummy logins, dest P&L, or LIVE=ON.
WEB will paint API zeros / slippage-as-P&L / FakeMt5 health if the host sends them.
15s axios + unpaged GET /api/traders will not carry ~5000 logins.
No secrets in apps/web/src.
```

---

## 1. Inventory (every page + hooks)

15 page modules under `D:\Prop\apps\web\src\pages`. Router: `App.tsx` (15 leaves). Hooks: `hooks.ts` (11 `useQuery`, 0 mutations).

| Page | File | Hook | Data source | Honesty note |
|---|---|---|---|---|
| Overview | `OverviewPage.tsx` | `useOverview` | `GET /api/overview` | Paints `realCopyEnabled` OFF from API. Labels dest/shadow P&L as P&L. |
| Brokers | `BrokersPage.tsx` | `useBrokers` | `GET /api/brokers` | Paints `connected`. Extra `**` after already-masked login. |
| Groups | `GroupsPage.tsx` | `useGroups` | `GET /api/groups` | Honest “plan mappings are labels only”. |
| Traders | `TradersPage.tsx` | `useTraders({})` | `GET /api/traders` | Full list, no error UI, no page size. |
| Trader detail | `TraderDetailPage.tsx` | `useTraderDetail` | `GET /api/traders/{broker}/{login}` | Route login from URL, not a constant. |
| Trades | `TradeExplorerPage.tsx` | `useTrades` | `GET /api/trades` | Last 200 reconstructed rows (host `Take(200)`). |
| Scoring | `ScoringPage.tsx` | `useTraders({})` | same list | “XGBoost is not active” honest. `behaviorScore ?? 0` is a **zero fabricate**. |
| Shadow | `ShadowPortfolioPage.tsx` | **none** | static copy | Says NewOrderSingle disabled + demo seed. No fake fills. |
| Live | `LiveCopyPage.tsx` | **none** | static copy | Literal “is false”. No dest rows. |
| FIX | `FixSessionsPage.tsx` | `useFixSessions` 5 s | `GET /api/fix/sessions` | “Password is never shown.” |
| Risk | `RiskPage.tsx` | `useRiskStatus` 5 s | `GET /api/risk` | Real copy ON/OFF from API. Daily P&L is API `0`. |
| Recon | `ReconciliationPage.tsx` | `useReconciliation` | `GET /api/reconciliation/status` | Raw JSON of **forged** zeros. |
| Health | `SystemHealthPage.tsx` | `useHealth` 10 s | `GET /api/health` | Raw JSON of **hardcoded** FakeMt5 `healthy: true`. |
| Audit | `AuditPage.tsx` | **none** | static copy | “RBAC is not enabled in the demo seed.” |
| Settings | `SettingsPage.tsx` | `useSettings` | `GET /api/settings` | “Secrets are never returned.” Dumps flag `false`. |

`hooks.ts` entire surface (53 lines):

| Hook | Path | Extra |
|---|---|---|
| `useOverview` | `/api/overview` | — |
| `useBrokers` | `/api/brokers` | — |
| `useGroups` | `/api/groups` | — |
| `useTraders` | `/api/traders` + `params: { broker?, state? }` | pages pass `{}` |
| `useTraderDetail` | `/api/traders/${broker}/${login}` | `enabled: !!broker && !!login` |
| `useTrades` | `/api/trades` | no filters |
| `useFixSessions` | `/api/fix/sessions` | `refetchInterval: 5000` |
| `useRiskStatus` | `/api/risk` | `refetchInterval: 5000` |
| `useReconciliation` | `/api/reconciliation/status` | — |
| `useHealth` | `/api/health` | `refetchInterval: 10000` |
| `useSettings` | `/api/settings` | — |

No `useLive`, `useShadow`, `useAudit`, `useLogin`. `types/index.ts` is **unused** (0 imports). `PnlChart` is unused.

Transport (`client.ts`):

```ts
timeout: 15000
baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000'
```

`main.tsx` QueryClient: `retry: 2`, `staleTime: 30_000`, `refetchOnWindowFocus: false`. A traders timeout therefore burns ~15 s × (1 + 2 retries) ≈ **45 s** of spinner with **no error copy** on Traders/Scoring.

---

## 2. Hardcoded dummy logins — **none in React**

Grep of `apps/web/src` for `10001`, `99001`, `10002`, `10003`, `password`, `dummy`, `admin` in page/hook modules: **0 login constants**.

What exists:

| Surface | What it does |
|---|---|
| `TradersPage` | `{t.login}` from API; link `/traders/${t.broker}/${t.login}` |
| `TraderDetailPage` | `useParams()` `brokerId` + `login` |
| `hooks.useTraderDetail` | interpolates those strings into the URL |
| `TradeExplorerPage` | `{t.login}` from reconstructed trades |
| `ScoringPage` | `{t.broker}:{t.login}` |
| `App.tsx` | route `traders/:brokerId/:login` |
| `types/index.ts` | `login: number` field on unused interfaces |

There is **no** `LoginPage`, no demo user/pass form, no `localStorage` token, no hardcoded `Authorization`.

Dummy logins **do** exist off the web tree (do not attribute to pages):

- `apps/api/Program.cs` `POST /api/ops/resync` loops `{ 10001, 10002, 10003, 99001 }`. **No React hook** calls this.
- `DemoSeeder` / `FakeMt5BrokerConnector` tape is those four logins.

**Verdict:** the dashboard will **display** canned logins if the API seeded them. It does not **author** them. An operator seeing 10001 on `/traders` is looking at the demo book, not a frontend fixture.

---

## 3. Fake P&L — React does not invent numbers; labels can lie

### 3.1 What pages paint

| UI label | Binding | Frontend invents value? | Backend truth (this tree) |
|---|---|---|---|
| Overview **Shadow P&L** | `data.shadowPnl` | no | `Sum(ShadowOrders.SourceVsShadowSlippage)` — **slippage, not P&L** |
| Overview **Dest. real P&L** | `data.destinationRealPnl` | no | constructor literal **`0`** (`GetOverviewAsync`) |
| Traders **Net P&L** | `t.netSourcePnl` | no | Σ completed `ReconstructedTrades.NetRealizedPnl` per login |
| Detail **Net P&L** | `h.netSourcePnl` | no | same header row |
| Detail / explorer trade **Net** | `t.netRealizedPnl` | no | reconstructed trade field |
| Risk **Daily P&L** | `data.dailyPnl` | no | `RiskDashboardDto` first arg **`0`** |
| Risk **XAU net** | `data.xauNet` | no | **`0`** |
| Live dest P&L | — | n/a | page has **no** P&L node |
| `PnlChart` | unused | n/a | no page passes series |

Trader-row `TraderRowDto.ShadowPnl` is hardcoded **`0`** in `GetTradersAsync` and **never read** by any page. Overview 248.20 (demo) vs row 0 is an **API inconsistency**, not a React fake.

### 3.2 Frontend-side honesty defects (not invented dest books)

1. **Wrong noun:** Overview tile “Shadow P&L” will show slippage Σ and look like profit. That is a **label lie** if the operator treats it as shadow mark-to-market.
2. **Honest zero, dishonest capability:** Dest real P&L and Risk daily P&L will show `0.00` even if a live dest book existed — the query never reads dest positions (table **MISSING**). The number is not a random fake; it is a **floor that cannot become non-zero**.
3. **Scoring `behaviorScore ?? 0`:** `TraderRowDto` has no `behaviorScore`. The column always paints `0.0`. That **is** a frontend fabricate (default), not dest P&L.
4. **`Number(undefined).toFixed(2)` → `"NaN"`** if a field is missing. Demo DTO names match; stale `types/index.ts` (`pnl`, `realPnl`) is unused and must not be wired.

**Do not** treat painted source-tape P&L on canned deals as live-venue P&L. Algorithm is real; tape is demo (E030).

---

## 4. Live-copy claims — **no page says ON as a fact**

### 4.1 Surfaces that mention live / real copy

| Surface | Exact claim | Sourced? |
|---|---|---|
| `LiveCopyPage` H1 | “Live copy portfolio” | chrome title |
| `LiveCopyPage` body | “`REAL_COPY_EXECUTION_ENABLED` is false. This page will stay empty until go-live gates pass.” | **JSX literal**, not `useSettings` / overview |
| Nav | `{ to: '/live', label: 'Live' }` | abbreviated §46 label |
| Overview footer | `Real copy execution: {data.realCopyEnabled ? 'ON' : 'OFF'}`. “Trade #3 never auto-promotes to LIVE.” | API last ctor arg **`false`** |
| Overview tile | “Live **candidates**” = `data.liveCandidates` (state count, not dest) | API |
| Risk tile | `Real copy` = same ternary, always amber class | API `false` |
| Shadow copy | “Live NewOrderSingle remains disabled.” | static, directionally true |
| Trader detail footer | “Live promotion is not automatic.” | static, true vs `CanPromoteToLive => false` |
| Settings dump | `featureFlags.REAL_COPY_EXECUTION_ENABLED: false` | API hardcoded dictionary |

`GetFixSessionsAsync` last arg `executionEnabled: false` (literal). FIX page prints `String(s.executionEnabled)` → `false`.

### 4.2 What would be a lie (not present)

| Forbidden claim | Present? |
|---|---|
| Hardcoded `ON` / `enabled: true` for real copy in TSX | **No** |
| Invented dest positions / `clOrdId` | **No** |
| `PnlChart` of live dest P&L | **No** (widget unused) |
| Overview treating `data.live` (LIVE state count) as “copied” | Page **does not render** `data.live` |
| Enable / flatten / send buttons | **No** |

### 4.3 Residual honesty nits (not “live is on”)

- `/live` flag is a **string**, so if a later wave flips the API flag the page still says false. Directionally safe (fail-closed text), not a live read.
- Sidebar `Live` can be read as “we are live.” The page body contradicts that. Prefer §46 `Live Copy Portfolio`.
- Overview / Risk will paint **ON** if the API ever returns `realCopyEnabled: true` **without** a wired send gate. That is an API/control problem, not a current UI hardcode.

**Verdict:** the web app **does not claim live copy is on.** It claims the opposite, sometimes from a literal.

---

## 5. All-traders list timeout — **15 s is too short at 5k**

### 5.1 Client

`D:\Prop\apps\web\src\api\client.ts` L5: `timeout: 15000`.

`useTraders` (hooks L16–21):

- no `timeout` override
- no `placeholderData` / infinite query
- `queryKey: ['traders', filters]` with pages passing `{}` → **unfiltered full list**
- used by **both** `TradersPage` and `ScoringPage` (two full downloads)

`TradersPage` has loading text, **no `error` branch** (unlike Overview). A timeout surfaces as an empty-looking table (`data = []`) after retries, or a thrown query with a blank page depending on react-query error bubbling. Either way: **no “timeout / start API” copy**.

### 5.2 Host work behind that 15 s

`GetTradersAsync` (`EfDashboardQueries.cs` L74–117):

1. `TraderScores.ToListAsync()` — **all** scores
2. `Brokers.ToDictionaryAsync()` — all brokers
3. `Mt5Accounts.ToListAsync()` — **all ingested accounts** (join only)
4. `ReconstructedTrades` completed, grouped PnL — **all** completed trades
5. In-memory filter + `OrderByDescending(EarlyScore)`
6. **No `Skip`/`Take`. No envelope. No page size.**

`GET /api/traders` is then JSON of the entire array. Axios must download + parse inside 15 s.

### 5.3 Scale

| Book | Rows | 15 s timeout |
|---|---|---|
| Demo seed | 4 scored logins | **OK** |
| Architecture §69.3 | ~5 000 accounts | **NOT SAFE** — full table + full account load + PnL group-by + no virtualization |
| `LiveIngestHostedService` (if scoring every login) | up to every stored login | same |

A005 already: “Not a 5k-safe UI.” This pass reconfirms from the page/hook side.

Also: `useTrades` pulls host `Take(200)` only (host-side). Traders list has **no** equivalent cap. Positions ingest `Take(200)` is **not** a traders-page cap.

**Verdict:** timeout is **too short for the all-traders list at the stated census**. It is not a demo-blocker. Do not raise timeout as a substitute for paging (`A92` envelope). Do not claim the leaderboard can show ~5 000 live Manager logins.

---

## 6. Secrets — **none in pages / hooks / client**

Scoped grep of `apps/web/src` `*.ts` / `*.tsx` (excluding lockfile `js-tokens`):

| Pattern | Hits in product TS/TSX |
|---|---|
| `password` | FIX copy: “Password is never shown.” Settings: none. |
| `secret` / `apiKey` / `Bearer` / `Authorization` | **0** |
| Hardcoded tokens | **0** |
| `VITE_*` | `VITE_API_URL` host only (`client.ts`, `signalr.ts`) |
| `.env*` under `apps/web` | **none** (E028) |

Settings page (`SettingsPage.tsx`):

- Copy: “Secrets are never returned to the browser.”
- Renders `JSON.stringify(data)` of `/api/settings`.
- Host body: `riskLimits`, `featureFlags.REAL_COPY_EXECUTION_ENABLED = false`, two broker **labels**. **No** MT5 password, **no** FIX password, **no** connection string.

FIX page renders `host:port`, seq, bid/ask — **not** Sender password / `RawData`.

Brokers page: `{b.managerLoginMasked}**` — numeric mask from `MaskLogin` (truncates last two digits), plus extra asterisks. That is a **manager login id**, already reduced, not a password. Do not treat it as a secret leak. Do not unmask.

`GET /api/trades` serializes raw EF entities (Guid `BrokerId`, lots, prices). **No password columns** on `ReconstructedTrade`. Still an allow-list FAIL (D39), not a secret FAIL.

SignalR `startConnection()` is anonymous. CORS `AllowAnyOrigin` is an **auth hole**, not a printed secret.

**This report does not read `D:\Prop\.env`.** API does not load it into the Vite bundle.

---

## 7. Adjacent honesty (painted lies the pages will show)

Not invented in TSX; operators will still believe the tiles.

| If the operator looks at… | They will see | Truth |
|---|---|---|
| Overview **MT5 health OK** | `data.mt5Healthy` | `brokers > 0`, not Manager (C42) |
| Brokers **connected** | green `connected` | query literal `true` |
| Health JSON | ACHIEVER `healthy: true` | hardcoded FakeMt5 footnote |
| Recon JSON | zeros + `lastReconciliation: now` | hardcoded in `Program.cs` |
| FIX bid 2399.45 | numbers | invented dest quote (seeder) |
| Overview dest P&L `0.00` | a real P&L tile | literal 0 |
| Scoring Behavior `0.0` | a score | missing field |

These are **in scope** because the assigned pages **render them without a footnote** (except Overview “Live FIX send is off” and Live/Shadow stubs).

---

## 8. Scorecard

| Check | Result |
|---|---|
| Dummy logins hardcoded in pages/hooks | **PASS** (none) |
| Dummy logins may appear from API seed | **DEMO** (not a web author) |
| Dest / live P&L invented in React | **PASS** (none) |
| Shadow P&L label = slippage | **FAIL** (noun) |
| Dest / daily P&L stuck at API 0 | **FAIL** as a live book; **honest zero** as a number |
| `behaviorScore ?? 0` | **FAIL** (frontend zero) |
| Live copy claimed ON | **PASS** (never) |
| Live flag from API on `/live` | **FAIL** (literal) |
| 15 s timeout vs 4 demo rows | **PASS** |
| 15 s timeout vs ~5k unpaged list | **FAIL / UNSAFE** |
| Secrets in web source | **PASS** (none) |
| Settings/FIX promise vs body | **PASS** (no secret fields) |

§73.B pages as a demo paint layer: `EXISTS_NEEDS_REFACTOR`.  
§73.B as a live copy / 5k leaderboard: `MISSING`.  
§73.B secret handling: `EXISTS_AND_GOOD` by absence.

---

## 9. What a later wave must not do

1. Do **not** hardcode `10001` et al. into React to “help” empty states.
2. Do **not** invent dest P&L, dest rows, or a green **ON** to make `/live` look finished.
3. Do **not** copy `netSourcePnl` into dest / live fields.
4. Do **not** “fix” 5k by only raising `timeout` past 15 s. Page + envelope first.
5. Do **not** dump `/api/settings` if a later host adds secret-bearing fields; the current `JSON.stringify` is safe **only** because the body is flags/labels.
6. Do **not** print `.env` or Manager/FIX passwords in follow-up reports.
7. Do **not** treat this file as permission to enable send.

---

## 10. Evidence pins

- Pages: `D:\Prop\apps\web\src\pages\` — 15 files listed in §1.
- Hooks: `D:\Prop\apps\web\src\api\hooks.ts` (11 GETs; traders L16–21).
- Timeout: `D:\Prop\apps\web\src\api\client.ts` L5 `timeout: 15000`.
- Live stub: `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` (literal false; 0 hooks).
- Overview copy: `OverviewPage.tsx` L14 “Live FIX send is off”; L31 `realCopyEnabled ? 'ON' : 'OFF'`.
- Risk: `RiskPage.tsx` L12 same ternary; L13 daily P&L.
- Traders query: `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` L74–117 (no Take); L36 dest P&L `0`; L42 `RealCopyEnabled` `false`; L196 risk zeros + `false`.
- Settings host: `D:\Prop\apps\api\Program.cs` L42–47 (flag false, no secrets); L79 dummy logins **API-only**.
- Prior: D39, D81, A005, E028, E030, E031.

---

## One-line close

**React does not hardcode dummy logins, dest P&L, secrets, or “live copy ON”; it will faithfully paint API zeros/slippage and will time out a ~5k unpaged `/api/traders` at 15 s.**
