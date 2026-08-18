using System.Globalization;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var s = new BaselineScorer();
var outPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "C32_measured.tsv"));
File.WriteAllText(outPath, "");

static ReconstructedTradeResult T(
    int n,
    decimal pnl,
    decimal lots,
    decimal? sl = 2290m,
    bool avg = false,
    string symbol = "XAUUSD",
    bool completed = true,
    DateTimeOffset? closedAt = null)
{
    var opened = DateTimeOffset.UnixEpoch.AddHours(n);
    return new ReconstructedTradeResult
    {
        Id = n.ToString(),
        BrokerId = "ACHIEVER",
        Login = 1,
        PositionId = n,
        CanonicalSymbol = symbol,
        SourceSymbol = symbol,
        Direction = TradeDirection.Long,
        OpenedAt = opened,
        ClosedAt = closedAt ?? opened.AddMinutes(30),
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
        InitialSl = sl,
        WasScaledIn = false,
        WasPartialClose = false,
        WasAveragedDown = avg,
        Completed = completed
    };
}

void Run(string id, params ReconstructedTradeResult[] trades)
{
    var score = s.Score(trades);
    var f = score.Features;
    var q70 = score.EarlyQualityScore >= 70m;
    var line = string.Join('|',
            id,
            f.CompletedXauTrades,
            f.Martingale ? 1 : 0,
            f.LotEscalation ? 1 : 0,
            f.AveragingDown ? 1 : 0,
            f.NetPnl.ToString("0.####"),
            f.ProfitFactor.ToString("0.####"),
            f.LotCv.ToString("0.####"),
            f.LossSizeCv.ToString("0.####"),
            f.SlUseRate.ToString("0.####"),
            f.MaxDrawdown.ToString("0.####"),
            f.GrossProfit.ToString("0.####"),
            score.RiskScore.ToString("0.##"),
            score.BehaviorScore.ToString("0.##"),
            score.EarlyQualityScore.ToString("0.##"),
            q70 ? "Q>=70" : "Q<70",
            score.SuggestedState,
            score.EarlyScoreEligible ? 1 : 0);
    Console.WriteLine(line);
    File.AppendAllText(outPath, line + Environment.NewLine);
}

var header = string.Join('|',
    "id", "N", "mart", "esc", "avg", "net", "pf", "lotCv", "lossCv", "sl", "dd", "gp",
    "risk", "beh", "qual", "q70", "state", "elig");
Console.WriteLine(header);
File.AppendAllText(outPath, header + Environment.NewLine);

// --- load-bearing quality>=70 with martingale ---
Run("MILD_1.26_SL",
    T(1, -50, 0.10m), T(2, 200, 0.126m), T(3, 50, 0.10m));

Run("MILD_1.30_SL_B12",
    T(1, -50, 0.10m), T(2, -50, 0.13m), T(3, 400, 0.13m));

Run("MILD_1.50_EXACT",
    T(1, -50, 0.10m), T(2, 200, 0.15m), T(3, 50, 0.10m));

Run("BOUND_1.25_EXACT_NOT_MART",
    T(1, -50, 0.10m), T(2, 200, 0.125m), T(3, 50, 0.10m));

Run("JUST_OVER_1.25",
    T(1, -50, 0.10m), T(2, 200, 0.1250001m), T(3, 50, 0.10m));

Run("PF_1.05_MILD",
    T(1, -100, 0.10m), T(2, 80, 0.13m), T(3, 25, 0.10m)); // GP=105 GL=100 PF=1.05 NET=+5

Run("PF_1.19_MILD",
    T(1, -100, 0.10m), T(2, 90, 0.13m), T(3, 29, 0.10m)); // GP=119 GL=100

Run("PF_1.20_MILD",
    T(1, -100, 0.10m), T(2, 90, 0.13m), T(3, 30, 0.10m)); // GP=120 GL=100

Run("PF_1.79_MILD",
    T(1, -100, 0.10m), T(2, 100, 0.13m), T(3, 79, 0.10m));

Run("PF_1.80_MILD",
    T(1, -100, 0.10m), T(2, 100, 0.13m), T(3, 80, 0.10m));

