# W500_VERIFY_22 — Adversarial live-path verify (slot 22)

- **slot:** 22
- **date:** 2026-08-18
- **role:** Adversarial verifier. Read live path files. Do **not** trust other agents.
- **method:** Independent `read_file` of assigned product files + targeted `grep`. No Manager re-attach. No FIX send. No `.env` secret values printed (boolean flags only). Product source **not** edited.
- **verdict:** **FAIL**
- **risk_to_capital:** **NONE** (`SAFE_BY_ABSENCE` of a product-host NewOrderSingle / `35=D` sender)

## 0. Assigned claims

Prove from **files**, or FAIL the claim.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` token count under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | **PASS** | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty-list fallback L174 `GroupTotal` + `GroupNext`. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. `GetAccountsCore(null)` walks every group name. |
| 4 | `CTraderFixSession` has **no** `35=D` | **PASS** | File **135/135**. Only outbound MsgType is `(35, "A")` L96. `grep` `35=D` / `(35, "D")` in that file = **0**. `NewOrderSingle` = **0**. |
| 5 | `REAL_COPY_EXECUTION` **stays false** | **FAIL** | Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. DI L41 binds that key onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host **does not** re-pin false. `/api/settings` echoes the runtime bool. |

**Slot rule:** FAIL if any claim cannot be proven from the file. Claim 5 is **disproven** (opposite is on disk). Overall **FAIL**.

Claims 1–4 stand independently. They do **not** rescue claim 5.

This slot did **not** live-attach Manager or FIX. Census numbers from other reports are **not** re-proven here.

## 1. Claim 1 — DemoSeeder is not the API startup path — PASS

Files read:

- `D:\Prop\apps\api\Program.cs` (**160/160**)
- `D:\Prop\apps\mt5-worker\Program.cs` (**18/18**)
- `D:\Prop\apps\fix-worker\Program.cs` (**18/18**)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (header + `EnsureAsync`)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (class still exists at L14)

API startup after `app.Build()`:

```152:157:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync` call. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

`grep` `DemoSeeder` under `D:\Prop\apps` = **0**.

Product C# callers of `DemoSeeder.SeedAsync` (this slot):

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class definition. **Not** invoked by hosts. |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | Test only. |
| `reports\swarm\20260818\_tmp_*` harnesses | Offline evals, not API. |

Both workers also seed `BrokerCatalogSeed.EnsureAsync` only (`apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15).

**Residual (not a claim fail):** `DemoSeeder.cs` remains on disk. Tests can still seed FakeMt5 / 10001. That is **not** the API process path.

Stale reports that still say API startup calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005_dashboard_traders.md`) are **wrong for the current 160-line `Program.cs`**.

## 2. Claim 2 — Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

File read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (**458/458**).

Primary walk (request API, mask `*`):

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
```

Measured facts from this file:

- `GroupRequestArray("*", arr)` is the **first** enumerator.
- If that list is empty, `GroupTotal()` + `GroupNext` walks the local cache.
- `_pumpEnabled` is written (L96/L110) and **never** read by `GetGroupsCore`.
- Connect tries pump (`PUMP_MODE_GROUPS | USERS | POSITIONS`) then retries `PUMP_MODE_NONE` (L101). Request APIs remain valid on the no-pump path.
- `Take(` / plan-map filter: **0** in this type. Empty group names are skipped; others are kept (`AddGroup` L369–381).

**Honesty bound:** “ALL groups” means **every group this manager ACL can see**, not every group on the trade server. This slot did **not** re-run `LiveBrokerProbe`. Capability is proven from source; live 8+10=18 is **not** re-measured here.

Stale `A001_native_connector.md` (“zero `GroupRequestArray` under `src`”; groups = cache only) is **wrong** for the current file.

## 3. Claim 3 — All traders via UserRequestArray / UserLogins — PASS

Same connector file.

`GetAccountsCore(null)` (the ingest / probe “all accounts” path) collects every name from `GetGroupsCore()`, then `ReadAccountsForGroup` per name (L189–213).

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

Measured order:

1. `UserRequestArray(gname, users)` — network request (primary).
2. Hard fail only → `UserGetByGroup` (pump cache).
3. Empty array → `UserLogins` + `UserRequestByLogins`.

Account money fields come from `UserAccountRequestArray` with `UserAccountGetByGroup` fallback (L235–237). That is adjacent, not the login enumerator.

DI live path is Native ×2 only (`LiveMt5Registration.CreateConnectors`: Achiever + StarwaveFX). Fake is not registered when `AddTraderIntelligence` succeeds (`DependencyInjection.cs` L36–48).

**Honesty bound:** this slot did not re-count logins. “Can list all traders the manager sees” is proven as a code path. Prior 6512+1948=8460 is **not** re-proven.

## 4. Claim 4 — CTraderFixSession has no 35=D — PASS

File read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

`grep` in that file:

| Pattern | Count |
|---|---|
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "A")` | **1** (L96) |
| `WriteAsync` | **1** (L49, Logon bytes) |

Only outbound builder:

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

`TryLogonAsync`: one TCP+TLS connect, one write, one 4096-byte read, then `using` disposes `TcpClient` / `SslStream`. No heartbeat loop. No order book. No tag 11 / 38.

Hosted caller `CTraderFixLogonHostedService` (read **112/112**) invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and logs `NewOrderSingle still unimplemented`. It never builds MsgType D.

**Scope honesty (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.cs` has `Build("D")` at L139 / L163 / L197. That class is **not** `CTraderFixSession`. It is a tools/demo helper (`tools/DemoFixTestTrade`), not wired in `AddTraderIntelligence`. Claim 4 is the assigned type only.

