using System.Globalization;
using System.Reflection;
using TraderIntelligence.Domain.Entities;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;
using TraderIntelligence.Domain.Volume;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var r = new TradeReconstructor(VolumeConverter.Manager);
var scorer = new BaselineScorer();
var rt = typeof(ReconstructedTradeResult);
var ent = typeof(ReconstructedTrade);

Console.WriteLine(
    "META"
    + "\thasDirty=" + (rt.GetProperty("Dirty") is not null)
    + "\thasFailure=" + (rt.GetProperty("FailureCode") is not null || rt.GetProperty("Failure") is not null)
    + "\thasSeq=" + (rt.GetProperty("LifecycleSeq") is not null)
    + "\thasEligible=" + (rt.GetProperty("EligibleForFirstThree") is not null)
    + "\thasFirst3Keys=" + (rt.GetProperty("First3Keys") is not null)
    + "\tentityDirty=" + (ent.GetProperty("Dirty") is not null)
    + "\tentityEligible=" + (ent.GetProperty("EligibleForFirstThree") is not null)
    + "\tentityFailure=" + (ent.GetProperty("FailureCode") is not null)
    + "\tearlyN=" + BaselineScorer.EarlyScoreTradeCount
    + "\tisTradingBuyCanceled=" + new NormalizedDeal
    {
        BrokerId = "ACHIEVER", Login = 1, DealTicket = 1, OrderTicket = 1, PositionId = 1,
        SourceSymbol = "XAUUSD", Action = DealAction.BuyCanceled, Entry = DealEntry.In,
        VolumeNative = 1000, Price = 2400, Profit = 0, Commission = 0, Swap = 0,
        Time = DateTimeOffset.UnixEpoch
    }.IsTradingDeal
    + "\tisTradingSellCanceled=" + new NormalizedDeal
    {
        BrokerId = "ACHIEVER", Login = 1, DealTicket = 2, OrderTicket = 2, PositionId = 1,
        SourceSymbol = "XAUUSD", Action = DealAction.SellCanceled, Entry = DealEntry.Out,
        VolumeNative = 1000, Price = 2410, Profit = 0, Commission = 0, Swap = 0,
        Time = DateTimeOffset.UnixEpoch
    }.IsTradingDeal);

NormalizedDeal D(
    long ticket, long pos, DealAction a, DealEntry e, ulong vol, decimal px, decimal pnl, int t,
    string symbol = "XAUUSDm", DealReason? reason = null, string broker = "ACHIEVER", long login = 1,
    decimal comm = 0, decimal swap = 0) => new()
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
    Commission = comm,
    Swap = swap,
    Time = DateTimeOffset.UnixEpoch.AddMinutes(t),
    Reason = reason
};

(NormalizedDeal, NormalizedDeal) Round(long pos, int t, string symbol = "XAUUSDm", decimal pnl = 10m)
{
    var ticket = pos * 10;
    return (
        D(ticket, pos, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t, symbol),
        D(ticket + 1, pos, DealAction.Sell, DealEntry.Out, 1000, 2310m, pnl, t + 1, symbol));
}

void Dump(string id, IReadOnlyList<ReconstructedTradeResult> trades)
{
    var i = 0;
    foreach (var t in trades)
    {
        Console.WriteLine(
            $"{id}\ti={i++}\tpos={t.PositionId}\tcomp={t.Completed}\txau={t.IsXauUsd}"
            + $"\telig={t.EligibleForFirstThree}\tdir={t.Direction}\tcanon={t.CanonicalSymbol}"
            + $"\tsrc={t.SourceSymbol}\tinit={t.InitialVolumeLots}\tmax={t.MaxVolumeLots}"
            + $"\tclosed={t.ClosedVolumeLots}\trem={t.RemainingVolumeLots}\tnet={t.NetRealizedPnl}"
            + $"\tdeals={t.DealCount}\ttickets=[{string.Join(',', t.DealTickets)}]"
            + $"\tentry={t.EntryVwap}\texit={t.ExitVwap}");
    }
}

