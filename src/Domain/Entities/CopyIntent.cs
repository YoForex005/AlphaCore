using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class CopyIntent
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public Guid? SourceTradeId { get; set; }
    public long SourcePositionId { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public CopyIntentAction Action { get; set; }
    public TradeDirection Direction { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ExpectedPrice { get; set; }
    public DateTimeOffset SourceEventTime { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string Status { get; set; } = "Pending";
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid? RiskDecisionId { get; set; }
    public Guid? ExecutionIntentId { get; set; }
}
