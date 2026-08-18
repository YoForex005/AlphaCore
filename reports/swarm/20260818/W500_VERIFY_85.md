# W500_VERIFY_85 — Adversarial live-path verify (slot 85)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 85 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence. This slot re-read the product files listed below. Chat / other-agent reports were used only as a map of paths.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from live files. Claim 5 is false on the API composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + env load + `/api/settings` flag echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/trader walks |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog caller `*` / `null` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Entire 135-line hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default unread |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` → process env |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Send still unimplemented |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick only |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Only product `DemoSeeder` caller |
| `D:\Prop\.env` L73 + L106 | Flag booleans only |

---

## 1. DemoSeeder is not the API startup path — PASS

API startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`apps/api/Program.cs` is 160 lines. Zero `DemoSeeder` tokens.

Grep of `D:\Prop\apps` `*.cs` / `*.json` for `DemoSeeder`: **0 hits**.

Both workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

DI fail-closes Fake/dummy before connectors exist, then registers Native only:

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `D:\Prop\src\Infrastructure` for `FakeMt5`: **0**. The `FakeMt5BrokerConnector` class still exists under `D:\Prop\src\Mt5\Connectors\` but is not registered on the host hop.

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25). That is not `apps/api`.
- Scratch programs under `reports/swarm/20260818/_tmp_*` also call the seeder; they are not the API process.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–186.

Primary (network request, mask `*`):

```152:165:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Fallback when the request list is empty (pump cache walk):

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

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–46). Hosted `LiveIngestHostedService` L56 calls `SyncCatalogAsync`. `_pumpEnabled` does **not** gate `GetGroupsCore`. Connect still tries `PUMP_MODE_GROUPS|USERS|POSITIONS` first, then `PUMP_MODE_NONE` (`NativeMt5BrokerConnector` L89–110); fetch is request-first either way.

**Honesty limits (not a FAIL of the capability claim):**

- This slot did **not** attach to Achiever/Starwave. Any prior 18-group census is **not** re-proven here. Result is therefore **PASS_SOURCE**, not a live count.
- If `GroupRequestArray("*")` returns `OK`/`OK_NONE` with a **non-empty but ACL-incomplete** array, the `GroupTotal` fallback is skipped. Completeness is then “whatever the manager ACL returns,” which is the correct Manager-API meaning of ALL.
- Empty request + empty cache → empty list, no throw.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Read: `NativeMt5BrokerConnector.GetAccountsCore` L189–214 + `ReadAccountsForGroup` L216–271.

`GetAccountsAsync(null)` (the live catalog argument) walks **every group name** from `GetGroupsCore()`, then per group:

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

Order:

1. **`UserRequestArray`** (network) — primary
2. **`UserGetByGroup`** — only on hard fail (not OK / OK_NONE / NOTFOUND). Pump-cache.
3. **`UserLogins` + `UserRequestByLogins`** — if the user array is still empty

Catalog caller: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)`. Hosted `LiveIngestHostedService` L56 calls `SyncCatalogAsync`. Manual `/api/ops/resync` does the same (`apps/api/Program.cs` L129).

**Honesty limits:**

- This slot did **not** re-count logins. Prior Achiever/Starwave login sums are **not** re-proven. Result is **PASS_SOURCE**.
- Hosted **scoring** later uses `ListLoginsWithDealsAsync` only. Catalog persist is still all accounts from the request walk; zero-deal logins stay un-scored unless `/api/ops/resync` runs (`ListLoginsAsync` at `Program.cs` L134).
- `UserGetByGroup` cache fallback is a residual hole if request hard-fails **and** pump users were never filled.

Capability claim is proven from the connector file.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of that file for `35=D` and `NewOrderSingle`: **0 hits**.

Outbound builder is only Logon. Single `WriteAsync` at L49. Sockets disposed via `using`.

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

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists status. It never builds a NewOrderSingle.

**Off-hop residual (does not change claim 4):** sibling `CTraderFixDemoTestTrade.cs` can `Build("D")`. That is **not** `CTraderFixSession`. Copy hop const `NewOrderSingleImplemented = false` (`CopyTradingService` L18). Persist always writes `AllowFixSend = false` (`CopyTradingService` L306). Copy hosted service ticks SHADOW/roster only (`CopyTradingHostedService` L28–33).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproven)

The claim is that the flag **stays false**. The live API path **arms** it.

Chain:

1. API first line after usings: `EnvFile.FindAndLoad()` (`apps/api/Program.cs` L10). Then `builder.Configuration.AddEnvironmentVariables()` (L13).
2. `EnvFile` walks cwd parents and the hard path `D:\Prop\.env` (`EnvFile.cs` L8–15), then `Environment.SetEnvironmentVariable` for every `KEY=value` (`EnvFile.cs` L28–38).
3. Grep of `D:\Prop\.env` for `REAL_COPY` / `FEATURE_COPY` (booleans only; no secrets):
   - L73 `REAL_COPY_EXECUTION_ENABLED=true`
   - L106 `FEATURE_COPY_TRADING_ENABLED=true`
4. DI is the **only** C# assignment of `RealCopyEnabled` (repo-wide `RealCopyEnabled =` grep: 1 hit):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

5. `CTraderFixLogonHostedService` does **not** re-pin the flag. It logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled` (L68–70) and only updates Quote/Trade status.
6. `/api/settings` **echoes** the runtime boolean — it is not a hardcoded false:

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

`apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key (`FeatureFlags.LiveCopyEnabled=false` is a different unread JSON path).

**Unread / stale pins (do not rescue claim 5):**

- `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). Hosted logon does not bind this POCO onto `LiveRuntimeStatus`.
- Fix-worker `Worker.cs` L21 reads **`CTrader:RealCopyExecutionEnabled`** with default `false`. Different key. Log-only. Does not write the API runtime singleton.
- Architecture docs still say `REAL_COPY_EXECUTION_ENABLED=false`. Docs are not the live bind.
- Workers do **not** call `EnvFile.FindAndLoad()`. Their `RealCopyEnabled` is process-env / appsettings only. The **API** path does load `.env` and therefore **does not stay false**.

Copy hop remains `SAFE_BY_ABSENCE` even while the flag is armed: `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`, `CTraderFixSession` is `35=A` only, `VenueReconciled=false`. Residual: a future sender wired to `LiveRuntimeStatus.RealCopyEnabled` would see **true** on the API host.

---

## Risk to capital

**NONE** today (`SAFE_BY_ABSENCE`).

No live Manager attach this slot. No `35=D` on `CTraderFixSession`. Copy pipeline writes SHADOW intents and hard-false `AllowFixSend`. Dest cannot receive a ticket from this hop.

Residual risk is **configuration**, not a send: the API runtime flag is armed. Do not add a sender while L73 is `true` and DI binds it.

---

## What this slot did not do

- Did not attach Achiever/Starwave Manager.
- Did not GET `/api/settings` on a running host (loopback not used).
- Did not flip `.env`.
- Did not modify product or test source.
- Did not invoke `CTraderFixDemoTestTrade` / `tools/DemoFixTestTrade`.