void Sum(string id, IReadOnlyList<NormalizedDeal> deals)
{
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var helperN = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var helperElig = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    var completed = trades.Where(t => t.Completed).ToList();
    var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
    var eligFalse = trades.Count(t => !t.EligibleForFirstThree);
    var score = scorer.Score(completedXau);
    Console.WriteLine(
        $"{id}\tn={trades.Count}\tcomp={completed.Count}\txauComp={completedXau.Count}"
        + $"\thelperN={helperN}\thelperElig={helperElig}"
        + $"\tinelig={eligFalse}"
        + $"\tscoreN={score.Features.CompletedXauTrades}\tscoreElig={score.EarlyScoreEligible}"
        + $"\tscoreNet={score.Features.NetPnl}\tstate={score.SuggestedState}");
    Dump(id, trades);
}

// UNIT — replica of Canceled_deal_on_a_position_excludes_it_from_first_three
{
    var deals = new List<NormalizedDeal>
    {
        D(1, 10, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        D(2, 10, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        D(3, 10, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 3),
        D(4, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 4),
        D(5, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 5),
        D(6, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 6),
        D(7, 30, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 7)
    };
    Sum("UNIT", deals);
}

// F17 — extra-ticket cancel on 5017 + clean 5018 (A21; native 1000 = 0.10 lots)
{
    var deals = new[]
    {
        D(961, 5017, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(962, 5017, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 2, "XAUUSD"),
        D(963, 5018, DealAction.Buy, DealEntry.In, 1000, 2401m, 0, 3, "XAUUSD"),
        D(964, 5018, DealAction.Sell, DealEntry.Out, 1000, 2411m, 10, 4, "XAUUSD")
    };
    Sum("F17", deals);
}

// F17_FLAT — extra-ticket cancel then later real OUT on same id
{
    var deals = new[]
    {
        D(961, 5017, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(962, 5017, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 2, "XAUUSD"),
        D(965, 5017, DealAction.Sell, DealEntry.Out, 1000, 2410m, 10, 3, "XAUUSD")
    };
    Sum("F17_FLAT", deals);
}

// F17b — official in-place: latest row is canceled only
{
    var deals = new[]
    {
        D(970, 5027, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD")
    };
    Sum("F17B", deals);
}

// F17c — surviving BUY IN + canceled extra scale-in + OUT
{
    var deals = new[]
    {
        D(971, 5028, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(972, 5028, DealAction.BuyCanceled, DealEntry.In, 2000, 2390m, 0, 2, "XAUUSD"),
        D(973, 5028, DealAction.Sell, DealEntry.Out, 1000, 2410m, 10, 3, "XAUUSD")
    };
    Sum("F17C", deals);
}

// F17d — close canceled → book should stay open
{
    var deals = new[]
    {
        D(981, 5029, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(982, 5029, DealAction.SellCanceled, DealEntry.Out, 1000, 2410m, 0, 2, "XAUUSD")
    };
    Sum("F17D", deals);
}

// F17e — clawback cash after in-place cancel
{
    var deals = new[]
    {
        D(970, 5027, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(983, 0, DealAction.Balance, DealEntry.In, 0, 0, -1m, 3, "")
    };
    Sum("F17E", deals);
}

// F17f — EURUSD cancel + clean XAU
{
    var deals = new List<NormalizedDeal>
    {
        D(984, 9, DealAction.BuyCanceled, DealEntry.In, 10000, 1.10m, 0, 1, "EURUSD")
    };
    var (a, b) = Round(40, 10, "XAUUSD");
    deals.Add(a); deals.Add(b);
    Sum("F17F", deals);
}

// F17g — 3 clean then retract #3 close to SellCanceled (latest-per-ticket encoding)
{
    var deals = new List<NormalizedDeal>();
    for (var i = 0; i < 3; i++)
    {
        deals.Add(D(10 + i * 2, 100 + i, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, i * 2 + 1));
        deals.Add(D(11 + i * 2, 100 + i, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, i * 2 + 2));
    }
    Sum("F17G_BEFORE", deals);
    deals[5] = D(15, 102, DealAction.SellCanceled, DealEntry.Out, 1000, 2310m, 0, 6);
    Sum("F17G_AFTER", deals);
}

// SELL_CANCELED extra-ticket on completed long
{
    var deals = new[]
    {
        D(1, 10, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        D(2, 10, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        D(3, 10, DealAction.SellCanceled, DealEntry.Out, 1000, 2310m, 0, 3),
        D(4, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 4),
        D(5, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 5),
        D(6, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 6),
        D(7, 30, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 7)
    };
    Sum("SELL_CXL", deals);
}

// NETTING reuse: complete pos 50, then later cancel, then new lifecycle on same pos
{
    var deals = new[]
    {
        D(1, 50, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        D(2, 50, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        D(3, 50, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 3),
        D(4, 50, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, 4),
        D(5, 50, DealAction.Sell, DealEntry.Out, 1000, 2330m, 10, 5)
    };
    Sum("NETTING", deals);
}

// POS0 cancel must not taint other books
{
    var deals = new List<NormalizedDeal>
    {
        D(99, 0, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 1)
    };
    var (a, b) = Round(60, 2);
    var (c, d) = Round(61, 4);
    var (e, f) = Round(62, 6);
    deals.AddRange(new[] { a, b, c, d, e, f });
    Sum("POS0", deals);
}

// OTHER_LOGIN cancel must not taint login 1
{
    var deals = new List<NormalizedDeal>
    {
        D(3, 10, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 3, login: 2)
    };
    var (a, b) = Round(10, 1);
    var (c, d) = Round(20, 4);
    var (e, f) = Round(30, 6);
    deals.AddRange(new[] { a, b, c, d, e, f });
    Sum("OTHER_LOGIN", deals);
}

// OTHER_BROKER cancel
{
    var deals = new List<NormalizedDeal>
    {
        D(3, 10, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 3, broker: "STARWAVE")
    };
    var (a, b) = Round(10, 1);
    var (c, d) = Round(20, 4);
    var (e, f) = Round(30, 6);
    deals.AddRange(new[] { a, b, c, d, e, f });
    Sum("OTHER_BROKER", deals);
}

// REASON on canceled row (CorporateAction) still dirties
{
    var deals = new[]
    {
        D(1, 70, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        D(2, 70, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        D(3, 70, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 3, reason: DealReason.CorporateAction)
    };
    Sum("REASON", deals);
}

// EMPTY_SYM cancel with position already XAU
{
    var deals = new[]
    {
        D(1, 80, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        D(2, 80, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 2, symbol: "")
    };
    Sum("EMPTY_SYM", deals);
}

// M5 score leak: 2 clean + 1 canceled-tainted complete
{
    var deals = new List<NormalizedDeal>();
    var (a, b) = Round(1, 1);
    var (c, d) = Round(2, 3);
    deals.AddRange(new[] { a, b, c, d });
    deals.Add(D(31, 3, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 5));
    deals.Add(D(32, 3, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 6));
    deals.Add(D(33, 3, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 7));
    Sum("M5", deals);
}

// Inverse check: do not subtract canceled volume
{
    var deals = new[]
    {
        D(1, 90, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1, "XAUUSD"),
        D(2, 90, DealAction.BuyCanceled, DealEntry.In, 2000, 2390m, 0, 2, "XAUUSD")
    };
    Sum("NO_INVERSE", deals);
}

// Dashboard-style first-3 on persisted-shape (Completed && XAU, ignore elig)
{
    var deals = new List<NormalizedDeal>();
    var (a, b) = Round(1, 1);
    var (c, d) = Round(2, 3);
    deals.AddRange(new[] { a, b, c, d });
    deals.Add(D(31, 3, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 5));
    deals.Add(D(32, 3, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 6));
    deals.Add(D(33, 3, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, 7));
    var trades = r.Reconstruct("ACHIEVER", 1, deals).OrderBy(t => t.ClosedAt ?? t.OpenedAt).ToList();
    var firstThree = 0;
    foreach (var t in trades)
    {
        var first = t.Completed && t.CanonicalSymbol == "XAUUSD" && firstThree < 3;
        if (first) firstThree++;
        Console.WriteLine($"DASH\tpos={t.PositionId}\tcomp={t.Completed}\telig={t.EligibleForFirstThree}\tfirst={first}");
    }
    Console.WriteLine($"DASH\thighlighted={firstThree}");
}

// Persist drop: entity property census
{
    var persistNames = typeof(ReconstructedTrade).GetProperties().Select(p => p.Name).OrderBy(x => x);
    Console.WriteLine("ENTITY\t" + string.Join(",", persistNames));
    var resultNames = typeof(ReconstructedTradeResult).GetProperties().Select(p => p.Name).OrderBy(x => x);
    Console.WriteLine("RESULT\t" + string.Join(",", resultNames));
}
