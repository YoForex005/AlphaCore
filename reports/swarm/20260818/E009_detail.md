# E009 — `GetTraderDetailAsync` + `TraderDetailPage` vs architecture §51 / A93

| Field | Value |
|---|---|
| Agent | E009 (senior engineer; trader-detail query + page only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18T13:50:12+05:30 |
| Workspace | `D:\Prop` (Vite app is **not** under `D:\Prop\src`) |
| Assigned | Read `GetTraderDetailAsync` and `TraderDetailPage`. Write this file. **Do not modify product source.** |
| Targets | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` `GetTraderDetailAsync` (L125–160); `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` |
| Adjacent (read, not edited) | `DashboardModels.cs` `TraderDetailDto` / `TradeHighlightDto` / `GetTraderAsync`; `apps/api/Program.cs` `GET /api/traders/{broker}/{login:long}`; `hooks.ts` `useTraderDetail`; `client.ts`; `types/index.ts`; `TradersPage.tsx`; `App.tsx`; `DashboardLayout.tsx`; `ReconstructedTrade.cs`; `TraderScore.cs`; `Mt5Account.cs`; `DependencyInjection.cs`; `DemoSeeder.cs` |
| Product source modified | **No.** This report (plus catalog notes in `INDEX.md` / `SWARM_LOG.md`) are the only writes. |
| Test source modified | **No** |
| Method | Full `read_file` of query + page + DTO + host + hook; PowerShell SHA-256 / bytes / physical-line / last-write / `git hash-object`; live HTTP against `http://127.0.0.1:5000`; grep `tests/` and product call sites; compare architecture §51, A93, A26 §6.5 (superseded), A62 §10.5, D49, D72, D39 |
| Binding law | Architecture v2 **§51**; A93 (wins for this resource); A21 first-3 eligibility; A22 scores; A45 MFE/MAE; A69 states; A92 list row stays the leaderboard |
| Prior census | A93 §15 “weatherforecast / DTO missing / page absent” (stale inventory); D49 (`GetTraderAsync` thin; wrapper not A93); D39 (host already calls this method); D72 (dashboard first-3 is recomputed, no dirty bit) |
| Measure HEAD | `398a14200ec65714c4077eed55c46808382ca1e3` (`docs: add PNG fallback and update README; add conversion script`, 2026-08-18 13:24:21 +0530) |
| Live API | **Yes.** Captures in §7. The React page was **not** driven in a browser. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `DEPRECATED` | `UNSAFE`.

This is a **query + page close-read with live HTTP**. It is **not** a claim that §51 is done, that `/api/v1/traders/{brokerId}/{login}` exists, that first-3 is persisted, or that MT5/FIX are live.

---

## 0. Verdict

**The method and the page exist and are wired to each other. Architecture §51 / A93 trader detail is not implemented.**

Do not answer “missing” from A93 §15 (weatherforecast host, no `TraderDetailDto`, page file absent). Those snapshots predate `GetTraderDetailAsync` (13:35:15) and `TraderDetailPage.tsx` (13:35:59). Do not answer “done” from the route lighting up, from the type name `TraderDetailDto`, or from a `First 3` column that prints `yes`.

| Layer | Required | Now | Classification |
|---|---|---|---|
| §46 / A62 route | `/traders/:brokerId/:login` | `App.tsx` L28; param is **broker code**, not UUID | **PRESENT** (shape) / **WRONG** (identity) |
| Nav leaf | not a sidebar item | not in `DashboardLayout` (14 links); reached from `TradersPage` `Link` | **ALIGNED** |
| Page module | A62 `pages/trader-detail/TraderDetailPage.tsx` + 16 §51 blocks | `pages/TraderDetailPage.tsx` (2402 B, 56 lines) | **EXISTS_NEEDS_REFACTOR** (file) / **MISSING** (contract) |
| Hook | `useTraderDetail` → `GET /api/v1/traders/{brokerId}/{login}` + `{ data }` | `GET /api/traders/${broker}/${login}`, bare body, untyped | **EXISTS_NEEDS_REFACTOR** |
| Host map | A93 envelope + 404 | `MapGet("/api/traders/{broker}/{login:long}")` → `GetTraderDetailAsync`; miss = **200 `null`** | **EXISTS_NEEDS_REFACTOR** (demo) / **MISSING** (`/api/v1`) |
| Application DTO | A93 13-object root + `FirstThreeBlock` | 2-field `{ header: TraderRowDto, trades: TradeHighlightDto[] }` **same type name** | **EXISTS_NEEDS_REFACTOR** (collision) |
| Query | keyed `(brokerId, login)`; 3-slot first-3; no full-book dump | `GetTraderAsync` = full leaderboard + `FirstOrDefault`; then **all** reconstructed rows; boolean flag | **EXISTS_NEEDS_REFACTOR** / **UNSAFE** (5k scan) |
| §51 blocks painted | **16** | **~2 partial** (8 header chips + 4-col table) | **2 / 16** |
| A93 always-on root objects | **13** | **0** (`header`/`trades` are not those objects) | **0 / 13** |
| First-3 | `data.firstThree` exactly 3 slots | boolean `isFirstThree` on an unbounded list | **MISSING** (block) / **PARTIAL** (flag) |
| TS type | A93 `TraderDetailDto` | unused `types/index.ts` `TraderDetail` (`isFirst3`, `ticket: number`) | **DEPRECATED** (unused) |
| SignalR | `trader.updated` | page never `onEvent`; host has **0** `MapHub` | **MISSING** |
| Tests T1–T16 | required | **0** hits under `tests/` | **MISSING** |
| Mutations on this page | none in v1 first-useful GET | none | **ALIGNED** |
| Live `NewOrderSingle` | off | footnote: “Live promotion is not automatic.” | **ALIGNED** |
| `mlProbability` | always `null` in v1; never render `0` | JSON `null`; page `'not trained'` | **ALIGNED** |

**One-line:** `GetTraderDetailAsync` is a leaderboard-row wrapper plus an unbounded reconstructed-trade table with a walk-order `isFirstThree` bit; `TraderDetailPage` unwraps that pair and paints eight chips and four columns. That is a demo explorer, not §51.

**Direct answer to “Is trader detail done?”**

| If the question means… | Answer |
|---|---|
| Is there no file / no route / no GET? | **No.** File + `/traders/:brokerId/:login` + `GET /api/traders/{broker}/{login}` exist and match. |
| Is architecture §51 / A93 implemented? | **Yes — missing.** File is a demo stand-in. Type name is a collision. |
| Does the live demo at least show first-3 XAU for seed 10001? | **Yes, as a boolean column.** 10001/10002/99001 return three `isFirstThree: true` XAU rows. That is **not** `FirstThreeBlock`. |
| Must first-useful (§69) ship every A62 chart? | **No.** Honest empty slots / `UNAVAILABLE` MFE are allowed. A 200 `null` miss, case-split empty trades, and a provisional `earlyScore: 40` on N=0 are not. |

This report does **not** authorize creating or rewriting product files.

---

## 1. Method (this pass)

1. Read `GetTraderDetailAsync` (L125–160) and its header loader `GetTraderAsync` / `GetTradersAsync` in full. Read `TraderDetailDto` / `TradeHighlightDto` / `IDashboardQueries`.
2. Read `TraderDetailPage.tsx` in full (56 lines). Read `useTraderDetail`, axios client, unused `types/index.ts`, router, `TradersPage` link, layout.
3. PowerShell `Get-FileHash SHA256` + `.Length` + physical / non-blank lines + `LastWriteTime` at **13:50:12+05:30**. `git rev-parse HEAD`, `git status --short`, `git hash-object`.
4. Live `Invoke-WebRequest` to `http://127.0.0.1:5000` — `/health` **200**; detail captures in §7. Page itself **not** opened in a browser.
5. Grep `tests/` for `GetTraderDetailAsync` / `TraderDetailDto` / `TradeHighlightDto` / `TraderDetailPage` / `useTraderDetail`: **0**. Grep product for the method: Application interface + Infrastructure impl + `Program.cs` L59–60.
6. Compare §51 verbatim list, A93 D1–D15 + `FirstThreeBlock` + T1–T16, A26 §6.5 (superseded), A62 §10.5, D49, D72, D39.
7. **Did not** run `npm` / `tsc` / Playwright. **Did not** edit product source.

---

## 2. Measured files

| Path | Bytes | Phys. | Non-blank | SHA-256 | Last write (local) | Git |
|---|---:|---:|---:|---|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | **8708** | **205** | 182 | `328D0924112183A93AFB5C97A8AF5396D7FF9BB5B746BD7F1D7FC4CDE9243B60` | 2026-08-18T13:35:15+05:30 | **untracked** (`??`); blob `d9bed4fc73f4e189cd21c1d31e58ab4a2c291dac` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | **3088** | **114** | 104 | `9A3888AE37ECAB2596434077AE8F7088AD67A6A581FDD3C4C21573F7CD428496` | 2026-08-18T13:34:59+05:30 | **modified** (`M`); blob `16be45846cd3b3c16c745ef3a5842b2d5621c7b7` |
| `D:\Prop\apps\api\Program.cs` | **4731** | **95** | 86 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | 2026-08-18T13:35:15+05:30 | **modified** (`M`); blob `9d623e1da0adc050b15a6d04f330169be2a125e5` |
| `D:\Prop\apps\web\src\pages\TraderDetailPage.tsx` | **2402** | **56** | 54 | `C849449B6B76E6E4147AD2503DF00FD5E101C5B5D05ADB7E05708130A8556EB2` | 2026-08-18T13:35:59+05:30 | **untracked** (`??`); blob `9486d01bf98c7b25d467dd458024d0cd277bb680` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | 42 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | 2026-08-18T13:16:00+05:30 | **modified** (`M`); blob `0c8b7f48fee540f1361f7e5073df7af7844a9a12` |
| `D:\Prop\apps\web\src\api\client.ts` | 232 | 9 | 7 | `9A04E60CA14613DB9730865A33D5B7CC7F15A1042EC2C6951C6A62891CDD6F78` | 2026-08-18T13:08:06+05:30 | |
| `D:\Prop\apps\web\src\types\index.ts` | 2905 | 135 | 123 | `B9CE20C1412B25925CC08769355021E6E98E933F8532061B3BC6593F370AF081` | 2026-08-18T13:08:18+05:30 | |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | 1604 | 42 | 41 | `0AF0FF5BD2EE6B7B4BB06F483B065589A1235FE94EE63B2F4491EC00C510518F` | 2026-08-18T13:16:00+05:30 | **untracked** |
| `D:\Prop\apps\web\src\App.tsx` | 2062 | 42 | 41 | `A0E92C9779A0C777861DCF27014BA5D5CD5ADFF52767C2DF6CFB6326C2F99099` | 2026-08-18T13:20:38+05:30 | |
| `D:\Prop\src\Domain\Entities\ReconstructedTrade.cs` | 1430 | 36 | 34 | `06A1A7651EDFD9C7E4482293774F9F9BBEA778AEAB6ECFBAF95E5B49F90F8014` | 2026-08-18T13:08:41+05:30 | |
| `D:\Prop\src\Domain\Entities\TraderScore.cs` | 652 | 19 | 17 | `48E4C10B5E5A356DA5BB824A32D0A4C857AA2208FA9E4EDE7D145BCCB401ECBA` | 2026-08-18T13:08:41+05:30 | |
| `D:\Prop\src\Domain\Entities\Mt5Account.cs` | 639 | 18 | 17 | `B13CB025741FB7DDF290B67070727C9FAFC0FDF071572FCD1DB7CCADDB6DA549` | 2026-08-18T13:08:41+05:30 | |

Hashes of the query class, models, host, and page **match D49** (remeasured; no drift since 13:38). D21 / C36 SHAs for `EfDashboardQueries` (`37A4DDD2…`, 168 lines) remain stale.

DI: `AddScoped<IDashboardQueries, EfDashboardQueries>()` in `DependencyInjection.cs` L37.

---

## 3. Call chain (measured)

```text
TradersPage  Link `/traders/${t.broker}/${t.login}`     // broker CODE
    → App.tsx  path="traders/:brokerId/:login"
        → TraderDetailPage  useParams() brokerId, login
            → useTraderDetail(broker, login)
                → GET {VITE_API_URL||http://localhost:5000}/api/traders/${broker}/${login}
                    → Program.cs MapGet("/api/traders/{broker}/{login:long}")
                        → IDashboardQueries.GetTraderDetailAsync(broker, login, ct)
                            → GetTraderAsync(broker, login)
                                → GetTradersAsync(broker, null)   // ALL scores + accounts + pnl group
                                → FirstOrDefault(t => t.Login == login)
                            → Brokers.SingleOrDefault(x => x.Code == broker)  // ordinal / provider equality
                            → ReconstructedTrades WHERE BrokerId && Login, ORDER BY ClosedAt ?? OpenedAt
                            → walk-flag first 3 Completed && CanonicalSymbol == "XAUUSD"
                            → TraderDetailDto(header, highlights) | null
```

No envelope. No auth. CORS `AllowAnyOrigin`. JSON enums via `JsonStringEnumConverter` (D74): `"SHADOW"`, `"Long"`, not ints.

---

## 4. `GetTraderDetailAsync` close-read

```125:160:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderDetailDto?> GetTraderDetailAsync(string broker, long login, CancellationToken ct)
    {
        var header = await GetTraderAsync(broker, login, ct);
        if (header is null)
            return null;

        var b = await _db.Brokers.AsNoTracking().SingleOrDefaultAsync(x => x.Code == broker, ct);
        if (b is null)
            return new TraderDetailDto(header, Array.Empty<TradeHighlightDto>());

        var trades = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.BrokerId == b.Id && t.Login == login)
            .OrderBy(t => t.ClosedAt ?? t.OpenedAt)
            .ToListAsync(ct);

        var firstThree = 0;
        var highlights = trades.Select(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
            if (first)
                firstThree++;
            return new TradeHighlightDto(
                t.PositionId,
                t.SourceSymbol,
                t.CanonicalSymbol,
                t.Direction,
                t.OpenedAt,
                t.ClosedAt,
                t.NetRealizedPnl,
                t.MaxVolumeLots,
                t.Completed,
                first);
        }).ToList();

        return new TraderDetailDto(header, highlights);
    }
```

### 4.1 What it does (honest)

| Step | Behavior |
|---|---|
| Header | Delegates to `GetTraderAsync` → `GetTradersAsync(broker, null)` → in-memory `FirstOrDefault` on `Login`. Same 14-field A92 row as the leaderboard. |
| Miss | `header is null` → `null`. Host serializes that as **HTTP 200 + body `null`** (§7). Not A93 404. |
| Broker re-lookup | `Brokers.Code == broker` after a **case-insensitive** list filter. Live: `achiever/10001` keeps the header and returns **`trades: []`** (§7.3). |
| Trade set | **Every** `reconstructed_trades` row for `(BrokerId, Login)` — any symbol, open or closed. Unbounded. |
| Order | `ClosedAt ?? OpenedAt` only. A93 wants `closedAt ASC, openedAt ASC, reconstructedTradeId ASC`. Open rows interleave by open time. |
| First-3 flag | Side-effecting `Select`: first three rows that are `Completed && CanonicalSymbol == "XAUUSD"`. Cap at 3. |
| Not checked | `closedAt IS NOT NULL`, `closedVolumeLots > 0`, `dirty == false`, `EligibleForFirstThree`. Entity has **no** dirty / eligible / lifecycle columns (D72). |
| Not projected | `ReconstructedTrade.Id`, VWAP, SL/TP, scale/partial/avg, fees, deal/order counts, `ClosedVolumeLots`, `InitialVolumeLots`. |
| Header holes inherited | `MlProbability` hardcoded `null` (good). `ShadowPnl` hardcoded `0` (D48: seed **does** write shadow orders for 10001/99001 — header lies). `BehaviorScore` on `TraderScore` is **dropped**. `LastScored` is the scorer clock, not A93 `generatedAt`. |

### 4.2 `GetTraderAsync` (header subroutine)

```119:123:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }
```

`GetTradersAsync` materializes **all** `TraderScores`, **all** `Brokers`, **all** `Mt5Accounts`, and a completed-PnL group-by, then filters broker in memory. Unique index talk on `(BrokerId, Login)` is unused. Class: **`UNSAFE`** as a keyed lookup (D49-2 still holds). HTTP no longer binds this method directly.

Existence rule vs A93 D5.1: 200 only if a **score row** survived the broker dictionary. An account-only or trades-only key with no `trader_scores` row returns `null` (200 `null`). Seed 10003 **has** a score, so N=0 still 200 with a header.

### 4.3 Application records (name collision)

```59:73:D:\Prop\src\Application\Dashboard\DashboardModels.cs
public sealed record TradeHighlightDto(
    long PositionId,
    string SourceSymbol,
    string CanonicalSymbol,
    TradeDirection Direction,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal NetRealizedPnl,
    decimal MaxVolumeLots,
    bool Completed,
    bool IsFirstThree);

public sealed record TraderDetailDto(
    TraderRowDto Header,
    IReadOnlyList<TradeHighlightDto> Trades);
```

A93 §5 / §13 require a 13-object root in `apps/api/Contracts/TraderDetailDto.cs` (`identity`, `accountOverview`, `firstThree`, `scores`, …). That folder does **not** exist. Stealing the type name is not compliance.

`TradeHighlightDto.PositionId` is `long` → JSON **number**. A93 D11: tickets / position ids are JSON **string**. `Direction` wires as `"Long"` / `"Short"` (enum name). A93 D12: `LONG` / `SHORT`.

---

## 5. `TraderDetailPage` close-read

Entire module is 56 lines: default export + local `Info`.

```1:56:D:\Prop\apps\web\src\pages\TraderDetailPage.tsx
import { useParams } from 'react-router-dom';
import { useTraderDetail } from '../api/hooks';

export default function TraderDetailPage() {
  const { brokerId = '', login = '' } = useParams();
  const { data, isLoading } = useTraderDetail(brokerId, login);
  if (isLoading) return <p className="text-gray-400">Loading trader…</p>;
  if (!data) return <p className="text-gray-400">Trader not found.</p>;
  const h = data.header ?? data;
  const trades = data.trades ?? [];
  // 8 Info chips + 4-column table + footnote
}

function Info({ k, v }: { k: string; v: any }) { /* gray card */ }
```

### 5.1 Line facts

| Lines | What | Note |
|---|---|---|
| 1–2 | `useParams` + hook only | No `Link` back to `/traders`. No `types`. No `formatters`. No SignalR. |
| 5–6 | `brokerId`, `login` default `''` | Hook `enabled: !!broker && !!login` skips the empty-string fetch. `login` stays **string**. |
| 7 | Loading copy | OK. |
| 8 | `!data` → “Trader not found.” | Covers 200 `null`, 204, and **also** network / 5xx (`isError` unused). Wrong copy for “API down”. |
| 9 | `h = data.header ?? data` | Tracks the thin-plus DTO **and** the older bare `TraderRowDto`. Compat, not A93 unwrap of `{ data }`. |
| 10 | `trades = data.trades ?? []` | Empty array on miss-shaped bodies. Lowercase-broker live case paints header + empty table with no empty-state copy. |
| 13 | Title `{h.broker} / {h.login}` | From header, not route. Fine when header exists. |
| 14–23 | 8 chips | State, Completed XAU, Early, Risk, Net P&L, Martingale, Averaging down, ML. |
| 22 | `h.mlProbability ?? 'not trained'` | Honest. Must **not** become `0` (C44). |
| 24–43 | Table | `Pos`, `Symbol`, `Net`, `First 3`. Rows typed `any`. Key = `positionId` (collides if two lifecycles share an id). |
| 39 | `t.isFirstThree ? 'yes' : ''` | Text flag. No row highlight, no `TRADE_1..3` badge, no waiting cards. |
| 44 | Footnote | “First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.” **ALIGNED** with §15 / A69. |
| 49–56 | `Info` | `v: any` → `String(v)`. Enums already strings on the wire. |

### 5.2 Header fields the page ignores

Present on live `header`, not painted:

| Field | Live 10001 | Why it matters |
|---|---|---|
| `group` | `demo\Maxmaster` | §51 account overview / identity |
| `lotEscalation` | `false` (10002: `true`) | A93 `riskFlags` — 10002 would hide ESC |
| `shadowPnl` | `0` (hardcoded) | §51 shadow P&L; value is a lie even if painted |
| `lastScored` | `2026-08-18T08:12:17.0457102+00:00` | scores freshness |

### 5.3 Trade fields the page ignores

Present on live `trades[]`, not painted: `sourceSymbol`, `direction`, `openedAt`, `closedAt`, `maxVolumeLots`, `completed`.

So the only “XAU history” the operator sees is position id + canonical symbol + net + yes/blank.

### 5.4 Hook (unchanged since 13:16)

```23:29:D:\Prop\apps\web\src\api\hooks.ts
export function useTraderDetail(broker: string, login: string) {
  return useQuery({
    queryKey: ['trader', broker, login],
    queryFn: () => client.get(`/api/traders/${broker}/${login}`).then(r => r.data),
    enabled: !!broker && !!login,
  });
}
```

No `refetchInterval` (focus-refetch off via `main.tsx` `refetchOnWindowFocus: false`, `staleTime: 30_000`). No `{ data }` unwrap. No `useQuery<TraderDetailDto>`. Query-key shape `['trader', broker, login]` matches A93’s suggested key **shape**, not the UUID identity.

`types/index.ts` `TraderDetail` / `Trade.isFirst3` / `ticket: number` has **0 imports** (D76). The page talks to JSON via `any`. Stub name `isFirst3` ≠ live `isFirstThree`.

---

## 6. Host map

```59:60:D:\Prop\apps\api\Program.cs
app.MapGet("/api/traders/{broker}/{login:long}", (IDashboardQueries q, string broker, long login, CancellationToken ct) =>
    q.GetTraderDetailAsync(broker, login, ct));
```

| Item | A93 / A26 | Measured |
|---|---|---|
| Path | `/api/v1/traders/{brokerId}/{login}` | `/api/traders/{broker}/{login:long}` |
| `brokerId` | UUID of `brokers.id` | **code** (`ACHIEVER`) |
| Auth / RBAC | Bearer + ReadOnly+ | **none** (D53) |
| Success | 200 + `{ data: TraderDetailDto }` | 200 + bare `{ header, trades }` |
| Missing | 404 `NOT_FOUND` | **200 + `null`** |
| Bad login | 400 `VALIDATION_FAILED` | `{login:long}` → ASP.NET **404** (`/api/traders/ACHIEVER/notanumber`) |
| Sub-resource `/trades` | required for full XAU history | **MISSING** as a keyed trader sub-resource. Host `GET /api/trades` dumps last 200 **raw EF entities** (optional `login` only; broker filter unused). |
| `PATCH .../state` | A63 | **MISSING** |
| `include=` embeds | scoreHistory, lotTimeline, … | **MISSING** |

---

## 7. Live HTTP (`http://127.0.0.1:5000`, this pass)

`GET /health` → **200**. Process is the demo host (in-memory / `EnsureCreated` + `DemoSeeder`). Not live Manager / not live FIX.

### 7.1 `GET /api/traders/ACHIEVER/10001` → **200** (1069 B)

Header: `broker=ACHIEVER`, `login=10001`, `group=demo\Maxmaster`, `completedXauTrades=3`, `netSourcePnl=223.60`, `earlyScore=95.50`, `mlProbability=null`, `riskScore=10`, flags all false, `state=SHADOW`, `shadowPnl=0`, `lastScored=2026-08-18T08:12:17.0457102+00:00`.

Trades (all `completed=true`, `canonicalSymbol=XAUUSD`, `isFirstThree=true`):

| positionId | direction | netRealizedPnl | maxVolumeLots | openedAt → closedAt |
|---:|---|---:|---:|---|
| 501 | `Long` | 151.4 | 0.1 | 2026-06-01T08:00Z → 08:45Z |
| 502 | `Short` | −89.40 | 0.1 | 11:00Z → 11:45Z |
| 503 | `Long` | 161.6 | 0.1 | 14:00Z → 14:45Z |

Demo tape happens to be three completed XAU, so the boolean column and A93 ranks 1–3 **coincide**. That is fixture luck, not `FirstThreeBlock`.

### 7.2 `GET /api/traders/ACHIEVER/10002` → **200**

`completedXauTrades=3`, `netSourcePnl=-2107.0`, `earlyScore=42.50`, `riskScore=70`, `martingale=true`, `lotEscalation=true`, `state=RISK_BLOCKED`. Positions 601/602/603, lots 0.1 / 0.2 / 0.4, all `isFirstThree=true`. Page would hide **lot escalation** (not a chip).

### 7.3 `GET /api/traders/ACHIEVER/10003` → **200** (332 B)

`completedXauTrades=0`, `netSourcePnl=0`, **`earlyScore=40`**, `riskScore=10`, `state=INSUFFICIENT_DATA`, `trades=[]`.

A93 T2 / A92 L7: N=0 → three **waiting** slots, official scores **`null`**, state `INSUFFICIENT_DATA`. State is correct; publishing `40` is the C23 provisional leak; no slots.

### 7.4 `GET /api/traders/STARWAVEFX/99001` → **200**

Compound identity works for a second broker. Positions 701–703, lots 0.05, `state=SHADOW`, `earlyScore=95.50`, `shadowPnl=0`. Distinct from Achiever 10001. A93 T13 **PARTIAL** (code+login, not UUID+login).

### 7.5 `GET /api/traders/ACHIEVER/99999` → **200**, body `null`

A93 T1 requires **404** and no `data`. D39 claimed **204**. **Measured: 200 + JSON `null`.** Axios `r.data === null` → page “Trader not found.” (works by accident).

### 7.6 `GET /api/traders/achiever/10001` → **200**, header + **`trades: []`**

Confirms §4.1 case split: `GetTradersAsync` filter is `OrdinalIgnoreCase`; broker re-lookup is `Code == broker`. Header chip page, empty table, no error. **UNSAFE** demo footgun.

### 7.7 `GET /api/traders/ACHIEVER/notanumber` → **404**

Route constraint, not A93 400 `VALIDATION_FAILED`.

No `{ data: … }` wrapper on any of the above.

---

## 8. §51 block matrix (page + payload)

Architecture §51 list, scored against **this** GET + **this** page (not against unused `types/index.ts`).

| # | §51 block | On `GetTraderDetailAsync` | Painted by `TraderDetailPage` | Class |
|---:|---|---|---|---|
| 1 | Account overview | **No** (`Mt5Account` balance/equity/margin/leverage exist, unused) | **No** | **MISSING** |
| 2 | XAU trade history | All symbols, unbounded; not XAU-only; not the `/trades` sub-resource | 4 of 10 highlight fields | **PARTIAL** |
| 3 | First 3 trades highlighted | Boolean on the dump | `yes` / blank cell | **PARTIAL** / **MISSING** as 3-slot block |
| 4 | Score timeline | **No** (`TraderScoreHistory` unused) | **No** | **MISSING** |
| 5 | Risk flags | Header has 3/5 (`abnormalSizing` / `severeRisk` absent) | 2/5 (hides `lotEscalation`) | **PARTIAL** |
| 6 | Behavior features | Net PnL only (all-completed, not eligible-XAU set) | Net P&L chip | **PARTIAL** |
| 7 | Lot-size timeline | `maxVolumeLots` per row, no series | **No** | **MISSING** |
| 8 | Holding-time distribution | Open/close times present, unused | **No** | **MISSING** |
| 9 | SL/TP behavior | Entity has SL/TP; DTO drops them | **No** | **MISSING** |
| 10 | Drawdown | **No** | **No** | **MISSING** |
| 11 | MFE/MAE when valid | **No** (honest omit; A45 still UNAVAILABLE — D56/D57) | **No** | **MISSING** (allowed as honest null; not published as `UNAVAILABLE`) |
| 12 | Shadow copied positions | **No** | **No** | **MISSING** |
| 13 | Shadow P&L | Header `shadowPnl=0` always | **No** | **MISSING** (and header value is false) |
| 14 | Live copied positions | **No** | **No** | **MISSING** |
| 15 | Live P&L | **No** | **No** | **MISSING** |
| 16 | Source-to-destination mapping | **No** | **No** | **MISSING** |

**~2 / 16** partial. A62 §10.5 ECharts modules: **0**.

A26 §6.5 boolean `firstThreeTradesHighlighted` + `[0,1]` scores + `BUY`/`SELL` is **superseded by A93**. Implementation matches **neither** A26 nor A93.

---

## 9. A93 root objects (0 / 13)

| # | A93 root | On the wire |
|---:|---|---|
| 1 | `generatedAt` | **absent** |
| 2 | `identity` | **absent** (flattened into `header.broker` code + login + group + state) |
| 3 | `accountOverview` | **absent** |
| 4 | `firstThree` | **absent** |
| 5 | `completedXauTrades` | on `header` only |
| 6 | `openXauSourcePositions` | **absent** |
| 7 | `scores` | flattened; no `version`, no `behaviorScore`, no `earlyQualityScore` alias, no freeze |
| 8 | `riskFlags` | 3 bools on header; no `abnormalSizing` / `severeRisk` |
| 9 | `behaviorFeatures` | **absent** (net PnL on header is all-completed) |
| 10 | `shadow` | **absent** |
| 11 | `live` | **absent** |
| 12 | `sourceDestinationPreview` | **absent** |
| 13 | `embeds` | **absent** |

`earlyScore === earlyQualityScore` cannot be checked: the latter is never serialized. `TraderScore.BehaviorScore` is loaded by `GetTradersAsync` and discarded.

---

## 10. First-3 ranking vs A93 D2–D4 / D72

| Rule | A93 / A21 | `GetTraderDetailAsync` |
|---|---|---|
| Unit | completed reconstructed **XAUUSD** lifecycle | `Completed && CanonicalSymbol == "XAUUSD"` |
| Dirty / `EligibleForFirstThree` | exclude | **not persisted, not filtered** (D72) |
| `closedAt` / `closedVolumeLots > 0` | required | **not required** |
| Sort | `closedAt, openedAt, id` | `ClosedAt ?? OpenedAt` |
| Shape | always 3 slots; missing = `waiting` | unbounded list + boolean |
| Rank 3 latch | `EARLY_SCORE_ELIGIBLE` on slot 3 | **not emitted** |
| N≥4 | ranks 1–3 frozen; extras on `/trades` | first three in walk order stay flagged **if** the same filter/order is stable; extras still dumped here with `isFirstThree=false` |
| Persist | `first3_keys` | **recomputed every GET** |
| Page | three highlight cards; do not re-rank in the browser | prints `yes` |

Seed 10001 is N=3 clean XAU, so T6-shaped **data** exists and the boolean looks right. T3 (2 XAU + partial + open) is **not** what this host returns for any seed login. Empty 10003 does **not** render waiting cards.

Page footnote is the only place the product states the first-3 **policy** correctly.

---

## 11. Acceptance tests T1–T16 (A93 §16)

Grep of `D:\Prop\tests` for `GetTraderAsync` / `GetTraderDetailAsync` / `TraderDetailDto` / `TradeHighlightDto` / `TraderDetailPage` / `firstThree`: **0 hits**.

| ID | Expect | Live / source | Status |
|---|---|---|---|
| T1 | Unknown key → 404, no `data` | 200 `null` | **FAIL** |
| T2 | Account N=0 → 3 waiting slots, scores null, `INSUFFICIENT_DATA` | 10003: state OK; `earlyScore=40`; `trades=[]`; no slots | **FAIL** |
| T3 | 2 XAU + partial + open → `count=2`, slot 3 waiting | no such fixture on this GET; no `count` | **FAIL** |
| T4 | Non-XAU only → `count=0` | no `count` | **FAIL** |
| T5 | Dirty completed XAU excluded | dirty not persisted / not filtered | **FAIL** |
| T6 | Third close → `EARLY_SCORE_ELIGIBLE`, not `PROVEN_PROFITABLE`, not LIVE | 10001 is `SHADOW` (good); trigger token absent | **FAIL** (token) / **PARTIAL** (state) |
| T7 | N=7 → still 3 highlighted; `/trades` has 7 | no `/trades` sub-resource; detail dumps all | **FAIL** |
| T8 | `earlyScore === earlyQualityScore` | `earlyQualityScore` absent | **FAIL** |
| T9 | `mlProbability` JSON `null` | live `null` | **PASS** |
| T10 | No ticks → mfe/mae null + `UNAVAILABLE` | fields absent | **FAIL** |
| T11 | Alias `XAUUSDm` / `GOLD` counts if mapped | unproven here; flag uses persisted `CanonicalSymbol` | **UNPROVEN** |
| T12 | Same book twice → bit-identical first-three ids | no `reconstructedTradeId` on highlight | **UNPROVEN** |
| T13 | Achiever 1001 vs StarwaveFX 1001 distinct | 10001 vs 99001 both 200 | **PARTIAL** |
| T14 | Live flag off → live pnl 0, empty dest ids | `live` object absent | **FAIL** |
| T15 | Sanitizer / denylist | allow-list DTO; no manager login on this payload | **PARTIAL** |
| T16 | High score at N=3 → not LIVE | 10001 `SHADOW`, page footnote | **PARTIAL** (no test; live state agrees) |

**1 / 16** hard pass (T9). A93 §18: do not call §51 done before T1–T16.

---

## 12. Data already on disk that this GET ignores

Queryable today, unused by `GetTraderDetailAsync` beyond the thin header + highlight slice:

| Already on disk | A93 / §51 field |
|---|---|
| `Mt5Account.Balance/Equity/Margin/MarginFree/Profit/Leverage/LastSyncedAt` | `accountOverview.*` |
| `Mt5Account.GroupName` (already on header) | `identity.group` — page ignores it |
| `TraderScore.BehaviorScore` | `scores.behaviorScore` |
| `TraderScoreHistory` | `embeds.scoreHistory` |
| `ReconstructedTrade` VWAP, SL/TP, volumes, scale/partial/avg, fees, `Id` | `FirstThreeTradeDto` / trade history |
| `Mt5Positions` | `openXauSourcePositions` |
| `ShadowOrders` (D48: 6 rows after seed for 10001+99001) | shadow book / P&L — header still `0` |
| `CopyIntent` / `ExecutionIntent` | `sourceDestinationPreview` |
| MFE/MAE | omit + `UNAVAILABLE` until A45 (D56/D57) |

Thinness is a **projection choice**, not a missing table for overview / first-3 **shape**. Dirty / `first3_keys` / FIRST3-freeze / `trader_states` remain real persist gaps.

Secrets: this DTO does **not** serialize `Broker.ManagerLogin`, FIX passwords, or connection strings. Class **PASS** on D14 for this resource (raw `/api/trades` is a different, **UNSAFE** map).

---

## 13. Stale snapshots (do not copy)

| Older claim | This measure |
|---|---|
| A93 §15: weatherforecast; 0 routes; `TraderDetailDto` MISSING; React page absent | Unversioned detail GET exists. Application has a **different** `TraderDetailDto`. `TraderDetailPage.tsx` exists (untracked). |
| A93 intro / B06 / B20 / B29 / C04 / D02: HTTP = `GetTraderAsync` same as list | Host binds **`GetTraderDetailAsync`**. Header is still the list row. |
| D39 / D49: miss = **204** | Live miss = **200 `null`**. |
| D21 / C36: `EfDashboardQueries` 168 / 7407 / `37A4DDD2…`, 7 methods, HTTP = `GetTraderAsync` | 205 / 8708 / `328D0924…`, **8** methods, HTTP = `GetTraderDetailAsync`. |
| C13: “no first-3 trade block” on the page | Page now has a **First 3** column. Still no block. |
| B30: page will break if the host returns `{ header, trades }` | Page already does `data.header ?? data` / `data.trades ?? []`. |
| B29: integer enums on the wire | **Stale.** Live `"SHADOW"` / `"Long"` (D74). |

A93 **contract** is not stale. A93 **gap table §15** is. D49 verdict on thinness **still holds**; this file adds live HTTP and a page close-read.

---

## 14. What must change (spec only — not done in this pass)

Restated from A93 §18 against the current fork. **Do not implement from this report.**

1. Stop giving the 2-field Application record the A93 name. Put allow-list records in `apps/api/Contracts/` (`FirstThreeBlockDto`, `TraderIdentityDto`, …). Keep `GetTradersAsync` on `TraderRowDto`.
2. Keyed query: `brokers.code` **or** `brokers.id` + login. **Do not** call `GetTradersAsync` for one trader. Use one collation for header and trade lookup (the `achiever` vs `ACHIEVER` split is a live bug).
3. Build `FirstThreeBlock` from A21 eligibility + sort; always 3 slots; do not dump the full book on this GET.
4. Map overview / scores (`behaviorScore`, `earlyQualityScore == earlyScore`, `version=baseline.v1`) / flags / honest feature nulls / shadow+live summaries. `mlProbability` stays null. MFE/MAE stay null + `UNAVAILABLE` until A45.
5. N<3 official scores **`null`**, not `40`. 10003 is the fixture.
6. Version the route `/api/v1/traders/{brokerId}/{login}`, envelope `{ data }`, **404** not 200 `null`, ReadOnly+ auth.
7. Retarget the page to that payload. Three slot cards. Paint `lotEscalation`. Do not re-rank in the browser. Keep the live-promotion footnote.
8. Tests T1–T16.

Non-goals: do not invent ticks, do not auto-LIVE, do not put first-3 dollar PnL in the ranking key, do not modify product source in this swarm file.

---

## 15. Related reports

| File | Role |
|---|---|
| `A93_trader_detail_dto.md` | Binding detail contract |
| `A92_leaderboard_dto.md` | Binding **list** row — what the header still is |
| `A21_reconstruction_spec.md` / `D72_first3.md` | First-3 eligibility vs dashboard recompute |
| `A26_dashboard_api_spec.md` §6.5 | Superseded sketch |
| `A62_react_scaffold.md` §10.5 | Page module inventory |
| `D49_detail_thin.md` | `GetTraderAsync` vs A93; wrapper already judged thin |
| `D39_hooks.md` / `D30_api.md` / `D06_api_census.md` | Host already calls this method (204 claim superseded by §7.5) |
| `D76_types.md` | `types/index.ts` unused / wrong |
| `D78_traders.md` | List page that links here |
| `C23_empty_trader.md` | 10003 `INSUFFICIENT_DATA` + quality 40 leak |
| `C44_honesty_no_ml.md` | `not trained` copy is allowed |
| `D48_shadow_rows.md` | Header `shadowPnl=0` is false for 10001/99001 |

---

## 16. Classification close

| ID | Surface | Class |
|---|---|---|
| E009-1 | `GetTraderDetailAsync` vs A93 payload | **`MISSING`** (method exists; contract does not) |
| E009-2 | `GetTraderDetailAsync` as demo table | **`EXISTS_NEEDS_REFACTOR`** |
| E009-3 | Header via full `GetTradersAsync` | **`UNSAFE`** |
| E009-4 | Case-split `Code == broker` after CI filter | **`UNSAFE`** (live empty trades) |
| E009-5 | Application `TraderDetailDto` name | **`EXISTS_NEEDS_REFACTOR`** (collision) |
| E009-6 | `TraderDetailPage.tsx` vs §51 | **`EXISTS_NEEDS_REFACTOR`** (file) / **`MISSING`** (16-block page) |
| E009-7 | `GET /api/v1/traders/{brokerId}/{login}` + envelope + 404 + RBAC | **`MISSING`** |
| E009-8 | `FirstThreeBlock` | **`MISSING`** |
| E009-9 | T1–T16 | **`MISSING`** (1 accidental pass: T9) |
| E009-10 | Secrets on this DTO | **PASS** |
| E009-11 | `mlProbability` + page copy | **PASS** |
| E009-12 | Live-promotion footnote | **PASS** |
| E009-13 | Unused `types/index.ts` `TraderDetail` | **`DEPRECATED`** |

**§51 / A93 trader detail is not implemented.** `GetTraderDetailAsync` is a header-plus-table wrapper around the A92 row. `TraderDetailPage` is the only consumer and it paints that wrapper. Naming either surface “detail” does not make it the §51 document.

**Measured at close:** 2026-08-18T13:50:12+05:30. Product source unmodified.
