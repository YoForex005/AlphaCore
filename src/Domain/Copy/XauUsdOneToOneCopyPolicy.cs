using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Execution;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Domain.Copy;

public enum CopyOrdType
{
    Market = 1,
    Limit = 2,
    Stop = 3
}

public sealed record CopyTraderSnapshot
{
    public required TraderState State { get; init; }
    public required int CompletedXauTrades { get; init; }
    public required decimal XauNetPnl { get; init; }
    public required bool Martingale { get; init; }
    public required bool AveragingDown { get; init; }
    public required bool LotEscalation { get; init; }
    public string? GroupName { get; init; }
}

public sealed record CopySignal
{
    public required string SourceSymbol { get; init; }
    public required string CanonicalSymbol { get; init; }
    public required CopyIntentAction Action { get; init; }
    public required TradeDirection Direction { get; init; }
    public required decimal SourceLots { get; init; }
    public required decimal EntryPrice { get; init; }
    public required DateTimeOffset SourceEventTime { get; init; }
    public required bool SourceStillOpen { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public decimal? LimitPrice { get; init; }
    public decimal? StopTrigger { get; init; }
    public CopyOrdType OrdType { get; init; } = CopyOrdType.Market;
}

public sealed record CopyInstruction
{
    public required bool Accept { get; init; }
    public required string Reason { get; init; }
    public CopyIntentAction? Action { get; init; }
    public TradeDirection? Direction { get; init; }
    public decimal Lots { get; init; }
    public decimal FixOrderQtyUnits { get; init; }
    public decimal? StopLoss { get; init; }
    public decimal? TakeProfit { get; init; }
    public decimal? LimitPrice { get; init; }
    public decimal? StopTrigger { get; init; }
    public CopyOrdType OrdType { get; init; } = CopyOrdType.Market;
}

/// <summary>
/// Live copy selects <b>traders</b> with a measured XAUUSD edge, then copies their
/// next XAUUSD events 1:1 (lots, SL/TP, side). It does not wait until a ticket
/// is profitable — that is lookahead and cannot be traded live.
/// Close is copied when the source closes, not at a predicted time.
/// </summary>
public sealed class XauUsdOneToOneCopyPolicy
{
    public const int MinCompletedXauTrades = 20;
    public const decimal AllocationFactor = 1m;
    public const decimal GoldOuncesPerLot = 100m;

    public static readonly InstrumentQuantitySpec GoldLots = new(0.01m, 5m, 0.01m, 2);

    private readonly QuantityNormalizer _qty = new();

    public bool IsTraderEligible(CopyTraderSnapshot trader, out string reason)
    {
        if (trader.State is TraderState.RISK_BLOCKED or TraderState.DISQUALIFIED or TraderState.PAUSED)
        {
            reason = "TRADER_BLOCKED_" + trader.State;
            return false;
        }

        if (trader.State is TraderState.INSUFFICIENT_DATA or TraderState.EARLY_SCORE or TraderState.WATCH)
        {
            reason = "TRADER_NOT_SHADOW_YET";
            return false;
        }

        if (trader.Martingale || trader.AveragingDown || trader.LotEscalation)
        {
            reason = "TRADER_SIZE_PATTERN_BLOCK";
            return false;
        }

        if (trader.CompletedXauTrades < MinCompletedXauTrades)
        {
            reason = "NEED_MORE_XAU_HISTORY";
            return false;
        }

        if (trader.XauNetPnl <= 0)
        {
            reason = "XAU_BOOK_NOT_PROFITABLE";
            return false;
        }

        if (!CopyGroupFilter.IsDemoOrContest(trader.GroupName))
        {
            reason = "NOT_DEMO_OR_CONTEST_GROUP";
            return false;
        }

        reason = "TRADER_ELIGIBLE";
        return true;
    }

    public CopyInstruction Evaluate(CopyTraderSnapshot trader, CopySignal signal)
    {
        if (!IsTraderEligible(trader, out var traderReason))
            return Reject(traderReason);

        if (!IsXauUsd(signal.CanonicalSymbol) && !IsXauUsd(signal.SourceSymbol))
            return Reject("NOT_XAUUSD");

        if (signal.Action is CopyIntentAction.OpenExposure
            or CopyIntentAction.IncreaseExposure
            or CopyIntentAction.ReduceExposure)
        {
            if (!signal.SourceStillOpen && signal.Action != CopyIntentAction.ReduceExposure)
                return Reject("NO_LOOKAHEAD_CLOSED_WINNER");
            if (signal.Action == CopyIntentAction.ReduceExposure && !signal.SourceStillOpen)
                return Reject("REDUCE_REQUIRES_OPEN_SOURCE");
        }

        decimal lots;
        try
        {
            lots = _qty.Normalize(signal.SourceLots, AllocationFactor, GoldLots);
        }
        catch (ArgumentOutOfRangeException)
        {
            return Reject("INVALID_SOURCE_LOTS");
        }

        if (lots <= 0)
            return Reject("QTY_BELOW_MIN_OR_STEP");

        if (signal.OrdType == CopyOrdType.Limit && signal.LimitPrice is null or <= 0)
            return Reject("LIMIT_REQUIRES_PRICE");

        if (signal.OrdType == CopyOrdType.Stop && signal.StopTrigger is null or <= 0)
            return Reject("STOP_REQUIRES_TRIGGER");

        return new CopyInstruction
        {
            Accept = true,
            Reason = "ONE_TO_ONE_XAUUSD",
            Action = signal.Action,
            Direction = signal.Direction,
            Lots = lots,
            FixOrderQtyUnits = decimal.Round(lots * GoldOuncesPerLot, 2, MidpointRounding.ToZero),
            StopLoss = signal.StopLoss,
            TakeProfit = signal.TakeProfit,
            LimitPrice = signal.LimitPrice,
            StopTrigger = signal.StopTrigger,
            OrdType = signal.OrdType
        };
    }

    private static bool IsXauUsd(string? symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return false;
        return symbol.Equals("XAUUSD", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSD.", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSDM", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSD.A", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSD.I", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSD.S", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSD.PRO", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSDPRO", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("XAUUSDpro", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("GOLD", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("GOLD.", StringComparison.OrdinalIgnoreCase)
               || symbol.Equals("GOLD.A", StringComparison.OrdinalIgnoreCase);
    }

    private static CopyInstruction Reject(string reason) =>
        new() { Accept = false, Reason = reason };
}
