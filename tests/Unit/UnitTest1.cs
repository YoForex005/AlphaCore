namespace TraderIntelligence.Tests.Unit;

public class SmokeTests
{
    [Fact]
    public void Domain_assembly_loads()
    {
        Assert.NotNull(typeof(TraderIntelligence.Domain.Volume.VolumeConverter).Assembly);
    }
}
