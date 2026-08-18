# W500_VERIFY_63 — Adversarial live-path verify (slot 63)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **63** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** (the opposite is in the files). Claims 2–3 are **file-capability only** (this slot did not re-attach).

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` (160 lines) seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` calls `GroupRequestArray("*")` then, if the list is empty, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)` → every group from `GetGroupsCore`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file 135/135. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync` (Logon). |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` includes `D:\Prop\.env`. DI L41 binds that string onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

Overall **FAIL** because claim 5 cannot be proved — the live files show the flag is env-armed.

Destination risk this process: **NONE** (`SAFE_BY_ABSENCE`). `CTraderFixSession` cannot send `NewOrderSingle`. Copy persist forces `AllowFixSend=false`. Const `NewOrderSingleImplemented=false`. Residual: the next sender would see `RealCopyEnabled=true` on the API host.

---

## 1. DemoSeeder is not the API startup path — PASS

Read this slot: `D:\Prop\apps\api\Program.cs` (160 lines).

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
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`:
  - `apps/api/Program.cs` L156
  - `apps/mt5-worker/Program.cs` L15
  - `apps/fix-worker/Program.cs` L15

`DemoSeeder` C# call sites (product + tests, not reports):

- `src/Infrastructure/Seeding/DemoSeeder.cs` L14 — class still exists
- `tests/Integration/SeedingAndStoreTests.cs` L25 — test-only
- leftover `_tmp_*` harnesses under `reports/swarm/20260818/` — not API boot

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. Starwave `ProxyEnabled` is hardcoded `false`.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` remains on disk for tests. **API process does not call it.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a full `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup. Hosted ingest (`LiveIngestHostedService`) uses `SyncCatalogAsync` + `GetAccountsAsync(null)` + `ListLoginsWithDealsAsync` — not those four logins.
- Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read this slot: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

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

File-proven facts:

- Request path uses Manager mask `"*"` (all groups).
- Fallback `GroupTotal`/`GroupNext` runs **only if** the request path produced `list.Count == 0`.
- Dedup is name-based (`HashSet` ordinal-ignore-case).
- Live ingest calls this via `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (L45). Flag-blind: no `REAL_COPY` / `FEATURE_COPY` gate on fetch.

**Adversarial limits (why this is PASS_SOURCE, not live-complete):**

- This slot did **not** attach Achiever/Starwave. Cannot re-prove the 18-group census from a Manager reply.
- If `GroupRequestArray("*")` returns a **partial** non-empty set, `GroupTotal` is skipped. Completeness then depends on that RPC, not the fallback.
- `GroupTotal` enumerates the pump cache; `Connect` first tries `PUMP_MODE_GROUPS|USERS|POSITIONS` and falls back to `PUMP_MODE_NONE` (L89–110). Request APIs do not branch on `_pumpEnabled`.

Capability claim **proved from the file**. Runtime “all groups on this LAN” **not** re-proved this slot.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `GetAccountsCore` + `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        ...
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            ...
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
```

```216:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Live catalog path: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)` — **all groups**, not a hard-coded four-login set.

File-proven facts:

- Primary: `UserRequestArray(gname)`.
- Intermediate (not in the assigned claim, but present): `UserGetByGroup` if the request retcode is not OK / OK_NONE / NOTFOUND.
- Fallback: `UserLogins` + `UserRequestByLogins` when the user array is still empty.
- Dedup by login across groups.

**Adversarial limits:**

- `UserLogins` is **not** always called. It runs only when `users.Total()==0`.
- “All traders” is “every login returned for every group from `GetGroupsCore`.” If claim 2’s group list is partial, traders are partial.
- This slot did **not** re-attach. Cannot re-sum 8460 logins from a live JSON here.

Capability claim **proved from the file**. Runtime census **not** re-proved this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read this slot: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, entire file).

Outbound builder:

```89:110:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        ...
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            ...
            (554, password)
        };
        return Assemble(fields);
    }
```

File-proven facts:

- Tokens `35=D`, `NewOrderSingle`, `Build("D")`, `(35, "D")` in this file: **0**.
- Only outbound MsgType: `(35, "A")` Logon.
- Single `ssl.WriteAsync` (L49). TCP + SSL disposed via `using`.
- Inbound `35` is parsed only to accept `"A"` or record a reject (`Logon rejected 35={msgType}`).
- Product caller: `CTraderFixLogonHostedService` L48 + L54 `TryLogonAsync` (QUOTE 5211, TRADE 5212). Then persist. No order send.

Copy hop remains `SAFE_BY_ABSENCE`:

```16:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

```211:217:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
                    DecidedAt = now
                };
                ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
```

`AllowFixSend` is **persisted false** regardless of the risk engine’s in-memory `decision.AllowFixSend`. Even if that `if` were reached, the status written is `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — there is no FIX write.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade` (`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs`) has `Build("D", ...)` at L139, L163, L197. That class is **not** `CTraderFixSession`. Call site is `tools/DemoFixTestTrade/Program.cs` L44 only — not DI, not API, not `CopyTradingService`. Demo-gated: refuses `live-*` host, `live.` sender, account `1369850` (L43–59). Assigned claim is `CTraderFixSession` only.

