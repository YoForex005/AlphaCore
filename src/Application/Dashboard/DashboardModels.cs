using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Application.Dashboard;

public sealed record OverviewDto(
    int TotalAccounts,
    int ConnectedBrokers,
    int XauTraders,
    int TradersWithThreeTrades,
    int Watch,
    int Shadow,
    int LiveCandidates,
    int Live,
    int RiskBlocked,
    decimal ShadowPnl,
    decimal DestinationRealPnl,
    decimal XauGross,
    decimal XauNet,
    bool Mt5Healthy,
    bool QuoteHealthy,
    bool TradeHealthy,
    bool RealCopyEnabled);

public sealed record BrokerStatusDto(
    string Code,
    string DisplayName,
    string Server,
    long ManagerLoginMasked,
    bool Connected,
    int GroupCount,
    int AccountCount,
    DateTimeOffset? LastEventAt);

public sealed record GroupRowDto(
    string Broker,
    string Group,
    int Accounts,
    bool EnabledForAnalysis,
    string? PlanMapping,
    DateTimeOffset? LastDiscovered,
    DateTimeOffset? LastSynced);

public sealed record TraderRowDto(
    string Broker,
    long Login,
    string? Group,
    int CompletedXauTrades,
    decimal NetSourcePnl,
    decimal EarlyScore,
    decimal? MlProbability,
    decimal RiskScore,
    bool Martingale,
    bool AveragingDown,
    bool LotEscalation,
    TraderState State,
    decimal ShadowPnl,
    DateTimeOffset LastScored);

public sealed record FixSessionDto(
    string Qualifier,
    string Host,
    int Port,
    bool Connected,
    bool LoggedOn,
    string Status,
    DateTimeOffset? LastInbound,
    DateTimeOffset? LastOutbound,
    int InboundSeq,
    int OutboundSeq,
    int ReconnectCount,
    string? LastError,
    string? InstrumentId,
    decimal? Bid,
    decimal? Ask,
    double? QuoteAgeSeconds,
    bool ExecutionEnabled);

public sealed record RiskDashboardDto(
    decimal DailyPnl,
    decimal Drawdown,
    decimal XauLong,
    decimal XauShort,
    decimal XauNet,
    string KillSwitch,
    bool RealCopyEnabled,
    IReadOnlyList<string> RecentRejectReasons);

public interface IDashboardQueries
{
    Task<OverviewDto> GetOverviewAsync(CancellationToken ct);
    Task<IReadOnlyList<BrokerStatusDto>> GetBrokersAsync(CancellationToken ct);
    Task<IReadOnlyList<GroupRowDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct);
    Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct);
    Task<IReadOnlyList<FixSessionDto>> GetFixSessionsAsync(CancellationToken ct);
    Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct);
}
