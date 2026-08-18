# C27 — StackExchange.Redis referenced; workers use no lease

| Field | Value |
|---|---|
| Agent | C27 (Redis lease gap) |
| Date | 2026-08-18 |
| Assigned question | `StackExchange.Redis` is referenced. Do workers implement / use a lease? |
| Artifact | `D:\Prop\reports\swarm\20260818\C27_redis_gap.md` |
| Product source modified | **No** |
| Method | Full read of Infrastructure csproj + DI, both worker hosts, `FixSessionOwnership`, `FixSessionState`, compose, API health, A46/A99 specs. Grep of product `*.cs` (exclude vendor) for Redis client types and lease ports. SHA-256 of evidence files. |
| Binding law | Architecture v2 §5 (Redis coordination, not SoT), §28 (single-active TRADE ownership), §62 (fail closed if DB down) |
| Binding siblings | A03/B03 (infra), A05/B05 (Fix.CTrader), A08/B07/C07 (workers), A25 (FIX session), A46 (Redis lease + fence), A64 (pipelines), A65/B37 (compose), A99 (key allow-list) |
| Supersedes for *this question* | A03 “0 Redis usings / unused package” and B07 “StackExchange.Redis referenced by Infrastructure and unused” — **re-measured 2026-08-18; same verdict, more precise worker evidence** |

**Honesty rule:** a NuGet reference is not a lease. An in-memory `ConcurrentDictionary` is not a Redis lease. `OwnerHeld` columns that nobody writes are not ownership. `depends_on: redis` is not a multiplexer. Vacuous safety (no TRADE socket today) is **not** an implemented §28 control.

---

## 0. Verdict

**Package present. Lease absent. Workers do not acquire, renew, or release any Redis (or Postgres) session lease.**

| Question | Answer |
|---|---|
| Is `StackExchange.Redis` referenced? | **Yes.** `TraderIntelligence.Infrastructure.csproj` pins **2.8.0**. Restored. Copied next to both worker exes. |
| Does any product `.cs` `using StackExchange.Redis`? | **No. 0 hits.** |
| Is there a `ConnectionMultiplexer` / `IConnectionMultiplexer` / `IDatabase`? | **No.** |
| Is there `RedisTradeSessionLease` / `IRedisLease` / Lua SET-PX bind? | **No.** Spec-only in A46 §14 / A99. |
| Do `apps/fix-worker` or `apps/mt5-worker` call any lock/lease? | **No.** |
| Is `FixSessionOwnership` registered in DI? | **No.** |
| Do tests construct the in-memory lock? | **No.** 0 hits under `D:\Prop\tests`. |
| Is this a live dual-owner incident today? | **No** — there is no FIX socket. **SAFE_BY_ABSENCE**, not by lease. |
| Will two `fix-worker` replicas be safe after a naive TRADE session is added? | **No. UNSAFE.** Both will Logon. cServer copies every `35=8` to every connection (A05/A25/A46). |

Classification:

| Slice | Class |
|---|---|
| `StackExchange.Redis` 2.8.0 as the *client* choice | **EXISTS_AND_GOOD** (raw client; `IDistributedCache` correctly omitted) |
| Redis multiplexer + options + connection string | **MISSING** |
| Redis façade / key allow-list in code (A99) | **MISSING** |
| Redis TRADE/QUOTE lease + fencing Lua (A46 §4) | **MISSING** |
| Postgres `fix_session_leases` + token mint (A46 §5) | **MISSING** |
| Application ports `ITradeSessionOwnership` / `IRedisLease` / `IFencingTokenStore` | **MISSING** |
| `FixSessionOwnership` + `InMemoryDistributedLockWithFencing` | **EXISTS_NEEDS_REFACTOR** — process-local stub; unused |
| Worker use of any lease | **MISSING** |
| Dual-owner protection in production | **MISSING** (vacuous today) |
| Redis as SoT for orders / positions / balances | **Not UNSAFE** — nothing writes Redis. Vacuous ≠ designed. |
| Compose `redis:` + API `/api/health` `redis.healthy=false` | **THEATER** — lab container unused by product code |

