using System.Collections.Concurrent;

namespace TraderIntelligence.Application.Runtime;

public sealed class BrokerLiveStatus
{
    public string BrokerCode { get; set; } = string.Empty;
    public bool Connected { get; set; }
    public string? LastError { get; set; }
    public int Groups { get; set; }
    public int Accounts { get; set; }
    public int DealsInserted { get; set; }
    public int Positions { get; set; }
    public int Scored { get; set; }
    public string Phase { get; set; } = "idle";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class FixLiveStatus
{
    public bool LoggedOn { get; set; }
    public string Status { get; set; } = "Disconnected";
    public string? LastError { get; set; }
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LiveRuntimeStatus
{
    public ConcurrentDictionary<string, BrokerLiveStatus> Brokers { get; } = new(StringComparer.OrdinalIgnoreCase);
    public FixLiveStatus Quote { get; } = new();
    public FixLiveStatus Trade { get; } = new();
    public bool RealCopyEnabled { get; set; }
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public BrokerLiveStatus Broker(string code) =>
        Brokers.GetOrAdd(code, c => new BrokerLiveStatus { BrokerCode = c });

    public object Snapshot() => new
    {
        startedAt = StartedAt,
        realCopyEnabled = RealCopyEnabled,
        copyNote = RealCopyEnabled
            ? "REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."
            : "NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process.",
        brokers = Brokers.Values
            .OrderBy(b => b.BrokerCode)
            .Select(b => new
            {
                b.BrokerCode,
                b.Connected,
                b.LastError,
                b.Groups,
                b.Accounts,
                b.DealsInserted,
                b.Positions,
                b.Scored,
                b.Phase,
                b.UpdatedAt
            }),
        fix = new
        {
            quote = Quote,
            trade = Trade
        }
    };
}
