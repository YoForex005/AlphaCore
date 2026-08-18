# W500_SLICE_71

- **slot:** 71
- **file:** `D:/Prop/apps/web/src/pages/OverviewPage.tsx`
- **angle:** failure to fetch ALL manager groups
- **read:** full file (35/35 lines) via `read_file`; grep on this file for `group|Group|manager|useGroups|/api/groups|GroupTotal|GroupNext|Take\(|MT5_GROUP` returned **no matches**
- **adjacent (read, not this slice’s contract):** `apps/web/src/api/hooks.ts` `useOverview` / `useGroups`; `apps/web/src/pages/GroupsPage.tsx`; `Application/Dashboard/DashboardModels.cs` `OverviewDto` (no group list); `Infrastructure/Dashboard/EfDashboardQueries.GetOverviewAsync`; architecture §47 Overview tiles vs §7 Manager group census
- **verdict:** PASS

## Binding law (this angle)

Architecture v2 §7: `demo\Maxmaster` is not the only group; startup/resync must **dynamically enumerate all groups accessible to the Manager login** (Connect → enumerate groups → upsert → accounts → history). That is a Manager-API / ingest duty (`GetGroupsAsync` / `GroupTotal` / `GroupNext`), not an Overview tile.

Architecture v2 §47 Overview page (verbatim tiles): Total MT5 accounts, connected brokers, XAUUSD traders, ≥3 trades, Watch / Shadow / Live candidates / Live copied / Risk blocked, Shadow P&L, destination real P&L, XAU gross/net, dest margin, MT5 / QUOTE / TRADE health. **No group list, no group count, no manager-group walk.**

A91 §2.2: Overview is **read-only**; Brokers / Groups pages are out of Overview’s contract.

## Evidence quotes

`OverviewPage` is a 35-line read-only metric grid. Entire module:

```1:35:D:/Prop/apps/web/src/pages/OverviewPage.tsx
import MetricCard from '../components/MetricCard';
import { useOverview } from '../api/hooks';

export default function OverviewPage() {
  const { data, isLoading, error } = useOverview();
  if (isLoading) return <p className="text-gray-400">Loading overview…</p>;
  if (error) return <p className="text-red-400">API unavailable. Start the ASP.NET API on port 5000.</p>;
  if (!data) return null;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold text-white">Overview</h1>
        <p className="text-sm text-gray-400">First useful version: ingestion, reconstruction, baseline scores, shadow-ready. Live FIX send is off.</p>
      </div>
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        <MetricCard label="MT5 accounts" value={data.totalAccounts} />
        <MetricCard label="Brokers" value={data.connectedBrokers} />
        <MetricCard label="XAU traders" value={data.xauTraders} />
        <MetricCard label="≥ 3 trades" value={data.tradersWithThreeTrades} />
        <MetricCard label="Watch" value={data.watch} />
        <MetricCard label="Shadow" value={data.shadow} color="text-blue-300" />
        <MetricCard label="Live candidates" value={data.liveCandidates} />
        <MetricCard label="Risk blocked" value={data.riskBlocked} color="text-amber-300" />
        <MetricCard label="Shadow P&L" value={Number(data.shadowPnl).toFixed(2)} />
        <MetricCard label="Dest. real P&L" value={Number(data.destinationRealPnl).toFixed(2)} />
        <MetricCard label="MT5 health" value={data.mt5Healthy ? 'OK' : 'DOWN'} color={data.mt5Healthy ? 'text-emerald-300' : 'text-red-400'} />
        <MetricCard label="QUOTE / TRADE" value={`${data.quoteHealthy ? 'Q' : '-'} / ${data.tradeHealthy ? 'T' : '-'}`} />
      </div>
      <div className="rounded border border-gray-800 bg-gray-950 p-4 text-sm text-gray-300">
        Real copy execution: <strong className="text-amber-300">{data.realCopyEnabled ? 'ON' : 'OFF'}</strong>. Trade #3 never auto-promotes to LIVE.
      </div>
    </div>
  );
}
```

Sole data path is `useOverview()` → `GET /api/overview`. There is no second query, no groups key, and no Manager API call:

```4:5:D:/Prop/apps/web/src/api/hooks.ts
export function useOverview() {
  return useQuery({ queryKey: ['overview'], queryFn: () => client.get('/api/overview').then(r => r.data) });
```

The wire DTO this page consumes has no group collection and no group count:

```5:22:D:/Prop/src/Application/Dashboard/DashboardModels.cs
public sealed record OverviewDto(
    int TotalAccounts,
    int ConnectedBrokers,
    int XauTraders,
    int TradersWithThreeTrades,
    int Watch,
    int Shadow,
    int LiveCandidates,
    int Live,
    int RiskBlocked,
    decimal ShadowPnl,
    decimal DestinationRealPnl,
    decimal XauGross,
    decimal XauNet,
    bool Mt5Healthy,
    bool QuoteHealthy,
    bool TradeHealthy,
    bool RealCopyEnabled);
```

`GetOverviewAsync` counts `Mt5Accounts` / enabled `Brokers` / `TraderScores` / shadow slip / FIX session rows. It never reads `Mt5Groups` and never calls `GetGroupsAsync`.

Manager-group listing is a **different** page and hook (`GroupsPage` + `useGroups` → `GET /api/groups`). Ingestion enumerates groups in `DealIngestionService` via `connector.GetGroupsAsync`. Those files are out of this slice.

This file does not contain:

- `useGroups` / `/api/groups` / `GroupRowDto`
- `GroupTotal` / `GroupNext` / `GroupRequest` / `GetAllGroups` / `GetGroupsAsync`
- plan-map intersection (`MT5_GROUP_*`, `PlanMapping` as a fetch mask)
- pagination / `Take(` / first-N group cutoff
- any POST/PUT/DELETE or order-send

There is therefore no truncated or plan-filtered manager-group fetch on Overview. The page cannot “fail to fetch ALL groups” because it does not fetch groups.

## No-loss implication

Omitting group discovery on Overview cannot drop accounts from ingestion, cannot shrink the manager-visible universe, and cannot place, size, or cancel destination orders. Worst case is a dashboard that does not list groups (operators use `/groups` for that). Slot 71 has **no live capital-loss path** and **no missed-group execution path** in the assigned file.

Empty-PASS justification: the assigned file was fully read (35/35 lines); the angle (failure to fetch ALL manager groups) is absent by construction, not by skipped review.
