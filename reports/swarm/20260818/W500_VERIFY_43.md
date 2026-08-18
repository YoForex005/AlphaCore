# W500_VERIFY_43 — Adversarial live-path verify (slot 43)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **43** |
| Role | Adversarial verifier. Read live path files independently. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files). Claims 1–4 are file-proven.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160/160) seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; if empty, L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray` first; if `users.Total()==0`, L230 `UserLogins` + `UserRequestByLogins`. Catalog `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Grep `35=D` / `(35, "D")` / `NewOrderSingle` = **0**. Only outbound MsgType is `(35, "A")` L96. One `WriteAsync`. Sockets `using`-disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin. Only assignment of `RealCopyEnabled =` in `src` is that bind. |

Overall **FAIL** because claim 5 cannot be proved (files show the flag is armed).

Destination capital risk this slot: **NONE** (`SAFE_BY_ABSENCE`). Armed flag cannot emit a ticket: `CTraderFixSession` is `35=A` only; `CopyTradingService.NewOrderSingleImplemented=false`; persist `AllowFixSend=false`.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot (full files):

- `D:\Prop\apps\api\Program.cs` (160 lines)
- `D:\Prop\apps\mt5-worker\Program.cs` (18 lines)
- `D:\Prop\apps\fix-worker\Program.cs` (18 lines)
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (112 lines)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header + class only)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs` (62 lines)
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` (94 lines)

API startup seed is catalog-only:

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
- Remaining `DemoSeeder` C# callers: `tests/Integration/SeedingAndStoreTests.cs` L25 plus report-scratch `_tmp_*` programs. **Not** the API process.

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. No substitution on the throw path.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

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
```

Facts from this file:

- Primary enumerator is the **request** API `GroupRequestArray("*")` (mask `*` = all manager-visible groups).
- Fallback is pump-cache `GroupTotal` / `GroupNext` **only if** the request list is empty.
- `_pumpEnabled` is never consulted in `GetGroupsCore`. Request-first even after `Connect(..., PUMP_MODE_NONE)` (L101–110).
- Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` → `GetGroupsCore`.

Adversarial limits (do **not** over-claim):

- This slot did **not** live-attach. Completeness (18 groups / 8460 traders from prior 08:42Z JSON) is **not re-proved** here.
- `GroupTotal` is a cache walk. If request fails with a code other than OK/OK_NONE **and** the cache is cold, the method returns empty without throwing. Claim is **capability** (`can` list via those APIs), not a measured census.

Verdict for the assigned wording (“can list … via GroupRequestArray **or** GroupTotal”): **PASS_SOURCE**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `GetAccountsCore` + `ReadAccountsForGroup`.

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

Facts:

- ALL-traders path is `GetAccountsAsync(null)` → every name from `GetGroupsCore` → `ReadAccountsForGroup`.
- Primary user pull is **network** `UserRequestArray`.
- `UserGetByGroup` (pump cache) only on **hard** request failure (not OK / OK_NONE / NOTFOUND).
- Empty after that → `UserLogins` + `UserRequestByLogins`.
- Ingest catalog: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)`. Same for `SyncBrokerAsync` L62. Hosted ingest (`LiveIngestHostedService`) calls `SyncCatalogAsync` per connector.

Adversarial limits:

- Completeness not re-attached this slot.
- `UserGetByGroup` remains a cache fallback; it is **not** the primary ALL path.
- Dedup is by login dictionary, so overlapping group masks do not double-count.

Verdict: **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135/135**).

This-slot grep of that file for `35=D`, `(35, "D")`, `NewOrderSingle`: **0 hits**.

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

Session body: one `TcpClient` + `SslStream`, one `WriteAsync` of that Logon, one `ReadAsync`, then `using` disposes both sockets. No heartbeat loop, no `35=D`, no `35=F`/`35=G`, no tag 38/11 builder.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and logs `NewOrderSingle still unimplemented`. It never builds a NewOrderSingle.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 is **not** `CTraderFixSession`. It is demo-gated (refuses `live-*` / `live.` / account `1369850`) and called only from `tools/DemoFixTestTrade` (0 hits from API / DI / workers). Assigned claim is the session class.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim requires the flag to **remain false** on the live path. Files show it is **armed**.

### 5.1 Lab env is true

`D:\Prop\.env` L73 (boolean only; no secrets quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

(L106 `FEATURE_COPY_TRADING_ENABLED=true` is a different flag; API `/api/settings` hardcodes that one `true` at L77.)

### 5.2 API loads that file into process environment

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs`) walks CWD parents then **`D:\Prop\.env`**, then `Environment.SetEnvironmentVariable` for every `KEY=value` line. `AddEnvironmentVariables()` then exposes those keys on `IConfiguration`.

