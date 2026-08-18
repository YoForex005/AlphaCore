# W500_VERIFY_45 — Adversarial live-path verify (slot 45)

| Item | Value |
|---|---|
| Date | 2026-08-18 |
| Slot | **45** |
| Role | Adversarial verifier. Read live path files. Do not trust other agents. |
| Product source modified | **No** |
| Live Manager attach this slot | **No** |
| Live `35=D` sent | **No** |
| Secret values printed | **None** (quoted only the boolean `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`) |
| Overall verdict | **FAIL** |

**Rule used:** FAIL if any assigned claim cannot be proved from the live file. Claim 5 is **disproved** by the live files.

This slot independently re-read: `apps/api/Program.cs`, both worker `Program.cs`, `NativeMt5BrokerConnector.cs`, `CTraderFixSession.cs`, `DependencyInjection.cs`, `LiveMt5Registration.cs`, `EnvFile.cs`, `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` (existence only), `CTraderFixDemoTestTrade.cs` (residual, not copy hop), and the boolean-only line of `D:\Prop\.env` L73. Prior swarm notes were **not** treated as evidence.

---

## Assigned claims

| # | Claim | Verdict | Proof |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `apps/`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `GetGroupsCore` calls `GroupRequestArray("*")` then, if `list.Count == 0`, `GroupTotal`/`GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` calls `UserRequestArray` first; if `users.Total()==0`, `UserLogins` + `UserRequestByLogins`. Catalog uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Entire file **135/135**. Zero `35=D` / `NewOrderSingle`. Only outbound MsgType is `(35, "A")`. One `WriteAsync`. Sockets disposed. |
| 5 | `REAL_COPY_EXECUTION` stays **false** | **FAIL** | Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. DI L41 binds it onto `LiveRuntimeStatus.RealCopyEnabled`. Hosted logon does **not** re-pin false. `/api/settings` echoes the runtime. |

Overall **FAIL** because claim 5 cannot be proved (the opposite is in the files).

Destination capital risk remains **NONE** (`SAFE_BY_ABSENCE`): copy hop still has no `35=D` builder, `NewOrderSingleImplemented=false`, persist `AllowFixSend=false`.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160 lines).

Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists solely for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- `DemoSeeder` under `D:\Prop\apps\api` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15

DI fail-closes Fake and registers Native only:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` (`D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` L20–49) returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`. Dual-AND of `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` via `IsSecret`.

`BrokerCatalogSeed.EnsureAsync` writes Achiever + StarwaveFX broker rows, XAUUSD, kill-switch `None`, and two FIX rows already `Disconnected` with LastError `"session up for logon/recon only; NewOrderSingle off"` (`BrokerCatalogSeed.cs` L77–107). It does not ingest Fake logins 10001/10002.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests (`tests/Integration/SeedingAndStoreTests.cs` L25) still call `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}`. That is a leftover worker scorer, **not** API startup.
- Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService.cs` L106), not the dummy quartet.

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459 lines).

```144:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

File-proved:

- Primary walk is `GroupRequestArray("*")` (wildcard, not `MT5_GROUP_*` plan filter).
- Fallback is `GroupTotal` + `GroupNext` **only when** the request list is empty.
- `_pumpEnabled` does **not** gate this method (no branch).
- Live ingest calls `connector.GetGroupsAsync` via `DealIngestionService.SyncCatalogAsync` L45.

**Not proved this slot (so not claimed as live census):** Achiever 8 / Starwave 10 / 18 groups. This slot did not attach.

**Adversarial residual:** if `GroupRequestArray("*")` returns a **non-empty partial**, `GroupTotal` never runs. The OR is request-first / total-if-empty, not a union. That does not un-prove the assigned “via GroupRequestArray or GroupTotal” wiring.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

Same file, `GetAccountsCore` + `ReadAccountsForGroup`:

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
            else
            {
                foreach (var g in GetGroupsCore())
                    groups.Add(g.Name);
            }
            // ...
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
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

File-proved:

- Catalog `GetAccountsAsync(null)` walks **every** group from `GetGroupsCore`.
- Per group: `UserRequestArray` first.
- Cache `UserGetByGroup` only on hard fail (not OK / OK_NONE / NOTFOUND).
- Empty array → `UserLogins` + `UserRequestByLogins`.
- Ingest: `DealIngestionService.SyncCatalogAsync` L48 `GetAccountsAsync(null, ct)` — flag-blind (does not read `REAL_COPY` / `FEATURE_COPY`).

**Not proved this slot:** 6512 + 1948 = 8460 logins. Not re-attached.

**Adversarial residual:** `UserLogins` is skipped when `UserRequestArray` already returned a non-empty set (same partial-success hole as groups). Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106) — catalog can still hold all accounts; scores do not.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135** physical lines).

Outbound builder is Logon only:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
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

Independent greps this slot on that file:

| Token | Count |
|---|---|
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `NewOrderSingle` | **0** |
| `(35, "A")` | **1** (L96) |
| `WriteAsync` | **1** (L49) |

`TryLogonAsync` is one-shot: `using` TcpClient + SslStream, one write, one read, then dispose. Hosted caller is `CTraderFixLogonHostedService` L48/L54 (QUOTE 5211 / TRADE 5212). Persist path does not invent a sender.

