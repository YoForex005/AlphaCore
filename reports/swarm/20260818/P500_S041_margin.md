# P500_S041 — `MaxMarginUsage=0.70` is gold liquidation territory; dest cap belongs at 10–20% and is not wired to live equity

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **P500_S041** |
| Agent | P500_S041 (senior risk / dest-margin, read-only) |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S041_margin.md` |
| SUT | `D:\Prop\src\Domain\Risk\RiskEngine.cs` (`RiskLimits.MaxMarginUsage`, `RiskEvaluationRequest.MarginUsage`, `Evaluate` L138–139) |
| Product caller | `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` L180 (`MarginUsage = 0`) |
| Tests | `D:\Prop\tests\Unit\RiskEngineTests.cs` (fixture `MarginUsage = 0.1m`; **no** `MAX_MARGIN_USAGE` fact) |
| Architecture | `MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md` §39 “max execution account margin usage”; dest free-margin / margin-level on overview §47 |
| Spec siblings | A23 (`MAX_MARGIN_USAGE` hard cap + `INSUFFICIENT_MARGIN`); A43 §4.6 `room_margin_oz`; A71 G28; A95 `maxMarginUsage: 0.70`; B36 `RF-RA-MARGIN`; C03/D35/E017 missing case; P500_S003 reason catalog row 17 |
| Product source modified | **No** |
| Test source modified | **No** |
| Secrets printed | **None** |
| Live FIX send | **None** (`NewOrderSingleImplemented = false`; persist forces `AllowFixSend = false`) |
| Capital at risk from this process | **None** (`SAFE_BY_ABSENCE` on the wire). The **policy number is still wrong** and would be unsafe if a send hop appeared. |

Classification: **UNSAFE as a dest-gold control** (`0.70` + pre-trade-only + caller-invented fraction). **MISSING as a live-equity gate** (no dest equity / free-margin / leverage on the request; only product path stamps `0`). **SAFE_BY_ABSENCE** as a Pepperstone / `35=D` choke.

---

## 0. Verdict

`RiskLimits.MaxMarginUsage` defaults to **`0.70m`** (70% of whatever the caller claims `MarginUsage` is). On **XAUUSD / gold**, 70% dest margin usage is **liquidation territory**, not a conservative copy cap.

Lower dest loss requires a **10–20% dest margin-usage cap** (`0.10`–`0.20`), computed from **live destination equity and used margin**, and checked **after** the proposed size (A43 `room_margin_oz`), not against a caller-supplied snapshot that the only product path hardcodes to **zero**.

Today the 70% line **cannot fire on the copy hop**: `CopyTradingService.GenerateShadowIntentsAsync` builds `RiskEvaluationRequest` with `MarginUsage = 0`. `0 > 0.70` is false. The check is a vocabulary stub.

Do **not** treat `MaxMarginUsage = 0.70` as a go-live number. Do **not** edit product in this slot.

---

## 1. File identity (re-read 2026-08-18)

| Type | Path | Role |
|---|---|---|
| `RiskLimits` | `src\Domain\Risk\RiskEngine.cs` L5–22 | Compile-default policy. `MaxMarginUsage { get; init; } = 0.70m` at **L19**. |
| `RiskEvaluationRequest` | same file L32–56 | Caller snapshot. `MarginUsage` is a **required decimal** at **L53**. No equity, balance, free margin, margin level, leverage, dest account id. |
| `RiskEngine.Evaluate` | L76–172 | First-blocking-check sequence. Margin branch **L138–139**. |
| `Reject` | L180–188 | Always `ApprovedQuantity = 0`, `AllowFixSend = false`. |

Prior swarm hash of this unit (D13 / E005, same 189-line shape): `AE0F9FAE846FF45672207570189C5ED296F4C651F40F2C6C1AFA131CEA79052D`. This slot re-read the file; it did not re-hash.

`docs\risk.md` does **not** mention dest margin usage. Its 5% / 10% / 50-lot table is a **stale marketing doc** and must not be used as the dest-margin policy.

---

## 2. What the engine actually does

```text
RiskLimits.MaxMarginUsage          = 0.70m          // L19, compile default
request.MarginUsage                = <caller>       // L53, not computed
if (request.MarginUsage > 0.70
    && action is OpenExposure or IncreaseExposure)
        Reject(MAX_MARGIN_USAGE)                   // L138–139
