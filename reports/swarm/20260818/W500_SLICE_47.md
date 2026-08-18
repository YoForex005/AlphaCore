# W500_SLICE_47

- **slot:** 47
- **file:** `D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (80/80 lines) via `read_file`; grep on `D:/Prop/src/Fix.CTrader` for `200|account|position|limit` hit only `AccountId` (FIX username) in this file plus unrelated parser comments; workspace grep for `Take(200)` / `first 200` / `positions limited` found **no matches in this file**
- **verdict:** PASS

## Evidence quotes

`CTraderFixOptions` is a sealed options POCO for one cTrader FIX session pair (QUOTE + TRADE). It binds host, a single `AccountId`, password, ports, CompIDs, heartbeat, quote-age, SSL, and two feature flags. It does not enumerate MT5 manager logins, does not call `GetPositionsAsync` / `PositionRequest`, and does not apply `Take(200)` (or any other N) to an account list.

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

Nested session knobs are ports and FIX identity only (QUOTE 5211/5201, TRADE 5212/5202). No position census, no account batch size:

```41:78:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
    public sealed class QuoteFixOptions
    {
        public int SslPort { get; set; } = 5211;
        public int PlainPort { get; set; } = 5201;
        public string SenderCompId { get; set; } = "live.pepperstone.1369850";
        public string TargetCompId { get; set; } = "cServer";
        public string TargetSubId { get; set; } = "QUOTE";
        public string SenderSubId { get; set; } = string.Empty;
    }

    public sealed class TradeFixOptions
    {
        public int SslPort { get; set; } = 5212;
        public int PlainPort { get; set; } = 5202;
        public string SenderCompId { get; set; } = "live.pepperstone.1369850";
        public string TargetCompId { get; set; } = "cServer";
        public string TargetSubId { get; set; } = "TRADE";
        public string SenderSubId { get; set; } = string.Empty;
    }
```

Numeric literals in this type are **not** a 200-account cap: `HeartbeatIntervalSec = 30`, `MaxQuoteAgeMs = 5000`, SSL/plain ports `5211`/`5201`/`5212`/`5202`. There is no `int` for max accounts, max positions, page size, or snapshot batch.

This file does not contain:

- `Take(200)` / `Take(` / `.Skip(` / first-N account cutoff
- `GetPositionsAsync` / `ReplacePositionsAsync` / `PositionRequest` / `PositionRequestByGroup`
- `IMt5BrokerConnector` account lists / `Mt5Accounts` / `foreach (var account`
- any `IReadOnlyList` of logins, positions, or reconstructed trades
- a bindable `MaxAccounts` / `PositionSnapshotLimit` / `MaxPositionAccounts` property

The assigned angle **does** exist elsewhere, but **not in this file**. MT5 ingest snapshots positions only for the first 200 connector accounts (`DealIngestionService.SyncBrokerAsync` → `accounts.Take(200)`). A separate `Take(200)` on `GET /api/trades` limits reconstructed-trade rows, not position snapshots. Those paths are **out of this slice’s file**. Slot 47 does not own them and cannot apply them.

`AccountId` here is a single FIX username string (default empty), not a collection that could be truncated to 200. `RealCopyExecutionEnabled` defaults **OFF**, so this options type does not even enable `NewOrderSingle` unless a caller flips the flag.

## No-loss implication

`CTraderFixOptions` cannot silently drop open-position snapshots for accounts 201+. It never iterates accounts and never writes `Mt5Positions`. Worst case inside this type is a mis-bound host/port/CompID or an empty `AccountId`/`Password` (logon fails) plus `RealCopyExecutionEnabled = false` by default (no NewOrderSingle). That is a **session-config / fail-closed logon** path, not a truncated position census and not a live sizing/send decision.

The real first-200 position-refresh cap remains in `DealIngestionService` (and the `/api/trades` row cap in `apps/api/Program.cs`). Those can hide open risk on the dashboard/copy path for logins beyond 200; they are **not** introduced or configured by `CTraderFixOptions`. Slot 47 therefore has **no live capital-loss path** and **no first-200 position-limit path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (80/80 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
