# W500_SLICE_12

- **slot:** 12
- **file:** `D:/Prop/apps/api/TraderIntelligence.Api.csproj`
- **angle:** failure to fetch ALL manager traders/logins
- **read:** full file (20/20 lines) via `read_file`; grep on this file for manager/login fetch (none); restore assets `obj/project.assets.json` NU1201; downstream TFM + `GetAccountsCore` / `UserRequestArray` / `UserLogins` on the Infrastructure → Mt5 path this host cannot bind
- **verdict:** FAIL

## Evidence quotes

Assigned host project is a plain `Microsoft.NET.Sdk.Web` `net8.0` app. It references Domain, Application, and Infrastructure only. There is no `net8.0-windows` TFM, no `PlatformTarget` x64, no `RuntimeIdentifier` `win-x64`, no `ProjectReference` to `TraderIntelligence.Mt5`, and no `None`/`Content` items that copy `MT5APIManager64.dll` / `MetaQuotes.MT5ManagerAPI64.dll` into the API output.

```1:20:D:/Prop/apps/api/TraderIntelligence.Api.csproj
<Project Sdk="Microsoft.NET.Sdk.Web">

  <ItemGroup>
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.SignalR.Common" Version="8.0.4" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.2" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

The only live path that can enumerate manager users is Infrastructure → Mt5 (`NativeMt5BrokerConnector`). Both of those projects are Windows x64 Manager-API hosts, not `net8.0`:

```21:25:D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
```

```25:29:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <PlatformTarget>x64</PlatformTarget>
```

NuGet restore of this exact csproj already records a hard incompatibility. The Infrastructure reference (and therefore the native Manager connector) is not a restoreable asset for `net8.0`:

```2022:2031:D:/Prop/apps/api/obj/project.assets.json
  "logs": [
    {
      "code": "NU1201",
      "level": "Error",
      "message": "Project TraderIntelligence.Infrastructure is not compatible with net8.0 (.NETCoreApp,Version=v8.0). Project TraderIntelligence.Infrastructure supports: net8.0-windows7.0 (.NETCoreApp,Version=v8.0)",
      "libraryId": "TraderIntelligence.Infrastructure",
      "targetGraphs": [
        "net8.0"
      ]
    }
  ]
```

That restore error is caused by this file’s `<TargetFramework>net8.0</TargetFramework>` plus the Infrastructure `ProjectReference`. It is not a missing-angle / empty-file case.

What the host is supposed to run (and cannot bind) is the Manager request path that walks **all** groups then **all** logins:

```17:19:D:/Prop/src/Infrastructure/Mt5Live/LiveMt5Registration.cs
    public static IReadOnlyList<IMt5BrokerConnector> CreateConnectors(IConfiguration config)
    {
        var dllDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory));
```

```223:232:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            var req = _manager.UserRequestArray(gname, users);
            if (req != MTRetCode.MT_RET_OK && req != MTRetCode.MT_RET_OK_NONE && req != MTRetCode.MT_RET_ERR_NOTFOUND)
                _manager.UserGetByGroup(gname, users);

            if (users.Total() == 0)
            {
                var loginRes = MTRetCode.MT_RET_OK;
                var logins = _manager.UserLogins(gname, out loginRes);
                if (loginRes == MTRetCode.MT_RET_OK && logins is { Length: > 0 })
                    _manager.UserRequestByLogins(logins, users);
```

`LiveMt5Registration` sets `NativeDllDirectory` to the API process `AppContext.BaseDirectory`. This csproj never copies the native Manager DLLs there, and never retargets the process to `net8.0-windows` / x64 so those DLLs can load.

`Program.cs` (same app, hosted by this csproj) still maps `/api/traders` to `GetTradersAsync` (scored rows only) and `/api/ops/resync` only rebuilds four hardcoded logins (`10001`, `10002`, `10003`, `99001`). Those are downstream symptoms; the **csproj-level** defect is that the host cannot restore/load the Manager stack at all.

This file does not contain:

- `net8.0-windows` / `PlatformTarget` / `RuntimeIdentifier`
- any `TraderIntelligence.Mt5` reference
- any `MT5APIManager64` / `MetaQuotes.MT5*` copy items
- any login-list / `UserRequest*` / `GetAccountsAsync` wiring (expected in a csproj; the failure is that it blocks the project that does)

## No-loss implication

This host is the process that would call `DealIngestionService.SyncBrokerAsync` → `GetAccountsAsync(null)` → Manager `UserRequestArray` / `UserLogins` for the full login universe. Because `TraderIntelligence.Api.csproj` is `net8.0` and restore already fails NU1201 against `net8.0-windows` Infrastructure, **the API cannot fetch any live manager traders/logins, therefore it cannot fetch ALL of them**.

No-loss consequence: `/api/traders`, scoring, deals, and `mt5_positions_current` cannot be treated as the complete source book. A partial or empty login universe hides source risk (martingale / open XAU / margin) and can let copy/risk decisions run on an incomplete set. Do not enable real copy or assume dashboard counts are the full Manager user list until this csproj is retargeted to `net8.0-windows` + x64 and deploys the Manager native DLLs next to the API.

This is not an empty-PASS. The assigned file was fully read; the angle is present as a host-packaging / TFM defect that blocks ALL-login fetch.
