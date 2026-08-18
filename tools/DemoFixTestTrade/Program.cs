using System.Text.Json;
using TraderIntelligence.Fix.CTrader.Sessions;

foreach (var path in new[]
         {
             Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env")),
             Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")),
             @"D:\Prop\.env"
         })
{
    if (!File.Exists(path))
        continue;
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#') || !line.Contains('='))
            continue;
        var i = line.IndexOf('=');
        var key = line[..i].Trim();
        var value = line[(i + 1)..].Trim();
        Environment.SetEnvironmentVariable(key, value);
    }
    break;
}

var host = Environment.GetEnvironmentVariable("CTRADER_FIX_HOST") ?? "";
var account = Environment.GetEnvironmentVariable("CTRADER_FIX_ACCOUNT_ID") ?? "";
var sender = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_SENDER_COMP_ID") ?? "";
var target = Environment.GetEnvironmentVariable("CTRADER_FIX_TRADE_TARGET_COMP_ID") ?? "cServer";
var password = Environment.GetEnvironmentVariable("CTRADER_FIX_PASSWORD") ?? "";

var result = await CTraderFixDemoTestTrade.SendAsync(host, 5212, sender, target, account, password, CancellationToken.None);
var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
Directory.CreateDirectory(@"D:\Prop\reports\swarm\20260818");
File.WriteAllText(@"D:\Prop\reports\swarm\20260818\DEMO_FIX_TEST_TRADE.json", json);
Console.WriteLine(json);
return result.OrderSent ? 0 : 2;
