# W500_VERIFY_17 — Adversarial live-path verify (slot 17)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_17.md` |
| Agent / slot | W500 **VERIFY 17** (adversarial; do not trust sibling agents) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product paths under `apps/`, `src/` |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No.** Quoted **boolean only** (`REAL_COPY_EXECUTION_ENABLED=true` at L73). No MT5 / FIX / proxy / DB secrets. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. |
| Method | Independent `read_file` of API + worker `Program.cs`, `NativeMt5BrokerConnector.cs` (full), `CTraderFixSession.cs` (**135/135**), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `LiveMt5Registration.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` header, `EnvFile.cs`, `RiskEngine.cs` allow-send, `CTraderFixDemoTestTrade.cs` gate + `Build("D")`, `apps/api/appsettings.json`, `.env` L70–73 only. Targeted `grep` of `apps/` + `src/**/*.cs` for `DemoSeeder`, `GroupRequestArray`, `GroupTotal`, `UserRequestArray`, `UserLogins`, `35=D`, `(35, "D")`, `REAL_COPY_EXECUTION`, `RealCopyEnabled =`. |
| Binding rule | **FAIL if any assigned claim cannot be proven from the live file.** Prior swarm text is not evidence. |

**Honesty:** capability in source ≠ a live census. A Logon `35=A` is not a fill. An armed flag is not a ticket. A demo helper `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1–4** are proven from the live files. Claim **5** (`REAL_COPY_EXECUTION stays false`) is **disproven** on the API live path: lab `.env` L73 is `true`, API loads that file, DI copies it onto `LiveRuntimeStatus.RealCopyEnabled`, and no later writer re-pins false.

| # | Claim | Result | Class |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | proven |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** | capability proven; not re-attached |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** | capability proven; not re-attached |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | 135/135; outbound MsgType `"A"` only |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | live API **arms** the bit |

One-line:

```text
FAIL. DemoSeeder off API boot (BrokerCatalogSeed only). Native GetGroupsCore = GroupRequestArray("*") else GroupTotal/GroupNext. Traders = UserRequestArray then UserLogins. CTraderFixSession 35=A only. REAL_COPY does NOT stay false: .env L73=true + EnvFile + DI L41. Copy hop still SAFE_BY_ABSENCE (no 35=D builder; NOS const false; persist AllowFixSend=false).
```

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` on the copy hop). Armed flag cannot emit a Pepperstone ticket from this process.

---

## 1. DemoSeeder is not the API startup path — PASS

API boot seed is **only** `BrokerCatalogSeed.EnsureAsync`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`grep DemoSeeder` over `D:\Prop\apps` = **0**. `grep DemoSeeder` over `D:\Prop\apps\api\Program.cs` = **0**. The `using TraderIntelligence.Infrastructure.Seeding;` at API L6 exists for `BrokerCatalogSeed`, not `DemoSeeder`.

Same seed on both workers (`D:\Prop\apps\fix-worker\Program.cs` L15, `D:\Prop\apps\mt5-worker\Program.cs` L15): `BrokerCatalogSeed.EnsureAsync` only.