Do **not** treat the Redis DLL on disk as §28. Do **not** add a TRADE initiator until A46 is implemented. Do **not** fall back to the in-memory lock in production.

---

## 1. Inventory (source of truth; exclude `bin/` / `obj/` unless noted)

Hashes measured 2026-08-18 (SHA-256). Worker host hashes match C07/B07.

| Path | Bytes | SHA-256 | Role vs lease |
|---|---:|---|---|
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | **Only** product `PackageReference` of `StackExchange.Redis` 2.8.0 |
| `src/Infrastructure/DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | EF + Fake MT5 only. **No** Redis / lock registration |
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | 4719 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | In-memory fence stub. Comment says “Replace with a Redis-backed lock.” |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | Domain + Application only. **No** Redis package |
| `src/Domain/Entities/FixSessionState.cs` | 979 | `46C20D6A1BF5F84769DB483FD17A0EBEB8BDA8C1C56BBA2B8B30A59FCE44697E` | `OwnerHeld` / `OwnerInstance` columns; **no** fencing token |
| `apps/fix-worker/Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | `AddTraderIntelligence` + seeder. **No** Fix.CTrader type used |
| `apps/fix-worker/Worker.cs` | 1971 | `B48033A5A13C56DB747D3C9F0B94E36CB8DC2866FBCF3789A62C3D7B318B0D48` | 15 s `fix_sessions` heartbeat. **No** acquire/renew |
| `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | 856 | `D7572CBFF273089587D4A68B2A95EB1933C2DF388177F8665BE7C3CC338B5DB4` | Refs Infrastructure + Fix.CTrader. **No** direct Redis package |
| `apps/mt5-worker/Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | Same boot. Ingest host, not TRADE owner |
| `apps/mt5-worker/Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s Fake ingest + score. **No** Redis scores, **no** lease |
| `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | 840 | `E0321028B0E12EEFE97A9BE2D0A08E8E8F89B819CCA4403D301B76A90C56B91C` | Refs Infrastructure + Mt5. **No** direct Redis package |
| `apps/api/Program.cs` | 4658 | `E914FA984A377972D13B5E8C47FDE7B8A48462101C547B81B6DA5A502345AEE9` | Hard-codes `redis.healthy = false`, `"not required for demo"` |
| `docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `redis:7` on 6379. API `depends_on: redis`. **Workers not in compose** |

`Directory.Build.props` has **no** `PackageVersion` for Redis (A102 planned a central pin; not applied). Version **2.8.0** is local to the Infrastructure csproj.

Infrastructure source tree (product `.cs` only): `DependencyInjection.cs`, `Dashboard/EfDashboardQueries.cs`, `Persistence/TraderDbContext.cs`, `Persistence/EfTradingStore.cs`, `Seeding/DemoSeeder.cs`. **No** `Redis/`, **no** `Leases/`, **no** `Locks/`.

---

## 2. What “referenced” actually means

### 2.1 The only product reference

```16:16:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
    <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
