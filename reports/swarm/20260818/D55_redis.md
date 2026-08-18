# D55 — Is `StackExchange.Redis` used?

| Field | Value |
|---|---|
| Agent | D55 |
| Date | 2026-08-18 |
| Assigned question | `StackExchange.Redis` used? |
| Artifact | `D:\Prop\reports\swarm\20260818\D55_redis.md` |
| Product source modified | **No** |
| Method | Grep of `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests` (`*.cs`, `*.csproj`, `*.props`, host `appsettings*.json`). Full read of Infrastructure csproj + DI, API host + `SettingsController`, both workers, `FixSessionOwnership`, compose, A99 key catalog. SHA-256 of evidence files. Restore/copy graph from `project.assets.json` + host `deps.json` + on-disk DLLs. |
| Binding law | Architecture v2 §5 (Redis coordination / cache, **not** SoT), §28 (TRADE lease + fence), §55 (no secrets in keys/values), §62 (fail closed if DB down) |
| Binding siblings | A03/B03/D03 (infra), A46 (lease spec), A49 (flags), A65/B37 (compose), A99 (key allow-list), C27 (lease gap), D23 (DI), D06 (API census) |
| Supersedes for *this question* | A03 / A99 / C27 / D03 / D23 / D06 claims of **0** product `using StackExchange.Redis` / **0** `StringSet` / “API has no `Controllers/`” / “host does not use Redis types”. **Re-measured 2026-08-18 after `SettingsController.cs` landed.** C27 remains correct on **workers + lease**. |

**Honesty rule:** a NuGet pin is not usage. A DLL next to an exe is not a multiplexer. A `using` on an unmapped controller is not a live Redis client. `depends_on: redis` is not a connection. `ConnectionStrings:Redis` is not a read. Hard-coded `/api/health` `redis.healthy=false` is theater.

---

## 0. Verdict

**Package present. One orphaned source-level use. No runtime multiplexer. No lease. Not used by the running hosts.**

| Question | Answer |
|---|---|
| Is `StackExchange.Redis` referenced? | **Yes.** Sole product `PackageReference` is `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` pin **2.8.0**. |
| Is it restored onto host output? | **Yes.** `StackExchange.Redis.dll` (fileVersion `2.8.0.27420`, 902 144 B) sits next to API + both worker Debug exes. Transitive via Infrastructure. |
| Does any product `.cs` `using StackExchange.Redis`? | **Yes — 1 file.** `D:\Prop\apps\api\Controllers\SettingsController.cs`. |
| Is there a live `ConnectionMultiplexer` / DI registration? | **No.** `AddTraderIntelligence` does not register `IConnectionMultiplexer`. No host constructs one. `GetConnectionString("Redis")` is **never called**. |
| Is `SettingsController` on the HTTP surface? | **No.** API is a minimal-API host. **0** `AddControllers` / `MapControllers`. Live `GET /api/settings` is the `Program.cs` stub (no Redis). |
| Do workers use Redis? | **No.** Ingest + `fix_sessions` heartbeat only. |
| Is A46 `RedisTradeSessionLease` / Lua SET-PX implemented? | **No.** `FixSessionOwnership` is still an unused in-process `ConcurrentDictionary`. |
| Competing Redis clients (`ServiceStack.Redis`, `NRedisStack`, `FreeRedis`, `CSRedis`, `RedLock.net`, `Microsoft.Extensions.Caching.StackExchangeRedis`)? | **None.** |
| Tests? | **0** hits under `D:\Prop\tests`. |
| Redis as SoT for orders / positions / balances? | **Not written today** (controller unreachable). Vacuous ≠ designed. |
| If `MapControllers` + multiplexer were added as-is? | **UNSAFE vs A99.** `Put` does generic `StringSetAsync` of `settings:*` keys (no `ti:` prefix, no env, no TTL). GET still reads `IConfiguration`, not Redis. |

One-liner: **`StackExchange.Redis` 2.8.0 is pinned and copied; the only C# use is a dead MVC controller; nothing connects to Redis at runtime.**

Classification:

