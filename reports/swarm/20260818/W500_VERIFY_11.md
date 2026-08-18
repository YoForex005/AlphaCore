# W500_VERIFY_11 — Adversarial live-path re-read (slot 11)

| Field | Value |
|---|---|
| Artifact | `D:\Prop\reports\swarm\20260818\W500_VERIFY_11.md` |
| Agent / slot | Adversarial verifier **11** |
| Date | 2026-08-18 |
| Workspace | `D:\Prop` live product files under `apps/`, `src/` (not other agents) |
| Assigned claims | (1) DemoSeeder is **not** the API startup path. (2) Native connector can list **all** groups via `GroupRequestArray` **or** `GroupTotal`. (3) All traders via `UserRequestArray` / `UserLogins`. (4) `CTraderFixSession` has **no** `35=D`. (5) `REAL_COPY_EXECUTION` **stays false**. |
| Method | Independent `read_file` of `apps/api/Program.cs` (160/160), `NativeMt5BrokerConnector.cs` (458/458), `CTraderFixSession.cs` (135/135), `DependencyInjection.cs`, `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `CTraderFixLogonHostedService.cs`, `BrokerCatalogSeed.cs`, `DemoSeeder.cs` header + factory block, `LiveMt5Registration.cs`, `LiveIngestHostedService.cs`, `DealIngestionService.cs`, `CopyTradingService.cs`, `CopyTradingHostedService.cs`, worker `Program.cs` files, `apps/fix-worker/Worker.cs`, `apps/mt5-worker/Worker.cs`, `apps/api/appsettings.json`. Targeted `grep` of `apps/` for `DemoSeeder`; of `CTraderFixSession.cs` for `35=D` / `NewOrderSingle`; of product `src`+`apps` `*.cs` for `REAL_COPY_EXECUTION`. `.env` flag **boolean only**. |
| Product source modified | **No.** |
| `.env` modified | **No.** |
| Secrets printed | **None.** Quoted only `REAL_COPY_EXECUTION_ENABLED=true` / `FEATURE_COPY_TRADING_ENABLED=true` booleans. No MT5 / FIX / proxy / DB passwords. Tag 554 never dumped. |
| Live attach this pass | **No.** No Manager Connect. No TLS. No Logon. No order. Prior census 18/8460 **cited, not re-probed**. |
| Honesty rule | Verdict **FAIL** if any assigned claim cannot be proven from the live file. Other-agent reports (A002/A014/W500_68/W500_108/CREDENTIALS) are **not** evidence. |

**One-line:**

```text
FAIL. Claims 1–4 proven from live files. Claim 5 not true: lab .env L73 REAL_COPY_EXECUTION_ENABLED=true and DI L41 binds it; hosted logon does not re-pin false. Copy hop still SAFE_BY_ABSENCE (CTraderFixSession 35=A only; NewOrderSingleImplemented=false).
```

---

## 0. Verdict matrix

| # | Claim | Verdict | Proof class |
|---|---|---|---|
| 1 | DemoSeeder is not the API startup path | **PASS** | `apps/api/Program.cs` L152–156 seeds `BrokerCatalogSeed.EnsureAsync` only. `DemoSeeder` token count under `D:\Prop\apps` = **0**. |
| 2 | Native connector can list all groups via `GroupRequestArray` or `GroupTotal` | **PASS** (capability) | `GetGroupsCore` L155 `GroupRequestArray("*")`; empty → L174 `GroupTotal` + `GroupNext`. Ingest L45 `GetGroupsAsync`. |
| 3 | All traders via `UserRequestArray` / `UserLogins` | **PASS** (capability) | `ReadAccountsForGroup` L223 `UserRequestArray`; empty → L230 `UserLogins` + `UserRequestByLogins`. Ingest L48 `GetAccountsAsync(null)`. |
| 4 | `CTraderFixSession` has no `35=D` | **PASS** | Full 135/135 read. Outbound tag 35 is `"A"` only (`BuildLogon` L96). File grep `35=D` / `NewOrderSingle` / `(35, "D")` = **0**. |
| 5 | `REAL_COPY_EXECUTION` stays false | **FAIL** | Lab `.env` L73 is `true`. `DependencyInjection.cs` L41 binds that string onto `LiveRuntimeStatus.RealCopyEnabled`. `CTraderFixLogonHostedService` **does not** assign `false`. `/api/settings` echoes the runtime bit. Cannot prove “stays false”; the live bind proves the opposite. |

**Overall slot verdict: `FAIL`**

Claim 5 is an assigned claim. It is not proven. W500_68 / W500_108 / `CREDENTIALS_AND_COPY_STATUS.md` “forced false” / A014 “DI pins false” are **stale**. Architecture still **requires** the flag stay false; the **process** no longer enforces that.

Risk to capital: **NONE** (`SAFE_BY_ABSENCE`). Armed flag cannot emit a ticket: hosted hop has no NewOrderSingle builder; `CopyTradingService.NewOrderSingleImplemented = false`; persist `AllowFixSend = false`.

---

## 1. DemoSeeder is not the API startup path — PASS

Live API host (`D:\Prop\apps\api\Program.cs`, 160 lines):

```152:156:D:\Prop\apps\api\Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

