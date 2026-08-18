# C36 — `EfDashboardQueries` remaining query / performance issues

| Field | Value |
|---|---|
| Agent | C36 (senior engineer, dashboard query / perf review only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (read-only pass; **no** `EXPLAIN ANALYZE`; **no** live Postgres) |
| Workspace | `D:\Prop` |
| Assigned question | Read `EfDashboardQueries`. Remaining query issues? Write this report. |
| Subject | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| Port | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` `IDashboardQueries` |
| Host | `D:\Prop\apps\api\Program.cs` (`/api/overview` … `/api/risk`, plus adjacent `/api/trades`) |
| Model | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| Law | Architecture v2 §§10, 15, 47–53; A91–A95 (DTO / query plans); A98 (index contract); A26 pagination |
| Siblings | C04 (API), C06 (keys), A98 (indexes), B39 (`mlProbability=null`) |
| Product source modified | **No.** This report is the only required write. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict

**Yes — remaining query issues. The class is a demo materializer, not a 5,000-account dashboard read path.**

`EfDashboardQueries` compiles and is wired (`AddScoped<IDashboardQueries, EfDashboardQueries>`). On the **demo seed** (2 brokers, 4 groups, 4 accounts, 4 scores, ~9 reconstructed trades) every endpoint is cheap by accident. Against the A98 planning envelope (~5,000 logins, 0.1M–0.8M reconstructed rows, unbounded quote / shadow / risk-decision history) the same LINQ is **`EXISTS_NEEDS_REFACTOR`** and, for the leaderboard + trader-detail + groups census, **`UNSAFE` as a Postgres access path**.

| Question | Answer |
|---|---|
| Are there remaining query issues? | **Yes.** N+1 on brokers/groups; full-table loads; in-memory filter/sort; no pagination; missing indexes on the hot quote/shadow/reject paths; detail reuses the full leaderboard. |
| Was this proven with `EXPLAIN`? | **No.** Default DI is **EF InMemory** when `ConnectionStrings:TraderIntelligence` is empty or contains `<SECRET>` (`DependencyInjection.cs` 19–28). InMemory cannot prove btree, partials, or seq-scan cost. |
| Does demo “feel fast”? | **Yes, and that is a lie.** Seed size hides every issue below. |
| Are there dashboard query tests? | **None.** `grep` of `D:\Prop\tests` for `EfDashboardQueries` / `GetTradersAsync` / `GetOverviewAsync` = **0**. |
| Class of the current query layer | **`EXISTS_NEEDS_REFACTOR`** (demo) / **`FAIL` as a 5k read plane** |

Honest one-liner: **seven methods, ~28–32 SQL statements on a cold dashboard paint, three of them `SELECT *` of whole tables, two N+1 loops, one detail route that reloads the entire leaderboard. Do not call this indexed.**

---

## 1. Method (read-only)

1. Read `EfDashboardQueries.cs` in full (168 lines). Re-hash this pass (`Get-FileHash SHA256`).
2. Read `IDashboardQueries` + six DTOs, `TraderDbContext` fluent indexes, `DependencyInjection` tracking/provider, `apps/api/Program.cs` route map, `DemoSeeder` + `DemoBrokerFactory` row counts, `apps/web/src/api/hooks.ts` poll intervals.
3. Cross-check A91 §11 query plan, A92 filters/pagination, A93 detail GET, A94 §8.1 FIX mapping, A95 risk stub, A98 index families, C06 store/API keys.
4. Grep `AsNoTracking`, `QueryTrackingBehavior`, `ToListAsync`, `CountAsync`, `SumAsync`, `GroupBy` under `src/Infrastructure`.
5. **Did not** start the API, **did not** open Postgres, **did not** edit product source.

---

## 2. File identity (re-measured)

| Field | Value |
|---|---|
| Path | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| Bytes | **7407** |
| Lines | **168** |
| SHA-256 | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` |
| LastWriteUtc | `2026-08-18T07:44:18Z` |
| Matches B39 hash | **Yes** (same 7407 / same SHA). File has not moved since that census. |
| `AsNoTracking` sites | **4**, all inside `GetTradersAsync` (lines 76–79). Other methods track. |
| `QueryTrackingBehavior.NoTracking` on `AddDbContext` | **Absent** |
| `HasDatabaseName` / snake_case columns | **Absent** (C06 / A98). Fluent indexes are **intent**, not applied DDL. |
| Migrations | `src/Infrastructure/Persistence/Migrations/` **empty**. `Configurations/` **empty**. |

---

## 3. Demo working set (why nothing hurts yet)

From `DemoBrokerFactory.CreateDefault` + `DemoSeeder` (4 scored logins):

| Set | Demo rows | A98 planning |
|---|---:|---:|
| `brokers` | 2 | 2 |
| `mt5_groups` | 4 | tens–hundreds |
| `mt5_accounts` | 4 | 5,000–8,000 |
| `trader_scores` | 4 | ~5,000 |
| `reconstructed_trades` | ~9 completed XAU | 0.1M–0.8M |
| `destination_quotes` | 1 | unbounded (tick tape) |
| `shadow_orders` | 0 | grows with every shadow fill |
| `risk_decisions` | 0 | grows with every copy intent |
| `fix_sessions` | 2 | 2 (today; unique on `Qualifier` only) |
| `kill_switches` | 1 | 1 |

At this size every `ToListAsync` is a few KB. **None of the remaining issues can be closed by “it was fine in demo.”**

---

## 4. SQL census (every method)

Round-trips are **sequential `await`s on one scoped `TraderDbContext`**. That is the *correct* concurrency shape (EF Core contexts are not thread-safe; `Task.WhenAll` on `_db` would be a new bug). The remaining issue is **too many statements and too much payload**, not missing parallelism.

### 4.1 `GetOverviewAsync` — **7** statements

```14:24:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<OverviewDto> GetOverviewAsync(CancellationToken ct)
    {
        var accounts = await _db.Mt5Accounts.CountAsync(ct);
        var brokers = await _db.Brokers.CountAsync(b => b.Enabled, ct);
        var scores = await _db.TraderScores.ToListAsync(ct);
        var xauTraders = scores.Count(s => s.CompletedXauTrades > 0);
        var three = scores.Count(s => s.CompletedXauTrades >= 3);
        var shadowPnl = await _db.ShadowOrders.SumAsync(s => (decimal?)s.SourceVsShadowSlippage, ct) ?? 0;
        var quote = await _db.FixSessionStates.SingleOrDefaultAsync(s => s.Qualifier == FixSessionQualifier.Quote, ct);
        var trade = await _db.FixSessionStates.SingleOrDefaultAsync(s => s.Qualifier == FixSessionQualifier.Trade, ct);
```

| # | SQL shape | Tracking | Index today | Remaining issue |
|---|---|---|---|---|
| 1 | `COUNT(*) FROM mt5_accounts` | n/a | PK heap. A98: seq scan of 5k is **OK** | None at 5k |
| 2 | `COUNT(*) FROM brokers WHERE Enabled` | n/a | tiny | **Wrong predicate** (A91: connected + heartbeat, not `Enabled`) |
| 3 | `SELECT * FROM trader_scores` | **Yes** | unique `(BrokerId, Login)` unused | **Full table + tracked.** Counts belong in SQL `COUNT(*) FILTER` / `GROUP BY CurrentState` |
| 4 | `SUM(SourceVsShadowSlippage) FROM shadow_orders` | n/a | **no index at all** on `shadow_orders` | Unbounded seq scan; **not** A24 XAU lifetime book; not per-day |
| 5–6 | `SingleOrDefault` by `Qualifier` | Yes | unique `Qualifier` | Fine for 2 rows. Wrong unique shape (C06: should be `(venue_id, qualifier)`) |
| 7 | *(in-memory counts of 5 states)* | — | — | Extra CPU only after the full load |

Then the DTO **does not query** destination real P&L, XAU gross/net, or real-copy flag — it emits **`0, 0, 0, …, false`** (lines 36–42). Those are remaining *query omissions*, not extra SQL.

A91 §11 wants **one snapshot / one round-trip**. Current: 7 statements + 3 hardcoded zeros + health derived from enum membership (A101: greenwashes the seeder).

**Class:** `EXISTS_NEEDS_REFACTOR`.

### 4.2 `GetBrokersAsync` — **1 + 2N** (demo **5**; 2 brokers)

```47:54:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var brokers = await _db.Brokers.OrderBy(b => b.Code).ToListAsync(ct);
        var result = new List<BrokerStatusDto>();
        foreach (var b in brokers)
        {
            var groups = await _db.Mt5Groups.CountAsync(g => g.BrokerId == b.Id, ct);
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == b.Id, ct);
            result.Add(new BrokerStatusDto(b.Code, b.DisplayName, b.Server, MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
        }
```

| Issue | Detail |
|---|---|
| N+1 | Two `CountAsync` **per broker**. Should be two `GroupBy(BrokerId)` queries (or one SQL with conditional counts). |
| Tracking | Full `Broker` entities tracked (proxy fields, `ManagerLogin` raw in memory). |
| Fabricated columns | `Connected = true` literal; `LastEventAt = DateTimeOffset.UtcNow`. **No query** of connector / checkpoint / last deal. B04 already called this a lie. |
| Mask | `MaskLogin` is `login / 100 * 100` (2027 → 2000). Not a query bug; still a remaining DTO bug (B10). |

Broker count stays small (2). N+1 here is **P2** for this lab, **P1** if a later venue table grows.

**Class:** `EXISTS_NEEDS_REFACTOR`.

### 4.3 `GetGroupsAsync` — **2 + G** (demo **6**; 4 groups)

```61:68:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var groups = await _db.Mt5Groups.ToListAsync(ct);
        var brokers = await _db.Brokers.ToDictionaryAsync(b => b.Id, ct);
        ...
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == g.BrokerId && a.GroupName == g.Name, ct);
```

| Issue | Detail |
|---|---|
| N+1 **P0** | One `COUNT(*)` per group on `(BrokerId, GroupName)`. A98 §4.3 already named this. At 80 groups this is 82 round-trips. |
| Missing index | A98 `mt5_accounts_group_ix (broker_id, group_name)` is **not** in `TraderDbContext`. Each count is a seq scan of `mt5_accounts` even after the N+1 is fixed to one `GROUP BY`. |
| Tracking | All groups + all brokers tracked. |
| No projection | Loads `Currency`, `MarginCall`, `Company`, … then maps 7 DTO fields. |
| Filter | Returns **every** `mt5_groups` row (including unmapped). That is **correct** vs A39/A40 (do not hide unmapped). Not a perf bug. |

The index makes each count cheap; **it does not remove the N+1**. Fix is one grouped query:

```sql
SELECT broker_id, group_name, COUNT(*)
FROM mt5_accounts
GROUP BY 1, 2;
```

then join to groups in process (or a single SQL join).

**Class:** `UNSAFE` at 5k / many groups.

### 4.4 `GetTradersAsync` — **4** full-set reads, then in-memory filter/sort

```74:116:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<IReadOnlyList<TraderRowDto>> GetTradersAsync(string? broker, string? state, CancellationToken ct)
    {
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        ...
            var account = accounts.FirstOrDefault(a => a.BrokerId == s.BrokerId && a.Login == s.Login);
        ...
        if (!string.IsNullOrWhiteSpace(broker))
            filtered = filtered.Where(t => t.Broker.Equals(broker, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(state) && Enum.TryParse<TraderState>(state, true, out var st))
            filtered = filtered.Where(t => t.State == st);

        return filtered.OrderByDescending(t => t.EarlyScore).ToList();
```

This is the **worst remaining query**.

| # | Remaining issue | Why it fails at 5k |
|---|---|---|
| Q1 | `SELECT *` all `trader_scores` | 5k rows every leaderboard hit. Filters `broker` / `state` applied **after** materialization. |
| Q2 | `SELECT *` all `mt5_accounts` | Same 5k, only to read `GroupName`. Should be a join / lookup keyed by `(BrokerId, Login)`. |
| Q3 | `GROUP BY (BrokerId, Login) WHERE Completed` over **all** reconstructed trades | A98 §6.5 says this hash-agg is acceptable **if** measured. Missing `canonical_symbol = 'XAUUSD'` (A92 L14 / §15). Will mix non-XAU into `NetSourcePnl` the moment a second symbol exists. No partial `completed` index. |
| Q4 | `accounts.FirstOrDefault` per score | **O(\|scores\| × \|accounts\|)**. At 5k × 5k = **25 million** comparisons. Must be `ToDictionary((BrokerId, Login))`. |
| Q5 | No `page` / `pageSize` | A92 default 50 / max 200. API returns the **entire** mapped list. React `useTraders` has no limit. |
| Q6 | Sort is `EarlyScore DESC` only | A92 default is a multi-key chain with `NULLS LAST`. Unscored `0` sorts as a real score (C23: login 10003 publishes `EarlyScore=40` on empty). |
| Q7 | 6 of 8 §50 filters **not queried** | No `group`, score range, risk range, trade-count range, martingale / averaging / lotEscalation, `scoredFrom`/`scoredTo`, `q`, `enabledForAnalysis`. |
| Q8 | `state` parse is `true` (ignore-case) | A92 §5.4 tokens are **case-sensitive** `WATCH` etc. Bad token is **silently ignored** (no 400). |
| Q9 | `ShadowPnl` column is literal `0` | A92 L15 requires destination-quote marked shadow P&L **per** `(broker, login)`. No query. |
| Q10 | `MlProbability` literal `null` | **Correct** vs A52 / B39. Not a remaining issue. |
| Q11 | Scores with unknown `BrokerId` are `continue`d | Silent drop. No 5xx. Tiny integrity hole. |

`AsNoTracking` on this path is the **only** tracking hygiene in the class. Keep it.

**Class:** `UNSAFE` as a 5k leaderboard.

### 4.5 `GetTraderAsync` — **reuses Q4 in full** (P0)

```119:123:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }
```

| Remaining issue | Detail |
|---|---|
| Full leaderboard for one row | Four table-wide queries + in-memory join + filter by `login`. The unique `(BrokerId, Login)` on `trader_scores` is **never used**. |
| Wrong payload | A93 requires a detail DTO with first-3 XAU block, score timeline, flags, shadow/live books. This returns a **leaderboard row**. |
| Login-only after broker-scoped list | After `GetTradersAsync(broker)` the list is broker-filtered; then `Login == login`. That is §10-safer than `/api/trades`, but still not `WHERE brokers.code = $1 AND scores.login = $2`. |
| Amplification | React `useTraderDetail` calls this on every trader page. Cost = full `/api/traders`. |

Keyed shape that should exist later (not implemented here):

```sql
SELECT ...
FROM trader_scores s
JOIN brokers b ON b.id = s.broker_id
LEFT JOIN mt5_accounts a ON a.broker_id = s.broker_id AND a.login = s.login
WHERE b.code = $1 AND s.login = $2;
```

plus a **bounded** first-3:

```sql
SELECT * FROM reconstructed_trades
WHERE broker_id = $1 AND login = $2 AND completed AND canonical_symbol = 'XAUUSD'
ORDER BY closed_at
LIMIT 3;
```

A98 already specified `reconstructed_trades_xau_completed_ix` for that. It is **not** in fluent API.

**Class:** `UNSAFE`.

### 4.6 `GetFixSessionsAsync` — **2** statements

```127:128:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var sessions = await _db.FixSessionStates.OrderBy(s => s.Qualifier).ToListAsync(ct);
        var quote = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
```

| Remaining issue | Detail |
|---|---|
| `destination_quotes` latest row | **No index on `ReceivedAt`.** Table has PK only (`TraderDbContext` 149–153). Quote tape is the table most likely to grow without bound. Every 5 s poll (`hooks.ts` `refetchInterval: 5000`) becomes `ORDER BY "ReceivedAt" DESC LIMIT 1` on a fat heap. |
| No symbol / venue predicate | Latest row **of any instrument**. Not “mapped XAUUSD”. |
| Quote cloned onto every session | A94 §8.1: TRADE card must not carry bid/ask. One extra query used **incorrectly** (correctness + wasted payload). |
| Tracking | Sessions + the quote entity tracked. |
| Session table size | 2 rows; `OrderBy Qualifier` is fine. |

This is the **highest-growth** remaining scan. A later coder who appends every tick without prune + without `(canonical_symbol, received_at DESC)` (or a 1-row current-quote table) will take the FIX page down first.

**Class:** `UNSAFE` once the tape is real; `EXISTS_NEEDS_REFACTOR` on demo (1 row).

### 4.7 `GetRiskAsync` — **2** statements

```151:157:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var ks = await _db.KillSwitches.OrderByDescending(k => k.UpdatedAt).FirstOrDefaultAsync(ct);
        var rejects = await _db.RiskDecisions
            .Where(r => r.Outcome != RiskDecisionOutcome.Approve)
            .OrderByDescending(r => r.DecidedAt)
            .Take(20)
            .Select(r => r.Reason)
            .ToListAsync(ct);
```

| Remaining issue | Detail |
|---|---|
| Reject index | Fluent index is `CopyIntentId` only. Predicate is `Outcome != Approve ORDER BY DecidedAt DESC LIMIT 20`. **Cannot use** that index. Seq scan + sort as `risk_decisions` grows. Need `(decided_at DESC) WHERE outcome <> approve` or `(outcome, decided_at DESC)`. |
| Kill-switch table | 1 row; `ORDER BY UpdatedAt` is noise. A95 wants two independent controls, not `Mode.ToString()`. |
| Missing queries | Daily P&L, drawdown, XAU long/short/net are **literals `0`**. A95 classed this **UNSAFE / STUB**. Those are remaining omitted queries (destination positions / fills — tables mostly **MISSING**, B19). |
| Reasons only | `Select(r => r.Reason)` drops identity (`CopyIntentId`, login, broker). A95 requires `rejectedIntents[]` with identity. Cheap query, wrong projection. |
| Poll | Same 5 s interval as FIX. Cheap **until** rejects accumulate. |

**Class:** `EXISTS_NEEDS_REFACTOR` (shape) + `MISSING` (exposure / daily P&L sources).

---

## 5. Adjacent query that is **not** in this class (still a dashboard remaining issue)

`GET /api/trades` lives in `apps/api/Program.cs` 63–70, not in `EfDashboardQueries`:

```csharp
if (login.HasValue)
    query = query.Where(t => t.Login == login.Value);
var rows = await ... ToListAsync(query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
return rows; // raw ReconstructedTrade entities
```

| Remaining issue | Law |
|---|---|
| `broker` query-string **declared and ignored** (C04 / C06) | §10 |
| Filter is `Login` alone — **cannot use** any legal compound index | A98 §6.2 |
| Returns EF entities (allow-list fail) | §55 / A26 |
| `Take(200)` without `canonical_symbol` / `completed` default | A63 explorer |

React `useTrades` calls this with **no** `login` → last 200 of the **entire** table by `OpenedAt`. Index on `(BrokerId, Login, PositionId, OpenedAt)` cannot serve a global `ORDER BY OpenedAt`.

Not owned by `EfDashboardQueries`, but it is the eighth dashboard read and it is worse than `GetTradersAsync` on identity.

---

## 6. Tracking, payload, and context hygiene

| Check | Measured |
|---|---|
| Global `UseQueryTrackingBehavior(NoTracking)` | **No** |
| `AsNoTracking` | Only `GetTradersAsync` |
| `Select` projections (DTO-shaped SQL) | Only risk `Reason` list + PnL group key |
| `EF.CompileAsyncQuery` | **0** |
| Redis snapshot (A91 / A99) | **Not used.** `StackExchange.Redis` is referenced; dashboard never calls it. |
| `CommandTimeout` | Default |
| Split / MARS / batch | Default Npgsql (one result set per await) |
| CancellationToken | **Passed on every EF call** — keep |
| Same-context `Task.WhenAll` | **Not present** (good). Do **not** “fix” sequential awaits that way. |

Tracked `ToList` on overview scores + groups + brokers + FIX sessions dirties the scoped context for the rest of the request (e.g. if `/api/ops/resync` shared a context it would snapshot-conflict; today routes are separate scopes so this is **extra allocations**, not a write bug).

`GetBrokersAsync` / `GetGroupsAsync` load full entities including `Broker.ManagerLogin` (masked only on the DTO) and unused group margin fields.

---

## 7. Index coverage vs these exact predicates

Source of fluent map: `TraderDbContext.OnModelCreating`. A98 names are **not** on disk.

| Predicate used by `EfDashboardQueries` | Fluent index? | A98 / needed | Hit? |
|---|---|---|---|
| `mt5_accounts` `COUNT(*)` | PK | none required at 5k | seq OK |
| `mt5_accounts` `COUNT` by `BrokerId` | prefix of `(BrokerId, Login)` UK | yes (UK prefix) | **would hit** if Postgres |
| `mt5_accounts` `COUNT` by `(BrokerId, GroupName)` | **No** | `mt5_accounts_group_ix` | **seq scan** |
| `mt5_groups` `(BrokerId, Name)` UK | Yes | identity | unused by dashboard (load-all) |
| `trader_scores` `(BrokerId, Login)` UK | Yes | leaderboard join / **detail lookup** | **unused** (full scan + `GetTrader` reuse) |
| `trader_scores` `GROUP BY CurrentState` | **No** | cheap at 5k even as seq | full `SELECT *` instead |
| `reconstructed_trades` `WHERE Completed GROUP BY (BrokerId, Login)` | 4-col non-unique `(BrokerId, Login, PositionId, OpenedAt)` | A98: UK prefix / login_closed; **no** extra covering INCLUDE until measured | prefix usable; `Completed` not leading; **no XAU filter** |
| `reconstructed_trades` first-3 XAU | **No** | `reconstructed_trades_xau_completed_ix` | query **does not exist** |
| `shadow_orders` `SUM(slippage)` | **No indexes** | lifetime book should not be this table raw (A24 `shadow_performance`) | **seq scan** |
| `destination_quotes` `ORDER BY ReceivedAt DESC LIMIT 1` | **No** | `(canonical_symbol, received_at DESC)` or a 1-row current table | **seq scan + sort** |
| `risk_decisions` `Outcome != Approve ORDER BY DecidedAt DESC LIMIT 20` | `CopyIntentId` only | reject partial / `(outcome, decided_at)` | **seq scan + sort** |
| `kill_switches` latest `UpdatedAt` | PK only | 1-row table | OK |
| `fix_sessions` by `Qualifier` | unique `Qualifier` | wrong unique (no venue) | hits; query only works because unique is **global** (C06) |

A98 already said: *"`GetTradersAsync` loads all accounts into memory. At 5k that is acceptable; do not add indexes to paper over the full table read."* That sentence is about **not** inventing a covering index to excuse `ToList` of accounts. It is **not** a pass on the leaderboard. Pagination + SQL filters remain required (A92).

---

## 8. Round-trip budget (one React paint)

Hooks: `useOverview`, `useBrokers`, `useGroups`, `useTraders` have **no** `refetchInterval` (default focus-refetch). `useFixSessions` + `useRiskStatus` poll **every 5 s**. `useHealth` every 10 s (not this class). `useTraderDetail` = full leaderboard.

| User action | Methods | SQL (demo) | SQL (5k / ~80 groups / 2 brokers) |
|---|---|---:|---:|
| Overview page | `GetOverview` | 7 | 7 + 5k-row `SELECT *` scores + full `SUM(shadow)` |
| Brokers page | `GetBrokers` | 5 | 5 |
| Groups page | `GetGroups` | 6 | **82** |
| Traders page | `GetTraders` | 4 | 4 **full-table** + 25M in-memory joins + unbounded JSON |
| Trader detail | `GetTrader` → `GetTraders` | 4 | **same as traders page** |
| FIX page (every 5 s) | `GetFixSessions` | 2 | 2, second is **latest-quote sort** |
| Risk page (every 5 s) | `GetRisk` | 2 | 2, second is reject seq-scan |

A dashboard left open on FIX+Risk is **~48 queries/minute** before any click, and the quote query is the one that gets more expensive with time.

---

## 9. Remaining issues ranked

### P0 — will not survive first useful / 5k

| ID | Issue | Where | Fix direction (later coder; **not done here**) |
|---|---|---|---|
| P0-1 | `GetTraderAsync` = full `GetTradersAsync` | L119–123 | Keyed `(broker_code, login)` lookup. New A93 payload. |
| P0-2 | Leaderboard loads all scores + all accounts; filters/sorts in memory; no page | L76–116 | SQL `WHERE` for A92 filters; `ORDER BY` + `OFFSET/LIMIT`; join group name. |
| P0-3 | Groups N+1 `CountAsync` | L67 | One `GROUP BY (BrokerId, GroupName)`. |
| P0-4 | `destination_quotes` latest without index / prune | L128 | Current-quote table **or** `(canonical_symbol, received_at DESC)` + retention. |
| P0-5 | In-memory `FirstOrDefault` account join | L91 | `ToDictionary((BrokerId, Login))`. |

### P1 — wrong answer or missing access path

| ID | Issue | Where |
|---|---|---|
| P1-1 | PnL `GROUP BY` omits `canonical_symbol = 'XAUUSD'` | L79–83 |
| P1-2 | Overview `ToList` + tracked scores instead of SQL counts | L18–20, 30–34 |
| P1-3 | Overview XAU-trader count uses `CompletedXauTrades > 0` on **scores**, not `COUNT(DISTINCT (broker_id, login))` on reconstructed XAU (A91 §11) | L19 |
| P1-4 | `ConnectedBrokers` = `COUNT(Enabled)`, not live connection | L17, 39 |
| P1-5 | `shadow_orders` unconstrained `SUM`; no index; not A24 book | L21 |
| P1-6 | `mt5_accounts_group_ix` missing | DbContext L52–56 |
| P1-7 | Reject feed missing `(outcome, decided_at)` index | DbContext L129–134 |
| P1-8 | Tracking on overview / brokers / groups / FIX / risk | all but `GetTraders` |
| P1-9 | `/api/trades` login-only + raw entity (adjacent) | `Program.cs` 63–70 |
| P1-10 | Per-row `ShadowPnl = 0`; dest P&L / XAU exposure / risk KPIs never queried | L36–38, 107, 159 |

### P2 — hygiene / contract

| ID | Issue |
|---|---|
| P2-1 | Brokers N+1 (only 2 brokers today) |
| P2-2 | `SELECT *` instead of DTO projections |
| P2-3 | Silent ignore of unparsable `state`; case-insensitive vs A92 |
| P2-4 | Quote fields copied onto TRADE session (A94) |
| P2-5 | Health bools from enum set (A101 greenwash) — not extra SQL, but the query **selects the wrong fact** |
| P2-6 | No compiled queries, no Redis snapshot, no tests |
| P2-7 | InMemory default — indexes unprovable |
| P2-8 | `GetRiskAsync` used by both `/api/risk` and `/api/risk/status` (two routes, same 2-query stub) |

---

## 10. What is **not** a remaining query bug

Do not “fix” these as perf work:

| Observation | Why it is OK / out of scope |
|---|---|
| `mlProbability: null` | Required (A52 / A92 L6 / B39). |
| Unmapped groups listed | Required (A39 / A40 / C10). |
| Sequential awaits on one `DbContext` | Required. Combine SQL; do not `WhenAll`. |
| `CountAsync` of 5k accounts on overview | A98: seq scan fine. |
| `Take(20)` on rejects | Right bound; missing index is the issue. |
| Unique `Qualifier` lookup | Works **because** the unique is global (wrong model, cheap query). |
| Cancellation tokens | Present. |
| No secrets in SELECT lists | `CTraderFixOptions.Password` is not on this path (A94). `ManagerLogin` is loaded then masked — remaining **DTO** issue, not a secret-on-the-wire from this SELECT. |

---

## 11. Target query shapes (implementation notes only)

A later increment should replace the seven methods with **bounded, indexed SQL**. Suggested statement counts:

| Method | Target statements | Notes |
|---|---|---|
| Overview | **1–2** | `COUNT(*)` accounts + `COUNT(*) FILTER` / `GROUP BY current_state` on scores + scalar shadow/dest P&L from the **A24** table (not raw `shadow_orders`) + two session rows by qualifier. A91 prefers one round-trip. |
| Brokers | **1** | Brokers ⨝ grouped group-count ⨝ grouped account-count. Connected from heartbeat, not `true`. |
| Groups | **1** | Groups ⨝ brokers ⨝ `COUNT` accounts `GROUP BY (broker_id, group_name)`. |
| Traders | **1 + count** | Filtered join + `COUNT(*) OVER()` or a separate `COUNT` for A92 envelope. `pageSize` ≤ 200. |
| Trader | **2–4** | Header by UK; first-3 XAU partial; optional history/shadow bounded. **Never** call `GetTradersAsync`. |
| FIX | **2** | Two session rows; **one** latest XAU quote via indexed `LIMIT 1` (QUOTE card only). |
| Risk | **2–4** | Latest kill-switch row(s) A48 shape; rejects `LIMIT 20` on partial index; exposure from dest positions **when that table exists** — until then honest `null` / `UNAVAILABLE`, not `0`. |

Do **not** add ClickHouse, Kafka, or a second database “because the leaderboard is slow” (A80 / A98 L7).

---

## 12. Honesty / not claimed

- **No** Postgres `EXPLAIN (ANALYZE, BUFFERS)` was run. Every “seq scan” above is from the **predicate vs fluent index** match, not a planner dump.
- **No** stopwatch numbers. Demo seed cannot produce a meaningful p95.
- This file does **not** implement A91–A95 DTOs. Several “remaining query issues” are **omitted queries** (zeros) as well as **expensive queries**.
- Hash `37A4DDD2…715EF4ACE` is the product file; this report did not change it.
- Product source was **not** modified.

---

## 13. Sources

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | SUT (168 / 7407 / SHA above) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | Port + DTOs |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Indexes / tables |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | InMemory vs Npgsql; no NoTracking default |
| `D:\Prop\apps\api\Program.cs` | Route map + `/api/trades` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 5 s FIX/risk poll |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Demo 4 accounts / 4 groups |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 2 brokers, 1 quote, 2 sessions |
| `D:\Prop\reports\swarm\20260818\A91_overview_dto.md` | One-snapshot query plan |
| `D:\Prop\reports\swarm\20260818\A92_leaderboard_dto.md` | Filters + page 50/200 |
| `D:\Prop\reports\swarm\20260818\A93_trader_detail_dto.md` | Detail ≠ leaderboard row |
| `D:\Prop\reports\swarm\20260818\A94_fix_page_dto.md` | Quote not on TRADE |
| `D:\Prop\reports\swarm\20260818\A95_risk_page_dto.md` | Risk stub / omitted KPIs |
| `D:\Prop\reports\swarm\20260818\A98_pg_indexes.md` | Index contract; N+1 already flagged |
| `D:\Prop\reports\swarm\20260818\C06_dbcontext_review.md` | Keys; `/api/trades` login-only |

---

## 14. Direct answer

**Remaining query issues: yes.**

The leftover work is not a missing `Include`. It is:

1. **N+1** group (and broker) counts.
2. **Full-table** leaderboard + **O(n²)** account join + **no pagination**.
3. **Detail route that re-runs the leaderboard.**
4. **Latest-quote and shadow-sum and reject-feed** with **no supporting index** on tables that grow.
5. **Overview that downloads every score row** to count it.
6. **Several required aggregates never queried** (dest P&L, XAU exposure, per-trader shadow, live connection).
7. **Zero tests** and **no Postgres plan** to prove otherwise.

Until those are fixed, treat `EfDashboardQueries` as a **demo read model**. Do not point a 5,000-login census at it.
