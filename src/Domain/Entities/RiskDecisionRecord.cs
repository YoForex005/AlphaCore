using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class RiskDecisionRecord
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public RiskDecisionOutcome Outcome { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool AllowFixSend { get; set; }
    public DateTimeOffset DecidedAt { get; set; }
}
