# W500_SLICE_95

- **slot:** 95
- **file:** `D:/Prop/apps/web/src/pages/TradersPage.tsx`
- **angle:** API not net8.0-windows x64 so MetaQuotes DLL cannot load
- **read:** full file (42 lines) via `read_file`; cross-read `apps/web/src/api/hooks.ts`, `apps/api/TraderIntelligence.Api.csproj`, `src/Mt5/TraderIntelligence.Mt5.csproj`, `src/Infrastructure/TraderIntelligence.Infrastructure.csproj`, `src/Mt5/Connectors/NativeMt5BrokerConnector.cs`
- **verdict:** **PASS**

## Evidence quotes

`TradersPage.tsx` is a React leaderboard. It does not P/Invoke, load, or reference any MetaQuotes native DLL. The whole surface is `useTraders({})` → table cells:

```1:6:D:/Prop/apps/web/src/pages/TradersPage.tsx
import { Link } from 'react-router-dom';
import { useTraders } from '../api/hooks';

export default function TradersPage() {
  const { data = [], isLoading } = useTraders({});
  if (isLoading) return <p className="text-gray-400">Loading traders…</p>;
```

```25:38:D:/Prop/apps/web/src/pages/TradersPage.tsx
          {data.map((t: any) => (
            <tr key={`${t.broker}-${t.login}`} className="border-b border-gray-800 text-gray-200">
              <td className="py-2">{t.broker}</td>
              <td><Link className="text-blue-300" to={`/traders/${t.broker}/${t.login}`}>{t.login}</Link></td>
              <td>{t.group}</td>
              <td>{t.completedXauTrades}</td>
              <td>{Number(t.netSourcePnl).toFixed(2)}</td>
              <td>{Number(t.earlyScore).toFixed(1)}</td>
              <td>{Number(t.riskScore).toFixed(1)}</td>
              <td>{[t.martingale && 'MG', t.averagingDown && 'AVG', t.lotEscalation && 'ESC'].filter(Boolean).join(' ') || '—'}</td>
              <td>{t.state}</td>
            </tr>
          ))}
```

The hook is HTTP GET only (`/api/traders`). No native load path exists in the web app:

```20:25:D:/Prop/apps/web/src/api/hooks.ts
export function useTraders(filters: { broker?: string; state?: string }) {
  return useQuery({
    queryKey: ['traders', filters],
    queryFn: () => client.get('/api/traders', { params: filters }).then(r => r.data),
    refetchInterval: 5000,
  });
}
```

The claimed defect is that the **API** is not `net8.0-windows` x64, so `MetaQuotes.*.dll` / `MT5APIManager64.dll` cannot load. That claim is false for the host this page calls. `TraderIntelligence.Api` is Windows x64 and references the Mt5 project that binds the 64-bit Manager assemblies:

```3:22:D:/Prop/apps/api/TraderIntelligence.Api.csproj
    <ProjectReference Include="..\..\src\Domain\TraderIntelligence.Domain.csproj" />
    <ProjectReference Include="..\..\src\Application\TraderIntelligence.Application.csproj" />
    <ProjectReference Include="..\..\src\Infrastructure\TraderIntelligence.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\Mt5\TraderIntelligence.Mt5.csproj" />
    <ProjectReference Include="..\..\src\Fix.CTrader\TraderIntelligence.Fix.CTrader.csproj" />
  </ItemGroup>
  <!-- packages omitted -->
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

```6:29:D:/Prop/src/Mt5/TraderIntelligence.Mt5.csproj
    <Reference Include="MetaQuotes.MT5CommonAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5CommonAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <Reference Include="MetaQuotes.MT5ManagerAPI64">
      <HintPath>..\..\mt5-sdk\vendor\MetaTrader5SDK\Libs\MetaQuotes.MT5ManagerAPI64.dll</HintPath>
      <Private>true</Private>
    </Reference>
    <!-- CopyToOutputDirectory for MT5APIManager64.dll + both managed wrappers -->
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Infrastructure (also referenced by the API) matches the same TFM/platform:

```21:24:D:/Prop/src/Infrastructure/TraderIntelligence.Infrastructure.csproj
    <TargetFramework>net8.0-windows</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
```

Native initialize is gated on Windows and loads from `AppContext.BaseDirectory` (the API output dir when the API process hosts the connector):

```66:70:D:/Prop/src/Mt5/Connectors/NativeMt5BrokerConnector.cs
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException("Native MT5 Manager API is Windows x64 only.");

            var dllDir = _opt.NativeDllDirectory ?? AppContext.BaseDirectory;
            var init = SMTManagerAPIFactory.Initialize(dllDir);
```

This file contains no `DllImport`, no `net8.0` (non-windows) host, and no AnyCPU/x86 load of `MT5APIManager64.dll`. Slot 95’s hypothesized API TFM mismatch is not present.

Out-of-file note (not used to FAIL this slice): `apps/mt5-worker/TraderIntelligence.Mt5Worker.csproj` is `net8.0` without `PlatformTarget` x64. That is a separate host. The angle names the API; the API host that `/api/traders` runs on is `net8.0-windows` x64.

Empty-PASS justification: `TradersPage.tsx` was fully read (42 lines). The angle’s failure mode does not exist on this page or on the API it calls.

## No-loss implication

`TradersPage` is display-only GET. It cannot send, amend, or cancel destination orders, and it cannot load a MetaQuotes DLL in the browser. If Manager ingest failed to load the 64-bit DLL, this table would be empty or stale — fail-closed UI, not a live fill. Because the API that serves `/api/traders` **is** `net8.0-windows` + `x64` and copies `MetaQuotes.MT5*64` / `MT5APIManager64.dll`, Slot 95 does not create a “wrong RID → DLL cannot load → silent dummy book / runaway copy” path through this page. Capital is not at risk from this file.
