# W500_VERIFY_2 — Adversarial live-path verify (slot 2)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_2.md` |
| Agent / slot | Adversarial verifier **slot 2** |
| Date | 2026-08-18 |
| Role | Independent re-read of live product files. **Do not trust sibling agents.** |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** (boolean quoted only) |
| Secrets printed | **None.** Only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Runtime flag inferred from source + `.env` bind. |
| Method | Direct `read_file` of `apps/api/Program.cs` (160/160), both worker `Program.cs`, `DemoSeeder.cs` header, `BrokerCatalogSeed.cs`, `NativeMt5BrokerConnector.cs` (458/458), `DealIngestionService.cs`, `LiveIngestHostedService.cs` head, `LiveMt5Registration.cs` head, `CTraderFixSession.cs` (135/135), `CTraderFixDemoTestTrade.cs` head + gates, `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CopyTradingService.cs` (gates + persist), `EnvFile.cs`, `apps/api/appsettings.json`, `SettingsController.cs`, `apps/fix-worker/Worker.cs` head. Targeted `grep` of `DemoSeeder` (apps=0; product C# = class + tests only), `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35`/`"D"`/`NewOrderSingle` in `CTraderFixSession.cs`, `REAL_COPY_EXECUTION_ENABLED` in `.env` (L73 only). |

**Honesty rule:** prove each assigned claim from the file or **FAIL** that claim. Capability in source is not a live census. A POCO default is not a process pin. An armed flag is not a ticket. Absence of a copy-path `35=D` is `SAFE_BY_ABSENCE`, not a §68/§70 PASS. Sibling reports are **not** evidence.

---

## 0. Verdict (binding)

**FAIL** — claim **(5) is disproven** from live files. Claims (1)–(4) are **PASS** from the same files.

| # | Assigned claim | Verdict | Why |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | API `Program.cs` seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (capability) | `GetGroupsCore` L155 `GroupRequestArray("*")` first; L174 `GroupTotal` + `GroupNext` if that list is empty. This slot did **not** re-attach; live 18-group count is **not** re-proven here. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest calls `GetAccountsAsync(null)`. Live 8460 is **not** re-proven here. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Assigned file 135/135: outbound MsgType is `(35, "A")` only. Zero `"D"` / `NewOrderSingle` tokens. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. DI L41 binds that string onto `LiveRuntimeStatus.RealCopyEnabled`. Logon host **does not** re-pin false. `/api/settings` exposes the runtime bit. |

One-line:

```text
SLOT2 FAIL: DemoSeeder off API path; Native GroupRequestArray(*) / UserRequestArray+UserLogins present; CTraderFixSession 35=A only; REAL_COPY does NOT stay false (.env true + DI binds). Capital risk NONE (SAFE_BY_ABSENCE).
```

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` 152–159.

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

- `using TraderIntelligence.Infrastructure.Seeding;` (L6) exists **only** so `BrokerCatalogSeed` resolves.
- There is **no** `DemoSeeder.SeedAsync`.
- Grep `DemoSeeder` under `D:\Prop\apps` = **0** hits.
- Same seed on both workers: `apps/mt5-worker/Program.cs` L15 and `apps/fix-worker/Program.cs` L15 are `BrokerCatalogSeed.EnsureAsync` only.

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Product C# callers: `tests/Integration/SeedingAndStoreTests.cs` L25 + throwaway `_tmp_*` trees under reports. **API process does not call it.**

DI (`DependencyInjection.cs` L36–48) fail-closes without both real MT5 passwords and registers `LiveMt5Registration.CreateConnectors` — Native ×2 only (`LiveMt5Registration.cs` L23–49). No Fake registration on the host path.

`A002_api_dummy_path.md` (API still calls `DemoSeeder`) is **stale** vs this tree.

---

## 2. Native connector can list all groups — PASS (source capability)

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` L144–187.

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

- Primary: request API `GroupRequestArray("*")` (manager-visible mask, not a plan list).
- Fallback: cache walk `GroupTotal` + `GroupNext` **only if** the request list is empty.
- Live ingest uses this: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`; `LiveIngestHostedService` L56 `SyncCatalogAsync`.

**What this slot cannot prove from the file:** that a live Achiever+Starwave attach currently returns 18 groups. That would require a Manager Connect this slot did not perform. `A001_native_connector.md` (“zero `GroupRequestArray` under `src`”) is **stale**.

**Residual (not a FAIL of the assigned claim):** if `GroupRequestArray` returns OK with a **partial** array, the cache fallback is skipped.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (source capability)

Read: `GetAccountsCore` L189–214 + `ReadAccountsForGroup` L216–271.

```189:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        lock (_gate)
        {
            Ensure();
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

            var byLogin = new Dictionary<ulong, Mt5AccountDto>();
            foreach (var gname in groups)
            {
                foreach (var row in ReadAccountsForGroup(gname))
                    byLogin[ (ulong)row.Login ] = row;
            }

            return byLogin.Values.ToList();
        }
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

- `GetAccountsAsync(null)` walks **every group name** from claim 2, then per-group users.
- Primary: `UserRequestArray(gname)`.
- Hard-fail fallback: `UserGetByGroup` (pump cache).
- Empty array: `UserLogins` then `UserRequestByLogins`.
- Ingest: `DealIngestionService` L48 / L62 `GetAccountsAsync(null, ct)`.

**What this slot cannot prove from the file:** live login totals (prior reports cite 6512+1948=8460). Not re-attached.

**Residual:** if `UserRequestArray` returns OK with a non-empty **subset**, `UserLogins` is not called (`users.Total() == 0` gate). That is the SDK contract, not a missing symbol.

---

## 4. CTraderFixSession has no 35=D — PASS

Read entire file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135 lines).

