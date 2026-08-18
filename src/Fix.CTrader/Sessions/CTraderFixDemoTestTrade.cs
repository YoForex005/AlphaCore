using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Fix.CTrader.Sessions;

public sealed class DemoTestTradeResult
{
    public required bool Allowed { get; init; }
    public required bool LoggedOn { get; init; }
    public required bool OrderSent { get; init; }
    public required bool Filled { get; init; }
    public required bool Flattened { get; init; }
    public string? SymbolId { get; init; }
    public string? SymbolName { get; init; }
    public string? ClOrdId { get; init; }
    public string? ExecType { get; init; }
    public string? OrdStatus { get; init; }
    public string? LastPx { get; init; }
    public string? LastQty { get; init; }
    public string? Text { get; init; }
    public string? Host { get; init; }
    public string? Account { get; init; }
    public string? Raw { get; init; }
    public string? Error { get; init; }
}

public static class CTraderFixDemoTestTrade
{
    public static async Task<DemoTestTradeResult> SendAsync(
        string host,
        int tradePort,
        string senderCompId,
        string targetCompId,
        string account,
        string password,
        CancellationToken ct,
        bool flattenOnly = false)
    {
        if (!host.StartsWith("demo-", StringComparison.OrdinalIgnoreCase)
            || !senderCompId.StartsWith("demo.", StringComparison.OrdinalIgnoreCase)
            || senderCompId.Contains("live.", StringComparison.OrdinalIgnoreCase)
            || host.Contains("live-", StringComparison.OrdinalIgnoreCase)
            || account == "1369850")
        {
            return new DemoTestTradeResult
            {
                Allowed = false,
                LoggedOn = false,
                OrderSent = false,
                Filled = false,
                Flattened = false,
                Error = "Refused: test trade is demo-only (host/sender/account gate).",
                Host = host,
                Account = account
            };
        }

        try
        {
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(45));
            await tcp.ConnectAsync(host, tradePort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeoutCts.Token);

            var seq = 1;
            await WriteAsync(ssl, Build("A", senderCompId, targetCompId, "TRADE", "TRADE", seq++,
                (98, "0"), (108, "30"), (141, "Y"), (553, account), (554, password)), timeoutCts.Token);

            var logon = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(12), "A", "5");
            if (TypeOf(logon) != "A")
            {
                return new DemoTestTradeResult
                {
                    Allowed = true,
                    LoggedOn = false,
                    OrderSent = false,
                    Filled = false,
                    Flattened = false,
                    Host = host,
                    Account = account,
                    Error = "TRADE logon failed 35=" + TypeOf(logon) + " " + Tag(logon, "58"),
                    Raw = Sanitize(logon)
                };
            }

