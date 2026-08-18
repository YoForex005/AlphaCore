# W500_VERIFY_10 — Adversarial live-path verifier (slot 10)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_10.md` |
| Agent / slot | W500 verify **10** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Capability proven from source; census **not** re-measured. |
| Method | Independent `read_file` of `apps/api/Program.cs`, `DemoSeeder.cs`, `BrokerCatalogSeed.cs`, `NativeMt5BrokerConnector.cs`, `CTraderFixSession.cs`, `CTraderFixDemoTestTrade.cs`, `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `LiveMt5Registration.cs`, `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `EnvFile.cs`, worker `Program.cs` files, `appsettings*.json`, `launchSettings.json`, `docker-compose.yml`. Targeted `grep` across `D:\Prop` for `DemoSeeder`, `SeedAsync`, `GroupRequestArray`, `GroupTotal`, `UserRequestArray`, `UserLogins`, `35=D`, `REAL_COPY_EXECUTION`. `.env` grepped for the two flag keys only. |

**Honesty rule:** FAIL any claim that cannot be proven from the file just read. Prior swarm reports (`A002`, `A014`, `A015`, `CREDENTIALS_AND_COPY_STATUS.md`, W500_68/108 “forced false”) are **not** evidence. A comment is not a choke. A default is not a pin. A live census in another report is not a re-attach. Absence of `35=D` on the copy hop is `SAFE_BY_ABSENCE`, not a §68/§70 PASS.

---

## 0. Verdict (binding)

**FAIL.**

Four of five assigned claims are proven from current files. Claim **(5)** is **disproven**: lab `.env` has `REAL_COPY_EXECUTION_ENABLED=true` and the API process **binds** that value onto `LiveRuntimeStatus.RealCopyEnabled`. The flag does **not** stay false.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | API `Program.cs` calls `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. Sole `SeedAsync` product caller is integration tests. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (code capability) | `GetGroupsCore` calls `GroupRequestArray("*")` then, if empty, `GroupTotal` + `GroupNext`. This slot did **not** live-attach, so runtime completeness is **unproven**. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (code capability) | `GetAccountsAsync(null)` walks every group from (2); per group `UserRequestArray` then empty → `UserLogins` + `UserRequestByLogins`. Residual: `UserLogins` is skipped if `users.Total() > 0` (partial-array hole). Runtime ALL not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: outbound MsgType is `(35, "A")` only. Grep of that file for `35=D` / `Build("D")` = 0. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | `.env` L73 = `true`. API `EnvFile.FindAndLoad()` + DI L41 binds exact `"true"`. Hosted logon **does not** re-pin false. |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the copy hop). Flag-armed ≠ ticket. `CTraderFixSession` cannot send NewOrderSingle. `CopyTradingService.NewOrderSingleImplemented = false`, `VenueReconciled = false`, persist `AllowFixSend = false`. Sibling demo helper `CTraderFixDemoTestTrade.Build("D")` is **not** the copy hop and is demo-gated.

One-line:

```text
FAIL slot 10: DemoSeeder off API; Native GroupRequestArray/GroupTotal + UserRequestArray/UserLogins present; CTraderFixSession 35=A only; REAL_COPY does NOT stay false (.env true + DI bind). No live 35=D. Risk NONE.
```

---

## 1. Claim 1 — DemoSeeder is not the API startup path — **PASS**

Live API host: `D:\Prop\apps\api\Program.cs` (159 lines). Startup after `app.Build()`:

```152:157:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0 hits**.

`grep SeedAsync` product callers:

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L16 | definition only |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | test fixture |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | historical eval harnesses, not hosts |

Workers also seed catalog, not demo tape:

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Same pattern in `D:\Prop\apps\fix-worker\Program.cs` L11–16.

DI fail-closed Native only (`LiveMt5Registration.CreateConnectors` returns two `NativeMt5BrokerConnector`s). Fake is **not** registered. `DemoSeeder` still constructs `DemoBrokerFactory.CreateDefault()` for **tests**, which is not the API process.

**Stale prior reports (do not reuse):** `A002_api_dummy_path.md`, `A005_dashboard_traders.md`, `A011_fix_persist.md` still claim `Program.cs` calls `DemoSeeder.SeedAsync`. Live file does not.

**Residual (not a claim-1 fail):** `DemoSeeder.cs` remains on disk. `mt5-worker/Worker.cs` L31 still scores dummy logins `{10001,10002,10003,99001}`. Hosted API ingest scores `ListLoginsWithDealsAsync`, not those four.

---

## 2. Claim 2 — Native connector can list all groups via GroupRequestArray or GroupTotal — **PASS (code)**

Live file: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

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

Public surface: `GetGroupsAsync` → `GetGroupsCore` (L42–43).

Live ingest uses it:

```45:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

**What the file proves:** the connector **attempts** a full-manager group census via `GroupRequestArray("*")`, with `GroupTotal`/`GroupNext` as empty-result fallback.

**What the file does not prove (residual, not flipped to FAIL):**

- This slot did **not** attach a Manager. Prior 18-group census is **hearsay**.
- `GroupTotal` runs only if `list.Count == 0`. A **partial** successful `GroupRequestArray` is **not** unioned with pump `GroupTotal`.
- Manager permission / mask `"*"` completeness is a venue fact, not a C# fact.

---

## 3. Claim 3 — All traders via UserRequestArray / UserLogins — **PASS (code)**

Same connector file.

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore`, then `ReadAccountsForGroup`:

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
        // ...
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

Catalog path is `GetAccountsAsync(null, ct)` (`DealIngestionService` L48 / L62). That is the “all traders the Manager will enumerate per discovered group” walk.

**Residuals (do not greenwash):**

