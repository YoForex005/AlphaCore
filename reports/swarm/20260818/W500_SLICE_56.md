# W500_SLICE_56

- **slot:** 56
- **file:** `D:/Prop/src/Infrastructure/Seeding/DemoSeeder.cs`
- **angle:** dashboard hiding accounts that have no score yet
- **read:** full assigned file (140/140 lines) via `read_file`; grep `TraderScore` / `GetTradersAsync` / `RebuildTraderAsync` / `10001` across `D:/Prop/src`; followed `DemoBrokerFactory.CreateDefault`, `DealIngestionService.SyncBrokerAsync` + `RebuildTraderAsync`, `EfDashboardQueries.GetTradersAsync` / `GetOverviewAsync`, `LiveIngestHostedService` contrast, `BaselineScorer` / `TraderStateMachine` for login 10003 (0 deals)
- **verdict:** FAIL

## File (assigned)

`DemoSeeder.SeedAsync` is the demo backfill. It is **not** the dashboard query, but it is the seed-side producer of who has a `TraderScore` (and therefore who can appear on `/traders`). Control flow:

1. Early-out if any `Brokers` row exists (`if (await db.Brokers.AnyAsync(ct)) return`).
2. Insert two brokers, one `XAUUSD` instrument, two FIX session rows (`Disconnected`), one destination quote, one kill switch.
3. `DemoBrokerFactory.CreateDefault()` → two `FakeMt5BrokerConnector` instances.
4. `DealIngestionService.SyncBrokerAsync` for Achiever and StarwaveFX → catalog + deals + positions for **every** factory account.
5. Score a **hardcoded login allowlist**, not `store.ListLoginsAsync`.

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

No `ListLoginsAsync`. No loop over ingested `Mt5Accounts`. No score row for any login outside that array.

## Evidence quotes

### 1. Ingest persists the full fake catalog; score does not walk it

`SyncBrokerAsync` always upserts **all** connector accounts before deals:

```47:48:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Factory accounts today:

```88:105:D:/Prop/src/Mt5/Connectors/FakeMt5BrokerConnector.cs
            accounts: new[]
            {
                new Mt5AccountDto(10001, @"demo\Maxmaster", 100, 10_000, 10_240, 200, 9_800, 240),
                new Mt5AccountDto(10002, @"demo\yo-2step", 100, 5_000, 4_820, 150, 4_670, -180),
                new Mt5AccountDto(10003, @"contest\yo-2step", 200, 25_000, 25_000, 0, 25_000, 0)
            },
            // ...
            accounts: new[]
            {
                new Mt5AccountDto(99001, @"real\standard", 100, 8_000, 8_110, 80, 7_920, 110)
            },
```

`BuildAchieverDeals` writes closed XAU only for **10001 and 10002**. **10003 has 0 deals / 0 positions.** Starwave deals are 99001 only.

The seeder still calls `RebuildTraderAsync` for 10003, so the *current* four-row catalog is fully scored. That coincidence is not a contract: the score set is a literal `long[]`, not the catalog.

### 2. Dashboard traders list is score-driven, not account-driven

```74:117:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        // ...
        var mapped = new List<TraderRowDto>();
        foreach (var s in scores)
        {
            if (!brokers.TryGetValue(s.BrokerId, out var b))
                continue;
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
            // ... TraderRowDto from score + optional account.GroupName
        }
        // filter broker/state, OrderByDescending EarlyScore
    }
