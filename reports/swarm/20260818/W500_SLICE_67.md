# W500_SLICE_67

- **slot:** 67
- **file:** `D:/Prop/src/Application/Contracts/Mt5Contracts.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (80/80 lines) via `read_file`; grep on this file for `200|position|account` (8 hits, none a cap); workspace grep for `Take(200)` / `accounts.Take` / `GetPositionsAsync` / `GetGroupPositionsAsync`
- **verdict:** PASS

## Evidence quotes

`Mt5Contracts.cs` is DTO + connector/registry/bulk-reader surface only (80 lines). It defines `Mt5AccountDto`, `Mt5PositionDto`, per-login `GetPositionsAsync`, unbounded `GetAccountsAsync`, and optional group-wide `IMt5BulkPositionReader.GetGroupPositionsAsync`. There is no `200` literal, no `Take`, no page size, and no “first N accounts” rule.

```40:78:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
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

This file does not contain:

- `Take(200)` / `Take(`
- `200` as a constant, comment, parameter default, or XML doc
- account-rank filters (`Skip`, `MaxAccounts`, cursor, page)
- a signature that truncates the position book by account rank

Position-related members are `Mt5DealDto.PositionId`, `Mt5PositionDto` fields, `IMt5BrokerConnector.GetPositionsAsync(long login, …)` (one login, full list for that login), and `IMt5BulkPositionReader.GetGroupPositionsAsync(string? groupMask, …)` (optional group mask, still `IReadOnlyList<Mt5PositionDto>` with no count bound). `GetAccountsAsync(string? group, …)` likewise returns `IReadOnlyList<Mt5AccountDto>` with no count bound.

**Stale prior swarm (do not reuse):** `W500_SLICE_17.md` (same file + angle) quoted `DealIngestionService.SyncBrokerAsync` as `foreach (var account in accounts.Take(200))`. That loop is **gone** from current source. Grep of `D:\Prop\src` for `Take(200)` / `accounts.Take` is empty. Current ingest:

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

Native connector implements the bulk interface (`NativeMt5BrokerConnector : IMt5BrokerConnector, IMt5BulkDealReader, IMt5BulkPositionReader`) and maps `GetGroupPositionsAsync` to `PositionRequestByGroup` / `PositionGetByGroup` with no 200-account window. Fake connector implements only per-login `GetPositionsAsync` and filters `_positions.Where(p => p.Login == login)` with no rank cap.

The only remaining `Take(200)` in the C# tree is a reconstructed-trade explorer cap, **not** a live-position account window:

```101:108:D:/Prop/apps/api/Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
```

That path is out of this slice’s file.

## No-loss implication

`Mt5Contracts.cs` cannot silently drop open positions for accounts 201+. Any call through `GetPositionsAsync(login)` is specified as the full current book for that login; `GetGroupPositionsAsync(groupMask)` is specified as the full current book for the mask (`"*"` in ingest). This contract therefore has **no first-200-accounts position cap** and **no capital-loss / hidden-book path of its own**. Residual UI truncation of reconstructed trades (`GET /api/trades` `Take(200)`) cannot hide live MT5 exposure and is not owned by this file.

Empty-PASS justification: the assigned file was fully read (80/80 lines); the angle (positions limited to first 200 accounts) is absent from the contract by construction, not by skipped review.
