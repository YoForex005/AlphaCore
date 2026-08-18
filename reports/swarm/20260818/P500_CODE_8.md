# P500_CODE_8 — Demo challenge +$100 is not dest edge after venue costs

| Field | Value |
|---|---|
| Slot | **8** |
| File | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| Angle | Demo challenge PnL of **+$100** is **not** an edge after venue costs |
| Verdict | **FAIL — NOT EDGE.** File was read in full (217 lines). Overview paints `DestinationRealPnl = 0`. Leaderboard `NetSourcePnl` is source reconstructed challenge/demo dollars. No dest spread, dest commission, dest swap, dest fees, or contract-multiplier conversion is applied. A painted +$100 on Achiever challenge books cannot be treated as copyable expectancy. |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Method | Full `read_file` of `EfDashboardQueries.cs`. Grep + reads of `DashboardModels.cs`, `TradeReconstructor.ToResult`, `BaselineScorer`, `ShadowCopyEngine`, `OverviewPage.tsx`, `TradersPage.tsx`, `DependencyInjection.cs`, `CTraderFixLogonHostedService` (flag only). No passwords or FIX secrets printed. No `NewOrderSingle` built or sent. |

**Honesty:** This is **not** an empty PASS. The class is a read model. It does not send 35=D. It **does** present source challenge PnL as the only money number operators will rank. That number is not dest-book edge.

Measured live context (given, not re-probed here): **8463 accounts**; **Achiever scoring**; **Starwave deals-done scored 0**; **SHADOW all demo**; **`destinationRealPnl` 0**; **FIX LoggedOn**; **`REAL_COPY` false**.

---

## File

`D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` — sealed `IDashboardQueries` implementer. Injects `TraderDbContext` + `LiveRuntimeStatus`. Seven query methods. Money tiles that matter to this angle:

| DTO field | How this file fills it | Venue-cost adjusted? |
|---|---|---|
| `OverviewDto.ShadowPnl` | `SUM(ShadowOrders.SourceVsShadowSlippage)` | **No.** Raw price-diff sum, not $ after lot × contract × dest commission |
| `OverviewDto.DestinationRealPnl` | literal `0` | **N/A — dest book never queried** |
| `OverviewDto.XauGross` / `XauNet` | literal `0` | **No dest exposure** |
| `OverviewDto.RealCopyEnabled` | `_runtime.RealCopyEnabled` (DI + FIX host pin **false**) | Send still off |
| `TraderRowDto.NetSourcePnl` | `SUM(ReconstructedTrades.NetRealizedPnl)` where `Completed` | **Source broker only.** No dest costs |
| `TraderRowDto.ShadowPnl` | literal `0` | **Cannot see who would lose after venue** |
| `TradeHighlightDto.NetRealizedPnl` | source row as stored | Same source dollars |
| `RiskDashboardDto.DailyPnl` / `Drawdown` / XAU book | five literals `0` | Dest loss invisible |
| `FixSessionDto.ExecutionEnabled` | literal `false` | Honest: LoggedOn ≠ armed |

Port contract (`DashboardModels.cs`): `TraderRowDto.NetSourcePnl` is named **source**. `OverviewDto.DestinationRealPnl` exists as a **separate** field. This file never joins them.

---

## Angle

A demo / 2-step challenge account showing **+$100** reconstructed net is **not** a tradable edge on the destination venue (cTrader / Pepperstone book) after:

- dest bid/ask (buy ask, sell bid)
- dest commission + swap
- copy latency adverse (shadow model uses `0.05` after 250 ms)
- challenge-broker fill quality that does not exist on the live book

Question this slot answers: **does `EfDashboardQueries` prove +$100 source PnL survives those costs?**

Answer: **No. It cannot. It does not try.**

---

## Verdict

**FAIL — +$100 demo challenge PnL is not an edge after venue costs.**

Reasons, all from the file that was actually read:

1. **Dest money is a painted zero.** `GetOverviewAsync` writes `DestinationRealPnl`, `XauGross`, `XauNet` as `0, 0, 0`. That matches the live measurement `destinationRealPnl 0`. It is not a computed “after costs = 0”; it is **no dest ledger**.
2. **The only ranking money is source challenge dollars.** `GetTradersAsync` groups completed `ReconstructedTrades` and sums `NetRealizedPnl` into `NetSourcePnl`. Web `TradersPage` labels that column **“Net P&L”**. Sort is `OrderByDescending(t => t.EarlyScore)`. Early score itself adds **+15** when source `NetPnl > 0` (`BaselineScorer`). A +$100 challenge book wins the sort without a dest-cost haircut.
3. **Source net is demo-broker economics.** Reconstruction sets `Fees = 0` and `NetRealizedPnl = Gross + Commission + Swap + fees`. Those commission/swap fields are **source deal** fields, not cTrader venue fees.
4. **Per-trader dest / shadow $ is hardcoded 0.** Even if shadow rows exist, the leaderboard cannot show venue-adjusted PnL. Overview “Shadow P&L” is a **sum of slippage scalars**, not mark-to-market dollars.
5. **Live book does not corroborate.** 8463 accounts are being counted. Achiever (challenge/demo groups in catalog) is the scoring plane. Starwave **deals-done scored 0**. SHADOW is **all demo**. FIX may be LoggedOn; `REAL_COPY` is **false**. There is **zero** dest realized to validate +$100.
6. **Risk page cannot veto on dest bleed.** `GetRiskAsync` returns `DailyPnl=0, Drawdown=0, XauLong=0, XauShort=0, XauNet=0, RealCopyEnabled=false`. A +$100 source winner copied later would not show dest drawdown on this dashboard.

This file does **not** send `NewOrderSingle`. Capital is not at risk from the query class itself. The **operator risk** is treating the painted +$100 as permission to copy.

---

## Evidence quotes

Destination real PnL, dest XAU book, and real-copy flag — dest money is literals; copy flag is runtime (pinned false elsewhere):

```33:52:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            _runtime.Brokers.Values.Count(b => b.Connected) > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution
                || _runtime.Quote.LoggedOn,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution
                || _runtime.Trade.LoggedOn,
            _runtime.RealCopyEnabled);
```

Constructor slots for those three zeros (`DashboardModels.cs`):

```15:22:D:\Prop\src\Application\Dashboard\DashboardModels.cs
    decimal ShadowPnl,
    decimal DestinationRealPnl,
    decimal XauGross,
    decimal XauNet,
    bool Mt5Healthy,
    bool QuoteHealthy,
    bool TradeHealthy,
    bool RealCopyEnabled);
```

Overview “Shadow P&L” is **not** dest dollars. It sums stored **price slippage**, unscaled:

```29:29:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
```

Leaderboard money = source reconstructed net. No dest cost column. Per-trader `ShadowPnl` forced to `0`. Ranked by early score, not dest expectancy:

```90:128:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        var pnlMap = pnls.ToDictionary(x => (x.BrokerId, x.Login), x => x.Pnl);
        // ...
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,
                s?.EarlyQualityScore ?? 0,
                null,
                s?.RiskScore ?? 0,
                s?.Martingale ?? false,
                s?.AveragingDown ?? false,
                s?.LotEscalation ?? false,
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
                0,
                s?.LastScoredAt ?? account.LastSyncedAt));
        // ...
        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
```

Source net used for that column (fees never dest; always `0` in reconstructor):

```309:332:D:\Prop\src\Domain\Reconstruction\TradeReconstructor.cs
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

Scorer treats **any** source net > 0 as quality. +$100 challenge books get the same +15 as a dest-proven book:

```152:154:D:\Prop\src\Domain\Scoring\BaselineScorer.cs
        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
```

UI will show +100.00 as “Net P&L” and dest real as 0.00:

```26:27:D:\Prop\apps\web\src\pages\OverviewPage.tsx
        <MetricCard label="Shadow P&L" value={Number(data.shadowPnl).toFixed(2)} />
        <MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
```

```31:35:D:\Prop\apps\web\src\pages\TradersPage.tsx
              <td>{Number(t.netSourcePnl).toFixed(2)}</td>
              <td>{Number(t.earlyScore).toFixed(1)}</td>
              <td>{Number(t.riskScore).toFixed(1)}</td>
```

Risk / dest exposure cannot contradict a +$100 source story:

```208:208:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
```

FIX LoggedOn is allowed to paint healthy while execution stays off (session DTO last arg `false`):

```178:195:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        return sessions.Select(s => new FixSessionDto(
            s.Qualifier.ToString().ToUpperInvariant(),
            s.Host,
            s.Port,
            s.Status != FixSessionStatus.Disconnected && s.Status != FixSessionStatus.Error,
            s.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution or FixSessionStatus.Reconciling,
            s.Status.ToString(),
            // inbound/outbound seq and quote snapshot omitted in this quote
            quote is null ? null : (DateTimeOffset.UtcNow - quote.ReceivedAt).TotalSeconds,
            false)).ToList();
```

DI pin (not this file, but explains live `REAL_COPY false`):

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

Shadow engine **models** dest spread and 0.05 latency slip — this dashboard **does not convert that into a $ haircut** on the +$100:

```33:33:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public const decimal DefaultLatencySlippagePoints = 0.05m;
```

```50:59:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
        var slippage = direction == TradeDirection.Long ? raw - sourcePrice : sourcePrice - raw;
        return new ShadowFill
        {
            ShadowOrderId = shadowOrderId,
            Price = raw,
            Quantity = quantity,
            FilledAt = now,
            Spread = quote.Ask - quote.Bid,
            QuoteAge = now - quote.ReceivedAt,
            SourceVsShadowSlippage = slippage
        };
