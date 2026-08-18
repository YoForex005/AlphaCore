namespace TraderIntelligence.Domain.Execution;

public static class CopyIntentExpiry
{
    public static bool IsExpired(DateTimeOffset sourceEventTime, DateTimeOffset now, TimeSpan maxSignalAge) =>
        now - sourceEventTime > maxSignalAge;
}
