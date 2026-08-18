# W500_VERIFY_66 — Adversarial live-path verify (slot 66)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | 66 |
| Role | Adversarial verifier (read live files; do not trust other agents) |
| Product source modified | **No** |
| Live Manager attach this slot | **No** (not re-probed) |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans) |
| Verdict | **FAIL** |

## Assigned claims (AND)

Confirm from live path files:

1. `DemoSeeder` is **not** the API startup path
2. Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`
3. All traders via `UserRequestArray` / `UserLogins`
4. `CTraderFixSession` has **no** `35=D`
5. `REAL_COPY_EXECUTION` **stays false**

Rule: **FAIL if any claim cannot be proven from the file.** Prior swarm notes are not evidence. This slot re-read the product files listed below.

## Scoreboard

| # | Claim | Proven from file? | Result |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | Yes | **PASS** |
| 2 | Native can list all groups via `GroupRequestArray` or `GroupTotal` | Yes (capability; census not re-attached) | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | Yes (capability; census not re-attached) | **PASS** |
| 4 | `CTraderFixSession` has no `35=D` | Yes (135/135) | **PASS** |
| 5 | `REAL_COPY_EXECUTION` stays false | **No — proven opposite** | **FAIL** |

**AND of five = FAIL.** Claims 1–4 hold on current source. Claim 5 is false on the running API composition: lab `.env` L73 is `true` and DI binds it onto `LiveRuntimeStatus.RealCopyEnabled` with no hosted re-pin.

Risk to capital remains **NONE** (`SAFE_BY_ABSENCE`): the product hop still cannot emit a ticket.

---

## Files read this slot (primary evidence)

| Path | Why |
|---|---|
| `D:\Prop\apps\api\Program.cs` | API startup + `/api/settings` flag echo |
| `D:\Prop\apps\fix-worker\Program.cs` | worker seed path |
| `D:\Prop\apps\mt5-worker\Program.cs` | worker seed path |
| `D:\Prop\apps\fix-worker\Worker.cs` | unused nested `CTrader:RealCopyExecutionEnabled` log |
| `D:\Prop\apps\mt5-worker\Worker.cs` | leftover dummy login scorer (not API seed) |
| `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs` | actual host seeder |
| `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` | class still exists; not called from hosts |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | REAL_COPY bind + Native-only DI |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Native ×2; no Fake |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | group/trader request APIs |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | catalog `GetGroupsAsync` + `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | hosted catalog walk |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | entire 135-line compilation unit |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | logon; no RealCopy re-pin |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default still false (unread by host) |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` | sibling `Build("D")` — **not** the assigned class |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` field |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | NOS const + persist `AllowFixSend=false` |
| `D:\Prop\src\Mt5\Env\EnvFile.cs` | `.env` loader used by API |
| `D:\Prop\.env` L73 | boolean flag only |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | vendor `GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins` |

Grep (not trusted as proof; used only to locate then re-read): `DemoSeeder` under `D:\Prop\apps` = **0**; `35=D` / `Build("D")` under product `*.cs` is only the demo helper siblings; `RealCopyEnabled =` assignment is **only** DI L41.

---

## 1. DemoSeeder is not the API startup path — PASS

API boot seed is `BrokerCatalogSeed` only. There is no `DemoSeeder` token in `Program.cs`.

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`using TraderIntelligence.Infrastructure.Seeding;` at L7 exists solely for `BrokerCatalogSeed`.

Grep of `D:\Prop\apps` for `DemoSeeder`: **0 hits**.

Both workers seed the same way:

- `D:\Prop\apps\fix-worker\Program.cs` L11–16 — `BrokerCatalogSeed.EnsureAsync` only
- `D:\Prop\apps\mt5-worker\Program.cs` L11–16 — same

DI fail-closes Fake/dummy before connectors exist, then registers Native only:

```36:49:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
        services.AddScoped<CopyTradingService>();
        services.AddSingleton<TraderIntelligence.Domain.Risk.RiskEngine>();

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` constructs **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). No `FakeMt5BrokerConnector` on that path (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49).

**Residual (does not revive DemoSeeder as API startup):**

- Class still exists: `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 (`public static class DemoSeeder`).
- Product caller of `DemoSeeder.SeedAsync` is **tests** (`D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25) plus report-scratch `_tmp_*` programs. Those are not `apps/api`.
- `mt5-worker\Worker.cs` L31 still scores `{10001,10002,10003,99001}` in its own loop. That is a leftover dummy login set on the **worker**, not API seed. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106).

Stale reports that still say API calls `DemoSeeder` (`A002_api_dummy_path.md`, `A005`, `A011`) are **superseded** by current `Program.cs`.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–186.

Vendor surface (`CIMTManagerAPI`):

- `GroupTotal` — `MT5APIManager.h` L205
- `GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` — L212

Primary walk is the network request with mask `*`:

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

Hosted ingest uses this path flag-blind: `DealIngestionService.SyncCatalogAsync` L45–48 calls `GetGroupsAsync` then `GetAccountsAsync(null)`. `LiveIngestHostedService` L56 calls `SyncCatalogAsync`.

**Adversarial limits (do not over-claim):**

- This slot did **not** live-attach. Completeness of a live Achiever/Starwave census is **unproven here**.
- `GroupTotal`/`GroupNext` is pump-cache. After `Connect(..., PUMP_MODE_NONE)` fallback (L101–110) that cache can be empty. Completeness without pump is the `GroupRequestArray("*")` first hop. If that RPC fails (`res` not OK/OK_NONE) **and** cache is empty, the connector returns an empty list with no throw.
- “All groups” means manager-ACL-visible groups, not every group on the trade server.

Capability claimed in the assignment is **present in the file**. Live completeness is not re-proven this slot.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Read: `ReadAccountsForGroup` L216–271 and `GetAccountsCore` L189–214.