Run("NET_ZERO_MILD",
    T(1, -100, 0.10m), T(2, 50, 0.13m), T(3, 50, 0.10m));

Run("NET_NEG1_MILD",
    T(1, -100, 0.10m), T(2, 50, 0.13m), T(3, 49, 0.10m));

// --- Case B family ---
Run("CASEB_SL",
    T(1, -50, 0.10m), T(2, -100, 0.20m), T(3, 800, 0.40m));

Run("CASEB_NOSL",
    T(1, -50, 0.10m, sl: null), T(2, -100, 0.20m, sl: null), T(3, 800, 0.40m, sl: null));

Run("FX03_LOSING",
    T(1, -100, 0.10m), T(2, -200, 0.20m), T(3, -400, 0.40m));

Run("UNIT_3WIN",
    T(1, 80, 0.10m), T(2, 70, 0.10m), T(3, 90, 0.10m));

// --- extra flags ---
Run("MILD_NOSL",
    T(1, -50, 0.10m, sl: null), T(2, 200, 0.13m, sl: null), T(3, 50, 0.10m, sl: null));

Run("MILD_SL_1of3",
    T(1, -50, 0.10m, sl: 2290m), T(2, 200, 0.13m, sl: null), T(3, 50, 0.10m, sl: null));

Run("MILD_SL_0",
    T(1, -50, 0.10m, sl: 0m), T(2, 200, 0.13m, sl: 0m), T(3, 50, 0.10m, sl: 0m));

Run("MILD_AVG",
    T(1, -50, 0.10m, avg: true), T(2, 200, 0.13m), T(3, 50, 0.10m));

Run("MILD_ESC_1.51",
    T(1, -50, 0.10m), T(2, 200, 0.151m), T(3, 50, 0.10m));

Run("MILD_DD_GT_GP",
    T(1, -200, 0.10m), T(2, 80, 0.13m), T(3, 130, 0.10m)); // NET=+10 GP=210 DD=200? 0->-200 dd200; +80 eq-120; +130 eq+10 peak10 dd200. GP=210 DD=200 not >

Run("MILD_DD_GT_GP_TRUE",
    T(1, -300, 0.10m), T(2, 80, 0.13m), T(3, 230, 0.10m)); // NET=+10 GP=310 DD=300; 300>310? no
