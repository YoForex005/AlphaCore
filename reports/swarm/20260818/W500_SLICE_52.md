# W500_SLICE_52

- **slot:** 52
- **file:** `D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (53/53 lines) via `read_file` (twice); grep `D:/Prop/src/Infrastructure/Mt5Live` for `manager|trader|login|GetUser|UserLogins|GetLogins|FetchAll|paginat` (hits only `using` + two `NativeMt5Options.Login` assignments); grep `D:/Prop/src` `*.cs` for `UserLogins|GetAccountsAsync|CreateConnectors`
- **verdict:** PASS

## Evidence quotes

Assigned type is a **static manager-connection factory**. After a full-file read it contains three members: `HasRealPasswords`, `CreateConnectors`, `IsSecret`. It does **not** talk to `CIMTManagerAPI`. It does **not** enumerate, page, or subset manager users.

`HasRealPasswords` is a dual-broker secret gate. It reads only the two password keys and never lists traders:

```10:15:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

`CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). Each `Login` is the **manager API credential** later passed to `_manager.Connect`, not a trader-universe filter and not a one-account ingest list:

```17:47:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
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
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
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

What this file does **not** contain (measured, 53/53 lines):

- `UserLogins` / `UserRequestArray` / `UserGetByGroup` / `UserRequestByLogins` / `UserAccountRequestArray`
- `GetAccountsAsync` / `GetGroupsAsync` / `GetDealsAsync`
- `Take(` / skip / page size / `fromLogin` / a hardcoded trader login list (`10001`, `2027` as the only ingested account, etc.)
- a group mask, plan allow-list, or `EnabledForAnalysis` filter that would shrink the manager-visible user set
- a third broker slot (Domain catalog is also two codes only: `BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx`)

Caller wiring registers **every** connector `CreateConnectors` returns and does not pass a group or login subset:

```35:47:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
```

The assigned angle **does** exist as a connector/ingest concern, but **not in this file**. Full-account fetch is `GetAccountsAsync(null)` on the connector:

```47:48:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`NativeMt5BrokerConnector.GetAccountsCore` (out of this slice) walks every group from `GetGroupsCore`, then `ReadAccountsForGroup` (`UserRequestArray` → `UserGetByGroup` → `UserLogins` + `UserRequestByLogins` if the user array is still empty). Completeness / empty-group / first-N holes belong there, not here.

`Login = 0` when `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN` fail `ulong.TryParse` is a **connect-fail** risk (`CIMTManagerAPI.Connect` with login 0). It is not a silent “ingest only the manager login as the only trader.” `HasRealPasswords` does not check that those logins parsed; that is still fail-closed at `ConnectAsync`, not a partial census.

Live ingest walks `registry.All()` (both factory slots) and scores `store.ListLoginsAsync` (whatever the connector previously upserted). This factory does not truncate that list.

Empty-PASS justification: the assigned file was fully read (53/53 lines); the angle (failure to fetch ALL manager traders/logins) is **absent by construction** — this type never enumerates manager users and never filters the ones a later connector walk would return.

## No-loss implication

`LiveMt5Registration` cannot omit, page-cut, or one-login-shrink the manager-visible trader set because it never enumerates users. It cannot place, flatten, size, or copy destination orders. Worst case inside this type is constructing the two specified manager connectors (or failing DI when passwords look dummy). Incomplete `UserLogins` / empty-group / first-N account holes belong to `NativeMt5BrokerConnector.ReadAccountsForGroup` and `DealIngestionService`, not this factory. Slot 52 therefore has **no capital-loss path** and **no factory-side “fetch-all traders/logins” defect**.
