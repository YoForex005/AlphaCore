# W500_VERIFY_52 — Adversarial live-path verify (slot 52)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **52** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read the product files listed below. Prior W500_VERIFY / W500_RESEARCH reports were **not** treated as evidence.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/` and `apps/api`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*")` then, if the list is empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads it. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does not re-pin. |

Overall **FAIL** because claim 5 cannot be proved (the live files show the flag is armed).

---

## Files read this slot (primary)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup seed + `/api/settings` flag echo |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker startup seed |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker startup seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual four-login scorer (not API) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual startup seeder |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class still exists; not called from hosts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | RealCopy bind + Native-only register |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Dual Native connectors; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog walk `*` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` POCO |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Assigned FIX writer |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Residual off-hop `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Calls Logon only; no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unread by DI) |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const + persist `AllowFixSend=false` |
| `D:\Prop\.env` L73 / L106 | Boolean flags only |

Independent greps this slot (product, not reports):

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- `GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins` in `NativeMt5BrokerConnector.cs` = L155 / L174 / L223 / L230
- `35` in `CTraderFixSession.cs` = inbound parse L55/L73 + outbound `(35, "A")` L96. Zero `D`.
- `RealCopyEnabled =` in product `*.cs` = **only** `DependencyInjection.cs` L41

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

Same seed on both workers:

- `D:\Prop\apps\mt5-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\fix-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests (`tests/Integration/SeedingAndStoreTests.cs` L25) still call `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

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

Live catalog uses that path (flag-blind):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

**Proved from file:** the connector **can** enumerate groups with `GroupRequestArray("*")` and, if that returns an empty list, `GroupTotal` + `GroupNext`.

**Not proved (so not claimed as a live census):** this slot did **not** attach Manager. Prior 18/8460 figures are **not** re-measured here.

**Caveat (do not greenwash “always all”):** if `GroupRequestArray` returns a **non-empty partial** set, `list.Count == 0` is false and `GroupTotal` is skipped. That is a completeness hole, not a missing API.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file. `GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()`, then:

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

**Proved from file:** trader enumeration is request-first (`UserRequestArray`) with `UserLogins` fallback when the user array is empty. Catalog ingest calls `GetAccountsAsync(null)` (all groups).

**Caveats (adversarial):**

- `UserLogins` runs **only** when `users.Total() == 0`. A partial `UserRequestArray` (Total > 0 but incomplete) will **not** fall back.
- `UserGetByGroup` is a pump-cache rescue on **hard fail** of `UserRequestArray`, not the ALL-traders path.
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

Only socket write is L47–50 (`BuildLogon` → `ssl.WriteAsync`). Tag 35 is also **read** from the reply (`Extract(reply, "35")` L55) to accept Logon `A`. That is inbound parse, not a NewOrderSingle send. Sockets are `using`-disposed after one read.

Hosted caller `CTraderFixLogonHostedService` (`D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L48–58) calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No other MsgType.

Copy hop still has no sender:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- persist `AllowFixSend = false` (L211)
- `VenueReconciled = false` (const L16)
- LIVE-send branch L217 is unreachable while those consts stay false

**Residual (does not break claim 4, which names this type only):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` ×3 (`Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197). Demo-gated at L43–60 (refuses non-`demo-` host, non-`demo.` sender, `live.` / `live-`, account `1369850`). Called only from `tools/DemoFixTestTrade/Program.cs` L44 — **not** API / DI / copy.
- Sibling `CTraderFixDemoMatrix.Build("D")` (`Sessions\CTraderFixDemoMatrix.cs` L93). Same tree, not the assigned type.

Claims that “the product tree has 0 `35=D`” are **stale**. The assigned type `CTraderFixSession` has 0.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove it does **not**.

### 5.1 Lab env is armed

`D:\Prop\.env` line 73 (boolean only; no secrets quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

`.env` L106 is also `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; API settings hardcodes that one `true` at `Program.cs` L77 regardless).

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

`CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L70) as `RealCopyArmed={Armed}` and does not overwrite it.

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

Reports that still say `/api/settings` hardcodes `false` (E038 / A006 / A013) are **stale**.

### 5.4 POCO default is not the runtime flag

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35). That POCO is **not** what DI writes. `apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs; it never sends.

So: design default **false**; **API runtime** follows `.env` → **true**. Claim 5 as written (“stays false”) **fails**.

### 5.5 What still keeps capital safe (not the assigned claim)

Claim 5 is about the **flag**, not about send. Send is still blocked by absence:

| Gate | File | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | const **false** |
| `VenueReconciled` | same L16 | const **false** |
| Persist `AllowFixSend` | same L211 | forced **false** |
| Hosted FIX writer | `CTraderFixSession` | `35=A` only |
| Worker NOS | `apps/fix-worker/Worker.cs` | stamps `Disconnected`; never sends |

`BuildBlockers` only **mentions** `REAL_COPY_EXECUTION_ENABLED is false` when the runtime flag is false (L316–317). When the env is `true`, that blocker is **absent**. Other blockers remain. A future sender that checked only `RealCopyEnabled` would see **armed**.

`LiveRuntimeStatus.Snapshot()` (`LiveRuntimeStatus.cs` L42–44) already documents this: when armed, “NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent.”

---

## Risk to capital

**NONE today** (`SAFE_BY_ABSENCE` on the copy hop: no `CTraderFixSession` `35=D`, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`).

**Not** “flag stays false.” Lab `.env` + DI **arm** `LiveRuntimeStatus.RealCopyEnabled`. Residual ruin if a sender is added while that flag is true (policy `AllocationFactor` is 1:1 on `XauUsdOneToOneCopyPolicy`). This slot did not flip the env and did not send.

---

## What this slot did not do

- Did not live-attach Achiever/Starwave (no new 18/8460 proof).
- Did not GET `:5000/api/settings` (not required; binding is in source).
- Did not edit product source.
- Did not print passwords, proxy auth, FIX password, or connection-string secrets.
- Did not invoke `tools/DemoFixTestTrade`.

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
| Hosted logon re-pins `RealCopyEnabled=false` | **STALE** (logs only) |
