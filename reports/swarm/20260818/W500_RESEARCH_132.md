# W500_RESEARCH_132 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 132
- **date:** 2026-08-18
- **angle:** Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy-to-cTrader must **not** send live orders (no capital loss).
- **method:** `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). No secrets printed. This slot did **not** re-run live Connect; live counts are re-summed from the existing probe artifact.
- **verdict:** **PASS**
- **risk to capital:** **NONE** (`SAFE_BY_ABSENCE` of `35=D` / `NewOrderSingle`)

## 1. Question and answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes.** Primary group walk: `_manager.GroupRequestArray("*", arr)` (`GetGroupsCore` L155). |
| Does it call `UserRequestArray`? | **Yes.** Primary user walk per group: `_manager.UserRequestArray(gname, users)` (`ReadAccountsForGroup` L223). |
| Are those the no-pump complete enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410 (`MT5APIManager.h`, API **5570**, 30 Jan 2026). Mask `*` = every group this manager ACL may see. |
| Does fetch branch on pump? | **No.** `_pumpEnabled` is write-only (L30/36/96/110/140). `GetGroupsCore` / `ReadAccountsForGroup` never read it. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` / `SyncBrokerAsync` call `GetAccountsAsync(null)` → `GetAccountsCore` walks **every** name from `GetGroupsCore`. |
| Are Achiever + Starwave both registered? | **Yes.** `LiveMt5Registration.CreateConnectors` builds exactly two `NativeMt5BrokerConnector` instances (`BrokerCodes.Achiever`, `BrokerCodes.StarwaveFx`). Fake is **not** registered. |
| Live measured census (prior probe, same day)? | **ACHIEVER 8 / 6512 (HTTP proxy, 7213 ms); STARWAVEFX 10 / 1948 (direct). Total 18 groups / 8460 traders / 1984 open positions.** Artifact UTC `2026-08-18T08:42:16Z`. |
| Any `Take` / plan-map / dummy on this type? | **No.** `Take(` / plan-map / dummy: **0** hits in `NativeMt5BrokerConnector.cs`. Empty groups are kept (`demo\yo-instant` = 0). |
| Does copy-to-cTrader send live orders? | **No.** `CTraderFixSession` emits only `(35, "A")` Logon. Product `src` `35=D` = **0**. `NewOrderSingleImplemented = false`. `AllowFixSend` hardcoded `false`. |
| Risk to capital from this path? | **None.** Manager read APIs + FIX logon only. No `DealerSend` / `OrderSend` / `NewOrderSingle`. |

Stale reports that claimed “zero `GroupRequestArray` hits under `D:\Prop\src`” (`A001_native_connector.md`) are **wrong for the current file**. Current source has both request calls.

Stale reports that claimed `RealCopyEnabled` is **process-pinned false** (`A014`/`A015`/`W500_RESEARCH_108`) are **wrong for current DI**. See §7.

## 2. SDK contract (what ALL means)

Vendor header (same text in Prop vendor tree and YoPips SDK copy):

```11:12:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
#define MTManagerAPIVersion  5570
#define MTManagerAPIDate     L"30 Jan 2026"
```

```212:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```408:411:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

```254:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```672:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

YoPips header twins (same line numbers):

- `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` L212 / L254 / L410 / L672 / L1173.

| API | Header line | Kind | Pump required? | Product C# uses? |
|---|---:|---|---|---|
| `GroupRequestArray(mask)` | 212 | **Network** — ALL groups this manager ACL allows | **No** | **Yes — first** (`"*"`) |
| `GroupTotal` / `GroupNext` | 205–206 | **Cache** (`PUMP_MODE_GROUPS`) | Yes for completeness | Fallback only if request list empty |
| `UserRequestArray(group)` | 410 | **Network** — full user records | **No** | **Yes — first** per group |
| `UserGetByGroup` | 672 | **Cache** (`PUMP_MODE_USERS`) | Yes | Hard-fail fallback only |
| `UserLogins` + `UserRequestByLogins` | 254 / 671 | **Network** | **No** | Empty-array fallback |
| `UserAccountRequestArray` | 411 | **Network** balances | **No** | Yes, then cache `UserAccountGetByGroup` |
| `DealRequestByGroup` | 520 | **Network** | **No** (`PUMP_MODE_DEALS` does not exist) | Yes (`GetGroupDealsCore` L307) |
| `PositionRequestByGroup` | 534 | **Network** | **No** | Yes (`GetGroupPositionsCore` L344); cache `PositionGetByGroup` on hard fail |

- Interface used by the product: **`IMTManagerAPI`** (class starts L121). Request APIs hit the **server**.
- `IMTAdminAPI` (class starts L785) has `GroupTotal`/`GroupNext`/`GroupGet` (L910–912) but **no** `GroupRequest` / `GroupRequestArray`. Admin pump enum is MAIL/NEWS only (L790–794). Second `UserRequestArray` at header L1173 is on **Admin**, unused by this C# connector (`CIMTManagerAPI` only). `UserGetByGroup` is **Manager-only** (L672).
- Mask language (`CMTStr::CheckGroupMask`, A39): `*` = all groups this manager may see. Same mask language on `UserRequestArray` / `UserLogins`.
- **ALL in this product = manager-ACL-visible.** Server groups outside the manager record are invisible by design. Do not invent a second plan-map filter.

