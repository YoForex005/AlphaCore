# W500_VERIFY_93 — Adversarial live-path verify (slot 93)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 93 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Test source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence. This slot re-read the product files listed below.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS_SOURCE** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135; only outbound MsgType is `A`) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold from files. Claim 5 is false on the API composition: lab `.env` L73 is `true`, `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` load it, and DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup seed + env load + `/api/settings` echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Residual 4-login scorer (not DemoSeeder boot) |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Unused leftover; live `/api/settings` is the minimal API |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/trader walks |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog caller `GetGroupsAsync` / `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Entire 135-line hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default unread by DI |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` → process env |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Send still unimplemented |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick only |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Probe uses Native + `GetAccountsAsync(null)` |
| `D:\Prop\.env` L73 + L106 | Flag booleans only |

Product `*.cs` grep: `DemoSeeder` is **not** referenced from `apps/`. Hits are `DemoSeeder.cs` itself and `tests/Integration/SeedingAndStoreTests.cs` (+ swarm `_tmp_*` evals). `CTraderFixSession.cs` has **0** matches for `35=D` / `Build("D")`.

---

## 1. DemoSeeder is not the API startup path — PASS

API startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Same seed on both workers (`apps/fix-worker/Program.cs` L15, `apps/mt5-worker/Program.cs` L15). No `DemoSeeder` token in any of the three host `Program.cs` files.

`BrokerCatalogSeed.EnsureAsync` writes broker rows + XAU instrument + kill-switch + disconnected FIX session stubs. It does not invent dummy logins or quotes.

`DemoSeeder` still exists at `src/Infrastructure/Seeding/DemoSeeder.cs` (`public static class DemoSeeder` L14) and is called from integration tests only. That is not the API boot path.

DI refuses Fake/dummy when passwords are missing (`DependencyInjection.cs` L36–37) and registers only `LiveMt5Registration.CreateConnectors` (`L47–48`) → two `NativeMt5BrokerConnector` instances. Product `new Fake` is confined to `FakeMt5BrokerConnector.cs` itself.

Residual (does **not** revive claim 1): `apps/mt5-worker/Worker.cs` L31 still scores `{10001,10002,10003,99001}` after a real `SyncBrokerAsync`. Hosted API ingest (`LiveIngestHostedService`) uses `ListLoginsWithDealsAsync` / catalog, not that four-login list.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS_SOURCE

`GetGroupsCore` in `NativeMt5BrokerConnector.cs`:

1. Primary: `GroupRequestArray("*", arr)` (L155). On `MT_RET_OK` / `MT_RET_OK_NONE`, walk `arr.Total()` / `arr.Next(i)`.
2. Fallback if `list.Count == 0`: `GroupTotal()` + `GroupNext(i, grp)` (L174–179).

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

Ingest is flag-blind: `DealIngestionService.SyncCatalogAsync` calls `GetGroupsAsync` with no plan/env filter (`DealIngestionService.cs` L45). `LiveIngestHostedService` calls that catalog path (L56). Probe `tools/LiveBrokerProbe/Program.cs` L25 does the same.

**Honesty:** this slot did **not** live-attach a Manager. File proves the walk exists. Live census counts (prior 18/8460) are **not** re-proven here.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

`GetAccountsAsync(null)` walks every group from `GetGroupsCore()` (`NativeMt5BrokerConnector.cs` L199–202). Per group, `ReadAccountsForGroup`:

1. Primary: `UserRequestArray(gname, users)` (L223).
2. If request is not OK / OK_NONE / NOTFOUND: `UserGetByGroup` (pump-cache fallback, L225).
3. If `users.Total() == 0`: `UserLogins(gname, out loginRes)` then `UserRequestByLogins` (L227–232).

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

Catalog ingest uses `GetAccountsAsync(null, ct)` (`DealIngestionService.cs` L48, L62) — no group whitelist.

**Honesty:** same as claim 2 — source capability only. This slot did not re-count Manager logins.

---

## 4. CTraderFixSession has no 35=D — PASS

File is 135 lines. Entire outbound body is `BuildLogon` → tag `(35, "A")` (L96). `TryLogonAsync` writes that one buffer (L47–50), reads one reply, accepts only `msgType == "A"` (L55–56), then returns. No second write. No `Build("D")`. Grep of this file for `35=D` / `"D"` as MsgType: **0 hits**.

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

