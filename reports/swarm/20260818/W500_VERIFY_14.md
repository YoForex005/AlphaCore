# W500_VERIFY_14 — Adversarial live-path verify (slot 14)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_14.md` |
| Agent / slot | W500 adversarial verifier **14** |
| Date | 2026-08-18 |
| Role | Independent re-read of live path files. **Do not trust other agents.** |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean flags quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `CTraderFixOptions.cs` (80/80), `LiveRuntimeStatus.cs` (67/67), `CopyTradingService.cs` (320/320), `CTraderFixLogonHostedService.cs` (112/112), `LiveMt5Registration.cs` (head), `DealIngestionService.cs` (head), `LiveIngestHostedService.cs` (head), `CopyTradingHostedService.cs` (40/40), `BrokerCatalogSeed.cs` (head), worker `Program.cs` files, `EnvFile.cs`, `RiskEngine.cs` allowSend, `CTraderFixDemoTestTrade.cs` gate. Targeted `grep` of `DemoSeeder` under `apps/` (**0**), `35=D` / `(35, "D")` in `CTraderFixSession.cs` (**0**), `REAL_COPY_EXECUTION_ENABLED` in `.env` (boolean only), `RealCopyEnabled =` under `src/` (**1** bind). |

**Honesty rule:** capability in source is not a live attach. A POCO default is not the running bit. A comment / README / CREDENTIALS table is not the DI bind. Sibling `Build("D")` is not `CTraderFixSession`. **FAIL any claim that cannot be proved from the file.**

---

## 0. Verdict (binding)

**FAIL.**

Claims 1–4 are proved from the files. Claim 5 is **not** proved and is **disproved** as a runtime fact: lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` and DI copies that onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon no longer re-pins false. Therefore “REAL_COPY_EXECUTION stays false” **cannot** be confirmed.

Copy hop still cannot emit a ticket (`SAFE_BY_ABSENCE`). That does **not** rescue claim 5.

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 is `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` hits under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (code capability; no attach this slot) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty → L174 `GroupTotal` + `GroupNext`. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (code capability; no attach this slot) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest `GetAccountsAsync(null)`. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: outbound tag 35 is `"A"` only (L96). Grep `35=D` / `(35, "D")` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | `.env` L73 `=true`. `EnvFile.FindAndLoad` + `AddEnvironmentVariables`. DI L41 binds. Logon L68–70 logs `RealCopyArmed` and does **not** assign `false`. `/api/settings` follows `runtime.RealCopyEnabled`. |

One-line:

```text
SLOT 14 FAIL. DemoSeeder off API boot. Native GroupRequestArray("*")/GroupTotal + UserRequestArray/UserLogins present. CTraderFixSession 35=A only. REAL_COPY does NOT stay false (.env L73 true + DI bind). Capital risk NONE (SAFE_BY_ABSENCE).
```

---

## 1. DemoSeeder is not the API startup path — PASS

File: `D:\Prop\apps\api\Program.cs` (read 160/160).

Startup seed:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0** (API + both workers). Worker hosts also seed `BrokerCatalogSeed.EnsureAsync` only (`apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15).

DI (`DependencyInjection.cs` L36–49) fail-closes without real MT5 passwords and registers `LiveMt5Registration.CreateConnectors` (Native ×2). No Fake substitution on the throw path.

Residual (does **not** put DemoSeeder back on API boot):

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder`).
- `tests/Integration/SeedingAndStoreTests.cs` L25 still calls `DemoSeeder.SeedAsync`.
- A002 / A005 “API still seeds DemoSeeder” are **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (code)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (read 458/458).

`GetGroupsCore`:

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

Primary walk is the request API `GroupRequestArray("*")`. Fallback is the pump-cache walk `GroupTotal` + `GroupNext` when the request list is empty.

Live ingest uses that walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null)`. `_pumpEnabled` does not gate fetch.

A001 (“zero `GroupRequestArray` under `src`”) is **stale**.

**Not proved this slot:** a live Manager attach returning a measured group census. Prior 8+10=18 is cited by other reports only.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (code)

Same file. `GetAccountsCore(null)` walks every name from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

Order: `UserRequestArray` first; cache `UserGetByGroup` only on hard fail; empty array → `UserLogins` then `UserRequestByLogins`.

