using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Reconstruction;

namespace TraderIntelligence.Domain.Scoring;

public sealed record FeatureSnapshot
{
    public required int CompletedXauTrades { get; init; }
    public required decimal NetPnl { get; init; }
    public required decimal GrossProfit { get; init; }
    public required decimal GrossLoss { get; init; }
    public required decimal ProfitFactor { get; init; }
    public required decimal LotCv { get; init; }
    public required decimal LossSizeCv { get; init; }
    public required bool Martingale { get; init; }
    public required bool AveragingDown { get; init; }
    public required bool LotEscalation { get; init; }
    public required decimal AverageHoldSeconds { get; init; }
    public required decimal SlUseRate { get; init; }
    public required decimal MaxDrawdown { get; init; }
    public required decimal TradeFrequencyPerDay { get; init; }
    public FeatureQuality MaeMfeQuality { get; init; } = FeatureQuality.Unavailable;
    public decimal? AverageMfe { get; init; }
    public decimal? AverageMae { get; init; }
    public PriceSource PriceSource { get; init; } = PriceSource.Unknown;
}

public sealed record BaselineScore
{
    public required FeatureSnapshot Features { get; init; }
    public required decimal RiskScore { get; init; }
    public required decimal BehaviorScore { get; init; }
    public required decimal EarlyQualityScore { get; init; }
    public required TraderState SuggestedState { get; init; }
    public required bool EarlyScoreEligible { get; init; }
}

public sealed class BaselineScorer
{
    public const int EarlyScoreTradeCount = 3;

    public FeatureSnapshot ComputeFeatures(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var trades = completedXau.Where(t => t.Completed && t.IsXauUsd).OrderBy(t => t.ClosedAt).ToList();
        if (trades.Count == 0)
        {
            return new FeatureSnapshot
            {
                CompletedXauTrades = 0,
                NetPnl = 0,
                GrossProfit = 0,
                GrossLoss = 0,
                ProfitFactor = 0,
                LotCv = 0,
                LossSizeCv = 0,
                Martingale = false,
                AveragingDown = false,
                LotEscalation = false,
                AverageHoldSeconds = 0,
                SlUseRate = 0,
                MaxDrawdown = 0,
                TradeFrequencyPerDay = 0
            };
        }

        var net = trades.Sum(t => t.NetRealizedPnl);
        var wins = trades.Where(t => t.NetRealizedPnl > 0).Select(t => t.NetRealizedPnl).ToList();
        var losses = trades.Where(t => t.NetRealizedPnl < 0).Select(t => -t.NetRealizedPnl).ToList();
        var grossProfit = wins.Sum();
        var grossLoss = losses.Sum();
        var lots = trades.Select(t => t.MaxVolumeLots).ToList();

        var equity = 0m;
        var peak = 0m;
        var maxDd = 0m;
        foreach (var trade in trades)
        {
            equity += trade.NetRealizedPnl;
            if (equity > peak)
                peak = equity;
            var dd = peak - equity;
            if (dd > maxDd)
                maxDd = dd;
        }

        var martingale = false;
        var lotEscalation = false;
        for (var i = 1; i < trades.Count; i++)
        {
            if (trades[i - 1].NetRealizedPnl < 0 && trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.25m)
                martingale = true;
            if (trades[i].MaxVolumeLots > trades[i - 1].MaxVolumeLots * 1.5m)
                lotEscalation = true;
        }

        var holds = trades
            .Where(t => t.ClosedAt.HasValue)
            .Select(t => (decimal)(t.ClosedAt!.Value - t.OpenedAt).TotalSeconds)
            .ToList();

        var spanDays = 1m;
        if (trades.Count >= 2 && trades[0].ClosedAt.HasValue && trades[^1].ClosedAt.HasValue)
        {
            var days = (decimal)(trades[^1].ClosedAt!.Value - trades[0].ClosedAt!.Value).TotalDays;
            spanDays = Math.Max(1m, days);
        }

        return new FeatureSnapshot
        {
            CompletedXauTrades = trades.Count,
            NetPnl = net,
            GrossProfit = grossProfit,
            GrossLoss = grossLoss,
            ProfitFactor = grossLoss <= 0 ? (grossProfit > 0 ? 99m : 0m) : decimal.Round(grossProfit / grossLoss, 4),
            LotCv = CoefficientOfVariation(lots),
            LossSizeCv = CoefficientOfVariation(losses),
            Martingale = martingale,
            AveragingDown = trades.Any(t => t.WasAveragedDown),
            LotEscalation = lotEscalation,
            AverageHoldSeconds = holds.Count == 0 ? 0 : holds.Average(),
            SlUseRate = trades.Count == 0 ? 0 : (decimal)trades.Count(t => t.InitialSl.GetValueOrDefault() > 0) / trades.Count,
            MaxDrawdown = maxDd,
            TradeFrequencyPerDay = trades.Count / spanDays,
            MaeMfeQuality = FeatureQuality.Unavailable,
            PriceSource = PriceSource.Unknown
        };
    }

