using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Risk;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Domain.Shadow;
using TraderIntelligence.Infrastructure.Dashboard;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5.Connectors;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
Console.WriteLine("D48_SHADOW_ROWS_EVAL");
Console.WriteLine("mode=reconstruct+score+SimulateEntry (no EF; Infrastructure does not compile)");

var recon = new TradeReconstructor();
var scorer = new BaselineScorer();
var engine = new ShadowCopyEngine();
var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var to = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
var achieverId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
var starwaveId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
var now = DateTimeOffset.UtcNow;
var quote = new DestinationQuote("XAUUSD", null, 2399.45m, 2399.85m, now, null);
var delay = TimeSpan.FromMilliseconds(80);

NormalizedDeal ToN(string code, Mt5DealDto d) => new()
{
    BrokerId = code,
    Login = d.Login,
    DealTicket = d.DealTicket,
    OrderTicket = d.OrderTicket,
    PositionId = d.PositionId,
    SourceSymbol = d.Symbol,
    Action = d.Action,
    Entry = d.Entry,
    VolumeNative = d.VolumeNative,
    Price = d.Price,
    Profit = d.Profit,
    Commission = d.Commission,
    Swap = d.Swap,
    Time = d.Time,
    Comment = d.Comment
};

var expectedRows = 0;
var expectedIntents = 0;
decimal slipSum = 0;
foreach (var (code, connector, brokerId, login) in new (string, FakeMt5BrokerConnector, Guid, long)[]
{
    ("ACHIEVER", achiever, achieverId, 10001),
    ("ACHIEVER", achiever, achieverId, 10002),
    ("ACHIEVER", achiever, achieverId, 10003),
    ("STARWAVEFX", starwave, starwaveId, 99001)
})
{
    var deals = (await connector.GetDealsAsync(login, from, to, CancellationToken.None))
        .Select(d => ToN(code, d))
        .ToList();
    var trades = recon.Reconstruct(code, login, deals);
    var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
    var score = scorer.Score(completedXau);
    Console.WriteLine($"TRADER login={login} broker={code} deals={deals.Count} recon={trades.Count} completedXau={completedXau.Count} state={score.SuggestedState} q={score.EarlyQualityScore} r={score.RiskScore} b={score.BehaviorScore}");

    if (score.SuggestedState != TraderState.SHADOW)
    {
        Console.WriteLine($"  SKIP_SHADOW_ORDERS reason=state_not_SHADOW");
        continue;
    }

    foreach (var trade in completedXau.Where(t => t.Completed).OrderBy(t => t.ClosedAt))
    {
        var key = $"shadow:{brokerId}:{login}:{trade.PositionId}";
        var fill = engine.SimulateEntry(
            Guid.Empty.ToString(),
            trade.Direction,
            trade.MaxVolumeLots,
            trade.EntryVwap,
            quote,
            now,
            delay);
        expectedRows++;
        expectedIntents++;
        slipSum += fill.SourceVsShadowSlippage;
        Console.WriteLine(
            $"  ROW login={login} pos={trade.PositionId} dir={trade.Direction} qty={fill.Quantity} entryVwap={trade.EntryVwap} px={fill.Price} spread={fill.Spread} slip={fill.SourceVsShadowSlippage} key={key} expires={trade.OpenedAt.AddSeconds(15):o} opened={trade.OpenedAt:o}");
    }
}

Console.WriteLine($"EXPECTED_SHADOW_ORDERS={expectedRows}");
Console.WriteLine($"EXPECTED_COPY_INTENTS={expectedIntents}");
Console.WriteLine($"EXPECTED_SLIP_SUM={slipSum}");
Console.WriteLine($"QUOTE bid={quote.Bid} ask={quote.Ask} spread={quote.Ask - quote.Bid} delayMs={delay.TotalMilliseconds} overlay={(delay > TimeSpan.FromMilliseconds(250) ? "YES" : "NO")}");

var options = new DbContextOptionsBuilder<TraderDbContext>()
    .UseInMemoryDatabase("d48-shadow-" + Guid.NewGuid())
    .Options;
await using var db = new TraderDbContext(options);
var store = new EfTradingStore(db);
var scoring = new ReconstructionScoringService(store, recon, scorer);
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);

