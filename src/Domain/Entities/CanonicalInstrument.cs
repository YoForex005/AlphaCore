namespace TraderIntelligence.Domain.Entities;

public sealed class CanonicalInstrument
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}
