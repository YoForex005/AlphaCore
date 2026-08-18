# W500_RESEARCH_53 — Api.csproj TargetFramework vs `MT5APIManager64` load (slot 53)

| Field | Value |
|---|---|
| Slot | **53** |
| Wave | W500 research |
| Date | 2026-08-18 |
| Artifact | `D:\Prop\reports\swarm\20260818\W500_RESEARCH_53.md` |
| Assigned topic | Check `Api.csproj` `TargetFramework`. `net8.0` without windows/x64 cannot load `MT5APIManager64`. |
| Goal overlay | Fetch **ALL** Achiever + StarwaveFX groups and **ALL** manager traders. Copy to cTrader must **not** send live orders (no loss). |
| Product source modified | **No.** This report is the only write. |
| Secrets printed | **None.** Key *names*, non-secret host/login/group identifiers only. No passwords, proxy auth, or FIX password values. |
| Method | Independent `read_file` + `grep` + `list_dir` on `D:\Prop` and `D:\Projects\YoPips\Backend\C++ Backend PropFirm`. Re-read current csproj / dgspec / bin listings / native connector / FIX send surface / R021 PE inspect / live probe JSON. No shell. No product edit. |
| Same-topic sibling | `W500_RESEARCH_13.md` (same TFM question, earlier slot). This file is a **re-measure**, not a copy. |
| Stale siblings (do not treat as current API state) | `A012_worker_tfm.md`, `A002_api_dummy_path.md` (both quote API as portable `net8.0`). `R021` NU1201 row for `apps/api` is **historical** (pre-retarget). |

**Verdict: PASS on the assigned API host. Residual FAIL on worker / tests / Linux compose / leftover `net8.0` bins. Live send: `SAFE_BY_ABSENCE`.**

There is **no** file named `Api.csproj`. The API host is `D:\Prop\apps\api\TraderIntelligence.Api.csproj`. As of this re-read it is already:

- `<TargetFramework>net8.0-windows</TargetFramework>`
- `<PlatformTarget>x64</PlatformTarget>`

