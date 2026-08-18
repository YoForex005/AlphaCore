using FluentAssertions;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Domain.Execution;

namespace TraderIntelligence.Tests.Unit;

public class ExecutionAndSizingTests
{
    [Fact]
    public void Unknown_ack_cannot_retry_new_order()
    {
        var sent = ExecutionOrderStateMachine.AfterSendAttempt();
        sent.Should().Be(ExecutionOrderStatus.SentAcknowledgementUnknown);
        ExecutionOrderStateMachine.MayRetryNewOrderSingle(sent).Should().BeFalse();
        ExecutionOrderStateMachine.RequiresReconciliation(sent).Should().BeTrue();
    }

    [Fact]
    public void Disconnect_after_send_is_unknown_state()
    {
        ExecutionOrderStateMachine.AfterDisconnectWithUnknownAck()
            .Should().Be(ExecutionOrderStatus.ExecutionStateUnknown);
    }

    [Fact]
    public void Filled_report_is_terminal()
    {
        var status = ExecutionOrderStateMachine.Apply(
            ExecutionOrderStatus.Accepted,
            new ExecutionReportInput("c1", "v1", "FILL", "2", 0.1m, 0.1m, 0, null));
        status.Should().Be(ExecutionOrderStatus.Filled);
    }

    [Fact]
    public void Quantity_normalizer_steps_and_min()
    {
        var n = new QuantityNormalizer();
        var spec = new InstrumentQuantitySpec(0.01m, 5m, 0.01m, 2);
        n.Normalize(0.10m, 1m, spec).Should().Be(0.10m);
        n.Normalize(0.10m, 0.05m, spec).Should().Be(0m);
        n.Normalize(0.333m, 1m, spec).Should().Be(0.33m);
    }

    [Fact]
    public void ClOrdId_is_deterministic_and_unique_per_sequence()
    {
        var f = new ClOrdIdFactory();
        var now = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        var a = f.Next("intent-1", now, 0);
        var b = f.Next("intent-1", now, 1);
        a.Should().NotBe(b);
        a.Should().StartWith("TI20260818120000");
    }

    [Fact]
    public void Copy_intent_expires()
    {
        var t = DateTimeOffset.Parse("2026-08-18T12:00:00Z");
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(16), TimeSpan.FromSeconds(15)).Should().BeTrue();
        CopyIntentExpiry.IsExpired(t, t.AddSeconds(5), TimeSpan.FromSeconds(15)).Should().BeFalse();
    }
}
