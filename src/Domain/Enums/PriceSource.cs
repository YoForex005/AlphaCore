namespace TraderIntelligence.Domain.Enums;

public enum PriceSource
{
    Unknown = 0,
    AchieverMt5Ticks = 1,
    StarwaveMt5Ticks = 2,
    BarApproximation = 3,
    CTraderQuoteSession = 4
}
