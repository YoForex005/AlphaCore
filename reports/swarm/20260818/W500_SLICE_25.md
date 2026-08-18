# W500_SLICE_25

- **slot:** 25
- **file:** `D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (330 lines) via `read_file`; grep on `src/Mt5` and `*.csproj` for `net8.0|TargetFramework|RuntimeIdentifier|win-x64|PlatformTarget|MetaQuotes|DllImport`
- **owning project:** `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj`
- **verdict:** PASS

## Evidence quotes

The assigned type is a Manager-API adapter. It binds the vendor C++/CLI wrappers (`MetaQuotes.MT5CommonAPI`, `MetaQuotes.MT5ManagerAPI`) and calls `SMTManagerAPIFactory` — it does **not** declare a TFM (csproj does). It also does **not** P/Invoke; there is no `[DllImport]`.

```1:4:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
using System.Runtime.InteropServices;
using MetaQuotes.MT5CommonAPI;
using MetaQuotes.MT5ManagerAPI;
using TraderIntelligence.Application.Contracts;
```

Connect is Windows-gated before factory init. The exception text states the x64 Manager contract:

```61:80:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory
                         ?? Path.Combine(AppContext.BaseDirectory);
            var init = SMTManagerAPIFactory.Initialize(dllDir);
            if (init != MTRetCode.MT_RET_OK)
            {
                LastError = $"Factory init failed: {init}";
                throw new InvalidOperationException(LastError);
            }

            uint version = 0;
            SMTManagerAPIFactory.GetVersion(out version);
            var created = SMTManagerAPIFactory.CreateManager(version, out var createRes);
            if (createRes != MTRetCode.MT_RET_OK || created is null)
            {
                LastError = $"CreateManager failed: {createRes}";
                throw new InvalidOperationException(LastError);
            }
```

Owning library TFM/arch (the hypothesized gap is **absent** here):

```25:30:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>
```

Vendor mixed-mode + native trio is referenced and copied next to output (`MetaQuotes.MT5CommonAPI64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MT5APIManager64.dll`):

```6:22:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
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
```

Infrastructure (the other `net8.0-windows` x64 assembly that constructs this type) matches the same TFM/arch:

```20:25:D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
```

R021 already measured the load claim this angle restates. Isolated `net8.0` **and** `net8.0-windows` x64 processes can reference and load `MetaQuotes.MT5ManagerAPI64.dll` on Windows x64 when `MT5APIManager64.dll` is beside the process. The vendor image is Framework 4.7.2 mixed-mode AMD64, not a net8 IL-only assembly — but **lack of `-windows` is not what blocks Load**. Quote from `D:/Prop/reports/swarm/20260818/R021_dll_load.md`:

> Must the project be `net8.0-windows`? **No for the DLL itself.** Isolated `net8.0` compiled and loaded it. `net8.0-windows` is still the honest TFM for a process that will **run** this Windows mixed-mode image.

This file’s project **is** that honest TFM (`net8.0-windows` + `PlatformTarget=x64`). The angle’s causal sentence (“API not net8.0-windows x64 **so** MetaQuotes DLL cannot load”) is **false for the assigned file**.

This file does not contain:

- `TargetFramework` / `net8.0` without `-windows` (csproj is `net8.0-windows`)
- `PlatformTarget` AnyCPU/x86 (csproj is `x64`)
- `[DllImport]` / manual `LoadLibrary` of a 32-bit PE
- a Linux-portable TFM on the Manager wrapper

### Out of this slice (host graph — not a TFM defect in this file)

`apps/api` and `apps/mt5-worker` are still portable `net8.0` with **no** `PlatformTarget` / `RuntimeIdentifier`:

```15:19:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

```3:8:D:/Prop/apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>dotnet-TraderIntelligence.Mt5Worker-6850a13e-19ab-4410-9156-a0d5b0d746d1</UserSecretsId>
  </PropertyGroup>
```

R021 measured `NU1201` when those `net8.0` hosts `ProjectReference` `net8.0-windows` Infrastructure/Mt5. That is a **host TFM mismatch**, not “this connector’s API is not `net8.0-windows` x64.” A54 also says Linux `apps/api` must never load `TraderIntelligence.Mt5`; `Program.cs` still calls `AddTraderIntelligence`, which constructs `NativeMt5BrokerConnector`. That wiring/graph issue is owned by `apps/api` + `Infrastructure/DependencyInjection.cs` + `LiveMt5Registration.cs`, not by a missing Windows TFM on this file.

## No-loss implication

`NativeMt5BrokerConnector` implements `IMt5BrokerConnector` / `IMt5BulkDealReader` only: `Connect` / `Disconnect` / `GetGroups` / `GetAccounts` / `GetDeals` / `GetGroupDeals` / `GetPositions`. There is no `OrderSend`, no deal write, no balance/credit, no FIX `NewOrderSingle`. Pump flags are `PUMP_MODE_GROUPS | PUMP_MODE_USERS | PUMP_MODE_POSITIONS` — data pump, not trade send.

If the MetaQuotes image cannot load, `ConnectCore` fails closed (`PlatformNotSupportedException` or `InvalidOperationException` on `Initialize`/`CreateManager`/`Connect`) and `_connected` stays false. `Ensure()` then refuses reads. A failed DLL load cannot reduce broker equity. Slot 25 therefore has **no capital-loss path** in the assigned file; the hypothesized “wrong TFM ⇒ DLL cannot load” defect is **not present** on this type (owning csproj is already `net8.0-windows` x64).

Empty-PASS justification: the assigned file was fully read (330/330 lines) plus owning csproj; the angle is contradicted by measured TFM/arch, not skipped.
