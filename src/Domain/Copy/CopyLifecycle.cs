namespace TraderIntelligence.Domain.Copy;

public static class CopyLifecycle
{
    public static bool ShouldOpenDest(bool sourceStillOpen, bool destAlreadyFilled) =>
        sourceStillOpen && !destAlreadyFilled;

    public static bool ShouldCloseDest(bool sourceCompleted, bool destFilled, bool destAlreadyClosed) =>
        sourceCompleted && destFilled && !destAlreadyClosed;

    public static bool ShouldCloseDestBecauseMasterGone(
        bool masterTicketInBook, bool destFilled, bool destAlreadyClosed) =>
        !masterTicketInBook && destFilled && !destAlreadyClosed;

    public static bool TrustManagerBook(int positionCount) => positionCount > 0;

    public static bool TrustDestVenueSnapshot(bool complete, int venueOpen, int ledgerOpen) =>
        complete && (venueOpen > 0 || ledgerOpen == 0);
}
