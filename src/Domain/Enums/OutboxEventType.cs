namespace TraderIntelligence.Domain.Enums;

public enum OutboxEventType
{
    TradeCompleted = 0,
    ScoreUpdate = 1,
    ShadowCopyIntent = 2,
    RiskCheckRequest = 3,
    NotificationEvent = 4
}

