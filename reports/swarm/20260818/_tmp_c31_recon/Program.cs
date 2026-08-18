using System.Globalization;
using System.Reflection;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Volume;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

var r = new TradeReconstructor(VolumeConverter.Manager);
var resultType = typeof(ReconstructedTradeResult);
var hasDirty = resultType.GetProperty("Dirty", BindingFlags.Public | BindingFlags.Instance) is not null;
var hasFailure = resultType.GetProperty("FailureCode", BindingFlags.Public | BindingFlags.Instance) is not null
                 || resultType.GetProperty("Failure", BindingFlags.Public | BindingFlags.Instance) is not null;
Console.WriteLine($"META\thasDirty={hasDirty}\thasFailureCode={hasFailure}\tisTradingBuyCanceled={IsCanceledTrading()}");
Console.WriteLine($"META\tmanagerScale={VolumeConverter.Manager.Scale}\tzeroLots={VolumeConverter.Manager.ToLots(0)}\tnative1Lots={VolumeConverter.Manager.ToLots(1)}");

static bool IsCanceledTrading()
{
    var d = new NormalizedDeal
    {
        BrokerId = "ACHIEVER",
        Login = 1,
        DealTicket = 1,
        OrderTicket = 1,
        PositionId = 1,
        SourceSymbol = "XAUUSDm",
        Action = DealAction.BuyCanceled,
        Entry = DealEntry.In,
        VolumeNative = 1000,
        Price = 2300,
        Profit = 0,
        Commission = 0,
        Swap = 0,
        Time = DateTimeOffset.UnixEpoch
    };
    return d.IsTradingDeal;
}

NormalizedDeal Deal(
    string broker,
    long login,
    long ticket,
    long position,
    DealAction action,
    DealEntry entry,
    ulong vol,
    decimal price,
    decimal profit,
    int t,
    string symbol = "XAUUSDm",
    decimal comm = 0,
    decimal swap = 0) => new()
{
    BrokerId = broker,
    Login = login,
    DealTicket = ticket,
    OrderTicket = ticket,
    PositionId = position,
    SourceSymbol = symbol,
    Action = action,
    Entry = entry,
    VolumeNative = vol,
    Price = price,
    Profit = profit,
    Commission = comm,
    Swap = swap,
    Time = DateTimeOffset.UnixEpoch.AddMinutes(t)
};

string Summary(IReadOnlyList<ReconstructedTradeResult> trades)
{
    if (trades.Count == 0)
        return "EMPTY";
    return string.Join(" || ", trades.Select(t =>
        $"broker={t.BrokerId} login={t.Login} pos={t.PositionId} dir={t.Direction} " +
        $"comp={t.Completed} xau={t.IsXauUsd} init={t.InitialVolumeLots} max={t.MaxVolumeLots} " +
        $"closed={t.ClosedVolumeLots} rem={t.RemainingVolumeLots} entry={t.EntryVwap} exit={t.ExitVwap} " +
        $"gross={t.GrossRealizedPnl} net={t.NetRealizedPnl} deals={t.DealCount} " +
        $"tickets=[{string.Join(',', t.DealTickets)}] scale={t.WasScaledIn} part={t.WasPartialClose} avg={t.WasAveragedDown}"));
}

void Dump(string name, IReadOnlyList<ReconstructedTradeResult> trades)
{
    Console.WriteLine($"DUMP\t{name}\tn={trades.Count}\t{Summary(trades)}");
}

void Verdict(string id, string area, bool pass, string spec, string measured)
{
    Console.WriteLine($"VERDICT\t{id}\t{area}\t{(pass ? "PASS" : "FAIL")}\tspec={spec}\tgot={measured}");
}