```

Challenge-shaped source books (catalog, not a $100 fixture, but the scoring plane): Achiever groups are `demo\*` / `contest\*`. Starwave seed has a `real\standard` stub; live says Starwave scored **0** deals.

```84:86:D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs
                new Mt5GroupDto(@"demo\Maxmaster", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"contest\yo-2step", "USD", 2, "Achiever", 100, 50, true)
```

---

## Profit implication

- **There is no proven dest profit in this number.** Live `destinationRealPnl` is **0** because this file **always** returns 0. FIX LoggedOn + 8463 accounts do not mint dest dollars.
- **+$100 source is a ranking signal, not an edge.** Across 8463 Achiever-scored logins, many challenge books will print small positive nets (payout / 2-step pass theater). `OrderByDescending(EarlyScore)` plus `NetPnl > 0 → +15` will surface those names. Copying the top of that list would buy **demo fill quality**, not dest expectancy.
- **Venue costs eat a $100 challenge print quickly.** Reconstruction never subtracts dest commission. Shadow slippage is summed as **price points**, so a 0.30 dest spread on 0.10 lot XAU (≈ 10 oz on standard contract) is **~$3/side** and is **not** deducted from the +$100 tile. Ten copied round-trips at 0.25–0.50 dest spread plus dest commission can consume **$30–$100+** of that print before any edge. News-wide gold spreads consume it in one clip.
- **Starwave scored 0** means the one non-challenge broker plane has **not** confirmed the same traders make money on a real book. Achiever-only +$100 is **selection on the cheap tape**.
- **SHADOW all demo** means even the shadow path is not a live dest mark. Overview Shadow P&L, if non-zero, is still the wrong unit (slippage sum). Per-trader Shadow P&L is **0**, so you cannot pick the subset whose dest-adjusted $ stayed positive.
- **Profit action:** do **not** size, copy, or advertise +$100 challenge winners. Keep `REAL_COPY` false. Treat dest real PnL **0** as “no harvest,” not “flat and safe to turn on.”

---

## Lower-loss implication

- **Not copying +$100 challenge winners is the loss reduction.** The cheapest XAU error is paying dest spread/commission to clone a demo pass. With 8463 accounts the false-positive rate on “net > 0” is the default, not the exception.
- **This file currently lowers loss by omission:** `DestinationRealPnl` 0, trader `ShadowPnl` 0, risk book 0, `ExecutionEnabled` false, `RealCopyEnabled` from a false pin. Those zeros are **not** a dest risk engine; they are **no dest orders**. Leave them that way until a dest-cost-adjusted shadow $ series is positive **after** spread + commission + latency on the **same** tickets.
- **Hidden-loss trap if copy is armed later:** risk `DailyPnl`/`Drawdown` stay 0 in this class even if dest starts losing. Operators would still see source +$100 and dest real 0 (unless someone wires dest). That combination looks like “source works, dest flat” while the venue book bleeds. **Do not arm send until `GetRiskAsync` and `DestinationRealPnl` read dest fills, not literals.**
- **Starwave scored 0 + SHADOW all demo** is a **gate**, not a gap to fill with Achiever challenge PnL. Loss-down path: require dest-adjusted shadow PnL **< 0 filter** (haircut +$100 by modeled dest costs) **before** SHADOW even ranks a name; never promote on source net alone.
- **Concrete haircut rule this dashboard does not implement (and must, before any copy):**  
  `edge? = source_net − Σ(dest_spread × lots × oz_per_lot) − dest_commission − dest_swap − latency_slip_$`  
  If that residual is ≤ 0, the +$100 is **not** edge. Today the residual is **undefined** and dest realized is **0**.

---

## What this file is not

- Not a FIX sender. No `35=D`, no `NewOrderSingle`.
- Not a dest PnL ledger.
- Not a venue-cost engine.
- Not proof that 8463 accounts contain copyable XAU edge.

---

## Slot close

| Item | Result |
|---|---|
| File read | **Yes.** 217 lines. Empty PASS refused. |
| Angle held | **Yes.** +$100 demo challenge PnL ≠ dest edge after venue costs. |
| Verdict | **FAIL — NOT EDGE** |
| Live dest $ | **0** (literal + measured) |
| Live copy | **false** |
| Capital from this class | **None** (queries only) |
| Operator capital if they believe the +$100 | **Would be lost to dest costs; do not copy** |
