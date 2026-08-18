# W500_VERIFY_86 — Adversarial live-path verify (slot 86)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **86** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved**.

This slot re-read the product files itself. Prior W500_VERIFY_* verdicts were not used as proof.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160/160) seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync`; sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime bit. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

Risk to destination capital: **NONE** (`SAFE_BY_ABSENCE`). Arming the flag does not create a sender.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot (full files):

- `D:\Prop\apps\api\Program.cs` (160/160)
- `D:\Prop\apps\mt5-worker\Program.cs` (18/18)
- `D:\Prop\apps\fix-worker\Program.cs` (18/18)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (header + `EnsureAsync` start)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (declaration only; not invoked by hosts)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (62/62)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94/94)

API startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

Both workers use the same seed, not DemoSeeder:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\src` product (non-test) = **1** class declaration (`Seeding\DemoSeeder.cs` L14)
- Product host `Program.cs` files (API + both workers) have **0** `DemoSeeder` / `FakeMt5` tokens
- Tests: `tests\Integration\SeedingAndStoreTests.cs` still calls `DemoSeeder.SeedAsync`. **API process does not.**

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed.EnsureAsync` writes broker catalog + instrument + kill-switch + two FIX rows already `Disconnected`. It does **not** score demo logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists for tests.
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

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

Hosted ingest uses that walk, flag-blind:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetGroupsAsync` → `GetGroupsCore`. Mask `"*"` is ALL manager-visible groups. Empty `GroupRequestArray` falls back to pump-cache `GroupTotal`/`GroupNext`. `_pumpEnabled` does **not** gate the request path.

This slot **did not re-attach** Manager. Census 18/8460 cited by other waves is **not re-measured here**. Verdict is file-capability only: **PASS_SOURCE**.

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
            // ...
        }
    }

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

Primary path is `UserRequestArray`. Cache `UserGetByGroup` only on hard fail. Empty array → `UserLogins` + `UserRequestByLogins`. Dedup is by login dictionary.

This slot **did not re-attach**. Completeness of “all manager-visible traders” is source-wired, not re-counted. **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep this file for `35=D` / `NewOrderSingle` / `(35, "D")` / `Build("D")` = **0**.

Hits in this file: `WriteAsync` at L49; `(35, "A")` at L96. Nothing else.

Only outbound MsgType:

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

`TryLogonAsync` (L21–87): one `TcpClient` + `SslStream`, one `WriteAsync` of that Logon, one `ReadAsync`, then `using` disposes both sockets. No heartbeat loop. No NewOrderSingle. No tag 38 / tag 11 builder.

Hosted caller (`CTraderFixLogonHostedService.cs` L48–58) invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” It never writes a D.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` exists (L139 / L163 / L197). Grep of `apps/` and `src/Infrastructure` for `CTraderFixDemoTestTrade` = **0**. Only `tools/DemoFixTestTrade` calls it. **Not on the copy hop. Not in DI.** Claim 4 is scoped to `CTraderFixSession`.

Copy hop still cannot send:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L18)
- `VenueReconciled = false` (const L17)
- persist `AllowFixSend = false` (L306), even when `decision.AllowFixSend` is later AND-gated with LIVE + NOS + reconciled (L312)
- `CTraderFixOptions.RealCopyExecutionEnabled` POCO default remains `false` and is **unread** by `CTraderFixSession`

Claim 4 is **PASS** for the assigned type.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show the opposite on the API host.

Chain this slot:

1. `D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret printed). L106: `FEATURE_COPY_TRADING_ENABLED=true`.
2. API boot loads that file: `EnvFile.FindAndLoad()` at `apps/api/Program.cs` L10, including hard path `D:\Prop\.env` (`EnvFile.cs` L14). Then `AddEnvironmentVariables()` at L13.
3. DI binds the env string onto process state:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. `CTraderFixLogonHostedService` (112/112) **does not** assign `_runtime.RealCopyEnabled = false`. It only logs `RealCopyArmed={Armed}` (L68–70). Older “hosted pin-false” reports (W500_68 / W500_108 / A015) are **stale**.
5. `/api/settings` echoes the runtime bit, not a hard-coded false:

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

6. `/api/health` also reports `realCopyEnabled = runtime.RealCopyEnabled` (L55). Dashboard `EfDashboardQueries` passes `_runtime.RealCopyEnabled` (L52).
7. `CTraderFixOptions.RealCopyExecutionEnabled` still defaults **false** (L35) and `apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. That does **not** rescue claim 5: API configuration is env-driven after `FindAndLoad`.
8. `apps/fix-worker/Worker.cs` L21 reads **`CTrader:RealCopyExecutionEnabled`** (nested), default `false`. That worker flag is a **different key** and only logs; it does not re-pin the API `LiveRuntimeStatus`.

Therefore the claim “`REAL_COPY_EXECUTION` stays false” is **false** for the API process: env `true` + DI bind + no re-pin = runtime armed.

`CREDENTIALS_AND_COPY_STATUS.md` “forced false”, architecture docs, and README “off” are **stale vs DI**.

Arming the flag still cannot emit a ticket (`SAFE_BY_ABSENCE` on claim 4 + copy consts). That is **not** the assigned claim.

---

## Honesty / residuals

- This slot did **not** live-attach Manager and did **not** hit `:5000`. Claims 2–3 are source capability, not a fresh census.
- Sibling demo helper can assemble `35=D` off-hop; it is unused by API/workers/DI.
- `apps/mt5-worker/Worker.cs` leftover four-login scorer is not API startup.
- Next sender, if one is ever written, would see `RealCopyEnabled=true` on the API host.

---

## Verdict

**FAIL.** Claims 1–4 file-proven (2–3 capability only). Claim 5 disproven: `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` + `DependencyInjection.cs` L41 bind + no hosted re-pin. Risk to capital **NONE** (`SAFE_BY_ABSENCE`).
