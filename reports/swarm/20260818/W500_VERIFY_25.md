# W500_VERIFY_25 — Adversarial live-path verifier (slot 25)

| Field | Value |
|---|---|
| Slot | **25** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do **not** trust sibling agents. |
| Assigned claims | (1) DemoSeeder is not the API startup path. (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`. (3) All traders via `UserRequestArray`/`UserLogins`. (4) `CTraderFixSession` has no `35=D`. (5) `REAL_COPY_EXECUTION` stays **false**. |
| Method | Independent `read_file` + `grep` of product HEAD. No Manager re-attach. No FIX TLS. No loopback HTTP. No `.env` secret dump. |
| Product source modified | **No** |
| Config / `.env` edited | **No** |
| Secrets printed | **None.** Quoted only flag names `=true`/`=false`. No MT5 / FIX / proxy / DB passwords. Tag 554 not dumped. |
| Binding rule | **FAIL if any assigned claim cannot be proven from the file.** |

**Honesty:** capability in source ≠ measured live census this slot. A POCO default of `false` is not a process pin. Binding env `true` arms a bit; it does not create a `NewOrderSingle`. Absence of a copy-hop `35=D` assembler is `SAFE_BY_ABSENCE`, not “the flag stays false.”

---

## 0. Verdict

| # | Claim | From-file proof? | Slot result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **Yes** | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | **Yes** (source capability; not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **Yes** (source capability; not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | **Yes** (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **No — disproven** | **FAIL** |

**Slot-25 overall: `FAIL`.**

Claim 5 is the fail. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API startup loads that file (`EnvFile.FindAndLoad` + `AddEnvironmentVariables`). DI **binds** the token onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host **does not** re-pin false. `/api/settings` and `/api/health` expose the runtime bit. Architecture/POCO/worker-fallback **defaults** remain `false`; that is not “stays false” on the live host.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Copy hop still has no `35=D` builder. Persist `AllowFixSend=false`. `NewOrderSingleImplemented=false`. This slot did not send and did not attach.

Stale reports (do not reuse): A002 (`DemoSeeder` on API startup), A001 (zero `GroupRequestArray` under `src`), E038 / CREDENTIALS “REAL_COPY forced false”, W500_68/108 “DI+hosted pin false”, A014 “DI pins `RealCopyEnabled=false`”.

---

## 1. DemoSeeder is not the API startup path — PASS

### 1.1 What the API process actually seeds

`D:\Prop\apps\api\Program.cs` (160 lines, full read):

```152:157:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`.

DI is live-only:

```15:15:D:\Prop\apps\api\Program.cs
builder.Services.AddTraderIntelligence(builder.Configuration);
```

`AddTraderIntelligence` throws unless both real MT5 password keys pass `IsSecret`, then registers **Native ×2** only (`LiveMt5Registration.CreateConnectors`). No `FakeMt5BrokerConnector` on that path.

### 1.2 Hosts that share the same seed

| Host | Startup seed | `DemoSeeder` token |
|---|---|---|
| `D:\Prop\apps\api\Program.cs` | `BrokerCatalogSeed.EnsureAsync` | **0** |
| `D:\Prop\apps\mt5-worker\Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` | **0** |
| `D:\Prop\apps\fix-worker\Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` | **0** |

`grep DemoSeeder` under `D:\Prop\apps` = **0**. Product C# callers of `DemoSeeder.SeedAsync` are `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` plus `_tmp_*` eval harnesses under reports. The class file **still exists** (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14). That is **not** API startup.

`BrokerCatalogSeed` writes Achiever + StarwaveFX catalog rows, XAUUSD, kill-switch default, and FIX session rows already `Disconnected` with “NewOrderSingle off” on TRADE. No Fake tape. No logins 10001/10002.

**Claim 1 proven.** Residual: unused `DemoSeeder.cs` remains on disk for tests.

---

## 2. Native can list all groups via GroupRequestArray or GroupTotal — PASS

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines, full read).

`GetGroupsCore` (L144–187):

1. **Primary (network request):** `GroupRequestArray("*", arr)` L155. Walk `arr.Next` when retcode is `MT_RET_OK` or `MT_RET_OK_NONE`.
2. **Fallback if `list.Count == 0`:** pump-cache `GroupTotal()` + `GroupNext` L174–180.

`GetAccountsAsync(null)` L189–214 first calls `GetGroupsCore()` then enumerates every returned name. Ingest is flag-blind:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` calls `SyncCatalogAsync` per registered Native connector (Achiever + StarwaveFX). `_pumpEnabled` never gates the group walk.

**Caveats (adversarial, not a claim fail):**
- This slot **did not** re-attach Manager. Capability is proven from the file; live 18-group census is **prior** (`LiveBrokerProbe` 2026-08-18T08:42:16Z, Achiever 8 + Starwave 10) and is **not** re-measured here.
- If `GroupRequestArray` fails **and** pump cache is cold (`PUMP_MODE_NONE` fallback Connect L101), `GroupTotal` can return 0. The code still *uses* the two assigned APIs.
- `*` is manager-ACL-visible groups, not “every group on the trade server regardless of manager rights.” That is the Manager contract.

**Claim 2 proven as source capability.**

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Same file, `ReadAccountsForGroup` L216–271:

1. **Primary:** `UserRequestArray(gname, users)` L223.
2. Hard-fail only (`not OK / OK_NONE / NOTFOUND`): cache `UserGetByGroup` L225.
3. If `users.Total() == 0`: `UserLogins(gname, out loginRes)` L230 then `UserRequestByLogins` L232.

`GetAccountsCore(null)` walks **every** group from claim 2 and unions by login. Catalog ingest uses that null mask (DealIngestionService L48). No `Take`/`Skip` on the connector or catalog path. The only leftover `Take(200)` is `GET /api/trades` reconstructed rows (`apps/api/Program.cs` L110) — HTTP page, not the Manager walk.

**Caveats:**
- Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Catalog persist is all accounts; scores may be a deals subset. That does **not** shrink the connector list.
- `UserGetByGroup` remains a pump-cache fallback. ALL-traders request path is still `UserRequestArray` / `UserLogins` as assigned.
- This slot did not re-probe trader counts. Prior cited census 6512 + 1948 = **8460** is **not** re-proven here.

**Claim 3 proven as source capability.**

---

## 4. CTraderFixSession has no 35=D — PASS

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` — **135/135** physical lines, full read this slot.

| Search | Count in this file |
|---|---|
| `NewOrderSingle` | **0** |
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `(35, "A")` | **1** — `BuildLogon` L96 |
| `WriteAsync` | **1** — L49, the logon frame |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one `ReadAsync` |

Outbound field list is Logon only: 35=A, 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554. No tag 11, 38, 40, 54. Hosted caller is `CTraderFixLogonHostedService` (QUOTE 5211 + TRADE 5212) and it never asks for a second write.

`grep` of `D:\Prop\src\Fix.CTrader\Sessions` for `35=D` / `Build("D")` in **this** class: **0**.

**Residual (does not falsify the assigned class):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Gated off live identity (`demo-` host, `demo.` sender, refuse account `1369850` / `live-*` / `live.*`). Only caller is `D:\Prop\tools\DemoFixTestTrade\Program.cs`. **Not** registered in DI. **Not** called from API, copy, or logon host. Copy service const `NewOrderSingleImplemented = false`. Persist `AllowFixSend = false` (`CopyTradingService` L211).

**Claim 4 proven for `CTraderFixSession`.**

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned sentence is the **flag stays false**, not “send stays impossible.”

### 5.1 What would prove “stays false”

A process pin: DI / hosted logon / settings API / committed config all force `false` regardless of env. That pin is **gone**.

### 5.2 What the files actually do

| Surface | Measured |
|---|---|
| Architecture / docs default | `false` (design law; not a runtime pin) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `= false` L35. **Not** bound from env (`Configure<CTraderFixOptions>` = 0 product hits). |
| fix-worker | `GetValue("CTrader:RealCopyExecutionEnabled", false)` **log-only**. Nested key, not `REAL_COPY_EXECUTION_ENABLED`. |
| `apps/api/appsettings.json` | **No** `REAL_COPY_EXECUTION_ENABLED` key. Leftover `FeatureFlags:LiveCopyEnabled=false` is unused (no `MapControllers`). |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no secret). L106 `FEATURE_COPY_TRADING_ENABLED=true`. |
| `EnvFile.FindAndLoad` | Loads `D:\Prop\.env` (hardcoded last candidate) into process env. API `Program.cs` L10 + L13. |
| DI | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` — **binds env**. `D:\Prop\src\Infrastructure\DependencyInjection.cs` L41. |
| `CTraderFixLogonHostedService` | Logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled`. **Does not** assign false. |
| `/api/settings` | `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` (`Program.cs` L76). **Not** a hardcoded false. |
| `/api/health` | `realCopyEnabled = runtime.RealCopyEnabled` L55. |
| Copy service | Passes `_runtime.RealCopyEnabled` into `RiskEngine` as `RealExecutionEnabled` L190. Then **overwrites** persist `AllowFixSend = false` L211. |

If the API host starts with the current lab `.env`, `LiveRuntimeStatus.RealCopyEnabled` is **true**. That is the opposite of “stays false.”

Unset-env default of the `string.Equals(..., "true")` expression is still `false`. That is a **default**, not a stay-false invariant.

`RiskEngine` will compute `AllowFixSend = true` when `RealExecutionEnabled && KillSwitch==None && Reconciled && VenueHealthy` (L147–150). Copy persist then **forces** `AllowFixSend=false` anyway. That is a persist override, **not** a false flag.

### 5.3 Why this is FAIL, not a papered PASS

Sibling slots that wrote `CONFIRMED_MUST_STAY_FALSE` while quoting `.env=true` + DI bind are **internally contradictory**. This slot refuses that. Claim 5 as written is **false on disk**.

What **is** still true (and is **not** claim 5):

- No copy-hop `35=D` (`SAFE_BY_ABSENCE`).
- `NewOrderSingleImplemented = false`, `VenueReconciled = false`.
- Persist `AllowFixSend := false`.
- `CanPromoteToLive` remains a closed door on the scorer (not re-quoted here as a claim).
- Next sender that trusts `runtime.RealCopyEnabled` will see **true** on this lab host.

**Claim 5 cannot be proven. Verdict FAIL.**

---

## 6. Risk to capital

| Path | Can lose dest capital this process? |
|---|---|
| API / ingest / catalog | **No** — Manager GET / request APIs only |
| `CTraderFixSession` | **No** — `35=A` then dispose |
| `CopyTradingService` / hosted tick | **No** — SHADOW rows; persist `AllowFixSend=false`; NOS unimplemented |
| `CTraderFixDemoTestTrade` | **Not on host hop.** Demo-gated CLI only. Not invoked this slot. |
| Flag arm | **Arms a bit.** Does not emit a ticket today. |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the copy hop). Do **not** treat this as a §68 / §70 go-live. Flip lab `.env` L73 back to `false` before anyone implements a sender.

This slot did **not** live-attach Achiever or Starwave. Do **not** cite 18/8460 as this-slot measured proof.

---

## 7. Files read (this slot)

- `D:\Prop\apps\api\Program.cs` (160/160)
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs` (partial)
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\api\Controllers\SettingsController.cs` (dead MVC leftover)
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header — unused by API)
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458)
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135)
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (gate + `Build("D")` residual)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (AllowFixSend compute)
- `D:\Prop\.env` L73 / L106 **booleans only**

---

## 8. One-liner

**FAIL.** Claims 1–4 proven from HEAD files. Claim 5 (`REAL_COPY_EXECUTION` stays false) is **disproven**: lab `.env` is `true` and DI binds it. Dest capital still **NONE** because the copy hop has no `35=D`.
