# W500_VERIFY_96 — Adversarial live-path verify (slot 96)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_96.md` |
| Agent / slot | W500 adversarial **verify 96** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A002 / A014 / A015 / CREDENTIALS / INDEX prose. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs` (18/18), `apps/mt5-worker/Program.cs` (18/18), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs` (66/66), `CTraderFixOptions.cs` (80/80), `CTraderFixLogonHostedService.cs` (112/112), `CopyTradingService.cs` (const + persist + send-if + blockers), `DealIngestionService.SyncCatalogAsync`, `LiveIngestHostedService` scoring loop, `CopyTradingHostedService.cs` (43/43), `LiveMt5Registration.cs` (94/94), `EnvFile.cs` (41/41), `BrokerCatalogSeed` FIX-row insert, `CTraderFixDemoTestTrade` demo gate + `Build("D")`, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/Controllers/SettingsController.cs`. Targeted `grep` of `DemoSeeder` (apps vs tests), `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `new ExecutionIntent`, `FakeMt5`, `RealCopyEnabled =`. Flag-only read of `D:\Prop\.env` L73 / L106. |

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

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`; `new ExecutionIntent` writers = **0**. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A014 “DI pins false”; A015 “logon sets `_runtime.RealCopyEnabled = false`”; A006 / older P500_CODE slots “`/api/settings` hardcodes FEATURE false”; `SettingsController` is **not** the live `/api/settings` hop (`Program.cs` has no `AddControllers` / `MapControllers`).

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

`D:\Prop\apps\fix-worker\Program.cs` L15 and `D:\Prop\apps\mt5-worker\Program.cs` L15 both call `BrokerCatalogSeed.EnsureAsync` only.

DI (`DependencyInjection.cs` L36–58) fail-closes without real MT5 passwords (`LiveMt5Registration.HasRealPasswords`), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** (Achiever + Starwave; Starwave `ProxyEnabled=false` hardcoded at `LiveMt5Registration.cs` L45). Hosted services: `LiveIngestHostedService`, `CTraderFixLogonHostedService`, `CopyTradingHostedService`. `grep FakeMt5` in `Infrastructure/` product DI = **0** (Fake exists only as a leftover class + DemoSeeder factory).

### 1.3 Residual (does not put DemoSeeder on API startup)

| Residual | Path | Why it is not claim-1 FAIL |
|---|---|---|
| `DemoSeeder` class still on disk | `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder` | Tests + leftover file. **Not** called from `apps/`. |
| Integration test still seeds it | `tests/Integration/SeedingAndStoreTests.cs` L25 | Test host, not API process. |
| Report `_tmp_*` harnesses call it | `reports/swarm/20260818/_tmp_*` | Eval sandboxes, not API. |
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

```45:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
```

`GetAccountsAsync(null)` re-enters `GetGroupsCore` (`GetAccountsCore` L201–202).

### 2.1 What this file proves

- Primary enumerator is the request API `GroupRequestArray("*")` — the official ALL-groups mask.
- If that walk yields **zero** rows, cache walk `GroupTotal` + `GroupNext` runs.
- Fetch is **flag-blind** (`_pumpEnabled` is recorded at Connect L90–110 but **never** gates `GetGroupsCore`).

### 2.2 What this file does **not** prove (residuals, not claim-2 FAIL)

| Residual | Why |
|---|---|
| Live census 18/8460 | Prior probe JSON. **This slot did not attach.** Do not re-certify the count. |
| `GroupRequestArray` fail with non-empty-but-partial | If the request returns OK with a truncated set, `list.Count != 0` skips `GroupTotal`. File cannot prove server completeness without a live attach. |
| YoPips C++ `GetAllGroups` is cache-only | Different tree. C# path is request-first. |

**Claim 2 proven as source capability.** Not live-proven this slot.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

`ReadAccountsForGroup`:

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

`GetAccountsCore` with `group == null` walks **every** group from `GetGroupsCore` (L199–202). Ingest and `LiveIngestHostedService` call `GetAccountsAsync(null)` / `SyncCatalogAsync`.

### 3.1 What this file proves

- Primary trader pull is `UserRequestArray(gname, users)` (network request).
- Empty array after request → `UserLogins` then `UserRequestByLogins`.
- Cache `UserGetByGroup` only on **hard fail** of the request (not OK / OK_NONE / NOTFOUND).
- `GetAccountsAsync(null)` is the ALL-traders composition over ALL groups.

### 3.2 What this file does **not** prove

| Residual | Why |
|---|---|
| Live 8460 traders | Prior census. Not re-attached. |
| Hosted **score** set | `LiveIngestHostedService` L106 scores `ListLoginsWithDealsAsync` only (deals-present). Catalog still upserts all accounts. Claim is list-all, not score-all. |
| `mt5-worker/Worker.cs` dummy four-login scorer | Leftover; hosted API ingest does not use it. |
| Manual `/api/ops/resync` | Scores `ListLoginsAsync` (all catalog logins), not just deals. Still not DemoSeeder. |

**Claim 3 proven as source capability.** Not live-proven this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

`D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` is **135/135** lines. Full read this slot.

Outbound MsgType is built once:

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

The only wire write is `ssl.WriteAsync` of that Logon (L47–50). Socket is `using`/`await using` and disposed on return. Inbound `35` is **parsed** (L55–56) to decide LoggedOn; it is not sent.

`grep` of **this file** for `35=D` / `NewOrderSingle` / `"D"` as MsgType = **0**. Tag 35 appears twice: send `"A"` (L96) and compare inbound `== "A"` (L56).

### 4.1 Residual that is **not** CTraderFixSession (does not fail claim 4)

Sibling `CTraderFixDemoTestTrade` (`Sessions/CTraderFixDemoTestTrade.cs`) **can** `Build("D")` at L139 / L163 / L197. That is a **different type**. Callers: `tools/DemoFixTestTrade/Program.cs` only. Gate at L43–47 refuses `live-*` / `live.` / account `1369850`. Not in DI. Not called by API / workers / copy hosted service.

Hosted logon (`CTraderFixLogonHostedService` L48–58) calls **only** `CTraderFixSession.TryLogonAsync` twice (QUOTE 5211, TRADE 5212) then persists status. No NOS.

**Claim 4 proven** for the assigned type. W500_148 “product tree has only 35=A” would be **stale** if applied to the whole `Fix.CTrader` folder; this claim is `CTraderFixSession` only.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is a **runtime pin**, not a POCO default and not “sender missing.”

### 5.1 Lab env is `true`

`D:\Prop\.env` L73 (boolean only; no other keys from this region quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (boolean only): `FEATURE_COPY_TRADING_ENABLED=true`.

### 5.2 API loads that file and binds it

`apps/api/Program.cs` L10 `EnvFile.FindAndLoad()` — `EnvFile.cs` L15 includes the literal path `D:\Prop\.env`; L38 `Environment.SetEnvironmentVariable(key, value)`.

L13 `builder.Configuration.AddEnvironmentVariables()`.

DI is the **only** `RealCopyEnabled =` assignment in product C#:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`grep RealCopyEnabled =` under `*.cs` = **that one line**. Hosted logon **reads** `_runtime.RealCopyEnabled` to log `RealCopyArmed` (L69–70). It never writes the property.

### 5.3 Surfaces that expose the armed bit

| Surface | Behavior |
|---|---|
| `GET /api/settings` (`Program.cs` L71–77) | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` |
| `GET /api/health` L55 | `realCopyEnabled = runtime.RealCopyEnabled` |
| `LiveRuntimeStatus.Snapshot()` L41–44 | copyNote says “REAL_COPY armed…” when true |
| `CopyTradingService.GetStatusAsync` L46 | `RealCopyArmed: _runtime.RealCopyEnabled` |
| `CopyTradingService` L285 | `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine.Evaluate` |

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults false** (L35) but DI **does not bind** that POCO. `apps/fix-worker/Worker.cs` L21 reads a **different** key `CTrader:RealCopyExecutionEnabled` (default false) for a log line only; it does not re-pin `LiveRuntimeStatus`.

