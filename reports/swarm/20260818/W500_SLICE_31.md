# W500_SLICE_31

- **slot:** 31
- **file:** `D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (140 lines) via `read_file`; grep on `D:/Prop/src` for `GetGroups|GroupTotal|GroupNext|GroupRequestArray|CreateDefault|DemoSeeder`; cross-read `FakeMt5BrokerConnector.DemoBrokerFactory`, `DealIngestionService.SyncBrokerAsync`, `NativeMt5BrokerConnector.GetGroupsCore`, `LiveIngestHostedService`, `BrokerCatalogSeed`, API/worker callers
- **verdict:** FAIL

## Evidence quotes

`DemoSeeder.SeedAsync` never connects as a manager login and never enumerates manager-visible groups. After catalog inserts it builds a **private** `BrokerRegistry` from `DemoBrokerFactory.CreateDefault()` (`FakeMt5BrokerConnector` only), then `DealIngestionService.SyncBrokerAsync` on that Fake graph. There is no `NativeMt5BrokerConnector`, no `GroupRequestArray("*")`, no `GroupTotal`/`GroupNext`, no DI `IBrokerRegistry`.

```126:138:D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs
        var (achiever, starwave) = DemoBrokerFactory.CreateDefault();
        var registry = new BrokerRegistry(new IMt5BrokerConnector[] { achiever, starwave });
        var ingestion = new DealIngestionService(registry, store);
        var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
        await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, ct);
        await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, ct);

        foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
        {
            var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
            await scoring.RebuildTraderAsync(code, login, ct);
        }
```

The Fake factory is a **closed four-name list**, not the manager ACL universe (A39 object A). Missing live names include at least the env catalog siblings `demo\yo-1step`, `contest\yo-1step`, `contest\yo-instant`, `demo\yo-payp`, `contest\yo-payp`, plus any contest/manager/rebate/archive group the manager can see.

```80:105:D:/Prop/src/Mt5/Connectors/FakeMt5BrokerConnector.cs
        var achiever = new FakeMt5BrokerConnector(
            "ACHIEVER",
            groups: new[]
            {
                new Mt5GroupDto(@"demo\Maxmaster", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"demo\yo-2step", "USD", 2, "Achiever", 100, 50, true),
                new Mt5GroupDto(@"contest\yo-2step", "USD", 2, "Achiever", 100, 50, true)
            },
            ...
        var starwave = new FakeMt5BrokerConnector(
            "STARWAVEFX",
            groups: new[]
            {
                new Mt5GroupDto(@"real\standard", "USD", 2, "StarwaveFX", 80, 50, true)
            },
```

`DealIngestionService` will upsert **whatever** `GetGroupsAsync` returns, then walk that same list for bulk deals. On this seeder path that is the Fake subset, not `CIMTManagerAPI` discovery.

```40:54:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        ...
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
```

`FakeMt5BrokerConnector.GetGroupsAsync` returns the constructor list only. Fake does not implement `IMt5BulkDealReader`, so deals come from the four hardcoded logins, not `DealRequestByGroup` over every manager group.

First-writer lock: if any `Broker` row exists, the seeder returns without discovery. First empty-store boot of API / mt5-worker / fix-worker plants the Fake four groups.

```22:23:D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs
        if (await db.Brokers.AnyAsync(ct))
            return;
```

Catalog `Broker` rows carry manager logins `2027` / `9904` and lab IPs but are **never used** as a connect/enumerate input. Group rows are a Fake side-effect, not `GroupRequestArray`.

Native `GetGroupsCore` (`GroupRequestArray("*")` then cache `GroupTotal`/`GroupNext`) exists on `NativeMt5BrokerConnector` and is used by `LiveIngestHostedService` via DI. **This file bypasses that path.** A39 “Never”: hard-coded `demo\yo-2step` / plan names as the enumerator.

Callers of `DemoSeeder.SeedAsync` (product): `D:\Prop\apps\api\Program.cs` (startup after `EnsureCreatedAsync`), `D:\Prop\apps\mt5-worker\Program.cs`, `D:\Prop\apps\fix-worker\Program.cs`, plus `SeedingAndStoreTests`. `/api/ops/resync` is a sibling defect (same four logins) but is not this file.

This is **not** an empty-PASS. The assigned file is the first writer of `mt5_groups` and it **does** populate groups — from a Fake subset, not ALL manager groups.

## No-loss implication

No-loss / RISK_BLOCKED / shadow gates can only see accounts, deals, and positions that were ingested. A four-name Fake snapshot omits every other manager-visible group (`demo\yo-1step`, `contest\yo-instant`, pay-first, Flexy, rebate, archive, and any group added on the server). Hidden logins keep trading; their drawdown never reaches `BaselineScorer` / kill-switch / copy eligibility. Dashboard `mt5_groups` after first seed looks complete while the live book is larger. Live ingest may later upsert more groups **if** it runs and Connect succeeds; until then (and if the seeder is treated as the universe) capital can be lost in groups this file never fetched. Slot 31 is a **FAIL** on ALL-group discovery in `DemoSeeder`.
