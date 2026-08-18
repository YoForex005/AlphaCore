namespace TraderIntelligence.Domain.Copy;

/// <summary>
/// This book copies <b>only</b> demo/contest challenge groups (Achiever <c>demo\</c>/<c>contest\</c>
/// and Starwave path segments like <c>Starwave\demo\</c>). Real/live groups are excluded.
/// </summary>
public static class CopyGroupFilter
{
    public static bool IsDemoOrContest(string? groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return false;

        var parts = groupName.Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Equals("demo", StringComparison.OrdinalIgnoreCase)
                || part.Equals("contest", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
