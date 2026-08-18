namespace TraderIntelligence.Domain.Entities;

public sealed class DestinationQuoteSnapshot
{
    public Guid Id { get; set; }
    public string CanonicalSymbol { get; set; } = "XAUUSD";
    public string? VenueInstrumentId { get; set; }
    public decimal Bid { get; set; }
    public decimal Ask { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? VenueTimestamp { get; set; }
}
