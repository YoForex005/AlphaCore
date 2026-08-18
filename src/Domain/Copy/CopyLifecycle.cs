namespace TraderIntelligence.Domain.Copy;

public static class CopyLifecycle
{
    public static bool ShouldOpenDest(bool sourceStillOpen, bool destAlreadyFilled) =>
        sourceStillOpen && !destAlreadyFilled;

    public static bool ShouldCloseDest(bool sourceCompleted, bool destFilled, bool destAlreadyClosed) =>
        sourceCompleted && destFilled && !destAlreadyClosed;
}