- `using TraderIntelligence.Infrastructure.Seeding;` at L6 exists for `BrokerCatalogSeed`, not `DemoSeeder`.
- Zero `DemoSeeder` / `FakeMt5` / `10001` / `10002` tokens in this file.
- Grep of `D:\Prop\apps` for `DemoSeeder`: **0**.

Same catalog seed on both workers:

```11:16:D:\Prop\apps\fix-worker\Program.cs
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TraderDbContext>();
    await db.Database.EnsureCreatedAsync();
    await BrokerCatalogSeed.EnsureAsync(db, CancellationToken.None);
}
```

`apps/mt5-worker/Program.cs` L11–16 is the same `BrokerCatalogSeed` block.

DI live connectors are Native ×2 only (`LiveMt5Registration.CreateConnectors`). No `FakeMt5BrokerConnector` under `src/Infrastructure`. Fail-closed: missing both real passwords throws before `CreateConnectors` (`DependencyInjection.cs` L36–37).

**What still exists (not API startup):**

| Residual | Path | Role |
|---|---|---|
| `DemoSeeder` class | `src/Infrastructure/Seeding/DemoSeeder.cs` L14 | File remains. Seeds Fake via `DemoBrokerFactory.CreateDefault()` L126 and scores `{10001,10002,10003,99001}` L134. |
| Integration test | `tests/Integration/SeedingAndStoreTests.cs` L25 | Calls `DemoSeeder.SeedAsync`. |
| Scratch trees | `reports/swarm/20260818/_tmp_*` | Not product hosts. |
| mt5-worker scorer | `apps/mt5-worker/Worker.cs` L31–35 | Still rebuilds the four dummy logins **after** a real `SyncBrokerAsync` of both brokers. Does **not** seed DemoSeeder. Hosted API ingest scores `ListLoginsAsync` (resync) / `ListLoginsWithDealsAsync` (ingest) — not those four. |

A002 / A005 (“API startup still `DemoSeeder.SeedAsync`”) are **stale**. A014 is current on this claim.

`BrokerCatalogSeed` writes brokers + FIX rows as `Disconnected` / “NewOrderSingle off”. It does **not** insert demo logins 10001–99001.

---

## 2. Native connector can list all groups via `GroupRequestArray` or `GroupTotal` — PASS (capability)

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` `GetGroupsCore`:

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

Live ingest uses that walk, unfiltered:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`GetAccountsAsync(null)` re-enters `GetGroupsCore` (L201–202).

**Adversarial limits (do not over-claim):**

- This slot did **not** attach. Completeness of a live 18-group census is **prior**, not re-proven here.
- `GroupRequestArray("*")` is the no-pump complete enumerator **when** the Manager ACL allows and the RPC returns OK / OK_NONE.
- `GroupTotal` / `GroupNext` is the **pump cache**. It is used only if the request list is empty. If pump is `PUMP_MODE_NONE` (connect fallback L101) **and** the request fails, `GetGroupsCore` can honestly return `[]`.
- `_pumpEnabled` never gates the fetch (no branch). That is good.

A001 (“zero `GroupRequestArray` hits under `src`”) is **stale**.

---

## 3. All traders via `UserRequestArray` / `UserLogins` — PASS (capability)

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

Order: **request first** (`UserRequestArray`) → pump-cache `UserGetByGroup` only on hard fail → if still empty, **`UserLogins`** + `UserRequestByLogins`.

`GetAccountsCore(null)` walks every name from `GetGroupsCore` and unions by login (L205–209). Ingest / resync / `LiveIngestHostedService` call `GetAccountsAsync(null)` / `SyncCatalogAsync`. Flag-blind: no `REAL_COPY` check on the fetch.

**Adversarial limits:**

- If `UserRequestArray` returns OK with a **partial** array, `UserLogins` is skipped. Completeness then depends on the Manager RPC.
- `UserGetByGroup` is pump-cache; it is not the primary path.
- “ALL manager-visible traders” is an ACL + RPC property. This slot did not re-count 6512 + 1948.

A001 (“zero `UserRequestArray` / `UserLogins` under `src`”) is **stale**.

---

## 4. `CTraderFixSession` has no `35=D` — PASS

Assigned file `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` read **135/135**.

Outbound body is only Logon:

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

- One `WriteAsync` (L49). One `ReadAsync` (L53). `using` disposes `TcpClient` / `SslStream`.
- Tag-35 `Extract` is **inbound** only (accept `A`, else Error).
- File-local grep: `35=D` = 0, `(35, "D")` = 0, `NewOrderSingle` = 0.

Hosted caller `CTraderFixLogonHostedService` invokes `TryLogonAsync` twice (QUOTE 5211, TRADE 5212). Persist updates existing FIX rows; does not send an order.

**Sibling residual (not this class):** `CTraderFixDemoTestTrade.Build("D")` at L139 / L163 / L197. Demo-gated (`demo-` host, `demo.` SenderCompID, refuse account `1369850`). Called from `tools/DemoFixTestTrade`, not DI / API / workers / `CopyTradingService`. Claim 4 is about `CTraderFixSession`. Do **not** claim “product `35=D=0`” globally.

---

## 5. `REAL_COPY_EXECUTION` stays false — FAIL

The assigned claim is that the flag **stays false**. Live files disprove a process pin.

| Surface | Measured | Stays false? |
|---|---|---|
| Architecture / docs / README | Policy `=false` | policy only |
| `CTraderFixOptions.RealCopyExecutionEnabled` L35 | default `false` | **unbound** (no `Configure<CTraderFixOptions>`) |
| `LiveRuntimeStatus.RealCopyEnabled` | DI L41: env `REAL_COPY_EXECUTION_ENABLED` equals `"true"` (ignore-case) | **bound** |
| `CTraderFixLogonHostedService` | L68–70 logs `RealCopyArmed={Armed}`; **no** `_runtime.RealCopyEnabled = false` | **no re-pin** |
| `apps/api/Program.cs` L10 + L13 | `EnvFile.FindAndLoad()` then `AddEnvironmentVariables()` | process **loads** lab `.env` |
| Lab `.env` L73 | `REAL_COPY_EXECUTION_ENABLED=true` | **operator leftover — policy violation** |
| Lab `.env` L106 | `FEATURE_COPY_TRADING_ENABLED=true` | unused by API (FEATURE is a **literal** `true` at `Program.cs` L77) |
| `GET /api/settings` L76 | `["REAL_COPY_EXECUTION_ENABLED"] = runtime.RealCopyEnabled` | display follows env |
| `apps/api/appsettings.json` | `FeatureFlags:LiveCopyEnabled=false` | **different name**; not the §41 token |
| `apps/api/Controllers/SettingsController.cs` | `LiveCopyEnabled` default false | **dead** — API host has **no** `AddControllers` / `MapControllers` |
| `apps/fix-worker/Worker.cs` L21 | `GetValue("CTrader:RealCopyExecutionEnabled", false)` | **different key**; log-only; still stamps Disconnected |
| `CopyTradingService` | `RealCopyArmed: _runtime.RealCopyEnabled` (L44); blocker only if **false** (L316–317) | next sender would see **armed** |

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Therefore: **cannot prove claim 5.** The runtime bit **does not stay false** on this lab host. W500_68 / W500_108 / A014 L270 / CREDENTIALS “forced false” are **stale**.

What **does** stay false / absent (not the assigned claim, but capital-relevant):

```16:17:D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs
    public const bool VenueReconciled = false;
    public const bool NewOrderSingleImplemented = false;
