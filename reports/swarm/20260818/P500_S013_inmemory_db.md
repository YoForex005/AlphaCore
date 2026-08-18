# P500_S013 — In-memory EF is not a SoT; scores vanish; Postgres before any 35=D

| Field | Value |
|---|---|
| Agent | P500_S013 (senior engineer, persistence / live-copy gate, read-only) |
| Date | 2026-08-18 |
| Assigned | Read `Program.cs` DATABASE_URL placeholder, `CREDENTIALS_AND_COPY_STATUS.md`, Infrastructure DI. Write this file. Scores vanish on restart. You cannot run profitable live copy on in-memory EF. Postgres must be the SoT before any `35=D`. |
| Artifact | `D:\Prop\reports\swarm\20260818\P500_S013_inmemory_db.md` |
| Workspace | `D:\Prop` |
| Product source modified | **No.** Report only. |
| Secrets printed | **None.** Key names and the literal token `<SECRET>` only. |
| Law | Architecture v2 **§ Database** (“PostgreSQL remains the durable source of truth”), **§60** (PostgreSQL migrations), **§72.3** (“Use migrations.”) |
| Status pin | `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` |
| Related (do not treat as this file) | A008 (env wiring), C29 / D51 (migrations + EnsureCreated), D23 (DI inventory — **stale** on FakeMt5 / InMemory name), A61 (EF schema), E016 (copy status) |

Classification vocabulary is architecture §73.B: `EXISTS_AND_GOOD` / `EXISTS_NEEDS_REFACTOR` / `MISSING` / `DEPRECATED` / `UNSAFE`.

---

## 0. Verdict (honest, measured)

**BLOCK. Default runtime is EF Core InMemory. Scores, deals, checkpoints, and copy intents die with the process. That is not a source of truth. It cannot host profitable live copy. Postgres must be the durable SoT before any FIX `35=D` NewOrderSingle is implemented or armed.**

This is not a style note. It is a capital-safety gate.

| Item | Measured 2026-08-18 | Class |
|---|---|---|
| `ConnectionStrings:TraderIntelligence` in any `appsettings*.json` | **absent** (API has unused `ConnectionStrings:Postgres`) | **UNSAFE** (key mismatch) |
| `DATABASE_URL` in operator `.env` | **present as placeholder** (`Password=<SECRET>`) | **UNSAFE** |
| DI fallback when CS empty / contains `<SECRET>` | `UseInMemoryDatabase("trader-intelligence-live")` | **UNSAFE** |
| API `/api/health` database field | always `{ name: "in-memory-or-postgres", healthy: true }` | **UNSAFE** (hides provider) |
| Score persistence | `trader_scores` + `trader_score_history` via `UpsertScoreAsync` on the same DbContext | **EXISTS_NEEDS_REFACTOR** (shape OK, store not durable) |
| Schema authority | `EnsureCreatedAsync()` × 3 hosts; **0** `Migrations/`; **0** `Migrate()` | **UNSAFE** |
| Process isolation | API / mt5-worker / fix-worker each get their **own** InMemory store | **UNSAFE** |
| Live `35=D` | method **does not exist**; `CopyTradingService.NewOrderSingleImplemented = false` | **SAFE_BY_ABSENCE** (today) |
| `RealCopyEnabled` | **env-read** `REAL_COPY_EXECUTION_ENABLED=="true"` (DI L41–42) — **not** hard-forced false | **UNSAFE** if someone flips env on InMemory |
| Profitable live copy on this store | **impossible** | **BLOCK** |

`SAFE_BY_ABSENCE` on send does **not** make InMemory acceptable. The moment someone adds NewOrderSingle, this store would forget LIVE / RISK_BLOCKED / first-3 / ClOrdID history on every restart. That is how you lose money, not how you copy profitably.

## Profit implication

You cannot run a profitable live book on RAM. Restart wipes scores, checkpoints, copy intents, and any future ClOrdID. Re-ingest is a cache warm, not durability. A flipped `REAL_COPY` flag on InMemory is a **loss** path (forgotten RISK_BLOCKED + possible duplicate send). Postgres must be the SoT **before** any `35=D`.

