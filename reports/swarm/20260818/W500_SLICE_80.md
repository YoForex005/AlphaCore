# W500_SLICE_80 — DealIngestionService vs dummy/seeded data on the live path

- **slot:** 80
- **file:** `D:/Prop/src/Application/Ingestion/DealIngestionService.cs`
- **angle:** dummy or seeded data still reachable on the live path
- **read:** full file (145/145 lines) via `read_file`; `grep` on this file and `Application/Ingestion` for dummy/seed/fake/mock/sample/hardcoded/placeholder/synthetic/fixture/demo/stub (`PersistDemoShadowAsync` only); callers `DemoSeeder`, `LiveIngestHostedService`, `DependencyInjection`, `LiveMt5Registration`, `apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `apps/mt5-worker/Worker.cs`, `FakeMt5BrokerConnector`/`DemoBrokerFactory`, `EfTradingStore.PersistDemoShadowAsync`, `BrokerCatalogSeed`
- **product source modified:** **No**
- **verdict:** **FAIL**

## File

`D:/Prop/src/Application/Ingestion/DealIngestionService.cs` is the only file under `Application/Ingestion`. It defines `ITradingStore`, `DealIngestionService` (`SyncCatalogAsync` / `SyncBrokerAsync`), `BrokerSyncResult`, and `ReconstructionScoringService.RebuildTraderAsync`.

The assigned type does **not** embed deal arrays, tickets, prices, or login tables. Ingest is connector-driven. That is not an empty PASS: dummy tape is still written **through this type** on live worker hosts.

`grep` of this file (case-insensitive dummy/seed/fake/mock/sample/test-data/hardcoded/placeholder/synthetic/fixture/demo/stub): **2 hits**, both `PersistDemoShadowAsync` (interface L17, `RebuildTraderAsync` L143). No `10001`, `DemoBrokerFactory`, or `FakeMt5` tokens in this file.

## Angle

Assigned defect: dummy or seeded market/book rows can still reach the live ingest/score path. A PASS would require that this type cannot persist FakeMt5/`DemoBrokerFactory` books into the store the live host later reads, and that same-file scoring does not keep a demo persist hook on every live rebuild.

Observed: DI native connectors are fail-closed, and the **API** no longer calls `DemoSeeder` (stale vs `W500_SLICE_30`). **mt5-worker** and **fix-worker** still construct `new DealIngestionService(fakeRegistry, liveStore)` via `DemoSeeder` before `Run()`. The class has no refuse-fake guard.

## Evidence quotes

### 1. This type is an unguarded pump

`SyncCatalogAsync` / `SyncBrokerAsync` resolve any `IBrokerRegistry` implementor and upsert whatever it returns. No `is FakeMt5BrokerConnector` throw, no seed flag, no env gate:

```37:50:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);

        return new BrokerSyncResult(groups.Count, accounts.Count, 0, 0);
    }
```

```53:79:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var catalog = await SyncCatalogAsync(brokerCode, ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;
        var groups = await connector.GetGroupsAsync(ct);
        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;

        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
        else
        {
            foreach (var account in accounts)
            {
                var deals = await connector.GetDealsAsync(account.Login, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
```

`FakeMt5BrokerConnector` does **not** implement `IMt5BulkDealReader`, so the seeder path uses the per-login `GetDealsAsync` branch and still upserts the canned book.

### 2. Live worker hosts still seed through this type

`new DealIngestionService` exists in one product site — `DemoSeeder` — against `DemoBrokerFactory.CreateDefault()` (`FakeMt5BrokerConnector` pair) and the **same** DI `ITradingStore`:

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

That seeder is still invoked after `EnsureCreatedAsync` on both worker hosts (before `host.Run()`):

```11:20:D:/Prop/apps/mt5-worker/Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DemoSeeder.SeedAsync(
        db,
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ITradingStore>(),
        scope.ServiceProvider.GetRequiredService<TraderIntelligence.Application.Ingestion.ReconstructionScoringService>(),
        CancellationToken.None);
}
```

`apps/fix-worker/Program.cs` is the same `DemoSeeder.SeedAsync` block (lines 11–19). `DemoSeeder` no-ops only if `db.Brokers.AnyAsync`. First boot, empty Postgres, or the DI in-memory fallback `UseInMemoryDatabase("trader-intelligence-live")` **will** ingest FakeMt5 groups/accounts/deals through `DealIngestionService` into a database named live.

Seed payload (`DemoBrokerFactory.CreateDefault`): groups `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`, `real\standard`; logins **10001, 10002, 10003, 99001**; synthetic XAUUSD round-trips (`BuildAchieverDeals` / `BuildStarwaveDeals`).

### 3. Same-file scoring always hits the demo-named persist on the live rebuild path

`LiveIngestHostedService` and `/api/ops/resync` call `ReconstructionScoringService.RebuildTraderAsync` for every store login. That method always loads deals, reconstructs, scores, then:

```118:143:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);

        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            // ... scores / flags from BaselineScorer ...
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);

        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
    }
