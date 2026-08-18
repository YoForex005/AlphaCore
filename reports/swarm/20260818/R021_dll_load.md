# R021 — Can net8 reference `MetaQuotes.MT5ManagerAPI64.dll`?

| Field | Value |
|---|---|
| Agent | R021 (focused worker; evidence-only) |
| Date | 2026-08-18 |
| Question | Can a **.NET 8** (`net8.0` / `net8.0-windows`) project **reference** `MetaQuotes.MT5ManagerAPI64.dll`? |
| Product source modified | **No.** This file is the only product-adjacent write. Scratch trees live under `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\`. |
| Host | Windows 10.0.26200 x64; .NET SDK **8.0.424**; runtime **Microsoft.NETCore.App 8.0.30**; also installed `Microsoft.WindowsDesktop.App 8.0.30`. No x86 runtime. |
| Not this file | E005 rule-id `R021` (REDUCE/CLOSE sizing). This report is **DLL load**, not that rule. |

**Verdict: YES — measured.** A `net8.0` or `net8.0-windows` SDK-style project can `<Reference>` the vendor mixed-mode C++/CLI wrapper, Roslyn can compile against its public types, and a **Windows x64** .NET 8 process can load the assembly and call `SMTManagerAPIFactory.Initialize` / `CreateManager`. That is **not** the same as “portable net8 everywhere,” **not** Linux, and **not** “project `net8.0` can consume a `net8.0-windows` sibling.”

Honest one-liner: **net8 can reference and load this AMD64 mixed-mode Framework 4.7.2 wrapper on Windows x64; factory init still needs `MT5APIManager64.dll` beside the process.**

---

## 1. What was asked vs what was measured

“Reference” is three different operations. All three were executed on this machine.

| Layer | Meaning | Result on this host |
|---|---|---|
| **A. MSBuild `<Reference>`** | `HintPath` to the PE; copy to output | **PASS** `net8.0` and `net8.0-windows`, 0 warnings (x64 / AnyCPU) |
| **B. Roslyn compile** | `using MetaQuotes.MT5ManagerAPI;` + `typeof(SMTManagerAPIFactory)` | **PASS** — `csc` gets `/reference:...\MetaQuotes.MT5ManagerAPI64.dll` |
| **C. Runtime load + factory** | `Assembly.LoadFrom` / default ALC; `Initialize` + `CreateManager` | **PASS** on win-x64 .NET 8.0.30 when native `MT5APIManager64.dll` is present |

Not measured (and not claimed): live `Connect` to a broker, Linux, Wine, collectible ALC (fails), x86 process (no 32-bit runtime here; PE is AMD64 anyway).

---

## 2. The file (not folklore)

Path: `D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll`

| Property | Measured |
|---|---|
| Bytes | 396,872 |
| SHA-256 | `41A66C5D65BAE8B114737FB18E330B19A424B1B295BC4FCB5FF9DC251AAAEDAB` |
| DOS / optional / machine | `MZ` / `0x020B` PE32+ / `0x8664` AMD64 |
| Subsystem | `WINDOWS_GUI` |
| CLR | Yes. CorHeader 2.5, metadata BSJB, CLI version **`v4.0.30319`** |
| `CorFlags` | `0x00000010` = **`NATIVE_ENTRYPOINT`**, **not `ILONLY`** → mixed-mode C++/CLI |
| Sections | `.text,.nep,.rdata,.data,.pdata,.rsrc,.reloc` (`.nep` = C++/CLI native entry) |
| Assembly name | `MetaQuotes.MT5ManagerAPI64, Version=5.5570.0.0, Culture=neutral, PublicKeyToken=null` |
| FileVersion / ProductVersion | `5.0.0.5584` (Win32 resource). `OriginalFilename=MetaQuotes.MT5ManagerAPI` |
| `TargetFrameworkAttribute` | **`.NETFramework,Version=v4.7.2`** / display name `.NET Framework 4.7.2` |
| Description | `MetaTrader 5 Manager .NET API Library` |
| Copyright | `Copyright 2000-2026, MetaQuotes Ltd.` |
| Mvid | `21b945a4-1989-47b8-b2ab-57b63d855645` |
| Strong name | none |
| Assembly refs | `mscorlib 4.0.0.0`, `System 4.0.0.0`, **`MetaQuotes.MT5CommonAPI64 5.5570.0.0`** |
| Type defs | 237 total; 13 exported types (factory + manager/admin/dealer sinks + `MTProxyInfo`) |
| Imports | `KERNEL32.dll`, `ADVAPI32.dll`, **`VCRUNTIME140.dll`**, **`VCRUNTIME140_1.dll`**, UCRT `api-ms-win-crt-*.dll`, **`mscoree.dll`** |
| Exports | none (managed + native entry; not a classic export table) |

This is **not** IL-only AnyCPU. It is **not** `netstandard` / `net8.0`. It is a **Framework 4.7.2 C++/CLI mixed-mode AMD64** wrapper that still exposes usable CLR metadata.

Required sibling (same folder, same FileVersion `5.0.0.5584`):

| File | Role | SHA-256 |
|---|---|---|
| `MetaQuotes.MT5CommonAPI64.dll` (1,046,632 B) | Mixed-mode common types (`MTRetCode`, deal/user wrappers). Same TFM 4.7.2, same CorFlags. | `DB28E45E082B9FAF86169739B5B08FF725C056A974A7A0A4955B649794C0DD2F` |
| `MT5APIManager64.dll` (7,185,272 B) | **Native** Manager factory (`HasClr=false`). What `Initialize` `LoadLibrary`s. | `51A590CD435B19005621EA5B419E86587C1BA513D4E2138617997F6842B430A9` |

Do **not** confuse with `MetaQuotes.MT5WebAPI.dll` (ILONLY, **i386** PE32, different API). Do **not** confuse with `MT5APIGateway64*.dll`.

Vendor .NET samples (`Examples/Manager/{Balance,Dealer}Example.NET`) are **.NET Framework 4.7.2 WinForms**, `PlatformTarget=x64`, HintPath `..\..\..\Libs\MetaQuotes.MT5ManagerAPI$(TargetNamePostfix).dll` with `TargetNamePostfix=64`. They do **not** target net8. That is the official sample TFM, not a prohibition on net8.

---

## 3. Compile-time reference (layer A+B)

Throwaway SDK projects (not product):

```xml
<TargetFramework>net8.0</TargetFramework>          <!-- also tried net8.0-windows -->
<PlatformTarget>x64</PlatformTarget>               <!-- also tried default AnyCPU -->
<Reference Include="MetaQuotes.MT5CommonAPI">
  <HintPath>D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
  <Private>true</Private>