void DumpSeed(string tag)
{
    Console.WriteLine($"{tag}_SHADOW_ORDERS={db.ShadowOrders.Count()}");
    Console.WriteLine($"{tag}_COPY_INTENTS={db.CopyIntents.Count()}");
    Console.WriteLine($"{tag}_OUTBOX={db.OutboxEvents.Count()}");
    Console.WriteLine($"{tag}_DEST_QUOTES={db.DestinationQuotes.Count()}");
    Console.WriteLine($"{tag}_SCORES=" + string.Join(";", db.TraderScores.OrderBy(s => s.Login).Select(s => $"{s.Login}:{s.CurrentState}:N={s.CompletedXauTrades}")));
    foreach (var row in db.ShadowOrders.OrderBy(s => s.SourceLogin).ThenBy(s => s.CopyIntentId))
        Console.WriteLine($"{tag}_ROW login={row.SourceLogin} dir={row.Direction} qty={row.Quantity} px={row.Price} spread={row.Spread} slip={row.SourceVsShadowSlippage} intent={row.CopyIntentId} broker={row.BrokerId}");
    foreach (var intent in db.CopyIntents.OrderBy(c => c.SourceLogin).ThenBy(c => c.IdempotencyKey))
        Console.WriteLine($"{tag}_INTENT login={intent.SourceLogin} status={intent.Status} action={intent.Action} qty={intent.RequestedQuantity} px={intent.ExpectedPrice} key={intent.IdempotencyKey} exp={intent.ExpiresAt:o}");
    foreach (var ev in db.OutboxEvents.OrderBy(e => e.OccurredAt))
        Console.WriteLine($"{tag}_OUTBOX_ROW type={ev.Type} agg={ev.AggregateId} payload={ev.PayloadJson}");
}

DumpSeed("SEED1");
var slipDb = db.ShadowOrders.Sum(s => s.SourceVsShadowSlippage);
Console.WriteLine($"SEED1_SLIP_SUM={slipDb}");

await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);
Console.WriteLine($"SEED2_EARLY_RETURN_SHADOW_ORDERS={db.ShadowOrders.Count()}");
Console.WriteLine($"SEED2_EARLY_RETURN_COPY_INTENTS={db.CopyIntents.Count()}");
Console.WriteLine($"SEED2_EARLY_RETURN_OUTBOX={db.OutboxEvents.Count()}");

foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
{
    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
    await scoring.RebuildTraderAsync(code, login, CancellationToken.None);
}
Console.WriteLine($"REBUILD_SHADOW_ORDERS={db.ShadowOrders.Count()}");
Console.WriteLine($"REBUILD_COPY_INTENTS={db.CopyIntents.Count()}");
Console.WriteLine($"REBUILD_OUTBOX={db.OutboxEvents.Count()}");

var queries = new EfDashboardQueries(db);
var overview = await queries.GetOverviewAsync(CancellationToken.None);
Console.WriteLine($"DASH_SHADOW_COUNT={overview.Shadow} DASH_SHADOW_PNL={overview.ShadowPnl} DASH_BLOCKED={overview.RiskBlocked}");

var noDirect = !File.ReadAllText(@"D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs")
    .Contains("ShadowOrders", StringComparison.Ordinal);
Console.WriteLine($"SEEDER_TEXT_CONTAINS_ShadowOrders={!noDirect}");

bool pass = expectedRows == 6
    && db.ShadowOrders.Count() == 6
    && db.CopyIntents.Count() == 6
    && db.CopyIntents.All(c => c.Status == "SHADOW_ONLY")
    && db.TraderScores.Count(s => s.CurrentState == TraderState.SHADOW) == 2
    && db.ShadowOrders.Count(s => s.SourceLogin == 10001) == 3
    && db.ShadowOrders.Count(s => s.SourceLogin == 99001) == 3
    && db.ShadowOrders.Count(s => s.SourceLogin == 10002) == 0
    && db.ShadowOrders.Count(s => s.SourceLogin == 10003) == 0
    && slipDb == 248.20m
    && noDirect;
Console.WriteLine(pass ? "VERDICT=YES_SIX_SHADOW_ROWS_VIA_REBUILD" : "VERDICT=FAIL");
