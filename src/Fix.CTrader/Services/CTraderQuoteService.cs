using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TraderIntelligence.Fix.CTrader.Configuration;

namespace TraderIntelligence.Fix.CTrader.Services;

/// <summary>
/// QUOTE-side service responsibilities:
/// 1) Discover instruments via SecurityList.
/// 2) Identify the cTrader numeric instrument id for XAUUSD.
/// 3) Subscribe to market data for the XAU instrument.
/// 4) Keep latest bid/ask/timestamp and reject stale quotes.
/// </summary>
public sealed class CTraderQuoteService
{
    private readonly CTraderFixOptions _options;

    // cTrader instrument id stored from SecurityList response (tag 55).
    private long? _xauInstrumentId;

    private decimal? _latestBid;
    private decimal? _latestAsk;
    private DateTimeOffset? _latestTimestampUtc;

    public CTraderQuoteService(CTraderFixOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.Quote is null) throw new ArgumentException("Quote options must be provided.", nameof(options));
    }

    public bool IsInstrumentResolved => _xauInstrumentId.HasValue;

    public long XauInstrumentId => _xauInstrumentId
        ?? throw new InvalidOperationException("XAUUSD instrument not resolved yet.");

    public decimal? LatestBid => _latestBid;
    public decimal? LatestAsk => _latestAsk;
    public DateTimeOffset? LatestTimestampUtc => _latestTimestampUtc;

    /// <summary>
    /// Maps XAUUSD by iterating a parsed SecurityList response.
    /// Expected mapping: tag 1007 = SymbolName, tag 55 = InstrumentID (numeric string).
    /// </summary>
    public void OnSecurityListResponse(IEnumerable<IReadOnlyDictionary<int, string>> instrumentEntries)
    {
        if (instrumentEntries is null) throw new ArgumentNullException(nameof(instrumentEntries));

        foreach (var entry in instrumentEntries)
        {
            if (!entry.TryGetValue(1007, out var symbolName))
                continue;
            if (!string.Equals(symbolName, "XAUUSD", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!entry.TryGetValue(55, out var instrumentIdRaw))
                throw new FormatException("SecurityList entry missing tag 55 (InstrumentID) for XAUUSD.");
            if (!long.TryParse(instrumentIdRaw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var instrumentId))
                throw new FormatException($"SecurityList tag 55 (InstrumentID) is not numeric: '{instrumentIdRaw}'.");

            _xauInstrumentId = instrumentId;
            return;
        }

        throw new InvalidOperationException("SecurityList did not contain XAUUSD.");
    }

    /// <summary>
    /// Updates quote state from a parsed MarketData snapshot.
    /// Simplified harness tags: 1320=bid, 1321=ask, 60=timestamp.
    /// </summary>
    public bool TryAcceptMarketDataSnapshot(IReadOnlyDictionary<int, string> tags, out string? rejectReason)
    {
        rejectReason = null;
        if (tags is null) throw new ArgumentNullException(nameof(tags));

        if (!tags.TryGetValue(1320, out var bidRaw) || !decimal.TryParse(bidRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var bid))
        {
            rejectReason = "Missing/invalid bid (tag 1320).";
            return false;
        }
        if (!tags.TryGetValue(1321, out var askRaw) || !decimal.TryParse(askRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var ask))
        {
            rejectReason = "Missing/invalid ask (tag 1321).";
            return false;
        }
        if (!tags.TryGetValue(60, out var tsRaw) ||
            !DateTimeOffset.TryParseExact(tsRaw, "yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var ts))
        {
            rejectReason = "Missing/invalid timestamp (tag 60).";
            return false;
        }

        var ageMs = (DateTimeOffset.UtcNow - ts.ToUniversalTime()).TotalMilliseconds;
        if (ageMs < 0) ageMs = 0;
        if (ageMs > _options.MaxQuoteAgeMs)
        {
            rejectReason = $"Quote is stale. AgeMs={ageMs:0}. ThresholdMs={_options.MaxQuoteAgeMs}.";
            return false;
        }

        _latestBid = bid;
        _latestAsk = ask;
        _latestTimestampUtc = ts.ToUniversalTime();
        return true;
    }

    /// <summary>
    /// Outgoing FIX tag set for SecurityListRequest.
    /// </summary>
    public IReadOnlyList<KeyValuePair<int, string>> BuildSecurityListRequestTags()
    {
        return new List<KeyValuePair<int, string>> { new(35, "y") };
    }

    /// <summary>
    /// Outgoing FIX tag set for subscribing to market data for the resolved XAU instrument.
    /// SubscriptionRequestType=1, MarketDepth=1 (spot).
    /// </summary>
    public IReadOnlyList<KeyValuePair<int, string>> BuildMarketDataRequestTags()
    {
        if (!_xauInstrumentId.HasValue)
            throw new InvalidOperationException("XAUUSD instrument id not resolved.");

        return new List<KeyValuePair<int, string>>
        {
            new(35, "V"), // MarketDataRequest
            new(55, _xauInstrumentId.Value.ToString(CultureInfo.InvariantCulture)),
            new(263, "1"), // SubscriptionRequestType=1
            new(264, "1")  // MarketDepth=1 (spot)
        };
    }
}
