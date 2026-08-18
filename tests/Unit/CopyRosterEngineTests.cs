using FluentAssertions;
using TraderIntelligence.Domain.Copy;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Tests.Unit;

public class CopyRosterEngineTests
{
    private readonly CopyRosterEngine _e = new();

    private static CopyTraderSnapshot Shadow(decimal pnl = 400, int n = 22) => new()
    {
        State = TraderState.SHADOW,
        CompletedXauTrades = n,
        XauNetPnl = pnl,
        Martingale = false,
        AveragingDown = false,
        LotEscalation = false,
        GroupName = "demo\\yo-2step"
    };

    private static ReconstructedTradeResult Xau(int id, decimal pnl, DateTimeOffset? closed = null) =>
        new()
        {
            Id = id.ToString(),
            BrokerId = "B",
            Login = 1,
            PositionId = id,
            CanonicalSymbol = "XAUUSD",
            SourceSymbol = "XAUUSD",
            Direction = TradeDirection.Long,
            OpenedAt = DateTimeOffset.UtcNow.AddDays(-id),
            ClosedAt = closed ?? DateTimeOffset.UtcNow.AddDays(-id).AddHours(1),
            EntryVwap = 2300,
            InitialVolumeLots = 0.05m,
            MaxVolumeLots = 0.05m,
            ClosedVolumeLots = 0.05m,
            RemainingVolumeLots = 0,
            GrossRealizedPnl = pnl,
            Commission = 0,
            Swap = 0,
            Fees = 0,
            NetRealizedPnl = pnl,
            DealCount = 2,
            OrderCount = 2,
            WasScaledIn = false,
            WasPartialClose = false,
            WasAveragedDown = false,
            Completed = true
        };

    [Fact]
    public void New_eligible_trader_is_auto_admitted()
    {
        var many = Enumerable.Range(1, 22).Select(i => Xau(i, 10)).ToList();
        var d = _e.Decide(Shadow(), many, alreadyOnRoster: false);
        d.Action.Should().Be(RosterAction.Admit);
        d.AllowNewOpens.Should().BeTrue();
        d.FlattenDestination.Should().BeFalse();
        d.Reason.Should().Be("AUTO_ADMIT");
    }

    [Fact]
    public void Watch_is_not_admitted()
    {
        var d = _e.Decide(Shadow() with { State = TraderState.WATCH, CompletedXauTrades = 5 }, Array.Empty<ReconstructedTradeResult>(), false);
        d.AllowNewOpens.Should().BeFalse();
        d.Action.Should().NotBe(RosterAction.Admit);
    }

    [Fact]
    public void Book_turning_negative_removes_and_flattens()
    {
        var trades = Enumerable.Range(1, 22).Select(i => Xau(i, i < 20 ? 10 : -300)).ToList();
        var snap = Shadow(trades.Sum(t => t.NetRealizedPnl));
        var d = _e.Decide(snap, trades, alreadyOnRoster: true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.FlattenDestination.Should().BeTrue();
        d.AllowNewOpens.Should().BeFalse();
        d.Reason.Should().Be("XAU_BOOK_TURNED_NEGATIVE");
    }

    [Fact]
    public void Three_consecutive_losses_remove()
    {
        var trades = Enumerable.Range(1, 22).Select(i => Xau(i, i <= 3 ? -5 : 20)).ToList();
        var snap = Shadow(trades.Sum(t => t.NetRealizedPnl));
        var d = _e.Decide(snap, trades, true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.Reason.Should().StartWith("CONSECUTIVE_LOSSES_");
    }

    [Fact]
    public void Martingale_removes_even_if_pnl_green()
    {
        var d = _e.Decide(Shadow() with { Martingale = true }, Enumerable.Range(1, 22).Select(i => Xau(i, 10)).ToList(), true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.Reason.Should().Be("SIZE_PATTERN");
    }

    [Fact]
    public void Only_demo_or_contest_groups_are_admitted()
    {
        var trades = Enumerable.Range(1, 22).Select(i => Xau(i, 10)).ToList();
        var real = _e.Decide(Shadow() with { GroupName = "Starwave\\real\\FX3\\grp1" }, trades, false);
        real.Action.Should().Be(RosterAction.RemoveAndFlatten);
        real.Reason.Should().Be("NOT_DEMO_OR_CONTEST_GROUP");

        var demo = _e.Decide(Shadow() with { GroupName = "demo\\yo-2step" }, trades, false);
        demo.Action.Should().Be(RosterAction.Admit);

        var contest = _e.Decide(Shadow() with { GroupName = "contest\\yo-2step" }, trades, false);
        contest.Action.Should().Be(RosterAction.Admit);

        var starwaveDemo = _e.Decide(Shadow() with { GroupName = "Starwave\\demo\\FX2\\grp1" }, trades, false);
        starwaveDemo.Action.Should().Be(RosterAction.Admit);
    }

    [Fact]
    public void Open_unrealized_loss_beyond_cap_flattens_that_copy()
    {
        _e.ShouldFlattenOpenCopy(-151m).Should().BeTrue();
        _e.ShouldFlattenOpenCopy(-10m).Should().BeFalse();
    }

    [Fact]
    public void Peak_drawdown_removes()
    {
        var trades = new List<ReconstructedTradeResult>();
        for (var i = 1; i <= 20; i++)
            trades.Add(Xau(i, 50));
        trades.Add(Xau(21, -700));
        var snap = Shadow(trades.Sum(t => t.NetRealizedPnl), 21);
        var d = _e.Decide(snap, trades, true);
        d.Action.Should().Be(RosterAction.RemoveAndFlatten);
        d.Reason.Should().Be("DRAWDOWN_FROM_PEAK");
    }
}
