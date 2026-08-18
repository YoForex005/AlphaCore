using System.Text.Json;
using TraderIntelligence.Fix.CTrader.Sessions;
using TraderIntelligence.Infrastructure.Copy;
using TraderIntelligence.Infrastructure.Mt5Live;
using TraderIntelligence.Mt5.Env;

EnvFile.FindAndLoad();

var host = Environment.GetEnvironmentVariable("CTRADER_FIX_HOST") ?? "";
var sender = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_SENDER_COMP_ID") ?? "";
var target = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_TARGET_COMP_ID") ?? "cServer";
var account = Environment.GetEnvironmentVariable("CTRADER_FIX_ACCOUNT_ID") ?? "";
var password = Environment.GetEnvironmentVariable("CTRADER_FIX_PASSWORD") ?? "";

if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase) || account == "1369850")
{
    Console.WriteLine("REFUSED live dest");
    return 2;
}

var connectors = LiveMt5Registration.CreateConnectorsFromEnvironment();
foreach (var c in connectors)
    await c.ConnectAsync(CancellationToken.None);

Console.WriteLine("FAST_COPY_WATCH manager-position poll 500ms (not reconstructed HTTP)");

while (true)
{
    try
    {
        var ledger = DemoCopyLedger.Load();
        var openByBroker = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        var probeOk = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var loginGroup in ledger.Where(f => !f.DestClosed).GroupBy(f => (f.Broker ?? "ACHIEVER", f.SourceLogin)))
        {
            var broker = loginGroup.Key.Item1;
            if (!long.TryParse(loginGroup.Key.Item2, out var login))
                continue;
            var conn = connectors.FirstOrDefault(c =>
                string.Equals(c.BrokerCode, broker, StringComparison.OrdinalIgnoreCase));
            if (conn is null)
                continue;
            try
            {
                var pos = await conn.GetPositionsAsync(login, CancellationToken.None);
                var key = broker + ":" + login;
                probeOk.Add(key);
                openByBroker[key] = pos.Select(p => p.PositionTicket.ToString()).ToHashSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine("POS_FAIL " + broker + "/" + login + " " + ex.GetType().Name);
            }
        }

        var closed = 0;
        foreach (var fill in ledger.Where(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId)).ToList())
        {
            var broker = string.IsNullOrWhiteSpace(fill.Broker) ? "ACHIEVER" : fill.Broker;
            var key = broker + ":" + fill.SourceLogin;
            if (!probeOk.Contains(key))
                continue;
            if (openByBroker[key].Contains(fill.SourcePositionId))
                continue;

            var r = await CTraderFixCopyOpen.SendAsync(
                host, sender, target, account, password,
                fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots,
                CancellationToken.None, fill.DestPositionId);
            Console.WriteLine($"CLOSE {fill.SourceLogin}/{fill.SourcePositionId} dest={fill.DestPositionId} filled={r.Filled} px={r.LastPx} err={r.Error}");
            if (r.Filled || r.OrderSent)
            {
                fill.DestClosed = true;
                closed++;
            }
        }

        if (closed > 0)
            DemoCopyLedger.Save(ledger);
    }
    catch (Exception ex)
    {
        Console.WriteLine("LOOP " + ex.GetType().Name + ": " + ex.Message);
    }

    await Task.Delay(500);
}
