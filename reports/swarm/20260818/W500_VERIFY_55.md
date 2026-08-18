# W500_VERIFY_55 — Adversarial live-path verify (slot 55)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **55** |
| Role | Adversarial verifier. Read live path files independently. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` = **0** under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; if the list stays empty, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads it. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved from the files (it is false on the API host).

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160/160).

Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`.

Independent greps this slot (product C# only):

| Scope | `DemoSeeder` hits |
|---|---|
| `D:\Prop\apps` | **0** |
| `D:\Prop\apps\api\Program.cs` | **0** |
| `D:\Prop\apps\mt5-worker\Program.cs` | **0** (seeds `BrokerCatalogSeed.EnsureAsync` L15) |
| `D:\Prop\apps\fix-worker\Program.cs` | **0** (seeds `BrokerCatalogSeed.EnsureAsync` L15) |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists (`public static class DemoSeeder` L14) |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | test-only `SeedAsync` |

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

Hosted ingest (`LiveIngestHostedService`) walks `registry.All()` and calls `DealIngestionService.SyncCatalogAsync` → `GetGroupsAsync` + `GetAccountsAsync(null)`. No seeder on that path.

**Residual (does not revive claim 1):**

- `DemoSeeder.cs` remains on disk for tests. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores leftover logins `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a worker scorer leftover, **not** API startup.
- Prior reports A002 / A005 / A010 / A011 that say API startup still calls `DemoSeeder` are **stale** against current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

```144:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

File-proven shape:

1. Request-first: `GroupRequestArray("*")` — manager-visible mask, not `MT5_GROUP_*` plan labels.
2. Fallback only if the request list is empty: pump-cache `GroupTotal` + `GroupNext`.
3. `_pumpEnabled` never gates this walk. Connect tries `PUMP_MODE_GROUPS|USERS|POSITIONS`, then `PUMP_MODE_NONE` (L89–110). Request APIs remain callable.

Live ingest uses this walk:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

**Honesty bound:** this slot did **not** re-attach Achiever/Starwave. The claim that is proved is **connector capability** (“can list via those APIs”), not a fresh census. Empty `GroupRequestArray` + empty cache would return `[]` without throwing.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `GetAccountsCore` + `ReadAccountsForGroup`.

`GetAccountsAsync(null)` walks **every group name** from `GetGroupsCore()` (L199–203), then per-group:

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

File-proven shape:

1. Primary: `UserRequestArray(group)` (network request).
2. Hard-fail only: pump-cache `UserGetByGroup`.
3. If still empty: `UserLogins` + `UserRequestByLogins`.

No `Take`/`Skip` on the catalog walk. Flag-blind (does not read `REAL_COPY`).

**Honesty bound:** completeness of “ALL manager traders on the live servers” is **not** re-proved this slot (no attach). Source capability is proved. Hosted **scoring** is narrower (`ListLoginsWithDealsAsync` only) — that does not shrink the catalog fetch.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

This compilation unit is two types: `CTraderFixSessionResult` + static `CTraderFixSession`. There is no order builder.

Outbound MsgType is hardcoded Logon:

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

Wire I/O (`TryLogonAsync` L33–50):

- one `TcpClient` + `SslStream` (TLS 1.2|1.3)
- one `WriteAsync` of `BuildLogon`
- one `ReadAsync`
- `using` disposes both sockets

Inbound `35` is parsed only to accept Logon (`msgType == "A"`) or return Error. No NewOrderSingle encode path. Grep of this file for `35=D` / `NewOrderSingle` / `(35, "D")` = **0**.

Hosted caller (`CTraderFixLogonHostedService` L48–58) invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” Persist never sets `AllowFixSend`.

Copy hop cannot emit a ticket even if the flag is armed:

```16:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

```211:211:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

```217:223:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139/L163/L197. That class is **not** `CTraderFixSession`. Callers: `tools/DemoFixTestTrade` only (0 hits in API/workers/DI). It refuses `live-*` / `live.*` / account `1369850`. Not on the copy hop.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove the opposite on the API host.

| Surface | Measured | Stays false? |
|---|---|---|
| Lab `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **No** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; unused by DI for REAL_COPY) | n/a |
| API boot | `EnvFile.FindAndLoad()` (`apps/api/Program.cs` L10) loads `D:\Prop\.env` into process env | loads the `true` |
| Config | `builder.Configuration.AddEnvironmentVariables()` L13 | binds it |
| DI | `LiveRuntimeStatus.RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` L41 | **armed** |
| Hosted FIX logon | reads `_runtime.RealCopyEnabled` for the log line only; **no** `RealCopyEnabled = false` assignment anywhere except the DI bind | **no re-pin** |
| `/api/settings` | `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` L76 | echoes armed |
| `/api/health` | `realCopyEnabled = runtime.RealCopyEnabled` L55 | echoes armed |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default `false` (L35) | unread by session / not bound from this env key |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — **different key**; default false; log-only; still stamps `Disconnected` | split-brain |
| Architecture `docs/architecture.md` L20 | documents `=false` | docs ≠ runtime |

Grep of product `*.cs` for `RealCopyEnabled =` is **one** assignment: DI L41. There is no hosted force-false.

Copy still cannot send (`SAFE_BY_ABSENCE`): no `35=D` builder on the hosted session, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`, `VenueReconciled=false`. That does **not** make claim 5 true. The runtime flag is armed. The next sender wired to `LiveRuntimeStatus.RealCopyEnabled` would see `true`.

Reports that still claim DI/hosted pin the flag false (W500_68 / W500_108 / CREDENTIALS “forced false”) are **stale**.

---

## Risk to capital

**NONE** today (`SAFE_BY_ABSENCE`).

Reasons (file-proven, this slot):

1. `CTraderFixSession` cannot emit `35=D`.
2. Copy service const `NewOrderSingleImplemented=false` and hard-writes `AllowFixSend=false`.
3. Hosted FIX is a one-shot Logon then dispose.
4. This slot did not send and did not flip the env.

Residual if a sender is later added while `.env` stays `true`: the flag is already armed on the API host. Do not treat claim 5 as a live safety pin.

---

## What this slot did not do

- Did not attach Manager (no fresh 18/8460 proof).
- Did not GET localhost `:5000` (not required; claim 5 is file-proved without HTTP).
- Did not edit product source.
- Did not print secrets, passwords, connection strings, or FIX credentials.
- Did not invoke `tools/DemoFixTestTrade`.

---

## Verdict

**FAIL.** Claims 1–4 proved from live files. Claim 5 **disproved**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false (`.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 + no hosted re-pin). Dest capital risk remains **NONE** only because the send path is absent.