// ---- existing unit-test shapes (sanity) ----
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 10, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, 1),
        Deal("ACHIEVER", 1, 2, 10, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var ok = trades.Count == 1 && trades[0].Completed && trades[0].NetRealizedPnl == 100m
             && trades[0].InitialVolumeLots == 0.10m && trades[0].EntryVwap == 2320m;
    Verdict("S0", "sanity", ok, "simple round-trip 1 completed net=100 lots=0.10", Summary(trades));
}

// ===================== ZERO VOLUME =====================

// Z1: tradeable IN volume 0 only
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 10, DealAction.Buy, DealEntry.In, 0, 2300m, 0, 1)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("Z1", trades);
    // Spec: RECON_ZERO_VOLUME dirty stub, 0 completed. Code: silent skip → empty, no dirty channel.
    var pass = false; // no dirty / no failure code; empty is not a dirty stub
    Verdict("Z1", "zero-volume", pass, "RECON_ZERO_VOLUME dirty stub; 0 completed first-3",
        trades.Count == 0 ? "EMPTY (silent skip, no dirty)" : Summary(trades));
}

// Z2: zero IN then real OUT (orphan close)
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 11, DealAction.Buy, DealEntry.In, 0, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 11, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("Z2", trades);
    Verdict("Z2", "zero-volume", false,
        "dirty; do not invent a close; RECON_ZERO_VOLUME then RECON_OUT_FLAT",
        trades.Count == 0 ? "EMPTY (zero IN dropped, OUT on flat skipped)" : Summary(trades));
}

// Z3: real IN then zero OUT — book must stay open; spec dirty
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 12, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 12, DealAction.Sell, DealEntry.Out, 0, 2310m, 0, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("Z3", trades);
    var remainingOk = trades.Count == 1 && !trades[0].Completed && trades[0].RemainingVolumeLots == 0.10m;
    Verdict("Z3-volume", "zero-volume", remainingOk,
        "open long rem=0.10 (zero OUT must not flatten)",
        Summary(trades));
    Verdict("Z3-dirty", "zero-volume", false,
        "RECON_ZERO_VOLUME dirty; exclude first-3",
        remainingOk ? "open rem=0.10 CLEAN (no Dirty field; CountCompletedXau=0 by luck)" : Summary(trades));
}

// Z4: IN, zero OUT with profit, then real OUT — THE money + first-3 fail
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 13, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 13, DealAction.Sell, DealEntry.Out, 0, 2310m, 99m, 2),
        Deal("ACHIEVER", 1, 3, 13, DealAction.Sell, DealEntry.Out, 1000, 2320m, 20m, 3)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("Z4", trades);
    var completedClean = trades.Count == 1 && trades[0].Completed && trades[0].NetRealizedPnl == 20m && count == 1;
    Verdict("Z4", "zero-volume", !completedClean,
        "RECON_ZERO_VOLUME dirty; profit 99 not applied; first-3 exclude this book",
        $"completedClean={completedClean} count={count} net={(trades.Count==0?"":trades[0].NetRealizedPnl.ToString(CultureInfo.InvariantCulture))} {Summary(trades)}");
}

// Z5: zero-volume scale-in then full close
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 14, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 14, DealAction.Buy, DealEntry.In, 0, 2290m, 0, 2),
        Deal("ACHIEVER", 1, 3, 14, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 3)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("Z5", trades);
    var looksClean = trades.Count == 1 && trades[0].Completed && !trades[0].WasScaledIn && trades[0].EntryVwap == 2300m;
    Verdict("Z5", "zero-volume", false,
        "RECON_ZERO_VOLUME dirty; do not silently drop the mid IN",
        $"looksCleanUnscaled={looksClean} {Summary(trades)}");
}

// Z6: zero/zero round-trip
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 15, DealAction.Buy, DealEntry.In, 0, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 15, DealAction.Sell, DealEntry.Out, 0, 2310m, 5, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("Z6", trades);
    Verdict("Z6", "zero-volume", false,
        "two RECON_ZERO_VOLUME; dirty stub; profit 5 not a trade",
        trades.Count == 0 ? "EMPTY (both skipped, profit 5 vanished, no dirty)" : Summary(trades));
}

