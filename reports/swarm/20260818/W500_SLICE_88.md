# W500_SLICE_88

- **slot:** 88
- **file:** `D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj`
- **angle:** DealRequestByGroup 90-day timeout without chunking
- **read:** full assigned file (31/31 lines) via `read_file`; grep on `D:/Prop/src/Mt5` for `DealRequestByGroup|90.?day|timeout|chunk` (hits only in `NativeMt5BrokerConnector.cs`); followed compile-included `GetGroupDealsCore` / `Windows`, callers `LiveIngestHostedService` + `DealIngestionService`, contract `IMt5BulkDealReader`, SDK `MT5APIManager.h:520–526`
- **verdict:** PASS

## File (assigned)

`TraderIntelligence.Mt5.csproj` is an SDK-style MSBuild project. It contains **zero** C# statements. Implicit `Microsoft.NET.Sdk` compile includes every `.cs` under `D:/Prop/src/Mt5/`, so this file is the compile owner of the only production `DealRequestByGroup` caller (`Connectors/NativeMt5BrokerConnector.cs`).

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

Grep of this `.csproj` for `DealRequestByGroup`, `90`, `timeout`, `chunk` is empty (XML only). That is **not** an empty-PASS skip: the angle is scored on the compile unit this project owns, not treated as absent because the assigned path is a project file.

`NativeMt5BrokerConnector` is the sole `IMt5BulkDealReader` implementer under `D:/Prop/src` and the sole `DealRequestByGroup` call site.

## Evidence quotes

### 1. Host still requests a 90-day ingest window

```37:64:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
            var from = DateTimeOffset.UtcNow.AddDays(-90);
            var to = DateTimeOffset.UtcNow.AddMinutes(1);

            foreach (var connector in registry.All())
            {
                var st = _runtime.Broker(connector.BrokerCode);
                st.Phase = "connecting";
                // ...
                    var deals = await ingest.SyncBrokerAsync(connector.BrokerCode, from, to, stoppingToken);
```

`DealIngestionService` forwards that same `[from, to]` to the bulk reader when the connector implements `IMt5BulkDealReader` (the native type compiled by this `.csproj`):

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

```71:74:D:/Prop/src/Application/Contracts/Mt5Contracts.cs
public interface IMt5BulkDealReader
{
    Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct);
}
```

The assigned defect is whether that 90-day span is sent as **one** unchunked `DealRequestByGroup`.

### 2. Compiled `GetGroupDealsCore` chunks via 14-day `Windows`

```51:52:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
    public Task<IReadOnlyList<Mt5DealDto>> GetGroupDealsAsync(string group, DateTimeOffset from, DateTimeOffset to, CancellationToken ct) =>
        Task.Run(() => GetGroupDealsCore(group, from, to), ct);
```

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

A host 90-day + 1-minute window is therefore **~7** native `DealRequestByGroup` RPCs per group (`90/14 ≈ 6.43`, plus the leftover days and the +1 minute), not one 90-day dump. `GetDealsCore` uses the same `Windows` splitter for per-login `DealRequest` (L279). Adjacent slices share the `end` unix second (`cursor = end`); that is a one-second closed/open overlap, not an unchunked 90-day call.

Older swarm notes (`W500_SLICE_18` / `W500_SLICE_28`) quoted a one-shot `DealRequestByGroup(group, from, to, arr)` with the full caller range. That body is **gone** from the current connector.

### 3. Timeout / network is fail-closed; there is no per-RPC timeout arg

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

Codes 7 and 9 on any 14-day chunk throw (`res != OK && != OK_NONE && != NOTFOUND`). Host catch does not invent a book:

```93:99:D:/Prop/src/Infrastructure/Hosting/LiveIngestHostedService.cs
                catch (Exception ex)
                {
                    st.Connected = false;
                    st.LastError = ex.GetType().Name + ": " + ex.Message;
                    st.Phase = "failed";
                    st.UpdatedAt = DateTimeOffset.UtcNow;
                    _log.LogError(ex, "{Broker} live ingest failed. No dummy data will be substituted.", connector.BrokerCode);
                }
```

`Connect` is bounded (`30000` ms at `ConnectCore` L92 / L101). The SDK `DealRequestByGroup` signature has **no** timeout parameter:

```520:526:D:/Prop/mt5-sdk/vendor/MetaTrader5SDK/Include/MT5APIManager.h
   virtual MTAPIRES  DealRequestByGroup(LPCWSTR group,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByLogins(const uint64_t *logins,const uint32_t logins_total,const int64_t from,const int64_t to,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealRequestByTickets(const uint64_t *tickets,const uint32_t tickets_total,IMTDealArray* deals)=0;
   virtual MTAPIRES  DealUpdateBatch(IMTDealArray* deals,MTAPIRES* results)=0;
   virtual MTAPIRES  DealUpdateBatchArray(IMTDeal** deals,const uint32_t deals_total,MTAPIRES* results)=0;
   virtual MTAPIRES  DealDeleteBatch(const uint64_t* tickets,const uint32_t tickets_total,MTAPIRES* results)=0;
   virtual MTAPIRES  DealRequestPage(const uint64_t login,const int64_t from,const int64_t to,const uint32_t offset,const uint32_t total,IMTDealArray* deals)=0;
```

`GetGroupDealsAsync` wraps `GetGroupDealsCore` in `Task.Run(..., ct)` but `ct` is **not** observed inside the lock; a hung native call still holds `_gate`.

### 4. Residuals that are **not** the assigned 90-day-unchunked defect

- Grep of `D:/Prop/src/Mt5` for `DealRequestPage` is **zero**. SDK paging is per-login anyway, not per-group.
- 14-day slices are wider than the 1–7 day preference in prior A59 notes, but the assigned angle is **90-day without chunking**, which is false.
- `FakeMt5BrokerConnector` (same project) does **not** implement `IMt5BulkDealReader`; it cannot issue `DealRequestByGroup`.

### 5. Empty-PASS is not applicable

Assigned file was fully read (31/31 lines). The angle is **present in the compile unit** and was scored on current `GetGroupDealsCore` / `Windows`, not skipped because the `.csproj` is XML.

## No-loss implication

This project is Manager **read** + catalog/history ingest. The `.csproj` and compiled connector do not emit FIX `NewOrderSingle`, `DealerSend`, or any close/modify. Direct equity reduction from this file is **none**.

History-completeness risk that would have come from **one 90-day `DealRequestByGroup`** (timeout hang under `lock (_gate)`, silent cap of a huge group dump, reconstruction missing losers / promoting a bad leader) is **mitigated** by 14-day windowing plus throw-on-timeout. Residual: a busy group’s 14-day dump can still stall the singleton connector mutex, `ct` cannot abort the native call, and there is no `DealRequestPage` completeness proof. Those residuals do **not** restore the assigned “90-day unchunked” defect. On throw, ingest logs and does **not** substitute dummy deals, so copy/score cannot treat a timed-out window as a clean book.

**Risk to capital:** none from order send. Operational completeness only; the 90-day unchunked timeout path is closed in this compile unit.

## Verdict rationale

PASS: the assigned MT5 project’s only `DealRequestByGroup` site (`GetGroupDealsCore`) iterates `Windows(from, to)` at 14 days, so a host 90-day `GetGroupDealsAsync` is ~7 RPCs, fail-closed on codes 7/9, and is not a single unchunked 90-day Manager call.