    public BaselineScore Score(IReadOnlyList<ReconstructedTradeResult> completedXau)
    {
        var features = ComputeFeatures(completedXau);
        var eligible = features.CompletedXauTrades >= EarlyScoreTradeCount;

        var risk = 0m;
        if (features.Martingale) risk += 35;
        if (features.AveragingDown) risk += 20;
        if (features.LotEscalation) risk += 15;
        if (features.LotCv > 0.5m) risk += 10;
        if (features.SlUseRate < 0.3m) risk += 10;
        if (features.MaxDrawdown > 0 && features.GrossProfit > 0 && features.MaxDrawdown > features.GrossProfit)
            risk += 10;
        risk = Math.Min(100m, risk);

        var behavior = 100m;
        if (features.Martingale) behavior -= 30;
        if (features.AveragingDown) behavior -= 15;
        if (features.LotCv > 0.4m) behavior -= 10;
        if (features.SlUseRate < 0.5m) behavior -= 10;
        if (features.LossSizeCv > 0.8m) behavior -= 10;
        behavior = Math.Clamp(behavior, 0m, 100m);

        var quality = 50m;
        if (features.NetPnl > 0) quality += 15;
        if (features.ProfitFactor >= 1.2m) quality += 10;
        if (features.ProfitFactor >= 1.8m) quality += 5;
        quality += behavior * 0.2m;
        quality -= risk * 0.25m;
        if (features.CompletedXauTrades < EarlyScoreTradeCount)
            quality = Math.Min(quality, 40m);
        quality = Math.Clamp(decimal.Round(quality, 2), 0m, 100m);

        var state = TraderStateMachine.FromBaseline(eligible, quality, risk, features);
        return new BaselineScore
        {
            Features = features,
            RiskScore = decimal.Round(risk, 2),
            BehaviorScore = decimal.Round(behavior, 2),
            EarlyQualityScore = quality,
            SuggestedState = state,
            EarlyScoreEligible = eligible
        };
    }

    private static decimal CoefficientOfVariation(IReadOnlyList<decimal> values)
    {
        if (values.Count < 2)
            return 0;
        var mean = values.Average();
        if (mean == 0)
            return 0;
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        var std = (decimal)Math.Sqrt((double)variance);
        return decimal.Round(Math.Abs(std / mean), 4);
    }
}

public static class TraderStateMachine
{
    public static TraderState FromBaseline(bool earlyEligible, decimal quality, decimal risk, FeatureSnapshot features)
    {
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;

        if (risk >= 80 || (features.Martingale && features.MaxDrawdown > 0 && features.NetPnl < 0))
            return TraderState.RISK_BLOCKED;

        if (!earlyEligible)
            return TraderState.INSUFFICIENT_DATA;

        if (quality >= 70 && risk < 40)
            return TraderState.SHADOW;

        if (quality >= 55)
            return TraderState.WATCH;

        return TraderState.EARLY_SCORE;
    }

    public static TraderState AfterHighEarlyScore() => TraderState.SHADOW;

    public static bool CanPromoteToLive(TraderState current) => false;
}
