# W500_RESEARCH_72 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 72
- **date:** 2026-08-18
- **angle:** Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders (no capital loss).
- **method:** Independent `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Full read of `LiveMt5Registration.cs` (94/94), `DealIngestionService` catalog/deal path, `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveBrokerProbe\Program.cs`, `CTraderFixOptions.cs`, `LiveRuntimeStatus.cs`. SDK header `MT5APIManager.h` (`MTManagerAPIVersion 5570`, `30 Jan 2026`). Live census from existing same-day probe JSON (group name rows only; no password keys). **This slot did not re-attach live.** No secrets printed.
- **verdict:** **PASS**
- **risk_to_capital:** **NONE**

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes. Exactly one product call site.** `GetGroupsCore` L155: `_manager.GroupRequestArray("*", arr)`. |
| Does it call `UserRequestArray`? | **Yes. Exactly one product call site.** `ReadAccountsForGroup` L223: `_manager.UserRequestArray(gname, users)`. |
| Are those the complete no-pump enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410. Request APIs hit the trade server. They do **not** require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`. |
| Is mask `*` the ALL-groups request? | **Yes.** `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809): comma templates, leading `!` exclude, `*` wildcard via `CheckGroupTemplate`. Mask `*` = every group this **manager ACL** may see. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62: `GetAccountsAsync(null)` → `GetAccountsCore(null)` walks **every** name from `GetGroupsCore()`. |
| Are both owned brokers on this type? | **Yes.** `LiveMt5Registration.CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector` instances: `BrokerCodes.Achiever` (`"ACHIEVER"`, optional HTTP proxy) + `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`, `ProxyEnabled = false`). |
| Live measured census (same-day, not re-run here)? | **ACHIEVER 8 / 6512; STARWAVEFX 10 / 1948. Total 18 groups / 8460 traders / 1984 open positions.** Path = this connector. Artifact `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, `utc` `2026-08-18T08:42:16.8519545+00:00`). Zero `password` keys. |
| Can copy-to-cTrader send a live order from this process? | **No.** `CTraderFixSession.BuildLogon` emits only `(35, "A")`. Product C# under `D:\Prop\src` has **0** `35=D` string literals. `RealCopyEnabled` forced `false` in DI and again after FIX logon. |
| Risk to capital? | **NONE.** Manager **read** APIs + FIX Logon only. Connector has 0 hits for `DealerSend` / `OrderSend` / `TradeRequest` / `UserUpdate` / `DealerBalance`. |

Stale report `A001_native_connector.md` (groups = cache `GroupTotal`/`GroupNext` only; “zero `GroupRequestArray` under `src`”) is **wrong for the current 458-line file**. Current source has both request calls as **primary** walks. Use this file (or any post-A014 W500 slot that re-read the same lines), not A001.

## 2. Search inventory (this slot)

`grep` `GroupRequestArray|UserRequestArray` on product sources:

