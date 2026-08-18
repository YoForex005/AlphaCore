# W500_VERIFY_61 — Adversarial live-path verify (slot 61)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **61** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes are **not** evidence. Claim 5 is **disproved** by the files.

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
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (source capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (source capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound `(35, "A")`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from the files this slot re-read. Claim 5 cannot be proved: lab `.env` L73 is `true`, the API loads that file, DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and the logon host does **not** re-pin false.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product copy hop still cannot emit a ticket.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot:

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\fix-worker\Program.cs` (18 lines)
- `D:\Prop\apps\mt5-worker\Program.cs` (18 lines)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (class still exists)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`

API startup seed is catalog-only:

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
- `DemoSeeder` under `D:\Prop\src` = **1** (`Seeding\DemoSeeder.cs` class definition only)
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

Workers seed the same catalog writer, not DemoSeeder:

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

DI fail-closes Fake **before** connectors exist and registers Native only:

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `FakeMt5` under `D:\Prop\src\Infrastructure` = **0**.

**Residual (does not revive claim 1):**

- `DemoSeeder.cs` remains on disk for tests (`tests/Integration/SeedingAndStoreTests.cs` still calls `DemoSeeder.SeedAsync`). **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores leftover logins `{10001, 10002, 10003, 99001}`. That is a worker scorer, **not** API startup.

Reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

`GetGroupsCore` is request-first, then cache fallback:

```152:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

- Primary: `GroupRequestArray("*")` — manager-visible mask.
- Fallback: `GroupTotal` + `GroupNext` when the request list is empty.

Live ingest uses that walk: `DealIngestionService.SyncCatalogAsync` calls `connector.GetGroupsAsync` (`DealIngestionService.cs` L45).

**Limit of this proof:** file capability only. This slot did **not** re-attach Achiever/Starwave, so ALL-group **counts** are not re-measured here. A001 (“zero `GroupRequestArray` under `src`”) is **stale**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Same connector. `GetAccountsCore(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

```216:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        var rows = new List<Mt5AccountDto>();
        var users = _manager!.UserCreateArray();
        var accounts = _manager.UserCreateAccountArray();
        try
        {
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

- Primary: `UserRequestArray(gname, users)`
- Empty: `UserLogins` then `UserRequestByLogins`
- Cache `UserGetByGroup` only on hard request fail

Live catalog ingest is unfiltered: `GetAccountsAsync(null, ct)` (`DealIngestionService.cs` L48 and L62). Hosted ingest (`LiveIngestHostedService`) calls `SyncCatalogAsync` for every registered Native connector.

**Limit of this proof:** source walk only. Completeness of the live census was **not** re-attached this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of this file for `35=D` / `"D"` / `NewOrderSingle`: **0**.

Only outbound MsgType is Logon:

```94:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

`TryLogonAsync` does one `WriteAsync` of that Logon, one `ReadAsync`, then `using` disposes `TcpClient` / `SslStream`. No heartbeat loop. No order builder.

Hosted caller `CTraderFixLogonHostedService` invokes `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs `NewOrderSingle still unimplemented`. It never builds MsgType D.

**Residual (does not revive claim 4):** sibling `CTraderFixDemoTestTrade.cs` can `Build("D")` (L139 / L163 / L197). That type is **not** `CTraderFixSession`, is not registered in API/worker DI, and is the `tools/DemoFixTestTrade` CLI. The assigned claim is the session class. `CTraderQuoteService` can assemble `35=V` / `35=y` but has no callers from the logon host.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live composition proves the opposite.

### 5.1 Lab env is armed

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`  
`D:\Prop\.env` L106: `FEATURE_COPY_TRADING_ENABLED=true`

Boolean names/values only. Neighboring secrets were **not** copied into this report.

### 5.2 API loads that file into process env

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` walks cwd / parents and the hardcoded candidate `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value` (`EnvFile.cs` L8–38).

### 5.3 DI binds the architecture token (no hard-false pin)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is no `= false` pin and no comment forcing the bit off.

### 5.4 Hosted logon does not re-pin false

`CTraderFixLogonHostedService.ExecuteAsync` writes Quote/Trade logon state and **logs** `_runtime.RealCopyEnabled`. It never assigns `RealCopyEnabled = false`.

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

### 5.5 Settings API echoes the runtime bit

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

Therefore a process that starts from this `.env` **advertises** `REAL_COPY_EXECUTION_ENABLED=true`. Claim 5 is false.

### 5.6 What still stays false (does not rescue claim 5)

These are **different** surfaces. They keep send off. They do **not** make the assigned flag stay false.

| Surface | Measured this slot |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` POCO default | `false` (`CTraderFixOptions.cs` L35). **Not** bound from env `REAL_COPY_EXECUTION_ENABLED`. |
| `apps/fix-worker/Worker.cs` | Reads nested `CTrader:RealCopyExecutionEnabled`, default **false**. Log-only. Stamps sessions `Disconnected`. |
| `CopyTradingService.NewOrderSingleImplemented` | `const bool` **false** (L17) |
| `CopyTradingService.VenueReconciled` | `const bool` **false** (L16) |
| Persist | `AllowFixSend = false` always (`CopyTradingService.cs` L211) |
| Architecture / README / `docs/architecture.md` | still write `=false` (policy text, not the running bind) |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” | **stale** vs current DI |

Workers do **not** call `EnvFile.FindAndLoad`. If launched without inheriting process env they would see a missing key → `RealCopyEnabled=false`. The **API** live path does load the file. Claim 5 is about the flag staying false, not about a worker that never loaded dotenv.

W500_68 / W500_108 / A014 “DI pins false” / E038 “settings hardcodes false” are **stale**.

---

## Copy hop / capital (not a claim-5 rescue)

Even with the flag armed, this slot found **no** product path that can send a live NewOrderSingle:

- `CTraderFixSession` outbound = `35=A` only
- `CopyTradingService` const `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`
- Live send branch still requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (`CopyTradingService.cs` L217) — three of four are hard-false
- `BuildBlockers` always includes `No NewOrderSingle sender — SAFE_BY_ABSENCE`

So dest risk is **NONE** today by **absence of a sender**, not because `REAL_COPY_EXECUTION` stayed false.

---

## Files read this slot (primary)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | Startup seed + env load + settings echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested flag, no send |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Leftover 4-login scorer residual |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two Native connectors |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual startup seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Exists; not API-called |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | dotenv loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135, no `35=D` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | No re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Mutable `RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const false; persist `AllowFixSend=false` |
| `D:\Prop\.env` L73 / L106 | Boolean flags only |

---

## Verdict

**FAIL.**

Claims 1–4 are file-proven. Claim 5 is **disproved**: `REAL_COPY_EXECUTION` does **not** stay false on the API live path.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). This slot did not live-attach and did not send `35=D`.
