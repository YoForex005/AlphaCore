# W500_SLICE_32

- **slot:** 32
- **file:** `D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (112 lines) via `read_file`; grep of this file for `UserLogins|GetAccounts|ListLogins|Mt5Account|UserGetByGroup|UserRequest` returned no matches; workspace grep located ALL-login fetch only in `Mt5/Connectors/NativeMt5BrokerConnector.cs` (`ReadAccountsForGroup` / `UserLogins`) and ingest (`DealIngestionService.SyncBrokerAsync` → `GetAccountsAsync(null)`)
- **verdict:** PASS

## Evidence quotes

`BrokerCatalogSeed.EnsureAsync` is a catalog bootstrap. It inserts missing broker rows, one `XAUUSD` canonical instrument, a default kill-switch, and two FIX session-state rows, then `SaveChangesAsync`. It never connects a manager, never enumerates groups, and never writes trader logins.

```9:12:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
public static class BrokerCatalogSeed
{
    public static async Task EnsureAsync(TraderDbContext db, CancellationToken ct)
    {
```

Each broker row stores a **single scalar** `ManagerLogin` (manager-API identity), not a trader/login list:

```23:23:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
                ManagerLogin = 2027,
```

```45:45:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
                ManagerLogin = 9904,
```

Guards are existence checks on catalog tables only (`Brokers` by code, `CanonicalInstruments`, `KillSwitches`, `FixSessionStates`). No `Mt5Accounts`, no `UserLogins`, no group walk:

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

Kill-switch seed is `None` / `"default"` — not a login-window or group-filter policy:

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

FIX rows are session metadata (`Disconnected`; trade last-error states NewOrderSingle off). They do not iterate manager users.

This file does not contain:

- `UserLogins` / `UserGetByGroup` / `UserRequestArray` / `UserRequestByLogins`
- `GetAccountsAsync` / `ListLoginsAsync` / `Mt5Account` / `Mt5AccountDto`
- `GroupTotal` / `GroupNext` / `GetGroupsAsync`
- any loop over logins, groups, or traders
- pagination, `Take(`, or a max-login cap

`ManagerLogin` on `Broker` is the manager-API login used for connect identity (dashboard masks it). It is **not** consumed as the trader census. Live connectors take manager login from env (`LiveMt5Registration.CreateConnectors` → `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN`), not from this seed row. `ListLoginsAsync` reads `Mt5Accounts` populated by ingest.

ALL-login fetch lives **outside this slice**:

```44:47:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
```

```227:232:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
```

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

`EnsureAsync` is also unused on the API startup path (hosts call `DemoSeeder`, not this type). That is a wiring observation, not a truncated `UserLogins` call in this file.

## No-loss implication

`BrokerCatalogSeed` cannot drop, cap, or mis-page manager traders because it never fetches them. It cannot size, flatten, or copy risk from a partial login set. Worst case of this file is inserting two broker catalog rows (one manager identity each), a kill-switch, and disconnected FIX-state rows. Slot 32 therefore has **no failure-to-fetch-ALL-manager-traders/logins in the assigned file**.

Residual completeness risk (a `UserGetByGroup` / `UserLogins` miss, or `ListLoginsAsync` empty until ingest runs) belongs to `NativeMt5BrokerConnector` / `DealIngestionService` / `EfTradingStore`, not this seeder.

Empty-PASS justification: the assigned file was fully read (112/112 lines); the angle (failure to fetch ALL manager traders/logins) is absent by construction, not by skipped review.
