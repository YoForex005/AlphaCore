# W500_RESEARCH_32 — NativeMt5BrokerConnector `GroupRequestArray` / `UserRequestArray`

- **slot:** 32
- **date:** 2026-08-18
- **goal:** Fetch **ALL** Achiever + Starwave groups and **ALL** manager traders. Copy-to-cTrader must **not** send live orders (no capital loss).
- **method:** `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Full read of `NativeMt5BrokerConnector.cs` (458/458). Live counts from existing same-day probe artifact. This slot did **not** re-run Manager `Connect` and did **not** print passwords, proxy auth, or FIX secrets.
- **verdict:** **PASS**

## 1. Question and measured answer

| Question | Measured answer |
|---|---|
| Does `NativeMt5BrokerConnector` call `GroupRequestArray`? | **Yes.** Primary group walk is `_manager.GroupRequestArray("*", arr)` at `GetGroupsCore` L155. |
| Does it call `UserRequestArray`? | **Yes.** Primary user walk per group is `_manager.UserRequestArray(gname, users)` at `ReadAccountsForGroup` L223. |
| Are those the vendor no-pump complete enumerators? | **Yes, by header contract.** `IMTManagerAPI` `GroupRequestArray` L212 and `UserRequestArray` L410 in `MT5APIManager.h` (API **5570**, date **30 Jan 2026**). Mask `*` = every group this manager ACL may see. Request APIs hit the **server** and do not require `PUMP_MODE_GROUPS` / `PUMP_MODE_USERS`. |
| Does ingest ask for ALL traders? | **Yes.** `DealIngestionService.SyncCatalogAsync` L48 and `SyncBrokerAsync` L62 call `GetAccountsAsync(null, ct)`. `GetAccountsCore` with null walks **every** name returned by `GetGroupsCore`. |
| Are both owned brokers registered? | **Yes.** `LiveMt5Registration.CreateConnectors` constructs exactly two `NativeMt5BrokerConnector` instances: `BrokerCodes.Achiever` (`"ACHIEVER"`) and `BrokerCodes.StarwaveFx` (`"STARWAVEFX"`). |
| Dummy/Fake on the live DI path? | **No.** `AddTraderIntelligence` throws unless both password keys look real, then registers only the two native connectors. `DemoSeeder` is **not** called from `apps/api/Program.cs`. |
| Live measured census (same-day probe)? | **ACHIEVER:** 8 groups / 6512 traders / 1506 open positions. **STARWAVEFX:** 10 groups / 1948 traders / 478 open positions. **Total 18 groups / 8460 manager traders.** |
| Does copy-to-cTrader send live orders? | **No.** `CTraderFixSession` emits only `35=A` Logon. Zero `35=D` builders. `RealCopyEnabled` forced `false` in DI and again after FIX logon. Native connector has no dealer/trade send. |
| Risk to capital from this path? | **None.** Manager **read** APIs + FIX logon only. No `DealerSend` / `OrderSend` / `TradeRequest` / `NewOrderSingle`. |

Stale inventory (`A001_native_connector.md`) that claimed **zero** `GroupRequestArray` / `UserRequestArray` hits under `D:\Prop\src` is **wrong for the current file**. Current `src` has both request calls (5 hits, all in `NativeMt5BrokerConnector.cs`).

## 2. SDK contract — what ALL means

Vendor header (not YoPips product code). Same symbols exist in both trees:

```212:212:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

```408:411:D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTUserArray* UserCreateArray(void)=0;
   virtual IMTAccountArray* UserCreateAccountArray(void)=0;
   virtual MTAPIRES  UserRequestArray(LPCWSTR group,IMTUserArray* users)=0;
   virtual MTAPIRES  UserAccountRequestArray(LPCWSTR group,IMTAccountArray *accounts)=0;
```

Prop copy (identical declarations):

```211:212:D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h
   virtual IMTConGroupArray* GroupCreateArray(void)=0;
   virtual MTAPIRES  GroupRequestArray(LPCWSTR mask,IMTConGroupArray* groups)=0;
```

Supporting request APIs used by the C# connector:

| API | Header line (`IMTManagerAPI`) | Role |
|---|---:|---|
| `GroupCreateArray` | 211 | Allocate group array |
| `GroupRequestArray(mask, arr)` | 212 | **Network** snapshot of groups matching mask |
| `GroupTotal` / `GroupNext` | 205–206 | Local pump/config **cache** walk |
| `UserCreateArray` | 408 | Allocate user array |
| `UserRequestArray(group, users)` | 410 | **Network** full user records by group mask |
| `UserAccountRequestArray` | 411 | **Network** trading-account snapshot |
| `UserLogins` | 254 | **Network** login IDs only |
| `UserRequestByLogins` | 671 | **Network** users by login list |
| `UserGetByGroup` | 672 | **Cache** users by group |

