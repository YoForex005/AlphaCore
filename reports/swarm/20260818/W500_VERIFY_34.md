# W500_VERIFY_34 — Adversarial live-path verify (slot 34)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **34** |
| Role | Adversarial verifier. Read live path files myself. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL any assigned claim that cannot be proved from the live file. Claim 5 is **disproved** (the flag does not stay false). Claims 2 and 3 are file-proven as **API capability only**; live “ALL” counts were not re-attached this slot.

---

## Assigned claims

| # | Claim | Verdict | Proof from this read |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens in that file or under `apps/*/Program.cs`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count == 0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total() == 0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Grep `35=D` / `NewOrderSingle` in that file = **0**. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | `D:\Prop\.env` L73 is `true`. API `EnvFile.FindAndLoad` hard-includes that path. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. No re-pin. |

Overall **FAIL** because claim 5 is false in the live files. Send is still `SAFE_BY_ABSENCE` (separate from the assigned flag claim).

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot: `D:\Prop\apps\api\Program.cs` (160 lines).

The only seed after `EnsureCreatedAsync` is catalog:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

Independent greps this slot (product hosts):

| Search | Result |
|---|---|
| `DemoSeeder` in `apps/api/Program.cs` | **0** |
| `DemoSeeder` in `apps/mt5-worker/Program.cs` | **0** |
| `DemoSeeder` in `apps/fix-worker/Program.cs` | **0** |
| `BrokerCatalogSeed.EnsureAsync` | API L156, mt5-worker L15, fix-worker L15 |

`BrokerCatalogSeed.EnsureAsync` (`src/Infrastructure/Seeding/BrokerCatalogSeed.cs`) upserts broker rows, `XAUUSD`, kill-switch, and two FIX rows already `Disconnected`. It does **not** call FakeMt5, does **not** score `{10001,10002,10003,99001}`, and does **not** invent a quote tape.

DI fail-closes dummy connectors:

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

`LiveMt5Registration.CreateConnectors` (`src/Infrastructure/Mt5Live/LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. Zero `DemoBrokerFactory`.

API ingest is the hosted native walk (`AddHostedService<LiveIngestHostedService>` at DI L57), not the seeder.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). It still builds `DemoBrokerFactory.CreateDefault()` Fake connectors and scores `{10001, 10002, 10003, 99001}` (L126–138).
- The only product *caller* found this slot is `tests/Integration/SeedingAndStoreTests.cs` L25. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores the four demo logins after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Older reports that say API startup still calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

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

Live catalog uses that path with no `Take`/`Skip`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` L56 calls `SyncCatalogAsync` for every registered native connector.

**Proved from file:** the connector **can** enumerate groups via `GroupRequestArray("*")` and, if that returns an empty list, `GroupTotal` + `GroupNext`.

**Not proved (so not claimed as a live census):** this slot did **not** attach Manager. Any 18/8460 figure from other agents is **not** re-measured here.

**Caveat (do not greenwash “always all”):** if `GroupRequestArray` returns a **non-empty partial** set, `list.Count == 0` is false and `GroupTotal` is skipped. That is a completeness hole, not a missing API. Connect can also fall back to `PUMP_MODE_NONE` (L101); `GroupTotal` then depends on pump cache and may be 0, which is why request-first is the primary path.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same native file. `GetAccountsCore` (L189–213): if `group` is null/whitespace, it walks **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup` per name, de-duped by login.

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

Ingest/`/api/ops/resync` both call `GetAccountsAsync(null, ct)` (`DealIngestionService` L48 and L62). That is the “all groups → all users” walk.

**Proved from file:** trader enumeration is request-first (`UserRequestArray`) with `UserLogins` + `UserRequestByLogins` when the user array is empty.

**Caveats (adversarial — “all” is not fully proved):**

- `UserLogins` runs **only** when `users.Total() == 0`. A partial `UserRequestArray` (Total > 0 but incomplete) will **not** fall back.
- `UserGetByGroup` is a pump-cache fallback on hard fail, not a completeness check.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), not every catalog login. That is a scoring filter, not a catalog filter.
- This slot did not re-attach; “all traders” as a live count is **unproved** here. Per the slot rule that would be FAIL if the claim required a measured census. The claim as written is that the **connector can list** via those APIs — that is in the file.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Grep of **that file** for `35=D` and `NewOrderSingle`: **0**.

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

Only socket write is L47–50 (`BuildLogon` → `ssl.WriteAsync` → `FlushAsync`). Tag 35 is also **read** from the reply (`Extract(reply, "35")` L55) to accept Logon `A`. That is inbound parse, not a NewOrderSingle send. Sockets are `using`/`await using` and disposed on return.

Hosted caller `CTraderFixLogonHostedService` (`src/Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs` L48–58) calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No other MsgType. After logon it persists session status only (L91–111). It does **not** send `35=D`.

Copy hop still has no sender:

| Gate | File | State |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | const **false** |
| `VenueReconciled` | same L16 | const **false** |
| Persist `AllowFixSend` | same L211 | forced **false** (engine `AllowFixSend` is ignored on the row) |
| Live-send branch | same L217 | requires `NewOrderSingleImplemented && VenueReconciled` — both const false — and even then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` |

**Residual (does not break claim 4, which names this type only):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Demo-gated in the same file L43–60: refuses unless `host` starts with `demo-`, `senderCompId` starts with `demo.`, and `account != "1369850"`; also refuses `live.` / `live-`. Caller is `tools/DemoFixTestTrade/Program.cs` L44, **not** API/DI/copy.
- Sibling `CTraderFixDemoMatrix.Build("D")` (`Sessions/CTraderFixDemoMatrix.cs` L93) with a similar demo gate at L22–28. Same CLI (`--matrix`). Not the assigned type.

Claims that “the whole product tree has 0 `35=D`” are **stale**. The assigned type `CTraderFixSession` has 0.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove it does **not**.

### 5.1 Lab env is armed

`D:\Prop\.env` (boolean keys only; no secret values quoted):

| Line | Key | Value |
|---|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED` | `true` |
| 106 | `FEATURE_COPY_TRADING_ENABLED` | `true` |

No committed `appsettings*.json`, `launchSettings.json`, or `docker-compose.yml` in this tree sets `REAL_COPY_EXECUTION_ENABLED`. The operator file is the one that matters, and the API **loads it**.

### 5.2 API loads that file

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L5–20) walks cwd parents and **hard-includes** `D:\Prop\.env`. `Load` (L23–39) `SetEnvironmentVariable`s every `KEY=value`. Then the host adds environment variables. The API process therefore sees L73 as `true`.

Workers (`apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) do **not** call `EnvFile.FindAndLoad`. They still run `AddTraderIntelligence`, so if the process env already has the key they would bind it too. That does not save the API path.