Vendor surface:

- `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` — `MT5APIManager.h` L254
- `UserRequestArray(LPCWSTR group, IMTUserArray* users)` — L410

C# walk (per group, then union by login):

```223:232:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`GetAccountsCore(null)` walks **every** group from `GetGroupsCore()` then `ReadAccountsForGroup`. Ingest uses that null-group path (`DealIngestionService` L48).

**Adversarial limits:**

- `UserGetByGroup` is pump-cache fallback **only** on hard fail of `UserRequestArray`. Empty request result skips cache and goes to `UserLogins`.
- This slot did not re-attach; trader counts (prior notes 8+10 groups / 6512+1948 logins) are **not** re-measured.
- Hosted **scoring** is deals-only (`ListLoginsWithDealsAsync`). Catalog persist of all manager traders is a different hop from scoring coverage.

Capability claimed is **present in the file**.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire compilation unit `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Only outbound MsgType is Logon `A`:

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
        return Assemble(fields);
```

Wire I/O: one `WriteAsync` (L49), one `ReadAsync` (L53), then `using` disposes `TcpClient` / `SslStream`. No loop, no heartbeat, no NewOrderSingle, no `(35, "D")`. Inbound `Extract(..., "35")` only classifies the Logon reply.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and logs `NewOrderSingle still unimplemented` (L68–70). Persist updates status only; it does not send.

**Residual (does not put `35=D` on `CTraderFixSession`):**

- Sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 is a **different type**. Gated to `demo-*` host / `demo.*` sender; refuses `live-*` / `live.` / account `1369850`. Called only from `D:\Prop\tools\DemoFixTestTrade\Program.cs`. Not in DI. Not on the API/copy hop.
- Sibling `CTraderFixDemoMatrix` L93 also `Build("D")` — same tools path.

Assigned claim is **`CTraderFixSession` has no `35=D`**. Proven.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live composition **arms it**.

### 5.1 Lab env is true

`D:\Prop\.env` L73 (boolean only; no other keys quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

API loads that file before building configuration:

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` includes the hard path `D:\Prop\.env` (`EnvFile.cs` L14) and `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line.

### 5.2 DI binds the env token onto process runtime

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Grep of product `*.cs` for `RealCopyEnabled =` assignment: **only this line**. There is no later `= false`.

### 5.3 Hosted FIX logon does not re-pin false

`CTraderFixLogonHostedService.ExecuteAsync` writes quote/trade logon state and logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled`. It never assigns `RealCopyEnabled`.

`/api/settings` echoes the runtime bit (not a hardcoded false):

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

`CopyTradingService.GetStatusAsync` reports `RealCopyArmed: _runtime.RealCopyEnabled` (L46). `BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only when the bit is already false** (L478–479). When env is `true`, that blocker is absent.

### 5.4 What still looks “false” (and why it does not save the claim)

| Surface | Value | Bound to hosted hop? |
|---|---|---|
| Architecture / README / docs | `REAL_COPY_EXECUTION_ENABLED=false` | Docs only |
| `CTraderFixOptions.RealCopyExecutionEnabled` default | `false` (L35) | **Unread** by logon host / copy service |
| `fix-worker` `CTrader:RealCopyExecutionEnabled` | `GetValue(..., false)` | Different key; log-only; worker does not send |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Blocks send; **not** the flag |
| Persist `AllowFixSend` | literal `false` (L306) | Blocks send; **not** the flag |

Docs and a default POCO do not pin the **runtime** flag. The claim is “stays false.” On the API process it becomes **true**.

Slots / notes that still say “DI/hosted pin false” (W500_RESEARCH_68/108, CREDENTIALS_AND_COPY_STATUS “forced false”) are **stale**.

---

## Copy hop residual — SAFE_BY_ABSENCE (does not flip verdict)

Claim 5 fails. Destination capital is still not at risk **today** because there is no sender:

- `CTraderFixSession` outbound is `35=A` only (claim 4).
- `CopyTradingService.NewOrderSingleImplemented = false` and `VenueReconciled = false`.
- Persist writes `AllowFixSend = false` unconditionally (L306). The LIVE-send branch (L312) is dead: it requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled`.
- Intents are forced `SHADOW_ONLY` (L318).
- No `ExecutionIntent` writer on this hop.

`SAFE_BY_ABSENCE` is **not** the assigned claim. The assigned claim is the flag stays false. It does not.

If a sender is added while `.env` remains `true` and DI still binds it, the next hop would see runtime armed (`LiveRuntimeStatus` copyNote already says “REAL_COPY armed…” when the bit is true).

---

## Honesty / not proven this slot

- No Manager re-attach. Group/trader **counts** are not re-measured.
- No FIX socket opened by this slot. Logon success/failure not measured.
- No `/api/settings` HTTP GET (loopback not used). File proof is enough for the bind.
- Worker hosts do not call `EnvFile.FindAndLoad()`. Their `RealCopyEnabled` depends on process env inheritance. The **API** host does load `.env`. That is enough to fail claim 5.

---

## Verdict

**FAIL.**

1. DemoSeeder is **not** the API startup path (`BrokerCatalogSeed` only). **PASS.**
2. Native **can** list groups via `GroupRequestArray("*")` then `GroupTotal`/`GroupNext`. **PASS** (source capability).
3. Native **can** list traders via `UserRequestArray` then `UserLogins`. **PASS** (source capability).
4. `CTraderFixSession` has **no** `35=D` (135/135; only `(35, "A")`). **PASS.**
5. `REAL_COPY_EXECUTION` does **not** stay false: `.env` L73 `true` + `EnvFile.FindAndLoad` + DI L41 bind + no logon re-pin. **FAIL.**

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` on the copy hop).
