# W500_VERIFY_28 — Adversarial live-path re-read (slot 28)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_28.md` |
| Slot | **28** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live path files. Do **not** trust other agents. |
| Assigned | Confirm: (1) DemoSeeder is not the API startup path; (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`; (3) all traders via `UserRequestArray` / `UserLogins`; (4) `CTraderFixSession` has no `35=D`; (5) `REAL_COPY_EXECUTION` stays false. |
| Product source modified | **No** |
| Test source modified | **No** |
| Secrets printed | **None** (flag boolean only; no passwords, no FIX password, no proxy auth) |
| Manager re-attach this slot | **No** (census 18/8460 is prior; not re-proven here) |
| Local API re-probe | **No** |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `CTraderFixLogonHostedService.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `EnvFile.cs`, both worker `Program.cs`, `BrokerCatalogSeed.cs` header, `DemoSeeder.cs` header. Grep of `DemoSeeder` under `apps/`, `35=D`/`NewOrderSingle` under `Fix.CTrader`, `REAL_COPY_EXECUTION_ENABLED` in `.env` (key+boolean only), `RealCopyEnabled` under `src/`. |

**Honesty rule:** FAIL any claim that cannot be proven from the file just read. Prior swarm reports are **not** evidence. `W500_RESEARCH_28.md` claimed DI/logon/`.env` pins false — that is **stale** and is contradicted by the current tree.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from current files. Claim 5 is **disproven**.

`REAL_COPY_EXECUTION_ENABLED` does **not** stay false. Lab `.env` L73 is `true`. API startup loads that file (`EnvFile.FindAndLoad` → `AddEnvironmentVariables`). DI copies the string `"true"` onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted FIX logon **does not** re-pin false. `/api/settings` exposes the runtime value, not a hardcoded `false`.

Live send is still **off by absence** (`CTraderFixSession` outbound is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). That is **not** the same as “the flag stays false.”

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | API + both workers seed `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** | `GetGroupsCore` requests `"*"` first; cache `GroupTotal`/`GroupNext` only if that list is empty. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** | `ReadAccountsForGroup` calls `UserRequestArray` first; empty → `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: only outbound MsgType is `(35, "A")`. Grep `35=D` / `NewOrderSingle` in this file = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Env **true**, DI **binds**, no re-pin. POCO default `false` is unused by `LiveRuntimeStatus`. |

```text
VERDICT=FAIL
  C1 PASS  API startup = BrokerCatalogSeed; DemoSeeder tests-only
  C2 PASS  GroupRequestArray("*") then GroupTotal
  C3 PASS  UserRequestArray then UserLogins
  C4 PASS  CTraderFixSession 35=A only
  C5 FAIL  REAL_COPY_EXECUTION_ENABLED=true is loaded and bound
```

Risk to capital **today:** **NONE** (`SAFE_BY_ABSENCE` on the hosted copy hop). Residual: next sender would see **runtime armed**.

---

## 1. Claim 1 — DemoSeeder is not the API startup path — **PASS**

### 1.1 API host (read 160/160)

`D:\Prop\apps\api\Program.cs` startup after `app.Build()`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`. File ends at `app.Run();` (L159).

`/api/health` (L33–58) reports `runtime.Brokers` live Manager counts, not FakeMt5. `/api/ops/resync` (L114–149) walks `ACHIEVER` + `STARWAVEFX` via `ingestion.SyncCatalogAsync` + `store.ListLoginsAsync` — not `{10001,10002,10003,99001}`.

### 1.2 Workers

| File | Seed call |
|---|---|
| `D:\Prop\apps\mt5-worker\Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` |
| `D:\Prop\apps\fix-worker\Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` |

### 1.3 Grep

`DemoSeeder` under `D:\Prop\apps` = **0 hits**.

`DemoSeeder` still exists (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder`). Product callers: `tests/Integration/SeedingAndStoreTests.cs` + swarm `_tmp_*` harnesses. **Not API startup.**

A002 / A005 (“API still calls `DemoSeeder`”) are **stale**.

