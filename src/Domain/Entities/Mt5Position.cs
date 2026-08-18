using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class Mt5Position
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long PositionTicket { get; set; }
    public long Login { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public TradeDirection Direction { get; set; }
    public ulong VolumeNative { get; set; }
    public decimal PriceOpen { get; set; }
    public decimal PriceCurrent { get; set; }
    public decimal PriceSl { get; set; }
    public decimal PriceTp { get; set; }
    public decimal Profit { get; set; }
    public decimal Swap { get; set; }
    public DateTimeOffset TimeCreate { get; set; }
    public DateTimeOffset TimeUpdate { get; set; }
}
