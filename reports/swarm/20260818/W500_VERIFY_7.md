# W500_VERIFY_7 — Adversarial live-path verify (slot 7)

| Field | Value |
|---|---|
| Slot | **7** |
| Date | 2026-08-18 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source edited | **No** |
| Live attach this slot | **No** |
| Secrets printed | **None** (boolean flags only) |
| Verdict | **FAIL** |

## Assigned claims

Confirm from the live tree, not prior swarm notes:

1. `DemoSeeder` is **not** the API startup path.
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`.
3. Native connector can list **all** traders via `UserRequestArray` / `UserLogins`.
4. `CTraderFixSession` has **no** `35=D`.
5. `REAL_COPY_EXECUTION` **stays false**.

Rule: **FAIL if any claim cannot be proven from the file.**

## Files read (this slot)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` (160 lines) | API startup seed + `/api/settings` flag binding |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer (not API startup) |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` (log-only) |
| `D:\Prop\apps\api\appsettings.json` | Committed flags (no `REAL_COPY_EXECUTION_ENABLED` key) |
| `D:\Prop\apps\api\appsettings.Development.json` | No REAL_COPY key |
| `D:\Prop\apps\api\Properties\launchSettings.json` | No REAL_COPY env |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Dead MVC surface; not the mapped `/api/settings` |
| `D:\Prop\docker-compose.yml` | No REAL_COPY env |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Exists; not host-wired |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Binds `REAL_COPY` + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog walk |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow-only loop |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Persist `AllowFixSend=false`; NOS const false |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` is settable |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `AllowFixSend` follows `RealExecutionEnabled` in-memory |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/user request APIs |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Exists; not registered by DI |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | Loads `D:\Prop\.env` into process env |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135) | Outbound FIX |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Sibling residual only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | Sibling residual only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon; **no** re-pin of `RealCopyEnabled` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default `false` |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` | Only remaining `DemoSeeder.SeedAsync` caller in product tests |
| `D:\Prop\.env` L73 + L106 | Boolean flags only (no secret values copied here) |

## Scorecard

| # | Claim | Verdict | Proof status |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | Proven from `apps\api\Program.cs` L152–156 (`BrokerCatalogSeed` only) |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** | Proven as **capability** from `NativeMt5BrokerConnector.GetGroupsCore`. This slot did **not** live-attach. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** | Proven as **capability** from `ReadAccountsForGroup`. This slot did **not** live-attach. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Proven: 135/135 file; grep 0 `35=D` / 0 `NewOrderSingle`; only outbound `(35, "A")` |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | **Disproven.** Lab `.env` L73 is `true`; API loads it; DI binds it; logon host does not re-pin. |

**Aggregate: FAIL** because claim 5 cannot be proven (the live binding is the opposite of “stays false”).

---

## 1. DemoSeeder is not the API startup path — PASS

`D:\Prop\apps\api\Program.cs` startup after route maps:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Independent greps this slot:

- `DemoSeeder` token count in `D:\Prop\apps\**\*.cs` = **0**.
- All three hosts seed `BrokerCatalogSeed.EnsureAsync` only:
  - `apps\api\Program.cs` L156
  - `apps\mt5-worker\Program.cs` L15
  - `apps\fix-worker\Program.cs` L15
- `tools\` `*.cs` has **0** `DemoSeeder` / `FakeMt5` / `BrokerCatalogSeed` hits.

DI fail-closes without real MT5 passwords and registers Native only:

```36:48:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs two `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector` registrations.

**Residual (does not fail the claim):**

- `DemoSeeder` still exists at `src\Infrastructure\Seeding\DemoSeeder.cs` and still calls `DemoBrokerFactory.CreateDefault()` (FakeMt5) then scores `{10001,10002,10003,99001}`.
- Product-test caller: `tests\Integration\SeedingAndStoreTests.cs` L25 `DemoSeeder.SeedAsync`.
- `apps\mt5-worker\Worker.cs` L31 still scores the same four dummy logins after a **live** `SyncBrokerAsync`. That is **not** API startup and does **not** put `DemoSeeder` back on the API path.

Prior notes that say API `Program.cs` still calls `DemoSeeder.SeedAsync` (e.g. `A002_api_dummy_path.md`) are **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (capability)

`GetGroupsCore` (read this slot):

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

Live catalog uses that walk:

- `DealIngestionService.SyncCatalogAsync` L45: `connector.GetGroupsAsync(ct)`
- `LiveIngestHostedService` L56: `ingest.SyncCatalogAsync(...)`
- `GetAccountsAsync(null)` L201–202 re-walks `GetGroupsCore()` then every group name

**Honest limits (not a FAIL of “can list”):**

- Request path is `GroupRequestArray("*")` first; cache `GroupTotal`/`GroupNext` only if the request list is **empty**. A non-empty partial request result skips `GroupTotal`.
- Completeness is manager-ACL + retcode. This slot did not attach, so it does **not** re-prove a live 18-group census.
- `_pumpEnabled` does not gate this method.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

`ReadAccountsForGroup` (read this slot):

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

`GetAccountsCore(null)` iterates every group from `GetGroupsCore()` (L201–209). Catalog ingest calls `GetAccountsAsync(null)` (`DealIngestionService` L48).

