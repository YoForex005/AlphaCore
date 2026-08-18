# W500_VERIFY_0 — Adversarial live-path verify (slot 0)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_0.md` |
| Agent / slot | W500 verify **0** (adversarial; do not trust siblings) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted as boolean only. |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Claims proven or failed from **on-disk product files** only. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), both worker `Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `LiveMt5Registration.cs`, `EnvFile.cs`, `BrokerCatalogSeed.cs` header, `CTraderFixDemoTestTrade.cs` (gate + `Build("D")` only), `apps/api/appsettings.json`. Targeted `grep` of `apps/**/*.cs` for `DemoSeeder` / `BrokerCatalogSeed`; of `src/Fix.CTrader/Sessions/CTraderFixSession.cs` for `(35,`; of product `*.cs` for `REAL_COPY` / `RealCopyEnabled` / `35=D`. `.env` L73/L106 boolean keys only. |

**Honesty rule:** sibling reports are **not** evidence. A comment, log, dashboard bit, or `LastError` string is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A demo-only `Build("D")` helper is **not** `CTraderFixSession`. A POCO `= false` default is **not** a process pin. An env bind that evaluates `true` **disproves** “stays false.” Set **FAIL** if any assigned claim cannot be proven from the file.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1–4** are proven from the live product files. Claim **5** (`REAL_COPY_EXECUTION stays false`) is **not** proven and is **disproven as process state** on the API host: `EnvFile.FindAndLoad()` + DI binds `REAL_COPY_EXECUTION_ENABLED`; lab `.env` L73 is `true`; hosted logon **does not** re-pin false. Instruction: fail the slot if any claim cannot be proven from the file.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Token `DemoSeeder` = **0** in `apps/api/Program.cs` and **0** under `D:\Prop\apps\**\*.cs`. | **PASS** |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | `NativeMt5BrokerConnector.GetGroupsCore` L155 `GroupRequestArray("*", arr)` first; if `list.Count == 0` then L174 `GroupTotal()` + `GroupNext`. Ingest calls `GetGroupsAsync` (`DealIngestionService` L45/L61). | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | `ReadAccountsForGroup` L223 `UserRequestArray(gname, users)`; if `users.Total()==0` then L230 `UserLogins` + `UserRequestByLogins`. `GetAccountsCore(null)` walks every group from claim 2. Ingest uses `GetAccountsAsync(null)` L48/L62. | **PASS** |
| 4 | `CTraderFixSession` has **no** `35=D` | Assigned file 135/135. Only outbound tag 35 is `(35, "A")` at L96. One `WriteAsync` (L49). Grep `(35,` in that file = **1** hit. Literal `35=D` / `NewOrderSingle` = **0**. | **PASS** |
| 5 | `REAL_COPY_EXECUTION` **stays false** | Cannot prove. DI L41 sets `RealCopyEnabled` from env `== "true"`. `.env` L73 is `true`. API L10 loads `.env`. Logon host L68–70 **logs** `RealCopyArmed` and does **not** assign `false`. `/api/settings` L76 exposes `runtime.RealCopyEnabled`. POCO default false (`CTraderFixOptions` L35) is **unbound** and is **not** the API gate. | **FAIL** |

One-line:

```text
FAIL. DemoSeeder not API startup (BrokerCatalogSeed). Native ALL groups GroupRequestArray("*") else GroupTotal; ALL traders UserRequestArray then UserLogins. CTraderFixSession 35=A only. REAL_COPY does not stay false: .env L73=true, DI binds, no logon re-pin. Copy hop still SAFE_BY_ABSENCE (no 35=D sender).
```

---

## 1. DemoSeeder is not the API startup path — PASS

Live API host: `D:\Prop\apps\api\Program.cs` (160 lines, full read).

Startup after `app.Build()`:

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- `using TraderIntelligence.Infrastructure.Seeding;` (L7) exists **for** `BrokerCatalogSeed`, not `DemoSeeder`.
- `grep DemoSeeder` on `apps/api/Program.cs` = **0**.
- `grep DemoSeeder|BrokerCatalogSeed` under `D:\Prop\apps\**\*.cs`:
  - API / mt5-worker / fix-worker `Program.cs` each call **only** `BrokerCatalogSeed.EnsureAsync`.
- DI (`AddTraderIntelligence`) fail-closes unless both MT5 passwords pass `IsSecret`, then registers `LiveMt5Registration.CreateConnectors` → **Native ×2** only (`DependencyInjection.cs` L36–48). No `DemoSeeder`. No `FakeMt5BrokerConnector` on that path.

**Residual (not a fail of this claim):** `DemoSeeder.cs` still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` and is called from `tests/Integration/SeedingAndStoreTests.cs`. That is **not** API/worker startup. Leftover MVC `SettingsController` is unused by the minimal-API `Program.cs` (no `AddControllers` / `MapControllers`). mt5-worker `Worker.cs` L31 still **scores** `{10001,10002,10003,99001}` after a real catalog sync — scoring leftover, not seed-as-startup.

A002 / A005 / A011 claims that API startup still calls `DemoSeeder` are **stale**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458).

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

- Primary: network request `GroupRequestArray("*")` — manager-visible set, pump **not** required.
- Fallback: local cache `GroupTotal` + `GroupNext` only when the request list is empty.
- Live ingest: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync` (no plan-env filter). `GetAccountsCore` L201–202 walks **every** name from `GetGroupsCore()` when `group` is null.

**Not proven this slot:** a live 18-group census. This slot did **not** attach. Capability is file-proven. A001 “zero `GroupRequestArray` under `src`” is **stale**.

**Caveat (does not fail the claim):** if `GroupRequestArray` fails **and** the pump cache is empty (`GroupTotal()==0`), the method returns an empty list with no throw. That is an empty-result hole, not absence of the APIs.

---

## 3. All traders via UserRequestArray / UserLogins — PASS

Same connector, `ReadAccountsForGroup`:

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

- Primary: `UserRequestArray` (network user records).
- Empty array: `UserLogins` then `UserRequestByLogins`.
- Cache `UserGetByGroup` only on **hard fail** of the request (not OK / OK_NONE / NOTFOUND).
- `GetAccountsCore(null)` unions every group from claim 2 → **all manager-visible traders**.
- Ingest: `GetAccountsAsync(null)` at `DealIngestionService` L48 and L62.

**Not proven this slot:** live 8460-login arithmetic. Capability is file-proven. A001 “zero `UserRequestArray` / `UserLogins` under `src`” is **stale**.

---

## 4. CTraderFixSession has no 35=D — PASS

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**).

- Types: `CTraderFixSessionResult` + static `CTraderFixSession`.
- Only outbound MsgType:

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

- One `ssl.WriteAsync` (L49). One `ReadAsync` (L53). `using` disposes `TcpClient` / `SslStream`.
- Grep `(35,` in this file = **1** (`"A"`). Tokens `35=D`, `NewOrderSingle`, `(35, "D")` = **0**.
- Hosted caller `CTraderFixLogonHostedService` L48–58 invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No second writer.

**Residual (does not fail claim 4):** sibling `CTraderFixDemoTestTrade` (`Build("D")` at L139 / L163 / L197) and `CTraderFixDemoMatrix` (`Build("D")` at L93) can assemble MsgType D. They are **not** `CTraderFixSession`. Demo helper is gated (`demo-` host / `demo.` sender; refuse `live-*` / `live.` / account `1369850`) and is invoked from `tools/DemoFixTestTrade`, not API/DI/copy. Claim text is **this class**.

Copy hop still cannot send: `CopyTradingService.NewOrderSingleImplemented = false` (L17); persist `AllowFixSend = false` (L211); `VenueReconciled = false` (L16); 0 `ExecutionIntent` writers (not re-grepped as a sixth claim).

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (cannot prove; process is armed)

Assigned claim is a **state** claim: the flag **stays false**. Files do **not** pin it false.

| Surface | Measured | Why claim 5 fails |
|---|---|---|
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | operator leftover already **true** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | unused by DI; API FEATURE is a **literal** `true` (`Program.cs` L77) |
| API env load | `Program.cs` L10 `EnvFile.FindAndLoad()`; L13 `AddEnvironmentVariables()` | process **imports** L73 |
| `EnvFile` | hard path includes `D:\Prop\.env` | load is deterministic if file exists |
| DI bind | `DependencyInjection.cs` L41 `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **bound, not pinned** |
| Logon re-pin | `CTraderFixLogonHostedService` L68–70 logs `RealCopyArmed={Armed}`; **no** `_runtime.RealCopyEnabled = false` | W500_68/108 pin **stale** |
| API display | `/api/settings` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | will report **true** after L10+L41 |
| `/api/health` | L55 `realCopyEnabled = runtime.RealCopyEnabled` | same |
| POCO | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | default only; **no** `Configure<CTraderFixOptions>` in DI |
| Worker | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; fallback false; **log-only**, stamps TRADE `Disconnected` |
| `appsettings.json` | `FeatureFlags:LiveCopyEnabled=false` | **different name**; not §41 token |
| Copy gate | `BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** `!_runtime.RealCopyEnabled` (L316–317) | armed host **drops** that blocker |

Policy docs (`docs/architecture.md`, README, `CTraderFixOptions` comment) still **say** the flag should be false. That is **not** proof it stays false in the running API. Architecture “must stay false until §68/§70” is a **should**, violated by the leftover `true` plus the bind.

What **is** still false (not the assigned claim):

- POCO default `RealCopyExecutionEnabled`.
- Worker nested-key fallback.
- `NewOrderSingleImplemented` const.
- Persisted `AllowFixSend`.
- `CTraderFixSession` outbound still `35=A` only.

**SAFE_BY_ABSENCE** of a copy-path `35=D` builder is **not** “the flag stays false.” Next sender that keys off `LiveRuntimeStatus.RealCopyEnabled` would see **armed**.

This slot did **not** flip `.env`. Do **not** treat the leftover `true` as a go-live.

---

## 6. Risk to capital

**NONE** today (`SAFE_BY_ABSENCE` on the product copy hop).

- Hosted FIX session cannot emit `35=D`.
- Copy service cannot honor a send (`NewOrderSingleImplemented=false`, `AllowFixSend` forced false, `VenueReconciled=false`).
- Catalog walks are Manager **read** APIs.

**Residual risk (not capital today):** runtime flag is **armed**. A future sender wired to `RealCopyEnabled` would skip the “flag is false” blocker. Demo `Build("D")` exists off-hop and is demo-gated; not invoked by API/workers. This slot did not attach and did not send.

---

## 7. What this slot did **not** do

- No Manager attach / no re-sum of 18/8460.
- No TLS / no `35=A` measured on the wire this pass.
- No product edit. No `.env` edit. No INDEX / SWARM_LOG write (assigned artifact only).
- No §68 / §70 re-score. Absence of send is **not** those gates PASS.

---

## 8. Stale siblings (this read)

| Sibling claim | Status vs files this slot |
|---|---|
| A002 / A005: API startup still `DemoSeeder` | **STALE** — `BrokerCatalogSeed` only |
| A001: zero `GroupRequestArray` / `UserRequestArray` under `src` | **STALE** — L155 / L223 |
| W500_68 / W500_108: DI + logon + `.env` pinned false | **STALE** — DI binds; logon does not re-pin; `.env` L73 `true` |
| “product has 0 `35=D` writers” as a **tree** claim | **HALF-STALE** — `CTraderFixSession` is clean; demo helper / matrix assemble `Build("D")` |
| CREDENTIALS / E038: `/api/settings` hardcoded `REAL_COPY=false` | **STALE** — now `runtime.RealCopyEnabled` |

---

## 9. Binding table (slot 0)

| Item | Value |
|---|---|
| Slot | **0** |
| Verdict | **FAIL** |
| Failed claim | (5) `REAL_COPY_EXECUTION stays false` — env-bound `true`, no process pin |
| Proven | (1) DemoSeeder not API startup; (2) groups `GroupRequestArray`/`GroupTotal`; (3) traders `UserRequestArray`/`UserLogins`; (4) `CTraderFixSession` no `35=D` |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE`) |
