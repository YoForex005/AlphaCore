# W500_VERIFY_9 — Adversarial live-path verify (slot 9)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_9.md` |
| Agent / slot | W500 adversarial verifier **9** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live `src/`, `apps/`, `tests/`, `tools/`, lab `.env`) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Flag state is from `read_file` of source + `.env` boolean line. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), both worker `Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CopyTradingService.cs` (gates), `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` (header), `LiveMt5Registration.cs`, `EnvFile.cs`, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/appsettings.json`, `apps/api/Controllers/SettingsController.cs`, `CTraderFixDemoTestTrade.cs` (header + live-identity refuse), `tools/DemoFixTestTrade/Program.cs` (header). Targeted `grep` of `apps/` + product `*.cs` for `DemoSeeder`, `GroupRequestArray`/`GroupTotal`, `UserRequestArray`/`UserLogins`, `35=D`/`NewOrderSingle`, `REAL_COPY_EXECUTION`. Did **not** trust A001/A002/A014/W500_68/W500_108. |
| Binding rule | **FAIL the slot if any assigned claim cannot be proven from the live file.** Sibling reports are not evidence. |

**Honesty:** “can list” is a **source capability** claim, not a re-measured live census. This slot did **not** attach. Prior 18/8460 is cited only as prior measure, not re-proven here. `SAFE_BY_ABSENCE` is **not** “flag stays false.” An armed env bit with no sender is still an armed bit.

---

## 0. Verdict (binding)

**FAIL.** Four of five assigned claims are proven from the live files. Claim **(5) `REAL_COPY_EXECUTION` stays false** is **not** true on disk and therefore **cannot** be scored PASS.

| # | Assigned claim | Measured | Class |
|---|---|---|---|
| 1 | `DemoSeeder` is not the API startup path | **PROVEN** | PASS |
| 2 | Native connector can list all groups via `GroupRequestArray` **or** `GroupTotal` | **PROVEN** (source walk) | PASS |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PROVEN** (source walk) | PASS |
| 4 | `CTraderFixSession` has no `35=D` | **PROVEN** (135/135) | PASS |
| 5 | `REAL_COPY_EXECUTION` stays false | **DISPROVEN** | **FAIL** |

One-line:

```text
FAIL slot 9: DemoSeeder not on API boot; Native GroupRequestArray(*) then GroupTotal; UserRequestArray then UserLogins; CTraderFixSession 35=A only. REAL_COPY does NOT stay false — .env L73=true and DI L41 binds it; logon host no longer re-pins. Copy hop still SAFE_BY_ABSENCE (no NOS). Risk to capital NONE.
```

Do **not** treat this FAIL as a license to send. Do **not** flip the leftover `true` into a go-live. Operator should restore `.env` L73 to `false` (this slot did **not** edit it).

---

## 1. Claim 1 — DemoSeeder is not the API startup path — PASS

### 1.1 API host (full file)

`D:\Prop\apps\api\Program.cs` is **160** lines. Startup seed after `EnsureCreatedAsync` is **only** `BrokerCatalogSeed.EnsureAsync`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

Zero tokens in this file: `DemoSeeder`, `FakeMt5`, `10001`, `10002`, `dummy`. The `using TraderIntelligence.Infrastructure.Seeding;` exists for `BrokerCatalogSeed`.

`grep DemoSeeder` under `D:\Prop\apps` = **0**. Worker hosts match the same seed:

- `D:\Prop\apps\fix-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`
- `D:\Prop\apps\mt5-worker\Program.cs` L15 `BrokerCatalogSeed.EnsureAsync`

### 1.2 File still exists — not the host path

`D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` L14 is still `public static class DemoSeeder`. Product C# callers of `DemoSeeder.SeedAsync` are:

| Path | Role |
|---|---|
| `tests/Integration/SeedingAndStoreTests.cs` L25 | test fixture |
| `reports/swarm/20260818/_tmp_*` | throwaway eval trees |

**API process does not call it.** A001/A002 (“API still seeds FakeMt5 10001”) are **stale**.

Residual (does **not** revive DemoSeeder as API startup): `apps/mt5-worker/Worker.cs` L31–35 still scores `{10001,10002,10003,99001}` after a real `SyncBrokerAsync`. Hosted ingest (`LiveIngestHostedService`) scores `ListLoginsWithDealsAsync`, not those four.

DI fail-closes dummy brokers: `DependencyInjection.cs` L36–37 throws unless both real MT5 passwords pass `IsSecret`; then `LiveMt5Registration.CreateConnectors` returns **Native ×2** only.

---

## 2. Claim 2 — Native can list all groups via GroupRequestArray or GroupTotal — PASS

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore` (L144–187):

1. **Primary:** `GroupRequestArray("*", arr)` (L155). On `MT_RET_OK` / `MT_RET_OK_NONE`, walk `arr.Next`.
2. **Fallback only if `list.Count == 0`:** `GroupTotal()` + `GroupNext` cache walk (L174–180).

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
```

Live ingest uses this walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` then L48 `GetAccountsAsync(null, ct)`. Hosted `LiveIngestHostedService` L56 calls that catalog. Manual `/api/ops/resync` L129 does the same.

A001 (“zero hits for `GroupRequestArray` under `src`”) is **stale**.

**Not proven this slot:** a fresh Manager attach returning 18 groups. Capability is on disk. Completeness of a live ACL is **prior** (Achiever 8 + Starwave 10) and was **not** re-probed.

Adversarial caveat (does not fail the assigned wording): if `GroupRequestArray` returns OK with a **non-empty subset**, the cache walk is skipped. The assigned claim is “can list via GroupRequestArray **or** GroupTotal,” which the file implements.

---

## 3. Claim 3 — All traders via UserRequestArray / UserLogins — PASS

