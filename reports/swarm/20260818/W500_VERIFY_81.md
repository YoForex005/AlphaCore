# W500_VERIFY_81 — Adversarial live-path verify (slot 81)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **81** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes are **not** evidence. This slot re-read the product files listed below. Claim 5 is **disproved**.

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

**AND of five = FAIL.** Claims 1–4 hold from files this slot. Claim 5 cannot be proved: lab `.env` L73 is `true`, API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` loads it, DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and `CTraderFixLogonHostedService` does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` + seed |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\api\appsettings.json` | committed flags (`LiveCopyEnabled=false`) |
| `D:\Prop\apps\api\appsettings.Development.json` | no REAL_COPY key |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no REAL_COPY env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC controller (minimal endpoints used) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind + Native register |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | live ingest uses Native via registry |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `RealCopyExecutionEnabled=false` (unbound) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented + persist `AllowFixSend=false` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | only remaining `DemoSeeder` caller |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |
| `D:\Prop\docker-compose.yml` | no `REAL_COPY` key |

Grep this slot (product C#): `DemoSeeder` = class + test caller only; `GroupRequestArray`/`GroupTotal` = `NativeMt5BrokerConnector`; `UserRequestArray`/`UserLogins` = same; `CTraderFixSession` tag 35 = extract inbound + outbound `(35,"A")` only.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (159 lines).

Startup seed is catalog-only. `using TraderIntelligence.Infrastructure.Seeding` is present, but the only call is `BrokerCatalogSeed.EnsureAsync`. There is **no** `DemoSeeder` token in this file.

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Workers match:

- `D:\Prop\apps\fix-worker\Program.cs` L11–16: `EnsureCreatedAsync` + `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16: same

`DemoSeeder` still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140 lines). Grep of `D:\Prop\apps` = **0** hits. Grep of `D:\Prop\src` = the class definition only. Remaining caller is the test `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 (`await DemoSeeder.SeedAsync(...)`).

DI fail-closed: `AddTraderIntelligence` throws unless both MT5 passwords pass `HasRealPasswords`, then registers `LiveMt5Registration.CreateConnectors` — `NativeMt5BrokerConnector` ×2 only. No Fake on the host path.

`BrokerCatalogSeed` inserts broker/instrument/kill/FIX **session shells** (`Disconnected`, “NewOrderSingle off”). It does **not** invent deals, scores, or logins 10001/10002/10003/99001. Those dummy logins remain only inside `DemoSeeder` (test path).

Claim 1 **proved**.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (source capability)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

Live registration (`LiveMt5Registration.CreateConnectors`) constructs only `NativeMt5BrokerConnector` for ACHIEVER and STARWAVEFX. Hosted ingest (`LiveIngestHostedService` → `DealIngestionService.SyncCatalogAsync`) calls `connector.GetGroupsAsync`.

`GetGroupsCore` (L144–187):

1. Primary: `GroupRequestArray("*", arr)` then walk `arr.Total()` / `arr.Next(i)`.
2. Fallback **only if** `list.Count == 0`: `GroupTotal()` + `GroupNext(i, grp)`.

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

Wildcard `"*"` plus `GroupTotal` walk is the file-level ALL-groups enumerator. This slot did **not** re-attach Manager, so live census is not re-proved here. Claim is **source capability**, not a new attach.

Claim 2 **proved from file**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source capability)

Same connector. `GetAccountsAsync(null)` (L189–213) walks **every** group from `GetGroupsCore()`, then `ReadAccountsForGroup` per name, de-duped by login.

`ReadAccountsForGroup` (L216–271):

1. Primary: `UserRequestArray(gname, users)`.
2. On hard fail (not OK / OK_NONE / NOTFOUND): `UserGetByGroup` (pump-cache fallback).
3. If `users.Total() == 0`: `UserLogins(gname, out loginRes)` then `UserRequestByLogins`.

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

Catalog path: `DealIngestionService.SyncCatalogAsync` L45–49 calls `GetGroupsAsync` then `GetAccountsAsync(null, ct)`. That is the live ALL-traders walk.

Caveats (honesty, not FAIL of the assigned claim):

- This slot did **not** re-attach; completeness vs server is file-intent only.
- `UserLogins` is a **empty-array fallback**, not the first hop.
- Hosted **scoring** is a later deals-only pass (`ListLoginsWithDealsAsync` on the ingest host); listing/catalog is still all accounts from this walk.

Claim 3 **proved from file** as “can enumerate all traders via those APIs.”

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Tag 35 occurrences in this file:

| Line | What |
|---|---|
| 55 | inbound `Extract(reply, "35")` |
| 73 | error string interpolates inbound `35={msgType}` |
| 96 | outbound `(35, "A")` in `BuildLogon` |

Only `WriteAsync` is the logon bytes. Sockets are `using`/`await using` and disposed after one read. No `NewOrderSingle` token. No `"D"` MsgType.

Hosted caller `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and logs `NewOrderSingle still unimplemented`. It never assembles a D.

