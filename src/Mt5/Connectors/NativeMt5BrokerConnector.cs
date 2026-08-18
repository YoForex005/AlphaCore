using System.Runtime.InteropServices;
using MetaQuotes.MT5CommonAPI;
using MetaQuotes.MT5ManagerAPI;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Mt5.Connectors;

public sealed class NativeMt5Options
{
    public required string BrokerCode { get; init; }
    public required string Server { get; init; }
    public required int Port { get; init; }
    public required ulong Login { get; init; }
    public required string Password { get; init; }
    public bool ProxyEnabled { get; init; }
    public string? ProxyHost { get; init; }
    public int ProxyPort { get; init; }
    public string? ProxyUser { get; init; }
    public string? ProxyPassword { get; init; }
    public string? NativeDllDirectory { get; init; }
}

public sealed class NativeMt5BrokerConnector : IMt5BrokerConnector, IMt5BulkDealReader, IMt5BulkPositionReader, IDisposable
{
    private readonly NativeMt5Options _opt;
    private readonly object _gate = new();
    private CIMTManagerAPI? _manager;
    private bool _connected;
    private bool _pumpEnabled;

    public NativeMt5BrokerConnector(NativeMt5Options opt) => _opt = opt;

    public string BrokerCode => _opt.BrokerCode;
    public string? LastError { get; private set; }
    public bool PumpEnabled => _pumpEnabled;

    public Task ConnectAsync(CancellationToken ct) => Task.Run(ConnectCore, ct);
    public Task DisconnectAsync(CancellationToken ct) { DisconnectCore(); return Task.CompletedTask; }
    public Task<bool> IsConnectedAsync(CancellationToken ct) => Task.FromResult(_connected);

