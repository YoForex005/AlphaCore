using TraderIntelligence.Application.Contracts;

namespace TraderIntelligence.Mt5.Connectors;

public sealed class BrokerRegistry : IBrokerRegistry
{
    private readonly Dictionary<string, IMt5BrokerConnector> _connectors;

    public BrokerRegistry(IEnumerable<IMt5BrokerConnector> connectors)
    {
        _connectors = connectors.ToDictionary(c => c.BrokerCode, StringComparer.OrdinalIgnoreCase);
    }

    public IMt5BrokerConnector Get(string brokerCode)
    {
        if (!_connectors.TryGetValue(brokerCode, out var connector))
            throw new KeyNotFoundException($"Unknown broker '{brokerCode}'.");
        return connector;
    }

    public IReadOnlyList<IMt5BrokerConnector> All() => _connectors.Values.ToList();
}
