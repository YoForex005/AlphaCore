# W500_SLICE_66

- **slot:** 66
- **file:** `D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (91 lines) via `read_file`; grep on workspace for `score` / `dashboard` / `GetTradersAsync` / `TraderScores`; cross-read `EfDashboardQueries.GetTradersAsync` to locate the actual hide
- **verdict:** PASS

## Evidence quotes

`ShadowCopyEngine.cs` is a destination-quote taker-touch calculator. The file defines `ShadowFill`, `ShadowPosition`, and three methods (`SimulateEntry`, `SimulateExit`, `MarkToMarket`). It has no dashboard query, no `Mt5Account` enumeration, no `TraderScore` join, and no visibility filter.

The class surface is only fill simulation:

```31:61:D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs
public sealed class ShadowCopyEngine
{
    public const decimal DefaultLatencySlippagePoints = 0.05m;

    public ShadowFill SimulateEntry(
        string shadowOrderId,
        TradeDirection direction,
        decimal quantity,
        decimal sourcePrice,
        DestinationQuote quote,
        DateTimeOffset now,
        TimeSpan modeledDelay)
    {
        var useAsk = direction == TradeDirection.Long;
        var raw = useAsk ? quote.Ask : quote.Bid;
        var adverse = direction == TradeDirection.Long ? DefaultLatencySlippagePoints : -DefaultLatencySlippagePoints;
        if (modeledDelay > TimeSpan.FromMilliseconds(250))
            raw += adverse;

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
    }
```

Exit and MTM are the same kind of arithmetic (bid for long close / short mark, ask for short close / long mark). No score field is read or written:

```63:90:D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs
    public ShadowFill SimulateExit(
        string shadowOrderId,
        TradeDirection openDirection,
        decimal quantity,
        decimal sourceExitPrice,
        DestinationQuote quote,
        DateTimeOffset now)
    {
        var raw = openDirection == TradeDirection.Long ? quote.Bid : quote.Ask;
        var slippage = openDirection == TradeDirection.Long ? sourceExitPrice - raw : raw - sourceExitPrice;
        return new ShadowFill { /* Price = raw; no score, no account list */ };
    }

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
```

This file does not contain:

- `dashboard`, `GetTradersAsync`, `TraderRowDto`, `IDashboardQueries`
- `TraderScore`, `EarlyScore`, `LastScored`, `CompletedXauTrades`
- `Mt5Accounts` / account listing / hide / filter / `Where` on score presence
- any HTTP, EF, or UI type

`ShadowPosition` carries `BrokerId` + `SourceLogin` as fill identity only (`ShadowPosition` L17–29). That is not a dashboard census and cannot omit an unscored login from `/traders`.

The angle (dashboard hides accounts that have no score yet) is real, but it lives in `EfDashboardQueries.GetTradersAsync`, which drives the trader list from `TraderScores` and only uses `Mt5Accounts` for `GroupName`:

```74:116:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        // ...
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        foreach (var s in scores)
        {
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
            mapped.Add(new TraderRowDto(/* ... s.EarlyQualityScore ... s.LastScoredAt */));
        }
        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
    }
```

Overview still counts all `Mt5Accounts` (`GetOverviewAsync` L16 `var accounts = await _db.Mt5Accounts.CountAsync(ct)`), then counts WATCH/SHADOW/LIVE from `scores` only (L30–34). That census split is not implemented by `ShadowCopyEngine`.

The only production call site (`EfTradingStore.PersistDemoShadowAsync`) constructs `new ShadowCopyEngine()` after a trader is already in `TraderState.SHADOW` and already has a score upsert. It never feeds the dashboard list.

## No-loss implication

`ShadowCopyEngine` cannot hide an unscored account and cannot send a destination order. It prices paper fills from a `DestinationQuote` and returns `ShadowFill` / MTM decimals. Dashboard omission of `Mt5Accounts` without a `TraderScores` row is a visibility gap in `EfDashboardQueries`, not a capital path in this file. No live copy, no FIX NOS, no size change, no kill-switch interaction. **No capital-loss path from this slice.**

Empty-PASS justification: the assigned file was fully read (91 lines); the angle (dashboard hiding accounts that have no score yet) is absent from `ShadowCopyEngine` by construction, not by skipped review.
