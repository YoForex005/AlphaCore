# W500_SLICE_30 — DealIngestionService vs dummy/seeded data on the live path

| Field | Value |
|---|---|
| Slot | 30 |
| File | `D:/Prop/src/Application/Ingestion/DealIngestionService.cs` |
| Angle | dummy or seeded data still reachable on the live path |
| Date | 2026-08-18 |
| Method | `read_file` of the assigned file (126 lines, full) + `grep` on `Application/Ingestion` and `src` for dummy/seed/fake/mock + read of live callers (`DemoSeeder`, `LiveIngestHostedService`, `DependencyInjection`, `apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `FakeMt5BrokerConnector`/`DemoBrokerFactory`, `EfTradingStore`) |
| Product source modified | **No** |
| Verdict | **FAIL** |

---

## 1. What was read

`DealIngestionService.cs` is the whole `Application/Ingestion` folder (one file). It defines:

- `ITradingStore` (including `PersistDemoShadowAsync`)
- `DealIngestionService.SyncBrokerAsync` — live ingest entry
- `ReconstructionScoringService.RebuildTraderAsync` — reconstruct + score + shadow persist

The assigned file contains **no** hardcoded deal arrays, prices, tickets, or login tables. Ingest is entirely connector-driven:

```33:80:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        foreach (var group in groups)
            await _store.UpsertGroupAsync(brokerId, group, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        var insertedDeals = 0;
        foreach (var account in accounts)
            await _store.UpsertAccountAsync(brokerId, account, now, ct);
        // ... bulk or per-login GetDealsAsync → UpsertDealAsync ...
        foreach (var account in accounts.Take(200))
        {
            var positions = await connector.GetPositionsAsync(account.Login, ct);
            await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
        }

        return insertedDeals;
    }
```

`grep` of `D:/Prop/src/Application/Ingestion` for dummy/seed/fake/mock/sample/hardcoded/placeholder/synthetic: **2 hits**, both the `PersistDemoShadowAsync` name (interface + `RebuildTraderAsync` call). No seed literals in this file.

That is **not** an empty PASS. Dummy/seeded deals are still reachable on the live process path **through this type**, because live hosts still construct `DealIngestionService` with `DemoBrokerFactory` fakes and write into the same `ITradingStore` the hosted live ingest later reads.

---

## 2. Angle check — dummy still reaches live

### 2a. Assigned type has no live-vs-fake guard

`SyncBrokerAsync` accepts any `IMt5BrokerConnector` from `IBrokerRegistry.Get`. There is no type check, no `FakeMt5BrokerConnector` reject, no “demo seed only” flag, and no environment gate. Whatever the connector returns is upserted as production groups/accounts/deals/positions.

### 2b. Live hosts still seed via a second `DealIngestionService` on the same store

All three live entrypoints call `DemoSeeder.SeedAsync` after `EnsureCreatedAsync`, **before** `Run()`:

- `D:/Prop/apps/api/Program.cs` lines 84–93
- `D:/Prop/apps/mt5-worker/Program.cs` lines 11–19
- `D:/Prop/apps/fix-worker/Program.cs` lines 11–19

`DemoSeeder` constructs **this** class against `DemoBrokerFactory.CreateDefault()` (`FakeMt5BrokerConnector` pair) and the **same** DI `ITradingStore`:

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

Seed payload (`FakeMt5BrokerConnector.cs` `DemoBrokerFactory`):

- Groups: `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step`, `real\standard`
- Accounts: logins **10001, 10002, 10003, 99001** with fabricated balance/equity
- Deals: `BuildAchieverDeals` / `BuildStarwaveDeals` — synthetic XAUUSD round-trips (e.g. 10001 +153/−88/+163; 10002 martingale −200/−500/−1400; 99001 +40/+40/+30)

`DemoSeeder` only no-ops if `db.Brokers.AnyAsync`. First boot, empty Postgres, or the DI in-memory fallback `UseInMemoryDatabase("trader-intelligence-live")` (`DependencyInjection.cs` 24–26) **will** ingest those fakes through `DealIngestionService` into a database named live.

`BrokerCatalogSeed.EnsureAsync` (catalog-only, no deals) is **never** called from any Program.cs; live bootstrap is DemoSeeder, not catalog-only.

### 2c. Seeded rows are not purged; live ingest will score them

`EfTradingStore.UpsertDealAsync` is insert-if-absent on `(BrokerId, DealTicket)`. Seed tickets stay forever unless a later real deal collides on the same ticket.

`LiveIngestHostedService` then lists **all** store logins and rebuilds every one:

```42:48:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
```

`ListLoginsAsync` is `Mt5Accounts` for that broker — includes 10001–10003 / 99001 after seed. The hosted service’s “No dummy data will be substituted” log is only on **connector exception**; it does not delete or exclude already-seeded rows.

### 2d. Same-file scoring writes SHADOW artifacts from those deals

```101:125:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task RebuildTraderAsync(string brokerCode, long login, CancellationToken ct)
    {
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var deals = await _store.LoadDealsAsync(brokerId, brokerCode, login, ct);
        var trades = _reconstructor.Reconstruct(brokerCode, login, deals);
        await _store.ReplaceReconstructedAsync(brokerId, login, trades, ct);
        // ... BaselineScorer on completed XAU ...
        await _store.PersistDemoShadowAsync(brokerId, login, score.SuggestedState, completedXau, ct);
    }
```

`PersistDemoShadowAsync` is not a deal factory, but it **is** the live shadow persist: outbox `ScoreUpdate` always; if `TraderState.SHADOW`, `CopyIntent` rows with `Status = "SHADOW_ONLY"` plus `ShadowCopyEngine.SimulateEntry`. Seed login 10002 is an explicit averaging-down / lot-escalation book — exactly the pattern that can land SHADOW.

### 2e. Live HTTP resync still hardcodes the seed login set

`POST /api/ops/resync` (`apps/api/Program.cs` 73–81) injects the **DI** `DealIngestionService` (native connectors if passwords exist) but then rebuilds only `{ 10001, 10002, 10003, 99001 }` — the DemoBrokerFactory logins — over a 2026-01-01 window matching the seeder. Health still advertises `demo FakeMt5BrokerConnector — not live Manager` (`Program.cs` 28).

### 2f. What the DI live connector path *does* close

Honest counter-evidence (does **not** flip the verdict):

```33:34:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`LiveMt5Registration.CreateConnectors` returns only `NativeMt5BrokerConnector`. Fake connectors are not registered in DI. `LiveIngestHostedService` will not fabricate deals on a failed live pull.

That fail-closed path is **bypassed** by DemoSeeder’s `new DealIngestionService(fakeRegistry, liveStore)` on every empty-broker live start.

---

## 3. Evidence quotes

| Claim | Quote / measurement |
|---|---|
| File has no seed arrays | Full 126-line read; dummy/seed grep in `Application/Ingestion` = only `PersistDemoShadowAsync` |
| Ingest trusts any connector | `var connector = _registry.Get(brokerCode);` then `GetGroups/GetAccounts/GetDeals/GetPositions` → upsert |
| Live API seeds via this type | `var ingestion = new DealIngestionService(registry, store);` + `SyncBrokerAsync` in `DemoSeeder` |
| Live hosts call seeder | `await DemoSeeder.SeedAsync(...)` in api / mt5-worker / fix-worker `Program.cs` |
| Fake book contents | `new Mt5AccountDto(10001, @"demo\Maxmaster", ...)` + `BuildAchieverDeals` / `BuildStarwaveDeals` |
| Deals never replaced | `if (exists) return false;` then insert (`EfTradingStore.UpsertDealAsync`) |
| Live score walks seed logins | `ListLoginsAsync` → all `Mt5Accounts`; `RebuildTraderAsync` per login |
| Shadow persist on same file | `await _store.PersistDemoShadowAsync(...)` after score |
| HTTP still tied to seed logins | `foreach (var login in new long[] { 10001, 10002, 10003, 99001 })` on `/api/ops/resync` |
| In-memory DB named live | `UseInMemoryDatabase("trader-intelligence-live")` when connection missing/`<SECRET>` |

---

## 4. No-loss implication

**Contaminated live book, not a NewOrderSingle from this file.** Fabricated XAUUSD fills and demo logins persist beside (or instead of, on empty/in-memory “live” DB) real manager deals. `RebuildTraderAsync` will score those books and, for SHADOW, emit `CopyIntent` / simulated fills. That can:

- present fake equity, martingale, and completed-XAU counts as live trader intelligence
- leave seed accounts in the copy/shadow pipeline after a later real ingest (deals are insert-if-absent, not replaced)
- cause operators or a future `RealCopyExecutionEnabled` flip to size or select from a mixed dummy+live set

This file does not send FIX `NewOrderSingle` and does not place MT5 orders. The no-loss failure mode is **false live state and shadow intents derived from seeded deals that the live path still accepts**.

---

## 5. What this FAIL is not

- Not a claim that `DealIngestionService.cs` itself embeds ticket/price constants (it does not).
- Not a claim that DI registers `FakeMt5BrokerConnector` (it throws if real MT5 passwords are absent).
- Not a claim that `PersistDemoShadowAsync` manufactures dummy deals (it persists shadow/outbox from already-loaded trades).
- Not a PASS on “file has no dummy literals, so live path is clean” — the assigned angle is **reachability**, and live hosts still reach this type with `DemoBrokerFactory` data.
