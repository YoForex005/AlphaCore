# W500_SLICE_84

- **slot:** 84
- **file:** `D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`
- **angle:** Achiever HTTP proxy / `MT_RET_AUTH_MANAGER_IPBLOCK` 1012
- **read:** full file (110/110 lines) via `read_file`; grep `MT_RET_AUTH_MANAGER_IPBLOCK|1012|Achiever|HTTP proxy|HttpProxy|ip.?block|ProxyEnabled|ProxyHost` on this file and `D:\Prop\src\Fix.CTrader` (zero hits except `RealCopyEnabled` / `NewOrderSingle still disabled`); cross-checked the real 1012/proxy path in `NativeMt5BrokerConnector.Describe` / `ApplyProxy` and `LiveMt5Registration` (`ACHIEVER_PROXY_*`)
- **verdict:** PASS
- **empty PASS:** yes — defect class does not apply to this file (file was actually read)

## Binding law (this angle)

`MT_RET_AUTH_MANAGER_IPBLOCK = 1012` is a **MetaQuotes Manager API** authentication retcode: the Achiever MT5 server refused the manager TCP source IP. Recovery on a workstation that is not the allow-listed egress is `MTProxyInfo` + `ProxySet(PROXY_HTTP)` before `Connect`, wired as `ACHIEVER_PROXY_*` on `NativeMt5BrokerConnector`.

This slice asks whether **this file** can omit that hop, mis-handle 1012, or otherwise open a live capital path while Achiever is IP-blocked.

## Evidence quotes

`CTraderFixLogonHostedService` is a one-shot `BackgroundService` for **cTrader FIX 4.4 logon probes** (Pepperstone / `cServer`), not Achiever Manager. The only credentials it reads are `CTRADER_FIX_*`. There is no `ACHIEVER_PROXY_*`, no `MT5_*` server/login, no `CIMTManagerAPI`, no `ProxySet`.

Password gate is fail-closed for FIX only (missing/placeholder secret → skip logon). It does not mention Achiever or 1012:

```33:38:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        var password = _config["CTRADER_FIX_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password) || password.Contains("<SECRET>", StringComparison.Ordinal))
        {
            _log.LogWarning("cTrader FIX password missing. QUOTE/TRADE logon skipped.");
            return;
        }
```

Host/account/sender are the **cTrader** live US EQX gateway, not `AchieverGlobalMarkets-Server` / `57.128.141.65:443`:

```40:55:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
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

After the two `35=A` probes it **forces** destination copy off and logs that `NewOrderSingle` is still disabled. Persist is session-status rows only (`FixSessionState` host/port/status/error timestamps):

```57:68:D:/Prop/src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs
        _runtime.Quote.LoggedOn = quote.LoggedOn;
        _runtime.Quote.Status = quote.Status;
        _runtime.Quote.LastError = quote.LastError;
        _runtime.Quote.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.Trade.LoggedOn = trade.LoggedOn;
        _runtime.Trade.Status = trade.Status;
        _runtime.Trade.LastError = trade.LastError;
        _runtime.Trade.UpdatedAt = DateTimeOffset.UtcNow;
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

Callee `CTraderFixSession.TryLogonAsync` is TCP+TLS → one Logon → one 4 KiB read. Direct `TcpClient.ConnectAsync(host, sslPort)` — no HTTP CONNECT, no `MTProxyInfo`. Outbound FIX body is `35=A` only (no `D` / NewOrderSingle).

This assigned file does **not** contain:

- `Achiever` / `BrokerCodes.Achiever` / `AchieverGlobalMarkets`
- `ACHIEVER_PROXY_ENABLED` / `ACHIEVER_PROXY_HOST` / `ProxyEnabled` / `ProxyHost` / `ProxySet` / `PROXY_HTTP`
- `MT_RET_AUTH_MANAGER_IPBLOCK` / `1012` / `IPBLOCK` / `MTRetCode`
- `CIMTManagerAPI` / `Connect(endpoint, login, password, …)`
- any order send (`NewOrderSingle`, `35=D`, dealer balance)

### Where the angle actually lives (out of this slice; cited so empty-PASS is not a miss)

Achiever HTTP proxy is applied only on the native Manager connector, then 1012 is mapped as a connect failure string:

```115:129:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
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
        if (set != MTRetCode.MT_RET_OK)
            throw new InvalidOperationException(Describe(set, $"{BrokerCode} ProxySet"));
    }
```

```443:454:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            // ...
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }
```

Live wiring for that hop is `LiveMt5Registration` (`ACHIEVER_PROXY_*` on Achiever only; Starwave `ProxyEnabled = false`). Seed catalog names the allow-list hop `81.29.145.69` on the Achiever broker row. **None** of those symbols are imported or read by `CTraderFixLogonHostedService`.

## No-loss implication

1012 cannot be produced or swallowed by this host. It never opens an Achiever Manager socket, so omitting HTTP proxy **here** cannot change the source IP Achiever sees and cannot authenticate a blocked manager.

Even on a successful Pepperstone FIX logon:

- this type does not send size (`NewOrderSingle still disabled`)
- it overwrites `_runtime.RealCopyEnabled = false` after the probes (destination copy flag is forced off for the process runtime object this service owns)
- persist updates `FixSessionState` status/error only; no deal/order/position rows

Worst case inside this file: FIX logon skipped or `Disconnected`/`Error` status persisted. That is session telemetry, not a fill. Achiever books stay untouched because this is the wrong protocol, wrong host, and wrong API for Manager IP-block.

**Residual (out of slice):** if `ACHIEVER_PROXY_*` is off on a non-whitelisted desktop, `NativeMt5BrokerConnector.Connect` is expected to throw 1012. That failure is fail-closed on the Manager collector, not on this FIX probe.

Empty-PASS justification: the assigned file was fully read (110 lines). The Achiever HTTP-proxy / 1012 defect class is absent by construction (cTrader FIX logon-only host), not by skipped review.
