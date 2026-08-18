using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Reconstruction;

public sealed record NormalizedDeal
{
    public required string BrokerId { get; init; }
    public required long Login { get; init; }
    public required long DealTicket { get; init; }
    public required long OrderTicket { get; init; }
    public required long PositionId { get; init; }
    public required string SourceSymbol { get; init; }
    public required DealAction Action { get; init; }
    public required DealEntry Entry { get; init; }
    public required ulong VolumeNative { get; init; }
    public required decimal Price { get; init; }
    public required decimal Profit { get; init; }
    public required decimal Commission { get; init; }
    public required decimal Swap { get; init; }
    public required DateTimeOffset Time { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public string? Comment { get; init; }
    public DealReason? Reason { get; init; }

    public bool IsTradingDeal =>
        Action is DealAction.Buy or DealAction.Sell
        && DealReasons.CountsAsTraderActivity(Reason);
}
