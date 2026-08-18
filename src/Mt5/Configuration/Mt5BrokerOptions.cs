using System.ComponentModel.DataAnnotations;

namespace TraderIntelligence.Mt5.Configuration;

/// <summary>
/// Broker configuration for MT5 integration.
/// Supports either a local C++ bridge (remote URL mode) or a direct native manager mode (future).
/// </summary>
public sealed class Mt5BrokerOptions
{
    [Required]
    public Guid BrokerId { get; set; }

    [Required]
    public string DisplayName { get; set; } = string.Empty;

    // Native manager / watchdog settings (documented for local mode).
    public string? Server { get; set; }
    public int Port { get; set; }
    public ulong Login { get; set; }
    public string? Password { get; set; } // secret placeholder in config
    public string? ServerName { get; set; }

    /// <summary>
    /// "local" or "remote" (HTTP bridge).
    /// </summary>
    [Required]
    public string Mode { get; set; } = "remote";

    public int PoolSize { get; set; } = 25;

    // Proxy settings (documented for local mode).
    public bool ProxyEnabled { get; set; }
    public string? ProxyType { get; set; }
    public string? ProxyHost { get; set; }
    public int ProxyPort { get; set; }
    public string? ProxyLogin { get; set; }
    public string? ProxyPassword { get; set; }

    // HTTP bridge settings (remote mode).
    [Required]
    public string? RemoteUrl { get; set; } // e.g. http://localhost:8080

    public string? ApiKey { get; set; }

    /// <summary>
    /// Egress IP used for broker allowlisting documentation.
    /// </summary>
    public string? EgressIp { get; set; }
}

