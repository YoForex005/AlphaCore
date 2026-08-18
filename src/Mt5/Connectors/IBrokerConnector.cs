using TraderIntelligence.Domain.Entities;

namespace TraderIntelligence.Mt5.Connectors;

public interface IBrokerConnector
{
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    bool IsConnected { get; }

    Task<IReadOnlyList<Mt5Group>> GetGroupsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Mt5Account>> GetAccountsAsync(Mt5Group group, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Mt5Deal>> GetDealsAsync(
        ulong login,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Mt5Position>> GetPositionsAsync(
        ulong login,
        CancellationToken cancellationToken = default);

    Task<DateTimeOffset> GetServerTimeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to a broker event stream.
    /// Expected events: deal added/updated, and position change.
    /// </summary>
    IAsyncEnumerable<Mt5BrokerEvent> SubscribeEventsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Event emitted by an MT5 broker connector (SSE or future native binding).
/// </summary>
public sealed record Mt5BrokerEvent(
    Guid BrokerId,
    ulong Login,
    ulong? DealTicket,
    Mt5Deal? Deal,
    ulong? PositionTicket,
    Mt5Position? Position,
    DateTimeOffset EventTimeUtc,
    string EventType);

