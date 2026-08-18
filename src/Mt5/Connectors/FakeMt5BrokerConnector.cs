using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Mt5.Connectors;

public sealed class FakeMt5BrokerConnector : IMt5BrokerConnector
{
    private readonly List<Mt5GroupDto> _groups;
    private readonly List<Mt5AccountDto> _accounts;
    private readonly List<Mt5DealDto> _deals;
    private readonly List<Mt5PositionDto> _positions;
    private bool _connected;

    public FakeMt5BrokerConnector(
        string brokerCode,
        IEnumerable<Mt5GroupDto>? groups = null,
        IEnumerable<Mt5AccountDto>? accounts = null,
        IEnumerable<Mt5DealDto>? deals = null,
        IEnumerable<Mt5PositionDto>? positions = null)
    {
        BrokerCode = brokerCode;
        _groups = groups?.ToList() ?? new List<Mt5GroupDto>();
        _accounts = accounts?.ToList() ?? new List<Mt5AccountDto>();
        _deals = deals?.ToList() ?? new List<Mt5DealDto>();
        _positions = positions?.ToList() ?? new List<Mt5PositionDto>();
    }

    public string BrokerCode { get; }

    public Task ConnectAsync(CancellationToken ct)
    {
        _connected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public Task<bool> IsConnectedAsync(CancellationToken ct) => Task.FromResult(_connected);

    public Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Mt5GroupDto>>(_groups);

    public Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct)
    {
        var rows = string.IsNullOrWhiteSpace(group)
            ? _accounts
            : _accounts.Where(a => a.GroupName == group).ToList();
        return Task.FromResult<IReadOnlyList<Mt5AccountDto>>(rows);
    }

    public Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var rows = _deals.Where(d => d.Login == login && d.Time >= from && d.Time <= to).ToList();
        return Task.FromResult<IReadOnlyList<Mt5DealDto>>(rows);
    }

    public Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct)
    {
        var rows = _positions.Where(p => p.Login == login).ToList();
        return Task.FromResult<IReadOnlyList<Mt5PositionDto>>(rows);
    }

    public void AddDeal(Mt5DealDto deal) => _deals.Add(deal);
}

public sealed class BrokerRegistry : IBrokerRegistry
{
    private readonly Dictionary<string, IMt5BrokerConnector> _connectors;

    public BrokerRegistry(IEnumerable<IMt5BrokerConnector> connectors)
    {
        _connectors = connectors.ToDictionary(c => c.BrokerCode, StringComparer.OrdinalIgnoreCase);
    }

    public IMt5BrokerConnector Get(string brokerCode)
    {
        if (!_connectors.TryGetValue(brokerCode, out var connector))
            throw new KeyNotFoundException($"Unknown broker '{brokerCode}'.");
        return connector;
    }

    public IReadOnlyList<IMt5BrokerConnector> All() => _connectors.Values.ToList();
}

public static class DemoBrokerFactory
{
    public const decimal VolumeScale = 10_000m;

    public static ulong Lots(decimal lots) => (ulong)decimal.Round(lots * VolumeScale, 0, MidpointRounding.AwayFromZero);

    public static (FakeMt5BrokerConnector Achiever, FakeMt5BrokerConnector Starwave) CreateDefault()
    {
        var t0 = new DateTimeOffset(2026, 6, 1, 8, 0, 0, TimeSpan.Zero);

        var achiever = new FakeMt5BrokerConnector(
            "ACHIEVER",
            groups: new[]
            {
                new Mt5GroupDto(@"demo\Maxmaster", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"contest\yo-2step", "USD", 2, "Achiever", 100, 50, true)
            },
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            deals: BuildAchieverDeals(t0));

        var starwave = new FakeMt5BrokerConnector(
            "STARWAVEFX",
            groups: new[]
            {
                new Mt5GroupDto(@"real\standard", "USD", 2, "StarwaveFX", 80, 50, true)
            },
            accounts: new[]
            {
                new Mt5AccountDto(99001, @"real\standard", 100, 8_000, 8_110, 80, 7_920, 110)
            },
            deals: BuildStarwaveDeals(t0));

        return (achiever, starwave);
    }

    private static List<Mt5DealDto> BuildAchieverDeals(DateTimeOffset t0)
    {
        var deals = new List<Mt5DealDto>();
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 501, 1, t0, 2320.10m, 2335.40m, 0.10m, 153m, -1.2m, -0.4m));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 502, 2, t0.AddHours(3), 2338.00m, 2329.20m, 0.10m, -88m, -1.1m, -0.3m, shortSide: true));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10001, 503, 3, t0.AddHours(6), 2325.50m, 2341.80m, 0.10m, 163m, -1.2m, -0.2m));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 601, 11, t0, 2320m, 2300m, 0.10m, -200m, -1m, 0));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 602, 12, t0.AddHours(2), 2300m, 2275m, 0.20m, -500m, -2m, 0));
        deals.AddRange(ClosedRoundTrip("ACHIEVER", 10002, 603, 13, t0.AddHours(4), 2275m, 2240m, 0.40m, -1400m, -4m, 0));
        return deals;
    }

    private static List<Mt5DealDto> BuildStarwaveDeals(DateTimeOffset t0)
    {
        return ClosedRoundTrip("STARWAVEFX", 99001, 701, 21, t0.AddDays(1), 2340m, 2348m, 0.05m, 40m, -0.6m, 0)
            .Concat(ClosedRoundTrip("STARWAVEFX", 99001, 702, 22, t0.AddDays(1).AddHours(2), 2348m, 2356m, 0.05m, 40m, -0.6m, 0))
            .Concat(ClosedRoundTrip("STARWAVEFX", 99001, 703, 23, t0.AddDays(1).AddHours(4), 2356m, 2362m, 0.05m, 30m, -0.6m, 0))
            .ToList();
    }

    private static IEnumerable<Mt5DealDto> ClosedRoundTrip(
        string broker,
        long login,
        long positionId,
        int seq,
        DateTimeOffset open,
        decimal entry,
        decimal exit,
        decimal lots,
        decimal profit,
        decimal commission,
        decimal swap,
        bool shortSide = false)
    {
        var vol = Lots(lots);
        var inAction = shortSide ? DealAction.Sell : DealAction.Buy;
        var outAction = shortSide ? DealAction.Buy : DealAction.Sell;
        yield return new Mt5DealDto(10_000 + seq, login, 20_000 + seq, positionId, "XAUUSD", inAction, DealEntry.In, vol, entry, 0, commission / 2, 0, open, "open");
        yield return new Mt5DealDto(10_500 + seq, login, 20_500 + seq, positionId, "XAUUSD", outAction, DealEntry.Out, vol, exit, profit, commission / 2, swap, open.AddMinutes(45), "close");
    }
}
