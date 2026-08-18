# W500_SLICE_57

- **slot:** 57
- **file:** `D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (112/112 lines) via `read_file`; grep on this file for `200|Take\(|position|account|limit|first` returned **no matches**; grep `Take(200)` under `D:/Prop/src` returned **no matches**; only remaining `Take(200)` in-repo is `GET /api/trades` on reconstructed rows (`D:/Prop/apps/api/Program.cs`)
- **verdict:** PASS

## Evidence quotes

`BrokerCatalogSeed` is a one-shot catalog bootstrap. `EnsureAsync` inserts missing broker rows, one `XAUUSD` canonical instrument, a default kill-switch, and two FIX session-state rows, then `SaveChangesAsync`. It never lists logins and never writes `Mt5Position` / `mt5_positions_current`.

```9:12:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
public static class BrokerCatalogSeed
{
    public static async Task EnsureAsync(TraderDbContext db, CancellationToken ct)
    {
```

Guards are existence checks on catalog tables only (`Brokers` by code, `CanonicalInstruments`, `KillSwitches`, `FixSessionStates`). There is no `Mt5Accounts` query, no login list, and no position replace:

```14:15:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.Achiever, ct))
        {
```

```36:37:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.StarwaveFx, ct))
        {
```

Achiever / StarwaveFX rows set identity, server, pool size, and enabled flags. They do **not** set a max-account window, a position-snapshot batch size, or any `Take(N)` policy. (Proxy host/port and manager logins are present as broker connection fields; they are not quoted here.)

Instrument seed is a single canonical symbol, not an account census:

```55:62:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.CanonicalInstruments.AnyAsync(ct))
        {
            db.CanonicalInstruments.Add(new CanonicalInstrument
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"),
                Code = "XAUUSD",
                Description = "Gold vs US Dollar"
            });
```

Kill-switch seed is `None` / `"default"` — not an account-window or position-sync cap:

```65:75:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.KillSwitches.AnyAsync(ct))
        {
            db.KillSwitches.Add(new KillSwitch
            {
                Id = Guid.Parse("dddddddd-dddd-dddd-dddd-ddddddddddd1"),
                Mode = KillSwitchMode.None,
                SetBy = "system",
                Reason = "default",
                UpdatedAt = now
            });
        }
```

FIX rows are session metadata only (`Disconnected`; trade last-error states NewOrderSingle off). They are not position books and do not iterate logins. CompIDs / host fields are omitted from this report.

```77:78:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.FixSessionStates.AnyAsync(ct))
        {
```

```110:111:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        await db.SaveChangesAsync(ct);
    }
```

This file does not contain:

- `200` / `Take(200)` / `Take(` / `.Skip(` / any first-N account cutoff
- `GetAccountsAsync` / `ListLoginsAsync` / `Mt5Account` entities
- `GetPositionsAsync` / `ReplacePositionsAsync` / `ReplaceBrokerPositionsAsync` / `Mt5PositionDto`
- writes to `Mt5Positions` / `mt5_positions_current`
- a bindable `MaxAccounts` / `PositionSnapshotLimit` / `MaxPositionAccounts`

The assigned angle is **absent from this file by construction**. Caller of the seeder is API startup (`apps/api/Program.cs` `BrokerCatalogSeed.EnsureAsync`) — catalog rows only.

## Current ingest vs stale prior slices

Older swarm notes (e.g. A005, W500_SLICE_7) quoted a per-login position loop capped as `foreach (var account in accounts.Take(200))`. **That literal is gone from current `D:/Prop/src`.** Live ingest now either bulk-replaces the broker book or walks the **full** account list:

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

The only remaining in-repo `Take(200)` is a reconstructed-trade explorer page, **not** a live-position account window:

```101:108:D:/Prop/apps/api/Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});
```

Those paths are **out of this slice’s file**. Slot 57 asked whether **this** seeder limits position sync to the first 200 accounts. It does not.

## No-loss implication

`BrokerCatalogSeed` cannot omit, stale-out, or truncate open positions for accounts 201+ because it never reads or writes positions. It cannot size, flatten, or copy risk. Worst case of this file is inserting catalog / kill-switch / disconnected FIX-state rows (and enabling NewOrderSingle is explicitly *not* seeded — trade session last-error records “session up for logon/recon only; NewOrderSingle off”). Slot 57 therefore has **no first-200-accounts position-blind-spot in the assigned file**.

Residual no-loss notes (not owned by this file): (1) current `DealIngestionService` no longer applies `accounts.Take(200)` — completeness of the open book now depends on the connector returning the full account list / `GetGroupPositionsAsync("*")`; (2) `GET /api/trades` `Take(200)` can hide older reconstructed rows from the explorer, which is a UI/API window, not a live-position capital path; (3) prior reports that still cite ingest `Take(200)` are stale vs the source read for this slot.

Empty-PASS justification: the assigned file was fully read (112/112 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
