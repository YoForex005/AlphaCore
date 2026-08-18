# W500_SLICE_50

- **slot:** 50
- **file:** `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (458 lines) via `read_file` (lines 1–458); grep on this file for `dummy|seeded|seed|fake|mock|stub|sample|placeholder|hardcoded|synthetic|Simulat|IsLive|IsDemo|paper|sandbox|UseDummy|UseSeed|fallback` returned **no matches**. Broader token scan (`Random`, `Guid.`, `NotImplemented`, `10001`, `10_000`, `DemoBrokerFactory`, `FakeMt5`, `DemoSeeder`) also **zero hits** in this type.
- **verdict:** PASS

## File (assigned)

`D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs` is the live Manager-API connector (`IMt5BrokerConnector`, `IMt5BulkDealReader`, `IMt5BulkPositionReader`). Same compilation unit also defines `NativeMt5Options`. There is no in-memory book, no `DemoBrokerFactory` import, and no `FakeMt5BrokerConnector` reference.

Sibling `D:/Prop/src/Mt5/Connectors/FakeMt5BrokerConnector.cs` still holds canned Achiever/Starwave logins (`10001` / `10002` / `10003` / `99001`) and XAU round-trips. That type is **not reachable** from this class.

## Evidence quotes

### 1. No dummy / seed / fake symbols in the assigned file

Workspace grep of `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs` (case-insensitive) for dummy/seed/fake/mock/stub/sample/placeholder/synthetic/`10001`/`DemoBrokerFactory`/`FakeMt5`/`Random`/`NotImplemented` returned **zero** hits. The file never names `DemoSeeder`, `BrokerCatalogSeed`, `FakeMt5BrokerConnector`, or `DemoBrokerFactory`.

Construction is options-only. No canned groups, deals, or balances:

```32:40:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    public NativeMt5BrokerConnector(NativeMt5Options opt) => _opt = opt;

    public string BrokerCode => _opt.BrokerCode;
    public string? LastError { get; private set; }
    public bool PumpEnabled => _pumpEnabled;

    public Task ConnectAsync(CancellationToken ct) => Task.Run(ConnectCore, ct);
    public Task DisconnectAsync(CancellationToken ct) { DisconnectCore(); return Task.CompletedTask; }
    public Task<bool> IsConnectedAsync(CancellationToken ct) => Task.FromResult(_connected);
```

### 2. Connect is native Manager API only — pump fail retries live, not a demo book

`ConnectCore` initializes `SMTManagerAPIFactory`, creates `CIMTManagerAPI`, optionally applies HTTP proxy, then `_manager.Connect`. On pump failure it retries `PUMP_MODE_NONE`. Both attempts use the same live endpoint/login. There is no `_connected = true` short-circuit and no injected rows:

```60:112:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void ConnectCore()
    {
        lock (_gate)
        {
            if (_connected)
                return;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");
            // ...
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
```

### 3. Reads are Manager request/cache APIs; Ensure() fail-closed

Unconnected work throws. No dummy DTO list is substituted:

```436:440:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void Ensure()
    {
        if (_manager is null || !_connected)
            throw new InvalidOperationException($"{BrokerCode} is not connected. {LastError}");
    }
```

Groups: `GroupRequestArray("*")`, then `GroupTotal`/`GroupNext` if the array is empty. Accounts: `UserRequestArray` / `UserGetByGroup` / `UserLogins`+`UserRequestByLogins` plus `UserAccountRequestArray` / `UserAccountGetByGroup`. Deals: `DealRequest` / `DealRequestByGroup` over 14-day `Windows`. Positions: `PositionRequest` / `PositionRequestByGroup` / `PositionGetByGroup`.

Deal fetch fails closed on unexpected retcodes (no canned tape):

```273:293:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
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
```

`ReadDeals` / `ReadPositions` / `AddGroup` map fields from `CIMTDeal` / `CIMTPosition` / `CIMTConGroup` only. No literal tickets, symbols, or PnL.

### 4. Empty / zero is not a seeded book

Missing `CIMTAccount` snapshots become `0`, not demo balances (`10_000` / login `10001` live only in `FakeMt5BrokerConnector`):

```253:261:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
                rows.Add(new Mt5AccountDto(
                    (long)u.Login(),
                    u.Group(),
                    (int)u.Leverage(),
                    (decimal)(acc?.Balance() ?? 0),
                    (decimal)(acc?.Equity() ?? 0),
                    (decimal)(acc?.Margin() ?? 0),
                    (decimal)(acc?.MarginFree() ?? 0),
                    (decimal)(acc?.Profit() ?? 0)));
```

`GetPositionsCore` / `GetGroupPositionsCore` return `Array.Empty<Mt5PositionDto>()` on some non-OK retcodes — **empty, not seeded**. Silent-empty is a completeness risk, not a dummy book.

### 5. Live composition does not register this type as a fake fallback

`AddTraderIntelligence` refuses fake brokers, then only constructs natives via `LiveMt5Registration.CreateConnectors`:

```35:47:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

```20:46:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // Server/Port/Login/Password from config — values not printed
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

Live ingest catch path logs that dummy data will not be substituted (`LiveIngestHostedService`: `"{Broker} live ingest failed. No dummy data will be substituted."`). Seeded demo logins exist only on the non-live seeder path (`DemoSeeder` → `DemoBrokerFactory.CreateDefault()` → `FakeMt5BrokerConnector`). Those files do not inject into `NativeMt5BrokerConnector`.

This file does not contain:

- `FakeMt5BrokerConnector` / `DemoBrokerFactory` / `DemoSeeder`
- hardcoded logins, groups, XAU round-trips, or balance books
- `Random()`, `Guid` fixtures, or `NotImplemented` stubs that return sample DTOs
- a fallback from failed Manager I/O to any in-process demo book

## No-loss implication

On the live path this connector cannot publish canned Achiever/Starwave demo traders or fabricated deals. Worst case inside this type is throw (`Connect` / `DealRequest` fail-closed) or empty/zero rows (`PositionRequest` empty list; missing account snapshot `?? 0`). Those zeros/empties are not seeded PnL and do not open or close live positions — this class is Manager read/connect only, no order send. Dummy books cannot reach live ingest through `NativeMt5BrokerConnector`.

Empty-PASS justification: the assigned file was fully read (458/458 lines). Dummy/seeded live-path data is absent by construction, not by skipped review.
