using System.Globalization;
using System.Reflection;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Volume;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var r = new TradeReconstructor(VolumeConverter.Manager);
var rt = typeof(ReconstructedTradeResult);
Console.WriteLine("META\tlines=334\thasDirty=" + (rt.GetProperty("Dirty") is not null)
    + "\thasFailure=" + (rt.GetProperty("FailureCode") is not null)
    + "\thasSeq=" + (rt.GetProperty("LifecycleSeq") is not null)
    + "\tzeroLots=" + VolumeConverter.Manager.ToLots(0)
    + "\tnative1=" + VolumeConverter.Manager.ToLots(1));

NormalizedDeal D(
    long ticket, long pos, DealAction a, DealEntry e, ulong vol, decimal px, decimal pnl, int t,
    string symbol = "XAUUSDm", decimal comm = 0, decimal swap = 0, string broker = "ACHIEVER",
    long login = 1, decimal? sl = null, decimal? tp = null) => new()
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
    Time = DateTimeOffset.UnixEpoch.AddMilliseconds(t),
    StopLoss = sl,
    TakeProfit = tp
};

void Dump(string id, IReadOnlyList<ReconstructedTradeResult> trades, string extra = "")
{
    if (trades.Count == 0)
    {
        Console.WriteLine($"{id}\tEMPTY\t{extra}");
        return;
    }
    var i = 0;
    foreach (var t in trades)
    {
        Console.WriteLine(
            $"{id}\ti={i++}\tid={t.Id}\tpos={t.PositionId}\tdir={t.Direction}\tcomp={t.Completed}" +
            $"\txau={t.IsXauUsd}\tinit={t.InitialVolumeLots}\tmax={t.MaxVolumeLots}" +
            $"\tclosed={t.ClosedVolumeLots}\trem={t.RemainingVolumeLots}" +
            $"\tentry={t.EntryVwap}\texit={t.ExitVwap}\tgross={t.GrossRealizedPnl}" +
            $"\tcomm={t.Commission}\tswap={t.Swap}\tfees={t.Fees}\tnet={t.NetRealizedPnl}" +
            $"\tdeals={t.DealCount}\torders={t.OrderCount}\ttickets=[{string.Join(',', t.DealTickets)}]" +
            $"\tscale={t.WasScaledIn}\tpart={t.WasPartialClose}\tavg={t.WasAveragedDown}" +
            $"\topened={t.OpenedAt.ToUnixTimeMilliseconds()}\tclosedAt={(t.ClosedAt?.ToUnixTimeMilliseconds().ToString() ?? "null")}" +
            $"\tcanon={t.CanonicalSymbol}\tsrc={t.SourceSymbol}\tbroker={t.BrokerId}");
    }
    if (!string.IsNullOrEmpty(extra))
        Console.WriteLine($"{id}\textra\t{extra}");
}