---

## 1. Method

Read-only. Did **not** edit `src/`, `apps/`, `tests/`, `mt5-sdk/`, `.env`.

| Read | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | Env load, health, EnsureCreated, `/api/ops/resync` scoring |
| `D:\Prop\apps\mt5-worker\Program.cs` | EnsureCreated; **no** `EnvFile` |
| `D:\Prop\apps\fix-worker\Program.cs` | same |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | DATABASE_URL / InMemory / Npgsql / `RealCopyEnabled` |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | operator pin: placeholder → in-memory |
| `D:\Prop\apps\api\appsettings.json` + Development | unused `ConnectionStrings:Postgres` |
| `D:\Prop\src\Infrastructure\Persistence\TraderDbContext.cs` | `TraderScores` / `TraderScoreHistory` maps |
| `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs` | `UpsertScoreAsync` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `RebuildTraderAsync` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | dashboard reads scores from EF |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | 90-day resync + score rebuild into the same store |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | who loads `.env` |
| `D:\Prop\docker-compose.yml` | Postgres up, **not** wired to API |

Grep (product `*.cs` / `appsettings*.json`): `DATABASE_URL`, `UseInMemoryDatabase`, `EnsureCreated`, `GetConnectionString`, `NewOrderSingle`, `35=D`, `CanPromoteToLive`.

Did **not** dump `.env` values. Observed only that `DATABASE_URL` is set and that the value contains the literal token `<SECRET>` (same rule the DI code tests).

---

## 2. Program.cs does not choose Postgres. It loads the placeholder and hides the provider.

`apps/api/Program.cs` never reads `DATABASE_URL` by name. It:

1. Calls `EnvFile.FindAndLoad()` (walks cwd parents then `D:\Prop\.env`).
2. Adds environment variables into configuration.
3. Calls `AddTraderIntelligence(builder.Configuration)` — **that** is where the provider is chosen.
4. Boots schema with `EnsureCreatedAsync()` + `BrokerCatalogSeed` only (no score restore).
5. Advertises a health blob that cannot fail closed on InMemory.

```9:14:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

```51:51:D:\Prop\apps\api\Program.cs
        database = new { name = "in-memory-or-postgres", healthy = true, lastCheck = DateTimeOffset.UtcNow },
```

```149:154:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Workers are worse: they call `AddTraderIntelligence` and `EnsureCreatedAsync` but **never** call `EnvFile.FindAndLoad()`. Unless the OS process already has a real `DATABASE_URL` without `<SECRET>`, both workers are InMemory even if the API later is not.

`docker-compose.yml` starts `postgres:16` (`POSTGRES_DB=trader_intelligence`) and an `api` service, but the `api` environment block sets only `ASPNETCORE_ENVIRONMENT`. No `DATABASE_URL`, no `ConnectionStrings__TraderIntelligence`. Compose Postgres can be healthy while the API still scores into RAM.

---

## 3. Credentials pin matches the code path.

`D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` (remeasured 2026-08-18):

| Secret | Present? |
|---|---|
| `DATABASE_URL` | placeholder → API uses in-memory DB |

Also pinned there (names only): `.env` exists; MT5 / FIX passwords present; `REAL_COPY_EXECUTION_ENABLED` **false** (forced); live `35=D` **OFF** — method does not exist; dummy FakeMt5 seed **OFF**; dashboard `/api/traders` returned **8460**.

That census is live Manager data held in **this process’s** EF InMemory + `LiveRuntimeStatus`. It is not a durable book.

Do not treat “8460 traders on the dashboard” as “we have a database.” Those rows evaporate when `TraderIntelligence.Api` exits.

---

## 4. Infrastructure DI: silent InMemory is the default, not a last resort.

