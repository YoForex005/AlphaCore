# W500_VERIFY_73 — Adversarial live-path verify (slot 73)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **73** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** from the files.

This slot re-read the product files listed below. Prior swarm reports (A002 DemoSeeder-on-boot, A001 “zero `GroupRequestArray`”, W500_68/108 “flag pinned false”, CREDENTIALS “forced false”) are treated as **stale** unless the current file still says that.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` = **0** hits under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count==0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")` at L96. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). `CTraderFixSession` cannot emit NewOrderSingle. Copy persist `AllowFixSend = false`. `NewOrderSingleImplemented = false`.

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
  - `apps/api/Program.cs` L156
  - `apps/mt5-worker/Program.cs` L15
  - `apps/fix-worker/Program.cs` L15

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

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). `tests/Integration/SeedingAndStoreTests.cs` still calls `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup. Hosted ingest scores `ListLoginsWithDealsAsync` only (`LiveIngestHostedService.cs` L106).

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

Request-first: `GroupRequestArray("*")`. Cache walk `GroupTotal`/`GroupNext` only if the request list is empty. `_pumpEnabled` never gates this walk.

Live catalog hop (`DealIngestionService.SyncCatalogAsync` L45–48):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` per connector (L56). No plan-env filter (`MT5_GROUP_*` unread on this walk).

**Honesty limits:** this slot did **not** re-attach Achiever/Starwave. File proves the connector **can** enumerate via those APIs. It does **not** prove today’s live group count. Adversarial residual: if `GroupRequestArray` returns OK with a **non-empty subset**, the `GroupTotal` fallback is skipped.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file, `ReadAccountsForGroup`:

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

`GetAccountsCore(null)` (L189–214) walks **every** group from `GetGroupsCore()` then `ReadAccountsForGroup`. Ingest calls `GetAccountsAsync(null)`. No `Take`/`Skip` on the connector walk.

**Honesty limits:** capability only. Not re-attached. `UserGetByGroup` is pump-cache and is only used when `UserRequestArray` hard-fails. Empty request array then uses `UserLogins`. If both request APIs fail and pump is off, the group can yield 0 users.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Grep this slot on that file for `35=D`, `NewOrderSingle`, `"D"`: **0 hits**.

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

Single wire write at L49 (`ssl.WriteAsync`). TCP + SSL disposed via `using`/`await using`. Hosted caller (`CTraderFixLogonHostedService` L48–58) only invokes `TryLogonAsync` (QUOTE 5211 + TRADE 5212). No heartbeat, no MD, no NewOrderSingle.

**Residual (does not revive claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139/L163/L197 is **not** `CTraderFixSession`. It is tools-only (`tools/DemoFixTestTrade`) and refuses live host / account `1369850` (L46–47). Not wired to API / workers / copy / DI.

Copy hop extra absence: `CopyTradingService.NewOrderSingleImplemented = false` (L18) and persist `AllowFixSend = false` (L306). Even if REAL_COPY is armed, no ticket encoder exists on this hop.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Claim is **false** on the API host.

Chain this slot:

1. `D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secrets quoted).
2. `D:\Prop\.env` L106: `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; not the send flag).
3. API `Program.cs` L10 `EnvFile.FindAndLoad()` (candidates include `D:\Prop\.env`).
4. API L13 `builder.Configuration.AddEnvironmentVariables()`.
5. DI L41 binds that key onto `LiveRuntimeStatus.RealCopyEnabled`. Grep `RealCopyEnabled =` under `D:\Prop\src`: **only that one assignment**.
6. `CTraderFixLogonHostedService` logs `_runtime.RealCopyEnabled` (L69–70) and **never** writes it back to false.
7. `/api/settings` L76: `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` — echoes the bound runtime, not a hard `false`.
8. `/api/health` L55: `realCopyEnabled = runtime.RealCopyEnabled`.

POCO default is still false (`CTraderFixOptions.RealCopyExecutionEnabled = false` L35) but that POCO is **not** what the API runtime uses. Fix-worker `Worker.cs` L21 reads nested `CTrader:RealCopyExecutionEnabled` (default false) for **log only**; it does not send.

Workers do **not** call `EnvFile.FindAndLoad` (only API + `tools/LiveBrokerProbe`). That does **not** save claim 5: the assigned API path **does** load `.env` and **does** arm the runtime.

W500_68/108 / CREDENTIALS “forced false” / A014 “DI pins false” are **stale**.

Copy cannot spend the arm today (`SAFE_BY_ABSENCE`). The **flag claim** still fails.

---

## Risk to capital

**NONE** (`SAFE_BY_ABSENCE`).

| Gate | File | State |
|---|---|---|
| Hosted FIX outbound | `CTraderFixSession` 135/135 | `(35,"A")` only |
| Copy sender | `CopyTradingService` L18 | `NewOrderSingleImplemented=false` |
| Persist send | `CopyTradingService` L306 | `AllowFixSend=false` |
| Venue recon | `CopyTradingService` L17 | `VenueReconciled=false` |
| Promotion | `BaselineScorer` (not re-litigated) | no auto-LIVE required for this verdict |

If a later sender is added while `.env` L73 stays `true` and DI L41 stays bound, the next hop would see **runtime armed**. That is residual, not current dest risk.

---

## Files read this slot (not other-agent reports)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json` (no `REAL_COPY_EXECUTION_ENABLED` key)
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header only; unused by API)
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (residual sibling)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\.env` L73 and L106 **flag names/booleans only**

---

## Verdict

**FAIL.** Claims 1–4 proved from live files (2–3 capability only; not re-attached). Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + DI L41 bind + no hosted re-pin. Copy hop remains `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
