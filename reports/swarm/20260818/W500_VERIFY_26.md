# W500_VERIFY_26 — Adversarial live-path verifier (slot 26)

| Field | Value |
|---|---|
| Slot | **26** |
| Date | 2026-08-18 |
| Role | Adversarial verifier. Read live-path files. Do not trust other agents. |
| Product source | **Not modified.** Report only. |
| Live attach / Manager probe this slot | **No** |
| Secrets printed | **None** (boolean flags and public identifiers only) |

**Rule used:** FAIL the slot if any assigned claim cannot be proven from the file, or if a file disproves it.

---

## Overall verdict: **FAIL**

Claims 1 and 4 are file-proven. Claims 2 and 3 are file-proven as **connector request paths** (not as a re-measured live census). Claim 5 is **disproven**: `REAL_COPY_EXECUTION` does **not** stay false.

| # | Claim | Verdict | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | API `Program.cs` 159/159: 0 `DemoSeeder` / `SeedAsync` tokens. Startup seed is `BrokerCatalogSeed.EnsureAsync` only. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_CODE** | `GetGroupsCore` L155 `GroupRequestArray("*")`; L174 `GroupTotal()` only if that list is empty. Live ALL not re-probed this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_CODE** | `GetAccountsAsync(null)` walks every group; `ReadAccountsForGroup` L223 `UserRequestArray`, L230 `UserLogins` if `users.Total()==0`. Completeness hole if request returns a non-empty subset. Live ALL not re-probed. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: outbound MsgType is only `(35, "A")`; `WriteAsync=1`; 0 `NewOrderSingle` / `35=D`. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env exact `"true"`. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API `EnvFile.FindAndLoad()` loads it. Logon host does **not** re-pin false. |

**Bottom line:** dummy seed is off the API host; native request APIs are wired; hosted FIX session still cannot emit NewOrderSingle. The assigned “flag stays false” claim is **false**. Older reports that quote `RealCopyEnabled = false` in DI / logon overwrite (`A014`, `A015`, `W500_68`, `CREDENTIALS_AND_COPY_STATUS` “forced”) are **stale**.

Risk to capital **today:** **NONE** (`SAFE_BY_ABSENCE` of a product `35=D` sender). Flag-armed ≠ ticket.

---

## 1. DemoSeeder is not the API startup path — **PASS**

Read: `D:\Prop\apps\api\Program.cs` (159 lines), `D:\Prop\apps\mt5-worker\Program.cs`, `D:\Prop\apps\fix-worker\Program.cs`, `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`, `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`.

Grep `DemoSeeder` under `D:\Prop\apps`: **0 hits**.

Startup after maps:

```152:157:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` exists only for `BrokerCatalogSeed`. There is no `DemoSeeder.SeedAsync`.

Workers match: both call `BrokerCatalogSeed.EnsureAsync` and never `DemoSeeder`.

`DemoSeeder.cs` **still exists** and still composes `DemoBrokerFactory.CreateDefault()` + scores `{10001, 10002, 10003, 99001}`. Integration tests still call it (`tests/Integration/SeedingAndStoreTests.cs`). That is **not** API startup.

Residual (does not fail claim 1): `apps/mt5-worker/Worker.cs` L31 still scores the four demo logins after a real `SyncBrokerAsync`. Hosted API ingest (`LiveIngestHostedService`) scores `ListLoginsWithDealsAsync`, not those four.

---

## 2. Native connector groups via GroupRequestArray or GroupTotal — **PASS_CODE**

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). Live ingest: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`.

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

**Proven from the file:** the connector **requests** every group (`"*"`) and **falls back** to `GroupTotal`/`GroupNext` when the request list is empty.

**Not proven from the file (residual, not a live census):**

- If `GroupRequestArray` returns OK/OK_NONE with a **partial non-empty** array, `GroupTotal` never runs.
- This slot did **not** re-attach Manager. Prior 18/8460 figures are **not** re-used as proof.

---

## 3. All traders via UserRequestArray / UserLogins — **PASS_CODE**

Ingest calls `GetAccountsAsync(null)`:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`null` group walks **every** name from `GetGroupsCore()`:

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var groups = new List<string>();
            if (!string.IsNullOrWhiteSpace(group))
            {
                groups.Add(group);
            }
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            // ... ReadAccountsForGroup(gname) per group, de-dupe by login
```

Per group:

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

**Proven:** intended ALL-traders walk is `UserRequestArray` first, `UserLogins` + `UserRequestByLogins` if the array is empty.

**Not proven / residual:**

