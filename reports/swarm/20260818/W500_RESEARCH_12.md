# W500_RESEARCH_12 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 12
- **date:** 2026-08-18
- **angle:** Does `NativeMt5BrokerConnector` fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders via Manager request arrays? Must copy-to-cTrader **not** send live orders (no capital loss).
- **method:** `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). No secrets printed. This slot did **not** re-run live Connect; live counts come from the existing probe artifact.
- **verdict:** **PASS**

## 1. Question and answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes.** Primary group walk: `_manager.GroupRequestArray("*", arr)` (`GetGroupsCore` L155). |
| Does it call `UserRequestArray`? | **Yes.** Primary user walk per group: `_manager.UserRequestArray(gname, users)` (`ReadAccountsForGroup` L223). |
| Are those the no-pump complete enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410 (`MT5APIManager.h`, API 5570, 30 Jan 2026). Mask `*` = every group this manager ACL may see. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` call `GetAccountsAsync(null)` → `GetAccountsCore` walks **every** name from `GetGroupsCore`. |
| Are Achiever + Starwave both registered? | **Yes.** `LiveMt5Registration.CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances (`BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx`). |
| Live measured census (prior probe, same day)? | **ACHIEVER 8 groups / 6512 traders; STARWAVEFX 10 groups / 1948 traders. Total 18 / 8460.** |
| Does copy-to-cTrader send live orders? | **No.** `CTraderFixSession` emits only `35=A` Logon. No `35=D`. `RealCopyEnabled` forced `false` in DI and again after FIX logon. |
| Risk to capital from this path? | **None.** Manager read APIs + FIX logon only. No `DealerSend` / `OrderSend` / `NewOrderSingle`. |

Stale reports that claimed “zero `GroupRequestArray` hits under `D:\Prop\src`” (`A001_native_connector.md`) are **wrong for the current file**. Current source has the two request calls.

## 2. SDK contract (what ALL means)

Vendor header (not YoPips product code):

```212:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```408:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

- Interface: **`IMTManagerAPI`** (class starts L121). Request APIs hit the **server** and do **not** require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`.
- `IMTAdminAPI` (class starts L785) has `GroupTotal`/`GroupNext`/`GroupGet` but **no** `GroupRequest` / `GroupRequestArray`. Second `UserRequestArray` at header L1173 is on **Admin**, unused by this C# connector (`CIMTManagerAPI` only).
- Mask language (`CMTStr::CheckGroupMask`, A39): `*` = all groups this manager may see. Same mask language on `UserRequestArray` / `UserLogins`.
- **ALL in this product = manager-ACL-visible.** Server groups outside the manager record are invisible by design. Do not invent a second plan-map filter.

