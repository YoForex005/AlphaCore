using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Risk;

namespace TraderIntelligence.Tests.Unit;

public class RiskEngineTests
{
    private readonly RiskEngine _e = new();

    [Fact]
    public void Stale_quote_rejects_open()
    {
        var d = _e.Evaluate(Base(q => q with { Quote = q.Quote! with { ReceivedAt = q.DecisionTime.AddSeconds(-30) } }));
        d.Outcome.Should().Be(RiskDecisionOutcome.Reject);
        d.Reason.Should().Be("QUOTE_STALE");
        d.AllowFixSend.Should().BeFalse();
    }

    [Fact]
    public void Real_flag_false_never_allows_fix_send()
    {
        var d = _e.Evaluate(Base());
        d.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        d.AllowFixSend.Should().BeFalse();
    }

    [Fact]
    public void Stop_new_execution_blocks_opens_not_closes()
    {
        var open = _e.Evaluate(Base(q => q with { KillSwitch = KillSwitchMode.StopNewExecution }));
        open.Outcome.Should().Be(RiskDecisionOutcome.GlobalStop);

        var close = _e.Evaluate(Base(q => q with
        {
            Action = CopyIntentAction.CloseExposure,
            KillSwitch = KillSwitchMode.StopNewExecution
        }));
        close.Outcome.Should().Be(RiskDecisionOutcome.Approve);
        close.AllowFixSend.Should().BeFalse();
    }

    [Fact]
    public void Unreconciled_venue_blocks_new_exposure()
    {
        var d = _e.Evaluate(Base(q => q with { Reconciled = false }));
        d.Reason.Should().Be("VENUE_NOT_RECONCILED");
    }

    [Fact]
    public void Stale_signal_rejected()
    {
        var d = _e.Evaluate(Base(q => q with { SourceEventTime = q.DecisionTime.AddMinutes(-5) }));
        d.Reason.Should().Be("SIGNAL_STALE");
    }

    private static RiskEvaluationRequest Base(Func<RiskEvaluationRequest, RiskEvaluationRequest>? tweak = null)
    {
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var req = new RiskEvaluationRequest
        {
            CopyIntentId = "c1",
            BrokerId = "ACHIEVER",
            SourceLogin = 1,
            Action = CopyIntentAction.OpenExposure,
            RequestedQuantity = 0.10m,
            ExpectedPrice = 2400m,
            SourceEventTime = now.AddSeconds(-1),
            DecisionTime = now,
            Quote = new DestinationQuote("XAUUSD", "1", 2399.9m, 2400.1m, now.AddMilliseconds(-200), now),
            VenueHealthy = true,
            RealExecutionEnabled = false,
            Reconciled = true,
            KillSwitch = KillSwitchMode.None,
            TraderRealizedLoss = 0,
            DailyExecutionPnl = 0,
            PortfolioDrawdown = 0,
            CurrentGrossXau = 0,
            CurrentNetXau = 0,
            OpenPositions = 0,
            MarginUsage = 0.1m,
            MartingaleFlag = false,
            AbnormalSizing = false
        };
        return tweak is null ? req : tweak(req);
    }
}
