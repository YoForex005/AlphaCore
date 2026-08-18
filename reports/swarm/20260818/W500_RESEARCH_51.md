# W500_RESEARCH_51 — Host `Program.cs`: DemoSeeder / FakeMt5 / 10001 / 10002 / dummy

| Field | Value |
|---|---|
| Slot | **51** |
| Agent | W500_RESEARCH_51 |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_51.md` |
| Product source modified | **No.** Read-only. This report is the only write. |
| Secrets printed | **None.** Password values not read, not copied. Config **key names** only. |
| Assigned | Search `Program.cs` for `DemoSeeder`, `FakeMt5`, `10001`, `10002`, `dummy`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager traders; copy to cTrader must **not** send live orders yet (no loss). |

**Honesty:** `A002_api_dummy_path.md` is **stale**. It quotes an older `apps/api/Program.cs` that called `DemoSeeder.SeedAsync` and advertised FakeMt5 on `/api/health`. The files on disk today do **not**. This report quotes the current tree only.

---

## 0. Verdict (do not greenwash)

**PASS on the assigned search.** Product host `Program.cs` files contain **zero** of the five dummy tokens. API startup no longer seeds FakeMt5 logins `10001`/`10002`. The live catalog path (API resync + `LiveIngestHostedService` + `LiveBrokerProbe`) asks Manager for **every** group and **every** login those two manager ACLs can see. Live `NewOrderSingle` (`35=D`) **does not exist**. Capital at risk from this process: **none**.

| Claim | Result | Class |
|---|---|---|
| `DemoSeeder` in product `Program.cs` | **0 hits** | **ABSENT** from hosts |
| `FakeMt5` in product `Program.cs` | **0 hits** | **ABSENT** from hosts |
| `10001` / `10002` in product `Program.cs` | **0 hits** | **ABSENT** from hosts |
| `dummy` in product `Program.cs` | **0 hits** | **ABSENT** from hosts |
| API still calls `DemoSeeder.SeedAsync` | **No** | startup = `BrokerCatalogSeed.EnsureAsync` only |
| `/api/ops/resync` still hardcodes `{10001,10002,10003,99001}` | **No** | scores `ListLoginsAsync` (all persisted accounts) |
| `/api/health` still says FakeMt5 | **No** | runtime `LiveRuntimeStatus` (groups/accounts/phase) |
| Dummy book still in the tree | **Yes** | `DemoSeeder.cs` + `FakeMt5BrokerConnector.cs` (tests) |
| Leftover dummy scorer | **Yes** | `apps/mt5-worker/Worker.cs` L31 still loops four fake logins |
| Live census of ALL groups/traders | **Measured 2026-08-18T08:42:16Z** | Achiever **8 / 6512**, Starwave **10 / 1948**, total **18 / 8460** |
| Dummy logins present in that dump | **No** | 0 hits for `10001` / `10002` / `10003` / `99001` |
| Copy to cTrader can place a live order | **No** | no `35=D` builder; `RealCopyEnabled` pinned **false** |

One-liner:

```text
Host Program.cs: DemoSeeder=0 FakeMt5=0 10001=0 10002=0 dummy=0.
API seeds BrokerCatalog only; resync walks ALL stored logins.
Live probe 18/8460. 35=D missing. No capital at risk.
```

Do **not** re-wire `DemoSeeder` into a host. Do **not** add `35=D`. Do **not** set `REAL_COPY_EXECUTION_ENABLED=true`. Fetch is a Manager **read**. Copy stays SHADOW / intent-only.

---

## 1. Assigned search — every product `Program.cs`

Grep (`DemoSeeder|FakeMt5|10001|10002|dummy`) against every `Program.cs` under `D:\Prop`:

| Path | Lines | Hits |
|---|---:|---|
| `D:\Prop\apps\api\Program.cs` | **156** | **0** |
| `D:\Prop\apps\mt5-worker\Program.cs` | **18** | **0** |
| `D:\Prop\apps\fix-worker\Program.cs` | **18** | **0** |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | **85** | **0** |

Hits exist **only** under `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` (scratch eval harnesses, not hosts). Those are report junk, not the API.

`A002` quoted this startup (now gone):

```text
await DemoSeeder.SeedAsync(db, store, scoring, CancellationToken.None);
```

Current API startup is catalog-only:

```149:154:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`BrokerCatalogSeed` writes Achiever + StarwaveFX **broker rows**, XAUUSD instrument, kill-switch default, and two FIX session rows stamped `Disconnected` / “NewOrderSingle off”. It does **not** insert `Mt5Account` `10001`/`10002`, does **not** call `DemoBrokerFactory`, and does **not** invent deals.

