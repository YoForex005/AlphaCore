# W500_VERIFY_5 — Adversarial live-path re-read (slot 5)

| Field | Value |
|---|---|
| Date | 2026-08-18 |
| Agent | W500_VERIFY_5 |
| Slot | **5** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Assigned | Confirm: (1) DemoSeeder is not the API startup path; (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`; (3) all traders via `UserRequestArray`/`UserLogins`; (4) `CTraderFixSession` has no `35=D`; (5) `REAL_COPY_EXECUTION` stays false. |
| Product source modified | **No** |
| Live attach this slot | **No** (source-only) |
| Secret values printed | **None** (flag booleans only; no passwords, no connection strings, no FIX password) |
| Live `35=D` sent | **No** |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_5.md` |

**Honesty rule:** prove every claim from the file as it sits on disk. FAIL any claim that is not proven. Prior swarm notes (A014, W500_RESEARCH_*, `CREDENTIALS_AND_COPY_STATUS.md`) are **not** evidence.

---

## Verdict

**FAIL** — four claims proven; **claim (5) is disproven**.

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | API `Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (source capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty list falls back to `GroupTotal`/`GroupNext` L174–180. |
| 3 | All traders via `UserRequestArray`/`UserLogins` | **PASS** (source capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → `UserLogins` + `UserRequestByLogins` L230–232. Ingest uses `GetAccountsAsync(null)`. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: only outbound MsgType is `(35, "A")` L96. One `WriteAsync`. Sockets `using`-disposed. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()`. DI L41 binds that key onto `LiveRuntimeStatus.RealCopyEnabled`. FIX logon host **does not** re-pin false. |

Slot did **not** live-attach Manager. Census 18/8460 is **not** re-proven here. Claims 2–3 are **source walks**, not a measured attach.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Flag may be armed; there is still no copy-hop `35=D` builder.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160/160).

Startup after route maps:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- Zero `DemoSeeder` tokens in this file.
- `using TraderIntelligence.Infrastructure.Seeding;` exists for `BrokerCatalogSeed` only.
- Grep `DemoSeeder` under `D:\Prop\apps` = **0**.
- Worker hosts (`apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15) also call `BrokerCatalogSeed.EnsureAsync`, not `DemoSeeder`.

`DemoSeeder.cs` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Integration tests still call it. That is **not** API/worker startup.

Residual (not this claim): `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. Hosted API ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Manual `/api/ops/resync` walks `store.ListLoginsAsync` (all persisted logins), not the four demo logins.

A002 (`Program.cs` still calls `DemoSeeder`) is **stale** on this disk.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS (source)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458).

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

- Mask is `"*"` — manager-visible groups, not a plan-name filter.
- Request array first; cache `GroupTotal`/`GroupNext` only if the request list is empty.
- Ingest `DealIngestionService.SyncCatalogAsync` L45 calls `GetGroupsAsync` (this walk). No `Take(`/`Skip` in that service.

Not proven this slot: a live `MT_RET_OK` and a group count. Code can enumerate; attach was not run.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source)

Same connector, `GetAccountsCore` + `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
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
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
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

- `GetAccountsAsync(null)` (ingest L48, L62) walks **every group name** from claim 2, then per-group `UserRequestArray`.
- Empty / hard-fail: cache `UserGetByGroup`, then `UserLogins` + `UserRequestByLogins`.
- Dedup is `Dictionary<ulong, Mt5AccountDto>` by login.

Not proven this slot: live trader totals. Source path is ALL groups × request/logins.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

- Only public send path: `TryLogonAsync` → `BuildLogon` → one `ssl.WriteAsync` → one `ReadAsync` → dispose `TcpClient`/`SslStream`.
- `BuildLogon` tag 35 is literal `"A"` (L96).
- File has **0** `35=D`, **0** `(35, "D")`, **0** `NewOrderSingle`.
- Hosted caller `CTraderFixLogonHostedService` only invokes `TryLogonAsync` (QUOTE 5211, TRADE 5212). No order builder.

Residual **outside** this class (does **not** fail claim 4): sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Wired only from `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Demo-gated (refuses `live-*` / `live.` / account `1369850`). Not registered in API/worker DI. Copy hop is `CopyTradingService` + `CTraderFixSession`, not the demo helper.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim is a **runtime fact**, not a comment. Live files disprove it.

