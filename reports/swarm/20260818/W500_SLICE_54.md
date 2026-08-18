# W500_SLICE_54 — API `Program.cs` vs Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012

| Field | Value |
|---|---|
| Slot | 54 |
| File | `D:/Prop/apps/api/Program.cs` |
| Angle | Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012 |
| Date | 2026-08-18 |
| Method | Full `read_file` of assigned file (156 lines) + `grep` on `apps/api` and supporting live-path files (`LiveMt5Registration`, `NativeMt5BrokerConnector`, `LiveIngestHostedService`, `DealIngestionService`, `BrokerCatalogSeed`, `EnvFile`). Product source not edited. Secrets / proxy auth / FIX passwords not printed. |
| Product source modified | **No** |
| Verdict | **FAIL** |

---

## 1. What was read

`D:/Prop/apps/api/Program.cs` is the API composition root (156 lines). Entire surface that matters for this angle:

- `EnvFile.FindAndLoad()` then `WebApplication.CreateBuilder` then `AddEnvironmentVariables()` then `AddTraderIntelligence`.
- Hardcoded JSON endpoints: `/health`, `/api/health`, `/api/ingest/status`, `/api/settings`, `/ready`, `/api/ops/resync`.
- Startup: `EnsureCreated` + `BrokerCatalogSeed.EnsureAsync` (no `DemoSeeder`).
- **Zero** tokens in this file: `1012`, `IPBLOCK`, `MT_RET_AUTH_MANAGER_IPBLOCK`, `ACHIEVER_PROXY`, `ProxySet`, `PROXY_HTTP`, `ProxyEnabled`.
- `grep` of `D:/Prop/apps/api` for those tokens: **no matches**.

This is not an empty PASS. The file is the host that loads env (the only way `ACHIEVER_PROXY_*` reach DI), starts live Achiever ingest, and advertises Achiever health/ready/resync. Proxy / 1012 apply here.

---

## 2. Evidence quotes

### 2.1 Env is loaded, but this file never requires the Achiever HTTP proxy

```9:14:D:/Prop/apps/api/Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`AddTraderIntelligence` registers `NativeMt5BrokerConnector` for `ACHIEVER` and `LiveIngestHostedService` (which calls `ConnectAsync`). Live proxy flags are **not** read in `Program.cs`. They are optional env keys in `LiveMt5Registration`:

```20:32:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
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
```

`ProxyEnabled` is true **only** when `ACHIEVER_PROXY_ENABLED` parses as `true`. Missing / false / typo → `ApplyProxy()` returns immediately and Manager connects **direct**. On this LAN that is the measured 1012 path (R012: desktop egress ≠ allow-list `81.29.145.69`; HTTP hop `81.29.145.69:49527`). `Program.cs` does not fail startup when the Achiever proxy keys are absent.

Native mapping of 1012 (not in this file; quoted to bind the angle):

```115:129:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void ApplyProxy()
    {
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;
        // PROXY_HTTP address=host:port; auth not quoted here
        var set = _manager.ProxySet(proxy);
        if (set != MTRetCode.MT_RET_OK)
            throw new InvalidOperationException(Describe(set, $"{BrokerCode} ProxySet"));
    }
