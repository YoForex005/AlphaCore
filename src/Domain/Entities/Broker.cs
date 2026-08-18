namespace TraderIntelligence.Domain.Entities;

public sealed class Broker
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; }
    public long ManagerLogin { get; set; }
    public string ServerName { get; set; } = string.Empty;
    public string Mode { get; set; } = "local";
    public int PoolSize { get; set; } = 4;
    public bool ProxyEnabled { get; set; }
    public string? ProxyHost { get; set; }
    public int? ProxyPort { get; set; }
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
