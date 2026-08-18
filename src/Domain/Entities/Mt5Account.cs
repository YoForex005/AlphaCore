namespace TraderIntelligence.Domain.Entities;

public sealed class Mt5Account
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public long Login { get; set; }
    public string? GroupName { get; set; }
    public int Leverage { get; set; }
    public decimal Balance { get; set; }
    public decimal Equity { get; set; }
    public decimal Margin { get; set; }
    public decimal MarginFree { get; set; }
    public decimal Profit { get; set; }
    public DateTimeOffset? RegistrationAt { get; set; }
    public DateTimeOffset? LastAccessAt { get; set; }
    public DateTimeOffset LastSyncedAt { get; set; }
}
