# A014 — Live path now (post-rewrite re-read)

| Field | Value |
|---|---|
| Agent | A014 |
| Date | 2026-08-18 |
| Role | Senior engineer, evidence re-read after live-path rewrite |
| Assigned | Re-read `Program.cs`, `NativeMt5BrokerConnector.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `Api.csproj`. Confirm: DemoSeeder gone from API startup; `net8.0-windows`; `GroupRequestArray` / `UserRequestArray`; no `Take(200)`; no `35=D`. Quote evidence. No secrets. |
| Product source | **Not modified** by this agent. Report only. |

**Honesty rule:** quote the files as they sit on disk *after* the rewrite. Do not inherit A001/A002/A005 conclusions. Partial passes stay partial.

---

## Verdict (measured, 2026-08-18)

| Claim | Result | Evidence |
|---|---|---|
| DemoSeeder gone from **API startup** | **PASS** | `Program.cs` startup seed is `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| API TFM `net8.0-windows` x64 | **PASS** | `TraderIntelligence.Api.csproj` L18–19. Infra + Mt5 match. Application/Domain stay portable `net8.0` (expected). |
| Groups via `GroupRequestArray` | **PASS** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*", arr)` first; cache `GroupNext` only if that list is empty. |
| Users via `UserRequestArray` | **PASS** | `ReadAccountsForGroup` L223 `UserRequestArray(gname, users)`; fallbacks `UserGetByGroup` / `UserLogins` + `UserRequestByLogins`. |
| No `Take(200)` on **ingest / positions** | **PASS** | `DealIngestionService` has **zero** `Take(` calls. Positions go through `GetGroupPositionsAsync("*")` or every account. |
| No `Take(200)` **anywhere on the API host** | **FAIL (narrow)** | `GET /api/trades` still `OrderByDescending(t => t.OpenedAt).Take(200)` (`Program.cs` L107). Dashboard read cap, not Manager enumeration. |
| No live `35=D` | **PASS — `SAFE_BY_ABSENCE`** | No `35=D` / `(35, "D")` / `MsgType="D"` builder in assigned files or `Fix.CTrader` Sessions. Copy flag hardcoded `false`. |

**Bottom line:** the running API live path is now **native Manager ingest + catalog seed**, not FakeMt5 / DemoSeeder / 4 demo logins. The Manager64 host TFM is correct. Request APIs are wired. Ingest no longer silently drops accounts after 200. **Live NewOrderSingle is still impossible.** The only leftover `Take(200)` is the reconstructed-trades HTTP page.

`DemoSeeder.cs` **still exists** (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`) and tests may still call it. That is **not** API startup.

---

## 1. API startup — DemoSeeder is gone

### 1.1 What `Program.cs` actually runs

Startup block after endpoint maps:

```149:154:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`.

`/api/ops/resync` no longer hardcodes `{10001, 10002, 10003, 99001}`. It walks `ACHIEVER` + `STARWAVEFX` catalog + `store.ListLoginsAsync`:

```111:147:D:\Prop\apps\api\Program.cs
app.MapPost("/api/ops/resync", async (
    DealIngestionService ingestion,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    // ...
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        // SyncCatalogAsync → SyncBrokerAsync → ListLoginsAsync → RebuildTraderAsync per login
    }
    return Results.Ok(result);
});
```

`/api/health` no longer advertises FakeMt5. It reports `LiveRuntimeStatus` broker rows (`live Manager groups=… accounts=… phase=…` or `LastError`).

### 1.2 DI refuses dummy when this host starts

```35:56:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        // ...
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
```

Connectors constructed: two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX) from env keys. Password **values are not quoted here**. Gate: both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` must be non-empty and not a `<SECRET>` / `(a/c` placeholder (`LiveMt5Registration.IsSecret`).

### 1.3 What `BrokerCatalogSeed` writes (no fake tape)

