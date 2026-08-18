using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Domain.Entities;

public sealed class FixSessionState
{
    public Guid Id { get; set; }
    public FixSessionQualifier Qualifier { get; set; }
    public FixSessionStatus Status { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string SenderCompId { get; set; } = string.Empty;
    public string TargetCompId { get; set; } = string.Empty;
    public string? SenderSubId { get; set; }
    public string? TargetSubId { get; set; }
    public int InboundSeq { get; set; }
    public int OutboundSeq { get; set; }
    public DateTimeOffset? LastInboundAt { get; set; }
    public DateTimeOffset? LastOutboundAt { get; set; }
    public int ReconnectCount { get; set; }
    public string? LastError { get; set; }
    public bool OwnerHeld { get; set; }
    public string? OwnerInstance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