| Slice | Class |
|---|---|
| `StackExchange.Redis` 2.8.0 as the *client* choice | **EXISTS_AND_GOOD** (raw client; `IDistributedCache` correctly omitted) |
| Central `Directory.Packages.props` pin (A102) | **MISSING** — version is local to Infrastructure csproj |
| Multiplexer + options + `GetConnectionString("Redis")` | **MISSING** |
| Redis façade / A99 allow-list in code | **MISSING** |
| `SettingsController` | **EXISTS_NEEDS_REFACTOR** — compiled, unmapped, DI-unresolvable, writes forbidden keys |
| `Program.cs` `GET /api/settings` stub | **EXISTS_NEEDS_REFACTOR** — live route; ignores Redis and the controller |
| A46 TRADE/QUOTE lease + fence | **MISSING** |
| Worker use of any Redis API | **MISSING** |
| Compose `redis:7` + `/api/health` redis object | **THEATER** — container unused by product code |
| Redis as SoT for orders / positions / balances | **not UNSAFE today** (no write path reaches Redis) |
| `settings:*` StringSet (if wired) | **UNSAFE** vs A99 / §5 (generic SET, not an allow-listed family; settings become a Redis book) |

Do **not** treat the Redis DLL as §28. Do **not** `MapControllers` this `SettingsController` as-is. Do **not** add a TRADE initiator until A46 is implemented.

---

## 1. Inventory (source of truth; exclude `bin/` / `obj/` unless noted)

Hashes measured 2026-08-18 (`Get-FileHash -Algorithm SHA256`).

| Path | Bytes | SHA-256 | Role vs Redis |
|---|---:|---|---|
| `src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | 1035 | `4DABF29CA934261CFC46C72514CB7AA04D5E8F9CC8FFAC1BA051BF0CD0668EED` | **Only** product `PackageReference` of `StackExchange.Redis` 2.8.0. Unchanged vs C27. |
| `src/Infrastructure/DependencyInjection.cs` | 1900 | `EF0E0E466A23F7244F3DA9BC6BF46529949237BA75FC251D810C4AA88DA7A380` | EF + Fake MT5 only. **No** multiplexer. Unchanged vs C27. |
| `apps/api/Controllers/SettingsController.cs` | 3732 | `B19274DC71F6BECB54B6C1A270D3CA9F47C9B1CCD340D0C303F4C663CA50C23F` | **Only** product `using StackExchange.Redis`. **New vs C27/D06.** |
| `apps/api/Program.cs` | 4731 | `61B1E0D105C1C998FD0449BE1C29325399BC1085B1EBB3C77115D2C8A322F58E` | Minimal APIs. Hard-codes `redis.healthy = false`. Same SHA as D06. **No** `AddControllers`. |
| `apps/api/TraderIntelligence.Api.csproj` | 803 | `A5868FA8BF8C717946FA332B0669BEE8551043AF3A05272942196E36619ED999` | Refs Infrastructure. **No** direct Redis package. |
| `apps/api/appsettings.json` | 1254 | `69D41CAD33EDFDE3B76B53F708C124CEACFF2B1F7706EB86F5EC9EFA5984AD20` | `ConnectionStrings:Redis` = `localhost:6379`. Grew after D06 (was 431 B / `8DCE4CBE…`). |
| `apps/api/appsettings.Development.json` | 478 | `81B5E6DC0290CB48038DD67C6F9C37851C16F8362A6350BD1A43D9B27E8B0481` | Same Redis key. Grew after D06 (was 127 B). |
| `apps/mt5-worker/Program.cs` | 859 | `2FACC25C7E9E9E251AEDEE9C2AB0C34AE804CBB9B02B1E30715693933F870A79` | `AddTraderIntelligence` + seeder. |
| `apps/mt5-worker/Worker.cs` | 1882 | `5749970025C357A282A0A950D3D113E65A1FE9808A44EF699E9E469E73ECB92B` | 30 s Fake ingest + score. **No** Redis. |
| `apps/fix-worker/Program.cs` | 859 | `05732C24D12C8012A493553299E19AF8C7BF126EF48B15D5FD36AFFFF79BD7CC` | Same boot. |
| `apps/fix-worker/Worker.cs` | 2093 | `92A8F492D1F1F6B5627EA4B3389D8D4D80F8B48C1B6835A22916ECB5B660B0E2` | 15 s `fix_sessions` heartbeat. **No** acquire/renew. |
| `src/Fix.CTrader/Services/FixSessionOwnership.cs` | 4719 | `30029E29EE66C2114643AAF8FD0E0D8566C075A0FF693CCA7043CEADED5E6D20` | In-memory fence stub. Comment: “Replace with a Redis-backed lock.” |
| `src/Fix.CTrader/TraderIntelligence.Fix.CTrader.csproj` | 419 | `0AD91D39D5B6802E3F04EAEDDB71E3C0E4770691864931C98324F78900E8609F` | **No** Redis package (correct). |
| `docker-compose.yml` | 687 | `1ED8787F0F7602429A65CDBFA21EF44F8727F222F28E56109F54DBDFF59C35A1` | `redis:7` on 6379. API `depends_on: redis`. Workers not in compose. |
| `Directory.Build.props` | 269 | `5ACD33B0F8E1A8D2E66956EF2B04A11E321661A5E3297F8F5C13051345562DD0` | No `PackageVersion`. `Directory.Packages.props` **does not exist**. |

Infrastructure product `.cs` (still **no** `Redis/` / `Leases/` / `Locks/`): `DependencyInjection.cs`, `Dashboard/EfDashboardQueries.cs`, `Persistence/TraderDbContext.cs`, `Persistence/EfTradingStore.cs`, `Persistence/Configurations/ReconstructedTradesConfiguration.cs`, `Seeding/DemoSeeder.cs`. **Zero** Redis types in this project.

Application has **zero** `IRedisLease` / `IConnectionMultiplexer` / Redis ports.

---

## 2. What “referenced” actually means

### 2.1 The only product package pin

```16:16:D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj
    <PackageReference Include="StackExchange.Redis" Version="2.8.0" />