Restore dgspec `originalTargetFrameworks` = `net8.0-windows` (framework id `net8.0-windows7.0`). Generated editorconfig: `build_property.TargetFramework = net8.0-windows`. Current Debug output `D:\Prop\apps\api\bin\Debug\net8.0-windows\` **contains** `MT5APIManager64.dll` plus both MetaQuotes mixed-mode wrappers, and `TraderIntelligence.Api.deps.json` records `MetaQuotes.MT5ManagerAPI64/5.5570.0.0` + `MetaQuotes.MT5CommonAPI64/5.5570.0.0`.

The hypothesized defect “API is still portable `net8.0`, therefore Manager64 cannot load” is **absent on this host**. The slogan remains the correct warning for **every other `net8.0` process that still `ProjectReference`s `src/Mt5`**.

---

## 0. Answers (measured this slot)

| Question | Answer |
|---|---|
| What is `TraderIntelligence.Api.csproj` TFM **now**? | **`net8.0-windows`** + **`PlatformTarget` x64**. Restore alias `net8.0-windows7.0`. |
| Does `Directory.Build.props` set TFM / RID / `PlatformTarget`? | **No.** LangVersion / Nullable / ImplicitUsings / TreatWarningsAsErrors / Deterministic only. |
| Is `RuntimeIdentifier=win-x64` set on API? | **No.** Bitness is pinned by `PlatformTarget` x64, not by RID. |
| Does current API output contain `MT5APIManager64.dll`? | **Yes** — `D:\Prop\apps\api\bin\Debug\net8.0-windows\MT5APIManager64.dll` + `MetaQuotes.MT5ManagerAPI64.dll` + `MetaQuotes.MT5CommonAPI64.dll`. |
| Does leftover `bin\Debug\net8.0\` contain it? | **No.** Stale portable output. Operator trap if launched. `deps.json` in that folder has **zero** `MetaQuotes` / `MT5APIManager` tokens. |
| Can isolated portable `net8.0` *load the PE* on Windows x64 if the file is present? | **Yes (R021).** `SMTManagerAPIFactory.Initialize` → `MT_RET_OK`. Missing native file → `MT_RET_ERR_NOTFOUND` (13). |
| Can a product `net8.0` host *without* Windows TFM load it **as this graph sits**? | **No.** NU1201 vs `src/Mt5` / `src/Infrastructure` (`net8.0-windows7.0`). Trio never copies. Type-load fails before `LoadLibraryW`. |
| Can Linux / compose `mcr.microsoft.com/dotnet/sdk:8.0` load it? | **No.** PE32+ AMD64 + `LoadLibraryW`. Compose comment already forbids this. |
| Can x86 / ARM64 Windows load *this* file? | **No.** Machine `0x8664`. ARM wants `MT5APIManager64arm.dll` (`0xAA64`). |
| Does API enumerate **all** Achiever + Starwave groups + traders? | **Code: yes** (`GroupRequestArray("*")` + per-group `UserRequestArray` / `UserLogins`). **Last live probe: yes** — 8+10 groups, 6512+1948 accounts. Scoring after ingest is **deals-only** (`ListLoginsWithDealsAsync`), not a second filter on the catalog. |
| Can copy-to-cTrader send a live order from this process? | **No.** No `35=D` builder. `RealCopyEnabled` forced `false`. FIX host only writes Logon `35=A`. CopyIntents are `SHADOW_ONLY`. |

Honest one-liner:

```text
API is net8.0-windows x64 and already stages MT5APIManager64.dll.
net8.0 without Windows/x64 cannot restore this graph (NU1201) and cannot LoadLibrary the PE on Linux/x86.
Isolated Windows x64 net8.0 CAN load the PE if the file is present (R021).
Live catalog is ALL groups / ALL manager logins (last census 18/8460). Live 35=D does not exist.
```

---

## 1. Current TFM census (source of truth = csproj + dgspec + editorconfig)

### 1.1 Assigned host — API

`D:\Prop\apps\api\TraderIntelligence.Api.csproj` (full PropertyGroup + the Mt5 reference that makes TFM matter):

```3:8:D:\Prop\apps\api\TraderIntelligence.Api.csproj
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
```

```17:22:D:\Prop\apps\api\TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

Restore (`D:\Prop\apps\api\obj\TraderIntelligence.Api.csproj.nuget.dgspec.json`):

| Key | Value |
|---|---|
| `originalTargetFrameworks` | `net8.0-windows` |
| restore framework | `net8.0-windows7.0` (alias `net8.0-windows`) |
| `RuntimeIdentifier` | **absent** |
| `EnableWindowsTargeting` | **absent** |
| `RestoreSuccess` (`nuget.g.props`) | **True** |

MSBuild editorconfig of the **current** output:

```text
D:\Prop\apps\api\obj\Debug\net8.0-windows\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig
build_property.TargetFramework = net8.0-windows
```

Stale leftover (do not launch):

```text
D:\Prop\apps\api\obj\Debug\net8.0\TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig
build_property.TargetFramework = net8.0
```

`runtimeconfig.json` `tfm` is `"net8.0"` even under `bin\Debug\net8.0-windows\`. That is **normal** for a `net8.0-windows` host (base framework moniker). It is **not** evidence the project is portable `net8.0`. Frameworks listed: `Microsoft.NETCore.App` + `Microsoft.AspNetCore.App` only. R021 already showed the WindowsDesktop pack is **not** required just to load the MetaQuotes mixed-mode image.

### 1.2 Who else is Windows x64 vs leftover `net8.0`

`Directory.Build.props` does **not** override TFM:

```1:9:D:\Prop\Directory.Build.props
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
  </PropertyGroup>
