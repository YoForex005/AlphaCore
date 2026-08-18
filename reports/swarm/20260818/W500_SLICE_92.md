# W500_SLICE_92

- **slot:** 92
- **file:** `D:/Prop/src/Application/Contracts/Mt5Contracts.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (80/80 lines) via `read_file`; grep on this file for `manager|trader|login|ALL` (10 hits, all DTO fields / `GetDealsAsync`/`GetPositionsAsync` login args / `IBrokerRegistry.All()`); grep for `Take(|Skip(|page|limit|200|Maxmaster|MT5_GROUP|filter|HasMore|cursor|offset` — no fetch-subset hits; followed callers `DealIngestionService.GetAccountsAsync(null)` and implementers `NativeMt5BrokerConnector.GetAccountsCore` / `FakeMt5BrokerConnector.GetAccountsAsync`
- **verdict:** PASS

## Binding law (this angle)

Architecture v2 §6: `IMt5BrokerConnector.GetAccountsAsync(...)` is the Manager-side account/login census port. The system must be able to request the **full** manager-visible trader set, not a plan-mapped or first-N subset.

Architecture v2 §7 / §9: `MT5_DEFAULT_GROUP` / `MT5_GROUP_*` must **not** determine which accounts are fetched. Discovery is Manager API (groups → users/logins). Plan labels are optional mapping after census.

Phase / A005: live ingest asks `GetAccountsAsync(null)` and the native walker must return every group’s logins (`UserRequestArray` / `UserGetByGroup` / `UserLogins` fallback). The **contract** must not encode a page size, login allowlist, or default-group-only signature.

This slice owns **only** the Application port/DTO file. It does not run Manager API. FAIL here would mean the port itself cannot request ALL logins, or it encodes a subset.

## Evidence quotes

`Mt5Contracts.cs` is DTO + port surface only (80 lines). Trader identity is `Mt5AccountDto.Login`. There is no rights/enable/plan field that would drop a login at the contract layer:

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

The ALL-traders/logins port is unbounded `IReadOnlyList<Mt5AccountDto>` with an **optional** group. There is no `Take`, page, cursor, `maxAccounts`, or hardcoded group name:

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

`IBrokerRegistry.All()` is **connectors**, not traders. Bulk deal/position ports exist; they are group-wide history/exposure, not a login census, and they do not cap accounts:

```65:79:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IBrokerRegistry
{
    IMt5BrokerConnector Get(string brokerCode);
    IReadOnlyList<IMt5BrokerConnector> All();
}

public interface IMt5BulkDealReader
{
    Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}

public interface IMt5BulkPositionReader
{
    Task<IReadOnlyList<Mt5PositionDto>> GetGroupPositionsAsync(string? groupMask, CancellationToken ct);
}
```

This file does **not** contain:

- `UserLogins` / `UserRequestArray` / `UserGetByGroup` / `UserRequestByLogins` (implementation-only; not required on the port)
- `Take(` / `Skip(` / `200` / `MaxAccounts` / cursor / `HasMore`
- `MT5_DEFAULT_GROUP` / `MT5_GROUP_*` / `demo\Maxmaster` as a fetch filter
- a login allowlist or “manager login is the only trader” field
- pagination parameters on `GetAccountsAsync`

Callers already treat `group: null` as the full manager book (not a hole in this file):

```47:48:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Same shape in `SyncBrokerAsync` (line 61). Fake connector maps null/whitespace → entire in-memory list. Native `GetAccountsCore` maps null/whitespace → every name from `GetGroupsCore()`, then `ReadAccountsForGroup` (those bodies are **out of this slice**).

Residual (not a contract FAIL): `string? group` is undocumented XML-wise, so a future implementer could misread null as “no rows.” Current implementers and callers agree null = ALL. Completeness vs `UserLogins` length / `GroupTotal` is an implementer/host concern (`NativeMt5BrokerConnector`, `LiveIngestHostedService`), not a subset encoded here.

## No-loss implication

`Mt5Contracts.cs` cannot omit, page-cut, or one-login-shrink the manager-visible trader set. It is a request surface: `GetAccountsAsync(null)` is specified as an unbounded list of `Mt5AccountDto` (each with `Login`). This file does not place, flatten, or copy live orders.

Capital-relevant holes (partial `UserRequestArray`, empty pump cache, host one-shot ingest, swallowed connect errors) live **downstream** of this port. They are not introduced by the contract. No-loss path in this slice: the Application layer **can** ask for every manager trader/login; nothing in `Mt5Contracts.cs` forbids or truncates that census.

Empty-PASS justification: the assigned file was fully read (80/80 lines); the angle (failure to fetch ALL manager traders/logins) is **absent from this contract by construction** — the port is the unbounded ALL-accounts request — not by skipped review.
