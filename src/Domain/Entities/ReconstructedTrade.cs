using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class ReconstructedTrade
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public long PositionId { get; set; }
    public string CanonicalSymbol { get; set; } = string.Empty;
    public string SourceSymbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public DateTimeOffset OpenedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public decimal EntryVwap { get; set; }
    public decimal? ExitVwap { get; set; }
    public decimal InitialVolumeLots { get; set; }
    public decimal MaxVolumeLots { get; set; }
    public decimal ClosedVolumeLots { get; set; }
    public decimal GrossRealizedPnl { get; set; }
    public decimal Commission { get; set; }
    public decimal Swap { get; set; }
    public decimal Fees { get; set; }
    public decimal NetRealizedPnl { get; set; }
    public int DealCount { get; set; }
    public int OrderCount { get; set; }
    public decimal? InitialSl { get; set; }
    public decimal? InitialTp { get; set; }
    public decimal? FinalSl { get; set; }
    public decimal? FinalTp { get; set; }
    public bool WasScaledIn { get; set; }
    public bool WasPartialClose { get; set; }
    public bool WasAveragedDown { get; set; }
    public bool Completed { get; set; }
}
