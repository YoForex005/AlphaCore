# W500_SLICE_0

- **slot:** 0
- **file:** `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (329 lines) via `read_file`; grep on this file for dummy/seed/fake/mock/stub/placeholder/hardcoded/sample/synthetic/Random/TODO/NotImplemented returned no market-data hits (only proxy `auth` line 90, unrelated)
- **verdict:** PASS

## Evidence quotes

`NativeMt5BrokerConnector` is a live Manager-API collector. Construction takes `NativeMt5Options` (broker/server/port/login/password/proxy). There is no in-memory book, no `DemoBrokerFactory`, and no `FakeMt5BrokerConnector` reference.

Connect talks to the native factory and `_manager.Connect`; it does not short-circuit `_connected = true` or inject canned rows:

```55:109:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
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
            if (res != MTRetCode.MT_RET_OK)
            {
                LastError = $"Connect {BrokerCode} {endpoint} failed: {res}";
                _connected = false;
                throw new InvalidOperationException(LastError);
            }

            _connected = true;
            LastError = null;
        }
    }
```

Reads are Manager API only (`GroupTotal`/`GroupNext`, `UserGetByGroup`/`UserAccountGetByGroup`, `DealRequest`/`DealRequestByGroup`, `PositionRequest`). `Ensure()` refuses work when not connected:

```322:326:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void Ensure()
    {
        if (_manager is null || !_connected)
            throw new InvalidOperationException($"{BrokerCode} is not connected. {LastError}");
    }
```

Deal fetch fails closed on unexpected retcodes (no canned deals). `MT_RET_OK_NONE` / `MT_RET_ERR_NOTFOUND` maps to whatever the native array holds (normally empty), then `ReadDeals`:

```223:237:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetDealsCore(long login, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.DealCreateArray();
            try
            {
                var res = _manager.DealRequest((ulong)login, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    throw new InvalidOperationException($"{BrokerCode} DealRequest {login} failed: {res}");
                return ReadDeals(arr);
            }
            finally { arr.Release(); }
        }
    }
```

Missing `CIMTAccount` fields become `0`, not demo balances (`10001` / `10_000` live in `FakeMt5BrokerConnector` only):

```201:209:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
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

`GetPositionsCore` returns `Array.Empty<Mt5PositionDto>()` on some non-OK retcodes — empty, not seeded. That is a silent-empty risk, not dummy books.

This type is what the live host actually registers. `AddTraderIntelligence` refuses fake brokers, then only constructs natives:

```33:37:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

```20:46:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

Seeded demo logins (`10001`, `10002`, `10003`, `99001`) exist only on the non-live seeder path (`DemoSeeder` → `DemoBrokerFactory.CreateDefault()` → `FakeMt5BrokerConnector`). Live ingest logs that it will not substitute dummy data on connector failure (`LiveIngestHostedService` catch). Those files are out of this slice’s type; they do not inject into `NativeMt5BrokerConnector`.

This file does not contain:

- `FakeMt5BrokerConnector` / `DemoBrokerFactory` / `DemoSeeder`
- hardcoded logins, groups, XAU round-trips, or balance books
- `Random()`, `Guid` fixtures, or `NotImplemented` stubs that return sample DTOs
- a fallback from failed Manager I/O to any in-process demo book

## No-loss implication

On the live path this connector cannot publish canned Achiever/Starwave demo traders or fabricated deals. Worst case inside this type is throw (`Connect`/`DealRequest` fail-closed) or empty/zero rows (`PositionRequest` empty list; missing account snapshot `?? 0`). Those zeros/empties are not seeded PnL and do not open or close live positions — this class is Manager read/connect only, no order send. Dummy books cannot reach live ingest through `NativeMt5BrokerConnector`.

Empty-PASS justification: the assigned file was fully read (329 lines); dummy/seeded live-path data is absent by construction, not by skipped review.