`GetAccountsCore` (L189–213): if `group` is null/empty, it walks **every** name from `GetGroupsCore()`, then `ReadAccountsForGroup`.

`ReadAccountsForGroup` (L216–271):

1. **Primary:** `UserRequestArray(gname, users)` L223.
2. Hard-fail only: `UserGetByGroup` (pump cache) L226.
3. If `users.Total() == 0`: `UserLogins(gname, out loginRes)` L230 then `UserRequestByLogins` L232.

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

Ingest/probe path is `GetAccountsAsync(null)` = all groups × this user walk. A001 (“C# has neither”) is **stale**.

**Not proven this slot:** live 8460 login arithmetic. Source path for ALL manager-visible users is present.

---

## 4. Claim 4 — CTraderFixSession has no 35=D — PASS

Full `read_file` of `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

| Token in this file | Count |
|---|---|
| `(35, "A")` | **1** (`BuildLogon` L96) |
| `(35, "D")` / `35=D` / `NewOrderSingle` | **0** |
| `WriteAsync` | **1** (L49, the Logon frame) |
| Socket lifetime | `using TcpClient` + `await using SslStream`; disposed after one `ReadAsync` |

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

Hosted caller `CTraderFixLogonHostedService` L48–58 invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No other `CTraderFixSession` method exists.

**Sibling, not this class:** `CTraderFixDemoTestTrade.Build("D")` at L139/L163/L197. Called from `tools/DemoFixTestTrade` only (`grep` of `apps/` = 0). Live identity refused (`live-*` / `live.` / account `1369850`). That does **not** put `35=D` into `CTraderFixSession`. W500_130/150 “product `35=D=0` everywhere” is **stale** as a product-wide claim; the **assigned class** is still `35=A` only.

---

## 5. Claim 5 — REAL_COPY_EXECUTION stays false — FAIL

The assigned wording is the **flag**, not “no ticket can be sent.”

### 5.1 What the live files actually do

| Surface | Measured | Stays false? |
|---|---|---|
| Architecture / README / docs | `REAL_COPY_EXECUTION_ENABLED=false` (policy text) | policy only |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default **`false`** | default; **unbound** (no `Configure<CTraderFixOptions>`) |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | **false** | **different name**; unused by DI |
| Dead `SettingsController` `LiveCopyEnabled` | `GetValue(..., false)` | **not mapped** (`Program.cs` has **0** `MapControllers`) |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **NO** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | shadow wish; API ignores the key |
| API boot | `EnvFile.FindAndLoad()` L10 (includes `D:\Prop\.env`) then `AddEnvironmentVariables()` L13 | process **loads** L73 |
| DI L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **binds env** |
| `CTraderFixLogonHostedService` | logs `RealCopyArmed={Armed}` L68–70; **never** assigns `false` | re-pin **gone** |
| `/api/settings` L76 | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | **follows env** |
| `/api/health` L55 | `realCopyEnabled = runtime.RealCopyEnabled` | **follows env** |
| `CopyTradingService.GetStatusAsync` L44 | `RealCopyArmed: _runtime.RealCopyEnabled` | **follows env** |
| `CopyTradingService.BuildBlockers` L316–317 | adds “is false” **only if** runtime is false | armed ⇒ that blocker **absent** |
| fix-worker L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | nested key; **log-only**; default false unless `CTrader__*` set |

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

```73:73:D:\Prop\.env
REAL_COPY_EXECUTION_ENABLED=true
```

W500_68 / W500_108 / CREDENTIALS_AND_COPY_STATUS “forced false” / A014 “DI pins false” are **stale**.

### 5.2 What still blocks a ticket (not claim 5)

These prove **no live send today**. They do **not** prove the flag stays false.

- `CopyTradingService.NewOrderSingleImplemented = false` (L17 const)
- `VenueReconciled = false` (L16 const)
- persist `AllowFixSend = false` hardcoded (L211)
- LIVE send branch is dead (`NewOrderSingleImplemented` is const false) (L217)
- `CTraderFixSession` outbound is `35=A` only (claim 4)
- 0 `ExecutionIntent` writers on the copy hop
- `BaselineScorer.CanPromoteToLive` is false (not re-read this slot; cited as residual, not a claim-5 proof)

Risk to capital from this process: **NONE** (`SAFE_BY_ABSENCE`). Next person who adds a sender will see `RealCopyEnabled=true` on the API host.

---

## 6. Stale siblings (do not reuse)

| File | Stale claim vs this re-read |
|---|---|
| A001 | No `GroupRequestArray` / `UserRequestArray` in C# |
| A002 / A005 | API startup still `DemoSeeder` / FakeMt5 10001 |
| A014 L270 | DI pins `RealCopyEnabled` false |
| W500_68 / W500_108 / CREDENTIALS | flag forced false in DI + hosted + `.env` |
| W500_130 / W500_150 | product-wide `35=D=0` (demo helper now exists off-hop) |

---

## 7. Risk to capital

**NONE** on the copy hop. Native catalog is Manager **GET**. Hosted FIX writes one Logon `35=A` and disposes. No NewOrderSingle assembler on `CTraderFixSession`. Persist `AllowFixSend=false`. Demo `Build("D")` is tools-only and refuses the live account.

The FAIL is **honesty about the flag**, not a live ticket.

---

## 8. Checklist

- [x] DemoSeeder not on API / worker `Program.cs` startup
- [x] Native `GroupRequestArray("*")` then `GroupTotal`/`GroupNext`
- [x] Native `UserRequestArray` then `UserLogins`
- [x] `CTraderFixSession` 135/135: no `35=D`
- [x] `REAL_COPY_EXECUTION_ENABLED` **does not stay false** (`.env` true + DI bind)
- [x] No secrets printed
- [x] Product source not edited
