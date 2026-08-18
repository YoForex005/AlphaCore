using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Instruments;
using TraderIntelligence.Domain.Volume;

namespace TraderIntelligence.Domain.Reconstruction;

/// <summary>
/// Rebuilds logical position lifecycles from MT5 deals.
/// One completed reconstructed trade = one position that returned to flat
/// (or a reversal that closed the prior side).
/// </summary>
public sealed class TradeReconstructor
{
    private readonly VolumeConverter _volume;
    private readonly SymbolNormalizer _symbols;
    private const decimal FlatEpsilon = 0.0000001m;

    public TradeReconstructor(VolumeConverter? volume = null, SymbolNormalizer? symbols = null)
    {
        _volume = volume ?? VolumeConverter.Manager;
        _symbols = symbols ?? new SymbolNormalizer();
    }

    public IReadOnlyList<ReconstructedTradeResult> Reconstruct(
        string brokerId,
        long login,
        IReadOnlyList<NormalizedDeal> deals)
    {
        var trading = deals
            .Where(d => d.IsTradingDeal)
            .Where(d => string.Equals(d.BrokerId, brokerId, StringComparison.OrdinalIgnoreCase))
            .Where(d => d.Login == login)
            .OrderBy(d => d.Time)
            .ThenBy(d => d.DealTicket)
            .ToList();

        var results = new List<ReconstructedTradeResult>();
        foreach (var group in trading.GroupBy(d => d.PositionId))
            results.AddRange(ReconstructPosition(brokerId, login, group.Key, group.ToList()));

        return results
            .OrderBy(t => t.OpenedAt)
            .ThenBy(t => t.PositionId)
            .ToList();
    }

    public IReadOnlyList<ReconstructedTradeResult> CompletedXauUsdTrades(
        string brokerId,
        long login,
        IReadOnlyList<NormalizedDeal> deals)
    {
        return Reconstruct(brokerId, login, deals)
            .Where(t => t.Completed && t.IsXauUsd)
            .OrderBy(t => t.ClosedAt)
            .ThenBy(t => t.OpenedAt)
            .ToList();
    }

    public int CountCompletedXauUsdTrades(string brokerId, long login, IReadOnlyList<NormalizedDeal> deals) =>
        CompletedXauUsdTrades(brokerId, login, deals).Count;

    public bool IsEarlyScoreEligible(string brokerId, long login, IReadOnlyList<NormalizedDeal> deals) =>
        CountCompletedXauUsdTrades(brokerId, login, deals) >= 3;

    private IReadOnlyList<ReconstructedTradeResult> ReconstructPosition(
        string brokerId,
        long login,
        long positionId,
        List<NormalizedDeal> deals)
    {
        var completed = new List<ReconstructedTradeResult>();
        OpenTrade? open = null;

        foreach (var deal in deals)
        {
            var lots = _volume.ToLots(deal.VolumeNative);
            if (lots <= 0)
                continue;

            switch (deal.Entry)
            {
                case DealEntry.In:
                    open = ApplyIn(open, deal, lots, brokerId, login, positionId);
                    break;
                case DealEntry.Out:
                case DealEntry.OutBy:
                    if (open is null)
                        continue;
                    if (ApplyOut(open, deal, lots, out var closed))
                    {
                        completed.Add(closed);
                        open = null;
                    }
                    break;
                case DealEntry.InOut:
                    var (closedReverse, newOpen) = ApplyReverse(open, deal, lots, brokerId, login, positionId);
                    if (closedReverse is not null)
                        completed.Add(closedReverse);
                    open = newOpen;
                    break;
            }
        }

        if (open is not null)
            completed.Add(open.ToResult(completed: false));

        return completed;
    }

    private OpenTrade ApplyIn(OpenTrade? open, NormalizedDeal deal, decimal lots, string brokerId, long login, long positionId)
    {
        var direction = deal.Action == DealAction.Buy ? TradeDirection.Long : TradeDirection.Short;
        if (open is null)
            return OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);

