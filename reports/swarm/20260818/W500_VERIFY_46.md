# W500_VERIFY_46 — Adversarial live-path verify (slot 46)

| Field | Value |
|---|---|
| Slot | **46** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Assigned claims | (1) `DemoSeeder` is not the API startup path. (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`. (3) All traders via `UserRequestArray` / `UserLogins`. (4) `CTraderFixSession` has no `35=D`. (5) `REAL_COPY_EXECUTION` stays false. |
| Rule | **FAIL the slot if any claim cannot be proven from the file.** Never print secrets. |
| Product source | **Not modified.** Report only. |
| Live Manager attach this slot | **No.** Runtime group/trader counts are **unproven** here. |
| Live `35=D` sent | **No** (assigned session class has no NewOrderSingle builder). |
| Secret values printed | **None** (quoted only boolean keys `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`). |
| `REAL_COPY` flipped this slot | **No** |

---

## Verdict

**FAIL**

Claims 1–4 are proven from the files on disk this slot (2–3 as **source capability**; live completeness not re-attached). Claim 5 is **disproved**: lab `D:\Prop\.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`, the API loads that file, and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted FIX logon does **not** re-pin the flag false.

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; if the list is empty, L174 `GroupTotal()` + `GroupNext`. Live ALL not re-probed. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `GetAccountsAsync(null)` walks every group; `ReadAccountsForGroup` L223 `UserRequestArray`, empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest calls `GetAccountsAsync(null)`. Live ALL not re-probed. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135. Only outbound MsgType is `(35, "A")` L96. Zero `35=D` / `(35, "D")` / `NewOrderSingle`. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | `.env` L73 `true`. `EnvFile.FindAndLoad` + `AddEnvironmentVariables`. DI L41 binds it. Logon host does not overwrite. `/api/settings` and `/api/health` expose `runtime.RealCopyEnabled`. |

**Slot rule:** one unproven/false claim → **FAIL**.

**Risk to capital:** **NONE** on the copy hop (`SAFE_BY_ABSENCE`). `CTraderFixSession` cannot emit NewOrderSingle. `CopyTradingService.NewOrderSingleImplemented` is `const false`. Persist writes `AllowFixSend = false` even if `RiskEngine` would approve an armed flag. That does **not** rescue claim 5. The flag itself does **not** stay false.

---

## Files read this slot (independent)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` (160 lines) | Startup seed + flag surfaces |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed (corroboration) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed (corroboration) |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual four-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` (log-only) |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\apps\api\appsettings.Development.json` | Same |
| `D:\Prop\apps\api\Properties\launchSettings.json` | No flag pin |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Exists; not host-wired |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Env bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog walk |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick; no flag pin |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const; persist `AllowFixSend=false` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` is settable |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135) | Outbound FIX |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Sibling residual only; not DI |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon; **no** re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Unused POCO default `false` |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` L147–150 | Engine can set `AllowFixSend` from armed flag |
| `D:\Prop\src\Domain\Copy\XauUsdOneToOneCopyPolicy.cs` L66 | `AllocationFactor = 1m` (paper-only today) |
| `D:\Prop\.env` L73 + L106 | Boolean flags only — no other `.env` values quoted |

Grep this slot: `DemoSeeder`, `REAL_COPY_EXECUTION`, `RealCopyEnabled`, `GroupRequestArray`/`GroupTotal`, `UserRequestArray`/`UserLogins`, `(35, "` / `35=D` / `Build("D")`, `AllowFixSend`, `FakeMt5BrokerConnector` in Infrastructure.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 What the API process actually runs

`D:\Prop\apps\api\Program.cs` after endpoint maps:

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- Product host callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- Remaining `DemoSeeder.SeedAsync` caller: `tests/Integration/SeedingAndStoreTests.cs` L25 only

### 1.2 DI fail-closes Fake; Native only

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

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` tokens under `src/Infrastructure`.

`BrokerCatalogSeed` writes broker/instrument/kill-switch/FIX-`Disconnected` rows. It does **not** ingest FakeMt5 deals or score `{10001,10002,10003,99001}`.

### 1.3 Residual (does not revive claim 1)

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14) and still scores those four logins **if tests call it**. API process does not.
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a real `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup.
- Hosted ingest scoring is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), not the demo quartet.
- Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

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

Proven from the file:

- Primary walk is `GroupRequestArray("*")` (all-groups mask).
- Fallback is `GroupTotal` + `GroupNext` **only when the request list is empty**.
- Ingest (`DealIngestionService.SyncCatalogAsync` L45) calls `GetGroupsAsync` with no filter.

Adversarial caveats (do **not** over-claim):

- If `GroupRequestArray("*")` returns a **non-empty subset**, `GroupTotal` is skipped. Completeness then depends on the Manager RPC, which this slot did not attach to prove.
- Prior census numbers (18 groups / 8460 traders) are **not re-measured** here. This slot does not treat them as proof.

Capability claim **PASS_SOURCE**. Live “ALL groups returned” **unproven** this slot.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

