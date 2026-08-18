# W500_SLICE_59 — CTraderFixLogonHostedService vs dotenv-before-CreateBuilder

- **slot:** 59
- **file:** `D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`
- **angle:** env file not loaded before `WebApplication.CreateBuilder`
- **read:** full file (96 lines) via `read_file`; `grep` on `D:/Prop/src/Fix.CTrader` for `WebApplication|CreateBuilder|EnvFile|DotNetEnv|LoadEnv` = **0 hits**; hosts grepped for `EnvFile.Load` / `CreateBuilder` / `CreateApplicationBuilder`
- **verdict:** **PASS** (empty PASS after full read — this type is not a host entrypoint)

## File

`D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` is a sealed `BackgroundService` in `TraderIntelligence.Fix.CTrader.Hosting`. It is constructed by DI with already-built `IConfiguration`. It never constructs `WebApplication`, never calls `WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder`, and never opens a dotenv / `.env` file.

## Angle

Assigned defect: a dotenv / `.env` file is not applied to the process environment **before** `WebApplication.CreateBuilder(args)`, so `IConfiguration` would miss `CTRADER_FIX_*` / `MT5_*` keys that exist only in a file.

That ordering can only fail (or succeed) in a **host `Program.cs`**. It cannot originate inside this worker type.

## Evidence quotes

Assigned type is a `BackgroundService`. First executable path is constructor + `ExecuteAsync` consuming injected `_config` — not a builder:

```11:34:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
public sealed class CTraderFixLogonHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IConfiguration _config;
    private readonly ILogger<CTraderFixLogonHostedService> _log;

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

If the password key is empty or still a `<SECRET>` placeholder, logon is skipped and the method returns. No TCP/TLS is opened. The only live I/O (when a non-placeholder password is already in `IConfiguration`) is `CTraderFixSession.TryLogonAsync` (FIX `35=A` Logon). The service itself states orders stay off:

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

Persist path updates existing `FixSessionState` rows only; it does not place orders:

```75:91:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
    private static async Task PersistAsync(object db, CTraderFixSessionResult quote, CTraderFixSessionResult trade, string host, CancellationToken ct)
    {
        if (db is not DbContext ctx)
            return;
        var set = ctx.Set<TraderIntelligence.Domain.Entities.FixSessionState>();
        foreach (var result in new[] { quote, trade })
        {
            var row = await set.FirstOrDefaultAsync(s => s.Qualifier == result.Qualifier, ct);
            if (row is null)
                continue;
            row.Host = host;
            row.Port = result.Qualifier == FixSessionQualifier.Quote ? 5211 : 5212;
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
```

`grep` of `D:/Prop/src/Fix.CTrader` for `WebApplication`, `CreateBuilder`, `EnvFile`, `DotNetEnv`, `LoadEnv` → **0 matches**. Registration is later, after a host already created its builder (`AddTraderIntelligence` → `services.AddHostedService<CTraderFixLogonHostedService>()`).

This file does not contain:

- `WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder`
- `EnvFile.FindAndLoad` / `EnvFile.Load` / DotNetEnv / `AddJsonFile(".env")`
- FIX `NewOrderSingle` / tag `35=D`
- order send, cancel/replace, or position sizing

### Out-of-file context (does **not** change this slice’s verdict)

`D:/Prop/apps/api/Program.cs` **does** load dotenv **before** `CreateBuilder` (measured this pass; older swarm notes that said “zero callers” are stale):

```9:12:D:/Prop/apps/api/Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

Workers still start with `Host.CreateApplicationBuilder(args)` and **do not** call `EnvFile` (`apps/mt5-worker/Program.cs` L6, `apps/fix-worker/Program.cs` L6). Those host boot-order questions belong to Program.cs slices, not this type.

## No-loss implication

A missing `.env` load before `WebApplication.CreateBuilder` cannot originate in this file and cannot make this service send size. When `CTRADER_FIX_PASSWORD` is absent from the already-built `IConfiguration` (or still contains `<SECRET>`), `ExecuteAsync` returns before any TCP/TLS. When a password *is* present from process env / user-secrets / launchSettings / a host that already called `EnvFile.FindAndLoad`, this type may attempt diagnostic Logon (`35=A`) only and still logs `NewOrderSingle still disabled`. Persist updates `FixSessionState` rows; it does not place orders. Slot 59 therefore has **no live capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (96/96 lines); the angle is absent by construction — this type is not the host entrypoint — not by skipped review.

## Verdict

**PASS**
