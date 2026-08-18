using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Execution;

public sealed record ExecutionReportInput(
    string ClOrdId,
    string? VenueOrderId,
    string ExecType,
    string OrdStatus,
    decimal? LastQty,
    decimal? CumQty,
    decimal? LeavesQty,
    string? Text);

public static class ExecutionOrderStateMachine
{
    public static ExecutionOrderStatus AfterSendAttempt() =>
        ExecutionOrderStatus.SentAcknowledgementUnknown;

    public static ExecutionOrderStatus AfterDisconnectWithUnknownAck() =>
        ExecutionOrderStatus.ExecutionStateUnknown;

    public static ExecutionOrderStatus Apply(ExecutionOrderStatus current, ExecutionReportInput report)
    {
        var status = MapOrdStatus(report.OrdStatus, report.ExecType);
        if (current == ExecutionOrderStatus.Filled && status != ExecutionOrderStatus.Filled)
            return current;

        if (current == ExecutionOrderStatus.Rejected || current == ExecutionOrderStatus.Cancelled)
            return current;

        return status;
    }

    public static bool MayRetryNewOrderSingle(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.NotSent or ExecutionOrderStatus.Rejected;

    public static bool RequiresReconciliation(ExecutionOrderStatus status) =>
        status is ExecutionOrderStatus.SentAcknowledgementUnknown
            or ExecutionOrderStatus.ExecutionStateUnknown;

    private static ExecutionOrderStatus MapOrdStatus(string ordStatus, string execType)
    {
        var key = string.IsNullOrWhiteSpace(ordStatus) ? execType : ordStatus;
        return key.ToUpperInvariant() switch
        {
            "0" or "NEW" => ExecutionOrderStatus.Accepted,
            "1" or "PARTIAL" or "PARTIALLY FILLED" => ExecutionOrderStatus.PartiallyFilled,
            "2" or "FILL" or "FILLED" => ExecutionOrderStatus.Filled,
            "4" or "CANCELED" or "CANCELLED" => ExecutionOrderStatus.Cancelled,
            "8" or "REJECTED" or "REJECT" => ExecutionOrderStatus.Rejected,
            "A" or "PENDING_NEW" => ExecutionOrderStatus.Accepted,
            _ => ExecutionOrderStatus.ExecutionStateUnknown
        };
    }
}