YoPips C++ product (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`) **does not call** `GroupRequestArray` or `UserRequestArray` (0 hits under `src\`). `MT5Manager::GetAllGroups` is cache `GroupTotal`/`GroupNext` only (`mt5_manager.cpp` L962–981). `GetUserLogins` is `UserLogins` only (L315–327). That is the **old** wrapper. The Prop C# connector is the path that implements A39’s request-API fallback.

## 3. Connector — groups (`GroupRequestArray`)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines).

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

Measured properties:

| Property | Evidence |
|---|---|
| Mask | Literal `"*"` — not `MT5_GROUP_*`, not plan names, not `demo\yo-*` only. |
| Dedup | `HashSet<string>` ordinal-ignore-case on `grp.Group()`. |
| Empty / not-found | `MT_RET_OK_NONE` still walks `arr` (usually empty) then cache fallback. |
| Cache fallback | `GroupTotal`/`GroupNext` **only if** `list.Count == 0`. Needed when pump is off (`Connect` may retry `PUMP_MODE_NONE`, L101–111). |
| Cap / Take / page | **None** in this file (`grep Take\|Skip\|pageSize` under `D:\Prop\src\Mt5` = 0). |
| Plan filter | **None.** `AddGroup` copies name/currency/digits/company/margin/connections flag only. |

Connect pump preference (helps cache fallback, not required for request APIs):

```89:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            // ...
            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
```

`Ensure()` refuses work when not connected (L436–439). No dummy book.

## 4. Connector — traders (`UserRequestArray`)

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // ...
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
```

`GetGroupsCore` is called while already holding `_gate`. C# `lock` is re-entrant — not a deadlock.

```216:271:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

            var acctReq = _manager.UserAccountRequestArray(gname, accounts);
            // ...
```

Call graph for ALL traders:

```
GetAccountsAsync(null)
  → GetAccountsCore(null)
      → GetGroupsCore()                 // GroupRequestArray("*") then cache if empty
      → for each group name
           ReadAccountsForGroup(name)
             1. UserRequestArray(name)          // network full user records
             2. else UserGetByGroup             // pump cache
             3. if still empty: UserLogins + UserRequestByLogins
             4. UserAccountRequestArray         // balances/equity (not the login census)
```

Ingest always uses the null-group path:

```44:48:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

Dashboard `GetTradersAsync` walks **all** `Mt5Accounts` with no `Take` (`EfDashboardQueries.cs` L85–128). The only `Take(20)` in that file is risk-reject **reasons** (L204), not the trader universe.

## 5. Both brokers (Achiever + Starwave)

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // server/port/login/password/proxy from MT5_* / ACHIEVER_PROXY_* — values not quoted
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_* — values not quoted
            ProxyEnabled = false,
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

`BrokerCodes`: `ACHIEVER`, `STARWAVEFX` only. DI fail-closes without real password keys (`DependencyInjection.cs` L35–36). No FakeMt5 on the live path.

## 6. Live measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at `2026-08-18T08:42:16.8519545+00:00`. Note in file: “Passwords never written.”

| Broker | Connect | Groups | Accounts | Open positions | Path |
|---|---|---:|---:|---:|---|
| ACHIEVER | connected, 7213 ms | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | connected, 6413 ms | 10 | 1948 | 478 | same |
| **Total** | | **18** | **8460** | **1984** | |

Achiever group account sums: 2+179+4+5+4+6295+0+23 = **6512**.  
Starwave group account sums: 11+4+170+1735+22+0+0+4+0+2 = **1948**.

Group names (no logins printed here; full login list is in the JSON):

- Achiever: `contest\yo-1step`, `contest\yo-2step`, `contest\yo-instant`, `contest\yo-payp`, `demo\yo-1step`, `demo\yo-2step`, `demo\yo-instant`, `demo\yo-payp`.
- Starwave: `Starwave\cent\FX1\grp1`, `...\grp2`, `Starwave\demo\FX2\grp1`, `...\grp2`, `Starwave\real\FX3\grp1`–`grp5`, `Starwave\real\FX3\LP`.

These are **all groups those two manager logins could see at probe time**. If Administrator later adds a group the manager ACL allows, a one-shot ingest (`LiveIngestHostedService` runs the catalog once after 2 s, no `PeriodicTimer`) will miss it until process restart / `/api/ops/resync`. That is a **host** hole, not a missing request API on the connector.

## 7. Completeness holes (honest residuals — not FAIL on this slot)

The connector **shape** is ALL-manager-visible. These are residual ways a row can still be missed:

1. **Partial `GroupRequestArray` accepted.** Cache `GroupTotal`/`GroupNext` runs only when `list.Count == 0`. A non-empty but truncated request result is treated as complete. No compare to `GroupTotal()`. Failed request (not OK / OK_NONE) is swallowed, then cache used only if the list is still empty.
2. **Partial `UserRequestArray` accepted.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returns some but not all users, missing logins are not recovered. `UserGetByGroup` is skipped on `OK` / `OK_NONE` / `NOTFOUND`.
3. **ACL bound.** Groups/users outside the manager’s Administrator “Groups” mask never appear. That is correct Manager semantics, not a product filter.
4. **Zero-account groups are kept** (`demo\yo-instant` = 0, several Starwave real groups = 0). Good — empty ≠ omitted.
5. This slot **did not** re-measure Connect. Counts are from the 08:42 UTC probe, not a fresh run.

None of these holes send an order.

## 8. Copy to cTrader — no live orders (no loss)

`NativeMt5BrokerConnector` implements `IMt5BrokerConnector` / `IMt5BulkDealReader` / `IMt5BulkPositionReader` only. `grep` for `DealerSend|DealerBalance|TradeRequest|UserUpdate|OrderSend` in that file: **0 hits**. Reads: `DealRequest` / `DealRequestByGroup` / `PositionRequest` / `PositionRequestByGroup`.

FIX session builds **only** Logon (`35=A`). No NewOrderSingle:

```94:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
        var fields = new List<(int tag, string value)>
        {
            (35, "A"),
            (34, seq.ToString(CultureInfo.InvariantCulture)),
            // ...
            (553, username),
            (554, password)
        };
```

`grep` `35=D|NewOrderSingle|SendRaw` under `D:\Prop\src\Fix.CTrader`: only comments / log text saying NewOrderSingle is **disabled**.

Hard off:

```38:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
```

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
```

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** and is **not bound** from env anywhere else under `src` (single hit: the property declaration). Snapshot copy note: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` (`LiveRuntimeStatus.cs` L42–43).

Tag 553 is the integer account id, not SenderCompID (`CTraderFixLogonHostedService` L45–46) — logon-only; still no `35=D`.

## 9. Verdict

**PASS.**

`NativeMt5BrokerConnector` **does** use `GroupRequestArray("*")` and per-group `UserRequestArray` as the primary ALL enumerators for both Achiever and Starwave. Ingest asks `GetAccountsAsync(null)`. There is no `Take`, no plan-map filter, no dummy substitution on this type. A same-day live probe recorded 18 manager-visible groups and 8460 traders. Copy-to-cTrader cannot place live orders from this process (`35=A` only; `RealCopyEnabled=false`).

Residual (documented, not a capital-loss FAIL): partial request arrays are not cross-checked against `GroupTotal` / `UserLogins` length; ingest is one-shot; ALL is ACL-bounded.

## 10. Files read

| Path | Role |
|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Assigned type (full 458) |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | 2× connector factory |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Live-only DI, copy flag off |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | One-shot catalog+deals |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Ports / DTOs |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot honesty |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | ACHIEVER / STARWAVEFX |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces copy off |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Infrastructure\Dashboard\EfDashboardQueries.cs` | Traders list unbounded |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | L212, L410, L1173; IMTManagerAPI vs IMTAdminAPI |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Cache-only groups; `UserLogins` only |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Measured 8+10 / 6512+1948 |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Same-day summary |
