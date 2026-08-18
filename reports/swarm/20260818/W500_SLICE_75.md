# W500_SLICE_75 — NativeMt5BrokerConnector vs API not net8.0-windows x64

| Field | Value |
|---|---|
| Slot | 75 |
| File | `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs` |
| Angle | API not `net8.0-windows` x64 so MetaQuotes DLL cannot load |
| Date | 2026-08-18 |
| Method | `read_file` of assigned file (full **458** lines) + owning/host csprojs + API nuget dgspec/cache + R021 pin; `grep` for `TargetFramework` / `PlatformTarget` / `MetaQuotes` / `DllImport` / send APIs |
| Product source modified | **No** |
| Verdict | **PASS** (substantive — file + TFM graph actually read; hypothesized defect is absent) |

---

## 1. What was read

Assigned type `NativeMt5BrokerConnector` (458 lines) is a live Manager-API collector: `ConnectCore` → `SMTManagerAPIFactory.Initialize` / `CreateManager` / `_manager.Connect`, then group/user/deal/position **reads**. No `[DllImport]`. No TFM declaration in the `.cs` (csproj owns that).

Supporting reads (not edited):

| Path | Measured |
|---|---|
| `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj` | `net8.0-windows` + `PlatformTarget` **x64**; copies `MetaQuotes.MT5*64.dll` + `MT5APIManager64.dll` |
| `D:/Prop/apps/api/TraderIntelligence.Api.csproj` | `net8.0-windows` + `PlatformTarget` **x64**; `ProjectReference` to Mt5 + Infrastructure |
| `D:/Prop/apps/api/obj/TraderIntelligence.Api.csproj.nuget.dgspec.json` | `originalTargetFrameworks: ["net8.0-windows"]` |
| `D:/Prop/apps/api/obj/project.nuget.cache` | `"success": true`, `logs: []` (no NU1201) |
| `D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj` | `net8.0-windows` + x64; constructs this type via `LiveMt5Registration` |
| `D:/Prop/reports/swarm/20260818/R021_dll_load.md` | Isolated `net8.0` **and** `net8.0-windows` x64 **can** load the vendor mixed-mode PE on Windows x64 |

---

## 2. Angle check

The assigned defect is: the **API host** is not `net8.0-windows` x64, therefore the MetaQuotes Manager PE cannot `LoadLibrary` and this connector cannot run.

**That sentence is false on the current tree.**

1. **This file’s owning project is already `net8.0-windows` x64** and copies the vendor trio next to output.
2. **The API host has been retargeted** to the same TFM/arch (older W500 notes that quoted `apps/api` as portable `net8.0` + NU1201 are **stale**). Restore now succeeds against `net8.0-windows7.0`.
3. **R021 already falsified the causal “`-windows` is required to Load” claim.** `MetaQuotes.MT5ManagerAPI64.dll` is Framework 4.7.2 mixed-mode **AMD64**. Isolated `net8.0` x64 on Windows loaded it and `Initialize` returned `MT_RET_OK` when `MT5APIManager64.dll` sat beside the exe. Missing `-windows` is **not** what blocks Load. x86 / Linux / missing native sibling **are**.

What this file actually does at load time: Windows OS gate, then `Initialize(dllDir)` with `NativeDllDirectory ?? AppContext.BaseDirectory`. It does **not** check `Environment.Is64BitProcess` (message says “Windows x64 only” but only `IsOSPlatform(Windows)` is tested). That is a residual hardness gap, not the assigned TFM defect: the API exe is `PlatformTarget` x64.

### Residual (not this slot’s FAIL)

