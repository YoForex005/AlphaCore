namespace TraderIntelligence.Domain.Enums;

public enum ReconciliationIssueType
{
    UnknownExternalPosition = 0,
    MissingInternalPosition = 1,
    QuantityMismatch = 2,
    SideMismatch = 3,
    OrphanExecutionReport = 4,
    UnexpectedFill = 5,
    UnresolvedExecutionState = 6
}
