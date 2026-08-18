using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class ShadowOrder
{
    public Guid Id { get; set; }
    public Guid CopyIntentId { get; set; }
    public Guid BrokerId { get; set; }
    public long SourceLogin { get; set; }
    public TradeDirection Direction { get; set; }
    public decimal Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Spread { get; set; }
    public decimal SourceVsShadowSlippage { get; set; }
    public DateTimeOffset FilledAt { get; set; }
}
