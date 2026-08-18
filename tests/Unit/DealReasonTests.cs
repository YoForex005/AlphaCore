using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;
using TraderIntelligence.Domain.Volume;

namespace TraderIntelligence.Tests.Unit;

public class DealReasonTests
{
    [Fact]
    public void Rollover_is_not_a_trader_lifecycle_deal()
    {
        var deal = new NormalizedDeal
        {
            BrokerId = "ACHIEVER",
            Login = 1,
            DealTicket = 1,
            OrderTicket = 1,
            PositionId = 9,
            SourceSymbol = "XAUUSD",
            Action = DealAction.Buy,
            Entry = DealEntry.In,
            VolumeNative = 1000,
            Price = 2300,
            Profit = 0,
            Commission = 0,
            Swap = 1,
            Time = DateTimeOffset.UnixEpoch,
            Reason = DealReason.Rollover
        };
        deal.IsTradingDeal.Should().BeFalse();

        var trades = new TradeReconstructor(VolumeConverter.Manager).Reconstruct("ACHIEVER", 1, new[] { deal });
        trades.Should().BeEmpty();
    }

    [Fact]
    public void Client_buy_still_counts()
    {
        DealReasons.CountsAsTraderActivity(DealReason.Client).Should().BeTrue();
        DealReasons.CountsAsTraderActivity(DealReason.Migration).Should().BeFalse();
        DealReasons.CountsAsTraderActivity(null).Should().BeTrue();
    }
}
