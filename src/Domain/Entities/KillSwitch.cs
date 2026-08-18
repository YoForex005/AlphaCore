using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class KillSwitch
{
    public Guid Id { get; set; }
    public KillSwitchMode Mode { get; set; }
    public string? SetBy { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
