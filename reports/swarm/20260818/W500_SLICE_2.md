# W500_SLICE_2

- **slot:** 2
- **file:** `D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (53 lines) via `read_file`; grep on this file for `UserLogins|GetAccounts|Take\(|login` returned **no matches**
- **verdict:** PASS

## Evidence quotes

`LiveMt5Registration` is manager-connection factory only. It never calls Manager API user/login enumeration (`UserLogins`, `UserRequestArray`, `UserGetByGroup`, `UserRequestByLogins`). It never implements `GetAccountsAsync`. It never takes a subset of logins.

`HasRealPasswords` is a dual-broker secret gate (both Achiever and StarwaveFX must look like real secrets). It does not list traders:

```10:15:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

`CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances. Each `Login` is the **manager API credential** used later by `CIMTManagerAPI.Connect`, not a trader-universe filter and not a one-account ingest list:

```17:46:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_PORT"], out var ap) ? ap : 443,
            Login = ulong.TryParse(config["MT5_LOGIN"], out var al) ? al : 0,
            Password = config["MT5_PASSWORD"] ?? "",
            // proxy fields omitted in this quote
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            Server = config["MT5_STARWAVEFX_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_STARWAVEFX_PORT"], out var sp) ? sp : 443,
            Login = ulong.TryParse(config["MT5_STARWAVEFX_LOGIN"], out var sl) ? sl : 0,
            Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

This file does not contain:

- `UserLogins` / `UserRequestArray` / `UserGetByGroup` / `UserRequestByLogins`
- `GetAccountsAsync` / `GetGroupsAsync` / any group mask besides what the connector later chooses
- `Take(` / page size / `fromLogin` / a hardcoded trader login list
- a filter that would keep only `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN` as the ingested accounts

Caller wiring (`Infrastructure/DependencyInjection.cs`) registers every connector `CreateConnectors` returns and does not pass a group or login subset:

```33:39:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

The assigned angle **does** exist as a connector/ingest concern, but **not in this file**. `DealIngestionService.SyncBrokerAsync` asks for the full account set with `group: null`. `NativeMt5BrokerConnector.GetAccountsCore` walks every group from `GetGroupsCore` and `ReadAccountsForGroup` (network `UserRequestArray`, cache `UserGetByGroup`, then `UserLogins` + `UserRequestByLogins` if the user array is still empty). That completeness logic is **out of this slice’s file**.

`Login = 0` when `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN` fail to parse is a **connect-fail** risk (`CIMTManagerAPI.Connect` with login 0), not a silent “ingest only the manager login as the only trader.”

## No-loss implication

`LiveMt5Registration` cannot omit, page-cut, or one-login-shrink the manager-visible trader set because it never enumerates users. It cannot place, flatten, or copy live orders. Worst case inside this type is constructing two manager connectors (or failing DI when passwords look dummy). Incomplete `UserLogins` / empty-group / first-N account holes belong to `NativeMt5BrokerConnector` and `DealIngestionService`, not this factory.

Empty-PASS justification: the assigned file was fully read (53/53 lines); the angle (failure to fetch ALL manager traders/logins) is absent by construction, not by skipped review.
