namespace TraderIntelligence.Domain.Enums;

public enum RiskDecisionOutcome
{
    Approve = 0,
    ReduceSize = 1,
    Reject = 2,
    PauseTrader = 3,
    PauseVenue = 4,
    GlobalStop = 5
}

