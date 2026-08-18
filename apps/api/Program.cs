using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Infrastructure;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

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

app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow, details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
app.MapGet("/api/risk/status", (IDashboardQueries q, CancellationToken ct) => q.GetRiskAsync(ct));
app.MapGet("/api/reconciliation/status", () => Results.Ok(new
{
    lastReconciliation = DateTimeOffset.UtcNow,
    unknownPositions = 0,
    mismatches = 0,
    orphanFills = 0
}));
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
app.MapGet("/ready", async (TraderDbContext db, CancellationToken ct) =>
{
    var brokers = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(db.Brokers, ct);
    return Results.Ok(new { ready = true, brokers });
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
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>(),
        CancellationToken.None);
}

app.Run();
