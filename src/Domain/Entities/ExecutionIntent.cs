using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class ExecutionIntent
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public Guid RiskDecisionId { get; set; }
    public string DestinationSymbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public decimal VolumeLots { get; set; }
    public string? ClOrdId { get; set; }
    public string? FixOrderId { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? FilledAt { get; set; }
    public decimal? FillPrice { get; set; }
    public string? RejectReason { get; set; }
}
