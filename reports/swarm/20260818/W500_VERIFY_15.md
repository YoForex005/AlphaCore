# W500_VERIFY_15 — Adversarial live-path verify (slot 15)

| Field | Value |
|---|---|
| Slot | **15** |
| Date | 2026-08-18 |
| Agent | W500_VERIFY_15 (adversarial; did **not** trust sibling reports) |
| Assigned | Confirm from **live path files on disk**: (1) DemoSeeder is not the API startup path; (2) Native connector can list all groups via `GroupRequestArray` or `GroupTotal`; (3) all traders via `UserRequestArray` / `UserLogins`; (4) `CTraderFixSession` has no `35=D`; (5) `REAL_COPY_EXECUTION` stays false. |
| Product source modified | **No.** Report only. |
| Test source modified | **No.** |
| Secrets printed | **None.** Password / proxy-auth / FIX password values were not read and are not quoted. Only boolean tokens `REAL_COPY_EXECUTION_ENABLED=true` and `FEATURE_COPY_TRADING_ENABLED=true` are named. |
| Live attach this slot | **No.** No Manager socket. No FIX socket. No loopback `/api/settings` GET. Census **not** re-proved here. |
| Method | Full `read_file` of `apps/api/Program.cs` (160), `NativeMt5BrokerConnector.cs` (458), `CTraderFixSession.cs` (135), `DependencyInjection.cs` (62), `CTraderFixOptions.cs` (80), `LiveRuntimeStatus.cs` (66), `CopyTradingService.cs` (320), `CTraderFixLogonHostedService.cs` (112), `DealIngestionService.cs` (146), `LiveMt5Registration.cs` (94), `EnvFile.cs` (41), `BrokerCatalogSeed.cs` (head), `DemoSeeder.cs` (head), both worker `Program.cs`, `fix-worker/Worker.cs`, `mt5-worker/Worker.cs` (head), `appsettings.json`, `launchSettings.json`. Targeted `grep` on `DemoSeeder`, `GroupRequestArray`/`GroupTotal`, `UserRequestArray`/`UserLogins`, `35=D`/`NewOrderSingle`/`Build("D")`, `REAL_COPY_EXECUTION_ENABLED`. Vendor header `MT5APIManager.h` L205–212 / L254 / L410 confirmed the same symbol names. `.env` L73 and L106 inspected **for those two flag keys only**. |
| Honesty rule | **FAIL any claim that cannot be proved from a file this slot.** Sibling `W500_*` / `A014` / `CREDENTIALS_AND_COPY_STATUS.md` are **not** evidence. A policy comment that says “must stay false” is **not** a runtime pin. |

---

## 0. Verdict (binding)

| # | Claim | Result | Proof class |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Token `DemoSeeder` under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; if `list.Count == 0`, L174 `GroupTotal` + `GroupNext`. Ingest calls `GetGroupsAsync`. **Not** a live-attach proof of completeness. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty array → L230 `UserLogins` + `UserRequestByLogins`. `GetAccountsAsync(null)` walks every group from (2). **Not** a live-attach proof of census. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | File **135/135**. Only outbound MsgType is `(35, "A")` at L96. Grep of this file for `35=D` / `(35, "D")` / `NewOrderSingle` / `Build("D")` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Cannot prove. Opposite is on disk: DI L41 binds env; lab `.env` L73 is `true`; logon host **does not** re-pin false; `/api/settings` echoes `runtime.RealCopyEnabled`. |

**Slot verdict: `FAIL`.**

Claim 5 is **disproved**, not merely unproved. Four of five assigned claims hold from source. One FAIL is enough.

**Risk to capital: `NONE` (`SAFE_BY_ABSENCE`).** The armed flag is **not** a ticket. Hosted copy hop still has no `35=D` builder (`CTraderFixSession` is Logon-only), `CopyTradingService.NewOrderSingleImplemented = false`, persist `AllowFixSend = false`. This slot sent **0** NewOrderSingle.

---

## 1. Claim 1 — DemoSeeder is not the API startup path — **PASS**

Assigned host: `D:\Prop\apps\api\Program.cs` (160 lines, full read).

Startup after `app.Build()`:

```152:159:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}

app.Run();
```

| Check | Measured |
|---|---|
| `DemoSeeder` token in `apps/api/Program.cs` | **0** |
| `DemoSeeder` token under `D:\Prop\apps` | **0** |
| Seed call on API boot | `BrokerCatalogSeed.EnsureAsync` only (L156) |
| `using TraderIntelligence.Infrastructure.Seeding` | present (L7) **for** `BrokerCatalogSeed`, not `DemoSeeder` |
| `launchSettings.json` | no seeder / dummy / FakeMt5 keys |
| DI startup connectors | `LiveMt5Registration.CreateConnectors` → `NativeMt5BrokerConnector` ×2; throws if passwords missing (`DependencyInjection.cs` L36–37) |

`DemoSeeder` **still exists** at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Product C# callers of `DemoSeeder.SeedAsync` this slot:

- `D:\Prop\tests\Integration\SeedingAndStoreTests.cs` L25 (tests)
- leftover `_tmp_*` eval trees under `reports/swarm/20260818/` (not hosts)

