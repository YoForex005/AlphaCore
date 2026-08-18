# W500_VERIFY_31 — Adversarial live-path re-read (slot 31)

| Field | Value |
|---|---|
| Slot | **31** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source | **Not modified.** Report only. |
| Secrets printed | **None.** Boolean flags and public identifiers only. No passwords, proxy auth, FIX secrets, or connection strings. |
| Live attach this slot | **No.** Capability claims proven from source. Runtime census of groups/traders **not** re-measured here. |

**Honesty rule:** quote the files as they sit on disk. If a claim cannot be proven from the file, that claim is **FAIL**. Overall verdict is **FAIL** if any assigned claim fails.

---

## Verdict

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–157 calls `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS (code path)** | `GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count == 0`, L174 `GroupTotal()` + `GroupNext`. This slot did not live-attach, so completeness is not re-measured. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS (code path)** | `ReadAccountsForGroup` L223 `UserRequestArray`; L230–232 `UserLogins` + `UserRequestByLogins` when `users.Total() == 0`. Catalog ingest uses `GetAccountsAsync(null)`. Completeness not re-measured. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: only outbound MsgType is `(35, "A")` Logon. No `"D"` token. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `D:\Prop\.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API loads that file. DI binds it to `LiveRuntimeStatus.RealCopyEnabled`. Hosted FIX logon **does not** re-pin false. |

**Overall: FAIL** — claim 5 is disproven from the live files. Claims 1 and 4 are proven. Claims 2–3 are proven as wired Manager request APIs, not as a live census.

**Risk to capital: NONE (`SAFE_BY_ABSENCE`).** An armed `RealCopyEnabled` still cannot emit a live ticket: copy hop has no `35=D` builder, `NewOrderSingleImplemented = false`, persist `AllowFixSend = false`, `VenueReconciled = false`.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 What the API actually runs

```152:158:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

`/api/ops/resync` (L114–149) walks `ACHIEVER` + `STARWAVEFX` via `ingestion.SyncCatalogAsync` / `SyncBrokerAsync` and `store.ListLoginsAsync`. It does **not** hardcode `{10001, 10002, 10003, 99001}`.

### 1.2 Composition root is Native, not Fake

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs two `NativeMt5BrokerConnector` instances (Achiever + StarwaveFX). `FakeMt5BrokerConnector` is **not** registered.

### 1.3 Where DemoSeeder still lives (not API startup)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class still on disk. Seeds Fake tape + logins `10001/10002/10003/99001`. |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | Test fixture calls `DemoSeeder.SeedAsync`. |
| `reports/swarm/20260818/_tmp_*` scratch hosts | Not product startup. |

`rg DemoSeeder --glob *.cs` under `D:\Prop\apps` = **0**. Both workers seed `BrokerCatalogSeed.EnsureAsync` (`apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15).

**Residual (does not flip claim 1):** `apps/mt5-worker/Worker.cs` L31–35 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover scorer set, not API DemoSeeder startup.

Older reports that say `Program.cs` still calls `DemoSeeder` (A002, A005, A010, A011) are **stale** versus the file on disk.

---

## 2. Native connector groups via GroupRequestArray / GroupTotal — PASS (code path)

```144:186:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5GroupDto> GetGroupsCore()
    {
        lock (_gate)
        {
            Ensure();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var list = new List<Mt5GroupDto>();

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

            return list;
        }
    }
```

Live ingest uses that path without a group filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

**Adversarial residual (does not fail the wired-API claim):** `GroupTotal`/`GroupNext` run only when `list.Count == 0`. A **partial** successful `GroupRequestArray` would **not** fall through to `GroupTotal`. This slot did not attach to Manager, so “all groups on the live servers” is **not independently proven here**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (code path)

`GetAccountsCore` with `group == null` walks every name from `GetGroupsCore()`, then `ReadAccountsForGroup`:

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

`GetAccountsAsync(null)` is the catalog path (`DealIngestionService` L48, L62; `LiveBrokerProbe` L26).

**Adversarial residual:** `UserLogins` is used only when `users.Total() == 0`. A non-empty partial `UserRequestArray` would skip `UserLogins`. Extra fallback `UserGetByGroup` is pump-cache, not a request API. This slot did not re-sum a live census.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines) was read in full.

Outbound builder:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        // ...
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            // 49/56/50/57/52/98/108/141/553/554 only
        };
        return Assemble(fields);
    }
