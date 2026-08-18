# W500_VERIFY_19 — Adversarial live-path verifier (slot 19)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_19.md` |
| Agent / slot | W500 **VERIFY 19** (adversarial; do not trust sibling agents) |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` (live product files under `apps/`, `src/`, vendor header, lab `.env` **boolean keys only**) |
| Product source modified | **No.** |
| Test source modified | **No.** |
| `.env` modified | **No.** Quoted only as `REAL_COPY_EXECUTION_ENABLED=true` (L73) and `FEATURE_COPY_TRADING_ENABLED=true` (L106). |
| Secrets printed | **None.** No MT5 / FIX / proxy / DB passwords. Tag 554 not dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Claims proved or failed from **current files only**. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixLogonHostedService.cs`, `CTraderFixOptions.cs`, `CopyTradingService.cs` (const + persist), `DealIngestionService.cs`, `LiveIngestHostedService.cs`, `LiveMt5Registration.cs`, `BrokerCatalogSeed.cs` header, `DemoSeeder.cs` header, `EnvFile.cs`, `CopyTradingHostedService.cs`, vendor `MT5APIManager.h` L200–254 / L408–411. Targeted `grep` of `apps/` and product `*.cs` for `DemoSeeder`, `GroupRequestArray`, `UserRequestArray`, `UserLogins`, `35=D`, `NewOrderSingle`, `REAL_COPY_EXECUTION`. `.env` key grep only. |

**Honesty rule:** prove from the file or **FAIL** the claim. Prior swarm notes (A002 DemoSeeder-on-startup, W500_68/108 pin-false, A001 cache-only groups) are **stale unless re-read**. Absence of a copy-hop `35=D` is `SAFE_BY_ABSENCE`, not a §68/§70 PASS. A runtime flag that is **true** is not “stays false.” Do not print secrets.

---

## 0. Verdict (binding)

**FAIL** — four of five assigned claims are file-proven; **claim 5 is not.**

`REAL_COPY_EXECUTION` does **not** stay false. Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true`. API `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()`. `DependencyInjection` L41 **binds** that key onto `LiveRuntimeStatus.RealCopyEnabled`. `CTraderFixLogonHostedService` **does not re-pin** false. `/api/settings` exposes `runtime.RealCopyEnabled` (follows env).

Copy hop still cannot emit a ticket (`CTraderFixSession` is `35=A` only; `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`). That is **not** the assigned claim.

| # | Claim | File proof | Result |
|---|---|---|---|
| 1 | DemoSeeder is **not** the API startup path | `apps/api/Program.cs` L152–157 seeds `BrokerCatalogSeed.EnsureAsync` only. Token `DemoSeeder` = **0** in `apps/api/Program.cs`, `apps/mt5-worker/Program.cs`, `apps/fix-worker/Program.cs`, and all of `D:\Prop\apps`. | **PASS** |
| 2 | Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal` | `GetGroupsCore` L152–183: `GroupRequestArray("*")` first; if `list.Count==0`, `GroupTotal` + `GroupNext`. Vendor `MT5APIManager.h` L205 / L212. Ingest `GetGroupsAsync`. | **PASS** (capability; not re-attached) |
| 3 | All traders via `UserRequestArray` / `UserLogins` | `ReadAccountsForGroup` L223 `UserRequestArray`; L227–232 empty → `UserLogins` then `UserRequestByLogins`. `GetAccountsCore(null)` walks every group from (2). Ingest `GetAccountsAsync(null)`. | **PASS** (capability; not re-attached) |
| 4 | `CTraderFixSession` has **no** `35=D` | File 135/135. Outbound tag 35 is only `(35, "A")` L96. `grep` of that file for `35=D` / `(35, "D")` / `Build("D")` / `NewOrderSingle` = **0**. One `WriteAsync` (L49); sockets `using`-disposed. | **PASS** |
| 5 | `REAL_COPY_EXECUTION` **stays false** | **Disproved.** DI L41 env-binds `"true"`. `.env` L73 is `true`. Hosted logon L68–70 logs `RealCopyArmed` and does not assign false. Settings L76 follows runtime. POCO default `false` (`CTraderFixOptions` L35) is **unbound**. | **FAIL** |

One-line:

```text
FAIL claim5: REAL_COPY does not stay false (DI binds .env=true; no hosted re-pin). Claims 1–4 PASS from files: API seed=BrokerCatalogSeed not DemoSeeder; GroupRequestArray(*)/GroupTotal; UserRequestArray/UserLogins; CTraderFixSession 35=A only. Copy send still SAFE_BY_ABSENCE. No secrets. No attach.
```

