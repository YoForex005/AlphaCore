# W500_VERIFY_72 — Adversarial live-path verify (slot 72)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **72** |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** by the current files (the opposite is true).

This slot re-read the product files listed below. Prior swarm notes (A002 DemoSeeder-on-boot, A001 “zero `GroupRequestArray`”, W500_68/108 “flag pinned false”, CREDENTIALS “forced false”) are treated as **stale** unless the current file still says that.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `D:\Prop\apps\api\Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens in that file. Zero `DemoSeeder` hits under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count==0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; L226 `UserGetByGroup` on hard fail; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog calls `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle` / `Build("D")`. Only outbound MsgType is `(35, "A")` at L96. One `WriteAsync` (L49). Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

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
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`:
  - `D:\Prop\apps\api\Program.cs` L156
  - `D:\Prop\apps\mt5-worker\Program.cs` L15
  - `D:\Prop\apps\fix-worker\Program.cs` L15
- `DemoSeeder` class still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14
- Only product C# caller of `DemoSeeder.SeedAsync` found: `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. Dual-AND `HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` via `IsSecret`.

**Residual (does not revive claim 1):**

- `DemoSeeder` file remains and still calls `DemoBrokerFactory.CreateDefault()` (`DemoSeeder.cs` L126) — tests only.
- `D:\Prop\apps\mt5-worker\Worker.cs` L31 still scores hardcoded `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover demo-login scorer, **not** the API seed path. Hosted ingest (`LiveIngestHostedService`) walks connectors via `DealIngestionService.SyncCatalogAsync`.

Claim 1 is **proved from the files**.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

`GetGroupsCore` is request-first, then pump-total fallback:

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

Live ingest uses that walk, not a demo tape:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

**What the file proves:** the connector **can** enumerate groups with `GroupRequestArray("*")` or, if that returns an empty list, `GroupTotal`/`GroupNext`.

**What this slot does not prove:** a live Manager attach, or that every server-side group is returned (manager ACL / pump-none / request failure can still yield an empty list). Claim is therefore **PASS_SOURCE**, not a live census.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file. `GetAccountsCore` with `group == null` walks **every** name from `GetGroupsCore()` (L201–202), then `ReadAccountsForGroup`:

```223:232:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Catalog path is `GetAccountsAsync(null, …)` (`DealIngestionService` L48; `LiveBrokerProbe` L26). Dedup is `byLogin` (`GetAccountsCore` L205–209).

**What the file proves:** per-group request (`UserRequestArray`) plus empty-array fallback (`UserLogins` → `UserRequestByLogins`). That is the assigned “can list all traders” capability.

**What this slot does not prove:** live account counts. No Manager attach. Prior 18/8460 figures are **not** re-summed here and are **not** used as proof.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

Assigned-file grep this slot:

- `35=D` / `Build("D")` / `NewOrderSingle` = **0**
- Only outbound MsgType: `(35, "A")` at L96 inside `BuildLogon`
- Single `ssl.WriteAsync` at L49
- Inbound `Extract(reply, "35")` at L55 is a **read**, not a send
- `using var tcp` + `await using var ssl` — sockets disposed

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Hosted copy hop calls **only** this class:

```48:58:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            _config["CTRADER_FIX_QUOTE_SENDER_SUB_ID"] ?? "QUOTE",
            _config["CTRADER_FIX_QUOTE_TARGET_SUB_ID"] ?? "QUOTE",
            username, password, stoppingToken);

        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            _config["CTRADER_FIX_TRADE_SENDER_SUB_ID"] ?? "TRADE",
            _config["CTRADER_FIX_TRADE_TARGET_SUB_ID"] ?? "TRADE",
            username, password, stoppingToken);
```

Copy pipeline has no FIX writer (`CopyTradingHostedService` ticks `TickRosterAsync` + `GenerateShadowIntentsAsync` only). `CopyTradingService` const `NewOrderSingleImplemented = false` (L17). Persist forces `AllowFixSend = false` (L306).

**Residual (does not break claim 4):** sibling helpers `CTraderFixDemoTestTrade` (`Build("D")` at L139/L163/L197) and `CTraderFixDemoMatrix` (`Build("D")` at L93) exist. They are **not** `CTraderFixSession`. Grep for those type names under `*.cs` = definitions only; the only invoker found is `D:\Prop\tools\DemoFixTestTrade\Program.cs` (CLI, demo-gated: refuse `live-*` / `live.` / account `1369850`). Not wired in DI/API/copy.