That is **not** the API startup path. Older notes that said `Program.cs` still calls `DemoSeeder.SeedAsync` (A002 / A005 / A010) are **stale vs this file**.

Same seed shape on workers (not the assigned API claim, recorded so nobody confuses them):

| Host | Startup seed |
|---|---|
| `apps/mt5-worker/Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` |
| `apps/fix-worker/Program.cs` L15 | `BrokerCatalogSeed.EnsureAsync` |

Residual (does **not** fail claim 1): `apps/mt5-worker/Worker.cs` L31 still scores `{10001, 10002, 10003, 99001}` after a live `SyncBrokerAsync`. That is a leftover dummy **score set**, not API `DemoSeeder` startup.

---

## 2. Claim 2 — Native can list all groups via GroupRequestArray or GroupTotal — **PASS** (capability)

Assigned file: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`.

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

Vendor symbols (same names the C# wrapper calls):

- `IMTManagerAPI::GroupTotal` — `MT5APIManager.h` L205
- `IMTManagerAPI::GroupRequestArray(LPCWSTR mask, IMTConGroupArray* groups)` — L212

Live ingest uses this walk, not a plan-group filter:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

### Adversarial limits (do not over-claim)

| Limit | Why it does not flip this claim to FAIL |
|---|---|
| `"all groups"` = manager-visible under mask `"*"`, not every group on the trade server if ACL hides some | That is what `GroupRequestArray("*")` is specified to return. |
| `GroupTotal`/`GroupNext` is the **pump cache** fallback, only if the request list is empty | Claim is **or**. Both APIs are present. |
| Empty request **and** empty cache → empty list, no throw | Capability still exists; this slot did not attach, so live non-empty is **unproved** (not a source FAIL). |
| A001 (“zero `GroupRequestArray` under `src`”) | **Stale.** L155 is on disk now. |

This slot **did not** Connect. File proves the enumerator. File does **not** prove today’s Achiever/Starwave counts.

---

## 3. Claim 3 — all traders via UserRequestArray / UserLogins — **PASS** (capability)

Same connector. Catalog of **all** groups (claim 2) feeds per-group user pull:

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

Vendor:

- `UserLogins(LPCWSTR group, uint64_t*& logins, uint32_t& logins_total)` — `MT5APIManager.h` L254
- `UserRequestArray(LPCWSTR group, IMTUserArray* users)` — L410

Ingest `GetAccountsAsync(null)` (DealIngestionService L49 / L62) therefore walks **every group name returned by `GetGroupsCore`**, request-first.

### Adversarial limits

| Limit | Notes |
|---|---|
| Incomplete group list ⇒ incomplete trader list | Depends on claim 2 succeeding at runtime. |
| `UserGetByGroup` on hard request fail is **pump-cache**, not the assigned pair | Assigned pair is still the **primary** (`UserRequestArray`) and the **empty-array** fallback (`UserLogins`). Extra cache path does not remove the request path. |
| Dedup is `byLogin` | Same login in two groups is one DTO. That is still “all traders,” not a silent drop of distinct logins. |
| Live 18/8460 | **Not proved this slot.** Do not treat prior probe JSON as this verifier’s measurement. |

---

## 4. Claim 4 — CTraderFixSession has no 35=D — **PASS**

Assigned file: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` — **135/135 physical lines, full read**.

Only outbound builder:

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

| Search in `CTraderFixSession.cs` | Hits |
|---|---:|
| `35=D` | **0** |
| `(35, "D")` | **0** |
| `NewOrderSingle` | **0** |
| `Build("D")` | **0** |
| Outbound MsgType literals | **one:** `(35, "A")` L96 |
| `WriteAsync` | **1** (L49, the Logon frame) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; dispose after one `ReadAsync` |

`Extract(reply, "35")` at L55 reads the **inbound** type. A server reply of `D` would be classified as “Logon rejected,” not sent.

API hosted caller is `CTraderFixLogonHostedService` L48 / L54 — `TryLogonAsync` only (QUOTE 5211, TRADE 5212).

### Residual (does **not** fail this claim)

Claim is **`CTraderFixSession`**, not the whole `Fix.CTrader` folder.

Sibling `CTraderFixDemoTestTrade.cs` **does** `Build("D")` at L139 / L163 / L197. Only `tools/DemoFixTestTrade/Program.cs` L44 calls `SendAsync`. Gate at L43–47 refuses `live-*` / `live.` / account `1369850`. That helper is **not** referenced by API DI, workers, or `CTraderFixSession`. Product-wide “`35=D` = 0” is therefore **false**; the assigned class still has **zero**.

`CTraderFixDemoMatrix.cs` L87 also `Build("D")` — matrix helper, not the assigned session.

---

## 5. Claim 5 — REAL_COPY_EXECUTION stays false — **FAIL**

The assigned wording is the **flag stays false**, not “send stays impossible.”

### What the files actually do

DI **binds** the env token onto process runtime (no hard-false pin):

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

API host **loads** `D:\Prop\.env` before that binder:

