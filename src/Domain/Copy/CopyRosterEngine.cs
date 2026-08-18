using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Domain.Copy;

public enum RosterAction
{
    Admit,
    Keep,
    RemoveAndFlatten
}

public sealed record RosterLimits
{
    public int MaxConsecutiveXauLosses { get; init; } = 3;
    public decimal MaxDrawdownVsPeak { get; init; } = 0.40m;
    public decimal MaxUnrealizedLossLotsUsd { get; init; } = 150m;
    public int MinCompletedXauToAdmit { get; init; } = XauUsdOneToOneCopyPolicy.MinCompletedXauTrades;
}

public sealed record RosterDecision
{
    public required RosterAction Action { get; init; }
    public required string Reason { get; init; }
    public required bool FlattenDestination { get; init; }
    public required bool AllowNewOpens { get; init; }
}

/// <summary>
/// Auto-admit eligible XAUUSD traders and auto-remove losers.
/// Flatten is destination-only. Never touches the MT5 source book.
/// </summary>
public sealed class CopyRosterEngine
{
    private readonly XauUsdOneToOneCopyPolicy _policy;
    private readonly RosterLimits _limits;

    public CopyRosterEngine(XauUsdOneToOneCopyPolicy? policy = null, RosterLimits? limits = null)
    {
        _policy = policy ?? new XauUsdOneToOneCopyPolicy();
        _limits = limits ?? new RosterLimits();
    }

    public RosterDecision Decide(CopyTraderSnapshot trader, IReadOnlyList<ReconstructedTradeResult> completedXau, bool alreadyOnRoster)
    {
        if (trader.State is TraderState.RISK_BLOCKED or TraderState.DISQUALIFIED or TraderState.PAUSED)
            return Remove("STATE_" + trader.State);

        if (trader.Martingale || trader.AveragingDown || trader.LotEscalation)
            return Remove("SIZE_PATTERN");

        if (!CopyGroupFilter.IsDemoOrContest(trader.GroupName))
            return Remove("NOT_DEMO_OR_CONTEST_GROUP");

        var xau = completedXau
            .Where(t => t.Completed && t.IsXauUsd)
            .OrderBy(t => t.ClosedAt ?? t.OpenedAt)
            .ToList();

        var net = xau.Sum(t => t.NetRealizedPnl);
        if (alreadyOnRoster && net <= 0)
            return Remove("XAU_BOOK_TURNED_NEGATIVE");

        var streak = ConsecutiveLosses(xau);
        if (alreadyOnRoster && streak >= _limits.MaxConsecutiveXauLosses)
            return Remove("CONSECUTIVE_LOSSES_" + streak);

        var dd = DrawdownFromPeak(xau);
        if (alreadyOnRoster && dd.peak > 0 && dd.drawdown / dd.peak >= _limits.MaxDrawdownVsPeak)
            return Remove("DRAWDOWN_FROM_PEAK");

        if (_policy.IsTraderEligible(trader, out var reason))
        {
            return new RosterDecision
            {
                Action = alreadyOnRoster ? RosterAction.Keep : RosterAction.Admit,
                Reason = alreadyOnRoster ? "KEEP" : "AUTO_ADMIT",
                FlattenDestination = false,
                AllowNewOpens = true
            };
        }

        if (alreadyOnRoster)
            return Remove("NO_LONGER_ELIGIBLE_" + reason);

        return new RosterDecision
        {
            Action = RosterAction.Keep,
            Reason = "NOT_YET_" + reason,
            FlattenDestination = false,
            AllowNewOpens = false
        };
    }

    public bool ShouldFlattenOpenCopy(decimal unrealizedPnl) =>
        unrealizedPnl <= -_limits.MaxUnrealizedLossLotsUsd;

    public static int ConsecutiveLosses(IReadOnlyList<ReconstructedTradeResult> closedNewestLast)
    {
        var n = 0;
        for (var i = closedNewestLast.Count - 1; i >= 0; i--)
        {
            if (closedNewestLast[i].NetRealizedPnl < 0)
                n++;
            else
                break;
        }
        return n;
    }

    public static (decimal peak, decimal drawdown) DrawdownFromPeak(IReadOnlyList<ReconstructedTradeResult> closed)
    {
        var equity = 0m;
        var peak = 0m;
        var maxDd = 0m;
        foreach (var t in closed)
        {
            equity += t.NetRealizedPnl;
            if (equity > peak)
                peak = equity;
            var dd = peak - equity;
            if (dd > maxDd)
                maxDd = dd;
        }
        return (peak, maxDd);
    }

    private static RosterDecision Remove(string reason) =>
        new()
        {
            Action = RosterAction.RemoveAndFlatten,
            Reason = reason,
            FlattenDestination = true,
            AllowNewOpens = false
        };
}