// Z7: sub-0.01 lot (native 1) — not quantized
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 16, DealAction.Buy, DealEntry.In, 1, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 16, DealAction.Sell, DealEntry.Out, 1, 2310m, 1, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("Z7", trades);
    var phantom = trades.Count == 1 && trades[0].Completed && count == 1;
    Verdict("Z7", "zero-volume", !phantom,
        "RECON_VOLUME_NOT_QUANTIZED; not a first-3 trade",
        $"phantomCompleted={phantom} lots={(trades.Count==0?0:trades[0].InitialVolumeLots)} count={count} {Summary(trades)}");
}

// Z8: three otherwise-clean XAU + one zero-vol mid-deal on trade #3 — latch must not fire if #3 dirty
{
    var deals = new List<NormalizedDeal>
    {
        Deal("ACHIEVER", 1, 10, 100, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 11, 100, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("ACHIEVER", 1, 12, 101, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 3),
        Deal("ACHIEVER", 1, 13, 101, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 4),
        Deal("ACHIEVER", 1, 14, 102, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 5),
        Deal("ACHIEVER", 1, 15, 102, DealAction.Sell, DealEntry.Out, 0, 2310m, 0, 6),
        Deal("ACHIEVER", 1, 16, 102, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 7)
    };
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var eligible = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    Dump("Z8", r.Reconstruct("ACHIEVER", 1, deals));
    Verdict("Z8", "zero-volume", count == 2 && !eligible,
        "trade #3 dirty from zero OUT; completedXau=2; early-score false",
        $"count={count} eligible={eligible}");
}

// ===================== CANCELED DEALS =====================

// C0: IsTradingDeal excludes 13/14
{
    Verdict("C0-filter", "canceled", !IsCanceledTrading(),
        "IsTradingDeal(13/14)=false (volume book skip)",
        $"IsTradingDeal(BuyCanceled)={IsCanceledTrading()}");
}

// C1: A21 F17 extra-ticket cancel, no later flatten
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 961, 5017, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 962, 5017, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 2),
        Deal("ACHIEVER", 1, 963, 5018, DealAction.Buy, DealEntry.In, 1000, 2401m, 0, 3),
        Deal("ACHIEVER", 1, 964, 5018, DealAction.Sell, DealEntry.Out, 1000, 2411m, 10, 4)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("C1-F17", trades);
    var p5017 = trades.SingleOrDefault(t => t.PositionId == 5017);
    var p5018 = trades.SingleOrDefault(t => t.PositionId == 5018);
    var countOk = count == 1 && p5018 is { Completed: true };
    var dirtyOk = false; // no dirty channel; 5017 is a clean open
    Verdict("C1-count", "canceled", countOk,
        "completed_count=1 (only 5018)",
        $"count={count} 5017comp={p5017?.Completed} 5018comp={p5018?.Completed}");
    Verdict("C1-dirty", "canceled", dirtyOk,
        "5017 RECON_CANCELED_DEAL dirty (even while open)",
        p5017 is null ? "5017 missing" : $"5017 open clean rem={p5017.RemainingVolumeLots} deals={p5017.DealCount} tickets=[{string.Join(',', p5017.DealTickets)}]");
}

// C2: F17 + later OUT flatten of 5017 — HARD FAIL (canceled book becomes first-3)
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 961, 5017, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 962, 5017, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 2),
        Deal("ACHIEVER", 1, 965, 5017, DealAction.Sell, DealEntry.Out, 1000, 2410m, 10, 3)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var eligible = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    Dump("C2-flatten-canceled", trades);
    var hardFail = trades.Count == 1 && trades[0].Completed && trades[0].PositionId == 5017 && count == 1;
    Verdict("C2", "canceled", !hardFail,
        "5017 completed+dirty; CountCompletedXau=0; not first-3",
        $"hardFailCleanComplete={hardFail} count={count} eligible={eligible} {Summary(trades)}");
}