Idempotent catalog only: two `Brokers` rows if missing, one `CanonicalInstrument` (XAUUSD), one `KillSwitch`, two `FixSessionState` rows (`Disconnected`, TRADE `LastError` = “NewOrderSingle off”). **No** demo logins 10001–10003, **no** canned deals, **no** `LoggedOn` forge.

`DemoSeeder` class remains on disk for tests (`public static class DemoSeeder` at `Seeding\DemoSeeder.cs` L14). Integration tests under `D:\Prop\tests` still call `DemoSeeder.SeedAsync`. **API process does not.**

---

## 2. TFM — API is `net8.0-windows` x64

```17:22:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

Native stack matches (required to load Manager64):

| Project | TFM | Platform |
|---|---|---|
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | `net8.0-windows` | x64 |
| `D:\Prop\src\Infrastructure\TraderIntelligence.Infrastructure.csproj` | `net8.0-windows` | x64 |
| `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` | `net8.0-windows` | x64 |
| `D:\Prop\src\Application\TraderIntelligence.Application.csproj` | `net8.0` | (none) |
| `D:\Prop\src\Domain\TraderIntelligence.Domain.csproj` | `net8.0` | (none) |

Connector also fail-closes off Windows:

```66:67:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");
```

A002’s “API is still `net8.0`” is **stale**. This re-read supersedes it for the API host.

---

## 3. Native connector — request arrays, not cache-only

Connect: pump `GROUPS|USERS|POSITIONS` first; on failure, `PUMP_MODE_NONE` and keep request APIs (`m_pumpMode` equivalent = `_pumpEnabled`).

### 3.1 Groups — `GroupRequestArray("*")`

```152:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var arr = _manager!.GroupCreateArray();
            try
            {
                var res = _manager.GroupRequestArray("*", arr);
                if (res == MTRetCode.MT_RET_OK || res == MTRetCode.MT_RET_OK_NONE)
                {
                    for (uint i = 0; i < arr.Total(); i++)
                    {
                        var g = arr.Next(i);
                        if (g is null)
                            continue;
                        AddGroup(list, seen, g);
                    }
                }
            }
            finally { arr.Release(); }

            if (list.Count == 0)
            {
                // fallback: GroupTotal / GroupNext cache walk
            }
```

A001 (“Zero hits for `GroupRequestArray` under `src`”) is **stale**.

### 3.2 Users — `UserRequestArray` (+ account array)

```223:237:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
            }

            var acctReq = _manager.UserAccountRequestArray(gname, accounts);
