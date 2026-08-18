# W500_SLICE_42

- **slot:** 42
- **file:** `D:/Prop/src/Application/Contracts/Mt5Contracts.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (80/80 lines) via `read_file`; grep on this file for `Take|limit|page|Max|UserLogins|GetLogins|mask` → only the three `Login` DTO fields (no cap, no dedicated login enumerator, no page); workspace grep of `GetAccountsAsync` / `UserLogins` to bind callers
- **verdict:** PASS

## Evidence quotes

`Mt5Contracts.cs` is DTO + connector/registry/bulk-reader surface only. It is not a Manager session and does not call `UserLogins` / `UserRequestArray`. The assigned angle (failure to fetch ALL manager traders/logins) would have to be encoded here as a missing census API, a page/limit, or a signature that cannot express “all visible users.” None of those are present.

Trader identity is a required `long Login` on the account row. Deals and positions also carry `Login`, so any account list returned by the connector is already a login census:

```14:22:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public sealed record Mt5AccountDto(
    long Login,
    string? GroupName,
    int Leverage,
    decimal Balance,
    decimal Equity,
    decimal Margin,
    decimal MarginFree,
    decimal Profit);
```

The live Manager connector contract exposes an unbounded group list and an unbounded account list. `GetAccountsAsync` takes an optional `string? group` and returns `IReadOnlyList<Mt5AccountDto>` — no `skip`, `take`, `page`, `cursor`, `max`, or `limit` parameter:

```53:63:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5AccountDto>> GetAccountsAsync(string? group, CancellationToken ct);
    Task<IReadOnlyList<Mt5DealDto>> GetDealsAsync(long login, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
    Task<IReadOnlyList<Mt5PositionDto>> GetPositionsAsync(long login, CancellationToken ct);
}
```

`IBrokerRegistry.All()` is **all connectors**, not a truncated trader book:

```66:69:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}
```

Bulk readers are group-scoped and also return `IReadOnlyList` with no count bound. Positions already accept a nullable group **mask** (`*`-style). Deals take a required group name (callers walk `GetGroupsAsync`):

```71:79:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IMt5BulkDealReader
{
    Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}

public interface IMt5BulkPositionReader
{
    Task<IReadOnlyList<Mt5PositionDto>> GetGroupPositionsAsync(string? groupMask, CancellationToken ct);
}
```

This file does not contain:

- `GetUserLogins` / `GetGroupLogins` / `GetLoginsAsync` as a separate method (not required: `GetAccountsAsync` already returns every `Mt5AccountDto.Login`)
- `Take(`, `Skip(`, `page`, `PageSize`, `MaxAccounts`, `200`, cursor, or any other login-universe cap
- a comment or default that first-N / demo / mapped-plan logins are the census
- a required non-null group that would make “all traders” inexpressible (`group` is `string?`)

Product convention for “ALL manager traders/logins” is already `GetAccountsAsync(null, ct)`. The ingestion caller (out of this slice’s file) uses that exact shape and does not `Take` the account list:

```44:48:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

```60:61:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
```

Incomplete live enumeration, if it happens, is an **implementation** concern (`NativeMt5BrokerConnector.ReadAccountsForGroup` `UserRequestArray` → `UserGetByGroup` → `UserLogins` fallback), not a contract-level refusal to ask for the full set. This file does not implement that walk and does not forbid `UserLogins` / mask `*`.

There is no dedicated completeness token (`total` vs returned). That is a verification gap, not a fetch-all failure: the specified return type is still the entire `IReadOnlyList`, not a page.

## No-loss implication

`Mt5Contracts.cs` cannot silently drop manager traders or logins. `GetAccountsAsync(null)` is specified as the full account/login census (`IReadOnlyList<Mt5AccountDto>` with required `Login`, nullable group filter). Per-login deals/positions and group bulk readers all consume that universe; none of those signatures encode a first-N or mapped-subset cap. Any missing 5k-book login would be a downstream connector or host bind (`UserLogins` retcode, TFM, pump/cache), out of this slice’s file. This contract therefore has **no “failure to fetch ALL manager traders/logins” path of its own** and **no hidden-book / missed-copy / missed-risk-flag path encoded here**.

Empty-PASS justification: the assigned file was fully read (80 lines); the angle is absent from the contract by construction (unbounded `GetAccountsAsync(string? group)`), not by skipped review.
