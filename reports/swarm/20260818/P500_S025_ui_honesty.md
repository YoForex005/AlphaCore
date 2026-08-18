# P500_S025 — UI honesty: no live profit, warning text, dest P&L = 0

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S025_ui_honesty.md` |
| Agent | P500_S025 (read-only UI honesty; LiveCopy / Overview / Traders + hooks) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (source read; no product edit; no HTTP launch in this slot) |
| Workspace | `D:\Prop` (Vite app is `D:\Prop\apps\web`, **not** under `D:\Prop\src`) |
| Assigned | Read `LiveCopyPage.tsx`, `OverviewPage.tsx`, `TradersPage.tsx`, API hooks. Write this file. **UI must not imply live profit.** Confirm warning text. Destination P&L is **0**. **Do not edit product.** |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** |
| Precedence | On-disk TSX is the UI. Wire values come from `GET /api/*`. Backend literal `destinationRealPnl = 0` is an API floor, not a React invention. `A013_web_honesty.md` is **stale** on LiveCopy warning text, Overview subtitle, and axios timeout. |
| Relates | A013 (stale web honesty), C37 / D81 / P500_CODE_10 / P500_CODE_30 (live stub), D77 (overview), D39 (hooks), D21 (queries), E031 |

This is a **web honesty census**. It is **not** a §68 / §69 / §70 PASS and does **not** authorize `REAL_COPY_EXECUTION_ENABLED=true`.

---

## 0. Binding answers

| Question | Answer | Class |
|---|---|---|
| Does the UI claim **live dest profit**? | **No invented dest book.** No dest rows, no dest fill list, no `PnlChart` on `/live`. Overview paints `destinationRealPnl` as **`0.00`**. Traders **Net P&L** is **source** reconstructed P&L (`netSourcePnl`), not Pepperstone/cTrader P&L. | **PASS** (no live-profit claim) |
| Warning text present and honest? | **Yes.** Three static/API-backed warnings (verbatim below). `/live` amber copy says SHADOW-only + NewOrderSingle disabled + remaining gates. Overview subtitle says live FIX send is off. | **CONFIRMED** |
| Destination P&L is 0? | **Yes, by constructor literal.** `GetOverviewAsync` passes `0` as `DestinationRealPnl`. The query never reads dest positions (table absent). The number **cannot become non-zero** in this tree. | **CONFIRMED** |
| Product edited this slot? | **No.** | — |

**One-liner:**

```text
UI does not invent live dest profit.
/live warning is SHADOW-only + NOS disabled + gates.
Dest. real P&L is API literal 0 (painted as 0.00).
Traders Net P&L is source tape, not Pepperstone.
```

---

## 1. Warning text (verbatim, CONFIRMED)

### 1.1 `/live` — `LiveCopyPage.tsx` (entire module)

`D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` — 8 physical lines, **0 imports**, **0 hooks**.

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
| `NewOrderSingle` | **1** — English: **disabled** |
| Profit / dest P&L node | **0** |
| Buttons / forms / POST / fetch | **0** |
| `useQuery` / `useLive` | **0** |
| Hardcoded `ON` | **0** |

`A013` quoted the **old** body (`REAL_COPY_EXECUTION_ENABLED is false. This page will stay empty until go-live gates pass.`). That string is **gone**. Current amber copy is the binding text.

### 1.2 Overview subtitle + footer

`D:\Prop\apps\web\src\pages\OverviewPage.tsx`

- L15: `Live Manager ingest from Achiever + Starwave. Live FIX NewOrderSingle is off — no capital at risk from this dashboard.`
- L32: `Real copy execution: {data.realCopyEnabled ? 'ON' : 'OFF'}. Trade #3 never auto-promotes to LIVE.`

`realCopyEnabled` is the last `OverviewDto` field. Host constructor: `_runtime.RealCopyEnabled`. DI pins `RealCopyEnabled = false` (`D:\Prop\src\Infrastructure\DependencyInjection.cs` L41). FIX logon host re-pins `false` (`CTraderFixLogonHostedService.cs` L68). Overview will paint **OFF** unless a later host flips the runtime bool.

### 1.3 Adjacent static warnings (not assigned pages, same honesty contract)

| Surface | Verbatim |
|---|---|
| `ShadowPortfolioPage.tsx` | `Live NewOrderSingle remains disabled.` |
| `TraderDetailPage.tsx` | `First 3 completed XAUUSD trades unlock EARLY_SCORE / SHADOW only. Live promotion is not automatic.` |
| `LiveRuntimeStatus.Snapshot().copyNote` (ingest JSON on Overview) | When flag false: `NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.` |

---

## 2. Destination P&L is 0

### 2.1 API floor (cannot go non-zero)

`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` `GetOverviewAsync` return:

```csharp
return new OverviewDto(
    accounts,
    brokers,
    xauTraders,
    three,
    /* WATCH / SHADOW / LIVE_CANDIDATE / LIVE / RISK_BLOCKED counts */,
    shadowPnl,   // Sum(ShadowOrders.SourceVsShadowSlippage) — slippage, not dest P&L
    0,           // DestinationRealPnl  ← literal
    0,           // XauGross
    0,           // XauNet
    /* health bits */,
    _runtime.RealCopyEnabled);
