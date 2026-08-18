namespace TraderIntelligence.Application.Copy;

public sealed record CopyGateStatus(
    bool FeatureCopyEnabled,
    bool RealCopyArmed,
    bool QuoteLoggedOn,
    bool TradeLoggedOn,
    bool VenueReconciled,
    bool NewOrderSingleImplemented,
    int LiveTraders,
    int ShadowTraders,
    int WatchTraders,
    int Intents,
    int ShadowFills,
    int LiveSends,
    IReadOnlyList<string> Blockers,
    string Summary);

public sealed record CopyIntentRow(
    string Broker,
    long Login,
    long PositionId,
    string Action,
    string Direction,
    decimal Quantity,
    decimal ExpectedPrice,
    string Status,
    string? RiskReason,
    bool AllowFixSend,
    DateTimeOffset CreatedAt);