    public Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct) =>
        Task.Run(GetGroupsCore, ct);

    public Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct) =>
        Task.Run(() => GetAccountsCore(group), ct);

    public Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.Run(() => GetDealsCore(login, from, to), ct);

    public Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.Run(() => GetGroupDealsCore(group, from, to), ct);

    public Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct) =>
        Task.Run(() => GetPositionsCore(login), ct);

    public Task<IReadOnlyList<Mt5PositionDto>> GetGroupPositionsAsync(string? groupMask, CancellationToken ct) =>
        Task.Run(() => GetGroupPositionsCore(string.IsNullOrWhiteSpace(groupMask) ? "*" : groupMask), ct);

    private void ConnectCore()
    {
        lock (_gate)
        {
            if (_connected)
                return;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
            if (init != MTRetCode.MT_RET_OK && init != MTRetCode.MT_RET_ERR_DUPLICATE)
            {
                LastError = Describe(init, "Factory.Initialize");
                throw new InvalidOperationException(LastError);
            }

            SMTManagerAPIFactory.GetVersion(out var version);
            var created = SMTManagerAPIFactory.CreateManager(version, out var createRes);
            if (createRes != MTRetCode.MT_RET_OK || created is null)
            {
                LastError = Describe(createRes, "CreateManager");
                throw new InvalidOperationException(LastError);
            }

            _manager = created;
            ApplyProxy();

            var endpoint = $"{_opt.Server}:{_opt.Port}";
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                LastError = null;
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
            if (res != MTRetCode.MT_RET_OK)
            {
                LastError = Describe(res, $"Connect {BrokerCode} {endpoint} proxy={_opt.ProxyEnabled}");
                _connected = false;
                throw new InvalidOperationException(LastError);
            }

            _connected = true;
            _pumpEnabled = false;
            LastError = null;
        }
    }

    private void ApplyProxy()
    {
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;

        var proxy = new MTProxyInfo
        {
            enable = 1,
            type = MTProxyInfo.Type.PROXY_HTTP,
            address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
            auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
        };
        var set = _manager.ProxySet(proxy);
        if (set != MTRetCode.MT_RET_OK)
            throw new InvalidOperationException(Describe(set, $"{BrokerCode} ProxySet"));
    }

    private void DisconnectCore()
    {
        lock (_gate)
        {
            _manager?.Disconnect();
            _manager?.Dispose();
            _manager = null;
            _connected = false;
            _pumpEnabled = false;
        }
    }

    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        lock (_gate)
        {
            Ensure();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Mt5GroupDto>();

            var arr = _manager!.GroupCreateArray();
            try
            {
                var res = _manager.GroupRequestArray("*", arr);
                if (res == MTRetCode.MT_RET_OK || res == MTRetCode.MT_RET_OK_NONE)
                {
                    for (uint i = 0; i < arr.Total(); i++)
                    {
                        var g = arr.Next(i);
                        if (g is null)
                            continue;
                        AddGroup(list, seen, g);
                    }
                }
            }
            finally { arr.Release(); }

            if (list.Count == 0)
            {
                var grp = _manager.GroupCreate();
                try
                {
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
                        AddGroup(list, seen, grp);
                    }
                }
                finally { grp.Release(); }
            }

            return list;
        }
    }

    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }

            return byLogin.Values.ToList();
        }
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        var rows = new List<Mt5AccountDto>();
        var users = _manager!.UserCreateArray();
        var accounts = _manager.UserCreateAccountArray();
        try
        {
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
            }

            var acctReq = _manager.UserAccountRequestArray(gname, accounts);
            if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
                _manager.UserAccountGetByGroup(gname, accounts);

            var acctByLogin = new Dictionary<ulong, CIMTAccount>();
            for (uint i = 0; i < accounts.Total(); i++)
            {
                var a = accounts.Next(i);
                if (a is not null)
                    acctByLogin[a.Login()] = a;
            }

            for (uint i = 0; i < users.Total(); i++)
            {
                var u = users.Next(i);
                if (u is null)
                    continue;
                acctByLogin.TryGetValue(u.Login(), out var acc);
                rows.Add(new Mt5AccountDto(
                    (long)u.Login(),
                    u.Group(),
                    (int)u.Leverage(),
                    (decimal)(acc?.Balance() ?? 0),
                    (decimal)(acc?.Equity() ?? 0),
                    (decimal)(acc?.Margin() ?? 0),
                    (decimal)(acc?.MarginFree() ?? 0),
                    (decimal)(acc?.Profit() ?? 0)));
            }
        }
        finally
        {
            users.Release();
            accounts.Release();
        }

        return rows;
    }

    private IReadOnlyList<Mt5DealDto> GetDealsCore(long login, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequest((ulong)login, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequest {login}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }

            return all;
        }
    }

    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequestByGroup {group}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }

            return all;
        }
    }

    private IReadOnlyList<Mt5PositionDto> GetPositionsCore(long login)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.PositionCreateArray();
            try
            {
                var res = _manager.PositionRequest((ulong)login, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    return Array.Empty<Mt5PositionDto>();
                return ReadPositions(arr);
            }
            finally { arr.Release(); }
        }
    }

    private IReadOnlyList<Mt5PositionDto> GetGroupPositionsCore(string mask)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.PositionCreateArray();
            try
            {
                var res = _manager.PositionRequestByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    res = _manager.PositionGetByGroup(mask, arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    return Array.Empty<Mt5PositionDto>();
                return ReadPositions(arr);
            }
            finally { arr.Release(); }
        }
    }

    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Windows(DateTimeOffset from, DateTimeOffset to)
    {
        var cursor = from;
        while (cursor < to)
        {
            var end = cursor.AddDays(14);
            if (end > to)
                end = to;
            yield return (cursor, end);
            cursor = end;
        }
    }

    private static void AddGroup(List<Mt5GroupDto> list, HashSet<string> seen, CIMTConGroup grp)
    {
        var name = grp.Group();
        if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
            return;
        list.Add(new Mt5GroupDto(
            name,
            grp.Currency(),
            (int)grp.CurrencyDigits(),
            grp.Company(),
            (decimal)grp.MarginCall(),
            (decimal)grp.MarginStopOut(),
            ((ulong)grp.PermissionsFlags() & 0x2) != 0));
    }

    private static List<Mt5PositionDto> ReadPositions(CIMTPositionArray arr)
    {
        var rows = new List<Mt5PositionDto>((int)arr.Total());
        for (uint i = 0; i < arr.Total(); i++)
        {
            var p = arr.Next(i);
            if (p is null)
                continue;
            rows.Add(new Mt5PositionDto(
                (long)p.Position(),
                (long)p.Login(),
                p.Symbol(),
                p.Action() == 0 ? TradeDirection.Long : TradeDirection.Short,
                p.Volume(),
                (decimal)p.PriceOpen(),
                (decimal)p.PriceCurrent(),
                (decimal)p.PriceSL(),
                (decimal)p.PriceTP(),
                (decimal)p.Profit(),
                DateTimeOffset.FromUnixTimeSeconds(p.TimeCreate())));
        }

        return rows;
    }

    private static List<Mt5DealDto> ReadDeals(CIMTDealArray arr)
    {
        var rows = new List<Mt5DealDto>((int)arr.Total());
        for (uint i = 0; i < arr.Total(); i++)
        {
            var d = arr.Next(i);
            if (d is null)
                continue;
            rows.Add(new Mt5DealDto(
                (long)d.Deal(),
                (long)d.Login(),
                (long)d.Order(),
                (long)d.PositionID(),
                d.Symbol(),
                (DealAction)d.Action(),
                (DealEntry)d.Entry(),
                d.Volume(),
                (decimal)d.Price(),
                (decimal)d.Profit(),
                (decimal)d.Commission(),
                (decimal)d.Storage(),
                DateTimeOffset.FromUnixTimeSeconds(d.Time()),
                d.Comment()));
        }

        return rows;
    }

    private void Ensure()
    {
        if (_manager is null || !_connected)
            throw new InvalidOperationException($"{BrokerCode} is not connected. {LastError}");
    }

    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            5 => "disk/no-connect in some builds — server unreachable",
            10 => "no connection",
            9 => "timeout",
            _ => code.ToString()
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }

    public void Dispose() => DisconnectCore();
}