```

Siblings on the same csproj: EF Core Design 8.0.4, EF InMemory 8.0.4, Npgsql.EFCore 8.0.4. Those have types (`TraderDbContext`). Redis does not.

`Microsoft.Extensions.Caching.StackExchangeRedis` is **absent**. That omission is still correct: `IDistributedCache` cannot carry a fencing token (A03, A46).

### 2.2 Restore / copy graph (measured)

| Node | Evidence | Redis 2.8.0? |
|---|---|---|
| `src/Infrastructure/obj/project.assets.json` | `"StackExchange.Redis >= 2.8.0"` | Yes |
| `src/Infrastructure/bin/Debug/net8.0/TraderIntelligence.Infrastructure.deps.json` | top-level dependency `"StackExchange.Redis": "2.8.0"` | Yes |
| `apps/fix-worker/obj/project.assets.json` | transitive via Infrastructure | Yes |
| `apps/mt5-worker/obj/project.assets.json` | transitive via Infrastructure | Yes |
| `apps/fix-worker/bin/Debug/net8.0/TraderIntelligence.FixWorker.deps.json` | runtime `lib/net6.0/StackExchange.Redis.dll` fileVersion `2.8.0.27420` + `Pipelines.Sockets.Unofficial` | Yes |
| `apps/fix-worker/bin/Debug/net8.0/StackExchange.Redis.dll` | **on disk next to the exe** | Yes |

So a running `TraderIntelligence.FixWorker.exe` **loads the Redis client assembly** (or at least has it in the probe path). It never constructs a multiplexer. The DLL is dead weight.

Fix.CTrader does **not** reference Redis. The only lock type lives in the adapter and uses `System.Collections.Concurrent` only.

### 2.3 Product C# grep (2026-08-18)

Search roots: `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`. Glob `*.cs`.

| Token | Hits |
|---|---:|
| `using StackExchange.Redis` | **0** |
| `ConnectionMultiplexer` / `IConnectionMultiplexer` | **0** |
| `GetDatabase(` / `IDatabase` | **0** |
| `IRedisLease` / `RedisTradeSessionLease` / `ITradeSessionOwnership` | **0** |
| `IFencingTokenStore` / `FencingToken` (type) | **0** |
| `StringSet` / `ScriptEvaluate` / `Lua` | **0** |
| `AddStackExchangeRedis` / `REDIS_URL` / `ConnectionStrings:Redis` in product `.cs` / worker `appsettings` | **0** |
| `FixSessionOwnership` / `InMemoryDistributedLockWithFencing` / `IDistributedLockWithFencing` | **all 6+ hits in one file** — `src/Fix.CTrader/Services/FixSessionOwnership.cs` |

`src/Application` has **zero** lease/Redis ports.

---

## 3. What workers actually do (no lease)

### 3.1 Shared boot

Both hosts:

```csharp
builder.Services.AddTraderIntelligence(builder.Configuration);
builder.Services.AddHostedService<Worker>();
// EnsureCreated + DemoSeeder
host.Run();
```

`AddTraderIntelligence` (`src/Infrastructure/DependencyInjection.cs`) registers:

- `TraderDbContext` (InMemory if connection empty / `<SECRET>` / missing — **worker `appsettings.json` have no connection string**)
- two `FakeMt5BrokerConnector` singletons
- `EfTradingStore`, `EfDashboardQueries`, reconstructor, scorer, ingest

It does **not** register:

- `IConnectionMultiplexer`
- `FixSessionOwnership`
- `IDistributedLockWithFencing`
- `CTraderFixOptions`
- any hosted lease loop

`fix-worker` **project-references** `Fix.CTrader` but `Program.cs` / `Worker.cs` do not `using` any type from that assembly (C07). The lease stub is not even constructed.

Worker `appsettings.json` / `appsettings.Development.json` are logging-only (C07 hash `AB16B7B7…FF33`). No `Redis`, no `REDIS_URL`, no `TRADE_LEASE_*`.

### 3.2 `apps/fix-worker` — heartbeat, not owner

```19:47:D:\Prop\apps\fix-worker\Worker.cs
        var real = _config.GetValue("CTrader:RealCopyExecutionEnabled", false);
        _logger.LogInformation("FIX worker started. REAL_COPY_EXECUTION_ENABLED={Enabled}. NewOrderSingle disabled unless explicitly enabled.", real);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
            var quote = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Quote);
            // LastInboundAt = now; Status = ReadyForMarketData
            var trade = db.FixSessionStates.SingleOrDefault(s => s.Qualifier == FixSessionQualifier.Trade);
            // LastInboundAt = now; Status = LoggedOn (both branches of the ternary)
            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        }
