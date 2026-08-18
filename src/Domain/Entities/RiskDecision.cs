using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class RiskDecision
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public RiskDecisionOutcome Outcome { get; set; }
    public decimal? AdjustedVolumeLots { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}
