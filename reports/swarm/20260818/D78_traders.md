# D78 — `TradersPage.tsx` vs architecture §50 leaderboard

| Field | Value |
|---|---|
| Agent | D78 (senior engineer, Traders / leaderboard page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:45:04+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `TradersPage.tsx`. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\TradersPage.tsx` |
| Route | `/traders` via `App.tsx` `path="traders"` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full `read_file` of the page (42 physical lines, LF-only). Cross-read `useTraders`, `App.tsx`, `DashboardLayout`, `TraderDetailPage`, `ScoringPage`, `types/index.ts`, `formatters.ts`, `StatusBadge`, `client.ts`, `Program.cs` `GET /api/traders`, `TraderRowDto`, `GetTradersAsync`, `TraderState`, `TraderScore`, `DemoSeeder`. PowerShell SHA-256 + bytes + physical-line + last-write + `git hash-object`. Compare architecture §50, A26 §6.4, A62 §10.4, **A92** (wins on this resource). |
| Binding law | Architecture v2 **§50** (columns + 8 filter words). A92 is the implementer contract for `GET /api/v1/traders`. A26 §5.2 route `/traders`. A62 persist-in-URL + `TraderLeaderboardPage`. A52 / A92 L6: `mlProbability` is `null` → render `—`. A69 / §22 state tokens. A57 / §69 item 8 (rank traders). |
| Prior | A92 (DTO + filters + sort + envelope), A26 §6.4 (sketch; 0–1 scores **superseded**), A62 §10.4, B20 / B29 / B30 / C08 / C13 / C23 / D08 / D21 / D38 / D39 |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **single-page census** of the React leaderboard. It is **not** a claim that `/api/v1/traders` exists, that A92 ranking is implemented, that §69 item 8 is accepted, or that the dashboard is first-useful.

---

## 0. Verdict

**The `/traders` chrome and a 9-column demo table exist. The architecture §50 / A92 trader leaderboard does not.**

`TradersPage` is a 42-line anonymous table over `useTraders({})` → `GET /api/traders`. It paints broker **code**, login (as a detail link), group, completed-XAU count, net P&L, early score, risk score, a collapsed MG/AVG/ESC flag cell, and state. It has **no** filter bar, **no** sort control, **no** pagination, **no** URL query persistence, **no** envelope unwrap, **no** empty-state copy, **no** error UI, and **no** four of the sixteen §50 columns (ML probability, Shadow P&L, Live allocation, Last scored).

The file SHA-256 `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` is **unchanged vs D08 / C08 / B22**. Last write `2026-08-18T13:16:00+05:30`. The page is **untracked** (`??`) vs HEAD `398a142` — a clean checkout does not contain this file.

| Layer | Required | Now | Class |
|---|---|---|---|
| §46 nav label | `Traders` | **`Traders`** | **EXISTS_AND_GOOD** |
| A26 route | `/traders` | `/traders` | **EXISTS_AND_GOOD** |
| A62 module | `pages/traders/TraderLeaderboardPage.tsx` | `pages/TradersPage.tsx` (1604 B, 42 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Hook | A92 query object + `/api/v1/traders` | `useTraders({})` → `GET /api/traders?broker&state` | **EXISTS_NEEDS_REFACTOR** |
| API | `GET /api/v1/traders` + envelope | anonymous `MapGet("/api/traders")` returns a **bare array** | **MISSING** as A92; demo map **EXISTS_NEEDS_REFACTOR** |
| §50 columns painted | 16 | **9** headers (flags collapsed; 4 columns absent) | **EXISTS_NEEDS_REFACTOR** |
| §50 / A92 filters | 8 words / full A92 dictionary | **0** in the UI (`{}`) | **MISSING** |
| Default sort | A57 chain + NULLS LAST | server `OrderByDescending(EarlyScore)` only | **MISSING** |
| Persist filters in URL | A62 / A92 | **0** `useSearchParams` | **MISSING** |
| Detail identity | `/traders/{brokerId}/{login}` UUID | `/traders/${t.broker}/${t.login}` **code** | **EXISTS_NEEDS_REFACTOR** (demo works; A26 does not) |
| Auth / RBAC | A51 `dash.read` | none | **MISSING** |
| SignalR patches | A97 | page never subscribes | **MISSING** |

**§73.B for this page as a file / route / sidebar leaf:** `EXISTS_NEEDS_REFACTOR`.  
**§73.B for this page as the §50 / A92 leaderboard:** `MISSING`.  
**§69 item 8 (rank traders):** still **not accepted**. A demo `ORDER BY early_score DESC` over four seeded logins is not the ranking query.

**One-line:** `/traders` shows a demo score table; it is not the leaderboard.

| If the question means… | Answer |
|---|---|
| Is there a Traders page file, route, and nav row? | **Yes.** |
| Does it list demo `TraderRowDto`s when the API is up? | **Yes** (4 seed logins after `DemoSeeder`). |
| Is architecture §50 implemented? | **No.** |
| Must first-useful (§69) ship this React table? | **It must ship a ranked `/api/v1/traders` view.** This file is not that view. |

This report does **not** authorize creating or rewriting product files.

---

## 1. Measured files

| Path | Bytes | Phys. lines | SHA-256 | Last write (local) | Git |
|---|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | 1604 | 42 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` | 2026-08-18T13:16:00+05:30 | **untracked** (`??`); worktree blob `fe06db74dc8398a847ae4df0e19571adea59a292` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | unstaged (`M`); `useTraders` L16–21 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 | axios → `VITE_API_URL` or `http://localhost:5000` |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | unstaged (`M`); L27 `path="traders"` |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38+05:30 | unstaged (`M`); nav row `Traders` → `/traders` |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | 2402 | 56 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` | 2026-08-18T13:35:59+05:30 | **untracked**; grown vs D08 (1592 / `6CAE0FC9…`) |
| `D:\Prop\apps\web\src\pages\ScoringPage.tsx` | 1288 | 33 | `F417592E7ECC16F19BA7D547BDE8B29DC2882DDCAE1925487EAF159D836BB185` | 2026-08-18T13:16:43+05:30 | **untracked**; also `useTraders({})` |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | **UNUSED** by this page; `Trader` shape ≠ wire |
| `D:\Prop\apps\web\src\utils\formatters.ts` | 947 | 31 | `FD0214751EA05923973B4EBD73E35EE21AC3D253FDE61538F4616EC2DC3B6F66` | 2026-08-18T13:08:39+05:30 | `pnlColor` unused here |
| `D:\Prop\apps\web\src\components\StatusBadge.tsx` | 699 | 17 | `F6ECFE83230269617140C286CFFA0F065D488EF5FB665FA75E8E4586D60F807E` | 2026-08-18T13:08:38+05:30 | unused (0 imports) |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39+05:30 | `staleTime: 30_000`, `retry: 2` |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | L57–58 list; L59–60 detail; **JsonStringEnumConverter on** |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | `TraderRowDto` L43–57 |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15+05:30 | `GetTradersAsync` L74–117 |
| `D:\Prop\src\Domain\Enums\TraderState.cs` | 264 | 15 | `E509C59F1597EE0F424A9F9408D0B27B2C7063D724B3D0A63880E2558B930D68` | 2026-08-18T13:03:45+05:30 | A69 tokens |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | 652 | 19 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | 2026-08-18T13:08:41+05:30 | current-row scores |

Page is LF-only (42 `LF`, 0 `CR`). SHA matches D08 row 15. This agent did **not** create, edit, or stage the page.

`git ls-files` for `apps/web/src/pages/TradersPage.tsx`: **pathspec did not match**. HEAD has no traders page.

---

## 2. Full page (verbatim)

42 physical lines. Entire module:

```tsx
import { Link } from 'react-router-dom';
import { useTraders } from '../api/hooks';

export default function TradersPage() {
  const { data = [], isLoading } = useTraders({});
  if (isLoading) return <p className="text-gray-400">Loading traders…</p>;
  return (
    <div>
      <h1 className="text-2xl font-semibold text-white mb-4">Trader leaderboard</h1>
      <table className="w-full text-sm text-left">
        <thead className="text-gray-400 border-b border-gray-800">
          <tr>
            <th className="py-2">Broker</th>
            <th>Login</th>
            <th>Group</th>
            <th>XAU trades</th>
            <th>Net P&L</th>
            <th>Early</th>
            <th>Risk</th>
            <th>Flags</th>
            <th>State</th>
          </tr>
        </thead>
        <tbody>
          {data.map((t: any) => (
            <tr key={`${t.broker}-${t.login}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.broker}</td>
              <td><Link className="text-blue-300" to={`/traders/${t.broker}/${t.login}`}>{t.login}</Link></td>
              <td>{t.group}</td>
              <td>{t.completedXauTrades}</td>
              <td>{Number(t.netSourcePnl).toFixed(2)}</td>
              <td>{Number(t.earlyScore).toFixed(1)}</td>
              <td>{Number(t.riskScore).toFixed(1)}</td>
              <td>{[t.martingale && 'MG', t.averagingDown && 'AVG', t.lotEscalation && 'ESC'].filter(Boolean).join(' ') || '—'}</td>
              <td>{t.state}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
```

Default export name `TradersPage` matches `App.tsx` import. No other exports. No tests under `apps/web` reference this file.

---

## 3. Mount, hook, and wire path

```text
BrowserRouter
  DashboardLayout          nav "Traders" → /traders  (exact §46 label)
    Route path="traders"   <TradersPage />
      useTraders({})
        queryKey ['traders', {}]
        GET {baseURL}/api/traders     // no query string
          Program.cs L57–58
            IDashboardQueries.GetTradersAsync(broker: null, state: null)
              EfDashboardQueries: load all scores/brokers/accounts/pnls
              filter none; OrderByDescending(EarlyScore)
              IReadOnlyList<TraderRowDto>   // bare array, not A92 envelope
```

`useTraders` (hooks.ts L16–21) **does** accept `{ broker?: string; state?: string }` and forwards them as axios `params`. This page always passes `{}`. The other consumer, `ScoringPage`, also passes `{}`. The two-filter hook is therefore **dead on both pages**.

Inherited QueryClient (`main.tsx`): retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000`. **No** `refetchInterval` (FIX/risk poll; this list does not). **No** `isError` / `error` destructure. **No** `useMutation`. **No** SignalR.

Axios `baseURL` is `import.meta.env.VITE_API_URL || 'http://localhost:5000'`. Vite has **no** `/api` proxy (`vite.config.ts` 169 B). CORS on the API is `AllowAnyOrigin`.

ASP.NET `ConfigureHttpJsonOptions` adds `JsonStringEnumConverter` (Program.cs L10–13). Default property naming is camelCase. The demo row the page actually binds is:

| JSON key the page reads | C# `TraderRowDto` | Painted? |
|---|---|---|
| `broker` | `Broker` (registry **code**) | yes |
| `login` | `Login` (`long`) | yes (link text) |
| `group` | `Group` (`string?`) | yes |
| `completedXauTrades` | `CompletedXauTrades` | yes |
| `netSourcePnl` | `NetSourcePnl` | yes, `toFixed(2)` |
| `earlyScore` | `EarlyScore` (`decimal`, **non-null**) | yes, `toFixed(1)` |
| `riskScore` | `RiskScore` (`decimal`, **non-null**) | yes, `toFixed(1)` |
| `martingale` / `averagingDown` / `lotEscalation` | flattened bools | yes, collapsed |
| `state` | `TraderState` → **string token** | yes |
| `mlProbability` | `MlProbability` (`null`) | **no** |
| `shadowPnl` | `ShadowPnl` (literal `0`) | **no** |
| `lastScored` | `LastScored` | **no** (A92 name is `lastScoredAt`) |

A92 row fields this DTO / page never have: `brokerId`, `brokerCode`, `brokerDisplayName`, `groupId`, nested `flags`, `behaviorScore`, `liveAllocation`, envelope `data/page/pageSize/totalItems/totalPages/generatedAt/query`.

**B29 is stale on this page.** B29 said `{t.state}` would print `2` without `JsonStringEnumConverter`. That converter is now registered. Demo state paints `WATCH` / `SHADOW` / `INSUFFICIENT_DATA`, not the ordinal. Do not repeat the numeric-enum claim against this `Program.cs` SHA.

---

## 4. Architecture §50 column matrix

Verbatim §50 columns (`…Architecture_v2.md` L1838–1856) vs this `<thead>`:

| # | §50 column | Page header | Bound field | Status |
|---:|---|---|---|---|
| 1 | Broker | Broker | `t.broker` (**code**, not display name) | **PARTIAL** — wrong identity |
| 2 | Login | Login | `t.login` as `<Link>` | **PRESENT** |
| 3 | Group | Group | `t.group` | **PRESENT** (null renders empty) |
| 4 | Completed XAU trades | XAU trades | `t.completedXauTrades` | **PRESENT** (abbreviated label) |
| 5 | Net source P&L | Net P&L | `Number(t.netSourcePnl).toFixed(2)` | **PRESENT** (abbreviated; uncolored) |
| 6 | Early score | Early | `Number(t.earlyScore).toFixed(1)` | **PRESENT** (1 dp; A92 is 2 dp) |
| 7 | ML probability | — | (wire `mlProbability` is `null`) | **MISSING** — A52/A92 require column + `—` |
| 8 | Risk score | Risk | `Number(t.riskScore).toFixed(1)` | **PRESENT** (1 dp) |
| 9 | Martingale flag | Flags | `MG` token | **COLLAPSED** |
| 10 | Averaging-down flag | Flags | `AVG` token | **COLLAPSED** |
| 11 | Lot escalation flag | Flags | `ESC` token | **COLLAPSED** |
| 12 | Current state | State | `t.state` | **PRESENT** (raw token; no `TraderStateBadge`) |
| 13 | Shadow P&L | — | DTO `shadowPnl` is literal `0` | **MISSING** |
| 14 | Live allocation | — | **no DTO field** | **MISSING** (0 until live copy is law; still a column) |
| 15 | Last scored | — | DTO `lastScored` unused | **MISSING** |

**Painted headers: 9 / 16.** Three flags share one cell. Four columns are absent. `behaviorScore` is correctly **not** a §50 column (A92 §9.3); ScoringPage is the sibling that tries to show it and reads `t.behaviorScore ?? 0` — that field is **not on `TraderRowDto`**, so Scoring always paints `0.0`. Out of this page’s paint, but it is the same payload.

§50 filters (verbatim L1861–1869): `broker`, `group`, `state`, `score`, `risk`, `trade count`, `martingale`, `date`. **UI count: 0.** No `<input>`, no `<select>`, no chips, no `useSearchParams`.

A62 §10.4 also requires `q` (login contains) and persist-in-URL. **0** hits for `searchParams`, `useSearchParams`, `minCompletedXauTrades`, `page`, `sort` under this file.

---

## 5. A92 contract vs what the page assumes

A92 **wins** for `GET /api/v1/traders`. This page would **break or lie** if the host were swapped to A92 without a rewrite.

| A92 rule | Page behavior | Result if A92 shipped as-is |
|---|---|---|
| Path `/api/v1/traders` | Hook calls `/api/traders` | 404 unless a rewrite remains |
| Envelope `{ data, page, pageSize, totalItems, … }` | `data = []` then `data.map` | **TypeError** (`data.map is not a function`) — axios body is the envelope object |
| Seed `minCompletedXauTrades=3` for the useful view | no query | Unscored rows stay in the list |
| Default sort A57 chain + NULLS LAST | none; server EarlyScore DESC | Unscored `N<3` with a leaked quality number (C23: **40.00** on 10003) ranks as a real score |
| `earlyScore` / `riskScore` **null** when `N<3` (L7) | `Number(null).toFixed(1)` → **`"0.0"`** | Paints a fake official zero. **Forbidden.** |
| `mlProbability` null → render `—` | column absent | Cannot honor L6 |
| Flags nested `flags.martingale` | reads top-level `t.martingale` | All flags become `—` |
| Identity `brokerId` UUID in the path | `t.broker` is `ACHIEVER` / `STARWAVEFX` | Detail URL is not A26; demo API happens to resolve **code** |
| `page` / `pageSize` (default 50, max 200) | full array | Fine on 4 seed rows; **UNSAFE** at ~5k |
| Unknown query key → 400 | page sends nothing | n/a |
| Auth Bearer | none | 401 would become empty table (see §6) |
| Row `additionalProperties` forbidden | `t: any` | Type hole; extra keys are not auto-printed |

Honest pairing today: **demo array ↔ demo page**. That coincidence is why the table lights up after `DemoSeeder`. It is not forward-compatible with A92.

---

## 6. Runtime behavior (measured from source; API not launched)

| Event | What the user sees |
|---|---|
| First paint, query pending | `Loading traders…` only. Title and table unmounted. |
| 200 + 4 seed rows | Table of ACHIEVER 10001/10002/10003 and STARWAVEFX 99001, sorted by `EarlyScore` DESC on the server. |
| 200 + `[]` | Title + empty `<tbody>`. **No** “no traders” copy. |
| HTTP 4xx/5xx after 2 retries | `isLoading` false, `data` undefined → default `[]` → **same empty table**. `isError` is ignored. Fail-open to “nobody exists.” |
| `data` is an object (A92 envelope) | `data.map` throws. React error overlay. No error boundary in `App.tsx`. |
| `earlyScore` / `netSourcePnl` missing | `Number(undefined).toFixed(…)` → **`NaN`** in the cell. |
| `earlyScore` JSON `null` (A92 L7) | `Number(null) === 0` → **`0.0`**. Looks official. |
| Flag all-false | em dash `—` (correct empty glyph for the collapsed cell). |
| Click login | `/traders/ACHIEVER/10001` (code, not UUID). `TraderDetailPage` reads `useParams().brokerId` and passes it to `GET /api/traders/{broker}/{login}`. `GetTraderDetailAsync` resolves `Brokers.Code`. **Demo navigates.** A26 UUID path would 404 on this host. |
| Stay on `/traders/ACHIEVER/10001` | Sidebar `NavLink` to `/traders` is **active** (RR v6 prefix match). Intended. |

Row key `` `${t.broker}-${t.login}` `` is the right compound identity given §10 (login is never globally unique). That is one thing this page gets right.

`formatters.pnlColor` / `pnlBg` exist and are unused: P&L is always `text-gray-200`. `StatusBadge` is unused: state is raw text.

---

## 7. Demo payload the table will show (not live-probed)

`DemoSeeder` rebuilds logins `10001`, `10002`, `10003`, `99001`. C23 measured 10003 as `INSUFFICIENT_DATA` with persisted quality **40.00** (cap leak). `GetTradersAsync` still emits non-null `EarlyScore` / `RiskScore` and sorts DESC by that number, so 10003 can sit **above** a weaker official score. The page has no `N < 3` guard and no `—` for unofficial rows.

`GetTradersAsync` P&L is `SUM(NetRealizedPnl)` over **all completed** reconstructed trades, **not** canonical XAUUSD only (D21 Q3 / A92 L14). The column header says “Net P&L” / “XAU trades” as if they were the same universe. They are not, the moment a second symbol exists.

`ShadowPnl` is hardcoded `0` in the query (D21 Q9). Even if the page added the §50 column tomorrow, it would paint `0.00`, not the six demo `shadow_orders` D48 measured. Do not read a future Shadow column on this DTO as A24.

`MlProbability` is literal `null` — **correct** vs A52. The page simply never shows it.

---

## 8. Identity and navigation (broker code vs `brokerId`)

| Surface | Identity used |
|---|---|
| A26 / A62 / A92 | `{ brokerId: uuid, login }` |
| `TraderRowDto.Broker` | `brokers.Code` (`ACHIEVER`, `STARWAVEFX`) |
| Page cell | that code |
| Page `<Link>` | `/traders/${t.broker}/${t.login}` |
| `App.tsx` param name | `:brokerId` |
| `TraderDetailPage` | `useParams().brokerId` (holds the **code** today) |
| API detail map | `{broker}` string compared to `Brokers.Code` |

C08 already recorded this. Remeasured: **unchanged** on `TradersPage` (same SHA). `TraderDetailPage` has grown (2402 B / `C849449B…` at 13:35:59) and still consumes the same param. The leaderboard is the producer of the wrong identifier.

`types/index.ts` `Trader` uses `brokerId`, `completedTrades`, `pnl`, `score`, `riskFlags[]` — **zero imports**. Dead sketch. The live page correctly does **not** use it (the sketch would not bind).

---

## 9. What this page is not

| Claim | Measured |
|---|---|
| A62 `pages/traders/TraderLeaderboardPage.tsx` | **No such path.** Flat `pages/TradersPage.tsx`. |
| A92 `searchParams.ts` codec | **No file.** |
| Filter bar / sort headers / pager | **0** |
| `TraderStateBadge` | **0** |
| ECharts / sparkline | **0** (correct — those belong on §51 detail) |
| `POST .../copy-control` | **0** (correct — A92 L13: GET is read-only) |
| RBAC hide of DISQUALIFIED / etc. | **0** — every scored row in the table |
| Live allocation / Models / ML | **0** — and must stay honest-null, not `0` |
| Test | **0** files under `apps/web` import `TradersPage` |

---

## 10. Stale prior claims (use this file for the page)

| Prior | This pass |
|---|---|
| A62 §3 / A57: pages missing / weatherforecast | **Stale.** File + demo `GET /api/traders` exist. |
| A57 item 8 “MISSING ranking query/API” | **Still not accepted.** Demo `OrderByDescending(EarlyScore)` over an in-memory full scan is not A92. D41 §69 remains 0/12. |
| B29: state prints as `2` | **Stale** vs `Program.cs` SHA `61B1E0D1…` (`JsonStringEnumConverter`). |
| B30: hook uses `minScore`/`maxScore` and `{ items, total }` | **Stale.** Current hook is `{ broker?, state? }` and returns the raw array. |
| A92 §1 “React stub uses minScore/maxScore + envelope” | **Stale** as a description of `hooks.ts` SHA `5FDC969C…`. The **contract** in A92 is not stale. |
| D08 / C08 SHA `0AF0FF5B…` | **Still the page.** Unchanged since 13:16:00. |
| D21 `GetTradersAsync` catalog | Still the server behind this table. D21 file-size (7407 B) is **stale**; current query file is 8708 B / `328D0924…` after detail-DTO work. The leaderboard method body is the same shape (4 full-set reads, EarlyScore DESC). |

---

## 11. Classification summary

| Subject | §73.B |
|---|---|
| `TradersPage.tsx` as a routed module | `EXISTS_NEEDS_REFACTOR` |
| Sidebar + `/traders` path | `EXISTS_AND_GOOD` |
| §50 column set | `EXISTS_NEEDS_REFACTOR` (9/16 painted; flags collapsed) |
| §50 / A92 filter + sort + page | `MISSING` |
| A92 `/api/v1/traders` client | `MISSING` (demo `/api/traders` only) |
| Detail link compound key | `EXISTS_NEEDS_REFACTOR` (code, not UUID) |
| `t: any` + unused `types/index.ts` | `EXISTS_NEEDS_REFACTOR` |
| Error → empty table | `UNSAFE` as an ops surface (silence on 5xx) |
| `Number(null)` score paint | `UNSAFE` **if** A92 nulls are introduced without a page change |
| Password / secrets on this page | `EXISTS_AND_GOOD` (no secret fields bound; none on `TraderRowDto`) |
| §69 item 8 | **FAIL** (not accepted) |

Do **not** treat “Trader leaderboard” in the `<h1>` as evidence that ranking is done.

---

## 12. Next increment (not done this pass)

When someone is authorized to touch product source, the replace — not grow — path is A92 + A62 §10.4:

1. New versioned client against `GET /api/v1/traders` with the A92 query object; unwrap `data[]`; persist filters in the URL.
2. Sixteen columns; `mlProbability` / null scores render `—`; flags as three columns or an explicit badge set that still maps 1:1.
3. Detail href `/traders/{brokerId}/{login}` using the UUID, not `brokers.Code`.
4. Seed `minCompletedXauTrades=3` for the useful view; pager; A57 sort echo.
5. `isError` fail-closed. Never default a failed GET to `[]` without a message.
6. Leave Models / live allocation honest (`null` / `0` with label), and do **not** invent ML.

Until that ships, this file remains a demo table on an unversioned list.

**Product source edited this pass: 0.**