```

DTO field: `OverviewDto.DestinationRealPnl` (`DashboardModels.cs` L16).

UI bind (`OverviewPage.tsx` L27):

```tsx
<MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
```

Painted value is always **`0.00`** while this query exists. React does **not** invent a dest book. MetricCard uses default `text-gray-100` (not green), so a zero is not colored as profit.

### 2.2 Other dest-P&L surfaces

| Surface | Dest P&L? |
|---|---|
| `LiveCopyPage` | **none** (no number) |
| `TradersPage` **Net P&L** | `t.netSourcePnl` — Σ completed `ReconstructedTrades.NetRealizedPnl` **on source MT5**, not dest |
| `TraderRowDto.ShadowPnl` | query literal **`0`**, **never read** by any page |
| `RiskPage` **Daily P&L** | `RiskDashboardDto` first arg **`0`** (`GetRiskAsync`) |
| `types/index.ts` `realPnl` | **unused** (0 imports of that module) |
| `PnlChart` | unused widget |

**Verdict:** destination real P&L is **0**. Do not treat Overview `0.00` as “we measured a flat live book.” It is a **hardcoded floor**.

---

## 3. Assigned pages vs live-profit implication

### 3.1 `LiveCopyPage` — does not imply profit

Chrome title “Live copy portfolio” is a §46 nav leaf, not a P&L. Body forbids a losing live open. No fills, no dest rows, no green number.

Residual nit (not a live-profit claim): sidebar label is abbreviated **`Live`** (`DashboardLayout.tsx`). The page body contradicts “we are live.” Prefer architecture §46 `Live Copy Portfolio`. **Do not** add dest P&L to “complete” the page.

### 3.2 `OverviewPage` — dest tile is 0; shadow tile is **not** dest profit

| Tile | Bind | Honesty |
|---|---|---|
| Shadow P&L | `data.shadowPnl` | **Wrong noun.** Backend is Σ `SourceVsShadowSlippage`, not shadow MTM profit. Can look like “we made money.” |
| Dest. real P&L | `data.destinationRealPnl` | Number is honest **0**. Label “real P&L” can be read as “a live dest book exists and is flat.” Footnote on L15/L32 mitigates. |
| Live candidates | `data.liveCandidates` | Trader **state count**, not dest positions. |
| Real copy execution | `data.realCopyEnabled` | Ternary; currently **OFF**. |

Overview subtitle is explicit: live FIX send is off; no capital at risk **from this dashboard**. That is the required anti-profit frame.

### 3.3 `TradersPage` — source P&L only

`D:\Prop\apps\web\src\pages\TradersPage.tsx`

- Hook: `useTraders({})` → `GET /api/traders` (unfiltered).
- Column header: **Net P&L**.
- Cell: `Number(t.netSourcePnl).toFixed(2)`.

Field name on the wire is `netSourcePnl`. That is **source-manager reconstructed realized P&L**, not Pepperstone dest P&L. The page does **not** show `shadowPnl`, dest P&L, or a live-copy badge. States (`WATCH` / `SHADOW` / `LIVE_CANDIDATE` / `RISK_BLOCKED` / …) are source-trader states.

Honesty nit: the column says “Net P&L” without the word **source**. An operator who just left `/live` could misread it as dest. The DTO name is correct; the header is abbreviated. **Not** a fabricated dest profit.

No error UI (unlike Overview). Loading only. Not a profit lie.

### 3.4 Hooks — read-only GETs, no live book

`D:\Prop\apps\web\src\api\hooks.ts` (11 `useQuery`, **0 mutations**).

| Hook | Path | Used by assigned pages |
|---|---|---|
| `useOverview` | `GET /api/overview` every 4 s | Overview |
| `useIngestStatus` | `GET /api/ingest/status` every 2 s | Overview (broker dump) |
| `useTraders` | `GET /api/traders` every 5 s | Traders (and Scoring) |
| others | brokers/groups/detail/trades/fix/risk/recon/health/settings | not assigned |

No `useLive`, `useShadow`, `useMutation`, no POST that could send `35=D`.

Transport (`client.ts`): `timeout: 60000`, `baseURL` `VITE_API_URL` or `http://localhost:5000`. **A013’s `timeout: 15000` is stale.**

