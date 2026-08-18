# W500_VERIFY_82 — Adversarial live-path verify (slot 82)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **82** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes, INDEX blurbs, and `CREDENTIALS_AND_COPY_STATUS.md` are **not** evidence. Claim 5 is **disproved** from the files this slot.

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

**AND of five = FAIL.** Claims 1–4 hold from files this slot. Claim 5 cannot be proved: lab `.env` L73 is `true`, API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` injects it, DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and `CTraderFixLogonHostedService` does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | no `AddControllers` path required |
| `D:\Prop\apps\api\appsettings.json` | committed flags; no `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no `REAL_COPY` env override |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC controller (no `MapControllers`) |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (different key) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists; not called from hosts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | live ingest |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow tick only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented; persist `AllowFixSend=false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | AllowFixSend formula |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | CLI-only sibling, not DI |
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
- API `Program.cs` has no `AddControllers` / `MapControllers`; `SettingsController` cannot be the boot path

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed` writes broker rows + Disconnected FIX sessions + kill-switch default. It does **not** score demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14) and still seeds dummy logins `{10001,10002,10003,99001}` (L134). `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. **API / worker Program.cs processes do not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

`GetGroupsCore` is request-first (`GroupRequestArray("*")`), then `GroupTotal` + `GroupNext` only if the request walk produced **zero** groups:

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

Mask `"*"` is the manager-visible complete enumerator. `_pumpEnabled` is **not** a branch on this walk (connect may use pump or `PUMP_MODE_NONE`; fetch still runs).

Live ingest uses that walk with no group-name filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** name returned by `GetGroupsCore()` (connector L201–202).

**Adversarial honesty (does not fail the “can” claim):**

- This slot did **not** re-attach Achiever/Starwave. File proves the ALL-groups **capability**. Completeness of a live census is **unverified here**.
- `GroupTotal` runs only when `list.Count == 0`. If `GroupRequestArray("*")` returned a **partial non-empty** set, the fallback would be skipped. That is a runtime completeness risk, not absence of the two APIs.

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

**Adversarial honesty:** file capability **PASS**. Live “all traders” count was **not** re-measured this slot. `UserLogins` is skipped if `users.Total() != 0` after `UserRequestArray` (partial-array risk, same as groups). Hosted scoring is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), which does **not** shrink the catalog walk.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**). File ends at L135 (`}`).

Grep of that file for `35`:

- L55 inbound extract of tag `35` from the logon **reply**
- L73 error string `Logon rejected 35={msgType}`
- L96 outbound field `(35, "A")` only

Grep of that file for `35=D`, `NewOrderSingle`, `(35, "D")`, `Build("D")`: **0**.

Only outbound MsgType is Logon `A`. One `ssl.WriteAsync` (L49). `using` disposes `TcpClient` / `SslStream` after one read. Hosted service calls `TryLogonAsync` twice (QUOTE then TRADE) and returns; no order loop.

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

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. That is a **different type**. It is demo-gated (`host` must start with `demo-`; refuses `live-` / `live.` / account `1369850`; L43–60) and is invoked only from `tools/DemoFixTestTrade/Program.cs` L44. Zero callers from `AddTraderIntelligence` / API / copy hop / `CTraderFixLogonHostedService`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove the opposite.

### 5a. Lab `.env` is `true`

`D:\Prop\.env` L73 (boolean only; no secret quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (boolean only):

```
FEATURE_COPY_TRADING_ENABLED=true
```

### 5b. API loads that file into process env, then configuration

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–19) hard-includes `D:\Prop\.env` as the last candidate and `Environment.SetEnvironmentVariable` for every `KEY=VALUE` line (L38).

`apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. `FeatureFlags:LiveCopyEnabled` is committed `false` (L47) but is **unread** by DI. `launchSettings.json` sets only `ASPNETCORE_ENVIRONMENT`. Therefore the process env boolean wins.

### 5c. DI binds the env string onto the live runtime singleton

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

With `.env` L73 `true`, `runtime.RealCopyEnabled` is **true** at host construction.

### 5d. Logon host does **not** re-pin false

Read entire `CTraderFixLogonHostedService.cs` (112 lines). It copies logon results onto `_runtime.Quote` / `_runtime.Trade` (L60–67) and **logs** `_runtime.RealCopyEnabled` (L69–70). There is no assignment `_runtime.RealCopyEnabled = false`.

API `/api/settings` **exposes** the bound value, it does not force false:

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

### 5e. Stale “forced false” documents

These are **not** the live path:

| Source | What it says | Why stale |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 default `false` | POCO default OFF | **Never bound** by `AddTraderIntelligence` |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled=false` | committed false | Unread by DI |
| `apps/fix-worker/Worker.cs` L21 `_config.GetValue("CTrader:RealCopyExecutionEnabled", false)` | worker-local default false | **Different key**; not the API runtime flag |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” | docs | Contradicted by DI L41 + `.env` L73 |
| `README.md` “Real NewOrderSingle is off (`REAL_COPY_EXECUTION_ENABLED=false`)” | docs | Contradicted by lab `.env` |

Claim 5 is therefore **disproved**, not merely unproven.

---

## Capital / send hop (does not rescue claim 5)

Claim 5 fails. Destination capital is still **not** at risk from this process because the send hop is absent:

| Gate | File proof |
|---|---|
| `NewOrderSingleImplemented = false` const | `CopyTradingService.cs` L18 |
| `VenueReconciled = false` const | same L17 |
| Persist `AllowFixSend = false` **hardcoded** (ignores `decision.AllowFixSend`) | L306 |
| Send branch requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` and then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — still no socket write | L312–315 |
| Hosted copy tick is `TickRosterAsync` + `GenerateShadowIntentsAsync` only | `CopyTradingHostedService.cs` L28–32 |
| `CTraderFixSession` outbound is `35=A` only | claim 4 |
| RiskEngine `AllowFixSend` can become true if `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–150), but persist overwrites to false and NOS is missing | SAFE_BY_ABSENCE |

`LiveRuntimeStatus.Snapshot()` even documents the armed-but-unimplemented state (L42–44): *“REAL_COPY armed. NewOrderSingle still unimplemented…”*.

If a sender were added tomorrow against this env bind, the next process would see `RealCopyEnabled=true`. That is why claim 5 is FAIL even though today’s dest risk is NONE.

---

## Verdict

**FAIL.** Claims 1–4 file-proven (DemoSeeder off API boot; Native `GroupRequestArray("*")` / `GroupTotal`; `UserRequestArray` / `UserLogins`; `CTraderFixSession` 135/135 is `35=A` only). Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin.

Risk to destination capital: **NONE** (`SAFE_BY_ABSENCE`).
