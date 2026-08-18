# W500_VERIFY_37 — Adversarial live-path verify (slot 37)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **37** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read the product files. Prior swarm notes were treated as untrusted.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if empty, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()`. DI L41 binds `LiveRuntimeStatus.RealCopyEnabled`. No re-pin. |

Overall **FAIL** because claim 5 cannot be proved from the files — the files prove the flag is **armed**.

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
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- `DemoSeeder.SeedAsync` in product `*.cs` (excluding reports): **tests only** (`tests/Integration/SeedingAndStoreTests.cs` L25)

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (`src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed` (`src\Infrastructure\Seeding\BrokerCatalogSeed.cs`) writes broker rows + kill-switch + FIX session rows already `Disconnected`. It does not seed demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call it. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a real `SyncBrokerAsync`. That is a leftover worker scorer, **not** the API startup path.

Older reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

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

Live catalog uses that path (no group-name env filter):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetGroupsAsync` is `Task.Run(GetGroupsCore)` (connector L42–43). `_pumpEnabled` is **never** consulted on this walk.

**Proved from file:** the connector **can** enumerate groups with `GroupRequestArray("*")` **or**, if that list is empty, `GroupTotal` + `GroupNext`.

**Not proved (so not claimed as a live census):** this slot did **not** attach Manager. Prior 18/8460 figures are **not** re-measured here.

**Caveat (do not greenwash “always all”):** if `GroupRequestArray` returns a **non-empty partial** set, `list.Count == 0` is false and `GroupTotal` is skipped. That is a completeness hole, not a missing API.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file. `GetAccountsAsync(null)` (L189–213) walks **every** group from `GetGroupsCore()`, then:

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

**Proved from file:** trader enumeration is request-first (`UserRequestArray`) with `UserLogins` fallback when the user array is empty. Catalog ingest calls `GetAccountsAsync(null)` (all groups returned by claim 2).

**Caveats (adversarial):**

- `UserLogins` runs **only** when `users.Total() == 0`. A partial `UserRequestArray` (Total > 0 but incomplete) will **not** fall back.
- Hosted **scoring** is `ListLoginsWithDealsAsync`, not all catalog logins (`LiveIngestHostedService` L106). That is a scoring filter, not a catalog filter.
- This slot did not re-attach; “all traders” as a live count is **unproved** here.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Grep of that file for `35=D` and `NewOrderSingle`: **0**.

Only outbound builder:

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

Only socket write is L47–50 (`BuildLogon` → `ssl.WriteAsync`). Tag 35 is also **read** from the reply (`Extract(reply, "35")` L55) to accept Logon `A`. That is inbound parse, not a NewOrderSingle send. `using` disposes `TcpClient` / `SslStream` after one reply.

Hosted caller `CTraderFixLogonHostedService` (`src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L48–58) calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No other MsgType. Log line L69: “NewOrderSingle still unimplemented.”

Copy hop still has no sender:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- persist `AllowFixSend = false` (L211), even when `RiskEngine` would allow
- live-send branch L217 is unreachable (`NewOrderSingleImplemented` is const false)
- `CopyTradingHostedService` only calls `GenerateShadowIntentsAsync`

**Residual (does not break claim 4, which names this type only):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` ×3 (`Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197). Demo-gated: refuses non-`demo-` host, non-`demo.` sender, `live.` / `live-`, and account `1369850` (L43–47). Called from `tools/DemoFixTestTrade`, not API/DI/copy.
- Sibling `CTraderFixDemoMatrix` `Build("D")` (`Sessions\CTraderFixDemoMatrix.cs` L93). Same tree, not the assigned type.

Claims that “the product tree has 0 `35=D`” are **stale**. The assigned type `CTraderFixSession` has 0.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove it does **not**.

### 5.1 Lab env is armed