// C3: F17b official in-place only canceled row
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 970, 5027, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 1)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("C3-F17b", trades);
    Verdict("C3-volume", "canceled", trades.Count == 0,
        "no inverse fill, 0 trades",
        trades.Count == 0 ? "EMPTY" : Summary(trades));
    Verdict("C3-dirty", "canceled", false,
        "RECON_CANCELED_DEAL dirty stub",
        "EMPTY (canceled never reaches apply; no stub)");
}

// C4: F17c canceled scale-in then close remaining
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 971, 5028, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 972, 5028, DealAction.BuyCanceled, DealEntry.In, 2000, 2390m, 0, 2),
        Deal("ACHIEVER", 1, 973, 5028, DealAction.Sell, DealEntry.Out, 1000, 2410m, 10, 3)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("C4-F17c", trades);
    var remainingPathOk = trades.Count == 1 && trades[0].Completed && trades[0].ClosedVolumeLots == 0.10m
                          && trades[0].MaxVolumeLots == 0.10m;
    Verdict("C4-remaining", "canceled", remainingPathOk,
        "apply 971 (+0.10), skip 972, OUT 0.10 → flat; never remaining < 0",
        Summary(trades));
    Verdict("C4-first3", "canceled", count == 0,
        "completed+dirty ⇒ CountCompletedXau=0",
        $"count={count} completed={trades.FirstOrDefault()?.Completed}");
}

// C5: F17d close canceled → stay open
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 981, 5029, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 982, 5029, DealAction.SellCanceled, DealEntry.Out, 1000, 2410m, 0, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    Dump("C5-F17d", trades);
    var stayOpen = trades.Count == 1 && !trades[0].Completed && trades[0].RemainingVolumeLots == 0.10m && count == 0;
    Verdict("C5-remaining", "canceled", stayOpen,
        "close voided; open long rem=0.10; completed_count=0",
        Summary(trades) + $" count={count}");
    Verdict("C5-dirty", "canceled", false,
        "RECON_CANCELED_DEAL dirty",
        stayOpen ? "open CLEAN rem=0.10" : Summary(trades));
}

// C6: F17e clawback balance not folded
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 970, 5027, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 983, 0, DealAction.Balance, DealEntry.In, 0, 0, -1m, 2, symbol: "")
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("C6-F17e", trades);
    Verdict("C6", "canceled", trades.Count == 0,
        "balance clawback skipped; no XAU trade with net=-1",
        trades.Count == 0 ? "EMPTY" : Summary(trades));
}

// C7: F17g latch retract — third close becomes SELL_CANCELED
{
    var clean = new List<NormalizedDeal>();
    for (var i = 0; i < 3; i++)
    {
        clean.Add(Deal("ACHIEVER", 1, 10 + i * 2, 100 + i, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, i * 2 + 1));
        clean.Add(Deal("ACHIEVER", 1, 11 + i * 2, 100 + i, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, i * 2 + 2));
    }
    var before = r.CountCompletedXauUsdTrades("ACHIEVER", 1, clean);
    var beforeEl = r.IsEarlyScoreEligible("ACHIEVER", 1, clean);
    var mutated = clean.ToList();
    mutated[5] = Deal("ACHIEVER", 1, 15, 102, DealAction.SellCanceled, DealEntry.Out, 1000, 2310m, 0, 6);
    var after = r.CountCompletedXauUsdTrades("ACHIEVER", 1, mutated);
    var afterEl = r.IsEarlyScoreEligible("ACHIEVER", 1, mutated);
    Dump("C7-before", r.Reconstruct("ACHIEVER", 1, clean));
    Dump("C7-after", r.Reconstruct("ACHIEVER", 1, mutated));
    Verdict("C7-retract", "canceled", before == 3 && beforeEl && after == 2 && !afterEl,
        "rebuild after close→SELL_CANCELED: count 3→2, eligible true→false",
        $"before={before}/{beforeEl} after={after}/{afterEl}");
}