Hosted caller is `CTraderFixLogonHostedService` only (`TryLogonAsync` L48 and L54). One-shot QUOTE + TRADE logon, then persist status. No order path.

Residual **off-hop** `35=D`: `CTraderFixDemoTestTrade.cs` and `CTraderFixDemoMatrix.cs` `Build("D")`, invoked only from `tools/DemoFixTestTrade/Program.cs`. Not `CTraderFixSession`. Not registered in DI.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproven)

The assigned claim is that the flag **stays false**. Live files prove the opposite on the API composition.

| Layer | What the file says |
|---|---|
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secrets quoted) |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` (display; API also hardcodes this true at `Program.cs` L77) |
| `apps/api/Program.cs` L10 | `EnvFile.FindAndLoad()` |
| `apps/api/Program.cs` L13 | `builder.Configuration.AddEnvironmentVariables()` |
| `EnvFile.cs` L9–20, L38 | walks cwd / parents / `D:\Prop\.env`, `Environment.SetEnvironmentVariable` |
| `DependencyInjection.cs` L39–41 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` |
| `CTraderFixLogonHostedService.cs` | **never** assigns `_runtime.RealCopyEnabled`; logs it as `RealCopyArmed` (L69–70) |
| `/api/settings` (`Program.cs` L71–77) | echoes `runtime.RealCopyEnabled` under `featureFlags.REAL_COPY_EXECUTION_ENABLED` |
| `CTraderFixOptions.RealCopyExecutionEnabled` | POCO default `false` (`CTraderFixOptions.cs` L35) — **unread** by DI for `LiveRuntimeStatus` |
| `apps/fix-worker/Worker.cs` L21 | different key `CTrader:RealCopyExecutionEnabled` default `false` — **log-only**; workers do **not** call `EnvFile.FindAndLoad` |
| `appsettings*.json` | **0** `REAL_COPY` keys |

Single assignment site for `RealCopyEnabled =` in product C#: `DependencyInjection.cs` L41.

Therefore: when the API process starts against `D:\Prop\.env`, `LiveRuntimeStatus.RealCopyEnabled` is **true**. Claim 5 is **false**.

Workers without a loaded `.env` would bind false (missing config key). That does **not** rescue the claim: the API is the live composition and it arms the flag.

---

## Copy hop (capital) — SAFE_BY_ABSENCE despite armed flag

Flag-armed ≠ ticket sent.

- `CopyTradingService.NewOrderSingleImplemented = false` (L18)
- `VenueReconciled = false` (L17)
- Persist path hard-sets `AllowFixSend = false` (L306)
- Live-send branch is a status string only: `LIVE_SEND_BLOCKED_UNIMPLEMENTED` (L312–314)
- Hosted copy tick: `TickRosterAsync` + `GenerateShadowIntentsAsync` (`CopyTradingHostedService.cs` L28–29)
- `CTraderFixSession` outbound is `35=A` only (claim 4)
- `BuildBlockers` always includes `No NewOrderSingle sender — SAFE_BY_ABSENCE` (L468–469)

`/api/settings` leftover controller (`apps/api/Controllers/SettingsController.cs`) is unused; the live route is the minimal API in `Program.cs`.

---

## Residuals (not used to greenwash claim 5)

- Demo `Build("D")` exists off-hop (`CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` / `tools/DemoFixTestTrade`).
- `mt5-worker/Worker.cs` still scores four dummy logins after a real catalog sync.
- This slot did not live-attach Manager or FIX. Claims 2–3 are source-capability only.
- If a sender were later implemented while `.env` remains `true`, dest risk would flip. That sender is **absent** today.

---

## Verdict

**FAIL.**

1. PASS — API/workers seed `BrokerCatalogSeed`, not `DemoSeeder`.
2. PASS_SOURCE — `GroupRequestArray("*")` then `GroupTotal`/`GroupNext`.
3. PASS_SOURCE — `UserRequestArray` then `UserLogins`/`UserRequestByLogins`.
4. PASS — `CTraderFixSession` 135/135 is `35=A` only.
5. FAIL — `REAL_COPY_EXECUTION` does **not** stay false (`.env` L73 `true` + API env load + DI L41; logon host no re-pin).

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`).
