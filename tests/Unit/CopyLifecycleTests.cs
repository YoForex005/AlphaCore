using FluentAssertions;
using TraderIntelligence.Domain.Copy;

namespace TraderIntelligence.Tests.Unit;

public class CopyLifecycleTests
{
    [Fact]
    public void Open_only_while_source_open_and_dest_empty()
    {
        CopyLifecycle.ShouldOpenDest(true, false).Should().BeTrue();
        CopyLifecycle.ShouldOpenDest(false, false).Should().BeFalse();
        CopyLifecycle.ShouldOpenDest(true, true).Should().BeFalse();
    }

    [Fact]
    public void Close_when_source_closes_after_dest_fill()
    {
        CopyLifecycle.ShouldCloseDest(sourceCompleted: true, destFilled: true, destAlreadyClosed: false).Should().BeTrue();
        CopyLifecycle.ShouldCloseDest(false, true, false).Should().BeFalse();
        CopyLifecycle.ShouldCloseDest(true, false, false).Should().BeFalse();
        CopyLifecycle.ShouldCloseDest(true, true, true).Should().BeFalse();
    }
}
