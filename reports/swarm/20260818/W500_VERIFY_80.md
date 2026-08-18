# W500_VERIFY_80 — Adversarial live-path verify (slot 80)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **80** |
| Role | Adversarial verifier. Read live path files independently. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files). Claims 2–3 are file-capability only (this slot did not re-attach Manager).

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `D:\Prop\apps\api\Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Token `DemoSeeder` = **0** in that file and in `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count == 0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Grep `35=D` / `"D"` as MsgType = **0**. Only outbound MsgType is `(35, "A")` L96. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads it. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved from the files (it is false on the API host).

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot: `D:\Prop\apps\api\Program.cs` (160/160).

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

| Scope | `DemoSeeder` hits |
|---|---|
| `D:\Prop\apps\api\Program.cs` | **0** |
| `D:\Prop\apps\mt5-worker\Program.cs` | **0** (seeds `BrokerCatalogSeed.EnsureAsync` L15) |
| `D:\Prop\apps\fix-worker\Program.cs` | **0** (seeds `BrokerCatalogSeed.EnsureAsync` L15) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists (`public static class DemoSeeder` L14) |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | test-only `SeedAsync` |
| `D:\Prop\reports\swarm\20260818\_tmp_*` | leftover eval harnesses only |

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`DemoBrokerFactory.CreateDefault()` (Fake 10001/10002/10003/99001) is called only from `DemoSeeder.cs` L126. That factory is **not** on the API/worker DI path.

Hosted ingest (`LiveIngestHostedService`) walks `registry.All()` and calls `DealIngestionService.SyncCatalogAsync` → `GetGroupsAsync` + `GetAccountsAsync(null)`. No seeder on that path.

`BrokerCatalogSeed.EnsureAsync` writes broker rows, XAUUSD instrument, kill-switch, and **Disconnected** FIX session placeholders. It does **not** insert dummy traders, dummy deals, or forged `LoggedOn`.

**Residual (does not revive claim 1):**

- `DemoSeeder.cs` remains on disk for tests. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores leftover logins `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a worker scorer leftover, **not** API startup.
- Prior reports that say API startup still calls `DemoSeeder` are **stale** against current `Program.cs`.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read this slot: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458).

```144:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Live catalog driver (`DealIngestionService.SyncCatalogAsync` L45–49) calls `GetGroupsAsync` then `GetAccountsAsync(null)`. No group-name filter. No `Take`/`Skip` on the catalog walk.

**File-proved:** the native connector **attempts** ALL groups via `GroupRequestArray("*")`, then `GroupTotal`/`GroupNext` if the request array is empty.

**Not proved this slot (so not PASS_LIVE):**

- This slot did **not** attach Manager. Live group count is not re-measured here.
- Fallback `GroupTotal` runs only when `list.Count == 0`. A **partial** `GroupRequestArray` (Count > 0 but not every group) would **not** fall through.
- Connect tries pump (`PUMP_MODE_GROUPS|USERS|POSITIONS`) then `PUMP_MODE_NONE`. Fetch is request-first; `_pumpEnabled` does not gate `GetGroupsCore`.

Claim 2 is **PASS_SOURCE** (capability in file). It is **not** a live census.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, accounts walk:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        ...
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            ...
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
```

```216:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`SyncCatalogAsync` L48: `GetAccountsAsync(null, ct)` → every group from claim 2, then per-group `UserRequestArray`, then `UserLogins` only if that group’s user array is empty.

**File-proved:** the ALL-traders walk exists and uses the named Manager APIs.

**Not proved this slot:**

- No live attach. Account totals not re-measured.
- `UserLogins` is skipped when `users.Total() > 0`. Partial `UserRequestArray` would not union with `UserLogins`.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), not every catalog login. Catalog persist is still ALL accounts; scoring is deals-only. That does not undo the fetch walk.

Claim 3 is **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read this slot: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

Grep this file:

| Pattern | Hits |
|---|---|
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35,` | **1** — L96 `(35, "A")` |
| `WriteAsync` | **1** — L49 logon only |

Outbound builder:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        ...
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            ...
        };
        return Assemble(fields);
    }
```

