# W500_RESEARCH_192 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 192
- **date:** 2026-08-18
- **angle:** Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders (no capital loss).
- **method:** Independent `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (**458/458**). Supporting reads: `LiveMt5Registration.cs` (**94/94**), `DealIngestionService.cs` (**146/146**, ingest class L28–99), `LiveIngestHostedService.cs` (**141/141**), `CTraderFixSession.cs` (**135/135**), `CTraderFixLogonHostedService.cs` (**112/112**), `CTraderFixDemoTestTrade.cs` (**391/391**), `CopyTradingService.cs` (**257/257**), `CopyTradingHostedService.cs` (**40/40**), `DependencyInjection.cs` (**62/62**), `LiveRuntimeStatus.cs`, `CTraderFixOptions.cs`, `LiveBrokerProbe\Program.cs` (**86/86**), `BrokerCodes.cs`, `RiskEngine.cs` persist tail, `apps\api\Program.cs` settings + `/api/trades`. Vendor header `MT5APIManager.h` (`MTManagerAPIVersion 5570`, `30 Jan 2026`) on both Prop vendor tree and YoPips SDK copy. Mask language `CMTStr::CheckGroupMask` in `Classes\MT5APIStr.h` L775–809. Live census **re-summed** from `LIVE_GROUPS_AND_TRADERS.json` (group-name rows only; logins not recopied). **This slot did not re-attach Manager or FIX.** No secrets printed.
- **verdict:** **PASS**
- **risk_to_capital:** **NONE** (`SAFE_BY_ABSENCE` of a product-host `35=D` sender)

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes. One product site.** `GetGroupsCore` L155: `_manager.GroupRequestArray("*", arr)`. |
| Does it call `UserRequestArray`? | **Yes. One product site.** `ReadAccountsForGroup` L223: `_manager.UserRequestArray(gname, users)`. |
| Are those the no-pump complete enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410. Request APIs hit the trade server. They do **not** require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`. Header has **0** `DealGet(` and **0** `PUMP_MODE_DEALS`. |
| Is mask `*` ALL groups this manager can see? | **Yes.** `CMTStr::CheckGroupMask` (`Classes\MT5APIStr.h` L775–809): comma templates, leading `!` exclude, wildcard via `CheckGroupTemplate`. Mask `*` = every group this **manager ACL** may see. |
| Does fetch branch on `_pumpEnabled`? | **No.** Field is written at L30/36/96/110/140 and exposed as `PumpEnabled`. `GetGroupsCore` / `ReadAccountsForGroup` never read it. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62: `GetAccountsAsync(null)` → `GetAccountsCore(null)` walks **every** name from `GetGroupsCore()`. |
| Are Achiever + Starwave both on this type? | **Yes.** `LiveMt5Registration.CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector`s: `BrokerCodes.Achiever` (`"ACHIEVER"`, optional HTTP proxy) + `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`, `ProxyEnabled = false` L45). |
| Live measured census (same-day, not re-run)? | **ACHIEVER 8 / 6512 / 1506 (HTTP proxy, 7212.5885 ms); STARWAVEFX 10 / 1948 / 478 (direct, 6413.478 ms). Total 18 groups / 8460 traders / 1984 open positions.** Artifact `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, `utc` `2026-08-18T08:42:16.8519545+00:00`). |
| Any `Take` / plan-map / dummy on this type? | **No.** `Take(` under `D:\Prop\src\Mt5` = **0**. Residual `Take(200)` is `GET /api/trades` (`apps\api\Program.cs` L110) — reconstructed-trade page, not the Manager census. Empty groups are kept. |
| Does copy-to-cTrader send live orders from the product host? | **No.** `CTraderFixSession.BuildLogon` emits only `(35, "A")`. Product `src` `*.cs`/`*.json`/`*.csproj` have **0** literal `35=D`. `NewOrderSingleImplemented = false`. Persist `AllowFixSend = false`. Hosted copy writes **SHADOW** intents only. |
| Risk to capital from this path? | **NONE.** Manager **read** APIs + FIX Logon. Connector has 0 hits for `DealerSend` / `OrderSend` / `TradeRequest` / `UserAdd` / `UserDelete` / `GroupUpdate` / `DealerBalance`. |

Stale report `A001_native_connector.md` (groups = cache only; “zero `GroupRequestArray` under `src`”) is **wrong for the current 458-line file**. Current source has both request calls as **primary** walks.

Stale slots that claimed `RealCopyEnabled` is process-pinned **false** (`A014`/`A015`/`W500_RESEARCH_108`/`W500_RESEARCH_112`) are **wrong for current DI**. See §7.

Stale sibling `W500_RESEARCH_172` L322 citing `Build("D")` at L124/L155 is **wrong for the current 391-line demo helper** (now L139 / L163 / L197). The **copy-hop** claim still holds.

## 2. Search inventory (this slot)

`grep` `GroupRequestArray|UserRequestArray` remeasured 2026-08-18, slot 192:

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
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | L212 / L410 / L1173 | Same vendor header (`5570` / `30 Jan 2026`) |

`GroupRequestArray` exists **only** on `IMTManagerAPI` (L212). `IMTAdminAPI` (class starts L785) has `GroupTotal` / `GroupNext` / `GroupGet` at L910–912 and **no** `GroupRequest` / `GroupRequestArray`. Admin pump enum is MAIL/NEWS only (L789–794). Stay on Manager. Do not “fix” completeness by switching to Admin.

`UserGetByGroup` is **Manager-only** (L672). Admin still exposes `UserRequestArray` at L1173 (request, no user pump bit) — unused by this C# connector.

## 3. SDK contract (what ALL means)

Vendor header `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` (YoPips twin identical at L11–12):

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

```254:254:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserLogins(LPCWSTR group,uint64_t*& logins,uint32_t& logins_total)=0;
```

```671:673:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  UserRequestByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByGroup(LPCWSTR mask,IMTUserArray* users)=0;
   virtual MTAPIRES  UserGetByLogins(const uint64_t *logins,const uint32_t logins_total,IMTUserArray* users)=0;
```

Pump bits used by `ConnectCore` (L89–91 first try; L101 fallback `PUMP_MODE_NONE` = 0):

```125:133:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   enum EnPumpModes
     {
      PUMP_MODE_USERS         =0x00000001,   // pump users
      PUMP_MODE_ACTIVITY      =0x00000002,   // pump users online activity
      PUMP_MODE_MAIL          =0x00000004,   // pump mails
      PUMP_MODE_ORDERS        =0x00000008,   // pump orders
      PUMP_MODE_NEWS          =0x00000020,   // pump news
      PUMP_MODE_POSITIONS     =0x00000080,   // pump positions
      PUMP_MODE_GROUPS        =0x00000100,   // pump group configurations
```

First connect mask is `GROUPS|USERS|POSITIONS` = `0x00000181` (385). C++ `EnPumpModes` has **no** named `PUMP_MODE_NONE` and **no** `PUMP_MODE_DEALS`. C# wrapper names the zero mask `CIMTManagerAPI.EnPumpModes.PUMP_MODE_NONE`.

| API | Header | Kind | Pump required? | Product C# uses? |
|---|---:|---|---|---|
| `GroupRequestArray(mask)` | 212 | **Network** — ALL groups this manager ACL allows | **No** | **Yes — first** (`"*"`) |
| `GroupTotal` / `GroupNext` | 205–206 | **Cache** (`PUMP_MODE_GROUPS`) | Yes for completeness | Fallback only if request list empty |
| `UserRequestArray(group)` | 410 | **Network** — full user records | **No** | **Yes — first** per group |
| `UserGetByGroup` | 672 | **Cache** (`PUMP_MODE_USERS`) | Yes | Hard-fail fallback only |
| `UserLogins` + `UserRequestByLogins` | 254 / 671 | **Network** | **No** | Empty-array fallback |
| `UserAccountRequestArray` | 411 | **Network** balances | **No** | Yes, then cache `UserAccountGetByGroup` (L742) |
| `DealRequest` / `DealRequestByGroup` | 270 / 520 | **Network** | **No** (no deal pump bit) | Yes (`GetDealsCore` / `GetGroupDealsCore` L307) |
| `PositionRequest` / `PositionRequestByGroup` | 282 / 534 | **Network** | **No** | Yes (`GetGroupPositionsCore` L344); cache `PositionGetByGroup` L286 on hard fail |

**ALL in this product = manager-ACL-visible.** Server groups outside the manager record are invisible by design. Do not invent a second plan-map filter.

## 4. Product walk (current 458-line connector)

### 4.1 Connect — pump preferred, request-only fallback

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

Achiever `ApplyProxy` (L115–129) packs HTTP `host:port` + `user:pass` and **checks** `ProxySet` retcode. Starwave never enables it (`ProxyEnabled = false` hard pin at `LiveMt5Registration.cs` L45). Request enumerators are **not** gated on `_pumpEnabled`. A no-pump session still fetches ALL groups/users via `GroupRequestArray` / `UserRequestArray`.

Probe JSON does **not** record `PumpEnabled`. Completeness of the 08:42Z census still comes from the request walk, not from the pump cache.

### 4.2 Groups — request `*` first

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

No plan-map filter. No `MT5_GROUP_*` intersection. Dedup is `HashSet` on `grp.Group()` (case-insensitive). `AddGroup` also records currency, digits, company, margin call/stop-out, and `PermissionsFlags & 0x2` (connections allowed).

### 4.3 Traders — per-group `UserRequestArray`, then cache, then logins

```189:210:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
```

```223:237:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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
            if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
                _manager.UserAccountGetByGroup(gname, accounts);
```

`GetAccountsAsync(null)` (ingest + probe) therefore enumerates **every** manager-visible group, then **every** user in each group. Empty-group `NOTFOUND` is treated as success (do not fall through to cache). Hard fail only then tries pump-cache `UserGetByGroup`. Empty array then tries `UserLogins` + `UserRequestByLogins`. Missing account rows still emit the user with zero balances (L253–261) — completeness of **logins**, not of balances.

### 4.4 Who calls it

| Caller | Call | ALL? |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L45–48 | `GetGroupsAsync` + `GetAccountsAsync(null)` | Yes |
| `DealIngestionService.SyncBrokerAsync` L61–62 | same, then deals/positions | Yes (catalog); deals by each group name; positions `GetGroupPositionsAsync("*")` L84 |
| `LiveIngestHostedService` | `SyncCatalogAsync` then `SyncBrokerAsync` (90-day window L37) then `ListLoginsWithDealsAsync` for scoring | Catalog ALL; **score subset** = logins that already have deals |
| `LiveBrokerProbe` L25–26 | `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")` | Yes |
| `FakeMt5BrokerConnector` | in-memory lists | Exists at `src\Mt5\Connectors\FakeMt5BrokerConnector.cs`; **not** registered on the live DI throw-closed path |

DI (`DependencyInjection.cs` L36–48): throws `Real MT5 passwords are required. Dummy/fake broker data is disabled.` unless both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` pass `IsSecret`; then `CreateConnectors` → **Native ×2 only**. `grep FakeMt5` under `Infrastructure` = **0**.

Dual-broker construction (names only; password values not printed):

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            Server = config["MT5_SERVER"] ?? "",
            ...
            ProxyEnabled = bool.TryParse(config["ACHIEVER_PROXY_ENABLED"], out var pe) && pe,
            ...
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            ...
            ProxyEnabled = false,
            NativeDllDirectory = dllDir
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

## 5. YoPips C++ wrapper — cache-only contrast

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- `GetAllGroups` L962–982 = `GroupTotal` + `GroupNext` only. **0** `GroupRequestArray`.
- `GetUserLogins` L315–328 = `UserLogins` (request). Fail-closes if `!raw_logins`. **0** `UserRequestArray`.
- `GetGroupDetails` L984+ = same cache walk.

Pool sessions historically connect `pump_mode=0`, so `GroupTotal()==0` is **ambiguous** (cold cache vs ACL empty). YoPips `src` **does** have `DealerSend` (internal MT5 dealer — not cTrader FIX). That is a different product. Prop C# is the ALL-groups/ALL-traders collector for this lab.

Local Prop copy `D:\Prop\mt5-sdk\src` also has **0** `GroupRequestArray` / `UserRequestArray`. Do not treat either C++ wrapper as the ALL-groups/ALL-traders implementation.

## 6. Live census (re-summed this slot, not re-probed)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`, `note="Passwords never written. Groups and manager logins only."`  
Password-key grep: **1** hit — that prose note. No password values.

Probe driver (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L19–26): `CreateConnectorsFromEnvironment()` → `ConnectAsync` → `GetGroupsAsync` → `GetAccountsAsync(null)` → `GetGroupPositionsAsync("*")`. That **is** this connector’s request walk.

### ACHIEVER (HTTP proxy; `elapsedMs` 7212.5885)

| Group | Accounts |
|---|---:|
| contest\yo-1step | 2 |
| contest\yo-2step | 179 |
| contest\yo-instant | 4 |
| contest\yo-payp | 5 |
| demo\yo-1step | 4 |
| demo\yo-2step | 6295 |
| demo\yo-instant | 0 |
| demo\yo-payp | 23 |
| **8 groups** | **6512** (2+179+4+5+4+6295+0+23) |

Open positions: **1506**.

### STARWAVEFX (direct; `ProxyEnabled=false`; `elapsedMs` 6413.478)

| Group | Accounts |
|---|---:|
| Starwave\cent\FX1\grp1 | 11 |
| Starwave\cent\FX1\grp2 | 4 |
| Starwave\demo\FX2\grp1 | 170 |
| Starwave\demo\FX2\grp2 | 1735 |
| Starwave\real\FX3\grp1 | 22 |
| Starwave\real\FX3\grp2 | 0 |
| Starwave\real\FX3\grp3 | 0 |
| Starwave\real\FX3\grp4 | 4 |
| Starwave\real\FX3\grp5 | 0 |
| Starwave\real\FX3\LP | 2 |
| **10 groups** | **1948** (11+4+170+1735+22+0+0+4+0+2) |

Open positions: **478**.

**Totals (re-summed):** 8+10 = **18** groups; 6512+1948 = **8460** traders; 1506+478 = **1984** open positions.

**ALL here means every group and login these two manager ACLs can see.** Groups outside the manager permission set are invisible to `GroupRequestArray("*")` by vendor contract. Zero-account groups (`demo\yo-instant`, three Starwave real groups) are still catalogued — the walk does not drop empty groups.

## 7. Copy to cTrader — no live send from the product host

| Gate | Measured | Live send from host? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | `(35, "A")` only | No |
| `CTraderFixSession` write sites | one `ssl.WriteAsync` of the Logon; socket disposed after reply | No persistent TRADE sender |
| Literal `35=D` / `(35, "D")` under `D:\Prop\src` `*.cs`/`*.json`/`*.csproj` | **0** string literals | No (`SAFE_BY_ABSENCE` on the host path) |
| `CopyTradingService.NewOrderSingleImplemented` | `const bool` **false** (L16) | No |
| `CopyTradingService.VenueReconciled` | `const bool` **false** (L15) | No |
| Persist `AllowFixSend` | hardcoded `false` (L192) | No |
| Live-send branch L198 | would set `LIVE_SEND_BLOCKED_UNIMPLEMENTED` even if all flags true | Still no wire |
| Hosted copy | `CopyTradingHostedService` L28–30 creates **SHADOW** intents only | No |
| FIX hosted service | logon + persist session rows; logs “NewOrderSingle still unimplemented”; does **not** re-pin `RealCopyEnabled` | No |
| YoPips `src` `35=D` / `NewOrderSingle` | **0** | No cTrader FIX sender there (YoPips `DealerSend` is MT5 dealer, not this hop) |

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults false** (L35). That POCO is **not** what DI binds.

### Residual — do not greenwash the flag or the demo tool

`DependencyInjection.cs` L41 now binds:

```39:42:D:\Prop\src\Infrastructure\DependencyInjection.cs
        var runtime = new LiveRuntimeStatus
        {
            RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
        };
```

Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (flag **name** + boolean only; no other env values quoted). `/api/settings` L76 exposes `runtime.RealCopyEnabled`. Older slots that said “DI pins false” / “`.env` is false” are **stale**. Flipping the flag **cannot** emit a product-host `35=D` because `CTraderFixSession` has no NewOrderSingle builder.

Separate leftover (not on the host hop): `CTraderFixDemoTestTrade.Build("D", …)` at **L139 / L163 / L197** **is** a NewOrderSingle (flatten / open qty `"1"` / close). Callers: `D:\Prop\tools\DemoFixTestTrade\Program.cs` only (`src` self-def + that tool; **0** hits in `Infrastructure` / `apps`). Not registered in `AddTraderIntelligence`. Gate at L43–60 refuses unless host starts with `demo-`, sender starts with `demo.`, host/sender do not contain `live`, and account is **not** `1369850`. That tool is **out of scope** of the live copy pipeline. Product host risk remains **NONE** by absence of a sender on the ingest/copy hop.

## 8. Honesty / residuals

1. **This slot did not live-attach.** Census is the 08:42Z `LiveBrokerProbe` JSON, arithmetic re-summed here.
2. **ALL = manager ACL**, not “every group on the trade server.”
3. Hosted **scoring** is `ListLoginsWithDealsAsync` only (`LiveIngestHostedService` L106). Catalog still upserts all 8460 accounts; unscored rows stay `INSUFFICIENT_DATA` until they have deals.
4. C++ wrappers (`YoPips` + `D:\Prop\mt5-sdk\src`) still cache-walk groups. Completeness for this product is the C# request path.
5. `REAL_COPY_EXECUTION_ENABLED` may be **true** in lab env and is now bound onto `LiveRuntimeStatus`. Safety is `SAFE_BY_ABSENCE` of a host `35=D` + `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`.
6. Empty `GroupRequestArray` result falls back to cache; a permission-empty manager plus cold cache still returns `[]` (correct, not a cap).
7. `A001` / `A14` / `A84` “request APIs unused” are **stale** against the 458-line connector.
8. `CTraderFixDemoTestTrade` (391 lines this slot) can emit MsgType `D` on the **demo** tool only. Do not claim the entire tree has zero NewOrderSingle constructors; claim the **product host / copy hop** cannot send.
9. Product source was **not** modified by this slot.

## 9. Verdict

**PASS.** Primary walks are `GroupRequestArray("*")` at L155 and per-group `UserRequestArray` at L223. Ingest and `LiveBrokerProbe` call `GetAccountsAsync(null)`, so both owned brokers fetch ALL manager-visible groups and ALL manager-visible traders. Same-day measured census (re-summed, not re-probed): Achiever 8/6512 + Starwave 10/1948 = **18/8460**. Copy-to-cTrader on the product host cannot send live orders (`CTraderFixSession` is `35=A` only; SHADOW intents only). Risk to capital: **NONE**.
