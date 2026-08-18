# W500_SLICE_22

- **slot:** 22
- **file:** `D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (80 lines) via `read_file`; grep on `Fix.CTrader` for `UserRequest|UserList|AllLogins|GetUsers|UserInfo|ManagerLogin|trader.*login|login.*all` and on this file for `AccountId|UserLogins|IList|IReadOnlyList|IEnumerable`; grep of `Fix.CTrader` for `UserLogins|GroupTotal|GroupNext` returned no matches
- **verdict:** PASS

## Evidence quotes

`CTraderFixOptions` is a sealed options POCO for one cTrader FIX destination (QUOTE + TRADE). It holds host, one `AccountId`, one `Password` (defaults empty; must not be logged), nested port/CompID settings, SSL/heartbeat/quote-age knobs, and a default-off `RealCopyExecutionEnabled` flag. There is no method, loop, page, group filter, or collection that enumerates MT5 manager traders or logins.

Identity is a single string, not a login list:

```13:15:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
    /// FIX username (AccountId). Must never be logged.
    /// </summary>
    public string AccountId { get; set; } = string.Empty;
```

Session enablement is boolean flags, not a trader census:

```26:35:D:/Prop/src/Fix.CTrader/Configuration/CTraderFixOptions.cs
    public bool UseSsl { get; set; } = true;

    public bool QuoteEnabled { get; set; } = true;

    public bool TradeSessionEnabled { get; set; } = true;

    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Nested types only carry FIX session addressing (`SslPort`/`PlainPort`, `SenderCompId`, `TargetCompId`, `TargetSubId` QUOTE/TRADE, optional `SenderSubId`). No `UserLogins`, `GroupTotal`, `GroupNext`, login array, or pagination fields exist in the 80-line type.

This file does not contain:

- `UserLogins` / `GroupTotal` / `GroupNext` / `IMTManagerAPI` / `CIMTManagerAPI`
- any `IList`/`IReadOnlyList`/`IEnumerable` of traders or logins
- a fetch, request, page-loop, or `while`/`for` over manager users
- `ManagerLogin`, group mask, or “first N traders” cap
- any I/O besides property defaults

Manager-side “fetch ALL traders/logins” lives on MT5 Manager (`IMTManagerAPI.UserLogins` / `GroupNext`), not in this cTrader FIX options bag. That surface is out of this slice’s file (see `D:\Prop\reports\swarm\20260818\R020_manager_apis.md`).

## No-loss implication

This type cannot drop, truncate, or skip manager traders because it never fetches them. Incomplete source-login coverage cannot originate here; a missing trader on the copy book would be an MT5 manager ingestion defect, not a `CTraderFixOptions` bind defect. The single `AccountId` is the destination FIX username, not a manager user census. Default `RealCopyExecutionEnabled = false` further keeps this POCO from authorizing NewOrderSingle. Slot 22 therefore has **no path that fails to fetch ALL manager traders/logins** and **no capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read; the angle (failure to fetch ALL manager traders/logins) is absent by construction, not by skipped review.