```

`Mt5Accounts` is join payload (`GroupName`) only. An ingested login with no `TraderScore` never becomes a `TraderRowDto`.

```119:129:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }

    public async Task<TraderDetailDto?> GetTraderDetailAsync(string broker, long login, CancellationToken ct)
    {
        var header = await GetTraderAsync(broker, login, ct);
        if (header is null)
            return null;
```

Unscored login → `/api/traders/{broker}/{login}` and detail are **null**. Hidden, not “pending score.”

### 3. Overview KPI split: accounts counted, states scored-only

```16:34:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        var accounts = await _db.Mt5Accounts.CountAsync(ct);
        var brokers = await _db.Brokers.CountAsync(b => b.Enabled, ct);
        var scores = await _db.TraderScores.ToListAsync(ct);
        var xauTraders = scores.Count(s => s.CompletedXauTrades > 0);
        var three = scores.Count(s => s.CompletedXauTrades >= 3);
        // ...
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
```

`totalAccounts` can include logins that never appear on the traders page. There is **no** `INSUFFICIENT_DATA` / “unscored” bucket on overview. Unscored accounts inflate the account count and vanish from every state column.

### 4. Contrast: live ingest scores every persisted login; seeder does not

```42:48:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                    var brokerId = await store.ResolveBrokerIdAsync(connector.BrokerCode, stoppingToken);
                    var logins = await store.ListLoginsAsync(brokerId, stoppingToken);
                    var scored = 0;
                    foreach (var login in logins)
                    {
                        await scoring.RebuildTraderAsync(connector.BrokerCode, login, stoppingToken);
                        scored++;
                    }
```

`RebuildTraderAsync` always upserts a score, including 0 completed XAU:

```125:141:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        var completedXau = trades.Where(t => t.Completed && t.IsXauUsd).ToList();
        var score = _scorer.Score(completedXau);
        await _store.UpsertScoreAsync(new TraderScore
        {
            Id = Guid.NewGuid(),
            BrokerId = brokerId,
            Login = login,
            // ...
            CompletedXauTrades = score.Features.CompletedXauTrades,
            CurrentState = score.SuggestedState,
            LastScoredAt = DateTimeOffset.UtcNow
        }, ct);
```

Zero-trade book is not “no row”; it is `INSUFFICIENT_DATA`:

```191:192:D:/Prop/src/Domain/Scoring/BaselineScorer.cs
        if (features.CompletedXauTrades == 0)
            return TraderState.INSUFFICIENT_DATA;
```

That is why **10003 is visible today** (score row, `N=0`, `INSUFFICIENT_DATA`). Hiding happens only when `RebuildTraderAsync` is **never called**. DemoSeeder is the path that can skip the call.

### 5. Current demo coincidence vs the defect

| Login | Ingested (`Mt5Accounts`) | Deals in factory | Scored by seeder allowlist | Dashboard `/traders` |
|---|---|---|---|---|
| 10001 | yes | 3 XAU round-trips | yes | visible |
| 10002 | yes | 3 XAU (lot escalate) | yes | visible |
| 10003 | yes | **none** | yes (empty book) | visible as `INSUFFICIENT_DATA` |
| 99001 | yes | 3 XAU | yes | visible |
| any other catalog login | would be upserted by `SyncBrokerAsync` | n/a | **no** | **hidden** |

Today factory == allowlist (4/4). The hide is still encoded: add a fifth `Mt5AccountDto` to `CreateDefault` and do not touch the `long[]`, and `/traders` will omit it while overview `accounts` increments.

Same allowlist is copied on `/api/ops/resync` and `apps/mt5-worker` (see `A002_api_dummy_path.md`, `A005_dashboard_traders.md`). Demo seed is the template for “score these four, ignore the rest.”

### 6. Empty-PASS is not applicable

Assigned file was fully read (140/140 lines). The angle is **present**: seeder decides the score set with a hardcoded allowlist; dashboard then lists only that set. Empty PASS would be a skip.

No passwords / proxy auth / FIX passwords are in this file. `ManagerLogin` and FIX CompIDs are identifiers only; they are not quoted here as secrets.

## No-loss implication

`DemoSeeder` does **not** send FIX `NewOrderSingle` (`35=D`), Manager `DealerSend`, or any close/modify. Seeded FIX rows are `Disconnected` with last-error text that NewOrderSingle is off. Direct equity reduction from this file is **none**.

Indirect book / operator risk (the actual slice):

1. **Invisible source accounts.** `/traders` and trader detail are `TraderScores`-inner-join. An ingested `Mt5Account` with no score is omitted. Operators can believe the leaderboard is the full book while `mt5_accounts` holds extra logins.
2. **Overview lie by omission.** `accounts` = `COUNT(mt5_accounts)`; WATCH/SHADOW/LIVE/RISK_BLOCKED = scores only. Unscored logins are missing from every state, including `RISK_BLOCKED`. A martingale book that has not been rebuilt does not appear as blocked; it disappears.
3. **Allowlist reused on live-shaped hosts.** Worker/resync copy the same four logins. If those hosts ever see a real Manager catalog via `SyncBrokerAsync`, thousands of logins persist to `mt5_accounts` and stay hidden until someone scores them. Live host (`ListLoginsAsync`) does not have this hole; DemoSeeder does.
4. **10003 is the proof that “no trades” ≠ “hidden.”** Zero-deal accounts *should* show as `INSUFFICIENT_DATA`. The hide is “never scored,” not “not yet eligible.” DemoSeeder is the component that can skip the score write.

**Risk to capital:** observability / completeness, not an order-send path. Incomplete leaderboard can hide source risk (open XAU, averaging-down, lot escalate) until a score row exists. Do not enable real copy, and do not treat `/api/traders` as the Manager user census, while scoring is an allowlist and the dashboard is score-inner-join.

## Verdict rationale

FAIL: `DemoSeeder` ingests every factory account then scores only `{10001, 10002, 10003, 99001}`. `EfDashboardQueries.GetTradersAsync` iterates `TraderScores`, not `Mt5Accounts`, so any login outside that allowlist is hidden on the traders dashboard (detail returns null). Current factory happens to match the four logins, so the stock demo is not missing rows — the hide-if-unscored coupling is still in this file. Live ingest already walks `ListLoginsAsync`; the seeder does not. Slot 56 angle is present.
