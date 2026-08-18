# W500_VERIFY_68 — Adversarial live-path verify (slot 68)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 68 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
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

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence. This slot re-read the product files listed below.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold on the files. Claim 5 is false on the running API composition: lab `.env` L73 is `true`, `EnvFile.FindAndLoad()` loads it, DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and the hosted logon service does not re-pin false.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (live paths only)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + settings echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Connector + REAL_COPY bind |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2, no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` load including `D:\Prop\.env` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog `GetGroups` + `GetAccounts(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog/score |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Assigned FIX writer (135 lines) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Off-hop residual `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon hop; no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false (unbound) |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Runtime flag |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented; persist `AllowFixSend=false` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor request APIs |
| `D:\Prop\.env` | Boolean only: L73 / L106 |

Grep (no secret values): `DemoSeeder` under `D:\Prop\apps` = **0**. `REAL_COPY_EXECUTION_ENABLED` on `.env` L73 = `true`. `FEATURE_COPY_TRADING_ENABLED` on `.env` L106 = `true`. `RealCopyEnabled =` assignment in `src/` = **only** DI L41.

---

## 1. DemoSeeder is not the API startup path — PASS

API startup seed (only catalog writer on the host):

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`, not `DemoSeeder`. There is **no** `DemoSeeder.SeedAsync` token in this file (160 lines).

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**. Both workers seed the same way:

- `D:\Prop\apps\fix-worker\Program.cs` L11–16 → `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16 → `BrokerCatalogSeed.EnsureAsync`

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path.

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25) plus report-scratch `_tmp_*` programs. Those are not `apps/api`.
- `mt5-worker\Worker.cs` L31 still scores `{10001,10002,10003,99001}` in its own loop. That is a leftover dummy login set on the **worker**, not API seed. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005`, `A011`) are **superseded** by current `Program.cs`.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

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

Vendor surface (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`):

- L205 `GroupTotal`
- L212 `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)`

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–46). `_pumpEnabled` does **not** gate `GetGroupsCore`. Connect still tries `PUMP_MODE_GROUPS|USERS|POSITIONS` first, then `PUMP_MODE_NONE` (`NativeMt5BrokerConnector` L89–110); fetch is request-first either way.

**Honesty limits (not a FAIL of the capability claim):**

- This slot did **not** attach to Achiever/Starwave. Prior 18-group census is **not** re-proven here.
- If `GroupRequestArray("*")` returns `OK`/`OK_NONE` with a **non-empty but ACL-incomplete** array, the `GroupTotal` fallback is skipped. Completeness is then “whatever the manager ACL returns,” which is the correct Manager-API meaning of ALL.
- Empty request + empty cache → empty list, no throw.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

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

Vendor (`MT5APIManager.h`): L254 `UserLogins`, L410 `UserRequestArray`.

Catalog caller: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)`. Hosted `LiveIngestHostedService` L56 calls `SyncCatalogAsync`. Manual `/api/ops/resync` does the same (`apps/api/Program.cs` L129).

**Honesty limits:**

- This slot did **not** re-count logins. Prior 8/6512 + 10/1948 = 18/8460 is **not** re-proven.
- Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Catalog persist is still all accounts; scores for zero-deal logins stay unbuilt unless `/api/ops/resync` runs (`ListLoginsAsync`).
- `UserGetByGroup` cache fallback is a residual hole if request hard-fails **and** pump users were never filled.

Capability claim is proven from the connector file.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Outbound builder is only Logon:

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

In this compilation unit:

- Literal `(35, "D")` / `35=D` / `NewOrderSingle`: **0**
- `WriteAsync`: **1** (the Logon frame, L49)
- Socket: `using TcpClient` + `await using SslStream` — disposed after one `ReadAsync`
- Inbound `Extract(reply, "35")` accepts reply type `A` as LoggedOn; other types are Error. That is **not** an outbound NewOrderSingle.

Hosted hop: `CTraderFixLogonHostedService` L48–58 calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other public method exists on the class.

Grep `35=D|NewOrderSingle` inside `CTraderFixSession.cs`: **0 hits**.

**Residual (outside the assigned type; does not fail claim 4):**

Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (0 hits from `apps/` / DI). Gate at L43–47 refuses `live-*` host, `live.*` sender, and account `1369850`. **Not** the `CTraderFixSession` hop.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live composition **binds env `true`**. That disproves the claim.

Chain (no secret values):

1. `D:\Prop\.env` **L73** `REAL_COPY_EXECUTION_ENABLED=true` (boolean only).
2. API boot loads that file: `apps/api/Program.cs` L10 `EnvFile.FindAndLoad()`; L13 `AddEnvironmentVariables()`.
3. `EnvFile` candidate list includes `D:\Prop\.env` (`src/Mt5/Env/EnvFile.cs` L14) and writes process env (`L38`).
4. DI copies the env token onto runtime:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

5. Hosted logon **does not** assign `RealCopyEnabled = false`. It only logs the already-bound value (`CTraderFixLogonHostedService` L68–70 `RealCopyArmed={Armed}`). Grep of `src/` for `RealCopyEnabled =` = **one hit** (DI L41).
6. `/api/settings` echoes the runtime flag, not a hard-false literal:

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

`FEATURE_COPY_TRADING_ENABLED` is a **literal true** on the API (L77). Lab `.env` L106 is also `true`. That is a display/pipeline flag, not a sender.

What **is** still false (does not rescue claim 5):

| Surface | State |
|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` POCO default | `false` (`CTraderFixOptions.cs` L35). **Not** bound from env `REAL_COPY_EXECUTION_ENABLED`. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` L17 |
| `CopyTradingService.VenueReconciled` | `const false` L16 |
| Persist `AllowFixSend` | hardcoded `false` L306 |
| LIVE send branch | L312 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`; then still writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — no FIX write |
| `ExecutionIntent` writers | **0** (only `CountAsync` at L39) |
| FIX worker | reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log-only; stamps `Disconnected`) |
| Workers `Program.cs` | do **not** call `EnvFile.FindAndLoad()` |

Reports that still say DI/hosted pin `RealCopyEnabled=false` (`CREDENTIALS_AND_COPY_STATUS.md`, W500_RESEARCH_68/108, A014 “DI pins false”) are **STALE**.

Claim 5 as written fails. Runtime is **armed**; the sender is still missing.

---

## Capital risk

**NONE today** (`SAFE_BY_ABSENCE`).

- Assigned hop `CTraderFixSession` cannot emit `35=D`.
- Copy persist forces `AllowFixSend=false`; NOS + venue consts are false.
- Demo `Build("D")` is tools-only + demo-gated.
- This slot sent **zero** FIX orders and did **not** attach Manager.

Residual: next sender written against `LiveRuntimeStatus.RealCopyEnabled` would see **true** on the API host. That is why claim 5 is FAIL even though dest PnL is still $0.

---

## Verdict

**FAIL.** Claims 1–4 proven from live files. Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + DI L41 bind + no hosted re-pin. Copy hop still `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