| Surface | What the file says |
|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| `D:\Prop\.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (adjacent; not this flag) |
| `apps/api/Program.cs` L10 | `EnvFile.FindAndLoad()` — candidates include `D:\Prop\.env` |
| `apps/api/Program.cs` L13 | `builder.Configuration.AddEnvironmentVariables()` |
| `DependencyInjection.cs` L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `CTraderFixLogonHostedService` | Logs `RealCopyArmed={Armed}`; **no** assignment of `RealCopyEnabled = false` |
| `apps/api/Program.cs` L55, L76 | `/api/health` and `/api/settings` expose `runtime.RealCopyEnabled` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO **default** `false` (L35) — **unread** by the live runtime flag |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` — **different name**; not the DI key |
| `fix-worker/Worker.cs` L21 | Reads nested `CTrader:RealCopyExecutionEnabled` default **false** (log-only) |
| `CREDENTIALS_AND_COPY_STATUS.md` L30 | `false (forced)` — **STALE** vs current DI |

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

On the API host that loads `.env`, `LiveRuntimeStatus.RealCopyEnabled` is **true**. That is the opposite of “stays false.”

What **does** stay closed (does not rescue claim 5):

| Gate | File | Value |
|---|---|---|
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const false` |
| `VenueReconciled` | same L16 | `const false` |
| Persist `AllowFixSend` | `CopyTradingService.cs` L211 | hardcoded `false` (ignores `decision.AllowFixSend`) |
| Copy tick | `CopyTradingHostedService` | SHADOW intents only |
| Session sender | `CTraderFixSession` | `35=A` only |

`CREDENTIALS_AND_COPY_STATUS` “forced false” and older W500 “DI pins false / hosted re-pins false” cites are **stale**. Policy (architecture / `.env.example` if present) still *wants* false. The live API **does not keep it false**.

---

## Live path (source, this slot)

```
API Program
  EnvFile.FindAndLoad()            // D:\Prop\.env → REAL_COPY=true
  AddTraderIntelligence
    throw if both MT5 passwords missing
    LiveRuntimeStatus.RealCopyEnabled ← env  // ARMED
    NativeMt5BrokerConnector ×2
    LiveIngestHostedService
    CTraderFixLogonHostedService   // 35=A only; no re-pin
    CopyTradingHostedService       // SHADOW only
  EnsureCreated + BrokerCatalogSeed   // NOT DemoSeeder
```

Ingest catalog: `GetGroupsAsync` → `GroupRequestArray("*")` / `GroupTotal`; `GetAccountsAsync(null)` → `UserRequestArray` / `UserLogins`.

---

## Residuals (not claim 1–4 blockers)

1. `.env` `REAL_COPY_EXECUTION_ENABLED=true` is **bound**. Next sender would see an armed flag.
2. `CTraderFixDemoTestTrade.Build("D")` exists off-hop (tools + demo gate).
3. `mt5-worker/Worker.cs` still scores four dummy logins (hosted API does not).
4. `GET /api/trades` still `Take(200)` — HTTP page, not Manager enumeration.
5. This slot did not attach Manager; group/trader counts not re-measured.
6. `SettingsController` (`FeatureFlags.LiveCopyEnabled`) is a second unused settings surface; minimal APIs win `/api/settings`.

---

## Risk to capital

**NONE** today. Copy hop cannot emit `35=D` (`CTraderFixSession` has no builder; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). Dest capital is unharmed by **absence**, not by the env flag.

Flag is **armed**. Do not treat claim 5 as PASS. Do not add a NewOrderSingle builder while `.env` is `true`.

---

## Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\Properties\launchSettings.json`
- `D:\Prop\apps\api\Controllers\SettingsController.cs`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header only)
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (residual)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (AllowFixSend conjunction)
- `D:\Prop\.env` (flag keys/booleans only)

**Overall: FAIL** (claim 5). Claims 1–4 PASS from file. Risk **NONE**.
