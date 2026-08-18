# W500_VERIFY_39 — Adversarial live-path verify (slot 39)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **39** |
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
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API loads that file. DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host does not re-pin. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot: `D:\Prop\apps\api\Program.cs` (160 lines).

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
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- Remaining `DemoSeeder` hits are `src/Infrastructure/Seeding/DemoSeeder.cs` (class still on disk), `tests/Integration/SeedingAndStoreTests.cs`, and throwaway `_tmp_*` trees. **None of those are API boot.**

DI fail-closes Fake:

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on the host path.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup. The worker `Program.cs` seed is also `BrokerCatalogSeed.EnsureAsync`.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read this slot: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

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

File-proved:

- Primary walk is `GroupRequestArray("*")` (mask = all groups the manager may see).
- Empty-result fallback is `GroupTotal` + `GroupNext`.
- Dedup is name-based (`HashSet` ordinal-ignore-case). No plan-env filter (`MT5_GROUP_*` unread in this class).

Live ingest uses that walk:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

This slot **did not live-attach**. Completeness of the live Achiever+Starwave census is **not re-proved** here. Source capability is proved. Prior 08:42Z pin (8/6512 + 10/1948 = 18/8460) is **cited, not re-measured**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `GetAccountsCore` + `ReadAccountsForGroup`:

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

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }

            return byLogin.Values.ToList();
        }
    }

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

File-proved:

- `GetAccountsAsync(null)` (ingest + `/api/ops/resync`) walks **every** group from `GetGroupsCore`, then every user in that group.
- Primary user request is `UserRequestArray`.
- Cache `UserGetByGroup` only on a **hard** request fail (not `OK` / `OK_NONE` / `NOTFOUND`).
- Empty array falls through to `UserLogins` + `UserRequestByLogins`.

This slot **did not live-attach**. “ALL manager traders on the wire right now” is therefore **not re-proved**. Source path is proved.

---

## 4. CTraderFixSession has no 35=D — PASS

Read this slot: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Independent grep this slot on that file for `35=D` / `"D"` / `NewOrderSingle` = **0**.

Only outbound MsgType in the compilation unit:

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

Wire path: one `TcpClient` + `SslStream`, one `WriteAsync` of that Logon, one `ReadAsync`, then `using` disposes both sockets. No heartbeat loop. No order builder.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists status. It never asks for `35=D`.

**Residual (does not revive claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. That is **not** `CTraderFixSession`. It is demo-gated (refuses `live-*` / `live.` / account `1369850`) and is invoked from `tools/DemoFixTestTrade`, not from API / DI / copy.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove the opposite on the API path.

1. Lab `.env` L73 (boolean only; no secret dumped):

```
REAL_COPY_EXECUTION_ENABLED=true
```

2. API boot loads that file into the process environment:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` includes the hard path `D:\Prop\.env` (`EnvFile.cs` L14) and `Environment.SetEnvironmentVariable` for every `KEY=VALUE` line.

3. DI **binds** the env token onto the live runtime object (no hard-false pin):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. `/api/settings` **exposes** that bound value (not a hardcoded `false`):

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

5. `CTraderFixLogonHostedService` **does not re-pin** `RealCopyEnabled`. It only logs the current value (`RealCopyArmed={Armed}` at L68–70).

What is still false (and **does not** rescue claim 5):

| Surface | Value | Why it is not “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` POCO default | `false` (L35) | Unused by API DI. Not the live runtime flag. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Sender missing; flag can still be **armed**. |
| Persist `AllowFixSend` | hardcoded `false` (CopyTradingService L211) | Safety-by-absence, not a false pin of REAL_COPY. |
| `apps/fix-worker/Worker.cs` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Reads a **different** nested key. Worker `Program.cs` does **not** call `EnvFile.FindAndLoad`. |
| Architecture docs / README | document `false` | Docs are not the running bind. |

**Honest reading:** the live API **will** set `LiveRuntimeStatus.RealCopyEnabled=true` whenever `D:\Prop\.env` is loaded. The claim “REAL_COPY_EXECUTION stays false” is **false**.

Copy hop remains **SAFE_BY_ABSENCE** (no `35=D` on `CTraderFixSession`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). That is capital safety. It is **not** a proof that the flag stays false.

Older reports that say DI/logon hard-pin `RealCopyEnabled=false` (W500_RESEARCH_57 / 68 / 108 and CREDENTIALS_AND_COPY_STATUS “forced false”) are **stale** against current `DependencyInjection.cs` L41 and `CTraderFixLogonHostedService`.

---

## Capital / send

| Gate | Measured this slot |
|---|---|
| Product `CTraderFixSession` outbound | `(35, "A")` only; sockets disposed |
| `NewOrderSingleImplemented` | `false` |
| Persist `AllowFixSend` | forced `false` |
| LIVE traders / venue recon | `VenueReconciled=false`; promotion still cannot auto-LIVE |
| This slot sent `35=D` | **No** |
| This slot attached Manager | **No** |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`). Next sender would see the API runtime **armed**.

---

## Files read this slot (live paths only)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (residual `Build("D")` only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\.env` L73 boolean key only

Product source was **not** edited.
