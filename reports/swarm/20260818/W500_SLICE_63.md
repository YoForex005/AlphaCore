# W500_SLICE_63

- **slot:** 63
- **file:** `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj`
- **angle:** live cTrader NewOrderSingle or capital-loss path
- **read:** full file (31 lines) via `read_file`; grep on `D:/Prop/src/Mt5` for `NewOrderSingle|cTrader|capital.?loss|OrderSend|PlaceOrder` returned **no** matches under `Mt5/` (zero hits in the assigned `.csproj`; no cTrader / FIX send tokens in that project tree)
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

What it *does* contain is a **compile-time** bind to official MetaQuotes Manager API 64-bit assemblies (`MetaQuotes.MT5CommonAPI64`, `MetaQuotes.MT5ManagerAPI64`, native `MT5APIManager64.dll`) plus `net8.0-windows` / `x64`. That is a build graph for the MT5 **read/manager** project, not a live cTrader execution path.

Repo-wide `NewOrderSingle` hits exist only outside this file (context, not this slice):

- `Fix.CTrader/Configuration/CTraderFixOptions.cs`: `When true, allow placing new orders (NewOrderSingle). Default OFF.` (`RealCopyExecutionEnabled` defaults false)
- `Application/Runtime/LiveRuntimeStatus.cs`: `"NewOrderSingle disabled. SHADOW/CopyIntent only. No capital at risk from this process."`
- `Fix.CTrader/Hosting/CTraderFixLogonHostedService.cs`: `"FIX QUOTE logon={Q} TRADE logon={T} (NewOrderSingle still disabled)."`

Those live in other projects. Slot 63’s assigned file cannot enable or emit them.

## No-loss implication

A `.csproj` cannot open a TRADE session, cannot emit FIX `NewOrderSingle`, and cannot reduce account equity. Worst case of this file is copying vendor MT5 Manager DLLs next to the output and compiling x64 Windows manager-client code. Slot 63 therefore has **no live cTrader NewOrderSingle path** and **no capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (all 31 lines); the angle (live cTrader NewOrderSingle / capital-loss) is absent by construction, not by skipped review.
