# W500_VERIFY_3 — Adversarial re-read of five live-path claims (slot 3)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_3.md` |
| Agent / slot | W500 **VERIFY 3** (adversarial; do not trust sibling agents) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` product tree (`apps/`, `src/`, lab `.env` **flag names only**) |
| Product source modified | **No.** This report is the only write. |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only as `REAL_COPY_EXECUTION_ENABLED=true` (L73) and `FEATURE_COPY_TRADING_ENABLED=true` (L106). |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. Broker manager logins in `BrokerCatalogSeed` not re-quoted. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Capability claims proven from current files only. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), `apps/api/TraderIntelligence.Api.csproj`, `apps/{api,mt5-worker,fix-worker}/Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `DealIngestionService.cs` (catalog), `LiveIngestHostedService.cs`, `CTraderFixSession.cs` (135/135), `CTraderFixDemoTestTrade.cs` (demo residual), `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs` (gates + persist), `BrokerCatalogSeed.cs` (startup seed), `EnvFile.cs`, `apps/api/appsettings.json`, `apps/fix-worker/Worker.cs`, `SettingsController.cs`. Targeted `grep` of `apps/` for `DemoSeeder`; of `src/Fix.CTrader/Sessions/CTraderFixSession.cs` for `35=D`; of `*.cs` for `RealCopyEnabled =`; of `.env` for the two flag **keys**. |
| Binding rule | **FAIL if any of the five claims cannot be proven from the file.** Prior swarm notes (A001 / A002 / A014 / W500_68 / W500_108 / CREDENTIALS) are **not** evidence. |

**Honesty:** a compile-time `= false` is a default. Binding env `true` is an **armed runtime bit**, not “stays false.” `SAFE_BY_ABSENCE` (no copy-hop `35=D` builder) is **not** the same claim as “`REAL_COPY_EXECUTION` stays false.” Fetch-all is Manager **read-only**. Wanting copy and no loss does not make the flag false.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1–4** are proven from the live files. Claim **5** is **not** proven and is **contradicted**: lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`, API `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` load it, and `DependencyInjection.cs` L41 copies that token onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted FIX logon **does not** re-pin false. One failed claim ⇒ slot FAIL.

| # | Claim | File proof | Class |
|---|---|---|---|
| 1 | DemoSeeder is **not** the API startup path | `apps/api/Program.cs` L152–156 `BrokerCatalogSeed.EnsureAsync` only. `grep DemoSeeder` under `D:\Prop\apps` = **0**. | **PASS** |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")` first; L174–180 `GroupTotal` + `GroupNext` if the request list is empty. | **PASS** (capability; not re-attached) |
| 3 | All traders via `UserRequestArray` / `UserLogins` | `ReadAccountsForGroup` L223 `UserRequestArray`; L230–232 `UserLogins` + `UserRequestByLogins` if array empty. `GetAccountsAsync(null)` walks every group from `GetGroupsCore`. Ingest L45–48 uses that path. | **PASS** (capability; not re-attached) |
| 4 | `CTraderFixSession` has **no** `35=D` | Full file 135/135. Only outbound MsgType is `(35, "A")` at L96. `grep 35=D` in that file = **0**. One `WriteAsync` (L49), then dispose. | **PASS** |
| 5 | `REAL_COPY_EXECUTION` **stays false** | **Cannot prove.** `.env` L73 `=true`. DI L41 binds it. Logon host L68–70 **logs** `RealCopyArmed` and never assigns `false`. `/api/settings` L76 exposes `runtime.RealCopyEnabled`. POCO default `false` is **unbound**. | **FAIL** |

One-line:

```text
SLOT 3 FAIL. DemoSeeder off API startup (BrokerCatalogSeed). Native GroupRequestArray("*") / GroupTotal + UserRequestArray / UserLogins exist. CTraderFixSession 35=A only. REAL_COPY does NOT stay false: .env L73 true, DI binds, no re-pin. Copy hop still SAFE_BY_ABSENCE (NOS const false). Risk to capital NONE.
```

