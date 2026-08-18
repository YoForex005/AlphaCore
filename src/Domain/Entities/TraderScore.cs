using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class TraderScore
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public decimal RiskScore { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal EarlyQualityScore { get; set; }
    public int CompletedXauTrades { get; set; }
    public bool Martingale { get; set; }
    public bool AveragingDown { get; set; }
    public bool LotEscalation { get; set; }
    public TraderState CurrentState { get; set; }
    public DateTimeOffset LastScoredAt { get; set; }
}
