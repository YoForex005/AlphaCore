# W500_VERIFY_18 — Adversarial live-path verifier (slot 18)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_18.md` |
| Agent / slot | W500 **VERIFY 18** (adversarial; do not trust other agents) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product files under `src/`, `apps/`, lab `.env` (boolean keys only) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `BrokerCatalogSeed.cs`, both worker `Program.cs`, `EnvFile.cs`, `LiveMt5Registration.cs`, `RiskEngine.cs` allow-send clause, `CTraderFixDemoTestTrade.cs` (residual only). Targeted `grep` of `D:\Prop\apps` for `DemoSeeder`; of `Fix.CTrader/Sessions` for `35=D` / `(35, "D")` / `NewOrderSingle`; of product `*.cs`/`*.json`/`*.env` for `REAL_COPY_EXECUTION_ENABLED`. |
| Binding rule | **FAIL if any assigned claim cannot be proven from the live file.** Other swarm notes are untrusted. |

**Honesty:** a POCO default is not a runtime pin. A settings dictionary is a display. `SAFE_BY_ABSENCE` of a `35=D` builder is not “the flag stays false.” Fetch-all is read-only and is not a send license. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.**

| # | Claim | Result | Why |
|---|---|---|---|
| 1 | DemoSeeder is **not** the API startup path | **PASS** | Proven from `apps/api/Program.cs` |
| 2 | Native connector can list **all groups** via `GroupRequestArray` **or** `GroupTotal` | **PASS** | Proven from `NativeMt5BrokerConnector.GetGroupsCore` |
| 3 | Native connector can list **all traders** via `UserRequestArray` / `UserLogins` | **PASS** (residual) | Proven walk exists; `UserLogins` is empty-array fallback only |
| 4 | `CTraderFixSession` has **no** `35=D` | **PASS** | Proven from 135/135 read + session grep |
| 5 | `REAL_COPY_EXECUTION` **stays false** | **FAIL** | **Disproven.** Lab `.env` L73 is `true`; DI **binds** it onto `LiveRuntimeStatus.RealCopyEnabled`; hosted logon does **not** re-pin false |

One failed claim forces the slot verdict to **FAIL**. Claims 1–4 are independently proven. Claim 5 cannot be proven and is the opposite of the live bind.

One-line:

```text
Slot 18 FAIL. DemoSeeder off API boot (BrokerCatalogSeed only). Native GroupRequestArray("*")/GroupTotal + UserRequestArray/UserLogins walks exist. CTraderFixSession outbound 35=A only. REAL_COPY does NOT stay false: .env L73=true and DependencyInjection L41 binds it. Copy hop still SAFE_BY_ABSENCE (no 35=D builder). Risk to capital NONE.
```

---

## 1. DemoSeeder is not the API startup path — **PASS**

Live file: `D:\Prop\apps\api\Program.cs` (160 physical lines, full read).

Startup seed after `EnsureCreatedAsync` is **only** `BrokerCatalogSeed.EnsureAsync`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- Zero `DemoSeeder` tokens in this file.
- `using TraderIntelligence.Infrastructure.Seeding;` exists solely so `BrokerCatalogSeed` resolves.
- `grep DemoSeeder` over `D:\Prop\apps` = **0** (API + `mt5-worker` + `fix-worker`).
- Worker hosts (`D:\Prop\apps\mt5-worker\Program.cs` L15, `D:\Prop\apps\fix-worker\Program.cs` L15) also call `BrokerCatalogSeed.EnsureAsync` only.

Residual (does **not** fail the claim): `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists and tests may call `DemoSeeder.SeedAsync`. That is **not** the API process startup path. Older notes that say `Program.cs` still seeds FakeMt5 / 10001 (`A002_api_dummy_path.md`) are **stale** vs this file.

---

## 2. Native connector lists all groups via `GroupRequestArray` or `GroupTotal` — **PASS**

Live file: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458).

`GetGroupsAsync` → `GetGroupsCore`:

1. Primary: `GroupRequestArray("*", arr)` then `arr.Total()` / `arr.Next(i)`.
2. Fallback **only if** the request list is empty: `GroupTotal()` + `GroupNext(i, grp)`.

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

Ingest live path uses this: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null)`.

Residual: “all” = manager-visible groups for mask `"*"`. This slot did **not** re-attach, so live census counts are **not** re-proven here.

---

## 3. Native connector lists all traders via `UserRequestArray` / `UserLogins` — **PASS** (residual)

Same file, `GetAccountsCore` + `ReadAccountsForGroup`.

- `GetAccountsAsync(null)` walks **every** group from `GetGroupsCore()` (L199–203).
- Per group, primary is `UserRequestArray(gname, users)` (L223).
- Hard-fail only (`not` OK / OK_NONE / NOTFOUND) falls back to pump-cache `UserGetByGroup` (L224–225).
- If `users.Total() == 0`, `UserLogins(gname, out loginRes)` then `UserRequestByLogins` (L227–232).

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

Capability claim is proven: the connector **can** enumerate traders with those request APIs, and the ingest catalog (`GetAccountsAsync(null)`) asks for every group.

Residual (not a FAIL): `UserLogins` runs **only** when the user array is empty. A silent partial `UserRequestArray` would **not** be completed by `UserLogins`. Completeness of a live broker dump is **not** proven in this slot (no attach).

