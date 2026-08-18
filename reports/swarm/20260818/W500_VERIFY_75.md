# W500_VERIFY_75 — Adversarial live-path verify (slot 75)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **75** |
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
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound MsgType is `A`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from files this slot (2–3 as capability only). Claim 5 cannot be proved: lab `.env` L73 is `true`, API `EnvFile.FindAndLoad()` loads that file (hard path `D:\Prop\.env`), DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`, `/api/settings` exposes that value, and `CTraderFixLogonHostedService` does **not** re-pin false.

Risk to destination capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (not other agents)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` (160 lines) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Worker.cs` | FIX worker flag read (nested `CTrader:` key; log-only) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | live seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists; not called by hosts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind L41 |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader walks (458 lines) |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | live ingest |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | outbound MsgType (135 lines) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` off hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented; persist `AllowFixSend=false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` formula |
| `D:\Prop\tools\DemoFixTestTrade\Program.cs` | only caller of demo `Build("D")` |
| `D:\Prop\.env` L73 + L106 **flag names/booleans only** | live arm |

Independent greps this slot: `DemoSeeder` under `D:\Prop\apps` = **0**. `35` / `"D"` in `CTraderFixSession.cs` = inbound extract + error format + outbound `(35, "A")` only.

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

Product host callers of `BrokerCatalogSeed.EnsureAsync` this slot:

- `apps/api/Program.cs` L156
- `apps/mt5-worker/Program.cs` L15
- `apps/fix-worker/Program.cs` L15

Workers:

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`apps/mt5-worker/Program.cs` is the same catalog seed.

DI fail-closes Fake before connectors exist:

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        services.AddSingleton<TraderIntelligence.Domain.Risk.RiskEngine>();

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed` writes broker rows + Disconnected FIX sessions + kill-switch default. It does **not** score demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

`GetGroupsCore` request-first, then cache fallback **only if the request list is empty**:

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

Mask `"*"` is the manager-visible enumerator. `_pumpEnabled` is **not** a gate on this walk (connect may use pump or `PUMP_MODE_NONE`; fetch still runs).

Live ingest uses that walk with no group-name filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** name returned by `GetGroupsCore()` (L201–202).

**Adversarial caveat (does not fail the “can list via those APIs” claim):** if `GroupRequestArray("*")` returns OK with a **non-empty subset**, `GroupTotal` is skipped. File proves the ALL-groups *capability*. Completeness of a live census is **unverified here** (this slot did not attach).

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `ReadAccountsForGroup`:

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

Order: `UserRequestArray` (network) → cache `UserGetByGroup` only on hard fail → if still empty, `UserLogins` + `UserRequestByLogins`.

`GetAccountsCore(null)` unions every group. Hosted catalog + `/api/ops/resync` both call `GetAccountsAsync(null)`.

**Adversarial caveat:** if `UserRequestArray` returns OK with a **non-empty subset**, `UserLogins` is skipped. File capability **PASS**. Live “all traders” count was **not** re-measured this slot. Hosted scoring is deals-driven (`ListLoginsWithDealsAsync` on ingest), which does **not** shrink the catalog walk.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Outbound MsgType is only Logon `A`. One `WriteAsync`. Socket + SSL disposed via `using`.

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

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

Tag `35` appears three times: extract inbound type (L55), reject text `35={msgType}` (L73), outbound `(35, "A")` (L96). **Zero** `"D"` / `NewOrderSingle` tokens in this type.

Hosted logon calls this type twice (QUOTE 5211, TRADE 5212) and then **disposes**. No heartbeats, no `35=x` / `35=V`, no order.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` exists (L139 flatten, L163 open, L197 close). Demo-gated (`demo-` host / `demo.` sender; refuses `live-*` / `live.` / account `1369850`). Only caller is `D:\Prop\tools\DemoFixTestTrade\Program.cs` — **not** DI, **not** API, **not** copy. Claim is `CTraderFixSession`, not the whole product.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

**Disproved from files.** The flag is armed in lab env and bound into process state.

1. `D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secrets quoted). L106: `FEATURE_COPY_TRADING_ENABLED=true` (unused by DI; API hardcodes FEATURE `true` at `Program.cs` L77).
2. API boot loads that file:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` walks cwd-relative candidates then **`D:\Prop\.env`** (`EnvFile.cs` L8–15) and `SetEnvironmentVariable` for every `KEY=value`.

3. DI copies the env token onto the singleton (no hard-false pin):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. `/api/settings` and `/api/health` expose `runtime.RealCopyEnabled` (`Program.cs` L55, L76). With L73 `true` and the bind above, the running API **advertises armed**.
5. `CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L69–70) and **never** assigns `RealCopyEnabled = false`. No re-pin exists in that file.
6. `CTraderFixOptions.RealCopyExecutionEnabled` still **defaults false** (`CTraderFixOptions.cs` L35). That POCO is **not** the API runtime flag. `apps/fix-worker/Worker.cs` L21 reads **`CTrader:RealCopyExecutionEnabled`** (nested, default false) — a **different key** from `REAL_COPY_EXECUTION_ENABLED`. Worker still stamps FIX rows `Disconnected` and does not send.

**Stale reports:** W500_68 / W500_108 / CREDENTIALS “forced false” / A014 “DI pins false” / A006 “API hardcodes false” do **not** match current `Program.cs` L76 + `DependencyInjection.cs` L41.

Copy hop still cannot send even when the flag is true:

- `CopyTradingService.NewOrderSingleImplemented = false` (const)
- `VenueReconciled = false` (const)
- persist `AllowFixSend = false` (`CopyTradingService.cs` L306) — **overrides** `RiskEngine` formula
- live-send branch also requires `NewOrderSingleImplemented && VenueReconciled` (L312)
- `CTraderFixSession` has no `35=D` builder

So claim 5 (**stays false**) fails, while destination capital risk stays **NONE** by absence of a sender.

---

## Capital / send

| Gate | State this slot |
|---|---|
| Product `CTraderFixSession` outbound | `35=A` only |
| `NewOrderSingleImplemented` | `false` |
| Persist `AllowFixSend` | hardcoded `false` |
| `VenueReconciled` | `false` |
| Runtime `RealCopyEnabled` | **true if env is true** (lab `.env` L73) |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`). Residual: the next person who adds a `35=D` builder will see the API host already armed.

---

## Verdict

**FAIL.** Claims 1 and 4 proved. Claims 2–3 proved as file capability only (not re-attached). Claim 5 **disproved**: `REAL_COPY_EXECUTION` does **not** stay false.

Slot 75. Product source not edited. Secrets not printed.
