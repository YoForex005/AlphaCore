namespace TraderIntelligence.Domain.Enums;

public enum FixSessionStatus
{
    Disconnected = 0,
    Connecting = 1,
    LogonSent = 2,
    LoggedOn = 3,
    Reconciling = 4,
    ReadyForMarketData = 5,
    ReadyForExecution = 6,
    LogoutSent = 7,
    Error = 8
}
