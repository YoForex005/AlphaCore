# P500_S030 — `InitialSl` / `SlUseRate` measurement

**Date:** 2026-08-18  
**Scope:** How `InitialSl` is set on reconstructed trades, how `SlUseRate` feeds risk/behavior, and what that implies for the **95.50** quality cluster.  
**Product source:** not edited. Measured from code only.

---

## 1. Verdict (one paragraph)

Production `InitialSl` is **never populated from ingested deals**. The reconstructor *would* copy `NormalizedDeal.StopLoss` onto the first open, but that field is left at its default `null` on every store-loaded deal. `Mt5DealDto` / `Mt5Deal` have **no SL column**; `ReadDeals` never calls `CIMTDeal.PriceSL()` even though the Manager API exposes it; demo seed deals also omit SL. `SlUseRate` is then `0` for every real/demo book. The scorer **always** adds **+10 risk** (`< 0.3`) and **−10 behavior** (`< 0.5`). For a clean profitable 3-trade tape (net > 0, PF ≥ 1.8, no martingale / averaging / lot-CV / DD>GP), quality is bit-identical **95.50**. That cluster is **not** “SL often present.” It is “SL always missing, penalty always applied, other flags clean.” Unit tests hide this by hardcoding `InitialSl = 2290`.

---

## 2. How `InitialSl` is set (reconstructor)

Source: `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs`.

### 2.1 First open only

`OpenTrade.Start` (first `DealEntry.In`, or leftover after `InOut` reverse) assigns:

```text
InitialSl = deal.StopLoss
InitialTp = deal.TakeProfit
FinalSl   = deal.StopLoss
FinalTp   = deal.TakeProfit
```

(`TradeReconstructor.cs` lines 234–237.)

### 2.2 Scale-in / close do not touch `InitialSl`

`ScaleIn` and `CloseOut` both call `ApplyCommon`. `ApplyCommon` updates **Final** SL/TP only:

```text
if (deal.StopLoss.HasValue)  FinalSl = deal.StopLoss
if (deal.TakeProfit.HasValue) FinalTp = deal.TakeProfit
```

(`TradeReconstructor.cs` lines 292–295.)

`InitialSl` is never rewritten after `Start`. A later IN/OUT with SL cannot backfill the initial value.

### 2.3 Persistence is a pass-through

`EfTradingStore.ReplaceReconstructedAsync` copies `t.InitialSl` onto `ReconstructedTrade.InitialSl` (`EfTradingStore.cs` line 201). Scoring does **not** re-read that column: `ReconstructionScoringService.RebuildTraderAsync` scores the in-memory `ReconstructedTradeResult` list.

### 2.4 `SL = 0` vs `null`

`NormalizedDeal.StopLoss` is `decimal?` (`NormalizedDeal.cs` line 21). Default is `null`.

| Input on first open | `InitialSl` | `InitialSl.GetValueOrDefault()` | Counts as SL? (`> 0`) |
|---|---|---|---|
| omitted / `null` | `null` | `0` | **no** |
| `0` | `0` | `0` | **no** (HasValue true; scorer still rejects) |
| `2290` | `2290` | `2290` | **yes** |

`0` is a HasValue write, not a missing-SL sentinel. The `> 0` test accidentally treats it as unused SL (see D11 M4). Production never reaches this case because StopLoss is never assigned.

---

## 3. Ingest path: SL is dropped before reconstruction

Four independent holes. Any one is fatal. All four are present.

### 3.1 `Mt5DealDto` has no SL/TP

`D:\Prop\src\Application\Contracts\Mt5Contracts.cs` lines 24–38:

```text
Mt5DealDto(DealTicket, Login, OrderTicket, PositionId, Symbol,
           Action, Entry, VolumeNative, Price, Profit,
           Commission, Swap, Time, Comment)
```

No `PriceSl`. Positions *do* have `PriceSl` (`Mt5PositionDto`). Reconstruction is deal-grouped, not position-joined.

### 3.2 Native connector drops `PriceSL()` on deals

