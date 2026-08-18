# W500_SLICE_55

- **slot:** 55
- **file:** `D:/Prop/src/Application/Ingestion/DealIngestionService.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (145 lines) via `read_file`; grep on this file for `MetaQuotes|DllImport|LoadLibrary|SMTManager|PlatformTarget|net8|windows|x64` → **0 hits**; grep for `NewOrderSingle|PlaceOrder|DealerSend|SendOrder` → **0 hits**. Supporting reads: `TraderIntelligence.Application.csproj`, `apps/api/TraderIntelligence.Api.csproj`, `apps/api/Program.cs`, `apps/api/obj/project.nuget.cache`, `apps/api/obj/TraderIntelligence.Api.csproj.nuget.dgspec.json`, `apps/api/obj/Debug/net8.0-windows/TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig`, `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.runtimeconfig.json`, `Infrastructure/DependencyInjection.cs`, `Infrastructure/Mt5Live/LiveMt5Registration.cs`, `Infrastructure/Hosting/LiveIngestHostedService.cs`, `Mt5/TraderIntelligence.Mt5.csproj`, `Mt5/Connectors/NativeMt5BrokerConnector.cs`, `Mt5/Connectors/BrokerRegistry.cs`. Prior pin `W500_SLICE_5.md` (same file+angle, stale `net8.0` API + NU1201) and `R021_dll_load.md`.
- **verdict:** PASS

## Evidence quotes

`DealIngestionService` is TFM-neutral Application orchestration. It does not reference MetaQuotes, does not `DllImport` / `LoadLibrary`, and does not set `TargetFramework` or `PlatformTarget`. Grep on the assigned file for those tokens is empty.

It **does** resolve a connector and call `ConnectAsync` on both catalog and broker sync. That is the only native-load trigger reachable from this type:

```37:50:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<BrokerSyncResult> SyncCatalogAsync(string brokerCode, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
        var now = DateTimeOffset.UtcNow;

        var groups = await connector.GetGroupsAsync(ct);
        await _store.UpsertGroupsBatchAsync(brokerId, groups, now, ct);

        var accounts = await connector.GetAccountsAsync(null, ct);
        await _store.UpsertAccountsBatchAsync(brokerId, accounts, now, ct);
```

```53:57:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
```

The rest of `SyncBrokerAsync` is read + persist only (`GetGroupsAsync` / `GetAccountsAsync` / `GetDealsAsync` or `IMt5BulkDealReader.GetGroupDealsAsync` / positions → store upserts). No order send.

The type lives in Application, which is a portable class library (`net8.0`, no x64). That is correct for a library that never binds the vendor PE:

```11:15:D:/Prop/src/Application/TraderIntelligence.Application.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
```

The **host** named by the angle is `apps/api`. On current disk it **is** `net8.0-windows` + `PlatformTarget` x64, and it `ProjectReference`s `TraderIntelligence.Mt5` (the project that copies the Manager trio):

```1:22:D:/Prop/apps/api/TraderIntelligence.Api.csproj
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
  </ItemGroup>
  ...
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Restore of that host now **succeeds** (this is the opposite of `W500_SLICE_5`, which recorded `success: false` / NU1201 against `net8.0`):

```1:5:D:/Prop/apps/api/obj/project.nuget.cache
{
  "version": 2,
  "dgSpecHash": "O1qLDN+S43s=",
  "success": true,
  "projectFilePath": "D:\\Prop\\apps\\api\\TraderIntelligence.Api.csproj",
```

```19:21:D:/Prop/apps/api/obj/TraderIntelligence.Api.csproj.nuget.dgspec.json
        "originalTargetFrameworks": [
          "net8.0-windows"
        ],
```

Design-time editorconfig matches:

```1:3:D:/Prop/apps/api/obj/Debug/net8.0-windows/TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig
is_global = true
build_property.TargetFramework = net8.0-windows
build_property.TargetPlatformMinVersion = 7.0
```

`Mt5` (what the API now references) is already the Windows x64 Manager bind + copy-dlls:

```6:30:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    ...
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
```

API `Program.cs` still injects this service as the live resync door (`AddTraderIntelligence` + `MapPost("/api/ops/resync", … DealIngestionService …)`). DI still constructs only `NativeMt5BrokerConnector` after the real-password gate. Native connect (not in the assigned file) is:

```66:70:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

**Honesty on the angle:** the causal sentence is “API is not `net8.0-windows` x64 **so** MetaQuotes cannot load.”

1. Premise is **false** on current disk. API csproj + dgspec + restore + editorconfig are `net8.0-windows` / x64.
2. Isolated `net8.0` on Windows x64 **can** load `MetaQuotes.MT5ManagerAPI64.dll` when `MT5APIManager64.dll` sits beside the process (`R021_dll_load.md`). Missing `-windows` was never the PE-load blocker; the old product break was **NU1201** (`net8.0` host cannot consume `net8.0-windows` Infrastructure/Mt5). That NU1201 is gone (`success: true`).
3. Stale `D:/Prop/apps/api/bin/Debug/net8.0/` still exists (`tfm: net8.0`, no MetaQuotes trio in the listing). `bin/Debug/net8.0-windows/` exists but is empty (restore/design-time only; no compiled API exe yet). Those are **output-freshness** facts, not “API TFM is still portable net8.0.” This slot does **not** claim a measured in-process `Initialize` of the API exe.

`apps/mt5-worker` remains portable `net8.0` with no `PlatformTarget`. That is **not** the assigned file or the assigned “API” host.

## No-loss implication

`DealIngestionService` cannot place, cancel, or resize destination orders. It only `ConnectAsync` + pull groups/accounts/deals/positions + upsert. There is no FIX `NewOrderSingle`, no dealer send, no cTrader TRADE path, and no volume/price mutation toward a live book.

The assigned defect (API not `net8.0-windows` x64 ⇒ Manager64 cannot load in the process that hosts this service) is **not present**: the API host is declared `net8.0-windows` x64 and restore of the Mt5/Infrastructure graph succeeds. Worst case if `ConnectAsync` later fails (empty windows output, missing native trio beside a stale `net8.0` exe): throw / empty ingest, still no send. Slot 55 therefore has **no live capital-loss path** in the assigned file.

This PASS is not a measured `SMTManagerAPIFactory.Initialize` of `TraderIntelligence.Api.exe`, not a PASS on `mt5-worker` TFM, and not a claim that `bin/Debug/net8.0-windows` already contains the vendor DLLs.
