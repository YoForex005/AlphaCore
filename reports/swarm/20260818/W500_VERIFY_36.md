# W500_VERIFY_36 — Adversarial live-path verify (slot 36)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **36** |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted flag booleans only) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from **this** read of live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule applied: **FAIL if any claim cannot be proven from the file.** Prior swarm notes (`W500_VERIFY_*`, `A002`, `CREDENTIALS_AND_COPY_STATUS.md`) are **not** evidence. Census 18/8460 is **not** re-measured here.

## Scoreboard

| # | Claim | Proven from file this slot? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes — API seed is `BrokerCatalogSeed.EnsureAsync` only; `apps/**` has 0 `DemoSeeder` tokens | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes — `GetGroupsCore` request-first `*` then `GroupTotal`/`GroupNext` | **PASS** (capability; not re-attached) |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes — `ReadAccountsForGroup` request-first then `UserLogins` | **PASS** (capability; not re-attached) |
| 4 | `CTraderFixSession` has no `35=D` | Yes — entire file 135/135; only outbound MsgType is `(35, "A")` | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — live files prove the opposite on the API host** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 are file-proven. Claim 5 is **disproven**.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket. That does **not** rescue claim 5.

---

## 1. DemoSeeder is not the API startup path — PASS

Files read this slot:

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`

API boot seed (the only catalog writer on the API host):

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`Program.cs` contains **zero** `DemoSeeder` identifiers. Grep of `D:\Prop\apps` (`*.cs`) for `DemoSeeder`: **0 hits**.

Workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

DI fail-closes Fake/dummy **before** any connector is registered:

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path.

`DemoSeeder` still exists (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14) and still rebuilds `{10001,10002,10003,99001}` via `DemoBrokerFactory.CreateDefault()`. Product callers of `DemoSeeder.SeedAsync` this slot:

| Caller | Role |
|---|---|
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | tests |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | report scratch, not `apps/api` |

Those are **not** the API startup path.

**Residual (does not revive claim 1 as FAIL):** `D:\Prop\apps\mt5-worker\Worker.cs` L31 still scores `{10001,10002,10003,99001}` in its own loop. That is leftover dummy login scoring on the **worker**, not API seed. Hosted ingest on the API process scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005`, `A011`) are **superseded** by current `apps/api/Program.cs`.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

File read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–186 (full connector 458 lines).

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

Fallback when the request list is empty (pump-cache walk):

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

Vendor surface (`D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`): `GroupTotal` (L205) and `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` (L212).

Live catalog uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`DealIngestionService.cs` L45–46). `_pumpEnabled` does **not** gate `GetGroupsCore`. `GetAccountsAsync(null)` also calls `GetGroupsCore()` to enumerate every group name (L201–202).

**Honesty limits (not a FAIL of the capability claim):**

- This slot did **not** attach to Achiever/Starwave. Prior 18-group census is **not** re-proven here.
- If `GroupRequestArray("*")` returns `OK`/`OK_NONE` with a **non-empty but ACL-incomplete** array, the `GroupTotal` fallback is skipped. Completeness is then “whatever the manager ACL returns,” which is the correct Manager-API meaning of ALL.
- Empty request + empty cache → empty list, no throw.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

File read: `NativeMt5BrokerConnector.GetAccountsCore` L189–214 + `ReadAccountsForGroup` L216–271.

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
- `UserGetByGroup` cache fallback is a residual hole if request hard-fails **and** pump users were never filled (`_pumpEnabled=false` after the `PUMP_MODE_NONE` reconnect at L101–110).

Capability claim is proven from the connector file.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

Grep of that compilation unit for outbound MsgType tokens: only three `35` hits — inbound `Extract(reply, "35")` (L55), reject text (L73), and the Logon builder `(35, "A")` (L96). **Zero** `(35, "D")`, `35=D`, `NewOrderSingle`, or `Build("D")`.

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

Hosted hop: `CTraderFixLogonHostedService` L48–58 calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other public method exists on the class.

**Residual (outside the assigned type; does not fail claim 4):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197.
- Sibling `CTraderFixDemoMatrix.SendD` → `Build("D")` at L93.
- Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (0 hits from `apps/` / DI).
- Gate at `CTraderFixDemoTestTrade` L43–47 refuses host not starting `demo-`, sender not starting `demo.`, any `live.` / `live-`, and account `1369850`.

