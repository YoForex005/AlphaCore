# W500_RESEARCH_13 — Api.csproj TargetFramework vs `MT5APIManager64` load

| Field | Value |
|---|---|
| Slot | **13** |
| Wave | W500 research |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_13.md` |
| Assigned topic | Check `Api.csproj` `TargetFramework`. `net8.0` without windows/x64 cannot load `MT5APIManager64`. |
| Goal overlay | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** Key *names* and non-secret host/login/group identifiers only. No passwords, no proxy auth, no FIX password. |
| Method | `read_file` + `grep` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current csproj / dgspec / bin listings / native connector / FIX send surface / live probe JSON. No shell, no product edit. |
| Stale siblings (do not treat as current) | `A012_worker_tfm.md` (API still quoted as portable `net8.0` — **false now**). `R021` NU1201 row for `apps/api` is historical (pre-retarget). |
| Current siblings | `R021_dll_load.md` (isolated PE load), `A105_windows_dlls.md` (PE32+ `0x8664`), `W500_SLICE_105.md` (same TFM angle), `E002` / `E034` (no `35=D`), `A004` (YoPips group recipe), `LIVE_GROUPS_AND_TRADERS.json` |

**Verdict: PASS (API TFM + load contract). Residual FAIL on worker/tests/compose. Live send: SAFE_BY_ABSENCE.**

There is **no** file named `Api.csproj`. The API host is `D:\Prop\apps\api\TraderIntelligence.Api.csproj`. It is **already** `<TargetFramework>net8.0-windows</TargetFramework>` + `<PlatformTarget>x64</PlatformTarget>`. The current Debug output `bin\Debug\net8.0-windows\` **contains** `MT5APIManager64.dll` plus both MetaQuotes mixed-mode wrappers. The hypothesized “API is still portable `net8.0` so Manager64 cannot load” defect is **absent** on this host.

The slogan “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is **true for the product graph and for Linux/x86**, and **false as a PE law**. Isolated R021: a Windows AMD64 `.NET 8` process on TFM `net8.0` **can** `SMTManagerAPIFactory.Initialize` when the native file sits beside the exe. What `net8.0` (no Windows TFM) **cannot** do in *this* tree is `ProjectReference` `src/Mt5` (`net8.0-windows7.0`) — restore **NU1201** — so the trio never copies and the type-load fails before `LoadLibraryW`.

---

## 0. Answers (measured)

| Question | Answer |
|---|---|
| What is `Api.csproj` TFM **now**? | **`net8.0-windows`** + **`PlatformTarget` x64**. Restore alias `net8.0-windows7.0`. |
| Does `Directory.Build.props` set TFM / RID? | **No.** LangVersion / Nullable / ImplicitUsings / Deterministic only. |
| Is `RuntimeIdentifier=win-x64` set on API? | **No.** RID is absent. Bitness is pinned by `PlatformTarget` x64, not by RID. |
| Does current API output contain `MT5APIManager64.dll`? | **Yes** — `D:\Prop\apps\api\bin\Debug\net8.0-windows\MT5APIManager64.dll` plus `MetaQuotes.MT5ManagerAPI64.dll` and `MetaQuotes.MT5CommonAPI64.dll`. |
| Does leftover `bin\Debug\net8.0\` contain it? | **No.** Stale portable output. Operator error if launched. |
| Can portable `net8.0` *load the PE* on Windows x64 if the file is present? | **Yes (R021).** `Initialize` → `MT_RET_OK`. Missing native file → `MT_RET_ERR_NOTFOUND` (13). |
| Can portable `net8.0` *product host* load it **as this graph sits**? | **No.** NU1201 vs `Mt5`/`Infrastructure`. Trio never copied. |
| Can Linux / compose `mcr.microsoft.com/dotnet/sdk:8.0` load it? | **No.** PE32+ AMD64 + `LoadLibraryW`. Compose comment already forbids this. |
| Can x86 / ARM64 Windows load *this* file? | **No.** Machine `0x8664`. ARM wants `MT5APIManager64arm.dll` (`0xAA64`). |
| Does API enumerate **all** Achiever + Starwave groups + traders? | **Code: yes** (`GroupRequestArray("*")` + per-group `UserRequestArray` / `UserLogins`). **Live probe: yes** — 8+10 groups, 6512+1948 accounts. |
| Can copy-to-cTrader send a live order from this process? | **No.** No `35=D` builder. `RealCopyEnabled` forced `false`. FIX host only writes Logon `35=A`. Shadow rows are `SHADOW_ONLY`. |

Honest one-liner:

```text
API is net8.0-windows x64 and already stages MT5APIManager64.dll.
net8.0 without Windows/x64 cannot restore this graph (NU1201) and cannot LoadLibrary the PE on Linux/x86.
Isolated Windows x64 net8.0 CAN load the PE if the file is present (R021).
Live catalog is ALL groups / ALL manager logins. Live 35=D does not exist.
```

---

## 1. Current TFM census (source of truth = csproj + dgspec, not A012)

### 1.1 API host (assigned)

`D:\Prop\apps\api\TraderIntelligence.Api.csproj`:

```17:22:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