- `UserLogins` is **only** when `users.Total() == 0`. A non-empty **partial** `UserRequestArray` skips the login-array fallback.
- `UserGetByGroup` is pump-cache, used only on hard fail of `UserRequestArray`.
- Hosted **scoring** is `ListLoginsWithDealsAsync`, not every catalog login (`LiveIngestHostedService` L106). Catalog still upserts all returned accounts.
- Runtime ALL-trader count **not** re-attached this slot.

Claim is “can list via those APIs.” File proves the methods are wired on the live Native path. Claim is not “this process just counted 8460.”

---

## 4. Claim 4 — CTraderFixSession has no 35=D — **PASS**

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines) read in full.

Outbound builder:

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

Single `WriteAsync` (L49) of that Logon. Tag 35 is also **read** from the reply (`Extract(reply, "35")` L55) and interpolated into an error string (`"Logon rejected 35={msgType}"` L73). That is inbound parse, not a NewOrderSingle.

`grep` on this file for `35=D` / `Build("D")` / `NewOrderSingle` = **0**.

Hosted caller `CTraderFixLogonHostedService` invokes `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and never any other session API.

**Residual (not this claim):** sibling `CTraderFixDemoTestTrade.cs` **does** `Build("D", ...)` at L139 / L163 / L197. That class is a standalone demo tool (`tools/DemoFixTestTrade`), gated off `live-*` / `live.*` / account `1369850`. It is **not** `CTraderFixSession` and is **not** registered in DI / copy / API. `CTraderFixDemoMatrix.cs` L87 also `Build("D")`. Copy hop still has **no** assembler.

---

## 5. Claim 5 — REAL_COPY_EXECUTION stays false — **FAIL**

The assigned claim is that the flag **stays false**. Live files prove the opposite on the API process.

### 5.1 DI binds env `"true"` — no hard pin

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed={Armed}` and does **not** assign `false`. W500_68/108 / A014 / A015 / `CREDENTIALS_AND_COPY_STATUS.md` “forced false” is **stale**.

### 5.2 API loads lab `.env`

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
// ...
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad()` includes hard path `D:\Prop\.env` (`EnvFile.cs` L15). `grep` of that file (flag keys only):

| Line | Key | Value (boolean only) |
|---:|---|---|
| 73 | `REAL_COPY_EXECUTION_ENABLED` | `true` |
| 106 | `FEATURE_COPY_TRADING_ENABLED` | `true` |

Therefore API `LiveRuntimeStatus.RealCopyEnabled` becomes **true** when `.env` is present.

`/api/settings` exposes the runtime bit (not a hardcoded false):

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    // ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`/api/health` L55 also returns `realCopyEnabled = runtime.RealCopyEnabled`.

### 5.3 Defaults that are **not** a pin

| Surface | Observed | Pin? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (`CTraderFixOptions.cs` L35) | No. POCO unbound (`Configure<>` absent). |
| `apps/api/appsettings.json` | no `REAL_COPY_EXECUTION_ENABLED`; `FeatureFlags:LiveCopyEnabled=false` (different name) | No. |
| `appsettings.Development.json` | flag absent | No. |
| `launchSettings.json` | only `ASPNETCORE_ENVIRONMENT` | No. |
| `docker-compose.yml` | flag absent | No. |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Different key; log-only. Workers do **not** call `EnvFile.FindAndLoad()`. |
| `CopyTradingService.BuildBlockers` | adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** `!_runtime.RealCopyEnabled` | Display. After env bind this blocker is **gone**. |

### 5.4 Why capital is still not at risk (does **not** rescue claim 5)

Flag-armed is still **not** a NewOrderSingle:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

Persist path forces `AllowFixSend = false` (L211) even if `RiskEngine` would set `allowSend` from `RealExecutionEnabled && Reconciled && VenueHealthy`. The LIVE-send branch (L217) still requires `NewOrderSingleImplemented && VenueReconciled` (both const false) and then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — it never writes FIX.

Copy hosted service only calls `GenerateShadowIntentsAsync`.

**Claim 5 still FAIL.** “Stays false” is a flag-state claim. The live API flag state is **true** when `.env` loads.

---

## 6. Stale artifacts this slot invalidates

| Artifact | Stale claim | Live fact |
|---|---|---|
| `reports/CREDENTIALS_AND_COPY_STATUS.md` L30 | `REAL_COPY_EXECUTION_ENABLED` **false (forced)** | DI binds `.env=true`; no re-pin |
| `A014_live_path_now.md` | `RealCopyEnabled = false` at DI | L41 is env equality |
| `A015_enable_copy_gates.md` | logon forces `_runtime.RealCopyEnabled = false` | logon only reads/logs |
| `A002_api_dummy_path.md` | API startup = `DemoSeeder` | API startup = `BrokerCatalogSeed` |
| W500_68 / W500_108 | env + DI + hosted pinned false | env true, DI bound, hosted unpinned |

---

## 7. What this slot did **not** do

- Did not live-attach Achiever or Starwave (no 1012 / group / login recount).
- Did not open cTrader TLS or send `35=A` / `35=D`.
- Did not edit product source, `.env`, or tests.
- Did not treat prior 18/8460 census as this slot’s measurement.

---

## 8. Binding summary

| Check | Result |
|---|---|
| API dummy seeder | **OFF** (`BrokerCatalogSeed` only) |
| Native group walk | **`GroupRequestArray("*")` then `GroupTotal`** |
| Native trader walk | **`UserRequestArray` then `UserLogins`** (empty fallback) |
| `CTraderFixSession` outbound 35 | **`A` only** |
| `REAL_COPY_EXECUTION_ENABLED` process bit on API | **true if `.env` loads** — claim FAIL |
| Copy-path `35=D` | **absent** |
| Risk to capital | **NONE** |
| Overall slot 10 | **FAIL** |