// B1 F04 INOUT money double-apply (A21 leftover net=0)
{
    var deals = new[]
    {
        D(401, 5004, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(402, 5004, DealAction.Sell, DealEntry.InOut, 15000, 2410m, 10m, 2000, comm: -1.05m)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("B1_F04_INOUT_MONEY", trades);
}

// B2 F05 close leftover after reverse
{
    var deals = new[]
    {
        D(401, 5004, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(402, 5004, DealAction.Sell, DealEntry.InOut, 15000, 2410m, 10m, 2000, comm: -1.05m),
        D(403, 5004, DealAction.Buy, DealEntry.Out, 5000, 2390m, 10m, 3000, comm: -0.35m)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var sum = trades.Where(t => t.Completed).Sum(t => t.NetRealizedPnl);
    Dump("B2_F05_REVERSE_CLOSE", trades, $"completedNetSum={sum}");
}

// B3 Opposite ENTRY_IN discards closed long
{
    var deals = new[]
    {
        D(981, 5019, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(982, 5019, DealAction.Sell, DealEntry.In, 5000, 2410m, 10m, 2000)
    };
    Dump("B3_OPPOSITE_IN", r.Reconstruct("ACHIEVER", 1, deals));
}

// B4 INOUT volume < remaining (should not flatten all)
{
    var deals = new[]
    {
        D(1, 10, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 10, DealAction.Sell, DealEntry.InOut, 3000, 2410m, 3m, 2000)
    };
    Dump("B4_INOUT_UNDERVOL", r.Reconstruct("ACHIEVER", 1, deals));
}

// B5 OUT overclose clip
{
    var deals = new[]
    {
        D(971, 5018, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(972, 5018, DealAction.Sell, DealEntry.Out, 15000, 2410m, 10m, 2000)
    };
    Dump("B5_OUT_OVERCLOSE", r.Reconstruct("ACHIEVER", 1, deals));
}

// B6 same-sign INOUT phantom complete
{
    var deals = new[]
    {
        D(1, 20, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 20, DealAction.Buy, DealEntry.InOut, 15000, 2410m, 0, 2000)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("B6_SAMESIGN_INOUT", trades, "countCompleted=" + trades.Count(t => t.Completed));
}

// B7 F20 INOUT exact flatten (no new volume)
{
    var deals = new[]
    {
        D(991, 5020, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(992, 5020, DealAction.Sell, DealEntry.InOut, 10000, 2410m, 10m, 2000)
    };
    Dump("B7_INOUT_EXACT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B8 F16 duplicate ticket replay
{
    var a = D(101, 5001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m);
    var b = D(102, 5001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000, comm: -0.70m, swap: -0.20m);
    var trades = r.Reconstruct("ACHIEVER", 1, new[] { a, b, a, b });
    Dump("B8_F16_DUP_REPLAY", trades, "n=" + trades.Count + " completed=" + trades.Count(t => t.Completed));
}

// B9 same-millisecond reopen Id collision
{
    var deals = new[]
    {
        D(1, 77, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 77, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 1000),
        D(3, 77, DealAction.Buy, DealEntry.In, 10000, 2412m, 0, 1000)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var ids = trades.Select(t => t.Id).ToList();
    Dump("B9_SAME_MS_ID", trades, "uniqueIds=" + ids.Distinct().Count() + " n=" + ids.Count);
}

// B10 zero-volume OUT then real OUT
{
    var deals = new[]
    {
        D(1, 13, DealAction.Buy, DealEntry.In, 10000, 2300m, 0, 1000),
        D(2, 13, DealAction.Sell, DealEntry.Out, 0, 2310m, 99m, 2000),
        D(3, 13, DealAction.Sell, DealEntry.Out, 10000, 2320m, 20m, 3000)
    };
    Dump("B10_ZERO_VOL_OUT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B11 canceled extra ticket then flatten
{
    var deals = new[]
    {
        D(961, 5017, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(962, 5017, DealAction.BuyCanceled, DealEntry.In, 10000, 2400m, 0, 1100),
        D(965, 5017, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000)
    };
    Dump("B11_CANCELED", r.Reconstruct("ACHIEVER", 1, deals));
}

// B12 position_id == 0
{
    var deals = new[]
    {
        D(995, 0, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1000),
        D(996, 0, DealAction.Sell, DealEntry.Out, 1000, 2410m, 1m, 2000)
    };
    Dump("B12_POS0", r.Reconstruct("ACHIEVER", 1, deals));
}

// B13 price == 0
{
    var deals = new[]
    {
        D(1, 88, DealAction.Buy, DealEntry.In, 10000, 0m, 0, 1000),
        D(2, 88, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000)
    };
    Dump("B13_PRICE0", r.Reconstruct("ACHIEVER", 1, deals));
}

// B14 unknown entry 255
{
    var deals = new[]
    {
        D(1, 89, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 89, DealAction.Sell, (DealEntry)255, 10000, 2410m, 10m, 2000)
    };
    Dump("B14_UNKNOWN_ENTRY", r.Reconstruct("ACHIEVER", 1, deals));
}

// B15 same-sign OUT
{
    var deals = new[]
    {
        D(1, 90, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 90, DealAction.Buy, DealEntry.Out, 4000, 2410m, 4m, 2000)
    };
    Dump("B15_SAMESIGN_OUT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B16 INOUT on flat
{
    var deals = new[]
    {
        D(1, 91, DealAction.Sell, DealEntry.InOut, 10000, 2400m, 0, 1000)
    };
    Dump("B16_INOUT_FLAT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B17 OUT on flat
{
    var deals = new[]
    {
        D(1, 92, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 1000)
    };
    Dump("B17_OUT_FLAT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B18 first-3 poison: two clean + same-sign INOUT
{
    var deals = new List<NormalizedDeal>
    {
        D(1, 1, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 1, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 1100),
        D(3, 2, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 2000),
        D(4, 2, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2100),
        D(5, 3, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 3000),
        D(6, 3, DealAction.Buy, DealEntry.InOut, 15000, 2410m, 0, 3100)
    };
    var n = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var elig = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    Dump("B18_FIRST3_PHANTOM", r.Reconstruct("ACHIEVER", 1, deals), $"count={n} eligible={elig}");
}

// B19 duplicate IN then one OUT (double scale)
{
    var a = D(101, 93, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000);
    var b = D(102, 93, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000);
    Dump("B19_DUP_IN", r.Reconstruct("ACHIEVER", 1, new[] { a, a, b }));
}

// B20 sub-0.01 lot (native 1)
{
    var deals = new[]
    {
        D(1, 94, DealAction.Buy, DealEntry.In, 1, 2400m, 0, 1000),
        D(2, 94, DealAction.Sell, DealEntry.Out, 1, 2410m, 0.01m, 2000)
    };
    Dump("B20_SUB_LOT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B21 XAG + XAUUSD.a
{
    var deals = new[]
    {
        D(996, 8801, DealAction.Buy, DealEntry.In, 10000, 30m, 0, 1000, symbol: "XAGUSD"),
        D(997, 8801, DealAction.Sell, DealEntry.Out, 10000, 31m, 100m, 2000, symbol: "XAGUSD"),
        D(998, 8802, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 3000, symbol: "XAUUSD.a"),
        D(999, 8802, DealAction.Sell, DealEntry.Out, 1000, 2410m, 1m, 4000, symbol: "XAUUSD.a")
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("B21_F24_SYMBOL", trades, "completedXau=" + r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals));
}

// B22 permissive XAUUSD suffix
{
    var deals = new[]
    {
        D(1, 95, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, symbol: "XAUUSDFUT"),
        D(2, 95, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000, symbol: "XAUUSDFUT")
    };
    Dump("B22_PERMISSIVE_XAU", r.Reconstruct("ACHIEVER", 1, deals),
        "countXau=" + r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals));
}

// B23 mixed symbols same position_id
{
    var deals = new[]
    {
        D(1, 96, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, symbol: "XAUUSDm"),
        D(2, 96, DealAction.Sell, DealEntry.Out, 10000, 31m, 10m, 2000, symbol: "XAGUSD")
    };
    Dump("B23_MIXED_SYMBOL_POS", r.Reconstruct("ACHIEVER", 1, deals));
}

// B24 opposite IN leftover formula: remaining 1.00, sell IN 1.50
{
    var deals = new[]
    {
        D(1, 97, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 97, DealAction.Sell, DealEntry.In, 15000, 2410m, 10m, 2000)
    };
    Dump("B24_OPP_IN_1p5", r.Reconstruct("ACHIEVER", 1, deals));
}

// B25 fees always 0 even with profit
{
    var deals = new[]
    {
        D(1, 98, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m, swap: -0.20m),
        D(2, 98, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000, comm: -0.70m)
    };
    Dump("B25_FEES", r.Reconstruct("ACHIEVER", 1, deals));
}

// B26 SL=0 vs null
{
    var deals = new[]
    {
        D(1, 99, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, sl: 0m, tp: 2500m),
        D(2, 99, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000, sl: 0m)
    };
    var t = r.Reconstruct("ACHIEVER", 1, deals)[0];
    Console.WriteLine($"B26_SL0\tinitSl={t.InitialSl}\tfinalSl={t.FinalSl}\tinitTp={t.InitialTp}\tfinalTp={t.FinalTp}");
}

// B27 first-3 close-time tie (no ticket tiebreak)
{
    var deals = new[]
    {
        D(10, 201, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(20, 202, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1100),
        D(11, 201, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 5000),
        D(21, 202, DealAction.Sell, DealEntry.Out, 10000, 2410m, 20m, 5000)
    };
    var ordered = r.CompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("B27_CLOSE_TIE", ordered);
}

// B28 caller broker casing becomes identity
{
    var deals = new[]
    {
        D(1, 203, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 203, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000)
    };
    var a = r.Reconstruct("ACHIEVER", 1, deals);
    var b = r.Reconstruct("achiever", 1, deals);
    Console.WriteLine($"B28_CASE\tidA={a[0].Id}\tidB={b[0].Id}\tbrokerA={a[0].BrokerId}\tbrokerB={b[0].BrokerId}");
}

// B29 reconstruct null list
try
{
    _ = r.Reconstruct("ACHIEVER", 1, null!);
    Console.WriteLine("B29_NULL\tNO_THROW");
}
catch (Exception ex)
{
    Console.WriteLine($"B29_NULL\t{ex.GetType().Name}\t{ex.Message}");
}

// B30 F01 happy path sanity
{
    var deals = new[]
    {
        D(101, 5001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(102, 5001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000, comm: -0.70m, swap: -0.20m)
    };
    Dump("B30_F01", r.Reconstruct("ACHIEVER", 1, deals));
}

// B31 scale-in VWAP F02
{
    var deals = new[]
    {
        D(201, 5002, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(202, 5002, DealAction.Buy, DealEntry.In, 5000, 2410m, 0, 1500, comm: -0.35m),
        D(203, 5002, DealAction.Sell, DealEntry.Out, 15000, 2420m, 25m, 2000, comm: -1.05m)
    };
    Dump("B31_F02", r.Reconstruct("ACHIEVER", 1, deals));
}

// B32 F11 netting reuse
{
    var deals = new[]
    {
        D(911, 7001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(912, 7001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2000),
        D(913, 7001, DealAction.Sell, DealEntry.In, 8000, 2412m, 0, 3000),
        D(914, 7001, DealAction.Buy, DealEntry.Out, 8000, 2402m, 8m, 4000)
    };
    Dump("B32_F11", r.Reconstruct("ACHIEVER", 1, deals));
}

// B33 sort OUT before IN in list
{
    var deals = new[]
    {
        D(952, 5015, DealAction.Sell, DealEntry.Out, 1000, 2410m, 1m, 2000),
        D(951, 5015, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1000)
    };
    Dump("B33_F15_SORT", r.Reconstruct("ACHIEVER", 1, deals));
}

// B34 hedge two position ids OUT_BY
{
    var deals = new[]
    {
        D(801, 6001, DealAction.Buy, DealEntry.In, 5000, 2400m, 0, 1000, comm: -0.35m),
        D(802, 6002, DealAction.Sell, DealEntry.In, 5000, 2410m, 0, 1100, comm: -0.35m),
        D(803, 6001, DealAction.Sell, DealEntry.OutBy, 5000, 2408m, 4m, 2000, comm: -0.35m),
        D(804, 6002, DealAction.Buy, DealEntry.OutBy, 5000, 2408m, 1m, 2000, comm: -0.35m)
    };
    Dump("B34_F09", r.Reconstruct("ACHIEVER", 1, deals));
}

// B35 short avg-down
{
    var deals = new[]
    {
        D(711, 5008, DealAction.Sell, DealEntry.In, 10000, 2400m, 0, 1000),
        D(712, 5008, DealAction.Sell, DealEntry.In, 5000, 2415m, 0, 1500),
        D(713, 5008, DealAction.Buy, DealEntry.Out, 15000, 2405m, -7.5m, 2000)
    };
    Dump("B35_F08_SHORT_AVG", r.Reconstruct("ACHIEVER", 1, deals));
}

// B36 long add higher not avg-down
{
    var deals = new[]
    {
        D(201, 5002, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(202, 5002, DealAction.Buy, DealEntry.In, 5000, 2410m, 0, 1500),
        D(203, 5002, DealAction.Sell, DealEntry.Out, 15000, 2420m, 25m, 2000)
    };
    Dump("B36_F02_NOT_AVG", r.Reconstruct("ACHIEVER", 1, deals));
}

// B37 INOUT leftover tickets listed on both
{
    var deals = new[]
    {
        D(401, 5004, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(402, 5004, DealAction.Sell, DealEntry.InOut, 15000, 2410m, 10m, 2000)
    };
    Dump("B37_TICKET_BOTH", r.Reconstruct("ACHIEVER", 1, deals));
}

// B38 partial then remainder one trade
{
    var deals = new[]
    {
        D(301, 5003, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(302, 5003, DealAction.Sell, DealEntry.Out, 4000, 2410m, 4m, 1400, comm: -0.28m),
        D(303, 5003, DealAction.Sell, DealEntry.Out, 6000, 2420m, 12m, 2000, comm: -0.42m)
    };
    Dump("B38_F03", r.Reconstruct("ACHIEVER", 1, deals));
}

// B39 first-3 count after cancel-tainted complete + 2 clean
{
    var deals = new List<NormalizedDeal>
    {
        D(1, 1, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000),
        D(2, 1, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 1100),
        D(3, 2, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 2000),
        D(4, 2, DealAction.BuyCanceled, DealEntry.In, 10000, 2400m, 0, 2050),
        D(5, 2, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 2100),
        D(6, 3, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 3000),
        D(7, 3, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10m, 3100)
    };
    Console.WriteLine($"B39_CANCEL_FIRST3\tcount={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}\teligible={r.IsEarlyScoreEligible("ACHIEVER", 1, deals)}");
}

// B40 INOUT leftover then score-relevant money
{
    var deals = new[]
    {
        D(1, 40, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1000, comm: -0.70m),
        D(2, 40, DealAction.Sell, DealEntry.InOut, 15000, 2410m, 10m, 2000, comm: -1.05m),
        D(3, 40, DealAction.Buy, DealEntry.Out, 5000, 2390m, 10m, 3000, comm: -0.35m)
    };
    var completed = r.Reconstruct("ACHIEVER", 1, deals).Where(t => t.Completed && t.IsXauUsd).ToList();
    Console.WriteLine($"B40_MONEY_SUM\tn={completed.Count}\tsumNet={completed.Sum(t => t.NetRealizedPnl)}\tspec=17.90");
}

// B41 Open only does not count
{
    var deals = new[] { D(931, 5013, DealAction.Buy, DealEntry.In, 2500, 2400m, 0, 1000) };
    Console.WriteLine($"B41_OPEN_ONLY\tn={r.Reconstruct("ACHIEVER", 1, deals).Count}\tcompleted={r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals)}");
}

// B42 balance skip
{
    var deals = new[]
    {
        D(941, 0, DealAction.Balance, DealEntry.In, 0, 0, 10000m, 500),
        D(942, 5014, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1000),
        D(943, 0, DealAction.Commission, DealEntry.In, 0, 0, -2m, 1100),
        D(944, 5014, DealAction.Sell, DealEntry.Out, 1000, 2410m, 1m, 2000)
    };
    Dump("B42_F14", r.Reconstruct("ACHIEVER", 1, deals));
}

Console.WriteLine("DONE");
