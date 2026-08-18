using FluentAssertions;
using TraderIntelligence.Domain.Instruments;

namespace TraderIntelligence.Tests.Unit;

public class SymbolNormalizerTests
{
    private readonly SymbolNormalizer _n = new();

    [Theory]
    [InlineData("XAUUSD")]
    [InlineData("XAUUSD.")]
    [InlineData("XAUUSDm")]
    [InlineData("XAUUSD.a")]
    [InlineData("GOLD")]
    public void Maps_known_aliases_to_XAUUSD(string source)
    {
        _n.IsXauUsd(source).Should().BeTrue();
        _n.TryMapSource(source, out var canonical).Should().BeTrue();
        canonical.Should().Be("XAUUSD");
    }

    [Fact]
    public void Does_not_guess_venue_instrument_ids()
    {
        _n.TryMapVenueInstrumentId("123456", out _).Should().BeFalse();
        _n.RegisterVenueInstrument("123456", "XAUUSD");
        _n.TryMapVenueInstrumentId("123456", out var c).Should().BeTrue();
        c.Should().Be("XAUUSD");
    }
}
