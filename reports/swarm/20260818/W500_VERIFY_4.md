# W500_VERIFY_4 — Adversarial live-path verify (slot 4)

| Field | Value |
|---|---|
| Slot | **4** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Product source edited | **No** |
| Test source edited | **No** |
| `.env` edited | **No** |
| Live attach this slot | **No** (no Manager Connect, no FIX TLS, no order) |
| Secrets printed | **None** (boolean flags only; no passwords, proxy auth, tag 554, or connection strings) |
| Verdict | **FAIL** |

## Assigned claims

Confirm from the live tree, not prior swarm notes:

1. `DemoSeeder` is **not** the API startup path.
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`.
3. Native connector can list **all** traders via `UserRequestArray` / `UserLogins`.
4. `CTraderFixSession` has **no** `35=D`.
5. `REAL_COPY_EXECUTION` **stays false**.

Rule used: **FAIL the claim (and the slot) if it cannot be proven from the file.** A comment, a dashboard chip, or a prior report is not proof.

## Files read this slot (primary evidence)

| Path | What was proven |
|---|---|
| `D:\Prop\apps\api\Program.cs` (160 lines) | Startup seed, env load, `/api/settings` flag source |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed = `BrokerCatalogSeed` |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer (not API seed) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed = `BrokerCatalogSeed` |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\appsettings.Development.json` | No flag |
| `D:\Prop\apps\api\Properties\launchSettings.json` | No `REAL_COPY_*` env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Dead MVC; different flag names |
| `D:\Prop\apps\api\TraderIntelligence.Api.csproj` | `net8.0-windows` x64; references Infra + Mt5 + Fix |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Exists; FakeMt5 + 10001…; **not** host-wired |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Native-only + **env-bound** `RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only; Fake never registered |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog via `SyncCatalogAsync` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow intents only |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const false; persist `AllowFixSend=false` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Traders = all `Mt5Accounts` (no Take) |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` is a settable bool |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request + fallbacks |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Type exists; not DI |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` into process env |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135) | Outbound `(35,"A")` only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Sibling residual `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; **no** re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false; **unbound** |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` needs `RealExecutionEnabled && Reconciled && VenueHealthy` |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Official API names exist |
| `D:\Prop\.env` L73 + L106 | Boolean flags **only** |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Probe uses same Native `GetGroups` / `GetAccounts(null)` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Only product `DemoSeeder` caller |

Grep this slot (product, not reports): `DemoSeeder` under `D:\Prop\apps` = **0**. `DemoSeeder.SeedAsync` product callers = tests + leftover `_tmp_*` eval hosts. `GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins` live only in `NativeMt5BrokerConnector.cs`. `CTraderFixSession.cs` has **0** `"D"` / `35=D` / `NewOrderSingle`.

---

## Scorecard

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | Proven: `Program.cs` L155–156 calls `BrokerCatalogSeed.EnsureAsync` only. 0 `DemoSeeder` tokens in `apps/`. |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS (capability)** | Proven from connector L155 + L174. **ALL-at-runtime not re-proven** (no attach). |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS (capability)** | Proven from connector L223 + L230 and `GetAccountsAsync(null)`. **ALL-at-runtime not re-proven.** |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Proven: 135/135 file; only outbound MsgType is `(35,"A")`. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | **Disproven from files.** `.env` L73 `=true`; DI L41 binds it; logon host does not re-pin. |

**Aggregate: FAIL** — claim 5 cannot be proven; the live bind is the opposite of “stays false.”

Claims 2–3 are **capability** PASS only. This slot refuses to stamp a live 18/8460 census. Completeness of Manager ACL + retcode is **unproven here**. That is **not** enough to fail “can list via those APIs.”

---

## 1. DemoSeeder is not the API startup path — PASS

Live API composition (`D:\Prop\apps\api\Program.cs`):

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

- The `using TraderIntelligence.Infrastructure.Seeding;` on L6 is the namespace of **both** seed types. The **call** is `BrokerCatalogSeed.EnsureAsync`. There is no `DemoSeeder` identifier in this file.
- Grep `DemoSeeder` under `D:\Prop\apps` = **0** (API + mt5-worker + fix-worker).
- Both workers seed the same catalog writer (`apps\mt5-worker\Program.cs` L15, `apps\fix-worker\Program.cs` L15).

DI fail-closes Fake and never registers it:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances only (`LiveMt5Registration.cs` L23–49). `FakeMt5BrokerConnector` is not referenced.

`BrokerCatalogSeed.EnsureAsync` writes Achiever/Starwave catalog rows, XAUUSD, kill switch, and **Disconnected** FIX placeholders. It does **not** call FakeMt5, does **not** ingest deals, does **not** score 10001.

### Residuals (do not fail claim 1)

| Residual | Why it is not API startup |
|---|---|
| `DemoSeeder.cs` still on disk | Tests + leftover `_tmp_*` hosts call `SeedAsync`. Hosts do not. |
| `DemoSeeder` still does `DemoBrokerFactory.CreateDefault()` and scores `{10001,10002,10003,99001}` | Only if a test/eval host invokes it. |
| `apps\mt5-worker\Worker.cs` L31–35 still rebuilds those four logins | **Worker loop**, not `DemoSeeder`, not `apps\api\Program.cs`. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). |
| Prior `A002_api_dummy_path.md` says API still calls `DemoSeeder` | **Stale** vs the 160-line file read this slot. |

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (capability)

`GetGroupsCore` (`NativeMt5BrokerConnector.cs`):

```152:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Official Manager headers confirm the names (`MT5APIManager.h`): `GroupTotal` L205, `GroupRequestArray` L212.

