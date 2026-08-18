using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class TraderScoreHistory
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public decimal RiskScore { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal EarlyQualityScore { get; set; }
    public TraderState State { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
