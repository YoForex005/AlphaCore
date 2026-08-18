# W500_VERIFY_33 — Adversarial live-path verify (slot 33)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 33 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Secret values printed | **None** (quoted flag booleans only: `REAL_COPY_EXECUTION_ENABLED=true`, `FEATURE_COPY_TRADING_ENABLED=true`) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence. This slot re-read the current files.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold on current files. Claim 5 is false on the running API composition: lab `.env` L73 is `true`, `EnvFile.FindAndLoad` injects it, DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`, and `CTraderFixLogonHostedService` never re-pins false.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product copy hop still cannot emit a ticket. The flag being armed is a license bit, not a sender.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot:

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`

API startup seed (the only catalog writer on the host):

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
```

`using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`, not `DemoSeeder`. Token `DemoSeeder` does not appear in `Program.cs`.

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

Both workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

DI fail-closes Fake/dummy **before** connectors exist:

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

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path. The only product `CreateDefault()` call is inside `DemoSeeder` L126 (`DemoBrokerFactory.CreateDefault()`), which is off the API boot path.

`BrokerCatalogSeed.EnsureAsync` writes broker stubs + XAUUSD + kill-switch + FIX rows at `Disconnected`. It does **not** ingest FakeMt5 or score logins `10001`/`10002`/`10003`/`99001`.

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product C# caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25) plus report-scratch `_tmp_*` programs. Those are not `apps/api`.
- `mt5-worker\Worker.cs` L31 still scores `{10001,10002,10003,99001}` in its own loop. That is a leftover dummy login set on the **worker**, not API seed. Hosted ingest on the API process scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005`, `A011`) are **superseded** by current `Program.cs`.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–186.

Primary (network request, mask `*` = all groups the manager may see):

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

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–46). `_pumpEnabled` does **not** gate `GetGroupsCore`.

**Honesty limits (not a FAIL of the capability claim):**

- This slot did **not** attach to Achiever/Starwave. Any prior 18-group census is **not** re-proven here.
- If `GroupRequestArray("*")` returns `OK`/`OK_NONE` with a **non-empty but ACL-incomplete** array, the `GroupTotal` fallback is skipped. Completeness is then “whatever the manager ACL returns,” which is the correct Manager-API meaning of ALL.
- Empty request + empty cache → empty list, no throw.
- Claim 2 is a **file-proven capability**, not a live census.

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

- This slot did **not** re-count logins. Any prior 8/6512 + 10/1948 = 18/8460 figure is **not** re-proven here.
- Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Catalog persist is still all accounts; scores for zero-deal logins stay unbuilt unless `/api/ops/resync` runs (`ListLoginsAsync`).
- If `UserRequestArray` returns a **non-empty but incomplete** array, `UserLogins` is skipped (`users.Total() == 0` is the only fallback gate). Same ACL meaning as groups: ALL = what the manager returns for that group mask.
- `UserGetByGroup` cache fallback is a residual hole if request hard-fails **and** pump users were never filled.

Capability claim is proven from the connector file.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of this compilation unit for `35=D`, `(35, "D")`, `Build("D")`, `NewOrderSingle`: **0**.

Tag `35` occurs only three times, all logon:

- L55 inbound `Extract(reply, "35")`
- L73 error string `Logon rejected 35={msgType}`
- L96 outbound `(35, "A")`

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
- `WriteAsync`: **1** (the Logon frame at L49)
- Socket: `using TcpClient` + `await using SslStream` — disposed after one `ReadAsync`
- Inbound `Extract(reply, "35")` accepts reply type `A` as LoggedOn; other types are Error. That is **not** an outbound NewOrderSingle.

Hosted hop: `CTraderFixLogonHostedService` L48–58 calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other public method exists on `CTraderFixSession`.

**Residual (outside the assigned type; does not fail claim 4):**

Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (0 hits from `apps/` / DI). Gate at L43–47 refuses non-`demo-` host, non-`demo.` sender, `live-` / `live.` identity, and account `1369850`. **Not** the `CTraderFixSession` hop.

Sibling `CTraderFixDemoMatrix` L93 also `Build("D")` — same tools-only matrix, not wired to copy.

Copy persist still forces `AllowFixSend = false` (`CopyTradingService` L211) and `NewOrderSingleImplemented = false` (L17 const). Hosted copy loop only calls `GenerateShadowIntentsAsync` (`CopyTradingHostedService` L28).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove this claim. The live files prove the **opposite** on the API process.

### What the files actually do

| Surface | Value | File |
|---|---|---|
| Architecture policy | `false` (docs only) | `D:\Prop\docs\architecture.md` L20 |
| POCO default | `false` | `CTraderFixOptions.RealCopyExecutionEnabled` L35 |
| Committed appsettings | key **absent** | `apps/api/appsettings.json`, `appsettings.Development.json` |
| launchSettings | key **absent** | `apps/api/Properties/launchSettings.json` |
| docker-compose | key **absent** | `D:\Prop\docker-compose.yml` |
| Lab env | **`true`** | `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` |
| Sibling flag | **`true`** (unused by DI for copy send) | `D:\Prop\.env` L106 `FEATURE_COPY_TRADING_ENABLED=true` |
| Env loader | loads `.env` into process env, including hardcoded `D:\Prop\.env` | `EnvFile.FindAndLoad` L9–19; `Load` L38 `SetEnvironmentVariable` |
| API host | calls loader **before** DI | `apps/api/Program.cs` L10 + L13 `AddEnvironmentVariables()` |
| Runtime bind | **env `true` → `RealCopyEnabled=true`** | `DependencyInjection.cs` L41 |
| Hosted re-pin | **absent** | `CTraderFixLogonHostedService` never writes `RealCopyEnabled` |
| Settings API | mirrors runtime | `apps/api/Program.cs` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |
| Health API | mirrors runtime | `apps/api/Program.cs` L55 `realCopyEnabled = runtime.RealCopyEnabled` |
| Grep `RealCopyEnabled =` in `*.cs` | **one write** (DI L41) | no other assignment |

DI (the live pin — not false):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`EnvFile.FindAndLoad` candidate list **includes the literal** `D:\Prop\.env` (L14). API `Program.cs` L10 runs that loader, then L13 `AddEnvironmentVariables()`. Therefore: if the API starts on this lab box with `D:\Prop\.env` present, **`LiveRuntimeStatus.RealCopyEnabled` is true**. `/api/settings` will advertise `REAL_COPY_EXECUTION_ENABLED: true`. Copy status `RealCopyArmed` follows the same field (`CopyTradingService.GetStatusAsync` L44).

`CTraderFixOptions.RealCopyExecutionEnabled = false` is **unread** by `CTraderFixSession` and by DI. It does not keep the runtime flag false.

`CTraderFixLogonHostedService` logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled` (L69–70) and does **not** overwrite it. Older reports that claimed a hosted re-pin to false are **stale**.

