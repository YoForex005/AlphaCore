# W500_SLICE_34 — CTrader FIX logon vs Achiever HTTP proxy / 1012

- **slot:** 34
- **file:** `D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`
- **angle:** Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012
- **read:** full file (96 lines) via `read_file`; grep on `D:/Prop/src/Fix.CTrader` for `Proxy|proxy|1012|IPBLOCK|Achiever` returned **no matches**
- **verdict:** PASS

## Evidence quotes

`CTraderFixLogonHostedService` is a one-shot `BackgroundService` that attempts **cTrader FIX** QUOTE/TRADE logon over TLS TCP. It is not an MT5 Manager client. It does not construct `NativeMt5BrokerConnector`, does not call `ProxySet`, and never sees MetaQuotes retcodes.

Password gate + venue are cTrader-only (`CTRADER_FIX_*`, default host `live-us-eqx-01.p.c-trader.com`, ports 5211/5212):

```29:51:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }

        var host = _config["CTRADER_FIX_HOST"] ?? "live-us-eqx-01.p.c-trader.com";
        var account = _config["CTRADER_FIX_ACCOUNT_ID"] ?? "1369850";
        var sender = _config["CTRADER_FIX_QUOTE_SENDER_COMP_ID"] ?? "live.pepperstone.1369850";
        var target = _config["CTRADER_FIX_QUOTE_TARGET_COMP_ID"] ?? "cServer";

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
```

The only order-related comment in this type is that **NewOrderSingle stays off**. Persist is session-row status only:

```53:54:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

```85:91:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
            row.Host = host;
            row.Port = result.Qualifier == FixSessionQualifier.Quote ? 5211 : 5212;
            row.Status = result.LoggedOn ? FixSessionStatus.LoggedOn : FixSessionStatus.Error;
            row.LastError = result.LastError;
            row.LastInboundAt = DateTimeOffset.UtcNow;
            row.LastOutboundAt = DateTimeOffset.UtcNow;
            row.UpdatedAt = DateTimeOffset.UtcNow;
```

Callee `CTraderFixSession.TryLogonAsync` is a direct `TcpClient` + `SslStream` Logon (35=A). No HTTP CONNECT, no `ACHIEVER_PROXY_*`, no Manager pump:

```35:44:D:/Prop/src/Fix.CTrader/Sessions/CTraderFixSession.cs
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

This file does not contain:

- `ProxyEnabled` / `ProxySet` / `MTProxyInfo` / `PROXY_HTTP`
- `ACHIEVER_PROXY_HOST` / `ACHIEVER_PROXY_ENABLED`
- `1012` / `MT_RET_AUTH_MANAGER_IPBLOCK` / `MTRetCode`
- `Achiever` / `BrokerCodes.Achiever` / Manager `Connect`

The Achiever whitelist-proxy / 1012 path lives **outside** this slice, on the MT5 Manager connector:

```115:127:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void ApplyProxy()
    {
        if (_manager is null || !_opt.ProxyEnabled || string.IsNullOrWhiteSpace(_opt.ProxyHost))
            return;

        var proxy = new MTProxyInfo
        {
            enable = 1,
            type = MTProxyInfo.Type.PROXY_HTTP,
            address = $"{_opt.ProxyHost}:{_opt.ProxyPort}",
            auth = string.IsNullOrEmpty(_opt.ProxyUser) ? "" : $"{_opt.ProxyUser}:{_opt.ProxyPassword}"
        };
        var set = _manager.ProxySet(proxy);
```

```444:452:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            5 => "disk/no-connect in some builds — server unreachable",
            10 => "no connection",
            9 => "timeout",
            _ => code.ToString()
        };
```

Vendor name for 1012 is Manager-only (`MT5APIConstants.h`: `MT_RET_AUTH_MANAGER_IPBLOCK = 1012` — “IP address unallowed for manager”). cTrader FIX has no such retcode.

Achiever proxy flags are bound in `LiveMt5Registration.CreateConnectors` (`ACHIEVER_PROXY_ENABLED` / host / port / user), not in this hosted service.

## No-loss implication

This type cannot present a non-allowlisted egress IP to Achiever Manager, cannot omit `ProxySet`, and cannot produce `MT_RET_AUTH_MANAGER_IPBLOCK` 1012. Those outcomes require `NativeMt5BrokerConnector.Connect` against Achiever. A failed or skipped cTrader FIX logon here only writes `FixSessionState` (or returns early when the FIX password is missing). It does not send `NewOrderSingle`, does not size or copy, and does not leave the Achiever book in a half-connected Manager session. Capital-loss risk from a missing Achiever HTTP proxy (direct desktop egress → 1012, no live deals/positions) is owned by the MT5 connector/registration path, not by `CTraderFixLogonHostedService`.

Empty-PASS justification: the assigned file was fully read (96/96 lines); the angle (Achiever HTTP proxy / 1012 IP-block) is absent by construction (different venue, protocol, and config keys), not by skipped review.
