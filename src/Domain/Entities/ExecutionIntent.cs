using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class ExecutionIntent
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public Guid RiskDecisionId { get; set; }
    public string ClOrdId { get; set; } = string.Empty;
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public Guid? SourceTradeId { get; set; }
    public string DestinationAccount { get; set; } = string.Empty;
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public TradeDirection Side { get; set; }
    public decimal RequestedQuantity { get; set; }
    public ExecutionOrderStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