Admin surface is **not** this connector’s path:

- `IMTAdminAPI` starts at header L785. Group block L900–916 has `GroupTotal` / `GroupNext` / `GroupGet` and **no** `GroupRequest` / `GroupRequestArray` (confirmed: `GroupRequestArray` appears **once** in that header, L212, on Manager).
- Second `UserRequestArray` at header L1173 is on **Admin**. Product C# uses `CIMTManagerAPI` only.

Mask language (`CMTStr::CheckGroupMask`, `MT5APIStr.h` L775–809): comma-separated templates, leading `!` = exclude, `*` via `CheckGroupTemplate`. Mask `"*"` on `GroupRequestArray` / `UserRequestArray` / `UserLogins` means **every group this manager record may see**. Server groups outside that ACL are invisible by design. **ALL in this product = manager-ACL-visible**, not “every group on the box.” Do not add a plan-map filter.

Connect pump (product, L89–91): `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`, fallback `PUMP_MODE_NONE` (L101). Request arrays still work after a no-pump fallback (`_pumpEnabled = false`).

## 3. YoPips C++ product does **not** implement this enumerator

`grep` of `GroupRequestArray` / `UserRequestArray` under `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src`: **0 hits**.

Hits exist only in vendor `MetaTrader5SDK\Include\MT5APIManager.h` (L212, L410, L1173) and SDK examples.

YoPips wrapper is **cache / login-id only**:

```962:981:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetAllGroups(std::vector<std::string>& groups) {
    // ...
    uint32_t total = m_manager->GroupTotal();
    // ...
    for (uint32_t i = 0; i < total; i++) {
        if (m_manager->GroupNext(i, grp) == MT_RET_OK) {
            groups.push_back(StringUtils::toUtf8(grp->Group()));
        }
    }
```

```315:327:D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp
bool MT5Manager::GetUserLogins(const std::wstring& group, std::vector<uint64_t>& logins) {
    // ...
    MTAPIRES res = m_manager->UserLogins(group.c_str(), raw_logins, total);
    if (res != MT_RET_OK || !raw_logins) return false;
    logins.assign(raw_logins, raw_logins + total);
```

Same cache walk in `MT5Session::GetAllGroups` / `GetUserLogins` (`mt5_pool.cpp` L211–217, L656–675). That is the **old** wrapper. The Prop C# connector is the path that implements A39’s request-API enumerator.

## 4. Connector — groups (`GroupRequestArray`)

File: `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` (458 lines). Type implements `IMt5BrokerConnector`, `IMt5BulkDealReader`, `IMt5BulkPositionReader`.

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

1. Always tries **network** `GroupRequestArray("*")` first (not a plan list, not a subset mask).
2. Accepts `MT_RET_OK` and `MT_RET_OK_NONE`.
3. Dedupes names case-insensitively (`HashSet`).
4. Cache `GroupTotal`/`GroupNext` runs **only** when the request list is empty (failed request, empty ACL, or cold result).
5. No `Take(...)`. No `MT5_GROUP_*` filter. No hardcoded Achiever/Starwave group names.

`AddGroup` (L368–381) maps `CIMTConGroup` → `Mt5GroupDto` (name, currency, digits, company, margin call/stop-out, `PermissionsFlags & 0x2` = connections allowed).

## 5. Connector — traders (`UserRequestArray`)

```189:213:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5AccountDto> GetAccountsCore(string? group)
    {
        // null/blank → every name from GetGroupsCore()
        // then ReadAccountsForGroup(gname) into Dictionary<ulong, Mt5AccountDto>
    }
```

```216:271:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
    private List<Mt5AccountDto> ReadAccountsForGroup(string gname)
    {
        var req = _manager.UserRequestArray(gname, users);
        if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
            _manager.UserGetByGroup(gname, users);

        if (users.Total() == 0)
        {
            var logins = _manager.UserLogins(gname, out loginRes);
            if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                _manager.UserRequestByLogins(logins, users);
        }

        var acctReq = _manager.UserAccountRequestArray(gname, accounts);
        if (acctReq != MTRetCode.MT_RET_OK && acctReq != MTRetCode.MT_RET_OK_NONE)
            _manager.UserAccountGetByGroup(gname, accounts);
        // join users × accounts by login → Mt5AccountDto
    }
```

Call graph:

```
GetAccountsAsync(null)
  → GetAccountsCore(null)
      → GetGroupsCore()                 // GroupRequestArray("*") then cache if empty
      → foreach group name
           ReadAccountsForGroup(name)
             1. UserRequestArray(name)          // network full user records  (PRIMARY)
             2. UserGetByGroup(name)            // cache, only on hard error
             3. UserLogins + UserRequestByLogins // only if users.Total()==0
             4. UserAccountRequestArray(name)   // balances/equity
```

