# W500_VERIFY_90 — Adversarial live-path verify (slot 90)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 90 |
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
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound MsgType is `A`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from this tree. Claim 5 is false on the API composition: lab `.env` L73 is `true`, `EnvFile.FindAndLoad()` injects it, and `DependencyInjection.cs` L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. `CTraderFixLogonHostedService` does not re-pin false. `/api/settings` echoes the runtime bool.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` flag echo (160 lines) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/trader walks |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog caller `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Entire 135-line hop |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Sibling residual `Build("D")` (not this hop) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `false`; unread by DI |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` → process env |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Send still unimplemented |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick only |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Only product `DemoSeeder.SeedAsync` caller |
| `D:\Prop\.env` L73 + L106 | Flag booleans only |

This slot did **not** attach to Manager or FIX TRADE. Claims 2–3 are **source capability**, not a live census.

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

`apps/api/Program.cs` is 160 lines. Zero `DemoSeeder` tokens. Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

Both workers seed the same way (`BrokerCatalogSeed.EnsureAsync` only):

- `D:\Prop\apps\fix-worker\Program.cs` L11–16
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16

DI refuse-closes Fake/dummy before connectors exist, then registers Native only:

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

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Grep of `D:\Prop\src\Infrastructure` for `FakeMt5`: **0**.

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25). That is not `apps/api`.
- Scratch copies under `reports/swarm/20260818/_tmp_*` also call it; they are not the host.
- Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A015` “process pin”) are **superseded** by current `Program.cs` / DI.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

Primary walk is the Manager request API with mask `"*"`. Empty result falls back to the cache enumerator:

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

No `Take`/`Skip`/plan-name filter on this walk. Hosted ingest uses the same method:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` per registered Native connector (`D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` L56).

**Honesty:** this slot did not re-attach. The file proves the connector **can** enumerate manager-visible groups via `GroupRequestArray("*")` or `GroupTotal`/`GroupNext`. It does **not** re-prove any prior 18-group census.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

`GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()`, then `ReadAccountsForGroup`:

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
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
```

```223:232:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Primary request path is `UserRequestArray`. Empty array → `UserLogins` + `UserRequestByLogins`. `UserGetByGroup` is only a hard-fail fallback (pump cache). Hosted catalog calls `GetAccountsAsync(null, ct)` so the group list is the full Native walk from claim 2.

**Honesty:** file-capability only. Not re-attached. If `UserRequestArray` returns OK with a non-empty but incomplete set, `UserLogins` is skipped — that is a residual of the source, not a live count.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135/135** lines. Re-read this slot:

- Only outbound MsgType construction: `(35, "A")` at L96 inside `BuildLogon`.
- Single `ssl.WriteAsync` at L49.
- `using var tcp` / `await using var ssl` — sockets disposed after the one-shot probe.
- Inbound `Extract(reply, "35")` at L55; success path requires `msgType == "A"` (L56).
- Tokens `35=D`, `(35, "D")`, `Build("D")`, `NewOrderSingle`: **0**.

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

Hosted caller is logon-only (QUOTE 5211, TRADE 5212) and states send is unimplemented:

```48:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        var quote = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Quote, host, 5211, sender, target,
            ...
        var trade = await CTraderFixSession.TryLogonAsync(
            FixSessionQualifier.Trade, host, 5212, sender, target,
            ...
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

No `_runtime.RealCopyEnabled = false` assignment exists in this file.

**Residual (does not put `35=D` on `CTraderFixSession`):** sibling `CTraderFixDemoTestTrade` `Build("D")` at L139 / L163 / L197. Caller is `D:\Prop\tools\DemoFixTestTrade\Program.cs` only. Not in DI. Demo-gated (`host` must start `demo-`; refuses `live-*` / `live.` / account `1369850`). Copy hop does not call it.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproven)

The assigned claim is that the flag **stays false**. Live files prove the opposite on the API host.

1. Lab `.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret printed). L106: `FEATURE_COPY_TRADING_ENABLED=true`.
2. API boot loads that file before configuration bind:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) searches cwd parents and hard-path `D:\Prop\.env`, then `Environment.SetEnvironmentVariable` for every `KEY=value`.

3. DI binds the env key onto the singleton (not a hard `false`):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. Logon host **reads** `_runtime.RealCopyEnabled` for a log line and does **not** overwrite it (see L68–70 quote above). Older “hosted pin false” notes (`A015`, `CREDENTIALS_AND_COPY_STATUS.md` “forced”) are **stale**.
5. Settings endpoint echoes the runtime bool (not a hardcoded `false`):

```71:77:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    riskLimits = new Dictionary<string, decimal> { ["maxQuoteAgeSeconds"] = 3, ["maxSignalAgeSeconds"] = 15 },
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`FEATURE_COPY_TRADING_ENABLED` is a **literal `true`** in the API (env L106 is unused by this map). `CopyTradingService.GetStatusAsync` also hardcodes `FeatureCopyEnabled: true` (L45) and reports `RealCopyArmed: _runtime.RealCopyEnabled` (L46).

6. `CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (`D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35) but **is not registered** in DI (`Configure<CTraderFixOptions>` = 0). The POCO is not the API flag.
7. FIX worker logs a **different** key: `_config.GetValue("CTrader:RealCopyExecutionEnabled", false)` (`apps/fix-worker/Worker.cs` L21). Workers do **not** call `EnvFile.FindAndLoad()`. That does not rescue the API host, which does load `.env` and binds `REAL_COPY_EXECUTION_ENABLED`.
8. `apps/api/appsettings.json` has no `REAL_COPY_EXECUTION_ENABLED` key; FeatureFlags.LiveCopyEnabled is a different unused name. The live API flag is env + DI.

**Therefore claim 5 cannot be proven. It is disproven.** Overall verdict **FAIL**.

Copy hop still cannot send. That is a different claim:

```17:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

Persist always writes `AllowFixSend = false` (L306). The live-send `if` also requires `NewOrderSingleImplemented && VenueReconciled` (L312) and only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. `BuildBlockers` always includes `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` (L468–469). Hosted copy ticks `TickRosterAsync` + `GenerateShadowIntentsAsync` only (`CopyTradingHostedService` L28–29).

README L28 still says `REAL_COPY_EXECUTION_ENABLED=false`. That sentence is **stale** versus `.env` L73 + DI L41.

---

## Risk to capital

**NONE** today (`SAFE_BY_ABSENCE`).

Armed `RealCopyEnabled=true` on the API process is **not** a ticket. Dest send still requires a `35=D` assembler that `CTraderFixSession` does not have, plus `NewOrderSingleImplemented=true` (const false) and `VenueReconciled=true` (const false). Persist forces `AllowFixSend=false`.

If a sender were later wired against the current DI bind, the next hop would see the flag **already true**. That is why claim 5 fails even though capital is not at risk yet.

This slot did not live-attach Manager or send FIX TRADE.

---

## Verdict

**FAIL.**

| Claim | Result |
|---|---|
| 1 DemoSeeder off API startup | **PASS** (`BrokerCatalogSeed` only) |
| 2 Native ALL groups | **PASS** (source: `GroupRequestArray("*")` / `GroupTotal`) |
| 3 Native ALL traders | **PASS** (source: `UserRequestArray` / `UserLogins`) |
| 4 `CTraderFixSession` no `35=D` | **PASS** (135/135 is `35=A` only) |
| 5 `REAL_COPY_EXECUTION` stays false | **FAIL** (`.env` L73 `true` + DI L41; no re-pin) |

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
