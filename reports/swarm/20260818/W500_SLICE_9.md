# W500_SLICE_9

- **slot:** 9
- **file:** `D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`
- **angle:** env file not loaded before `WebApplication.CreateBuilder`
- **read:** full file (96 lines) via `read_file`; also grepped `Fix.CTrader` for `WebApplication|CreateBuilder|EnvFile|NewOrderSingle|35=D` and hosts for `EnvFile.Load` / `CreateBuilder`
- **verdict:** PASS

## Evidence quotes

Assigned type is a `BackgroundService`. It never constructs a host, never calls `WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder`, and never calls `EnvFile.Load`. The angle (dotenv must be applied **before** `CreateBuilder` snapshots environment into `IConfiguration`) cannot occur *inside this file*.

It only *consumes* injected `IConfiguration` after the host already built:

```17:34:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
    public CTraderFixLogonHostedService(
        IServiceScopeFactory scopes,
        IConfiguration config,
        ILogger<CTraderFixLogonHostedService> log)
    {
        _scopes = scopes;
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }
```

If the password slot is empty or still a `<SECRET>` placeholder, logon is skipped and the method returns. Host/account/sender then have non-secret defaults; the only live I/O is `CTraderFixSession.TryLogonAsync` (FIX `35=A` Logon), and the service itself states orders stay off:

```41:54:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            sender, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            sender, password, stoppingToken);

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

Grep of `D:/Prop/src/Fix.CTrader` for `WebApplication`, `CreateBuilder`, `EnvFile`: **no hits in this file**; only `NewOrderSingle` is the log string above. Registration is later, after the host already created its builder (`AddTraderIntelligence` → `services.AddHostedService<CTraderFixLogonHostedService>()`).

Out-of-file context (does **not** change this slice’s verdict): `apps/api/Program.cs` line 7 is `var builder = WebApplication.CreateBuilder(args);` with no preceding `EnvFile.Load`. `EnvFile.Load` (`D:/Prop/src/Mt5/Env/EnvFile.cs`) has **zero callers** in product C#. That host boot-order question belongs to the API/worker `Program.cs` slices, not this type.

This file does not contain:

- `WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder`
- `EnvFile.Load` / dotenv / `AddJsonFile(".env")`
- FIX `NewOrderSingle` / tag `35=D`
- order send, cancel/replace, or position sizing

## No-loss implication

A missing `.env` load before `CreateBuilder` cannot make this service send size. When `CTRADER_FIX_PASSWORD` is absent from the already-built `IConfiguration` (the usual case if dotenv was never applied to process env), `ExecuteAsync` returns before any TCP/TLS. When a password *is* present from process env / user-secrets / launchSettings, this type may attempt diagnostic Logon (`35=A`) only and still logs `NewOrderSingle still disabled`. Persist path updates `FixSessionState` rows; it does not place orders. Slot 9 therefore has **no live capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (env not loaded before `WebApplication.CreateBuilder`) is absent by construction — this type is not the host entrypoint — not by skipped review.