| Item | State |
|---|---|
| Stale `D:\Prop\apps\api\bin\Debug\net8.0\` | Leftover portable output: `tfm: net8.0`, **zero** `MetaQuotes*` / `MT5APIManager64.dll` in `deps.json`. Do not launch that exe. Current source TFM is `net8.0-windows`. `bin\Debug\net8.0-windows\` exists but has no compiled outputs yet (obj editorconfig shows TFM; no `CoreCompileInputs`). |
| `apps/mt5-worker` | Still `net8.0`. Restore **FAIL NU1201** vs `net8.0-windows` Infrastructure/Mt5. Worker cannot host this connector until retargeted. Angle is **API**, not worker. |
| `tests/Integration` | Still `net8.0` + refs Mt5/Infrastructure (same NU1201 shape). Test host, not API. |

---

## 3. Evidence quotes

Connector binds official 64-bit Manager wrappers (not WebAPI / Gateway):

```1:4:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
using System.Runtime.InteropServices;
using MetaQuotes.MT5CommonAPI;
using MetaQuotes.MT5ManagerAPI;
using TraderIntelligence.Application.Contracts;
```

Connect is Windows-gated, then factory-init from the process directory (fail-closed on non-OK / non-duplicate):

```66:83:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
            if (init != MTRetCode.MT_RET_OK && init != MTRetCode.MT_RET_ERR_DUPLICATE)
            {
                LastError = Describe(init, "Factory.Initialize");
                throw new InvalidOperationException(LastError);
            }

            SMTManagerAPIFactory.GetVersion(out var version);
            var created = SMTManagerAPIFactory.CreateManager(version, out var createRes);
            if (createRes != MTRetCode.MT_RET_OK || created is null)
            {
                LastError = Describe(createRes, "CreateManager");
                throw new InvalidOperationException(LastError);
            }
```

Reads refuse work when the factory never connected:

```436:440:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void Ensure()
    {
        if (_manager is null || !_connected)
            throw new InvalidOperationException($"{BrokerCode} is not connected. {LastError}");
    }
```

Owning library TFM/arch + copy of the CMake trio (the hypothesized gap is **absent** here):

```6:30:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
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
    <PlatformTarget>x64</PlatformTarget>
```

`src/Mt5/bin/Release/net8.0-windows/` FileListAbsolute lists the three vendor PEs beside `TraderIntelligence.Mt5.dll`.

API host (the process named by the angle) is **now** the same TFM/arch:

```17:22:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

API restore pin (`TraderIntelligence.Api.csproj.nuget.dgspec.json`): `"originalTargetFrameworks": ["net8.0-windows"]`, framework `net8.0-windows7.0`, project refs include Mt5. `project.nuget.cache` `"success": true`.

R021 (measured, not folklore):

> Must the project be `net8.0-windows`? **No for the DLL itself.** Isolated `net8.0` compiled and loaded it. `net8.0-windows` is still the honest TFM for a process that will **run** this Windows mixed-mode image.

`grep` `OrderSend|DealerSend|TradeRequest|NewOrderSingle|DealAdd|PositionCreate\(` on the assigned file → **0 send hits** (only `acc?.Balance()` on the account DTO).

---

## 4. No-loss implication

**No capital-loss path on this angle.** `NativeMt5BrokerConnector` is a **read/pump** adapter (`IMt5BrokerConnector` / bulk deal + position readers). Pump flags are `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS`. There is no Manager dealer send, no balance/credit write, no FIX `NewOrderSingle`.

If the MetaQuotes image cannot load (wrong OS, x86 process, missing `MT5APIManager64.dll` next to `NativeDllDirectory`), `ConnectCore` throws and `_connected` stays false. `Ensure()` then refuses every read. A failed `LoadLibrary` / `Initialize` cannot reduce broker equity.

The hypothesized “API is not `net8.0-windows` x64 **so** the DLL cannot load” defect is **not present**: API + Mt5 + Infrastructure are `net8.0-windows` / x64, API restore is green, and R021 showed `-windows` is not the load gate anyway.

---

## 5. What this PASS is not

- Not a claim that `apps/api/bin/Debug/net8.0` (stale) can load Manager64 — that leftover has **no** vendor DLLs; do not run it.
- Not a PASS on `apps/mt5-worker` or `tests/Integration` (still `net8.0` → NU1201).
- Not a measured live `Connect` to Achiever/Starwave in this slot.
- Not a claim that `ConnectCore` checks process bitness (`Is64BitProcess` is not asserted).
- Not an empty PASS: the assigned file was read in full (458/458) and the API/Mt5 TFMs were re-read against the current tree.

Empty-PASS not used: the angle applies to this type (it is the MetaQuotes load site), and the measured result is **defect absent**, not **file inapplicable**.
