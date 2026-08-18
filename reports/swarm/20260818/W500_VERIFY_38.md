# W500_VERIFY_38 — Adversarial live-path verify (slot 38)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_38.md` |
| Agent / slot | W500 adversarial **verify 38** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / INDEX / CREDENTIALS / A014 prose. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. Proxy host/port in `BrokerCatalogSeed` already committed as catalog rows; not re-printed as secrets. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (320/320), `DealIngestionService.cs` (146/146), `LiveIngestHostedService.cs` (141/141), `CopyTradingHostedService.cs` (40/40), `LiveMt5Registration.cs` (94/94), `EnvFile.cs` (41/41), `BrokerCatalogSeed.cs` (112/112), `DemoSeeder.cs` (140/140), both `Worker.cs`, `SettingsController.cs`, `apps/api/appsettings.json`, `launchSettings.json`. Targeted `grep`: `DemoSeeder` (apps + repo), `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`(35, "D")`/`NewOrderSingle`, `RealCopyEnabled =`, `FakeMt5`/`DemoBrokerFactory`, `Take(`/`Skip(` in the native connector, `CTraderFixSession` callers. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false on the API host.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `grep DemoSeeder` under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. No `Take`/`Skip` in this file. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest `GetAccountsAsync(null)` walks every group. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

This slot did **not** treat sibling VERIFY_27 / VERIFY_30 as evidence. The same four/one split reappeared because the files still say that.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A006 “`/api/settings` hardcodes false”.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 Live API host

`D:\Prop\apps\api\Program.cs` (160 physical lines, full read this slot):

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

`grep DemoSeeder` under `D:\Prop\apps` = **0** (this slot).

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker\Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only. Neither worker calls `EnvFile.FindAndLoad` (residual for claim 5, not claim 1).

DI (`DependencyInjection.cs` L36–58) **fail-closes** without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave), plus `LiveIngestHostedService` / `CTraderFixLogonHostedService` / `CopyTradingHostedService`. `FakeMt5BrokerConnector` / `DemoBrokerFactory` are **not** constructed on this throw-pass path.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 | Tests + leftover file. **Not** called from `apps/`. |
| `DemoSeeder` uses `DemoBrokerFactory.CreateDefault()` | `DemoSeeder.cs` L126 | Only if someone calls `SeedAsync`. API does not. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| Report `_tmp_*` copies still call it | `reports/swarm/20260818/_tmp_*` | Not product. |
| `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` | L31–35 | Dummy **scorer** leftover. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Not DemoSeeder. |

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows + FIX rows at `Disconnected` (L77–107). It does **not** ingest FakeMt5 tape or score 10001.

**Claim 1 proven from the file.** A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (full file 458 lines this slot):

```152:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

What the file proves:

- Primary: network `GroupRequestArray("*")` — mask is all groups the manager API accepts as “all.”
- Fallback when the request list is **empty**: pump-cache `GroupTotal` + `GroupNext`.
- Dedup by name (`HashSet` ordinal-ignore-case) in `AddGroup`.
- `_pumpEnabled` is **not** a gate on this walk. Connect tries GROUPS|USERS|POSITIONS pump, then `PUMP_MODE_NONE` (L89–111). Fetch still runs.
- `grep Take\(|Skip\(` in this file = **0**.

Ingest is flag-blind:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group returned above (`GetAccountsCore` L199–203).

**Not proven this slot (and not required to pass “can list”):** a live Manager attach returning N groups. Prior census 8+10=18 is **cited, not re-measured**. Capability is on disk.

Residual (does not fail “can list”): if `GroupRequestArray("*")` returned a **non-empty partial** set, `GroupTotal` would **not** run (`if (list.Count == 0)`). That is Manager-API completeness, not a missing code path. This slot cannot prove server-side completeness without attach.

**Claim 2 proven from the file.**

---

## 3. All traders via UserRequestArray / UserLogins — PASS

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

Order of operations:

1. **Request** `UserRequestArray(gname)` — users in that group (network).
2. Hard-fail (not OK / OK_NONE / NOTFOUND) → pump-cache `UserGetByGroup` only.
3. If the array is still empty → **`UserLogins`** then `UserRequestByLogins`.

`GetAccountsCore(null)` unions every group from `GetGroupsCore`. Ingest (`SyncCatalogAsync` L48) and `/api/ops/resync` (`Program.cs` L129) both call catalog sync with no group filter.

Residuals (do not break “can list”):

- `UserGetByGroup` is a **cache** fallback, not the ALL-traders primary.
- If `UserRequestArray` returned a **non-empty partial** set, `UserLogins` would **not** run. Same Manager-API completeness caveat as claim 2. Not re-attached.
- Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Unscored logins remain in catalog. Manual resync scores `ListLoginsAsync` (all catalog logins). That is a scoring scope residual, not a fetch-all hole.
- `mt5-worker/Worker.cs` L31–35 still scores four dummy logins **in addition** to `SyncBrokerAsync` (which already catalogs all). Dummy scores do not cap the fetch.
- This slot did **not** re-attach; prior 6512+1948=8460 is **not** re-proven here.

**Claim 3 proven from the file** as a connector capability + ingest call site.

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` — **135/135** lines read this slot.

Only outbound constructor is `BuildLogon`:

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

Wire path: one `WriteAsync` of that Logon (L47–50), one `ReadAsync` (L52–53), then `using` disposes `TcpClient` / `SslStream`. No loop. No heartbeat. No NewOrderSingle. No OrderQty / ClOrdID.

`grep` of **this file** for `35=D`, `(35, "D")`, `Build("D")`, `NewOrderSingle` = **0**.

Hosted caller (`CTraderFixLogonHostedService` L48–58) only invokes `TryLogonAsync` for QUOTE 5211 and TRADE 5212. Password missing/`<SECRET>` → logon skipped (L33–38).

### 4.1 Residual sibling (does **not** fail claim 4)

`CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. `CTraderFixDemoMatrix` also `Build("D")` at L93. They are:

- **not** `CTraderFixSession`;
- called only from `tools/DemoFixTestTrade/Program.cs` L38 / L44;
- **not** registered in API / DI / copy / workers (`grep CTraderFixDemo` under product callers = tools only).

Literal token `35=D` is **0** in `CTraderFixSession.cs`. The demo helper uses `Build("D")`, not the string `35=D`.

**Claim 4 proven** for the assigned type. Product-wide “zero 35=D builders” is **false** if the sibling is in scope; this claim named `CTraderFixSession` only.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove the flag stays false. The live files prove the opposite on the API host.

### 5.1 Process bind (this is the SUT)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`LiveRuntimeStatus.RealCopyEnabled` default is `false` **until** this assignment. Repo-wide `grep RealCopyEnabled\s*=` under `*.cs` = **this one write only**. There is no later `= false`.

`CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed={Armed}` and does **not** assign `_runtime.RealCopyEnabled = false`. The old pin is gone.

### 5.2 API loads lab `.env` into the process, then configuration

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
...
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L8–19) always includes `D:\Prop\.env` as a candidate and, if found, `Load` does `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line (L23–39).

Flag-only grep of `D:\Prop\.env` (no other keys, no values of secrets):

- L73: `REAL_COPY_EXECUTION_ENABLED=true`
- L106: `FEATURE_COPY_TRADING_ENABLED=true`

Therefore, when the API starts on this machine with that `.env` present, `configuration["REAL_COPY_EXECUTION_ENABLED"]` is `"true"` and `runtime.RealCopyEnabled` becomes **true**.

`launchSettings.json` sets only `ASPNETCORE_ENVIRONMENT=Development`. It does **not** override the flag to false.

`/api/settings` is **not** a hardcoded false:

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

`/api/health` L55 also exposes `realCopyEnabled = runtime.RealCopyEnabled`.

`CopyTradingService.GetStatusAsync` L44 exposes `RealCopyArmed: _runtime.RealCopyEnabled`. `BuildBlockers` L316 only adds `"REAL_COPY_EXECUTION_ENABLED is false"` when the runtime flag is already false — so an armed API will **not** list that blocker.

`RiskEngine.Evaluate` is called with `RealExecutionEnabled = _runtime.RealCopyEnabled` (CopyTradingService L190). Persist then **overwrites** `AllowFixSend = false` (L211). That is a persist pin, not a flag pin.

### 5.3 What still is false (not the assigned claim)

| Surface | Value | Why it does not save claim 5 |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | **Unbound.** No `Configure<CTraderFixOptions>`. Not `LiveRuntimeStatus`. |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **Different name.** Unused by DI L41. |
| `SettingsController` `LiveCopyEnabled` | default false | **Dead leftover.** `Program.cs` never calls `AddControllers()`. Live route is the minimal-API `MapGet`. Different name. |
| `launchSettings.json` | no `REAL_COPY_*` | Does not override `.env`. |
| fix-worker `GetValue("CTrader:RealCopyExecutionEnabled", false)` | default **false** | Nested key; **not** the env token. Log-only. Worker stamps sessions `Disconnected`. |
| Workers do not call `EnvFile.FindAndLoad` | process env only | API **does** load `.env`. Claim is not “workers stay false.” |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Blocks send; **does not** pin the flag. |
| Persist `AllowFixSend` | literal `false` (CopyTradingService L211) | Blocks persist-send; flag can still be true. |
| `VenueReconciled` | `const false` | Extra send gate; not the flag. |
| Architecture §41 / README | document `false` | Policy, not process. |
| `CREDENTIALS_AND_COPY_STATUS.md` | “false (forced)” | **STALE vs DI L41 + .env L73.** |

Copy hop still cannot emit a ticket (`SAFE_BY_ABSENCE`). That is **orthogonal**. Assigned wording is “`REAL_COPY_EXECUTION` stays false.” On the live API composition it **does not**.

**Claim 5 FAIL.** Operator leftover: lab `.env` L73 is already `true` and DI honors it. This slot did **not** flip the file.

---

## 6. Cross-claim diagram (live path only)

```
API Program
  EnvFile.FindAndLoad()           // D:\Prop\.env → process env
  AddTraderIntelligence
    HasRealPasswords dual-AND     // else throw; no Fake
    RealCopyEnabled := env==true  // CLAIM 5 FAIL if .env true
    NativeMt5 ×2
    LiveIngest / FixLogon / Copy hosted
  EnsureCreated
  BrokerCatalogSeed               // CLAIM 1 PASS (not DemoSeeder)
  app.Run

Native GetGroupsCore
  GroupRequestArray("*")          // CLAIM 2
  else GroupTotal + GroupNext

Native ReadAccountsForGroup
  UserRequestArray                // CLAIM 3
  else UserLogins + UserRequestByLogins

CTraderFixSession.TryLogonAsync
  BuildLogon (35,"A") only        // CLAIM 4
  1 WriteAsync, dispose

CopyTradingService
  NewOrderSingleImplemented=false
  AllowFixSend persist false
  SHADOW intents only
```

---

## 7. What this slot did **not** do

- Did not live-attach Achiever / Starwave. Did not re-sum 18/8460/1984.
- Did not GET `:5000/api/settings` (would be the runtime proof of claim 5 arming; source bind is already enough to FAIL).
- Did not edit product, `.env`, or tests.
- Did not send or construct a live `35=D` on the copy hop.
- Did not invoke `tools/DemoFixTestTrade`.

---

## 8. Residuals / stale docs

1. **Claim 5 FAIL** is the only assigned miss. Next sender would see `RealCopyEnabled=true` on the API host.
2. `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` can still `Build("D")` off-hop; unused by copy.
3. `mt5-worker/Worker.cs` still scores four dummy logins; hosted ingest does not.
4. `SettingsController` (`FeatureFlags:LiveCopyEnabled`) is a leftover surface; live settings route is the minimal-API `MapGet`.
5. `CREDENTIALS_AND_COPY_STATUS.md` “false (forced)” is **false today**.
6. Hosted scoring = deals-only; catalog can hold more logins than scores.
7. `GroupTotal` / `UserLogins` are empty-list fallbacks, not dual-always walks.

---

## 9. One-line

```text
W500_VERIFY_38 FAIL. 1–4 PASS from files: API seeds BrokerCatalogSeed not DemoSeeder; Native GroupRequestArray("*")/GroupTotal; UserRequestArray/UserLogins; CTraderFixSession 35=A only. Claim 5 FAIL: DI binds .env REAL_COPY_EXECUTION_ENABLED=true; logon does not re-pin. Copy hop still SAFE_BY_ABSENCE (no NOS). Secrets not printed. Source not edited.
```

Do **not** treat this FAIL as a license to send. Do **not** add `35=D` to `CTraderFixSession`. Do **not** wire the demo helper into copy. Operator should set lab `.env` L73 back to `false` (this slot did not).