```23:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var connection = configuration.GetConnectionString("TraderIntelligence")
                         ?? configuration["DATABASE_URL"];

        if (string.IsNullOrWhiteSpace(connection) || connection.Contains("<SECRET>", StringComparison.Ordinal))
        {
            services.AddDbContext<TraderDbContext>(o => o.UseInMemoryDatabase("trader-intelligence-live"));
        }
        else
        {
            services.AddDbContext<TraderDbContext>(o => o.UseNpgsql(connection));
        }

        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        services.AddSingleton<TraderIntelligence.Domain.Risk.RiskEngine>();
```

**Remeasured 2026-08-18:** first draft said `RealCopyEnabled = false` and “no RiskEngine in DI.” Current `DependencyInjection.cs` L39–45 **reads the env flag** and **does** register `RiskEngine` + `CopyTradingService` + `CopyTradingHostedService`. Send is still off because `NewOrderSingleImplemented = false` and there is no `35=D` builder — not because DI hard-forces the flag.

Measured consequences:

1. **Wrong appsettings key.** API JSON has `ConnectionStrings:Postgres` (localhost / empty password). DI never reads `Postgres`. Grep of `appsettings*.json` for `TraderIntelligence`: **0 hits**. The committed Postgres string is dead.
2. **Placeholder is an explicit InMemory trigger.** Operator `DATABASE_URL` contains the substring `<SECRET>`. That is not “almost Postgres.” The `Contains("<SECRET>")` test **forces** InMemory even though Host/Port/Database/Username look real.
3. **No throw.** Missing MT5 passwords abort startup. Missing / placeholder DB does **not**. Live Manager can connect and write thousands of scores into a store that will not exist after SIGTERM.
4. **Named InMemory is not a shared SoT.** `"trader-intelligence-live"` is shared only inside one process / default InMemory root. API, mt5-worker, and fix-worker are three processes → three empty universes. Restart of any one → that universe is gone.
5. **Npgsql path is still not production.** `UseNpgsql(connection)` has no `MigrationsAssembly`, no retry, no `Migrate()`. `EnsureCreated` on a real server is not versioned schema. That is a later increment (C29 / D51 / C47.1). It is still **strictly better** than InMemory for durability, and it is **not** what default launch uses.

D23 quoted `UseInMemoryDatabase("trader-intelligence")` and FakeMt5 `CreateDefault()`. **Current** code uses `"trader-intelligence-live"` and `LiveMt5Registration`. The **silent fallback rule is unchanged.**

---

## 5. Where scores live — and why they vanish.

`TraderDbContext` maps:

- `trader_scores` unique `(BrokerId, Login)`
- `trader_score_history` indexed `(BrokerId, Login, RecordedAt)`

`ReconstructionScoringService.RebuildTraderAsync` reconstructs from **store deals**, then `UpsertScoreAsync` writes current scores + a history row.

Dashboard `/api/overview` and `/api/traders` read **only** those EF sets (`EfDashboardQueries` loads `TraderScores` then joins accounts). There is no Redis score SoT (health already says Redis `healthy: false`, cache only).

Writers into the same InMemory context:

| Writer | When |
|---|---|
| `LiveIngestHostedService` | 2s after API start: catalog + 90-day deals + per-login rebuild |
| `POST /api/ops/resync` | manual full rebuild for ACHIEVER + STARWAVEFX |
| `apps/mt5-worker/Worker.cs` | worker-local DbContext (not the API’s) |

On process exit, EF InMemory drops the entire model. Next start:

1. `EnsureCreatedAsync` creates an **empty** in-process store.
2. `BrokerCatalogSeed` inserts broker / FIX-session stubs.
3. Scores are **0** until ingest/scoring runs again.
4. `sync_checkpoints` are gone → another 90-day Manager pull.
5. `trader_score_history` is gone → no audit of yesterday’s RISK_BLOCKED / SHADOW.
6. `reconstructed_trades`, `copy_intents`, `shadow_orders`, `execution_intents`, `risk_decisions`, `outbox_events` are gone.

Re-ingest is **not** durability. It is an expensive cache warm. It cannot recover:

