# W500_VERIFY_67 — Adversarial live-path verify (slot 67)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_67.md` |
| Agent / slot | W500 adversarial **verify 67** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A002 / A014 / A015 / CREDENTIALS reports. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (gate + persist + blockers), `DealIngestionService.cs` (catalog + ingest), `LiveIngestHostedService.cs` (140/140), `CopyTradingHostedService.cs` (43/43), `LiveMt5Registration.cs` (94/94), `EnvFile.cs` (41/41), `BrokerCatalogSeed.cs` (112/112), `DemoSeeder.cs` header, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/Controllers/SettingsController.cs`, `apps/api/appsettings.json`. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `RealCopyEnabled =`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (source capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Not re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (source capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` = **0**. `WriteAsync` = 1. |
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

API is minimal-hosting (`MapGet`/`MapPost` only). There is **no** `AddControllers()`. `apps/api/Controllers/SettingsController.cs` exists on disk but is **not** the live `/api/settings` hop (`Program.cs` L71–84).

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker/Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only.

DI (`DependencyInjection.cs` L36–58) fail-closes without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave; Starwave `ProxyEnabled=false`), plus `LiveIngestHostedService`. No `FakeMt5` / `DemoSeeder` tokens in `DependencyInjection.cs`.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder` | Tests + leftover file. **Not** called from `apps/`. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` | L31–35 | Dummy **scorer** leftover. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Not DemoSeeder. |

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows + FIX rows at `Disconnected` (L77–107). It does **not** ingest FakeMt5 tape or score 10001.

**Claim 1 proven.** A014 “DemoSeeder gone from API startup” is **still true**. A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

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

Live ingest walks this list:

```45:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetAccountsAsync(null)` re-enters `GetGroupsCore` (`GetAccountsCore` L201–202).

Vendor surface exists: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` `GroupTotal` / `GroupRequestArray`. This slot treats the C# call sites as the product proof.

### 2.1 What this file proves

- Primary enumerator is the request API `GroupRequestArray("*")` — the official ALL-groups mask.
- If that walk yields **zero** rows, cache walk `GroupTotal` + `GroupNext` runs.
- Fetch is **flag-blind** (`_pumpEnabled` is recorded at Connect but **never** gates `GetGroupsCore`).

### 2.2 What this file does **not** prove (residuals, not claim-2 FAIL)

| Residual | Why |
|---|---|
| No live attach this slot | Cannot re-measure 8+10=18 groups. Prior 08:42Z census is **not** this slot’s evidence. |
| Partial non-empty request | If `GroupRequestArray` returns count > 0 but incomplete, `GroupTotal` fallback is **skipped**. |
| Manager ACL | `*` is “all groups this manager may see,” not “every group on the server.” |

A001 “zero `GroupRequestArray` under `src`” is **stale**.

**Claim 2 proven as source capability.** Runtime “ALL on Achiever+Starwave today” is **unproven this slot**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

`ReadAccountsForGroup`:

```223:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`GetAccountsCore` with `group == null` walks **every** group from `GetGroupsCore`, then dedupes by login (L189–213). Live catalog uses `GetAccountsAsync(null)` (`DealIngestionService` L48 / L62).

Vendor surface: `MT5APIManager.h` `UserLogins` / `UserRequestArray`. Product proof is the C# walk above.

### 3.1 What this file proves

- Primary trader enumerator is `UserRequestArray` (network request).
- Empty array → `UserLogins` + `UserRequestByLogins`.
- `UserGetByGroup` is **only** the hard-fail cache fallback (not the ALL path).
- Hosted scoring is `ListLoginsWithDealsAsync` (subset of catalog). Catalog itself is still the all-users walk.

### 3.2 Residuals (not claim-3 FAIL)

| Residual | Why |
|---|---|
| No live attach | Cannot re-measure 6512+1948=8460. |
| Partial non-empty `UserRequestArray` | `UserLogins` skipped when `users.Total() != 0`. |
| Hosted score ≠ catalog | Deals-only scorer. `/api/ops/resync` scores `ListLoginsAsync` (all persisted accounts). Different surfaces. |
| Dummy Worker scorer | `mt5-worker/Worker.cs` L31 still rebuilds 10001–99001. Not the API ingest hop. |

**Claim 3 proven as source capability.** Live ALL-trader census is **unproven this slot**.

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

Outbound builder:

```94:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

- Grep of this file for `35=D` / `"D"` as MsgType = **0**.
- Only outbound MsgType is `(35, "A")` Logon.
- Only `WriteAsync` is L49 (the Logon bytes). Socket disposed via `using`.
- Reply parse accepts inbound `35=A` as success; any other type is “Logon rejected.” No NewOrderSingle method exists on this type.

Hosted caller (`CTraderFixLogonHostedService` L48–58) calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and persists status. It never builds tag 35=`D`. Log line L69 still says “NewOrderSingle still unimplemented.”

### 4.1 Adjacent residual (does **not** put 35=D on `CTraderFixSession`)

Sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. Demo-gated (this slot did not re-read the gate body; not required — the assigned type is `CTraderFixSession`). `grep CTraderFixDemoTestTrade` under product `apps/` and `Infrastructure/` = **0**. Only caller is `tools/DemoFixTestTrade/Program.cs` L44. Not DI. Not copy hop.

**Claim 4 proven** for the assigned type. Product-wide “zero `35=D`” would be **FAIL** because of the demo helper; that is **not** the assigned claim.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. It does not.

### 5.1 Lab env is true

Flag-only grep of `D:\Prop\.env` (no other keys printed):

| Line | Key | Value |
|---|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED` | `true` |
| 106 | `FEATURE_COPY_TRADING_ENABLED` | `true` |

### 5.2 API loads that env, then DI binds it

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) walks cwd / parents then hard path `D:\Prop\.env` and `SetEnvironmentVariable` for every `KEY=value` line.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`grep RealCopyEnabled =` under product `*.cs` = **exactly one assignment**: DI L41. There is no later force-false.

### 5.3 Hosted logon does **not** re-pin

`CTraderFixLogonHostedService.ExecuteAsync` reads FIX password/host/account and calls `TryLogonAsync`. It logs `_runtime.RealCopyEnabled` (L70) as `RealCopyArmed`. It never writes `RealCopyEnabled = false`.

### 5.4 Settings API echoes the runtime bit

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

`FEATURE_COPY_TRADING_ENABLED` is a **literal true** in the API (env L106 unused by this hop). `REAL_COPY_EXECUTION_ENABLED` is the bound runtime flag. After a lab start with `.env` L73 `true`, this JSON is **true**.

Dead leftover: `SettingsController` `FeatureFlags.LiveCopyEnabled` defaults **false** (`appsettings.json` L47) but that controller is **not registered**. Do not treat it as the live flag.

### 5.5 What still stays false (does **not** rescue claim 5)

| Surface | State | Why it is not “flag stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` POCO default | `false` (L35) | Unbound by DI. Hosted logon does not read this POCO. |
| `apps/fix-worker/Worker.cs` | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Different key. Logs only; still stamps `Disconnected`. Not the API runtime bit. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` (L17) | Send absence, not flag pin. |
| Persist `AllowFixSend` | **literal `false`** (`CopyTradingService` L305) | Overrides `decision.AllowFixSend` for the row. |
| `VenueReconciled` | `const false` (L16) | Send conjunction still closed. |
| Copy hosted tick | `GenerateShadowIntentsAsync` only | Shadow/intent; no FIX write. |

`LiveRuntimeStatus.Snapshot()` L42–44 even documents the armed state: *“REAL_COPY armed. NewOrderSingle still unimplemented…”*

**Claim 5 disproven.** W500_68 / W500_108 / CREDENTIALS “forced false” are **stale**.

---

## 6. Risk to capital

**NONE** on destination capital from the **copy hop**, by absence of a sender:

1. `CTraderFixSession` outbound is Logon `35=A` only; socket disposed after one read.
2. `CopyTradingService` hard-codes `NewOrderSingleImplemented = false`, `VenueReconciled = false`, persist `AllowFixSend = false`.
3. The only `Build("D")` lives in `CTraderFixDemoTestTrade`, called only from `tools/DemoFixTestTrade` (not API/workers/DI).
4. This slot sent **zero** orders and did not attach.

**Not** “safe because the flag is false.” The flag is **armed** on the API host. The next person who implements `35=D` against `LiveRuntimeStatus.RealCopyEnabled` will see **true**.

---

## 7. What this slot did not do

- No Manager Connect / `GroupRequestArray` live call.
- No cTrader TLS / Logon.
- No `/api/settings` HTTP GET (file proof only).
- No census re-sum of 18/8460.
- No product edit.

---

## 8. One-line pin

Slot 67 **FAIL**: DemoSeeder off API boot; Native can walk groups (`GroupRequestArray("*")` / `GroupTotal`) and traders (`UserRequestArray` / `UserLogins`) in source; `CTraderFixSession` is `35=A` only; **`REAL_COPY_EXECUTION_ENABLED` does not stay false** (`.env` L73 `true` + DI L41, no re-pin). Dest risk **NONE** (`SAFE_BY_ABSENCE`).
