using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace TraderIntelligence.Fix.CTrader.Sessions;

public sealed record CopyOpenResult
{
    public required bool Allowed { get; init; }
    public required bool LoggedOn { get; init; }
    public required bool OrderSent { get; init; }
    public required bool Filled { get; init; }
    public string? SourceLogin { get; init; }
    public string? SourcePositionId { get; init; }
    public string? Side { get; init; }
    public decimal Lots { get; init; }
    public string? Units { get; init; }
    public string? SymbolId { get; init; }
    public string? ClOrdId { get; init; }
    public string? LastPx { get; init; }
    public string? PosId { get; init; }
    public string? Host { get; init; }
    public string? Account { get; init; }
    public string? Raw { get; init; }
    public string? Error { get; init; }
}

public static class CTraderFixCopyOpen
{
    public static async Task<CopyOpenResult> SendAsync(
        string host, string sender, string target, string account, string password,
        string sourceLogin, string sourcePositionId, bool isLong, decimal lots,
        CancellationToken ct, string? destPositionId = null)
    {
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return Fail("Refused: dest must be demo FIX, not live 1369850.", host, account, sourceLogin);
        }

        var units = decimal.Round(lots * 100m, 2, MidpointRounding.ToZero);
        if (units < 1m)
            units = 1m;

        try
        {
            using var tcp = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            await tcp.ConnectAsync(host, 5212, timeout.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeout.Token);

            var seq = 1;
            await Write(ssl, Build("A", sender, target, seq++,
                (98, "0"), (108, "30"), (141, "Y"), (553, account), (554, password)), timeout.Token);
            var logon = await Read(ssl, timeout.Token, 12, "A", "5");
            if (Tag(logon, "35") != "A")
                return Fail("logon failed " + Tag(logon, "58"), host, account, sourceLogin, logon);

            await Write(ssl, Build("x", sender, target, seq++,
                (320, "SL" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture)),
                (559, "0")), timeout.Token);
            var list = await Read(ssl, timeout.Token, 18, "y");
            var gold = FindGold(list);
            if (gold is null)
            {
                list += await Read(ssl, timeout.Token, 10, "y");
                gold = FindGold(list);
            }
            if (gold is null)
                return Fail("no XAUUSD in SecurityList", host, account, sourceLogin, list);

            _ = await Read(ssl, timeout.Token, 3, "y");

            var closing = !string.IsNullOrWhiteSpace(destPositionId);
            var cl = (closing ? "X" : "C") + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var side = closing ? (isLong ? "2" : "1") : (isLong ? "1" : "2");
            var extra = new List<(int, string)>
            {
                (11, cl), (55, gold), (54, side), (60, now), (40, "1"),
                (38, units.ToString("0.##", CultureInfo.InvariantCulture)),
                (494, (closing ? "close-" : "copy-") + sourceLogin + "-" + sourcePositionId)
            };
            if (closing)
                extra.Add((721, destPositionId!));
            await Write(ssl, Build("D", sender, target, seq, extra.ToArray()), timeout.Token);

            var er = await Read(ssl, timeout.Token, 8, "8", "3", "j");
            if (Tag(er, "150") == "0")
                er += "||" + await Read(ssl, timeout.Token, 8, "8", "3", "j");
            var filled = er.Contains("|150=F|") || er.Contains("|39=2|");
            return new CopyOpenResult
            {
                Allowed = true,
                LoggedOn = true,
                OrderSent = Tag(er, "35") == "8" || er.Contains("|35=8|"),
                Filled = filled,
                SourceLogin = sourceLogin,
                SourcePositionId = sourcePositionId,
                Side = closing ? (isLong ? "SellToClose" : "BuyToClose") : (isLong ? "Buy" : "Sell"),
                Lots = lots,
                Units = units.ToString(CultureInfo.InvariantCulture),
                SymbolId = gold,
                ClOrdId = cl,
                LastPx = Tag(er, "6"),
                PosId = Tag(er, "721"),
                Host = host,
                Account = account,
                Raw = Sanitize(er),
                Error = Tag(er, "58")
            };
        }
        catch (Exception ex)
        {
            return Fail(ex.GetType().Name + ": " + ex.Message, host, account, sourceLogin);
        }
    }

    private static CopyOpenResult Fail(string err, string host, string account, string login, string? raw = null) =>
        new()
        {
            Allowed = !err.StartsWith("Refused", StringComparison.Ordinal),
            LoggedOn = false,
            OrderSent = false,
            Filled = false,
            SourceLogin = login,
            Host = host,
            Account = account,
            Error = err,
            Raw = raw is null ? null : Sanitize(raw)
        };

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

    private static string? FindGold(string pipe)
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
                return last55;
        }
        return null;
    }

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
        return j.Length > 900 ? j[..900] : j;
    }
}
