# W500_VERIFY_83 — Adversarial live-path verify (slot 83)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **83** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** (capability from source only) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes are **not** evidence. Claim 5 is **disproved** from `.env` + DI + logon host.

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

**AND of five = FAIL.** Claims 1–4 hold from files this slot. Claim 5 cannot be proved: lab `.env` L73 is `true`, API `EnvFile.FindAndLoad()` loads it, DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and `CTraderFixLogonHostedService` does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (different key) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\api\appsettings.json` | committed flags; no `REAL_COPY` key |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no `REAL_COPY` env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC leftover |
| `D:\Prop\apps\fix-worker\appsettings.json` | no RealCopy pin |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` (not this type) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented; persist `AllowFixSend=false` |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |

Product source was **not** modified. This slot did **not** live-attach Manager or send FIX.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines). Token `DemoSeeder` is **absent**. Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Same pattern on both workers (`D:\Prop\apps\fix-worker\Program.cs` L15, `D:\Prop\apps\mt5-worker\Program.cs` L15). `apps\` has **0** `DemoSeeder` hits. `tools\` has **0**.

Class still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder`). The only caller found is `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25. That is test-only.

DI fail-closed: `DependencyInjection.cs` L36–37 throws `Real MT5 passwords are required. Dummy/fake broker data is disabled.` then `LiveMt5Registration.CreateConnectors` returns **Native ×2** only (Achiever + Starwave). No `FakeMt5BrokerConnector` on the host path.

Residual (does not make DemoSeeder the API startup path): `apps\mt5-worker\Worker.cs` L31 still scores hardcoded `{10001,10002,10003,99001}` after a real `SyncBrokerAsync` of both brokers.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (source)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–187.

Primary walk:

```152:167:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Fallback when the request array is empty:

```169:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Mask is `"*"`. `AddGroup` dedupes by name. Ingest uses this path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`.

**Not proved this slot:** a live Manager census. Capability is file-proven. Not re-attached.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source)

Read: same connector, `GetAccountsCore` + `ReadAccountsForGroup`.

`GetAccountsAsync(null)` (ingest L48) walks **every** group from claim 2, then per group:

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

Primary = `UserRequestArray`. Empty → `UserLogins` + `UserRequestByLogins`. Cache `UserGetByGroup` only on hard fail of the request API. Results keyed by login (union across groups).

**Not proved this slot:** live trader counts. Capability is file-proven. Not re-attached.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

- No token `35=D`, no `NewOrderSingle`, no `Build("D")`.
- Only outbound MsgType is Logon:

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

Single wire write: `ssl.WriteAsync` L49. One-shot connect → logon → read reply → dispose. Hosted service calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) then returns; no market-data or order loop.

**Residual (not this type, not on the copy hop):** sibling `CTraderFixDemoTestTrade.cs` `Build("D")` at L139/L163/L197 is demo-gated (`demo-` host / `demo.` sender / refuse account `1369850`). `CTraderFixDemoMatrix.cs` L93 also `Build("D")`. Neither is referenced from `apps\` or `Infrastructure\`. Claim is `CTraderFixSession` only.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproved)

The assigned claim is that the flag **stays false**. Live files show the opposite on the API host.

1. Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secrets). L106: `FEATURE_COPY_TRADING_ENABLED=true`.
2. API boot loads that file then environment:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) walks cwd/`..`/`D:\Prop\.env` and `SetEnvironmentVariable`s every `KEY=value`.

3. DI **binds** the env string onto the singleton runtime (no hard-false pin):

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

4. `CTraderFixLogonHostedService` **does not** assign `_runtime.RealCopyEnabled = false`. It only **logs** the already-bound value (`RealCopyArmed={Armed}` L69–70).
5. `/api/settings` echoes the runtime boolean, not a hardcoded false:

```71:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

6. `launchSettings.json` does not override the flag. Committed `apps\api\appsettings.json` has `FeatureFlags.LiveCopyEnabled=false` (different name) and **no** `REAL_COPY_EXECUTION_ENABLED` key. POCO `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) but is **not** the DI runtime bind.

Therefore the claim “stays false” is **false** for the hosted API. Architecture/POCO default remaining false does not satisfy the claim.

Unused leftover: `SettingsController` still reports `LiveCopyEnabled` from `FeatureFlags:LiveCopyEnabled` (default false) but API `Program.cs` never calls `AddControllers`; the live route is the minimal API above.

Fix-worker residual: `Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (default false) — a **different** key than `.env` `REAL_COPY_EXECUTION_ENABLED`. That does not re-pin the API runtime.

---

## Copy hop still cannot send (does not rescue claim 5)

Claim 5 is about the **flag**, not about whether a ticket can be sent. The send path is still paper-only:

| Gate | File | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L18 | `const false` |
| `VenueReconciled` | L17 | `const false` |
| Persist `AllowFixSend` | L306 | **hardcoded `false`** (ignores `decision.AllowFixSend`) |
| Live-send branch | L312 | requires `AllowFixSend && LIVE && NOS && VenueReconciled` — unreachable |
| Else | L318 | `SHADOW_ONLY` |
| Product hop MsgType | `CTraderFixSession` | `35=A` only |

`SAFE_BY_ABSENCE` on destination capital. Next hypothetical sender would see **runtime armed**.

---

## Verdict

**FAIL.** Claims 1–4 file-proven (2–3 capability only; this slot did not attach). Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Copy hop `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