`CTraderFixOptions.RealCopyExecutionEnabled` default remains `false` (POCO L35). Nothing in product binds env `REAL_COPY_EXECUTION_ENABLED` onto that POCO. The **runtime** flag is `LiveRuntimeStatus.RealCopyEnabled` (claim 5).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live files prove the opposite on the API host.

### 5.1 Lab env is armed

`D:\Prop\.env` L73 (only occurrence of this key in that file):

```
REAL_COPY_EXECUTION_ENABLED=true
```

(Boolean only. No secret quoted.) Adjacent non-secret: L106 `FEATURE_COPY_TRADING_ENABLED=true`.

Committed `apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. FeatureFlags there are `LiveCopyEnabled=false` (different name, unused by the live `/api/settings` lambda).

### 5.2 API loads that file into the process environment

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L8–15) candidates include `D:\Prop\.env` as a hard-coded last path. `Load` calls `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line.

### 5.3 DI binds the env string onto the process singleton

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

`LiveRuntimeStatus.RealCopyEnabled` is a public **settable** bool defaulting false only until this assignment.

### 5.4 Hosted FIX logon does **not** re-pin false

Read this slot: `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` (113 lines).

No assignment to `_runtime.RealCopyEnabled`. It **reads** the armed value and logs it:

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

Reports that still claim a logon/DI hard-false pin (W500_68 / W500_108 / CREDENTIALS “forced false” / older slots 3/63/83) are **stale**. This **is** slot 63 re-reading that pin: it is gone.

### 5.5 Settings API exposes the bound runtime flag

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

Not a hardcoded `false`. If `.env` loaded, a running API will advertise **true**.

`apps/api/Controllers/SettingsController.cs` still exists and maps `LiveCopyEnabled` (default false) — a **different** unused name. Minimal APIs in `Program.cs` own `GET /api/settings`.

### 5.6 Copy service treats the runtime flag as the arm

`CopyTradingService.GetStatusAsync` L44 `RealCopyArmed: _runtime.RealCopyEnabled`.

`BuildBlockers` L316–317 adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only when** `!_runtime.RealCopyEnabled`. When env is `true`, that blocker is **absent**. Other blockers still hold (`NewOrderSingleImplemented=false`, `VenueReconciled=false`, 0 LIVE traders, FIX not logged on).

FIX worker (`apps/fix-worker/Worker.cs` L21) reads **`CTrader:RealCopyExecutionEnabled`** (nested, default `false`) — log-only. It still stamps sessions `Disconnected` and never sends. That worker path stays false **unless** that nested key is set. It is **not** the API runtime flag.

POCO `CTraderFixOptions.RealCopyExecutionEnabled = false` is unused by DI. Do not confuse the unused POCO default with the live `LiveRuntimeStatus` bind.

**Claim 5 verdict:** cannot prove “stays false.” Files prove the API process will set `RealCopyEnabled=true` whenever `D:\Prop\.env` L73 is loaded. **FAIL.**

---

## Risk to capital

| Surface | Send possible? | Why |
|---|---|---|
| `CTraderFixSession` | **No** | Only `(35, "A")`. No `35=D` builder. |
| `CopyTradingService` | **No** | `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; no FIX write. |
| `CTraderFixLogonHostedService` | **No** | Two `TryLogonAsync` then persist. |
| `CTraderFixDemoTestTrade` | Demo-only, tools CLI | Not on copy/API/DI. Refuses live identity. Not invoked this slot. |
| Flag | **Armed** on API | `.env` + DI. Necessary, not sufficient. |

**Risk to capital: NONE** this process (`SAFE_BY_ABSENCE`). Dest tickets cannot leave `CTraderFixSession`. Residual: the **next** implemented sender would see runtime armed. Do not treat claim-5 FAIL as a live-send proof.

This slot did **not** live-attach Manager, did **not** GET `/api/settings`, did **not** send FIX.

---

## Stale documents (do not reuse)

| Doc | Why stale |
|---|---|
| A002 / A005 / A010 / A011 | API startup is `BrokerCatalogSeed`, not `DemoSeeder`. |
| CREDENTIALS_AND_COPY_STATUS.md “REAL_COPY false (forced)” | DI binds env; `.env` is `true`. |
| W500_68 / W500_108 / older “slot 63 hard-false pin” | Logon host no longer pins `RealCopyEnabled=false`. |
| W500_130 / W500_150 “product 35=D=0 / single FIX writer” | Sibling `CTraderFixDemoTestTrade` can `Build("D")` (tools-only, demo-gated). Assigned `CTraderFixSession` is still `35=A` only. |

---

## Files read this slot (primary evidence)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (sibling residual only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\.env` L73 + L106 (booleans only)

Product source was **not** edited.
