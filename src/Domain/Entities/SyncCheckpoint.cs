namespace TraderIntelligence.Domain.Entities;

public sealed class SyncCheckpoint
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public string Stream { get; set; } = "deals";
    public DateTimeOffset? LastTimestamp { get; set; }
    public long? LastTicket { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