mt5-worker and fix-worker hosts match:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` remains on the API / workers because **`BrokerCatalogSeed`** lives in that namespace — not because `DemoSeeder` is invoked.

---

## 2. What the current API `Program.cs` actually does

Read in full: `D:\Prop\apps\api\Program.cs` (156 / 156). TFM is now `net8.0-windows` + `PlatformTarget` x64 (`TraderIntelligence.Api.csproj` L18–19) — A002’s `net8.0` claim is also stale.

### 2.1 Health is runtime, not FakeMt5 paint

```32:56:D:\Prop\apps\api\Program.cs
app.MapGet("/api/health", (LiveRuntimeStatus runtime) =>
{
    var brokers = runtime.Brokers.Values.Select(b => new
    {
        name = b.BrokerCode,
        healthy = b.Connected,
        lastCheck = b.UpdatedAt,
        details = b.Connected
            ? $"live Manager groups={b.Groups} accounts={b.Accounts} phase={b.Phase}"
            : (b.LastError ?? "not connected")
    }).ToArray();
    return Results.Ok(new
    {
        mt5Connections = brokers,
        // ...
        realCopyEnabled = runtime.RealCopyEnabled,
        envFile = loadedEnv is null ? "missing" : "loaded"
    });
});
```

There is **no** literal `"demo FakeMt5BrokerConnector — not live Manager"`.

### 2.2 Settings: copy flags off

```70:83:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = false
    },
    brokerConfigs = new[]
    {
        new { id = "ACHIEVER", name = "Achiever", enabled = true },
        new { id = "STARWAVEFX", name = "StarwaveFX", enabled = true }
    }
}));
```

`FEATURE_COPY_TRADING_ENABLED` is a **literal false**. `REAL_COPY_EXECUTION_ENABLED` is `runtime.RealCopyEnabled`, which DI pins **false** (see §5).

Recon endpoint is explicit: `"NewOrderSingle still off"` (L68).

### 2.3 `/api/ops/resync` is ALL-catalog, not four dummy logins

```111:147:D:\Prop\apps\api\Program.cs
app.MapPost("/api/ops/resync", async (
    DealIngestionService ingestion,
    ReconstructionScoringService scoring,
    ITradingStore store,
    LiveRuntimeStatus runtime,
    CancellationToken ct) =>
{
    var from = DateTimeOffset.UtcNow.AddDays(-90);
    var to = DateTimeOffset.UtcNow.AddMinutes(1);
    var result = new Dictionary<string, object>();
    foreach (var code in new[] { "ACHIEVER", "STARWAVEFX" })
    {
        // SyncCatalogAsync + SyncBrokerAsync + ListLoginsAsync + RebuildTraderAsync
        ...
        result[code] = new { catalog.Groups, catalog.Accounts, deals, scored, logins = logins.Count };
    }
    return Results.Ok(result);
});
```

Measured loop:

1. `ingestion.SyncCatalogAsync(code)` → `GetGroupsAsync` + `GetAccountsAsync(null)` → **every group, every account**.
2. `ingestion.SyncBrokerAsync(code, −90d, now)` → deals (bulk `DealRequestByGroup` on native) + positions (`PositionRequestByGroup("*")`).
3. `store.ListLoginsAsync(brokerId)` → **all** `Mt5Accounts` for that broker, not `{10001,10002,10003,99001}`.
4. `scoring.RebuildTraderAsync(code, login)` for each stored login.

`ListLoginsAsync`:

```339:341:D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs
    public Task<IReadOnlyList<long>> ListLoginsAsync(Guid brokerId, CancellationToken ct) =>
        _db.Mt5Accounts.Where(a => a.BrokerId == brokerId).Select(a => a.Login).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<long>)t.Result, ct);