---

## 2. Claim 2 — Native can list all groups via `GroupRequestArray` or `GroupTotal` — **PASS**

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (read in full):

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

Primary enumerator is the request API with mask `"*"` (manager-ACL-visible groups). Fallback is the pump cache (`GroupTotal` + `GroupNext`) **only when the request list is empty**.

Live ingest uses that walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null)`.

**Scope honesty:** this slot proves the **code path**. It does **not** re-measure live 18 groups. A001 (“zero `GroupRequestArray` under `src`”) is **stale**. Completeness still depends on a successful Connect (pump or request). If both request and cache are empty, the method returns `[]` with no error.

---

## 3. Claim 3 — All traders via `UserRequestArray` / `UserLogins` — **PASS**

`GetAccountsCore(null)` walks **every group name** from `GetGroupsCore`, then `ReadAccountsForGroup`:

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

Order: **request** `UserRequestArray` → cache `UserGetByGroup` only on hard fail → if still empty, **request** `UserLogins` + `UserRequestByLogins`. Dedup by login in `GetAccountsCore`.

Catalog ingest: `GetAccountsAsync(null)` (`DealIngestionService.cs` L48 / L62). No plan-env group filter in this connector.

**Scope honesty:** capability proven. Live 8460 logins not re-attached this slot.

---

## 4. Claim 4 — `CTraderFixSession` has no `35=D` — **PASS**

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read **135/135**.

- Only public method: `TryLogonAsync`.
- Only wire write: `ssl.WriteAsync` of `BuildLogon` (L47–50).
- `BuildLogon` body starts `(35, "A")` (L96). Other outbound tags: 34, 49, 56, 50, 57, 52, 98, 108, 141, 553, 554.
- Sockets: `using var tcp` + `await using var ssl` — disposed after one read.
- Grep of **this file** for `35=D`, `(35, "D")`, `NewOrderSingle` = **0**.
- Inbound `Extract(reply, "35")` can observe a reject type; it never **sends** `D`.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs “NewOrderSingle still unimplemented.” Persist updates existing `FixSessionState` rows only (no insert; no order).

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139/163/197 and `CTraderFixDemoMatrix` L87. Those are **not** `CTraderFixSession`. Demo helper is host-gated (`demo-` / `demo.`; refuses `live-*` / `live.` / account `1369850`) and is not referenced by API/DI/copy. Product-wide “`35=D=0`” claims (W500_RESEARCH_28 §0) are **stale**.

---

## 5. Claim 5 — `REAL_COPY_EXECUTION` stays false — **FAIL**

This is the assigned fail. The **policy** (“must stay false until §68/§70”) is not the same as the **runtime**.

### 5.1 What the files actually do

| Surface | Measured | Stays false? |
|---|---|---|
| `D:\Prop\.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **No** |
| `EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L5–19) | Loads `D:\Prop\.env` into process env | Arms host |
| `apps/api/Program.cs` L10 + L13 | `FindAndLoad()` then `AddEnvironmentVariables()` | Binds into IConfiguration |
| `DependencyInjection.cs` L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **Binds env. No hard-false pin.** |
| `CTraderFixLogonHostedService.cs` | Reads `_runtime.RealCopyEnabled` at L70 only | **No re-pin** |
| `CopyTradingHostedService.cs` | Shadow tick only | Does not touch the flag |
| `apps/api/Program.cs` L76 | `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | Mirrors runtime (will be **true** when `.env` loaded) |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | POCO default `= false` | **Unused** by `LiveRuntimeStatus` |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Different key; log-only; default false |
| `apps/api/appsettings.json` | No `REAL_COPY_EXECUTION_ENABLED` key | Does not pin false |
| Product `RealCopyEnabled = false` assignment | **0 hits** under `src/` | No process pin |

The **only** assignment of `LiveRuntimeStatus.RealCopyEnabled` in product C# is DI L41.

`LiveRuntimeStatus.Snapshot()` L42–44 even documents the armed state: *“REAL_COPY armed. NewOrderSingle still unimplemented…”*.