---

## 4. `CTraderFixSession` has no `35=D` — **PASS**

Live file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

- Only outbound MsgType builder is `BuildLogon` field `(35, "A")` at L96.
- One `WriteAsync` (L49), one `ReadAsync` (L53), then `using` disposes `TcpClient` / `SslStream`.
- `grep` of this file and of `Fix.CTrader/Sessions` for `35=D`, `(35, "D")`, `NewOrderSingle` against **`CTraderFixSession.cs`**: **0**.

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

Hosted caller `CTraderFixLogonHostedService` only invokes `TryLogonAsync` (QUOTE 5211, TRADE 5212). No order send.

Residual (does **not** fail **this** claim): sibling `CTraderFixDemoTestTrade.Build("D")` exists at L139 / L163 / L197. It is **not** `CTraderFixSession`. Only caller found: `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Demo-gated (refuses `live-*` / `live.` / account `1369850`). **Not** wired into API / workers / `CopyTradingService`.

---

## 5. `REAL_COPY_EXECUTION` stays false — **FAIL**

The assigned claim is that the flag **stays false**. Live files prove it does **not**.

### 5.1 Runtime bind (product)

`D:\Prop\src\Infrastructure\DependencyInjection.cs` L39–42:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`LiveRuntimeStatus.RealCopyEnabled` is a public **settable** bool (default `false` only until this assignment). There is no later write that forces it back to false.

### 5.2 Lab env (boolean only; no secrets)

`D:\Prop\.env`:

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true` (different key; API ignores this token)

API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. `EnvFile` includes hardcoded candidate `D:\Prop\.env`. On a normal API start the process **will arm** `RealCopyEnabled=true`.

### 5.3 No re-pin

`CTraderFixLogonHostedService` L68–70 logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled`. It does **not** assign `false`. Notes that claim hosted logon pins the flag false are **stale**.

### 5.4 Surfaces that follow the armed bit

| Surface | File | Behavior |
|---|---|---|
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | `apps/api/Program.cs` L76 | `= runtime.RealCopyEnabled` (follows env) |
| `/api/health` `realCopyEnabled` | same file L55 | same |
| `CopyTradingService.GetStatusAsync` `RealCopyArmed` | L44 | same |
| `RiskEngine.Evaluate` `RealExecutionEnabled` | `CopyTradingService` L190 | same |
| `CTraderFixOptions.RealCopyExecutionEnabled` | L35 `= false` | POCO default **only**; **not** `Configure<>`’d from the env token |
| `apps/fix-worker/Worker.cs` | L21 `GetValue("CTrader:RealCopyExecutionEnabled", false)` | nested key; log-only; **not** the §41 token |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different unused name** |
| Architecture / README / `docs/*` | default `false` | policy text, not a process pin |

`CREDENTIALS_AND_COPY_STATUS.md` “forced false” and older “DI pins false” reports are **stale** vs `DependencyInjection.cs` L41.

### 5.5 What this FAIL is **not**

This is **not** a live-send proof. Copy hop still cannot emit `35=D`:

- `CopyTradingService.NewOrderSingleImplemented = false` (const L17)
- `VenueReconciled = false` (const L16)
- persist always `AllowFixSend = false` (L211)
- `CTraderFixSession` has no NewOrderSingle builder (claim 4)
- zero `ExecutionIntent` writers found on the copy hop

`RiskEngine` *would* compute `AllowFixSend=true` if `RealExecutionEnabled && Reconciled && VenueHealthy` (L147–150), but the hosted copy path passes `Reconciled = VenueReconciled` (**false**) and then **overwrites** persist to `AllowFixSend = false`. Safety today is **`SAFE_BY_ABSENCE` + persist force-off**, not “flag stays false.”

Operator leftover: lab `.env` L73 is already `true`. This slot did **not** edit it.

---

## 6. Risk to capital

**NONE** from the current copy hop (`SAFE_BY_ABSENCE`).

Armed `REAL_COPY` cannot open a Pepperstone/cTrader ticket until someone adds a hosted `35=D` sender. That is the residual risk: the next sender would see `LiveRuntimeStatus.RealCopyEnabled=true` on an API host that loaded `D:\Prop\.env`.

---

## 7. Files read (this slot)

| Path | Role |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup; settings flag bind |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed = catalog |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed = catalog |
| `D:\Prop\apps\fix-worker\Worker.cs` | nested REAL_COPY log-only |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/user request walks |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | loads `D:\Prop\.env` |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | env bind |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | API/worker seed |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | SHADOW + persist `AllowFixSend=false` |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | shadow tick |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2 only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | residual demo `Build("D")` |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | no re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Domain\Risk\RiskEngine.cs` | `allowSend` conjunction |
| `D:\Prop\apps\api\appsettings.json` | unused `LiveCopyEnabled` |
| `D:\Prop\.env` L73 / L106 | boolean flags only |

---

## 8. Do / do not

- **Do** treat claim 5 as FAIL until `.env` is `false` **and** DI stops honoring `true`, **or** hosted code re-pins `RealCopyEnabled=false` (this slot did not change code).
- **Do not** treat POCO default / README / CREDENTIALS “forced false” as current runtime.
- **Do not** add a copy-path `35=D` builder.
- **Do not** wire `CTraderFixDemoTestTrade` into API/workers.
- **Do not** print secrets.