```

Dashboard `GET /api/groups` / `GET /api/traders` walk **all** `Mt5Groups` / **all** `Mt5Accounts` (`EfDashboardQueries` L70–129). Unscored accounts still render as `INSUFFICIENT_DATA`. That is the “ALL manager traders” surface.

---

## 3. Dummy tokens still exist — but **not** in `Program.cs`

These are **leftovers**. They do not run on API/worker host startup.

### 3.1 `DemoSeeder` (tests + dead host path)

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140 lines).

Product call sites of `DemoSeeder.SeedAsync` (C# only):

| File | Role |
|---|---|
| `DemoSeeder.cs` L16 | definition |
| `tests/Integration/SeedingAndStoreTests.cs` L25 | integration fixture |
| `_tmp_*` report harnesses | not product |

**Zero** host `Program.cs` references. Seeder still:

- early-returns if any `Brokers` row exists (L22–23) — so a live API that already ran `BrokerCatalogSeed` would **skip** even if someone reattached the call;
- builds a **private** `DemoBrokerFactory.CreateDefault()` pair (L126–127), ignoring DI `IBrokerRegistry`;
- scores hardcoded `{10001, 10002, 10003, 99001}` (L134–138).

### 3.2 `FakeMt5BrokerConnector` / `DemoBrokerFactory`

`D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`.

In-process lists only. `ConnectAsync` flips `_connected = true` (L30–34). No socket, no DLL, no password.

Canned book:

| Broker | Groups | Logins | Tape |
|---|---|---|---|
| ACHIEVER | `demo\Maxmaster`, `demo\yo-2step`, `contest\yo-2step` | **10001**, **10002**, 10003 | 3 closed XAU round-trips on 10001; 3 losing/martingale on 10002; 10003 empty |
| STARWAVEFX | `real\standard` | 99001 | 3 closed XAU round-trips |

Product C# `CreateDefault()` / `new FakeMt5BrokerConnector` call sites: this file + `DemoSeeder` L126. **DI never registers it.**

### 3.3 Leftover dummy scorer — `mt5-worker/Worker.cs` (not `Program.cs`)

`apps/mt5-worker/Program.cs` is clean. The **Worker** is not:

```29:35:D:\Prop\apps\mt5-worker\Worker.cs
                await ingestion.SyncBrokerAsync(BrokerCodes.Achiever, from, to, stoppingToken);
                await ingestion.SyncBrokerAsync(BrokerCodes.StarwaveFx, from, to, stoppingToken);
                foreach (var login in new long[] { 10001, 10002, 10003, 99001 })
                {
                    var code = login >= 99000 ? BrokerCodes.StarwaveFx : BrokerCodes.Achiever;
                    await scoring.RebuildTraderAsync(code, login, stoppingToken);
                }
```

If this worker is started **instead of** relying on `LiveIngestHostedService` (which the API host already registers via DI), deal ingest can still walk the live catalog, but **scoring is only the four dummy logins**. Those four are **absent** from the 2026-08-18 live dump (grep of `LIVE_GROUPS_AND_TRADERS.json`: **0** hits). Rebuild would then score empty books.

This is a **host leftover**, not a `Program.cs` dummy seed. It cannot send cTrader orders.

### 3.4 DI refuses Fake when passwords are missing

```35:46:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        services.AddSingleton(runtime);

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`CreateConnectors` returns two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). No `FakeMt5BrokerConnector`.

`LiveIngestHostedService` on catalog failure: `"No dummy data will be substituted."` (L70).

---

## 4. ALL groups + ALL manager traders — measured path

### 4.1 Code path (request APIs, not four logins)

`DealIngestionService.SyncCatalogAsync`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*")` then falls back to `GroupTotal`/`GroupNext` only if the request array is empty (L152–185).

`GetAccountsCore(null)` walks **every** group name and, per group, `UserRequestArray` → (error only) `UserGetByGroup` → (if still empty) `UserLogins` + `UserRequestByLogins` (L189–214, L216–233). That is the ALL-traders net for the manager ACL.

`LiveBrokerProbe/Program.cs` (no dummy tokens) does the same read-only walk and writes `LIVE_GROUPS_AND_TRADERS.json`. Passwords are never serialized (`note = "Passwords never written. Groups and manager logins only."`).

### 4.2 Live census (already measured; this slot did not re-attach)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
UTC: `2026-08-18T08:42:16.8519545+00:00`  
`envLoaded: true`. Probe: `LiveBrokerProbe`.

| Broker | Connect | Groups | Traders | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | OK (HTTP proxy) | 8 | 6512 | 1506 |
| STARWAVEFX | OK (direct) | 10 | 1948 | 478 |
| **Total** | | **18** | **8460** | **1984** |

Achiever groups (sum **6512**):

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |

Starwave groups (sum **1948**):

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |

These are **all groups those manager logins can see**. Groups outside the ACL are invisible; this slot does not claim “every group on the server.”

Dashboard pin (`CREDENTIALS_AND_COPY_STATUS.md`): `/api/traders` **8460**, `/api/groups` **18**. Not the FakeMt5 four.

### 4.3 Scoring completeness gap (honest)

| Surface | Who gets scored |
|---|---|
| `POST /api/ops/resync` | **all** `ListLoginsAsync` accounts |
| `LiveIngestHostedService` | **only** `ListLoginsWithDealsAsync` (logins that have deal rows) |
| `mt5-worker/Worker` | **only** `{10001,10002,10003,99001}` |

Catalog persist is ALL. Dashboard traders list is ALL accounts. Automatic ingest scoring is **deal-bearing logins only**. That is not dummy seed; it is a completeness gap vs “score every manager trader.”

---

## 5. Copy to cTrader — no live order, no loss

Assigned constraint: copy must **not** send live orders yet.

