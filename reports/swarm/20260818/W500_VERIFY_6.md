# W500_VERIFY_6 — Adversarial live-path re-read (slot 6)

| Field | Value |
|---|---|
| Slot | **6** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read the live path files myself. Do not trust other agents. |
| Product source | **Not modified.** Report only. |
| Secrets printed | **None.** Quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`. No passwords, no connection strings, no FIX username values. |
| Live Manager attach this slot | **No.** Capability claims are from source. Census 18/8460 is **prior / not re-probed**. |

**Honesty rule:** FAIL the slot if any assigned claim cannot be proven from the file as written. Partial PASSes stay partial. Stale reports (A001 / A002 / A014 pin-false / CREDENTIALS “forced false”) are named when they conflict with the tree.

---

## Verdict

**FAIL** — claim (5) is disproven from the live files.

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | API `Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS (capability)** | `GetGroupsCore` calls `GroupRequestArray("*")` first; empty list falls back to `GroupTotal` + `GroupNext`. Not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS (capability)** | `ReadAccountsForGroup` calls `UserRequestArray` first; empty → `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Not re-attached this slot. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Full file 135/135: only outbound MsgType is `(35, "A")`. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Cannot prove it stays false. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then DI L41 binds that key onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host does **not** re-pin false. |

**Bottom line:** live API path is Native Manager catalog ingest + `BrokerCatalogSeed`, not FakeMt5/`DemoSeeder`. Request APIs for groups/users are wired. Hosted FIX hop cannot emit NewOrderSingle. **The runtime copy-arm flag does not stay false.** Send remains impossible by absence of a builder (`SAFE_BY_ABSENCE`), not by a pinned-false flag.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).

---

## 1. DemoSeeder is not the API startup path — PASS

Assigned files read this slot:

- `D:\Prop\apps\api\Program.cs` (160/160)
- `D:\Prop\apps\fix-worker\Program.cs` (18/18)
- `D:\Prop\apps\mt5-worker\Program.cs` (18/18)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (140/140)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (112/112)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (63/63)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94/94)

API startup after maps:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

Both workers seed the same catalog writer, not the demo seeder:

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Grep of `D:\Prop\apps` for `DemoSeeder`: **0**. Grep of product `*.cs` / `*.csproj` / `*.json` for `DemoSeeder` hits only:

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (class still on disk)
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` (test fixture)
- leftover `_tmp_*` eval programs under `reports\swarm\20260818\` (not hosts)

DI fail-closes Fake/dummy before connectors:

```36:48:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`CreateConnectors` constructs **Native ×2** only (`Achiever` + `StarwaveFx`). `DemoBrokerFactory.CreateDefault()` / `FakeMt5BrokerConnector` is reached only from `DemoSeeder` L126, which no host calls.

**Residual (does not flip claim 1):** `apps\mt5-worker\Worker.cs` L31–35 still scores hardcoded `{10001, 10002, 10003, 99001}` **after** a live `SyncBrokerAsync`. That is leftover dummy-login scoring, not `DemoSeeder` on API startup. Hosted API ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). `/api/ops/resync` walks `store.ListLoginsAsync` (API `Program.cs` L134).

A002 (“API still calls `DemoSeeder.SeedAsync`”) is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (capability)

File read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

Vendor surface (not a product caller; proves the symbols exist):

- `IMTManagerAPI::GroupTotal` — `MT5APIManager.h` L205
- `IMTManagerAPI::GroupRequestArray` — `MT5APIManager.h` L212

Product walk:

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

Primary = network `GroupRequestArray("*")` (manager-ACL-visible groups). Fallback = pump-cache `GroupTotal`/`GroupNext` only when the request list is empty. `_pumpEnabled` does **not** gate this method.

Live ingest uses that walk:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

A001 (“Zero hits for `GroupRequestArray` under `src`”) is **stale**.

