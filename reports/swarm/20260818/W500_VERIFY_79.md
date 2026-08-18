# W500_VERIFY_79 — Adversarial live-path verify (slot 79)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **79** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the booleans `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files).

This slot independently re-read product files under `D:\Prop\apps` and `D:\Prop\src`. Prior swarm reports were **not** treated as evidence.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` tokens under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if the list is empty, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. `/api/settings` echoes runtime. Hosted logon does **not** re-pin false. |

Overall **FAIL** because claim 5 cannot be proved (the files prove the flag is armed).

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Armed flag cannot emit a ticket: `CTraderFixSession` has no NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`.

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
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- Product `*.cs` callers of `DemoSeeder.SeedAsync`: `tests\Integration\SeedingAndStoreTests.cs` L25 only (plus report `_tmp_*` scratch programs, not hosts)

DI fail-closes Fake before connectors register:

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

`LiveMt5Registration.CreateConnectors` (`src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–50) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` on that path. The only `FakeMt5BrokerConnector` type lives in `src\Mt5\Connectors\FakeMt5BrokerConnector.cs` (used by `DemoSeeder` L126 via `DemoBrokerFactory.CreateDefault()`, not by DI).

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration test still calls it. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

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

Live catalog uses that path (flag-blind):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` (`src\Infrastructure\Hosting\LiveIngestHostedService.cs` L56) calls `SyncCatalogAsync` after `ConnectAsync` for every registered Native connector. `tools/LiveBrokerProbe/Program.cs` L25–26 uses the same `GetGroupsAsync` + `GetAccountsAsync(null)`.

**Proved from file:** the connector **can** enumerate groups with `GroupRequestArray("*")` and, if that returns an empty list, `GroupTotal` + `GroupNext`.

**Not proved (so not claimed as a live census):** this slot did **not** attach Manager. Prior 18/8460 figures are **not** re-measured here.

**Caveat (do not greenwash “always all”):** if `GroupRequestArray` returns a **non-empty partial** set, `list.Count == 0` is false and `GroupTotal` is skipped. That is a completeness hole, not a missing API.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same connector file. `GetAccountsAsync(null)` (`GetAccountsCore` L189–213) walks **every** group from `GetGroupsCore()`, then `ReadAccountsForGroup`:

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

**Proved from file:** trader enumeration is request-first (`UserRequestArray`) with `UserLogins` fallback when the user array is empty. Catalog ingest calls `GetAccountsAsync(null)` (all groups from claim 2).

**Caveats (adversarial):**

- `UserLogins` runs **only** when `users.Total() == 0`. A partial `UserRequestArray` (Total > 0 but incomplete) will **not** fall back.
- `UserGetByGroup` (pump-cache) is used only when `UserRequestArray` returns a hard fail (not OK / OK_NONE / NOTFOUND).
- Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), not all catalog logins. That is a scoring filter, not a catalog filter.
- This slot did not re-attach; “all traders” as a live count is **unproved** here.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Grep of that file for `35=D`, `(35, "D")`, and `NewOrderSingle`: **0**.

Only outbound builder:

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

Only socket write is L47–50 (`BuildLogon` → `ssl.WriteAsync`). Tag 35 is also **read** from the reply (`Extract(reply, "35")` L55) to accept Logon `A`. That is inbound parse, not a NewOrderSingle send. `using` disposes `TcpClient` / `SslStream` after one read.

Hosted caller `CTraderFixLogonHostedService` (`src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` L48–58) calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No other MsgType. Log line L69: `NewOrderSingle still unimplemented`.

Copy hop still has no sender:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L18)
- persist `AllowFixSend = false` (L306) regardless of `decision.AllowFixSend`
- `VenueReconciled = false` (const L17)
- LIVE send branch L312 is unreachable because the const is false

**Residual (does not break claim 4, which names this type only):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` ×3 (`Sessions\CTraderFixDemoTestTrade.cs` L139 / L163 / L197). Demo-gated at L43–47 (refuses non-`demo-` host, non-`demo.` sender, `live.` / `live-`, account `1369850`). Called from `tools/DemoFixTestTrade`, not API / DI / copy.
- Claims that “the product tree has 0 `35=D` / 0 `Build("D")`” are **stale**. The assigned type `CTraderFixSession` has 0.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files prove it does **not**.

### 5.1 Lab env is armed

`D:\Prop\.env` line 73 (boolean only; **no secrets quoted**):

```
REAL_COPY_EXECUTION_ENABLED=true
```

Also `.env` L106 `FEATURE_COPY_TRADING_ENABLED=true` (display/pipeline; API `/api/settings` hardcodes that flag `true` at L77).

No committed `appsettings*.json` sets `REAL_COPY_EXECUTION_ENABLED`. `apps/api/appsettings.json` has `FeatureFlags.LiveCopyEnabled=false` — a **different name**, unread by DI L41.

POCO `CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`src\Fix.CTrader\Configuration\CTraderFixOptions.cs` L35). That POCO is **not** what DI binds. Architecture / README still say the flag should be false. Those docs are **not** the runtime path.

### 5.2 API loads that file

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`src\Mt5\Env\EnvFile.cs` L5–20) walks cwd parents and **hard-includes** `D:\Prop\.env`. It `SetEnvironmentVariable`s every `KEY=value` (L38). Then the host adds environment variables. The API process therefore sees L73.

### 5.3 DI binds it. Nothing re-pins it.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of product `*.cs` for `RealCopyEnabled =`: **only** that DI assignment. `CTraderFixLogonHostedService` logs `_runtime.RealCopyEnabled` (L70) and does **not** force it false. No hosted re-pin remains.

`/api/settings` echoes the runtime boolean (not a hardcoded false):

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

`/api/health` also exposes `realCopyEnabled = runtime.RealCopyEnabled` (L55).

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L46). The blocker `"REAL_COPY_EXECUTION_ENABLED is false"` (L478–479) is added **only when** `!_runtime.RealCopyEnabled`. With `.env` L73 + DI bind, that blocker is **absent** on the API host.

### 5.4 Split-brain (does not salvage claim 5)

`apps/fix-worker/Worker.cs` L21 reads **`CTrader:RealCopyExecutionEnabled`** with default **false** — a **different key** than `REAL_COPY_EXECUTION_ENABLED`. That worker only logs and stamps sessions `Disconnected`. It is not the API runtime flag.

`CREDENTIALS_AND_COPY_STATUS.md` / older “forced false” pins are **stale**.

Claim 5 as written (“stays false”) is **false** on the live API path.

---

## Capital / send

| Surface | Live send possible? |
|---|---|
| `CTraderFixSession` | **No** — only `(35, "A")`; one write; sockets disposed |
| `CopyTradingService` | **No** — `NewOrderSingleImplemented=false`; persist `AllowFixSend=false` |
| `CTraderFixLogonHostedService` | **No** — Logon only |
| `apps/fix-worker/Worker.cs` | **No** — stamps `Disconnected`; no FIX writer |
| `CTraderFixDemoTestTrade` | Off-hop CLI; demo-gated; not wired to copy/DI |

Next sender that **did** exist would see `LiveRuntimeStatus.RealCopyEnabled == true` on this host. That is an armed license with no gun. Do **not** add `35=D` while L73 is `true`.

---

## What this slot did not do

- Did not attach Achiever / Starwave Manager.
- Did not GET `:5000` (not required; claim 5 is already file-disproved).
- Did not flip `.env`.
- Did not print passwords, hosts-as-secrets, or FIX credentials.

---

## Verdict

**FAIL.** Claims 1–4 proved from live files (2–3 as source capability only). Claim 5 **FAIL**: `REAL_COPY_EXECUTION` does **not** stay false (`.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 + no hosted re-pin). Copy hop remains `SAFE_BY_ABSENCE`. Risk to capital **NONE**.