Do **not** treat this FAIL as a license to send. Do **not** add `35=D` to `CTraderFixSession`. Do **not** wire `CTraderFixDemoTestTrade` into API/workers. Operator leftover: flip lab `.env` L73 back to `false` (this slot did **not** edit it).

---

## 1. Claim 1 — DemoSeeder is not the API startup path — **PASS**

### 1.1 Live API host

`D:\Prop\apps\api\Program.cs` (read 160/160):

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for **`BrokerCatalogSeed`**, not `DemoSeeder`. There is no `DemoSeeder.SeedAsync` token in this file.

### 1.2 Product hosts

| Host | Startup seed | `DemoSeeder` hits |
|---|---|---|
| `apps/api/Program.cs` L156 | `BrokerCatalogSeed.EnsureAsync` | **0** |
| `apps/mt5-worker/Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` | **0** |
| `apps/fix-worker/Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` | **0** |
| `grep DemoSeeder` under `D:\Prop\apps` | — | **0** |

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14) and is called from `tests/Integration/SeedingAndStoreTests.cs` L25 plus swarm `_tmp_*` harnesses. That is **not** API/worker startup.

### 1.3 DI after seed

`AddTraderIntelligence` (`DependencyInjection.cs` L36–49) throws unless both real MT5 password keys pass `HasRealPasswords`, then registers `LiveMt5Registration.CreateConnectors` (Native ×2). Fake/Demo connectors are **not** on the host DI path.

### 1.4 Stale siblings

- **A002** (“`Program.cs` still calls `DemoSeeder.SeedAsync`”) is **STALE**.
- **A005 / A010** (same DemoSeeder-on-startup claim) are **STALE** for the current files.

---

## 2. Claim 2 — Native connector can list all groups via `GroupRequestArray` or `GroupTotal` — **PASS**

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (read 458/458).

```144:186:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

| Fact | Evidence |
|---|---|
| Primary enumerator | `GroupRequestArray("*", arr)` L155 (mask `*` = manager-visible set) |
| Fallback | `GroupTotal()` + `GroupNext` L174–180 **only if** the request walk produced `list.Count == 0` |
| Plan-group env filter | **None** in this method |
| Pump gate | `_pumpEnabled` is **not** consulted; request-first even after `PUMP_MODE_NONE` retry (L101–110) |
| Live ingest uses it | `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` |

**What this slot does *not* prove:** a fresh Manager attach or a live count. Prior census 8+10 groups is **cited elsewhere, not re-measured here.** Completeness still depends on Connect success + manager ACL. The **code** can list via the two named APIs.

**Stale sibling:** A001 (“Zero hits for `GroupRequestArray` under `src`”; groups = cache only) is **STALE**.

---

## 3. Claim 3 — All traders via `UserRequestArray` / `UserLogins` — **PASS**

Same connector file.

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            // ... ReadAccountsForGroup per name, keyed by login
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

| Fact | Evidence |
|---|---|
| Primary | `UserRequestArray(gname, users)` L223 |
| Empty-array fallback | `UserLogins` + `UserRequestByLogins` L230–232 |
| Hard-fail fallback | pump-cache `UserGetByGroup` **only** if request retcode is not OK / OK_NONE / NOTFOUND (L224–225) |
| `group == null` | walks **every** name from `GetGroupsCore()` (L201–202) |
| Ingest | `SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)` — no `Take` / login allow-list |
| Hosted score residual | `LiveIngestHostedService` L106 scores `ListLoginsWithDealsAsync` only (catalog still upserts all accounts) |

A001 (“zero `UserRequestArray` / `UserLogins` under `src`”) is **STALE**.

This slot did **not** re-attach; “all traders” is proven as the **code path**, not as a new 8460 count.

---

## 4. Claim 4 — `CTraderFixSession` has no `35=D` — **PASS**

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (read **135/135**).

`grep` of that file for `35=D` / `NewOrderSingle` / `(35, "D")` = **0**.

Only outbound MsgType:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        var sendingTime = DateTime.UtcNow.ToString("yyyyMMdd-HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            // 49, 56, 50, 57, 52, 98, 108, 141, 553, 554
        };
        return Assemble(fields);
    }
```

