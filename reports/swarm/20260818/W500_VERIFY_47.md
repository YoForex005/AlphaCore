# W500_VERIFY_47 — Adversarial live-path verify (slot 47)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **47** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot re-read the product files listed below. Prior swarm reports were **not** used as evidence.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync` of that logon. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` loads it. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host does not re-pin false. |

Overall **FAIL** because claim 5 cannot be proved (the files show the flag is env-bound and currently `true`).

Copy hop remains **SAFE_BY_ABSENCE**. Dest capital at risk this process: **NONE**.

---

## Files read this slot (absolute)

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\mt5-worker\Program.cs` (18 lines)
- `D:\Prop\apps\fix-worker\Program.cs` (18 lines)
- `D:\Prop\apps\mt5-worker\Worker.cs` (first 50)
- `D:\Prop\apps\fix-worker\Worker.cs` (first 40)
- `D:\Prop\apps\api\appsettings.json` (50 lines; no `REAL_COPY` key)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header only; class exists)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (112 lines)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (63 lines)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94 lines)
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` (141 lines)
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` (selected)
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` (146 lines)
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` (67 lines)
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines)
- `D:\Prop\src\Mt5\Env\EnvFile.cs` (42 lines)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (113 lines)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (header + send sites; **not** the assigned session class)
- `D:\Prop\.env` L73 boolean only

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs`.

Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `D:\Prop\apps\mt5-worker\Program.cs` L15, `D:\Prop\apps\fix-worker\Program.cs` L15
- Remaining `DemoSeeder` C# hits: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (class still on disk), `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25, plus report-tmp programs under `reports\swarm\20260818\_tmp_*`. **None of those are API startup.**

DI fail-closes Fake:

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

Live ingest walks those native connectors only (`LiveIngestHostedService` L39–56). Catalog ingest calls `GetGroupsAsync` + `GetAccountsAsync(null)` (`DealIngestionService` L45–49). No dummy substitution on catalog fail (L70: "No dummy data will be substituted.").

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call `DemoSeeder.SeedAsync`. **API process does not.**
- `D:\Prop\apps\mt5-worker\Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a full `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

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

What the file proves:

- Primary walk is `GroupRequestArray("*")` (wildcard, not a plan-name filter).
- If that walk yields **zero** groups, fallback is `GroupTotal` + `GroupNext`.
- `AddGroup` only skips blank names / duplicates. No hard-coded group allow-list.

What the file does **not** prove (this slot did not attach):

- That the live Manager actually returns every Achiever + Starwave group.
- Any census count. Prior 18/8460 numbers are **not** re-measured here.

Live catalog path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` (→ `GetGroupsCore`). `GetAccountsAsync(null)` L48 re-enters `GetGroupsCore` when `group` is null (connector L199–203).

Verdict: **PASS_SOURCE** — the connector **can** list groups via those two APIs. Runtime “all groups” completeness is unproven this slot and is **not** claimed as measured.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file.

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

`GetAccountsCore` (L189–214): if `group` is null/blank, it walks **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup` per name, de-duped by login.

Ingest: `GetAccountsAsync(null, ct)` at `DealIngestionService` L48 and L62.

What the file proves:

- Per-group primary is `UserRequestArray`.
- Empty result falls through to `UserLogins` then `UserRequestByLogins`.
- Catalog-null means “all groups already enumerated,” not a four-login demo set.

What the file does **not** prove:

- Live Manager user counts. This slot did not attach.
- Hosted **scoring** completeness: `LiveIngestHostedService` L106 scores `ListLoginsWithDealsAsync` only (deal-bearing logins). Catalog still upserts every account from the walk.

Verdict: **PASS_SOURCE**. Method is file-proven. Live “all traders” count is unproven this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135). Independent grep of that file for `35=D`, `NewOrderSingle`, and `Build("D")`: **0 hits**.

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

Single socket write: L47–50 `WriteAsync` of that logon. Then one `ReadAsync`. `using` TcpClient + SslStream — session is disposed. No market-data subscribe (`35=V`/`35=x`). No NewOrderSingle.

Hosted caller `CTraderFixLogonHostedService` L48–58 calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” It never sends `35=D`.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 exists. It is **not** `CTraderFixSession`. Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only. Demo-gated (refuses non-`demo-` host, non-`demo.` sender, `live-` host, and account `1369850`). Not registered in DI. Not on the copy hop.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show the opposite on the API host.

| Surface | What this slot read |
|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret quoted) |
| `D:\Prop\apps\api\Program.cs` L10 | `EnvFile.FindAndLoad()` |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` L14 + L38 | candidates include `D:\Prop\.env`; `Environment.SetEnvironmentVariable(key, value)` |
| `D:\Prop\apps\api\Program.cs` L13 | `builder.Configuration.AddEnvironmentVariables()` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` L41 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logs `RealCopyArmed={Armed}` (L68–70); **does not** assign `RealCopyEnabled = false` |
| `D:\Prop\apps\api\Program.cs` L55, L76 | `/api/health` and `/api/settings` echo `runtime.RealCopyEnabled` |
| `D:\Prop\apps\api\appsettings.json` | no `REAL_COPY_EXECUTION_ENABLED` key (env wins) |

Architecture docs / README still write `=false`. That is **not** the running bind. `LiveRuntimeStatus.RealCopyEnabled` defaults unset (`false`) only until DI overwrites it.

`apps/fix-worker/Worker.cs` L21 still reads `CTrader:RealCopyExecutionEnabled` (default `false`). That is a **different key** from `REAL_COPY_EXECUTION_ENABLED`. It does not re-pin the API runtime singleton.

**Copy hop is still unable to send** (does **not** rescue claim 5):

```16:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

- Persist path hard-sets `AllowFixSend = false` (L211).
- Live-send branch L217 is unreachable unless `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`.
- `CTraderFixSession` cannot emit `35=D` (claim 4).
- `BuildBlockers` still lists “No NewOrderSingle sender — SAFE_BY_ABSENCE”.

Claim 5 is about the **flag staying false**. It does not stay false. Verdict: **FAIL**.

---

## Risk to capital

**NONE** (`SAFE_BY_ABSENCE`).

Reason: even with `RealCopyEnabled=true` on the API host, there is no product NewOrderSingle writer on `CTraderFixSession`. Persist `AllowFixSend=false`. `NewOrderSingleImplemented=false`. `VenueReconciled=false`. This slot sent no FIX `35=D` and did not attach Manager.

**HIGH if** a sender is later wired while `.env` remains `true` and allocation is 1:1 (`CopyTradingService.AllocationFactor` / `XauUsdOneToOneCopyPolicy`). That is a future-risk note, not a current send.

---

## Honesty / not claimed

- Did not live-attach Achiever or Starwave. Did not re-sum 18/8460.
- Did not hit `/api/settings` on a running process.
- Did not execute `DemoFixTestTrade`.
- Did not print passwords, proxy auth, or FIX account secrets.
- Claims 2–3 are **source capability**, not a measured census.

---

## Checklist

- [x] DemoSeeder gone from **API startup** (file remains for tests)
- [x] Native `GroupRequestArray("*")` + `GroupTotal` fallback present
- [x] Native `UserRequestArray` + `UserLogins` present
- [x] `CTraderFixSession` `35=D=0`
- [ ] `REAL_COPY_EXECUTION` stays false — **NO** (env-bound `true`)
