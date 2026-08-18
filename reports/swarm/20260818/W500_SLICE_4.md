# W500_SLICE_4

- **slot:** 4
- **file:** `D:/Prop/apps/api/Program.cs`
- **angle:** Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012
- **read:** full file (96 lines) via `read_file`; grep on `D:/Prop/apps/api` for `IPBLOCK|1012|Achiever|MT_RET_AUTH|proxy|AUTH_MANAGER` → only `ACHIEVER` paint on L46
- **verdict:** FAIL

## Evidence quotes

`Program.cs` is the API host (96 lines). It never loads `.env`, never calls `EnvFile.Load`, never binds `ACHIEVER_PROXY_*`, never maps Manager retcode **1012**, and never probes `IMt5BrokerConnector.IsConnectedAsync` / `LastError`. `/api/health` **hard-codes** Achiever as healthy via a demo fake that DI no longer uses.

```7:9:D:/Prop/apps/api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTraderIntelligence(builder.Configuration);
```

```25:33:D:/Prop/apps/api/Program.cs
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
```

```42:47:D:/Prop/apps/api/Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

```73:81:D:/Prop/apps/api/Program.cs
app.MapPost("/api/ops/resync", async (DealIngestionService ingestion, ReconstructionScoringService scoring, CancellationToken ct) =>
{
    var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var to = DateTimeOffset.UtcNow;
    var a = await ingestion.SyncBrokerAsync("ACHIEVER", from, to, ct);
    var s = await ingestion.SyncBrokerAsync("STARWAVEFX", from, to, ct);
    foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        await scoring.RebuildTraderAsync(login >= 99000 ? "STARWAVEFX" : "ACHIEVER", login, ct);
    return Results.Ok(new { achieverDeals = a, starwaveDeals = s });
});
```

This file does **not** contain:

- `ACHIEVER_PROXY_ENABLED` / `ACHIEVER_PROXY_HOST` / `ACHIEVER_PROXY_PORT` / `ProxySet`
- `1012` / `MT_RET_AUTH_MANAGER_IPBLOCK` / `IPBLOCK` / allow-list `81.29.145.69`
- `EnvFile.Load` (repo loader exists at `D:/Prop/src/Mt5/Env/EnvFile.cs`; **zero callers** from this host — confirmed by prior R030/R031 and this pass)
- any mapping of Manager connect failure → operator “need HTTP proxy” text

`apps/api` grep for `proxy|1012|IPBLOCK|ACHIEVER_PROXY|MT5_PROXY` is empty. Launch profile (`apps/api/Properties/launchSettings.json`) sets only `ASPNETCORE_ENVIRONMENT=Development`. `appsettings.json` / `appsettings.Development.json` have no Achiever proxy stanza.

DI (not this file, but invoked on L9) now **requires** real MT5 passwords and registers `NativeMt5BrokerConnector` via `LiveMt5Registration` (`src/Infrastructure/DependencyInjection.cs` L33–37). That registration **does** bind Achiever HTTP proxy from `ACHIEVER_PROXY_*` and `NativeMt5BrokerConnector` `ProxySet`s `PROXY_HTTP` when enabled. `LiveIngestHostedService` then `ConnectAsync`s every registered broker ~2s after start. Those keys never enter `IConfiguration` from this host unless already in the process environment — `WebApplication.CreateBuilder` does not load `D:\Prop\.env`.

SDK mapping (not this file; the 1012 contract this host should surface and does not):

```61:64:D:/Prop/mt5-sdk/src/core/mt5_manager.cpp
static std::string mt5ErrorReason(MTAPIRES code) {
    switch (code) {
        case 7:    return "Network timeout (MT_RET_ERR_NETWORK). MT5 server unreachable - check proxy/firewall and MT5 server IP whitelist.";
        case 1012: return "IP blocked by MT5 server (MT_RET_AUTH_MANAGER_IPBLOCK). Ask MT5 server admin to whitelist this machine's IP.";
```

```46:46:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIConstants.h
   MT_RET_AUTH_MANAGER_IPBLOCK  =1012,    // IP address unallowed for manager
```

R012 pin (this workstation): public egress `106.219.132.213` ≠ allow-list `81.29.145.69`; Achiever Manager TCP `:443` is reachable; without HTTP `ProxySet` to `81.29.145.69:49527`, local Manager connect historically returns **1012**. StarwaveFX does not need this proxy.

Honesty mismatch: `/api/health` still paints `healthy = true` + `demo FakeMt5BrokerConnector — not live Manager` while L9 DI refuses fakes. A 1012 on live ingest is invisible on this endpoint (literal `true`, no `LastError`). `/ready` is `ready = true` after a broker-row count only.

## No-loss implication

`Program.cs` has **no order-send / NewOrderSingle / Manager DealRequest path**. `/api/settings` hard-codes `REAL_COPY_EXECUTION_ENABLED = false`. A 1012 is fail-closed: no Achiever Manager session ⇒ no live deal pump from this host.

Residual risk is **operator-honesty / live-ingest attach**, not a direct capital debit:

1. False `ACHIEVER healthy=true` hides IP-block. Operators can treat the API as Achiever-connected when the process either never applied HTTP `ProxySet` or already failed 1012.
2. `POST /api/ops/resync` and `LiveIngestHostedService` (registered by L9) will `SyncBrokerAsync("ACHIEVER")` / `ConnectAsync` on the native connector. On this desktop, missing `ACHIEVER_PROXY_*` in process env ⇒ **1012**. That fails closed; it does not place trades.
3. If passwords are in the process env but proxy is off, Connect from `106.219.132.213` is the known 1012 path. If both password and proxy env are absent, `HasRealPasswords` throws and the host does not start (also fail-closed).

Slot 4 is **not** an empty PASS: the assigned file was fully read; the angle applies (this host advertises Achiever health and can trigger Achiever sync); proxy/1012 handling is **absent** and health **lies**.

Product source not modified. Secrets not printed.