```

Also implemented (not cache-only): `DealRequest`, `DealRequestByGroup`, `PositionRequest`, `PositionRequestByGroup` (then `PositionGetByGroup`). Deals are windowed in 14-day slices (`Windows`).

Connector implements `IMt5BulkDealReader` + `IMt5BulkPositionReader` so ingest can avoid per-login loops when the native type is registered.

---

## 4. Ingest host + service — all logins, no account `Take(200)`

### 4.1 `LiveIngestHostedService`

After 2 s delay, for **every** `registry.All()` connector:

1. `ConnectAsync` + `IsConnectedAsync`
2. `SyncCatalogAsync` (groups + accounts batch)
3. `SyncBrokerAsync` (deals + positions)
4. `ListLoginsAsync` then **score every login** (progress every 50)
5. On exception: `Connected = false`, `Phase = failed`, log *“No dummy data will be substituted.”*

No Fake fallback. No 4-login allow-list.

### 4.2 `DealIngestionService` — `Take(` count = 0

```37:96:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
        // ...
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
        else
        {
            foreach (var account in accounts) { /* per-login DealRequest */ }
        }

        if (connector is IMt5BulkPositionReader posBulk)
        {
            var positions = await posBulk.GetGroupPositionsAsync("*", ct);
            await _store.ReplaceBrokerPositionsAsync(brokerId, positions, ct);
        }
        else
        {
            foreach (var account in accounts) { /* per-login PositionRequest */ }
        }
```

A005/A007 `accounts.Take(200)` on positions is **gone**. Native path uses group deal + group position request.

### 4.3 Residual `Take(200)` — HTTP trades only

```101:108:D:\Prop\apps\api\Program.cs
app.MapGet("/api/trades", async (TraderDbContext db, string? broker, long? login, CancellationToken ct) =>
{
    var query = db.ReconstructedTrades.AsQueryable();
    if (login.HasValue)
        query = query.Where(t => t.Login == login.Value);
    var rows = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
        query.OrderByDescending(t => t.OpenedAt).Take(200), ct);
    return rows;
});
```

This is a **read page**, not a Manager ACL cap. Filter by `login` still applies `Take(200)` after `OpenedAt` desc (newest 200 reconstructed trades). Broker query-string is unused. Do not claim “zero Take(200) in the tree.”

---

## 5. No `35=D`

Assigned files + FIX sessions:

| Location | What exists | Wire `35=D`? |
|---|---|---|
| `Program.cs` L68 | English: “NewOrderSingle still off” | No |
| `DependencyInjection` L40–41 | `RealCopyEnabled = false` comment + assignment | No |
| `BrokerCatalogSeed` TRADE `LastError` | “NewOrderSingle off” | No |
| `NativeMt5BrokerConnector` | Manager read APIs only; no `SendTrade` / dealer send | No |
| `LiveIngestHostedService` | Connect / catalog / deals / score | No |
| `DealIngestionService` | Persist + reconstruct + `PersistDemoShadowAsync` | No send |
| `Fix.CTrader` Sessions | **0** hits for `35=D` / `NewOrderSingle` | **`SAFE_BY_ABSENCE`** |
| `CTraderFixOptions.RealCopyExecutionEnabled` | Default **false** | Flag only |

`/api/settings` exposes `REAL_COPY_EXECUTION_ENABLED` from `runtime.RealCopyEnabled`, which DI pins **false**. Flipping an env flag cannot emit `35=D` because there is no builder.

Live send if this process starts now: Manager **read** + optional FIX **Logon `35=A`** (not proven in this file). **`35=D` cannot.**

---

## 6. Live path shape (honest)

```
API Process (net8.0-windows x64)
  ├─ EnsureCreated
  ├─ BrokerCatalogSeed          // brokers + FIX rows Disconnected; NOT DemoSeeder
  ├─ LiveMt5Registration        // 2× NativeMt5BrokerConnector; fail if passwords missing
  ├─ LiveIngestHostedService
  │    Connect (pump, else none)
  │    GroupRequestArray("*") → UserRequestArray(group)
  │    DealRequestByGroup / PositionRequestByGroup
  │    Upsert*Batch + score every stored login
  └─ CTraderFixLogonHostedService
       35=A only; NewOrderSingle still disabled
```

**Not proven by this re-read:** a successful Manager connect, a non-empty group list against live Achiever/Starwave, FIX TLS Logon, or Postgres (InMemory still used when `DATABASE_URL` / connection string is empty or contains `<SECRET>`).

---

## 7. Checklist vs assignment

- [x] DemoSeeder gone from **API startup** (file remains for tests)
- [x] `Api.csproj` `net8.0-windows` + `PlatformTarget` x64
- [x] `GroupRequestArray` + `UserRequestArray` on the live connector
- [x] Ingest `Take(200)` **removed**
- [ ] **Zero** `Take(200)` on the API host — **still L107 `/api/trades`**
- [x] No `35=D` (`SAFE_BY_ABSENCE`)
- [x] No secrets printed

A001 (no request arrays), A002 (API `net8.0` + DemoSeeder startup), A005 (ingest `Take(200)`) are **superseded** for those specific claims. `/api/trades` page cap and unused `DemoSeeder.cs` are remaining debt, not live-path blockers.
)
