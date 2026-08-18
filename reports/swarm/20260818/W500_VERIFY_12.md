# W500_VERIFY_12 — Adversarial live-path re-read (slot 12)

| Field | Value |
|---|---|
| Slot | **12** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Assigned claims | (1) DemoSeeder is not the API startup path. (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`. (3) All traders via `UserRequestArray` / `UserLogins`. (4) `CTraderFixSession` has no `35=D`. (5) `REAL_COPY_EXECUTION` stays false. |
| Rule | **FAIL the slot if any claim cannot be proven from the file.** Never print secrets. |
| Product source | **Not modified.** Report only. |
| Live attach | **Not performed.** Runtime census (groups/traders counts) is **unproven** this slot. |

---

## Verdict

**FAIL**

Claims 1–4 are proven from the files on disk. Claim 5 is **disproven**: lab `.env` arms `REAL_COPY_EXECUTION_ENABLED=true` and API DI **binds** that string onto `LiveRuntimeStatus.RealCopyEnabled`. Nothing on the API startup path re-pins it false. Reports that still say “DI hard-false / hosted re-pin” are **stale**.

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (capability) | `GetGroupsCore` calls `GroupRequestArray("*", arr)` first; if the list is empty, iterates `GroupTotal()` + `GroupNext`. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (capability) | `GetAccountsAsync(null)` walks every group; `ReadAccountsForGroup` uses `UserRequestArray` then `UserLogins` + `UserRequestByLogins` when the user array is empty. Ingest calls `GetAccountsAsync(null)`. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: only outbound MsgType is `(35, "A")`. Zero `35=D` / `(35, "D")` / `NewOrderSingle`. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | `.env` L73 is `true`. `DependencyInjection.cs` L41 binds it. Logon host does **not** overwrite. `/api/settings` and `/api/health` expose `runtime.RealCopyEnabled`. |

**Slot rule:** one unproven/false claim → **FAIL**.

**Risk to capital:** **NONE** on the copy hop (`SAFE_BY_ABSENCE`). `CTraderFixSession` cannot emit `NewOrderSingle`. `CopyTradingService.NewOrderSingleImplemented` is `const false`. Persist writes `AllowFixSend = false` even if `RiskEngine` would approve. That does **not** rescue claim 5. The flag itself does **not** stay false.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 What the API process actually runs

`D:\Prop\apps\api\Program.cs` (160 lines). After endpoint maps:

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

There is **no** `DemoSeeder.SeedAsync`. Grep of this file for `DemoSeeder`: **0 hits**. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`.

Same seed path on workers (not API, cited only as corroboration):

- `D:\Prop\apps\mt5-worker\Program.cs` L11–16: `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\fix-worker\Program.cs` L11–16: `BrokerCatalogSeed.EnsureAsync`

Grep `DemoSeeder` under `D:\Prop\apps`: **0 hits**.

### 1.2 Where DemoSeeder still lives (not startup)

| Path | Role |
|---|---|
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 | Class still on disk. Fake/demo tape. |
| `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 | Test calls `DemoSeeder.SeedAsync`. |
| `D:\Prop\reports\swarm\20260818\_tmp_*\Program.cs` | Scratch evals. Not a host. |

`DemoSeeder` remaining on disk does **not** make it the API startup path. A002 (“API still calls `DemoSeeder`”) is **stale**.

### 1.3 DI refuse-dummy (related, not the claim)

`DependencyInjection.AddTraderIntelligence` throws if real MT5 passwords are missing (`LiveMt5Registration.HasRealPasswords`) and registers **only** `NativeMt5BrokerConnector` instances. Fake connectors are not on the API DI path.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS (capability)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

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

- Primary: `GroupRequestArray("*", …)` — Manager mask for **all** groups.
- Fallback: `GroupTotal()` + `GroupNext` **only when** the request-array list is empty.

Live ingest uses this path: `DealIngestionService.SyncCatalogAsync` → `connector.GetGroupsAsync` (`DealIngestionService.cs` L45). `LiveIngestHostedService` and `/api/ops/resync` call `SyncCatalogAsync` / `SyncBrokerAsync`.

### Caveats (not enough to fail the capability claim)

- This slot **did not** attach Manager. File proof ≠ measured 18/8460 census.
- If `GroupRequestArray` returns a **non-empty partial** set, `GroupTotal` is **not** consulted. Completeness then depends on the Manager request API, not a union of both.
- `GroupTotal`/`GroupNext` are pump-cache APIs. Connect tries pump first (`PUMP_MODE_GROUPS|USERS|POSITIONS`); on failure it reconnects with `PUMP_MODE_NONE` (`NativeMt5BrokerConnector.cs` L88–111). Request-array is the intended pump-independent path.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

Same connector file.

`GetAccountsCore(null)` enumerates **every group name** from `GetGroupsCore()`, then `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Live catalog calls `GetAccountsAsync(null, ct)` (`DealIngestionService.cs` L48 and L62). Probe `tools/LiveBrokerProbe/Program.cs` L26 does the same.

Proven from the file: the connector **can** enumerate traders with `UserRequestArray` and, when that array is empty, `UserLogins` + `UserRequestByLogins`. Dedup is by login (`Dictionary<ulong, Mt5AccountDto>`).

### Caveats

- There is **no** single `UserRequestArray("*")` global call. “All traders” = union over groups returned by claim 2. If groups are incomplete, traders are incomplete.
- `UserLogins` runs **only** when `users.Total() == 0`. A non-empty partial `UserRequestArray` result **skips** the login-list fallback.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (deals-only), not every catalog login. Residual: `apps/mt5-worker/Worker.cs` L31–35 still scores `{10001,10002,10003,99001}`. That does **not** shrink the native list APIs.

Runtime head-count is **unproven** this slot (no attach).

---

## 4. CTraderFixSession has no 35=D — PASS

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Grep of this file for `35=D`, `(35, "D")`, `"D"`, `NewOrderSingle`: **0 hits**.

Only outbound builder:

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
```

Single `WriteAsync` (L49) of that Logon. Socket/`SslStream` disposed via `using`. Inbound `Extract(reply, "35")` is read-only (accepts `"A"` or records reject). Hosted caller is `CTraderFixLogonHostedService` → `TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No order send.

**Sibling files are out of claim scope** but must not be papered over:

| File | `35=D`? | On copy/API hop? |
|---|---|---|
| `CTraderFixSession.cs` | **No** | Yes (logon only) |
| `CTraderFixDemoTestTrade.cs` | `Build("D")` ×3 | **No.** `tools/DemoFixTestTrade` only. Demo-gated (`demo-` host / `demo.` sender; refuse `live-*` / `live.` / account `1369850`). |
| `CTraderFixDemoMatrix.cs` | `Build("D")` | **No.** Matrix helper, not DI. |

Copy hop: `CopyTradingService.NewOrderSingleImplemented = false` (`CopyTradingService.cs` L17). Persist always `AllowFixSend = false` (L211).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

This is the slot-killing claim. “Stays false” is a **runtime flag** claim, not a “sender missing” claim.

### 5.1 Lab env is armed (boolean only; no secrets)

`D:\Prop\.env` L73:

```
REAL_COPY_EXECUTION_ENABLED=true
```

API host loads that file **before** DI:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes the hard path `D:\Prop\.env` (`EnvFile.cs` L14) and `SetEnvironmentVariable` for every `KEY=value` line.

### 5.2 DI binds the env string (no hard-false pin)

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

If configuration is the string `true` (case-insensitive), `LiveRuntimeStatus.RealCopyEnabled` is **true**.

A014’s quote of `RealCopyEnabled = false` in this file is **STALE**.

### 5.3 Nothing re-pins false after bind

`CTraderFixLogonHostedService` **reads** `_runtime.RealCopyEnabled` for a log line (L68–70). It does **not** assign `false`. `CopyTradingHostedService` does not touch the flag. `RiskEngine.Evaluate` uses `request.RealExecutionEnabled` and **can** set `AllowFixSend = true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (`RiskEngine.cs` L147–150). Copy persist then **overwrites** the stored row to `AllowFixSend = false` (`CopyTradingService.cs` L211) — a send brake, not a flag pin.

### 5.4 API advertises the bound value

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

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L44). `BuildBlockers` only adds `"REAL_COPY_EXECUTION_ENABLED is false"` when the runtime bit is false (L316–317). With `.env=true`, that blocker is **absent**.

### 5.5 What is still false (does not save the claim)

| Surface | Value | Bound to env `REAL_COPY_EXECUTION_ENABLED`? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` default | `false` | **No.** API DI does not populate this POCO from the env key. Unused by logon host. |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **No.** Nested key; logs only; worker still stamps sessions `Disconnected`. |
| `apps/api/appsettings.json` `FeatureFlags.LiveCopyEnabled` | `false` | **No.** Different name. `/api/settings` does not read it. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | N/A (send absence) |
| Persist `RiskDecisionRecord.AllowFixSend` | hardcoded `false` | N/A (send absence) |

POCO default false + send-by-absence ≠ “`REAL_COPY_EXECUTION` stays false.” On the API process with `D:\Prop\.env` loaded, the flag is **true**.

---

## Stale reports this re-read kills

| Prior claim | Status vs files today |
|---|---|
| A002: API startup still `DemoSeeder` | **STALE.** Startup is `BrokerCatalogSeed` only. |
| A014: DI pins `RealCopyEnabled = false` | **STALE.** DI binds env. |
| W500_68 / 108 “flag pinned false in DI + hosted + .env” | **STALE.** `.env` true; DI binds; hosted does not re-pin. |
| “Product `35=D` = 0 everywhere” | **STALE if global.** True for `CTraderFixSession`. False for `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix`. |

---

## Residuals (not slot claims)

- `DemoSeeder.cs` remains for tests.
- `mt5-worker/Worker.cs` still scores four dummy logins after a real `SyncBrokerAsync`.
- `GET /api/trades` still `Take(200)` (dashboard page, not Manager enumeration).
- Sibling demo FIX helper can `Build("D")` off the copy hop, demo-gated.
- Next implemented sender would see **runtime armed** (`RealCopyEnabled=true`) even while today’s sender is absent.

---

## Secrets

No passwords, tokens, or connection strings printed. `.env` quoted **only** as the boolean `REAL_COPY_EXECUTION_ENABLED=true`.

---

## Bottom line

| Claim | File-proven? |
|---|---|
| 1 DemoSeeder ≠ API startup | **Yes — PASS** |
| 2 Groups via `GroupRequestArray` / `GroupTotal` | **Yes (capability) — PASS** |
| 3 Traders via `UserRequestArray` / `UserLogins` | **Yes (capability) — PASS** |
| 4 `CTraderFixSession` has no `35=D` | **Yes — PASS** |
| 5 `REAL_COPY_EXECUTION` stays false | **No — FAIL** (env true + DI bind; no re-pin) |

**Slot 12 verdict: FAIL.** Risk to capital **NONE** (`SAFE_BY_ABSENCE` on `CTraderFixSession` + unimplemented NOS). Do not treat the armed flag as a send license; do not claim the flag stays false.
