# A005 — Dashboard traders: ingested accounts vs scored rows, `Take(200)`, health FakeMt5

| Field | Value |
|---|---|
| Agent | A005 (senior engineer, dashboard traders / ingest / health only) |
| Date | 2026-08-18 |
| Measured at | 2026-08-18 (source read; no live HTTP; no Manager probe) |
| Workspace | `D:\Prop` (API `apps\api`, web `apps\web`, domain/infra `src\`) |
| Assigned | Read `EfDashboardQueries`, `DealIngestionService`, `GroupsPage`, `TradersPage`, `OverviewPage`, `hooks.ts`, `client.ts`. Answer: all ingested `Mt5Accounts` vs scored traders? `Take(200)` on positions? thousands of real logins render? health still FakeMt5? Write this file. |
| Product source modified | **No.** This report is the only write. |
| Secrets | **None printed.** Config keys named only. Password values not read into this file. |

Classification: `EXISTS_AND_GOOD` | `EXISTS_NEEDS_REFACTOR` | `MISSING` | `UNSAFE`.

---

## 0. Verdict (four questions)

| Question | Measured answer |
|---|---|
| Does the dashboard list **all** ingested `Mt5Accounts` or only scored traders? | **Only scored traders.** `GetTradersAsync` iterates `TraderScores`, not `Mt5Accounts`. Unscored logins are invisible on `/traders` even if they exist in `Mt5Accounts`. Overview/groups **count** all accounts. |
| Is there a `Take(200)` on positions? | **Yes — on accounts whose positions are refreshed, not on the position rows of one login.** `DealIngestionService.SyncBrokerAsync` upserts **every** account, then `foreach (var account in accounts.Take(200))` calls `GetPositionsAsync` + `ReplacePositionsAsync`. A second `Take(200)` exists on `GET /api/trades` (reconstructed trades), not positions. |
| Will thousands of real logins render? | **They can be returned and painted as one unpaginated `<table>`, if they have `TraderScore` rows.** There is no virtualization, no page size, no API envelope. `LiveIngestHostedService` scores **every** `ListLoginsAsync` login. Demo seeder, `/api/ops/resync`, and `apps/mt5-worker` score only `{10001,10002,10003,99001}`. Axios timeout is 15s. Positions snapshots stop at 200 accounts. **Not a 5k-safe UI.** |
| Health endpoint still says FakeMt5? | **Yes.** `GET /api/health` is a hardcoded anonymous object: `details = "demo FakeMt5BrokerConnector — not live Manager"`, `healthy = true` for ACHIEVER. DI now **throws** unless real MT5 passwords exist and registers `NativeMt5BrokerConnector`. Health text is stale. |

**One-line:** Traders page = `TraderScores` leaderboard; ingest stores all accounts but only snapshots positions for the first 200; health still claims FakeMt5.

§73.B traders query as demo: **EXISTS_NEEDS_REFACTOR**.  
§73.B as “list every live Manager login”: **MISSING**.  
§73.B `Take(200)` positions cap: **UNSAFE** at scale (silent drop).  
§73.B `/api/health` FakeMt5 string vs live DI: **UNSAFE** (operator lie).

---

## 1. Files read

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Overview counts, groups census, **traders from scores** |
| `D:\Prop\src\Application\Dashboard\DashboardModels.cs` | `TraderRowDto`, `IDashboardQueries` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Account upsert + **`accounts.Take(200)`** positions |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `UpsertAccountAsync`, `ListLoginsAsync` (all logins) |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Live path: ingest then score **every** stored login |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Seed: ingest fake connectors, score **4** logins |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Real passwords required; native connectors; live ingest host |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | `GetAccountsCore` — **no** account cap |
| `D:\Prop\apps\api\Program.cs` | `/api/traders`, `/api/health` FakeMt5, `/api/trades` `Take(200)`, resync 4 logins, `DemoSeeder` on startup |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Cycle scores **only** 4 demo logins |
| `D:\Prop\apps\web\src\pages\TradersPage.tsx` | Unfiltered `useTraders({})`, `data.map` table |
| `D:\Prop\apps\web\src\pages\GroupsPage.tsx` | Group **account counts** (not trader rows) |
| `D:\Prop\apps\web\src\pages\OverviewPage.tsx` | `totalAccounts` = all `Mt5Accounts` |
| `D:\Prop\apps\web\src\pages\SystemHealthPage.tsx` | Raw JSON of `/api/health` |
| `D:\Prop\apps\web\src\api\hooks.ts` | `useTraders` / `useHealth` |
| `D:\Prop\apps\web\src\api\client.ts` | axios, 15s timeout, no paging params |

---

## 2. All ingested accounts vs scored traders

### 2.1 Ingest writes every account

`DealIngestionService.SyncBrokerAsync`:

- `GetAccountsAsync(null)` — native connector walks **every group** and every `UserGetByGroup` login. No `Take` on the account list.
- `foreach (var account in accounts) await _store.UpsertAccountAsync(...)` — **all** accounts persist to `Mt5Accounts`.
- Deals: bulk-by-group if `IMt5BulkDealReader`, else **per account, no cap**.

`NativeMt5BrokerConnector.GetAccountsCore` builds an unbounded `List<Mt5AccountDto>` (no `Take`).

### 2.2 Traders API is score-driven

`EfDashboardQueries.GetTradersAsync` (lines 74–117):

1. `TraderScores.AsNoTracking().ToListAsync()` — **driver set**.
2. Loads **all** `Brokers` and **all** `Mt5Accounts` (join payload only).
3. Aggregates completed `ReconstructedTrades` PnL by `(BrokerId, Login)`.
4. `foreach (var s in scores)` — skip if broker missing; `FirstOrDefault` account for `GroupName`.
5. Optional in-memory filter `broker` / `TraderState`.
6. `OrderByDescending(t => t.EarlyScore)` — **no `Skip`/`Take`**.

An ingested `Mt5Account` with no `TraderScore` **never becomes a `TraderRowDto`**.

`GetTraderAsync` / `GetTraderDetailAsync` reuse that list. Detail of an unscored login returns **null**.

### 2.3 Who creates `TraderScore` rows

| Path | Logins scored |
|---|---|
| `ReconstructionScoringService.RebuildTraderAsync` | Always upserts a score for the given login (even 0 XAU trades) |
| `LiveIngestHostedService` | `ListLoginsAsync` = **all** `Mt5Accounts` for the broker, then score each |
| `DemoSeeder` | `{10001, 10002, 10003, 99001}` only |
| `POST /api/ops/resync` | same four hardcoded logins |
| `apps/mt5-worker/Worker.cs` | same four hardcoded logins **every 30s** |

API startup still runs `DemoSeeder.SeedAsync` after `EnsureCreatedAsync`. Worker/resync scoring sets are **demo logins**, not live Manager users.

**Effective UI set:**

- After **live ingest host** succeeds: traders ≈ all ingested logins (each has a score, including zeros).
- After **demo seed / resync / mt5-worker only**: traders = **4** demo logins; remaining ingested accounts appear only in Overview/Groups **counts**.

### 2.4 Overview and Groups are account counts, not the leaderboard

| Surface | Source |
|---|---|
| Overview `MT5 accounts` | `Mt5Accounts.CountAsync` (all rows) |
| Overview XAU / Watch / Shadow / … | **`TraderScores` only** |
| Groups `Accounts` column | `Mt5Accounts.CountAsync` per `(BrokerId, GroupName)` — N+1 |
| Brokers `AccountCount` | `Mt5Accounts.CountAsync` per broker |
| Traders table | `TraderScores` |

`GroupsPage` copy: “Plan mappings are labels only — they do not filter ingestion.” Correct. Groups do **not** list logins.

---

## 3. `Take(200)` — where it is and is not

| Location | What is capped |
|---|---|
| `DealIngestionService.cs:74` `accounts.Take(200)` | **Which logins get a live position snapshot.** First 200 of the connector’s account list order. Rest: account+deals may exist; **positions not replaced**. |
| `Program.cs` `GET /api/trades` | `ReconstructedTrades` ordered by `OpenedAt` desc **`Take(200)`**. Not positions. |
| `GetRiskAsync` | Last **20** non-Approve `RiskDecisions`. |
| Account upsert | **No cap.** |
| Deal ingest | **No cap.** |
| `GetTradersAsync` | **No cap.** |
| Native `GetAccountsCore` | **No cap.** |
| `ReplacePositionsAsync` argument | Full list for that one login (uncapped per login). |

`Take(200)` is **not** `positions.Take(200)` inside one account. It is **200 accounts** selected for `GetPositionsAsync`.

Connector enumeration order is Manager group/user order, not “most important traders.” Accounts 201+ silently skip position refresh. Dashboard traders do not show open-position columns, so this cap does not shrink the table; it **starves** later logins of `Mt5Positions` rows.

---

## 4. Will thousands of real logins render?

### 4.1 Backend

`GET /api/traders` → `GetTradersAsync` materializes:

- entire `TraderScores`
- entire `Mt5Accounts`
- entire `Brokers`
- grouped completed-trade PnL

No pagination query params (hooks send only optional `broker` / `state`). `TradersPage` calls `useTraders({})` — **no filters**.

If live ingest scored 3k–10k logins, the JSON is a **bare array of thousands of `TraderRowDto`**. In-memory `FirstOrDefault` account lookup is O(scores × accounts). Groups/brokers add N+1 counts.

### 4.2 Frontend

`TradersPage.tsx`:

- `data = []` default; `data.map` one `<tr>` per row.
- No windowing, no “load more”, no empty state, no error UI (unlike Overview).
- `Number(t.netSourcePnl).toFixed(2)` will throw if a row is malformed; not a volume issue.

`client.ts`: `timeout: 15000`. A large unindexed Postgres + full-table serialize can 15s-timeout; InMemory demo will not.

`hooks.ts`: React Query, **no** `refetchInterval` on traders (health is 10s). One shot per filter key.

### 4.3 Honest scale answer

| Scenario | What the user sees |
|---|---|
| Demo seed only | 4 rows. Fast. Lies about live coverage. |
| Live ingest scored all accounts | Browser **will attempt** to render thousands of table rows. Technically “yes, they render.” Practically: long paint, no virtualization, 15s API timeout risk, N+1 groups page, positions stale after login 200. |
| Worker/resync after live ingest | Scores **overwrite only 4 demo logins**. Extra `TraderScore` rows from a prior live pass would remain unless DB reset. New live logins **not** added by worker. |

**Do not call this “thousands of real logins supported.”** It is “unbounded `ToList` + DOM table.”

---

## 5. Health still says FakeMt5

### 5.1 Endpoints

`D:\Prop\apps\api\Program.cs`:

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "ok", utc = DateTimeOffset.UtcNow }));
app.MapGet("/api/health", () => Results.Ok(new
{
    mt5Connections = new[] { new { name = "ACHIEVER", healthy = true, lastCheck = DateTimeOffset.UtcNow,
        details = "demo FakeMt5BrokerConnector — not live Manager" } },
    fixSessions = new[] { new { name = "QUOTE", healthy = false, lastCheck = DateTimeOffset.UtcNow,
        details = "no live TLS socket" } },
    database = new { name = "postgres-or-inmemory", healthy = true, lastCheck = DateTimeOffset.UtcNow },
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
    outboxBacklog = 0
}));
```

- **No** `IDashboardQueries`, **no** connector ping, **no** `HealthCheck`.
- STARWAVEFX omitted from the array.
- `healthy: true` + FakeMt5 details is the E026 operator-lie pattern, still present.

`useHealth` → `GET /api/health` every 10s. `SystemHealthPage` `JSON.stringify`s it.

### 5.2 Contradiction with current DI

`DependencyInjection.AddTraderIntelligence`:

- Throws if `MT5_PASSWORD` / `MT5_STARWAVEFX_PASSWORD` are missing or placeholder.
- Registers `LiveMt5Registration.CreateConnectors` → **`NativeMt5BrokerConnector` × 2**.
- Registers `LiveIngestHostedService`.
- Fake connectors remain in the tree (`FakeMt5BrokerConnector.CreateDefault`) for **DemoSeeder / tests**, not for DI when the API actually starts with secrets.

Overview `mt5Healthy` is `brokers > 0` after seed — **not** Manager connectivity.

**Answer: yes, `/api/health` still says FakeMt5.** `/health` is a bare liveness `{status:ok}`.

---

## 6. Request path (web)

```
TradersPage  → useTraders({})     → GET /api/traders
GroupsPage   → useGroups()        → GET /api/groups
OverviewPage → useOverview()      → GET /api/overview
SystemHealth → useHealth() 10s    → GET /api/health   (FakeMt5 string)
client.ts    → axios localhost:5000, 15s
```

No `/api/v1/*`. No page/size. No SignalR.

---

## 7. What this report does **not** claim

- Live Achiever/Starwave login counts (Manager not queried here).
- Whether `LiveIngestHostedService` has already scored a given DB.
- That `/api/health` `healthy: true` means the native DLL is connected.
- That `Take(200)` was intended as a product limit vs a leftover demo guard.

---

## 8. Implications (for the next increment; not done here)

1. Leaderboard should left-join `Mt5Accounts` (or explicitly document “scored only”) and page (`limit`/`cursor`).
2. Position refresh must not silently `Take(200)` if open-risk or copy depends on `Mt5Positions`.
3. `mt5-worker` and `/api/ops/resync` must stop scoring four demo logins if live ingest is the source of truth.
4. Replace `/api/health` FakeMt5 literal with measured native-connector / session state; keep `/health` as liveness.

---

## 9. Evidence quotes (no secrets)

`GetTradersAsync` driver (`EfDashboardQueries.cs`):

```
var scores = await _db.TraderScores.AsNoTracking().ToListAsync(ct);
...
var accounts = await _db.Mt5Accounts.AsNoTracking().ToListAsync(ct);
...
foreach (var s in scores) { ... }
```

Position cap (`DealIngestionService.cs`):

```
foreach (var account in accounts.Take(200))
{
    var positions = await connector.GetPositionsAsync(account.Login, ct);
    await _store.ReplacePositionsAsync(brokerId, account.Login, positions, ct);
}
```

Health (`apps/api/Program.cs`):

```
details = "demo FakeMt5BrokerConnector — not live Manager"
```