**Residual (not this type):** `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs` contain `Build("D", ...)`. Those are sibling helpers, not `CTraderFixSession`, and are not on the copy hop. Assigned claim is specifically `CTraderFixSession`.

Claim 4 **proved**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproved)

The assigned claim is that the flag **stays false**. Live files prove it can be, and in this lab **is**, true.

### 5.1 DI binds env exact `"true"`

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is no hard pin `RealCopyEnabled = false`. Older reports that quote that pin are **stale**.

### 5.2 API loads `.env` then environment variables

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) walks cwd parents and hard-includes `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`.

### 5.3 Lab `.env` arms the flag

`D:\Prop\.env` L73 (boolean only; no secrets quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (adjacent feature flag, also boolean only):

```
FEATURE_COPY_TRADING_ENABLED=true
```

### 5.4 Hosted logon does **not** re-pin false

`CTraderFixLogonHostedService.ExecuteAsync` L60–70 writes Quote/Trade logon status and **logs** `_runtime.RealCopyEnabled`. It never assigns `_runtime.RealCopyEnabled = false`. Older “hosted pin” quotes are **stale**.

### 5.5 Surfaces that do **not** keep it false

| Surface | What this slot saw |
|---|---|
| `launchSettings.json` | no `REAL_COPY_*` key (does not override `.env`) |
| `apps/api/appsettings.json` | `FeatureFlags.LiveCopyEnabled=false` — **unread** by DI for this runtime flag |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default `false`; hosted service does **not** bind `IOptions<CTraderFixOptions>` |
| `/api/settings` (minimal) | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` — **mirrors the bound flag** |
| `/api/health` | `realCopyEnabled = runtime.RealCopyEnabled` |
| `SettingsController` | unused MVC; `LiveCopyEnabled` from JSON default false; not the startup path |

Therefore: on a normal API boot that finds `D:\Prop\.env`, `LiveRuntimeStatus.RealCopyEnabled` becomes **true**. Claim 5 is **false**.

### 5.6 Why capital is still not at risk (does not rescue claim 5)

`CopyTradingService`:

- `NewOrderSingleImplemented = false` (const)
- `VenueReconciled = false` (const)
- persist `AllowFixSend = false` (L306 hardcoded on the record)
- send branch L312 cannot fire without NOS + LIVE + reconciled; even then it only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`
- blocker list L468–479 includes `No NewOrderSingle sender — SAFE_BY_ABSENCE` and, when the runtime flag is false, `REAL_COPY_EXECUTION_ENABLED is false` (that last blocker is **not** present when `.env` is true)

`CTraderFixSession` still cannot emit `35=D`. Armed flag ≠ ticket.

Claim 5 **FAIL**.

---

## Residuals (do not change scoreboard)

- `DemoSeeder` remains in tree; tests still call it. Not API startup.
- Sibling demo FIX helpers can `Build("D")`. Not `CTraderFixSession`. Not wired to copy.
- `FEATURE_COPY_TRADING_ENABLED` is `true` in `.env` and the API settings endpoint hardcodes `FEATURE_COPY_TRADING_ENABLED=true`. Pipeline is on; send is still absent.
- `AllocationFactor` is now the 1:1 policy constant. Irrelevant while NOS is unimplemented.
- This slot did not live-attach Manager and did not send FIX beyond reading source.

---

## Verdict

**FAIL.**

Claims 1–4 are file-proven. Claim 5 is disproved: `REAL_COPY_EXECUTION` does **not** stay false.

Risk to destination capital: **NONE** (`SAFE_BY_ABSENCE` of `35=D` / `NewOrderSingleImplemented=false`).
