# D77 — `OverviewPage.tsx` vs architecture §47

| Field | Value |
|---|---|
| Agent | D77 (senior engineer, Overview page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:43:05+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `OverviewPage.tsx`. Write this file. **Do not modify product source.** |
| Target | `D:\Prop\apps\web\src\pages\OverviewPage.tsx` |
| Adjacent (read, not edited) | `MetricCard.tsx`, `hooks.ts`, `client.ts`, `signalr.ts`, `types/index.ts`, `utils/formatters.ts`, `App.tsx`, `DashboardLayout.tsx`, `main.tsx`, `DashboardModels.cs` `OverviewDto`, `EfDashboardQueries.GetOverviewAsync`, `apps/api/Program.cs` `GET /api/overview`, `DemoSeeder.cs` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Method | Full `read_file` of the page (35 lines) + MetricCard + hook + C# DTO + query + `Program.cs` map; PowerShell SHA-256 / bytes / physical-line / last-write; `git` identity vs HEAD `398a142`; live `GET http://127.0.0.1:5000/api/overview` (HTTP 200 captured); grep for types imports, click-through, tests; compare architecture §47, A26 §5.2–§5.3 / §6.1, A62 §10.1, A91 tile contract, D08 §7.1, D21 §5.1, D39 hook row |
| Binding law | Architecture **§47** (18 tiles); A26 §6.1 envelope; A91 health enums + click-through; A62 layout modules; A48/A49 flags (header, not extra tiles); C43 / D32 honesty on FIX |
| Prior census | A91 §0 “page **MISSING**”; A62 §4 §47 **MISSING**; A29 U01 **MISSING**; D08 §7.1 (12 cards, SHA already `6497193F…`); C13 “demo counts + liar MT5/FIX health” |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Live API | **Yes.** `GET /api/overview` returned the JSON in §7. The page itself was **not** driven in a browser. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **page close-read**. It is **not** a claim that §47 is complete, that `/api/v1/overview` exists, that RBAC is on, or that MT5/FIX are live.

---

## 0. Verdict

**The Overview *file* exists and is the only page that actually paints `GET /api/overview`. The §47 Overview *page* is not implemented.**

Do not answer “missing” from A91 §0 / A62 §4 / A29 U01. Those snapshots predate `OverviewPage.tsx` (13:16:00, untracked vs HEAD). Do not answer “done” from the sidebar **Overview** leaf or from 12 `MetricCard`s lighting up. Twelve boolean/int cards over a flat unversioned DTO is a demo dashboard, not the 18-tile health grid.

| Layer | Required | Now | Classification |
|---|---|---|---|
| §46 / A26 route | `/overview` (default landing) | `/` → `/overview`; `path="overview"` | **PRESENT** |
| §46 nav label | `Overview` | `Overview` | **EXACT** |
| Page module | A62 `pages/overview/OverviewPage.tsx` + 5 row modules | `pages/OverviewPage.tsx` (2078 B, 35 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Hook | `useOverview` → `GET /api/v1/overview` + `{ data }` envelope | `useOverview` → `GET /api/overview`, bare body | **EXISTS_NEEDS_REFACTOR** |
| API | A26 / A91 nested snapshot | `OverviewDto` 17 flat fields, no envelope, no `generatedAt` | **EXISTS_NEEDS_REFACTOR** (demo) / **DEPRECATED** as catalog |
| §47 tiles painted as dedicated cards | **18** | **11** dedicated + **1** merged QUOTE/TRADE | **11 / 18** |
| A91 click-through | 18 navigations | **0** (`MetricCard` has no `onClick`; page has no `Link`) | **MISSING** |
| Header strip (A26 §5.3) | Real-copy · Stop-new · MT5 · QUOTE · TRADE | Layout has none; page has a real-copy footer only | **MISSING** |
| Health model | 5-value `healthStatus` + `reasonCode` + extras | 3 booleans → `OK`/`DOWN` and `Q`/`-` `T`/`-` | **WRONG** |
| TS type | A91 nested `OverviewSnapshot` | unused `types/index.ts` `Overview` (6 wrong fields) | **DEPRECATED** (unused) |
| SignalR | `overview.updated` / `ops.header` | page never `onEvent`; host has **0** `MapHub` | **MISSING** |
| Tests | Overview DTO / page / query | **0** hits under `tests/` | **MISSING** |
| Mutations on this page | **none** | none | **ALIGNED** |
| Live `NewOrderSingle` | off | banner + `realCopyEnabled: false` | **ALIGNED** |

**One-line:** Architecture §47 requires an 18-tile one-GET health grid; the dashboard has a 12-card untyped demo that hides `live` / `xauGross` / `xauNet` and paints MT5 as OK because two `brokers.Enabled` rows exist.

**Direct answer to “Is Overview done?”**

| If the question means… | Answer |
|---|---|
| Is there no file / no route / no sidebar entry? | **No.** File + `/overview` + exact nav exist. Index redirects here. |
| Is the architecture §47 / A26 / A91 Overview page implemented? | **Yes — missing.** File is a demo stand-in. |
| Must first-useful (§69) ship the full A91 extras (per-broker health, dest margin)? | **No.** Honest zeros / `UNKNOWN` / `null` are allowed. Lying `mt5Healthy: true` is not. |

This report does **not** authorize creating or rewriting product files.

---

## 1. Method (this pass)

1. Read `OverviewPage.tsx` in full (not truncated). Read `MetricCard`, `useOverview`, axios client, SignalR stub, unused `types`/`formatters`, router, layout, `main.tsx` QueryClient.
2. Read `OverviewDto` + `GetOverviewAsync` + `Program.cs` `MapGet("/api/overview")` + `DemoSeeder` FIX rows.
3. PowerShell `Get-FileHash SHA256` + `.Length` + physical / non-blank lines + `LastWriteTime` at **13:43:05+05:30**.
4. `git rev-parse HEAD`, `git status --short` on the page and the wire, `git hash-object` of the page.
5. Live `Invoke-WebRequest http://127.0.0.1:5000/api/overview` — HTTP **200**, body in §7.
6. Grep `apps/web/src` for `from '…types'`, click-through on the page, `tests/` for `OverviewPage` / `GetOverviewAsync` / `OverviewDto` / `useOverview`.
7. Compare §47 verbatim list, A26 §6.1 JSON, A91 tile/click/health rules, D08 §7.1, D21 §5.1, D39 hook #1.
8. **Did not** run `npm` / `tsc` / Playwright. **Did not** edit product source.

---

## 2. Measured files

| Path | Bytes | Phys. | Non-blank | SHA-256 | Last write (local) | Git |
|---|---:|---:|---:|---|---|---|
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | **2078** | **35** | 33 | `6497193F190445CCF76AED218E01E3BC85050238CB89D06002837FC9C502825C` | 2026-08-18T13:16:00+05:30 | **untracked** (`??`); worktree blob `05be71c57a9ac1300e478d154b8b19d32645e11d` |
| `D:\Prop\apps\web\src\components\MetricCard.tsx` | 521 | 11 | 10 | `1C6A6AF96D39C3B0C5BA4544337E19EDE460279BB61DF547687BDAF85B36991B` | 2026-08-18T13:08:39+05:30 | (shared with Risk) |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | 42 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | **unstaged** (`M`); `useOverview` unchanged vs D08/D39 |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | 7 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 | `baseURL` = `VITE_API_URL` or `http://localhost:5000`; timeout 15 s |
| `D:\Prop\apps\web\src\api\signalr.ts` | 899 | 28 | 23 | `AB913FF7817D9AF775EFFE87D927EDE89C5442DA561C093AA326B84BD8E9C793` | 2026-08-18T13:08:02+05:30 | `/hubs/dashboard` (A26 is `/hubs/ops`) |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | 123 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | **0 imports** anywhere under `apps/web/src` |
| `D:\Prop\apps\web\src\utils\formatters.ts` | 947 | 31 | 27 | `FD0214751EA05923973B4EBD73E35EE21AC3D253FDE61538F4616EC2DC3B6F66` | 2026-08-18T13:08:39+05:30 | `pnlColor` / `timeAgo` unused by Overview |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | 41 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | unstaged vs HEAD |
| `D:\Prop\apps\web\src\layouts\DashboardLayout.tsx` | 1854 | 44 | 41 | `48F7073E50B75B3766AB8B918AFF5EA0608CD96AF76F6616D6A31426A0797C21` | 2026-08-18T13:20:38+05:30 | unstaged vs HEAD |
| `D:\Prop\apps\web\src\main.tsx` | 648 | 22 | 20 | `25A2B880FDD5D6831E5DABA65F7078E4D35C263B8BBEC2B6AC1391F7EF647FB3` | 2026-08-18T13:06:39+05:30 | retry 2, `refetchOnWindowFocus: false`, `staleTime: 30_000` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 3088 | 114 | 104 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | unstaged (`M`); `OverviewDto` 17 fields |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | 8708 | 205 | 182 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15+05:30 | **untracked** (`??`); `GetOverviewAsync` L14–43 |
| `D:\Prop\apps\api\Program.cs` | 4731 | 95 | 86 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | unstaged (`M`); L54 `MapGet("/api/overview")` |

Page SHA `6497193F…` is **unchanged vs D08 §3** (remeasured). The file is still **not in HEAD**. A clean checkout of `398a142` has **no** `OverviewPage.tsx`.

No `pages/overview/` folder. No `OverviewKpiGrid` / `OverviewStateRow` / `OverviewPnlPair` / `OverviewExposureRow` / `OverviewHealthRow`. No Overview test.

---

## 3. What the file actually is

Entire module (35 physical lines):

```tsx
import MetricCard from '../components/MetricCard';
import { useOverview } from '../api/hooks';

export default function OverviewPage() {
  const { data, isLoading, error } = useOverview();
  if (isLoading) return <p className="text-gray-400">Loading overview…</p>;
  if (error) return <p className="text-red-400">API unavailable. Start the ASP.NET API on port 5000.</p>;
  if (!data) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Overview</h1>
        <p className="text-sm text-gray-400">First useful version: ingestion, reconstruction, baseline scores, shadow-ready. Live FIX send is off.</p>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="MT5 accounts" value={data.totalAccounts} />
        <MetricCard label="Brokers" value={data.connectedBrokers} />
        <MetricCard label="XAU traders" value={data.xauTraders} />
        <MetricCard label="≥ 3 trades" value={data.tradersWithThreeTrades} />
        <MetricCard label="Watch" value={data.watch} />
        <MetricCard label="Shadow" value={data.shadow} color="text-blue-300" />
        <MetricCard label="Live candidates" value={data.liveCandidates} />
        <MetricCard label="Risk blocked" value={data.riskBlocked} color="text-amber-300" />
        <MetricCard label="Shadow P&L" value={Number(data.shadowPnl).toFixed(2)} />
        <MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
        <MetricCard label="MT5 health" value={data.mt5Healthy ? 'OK' : 'DOWN'} color={data.mt5Healthy ? 'text-emerald-300' : 'text-red-400'} />
        <MetricCard label="QUOTE / TRADE" value={`${data.quoteHealthy ? 'Q' : '-'} / ${data.tradeHealthy ? 'T' : '-'}`} />
      </div>
      <div className="rounded border border-gray-800 bg-gray-950 p-4 text-sm text-gray-300">
        Real copy execution: <strong className="text-amber-300">{data.realCopyEnabled ? 'ON' : 'OFF'}</strong>. Trade #3 never auto-promotes to LIVE.
      </div>
    </div>
  );
}
```

`MetricCard` (`components/MetricCard.tsx`, 11 lines) is a presentational box: `label` + `value` + optional `sub` + optional `color`. **No** `onClick`, **no** `to`, **no** `href`, **no** health-dot enum.

Hook (`hooks.ts` L4–6):

```ts
export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
}
```

No generic. No `{ data }` unwrap. No `refetchInterval` (FIX/risk poll; Overview does not). Query key `['overview']` matches A26 §13.6 spelling only.

Render states:

| Branch | When | UI |
|---|---|---|
| `isLoading` | first fetch | gray “Loading overview…” — **no** skeleton of 18 tiles |
| `error` | axios fail after 2 retries | red “API unavailable. Start the ASP.NET API on port 5000.” |
| `!data` | 200 with empty body (should not happen) | `null` — empty `<Outlet />` |
| success | object | H1 + subtitle + 12 cards + real-copy footer |

`data` is implicit `any`. TypeScript will not catch `data.live` / `data.xauGross` being ignored, or a rename of `connectedBrokers`.

---

## 4. Route and chrome (this page only)

| Item | Required | Measured |
|---|---|---|
| Default landing | `/` → `/overview` | `App.tsx` L23 `<Navigate to="/overview" replace />` |
| Route | `/overview` | `App.tsx` L24 |
| Sidebar | exact `Overview` | `DashboardLayout` L6 `{ to: '/overview', label: 'Overview' }` |
| Catch-all → `/overview` | A26 §5.2 | **MISSING** (`path="*"` = 0; D38) |
| Auth wrapper | `RequireAuth` | **MISSING** (D53: API also anonymous) |
| Header strip | 5 chips from same GET | **MISSING** on layout. Real-copy is a **page footer**, not a chip. Stop-new and flatten are absent. |
| SignalR keep-hot | `ops.header` / `overview.updated` | Layout calls `startConnection()` to **`/hubs/dashboard`**. Host `MapHub` = **0** (D50). Page never `onEvent`. |

Index redirect + exact label are the only §46 rows this page fully satisfies.

---

## 5. Architecture §47 tile matrix

Verbatim §47 list (`…Architecture_v2.md` L1766–1790) is **18 tiles**. A62 §10.1 / A91 §4 bind a 5-row layout. Current UI is **one** `grid-cols-2 md:grid-cols-4` of 12 cards (3×4).

| # | §47 tile | A91 JSON | Page card | Painted? | Notes |
|---|---|---|---|---|---|
| 1 | Total MT5 accounts | `accounts.totalMt5Accounts` | “MT5 accounts” ← `totalAccounts` | **YES** | Label abbreviated |
| 2 | Connected source brokers | `accounts.connectedSourceBrokers` | “Brokers” ← `connectedBrokers` | **YES, WRONG FACT** | Query counts `brokers.Enabled`, not live-connected (D21 §5.1) |
| 3 | XAUUSD traders | `accounts.xauusdTraders` | “XAU traders” ← `xauTraders` | **YES, WRONG SOURCE** | `TraderScores.CompletedXauTrades > 0`, not distinct reconstructed XAU |
| 4 | Traders with >= 3 completed trades | `accounts.tradersWithMinThreeCompletedTrades` | “≥ 3 trades” ← `tradersWithThreeTrades` | **YES** | Same score table, `>= 3` |
| 5 | Watch | `traderStates.watch` | “Watch” ← `watch` | **YES** | `TraderState.WATCH` only — leftover states correctly omitted |
| 6 | Shadow | `traderStates.shadow` | “Shadow” ← `shadow` | **YES** | |
| 7 | Live candidates | `traderStates.liveCandidates` | “Live candidates” ← `liveCandidates` | **YES** | |
| 8 | Live copied | `traderStates.liveCopied` | **none** | **NO** | DTO field is `live`. Page never reads it |
| 9 | Risk blocked | `traderStates.riskBlocked` | “Risk blocked” ← `riskBlocked` | **YES** | amber label color only |
| 10 | Shadow P&L | `pnl.shadowPnl` | “Shadow P&L” ← `shadowPnl` | **YES, WRONG FORMULA** | `SUM(shadow_orders.SourceVsShadowSlippage)` — not A24 net. `Number().toFixed(2)` cannot paint A91 `null` / `UNAVAILABLE` |
| 11 | Destination real P&L | `pnl.destinationRealPnl` | “Dest. real P&L” | **YES, HARDCODED 0** | Query literal `0`. First-useful zero is allowed **if** quality is `EXACT` and live is off — quality field is absent |
| 12 | Current XAU gross exposure | `exposure.grossQuantity` | **none** | **NO** | DTO has `xauGross` (always 0). Page ignores it |
| 13 | Current XAU net exposure | `exposure.netQuantity` | **none** | **NO** | DTO has `xauNet` (always 0). Page ignores it |
| 14 | Destination free margin | `exposure.destinationFreeMargin` | **none** | **NO** | Not on `OverviewDto` |
| 15 | Destination margin level | `exposure.destinationMarginLevel` | **none** | **NO** | Not on `OverviewDto` |
| 16 | MT5 ingestion health | `health.mt5Ingestion` | “MT5 health” bool → `OK`/`DOWN` | **YES, WRONG MODEL** | `mt5Healthy = enabledBrokers > 0` → live capture **`true` / OK** with no Manager |
| 17 | FIX quote health | `health.fixQuote` | merged “QUOTE / TRADE” | **PARTIAL** | bool `quoteHealthy`; no `STALE` / `UNKNOWN` / bid / age |
| 18 | FIX trade health | `health.fixTrade` | merged into #17 | **PARTIAL** | bool `tradeHealthy`; first-useful TRADE-dark should be `UNKNOWN`, not a blank `-` |

**Dedicated §47 tiles on screen: 11 / 18.**  
**Tiles present on the wire but not painted: 3** (`live`, `xauGross`, `xauNet`).  
**Tiles missing from the wire: 2** (free margin, margin level) plus the entire nested health/flags/quality objects.

A91 forbids inventing a 19th “overall green.” The page does not add one. It **does** invent a merged 17+18 card that can hide a red TRADE behind a green Q (or the reverse). Today both bits are false, so the combo paints `- / -`.

---

## 6. Field matrix: A91 ↔ `OverviewDto` ↔ page ↔ unused TS

C# record (`DashboardModels.cs` L5–22), JSON camelCase, live keys confirmed in §7.

| A91 path | C# / live JSON | Page reads? | `types/index.ts` `Overview` | Kind |
|---|---|---|---|---|
| `generatedAt` | **absent** | — | — | **C# missing** |
| envelope `data` | **absent** (bare object) | n/a (`r.data` is axios body) | — | **WRONG SHAPE** |
| `accounts.totalMt5Accounts` | `totalAccounts` | yes | `totalAccounts` | name drift vs A91; page↔C# **MATCH** |
| `accounts.connectedSourceBrokers` | `connectedBrokers` | yes | `totalBrokers` (**NAME**) | page↔C# MATCH |
| `accounts.xauusdTraders` | `xauTraders` | yes | **absent** | page↔C# MATCH |
| `accounts.tradersWithMinThreeCompletedTrades` | `tradersWithThreeTrades` | yes | **absent** | page↔C# MATCH |
| `traderStates.watch` | `watch` | yes | folded into `tradersByState` | page↔C# MATCH |
| `traderStates.shadow` | `shadow` | yes | folded | page↔C# MATCH |
| `traderStates.liveCandidates` | `liveCandidates` | yes | folded | page↔C# MATCH |
| `traderStates.liveCopied` | **`live`** | **NO** | folded | **PAGE DROP** + A91 name |
| `traderStates.riskBlocked` | `riskBlocked` | yes | folded | page↔C# MATCH |
| `pnl.shadowPnl` | `shadowPnl` | yes | `shadowPnl` | MATCH key; formula wrong |
| `pnl.destinationRealPnl` | `destinationRealPnl` | yes | `realPnl` (**NAME**) | page↔C# MATCH |
| `pnl.currency` / `*Quality` | **absent** | — | — | **C# missing** |
| `exposure.grossQuantity` | `xauGross` | **NO** | **absent** | **PAGE DROP** |
| `exposure.netQuantity` | `xauNet` | **NO** | **absent** | **PAGE DROP** |
| `exposure.destinationFreeMargin` | **absent** | — | — | **C# missing** |
| `exposure.destinationMarginLevel` | **absent** | — | — | **C# missing** |
| `health.mt5Ingestion` object | `mt5Healthy` bool | yes (as OK/DOWN) | `fixHealthy` one bool | **SHAPE** |
| `health.fixQuote` object | `quoteHealthy` bool | yes (merged) | folded into `fixHealthy` | **SHAPE** |
| `health.fixTrade` object | `tradeHealthy` bool | yes (merged) | folded | **SHAPE** |
| `flags.realCopyExecutionEnabled` | `realCopyEnabled` | yes (footer) | **absent** | page↔C# MATCH |
| `flags.stopNewExecution` | **absent** | — | — | **C# missing** |
| `flags.emergencyFlattenAvailable` | **absent** | — | — | **C# missing** |

B29 §3.1 still holds for TS vs C#. The **page does not use the TS type**, so the unused `Overview` interface cannot break compile. It **can** break a future `useQuery<Overview>`. Do not type the hook from `types/index.ts`.

`Number(data.shadowPnl).toFixed(2)`: if the key is missing, the card paints **`NaN`**. A91 `null` (UNPRICED book) would also become **`NaN`**, not “—”.

---

## 7. Live wire capture (2026-08-18T13:43:05+05:30)

`GET http://127.0.0.1:5000/api/overview` → **HTTP 200**, no `Authorization`, no `Cache-Control: no-store`, no `{ data }` envelope:

```json
{
  "totalAccounts": 4,
  "connectedBrokers": 2,
  "xauTraders": 3,
  "tradersWithThreeTrades": 3,
  "watch": 0,
  "shadow": 2,
  "liveCandidates": 0,
  "live": 0,
  "riskBlocked": 1,
  "shadowPnl": 248.20,
  "destinationRealPnl": 0,
  "xauGross": 0,
  "xauNet": 0,
  "mt5Healthy": true,
  "quoteHealthy": false,
  "tradeHealthy": false,
  "realCopyEnabled": false
}
```

What the page **would** paint from this body (not browser-verified):

| Card | Value |
|---|---|
| MT5 accounts | `4` |
| Brokers | `2` |
| XAU traders | `3` |
| ≥ 3 trades | `3` |
| Watch | `0` |
| Shadow | `2` |
| Live candidates | `0` |
| Risk blocked | `1` |
| Shadow P&L | `248.20` |
| Dest. real P&L | `0.00` |
| MT5 health | **OK** (emerald) |
| QUOTE / TRADE | `- / -` |
| Footer | Real copy **OFF**; “Trade #3 never auto-promotes to LIVE.” |

`live: 0`, `xauGross: 0`, `xauNet: 0` are on the wire and **invisible**.

This matches `CREDENTIALS_AND_COPY_STATUS.md` (“2 SHADOW, 1 RISK_BLOCKED, 0 LIVE, `realCopyEnabled=false`”) and the Fake book: logins `10001` / `10002` / `10003` / `99001` (4 accounts); `10003` has **no** closed XAU round-trips → not in `xauTraders`; two SHADOW (`10001`, `99001` per D48 shadow side-effect) + one `RISK_BLOCKED` (`10002` lot-escalation book) + one leftover state (not a §47 tile).

`shadowPnl: 248.20` is **not** the D21-era `0`. D48: seeder rebuild now leaves **6** `shadow_orders` for 10001+99001. The number is `SUM(SourceVsShadowSlippage)`, still not A24 `shadow_performance.net`.

---

## 8. Query honesty (`GetOverviewAsync`)

Seven sequential EF statements (`EfDashboardQueries.cs` L14–43). Constructor literals: `DestinationRealPnl = 0`, `XauGross = 0`, `XauNet = 0`, `RealCopyEnabled = false`.

| DTO field | How it is produced | Honest? |
|---|---|---|
| `TotalAccounts` | `COUNT(*)` `mt5_accounts` | **Yes** for demo (4). Not 5k live. |
| `ConnectedBrokers` | `COUNT(*)` `brokers WHERE Enabled` | **No.** Registered-enabled ≠ connected + fresh heartbeat (A91 §6.1). Live `2` with Fake connector. |
| `XauTraders` / `TradersWithThreeTrades` / five state ints | `ToListAsync` **all** `trader_scores`, count in memory | Demo-ok; A91 wants reconstructed XAU / `trader_states` current row. Full-table load is D21 leftover. |
| `ShadowPnl` | `SUM(SourceVsShadowSlippage)` | **Wrong grain.** Slippage sum ≠ book net. Unpriced → 0, which A24 calls a lie. |
| `DestinationRealPnl` / `XauGross` / `XauNet` | literals `0` | Allowed while live is off **only** with quality + dest-book semantics. No dest positions queried. |
| `Mt5Healthy` | `brokers > 0` | **Lie.** Two enabled catalog rows ⇒ `true` / page **OK**. No `broker_connections`, no worker heartbeat, no last persisted deal. FakeMt5 is not Manager. |
| `QuoteHealthy` | QUOTE row in `{ LoggedOn, ReadyForMarketData, ReadyForExecution }` | Seeder now writes **`Disconnected`** (D22 “LoggedOn” is **stale**). Live `false`. Bit is honest **today**; enum membership still cannot express `UNKNOWN` / `STALE`. |
| `TradeHealthy` | TRADE row in `{ LoggedOn, Reconciling, ReadyForExecution }` | Same: seeded `Disconnected` → `false`. First-useful TRADE-dark should be `UNKNOWN` + `SESSION_DISABLED`, not a boolean down. |
| `RealCopyEnabled` | literal `false` | **Yes** vs A49 default. Not read from env / settings. |

C13 “liar MT5/**FIX** health” is **half-stale**. FIX bits are currently false because the seeder stopped forging `LoggedOn` (current `DemoSeeder` L73 / L91 = `Disconnected`). **MT5 OK is still a lie.**

A91: do not fail the whole GET because TRADE is dark. The demo host returns **200** with booleans. That part is fine. The page’s error branch only fires when the process is down (axios), not when health is unknown.

---

## 9. Click-through, formatters, SignalR, tests

A91 §4 click-through (Overview has no mutations):

| Tile | Navigate to | On disk |
|---|---|---|
| accounts / brokers | `/brokers` | **no** |
| XAU / ≥3 / states | `/traders` (+ query) | **no** |
| Shadow P&L | `/shadow` | **no** |
| Dest real P&L | `/live` | **no** |
| gross / net / margin | `/risk` | **no** |
| MT5 health | `/health` | **no** |
| QUOTE / TRADE | `/fix` | **no** |

Grep of `OverviewPage.tsx` for `useNavigate`, `Link`, `onClick`, `to=`: **zero** hits.

`utils/formatters.ts` already has `pnlColor` / `pnlBg` / `timeAgo` (A91 STALE paint). Overview does not import them. P&L cards stay default gray even at `248.20`.

`useOverview` has no `refetchInterval`. Combined with `staleTime: 30_000` and `refetchOnWindowFocus: false`, the page is a **one-shot** after mount unless the component remounts. A06: polling Overview is enough to unblock §69; this hook barely polls.

Tests: `tests/` grep `OverviewPage|GetOverviewAsync|OverviewDto|useOverview` → **0** lines.

RBAC: page has no role gate. Host `GET /api/overview` is anonymous (D53). CORS `AllowAnyOrigin`.

---

## 10. Stale prior claims (do not reuse)

| Claim | Source | This pass |
|---|---|---|
| Overview page **MISSING** (route exists, no file) | A91 §0; A62 §4; A29 U01 | **Stale.** File SHA `6497193F…` since 13:16:00 |
| `pages/` empty / 13 files | A62 §3; B22 | **Stale.** D08 / C08: 15 files including this one |
| Seeder paints QUOTE/TRADE green via `LoggedOn` | D22 §0; C13 FIX liar | **Stale for FIX bits.** Current seed is `Disconnected`; live `quoteHealthy/tradeHealthy` = false |
| Demo `shadowPnl` is 0 | D21 §4 / §5.1 | **Stale.** Live `248.20` after D48 shadow side-effect |
| `types/index.ts` `Overview` is what the page consumes | implied by B29 pairing | **False.** 0 imports. Page uses C# camelCase via `any` |
| A91 page still missing so no DTO work needed | — | **False.** File exists; DTO/page/query still need the A91 replace — later coding wave |

D08 §7.1 (“12 MetricCards; DTO `live` / `xauGross` / `xauNet` not painted”) is **still exact**. This file narrows that paragraph to a measured contract gap.

---

## 11. Classification and non-goals

| Slice | §73.B |
|---|---|
| `OverviewPage.tsx` as a routed leaf | `EXISTS_NEEDS_REFACTOR` |
| `OverviewPage.tsx` as architecture §47 | `MISSING` (contract) |
| `useOverview` / `GET /api/overview` demo path | `EXISTS_NEEDS_REFACTOR` |
| Same path as A26 `GET /api/v1/overview` | `DEPRECATED` / `MISSING` |
| Boolean health tiles | `UNSAFE` if treated as ops truth (`mt5Healthy: true` today) |
| Real-copy footer + “no auto-promote” copy | `EXISTS_AND_GOOD` as honesty chrome |
| Unused `types` `Overview` | `DEPRECATED` |
| Header strip / click-through / SignalR / tests | `MISSING` |
| Product source this pass | **unchanged** |

**Do not** (this report is not a coding ticket):

- Recreate `OverviewPage.tsx`.
- Hand-type A91 JSON in the React file.
- Paint `mt5Healthy` from `brokers.Enabled`.
- Add a working live book to make dest P&L look busy.
- Type `useOverview` with `types/index.ts` `Overview`.
- Treat 11/18 cards + a 200 as §47 / §69.12 done.

When a coding wave touches Overview: replace the flat DTO (A91 §9), version the GET, paint all 18 tiles including honest `UNKNOWN` health and `null` P&L, add click-through, move flags to the **layout** header, keep live send off.

---

## 12. Honesty

- File exists. Route exists. Label is exact. Hook hits a live map. Demo JSON is real.
- **11 / 18** §47 tiles have a dedicated card. Live-copied, dest margin pair, and dest exposure pair do not.
- Three wire fields (`live`, `xauGross`, `xauNet`) are computed (or zeroed) and then **dropped on the floor**.
- **MT5 health OK is a catalog-count lie.** FIX booleans are currently false and therefore less dangerous than D22’s era; they are still the wrong type.
- Shadow P&L `248.20` is a slippage sum, not A24 net.
- Destination real P&L `0.00` happens to be the first-useful truth (execution off) and is also a constructor literal.
- No `/api/v1`, no envelope, no RBAC, no hub, no tests, no click-through, no header strip.
- HEAD `398a142` does **not** contain this page. Do not review a clean checkout and declare Overview absent — and do not merge-review this SHA as §47 complete.
