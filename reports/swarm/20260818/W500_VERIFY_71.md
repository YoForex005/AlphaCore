# W500_VERIFY_71 — Adversarial live-path verify (slot 71)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **71** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read the product files listed below. Prior W500_VERIFY / W500_RESEARCH / INDEX / SWARM_LOG rows were **not** treated as evidence.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. Class remains on disk for tests. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` (L155). If `list.Count == 0`, walks `GroupTotal`/`GroupNext` (L174–L179). Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first (L223); if `users.Total()==0`, `UserLogins` + `UserRequestByLogins` (L230–L232). Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")` L96. One `WriteAsync` L49. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads it. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin. `/api/settings` echoes the runtime bool. |

Overall **FAIL** because claim 5 cannot be proved (the live files show the flag is armed).

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Armed flag has no sender.

---

## Files read this slot (primary)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup seed + `/api/settings` flag echo (160 lines) |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker startup seed |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker startup seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual four-login scorer (not API) |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unused MVC settings surface (`LiveCopyEnabled`, different name) |
| `D:\Prop\apps\api\appsettings.json` | `FeatureFlags.LiveCopyEnabled=false` (unbound to `LiveRuntimeStatus`) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual startup seeder |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class still exists; not called from hosts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | RealCopy bind + Native-only register |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Dual Native connectors; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog walk `*` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` POCO |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog uses Native `SyncCatalogAsync` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Assigned FIX writer |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Residual off-hop `Build("D")` (demo-gated) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Calls Logon only; no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unread by DI) |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const + persist `AllowFixSend=false` |
| `D:\Prop\.env` L73 / L106 | Boolean flags only |

Independent greps this slot (product, not reports):

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\src` = class declaration only (`DemoSeeder.cs` L14)
- `DemoSeeder` under `D:\Prop\tests` = `SeedingAndStoreTests.cs` L25 only
- `GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins` in `NativeMt5BrokerConnector.cs` = L155 / L174 / L223 / L230
- `35` in `CTraderFixSession.cs` = inbound parse L55/L73 + outbound `(35, "A")` L96. Zero `D`.
- `RealCopyEnabled =` in product `*.cs` = **only** `DependencyInjection.cs` L41
- `new ExecutionIntent` / `ExecutionIntents.Add` under `src/` = **0**
- `EnvFile` under `apps/` = API `Program.cs` L10 only

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

The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`. There is **no** `DemoSeeder.SeedAsync` token in this file.

Workers match:

```13:15:D:\Prop\apps\mt5-worker\Program.cs
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
```

```13:15:D:\Prop\apps\fix-worker\Program.cs
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
```

DI fail-closes Fake/dummy before any seeder:

```36:37:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`LiveMt5Registration.CreateConnectors` returns two `NativeMt5BrokerConnector` instances only (Achiever + Starwave). No `FakeMt5BrokerConnector`.

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Integration test `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. That is **not** API/worker startup.

Residual (does not fail claim 1): `apps/mt5-worker/Worker.cs` L31 still scores hardcoded `{10001, 10002, 10003, 99001}` after a real `SyncBrokerAsync`. Hosted API ingest (`LiveIngestHostedService`) uses `DealIngestionService.SyncCatalogAsync` + `ListLoginsWithDealsAsync`, not those four logins.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

Primary walk:

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

`GetAccountsAsync(null)` (ingest catalog) calls `GetGroupsCore()` then walks every returned group name.

Honest limits (why this is **PASS_SOURCE**, not a live census):

- `GroupTotal`/`GroupNext` run **only** when the request-array list is empty. A non-empty but incomplete `GroupRequestArray("*")` would **not** fall through.
- This slot did **not** attach Manager or re-sum probe JSON. File capability is proved; live “18 groups” is **not** re-proved here.

Ingest uses the same walk:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

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

`GetAccountsCore(null)` iterates every group from `GetGroupsCore()` and unions by login. That is the ALL-traders catalog path.

Honest limits:

- `UserGetByGroup` is a **hard-fail cache fallback**, not the ALL-traders primary.
- `UserLogins` runs only when `users.Total()==0`. A non-empty but incomplete `UserRequestArray` would skip it.
- Completeness vs the live book is **not** re-attached this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Hits on `35` / `WriteAsync` / `"D"` / `NewOrderSingle` in **this file**:

| Line | What |
|---|---|
| L49 | `ssl.WriteAsync` — the only outbound write |
| L55 | inbound parse `Extract(reply, "35")` |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound field `(35, "A")` Logon |
| — | **0** `NewOrderSingle` |
| — | **0** `"D"` / `35=D` |

Logon body:

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

`CTraderFixLogonHostedService` calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and then disposes. It does not send any other MsgType.

Residual **off this type** (does **not** fail claim 4):

- `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Gated: refuses non-`demo-` host, non-`demo.` sender, `live.` / `live-`, and account `1369850`. Wired from `tools/DemoFixTestTrade`, not API/DI/copy.
- `CTraderFixDemoMatrix.Build("D")` at L93 — same sibling, not the assigned session class.

