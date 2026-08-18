# W500_SLICE_36

- **slot:** 36
- **file:** `D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full file (342 lines) via `read_file`; grep on `D:\Prop\src` for `GetTradersAsync|TraderScores|ListLoginsAsync|UpsertScoreAsync` to locate the real list path
- **verdict:** PASS

## Evidence quotes

Assigned file was read in full (lines 1–342). `EfTradingStore` is an `ITradingStore` write/read adapter. It has **no** dashboard method, **no** `TraderRowDto` mapping, and **no** query that drops `Mt5Accounts` lacking a `TraderScore`.

Account persist is independent of scoring. `UpsertAccountAsync` writes `Mt5Accounts` only — it does not insert, require, or filter on `TraderScores`:

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
                // ...
                LastSyncedAt = now
            });
        }
        else
        {
            existing.GroupName = account.GroupName;
            existing.Balance = account.Balance;
            existing.Equity = account.Equity;
            existing.LastSyncedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
```

Score persist is a separate upsert. Missing score ⇒ insert; existing score ⇒ overwrite fields. Accounts are never deleted or marked hidden:

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
        // ... history row + SaveChanges
```

The only login enumeration in this type returns **every** `Mt5Account` for the broker. It does **not** join `TraderScores` and does **not** drop logins with `LastScoredAt` default / no row:

```339:341:D:/Prop/src/Infrastructure/Persistence/EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

This file does not contain:

- `GetTradersAsync` / `IDashboardQueries` / `TraderRowDto`
- `Where(s => s.LastScoredAt != default)` or any “has score” predicate on accounts
- a left-anti-join that removes unscored logins from a dashboard payload
- deletion of `Mt5Account` when a score is absent

The dashboard hide lives **outside** this slice, in the query layer that drives `/api/traders` from `TraderScores` only (accounts loaded as a join payload, not as the driver set):

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

`GetTraderAsync` / `GetTraderDetailAsync` reuse that list, so an ingested login with no score row is `null` on detail as well. Overview/groups still **count** all `Mt5Accounts`. That is `EfDashboardQueries`, not `EfTradingStore`.

Adjacent write path (not this file): `ReconstructionScoringService.RebuildTraderAsync` is the only product caller that creates a `TraderScore`. Live ingest then scores every `ListLoginsAsync` login. Until that runs, the store correctly holds accounts without scores; it does not hide them.

## No-loss implication

This store cannot omit an ingested login from a dashboard response because it never builds one. Worst case inside this type is an account row with no matching `TraderScore` until `UpsertScoreAsync` is invoked. That gap is observability on `/traders` (query-layer inner-join), not a sizing or send path.

`PersistDemoShadowAsync` only runs after scoring and only writes `SHADOW_ONLY` intents; it does not send destination orders. Unscored accounts have no `CurrentState` and therefore cannot be promoted to copy from this class. Slot 36 has **no capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (342/342 lines); the angle (dashboard hiding accounts that have no score yet) is absent from `EfTradingStore` by construction, not by skipped review. The hide is owned by `EfDashboardQueries.GetTradersAsync`.
