using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TraderIntelligence.Fix.CTrader.Parsing;

/// <summary>
/// Lightweight FIX 4.x parser/builder intended for unit tests.
/// Input examples use '|' instead of SOH for field separators.
/// </summary>
public sealed class FixMessageParser
{
    private const char SeparatorChar = '|';
    private const char SohChar = '\u0001';

    /// <summary>
    /// Parses tag/value pairs from a pipe-delimited FIX string (e.g. cTrader examples).
    /// Validates checksum (tag 10) and throws if invalid.
    /// </summary>
    public IReadOnlyDictionary<int, string> Parse(string fixPipeDelimited)
    {
        if (string.IsNullOrWhiteSpace(fixPipeDelimited))
            throw new ArgumentException("FIX message cannot be null/empty.", nameof(fixPipeDelimited));

        // Normalize: trim whitespace, drop trailing separator if present.
        var normalized = fixPipeDelimited.Trim();
        normalized = normalized.EndsWith(SeparatorChar, StringComparison.Ordinal)
            ? normalized[..^1]
            : normalized;

        var parts = normalized.Split(SeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            throw new FormatException("FIX message contains no fields.");

        // Tag 10 must be last in valid FIX message.
        var last = parts[^1];
        if (!last.StartsWith("10=", StringComparison.Ordinal))
            throw new FormatException("FIX message missing checksum (10=...).");

        if (!int.TryParse(last.AsSpan(3), out _))
            throw new FormatException("FIX checksum (10) is not numeric.");

        var providedChecksum = last[3..];
        var expectedChecksum = ComputeChecksum(parts.Take(parts.Length - 1));
        if (!string.Equals(providedChecksum, expectedChecksum, StringComparison.Ordinal))
            throw new InvalidOperationException($"Invalid FIX checksum. Expected {expectedChecksum}, got {providedChecksum}.");

        var tags = new Dictionary<int, string>(capacity: parts.Length);
        foreach (var p in parts)
        {
            var eqIdx = p.IndexOf('=');
            if (eqIdx <= 0)
                throw new FormatException($"Invalid FIX field segment: '{p}'");

            var tagStr = p[..eqIdx];
            var val = p[(eqIdx + 1)..];
            if (!int.TryParse(tagStr, out var tag))
                throw new FormatException($"Invalid FIX tag '{tagStr}'.");

            tags[tag] = val;
        }

        return tags;
    }

    /// <summary>
    /// Builds a pipe-delimited FIX message from tag/value pairs.
    /// Calculates BodyLength (9) and Checksum (10).
    /// </summary>
    public string BuildFixMessage(IEnumerable<KeyValuePair<int, string>> fields)
    {
        if (fields is null) throw new ArgumentNullException(nameof(fields));

        var ordered = fields
            .Where(kv => kv.Key != 9 && kv.Key != 10) // 9 and 10 are computed
            .ToList();

        // Ensure required BeginString exists.
        var beginStringKv = ordered.FirstOrDefault(kv => kv.Key == 8);
        if (beginStringKv.Key != 8)
            throw new ArgumentException("fields must include tag 8 (BeginString).", nameof(fields));

        // In unit tests we don't try to emulate every possible field ordering rule.
        // We place BeginString first, then MsgType (if present), then remaining tags ascending.
        var msgTypeKv = ordered.FirstOrDefault(kv => kv.Key == 35);
        var remaining = ordered.Where(kv => kv.Key != 8 && kv.Key != 35).OrderBy(kv => kv.Key).ToList();

        var bodyFields = new List<KeyValuePair<int, string>>(capacity: ordered.Count + 1)
        {
            // body starts after BodyLength field
            msgTypeKv.Key == 35 ? msgTypeKv : default,
        };
        // Remove default MsgType entry if tag 35 missing.
        bodyFields = bodyFields.Where(kv => kv.Key != 0).ToList();
        bodyFields.AddRange(remaining);

        // FIX wire format includes the SOH delimiter before tag 10.
        // That delimiter is the SOH after the last body field, so it must be included
        // both in BodyLength calculation and checksum calculation.
        var body = JoinSohFields(bodyFields) + SohChar;
        var beginString = $"8={beginStringKv.Value}";

        // BodyLength is the number of bytes in the message following the 9=BodyLength field up to (but not including) the 10= checksum field.
        // That means: MsgType...fields (with SOH separators) as used in FIX wire format.
        var bodyLen = Encoding.ASCII.GetByteCount(body);
        var header = $"{beginString}{SohChar}9={bodyLen}{SohChar}";

        var withoutChecksum = header + body;
        var checksum = ComputeChecksumFromRaw(withoutChecksum);
        return withoutChecksum.Replace(SohChar, SeparatorChar) + $"|10={checksum}";
    }

    private static string JoinSohFields(IEnumerable<KeyValuePair<int, string>> fields)
    {
        var sb = new StringBuilder();
        var first = true;
        foreach (var kv in fields)
        {
            if (!first) sb.Append(SohChar);
            first = false;
            sb.Append(kv.Key);
            sb.Append('=');
            sb.Append(kv.Value);
        }

        return sb.ToString();
    }

    private static string ComputeChecksum(IEnumerable<string> fieldsBeforeChecksum)
    {
        // Rebuild raw message segment using SOH separators and trailing SOH before checksum field.
        var rawSegment = string.Join(SohChar, fieldsBeforeChecksum) + SohChar;
        return ComputeChecksumFromRaw(rawSegment);
    }

    private static string ComputeChecksumFromRaw(string rawWithoutChecksumField)
    {
        var bytes = Encoding.ASCII.GetBytes(rawWithoutChecksumField);
        var sum = bytes.Sum(b => b);
        var check = sum % 256;
        return check.ToString("D3");
    }
}