Copy hop still cannot emit a ticket:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L18)
- persist `AllowFixSend = false` (L306)
- `VenueReconciled = false` (const L17)
- 0 `ExecutionIntent` writers under `src/`

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live files show the opposite on the API host.

1. Lab env (boolean only; no secrets):

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

2. API loads that file before DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes hard path `D:\Prop\.env` (L14) and writes each `KEY=value` into the process environment (L38).

3. DI binds the env string onto the singleton the rest of the host reads:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

This is the **only** `RealCopyEnabled =` assignment in product `*.cs`.

4. Hosted logon does **not** re-pin false. It only logs the already-bound value:

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

5. `/api/settings` echoes runtime, not a hard `false`:

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

Surfaces that still say false are **not** the API runtime pin:

| Surface | What it actually does |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled { get; set; } = false` | POCO default. **Not** registered/bound by DI. Unused by logon host. |
| `appsettings.json` `FeatureFlags.LiveCopyEnabled=false` | Different name. Not copied onto `LiveRuntimeStatus`. |
| `SettingsController.FeatureFlags.LiveCopyEnabled` | MVC leftover; live `/api/settings` is the minimal-API map above. |
| `fix-worker/Worker.cs` L21 | Reads `CTrader:RealCopyExecutionEnabled` default **false** (nested key, not `REAL_COPY_EXECUTION_ENABLED`). Log-only. Workers do **not** call `EnvFile.FindAndLoad`. |
| `CopyTradingService.BuildBlockers` L478 | Adds `"REAL_COPY_EXECUTION_ENABLED is false"` only when `_runtime.RealCopyEnabled` is false — so on the API host this blocker **does not fire**. |

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L46). After API boot with this `.env`, that is **true**.

Claim 5 is therefore **FAIL**. Docs / CREDENTIALS / README that still say “forced false” are stale relative to these files.

---

## Risk to capital

**NONE** — `SAFE_BY_ABSENCE`.

The armed flag is a license bit with no sender:

- Assigned hop `CTraderFixSession` outbound is `35=A` only.
- `NewOrderSingleImplemented` const false.
- Persist `AllowFixSend:=false`.
- 0 `ExecutionIntent` writers.
- This slot sent no `35=D` and did not attach Manager.

If a sender were added tomorrow against this DI bind, the next API process would already see `RealCopyEnabled=true`. That is a residual, not current dest risk.

---

## Residuals (do not flip claims 1–4)

- Sibling `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` can `Build("D")` off the copy hop (demo-gated).
- `mt5-worker/Worker.cs` still scores four dummy logins after a real catalog sync.
- `SettingsController` + `appsettings.json` `LiveCopyEnabled` is a split-brain unused name.
- `CTraderFixOptions.RealCopyExecutionEnabled` default false is unread.
- Claims 2–3 are source capability only; this slot did not live-attach.

---

## Verdict

**FAIL.**

Claims 1 and 4 proved from files. Claims 2 and 3 proved as **file capability** (`PASS_SOURCE`), not as a live ALL-groups/ALL-traders census. Claim 5 is **disproved**: `REAL_COPY_EXECUTION` does **not** stay false on the API host.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
