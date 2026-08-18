# W500_VERIFY_60 — Adversarial live-path verify (slot 60)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **60** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved**.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160/160) seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
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
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (62/62)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94/94)

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

Both workers use the same seed, not DemoSeeder:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\src` product (non-test) = **1** class declaration only (`Seeding\DemoSeeder.cs` L14)
- `FakeMt5` / `DemoSeeder` under `D:\Prop\apps` = **0**
- Tests: `tests\Integration\SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. **API process does not.**

DI fail-closes Fake and registers Native only:

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed.EnsureAsync` writes broker catalog + XAU instrument + kill-switch + two FIX rows already `Disconnected`. It does **not** score demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists for tests.
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

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

Hosted ingest uses that walk, flag-blind:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` / `SyncBrokerAsync` for every registered Native connector. Mask `"*"` is ALL groups. Empty `GroupRequestArray` falls back to pump-cache `GroupTotal`/`GroupNext`.

This slot **did not re-attach** Manager. Census 18/8460 cited by other waves is **not re-measured here**. Verdict is file-capability only: **PASS_SOURCE**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file. `GetAccountsAsync(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

Primary path is `UserRequestArray`. Cache `UserGetByGroup` only on hard fail. Empty array → `UserLogins` + `UserRequestByLogins`. Dedup is by login dictionary.

`_pumpEnabled` is **not** a fetch gate. Request APIs run after either pump or `PUMP_MODE_NONE` connect.

This slot **did not re-attach**. Completeness of “all manager-visible traders” is source-wired, not re-counted. **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep this file for `35=D` / `NewOrderSingle` / `(35, "D")` = **0**.

Only outbound MsgType:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
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
    }
```

`TryLogonAsync` (L21–87): one `TcpClient` + `SslStream`, one `WriteAsync` of that Logon, one `ReadAsync`, then `using` disposes both sockets. No heartbeat loop. No NewOrderSingle. No tag 38 / tag 11 builder.

Hosted caller (`CTraderFixLogonHostedService.cs` L48–58) invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” It never writes a D.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 exists. Grep of `apps/` for `CTraderFixDemoTestTrade` = **0**. Only `tools/DemoFixTestTrade` calls it. Demo-gated: refuses `live-*` host, `live.` sender, and account `1369850` (L43–60). **Not on the copy hop. Not in DI.**

Copy hop still cannot send:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- persist `AllowFixSend = false` (L211), even when `decision.AllowFixSend` is later AND-gated with LIVE + NOS + reconciled (L217)
- `CTraderFixOptions.RealCopyExecutionEnabled` POCO default remains `false` and is **unread** by `CTraderFixSession`

Claim 4 is **PASS** for the assigned type.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live files show the opposite on the API host.

### 5.1 Lab env is armed

`D:\Prop\.env` L73 (boolean only; neighboring secrets not copied):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 is also `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; not the send license).

### 5.2 API loads that file into process env

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`src\Mt5\Env\EnvFile.cs` L5–20) walks cwd / parents and hardcoded `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`.

Workers (`mt5-worker` / `fix-worker` `Program.cs`) do **not** call `EnvFile.FindAndLoad`. They still bind whatever is already in process/machine env via `AddTraderIntelligence`.

### 5.3 DI binds the bit; logon does not re-pin

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** comment pinning false. Comparison is ordinal-ignore-case against `"true"`.

`CTraderFixLogonHostedService.ExecuteAsync` writes Quote/Trade logon state (L60–70) and **never assigns** `_runtime.RealCopyEnabled = false`. Slots that claimed a hosted hard-false pin (W500_68 / 108 and similar) are **stale**.

### 5.4 Surfaces that expose the armed bit

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

`/api/health` L55 also echoes `realCopyEnabled = runtime.RealCopyEnabled`.

`CopyTradingService.GetStatusAsync` sets `RealCopyArmed: _runtime.RealCopyEnabled` (L44) and only adds blocker `"REAL_COPY_EXECUTION_ENABLED is false"` when the bit is already false (L316–317). With `.env=true` that blocker is **absent**.

`RiskEngine.Evaluate` is called with `RealExecutionEnabled = _runtime.RealCopyEnabled` (CopyTradingService L190). Persist still forces `AllowFixSend = false`.

`CTraderFixOptions.RealCopyExecutionEnabled = false` (POCO L35) is **not** the live runtime bit. Grep of product `src` for `Configure<CTraderFixOptions>` = **0**. Fix-worker `Worker.cs` L21 reads nested `CTrader:RealCopyExecutionEnabled` (default false) — a **different key** than the env token. That log-only path does not rescue claim 5 on the API host.

`CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is **stale**.

**Claim 5 FAIL.** The operator flag does not stay false.

---

## Copy-hop residual (does not turn claim 5 into PASS)

| Gate | Live file | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const false` |
| `VenueReconciled` | L16 | `const false` |
| Persist `AllowFixSend` | L211 | literal `false` |
| `CTraderFixSession` outbound | 135/135 | `(35, "A")` only |
| Hosted logon | `CTraderFixLogonHostedService` | Logon probe only |
| Demo `Build("D")` | `CTraderFixDemoTestTrade` | tools-only + demo-gated |

So destination capital is still **not** at risk from the copy pipeline. That is `SAFE_BY_ABSENCE`, not “flag stays false.”

---

## What this slot did **not** do

- No Manager re-attach (claims 2–3 are source-capability).
- No live HTTP to `:5000` (settings echo inferred from source, not measured 200).
- No product edit. No secret printed. No `35=D` attempted.

---

## Files read this slot

| Path | Lines read |
|---|---|
| `D:\Prop\apps\api\Program.cs` | 160/160 |
| `D:\Prop\apps\mt5-worker\Program.cs` | 18/18 |
| `D:\Prop\apps\fix-worker\Program.cs` | 18/18 |
| `D:\Prop\apps\mt5-worker\Worker.cs` | 45/50 |
| `D:\Prop\apps\fix-worker\Worker.cs` | 51/51 |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | 459/459 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | gate + `Build("D")` sites |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112/112 |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 80/80 |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 62/62 |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | 94/94 |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | 112/112 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | 141/141 |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | 320/320 |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | 146/146 |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66/66 |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | 41/41 |
| `D:\Prop\.env` | L73 boolean only |

---

## Verdict

**FAIL.** Claims 1 and 4 proved from live files. Claims 2 and 3 proved as **source capability** only (no re-attach). Claim 5 **disproved**: `REAL_COPY_EXECUTION_ENABLED` is `true` in lab `.env` and DI binds it; hosted logon does not re-pin.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` — no copy-hop `35=D`, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`).
