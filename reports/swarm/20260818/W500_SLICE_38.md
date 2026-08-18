# W500_SLICE_38

- **slot:** 38
- **file:** `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full assigned file (31 lines) via `read_file`; grep `DealRequestByGroup|90.?day|FromDays\(90\)|Windows\(|AddDays\(14\)|DealRequestPage|timeout` on `D:/Prop/src/Mt5` and `DealRequestByGroup` under `D:/Prop/src`; followed compile-included `NativeMt5BrokerConnector.cs` plus callers `LiveIngestHostedService` / `DealIngestionService` / SDK `MT5APIManager.h`
- **verdict:** PASS

## File (assigned)

`TraderIntelligence.Mt5.csproj` is an SDK-style MSBuild project (31/31 lines). It has **zero** C# statements. It binds Domain + Application, official MetaQuotes Manager 64-bit assemblies, and copies `MT5APIManager64.dll` / wrapper DLLs to output. Implicit SDK compile includes every `.cs` under `D:/Prop/src/Mt5/`, including the only production `DealRequestByGroup` caller.

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

Grep of this `.csproj` for `DealRequestByGroup`, `90`, `timeout`, `chunk` is empty (XML only). That is **not** an empty-PASS skip: the angle lives in the compile graph this file owns. `NativeMt5BrokerConnector` implements `IMt5BulkDealReader` and is the sole `DealRequestByGroup` site under `D:/Prop/src`.

## Evidence quotes

### 1. Host still asks for 90 days; ingest forwards the whole window

```32:41:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);

            foreach (var connector in registry.All())
            {
                _log.LogInformation("Live ingest starting for {Broker}", connector.BrokerCode);
                try
                {
                    await connector.ConnectAsync(stoppingToken);
                    var n = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

```64:70:D:/Prop/src/Application/Ingestion/DealIngestionService.cs
        if (connector is IMt5BulkDealReader bulk)
        {
            foreach (var group in groups)
            {
                var deals = await bulk.GetGroupDealsAsync(group.Name, from, to, ct);
                insertedDeals += await _store.UpsertDealsBatchAsync(brokerId, deals, now, ct);
            }
        }
```

The 90-day `[UtcNow-90d, UtcNow+1m]` span is still the **caller** window. The question for this slot is whether that window is sent as **one** `DealRequestByGroup`.

### 2. Compiled connector chunks every group request into 14-day `Windows`

```296:366:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private IReadOnlyList<Mt5DealDto> GetGroupDealsCore(string group, DateTimeOffset from, DateTimeOffset to)
    {
        lock (_gate)
        {
            Ensure();
            var all = new List<Mt5DealDto>();
            foreach (var (start, end) in Windows(from, to))
            {
                var arr = _manager!.DealCreateArray();
                try
                {
                    var res = _manager.DealRequestByGroup(group, start.ToUnixTimeSeconds(), end.ToUnixTimeSeconds(), arr);
                    if (res != MTRetCode.MT_RET_OK && res != MTRetCode.MT_RET_OK_NONE && res != MTRetCode.MT_RET_ERR_NOTFOUND)
                        throw new InvalidOperationException(Describe(res, $"{BrokerCode} DealRequestByGroup {group}"));
                    all.AddRange(ReadDeals(arr));
                }
                finally { arr.Release(); }
            }

            return all;
        }
    }

    private static IEnumerable<(DateTimeOffset Start, DateTimeOffset End)> Windows(DateTimeOffset from, DateTimeOffset to)
    {
        var cursor = from;
        while (cursor < to)
        {
            var end = cursor.AddDays(14);
            if (end > to)
                end = to;
            yield return (cursor, end);
            cursor = end;
        }
    }
```

A 90-day ingest is therefore **~7** native `DealRequestByGroup` RPCs per group, not one 90-day dump. The same `Windows` splitter is used by per-login `DealRequest` (`GetDealsCore` L279). Adjacent windows share the `end` instant (`cursor = end`); that is a closed/open-boundary overlap of a single unix second, not an unchunked 90-day call.

This is a change vs older swarm notes (`W500_SLICE_18` / `W500_SLICE_28`) that quoted a one-shot `DealRequestByGroup(group, from, to, arr)` with the full caller range. That one-shot body is **gone** from the current file.

### 3. Fail-closed on Manager timeout / network; Connect timeout is separate

```443:455:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    private static string Describe(MTRetCode code, string op)
    {
        var hint = (int)code switch
        {
            7 => "network/timeout — check proxy, firewall, server",
            1012 => "manager IP blocked — Achiever requires the whitelist HTTP proxy",
            3 => "params/auth — check manager login",
            5 => "disk/no-connect in some builds — server unreachable",
            10 => "no connection",
            9 => "timeout",
            _ => code.ToString()
        };
        return $"{op} failed: {(int)code} {code} ({hint})";
    }
```

Codes 7 and 9 on a chunk throw. Host catch is fail-closed (no dummy book):

```55:57:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                catch (Exception ex)
                {
                    _log.LogError(ex, "{Broker} live ingest failed. No dummy data will be substituted.", connector.BrokerCode);
```

`Connect` uses `30000` ms (`ConnectCore` L92 / L101). There is still **no** per-`DealRequestByGroup` timeout argument (SDK signature is `group, from, to, array` only — `MT5APIManager.h:520`). `GetGroupDealsAsync` is `Task.Run(() => GetGroupDealsCore(...), ct)` (L51–52); `ct` is not observed inside the lock.

### 4. `DealRequestPage` still unused (residual, not the assigned defect)

SDK pages **per login**, not per group:

```520:526:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByTickets(const uint64_t *tickets,const uint32_t tickets_total,IMTDealArray* deals)=0;
   ...
   virtual MTAPIRES  DealRequestPage(const uint64_t login,const int64_t from,const int64_t to,const uint32_t offset,const uint32_t total,IMTDealArray* deals)=0;
```

Grep of `D:/Prop/src/Mt5` for `DealRequestPage` is **zero**. A59 still prefers 1–7 day chunks until paging exists. Current slice is **14 days**, wider than that guidance, but the assigned angle is **90-day without chunking**, which is no longer true.

`FakeMt5BrokerConnector` (same project) does **not** implement `IMt5BulkDealReader`; it cannot issue `DealRequestByGroup`.

### 5. Empty-PASS is not applicable

Assigned file was fully read (31/31 lines). The angle is **present in the compile unit** and was scored on the compiled `GetGroupDealsCore` / `Windows` path, not skipped because the `.csproj` is XML.

## No-loss implication

This project is Manager **read** + catalog/history ingest. The `.csproj` and `NativeMt5BrokerConnector` do not emit FIX `NewOrderSingle`, `DealerSend`, or any close/modify. Direct equity reduction from this file is **none**.

History-completeness risk that would have come from **one 90-day `DealRequestByGroup`** (timeout hang under `lock (_gate)`, silent cap of a huge group dump, reconstruction missing losers) is **mitigated** by 14-day windowing plus throw-on-timeout. Residual: a busy group’s 14-day dump can still stall the singleton connector mutex, `ct` cannot abort the native call, and there is no `DealRequestPage` completeness proof. Those residuals do not restore the assigned “90-day unchunked” defect. On throw, ingest logs and does **not** substitute dummy deals, so copy/score cannot treat a timed-out window as a clean book.

**Risk to capital:** none from order send. Operational completeness only; 90-day unchunked timeout path is closed in this compile unit.

## Verdict rationale

PASS: the assigned MT5 project’s only `DealRequestByGroup` site (`GetGroupDealsCore`) iterates `Windows(from, to)` at 14 days, so a host 90-day `GetGroupDealsAsync` is ~7 RPCs, fail-closed on codes 7/9, and is not a single unchunked 90-day Manager call.
