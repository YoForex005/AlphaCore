using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Dashboard;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;
using TraderIntelligence.Mt5.Connectors;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
const long EmptyLogin = 10003;
var recon = new TradeReconstructor();
var scorer = new BaselineScorer();
var (achiever, starwave) = DemoBrokerFactory.CreateDefault();

Console.WriteLine("C23_EMPTY_TRADER_EVAL");
Console.WriteLine("login=" + EmptyLogin);

var accounts = await achiever.GetAccountsAsync(null, CancellationToken.None);
var acc = accounts.SingleOrDefault(a => a.Login == EmptyLogin);
Console.WriteLine("ACCOUNT_FOUND=" + (acc is not null));
if (acc is not null)
{
    Console.WriteLine($"ACCOUNT group={acc.GroupName} leverage={acc.Leverage} balance={acc.Balance} equity={acc.Equity} margin={acc.Margin} marginFree={acc.MarginFree} profit={acc.Profit}");
}

var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
var to = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
var windowDeals = await achiever.GetDealsAsync(EmptyLogin, from, to, CancellationToken.None);
var allDeals = await achiever.GetDealsAsync(EmptyLogin, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None);
var positions = await achiever.GetPositionsAsync(EmptyLogin, CancellationToken.None);
var otherLogins = new long[] { 10001, 10002, 99001 };
Console.WriteLine("DEALS_SEED_WINDOW=" + windowDeals.Count);
Console.WriteLine("DEALS_UNBOUNDED=" + allDeals.Count);
Console.WriteLine("POSITIONS=" + positions.Count);
foreach (var login in otherLogins)
{
    var c = login >= 99000 ? starwave : achiever;
    var n = (await c.GetDealsAsync(login, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None)).Count;
    Console.WriteLine($"DEALS_OTHER login={login} n={n}");
}

var dealLogins = (await achiever.GetDealsAsync(0, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None))
    .Select(d => d.Login)
    .Concat((await starwave.GetDealsAsync(0, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None)).Select(d => d.Login))
    .Distinct()
    .OrderBy(x => x)
    .ToArray();
// GetDeals filters by login; dump raw by querying each known + confirm 10003 absent from others
var achieverAll = new List<long>();
foreach (var login in new long[] { 10001, 10002, 10003, 99001, 0, 1 })
    achieverAll.AddRange((await achiever.GetDealsAsync(login, DateTimeOffset.MinValue, DateTimeOffset.MaxValue, CancellationToken.None)).Select(d => d.Login));
Console.WriteLine("ACHIEVER_DEAL_LOGINS=" + string.Join(",", achieverAll.Distinct().OrderBy(x => x)));

NormalizedDeal ToN(string code, TraderIntelligence.Application.Contracts.Mt5DealDto d) => new()
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

var nd = allDeals.Select(d => ToN(achiever.BrokerCode, d)).ToList();
var trades = recon.Reconstruct(achiever.BrokerCode, EmptyLogin, nd);
var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
Console.WriteLine($"RECON trades={trades.Count} completedXau={completedXau.Count} earlyEligible={recon.IsEarlyScoreEligible(achiever.BrokerCode, EmptyLogin, nd)}");

void DumpScore(string name, IReadOnlyList<ReconstructedTradeResult> input)
{
    var s = scorer.Score(input);
    var f = s.Features;
    Console.WriteLine($"SCORE_{name} N={f.CompletedXauTrades} eligible={s.EarlyScoreEligible} state={s.SuggestedState} risk={s.RiskScore} behavior={s.BehaviorScore} quality={s.EarlyQualityScore} sl={f.SlUseRate} mart={f.Martingale} canLive={TraderStateMachine.CanPromoteToLive(s.SuggestedState)}");
}

DumpScore("EMPTY_ARRAY", Array.Empty<ReconstructedTradeResult>());
DumpScore("RECON_10003", trades);
DumpScore("N2_CONTROL", new[]
{
    Closed(1, 10),
    Closed(2, 10)
});

var n0State = TraderStateMachine.FromBaseline(false, 99m, 0m, new FeatureSnapshot
{
    CompletedXauTrades = 0,
    NetPnl = 0, GrossProfit = 0, GrossLoss = 0, ProfitFactor = 0,
    LotCv = 0, LossSizeCv = 0, Martingale = false, AveragingDown = false,
    LotEscalation = false, AverageHoldSeconds = 0, SlUseRate = 0, MaxDrawdown = 0,
    TradeFrequencyPerDay = 0
});
Console.WriteLine("STATE_MACHINE_N0_FORCED=" + n0State);

var options = new DbContextOptionsBuilder<TraderDbContext>()
    .UseInMemoryDatabase("c23-empty-" + Guid.NewGuid())
    .Options;
await using var db = new TraderDbContext(options);
var store = new EfTradingStore(db);
var scoring = new ReconstructionScoringService(store, recon, scorer);
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);