```10:15:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
// ...
builder.Services.AddTraderIntelligence(builder.Configuration);
```

`EnvFile.FindAndLoad` (`D:\Prop\src\Mt5\Env\EnvFile.cs` L5–20) includes the literal candidate `D:\Prop\.env` and `Environment.SetEnvironmentVariable` for every `KEY=value` line. `builder.Configuration.AddEnvironmentVariables()` (Program.cs L13) then surfaces those keys to DI.

Lab env, **flag keys only** (no secrets):

| File | Line | Token |
|---|---:|---|
| `D:\Prop\.env` | 73 | `REAL_COPY_EXECUTION_ENABLED=true` |
| `D:\Prop\.env` | 106 | `FEATURE_COPY_TRADING_ENABLED=true` |

`/api/settings` does **not** hardcode false. It echoes the bound runtime:

```71:78:D:\Prop\apps\api\Program.cs
app.MapGet("/api/settings", (LiveRuntimeStatus runtime) => Results.Ok(new
{
    // ...
    featureFlags = new Dictionary<string, bool>
    {
        ["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled,
        ["FEATURE_COPY_TRADING_ENABLED"] = true
    },
```

`CTraderFixLogonHostedService` **logs** `_runtime.RealCopyEnabled` (L69–70) and **never assigns it false**. Prior “hosted re-pin false” notes are **stale vs this file**.

`CTraderFixOptions.RealCopyExecutionEnabled` still **defaults** `false` (`CTraderFixOptions.cs` L35). That POCO is **not** what `/api/settings` or `CopyTradingService` read. `CopyTradingService` uses `_runtime.RealCopyEnabled` (L44, L190, L316). `fix-worker/Worker.cs` L21 reads a **different** key `CTrader:RealCopyExecutionEnabled` with default `false` (log-only; still no send).

### Why this is FAIL, not “PASS with residual”

| Surface | Stays false? |
|---|---|
| Architecture / docs (`docs/architecture.md`, README) | Policy says false — **not a runtime pin** |
| `CTraderFixOptions` POCO default | Yes, default false — **unread by API runtime flag** |
| `LiveRuntimeStatus.RealCopyEnabled` on API host with this `.env` | **No — becomes true** |
| `/api/settings` `featureFlags.REAL_COPY_EXECUTION_ENABLED` | **No — follows runtime** |
| `CopyTradingService` “REAL_COPY armed” | **No — follows runtime** |
| Committed `apps/api/appsettings.json` | Has `FeatureFlags.LiveCopyEnabled=false` — **different name**, unused by DI L41 |

`CREDENTIALS_AND_COPY_STATUS.md` “forced false” and E038 “hardcoded settings false” are **stale**. Cannot prove claim 5 from any live-path file. The live-path files prove the opposite for this lab host.

**Do not confuse with send safety.** Execution is still off by **absence of a builder** (claim 4 + `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`). That is a different claim. The assigned sentence is the flag.

---

## 6. Copy hop (context — not a sixth assigned claim)

```16:18:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

```211:223:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
                    AllowFixSend = false,
                    DecidedAt = now
                };
                // ...
                if (decision.AllowFixSend && score.CurrentState == TraderState.LIVE && NewOrderSingleImplemented && VenueReconciled)
                {
                    intent.Status = "LIVE_SEND_BLOCKED_UNIMPLEMENTED";
                }
                else
                {
                    intent.Status = "SHADOW_ONLY";
```

Even if `decision.AllowFixSend` were true, persist **forces** `AllowFixSend = false`. There is no `ExecutionIntent` writer on this path.

---

## 7. What this slot did **not** prove

- Live Manager Connect / group+trader counts (no attach).
- Loopback `GET /api/settings` body (not fetched).
- §68 / §70 go-live lists.
- That `CTraderFixDemoTestTrade` cannot send if someone runs `tools/DemoFixTestTrade` against a **demo** host (it is written to try `35=D` there). That is off the API copy hop.

---

## 8. Stale siblings (so the next agent does not copy them)

| Older claim | This slot |
|---|---|
| A002 / A005 / A010: API startup still `DemoSeeder` | **Stale.** Catalog seed only. |
| A001: zero `GroupRequestArray` / `UserRequestArray` under `src` | **Stale.** L155 / L223. |
| A014 / E038: `/api/settings` hardcodes REAL_COPY false; DI pins false | **Stale.** DI binds env. |
| W500_68 / W500_108: flag pinned false in DI + hosted + `.env` | **Stale.** `.env` true; DI binds; hosted no re-pin. |
| “Product `35=D` = 0 everywhere” | **Stale** if applied to `Fix.CTrader` as a folder (`CTraderFixDemoTestTrade` / matrix). **True** for `CTraderFixSession.cs`. |

---

## 9. Bottom line

| Item | Value |
|---|---|
| Claims 1–4 | **PASS** from files this slot |
| Claim 5 | **FAIL** — `REAL_COPY_EXECUTION_ENABLED` is env-bound and lab `.env` L73 is `true` |
| Slot verdict | **FAIL** |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE` of NewOrderSingle on the hosted hop) |
| Product edited | **No** |
