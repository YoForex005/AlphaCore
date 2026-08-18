# W500_VERIFY_32 — Adversarial live-path verify (slot 32)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_32.md` |
| Agent / slot | W500 verify **32** (adversarial; did **not** trust sibling reports) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted as boolean keys only. |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true`. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Claims proven or failed from **on-disk product files** only. |
| Method | Independent full `read_file` of `apps/api/Program.cs` (160/160), `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs` (62/62), `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `CopyTradingService.cs`, `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `EnvFile.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` header, `CTraderFixDemoTestTrade.cs` gate + `Build("D")` sites, `CTraderFixDemoMatrix.cs` gate, `apps/api/appsettings.json`, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs` header. Targeted `grep`: `DemoSeeder` under `apps/` (**0**) and product `*.cs`; `GroupRequestArray`/`GroupTotal`/`UserRequestArray`/`UserLogins` in the native connector; `(35,` / `Build("D")` under `src/Fix.CTrader`; `REAL_COPY` / `RealCopyEnabled` / `AllowFixSend`. `.env` L73/L106 boolean keys only. |

**Honesty rule:** sibling swarm markdown is **not** evidence. A comment, log line, dashboard bit, or `LastError` string is **not** a builder. `35=A` Logon is **not** NewOrderSingle. A demo-only `Build("D")` helper is **not** `CTraderFixSession`. A POCO `= false` default is **not** a process pin. An env bind that evaluates `true` **disproves** “stays false.” Set **FAIL** if any assigned claim cannot be proven from the file.

---

## 0. Verdict (binding)

**FAIL.**

Claims **1–4** are proven from the live product files this slot read. Claim **5** (`REAL_COPY_EXECUTION stays false`) is **not** proven and is **disproven as API process state**: `EnvFile.FindAndLoad()` + `AddEnvironmentVariables()` + DI L41 bind `REAL_COPY_EXECUTION_ENABLED`; lab `.env` L73 is `true`; `CTraderFixLogonHostedService` **does not** assign `_runtime.RealCopyEnabled = false`. Instruction: fail the slot if any claim cannot be proven from the file.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | `DemoSeeder` is **not** the API startup path | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. Token `DemoSeeder` = **0** in `apps/api/Program.cs` and **0** under `D:\Prop\apps\**`. | **PASS** |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | `GetGroupsCore` L155 `GroupRequestArray("*", arr)` first; if `list.Count == 0` then L174 `GroupTotal()` + `GroupNext`. Ingest `GetGroupsAsync` (`DealIngestionService` L45/L61). | **PASS** |
| 3 | All traders via `UserRequestArray` / `UserLogins` | `ReadAccountsForGroup` L223 `UserRequestArray(gname, users)`; if `users.Total()==0` then L230 `UserLogins` + `UserRequestByLogins`. `GetAccountsCore(null)` walks every group from claim 2. Ingest `GetAccountsAsync(null)` L48/L62. | **PASS** |
| 4 | `CTraderFixSession` has **no** `35=D` | Assigned file 135/135. Only outbound tag 35 is `(35, "A")` at L96. One `WriteAsync` (L49). Literal `35=D` / `NewOrderSingle` / `(35, "D")` = **0** in that file. | **PASS** |
| 5 | `REAL_COPY_EXECUTION` **stays false** | Cannot prove. DI L41 sets `RealCopyEnabled` from env `== "true"`. `.env` L73 is `true`. API L10 loads `.env`. Logon host L68–70 **logs** `RealCopyArmed` and does **not** assign `false`. `/api/settings` L76 exposes `runtime.RealCopyEnabled`. POCO default false is **unbound**. | **FAIL** |

One-line:

```text
FAIL. DemoSeeder not API startup (BrokerCatalogSeed). Native ALL groups GroupRequestArray("*") else GroupTotal; ALL traders UserRequestArray then UserLogins. CTraderFixSession 35=A only. REAL_COPY does not stay false: .env L73=true, DI binds, no logon re-pin. Copy hop still SAFE_BY_ABSENCE (no 35=D sender).
```