---

## 1. Claim 1 — DemoSeeder is not the API startup path — **PASS**

API host (`D:\Prop\apps\api\Program.cs`, full 160 lines):

```152:157:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- L6 `using TraderIntelligence.Infrastructure.Seeding;` exists **only** for `BrokerCatalogSeed`.
- L15 `AddTraderIntelligence` → `LiveMt5Registration.CreateConnectors` (Native ×2). DI throws before Fake if passwords missing (`DependencyInjection.cs` L36–37).
- `grep DemoSeeder` on `apps/api/Program.cs` = **0**.

Both workers match the same seed, not DemoSeeder:

```10:16:D:\Prop\apps\mt5-worker\Program.cs
var host = builder.Build();
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

(`apps/fix-worker/Program.cs` L10–16 identical.)

**Residual (does not put DemoSeeder on API startup):** class still exists at `src/Infrastructure/Seeding/DemoSeeder.cs` L14. Product `*.cs` callers: `tests/Integration/SeedingAndStoreTests.cs` L25 plus swarm `_tmp_*` harnesses. A002 (“API still calls DemoSeeder”) is **stale**.

---

## 2. Claim 2 — Native can list all groups via GroupRequestArray or GroupTotal — **PASS**

Vendor (`CIMTManagerAPI`):

```205:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual uint32_t  GroupTotal(void)=0;
   virtual MTAPIRES  GroupNext(const uint32_t pos,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupGet(LPCWSTR name,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupRequest(LPCWSTR name,IMTConGroup* group)=0;
   virtual MTAPIRES  GroupUpdate(IMTConGroup* group)=0;
   virtual MTAPIRES  GroupUpdateBatch(IMTConGroup** configs,const uint32_t config_total,MTAPIRES* results)=0;
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

C# primary + fallback (`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs`):

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

Ingest uses that walk: `DealIngestionService.SyncCatalogAsync` L45 `GetGroupsAsync`; L48 `GetAccountsAsync(null, ct)`.

**What the file does *not* prove (residuals, not a FAIL of the OR claim):**

- `GroupRequestArray("*")` is the manager-ACL **request** enumerator. Completeness is ACL + RPC, not re-measured this slot.
- `GroupTotal`/`GroupNext` is the **pump cache**. Fallback only when the request list is empty. After `Connect(..., PUMP_MODE_NONE)` (L101) that cache may be empty.
- Non-OK request results are **swallowed** (no throw); empty list can mean “request failed and cache cold,” not “zero groups.”
- A001 (“zero `GroupRequestArray` under `src`”) is **stale**.

---

## 3. Claim 3 — All traders via UserRequestArray / UserLogins — **PASS**

Vendor: `UserLogins` h:254; `UserRequestArray` h:410.

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

`GetAccountsCore` L189–213: if `group` is null, enumerate **every name** from `GetGroupsCore()`, then `ReadAccountsForGroup` per name. Catalog ingest calls `GetAccountsAsync(null)`.

**Residuals:**

- Walk is **per group from claim 2**, not a single `UserRequestArray("*")`. Missed groups ⇒ missed traders.
- Hard-fail of `UserRequestArray` still tries pump-cache `UserGetByGroup` **before** `UserLogins`.
- `UserLogins` runs only when `users.Total()==0`. A successful but truncated request would skip it (vendor is expected to be complete; not re-attached).
- Hosted **scoring** is `ListLoginsWithDealsAsync` (`LiveIngestHostedService` L106) — subset of catalog. Catalog persist is still all accounts from this walk.
- This slot did **not** re-sum a live census.

---

## 4. Claim 4 — CTraderFixSession has no 35=D — **PASS**

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read **135/135**.

Outbound builder (only MsgType in the compilation unit):

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

- One `ssl.WriteAsync` (L49), one `ReadAsync` (L53), then `using` disposes `TcpClient`/`SslStream`.
- L55–56 `Extract(reply, "35")` is **inbound** only (Logon accept vs reject).
- File grep: `35=D`, `(35, "D")`, `Build("D")`, `NewOrderSingle` = **0**.

Hosted caller `CTraderFixLogonHostedService` L48–58 invokes `TryLogonAsync` twice (QUOTE 5211 / TRADE 5212). Persist L91–111 updates session rows; **no** order send.

**Residual (not the assigned type):** sibling `CTraderFixDemoTestTrade.cs` `Build("D")` at L139 / L163 / L197. `CTraderFixDemoMatrix.cs` L87 also `Build("D")`. Those are demo/tools, not `CTraderFixSession`, not wired from API DI. Claim 4 as stated is still **PASS**.

Copy hop extra (not required to prove 4, but relevant to capital): `CopyTradingService.NewOrderSingleImplemented = false` (L17); persist `AllowFixSend = false` (L211).

---

## 5. Claim 5 — REAL_COPY_EXECUTION stays false — **FAIL**

Cannot prove “stays false” from the live files. The opposite is on disk.

**Bind (runtime send bit):**

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

**API loads lab env into that configuration:**

```10:13:D:\Prop\apps\api\Program.cs
var loadedEnv = EnvFile.FindAndLoad();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
```

`EnvFile.FindAndLoad` (`src/Mt5/Env/EnvFile.cs` L8–19) includes `D:\Prop\.env`. Grep of that file (boolean key only):

- L73 `REAL_COPY_EXECUTION_ENABLED=true`
- L106 `FEATURE_COPY_TRADING_ENABLED=true` (API ignores this key; settings hardcodes FEATURE copy **true** at Program.cs L77)

**No hosted re-pin.** `CTraderFixLogonHostedService` L68–70:

```68:70:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _log.LogInformation(
            "FIX QUOTE logon={Q} TRADE logon={T}. RealCopyArmed={Armed} NewOrderSingle still unimplemented. Account {Account}",
            quote.LoggedOn, trade.LoggedOn, _runtime.RealCopyEnabled, account);
