using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Risk;

public sealed class RiskLimits
{
    public decimal MaxLossPerTrader { get; init; } = 500m;
    public decimal MaxDailyExecutionLoss { get; init; } = 2_000m;
    public decimal MaxPortfolioDrawdown { get; init; } = 3_000m;
    public decimal MaxXauGrossExposure { get; init; } = 20m;
    public decimal MaxXauNetExposure { get; init; } = 10m;
    public decimal MaxPositionQuantity { get; init; } = 5m;
    public int MaxOpenPositions { get; init; } = 20;
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
    public decimal MaxMarginUsage { get; init; } = 0.70m;
    public bool BlockMartingale { get; init; } = true;
    public bool BlockAbnormalSizing { get; init; } = true;
}

public sealed record DestinationQuote(
    string CanonicalSymbol,
    string? VenueInstrumentId,
    decimal Bid,
    decimal Ask,
    DateTimeOffset ReceivedAt,
    DateTimeOffset? VenueTimestamp);

public sealed record RiskEvaluationRequest
{
    public required string CopyIntentId { get; init; }
    public required string BrokerId { get; init; }
    public required long SourceLogin { get; init; }
    public required CopyIntentAction Action { get; init; }
    public required decimal RequestedQuantity { get; init; }
    public required decimal ExpectedPrice { get; init; }
    public required DateTimeOffset SourceEventTime { get; init; }
    public required DateTimeOffset DecisionTime { get; init; }
    public required DestinationQuote? Quote { get; init; }
    public required bool VenueHealthy { get; init; }
    public required bool RealExecutionEnabled { get; init; }
    public required bool Reconciled { get; init; }
    public required KillSwitchMode KillSwitch { get; init; }
    public required decimal TraderRealizedLoss { get; init; }
    public required decimal DailyExecutionPnl { get; init; }
    public required decimal PortfolioDrawdown { get; init; }
    public required decimal CurrentGrossXau { get; init; }
    public required decimal CurrentNetXau { get; init; }
    public required int OpenPositions { get; init; }
    public required decimal MarginUsage { get; init; }
    public required bool MartingaleFlag { get; init; }
    public required bool AbnormalSizing { get; init; }
}

public sealed record RiskDecision
{
    public required string CopyIntentId { get; init; }
    public required RiskDecisionOutcome Outcome { get; init; }
    public required decimal ApprovedQuantity { get; init; }
    public required string Reason { get; init; }
    public required bool AllowFixSend { get; init; }
}

public sealed class RiskEngine
{
    private readonly RiskLimits _limits;

    public RiskEngine(RiskLimits? limits = null)
    {
        _limits = limits ?? new RiskLimits();
    }

    public RiskDecision Evaluate(RiskEvaluationRequest request)
    {
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");

        if (!request.Reconciled && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "VENUE_NOT_RECONCILED");

        if (!request.VenueHealthy && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseVenue, "VENUE_UNHEALTHY");

        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }

        if (request.Quote is null && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_MISSING");

        if (request.Quote is not null)
        {
            var age = request.DecisionTime - request.Quote.ReceivedAt;
            if (age > _limits.MaxQuoteAge && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "QUOTE_STALE");

            var spread = request.Quote.Ask - request.Quote.Bid;
            if (spread > _limits.MaxAllowedSpread && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "SPREAD_TOO_WIDE");

            var mid = (request.Quote.Bid + request.Quote.Ask) / 2m;
            if (Math.Abs(mid - request.ExpectedPrice) > _limits.MaxPriceMove && IsIncreasing(request.Action))
                return Reject(request, RiskDecisionOutcome.Reject, "PRICE_MOVED_TOO_FAR");
        }

        var signalAge = request.DecisionTime - request.SourceEventTime;
        if (signalAge > _limits.MaxSourceSignalAge && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "SIGNAL_STALE");

        if (request.TraderRealizedLoss <= -_limits.MaxLossPerTrader)
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MAX_LOSS_PER_TRADER");

        if (request.DailyExecutionPnl <= -_limits.MaxDailyExecutionLoss)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_DAILY_EXECUTION_LOSS");

        if (request.PortfolioDrawdown >= _limits.MaxPortfolioDrawdown)
            return Reject(request, RiskDecisionOutcome.GlobalStop, "MAX_PORTFOLIO_DRAWDOWN");

        if (request.OpenPositions >= _limits.MaxOpenPositions && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_OPEN_POSITIONS");

        if (request.RequestedQuantity > _limits.MaxPositionQuantity && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_POSITION_QUANTITY");

        if (request.CurrentGrossXau + request.RequestedQuantity > _limits.MaxXauGrossExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_XAU_GROSS");

        if (Math.Abs(request.CurrentNetXau) + request.RequestedQuantity > _limits.MaxXauNetExposure && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.ReduceSize, "MAX_XAU_NET");

        if (request.MarginUsage > _limits.MaxMarginUsage && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "MAX_MARGIN_USAGE");

        if (_limits.BlockMartingale && request.MartingaleFlag && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseTrader, "MARTINGALE_BLOCK");

        if (_limits.BlockAbnormalSizing && request.AbnormalSizing && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "ABNORMAL_SIZING_BLOCK");

        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }

        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
    }

    private static bool IsIncreasing(CopyIntentAction action) =>
        action is CopyIntentAction.OpenExposure or CopyIntentAction.IncreaseExposure;

    private static bool IsReducing(CopyIntentAction action) =>
        action is CopyIntentAction.ReduceExposure or CopyIntentAction.CloseExposure;

    private static RiskDecision Reject(RiskEvaluationRequest request, RiskDecisionOutcome outcome, string reason) =>
        new()
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = outcome,
            ApprovedQuantity = 0,
            Reason = reason,
            AllowFixSend = false
        };
}