**Not proven this slot:** a live Connect + non-zero `GroupTotal` / request array. Prior census 8+10=18 groups is **not re-measured here**. Claim as written is **can**, and the file shows the enumerator.

Honest bound: “ALL” = every group the **manager ACL** returns for mask `*`, not every group on the trade server.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

Vendor:

- `UserLogins` — `MT5APIManager.h` L254
- `UserRequestArray` — `MT5APIManager.h` L410

Product:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            // ... ReadAccountsForGroup per name ...
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        // ...
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

Order: request array → cache `UserGetByGroup` only on hard fail → if still empty, `UserLogins` then `UserRequestByLogins`. `GetAccountsAsync(null)` walks **every** group from claim 2, then unions by login.

Ingest / probe path is `GetAccountsAsync(null)` (`DealIngestionService` L48 / L62). Hosted scoring is a **subset** (`ListLoginsWithDealsAsync`) — that does not shrink the catalog persist.

**Not proven this slot:** live login counts. Prior 6512+1948=8460 is **not re-probed**. Claim as written is capability.

---

## 4. CTraderFixSession has no 35=D — PASS

File read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of that file for `35=D`, `(35, "D")`, `NewOrderSingle`: **0**.

Only outbound MsgType constructor:

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

`TryLogonAsync` writes that one frame (`WriteAsync` L49), reads one 4096-byte chunk, then `using` disposes `TcpClient` / `SslStream`. No heartbeat loop. No order builder.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” It never builds tag 35=D.

Copy hop independently cannot send even if someone added a builder later today:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- persist `AllowFixSend = false` hardcoded L211
- 0 `ExecutionIntent` writers in this tree (not re-grepped as a claim; not required to pass (4))

**Residual (does not flip claim 4):** sibling `CTraderFixDemoTestTrade` **does** `Build("D")` at L139 / L163 / L197. That type is **not** `CTraderFixSession`. Caller is only `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Gate at L43–47 refuses `live-*` / `live.*` / account `1369850`. Not registered in DI / API / workers.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Assigned reading: `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `apps\api\Program.cs`, `apps\fix-worker\Worker.cs`, `CopyTradingService.cs`, lab `.env` **flag line only**.

### What “stays false” would require

A process pin: DI / hosted logon / committed config force `false` regardless of env. That pin is **gone**.

### What the files actually do

API loads lab env first:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` includes the literal path `D:\Prop\.env` (L14).

Lab flag (boolean only; no other `.env` keys quoted):

```
D:\Prop\.env L73: REAL_COPY_EXECUTION_ENABLED=true
```

DI copies that string onto the live runtime object:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for a log line (L69–70). It never assigns `false`. The old “hosted re-pin false” is **absent** from the 112-line file.

`/api/settings` is **not** a hardcoded false. It mirrors the runtime field:

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

There is no `MapControllers()` in the API host. Dead leftover `Controllers\SettingsController.cs` (different DTO names: `LiveCopyEnabled`) is **not** the live `/api/settings`.

POCO default is still false and is **not** the runtime choke:

```32:35:D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs
    /// <summary>
    /// When true, allow placing new orders (NewOrderSingle). Default OFF.
    /// </summary>
    public bool RealCopyExecutionEnabled { get; set; } = false;
