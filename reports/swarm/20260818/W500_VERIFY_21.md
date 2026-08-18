# W500_VERIFY_21 — Adversarial live-path verify (slot 21)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_21.md` |
| Agent / slot | W500 adversarial verifier **21** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product files (`apps/`, `src/`). Reports / other agents **not trusted**. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted **boolean keys only**. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 not dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Capability proven from source only. |
| Method | Independent `read_file` + targeted `grep` of live files. Fail any claim that cannot be proven from those files. |

**Honesty rule:** a prior swarm PASS is not evidence. A comment is not a choke. A POCO default is not a runtime pin. Absence of `35=D` on the copy hop is `SAFE_BY_ABSENCE`, not a go-live. Do **not** print secrets.

---

## 0. Verdict (binding)

**FAIL.**

Four of five assigned claims are proven from live files. Claim **(5)** is **disproven**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false.

| # | Claim | Result | Class |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | file-proven |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | **PASS** (code path) | file-proven; not live-attached |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (code path) | file-proven; not live-attached |
| 4 | `CTraderFixSession` has **no** `35=D` | **PASS** | file-proven (`SAFE_BY_ABSENCE`) |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | lab `.env` L73 `=true`; DI binds it; no re-pin |

One-line:

```text
DemoSeeder OFF API startup (BrokerCatalogSeed only). Native ALL-groups = GroupRequestArray("*") else GroupTotal+GroupNext. ALL-traders = UserRequestArray then UserLogins. CTraderFixSession outbound 35=A only. REAL_COPY does NOT stay false (.env true + DI bind). Slot FAIL. Risk NONE (SAFE_BY_ABSENCE).
```

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` on the copy hop). Flag may be **armed**; sender still missing.

---

## 1. DemoSeeder is not the API startup path — PASS

Live file: `D:\Prop\apps\api\Program.cs` (160 lines).

Startup seed is **only** `BrokerCatalogSeed.EnsureAsync` after `EnsureCreatedAsync`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Grep of `D:\Prop\apps\api\Program.cs` for `DemoSeeder`: **0 hits**.

Same seed on both workers (not DemoSeeder):

- `D:\Prop\apps\mt5-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\fix-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`

DI fail-closed Native only (`DependencyInjection.cs` L36–37 throws if both real MT5 passwords missing). `LiveMt5Registration.CreateConnectors` returns two `NativeMt5BrokerConnector` instances (Achiever + Starwave). No Fake on that path.

**Residual (does not revive claim 1):** `DemoSeeder` still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` and is called from `tests/Integration/SeedingAndStoreTests.cs` plus swarm `_tmp_*` eval programs. That is **test/eval**, not API startup. Stale reports that still say `Program.cs` calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005_dashboard_traders.md`) are **wrong against current files**.

`apps/mt5-worker/Worker.cs` L31 still scores hardcoded `{10001,10002,10003,99001}` after a real `SyncBrokerAsync`. That is a residual **scorer** leak, not a DemoSeeder startup path.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (code)

Live file: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

`GetGroupsCore` (called by `GetGroupsAsync`):

1. Primary: `_manager.GroupRequestArray("*", arr)` then walk `arr.Total()` / `arr.Next(i)` (L155–165).
2. Fallback **only if** `list.Count == 0`: `_manager.GroupTotal()` + `GroupNext(i, grp)` (L169–180).

```152:180:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Ingest uses that walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null, ct)` — no `Take(`/`Skip` on the catalog.

**Caveats (not enough to fail the claim):**

- “All” = **manager-visible** groups. No extra plan-name filter in this file.
- `GroupTotal` fallback is cache/pump (`PUMP_MODE_GROUPS` is requested on first Connect, L89–92). If request returns a **non-empty partial** set, fallback is skipped. File does not prove the request is always complete.
- This slot **did not live-attach**. Census 18/8460 from other reports is **not** re-proven here.

The assigned claim is capability via those two APIs. **Proven from the file.**

---

## 3. All traders via UserRequestArray / UserLogins — PASS (code)

Same connector. `GetAccountsAsync(null)` walks **every group** from `GetGroupsCore()`, then `ReadAccountsForGroup`:

1. Primary: `_manager.UserRequestArray(gname, users)` (L223).
2. On hard fail (not OK / OK_NONE / NOTFOUND): cache `UserGetByGroup` (L225).
3. If `users.Total() == 0`: `_manager.UserLogins(gname, out loginRes)` then `UserRequestByLogins` (L227–232).

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

`GetAccountsCore(null)` L201–203: `foreach (var g in GetGroupsCore()) groups.Add(g.Name)` then union by login.

**Caveats:**

- Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Catalog still upserts **all** accounts; score rows are a subset.
- `UserGetByGroup` is pump-cache; it is **not** the primary ALL path.
- This slot did not live-attach; trader counts are **not** re-measured.

The assigned claim (connector **can** list all traders via those APIs) is **proven from the file**.

---

## 4. CTraderFixSession has no 35=D — PASS

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep of that file for `35=D` / `(35, "D")`: **0 hits**. `NewOrderSingle`: **0 hits**.

Only outbound MsgType is Logon `"A"`. Single `WriteAsync` of that buffer. Socket disposed by `using`.

```46:50:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
            var seq = 1;
            var logon = BuildLogon(senderCompId, targetCompId, senderSubId, targetSubId, username, password, seq);
            var bytes = Encoding.ASCII.GetBytes(logon);
            await ssl.WriteAsync(bytes, timeoutCts.Token);
            await ssl.FlushAsync(timeoutCts.Token);
```

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

Tag 35 inbound is **read** (`Extract(reply, "35")`) to accept Logon or report reject. That is not an outbound NewOrderSingle.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.cs` has `Build("D")` at L139 / L163 / L197; `CTraderFixDemoMatrix.cs` has `Build("D")` at L87. Those are **not** `CTraderFixSession`. Copy hop const `CopyTradingService.NewOrderSingleImplemented = false` (L17) and persist `AllowFixSend = false` (L211). Hosted copy tick only calls `GenerateShadowIntentsAsync`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The claim is a **runtime pin**. It is **not** pinned.

### 5.1 What is actually false

| Surface | Value | Note |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | POCO **unbound** — no `Configure<CTraderFixOptions>` in DI |
| `LiveRuntimeStatus.RealCopyEnabled` field default | `false` | overwritten at DI |
| fix-worker `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback `false` | **different key**; log-only |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different name**; unused by DI |
| Architecture / README / CREDENTIALS docs | say `false` | docs, not runtime |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | sender missing — **not** the flag |

### 5.2 What is actually true / unbound

API startup loads `D:\Prop\.env` then environment variables:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes hard path `D:\Prop\.env` (L14).

Lab `.env` (boolean only):

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true`

DI **binds** the §41 token onto the runtime bit. **Only writer** of `RealCopyEnabled` in product C#:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`CTraderFixLogonHostedService` **does not** assign `RealCopyEnabled = false`. It only logs `RealCopyArmed={Armed}` (L68–70). Prior “hosted re-pin false” reports are **stale**.

API `/api/settings` **exposes** the bound bit (not a hardcoded false):

```74:77:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

Therefore: if the API process starts with the current `.env`, `runtime.RealCopyEnabled` is **true**. Claim “stays false” **fails**.

`CopyTradingService.BuildBlockers` only adds `"REAL_COPY_EXECUTION_ENABLED is false"` when the bit is already false (L316–317). When env is `true`, that blocker is **absent**. Send is still blocked by `NewOrderSingleImplemented=false` and `VenueReconciled=false`.

### 5.3 Why this is FAIL, not “must stay false”

Policy (“must stay false until §68/§70”) is a **wish**. The assigned verify was the **state**. State is: flag is **armed** on the API host bind path. W500_68 / W500_108 / `CREDENTIALS_AND_COPY_STATUS.md` “forced false” are **stale**.

This slot did **not** flip `.env` back to `false`.

---

## 6. Cross-claim residuals

| Residual | Why it matters |
|---|---|
| `.env` L73 `true` + DI bind | Next copy sender would see an armed flag |
| `CTraderFixDemoTestTrade.Build("D")` | Off-hop demo helper; not `CTraderFixSession`; do not wire it |
| mt5-worker four-login scorer | Dummy logins still scored after live sync |
| Hosted score = deals-only | Catalog lists all; scores subset |
| `GroupTotal` fallback skipped if request returns any rows | Completeness of a partial request not proven |
| No live attach | Census 18/8460 **not** re-proven this slot |
| `FEATURE_COPY_TRADING_ENABLED` API literal `true` | Shadow pipeline on; unused env key L106 |

---

## 7. Risk to capital

**NONE** from the copy hop of this process.

- `CTraderFixSession` cannot emit `35=D` (`SAFE_BY_ABSENCE`).
- `CopyTradingService` hard-codes `NewOrderSingleImplemented=false`, `VenueReconciled=false`, persist `AllowFixSend=false`.
- Hosted copy writes **SHADOW** intents only.
- Catalog/user walks are Manager **read** APIs.

Armed `REAL_COPY` is **not** a ticket. It **is** a failed pin. Do not add a sender while L73 is `true`.

---

## 8. Files read (this slot)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` (header)
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (header)
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\.env` flag **names/booleans only** (L73, L106)

No product source edited.
