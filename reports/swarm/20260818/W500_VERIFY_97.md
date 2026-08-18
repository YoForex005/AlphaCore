# W500_VERIFY_97 — Adversarial live-path verify (slot 97)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_97.md` |
| Agent / slot | W500 adversarial **verify 97** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A002 / A014 / A015 / CREDENTIALS reports. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (gate + persist + blockers), `DealIngestionService.cs` (catalog + ingest), `LiveIngestHostedService.cs` (140/140), `CopyTradingHostedService.cs` (43/43), `LiveMt5Registration.cs` (94/94), `EnvFile.cs` (41/41), `BrokerCatalogSeed.cs` (112/112), `DemoSeeder.cs` header, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/Controllers/SettingsController.cs`, `apps/api/appsettings.json`, `apps/api/TraderIntelligence.Api.csproj`, `RiskEngine.cs` allow-send. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `RealCopyEnabled =`, `AddControllers`/`MapControllers`, `FindAndLoad`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (source capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Not re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (source capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `"D"` = **0**. `WriteAsync` = 1 (logon only). |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. Only `RealCopyEnabled =` write in product C# is DI L41. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file; claim 5 is affirmatively false).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A015 “logon sets `_runtime.RealCopyEnabled = false`”; A006 “`/api/settings` hardcodes false”; A002 “API still calls `DemoSeeder`”; SettingsController is **not** the live `/api/settings` hop.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 Live API host

`D:\Prop\apps\api\Program.cs` (160 physical lines, full read):

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

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0**.

API is minimal-hosting (`MapGet`/`MapPost` only). There is **no** `AddControllers()` and **no** `MapControllers()` (repo-wide grep under `apps/api` = 0). `apps/api/Controllers/SettingsController.cs` exists on disk but is **not** the live `/api/settings` hop (`Program.cs` L71–84). `TraderIntelligence.Api.csproj` is `Microsoft.NET.Sdk.Web` with no extra controller wiring.

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker\Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only. Neither worker calls `EnvFile.FindAndLoad()` (only API + `tools/LiveBrokerProbe`).

DI (`DependencyInjection.cs` L36–58) fail-closes without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave; Starwave `ProxyEnabled=false`), plus `LiveIngestHostedService`. No `FakeMt5` / `DemoSeeder` tokens in `DependencyInjection.cs`.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder` | Tests + leftover file. **Not** called from `apps/`. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| Swarm `_tmp_*` programs | `reports/swarm/20260818/_tmp_*` | Offline eval, not API boot. |
| `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` | L31–35 | Dummy **scorer** leftover. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Not DemoSeeder. |
| `FakeMt5BrokerConnector.CreateDefault()` | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | Class exists. DI does **not** register it. |

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows + FIX rows at `Disconnected` (L77–107). It does **not** ingest FakeMt5 tape or score 10001.

**Claim 1 proven.** A014 “DemoSeeder gone from API startup” is **still true**. A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (source)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (full file 458 lines):

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

Primary path is Manager `GroupRequestArray("*")`. Fallback is `GroupTotal` + `GroupNext` **only when the first path produced zero rows**.

**Adversarial caveat (does not flip this claim):** if `GroupRequestArray("*")` returns `MT_RET_OK` with a **partial** non-empty array, `GroupTotal` is **not** consulted. This slot did not live-attach, so completeness of a live Manager dump is **unproven**. The assigned claim is that the connector **can list via those APIs**; that source path is present.