```

Walk of every product `*.csproj` / `*.props` under `D:\Prop` (exclude `bin`/`obj`/`node_modules` / report `_tmp_*`): **this is the only hit**.

Siblings on the same csproj: EF Core Design 8.0.4, EF InMemory 8.0.4, Npgsql.EFCore 8.0.4. Those have types (`TraderDbContext`). Redis has **no** Infrastructure type.

`Microsoft.Extensions.Caching.StackExchangeRedis` is **absent**. That omission is still correct: `IDistributedCache` cannot carry a fencing token (A03, A46).

`Directory.Packages.props` is **MISSING** (C56). Version **2.8.0** is not centrally pinned.

### 2.2 Restore / copy graph (measured)

| Node | Evidence | Redis 2.8.0? |
|---|---|---|
| `src/Infrastructure/obj/project.assets.json` | `"StackExchange.Redis >= 2.8.0"` / compile+runtime `lib/net6.0/StackExchange.Redis.dll` | Yes |
| `src/Infrastructure/bin/Debug/net8.0/TraderIntelligence.Infrastructure.deps.json` | top-level `"StackExchange.Redis": "2.8.0"` | Yes |
| `src/Infrastructure/bin/Debug/net8.0/StackExchange.Redis.dll` | class-lib output does **not** copy lockfile assemblies | **Absent** (expected) |
| `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.deps.json` | runtime `lib/net6.0/StackExchange.Redis.dll` fileVersion **`2.8.0.27420`**; package sha512 `MjAJ0ejH8zLhtuN5+Z+/I07NmPGdVuGEvE2+4xONQoFwgl+7vbQ/A6jlUgH9UkZb4s9Mu9QDyBq1TkRqQcOgTQ==` | Yes |
| `apps/api/bin/Debug/net8.0/StackExchange.Redis.dll` | 902 144 B, SHA-256 `19E3AB7C6AC6FCA7A597A6FC6E11794DC427795CB8572B646F21035EB444B82D` | Yes |
| `apps/mt5-worker` + `apps/fix-worker` Debug `deps.json` + DLL | same fileVersion + same DLL hash | Yes |
| Transitive | `Pipelines.Sockets.Unofficial` 2.2.8 (Redis package dependency) | Present next to hosts |

So every host **can load** the client assembly. None construct it.

API csproj does **not** reference Redis directly. `SettingsController` compiles because the Web SDK project references Infrastructure, which flows the compile asset.

---

## 3. Product C# grep (2026-08-18)

Search roots: `D:\Prop\src`, `D:\Prop\apps`, `D:\Prop\tests`. Glob `*.cs`. Exclude `bin/` / `obj/`.

| Token | Hits | Where |
|---|---:|---|
| `using StackExchange.Redis` | **1** | `apps/api/Controllers/SettingsController.cs:2` |
| `IConnectionMultiplexer` | **3** (field + ctor + param) | same file only |
| `ConnectionMultiplexer.Connect` / `ConnectionMultiplexer.ConnectAsync` | **0** | — |
| `GetDatabase(` | **1** | `SettingsController.Put` |
| `StringSetAsync` | **6** | same `Put` |
| `StringGet` / `HashSet` / `ScriptEvaluate` / Lua | **0** | — |
| `IRedisLease` / `RedisTradeSessionLease` / `ITradeSessionOwnership` | **0** | — |
| `IFencingTokenStore` | **0** | — |
| `AddStackExchangeRedis` / `AddStackExchangeRedisCache` | **0** | — |
| `GetConnectionString("Redis")` / `REDIS_URL` | **0** | — |
| `AddControllers` / `MapControllers` | **0** | entire product tree |
| `FixSessionOwnership` / `InMemoryDistributedLockWithFencing` | all in one file | `src/Fix.CTrader/Services/FixSessionOwnership.cs` |

`GetConnectionString` in product `.cs`: **one** call, `DependencyInjection.cs:19`, name **`"TraderIntelligence"`** (Postgres) or `DATABASE_URL`. The appsettings key `ConnectionStrings:Redis` is **unread**.

---

## 4. The only source-level use — dead controller

`D:\Prop\apps\api\Controllers\SettingsController.cs` (94 lines):

- `[ApiController]` `[Route("api/settings")]`.
- Ctor **requires** `IConnectionMultiplexer` even for GET (GET never touches `_redis`).
- GET reads `IConfiguration` (`RiskEngine:*`, `FeatureFlags:*`) and returns JSON. **No Redis read.**
- PUT calls `_redis.GetDatabase()` then six `StringSetAsync` with **no expiry**:

```text
settings:risk:max_daily_drawdown_pct
settings:risk:max_position_size
settings:risk:max_open_positions
settings:flags:shadow_trading
settings:flags:live_copy
settings:flags:auto_promotion
```

Why this is **not** “used” at runtime:

1. `Program.cs` never calls `AddControllers()` or `MapControllers()`. ASP.NET will not construct this type.
2. `IConnectionMultiplexer` is not in the service collection. If someone mapped controllers tomorrow, the first request would throw `InvalidOperationException` (unable to resolve `IConnectionMultiplexer`).
3. The **live** settings route is the minimal-API stub, which does not mention Redis:

```42:47:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", () => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool> { ["REAL_COPY_EXECUTION_ENABLED"] = false },
    brokerConfigs = new[] { new { id = "ACHIEVER", name = "Achiever", enabled = true }, new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true } }
}));
```

D06 (same `Program.cs` SHA) recorded **no** `Controllers/` folder. That census is **stale on the tree**, not on the route table: the folder exists now; the host still does not map it.

### 4.1 A99 / §5 if this PUT were ever reached

A99 allow-list families: `ti:score`, `ti:quote`, `ti:fix:lease`, `ti:dash`, `ti:lock`, `ti:hb`, channel `ti:ops:events`. Grammar requires prefix `ti:` and an environment token. Façade **must not** expose generic `StringSet`.

`settings:*` fails all of that:

- no `ti:` prefix
- family `settings` is not on the allow-list
- no `live|demo|staging`
- no TTL (`StringSet` is sticky; A65 lab Redis is `allkeys-lru` / no AOF — still the wrong book)
- PUT writes Redis; GET reads config — **split brain** even if wired
- risk limits / feature flags as a Redis document would make Redis the settings SoT (A49 flags belong in config + Postgres, not a generic SET)

So the only C# “use” is also the wrong use. Do not ship it.

---

## 5. DI / health / compose — still unused

`AddTraderIntelligence` (`src/Infrastructure/DependencyInjection.cs`) registers DbContext (InMemory unless `ConnectionStrings:TraderIntelligence` / `DATABASE_URL` is real), two `FakeMt5BrokerConnector`s, store, dashboard queries, reconstructor, scorer, ingest. It does **not** register:

- `IConnectionMultiplexer`
- any Redis options binder
- `FixSessionOwnership`
- a typed Redis façade

API `GET /api/health` (not a real check):

```csharp
redis = new { name = "redis", healthy = false, lastCheck = DateTimeOffset.UtcNow, details = "not required for demo" }
```

`GET /health` is `{ status = "ok" }` only. `GET /ready` counts EF `Brokers`. Neither pings Redis.

`docker-compose.yml` runs `redis:7` and `api depends_on: redis`. That is a lab process, not a client. Workers are **not** in compose.

Worker `appsettings.json` files have **no** `ConnectionStrings` and **no** Redis key.

---

## 6. Workers still do not use a lease (C27 still holds)

| Host | What the loop does | Redis? |
|---|---|---|
| `apps/mt5-worker` | `DealIngestionService` + `ReconstructionScoringService` every 30 s on Fake connectors | **No** |
| `apps/fix-worker` | Stamp `FixSessionState` rows `Disconnected` every 15 s; refuse `NewOrderSingle` | **No** |

`Fix.CTrader` correctly has **no** Redis package. The stub lock lives in the adapter and uses `System.Collections.Concurrent` only. It is **not** registered. Tests do **not** construct it.

§28 dual-owner protection is **MISSING**. Today that is **SAFE_BY_ABSENCE** of a TRADE socket, not a lease. Two `fix-worker` replicas after a naive TRADE Logon would both own the session (C27).

---

## 7. Drift vs earlier same-day reports

| Claim (earlier file) | This re-measure |
|---|---|
| A03 / C27 / D03 / D23: **0** `using StackExchange.Redis` | **1** file (`SettingsController`) |
| A99: **0** `StringSet` in product `.cs` | **6** `StringSetAsync` (unreachable) |
| D06: **no** `Controllers/` folder; host “does not use Redis types” | Folder exists; types used in unmapped controller; **runtime** claim still true |
| C27: workers acquire no lease | **Unchanged. Still true.** |
| D06 `appsettings.json` 431 B / `8DCE4CBE…` | Now 1254 B / `69D41CAD…` (`ConnectionStrings:Redis` + `RiskEngine` + `FeatureFlags`) |
| C27 `apps/api/Program.cs` 4658 B / `E914FA98…` | Now 4731 B / `61B1E0D1…` (matches D06). Still no multiplexer. |

Use **this file** for “is StackExchange.Redis used?”. Use **C27** for the lease question. Use **A99/A46** as the implementation contract when a façade is written.

---

## 8. What “used” must mean before anyone marks this EXISTS_AND_GOOD as a *feature*

All of the following, in order:

1. Typed Infrastructure façade that can **only** write A99 families (`ti:score`, `ti:quote`, `ti:fix:lease`, `ti:dash`, `ti:lock`, `ti:hb`, `ti:ops:events`). **No** generic `StringSet` helper.
2. Single `IConnectionMultiplexer` singleton registered from `ConnectionStrings:Redis` / `REDIS_URL` (secret via env, not committed AUTH).
3. A46 Lua lease + Postgres fencing token **before** any TRADE socket.
4. Delete or replace `SettingsController` PUT. Settings / flags are not a Redis book (A49).
5. Real `/ready` Redis PING (A77), not a hard-coded `healthy: false`.
6. Tests: façade reject of `settings:` / `order:` keys; lease acquire/renew/release; host does not start TRADE without fence.

Until then the honest answer to the assigned question is:

**Referenced: yes (2.8.0). Compiled use: one dead controller. Runtime use: no.**