### 5.2 Why W500_RESEARCH_28 is rejected on this claim

`W500_RESEARCH_28.md` §0 / §9 said:

- `LiveRuntimeStatus.RealCopyEnabled` pinned false in DI **and** after FIX logon
- `.env` L73 is false
- `FEATURE_COPY_TRADING_ENABLED = false`
- “C# / env / API / DI / post-logon pins **all false**”
- 0 production `Evaluate(` callers

Live files now:

| W500_28 claim | Live file |
|---|---|
| DI pins false | DI L41 **binds env** |
| post-logon re-pin | **absent** (hosted only logs `RealCopyArmed`) |
| `.env` L73 false | **`true`** |
| FEATURE_COPY false | API L77 literal **`true`** |
| 0 `Evaluate(` callers | `CopyTradingService.cs` L178 `_risk.Evaluate(...)` |
| CREDENTIALS “false (forced)” | **stale** vs current DI |

`reports/CREDENTIALS_AND_COPY_STATUS.md` L30 (`false (forced)`) is the same stale pin.

### 5.3 What still blocks a ticket (not claim 5)

These prove **no live send**, not **flag stays false**:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- persist `AllowFixSend = false` (L211) regardless of `Evaluate`
- send conjunction L217 also requires LIVE + implemented + reconciled
- hosted copy writes `SHADOW_ONLY` (`CopyTradingHostedService` L30)
- `CTraderFixSession` has no builder (claim 4)

If a sender is added tomorrow, `RealCopyEnabled` is already **true** on an API process that loaded `.env`. That is why claim 5 is FAIL.

---

## 6. Stale reports this slot contradicts

| Report | Stale claim | Live |
|---|---|---|
| `W500_RESEARCH_28.md` | DI + logon + `.env` all false | env true; DI binds; no re-pin |
| `CREDENTIALS_AND_COPY_STATUS.md` L30 | `REAL_COPY` false (forced) | not forced |
| A002 / A005 | API startup `DemoSeeder` | `BrokerCatalogSeed` |
| A001 | 0 `GroupRequestArray` / `UserRequestArray` in `src` | L155 / L223 |
| A014 L270 | `/api/settings` DI-pins false | settings = runtime (env-driven) |
| W500_28 “product `35=D` 0 hits” | whole product | siblings `Build("D")` exist; **session file** still 0 |

---

## 7. Risk to capital

| Question | Answer |
|---|---|
| Can hosted copy emit `35=D` today? | **No** (`SAFE_BY_ABSENCE`) |
| Does `REAL_COPY` stay false? | **No** (this FAIL) |
| Dest book if process starts with lab `.env` | Flag **armed**; sender **missing** |
| This slot send / flag flip / secret dump | **None** |

**risk_to_capital = NONE** for a live ticket from the copy hop. Residual landmine: runtime is armed for the next implemented sender.

---

## 8. Assigned answers (do not paraphrase away)

1. **Is DemoSeeder the API startup path?** **No.** `BrokerCatalogSeed.EnsureAsync` only. File remains for tests.
2. **Can the native connector list all groups via `GroupRequestArray` or `GroupTotal`?** **Yes.** `"*"` request first; `GroupTotal`/`GroupNext` fallback.
3. **All traders via `UserRequestArray` / `UserLogins`?** **Yes.** Request first; `UserLogins` if the user array is empty. Ingest `GetAccountsAsync(null)`.
4. **Does `CTraderFixSession` have `35=D`?** **No.** Only `(35, "A")`. Siblings with `Build("D")` are off-hop.
5. **Does `REAL_COPY_EXECUTION` stay false?** **No. FAIL.** `.env` L73 `true` is loaded and bound. No DI/logon pin.

**Do not treat this FAIL as a license to send.** Do **not** add `35=D` to `CTraderFixSession`. Flip `.env` back to `false` if the operator intent is still “flag stays false.”

*End of W500_VERIFY_28. Product source was not modified.*
