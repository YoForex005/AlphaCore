namespace TraderIntelligence.Domain.Enums;

public enum ExecutionOrderStatus
{
    NotSent = 0,
    SentAcknowledgementUnknown = 1,
    Accepted = 2,
    PartiallyFilled = 3,
    Filled = 4,
    Rejected = 5,
    Cancelled = 6,
    ExecutionStateUnknown = 7
}
