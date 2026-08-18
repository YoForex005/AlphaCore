using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Application.Contracts;

public sealed record Mt5GroupDto(
    string Name,
    string? Currency,
    int CurrencyDigits,
    string? Company,
    decimal? MarginCall,
    decimal? MarginStopOut,
    bool ConnectionsAllowed);

public sealed record Mt5AccountDto(
    long Login,
    string? GroupName,
    int Leverage,
    decimal Balance,
    decimal Equity,
    decimal Margin,
    decimal MarginFree,
    decimal Profit);

public sealed record Mt5DealDto(
    long DealTicket,
    long Login,
    long OrderTicket,
    long PositionId,
    string Symbol,
    DealAction Action,
    DealEntry Entry,
    ulong VolumeNative,
    decimal Price,
    decimal Profit,
    decimal Commission,
    decimal Swap,
    DateTimeOffset Time,
    string? Comment);

public sealed record Mt5PositionDto(
    long PositionTicket,
    long Login,
    string Symbol,
    TradeDirection Direction,
    ulong VolumeNative,
    decimal PriceOpen,
    decimal PriceCurrent,
    decimal PriceSl,
    decimal PriceTp,
    decimal Profit,
    DateTimeOffset TimeCreate);

public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}

public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}

public interface IMt5BulkDealReader
{
    Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
