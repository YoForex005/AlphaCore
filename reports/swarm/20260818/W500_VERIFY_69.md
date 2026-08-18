# W500_VERIFY_69 — Adversarial live-path verify (slot 69)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **69** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL any assigned claim that cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files). Claims 2–3 are source-capability only (this slot did not re-attach Manager).

Prior swarm notes (A002 “API still calls DemoSeeder”, A001 “zero `GroupRequestArray`”, slots that said the flag is hard-pinned false / logon re-pins false) are treated as **stale** unless the current file still says that. This slot re-read the product files listed below.

---

## Assigned claims

| # | Claim | Verdict | Proof from this slot’s file read |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `D:\Prop\apps\api\Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` = **0** hits under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count==0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Catalog `GetAccountsAsync(null)` walks every group. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")` at L96. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. **Zero** other C# writers of `RealCopyEnabled`. Hosted logon does **not** re-pin false. |

Overall **FAIL** because claim 5 cannot be proved — the live files prove the opposite.

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

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`:
  - `apps/api/Program.cs` L156
  - `apps/mt5-worker/Program.cs` L15
  - `apps/fix-worker/Program.cs` L15

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (class L14, `SeedAsync` L16). Only remaining product-tree caller is the test `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25. That is **not** API startup.

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. Dual-AND `HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` via `IsSecret`.

**Residual (does not revive claim 1):** seeder file remains for tests; `DemoSeeder` still composes `DemoBrokerFactory.CreateDefault()` (Fake tape + logins 10001/10002/10003/99001) if a test calls it.

---

## 2. Native can list groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). This slot did **not** live-attach Manager, so “all” is a **source-capability** claim, not a census.

`GetGroupsAsync` → `GetGroupsCore`:

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

Live ingest uses this path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`, L48 `GetAccountsAsync(null)`. `LiveIngestHostedService` L56 calls `SyncCatalogAsync` for every registered connector.

**Adversarial caveat (does not fail the capability claim):** if `GroupRequestArray("*")` returns OK/NONE with a **non-empty partial** set, `GroupTotal` is skipped. Completeness then depends on the Manager `*` contract. This slot did not re-probe.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file. `GetAccountsAsync(null)` → `GetAccountsCore` walks **every** group from `GetGroupsCore` (L199–203), then `ReadAccountsForGroup`:

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

Catalog hop is flag-blind (`GetAccountsAsync(null)` in `DealIngestionService` L48 and L62).

**Adversarial caveat:** if `UserRequestArray` returns a **non-empty partial** array, `UserLogins` is skipped. `UserGetByGroup` is pump-cache fallback on hard fail only. Completeness not re-attached this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep of that file for `35` / `NewOrderSingle` / `"D"`:

| Line | What it is |
|---|---|
| L55 | inbound `Extract(reply, "35")` (read, not send) |
| L73 | error string `Logon rejected 35={msgType}` |
| L96 | outbound `(35, "A")` Logon **only** |

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

One `WriteAsync` (L49). `using` TcpClient + `await using` SslStream — sockets disposed. No `NewOrderSingle`. No tag 38/40/54 order fields.

Hosted path (`CTraderFixLogonHostedService` L48–58) calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and then **returns**. It never keeps a TRADE socket for later D.

Copy hop still cannot emit a ticket even if the flag is armed:

- `CopyTradingService.NewOrderSingleImplemented = false` (L18)
- persist `AllowFixSend = false` (L306) regardless of `RiskEngine.allowSend`
- live-send branch (L312) still requires `NewOrderSingleImplemented && VenueReconciled` (`VenueReconciled = false` L17)
- `CopyTradingHostedService` only ticks roster + `GenerateShadowIntentsAsync`

**Residual (does not fail claim 4):** sibling **off-hop** helpers `CTraderFixDemoTestTrade.cs` (`Build("D")` L139/L163/L197) and `CTraderFixDemoMatrix.cs` L93 can emit MsgType D. They are **not** `CTraderFixSession`, not registered in DI, not called from API/copy. Claim is specifically `CTraderFixSession`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The live key is `REAL_COPY_EXECUTION_ENABLED`. It does **not** stay false.

Chain this slot re-read:

1. `D:\Prop\.env` **L73** `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted). L106 `FEATURE_COPY_TRADING_ENABLED=true`.
2. API `Program.cs` L10 `EnvFile.FindAndLoad()` — `EnvFile` L14 hard-includes `D:\Prop\.env` and L38 `Environment.SetEnvironmentVariable`.
3. API L13 `builder.Configuration.AddEnvironmentVariables()`.
4. `AddTraderIntelligence` DI L41:
   `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)`
5. Grep `RealCopyEnabled =` across `*.cs` = **exactly one writer** (DI L41). `CTraderFixLogonHostedService` L70 **logs** `_runtime.RealCopyEnabled`; it does **not** assign false.
6. `/api/settings` L76 echoes `runtime.RealCopyEnabled` under key `REAL_COPY_EXECUTION_ENABLED`. `/api/health` L55 exposes `realCopyEnabled`.
7. `apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. `FeatureFlags.LiveCopyEnabled=false` (L47) is a **different unused JSON path** — DI does not bind it.
8. `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (L35) but is **not** what the API host uses (no `IOptions<CTraderFixOptions>` bind on the logon/copy hop). `CTraderQuoteService` is not in DI.

`fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` default **false** — a **split key**, not the API runtime. That does not re-pin the API singleton.

So claim 5 is **disproved**: on the API live path the flag is env-true and stays armed for the process lifetime.

---

## Risk to capital

**NONE** on the current copy hop (`SAFE_BY_ABSENCE`):

| Gate | Live file |
|---|---|
| No NewOrderSingle sender | `CopyTradingService.NewOrderSingleImplemented = false` L18 |
| Venue unreconciled | `VenueReconciled = false` L17 |
| Persist never arms send | `AllowFixSend = false` L306 |
| Hosted FIX is logon-only | `CTraderFixSession` `(35,"A")` then dispose |
| Copy host writes shadows | `CopyTradingHostedService` L28–29 |

`RiskEngine` **can** set in-memory `AllowFixSend=true` when `RealExecutionEnabled && Reconciled && VenueHealthy` (L147–150). Copy persist **overwrites** that to false. Next sender that reads `LiveRuntimeStatus.RealCopyEnabled` would see **armed**. That is a residual, not a live ticket.

This slot did not flip `.env`, did not send `35=D`, and did not attach Manager.

---

## Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\.env` (boolean keys only)

---

## Verdict

**FAIL.** Claims 1 and 4 proved from files. Claims 2–3 proved as source capability (`GroupRequestArray("*")`/`GroupTotal`; `UserRequestArray`/`UserLogins`); not re-attached. Claim 5 **disproved**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + DI L41 bind + no re-pin. Copy hop remains `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