Claim 4 is **proved from the assigned file**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show it does **not**.

### 5.1 Lab env is true

`D:\Prop\.env` L73 (boolean only; no secret printed):

```
REAL_COPY_EXECUTION_ENABLED=true
```

Same file L106: `FEATURE_COPY_TRADING_ENABLED=true`.

API boot loads that file, then overlays process env:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L8–19) includes the hard path `D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=VALUE` line.

### 5.2 DI binds the env string onto runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep `RealCopyEnabled =` under `*.cs` this slot: **one** assignment. There is **no** later `RealCopyEnabled = false` pin.

### 5.3 Hosted FIX logon does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` reads password/host/account, calls `CTraderFixSession.TryLogonAsync` twice, copies logon status onto `_runtime.Quote` / `_runtime.Trade`, and **logs** `_runtime.RealCopyEnabled` (L68–70). It never writes `RealCopyEnabled`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (`CTraderFixOptions.cs` L35) but is **not** registered/bound in `AddTraderIntelligence`. That POCO default is dead on the API/copy host. The live switch is `LiveRuntimeStatus.RealCopyEnabled`.

### 5.4 API surface echoes the bound runtime

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

`FEATURE_COPY_TRADING_ENABLED` is a **literal true** on the settings payload (env L106 is unused for this key). `REAL_COPY` is **not** hardcoded false.

`appsettings.json` has `FeatureFlags.LiveCopyEnabled: false` — a **different** name. It is not what DI reads. `SettingsController` (`D:\Prop\apps\api\Controllers\SettingsController.cs`) also uses `LiveCopyEnabled` and is **not** the mapped `/api/settings` (minimal API in `Program.cs` wins).

`apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` default **false** (another unread name vs `REAL_COPY_EXECUTION_ENABLED`). That worker still stamps TRADE `Disconnected` / “NewOrderSingle remains off.” It is not a pin of the API runtime flag.

### 5.5 Copy hop is still SAFE_BY_ABSENCE (does not rescue claim 5)

Claim 5 is about the **flag staying false**, not about whether a ticket can be sent.

Send remains impossible on this process because:

| Gate | File | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const false` |
| `VenueReconciled` | `CopyTradingService.cs` L17 | `const false` |
| Persist `AllowFixSend` | `CopyTradingService.cs` L306 | literal `false` (risk `AllowFixSend` ignored) |
| Branch that would mark live send | `CopyTradingService.cs` L312 | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — then still only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED` |
| Hosted FIX writer | `CTraderFixSession` | `35=A` only |
| Copy host | `CopyTradingHostedService.cs` | roster + shadow intents only |

`BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** `!_runtime.RealCopyEnabled` (L478–479). With env `true` + DI bind, that blocker is **absent**. The remaining blockers still include `SAFE_BY_ABSENCE` (`NewOrderSingleImplemented=false`).

**Therefore:** dest capital is **not** at risk today, but the assigned claim “stays false” is **false**. Next sender wired against `LiveRuntimeStatus.RealCopyEnabled` would see the host **armed**.

Claim 5 = **FAIL**.

---

## Risk to capital

**NONE** on the current copy hop (`SAFE_BY_ABSENCE`).

No `35=D` in `CTraderFixSession`. No NewOrderSingle implementation. Persist `AllowFixSend=false`. Hosted copy writes SHADOW/intent rows only.

Residual **arming** (not a send): `.env` L73 `true` + DI L41 bind + no hosted re-pin. Sibling demo CLI can `Build("D")` only when demo-gated and is unused by copy/API.

This slot did **not** attach Manager, did **not** send FIX, did **not** print secrets, did **not** edit product source.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | unread `CTrader:RealCopyExecutionEnabled` |
| `D:\Prop\apps\api\appsettings.json` | `LiveCopyEnabled` name mismatch |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused vs minimal API |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | actual seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | exists, not API-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Native-only + flag bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | two Native connectors |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | no sender; persist `AllowFixSend=false` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | catalog via Native |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow only |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroups` + `GetAccounts(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag surface |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads `D:\Prop\.env` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\.env` L73 / L106 | booleans only |

---

## Verdict

**FAIL.** Claims 1–4 are proved from the current files (2–3 as source capability only). Claim 5 is disproved: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false. Dest risk **NONE** (`SAFE_BY_ABSENCE`).
