# W500_SLICE_33

- **slot:** 33
- **file:** `D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (135 lines) via `read_file`; grep on this file for `NewOrderSingle|capital|loss|Order|Cancel|Modify|Send|Reject|Protect` hit only `CancellationToken` / `timeoutCts` (lines 31, 36–37). Grep of `D:/Prop/src/Fix.CTrader` for `NewOrderSingle|35.?=.?D` found **zero** hits in this session type; the only sibling mentions are the default-off flag comment in `Configuration/CTraderFixOptions.cs` and the hosted-service log line “NewOrderSingle still disabled”.
- **verdict:** PASS

## Evidence quotes

`CTraderFixSession` is a one-shot FIX 4.4 **Logon probe**. Public surface is `TryLogonAsync` → TCP + TLS → `BuildLogon` (`35=A`) → one `WriteAsync` → one 4 KiB `ReadAsync` → classify reply as LoggedOn / Error / Disconnected. Sockets are `using` / `await using` and disposed immediately after that single read. There is no keep-alive TRADE initiator, no heartbeat loop, no execution report pump, and no second write.

```19:50:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

Reply handling accepts only Logon (`35=A`) or records a reject / disconnect. No ExecutionReport, no fill apply, no qty/price parse:

```52:75:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

The only outbound FIX body is Logon. MsgType is hardcoded `"A"`. Session tags only: seq `34`, CompIDs `49`/`56`, SubIDs `50`/`57`, sending time `52`, EncryptMethod `98=0`, HeartBtInt `108=30`, ResetSeqNumFlag `141=Y`, username `553`, password `554`. No ClOrdID (`11`), Side (`54`), OrderQty (`38`), Symbol (`55`), OrdType (`40`), Price (`44`), TimeInForce (`59`), or any cancel/replace tags:

```89:109:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

`Assemble` is `private static` and is only called from `BuildLogon`. Result DTO carries qualifier / logged-on / status / last error / raw type — no order identity:

```10:17:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
public sealed class CTraderFixSessionResult
{
    public required FixSessionQualifier Qualifier { get; init; }
    public required bool LoggedOn { get; init; }
    public required string Status { get; init; }
    public string? LastError { get; init; }
    public string? RawLogonType { get; init; }
}
```

This file does not contain:

- `NewOrderSingle` / FIX tag `35=D` / `35=F` (OrderCancelRequest) / `35=G` (OrderCancelReplace) / `35=H` (OrderStatusRequest)
- `ClOrdID`, `OrderQty`, `Side`, `Symbol`, `OrdType`, `Price`, `StopPx`, SL/TP
- `RealCopyExecutionEnabled` / any send gate / persist-before-send
- QuickFIX initiator, data dictionary, or a second `WriteAsync` after Logon
- `OrderSend` / `DealerSend` / position flatten / qty mutation

The only product caller is `CTraderFixLogonHostedService` (out of this slice’s file). It probes QUOTE 5211 and TRADE 5212 then logs that orders stay off; it does not call any send API because none exists here:

```41:54:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            sender, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            sender, password, stoppingToken);

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

Live-send gate and SAFE_BY_ABSENCE pin live elsewhere (not this file): `CTraderFixOptions.RealCopyExecutionEnabled = false` (“When true, allow placing new orders (NewOrderSingle). Default OFF.”) and `E002_no_live_send.md` (`no function that emits FIX MsgType=D to a socket`).

Residual non-order notes (do not flip this slice to FAIL): accept-all TLS callback `(_, _, _, _) => true` is a credential-MITM hygiene defect, not an order path; `141=Y` plus a caller-chosen TRADE logon can disrupt a single-active TRADE session, but this type still emits only `35=A` and then disconnects — it does not place, size, replace, or flatten.

## No-loss implication

`TryLogonAsync` cannot emit `NewOrderSingle`, cannot attach qty/side/symbol/price, and cannot reduce account equity by filling or flattening. Worst case of **this file** is a 20-second FIX `35=A` probe (success, reject, or disconnect) whose sockets are disposed before any later message. Slot 33 therefore has **no live cTrader NewOrderSingle path** and **no capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (all 135 lines); the angle (live cTrader NewOrderSingle / capital-loss) is absent by construction, not by skipped review.
