# W500_VERIFY_23 — Adversarial live-path re-read (slot 23)

| Field | Value |
|---|---|
| Agent | W500_VERIFY_23 |
| Slot | 23 |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source | **Not modified.** Report only. |
| Secrets printed | **None** (boolean flags only). |

**Honesty rule:** prove each claim from the file on disk. If a claim cannot be proven, verdict is **FAIL**. Partial evidence is not a PASS.

---

## Verdict

**FAIL** — conjunction of five claims. Claims 1–4 are proven from product files. Claim 5 is **disproven**.

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | API `Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS (capability)** | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty-list fallback L174 `GroupTotal()` + `GroupNext`. This slot did **not** live-attach. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS (capability)** | `ReadAccountsForGroup` L223 `UserRequestArray`; L230 `UserLogins` + `UserRequestByLogins` when `users.Total()==0`. `GetAccountsAsync(null)` walks every group. Not live-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: only outbound MsgType is `(35, "A")` L96. One `WriteAsync` (L49). Zero `NewOrderSingle` / `"D"` / `35=D`. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | DI binds env (`DependencyInjection.cs` L41). Lab `.env` L73 is `true`. API loads `.env` (`Program.cs` L10). Hosted FIX logon does **not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

**Bottom line:** dummy seed is off the API host. Manager request APIs are wired for catalog-wide groups/users. Hosted FIX session cannot emit NewOrderSingle. **The runtime copy-arm flag does not stay false.** Live send is still blocked by absence of a sender (`NewOrderSingleImplemented=false`, persist `AllowFixSend=false`), not by a pinned-false flag.

`reports/CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” and A014 “DI pins `RealCopyEnabled=false`” are **STALE**.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 What the API actually runs

`D:\Prop\apps\api\Program.cs` (160 lines). Startup after maps:

```152:158:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

Zero `DemoSeeder` identifiers in this file. `using TraderIntelligence.Infrastructure.Seeding;` (L6) is required for `BrokerCatalogSeed` only.

Same seed on both workers:

- `D:\Prop\apps\mt5-worker\Program.cs` L11–16: `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\fix-worker\Program.cs` L11–16: `BrokerCatalogSeed.EnsureAsync`

Grep `DemoSeeder` under `D:\Prop\apps` (all `*.cs`): **0 hits**.

### 1.2 Where DemoSeeder still lives (not API startup)

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists. It still composes `DemoBrokerFactory.CreateDefault()` and scores `{10001,10002,10003,99001}`. Callers found in this tree:

- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25
- swarm `_tmp_*` eval hosts (not product)

DI refuses Fake/dummy connectors:

```36:37:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
```

`LiveMt5Registration.CreateConnectors` instantiates only `NativeMt5BrokerConnector` (Achiever + Starwave). No `FakeMt5BrokerConnector` registration on the host path.

### 1.3 Residual (does not flip claim 1)

`D:\Prop\apps\mt5-worker\Worker.cs` L31–35 still **scores** `{10001,10002,10003,99001}` after a real `SyncBrokerAsync`. That is a leftover four-login scorer, **not** `DemoSeeder` on API startup. Hosted ingest (`LiveIngestHostedService`) scores `ListLoginsWithDealsAsync`, not those four.

---

## 2. Groups via GroupRequestArray or GroupTotal — PASS (capability)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore`:

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

Proven from the file:

- Primary walk is network `GroupRequestArray("*")` (all-groups mask).
- Fallback is pump/cache `GroupTotal()` + `GroupNext`.
- Dedup by name (`HashSet` in `AddGroup`).

Not proven from the file (residual, does not fail the capability claim):

- This slot did **not** attach to Achiever/Starwave. Completeness vs live Manager is not re-measured here.
- If `GroupRequestArray` returns a **non-empty partial** set, `GroupTotal` is skipped (`list.Count == 0` gate). That is a completeness hole, not an absence of the APIs.
- Prior census 18/8460 in `CREDENTIALS_AND_COPY_STATUS.md` is **other-agent / prior-probe**; not used as proof.

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` → `GetGroupsAsync` then `GetAccountsAsync(null)`.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

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

`GetAccountsAsync(null)` (L189–213) first calls `GetGroupsCore()`, then `ReadAccountsForGroup` for **every** group name, keyed by login.

Catalog path: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)` — flag-blind, no `Take`.

Residual (capability still holds):

