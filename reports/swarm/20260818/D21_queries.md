# D21 — `EfDashboardQueries` query catalog (what the dashboard actually reads)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\D21_queries.md` |
| Agent | D21 (dashboard query inventory; read-only) |
| Date | 2026-08-18 |
| Assigned | Read `EfDashboardQueries.cs`. Write this file. Do not modify product source. |
| Subject | `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` |
| Port | `D:\Prop\src\Application\Dashboard\DashboardModels.cs` (`IDashboardQueries` + 6 DTOs) |
| Host | `D:\Prop\apps\api\Program.cs` |
| Model | `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Postgres / `EXPLAIN` | **Not run.** Default DI is EF InMemory when the connection string is empty or contains `<SECRET>`. |
| Sibling | C36 (perf leftovers on the **same** SHA). This file is the **method + field catalog**, not a copy of C36. |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `UNSAFE`.

---

## 0. Verdict

`EfDashboardQueries` is a **sealed, compiled, DI-wired demo read model**. It implements all **7** `IDashboardQueries` methods. It is **not** the A91–A95 snapshot, **not** a 5,000-login access path, and **not** tested.

| Question | Answer |
|---|---|
| Does the class exist and compile? | **Yes.** `AddScoped<IDashboardQueries, EfDashboardQueries>` in `DependencyInjection.cs` L37. |
| Does it implement the whole port? | **Yes** — 7/7 methods. No extra public surface. |
| Is it the A26 `/api/v1/**` contract? | **No.** Unversioned `/api/*`, flat DTOs, no envelope, no pagination, no RBAC. |
| How many EF round-trips on a cold dashboard paint? | **~28–32** sequential `await`s (overview 7 + brokers 1+2N + groups 2+G + traders 4 + FIX 2 + risk 2). Detail **re-runs** the 4 trader queries. |
| How many DTO fields are real queries vs literals? | **32 queried / derived**, **16 hardcoded** (`0` / `null` / `true` / `false` / `UtcNow`). |
| Are there query tests? | **None.** `tests/` has **0** hits for `EfDashboardQueries`, `IDashboardQueries`, `GetOverviewAsync`, `GetTradersAsync`. |
| File moved since C36? | **No.** Same 168 / 7407 / SHA-256 `37A4DDD2…715EF4ACE`. |

Honest one-liner: **seven methods, four full-table loads, two N+1 loops, one detail that reloads the leaderboard, sixteen painted zeros/flags, zero tests.** Treat it as a demo materializer.

**Class:** `EXISTS_NEEDS_REFACTOR` on the demo seed; **`UNSAFE`** as a 5k Postgres read plane (groups N+1, leaderboard `SELECT *`, `GetTraderAsync` reuse, latest-quote heap sort).

---

## 1. Method (this pass)

1. Re-read `EfDashboardQueries.cs` in full (168 lines). Re-hash SHA-256.
2. Read `IDashboardQueries` + six records, `TraderDbContext` fluent indexes, DI provider switch, `Program.cs` route map, `DemoSeeder` + `DemoBrokerFactory` working set, React `hooks.ts`.
3. Map every DTO constructor argument to a query, an in-memory derivation, or a literal.
4. List `TraderDbContext` `DbSet`s the class **never** touches.
5. Grep `tests/` for the class / method names.
6. **Did not** start the API, **did not** open Postgres, **did not** edit product source.

---

## 2. File identity (re-measured 2026-08-18)

| File | Bytes | Lines | SHA-256 | LastWriteUtc |
|---|---:|---:|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | **7407** | **168** | `37A4DDD23305708566888F0BBE2A6CC5DE253FB7151BDEE848195DE715EF4ACE` | `2026-08-18T07:44:18Z` |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | 2577 | 97 | `7A69C0E729A6962D4CB04D3E74E8316522D53DC63B573A89737BE4DD4DE5B439` | `2026-08-18T07:39:51Z` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | 5951 | 174 | `AFB195ACB2C061EF47C4647D0277DFA94475503966084CBA0D398CCF9AEE07FB` | `2026-08-18T07:42:48Z` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 1900 | 44 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | `2026-08-18T07:44:18Z` |
| `D:\Prop\apps\api\Program.cs` | 4658 | 95 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | `2026-08-18T07:52:04Z` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 1935 | 53 | `5FDC969CAAE78A3049B81CD9BAA57491C728496C953D3ADD856983A2AE92BF20` | `2026-08-18T07:46:00Z` |

Matches C36 on the SUT hash. The file has not moved.

| Hygiene | Measured |
|---|---|
| `AsNoTracking` | **4** sites, all inside `GetTradersAsync` (L76–79). Other methods track. |
| `UseQueryTrackingBehavior(NoTracking)` on `AddDbContext` | **Absent** |
| `Select` DTO projections | Risk `Reason` list + reconstructed P&L group key only |
| `EF.CompileAsyncQuery` | **0** |
| Redis snapshot (A91 / A99) | **Not used** |
| `CancellationToken` | Passed on **every** EF call |
| Same-context `Task.WhenAll` | **Not present** (correct — do not add) |
| Migrations | `Persistence/Migrations/` empty; `Configurations/` empty |

---

## 3. Port and HTTP map

`IDashboardQueries` (`DashboardModels.cs` L88–97):

| Method | Return | API route(s) | React hook | Poll |
|---|---|---|---|---|
| `GetOverviewAsync` | `OverviewDto` | `GET /api/overview` | `useOverview` | focus-refetch |
| `GetBrokersAsync` | `IReadOnlyList<BrokerStatusDto>` | `GET /api/brokers` | `useBrokers` | focus-refetch |
| `GetGroupsAsync` | `IReadOnlyList<GroupRowDto>` | `GET /api/groups` | `useGroups` | focus-refetch |
| `GetTradersAsync(broker, state)` | `IReadOnlyList<TraderRowDto>` | `GET /api/traders?broker=&state=` | `useTraders` | focus-refetch |
| `GetTraderAsync(broker, login)` | `TraderRowDto?` | `GET /api/traders/{broker}/{login}` | `useTraderDetail` | focus-refetch |
| `GetFixSessionsAsync` | `IReadOnlyList<FixSessionDto>` | `GET /api/fix/sessions` | `useFixSessions` | **5 s** |
| `GetRiskAsync` | `RiskDashboardDto` | `GET /api/risk` **and** `GET /api/risk/status` | `useRiskStatus` → `/api/risk` | **5 s** |

Not owned by this class (adjacent dashboard read):

| Route | Owner | Note |
|---|---|---|
| `GET /api/trades?broker=&login=` | `Program.cs` L63–70 inline on `TraderDbContext` | `broker` declared and **ignored**; filter is `Login` only; returns raw `ReconstructedTrade` entities; `Take(200)` |
| `GET /api/health`, `/api/reconciliation/status`, `/api/settings` | anonymous objects in `Program.cs` | hardcoded / demo |

JSON: `ConfigureHttpJsonOptions` registers `JsonStringEnumConverter` (`Program.cs` L10–13), so `TraderState` / session enums serialize as **strings**, not ints. B29’s “no converter” line is **stale** vs this `Program.cs` hash.

No `/api/v1` prefix. No `{ data }` envelope. No auth.

---

## 4. Demo working set (why nothing hurts yet)

From `DemoSeeder` + `DemoBrokerFactory.CreateDefault` (4 scored logins):

| Set | Demo rows | Used by this class? |
|---|---:|---|
| `brokers` | 2 (`ACHIEVER`, `STARWAVEFX`) | yes |
| `mt5_groups` | 4 | yes |
| `mt5_accounts` | 4 (10001, 10002, 10003, 99001) | yes |
| `trader_scores` | 4 | yes |
| `reconstructed_trades` | ~9 completed XAU | yes (`GetTraders` P&L only) |
| `destination_quotes` | 1 | yes (latest row, any symbol) |
| `shadow_orders` | 0 | yes (`SUM` → 0) |
| `risk_decisions` | 0 | yes (`Take(20)` → empty) |
| `fix_sessions` | 2 | yes |
| `kill_switches` | 1 (`None`) | yes |

At this size every `ToListAsync` is a few KB. **Demo speed is not evidence.**

---

## 5. Method catalog

Round-trips are **sequential `await`s on one scoped `TraderDbContext`**. That concurrency shape is correct. The leftover problem is **too many statements and too much payload**.

### 5.1 `GetOverviewAsync` — 7 statements

```14:43:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
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
        // OverviewDto: DestinationRealPnl=0, XauGross=0, XauNet=0, RealCopyEnabled=false
        // Mt5Healthy = brokers > 0
    }
```

| # | Shape | Tracking | Index today | Notes |
|---|---|---|---|---|
| 1 | `COUNT(*) FROM mt5_accounts` | n/a | PK | OK at 5k |
| 2 | `COUNT(*) FROM brokers WHERE Enabled` | n/a | tiny | **Wrong fact** vs A91: this is registered-enabled, not live-connected |
| 3 | `SELECT * FROM trader_scores` | **Yes** | unique `(BrokerId, Login)` unused | Counts belong in SQL `COUNT(*) FILTER` / `GROUP BY CurrentState` |
| 4 | `SUM(SourceVsShadowSlippage) FROM shadow_orders` | n/a | **no index** | Not A24 `shadow_performance`. Unbounded. Demo = 0 |
| 5–6 | `SingleOrDefault` by `Qualifier` | Yes | unique `Qualifier` | Fine for 2 rows. Unique is global (no venue) |
| 7 | in-memory 5 state counts | — | — | After the full load |

A91 §11 wants **one snapshot / one round-trip**. Current: 7 statements + 3 hardcoded money/qty zeros + health from enum membership.

**Class:** `EXISTS_NEEDS_REFACTOR`.

### 5.2 `GetBrokersAsync` — 1 + 2N (demo 5)

```45:57:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var brokers = await _db.Brokers.OrderBy(b => b.Code).ToListAsync(ct);
        foreach (var b in brokers)
        {
            var groups = await _db.Mt5Groups.CountAsync(g => g.BrokerId == b.Id, ct);
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == b.Id, ct);
            result.Add(new BrokerStatusDto(..., MaskLogin(b.ManagerLogin), true, groups, accounts, DateTimeOffset.UtcNow));
        }
```

| Issue | Detail |
|---|---|
| N+1 | Two `CountAsync` **per broker**. Should be two `GroupBy(BrokerId)` (or one SQL). |
| Tracking | Full `Broker` entities tracked, including raw `ManagerLogin`. |
| Fabricated | `Connected = true` literal; `LastEventAt = DateTimeOffset.UtcNow`. No checkpoint / last-deal query. |
| Mask | `MaskLogin` = `login / 100 * 100` (2027 → **2000**). A26 §3.2 wants last-two-digit mask (`**27`). |

Broker count stays 2. N+1 here is **P2** for this lab.

**Class:** `EXISTS_NEEDS_REFACTOR`.

### 5.3 `GetGroupsAsync` — 2 + G (demo 6; 80 groups → 82)

```59:72:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var groups = await _db.Mt5Groups.ToListAsync(ct);
        var brokers = await _db.Brokers.ToDictionaryAsync(b => b.Id, ct);
        foreach (var g in groups)
        {
            var accounts = await _db.Mt5Accounts.CountAsync(a => a.BrokerId == g.BrokerId && a.GroupName == g.Name, ct);
            rows.Add(new GroupRowDto(...));
        }
```

| Issue | Detail |
|---|---|
| N+1 **P0** | One `COUNT(*)` per group on `(BrokerId, GroupName)`. |
| Missing index | A98 `mt5_accounts_group_ix` is **not** in `TraderDbContext`. Each count is a seq scan of `mt5_accounts` even after collapsing to one `GROUP BY`. |
| Tracking | All groups + all brokers tracked. |
| Filter | Returns **every** `mt5_groups` row, including unmapped. **Correct** vs A39/A40. |
| Orphan broker | Unknown `BrokerId` falls back to `g.BrokerId.ToString()` (a GUID string as `Broker`). |

Needed shape (not implemented):

```sql
SELECT broker_id, group_name, COUNT(*)
FROM mt5_accounts
GROUP BY 1, 2;
```

**Class:** `UNSAFE` at many groups.

### 5.4 `GetTradersAsync` — 4 full-set reads, then in-memory filter/sort

```74:117:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
        var brokers = await _db.Brokers.AsNoTracking().ToDictionaryAsync(b => b.Id, ct);
        var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
        var pnls = await _db.ReconstructedTrades.AsNoTracking()
            .Where(t => t.Completed)
            .GroupBy(t => new { t.BrokerId, t.Login })
            .Select(g => new { g.Key.BrokerId, g.Key.Login, Pnl = g.Sum(x => x.NetRealizedPnl) })
            .ToListAsync(ct);
        // account lookup: accounts.FirstOrDefault per score  (O(n²))
        // filter broker/state in memory; OrderByDescending EarlyScore
```

This is the **worst remaining query**.

| ID | What it does | Why it fails at 5k / vs A92 |
|---|---|---|
| Q1 | `SELECT *` all `trader_scores` | Filters applied **after** materialization |
| Q2 | `SELECT *` all `mt5_accounts` | Only `GroupName` is used |
| Q3 | `GROUP BY (BrokerId, Login) WHERE Completed` over **all** reconstructed trades | **No** `canonical_symbol = 'XAUUSD'`. Mixes non-XAU the moment a second symbol exists |
| Q4 | `accounts.FirstOrDefault` per score | **O(\|scores\| × \|accounts\|)** = 25M comparisons at 5k |
| Q5 | No `page` / `pageSize` | A92 default 50 / max 200. API returns the entire list |
| Q6 | Sort is `EarlyScore DESC` only | No `NULLS LAST` multi-key chain |
| Q7 | 6 of 8 §50 filters **not queried** | No `group`, score range, risk range, trade-count, flags, date, `q` |
| Q8 | `state` parse is ignore-case | Bad token **silently ignored** (no 400) |
| Q9 | `ShadowPnl` literal `0` | No per-login shadow book |
| Q10 | `MlProbability` literal `null` | **Correct** vs A52 / B39 |
| Q11 | Unknown `BrokerId` → `continue` | Silent drop |

`AsNoTracking` on this path is the **only** tracking hygiene in the class. Keep it.

**Class:** `UNSAFE` as a 5k leaderboard.

### 5.5 `GetTraderAsync` — reuses 5.4 in full

```119:123:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    public async Task<TraderRowDto?> GetTraderAsync(string broker, long login, CancellationToken ct)
    {
        var rows = await GetTradersAsync(broker, null, ct);
        return rows.FirstOrDefault(t => t.Login == login);
    }
```

| Fact | Detail |
|---|---|
| Unique `(BrokerId, Login)` on `trader_scores` | **Never used** |
| Payload | Leaderboard **row**, not A93 detail (no first-3 XAU, no score history, no shadow/live books) |
| Identity | Broker-scoped list then `Login == login` — safer than `/api/trades`, still not a keyed `WHERE` |
| Amplification | Every trader-page hit = full `/api/traders` cost |

**Class:** `UNSAFE`.

### 5.6 `GetFixSessionsAsync` — 2 statements

```125:147:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var sessions = await _db.FixSessionStates.OrderBy(s => s.Qualifier).ToListAsync(ct);
        var quote = await _db.DestinationQuotes.OrderByDescending(q => q.ReceivedAt).FirstOrDefaultAsync(ct);
        // latest quote cloned onto EVERY session; ExecutionEnabled = false
```

| Issue | Detail |
|---|---|
| Latest quote | `ORDER BY ReceivedAt DESC LIMIT 1` with **no index** on `destination_quotes` (PK only). Highest-growth scan. Polled every 5 s. |
| Symbol | Latest row **of any instrument**. Not mapped XAUUSD. |
| Quote on TRADE | A94 §8.1: TRADE card must not carry bid/ask. One extra query used **incorrectly**. |
| `Connected` | `Status` not in `{Disconnected, Error}` |
| `LoggedOn` | `LoggedOn` \| `ReadyForMarketData` \| `ReadyForExecution` \| `Reconciling` |
| `ExecutionEnabled` | literal `false` (honest for first useful; not a query of the flag) |
| `Qualifier` | `ToString().ToUpperInvariant()` → `QUOTE` / `TRADE` |

**Class:** `UNSAFE` once the tape is real; `EXISTS_NEEDS_REFACTOR` on demo (1 row).

### 5.7 `GetRiskAsync` — 2 statements

```149:160:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
        var ks = await _db.KillSwitches.OrderByDescending(k => k.UpdatedAt).FirstOrDefaultAsync(ct);
        var rejects = await _db.RiskDecisions
            .Where(r => r.Outcome != RiskDecisionOutcome.Approve)
            .OrderByDescending(r => r.DecidedAt)
            .Take(20)
            .Select(r => r.Reason)
            .ToListAsync(ct);
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
```

| Issue | Detail |
|---|---|
| Reject index | Fluent index is `CopyIntentId` only. Predicate is `Outcome != Approve ORDER BY DecidedAt DESC LIMIT 20`. **Cannot use** that index. |
| Kill switch | Latest row’s `Mode.ToString()`. A48/A95 want **two independent** controls, not one exclusive enum. |
| Missing queries | Daily P&L, drawdown, XAU long/short/net are literals `0`. Dest position tables are mostly **MISSING** (B19). |
| Projection | `Reason` only — drops `CopyIntentId`, login, broker. |
| Dual route | Same 2-query stub serves `/api/risk` and `/api/risk/status`. |

**Class:** `EXISTS_NEEDS_REFACTOR` (shape) + `MISSING` (exposure / dest-account sources).

### 5.8 `MaskLogin` (private helper)

```162:167:D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs
    private static long MaskLogin(long login)
    {
        if (login < 100)
            return login;
        return login / 100 * 100;
    }
```

Returns a **long**, not a masked string. Demo 2027 → 2000; 9904 → 9900. Logins `< 100` are returned raw. Not a query bug; remaining **DTO / §48** bug.

---

## 6. Field-level map (query vs literal)

### 6.1 `OverviewDto` (18 ctor args)

| Field | Source | Kind |
|---|---|---|
| `TotalAccounts` | `COUNT(mt5_accounts)` | query |
| `ConnectedBrokers` | `COUNT(brokers WHERE Enabled)` | query, **wrong predicate** |
| `XauTraders` | in-memory `CompletedXauTrades > 0` | derived after full load; A91 wants `COUNT DISTINCT` on reconstructed XAU |
| `TradersWithThreeTrades` | in-memory `CompletedXauTrades >= 3` | derived |
| `Watch` / `Shadow` / `LiveCandidates` / `Live` / `RiskBlocked` | in-memory `CurrentState` | derived; leftover states (`INSUFFICIENT_DATA`, `EARLY_SCORE`, `PAUSED`, `DISQUALIFIED`) not tiled (correct vs §47) |
| `ShadowPnl` | `SUM(shadow_orders.SourceVsShadowSlippage)` | query, **wrong book** |
| `DestinationRealPnl` | `0` | **literal** |
| `XauGross` | `0` | **literal** |
| `XauNet` | `0` | **literal** |
| `Mt5Healthy` | `brokers > 0` | derived lie (enabled count ≠ ingestion heartbeat) |
| `QuoteHealthy` | quote session status ∈ {`LoggedOn`, `ReadyForMarketData`, `ReadyForExecution`} | derived from enum set |
| `TradeHealthy` | trade session status ∈ {`LoggedOn`, `Reconciling`, `ReadyForExecution`} | derived; seeder `LoggedOn` paints healthy without a live Logon |
| `RealCopyEnabled` | `false` | **literal** (honest for first useful) |

A91 still missing on the wire: `generatedAt`, nested objects, dest free margin / margin level, structured `healthStatus`, two independent kill flags, `featureQuality`.

### 6.2 `BrokerStatusDto`

| Field | Source | Kind |
|---|---|---|
| `Code` / `DisplayName` / `Server` | `Broker` | query |
| `ManagerLoginMasked` | `MaskLogin(ManagerLogin)` | derived (wrong algorithm) |
| `Connected` | `true` | **literal** |
| `GroupCount` / `AccountCount` | per-broker `CountAsync` | query (N+1) |
| `LastEventAt` | `DateTimeOffset.UtcNow` | **literal now** |

Never queried: `Port`, `ServerName`, `Mode`, `PoolSize`, proxy, `SyncCheckpoint`, last deal.

### 6.3 `GroupRowDto`

| Field | Source | Kind |
|---|---|---|
| `Broker` | `brokers.Code` (or GUID string) | query |
| `Group` | `Mt5Group.Name` | query |
| `Accounts` | per-group `CountAsync` | query (N+1) |
| `EnabledForAnalysis` / `PlanMapping` / `LastDiscovered` / `LastSynced` | group columns | query |

### 6.4 `TraderRowDto`

| Field | Source | Kind |
|---|---|---|
| `Broker` | `brokers.Code` | query |
| `Login` | `TraderScore.Login` | query |
| `Group` | `Mt5Account.GroupName` | query (O(n²) join) |
| `CompletedXauTrades` | score column | query |
| `NetSourcePnl` | completed reconstructed `Sum(NetRealizedPnl)` | query, **no XAU filter** |
| `EarlyScore` | `EarlyQualityScore` | query (domain `[0,100]`, not A26 0–1) |
| `MlProbability` | `null` | **literal — correct** |
| `RiskScore` | score column | query |
| `Martingale` / `AveragingDown` / `LotEscalation` | score flags | query |
| `State` | `CurrentState` | query |
| `ShadowPnl` | `0` | **literal** |
| `LastScored` | `LastScoredAt` | query |

Never queried: `TraderScoreHistory`, first-3 reconstructed block, live allocation, dest positions.

### 6.5 `FixSessionDto`

| Field | Source | Kind |
|---|---|---|
| `Qualifier` | enum `ToString().ToUpperInvariant()` | derived |
| `Host` / `Port` / seq / reconnect / `LastError` / inbound-outbound stamps | `FixSessionState` | query |
| `Connected` / `LoggedOn` | status set membership | derived |
| `Status` | `Status.ToString()` | derived (PascalCase enum name) |
| `InstrumentId` / `Bid` / `Ask` | latest `DestinationQuoteSnapshot` | query, cloned onto **both** cards |
| `QuoteAgeSeconds` | `UtcNow - ReceivedAt` | derived |
| `ExecutionEnabled` | `false` | **literal** |

Never queried: `SenderCompId` / `TargetCompId` (loaded on entity, not mapped), ownership, last ER, last recon, SSL, heartbeat.

### 6.6 `RiskDashboardDto`

| Field | Source | Kind |
|---|---|---|
| `DailyPnl` / `Drawdown` / `XauLong` / `XauShort` / `XauNet` | `0` | **literal** |
| `KillSwitch` | latest `KillSwitch.Mode.ToString()` or `"None"` | query of one exclusive enum |
| `RealCopyEnabled` | `false` | **literal** |
| `RecentRejectReasons` | last 20 non-`Approve` `Reason` strings | query, identity stripped |

---

## 7. Tables this class touches vs ignores

`TraderDbContext` exposes **20** `DbSet`s.

| `DbSet` | Table | Used by |
|---|---|---|
| `Brokers` | `brokers` | Overview count; Brokers list; Groups dict; Traders dict |
| `Mt5Groups` | `mt5_groups` | Brokers count; Groups list |
| `Mt5Accounts` | `mt5_accounts` | Overview count; Brokers count; Groups count; Traders group name |
| `TraderScores` | `trader_scores` | Overview (full); Traders (full) |
| `ReconstructedTrades` | `reconstructed_trades` | Traders P&L `GROUP BY` only |
| `ShadowOrders` | `shadow_orders` | Overview `SUM` only |
| `DestinationQuotes` | `destination_quotes` | FIX latest row |
| `FixSessionStates` | `fix_sessions` | Overview health + FIX list |
| `RiskDecisions` | `risk_decisions` | Risk reject feed |
| `KillSwitches` | `kill_switches` | Risk mode |

**Never queried** (present on the context, unused by the dashboard read model):

| `DbSet` | Why it matters |
|---|---|
| `Mt5Deals` | ingestion truth; not a dashboard grain |
| `Mt5Positions` | source book — A91 dest exposure must **not** use this |
| `CanonicalInstruments` / `SourceSymbolMappings` | XAU filter / tag 55 — unused; leaderboard P&L has no symbol predicate |
| `TraderScoreHistory` | A93 timeline |
| `OutboxEvents` | health / backlog |
| `SyncCheckpoints` | A91 MT5 ingestion freshness |
| `CopyIntents` | reject identity, live map |
| `ExecutionIntents` | TRADE card last ER / ClOrdID |
| `AuditLogs` | `/audit` |

No dest-position / dest-account / `shadow_performance` / `broker_connections` sets exist (B19). Those A91/A95 fields are therefore **unqueryable** today — the class paints `0` instead of `null` / `UNAVAILABLE`.

---

## 8. Index vs predicate (fluent only — not applied DDL)

Source: `TraderDbContext.OnModelCreating`. A98 names are **not** on disk. `EnsureCreated` is what the API uses (`Program.cs` L87).

| Predicate this class uses | Fluent index? | Hits if Postgres existed? |
|---|---|---|
| `mt5_accounts` `COUNT(*)` | PK | seq OK at 5k |
| `mt5_accounts` `COUNT` by `BrokerId` | prefix of unique `(BrokerId, Login)` | would hit |
| `mt5_accounts` `COUNT` by `(BrokerId, GroupName)` | **No** | seq scan |
| `trader_scores` unique `(BrokerId, Login)` | Yes | **unused** (full scan + detail reuse) |
| `reconstructed_trades` `WHERE Completed GROUP BY (BrokerId, Login)` | 4-col `(BrokerId, Login, PositionId, OpenedAt)` | prefix usable; `Completed` not leading; no XAU |
| `shadow_orders` `SUM` | **No indexes** | seq scan |
| `destination_quotes` `ORDER BY ReceivedAt DESC LIMIT 1` | **No** | seq scan + sort |
| `risk_decisions` `Outcome != Approve ORDER BY DecidedAt DESC LIMIT 20` | `CopyIntentId` only | seq scan + sort |
| `fix_sessions` by `Qualifier` | unique `Qualifier` | hits because unique is global |
| `kill_switches` latest `UpdatedAt` | PK only | OK (1 row) |

---

## 9. Round-trip budget (one React paint)

`useFixSessions` + `useRiskStatus` poll every **5 s**. `useTraderDetail` = full leaderboard.

| User action | Methods | SQL (demo) | SQL (5k / ~80 groups / 2 brokers) |
|---|---|---:|---:|
| Overview | `GetOverview` | 7 | 7 + 5k-row tracked `SELECT *` scores + full `SUM(shadow)` |
| Brokers | `GetBrokers` | 5 | 5 |
| Groups | `GetGroups` | 6 | **82** |
| Traders | `GetTraders` | 4 | 4 **full-table** + 25M in-memory joins + unbounded JSON |
| Trader detail | `GetTrader` → `GetTraders` | 4 | **same as traders** |
| FIX (every 5 s) | `GetFixSessions` | 2 | 2, second is latest-quote sort |
| Risk (every 5 s) | `GetRisk` | 2 | 2, second is reject seq-scan |

A dashboard left open on FIX+Risk is **~48 queries/minute** before any click.

---

## 10. Contract gaps (A91–A95) that are query omissions

These are **not** extra SQL today. They are required facts the class never reads.

| Spec | Required read | Current |
|---|---|---|
| A91 §11 | One snapshot; XAU traders from reconstructed XAU; connected = heartbeat | 7 statements; score `CompletedXauTrades > 0`; `COUNT(Enabled)` |
| A91 §6.3–6.4 | A24 shadow book + dest P&L/exposure with quality | `SUM(slippage)`; dest P&L / XAU qty = `0` |
| A91 §7 | Two independent flags + flatten availability | one `RealCopyEnabled=false` |
| A92 | page 50/200; 8 filters; per-row shadow P&L; live allocation | 2 filters in memory; `ShadowPnl=0`; no page |
| A93 | Keyed detail + first-3 XAU + history | leaderboard row via full list |
| A94 | Two cards; quote **only** on QUOTE; no password | one list; quote cloned; no password column selected (absence ≠ allow-list) |
| A95 / A48 | Dest account + exposure + `rejectedIntents[]` with identity; two kill bits | five zeros + `Mode.ToString()` + reason strings |

`mlProbability: null` is **not** a gap (A52). Unmapped groups listed is **not** a gap (A39). Sequential awaits on one context is **not** a gap.

---

## 11. Secrets surface on these SELECTs

| Check | Measured |
|---|---|
| FIX password / RawData / tag 96 | **Not** on `FixSessionState` or this SELECT list |
| `CTraderFixOptions.Password` | Not on this path |
| `Broker.ManagerLogin` | Loaded as a long, then floor-masked on the DTO. Raw value sits in the tracked entity |
| MT5 / proxy / DB passwords | Not selected |
| Response sanitizer (A26 §3.4) | **Not implemented** on these routes |

No live password is returned today. That is **SAFE_BY_ABSENCE**, not an allow-list.

---

## 12. Target statement counts (implementation notes only — not done here)

| Method | Target statements | Notes |
|---|---|---|
| Overview | **1–2** | SQL `COUNT` / `COUNT(*) FILTER` / `GROUP BY current_state`; shadow/dest from A24 / dest book; two session rows. A91 prefers one round-trip. |
| Brokers | **1** | Brokers ⨝ grouped counts. Connected from heartbeat, not `true`. |
| Groups | **1** | Groups ⨝ brokers ⨝ `COUNT` accounts `GROUP BY (broker_id, group_name)`. |
| Traders | **1 + count** | SQL `WHERE` + `ORDER BY` + `LIMIT`; `pageSize` ≤ 200. |
| Trader | **2–4** | Header by UK; first-3 XAU partial; **never** call `GetTradersAsync`. |
| FIX | **2** | Two session rows; one latest **XAU** quote via indexed `LIMIT 1` (QUOTE card only). |
| Risk | **2–4** | Latest A48 latch row(s); rejects `LIMIT 20` on a supporting index; exposure when dest tables exist — until then honest `null` / `UNAVAILABLE`, not `0`. |

Do **not** add ClickHouse / Kafka / a second database because the leaderboard is slow (A80 / A98).

---

## 13. Honesty / not claimed

- **No** `EXPLAIN (ANALYZE, BUFFERS)`. Every “seq scan” is predicate-vs-fluent-index, not a planner dump.
- **No** stopwatch numbers. Demo seed cannot produce a meaningful p95.
- This file does **not** implement A91–A95. Several “gaps” are **omitted queries** (zeros) as well as **expensive queries**.
- SUT hash `37A4DDD2…715EF4ACE` is unchanged by this pass.
- Product source was **not** modified.

---

## 14. Sources

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | SUT (168 / 7407 / SHA above) |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | Port + 6 DTOs |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | Tables / fluent indexes |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | InMemory vs Npgsql; scoped registration |
| `D:\Prop\apps\api\Program.cs` | Route map + `/api/trades` + `EnsureCreated` |
| `D:\Prop\apps\web\src\api\hooks.ts` | 5 s FIX/risk poll |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | 2 brokers, 1 quote, 2 sessions |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | 4 accounts / 4 groups / demo deals |
| `D:\Prop\reports\swarm\20260818\A91_overview_dto.md` | One-snapshot query plan |
| `D:\Prop\reports\swarm\20260818\A92_leaderboard_dto.md` | Filters + page 50/200 |
| `D:\Prop\reports\swarm\20260818\A93_trader_detail_dto.md` | Detail ≠ leaderboard row |
| `D:\Prop\reports\swarm\20260818\A94_fix_page_dto.md` | Quote not on TRADE |
| `D:\Prop\reports\swarm\20260818\A95_risk_page_dto.md` | Risk stub / omitted KPIs |
| `D:\Prop\reports\swarm\20260818\A98_pg_indexes.md` | Index contract |
| `D:\Prop\reports\swarm\20260818\C36_query_perf.md` | Prior perf pass; same SHA |

---

## 15. Direct answer

`EfDashboardQueries` is a **7-method demo catalog**, not a dashboard query plane.

What it actually reads:

1. **Overview** — account count, enabled-broker count, **all** scores (then counts in process), unbounded shadow-slippage sum, two FIX rows. Dest P&L / XAU exposure / real-copy flag are **literals**.
2. **Brokers** — all brokers + **N+1** group/account counts. `Connected` and `LastEventAt` are **fabricated**.
3. **Groups** — all groups + all brokers + **N+1** account counts. Unmapped groups correctly listed.
4. **Traders** — all scores, all brokers, all accounts, all completed-trade P&L aggregates; filter/sort **in memory**; no page; `ShadowPnl=0`; `MlProbability=null` (correct).
5. **Trader detail** — **re-runs (4)**.
6. **FIX** — all sessions + latest quote of **any** symbol, cloned onto TRADE, polled every 5 s with **no** `ReceivedAt` index.
7. **Risk** — latest exclusive `KillSwitchMode` + 20 reject **strings**. Five KPIs are `0`.

**16 constructor fields are hardcoded.** **10 of 20** context sets are unused. **0 tests.** Same SHA as C36. Do not point a 5,000-login census at this class until the leaderboard is filtered/paged in SQL, `GetTraderAsync` is keyed, group counts are one `GROUP BY`, and quote/reject/shadow paths have supporting indexes (or honest `UNAVAILABLE` instead of painted zeros).
