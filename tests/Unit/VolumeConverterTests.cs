using FluentAssertions;
using TraderIntelligence.Domain.Volume;

namespace TraderIntelligence.Tests.Unit;

public class VolumeConverterTests
{
    [Fact]
    public void Manager_scale_maps_0_10_lots_to_1000_native()
    {
        var c = VolumeConverter.Manager;
        c.Scale.Should().Be(10_000m);
        c.ToNative(0.10m).Should().Be(1000);
        c.ToLots(1000).Should().Be(0.10m);
    }

    [Fact]
    public void Extended_scale_maps_one_lot_to_100_million()
    {
        VolumeConverter.Extended.ToNative(1m).Should().Be(100_000_000);
        VolumeConverter.Extended.ToLots(100_000_000).Should().Be(1m);
    }

    [Fact]
    public void Hundredths_comment_is_not_the_default()
    {
        VolumeConverter.Manager.Scale.Should().NotBe(VolumeConverter.HundredthsScale);
    }
}