```

Persist L211: `AllowFixSend = false`. Generate path writes `SHADOW_ONLY`. Hosted copy tick (`CopyTradingHostedService`) only calls `GenerateShadowIntentsAsync`.

---

## 6. Risk to capital

| Question | Answer |
|---|---|
| Can this process send `35=D` on the copy hop? | **No.** `CTraderFixSession` outbound is `35=A` only. No QuickFIX initiator on the hop. `NewOrderSingleImplemented=false`. |
| Does an armed `REAL_COPY` change that today? | **No send.** It **does** flip `/api/settings`, `/api/health.realCopyEnabled`, `CopyGateStatus.RealCopyArmed`, and `RiskEngine` input `RealExecutionEnabled`. Persist still forces `AllowFixSend=false`. |
| Demo helper `Build("D")` | Tools + demo-host gate. Not wired to copy. Not invoked this slot. |
| Fetch ALL groups/traders | Read-only Manager RPCs. Independent of the flag. |
| This slot live attach | **No.** |
| Risk to dest capital | **NONE** (`SAFE_BY_ABSENCE`) |

Do **not** treat FAIL-on-claim-5 as “capital at risk now.” Treat it as: the **named safety pin is gone**; the next sender would see an armed runtime. Operator should set `.env` L73 back to `false` (this slot did **not** edit it). Do **not** add a copy-path `35=D` builder.

---

## 7. Stale reports this read supersedes (for these five claims)

| Prior | Why stale |
|---|---|
| A002 / A005 | API startup is `BrokerCatalogSeed`, not `DemoSeeder`. |
| A001 | `GroupRequestArray` / `UserRequestArray` / `UserLogins` exist on the Native connector. |
| A014 L270 / W500_68 / W500_108 / CREDENTIALS “forced false” | DI binds env; hosted logon no longer re-pins. |
| E038 “`/api/settings` hardcoded false” | Now `runtime.RealCopyEnabled`. |
| “Product `35=D=0`” as a global | Sibling `CTraderFixDemoTestTrade` can `Build("D")`. Assigned class still 0. |

---

## 8. Checklist

- [x] Read live `Program.cs`, Native connector, `CTraderFixSession`, DI, logon host, ingest (not other agents)
- [x] Claim 1 proven PASS
- [x] Claim 2 proven PASS (capability; no re-attach)
- [x] Claim 3 proven PASS (capability; no re-attach)
- [x] Claim 4 proven PASS (135/135)
- [x] Claim 5 **FAIL** — cannot prove “stays false”; files show bind-to-true
- [x] No secrets printed
- [x] Product source not modified
- [x] Overall verdict **FAIL**