        if (open.Direction == direction)
        {
            open.ScaleIn(deal, lots);
            return open;
        }

        // Unexpected opposite IN on same position id: treat remaining as reverse remainder.
        var (closed, next) = ApplyReverse(open, deal, lots + open.RemainingLots, brokerId, login, positionId);
        _ = closed;
        return next ?? OpenTrade.Start(brokerId, login, positionId, deal, lots, direction, _symbols);
    }

    private static bool ApplyOut(OpenTrade open, NormalizedDeal deal, decimal lots, out ReconstructedTradeResult closed)
    {
        open.CloseOut(deal, lots);
        if (open.RemainingLots <= FlatEpsilon)
        {
            closed = open.ToResult(completed: true);
            return true;
        }

        closed = null!;
        return false;
    }

    private (ReconstructedTradeResult? Closed, OpenTrade? Next) ApplyReverse(
        OpenTrade? open,
        NormalizedDeal deal,
        decimal dealLots,
        string brokerId,
        long login,
        long positionId)
    {
        var newDirection = deal.Action == DealAction.Buy ? TradeDirection.Long : TradeDirection.Short;
        if (open is null)
        {
            return (null, OpenTrade.Start(brokerId, login, positionId, deal, dealLots, newDirection, _symbols));
        }

        var closeLots = open.RemainingLots;
        open.CloseOut(deal, closeLots);
        var closed = open.ToResult(completed: true);

        var leftover = dealLots - closeLots;
        if (leftover <= FlatEpsilon)
            return (closed, null);

        return (closed, OpenTrade.Start(brokerId, login, positionId, deal, leftover, newDirection, _symbols));
    }

    private sealed class OpenTrade
    {
        private readonly List<long> _deals = new();
        private readonly HashSet<long> _orders = new();
        private decimal _entryNotional;
        private decimal _entryLots;
        private decimal _exitNotional;
        private decimal _exitLots;

        public required string BrokerId { get; init; }
        public required long Login { get; init; }
        public required long PositionId { get; init; }
        public required string CanonicalSymbol { get; init; }
        public required string SourceSymbol { get; init; }
        public required TradeDirection Direction { get; set; }
        public required DateTimeOffset OpenedAt { get; init; }
        public DateTimeOffset? LastEventAt { get; set; }
        public decimal InitialVolumeLots { get; set; }
        public decimal MaxVolumeLots { get; set; }
        public decimal RemainingLots { get; set; }
        public decimal ClosedVolumeLots { get; set; }
        public decimal GrossRealizedPnl { get; set; }
        public decimal Commission { get; set; }
        public decimal Swap { get; set; }
        public decimal? InitialSl { get; set; }
        public decimal? InitialTp { get; set; }
        public decimal? FinalSl { get; set; }
        public decimal? FinalTp { get; set; }
        public bool WasScaledIn { get; set; }
        public bool WasPartialClose { get; set; }
        public bool WasAveragedDown { get; set; }

        public static OpenTrade Start(
            string brokerId,
            long login,
            long positionId,
            NormalizedDeal deal,
            decimal lots,
            TradeDirection direction,
            SymbolNormalizer symbols)
        {
            symbols.TryMapSource(deal.SourceSymbol, out var canonical);
            var trade = new OpenTrade
            {
                BrokerId = brokerId,
                Login = login,
                PositionId = positionId,
                CanonicalSymbol = string.IsNullOrEmpty(canonical) ? deal.SourceSymbol : canonical,
                SourceSymbol = deal.SourceSymbol,
                Direction = direction,
                OpenedAt = deal.Time,
                LastEventAt = deal.Time,
                InitialVolumeLots = lots,
                MaxVolumeLots = lots,
                RemainingLots = lots,
                InitialSl = deal.StopLoss,
                InitialTp = deal.TakeProfit,
                FinalSl = deal.StopLoss,
                FinalTp = deal.TakeProfit
            };
            trade.AddDealMeta(deal);
            trade._entryNotional = deal.Price * lots;
            trade._entryLots = lots;
            trade.Commission += deal.Commission;
            trade.Swap += deal.Swap;
            trade.GrossRealizedPnl += deal.Profit;
            return trade;
        }

        public void ScaleIn(NormalizedDeal deal, decimal lots)
        {
            var worse = Direction == TradeDirection.Long
                ? deal.Price > EntryVwap
                : deal.Price < EntryVwap;
            if (worse)
                WasAveragedDown = true;

            WasScaledIn = true;
            RemainingLots += lots;
            if (RemainingLots > MaxVolumeLots)
                MaxVolumeLots = RemainingLots;
            _entryNotional += deal.Price * lots;
            _entryLots += lots;
            ApplyCommon(deal);
        }

        public void CloseOut(NormalizedDeal deal, decimal lots)
        {
            var closeLots = Math.Min(lots, RemainingLots);
            if (closeLots <= 0)
            {
                ApplyCommon(deal);
                return;
            }

            if (closeLots < RemainingLots - FlatEpsilon)
                WasPartialClose = true;

            RemainingLots -= closeLots;
            ClosedVolumeLots += closeLots;
            _exitNotional += deal.Price * closeLots;
            _exitLots += closeLots;
            ApplyCommon(deal);
        }

        private void ApplyCommon(NormalizedDeal deal)
        {
            AddDealMeta(deal);
            LastEventAt = deal.Time;
            Commission += deal.Commission;
            Swap += deal.Swap;
            GrossRealizedPnl += deal.Profit;
            if (deal.StopLoss.HasValue)
                FinalSl = deal.StopLoss;
            if (deal.TakeProfit.HasValue)
                FinalTp = deal.TakeProfit;
        }

        private void AddDealMeta(NormalizedDeal deal)
        {
            _deals.Add(deal.DealTicket);
            if (deal.OrderTicket != 0)
                _orders.Add(deal.OrderTicket);
        }

        private decimal EntryVwap => _entryLots <= 0 ? 0 : _entryNotional / _entryLots;
        private decimal? ExitVwap => _exitLots <= 0 ? null : _exitNotional / _exitLots;

        public ReconstructedTradeResult ToResult(bool completed)
        {
            var fees = 0m;
            return new ReconstructedTradeResult
            {
                Id = $"{BrokerId}:{Login}:{PositionId}:{OpenedAt.ToUnixTimeMilliseconds()}",
                BrokerId = BrokerId,
                Login = Login,
                PositionId = PositionId,
                CanonicalSymbol = CanonicalSymbol,
                SourceSymbol = SourceSymbol,
                Direction = Direction,
                OpenedAt = OpenedAt,
                ClosedAt = completed ? LastEventAt : null,
                EntryVwap = EntryVwap,
                ExitVwap = ExitVwap,
                InitialVolumeLots = InitialVolumeLots,
                MaxVolumeLots = MaxVolumeLots,
                ClosedVolumeLots = ClosedVolumeLots,
                RemainingVolumeLots = RemainingLots,
                GrossRealizedPnl = GrossRealizedPnl,
                Commission = Commission,
                Swap = Swap,
                Fees = fees,
                NetRealizedPnl = GrossRealizedPnl + Commission + Swap + fees,
                DealCount = _deals.Count,
                OrderCount = _orders.Count,
                InitialSl = InitialSl,
                InitialTp = InitialTp,
                FinalSl = FinalSl,
                FinalTp = FinalTp,
                WasScaledIn = WasScaledIn,
                WasPartialClose = WasPartialClose,
                WasAveragedDown = WasAveragedDown,
                Completed = completed,
                DealTickets = _deals.ToArray()
            };
        }
    }
}
