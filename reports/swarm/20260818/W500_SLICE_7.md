# W500_SLICE_7

- **slot:** 7
- **file:** `D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs`
- **angle:** positions limited to first 200 accounts
- **read:** full file (112 lines) via `read_file`; grep on this file for `200` returned no matches; workspace grep for `Take(200)` / position replace located the cap only in `Application/Ingestion/DealIngestionService.cs`
- **verdict:** PASS

## Evidence quotes

`BrokerCatalogSeed.EnsureAsync` is a catalog bootstrap. It inserts missing broker rows, a single `XAUUSD` canonical instrument, a default kill-switch, and two FIX session-state rows, then `SaveChangesAsync`. It never enumerates accounts and never writes position snapshots.

```9:12:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
public static class BrokerCatalogSeed
{
    public static async Task EnsureAsync(TraderDbContext db, CancellationToken ct)
    {
```

Guards are existence checks on catalog tables only (`Brokers` by code, `CanonicalInstruments`, `KillSwitches`, `FixSessionStates`). No `Accounts`, no `mt5_positions_current`, no login list:

```14:15:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.Achiever, ct))
        {
```

```36:37:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        if (!await db.Brokers.AnyAsync(b => b.Code == BrokerCodes.StarwaveFx, ct))
        {
```

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

Kill-switch seed is `None` / `"default"` — not an account-window or position-sync policy:

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

FIX rows are session metadata (`Disconnected`; trade last-error states NewOrderSingle off). They are not position rows and do not iterate logins.

This file does not contain:

- `200` / `Take(200)` / any account-window constant
- `GetAccountsAsync` / `ListLoginsAsync` / account entities
- `GetPositionsAsync` / `ReplacePositionsAsync` / `Mt5PositionDto`
- writes to `mt5_positions_current`

The first-200-accounts position refresh lives **outside this slice**, in ingestion:

```74:78:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        foreach (var account in accounts.Take(200))
        {
            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }
```

Account upserts in that same method walk the full `accounts` list; only the position replace loop is capped. That is not implemented or configured by `BrokerCatalogSeed`.

## No-loss implication

`BrokerCatalogSeed` cannot omit, stale-out, or truncate open positions for accounts 201+ because it never reads or writes positions. It cannot size, flatten, or copy risk. Worst case of this file is inserting catalog / kill-switch / disconnected FIX-state rows. Slot 7 therefore has **no first-200-accounts position-blind-spot in the assigned file**.

Residual no-loss risk (stale or empty `mt5_positions_current` for logins beyond the first 200 if any consumer treats that table as complete open risk) belongs to `DealIngestionService`, not this seeder.

Empty-PASS justification: the assigned file was fully read (112/112 lines); the angle (positions limited to first 200 accounts) is absent by construction, not by skipped review.
