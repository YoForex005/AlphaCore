namespace TraderIntelligence.Domain.Enums;

public enum TraderState
{
    INSUFFICIENT_DATA = 0,
    EARLY_SCORE = 1,
    WATCH = 2,
    SHADOW = 3,
    LIVE_CANDIDATE = 4,
    LIVE = 5,
    PAUSED = 6,
    RISK_BLOCKED = 7,
    DISQUALIFIED = 8
}