</Project>
```

| Project | Path | TFM | `PlatformTarget` | Can `ProjectReference` `src/Mt5`? | Stages Manager trio? |
|---|---|---|---|---|---|
| API | `apps/api/TraderIntelligence.Api.csproj` | **`net8.0-windows`** | **x64** | **Yes** | **Yes** (`bin\Debug\net8.0-windows\`) |
| LiveBrokerProbe | `tools/LiveBrokerProbe/LiveBrokerProbe.csproj` | **`net8.0-windows`** | **x64** | Yes | via Mt5 copy |
| `src/Mt5` | `TraderIntelligence.Mt5.csproj` | **`net8.0-windows`** | **x64** | (self) | **Yes** — HintPath + `CopyToOutputDirectory` |
| `src/Infrastructure` | `TraderIntelligence.Infrastructure.csproj` | **`net8.0-windows`** | **x64** | Yes | transitive |
| `src/Domain` | portable | `net8.0` | — | N/A | no |
| `src/Application` | portable | `net8.0` | — | N/A | no (must stay portable) |
| `src/Fix.CTrader` | portable | `net8.0` | — | N/A | no (FIX is not Manager64) |
| **mt5-worker** | `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` | **`net8.0`** | **absent** | **NU1201** | **No** — `bin\Debug\net8.0\` has **zero** MetaQuotes / `MT5APIManager64.dll` (listing re-checked this slot) |
| **fix-worker** | `apps/fix-worker/TraderIntelligence.FixWorker.csproj` | **`net8.0`** | **absent** | NU1201 (refs Infrastructure) | should not load Manager64 |
| **Integration tests** | `tests/Integration/TraderIntelligence.Tests.Integration.csproj` | **`net8.0`** | absent | NU1201 (refs Mt5 + Infrastructure) | no |
| **Unit tests** | `tests/Unit/TraderIntelligence.Tests.Unit.csproj` | **`net8.0`** | absent | no Mt5 ref | n/a |

mt5-worker dgspec still records `"originalTargetFrameworks": ["net8.0"]` while listing project references to `Infrastructure` + `Mt5` (`net8.0-windows`). That is the NU1201 pair R021 measured. **Different process from API.** Slot 53 is the API TFM check; the worker residual is recorded so nobody greenwashes “the whole tree is Windows now.”

### 1.3 A012 / A002 / R021 API-NU1201 are stale

- `A012_worker_tfm.md` §0: “All three [api / mt5-worker / fix-worker] still target `net8.0`.” True when written. **API has since been retargeted.**
- `A002_api_dummy_path.md` asked “Is TFM `net8.0` instead of `net8.0-windows` x64?” and answered **YES**. **False now.**
- R021 product-tree table: `apps/api` `net8.0` → **FAIL NU1201**. Historical. Current API dgspec is `net8.0-windows` / restore **True**.

Worker + FIX + Integration tests have **not** been retargeted. Do not quote A012 as the API state of record. Do not quote this file as “workers are fixed.”

---

## 2. Why “`net8.0` without windows/x64 cannot load `MT5APIManager64`” is only *mostly* true

Three different operations. Do not collapse them.

### 2.1 The file is a Windows AMD64 PE, not a net8 asset

Vendor drop (R021 `pe_inspect.json`, same folder `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\`):

| File | Bytes | Machine | Kind | SHA-256 |
|---|---:|---|---|---|
| `MT5APIManager64.dll` | 7,185,272 | `0x8664` AMD64 | **native** (`HasClr=false`) | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |
| `MetaQuotes.MT5ManagerAPI64.dll` | 396,872 | `0x8664` | mixed-mode C++/CLI, `.NETFramework,Version=v4.7.2`, CorFlags `NATIVE_ENTRYPOINT` | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| `MetaQuotes.MT5CommonAPI64.dll` | 1,046,632 | `0x8664` | same mixed-mode family | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |

InspectMeta (`_tmp_r021_dll_load\inspect_stdout.txt`): wrapper assembly `MetaQuotes.MT5ManagerAPI64, Version=5.5570.0.0`, `PEHeaders.CoffHeader.Machine=Amd64`, `Magic=PE32Plus`, `ILONLY=False`, `NativeEntryPoint=True`. `SMTManagerAPIFactory.Initialize(string)` is the C# entry that maps the native factory.

Factory loader (`MT5APIManager.h` 1769–1831, same text in YoPips `MetaTrader5SDK\Include\MT5APIManager.h`): `FindLibrary` prefers AVX2 / AVX / ARM then vanilla `MT5APIManager64.dll`, then three parent folders + `.\libs\`, then **PATH** (always returns `true` with a bare filename). Product must pass an **absolute directory**. Product does: `NativeDllDirectory = AppContext.BaseDirectory`.

There is no `dlopen` path. Missing file or failed map → `MT_RET_ERR_NOTFOUND`.

### 2.2 Isolated PE fact (R021) — TFM `net8.0` is not the LoadLibrary bit

Scratch hosts under `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\`:

| Isolated experiment | Result |
|---|---|
| `RefNet8` `net8.0` x64 `<Reference>` Manager64 + CommonAPI64 | compile **PASS** |
| `RefNet8Win` `net8.0-windows` x64 same | compile **PASS** |
| Windows x64 process `Initialize` with trio beside exe | **`MT_RET_OK` (0)** |
| Same without `MT5APIManager64.dll` (`_nonative`) | wrapper still loads; init **`MT_RET_ERR_NOTFOUND` (13)** |
| Collectible ALC | `BadImageFormatException` (mixed-mode) |
| `RefNet8x86` `net8.0` x86 | CS8012; no x86 runtime on this box; PE is AMD64 anyway |
| Linux / Wine / Linux container | **cannot** map PE32+ |

So: retargeting the API to `net8.0-windows` + x64 is the **honest product TFM** (Windows-only Manager host + NU1201 fix + copy-dlls flow). It is **not** a magic extra PE flag. `net8.0` x64 already could load the PE **in isolation** when the native file sits beside the exe.

### 2.3 Product-graph fact — `net8.0` host **cannot** consume this `src/Mt5`

`src/Mt5` is the only project that binds the official trio:

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

A portable `net8.0` exe **cannot** `ProjectReference` a `net8.0-windows7.0` library (NU1201). That is why leftover `apps/api/bin/Debug/net8.0\` still has `TraderIntelligence.Mt5.dll` from an older portable build and **zero** of the trio (re-confirmed: grep of that folder’s `deps.json` = no `MetaQuotes` / `MT5APIManager`). That leftover **cannot** `LoadLibrary` Manager64 because the native file is not next to the exe. `NativeMt5BrokerConnector` also JITs `MetaQuotes.MT5ManagerAPI` — missing wrappers → `FileNotFoundException` / `BadImageFormatException` **before** `ConnectAsync`.

`apps/mt5-worker/bin/Debug/net8.0\` listing (this slot): `TraderIntelligence.Mt5.dll` is present; **no** `MT5APIManager64.dll`, **no** `MetaQuotes.MT5*.dll`. Same product-graph failure, still live on the worker.

### 2.4 Current API output (the live contract)

`D:\Prop\apps\api\bin\Debug\net8.0-windows\` listing includes:

- `TraderIntelligence.Api.exe`
- `TraderIntelligence.Mt5.dll`
- `MT5APIManager64.dll`
- `MetaQuotes.MT5ManagerAPI64.dll`
- `MetaQuotes.MT5CommonAPI64.dll`
- Manager cache trees `bases/AchieverGlobalMarkets-Server/` (login **2027**) and `bases/StarwaveFX-Server/` (login **9904`)