Copy persist still forces `AllowFixSend = false` (`CopyTradingService` L211) and `NewOrderSingleImplemented = false` (L17). Hosted copy loop only calls `GenerateShadowIntentsAsync` (`CopyTradingHostedService` L28).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove this claim. The live files prove the **opposite** on the API process.

The **only** C# assignment of `LiveRuntimeStatus.RealCopyEnabled` in the tree is DI binding the env string:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of `D:\Prop` `*.cs` for `RealCopyEnabled =`: **that one line**. There is no hosted re-pin.

### What the files actually do

| Surface | Value | File |
|---|---|---|
| Architecture / docs policy | `false` (docs only; not executed) | `docs/architecture.md` L20; `README.md` L28 |
| POCO default | `false` | `CTraderFixOptions.RealCopyExecutionEnabled` L35 |
| Lab env | **`true`** | `D:\Prop\.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` |
| Sibling flag (not this claim) | `true` | `D:\Prop\.env` L106 `FEATURE_COPY_TRADING_ENABLED=true` |
| Env loader | loads `.env` into process env, including hardcoded `D:\Prop\.env` | `EnvFile.FindAndLoad` L9–19; `Load` L38 `SetEnvironmentVariable` |
| API host | calls loader **before** DI | `apps/api/Program.cs` L10 + L13 `AddEnvironmentVariables()` |
| Runtime bind | **env `true` → `RealCopyEnabled=true`** | `DependencyInjection.cs` L41 |
| Hosted re-pin | **absent** | `CTraderFixLogonHostedService` never writes `RealCopyEnabled` (only logs it at L69) |
| launchSettings | no `REAL_COPY_*` key | `apps/api/Properties/launchSettings.json` |
| appsettings.json | `FeatureFlags.LiveCopyEnabled=false` — **different unread key** | `apps/api/appsettings.json` L46 |
| Settings API | mirrors runtime | `apps/api/Program.cs` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |
| Copy status | `RealCopyArmed: _runtime.RealCopyEnabled` | `CopyTradingService.GetStatusAsync` L44 |

Therefore: if the API starts on this lab box with `D:\Prop\.env` present, **`LiveRuntimeStatus.RealCopyEnabled` is true**. `/api/settings` will advertise `REAL_COPY_EXECUTION_ENABLED: true`. Copy status `RealCopyArmed` follows the same field.

`CTraderFixOptions.RealCopyExecutionEnabled = false` is **unread** by `CTraderFixSession` and by DI. It does not keep the runtime flag false.

`fix-worker\Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. That is not a pin of the API runtime flag. Workers also do **not** call `EnvFile.FindAndLoad()`; the API does.

Stale “forced false” cites (`CREDENTIALS_AND_COPY_STATUS.md`, W500_68/108, A014 “DI pins false”) are **wrong against current `DependencyInjection.cs`**.

This slot did **not** GET a running `/api/settings`. Binding is proven from source + `.env` L73 + API `FindAndLoad`. That is enough to **disprove** “stays false.” A missing live GET would be FAIL-unproven; here the opposite is proven.

### What still blocks send (does not rescue claim 5)

Claim 5 is about the **flag staying false**. It does not. Send is still impossible:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- every `RiskDecisionRecord.AllowFixSend = false` (literal, L211)
- `CTraderFixSession` has no NewOrderSingle builder
- hosted copy loop only calls `GenerateShadowIntentsAsync`
- `BuildBlockers` still lists “No NewOrderSingle sender — SAFE_BY_ABSENCE” regardless of the flag (L306–307)

So: **flag armed; ticket absent.** That is `SAFE_BY_ABSENCE`, not “stays false.” Next sender would see the runtime **armed**.

---

## Cross-claim honesty

| Topic | This slot |
|---|---|
| Live census 18/8460/1984 | **Not re-attached.** Do not treat as measured here. |
| `/api/settings` live GET | **Not called** (would need a running host). Binding is proven from source + `.env` L73. |
| Passwords / FIX secrets / proxy auth | Not printed. `.env` quoted only as `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`. |
| Product source | Not modified. |
| Claim 5 wording | “stays false” is a **policy** claim. Live composition **does not** enforce it. |

---

## Verdict

**FAIL.**

Four of five assigned claims are proven from live files. The fifth is **disproven**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false. Lab `.env` L73 is `true`; `EnvFile` loads it on the API; DI L41 copies it onto `LiveRuntimeStatus.RealCopyEnabled`; the logon host no longer re-pins.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` — no product `35=D` on `CTraderFixSession`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Next sender would see the runtime **armed**.

Do not treat this slot as authorization to send.
