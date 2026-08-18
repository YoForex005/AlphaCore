# W500_VERIFY_89 — Adversarial live-path verify (slot 89)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 89 |
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
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; outbound `(35,"A")` only) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from this slot's file reads. Claim 5 is false on the API composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` flag echo (160 lines) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\appsettings.Development.json` | No flag |
| `D:\Prop\apps\api\Properties\launchSettings.json` | No flag |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/trader walks |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog caller `*` / `null` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Entire 135-line hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default unread by DI |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` → process env |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Send still unimplemented |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick only |
| `D:\Prop\.env` L73 + L106 | Flag booleans only |

Grep (this slot): `DemoSeeder` in `*.cs`; `GroupRequestArray`/`GroupTotal`; `UserRequestArray`/`UserLogins`; `35=D` / `NewOrderSingle` under `Fix.CTrader`; `REAL_COPY_EXECUTION` across `D:\Prop`; `FakeMt5` in DI.

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

`apps/api/Program.cs` is 160 lines. Token `DemoSeeder`: **0**. `using TraderIntelligence.Infrastructure.Seeding;` is present only so `BrokerCatalogSeed` resolves.

Both workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

Product C# callers of `DemoSeeder` (this slot's grep):

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 | Class definition |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | Test only |
| `D:\Prop\reports\swarm\20260818\_tmp_*` | Scratch evals, not hosts |

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

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

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `D:\Prop\src\Infrastructure` for `FakeMt5`: **0**. `DemoBrokerFactory.CreateDefault()` is called only from `DemoSeeder.SeedAsync` L126 (and tests/tmp).

**Residual (does not revive DemoSeeder as API startup):** class still exists; integration tests still call it. That is not `apps/api`.

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

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–46). Hosted `LiveIngestHostedService` L56 calls `SyncCatalogAsync`. `_pumpEnabled` does **not** gate `GetGroupsCore`. Connect still tries `PUMP_MODE_GROUPS|USERS|POSITIONS` first, then `PUMP_MODE_NONE` (`NativeMt5BrokerConnector` L89–110); fetch is request-first either way.

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

Order per group:

1. `UserRequestArray(gname, users)`
2. On hard fail (not OK / OK_NONE / NOTFOUND): `UserGetByGroup`
3. If still `users.Total() == 0`: `UserLogins` then `UserRequestByLogins`

Live catalog: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)`.

**Honesty limits:**

- This slot did **not** re-attach. Prior 8460-login census is **not** re-proven here.
- Same partial-array caveat as groups: non-empty `UserRequestArray` skips `UserLogins`.
- ALL traders is ALL users the manager can see, unioned by login across groups.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Outbound MsgType is Logon only:

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

Single `WriteAsync` (L49) of that logon; socket disposed on return. Reply parse accepts `35=A` as logon-ok; any other type is an error string (`Logon rejected 35={msgType}`).

Grep of this file for `35=D`, `(35, "D")`, `NewOrderSingle`: **0**.

Hosted caller `CTraderFixLogonHostedService` L48–58 calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) then persists status. No order send.

**Residual (does not put `35=D` inside `CTraderFixSession`):** sibling `CTraderFixDemoTestTrade.cs` / `CTraderFixDemoMatrix.cs` contain `Build("D")`. Those are not this type, not registered in DI, not called by the API/copy hop. Claim 4 is scoped to `CTraderFixSession`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim requires the live flag to **stay** false. Files prove the opposite on the API host.

**Load path (API):**

1. `apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` — first existing candidate, including hard path `D:\Prop\.env` (`EnvFile.cs` L8–15).
2. `EnvFile.Load` L28–38 writes every `KEY=VALUE` into process environment (no allow-list).
3. `Program.cs` L13 `builder.Configuration.AddEnvironmentVariables()`.
4. `AddTraderIntelligence` binds:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

**Lab value (boolean only, no secrets):**

- `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true`
- `D:\Prop\.env` L106 `FEATURE_COPY_TRADING_ENABLED=true`

Therefore `LiveRuntimeStatus.RealCopyEnabled` becomes **true** when the API loads that `.env`.

**No re-pin:**

- `CTraderFixLogonHostedService` reads `_runtime.RealCopyEnabled` for a log line (L69–70) and never assigns it false.
- Grep of product `*.cs` for `RealCopyEnabled =`: **only** DI L41.
- `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35) but is **unread** by DI / logon / copy.
- `apps/fix-worker/Worker.cs` L21 reads a **different** nested key `CTrader:RealCopyExecutionEnabled` (default false) for a log line only. That does not overwrite the API singleton.
- `apps/api/appsettings.json`, `appsettings.Development.json`, `launchSettings.json`, `docker-compose.yml`: no `REAL_COPY_EXECUTION_ENABLED` key to override `.env`.
- `/api/settings` echoes `runtime.RealCopyEnabled` (`Program.cs` L76), so the armed boolean is visible on the API.

Architecture docs (`docs/architecture.md`, `README.md`) still say `false`. Those docs are not the runtime. `reports/CREDENTIALS_AND_COPY_STATUS.md` “forced false” is **stale** versus current DI.

**Copy hop still cannot send (does not rescue claim 5):**

- `CopyTradingService.NewOrderSingleImplemented = false` (const L18)
- `VenueReconciled = false` (const L17)
- Persist `AllowFixSend = false` (L306)
- Send branch requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L312) and only sets a blocked status string
- Hosted copy ticks `GenerateShadowIntentsAsync` only (`CopyTradingHostedService` L28–32)
- `CTraderFixSession` still has no `35=D`

Claim 5 is about the **flag staying false**. The flag is env-bound `true`. **FAIL.**

---

## Risk to capital

**NONE** (`SAFE_BY_ABSENCE`).

Armed `RealCopyEnabled=true` is a status/license bit only. There is no NewOrderSingle sender on the product hop. Dest venue cannot receive a ticket from `CTraderFixSession`. This slot did not live-attach and did not send FIX.

If a sender is later wired without flipping `.env` L73 and without a hosted re-pin, the next hop would see runtime **armed**. That is residual, not current capital risk.

---

## Verdict

**FAIL.**

1–4 file-proven (2–3 capability only; no attach this slot). 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `EnvFile.FindAndLoad` + `AddEnvironmentVariables` + DI L41 bind + no logon re-pin.
