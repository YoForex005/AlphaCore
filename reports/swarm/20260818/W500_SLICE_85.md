# W500_SLICE_85 — EfDashboardQueries vs API TFM / MetaQuotes load

| Field | Value |
|---|---|
| Slot | 85 |
| File | `D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs` |
| Angle | API not `net8.0-windows` x64 so MetaQuotes DLL cannot load |
| Date | 2026-08-18 |
| Method | Full `read_file` of the assigned file (215 lines) + `grep` (no MetaQuotes hits) + host/library csproj + restore `dgspec` + R021/A002/A012 TFM notes |
| Product source modified | **No** |
| Verdict | **PASS** (empty PASS — hypothesized TFM defect is not present; assigned file never loads Manager64) |

---

## 1. Assigned file (fully read)

`EfDashboardQueries` is an EF Core dashboard adapter. It implements `IDashboardQueries` with `TraderDbContext` + `LiveRuntimeStatus` only.

```1:18:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
using Microsoft.EntityFrameworkCore;
using TraderIntelligence.Application.Dashboard;
using TraderIntelligence.Application.Runtime;
using TraderIntelligence.Domain.Enums;
using TraderIntelligence.Infrastructure.Persistence;

namespace TraderIntelligence.Infrastructure.Dashboard;

public sealed class EfDashboardQueries : IDashboardQueries
{
    private readonly TraderDbContext _db;
    private readonly LiveRuntimeStatus _runtime;

    public EfDashboardQueries(TraderDbContext db, LiveRuntimeStatus runtime)
    {
        _db = db;
        _runtime = runtime;
    }
```

`grep` on this file for `MetaQuotes|DllImport|SMTManager|NativeMt5|MT5API` → **no matches**.

The type:

- Counts brokers/accounts/scores from EF (`GetOverviewAsync`, `GetBrokersAsync`, `GetGroupsAsync`, `GetTradersAsync`).
- Reads FIX *state rows* already stored in `FixSessionStates` / `DestinationQuotes` (`GetFixSessionsAsync`).
- Surfaces kill-switch + last 20 reject reasons (`GetRiskAsync`).
- Masks manager logins for display (`MaskLogin`: `login / 100 * 100`).

It never `Initialize`s the Manager factory, never `LoadLibrary`s, never constructs `NativeMt5BrokerConnector`. Live flags are *already-computed* runtime bits:

```45:50:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
            _runtime.Brokers.Values.Count(b => b.Connected) > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution
                || _runtime.Quote.LoggedOn,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution
                || _runtime.Trade.LoggedOn,
            _runtime.RealCopyEnabled);
```

Risk DTO zeros are display placeholders, not order sends:

```206:206:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
```

DI wires this class as the dashboard port only:

```49:49:D:/Prop/src/Infrastructure/DependencyInjection.cs
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
```

API endpoints `/api/overview`, `/api/brokers`, `/api/groups`, `/api/traders`, `/api/fix/sessions`, `/api/risk` call this type. Native Manager load (if any) is `LiveMt5Registration.CreateConnectors` → `NativeMt5BrokerConnector.ConnectCore` → `SMTManagerAPIFactory.Initialize` — **outside this file**.

---

## 2. Angle check: is the API host still portable `net8.0` (no x64)?

**No. Source of record is already `net8.0-windows` + `PlatformTarget` x64.**

Earlier swarm notes (`A002_api_dummy_path.md` §2, `A012_worker_tfm.md` §0) quoted the API as `<TargetFramework>net8.0</TargetFramework>` with **no** `PlatformTarget`. That quote is **stale**. Current `apps/api/TraderIntelligence.Api.csproj`:

```17:22:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

Restore graph agrees (`originalTargetFrameworks` / alias):

```19:27:D:/Prop/apps/api/obj/TraderIntelligence.Api.csproj.nuget.dgspec.json
        "originalTargetFrameworks": [
          "net8.0-windows"
        ],
        ...
          "net8.0-windows7.0": {
            "targetAlias": "net8.0-windows",
```

`obj/Debug/net8.0-windows/TraderIntelligence.Api.GeneratedMSBuildEditorConfig.editorconfig` line 2: `build_property.TargetFramework = net8.0-windows`.

The library that *contains* the assigned file is the same TFM/bitness:

```21:26:D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
```

The only project that actually references the vendor mixed-mode wrappers is already Windows x64 and copies the CMake trio:

```6:29:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
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
    ...
    <PlatformTarget>x64</PlatformTarget>
```

API `ProjectReference`s include both `Infrastructure` and `Mt5`. Host TFM and Manager library TFM now match (`net8.0-windows7.0` restore alias). The A012 NU1201 case (`net8.0` host → `net8.0-windows` library) **does not apply to the API csproj of record**.

---

## 3. Causal claim is also false even if TFM were still `net8.0`

R021 measured on this machine: a **Windows AMD64** .NET 8 process can reference, JIT, and `SMTManagerAPIFactory.Initialize` / `CreateManager` from **either** `net8.0` **or** `net8.0-windows` when `MT5APIManager64.dll` sits beside the exe (`D:\Prop\reports\swarm\20260818\R021_dll_load.md`).

What actually blocks Manager64:

| Blocker | Role |
|---|---|
| Non-Windows / Linux container | PE32+ AMD64 + `KERNEL32` / `mscoree` |
| x86 / ARM64 process | Wrapper is `0x8664` |
| Missing `MT5APIManager64.dll` next to process | Wrapper still loads; `Initialize` → `MT_RET_ERR_NOTFOUND` |
| Missing `MetaQuotes.MT5CommonAPI64.dll` | Hard assembly ref |

`net8.0` vs `net8.0-windows` is **not** the PE load gate. The angle “API not `net8.0-windows` x64 **so** MetaQuotes cannot load” is the wrong cause. Current API is `net8.0-windows` x64 anyway.

`NativeMt5BrokerConnector` still refuses non-Windows at connect (not in the assigned file):

```66:67:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");
```

---

## 4. Residual notes (out of this angle; not a FAIL)

- Stale `apps/api/bin/Debug/net8.0/TraderIntelligence.Api.runtimeconfig.json` still has `"tfm": "net8.0"`. That is leftover output from the pre-retarget build. **Csproj + dgspec of record are `net8.0-windows`.**
- `apps/mt5-worker` and `apps/fix-worker` remain `<TargetFramework>net8.0</TargetFramework>` with no `PlatformTarget`. That is a **different host**, not this file / not the API csproj.
- API does not set `RuntimeIdentifier=win-x64`. `PlatformTarget=x64` is set; on this Windows x64 box that is enough for an x64 process. Missing RID is not “not `net8.0-windows` x64.”
- `EfDashboardQueries` cannot fix or cause DLL copy. Copy contract lives on `TraderIntelligence.Mt5.csproj` `<None CopyToOutputDirectory="PreserveNewest">`.

Empty-PASS justification: assigned file was read in full; it has no MetaQuotes load path; API + Infrastructure source of record already match the required Windows x64 TFM.

---

## 5. No-loss implication

This class is **read-only dashboard**. It does not connect Manager, does not send FIX `NewOrderSingle`, does not flip `REAL_COPY_EXECUTION_ENABLED`, does not size or close positions. If Manager64 failed to load in the API process, ingest/connect would throw fail-closed in `NativeMt5BrokerConnector` / DI; this type would only show disconnected/false live flags and the hardcoded risk zeros. Those zeros are **not** live fills and **cannot** open, close, or scale capital. Slot 85’s hypothesized TFM defect is absent, so it does not create a silent-loss path through `EfDashboardQueries`.
