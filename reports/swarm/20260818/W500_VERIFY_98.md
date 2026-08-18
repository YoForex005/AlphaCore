# W500_VERIFY_98 — Adversarial live-path verify (slot 98)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **98** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Prior swarm notes are **not** evidence. Claim 5 is **disproved**.

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
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (different key) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\api\appsettings.json` | committed flags (`LiveCopyEnabled=false`, unused by live `/api/settings`) |
| `D:\Prop\apps\api\Properties\launchSettings.json` | no `REAL_COPY` env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | unused MVC controller |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | live ingest (present; not re-run) |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow tick only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` off-hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unused by DI) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |

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

`DemoSeeder` token count in `apps/` = **0**. Product C# callers of `DemoSeeder` under `src/` = **the class definition only**. Remaining callers are tests (`tests/Integration/SeedingAndStoreTests.cs`) and swarm `_tmp_*` evals — not API/worker boot.

Same catalog seed on both workers:

- `D:\Prop\apps\fix-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\mt5-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`

DI refuses Fake/dummy before connectors are created:

```36:37:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path.

**Residual (does not put DemoSeeder on API boot):** `DemoSeeder` class still exists. `mt5-worker/Worker.cs` L31 still scores `{10001,10002,10003,99001}` after a real `SyncBrokerAsync` of both brokers. That is leftover dummy scoring, not the API startup seeder.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (source)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

Primary walk is wildcard request. Empty result falls back to pump cache `GroupTotal`/`GroupNext`:

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

Ingest uses that ALL path (`GetGroupsAsync` then `GetAccountsAsync(null)`):

```45:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()` (no plan-name filter).

This slot did **not** re-attach a Manager. Claim is **file capability**, not a live census.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source)

Same connector, `ReadAccountsForGroup`:

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

Order: `UserRequestArray` first; cache `UserGetByGroup` only on hard fail; empty array → `UserLogins` then `UserRequestByLogins`. Combined with claim 2, `group=null` enumerates every group then every user in that group.

Not re-attached this slot. **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

Outbound writer: one `ssl.WriteAsync` of `BuildLogon`. Only constructed MsgType:

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

Grep of that file for `35=D` / `NewOrderSingle` / `"D"` as MsgType: **0**. Tag `35` is read only on the inbound reply (`Extract(reply, "35")`). Sockets are `using`/`await using` and disposed after one logon read.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and then persists session rows. It never sends another MsgType.

**Off-hop residual (does not falsify claim 4):** sibling `CTraderFixDemoTestTrade.cs` `Build("D")` at L139 / L163 / L197. Only caller is `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Not in DI. Not on the copy hop.

Copy hop still cannot send:

```17:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

`AllowFixSend` is persisted `false`. The LIVE send branch is a status string only (`LIVE_SEND_BLOCKED_UNIMPLEMENTED`). Hosted copy tick is `TickRosterAsync` + `GenerateShadowIntentsAsync`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproved)

The claim is that the flag **stays false**. Live files prove the opposite.

1. Lab env (boolean only; no secrets): `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`. L106 `FEATURE_COPY_TRADING_ENABLED=true`.
2. API boot loads that file **before** configuration/DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes hard path `D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=value` line.

3. DI binds the env string onto the singleton runtime (case-insensitive `"true"`):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **one** assignment of `RealCopyEnabled =` in product C# — that bind. No later write sets it back to false.

4. Hosted FIX logon **logs** `_runtime.RealCopyEnabled` and does **not** assign it (`CTraderFixLogonHostedService.cs` L60–70). Prior “pin false on logon” is **gone**.

5. Live `/api/settings` (minimal API, not the unused MVC controller) echoes the runtime:

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

**Stale “false” pins that do not save claim 5:**

| Pin | Why it does not keep runtime false |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled = false` | POCO default; **not** `Configure<>`’d by DI |
| `apps/fix-worker/Worker.cs` `CTrader:RealCopyExecutionEnabled` default false | Different key; log-only; worker does not send |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled=false` | Different name; unused by live `/api/settings` |
| `SettingsController` `LiveCopyEnabled` default false | MVC; Redis ctor; live host uses the minimal-API map |
| Architecture/docs `REAL_COPY_EXECUTION_ENABLED=false` | Docs, not the running bind |
| Copy `NewOrderSingleImplemented=false` | Blocks send; does **not** force the flag false |

Therefore claim 5 is **false**: on a host that loads `D:\Prop\.env`, `LiveRuntimeStatus.RealCopyEnabled` is **true**.

---

## Capital risk (separate from claim 5)

Flag-armed ≠ ticket. Destination capital risk this slot: **NONE** (`SAFE_BY_ABSENCE`).

| Gate | Live file |
|---|---|
| Product hop outbound | `CTraderFixSession` `35=A` only; dispose after one read |
| Copy sender | `NewOrderSingleImplemented=false` |
| Venue | `VenueReconciled=false` |
| Persist | `AllowFixSend=false` |
| Hosted copy | shadow roster/intents only |
| Demo `Build("D")` | tools-only, not DI |

If a later change implements `35=D` while `.env` stays `true` and DI stays bound, the next sender would see an **armed** runtime. That is the residual of claim 5, not a live send today.

---

## Honesty / not claimed

- Did not live-attach Achiever or Starwave this slot. Do not cite 18/8460 as measured here.
- Did not hit a running `/api/settings`. Bind is file-proven; HTTP echo is not re-measured.
- Did not invoke `tools/DemoFixTestTrade`.
- Did not print passwords, hosts-as-secrets, or account credentials beyond already-public non-secret identifiers already in source comments.
- Product source not edited.

---

## Verdict

**FAIL.** Claims 1–4 file-proven (2–3 capability only). Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Copy hop still `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
