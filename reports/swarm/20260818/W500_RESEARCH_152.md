# W500_RESEARCH_152 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 152
- **date:** 2026-08-18
- **angle:** Search `NativeMt5BrokerConnector` for `GroupRequestArray` and `UserRequestArray`. Goal: fetch **ALL** Achiever + Starwave groups and **ALL** manager-visible traders. Copy to cTrader must **not** send live orders (no capital loss).
- **method:** Independent `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Full read of `LiveMt5Registration.cs` (94/94), `DealIngestionService` catalog/deal path, `CTraderFixSession.cs` (135/135), `CTraderFixLogonHostedService.cs`, `DependencyInjection.cs`, `CopyTradingService.cs`, `LiveBrokerProbe\Program.cs`, `CTraderFixOptions.cs`, `LiveRuntimeStatus.cs`, `LiveIngestHostedService.cs`. SDK header `MT5APIManager.h` (`MTManagerAPIVersion 5570`, `30 Jan 2026`) on both the Prop vendor copy and the YoPips C++ tree. Live census re-summed from existing same-day probe JSON (group-name rows only; no password keys). **This slot did not re-attach live.** No secrets printed.
- **verdict:** **PASS**
- **risk_to_capital:** **NONE** (`SAFE_BY_ABSENCE` of `35=D`)

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes. Exactly one product call site.** `GetGroupsCore` L155: `_manager.GroupRequestArray("*", arr)`. |
| Does it call `UserRequestArray`? | **Yes. Exactly one product call site.** `ReadAccountsForGroup` L223: `_manager.UserRequestArray(gname, users)`. |
| Are those the complete no-pump enumerators? | **Yes, by vendor contract.** `IMTManagerAPI` L212 / L410. Request APIs hit the trade server. They do **not** require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`. |
| Is mask `*` the ALL-groups request? | **Yes.** `CMTStr::CheckGroupMask` (`MT5APIStr.h` L775–809): comma templates, leading `!` exclude, `*` wildcard via `CheckGroupTemplate`. Mask `*` = every group this **manager ACL** may see. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62: `GetAccountsAsync(null)` → `GetAccountsCore(null)` walks **every** name from `GetGroupsCore()`. |
| Are both owned brokers on this type? | **Yes.** `LiveMt5Registration.CreateConnectors` builds **exactly two** `NativeMt5BrokerConnector` instances: `BrokerCodes.Achiever` (`"ACHIEVER"`, optional HTTP proxy) + `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`, `ProxyEnabled = false`). |
| Live measured census (same-day, not re-run here)? | **ACHIEVER 8 / 6512 / 1506; STARWAVEFX 10 / 1948 / 478. Total 18 groups / 8460 traders / 1984 open positions.** Path = this connector via `LiveBrokerProbe`. Artifact `LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, `utc` `2026-08-18T08:42:16.8519545+00:00`). Zero password keys (the only `password` string is the note `"Passwords never written"`). |
| Can copy-to-cTrader send a live order from this process? | **No.** `CTraderFixSession.BuildLogon` emits only `(35, "A")`. Product C# under `D:\Prop\src` (`*.cs`/`*.json`/`*.csproj`) has **0** `35=D` string literals. `CopyTradingService.NewOrderSingleImplemented = false`. Persist `AllowFixSend = false`. |
| Risk to capital? | **NONE.** Manager **read** APIs + FIX Logon only. Connector has 0 hits for `DealerSend` / `OrderSend` / `TradeRequest` / `UserAdd` / `UserDelete` / `GroupUpdate` / `DealerBalance`. |

Stale report `A001_native_connector.md` (groups = cache `GroupTotal`/`GroupNext` only; “zero `GroupRequestArray` under `src`”) is **wrong for the current 458-line file**. Current source has both request calls as **primary** walks. Use this file (or any post-A014 W500 slot that re-read the same lines), not A001.

## 2. Search inventory (this slot)

`grep` `GroupRequestArray|UserRequestArray` on product sources (remeasured 2026-08-18, slot 152):

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

`GroupRequestArray` exists **only** on `IMTManagerAPI` (L212). `IMTAdminAPI` (class starts L785) has `GroupTotal` / `GroupNext` / `GroupGet` at L910–912 and **no** `GroupRequest` / `GroupRequestArray`. Admin pump enum is MAIL/NEWS only (L789–794). Stay on Manager. Do not “fix” completeness by switching to Admin.

`Take(` under `D:\Prop\src\Mt5`: **0**. Residual `Take(200)` is `GET /api/trades` only (`D:\Prop\apps\api\Program.cs`) — reconstructed-trade page, not the Manager census.

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

Pump modes used by `ConnectCore` (L89–91, fallback L101):

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

Sibling request / cache APIs used as **fallbacks** in the same C# file:

| API | Header | Used? |
|---|---|---|
| `GroupTotal` / `GroupNext` | 205–206 | Cache fallback **only if** `GroupRequestArray` left `list.Count == 0` |
| `UserGetByGroup` | 672 | If `UserRequestArray` is not `OK` / `OK_NONE` / `NOTFOUND` |
| `UserLogins` | 254 | If `users.Total() == 0` after the above |
| `UserRequestByLogins` | 671 | After `UserLogins` returns a non-empty array |
| `UserAccountRequestArray` | 411 | Always attempted after users (balance/equity) |
| `UserAccountGetByGroup` | 742 | If account-array request is not `OK` / `OK_NONE` |
| `DealRequest` / `DealRequestByGroup` | 270 / 520 | Deal ingest (read) |
| `PositionRequest` / `PositionRequestByGroup` | 282 / 534 | Position ingest (read) |
| `PositionGetByGroup` | (cache sibling; used only if request fails) | `GetGroupPositionsCore` L346 |

`Get` vs `Request` (measured from naming + pump comments, not re-probed):

- `GroupTotal` / `GroupNext` / `UserGetByGroup` / `UserAccountGetByGroup` / `PositionGetByGroup` read the **local pump cache**. Empty when `PUMP_MODE_NONE` and nothing was requested.
- `GroupRequestArray` / `UserRequestArray` / `UserLogins` / `UserRequestByLogins` / `UserAccountRequestArray` / `DealRequest*` / `PositionRequest*` are **network** and work after the L101 `PUMP_MODE_NONE` fallback.

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

Request enumerators are **not** gated on `_pumpEnabled`. A no-pump session still fetches ALL groups/users via `GroupRequestArray` / `UserRequestArray`.

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
```

```223:233:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
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

`GetAccountsAsync(null)` (ingest + probe) therefore enumerates **every** manager-visible group, then **every** user in each group. Empty-group `NOTFOUND` is treated as success (do not fall through to cache). Hard fail only then tries pump-cache `UserGetByGroup`. Empty array then tries `UserLogins` + `UserRequestByLogins`.

Balances come from `UserAccountRequestArray` (L235) with `UserAccountGetByGroup` fallback (L237). Missing account rows still emit the user with zeros (L253–261). That is completeness of **logins**, not of balances.

### 4.4 Who calls it

| Caller | Call | ALL? |
|---|---|---|
| `DealIngestionService.SyncCatalogAsync` L45–48 | `GetGroupsAsync` + `GetAccountsAsync(null)` | Yes |
| `DealIngestionService.SyncBrokerAsync` L61–62 | same, then deals/positions | Yes (catalog); deals by each group name; positions `GetGroupPositionsAsync("*")` |
| `LiveIngestHostedService` | `SyncCatalogAsync` then `SyncBrokerAsync` (90-day window) then `ListLoginsWithDealsAsync` for scoring | Catalog ALL; **score subset** = logins that already have deals |
| `LiveBrokerProbe` L25–26 | `GetGroupsAsync` + `GetAccountsAsync(null)` | Yes |
| `FakeMt5BrokerConnector` | in-memory lists | Unused on DI throw-closed live path |

DI (`DependencyInjection.cs` L36–48): throws unless both `MT5_PASSWORD` and `MT5_STARWAVEFX_PASSWORD` pass `IsSecret`; then `CreateConnectors` → **Native ×2 only**. `FakeMt5BrokerConnector` exists but is **not** registered on that path.

## 5. YoPips C++ wrapper — cache-only contrast

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp`:

- `GetAllGroups` L962–982 = `GroupTotal` + `GroupNext` only. **0** `GroupRequestArray`.
- `GetUserLogins` L315–328 = `UserLogins` (request). **0** `UserRequestArray`.
- `GetGroupDetails` L984–1013 = same cache walk.

`mt5_pool.cpp` `MT5Session::GetAllGroups` L663–681 is the same cache walk. Pool sessions historically connect `pump_mode=0`, so `GroupTotal()==0` is **ambiguous** (cold cache vs ACL empty). Product C# is the path that actually uses the no-pump-complete enumerator.

Local Prop copy `D:\Prop\mt5-sdk\src` also has **0** `GroupRequestArray` / `UserRequestArray`. Do not treat the C++ wrapper as the ALL-groups/ALL-traders implementation.

## 6. Live census (re-summed, not re-probed)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
`probe=LiveBrokerProbe`, `utc=2026-08-18T08:42:16.8519545+00:00`, `envLoaded=true`.  
Password-key grep: **1** hit — the prose note on L5. No password values.

Probe driver (`D:\Prop\tools\LiveBrokerProbe\Program.cs` L19–26): `CreateConnectorsFromEnvironment()` → `ConnectAsync` → `GetGroupsAsync` → `GetAccountsAsync(null)` → `GetGroupPositionsAsync("*")`. That is this connector’s request walk.

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

### STARWAVEFX (direct; `ProxyEnabled=false`)

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

**ALL here means every group and login these two manager ACLs can see.** Groups outside the manager permission set are invisible to `GroupRequestArray("*")` by vendor contract. Zero-account groups (`demo\yo-instant`, three Starwave real groups) are still catalogued — the walk does not drop empty groups.

## 7. Copy to cTrader — no live send

| Gate | Measured | Live send? |
|---|---|---|
| `CTraderFixSession.BuildLogon` L96 | `(35, "A")` only | No |
| `35=D` / `(35, "D")` / `MsgType="D"` under `D:\Prop\src` `*.cs`/`*.json`/`*.csproj` | **0** | No (`SAFE_BY_ABSENCE`) |
| `CTraderFixSession` write sites | one `ssl.WriteAsync` of the Logon; socket disposed after reply | No persistent TRADE sender |
| `CopyTradingService.NewOrderSingleImplemented` | `const bool` **false** (L16) | No |
| `CopyTradingService.VenueReconciled` | `const bool` **false** (L15) | No |
| Persist `AllowFixSend` | hardcoded `false` (L192) | No |
| Live-send branch L198 | would set `LIVE_SEND_BLOCKED_UNIMPLEMENTED` even if all flags true | Still no wire |
| Hosted copy | `CopyTradingHostedService` L28–30 creates **SHADOW** intents only | No |
| FIX hosted service | logon + persist session rows; logs “NewOrderSingle still unimplemented” | No |

`CTraderFixOptions.RealCopyExecutionEnabled` **defaults false** (L35). That POCO is **not** what DI binds.

### Residual (do not greenwash)

`DependencyInjection.cs` L41 now binds:

```
RealCopyEnabled = string.Equals(configuration["REAL_COPY_EXECUTION_ENABLED"], "true", StringComparison.OrdinalIgnoreCase)
```

Lab `.env` L73 is `REAL_COPY_EXECUTION_ENABLED=true` (flag **name** only; no other env values quoted). `/api/settings` L76 exposes `runtime.RealCopyEnabled`. Older slots that said “DI pins false” / “`.env` is false” are **stale**.

This does **not** create a sender. Flipping the flag cannot emit `35=D` because there is no builder. Risk to capital remains **NONE** by absence, not by a hard-false process pin.

YoPips C++ `src` has **0** cTrader FIX `35=D` senders (same-day sibling searches; this slot confirmed 0 `GroupRequestArray`/`UserRequestArray` in that `src`, and product FIX lives only under `D:\Prop\src\Fix.CTrader`).

## 8. Honesty / residuals

1. **This slot did not live-attach.** Census is the 08:42Z `LiveBrokerProbe` JSON, re-summed here.
2. **ALL = manager ACL**, not “every group on the trade server.”
3. Hosted **scoring** is `ListLoginsWithDealsAsync` only. Catalog still upserts all 8460 accounts; unscored rows stay `INSUFFICIENT_DATA` until they have deals.
4. C++ wrappers (`YoPips` + `D:\Prop\mt5-sdk\src`) still cache-walk groups. Completeness for this product is the C# request path.
5. `REAL_COPY_EXECUTION_ENABLED` may be **true** in lab env and is now bound onto `LiveRuntimeStatus`. Safety is `SAFE_BY_ABSENCE` of `35=D` + `NewOrderSingleImplemented=false` + persist `AllowFixSend=false`.
6. Empty `GroupRequestArray` result falls back to cache; a permission-empty manager plus cold cache still returns `[]` (correct, not a cap).
7. A001 / A14 / A84 “request APIs unused” are **stale** against the 458-line connector.

## 9. Verdict

**PASS.** Primary walks are `GroupRequestArray("*")` at L155 and per-group `UserRequestArray` at L223. Ingest and `LiveBrokerProbe` call `GetAccountsAsync(null)`, so both owned brokers fetch ALL manager-visible groups and ALL manager-visible traders. Same-day measured census: Achiever 8/6512 + Starwave 10/1948 = **18/8460**. Copy-to-cTrader cannot send live orders (`35=D` absent; SHADOW intents only). Risk to capital: **NONE**.
