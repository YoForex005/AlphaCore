namespace TraderIntelligence.Domain.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Target { get; set; }
    public string? PayloadJson { get; set; }
    public DateTimeOffset At { get; set; }
}