            await WriteAsync(ssl, Build("x", senderCompId, targetCompId, "TRADE", "TRADE", seq++,
                (320, "SL-" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture)),
                (559, "0")), timeoutCts.Token);

            var list = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(20), "y");
            var (symbolId, symbolName) = FindGold(list);
            if (string.IsNullOrWhiteSpace(symbolId))
            {
                var more = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(12), "y");
                list += "|" + more;
                (symbolId, symbolName) = FindGold(list);
            }

            if (string.IsNullOrWhiteSpace(symbolId))
            {
                return new DemoTestTradeResult
                {
                    Allowed = true,
                    LoggedOn = true,
                    OrderSent = false,
                    Filled = false,
                    Flattened = false,
                    Host = host,
                    Account = account,
                    Error = "No symbol in SecurityList",
                    Raw = Sanitize(list)
                };
            }

            var posReq = "P" + DateTime.UtcNow.ToString("HHmmssfff", CultureInfo.InvariantCulture);
            await WriteAsync(ssl, Build("AN", senderCompId, targetCompId, "TRADE", "TRADE", seq++,
                (710, posReq)), timeoutCts.Token);
            var positions = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(8), "AP");
            var existingPos = Tag(positions, "721");
            var existingSym = Tag(positions, "55");
            if (!string.IsNullOrWhiteSpace(existingPos) && existingSym == symbolId)
            {
                var flattenId = "F" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                var flattenNow = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var longQty = Tag(positions, "704");
                var shortQty = Tag(positions, "705");
                var side = longQty is not null && longQty != "0" ? "2" : "1";
                var qty = (longQty is not null && longQty != "0" ? longQty : shortQty) ?? "1";
                await WriteAsync(ssl, Build("D", senderCompId, targetCompId, "TRADE", "TRADE", seq++,
                    (11, flattenId), (55, symbolId), (54, side), (60, flattenNow), (40, "1"), (38, qty), (721, existingPos)), timeoutCts.Token);
                await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(8), "8", "3", "j");
            }

            if (flattenOnly)
            {
                return new DemoTestTradeResult
                {
                    Allowed = true,
                    LoggedOn = true,
                    OrderSent = !string.IsNullOrWhiteSpace(existingPos),
                    Filled = false,
                    Flattened = !string.IsNullOrWhiteSpace(existingPos),
                    SymbolId = symbolId,
                    SymbolName = symbolName,
                    Host = host,
                    Account = account,
                    Text = existingPos is null ? "no open gold position" : "flatten submitted for " + existingPos
                };
            }

            var clOrd = "T" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
            var now = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
            await WriteAsync(ssl, Build("D", senderCompId, targetCompId, "TRADE", "TRADE", seq++,
                (11, clOrd),
                (55, symbolId),
                (54, "1"),
                (60, now),
                (40, "1"),
                (38, "1")), timeoutCts.Token);

            var er = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(8), "8", "3", "j");
            if (Tag(er, "150") == "0" && Tag(er, "39") == "0")
                er += " || " + await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(8), "8", "3", "j");
            var execType = Tag(er, "150");
            var ordStatus = Tag(er, "39");
            var filled = er.Contains("|150=F|") || er.Contains("|39=2|") || er.Contains("|39=1|")
                         || execType is "F" or "2" || ordStatus is "1" or "2";
            var rejected = execType == "8" || ordStatus == "8" || TypeOf(er) is "3" or "j";

            var flattened = false;
            if (filled)
            {
                var pos = Tag(er, "721");
                var closeId = "C" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
                var closeNow = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
                var closeFields = new List<(int, string)>
                {
                    (11, closeId),
                    (55, symbolId),
                    (54, "2"),
                    (60, closeNow),
                    (40, "1"),
                    (38, Tag(er, "32") ?? Tag(er, "14") ?? "1")
                };
                if (!string.IsNullOrWhiteSpace(pos))
                    closeFields.Add((721, pos));
                await WriteAsync(ssl, Build("D", senderCompId, targetCompId, "TRADE", "TRADE", seq,
                    closeFields.ToArray()), timeoutCts.Token);
                var closeEr = await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(12), "8", "3", "j");
                if (Tag(closeEr, "150") == "0")
                    closeEr += " || " + await ReadUntilAsync(ssl, timeoutCts.Token, TimeSpan.FromSeconds(8), "8", "3", "j");
                flattened = closeEr.Contains("|150=F|") || closeEr.Contains("|39=2|");
                er += " || CLOSE " + closeEr;
            }

            return new DemoTestTradeResult
            {
                Allowed = true,
                LoggedOn = true,
                OrderSent = true,
                Filled = filled,
                Flattened = flattened,
                SymbolId = symbolId,
                SymbolName = symbolName,
                ClOrdId = clOrd,
                ExecType = execType,
                OrdStatus = ordStatus,
                LastPx = Tag(er, "31"),
                LastQty = Tag(er, "32") ?? Tag(er, "14"),
                Text = Tag(er, "58"),
                Host = host,
                Account = account,
                Raw = Sanitize(er),
                Error = rejected ? (Tag(er, "58") ?? "rejected") : null
            };
        }
        catch (Exception ex)
        {
            return new DemoTestTradeResult
            {
                Allowed = true,
                LoggedOn = false,
                OrderSent = false,
                Filled = false,
                Flattened = false,
                Host = host,
                Account = account,
                Error = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static string Build(
        string msgType,
        string sender,
        string target,
        string senderSub,
        string targetSub,
        int seq,
        params (int tag, string value)[] extra)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, msgType),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime)
        };
        fields.AddRange(extra);
        return Assemble(fields);
    }

    private static string Assemble(IReadOnlyList<(int tag, string value)> bodyFields)
    {
        static string Pair(int tag, string value) => tag.ToString(CultureInfo.InvariantCulture) + "=" + value + "\u0001";
        var body = string.Concat(bodyFields.Select(f => Pair(f.tag, f.value)));
        var head = Pair(8, "FIX.4.4") + Pair(9, body.Length.ToString(CultureInfo.InvariantCulture));
        var soFar = head + body;
        var sum = soFar.Sum(ch => (int)ch) % 256;
        return soFar + Pair(10, sum.ToString("000", CultureInfo.InvariantCulture));
    }

    private static async Task WriteAsync(SslStream ssl, string message, CancellationToken ct)
    {
        var bytes = Encoding.ASCII.GetBytes(message);
        await ssl.WriteAsync(bytes, ct);
        await ssl.FlushAsync(ct);
    }

    private static async Task<string> ReadUntilAsync(SslStream ssl, CancellationToken ct, TimeSpan wait, params string[] wantTypes)
    {
        var acc = new StringBuilder();
        var tmp = new byte[16384];
        var deadline = DateTime.UtcNow + wait;
        while (DateTime.UtcNow < deadline)
        {
            using var slice = CancellationTokenSource.CreateLinkedTokenSource(ct);
            slice.CancelAfter(TimeSpan.FromMilliseconds(1500));
            int n;
            try
            {
                n = await ssl.ReadAsync(tmp, slice.Token);
            }
            catch (OperationCanceledException)
            {
                if (acc.Length > 0)
                    break;
                continue;
            }

            if (n <= 0)
                break;
            acc.Append(Encoding.ASCII.GetString(tmp, 0, n));
            var pipe = acc.ToString().Replace('\u0001', '|');
            foreach (var t in wantTypes)
            {
                if (pipe.Contains("|35=" + t + "|", StringComparison.Ordinal))
                    return pipe;
            }
        }

        return acc.ToString().Replace('\u0001', '|');
    }

    private static (string? id, string? name) FindGold(string pipe)
    {
        var parts = pipe.Split('|', StringSplitOptions.RemoveEmptyEntries);
        string? last55 = null;
        string? lastName = null;
        string? goldId = null;
        string? goldName = null;
        foreach (var p in parts)
        {
            var i = p.IndexOf('=');
            if (i <= 0)
                continue;
            var tag = p[..i];
            var val = p[(i + 1)..];
            if (tag == "55")
                last55 = val;
            if (tag is "107" or "965" or "58" or "1007")
                lastName = val;
            if (lastName is not null
                && lastName.Equals("XAUUSD", StringComparison.OrdinalIgnoreCase)
                && last55 is not null)
            {
                return (last55, lastName);
            }

            if (goldId is null
                && lastName is not null
                && lastName.Contains("XAUUSD", StringComparison.OrdinalIgnoreCase))
            {
                goldId = last55;
                goldName = lastName;
            }
        }

        return (goldId, goldName);
    }

    private static (string? id, string? name) FindFirstSymbol(string pipe)
    {
        foreach (var p in pipe.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            if (p.StartsWith("55=", StringComparison.Ordinal) && p.Length > 3)
                return (p[3..], "first-listed");
        }

        return (null, null);
    }

    private static string? TypeOf(string pipe) => Tag(pipe, "35");

    private static string? Tag(string pipe, string tag)
    {
        foreach (var part in pipe.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i <= 0)
                continue;
            if (part[..i] == tag)
                return part[(i + 1)..];
        }

        return null;
    }

    private static string Sanitize(string pipe)
    {
        var parts = pipe.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith("554=", StringComparison.Ordinal))
            .Select(p => p.Length > 80 ? p[..80] + "…" : p);
        var joined = string.Join('|', parts);
        return joined.Length > 1500 ? joined[..1500] : joined;
    }
}