`deps.json` runtime entries (not a copy of the native PE — native is a `None` copy item):

```500:515:D:\Prop\apps\api\bin\Debug\net8.0-windows\TraderIntelligence.Api.deps.json
      "MetaQuotes.MT5ManagerAPI64/5.5570.0.0": {
        "runtime": {
          "MetaQuotes.MT5ManagerAPI64.dll": {
            "assemblyVersion": "5.5570.0.0",
            "fileVersion": "5.0.0.5584"
          }
        }
      },
      "MetaQuotes.MT5CommonAPI64/5.5570.0.0": {
        "runtime": {
          "MetaQuotes.MT5CommonAPI64.dll": {
            "assemblyVersion": "5.5570.0.0",
            "fileVersion": "5.0.0.5584"
          }
        }
      }
```

This slot did **not** re-hash the copied trio in the API output folder. Files exist by name next to the exe; vendor hashes cited from R021 `pe_inspect.json`.

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

```35:46:D:\Prop\src\Infrastructure\DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        var runtime = new LiveRuntimeStatus
        {
            // Live NewOrderSingle is not implemented. Do not arm a flag that cannot be honored safely.
            RealCopyEnabled = false
        };
        ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
```

### 2.6 Compose / Linux trap (not a missing API TFM)

`D:\Prop\docker-compose.yml` `api` service: `image: mcr.microsoft.com/dotnet/sdk:8.0` (Linux) + `dotnet run --project apps/api/TraderIntelligence.Api.csproj`. Comment on disk: “Native MT5 Manager DLL workers stay on Windows hosts. Do not put them in Linux containers.”

