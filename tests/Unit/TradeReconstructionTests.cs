using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Volume;

namespace TraderIntelligence.Tests.Unit;

public class TradeReconstructionTests
{
    private readonly TradeReconstructor _r = new(VolumeConverter.Manager);

    [Fact]
    public void Reconstructs_simple_round_trip()
    {
        var deals = new[]
        {
            Deal(1, 10, DealAction.Buy, DealEntry.In, 1000, 2320m, 0, t: 1),
            Deal(2, 10, DealAction.Sell, DealEntry.Out, 1000, 2330m, 100, t: 2)
        };

        var trades = _r.Reconstruct("ACHIEVER", 1, deals);
        trades.Should().ContainSingle();
        trades[0].Completed.Should().BeTrue();
        trades[0].IsXauUsd.Should().BeTrue();
        trades[0].Direction.Should().Be(TradeDirection.Long);
        trades[0].InitialVolumeLots.Should().Be(0.10m);
        trades[0].NetRealizedPnl.Should().Be(100m);
        trades[0].EntryVwap.Should().Be(2320m);
        trades[0].ExitVwap.Should().Be(2330m);
    }

    [Fact]
    public void Scale_in_and_partial_close()
    {
        var deals = new[]
        {
            Deal(1, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 20, DealAction.Buy, DealEntry.In, 1000, 2290m, 0, t: 2),
            Deal(3, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 20, t: 3),
            Deal(4, 20, DealAction.Sell, DealEntry.Out, 1000, 2320m, 40, t: 4)
        };

        var trade = _r.Reconstruct("ACHIEVER", 1, deals).Should().ContainSingle().Subject;
        trade.WasScaledIn.Should().BeTrue();
        trade.WasPartialClose.Should().BeTrue();
        trade.WasAveragedDown.Should().BeTrue();
        trade.Completed.Should().BeTrue();
        trade.MaxVolumeLots.Should().Be(0.20m);
    }

    [Fact]
    public void Reverse_inout_closes_then_opens_opposite()
    {
        var deals = new[]
        {
            Deal(1, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 30, DealAction.Sell, DealEntry.InOut, 2000, 2290m, -10, t: 2)
        };

        var trades = _r.Reconstruct("ACHIEVER", 1, deals);
        trades.Should().HaveCount(2);
        trades[0].Completed.Should().BeTrue();
        trades[0].Direction.Should().Be(TradeDirection.Long);
        trades[1].Completed.Should().BeFalse();
        trades[1].Direction.Should().Be(TradeDirection.Short);
        trades[1].RemainingVolumeLots.Should().Be(0.10m);
    }

    [Fact]
    public void First_three_completed_xau_unlocks_early_score()
    {
        var deals = new List<NormalizedDeal>();
        for (var i = 0; i < 3; i++)
        {
            deals.Add(Deal(10 + i * 2, 100 + i, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: i * 2 + 1));
            deals.Add(Deal(11 + i * 2, 100 + i, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, t: i * 2 + 2));
        }

        _r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals).Should().Be(3);
        _r.IsEarlyScoreEligible("ACHIEVER", 1, deals).Should().BeTrue();
    }

    [Fact]
    public void Canceled_deal_on_a_position_excludes_it_from_first_three()
    {
        var deals = new List<NormalizedDeal>
        {
            Deal(1, 10, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 1),
            Deal(2, 10, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, t: 2),
            Deal(3, 10, DealAction.BuyCanceled, DealEntry.In, 1000, 2300m, 0, t: 3),
            Deal(4, 20, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 4),
            Deal(5, 20, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, t: 5),
            Deal(6, 30, DealAction.Buy, DealEntry.In, 1000, 2300m, 0, t: 6),
            Deal(7, 30, DealAction.Sell, DealEntry.Out, 1000, 2310m, 10, t: 7)
        };

        _r.Reconstruct("ACHIEVER", 1, deals).Count(t => t.Completed).Should().Be(3);
        _r.CountCompletedXauUsdTrades("ACHIEVER", 1, deals).Should().Be(2);
        _r.IsEarlyScoreEligible("ACHIEVER", 1, deals).Should().BeFalse();
    }

    [Fact]
    public void Ignores_balance_deals()
    {
        var deals = new[]
        {
            Deal(1, 1, DealAction.Balance, DealEntry.In, 0, 0, 1000, t: 1)
        };
        _r.Reconstruct("ACHIEVER", 1, deals).Should().BeEmpty();
    }

    private static NormalizedDeal Deal(
        long ticket, long position, DealAction action, DealEntry entry, ulong volume, decimal price, decimal profit, int t) =>
        new()
        {
            BrokerId = "ACHIEVER",
            Login = 1,
            DealTicket = ticket,
            OrderTicket = ticket,
            PositionId = position,
            SourceSymbol = "XAUUSDm",
            Action = action,
            Entry = entry,
            VolumeNative = volume,
            Price = price,
            Profit = profit,
            Commission = 0,
            Swap = 0,
            Time = DateTimeOffset.UnixEpoch.AddMinutes(t)
        };
}