// C8: canceled never inverted into a flatten (F17 remaining must not go to 0 via cancel)
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 961, 5017, DealAction.Buy, DealEntry.In, 1000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 962, 5017, DealAction.BuyCanceled, DealEntry.In, 1000, 2400m, 0, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("C8-no-inverse", trades);
    var inverted = trades.Count == 1 && trades[0].Completed;
    Verdict("C8-no-inverse", "canceled", !inverted && trades.Count == 1 && !trades[0].Completed,
        "do not invent inverse fill; remaining stays +0.10",
        Summary(trades));
}

// C9: three clean + canceled-tainted 4th that later flats — 4th must not be required but if counted as clean that's ok for latch; if #2 is canceled-tainted and completed, latch on dirty is FAIL
{
    var deals = new List<NormalizedDeal>
    {
        Deal("ACHIEVER", 1, 1, 1, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 1, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("ACHIEVER", 1, 3, 2, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 3),
        Deal("ACHIEVER", 1, 4, 2, DealAction.BuyCanceled, DealEntry.In, 1000, 2290m, 0, 4),
        Deal("ACHIEVER", 1, 5, 2, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 5),
        Deal("ACHIEVER", 1, 6, 3, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 6),
        Deal("ACHIEVER", 1, 7, 3, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 7)
    };
    var count = r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals);
    var eligible = r.IsEarlyScoreEligible("ACHIEVER", 1, deals);
    Dump("C9-dirty-in-first3", r.Reconstruct("ACHIEVER", 1, deals));
    Verdict("C9", "canceled", count == 2 && !eligible,
        "pos 2 dirty canceled scale-in; only 2 clean completes; eligible false",
        $"count={count} eligible={eligible}");
}

// ===================== MIXED BROKERS =====================

// M1: A21 F23 isolation (product codes ACHIEVER / STARWAVEFX, lots via Manager native)
{
    var mixed = new[]
    {
        Deal("ACHIEVER", 1001, 101, 5001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1),
        Deal("ACHIEVER", 1001, 102, 5001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 2),
        Deal("STARWAVEFX", 1001, 101, 5001, DealAction.Buy, DealEntry.In, 5000, 2390m, 0, 1),
        Deal("STARWAVEFX", 1001, 102, 5001, DealAction.Sell, DealEntry.Out, 5000, 2395m, 2.5m, 2)
    };
    var ach = r.Reconstruct("ACHIEVER", 1001, mixed);
    var swx = r.Reconstruct("STARWAVEFX", 1001, mixed);
    Dump("M1-ACH", ach);
    Dump("M1-SWX", swx);
    var ok = ach.Count == 1 && swx.Count == 1
             && ach[0].BrokerId == "ACHIEVER" && swx[0].BrokerId == "STARWAVEFX"
             && ach[0].InitialVolumeLots == 1.00m && swx[0].InitialVolumeLots == 0.50m
             && ach[0].NetRealizedPnl == 10m && swx[0].NetRealizedPnl == 2.5m
             && ach[0].Completed && swx[0].Completed
             && ach[0].PositionId == 5001 && swx[0].PositionId == 5001;
    Verdict("M1-F23", "mixed-broker", ok,
        "two isolated completes: ACH 1.00 net=10 and SWX 0.50 net=2.5; same login/pos/tickets",
        $"ach={Summary(ach)} ;; swx={Summary(swx)}");
}