---

## 1. DemoSeeder is not the API startup path — PASS

Live API host: `D:\Prop\apps\api\Program.cs` (160 lines, full read this slot).

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
- `grep DemoSeeder` on `D:\Prop\apps` = **0** matches.
- Same seed on workers: `apps/mt5-worker/Program.cs` L15 and `apps/fix-worker/Program.cs` L15 call **only** `BrokerCatalogSeed.EnsureAsync`.
- Composition: `builder.Services.AddTraderIntelligence` (API L15) fail-closes unless both MT5 passwords pass `LiveMt5Registration.HasRealPasswords` (`DependencyInjection.cs` L36–37: “Dummy/fake broker data is disabled”), then registers `LiveMt5Registration.CreateConnectors` → **Native ×2 only** (`DependencyInjection.cs` L47–48; `LiveMt5Registration.cs` L23–49). No `DemoSeeder`. No `FakeMt5BrokerConnector` on that path.
- API host is minimal APIs only: no `AddControllers` / `MapControllers`. Leftover `apps/api/Controllers/SettingsController.cs` is not on the startup map.

**Residual (does not fail this claim):**

- `DemoSeeder` still exists at `D:\Prop\src\Infrastructure\Seeding\DemoSeeder.cs` (`public static class DemoSeeder`, `SeedAsync`). Product call sites: **tests only** (`tests/Integration/SeedingAndStoreTests.cs` L25) plus swarm `_tmp_*` eval hosts under `reports/`. That is **not** API/worker startup.
- `apps/mt5-worker/Worker.cs` L31 still **scores** `{10001, 10002, 10003, 99001}` after a real `SyncBrokerAsync` of Achiever+Starwave. Scoring leftover, not seed-as-startup. Hosted ingest scores `ListLoginsWithDealsAsync` (`LiveIngestHostedService.cs` L106), not those four logins.

A002 / A005 / A011 claims that API startup still calls `DemoSeeder` are **stale vs this file**.

---

## 2. Native connector can list all groups via GroupRequestArray or GroupTotal — PASS

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458/458, full read).

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

