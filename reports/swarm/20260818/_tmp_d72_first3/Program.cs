using System.Globalization;
using System.Reflection;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Instruments;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Domain.Volume;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var r = new TradeReconstructor(VolumeConverter.Manager);
var n = new SymbolNormalizer();
var scorer = new BaselineScorer();
var rt = typeof(ReconstructedTradeResult);
var entHasEligible = typeof(TraderIntelligence.Domain.Entities.ReconstructedTrade)
    .GetProperty("EligibleForFirstThree") is not null;
var entHasFirst3 = typeof(TraderIntelligence.Domain.Entities.ReconstructedTrade)
    .GetProperty("IsFirstThree") is not null
    || typeof(TraderIntelligence.Domain.Entities.ReconstructedTrade).GetProperty("Dirty") is not null;

Console.WriteLine(
    "META\thasDirty=" + (rt.GetProperty("Dirty") is not null)
    + "\thasSeq=" + (rt.GetProperty("LifecycleSeq") is not null)
    + "\thasFirst3Keys=" + (rt.GetProperty("First3Keys") is not null)
    + "\thasEligible=" + (rt.GetProperty("EligibleForFirstThree") is not null)
    + "\tentityEligible=" + entHasEligible
    + "\tentityFirst3OrDirty=" + entHasFirst3
    + "\tearlyN=" + BaselineScorer.EarlyScoreTradeCount);

NormalizedDeal D(
    long ticket, long pos, DealAction a, DealEntry e, ulong vol, decimal px, decimal pnl, int t,
    string symbol = "XAUUSDm", DealReason? reason = null, string broker = "ACHIEVER", long login = 1) => new()
{
    BrokerId = broker,
    Login = login,
    DealTicket = ticket,
    OrderTicket = ticket,
    PositionId = pos,
    SourceSymbol = symbol,
    Action = a,
    Entry = e,
    VolumeNative = vol,
    Price = px,
    Profit = pnl,
    Commission = 0,
    Swap = 0,
    Time = DateTimeOffset.UnixEpoch.AddMinutes(t),
    Reason = reason
};

(NormalizedDeal, NormalizedDeal) Round(long pos, int t, string symbol, decimal pnl = 10m)
{
    var ticket = pos * 10;
    return (
        D(ticket, pos, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, t, symbol),
        D(ticket + 1, pos, DealAction.Sell, DealEntry.Out, 10000, 2410m, pnl, t + 1, symbol));
}

void Line(string id, string kvs) => Console.WriteLine($"{id}\t{kvs}");

void DumpBooks(string id, IReadOnlyList<ReconstructedTradeResult> trades)
{
    var i = 0;
    foreach (var t in trades)
    {
        Line(id,
            $"i={i++}\tpos={t.PositionId}\tcomp={t.Completed}\txau={t.IsXauUsd}"
            + $"\telig={t.EligibleForFirstThree}\tcanon={t.CanonicalSymbol}\tsrc={t.SourceSymbol}"
            + $"\trem={t.RemainingVolumeLots}\tpart={t.WasPartialClose}");
    }
}

