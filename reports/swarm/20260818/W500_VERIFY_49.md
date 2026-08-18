# W500_VERIFY_49 — Adversarial live-path verify (slot 49)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **49** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read product files independently. Prior swarm reports (A002/A005/A010/A011 “API still DemoSeeder”; A014/W500_68/108 “DI pins RealCopy false”; CREDENTIALS “forced false”) were **not** used as proof.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*")` then, if `list.Count==0`, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Live catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads that file. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. |

Overall **FAIL** because claim 5 cannot be proved — the files show the flag **does not stay false** on the API host.

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

Independent greps this slot (`*.cs` only):

| Scope | `DemoSeeder` hits |
|---|---|
| `D:\Prop\apps` | **0** |
| `D:\Prop\apps\api\Program.cs` | **0** |
| Product hosts that seed | API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15 — all `BrokerCatalogSeed.EnsureAsync` |
| Remaining `DemoSeeder` callers | `src/Infrastructure/Seeding/DemoSeeder.cs` (definition), `tests/Integration/SeedingAndStoreTests.cs` L25, plus `reports/swarm/20260818/_tmp_*` eval harnesses |

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

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14) and still scores `{10001, 10002, 10003, 99001}` after Fake ingest. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores the same four demo logins after a real `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

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

What the file **does** prove:

- Primary path is the Manager request API `GroupRequestArray("*")` (wildcard = all groups the manager may see).
- Fallback is `GroupTotal` + `GroupNext` when the first path leaves `list.Count == 0`.
- Live ingest (`DealIngestionService.SyncCatalogAsync` L45) calls `connector.GetGroupsAsync` with no group filter.

What the file **does not** prove (this slot did **not** attach):

- Measured group count on Achiever/Starwave.
- That `GroupRequestArray` succeeds on this LAN (Achiever still needs HTTP `ProxySet`; Starwave is direct).
- That `GroupTotal` fallback is complete when pump is `PUMP_MODE_NONE` (connect retries without pump at L101). Empty request + empty pump cache ⇒ empty list.

Claim wording is “**can** list all groups via those APIs.” Source capability is proven. Live completeness is **not** proven this slot → **PASS_SOURCE**, not a live census PASS.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file. Per-group reader:

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

`GetAccountsCore` with `group == null` walks **every** name from `GetGroupsCore()` (L189–203). Live catalog/ingest calls `GetAccountsAsync(null, ct)`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Probe path (`tools/LiveBrokerProbe/Program.cs` L25–26) is the same `GetGroupsAsync` + `GetAccountsAsync(null)`.

Caveats (capability, not measured ALL):

- Traders in a group that `GetGroupsCore` never returned are invisible.
- `UserLogins` only runs when `users.Total()==0` after `UserRequestArray` / `UserGetByGroup`.
- No `Take`/`Skip` on this reader. HTTP `GET /api/trades` still `Take(200)` — reconstructed tape page, not the Manager walk.
- This slot did **not** re-attach; do not treat 18/8460 as this-slot evidence.

**PASS_SOURCE.** File proves the intended “all groups → all users” walk. File cannot prove live completeness.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Outbound builder is Logon only:

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

Single socket write: `ssl.WriteAsync` of that logon (L47–50). Inbound `Extract(reply, "35")` only accepts `"A"` as LoggedOn (L55–56). Error path prints `35={msgType}` of the **reply** (L73). Zero `NewOrderSingle`. Zero literal `"D"` as an outbound MsgType.

Product caller of this type: `CTraderFixLogonHostedService` L48 and L54 (`TryLogonAsync` Quote then Trade). Then sockets dispose.

**Sibling residual (does not fail claim 4):** `CTraderFixDemoTestTrade.Build("D")` exists (L139/163/197) and `CTraderFixDemoMatrix` can write `Build("D", ...)`. That is a **different type**. Only caller of the demo helper is `tools/DemoFixTestTrade/Program.cs`. Demo helper refuses `live-*` / `live.` / account `1369850`. Copy/DI/API do **not** call it.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Claim is an invariant. The files **disprove** it.

### 5.1 Lab env is armed

`D:\Prop\.env` (boolean keys only; no secrets quoted):

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

### 5.2 API loads that file into process env

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L8–19) walks cwd/`../`/`D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=VALUE` line.

Workers (`apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`) do **not** call `EnvFile.FindAndLoad`. They still run `AddTraderIntelligence`, so if the process already has the key, they bind it too. The API host **always** loads `.env` when the file exists.

### 5.3 DI copies the env key onto runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Default of the bool is `false` only when the key is missing or not the string `true`. With `.env` L73 present, API `RealCopyEnabled` becomes **true**.

### 5.4 Hosted FIX logon no longer re-pins false

Read entire `CTraderFixLogonHostedService.cs` (112 lines). It writes `_runtime.Quote` / `_runtime.Trade` and logs `_runtime.RealCopyEnabled`. There is **no** assignment `_runtime.RealCopyEnabled = false`.

`/api/settings` and `/api/health` expose `runtime.RealCopyEnabled` (API `Program.cs` L55, L76). They are **not** hardcoded false.

### 5.5 What still stays false (does not rescue claim 5)

| Surface | Value | Why it is not claim 5 |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` | Unused POCO. DI does **not** bind env onto this property. |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Different nested key. Log-only. Stamps sessions `Disconnected`. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Send absence, not the flag. |
| `CopyTradingService.VenueReconciled` | `const false` | Same. |
| Persist `AllowFixSend` | literal `false` (L211) | Persist choke. RiskEngine `allowSend` still needs `Reconciled` (const false). |
| Committed `appsettings*.json` / `launchSettings.json` | key **absent** | Env file overrides via `AddEnvironmentVariables`. |
| Architecture / README / CREDENTIALS | say false / “forced” | **Stale** vs DI L41 + `.env` L73. |

Claim 5 as written (“stays false”) **FAIL**. Operator arm is **true** and is **bound**.

---

## Risk to capital

**NONE** on the product copy hop (`SAFE_BY_ABSENCE`):

- Hosted FIX writer is `CTraderFixSession` Logon `35=A` only; sockets disposed after one read.
- `NewOrderSingleImplemented = false`; live-send `if` also requires `VenueReconciled` (false).
- Persist `AllowFixSend = false` regardless of `RiskEngine.AllowFixSend`.
- Copy hosted service only calls `GenerateShadowIntentsAsync` (SHADOW rows).

**Residual (next implementer):** API runtime is **armed**. A future sender that keys off `_runtime.RealCopyEnabled` alone would see `true`. Sibling `CTraderFixDemoTestTrade` can emit `35=D` off-hop (demo-gated, tools-only). Do not treat env-true as a go-live.

This slot did not live-attach Manager and did not send FIX.

---

## Verdict

**FAIL.**

Claims 1 and 4 proved from files. Claims 2 and 3 proved as source capability only (not live-complete). Claim 5 **disproved**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false.