```

`PersistDemoShadowAsync` does not invent deals. It always writes an outbox `ScoreUpdate`. If `state == SHADOW` and a `DestinationQuotes` row exists, it writes `CopyIntent` `Status = "SHADOW_ONLY"` and `ShadowCopyEngine.SimulateEntry` fills. Seed login 10002 is the averaging-down / lot-escalation tape that can land SHADOW. `LiveIngestHostedService` then `ListLoginsAsync`s **all** `Mt5Accounts` — seeded 10001–10003 / 99001 stay in the live score walk if the seeder ran first.

### 4. What the live DI path *does* close (does not flip the verdict)

API startup is catalog-only now (no FakeMt5 through this type on the API process):

```149:154:D:/Prop/apps/api/Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`POST /api/ops/resync` injects the DI `DealIngestionService` and scores `store.ListLoginsAsync` (not the hardcoded `{10001,10002,10003,99001}` set cited in `W500_SLICE_30` — that claim is stale).

DI refuses dummy connector registration:

```35:36:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`LiveMt5Registration.CreateConnectors` returns only `NativeMt5BrokerConnector`. `LiveIngestHostedService` logs that it will not substitute dummy data on a connector exception.

That fail-closed graph is **bypassed** by `DemoSeeder`’s `new DealIngestionService(fakeRegistry, liveStore)` on empty-broker worker start. `mt5-worker` `Worker.cs` then still rebuilds only `{10001,10002,10003,99001}` after a native `SyncBrokerAsync` (caller residual; not in this file).

### Stale prior (do not reuse)

`W500_SLICE_30` (same file + angle) is **partially stale**: API no longer calls `DemoSeeder`; `/api/ops/resync` no longer hardcodes the four demo logins; ingest no longer `accounts.Take(200)`. Worker `DemoSeeder` + unguarded `DealIngestionService` + live `PersistDemoShadowAsync` remain.

## No-loss implication

**Contaminated live book, not a NewOrderSingle from this file.** `DealIngestionService` does not send FIX, does not `OrderSend`, and does not size destination orders. Fabricated XAUUSD fills and demo logins can still persist beside (or instead of, on empty / in-memory `"trader-intelligence-live"`) real manager deals because this type accepts any connector. `RebuildTraderAsync` will score those books and, for SHADOW, emit `SHADOW_ONLY` intents plus simulated fills.

That can present fake equity / martingale / completed-XAU as live intelligence, leave seed accounts in the copy/shadow pipeline after a later real ingest (deals are insert-if-absent in the store), and give a future `REAL_COPY_EXECUTION_ENABLED` flip a mixed dummy+live selection set. Capital cannot be lost *by an order send inside this file*; the no-loss failure mode is **false live state and shadow intents derived from seeded deals this type still accepts**.

## What this FAIL is not

- Not a claim that `DealIngestionService.cs` embeds ticket/price constants (it does not).
- Not a claim that DI registers `FakeMt5BrokerConnector` (it throws if real MT5 passwords are absent).
- Not a claim that the API host still seeds FakeMt5 (it now uses `BrokerCatalogSeed` only).
- Not a claim that `PersistDemoShadowAsync` manufactures dummy deals (it persists shadow/outbox from already-loaded trades).
- Not a PASS on “file has no dummy literals, so live path is clean” — the assigned angle is **reachability**, and live worker hosts still reach this type with `DemoBrokerFactory` data.
