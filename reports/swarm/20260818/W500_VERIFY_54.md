# W500_VERIFY_54 — Adversarial live-path verify (slot 54)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **54** |
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
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\api\appsettings.json` | committed flags |
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
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | AllowFixSend formula |
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

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\src` product `*.cs` = **1** (`DemoSeeder.cs` class declaration only)
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

Both workers seed the same catalog writer, not DemoSeeder:

```11:16:D:\Prop\apps\fix-worker\Program.cs
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

`BrokerCatalogSeed` writes broker rows + Disconnected FIX sessions + kill-switch default. It does **not** score demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. **API process does not.**
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

Hosted caller is `CTraderFixLogonHostedService` (`TryLogonAsync` twice: QUOTE 5211, TRADE 5212). No order builder on that type.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. That helper is **not** `CTraderFixSession`. It is demo-gated (refuses `live-*` / `live.` / account `1369850`) and called only from `tools/DemoFixTestTrade` (not API / workers / DI / copy). Copy hop const `NewOrderSingleImplemented = false`. Persist `AllowFixSend = false` (hardcoded on `RiskDecisionRecord`).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Claim 5 is **false** on the live composition.

### 5.1 Lab env is armed

`D:\Prop\.env` L73 (boolean only; no secret dumped):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (display/pipeline flag, also boolean only):

```
FEATURE_COPY_TRADING_ENABLED=true
```

### 5.2 API loads that file

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` candidates include `D:\Prop\.env` (hardcoded last candidate). It `Environment.SetEnvironmentVariable`s every `KEY=value` line.

### 5.3 DI binds the env token — no hard-false pin

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of product `*.cs` for `RealCopyEnabled =`: **one** assignment — this line. Nothing later writes `false`.

### 5.4 Logon host does **not** re-pin

`CTraderFixLogonHostedService` only **reads** `_runtime.RealCopyEnabled` for a log line (L70: `RealCopyArmed={Armed}`). It never assigns the property.

### 5.5 Settings API echoes runtime, not a false literal

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

`FEATURE_COPY_TRADING_ENABLED` is a **literal true**. `REAL_COPY` follows DI. `AddControllers` / `MapControllers` are **absent**; `SettingsController` (`FeatureFlags.LiveCopyEnabled` default false) is **not** on the live route.

`apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. `FeatureFlags.LiveCopyEnabled` is a **different** name (`false`) and is unused by DI. `launchSettings.json` does not set the env token.

### 5.6 What is still false (does **not** rescue claim 5)

| Surface | Value | Bound to env? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | **No** (would need `CTrader__RealCopyExecutionEnabled`) |
| fix-worker `CTrader:RealCopyExecutionEnabled` | `GetValue(..., false)` | **No** (different key; log-only) |
| Architecture / README / docs | document `false` | docs, not runtime |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | send still missing |
| Persist `AllowFixSend` | hardcoded `false` | L211 |

POCO/docs default **false** is not “stays false.” Runtime `LiveRuntimeStatus.RealCopyEnabled` is **true** when the API host loads lab `.env`.

Slots that claimed DI/hosted pin-false (W500_68 / 108 / CREDENTIALS “forced false”) are **stale**.

---

## Copy hop — still SAFE_BY_ABSENCE

Even with the flag armed, this slot proves **no ticket** from the product hop:

- `CTraderFixSession` outbound is `35=A` only (claim 4).
- `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`.
- Persist `AllowFixSend = false` regardless of `RiskEngine.Evaluate`.
- The only `if (decision.AllowFixSend && … NewOrderSingleImplemented && VenueReconciled)` branch sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — it does not write FIX.
- `BuildBlockers` always includes `"No NewOrderSingle sender — SAFE_BY_ABSENCE"`.

Therefore **risk to destination capital = NONE** today. Residual: the next person who adds a `35=D` builder would see `RealCopyEnabled == true` on the API host.

---

## What this slot did **not** prove

- Live Manager attach / group+trader census (not re-probed).
- Live FIX logon success.
- That DemoSeeder is deleted (file remains for tests).
- That `REAL_COPY` is false (it is not, on the API composition).

---

## Verdict

**FAIL.**

Claims 1–4 **PASS** from live files. Claim 5 **FAIL**: `REAL_COPY_EXECUTION` does **not** stay false.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
