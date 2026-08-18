# W500_SLICE_11

- **slot:** 11
- **file:** `D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (342 lines) via `read_file`; grep across `D:/Prop/src` for `GetGroups|UpsertGroup|ManagerGroup|GetAllGroups|ListGroups|PlanMapping|MT5_GROUP_|EnabledForAnalysis`
- **verdict:** PASS

## Evidence quotes

`EfTradingStore` is the EF persistence adapter for `ITradingStore`. It does **not** talk to the MT5 Manager API. It does **not** enumerate, page, or subset manager groups. Group I/O in this file is a single-row upsert keyed by `(BrokerId, Name)`.

```22:51:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public async Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Groups.SingleOrDefaultAsync(
            g => g.BrokerId == brokerId && g.Name == group.Name, ct);
        if (existing is null)
        {
            _db.Mt5Groups.Add(new Mt5Group
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                Name = group.Name,
                Currency = group.Currency,
                CurrencyDigits = group.CurrencyDigits,
                Company = group.Company,
                MarginCall = group.MarginCall,
                MarginStopOut = group.MarginStopOut,
                ConnectionsAllowed = group.ConnectionsAllowed,
                EnabledForAnalysis = true,
                LastDiscoveredAt = now,
                LastSyncedAt = now
            });
        }
        else
        {
            existing.Currency = group.Currency;
            existing.LastSyncedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
```

What this method does **not** do (measured, after full-file read):

- no `GetGroupsAsync` / `ListGroups` / `GetAllGroups` / `GroupTotal` / `GroupNext` / `GroupRequestArray`
- no `Where` on `PlanMapping`, `EnabledForAnalysis`, `Name` allow-list, `MT5_GROUP_*`, or `DefaultGroupHint`
- no `Take` / skip / page that would persist only a subset
- no throw / `continue` / early `return` that drops a caller-supplied `Mt5GroupDto`

The port matches that write-only group contract. `ITradingStore` (in `DealIngestionService.cs`) exposes `UpsertGroupAsync` and has **no** group-read member:

```8:20:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
public interface ITradingStore
{
    Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct);
    Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct);
    Task<bool> UpsertDealAsync(Guid brokerId, Mt5DealDto deal, DateTimeOffset now, CancellationToken ct);
    Task ReplacePositionsAsync(Guid brokerId, long login, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct);
    Task<IReadOnlyList<NormalizedDeal>> LoadDealsAsync(Guid brokerId, string brokerCode, long login, CancellationToken ct);
    Task ReplaceReconstructedAsync(Guid brokerId, long login, IReadOnlyList<ReconstructedTradeResult> trades, CancellationToken ct);
    Task UpsertScoreAsync(TraderScore score, CancellationToken ct);
    Task PersistDemoShadowAsync(Guid brokerId, long login, TraderState state, IReadOnlyList<ReconstructedTradeResult> completedXau, CancellationToken ct);
    Task<Guid> ResolveBrokerIdAsync(string brokerCode, CancellationToken ct);
    Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct);
}
```

The only store-side census read is unfiltered logins for a broker, not a group subset:

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Manager-group **fetch** lives on the connector port, not this store:

```53:60:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IMt5BrokerConnector
{
    string BrokerCode { get; }
    Task ConnectAsync(CancellationToken ct);
    Task DisconnectAsync(CancellationToken ct);
    Task<bool> IsConnectedAsync(CancellationToken ct);
    Task<IReadOnlyList<Mt5GroupDto>> GetGroupsAsync(CancellationToken ct);
```

Ingestion already walks the connector list and upserts every name (out of this slice’s file, cited only to locate the angle):

```40:42:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);
```

Dashboard “all groups” read is `EfDashboardQueries.GetGroupsAsync` (`_db.Mt5Groups.ToListAsync`), not `EfTradingStore`.

Out-of-slice (not scored here): `UpsertGroupAsync` on update refreshes only `Currency` + `LastSyncedAt` (other detail fields freeze after first insert). That is persist-completeness, not a failure to fetch or persist **all names**. Completeness of `mt5_groups` vs the live Manager set is the connector walk (`NativeMt5BrokerConnector.GetGroupsCore`), not this adapter.

Empty-PASS justification: the assigned file was fully read; the angle (failure to fetch ALL manager groups) is **absent by construction** — this type never fetches manager groups and never filters the ones it is given.

## No-loss implication

This file cannot open, close, or size destination exposure. It cannot omit a manager group via a plan/allow-list filter, so it does not create a silent incomplete-census hole on the persist path. If a live Manager group is missing from `mt5_groups`, the drop happened **upstream** of `UpsertGroupAsync` (connector `GetGroupsAsync` / pump cache). Slot 11 therefore has **no capital-loss path** and **no store-side “fetch-all groups” defect** that would hide traders from the no-loss / shadow pipeline.
