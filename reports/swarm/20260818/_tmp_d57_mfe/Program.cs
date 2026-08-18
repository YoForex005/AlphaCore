using System.Globalization;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var s = new BaselineScorer();
var outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "D57_measured.tsv"));
File.WriteAllText(outPath, "id\tN\tAvgMfe\tAvgMae\tQuality\tPriceSource\tRisk\tBehavior\tEQ\tState\tNetPnl\tEntry\tExit\n");

static ReconstructedTradeResult T(
    int n,
    decimal pnl,
    decimal lots = 0.10m,
    decimal entry = 2300m,
    decimal? exit = 2301m,
    decimal? sl = 2290m,
    bool avg = false)
{
    var opened = DateTimeOffset.UnixEpoch.AddHours(n);
    return new ReconstructedTradeResult
    {
        Id = n.ToString(),
        BrokerId = "ACHIEVER",
        Login = 1,
        PositionId = n,
        CanonicalSymbol = "XAUUSD",
        SourceSymbol = "XAUUSD",
        Direction = TradeDirection.Long,
        OpenedAt = opened,
        ClosedAt = opened.AddMinutes(30),
        EntryVwap = entry,
        ExitVwap = exit,
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
        InitialSl = sl,
        WasScaledIn = false,
        WasPartialClose = false,
        WasAveragedDown = avg,
        Completed = true
    };
}

void Run(string id, params ReconstructedTradeResult[] trades)
{
    var score = s.Score(trades);
    var f = score.Features;
    var entry = trades.Length == 0 ? "" : trades[0].EntryVwap.ToString("0.####");
    var exit = trades.Length == 0 ? "" : (trades[0].ExitVwap?.ToString("0.####") ?? "null");
    var line = string.Join('\t',
        id,
        f.CompletedXauTrades,
        f.AverageMfe.HasValue ? f.AverageMfe.Value.ToString("0.####") : "NULL",
        f.AverageMae.HasValue ? f.AverageMae.Value.ToString("0.####") : "NULL",
        f.MaeMfeQuality,
        f.PriceSource,
        score.RiskScore.ToString("0.##"),
        score.BehaviorScore.ToString("0.##"),
        score.EarlyQualityScore.ToString("0.##"),
        score.SuggestedState,
        f.NetPnl.ToString("0.####"),
        entry,
        exit);
    Console.WriteLine(line);
    File.AppendAllText(outPath, line + Environment.NewLine);
}

Run("EMPTY");
Run("N1_DEAL_ONLY", T(1, 80));
Run("N2_DEAL_ONLY", T(1, 80), T(2, 70));
Run("FX02_WINNERS_VWAP_2300_2301", T(1, 80), T(2, 70), T(3, 90));
Run("FX02_WINNERS_VWAP_2000_3000", T(1, 80, entry: 2000m, exit: 3000m), T(2, 70, entry: 2000m, exit: 3000m), T(3, 90, entry: 2000m, exit: 3000m));
Run("FX02_WINNERS_VWAP_NULL_EXIT", T(1, 80, exit: null), T(2, 70, exit: null), T(3, 90, exit: null));
Run("FX03_LOSING_MART", T(1, -100, 0.10m), T(2, -200, 0.20m), T(3, -400, 0.40m));
Run("MILD_MART_WIN", T(1, -10, 0.10m), T(2, 50, 0.13m), T(3, 40, 0.13m));

var a = s.Score(new[] { T(1, 80), T(2, 70), T(3, 90) });
var b = s.Score(new[]
{
    T(1, 80, entry: 2000m, exit: 3000m),
    T(2, 70, entry: 2000m, exit: 3000m),
    T(3, 90, entry: 2000m, exit: 3000m)
});
var sameScores = a.RiskScore == b.RiskScore
    && a.BehaviorScore == b.BehaviorScore
    && a.EarlyQualityScore == b.EarlyQualityScore
    && a.SuggestedState == b.SuggestedState;
var bothNull = a.Features.AverageMfe is null && a.Features.AverageMae is null
    && b.Features.AverageMfe is null && b.Features.AverageMae is null;
var bothUnavail = a.Features.MaeMfeQuality == FeatureQuality.Unavailable
    && b.Features.MaeMfeQuality == FeatureQuality.Unavailable;
Console.WriteLine($"VWAP_MUTATION_SCORES_IDENTICAL={sameScores}");
Console.WriteLine($"BOTH_AVERAGES_NULL={bothNull}");
Console.WriteLine($"BOTH_QUALITY_UNAVAILABLE={bothUnavail}");
File.AppendAllText(outPath,
    $"VWAP_MUTATION_SCORES_IDENTICAL\t{sameScores}\t{bothNull}\t{bothUnavail}\n");
