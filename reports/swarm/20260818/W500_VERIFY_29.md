# W500_VERIFY_29 — Adversarial live-path verify (slot 29)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_29.md` |
| Agent / slot | W500 adversarial verifier **29** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live product `apps/`, `src/`, lab `.env` boolean only) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Claims 2–3 are **file-capability** only. |
| Method | Independent `read_file` of live hosts + connector + FIX + DI + copy hop. Targeted `grep`. Prior swarm text treated as **untrusted**. Verdict **FAIL** if any assigned claim is disproven or cannot be proven from the file. |

**Honesty rule:** a compile-time default is not a runtime pin. An env bind that can become `true` means the flag does **not** “stay false.” `GroupRequestArray("*")` is a capability, not a measured census. `SAFE_BY_ABSENCE` is not a §68 / §70 PASS. Sibling `Build("D")` is not `CTraderFixSession`. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL — claim 5 is disproven from live files.**

| # | Assigned claim | File-proven result | Class |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` token count under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_CODE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty-list fallback L174 `GroupTotal()` + `GroupNext`. Live completeness **not** re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_CODE** | `GetAccountsAsync(null)` walks every group from (2). Per group: `UserRequestArray` L223, then `UserLogins` L230 if `users.Total()==0`. Live completeness **not** re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). One `WriteAsync` (L49). Zero `NewOrderSingle` / `Build("D")` / `35=D` literals. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API `EnvFile.FindAndLoad()` (L10) + `AddEnvironmentVariables()` (L13). DI L41 binds `LiveRuntimeStatus.RealCopyEnabled` to that string. Hosted logon **does not** re-pin false. |

One-line:

```text
FAIL. DemoSeeder is off the API/worker startup path. Native group/user request APIs are wired. CTraderFixSession is 35=A only. REAL_COPY_EXECUTION_ENABLED does not stay false (.env L73=true, DI binds, no re-pin). Copy hop still cannot send (SAFE_BY_ABSENCE). Risk to capital NONE.
```

---

## 1. Claim 1 — `DemoSeeder` is not the API startup path — **PASS**

Read live `D:\Prop\apps\api\Program.cs` (160 lines). Startup after `app.Build()` is:

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

Same seed on both workers (not API, but confirms hosts):

```11:16:D:\Prop\apps\mt5-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`D:\Prop\apps\fix-worker\Program.cs` L11–16 is identical.

`grep` `DemoSeeder` under `D:\Prop\apps` `*.cs` = **0 hits**. Product C# callers remaining:

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` — class still on disk
- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` — test-only
- scratch trees under `reports/swarm/20260818/_tmp_*` — not hosts

DI fail-closed Native only (`LiveMt5Registration.CreateConnectors` returns Achiever + Starwave `NativeMt5BrokerConnector`). `FakeMt5` / `DemoSeeder` tokens in `apps/` = **0**.

**Residual (not claim 1):** `apps/mt5-worker/Worker.cs` L31 still scores `{10001,10002,10003,99001}`. That is a dummy **score set**, not API startup and not `DemoSeeder`. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). API `/api/ops/resync` scores `ListLoginsAsync` (all stored accounts).

**Stale reports:** `A002_api_dummy_path.md` / `A005_dashboard_traders.md` (API still calls `DemoSeeder`) are **superseded** by current `Program.cs`.

---

## 2. Claim 2 — Native can list all groups via `GroupRequestArray` or `GroupTotal` — **PASS_CODE**

Read live `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). `GetGroupsAsync` → `GetGroupsCore`:

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

Live ingest calls that path with no group cap:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

**Proven from file:** the connector **can** enumerate groups via those two Manager APIs. Mask is `"*"`.

**Not proven from file (residual, not this-slot FAIL):**

- This slot did **not** live-attach; 18/8460 is prior census, not re-measured.
- `GroupTotal` runs **only** when `list.Count == 0`. A non-empty **partial** `GroupRequestArray` will **not** fall through to `GroupTotal`.
- Completeness is Manager-rights / mask dependent. File cannot prove the server returns every group.

---

## 3. Claim 3 — All traders via `UserRequestArray` / `UserLogins` — **PASS_CODE**

`GetAccountsAsync(null)` walks **every** group name from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

**Proven from file:** request-first `UserRequestArray`, then `UserLogins` + `UserRequestByLogins` when the user array is empty. Catalog ingest uses `GetAccountsAsync(null)` (all groups).

**Not proven from file (residual):**

- `UserLogins` is **empty-array fallback**, not an always-on second census.
- A non-empty partial `UserRequestArray` skips `UserLogins`.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (deals-only), not every manager login. Catalog persist is still all accounts returned by the connector.
- This slot did not live-attach.

---

## 4. Claim 4 — `CTraderFixSession` has no `35=D` — **PASS**

