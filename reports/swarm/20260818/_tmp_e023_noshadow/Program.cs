using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Ingestion;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Infrastructure.Persistence;
using TraderIntelligence.Infrastructure.Seeding;

Console.WriteLine("E023_NO_SHADOW_BLOCKED_EVAL");

static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots) => new()
{
    Id = n.ToString(),
    BrokerId = "ACHIEVER",
    Login = 10002,
    PositionId = 600 + n,
    CanonicalSymbol = "XAUUSD",
    SourceSymbol = "XAUUSD",
    Direction = TradeDirection.Long,
    OpenedAt = DateTimeOffset.UnixEpoch.AddHours(n),
    ClosedAt = DateTimeOffset.UnixEpoch.AddHours(n).AddMinutes(30),
    EntryVwap = 2300,
    ExitVwap = 2301,
    InitialVolumeLots = lots,
    MaxVolumeLots = lots,
    ClosedVolumeLots = lots,
    RemainingVolumeLots = 0,
    GrossRealizedPnl = pnl,
    Commission = 0,
    Swap = 0,
    Fees = 0,
    NetRealizedPnl = pnl,
    DealCount = 2,
    OrderCount = 2,
    InitialSl = 0,
    WasScaledIn = false,
    WasPartialClose = false,
    WasAveragedDown = false,
    Completed = true
};

// --- Domain-only: 10002-shaped losing martingale ---
var losing = new[]
{
    Closed(1, -200, 0.10m),
    Closed(2, -500, 0.20m),
    Closed(3, -1400, 0.40m)
};
var scorer = new BaselineScorer();
var loseScore = scorer.Score(losing);
Console.WriteLine(
    $"DOMAIN_LOSING_MARTINGALE state={loseScore.SuggestedState} q={loseScore.EarlyQualityScore} r={loseScore.RiskScore} b={loseScore.BehaviorScore} mart={loseScore.Features.Martingale} net={loseScore.Features.NetPnl} dd={loseScore.Features.MaxDrawdown} n={loseScore.Features.CompletedXauTrades}");

var winMart = new[]
{
    Closed(1, -100, 0.10m),
    Closed(2, -200, 0.20m),
    Closed(3, 2000, 0.40m)
};
var winScore = scorer.Score(winMart);
Console.WriteLine(
    $"DOMAIN_WINNING_MARTINGALE state={winScore.SuggestedState} q={winScore.EarlyQualityScore} r={winScore.RiskScore} b={winScore.BehaviorScore} mart={winScore.Features.Martingale} net={winScore.Features.NetPnl} dd={winScore.Features.MaxDrawdown}");

var mild = new[]
{
    Closed(1, -50, 0.10m),
    Closed(2, 80, 0.13m),
    Closed(3, 90, 0.10m)
};
var mildScore = scorer.Score(mild);
Console.WriteLine(
    $"DOMAIN_MILD_1_30X state={mildScore.SuggestedState} q={mildScore.EarlyQualityScore} r={mildScore.RiskScore} mart={mildScore.Features.Martingale} net={mildScore.Features.NetPnl}");

// --- InMemory seed ---
var options = new DbContextOptionsBuilder<TraderDbContext>()
    .UseInMemoryDatabase("e023-" + Guid.NewGuid())
    .Options;
await using var db = new TraderDbContext(options);
var store = new EfTradingStore(db);
var scoring = new ReconstructionScoringService(store, new TradeReconstructor(), new BaselineScorer());
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);

Console.WriteLine($"SEED_SHADOW_ORDERS={db.ShadowOrders.Count()}");
Console.WriteLine($"SEED_COPY_INTENTS={db.CopyIntents.Count()}");
Console.WriteLine($"SEED_OUTBOX={db.OutboxEvents.Count()}");
Console.WriteLine($"SEED_DEST_QUOTES={db.DestinationQuotes.Count()}");

foreach (var s in db.TraderScores.OrderBy(x => x.Login))
    Console.WriteLine($"SEED_SCORE login={s.Login} state={s.CurrentState} n={s.CompletedXauTrades} risk={s.RiskScore} q={s.EarlyQualityScore} mart={s.Martingale}");

foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
{
    var so = db.ShadowOrders.Count(o => o.SourceLogin == login);
    var ci = db.CopyIntents.Count(c => c.SourceLogin == login);
    var ox = db.OutboxEvents.Count(o => o.AggregateId.EndsWith(":" + login));
    Console.WriteLine($"SEED_BY_LOGIN login={login} shadow_orders={so} copy_intents={ci} outbox={ox}");
}