`CIMTDeal` **does** expose it (`MT5APIDeal.h` lines 223–228: “order SL” `PriceSL()` / `PriceTP()`).

`NativeMt5BrokerConnector.ReadDeals` (`NativeMt5BrokerConnector.cs` 408–433) maps ticket, login, order, position, symbol, action, entry, volume, price, profit, commission, storage, time, comment. **Does not call `d.PriceSL()` / `d.PriceTP()`.**

`ReadPositions` **does** call `p.PriceSL()` / `p.PriceTP()` (lines 399–400). Open-position SL never flows into reconstructed trades.

C++ `mt5-sdk` likewise maps `price_sl` from **positions and orders**, not from the C# ingest deal DTO.

### 3.3 Persistence has no deal SL column

`Mt5Deal` (`D:\Prop\src\Domain\Entities\Mt5Deal.cs`) has no `PriceSl` / `StopLoss`. `TraderDbContext` maps `mt5_deals` without extra SL columns. Both `UpsertDealAsync` and `UpsertDealsBatchAsync` copy only the DTO fields.

### 3.4 `LoadDealsAsync` never sets `NormalizedDeal.StopLoss`

`EfTradingStore.LoadDealsAsync` (`EfTradingStore.cs` 152–169) constructs `NormalizedDeal` without `StopLoss` / `TakeProfit` / `Reason`. Those properties stay `null`.

`RebuildTraderAsync` then:

```text
deals  = LoadDealsAsync(...)          // StopLoss always null
trades = Reconstruct(..., deals)      // InitialSl = null
score  = _scorer.Score(completedXau)  // SlUseRate = 0
```

### 3.5 Demo seed is the same hole

`FakeMt5BrokerConnector.ClosedRoundTrip` builds `Mt5DealDto` open/close pairs with no SL (`FakeMt5BrokerConnector.cs` 148–149). Logins **10001**, **10002**, **99001** reconstruct with `InitialSl = null`.

---

## 4. How the scorer uses it

Source: `D:\Prop\src\Domain\Scoring\BaselineScorer.cs`.

### 4.1 Feature

```text
SlUseRate = count(InitialSl.GetValueOrDefault() > 0) / N
```

N = completed XAUUSD trades after `Completed && IsXauUsd`. Empty book: `SlUseRate = 0` (explicit zero snapshot, line 60).

`FinalSl` is unused. `DealReason.StopLoss` (close-reason enum 3) is unused — and `Reason` is also not loaded.

### 4.2 Thresholds (the assigned question)

| Condition | Axis | Delta |
|---|---|---|
| `SlUseRate < 0.3` | risk | **+10** (line 139) |
| `SlUseRate < 0.5` | behavior | **−10** (line 148) |

There is **no** intermediate band that scores “some SL.” A book with 0–29% SL use takes **both** hits. 30–49% takes the behavior hit only. ≥ 50% takes neither.

On production/demo data `SlUseRate` is **exactly 0**, so both fire. There is no per-trader variance on this feature.

### 4.3 Quality algebra that produces 95.50

```text
quality = 50
        + 15  if NetPnl > 0
        + 10  if PF >= 1.2
        +  5  if PF >= 1.8
        + behavior * 0.2
        − risk    * 0.25
```

Clean profitable tape **with SL always missing**:

| | With SL (`rate=1`) | No SL (`rate=0`, prod/demo) |
|---|---|---|
| risk | 0 | **10** |
| behavior | 100 | **90** |
| quality | 50+15+10+5+20−0 = **100** | 50+15+10+5+18−2.5 = **95.50** |

`decimal.Round(..., 2)` makes that **95.50** bit-identical.

Demo books that hit this cell:

| Login | Tape | Flags | Scores |
|---|---|---|---|
| ACHIEVER 10001 | mixed +151.4 / −89.4 / +161.6, PF≈3.50, lots 0.10 | no martingale / avg / lot-CV / DD>GP | risk 10, behavior 90, quality **95.50**, SHADOW |
| STARWAVEFX 99001 | three small wins, PF stub 99, lots 0.05 | same | same **95.50** / SHADOW |