// need DD > GP: lose 400, recover to net+10 => GP = 410, DD=400, 400>410 false
// lose 400, recover to net+1 => GP=401 DD=400, 400>401 false
// lose 400, recover GP=350 NET=-50? then net<0 blocked
// DD > GP with net>0: start win then big loss then martingale recovery
// t1 +50 peak50; t2 -200 eq-150 dd200; t3 +160 eq+10. GP=210 GL=200 NET=+10 DD=200. 200>210? no
// t1 +10; t2 -200 dd200 eq-190; t3 +191. GP=201 GL=200 NET+1 DD=200. 200>201? no
// t1 +0 BE; t2 -200; t3 +201. GP=201 DD=200
// To have DD > GP and NET>0: peak must be high from early wins, then large loss, then small recovery
// t1 +200 peak200; t2 -250 eq-50 dd250; t3 +60@1.3x (after loss). GP=260 GL=250 NET+10 DD=250. 250>260? no
// t1 +300; t2 -400; t3 +110. GP=410 GL=400 NET+10 DD=400. still DD < GP always if only one loss?
// Multiple losses: t1 +100; t2 -80; t3 +20 0.13x; wait
// equity: 100, 20, 40. dd from 100 is 80. GP=120 DD=80
// Actually: max_dd is peak-trough on completed path. GP is sum of wins.
// After a peak P, a trough P-D, then recover by R to net = P - D + R_after.
// GP includes the initial peak wins + later wins. DD = D (if that's max).
// GP >= peak_wins + later_wins. If peak is made of wins, GP >= peak + later_wins, DD <= peak + |later losses before recover|...
// Classic: only losses then wins: peak=0, DD=|cum losses before recover|, GP=later wins, NET=GP-GL, DD=GL_until_trough ≈ GL if monotone down then up.
// If monotone: DD=GL_prefix, GP=wins, DD>GP means prefix losses > total wins, so NET < 0.
// So DD>GP AND NET>0 requires a NEW peak then a crash larger than subsequent wins... 
// t1 +100 peak100; t2 -250 eq-150 dd250; t3 +160 eq+10. GP=260 GL=250 NET+10 DD=250. 250 > 260? NO
// t1 +50; t2 -300 eq-250 dd300; t3 +260 eq+10. GP=310 GL=300 NET+10 DD=300. 300>310? NO
// Pattern: DD = peak - min_equity. GP = sum positives.
// NET = GP-GL > 0 => GP > GL
// After first win W, then loss -L, eq = W-L, dd = L if W-L < 0 then dd = L? peak=W, eq=W-L, dd=L.
// Then win R: GP=W+R, DD=L. L > W+R would mean first loss bigger than all wins, NET = W+R-L < 0.
// So with ONE loss after a peak of only wins, DD>GP implies NET<0.
// Need TWO losses or peak that includes... peak can stay if we go up and down.
// t1 +10; t2 +10 peak20; t3 -100 eq-80 dd100; t4 +90@size. GP=110 GL=100 NET+10 DD=100. 100>110? no
// ALWAYS DD <= GL? Not if... DD is peak-trough, peak is max equity which is sum of prefix.
// Actually DD can exceed later GP portion but GP includes ALL wins including those that built the peak.
// Theorem: if all positive pnl contributed to some peak, GP >= peak_max (if peak built only from wins starting at 0).
// Peak starts 0. Peak = max running sum. Max running sum <= sum of all positive increments that occurred before that point <= GP.
// DD = peak - min_after. min_after = peak - losses_after_peak + wins_after_peak_before_trough.
// Worst trough after peak: peak - (losses after peak) + (wins after that don't recover yet).
// DD_max possible = sum of losses after a peak (minus intervening wins before trough).
// So DD <= GL always (losses only).
// DD > GP and NET>0 means DD > GP and GP > GL, so DD > GL. Contradiction if DD <= GL.
// Is DD always <= GL?
// Peak can include starting 0 and wins. Losses reduce equity. DD = drop from peak.
// The drop is composed of losses minus any wins during the drop. So DD <= sum of losses in that drop <= GL.
// Equality when the drop is pure losses.
// Therefore DD > GP and GP > 0 and NET > 0 is IMPOSSIBLE because DD <= GL < GP.
// UNLESS peak is 0 and we go negative then recover: DD = GL_prefix, GP = later wins, NET>0 => GP>GL >= DD so DD > GP is false.
// The code requires MaxDrawdown > GrossProfit AND GrossProfit > 0.
// So the DD>GP risk addend NEVER fires on a profitable book.
// It CAN fire when NET<=0 and GP>0 (some wins, more losses): DD can equal GL > GP.
// e.g. +10, -50, +10: equity 10, -40, -30. peak 10, dd 50. GP=20 GL=50. DD=50>20. NET=-30.
// With martingale and NET<0 → RISK_BLOCKED anyway.

Run("DD_GT_GP_UNPROFITABLE",
    T(1, 10, 0.10m), T(2, -50, 0.10m), T(3, 10, 0.13m)); // last size-up after loss? t2 is loss 0.10, t3 0.13. NET=-30 GP=20 DD=50

Run("HIGH_LOSS_CV",
    T(1, -10, 0.10m), T(2, -200, 0.13m), T(3, 400, 0.13m)); // losses 10,200 CV high, NET=+190

Run("HIGH_LOT_CV",
    T(1, -50, 0.01m), T(2, 200, 0.20m), T(3, 50, 0.01m)); // 20x is mart+esc, lot cv high

// --- evasions ---
Run("SPACER_BE",
    T(1, -50, 0.10m), T(2, 0, 0.10m), T(3, 200, 0.20m));

Run("SPACER_TINY_WIN",
    T(1, -50, 0.10m), T(2, 0.01m, 0.10m), T(3, 200, 0.20m));

Run("GEO_1.24x3",
    T(1, -50, 0.10m), T(2, -50, 0.124m), T(3, 200, 0.15376m));

