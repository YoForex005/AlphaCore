# W500_SLICE_8

- **slot:** 8
- **file:** `D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full file (136 lines) via `read_file`; grep on this file for `DealRequestByGroup|90.?day|chunk|FromTimestamp|ToTimestamp|deal.?request` hit only the 20s TLS logon `timeoutCts` (lines 36–53). Broader grep of `D:\Prop\src\Fix.CTrader` found **zero** `DealRequestByGroup` / 90-day window / deal-history chunking.
- **verdict:** PASS

## Evidence quotes

`CTraderFixSession` is a FIX 4.4 logon probe only. Public surface is `TryLogonAsync` → TCP+TLS → `BuildLogon` (`35=A`) → one 4 KiB read → classify reply as LoggedOn / Error / Disconnected. There is no Manager API, no group mask, no `from`/`to` unix window, and no deal array.

```19:45:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

The only timeout in this file is the 20-second connect/auth/write/read budget for that logon, not a 90-day history pull:

```36:37:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));
            await tcp.ConnectAsync(host, sslPort, timeoutCts.Token);
```

The only outbound FIX body is Logon. Heartbeat `108=30` and reset `141=Y` are session fields. No TradeCaptureReportRequest, no deal request, no time-range tags:

```94:108:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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
```

This file does not contain:

- `DealRequestByGroup` / `DealRequest` / `DealRequestPage` / `GetGroupDeals`
- `AddDays(-90)` / `FromDays(90)` / any lookback window
- chunking, paging, or checkpointed time slices
- MT5 Manager / `CIMTManagerAPI` / group mask
- deal apply / reconstruction / scoring

The 90-day unchunked `DealRequestByGroup` path exists **outside** this slice (out of scope, cited only so the empty-PASS is not a miss):

```32:41:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);
            // ...
                    await connector.ConnectAsync(stoppingToken);
                    var n = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

```49:53:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
```

```240:251:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var arr = _manager!.DealCreateArray();
            try
            {
                var res = _manager.DealRequestByGroup(group, from.ToUnixTimeSeconds(), to.ToUnixTimeSeconds(), arr);
                if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                    throw new InvalidOperationException($"{BrokerCode} DealRequestByGroup {group} failed: {res}");
```

Those types are MT5 Manager ingestion, not `CTraderFixSession`. Slot 8 cannot fail this angle by omission: the assigned type is not on that call graph.

## No-loss implication

`TryLogonAsync` cannot request 90 days of group deals, cannot stall/timeout that bulk pull, and cannot drop deal history that would hide losing trades or under-count risk. Worst case in this file is a failed or successful FIX `35=A` probe within 20 seconds (or a Disconnected exception). That does not size, send, or cancel orders, and it does not mutate ledger/PnL. Slot 8 therefore has **no DealRequestByGroup 90-day timeout / missing-chunk capital or reconstruction-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (DealRequestByGroup 90-day timeout without chunking) is absent by construction, not by skipped review.