### 5.3 DI binds the env key. No re-pin exists.

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

This-slot grep of `D:\Prop\src` for `RealCopyEnabled =`: **exactly one** assignment — DI L41.

`CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for a log line (L70) and does **not** write it. W500_68 / W500_108 “hosted pin-false” is **stale**.

### 5.4 API surface echoes the armed runtime

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

`LiveRuntimeStatus.Snapshot()` L42–44 also advertises “REAL_COPY armed…” when the bool is true.

### 5.5 POCO default is unused

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). Nothing in product binds `REAL_COPY_EXECUTION_ENABLED` onto that POCO. `apps/fix-worker/Worker.cs` L21 reads nested `CTrader:RealCopyExecutionEnabled` (default false) for a **log-only** warning and still stamps sessions `Disconnected`. That worker fallback does **not** keep the API host flag false.

### 5.6 Copy hop still cannot send (does not rescue claim 5)

Claim 5 is about the **flag staying false**, not about whether a ticket can leave. The flag does **not** stay false. Separate fail-closed facts:

| Gate | File | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const bool` **false** |
| `VenueReconciled` | same L16 | `const bool` **false** |
| Persist `AllowFixSend` | same L211 | **hardcoded `false`** (ignores `decision.AllowFixSend`) |
| Live-send `if` | same L217 | requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — unreachable |
| Intent status | L223 | `"SHADOW_ONLY"` |
| Session outbound | `CTraderFixSession` L96 | `(35, "A")` only |

`BuildBlockers` L316 only *adds* `"REAL_COPY_EXECUTION_ENABLED is false"` when the runtime bool is already false. When env-armed, that blocker is **absent**. Next sender would see runtime armed.

Verdict for claim 5: **FAIL**.

---

## Capital / no-loss

| Question | Answer |
|---|---|
| Can this process send a live NewOrderSingle today? | **No.** Assigned session has no builder. Copy const unimplemented. Persist `AllowFixSend=false`. |
| Is `REAL_COPY_EXECUTION` false? | **No.** Lab env + DI bind it **true** on the API host. |
| Dest capital at risk this slot | **NONE** (`SAFE_BY_ABSENCE`) |
| Would dest be at risk if a sender were added tomorrow? | **Yes**, unless the flag is re-pinned false **and** risk/recon gates exist. HEAD policy `AllocationFactor=1m` is 1:1. |

This slot did not live-attach Manager, did not call localhost, and did not print passwords, FIX secrets, connection strings, or proxy credentials.

---

## Stale citations (do not reuse)

| Citation | Why stale |
|---|---|
| A002 / A005 / A010 / A011 “API startup = DemoSeeder” | Current API seeds `BrokerCatalogSeed` only |
| A001 “zero `GroupRequestArray` / `UserRequestArray` under `src`” | Both are in `NativeMt5BrokerConnector` now |
| W500_68 / W500_108 / CREDENTIALS “REAL_COPY forced false / hosted re-pin” | DI L41 binds env; logon host no longer writes the bool |
| W500_148 “product only 35=A” as a **repo-wide** claim | Sibling `CTraderFixDemoTestTrade.Build("D")` exists (off-hop, demo-gated). Assigned `CTraderFixSession` is still `35=A` only |

---

## Verdict

**FAIL.**

1. DemoSeeder **not** API startup — **PASS**
2. Native **can** list groups via `GroupRequestArray("*")` or `GroupTotal` — **PASS_SOURCE** (not re-attached)
3. Native **can** list traders via `UserRequestArray` / `UserLogins` — **PASS_SOURCE** (not re-attached)
4. `CTraderFixSession` has **no** `35=D` — **PASS** (135/135)
5. `REAL_COPY_EXECUTION` stays false — **FAIL** (`.env` L73 `true` + DI L41 + no re-pin)

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
