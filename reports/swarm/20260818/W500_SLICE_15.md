# W500_SLICE_15

- **slot:** 15
- **file:** `D:/Prop/src/Domain/Risk/RiskEngine.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (190 lines) via `read_file`; Domain + API + Mt5 csproj TFMs via `read_file`; grep for `TargetFramework|PlatformTarget|MetaQuotes|DllImport`; `RiskEngine` usages under `D:\Prop\src` and `D:\Prop\tests`
- **verdict:** PASS

## Evidence quotes

`RiskEngine.cs` is a pure managed Domain gate. It has **no** `DllImport`, **no** `NativeLibrary`, **no** `using MetaQuotes.*`, **no** path to `MT5APIManager64.dll`, and **no** MSBuild TFM. The class only evaluates a `RiskEvaluationRequest` and returns a `RiskDecision`.

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

FIX send is a boolean computed from request flags. Rejects hard-set `AllowFixSend = false`. Approve still requires `RealExecutionEnabled && KillSwitch.None && Reconciled && VenueHealthy`.

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

Owning project TFM is portable `net8.0` (correct for Domain; this file must not take a Windows RID):

```1:9:D:/Prop/src/Domain/TraderIntelligence.Domain.csproj
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>
```

The angle’s **API host** facts (adjacent, not this file):

```15:19:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

No `net8.0-windows`, no `<PlatformTarget>x64</PlatformTarget>`. Stale `D:\Prop\apps\api\bin\Debug\net8.0\TraderIntelligence.Api.runtimeconfig.json` records `"tfm": "net8.0"` / `Microsoft.NETCore.App` only. That folder contains `TraderIntelligence.Mt5.dll` but **zero** `MetaQuotes.MT5*64.dll` / `MT5APIManager64.dll` (confirmed via `list_dir` + `deps.json` grep: no `MetaQuotes` entries).

Manager wrappers live only on the Mt5 project:

```6:29:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <!-- ... CopyToOutputDirectory trio ... -->
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Runtime load is `NativeMt5BrokerConnector.ConnectCore` (`SMTManagerAPIFactory.Initialize`), which is **not** `RiskEngine.cs`. That connector throws `PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.")` off-Windows.

Prior measured pin `D:\Prop\reports\swarm\20260818\R021_dll_load.md`: an isolated **`net8.0` AnyCPU** Windows x64 process **can** `<Reference>` and load `MetaQuotes.MT5ManagerAPI64.dll` and `Initialize` when `MT5APIManager64.dll` is beside the exe. The true host-graph issue is **NU1201** (`net8.0` exe cannot `ProjectReference` a `net8.0-windows` library), **not** “missing `-windows` TFM makes the vendor PE unloadable.” Causal claim in the angle is therefore **false**.

`RiskEngine` is **not** registered in `Infrastructure/DependencyInjection.AddTraderIntelligence`. Grep under `src/` finds the type only in this file (plus `AllowFixSend` on `RiskDecisionRecord`). Unit tests construct it directly; API `Program.cs` never new’s it. API `/api/settings` hardcodes `REAL_COPY_EXECUTION_ENABLED=false`.

This file does not contain:

- `TargetFramework` / `PlatformTarget` / RID
- `DllImport` / `LoadLibrary` / `SMTManagerAPIFactory`
- MetaQuotes assembly names
- any I/O besides in-memory arithmetic on the request

## No-loss implication

`RiskEngine.Evaluate` cannot load a MetaQuotes PE and cannot emit FIX. If the API host fails to restore (NU1201) or fails factory init (`MT_RET_ERR_NOTFOUND` without native DLL), that is **liveness** of Manager ingest — not a silent order. All reject paths set `AllowFixSend = false` and `ApprovedQuantity = 0`. Approve still keeps `AllowFixSend` false unless the caller already set `RealExecutionEnabled` and the venue is healthy + reconciled. A missing Manager session would present as missing quote / unreconciled / unhealthy venue — all blocked here (`QUOTE_MISSING`, `VENUE_NOT_RECONCILED`, `VENUE_UNHEALTHY`). Slot 15 therefore has **no MetaQuotes-load capital-loss path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (all 190 lines); the angle (API TFM blocks MetaQuotes load) is **absent from this file** and **overstated as a load rule** (R021). Not skipped review.