YoPips C++ product (`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`) **does not call** `GroupRequestArray` or `UserRequestArray` (**0** hits under `src\`). `MT5Manager::GetAllGroups` is cache `GroupTotal`/`GroupNext` only (`mt5_manager.cpp` L962–981). `GetUserLogins` is `UserLogins` only (L315–327). Pool sessions connect `pump_mode=0` (`mt5_pool.cpp` L74–76) and still only walk the cache for groups. That is the **old** wrapper. The Prop C# connector is the path that implements A39’s request-API enumerator.

## 3. Connector — groups (`GroupRequestArray`)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). Implements `IMt5BrokerConnector`, `IMt5BulkDealReader`, `IMt5BulkPositionReader`.

Connect tries pump `GROUPS|USERS|POSITIONS` (`0x00000181`) first, then `PUMP_MODE_NONE`:

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
            // ...
            _connected = true;
            _pumpEnabled = false;
```

Group walk is **request-first**. Cache is used only when the request array is empty:

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

Measured implications:

- Mask is literal `"*"`, not a plan list (`MT5_GROUP_*` unused here).
- Dedup is name-case-insensitive (`HashSet` + `OrdinalIgnoreCase`).
- Empty-name groups are skipped (`AddGroup` L369–372).
- If `GroupRequestArray` returns a hard error, the array stays empty and the cache walk still runs. Completeness then depends on `PUMP_MODE_GROUPS` having filled the cache. The live probe returned 18 named groups, so the request path produced a non-empty list on that run (cache fallback was not needed).

## 4. Connector — traders (`UserRequestArray`)

`GetAccountsAsync(null)` = walk **every** group from `GetGroupsCore`, then per-group users. No cap.

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

Per-group user walk is **request-first**:

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

Balances: `UserAccountRequestArray` first; cache `UserAccountGetByGroup` only on hard fail (L235–237). Users with no account row still land with balance/equity 0.

`UserGetByGroup` is **not** the ALL-traders path. It is pump-cache and is reached only when `UserRequestArray` hard-fails. `NOTFOUND` / `OK_NONE` do **not** fall into cache (those are treated as empty-but-successful). Empty then goes to `UserLogins` (network).

## 5. Dual-broker registration + ingest callers

```20:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        // ...
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / PORT / LOGIN / PASSWORD
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            // ...
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

Password **values are not quoted**. Gate: `HasRealPasswords` requires both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` non-empty and not a `<SECRET>` / `(a/c` placeholder. `AddTraderIntelligence` throws before `CreateConnectors` if that fails (`DependencyInjection.cs` L36–37). `FakeMt5BrokerConnector` exists (`FakeMt5BrokerConnector.cs`) but is **not** registered on the live host path.

Ingest (`DealIngestionService.cs` L45–48 and L61–62):

```
GetGroupsAsync → UpsertGroupsBatchAsync
GetAccountsAsync(null) → UpsertAccountsBatchAsync
```

`LiveIngestHostedService` calls `SyncCatalogAsync` per registered connector (L56). `LiveBrokerProbe` (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L19–26) uses the **same** factory + `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. Probe note: `"Passwords never written."`

## 6. Live census (re-summed, not re-attached)

Source: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `LiveBrokerProbe` at **2026-08-18T08:42:16.8519545+00:00**. `envLoaded=true`. This slot did **not** reconnect.

JSON totals: Achiever `groups=8` `accounts=6512` `openPositions=1506` `elapsedMs=7212.5885`. Starwave `groups=10` `accounts=1948` `openPositions=478`.

Re-sum of per-group `accounts` fields (this slot):

| Broker | Group | Accounts |
|---|---|---:|
| ACHIEVER | `contest\yo-1step` | 2 |
| ACHIEVER | `contest\yo-2step` | 179 |
| ACHIEVER | `contest\yo-instant` | 4 |
| ACHIEVER | `contest\yo-payp` | 5 |
| ACHIEVER | `demo\yo-1step` | 4 |
| ACHIEVER | `demo\yo-2step` | 6295 |
| ACHIEVER | `demo\yo-instant` | **0** |
| ACHIEVER | `demo\yo-payp` | 23 |
| ACHIEVER | **sum** | **6512** |
| STARWAVEFX | `Starwave\cent\FX1\grp1` | 11 |
| STARWAVEFX | `Starwave\cent\FX1\grp2` | 4 |
| STARWAVEFX | `Starwave\demo\FX2\grp1` | 170 |
| STARWAVEFX | `Starwave\demo\FX2\grp2` | 1735 |
| STARWAVEFX | `Starwave\real\FX3\grp1` | 22 |
| STARWAVEFX | `Starwave\real\FX3\grp2` | **0** |
| STARWAVEFX | `Starwave\real\FX3\grp3` | **0** |
| STARWAVEFX | `Starwave\real\FX3\grp4` | 4 |
| STARWAVEFX | `Starwave\real\FX3\grp5` | **0** |
| STARWAVEFX | `Starwave\real\FX3\LP` | 2 |
| STARWAVEFX | **sum** | **1948** |
| **both** | **18 groups** | **8460** |

Empty groups still appear. That is evidence the enumerator is group-config (`GroupRequestArray`), not “groups that happen to have users.”

JSON does **not** record `PumpEnabled`. Fetch is request-first, so the census is valid whether pump succeeded. Independent no-pump proof: vendor request APIs + YoPips pool `Connect(..., 0)` still calling `UserLogins` + C# fallback `PUMP_MODE_NONE`.

Related pin: `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` (same 8/6512 + 10/1948). Dashboard `/api/traders` was reported **8460** in `CREDENTIALS_AND_COPY_STATUS.md`. This slot did not re-hit HTTP.

## 7. Copy to cTrader — no live orders (no loss)

Measured send surface:

| Check | Result |
|---|---|
| `CTraderFixSession.BuildLogon` | Only outbound MsgType is `(35, "A")` (`CTraderFixSession.cs` L96). One `WriteAsync` (L49). Socket disposed. |
| `35=D` under `D:\Prop\src` | **0** hits |
| `35=D` under `D:\Prop\src\Fix.CTrader` | **0** |
| `NewOrderSingle` product sender | **None.** `CopyTradingService.NewOrderSingleImplemented = false` (const, L16). |
| `VenueReconciled` | const **false** (L15). |
| `AllowFixSend` | Hardcoded **false** on every `RiskDecisionRecord` (L192). |
| Live-send branch | Dead: needs `AllowFixSend && LIVE && NewOrderSingleImplemented && VenueReconciled` (L198). Else status `SHADOW_ONLY`. |
| Native Manager writes | `DealerSend` / `OrderSend` / `TradeRequest` / `UserUpdate` / `DealerBalance` in `NativeMt5BrokerConnector.cs`: **0**. Reads only: `DealRequest` / `DealRequestByGroup` / `PositionRequest` / `PositionRequestByGroup`. |
| YoPips C++ `src` cTrader / `35=D` / `NewOrderSingle` | **0** hits. |
| Copy hosted service | Creates SHADOW intents only (`CopyTradingHostedService.cs` L30). |

Honesty on the env flag (do **not** copy stale “pinned false” claims):

- `D:\Prop\.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (boolean only; no other env values printed).
- Current `DependencyInjection.cs` L41 **binds** that key: `RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", ...)`.
- `CTraderFixLogonHostedService` L68–70 **logs** `RealCopyArmed` and does **not** re-pin false (A015 L224 is stale).
- `CTraderFixOptions.RealCopyExecutionEnabled` still defaults **false** (L35) and is **not** the process choke.
- `LiveRuntimeStatus.Snapshot` copyNote when armed: `"REAL_COPY armed. NewOrderSingle still unimplemented; 0 LIVE traders; venue not reconciled. No ticket will be sent."` (L42–43).

So the operator flag may be **true** and the runtime boolean may follow it. That still **cannot** emit a ticket: there is no `35=D` assembler. Safety is **`SAFE_BY_ABSENCE`**, not a process pin.

## 8. What this slot did **not** do

- Did **not** re-attach Manager or FIX.
- Did **not** print passwords, proxy auth, FIX password, or account secrets.
- Did **not** edit product source.
- Did **not** claim groups outside the two manager ACLs.
- Did **not** claim “EX5 decompiled” or copy-trading live.

## 9. Verdict

`NativeMt5BrokerConnector` **does** use `GroupRequestArray("*")` (L155) and per-group `UserRequestArray` (L223) as the primary ALL enumerators for both Achiever and Starwave. Ingest and `LiveBrokerProbe` ask `GetAccountsAsync(null)`. There is no `Take`, no plan-map filter, no dummy substitution on this type. `_pumpEnabled` is not a fetch gate. A same-day live probe recorded **18** manager-visible groups and **8460** traders (re-summed this slot). Copy-to-cTrader cannot place live orders from this process (`35=A` only; `NewOrderSingleImplemented=false`; `AllowFixSend=false`).

**PASS.** Risk to capital: **NONE**.

## 10. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Assigned type (full 458) |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Dual Native connectors |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fail-closed Native ×2; REAL_COPY bind |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Hosted catalog |
| `D:\Prop\src\Infrastructure\Copy\CopyTradingService.cs` | SHADOW only; no NOS |
| `D:\Prop\src\Infrastructure\Hosting\CopyTradingHostedService.cs` | Shadow tick |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Logon, no send |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | POCO default false |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Snapshot copyNote |
| `D:\Prop\apps\api\Program.cs` | `/api/settings` flag from runtime |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Exists; unused by DI |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Same request walk |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor contract 5570 |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | Twin header |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | Cache `GetAllGroups`; `UserLogins` |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_pool.cpp` | `Connect(..., 0)` |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Census artifact |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Census pin |
| `D:\Prop\reports\CREDENTIALS_AND_COPY_STATUS.md` | Status (names only) |
