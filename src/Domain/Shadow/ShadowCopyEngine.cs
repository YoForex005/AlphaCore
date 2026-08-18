using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Risk;

namespace TraderIntelligence.Domain.Shadow;

public sealed record ShadowFill
{
    public required string ShadowOrderId { get; init; }
    public required decimal Price { get; init; }
    public required decimal Quantity { get; init; }
    public required DateTimeOffset FilledAt { get; init; }
    public required decimal Spread { get; init; }
    public required TimeSpan QuoteAge { get; init; }
    public required decimal SourceVsShadowSlippage { get; init; }
}

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

    public decimal MarkToMarket(TradeDirection direction, decimal qty, decimal entry, DestinationQuote quote)
    {
        var px = direction == TradeDirection.Long ? quote.Bid : quote.Ask;
        var sign = direction == TradeDirection.Long ? 1m : -1m;
        return (px - entry) * sign * qty;
    }
}