---

## 4. What would imply live profit (not present)

| Forbidden claim | Present? |
|---|---|
| Hardcoded dest P&L ≠ 0 | **No** |
| Copy of `netSourcePnl` into dest / live fields | **No** |
| Green “profit” styling on dest tile | **No** (`MetricCard` default gray) |
| Dest positions / `clOrdId` / live fills table | **No** |
| `PnlChart` of dest / live | **No** (unused) |
| “Live copy ON” as a hardcoded fact | **No** |
| Enable / flatten / send buttons | **No** |
| `/live` fetching a portfolio P&L | **No** |

---

## 5. Residual honesty nits (not “we are making live money”)

These are **label / chrome** issues. They do **not** invent a dest book. Do **not** “fix” them in this slot (product freeze).

1. **Shadow P&L noun** — Overview will show slippage Σ (demo historically 248.20) under a P&L label. That can be mistaken for profit. It is **not** dest real P&L.
2. **Dest. real P&L tile exists** — showing `0.00` is numerically true and capability-false (no dest ledger). Footer + subtitle are the required counterweight.
3. **Traders “Net P&L”** — source tape; header omits “source”.
4. **Nav `Live`** — chrome, not status. Body contradicts.
5. **`behaviorScore ?? 0` on Scoring** (adjacent) — frontend zero fabricate; not dest P&L.
6. **Risk Daily P&L `0.00`** — same API-zero class as dest P&L.
7. **`types/index.ts`** still has unused `realPnl` / `pnl` shapes. Dead types must not be wired.

---

## 6. Scorecard

| Check | Result |
|---|---|
| `/live` warning text (current) | **CONFIRMED** — SHADOW only + NOS disabled + gates |
| Overview “NewOrderSingle is off / no capital at risk” | **CONFIRMED** |
| Overview “Trade #3 never auto-promotes to LIVE” | **CONFIRMED** |
| Dest. real P&L API value | **CONFIRMED `0`** (literal in `GetOverviewAsync`) |
| Dest. real P&L UI paint | **`0.00`**, not green |
| React invents dest / live profit | **PASS** (none) |
| Traders P&L is dest / live | **PASS** (it is `netSourcePnl`) |
| UI claims live copy ON | **PASS** (never hardcoded; API paints OFF) |
| Product edited | **No** |

§73.B: assigned pages as an **anti-live-profit** surface: **EXISTS_AND_GOOD** (warnings + dest 0 + no dest book).  
§73.B as a working Live Copy Portfolio / dest ledger: **MISSING** (correct until send exists).  
Shadow-P&L noun: **EXISTS_NEEDS_REFACTOR** (label only).

---

## 7. What a later wave must not do

1. Do **not** copy `netSourcePnl` or shadow slippage into `destinationRealPnl`.
2. Do **not** invent dest rows, dest P&L, or a green **ON** to make `/live` look finished.
3. Do **not** drop the amber `/live` warning or the Overview “NewOrderSingle is off” line.
4. Do **not** treat dest `0.00` as measured venue P&L.
5. Do **not** enable `REAL_COPY_EXECUTION_ENABLED` from the UI.
6. Do **not** treat this file as permission to send `35=D`.

---

## 8. Evidence pins

- `D:\Prop\apps\web\src\pages\LiveCopyPage.tsx` — current warning (this file §1.1).
- `D:\Prop\apps\web\src\pages\OverviewPage.tsx` L15, L27, L32.
- `D:\Prop\apps\web\src\pages\TradersPage.tsx` L18, L31 (`Net P&L` / `netSourcePnl`).
- `D:\Prop\apps\web\src\api\hooks.ts` — GET-only; no live hook.
- `D:\Prop\apps\web\src\api\client.ts` — `timeout: 60000`.
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` — dest P&L literal `0`; trader `ShadowPnl` literal `0`; risk zeros.
- `D:\Prop\src\Application\Dashboard\DashboardModels.cs` — `DestinationRealPnl`.
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` L41 — `RealCopyEnabled = false`.
- Prior: A013 (stale warning/timeout), C37, D81, P500_CODE_10, P500_CODE_30.

---

## One-line close

**The UI does not imply live dest profit: `/live` is a SHADOW-only / NewOrderSingle-disabled warning with no P&L, Overview dest real P&L is the API literal 0 painted as 0.00, and Traders Net P&L is source reconstructed tape.**