```

Missing vs A46 / A64 lease loop:

| Required step | Happens? |
|---|---|
| Mint boot `owner_instance_id` | **No** |
| Postgres mint fencing token | **No** |
| Redis bind `ti:fix:lease:{session_key}` | **No** |
| Renew ≤ ⅓ TTL | **No** |
| Set `OwnerHeld` / `OwnerInstance` | **No** |
| Refuse second replica | **No** |
| Yield + increment token on shutdown | **No** |
| Gate send on token | N/A — no send path (C07) |

Two copies of this worker on a shared Postgres both stamp the same two `fix_sessions` rows every 15 s. That is a **write collision on status theater**, not a lease conflict — they never open TRADE. After a naive session is added, the same boot path will dual-Logon.

### 3.3 `apps/mt5-worker` — ingest only

`Worker.cs` calls `DealIngestionService.SyncBrokerAsync` + `ReconstructionScoringService.RebuildTraderAsync` on a 30 s loop. Architecture §5 also allows Redis for **live score projection**. That is also unimplemented: scores go only to Postgres via `EfTradingStore`. mt5-worker is **not** a TRADE owner (A54 / A64). It still does not use the referenced Redis package for scores, locks, or heartbeats (`ti:hb:…` in A99).

### 3.4 Types that exist and are unused by either worker (lease-relevant)

| Type | Path | Why it matters |
|---|---|---|
| `FixSessionOwnership` | `src/Fix.CTrader/Services/FixSessionOwnership.cs` | Only ownership API on disk. Never constructed |
| `InMemoryDistributedLockWithFencing` | nested in the same file | Dev stub. Not Redis |
| `FixSessionState.OwnerHeld` / `OwnerInstance` | `src/Domain/Entities/FixSessionState.cs` | Mapped on `fix_sessions`. Never written by seeder or worker |
| `CTraderFixOptions` | `src/Fix.CTrader/Configuration/CTraderFixOptions.cs` | Never bound; not a lease |

---

## 4. The in-memory stub is not A46

Full type: `TraderIntelligence.Fix.CTrader.Services.FixSessionOwnership`.

Comments on the type itself admit the gap:

```11:14:D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs
/// In production, the lock implementation should be backed by Redis and return a
/// monotonically increasing fencing token (to prevent split-brain execution).
```

```35:38:D:\Prop\src\Fix.CTrader\Services\FixSessionOwnership.cs
    /// Simple fallback implementation for development/unit tests.
    /// Replace with a Redis-backed lock in real deployments.
