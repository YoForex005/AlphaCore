# W500_VERIFY_64 — Adversarial live-path verify (slot 64)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 64 |
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
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold. Claim 5 is false on the API composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` flag echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
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
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor request APIs |
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

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

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

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `D:\Prop\src\Infrastructure` for `FakeMt5`: **0**.

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25). That is not `apps/api`.
- Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005`, `A011`) are **superseded** by current `Program.cs`.

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

- L205 `virtual uint32_t  GroupTotal(void)=0;`
- L212 `virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;`

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

Sibling `CTraderFixDemoTestTrade.Build("D")` exists (L139 / L163 / L197). Gate at L43–47 refuses `live-*` host, `live.` sender, and account `1369850`. Callers are `tools/DemoFixTestTrade`, not API/DI/workers. **Not** the `CTraderFixSession` hop.

Copy persist still forces `AllowFixSend = false` (`CopyTradingService` L211) and `NewOrderSingleImplemented = false` (L17). Grep of `src` for `ExecutionIntents.Add` / `new ExecutionIntent`: **0**. Hosted copy loop only calls `GenerateShadowIntentsAsync` (`CopyTradingHostedService` L28).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove this claim. The live files prove the **opposite** on the API process.

### What the files actually do

| Surface | Value | File |
|---|---|---|
| Architecture policy | `false` (docs only) | `D:\Prop\docs\architecture.md` L20 |
| POCO default | `false` | `CTraderFixOptions.RealCopyExecutionEnabled` L35 |
| Lab env | **`true`** | `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` |
| Env loader | loads `.env` into process env, including hardcoded `D:\Prop\.env` | `EnvFile.FindAndLoad` L9–19; `Load` L38 `SetEnvironmentVariable` |
| API host | calls loader **before** DI | `apps/api/Program.cs` L10 + L13 `AddEnvironmentVariables()` |
| Runtime bind | **env `true` → `RealCopyEnabled=true`** | `DependencyInjection.cs` L41 |
| Hosted re-pin | **absent** | `CTraderFixLogonHostedService` never writes `RealCopyEnabled` (only logs `RealCopyArmed={Armed}` at L69) |
| Settings API | mirrors runtime | `apps/api/Program.cs` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |
| Feature copy display | literal `true` (unrelated to send) | `apps/api/Program.cs` L77; `.env` L106 `FEATURE_COPY_TRADING_ENABLED=true` |

DI (the live pin — not false):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of `D:\Prop\src` for `RealCopyEnabled =`: **exactly one** assignment (`DependencyInjection.cs` L41). There is no later assignment that forces false.

Therefore: if the API starts on this lab box with `D:\Prop\.env` present, **`LiveRuntimeStatus.RealCopyEnabled` is true**. `/api/settings` will advertise `REAL_COPY_EXECUTION_ENABLED: true`. Copy status `RealCopyArmed` follows the same field (`CopyTradingService.GetStatusAsync` L44).

`CTraderFixOptions.RealCopyExecutionEnabled = false` is **unread** by `CTraderFixSession` and by DI. It does not keep the runtime flag false.

`fix-worker\Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. That is not a pin of the API runtime flag. Workers also do **not** call `EnvFile.FindAndLoad` (API-only at `Program.cs` L10).

Stale “forced false” cites (`CREDENTIALS_AND_COPY_STATUS.md`, W500_68/108, A014 “DI pins false”) are **wrong against current `DependencyInjection.cs`**.

### What still blocks send (does not rescue claim 5)

Claim 5 is about the **flag staying false**. It does not. Send is still impossible:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- every `RiskDecisionRecord.AllowFixSend = false` (literal L211)
- `CTraderFixSession` has no NewOrderSingle builder
- hosted copy loop only calls `GenerateShadowIntentsAsync`
- 0 `ExecutionIntent` writers under `src`

So: **flag armed; ticket absent.** That is `SAFE_BY_ABSENCE`, not “stays false.”

---

## Cross-claim honesty

| Topic | This slot |
|---|---|
| Live census 18/8460/1984 | **Not re-attached.** Do not treat as measured here. |
| `/api/settings` live GET | **Not called** (would need a running host). Binding is proven from source + `.env` L73. |
| Passwords / FIX secrets | Not printed. |
| Product source | Not modified. |

---

## Verdict

**FAIL.**

Four of five assigned claims are proven from live files. The fifth is **disproven**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false. Lab `.env` L73 is `true`; `EnvFile` loads it; DI L41 copies it onto `LiveRuntimeStatus.RealCopyEnabled`; the logon host no longer re-pins.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` — no product `35=D` on `CTraderFixSession`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Next sender would see the runtime **armed**.