Ingest always uses the null path:

```45:49:D:\Prop\src\Application\Ingestion\DealIngestionService.cs
        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

`LiveIngestHostedService` (L41–62) connects **every** registered connector, then `SyncCatalogAsync`. Manual `/api/ops/resync` repeats both `ACHIEVER` and `STARWAVEFX` (`Program.cs` L121–144). Dashboard `/api/groups` and `/api/traders` read the store, not FakeMt5 10001/10002.

`Take(200)` exists only on `/api/trades` reconstructed-trade listing (`Program.cs` L107). It does **not** cap the Manager catalog.

## 6. Dual-broker registration (Achiever + Starwave)

```23:49:D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs
        var achiever = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.Achiever,
            // MT5_SERVER / MT5_PORT / MT5_LOGIN / MT5_PASSWORD
            // ACHIEVER_PROXY_* when ACHIEVER_PROXY_ENABLED
        });

        var starwave = new NativeMt5BrokerConnector(new NativeMt5Options
        {
            BrokerCode = BrokerCodes.StarwaveFx,
            // MT5_STARWAVEFX_*
            ProxyEnabled = false,
        });

        return new IMt5BrokerConnector[] { achiever, starwave };
```

`AddTraderIntelligence` (`DependencyInjection.cs` L35–46): fail-closed if either password is empty / `<SECRET>` / `(a/c` placeholder; then `foreach` those two connectors as `IMt5BrokerConnector` singletons. **Password values not quoted.**

`FakeMt5BrokerConnector` / `DemoBrokerFactory.CreateDefault` exist for tests/seeder only. `DemoSeeder.SeedAsync` has **no** call site under `apps/api`.

## 7. Live measured census (not re-run this slot)

Artifact: `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json`  
Probe: `D:\Prop\tools\LiveBrokerProbe\Program.cs` — same `CreateConnectorsFromEnvironment()` → `GetGroupsAsync` + `GetAccountsAsync(null)` + `GetGroupPositionsAsync("*")`. UTC `2026-08-18T08:42:16.8519545+00:00`. Note on disk: `"Passwords never written. Groups and manager logins only."` This slot does **not** reprint logins.

| Broker | Connect | elapsedMs | Groups | Traders | Open positions | Path in source |
|---|---|---:|---:|---:|---:|---|
| ACHIEVER | `connected: true` (HTTP proxy when env says so) | 7212.5885 | 8 | 6512 | 1506 | `GroupRequestArray` + `UserRequestArray` |
| STARWAVEFX | `connected: true` (direct) | (same probe) | 10 | 1948 | 478 | same |

**Total: 18 groups, 8460 manager traders.**

Achiever groups (name → account count; sum 2+179+4+5+4+6295+0+23 = **6512**):

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

Starwave groups (11+4+170+1735+22+0+0+4+0+2 = **1948**):

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

These are **all groups this manager login can see**. If the trade server has more groups, they are outside this manager’s permission set. Zero-account groups (`demo\yo-instant`, three Starwave real groups) are still enumerated — evidence the walk is not “skip empty.”

## 8. Completeness caveats (do not greenwash)

1. **Partial `GroupRequestArray` accepted.** Cache `GroupTotal`/`GroupNext` runs only when `list.Count == 0`. A non-empty but truncated request result is treated as complete. No compare to `GroupTotal()`.
2. **Partial `UserRequestArray` accepted.** `UserLogins` + `UserRequestByLogins` runs only when `users.Total() == 0`. If the request returns some but not all users, missing logins are not recovered. `UserGetByGroup` is skipped on `OK` / `OK_NONE` / `NOTFOUND`.
3. **Users follow the group list.** A group omitted by (1) never gets a `UserRequestArray`.
4. **ACL ceiling.** `*` cannot see groups the manager record forbids. That is the correct product definition of ALL.
5. **This slot did not re-Connect.** Counts are the same-day `LiveBrokerProbe` artifact, not a fresh session.
6. **C++ YoPips still cache-only.** Do not claim the C++ wrapper now requests arrays; it does not.

None of these change the source fact: the product C# path’s **primary** enumerators are the vendor ALL-request APIs, and ingest asks for the full set.

## 9. Copy-to-cTrader — no live orders (no loss)

Goal constraint: copying to cTrader must not send live orders yet.

| Gate | Evidence | Can place an order? |
|---|---|---|
| FIX session builder | `CTraderFixSession.BuildLogon` L94–108: only `(35, "A")` plus seq/comp/sub/time/encrypt/heartbeat/reset/553/554 | No |
| `35=D` / `NewOrderSingle` builder | `grep` under `D:\Prop\src` for `35=D`, `NewOrderSingle` as emit, `(35, "D")`: **no send site**. Hits are comments, flags, and `MayRetryNewOrderSingle` FSM helper | No |
| Native Manager writes | `grep` `DealerSend\|DealerBalance\|TradeRequest\|UserUpdate\|OrderSend` in `D:\Prop\src`: **0**. Connector reads: `DealRequest` / `DealRequestByGroup` / `PositionRequest` / `PositionRequestByGroup` | No |
| DI flag | `DependencyInjection.cs` L40–41: `RealCopyEnabled = false` with comment “Live NewOrderSingle is not implemented” | No |
| After FIX logon | `CTraderFixLogonHostedService.cs` L68: `_runtime.RealCopyEnabled = false` | No |
| Options default | `CTraderFixOptions.RealCopyExecutionEnabled = false` (L35) | Flag only |
| Fix worker | `apps/fix-worker/Worker.cs` L21–46: reads `CTrader:RealCopyExecutionEnabled` (default false); **stamps Disconnected**; even if config true, only logs a warning — still no socket send | No |
| Settings API | `/api/settings` exposes `REAL_COPY_EXECUTION_ENABLED` from `runtime.RealCopyEnabled` (pinned false) | No |
| Shadow | `ShadowCopyEngine` is in-memory `SimulateEntry`/`SimulateExit`. No FIX/MT5 send | No |

Residual (not a send path): `CTraderFixLogonHostedService` **does** attempt TRADE logon on port 5212 (`35=A` only). A future engineer adding a `35=D` builder on that socket would be the first capital-risk change. Today that builder **does not exist**. Flipping any env flag cannot emit an order.

`LiveRuntimeStatus.Snapshot` copy note when flag is false: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`

## 10. Honesty

- This is **not** “EX5 decompiled” and not 95% live copy-trading.
- It **is** a measured source walk: product C# uses `GroupRequestArray("*")` + per-group `UserRequestArray` as the primary ALL enumerators for both owned brokers, ingest asks `GetAccountsAsync(null)`, and a same-day live probe recorded 18 manager-visible groups and 8460 traders.
- Copy-to-cTrader cannot place live orders from this process (`35=A` only; `RealCopyEnabled=false`; no dealer send).

## 11. Files read (absolute)

| Path | Why |
|---|---|
| `D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` | Assigned type (full 458) |
| `D:\Prop\src\Infrastructure\Mt5Live\LiveMt5Registration.cs` | Two native connectors |
| `D:\Prop\src\Infrastructure\DependencyInjection.cs` | Fail-closed + `RealCopyEnabled=false` |
| `D:\Prop\src\Infrastructure\Hosting\LiveIngestHostedService.cs` | Catalog both brokers |
| `D:\Prop\src\Application\Ingestion\DealIngestionService.cs` | `GetAccountsAsync(null)` |
| `D:\Prop\src\Application\Contracts\Mt5Contracts.cs` | Connector interfaces |
| `D:\Prop\src\Application\Runtime\LiveRuntimeStatus.cs` | Copy-note / flag |
| `D:\Prop\src\Domain\Brokers\BrokerCodes.cs` | `ACHIEVER` / `STARWAVEFX` |
| `D:\Prop\src\Domain\Shadow\ShadowCopyEngine.cs` | Shadow = simulate only |
| `D:\Prop\src\Mt5\Connectors\FakeMt5BrokerConnector.cs` | Not on live DI path |
| `D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs` | `35=A` only |
| `D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs` | Forces flag false |
| `D:\Prop\src\Fix.CTrader\Configuration\CTraderFixOptions.cs` | Default flag false |
| `D:\Prop\apps\api\Program.cs` | Resync both brokers; no DemoSeeder |
| `D:\Prop\apps\fix-worker\Worker.cs` | No send even if flag true |
| `D:\Prop\tools\LiveBrokerProbe\Program.cs` | Probe that wrote the JSON |
| `D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` | Counts + group names only |
| `D:\Prop\reports\swarm\20260818\LIVE_MANAGER_FETCH_MEASURED.md` | Same-day summary |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\MT5APIManager.h` | Prop SDK copy |
| `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Include\Classes\MT5APIStr.h` | Mask language |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\MetaTrader5SDK\Include\MT5APIManager.h` | Vendor Manager/Admin |
| `D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` | C++ cache-only walk |

## 12. Verdict

`NativeMt5BrokerConnector` **does** use `GroupRequestArray("*")` and per-group `UserRequestArray` as the primary ALL enumerators for both Achiever and Starwave. Ingest asks `GetAccountsAsync(null)`. There is no `Take` on the catalog, no plan-map filter, no dummy substitution on this type. A same-day live probe recorded 18 manager-visible groups and 8460 traders. Copy-to-cTrader cannot place live orders from this process (`35=A` only; `RealCopyEnabled=false`). **PASS.**