Run("GEO_1.24x4",
    T(1, -40, 0.10m), T(2, -40, 0.124m), T(3, -40, 0.15376m), T(4, 200, 0.1906624m));

Run("SIZEUP_AFTER_WIN",
    T(1, 50, 0.10m), T(2, 50, 0.20m), T(3, 50, 0.40m));

Run("EURUSD_SPACER",
    T(1, -50, 0.10m, symbol: "XAUUSD"),
    T(2, 0, 1.00m, symbol: "EURUSD"),
    T(3, 200, 0.20m, symbol: "XAUUSD"),
    T(4, 50, 0.10m, symbol: "XAUUSD"));

Run("INCOMPLETE_SPACER",
    T(1, -50, 0.10m), T(2, -1, 0.10m, completed: false), T(3, 200, 0.20m), T(4, 50, 0.10m));

Run("N2_MILD_WIN",
    T(1, -50, 0.10m), T(2, 200, 0.13m));

Run("N2_MILD_LOSE",
    T(1, -50, 0.10m), T(2, -80, 0.13m));

Run("N4_EXPAND_MILD",
    T(1, -50, 0.10m), T(2, 200, 0.13m), T(3, 50, 0.10m), T(4, 50, 0.10m));

Run("MAXVOL_NOT_INITIAL",
    // size-up detected on MaxVolumeLots only — helper sets both equal
    T(1, -50, 0.10m), T(2, 200, 0.13m), T(3, 50, 0.10m));

Run("STACK_ALL_FLAGS_WIN",
    T(1, -50, 0.01m, sl: null, avg: true),
    T(2, -80, 0.20m, sl: null, avg: true),
    T(3, 400, 0.80m, sl: null, avg: true));

Run("STACK_ALL_STILL_Q70?",
    T(1, -20, 0.10m, sl: null, avg: true),
    T(2, 50, 0.16m, sl: null, avg: false),
    T(3, 80, 0.10m, sl: null));

Run("A22_CASE_A",
    T(1, 80, 0.10m), T(2, -40, 0.10m), T(3, 60, 0.10m));

// boundary sl 0.3 and 0.5 with N=10 one martingale
Run("N10_SL3_MILD",
    T(1, -50, 0.10m, sl: 2290m),
    T(2, 200, 0.13m, sl: 2290m),
    T(3, 20, 0.10m, sl: 2290m),
    T(4, 20, 0.10m, sl: null),
    T(5, 20, 0.10m, sl: null),
    T(6, 20, 0.10m, sl: null),
    T(7, 20, 0.10m, sl: null),
    T(8, 20, 0.10m, sl: null),
    T(9, 20, 0.10m, sl: null),
    T(10, 20, 0.10m, sl: null)); // sl 3/10=0.3  -- <0.3 is false, <0.5 true

Run("N10_SL2_MILD",
    T(1, -50, 0.10m, sl: 2290m),
    T(2, 200, 0.13m, sl: 2290m),
    T(3, 20, 0.10m, sl: null),
    T(4, 20, 0.10m, sl: null),
    T(5, 20, 0.10m, sl: null),
    T(6, 20, 0.10m, sl: null),
    T(7, 20, 0.10m, sl: null),
    T(8, 20, 0.10m, sl: null),
    T(9, 20, 0.10m, sl: null),
    T(10, 20, 0.10m, sl: null)); // 2/10=0.2 <0.3

// equal ClosedAt order flip
var sameClose = DateTimeOffset.UnixEpoch.AddHours(5);
Run("SAME_CLOSE_LOSS_THEN_BIG",
    T(1, -50, 0.10m, closedAt: sameClose),
    T(2, 200, 0.20m, closedAt: sameClose),
    T(3, 50, 0.10m, closedAt: sameClose.AddHours(1)));

Run("SAME_CLOSE_BIG_THEN_LOSS",
    T(2, 200, 0.20m, closedAt: sameClose),
    T(1, -50, 0.10m, closedAt: sameClose),
    T(3, 50, 0.10m, closedAt: sameClose.AddHours(1)));