Primary = `UserRequestArray`. Hard-fail of request → pump-cache `UserGetByGroup`. Empty → `UserLogins` + `UserRequestByLogins`.

**Honest limits:** capability proven in source. `UserLogins` is empty-only fallback (same residual as groups). This slot did not re-attach; prior 8460-trader census is **not** re-measured here. Users not in any group returned by `GetGroupsCore` would be missed.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned file `CTraderFixSession.cs` is **135** lines. This slot read it whole.

- Grep in that file for `35=D`, `(35, "D")`, `Build("D")`, `NewOrderSingle`: **0**.
- Only outbound `MsgType` is tag `(35, "A")` in `BuildLogon` L96.
- Single `ssl.WriteAsync` of that logon (L49). Sockets disposed via `using`.
- Hosted caller `CTraderFixLogonHostedService` only calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other writer on that type.

**Residual (does not fail the assigned-file claim):**

- Sibling `CTraderFixDemoTestTrade` contains `Build("D")` at L139 / L163 / L197. Demo-gated (`demo-` host / `demo.` sender; refuses `live-*` / `live.` / account `1369850`) and invoked from `tools/DemoFixTestTrade`, not DI / API / copy.
- `CTraderFixDemoMatrix.cs` L87 also writes `Build("D")`.
- Those are **not** `CTraderFixSession`.

Copy hop still cannot emit a ticket: `CopyTradingService.NewOrderSingleImplemented = false` (const L17) and persist `AllowFixSend = false` (L211). `CopyTradingHostedService` only calls `GenerateShadowIntentsAsync`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The **policy** default is still false. The **live runtime flag does not stay false**.

### What stays false (not enough to pass the claim)

| Surface | Evidence |
|---|---|
| Architecture doc | `docs\architecture.md` L20: `REAL_COPY_EXECUTION_ENABLED=false` |
| POCO default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) |
| FIX worker nested key | `apps\fix-worker\Worker.cs` L21 `GetValue("CTrader:RealCopyExecutionEnabled", false)` — **different key**, log-only, still stamps sessions `Disconnected` |
| API `appsettings.json` | Has `FeatureFlags.LiveCopyEnabled=false`. **No** `REAL_COPY_EXECUTION_ENABLED` key |
| `launchSettings.json` | No REAL_COPY env |
| `docker-compose.yml` | No REAL_COPY env |
| Copy persist | `AllowFixSend = false` hardcoded (`CopyTradingService` L211) |
| NOS const | `NewOrderSingleImplemented = false` |

### What is actually true on the API host (claim breaker)

1. Lab `D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). L106 is `FEATURE_COPY_TRADING_ENABLED=true` (also boolean only).
2. API `Program.cs` L10: `EnvFile.FindAndLoad()` — candidates include `D:\Prop\.env` (`EnvFile.cs` L14).
3. `EnvFile.Load` writes every `KEY=value` into `Environment.SetEnvironmentVariable` (L38).
4. `Program.cs` L13: `builder.Configuration.AddEnvironmentVariables()`.
5. DI binds the env token onto the process singleton:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

6. `CTraderFixLogonHostedService` **logs** `RealCopyArmed={Armed}` (L69–70) and **does not** assign `RealCopyEnabled = false`. Prior “logon pin false” reports are **stale**.
7. `/api/settings` L71–76 exposes `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` — therefore **true** after `.env` load, not a hardcoded false. (Dead `SettingsController` is unused; minimal APIs win.)
8. Copy service feeds `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine.Evaluate` (L190). `RiskEngine` can compute in-memory `AllowFixSend=true` when `RealExecutionEnabled && Reconciled && VenueHealthy` (L147–150). Persist still forces `AllowFixSend=false` (L211) and the send branch also requires `NewOrderSingleImplemented && VenueReconciled` (both const false). Armed flag is **not** a ticket today.

**Cannot prove “stays false.”** Live files prove the opposite: env `true` + DI bind + no re-pin.

Reports that still say `RealCopyEnabled` is forced false in DI / hosted / settings API (`CREDENTIALS_AND_COPY_STATUS.md` L30 “false (forced)”, W500_68/108-style pins, `A014_live_path_now.md` “DI pins false”) are **stale**.

---

## Risk to capital

**NONE today (`SAFE_BY_ABSENCE`).**

- Assigned FIX session cannot send `35=D`.
- Copy hop has no NewOrderSingle implementation and persists `AllowFixSend=false`.
- Native walk is read-only Manager request/cache.

**Residual (not a send today):** if a sender is added later, `LiveRuntimeStatus.RealCopyEnabled` will already be **true** on this lab host. That is why claim 5 is FAIL even though capital is not at risk from the current hop.

## What this slot did not do

- Did not attach to Achiever/Starwave.
- Did not re-sum the 18/8460 census.
- Did not invoke `tools/DemoFixTestTrade`.
- Did not print passwords, proxy auth, or FIX secrets.

## Verdict

**FAIL** — claims 1–4 proven from live files; claim 5 (`REAL_COPY_EXECUTION` stays false) is **disproven** by `.env` L73 `=true` + `EnvFile.FindAndLoad` + `DependencyInjection.cs` L41 bind + no hosted re-pin.