- `UserLogins` is **empty-set only**. A non-empty incomplete `UserRequestArray` skips it.
- `UserGetByGroup` is pump-cache fallback on hard fail (not the primary ALL path).
- Hosted **scoring** is `ListLoginsWithDealsAsync`, not every catalog login. Claim 3 is about the **connector list**, not scoring coverage.
- Live ALL trader count not re-probed this slot.

---

## 4. CTraderFixSession has no 35=D — **PASS**

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines). Grep on that file for `35=D` / `NewOrderSingle`: **0**.

Only outbound builder:

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

One `ssl.WriteAsync` of that Logon. Sockets disposed (`using` TcpClient / SslStream). Reply `35=A` → LoggedOn; anything else → Error. No order fields.

Hosted caller `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and persists session status. It does **not** send D.

**Residual (not this type, not DI/API/copy hop):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 and `CTraderFixDemoMatrix.Build("D")` at L93. Wired only from `tools/DemoFixTestTrade`. Demo-gated (refuses `live-*` / `live.` / account `1369850`). Copy hop grep for `CTraderFixDemoTestTrade` under `apps/` and `Infrastructure/`: **0**. Product `ExecutionIntent` writers: **0**. `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`.

---

## 5. REAL_COPY_EXECUTION stays false — **FAIL**

This is the claim that cannot be proven and is in fact **false** on the live API path.

### 5.1 DI no longer pins false

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

No hard `= false`. Exact ordinal-ignore-case `"true"` arms the bit.

### 5.2 API loads lab `.env` before DI

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` always also tries `D:\Prop\.env` and `SetEnvironmentVariable`s every `KEY=value`.

### 5.3 Lab file is armed

`D:\Prop\.env` L73: `REAL_COPY_EXECUTION_ENABLED=true`  
(L106 is `FEATURE_COPY_TRADING_ENABLED=true` — different key. Neighboring secret values not quoted.)

Committed `apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. `FeatureFlags.LiveCopyEnabled` there is a **different unbound** name (`false`). Settings HTTP surface uses the runtime bit:

```71:77:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

### 5.4 Logon does not re-pin

`CTraderFixLogonHostedService` L68–70 **reads** `_runtime.RealCopyEnabled` and logs `RealCopyArmed={Armed}`. There is no `_runtime.RealCopyEnabled = false`.

`CTraderFixOptions.RealCopyExecutionEnabled` still defaults `false`, but **no product binder** copies the env token onto that POCO. The live process bit is `LiveRuntimeStatus.RealCopyEnabled`.

`CopyTradingService.BuildBlockers` will **omit** the “REAL_COPY_EXECUTION_ENABLED is false” blocker when the runtime bit is true. Send is still blocked by `NewOrderSingleImplemented=false` and `VenueReconciled=false`.

### 5.5 Stale “forced false” citations

Do not trust: `A014` / `A015` quoting DI `RealCopyEnabled = false` and logon overwrite; `reports/CREDENTIALS_AND_COPY_STATUS.md` “false (forced)”; early W500 slots that treated the pin as current.

POCO default and architecture text still **say** false. The **running API** that loads this `.env` will advertise `realCopyEnabled=true`. That is an arm, not a sender — and it is enough to **FAIL** “stays false.”

Workers do **not** call `EnvFile.FindAndLoad()`. `fix-worker` reads nested `CTrader:RealCopyExecutionEnabled` (default false) for a log line only and still stamps sessions `Disconnected`. That does not rescue claim 5 on the API host.

---

## Safety / capital (honest, not a PASS of claim 5)

| Gate | Measured this slot |
|---|---|
| Product `CTraderFixSession` outbound | `35=A` only |
| Copy `NewOrderSingleImplemented` | `const false` |
| Persist `AllowFixSend` | hardcoded `false` (`CopyTradingService` L211) |
| `VenueReconciled` | `const false` (so `RiskEngine.allowSend` cannot be true) |
| `ExecutionIntent` writers | 0 |
| Demo `Build("D")` | tools-only + demo-gated; not on API/copy hop |

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`). If a sender is later added while `.env` stays `true` and DI stays bound, the next hop would see the flag **already armed**. Do not treat this FAIL as a live fill.

This slot did not live-attach Manager or open a FIX socket.

---

## Files read (primary)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\TraderIntelligence.Api.csproj`
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (residual only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs`
- `D:\Prop\.env` (key names + booleans only)

## Method

Independent `read_file` + `grep` on live paths. Prior swarm prose treated as **hostile**. No product edit. No secret values printed.