`fix-worker\Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. That is not a pin of the API runtime flag. Workers do **not** call `EnvFile.FindAndLoad`; their `AddTraderIntelligence` bind depends on process env, not the hardcoded `.env` path — still irrelevant to the API host, which does load it.

`reports/CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` | **false** (forced)” is **stale** against current DI.

Claim 5 as written (“stays false”) is therefore **disproven**. Default-when-unset is still false (string compare to `"true"`). The live lab file sets it true and the API binds it.

---

## Capital / send gate (why FAIL ≠ money at risk)

Even with `RealCopyEnabled=true`, this slot can still prove **no product NewOrderSingle** on the copy hop:

| Gate | File proof |
|---|---|
| `CTraderFixSession` outbound MsgType | only `(35, "A")` |
| `NewOrderSingleImplemented` | `const bool` **false** (`CopyTradingService` L17) |
| `VenueReconciled` | `const bool` **false** (L16) |
| Persist `AllowFixSend` | **hardcoded false** (L211) even if risk engine would approve |
| Live-send branch | requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L217) — then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED`; **no socket write** |
| Hosted copy | `GenerateShadowIntentsAsync` only (`CopyTradingHostedService` L28) |
| Demo `Build("D")` | tools CLI, demo-gated, not in DI |

`SAFE_BY_ABSENCE` is the capital argument. It is **not** a proof that `REAL_COPY_EXECUTION` stays false.

Residual: the next person who adds a `35=D` sender will see the API runtime **already armed**. Do not treat claim 5 as PASS.

---

## Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\appsettings.Development.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs` (header)
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs` (header)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (header + Build("D") sites)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (API names/lines only)
- `D:\Prop\.env` L73 and L106 **keys/booleans only** (no passwords, hosts, or other values dumped)

## Verdict

**FAIL** — claim 5 disproven from live files. Claims 1–4 proven from files. Risk to capital **NONE** (`SAFE_BY_ABSENCE`).