`D:\Prop\.env` line 73 (boolean only; no secrets quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

Sibling boolean at L106 (also not a secret): `FEATURE_COPY_TRADING_ENABLED=true`. API `/api/settings` hardcodes that FEATURE flag `true` (`Program.cs` L77) regardless of env.

No committed `*.json` / `*.example` / compose file in this slot’s search sets the `REAL_COPY_EXECUTION_ENABLED` key. The operator file is the one that matters.

### 5.2 API loads that file

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`src\Mt5\Env\EnvFile.cs` L5–20) walks cwd parents and **hard-includes** `D:\Prop\.env`. It `SetEnvironmentVariable`s every `KEY=value` (L38). Then the host adds environment variables. The API process therefore sees L73.

### 5.3 DI binds it. Nothing re-pins it.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Repo-wide grep of `RealCopyEnabled =` in `*.cs`: **only this line**. There is no `RealCopyEnabled = false` pin in hosted FIX, copy, or API.

`CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L70) and does not overwrite it.

`/api/settings` exposes the bound value, not a hardcoded false:

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

Reports that still say `/api/settings` hardcodes `false` (E038 / A006 / A013) or that DI/logon pin false (W500_68 / W500_108 / `CREDENTIALS_AND_COPY_STATUS.md` “forced”) are **stale**.

Leftover MVC `SettingsController` (`apps/api/Controllers/SettingsController.cs`) uses a different name (`FeatureFlags.LiveCopyEnabled` default false). The **live** route is the minimal-API `MapGet` above.

### 5.4 POCO default is not the runtime flag

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35). That POCO is **not** what DI writes. `apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs; it stamps FIX rows `Disconnected` and never sends.

So: design/docs default **false**; **API runtime** follows `.env` → **true**. Claim 5 as written (“stays false”) **fails**.

### 5.5 What still keeps capital safe (not the assigned claim)

Claim 5 is about the **flag**, not about send. Send is still blocked by absence:

| Gate | File | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | const **false** |
| `VenueReconciled` | same L16 | const **false** |
| Persist `AllowFixSend` | same L211 | forced **false** |
| Hosted FIX writer | `CTraderFixSession` | `35=A` only |
| Worker NOS | `apps/fix-worker/Worker.cs` | stamps `Disconnected`; never sends |

`BuildBlockers` only **mentions** `REAL_COPY_EXECUTION_ENABLED is false` when the runtime flag is false (L316–317). When the env is `true`, that blocker is **absent**. Other blockers remain. A future sender that checked only `RealCopyEnabled` would see **armed**. Shadow policy `AllocationFactor = 1m` (`XauUsdOneToOneCopyPolicy` L66) is 1:1 lots — dest-ruin **if** a sender is added while the flag is true.

`LiveRuntimeStatus.Snapshot()` itself admits the armed state (`src\Application\Runtime\LiveRuntimeStatus.cs` L42–44): “REAL_COPY armed. NewOrderSingle still unimplemented…”

---

## Risk to capital

**NONE today** (`SAFE_BY_ABSENCE` on the copy hop: no `CTraderFixSession` `35=D`, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`).

**Not** “flag stays false.” Lab `.env` + DI **arm** `LiveRuntimeStatus.RealCopyEnabled`. Residual ruin if a sender is added while that flag is true (policy `AllocationFactor=1m` is 1:1). This slot did not flip the env and did not send.

---

## What this slot did not do

- Did not live-attach Achiever/Starwave (no new 18/8460 proof).
- Did not GET `:5000/api/settings` (binding is in source).
- Did not edit product source.
- Did not print passwords, proxy auth, FIX password, or connection-string secrets.

---

## Stale claims this read kills

| Older claim | Status after this read |
|---|---|
| API startup still calls `DemoSeeder` (A002/A005/A010) | **STALE** |
| Native connector has no request arrays (A001) | **STALE** |
| Product `CTraderFixSession` sends `35=D` | **FALSE** (0 in that file) |
| Product tree has 0 `35=D` | **STALE** (demo siblings) |
| `REAL_COPY_EXECUTION` is pinned false (W500_68/108/CREDENTIALS “forced”) | **STALE / FAIL** |
| `/api/settings` hardcodes REAL_COPY=false (E038) | **STALE** (binds runtime) |