- Primary: network `GroupRequestArray("*")` — manager-visible set; pump **not** required.
- Fallback: local cache `GroupTotal` + `GroupNext` **only when** the request list is empty (`list.Count == 0`).
- Live ingest uses this path: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`; `SyncBrokerAsync` L61 again. No plan-env / `Take(` filter on groups.
- `GetAccountsCore` L201–202 walks **every** name from `GetGroupsCore()` when `group` is null.

**Not proven this slot:** a live 18-group census. This slot did **not** attach. Capability is file-proven.

**Caveat (does not fail the assigned capability claim):** if `GroupRequestArray` returns OK/OK_NONE with a **non-empty but incomplete** array, `GroupTotal` is skipped. If the request fails **and** the pump cache is empty (`GroupTotal()==0`), the method returns `[]` with no throw. Manager rights still bound the visible set.

A001 “zero `GroupRequestArray` under `src`” is **stale**.

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

- Primary: `UserRequestArray` (network user records, **per group name**, not a global `"*"`).
- Hard fail of the request (not OK / OK_NONE / NOTFOUND): cache `UserGetByGroup`.
- Empty user array: `UserLogins` then `UserRequestByLogins`.
- `GetAccountsCore(null)` (L189–214) unions every group from claim 2 by login → **all manager-visible traders**.
- Ingest: `GetAccountsAsync(null)` at `DealIngestionService` L48 and L62. No `Take(` / `Skip` on the account walk.

**Not proven this slot:** live 8460-login arithmetic. Capability is file-proven.

**Caveat (does not fail the assigned capability claim):** `UserLogins` runs only when `users.Total()==0`. A partial `UserRequestArray` success would skip the login-list fallback. Hosted **scoring** is `ListLoginsWithDealsAsync` (deals-only), not every catalog login — that is scoring scope, not the connector list.

A001 “zero `UserRequestArray` / `UserLogins` under `src`” is **stale**.

---

## 4. CTraderFixSession has no 35=D — PASS

File: `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` (**135 / 135**, full read).

- Types: `CTraderFixSessionResult` + static `CTraderFixSession`.
- Only public method: `TryLogonAsync`.
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
- Inbound `Extract(reply, "35")` (L55) is **read**, not send. Accepts `"A"` as logon; otherwise `LastError` includes `35={msgType}`.
- Grep of this file: `(35,` = **1** (`"A"`). Tokens `35=D`, `NewOrderSingle`, `(35, "D")`, `Build("D")` = **0**.
- Hosted caller `CTraderFixLogonHostedService` L48–58 invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). No second writer on that class.

**Residual (does not fail claim 4 — claim text is this class):**

- Sibling `CTraderFixDemoTestTrade` `Build("D", …)` at L139 / L163 / L197. Gate L43–60 refuses unless host starts `demo-`, sender starts `demo.`, and account is not `1369850`; also refuses `live-` host / `live.` sender.
- Sibling `CTraderFixDemoMatrix` `Build("D", …)` at L93. Gate L22–28 same demo host/sender + refuse account `1369850`.
- Invoker is `tools/DemoFixTestTrade/Program.cs` (not API / DI / copy / workers). Not `CTraderFixSession`.

Copy hop still cannot send even if the flag is armed:

- `CopyTradingService.NewOrderSingleImplemented = false` (L17).
- Persist `AllowFixSend = false` (L211) — **forced**, ignores `RiskEngine` `AllowFixSend`.
- `VenueReconciled = false` (L16).
- Send branch L217 requires `decision.AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` and still only sets `LIVE_SEND_BLOCKED_UNIMPLEMENTED` — no FIX write.

---

## 5. REAL_COPY_EXECUTION stays false — FAIL (cannot prove; API process is armed)

Assigned claim is a **state** claim: the flag **stays false**. Files do **not** pin it false on the API host.

| Surface | Measured | Why claim 5 fails |
|---|---|---|
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | operator leftover already **true** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | unused by DI; API FEATURE is a **literal** `true` (`Program.cs` L77) |
| API env load | `Program.cs` L10 `EnvFile.FindAndLoad()`; L13 `AddEnvironmentVariables()` | process **imports** L73 |
| `EnvFile` | candidates include hard path `D:\Prop\.env` (L14) | load is deterministic if the file exists |
| DI bind | `DependencyInjection.cs` L41 `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", …)` | **bound, not pinned** |
| Logon re-pin | `CTraderFixLogonHostedService` L68–70 logs `RealCopyArmed={Armed}`; **no** `_runtime.RealCopyEnabled = false` | W500_68/108 pin **stale** |
| API display | `/api/settings` L76 `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | will report **true** after L10+L41 |
| `/api/health` | L55 `realCopyEnabled = runtime.RealCopyEnabled` | same |
| `LiveRuntimeStatus.Snapshot` | L42–44 copyNote branches on `RealCopyEnabled` | armed text if true |
| POCO | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | default only; **no** `Configure<CTraderFixOptions>` in DI |
| Worker | `GetValue("CTrader:RealCopyExecutionEnabled", false)` (`fix-worker/Worker.cs` L21) | **different key**; fallback false; **log-only**, stamps TRADE `Disconnected` |
| Worker `.env` | mt5/fix `Program.cs` do **not** call `EnvFile` | workers stay unarmed **unless** the process env already has the key |
| `appsettings.json` | `FeatureFlags:LiveCopyEnabled=false` | **different name**; not the §41 token |
| Copy gate | `BuildBlockers` adds `"REAL_COPY_EXECUTION_ENABLED is false"` **only if** `!_runtime.RealCopyEnabled` (L316–317) | armed API host **drops** that blocker |
| Risk input | `CopyTradingService` L190 `RealExecutionEnabled = _runtime.RealCopyEnabled` | engine `allowSend` can become true if venue+recon also true (`RiskEngine.cs` L147–150); persist still forces `AllowFixSend=false` |

Policy docs (`docs/architecture.md`, README, `CTraderFixOptions` comment) still **say** the flag should be false. That is **not** proof it stays false in the running API. Architecture “must stay false until §68/§70” is a **should**, violated by leftover `true` plus the bind.

What **is** still false (not the assigned claim):

- POCO default `RealCopyExecutionEnabled`.
- Worker nested-key fallback.
- `NewOrderSingleImplemented` const.
- Persisted `AllowFixSend`.
- `CTraderFixSession` outbound still `35=A` only.

**SAFE_BY_ABSENCE** of a copy-path `35=D` builder is **not** “the flag stays false.” Next sender that keys off `LiveRuntimeStatus.RealCopyEnabled` would see **armed** on the API host.

This slot did **not** flip `.env`. Do **not** treat the leftover `true` as a go-live.

---

## 6. Risk to capital

**NONE** today (`SAFE_BY_ABSENCE` on the product copy hop).

- Hosted FIX session (`CTraderFixSession`) cannot emit `35=D`.
- Copy service cannot honor a send (`NewOrderSingleImplemented=false`, persist `AllowFixSend` forced false, `VenueReconciled=false`).
- Catalog walks are Manager **read** APIs (`GroupRequestArray` / `GroupTotal` / `UserRequestArray` / `UserLogins`).

**Residual risk (not capital today):** runtime flag is **armed on the API host**. A future sender wired to `RealCopyEnabled` would skip the “flag is false” blocker. Demo `Build("D")` exists off-hop and is demo-gated; not invoked by API/workers. This slot did not attach and did not send.

---

## 7. What this slot did **not** do

- No Manager attach / no re-sum of any live census (18/8460 cited by siblings is **not** re-proven here).
- No TLS / no `35=A` measured on the wire this pass.
- No product edit. No `.env` edit. No INDEX / SWARM_LOG write (assigned artifact only).
- No §68 / §70 re-score. Absence of send is **not** those gates PASS.

---

## 8. Stale siblings (this read)

| Sibling claim | Status vs files this slot |
|---|---|
| A002 / A005 / A011: API startup still `DemoSeeder` | **STALE** — `BrokerCatalogSeed` only |
| A001: zero `GroupRequestArray` / `UserRequestArray` under `src` | **STALE** — L155 / L223 |
| W500_68 / W500_108: DI + logon + `.env` pinned false | **STALE** — DI binds; logon does not re-pin; `.env` L73 `true` |
| CREDENTIALS / E038: `/api/settings` hardcoded `REAL_COPY=false` | **STALE** — now `runtime.RealCopyEnabled` |
| “product has 0 `35=D` writers” as a **tree** claim | **HALF-STALE** — `CTraderFixSession` is clean; demo helper / matrix assemble `Build("D")` |
| “workers load `.env` the same as API” | **FALSE** — only API `Program.cs` calls `EnvFile.FindAndLoad()` |

---

## 9. Binding table (slot 32)

| Item | Value |
|---|---|
| Slot | **32** |
| Verdict | **FAIL** |
| Failed claim | (5) `REAL_COPY_EXECUTION stays false` — env-bound `true`, no process pin |
| Proven | (1) DemoSeeder not API startup; (2) groups `GroupRequestArray`/`GroupTotal`; (3) traders `UserRequestArray`/`UserLogins`; (4) `CTraderFixSession` no `35=D` |
| Risk to capital | **NONE** (`SAFE_BY_ABSENCE`) |
