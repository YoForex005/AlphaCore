# W500_VERIFY_58 — Adversarial live-path verify (slot 58)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_58.md` |
| Agent / slot | W500 adversarial **verify 58** |
| Date | 2026-08-18 |
| Role | Independent verifier. **Did not trust** sibling W500 / A002 / A014 / CREDENTIALS reports. Re-read live files this slot. |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Boolean keys quoted only. |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Full `read_file` of `apps/api/Program.cs` (160/160), `apps/fix-worker/Program.cs`, `apps/mt5-worker/Program.cs`, `NativeMt5BrokerConnector.cs` (459/459), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `EnvFile.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` header, `RiskEngine.cs` allow-send, both worker `Worker.cs`, `SettingsController.cs`, `apps/api/appsettings.json`, `tools/LiveBrokerProbe/Program.cs` header, `CTraderFixDemoTestTrade.cs` header. Targeted `grep` of `DemoSeeder`, `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins`, `35=D`/`Build("D")`/`NewOrderSingle`, `REAL_COPY_EXECUTION`, `EnvFile`. Flag-only grep of `D:\Prop\.env` L73 / L106. |

**Honesty rule:** if a claim cannot be proven from the file this slot, that claim is **FAIL**. Prior swarm prose is not evidence. `SAFE_BY_ABSENCE` is not “flag stays false.” A Logon `35=A` is not a NewOrderSingle. A demo helper that can `Build("D")` is not `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from live source. **Claim 5 is disproven:** `REAL_COPY_EXECUTION` does **not** stay false.

| # | Assigned claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS_SOURCE** | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*")`; empty fallback L174 `GroupTotal` + `GroupNext`. Completeness not re-attached this slot. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS_SOURCE** | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest uses `GetAccountsAsync(null)`. Completeness not re-attached. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File 135/135: outbound tag 35 is `"A"` only (`BuildLogon` L96). Grep of this file for `35=D` / `(35, "D")` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | DI L41 binds env. Lab `.env` L73 is `true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. Hosted logon **does not** re-pin false. `/api/settings` exposes `runtime.RealCopyEnabled`. |

**Overall slot verdict: FAIL** (instruction: FAIL if any claim cannot be proven from the file). Claim 5 is the opposite of the assigned statement.

**Risk to capital: NONE** (`SAFE_BY_ABSENCE` on the **copy hop**). Flag may be **armed**; there is still no `CTraderFixSession` NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`; `VenueReconciled = false`. That is **not** claim 5. Do not paper over the armed flag.

Stale siblings this slot contradicts: `CREDENTIALS_AND_COPY_STATUS.md` “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**”; W500_68 / W500_108 “DI/hosted pin false”; A002 “API still calls `DemoSeeder`”; A006 “`/api/settings` hardcodes false”; A014 if it claims DI pins false.

---

## 1. DemoSeeder is not the API startup path — PASS

Read: `D:\Prop\apps\api\Program.cs` (160/160).

Startup seed is catalog-only:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

There is **no** `DemoSeeder.SeedAsync`. The `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`.

Independent greps this slot:

- `DemoSeeder` under `D:\Prop\apps` = **0**
- Product `Program.cs` callers of `BrokerCatalogSeed.EnsureAsync`: API L156, `apps/mt5-worker/Program.cs` L15, `apps/fix-worker/Program.cs` L15
- C# callers of `DemoSeeder.SeedAsync` that remain: `tests/Integration/SeedingAndStoreTests.cs` L25 plus `_tmp_*` eval harnesses under `reports/swarm/20260818/`. **Not** API / workers / probe.

DI fail-closes Fake:

```36:50:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`LiveMt5Registration.CreateConnectors` returns **two** `NativeMt5BrokerConnector` instances (Achiever + Starwave). Zero `FakeMt5BrokerConnector`.

**Residual (does not revive claim 1):**

- `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` still exists (`public static class DemoSeeder` L14). Integration tests still call `DemoSeeder.SeedAsync`. **API process does not.**
- `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover worker scorer, **not** API startup. Hosted ingest scores `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106–125).

Prior reports that still say API startup calls `DemoSeeder` (A002 / A005 / A010 / A011) are **stale** against the current `Program.cs`.

---

## 2. Native groups via GroupRequestArray or GroupTotal — PASS_SOURCE

Read: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (459/459).

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

File-proven capability:

1. Primary: Manager request API `GroupRequestArray("*")` (wildcard = all groups the manager can see).
2. Fallback: if the request list is empty, walk `GroupTotal()` + `GroupNext`.

Live ingest uses this path:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` walks **every** group returned by `GetGroupsCore()` (`NativeMt5BrokerConnector` L199–203).

**Honesty limits (not a claim-2 FAIL):**

- This slot did **not** live-attach. Counts 18/8460 cited by siblings are **not** re-measured here.
- If `GroupRequestArray` returns `OK` with a **partial non-empty** set, `GroupTotal` is skipped (`if (list.Count == 0)`). The assigned claim is “via GroupRequestArray **or** GroupTotal,” which the file satisfies. Completeness of a live Manager reply is not in the file.

---

## 3. All traders via UserRequestArray / UserLogins — PASS_SOURCE

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

File-proven capability:

1. Primary: `UserRequestArray(gname, users)`.
2. Fallback when `users.Total()==0`: `UserLogins` then `UserRequestByLogins`.
3. Catalog `GetAccountsAsync(null)` iterates every group name from claim 2.

**Honesty limits (not a claim-3 FAIL):**

