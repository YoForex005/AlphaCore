# W500_VERIFY_56 — Adversarial live-path verify (slot 56)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_56.md` |
| Agent / slot | W500 adversarial verifier **56** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live product `apps/`, `src/`; lab `.env` **booleans only**) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager `Connect`. No TLS Logon. No order. Claims 2–3 are **file-capability** only. |
| Method | Independent `read_file` of live hosts + Native connector + FIX session + DI + logon host + copy hop + `.env` flag lines. Targeted `grep` on `apps/` and `src/`. Prior swarm text treated as **untrusted**. Verdict **FAIL** if any assigned claim is disproven or cannot be proven from the file. |

**Honesty rule:** a compile-time default is not a runtime pin. An env bind that can become `true` means the flag does **not** “stay false.” `GroupRequestArray("*")` is a capability, not a measured census. `SAFE_BY_ABSENCE` is not a §68 / §70 PASS. Sibling `CTraderFixDemoTestTrade.Build("D")` is not `CTraderFixSession`. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 5 is disproven from live files.**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` token count under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_CODE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty-list fallback L174 `GroupTotal()` + `GroupNext`. Live completeness **not** re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_CODE** | `GetAccountsAsync(null)` walks every group from (2). Per group: `UserRequestArray` L223, then `UserLogins` L230 if `users.Total()==0`. Live completeness **not** re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file **135/135**: outbound tag 35 is `"A"` only (`BuildLogon` L96). One `WriteAsync` (L49). Zero `NewOrderSingle` / `Build("D")` / `35=D` literals. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API `EnvFile.FindAndLoad()` (L10) + `AddEnvironmentVariables()` (L13). DI L41 binds `LiveRuntimeStatus.RealCopyEnabled` to that string. Hosted logon **does not** re-pin false. `/api/settings` echoes runtime. |

One-line:

```text
FAIL. DemoSeeder is off the API/worker startup path. Native group/user request APIs are wired (file-capability; not re-attached). CTraderFixSession is 35=A only. REAL_COPY_EXECUTION_ENABLED does not stay false (.env L73=true, DI binds, no re-pin). Copy hop still cannot send (SAFE_BY_ABSENCE). Risk to capital NONE.
```

---

## 1. Claim 1 — `DemoSeeder` is not the API startup path — **PASS**

Read live `D:\Prop\apps\api\Program.cs` (**160** physical lines). Startup after `app.Build()` is catalog-only:

```152:158:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`.

Same seed on both workers (not the API claim, but confirms product hosts):

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`D:\Prop\apps\fix-worker\Program.cs` L11–16 is identical.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- Product C# callers remaining:
  - `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` — class still on disk (`public static class DemoSeeder` L14)
  - `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 — test-only
  - scratch trees under `reports/swarm/20260818/_tmp_*` — not hosts

DI fail-closes Fake before any connector is registered:

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

`BrokerCatalogSeed.EnsureAsync` writes broker catalog + FIX rows already `Disconnected` / “NewOrderSingle off”. It does not ingest FakeMt5 tape.

**Residual (does not revive claim 1):**

- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup and not `DemoSeeder`. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). API `/api/ops/resync` scores `ListLoginsAsync` (all stored accounts).
- `DemoSeeder.cs` remains on disk for tests. **API process does not call it.**

**Stale reports:** `A002_api_dummy_path.md` / `A005_dashboard_traders.md` / `A010_prior_swarm.md` / `A011_fix_persist.md` (API still calls `DemoSeeder`) are **superseded** by current `Program.cs`.

---

## 2. Native groups via `GroupRequestArray` or `GroupTotal` — **PASS_CODE**

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (**459** lines).

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