**Residual (not API startup):** `DemoSeeder` class still exists (`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 `public static class DemoSeeder`). Callers are tests (`tests/Integration/SeedingAndStoreTests.cs` L25) and throwaway `_tmp_*` trees. DI fail-closes Fake: `LiveMt5Registration.HasRealPasswords` must pass or `AddTraderIntelligence` throws before `CreateConnectors`; connectors are Native ×2 only.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

`GetGroupsCore` is request-first on mask `*`, then cache walk if the request list is empty:

```144:185:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Live ingest uses that walk with **no** plan-group filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` L56 calls `ingest.SyncCatalogAsync`. `_pumpEnabled` never gates fetch.

**Limits (not a FAIL of “can”):** this slot did **not** attach. `GroupTotal`/`GroupNext` is cache-only (empty if pump-none and request also empty). Completeness of a live ACL is **not** proven here. Sibling census 18/8460 is **not** re-verified.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

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

Order: **`UserRequestArray` first** → cache `UserGetByGroup` only on hard fail → empty array → **`UserLogins` + `UserRequestByLogins`**.

Live catalog: `GetAccountsAsync(null)` (`DealIngestionService` L48, L62). Probe tool same (`tools/LiveBrokerProbe/Program.cs` L26).

**Residual (not a FAIL of the connector walk):** hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106), so score rows can be a subset of catalog. `mt5-worker/Worker.cs` L31 still scores `{10001,10002,10003,99001}` — dummy leftover on that worker loop, **not** the Native enumerator. This slot did **not** live-count traders.

---

## 4. CTraderFixSession has no 35=D — PASS

Assigned type `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read **135/135**. Only outbound MsgType is Logon `"A"`. One `WriteAsync`. Sockets `using`-disposed.

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

Hits in this file: `NewOrderSingle` = **0**, `(35, "D")` = **0**, literal `35=D` = **0**. Inbound `Extract(..., "35")` only classifies the reply. Hosted caller is `CTraderFixLogonHostedService` L48/L54 (`TryLogonAsync` only).

`grep` of `D:\Prop\src/**/*.cs` for literal `35=D` / `(35, "D")` = **0**.

**Residual (not this type):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Demo-gated (`host` must start `demo-`; sender must start `demo.`; live host / `live.` / account `1369850` refused at L43–47). Only product caller is `tools/DemoFixTestTrade/Program.cs` — **not** DI, **not** API, **not** copy hop.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the **flag stays false**. The live API path **does not**.

Proven chain:

1. Lab `.env` L73 (boolean only): `REAL_COPY_EXECUTION_ENABLED=true`.
2. API `Program.cs` L10 `EnvFile.FindAndLoad()` then L13 `AddEnvironmentVariables()`. `EnvFile` includes `D:\Prop\.env` as a candidate (`EnvFile.cs` L14).
3. DI **binds** the env token onto process state (only `RealCopyEnabled =` assignment in `src/`):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

4. Hosted logon **does not** re-pin false. It logs `RealCopyArmed={Armed}` (`CTraderFixLogonHostedService.cs` L68–70).
5. `/api/settings` exposes `featureFlags["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` (`apps/api/Program.cs` L76). After a normal API start this display is **true**.
6. `CopyTradingService` passes `RealExecutionEnabled = _runtime.RealCopyEnabled` into `RiskEngine.Evaluate` (L190). `RiskEngine` L147–150 can set `AllowFixSend` from that bit **if** kill-switch none + reconciled + venue healthy.

What **is** still false (does **not** rescue claim 5):

| Surface | Value | Why it is not “flag stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | **unbound** (no `Configure<>`); unused by logon/copy |
| Architecture / README / `docs/*` | `=false` | policy text, not process |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different name**; unread by DI |
| fix-worker `GetValue("CTrader:RealCopyExecutionEnabled", false)` | default false | nested key; log-only; worker does not load `.env` |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` (L17) | send absent; flag still armed |
| persist `AllowFixSend` | hardcoded `false` (L211) | send absent; flag still armed |
| `VenueReconciled` | `const false` (L16) | send absent; flag still armed |

W500_68 / W500_108 / `CREDENTIALS_AND_COPY_STATUS.md` “forced false” / A014 “DI pins false” are **STALE**.

**Cannot prove claim 5 from the file.** The live files prove the opposite. Verdict on this claim is **FAIL**. That fails the slot.

Copy hop still cannot send: `CTraderFixSession` has no `35=D`; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; 0 `ExecutionIntent` writers; `CopyTradingHostedService` only calls `GenerateShadowIntentsAsync`. That is **`SAFE_BY_ABSENCE`**, not “REAL_COPY stays false.”

---

## 6. What this slot did not do

- No Manager attach. No group/trader recount. Prior 8/6512 + 10/1948 = 18/8460 is **unverified here**.
- No TLS Logon. No `/api/settings` HTTP re-probe.
- No product edit. `.env` left as found (`true` leftover remains).
- Did not invoke `tools/DemoFixTestTrade`.

---

## 7. Risk to capital

**NONE** on the hosted copy hop (`SAFE_BY_ABSENCE`).

`CTraderFixSession` cannot build NewOrderSingle. Copy persist forces `AllowFixSend=false`. NOS const is false. Catalog APIs are GET-only Manager walks.

**Residual risk if a sender is added later:** API process will already see `RealCopyEnabled=true`, and `RiskEngine.allowSend` will honor that bit once `Reconciled` and `VenueHealthy` become true. Operator leftover + missing re-pin is why claim 5 fails **now**.
