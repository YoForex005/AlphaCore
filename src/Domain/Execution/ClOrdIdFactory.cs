namespace TraderIntelligence.Domain.Execution;

public sealed class ClOrdIdFactory
{
    public string Next(string executionIntentId, DateTimeOffset now, int sequence)
    {
        if (string.IsNullOrWhiteSpace(executionIntentId))
            throw new ArgumentException("Execution intent id is required.", nameof(executionIntentId));
        if (sequence < 0)
            throw new ArgumentOutOfRangeException(nameof(sequence));

        var compact = executionIntentId.Replace("-", "", StringComparison.Ordinal);
        if (compact.Length > 16)
            compact = compact[..16];
        return $"TI{now:yyyyMMddHHmmss}{sequence:D4}{compact}";
    }
}
