using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Application.Runtime;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Copy;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5.Env;

var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

app.UseCors();
if (app.Environment.IsDevelopment())
    app.UseSwagger();

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow, envLoaded = loadedEnv is not null }));

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
        fixSessions = new[]
        {
            new { name = "QUOTE", healthy = runtime.Quote.LoggedOn, lastCheck = runtime.Quote.UpdatedAt, details = runtime.Quote.LastError ?? runtime.Quote.Status },
            new { name = "TRADE", healthy = runtime.Trade.LoggedOn, lastCheck = runtime.Trade.UpdatedAt, details = runtime.Trade.LastError ?? runtime.Trade.Status }
        },
        database = new { name = "in-memory-or-postgres", healthy = true, lastCheck = DateTimeOffset.UtcNow },
        redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "cache only; not required" },
        outboxBacklog = 0,
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
    });
});

app.MapGet("/api/ingest/status", (LiveRuntimeStatus runtime) => Results.Ok(runtime.Snapshot()));

app.MapGet("/api/risk/status", (IDashboardQueries q, CancellationToken ct) => q.GetRiskAsync(ct));
app.MapGet("/api/reconciliation/status", () => Results.Ok(new
{
    lastReconciliation = DateTimeOffset.UtcNow,
    unknownPositions = 0,
    mismatches = 0,
    orphanFills = 0,
    note = "recon runs only after FIX TRADE logon; NewOrderSingle still off"
}));
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
    brokerConfigs = new[]
    {
        new { id = "ACHIEVER", name = "Achiever", enabled = true },
        new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true }
    }
}));
app.MapGet("/ready", async (TraderDbContext db, CancellationToken ct) =>
{
    var brokers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Brokers, ct);
    var groups = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Mt5Groups, ct);
    var accounts = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Mt5Accounts, ct);
    return Results.Ok(new { ready = true, brokers, groups, accounts });
});

app.MapGet("/api/overview", (IDashboardQueries q, CancellationToken ct) => q.GetOverviewAsync(ct));
app.MapGet("/api/brokers", (IDashboardQueries q, CancellationToken ct) => q.GetBrokersAsync(ct));
app.MapGet("/api/groups", (IDashboardQueries q, CancellationToken ct) => q.GetGroupsAsync(ct));
app.MapGet("/api/traders", (IDashboardQueries q, string? broker, string? state, CancellationToken ct) =>
    q.GetTradersAsync(broker, state, ct));
app.MapGet("/api/traders/{broker}/{login:long}", (IDashboardQueries q, string broker, long login, CancellationToken ct) =>
    q.GetTraderDetailAsync(broker, login, ct));
app.MapGet("/api/fix/sessions", (IDashboardQueries q, CancellationToken ct) => q.GetFixSessionsAsync(ct));
app.MapGet("/api/risk", (IDashboardQueries q, CancellationToken ct) => q.GetRiskAsync(ct));
app.MapGet("/api/copy/status", (CopyTradingService copy, CancellationToken ct) => copy.GetStatusAsync(ct));
app.MapGet("/api/copy/intents", (CopyTradingService copy, CancellationToken ct) => copy.ListIntentsAsync(200, ct));
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});

app.MapPost("/api/ops/score/{broker}", async (
    string broker,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    var code = broker.Trim().ToUpperInvariant();
    if (code is not ("ACHIEVER" or "STARWAVEFX"))
        return Results.BadRequest(new { error = "broker must be ACHIEVER or STARWAVEFX" });
    var status = runtime.Broker(code);
    status.Phase = "manual-score";
    var brokerId = await store.ResolveBrokerIdAsync(code, ct);
    var logins = await store.ListLoginsWithDealsAsync(brokerId, ct);
    var scored = 0;
    foreach (var login in logins)
    {
        ct.ThrowIfCancellationRequested();
        await scoring.RebuildTraderAsync(code, login, ct);
        scored++;
        if (scored % 25 == 0)
        {
            status.Scored = scored;
            status.UpdatedAt = DateTimeOffset.UtcNow;
        }
    }
    status.Scored = scored;
    status.Phase = "score-done";
    status.UpdatedAt = DateTimeOffset.UtcNow;
    return Results.Ok(new { broker = code, logins = logins.Count, scored });
});

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
        status.Groups = catalog.Groups;
        status.Accounts = catalog.Accounts;
        var deals = await ingestion.SyncBrokerAsync(code, from, to, ct);
        var brokerId = await store.ResolveBrokerIdAsync(code, ct);
        var logins = await store.ListLoginsAsync(brokerId, ct);
        var scored = 0;
        foreach (var login in logins)
        {
            await scoring.RebuildTraderAsync(code, login, ct);
            scored++;
        }

        status.DealsInserted = deals;
        status.Scored = scored;
        status.Phase = "manual-done";
        status.UpdatedAt = DateTimeOffset.UtcNow;
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
    }

    return Results.Ok(result);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