foreach (var o in db.ShadowOrders.OrderBy(x => x.SourceLogin).ThenBy(x => x.FilledAt))
    Console.WriteLine($"SEED_SHADOW_ROW login={o.SourceLogin} qty={o.Quantity} px={o.Price} slip={o.SourceVsShadowSlippage}");

foreach (var c in db.CopyIntents.OrderBy(x => x.SourceLogin))
    Console.WriteLine($"SEED_INTENT login={c.SourceLogin} status={c.Status} action={c.Action} key={c.IdempotencyKey}");

var blocked = db.TraderScores.Single(s => s.Login == 10002);
var blockedShadow = db.ShadowOrders.Count(o => o.SourceLogin == 10002);
var blockedIntent = db.CopyIntents.Count(c => c.SourceLogin == 10002);
Console.WriteLine($"CLAIM_10002_IS_RISK_BLOCKED={(blocked.CurrentState == TraderState.RISK_BLOCKED)}");
Console.WriteLine($"CLAIM_10002_SHADOW_ZERO={(blockedShadow == 0)}");
Console.WriteLine($"CLAIM_10002_INTENT_ZERO={(blockedIntent == 0)}");

// --- Direct persist: RISK_BLOCKED must not write even if completed XAU + quote exist ---
var beforeOrders = db.ShadowOrders.Count();
var beforeIntents = db.CopyIntents.Count();
var beforeOutbox = db.OutboxEvents.Count();
await store.PersistDemoShadowAsync(
    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
    10002,
    TraderState.RISK_BLOCKED,
    losing,
    CancellationToken.None);
Console.WriteLine($"DIRECT_RISK_BLOCKED_DELTA_ORDERS={db.ShadowOrders.Count() - beforeOrders}");
Console.WriteLine($"DIRECT_RISK_BLOCKED_DELTA_INTENTS={db.CopyIntents.Count() - beforeIntents}");
Console.WriteLine($"DIRECT_RISK_BLOCKED_DELTA_OUTBOX={db.OutboxEvents.Count() - beforeOutbox}");
Console.WriteLine($"DIRECT_RISK_BLOCKED_OUTBOX_LAST={db.OutboxEvents.OrderBy(o => o.OccurredAt).Last().PayloadJson}");

// --- Contrast: same tape, caller lies SHADOW ---
var lieBeforeOrders = db.ShadowOrders.Count();
var lieBeforeIntents = db.CopyIntents.Count();
await store.PersistDemoShadowAsync(
    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
    10002,
    TraderState.SHADOW,
    losing,
    CancellationToken.None);
Console.WriteLine($"DIRECT_LIE_SHADOW_DELTA_ORDERS={db.ShadowOrders.Count() - lieBeforeOrders}");
Console.WriteLine($"DIRECT_LIE_SHADOW_DELTA_INTENTS={db.CopyIntents.Count() - lieBeforeIntents}");
Console.WriteLine($"DIRECT_LIE_SHADOW_LOGINS={string.Join(',', db.ShadowOrders.Where(o => o.SourceLogin == 10002).Select(o => o.Quantity))}");

// --- Other blocked-family tokens ---
foreach (var st in new[] { TraderState.WATCH, TraderState.PAUSED, TraderState.DISQUALIFIED, TraderState.INSUFFICIENT_DATA, TraderState.EARLY_SCORE, TraderState.LIVE, TraderState.LIVE_CANDIDATE })
{
    var b = db.ShadowOrders.Count();
    await store.PersistDemoShadowAsync(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
        88888,
        st,
        losing,
        CancellationToken.None);
    Console.WriteLine($"DIRECT_{st}_DELTA_ORDERS={db.ShadowOrders.Count() - b}");
}

var pass = blocked.CurrentState == TraderState.RISK_BLOCKED
           && blockedShadow == 0
           && blockedIntent == 0
           && loseScore.SuggestedState == TraderState.RISK_BLOCKED;
Console.WriteLine(pass
    ? "VERDICT=RISK_BLOCKED_CREATES_ZERO_SHADOW_ON_DEMO_AND_DIRECT"
    : "VERDICT=FAIL_UNEXPECTED_SHADOW");