Copy hop still cannot send: `CopyTradingService.NewOrderSingleImplemented = false` (const L17); persist `AllowFixSend = false` (L211); hosted copy writes SHADOW intents only (`CopyTradingHostedService` L28–30).

## 5. Claim 5 — REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Files prove it does **not**.

### 5.1 What is still false (insufficient)

| Surface | Value | Why it does not save the claim |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (L35) | POCO is **not** bound from env. Runtime flag is a **different** object. |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | **false** | Different name. Unused by DI. |
| Architecture / README / `docs/architecture.md` | policy `=false` | Docs are not the process. |
| `fix-worker` `CTrader:RealCopyExecutionEnabled` | `GetValue(..., false)` | Nested key; log-only. Worker still stamps `Disconnected`. |
| `CopyTradingService.NewOrderSingleImplemented` | const **false** | Blocks send. Does **not** keep `REAL_COPY_EXECUTION_ENABLED` false. |

### 5.2 What is actually armed (claim-killing)

Lab env (boolean only; **no** secrets quoted):

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
D:\Prop\.env L106: FEATURE_COPY_TRADING_ENABLED=true
```

API host loads that file before DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes hardcoded candidate `D:\Prop\.env` and `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line (`EnvFile.cs` L14–L38).

DI **binds** the env token onto the live singleton:

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`/api/settings` is **not** a hardcoded false:

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

`CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for a log line (L69–70) and **never writes it**. There is **no** re-pin to false.

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L44). `BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** the runtime bool is already false (L316–317) — that blocker is **absent** when `.env` loaded.

### 5.3 Stale “forced false” reports

These are **wrong** for current DI + `.env`:

- `reports/CREDENTIALS_AND_COPY_STATUS.md` (`REAL_COPY_EXECUTION_ENABLED` **false (forced)**)
- `W500_RESEARCH_68.md` / `W500_RESEARCH_108.md` (pin-false)
- `A014_live_path_now.md` (“DI pins false”)
- `E038_flag_api.md` (hardcoded `/api/settings` false)

### 5.4 Why this is FAIL and not a capital event

Claim 5 is about the **flag staying false**. The flag is **true** on the lab host after `EnvFile` + DI.

Live send is still impossible **today** because there is no `35=D` builder on `CTraderFixSession` and `NewOrderSingleImplemented` is const false. That is claim 4 + copy consts — **not** claim 5.

Residual: the **next** sender that keys off `LiveRuntimeStatus.RealCopyEnabled` will see **armed**. That is why the assigned claim must stay FAIL until `.env` L73 is `false` **or** DI stops binding it **or** a host re-pin is restored.

## 6. Inventory (this slot)

| File | Lines read | Used for |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` | 160/160 | Claims 1, 5 |
| `D:\Prop\apps\mt5-worker\Program.cs` | 18/18 | Claim 1 (workers also catalog-only) |
| `D:\Prop\apps\fix-worker\Program.cs` | 18/18 | Claim 1 |
| `D:\Prop\apps\fix-worker\Worker.cs` | 50/50 | Claim 5 (nested key, log-only) |
| `D:\Prop\apps\api\appsettings.json` | 50/50 | Claim 5 (`LiveCopyEnabled` unused) |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | 458/458 | Claims 2, 3 |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | 41/41 | Claim 5 load path |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | 62/62 | Claims 1, 5 |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | 80+ | Native ×2 only |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | header + EnsureAsync | Claim 1 |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | L1–20 | Exists; unused by API |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135 | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | 112/112 | Claims 4, 5 (no re-pin) |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | 79/79 | POCO default false (not the runtime flag) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | 66/66 | Claim 5 object |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | status + persist tail | Send still absent |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | 40/40 | SHADOW ticks only |
| `D:\Prop\.env` | **flags only** L73 / L106 | Claim 5 |

`grep` (no secret values):

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins` in `NativeMt5BrokerConnector.cs` = present at L155 / L174 / L223 / L230
- `35=D` / `(35, "D")` in `CTraderFixSession.cs` = **0**
- `REAL_COPY_EXECUTION_ENABLED` in `DependencyInjection.cs` L41 = env bind
- `D:\Prop\.env` L73 = `true`

## 7. Risk to capital

**NONE** from the live hop as coded:

- Manager connector is GET/request only (`Group*` / `User*` / `DealRequest*` / `PositionRequest*`). No `DealerSend` / `OrderSend` on this type.
- Product FIX session outbound is `35=A` Logon only; socket disposed.
- Copy pipeline: SHADOW intents; persist `AllowFixSend=false`; const `NewOrderSingleImplemented=false`.

**Not** “safe because REAL_COPY is false.” That sentence is **false** on this lab after `.env` load. Safety is **absence of a sender**.

## 8. Verdict

**FAIL.**

Claims 1–4 proven from current files. Claim 5 **fails**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false (`.env` L73 `true` + DI bind + no host re-pin).

Product source not modified. Secrets not printed. This slot did not attach Manager or send FIX.
