using System.Text.Json;
using TraderIntelligence.Application.Contracts;
using TraderIntelligence.Infrastructure.Mt5Live;
using TraderIntelligence.Mt5.Env;

var envPath = EnvFile.FindAndLoad();
var aPass = Environment.GetEnvironmentVariable("MT5_PASSWORD");
var sPass = Environment.GetEnvironmentVariable("MT5_STARWAVEFX_PASSWORD");
if (string.IsNullOrWhiteSpace(aPass) || string.IsNullOrWhiteSpace(sPass))
{
    Console.WriteLine(JsonSerializer.Serialize(new { ok = false, error = "real_passwords_missing", env = envPath }));
    return 2;
}

var reportDir = @"D:\Prop\reports\swarm\20260818";
Directory.CreateDirectory(reportDir);
var brokers = new List<object>();

foreach (var connector in LiveMt5Registration.CreateConnectorsFromEnvironment())
{
    var started = DateTimeOffset.UtcNow;
    try
    {
        await connector.ConnectAsync(CancellationToken.None);
        var groups = await connector.GetGroupsAsync(CancellationToken.None);
        var accounts = await connector.GetAccountsAsync(null, CancellationToken.None);
        var positions = connector is IMt5BulkPositionReader bulk
            ? await bulk.GetGroupPositionsAsync("*", CancellationToken.None)
            : Array.Empty<Mt5PositionDto>();

        var groupRows = groups
            .Select(g => new
            {
                name = g.Name,
                currency = g.Currency,
                accounts = accounts.Count(a => string.Equals(a.GroupName, g.Name, StringComparison.OrdinalIgnoreCase))
            })
            .OrderBy(g => g.name)
            .ToList();

        var accountRows = accounts
            .Select(a => new { login = a.Login, group = a.GroupName, leverage = a.Leverage, balance = a.Balance, equity = a.Equity })
            .OrderBy(a => a.group).ThenBy(a => a.login)
            .ToList();

        brokers.Add(new
        {
            broker = connector.BrokerCode,
            connected = true,
            elapsedMs = (DateTimeOffset.UtcNow - started).TotalMilliseconds,
            groups = groupRows.Count,
            accounts = accountRows.Count,
            openPositions = positions.Count,
            groupNames = groupRows,
            traders = accountRows
        });

        await connector.DisconnectAsync(CancellationToken.None);
    }
    catch (Exception ex)
    {
        brokers.Add(new
        {
            broker = connector.BrokerCode,
            connected = false,
            error = ex.GetType().Name + ": " + ex.Message,
            groups = 0,
            accounts = 0
        });
    }
}

var payload = new
{
    probe = "LiveBrokerProbe",
    utc = DateTimeOffset.UtcNow,
    envLoaded = envPath is not null,
    note = "Passwords never written. Groups and manager logins only.",
    brokers
};

var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(Path.Combine(reportDir, "LIVE_GROUPS_AND_TRADERS.json"), json);
Console.WriteLine(json);
return 0;
