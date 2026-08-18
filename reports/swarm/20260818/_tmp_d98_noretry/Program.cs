using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Execution;

var sent = ExecutionOrderStateMachine.AfterSendAttempt();
var disc = ExecutionOrderStateMachine.AfterDisconnectWithUnknownAck();

Console.WriteLine("AFTER_SEND=" + sent);
Console.WriteLine("MAY_RETRY_AFTER_SEND=" + ExecutionOrderStateMachine.MayRetryNewOrderSingle(sent));
Console.WriteLine("RECON_AFTER_SEND=" + ExecutionOrderStateMachine.RequiresReconciliation(sent));
Console.WriteLine("AFTER_DISCONNECT=" + disc);
Console.WriteLine("MAY_RETRY_AFTER_DISCONNECT=" + ExecutionOrderStateMachine.MayRetryNewOrderSingle(disc));
Console.WriteLine("RECON_AFTER_DISCONNECT=" + ExecutionOrderStateMachine.RequiresReconciliation(disc));
Console.WriteLine("MAY_RETRY_NOTSENT=" + ExecutionOrderStateMachine.MayRetryNewOrderSingle(ExecutionOrderStatus.NotSent));
Console.WriteLine("MAY_RETRY_REJECTED=" + ExecutionOrderStateMachine.MayRetryNewOrderSingle(ExecutionOrderStatus.Rejected));
Console.WriteLine("---MATRIX---");
foreach (ExecutionOrderStatus s in Enum.GetValues<ExecutionOrderStatus>())
{
    var retry = ExecutionOrderStateMachine.MayRetryNewOrderSingle(s);
    var recon = ExecutionOrderStateMachine.RequiresReconciliation(s);
    Console.WriteLine($"{(int)s}\t{s}\t{retry}\t{recon}");
}
