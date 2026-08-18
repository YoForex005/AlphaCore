# W500_VERIFY_41 — Adversarial live-path verify (slot 41)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_41.md` |
| Agent / slot | W500 adversarial **verify 41** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A014 / CREDENTIALS / INDEX prose. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager `Connect`. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs`, `apps/mt5-worker/Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `EnvFile.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` header, `RiskEngine.cs` allow-send, `CopyTradingHostedService.cs`, both `Worker.cs`, `SettingsController.cs`, `appsettings.json`, `launchSettings.json`, `CTraderFixDemoTestTrade.cs` residual, `tools/DemoFixTestTrade/Program.cs`. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `RealCopyEnabled =`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `(35, "D")` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A006 “`/api/settings` hardcodes false”.

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

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**.
- Product `*.cs` callers of `DemoSeeder` = `src/Infrastructure/Seeding/DemoSeeder.cs` (definition) + `tests/Integration/SeedingAndStoreTests.cs` L25 + `_tmp_*` report harnesses. **Not API.**

API TFM is `net8.0-windows` (`TraderIntelligence.Api.csproj` L18). No `AddControllers` / `MapControllers` — `SettingsController` is **unmapped**; live settings are the minimal-API `MapGet("/api/settings", ...)`.

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker/Program.cs` L15 and `D:\Prop\apps\mt5-worker/Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only.

DI (`DependencyInjection.cs` L36–49) fail-closes without real MT5 passwords, then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + StarwaveFX). `FakeMt5BrokerConnector` is **not** referenced from `Infrastructure/` (0 hits). No Fake on the throw-pass path.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 | Tests + leftover file. **Not** called from `apps/`. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| Report `_tmp_*` copies still call it | `reports/swarm/20260818/_tmp_*` | Not product. |
| `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` | L31–35 | Dummy **scorer** leftover. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Not DemoSeeder. |
| `FakeMt5BrokerConnector` still exists | `src/Mt5/Connectors/FakeMt5BrokerConnector.cs` | Unused by DI. |

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows + FIX rows at `Disconnected` (L77–107). It does **not** ingest FakeMt5 tape or score 10001.

**Claim 1 proven.** A014 “DemoSeeder gone from API startup” is **still true**. A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (full file 458 lines):

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

Capability from file:

1. Request-first: `GroupRequestArray("*")` — Manager all-groups mask.
2. Fallback **only if** `list.Count == 0`: `GroupTotal` + `GroupNext`.

Live ingest calls this: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null)`. Flag-blind. No `Take`/`Skip` on groups.

**Not proven this slot (and not required to FAIL the capability claim):** a live Manager attach returning a measured group count. This slot did not connect. Prior census 18/8460 is **cited, not re-measured**.

**Residual (does not kill the claim):** if `GroupRequestArray("*")` returns `MT_RET_OK` with a **partial** array, the `GroupTotal` fallback is skipped. That is a completeness hole, not absence of the two APIs.

**Claim 2 proven as source capability.**

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

`GetAccountsCore` (L189–213): if `group` is null/empty, walk **every** group from `GetGroupsCore()`, then union by login.

`ReadAccountsForGroup` (L216–271):

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

Order: `UserRequestArray` → (on unexpected retcode) `UserGetByGroup` → (if still empty) `UserLogins` + `UserRequestByLogins`.

Catalog path: `GetAccountsAsync(null)` = all groups × that walk. No login `Take`.

**Not proven this slot:** live “8460 traders” completeness. Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106) — deals-only, not a second trader census. Manual `/api/ops/resync` uses `ListLoginsAsync` (all catalog logins). Dummy Worker scorer `{10001…}` is leftover and is **not** the Native list path.

**Residual:** if `UserRequestArray` returns OK with a **partial** user array (`Total() > 0`), `UserLogins` is skipped.

**Claim 3 proven as source capability.**

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` — **135/135** lines, full read.