| Path | Hits | Role |
|---|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L155 | `GroupRequestArray("*", arr)` | **Product primary group enumerator** |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L223 | `UserRequestArray(gname, users)` | **Product primary user enumerator** |
| `D:\Prop\src` (all other `*.cs`) | **0 additional** | No second walk, no plan-map wrapper |
| `D:\Prop\mt5-sdk\src` | **0** | Local C++ wrapper never calls either |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L212 | `IMTManagerAPI` | Vendor contract (groups) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L410 | `IMTManagerAPI` | Vendor contract (users) |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L1173 | `IMTAdminAPI` | Unused (connector field is `CIMTManagerAPI?`) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` | **0** | Old wrapper never calls either |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | L212 / L410 / L1173 | Same vendor header |

`GroupRequestArray` exists **only** on `IMTManagerAPI` (L212). `IMTAdminAPI` (class starts L785) has `GroupTotal` / `GroupNext` / `GroupGet` at L910–912 and **no** `GroupRequest` / `GroupRequestArray`. Stay on Manager. Do not “fix” completeness by switching to Admin.

`Take(` under `D:\Prop\src\Mt5`: **0**. Residual `Take(200)` is `GET /api/trades` only (`apps/api/Program.cs` L107) — reconstructed-trade page, not the Manager census.

## 3. SDK contract (what ALL means)

Vendor header `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h`:

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

```211:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```407:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   //--- clients and trade accounts request
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

Sibling request / cache APIs used as **fallbacks** in the same C# file:

| API | Header | Used? |
|---|---|---|
| `GroupTotal` / `GroupNext` | 205–206 | Cache fallback **only if** `GroupRequestArray` left `list.Count == 0` |
| `UserGetByGroup` | 672 | If `UserRequestArray` is not `OK` / `OK_NONE` / `NOTFOUND` |
| `UserLogins` | 254 | If `users.Total() == 0` after the above |
| `UserRequestByLogins` | 671 | After `UserLogins` returns a non-empty array |
| `UserAccountRequestArray` | 411 | Always attempted after users (balance/equity) |
| `UserAccountGetByGroup` | 742 | If account-array request is not `OK` / `OK_NONE` |
| `DealRequest` / `DealRequestByGroup` | 284 / 520 | Deal ingest (read) |
| `PositionRequest` / `PositionRequestByGroup` | (login) / 534 | Position ingest (read) |

Pump bits (`EnPumpModes` L125–144): `PUMP_MODE_USERS=0x1`, `PUMP_MODE_POSITIONS=0x80`, `PUMP_MODE_GROUPS=0x100`. Connector `ConnectCore` L89–111 tries `GROUPS|USERS|POSITIONS` first; on any non-OK, retries `PUMP_MODE_NONE` (C# wrapper name for 0). Fetch completeness does **not** depend on pump succeeding: the walk is the request APIs.

**ALL in this product = manager-ACL-visible.** Groups outside the manager record are invisible by design. Do not invent a second plan-map filter. Do not treat YoPips `GetAllGroups` (`GroupTotal`+`GroupNext` only) as the ALL-groups collector.

Mask language (`CMTStr::CheckGroupMask`, `MT5APIStr.h` L775–809) is the same language used by Administrator “Groups” on the manager and by the `group` argument of `UserLogins` / `UserRequestArray`. A lone `*` matches every visible group name.

## 4. Connector — groups

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). Type implements `IMt5BrokerConnector` + `IMt5BulkDealReader` + `IMt5BulkPositionReader`. Field is `CIMTManagerAPI? _manager`.

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

Measured properties of this walk:

- Mask is the literal `"*"`, **not** a plan list, **not** `MT5_DEFAULT_GROUP`, **not** a contest-only filter.
- Dedup is name-only (`HashSet` ordinal-ignore-case). Empty names skipped.
- `AddGroup` (L368–381) copies name / currency / digits / company / margin-call / stop-out / connections-allowed. It does **not** drop disabled groups.
- Cache `GroupTotal`/`GroupNext` runs **only** when the request left `list.Count == 0`. A non-empty but truncated request result is accepted as complete (honesty gap §10.2).
- Failed request (not `OK` / `OK_NONE`) is swallowed; then cache is used only if the list is still empty.

Connect (for context; request APIs work either pump state):

```88:111:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var endpoint = $"{_opt.Server}:{_opt.Port}";
            var pump = CIMTManagerAPI.EnPumpModes.PUMP_MODE_GROUPS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_USERS
                       | CIMTManagerAPI.EnPumpModes.PUMP_MODE_POSITIONS;
            var res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, pump, 30000);
            if (res == MTRetCode.MT_RET_OK)
            {
                _connected = true;
                _pumpEnabled = true;
                LastError = null;
                return;
            }

            res = _manager.Connect(endpoint, _opt.Login, _opt.Password, null, CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE, 30000);
