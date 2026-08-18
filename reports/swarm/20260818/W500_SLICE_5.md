# W500_SLICE_5

- **slot:** 5
- **file:** `D:/Prop/src/Application/Ingestion/DealIngestionService.cs`
- **angle:** API not `net8.0-windows` x64 so MetaQuotes DLL cannot load
- **read:** full file (127 lines) via `read_file`; grep on this file for `MetaQuotes|DllImport|LoadLibrary|SMTManager|PlatformTarget|net8` → **0 hits**. Supporting reads: `TraderIntelligence.Application.csproj`, `apps/api/TraderIntelligence.Api.csproj`, `apps/api/Program.cs`, `apps/api/obj/project.nuget.cache`, `Infrastructure/DependencyInjection.cs`, `Infrastructure/Mt5Live/LiveMt5Registration.cs`, `Mt5/TraderIntelligence.Mt5.csproj`, `Mt5/Connectors/NativeMt5BrokerConnector.cs`, `Mt5/Connectors/BrokerRegistry.cs`.
- **verdict:** FAIL

## Evidence quotes

`DealIngestionService` is TFM-neutral application orchestration. It does not reference MetaQuotes, does not `DllImport`, and does not set `PlatformTarget`. It **does** resolve a connector and call `ConnectAsync` on every `SyncBrokerAsync`:

```33:37:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
    public async Task<int> SyncBrokerAsync(string brokerCode, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var connector = _registry.Get(brokerCode);
        await connector.ConnectAsync(ct);
        var brokerId = await _store.ResolveBrokerIdAsync(brokerCode, ct);
```

The remainder of the method is read + persist only (`GetGroupsAsync` / `GetAccountsAsync` / `GetDealsAsync` or `IMt5BulkDealReader.GetGroupDealsAsync` / `GetPositionsAsync` → store upserts). No order send.

The type lives in Application, which is portable `net8.0` (not windows, no x64):

```11:15:D:/Prop/src/Application/TraderIntelligence.Application.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
```

The API host that injects this service is also portable `net8.0` with **no** `PlatformTarget` and **no** `RuntimeIdentifier`:

```15:19:D:/Prop/apps/api/TraderIntelligence.Api.csproj
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
```

That host still `ProjectReference`s `TraderIntelligence.Infrastructure` (`net8.0-windows` + `PlatformTarget` x64), which `ProjectReference`s `TraderIntelligence.Mt5` (same TFM/x64, `<Reference>` + copy of `MetaQuotes.MT5CommonAPI64.dll`, `MetaQuotes.MT5ManagerAPI64.dll`, `MT5APIManager64.dll`). Restore of the API is **already failed** (not a warning):

```1:4:D:/Prop/apps/api/obj/project.nuget.cache
{
  "version": 2,
  "dgSpecHash": "Cr5wykv0pP4=",
  "success": false,
```

```43:52:D:/Prop/apps/api/obj/project.nuget.cache
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
```

API `Program.cs` still maps this service as the live resync door:

```73:78:D:/Prop/apps/api/Program.cs
app.MapPost("/api/ops/resync", async (DealIngestionService ingestion, ReconstructionScoringService scoring, CancellationToken ct) =>
{
    var from = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    var to = DateTimeOffset.UtcNow;
    var a = await ingestion.SyncBrokerAsync("ACHIEVER", from, to, ct);
    var s = await ingestion.SyncBrokerAsync("STARWAVEFX", from, to, ct);
```

DI for that host registers **only** `NativeMt5BrokerConnector` (after a real-password gate) and `DealIngestionService`:

```33:45:D:/Prop/src/Infrastructure/DependencyInjection.cs
        if (!LiveMt5Registration.HasRealPasswords(configuration))
            throw new InvalidOperationException("Real MT5 passwords are required. Dummy/fake broker data is disabled.");

        foreach (var c in LiveMt5Registration.CreateConnectors(configuration))
            services.AddSingleton<IMt5BrokerConnector>(c);

        services.AddSingleton<IBrokerRegistry>(sp => new BrokerRegistry(sp.GetServices<IMt5BrokerConnector>()));
        services.AddScoped<ITradingStore, EfTradingStore>();
        services.AddScoped<IDashboardQueries, EfDashboardQueries>();
        services.AddSingleton<TradeReconstructor>();
        services.AddSingleton<BaselineScorer>();
        services.AddScoped<DealIngestionService>();
```

Native connect is the MetaQuotes factory load (Windows x64 only). That is **not** in the assigned file; it is what `ConnectAsync` on the DI connector becomes:

```61:70:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private void ConnectCore()
    {
        lock (_gate)
        {
            if (_connected)
                return;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

**Honesty on the angle:** isolated `net8.0` **can** load `MetaQuotes.MT5ManagerAPI64.dll` on a Windows x64 process (R021 measured). The product break is **not** “`net8.0` IL cannot `LoadLibrary`.” It is: the API exe is `net8.0` (not `net8.0-windows` x64) and **cannot restore** the `net8.0-windows` Infrastructure/Mt5 graph (`NU1201`). Therefore the API-hosted `DealIngestionService` path cannot bring the MetaQuotes trio into the process. Same NU1201 shape exists on `apps/mt5-worker` (`net8.0` → Infrastructure). Architecture (`docs/deployment.md`) already wants the Manager DLL on the **Windows worker**, not the Linux/portable API.

Grep on `D:/Prop/apps/api` for `MetaQuotes` → **0 hits**. API does not copy or name the vendor DLLs itself.

## No-loss implication

`DealIngestionService.SyncBrokerAsync` cannot place, cancel, or resize destination orders. It only `ConnectAsync` + pull groups/accounts/deals/positions + upsert. There is no FIX `NewOrderSingle`, no dealer send, no cTrader TRADE path, and no volume/price mutation toward a live book.

The FAIL is **host/TFM**: API `net8.0` cannot consume the windows-x64 MetaQuotes stack, so this service cannot load Manager64 **inside the API process**. That blocks live source ingest from `/api/ops/resync`; it does **not** open a capital-loss path. Worst case if the TFM were later retargeted to `net8.0-windows` x64 and `ConnectAsync` succeeded: history/position **reads** and DB writes, still no send. Slot 5 therefore has **no live capital-loss path** in the assigned file; the FAIL is ingest/DLL hostability only.