</Reference>
<Reference Include="MetaQuotes.MT5ManagerAPI">
  <HintPath>D:\Prop\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
  <Private>true</Private>
</Reference>
```

`Include=` is only the MSBuild item name. The **assembly identity** is `MetaQuotes.MT5ManagerAPI64`. Both `Include="MetaQuotes.MT5ManagerAPI"` (vendor style) and `Include="MetaQuotes.MT5ManagerAPI64"` (current product `src/Mt5` style) resolve via `HintPath`.

| Host project TFM | PlatformTarget | `dotnet build -c Release` | Notes |
|---|---|---|---|
| `net8.0` | x64 | **0/0** (exit 0) | `csc /platform:x64 /reference:...ManagerAPI64.dll` |
| `net8.0-windows` | x64 | **0/0** (exit 0) | Same, defines `WINDOWS` |
| `net8.0` | (default AnyCPU) | **0/0** (exit 0) | 64-bit process on this host |
| `net8.0` | x86 | **1 warning CS8012**, 0 errors | “targets a different processor.” App-host then failed: **no x86 .NET 8 runtime** on this box (`0x80008083`) |

`deps.json` records them as type `reference`, versions `5.5570.0.0`, `fileVersion` `5.0.0.5584`. `runtimeconfig.json` stays `tfm: net8.0` / `Microsoft.NETCore.App 8.0.0`. No special `rollForward`, no `useLegacyV2RuntimeActivationPolicy` (that is Framework-only), no WindowsDesktop pack required **just to compile and load the wrapper**.

CMake already copies the same trio for C++ probes (`mt5sdk_copy_runtime_dlls` in `mt5-sdk/CMakeLists.txt`). That is a **copy** contract, not a C# reference.

---

## 4. Runtime load (layer C)

Measured in `InspectMeta` + `LoadNet8` + `RefNet8` / `RefNet8Win` (all .NET 8.0.30, process X64):

| Operation | Result |
|---|---|
| `Assembly.LoadFrom(path)` | **OK** `MetaQuotes.MT5ManagerAPI64, Version=5.5570.0.0, ...` `ImageRuntimeVersion=v4.0.30319` |
| `AssemblyLoadContext.Default.LoadFromAssemblyPath` | **OK** (same assembly) |
| New **collectible** ALC `LoadFromAssemblyPath` | **FAIL** `System.BadImageFormatException`: *Cannot load a mixed assembly into a collectible AssemblyLoadContext.* |
| `NativeLibrary.Load` (as a native PE) | **OK** handle returned (it is also a native image). This is **not** how C# should consume it. |
| `typeof(SMTManagerAPIFactory)` after `<Reference>` | **OK** AQN includes `MetaQuotes.MT5ManagerAPI64, Version=5.5570.0.0` |
| `SMTManagerAPIFactory.ManagerAPIVersion` | **`5570`** |
| `SMTManagerAPIFactory.ManagerAPIDate` | **`30 Jan 2026`** |
| `Initialize(null)` with `MT5APIManager64.dll` next to exe | **`MT_RET_OK` (0)** |
| `CreateManager(ManagerAPIVersion, out res)` | **`MT_RET_OK`, manager not null** |
| `Shutdown()` | **OK** |
| `Initialize(null)` **without** native `MT5APIManager64.dll` | Wrapper **still loads**. Init returns **`MT_RET_ERR_NOTFOUND` (13)**. Process does not crash. |

Public factory surface used by vendor `BalanceExample.NET/Manager.cs` and by product `NativeMt5BrokerConnector`:

```text
MTRetCode Initialize(string)
MTRetCode Shutdown()
MTRetCode GetVersion(ref uint)
CIMTManagerAPI CreateManager(uint, string, ref MTRetCode)
CIMTManagerAPI CreateManager(uint, ref MTRetCode)
CIMTAdminAPI   CreateAdmin(...)
MTRetCode LicenseCheckManager / LicenseCheckAdmin
uint   ManagerAPIVersion   = 5570
string ManagerAPIDate      = "30 Jan 2026"
```

`Connect(...)` to a live manager server was **not** called. Factory create ≠ broker session.

---

## 5. Conditions that stay true even though net8 works

| Constraint | Why |
|---|---|
| **Windows x64 process** | PE32+ AMD64 + `KERNEL32` + `mscoree` + VC++ CRT. Linux / WSL2 / Linux container = cannot map the PE. Architecture §5 already forbids faking this. |
| **VC++ 2015–2022 x64 redistributable** | Imports `VCRUNTIME140.dll` and `VCRUNTIME140_1.dll`. |
| **Copy `MetaQuotes.MT5CommonAPI64.dll` beside it** | Hard assembly ref, same version 5.5570.0.0, unsigned. |
| **Copy `MT5APIManager64.dll` beside it (or pass its directory to `Initialize`)** | Wrapper load ≠ factory init. Missing native file → `MT_RET_ERR_NOTFOUND`. |
| **Do not use a collectible `AssemblyLoadContext`** | Mixed-mode is rejected. Default ALC is fine. |
| **Do not target x86 / ARM64 for this file** | This exact file is `0x8664`. ARM64 would need `MetaQuotes.MT5ManagerAPIARM.dll` (vendor postfix `ARM`), which is a different PE. |
| **Do not treat TFM `net8.0` as “runs on Linux”** | Portable TFM compiles. Runtime of this DLL is still Windows. |
| **Keep wrapper + native + headers from the same SDK drop** | FileVersion `5.0.0.5584` / API `5570` / date `30 Jan 2026`. Mixing an older `MT5APIManager64.dll` is `UNSAFE`. |

Microsoft did **not** ship a net8 build of this wrapper. Compatibility here is ** empirically** “.NET 8 on Windows will load this Framework 4.7.2 mixed-mode C++/CLI image.” That can regress with a future runtime or a future MetaQuotes drop. Pin hashes.

---

## 6. Product tree (observed only — not edited by this agent)

Current `D:\Prop\src\Mt5\TraderIntelligence.Mt5.csproj` **already** has:

- `TargetFramework` = **`net8.0-windows`**
- `PlatformTarget` = **x64**
- `<Reference Include="MetaQuotes.MT5ManagerAPI64">` + CommonAPI HintPaths into `mt5-sdk\vendor\MetaTrader5SDK\Libs\`
- `<None CopyToOutputDirectory="PreserveNewest">` of the CMake trio

`D:\Prop\src\Mt5\Connectors\NativeMt5BrokerConnector.cs` already `using MetaQuotes.MT5ManagerAPI` and calls `SMTManagerAPIFactory.Initialize` / `CreateManager` / `Connect`. That is a **product** consumer, not this report’s experiment.

Measured product builds **after** that (still no source edits here):

| Project | TFM | Build |
|---|---|---|
| `src/Mt5` | `net8.0-windows` | **PASS** 0/0 → `bin\Release\net8.0-windows\TraderIntelligence.Mt5.dll` |
| `src/Infrastructure` | `net8.0-windows` | (already retargeted; consumed below) |
| Isolated `net8.0` RefNet8 | `net8.0` **direct** HintPath to the DLL | **PASS** |
| `apps/mt5-worker` | `net8.0` → refs Mt5 + Infrastructure | **FAIL NU1201** (`net8.0` cannot reference `net8.0-windows7.0`) |
| `apps/api` | `net8.0` → refs Infrastructure | **FAIL NU1201** |
| `tests/Integration` | `net8.0` → refs both | **FAIL NU1201** |

This NU1201 is **project TFM mismatch**, not “net8 cannot reference the vendor DLL.” Isolated `net8.0` **can** reference the DLL. A `net8.0` exe **cannot** `ProjectReference` a `net8.0-windows` library. If the worker must stay portable `net8.0`, keep the Manager wrapper behind a Windows-only project **or** retarget the worker to `net8.0-windows` + x64. This agent did **not** change that.

---

## 7. Recommended reference snippet (documentation only)

For a Windows x64 net8 host that is allowed to take a Windows TFM:

```xml
<PropertyGroup>
  <TargetFramework>net8.0-windows</TargetFramework>
  <PlatformTarget>x64</PlatformTarget>