```

### 4.1 What it does

- Nested port `IDistributedLockWithFencing`: `TryAcquireAsync` / `ReleaseAsync`.
- `InMemoryDistributedLockWithFencing` stores `(ownerId, fencingToken, expiresAt)` in a **process-local** `ConcurrentDictionary`.
- Token is `Interlocked.Increment` of a **process-local** `long`.
- `FixSessionOwnership.AcquireAsync` is **one-shot**. No renew. `ExecutionIntentsAllowed` requires `HasOwnership && MarkReconciled()`.

### 4.2 Why it fails §28 even if a worker called it

| A46 / §28 rule | In-memory stub |
|---|---|
| Two processes must not both own TRADE | Each process has its own dictionary → **both acquire** |
| Postgres is the **only** token mint | Token is RAM; restart resets to 0 |
| Redis key `ti:fix:lease:{env}:{broker}:{account}:{qualifier}` | No Redis key |
| Lua bind: SET PX only if absent or same instance | Dictionary overwrite after local TTL |
| Lua renew: PEXPIRE if instance+token match | **No renew API.** Same owner calling `TryAcquire` again while unexpired gets **`acquired=false`** |
| Release increments Postgres token (fence the loser) | Release deletes the dict entry; token not bumped |
| Fail closed if Redis down | No Redis; always “up” |
| `TRADE_OWNERSHIP_ALLOW_DB_ONLY` default false | N/A |
| Do not use Redlock / `IDistributedCache` | Avoided by absence |
| Port lives in Application | Nested inside the adapter (layering defect; B05) |

This stub is acceptable **only** as a unit-test double *after* the Application port exists. Using it as production “fencing” is a split-brain. It is **not used at all** today.

B05 already classified the file **EXISTS_NEEDS_REFACTOR**. C27 confirms: still unused, still not Redis, hash unchanged (`30029E29…DED5E6D20`).

---

## 5. Database is also not a lease

### 5.1 `fix_sessions` owner columns

```22:24:D:\Prop\src\Domain\Entities\FixSessionState.cs
    public bool OwnerHeld { get; set; }
    public string? OwnerInstance { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
```

`TraderDbContext` maps the entity to `fix_sessions` with `HasKey(Id)` and a **unique index on `Qualifier` only** (C06: globally unique qualifier is the wrong identity). **No** `fencing_token`, **no** `leased_until`, **no** `session_key`.

`DemoSeeder` inserts QUOTE `ReadyForMarketData` and TRADE `LoggedOn` with live host/ports/`cServer` and **does not set** `OwnerHeld` / `OwnerInstance` (they stay `false` / `null`).

`EfDashboardQueries.GetOverviewAsync` / `GetFixSessionsAsync` treat `LoggedOn` / `ReadyForMarketData` as healthy. They do **not** read owner fields or Redis. Dashboard “FIX healthy” is independent of any lease (C07 ops-lie).

### 5.2 `fix_session_leases` (A46 §5.1)

**Table does not exist.** No entity, no `DbSet`, no migration. A25/A46 named it. A101 still lists it as missing. Postgres cannot mint a fence today.

A46 conjunction (binding; unimplemented):

```text
may_own_trade_socket =
    Postgres row says I am owner
    AND my fencing_token == Postgres current token
    AND Redis key exists
    AND Redis value.instance_id == me
    AND Redis value.fencing_token == Postgres current token
    AND Redis PTTL >= min_remaining
```

Current tree implements **0 / 6** conjuncts.

---

## 6. Lab Redis vs product Redis

### 6.1 Compose

```11:28:D:\Prop\docker-compose.yml
  redis:
    image: redis:7
    ports:
      - "6379:6379"

  api:
    ...
    depends_on:
      - postgres
      - redis
```

No `--save ""`, no `--appendonly no`, no memory cap (A65 recommended those). **No** `REDIS_URL` env on `api`. **Workers are not compose services** (A54: mt5-worker stays on Windows). Starting compose brings up an unused Redis that no C# process connects to.

### 6.2 API health theater

```31:31:D:\Prop\apps\api\Program.cs
    redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" },
```

This is a constant, not a `PING`. It proves the author knew Redis is unused. It is **not** a lease health signal. A99 `ti:hb:…` worker heartbeats do not exist.

API `appsettings.json` has empty `ConnectionStrings:TraderIntelligence` and **no** Redis connection key. `CTrader:RealCopyExecutionEnabled` is false (send still off by absence — C07).

---

## 7. Architecture vs tree (scoreboard)

Architecture §5 (`MT5_XAUUSD_Trader_Intelligence_cTrader_FIX44_Architecture_v2.md`):

```text
Redis is for:
  - live scores
  - short-lived cache
  - distributed execution-session ownership
  - short-lived locks
  - live dashboard data

Do not use Redis as the authoritative store for orders, positions, or balances.
```

Architecture §28: Redis lease **with fencing token** is one legal single-active mechanism; **the database remains the authority for execution state.**

| Allowed Redis family (A99) | Implemented? | Worker use? |
|---|---|---|
| `ti:fix:lease:{session_key}` | **No** | **No** |
| `ti:score:…` live score projection | **No** | mt5-worker writes Postgres only |
| `ti:quote:…` quote cache | **No** | seeder/heartbeat write Postgres |
| `ti:dash:…` | **No** | API hits EF |
| `ti:lock:…` non-execution mutex | **No** | — |
| `ti:hb:…` worker heartbeat | **No** | — |
| `ti:ops:events` pub/sub | **No** | — |
| Forbidden `order:` / `position:` / `balance:` documents | **Not written** | Vacuous compliance |

A46 planned types still **absent**: `FencingToken`, `OwnerInstanceId`, `SessionOwnershipKey`, `ITradeSessionOwnership`, `IFencingTokenStore`, `IRedisLease`, `TradeSessionOwnershipCoordinator`, `TradeSessionSendGate`, `PostgresFencingTokenStore`, `RedisTradeSessionLease`.

---

## 8. Risk if someone “just adds TRADE”

Today: **SAFE_BY_ABSENCE** of a FIX initiator and of `35=D` (C07). The unused Redis package does not change that.

The day `CTraderTradeSession` is constructed from `fix-worker` **without** A46:

1. Replica count is not a control (laptop diagnostic, second deploy, forgotten staging box).
2. In-memory lock will not serialize two processes.
3. `fix_sessions.Qualifier` unique index will not serialize owners; both rows already exist and both workers update them.
4. cServer FAQ: every ExecutionReport is copied to every live TRADE connection → duplicate fills, broken dest book (A46 §0).
5. A Redis-only lock without a Postgres fence is still **unsafe** (GC pause / partition: old owner wakes with a live socket). Do not implement “just SET NX” as the fix.

P0 when TRADE exists; **not** an incident today. Treat lease bugs as equal to a double-send (A25).

---

## 9. What “done” looks like (do not implement in this task)

This report does **not** authorize product edits. When a later increment implements ownership, A46 + A99 win:

1. Application ports first (`ITradeSessionOwnership`, `IRedisLease`, `IFencingTokenStore`). Move the nested lock interface **out** of `Fix.CTrader`.
2. Postgres `fix_session_leases` via a versioned migration (not `EnsureCreated`). Postgres mints the token.
3. `RedisTradeSessionLease` in Infrastructure: A46 Lua only; `StackExchange.Redis` 2.8.0; **no** generic `StringSet`.
4. Hosted lease loop in `fix-worker` (not `Program.cs` god-code). mt5-worker does **not** take the TRADE lease.
5. Send / Logon / reconnect require the A46 conjunction. Redis down ⇒ **no** send (`TRADE_OWNERSHIP_ALLOW_DB_ONLY=false`).
6. Tests: two instances, steal after TTL, fenced send dropped, release does not `DEL` a foreign key. `tests/Fix` does not exist yet (B05).
7. Keep Redis off the order/position/balance path.

Until then, the package line in the Infrastructure csproj is **intent only**.

---

## 10. Related reports (do not delete; use the later file for adjacent questions)

| Report | Relationship |
|---|---|
| A03 / B03 | Package present, implementation missing — confirmed |
| A05 / B05 | `FixSessionOwnership` EXISTS_NEEDS_REFACTOR — hash unchanged |
| A08 / B07 / C07 | Workers are demo loops; C07 = send-off; **this file = lease-off** |
| A25 / A46 | Binding lease protocol; **0% implemented** |
| A64 | Pipeline said “Redis optional later; unused” — A46 made Redis+fence **required** for TRADE |
| A65 / B37 | Compose Redis exists; unused by product |
| A99 | Key catalog; still “Not implemented” |
| A101 | Live FIX acceptance still FAIL; in-memory fence unused |

---

## 11. One-line pin

**`StackExchange.Redis` 2.8.0 is restored onto both workers; no multiplexer, no lease, no fence. `FixSessionOwnership` is an unused in-process dictionary. §28 is not implemented.**