// M1 mixed book: 2 completed XAU + 1 EUR + 1 XAG + 1 GOLD + 1 open XAU + 1 partial XAU
{
    var deals = new List<NormalizedDeal>();
    var (a, b) = Round(1, 1, "XAUUSDm"); deals.Add(a); deals.Add(b);
    var (c, d) = Round(2, 10, "XAUUSD."); deals.Add(c); deals.Add(d);
    var (e, f) = Round(3, 20, "EURUSD", 5m); deals.Add(e); deals.Add(f);
    var (g, h) = Round(4, 30, "XAGUSD", 8m); deals.Add(g); deals.Add(h);
    var (i, j) = Round(5, 40, "GOLD", 12m); deals.Add(i); deals.Add(j);
    deals.Add(D(61, 6, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 50, "XAUUSD"));
    deals.Add(D(71, 7, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 60, "XAUUSDm"));
    deals.Add(D(72, 7, DealAction.Sell, DealEntry.Out, 4000, 2410m, 4m, 61, "XAUUSDm"));

    var all = r.Reconstruct("ACHIEVER", 1, deals);
    var xau = r.CompletedXauUsdTrades("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var elig = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    var scoreAll = scorer.Score(all);
    var scoreXau = scorer.Score(xau);
    var scoreCompletedXauNoElig = scorer.Score(all.Where(t => t.Completed && t.IsXauUsd).ToList());

    var first3 = 0;
    var dash = all.OrderBy(t => t.ClosedAt ?? t.OpenedAt).Select(t =>
    {
        var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && first3 < 3;
        if (first) first3++;
        return (t.PositionId, t.CanonicalSymbol, t.Completed, first, t.EligibleForFirstThree);
    }).ToList();

    Line("M1_MIXED",
        $"recon={all.Count}\tcompleted={all.Count(t => t.Completed)}"
        + $"\tcompletedXauFilter={all.Count(t => t.Completed && t.IsXauUsd)}"
        + $"\tcountHelper={count}\teligibleHelper={elig}"
        + $"\txauList={xau.Count}\txauPos=[{string.Join(',', xau.Select(t => t.PositionId))}]"
        + $"\tscoreAllN={scoreAll.Features.CompletedXauTrades}\tscoreAllElig={scoreAll.EarlyScoreEligible}"
        + $"\tscoreXauN={scoreXau.Features.CompletedXauTrades}\tscoreXauElig={scoreXau.EarlyScoreEligible}"
        + $"\tscoreNoEligN={scoreCompletedXauNoElig.Features.CompletedXauTrades}"
        + $"\tdashFirst3={dash.Count(x => x.first)}\tdashPos=[{string.Join(',', dash.Where(x => x.first).Select(x => x.PositionId))}]");
    DumpBooks("M1_BOOK", all);
}

// M2 alias / unmapped matrix
{
    string[] symbols =
    {
        "XAUUSD", "XAUUSD.", "XAUUSDm", "XAUUSD.a", "GOLD", "GOLD.",
        "XAUUSDFUT", "XAUEUR", "XAGUSD", "EURUSD", "SILVER", "XAUUSD.c"
    };
    foreach (var s in symbols)
    {
        var mapped = n.TryMapSource(s, out var canon);
        var (a, b) = Round(100 + Array.IndexOf(symbols, s), 1, s);
        var deals = new[] { a, b };
        var trades = r.Reconstruct("ACHIEVER", 1, deals);
        var t = trades[0];
        Line("M2_ALIAS",
            $"src={s}\tmapOk={mapped}\tmapCanon={(mapped ? canon : "")}"
            + $"\ttradeCanon={t.CanonicalSymbol}\txau={t.IsXauUsd}"
            + $"\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}");
    }
}

// M3 two XAU + one EUR — must not latch
{
    var deals = new List<NormalizedDeal>();
    var p = Round(1, 1, "XAUUSD"); deals.Add(p.Item1); deals.Add(p.Item2);
    p = Round(2, 10, "XAUUSD"); deals.Add(p.Item1); deals.Add(p.Item2);
    p = Round(3, 20, "EURUSD"); deals.Add(p.Item1); deals.Add(p.Item2);
    Line("M3_2XAU_1EUR",
        $"recon={r.Reconstruct("ACHIEVER", 1, deals).Count}"
        + $"\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}"
        + $"\teligible={r.IsEarlyScoreEligible("ACHIEVER", 1, deals)}");
}

// M4 four XAU — helper count is 4 not 3; dashboard first3=3
{
    var deals = new List<NormalizedDeal>();
    for (var i = 1; i <= 4; i++)
    {
        var p = Round(i, i * 10, "XAUUSD", i);
        deals.Add(p.Item1); deals.Add(p.Item2);
    }
    var all = r.Reconstruct("ACHIEVER", 1, deals);
    var xau = r.CompletedXauUsdTrades("ACHIEVER", 1, deals);
    var score = scorer.Score(all.Where(t => t.Completed && t.IsXauUsd).ToList());
    var first3 = 0;
    var dashN = all.OrderBy(t => t.ClosedAt ?? t.OpenedAt)
        .Count(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && first3 < 3;
            if (first) first3++;
            return first;
        });
    Line("M4_FOUR_XAU",
        $"count={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}"
        + $"\txauList={xau.Count}\tscoreN={score.Features.CompletedXauTrades}"
        + $"\tscoreElig={score.EarlyScoreEligible}\tdashFirst3={dashN}"
        + $"\thelperDoesNotCapAt3={xau.Count == 4}");
}