Callers that ask for the full catalog:

- `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`
- `DealIngestionService.SyncBrokerAsync` L61 `GetGroupsAsync`
- `LiveIngestHostedService` L56 `SyncCatalogAsync`
- `GetAccountsCore` with `group == null` L201–202 re-walks `GetGroupsCore()`
- `tools/LiveBrokerProbe/Program.cs` L25 `GetGroupsAsync`

There is **no** `Take(`/group-name allowlist in this walk. Mask is `"*"`. `_pumpEnabled` does **not** branch this method (request first; `GroupTotal` is the empty-list fallback).

### Honest limits (why this is not a live-census stamp)

1. `GroupTotal`/`GroupNext` runs **only if** `list.Count == 0`. A **partial** successful `GroupRequestArray("*")` is accepted as final.
2. Failed request (neither `OK` nor `OK_NONE`) is silent; then `GroupTotal` is tried. If pump is off (`PUMP_MODE_NONE` connect fallback, L101–111), `GroupTotal` can be 0.
3. This slot did **not** attach. Prior 8+10=18 group figures are **not** re-measured here.

Capability of the two named APIs: **proven from the file**. Runtime “all groups on the servers right now”: **unproven this slot**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

`GetAccountsCore` with `group == null` walks every name from `GetGroupsCore()` then `ReadAccountsForGroup` (L189–213). Catalog ingest calls `GetAccountsAsync(null)` (`DealIngestionService` L48).

```223:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Official names: `UserLogins` `MT5APIManager.h` L254; `UserRequestArray` L410.

Order: **request** `UserRequestArray` → hard-fail only then pump-cache `UserGetByGroup` → still empty then `UserLogins` + `UserRequestByLogins`. Dedup is `Dictionary<ulong, Mt5AccountDto>` across groups (L205–212). No `Take` on this path.

Dashboard `/api/traders` enumerates **all** `Mt5Accounts` rows (`EfDashboardQueries.GetTradersAsync` L89–128), not scores-only. Hosted **scoring** is narrower (`ListLoginsWithDealsAsync`) — that is a score-set residual, not a catalog-list cap.

### Honest limits

1. `UserLogins` runs only when `users.Total() == 0`. A **partial** `UserRequestArray` is accepted as final.
2. Completeness is manager ACL + retcode. Not re-attached this slot. Prior 6512+1948=8460 is **not** re-proven here.
3. `mt5-worker\Worker.cs` still scores four demo logins; that does **not** truncate Native `GetAccountsAsync(null)`.

Capability of the two named APIs: **proven from the file**. Runtime “all traders on the servers right now”: **unproven this slot**.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned type `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines, read whole).

Grep in **that file**:

| Token | Hits |
|---|---|
| `35=D` | **0** |
| `NewOrderSingle` | **0** |
| `"D"` | **0** |
| tag 35 | L55 extract inbound; L73 error text; L96 outbound **`"A"`** |

Only outbound assembler:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        // ...
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            // 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554
        };
        return Assemble(fields);
    }
```

One `ssl.WriteAsync` of that logon (L49). `using` disposes TCP/SSL. Hosted caller `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and nothing else on this type.

Copy hop cannot invent a ticket from this class: there is no NewOrderSingle method.

### Residual (does not fail the assigned-type claim)

Sibling `CTraderFixDemoTestTrade` writes `Build("D", ...)` at L139 / L163 / L197. `CTraderFixDemoMatrix.cs` L87 also `Build("D", ...)`. Those are **not** `CTraderFixSession`. Demo helper is gated to `demo-` host / `demo.` sender and refuses `live-*` / `live.` / account `1369850`. Invoked from `tools/DemoFixTestTrade`, not DI, not API, not `CopyTradingService`.

`CopyTradingService` still has `NewOrderSingleImplemented = false` (const L17) and persists `AllowFixSend = false` (L211).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The **policy** default is still false. The **live process flag does not stay false.** The claim is a live-path assertion, not a wish.

### Surfaces that are still false (not enough)