- `UserLogins` is **not** called when `UserRequestArray` returns a non-empty array. Partial non-empty request results would not fall through. The assigned claim is “via UserRequestArray / UserLogins,” which the file implements.
- Hosted **scoring** is `ListLoginsWithDealsAsync` (deals-only), not “score every login.” Catalog persist of traders is still `GetAccountsAsync(null)`.
- No live attach this slot.

---

## 4. CTraderFixSession has no 35=D — PASS

Read: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (135/135).

Grep of **this file** for `35`:

| Line | Token | Role |
|---|---|---|
| 55 | `Extract(reply, "35")` | inbound MsgType parse |
| 73 | interpolated `35={msgType}` | reject log text |
| 96 | `(35, "A")` | **only outbound MsgType** — Logon |

Zero `35=D`. Zero `(35, "D")`. Zero `NewOrderSingle`. Single `WriteAsync` (L49) of the Logon bytes. Sockets disposed via `using`.

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
            ...
            (553, username),
            (554, password)
        };
        return Assemble(fields);
    }
```

Hosted caller is one-shot logon then persist (`CTraderFixLogonHostedService` L48–58). Log text at L69: “NewOrderSingle still unimplemented.”

**Residual (does not fail claim 4):** siblings `CTraderFixDemoTestTrade` (`Build("D")` ×3) and `CTraderFixDemoMatrix` (`Build("D")` ×1) are **not** `CTraderFixSession`. Demo helper refuses live host / live sender / account `1369850`. Copy hop does not call them. `CopyTradingHostedService` only calls `GenerateShadowIntentsAsync`.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

The assigned claim is that the flag **stays false**. Files prove the opposite on the API host.

### 5.1 Lab env is true

Flag-only grep of `D:\Prop\.env`:

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true` (not the assigned flag; quoted as boolean only)

No secret values read or printed.

### 5.2 API loads that file into process env, then DI binds it

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
...
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` includes a hard path `D:\Prop\.env` (`EnvFile.cs` L14) and `SetEnvironmentVariable` for every `KEY=VALUE` line.

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

`apps/api/appsettings.json` has **no** `REAL_COPY_EXECUTION_ENABLED` key. `FeatureFlags.LiveCopyEnabled` (default false) is a **different name** and is **not** what DI reads.

### 5.3 Hosted FIX logon does not re-pin false

Read `CTraderFixLogonHostedService.cs` (112/112). It **reads** `_runtime.RealCopyEnabled` for a log line (L69–70). It never assigns `RealCopyEnabled = false`. Older “hosted pin false” reports are **stale**.

### 5.4 API surface exposes the bound bool

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`/api/health` also emits `realCopyEnabled = runtime.RealCopyEnabled` (L55).

`SettingsController` (`/api/settings` MVC) still talks about `FeatureFlags.LiveCopyEnabled` default false and is Redis-backed. That controller is **not** the flag that DI arms. Split-brain, but it does not save claim 5.

### 5.5 POCO default is false — unread by the live runtime

`CTraderFixOptions.RealCopyExecutionEnabled` defaults `false` (L35). `AddTraderIntelligence` does **not** bind `IOptions<CTraderFixOptions>` onto `LiveRuntimeStatus`. Fix-worker `Worker.cs` L21 reads `CTrader:RealCopyExecutionEnabled` (default false) for a log only; workers do **not** call `EnvFile.FindAndLoad()`. That worker pin is **not** the API runtime.

### 5.6 Copy hop is still SAFE_BY_ABSENCE (does not prove claim 5)

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

Persist path hardcodes `AllowFixSend = false` (L211). Live-send branch also requires `NewOrderSingleImplemented && VenueReconciled` (L217) and then only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED`. Hosted copy (`CopyTradingHostedService`) calls `GenerateShadowIntentsAsync` only.

`RiskEngine` *would* set `AllowFixSend = true` if `RealExecutionEnabled && Reconciled && VenueHealthy` (L147–150). Copy service **overwrites** persist to `false` and has no FIX sender.

`CREDENTIALS_AND_COPY_STATUS.md` L30 “`REAL_COPY_EXECUTION_ENABLED` **false (forced)**” is **stale** against DI + `.env` L73.

**Claim 5 verdict: FAIL.** The flag does not stay false on the API composition root.

---

## 6. What this slot did not prove

| Item | Status |
|---|---|
| Live Manager attach / group+login census | **Not done.** Do not reuse 18/8460 as this slot’s measurement. |
| Live FIX `35=A` success | **Not done.** |
| Any `35=D` on the wire | **Not sent** (and `CTraderFixSession` cannot build one). |
| Product source change | **None.** |
| Secret values | **None printed.** |

---

## 7. Residual register (honest, out of assigned claims)

1. `.env` L73 `REAL_COPY_EXECUTION_ENABLED=true` is DI-bound. Next sender would see runtime armed.
2. `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` can `Build("D")` (demo-gated, tools-only, not on copy hop).
3. `apps/mt5-worker/Worker.cs` still scores four dummy logins after live catalog sync.
4. `DemoSeeder.cs` remains on disk for tests.
5. `SettingsController` vs minimal `/api/settings` flag-name split (`LiveCopyEnabled` vs `REAL_COPY_EXECUTION_ENABLED`).
6. `GroupRequestArray` / `UserRequestArray` skip the `*Total` / `UserLogins` fallback when the request array is non-empty (partial-result hole).

---

## 8. Slot close

| Item | Value |
|---|---|
| Slot | **58** |
| Verdict | **FAIL** |
| Claims 1–4 | Proven from files (2–3 = source capability; not re-attached) |
| Claim 5 | **Disproven** — flag does not stay false |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE`) |
| Product edited | **No** |
