# W500_VERIFY_57 — Adversarial live-path verify (slot 57)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_57.md` |
| Slot | **57** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. **Did not trust** sibling W500 / A014 / CREDENTIALS / INDEX prose. |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `apps/fix-worker/Worker.cs` (head), `NativeMt5BrokerConnector.cs` (459/459), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (63/63), `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs` (113/113), `CopyTradingService.cs` (gates + persist), `DealIngestionService.cs` (147/147), `LiveIngestHostedService.cs` (142/142), `LiveMt5Registration.cs` (95/95), `EnvFile.cs`, `BrokerCatalogSeed.cs`. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`(35, "D")`/`NewOrderSingle`, `RealCopyEnabled =`, flag-only `.env` L73 / L106. |

**Honesty rule:** FAIL any assigned claim that cannot be proved from a file this slot. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is **not** “flag stays false.” A Logon `35=A` is not a NewOrderSingle. Sibling `CTraderFixDemoTestTrade` is not `CTraderFixSession`.

---

## 0. Verdict (binding)

**FAIL.** Claims 1–4 are proved from live source. **Claim 5 is disproved:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` tokens under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count == 0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty array → L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Grep of this file for `35=D` / `(35, "D")` / `NewOrderSingle` = **0**. Only outbound MsgType is `(35, "A")` at L96. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon **does not** re-pin false. `/api/settings` echoes the runtime flag. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; `CTraderFixSession` still has no NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A006 “`/api/settings` hardcodes false”; README “Real NewOrderSingle is off (`REAL_COPY_EXECUTION_ENABLED=false`)” as a *runtime* statement.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 physical lines).

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
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- `DemoSeeder` product callers: class definition `src/Infrastructure/Seeding/DemoSeeder.cs` L14 + `tests/Integration/SeedingAndStoreTests.cs` L25. **API process does not call it.**

DI fail-closes Fake before connectors:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (read 95/95) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on that path.

**Residual (does not revive claim 1):**

- `DemoSeeder.cs` still exists for tests.
- Prior reports A002 / A005 / A010 / A011 that still say API startup calls `DemoSeeder` are **stale** against the current `Program.cs`.

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

File-proven:

- Primary walk is the request API `GroupRequestArray("*")` (all-groups mask).
- Fallback when the request array is empty is `GroupTotal` + `GroupNext`.
- `_pumpEnabled` does **not** gate this method.

Ingest uses it flag-blind:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` then `SyncBrokerAsync` per connector. No group-name filter from plan env.

**Honesty:** this slot did **not** re-attach Manager. Live census 18/8460 cited by siblings is **not** re-proved here. Claim 2 is a **source capability** (`can` list via those APIs). PASS_SOURCE, not a live count.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file.

`GetAccountsAsync(null)` walks every group from `GetGroupsCore()`:

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Per-group request path:

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

File-proven:

- First call is `UserRequestArray` (network request).
- `UserGetByGroup` is a **hard-fail** cache fallback only.
- Empty array → `UserLogins` + `UserRequestByLogins`.
- Catalog `GetAccountsAsync(null)` unions every group from claim 2.

Hosted scoring is narrower (`ListLoginsWithDealsAsync` only) — that does **not** shrink the catalog walk.

**Honesty:** ALL-traders completeness on the wire is not re-attached this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep of **this file** for `35=D`, `(35, "D")`, `NewOrderSingle` = **0**.

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

`TryLogonAsync` writes **one** frame (`ssl.WriteAsync` L49), reads one reply, then `using` disposes `TcpClient` / `SslStream`. No loop. No heartbeat. No `35=D` / `F` / `G` / `V`.

Hosted hop (`CTraderFixLogonHostedService` L48–58) calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists status. It never asks for a NewOrderSingle.

**Residual (does not revive a 35=D on `CTraderFixSession`):** sibling `CTraderFixDemoTestTrade.cs` can `Build("D", …)` at L139 / L163 / L197. Caller is `tools/DemoFixTestTrade/Program.cs` L44 (`SendAsync`). Not registered in API/DI/copy. Assigned claim is **`CTraderFixSession`**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show the opposite.

### 5.1 Lab env is armed

Flag-only grep of `D:\Prop\.env` (no other keys printed):

- L73: `REAL_COPY_EXECUTION_ENABLED=true`
- L106: `FEATURE_COPY_TRADING_ENABLED=true` (not claim 5; residual)

### 5.2 API loads that file into process env

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) searches cwd parents and hard-coded `D:\Prop\.env`, then `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line.

### 5.3 DI binds the env token onto runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of product `*.cs` for `RealCopyEnabled =` = **exactly one assignment**: this DI line. There is **no** later `= false` pin.

### 5.4 Hosted logon does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` updates Quote/Trade logon fields and **logs** `_runtime.RealCopyEnabled`. It never writes the flag. Comment at L69: `RealCopyArmed={Armed} NewOrderSingle still unimplemented`.

### 5.5 Settings API echoes the bound runtime

```71:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`apps/api/appsettings*.json` have **0** `REAL_COPY` keys. The process value is the env bind.

### 5.6 What still stays false (not claim 5)

These do **not** rescue claim 5:

| Surface | Value | Role |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (POCO L35) | **Unread** by `CTraderFixSession` / logon host |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Log-only; worker stamps sessions `Disconnected` |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` L17 | Sender missing |
| `CopyTradingService.VenueReconciled` | `const false` L16 | Gate |
| Persist `AllowFixSend` | hardcoded `false` L211 | Risk row never authorizes send |
| Send conjunction L217 | `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` | Unreachable live send |

`CopyTradingService.BuildBlockers` L316 adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only when** `!_runtime.RealCopyEnabled`. After the env bind, that blocker is **absent**. Remaining blockers still include `No NewOrderSingle sender — SAFE_BY_ABSENCE`.

**Claim 5 is therefore FAIL.** Architecture / README / CREDENTIALS “forced false” is stale against DI + `.env`.

---

## 6. Copy hop / capital (not an assigned claim; honesty)

- Assigned session file cannot emit `35=D`.
- Copy service cannot send (`NewOrderSingleImplemented=false`, persist `AllowFixSend=false`).
- Ingest is read-only Manager request APIs (`GroupRequestArray` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup`).
- This slot sent **zero** orders and did **not** attach.

Destination capital at risk from this process: **NONE** (`SAFE_BY_ABSENCE`). Residual risk: the next person who adds a `35=D` builder will see `LiveRuntimeStatus.RealCopyEnabled == true` on a host that loaded `D:\Prop\.env`.

---

## 7. Files read this slot (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- Flag-only lines of `D:\Prop\.env` (L73, L106)

Product source not modified.

---

## 8. Slot result

| Slot | Verdict | Risk to capital |
|---|---|---|
| 57 | **FAIL** (claim 5 disproved; 1–4 file-proven) | **NONE** (`SAFE_BY_ABSENCE`) |