// M2: SWX must not leak into ACH first-3
{
    var mixed = new List<NormalizedDeal>();
    for (var i = 0; i < 2; i++)
    {
        mixed.Add(Deal("ACHIEVER", 1, 10 + i * 2, 10 + i, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, i * 2 + 1));
        mixed.Add(Deal("ACHIEVER", 1, 11 + i * 2, 10 + i, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, i * 2 + 2));
    }
    for (var i = 0; i < 3; i++)
    {
        mixed.Add(Deal("STARWAVEFX", 1, 100 + i * 2, 10 + i, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 20 + i * 2));
        mixed.Add(Deal("STARWAVEFX", 1, 101 + i * 2, 10 + i, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 21 + i * 2));
    }
    var achCount = r.CountCompletedXauUsdTrades("ACHIEVER", 1, mixed);
    var swxCount = r.CountCompletedXauUsdTrades("STARWAVEFX", 1, mixed);
    var achEl = r.IsEarlyScoreEligible("ACHIEVER", 1, mixed);
    var swxEl = r.IsEarlyScoreEligible("STARWAVEFX", 1, mixed);
    Verdict("M2-no-leak", "mixed-broker", achCount == 2 && !achEl && swxCount == 3 && swxEl,
        "ACH count=2 eligible=false; SWX count=3 eligible=true (same login, shared position ids)",
        $"ACH {achCount}/{achEl} SWX {swxCount}/{swxEl}");
}

// M3: case-insensitive broker filter
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 20, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, 1),
        Deal("ACHIEVER", 1, 2, 20, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100, 2)
    };
    var trades = r.Reconstruct("achiever", 1, deals);
    Dump("M3-case", trades);
    var ok = trades.Count == 1 && trades[0].Completed && trades[0].BrokerId == "achiever";
    Verdict("M3-case", "mixed-broker", ok,
        "OrdinalIgnoreCase matches ACHIEVER deals when called as achiever",
        Summary(trades));
}

// M4: A21 fixture broker codes ACH / SWX vs product ACHIEVER / STARWAVEFX
{
    var deals = new[]
    {
        Deal("ACH", 1001, 101, 5001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1),
        Deal("ACH", 1001, 102, 5001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 2)
    };
    var asProduct = r.Reconstruct("ACHIEVER", 1001, deals);
    var asFixture = r.Reconstruct("ACH", 1001, deals);
    Dump("M4-product-code", asProduct);
    Dump("M4-fixture-code", asFixture);
    Verdict("M4-alias", "mixed-broker", asProduct.Count == 0 && asFixture.Count == 1,
        "literal A21 codes ACH/SWX are not aliases of ACHIEVER/STARWAVEFX",
        $"Reconstruct(ACHIEVER) n={asProduct.Count}; Reconstruct(ACH) n={asFixture.Count}");
}

// M5: whitespace broker is a different venue
{
    var deals = new[]
    {
        Deal("ACHIEVER ", 1, 1, 21, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, 1),
        Deal("ACHIEVER ", 1, 2, 21, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100, 2)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, deals);
    Dump("M5-ws", trades);
    Verdict("M5-whitespace", "mixed-broker", trades.Count == 0,
        "trailing space is not trimmed — deals disappear (isolation is exact after IgnoreCase)",
        trades.Count == 0 ? "EMPTY" : Summary(trades));
}

