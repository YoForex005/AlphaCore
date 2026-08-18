# W500_SLICE_27

- **slot:** 27
- **file:** `D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (54 lines) via `read_file`; grep on this file for `200|position|account|limit|first` returned **no matches**
- **verdict:** PASS

## Evidence quotes

`LiveMt5Registration` is a static factory + secret-presence gate. It does not enumerate broker logins, does not call `GetPositionsAsync`, and does not apply `Take(200)` or any other account cap.

`HasRealPasswords` only checks that two configuration keys are non-placeholder secrets. It does not list accounts or positions:

```10:15:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }
```

`CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFx) from configuration and returns that two-element array. There is no account iteration and no position snapshot:

```17:47:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // server / port / manager login / proxy flags from IConfiguration
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // server / port / manager login from IConfiguration; ProxyEnabled = false
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

`IsSecret` is a placeholder filter only (`<SECRET>` / `(a/c`); no positions, no paging:

```49:52:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);
```

This file does not contain:

- `Take(200)` / `Skip` / page size / `limit`
- `GetPositionsAsync` / `ReplacePositionsAsync` / `Mt5Position`
- any `foreach` over accounts or groups
- any numeric `200` literal

The first-200-accounts position cap lives **outside** this slice, in ingestion (not registration):

```74:77:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        foreach (var account in accounts.Take(200))
        {
            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
```

That `Take(200)` is out of this slice’s file. Slot 27 asked whether **this** registration type limits position sync to the first 200 accounts. It does not.

## No-loss implication

`CreateConnectors` cannot drop, truncate, or skip a position book. It only constructs two manager-side connectors. It cannot send orders, cannot size positions, and cannot leave accounts 201+ with a stale local book — because it never reads or writes positions. The live completeness risk of `accounts.Take(200)` (missed position replace for logins beyond 200) is owned by `DealIngestionService`, not by `LiveMt5Registration`. Slot 27 therefore has **no position-cap capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (54/54 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
