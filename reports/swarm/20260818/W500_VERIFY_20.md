# W500_VERIFY_20 — Adversarial live-path verify (slot 20)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 20 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Secret values printed | **None** (quoted flag booleans only) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold. Claim 5 is false on the running composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.

---

## 1. DemoSeeder is not the API startup path — PASS

Read:

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`

API startup seed (only catalog writer on the host):

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

Both workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

DI fail-closes Fake/dummy before connectors exist:

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path.

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

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`D:\Prop\src\Application\Ingestion\DealIngestionService.cs` L45–46). `_pumpEnabled` does **not** gate `GetGroupsCore`.

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
- `WriteAsync`: **1** (the Logon frame)
- Socket: `using TcpClient` + `await using SslStream` — disposed after one `ReadAsync`
- Inbound `Extract(reply, "35")` accepts reply type `A` as LoggedOn; other types are Error. That is **not** an outbound NewOrderSingle.

Hosted hop: `CTraderFixLogonHostedService` L48–58 calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other method exists on the class.

**Residual (outside the assigned type; does not fail claim 4):**

Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 (`(35, msgType)` with `msgType=="D"`). Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (0 hits from `apps/` / DI). Gate at L43–47 refuses `live-*` host, `live.*` sender, and account `1369850`. **Not** the `CTraderFixSession` hop.

Copy persist still forces `AllowFixSend = false` (`CopyTradingService` L211) and `NewOrderSingleImplemented = false` (L17).

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
| Hosted re-pin | **absent** | `CTraderFixLogonHostedService` never writes `RealCopyEnabled` |
| Settings API | mirrors runtime | `apps/api/Program.cs` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |

DI (the live pin — not false):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Therefore: if the API starts on this lab box with `D:\Prop\.env` present, **`LiveRuntimeStatus.RealCopyEnabled` is true**. `/api/settings` will advertise `REAL_COPY_EXECUTION_ENABLED: true`. Copy status `RealCopyArmed` follows the same field (`CopyTradingService.GetStatusAsync` L44).

`CTraderFixOptions.RealCopyExecutionEnabled = false` is **unread** by `CTraderFixSession` and by DI. It does not keep the runtime flag false.

`fix-worker\Worker.cs` L21 reads a **different** key (`CTrader:RealCopyExecutionEnabled`, default false) and only logs. That is not a pin of the API runtime flag.

Stale “forced false” cites (`CREDENTIALS_AND_COPY_STATUS.md`, W500_68/108, A014 “DI pins false”) are **wrong against current `DependencyInjection.cs`**.

### What still blocks send (does not rescue claim 5)

Claim 5 is about the **flag staying false**. It does not. Send is still impossible:

- `CopyTradingService.NewOrderSingleImplemented = false` (const)
- `VenueReconciled = false` (const)
- every `RiskDecisionRecord.AllowFixSend = false` (literal, L211)
- `CTraderFixSession` has no NewOrderSingle builder
- hosted copy loop only calls `GenerateShadowIntentsAsync`

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

Do not treat this slot as authorization to send.