```

Measured properties of that branch:

| Property | Measured |
|---|---|
| Comparator | **strict `>`** — `MarginUsage == 0.70` **approves** |
| Action filter | `IsIncreasing` only (`OpenExposure` / `IncreaseExposure`) |
| Reduce / close | **Not checked** (A71 G28: closing frees margin — directionally OK) |
| Outcome | `RiskDecisionOutcome.Reject` (hard). Not A43 `ReduceSize` / clip to `room_margin_oz` |
| Quantity after hit | `0` via `Reject()` |
| Post-trade projection | **None.** Does not add `RequestedQuantity` to used margin |
| Quote / leverage / contract size | **Unused** by this check |
| Config binder | **None.** `new RiskEngine()` / DI `AddSingleton<RiskEngine>()` both take default `RiskLimits` |
| Settings API | `SettingsController` exposes `MaxDailyDrawdownPct` / `MaxPositionSize` / `MaxOpenPositions` / `KillSwitchEnabled`. **No `MaxMarginUsage`.** Redis keys do not include it. |
| Unit coverage | **Absent.** Five facts. Fixture stamps `0.1m`. No `0.71` / `0.70` / reduce-path cell (C03 M14, D35 #17, E017 #17, A89 class 50 still phantom) |

`MaxSlippage` (L18, `1.5m`) is declared and **never read**. Margin at least has a branch; it is still not a live-equity gate.

---

## 3. Not wired to live equity (measured)

`RiskEvaluationRequest` has **no** dest money fields. The engine cannot compute `used / equity`. It compares two decimals the **caller invented**.

| Field the A43 / §39 dest-margin gate needs | On `RiskEvaluationRequest`? | Where dest truth would live |
|---|---|---|
| Destination equity | **No** | cTrader TRADE account / FIX collateral. **Not** `Mt5Account.Equity` (that is **source** MT5). |
| Destination used margin | **No** | Same dest book. |
| Destination free margin | **No** | Architecture overview “Destination free margin” — `OverviewDto` today has **no** dest margin fields (`DashboardModels.cs` L5–22). |
| Destination margin level | **No** | Spec §47; product DTO omitted. |
| Account leverage | **No** | A43: `required_margin_per_oz = dest_side_price / account_leverage`. |
| Available margin after this order | **No** | A43 `room_margin_oz`. Engine never computes it. |
| `INSUFFICIENT_MARGIN` reason | **Not emitted** | A23 lists it; Evaluate never returns it. |

`Mt5Account` (`src\Domain\Entities\Mt5Account.cs`) **does** carry `Balance`, `Equity`, `Margin`, `MarginFree`, `Leverage` — those are **source manager-API snapshots** (Achiever / Starwave logins). Feeding them into dest `MarginUsage` would be a **unit-of-account lie** (prop-challenge USD vs Pepperstone dest).

`QuantityNormalizer` (`src\Domain\Execution\QuantityNormalizer.cs`) is dest-grid only (`min` / `max` / `step`). It has **no** margin-room clip. Conversion tests skip the missing `IQuantityConverter` margin path.

Dashboard / settings cannot display or bind the dest cap: `/api/settings` RiskEngine block is a **different, unbound** schema (`MaxDailyDrawdownPct=5`, `MaxPositionSize=10`) that `Evaluate` never reads.

---

## 4. The only product caller zeros the input

`CopyTradingService` is the measured product `Evaluate(` caller (`GenerateShadowIntentsAsync`). It constructs a **private** `new RiskEngine()` (L23). The DI singleton at `DependencyInjection.cs` L45 is **not** the instance on this hop.

```csharp
CurrentGrossXau = 0,
CurrentNetXau = 0,
OpenPositions = 0,
MarginUsage = 0,          // L180 — dest book is invisible
```

Also zeroed on the same request: `DailyExecutionPnl`, `PortfolioDrawdown`. `TraderRealizedLoss` is `Min(0, trade.NetRealizedPnl)` of the **single source trade**, not dest equity.

Consequences:

1. `MAX_MARGIN_USAGE` is **unreachable** on the copy hop (`0 > 0.70` is false).
2. `MAX_XAU_GROSS` / `MAX_XAU_NET` / `MAX_OPEN_POSITIONS` are likewise unreachable (gross/net/open stamped 0).
3. Persist then **overwrites** `AllowFixSend = false` (L192) even if Evaluate would have set true.
4. Live send is still blocked by `NewOrderSingleImplemented = false` and `VenueReconciled = false` (const).

So: **the 70% number is both too high for gold and not applied.**

`RiskEngineTests` fixture uses `MarginUsage = 0.1m` (already inside a 10–20% band) and never asserts the branch.

---

## 5. Why 70% on gold is liquidation territory

Interpret `MarginUsage` as the usual **used_margin / equity** fraction (the only meaning A43 / A95 give it).

| Usage | Implied margin level `equity / used` | Free margin | Character on **XAUUSD** |
|---|---|---|---|
| `0.10`–`0.20` (this slot’s dest cap) | **500%–1000%** | 80–90% of equity | Room for a gold spike; copy book cannot eat the dest account |
| `0.50` | 200% | 50% | Margin-call adjacent on many books |
| **`0.70` (compile default)** | **≈143%** | **30% of equity** | **Liquidation territory** for gold |
| `1.00` | 100% | 0 | Instant margin call |

Buffer to a **100% margin-call** (equity = used margin), if used margin is sticky:

```text
buffer_equity_frac = 1 − usage
at 0.70 → 30% of dest equity
```

Gold is not a 30-pip EURUSD day:

- Contract: **100 oz / lot** is the usual retail notional. **$1 / oz ≈ $100 / lot**.
- Engine fixture / source tape prices sit near **$2400**. A **$5** print is **$500 / lot**; a **$20** spike (NFP / weekend gap / London open) is **$2000 / lot**.
- At **1:100**, 1 lot needs ≈ **$2400** initial margin; at **1:200**, ≈ **$1200**.
- Dest equity **$40,000** at **70% usage** ⇒ **$28,000** used ⇒ ≈ **11.7 lots @ 1:100** or **23 lots @ 1:200**.
- Free margin **$12,000**. Adverse gold move to **100% ML**:
  - 11.7 lots × $100/oz ≈ **$10.3**
  - 23 lots × $100/oz ≈ **$5.2**
- Those are **routine** XAUUSD prints, not tail events. A $20 spike overshoots the buffer and is **stop-out / broker flatten** on many books (Pepperstone / cTrader stop-out is commonly **50%** margin level; some books call at **100%**).

At a **0.15** dest cap on the same $40k:

```text
used ≤ 0.15 × 40_000 = $6,000
lots @ 1:100 ≈ 2.5    @ 1:200 ≈ 5.0
free ≈ $34,000
$ move to 100% ML ≈ $136 (@ 1:100) / $68 (@ 1:200)
```

That is the difference between **surviving a gold spike** and **being liquidated by the dest venue**. Lower usage **is** lower dest loss: less notional, more buffer, no stop-out cascade.

The engine’s **other** money caps (`MaxLossPerTrader=500`, `MaxDailyExecutionLoss=2000`, `MaxPortfolioDrawdown=3000`) are **absolute USD** on caller-supplied PnL/DD. They do **not** replace a dest-margin cap: a $2000 daily-loss line still lets the book sit at 70% usage until the next tick liquidates **more** than $2000.

---

## 6. The check is also the wrong *shape* for lower dest loss

Even if `MaxMarginUsage` were cut to `0.15` tomorrow, **L138–139 still would not size the dest book**.

| Need (A43 §4.6 / A23) | Engine today |
|---|---|
| `required_margin_per_oz = dest_side_price / leverage` | Missing (no price/leverage in this branch) |
| `usable_margin = available_margin × max_margin_usage_factor` | Missing (`available_margin` not on request) |
| `room_margin_oz = usable / required` | Missing |
| `binding_oz = min(allocated, room_gross, room_net, room_pos, room_margin)` | Missing. Gross/net/open also stamped 0 by the caller |
| If binding < allocated → `ReduceSize` + `MAX_MARGIN_USAGE` / `INSUFFICIENT_MARGIN` | Hard `Reject` / qty `0` |
| Re-quantize after clip | `QuantityNormalizer` never sees margin |
| Pre-trade already-over-cap vs post-trade projection | **Only pre-trade.** A dest sitting at **10%** can add size that lands at **90%** and still approve, because `0.10 > 0.70` is false |

A 10–20% dest cap that is not **post-trade** is a night-watchman who only objects after the house is already on fire. The current 70% line is that same watchman with the alarm set to “building already collapsed.”

---

## 7. Architecture / spec vs product

| Spec | Product |
|---|---|
| §39 hard limit: “max execution account margin usage” | Default **0.70**; increasing-only; caller decimal |
| A23 `MAX_MARGIN_USAGE` + `INSUFFICIENT_MARGIN` | Only the first string; second never emitted |
| A43 §4.6 `room_margin_oz` then re-quantize | **Missing.** `QuantityNormalizer` has no margin argument |
| A71 G28: reject/reduce on open/increase; do not block close | Action split matches; **no reduce** |
| A95 dashboard `maxMarginUsage: 0.70` | Spec **repeats the unsafe default**. Dest fields specified `null` if unknown — matches product honesty, not a live gate |
| B36 `RF-RA-MARGIN`: `MarginUsage=0.95` → `MAX_MARGIN_USAGE` | Fixture not implemented |
| `docs\risk.md` 5% daily / 10% total | **Different document.** Not `RiskLimits`. Not dest margin |

A95 documenting `0.70` is **not** evidence the number is safe on gold. It is the same compile default this slot rejects.

---

## 8. What “lower dest loss” requires (policy; not implemented here)

Do **not** implement in this slot. When a future hop is allowed to size dest gold:

1. **Cap dest margin usage at `0.10`–`0.20` of live dest equity.** Prefer **0.15** as the lab default. `0.70` must not ship as a gold dest limit.
2. **Source the fraction from dest equity / dest used / dest free** (cTrader TRADE / FIX collateral), never from `Mt5Account` source snapshots, never from `0`.
3. **Project post-trade** usage (`used + incremental_margin(qty, price, leverage, contract)`). Reject or clip when **projected** usage exceeds the cap (`INSUFFICIENT_MARGIN` / `MAX_MARGIN_USAGE` + `ReduceSize` per A43).
4. Bind `RiskLimits.MaxMarginUsage` from config. Stop using a compile `0.70`. Expose the same number on `/api/settings` — the current settings DTO is a **different, unbound** schema.
5. Pin unit facts: `0.21` open → reject; `0.20` exact policy; reduce/close still allowed; missing dest equity → fail-closed (`QUOTE_MISSING`-class), not `MarginUsage=0`.
6. Keep `AllowFixSend` / `35=D` off until §68 / §70 and this gate are on the hop. `SAFE_BY_ABSENCE` is not a dest-margin PASS.

---

## 9. Honesty / capital

| Claim | Status |
|---|---|
| “RiskEngine enforces 70% dest margin” | **False** on the product hop (`MarginUsage=0`). True only as a library compare of two caller decimals. |
| “70% is a safe gold dest cap” | **False.** ~143% margin level / 30% free on XAU is liquidation-adjacent. |
| “Lower dest loss needs 10–20% dest margin cap” | **True** as policy. **Not coded.** |
| “Wired to live dest equity” | **False.** Request has no dest money fields. Source `Mt5Account` equity is the wrong book. |
| “Would this lose dest money if `35=D` existed?” | **Yes**, if dest gold were sized without a live 10–20% post-trade cap. Today **no** `35=D`. |
| Product edited this slot | **No** |

Live copy stays off. This report is the only write.