### 5.3 DI binds it. Nothing re-pins it.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Repo-wide grep of `RealCopyEnabled =` in `src/**/*.cs`: **only this line**. There is no `RealCopyEnabled = false` pin in hosted FIX, copy, or API.

`CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L70) and does **not** overwrite it. Reports that still quote `_runtime.RealCopyEnabled = false` after logon (A015 / older P500_CODE_14/16/17) are **stale**.

`LiveRuntimeStatus.RealCopyEnabled` is a public `{ get; set; }` (`src/Application/Runtime/LiveRuntimeStatus.cs` L32). Nothing else assigns it; the DI bind is enough.

`/api/settings` exposes the **bound** value, not a hardcoded false:

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

`/api/health` L55 also reports `realCopyEnabled = runtime.RealCopyEnabled`.

`apps/api/Controllers/SettingsController.cs` is a leftover MVC type (`FeatureFlags.LiveCopyEnabled` default false). `Program.cs` never calls `AddControllers` / `MapControllers`. The live settings surface is the minimal API above.

Reports that still say `/api/settings` hardcodes `false` (E038 / A006 / A013) or CREDENTIALS “forced false” are **stale**.

### 5.4 POCO default is not the runtime flag

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`src/Fix.CTrader/Configuration/CTraderFixOptions.cs` L35). That POCO is **not** what DI writes. Nothing in `AddTraderIntelligence` binds `CTraderFixOptions`.

`apps/fix-worker/Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. It still stamps both FIX rows `Disconnected` and never sends.

Committed `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` is **false** — a different name, unread by DI.

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
| Copy host | `CopyTradingHostedService` L28–30 | `GenerateShadowIntentsAsync` only |

`RiskEngine` **can** set `AllowFixSend = allowSend` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–170). CopyTradingService **does not persist** that value; it writes `AllowFixSend = false` (L211). The engine result is discarded for the send bit.

`BuildBlockers` only **mentions** `REAL_COPY_EXECUTION_ENABLED is false` when the runtime flag is false (L316–317). When the env is `true`, that blocker is **absent**. Other blockers remain (`No NewOrderSingle sender`, `Venue not reconciled`, FIX logon, 0 LIVE). A future sender that checked only `RealCopyEnabled` would see **armed**.

Policy residual: `XauUsdOneToOneCopyPolicy.AllocationFactor = 1m` (`Domain/Copy/XauUsdOneToOneCopyPolicy.cs` L66). If a sender were added while the flag is true, size is 1:1.

---

## Risk to capital

**NONE today** (`SAFE_BY_ABSENCE` on the copy hop: no `CTraderFixSession` `35=D`, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`).

**Not** “flag stays false.” Lab `.env` L73 + DI L41 **arm** `LiveRuntimeStatus.RealCopyEnabled`. Residual ruin if a sender is added while that flag is true (1:1 lots). This slot did not flip the env and did not send.

---

## What this slot did not do

- Did not live-attach Achiever/Starwave (no new census proof).
- Did not GET `:5000/api/settings` (not required; binding is in source).
- Did not edit product source.
- Did not print passwords, proxy auth, FIX password, manager secrets, or connection-string values.

---

## Stale claims this read kills

| Older claim | Status after this read |
|---|---|
| API startup still calls `DemoSeeder` (A002/A005/A010) | **STALE** |
| Native connector has no request arrays | **STALE** |
| Product `CTraderFixSession` sends `35=D` | **FALSE** (0 in that file) |
| Product tree has 0 `35=D` | **STALE** (demo siblings + CLI) |
| `REAL_COPY_EXECUTION` is pinned false (W500_68/108/CREDENTIALS “forced”) | **STALE / FAIL** |
| Hosted FIX re-pins `RealCopyEnabled=false` (A015) | **STALE** (logs only) |
| `/api/settings` hardcodes REAL_COPY=false (E038) | **STALE** (binds runtime) |