</PropertyGroup>
<ItemGroup>
  <Reference Include="MetaQuotes.MT5CommonAPI64">
    <HintPath>$(MSBuildThisFileDirectory)..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
    <Private>true</Private>
  </Reference>
  <Reference Include="MetaQuotes.MT5ManagerAPI64">
    <HintPath>$(MSBuildThisFileDirectory)..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
    <Private>true</Private>
  </Reference>
  <None Include="..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MT5APIManager64.dll"
        CopyToOutputDirectory="PreserveNewest"
        Link="MT5APIManager64.dll" />
</ItemGroup>
```

`Private=true` copies the two managed wrappers. The native DLL must be a `None`/`Content` copy — it has no CLR metadata, so `<Reference>` on `MT5APIManager64.dll` is the wrong item type.

`Initialize` should be passed an **absolute directory** (`vendor\...\Libs` or the app base after copy), not `null` + PATH. `null` worked in the scratch exe only because the native file sat next to it.

---

## 8. Experiment artifacts (scratch, not product)

| Path | What |
|---|---|
| `D:\Prop\reports\swarm\20260818\_tmp_r021_dll_load\pe_inspect.ps1` + `pe_inspect.json` | PE/CLR dump |
| `...\InspectMeta\` | `System.Reflection.Metadata` + MetadataLoadContext + `Assembly.LoadFrom` |
| `...\RefNet8\` | `net8.0` x64 compile + factory init **PASS** |
| `...\RefNet8Win\` | `net8.0-windows` x64 compile + factory init **PASS** |
| `...\RefNet8Any\` | default AnyCPU on 64-bit host **PASS**; printed `ManagerAPIDate` |
| `...\RefNet8x86\` | compile CS8012; run missing x86 runtime |
| `...\LoadNet8\` | LoadFrom / ALC / NativeLibrary |
| `...\_nonative\` | wrapper loads; `Initialize` → `MT_RET_ERR_NOTFOUND` |

---

## 9. Answer box

| Question | Answer |
|---|---|
| Can **net8** `<Reference>` `MetaQuotes.MT5ManagerAPI64.dll`? | **Yes.** Measured on SDK 8.0.424 / runtime 8.0.30. |
| Must the project be `net8.0-windows`? | **No for the DLL itself.** Isolated `net8.0` compiled and loaded it. `net8.0-windows` is still the honest TFM for a process that will **run** this Windows mixed-mode image. |
| Must it be Framework 4.7.2 like the vendor samples? | **No.** Samples are 4.7.2; net8 interop works here without a Framework host. |
| Is it a “normal” managed net8 assembly? | **No.** Mixed-mode C++/CLI, TFM **net472**, native entry, VC++ CRT, AMD64-only. |
| Does referencing it make `mt5-worker` Linux-legal? | **No.** |
| Does factory `Initialize` work without `MT5APIManager64.dll`? | **No.** `MT_RET_ERR_NOTFOUND`. |
| Was a live manager `Connect` proven? | **No.** |
| Did this agent change product source? | **No.** |

**PASS** for the asked question (net8 can reference the DLL). **Not** a go-live claim for Achiever/StarwaveFX connectivity.