Live catalog hop: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` → `GetGroupsCore`. Hosted by `LiveIngestHostedService` L56. Flag-blind.

**Claim 2 proven as source capability. Not a live census.**

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source)

`ReadAccountsForGroup` (same file):

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

`GetAccountsAsync(null)` (L189–213) walks **every** group from `GetGroupsCore()` then unions by login. Catalog/ingest both call `GetAccountsAsync(null, ct)` (`DealIngestionService` L48, L62).

**Adversarial caveat (does not flip this claim):** `UserLogins` runs only when `users.Total() == 0`. A partial `UserRequestArray` would skip the login-list fallback. This slot did not live-attach.

**Claim 3 proven as source capability. Not a live 8460-login re-count.**

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

Outbound body is built only in `BuildLogon`:

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

`ssl.WriteAsync` occurs **once** (L49) with that logon. Reply parse accepts inbound `35=A` as LoggedOn (L56). No NewOrderSingle, no `35=D`, no `"D"` token in this file. File-scoped grep of tag 35 = **one** hit: `(35, "A")`.

Hosted caller `CTraderFixLogonHostedService` (112/112) calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) then persists status. It does **not** send any other MsgType. After logon the `TcpClient`/`SslStream` are disposed (`using` in `TryLogonAsync`) — one-shot, no heartbeat, no order.

### 4.1 Residual 35=D (not CTraderFixSession)

| Residual | Path | Live hop? |
|---|---|---|
| `Build("D", …)` | `CTraderFixDemoTestTrade.cs` L139 / L163 / L197 | **No.** Tools `DemoFixTestTrade` only. |
| `Build("D", …)` | `CTraderFixDemoMatrix.cs` L93 | **No.** Same tools host. |
| `NewOrderSingleImplemented = false` | `CopyTradingService.cs` L18 | Product const. Persist `AllowFixSend = false` (L306). Send branch L312 is dead. |

**Claim 4 proven** for the assigned type `CTraderFixSession`. Demo helpers are out of scope and not registered in DI.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. It does not.

### 5.1 Lab env is true

Flag-only grep of `D:\Prop\.env` (values only, no secrets):

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

### 5.2 API loads that file and DI binds it

`EnvFile.FindAndLoad()` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) walks cwd / parents then hard path `D:\Prop\.env`, then `Environment.SetEnvironmentVariable`. API `Program.cs` L10 calls it; L13 `AddEnvironmentVariables()`.

DI is the **only** product C# write of `RealCopyEnabled =`:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`grep RealCopyEnabled =` under `*.cs` = **that one line**.

### 5.3 Hosted logon does not re-pin false

`CTraderFixLogonHostedService` L60–70 writes Quote/Trade logon status and **logs** `_runtime.RealCopyEnabled`. It never assigns `RealCopyEnabled = false`.

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) but is **unused** by the hosted logon path (host reads raw `_config["CTRADER_FIX_*"]` keys).

### 5.4 Settings hop echoes the runtime bool

Live `/api/settings` is `Program.cs` L71–84, **not** `SettingsController`:

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

`appsettings.json` has `FeatureFlags.LiveCopyEnabled: false` — **unread** by this hop. `FEATURE_COPY_TRADING_ENABLED` is a **literal true**.

### 5.5 Worker nuance (does not rescue claim 5)

`apps/fix-worker/Worker.cs` L21 reads `_config.GetValue("CTrader:RealCopyExecutionEnabled", false)` — a **different key**, default false. Workers do **not** call `EnvFile.FindAndLoad()`. That is a stale worker logger, not a force-false pin of `LiveRuntimeStatus` (workers still `AddTraderIntelligence`, so if the process env already has `REAL_COPY_EXECUTION_ENABLED=true`, DI L41 arms the singleton).

### 5.6 Copy hop remains SAFE_BY_ABSENCE (not claim 5)

| Gate | File proof | Blocks dest send? |
|---|---|---|
| `NewOrderSingleImplemented = false` | `CopyTradingService.cs` L18 | Yes |
| `VenueReconciled = false` | L17 | Yes (`RiskEngine` L147–150 `allowSend` needs `Reconciled`) |
| Persist overwrite | L306 `AllowFixSend = false` | Yes (engine value discarded) |
| Send branch | L312 requires LIVE + implemented + reconciled | Dead |
| Session MsgType | `CTraderFixSession` `(35,"A")` only | No D builder |
| Hosted copy tick | `CopyTradingHostedService` L28–29 roster + shadow intents only | No FIX write |

`SAFE_BY_ABSENCE` means **no dest ticket can be built/sent from this hop**. It does **not** mean the flag stays false. Claim 5 is about the flag. The flag is **armed**.

**Claim 5 FAIL.**

---

## 6. What this slot did **not** do

- No live Manager attach; no group/account census re-sum.
- No TLS Logon; no proof Quote/Trade are currently LoggedOn.
- No order, no flatten, no `35=D`.
- Did not flip `.env`.
- Did not treat sibling W500_VERIFY_* prose as evidence.

---

## 7. Stale claims this re-read kills

| Stale claim | Why dead this slot |
|---|---|
| API still starts via `DemoSeeder` | `Program.cs` L156 is `BrokerCatalogSeed` only |
| DI / hosted logon pins `REAL_COPY` false | DI binds env; logon does not assign the bool |
| `/api/settings` hardcodes REAL_COPY false | Echoes `runtime.RealCopyEnabled` |
| `SettingsController` is the settings API | No `AddControllers` / `MapControllers` |
| `CTraderFixSession` can NewOrderSingle | File is logon-only `35=A` |
| `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” | `.env` L73 `true` + DI bind |

---

## 8. Slot 97 scoreboard

| Claim | Verdict |
|---|---|
| 1 DemoSeeder ≠ API startup | **PASS** |
| 2 Native ALL groups via `GroupRequestArray` / `GroupTotal` | **PASS_SOURCE** |
| 3 ALL traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** |
| 4 `CTraderFixSession` has no `35=D` | **PASS** |
| 5 `REAL_COPY_EXECUTION` stays false | **FAIL** |
| **Overall** | **FAIL** |
| **Risk to capital** | **NONE** (`SAFE_BY_ABSENCE` on send hop; flag **armed**) |