```

```443:454:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            // ...
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
```

### 2.2 `/api/health` can surface 1012; `/ready` and settings cannot

Health is runtime-backed (good vs the old FakeMt5 `healthy=true` paint):

```32:56:D:/Prop/apps/api/Program.cs
app.MapGet("/api/health", (LiveRuntimeStatus runtime) =>
{
    var brokers = runtime.Brokers.Values.Select(b => new
    {
        name = b.BrokerCode,
        healthy = b.Connected,
        lastCheck = b.UpdatedAt,
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
    }).ToArray();
    return Results.Ok(new
    {
        mt5Connections = brokers,
        // ...
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
    });
});
```

`LiveIngestHostedService` on connect throw sets `Connected=false` and `LastError = ex.Type + ": " + ex.Message`, so a 1012 `InvalidOperationException` **can** appear in `/api/health` details after the hosted ingest has run.

`/ready` does **not** read `Connected` or `LastError`. After `BrokerCatalogSeed`, Achiever catalog rows exist, so `ready=true` even when Manager is 1012-blocked:

```84:90:D:/Prop/apps/api/Program.cs
app.MapGet("/ready", async (TraderDbContext db, CancellationToken ct) =>
{
    var brokers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Brokers, ct);
    var groups = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Mt5Groups, ct);
    var accounts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Mt5Accounts, ct);
    return Results.Ok(new { ready = true, brokers, groups, accounts });
});
```

Settings hard-enable Achiever with no proxy / 1012 gate:

```70:83:D:/Prop/apps/api/Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
    brokerConfigs = new[]
    {
        new { id = "ACHIEVER", name = "Achiever", enabled = true },
        new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true }
    }
}));
```

### 2.3 Manual resync hits live Achiever `Connect` with no 1012 / proxy handling

```111:147:D:/Prop/apps/api/Program.cs
app.MapPost("/api/ops/resync", async (
    DealIngestionService ingestion,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    var from = DateTimeOffset.UtcNow.AddDays(-90);
    var to = DateTimeOffset.UtcNow.AddMinutes(1);
    var result = new Dictionary<string, object>();
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        var status = runtime.Broker(code);
        status.Phase = "manual-resync";
        status.UpdatedAt = DateTimeOffset.UtcNow;
        var catalog = await ingestion.SyncCatalogAsync(code, ct);
        // ... SyncBrokerAsync + RebuildTraderAsync — no try/catch, no proxy check
        status.Phase = "manual-done";
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
    }
    return Results.Ok(result);
});
```

`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` both call `connector.ConnectAsync` first. On 1012 this handler:

- does **not** set `status.Connected = false` or `status.LastError`,
- leaves `Phase = "manual-resync"`,
- returns HTTP 500.

Health can then show a stale Connected/LastError from the last ingest host pass, while `/ready` stays `ready=true`.

Startup seed paints Achiever proxy metadata into the catalog **only**. `Program.cs` never copies those columns into `NativeMt5Options`:

```149:154:D:/Prop/apps/api/Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

(`BrokerCatalogSeed` Achiever row: `ProxyEnabled=true`, host `81.29.145.69`, port `49527`. Live `ProxySet` uses env via `LiveMt5Registration`, not this row.)

---

## 3. No-loss implication

`MT_RET_AUTH_MANAGER_IPBLOCK` (1012) is a **Manager logon deny**, not a fill. This file has:

- no FIX `NewOrderSingle` / no order router,
- `FEATURE_COPY_TRADING_ENABLED = false` (literal),
- `REAL_COPY_EXECUTION_ENABLED` taken from `LiveRuntimeStatus` (DI sets it only when config is the string `true`),
- recon note: `"recon runs only after FIX TRADE logon; NewOrderSingle still off"`.

Native connect **throws** on 1012; `LiveIngestHostedService` catch says dummy data will not be substituted. A 1012 therefore **cannot open, close, or resize destination risk from this process**.

Residual harm is **operational honesty**, not capital: `/ready` + `brokerConfigs.Achiever.enabled=true` + un-gated `/api/ops/resync` can look “up” while Achiever is IP-blocked, and missing `ACHIEVER_PROXY_ENABLED` on this LAN is the known 1012 trigger. That can pollute scores/catalog after a later successful hop, but it does not send size.

**Risk to capital:** none from 1012 in this host (fail-closed connect; copy flags off). Do not treat `/ready` as “Achiever Manager live.”

---

## 4. Verdict rationale

**FAIL** — composition root loads env and starts live Achiever ingest / resync but:

1. never requires `ACHIEVER_PROXY_ENABLED` + host/port before `AddTraderIntelligence` / `ConnectAsync` (direct path → 1012 on this LAN);
2. `/ready` stays `ready=true` from catalog counts after seed, independent of 1012;
3. `/api/ops/resync` calls live Achiever `Connect` with no proxy check and no `LastError` write on 1012;
4. catalog seed proxy is unused by this host’s live `ProxySet`.

`/api/health` LastError path is the only 1012-aware surface in this file, and it is populated by the hosted ingest catch, not by `Program.cs` itself.

---

## 5. Residual (not this slot’s job)

- Enforce Achiever proxy at composition root (or refuse live Achiever registration).
- `/ready` must AND `runtime.Broker("ACHIEVER").Connected` (and not lie after 1012).
- Resync: try/catch → `Connected=false`, persist 1012 text, HTTP 503 — do not leave `Phase=manual-resync`.
- Do not print or log proxy auth / Manager / FIX passwords.
