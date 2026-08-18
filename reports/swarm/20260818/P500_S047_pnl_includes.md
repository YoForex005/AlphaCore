# P500_S047 — `ReconstructedTradeResult.NetRealizedPnl` includes source commission + swap (not Pepperstone dest)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S047_pnl_includes.md` |
| Agent | P500_S047 (read-only reconstruction money) |
| Date | 2026-08-18 |
| Product source edited | **No** |
| Secrets printed | **None** |
| SUT | `D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs` (`OpenTrade.ToResult`) |
| Type | `D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs` |
| Ingest | `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (`ReadDeals`) |
| Persist | `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` (`ReplaceReconstructedAsync`) |
| Spec | `D:\Prop\reports\swarm\20260818\A21_reconstruction_spec.md` §4.1 / §7.8 |

**Verdict:** **Yes — source `Commission` and `Swap` are in `NetRealizedPnl`.** Formula is `GrossRealizedPnl + Commission + Swap + Fees`. Those three money fields are **source-broker deal fields** (`IMTDeal::Profit` / `Commission` / `Storage`). They are **not** Pepperstone / cTrader destination costs. Using source demo net as dest profit **overstates** what a copy would realize on the venue.

---

## 0. One-line answer

`NetRealizedPnl` **includes** commission and swap **from the source MT5 deal tape**. It does **not** include destination Pepperstone commission, swap, spread, or slippage. Destination cost schedule ≠ Achiever demo. Source net is the wrong series for copy expectancy.

---

## 1. Formula (compiled, not inferred)

```308:332:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        public ReconstructedTradeResult ToResult(bool completed)
        {
            var fees = 0m;
            return new ReconstructedTradeResult
            {
                // ...
                GrossRealizedPnl = GrossRealizedPnl,
                Commission = Commission,
                Swap = Swap,
                Fees = fees,
                NetRealizedPnl = GrossRealizedPnl + Commission + Swap + fees,
```

Identity:

```
NetRealizedPnl = GrossRealizedPnl + Commission + Swap + Fees
Fees           = 0m   // hardcoded every emit
```

This matches A21 §4.1 / §7.8:

```
gross_realized_pnl    = Σ deal.profit
commission            = Σ deal.commission
swap                  = Σ deal.storage
fees                  = Σ deal.fee
net_realized_pnl      = gross + commission + swap + fees
```

Implementation gap vs spec: **`NormalizedDeal` has no `Fee` field.** `ToResult` pins `fees = 0m`. `IMTDeal::Fee()` is never ingested (`Mt5DealDto` / `Mt5Deal` have Profit, Commission, Swap only).

---

## 2. Are commission and swap included?

| Component | In `NetRealizedPnl`? | Source | Sign (typical) |
|---|---|---|---|
| `GrossRealizedPnl` | **Yes** | Σ `NormalizedDeal.Profit` ← `IMTDeal::Profit()` | +/− raw price PnL |
| `Commission` | **Yes** | Σ `NormalizedDeal.Commission` ← `IMTDeal::Commission()` | usually **≤ 0** (A21) |
| `Swap` | **Yes** | Σ `NormalizedDeal.Swap` ← `IMTDeal::Storage()` | overnight +/− |
| `Fees` | **Slot only** | hardcoded `0m` | never source `Fee()` |
| Dest Pepperstone commission | **No** | not modeled on this type | — |
| Dest Pepperstone swap | **No** | not modeled on this type | — |
| Dest spread / taker slip | **No** | `ShadowCopyEngine` only (unused by recon) | — |
| Standalone `DealAction.Commission` (7/8/9) rows | **No** | `IsTradingDeal` is Buy/Sell only | A21 F14 |

So: **yes, commission and swap are included**, when they ride on **Buy/Sell trading deals**. They are **source** money, added algebraically (not absolute-valued). Broker-provided; recon does **not** recompute from VWAP × contract.

### 2.1 How they accumulate

Every applied IN / scale-in / OUT / reverse-close adds the deal’s money:

```285:296:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
        private void ApplyCommon(NormalizedDeal deal)
        {
            AddDealMeta(deal);
            LastEventAt = deal.Time;
            Commission += deal.Commission;
            Swap += deal.Swap;
            GrossRealizedPnl += deal.Profit;
```

`OpenTrade.Start` does the same on the opening deal (`Commission += deal.Commission`, `Swap += deal.Swap`, `GrossRealizedPnl += deal.Profit`).

Result fields on the record:

```23:27:D:\Prop\src\Domain\Reconstruction\ReconstructedTradeResult.cs
    public required decimal GrossRealizedPnl { get; init; }
    public required decimal Commission { get; init; }
    public required decimal Swap { get; init; }
    public required decimal Fees { get; init; }
    public required decimal NetRealizedPnl { get; init; }
```

Persisted 1:1 onto `ReconstructedTrade` (`EfTradingStore.ReplaceReconstructedAsync`). Dashboard `netSourcePnl` = Σ completed `NetRealizedPnl` (any symbol). Scorer sums the same field on completed XAU only.

### 2.2 Worked number (spec fixture, not live)

B34 F01 / F02:

| Gross | Commission | Swap | Fees | Net |
|---:|---:|---:|---:|---:|
| `100.00` | `-1.20` | `-0.20` | `0` | **`98.80`** |

`100 + (−1.20) + (−0.20) + 0 = 98.80`. If an operator reads only `Gross` they overstate source net by **$1.40** on that toy fill. Live Achiever demo often has **0** commission on the deal (see unit helper below) — then net = gross.

### 2.3 Unit tests hide costs

`TradeReconstructionTests.Deal(...)` hardcodes `Commission = 0`, `Swap = 0`. The only compiled assertion is `NetRealizedPnl.Should().Be(100m)` on a `Profit=100` OUT. **No unit fact locks the `+ Commission + Swap` identity.** A regression that dropped commission from net would stay green.

---

## 3. Ingest path (source tape only)

```416:430:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            rows.Add(new Mt5DealDto(
                (long)d.Deal(),
                (long)d.Login(),
                (long)d.Order(),
                (long)d.PositionID(),
                d.Symbol(),
                (DealAction)d.Action(),
                (DealEntry)d.Entry(),
                d.Volume(),
                (decimal)d.Price(),
                (decimal)d.Profit(),
                (decimal)d.Commission(),
                (decimal)d.Storage(),
                DateTimeOffset.FromUnixTimeSeconds(d.Time()),
                d.Comment()));
```

`Mt5DealDto` / `Mt5Deal` / `NormalizedDeal` all carry `Profit`, `Commission`, `Swap`. There is **no destination-account column** on these types. `IMTDeal::Fee()` is unused.

MT5 convention (and A21): `Profit` is **not** already net of commission/storage. Adding them in `ToResult` is correct for **source** net. It is **not** a dest-cost haircut.

Non-trade commission rows (`DealAction.Commission = 7`, daily/monthly) are **dropped** (`NormalizedDeal.IsTradingDeal` requires Buy/Sell). A21 F14: those dollars never fold into the reconstructed trade.

---

## 4. Destination Pepperstone costs will differ

Pepperstone / cServer is the **execution venue**, not the source ledger (A87). A24 §10.4 is explicit:

> Source-broker commission/swap from reconstruction are **not** copied into `shadow_pnl`. They remain on `reconstructed_trades` for source skill features.

Shadow spec dest money:

| Dest field | Meaning |
|---|---|
| `shadow_copy_fill.commission` | Destination cost model |
| `shadow_pnl.realized` | Fills **+ dest commission + dest swap** |
| `shadow_pnl.net` | dest realized + unrealized after dest costs |

`ShadowCopyEngine` today models **quote-touch + optional 0.05 pt latency slip**. It does **not** write dest commission or dest swap. Reconstruction never calls it. Copy risk uses **source** `trade.NetRealizedPnl` as `TraderRealizedLoss` (`CopyTradingService`).

What will differ on Pepperstone vs Achiever demo:

| Cost | Source `NetRealizedPnl` | Dest live / honest shadow |
|---|---|---|
| Commission schedule | Achiever (often cheap / $0 on challenge demo) | Pepperstone cTrader schedule |
| Swap / storage | Source group swap | Dest contract swap (different rollover) |
| Spread | Embedded in source `Profit` at **source** bid/ask | Dest bid/ask (usually wider on live XAU) |
| Slippage / delay | None (fill is the deal) | Taker + modeled delay (A24) |
| `Fee()` | Forced 0 | Dest fee model if any |
| Contract / qty | Source lots via `VolumeConverter` | §38 dest `OrderQty` (not 1:1 lots) |
| Challenge markup | Demo target tape | None — live money |

G15 (`destination costs / slippage measured`) is still **FAIL** in A100 / C14. There is no measured dest cost table to subtract from source net.

---

## 5. Source demo PnL overstates dest profit

This is not a style warning. It is the money identity:

1. **`NetRealizedPnl` is source-broker economics.** Demo `demo\yo-2step` / `demo\yo-payp` (P500_S004: SHADOW is 100% challenge demo). Those books exist to hit a profit target, often with **softer costs** than live Pepperstone XAU.
2. **Even when source commission/swap are non-zero, they are the wrong numbers.** Adding Achiever `−$1.20` commission does not estimate Pepperstone commission on the copied size.
3. **`Fees = 0` always.** Any dest fee is omitted.
4. **Gross is source fill PnL.** Dest fill is dest bid/ask after delay, not source VWAP. Spread + slip on XAU typically **cuts winners and enlarges losers**.
5. **Dashboard and score treat source net as the money.** `EfDashboardQueries` Σ `NetRealizedPnl` → `NetSourcePnl` (labeled “Net P&L”). `BaselineScorer` +15 iff XAU net > 0. `DestinationRealPnl` is a painted **0** (P500_CODE_8). A +$100 challenge close looks like +$100 edge. It is not dest edge.
6. **Copy of a +source trade can be dest-negative** after dest commission + dest swap + dest spread, even if source net already subtracted source costs.

Inequality (honest, not measured here):

```
E[Pepperstone net | copy]  <  source NetRealizedPnl
```

for this population (demo challenge XAU, dest live cTrader). Treating source net as dest profit **overstates** dest profit. Direction of bias: **too optimistic**. Magnitude: **unmeasured** until G15 / shadow dest cost model runs.

Do **not** use `ReconstructedTradeResult.NetRealizedPnl` as `future_net_copy_pnl` (A52). Wrong `y`.

---

## 6. Caveats (still not dest)

| Issue | Effect on source net | Dest? |
|---|---|---|
| Reverse `InOut`: `CloseOut` + leftover `Start` both add the **same** deal’s Profit/Commission/Swap | Can **double-count** money on A and leftover (B11 / C01) | still source |
| Open (incomplete) row: net = IN commission/swap/profit only | Open book can show small negative net | still source |
| Canceled position: rows exist; first-3 eligibility flipped off | Money still persisted | still source |
| Non-XAU completed rows in dashboard sum | Mixes symbols into “Net P&L” | still source |

---

## 7. Operator rule

| Question | Answer |
|---|---|
| Does `NetRealizedPnl` include commission? | **Yes — source deal commission.** |
| Does it include swap? | **Yes — source `Storage`.** |
| Does it include dest Pepperstone costs? | **No.** |
| Can I treat source demo net as dest profit? | **No. It overstates dest.** |
| What would dest net require? | Shadow/live fills on dest quotes + dest commission/swap model (A24 §10.4). Not this field. |

Product not edited.

*End of P500_S047.*
