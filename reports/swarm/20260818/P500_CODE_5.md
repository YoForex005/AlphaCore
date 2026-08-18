# P500_CODE_5 — Gold scalp holds under 3 minutes vs FIX latency + spread

| Field | Value |
|---|---|
| Slot | **5** |
| File | `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` |
| Angle | Would gold scalp holds under 3 minutes survive FIX latency plus spread? |
| Verdict | **FAIL — NO.** File was read in full (135 lines). This unit is a one-shot TLS Logon (`35=A`) that connects, writes one message, reads once, and disposes the socket. It does not keep a FIX session alive for a 180 s hold, does not subscribe to XAUUSD bid/ask, does not measure spread, and does not send `NewOrderSingle`. A sub-3-minute gold scalp cannot survive a path that cannot stay logged on, cannot price the book, and cannot send entry *or* exit. |
| Date | 2026-08-18 |
| Product source modified | **No.** Report only. |
| Method | Full `read_file` of `CTraderFixSession.cs`. Grep of `src/Fix.CTrader` for latency / spread / gold / scalp / LoggedOn / REAL_COPY / NewOrderSingle. Adjacent read of `CTraderFixOptions`, `CTraderQuoteService`, `CTraderFixLogonHostedService`, `FixWorker`, `RiskEngine`, `ShadowCopyEngine`, `docs/risk.md`, `docs/ctrader-fix.md`. |

**Honesty:** This is **not** an empty PASS. PASS would require measured dest fills showing sub-3-minute XAU holds remain net-positive after destination spread + source-to-fill latency. That evidence does not exist. `SAFE_BY_ABSENCE` of `35=D` protects Pepperstone capital today; it does **not** prove scalps would survive if copy were armed.

Measured live context (given, not re-measured here): 8463 accounts; Achiever scoring; Starwave deals-done scored 0; SHADOW all demo; `destinationRealPnl` 0; FIX LoggedOn; `REAL_COPY` false.

Last on-disk census (`reports/CREDENTIALS_AND_COPY_STATUS.md`): Achiever 8/6512 + Starwave 10/1948 = **18 / 8460**. Given 8463 is accepted as the slot brief; do not treat the +3 as proven extra books.

---

## Angle

Would XAUUSD / gold scalp holds **under 3 minutes** (hold < 180 s) survive the **FIX latency** implied by this session plus **destination spread** if copied to Pepperstone / cTrader?

---

## Verdict

**FAIL — they would not survive on this path.**

Reachable work in this compilation unit:

1. Open a TCP client to `host:sslPort`.
2. Wrap TLS 1.2/1.3 with **certificate validation disabled**.
3. Build and write a FIX 4.4 **Logon** (`35=A`) with `HeartBtInt=30`, `ResetSeqNumFlag=Y`, seq always `1`.
4. Read **one** 4096-byte chunk; treat `35=A` as `LoggedOn=true`.
5. Dispose the socket (no heartbeat loop, no MarketDataRequest, no NewOrderSingle, no logout).

A gold scalp under 3 minutes needs, at minimum: a live dest quote, a dest entry, a dest exit, and a remaining hold after copy delay that still covers **2× spread + slippage**. This file provides none of those. Adjacent product law makes the economics worse, not better.

---

## Evidence quotes

### 1. One-shot logon; 20 s budget already eats the scalp clock

The entire session is bounded by a **20-second** linked cancel. That is **11% of a 180 s hold** *before* any order exists, and it covers connect + TLS + write + a single read — not a measured RTT histogram.

```33:54:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

`using var tcp` / `await using var ssl` means the TCP+TLS session **dies when `TryLogonAsync` returns**. There is no long-lived initiator. A 3-minute scalp that needed dest entry at t=0 and dest exit at t<180 s would pay **a new connect+TLS+logon (up to 20 s each way)** if anyone reused this helper as a send path. That is fatal vs a sub-3-minute hold.

### 2. Only outbound MsgType is Logon `35=A`; heartbeat advertised, never run

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Grep of this file: `NewOrderSingle` = 0; `35=D` = 0; `MsgType` = 0. The only outbound type is `(35, "A")`. Tag `108=30` promises a 30 s heartbeat the class **never implements**. Tag `141=Y` plus `seq = 1` resets sequence numbers on every call — a reconnect mid-scalp cannot resume the same session. No `35=0` / `35=1` loop exists, so the session cannot stay alive for 180 s even if the socket were kept.

### 3. LoggedOn is a one-byte MsgType check, then the process pins REAL_COPY false

```55:65:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

Caller immediately **forces** copy off and logs that orders stay disabled:

