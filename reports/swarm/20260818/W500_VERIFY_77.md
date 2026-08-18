# W500_VERIFY_77 — Adversarial live-path verify (slot 77)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **77** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes, INDEX prose, and CREDENTIALS are **not** evidence. Claim 5 is **disproved**. `SAFE_BY_ABSENCE` is not “flag stays false.”

---

## Assigned claims (AND)

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound MsgType is `A`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from files this slot. Claim 5 cannot be proved: lab `.env` L73 is `true`, API loads that file, DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and the logon host does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` + env load |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (nested key) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\api\appsettings.json` | committed flags (`LiveCopyEnabled=false`; no `REAL_COPY_*`) |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no `REAL_COPY` env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC controller (not the live hop) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | hosted ingest + score |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType (full 135) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual sibling `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented; persist `AllowFixSend=false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` formula |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines).

Env is loaded first, then DI, then catalog seed — never `DemoSeeder`:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

Startup seed (after routes, before `app.Run()`):

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync` on this path. `using TraderIntelligence.Infrastructure.Seeding;` exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0**.

Workers use the same catalog seed:

- `D:\Prop\apps\fix-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\mt5-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`

DI fail-closes unless both manager passwords pass `IsSecret`, then registers **Native ×2** only (`LiveMt5Registration.CreateConnectors`). Hosted: `LiveIngestHostedService`, `CTraderFixLogonHostedService`, `CopyTradingHostedService`. No Fake substitution on the throw path.

`DemoSeeder` still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 and is called from `tests/Integration/SeedingAndStoreTests.cs` L25 plus leftover `_tmp_*` programs. That is **not** the API process.

Residual (does **not** put DemoSeeder on API boot): `apps/mt5-worker/Worker.cs` L31–35 still scores `{10001,10002,10003,99001}`. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

**Claim 1 proven.** A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (source capability)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore`.

Primary walk is the request API with mask `"*"` (manager-visible ALL). Empty result falls back to pump-cache `GroupTotal` / `GroupNext`. Zero `Take`/`Skip` in this file.

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

Public surface: `GetGroupsAsync` → `GetGroupsCore`. Ingest `DealIngestionService.SyncCatalogAsync` L45 calls `GetGroupsAsync`.

**Not proven this slot:** live completeness (manager rights, `*` vs server universe, request-OK-but-partial vs `GroupTotal` only when `list.Count==0`). This slot did **not** attach. Claim is **file capability**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source capability)

`GetAccountsAsync(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

```216:232:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Ingest uses the ALL path: `GetAccountsAsync(null)` at `DealIngestionService.cs` L48 and L62.

`UserGetByGroup` is cache fallback only on hard request fail. Empty array still tries `UserLogins`.

**Not proven this slot:** live trader counts. Prior census numbers (18/8460) are **not** re-attached here and are not used as proof.

---

## 4. CTraderFixSession has no 35=D — PASS

Full read of `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

- `NewOrderSingle` token: **0**
- `35=D` / `Build("D")` token: **0**
- Only outbound MsgType: `(35, "A")` in `BuildLogon` L96
- Single wire write: `ssl.WriteAsync` L49
- Sockets/`SslStream` disposed via `using`

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

Hosted logon (`CTraderFixLogonHostedService.cs`) calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and persists status. It never builds a NewOrderSingle.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.cs` `Build("D")` at L139/L163/L197. Demo-gated (`demo-` host / `demo.` sender; refuses `live-*` / `live.` / account `1369850`). Called from `tools/DemoFixTestTrade`, not copy/DI/API. Claim is specifically `CTraderFixSession`.

`grep new ExecutionIntent` under `D:\Prop\src` = **0**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproved)

Assigned claim is that the flag **stays false**. Live wiring does the opposite.

### 5.1 Lab env is armed (booleans only)

`D:\Prop\.env`:

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

No other `.env` values quoted.

### 5.2 API loads that file into process env

`EnvFile.FindAndLoad()` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) walks CWD parents then hard path `D:\Prop\.env`, then `Environment.SetEnvironmentVariable`. API then `builder.Configuration.AddEnvironmentVariables()`.

### 5.3 DI binds the env key onto runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** hard-false pin. `CTraderFixLogonHostedService` logs `_runtime.RealCopyEnabled` (L70) and never assigns it false.

### 5.4 API surfaces the bound value

`apps/api/Program.cs` L55 `realCopyEnabled = runtime.RealCopyEnabled` and L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled`. `launchSettings.json` does not override the key. `appsettings.json` has `FeatureFlags.LiveCopyEnabled=false` (different name; unused by this hop).

`SettingsController` (`LiveCopyEnabled` default false) is **not** the live `/api/settings` Minimal API.

### 5.5 What is still false (does not save claim 5)

| Surface | State | Why it is not claim 5 |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` | Unbound POCO. Live runtime uses DI env bind. |
| `fix-worker/Worker.cs` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Different nested key. Log-only. Stamps `Disconnected`. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Blocks send. Does not pin the flag. |
| Persist `AllowFixSend` | literal `false` (`CopyTradingService` L306) | Overrides engine; not a flag pin. |
| `VenueReconciled` | `const false` | Extra send gate. |
| Architecture / README / CREDENTIALS “forced false” | docs | **Stale** vs live DI. |

Copy hop remains `SAFE_BY_ABSENCE` (`35=A` only + NOS unimplemented + persist `AllowFixSend=false` + `0` `ExecutionIntent` writers). That is **capital safety**, not “flag stays false.”

**Claim 5 fail.** W500_68 / W500_108 / A014 / A015 / CREDENTIALS “forced false” / A006 “`/api/settings` hardcoded false” are **stale**.

---

## Risk to capital

**NONE** today (`SAFE_BY_ABSENCE` on the copy hop).

- Product `CTraderFixSession` cannot emit `35=D`
- Copy service const `NewOrderSingleImplemented=false`
- Persist `AllowFixSend:=false` even if `RiskEngine` would compute true
- `VenueReconciled=false`; send-if also requires `LIVE` + NOS + reconciled
- This slot sent **no** order and did **not** attach Manager

Residual: next sender wired against `_runtime.RealCopyEnabled` would see the flag **armed** on the API host. Demo CLI can send `35=D` on **demo** identity only; not the copy hop.

---

## Stale documents this re-read contradicts

- `reports/CREDENTIALS_AND_COPY_STATUS.md` — `REAL_COPY_EXECUTION_ENABLED` **false (forced)**
- W500_RESEARCH_68 / 108 — DI/hosted pin false
- A014 — DI pins false
- A015 — logon sets `_runtime.RealCopyEnabled = false`
- A006 — `/api/settings` hardcodes false
- A002 — API still calls `DemoSeeder`

---

## Verdict

**FAIL.** Claims 1–4 file-proven (2–3 capability only; not re-attached). Claim 5 disproved: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + DI L41 bind + no hosted re-pin. Copy hop still cannot send. Risk to capital **NONE**.
