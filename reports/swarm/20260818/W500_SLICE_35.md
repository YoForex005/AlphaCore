# W500_SLICE_35

- **slot:** 35
- **file:** `D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (215 lines) via `read_file`; grep on this file for `MetaQuotes|DllImport|NativeLibrary|LoadLibrary|SMTManager|net8\.0-windows|PlatformTarget|x64|MT5API` returned **no matches**; cross-read host TFMs (`apps/api/TraderIntelligence.Api.csproj`, `src/Infrastructure/TraderIntelligence.Infrastructure.csproj`, `src/Mt5/TraderIntelligence.Mt5.csproj`) and prior measured load note `R021_dll_load.md`
- **verdict:** PASS

## Evidence quotes

`EfDashboardQueries` is an EF Core + in-memory runtime-status **read model**. It never references `MetaQuotes.MT5CommonAPI` / `MetaQuotes.MT5ManagerAPI`, never calls `SMTManagerAPIFactory.Initialize` / `CreateManager`, and never `LoadLibrary` / `NativeLibrary.Load` / `DllImport`s `MT5APIManager64.dll`. Constructor is store + `LiveRuntimeStatus` only:

```14:18:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public EfDashboardQueries(TraderDbContext db, LiveRuntimeStatus runtime)
    {
        _db = db;
        _runtime = runtime;
    }
```

Overview `Mt5Healthy` is a **count of already-published** `LiveRuntimeStatus.Brokers` connected flags, not a native factory probe. Destination PnL fields stay `0`. `RealCopyEnabled` is forwarded from the in-process flag (default `false` on `LiveRuntimeStatus`):

```31:50:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
        return new OverviewDto(
            accounts,
            brokers,
            xauTraders,
            three,
            scores.Count(s => s.CurrentState == TraderState.WATCH),
            scores.Count(s => s.CurrentState == TraderState.SHADOW),
            scores.Count(s => s.CurrentState == TraderState.LIVE_CANDIDATE),
            scores.Count(s => s.CurrentState == TraderState.LIVE),
            scores.Count(s => s.CurrentState == TraderState.RISK_BLOCKED),
            shadowPnl,
            0,
            0,
            0,
            _runtime.Brokers.Values.Count(b => b.Connected) > 0,
            quote?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.ReadyForMarketData or FixSessionStatus.ReadyForExecution
                || _runtime.Quote.LoggedOn,
            trade?.Status is FixSessionStatus.LoggedOn or FixSessionStatus.Reconciling or FixSessionStatus.ReadyForExecution
                || _runtime.Trade.LoggedOn,
            _runtime.RealCopyEnabled);
```

Broker rows use the same in-memory `Connected` bit. Groups / traders / detail are `Mt5Groups` / `Mt5Accounts` / `TraderScores` / `ReconstructedTrades` SQL only. FIX sessions hard-code `ExecutionEnabled=false`. Risk hard-codes `RealCopyEnabled=false`:

```196:207:D:/Prop/src/Infrastructure/Dashboard/EfDashboardQueries.cs
    public async Task<RiskDashboardDto> GetRiskAsync(CancellationToken ct)
    {
        var ks = await _db.KillSwitches.OrderByDescending(k => k.UpdatedAt).FirstOrDefaultAsync(ct);
        var rejects = await _db.RiskDecisions
            .Where(r => r.Outcome != RiskDecisionOutcome.Approve)
            .OrderByDescending(r => r.DecidedAt)
            .Take(20)
            .Select(r => r.Reason)
            .ToListAsync(ct);

        return new RiskDashboardDto(0, 0, 0, 0, 0, (ks?.Mode ?? KillSwitchMode.None).ToString(), false, rejects);
    }
```

This file does not contain:

- `using MetaQuotes.*` / `SMTManagerAPIFactory` / `CIMTManagerAPI`
- `MT5APIManager64.dll` / `MetaQuotes.MT5ManagerAPI64` / `MetaQuotes.MT5CommonAPI64`
- `TargetFramework` / `PlatformTarget` / `RuntimeIdentifier` (those live in `.csproj`, not this type)
- any P/Invoke or mixed-mode load

The assigned type is compiled by **Infrastructure**, which is already Windows x64:

```21:24:D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Vendor wrappers are bound on `src/Mt5` (not this file): `net8.0-windows` + `PlatformTarget` x64 + `Reference`/`None` copy of `MetaQuotes.MT5CommonAPI64`, `MetaQuotes.MT5ManagerAPI64`, `MT5APIManager64.dll`.

The angle names **API** TFM. Current `apps/api/TraderIntelligence.Api.csproj` **is** `net8.0-windows` x64 (so the premise “API not net8.0-windows x64” is false as of this read):

```17:19:D:/Prop/apps/api/TraderIntelligence.Api.csproj
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Prior measured load (`D:/Prop/reports/swarm/20260818/R021_dll_load.md`): isolated `net8.0` **and** `net8.0-windows` x64 both compiled against the vendor mixed-mode wrapper and called `SMTManagerAPIFactory.Initialize` / `CreateManager` on Windows x64 (**PASS**). Quote from that pin:

> Must the project be `net8.0-windows`? **No for the DLL itself.** Isolated `net8.0` compiled and loaded it. `net8.0-windows` is still the honest TFM for a process that will **run** this Windows mixed-mode image.

So even the historical “API was `net8.0`” state would not, by itself, make `LoadLibrary` of `MT5APIManager64.dll` impossible on a Windows x64 host. Native load still requires the PE trio beside the process (`Initialize` → `MT_RET_ERR_NOTFOUND` without `MT5APIManager64.dll`). That copy/load path is `NativeMt5BrokerConnector.ConnectCore` + `Mt5.csproj` `<None CopyToOutputDirectory>`, **not** `EfDashboardQueries`.

Stale `apps/api/bin/Debug/net8.0/` exists from an earlier TFM (runtimeconfig `tfm: net8.0`); `bin/Debug/net8.0-windows/` is currently an empty output folder. Those artifacts are not this file and do not change the source TFM or the fact that this type never loads the DLL.

DLL load / factory init lives **outside** this slice:

```66:70:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

## No-loss implication

`EfDashboardQueries` cannot prevent or cause MetaQuotes DLL load. It cannot `Initialize`, `Connect`, `DealerSend`, or emit FIX `NewOrderSingle`. Worst case inside this type is painting `Mt5Healthy` from `_runtime.Brokers` (false until ingest publishes `Connected`) and `RealCopyEnabled` from an in-memory flag that this class never sets to true (risk DTO forces `false`; FIX DTO forces `ExecutionEnabled=false`). A failed or skipped Manager DLL load elsewhere leaves this read model on EF + default `Connected=false` — fail-closed display, not an armed send path.

Slot 35 therefore has **no “API not net8.0-windows x64 so MetaQuotes cannot load” defect in the assigned file**. Current API + Infrastructure TFMs are already `net8.0-windows` / x64. Residual load risk (empty `net8.0-windows` output, missing native trio next to a host, NU1201 on other `net8.0` hosts such as `apps/mt5-worker`) is owned by those projects / `NativeMt5BrokerConnector`, not by `EfDashboardQueries`.

Empty-PASS justification: the assigned file was fully read (215/215 lines); the angle (API TFM blocking MetaQuotes load) is absent from this type by construction (no load, no TFM), and the stated API-TFM premise is currently false — not a skipped review.
