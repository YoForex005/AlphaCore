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

var flattenOnly = args.Any(a => string.Equals(a, "--flatten-only", StringComparison.OrdinalIgnoreCase));
var matrix = args.Any(a => string.Equals(a, "--matrix", StringComparison.OrdinalIgnoreCase));
var cleanup = args.Any(a => string.Equals(a, "--cleanup", StringComparison.OrdinalIgnoreCase));
var copyOpen = args.Any(a => string.Equals(a, "--copy-open", StringComparison.OrdinalIgnoreCase));
var watch = args.Any(a => string.Equals(a, "--watch", StringComparison.OrdinalIgnoreCase));
Directory.CreateDirectory(@"D:\Prop\reports\swarm\20260818");
if (watch)
{
    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    Console.WriteLine("DEMO_COPY_WATCH start (close poll 500ms; open scan ~10s)");
    var tick = 0;
    while (true)
    {
        try
        {
            var ledgerPath = @"D:\Prop\data\demo_copy_ledger.json";
            var ledger = new List<WatchFill>();
            if (File.Exists(ledgerPath))
            {
                try { ledger = JsonSerializer.Deserialize<List<WatchFill>>(File.ReadAllText(ledgerPath)) ?? []; }
                catch { ledger = []; }
            }
            if (ledger.All(x => x.SourceLogin != "305750" || x.SourcePositionId != "21250421"))
            {
                ledger.Add(new WatchFill
                {
                    SourceLogin = "305750",
                    SourcePositionId = "21250421",
                    IsLong = true,
                    Lots = 0.01m,
                    DestPositionId = "237339770",
                    DestClOrdId = "C20260818093047317",
                    DestFillPrice = 4390.2m
                });
            }

            foreach (var fill in ledger.Where(f => !f.DestClosed && !string.IsNullOrWhiteSpace(f.DestPositionId)).ToList())
            {
                var fillBroker = string.IsNullOrWhiteSpace(fill.Broker) ? "ACHIEVER" : fill.Broker;
                var json = await http.GetStringAsync($"http://127.0.0.1:5000/api/traders/{fillBroker}/{fill.SourceLogin}");
                using var doc = JsonDocument.Parse(json);
                var closed = false;
                if (doc.RootElement.TryGetProperty("trades", out var trades))
                {
                    foreach (var t in trades.EnumerateArray())
                    {
                        if (t.GetProperty("positionId").GetInt64().ToString() != fill.SourcePositionId)
                            continue;
                        closed = t.GetProperty("completed").GetBoolean();
                    }
                }
                if (!closed)
                    continue;
                var r = await CTraderFixCopyOpen.SendAsync(host, sender, target, account, password,
                    fill.SourceLogin, fill.SourcePositionId, fill.IsLong, fill.Lots, CancellationToken.None, fill.DestPositionId);
                Console.WriteLine($"CLOSE {fill.SourceLogin}/{fill.SourcePositionId} filled={r.Filled} err={r.Error} px={r.LastPx}");
                if (r.Filled || r.OrderSent)
                    fill.DestClosed = true;
            }

            var openedThisTick = 0;
            tick++;
            if (tick % 20 != 0)
            {
                Directory.CreateDirectory(@"D:\Prop\data");
                File.WriteAllText(ledgerPath, JsonSerializer.Serialize(ledger, new JsonSerializerOptions { WriteIndented = true }));
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                continue;
            }
            var listJson = await http.GetStringAsync("http://127.0.0.1:5000/api/traders");
            using var listDoc = JsonDocument.Parse(listJson);
            foreach (var row in listDoc.RootElement.EnumerateArray())
            {
                if (openedThisTick >= 3)
                    break;
                var state = row.GetProperty("state").GetString() ?? "";
                if (state is not ("SHADOW" or "LIVE_CANDIDATE" or "LIVE"))
                    continue;
                if (row.GetProperty("martingale").GetBoolean())
                    continue;
                if (row.TryGetProperty("lotEscalation", out var le) && le.GetBoolean())
                    continue;
                var group = row.TryGetProperty("group", out var g) ? g.GetString() ?? "" : "";
                if (group.IndexOf("demo", StringComparison.OrdinalIgnoreCase) < 0
                    && group.IndexOf("contest", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var login = row.GetProperty("login").GetInt64();
                var broker = row.TryGetProperty("broker", out var br) ? (br.GetString() ?? "ACHIEVER") : "ACHIEVER";
                var detJson = await http.GetStringAsync($"http://127.0.0.1:5000/api/traders/{broker}/{login}");
                using var det = JsonDocument.Parse(detJson);
                if (!det.RootElement.TryGetProperty("trades", out var tlist))
                    continue;
                foreach (var t in tlist.EnumerateArray())
                {
                    if (openedThisTick >= 3)
                        break;
                    if (t.GetProperty("canonicalSymbol").GetString() != "XAUUSD")
                        continue;
                    if (t.GetProperty("completed").GetBoolean())
                        continue;
                    var lots = t.GetProperty("maxVolumeLots").GetDecimal();
                    if (lots <= 0)
                        continue;
                    var pos = t.GetProperty("positionId").GetInt64().ToString();
                    if (ledger.Any(f => f.SourceLogin == login.ToString() && f.SourcePositionId == pos && !string.IsNullOrWhiteSpace(f.DestPositionId)))
                        continue;
                    var isLong = (t.GetProperty("direction").GetString() ?? "Long") != "Short";
                    var r = await CTraderFixCopyOpen.SendAsync(host, sender, target, account, password,
                        login.ToString(), pos, isLong, lots, CancellationToken.None);
                    Console.WriteLine($"OPEN {login}/{pos} lots={lots} long={isLong} filled={r.Filled} px={r.LastPx} dest={r.PosId} err={r.Error}");
                    if (!r.Filled)
                        continue;
                    ledger.Add(new WatchFill
                    {
                        Broker = broker,
                        SourceLogin = login.ToString(),
                        SourcePositionId = pos,
                        IsLong = isLong,
                        Lots = lots,
                        DestPositionId = r.PosId,
                        DestClOrdId = r.ClOrdId,
                        DestFillPrice = decimal.TryParse(r.LastPx, out var px) ? px : null
                    });
                    openedThisTick++;
                }
            }

            Directory.CreateDirectory(@"D:\Prop\data");
            File.WriteAllText(ledgerPath, JsonSerializer.Serialize(ledger, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"TICK ledger={ledger.Count} destOpen={ledger.Count(x => !x.DestClosed)} newOpens={openedThisTick}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("WATCH " + ex.GetType().Name + ": " + ex.Message);
        }
        await Task.Delay(TimeSpan.FromMilliseconds(500));
    }
}
if (copyOpen)
{
    var login = args.SkipWhile(a => a != "--copy-open").Skip(1).FirstOrDefault() ?? "312762";
    var pos = args.SkipWhile(a => a != "--copy-open").Skip(2).FirstOrDefault() ?? "21251046";
    var copy = await CTraderFixCopyOpen.SendAsync(
        host, sender, target, account, password,
        login, pos, isLong: true, lots: 0.01m, CancellationToken.None);
    var copyJson = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(@"D:\Prop\reports\swarm\20260818\DEMO_COPY_OPEN.json", copyJson);
    Console.WriteLine(copyJson);
    return copy.Filled || copy.OrderSent ? 0 : 4;
}
if (matrix || cleanup)
{
    var rows = await CTraderFixDemoMatrix.RunAsync(host, sender, target, account, password, CancellationToken.None, cleanup);
    var json = JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(@"D:\Prop\reports\swarm\20260818\DEMO_FIX_MATRIX.json", json);
    Console.WriteLine(json);
    return rows.All(r => r.Pass) ? 0 : 3;
}
var result = await CTraderFixDemoTestTrade.SendAsync(host, 5212, sender, target, account, password, CancellationToken.None, flattenOnly);
var one = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(@"D:\Prop\reports\swarm\20260818\DEMO_FIX_TEST_TRADE.json", one);
Console.WriteLine(one);
return result.OrderSent || result.Flattened ? 0 : 2;

file sealed class WatchFill
{
    public string Broker { get; set; } = "ACHIEVER";
    public string SourceLogin { get; set; } = "";
    public string SourcePositionId { get; set; } = "";
    public bool IsLong { get; set; }
    public decimal Lots { get; set; }
    public string? DestPositionId { get; set; }
    public string? DestClOrdId { get; set; }
    public decimal? DestFillPrice { get; set; }
    public bool DestClosed { get; set; }
}
