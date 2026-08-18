# P500_S001 — Scorer can SHADOW a negative dashboard PnL

| Field | Value |
|---|---|
| Slot | S001 |
| Date | 2026-08-18 |
| Evidence | `src/Domain/Scoring/BaselineScorer.cs`, `src/Infrastructure/Dashboard/EfDashboardQueries.cs`, live `GET /api/traders` |
| Product source modified | No |

## Verdict

**SHADOW is not a profit filter.** Quality uses **completed XAU only**. Dashboard `netSourcePnl` sums **all completed reconstructed trades**. Live: ACHIEVER `302252` SHADOW `earlyScore=95.50` `netSourcePnl=-68.46`; `303174` SHADOW `95.50` / `-29.38`.

## Formula (quoted)

```
quality = 50
if (features.NetPnl > 0) quality += 15;
if (features.ProfitFactor >= 1.2m) quality += 10;
if (features.ProfitFactor >= 1.8m) quality += 5;
quality += behavior * 0.2m;
quality -= risk * 0.25m;
SHADOW if quality >= 70 && risk < 40 && trades >= 3
```

`EfDashboardQueries.GetTradersAsync` PnL:

```
ReconstructedTrades.Where(t => t.Completed).GroupBy(broker, login).Sum(NetRealizedPnl)
```

No XAU filter on that sum.

## Profit implication

A name can be SHADOW because **XAU** was slightly positive / high PF / no martingale flag, while **EUR/BTC** (or later XAU) made the dashboard red. Copying SHADOW blindly is not +EV.