`GetAccountsAsync(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

Catalog ingest:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Proven from the file:

- Unfiltered catalog walk: `GetAccountsAsync(null)` → every group name → `UserRequestArray`.
- Empty-array fallback: `UserLogins` + `UserRequestByLogins`.
- Dedup by login (`byLogin` dictionary).

Adversarial caveats:

- `UserLogins` runs only when `users.Total() == 0`. A non-empty **partial** `UserRequestArray` skips the login-array fallback.
- Hosted **scoring** is `ListLoginsWithDealsAsync`, not all catalog logins. Listing/catalog ≠ auto-score.
- Worker residual still scores the four demo logins. That does not replace the Native walk.
- Live “ALL traders returned” **unproven** this slot (no Manager attach).

Capability claim **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read 135/135.

Grep in that file this slot:

- `(35, "` → **one** hit: L96 `(35, "A")`
- `35=D` / `NewOrderSingle` / `Build("D")` → **0**

Only wire write:

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

Sockets are created inside `using`/`await using` and disposed at method end. Hosted service calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and never keeps a session for later order send.

### Residual (does not falsify claim 4)

Claim is specifically `CTraderFixSession`. Sibling helpers **can** `Build("D")`:

- `CTraderFixDemoTestTrade.cs` — demo-gated (`demo-` host / `demo.` sender; refuses account `1369850` and `live-*` / `live.`). **0** references from `apps/` or `Infrastructure`. Tools CLI only.
- `CTraderFixDemoMatrix.cs` — same family; not on copy hop.

Copy hop consts:

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

Persist always:

```211:211:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

`CopyTradingHostedService` only calls `GenerateShadowIntentsAsync`. No FIX writer.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. The live files show the opposite.

### 5.1 Lab env is true

`D:\Prop\.env` L73 (boolean only):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (not the assigned flag, but same file):

```
FEATURE_COPY_TRADING_ENABLED=true
```

No other `.env` values are quoted.

### 5.2 API loads that file and binds the string

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` includes hard path `D:\Prop\.env` (`EnvFile.cs` L14) and writes each key into `Environment`. `AddEnvironmentVariables()` then surfaces it on `IConfiguration`.

DI:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** hardcoded `RealCopyEnabled = false` after this bind.

### 5.3 Hosted FIX logon does not re-pin

`CTraderFixLogonHostedService` reads `_runtime.RealCopyEnabled` only to log it (L68–70). It never assigns `false`. `CopyTradingHostedService` never touches the flag.

### 5.4 Surfaces that will advertise armed

```55:55:D:\Prop\apps\api\Program.cs
        realCopyEnabled = runtime.RealCopyEnabled,
```

```74:77:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`appsettings.json` / `appsettings.Development.json` / `launchSettings.json` do **not** pin the flag false. `CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` but is **not** bound onto `LiveRuntimeStatus` (no `Configure<CTraderFixOptions>` in DI). Fix-worker reads a **different** key `CTrader:RealCopyExecutionEnabled` (default false, log-only) and stamps sessions `Disconnected` — that is split-brain, not a re-pin of the API runtime flag.

### 5.5 Stale “forced false” docs

`reports/CREDENTIALS_AND_COPY_STATUS.md` still says `REAL_COPY_EXECUTION_ENABLED` is **false (forced)**. Architecture/README/docs still print `=false` as policy. Those documents are **not** the running bind. W500_RESEARCH_68/108 “pinned false” is **stale** against DI L41 + `.env` L73.

### 5.6 Armed flag ≠ live send (does not rescue claim 5)

`RiskEngine` L147–150 **can** set `AllowFixSend` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy persist **overwrites** to `AllowFixSend = false`. Live send still requires a missing NewOrderSingle builder plus `NewOrderSingleImplemented && VenueReconciled && LIVE` (CopyTradingService L217). Capital is safe **by absence**. Claim 5 is still **FAIL**.

---

## Risk to capital

**NONE** on destination capital from this process (`SAFE_BY_ABSENCE`).

| Gate | State |
|---|---|
| `CTraderFixSession` outbound MsgType | `A` only |
| Hosted session lifetime | one-shot logon, then dispose |
| `NewOrderSingleImplemented` | `const false` |
| `VenueReconciled` | `const false` |
| Persist `AllowFixSend` | always `false` |
| Copy hosted loop | shadow intents only |
| Policy lots | `AllocationFactor = 1m` (ruin **if** a sender existed) |
| `REAL_COPY_EXECUTION_ENABLED` | **true** in lab env + DI-bound |

If a NewOrderSingle sender is added later while the env flag stays `true` and allocation is 1:1, risk flips to **HIGH**. Today there is no builder on the copy hop.

---

## What this slot did not do

- Did not attach Manager API / did not re-count groups or logins.
- Did not GET a live `:5000` process.
- Did not send FIX.
- Did not modify product source, `.env`, or flip `REAL_COPY`.
- Did not print secrets.

---

## Bottom line

Slot 46 **FAIL**. DemoSeeder is off the API startup path. Native request APIs for groups and traders are wired (`GroupRequestArray`/`GroupTotal`, `UserRequestArray`/`UserLogins`). `CTraderFixSession` cannot emit `35=D`. `REAL_COPY_EXECUTION` does **not** stay false.
