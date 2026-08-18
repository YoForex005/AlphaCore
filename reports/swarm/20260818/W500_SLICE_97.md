# W500_SLICE_97

- **slot:** 97
- **file:** `D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (80/80 lines) via `read_file`; grep `200` in `Fix.CTrader` (0 hits); grep `position` in `Fix.CTrader` (0 hits); grep `Take(200)` / `MaxAccount` / `first 200` under `D:/Prop/src` (no `Take(200)` remaining in `src/`); `CTraderFixOptions` references only in this file and `CTraderQuoteService`
- **verdict:** PASS

## Evidence quotes

`CTraderFixOptions.cs` is a FIX session options POCO. It binds one gateway host, one `AccountId` string, nested QUOTE/TRADE session IDs/ports, SSL/heartbeat/quote-age flags, and a default-OFF new-order gate. It does not enumerate broker logins, does not fetch positions, and does not apply `Take(200)` or any other account-rank cap.

```5:39:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
public sealed class CTraderFixOptions
{
    /// <summary>
    /// FIX gateway host (cTrader).
    /// </summary>
    public string Host { get; set; } = "live-us-eqx-01.p.c-trader.com";

    /// <summary>
    /// FIX username (AccountId). Must never be logged.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// FIX password. Must never be logged.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    public QuoteFixOptions Quote { get; set; } = new();

    public TradeFixOptions Trade { get; set; } = new();

    public bool UseSsl { get; set; } = true;

    public bool QuoteEnabled { get; set; } = true;

    public bool TradeSessionEnabled { get; set; } = true;

    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;

    public int HeartbeatIntervalSec { get; set; } = 30;

    public int MaxQuoteAgeMs { get; set; } = 5000;
```

Nested session options are ports + CompIDs/SubIDs only. No account list, no page size, no position snapshot policy:

```41:78:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;

        public int PlainPort { get; set; } = 5201;

        public string SenderCompId { get; set; } = "live.pepperstone.1369850";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "QUOTE";

        /// <summary>
        /// SenderSubID for QUOTE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }

    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;

        public int PlainPort { get; set; } = 5202;

        /// <summary>
        /// cTrader FIX gateway SenderCompID (configurable).
        /// </summary>
        public string SenderCompId { get; set; } = "live.pepperstone.1369850";

        public string TargetCompId { get; set; } = "cServer";

        public string TargetSubId { get; set; } = "TRADE";

        /// <summary>
        /// SenderSubID for TRADE session (configurable).
        /// </summary>
        public string SenderSubId { get; set; } = string.Empty;
    }
```

This file does not contain:

- the literal `200` (constant, comment, default, or port)
- `Take(200)` / `Take(` / `Skip(` / `MaxAccounts` / cursor / page
- `position` / `GetPositionsAsync` / `ReplacePositionsAsync`
- a collection of accounts (only singular `AccountId`)
- any loop that could truncate a position book to the first N logins

The only `AccountId` member is one FIX username string, default empty. That is a single-session identity, not a 200-login slice.

Sole consumer of this type in `src/` is quote-age gating, not position ingest:

```27:31:D:/Prop/src/Fix.CTrader/Services/CTraderQuoteService.cs
    public CTraderQuoteService(CTraderFixOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        if (_options.Quote is null) throw new ArgumentException("Quote options must be provided.", nameof(options));
    }
```

```94:99:D:/Prop/src/Fix.CTrader/Services/CTraderQuoteService.cs
        var ageMs = (DateTimeOffset.UtcNow - ts.ToUniversalTime()).TotalMilliseconds;
        if (ageMs < 0) ageMs = 0;
        if (ageMs > _options.MaxQuoteAgeMs)
        {
            rejectReason = $"Quote is stale. AgeMs={ageMs:0}. ThresholdMs={_options.MaxQuoteAgeMs}.";
```

`Fix.CTrader` grep for `position` returned **no matches**. The only `Take(` in that project is checksum framing in `FixMessageParser` (`parts.Take(parts.Length - 1)`), unrelated to accounts.

Workspace check (angle owner, not this file): `D:/Prop/src` currently has **no** `Take(200)` under `src/`. `DealIngestionService.SyncBrokerAsync` now walks **all** accounts when not using `IMt5BulkPositionReader`:

```81:93:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkPositionReader posBulk)
        {
            var positions = await posBulk.GetGroupPositionsAsync("*", ct);
            await _store.ReplaceBrokerPositionsAsync(brokerId, positions, ct);
        }
        else
        {
            foreach (var account in accounts)
            {
                var positions = await connector.GetPositionsAsync(account.Login, ct);
                await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
            }
        }
```

A leftover `Take(200)` exists only on `GET /api/trades` reconstructed-trade rows (`D:/Prop/apps/api/Program.cs`), not on positions and not on this options type. Historical swarm notes that cited `accounts.Take(200)` in ingestion are **stale vs current `src/`** and were never located in `CTraderFixOptions`.

Slot 97 asked whether **this** configuration type limits positions to the first 200 accounts. It does not impose, encode, or document that cap.

## No-loss implication

`CTraderFixOptions` cannot silently drop open positions for accounts 201+. It never lists logins and never snapshots a book. It configures one cTrader FIX identity plus QUOTE/TRADE session endpoints. `RealCopyExecutionEnabled` defaults **false**, so this type also cannot authorize NewOrderSingle unless an external binder flips that flag. Any first-200-accounts position cutoff is outside this slice (historical MT5 ingest `Take(200)`, now absent from `DealIngestionService`; remaining `Take(200)` is `/api/trades` history only). This options class therefore has **no first-200-accounts position cap** and **no capital-loss / hidden-book path of its own**.

Empty-PASS justification: the assigned file was fully read (80/80 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review. Secrets (`Password`) were not copied into this report.