Read live `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines). Grep of tag `35` in that file:

| Line | Token | Role |
|---|---|---|
| 55 | `Extract(reply, "35")` | **inbound** parse |
| 73 | `$"Logon rejected 35={msgType}"` | error text |
| 96 | `(35, "A")` | **only outbound MsgType** |

Outbound builder:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            // ... 34/49/56/50/57/52/98/108/141/553/554 ...
        };
        return Assemble(fields);
    }
```

Single socket write is that logon (`WriteAsync` L49). Sockets disposed via `using`. No `NewOrderSingle`, no `Build("D")`, no tag 38/54/40 order fields.

Hosted copy hop calls **only** `CTraderFixSession.TryLogonAsync` (`CTraderFixLogonHostedService` L48–58). `CopyTradingService.NewOrderSingleImplemented = false` (const L17). Persist `AllowFixSend = false` (L211).

**Residual (does not falsify claim 4):** sibling `CTraderFixDemoTestTrade` `Build("D")` ×3 and `CTraderFixDemoMatrix` `Build("D")` exist. They are **not** `CTraderFixSession`. Demo helper is demo-gated (`demo-` host / `demo.` sender / refuse account `1369850`) and is invoked from `tools/DemoFixTestTrade`, not DI / API / copy.

---

## 5. Claim 5 — `REAL_COPY_EXECUTION` stays false — **FAIL**

Assigned claim is that the flag **stays false**. Live files **disprove** that.

| Surface | Measured | Stays false? |
|---|---|---|
| Architecture / README / `CTraderFixOptions.RealCopyExecutionEnabled` | default **false** (POCO L35) | default only; POCO **unbound** (no `Configure<CTraderFixOptions>`) |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **NO** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (different key) | n/a |
| API `Program.cs` L10 + L13 | `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | process **loads** L73 |
| `EnvFile` L14 hard path | `D:\Prop\.env` is a candidate | will hit lab file |
| `DependencyInjection.cs` L39–41 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **binds** L73 → **true** |
| `CTraderFixLogonHostedService` | logs `RealCopyArmed={Armed}` (L68–70); **no** `_runtime.RealCopyEnabled = false` | re-pin **gone** |
| `/api/health` `realCopyEnabled` / `/api/settings` feature flag | `runtime.RealCopyEnabled` | will **display true** after API start with this `.env` |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | **false** | different name; unused by DI bind |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; defaults false; **not** the §41 token |

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

**Policy** (architecture §41, README, docs): the flag **must** stay false until risk/recon/§68/§70. **Operator leftover** already violates that. Prior slots that claimed a hard-false pin on DI / hosted logon are **stale**.

Copy hop still cannot emit a live ticket **even when the bit is armed**:

- `NewOrderSingleImplemented = false`
- `VenueReconciled = false`
- persist `AllowFixSend = false` (ignores `decision.AllowFixSend`)
- `CTraderFixSession` has no NewOrderSingle assembler
- 0 `ExecutionIntent` writers in product copy hop

That is **`SAFE_BY_ABSENCE`**, not “flag stays false.”

`RiskEngine` L147–150 **would** set `AllowFixSend=true` if `RealExecutionEnabled && Reconciled && VenueHealthy`. Copy service **overwrites** persist to `false` and requires `NewOrderSingleImplemented && VenueReconciled` before any live-status branch. Next sender added without those consts would see a runtime-armed flag.

This slot did **not** flip `.env`.

---

## 6. Risk to capital

**NONE** on the copy hop (`SAFE_BY_ABSENCE`).

`CTraderFixSession` cannot send `35=D`. Hosted copy writes SHADOW intents only. No live attach this slot. Flag-armed is a **policy FAIL**, not a ticket.

Do **not** treat this FAIL as a license to send. Do **not** wire `CTraderFixDemoTestTrade` into the copy hop. Operator should set `.env` L73 back to `false` (this slot did not edit it).

---

## 7. Files read (this slot; not prior-agent summaries)

| Path | Role |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup seed + settings flag |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed |
| `D:\Prop\apps\mt5-worker\Worker.cs` | residual 4-login scorer |
| `D:\Prop\apps\fix-worker\Worker.cs` | different REAL_COPY key |
| `D:\Prop\apps\api\appsettings.json` | unused `LiveCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | actual startup seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | env bind + Native-only |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | catalog + deals-only score |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow tick |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS=false; AllowFixSend persist false |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetGroups` + `GetAccounts(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/User request APIs |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads `D:\Prop\.env` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | 35=A only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | sibling Build("D") |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoMatrix.cs` | sibling Build("D") |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | unbound default false |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | allowSend from RealExecutionEnabled |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | probe uses Native GetGroups/GetAccounts; not DemoSeeder |
| `D:\Prop\.env` L73 / L106 | boolean flags only |

---

## 8. What this slot did **not** do

- Did not start API/workers or GET `:5000`.
- Did not connect Manager or FIX.
- Did not edit product, tests, or `.env`.
- Did not treat 18/8460 as this-slot measurement.
