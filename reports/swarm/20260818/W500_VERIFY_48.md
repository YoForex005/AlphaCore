# W500_VERIFY_48 — Adversarial live-path verify (slot 48)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **48** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read the live sources listed below. Prior swarm notes were used only as pointers; every claim below is backed by a file this slot opened.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API loads that file. DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`. No hosted re-pin. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

---

## Files read this slot (live, not reports)

| Path | What was proved |
|---|---|
| `D:\Prop\apps\api\Program.cs` (160) | Startup seed, env load, `/api/settings` echoes runtime |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | `net8.0-windows`; no `AddControllers` wiring |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Dead MVC (no `MapControllers`) |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key; `FeatureFlags:LiveCopyEnabled=false` (unread by live MapGet) |
| `D:\Prop\apps\mt5-worker\Program.cs` | `BrokerCatalogSeed` only |
| `D:\Prop\apps\fix-worker\Program.cs` | `BrokerCatalogSeed` only |
| `D:\Prop\apps\fix-worker\Worker.cs` | Reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log-only; not a re-pin) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer `{10001,10002,10003,99001}` — **not** API startup |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Catalog + FIX rows `Disconnected` |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class still exists; not called from `apps/` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fail-closed Native; **binds** `REAL_COPY_EXECUTION_ENABLED` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only; 0 Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459) | Request-first groups + traders |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` into process env |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog via ingest, no dummy fill |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135) | `(35,"A")` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; **does not** set `RealCopyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `RealCopyExecutionEnabled=false` (unread by DI runtime bit) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` is a writable bool |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | `NewOrderSingleImplemented=false`; persist `AllowFixSend=false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` conjunction includes `RealExecutionEnabled` |
| `D:\Prop\.env` L73 | Flag name + boolean **only** (`true`) |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Test-only `DemoSeeder.SeedAsync` |

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
- Product `src/**/*.cs` `DemoSeeder` = **1** (`Seeding\DemoSeeder.cs` type declaration)
- `AddControllers` / `MapControllers` in API `Program.cs` = **0**

Both workers also seed catalog only (`apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15). Neither is the API host.

DI fail-closes Fake before any seed:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. `FakeMt5BrokerConnector` remains on disk; `DemoSeeder` still calls `DemoBrokerFactory.CreateDefault()` at its L126. That is **test/seeder** code, not API boot.

`BrokerCatalogSeed.EnsureAsync` writes Achiever + StarwaveFX catalog rows, one XAU instrument, a default kill switch, and two FIX session rows already **`Disconnected`** (`LastError` says NewOrderSingle off). It does **not** insert FakeMt5 tape or logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call `DemoSeeder.SeedAsync` (`tests/Integration/SeedingAndStoreTests.cs` L25). **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup. Hosted ingest scores `ListLoginsWithDealsAsync`.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

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

Primary enumerator is the request API `GroupRequestArray("*")`. Cache walk `GroupTotal`/`GroupNext` runs only when the request list is empty. `_pumpEnabled` does **not** gate this method.

Live ingest uses that walk:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetAccountsAsync(null)` itself re-calls `GetGroupsCore()` then enumerates every returned name (`GetAccountsCore` L199–203).

**Not proved this slot:** a fresh Manager attach / census. File proves the connector **can** list groups via those APIs. Completeness (18/8460 cited elsewhere) is **not** re-attached here → **PASS_SOURCE**, not a live-count PASS.

A001 (“zero `GroupRequestArray` hits under `src`”) is **stale**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `ReadAccountsForGroup`:

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

Order of operations (file-proven):

1. `UserRequestArray(group)` — network request for every user in the group mask.
2. Cache `UserGetByGroup` **only** on a hard fail (not OK / OK_NONE / NOTFOUND).
3. If the user array is still empty: `UserLogins` then `UserRequestByLogins`.

`GetAccountsCore(null)` walks **every** group from `GetGroupsCore` and unions by login. Ingest/`LiveIngestHostedService` / `/api/ops/resync` all go through `GetAccountsAsync(null)` or `ListLoginsAsync` after that catalog.

**Not proved this slot:** a live trader count. File proves the ALL-traders request path exists. **PASS_SOURCE**.

A001 (“zero `UserRequestArray` / `UserLogins` under `src`”) is **stale**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep this slot on that file for `35=D` / `NewOrderSingle` / `(35, "D")` / `Build("D")` = **0**.

Only outbound MsgType:

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

`TryLogonAsync` writes **one** frame (`ssl.WriteAsync`), reads **one** reply, then `using` disposes `TcpClient` / `SslStream`. No heartbeat loop. No `35=D` / `F` / `G` / `V`. Inbound `35` is parsed only to accept Logon (`"A"`) or report reject.

Hosted caller (`CTraderFixLogonHostedService`) invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists status. It never builds an order.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 is **not** this class. It is demo-gated (refuses `live-*` / `live.` / account `1369850`) and is called from `tools/DemoFixTestTrade`, not from API DI / copy / logon host. Claim is specifically `CTraderFixSession`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Claim is **disproved**. The operator flag does **not** stay false on the API live path.

### 5.1 Lab `.env` is armed

`D:\Prop\.env` line 73 (flag + boolean only; neighboring secrets not quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

Line 106 (display flag, unused by DI): `FEATURE_COPY_TRADING_ENABLED=true`.

### 5.2 API loads that file before DI

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L5–20) walks cwd / parents and the hardcoded candidate `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`. Combined with `AddEnvironmentVariables()`, the boolean enters `IConfiguration["REAL_COPY_EXECUTION_ENABLED"]`.

Workers do **not** call `EnvFile.FindAndLoad`. That does not save the API process.

### 5.3 DI binds the env string. No pin.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is no hard `false`. There is no comment pinning the bit. `LiveRuntimeStatus.RealCopyEnabled` is a public setter (`LiveRuntimeStatus.cs` L32).

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults **false** (POCO L35). That POCO is **not** what `/api/settings` or `CopyTradingService` read. The live bit is `LiveRuntimeStatus`.

### 5.4 Hosted logon does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` updates Quote/Trade logon fields and **logs** `_runtime.RealCopyEnabled`. It never assigns `RealCopyEnabled = false`. Slots that claimed a hosted hard-false pin (W500_57 / 68 / 108 / 90 / 110) are **stale** against this file.

