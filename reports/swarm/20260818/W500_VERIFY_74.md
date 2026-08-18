# W500_VERIFY_74 — Adversarial live-path verify (slot 74)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **74** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved**.

This slot independently re-read the product files listed below. Prior swarm reports (A002 DemoSeeder-on-boot, A001 “zero `GroupRequestArray`”, W500_68/108 “flag pinned false”, CREDENTIALS “forced false”) are treated as **stale** unless the current file still says that.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` = **0** hits under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count==0`, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")` at L96. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

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

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). `tests/Integration/SeedingAndStoreTests.cs` still calls `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup. Hosted ingest scores `ListLoginsWithDealsAsync` only (`LiveIngestHostedService.cs` L106).

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

```144:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Request-first: `GroupRequestArray("*")`. Cache walk `GroupTotal`/`GroupNext` only if the request list is empty. `_pumpEnabled` never gates this walk (pump is used only at `Connect`; fetch is request-first).

Live catalog hop (`DealIngestionService.SyncCatalogAsync` L45–48):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` per connector (L56). No plan-env filter (`MT5_GROUP_*` unread on this walk).

**Honesty limits:** this slot did **not** re-attach Achiever/Starwave. File proves the connector **can** enumerate via those APIs. It does **not** prove today’s live group count. Adversarial residual: if `GroupRequestArray` returns OK with a **non-empty subset**, the `GroupTotal` fallback is skipped.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file, `ReadAccountsForGroup`:

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

`GetAccountsCore(null)` walks every name from `GetGroupsCore()` then `ReadAccountsForGroup` (L189–213). Ingest uses `GetAccountsAsync(null)` (L48 and L62). No `Take`/`Skip` on the catalog walk.

**Honesty limits:** not re-attached this slot. File-capability only. Adversarial residual: if `UserRequestArray` returns OK with a **non-empty subset**, `UserLogins` is not called. Cache `UserGetByGroup` is used only on hard fail (not on empty).

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Grep in that file this slot: `35=D` = **0**, `NewOrderSingle` = **0**, `(35, "D")` = **0**.

Only outbound MsgType:

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

`TryLogonAsync`: one TCP+TLS connect, one `WriteAsync` of that Logon (L49), one `ReadAsync`, then `using` disposes `TcpClient`/`SslStream`. No heartbeat loop. No order builder.

Hosted copy hop (`CTraderFixLogonHostedService.cs` L48–58) calls this class twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” Persist updates existing `FixSessionState` rows only; it never writes an order.

Copy service cannot emit a ticket even if the flag is armed:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

Persist hardcodes `AllowFixSend = false` (L306). Live-send `if` requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L312) — the last three are unreachable today.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 exists. It is **not** `CTraderFixSession`. Callers: `tools/DemoFixTestTrade` only (0 hits in `apps/` or Infrastructure DI). Gated off live identity (`demo-` host, `demo.` sender, refuse `live-*` / `live.` / account `1369850`). Unused by copy.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show it is **armed**.

| Surface | What the file says now |
|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; not the send license) |
| `apps/api/Program.cs` L10 | `EnvFile.FindAndLoad()` — hard path includes `D:\Prop\.env` |
| `apps/api/Program.cs` L13 | `AddEnvironmentVariables()` |
| `EnvFile.cs` L38 | `Environment.SetEnvironmentVariable(key, value)` |
| `DependencyInjection.cs` L41 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `CTraderFixLogonHostedService` | **No** `RealCopyEnabled = false`. Reads `_runtime.RealCopyEnabled` for the log only (L70). |
| `Program.cs` `/api/settings` L76 | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` (echo, not a pin) |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | POCO default still `false` — **unread** by the hosted logon / copy service |
| `apps/fix-worker/Worker.cs` L21 | Reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log-only; stamps `Disconnected`) |

Product-C# assignment of `RealCopyEnabled =` this slot: **exactly one** (`DependencyInjection.cs` L41). There is no hosted re-pin. Architecture docs / README / `CREDENTIALS_AND_COPY_STATUS.md` (“false (forced)”) and W500_68/108 “pin-false” reports are **stale**.

Claim 5 is therefore **false**. Verdict for this claim: **FAIL**.

Copy still cannot send (`SAFE_BY_ABSENCE` in §4). That does **not** make claim 5 true. Next sender that keys off `LiveRuntimeStatus.RealCopyEnabled` would see **armed**.

---

## Risk to capital

**NONE** on the copy hop (`SAFE_BY_ABSENCE`).

Reasons, all from files this slot:

1. `CTraderFixSession` cannot build `35=D`.
2. `CopyTradingService.NewOrderSingleImplemented = false` and `VenueReconciled = false`.
3. Persist `AllowFixSend = false`.
4. Demo `Build("D")` is tools-only and demo-gated.
5. This slot did not attach and did not send.

If a later change added a `35=D` builder that honored `LiveRuntimeStatus.RealCopyEnabled`, dest risk would flip immediately — the env flag is already `true` and DI binds it.

---

## Files read this slot (live, not prior reports)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + settings echo |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested flag log-only |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Flag bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Startup seed class |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Exists; not API-called |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` load |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog `*` / all users |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted ingest |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Claim 4 |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Residual `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | No re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS unimplemented |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Runtime flag |
| `D:\Prop\.env` L73 / L106 | Boolean flags only |

---

## Verdict

**FAIL.** Claims 1–4 proven from live files (2–3 capability only; not re-attached). Claim 5 **disproven**: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Copy hop remains `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
