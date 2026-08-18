# W500_SLICE_17

- **slot:** 17
- **file:** `D:/Prop/src/Application/Contracts/Mt5Contracts.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (73 lines) via `read_file`; grep on this file for `position` (4 hits, none a cap); workspace grep for `Take(200)` / `GetPositionsAsync`
- **verdict:** PASS

## Evidence quotes

`Mt5Contracts.cs` is DTO + connector/registry surface only. It defines `Mt5PositionDto`, a per-login `GetPositionsAsync`, and an unbounded `GetAccountsAsync`. There is no `200` literal, no `Take`, no page size, and no “first N accounts” rule.

```40:63:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public sealed record Mt5PositionDto(
    long PositionTicket,
    long Login,
    string Symbol,
    TradeDirection Direction,
    ulong VolumeNative,
    decimal PriceOpen,
    decimal PriceCurrent,
    decimal PriceSl,
    decimal PriceTp,
    decimal Profit,
    DateTimeOffset TimeCreate);

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

This file does not contain:

- `Take(200)` / `Take(`
- `200` as a constant, comment, or parameter default
- account-rank filters (`Skip`, `MaxAccounts`, cursor, page)
- a bulk `GetPositions` that could itself truncate the book

The only position-related members are `Mt5DealDto.PositionId`, `Mt5PositionDto` fields, and `IMt5BrokerConnector.GetPositionsAsync(long login, …)`. That signature is one login, full list for that login. `GetAccountsAsync(string? group, …)` likewise returns `IReadOnlyList<Mt5AccountDto>` with no count bound.

`IMt5BulkDealReader` exists for group-wide deals only:

```71:74:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IMt5BulkDealReader
{
    Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
```

No sibling bulk-position interface lives here.

The first-200-accounts positions cutoff is **not** in this contract. It is a caller-side loop in `DealIngestionService.SyncBrokerAsync`:

```74:78:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        foreach (var account in accounts.Take(200))
        {
            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }
```

That `Take(200)` consumes this interface; it is not defined by it. Slot 17’s assigned file does not impose, encode, or document that cap.

## No-loss implication

`Mt5Contracts.cs` cannot silently drop open positions for accounts 201+. Any call through `GetPositionsAsync(login)` is specified as the full current book for that login. Missing live exposure for later accounts is a downstream ingestion policy (`DealIngestionService` `accounts.Take(200)`), out of this slice’s file. This contract therefore has **no first-200-accounts position cap** and **no capital-loss / hidden-book path of its own**.

Empty-PASS justification: the assigned file was fully read; the angle (positions limited to first 200 accounts) is absent from the contract by construction, not by skipped review.
