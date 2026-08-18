using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using TraderIntelligence.Domain.Enums;

namespace TraderIntelligence.Fix.CTrader.Sessions;

public sealed class CTraderFixSessionResult
{
    public required FixSessionQualifier Qualifier { get; init; }
    public required bool LoggedOn { get; init; }
    public required string Status { get; init; }
    public string? LastError { get; init; }
    public string? RawLogonType { get; init; }
}

public static class CTraderFixSession
{
    public static async Task<CTraderFixSessionResult> TryLogonAsync(
        FixSessionQualifier qualifier,
        string host,
        int sslPort,
        string senderCompId,
        string targetCompId,
        string senderSubId,
        string targetSubId,
        string username,
        string password,
        CancellationToken ct)
    {
        try
        {
            using var tcp = new TcpClient();
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
            await using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            await ssl.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
            {
                TargetHost = host,
                EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13
            }, timeoutCts.Token);

            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);

            var buffer = new byte[4096];
            var read = await ssl.ReadAsync(buffer, timeoutCts.Token);
            var reply = Encoding.ASCII.GetString(buffer, 0, Math.Max(0, read)).Replace('\u0001', '|');
            var msgType = Extract(reply, "35");
            if (msgType == "A")
            {
                return new CTraderFixSessionResult
                {
                    Qualifier = qualifier,
                    LoggedOn = true,
                    Status = "LoggedOn",
                    RawLogonType = msgType
                };
            }

            var text = Extract(reply, "58");
            return new CTraderFixSessionResult
            {
                Qualifier = qualifier,
                LoggedOn = false,
                Status = "Error",
                LastError = $"Logon rejected 35={msgType} {text}".Trim(),
                RawLogonType = msgType
            };
        }
        catch (Exception ex)
        {
            return new CTraderFixSessionResult
            {
                Qualifier = qualifier,
                LoggedOn = false,
                Status = "Disconnected",
                LastError = ex.GetType().Name + ": " + ex.Message
            };
        }
    }

    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
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

    private static string? Extract(string pipeMessage, string tag)
    {
        foreach (var part in pipeMessage.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i <= 0)
                continue;
            if (part[..i] == tag)
                return part[(i + 1)..];
        }

        return null;
    }
}