| Surface | Evidence |
|---|---|
| Architecture / docs | `docs/architecture.md` / README say `REAL_COPY_EXECUTION_ENABLED=false` |
| POCO default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35). **Unbound** — no `Configure<CTraderFixOptions>`. |
| Committed `appsettings.json` | `FeatureFlags.LiveCopyEnabled=false` — **different name**. No `REAL_COPY_EXECUTION_ENABLED` key. |
| `launchSettings.json` | No `REAL_COPY_*`. |
| FIX worker | `GetValue("CTrader:RealCopyExecutionEnabled", false)` (`Worker.cs` L21) — **different nested key**, log-only; still stamps sessions `Disconnected`. |
| Dead MVC | `SettingsController` reads `FeatureFlags:LiveCopyEnabled` (default false). `Program.cs` has **no** `AddControllers` / `MapControllers`. Live `/api/settings` is the minimal lambda. |
| Copy persist | `AllowFixSend = false` hardcoded (`CopyTradingService` L211). |
| NOS / recon consts | `NewOrderSingleImplemented = false`, `VenueReconciled = false`. |

### What the live files actually do (claim breaker)

1. Lab `D:\Prop\.env` **L73**: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; neighboring secrets not copied).
2. Same file **L106**: `FEATURE_COPY_TRADING_ENABLED=true` (API ignores this key; `/api/settings` hardcodes FEATURE true at `Program.cs` L77).
3. API `Program.cs` L10: `EnvFile.FindAndLoad()`. Candidates include hardcoded `D:\Prop\.env` (`EnvFile.cs` L14).
4. `EnvFile.Load` L38: `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value`.
5. `Program.cs` L13: `builder.Configuration.AddEnvironmentVariables()`.
6. DI **binds** the env token onto the process singleton — not a hard `false`:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

7. `CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled` and **does not** assign `false`. Older “hosted pin false” notes are **stale**.
8. Live `/api/settings` L76: `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` — follows the bind, not a hardcoded false.
9. `/api/health` L55 and `/api/ingest/status` expose `runtime.RealCopyEnabled`.
10. `CopyTradingService` L190 passes `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine.Evaluate`. Persist still forces `AllowFixSend=false`. `RiskEngine` L147–150 would allow send only if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. `Reconciled` is const false on this hop, so even an armed flag cannot authorize FIX today.

**Cannot prove “stays false.”** Files prove: env `true` + DI bind + no re-pin ⇒ API process **will arm** `LiveRuntimeStatus.RealCopyEnabled`.

Workers do **not** call `EnvFile.FindAndLoad()`. A worker started from a clean VS profile without process env would see the key **unset** → DI `RealCopyEnabled=false`. That split does **not** save the API claim. The assigned live path is the API host.

`CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” is **stale** vs `DependencyInjection.cs` L41.

---

## Risk to capital

**NONE today (`SAFE_BY_ABSENCE`).**

| Gate | State this slot |
|---|---|
| `CTraderFixSession` `35=D` | **Absent** (Logon `35=A` only) |
| Copy `NewOrderSingleImplemented` | const **false** |
| Persist `AllowFixSend` | hardcoded **false** |
| `VenueReconciled` | const **false** |
| Native walk | read-only Manager request/cache |
| Demo `Build("D")` | tools-only + demo-gated; not on copy hop |

**Residual (why claim 5 still FAILs):** if a sender is added later, this lab API host will already have `RealCopyEnabled=true`. That is an armed bit with no ticket — not a license, and not “stays false.”

---

## What this slot did not do

- Did not attach to Achiever or StarwaveFX.
- Did not re-sum any 18/8460 census.
- Did not open FIX TLS.
- Did not invoke `tools/DemoFixTestTrade`.
- Did not print passwords, proxy auth, FIX tag 554, or connection strings.
- Did not edit product source or `.env`.

---

## Verdict

**FAIL**

| Claim | Result |
|---|---|
| 1 DemoSeeder not API startup | **PASS** (proven from `apps/api/Program.cs`) |
| 2 Groups via `GroupRequestArray` / `GroupTotal` | **PASS** capability from `NativeMt5BrokerConnector.cs`; ALL-at-runtime unproven |
| 3 Traders via `UserRequestArray` / `UserLogins` | **PASS** capability from same file; ALL-at-runtime unproven |
| 4 `CTraderFixSession` has no `35=D` | **PASS** (135/135; outbound `35=A` only) |
| 5 `REAL_COPY_EXECUTION` stays false | **FAIL** — `.env` L73 `true` + DI L41 bind + no hosted re-pin |

One-line:

```text
Slot 4 FAIL. DemoSeeder not on API start (BrokerCatalogSeed only). Native can walk groups/traders via GroupRequestArray|GroupTotal and UserRequestArray|UserLogins (ALL not re-attached). CTraderFixSession is 35=A only. REAL_COPY does not stay false: lab .env L73=true, DI binds it. Capital NONE (SAFE_BY_ABSENCE).
```
