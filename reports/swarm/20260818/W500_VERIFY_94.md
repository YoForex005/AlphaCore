# W500_VERIFY_94 — Adversarial live-path verify (slot 94)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_94.md` |
| Agent / slot | W500 adversarial **verify 94** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A002 / A014 / A015 / CREDENTIALS reports. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (const gates + persist + blockers), `DealIngestionService.cs` (catalog + ingest), `LiveIngestHostedService.cs` (140/140), `CopyTradingHostedService.cs` (43/43), `LiveMt5Registration.cs` (94/94), `EnvFile.cs` (41/41), `BrokerCatalogSeed.cs` (112/112), `DemoSeeder.cs` header, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/Controllers/SettingsController.cs`, `apps/api/appsettings.json`, `CTraderFixDemoTestTrade.cs` header + `Build("D")` lines, `MT5APIManager.h` request signatures. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `RealCopyEnabled =`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (source capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Not re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (source capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `NewOrderSingle` = **0**. `WriteAsync` = 1. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. Only `RealCopyEnabled =` write in product C# is DI L41. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file; claim 5 is affirmatively false).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A015 “logon sets `_runtime.RealCopyEnabled = false`”; A006 “`/api/settings` hardcodes false”; A002 “API still calls `DemoSeeder`”; SettingsController is **not** the live `/api/settings` hop.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 Live API host

`D:\Prop\apps\api\Program.cs` (160 physical lines, full read):

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

Startup seed (after routes, before `app.Run()`):

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0**.

API is minimal-hosting (`MapGet`/`MapPost` only). There is **no** `AddControllers()`. `apps/api/Controllers/SettingsController.cs` exists on disk (Redis + nested `FeatureFlags:LiveCopyEnabled`) but is **not** the live `/api/settings` hop (`Program.cs` L71–84).

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker\Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only.

DI (`DependencyInjection.cs` L36–58) fail-closes without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave; Starwave `ProxyEnabled=false`), plus `LiveIngestHostedService`. No `FakeMt5` / `DemoSeeder` tokens in `DependencyInjection.cs`.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder` | Tests + leftover file. **Not** called from `apps/`. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| `mt5-worker/Worker.cs` still scores `{10001,10002,10003,99001}` | L31–35 | Dummy **scorer** leftover. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106). Not DemoSeeder. |

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows + FIX rows at `Disconnected` (L77–107). It does **not** ingest FakeMt5 tape or score 10001.

**Claim 1 proven.** A014 “DemoSeeder gone from API startup” is **still true**. A002 “API still calls DemoSeeder” is **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (full file 458 lines):

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

Live ingest walks this list:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetAccountsAsync(null)` re-enters `GetGroupsCore` (`GetAccountsCore` L201–202). Hosted catalog is `LiveIngestHostedService` → `SyncCatalogAsync` (L56).

Vendor surface exists: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L205 `GroupTotal` / L212 `GroupRequestArray`. This slot treats the C# call sites as the product proof.

### 2.1 What this file proves

- Primary enumerator is the request API `GroupRequestArray("*")` — the official ALL-groups mask.
- If that walk yields **zero** rows, cache walk `GroupTotal` + `GroupNext` runs.
- Fetch is **flag-blind** (`_pumpEnabled` is recorded at Connect L96/L110 but **never** gates `GetGroupsCore`).

### 2.2 What this file does **not** prove (residuals, not claim-2 FAIL)

| Residual | Why |
|---|---|
| No live attach this slot | Cannot re-measure 8+10=18 groups. Prior 08:42Z census is **not** this slot’s evidence. |
| Partial non-empty request | If `GroupRequestArray` returns count > 0 but incomplete, `GroupTotal` fallback is **skipped**. |
| Manager ACL | `*` is “all groups this manager may see,” not “every group on the server.” |

A001 “zero `GroupRequestArray` under `src`” is **stale**.

**Claim 2 proven as source capability.** Runtime “ALL on Achiever+Starwave today” is **unproven this slot**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

`ReadAccountsForGroup`:

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

`GetAccountsCore` with `group == null` walks **every** group from `GetGroupsCore`, then dedupes by login (L189–213). Live catalog uses `GetAccountsAsync(null)` (`DealIngestionService` L48 / L62).

Vendor surface: `MT5APIManager.h` L254 `UserLogins` / L410 `UserRequestArray`. Product proof is the C# walk above.

### 3.1 What this file proves

- Primary trader enumerator is `UserRequestArray` (network request).
- Empty array → `UserLogins` + `UserRequestByLogins`.
- `UserGetByGroup` is **only** the hard-fail cache fallback (not the ALL path).
- Hosted scoring is `ListLoginsWithDealsAsync` (subset of catalog). Catalog itself is still the all-users walk.

### 3.2 Residuals (not claim-3 FAIL)

| Residual | Why |
|---|---|
| No live attach | Cannot re-measure 6512+1948=8460. |
| Partial non-empty `UserRequestArray` | `UserLogins` skipped when `users.Total() != 0`. |
| `mt5-worker` dummy score set | Worker L31 still scores four hardcoded logins. That is **not** the catalog walk. Hosted ingest is `ListLoginsWithDealsAsync`. |

**Claim 3 proven as source capability.** Runtime “ALL traders today” is **unproven this slot**.

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

Outbound body is assembled only in `BuildLogon`:

```94:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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

