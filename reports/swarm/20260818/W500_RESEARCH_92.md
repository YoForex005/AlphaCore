# W500_RESEARCH_92 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 92
- **date:** 2026-08-18
- **angle:** Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders yet (no capital loss).
- **method:** Independent `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Full read of `LiveMt5Registration.cs` (94/94), `DealIngestionService` catalog path, `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `LiveIngestHostedService.cs`, `LiveBrokerProbe/Program.cs`. SDK `MT5APIManager.h` (API 5570, 30 Jan 2026). Live census re-checked from `LIVE_GROUPS_AND_TRADERS.json` (utc `2026-08-18T08:42:16.8519545+00:00`). No secrets printed. **This slot did not re-attach live.**
- **verdict:** **PASS**
- **risk_to_capital:** **NONE**

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes. One call site.** `GetGroupsCore` L155: `_manager.GroupRequestArray("*", arr)`. |
| Does it call `UserRequestArray`? | **Yes. One call site.** `ReadAccountsForGroup` L223: `_manager.UserRequestArray(gname, users)`. |
| Are those the complete no-pump enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410. Request APIs hit the trade server. They do **not** require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`. |
| Is mask `*` the ALL-groups request? | **Yes.** `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809): comma templates, leading `!` exclude, `*` wildcard. Mask `*` = every group this **manager ACL** may see. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62: `GetAccountsAsync(null)` → `GetAccountsCore` walks **every** name from `GetGroupsCore`. |
| Are both owned brokers on this type? | **Yes.** `LiveMt5Registration.CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances: `BrokerCodes.Achiever` (optional HTTP proxy) + `BrokerCodes.StarwaveFx` (proxy off). |
| Is FakeMt5 / dummy 10001 on the live path? | **No.** `DependencyInjection` throws unless both real MT5 passwords exist. `CreateConnectors` never constructs `FakeMt5BrokerConnector`. `grep` `FakeMt5BrokerConnector` under `D:\Prop\src\*.cs` = only the Fake type + `DemoBrokerFactory` (tests/seed), **not** DI. |
| `Take` / plan-map cut on this walk? | **None.** Zero `Take(`/`Skip(` in `NativeMt5BrokerConnector.cs` and `DealIngestionService.cs`. |
| Live measured census (same-day artifact, not re-run here)? | **ACHIEVER 8 / 6512 / 1506; STARWAVEFX 10 / 1948 / 478. Total 18 groups / 8460 traders / 1984 open positions.** Path = this connector via `LiveBrokerProbe`. Artifact `LIVE_GROUPS_AND_TRADERS.json`. Zero `password` keys. |
| Can copy-to-cTrader send a live order from this process? | **No.** `CTraderFixSession.BuildLogon` emits only `(35, "A")`. Zero `35=D` / NewOrderSingle builders under `D:\Prop\src\Fix.CTrader`. `RealCopyEnabled` forced `false` in DI and again after FIX logon. |
| Risk to capital? | **NONE.** Manager **read** APIs + FIX Logon only. Connector has 0 hits for `DealerSend` / `OrderSend` / `TradeRequest` / `UserUpdate` / `DealerBalance` / `UserAdd` / `UserDelete`. |

Stale report `A001_native_connector.md` (groups = cache `GroupTotal`/`GroupNext` only; “zero `GroupRequestArray` under `src`”) is **wrong for the current 458-line file**. Current source has both request calls as **primary** walks.

## 2. Search inventory (this slot)

`grep` `GroupRequestArray|UserRequestArray` under product `*.{cs,h,cpp}`:

| Path | Hits | Role |
|---|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L155 | `GroupRequestArray("*", arr)` | **Product primary group enumerator** |
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` L223 | `UserRequestArray(gname, users)` | **Product primary user enumerator** |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L212 | `IMTManagerAPI` | Vendor contract |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L410 | `IMTManagerAPI` | Vendor contract |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` L1173 | `IMTAdminAPI` | Unused (connector is `CIMTManagerAPI` only) |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` | **0** | Old wrapper never calls either |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | L212 / L410 / L1173 | Same vendor header (API 5570) |

`GroupRequestArray` exists **only** on `IMTManagerAPI` (L212). `IMTAdminAPI` has `GroupTotal` / `GroupNext` / `GroupGet` at L910–912 and **no** `GroupRequest` / `GroupRequestArray`. Stay on Manager.

## 3. SDK contract (what ALL means)

Vendor header `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (`MTManagerAPIVersion 5570`, `MTManagerAPIDate L"30 Jan 2026"`). Same signatures in the YoPips copy of the header.

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

```254:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```671:672:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
```

Sibling request APIs used as **fallbacks** in the same C# file:

| API | Header | Used by C#? |
|---|---|---|
| `GroupTotal` / `GroupNext` | 205–206 | Cache fallback **only if** `GroupRequestArray` left `list.Count == 0` |
| `UserGetByGroup` | 672 | If `UserRequestArray` is not OK / OK_NONE / NOTFOUND |
| `UserLogins` | 254 | If `users.Total() == 0` after the above |
| `UserRequestByLogins` | 671 | After `UserLogins` returns a non-empty array |
| `UserAccountRequestArray` | 411 | Always attempted after users (balance/equity) |
| `UserAccountGetByGroup` | cache | If account-array request is not OK / OK_NONE |
| `DealRequest` / `DealRequestByGroup` | 520 | Yes — read history, not send |
| `PositionRequest` / `PositionRequestByGroup` | 534 | Yes — read snapshot, not send |

Pump bits (`EnPumpModes` L125–144): `PUMP_MODE_USERS=0x1`, `PUMP_MODE_POSITIONS=0x80`, `PUMP_MODE_GROUPS=0x100`. Connector `ConnectCore` tries `GROUPS|USERS|POSITIONS` first, then `PUMP_MODE_NONE` (C# wrapper name for 0; the C++ enum does **not** name `PUMP_MODE_NONE`). Fetch completeness does **not** depend on pump succeeding: the request APIs are the walk.

`Connect` signature (`MT5APIManager.h` L164) takes `pump_mode` independently of later `GroupRequestArray` / `UserRequestArray`. Those two are network request APIs.

**ALL in this product = manager-ACL-visible.** Groups outside the manager record are invisible by design. Do not invent a second plan-map filter. Do not treat YoPips `GetAllGroups` (`GroupTotal`+`GroupNext` only) as the ALL-groups collector.

Mask language (`CMTStr::CheckGroupMask`, `MT5APIStr.h` L775–809): comma-separated templates, leading `!` = exclude, `*` wildcards via `CheckGroupTemplate`. Same language on `UserRequestArray` / `UserLogins`. Connector uses `"*"` for groups and the **discovered group name** (not `*`) for per-group users.

## 4. Connector — groups (`GroupRequestArray`)

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

Measured behavior:

1. **Primary:** network `GroupRequestArray("*")` — mapping-blind, ACL-bounded ALL.
2. **Fallback:** local pump cache `GroupTotal`/`GroupNext` **only when the request list is empty**.
3. Dedup by name (`HashSet`, ordinal ignore-case).
4. Empty groups are kept (`AddGroup` does not drop zero-account groups; account counts come later).
5. Failed request (not OK / OK_NONE) is swallowed; cache is used only if `list.Count == 0`.

Connect recipe that makes the fallback safe when pump succeeds:

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

Achiever may `ProxySet` HTTP (`ApplyProxy` L115–129) when `ACHIEVER_PROXY_ENABLED` + host/port. Starwave `ProxyEnabled = false` at registration. Password values are not quoted here.

## 5. Connector — traders (`UserRequestArray`)

```189:270:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // group null/blank → every name from GetGroupsCore()
        // per group: ReadAccountsForGroup
        // dedup by login into Dictionary<ulong, Mt5AccountDto>
    }

    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        // 1. UserRequestArray(gname, users)          // network full records
        // 2. if not OK/OK_NONE/NOTFOUND → UserGetByGroup
        // 3. if users.Total()==0 → UserLogins + UserRequestByLogins
        // 4. UserAccountRequestArray (balance/equity), else UserAccountGetByGroup
        // 5. join user + account by login; no Take
    }
```

Exact request line:

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

`GetAccountsAsync` callers under `D:\Prop\src\*.cs`:

| Caller | Argument | Meaning |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L48 | `null` | ALL groups from `GetGroupsCore` |
| `DealIngestionService.SyncBrokerAsync` L62 | `null` | same |
| `LiveBrokerProbe/Program.cs` L26 | `null` | same (measured census) |
| Interface `IMt5BrokerConnector` L60 | optional `group` | single-group override unused by ingest |

There is no other production caller. Ingest cannot silently ask for one plan-mapped group.

## 6. Both brokers registered — Fake off

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD
            // ACHIEVER_PROXY_* optional
            NativeDllDirectory = dllDir
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_*
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
    }
```

`BrokerCodes`: `ACHIEVER` / `STARWAVEFX` (`D:\Prop\src\Domain\Brokers\BrokerCodes.cs`).

DI fail-closed:

```35:46:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

`HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` non-empty and not a `<SECRET>` / `(a/c` placeholder. Values are **not** printed here.

`DemoBrokerFactory.CreateDefault` still exists (`FakeMt5BrokerConnector` dummy logins 10001/10002/10003/99001). It is **not** registered by `AddTraderIntelligence`. Live ingest logs `"No dummy data will be substituted."` on catalog failure (`LiveIngestHostedService` L70).

Hosted catalog is one-shot after 2 s (`LiveIngestHostedService` L28, no `PeriodicTimer`). Both connectors from `registry.All()` run `SyncCatalogAsync` then `SyncBrokerAsync` (90-day deal window). Scoring walks `ListLoginsWithDealsAsync` only — catalog still stores every account from `GetAccountsAsync(null)`.

## 7. Live census (same-day artifact — not re-probed this slot)

File: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`

| Field | Measured |
|---|---|
| `probe` | `LiveBrokerProbe` |
| `utc` | `2026-08-18T08:42:16.8519545+00:00` |
| `envLoaded` | `true` |
| `note` | `Passwords never written. Groups and manager logins only.` |
| `password` keys in JSON | **0** (`grep` hit is the word “Passwords” in `note` only) |

Probe source (`D:\Prop\tools\LiveBrokerProbe\Program.cs`):

1. `LiveMt5Registration.CreateConnectorsFromEnvironment()` → same two `NativeMt5BrokerConnector`s.
2. `ConnectAsync` → `GetGroupsAsync` → `GetAccountsAsync(null)` → `GetGroupPositionsAsync("*")`.
3. Writes groups + login/group/leverage/balance/equity. **Does not write passwords.**

| Broker | Connected | Elapsed | Groups | Traders | Open positions |
|---|---|---:|---:|---:|---:|
| ACHIEVER | true | 7212.5885 ms | 8 | 6512 | 1506 |
| STARWAVEFX | true | 6413.478 ms | 10 | 1948 | 478 |
| **Total** | | | **18** | **8460** | **1984** |

Achiever groups (account counts sum to 6512):

| Group | Accounts |
|---|---:|
| `contest\yo-1step` | 2 |
| `contest\yo-2step` | 179 |
| `contest\yo-instant` | 4 |
| `contest\yo-payp` | 5 |
| `demo\yo-1step` | 4 |
| `demo\yo-2step` | 6295 |
| `demo\yo-instant` | 0 |
| `demo\yo-payp` | 23 |

Starwave groups (account counts sum to 1948):

| Group | Accounts | Currency |
|---|---:|---|
| `Starwave\cent\FX1\grp1` | 11 | USC |
| `Starwave\cent\FX1\grp2` | 4 | USC |
| `Starwave\demo\FX2\grp1` | 170 | USD |
| `Starwave\demo\FX2\grp2` | 1735 | USD |
| `Starwave\real\FX3\grp1` | 22 | USD |
| `Starwave\real\FX3\grp2` | 0 | USD |
| `Starwave\real\FX3\grp3` | 0 | USD |
| `Starwave\real\FX3\grp4` | 4 | USD |
| `Starwave\real\FX3\grp5` | 0 | USD |
| `Starwave\real\FX3\LP` | 2 | USD |

Arithmetic: Achiever `2+179+4+5+4+6295+0+23 = 6512`. Starwave `11+4+170+1735+22+0+0+4+0+2 = 1948`. Zero-account groups are **present** — empty ≠ omitted.

These are **all groups those two manager logins could see at probe time**. If Administrator later adds a group the manager ACL allows, a one-shot ingest (`LiveIngestHostedService` runs the catalog once after 2 s) will miss it until process restart / ops resync. That is a **host** hole, not a missing request API on the connector.

`LIVE_MANAGER_FETCH_MEASURED.md` records the same 8/6512 + 10/1948 and names the path `GroupRequestArray` + `UserRequestArray`.

## 8. YoPips C++ contrast (do not use as the ALL collector)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src` has **zero** `GroupRequestArray` / `UserRequestArray` hits.

`MT5Manager::GetAllGroups` (`mt5_manager.cpp` L962–981) walks **cache only**:

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    // GroupTotal + GroupNext only. No GroupRequestArray.
    uint32_t total = m_manager->GroupTotal();
    ...
}
```

Traders in YoPips: `GetUserLogins` → `UserLogins` only (L315–327). That is a request API for **login ids**, not full `UserRequestArray` records.

YoPips **does** implement `DealerSend` / `SendTrade` (`mt5_manager.cpp` L1119+, `mt5_pool.cpp` L370+). That is a **different process / different repo**. Prop C# `NativeMt5BrokerConnector` does not call those methods. Do not copy YoPips dealer send into this product.

## 9. Completeness holes (honest residuals — not FAIL on this slot)

The connector **shape** is ALL-manager-visible. Residual ways a row can still be missed:

1. **Partial `GroupRequestArray` accepted.** Cache `GroupTotal`/`GroupNext` runs only when `list.Count == 0`. A non-empty but truncated request result is treated as complete. No compare to `GroupTotal()`.
2. **Partial `UserRequestArray` accepted.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returns some but not all users, missing logins are not recovered. `UserGetByGroup` is skipped on `OK` / `OK_NONE` / `NOTFOUND`.
3. **ACL bound.** Groups/users outside the manager’s Administrator “Groups” mask never appear. Correct Manager semantics, not a product filter.
4. **Zero-account groups are kept.** Good — empty ≠ omitted.
5. **One-shot host.** `LiveIngestHostedService` has no periodic re-walk. New groups after start wait for restart/resync.
6. **This slot did not re-measure Connect.** Counts are from the 08:42 UTC probe, not a fresh run.

None of these holes send an order.

## 10. Copy to cTrader — no live orders (no loss)

`NativeMt5BrokerConnector` implements read ports only. `grep` `DealerSend|DealerBalance|TradeRequest|UserUpdate|OrderSend|UserAdd|UserDelete` in that file: **0 hits**. Reads: `DealRequest` / `DealRequestByGroup` / `PositionRequest` / `PositionRequestByGroup`.

FIX session builds **only** Logon (`35=A`). No NewOrderSingle:

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
```

`grep` `35=D` under `D:\Prop\src\Fix.CTrader`: **0 hits**. `NewOrderSingle` hits are comments / log text saying it is **disabled**. `FixSimulationHarness` builds inbound `35=8` ExecutionReport for tests only — not a live TRADE send.

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

`CTraderFixOptions.RealCopyExecutionEnabled` defaults **false** (`CTraderFixOptions.cs` L35) and is not bound from env to arm send. Snapshot copy note: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` (`LiveRuntimeStatus.cs` L42–43).

Tag 553 is the integer account id, not SenderCompID (`CTraderFixLogonHostedService` L45–46) — logon-only; still no `35=D`. TRADE socket may log on for recon; **logon ≠ order**.

## 11. Verdict

**PASS.**

`NativeMt5BrokerConnector` **does** use `GroupRequestArray("*")` and per-group `UserRequestArray` as the primary ALL enumerators for both Achiever and Starwave. Ingest and `LiveBrokerProbe` ask `GetAccountsAsync(null)`. There is no `Take`, no plan-map filter, no dummy substitution on this type. A same-day live probe recorded **18 manager-visible groups and 8460 traders** (Achiever 8/6512, Starwave 10/1948). Copy-to-cTrader cannot place live orders from this process (`35=A` only; `RealCopyEnabled=false`).

Residual (documented, not a capital-loss FAIL): partial request arrays are not cross-checked against `GroupTotal` / `UserLogins` length; ingest is one-shot; ALL is ACL-bounded; this slot did not re-run Connect.

**Risk to capital: NONE.**

## 12. Files read

| Path | Role |
|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Assigned type (full 458) |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | 2× connector factory (94) |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Live-only DI, copy flag off |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | One-shot catalog+deals |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Ports / DTOs |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | `RealCopyEnabled` snapshot |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Dummy factory — not in DI |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only (135) |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon + force copy off |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | `RealCopyExecutionEnabled=false` |
| `D:\Prop\src\Fix.CTrader\Testing\FixSimulationHarness.cs` | Test `35=8` only |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Census writer |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | 08:42Z census (no passwords) |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Same-day summary |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor `IMTManagerAPI` 5570 |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | `CheckGroupMask` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | Same request APIs |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Cache `GetAllGroups`; `UserLogins`; `DealerSend` exists **there** only |

No product source was modified. No secret values printed.
