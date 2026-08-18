using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Scoring;

namespace TraderIntelligence.Tests.Unit;

public class BaselineScorerTests
{
    private readonly BaselineScorer _s = new();

    [Fact]
    public void Two_trades_remain_insufficient()
    {
        var score = _s.Score(new[] { Closed(1, 10), Closed(2, 10) });
        score.EarlyScoreEligible.Should().BeFalse();
        score.SuggestedState.Should().Be(TraderState.INSUFFICIENT_DATA);
    }

    [Fact]
    public void Three_disciplined_winners_go_to_shadow_not_live()
    {
        var score = _s.Score(new[] { Closed(1, 80), Closed(2, 70), Closed(3, 90) });
        score.EarlyScoreEligible.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.SHADOW);
        TraderStateMachine.CanPromoteToLive(score.SuggestedState).Should().BeFalse();
    }

    [Fact]
    public void Martingale_after_losses_is_risk_blocked()
    {
        var trades = new[]
        {
            Closed(1, -100, lots: 0.10m),
            Closed(2, -200, lots: 0.20m),
            Closed(3, -400, lots: 0.40m)
        };
        var score = _s.Score(trades);
        score.Features.Martingale.Should().BeTrue();
        score.SuggestedState.Should().Be(TraderState.RISK_BLOCKED);
    }

    private static ReconstructedTradeResult Closed(int n, decimal pnl, decimal lots = 0.10m) =>
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
            InitialSl = 2290,
            WasScaledIn = false,
            WasPartialClose = false,
            WasAveragedDown = false,
            Completed = true
        };
}
