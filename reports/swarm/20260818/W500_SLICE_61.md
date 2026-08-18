# W500_SLICE_61 — EfTradingStore vs failure to fetch ALL manager groups

- **slot:** 61
- **file:** `D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (498 lines) via `read_file`; grep of this file for `Take(|Skip(|MT5_GROUP|PlanMapping|plan.?map` returned **no** matches; `EnabledForAnalysis` appears only as a write default (`true`) on insert, never as a read/filter predicate; workspace grep located Manager-side ALL-group fetch only in `Mt5/Connectors/NativeMt5BrokerConnector.cs` (`GetGroupsCore` → `GroupRequestArray("*")` then `GroupTotal`/`GroupNext`) and the ingest caller `DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync`
- **verdict:** PASS

## Evidence quotes

`EfTradingStore` is an EF persistence adapter for `ITradingStore`. It never talks to the Manager API. There is no `GetAllGroups`, `GroupRequestArray`, `GroupTotal`, `GroupNext`, `GetGroupsAsync`, or `ListGroupsAsync` on this type. Group *fetch* is the connector’s job; this file only upserts whatever `Mt5GroupDto` list the caller already obtained.

The port itself has write methods for groups and **no** group-read method:

```8:24:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
public interface ITradingStore
{
    Task UpsertGroupAsync(Guid brokerId, Mt5GroupDto group, DateTimeOffset now, CancellationToken ct);
    // ...
    Task UpsertGroupsBatchAsync(Guid brokerId, IReadOnlyList<Mt5GroupDto> groups, DateTimeOffset now, CancellationToken ct);
    Task UpsertAccountsBatchAsync(Guid brokerId, IReadOnlyList<Mt5AccountDto> accounts, DateTimeOffset now, CancellationToken ct);
    Task<int> UpsertDealsBatchAsync(Guid brokerId, IReadOnlyList<Mt5DealDto> deals, DateTimeOffset now, CancellationToken ct);
    Task ReplaceBrokerPositionsAsync(Guid brokerId, IReadOnlyList<Mt5PositionDto> positions, CancellationToken ct);
}
```

Single-row upsert keys on `(BrokerId, Name)` only. It does not subset by plan mapping, `EnabledForAnalysis`, or a hardcoded `MT5_GROUP_*` list:

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

The batch path is the live ingest write. It loads **all** existing `Mt5Groups` for the broker (`Where(g => g.BrokerId == brokerId).ToListAsync` — no `Take`/`Skip`) and then walks **every** incoming DTO. No plan overlay, no name allow-list, no `EnabledForAnalysis` predicate:

```343:379:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public async Task UpsertGroupsBatchAsync(Guid brokerId, IReadOnlyList<Mt5GroupDto> groups, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Groups.Where(g => g.BrokerId == brokerId).ToListAsync(ct);
        var byName = existing.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var group in groups)
        {
            if (byName.TryGetValue(group.Name, out var row))
            {
                row.Currency = group.Currency;
                row.CurrencyDigits = group.CurrencyDigits;
                row.Company = group.Company;
                row.MarginCall = group.MarginCall;
                row.MarginStopOut = group.MarginStopOut;
                row.ConnectionsAllowed = group.ConnectionsAllowed;
                row.LastSyncedAt = now;
            }
            else
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
        }

        await _db.SaveChangesAsync(ct);
    }
```

An empty incoming list is a no-op persist: the method still loads existing rows, writes none, and **does not delete** previously discovered groups. That is not a fetch-truncation bug inside this file.

This file does not contain:

- `GetAllGroups` / `GetGroupDetails` / `GroupRequestArray` / `GroupTotal` / `GroupNext`
- `GetGroupsAsync` / `ListGroupsAsync` / any `Mt5Groups` projection returned to a caller
- `Take` / `Skip` / `First` / `Take(50)` caps on groups
- `MT5_GROUP_*`, `PlanMapping`, or any name-subset filter
- a read filter on `EnabledForAnalysis` (it is insert-default `true` only)

ALL-manager-group enumeration lives **elsewhere** (out of this slice’s file):

```144:185:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        // GroupRequestArray("*") then GroupTotal/GroupNext fallback
        // ...
            return list;
    }
```

```44:45:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);
```

Dashboard re-read of stored groups is also out of this type (`EfDashboardQueries.GetGroupsAsync` uses `_db.Mt5Groups.ToListAsync` with no store involvement). Slot 61 does not re-score the connector or the dashboard query.

Residual notes (not this angle, not a FAIL here):

- `UpsertGroupAsync` update path only refreshes `Currency` + `LastSyncedAt` (other columns freeze after first insert). Field-completeness on a *single* group, not “drop groups from the census.”
- `ToDictionary(..., OrdinalIgnoreCase)` can throw if two persisted names collide only on case, aborting the batch. That is a rare persist-path exception, not a silent “fetch fewer than GroupTotal” cut.
- Completeness of `mt5_groups` still depends on the connector handing this store the full Manager walk. That defect, if any, is Slot-0/`NativeMt5BrokerConnector`, not this adapter.

## No-loss implication

`EfTradingStore` cannot omit Manager groups from a live census: it does not fetch them. It cannot send orders, size exposure, or filter which groups exist. Worst case inside this type for groups is persist-what-you-were-given (including empty → leave existing `mt5_groups` rows in place). An incomplete ALL-group walk would have to originate in `GetGroupsCore` / `IMt5BrokerConnector.GetGroupsAsync` before this class is invoked. Slot 61 therefore has **no “failure to fetch ALL manager groups” capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (498 lines); the angle (failure to fetch ALL manager groups) is absent by construction — persist-only, unfiltered foreach of the incoming list — not by skipped review.