// M5 cancel-tainted + 2 clean XAU: helper 2 / score-all-completed 3
{
    var deals = new List<NormalizedDeal>
    {
        D(1, 1, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1),
        D(2, 1, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 2),
        D(3, 2, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 3),
        D(4, 2, DealAction.BuyCanceled, DealEntry.In, 10000, 2400m, 0, 4),
        D(5, 2, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 5),
        D(6, 3, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 6),
        D(7, 3, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 7)
    };
    var all = r.Reconstruct("ACHIEVER", 1, deals);
    var helper = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var score = scorer.Score(all.Where(t => t.Completed && t.IsXauUsd).ToList());
    var first3 = 0;
    var dash = all.OrderBy(t => t.ClosedAt ?? t.OpenedAt)
        .Count(t =>
        {
            var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && first3 < 3;
            if (first) first3++;
            return first;
        });
    Line("M5_CANCEL",
        $"completed={all.Count(t => t.Completed)}\thelper={helper}"
        + $"\thelperElig={r.IsEarlyScoreEligible("ACHIEVER", 1, deals)}"
        + $"\tscoreN={score.Features.CompletedXauTrades}\tscoreElig={score.EarlyScoreEligible}"
        + $"\tdashFirst3={dash}"
        + $"\tdirtyFlags=[{string.Join(',', all.Select(t => t.EligibleForFirstThree))}]");
}

// M6 rollover + settlement flatten must not count (if reason wired)
{
    var deals = new[]
    {
        D(1, 1, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1, reason: DealReason.Rollover),
        D(2, 1, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 2, reason: DealReason.Rollover),
        D(3, 2, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 3, reason: DealReason.Client),
        D(4, 2, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 4, reason: DealReason.Settlement)
    };
    var all = r.Reconstruct("ACHIEVER", 1, deals);
    Line("M6_REASON",
        $"recon={all.Count}\tcompleted={all.Count(t => t.Completed)}"
        + $"\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}"
        + $"\tpos=[{string.Join(',', all.Select(t => $"{t.PositionId}:comp={t.Completed}"))}]");
}

// M7 balance / credit / open / partial isolation
{
    var deals = new[]
    {
        D(1, 0, DealAction.Balance, DealEntry.In, 0, 0, 1000, 1),
        D(2, 0, DealAction.Credit, DealEntry.In, 0, 0, 50, 2),
        D(3, 9, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 3),
        D(4, 8, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 4),
        D(5, 8, DealAction.Sell, DealEntry.Out, 4000, 2410m, 4, 5)
    };
    Line("M7_NOISE",
        $"recon={r.Reconstruct("ACHIEVER", 1, deals).Count}"
        + $"\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}"
        + $"\teligible={r.IsEarlyScoreEligible("ACHIEVER", 1, deals)}");
}

// M8 mixed symbols same position_id (XAU IN + XAG OUT)
{
    var deals = new[]
    {
        D(1, 96, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1, "XAUUSDm"),
        D(2, 96, DealAction.Sell, DealEntry.Out, 10000, 31m, 10, 2, "XAGUSD")
    };
    var t = r.Reconstruct("ACHIEVER", 1, deals)[0];
    Line("M8_MIXED_POS",
        $"comp={t.Completed}\txau={t.IsXauUsd}\tcanon={t.CanonicalSymbol}\tsrc={t.SourceSymbol}"
        + $"\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}");
}

// M9 GOLD. compact map + XAUEUR
{
    Line("M9_GOLD_DOT",
        $"goldDotMap={n.TryMapSource("GOLD.", out var g)}\tgoldDotCanon={g}"
        + $"\txaueurMap={n.TryMapSource("XAUEUR", out var x)}\txaueurCanon={x}");
}

Console.WriteLine("DONE");