**Not proved this slot:** live login totals. Completeness without a successful request (or a warm `PUMP_MODE_USERS` cache) can still be empty.

---

## 4. CTraderFixSession has no 35=D — PASS

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (read 135/135).

Outbound builder is `BuildLogon` only:

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
        return Assemble(fields);
```

Single `WriteAsync` (L49). Sockets are `using`-disposed after one read. Result type has no ClOrdID / OrderQty. Grep of this file for `35=D`, `(35, "D")`, `NewOrderSingle` = **0**.

Hosted caller (`CTraderFixLogonHostedService` L48–58) invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). That is Logon, not NewOrderSingle.

Residual **off this class** (does **not** put `35=D` into `CTraderFixSession`):

- `CTraderFixDemoTestTrade.Build("D")` ×3 — tools/CLI, demo-gated (refuse `live-*` / `live.*` / account `1369850`).
- `CTraderFixDemoMatrix` `Build("D")` — same sibling namespace, not the hosted hop.

Copy service still `NewOrderSingleImplemented = false` and persist `AllowFixSend = false`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove the flag stays false. The live API host **arms** it.

| Surface | Measured | Stays false? |
|---|---|---|
| Architecture / README / `docs/architecture.md` | write `=false` | design only |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default `false` | unbound POCO; no `Configure<>` |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different name** |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **NO** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | unused by DI |
| `EnvFile.FindAndLoad` (`Program.cs` L10) | loads `D:\Prop\.env` (hardcoded candidate) | process env set |
| `AddEnvironmentVariables()` (`Program.cs` L13) | binds into `IConfiguration` | yes |
| `DependencyInjection.cs` L39–41 | `RealCopyEnabled = configuration["REAL_COPY_EXECUTION_ENABLED"] == "true"` | **bound, not pinned** |
| `CTraderFixLogonHostedService` | logs `RealCopyArmed={Armed}` L68–70; **no** `RealCopyEnabled = false` | re-pin **gone** |
| `/api/settings` L76 | `runtime.RealCopyEnabled` | display follows env |
| `/api/health` L55 | `realCopyEnabled = runtime.RealCopyEnabled` | same |
| fix-worker `Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; log-only |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` | “false (forced)” | **STALE** |

`RealCopyEnabled =` assignment under `D:\Prop\src` exists **once**: the DI bind. Nothing later forces it back to false.

`CopyTradingService` still cannot send: `NewOrderSingleImplemented=false`, `VenueReconciled=false`, persist `AllowFixSend=false` (L211). `RiskEngine` L147–150 can compute `allowSend=true` **if** `RealExecutionEnabled && Reconciled && VenueHealthy`; the hop then **overwrites** persist to false. That is send-safety, not “flag stays false.”

W500_68 / W500_108 / CREDENTIALS “pinned false” and INDEX notes that slots 14/34/54/114 hard-false pins are stale — this verify **agrees** and therefore **FAILs** claim 5.

---

## 6. Risk to capital

**NONE** on the copy hop (`SAFE_BY_ABSENCE`).

Reasons (files, not hope):

- `CTraderFixSession` has no NewOrderSingle assembler.
- Hosted FIX is one-shot `35=A` then dispose.
- `CopyTradingService` const `NewOrderSingleImplemented=false`; persist `AllowFixSend:=false`.
- Copy hosted service only calls `GenerateShadowIntentsAsync`.
- Demo `Build("D")` is not wired to API/DI/copy.

Residual: next sender would see **runtime armed** (`RealCopyEnabled=true` after `.env` load). Do not treat the leftover `true` as a go-live. Do not add `35=D` to `CTraderFixSession`. Operator should flip `.env` L73 back to `false` (this slot did **not** edit it).

This slot did not live-attach. Census 18/8460 is **not** re-measured here.

---

## 7. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (gate only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (allowSend)
- `D:\Prop\.env` (flag keys/booleans only)

---

## 8. Checklist

- [x] DemoSeeder gone from **API startup** (file remains for tests)
- [x] Native `GroupRequestArray` + `GroupTotal` on live connector
- [x] Native `UserRequestArray` + `UserLogins` on live connector
- [x] `CTraderFixSession` no `35=D`
- [ ] `REAL_COPY_EXECUTION` stays false — **FAIL** (env armed + DI bind)
