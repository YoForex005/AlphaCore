# W500_VERIFY_95 — Adversarial live-path verify (slot 95)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_95.md` |
| Agent / slot | W500 adversarial **verify 95** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / INDEX / SWARM_LOG / CREDENTIALS / A014 / A015 prose. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (const + persist + send-if + blockers), `DealIngestionService.SyncCatalogAsync`, `LiveIngestHostedService` catalog loop, `CopyTradingHostedService.cs` (43/43), `LiveMt5Registration.cs` (connector factory), `EnvFile.cs` (41/41), `BrokerCatalogSeed` start, `CTraderFixDemoTestTrade` demo gate, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`. Targeted `grep` of `DemoSeeder` (apps vs tests), `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35` in `CTraderFixSession`, `Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `new ExecutionIntent`, `FakeMt5`, `RealCopyEnabled =`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (source capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Not re-attached. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (source capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` = **0**. `WriteAsync` = 1. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file; claim 5 is affirmatively false).

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`; `new ExecutionIntent` = **0** in `src/`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; A014 “DI pins false”; A015 “logon sets `_runtime.RealCopyEnabled = false`”; A006 “`/api/settings` hardcodes false”; `SettingsController` is **not** the live `/api/settings` hop (minimal API `MapGet` in `Program.cs`; no `AddControllers`/`MapControllers`).

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

### 1.2 Workers (same seed, not DemoSeeder)

`D:\Prop\apps\fix-worker/Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only. Neither worker `Program.cs` references `DemoSeeder`.

DI (`DependencyInjection.cs` L36–58) fail-closes without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave; Starwave `ProxyEnabled=false` hardcoded at `LiveMt5Registration.cs` L45). Hosted services: `LiveIngestHostedService`, `CTraderFixLogonHostedService`, `CopyTradingHostedService`. `grep FakeMt5` in `src/Infrastructure/` = **0**.

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still exists | `src/Infrastructure/Seeding/DemoSeeder.cs` | Tests + report `_tmp_*` harnesses only. Not referenced from `apps/`. |
| Integration tests call `DemoSeeder.SeedAsync` | `tests/Integration/SeedingAndStoreTests.cs` | Test host, not API boot. |
| mt5-worker still scores `{10001,10002,10003,99001}` | `apps/mt5-worker/Worker.cs` L31–35 | Residual dummy **scoring** loop. Hosted ingest on the API uses `ListLoginsWithDealsAsync` / catalog walk, not this worker’s four logins. Not DemoSeeder. |

Claim 1 **PASS**.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (source)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (full file 458/458):

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

- Primary walk: `GroupRequestArray("*")` — mask `*` is all manager-visible groups.
- Fallback when the request array is empty: `GroupTotal` + `GroupNext`.
- Dedup via `HashSet<string>` in `AddGroup`.
- Ingest: `DealIngestionService.SyncCatalogAsync` calls `connector.GetGroupsAsync` then `GetAccountsAsync(null, ct)`.

**Not proven this slot:** live Achiever+Starwave census (prior 8/6512 + 10/1948 = 18/8460). This slot did not attach. Claim is **capability from file**, not a re-probe.

Claim 2 **PASS_SOURCE**.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source)

`GetAccountsCore(null)` walks every group from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

Order of operations (file-proven):

1. `UserRequestArray(gname, users)` — request path, all users in that group/mask.
2. On hard fail (not OK / OK_NONE / NOTFOUND): pump-cache `UserGetByGroup`.
3. If still empty: `UserLogins` then `UserRequestByLogins`.
4. Accounts: `UserAccountRequestArray` then `UserAccountGetByGroup`.

`GetAccountsAsync(null)` (ingest + `/api/ops/resync` catalog) enumerates **all groups then all users**. No `Take`/`Skip` on this walk.

**Not proven this slot:** live trader count. Capability only.

Claim 3 **PASS_SOURCE**.

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135, full read).

Outbound MsgType construction is **only**:

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

Grep of **this file** for tag 35:

| Line | What |
|---|---|
| L55 | inbound `Extract(reply, "35")` |
| L73 | reject string `35={msgType}` |
| L96 | outbound `(35, "A")` |

`35=D` hits in this file: **0**. `NewOrderSingle` hits: **0**. Socket writes: **one** `ssl.WriteAsync` (L49) then one read, then `using` dispose of `TcpClient`/`SslStream`. One-shot Logon probe; no heartbeat, no `35=x`/`35=V`, no order.

Hosted caller `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212) and persists session rows. It never builds a `D`.

**Residual (not claim-4 FAIL):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139/163/197 and `CTraderFixDemoMatrix.Build("D")`. Demo-gated (`demo-` host / `demo.` sender / refuse account `1369850` / refuse `live-*`). `grep CTraderFixDemoTestTrade` under `apps/` and `src/Infrastructure/` = **0**. Not on the copy hop. Not `CTraderFixSession`.

Claim 4 **PASS**.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (disproven)

The assigned claim is that the flag **stays false**. The live API path binds it from env. Lab `.env` is `true`. Hosted logon does **not** overwrite it.

### 5.1 Env load (API)

`EnvFile.FindAndLoad()` (`src/Mt5/Env/EnvFile.cs` L5–20) walks cwd / parents then **`D:\Prop\.env`**, then `Load` `SetEnvironmentVariable` for every `KEY=value`.

`apps/api/Program.cs` L10 + L13: `FindAndLoad()` then `AddEnvironmentVariables()`.

Flag-only grep (no other keys, no values that are secrets):

| File | Line | Token |
|---|---|---|
| `D:\Prop\.env` | 73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| `D:\Prop\.env` | 106 | `FEATURE_COPY_TRADING_ENABLED=true` |

`grep REAL_COPY_EXECUTION` under `apps/**/appsettings*.json` = **0**. The live bind is env, not JSON.

### 5.2 DI bind (no hard-false)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`grep RealCopyEnabled =` under `src/` = **this one assignment only**.

`grep _runtime.RealCopyEnabled =` under `src/` = **0**. `CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for the log line (`RealCopyArmed={Armed}`) and does **not** assign false.

### 5.3 API surface echoes the runtime bit

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

`/api/health` also returns `realCopyEnabled = runtime.RealCopyEnabled` (L55).

Dead sibling: `apps/api/Controllers/SettingsController.cs` is **not** wired (`Program.cs` has no `AddControllers`/`MapControllers`). Its `LiveCopyEnabled` default-false is not the live hop.

### 5.4 POCO default is unused

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35). `AddTraderIntelligence` never `Configure<CTraderFixOptions>`. `apps/fix-worker/Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (different key, default false) and only logs it; it does not pin the API singleton.

`CTraderFixOptions` default-false **does not** keep `LiveRuntimeStatus.RealCopyEnabled` false on the API host.

### 5.5 What claim 5 is *not*

Copy hop is still `SAFE_BY_ABSENCE`:

| Gate | File evidence |
|---|---|
| No NOS builder on hosted session | `CTraderFixSession` 135/135 = Logon `35=A` only |
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L18 `const bool … = false` |
| `VenueReconciled` | L17 `const bool … = false` |
| Persist `AllowFixSend` | L306 `AllowFixSend = false` |
| Send `if` | L312 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` — then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED` |
| Hosted copy tick | `CopyTradingHostedService` L28–29 `TickRosterAsync` + `GenerateShadowIntentsAsync` only |
| `new ExecutionIntent` | **0** hits in `src/` |
| Blocker text | L468–469 `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` |

`SAFE_BY_ABSENCE` ≠ “flag stays false.” Claim 5 is about the **flag**. The flag is env-bound `true` on the API host.

Claim 5 **FAIL**.

---

## 6. Risk to capital

**NONE** today, because no ticket can leave `CTraderFixSession` and the copy service cannot construct NewOrderSingle.

**Not NONE if** a later change implements `35=D` while `.env` L73 remains `true` and DI continues to bind it with no logon re-pin. Next sender would see the runtime **armed**.

This slot did not live-attach, did not send, did not flip the flag.

---

## 7. What this slot did not do

- Did not modify product, tests, or `.env`.
- Did not print secrets.
- Did not Manager-connect Achiever or Starwave.
- Did not TLS-logon cTrader.
- Did not re-sum `LIVE_GROUPS_AND_TRADERS.json`.
- Did not GET `/api/settings` (loopback not required; bind is file-proven).

---

## 8. Slot close

| Item | Value |
|---|---|
| Slot | 95 |
| Verdict | **FAIL** |
| Evidence | Claims 1–4 file-proven. Claim 5 disproven: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 + no logon re-pin. |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE` on copy hop) |
