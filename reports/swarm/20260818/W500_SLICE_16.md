# W500_SLICE_16

- **slot:** 16
- **file:** `D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (91 lines) via `read_file`; grep on this file for `score|dashboard|hide|hidden|account|TraderScore|Mt5Account|GetTraders|unscored` returned **no matches**
- **verdict:** PASS

## Evidence quotes

`ShadowCopyEngine.cs` is a destination-touch fill calculator only: two records (`ShadowFill`, `ShadowPosition`) and three methods (`SimulateEntry`, `SimulateExit`, `MarkToMarket`). It has no dashboard query, no `TraderScore` join, no `Mt5Account` enumeration, and no filter that would drop logins lacking a score row.

```31:90:D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs
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
        // ...
    }

    public ShadowFill SimulateExit(/* dest bid/ask close; always returns a ShadowFill */) { /* ... */ }

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
}
```

This file does not contain:

- `TraderScore` / `LastScoredAt` / `EarlyQualityScore`
- `IDashboardQueries` / `GetTradersAsync` / `TraderRowDto`
- `Mt5Accounts` left-join or “show only scored”
- any `hide` / `Where(s => s.HasScore)` / `CompletedXauTrades` gate

The assigned angle **does** exist elsewhere, but **not in this file**. `EfDashboardQueries.GetTradersAsync` materializes only `TraderScores` and never emits an `Mt5Accounts` row that lacks a score:

```74:88:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
{
    var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
    var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
    var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
    // ...
    var mapped = new List<TraderRowDto>();
    foreach (var s in scores)
    {
        if (!brokers.TryGetValue(s.BrokerId, out var b))
            continue;
```

That leaderboard inner-join is **out of this slice’s file**. Slot 16 does not own it and does not implement it.

Product caller of this engine (`EfTradingStore.PersistDemoShadowAsync`) constructs `new ShadowCopyEngine()` only after `state == TraderState.SHADOW`. Unscored / never-scored logins never reach `SimulateEntry` from that path. The engine itself still does not decide dashboard visibility.

## No-loss implication

`ShadowCopyEngine` cannot hide (or un-hide) accounts on the dashboard: it never reads `Mt5Accounts` or `TraderScores`. It cannot emit live FIX / MT5 orders. A missing score row is an operator-visibility gap in `EfDashboardQueries`, not a fill-pricing decision here. Worst case inside this type is a simulated dest-touch price (plus a 0.05 overlay when `modeledDelay > 250ms`) written to in-memory `ShadowFill` / persisted `ShadowOrders` by a caller — **paper/shadow grain, not live capital**.

Empty-PASS justification: the assigned file was fully read (91/91 lines); the angle (dashboard hiding accounts that have no score yet) is absent by construction, not by skipped review.
