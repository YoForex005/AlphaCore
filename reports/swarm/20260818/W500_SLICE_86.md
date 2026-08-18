# W500_SLICE_86

- **slot:** 86
- **file:** `D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (498 lines) via `read_file`; grep on this file for `score|Score|dashboard|Dashboard|account` (53 hits, all persist/list/upsert — no dashboard read, no score-gated account filter). Adjacent reads: `EfDashboardQueries.GetTradersAsync` (driver set), `ITradingStore` / `ReconstructionScoringService`, `LiveIngestHostedService` scoring loop, `TradersPage.tsx`.
- **verdict:** PASS

## Evidence quotes

`EfTradingStore` is the write-side `ITradingStore`. It has **no** dashboard query methods and **no** `Where` that requires a `TraderScore` row before an account exists or is listed.

Account upsert writes `Mt5Account` only. No `TraderScores.Add`, no skip when `LastScoredAt` is missing, no delete of unscored logins:

```53:83:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public async Task UpsertAccountAsync(Guid brokerId, Mt5AccountDto account, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Accounts.SingleOrDefaultAsync(
            a => a.BrokerId == brokerId && a.Login == account.Login, ct);
        if (existing is null)
        {
            _db.Mt5Accounts.Add(new Mt5Account
            {
                Id = Guid.NewGuid(),
                BrokerId = brokerId,
                Login = account.Login,
                GroupName = account.GroupName,
                Leverage = account.Leverage,
                Balance = account.Balance,
                Equity = account.Equity,
                Margin = account.Margin,
                MarginFree = account.MarginFree,
                Profit = account.Profit,
                LastSyncedAt = now
            });
        }
        else
        {
            existing.GroupName = account.GroupName;
            existing.Balance = account.Balance;
            existing.Equity = account.Equity;
            existing.LastSyncedAt = now
        }

        await _db.SaveChangesAsync(ct);
    }
```

Batch account ingest is the same split: `Mt5Accounts` keyed by login, no score join:

```382:424:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public async Task UpsertAccountsBatchAsync(Guid brokerId, IReadOnlyList<Mt5AccountDto> accounts, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).ToListAsync(ct);
        var byLogin = existing.ToDictionary(a => a.Login);
        var n = 0;
        foreach (var account in accounts)
        {
            if (byLogin.TryGetValue(account.Login, out var row))
            {
                row.GroupName = account.GroupName;
                // ... balances / last sync ...
            }
            else
            {
                _db.Mt5Accounts.Add(new Mt5Account
                {
                    Id = Guid.NewGuid(),
                    BrokerId = brokerId,
                    Login = account.Login,
                    // ... no TraderScore row created here ...
                    LastSyncedAt = now
                });
            }
```

Score persist is a **separate** upsert. Missing score ≠ missing account. First score **adds**; later scores update in place. History is append-only. Accounts are never removed here:

```215:248:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public async Task UpsertScoreAsync(TraderScore score, CancellationToken ct)
    {
        var existing = await _db.TraderScores.SingleOrDefaultAsync(
            s => s.BrokerId == score.BrokerId && s.Login == score.Login, ct);
        if (existing is null)
        {
            _db.TraderScores.Add(score);
        }
        else
        {
            existing.RiskScore = score.RiskScore;
            existing.BehaviorScore = score.BehaviorScore;
            existing.EarlyQualityScore = score.EarlyQualityScore;
            existing.CompletedXauTrades = score.CompletedXauTrades;
            existing.Martingale = score.Martingale;
            existing.AveragingDown = score.AveragingDown;
            existing.LotEscalation = score.LotEscalation;
            existing.CurrentState = score.CurrentState;
            existing.LastScoredAt = score.LastScoredAt;
        }

        _db.TraderScoreHistory.Add(new TraderScoreHistory
        {
            // ...
        });

        await _db.SaveChangesAsync(ct);
    }
```

The only login census this store exposes is **all** `Mt5Accounts` for the broker — not `TraderScores`:

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Live ingest uses that unfiltered list, then scores **every** login (`RebuildTraderAsync` always calls `UpsertScoreAsync`, including 0 XAU / `INSUFFICIENT_DATA`):

```66:76:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    // ...
                    foreach (var login in logins)
                    {
                        stoppingToken.ThrowIfCancellationRequested();
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
```

Dashboard listing is **not** in this file. Current `GetTradersAsync` is account-driven with a **left join** on scores. A missing `TraderScore` still emits a row (`INSUFFICIENT_DATA`, zeros, `LastSyncedAt`):

```96:117:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        var mapped = new List<TraderRowDto>();
        foreach (var account in accounts)
        {
            if (!brokers.TryGetValue(account.BrokerId, out var b))
                continue;
            scoreMap.TryGetValue((account.BrokerId, account.Login), out var s);
            pnlMap.TryGetValue((account.BrokerId, account.Login), out var pnl);
            mapped.Add(new TraderRowDto(
                b.Code,
                account.Login,
                account.GroupName,
                s?.CompletedXauTrades ?? 0,
                pnl,
                s?.EarlyQualityScore ?? 0,
                null,
                s?.RiskScore ?? 0,
                s?.Martingale ?? false,
                s?.AveragingDown ?? false,
                s?.LotEscalation ?? false,
                s?.CurrentState ?? TraderState.INSUFFICIENT_DATA,
                0,
                s?.LastScoredAt ?? account.LastSyncedAt));
        }
```

`TradersPage.tsx` maps the API array; it does not filter out null/zero scores (only flag-chip `.filter(Boolean)`).

This file does not contain:

- a dashboard `GetTraders` / inner-join-on-`TraderScores` query
- `Where(a => _db.TraderScores.Any(...))` or `Take` on the account list
- deletion of `Mt5Account` when score is missing
- a requirement that `UpsertAccount*` also insert a score

Nuance (out of this type, not a store hide): overview **state tiles** still count `TraderScores` only (`WATCH`/`SHADOW`/…). `TotalAccounts` is `Mt5Accounts.CountAsync`. That can make tiles look smaller than the account census; it does **not** drop unscored logins from `/api/traders` or from `ListLoginsAsync`.

## No-loss implication

`EfTradingStore` cannot hide an ingested login from the dashboard or from the scoring census: accounts persist without scores, `ListLoginsAsync` returns every `Mt5Account`, and the current traders API left-joins scores with `INSUFFICIENT_DATA` defaults. Worst case in this file is an account that exists but has no `TraderScore` until `RebuildTraderAsync` runs. `PersistDemoShadowAsync` only emits `SHADOW_ONLY` copy intents when `state == TraderState.SHADOW`; missing score cannot open, size, or send live destination orders. Slot 86 therefore has **no capital path** from “dashboard hiding unscored accounts,” and the hide itself is **not present** in the assigned store (or in the current account-driven traders query).

Not an empty-PASS: the assigned file was fully read (498 lines); the account/score split was inspected; the hide hypothesis is **false** on this write path.