**Residual (off assigned file, not a claim-4 fail):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197 exists. Grep shows it is called only from `tools/DemoFixTestTrade/Program.cs` — **not** DI, **not** API, **not** copy. Demo-gated (`host` must start `demo-`; sender must start `demo.`; refuses `live-` / `live.` / account `1369850`). Copy hop remains `SAFE_BY_ABSENCE`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Live files show the opposite on the API runtime path.

### 5.1 Lab env is true

`D:\Prop\.env` L73 (boolean only; no other keys quoted):

```
REAL_COPY_EXECUTION_ENABLED=true
```

L106 (boolean only): `FEATURE_COPY_TRADING_ENABLED=true`.

### 5.2 API loads that file into process env

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) includes the hard path `D:\Prop\.env` and `Environment.SetEnvironmentVariable(key, value)` for every `KEY=value` line.

### 5.3 DI binds the env token onto runtime

```39:43:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(runtime);
```

With `.env` L73 `true`, `LiveRuntimeStatus.RealCopyEnabled` is **true** on API / both workers (all three call `AddTraderIntelligence`).

### 5.4 Hosted logon does not re-pin false

Full read of `CTraderFixLogonHostedService.cs` (112 lines): it writes Quote/Trade logon status and logs `RealCopyArmed={Armed}` from `_runtime.RealCopyEnabled` (L68–70). There is **no** `_runtime.RealCopyEnabled = false`. Reports that cite a hosted hard-false pin (W500_68 / 108 / 57 / 91 / 111) are **stale**.

### 5.5 Settings API echoes the runtime

```71:77:D:\Prop\apps\api\Program.cs
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`/api/health` also exposes `realCopyEnabled = runtime.RealCopyEnabled` (L55). E038 / A006 / CREDENTIALS “forced false” / README “off (`…=false`)” are **stale** against this binding.

### 5.6 What is still false (does not rescue claim 5)

| Surface | Value | Why it does not prove “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` POCO default | `false` (L35) | Unbound from env `REAL_COPY_EXECUTION_ENABLED` (would need `CTrader__RealCopyExecutionEnabled`). Dead default. |
| fix-worker `Worker.cs` L21 | `_config.GetValue("CTrader:RealCopyExecutionEnabled", false)` | **Different key.** Logs only; stamps sessions `Disconnected`; does not send. |
| `CopyTradingService.NewOrderSingleImplemented` | `const false` | Blocks send; does **not** keep the env/runtime flag false. |
| Persist `AllowFixSend` | literal `false` (`CopyTradingService.cs` L211) | Send choke, not a flag pin. |
| Architecture / `.env.example` / docs | say `false` | Policy, not the running lab. |

Claim 5 as written (“stays false”) is **disproved**. The lab is **armed** on `LiveRuntimeStatus`. A future sender that checked only the runtime flag would see `true`.

---

## Copy hop / risk to capital

Even with claim 5 FAIL, this slot did **not** find a live send path:

| Gate | File | State |
|---|---|---|
| `CTraderFixSession` outbound | assigned session | `(35, "A")` only |
| `NewOrderSingleImplemented` | `CopyTradingService.cs` L17 | `const false` |
| `VenueReconciled` | same L16 | `const false` |
| Persist `AllowFixSend` | L211 | always `false` (ignores `decision.AllowFixSend` for the row) |
| LIVE promotion | `BuildBlockers` L310 | “0 traders in LIVE (promotion is manual…)” |
| Hosted copy | `CopyTradingHostedService` | SHADOW intents only |
| Demo `Build("D")` | tools CLI | not wired to API/DI/copy; demo-gated off live identity |

`CopyTradingService.GetStatusAsync` L42–58: `FeatureCopyEnabled: true`, `RealCopyArmed: _runtime.RealCopyEnabled`, summary still “Shadow intents only. Pepperstone will not receive NewOrderSingle.” `BuildBlockers` still adds `"No NewOrderSingle sender — SAFE_BY_ABSENCE"` first.

**Risk to capital: NONE** (`SAFE_BY_ABSENCE`). Residual: next person who adds a `35=D` builder will see runtime already armed.

---

## Stale prior notes (do not reuse)

| Claim | Stale if it says… |
|---|---|
| 1 | API / workers still call `DemoSeeder` (A002, A005, A010, A011, B07) |
| 5 | DI / hosted / settings **pin** `REAL_COPY` false (W500_68, 108, 57, E038, CREDENTIALS, README L28) |
| 4 product-wide | “product `35=D=0`” — sibling demo helper can `Build("D")` off-hop |
| 5 “unbound” | A003 / A015 “env not bound to runtime” — **false** now; DI L41 binds it |

---

## What this slot did not do

- Did not attach Manager (no new 18/8460 proof).
- Did not GET `:5000/api/settings` (localhost not probed).
- Did not edit product source.
- Did not print passwords, hosts-as-secrets beyond already-public catalog IPs in `BrokerCatalogSeed`, or FIX credentials.

---

## Verdict

**FAIL.** Claims 1–4 file-proven (claim 2/3 as `PASS_SOURCE`, not live census). Claim 5 **FAIL**: `REAL_COPY_EXECUTION_ENABLED` does **not** stay false — `.env` L73 `true` + `EnvFile` + DI L41 + no logon re-pin. Copy hop still cannot send (`SAFE_BY_ABSENCE`). Risk to capital **NONE**.
