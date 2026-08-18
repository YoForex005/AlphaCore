# W500_SLICE_91

- **slot:** 91
- **file:** `D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (91 lines) via `read_file` (records `ShadowFill` / `ShadowPosition` + class `ShadowCopyEngine`); workspace grep on `D:/Prop/src/Domain/Shadow` for group/fetch tokens returned **0** matches; src-wide `GetGroups` / `GroupRequestArray` live only on the MT5 connector + ingest/dashboard paths, not this type
- **verdict:** **PASS**

## Evidence quotes

`ShadowCopyEngine` is a **pure in-memory taker-touch calculator**. The file defines two records and three methods. There is no `IMt5BrokerConnector`, no Manager API, no `GetGroupsAsync`, no `GroupRequestArray`, no `GroupTotal`/`GroupNext`, and no group mask. The type cannot fetch a subset of manager groups because it never fetches groups.

File inventory after the full read:

| Symbol | Role | I/O / groups |
|---|---|---|
| `ShadowFill` | modeled fill DTO | none |
| `ShadowPosition` | modeled book DTO | none (`BrokerId`/`SourceLogin` are fields only) |
| `SimulateEntry` | dest ask/bid + optional 0.05-pt latency overlay | none |
| `SimulateExit` | dest bid/ask close + source-vs-shadow slippage | none |
| `MarkToMarket` | conservative MTM (long bid / short ask) | none |

The only inputs are caller-supplied prices, quantity, `DestinationQuote`, and clocks:

```35:60:D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs
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

Exit and MTM are the same shape — quote math only:

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
        return new ShadowFill { /* Price = raw; no group walk */ };
    }

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
```

`ShadowPosition` carries `BrokerId` + `SourceLogin` as identity fields. It does not enumerate logins or groups:

```17:28:D:/Prop/src/Domain/Shadow/ShadowCopyEngine.cs
public sealed record ShadowPosition
{
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required string SourceTradeId { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal EntryPrice { get; init; }
    public decimal? ExitPrice { get; init; }
    public required decimal UnrealizedPnl { get; init; }
    public required decimal RealizedPnl { get; init; }
    public required bool Open { get; init; }
}
```

Tokens **absent** from this file (confirmed by full read + `Domain/Shadow` grep):

- `GetGroups` / `GetGroupsAsync` / `GetGroupsCore`
- `GroupRequestArray` / `GroupTotal` / `GroupNext` / `GroupCreate`
- `manager` / `CIMTManagerAPI` / `IMt5BrokerConnector`
- `PUMP_MODE_GROUPS` / group mask / `*` enumerator
- HTTP / EF / file I/O

**Where ALL-groups fetch actually lives (out of this slice):** `NativeMt5BrokerConnector.GetGroupsCore` (`GroupRequestArray("*")` then `GroupTotal`/`GroupNext` fallback) and `DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` (`connector.GetGroupsAsync`). Incomplete census, if any, is a connector/ingest defect, not a `ShadowCopyEngine` defect.

**Call site does not pull groups either.** The only production construction is ad-hoc in `EfTradingStore.PersistDemoShadowAsync`:

```280:280:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
        var engine = new TraderIntelligence.Domain.Shadow.ShadowCopyEngine();
```

That path prices already-selected `completedXau` trades against the latest `DestinationQuotes` row. It does not walk manager groups.

Empty PASS is allowed: the assigned file was fully read (91 lines). The angle does not apply to this unit.

## No-loss implication

`ShadowCopyEngine` cannot omit a manager group, silently drop a contest/real/hedge book, or under-count source logins — it has no group enumerator and sends no orders. A missed-group no-loss failure (unseen source risk, incomplete deal window, shadowing the wrong subset of accounts) would originate in `GetGroupsCore` / ingest, not here. Worst case inside this type is a wrong modeled fill or MTM on quantities the caller already chose; that does not open, close, or size live dest risk and does not hide an entire Manager ACL group.