```

Achiever may `ProxySet` HTTP (`ApplyProxy` L115–129) when `ACHIEVER_PROXY_ENABLED` is true. Starwave `ProxyEnabled` is hardcoded `false` in `LiveMt5Registration` L45. Proxy address / credentials are **not** printed here.

## 5. Connector — traders

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

`GetAccountsAsync(null)` is the ALL-traders entry. `IMt5BrokerConnector` (`Mt5Contracts.cs` L60) takes `string? group`; null/whitespace means “every group just discovered.”

Per group:

1. **Primary:** `UserRequestArray(gname)` — network, full `IMTUser` records.
2. **If retcode is not OK / OK_NONE / NOTFOUND:** cache `UserGetByGroup`.
3. **If `users.Total() == 0`:** `UserLogins` + `UserRequestByLogins`.
4. **Always:** `UserAccountRequestArray` (fallback `UserAccountGetByGroup`) to attach balance/equity. Missing account row → balances 0; the login is **still** emitted.

No `Take`. No `i < 200`. Dedup is by login across groups. Empty groups contribute **zero rows** but are still present in the group list (measured: `demo\yo-instant` = 0, three Starwave real groups = 0). That proves the group walk is not “only groups that have traders.”

## 6. Who calls this (ALL Achiever + Starwave)

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD + ACHIEVER_PROXY_*
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_* ; ProxyEnabled = false
            ...
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

DI (`DependencyInjection.cs` L35–48):

- Throws if either password is missing / `<SECRET>` / `(a/c` placeholder. Dummy/fake broker data is **disabled**.
- Registers **only** the two native connectors. `FakeMt5BrokerConnector` exists on disk (`CreateDefault` with 10001/10002) but is **not** constructed by DI. `DemoSeeder` is **not** called from `apps/api/Program.cs` (0 hits under `D:\Prop\apps`).
- `RealCopyEnabled = false` with comment: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”

Ingest (`DealIngestionService`):

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` iterates `registry.All()` (both brokers), connects, then `SyncCatalogAsync` → those two calls. Hosted loop never substitutes dummy data on failure (`No dummy data will be substituted.` L70).

Probe (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L19–29) uses the **same** factory and the **same** `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. That is the measured census path.

Call graph:

```
LiveMt5Registration.CreateConnectors
  ├─ NativeMt5BrokerConnector ACHIEVER   (optional HTTP proxy)
  └─ NativeMt5BrokerConnector STARWAVEFX (direct)
        │
        ├─ ConnectCore  (pump GROUPS|USERS|POSITIONS, else PUMP_MODE_NONE)
        │
        └─ GetAccountsAsync(null)
              → GetGroupsCore()                 // GroupRequestArray("*") then cache if empty
              → for each name:
                   1. UserRequestArray(name)    // network full user records
                   2. UserGetByGroup            // only on hard fail
                   3. UserLogins + RequestByLogins  // only if still empty
                   4. UserAccountRequestArray   // balances
```

## 7. YoPips C++ is **not** this enumerator

`grep` `GroupRequestArray|UserRequestArray` under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`: **0**. Same 0 under `D:\Prop\mt5-sdk\src`.

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    ...
    uint32_t total = m_manager->GroupTotal();
    ...
    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
```

`GetUserLogins` is `UserLogins` only (`mt5_manager.cpp` L315–327). After a no-pump connect that group cache can be empty while groups exist. **Do not use the C++ wrapper as the ALL-groups collector.** Prop C# is the path that implements A39’s request-API first.

## 8. Live census (prior probe, same connector, not re-run)

Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` — `CreateConnectorsFromEnvironment()` → `ConnectAsync` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Writes `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`. Note field: `"Passwords never written. Groups and manager logins only."` Grep `password` in that JSON: **0**.

This slot re-read the JSON **header + groupName rows only** (not the full trader dump). Counts match `LIVE_MANAGER_FETCH_MEASURED.md` and `CREDENTIALS_AND_COPY_STATUS.md`.

| Broker | Connect | Groups | Traders | Open positions | elapsedMs |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true (HTTP proxy) | 8 | 6512 | 1506 | 7212.5885 |
| STARWAVEFX | true (direct) | 10 | 1948 | 478 | 6413.478 |
| **Total** | | **18** | **8460** | **1984** | |

Achiever group account sums (re-added this slot): `2+179+4+5+4+6295+0+23 = 6512`.
Starwave group account sums: `11+4+170+1735+22+0+0+4+0+2 = 1948`.

| ACHIEVER | n | STARWAVEFX | n |
|---|---:|---|---:|
| contest\yo-1step | 2 | Starwave\cent\FX1\grp1 | 11 |
| contest\yo-2step | 179 | Starwave\cent\FX1\grp2 | 4 |
| contest\yo-instant | 4 | Starwave\demo\FX2\grp1 | 170 |
| contest\yo-payp | 5 | Starwave\demo\FX2\grp2 | 1735 |
| demo\yo-1step | 4 | Starwave\real\FX3\grp1 | 22 |
| demo\yo-2step | 6295 | Starwave\real\FX3\grp2 | 0 |
| demo\yo-instant | 0 | Starwave\real\FX3\grp3 | 0 |
| demo\yo-payp | 23 | Starwave\real\FX3\grp4 | 4 |
| | | Starwave\real\FX3\grp5 | 0 |
| | | Starwave\real\FX3\LP | 2 |

These are **all groups those two manager logins can see**. If either trade server has more groups, they are outside this manager permission set.

`LIVE_MANAGER_FETCH_MEASURED.md` records the same totals and the same path string: `GroupRequestArray` + `UserRequestArray`. Dashboard `/api/traders` returned **8460**; `/api/groups` returned **18** (same-day status doc).

## 9. Copy to cTrader — no live orders

| Gate | Evidence |
|---|---|
| FIX outbound MsgType | `CTraderFixSession.BuildLogon` L96: `(35, "A")` only. One `ssl.WriteAsync` of that logon (L49). Socket disposed after one read. |
| `35=D` / `NewOrderSingle` builder in `D:\Prop\src` | **0** string hits. Comments/log strings only: “NewOrderSingle still disabled.” |
| `RealCopyExecutionEnabled` | `CTraderFixOptions` L35 default **false**. |
| DI | `DependencyInjection.cs` L38–41: `RealCopyEnabled = false`. |
| After FIX logon | `CTraderFixLogonHostedService.cs` L68: `_runtime.RealCopyEnabled = false` again. |
| Settings API | `FEATURE_COPY_TRADING_ENABLED` literal **false** (`Program.cs` L76); `REAL_COPY_EXECUTION_ENABLED` bound to `runtime.RealCopyEnabled` (forced false). |
| FIX worker | `GetValue("CTrader:RealCopyExecutionEnabled", false)` — default false; worker still has **no** NewOrderSingle builder. |
| Runtime snapshot | `LiveRuntimeStatus.Snapshot` L42–43: when false, `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |
| Manager write APIs on connector | `DealerSend` / `OrderSend` / `TradeRequest` / `UserUpdate` / `DealerBalance`: **0** hits in `NativeMt5BrokerConnector.cs`. Reads: `DealRequest` / `DealRequestByGroup` / `PositionRequest` / `PositionRequestByGroup`. |

Fetching 8460 logins cannot open a cTrader position. Ingest writes catalog/deals/scores only. Copy intents stay SHADOW.

## 10. Honesty / residual gaps (do not greenwash)

1. **This slot did not live-attach.** Counts are the 2026-08-18T08:42Z probe. Source still matches that probe’s call path (`GetGroupsAsync` + `GetAccountsAsync(null)`).
2. **Partial `GroupRequestArray` is treated as complete.** Cache fallback runs only when `list.Count == 0`. A truncated non-empty array is not compared to `GroupTotal()`.
3. **Partial `UserRequestArray` is treated as complete.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returns some-but-not-all users, missing logins are not recovered.
4. **ALL = ACL, not “every group on the box.”** Manager permission is the ceiling.
5. **Empty groups are kept** (`demo\yo-instant` = 0, three Starwave real groups = 0). That is correct for ALL-groups; it is not a trader.
6. **Pump bit of the measured run is not recorded** in `LIVE_GROUPS_AND_TRADERS.json`. The fetch APIs are request APIs either way.
7. **A001 is stale.** Do not copy it for this question.
8. **YoPips `GetAllGroups` is still cache-only.** Do not cite a C++ `total: 0` as “broker has no groups.”
9. **This is not “copy trading live” and not EX5 decompile.** It is a Manager census + FIX logon-only dest.

## 11. Verdict

`NativeMt5BrokerConnector` **does** call `GroupRequestArray("*")` and per-group `UserRequestArray` as the primary ALL enumerators for Achiever and Starwave. Ingest and `LiveBrokerProbe` both pass `GetAccountsAsync(null)`. No `Take`, no plan-map filter, no FakeMt5 substitution on this type. Same-day live probe: **18 groups / 8460 traders**. Copy-to-cTrader cannot place live orders (`35=A` only; `RealCopyEnabled=false`). Risk to capital: **NONE**.

## 12. Files read

| Path | Why |
|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Assigned type (full 458) |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two native connectors |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog loop; no dummy fallback |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | `IMt5BrokerConnector` / bulk ports |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fail-closed + `RealCopyEnabled=false` |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Re-pins `RealCopyEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copy note |
| `D:\Prop\apps\api\Program.cs` | Settings flags; no DemoSeeder |
| `D:\Prop\apps\fix-worker\Worker.cs` | Default-false flag read |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Census producer |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor contract 5570 |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | Mask language |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Cache-only `GetAllGroups` |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Counts + group names (no passwords) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Same-day measured totals |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Copy-off status (names only) |