| Gate | Measured |
|---|---|
| `35=D` / `NewOrderSingle` builder in `CTraderFixSession.cs` | **0**. Only outbound `MsgType` is `(35, "A")` Logon (L96). One `ssl.WriteAsync` of that logon; sockets disposed. |
| `35=D` under `D:\Prop\src\Fix.CTrader` | **0** |
| `RealCopyEnabled` | DI L41 **false**; `CTraderFixLogonHostedService` L68 **forces false** after logon |
| API settings | `FEATURE_COPY_TRADING_ENABLED=false`; `REAL_COPY_EXECUTION_ENABLED` bound to that false flag |
| `appsettings.json` `FeatureFlags.LiveCopyEnabled` | **false** (unbound leftover JSON; hosted service does not read it as a send license) |
| `MayRetryNewOrderSingle` | status math only (`ExecutionOrderStateMachine.cs` L35–36). No socket. |
| `PersistDemoShadowAsync` | writes `CopyIntent.Status = "SHADOW_ONLY"` + in-process `ShadowCopyEngine.SimulateEntry`. **No FIX write.** |
| fix-worker | stamps TRADE `Disconnected` / “NewOrderSingle remains off.” Even if `CTrader:RealCopyExecutionEnabled=true`, it **logs a warning and still does not send** (L45–46). |
| YoPips C++ `FakeMt5Client` | **tests only** (`legacy_close_compatibility_test.cpp`). Not a Prop host, not a cTrader sender. |

`SAFE_BY_ABSENCE`: there is no NewOrderSingle to fire. Logon `35=A` on QUOTE 5211 / TRADE 5212 is **not** an order. Session-on is **not** a send license.

This process **cannot** take a market loss on Pepperstone / cTrader from the current `Program.cs` hosts.

---

## 6. Stale reports that must not be cited as current

| Report | Stale claim | Current fact |
|---|---|---|
| `A002_api_dummy_path.md` | API `Program.cs` calls `DemoSeeder`; health = FakeMt5; resync = four logins; TFM `net8.0` | `BrokerCatalogSeed`; live health; `ListLoginsAsync`; TFM `net8.0-windows` x64 |
| `A005_dashboard_traders.md` | health FakeMt5 string; resync four logins | both removed from `Program.cs` |
| `A010_prior_swarm.md` | API + mt5-worker `Program.cs` still seed Demo | both hosts use `BrokerCatalogSeed` |
| `C42_honesty_no_live_mt5.md` | sole connector is FakeMt5 | DI registers `NativeMt5BrokerConnector` only |
| `PHASE0_AUDIT.md` | FUV uses FakeMt5 | live Manager census already measured |

`DemoSeeder` + `FakeMt5BrokerConnector` remain as **test fixtures**. That is allowed. Wiring them back into a host is not.

---

## 7. What this slot does **not** claim

- Did **not** re-run `LiveBrokerProbe` or open a Manager/FIX socket.
- Did **not** print or re-read `.env` password values.
- Did **not** prove every server-side group beyond the two manager ACLs.
- Did **not** tick Architecture §68 / §70. `SAFE_BY_ABSENCE` ≠ go-live PASS.
- Did **not** delete `DemoSeeder` / `FakeMt5BrokerConnector` / the mt5-worker four-login loop (read-only).

---

## 8. Residual work (not this slot)

1. Replace `apps/mt5-worker/Worker.cs` four-login loop with `ListLoginsAsync` / `ListLoginsWithDealsAsync` (or retire the worker now that API hosts `LiveIngestHostedService`).
2. Keep `DemoSeeder` **tests-only**; never reattach to `Program.cs`.
3. Decide whether automatic ingest should score **all** accounts or only those with deals (today: deals-only).
4. Keep `REAL_COPY_EXECUTION_ENABLED=false` until recon + risk + persist-before-send exist. Do not add `35=D`.

---

## 9. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs`
- `D:\Prop\src\Infrastructure\Persistence\EfTradingStore.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Execution\ExecutionOrderStateMachine.cs`
- `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs`
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs`
- `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (header + broker/group census; no secrets)
- `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md`
- `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md`
- `D:\Prop\reports\swarm\20260818\A002_api_dummy_path.md` (stale contrast)

YoPips C++ `FakeMt5Client` / `10001` hits are **SDK retcodes** (`MT_RET_REQUEST_INWAY=10001`, `MT_RET_REQUEST_ACCEPTED=10002`) or **unit-test doubles**. They are not Prop host dummy seed and are not a cTrader send path.

---

## 10. Slot answer

| Question | Answer |
|---|---|
| Does any product `Program.cs` still mention DemoSeeder / FakeMt5 / 10001 / 10002 / dummy? | **No. 0 / 0 / 0 / 0 / 0.** |
| Is the running host still a 4-login FakeMt5 dashboard? | **No.** Catalog seed + native Manager. Prior live measure **18 / 8460**. |
| Can copy-to-cTrader send a live order from these hosts? | **No.** No `35=D`. Flag forced false. **No loss.** |