- `UserLogins` runs only when `users.Total()==0`. Partial `UserRequestArray` success will not backfill via logins.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), not every catalog login. Catalog persist is still all users returned by the connector.
- Manual `/api/ops/resync` scores `ListLoginsAsync` (all persisted accounts).
- This slot did not live-attach; “8460 traders” is not re-proven here.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Grep in that file for `35=D`, `(35, "D")`, `"D"`, `NewOrderSingle`: **0 hits**.

Only outbound message:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            // 49/56/50/57/52/98/108/141/553/554
        };
        return Assemble(fields);
    }
```

Single socket write: L49 `ssl.WriteAsync(bytes, ...)`. TCP/SSL disposed via `using`. Inbound tag `35` is parsed (L55) only to accept Logon `A` or record reject. Sockets are not kept for later NewOrderSingle.

Hosted caller `CTraderFixLogonHostedService` only calls `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212). Log line L69: “NewOrderSingle still unimplemented.”

Copy hop cannot emit `35=D` even if TRADE logs on:

```16:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

```211:211:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
```

### Sibling (not the assigned type)

`CTraderFixDemoTestTrade.Build("D")` exists (L139 / L163 / L197). That is **not** `CTraderFixSession`. It is demo-gated (`demo-` host / `demo.` sender / refuse account `1369850`) and invoked from `tools/DemoFixTestTrade`, not DI / API / copy hosted service. Claim 4 is about `CTraderFixSession` and is proven.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim is that the flag **stays false**. Files prove the opposite on the live API host.

### 5.1 DI binds the env key (no pin)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is no `RealCopyEnabled = false` override after this.

### 5.2 API loads `.env` before configuration bind

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes `D:\Prop\.env` (hard path at `EnvFile.cs` L14).

Lab `.env` (boolean only):

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

Therefore `LiveRuntimeStatus.RealCopyEnabled` becomes **true** when this API starts with the lab env.

### 5.3 Hosted FIX logon does not re-pin

`CTraderFixLogonHostedService.ExecuteAsync` reads the already-bound `_runtime.RealCopyEnabled` and logs it (`RealCopyArmed={Armed}` L69–70). No assignment to `false`.

### 5.4 API surface exposes the armed flag

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

`FEATURE_COPY_TRADING_ENABLED` is a **literal true** (env unused). `REAL_COPY` is the runtime bool, not a hardcoded false.

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L44). `RiskEngine.Evaluate` receives `RealExecutionEnabled = _runtime.RealCopyEnabled` (L190). Persist still **overwrites** `AllowFixSend = false` (L211).

### 5.5 What is still false (does not salvage claim 5)

| Surface | Value | Bound to env `REAL_COPY_EXECUTION_ENABLED`? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` | **No** — POCO unused by DI / logon host |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **No** — different name; unused by minimal API |
| `apps/fix-worker/Worker.cs` `CTrader:RealCopyExecutionEnabled` | default `false` | **No** — log-only; stamps sessions Disconnected |
| `CopyTradingService.NewOrderSingleImplemented` | const `false` | No |
| Persist `RiskDecisionRecord.AllowFixSend` | forced `false` | No |

POCO default false + sender absence ≠ “REAL_COPY stays false.” The named flag is **armed** on the API process.

Cannot prove claim 5 from the files. Claim 5 is **false**. Conjunction → **FAIL**.

---

## Risk to capital

**NONE today** (`SAFE_BY_ABSENCE`).

- Hosted FIX writer is Logon `35=A` only. No `35=D` in `CTraderFixSession`.
- Copy pipeline const `NewOrderSingleImplemented=false` and persist `AllowFixSend=false`.
- No `ExecutionIntent` send hop on this path.
- Armed env flag is a **future-sender** hazard, not a ticket today.

If a NewOrderSingle writer is added without re-pinning the flag, the next hop would see `RealCopyEnabled=true`. That is why claim 5 is FAIL even though capital is not at risk from this process yet.

---

## What this slot did not do

- No live Manager attach. Group/trader counts not re-summed.
- No GET `/api/settings` against a running host (file-level bind is enough to fail claim 5).
- No product edits.
- No secrets printed.

---

## Stale documents (do not cite as current)

| Doc | Stale claim |
|---|---|
| `reports/CREDENTIALS_AND_COPY_STATUS.md` L30 | `REAL_COPY_EXECUTION_ENABLED` **false (forced)** |
| `reports/swarm/20260818/A014_live_path_now.md` L79–80 | `RealCopyEnabled = false` comment + pin |
| `reports/swarm/20260818/A002_api_dummy_path.md` | API still calls `DemoSeeder` |
| W500_68 / W500_108 “flag pinned false in DI/hosted” | Pin removed; env bind is live |
