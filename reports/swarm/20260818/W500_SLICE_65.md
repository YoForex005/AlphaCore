# W500_SLICE_65

- **slot:** 65
- **file:** `D:/Prop/src/Domain/Risk/RiskEngine.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (190 lines) via `read_file`; Domain / API / Mt5 / Infrastructure csproj TFMs via `read_file`; grep on assigned file for `MetaQuotes|DllImport|LoadLibrary|SMTManager|MT5API|PInvoke` → **0 hits**; grep `Evaluate(` under `D:/Prop/src` → **definition only**; API restore cache + `Program.cs` + `DependencyInjection.cs`
- **verdict:** PASS

## Evidence quotes

`RiskEngine.cs` is a pure managed Domain gate. It has **no** `DllImport`, **no** `NativeLibrary`, **no** `using MetaQuotes.*`, **no** path to `MT5APIManager64.dll` / `MetaQuotes.MT5ManagerAPI64.dll`, and **no** MSBuild TFM. The class only evaluates a `RiskEvaluationRequest` and returns a `RiskDecision`.

```67:93:D:/Prop/src/Domain/Risk/RiskEngine.cs
public sealed class RiskEngine
{
    private readonly RiskLimits _limits;

    public RiskEngine(RiskLimits? limits = null)
    {
        _limits = limits ?? new RiskLimits();
    }

    public RiskDecision Evaluate(RiskEvaluationRequest request)
    {
        if (request.KillSwitch == KillSwitchMode.StopNewExecution && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "STOP_NEW_EXECUTION");

        if (request.KillSwitch == KillSwitchMode.EmergencyFlatten && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.GlobalStop, "EMERGENCY_FLATTEN_BLOCKS_NEW");

        if (!request.Reconciled && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.Reject, "VENUE_NOT_RECONCILED");

        if (!request.VenueHealthy && IsIncreasing(request.Action))
            return Reject(request, RiskDecisionOutcome.PauseVenue, "VENUE_UNHEALTHY");

        if (request.RealExecutionEnabled == false && request.Action != CopyIntentAction.CloseExposure)
        {
            // Shadow path still evaluates risk but never allows FIX send.
        }
```

FIX send is a boolean computed from request flags. Every reject hard-sets `AllowFixSend = false` and `ApprovedQuantity = 0`. Approve still requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`.

```147:188:D:/Prop/src/Domain/Risk/RiskEngine.cs
        var allowSend = request.RealExecutionEnabled
                        && request.KillSwitch == KillSwitchMode.None
                        && request.Reconciled
                        && request.VenueHealthy;

        if (IsReducing(request.Action))
        {
            return new RiskDecision
            {
                CopyIntentId = request.CopyIntentId,
                Outcome = RiskDecisionOutcome.Approve,
                ApprovedQuantity = request.RequestedQuantity,
                Reason = "RISK_REDUCTION",
                AllowFixSend = allowSend
            };
        }

        return new RiskDecision
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = RiskDecisionOutcome.Approve,
            ApprovedQuantity = request.RequestedQuantity,
            Reason = "APPROVED",
            AllowFixSend = allowSend
        };
    }
    // ...
    private static RiskDecision Reject(RiskEvaluationRequest request, RiskDecisionOutcome outcome, string reason) =>
        new()
        {
            CopyIntentId = request.CopyIntentId,
            Outcome = outcome,
            ApprovedQuantity = 0,
            Reason = reason,
            AllowFixSend = false
        };
```

Owning project TFM is portable `net8.0` (correct for Domain; this file must not take a Windows RID or pin x64):

```1:9:D:/Prop/src/Domain/TraderIntelligence.Domain.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

### Angle’s API-host premise is **stale / false** (adjacent, not this file)

W500_SLICE_15 (same file, same angle) quoted API as portable `net8.0` with NU1201 against `net8.0-windows` Infrastructure. **Measured now:** the API host **is** `net8.0-windows` + x64 and restore **succeeds**.

```17:22:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

```1:5:D:/Prop/apps/api/obj/project.nuget.cache
{
  "version": 2,
  "dgSpecHash": "O1qLDN+S43s=",
  "success": true,
  "projectFilePath": "D:\\Prop\\apps\\api\\TraderIntelligence.Api.csproj",
```

API `logs` in that cache is `[]` — **no NU1201**. The host also `ProjectReference`s `TraderIntelligence.Mt5` directly. That project is the only compile-time bind of the vendor trio:

```6:29:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <!-- CopyToOutputDirectory: MT5APIManager64.dll + both MetaQuotes.*64.dll -->
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Runtime load is `NativeMt5BrokerConnector.ConnectCore` (`SMTManagerAPIFactory.Initialize`), **not** `RiskEngine.cs`. That connector throws `PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.")` off-Windows. `AddTraderIntelligence` constructs those connectors after a real-password gate and **does not** register `RiskEngine`.

```35:56:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");
        // ...
        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);
        // ...
        services.AddScoped<DealIngestionService>();
        services.AddScoped<ReconstructionScoringService>();
        services.AddHostedService<LiveIngestHostedService>();
        services.AddHostedService<CTraderFixLogonHostedService>();
        return services;
```

Grep `new RiskEngine` / `IRiskEngine` / `AddSingleton<RiskEngine` / `AddScoped<RiskEngine` under `D:/Prop/src` → **0 hits**. Grep `Evaluate(` under product `src/**/*.cs` → **only** `RiskEngine.Evaluate` (this file). API `/api/settings` exposes hardcoded quote/signal ages and `REAL_COPY_EXECUTION_ENABLED` from `LiveRuntimeStatus`; it does not construct this type.

Prior measured pin `D:\Prop\reports\swarm\20260818\R021_dll_load.md`: an isolated **`net8.0` AnyCPU** Windows x64 process **can** `<Reference>` and load `MetaQuotes.MT5ManagerAPI64.dll` and `Initialize` when `MT5APIManager64.dll` is beside the exe. **Must the project be `net8.0-windows`? No for the DLL itself.** Causal sentence in the angle (“API not net8.0-windows x64 **so** MetaQuotes DLL cannot load”) is therefore **false twice**:

1. Missing `-windows` is not what blocks `LoadLibrary` of the vendor PE (R021).
2. The API host **is** now `net8.0-windows` + `PlatformTarget=x64` and restore is green.

`bin/Debug/net8.0-windows/` exists but is empty (no rebuild after the TFM flip). Stale `bin/Debug/net8.0/` still holds an older `TraderIntelligence.Api.exe` / `TraderIntelligence.Mt5.dll` without MetaQuotes names in that tree. That is **output freshness**, not a `RiskEngine` load path.

This file does not contain:

- `TargetFramework` / `PlatformTarget` / RID
- `DllImport` / `LoadLibrary` / `SMTManagerAPIFactory`
- MetaQuotes assembly names
- any I/O besides in-memory arithmetic on the request
- any product caller of `Evaluate`

## No-loss implication

`RiskEngine.Evaluate` cannot load a MetaQuotes PE and cannot emit FIX `NewOrderSingle`. A failed or successful Manager DLL load in the API process is **ingest liveness** (`NativeMt5BrokerConnector.ConnectCore`), not this type. All reject paths set `AllowFixSend = false` and `ApprovedQuantity = 0`. Approve still keeps `AllowFixSend` false unless the **caller** already set `RealExecutionEnabled` and the venue is healthy + reconciled + kill-switch `None`. A missing Manager session would present as missing quote / unreconciled / unhealthy venue — all blocked here (`QUOTE_MISSING`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`). Slot 65 therefore has **no MetaQuotes-load capital-loss path** in the assigned file.

The hypothesized defect (API not `net8.0-windows` x64 ⇒ vendor DLL cannot load) is **absent from this file** and **not true of the current API csproj**. Empty-PASS justification: the assigned file was fully read (all 190 lines); the angle is not a property of `RiskEngine` and is overstated as a load rule (R021) plus stale vs today’s API TFM. Not skipped review.