Direct `ProjectReference` to the native Manager assembly:

```3:8:D:\Prop\apps\api\TraderIntelligence.Api.csproj
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
```

Restore dgspec (`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`) agrees:

| Key | Value |
|---|---|
| `originalTargetFrameworks` | `net8.0-windows` |
| restore framework | `net8.0-windows7.0` (alias `net8.0-windows`) |
| `RuntimeIdentifier` | **absent** |
| `EnableWindowsTargeting` | **absent** |

`runtimeconfig.json` `tfm` is `"net8.0"` even for this host. That is **normal** for `net8.0-windows` (base framework moniker). It is **not** evidence the project is portable `net8.0`. Frameworks listed: `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` only — no WindowsDesktop pack is required just to load the MetaQuotes mixed-mode image (R021).

### 1.2 Who else is Windows x64 vs leftover `net8.0`

| Project | Path | TFM | `PlatformTarget` | Can `ProjectReference` `src/Mt5`? | Stages Manager trio? |
|---|---|---|---|---|---|
| API | `apps/api/TraderIntelligence.Api.csproj` | **`net8.0-windows`** | **x64** | **Yes** | **Yes** (current `net8.0-windows` bin) |
| LiveBrokerProbe | `tools/LiveBrokerProbe/LiveBrokerProbe.csproj` | **`net8.0-windows`** | **x64** | Yes | via Mt5 copy |
| `src/Mt5` | `TraderIntelligence.Mt5.csproj` | **`net8.0-windows`** | **x64** | (self) | **Yes** — HintPath + `CopyToOutputDirectory` |
| `src/Infrastructure` | `TraderIntelligence.Infrastructure.csproj` | **`net8.0-windows`** | **x64** | Yes | transitive |
| `src/Domain` | portable | `net8.0` | — | N/A | no |
| `src/Application` | portable | `net8.0` | — | N/A | no (must stay portable) |
| `src/Fix.CTrader` | portable | `net8.0` | — | N/A | no (FIX is not Manager64) |
| **mt5-worker** | `apps/mt5-worker/...csproj` | **`net8.0`** | **absent** | **NU1201** | **No** — `bin\Debug\net8.0\` has **zero** MetaQuotes / `MT5APIManager64.dll` |
| **fix-worker** | `apps/fix-worker/...csproj` | **`net8.0`** | **absent** | NU1201 (refs Infrastructure) | should not load Manager64 |
| **Integration tests** | `tests/Integration/...csproj` | **`net8.0`** | absent | NU1201 (refs Mt5 + Infrastructure) | no |

mt5-worker dgspec still records `"originalTargetFrameworks": ["net8.0"]` while referencing `Infrastructure` + `Mt5` (`net8.0-windows`). That is the NU1201 pair R021 measured. **Different process from API.** Slot 13 is the API TFM check.

### 1.3 A012 is stale

`A012_worker_tfm.md` §0: “All three [api / mt5-worker / fix-worker] still target `net8.0`.” That was true when written. **API has since been retargeted.** Worker + FIX + Integration tests have **not**. Do not quote A012 as the API state of record.

---

## 2. Why “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is only *mostly* true

Three different operations. Do not collapse them.

### 2.1 The file is a Windows AMD64 PE, not a net8 asset

Vendor drop (A105 / R021, same folder `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`):

| File | Bytes | Machine | Kind | SHA-256 |
|---|---:|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | `0x8664` AMD64 | **native** (no CLR) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | mixed-mode C++/CLI, `.NETFramework,Version=v4.7.2`, CorFlags `NATIVE_ENTRYPOINT` | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | same mixed-mode family | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

Factory loader (`MT5APIManager.h` 1715–1741): `FindLibrary` → **`LoadLibraryW`** → `GetProcAddress(MTManagerVersion / MTManagerCreateExt)`. There is no `dlopen` path. Missing file or failed map → `MT_RET_ERR_NOTFOUND`.

`FindLibrary` (same header 1769–1831) prefers `MT5APIManager64avx2.dll` / `avx` / `arm` then vanilla `MT5APIManager64.dll` then `.\libs\` then **PATH** (always returns true with a bare filename). Production must pass an **absolute directory**. Product does: `NativeDllDirectory = AppContext.BaseDirectory`.

### 2.2 Isolated PE fact (R021) — TFM `net8.0` is not the LoadLibrary bit

| Isolated experiment | Result |
|---|---|
| `net8.0` x64 `<Reference>` Manager64 + CommonAPI64 | compile **PASS** |
| `net8.0-windows` x64 same | compile **PASS** |
| Windows x64 process `Initialize` with trio beside exe | **`MT_RET_OK`** |
| Same without `MT5APIManager64.dll` | wrapper still loads; init **`MT_RET_ERR_NOTFOUND` (13)** |
| Collectible ALC | `BadImageFormatException` (mixed-mode) |
| `net8.0` x86 | CS8012; no x86 runtime on this box; PE is AMD64 anyway |
| Linux / Wine / Linux container | **cannot** map PE32+ |

So: retargeting the API to `net8.0-windows` + x64 is the **honest product TFM** (Windows-only Manager host + NU1201 fix + copy-dlls flow). It is **not** a magic extra PE flag. `net8.0` x64 already could load the PE **in isolation**.

### 2.3 Product-graph fact — `net8.0` host **cannot** consume this `src/Mt5`

`src/Mt5` is `net8.0-windows` + x64 and is the only project that binds the official trio:

```6:29:D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <None Include="..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    ...
    <TargetFramework>net8.0-windows</TargetFramework>
    ...
    <PlatformTarget>x64</PlatformTarget>
