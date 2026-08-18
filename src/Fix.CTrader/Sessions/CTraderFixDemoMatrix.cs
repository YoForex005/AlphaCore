using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace TraderIntelligence.Fix.CTrader.Sessions;

public sealed record DemoScenarioResult(
    string Name,
    bool Pass,
    string Detail,
    string? Raw);

public static class CTraderFixDemoMatrix
{
    public static async Task<IReadOnlyList<DemoScenarioResult>> RunAsync(
        string host, string sender, string target, string account, string password, CancellationToken ct,
        bool cleanupOnly = false)
    {
        var results = new List<DemoScenarioResult>();
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            results.Add(new DemoScenarioResult("gate", false, "refused: not demo", null));
            return results;
        }

        using var tcp = new TcpClient();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        await tcp.ConnectAsync(host, 5212, timeout.Token);
        await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
        await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
        {
            TargetHost = host,
            EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
        }, timeout.Token);

        var seq = 1;
        await Write(ssl, Build("A", sender, target, seq++, (98, "0"), (108, "30"), (141, "Y"), (553, account), (554, password)), timeout.Token);
        var logon = await Read(ssl, timeout.Token, 12, "A", "5");
        if (Type(logon) != "A")
        {
            results.Add(new DemoScenarioResult("logon", false, Tag(logon, "58") ?? logon, Sanitize(logon)));
            return results;
        }
        results.Add(new DemoScenarioResult("logon", true, "35=A", null));

        await Write(ssl, Build("x", sender, target, seq++,
            (320, "SL" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture)),
            (559, "0")), timeout.Token);
        var list = await Read(ssl, timeout.Token, 20, "y");
        var gold = FindGold(list);
        if (gold.id is null)
        {
            list += await Read(ssl, timeout.Token, 12, "y");
            gold = FindGold(list);
        }
        if (gold.id is null)
        {
            results.Add(new DemoScenarioResult("security-list", false, "no XAUUSD", Sanitize(list)));
            return results;
        }
        results.Add(new DemoScenarioResult("security-list", true, "XAUUSD id=" + gold.id, null));
        _ = await Read(ssl, timeout.Token, 3, "y");
        var sym = gold.id;

        await Write(ssl, Build("AF", sender, target, seq++,
            (584, "MS" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture)),
            (585, "7")), timeout.Token);
        var mass = await Read(ssl, timeout.Token, 6, "8", "j");
        foreach (var chunk in mass.Split("8=FIX.4.4", StringSplitOptions.RemoveEmptyEntries))
        {
            var msg = "8=FIX.4.4" + chunk;
            var oid = Tag(msg, "11");
            var st = Tag(msg, "39");
            if (oid is null || st is not ("0" or "1"))
                continue;
            await Write(ssl, Build("F", sender, target, seq++,
                (11, Cl("CL")), (41, oid)), timeout.Token);
            _ = await Read(ssl, timeout.Token, 5, "8", "9", "3");
        }
        if (cleanupOnly)
        {
            results.Add(new DemoScenarioResult("cleanup-working", true, "sent 35=F 11+41 only for working 39=0/1", Sanitize(mass)));
            return results;
        }

        async Task<string> SendD(params (int, string)[] extra)
        {
            await Write(ssl, Build("D", sender, target, seq++, extra), timeout.Token);
            var er = await Read(ssl, timeout.Token, 8, "8", "3", "j");
            if (Tag(er, "150") == "0" && (Tag(er, "39") == "0" || Tag(er, "151") == "1"))
                er += "||" + await Read(ssl, timeout.Token, 6, "8", "3", "j");
            return er;
        }

        string Cl(string p) => p + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture);
        string Now() => DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);

        // Market buy + absolute SL/TP then flatten
        var buyId = Cl("MB");
        var buy = await SendD((11, buyId), (55, sym), (54, "1"), (60, Now()), (40, "1"), (38, "1"));
        var buyFill = buy.Contains("|150=F|") || buy.Contains("|39=2|");
        var pos = Tag(buy, "721");
        var slEcho = buy.Contains("1002=") || buy.Contains("1000=");
        results.Add(new DemoScenarioResult("market-buy-sl-tp", buyFill,
            "fill=" + buyFill + " sltpEcho=" + slEcho + " px=" + (Tag(buy, "6") ?? "?") + " pos=" + pos,
            Sanitize(buy)));
        if (buyFill && pos is not null)
        {
            var close = await SendD((11, Cl("CB")), (55, sym), (54, "2"), (60, Now()), (40, "1"), (38, "1"), (721, pos));
            results.Add(new DemoScenarioResult("flatten-buy", close.Contains("|150=F|") || close.Contains("|39=2|"),
                Tag(close, "150") + "/" + Tag(close, "39"), Sanitize(close)));
        }

        // Market sell then flatten
        var sell = await SendD((11, Cl("MS")), (55, sym), (54, "2"), (60, Now()), (40, "1"), (38, "1"));
        var sellFill = sell.Contains("|150=F|") || sell.Contains("|39=2|");
        var sellPos = Tag(sell, "721");
        results.Add(new DemoScenarioResult("market-sell", sellFill, "px=" + (Tag(sell, "6") ?? "?"), Sanitize(sell)));
        if (sellFill && sellPos is not null)
        {
            var close = await SendD((11, Cl("CS")), (55, sym), (54, "1"), (60, Now()), (40, "1"), (38, "1"), (721, sellPos));
            results.Add(new DemoScenarioResult("flatten-sell", close.Contains("|150=F|") || close.Contains("|39=2|"),
                Tag(close, "150") + "/" + Tag(close, "39"), Sanitize(close)));
        }

        // Limit buy far below — should rest (New), then cancel
        var limB = Cl("LB");
        var limBuy = await SendD((11, limB), (55, sym), (54, "1"), (60, Now()), (40, "2"), (38, "1"), (44, "1000"));
        var limNew = Tag(limBuy, "150") == "0" && !limBuy.Contains("|150=F|");
        results.Add(new DemoScenarioResult("limit-buy-resting", limNew || Tag(limBuy, "150") == "8",
            "150=" + Tag(limBuy, "150") + " 39=" + Tag(limBuy, "39") + " 58=" + Tag(limBuy, "58"), Sanitize(limBuy)));
        if (limNew)
        {
            await Write(ssl, Build("F", sender, target, seq++,
                (11, Cl("XB")), (41, limB)), timeout.Token);
            var cxl = await Read(ssl, timeout.Token, 8, "8", "9", "3");
            results.Add(new DemoScenarioResult("cancel-limit-buy",
                Tag(cxl, "150") is "4" || Tag(cxl, "39") == "4",
                "150=" + Tag(cxl, "150"), Sanitize(cxl)));
        }

        // Limit sell far above
        var limS = Cl("LS");
        var limSell = await SendD((11, limS), (55, sym), (54, "2"), (60, Now()), (40, "2"), (38, "1"), (44, "9000"));
        var limSNew = Tag(limSell, "150") == "0" && !limSell.Contains("|150=F|");
        results.Add(new DemoScenarioResult("limit-sell-resting", limSNew || Tag(limSell, "150") == "8",
            "150=" + Tag(limSell, "150") + " 58=" + Tag(limSell, "58"), Sanitize(limSell)));
        if (limSNew)
        {
            await Write(ssl, Build("F", sender, target, seq++,
                (11, Cl("XS")), (41, limS)), timeout.Token);
            var cxl = await Read(ssl, timeout.Token, 8, "8", "9", "3");
            results.Add(new DemoScenarioResult("cancel-limit-sell",
                Tag(cxl, "150") is "4" || Tag(cxl, "39") == "4",
                "150=" + Tag(cxl, "150"), Sanitize(cxl)));
        }

        // Stop buy far above
        var stpB = Cl("SB");
        var stopBuy = await SendD((11, stpB), (55, sym), (54, "1"), (60, Now()), (40, "3"), (38, "1"), (99, "9000"));
        results.Add(new DemoScenarioResult("stop-buy-resting",
            Tag(stopBuy, "150") is "0" or "8",
            "150=" + Tag(stopBuy, "150") + " 58=" + Tag(stopBuy, "58"), Sanitize(stopBuy)));
        if (Tag(stopBuy, "150") == "0")
        {
            await Write(ssl, Build("F", sender, target, seq++,
                (11, Cl("XSB")), (41, stpB)), timeout.Token);
            var cxl = await Read(ssl, timeout.Token, 8, "8", "9", "3");
            results.Add(new DemoScenarioResult("cancel-stop-buy",
                Tag(cxl, "150") is "4" || Tag(cxl, "39") == "4",
                "150=" + Tag(cxl, "150"), Sanitize(cxl)));
        }

        // Stop sell far below
        var stpS = Cl("SS");
        var stopSell = await SendD((11, stpS), (55, sym), (54, "2"), (60, Now()), (40, "3"), (38, "1"), (99, "1000"));
        results.Add(new DemoScenarioResult("stop-sell-resting",
            Tag(stopSell, "150") is "0" or "8",
            "150=" + Tag(stopSell, "150") + " 58=" + Tag(stopSell, "58"), Sanitize(stopSell)));
        if (Tag(stopSell, "150") == "0")
        {
            await Write(ssl, Build("F", sender, target, seq++,
                (11, Cl("XSS")), (41, stpS)), timeout.Token);
            var cxl = await Read(ssl, timeout.Token, 8, "8", "9", "3");
            results.Add(new DemoScenarioResult("cancel-stop-sell",
                Tag(cxl, "150") is "4" || Tag(cxl, "39") == "4",
                "150=" + Tag(cxl, "150"), Sanitize(cxl)));
        }

        // Reject: unknown instrument
        var bad = await SendD((11, Cl("BAD")), (55, "99999999"), (54, "1"), (60, Now()), (40, "1"), (38, "1"));
        results.Add(new DemoScenarioResult("reject-bad-symbol",
            Tag(bad, "150") == "8" || Tag(bad, "39") == "8" || Type(bad) is "3" or "j",
            "150=" + Tag(bad, "150") + " 58=" + Tag(bad, "58"), Sanitize(bad)));

        return results;
    }

    private static string Build(string type, string sender, string target, int seq, params (int, string)[] extra)
    {
        var fields = new List<(int, string)>
        {
            (35, type), (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender), (56, target), (50, "TRADE"), (57, "TRADE"),
            (52, DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture))
        };
        fields.AddRange(extra);
        static string Pair(int t, string v) => t.ToString(CultureInfo.InvariantCulture) + "=" + v + "\u0001";
        var body = string.Concat(fields.Select(f => Pair(f.Item1, f.Item2)));
        var head = Pair(8, "FIX.4.4") + Pair(9, body.Length.ToString(CultureInfo.InvariantCulture));
        var soFar = head + body;
        return soFar + Pair(10, (soFar.Sum(ch => (int)ch) % 256).ToString("000", CultureInfo.InvariantCulture));
    }

    private static async Task Write(SslStream ssl, string msg, CancellationToken ct)
    {
        var b = Encoding.ASCII.GetBytes(msg);
        await ssl.WriteAsync(b, ct);
        await ssl.FlushAsync(ct);
    }

    private static async Task<string> Read(SslStream ssl, CancellationToken ct, int seconds, params string[] types)
    {
        var acc = new StringBuilder();
        var tmp = new byte[16384];
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            using var slice = CancellationTokenSource.CreateLinkedTokenSource(ct);
            slice.CancelAfter(1500);
            int n;
            try { n = await ssl.ReadAsync(tmp, slice.Token); }
            catch (OperationCanceledException)
            {
                if (acc.Length > 0) break;
                continue;
            }
            if (n <= 0) break;
            acc.Append(Encoding.ASCII.GetString(tmp, 0, n));
            var pipe = acc.ToString().Replace('\u0001', '|');
            foreach (var t in types)
                if (pipe.Contains("|35=" + t + "|", StringComparison.Ordinal))
                    return pipe;
        }
        return acc.ToString().Replace('\u0001', '|');
    }

    private static (string? id, string? name) FindGold(string pipe)
    {
        string? last55 = null, lastName = null;
        foreach (var p in pipe.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = p.IndexOf('=');
            if (i <= 0) continue;
            var tag = p[..i];
            var val = p[(i + 1)..];
            if (tag == "55") last55 = val;
            if (tag is "1007" or "107") lastName = val;
            if (lastName is not null && lastName.Equals("XAUUSD", StringComparison.OrdinalIgnoreCase) && last55 is not null)
                return (last55, lastName);
        }
        return (null, null);
    }

    private static string? Type(string pipe) => Tag(pipe, "35");

    private static string? Tag(string pipe, string tag)
    {
        foreach (var part in pipe.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i > 0 && part[..i] == tag) return part[(i + 1)..];
        }
        return null;
    }

    private static string Sanitize(string pipe)
    {
        var j = string.Join('|', pipe.Split('|').Where(p => !p.StartsWith("554=", StringComparison.Ordinal)));
        return j.Length > 800 ? j[..800] : j;
    }
}