Hosted caller (`CTraderFixLogonHostedService` L48–58) calls `TryLogonAsync` twice (QUOTE :5211, TRADE :5212) and persists status. No order send. Tcp/Ssl sockets are `using`-disposed after one reply.

**Off-hop residual (does not falsify claim 4):**

- `CTraderFixDemoTestTrade.Build("D")` at L139/L163/L197 and `CTraderFixDemoMatrix.Build("D")` at L93 exist.
- Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only. **Not** in API / workers / DI / `CopyTradingService`.
- Demo helper refuses `live-*` host, `live.` sender, and account `1369850` (`CTraderFixDemoTestTrade.cs` L43–47).
- This slot did **not** invoke that CLI.

Claim 4 is about `CTraderFixSession`. That type has **no** `35=D`. **PASS**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim is an invariant: the flag **stays** false. The live files disprove it.

**Lab env (boolean only; no secrets):**

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

`appsettings.json` / `appsettings.Development.json` / `launchSettings.json` do **not** set `REAL_COPY_EXECUTION_ENABLED`. The lab value comes from `.env`.

**API loads that file:**

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L8–19) includes the hardcoded candidate `D:\Prop\.env` and calls `Environment.SetEnvironmentVariable`. `AddEnvironmentVariables()` then makes the key visible to DI.

**DI binds it; nothing re-pins false:**

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of product `*.cs` for `RealCopyEnabled =` : **only** that DI assignment. `CTraderFixLogonHostedService` L69 **reads** `_runtime.RealCopyEnabled` for a log line (`RealCopyArmed={Armed}`) and does **not** assign false.

`/api/settings` L71–77 exposes `runtime.RealCopyEnabled` as `featureFlags.REAL_COPY_EXECUTION_ENABLED`. `/api/health` L55 exposes `realCopyEnabled = runtime.RealCopyEnabled`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) but that POCO is **not** what DI writes onto `LiveRuntimeStatus`. Citing the POCO default as the live pin is **stale**.

`CopyTradingService` L285 passes `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine`. Persist still forces `AllowFixSend = false` (L306) and `NewOrderSingleImplemented` is `const false` (L18). Those block **send**. They do **not** keep the **flag** false.

Claim 5 is **FAIL**. The flag is env-bound and **true** on this lab host.

---

## Risk to destination capital

**NONE** (`SAFE_BY_ABSENCE`) on the product copy hop, **despite** claim 5.

| Gate | State this slot |
|---|---|
| `CTraderFixSession` outbound MsgType | `35=A` only |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` (L18) |
| `CopyTradingService.VenueReconciled` | `const false` (L17) |
| Persist `RiskDecisionRecord.AllowFixSend` | hardcoded `false` (L306) |
| Dead send branch L312 | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — last two const false |
| Hosted copy | `GenerateShadowIntentsAsync` only; log “NewOrderSingle still unimplemented” |
| Demo `Build("D")` | tools-only, demo-gated, not invoked |

Armed flag ≠ ticket. If a sender is added later, DI will already present `RealCopyEnabled=true`. That is a **future** risk, not a fill today.

This slot did not send `35=D`, did not attach Manager, and did not print secrets.

---

## Stale claims (do not reuse)

| Claim | Why stale |
|---|---|
| API startup still calls `DemoSeeder` | Current `Program.cs` seeds `BrokerCatalogSeed` only |
| `REAL_COPY` is hard-pinned false in DI / hosted logon | DI binds env; logon does not re-pin |
| `CTraderFixOptions.RealCopyExecutionEnabled=false` is the live pin | Unused by `LiveRuntimeStatus` |
| Product tree has zero `35=D` builders | False if it includes `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` (off-hop) |
| Live census 18/8460 from this slot | **Not** re-attached here — do not cite as this slot’s measurement |

---

## Verdict

**FAIL.**

Claims 1 and 4 are proved from the files. Claims 2 and 3 are proved as **source capability** only (no live attach). Claim 5 is **disproved**: `REAL_COPY_EXECUTION_ENABLED=true` is loaded from `D:\Prop\.env` L73 and bound by `DependencyInjection.cs` L41.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
