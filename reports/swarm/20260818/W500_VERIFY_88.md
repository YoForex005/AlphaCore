# W500_VERIFY_88 — Adversarial live-path verify (slot 88)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **88** |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files). Prior swarm notes are not evidence.

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
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from the files read this slot. Claim 5 cannot be proved: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product copy hop still cannot emit a ticket.

---

## Files read this slot (primary)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` (160 lines) |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual four-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log only |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Dead (no `MapControllers`) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class still exists; not on host boot |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only DI |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 factory |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request APIs (459 lines) |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` into process env |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog `GetGroups` + `GetAccounts(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog walk |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Outbound MsgType (135/135) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | No flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unread by session) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented=false`; persist `AllowFixSend=false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` conjunction |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Off-hop `Build("D")` residual |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | Only product caller of demo helper |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor request-API declarations |
| `D:\Prop\.env` L73 / L106 | Boolean flags only (no secrets quoted) |

Grep this slot: `DemoSeeder` under `D:\Prop\apps` = **0**. Product `35=D` / `(35, "D")` / `NewOrderSingle` in `CTraderFixSession.cs` = **0**. `MapControllers` under `apps/api` = **0**.

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
- Product callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

Both workers seed the same way:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

DI fail-closes Fake/dummy before connectors exist, then registers Native only:

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on that path.

**Residual (does not revive claim 1):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25) plus report-scratch `_tmp_*` programs. Those are not `apps/api`.
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` in its own loop. That is a leftover worker scorer, **not** API startup. Hosted API ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

Stale reports that still say API startup calls `DemoSeeder` are **superseded** by current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines), `GetGroupsCore` L144–186.

Vendor surface (`MT5APIManager.h`):

- `GroupTotal` L205
- `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` L212

Primary (network request, mask `*`):

```152:165:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Fallback if the request list is empty (pump cache walk):

```169:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`_pumpEnabled` does **not** gate this walk. Connect tries pump (`PUMP_MODE_GROUPS|USERS|POSITIONS`) then `PUMP_MODE_NONE` (L89–111); both leave request APIs callable.

Live ingest uses this path: `DealIngestionService.SyncCatalogAsync` → `GetGroupsAsync` + `GetAccountsAsync(null)` (L45–49). `LiveIngestHostedService` calls `SyncCatalogAsync` per connector (L56).

**Residual (does not fail the file claim):** this slot did **not** re-attach Manager. Source capability is proved. Live completeness (every ACL-visible group actually returned) is **not** re-measured here. Prior census numbers are not reused as proof.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Read: `ReadAccountsForGroup` L216–271.

Vendor: `UserLogins` h:254; `UserRequestArray` h:410.

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

`GetAccountsCore(null)` walks every group from `GetGroupsCore()` then unions by login (L189–214). Ingest/resync call `GetAccountsAsync(null)`.

**Residual (does not fail the file claim):**

- `UserGetByGroup` is a pump-cache fallback on **hard fail** of `UserRequestArray`. If that cache returns a **non-empty subset**, `UserLogins` is skipped (`users.Total()==0` gate). Primary path is still the request APIs named in the claim.
- Live ALL-trader completeness was **not** re-attached this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep this file: `35=D` = **0**, `NewOrderSingle` = **0**, `(35, "D")` = **0**.

Only outbound MsgType:

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

`TryLogonAsync` writes **one** frame (`WriteAsync` L49), reads one reply, then `using` disposes `TcpClient` / `SslStream`. Hosted service calls this twice (QUOTE 5211, TRADE 5212) and never keeps a socket (`CTraderFixLogonHostedService` L48–58). Reply parse accepts only inbound `35=A` as logon success (L55–56).

**Residual (does not revive `35=D` on the assigned class):** sibling `CTraderFixDemoTestTrade` can `Build("D")` at L139 / L163 / L197 (`Build` L243–255 sets `(35, msgType)`). That helper is:

- **not** referenced by API / DI / workers (grep `CTraderFixDemoTestTrade` in product `*.cs`/`*.csproj` hits only the helper itself)
- called only from `D:\Prop\tools\DemoFixTestTrade\Program.cs` L44
- demo-gated: refuses non-`demo-` host, non-`demo.` sender, `live-` / `live.`, and account `1369850` (L43–60)

Assigned claim is `CTraderFixSession` has no `35=D`. That is true.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live composition proves the opposite.

### 5.1 Lab env is `true`

`D:\Prop\.env` L73:

```
REAL_COPY_EXECUTION_ENABLED=true
```

(L106 `FEATURE_COPY_TRADING_ENABLED=true` is a separate display/pipeline flag. No other `.env` values quoted.)

### 5.2 API loads that file into process env

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` includes the literal candidate `D:\Prop\.env` (L14) and `SetEnvironmentVariable` for every `KEY=value` line (L38).

Workers (`apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) do **not** call `EnvFile.FindAndLoad`. That does not rescue the API host: the assigned API startup path **does** load the file.

### 5.3 DI binds the env token onto runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** hard-false pin. `CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L69–70) and does **not** assign it false.

The only `RealCopyEnabled =` assignment in `D:\Prop\src` is DI L41.

### 5.4 `/api/settings` echoes the bound runtime

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

`apps/api` has **no** `MapControllers` / `AddControllers`. `SettingsController` (`FeatureFlags.LiveCopyEnabled` default false) is **unmapped**. The live settings surface is the minimal-API map above.

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults false** (POCO L35). That POCO is **not** the runtime choke and is **unread** by `CTraderFixSession`. `apps/api/appsettings.json` has `FeatureFlags.LiveCopyEnabled=false` and **no** `REAL_COPY_EXECUTION_ENABLED` key. The env token wins via `AddEnvironmentVariables`.

`apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default `false`) and only logs. It does not re-pin `LiveRuntimeStatus`.

### 5.5 Stale “forced false” reports

README / architecture text that still print `REAL_COPY_EXECUTION_ENABLED=false`, and older research that cite DI/hosted hard-false pins, are **stale** against current `DependencyInjection.cs` L41 and the logon host.

**Claim 5 FAIL.** The flag does **not** stay false on the API host.

---

## Copy hop (capital) — SAFE_BY_ABSENCE

Claim 5 FAIL is a **flag** failure, not a live send.

| Gate | File | State |
|---|---|---|
| Product session outbound | `CTraderFixSession` L96 | `(35, "A")` only |
| Copy const | `CopyTradingService` L18 | `NewOrderSingleImplemented = false` |
| Venue recon | L17 | `VenueReconciled = false` |
| Persist | L306 | `AllowFixSend = false` (forced, ignores `decision.AllowFixSend`) |
| Send branch | L312 | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — unreachable |
| Risk conjunction | `RiskEngine` L147–150 | `AllowFixSend` needs `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`; persist still overwrites false |
| Blocker text | `CopyTradingService` L468–469 | `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` |

`LiveRuntimeStatus.Snapshot` (L42–44) states: if armed, “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”

This slot did not live-attach and did not send `35=D`.

---

## Verdict

**FAIL.**

1. DemoSeeder **not** API startup — **PASS**
2. Native `GroupRequestArray("*")` then `GroupTotal` — **PASS** (source; not re-attached)
3. `UserRequestArray` then `UserLogins` — **PASS** (source; not re-attached)
4. `CTraderFixSession` **no** `35=D` — **PASS**
5. `REAL_COPY_EXECUTION` stays false — **FAIL** (`.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41; logon does not re-pin)

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
