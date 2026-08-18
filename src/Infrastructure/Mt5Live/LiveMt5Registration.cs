using Microsoft.Extensions.Configuration;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Domain.Brokers;
using TraderIntelligence.Mt5.Connectors;

namespace TraderIntelligence.Infrastructure.Mt5Live;

public static class LiveMt5Registration
{
    public static bool HasRealPasswords(IConfiguration config)
    {
        var a = config["MT5_PASSWORD"];
        var s = config["MT5_STARWAVEFX_PASSWORD"];
        return IsSecret(a) && IsSecret(s);
    }

    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectorsFromEnvironment() =>
        CreateConnectors(new EnvConfiguration());

    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_PORT"], out var ap) ? ap : 443,
            Login = ulong.TryParse(config["MT5_LOGIN"], out var al) ? al : 0,
            Password = config["MT5_PASSWORD"] ?? "",
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ProxyHost = config["ACHIEVER_PROXY_HOST"],
            ProxyPort = int.TryParse(config["ACHIEVER_PROXY_PORT"], out var pp) ? pp : 0,
            ProxyUser = config["ACHIEVER_PROXY_USERNAME"],
            ProxyPassword = config["ACHIEVER_PROXY_PASSWORD"],
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            Server = config["MT5_STARWAVEFX_SERVER"] ?? "",
            Port = int.TryParse(config["MT5_STARWAVEFX_PORT"], out var sp) ? sp : 443,
            Login = ulong.TryParse(config["MT5_STARWAVEFX_LOGIN"], out var sl) ? sl : 0,
            Password = config["MT5_STARWAVEFX_PASSWORD"] ?? "",
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }

    private static bool IsSecret(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains("<SECRET>", StringComparison.Ordinal)
        && !value.Contains("(a/c", StringComparison.Ordinal);

    private sealed class EnvConfiguration : IConfiguration
    {
        public string? this[string key]
        {
            get => Environment.GetEnvironmentVariable(key);
            set { }
        }

        public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
        public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NoopChangeToken();
        public IConfigurationSection GetSection(string key) => new EmptySection(key);

        private sealed class NoopChangeToken : Microsoft.Extensions.Primitives.IChangeToken
        {
            public bool HasChanged => false;
            public bool ActiveChangeCallbacks => false;
            public IDisposable RegisterChangeCallback(Action<object?> callback, object? state) => Empty.Instance;
        }

        private sealed class Empty : IDisposable
        {
            public static readonly Empty Instance = new();
            public void Dispose() { }
        }

        private sealed class EmptySection : IConfigurationSection
        {
            public EmptySection(string key) => Key = key;
            public string? this[string key] { get => null; set { } }
            public string Key { get; }
            public string Path => Key;
            public string? Value { get; set; }
            public IEnumerable<IConfigurationSection> GetChildren() => Array.Empty<IConfigurationSection>();
            public Microsoft.Extensions.Primitives.IChangeToken GetReloadToken() => new NoopChangeToken();
            public IConfigurationSection GetSection(string key) => new EmptySection(key);
        }
    }
}