| Fact | Evidence |
|---|---|
| Public API | `TryLogonAsync` only |
| Wire writes | **one** `ssl.WriteAsync` (L49) of `BuildLogon` |
| After one `ReadAsync` | `using` disposes `TcpClient` / `SslStream` — no heartbeat, no NOS |
| Inbound `35` | parsed only to decide LoggedOn vs Error (L55–75) |
| Hosted caller | `CTraderFixLogonHostedService` L48–58 calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). Log L69: “NewOrderSingle still unimplemented.” |

**Residual (does not falsify claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 can assemble NewOrderSingle. It is **not** `CTraderFixSession`. Callers: `tools/DemoFixTestTrade/Program.cs` L44 only. Gate at L43–47 refuses `live-*` / `live.` / account `1369850`. **Not** registered in API/DI/workers.

Copy hop still cannot send: `CopyTradingService.NewOrderSingleImplemented = false` (L17), `VenueReconciled = false` (L16), persist `AllowFixSend = false` (L211), no `ExecutionIntent` writer on this hop.

---

## 5. Claim 5 — `REAL_COPY_EXECUTION` stays false — **FAIL**

This is the claim that fails the slot. Policy *should* keep the flag false. The **live files do not keep it false**.

### 5.1 What “stays false” would require

All of: committed defaults false, lab `.env` false, runtime bit not bound to a leftover `true`, or a hard re-pin after load. **None of those last three hold.**

### 5.2 Measured flag surfaces

| Surface | Measured | Stays false? |
|---|---|---|
| Architecture / `docs/architecture.md` L20 | `REAL_COPY_EXECUTION_ENABLED=false` | policy only |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | `= false` | default only; **no** `services.Configure<CTraderFixOptions>` in DI |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different name**; unused by `LiveRuntimeStatus` |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; log-only; stamps sessions `Disconnected` |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **NO** |
| `EnvFile.FindAndLoad()` | API `Program.cs` L10; candidates include `D:\Prop\.env` (`EnvFile.cs` L14) | loads leftover |
| `AddEnvironmentVariables()` | API `Program.cs` L13 | binds into `IConfiguration` |
| `DependencyInjection.cs` L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **binds leftover `true`** |
| `CTraderFixLogonHostedService` | L68–70 logs `RealCopyArmed={Armed}`; **no** `_runtime.RealCopyEnabled = false` | **no re-pin** |
| `grep RealCopyEnabled =` in `*.cs` | **only** DI L41 | no other writer |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (`Program.cs` L76) | **follows env** |
| `/api/health` `realCopyEnabled` | L55 same bit | **follows env** |
| `CopyTradingService.GetStatusAsync` | `RealCopyArmed: _runtime.RealCopyEnabled` (L44) | **follows env** |
| `CopyTradingService` send gate L217 | requires `NewOrderSingleImplemented && VenueReconciled` (both **const false**) + `decision.AllowFixSend` (persist **forced false** L211) | send still blocked |
| MVC `SettingsController` | `LiveCopyEnabled` from `FeatureFlags:LiveCopyEnabled` (default false); **not** the §41 token; **not** mapped as a Minimal API competitor if that controller is even constructed (Redis ctor; Redis not registered in `Program.cs`) | irrelevant to claim |

### 5.3 Why this is FAIL, not “PASS with residual”

The assigned claim is **“REAL_COPY_EXECUTION stays false.”** That is a **flag-state** claim.

Adversarial reading:

1. I **cannot** prove the flag stays false from the files.
2. I **can** prove the opposite bind: leftover `true` → `LiveRuntimeStatus.RealCopyEnabled == true` on the next API start that loads `D:\Prop\.env`.
3. W500_68 / W500_108 / `reports/CREDENTIALS_AND_COPY_STATUS.md` (“forced false”) are **STALE**.

`SAFE_BY_ABSENCE` of a copy-hop `35=D` builder is **true** and **independent**. It does **not** rescue claim 5.

`CopyTradingService.BuildBlockers` L316–317 only adds `"REAL_COPY_EXECUTION_ENABLED is false"` when the bit is already false — so when the leftover is armed, that blocker **disappears**. Remaining blockers (`NewOrderSingleImplemented`, `VenueReconciled`, 0 LIVE, FIX not logged on) still keep send closed. That is residual safety, not a true flag.

---

## 6. Copy / capital (not a sixth claim; required for risk)

| Gate | File | State |
|---|---|---|
| NOS builder on hosted session | `CTraderFixSession` | **absent** (`35=A` only) |
| `NewOrderSingleImplemented` | `CopyTradingService` L17 | `const false` |
| `VenueReconciled` | L16 | `const false` |
| Persist `AllowFixSend` | L211 | **literal `false`** |
| LIVE auto-promote | (not re-proven this slot; not required for FAIL) | — |
| Demo `Build("D")` | `CTraderFixDemoTestTrade` + `tools/DemoFixTestTrade` | **off hop**, demo-gated |
| Fetch ALL | Manager request APIs | **read-only** |

**Risk to capital from the API/copy hop: NONE (`SAFE_BY_ABSENCE`).** Dest Pepperstone/cTrader cannot receive a product NewOrderSingle from `CTraderFixSession` / `CopyTradingService`. The FAIL is **flag honesty**, not a live ticket.

---

## 7. Stale notes this slot supersedes (for these five claims only)

| Note | Why stale |
|---|---|
| A001 groups/traders = cache only; 0 `GroupRequestArray` / `UserRequestArray` in `src` | Live connector L155 / L223 |
| A002 API startup = `DemoSeeder` | Live startup = `BrokerCatalogSeed` |
| W500_68 / W500_108 / CREDENTIALS “REAL_COPY forced false” | DI binds env; `.env` L73 `true`; logon pin gone |
| A014 “`/api/settings` REAL_COPY from `runtime` therefore false” | runtime is **no longer** pinned false |
| Any “product `35=D=0`” that ignores `CTraderFixDemoTestTrade` | Claim 4 is still true **for `CTraderFixSession`**; product sibling can `Build("D")` off-hop |

---

## 8. Checklist (assigned)

- [x] DemoSeeder is not the API startup path — **PASS** (`BrokerCatalogSeed` L156; 0 `DemoSeeder` under `apps/`)
- [x] Native can list all groups via `GroupRequestArray` or `GroupTotal` — **PASS** (L155 / L174; capability only)
- [x] All traders via `UserRequestArray` / `UserLogins` — **PASS** (L223 / L230; `GetAccountsAsync(null)`)
- [x] `CTraderFixSession` has no `35=D` — **PASS** (135/135; only `(35, "A")`)
- [x] `REAL_COPY_EXECUTION` stays false — **FAIL** (`.env` L73 `true` + DI L41 bind + no re-pin)
- [x] Secrets not printed
- [x] Product source not edited
- [x] Verdict FAIL because one claim cannot be proven from the file

---

## 9. Slot JSON (mirrors footer contract)

```json
{
  "slot": 3,
  "verdict": "FAIL",
  "evidence": "C1 PASS: apps/api/Program.cs L156 BrokerCatalogSeed only; grep DemoSeeder under apps=0. C2 PASS: NativeMt5BrokerConnector L155 GroupRequestArray(\"*\") then L174 GroupTotal. C3 PASS: L223 UserRequestArray + L230 UserLogins; ingest GetAccountsAsync(null). C4 PASS: CTraderFixSession 135/135 only (35,\"A\") L96; 35=D=0. C5 FAIL: .env L73 REAL_COPY_EXECUTION_ENABLED=true; EnvFile+AddEnvironmentVariables; DI L41 binds LiveRuntimeStatus.RealCopyEnabled; logon host does not re-pin; /api/settings L76 exposes the bit. Residual: CopyTradingService NewOrderSingleImplemented=false, AllowFixSend persist false; demo Build(D) tools-only. Not re-attached.",
  "risk_to_capital": "NONE (SAFE_BY_ABSENCE on copy hop; flag armed is not a ticket)"
}
```