- Primary walk is the Manager request API `GroupRequestArray("*")` — `*` is the all-groups mask.
- If that walk yields **zero** rows, fallback is pump-cache `GroupTotal()` + `GroupNext`.
- Ingest uses this path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null)`. Flag-blind.

What the file does **not** prove (this slot did not attach):

- That a live Achiever/Starwave session actually returns a complete set.
- That a **partial non-empty** `GroupRequestArray` result would be completed — fallback only fires when `list.Count == 0`. No pagination.
- Prior census 8+10=18 groups is **not re-measured** here.

`_pumpEnabled` is set from `Connect` (full pump vs `PUMP_MODE_NONE` fallback) and is **never** consulted by `GetGroupsCore`. Request-first.

---

## 3. All traders via `UserRequestArray` / `UserLogins` — **PASS_CODE**

Same file. Catalog of all users is `GetAccountsAsync(null)`:

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Per-group walk:

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

What the file proves:

- Request-first: `UserRequestArray(gname, users)`.
- Hard-fail only: pump-cache `UserGetByGroup`.
- Empty array: `UserLogins` then `UserRequestByLogins`.
- `GetAccountsAsync(null)` unions every group from claim 2.

What the file does **not** prove:

- Live completeness (not re-attached). Prior 6512+1948=8460 is **cited, not re-probed**.
- A partial non-empty `UserRequestArray` will **not** fall through to `UserLogins`.
- Hosted scoring is **not** “all traders”: `LiveIngestHostedService` L106 uses `ListLoginsWithDealsAsync` only. That is a scoring residual, not a connector hole.

---

## 4. `CTraderFixSession` has no `35=D` — **PASS**

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

The only outbound builder:

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

The only wire write:

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

Then one `ReadAsync`, then `using` disposes `TcpClient` / `SslStream`. No loop, no heartbeat, no quote subscribe, no NewOrderSingle.

This-slot grep of the assigned file:

| Token | Count |
|---|---|
| `(35, "A")` | **1** (L96) |
| `(35, "D")` / `35=D` / `NewOrderSingle` / `Build("D")` | **0** |
| `WriteAsync` | **1** (L49) |

Hosted caller is `CTraderFixLogonHostedService` L48–58: two `TryLogonAsync` (QUOTE 5211 / TRADE 5212). Log line L69: `"NewOrderSingle still unimplemented."`

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 is **not** this class. It is called only from `tools/DemoFixTestTrade/Program.cs` (not API / DI / workers / copy). It refuses live identity (`host` not `demo-*`, sender `live.`, account `1369850`) at L43–60. Claim 4 is scoped to `CTraderFixSession`.

---

## 5. `REAL_COPY_EXECUTION` stays false — **FAIL** (disproven)

The assigned claim is a **runtime pin**. A POCO default of `false` is not a pin once DI binds env.

### 5.1 Lab `.env` is `true`

`D:\Prop\.env` L73 (boolean only; no other keys quoted):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (adjacent flag, also a boolean):

```text
FEATURE_COPY_TRADING_ENABLED=true
```

No `appsettings*.json` under `apps/` defines `REAL_COPY`. The live value is env.

### 5.2 API loads that file before DI

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) searches cwd / parents and hard-path `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`. `AddEnvironmentVariables()` then surfaces `REAL_COPY_EXECUTION_ENABLED` to `IConfiguration`.

### 5.3 DI binds it onto the live singleton

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** `= false` assignment after this. `CTraderFixLogonHostedService` L60–70 copies logon status and **logs** `RealCopyArmed={Armed}`; it never writes `_runtime.RealCopyEnabled = false`.

### 5.4 Settings API echoes the armed runtime

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

A live API process that found `D:\Prop\.env` will therefore report `REAL_COPY_EXECUTION_ENABLED: true`. Prior “hardcoded false on `/api/settings`” reports (E038 / CREDENTIALS forced-false / W500_68 / W500_108 pin-false) are **stale**.

### 5.5 What is still false (does not rescue the claim)

| Surface | Value | Why it does not prove “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default `false` | **Unread** by `CTraderFixSession`. No `Configure<CTraderFixOptions>` bind of `REAL_COPY_EXECUTION_ENABLED`. Dead POCO default. |
| `apps/fix-worker/Worker.cs` L21 | `_config.GetValue("CTrader:RealCopyExecutionEnabled", false)` | **Different key** than `REAL_COPY_EXECUTION_ENABLED`. Log-only. Worker still stamps `Disconnected`. Split-brain, not a pin. |
| Workers `Program.cs` | do **not** call `EnvFile.FindAndLoad()` | Their DI `RealCopyEnabled` depends on process env. API **does** load `.env`. The live API path is armed. |
| Copy hop | `NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; 0 `new ExecutionIntent`; `CanPromoteToLive => false` | Proves **send is absent**, not that the flag stays false. |

`CopyTradingService` L16–17 / L211 / L217:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

```211:220:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
                    DecidedAt = now
                };
                _db.RiskDecisions.Add(rec);
                intent.RiskDecisionId = rec.Id;

                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
```

`BaselineScorer.CanPromoteToLive => false` (`BaselineScorer.cs` L211). Grep `new ExecutionIntent` under `D:\Prop\src` = **0**.

**Claim 5 FAIL** because the assigned words are “stays false,” and the live API path **binds `.env=true`** with **no re-pin**. Next sender that keys off `LiveRuntimeStatus.RealCopyEnabled` would see **armed**.

---

## 6. Risk to destination capital

**NONE** (`SAFE_BY_ABSENCE`).

Reasons (file-proven this slot):

1. `CTraderFixSession` cannot emit `35=D` (claim 4).
2. Copy service const `NewOrderSingleImplemented=false` and persist `AllowFixSend=false`.
3. No `ExecutionIntent` writer in product `src`.
4. Promotion to LIVE is hardcoded closed.
5. Hosted FIX is one-shot Logon then dispose.

This is **not** a go-live PASS. §68 / §70 were not re-run. `SAFE_BY_ABSENCE` collapses the moment a sender is added while DI still binds `.env=true`.

---

## 7. Files read (this slot; no secrets)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + settings + resync |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover 4-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | nested `CTrader:RealCopyExecutionEnabled` |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | actual seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class exists; not called from apps |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | claims 2–3 |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` load |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `*` / `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | hosted ingest |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | claim 4 (135/135) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unread POCO default |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual `Build("D")` |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | SHADOW only |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` |
| `D:\Prop\.env` L73 + L106 | booleans only |

---

## 8. What this slot did **not** do

- Did not live-attach Manager (claims 2–3 not re-censused).
- Did not open a FIX socket.
- Did not start the API to observe `/api/settings` over HTTP (file proof of bind is enough to fail claim 5).
- Did not modify product source, tests, or `.env`.
- Did not print passwords, hosts-as-secrets, or tag 554 values.

---

## 9. Binding close

Slot **56** overall **FAIL**.

Claims 1–4 hold from the files. Claim 5 does **not**: `REAL_COPY_EXECUTION` is env-bound and the lab file is `true`. Destination capital risk remains **NONE** only because the copy hop still has no NewOrderSingle (`SAFE_BY_ABSENCE`).
