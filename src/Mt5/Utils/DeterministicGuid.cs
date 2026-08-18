using System.Security.Cryptography;
using System.Text;

namespace TraderIntelligence.Mt5.Utils;

public static class DeterministicGuid
{
    /// <summary>
    /// Creates a stable GUID from arbitrary inputs.
    /// Useful for idempotent upserts where the source does not provide a GUID.
    /// </summary>
    public static Guid FromString(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        // GUID uses 16 bytes; we take the first 16 bytes from the hash.
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}

