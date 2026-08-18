# W500_VERIFY_24 — Adversarial live-path verifier (slot 24)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_24.md` |
| Agent / slot | W500 **VERIFY 24** (adversarial; do not trust other agents) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product tree (`apps/`, `src/`, `tools/`, `tests/`, `.env` booleans only) |
| Product source modified | **No** |
| Test source modified | **No** |
| `.env` modified | **No** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` (`.env` L73) and `FEATURE_COPY_TRADING_ENABLED=true` (`.env` L106). No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Census **not** re-probed. |
| Method | Independent `read_file` of API/worker `Program.cs`, `DependencyInjection.cs`, `LiveMt5Registration.cs`, `NativeMt5BrokerConnector.cs` (full), `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `LiveRuntimeStatus.cs`, `RiskEngine.cs` (allowSend), `BrokerCatalogSeed.cs`, `EnvFile.cs`, `SettingsController.cs`, `appsettings.json`, `LiveBrokerProbe/Program.cs`, vendor `MT5APIManager.h` (Group*/User*). Targeted `grep` of `apps/` + `src/` for `DemoSeeder`, `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `35=D`, `NewOrderSingle`, `RealCopyEnabled =`. `.env` grepped for flag **keys only**. |
| Binding rule | **FAIL the slot if any assigned claim cannot be proven from the live file.** Prior swarm notes are not evidence. |

**Honesty rule:** “can list ALL” in source is **capability**, not a re-measured census. A default `= false` is not a pin. A dashboard bit is display. `AllowFixSend` persist `false` is a write, not the env flag. Absence of copy-path `35=D` is **`SAFE_BY_ABSENCE`**, not a §68/§70 PASS. Sibling `CTraderFixDemoTestTrade.Build("D")` is **not** `CTraderFixSession`. Do not print secrets.

---

## 0. Verdict (binding)

**FAIL** — claim 5 is **disproven** from the live files. Claims 1–4 are proven from source.

| # | Assigned claim | Result | Why |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Zero `DemoSeeder` tokens under `D:\Prop\apps`. |
| 2 | Native connector can list **all groups** via `GroupRequestArray` **or** `GroupTotal` | **PASS** (capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty-list fallback L174 `GroupTotal` + `GroupNext`. SDK `IMTManagerAPI` h:212 / h:205. |
| 3 | **All traders** via `UserRequestArray` / `UserLogins` | **PASS** (capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; L230 `UserLogins` when array empty. `GetAccountsAsync(null)` walks every group. Ingest L48 same. |
| 4 | `CTraderFixSession` has **no** `35=D` | **PASS** | File 135/135. Grep of that file for `35=D` / `NewOrderSingle` / `Build("D")` = **0**. Only outbound MsgType is `(35, "A")` L96. One `WriteAsync` L49. |
| 5 | `REAL_COPY_EXECUTION` **stays false** | **FAIL** | Lab `.env` L73 is `true`. `EnvFile.FindAndLoad` + `AddEnvironmentVariables` + `DependencyInjection.cs` L41 bind it onto `LiveRuntimeStatus.RealCopyEnabled`. **Only** C# assignment of that property. Hosted logon **does not** re-pin false. |

One-line:

```text
FAIL. DemoSeeder off API boot (BrokerCatalogSeed). Native ALL-groups GroupRequestArray("*")/GroupTotal. ALL-traders UserRequestArray/UserLogins. CTraderFixSession 35=A only. REAL_COPY_EXECUTION does NOT stay false: .env L73=true and DI binds it. Live send still SAFE_BY_ABSENCE (no 35=D builder). Slot did not attach.
```

Risk to capital: **NONE** (`SAFE_BY_ABSENCE` on the copy hop). The armed flag cannot emit a ticket today. It **does** mean the next sender would see runtime armed. Operator leftover is a policy violation, not a send.

---

## 1. DemoSeeder is not the API startup path — PASS

Read `D:\Prop\apps\api\Program.cs` (160 lines) in full this slot.

Startup after `app.Build()`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- `using TraderIntelligence.Infrastructure.Seeding;` exists solely so `BrokerCatalogSeed` resolves.
- Token search `DemoSeeder|SeedAsync` under `D:\Prop\apps` hits **only** `BrokerCatalogSeed.EnsureAsync` in:
  - `apps/api/Program.cs` L156
  - `apps/mt5-worker/Program.cs` L15
  - `apps/fix-worker/Program.cs` L15
- `DemoSeeder.SeedAsync` is **not** on any host `Program.cs`.
- Class still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder` L14). Callers this tree: `tests/Integration/SeedingAndStoreTests.cs` + `_tmp_*` eval programs. **Tests / scratch, not API boot.**
- DI `AddTraderIntelligence` throws unless both real MT5 passwords pass `IsSecret`; then registers **Native ×2** only (`LiveMt5Registration.CreateConnectors`). No Fake substitution on the throw path.

**Residual (does not revive DemoSeeder as API seed):** `apps/mt5-worker/Worker.cs` L31–35 still scores hardcoded `{10001,10002,10003,99001}` after live `SyncBrokerAsync`. Hosted ingest scores `ListLoginsWithDealsAsync` only. That is a leftover scorer set, not the API seed path.

