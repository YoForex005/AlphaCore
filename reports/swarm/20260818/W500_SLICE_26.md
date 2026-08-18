# W500_SLICE_26

- **slot:** 26
- **file:** `D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (65 lines) via `read_file`; also read `ReconstructionScoringService.RebuildTraderAsync`, `ITradingStore.ListLoginsAsync` / `EfTradingStore.ListLoginsAsync`, `BaselineScorer` / `TraderStateMachine`, and `EfDashboardQueries.GetTradersAsync` / `GetOverviewAsync` (downstream hide surface — not this file)
- **verdict:** PASS

## Evidence quotes

`LiveIngestHostedService` is a one-shot `BackgroundService`. After a 2s delay it scopes DI, syncs each registered broker, lists **every** stored MT5 login, and calls `RebuildTraderAsync` for each. It does not query the dashboard, does not filter logins by prior score, and does not omit empty books.

```21:57:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        try
        {
            using var scope = _scopes.CreateScope();
            var registry = scope.ServiceProvider.GetRequiredService<IBrokerRegistry>();
            var ingest = scope.ServiceProvider.GetRequiredService<DealIngestionService>();
            var scoring = scope.ServiceProvider.GetRequiredService<ReconstructionScoringService>();
            var store = scope.ServiceProvider.GetRequiredService<ITradingStore>();
            // ...
            foreach (var connector in registry.All())
            {
                _log.LogInformation("Live ingest starting for {Broker}", connector.BrokerCode);
                try
                {
                    await connector.ConnectAsync(stoppingToken);
                    var n = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                    }

                    _log.LogInformation("{Broker} ingest done. dealsInserted={Deals} accounts={Accounts} scored={Scored}",
                        connector.BrokerCode, n, logins.Count, scored);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "{Broker} live ingest failed. No dummy data will be substituted.", connector.BrokerCode);
                }
            }
        }
```

Login census is **all `Mt5Accounts` for the broker**, not “already scored” / “has XAU” / “has deals”:

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

`RebuildTraderAsync` **always** persists a `TraderScore` after reconstruction, including N=0 completed XAU (empty account). That is the opposite of “leave them scoreless so the dashboard can drop them”:

```100:123:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
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
            // ...
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

Empty book → `INSUFFICIENT_DATA`, not a skipped row (`TraderStateMachine.FromBaseline` when `CompletedXauTrades == 0`).

This file does **not** contain:

- `IDashboardQueries` / `GetTradersAsync` / `TraderRowDto`
- any `Where` that drops logins without `TraderScores`
- a leaderboard hide / unpublished-score filter
- dummy / synthetic scores (catch text: “No dummy data will be substituted.”)

### Downstream hide (out of this slice file — recorded so the angle is not hand-waved)

`EfDashboardQueries.GetTradersAsync` materializes **only** `TraderScores`. `Mt5Accounts` are joined for `GroupName`, not used as the row source. `GetTraderAsync` / `GetTraderDetailAsync` therefore return `null` when no score row exists:

```74:107:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
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
            // ... TraderRowDto from score
        }
```

Overview `TotalAccounts` counts `Mt5Accounts`; state buckets count scores only. That split is the dashboard, not this host.

### Residual (does not flip this file to FAIL)

- ExecuteAsync is **not** a loop: accounts upserted after the single pass are never scored here.
- The scoring `foreach` has **no per-login try/catch**. One `RebuildTraderAsync` throw aborts remaining logins on that broker; those remain scoreless and are then omitted by `GetTradersAsync`.
- Until the 2s delay + sync + score pass finishes, the scores-only leaderboard is empty even if accounts already exist.

Those are visibility windows on an incomplete run. The assigned type’s success path scores every listed login (including no-deal accounts) so they **do** appear as `INSUFFICIENT_DATA` rather than being hidden by this class.

Empty-PASS justification: the assigned file was fully read; it does not implement dashboard hiding. It enumerates all stored logins and writes a score for each. Hide-if-no-`trader_scores` lives in `EfDashboardQueries`, outside slot 26.

## No-loss implication

`LiveIngestHostedService` only connects, syncs deals/accounts, and rebuilds reconstruction + scores. It never sends FIX `NewOrderSingle`, never sizes a live copy, and never flips `CanPromoteToLive` (domain always `false`). Ingest/score failure is fail-closed: log + no dummy tape. An account with no `TraderScore` cannot appear as `LIVE` / `LIVE_CANDIDATE` on the trader list and cannot be selected for copy from this path. Hiding a not-yet-scored login is **visibility**, not capital at risk. Slot 26 has **no live capital-loss path** in the assigned file.