This is a **deployment contradiction**, not evidence that `TraderIntelligence.Api.csproj` is still portable `net8.0`:

1. `net8.0-windows` will not restore/run on Linux without `EnableWindowsTargeting`, and even then the host is the wrong OS.
2. Linux cannot `LoadLibraryW` a PE32+ AMD64 image.
3. `NativeMt5BrokerConnector` throws `PlatformNotSupportedException` before init.

Do **not** copy `MT5APIManager64.dll` into a Debian/Alpine image “to have it nearby.” That is `UNSAFE` (A54 / A105 / D63).

---

## 3. Goal: ALL Achiever + Starwave groups and ALL manager traders

### 3.1 C# walk is mapping-blind (no `demo\` filter, no ingest `Take`)

Groups — request mask `"*"` then pump-cache fallback:

```152:183:D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs
            var arr = _manager!.GroupCreateArray();
            try
            {
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

Dedup is by login only. Grep of `D:\Prop\src\Application` for `Take(` = **0**. Dashboard `GET /api/trades` still `.Take(200)` — reconstructed-trades HTTP page, **not** Manager enumeration.

`DealIngestionService.SyncCatalogAsync` persists whatever the connector returns (`GetGroupsAsync` + `GetAccountsAsync(null)`). `LiveIngestHostedService` then:

- catalogs **all** groups + **all** accounts
- scores `store.ListLoginsWithDealsAsync` (deal-bearing subset). That is **not** a second filter on the catalog write.

API `POST /api/ops/resync` loops **both** `"ACHIEVER"` and `"STARWAVEFX"` and scores `ListLoginsAsync` (every persisted login).

`apps/mt5-worker/Worker.cs` still scores the dummy quartet `10001/10002/10003/99001` after `SyncBrokerAsync`. That worker is **not** the API live path and **cannot** load Manager64 until it is retargeted.

### 3.2 YoPips C++ (same SDK, same ALL-groups contract)

`D:\Projects\YoPips\Backend\C++ Backend PropFirm\src\core\mt5_manager.cpp` `GetAllGroups` = `GroupTotal` + `GroupNext` (no mask filter):

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

That walk is **cache-only**. Empty cache after pump-none can look like “zero groups.” C# prefers **network** `GroupRequestArray("*")` then falls back to the C++ cache walk. That is **stricter** than the C++ probe when pump is off.

Connect recipe (identifiers only; passwords not printed):

| Broker | Server:port (catalog / architecture) | Manager login (cache tree) | Proxy |
|---|---|---:|---|
| Achiever | `57.128.141.65:443` | 2027 | HTTP via allow-list `81.29.145.69:49527` **required** on this LAN (else **1012**) |
| StarwaveFX | `84.201.6.142:443` | 9904 | **off** |

C# `Connect` tries pump `GROUPS|USERS|POSITIONS` then `PUMP_MODE_NONE`. Request APIs remain valid after the fallback.

### 3.3 Live measurement (same connector family as API)

`D:\Prop\reports\swarm\20260818\LIVE_GROUPS_AND_TRADERS.json` (`probe=LiveBrokerProbe`, utc `2026-08-18T08:42:16Z`, `envLoaded=true`, passwords not written). Probe csproj is `net8.0-windows` x64 and calls `LiveMt5Registration.CreateConnectorsFromEnvironment()` → same `NativeMt5BrokerConnector`.

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

This slot did **not** re-run `LiveBrokerProbe` and did **not** re-hit HTTP. Completeness vs a second independent C++ `GroupTotal` in *this* process is **not** re-proven here. The C# mask `"*"` + live JSON 18/8460 is the evidence of record.

---

## 4. Copy to cTrader must not send live orders (no loss)

### 4.1 No `35=D` exists

Re-grep of `D:\Prop\src\Fix.CTrader` this slot: **zero** `35=D` / `MsgType="D"` builders. The only outbound FIX the API host can write is `CTraderFixSession.BuildLogon` tag **35=`A`**:

```96:108:D:\Prop\src\Fix.CTrader\Sessions\CTraderFixSession.cs
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
```

`ssl.WriteAsync` sends that logon only. There is no `CTraderTradeService`, no `BuildNewOrderSingle*`, no QuickFIX initiator.

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
| `DependencyInjection` `LiveRuntimeStatus.RealCopyEnabled` | **`false`** (comment: “Live NewOrderSingle is not implemented.”) |
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
| `apps/fix-worker` still `net8.0` but refs Windows `Infrastructure` | NU1201 / DI leak | FIX protocol does not need Manager64. Shared `AddTraderIntelligence` is a layering leak. |
| `tests/Integration` still `net8.0` + refs Mt5 | NU1201 | Tests, not the API host. |
| Leftover `apps/api/bin/Debug/net8.0\` and `obj/Debug/net8.0\` | operator trap | Current csproj output is `net8.0-windows`. |
| `RuntimeIdentifier` not `win-x64` | publish convenience | `PlatformTarget` x64 already pins bitness. |
| Linux compose `api` service | **cannot** load PE | Comment on compose already. Wrong OS, not wrong API csproj TFM. |
| `src/Application` is `net8.0` | expected | Ingest library must stay portable; the **host** is the loader. |
| This slot did not re-hash the copied trio in the API output folder | measurement gap | Files **exist by name** next to the exe; vendor hashes cited from R021. |
| This slot did not re-run the live probe | measurement gap | Cite `LIVE_GROUPS_AND_TRADERS.json` as the last measured census. |

---

## 6. Risk to capital

**None from the API TFM / Manager64 load path.**

- Successful `LoadLibrary` + Manager connect only **reads** groups/users/deals/positions.
- Failed load (`NOTFOUND`, leftover `net8.0` exe, Linux compose) yields empty/error status, not a destination order.
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
| Current API `net8.0-windows` output can stage `MT5APIManager64.dll` | **TRUE** (file present beside exe; deps records wrappers 5.5570.0.0) |
| Isolated `net8.0` Windows x64 can load the PE if the file is present | **TRUE** (R021) |
| Product `net8.0` host without Windows TFM can restore/copy/load in this graph | **FALSE** (NU1201 + missing trio) — still true for **worker / tests** |
| Linux / x86 / ARM64 can load *this* `MT5APIManager64.dll` | **FALSE** |
| ALL Achiever+Starwave groups + ALL manager traders | **CODE yes; last live census 18 / 8460** |
| Copy-to-cTrader can send a live order | **FALSE** — no `35=D` |

**Slot 53 verdict: PASS.**

The assigned API host is already the Windows x64 TFM required to restore `src/Mt5` and to sit next to `MT5APIManager64.dll`. The leftover slogan is still the correct warning for **worker / tests / Linux compose / stale `net8.0` bins**, not for `TraderIntelligence.Api.csproj` as it sits today.
