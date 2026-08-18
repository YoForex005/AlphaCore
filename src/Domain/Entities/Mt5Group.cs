namespace TraderIntelligence.Domain.Entities;

public sealed class Mt5Group
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Currency { get; set; }
    public int CurrencyDigits { get; set; } = 2;
    public string? Company { get; set; }
    public decimal? MarginCall { get; set; }
    public decimal? MarginStopOut { get; set; }
    public bool ConnectionsAllowed { get; set; }
    public bool EnabledForAnalysis { get; set; } = true;
    public string? PlanMapping { get; set; }
    public DateTimeOffset? LastDiscoveredAt { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }
}