- operator overrides that never lived in MT5
- first-3 / early-score clocks that are not a function of the last 90 days alone
- destination / shadow PnL already realized
- ClOrdIDs already sent (when send exists)
- outbox events that were about to dispatch

CREDENTIALS census: **8460** accounts, **1984** open positions. That book is too large to treat as a disposable RAM demo.

---

## 6. Why profitable live copy cannot run on this store

Architecture: “PostgreSQL remains the durable source of truth.” Copy trading is a **state machine over time**, not a screenshot of today’s Manager deals.

| Requirement for profitable / safe `35=D` | InMemory today |
|---|---|
| Remember `RISK_BLOCKED` across restarts | **No** — scorer would re-see a clean book |
| Remember SHADOW first-3 / early quality vs later decay | **No** |
| Shared book between API (dashboard / resync) and fix-worker (send) | **No** — isolated InMemory |
| Idempotent ClOrdID / execution intent / reject memory | **No** — rows die with process |
| Sync checkpoints so you do not double-copy a reopen | **No** |
| Outbox so a crash cannot lose “signal accepted → send” | **No** (table exists, dispatcher does not; store still RAM) |
| Audit why a size was sent | `audit_logs` / score history vanish |
| Multi-instance / restart during a session | new empty store + possible duplicate send |

`CanPromoteToLive` is hard-`false`. `NewOrderSingleImplemented` is `false`. NewOrderSingle **does not exist**. `RealCopyEnabled` is env-read (not hard-false). Those keep capital safe **today** only if the env flag stays false **and** the sender stays absent. They are not a license to arm send on InMemory later.

**Gate (binding for this slot):**

1. Real `DATABASE_URL` / `ConnectionStrings:TraderIntelligence` **without** `<SECRET>`.
2. DI **must not** silently fall back to InMemory when the operator intended Postgres (prod / live-copy profile should fail closed).
3. All hosts (api, mt5-worker, fix-worker) load the same connection and share **one** Postgres.
4. Versioned EF migrations + `Migrate()` (not `EnsureCreated` as SoT).
5. Prove scores / checkpoints / copy intents survive process kill.
6. **Only then** may anyone implement or arm `35=D`.

Until that list is measured true, live NewOrderSingle stays off. Postgres is the SoT. InMemory is a unit-test provider (`tests/Integration` already uses `Guid.NewGuid()` names — that is the correct scope).

---

## 7. Honesty / non-claims

- Did **not** start hosts, kill the API, or re-measure `/api/traders` after a restart. Vanishing is a property of `UseInMemoryDatabase`, not a guess.
- Did **not** connect to localhost:5432. Compose may or may not be up; it is unused by DI anyway.
- Did **not** print `.env` secrets. `DATABASE_URL` is classified **placeholder** because the value contains `<SECRET>` — the same predicate as `DependencyInjection.cs`.
- Did **not** claim EX5 / 95% / “Postgres already wired because appsettings has a Postgres key.”
- D23 / C29 InMemory **name** and FakeMt5 inventory are stale; the fallback **behavior** is current.

---

## 8. Classification summary

| Surface | Class |
|---|---|
| Score write path (`UpsertScoreAsync`) | `EXISTS_NEEDS_REFACTOR` |
| Score read path (dashboard) | `EXISTS_NEEDS_REFACTOR` |
| Default provider (InMemory) | `UNSAFE` |
| `ConnectionStrings:Postgres` vs DI `TraderIntelligence` | `UNSAFE` (dead key) |
| `DATABASE_URL` placeholder | `UNSAFE` |
| Health `database.healthy = true` always | `UNSAFE` |
| EF `Migrations/` | `MISSING` |
| Shared durable SoT | `MISSING` |
| Live `35=D` | `MISSING` / `SAFE_BY_ABSENCE` |
| Profitable live copy on current store | **BLOCK** |

**Bottom line:** Scores are RAM. Restart wipes them. You cannot run profitable live copy on in-memory EF. Postgres must be the source of truth **before** any `35=D`.
