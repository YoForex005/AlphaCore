namespace TraderIntelligence.Domain.Entities;

public sealed class SourceSymbolMapping
{
    public Guid Id { get; set; }
    public Guid BrokerId { get; set; }
    public string SourceSymbol { get; set; } = string.Empty;
    public Guid CanonicalInstrumentId { get; set; }
}