```

A portable `net8.0` exe **cannot** `ProjectReference` a `net8.0-windows7.0` library (NU1201). That is why leftover `apps/api/bin/Debug/net8.0\` has `TraderIntelligence.Mt5.dll` from an older portable build and **zero** of the trio. That leftover **cannot** `LoadLibrary` Manager64 because the native file is not next to the exe. `NativeMt5BrokerConnector` also JITs `MetaQuotes.MT5ManagerAPI` — missing wrappers → `FileNotFoundException` / `BadImageFormatException` **before** `ConnectAsync`.

### 2.4 Current API output (the live contract)

`D:\Prop\apps\api\bin\Debug\net8.0-windows\` listing includes:

- `TraderIntelligence.Api.exe`
- `TraderIntelligence.Mt5.dll`
- `MT5APIManager64.dll`
- `MetaQuotes.MT5ManagerAPI64.dll`
- `MetaQuotes.MT5CommonAPI64.dll`
- Manager cache trees `bases/AchieverGlobalMarkets-Server/` (login **2027**) and `bases/StarwaveFX-Server/` (login **9904**)

`D:\Prop\apps\api\bin\Debug\net8.0\` listing does **not** include any of the three vendor files.

`D:\Prop\apps\mt5-worker\bin\Debug\net8.0\` listing does **not** include any of the three vendor files.

### 2.5 Runtime load path in product C#

```66:75:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
            if (init != MTRetCode.MT_RET_OK && init != MTRetCode.MT_RET_ERR_DUPLICATE)
            {
                LastError = Describe(init, "Factory.Initialize");
                throw new InvalidOperationException(LastError);
            }
