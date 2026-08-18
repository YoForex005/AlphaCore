using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;

namespace TraderIntelligence.Fix.CTrader.Sessions;

public sealed record DestVenuePosition(string PosId, string? SymbolId, string? LongQty, string? ShortQty);

public sealed record DestBookResult
{
    public required bool Allowed { get; init; }
    public required bool LoggedOn { get; init; }
    public required bool Complete { get; init; }
    public IReadOnlyList<DestVenuePosition> Positions { get; init; } = [];
    public string? Error { get; init; }
    public string? Raw { get; init; }
}

public static class CTraderFixDestBook
{
    public static async Task<DestBookResult> RequestAsync(
        string host, string sender, string target, string account, string password, CancellationToken ct)
    {
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !sender.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return new DestBookResult
            {
                Allowed = false,
                LoggedOn = false,
                Complete = false,
                Error = "Refused: dest book is demo FIX only, not live 1369850."
            };
        }

        try
        {
            using var tcp = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(40));
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
            {
                return new DestBookResult
                {
                    Allowed = true,
                    LoggedOn = false,
                    Complete = false,
                    Error = "logon failed " + Tag(logon, "58"),
                    Raw = Sanitize(logon)
                };
            }

            var req = "P" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture);
            await Write(ssl, Build("AN", sender, target, seq, (710, req)), timeout.Token);
            var raw = await ReadAll(ssl, timeout.Token, 20);
            var positions = ParsePositions(raw, req);
            var complete = positions.Complete;
            return new DestBookResult
            {
                Allowed = true,
                LoggedOn = true,
                Complete = complete,
                Positions = positions.Rows,
                Error = complete ? null : "dest 35=AN incomplete",
                Raw = Sanitize(raw)
            };
        }
        catch (Exception ex)
        {
            return new DestBookResult
            {
                Allowed = true,
                LoggedOn = false,
                Complete = false,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static (bool Complete, List<DestVenuePosition> Rows) ParsePositions(string pipe, string reqId)
    {
        var rows = new List<DestVenuePosition>();
        int? expected = null;
        var sawEmpty = false;
        foreach (var msg in SplitMessages(pipe))
        {
            if (Tag(msg, "35") != "AP")
                continue;
            var echoed = Tag(msg, "710");
            if (!string.IsNullOrWhiteSpace(echoed) && echoed != reqId)
                continue;
            var result = Tag(msg, "728");
            if (int.TryParse(Tag(msg, "727"), out var total))
                expected = total;
            if (result == "2")
                sawEmpty = true;
            var pos = Tag(msg, "721");
            if (!string.IsNullOrWhiteSpace(pos))
                rows.Add(new DestVenuePosition(pos, Tag(msg, "55"), Tag(msg, "704"), Tag(msg, "705")));
        }

        if (sawEmpty && rows.Count == 0)
            return (true, rows);
        if (expected is int n && rows.Count >= n)
            return (true, rows);
        return (false, rows);
    }

    private static IEnumerable<string> SplitMessages(string pipe)
    {
        var parts = pipe.Split("8=FIX.4.4|", StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
            yield return "8=FIX.4.4|" + p;
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

    private static async Task<string> ReadAll(SslStream ssl, CancellationToken ct, int seconds)
    {
        var acc = new StringBuilder();
        var tmp = new byte[16384];
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        var idle = 0;
        while (DateTime.UtcNow < deadline && idle < 3)
        {
            using var slice = CancellationTokenSource.CreateLinkedTokenSource(ct);
            slice.CancelAfter(1500);
            int n;
            try { n = await ssl.ReadAsync(tmp, slice.Token); }
            catch (OperationCanceledException)
            {
                idle++;
                continue;
            }
            if (n <= 0) break;
            idle = 0;
            acc.Append(Encoding.ASCII.GetString(tmp, 0, n));
        }
        return acc.ToString().Replace('\u0001', '|');
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
        return j.Length > 400 ? j[..400] : j;
    }
}
