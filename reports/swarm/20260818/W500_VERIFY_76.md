# W500_VERIFY_76 — Adversarial live-path verify (slot 76)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **76** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes are **not** evidence. Claim 5 is **disproved**.

---

## Assigned claims (AND)

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound MsgType is `A`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from files this slot. Claim 5 cannot be proved: lab `.env` L73 is `true`, API loads that file, DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and the logon host does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (different key) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\api\appsettings.json` | committed flags (`LiveCopyEnabled=false`, unused) |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no REAL_COPY env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC controller |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | live ingest |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow tick only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` off-hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unused by DI) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines).

Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\src` product `*.cs` = **1** (`DemoSeeder.cs` class declaration L14)
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

Both workers seed the same catalog writer, not DemoSeeder:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

DI fail-closes Fake before connectors exist:

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists. `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

`GetGroupsCore` request-first, then cache fallback:

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
```

Mask `"*"` is the manager-visible complete enumerator. `_pumpEnabled` is **not** a gate on this walk (connect may use pump or `PUMP_MODE_NONE`; fetch still runs).

Live ingest uses that walk with no group-name filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** name returned by `GetGroupsCore()` (L201–202).

**Honesty:** this slot did **not** re-attach Achiever/Starwave. File proves the ALL-groups capability. Completeness of a live census is **unverified here**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Same file, `ReadAccountsForGroup`:

```223:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Order: `UserRequestArray` (network) → cache `UserGetByGroup` only on hard fail → if still empty, `UserLogins` + `UserRequestByLogins`.

`GetAccountsCore(null)` unions every group. Hosted catalog + `/api/ops/resync` both call `GetAccountsAsync(null)`.

**Honesty:** file capability **PASS**. Live “all traders” count was **not** re-measured this slot. Hosted scoring is `ListLoginsWithDealsAsync` (deals-only), which does **not** shrink the catalog walk.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of that file for `35=D`, `NewOrderSingle`, `(35, "D")`: **0**.

Only outbound MsgType is Logon `A`. One `WriteAsync`. `using` disposes `TcpClient` / `SslStream` after one read.

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            (49, sender),
            (56, target),
            (50, senderSub),
            (57, targetSub),
            (52, sendingTime),
            (98, "0"),
            (108, "30"),
            (141, "Y"),
            (553, username),
            (554, password)
        };
        return Assemble(fields);
```

Hosted hop calls only `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) then persists status. No order send.

**Residual off-hop (does not fail claim 4):** sibling `CTraderFixDemoTestTrade` can `Build("D")` at L139/L163/L197. It is **not** `CTraderFixSession`. Gate at L43–47 refuses `live-*` / `live.` / account `1369850`. Not registered in DI / API / workers. `CopyTradingHostedService` ticks shadow intents only (`NewOrderSingle still unimplemented`).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live files prove the opposite on the API host.

**Arm 1 — lab `.env` (boolean only, no secrets):**

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

**Arm 2 — API loads that file before DI:**

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes hard path `D:\Prop\.env` (L14) and `Environment.SetEnvironmentVariable` for every `KEY=value` (L38).

**Arm 3 — DI binds the env string onto runtime:**

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

**Arm 4 — hosted logon does not re-pin false.** `CTraderFixLogonHostedService` only *logs* `_runtime.RealCopyEnabled` (L69–70). Zero assignment to `RealCopyEnabled`.

**Arm 5 — `/api/settings` echoes the runtime boolean**, not a hardcoded false:

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`launchSettings.json` has no `REAL_COPY_*` key. `appsettings.json` `FeatureFlags.LiveCopyEnabled=false` is a **different name** and is unused by DI. `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) but is **not** what DI binds. `apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` default **false** (different key, log-only). MVC `SettingsController` is unused (minimal API owns `GET /api/settings`).

`CREDENTIALS_AND_COPY_STATUS.md` “forced false” and older W500_68/108 pin-false cites are **stale**.

Claim 5 is **disproved**. A later sender on this host would see `RealCopyEnabled=true`.

---

## Capital (not a sixth claim)

Copy hop remains `SAFE_BY_ABSENCE`:

- `CTraderFixSession` outbound is `35=A` only (claim 4).
- `CopyTradingService.NewOrderSingleImplemented = false` (L18).
- Persist hard-sets `AllowFixSend = false` (L306).
- Live-send `if` still requires `NewOrderSingleImplemented && VenueReconciled` (L312); `VenueReconciled = false` (L17).
- `CopyTradingHostedService` generates SHADOW intents only.

Destination capital at risk from this process: **NONE**. Residual: runtime is **armed**; next implemented sender would not be blocked by the env flag.

---

## Verdict

**FAIL.** Claims 1–4 proven from live files this slot (2–3 are source capability; this slot did not re-attach). Claim 5 **FAIL**: `REAL_COPY_EXECUTION` does **not** stay false.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