var dealCount = db.Mt5Deals.Count(d => d.Login == EmptyLogin);
var tradeCount = db.ReconstructedTrades.Count(t => t.Login == EmptyLogin);
var accountRow = db.Mt5Accounts.SingleOrDefault(a => a.Login == EmptyLogin);
var scoreRow = db.TraderScores.SingleOrDefault(s => s.Login == EmptyLogin);
var hist = db.TraderScoreHistory.Where(h => h.Login == EmptyLogin).ToList();
Console.WriteLine("SEED_BROKERS=" + db.Brokers.Count());
Console.WriteLine("SEED_ACCOUNTS=" + string.Join(",", db.Mt5Accounts.Select(a => a.Login).OrderBy(x => x)));
Console.WriteLine("SEED_DEALS_BY_LOGIN=" + string.Join(";", db.Mt5Deals.GroupBy(d => d.Login).OrderBy(g => g.Key).Select(g => g.Key + "=" + g.Count())));
Console.WriteLine("SEED_RECON_BY_LOGIN=" + string.Join(";", db.ReconstructedTrades.GroupBy(t => t.Login).OrderBy(g => g.Key).Select(g => g.Key + "=" + g.Count())));
Console.WriteLine("SEED_SCORES=" + string.Join(";", db.TraderScores.OrderBy(s => s.Login).Select(s => $"{s.Login}:{s.CurrentState}:N={s.CompletedXauTrades}:q={s.EarlyQualityScore}:r={s.RiskScore}:b={s.BehaviorScore}")));
Console.WriteLine("SEED_10003_ACCOUNT=" + (accountRow is not null));
Console.WriteLine("SEED_10003_DEALS=" + dealCount);
Console.WriteLine("SEED_10003_RECON=" + tradeCount);
Console.WriteLine("SEED_10003_SCORE_PRESENT=" + (scoreRow is not null));
if (scoreRow is not null)
{
    Console.WriteLine($"SEED_10003_STATE={scoreRow.CurrentState}");
    Console.WriteLine($"SEED_10003_N={scoreRow.CompletedXauTrades}");
    Console.WriteLine($"SEED_10003_RISK={scoreRow.RiskScore}");
    Console.WriteLine($"SEED_10003_BEHAVIOR={scoreRow.BehaviorScore}");
    Console.WriteLine($"SEED_10003_QUALITY={scoreRow.EarlyQualityScore}");
    Console.WriteLine($"SEED_10003_MART={scoreRow.Martingale}");
    Console.WriteLine($"SEED_10003_AVGDOWN={scoreRow.AveragingDown}");
    Console.WriteLine($"SEED_10003_ESC={scoreRow.LotEscalation}");
    Console.WriteLine($"SEED_10003_INSUFFICIENT={scoreRow.CurrentState == TraderState.INSUFFICIENT_DATA}");
}
Console.WriteLine("SEED_10003_HISTORY=" + hist.Count + (hist.Count == 0 ? "" : " state=" + hist[0].State));

var queries = new EfDashboardQueries(db);
var overview = await queries.GetOverviewAsync(CancellationToken.None);
var traders = await queries.GetTradersAsync(null, null, CancellationToken.None);
var row = traders.SingleOrDefault(t => t.Login == EmptyLogin);
Console.WriteLine($"DASH_OVERVIEW accounts={overview.TotalAccounts} xauTraders={overview.XauTraders} three={overview.TradersWithThreeTrades} watch={overview.Watch} shadow={overview.Shadow} blocked={overview.RiskBlocked}");
Console.WriteLine("DASH_TRADER_COUNT=" + traders.Count);
Console.WriteLine("DASH_10003_PRESENT=" + (row is not null));
if (row is not null)
{
    Console.WriteLine($"DASH_10003 broker={row.Broker} group={row.Group} N={row.CompletedXauTrades} pnl={row.NetSourcePnl} early={row.EarlyScore} risk={row.RiskScore} state={row.State}");
}
var rankedFirst = traders.FirstOrDefault();
Console.WriteLine("DASH_RANK1=" + (rankedFirst is null ? "none" : rankedFirst.Login + " early=" + rankedFirst.EarlyScore + " state=" + rankedFirst.State));
var insuff = traders.Where(t => t.State == TraderState.INSUFFICIENT_DATA).Select(t => t.Login).ToArray();
Console.WriteLine("DASH_INSUFFICIENT_LOGINS=" + string.Join(",", insuff));

bool pass =
    acc is not null
    && windowDeals.Count == 0
    && allDeals.Count == 0
    && positions.Count == 0
    && trades.Count == 0
    && completedXau.Count == 0
    && scorer.Score(Array.Empty<ReconstructedTradeResult>()).SuggestedState == TraderState.INSUFFICIENT_DATA
    && scorer.Score(trades).SuggestedState == TraderState.INSUFFICIENT_DATA
    && n0State == TraderState.INSUFFICIENT_DATA
    && accountRow is not null
    && dealCount == 0
    && tradeCount == 0
    && scoreRow is not null
    && scoreRow.CurrentState == TraderState.INSUFFICIENT_DATA
    && scoreRow.CompletedXauTrades == 0
    && !scoreRow.Martingale;
Console.WriteLine(pass ? "VERDICT=PASS_INSUFFICIENT_DATA" : "VERDICT=FAIL");

static ReconstructedTradeResult Closed(int n, decimal pnl) =>
    new()
    {
        Id = n.ToString(),
        BrokerId = "ACHIEVER",
        Login = 1,
        PositionId = n,
        CanonicalSymbol = "XAUUSD",
        SourceSymbol = "XAUUSD",
        Direction = TradeDirection.Long,
        OpenedAt = DateTimeOffset.UnixEpoch.AddHours(n),
        ClosedAt = DateTimeOffset.UnixEpoch.AddHours(n).AddMinutes(30),
        EntryVwap = 2300,
        ExitVwap = 2301,
        InitialVolumeLots = 0.10m,
        MaxVolumeLots = 0.10m,
        ClosedVolumeLots = 0.10m,
        RemainingVolumeLots = 0,
        GrossRealizedPnl = pnl,
        Commission = 0,
        Swap = 0,
        Fees = 0,
        NetRealizedPnl = pnl,
        DealCount = 2,
        OrderCount = 2,
        InitialSl = 2290,
        WasScaledIn = false,
        WasPartialClose = false,
        WasAveragedDown = false,
        Completed = true
    };
