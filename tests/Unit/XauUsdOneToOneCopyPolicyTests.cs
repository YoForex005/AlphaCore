using FluentAssertions;
using TraderIntelligence.Domain.Copy;
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Tests.Unit;

public class XauUsdOneToOneCopyPolicyTests
{
    private readonly XauUsdOneToOneCopyPolicy _p = new();

    private static CopyTraderSnapshot GoodTrader() => new()
    {
        State = TraderState.SHADOW,
        CompletedXauTrades = 22,
        XauNetPnl = 400m,
        Martingale = false,
        AveragingDown = false,
        LotEscalation = false,
        GroupName = "demo\\yo-2step"
    };

    private static CopySignal OpenGold(decimal lots = 0.05m, bool stillOpen = true) => new()
    {
        SourceSymbol = "XAUUSD",
        CanonicalSymbol = "XAUUSD",
        Action = CopyIntentAction.OpenExposure,
        Direction = TradeDirection.Long,
        SourceLots = lots,
        EntryPrice = 4391.47m,
        SourceEventTime = DateTimeOffset.UtcNow,
        SourceStillOpen = stillOpen,
        StopLoss = 4380m,
        TakeProfit = 4410m
    };

    [Fact]
    public void Eligible_open_xau_is_one_to_one_lots_and_sl_tp()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold(0.05m));
        i.Accept.Should().BeTrue();
        i.Reason.Should().Be("ONE_TO_ONE_XAUUSD");
        i.Lots.Should().Be(0.05m);
        i.FixOrderQtyUnits.Should().Be(5m);
        i.StopLoss.Should().Be(4380m);
        i.TakeProfit.Should().Be(4410m);
        i.OrdType.Should().Be(CopyOrdType.Market);
    }

    [Fact]
    public void Closed_winner_is_lookahead_and_rejected()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold(stillOpen: false));
        i.Accept.Should().BeFalse();
        i.Reason.Should().Be("NO_LOOKAHEAD_CLOSED_WINNER");
    }

    [Fact]
    public void EurUsd_is_rejected()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold() with { CanonicalSymbol = "EURUSD", SourceSymbol = "EURUSD" });
        i.Accept.Should().BeFalse();
        i.Reason.Should().Be("NOT_XAUUSD");
    }

    [Fact]
    public void XaeEur_and_goldnugget_are_not_gold()
    {
        _p.Evaluate(GoodTrader(), OpenGold() with { CanonicalSymbol = "XAUEUR", SourceSymbol = "XAUEUR" })
            .Reason.Should().Be("NOT_XAUUSD");
        _p.Evaluate(GoodTrader(), OpenGold() with { CanonicalSymbol = "GOLDNUGGET", SourceSymbol = "GOLDNUGGET" })
            .Reason.Should().Be("NOT_XAUUSD");
    }

    [Fact]
    public void Martingale_trader_blocked()
    {
        _p.IsTraderEligible(GoodTrader() with { Martingale = true }, out var reason).Should().BeFalse();
        reason.Should().Be("TRADER_SIZE_PATTERN_BLOCK");
    }

    [Fact]
    public void Negative_xau_pnl_blocked()
    {
        _p.IsTraderEligible(GoodTrader() with { XauNetPnl = -10m }, out var reason).Should().BeFalse();
        reason.Should().Be("XAU_BOOK_NOT_PROFITABLE");
    }

    [Fact]
    public void First_three_trades_not_enough()
    {
        _p.IsTraderEligible(GoodTrader() with { CompletedXauTrades = 3 }, out var reason).Should().BeFalse();
        reason.Should().Be("NEED_MORE_XAU_HISTORY");
    }

    [Fact]
    public void Real_group_blocked_demo_and_contest_allowed()
    {
        _p.IsTraderEligible(GoodTrader() with { GroupName = "Starwave\\real\\FX3\\grp1" }, out var reason).Should().BeFalse();
        reason.Should().Be("NOT_DEMO_OR_CONTEST_GROUP");
        _p.IsTraderEligible(GoodTrader() with { GroupName = "demo\\yo-2step" }, out _).Should().BeTrue();
        _p.IsTraderEligible(GoodTrader() with { GroupName = "contest\\yo-2step" }, out _).Should().BeTrue();
        _p.IsTraderEligible(GoodTrader() with { GroupName = "Starwave\\demo\\FX2\\grp1" }, out _).Should().BeTrue();
    }

    [Fact]
    public void Risk_blocked_state_rejected()
    {
        _p.IsTraderEligible(GoodTrader() with { State = TraderState.RISK_BLOCKED }, out _).Should().BeFalse();
    }

    [Fact]
    public void Limit_without_price_rejected()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold() with { OrdType = CopyOrdType.Limit, LimitPrice = null });
        i.Accept.Should().BeFalse();
        i.Reason.Should().Be("LIMIT_REQUIRES_PRICE");
    }

    [Fact]
    public void Stop_without_trigger_rejected()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold() with { OrdType = CopyOrdType.Stop, StopTrigger = null });
        i.Accept.Should().BeFalse();
        i.Reason.Should().Be("STOP_REQUIRES_TRIGGER");
    }

    [Fact]
    public void Close_of_open_book_is_one_to_one()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold(0.10m) with
        {
            Action = CopyIntentAction.CloseExposure,
            SourceStillOpen = false
        });
        i.Accept.Should().BeTrue();
        i.Lots.Should().Be(0.10m);
        i.FixOrderQtyUnits.Should().Be(10m);
        i.Action.Should().Be(CopyIntentAction.CloseExposure);
    }

    [Fact]
    public void Lot_below_min_rejected()
    {
        var i = _p.Evaluate(GoodTrader(), OpenGold(0.001m));
        i.Accept.Should().BeFalse();
        i.Reason.Should().Be("QTY_BELOW_MIN_OR_STEP");
    }
}