Outbound builder:

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
```

Facts from this file:

- Only `WriteAsync` is L49 (the Logon bytes).
- Only outbound `MsgType` is `(35, "A")`.
- Inbound parse reads tag `35` and treats `"A"` as success; any other type is “Logon rejected”.
- Zero tokens: `35=D`, `(35, "D")`, `NewOrderSingle`, `OrderQty`, `Build("D")`.
- Sockets disposed via `using` / `await using`.

Hosted caller `CTraderFixLogonHostedService` L48–58: two `TryLogonAsync` (QUOTE 5211, TRADE 5212). No other send.

### Residual (not the assigned type)

Sibling **not** `CTraderFixSession`:

| File | What | Wired to copy/API? |
|---|---|---|
| `CTraderFixDemoTestTrade.cs` | `Build("D")` ×3 (L139 flatten, L163 open, L197 close) | **No.** Only `tools/DemoFixTestTrade/Program.cs`. Demo-gated (refuse `live-*` / `live.` / account `1369850`). |
| `CTraderFixDemoMatrix.cs` | `Build("D")` L93 | Same tools CLI `--matrix`. |

W500 slots that said “product `35=D=0` everywhere” are **stale** for the tree; they remain **true for `CTraderFixSession`**. Claim 4 is the assigned type.

**Claim 4 proven.**

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show the opposite on the API host.

### 5.1 Lab env is armed

Flag-only grep of `D:\Prop\.env` (no other keys printed):

```
73:REAL_COPY_EXECUTION_ENABLED=true
106:FEATURE_COPY_TRADING_ENABLED=true
```

### 5.2 API loads that file into process env, then into IConfiguration

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
…
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`EnvFile.cs` L14) includes the hard path `D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=value` line.

Workers **do not** call `EnvFile.FindAndLoad` (only API + `tools/LiveBrokerProbe`). That does **not** save claim 5: the API composition root **does**.

### 5.3 DI binds the env token onto runtime (no pin)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep `RealCopyEnabled =` in product `*.cs` = **this one assignment only**. There is **no** later `RealCopyEnabled = false`.

`CTraderFixLogonHostedService` L68–70 **logs** `_runtime.RealCopyEnabled` as `RealCopyArmed`. It does **not** overwrite it.

`launchSettings.json` does not set `REAL_COPY_EXECUTION_ENABLED`. It cannot save the claim: `.env` + `AddEnvironmentVariables` still win.

### 5.4 Surfaces that will show true on a loaded API

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

`CopyTradingService.GetStatusAsync` L44: `RealCopyArmed: _runtime.RealCopyEnabled`. Blocker `"REAL_COPY_EXECUTION_ENABLED is false"` (L316–317) is **added only when the runtime flag is false** — so an API process that loaded `.env` L73 **will not** list that blocker.

### 5.5 What still defaults false (does not rescue claim 5)

| Surface | Value | Why it is not “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | **Unused.** DI never binds this POCO. |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | `false` | **Different name.** Not `REAL_COPY_EXECUTION_ENABLED`. |
| `SettingsController.FeatureFlags.LiveCopyEnabled` | config default false | Controller is **unmapped** (no `MapControllers`). |
| `fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Nested key, **log-only**. Worker still stamps `Disconnected`. Different token. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Send absence. Not the flag. |
| Persist `AllowFixSend` | hardcoded `false` (L211) | Send absence. Not the flag. |
| `RiskEngine` `allowSend` | needs `RealExecutionEnabled && Reconciled && VenueHealthy` | Copy service then **overrides** persist to false anyway. |

Architecture docs / README still write `REAL_COPY_EXECUTION_ENABLED=false`. Docs are not the running bind.

**Claim 5 disproven.** If a later agent implements a sender that reads `LiveRuntimeStatus.RealCopyEnabled`, the API host is already armed.

---

## 6. Copy hop (risk context — not claim 5)

`CopyTradingService`:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

```217:223:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

`AllowFixSend` on the persisted `RiskDecisionRecord` is **literal false** (L211). Hosted copy (`CopyTradingHostedService`) only calls `GenerateShadowIntentsAsync`.

Therefore dest capital risk **today** is **NONE** by **absence of a sender**, not by the flag staying false.

---

## 7. What this slot did not do

- No Manager attach / no proxy connect / no live group/trader recount.
- No TLS to cTrader / no Logon attempt.
- No `GET :5000/api/settings` (localhost SSRF would be blocked here; not needed — bind is in source).
- No product edit. No `.env` edit. No secret dump.

---

## 8. Slot 41 scorecard

| Claim | Verdict |
|---|---|
| 1 DemoSeeder ≠ API startup | **PASS** |
| 2 Groups via `GroupRequestArray` / `GroupTotal` | **PASS_SOURCE** |
| 3 Traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** |
| 4 `CTraderFixSession` has no `35=D` | **PASS** |
| 5 `REAL_COPY_EXECUTION` stays false | **FAIL** |
| **Overall** | **FAIL** |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE` on copy hop; flag **armed** on API) |