```

Nothing in `AddTraderIntelligence` binds `CTraderFixOptions` / `CTrader:RealCopyExecutionEnabled`. Fix-worker `Worker.cs` L21 reads the **nested** key with default `false` and only logs. That worker still stamps sessions `Disconnected`. It does not pin the API runtime flag.

`CopyTradingService.GenerateShadowIntentsAsync` passes `_runtime.RealCopyEnabled` into `RiskEngine.Evaluate` as `RealExecutionEnabled` (L190). Persist still forces `AllowFixSend = false` (L211). That proves **send stays off**, not that **the flag stays false**.

### Stale reports that claimed the opposite

| Report | Claim | Status |
|---|---|---|
| `reports\CREDENTIALS_AND_COPY_STATUS.md` L30 | `REAL_COPY_EXECUTION_ENABLED` **false (forced)** | **STALE** |
| A014 L270 / L26 | DI / settings pin false | **STALE** |
| W500_68 / W500_108 | hosted + DI pin false | **STALE** |

### Why this is a slot FAIL, not a capital FAIL

Claim (5) as written is a flag-state claim. The files prove the opposite: on the API host with `D:\Prop\.env` loaded, `LiveRuntimeStatus.RealCopyEnabled` becomes **true**. I therefore cannot prove “stays false.” Instruction: FAIL the slot.

Capital is still **not** at risk: claim (4) + `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`. Next person who adds a `35=D` builder would see the flag **already armed**.

---

## Cross-check: ingest is catalog-first, not 4 demo logins

```38:62:D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs
            var connectors = registry.All().ToList();
            foreach (var connector in connectors)
            {
                // Connect → SyncCatalogAsync (GetGroupsAsync + GetAccountsAsync(null))
```

`LiveMt5Registration.CreateConnectors` returns Native Achiever + Native Starwave only. No Fake substitution after the password AND.

---

## Residuals (do not greenwash)

1. **Flag armed.** `.env` L73 `true` + DI bind + no re-pin. Settings API will advertise `REAL_COPY_EXECUTION_ENABLED=true` when that env is loaded.
2. **Demo `Build("D")` exists** off the host hop (`CTraderFixDemoTestTrade`, tools-only, demo-gated).
3. **mt5-worker** still scores four dummy logins after a live sync.
4. **`DemoSeeder.cs` remains** for tests; not API startup.
5. **This slot did not live-attach.** 18 groups / 8460 traders / 1984 positions is prior arithmetic (8+10 / 6512+1948 / 1506+478), not a new probe.
6. **`GET /api/trades` still `Take(200)`** (`Program.cs` L110) — HTTP page cap, not Manager enumeration.
7. **In-memory DB fail-open** when `DATABASE_URL` contains `<SECRET>` (`DependencyInjection` L27–29). Unrelated to the five claims; scores die on restart.

---

## Risk to capital

**NONE** (`SAFE_BY_ABSENCE`).

`CTraderFixSession` cannot send NewOrderSingle. Copy persist cannot set `AllowFixSend`. Const `NewOrderSingleImplemented` is false. Demo helper is not on the API/worker hop and refuses the live account id. Fetch APIs are read-only Manager request/cache walks.

Do **not** treat this FAIL as “money is leaving.” Treat it as: **the operator arm is already true; only the missing sender protects the book.**

---

## Files read (this slot)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup, settings flag, resync |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | TFM `net8.0-windows` x64 |
| `D:\Prop\apps\api\appsettings.json` | no `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | dead leftover; not mapped |
| `D:\Prop\apps\fix-worker\Program.cs` | seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | nested flag log-only |
| `D:\Prop\apps\mt5-worker\Program.cs` | seed path |
| `D:\Prop\apps\mt5-worker\Worker.cs` | dummy-login residual |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | exists; not host-called |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | actual startup seed |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Native-only + env bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 factory |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | catalog ingest |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | SHADOW tick |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const false; AllowFixSend false |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` mutable |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/User request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads `D:\Prop\.env` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 135/135; `35=A` only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no false re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unused POCO default false |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | L205 / L212 / L254 / L410 |
| `D:\Prop\.env` L73 only | boolean `true` |

---

## Slot 6 close

```
verdict=FAIL
slot=6
claim1=PASS DemoSeeder not API startup (BrokerCatalogSeed only)
claim2=PASS capability GroupRequestArray("*") else GroupTotal
claim3=PASS capability UserRequestArray then UserLogins
claim4=PASS CTraderFixSession 35=A only; 35=D=0
claim5=FAIL REAL_COPY env-bound true; no process pin
risk_to_capital=NONE SAFE_BY_ABSENCE
secrets=none
```