`apps/fix-worker/Worker.cs` L21 reads **`CTrader:RealCopyExecutionEnabled`** with default `false`. That is a **different** config key. It is log-only. It does not overwrite `LiveRuntimeStatus`.

### 5.5 Settings API echoes the armed bit

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

On an API process that loaded this lab `.env`, `featureFlags.REAL_COPY_EXECUTION_ENABLED` is **true**. E038 / A006 “hardcoded false at L45” is **stale**.

`SettingsController` still exists and would report `FeatureFlags:LiveCopyEnabled` default **false**, but `Program.cs` has **no** `AddControllers`/`MapControllers`. The live route is the MapGet above.

`CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is **stale**.

Architecture / README / `docs/*` still *say* the flag should stay false. Policy text is not a runtime pin.

### 5.6 What still blocks send (does not rescue claim 5)

Claim 5 is about the **flag staying false**. It does not. Send is still impossible by **absence**:

| Gate | File | Value |
|---|---|---|
| `CTraderFixSession` outbound | session L96 | `(35, "A")` only |
| `NewOrderSingleImplemented` | `CopyTradingService` L17 | `const false` |
| `VenueReconciled` | same L16 | `const false` |
| Persist `AllowFixSend` | same L211 | **always written `false`** |
| Live-send `if` | same L217 | needs `AllowFixSend && LIVE && NOS && Reconciled` |
| `RiskEngine.allowSend` | L147–150 | needs `RealExecutionEnabled && Reconciled && VenueHealthy` |

`CopyTradingService.BuildBlockers` will **not** add `"REAL_COPY_EXECUTION_ENABLED is false"` when the runtime bit is true (L316). The remaining blockers (`No NewOrderSingle sender — SAFE_BY_ABSENCE`, venue, 0 LIVE, FIX logon) still keep Pepperstone ticket-less.

---

## Risk to capital

**NONE** (`SAFE_BY_ABSENCE`).

- Assigned hop `CTraderFixSession` cannot emit NewOrderSingle.
- Copy service cannot persist a sendable intent (`AllowFixSend:=false`, `NewOrderSingleImplemented=false`).
- This slot did not attach Manager and did not send FIX.
- Armed `REAL_COPY` is a **runtime wish bit**. It is not a ticket. It **would** become a send license the moment a `35=D` builder is wired to `LiveRuntimeStatus.RealCopyEnabled`.

Do **not** treat claim-5 FAIL as a dest-PnL event. Dest fill count from this process remains 0 until a sender exists.

---

## Stale pins (do not re-cite as current)

| Pin | Why stale |
|---|---|
| A002 / A005 / A010 / A011 API `DemoSeeder` startup | Current API seeds `BrokerCatalogSeed` |
| A001 no `GroupRequestArray` / `UserRequestArray` in `src` | Both are in `NativeMt5BrokerConnector` |
| E038 / A006 `/api/settings` hardcoded REAL_COPY=false | MapGet echoes `runtime.RealCopyEnabled` |
| W500_57 / 68 / 90 / 108 / 110 DI or hosted pin-false | DI binds env; logon does not re-pin |
| `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” | `.env` true + bind |
| W500_130 / 150 “product `35=D=0` everywhere” | Sibling demo helper can `Build("D")` (off hop) |

---

## Verdict

**FAIL.** Claims 1–4 are file-proven (2/3 as source capability; this slot did not re-attach). Claim 5 is **false**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false on the API live path (`.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 + no re-pin). Copy hop remains `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