Dead `SettingsController` (`apps/api/Controllers/SettingsController.cs`) is unused: API `Program.cs` has **no** `AddControllers` / `MapControllers`. Live hop is the minimal-API map.

### 5.4 Why “stays false” is false

On a host that starts from `D:\Prop` (or any cwd that `EnvFile` can walk to `D:\Prop\.env`):

1. `.env` L73 is `true`.
2. `EnvFile` injects it into the process environment.
3. DI copies it onto the singleton `LiveRuntimeStatus`.
4. Nothing later sets it back to false.

Architecture docs / README / `docs/architecture.md` still **say** the flag should be false. That is policy, not the running bind. Claim 5 is the running bind.

**Claim 5 FAIL.** W500_68 / W500_108 “pinned false” is stale. POCO default false is not the API path.

---

## 6. Copy hop remains SAFE_BY_ABSENCE (not claim 5)

This section is **capital risk**, not a rescue of claim 5.

| Gate | File proof |
|---|---|
| No NOS builder on `CTraderFixSession` | Claim 4. |
| `CopyTradingService.NewOrderSingleImplemented = false` | L18 const. |
| `VenueReconciled = false` | L17 const. |
| Persist `AllowFixSend = false` | L306 **forced**, ignores `decision.AllowFixSend`. |
| Send `if` still cannot emit | L312 requires `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`; then only sets status `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. |
| `new ExecutionIntent` | **0** product writers. |
| Hosted copy tick | `CopyTradingHostedService` L28–29: `TickRosterAsync` + `GenerateShadowIntentsAsync` only. |
| HEAD size | `XauUsdOneToOneCopyPolicy.AllocationFactor = 1m` (1:1). Ruin **if** a sender existed. No sender. |
| Demo `Build("D")` | Tools-only + demo-gated. Not on copy hop. |

`RiskEngine` L147–150 **can** compute `AllowFixSend = true` if `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy`. Copy persist then **overwrites** that to false (L306). The armed env flag is therefore a **next-sender** hazard, not a ticket today.

---

## 7. Stale pins this slot re-kills

| Older claim | Status now |
|---|---|
| A002 API still `DemoSeeder.SeedAsync` | **STALE** — `BrokerCatalogSeed` only. |
| A001 C# has no `UserRequestArray` / `UserLogins` | **STALE** — wired L223 / L230. |
| A015 / W500_68 / W500_108 hosted re-pins `RealCopyEnabled=false` | **STALE** — logon only logs `RealCopyArmed`. |
| `/api/settings` FEATURE literal `false` | **STALE** — L77 is literal `true`. REAL_COPY is runtime-bound. |
| “product 35=D = 0 everywhere” | **STALE** if applied to `CTraderFixDemoTestTrade`. **TRUE** for assigned `CTraderFixSession`. |
| CREDENTIALS “REAL_COPY false (forced)” | **STALE**. |

---

## 8. What this slot did **not** do

- Did not start the API, workers, or probe.
- Did not `Connect` Manager or open FIX TLS.
- Did not send or attempt `35=D`.
- Did not print secrets (no passwords, no tag 554 values, no proxy auth).
- Did not edit product / test / `.env`.

---

## 9. Bottom line

Slot 96 **FAIL**.

1. DemoSeeder is **not** the API startup path — **PASS**.
2. Native **can** list groups via `GroupRequestArray("*")` or `GroupTotal` — **PASS** (source).
3. Native **can** list traders via `UserRequestArray` then `UserLogins` — **PASS** (source).
4. `CTraderFixSession` has **no** `35=D` — **PASS**.
5. `REAL_COPY_EXECUTION` does **not** stay false — **FAIL** (`.env` L73 `true` + DI L41; no re-pin).

Destination capital risk **today: NONE** (`SAFE_BY_ABSENCE`). The flag is **armed**. That is the finding.
