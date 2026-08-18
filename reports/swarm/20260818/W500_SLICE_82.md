# W500_SLICE_82

- **slot:** 82
- **file:** `D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (112/112 lines) via `read_file`; grep of this file for `UserLogins|GetAccounts|Take\(|UserRequest|AllLogins|GetUsers|ListLogins|Mt5Account` returned **no matches**; workspace grep located ALL-login fetch only in `Mt5/Connectors/NativeMt5BrokerConnector.cs` (`GetAccountsCore` / `ReadAccountsForGroup` / `UserRequestArray` / `UserLogins`) and ingest (`DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` → `GetAccountsAsync(null)`)
- **verdict:** PASS

## Evidence quotes

`BrokerCatalogSeed.EnsureAsync` is a catalog bootstrap. It inserts missing broker rows, one `XAUUSD` canonical instrument, a default kill-switch, and two FIX session-state rows, then `SaveChangesAsync`. It never connects a manager, never enumerates groups, and never writes trader logins.

```9:12:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
public static class BrokerCatalogSeed
{
    public static async Task EnsureAsync(TraderDbContext db, CancellationToken ct)
    {
```

Each broker row stores a **single scalar** `ManagerLogin` (manager-API identity), not a trader/login census and not a fetch:

```16:24:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
            db.Brokers.Add(new Broker
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"),
                Code = BrokerCodes.Achiever,
                DisplayName = "Achiever",
                Server = "57.128.141.65",
                Port = 443,
                ManagerLogin = 2027,
                ServerName = "AchieverGlobalMarkets-Server",
```

```38:46:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
            db.Brokers.Add(new Broker
            {
                Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"),
                Code = BrokerCodes.StarwaveFx,
                DisplayName = "StarwaveFX",
                Server = "84.201.6.142",
                Port = 443,
                ManagerLogin = 9904,
                ServerName = "StarwaveFX",
```

`Broker.ManagerLogin` on the entity is likewise one `long`, not a collection:

```10:10:D:/Prop/src/Domain/Entities/Broker.cs
    public long ManagerLogin { get; set; }
```

Guards are existence checks on catalog tables only (`Brokers` by code, `CanonicalInstruments`, `KillSwitches`, `FixSessionStates`). There is no `Mt5Accounts` insert, no `UserLogins` walk, and no group/login loop:

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

FIX rows are destination-session metadata (`Disconnected`; trade last-error states NewOrderSingle off). They do not iterate manager users. Persist is catalog-only:

```110:111:D:/Prop/src/Infrastructure/Seeding/BrokerCatalogSeed.cs
        await db.SaveChangesAsync(ct);
    }
```

This file does not contain:

- `UserLogins` / `UserGetByGroup` / `UserRequestArray` / `UserRequestByLogins`
- `GetAccountsAsync` / `ListLoginsAsync` / `Mt5Account` / `Mt5AccountDto`
- `GroupTotal` / `GroupNext` / `GetGroupsAsync` / `IMTManagerAPI`
- any loop over logins, groups, or traders
- pagination, `Take(`, `fromLogin`, or a max-login cap
- a hardcoded trader list (`10001` / `10002` / `10003` / `99001` live only in `DemoSeeder`, not here)

`ManagerLogin` is the manager-API connect identity. It is **not** consumed as the trader universe. Live connectors take manager login from env (`LiveMt5Registration.CreateConnectors` → `MT5_LOGIN` / `MT5_STARWAVEFX_LOGIN`), not from this seed row. Dashboard only masks the scalar (`EfDashboardQueries` `MaskLogin(b.ManagerLogin)`).

API startup **does** call this seeder (catalog only — no FakeMt5, no `10001` rebuild):

```149:154:D:/Prop/apps/api/Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

That wiring inserts broker/instrument/kill-switch/FIX rows. It still does not fetch manager traders. Empty `Mt5Accounts` after seed is expected until ingest; it is not a truncated `UserLogins` in this file.

ALL-login fetch lives **outside this slice**:

```44:48:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

```189:210:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }
```

```223:232:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
```

## No-loss implication

`BrokerCatalogSeed` cannot drop, cap, or mis-page manager traders because it never fetches them. It cannot size, flatten, or copy risk from a partial login set. Worst case of this file is inserting two broker catalog rows (one manager identity each), a kill-switch (`None`), and disconnected FIX-state rows (trade last-error already records NewOrderSingle off). Slot 82 therefore has **no failure-to-fetch-ALL-manager-traders/logins in the assigned file** and **no capital-loss path** of its own.

Residual completeness risk (a `UserGetByGroup` / `UserLogins` miss, one-shot ingest, or `ListLoginsAsync` empty until live sync) belongs to `NativeMt5BrokerConnector` / `DealIngestionService` / the ingest host, not this seeder.

Empty-PASS justification: the assigned file was fully read (112/112 lines); the angle (failure to fetch ALL manager traders/logins) is absent by construction, not by skipped review.
