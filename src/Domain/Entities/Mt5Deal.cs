using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class Mt5Deal
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long DealTicket { get; set; }
    public long Login { get; set; }
    public long OrderTicket { get; set; }
    public long PositionId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DealAction Action { get; set; }
    public DealEntry Entry { get; set; }
    public ulong VolumeNative { get; set; }
    public decimal Price { get; set; }
    public decimal Profit { get; set; }
    public decimal Commission { get; set; }
    public decimal Swap { get; set; }
    public DateTimeOffset DealTime { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset IngestedAt { get; set; }
}
