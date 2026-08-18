# W500_VERIFY_8 — Adversarial live-path re-read (slot 8)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_8 (adversarial; did not trust prior swarm notes) |
| Slot | 8 |
| Purpose | Independently confirm five live-path claims from files. FAIL any claim not proven from the file. |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_8.md` |
| Product source modified | **No** |
| Live attach / Manager probe this slot | **No** |
| Live `35=D` sent | **No** (session class has no builder) |
| Secret values printed | **None** (quoted only boolean flag keys/values) |

## Verdict

**FAIL** — four of five assigned claims are proven from live product files. Claim 5 (`REAL_COPY_EXECUTION` stays false) is **disproven**. Lab `.env` sets `REAL_COPY_EXECUTION_ENABLED=true`; API `EnvFile.FindAndLoad` + `AddEnvironmentVariables` loads it; `DependencyInjection` binds it onto `LiveRuntimeStatus.RealCopyEnabled`; hosted FIX logon **does not** re-pin false.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Armed flag ≠ ticket. Copy hop still has no `NewOrderSingle` on `CTraderFixSession`; persist forces `AllowFixSend=false`; `CopyTradingService.NewOrderSingleImplemented` is `const false`.

---

## Claim table

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (code capability) | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")` first; L174 `GroupTotal`/`GroupNext` if request list empty. No plan-name filter. This slot did **not** re-attach, so live census is not re-proven. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (code capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest `GetAccountsAsync(null)` L48 walks every group from claim 2. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135. Only outbound MsgType is `(35, "A")` L96. Single `WriteAsync` of that logon (L47–49). Zero `NewOrderSingle` / `"D"` tokens. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | `.env` L73 `=true`. DI L41 binds env string `"true"` → `RealCopyEnabled`. API `/api/settings` L76 exposes that runtime bool. Logon host does not overwrite it. POCO default `CTraderFixOptions.RealCopyExecutionEnabled=false` is unused by this bind. |

Adversarial rule applied: one unproven/disproven assigned claim ⇒ slot **FAIL**.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines).

Startup after `app.Build()`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- `using TraderIntelligence.Infrastructure.Seeding;` (L6) exists for `BrokerCatalogSeed`, not `DemoSeeder`.
- Grep of `D:\Prop\apps` for `DemoSeeder`: **0** hits (API + both workers + web).
- Worker hosts also seed `BrokerCatalogSeed.EnsureAsync` only (`apps/mt5-worker/Program.cs` L15; `apps/fix-worker/Program.cs` L15).
- `DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Callers found: `tests/Integration/SeedingAndStoreTests.cs` L25 and `_tmp_*` harnesses under reports. That is **not** API startup.

Cannot claim the class is deleted. Can claim the **running API seed path is not DemoSeeder**.

---

## 2. Native connector — all groups via GroupRequestArray or GroupTotal — PASS (capability)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

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

- Request-first mask is `"*"` (manager-visible universe, not `MT5_GROUP_*` plan env).
- Cache walk (`GroupTotal`/`GroupNext`) is **fallback only** when the request list is empty.
- Live ingest uses this path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` → `GetGroupsCore`.

Honesty: this slot did not Connect. “Can list” is proven as **wired API**. “Did list 18 groups today” is **not** re-measured here. Older reports citing 18/8460 are **not** used as proof.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

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

`GetAccountsCore` (L189–213): if `group` is null/blank, it iterates **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup`. Catalog ingest passes `null` (`DealIngestionService` L48, L62).

Residual (not a claim fail): on hard request failure the code may fill from pump-cache `UserGetByGroup` before the `UserLogins` empty-array branch. The **empty** path still uses `UserLogins`. Completeness therefore depends on request retcode, not on a hardcoded login list.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Outbound construction:

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

- Only `WriteAsync` is L47–49 of that logon byte array.
- Grep of this file: `NewOrderSingle` = 0; `"D"` as MsgType = 0; `(35, "A")` = 1.
- Hosted caller `CTraderFixLogonHostedService` L48 / L54 calls `CTraderFixSession.TryLogonAsync` only. No send hop.

Residual (out of assigned type, recorded so later slots do not over-claim “product 35=D=0”): sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 is a **demo-gated CLI helper** (`tools/DemoFixTestTrade`), refused when host/sender looks live or account is `1369850`. Not referenced by `CTraderFixSession` or API DI.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Assigned claim requires the flag to **remain false**. Live files show the opposite on the API host.

1. Lab env (boolean only; no secrets):

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

2. API loads that file before DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
...
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L14–19) includes hard path `D:\Prop\.env` and `Environment.SetEnvironmentVariable`.

3. DI copies the env token onto process runtime (default of the bool property is C# `false` only until this assignment):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. Settings API publishes that same bool — **not** a hardcoded `false`:

```74:77:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

5. `CTraderFixLogonHostedService` logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled` (L69–70). It **never** assigns `RealCopyEnabled = false`. Older notes that claimed a hosted re-pin are **stale**.

What *is* still false (does **not** rescue claim 5):

| Surface | Value | Why it is not “flag stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (`CTraderFixOptions.cs` L35) | API DI does not bind this POCO for the settings/runtime flag |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` (L17) | sender missing |
| persist `RiskDecisionRecord.AllowFixSend` | hardcoded `false` (L211) | send gate, not the env flag |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | `false` | different name; unused by DI bind above |
| fix-worker `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback false | nested key; log-only |

**Cannot prove claim 5 from the files.** The live API path **arms** `REAL_COPY_EXECUTION_ENABLED` whenever the env value is the string `true`.

---

## Residuals / stale-note list

- `CREDENTIALS_AND_COPY_STATUS.md` / older W500 slots that say `RealCopyEnabled` is forced false: **stale**.
- `A002_api_dummy_path.md` “API still calls DemoSeeder”: **stale** vs current `Program.cs`.
- `A001_native_connector.md` “zero `GroupRequestArray` under src”: **stale**.
- Product hop remains SHADOW-only because NOS is unimplemented, not because the env flag is false.
- Next person who adds a `35=D` builder would see `LiveRuntimeStatus.RealCopyEnabled == true` on this machine.

---

## Checklist

- [x] Read live `Program.cs`, connector, `CTraderFixSession`, DI, `.env` boolean lines, copy persist
- [x] Did not trust other agents
- [x] Did not print secrets
- [x] Did not modify product source
- [x] Did not send `35=D`
- [x] Slot verdict FAIL because claim 5 is not proven (it is false)

**Slot 8 verdict: FAIL.**  
**Risk to capital: NONE (`SAFE_BY_ABSENCE`).**
