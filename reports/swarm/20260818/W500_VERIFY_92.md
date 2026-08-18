# W500_VERIFY_92 — Adversarial live-path verify (slot 92)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **92** |
| Role | Adversarial verifier. Read live path files this slot. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL any assigned claim that cannot be proved from the live file. Claim 5 is **disproved**. Claims 2–3 are source-capability only (this slot did not re-attach Manager).

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160/160) seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if the list is empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync`; sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime bit. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

Risk to destination capital: **NONE** (`SAFE_BY_ABSENCE`). Arming the flag does not create a sender.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot (full files):

- `D:\Prop\apps\api\Program.cs` (160/160)
- `D:\Prop\apps\mt5-worker\Program.cs` (18/18)
- `D:\Prop\apps\fix-worker\Program.cs` (18/18)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (112/112)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140/140)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (62/62)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94/94)

API startup seed is catalog-only. After `app.Build()` the only seed call is `BrokerCatalogSeed.EnsureAsync`. There is no `DemoSeeder.SeedAsync`:

```152:158:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

`using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed` (same namespace). That import is **not** a DemoSeeder invocation.

Both workers use the same catalog seed, not DemoSeeder:

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

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- Product class declaration only: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14
- Product call site of `DemoSeeder.SeedAsync` outside tests / swarm `_tmp_*` = **0**
- Tests still call it: `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25. **API process does not.**
- `DemoSeeder` uses `DemoBrokerFactory.CreateDefault()` (`FakeMt5BrokerConnector`). That factory is not registered on the API hop.

DI fail-closes dummy brokers and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        services.AddSingleton<TraderIntelligence.Domain.Risk.RiskEngine>();

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` returns **two** `new NativeMt5BrokerConnector(...)` (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on that hop (`CreateConnectors` 20–49).

`BrokerCatalogSeed.EnsureAsync` upserts broker catalog + XAU instrument + kill-switch + two FIX rows already `Disconnected`. It does **not** invent demo logins 10001/10002/10003/99001 or run `DealIngestionService` against Fake.

**Residual (does not revive claim 1):**

- `DemoSeeder.cs` still exists for tests.
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.
- Swarm `_tmp_*` copies under `reports/swarm/20260818/` still invoke DemoSeeder. Those are not `apps/api`.

Prior reports that still say API startup calls `DemoSeeder` are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

Live registration wires this class as the only `IMt5BrokerConnector` implementation (`LiveMt5Registration.cs` L23–49). Catalog hop:

```45:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetGroupsAsync` → `GetGroupsCore`:

```144:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        lock (_gate)
        {
            Ensure();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Mt5GroupDto>();

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
                var grp = _manager.GroupCreate();
                try
                {
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
                        AddGroup(list, seen, grp);
                    }
                }
                finally { grp.Release(); }
            }

            return list;
        }
    }
```

**What the file proves:** the Native connector **can** enumerate groups through `GroupRequestArray("*")` or, if that yields an empty list, `GroupTotal` + `GroupNext`. Mask is `"*"`, not a hard-coded subset.

**What the file does not prove (adversarial residuals):**

- This slot did **not** attach a live Manager, so “all groups on Achiever/Starwave” is **not** a measured census.
- Fallback to `GroupTotal` runs only when `list.Count == 0`. A **partial** `GroupRequestArray` result skips `GroupTotal`.
- Completeness still depends on Manager rights / pump mode (`PUMP_MODE_GROUPS` attempted first; `PUMP_MODE_NONE` is the connect fallback at L101).

Claim 2 is therefore **PASS_SOURCE** (API surface + call order), not a live “all groups” proof.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file. `GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()`, then `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            // ...
        }
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        // ...
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
```

Catalog + deal ingest both pass `group: null` (`DealIngestionService.cs` L48, L62), so the intended hop is **all groups → all users**.

**What the file proves:** Native **can** list traders through `UserRequestArray` with `UserLogins`/`UserRequestByLogins` as the empty-array fallback, plus `UserGetByGroup` on non-OK/OK_NONE/NOTFOUND.

**What the file does not prove:**

- No live attach this slot → no measured trader census.
- `UserLogins` runs only when `users.Total() == 0`. A **partial** `UserRequestArray` skips the login fallback.
- Trader completeness is bounded by group completeness (claim 2 residual).
- Scoring in `LiveIngestHostedService` then restricts to `ListLoginsWithDealsAsync` (L106). That is a later filter, not the catalog enumerator.

Claim 3 is **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read full file this slot: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Independent scan of that file for tag 35:

| Line | Token | Role |
|---|---|---|
| 55 | `Extract(reply, "35")` | inbound parse |
| 73 | interpolated `35={msgType}` in reject text | inbound log |
| 96 | `(35, "A")` | **only outbound MsgType** |

Zero `35=D`. Zero `NewOrderSingle`. Zero `Build("D"`. `BuildLogon` is Logon-only (`35=A` plus 34/49/56/50/57/52/98/108/141/553/554). `Assemble` emits one message. There is a single `ssl.WriteAsync` (L49). TCP/SSL disposed via `using`.

Hosted hop that **is** on the API process (`CTraderFixLogonHostedService.cs` L48–58) calls **only** `CTraderFixSession.TryLogonAsync` twice (QUOTE + TRADE). It never calls `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`. It does **not** write a NewOrderSingle.

**Out-of-claim (must not be used to weaken claim 4):** other files in the same project **do** contain `Build("D"`:

- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` L139, L163, L197
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` L93

Those classes are **not** `CTraderFixSession` and are **not** invoked by `CTraderFixLogonHostedService`. Claim 4 as assigned is **PASS**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

This claim is **false** on the live path. Files this slot:

### 5a. Lab env is armed

`D:\Prop\.env` **L73** (boolean only; no secrets quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

(`FEATURE_COPY_TRADING_ENABLED=true` at `.env` L106 is a different flag. API `/api/settings` hard-codes that display bit to `true` at `Program.cs` L77 and does not read the env key.)

### 5b. API loads that file into process env

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` 5–20) walks cwd / parents and **hard-includes** `D:\Prop\.env`. `Load` (`L23–39`) `SetEnvironmentVariable` for every `KEY=value` line. Combined with `AddEnvironmentVariables()`, L73 is visible to DI as `configuration["REAL_COPY_EXECUTION_ENABLED"]`.

### 5c. DI binds the bit; nothing re-pins it false

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`LiveRuntimeStatus.RealCopyEnabled` is a public settable `bool` (`LiveRuntimeStatus.cs` L32). The **only** product assignment of `RealCopyEnabled =` under `D:\Prop\src` is this DI line.

`CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for a log line (L69–70) and **never writes** it. There is no `RealCopyEnabled = false` after logon.

API surfaces the bound bit:

```55:55:D:\Prop\apps\api\Program.cs
        realCopyEnabled = runtime.RealCopyEnabled,
```

```74:78:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`apps/api/appsettings.json` does **not** define `REAL_COPY_EXECUTION_ENABLED` (so it cannot override `.env` back to false). `FeatureFlags.LiveCopyEnabled` in that file is a **different unmapped name**.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). That POCO is **not** what DI writes onto `LiveRuntimeStatus`.

`apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` default `false` — a **different configuration key**, log-only. It does not pin `LiveRuntimeStatus`.

**Therefore “stays false” is disproved:** if the API process starts with `D:\Prop\.env` loaded, `LiveRuntimeStatus.RealCopyEnabled` becomes **true**.

### 5d. Why capital is still not at risk (does not rescue claim 5)

Copy hop remains `SAFE_BY_ABSENCE` even when the flag is armed:

| Gate | File | Value |
|---|---|---|
| Outbound NewOrderSingle on session class | `CTraderFixSession.cs` | **absent** (only `35=A`) |
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L18 | `const false` |
| `VenueReconciled` | `CopyTradingService.cs` L17 | `const false` |
| Persisted `AllowFixSend` | `CopyTradingService.cs` L306 | **hard `false`** (ignores `decision.AllowFixSend`) |
| Live-send branch | `CopyTradingService.cs` L312 | requires `NewOrderSingleImplemented && VenueReconciled` — both const false. Branch only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; it does not write FIX. |
| Blocker list | `CopyTradingService.cs` L468–479 | always includes “No NewOrderSingle sender — SAFE_BY_ABSENCE” and “Venue not reconciled” |

`RiskEngine` **can** set `AllowFixSend = true` when `RealExecutionEnabled && Reconciled && VenueHealthy` (L147–170). The copy service **does not persist** that bit (`AllowFixSend = false` at L306). Combined with no `35=D` sender, destination capital is not reachable from this process.

Claim 5 is still **FAIL**. “Must stay false” is policy. Live files do not keep it false.

---

## What this slot did not do

- No live Manager connect / group / login census.
- No process dump of a running API to observe `realCopyEnabled` at HTTP time.
- No product source edits.
- No secrets printed (`.env` password lines were not copied into this report).

---

## Verdict

**FAIL.** Claims 1 and 4 proved from files. Claims 2 and 3 proved as Native **source capability** only (`GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`). Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` is loaded by `EnvFile.FindAndLoad` and bound by `DependencyInjection.cs` L41; hosted FIX logon does not re-pin false. Destination risk **NONE** (`SAFE_BY_ABSENCE`).
