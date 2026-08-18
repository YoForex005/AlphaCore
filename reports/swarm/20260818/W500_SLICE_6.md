# W500_SLICE_6

- **slot:** 6
- **file:** `D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (140 lines) via `read_file`; cross-read `FakeMt5BrokerConnector.DemoBrokerFactory`, `DealIngestionService` / `ReconstructionScoringService`, `EfDashboardQueries.GetTradersAsync` / `GetOverviewAsync`, `BaselineScorer` / `TraderStateMachine`, `LiveIngestHostedService`, `apps/api/Program.cs` `/api/ops/resync`, `EfTradingStore.ListLoginsAsync`; grep `RebuildTraderAsync` / `GetTradersAsync` / `TraderScores`
- **verdict:** PASS

## Evidence quotes

`DemoSeeder.SeedAsync` is a first-empty-broker bootstrap: catalog rows, Fake-MT5 ingest, then a score rebuild. It does not implement the dashboard query and does not filter `Mt5Accounts` by score.

```16:23:D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs
    public static async Task SeedAsync(
        TraderDbContext db,
        ITradingStore store,
        ReconstructionScoringService scoring,
        CancellationToken ct)
    {
        if (await db.Brokers.AnyAsync(ct))
            return;
```

Ingest walks the Fake connectors (all factory accounts), then the seeder scores a four-login list that is **identical** to `DemoBrokerFactory.CreateDefault()`:

```125:138:D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs
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

Factory accounts (the only rows `SyncBrokerAsync` can upsert on this path):

```88:105:D:/Prop/src/Mt5/Connectors/FakeMt5BrokerConnector.cs
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            deals: BuildAchieverDeals(t0));
        // ...
            accounts: new[]
            {
                new Mt5AccountDto(99001, @"real\standard", 100, 8_000, 8_110, 80, 7_920, 110)
            },
```

`BuildAchieverDeals` emits tape only for 10001 and 10002. Login **10003 is still in the seeder score loop**. `ReconstructionScoringService.RebuildTraderAsync` always `UpsertScoreAsync`s, including `N=0`:

```107:123:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            RiskScore = score.RiskScore,
            BehaviorScore = score.BehaviorScore,
            EarlyQualityScore = score.EarlyQualityScore,
            CompletedXauTrades = score.Features.CompletedXauTrades,
            Martingale = score.Features.Martingale,
            AveragingDown = score.AveragingDown,
            LotEscalation = score.LotEscalation,
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

Empty-book state is `INSUFFICIENT_DATA`, not “no row”:

```191:192:D:/Prop/src/Domain/Scoring/BaselineScorer.cs
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
```

The hide (score-inner, not account-left) lives **outside this slice**, in `EfDashboardQueries.GetTradersAsync`:

```74:91:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        foreach (var s in scores)
        {
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
```

Overview `TotalAccounts` counts `Mt5Accounts`; trader list / detail / state tiles walk `TraderScores`. After this seeder those sets are the same four logins. C23 measured `DASH_INSUFFICIENT_LOGINS=10003` — the zero-deal account is **on** the traders list, not hidden.

This file does not contain:

- `GetTradersAsync` / `TraderRowDto` mapping / a `Where(score != null)` dashboard filter
- `ListLoginsAsync` (it hard-codes the four factory logins instead)
- any omit-score-when-`N<3` branch that would drop 10003 from `trader_scores`

Residual (not a current hide): the score loop is a literal `{10001,10002,10003,99001}`, copied again in `apps/api/Program.cs` `/api/ops/resync` and `apps/mt5-worker/Worker.cs`. `LiveIngestHostedService` is the only path that scores `store.ListLoginsAsync`. If the Fake factory later added a fifth account, seed/resync would leave it unscored and **then** `GetTradersAsync` would omit it. That drift is not present in the files as read.

## No-loss implication

`DemoSeeder` cannot hide a live book from risk, cannot size, flatten, or copy, and cannot emit FIX `NewOrderSingle`. After a first-run seed every ingested demo login has a `TraderScore` row (10003 = `INSUFFICIENT_DATA`, not missing). Dashboard hide of **unscored** accounts is a query join in `EfDashboardQueries`, not this file. Slot 6 therefore has **no current “no-score-yet” hide in the assigned seeder**, and **no capital-loss path**.

Worst residual: operator blindness if a later account is ingested but not added to the hardcoded rebuild list — observability only; still no send.

Empty-PASS justification: the assigned file was fully read (140/140 lines); the angle (dashboard hiding accounts that have no score yet) is not implemented here. The seeder scores all four current factory logins, including the empty-deal fixture.