```

**Settings display follows the bound bit**, not a hardcoded false:

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

**What is still false (does not rescue claim 5):**

| Surface | Value | Why it is not “stays false” |
|---|---|---|
| `CTraderFixOptions.RealCopyExecutionEnabled` | default `false` (L35) | **Unbound.** No `Configure<CTraderFixOptions>`. Unused by `LiveRuntimeStatus`. |
| `apps/api/appsettings.json` `FeatureFlags:LiveCopyEnabled` | `false` | **Different name.** Not the §41 token. |
| fix-worker `CTrader:RealCopyExecutionEnabled` | `GetValue(..., false)` log-only (`Worker.cs` L21–22) | Nested key; worker stamps `Disconnected`; does not pin API runtime. |
| Architecture / README / docs | `REAL_COPY_EXECUTION_ENABLED=false` | Policy text, not process state. |
| `CREDENTIALS_AND_COPY_STATUS.md` / W500_68 / W500_108 | “forced false” | **Stale** vs current DI L41. |

`CopyTradingService.BuildBlockers` L316 only **adds a string** when the bit is already false. When env is true, that blocker is omitted. Send is still blocked by `NewOrderSingleImplemented=false` and `VenueReconciled=false` and persist `AllowFixSend=false`. That is **SAFE_BY_ABSENCE**, not “flag stays false.”

`LiveRuntimeStatus.Snapshot` L42–44 even documents the armed case: `"REAL_COPY armed. NewOrderSingle still unimplemented..."`.

---

## 6. Risk to capital

**NONE** (`SAFE_BY_ABSENCE` on the copy hop).

- Product `CTraderFixSession` cannot assemble `35=D`.
- Copy service const `NewOrderSingleImplemented=false`; persist `AllowFixSend=false`; `VenueReconciled=false`.
- Hosted copy loop (`CopyTradingHostedService`) only `GenerateShadowIntentsAsync`.
- Manager path is GET/request catalog (groups/users/deals/positions). `IMt5BrokerConnector` has no dealer send in this tree.
- Residual: demo/tools `Build("D")` is **off** the API/copy hop and is not claim 4. Next person who wires that helper to a live SenderCompID while `RealCopyEnabled` is already true would change this. This slot did not invoke it.

Claim 5 FAIL is a **control-plane honesty** fail (flag is armed), not a measured dest fill.

---

## 7. What this slot did not do

- Did not Connect Achiever/Starwave; did not re-sum 18/8460.
- Did not GET `/api/settings` on a running host.
- Did not edit product, tests, or `.env`.
- Did not treat prior agent PASS as evidence.

---

## 8. Binding takeaway

Prove-from-file score for the five assigned sentences: **4 PASS / 1 FAIL**.

Overall slot verdict = **FAIL** because claim 5 cannot be proved and is contradicted by `DependencyInjection.cs` L41 + `D:\Prop\.env` L73 + missing hosted re-pin.

Operator leftover (not edited here): flip `REAL_COPY_EXECUTION_ENABLED` back to `false` if the policy sentence is still law. Do not add a copy-path `35=D` builder. Do not wire `CTraderFixDemoTestTrade` into DI.
