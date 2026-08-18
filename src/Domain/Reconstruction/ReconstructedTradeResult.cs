using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Instruments;

namespace TraderIntelligence.Domain.Reconstruction;

public sealed class ReconstructedTradeResult
{
    public required string Id { get; init; }
    public required string BrokerId { get; init; }
    public required long Login { get; init; }
    public required long PositionId { get; init; }
    public required string CanonicalSymbol { get; init; }
    public required string SourceSymbol { get; init; }
    public required TradeDirection Direction { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
    public DateTimeOffset? ClosedAt { get; init; }
    public required decimal EntryVwap { get; init; }
    public decimal? ExitVwap { get; init; }
    public required decimal InitialVolumeLots { get; init; }
    public required decimal MaxVolumeLots { get; init; }
    public required decimal ClosedVolumeLots { get; init; }
    public required decimal RemainingVolumeLots { get; init; }
    public required decimal GrossRealizedPnl { get; init; }
    public required decimal Commission { get; init; }
    public required decimal Swap { get; init; }
    public required decimal Fees { get; init; }
    public required decimal NetRealizedPnl { get; init; }
    public required int DealCount { get; init; }
    public required int OrderCount { get; init; }
    public decimal? InitialSl { get; init; }
    public decimal? InitialTp { get; init; }
    public decimal? FinalSl { get; init; }
    public decimal? FinalTp { get; init; }
    public required bool WasScaledIn { get; init; }
    public required bool WasPartialClose { get; init; }
    public required bool WasAveragedDown { get; init; }
    public required bool Completed { get; init; }
    public IReadOnlyList<long> DealTickets { get; init; } = Array.Empty<long>();

    public bool IsXauUsd =>
        string.Equals(CanonicalSymbol, CanonicalInstrumentRef.XauUsd.Code, StringComparison.OrdinalIgnoreCase);
}
