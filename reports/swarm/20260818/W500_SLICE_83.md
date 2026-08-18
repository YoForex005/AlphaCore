# W500_SLICE_83

- **slot:** 83
- **file:** `D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (135 lines) via `read_file`; grep on this file for `NewOrderSingle|capital|loss|Order|SendOrder|PlaceOrder` (zero matches); grep on `src/Fix.CTrader` for `CTraderFixSession|NewOrderSingle|35=D` (this type is logon-only; `35=D` is absent from the project)
- **verdict:** PASS

## Binding law (this angle)

Live cTrader TRADE traffic that can lose capital is FIX NewOrderSingle (`35=D`) or any other order-mutating send (cancel/replace, market-data-driven auto-send) that places, increases, or leaves unprotected live exposure.

This slice PASSes if `CTraderFixSession` cannot emit those messages and has no other capital-loss path (no qty/side/symbol send, no post-logon keep-alive that later writes `35=D`). Empty PASS is allowed only after a full read. Residual TLS/auth issues that do not place orders are not a FAIL on this angle.

## Evidence quotes

The public surface is one async method. It connects TLS, writes a single Logon, reads one reply, and returns. There is no second write and no order builder:

```21:50:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

The only outbound FIX type this class can assemble is Logon `35=A`. Body tags are session identity, heartbeat, reset, and credentials — not ClOrdID/Symbol/Side/OrderQty/OrdType:

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

After the first inbound frame, the method only classifies Logon vs reject vs exception. `using` disposes `TcpClient`/`SslStream` on every return path, so a successful TRADE logon does not leave a socket that later code in this file could use to send `35=D`:

```52:85:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
            // ... Error / Disconnected results; no further ssl.WriteAsync
```

`Assemble` prefixes `8=FIX.4.4` + body length + checksum. It does not inject order tags. `Extract` is inbound-only.

This file does **not** contain:

- `NewOrderSingle`, `35=D`, `OrdType`, `ClOrdID`, tag `11`/`38`/`40`/`54`/`55`/`59`
- `OrderCancelRequest` (`35=F`), `OrderCancelReplaceRequest` (`35=G`), `ExecutionReport` handling that would ack a live fill
- any qty, side, symbol, price, or SL/TP field
- a send loop, heartbeat writer, or session object that stays connected after logon
- a call site inside this type that reads `RealCopyExecutionEnabled` and then places

Caller context (not owned by this file, cited only to confirm the type is not a hidden order sender): `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212), then logs `NewOrderSingle still disabled` and sets `_runtime.RealCopyEnabled = false`. `CTraderFixOptions.RealCopyExecutionEnabled` defaults false and is unused by `CTraderFixSession`.

Residual (out of this angle): `RemoteCertificateValidationCallback` is `(_, _, _, _) => true` (accepts any cert). That is a MITM/credential risk on Logon `553`/`554`, not a NewOrderSingle / capital-loss send path.

## No-loss implication

No live cTrader order can originate from this session type. The only write is FIX Logon `35=A`; the TCP/TLS session is torn down after the first reply. Even a TRADE-port logon (`qualifier` is unused except on the result DTO) cannot leave working orders, increase size, or skip risk.

Capital cannot be lost through NewOrderSingle here because the message is not built, not written, and the socket is not retained. Copy/kill-switch/size decisions are not reached from this file.

PASS on slot 83: empty NewOrderSingle / capital-loss path after a full read of `CTraderFixSession.cs`.