```

`LiveMt5Registration.CreateConnectors` sets `NativeDllDirectory = Path.GetFullPath(AppContext.BaseDirectory)` for **both** Achiever and StarwaveFX. DI fail-closes without real `MT5_PASSWORD` + `MT5_STARWAVEFX_PASSWORD` and registers **only** `NativeMt5BrokerConnector` — `FakeMt5BrokerConnector` is not on the API path.

### 2.6 Compose / Linux trap (not a missing API TFM)

`D:\Prop\docker-compose.yml` `api` service: `image: mcr.microsoft.com/dotnet/sdk:8.0` (Linux) + `dotnet run --project apps/api/TraderIntelligence.Api.csproj`. Comment on disk: “Native MT5 Manager DLL workers stay on Windows hosts.”

This is a **deployment contradiction**, not evidence that `TraderIntelligence.Api.csproj` is still portable `net8.0`:

1. `net8.0-windows` will not restore/run on Linux without `EnableWindowsTargeting`, and even then the host is the wrong OS.
2. Linux cannot `LoadLibraryW` a PE32+ AMD64 image.
3. `NativeMt5BrokerConnector` throws `PlatformNotSupportedException` before init.

Do **not** copy `MT5APIManager64.dll` into a Debian/Alpine image “to have it nearby.” That is `UNSAFE` (A54 / A105 / D63).

---

## 3. Goal: ALL Achiever + Starwave groups and ALL manager traders

### 3.1 C# walk is mapping-blind (no `demo\` filter, no `Take`)

Groups — request mask `"*"` then pump-cache fallback:

```155:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
                var res = _manager.GroupRequestArray("*", arr);
                ...
            if (list.Count == 0)
            {
                ...
                    var total = _manager.GroupTotal();
                    for (uint i = 0; i < total; i++)
                    {
                        if (_manager.GroupNext(i, grp) != MTRetCode.MT_RET_OK)
                            continue;
                        AddGroup(list, seen, grp);
                    }
```

Accounts — `GetAccountsAsync(null)` walks **every** group name, then per group:

1. `UserRequestArray(gname)`
2. else `UserGetByGroup`
3. if still empty: `UserLogins` + `UserRequestByLogins`
4. `UserAccountRequestArray` / `UserAccountGetByGroup` for balances

Dedup is by login only. No `Take(N)` on groups or accounts in `src/` ingest (`grep Take(` in `src` hits FIX checksum + dashboard `.Take(20)` display only).

`DealIngestionService.SyncCatalogAsync` persists whatever the connector returns (`GetGroupsAsync` + `GetAccountsAsync(null)`). `LiveIngestHostedService` then scores **every** `store.ListLoginsAsync` row — not the worker’s leftover dummy quartet `10001/10002/10003/99001`.

API `POST /api/ops/resync` loops **both** `"ACHIEVER"` and `"STARWAVEFX"`.

### 3.2 YoPips C++ (same SDK, same ALL-groups contract)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups` / `GetGroupDetails` = `GroupTotal` + `GroupNext` (no mask filter). `GetUserLogins` = `UserLogins(group)`. Prop `D:\Prop\mt5-sdk\src\core\mt5_manager.cpp` is the same walk (A004: identical for those methods).

C# prefers **network** `GroupRequestArray("*")` then falls back to the C++ `GroupTotal`/`GroupNext` cache walk. That is **stricter** than the C++ probe when pump is off.

Connect recipe (A004, identifiers only):

| Broker | Server:port | Manager login | Proxy |
|---|---|---:|---|
| Achiever | `57.128.141.65:443` | 2027 | HTTP via allow-list `81.29.145.69:49527` **required** on this LAN (else **1012**) |
| StarwaveFX | `84.201.6.142:443` | 9904 | **off** |

C# `Connect` tries pump `GROUPS|USERS|POSITIONS` then `PUMP_MODE_NONE`. YoPips wrapper remaps literal `0` to a pump set, then retries true pump-none. Different first attempt; same fallback.

C++ `mt5_group_probe` prints **groups only**. ALL logins is a second walk. C# `LiveBrokerProbe` does both.

### 3.3 Live measurement (same connector family as API)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, utc `2026-08-18T08:42:16Z`, `envLoaded=true`, passwords not written). Probe is `net8.0-windows` x64 and calls `LiveMt5Registration.CreateConnectorsFromEnvironment()` → same `NativeMt5BrokerConnector`.

| Broker | Connected | Groups | Accounts (manager traders) | Open positions |
|---|---|---:|---:|---:|
| ACHIEVER | **true** (7213 ms) | **8** | **6512** | 1506 |
| STARWAVEFX | **true** | **10** | **1948** | 478 |
| **Total** | | **18** | **8460** | 1984 |

Achiever group names (all `contest\` + `demo\` `yo-*`; **no** `demo\Maxmaster`-only filter):

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

StarwaveFX group names (cent + demo + real, including empty books):

| Group | Accounts |
|---|---:|
| `Starwave\cent\FX1\grp1` | 11 |
| `Starwave\cent\FX1\grp2` | 4 |
| `Starwave\demo\FX2\grp1` | 170 |
| `Starwave\demo\FX2\grp2` | 1735 |
| `Starwave\real\FX3\grp1` | 22 |
| `Starwave\real\FX3\grp2` | 0 |
| `Starwave\real\FX3\grp3` | 0 |
| `Starwave\real\FX3\grp4` | 4 |
| `Starwave\real\FX3\grp5` | 0 |
| `Starwave\real\FX3\LP` | 2 |

Empty groups are **kept**. That is ALL-groups, not “groups with traders.”

`CREDENTIALS_AND_COPY_STATUS.md` records the same census on dashboard `/api/groups` = 18 and `/api/traders` = 8460. This slot did **not** re-hit HTTP (no live call here). The JSON dump is the measured source.

This slot did **not** re-run `LiveBrokerProbe`. Completeness vs a second independent `GroupTotal` from the C++ probe in *this* process is **not** re-proven here. The C# mask `"*"` + live 18/8460 is the evidence of record.

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 No `35=D` exists

E034 (83 product `*.cs`): literal `35=D` / `(35, "D")` / `MsgType = "D"` = **0**. This slot re-grepped `src/Fix.CTrader`: the only outbound FIX the API host can write is `CTraderFixSession.BuildLogon` tag **35=`A`**. `CTraderQuoteService` builds tag lists `y` / `V` and does **not** write a socket. There is no `CTraderTradeService`, no `BuildNewOrderSingle*`, no QuickFIX initiator.

`CTraderFixLogonHostedService` after optional QUOTE:5211 / TRADE:5212 logon:

```68:71:D:\Prop\src\Fix.CTrader\Hosting\CTraderFixLogonHostedService.cs
        _runtime.RealCopyEnabled = false;

        _log.LogInformation("FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled). Account {Account}",
            quote.LoggedOn, trade.LoggedOn, account);
```

TRADE logon **≠** NewOrderSingle. Logged-on TRADE with no `35=D` builder is still **zero capital at risk**.

### 4.2 Flags forced off

| Surface | Value |
|---|---|
| `DependencyInjection` `LiveRuntimeStatus.RealCopyEnabled` | **`false`** (comment: “Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.”) |
| `CTraderFixOptions.RealCopyExecutionEnabled` | default **`false`** |
| API `/api/settings` `REAL_COPY_EXECUTION_ENABLED` | `runtime.RealCopyEnabled` (false) |
| API `/api/settings` `FEATURE_COPY_TRADING_ENABLED` | **hardcoded `false`** |
| `appsettings.json` `FeatureFlags:LiveCopyEnabled` | **`false`** |
| `/api/reconciliation/status` note | `"NewOrderSingle still off"` |
| `/api/ingest/status` `copyNote` when flag false | `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."` |

Flipping the flag to true would **still** not place an order: there is no sender. That is `SAFE_BY_ABSENCE`, not a unit-tested refuse-on-LoggedOn-TRADE gate. Do not tick Architecture §70.12 from this file. Do **not** enable `REAL_COPY_EXECUTION_ENABLED`.

### 4.3 “Copy” in this process is shadow rows only

`EfTradingStore.PersistDemoShadowAsync` writes `CopyIntent` with `Status = "SHADOW_ONLY"` and in-process `ShadowCopyEngine.SimulateEntry`. No socket. No MT5 `DealerSend`. No FIX `35=D` / `F` / `G` / `H`.

`DealIngestionService` only pulls catalog/deals/positions and upserts. Manager `Initialize` / `Connect` / `GroupRequestArray` / `UserRequestArray` / `DealRequestByGroup` / `PositionRequestByGroup` are **read** APIs. They do not place destination orders.

---

## 5. Residuals (do not flip the API TFM answer)

| Residual | Class | Why it does not make API “still net8.0” |
|---|---|---|
| `apps/mt5-worker` still `net8.0` AnyCPU; bin has **0** Manager DLLs; still scores dummy logins `10001…` | **FAIL** on that host | Different process. Architecture still wants the **Windows worker** to own `LoadLibrary`. |
| `apps/fix-worker` still `net8.0` but refs Windows `Infrastructure` | NU1201 / DI leak | FIX protocol does not need Manager64. Shared `AddTraderIntelligence` is a layering leak (C35). |
| `tests/Integration` still `net8.0` | NU1201 | Tests, not the API host. |
| Leftover `apps/api/bin/Debug/net8.0\` | operator trap | Current csproj output is `net8.0-windows`. |
| `RuntimeIdentifier` not `win-x64` | publish convenience | `PlatformTarget` x64 already pins bitness. |
| Linux compose `api` service | **cannot** load PE | Comment on compose already. Wrong OS, not wrong API csproj TFM. |
| `src/Application` is `net8.0` | expected | Ingest library must stay portable; the **host** is the loader. |
| This slot did not re-hash the copied trio in the API output folder | measurement gap | Files **exist by name** next to the exe; vendor hashes cited from A105/R021. |
| This slot did not re-run the live probe | measurement gap | Cite `LIVE_GROUPS_AND_TRADERS.json` as the last measured census. |

---

## 6. Risk to capital

**None from the API TFM / Manager64 load path.**

- Successful `LoadLibrary` + Manager connect only **reads** groups/users/deals/positions.
- Failed load (`NOTFOUND`, NU1201 leftover exe, Linux compose) yields empty/error status, not a destination order.
- Copy path cannot emit `35=D`. Shadow intents cannot hit the venue.
- Achiever proxy / Starwave direct affect **connect**, not cTrader send.

`risk_to_capital = none (SAFE_BY_ABSENCE)`.

---

## 7. Verdict box

| Claim | Result |
|---|---|
| API `TargetFramework` is portable `net8.0` | **FALSE now** — `net8.0-windows` |
| API `PlatformTarget` is x64 | **TRUE** |
| API `RuntimeIdentifier` is `win-x64` | **FALSE** (unset; not required for this PASS) |
| Current API `net8.0-windows` output can stage `MT5APIManager64.dll` | **TRUE** (file present beside exe) |
| Isolated `net8.0` Windows x64 can load the PE if the file is present | **TRUE** (R021) |
| Product `net8.0` host without Windows TFM can restore/copy/load in this graph | **FALSE** (NU1201 + missing trio) |
| Linux / x86 / ARM64 can load *this* `MT5APIManager64.dll` | **FALSE** |
| ALL Achiever+Starwave groups + ALL manager traders | **CODE yes; last live census 18 / 8460** |
| Copy-to-cTrader can send a live order | **FALSE** — no `35=D` |

**Slot 13 verdict: PASS.**

The assigned API host is already the Windows x64 TFM required to restore `src/Mt5` and to sit next to `MT5APIManager64.dll`. The leftover slogan is still the correct warning for **worker / tests / Linux compose / stale `net8.0` bins**, not for `TraderIntelligence.Api.csproj` as it sits today.
