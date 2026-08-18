# W500_VERIFY_42 — Adversarial live-path verify (slot 42)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **42** |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved**.

This slot independently re-read the product files listed below. Prior swarm notes were treated as untrusted and were not used as proof.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160/160) seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` = **0** hits under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` first; if the list is empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync`, then dispose. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime bool. |

Overall **FAIL** because claim 5 cannot be proved — the opposite is in the files.

Destination capital risk this slot: **NONE** (`SAFE_BY_ABSENCE`). Copy hop still has no `NewOrderSingle` builder on `CTraderFixSession`. `CopyTradingService.NewOrderSingleImplemented` and `VenueReconciled` are `const false`. Persist writes `AllowFixSend = false`.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines, full file).

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

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

Both worker hosts also seed catalog, not demo:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

DI fail-closes Fake before any connector is registered:

```36:48:D:\Prop\src\Infrastructure\DependencyInjection.cs
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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on that path. `ProxyEnabled` for Starwave is hardcoded `false`.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). The only caller found this slot is `tests/Integration/SeedingAndStoreTests.cs` L25. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a real `SyncBrokerAsync`. That is a leftover worker scorer, **not** the API startup path.
- Older notes that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines, full file).

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

That is the assigned enumerator pair: request-first `GroupRequestArray("*")`, then cache `GroupTotal`/`GroupNext` if the request list is empty. The mask is `*`. There is no `MT5_GROUP_*` / plan-name filter on this walk.

Live ingest actually calls that path:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` drives `ingest.SyncCatalogAsync(connector.BrokerCode, …)` per registered Native connector (L56). Flag-blind: no `REAL_COPY` / `FEATURE_COPY` gate on fetch.

**Not proved this slot:** a live Manager attach that the request returned every ACL-visible group. Source capability is present. Completeness is **PASS_SOURCE**, not a measured census.

Older A001 (“zero `GroupRequestArray` hits under `src`”) is **stale**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file. `GetAccountsAsync(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }

            return byLogin.Values.ToList();
        }
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        ...
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

Primary = `UserRequestArray`. Hard-fail only then uses pump-cache `UserGetByGroup`. Empty array then `UserLogins` + `UserRequestByLogins`. Catalog ingest (`GetAccountsAsync(null)`) therefore asks for every group’s traders.

`_pumpEnabled` is set on connect but **never** gates these fetch methods.

**Not proved this slot:** live 8/6512 + 10/1948 = 18/8460 (that arithmetic is prior probe JSON; this slot did not re-attach). Source capability is present.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep of this file for `35=D`, `NewOrderSingle`, `(35, "D")`: **0**.

Only outbound MsgType constructor is Logon:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(
        string sender, string target, string senderSub, string targetSub,
        string username, string password, int seq)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
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
    }
```

`TryLogonAsync` writes that frame once (`ssl.WriteAsync` L49), reads one reply, then `using` disposes `TcpClient`/`SslStream`. No heartbeat loop, no `35=D`, no `35=F`/`35=G`, no order qty.

Hosted caller is `CTraderFixLogonHostedService` (QUOTE 5211 + TRADE 5212). It logs `NewOrderSingle still unimplemented` and does **not** construct an order.

Product `Fix.CTrader` literal `35=D` = **0**.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade` can `Build("D")` at L139 / L163 / L197. That class is **not** `CTraderFixSession`. It is demo-gated (refuses `live-*` / `live.` / account `1369850`) and is invoked from `tools/DemoFixTestTrade`, not from API DI / copy / ingest. Claim 4 is the assigned session type only.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove the opposite.

| Surface | What the file does |
|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted) |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; unused as a send gate) |
| `apps/api/Program.cs` L10–13 | `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` |
| `src/Mt5/Env/EnvFile.cs` L14, L38 | Candidates include `D:\Prop\.env`; `Environment.SetEnvironmentVariable(key, value)` |
| `DependencyInjection.cs` L41 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `CTraderFixLogonHostedService.cs` | **No** `RealCopyEnabled = false` write. L70 logs `RealCopyArmed={Armed}` from runtime. |
| `apps/api/Program.cs` L76 | `/api/settings` `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default still `false` (L35) — **unread** by the hosted logon / DI runtime bind |
| `apps/fix-worker/Worker.cs` L21 | Reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log-only; stamps Disconnected) |
| `appsettings*.json` under `apps/` | **0** `REAL_COPY` keys |

There is exactly **one** assignment of `RealCopyEnabled =` in `src`: the DI env bind. No later pin-false.

Architecture / README / `docs/architecture.md` still say the flag should be false. That is policy text, not the running bind.

Reports that claim DI/hosted still pin false (W500_68 / W500_108 / CREDENTIALS “forced false” / E038 hardcoded `/api/settings=false`) are **stale**.

Copy hop remains unable to send even when the flag is armed:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

```204:223:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                var rec = new RiskDecisionRecord
                {
                    ...
                    AllowFixSend = false,
                    DecidedAt = now
                };
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

So claim 5 **fails** (flag does not stay false). Live send is still **SAFE_BY_ABSENCE**, not flag-gated.

---

## Honesty / residuals

- This slot did **not** live-attach Manager or FIX. Claims 2–3 are source capability only.
- Dummy seed is off the API/worker **startup** path. Residual four-login scorer remains on `mt5-worker`.
- Next person to add a `35=D` builder would see `LiveRuntimeStatus.RealCopyEnabled==true` on an API process that loaded `.env`.
- `FEATURE_COPY_TRADING_ENABLED` is a literal `true` on `/api/settings` L77 and is not a send license.
- No secrets printed. No product source edited.

---

## Verdict

**FAIL.** Claims 1–4 proved from live files. Claim 5 disproved: `REAL_COPY_EXECUTION` does **not** stay false. Risk to destination capital **NONE** (`SAFE_BY_ABSENCE`).