Grep of **this file** for `35` / `"D"` / `NewOrderSingle`:

| Line | Token | Role |
|---|---|---|
| 55 | `Extract(reply, "35")` | inbound MsgType parse |
| 73 | interpolated `35={msgType}` | reject log |
| 96 | `(35, "A")` | **only outbound MsgType** |

Zero `"D"`. Zero `NewOrderSingle`. Single `WriteAsync` at L49 of the Logon buffer from `BuildLogon`. Sockets disposed via `using`.

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
            // ... 49/56/50/57/52/98/108/141/553/554 — Logon only
        };
        return Assemble(fields);
    }
```

Hosted caller `CTraderFixLogonHostedService` L48–58 calls `TryLogonAsync` twice (QUOTE 5211, TRADE 5212). No other `CTraderFixSession` method exists.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade.Build("D")` at L139/163/197 is a **different type**. Demo-gated (`demo-` host / `demo.` sender / refuse `live-*` / refuse account `1369850`). Caller is `tools/DemoFixTestTrade`, not API/DI/copy. `CTraderFixDemoMatrix.cs` L87 also `Build("D")`. Those are **not** `CTraderFixSession`.

Copy hop still cannot send: `CopyTradingService.NewOrderSingleImplemented = false` (L17); persist `AllowFixSend = false` (L211); `VenueReconciled = false` (L16). Conjunction at L217 is unreachable.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Assigned claim is **process stays false**. Files prove it **does not**.

### 5.1 Lab leftover is already true

`D:\Prop\.env` L73 (boolean only):

```text
REAL_COPY_EXECUTION_ENABLED=true
```

L106 is `FEATURE_COPY_TRADING_ENABLED=true` (different token; API ignores this key and hardcodes FEATURE true).

### 5.2 API loads `.env` into the process, then DI binds it

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
// ...
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–19) searches cwd parents then **`D:\Prop\.env`**, then `Environment.SetEnvironmentVariable` (L38).

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

There is **no** hardcoded `false`. `CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed={Armed}` and does **not** assign `_runtime.RealCopyEnabled = false`.

`/api/settings` L76: `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` — follows the bind, so a host that loaded this `.env` **advertises true**.

`/api/health` L55 exposes `realCopyEnabled = runtime.RealCopyEnabled` the same way.

`CopyTradingService.GetStatusAsync` L44: `RealCopyArmed: _runtime.RealCopyEnabled`. Blocker `"REAL_COPY_EXECUTION_ENABLED is false"` is added **only if** the bit is already false (L316–317) — so an armed process **drops that blocker**.

### 5.3 What *is* still false (does not rescue claim 5)

| Surface | Value | Why it does not prove “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default `false` | POCO **unbound** (no `Configure<CTraderFixOptions>`). Not the runtime bit. |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **Different name.** Not `REAL_COPY_EXECUTION_ENABLED`. |
| `SettingsController` `LiveCopyEnabled` default | `false` | Controller is **unwired** (API `Program.cs` has no `AddControllers` / `MapControllers`). Live route is the minimal API. |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | Nested key, **log-only**. Worker still stamps TRADE `Disconnected`. |
| Architecture / README / docs | `=false` | Policy, not process. |

Reports that say DI/hosted/`.env` pin false (`W500_68` / `W500_108` / `CREDENTIALS_AND_COPY_STATUS` “forced false” / `A015` “process pin”) are **stale** vs this tree.

**Policy still says the flag must stay false.** The assigned claim was that it **stays** false. Measured: leftover `true` + bind + no re-pin = **FAIL**.

This slot did **not** flip `.env`. Operator should set L73 back to `false`. Do **not** treat the leftover `true` as a send license.

---

## 6. Risk to capital

**NONE** from the copy hop today (`SAFE_BY_ABSENCE`).

| Gate | Measured |
|---|---|
| `CTraderFixSession` outbound | `35=A` Logon only |
| Copy `NewOrderSingleImplemented` | `const false` |
| Persist `AllowFixSend` | forced `false` |
| `VenueReconciled` | `const false` |
| `ExecutionIntent` writers on this hop | none inspected as senders; L217 branch writes status string only |
| Demo `Build("D")` | tools + demo-host gate; not wired to API/workers/copy |

An armed `RealCopyEnabled=true` is a **wish bit**. The next engineer who adds a `35=D` builder would see the runtime already armed. That is why claim 5 is FAIL even though capital is not at risk yet.

Do **not** add a copy-path NewOrderSingle. Do **not** wire `CTraderFixDemoTestTrade` into hosted copy. Do **not** flatten MT5 source.

---

## 7. Stale siblings (do not recycle)

| Sibling claim | This slot |
|---|---|
| A002 / A005: API startup = `DemoSeeder` | **STALE** — `BrokerCatalogSeed` only |
| A001: zero `GroupRequestArray` / `UserRequestArray` under `src` | **STALE** — L155 / L223 live |
| A014 / W500_68 / W500_108: `RealCopyEnabled` hardcoded false + logon re-pin | **STALE** — DI binds env; logon pin gone |
| “product 35=D=0 everywhere” | **HALF-STALE** — `CTraderFixSession` is 0; demo helper is not |

---

## 8. Checklist

- [x] DemoSeeder gone from **API startup** (class remains for tests)
- [x] Native `GroupRequestArray("*")` + `GroupTotal` fallback
- [x] Native `UserRequestArray` + `UserLogins` fallback
- [x] `CTraderFixSession` has no `35=D`
- [ ] `REAL_COPY_EXECUTION` stays false — **FAIL** (`.env` true, DI binds, no re-pin)
- [x] Secrets not printed
- [x] Product source not edited