```

The only other `35` uses are **inbound** parse (`Extract(reply, "35")` L55) and a reject log string (`"Logon rejected 35={msgType}"` L73). File contains **zero** `"D"` literals.

Product copy hop also cannot send:

| Guard | File | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const bool` **false** |
| `VenueReconciled` | same L16 | `const bool` **false** |
| Persist `AllowFixSend` | same L211 | **hardcoded `false`** |
| Hosted copy loop | `CopyTradingHostedService.cs` L28–30 | `GenerateShadowIntentsAsync` only |

**Out of assigned class (must not be hidden):** sibling `CTraderFixDemoTestTrade.cs` and `CTraderFixDemoMatrix.cs` contain `Build("D", ...)`. Those are **not** `CTraderFixSession`. Demo helper refuses live host / `live.` SenderCompId / account `1369850`. Not wired by DI / API / copy hosted service.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove the opposite on the API host.

### 5.1 Lab env is armed

`D:\Prop\.env` L73 (boolean only):

```
REAL_COPY_EXECUTION_ENABLED=true
```

(No secret on that line. Other `.env` values were not copied into this report.)

`FEATURE_COPY_TRADING_ENABLED=true` is also present at `.env` L106. That is a different flag. API `/api/settings` hardcodes `FEATURE_COPY_TRADING_ENABLED = true` regardless (`Program.cs` L77).

### 5.2 API loads that file, then DI binds it

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

```14:15:D:\Prop\src\Mt5\Env\EnvFile.cs
            Path.GetFullPath(Path.Combine(cwd, "..", "..", "..", ".env")),
            @"D:\Prop\.env"
```

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`rg RealCopyEnabled\s*=` under `D:\Prop` `*.cs` = **one assignment** (DI L41). `CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L69–70) and does **not** set it back to false.

`/api/settings` and `/api/health` expose `runtime.RealCopyEnabled` (`Program.cs` L55, L76). With `.env` loaded, those surfaces will report **true**.

### 5.3 What still defaults false (does not rescue claim 5)

| Surface | Value | Bound to runtime? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (`CTraderFixOptions.cs` L35) | **No.** DI does not map this POCO onto `LiveRuntimeStatus`. |
| `apps/api/appsettings.json` / Development / launchSettings | **no** `REAL_COPY_EXECUTION_ENABLED` key | Without `.env`, DI compare-to-`"true"` would stay false. |
| `docker-compose.yml` api service | does **not** set the env | Compose-only would stay false. |
| `apps/fix-worker/Worker.cs` L21 | reads `CTrader:RealCopyExecutionEnabled` default **false** (different key) | Log + refuse only. Worker stamps sessions `Disconnected`. |

Committed JSON staying silent is **not** “stays false” when the API explicitly loads `D:\Prop\.env` and binds the armed boolean.

Older reports that claim DI/hosted “pin false” (W500_68 / W500_108 / CREDENTIALS “forced false”) are **stale**.

---

## Stale reports this slot contradicts

| Prior claim | Status vs live files |
|---|---|
| A002 / A005 / A010: API still `DemoSeeder.SeedAsync` | **Stale.** Startup is `BrokerCatalogSeed`. |
| A002: `/api/ops/resync` hardcodes four demo logins | **Stale.** Resync uses `ListLoginsAsync`. |
| W500_68 / 108 / CREDENTIALS: REAL_COPY forced false in process | **Stale.** DI binds env; `.env` L73 is `true`; no re-pin. |
| “Product `35=D=0` everywhere” | **Narrow-stale.** True for `CTraderFixSession` + copy hop. False for demo helpers `Build("D")`. |

---

## Risk to capital

**NONE — `SAFE_BY_ABSENCE` on the copy hop.**

Armed `REAL_COPY_EXECUTION_ENABLED` is an operator wish and a **runtime lie risk** on `/api/settings` / `/api/health`. It is **not** a sender. There is no NewOrderSingle implementation on `CTraderFixSession`. Copy persist forces `AllowFixSend = false`. Venue recon const is false.

Residual: the next engineer who adds a `35=D` builder will see `RealCopyEnabled == true` on a host that loaded `D:\Prop\.env`. That is why claim 5 is FAIL even though capital is not at risk **today**.

---

## Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (existence / not registered)
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\docker-compose.yml` (api env: no REAL_COPY key)
- `D:\Prop\.env` L73 boolean only
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` (DemoSeeder caller)
- `D:\Prop\tools\LiveBrokerProbe\Program.cs` (catalog `GetGroups` / `GetAccounts(null)`)

*End of W500_VERIFY_31. Product source was not modified. No secrets printed.*
