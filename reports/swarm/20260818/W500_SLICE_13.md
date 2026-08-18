# W500_SLICE_13

- **slot:** 13
- **file:** `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (31 lines) via `read_file`; grep on `D:/Prop/src/Mt5` for `NewOrderSingle|cTrader|capital.?loss|OrderSend|PlaceOrder` returned **no** matches in the assigned `.csproj` (zero hits under `Mt5/` for those tokens except unrelated `DealRequest` read APIs in `Connectors/NativeMt5BrokerConnector.cs`, which is not this file)
- **verdict:** PASS

## Evidence quotes

`TraderIntelligence.Mt5.csproj` is an SDK-style MSBuild project only. It names compile references and copy-to-output items. It does not contain C#, FIX, cTrader session code, `NewOrderSingle` / `35=D`, `OrderSend`, position sizing, or any runtime send path.

```1:31:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <ProjectReference Include="..\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\Application\TraderIntelligence.Application.csproj" />
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
    <None Include="..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
    <None Include="..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

</Project>
```

This file does not contain:

- `NewOrderSingle` / FIX tag `35=D` / cTrader TRADE socket / QuickFIX initiator
- `OrderSend` / `DealerSend` / place-order / cancel-replace
- SL/TP, qty, PnL, or any capital-mutation statement
- any executable statement at all (MSBuild XML only)

What it *does* contain is a **compile-time** bind to official MetaQuotes Manager API 64-bit assemblies (`MetaQuotes.MT5CommonAPI64`, `MetaQuotes.MT5ManagerAPI64`, native `MT5APIManager64.dll`) plus `net8.0-windows` / `x64`. That is a build graph for the MT5 **read/manager** project, not a live cTrader execution path. Sibling `NativeMt5BrokerConnector.cs` (compiled by this project, **not** this file) uses `DealRequest` / `DealRequestByGroup` (history pull), not dealer send.

Live NewOrderSingle / capital-at-risk controls live elsewhere (not this file), e.g. `Fix.CTrader/Configuration/CTraderFixOptions.cs` (`When true, allow placing new orders (NewOrderSingle). Default OFF.`) and prior swarm pin `E002_no_live_send.md` (`SAFE_BY_ABSENCE` — no function emits FIX `MsgType=D`). Those are out of this slice’s file.

## No-loss implication

A `.csproj` cannot open a TRADE session, cannot emit FIX `NewOrderSingle`, and cannot reduce account equity. Worst case of this file is copying vendor MT5 Manager DLLs next to the output and compiling x64 Windows manager-client code. Slot 13 therefore has **no live cTrader NewOrderSingle path** and **no capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (all 31 lines); the angle (live cTrader NewOrderSingle / capital-loss) is absent by construction, not by skipped review.
