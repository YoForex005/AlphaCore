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

    [Fact]
    public void Close_when_master_ticket_leaves_manager_book()
    {
        CopyLifecycle.ShouldCloseDestBecauseMasterGone(masterTicketInBook: false, destFilled: true, destAlreadyClosed: false)
            .Should().BeTrue();
        CopyLifecycle.ShouldCloseDestBecauseMasterGone(true, true, false).Should().BeFalse();
        CopyLifecycle.ShouldCloseDestBecauseMasterGone(false, false, false).Should().BeFalse();
        CopyLifecycle.ShouldCloseDestBecauseMasterGone(false, true, true).Should().BeFalse();
    }

    [Fact]
    public void Empty_manager_book_is_not_trusted()
    {
        CopyLifecycle.TrustManagerBook(0).Should().BeFalse();
        CopyLifecycle.TrustManagerBook(12).Should().BeTrue();
    }

    [Fact]
    public void Incomplete_dest_snapshot_is_not_trusted()
    {
        CopyLifecycle.TrustDestVenueSnapshot(complete: false, venueOpen: 10, ledgerOpen: 10).Should().BeFalse();
        CopyLifecycle.TrustDestVenueSnapshot(true, 0, 200).Should().BeFalse();
        CopyLifecycle.TrustDestVenueSnapshot(true, 214, 214).Should().BeTrue();
        CopyLifecycle.TrustDestVenueSnapshot(true, 0, 0).Should().BeTrue();
    }
}