A002 / A005 / A011 that say API still calls `DemoSeeder` are **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS (capability)

Read `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` in full this slot.

```144:186:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

Vendor surface (not guessed):

```205:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  GroupTotal(void)=0;
   ...
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

Live consumers:

- `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`.
- `GetAccountsCore(null)` L201–202 walks `GetGroupsCore()` then every name.
- `LiveBrokerProbe` L25–26 same pair.
- Connect tries pump `GROUPS|USERS|POSITIONS` then `PUMP_MODE_NONE` (L89–111). Fetch is **not** gated on `_pumpEnabled`.

**Caveats (not a FAIL of the assigned claim):**

- This slot did **not** re-attach. Prior census 8/6512 + 10/1948 = 18/8460 is **not** re-proven here.
- If `GroupRequestArray` returns OK with a **non-empty partial** list, the cache walk does not run. Completeness then depends on the Manager RPC, not `GroupTotal`.
- A001 “zero `GroupRequestArray` under `src`” is **stale**.

Assigned wording is **or**. Both APIs are in the live connector. Claim proven as capability.

---

## 3. All traders via UserRequestArray / UserLogins — PASS (capability)

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

`GetAccountsCore(null)` (L189–213) collects **every** group from `GetGroupsCore()` then `ReadAccountsForGroup` per name, de-duped by login.

Vendor:

- `UserLogins` — `MT5APIManager.h` L254
- `UserRequestArray` — `MT5APIManager.h` L410

Ingest: `GetAccountsAsync(null)` at `DealIngestionService.cs` L48 and L62. Probe: `LiveBrokerProbe/Program.cs` L26.

**Caveats:**

- `UserLogins` runs only when `users.Total() == 0`. A successful **partial** `UserRequestArray` would not fall through.
- `UserGetByGroup` is pump-cache, used only on hard fail of the request API.
- Hosted **scoring** is deals-only (`ListLoginsWithDealsAsync`). Catalog persist is still all accounts. Do not confuse scored count with trader census.
- This slot did not re-probe 8460.

Claim proven as capability: ALL-traders walk is request-first `UserRequestArray`, then `UserLogins`.

---

## 4. CTraderFixSession has no 35=D — PASS

Read `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` **135/135**.

Grep confined to that file (`35=D`, `NewOrderSingle`, `(35, "D")`, `Build("D")`): **0 hits**.

Outbound construction:

```89:109:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
    private static string BuildLogon(...)
    {
        ...
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

- One `ssl.WriteAsync` (L49). One `BuildLogon`. Sockets/`SslStream` in `using` / `await using`.
- Inbound parse of tag 35 is receive-only (L55–73). A rejected logon string `35={msgType}` is **not** a send of `D`.
- Hosted caller `CTraderFixLogonHostedService` calls `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212) and **never** sends an order.