Single write: L49 `await ssl.WriteAsync(bytes, timeoutCts.Token)`. After one `ReadAsync`, sockets dispose. Inbound `Extract(reply, "35")` is used only to accept Logon `A` or report reject. No `NewOrderSingle` identifier. File grep: `35=D` = 0, `NewOrderSingle` = 0, outbound tag-35 literal = one `A`.

Hosted caller `CTraderFixLogonHostedService` (112/112) calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists session rows. It never builds a D.

### 4.1 Residual that is **not** `CTraderFixSession`

Sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. That class is **not** the assigned file. It is demo-gated (`demo-` host / `demo.` sender; refuses `live-*` / `live.` / account `1369850`) and is invoked from `tools/DemoFixTestTrade`, not from API/DI/copy. Claim 4 is scoped to `CTraderFixSession`.

**Claim 4 proven.** Copy hop cannot emit NewOrderSingle from this type.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live composition **arms** it.

### 5.1 Bind path (file-proven)

1. Lab `D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret).
2. Lab `D:\Prop\.env` L106: `FEATURE_COPY_TRADING_ENABLED=true` (boolean only; copy feature, not the send hop).
3. API `Program.cs` L10 `EnvFile.FindAndLoad()` walks to `D:\Prop\.env` (`EnvFile.cs` L14 hardcoded last candidate) and `Environment.SetEnvironmentVariable`.
4. L13 `builder.Configuration.AddEnvironmentVariables()`.
5. `AddTraderIntelligence` constructs:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

6. Grep `RealCopyEnabled =` in product `*.cs` = **one** hit: DI L41. Nothing later assigns `false`.
7. `CTraderFixLogonHostedService` L68–70 **reads** `_runtime.RealCopyEnabled` as `RealCopyArmed={Armed}` and does **not** write it.
8. Live `/api/settings` (`Program.cs` L71–76) echoes `runtime.RealCopyEnabled` under `featureFlags.REAL_COPY_EXECUTION_ENABLED`.

`LiveRuntimeStatus.RealCopyEnabled` is a settable `bool` (L32) with default `false` only until DI overwrites it. `CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false` (L35) and is **unread** by the API host. Fix-worker `Worker.cs` L21 reads a **different** nested key `CTrader:RealCopyExecutionEnabled` (default false) and only logs.

`appsettings.json` has no `REAL_COPY_EXECUTION_ENABLED` key. Nested `FeatureFlags:LiveCopyEnabled=false` is unused by the live `/api/settings` hop.

### 5.2 What “stays false” would have required (absent)

- Hardcode `RealCopyEnabled = false` in DI, **or**
- Hosted logon re-pin `_runtime.RealCopyEnabled = false`, **or**
- Lab `.env` L73 = `false` / missing so `string.Equals(..., "true")` is false.

None of those are in the files.

**Claim 5 disproven.** Next API process that loads `D:\Prop\.env` will report `RealCopyEnabled=true`.

---

## 6. Copy hop still cannot send (not claim 5)

`CopyTradingService`:

```17:19:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
    public const decimal AllocationFactor = XauUsdOneToOneCopyPolicy.AllocationFactor;
```

Persist forces `AllowFixSend = false` (L306). The only live-send branch (L312) still writes `LIVE_SEND_BLOCKED_UNIMPLEMENTED` and never calls FIX. Hosted copy (`CopyTradingHostedService` L28–29) ticks roster + shadow intents only.

`GetStatusAsync` L45–60 reports `RealCopyArmed: _runtime.RealCopyEnabled` **and** `NewOrderSingleImplemented: false`. Blocker L468 is `"No NewOrderSingle sender — SAFE_BY_ABSENCE"`. Blocker L478 (`REAL_COPY_EXECUTION_ENABLED is false`) **will not fire** when the env is `true`.

Risk to destination capital remains **NONE** because there is no sender on the product hop. Residual: if a sender is added later, DI will already see the flag armed.

---

## 7. Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` flag echo |
| `D:\Prop\apps\fix-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | Worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | Nested `CTrader:RealCopyExecutionEnabled` log-only |
| `D:\Prop\apps\mt5-worker\Worker.cs` | Dummy four-login scorer residual |
| `D:\Prop\apps\api\Controllers\SettingsController.cs` | Dead controller; not live hop |
| `D:\Prop\apps\api\appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | Actual host seed |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | Class exists; not host-called |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Runtime flag bind + Native-only connectors |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Group/trader walks |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | Catalog caller `*` / `null` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | Entire 135-line hop |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | Residual `Build("D")` off-hop |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon only; no flag re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default unread |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` → process env |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | Send still unimplemented |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick only |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor request APIs |
| `D:\Prop\.env` L73 + L106 | Flag booleans only |

---

## 8. Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (file capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (file capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold. Claim 5 is false on the API composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): product hop still cannot emit a ticket.