```60:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
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

FIX **LoggedOn** in the slot brief is this MsgType-A probe, not a live quote tape and not a trade socket that can flatten a 3-minute gold position.

### 4. Worker then stamps both sessions Disconnected every 15 s

```28:42:D:\Prop\apps\fix-worker\Worker.cs
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            if (quote is not null)
            {
                quote.UpdatedAt = DateTimeOffset.UtcNow;
                quote.Status = FixSessionStatus.Disconnected;
                quote.LastError = "No live QUOTE socket. Simulator/demo only.";
            }

            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            if (trade is not null)
            {
                trade.UpdatedAt = DateTimeOffset.UtcNow;
                trade.Status = FixSessionStatus.Disconnected;
                trade.LastError = "No live TRADE socket. NewOrderSingle remains off.";
            }
```

A 180 s gold scalp is **12 worker cycles** of “no live socket.” There is no dest quote age to compare to a 3-minute hold.

### 5. This file never sees XAUUSD spread; quote-age defaults already disagree

`CTraderFixSession` has **zero** bid/ask/spread fields. The quote service (not called from this session) rejects quotes older than **5000 ms**:

```94:99:D:\Prop\src\Fix.CTrader\Services\CTraderQuoteService.cs
        var ageMs = (DateTimeOffset.UtcNow - ts.ToUniversalTime()).TotalMilliseconds;
        if (ageMs < 0) ageMs = 0;
        if (ageMs > _options.MaxQuoteAgeMs)
        {
            rejectReason = $"Quote is stale. AgeMs={ageMs:0}. ThresholdMs={_options.MaxQuoteAgeMs}.";
            return false;
```

```37:39:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    public int HeartbeatIntervalSec { get; set; } = 30;

    public int MaxQuoteAgeMs { get; set; } = 5000;
```

Risk defaults are **tighter on age, looser on spread**:

```14:18:D:\Prop\src\Domain\Risk\RiskEngine.cs
    public decimal MaxAllowedSpread { get; init; } = 2.0m;
    public TimeSpan MaxQuoteAge { get; init; } = TimeSpan.FromSeconds(3);
    public TimeSpan MaxSourceSignalAge { get; init; } = TimeSpan.FromSeconds(15);
    public decimal MaxPriceMove { get; init; } = 3.0m;
    public decimal MaxSlippage { get; init; } = 1.5m;
```

Settings API publishes the 3 s / 15 s pair (`apps/api/Program.cs`). Split-brain: FIX options allow a **5 s** quote; risk / settings claim **3 s**. `Evaluate` is not called from this session. Even if it were:

- `MaxAllowedSpread = 2.0` on XAUUSD is **$2.00 / oz**. A sub-3-minute gold scalp that targets $0.50–$2.00 is fully consumed by a *legal* dest spread before latency.
- `MaxSourceSignalAge = 15 s` is **8.3% of a 180 s hold**. Any OPEN older than 15 s is `SIGNAL_STALE`. The remaining hold after a 15 s copy is ≤165 s and has already paid dest ask (long) or bid (short).
- `MaxPriceMove = 3.0` ($3 mid vs source) is an entire typical scalp target; the guard would either reject the copy or allow a move that *is* the edge.

### 6. Shadow delay model is a 5-cent toy, not a gold-scalp proof

```33:48:D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs
    public const decimal DefaultLatencySlippagePoints = 0.05m;

    public ShadowFill SimulateEntry(
        string shadowOrderId,
        TradeDirection direction,
        decimal quantity,
        decimal sourcePrice,
        DestinationQuote quote,
        DateTimeOffset now,
        TimeSpan modeledDelay)
    {
        var useAsk = direction == TradeDirection.Long;
        var raw = useAsk ? quote.Ask : quote.Bid;
        var adverse = direction == TradeDirection.Long ? DefaultLatencySlippagePoints : -DefaultLatencySlippagePoints;
        if (modeledDelay > TimeSpan.FromMilliseconds(250))
            raw += adverse;
```

0.05 XAU “points” ≈ **$0.05**. Real dest XAU spread is typically tens of cents and spikes over $1 around news. Round-trip (pay ask in, hit bid out) is **2× spread**, not 5 cents one way. This model cannot certify that a <3-minute gold hold survives dest spread + FIX delay.

### 7. Product copy-timing law already says stale XAU copy destroys edge

`docs/risk.md`:

> Minimum delay: 100ms — prevents front-running detection by brokers  
> Maximum delay: 2000ms — stale trades are rejected, not copied  
> Delay is measured from MT5 deal timestamp to cTrader NewOrderSingle send time  
> Slippage Tolerance: 30 points

`docs/ctrader-fix.md`:

> Flag slippage exceeding threshold (default: 30 points)

On XAUUSD, 30 points is **$0.30** if 1 point = $0.01 — already a large fraction of a 3-minute scalp. Architecture §36 (quoted in `A73_copy_latency.md`): “For XAUUSD, stale trade copying can destroy expected edge.” Architecture §63 (normative): if FIX is down **3 minutes** while sources open 20 trades, reconnect must **not** fire those entries — they expire. So a FIX gap equal to the hold itself **kills** the scalp by product law.

This session class implements **none** of those clocks. It stamps tag 52 once on logon and forgets the socket.

### 8. Live measured context does not contain dest-survival numbers

| Brief / on-disk fact | Implication for this angle |
|---|---|
| 8463 accounts given (catalog last written 8460) | Observation set is large; **no dest fills** among them. |
| Achiever scoring | Source-side scores, not dest PnL. |
| Starwave deals-done scored **0** | No completed Starwave XAU book to measure hold-time vs copy delay. `AverageHoldSeconds` exists on `FeatureSnapshot` but this session never reads it. |
| SHADOW all demo | Dest “fills” are simulated or absent. |
| `destinationRealPnl` **0** | No measured dest profit or dest loss on gold. |
| FIX LoggedOn | Logon `35=A` only (this file). |
| `REAL_COPY` **false** | `RealCopyExecutionEnabled` defaults false; hosted service pins `RealCopyEnabled = false`. No live `35=D`. |

Without dest entry/exit timestamps and dest bid/ask at both, **survival is unproven**. Combined with the one-shot 20 s logon and the $2 allowed spread, the engineering prediction is **they would not survive**.

### 9. Latency + spread budget vs a 180 s gold scalp (honest arithmetic, not a live tick study)

This is a **budget**, not a claimed live measurement. There is no hop histogram in this tree (`A73` classified §36 **MISSING**).

| Hop / cost | Bound visible in this tree | Fraction of 180 s hold |
|---|---|---|
| TCP + TLS + Logon (this file) | up to **20 s** per call | 11% |
| Heartbeat interval advertised | **30 s**, never looped | session dies before hold ends |
| Worker “no live socket” stamp | every **15 s** | 12 stamps per hold |
| Quote age allow (options vs risk) | **5 s** vs **3 s** | 1.7–2.8% |
| Source-signal age allow | **15 s** then `SIGNAL_STALE` | 8.3% |
| Copy delay policy | 100–2000 ms | 0.06–1.1% |
| Shadow extra adverse | $0.05 after 250 ms | toy |
| Allowed dest spread | **$2.00** | can equal 100% of a $2 scalp |
| Allowed dest mid-move | **$3.00** | can exceed the scalp |
| Allowed slippage | **$1.50** | 50–150% of a typical scalp target |
| Round-trip dest spread | 2 × (ask−bid), **unmeasured here** | typically tens of cents to >$1 each way on XAU |

A long scalp that is already 60–120 s old when MT5 ingest notices it has **60–120 s left**. Add 15 s signal-age ceiling or a 20 s re-logon and the dest position, if it ever opened, is a late chase into a book this session cannot even read.

---

## Profit implication

**No destination profit from this file.** It cannot open, hedge, or close XAUUSD on Pepperstone. `LoggedOn=true` is not a quote and not a fill. With `REAL_COPY` false, SHADOW all-demo, `destinationRealPnl` 0, and Starwave deals-done scored 0, there is **zero** measured dest edge on gold scalps.

If someone later armed copy on top of this helper:

- Sub-3-minute gold scalps would pay dest spread **twice** (entry + exit) plus any re-logon delay up to 20 s.
- A $2 `MaxAllowedSpread` would admit books that wipe a 3-minute target.
- There is no persist-before-send, no ClOrdID, no ExecutionReport loop — even a lucky fill would be unreconciled.

Do **not** treat Achiever scoring of 8463 accounts as dest profit. Source-side scalp winners are the first books dest latency + spread would invert.

---

## Lower-loss implication

**Today: capital is not at risk from this file** (`SAFE_BY_ABSENCE` of `35=D`; hosted service pins `RealCopyEnabled = false`). That is **loss avoidance by not copying**, not proof that dest hedges would reduce source loss.

**If this session were used as a live send path, lower-loss would FAIL:**

- One-shot dispose + 20 s timeout + `141=Y` + seq=1 cannot flatten a 3-minute gold position reliably.
- Worker marks TRADE `Disconnected` / “NewOrderSingle remains off” every 15 s.
- No dest quote means no spread-aware refuse; the $2 spread cap (elsewhere, unused here) is too wide to protect a scalp.
- TLS callback `(_, _, _, _) => true` accepts any certificate — session integrity, not latency, but a hijacked “logon” is not a hedge.
- Architecture §63: a 3-minute FIX outage must expire the 20 source opens, not catch them up. Those scalps **die unhedged**. That is lower *dest* loss and **higher leftover source exposure**.

The only honest loss-reduction for sub-3-minute gold on this stack: keep `REAL_COPY` false; do not copy holds whose remaining life is ≤ 2× measured dest spread + measured source-to-fill. That measurement is **not** in `CTraderFixSession.cs`.

---

## Binding one-liner

`CTraderFixSession.cs` is a 20 s one-shot `35=A` probe. No quote, no spread, no `35=D`, socket disposed. Gold scalps under 3 minutes would **not** survive FIX latency plus dest spread on this path. Pepperstone capital is safe only because copy is off, not because the economics work.