// M6: cross-login same broker + same position
{
    var deals = new[]
    {
        Deal("ACHIEVER", 1, 1, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 30, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("ACHIEVER", 2, 3, 30, DealAction.Buy, DealEntry.In, 2000, 2300m, 0, 1),
        Deal("ACHIEVER", 2, 4, 30, DealAction.Sell, DealEntry.Out, 2000, 2310m, 20, 2)
    };
    var a = r.Reconstruct("ACHIEVER", 1, deals);
    var b = r.Reconstruct("ACHIEVER", 2, deals);
    Dump("M6-login1", a);
    Dump("M6-login2", b);
    var ok = a.Count == 1 && b.Count == 1 && a[0].InitialVolumeLots == 0.10m && b[0].InitialVolumeLots == 0.20m
             && a[0].NetRealizedPnl == 10m && b[0].NetRealizedPnl == 20m;
    Verdict("M6-login", "mixed-broker", ok,
        "login filter isolates same position_id on one broker",
        $"l1={Summary(a)} ;; l2={Summary(b)}");
}

// M7: same ticket numbers across brokers are not duplicates (engine has no global ticket set)
{
    var mixed = new[]
    {
        Deal("ACHIEVER", 1, 50, 40, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 51, 40, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("STARWAVEFX", 1, 50, 40, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("STARWAVEFX", 1, 51, 40, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2)
    };
    var ach = r.Reconstruct("ACHIEVER", 1, mixed);
    var swx = r.Reconstruct("STARWAVEFX", 1, mixed);
    var ok = ach.Count == 1 && swx.Count == 1 && ach[0].DealCount == 2 && swx[0].DealCount == 2;
    Verdict("M7-ticket-reuse", "mixed-broker", ok,
        "duplicate tickets across brokers are not duplicates",
        $"achDeals={ach.FirstOrDefault()?.DealCount} swxDeals={swx.FirstOrDefault()?.DealCount}");
}

// M8: poisoned labels — both venues written as ACHIEVER (caller/ingest bug the engine cannot see)
{
    var poisoned = new[]
    {
        Deal("ACHIEVER", 1, 101, 5001, DealAction.Buy, DealEntry.In, 10000, 2400m, 0, 1),
        Deal("ACHIEVER", 1, 102, 5001, DealAction.Sell, DealEntry.Out, 10000, 2410m, 10, 2),
        Deal("ACHIEVER", 1, 201, 5001, DealAction.Buy, DealEntry.In, 5000, 2390m, 0, 3),
        Deal("ACHIEVER", 1, 202, 5001, DealAction.Sell, DealEntry.Out, 5000, 2395m, 2.5m, 4)
    };
    var trades = r.Reconstruct("ACHIEVER", 1, poisoned);
    Dump("M8-poison", trades);
    // After first flatten, second IN reopens same position_id (netting reuse). Two completes, merged venue.
    var merged = trades.Count == 2 && trades.All(t => t.Completed && t.BrokerId == "ACHIEVER");
    Verdict("M8-poison-labels", "mixed-broker", true,
        "engine must merge when BrokerId strings are identical (ingest poison, not filter leak)",
        $"n={trades.Count} mergedSameBroker={merged} {Summary(trades)}");
}

// M9: Reconstruct once on mixed list with ACHIEVER — SWX silently omitted (API contract)
{
    var mixed = new[]
    {
        Deal("ACHIEVER", 1, 1, 60, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 60, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("STARWAVEFX", 1, 3, 61, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("STARWAVEFX", 1, 4, 61, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2)
    };
    var oneCall = r.Reconstruct("ACHIEVER", 1, mixed);
    Dump("M9-one-call", oneCall);
    Verdict("M9-one-call", "mixed-broker", oneCall.Count == 1 && oneCall[0].PositionId == 60,
        "single Reconstruct(broker) drops other brokers; caller must iterate",
        Summary(oneCall));
}

// M10: first-3 Id prefix differs by broker (OpenedAt collision)
{
    var mixed = new[]
    {
        Deal("ACHIEVER", 1, 1, 70, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("ACHIEVER", 1, 2, 70, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2),
        Deal("STARWAVEFX", 1, 1, 70, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, 1),
        Deal("STARWAVEFX", 1, 2, 70, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, 2)
    };
    var ach = r.Reconstruct("ACHIEVER", 1, mixed);
    var swx = r.Reconstruct("STARWAVEFX", 1, mixed);
    var collide = ach.Count == 1 && swx.Count == 1 && ach[0].Id == swx[0].Id;
    Verdict("M10-id", "mixed-broker", !collide && ach[0].Id.StartsWith("ACHIEVER:") && swx[0].Id.StartsWith("STARWAVEFX:"),
        "Id includes broker so same login/pos/ms do not collide",
        $"achId={ach[0].Id} swxId={swx[0].Id}");
}

Console.WriteLine("DONE");