Login 10002 is martingale (0.10 → 0.20 → 0.40 after losses) so it does **not** sit in the 95.50 cluster: risk/behavior take the much larger martingale/lot-escalation hits. Login 10003 has N=0: `SlUseRate=0` still yields risk 10 / behavior 90, but quality is capped at 40 (`CompletedXauTrades < 3`) and state is `INSUFFICIENT_DATA`.

---

## 5. Clustering interpretation

The prompt: *if SL is rarely populated from deals, almost everyone gets the penalty or nobody does — 95.5 suggests SL often present **or** often ignored consistently.*

Measured answer:

| Hypothesis | Compatible with 95.50 cluster? | Evidence |
|---|---|---|
| SL often present on reconstructed trades | **No** | Present SL would *remove* the 10/10 hits → quality **100**, not 95.50 |
| SL inconsistently present (mixed 0–1 rates) | **No** | Would spread quality (95.50 vs 97.50 vs 100 depending on whether only risk, only behavior, or both fire) |
| SL **never** present; penalty **always** applied; other flags clean | **Yes** | Ingest holes §3; demo 10001/99001; D37 / E031 measured 95.50 |

So the cluster is **consistent ignorance** (input hole), not consistent presence. The scorer is working as written. The feature is dead on the live path.

Unit tests (`BaselineScorerTests.Closed`) always set `InitialSl = 2290`. That path has `SlUseRate = 1`, never exercises `< 0.3` / `< 0.5`, and would cluster at **100** for three disciplined winners. Tests and production are **not** measuring the same SL feature.

---

## 6. What would populate `InitialSl` (not implemented)

If someone later wires SL, these are the only code sites that matter:

1. Add `PriceSl`/`PriceTp` to `Mt5DealDto` + `Mt5Deal` + EF map.
2. `ReadDeals`: `(decimal)d.PriceSL()` — Manager API already has it (`IMTDeal::PriceSL`, comment “order SL”). Broker may still send `0` when the order had no SL.
3. `LoadDealsAsync`: `StopLoss = d.PriceSl == 0 ? null : d.PriceSl` (or keep 0 and rely on `> 0`).
4. Reconstructor already copies first-open `deal.StopLoss` → `InitialSl`.

Until (1)–(3) exist, `InitialSl` on reconstructed trades is **always null** for every login that went through `ITradingStore`.

---

## 7. File map

| Path | Role |
|---|---|
| `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` | `InitialSl = deal.StopLoss` on first open only |
| `D:\Prop\src\Domain\Reconstruction\NormalizedDeal.cs` | `decimal? StopLoss` default null |
| `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` | scored `InitialSl` |
| `D:\Prop\src\Domain\Scoring\BaselineScorer.cs` | rate + 0.3/0.5 penalties + 95.50 algebra |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | deal DTO has no SL |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `ReadDeals` drops `PriceSL()` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | demo deals have no SL |
| `D:\Prop\src\Domain\Entities\Mt5Deal.cs` | no SL column |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | upsert + load omit StopLoss |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `RebuildTraderAsync` scores store-loaded deals |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Bases\MT5APIDeal.h` | `PriceSL()` exists on IMTDeal |
| `D:\Prop\tests\Unit\BaselineScorerTests.cs` | hardcodes `InitialSl = 2290` (masks hole) |
| `D:\Prop\tests\Unit\TradeReconstructionTests.cs` | helper omits `StopLoss` |

---

## 8. Honest metrics

| Claim | Status |
|---|---|
| Reconstructor *can* persist InitialSl if `NormalizedDeal.StopLoss` is set | **true** (Start + ToResult) |
| Production/demo `LoadDealsAsync` supplies StopLoss | **false** |
| Native `CIMTDeal.PriceSL` is mapped | **false** (API exists, C# ignores) |
| `SlUseRate < 0.3` → +10 risk | **true** (always on prod/demo) |
| `SlUseRate < 0.5` → −10 behavior | **true** (always on prod/demo) |
| 95.50 cluster = SL often present | **false** |
| 95.50 cluster = SL always absent + clean profitable tape | **true** (10001, 99001) |

No product files were modified.
