namespace TraderIntelligence.Domain.Instruments;

public sealed record CanonicalInstrumentRef(string Code)
{
    public static CanonicalInstrumentRef XauUsd { get; } = new("XAUUSD");

    public override string ToString() => Code;
}

public sealed class SymbolNormalizer
{
    private static readonly HashSet<string> DefaultXauAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "XAUUSD", "XAUUSD.", "XAUUSDM", "XAUUSD.A", "XAUUSD.I", "XAUUSD.S",
        "XAUUSD.PRO", "XAUUSDPRO", "GOLD", "GOLD.", "GOLD.A", "XAUUSDpro"
    };

    private readonly Dictionary<string, string> _sourceToCanonical;
    private readonly Dictionary<string, string> _venueIdToCanonical;

    public SymbolNormalizer(
        IEnumerable<KeyValuePair<string, string>>? extraSourceMappings = null,
        IEnumerable<KeyValuePair<string, string>>? venueIdMappings = null)
    {
        _sourceToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var alias in DefaultXauAliases)
            _sourceToCanonical[alias] = CanonicalInstrumentRef.XauUsd.Code;

        if (extraSourceMappings is not null)
        {
            foreach (var pair in extraSourceMappings)
                _sourceToCanonical[pair.Key] = pair.Value;
        }

        _venueIdToCanonical = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (venueIdMappings is not null)
        {
            foreach (var pair in venueIdMappings)
                _venueIdToCanonical[pair.Key] = pair.Value;
        }
    }

    public bool TryMapSource(string sourceSymbol, out string canonical)
    {
        if (string.IsNullOrWhiteSpace(sourceSymbol))
        {
            canonical = string.Empty;
            return false;
        }

        var key = sourceSymbol.Trim();
        if (_sourceToCanonical.TryGetValue(key, out canonical!))
            return true;

        var compact = key.Replace(".", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal);
        if (_sourceToCanonical.TryGetValue(compact, out canonical!))
            return true;

        if (compact.StartsWith("XAUUSD", StringComparison.OrdinalIgnoreCase)
            || compact.Equals("GOLD", StringComparison.OrdinalIgnoreCase))
        {
            canonical = CanonicalInstrumentRef.XauUsd.Code;
            return true;
        }

        canonical = string.Empty;
        return false;
    }

    public bool IsXauUsd(string sourceSymbol) =>
        TryMapSource(sourceSymbol, out var canonical)
        && string.Equals(canonical, CanonicalInstrumentRef.XauUsd.Code, StringComparison.OrdinalIgnoreCase);

    public bool TryMapVenueInstrumentId(string venueInstrumentId, out string canonical) =>
        _venueIdToCanonical.TryGetValue(venueInstrumentId, out canonical!);

    public void RegisterVenueInstrument(string venueInstrumentId, string canonical)
    {
        if (string.IsNullOrWhiteSpace(venueInstrumentId))
            throw new ArgumentException("Venue instrument id is required.", nameof(venueInstrumentId));
        _venueIdToCanonical[venueInstrumentId] = canonical;
    }
}