**Sibling residual (out of claim):** `CTraderFixDemoTestTrade` / `CTraderFixDemoMatrix` assemble `Build("D")`. Demo-gated (refuse `live-*` / `live.*` / account `1369850`). Caller is `tools/DemoFixTestTrade`, not copy / DI / API / this type. Claim is **`CTraderFixSession` has no 35=D`** — proven.

Copy hop still `CopyTradingService.NewOrderSingleImplemented = false` (L17) and persist `AllowFixSend = false` (L211). Hosted copy tick is `GenerateShadowIntentsAsync` only (`CopyTradingHostedService.cs` L28).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL

Cannot prove the flag stays false. The live files prove the opposite.

### 5.1 What the files actually do

| Surface | Measured | Stays false? |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | default only; **no** `Configure<>` bind |
| `fix-worker` `GetValue("CTrader:RealCopyExecutionEnabled", false)` | fallback false; log-only | different key; **not** the §41 token |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **different name**; unused by DI |
| Unwired `SettingsController` `LiveCopyEnabled` | default false | **not mapped** (`Program.cs` has no `MapControllers`) |
| Architecture / README / docs | `=false` | policy text, not runtime |
| `reports/CREDENTIALS_AND_COPY_STATUS.md` L30 | “**false (forced)**” | **STALE** vs DI L41 |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **armed** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | shadow wish; API ignores this key (hardcodes `true` at L77) |
| API load | `EnvFile.FindAndLoad()` L10 then `AddEnvironmentVariables()` L13 | process **sees** L73 |
| `DependencyInjection.cs` L39–42 | `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **bound, not pinned** |
| Other `RealCopyEnabled =` in `*.cs` | **exactly 1** (that DI line) | no later force-false |
| `CTraderFixLogonHostedService` | logs `RealCopyArmed={Armed}` L68–70; **does not assign** `false` | re-pin **gone** |
| `/api/settings` L76 | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | display follows env |
| `/api/health` L55 | `realCopyEnabled = runtime.RealCopyEnabled` | same |
| `CopyTradingService` L190 | `RealExecutionEnabled = _runtime.RealCopyEnabled` | RiskEngine **sees armed** |
| `RiskEngine` L147–150 | `allowSend = request.RealExecutionEnabled && …` | engine can compute true |
| Persist | `AllowFixSend = false` hardcoded L211 | write forced off; **not** the env flag |

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();
...
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile` includes `D:\Prop\.env` as a candidate (L15).

### 5.2 Why this is FAIL, not “must stay false / SAFE”

Assigned claim: **`REAL_COPY_EXECUTION stays false`**.

That is a **runtime-flag** claim. It is not “must stay false as policy.” It is not “NewOrderSingle stays unimplemented.” It is not “no 35=D on the wire.”

From files:

1. Operator leftover is already `true`.
2. API **loads** that leftover.
3. DI **copies** it onto the singleton used by health, settings, copy, and risk.
4. Nothing in product C# writes `RealCopyEnabled = false` after that.

Therefore the claim **cannot be proven** and is **disproven** on this machine if the API starts with `D:\Prop\.env`. Verdict **FAIL**.

W500_68 / W500_108 / CREDENTIALS “forced false” / A014 “DI pins false” / E038 “hardcoded false” are **stale**.

### 5.3 What still blocks a ticket (does not rescue claim 5)

- `CTraderFixSession` has no `35=D` builder (§4).
- `NewOrderSingleImplemented` const `false`; `VenueReconciled` const `false`.
- Persist `AllowFixSend = false` regardless of `RiskEngine.AllowFixSend`.
- 0 product `ExecutionIntent` writers found on this pass (not re-scored exhaustively beyond copy service).
- `CTraderFixOptions.RealCopyExecutionEnabled` unused by the hosted session.

Copy hop = **`SAFE_BY_ABSENCE`**. Flag = **armed**. Those are different facts. Adversarial verifier will not fold them.

---

## 6. Stale reports this slot contradicts

| Report / claim | Live file | Status |
|---|---|---|
| A002 / A005 / A011 — API `DemoSeeder` on startup | `apps/api/Program.cs` L156 `BrokerCatalogSeed` | **STALE** |
| A001 — 0 `GroupRequestArray` / 0 `UserRequestArray` under `src` | `NativeMt5BrokerConnector.cs` L155 / L223 | **STALE** |
| A014 / E038 / CREDENTIALS — `REAL_COPY` forced / hardcoded false | DI L41 + `.env` L73 `true` | **STALE** |
| W500_68 / W500_108 — DI + hosted + `.env` pinned false | hosted no re-pin; env true; DI binds | **STALE** |
| “product 35=D=0 / only writer is CTraderFixSession” | sibling `CTraderFixDemoTestTrade.Build("D")` | **half-stale** (session itself still clean) |

---

## 7. What this slot did **not** prove

- Did not live-attach Manager. Did not re-sum 18/8460.
- Did not open FIX TLS. Did not send `35=A` or `35=D`.
- Did not flip `.env` L73 back to `false` (operator should; this slot must not).
- Did not score Architecture §68 / §70 checkboxes (prior 0/19 and 0/14 still the last written scorecards; not re-audited item-by-item).
- Did not invoke `tools/DemoFixTestTrade`.

---

## 8. Risk to capital

**NONE** from this process as compiled:

- Native path is Manager **read** (groups / users / deals / positions).
- Copy hosted service writes SHADOW intents only.
- TRADE session type cannot assemble NewOrderSingle.
- Persist forbids `AllowFixSend`.

Residual: if a sender is added later **without** forcing the flag false, runtime is already armed. That is why claim 5 FAILs even though capital is not at risk today.

Do **not** treat this FAIL as a license to send. Do **not** add copy-path `35=D`. Do **not** wire the demo helper into DI.

---

## 9. Files read (absolute)

- `D:\Prop\apps\api\Program.cs`
- `D:\Prop\apps\api\Controllers\SettingsController.cs` (unwired leftover)
- `D:\Prop\apps\api\appsettings.json`
- `D:\Prop\apps\mt5-worker\Program.cs`
- `D:\Prop\apps\mt5-worker\Worker.cs`
- `D:\Prop\apps\fix-worker\Program.cs`
- `D:\Prop\apps\fix-worker\Worker.cs`
- `D:\Prop\tools\LiveBrokerProbe\Program.cs`
- `D:\Prop\src\Infrastructure\DependencyInjection.cs`
- `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs`
- `D:\Prop\src\Infrastructure\Seeding\BrokerCatalogSeed.cs`
- `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs`
- `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs`
- `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs`
- `D:\Prop\src\Application\Ingestion\DealIngestionService.cs`
- `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs`
- `D:\Prop\src\Application\Ingestion` via `DealIngestionService.cs`
- `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`
- `D:\Prop\src\Mt5\Env\EnvFile.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs`
- `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixDemoTestTrade.cs` (header + live-identity refuse only)
- `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs`
- `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs`
- `D:\Prop\src\Domain\Risk\RiskEngine.cs` (allowSend)
- `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (L205–212, L254, L410)
- `D:\Prop\.env` — flag **keys** L73 / L106 only
